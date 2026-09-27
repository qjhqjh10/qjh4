// NetSelfTest.cs — 联机（P2P）的**传输/握手/心跳/重连**自检。入口 `NetSelfTest.Run`。
//
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod NetSelfTest.Run -logFile -
//
// 为什么能在一个进程里验：**同一个进程开两个 socket 走 127.0.0.1** ——
//   真正要验的就是「帧/握手/心跳/重连」这几件，与「两台机器」无关；
//   而**同一工程不能同时跑两个 Unity 实例**（`CLAUDE.md` 铁律 1·b）⇒ 只能这么验。
//
// ⚠️ 这里**不碰玩家的真设置**（`NetConfig.OverridePath` 指到临时文件），也**不碰真存档**。
// ⚠️ 批处理下没有帧循环 ⇒ 全靠 `PumpUntil` 显式推进（带真实等待，因为心跳/重连是**按真实时间**判的）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;
using CardPresentation;      // `NetRuntime`（通知队列在这儿；它在 CardPresentation 而不是 .Net 下）
using CardPresentation.Net;
using RuleEngine;

public static class NetSelfTest
{
    static int _pass, _fail, _warn;

    public static void Run()
    {
        _pass = _fail = _warn = 0;
        string tmp = Path.Combine(Path.GetTempPath(), "wf_netselftest.json");
        NetConfig.OverridePath = tmp;                 // 🔴 别动玩家的真设置
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }

        try
        {
            TestFraming();
            TestPortBusy();
            TestHandshakeOk(FreePort());
            TestWrongPassword(FreePort());
            TestVersionMismatch(FreePort());
            TestBulkOrder(FreePort());
            TestDropAndReconnect(FreePort());
            TestPasswordFrozen(FreePort());
            TestSeatAndWire();
            TestNotices();
            TestHostResolve();
            TestAddressAndUpnp();     // 🆕 2026-09-27：地址判据（Teredo/6to4）+ UPnP 纯函数
        }
        catch (Exception e)
        {
            _fail++;
            Debug.LogError("[NetSelfTest] ✗ 自检本身炸了：" + e);
        }
        finally
        {
            NetConfig.OverridePath = null;
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Debug.Log($"[NetSelfTest] ===== 通过 {_pass} · 失败 {_fail} · 跳过/警告 {_warn} =====");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ==================================================================
    //  A. 组帧 / 拆帧
    // ==================================================================
    static void TestFraming()
    {
        var bytes = NetProtocol.Frame(NetKind.Ping, NetProtocol.Pack(new MsgPing { t = 12345 }));
        Eq(NetProtocol.FrameLength(bytes, 0), bytes.Length - NetProtocol.HeaderBytes,
           "A① 帧头里的长度 = 帧体长度");

        // 按拆帧的两步走：先读头拿长度，再按长度取 body
        var body = new byte[bytes.Length - NetProtocol.HeaderBytes];
        Buffer.BlockCopy(bytes, NetProtocol.HeaderBytes, body, 0, body.Length);
        var env = NetProtocol.Parse(body, body.Length);
        Ok(env != null && env.kind == NetKind.Ping, "A② 信封的 kind 还原得回来");
        var ping = NetProtocol.Unpack<MsgPing>(env.payload);
        Ok(ping != null && ping.t == 12345, "A③ 负载字段还原得回来（t=12345）");

        // 空负载也要能走（`bye` 这种没参数的）
        var f2 = NetProtocol.Frame(NetKind.Bye, "");
        var b2 = new byte[f2.Length - NetProtocol.HeaderBytes];
        Buffer.BlockCopy(f2, NetProtocol.HeaderBytes, b2, 0, b2.Length);
        Ok(NetProtocol.Parse(b2, b2.Length) != null, "A④ 空负载的帧也解得出（不是 null）");
    }

    // ==================================================================
    //  B. 端口被占用时**要说人话**（红线：不许静默）
    // ==================================================================
    static void TestPortBusy()
    {
        int p = FreePort();
        var a = NetSession.NewTcp();
        var b = NetSession.NewTcp();
        Ok(a.StartHost(Cfg(p, "x")), "B① 第一台主机起得来");
        bool second = b.StartHost(Cfg(p, "x"));
        Ok(!second, "B② 同一端口起第二台主机**起不来**");
        Ok(b.LastError != null && b.LastError.Contains("占用"),
           $"B③ 起不来时给的是**人话**（「{b.LastError}」）");
        a.Close(false); b.Close(false);
    }

    // ==================================================================
    //  C. 正常握手（无密码）
    // ==================================================================
    static void TestHandshakeOk(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        int hostReady = 0, cliReady = 0;
        host.OnPeerReady = () => hostReady++;
        cli.OnPeerReady = () => cliReady++;

        Ok(host.StartHost(Cfg(port, "")), "C① 主机起来了");
        cli.CheckConnection(Cfg(port, ""));
        bool ok = PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000);
        Ok(ok, $"C② 握手走完两边都到 Lobby（主机 {host.State} / 客机 {cli.State}）");
        Eq(hostReady, 1, "C③ 主机侧 `OnPeerReady` 只叫一次");
        Eq(cliReady, 1, "C④ 客机侧 `OnPeerReady` 只叫一次");
        Ok(!string.IsNullOrEmpty(cli.SessionToken), "C⑤ 客机拿到了对局钥匙（重连要用）");
        Ok(cli.SessionToken == host.SessionToken, "C⑥ 两边的钥匙是同一把");
        Ok(host.PeerName == Environment.MachineName,
           $"C⑦ 主机的 `PeerName` = 对面的机器名（实际「{host.PeerName}」；⚠️ 我们没有玩家名那套数据源，用机器名是我们挑的）");
        host.Close(false); cli.Close(false);
    }

