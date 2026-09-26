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

        // ---- 【刷新】按钮：取本机 IPv4 ----

        /// <summary>本机在局域网里的 IPv4（刷新按钮填的就是它）。
        /// ⚠️ 多网卡（有线+无线+虚拟网卡）时**取第一个非回环的** —— 挑不准是常态，
        /// 所以旁边那个输入框**始终可手改**，并且界面上要提醒「多网卡可换一个」。</summary>
        public static string LocalIPv4()
        {
            try
            {
                var addrs = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (var a in addrs)
                    if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                        return a.ToString();
                foreach (var a in addrs)
                    if (a.AddressFamily == AddressFamily.InterNetwork)
                        return a.ToString();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Net] 取本机 IP 失败：" + e.Message);
            }
            return "127.0.0.1";
        }

        /// <summary>界面上那句如实说明用：本机**所有** IPv4（多网卡时让玩家自己挑）。</summary>
        public static string[] AllLocalIPv4()
        {
            try
            {
                var list = new System.Collections.Generic.List<string>();
                foreach (var a in Dns.GetHostAddresses(Dns.GetHostName()))
                    if (a.AddressFamily == AddressFamily.InterNetwork)
                        list.Add(a.ToString());
                if (list.Count > 0) return list.ToArray();
            }
            catch { }
            return new[] { "127.0.0.1" };
        }

        /// <summary>端口合不合法（界面上要能**当场**说清，而不是等他点了才报错）。</summary>
        public static bool PortOk(int port) { return port > 0 && port <= 65535; }
    }
}
