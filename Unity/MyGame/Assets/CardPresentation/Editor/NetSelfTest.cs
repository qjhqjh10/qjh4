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
using System.Threading;
using UnityEditor;
using UnityEngine;
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
        Eq(NetProtocol.Seat(0), 1, "I① 发送方视角的 0（他自己）= 我这边的 1（对面）");
        Eq(NetProtocol.Seat(1), 0, "I② 发送方视角的 1（我）= 我这边的 0（自己）");
        Eq(NetProtocol.Seat(-1), -1, "I③ 没有目标时原样返回（别翻成 2）");

        var a = new AiAction { Kind = AiActionKind.AttackMelee, Slot = 3, TargetP = 1, TargetSlot = 4, Ranged = false };
        var w = NetProtocol.ToWire(a, null, 7);
        Eq(w.seq, 7, "I④ 序号带上了");
        Eq(w.targetP, 1, "I⑤ 线上写的是**发送方视角**的座位（原样 1，本层不翻）");
        var back = NetProtocol.FromWire(w);
        Ok(back.Kind == AiActionKind.AttackMelee && back.Slot == 3, "I⑥ 动作种类与出战格位原样过去");
        // 语义验一遍（这才是这一段真正要防的）：发送方说「打我的对面」（他自己的 1）
        //   ⇒ 那个「对面」就是**我** ⇒ 到我这边该翻成 0（我自己）。
        Eq(back.TargetP, 0, "I⑦ 发送方打「他的对面」（写 1）⇒ 翻成 0（= 我）—— **只翻一次**，没翻或翻两次都会露");
        var a2 = new AiAction { Kind = AiActionKind.AttackMelee, Slot = 0, TargetP = 0, TargetSlot = 2 };
        var back2 = NetProtocol.FromWire(NetProtocol.ToWire(a2, null, 0));
        Eq(back2.TargetP, 1, "I⑧ 发送方打「他自己那边」（写 0）⇒ 翻成 1（= 对面自己）");

        Ok(NetProtocol.Fingerprint(null) == 0, "I⑧ 空上下文指纹是 0（不炸）");
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
}