    // ==================================================================
    //  D. 密码错 ⇒ 明确拒绝
    // ==================================================================
    static void TestWrongPassword(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        bool? checkOk = null; string checkWhy = null;
        cli.OnCheckDone = (ok, why) => { checkOk = ok; checkWhy = why; };

        Ok(host.StartHost(Cfg(port, "正确的密码")), "D① 主机起来了（设了密码）");
        cli.CheckConnection(Cfg(port, "错的密码"));
        bool done = PumpUntil(host, cli, () => cli.State == NetState.Closed, 5000);
        Ok(done, "D② 密码不对时客机会被断开（不会停在 Lobby 假装连上了）");
        Ok(checkWhy != null && checkWhy.Contains("密码"),
           $"D③ 拒绝理由里有「密码」二字（「{checkWhy}」）");
        Ok(checkOk == false, "D④ 【检查连接】如实回报「没连上」");
        host.Close(false); cli.Close(false);
    }

    // ==================================================================
    //  E. 协议版本不符 ⇒ 明确拒绝（两端不是同一份构建时**不许静默兼容**）
    // ==================================================================
    static void TestVersionMismatch(int port)
    {
        var host = NetSession.NewTcp();
        Ok(host.StartHost(Cfg(port, "")), "E① 主机起来了");

        // 手搓一个协议版本 +1 的 proof 帧直接怼进去（模拟「对面是另一份构建」）
        var raw = new TcpTransport();
        Ok(raw.Connect("127.0.0.1", port, 3000), "E② 裸传输连得上（这一条在测的是协议而不是会话）");
        PumpOnce(host, 60);                                   // 让主机把 challenge 发出来
        raw.Send(NetKind.Proof, NetProtocol.Pack(new MsgProof
        { protoVer = NetProtocol.Version + 1, gameVer = "别的构建", name = "冒充的", proof = "" }));

        bool rejected = PumpUntil(host, null, () => host.State == NetState.Closed, 4000);
        Ok(rejected, "E③ 版本不符 ⇒ 主机把连接**关掉**");
        Ok(host.LastError != null && host.LastError.Contains("版本"),
           $"E④ 理由里点明是版本问题（「{host.LastError}」）");
        raw.Close(); host.Close(false);
    }

