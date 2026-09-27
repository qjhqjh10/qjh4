// NetConfig.cs — 联机设置（主机/客机 · IP · 密码 · 端口）的**存放与落盘**，外加「取本机 IP」那个刷新按钮要用的东西。
//
// 存在哪：`Application.persistentDataPath/WarpforgeNet.json`
//   —— 和 `RuleEngine/Data/DeckStore.cs` 同一套（`JsonUtility` + 一个给自检用的 `OverridePath`）。
//   ⚠️ **密码是明文存本地文件的**：这是「双人自己玩」的设置项，不是账号口令；
//   要藏起来得引入密钥链，与本项目的用途不成比例。**这一点写在这里，别当成 bug。**
//
// 用户给的界面规格（照抄 → `资料/联机P2P_设计与交接.md` §一）：
//   · 勾选「主机 / 客机」；主机：IP + 密码 + 【保存】，IP 输入框右边一个【刷新】（填本机 IP）；
//   · 客机：IP + 密码 + 【检查连接】（点了自动保存 + 试连 + 反馈成败）。
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace CardPresentation.Net
{
    /// <summary>联机设置（落盘的那份）。</summary>
    [Serializable]
    public class NetConfigData
    {
        /// <summary>照 <see cref="NetRole"/>：0=没开 · 1=主机 · 2=客机。</summary>
        public int role = 0;
        /// <summary>客机连谁 / 主机自己给出去的那个地址（主机侧只是**显示**用，监听永远绑 `IPAddress.Any`）。</summary>
        public string ip = "";
        public string password = "";
        public int port = NetConfig.DefaultPort;
    }

    public static class NetConfig
    {
        public const string FileName = "WarpforgeNet.json";
        public const int DefaultPort = 47777;

        /// <summary>自检用的路径改写口（设了就不动玩家的真设置）—— 与 `DeckStore.OverridePath` 同一套路。</summary>
        public static string OverridePath;

        static NetConfigData _cur;

        public static string Path
        {
            get { return OverridePath ?? System.IO.Path.Combine(Application.persistentDataPath, FileName); }
        }

        /// <summary>当前设置（第一次访问时读盘；读不出来就返回出厂值，**不抛**）。</summary>
        public static NetConfigData Current
        {
            get
            {
                if (_cur == null) Load();
                return _cur;
            }
        }

        public static void Load()
        {
            _cur = new NetConfigData();
            try
            {
                if (File.Exists(Path))
                {
                    var txt = File.ReadAllText(Path);
                    var d = JsonUtility.FromJson<NetConfigData>(txt);
                    if (d != null) _cur = d;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] 读设置失败（用出厂值继续）：{e.Message}");
            }
            if (_cur.port <= 0 || _cur.port > 65535) _cur.port = DefaultPort;   // 存档被改坏时的兜底
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(Current, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Net] 存设置失败：{e.Message}（路径 {Path}）");
            }
        }

        // ---- 界面要用的两个动作（主机【保存】/ 客机【检查连接】都走它们）----

        /// <summary>主机按【保存】：记下角色 + 端口 + 密码，并**开始监听**。</summary>
        public static void SaveAsHost(string ip, string password, int port)
        {
            var c = Current;
            c.role = (int)NetRole.Host;
            c.ip = ip ?? "";
            c.password = password ?? "";
            if (port > 0 && port <= 65535) c.port = port;
            Save();
        }

        /// <summary>客机按【检查连接】：**先自动保存**，再交给 `NetSession` 去试连（用户规格里写死的顺序）。</summary>
        public static void SaveAsClient(string ip, string password, int port)
        {
            var c = Current;
            c.role = (int)NetRole.Client;
            c.ip = ip ?? "";
            c.password = password ?? "";
            if (port > 0 && port <= 65535) c.port = port;
            Save();
        }

        // ---- 【刷新】按钮：列本机所有可用地址，让玩家自己挑 ----
        //
        // 🔴 **2026-09-26 换掉了实现**：原来走 `Dns.GetHostAddresses(Dns.GetHostName())`，
        //    **实测在批处理里抛 `String conversion error: Illegal byte sequence encounted in the input.`**
        //    ⇒ 【刷新】根本填不出来（只能回落 `127.0.0.1`）。
        //    现在**直接枚举网卡**（`NetworkInterface`）：不查 DNS、不依赖机器名；
        //    顺手还能拿到**网卡名**和**是不是虚拟网卡** —— 多网卡/VPN 时这两样正是玩家挑地址要看的。

        /// <summary>一个候选地址：地址 + 它来自哪块网卡 + 是不是虚拟网卡（VPN / 虚拟机 / 隧道）。</summary>
        public struct LocalAddr
        {
            public string addr;      // 192.168.x.x / 100.x.x.x / 2001:…
            public string nic;       // 网卡名（界面要显示 —— 多网卡时靠它认）
            public bool isV6;
            public bool isVirtual;
            public bool isLoopback;
            /// <summary>界面上那一行怎么显示。</summary>
            public string Label { get { return nic + "  " + addr; } }

            /// <summary>**能不能真的给对面填**。排除三类「看着像地址、其实出不去」的：
            /// · **回环**（`127.0.0.1` / `::1`）；
            /// · **IPv4 链路本地**（`169.254.x.x`，APIPA）—— **网线没插/没拿到 DHCP 时会有它**，
            ///   而且它往往排在真地址前面 ⇒ 不排除的话【刷新】第一下就填个废地址；
            /// · **IPv6 出不了公网的四种**：链路本地 `fe80::/10` · 唯一本地 `fc00::/7`（`fc..` / `fd..`）
            ///   · **Teredo `2001:0::/32`** · **6to4 `2002::/16`**（后两种见 <see cref="V6Routable"/>）。
            /// ✅ 剩下的是 **IPv4 私有段（192.168 / 10.x / 172.16-31）与 IPv6 全局（`2000::/3`）** ——
            ///    前者局域网里能用，后者**公网直连能用**。</summary>
            public bool Usable
            {
                get
                {
                    if (isLoopback) return false;
                    if (!isV6) return !addr.StartsWith("169.254.");
                    return V6Routable(addr);
                }
            }
        }

        /// <summary>这个 IPv6 地址**能不能拿去给对面直连**。
        /// 🔴 **2026-09-27 修的一条真缺陷**：原来这条判据是「字符串前缀不是 `fe80` / `fc` / `fd` 就算公网」，
        ///    于是 **Teredo 与 6to4 会被当成「公网 IPv6」列出来** —— 这两种**都落在 `2000::/3` 里、
        ///    却都不能用于入站直连**：
        ///    · **Teredo `2001:0::/32`** —— 它是**隧道**（把 v6 包塞进 v4 UDP 出去），地址由 Teredo 服务器分，
        ///      别人主动连它要经过中继 ⇒ **当不了主机**；
        ///    · **6to4 `2002::/16`** —— 老式过渡机制（地址里嵌一个公网 v4），如今绝大多数网络
        ///      已经没有 6to4 中继 ⇒ 同样连不上。
        ///    ⇒ 判据改成**逐段判字节**（不判字符串前缀：`2001::` 这种压缩写法前缀对不上）。
        /// 📌 出处：2026-09-27 用户问「我在 IPv6 测试网站上明明看得到 IPv6，你这里为什么看不到」
        ///    —— 那一轮顺带查出我们自己这条判据会**误报**（判据全文 → `资料/联机P2P_设计与交接.md` §11·4）。</summary>
        public static bool V6Routable(string addr)
        {
            IPAddress a;
            if (!IPAddress.TryParse(addr ?? "", out a)) return false;
            if (a.AddressFamily != AddressFamily.InterNetworkV6) return false;
            byte[] b = a.GetAddressBytes();
            if ((b[0] & 0xfe) == 0xfc) return false;                                          // fc00::/7  ULA
            if (b[0] == 0xfe && (b[1] & 0xc0) == 0x80) return false;                           // fe80::/10 链路本地
            if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x00) return false;    // 2001:0::/32 Teredo
            if (b[0] == 0x20 && b[1] == 0x02) return false;                                    // 2002::/16  6to4
            if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8) return false;    // 2001:db8::/32 文档用
            return true;
        }

        static bool LooksVirtual(string nic)
        {
            if (string.IsNullOrEmpty(nic)) return false;
            string s = nic.ToLowerInvariant();
            string[] keys = { "virtual", "vmware", "vbox", "hyper-v", "vethernet", "tailscale",
                              "zerotier", "hamachi", "tap-", "tun", "wireguard", "loopback",
                              "bluetooth", "npcap", "docker", "wsl" };
            foreach (var k in keys) if (s.Contains(k)) return true;
            return false;
        }

        /// <summary>本机**所有**候选地址，已排好序（回环最后 · **IPv4 优先** · 物理网卡优先 · 再按网卡名）。
        /// ⚠️ **虚拟网卡不排除** —— 玩家用 Tailscale / ZeroTier 联机时，
        /// **那张虚拟网卡的地址才是要给对面的**（所以只是把它排后面 + 让界面标出来）。</summary>
        public static System.Collections.Generic.List<LocalAddr> LocalAddresses()
        {
            var list = new System.Collections.Generic.List<LocalAddr>();
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        var a = ua.Address;
                        if (a == null) continue;
                        bool v6 = a.AddressFamily == AddressFamily.InterNetworkV6;
                        if (!v6 && a.AddressFamily != AddressFamily.InterNetwork) continue;
                        // v6 里跳过 IPv4 映射地址（`::ffff:192.168.x.x`）—— 那其实就是 v4，会重复列一遍
                        if (v6 && a.IsIPv4MappedToIPv6) continue;
                        list.Add(new LocalAddr
                        {
                            addr = a.ToString(),
                            nic = ni.Name,
                            isV6 = v6,
                            isVirtual = LooksVirtual(ni.Name) || LooksVirtual(ni.Description),
                            isLoopback = IPAddress.IsLoopback(a),
                        });
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning("[Net] 枚举网卡失败：" + e.Message); }

            list.Sort((x, y) =>
            {
                int c = x.isLoopback.CompareTo(y.isLoopback);  if (c != 0) return c;
                c = x.isV6.CompareTo(y.isV6);                  if (c != 0) return c;   // IPv4 优先
                c = x.isVirtual.CompareTo(y.isVirtual);        if (c != 0) return c;   // 物理网卡优先
                return string.CompareOrdinal(x.nic, y.nic);
            });
            return list;
        }

        /// <summary>【刷新】默认填的那个 = **第一个能用的**（回环、v6 链路本地都跳过）。</summary>
        public static string LocalIPv4()
        {
            var all = LocalAddresses();
            foreach (var a in all) if (a.Usable && !a.isV6) return a.addr;   // 先 IPv4
            foreach (var a in all) if (a.Usable) return a.addr;              // 只有 IPv6 也认
            return "127.0.0.1";                                              // 真一个都没有
        }

        /// <summary>本机**所有**可用 IPv4（界面上列出来让玩家挑）。</summary>
        public static string[] AllLocalIPv4()
        {
            var r = new System.Collections.Generic.List<string>();
            foreach (var a in LocalAddresses()) if (!a.isV6 && a.Usable) r.Add(a.addr);
            return r.Count > 0 ? r.ToArray() : new[] { "127.0.0.1" };
        }

        /// <summary>本机**所有**可用 IPv6 —— **公网直连那条路靠它**（有公网 v6 就不必端口映射）。</summary>
        public static string[] AllLocalIPv6()
        {
            var r = new System.Collections.Generic.List<string>();
            foreach (var a in LocalAddresses()) if (a.isV6 && a.Usable) r.Add(a.addr);
            return r.ToArray();
        }

        /// <summary>端口合不合法（界面上要能**当场**说清，而不是等他点了才报错）。</summary>
        public static bool PortOk(int port) { return port > 0 && port <= 65535; }

        // ==================================================================
        //  【测外网】—— **外网看到的地址**（和「本机网卡上的地址」是【两件事】）
        // ==================================================================
        //
        // 🔴 **为什么要有它**（2026-09-27 用户问：「我在 IPv6 测试网站上明明看得到 IPv6，
        //    你这里为什么说没有？」）：那两句**说的是两件事，而且两句都对** ——
        //    · **「本机地址」** = 从**网卡**上读（`LocalAddresses`）⇒ 别人**能不能直连过来**看它；
        //    · **「测试网站看到的」** = 从**外网回看**这条连接是从哪个地址出去的。
        //    两者**不一样**的常见原因 = **路由器在做 IPv6 NAT（NAT66）**：它自己有全局 v6、
        //    给内网只发 ULA，出站时把源地址换成自己那条 ⇒ 网站看到的是**路由器的**地址，
        //    而本机**压根没法被外面直接连上**。
        //    📌 实测与完整判据 → `资料/联机P2P_设计与交接.md` §11·4。
        //
        // ⚠️ **探测要联网、要花时间** ⇒ 一律**后台线程 + 短超时**，绝不在主线程上等。
        // ⚠️ **一个站探不到就换下一个**：实测**有站点在国内连不上**（`api.ipify.org` 的 v4、`6.ipw.cn`）
        //    ⇒ **一个都不通时必须如实说「探测不到」**，**不许**把它说成「你没有 IPv6」——
        //    那正是这次用户被绕进去的那个坑。

        /// <summary>回显站（返回体里带着「你从外网看起来是什么地址」）。
        /// ⚠️ 顺序 = 先实测可用的、再备用；**每一个都必须能失败**（见 <see cref="ProbeExternalAsync"/>）。</summary>
        static readonly string[] EchoV4 = { "http://ip.3322.net", "https://myip.ipip.net" };
        static readonly string[] EchoV6 = { "https://api6.ipify.org", "https://v6.ident.me" };

        /// <summary>「外网看到的地址」探测结果。**两个方向各自独立**（可能只有一个探得到）。</summary>
        public struct ExternalAddrs
        {
            public bool ok;        // 至少一个方向探到了
            public string v4;      // 外网看到的 IPv4（没探到 = 空）
            public string v6;      // 外网看到的 IPv6（没探到 = 空）
            public string v4From;  // 各是哪个站报的（**要如实标出处**）
            public string v6From;
            public string detail;  // 一个都没探到时的人话（红线的落点）
        }

        /// <summary>探一次「外网看到的地址」。**立刻返回**；结果在**后台线程**回调
        /// （调用方要碰 Unity 对象的话自己切回主线程）。</summary>
        public static void ProbeExternalAsync(Action<ExternalAddrs> done)
        {
            var th = new System.Threading.Thread(() =>
            {
                var r = new ExternalAddrs();
                try
                {
                    try { System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
                    r.v4 = EchoOnce(EchoV4, AddressFamily.InterNetwork, out r.v4From);
                    r.v6 = EchoOnce(EchoV6, AddressFamily.InterNetworkV6, out r.v6From);
                    r.ok = !string.IsNullOrEmpty(r.v4) || !string.IsNullOrEmpty(r.v6);
                    if (!r.ok)
                        r.detail = "两个方向都没探到 —— **可能是回显站被网络挡了**（不是「你没有公网地址」）。";
                }
                catch (Exception e)
                {
                    r.detail = "探测出错：" + e.Message;
                }
                Debug.Log("[Net] 外网看到的地址：v4=" + (string.IsNullOrEmpty(r.v4) ? "（没探到）" : r.v4 + " ← " + r.v4From)
                        + " · v6=" + (string.IsNullOrEmpty(r.v6) ? "（没探到）" : r.v6 + " ← " + r.v6From)
                        + (string.IsNullOrEmpty(r.detail) ? "" : " · " + r.detail));
                if (done != null) { try { done(r); } catch { } }
            }) { IsBackground = true, Name = "wf-net-echo" };
            th.Start();
        }

        /// <summary>逐个试回显站，返回第一个探到的地址（都失败返回 null，`from` 记是谁报的）。</summary>
        static string EchoOnce(string[] urls, AddressFamily want, out string from)
        {
            from = null;
            for (int i = 0; i < urls.Length; i++)
            {
                try
                {
                    string body = HttpGetQuick(urls[i], 4000);
                    string ip = FirstIpIn(body, want);
                    if (!string.IsNullOrEmpty(ip)) { from = urls[i]; return ip; }
                }
                catch (Exception e) { Debug.Log("[Net] 回显站 " + urls[i] + " 没通：" + e.Message); }
            }
            return null;
        }

        static string HttpGetQuick(string url, int ms)
        {
            var rq = (HttpWebRequest)WebRequest.Create(url);
            rq.Method = "GET";
            rq.Timeout = ms;
            rq.ReadWriteTimeout = ms;
            rq.UserAgent = "WarpforgeReplica/1.0";
            using (var rs = rq.GetResponse())
            using (var s = rs.GetResponseStream())
            using (var rd = new StreamReader(s))
                return rd.ReadToEnd();
        }

        /// <summary>从一段文本里抠出**第一个指定地址族的 IP**。
        /// ⚠️ **回显站返回的不一定是纯 IP**（`myip.ipip.net` 返回的是「当前 IP：117.183.96.20 来自于：中国 …」
        /// 这种中文句子）⇒ 必须**扫 token 再解析**，不能直接 `IPAddress.Parse(body)`。</summary>
        public static string FirstIpIn(string text, AddressFamily want)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var cur = new System.Text.StringBuilder();
            var toks = new System.Collections.Generic.List<string>();
            for (int i = 0; i <= text.Length; i++)
            {
                char c = i < text.Length ? text[i] : ' ';
                bool inSet = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')
                           || c == '.' || c == ':' || c == '[' || c == ']';
                if (inSet) { cur.Append(c); continue; }
                if (cur.Length > 0) { toks.Add(cur.ToString()); cur.Length = 0; }
            }
            for (int i = 0; i < toks.Count; i++)
            {
                string t = toks[i].Trim('[', ']');
                IPAddress a;
                if (IPAddress.TryParse(t, out a) && a.AddressFamily == want) return a.ToString();
            }
            return null;
        }

    }
}
