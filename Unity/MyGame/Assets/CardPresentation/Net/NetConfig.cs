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
            /// · **IPv6 出不了公网的两种**：链路本地 `fe80::/10` 与 **唯一本地 `fc00::/7`（`fc..` / `fd..`）**
            ///   —— 后者**看着很像公网地址**，但公网路由不到。
            /// ✅ 剩下的是 **IPv4 私有段（192.168 / 10.x / 172.16-31）与 IPv6 全局（`2000::/3`）** ——
            ///    前者局域网里能用，后者**公网直连能用**。</summary>
            public bool Usable
            {
                get
                {
                    if (isLoopback) return false;
                    if (!isV6) return !addr.StartsWith("169.254.");
                    string a = addr.ToLowerInvariant();
                    if (a.StartsWith("fe80")) return false;          // 链路本地
                    if (a.StartsWith("fc") || a.StartsWith("fd")) return false;   // 唯一本地 ULA
                    return true;
                }
            }
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
    }
}