    // ==================================================================
    //  F. 1000 条消息：不丢、不乱序
    // ==================================================================
    static void TestBulkOrder(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        Ok(host.StartHost(Cfg(port, "")), "F① 主机起来了");
        cli.CheckConnection(Cfg(port, ""));
        Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
           "F② 先握手");

        const int N = 1000;
        for (int i = 0; i < N; i++) host.Send("test.bulk", NetProtocol.Pack(new MsgPing { t = i }));
        bool got = PumpUntil(host, cli, () => cli.Inbox.Count >= N, 8000);
        Ok(got, $"F③ {N} 条全收到了（收到 {cli.Inbox.Count} 条）");
        bool ordered = true;
        for (int i = 0; i < Math.Min(N, cli.Inbox.Count); i++)
            if (NetProtocol.Unpack<MsgPing>(cli.Inbox[i].payload).t != i) { ordered = false; break; }
        Ok(ordered, "F④ 顺序与发送顺序**一字不差**（TCP 有序 + 我们只做了一次中转）");
        cli.Inbox.Clear();
        host.Close(false); cli.Close(false);
    }

    // ==================================================================
    //  G/H. 掉线 → 等重连 → 补动作流（正本 §5·6）
    // ==================================================================
    static void TestDropAndReconnect(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        int hostLost = 0, cliLost = 0, resumed = 0;
        List<MsgAction> gotActions = null;
        MsgStart gotStart = null;
        host.OnPeerLost = () => hostLost++;
        cli.OnPeerLost = () => cliLost++;

        Ok(host.StartHost(Cfg(port, "")), "G① 主机起来了");
        cli.CheckConnection(Cfg(port, ""));
        Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
           "G② 先握手");

        // 进对局（**掉线只有在对局中才走「等重连」**）
        host.EnterBattle(); cli.EnterBattle(0);
        var start = new MsgStart { seed = 20260926, mode = "Classic", arena = "Battle_arena1", hostDeckJson = "{}", clientDeckJson = "{}" };
        var acts = new List<MsgAction>
        {
            new MsgAction { seq = 0, kind = 0, handIdx = 0, slot = 0 },              // 出牌
            new MsgAction { seq = 1, kind = 1, slot = 2, targetP = 1, targetSlot = 1 }, // 近战打对面
            new MsgAction { seq = 2, kind = 4 },                                     // 结束回合
        };
        host.ResumeProvider = () => (start, acts);            // 主机手上的**权威动作流**
        cli.OnResumed = (s, a) => { resumed++; gotStart = s; gotActions = a; };

        // ---- 模拟掉线（客机这一头把连接掐掉）----
        cli.Transport.ClosePeer();
        Ok(PumpUntil(host, cli, () => host.State == NetState.WaitingReconnect, 4000),
           $"G③ 主机发现掉线并进入等待重连（实际 {host.State}）");
        Eq(hostLost, 1, "G④ 主机侧 `OnPeerLost` 叫了一次");
        Ok(host.State != NetState.Closed, "G⑤ 🔴 **没有判负** —— 停在等待重连（用户 2026-09-26 要的就是这个）");

        // ---- 客机自己也会发现，然后按退避重连（间隔 2 秒）----
        Ok(PumpUntil(host, cli, () => resumed > 0, 15000),
           $"H① 客机重连成功、并收到了主机补的进度（实际 {cli.State}）");
        Eq(resumed, 1, "H② `OnResumed` 叫了一次");
        Ok(gotStart != null && gotStart.seed == 20260926, "H③ 补下来的是**同一局**（种子一致）");
        Eq(gotActions == null ? -1 : gotActions.Count, 3, "H④ 权威动作流 3 条一条不少");
        Ok(gotActions != null && gotActions[1].targetP == 1 && gotActions[1].targetSlot == 1,
           "H⑤ 动作里的目标格位原样带回来了");
        Ok(cli.State == NetState.InBattle && host.State == NetState.InBattle,
           $"H⑥ 两边都回到对局中（主机 {host.State} / 客机 {cli.State}）");
        Ok(cliLost >= 1, "H⑦ 客机侧也报了掉线（它自己发现读不到了）");

        // 🔴 **H⑧ 是给一个真 bug 立的桩**（2026-09-26 实测踩到）：
        //    主机原来靠「状态 == 等重连 && 连着」去发握手包 —— 那是**电平**，
        //    于是他在等客机报进度的那几帧里**反复重发 challenge**，两边来回打转。
        //    现在改成边沿触发（看 `AcceptedCount` 变没变）；这条就盯「追平之后别再抖」。
        PumpBoth(host, cli, 2500);
        Ok(host.State == NetState.InBattle && cli.State == NetState.InBattle,
           $"H⑧ 追平之后**稳住**，不会来回抖（主机 {host.State} / 客机 {cli.State}）");
        host.Close(false); cli.Close(false);
    }

    // ==================================================================
    //  J. 主机的密码在**开台那一刻定格**（不许核对时去读全局设置）
    // ==================================================================
    static void TestPasswordFrozen(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        Ok(host.StartHost(Cfg(port, "开台时的密码")), "J① 主机带着「开台时的密码」起来了");

        // 开台之后**改掉全局设置**（模拟「同进程里又开了别的桌」/「玩家中途改了设置」）
        NetConfig.Current.password = "后来改的密码";
        cli.CheckConnection(Cfg(port, "开台时的密码"));

        bool ok = PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000);
        Ok(ok, "J② 客机拿**开台时**那个密码 ⇒ 照样连得上（主机核对的是定格值，不是运行时的全局设置）");
        host.Close(false); cli.Close(false);
    }

    // ==================================================================
    //  I. 座位翻译 / 动作线上往返
    // ==================================================================
    static void TestSeatAndWire()
    {
        // ⚠️ `Seat()` 是个**备用帮手，不在任何运行路径上** —— 它实现的正是**已作废的「镜像端点」**翻译。
        //    保留它是有意的（`项目任务.md` §三 第 14 条 表 第 9 条）；I①~I③ 只钉它**自己的**语义。
        Eq(NetProtocol.Seat(0), 1, "I① `Seat()` 自己：0 → 1（⚠️ **备用帮手，已退出运行路径**）");
        Eq(NetProtocol.Seat(1), 0, "I② `Seat()` 自己：1 → 0");
        Eq(NetProtocol.Seat(-1), -1, "I③ 没有目标时原样返回（别翻成 2）");

        // 🔴 **2026-09-26 改了 I⑤/I⑦/I⑧**：原来验的是「线上写**发送方视角**、接收方翻一下」——
        //    那是**镜像**那套，**已作废**（正本 §5·3：镜像在引擎层面不成立）。
        //    现在两端跑**绝对座位** ⇒ **原样过去、不翻**。
        //    判据 = **真实落地路径** `NetApply.Apply`：`int targetP = m.targetP;`（座位绝对 ⇒ 什么都不翻）
        //    —— ⚠️ `FromWire` 只是自检用的第二条路，**它必须和那条同语义**（原来它俩是反的）。
        var a = new AiAction { Kind = AiActionKind.AttackMelee, Slot = 3, TargetP = 1, TargetSlot = 4, Ranged = false };
        var w = NetProtocol.ToWire(a, null, 7);
        Eq(w.seq, 7, "I④ 序号带上了");
        Eq(w.targetP, 1, "I⑤ 线上写的就是**绝对座位**（原样 1，本层不翻）");
        var back = NetProtocol.FromWire(w);
        Ok(back.Kind == AiActionKind.AttackMelee && back.Slot == 3, "I⑥ 动作种类与出战格位原样过去");
        Eq(back.TargetP, 1, "I⑦ `FromWire` **不翻座位** —— 与 `NetApply.Apply` 同语义（**原来这里会翻成 0**）");
        var a2 = new AiAction { Kind = AiActionKind.AttackMelee, Slot = 0, TargetP = 0, TargetSlot = 2 };
        var back2 = NetProtocol.FromWire(NetProtocol.ToWire(a2, null, 0));
        Eq(back2.TargetP, 0, "I⑧ 座位 0 过来还是 0（**原来会翻成 1**）");

        Ok(NetProtocol.Fingerprint(null) == 0, "I⑨ 空上下文指纹是 0（不炸）");
    }

    // ==================================================================
    //  红线：联机层出的事要**告诉玩家**（`项目任务.md` §三 第 14 条 表 里的 4/5/6 三条）
    // ==================================================================
    /// <summary>联机层够不到窗口系统 ⇒ 通知排进 `NetRuntime` 的队里，由它的 `Update` 弹出来。
    /// 批处理没有帧循环 ⇒ 这里用 `DrainNoticesForTest()` 取出来验（**这样这三条才有断言盯着**，
    /// 不然又变成「只有日志、验不了」）。</summary>
    static void TestNotices()
    {
        var cfg = NetConfig.Current;
        int keepRole = cfg.role;
        try
        {
            // ① **没配过联机 ⇒ 不打扰**（「没连上照旧打 bot」是**设计好的**行为，不是错误）
            cfg.role = (int)NetRole.Off;
            NetRuntime.DrainNoticesForTest();
            NetMatchmaking.ExplainNotTakingOver("联机没连上");
            Eq(NetRuntime.DrainNoticesForTest().Length, 0,
               "J① **没配过联机**时，「联机没接管」**不弹窗** —— 单机玩家不该被打扰");

            // ② **配过联机、却没连上 ⇒ 必须说一声**（原来只有日志 ⇒ 玩家会以为对面是真人）
            cfg.role = (int)NetRole.Host;
            NetRuntime.DrainNoticesForTest();
            NetMatchmaking.ExplainNotTakingOver("联机没连上");
            var got = NetRuntime.DrainNoticesForTest();
            Eq(got.Length, 1,
               "J② ★ **配过联机**时，「联机没接管」**弹一条**（红线：不许静默）");
            Ok(got.Length == 1 && got[0].Contains("打的是电脑"),
               "J③ ★ 那条话要说清「**打的是电脑**」+ 告诉玩家怎么办（不是只报个错）");

            // ③ 客机那一支也要会说（`role` 决定提示他去点【检查连接】还是【保存】）
            cfg.role = (int)NetRole.Client;
            NetRuntime.DrainNoticesForTest();
            NetMatchmaking.ExplainNotTakingOver("还没握手完");
            var got2 = NetRuntime.DrainNoticesForTest();
            Ok(got2.Length == 1 && got2[0].Contains("检查连接"),
               "J④ ★ 客机拿到的提示是让他去点【**检查连接**】（主机才是【保存】）");

            // ④ 只取一次：排进去的话**取走即清空**（免得同一条反复弹）
            Eq(NetRuntime.DrainNoticesForTest().Length, 0, "J⑤ 取走即清空（同一条不会反复弹）");
        }
        finally
        {
            cfg.role = keepRole;                    // 别把设置改脏
            NetRuntime.DrainNoticesForTest();
        }
    }

    // ==================================================================
    //  地址解析（🔴 IPv6 那条路靠它）
    // ==================================================================
    /// <summary>🔴 **2026-09-26 加：地址解析必须认 IPv6 字面量**。
    /// 为什么单开一条：用户定了走「**公网 IPv6 直连**」（`资料/联机P2P_设计与交接.md` §十一），
    /// 而原来 `Connect` 用的是 **`new TcpClient()`** —— 在 Unity(Mono) 里那是 **IPv4 socket**
    /// ⇒ **拿 IPv6 地址去连必定失败**、整条路是死的。现在改成先解析、再按地址族建客户端，
    /// 这里钉住**解析**那一步（连不连得上是真 Play 的事）。</summary>
    static void TestHostResolve()
    {
        var v4 = TcpTransport.ResolveHost("192.168.1.10");
        Ok(v4 != null && v4.AddressFamily == AddressFamily.InterNetwork,
           "K① `192.168.1.10` 解析成 IPv4");

        var v6 = TcpTransport.ResolveHost("2001:db8::1");
        Ok(v6 != null && v6.AddressFamily == AddressFamily.InterNetworkV6,
           "K② ★ **IPv6 字面量解析成 IPv6** —— 原来 socket 是 IPv4 的 ⇒ 这条路**必定连不上**");

        var br = TcpTransport.ResolveHost("[::1]");
        Ok(br != null && br.AddressFamily == AddressFamily.InterNetworkV6,
           "K③ 方括号写法 `[::1]` 也认（玩家从别处复制地址常带方括号）");

        Ok(TcpTransport.ResolveHost("") == null, "K④ 空串 ⇒ null（**不许悄悄连到本机**）");
        Ok(TcpTransport.ResolveHost("这不是地址") == null, "K⑤ 解析不出来 ⇒ null（**不静默**）");
    }

    // ==================================================================
    //  夹具 / 工具
    // ==================================================================

    static NetConfigData Cfg(int port, string pwd, string ip = "127.0.0.1")
    {
        var c = NetConfig.Current;               // 用当前这份（host 那侧不看 ip）
        c.port = port;
        c.password = pwd;
        c.ip = ip;
        return c;
    }

    /// <summary>两边一起推，直到条件成立或超时。返回条件是否成立。</summary>
    static bool PumpUntil(NetSession a, NetSession b, Func<bool> cond, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (a != null) a.Pump();
            if (b != null) b.Pump();
            if (cond()) return true;
            Thread.Sleep(5);
        }
        if (a != null) a.Pump();
        if (b != null) b.Pump();
        return cond();
    }

    // ==================================================================
    //  K. 地址判据 + UPnP（2026-09-27 加：用户问「测试网站看得到 IPv6，你这里为什么看不到」那一轮）
    //     ⚠️ **纯函数全在这儿验**；真发 SSDP / 真去改路由器那一半**批处理里不跑**
    //        （`UpnpPortMapper.MapAsync` 里 `Application.isBatchMode` 直接返回 —— 理由见那儿）。
    // ==================================================================

    /// <summary>原版路由器那份设备描述的**真实形状**（照着本机 TP-LINK WTA301 的 `igd.xml` 抄的：
    /// 只有 WAN 连接设备那一层才有 `WANIPConnection`，而且 `controlURL` 是**相对路径** `/ipc`）。</summary>
    const string IgdXml =
        "<root><device><deviceType>urn:schemas-upnp-org:device:InternetGatewayDevice:1</deviceType>"
      + "<serviceList><service><serviceType>urn:schemas-upnp-org:service:Layer3Forwarding:1</serviceType>"
      + "<serviceId>urn:upnp-org:serviceId:L3Forwarding1</serviceId><controlURL>/l3f</controlURL></service></serviceList>"
      + "<deviceList><device><deviceType>urn:schemas-upnp-org:device:WANDevice:1</deviceType>"
      + "<serviceList><service><serviceType>urn:schemas-upnp-org:service:WANCommonInterfaceConfig:1</serviceType>"
      + "<controlURL>/ifc</controlURL></service></serviceList>"
      + "<deviceList><device><deviceType>urn:schemas-upnp-org:device:WANConnectionDevice:1</deviceType>"
      + "<serviceList><service><serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>"
      + "<serviceId>urn:upnp-org:serviceId:WANIPConnection</serviceId><controlURL>/ipc</controlURL></service></serviceList>"
      + "</device></deviceList></device></deviceList></device></root>";

    static void TestAddressAndUpnp()
    {
        // ---- ① Teredo / 6to4 **不能再被当成「公网 IPv6」**（2026-09-27 修的那条真缺陷）----
        Ok(!V6("2001:0:1234:5678::1"), "Teredo `2001:0::/32` 不算可用 IPv6（**这条原来会误报**）");
        Ok(!V6("2001::1"), "Teredo 的压缩写法 `2001::1` 同样不算");
        Ok(!V6("2002::1"), "6to4 `2002::/16` 不算可用 IPv6");
        Ok(!V6("fd00:485f:860:13a4::1"), "ULA `fd00::/8` 不算（本机实测那个 ULA 前缀）");
        Ok(!V6("fe80::825d:5b94:47a3:f81"), "链路本地不算");
        Ok(!V6("2001:db8::1"), "文档用段 `2001:db8::/32` 不算");
        Ok(V6("2409:8a5c:1e47:11a0::1"), "真全局单播 `2409:…` **算**（实测见过的电信段）");
        Ok(V6("2408:845d:1f30:89e9::1"), "真全局单播 `2408:…` **算**");
        // IPv4 那半边照旧
        Ok(new NetConfig.LocalAddr { addr = "192.168.2.104" }.Usable, "192.168 可用（局域网那条路）");
        Ok(!new NetConfig.LocalAddr { addr = "169.254.1.1" }.Usable, "169.254（APIPA）不算");
        Ok(!new NetConfig.LocalAddr { addr = "::1", isV6 = true, isLoopback = true }.Usable, "回环不算");

        // ---- ② 外网回显站的返回体是**句子**，不是纯 IP（`myip.ipip.net` 就是中文句子）----
        Eq(NetConfig.FirstIpIn("当前 IP：117.183.96.20  来自于：中国 广西 柳州", AddressFamily.InterNetwork),
           "117.183.96.20", "从中文句子里抠出 IPv4");
        Eq(NetConfig.FirstIpIn("2409:8a5c:1e47:11a0:4a5f:8ff:fe60:13a4\n", AddressFamily.InterNetworkV6),
           "2409:8a5c:1e47:11a0:4a5f:8ff:fe60:13a4", "从纯文本里抠出 IPv6");
        Ok(NetConfig.FirstIpIn("当前 IP：117.183.96.20", AddressFamily.InterNetworkV6) == null,
           "要的是 v6 时**不许**把句子里的 v4 当答案");

        // ---- ③ UPnP：设备描述里找端口映射服务（相对 controlURL 也要能找出来）----
        string svc;
        Eq(UpnpPortMapper.FindControlUrl(IgdXml, out svc), "/ipc", "从设备描述里找出 `controlURL`");
        Ok(svc != null && svc.Contains("WANIPConnection"), "找出来的服务是 `WANIPConnection:1`（实际 " + svc + "）");
        Ok(UpnpPortMapper.FindControlUrl(
               "<root><service><serviceType>urn:schemas-upnp-org:service:Layer3Forwarding:1</serviceType>"
             + "<controlURL>/l3f</controlURL></service></root>", out svc) == null,
           "**只有** Layer3Forwarding 的设备 ⇒ 找不到端口映射服务（要如实报「路由器不支持」）");
        Ok(UpnpPortMapper.FindControlUrl(
               "<service><serviceType>urn:schemas-upnp-org:service:WANPPPConnection:1</serviceType>"
             + "<controlURL>/ppp</controlURL></service>", out svc) == "/ppp",
           "PPPoE 型 WAN（`WANPPPConnection`）也认");
        Ok(UpnpPortMapper.FindControlUrl(
               "<service><serviceType>urn:schemas-upnp-org:service:WANPPPConnection:1</serviceType>"
             + "<controlURL>/ppp</controlURL></service>"
             + "<service><serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>"
             + "<controlURL>/ipc</controlURL></service>", out svc) == "/ipc",
           "两种都在时**优先 IP 型**（不看它们在文档里谁先出现）");

        // ---- ④ 相对 controlURL 必须变成绝对地址（不然 `WebRequest` 发不出去）----
        Eq(UpnpPortMapper.Absolutize("http://192.168.2.1:1900/igd.xml", "/ipc"),
           "http://192.168.2.1:1900/ipc", "`/ipc` → 绝对地址");
        Eq(UpnpPortMapper.Absolutize("http://192.168.2.1:1900/igd.xml", "ipc"),
           "http://192.168.2.1:1900/ipc", "没有前导斜杠的相对路径也对");
        Eq(UpnpPortMapper.Absolutize("http://r/igd.xml", "http://other/ctrl"), "http://other/ctrl",
           "本来就是绝对地址 ⇒ 原样返回");

        // ---- ⑤ 取 tag（CDATA 外壳要剥掉）----
        Eq(UpnpPortMapper.ParseTag("<NewExternalIPAddress>1.2.3.4</NewExternalIPAddress>", "NewExternalIPAddress"),
           "1.2.3.4", "取标签值");
        Eq(UpnpPortMapper.ParseTag("<a><![CDATA[9.9.9.9]]></a>", "a"), "9.9.9.9", "CDATA 外壳剥掉");
        Eq(UpnpPortMapper.ParseTag("<a>  </a>", "a"), "", "空值给空串（不抛）");

        // ---- ⑥ CGNAT / 私网判定（**这条决定要不要如实告诉玩家「映射了也没用」**）----
        Ok(UpnpPortMapper.IsPublicIpv4("117.183.96.20"), "实测那台的外网 IPv4 是公网");
        Ok(!UpnpPortMapper.IsPublicIpv4("100.64.1.1"), "`100.64/10` = **CGNAT**，不算公网");
        Ok(!UpnpPortMapper.IsPublicIpv4("10.1.1.1"), "10/8 不算");
        Ok(!UpnpPortMapper.IsPublicIpv4("172.16.0.1"), "172.16/12 不算");
        Ok(UpnpPortMapper.IsPublicIpv4("172.32.0.1"), "172.32 **在 /12 之外** ⇒ 算公网（别把整段 172 都判死）");
        Ok(!UpnpPortMapper.IsPublicIpv4("192.168.2.1"), "192.168/16 不算");
        Ok(!UpnpPortMapper.IsPublicIpv4("224.0.0.1"), "组播不算");
        Ok(!UpnpPortMapper.IsPublicIpv4(""), "空串不算");

        // ---- ⑦ SOAP 信封（`SOAPACTION` 那个头用的就是这个 serviceType + action）----
        string soap = UpnpPortMapper.BuildSoap("urn:schemas-upnp-org:service:WANIPConnection:1",
                                               "AddPortMapping", "<NewExternalPort>47777</NewExternalPort>");
        Ok(soap.Contains("<u:AddPortMapping") && soap.Contains("WANIPConnection:1")
           && soap.Contains("<NewExternalPort>47777</NewExternalPort>"),
           "SOAP 信封里有 action / serviceType / 参数");
    }

    static bool V6(string a) { return NetConfig.V6Routable(a); }

    static void PumpOnce(NetSession s, int ms) { s.Pump(); Thread.Sleep(ms); s.Pump(); }

    /// <summary>两边一起推一段固定时间（用来验「稳住了、别再抖」）。</summary>
    static void PumpBoth(NetSession a, NetSession b, int ms)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms) { a.Pump(); b.Pump(); Thread.Sleep(5); }
        a.Pump(); b.Pump();
    }

    /// <summary>随便找一个当时空着的端口（自检不能写死端口 —— 机器上可能正好占着）。</summary>
    static int FreePort()
    {
        var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        l.Start();
        int p = ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }

    static void Ok(bool cond, string msg)
    {
        if (cond) { _pass++; return; }
        _fail++;
        Debug.LogError("[NetSelfTest] ✗ " + msg);
    }
    static void Eq(int got, int want, string msg) { Ok(got == want, $"{msg}（实际 {got}，应为 {want}）"); }
    /// <summary>字符串版（2026-09-27 加：地址/URL 那批断言要比字符串）。</summary>
    static void Eq(string got, string want, string msg)
    {
        bool ok = string.Equals(got ?? "", want ?? "", StringComparison.Ordinal);
        Ok(ok, $"{msg}（实际 `{got ?? "<null>"}`，应为 `{want ?? "<null>"}`）");
    }
}
