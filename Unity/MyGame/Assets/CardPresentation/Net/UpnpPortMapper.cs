// UpnpPortMapper.cs — 主机开局时，**自己向路由器要一条入站映射**（UPnP IGD，SSDP + SOAP）。
//
// 【为什么有它】用户 2026-09-27 拍板做（判据 → `资料/联机P2P_设计与交接.md` §11·7 的 **B 档**）。
//   「网友联机」三条路里的第 ③ 条（公网直连）要求**别人能连进来**；而 IPv6 那条在这台机器上
//   被路由器的 IPv6 NAT 挡着（同文档 §11·4 实测）⇒ **IPv4 的自动端口映射是唯一一条
//   不要求玩家装任何第三方工具的路**。
//
// 【它做什么】全在**后台线程**（主线程一帧都不能停）：
//   ① SSDP 发现路由器（UDP `239.255.255.250:1900` 发 M-SEARCH，**同时给网关发一份单播**）
//   ② 取设备描述 XML → 找 `WANIPConnection` / `WANPPPConnection` 的 `controlURL`
//   ③ `GetExternalIPAddress` —— **顺带判 CGNAT**（WAN 地址落在私网段 ⇒ 映射了外面也进不来）
//   ④ `AddPortMapping(TCP, 端口)`
//
// 🔴 **红线：不许静默失败**（`CLAUDE.md` §三）。四种失败各有各的人话，一律走
//   `NetRuntime.Notice` + `Debug.Log`：**路由器不支持 / UPnP 关着 / 大内网(CGNAT) / 拿不到外网地址**。
//   ⚠️ **失败是常态**（很多路由器默认关 UPnP）⇒ 不许当异常吞掉，也不许假装成功。
//
// 🔴 **与 IPv6 那条路完全独立**：SSDP 走 IPv4 组播，**不碰** `NetTransport` 那张双栈 socket，
//   也不改任何 IPv6 行为。反过来也要说清：**它对 IPv6 入站毫无帮助**（见 §11·7）。
//
// ⚠️ **NAT-PMP 没做**（这是**有意的、不是漏做**）：本机路由器（TP-LINK WTA301）走的是 UPnP，
//    NAT-PMP 那条路**在这台机器上一步都跑不到** ⇒ 写了也是**没验过的代码**。
//    真需要时（苹果路由器一类）再加，加的时候要能说清「拿什么验的」。
//
// 【自检】纯函数（XML 解析 / SOAP 构造 / CGNAT 判定）在 `NetSelfTest` 里逐条断言；
//   **真发 SSDP 那一段只能真 Play 验** ⇒ `资料/真Play待验清单.md` **E7**。
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace CardPresentation.Net
{
    /// <summary>主机开局时自动开一条 IPv4 入站映射。**纯静态、无状态**（结果落在 <see cref="Last"/>）。</summary>
    public static class UpnpPortMapper
    {
        /// <summary>映射项在路由器上显示的名字（玩家在路由器管理页里能认出这是谁开的）。</summary>
        public const string MappingDescription = "Warpforge";

        const string SsdpAddr = "239.255.255.250";
        const int SsdpPort = 1900;
        /// <summary>收 SSDP 回应等多久。**不要太长** —— 主机点【保存】之后要马上给人话。</summary>
        const int DiscoverMs = 2500;
        const int HttpTimeoutMs = 3000;

        public enum Outcome
        {
            Ok,             // 映射成功
            Cgnat,          // 映射成功，但路由器 WAN 是私网 ⇒ **外面照样连不进来**（要如实说）
            NoRouter,       // SSDP 一个回应都没有（UPnP 关着 / 路由器不支持 / 网络不对）
            NoIgdService,   // 找到设备了，但它没提供 WAN 端口映射服务
            Failed,         // SOAP 被拒（附错误码）
            Error,          // 异常（附消息）
        }

        /// <summary>一次映射尝试的全部事实。**人话在 <see cref="message"/>**（要直接给玩家看）。</summary>
        public struct Result
        {
            public Outcome outcome;
            public string serviceType;   // `urn:schemas-upnp-org:service:WANIPConnection:1` 之类
            public string controlUrl;    // 相对 LOCATION 解析过、能直接 POST 的绝对地址
            public string externalIp;    // 路由器 WAN 地址（拿到才有）
            public string internalIp;    // 本机在局域网里的地址
            public int port;
            public string message;       // 人话
            public string detail;        // 机器可读的那点细节（错误码 / 异常），日志用
        }

        /// <summary>最近一次结果（主线程读它 —— 后台线程**不许直接摸 Unity 的窗口系统**）。</summary>
        public static Result Last { get; private set; }

        /// <summary>总开关。**自检要关掉它** —— 见 <see cref="MapAsync"/> 里那条「批处理一律不跑」。</summary>
        public static bool Enabled = true;

        /// <summary>正在跑 / 跑过没有（同一个端口不重复发起）。</summary>
        static int _busy;

        // ==================================================================
        //  真跑（后台线程）
        // ==================================================================

        /// <summary>给 `port` 要一条 TCP 入站映射。**立刻返回**；结果通过 <paramref name="done"/>
        /// 在**后台线程**回调（调用方要自己切回主线程才能碰 Unity 对象）。
        /// ⚠️ 同一个进程同一时刻只跑一次（`_busy`）—— 路由器那边是**慢设备**，并发请求会被它丢。
        ///
        /// 🔴 **批处理（自检）里一律不跑**：这件事**会改玩家路由器的映射表** ——
        ///    自检里反复开台就是反复去动玩家的网络设备，**测试不该有那种副作用**
        ///    （而且每次都要等 2.5 秒 SSDP）。⇒ `Application.isBatchMode` 直接返回，
        ///    并**打一行日志说出来**（红线：不许静默 —— 免得有人以为「它跑了但没成」）。
        ///    纯函数那半边（XML/绝对化/CGNAT 判定）照常在 `NetSelfTest` 里逐条断言。</summary>
        public static void MapAsync(int port, Action<Result> done = null)
        {
            if (port <= 0 || port > 65535) return;                 // 端口本身不合法，不必去问路由器
            if (!Enabled || Application.isBatchMode)
            {
                Debug.Log("[UPnP] 批处理/自检里**不向路由器要端口**（那会改玩家路由器上的映射表）"
                        + "—— 这条只在真 Play 里跑");
                return;
            }
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                // 已经有一次在跑 ⇒ 不重复发起（但**也不静默**：如实说这一条）
                var r0 = new Result { port = port, outcome = Outcome.Error,
                                      message = "上一次「向路由器要端口」还没跑完 —— 这次先跳过。",
                                      detail = "busy" };
                Last = r0;
                if (done != null) done(r0);
                return;
            }

            var th = new Thread(() =>
            {
                Result r;
                try { r = Map(port); }
                catch (Exception e)
                {
                    r = new Result { port = port, outcome = Outcome.Error,
                                     message = "向路由器要端口时出错：" + e.Message, detail = e.ToString() };
                }
                Last = r;
                try { NetRuntime.Notice(r.message); } catch { }        // 红线：必须让玩家看得见
                Debug.Log("[UPnP] " + r.message + (string.IsNullOrEmpty(r.detail) ? "" : "（" + r.detail + "）"));
                if (done != null) { try { done(r); } catch { } }
                Interlocked.Exchange(ref _busy, 0);
            }) { IsBackground = true, Name = "wf-net-upnp" };
            th.Start();
        }

        /// <summary>撤掉映射（关台/退出时调）。**失败只记日志** —— 玩家已经要走了，别再弹窗烦他。</summary>
        public static void UnmapAsync(int port)
        {
            var prev = Last;
            if (prev.outcome != Outcome.Ok && prev.outcome != Outcome.Cgnat) return;
            if (string.IsNullOrEmpty(prev.controlUrl) || string.IsNullOrEmpty(prev.serviceType)) return;
            var th = new Thread(() =>
            {
                try
                {
                    string body = "<NewRemoteHost></NewRemoteHost>"
                                + "<NewExternalPort>" + port + "</NewExternalPort>"
                                + "<NewProtocol>TCP</NewProtocol>";
                    Soap(prev.controlUrl, prev.serviceType, "DeletePortMapping", body);
                    Debug.Log("[UPnP] 已撤掉 " + port + " 端口上的映射");
                }
                catch (Exception e) { Debug.LogWarning("[UPnP] 撤映射没成功（不影响本次联机）：" + e.Message); }
            }) { IsBackground = true, Name = "wf-net-upnp-del" };
            th.Start();
        }

        static Result Map(int port)
        {
            var r = new Result { port = port };
            r.internalIp = PickLocalIPv4();

            // ---- ① SSDP ----
            string location = Discover(DiscoverMs);
            if (string.IsNullOrEmpty(location))
            {
                r.outcome = Outcome.NoRouter;
                r.message = "没能从路由器那里问到端口映射（UPnP 没开、或路由器不支持）。\n"
                          + "→ 想让网友连进来：① 去路由器管理页把 **UPnP 打开** 再点一次【保存】；"
                          + "② 或者两边装同一个虚拟局域网工具（Tailscale / ZeroTier 这类，见【怎么联机】）。";
                r.detail = "SSDP 0 回应";
                return r;
            }

            // ---- ② 设备描述 ----
            string xml = HttpGet(location);
            string svc;
            string ctrl = FindControlUrl(xml, out svc);
            if (string.IsNullOrEmpty(ctrl))
            {
                r.outcome = Outcome.NoIgdService;
                r.message = "路由器回应了，但它**没有提供端口映射服务**（不是常见的家用路由器固件）。\n"
                          + "→ 这条只能走虚拟局域网工具那条路（见【怎么联机】）。";
                r.detail = "设备描述里没有 WANIPConnection / WANPPPConnection";
                r.serviceType = svc;
                return r;
            }
            r.serviceType = svc;
            r.controlUrl = Absolutize(location, ctrl);

            // ---- ③ 先问 WAN 地址（顺带判 CGNAT）----
            string wan = "";
            try
            {
                string resp = Soap(r.controlUrl, svc, "GetExternalIPAddress", "");
                wan = ParseTag(resp, "NewExternalIPAddress");
                r.externalIp = wan;
            }
            catch (Exception e) { r.detail = "取外网地址失败：" + e.Message; }   // 拿不到也继续试映射

            // ---- ④ 要映射 ----
            string addBody = "<NewRemoteHost></NewRemoteHost>"
                           + "<NewExternalPort>" + port + "</NewExternalPort>"
                           + "<NewProtocol>TCP</NewProtocol>"
                           + "<NewInternalPort>" + port + "</NewInternalPort>"
                           + "<NewInternalClient>" + r.internalIp + "</NewInternalClient>"
                           + "<NewEnabled>1</NewEnabled>"
                           + "<NewPortMappingDescription>" + MappingDescription + "</NewPortMappingDescription>"
                           + "<NewLeaseDuration>0</NewLeaseDuration>";     // 0 = 永久（IGD:1）
            string addResp;
            try { addResp = Soap(r.controlUrl, svc, "AddPortMapping", addBody); }
            catch (WebException we)
            {
                int code = SoapErrorCode(we);
                // 725 = `OnlyPermanentLeasesSupported`（有些固件只认 0，我们已经给 0）
                // 718 = `ConflictInMappingEntry`（那条端口上已经有别人的映射）
                r.outcome = Outcome.Failed;
                r.detail = "AddPortMapping 被拒：错误码 " + code;
                if (code == 718)
                    r.message = "路由器说 **" + port + " 这个端口上已经有别的映射了** ⇒ 换一个端口再来"
                              + "（或者去路由器管理页把那条旧映射删掉）。";
                else if (code == 725)
                    r.message = "路由器不接受「永久」映射（错误码 725）—— 这一台得手动在路由器上做端口映射。";
                else
                    r.message = "路由器**拒绝了**端口映射请求（错误码 " + code + "）。\n"
                              + "→ 有些固件即使开着 UPnP 也不放行入站映射，这条只能走别的路（见【怎么联机】）。";
                return r;
            }

            // ---- 成功：再判一次 CGNAT ----
            if (!string.IsNullOrEmpty(wan) && !IsPublicIpv4(wan))
            {
                r.outcome = Outcome.Cgnat;
                r.message = "✅ 端口映射要到了，**但你这台大概率在「大内网」(CGNAT) 里** ——\n"
                          + "路由器自己的外网地址是 " + wan + "（**私网段**）⇒ 外面照样连不进来。\n"
                          + "→ 这种情况打客服电话要「公网 IP」才有用，或走虚拟局域网工具。";
            }
            else
            {
                r.outcome = Outcome.Ok;
                r.message = "✅ 已经在路由器上开好了 " + port + " 端口（TCP）"
                          + (string.IsNullOrEmpty(wan) ? "" : "，你家的外网地址是 " + wan)
                          + " —— 把**外网地址 + 端口**给朋友就能连进来。";
            }
            return r;
        }

        // ==================================================================
        //  纯函数（自检直接调这些）
        // ==================================================================

        /// <summary>从设备描述 XML 里找端口映射服务的 `controlURL`。
        /// 优先 `WANIPConnection`（IP 型 WAN），其次是 `WANPPPConnection`（PPPoE 型，电信宽带常见）。
        /// 找不到返回 null（`serviceType` 给 null）。</summary>
        public static string FindControlUrl(string deviceXml, out string serviceType)
        {
            serviceType = null;
            if (string.IsNullOrEmpty(deviceXml)) return null;
            // 按 <service> 逐块看：**先出现的那种服务不一定是我们想要的**，
            // 所以要看的是「哪一块的 serviceType 里带 WAN 连接」
            string[] pref = { "WANIPConnection", "WANPPPConnection" };
            for (int p = 0; p < pref.Length; p++)
            {
                int at = 0;
                while (true)
                {
                    int i = deviceXml.IndexOf(pref[p], at, StringComparison.Ordinal);
                    if (i < 0) break;
                    at = i + pref[p].Length;
                    // 这一块的边界：往前找最近的 `<service>`，往后找 `</service>`
                    int s = deviceXml.LastIndexOf("<service>", i, StringComparison.Ordinal);
                    int e = deviceXml.IndexOf("</service>", i, StringComparison.Ordinal);
                    if (s < 0 || e < 0) continue;                    // 不是标准块，跳过
                    string block = deviceXml.Substring(s, e - s);
                    string svc = ParseTag(block, "serviceType");
                    string ctl = ParseTag(block, "controlURL");
                    if (!string.IsNullOrEmpty(svc) && !string.IsNullOrEmpty(ctl))
                    { serviceType = svc; return ctl; }
                }
            }
            return null;
        }

        /// <summary>取 `<tag>值</tag>` 里的值（**不含** CDATA / 命名空间前缀那套 —— 设备描述用的是最朴素的形式）。
        /// 取不到返回空串。⚠️ 值里带 `<![CDATA[…]]>` 时剥掉外壳（少数固件这么写）。</summary>
        public static string ParseTag(string xml, string tag)
        {
            if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(tag)) return "";
            string open = "<" + tag + ">", close = "</" + tag + ">";
            int i = xml.IndexOf(open, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return "";
            i += open.Length;
            int j = xml.IndexOf(close, i, StringComparison.OrdinalIgnoreCase);
            if (j < 0) return "";
            string v = xml.Substring(i, j - i).Trim();
            if (v.StartsWith("<![CDATA[", StringComparison.Ordinal) && v.EndsWith("]]>", StringComparison.Ordinal))
                v = v.Substring(9, v.Length - 12);
            return v;
        }

        /// <summary>把 `controlURL`（设备描述里通常是 `/ipc` 这种相对路径）变成能直接 POST 的绝对地址。
        /// ⚠️ **不解析就是发不出去**（`WebRequest` 只吃绝对 URI）—— 这是 UPnP 里最容易踩的一步。</summary>
        public static string Absolutize(string location, string controlUrl)
        {
            if (string.IsNullOrEmpty(controlUrl)) return "";
            if (controlUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
             || controlUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return controlUrl;
            Uri b;
            if (!Uri.TryCreate(location, UriKind.Absolute, out b)) return "";
            return new Uri(b, controlUrl).AbsoluteUri;
        }

        /// <summary>组 SOAP 请求体（IGD 用的是 SOAP 1.1，`SOAPACTION` 那个头是**必须**的）。</summary>
        public static string BuildSoap(string serviceType, string action, string body)
        {
            return "<?xml version=\"1.0\"?>"
                 + "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
                 + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                 + "<s:Body><u:" + action + " xmlns:u=\"" + serviceType + "\">"
                 + body
                 + "</u:" + action + "></s:Body></s:Envelope>";
        }

        /// <summary>这个 IPv4 地址**是不是公网**。私网 / 大内网(CGNAT) / 保留段一律 `false`。
        /// 🔴 **CGNAT 判定就靠它**（`100.64.0.0/10` 是 RFC 6598 的运营商级 NAT 段）——
        /// 路由器 WAN 落在这些段里 ⇒ **端口映射做了也没用**。</summary>
        public static bool IsPublicIpv4(string ip)
        {
            IPAddress a;
            if (!IPAddress.TryParse(ip ?? "", out a)) return false;
            if (a.AddressFamily != AddressFamily.InterNetwork) return false;
            byte[] b = a.GetAddressBytes();
            if (b[0] == 10) return false;                                   // 10/8
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;      // 172.16/12
            if (b[0] == 192 && b[1] == 168) return false;                   // 192.168/16
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false;     // 100.64/10 ← CGNAT
            if (b[0] == 169 && b[1] == 254) return false;                   // 169.254/16
            if (b[0] == 127) return false;                                  // 回环
            if (b[0] == 0) return false;
            if (b[0] >= 224) return false;                                  // 组播 / 保留
            return true;
        }

        /// <summary>从 SOAP 错误响应里抠出 UPnP 错误码（抠不到给 0）。</summary>
        public static int SoapErrorCode(WebException we)
        {
            try
            {
                if (we != null && we.Response != null)
                {
                    using (var s = we.Response.GetResponseStream())
                    using (var rd = new StreamReader(s))
                    {
                        string body = rd.ReadToEnd();
                        string v = ParseTag(body, "errorCode");
                        int code;
                        if (int.TryParse(v, out code)) return code;
                    }
                }
            }
            catch { }
            return 0;
        }

        // ==================================================================
        //  底层 I/O
        // ==================================================================

        /// <summary>SSDP 发现：组播发一份，**再给默认网关单播发一份**
        /// （🔴 有些固件不应组播、只应单播 —— 只发组播会在那种路由器上白等一场）。
        /// 返回第一个回应的 `LOCATION`，没有就返回 null。</summary>
        static string Discover(int waitMs)
        {
            string found = null;
            var req = "M-SEARCH * HTTP/1.1\r\n"
                    + "HOST: " + SsdpAddr + ":" + SsdpPort + "\r\n"
                    + "MAN: \"ssdp:discover\"\r\n"
                    + "MX: 2\r\n"
                    + "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(req);

            using (var udp = new UdpClient())
            {
                udp.Client.ReceiveTimeout = 400;
                try { udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); } catch { }
                try { udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse(SsdpAddr), SsdpPort)); }
                catch (Exception e) { Debug.LogWarning("[UPnP] 组播发不出去：" + e.Message); }

                // 单播那一份：网关地址来自网卡属性（不是猜的）
                foreach (var gw in Gateways())
                {
                    try { udp.Send(bytes, bytes.Length, new IPEndPoint(gw, SsdpPort)); } catch { }
                }

                var deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        var from = new IPEndPoint(IPAddress.Any, 0);
                        byte[] resp = udp.Receive(ref from);
                        string txt = Encoding.ASCII.GetString(resp);
                        string loc = HeaderOf(txt, "LOCATION");
                        if (!string.IsNullOrEmpty(loc)) { found = loc; break; }
                    }
                    catch (SocketException) { /* 超时：继续等到 deadline */ }
                    catch { break; }
                }
            }
            return found;
        }

        /// <summary>本机的默认网关（可能有多块网卡 ⇒ 多个）。拿不到就空。</summary>
        static List<IPAddress> Gateways()
        {
            var r = new List<IPAddress>();
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    foreach (var g in ni.GetIPProperties().GatewayAddresses)
                        if (g != null && g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork)
                            r.Add(g.Address);
                }
            }
            catch { }
            return r;
        }

        /// <summary>HTTP 响应头里的某个字段（大小写不敏感）。</summary>
        static string HeaderOf(string http, string name)
        {
            if (string.IsNullOrEmpty(http)) return null;
            var lines = http.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i].Trim();
                int c = ln.IndexOf(':');
                if (c <= 0) continue;
                if (string.Equals(ln.Substring(0, c).Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return ln.Substring(c + 1).Trim();
            }
            return null;
        }

        static string HttpGet(string url)
        {
            var rq = (HttpWebRequest)WebRequest.Create(url);
            rq.Method = "GET";
            rq.Timeout = HttpTimeoutMs;
            rq.ReadWriteTimeout = HttpTimeoutMs;
            using (var rs = rq.GetResponse())
            using (var s = rs.GetResponseStream())
            using (var rd = new StreamReader(s))
                return rd.ReadToEnd();
        }

        static string Soap(string controlUrl, string serviceType, string action, string body)
        {
            string payload = BuildSoap(serviceType, action, body);
            byte[] data = Encoding.UTF8.GetBytes(payload);
            var rq = (HttpWebRequest)WebRequest.Create(controlUrl);
            rq.Method = "POST";
            rq.ContentType = "text/xml; charset=\"utf-8\"";
            rq.Headers["SOAPACTION"] = "\"" + serviceType + "#" + action + "\"";
            rq.Timeout = HttpTimeoutMs;
            rq.ReadWriteTimeout = HttpTimeoutMs;
            rq.ContentLength = data.Length;
            using (var st = rq.GetRequestStream()) st.Write(data, 0, data.Length);
            using (var rs = rq.GetResponse())
            using (var s = rs.GetResponseStream())
            using (var rd = new StreamReader(s))
                return rd.ReadToEnd();
        }

        /// <summary>本机在局域网里的 IPv4（给路由器填「转发到这个地址」）。取不到给空串。</summary>
        static string PickLocalIPv4()
        {
            try
            {
                var all = NetConfig.LocalAddresses();
                for (int i = 0; i < all.Count; i++)
                    if (!all[i].isV6 && all[i].Usable) return all[i].addr;
            }
            catch { }
            return "";
        }
    }
}
