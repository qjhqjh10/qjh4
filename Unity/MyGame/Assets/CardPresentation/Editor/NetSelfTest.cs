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

    /// <summary>🆕 2026-10-19（P6 · 双语）：取词条**从开头到第一个 `{0}` 之前**那一段 —— 本文件断言的锚。
    /// 形状与 `Editor/SettingsScene.cs:240` 的 `TermHead` 逐字相同。
    /// <para>🔴 为什么要有它：P6 把 `Net/` 那 6 份里**玩家可见**的中文直传换成了 `Loc.T(键)` ⇒
    /// 旧那些**写死中文子串**的断言在**英文档**下必红（本工程那条系统性毛病：只在一种语言下看着对）。</para>
    /// <para>⛔ 别拿它当第二条取值路 —— 它只服务断言；`{0}` 起的运行期参数不参与比较。</para></summary>
    static string TermHead(string key)
    {
        string v = Loc.T(key);
        if (string.IsNullOrEmpty(v)) return "";
        int i = v.IndexOf("{0}", System.StringComparison.Ordinal);
        return i < 0 ? v : v.Substring(0, i);
    }

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
            TestMatchCancel(FreePort());   // 🆕 2026-10-03：取消这一局的匹配（A1）· 🆕 2026-10-10 加了 A985⑫ 那条理由码
            TestReplayResultCodeA985();    // 🆕 2026-10-10（A985⑬）：录像头尾两条只落盘的 `[BattleResult]` 记录
            TestResultCodeBlackBoxA1296(); // 🆕 2026-10-11（A1296）：两张表（`ctx.Events` vs `ActionLog`）与黑匣子
            TestLobbyPeerGone(FreePort()); // 🆕 2026-10-17：大厅阶段对面掉线 / 离开（A902）
            TestHintLine(FreePort());      // 🆕 2026-10-17（B27）：提示行那行字的消费方（A925）
            TestStatePublishA943(FreePort()); // 🆕 2026-10-18（A943）：连接位与计数**由同一次写发布**
            TestSilentTimeoutReadSideA943();  // 🆕 2026-10-18（A943 读侧）：静默超时**不许分两次读下结论**
            TestPeerTextClampA961(FreePort()); // 🆕 2026-10-18（A961 + 「Send() 静默丢」）：对端可控文本要钳 · 丢包要出声
            TestFailVoiceA1267A1268(FreePort()); // 🆕 2026-10-19（A1267/A1268）：掉线原因要进日志 · 旧读线程迟到不许改状态
            TestPeerLostGateOffA1269(FreePort()); // 🆕 2026-10-19（A1269）：掉线闸必须挡 `Off`（收工后不许再报一次）
            TestOpponentHintA932();            // 🆕 2026-10-18（A932）：排位那扇全屏窗的**提示行**（自建节点）
            TestNetTermBilingual();            // 🆕 2026-10-19（P6 · 双语）：`Net/` 那批词条的两语档 + 灭自证
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
    //  🆕 2026-10-19（P6 · 双语）：`Net/` 整片接进语言表之后的**【两语档 + 灭自证】**
    // ==================================================================
    /// <summary>判据 = `资料/普查产出_第四会话/施工单_双语_Net整片_P6.md` §⑤
    /// + `施工单_双语③逐处换key.md` §⑧（**三条必备 + 灭自证 C1/C2**）。
    ///
    /// <para>🔴 **为什么要单开一节**：本轮把 `Net/` 那 6 份里**玩家可见**的中文直传换成了 `Loc.T(键)` ——
    /// 只断「当前档 == `Loc.T(键)`」**不够**：实现若被改回写死、期望值也一起改回写死 ⇒ **两处一起变绿**
    /// （本工程那条系统性毛病）。所以这里断三件：① 本批每条键**都在表里**（坑表 #18）且**值不是键名**（C2）
    /// ② 英文列**一个汉字都没有**（C1）③ 两语档的取值**真的不同且都非空**（灭自证主判据）。</para>
    ///
    /// <para>⚠️ 只切**内存里**的语言（`Loc.PersistOverride` 挡住写盘 —— 本工程规矩：自检不许动玩家的真设置），
    /// 收尾**逐值放回**（写法同 `Editor/SettingsScene.cs:1544` 那一族）。</para>
    ///
    /// <para>⚠️ 这是**静态**判据（表本身 + 两档取值）；「界面上那行字真跟着换」那一半由
    /// `ShellScene.Run` / `MainMenuScene.Run` / `SettingsScene.Run` 那几个宿主覆盖（不在本文件）。</para></summary>
    static void TestNetTermBilingual()
    {
        // 本批（P6a/b/c）用到的**全部**词条 —— 逐条列在这儿；少一条就等于少一格的判据。
        string[] keys =
        {
            // P6a `NetSession.cs` + `NetRuntime.cs`
            "Settings/Online/St/Off", "Settings/Online/St/HostFailed", "Settings/Online/St/Listening",
            "Settings/Online/St/Connecting", "Settings/Online/St/Handshaking", "Settings/Online/St/PeerLostInBattle",
            "Settings/Online/St/Disconnected", "Settings/Online/St/PeerBack", "Settings/Online/St/PeerJoined",
            "Settings/Online/St/SilentTimeout", "Settings/Online/St/Reconnecting", "Settings/Online/St/CaughtUp",
            "Settings/Online/St/ReconnectFailed", "Settings/Online/St/BadHello", "Settings/Online/St/VersionMismatch",
            "Settings/Online/St/WrongPassword", "Settings/Online/St/Refused", "Settings/Online/St/PeerBackWaitReport",
            "Settings/Online/St/PeerInLobby", "Settings/Online/St/PeerRefused", "Settings/Online/St/PeerLeft",
            "Settings/Online/St/ResumedWaitProgress", "Settings/Online/St/ClientLobby", "Settings/Online/St/ResumeSent",
            "Settings/Online/St/ResumeCaughtUp", "Settings/Online/St/RejectBadKey", "Settings/Online/St/RejectNoLog",
            "Settings/Online/St/InBattle", "Settings/Online/St/HostSide", "Settings/Online/St/ClientSide",
            "MainMenu/General/OK",
            // P6c `NetTransport.cs` / `NetConfig.cs` / `UpnpPortMapper.cs`
            "Settings/Online/St/PortBusy", "Settings/Online/St/ConnRefused", "Settings/Online/St/HostNotFound",
            "Settings/Online/St/SocketError", "Settings/Online/St/NoIp", "Settings/Online/St/ConnectTimeout",
            "Settings/Online/St/NoStream", "Settings/Online/St/ReadAbort", "Settings/Online/St/PeerClosed",
            "Settings/Online/St/BadFrame", "Settings/Online/St/BadEnvelope", "Settings/Online/St/NotConnected",
            "Settings/Online/St/SendFailed", "Settings/Online/St/StackDual", "Settings/Online/St/StackV4Only",
            "Settings/Online/Echo/NoEcho", "Settings/Online/Echo/ProbeError",
            "Settings/Online/Upnp/Busy", "Settings/Online/Upnp/Error", "Settings/Online/Upnp/NoResponse",
            "Settings/Online/Upnp/NoService", "Settings/Online/Upnp/PortTaken", "Settings/Online/Upnp/NotPermitted",
            "Settings/Online/Upnp/Rejected", "Settings/Online/Upnp/Cgnat", "Settings/Online/Upnp/Ok",
            // 🆕 **2026-10-19（P6d · A1079 + A1038 + A1081）**：本件新接/新键的那几条。
            //   ⚠️ `St/{BadHello,VersionMismatch,WrongPassword}` **已经在上面**（P6a 那批建的）——
            //      本轮只是把它们的**走线那一半**也接上（键没换、所以不重复列）。
            "Settings/Online/St/CheckOk",
            "Settings/Online/Upnp/OkWanSuffix",
            "Settings/Online/Wire/HostRestarted", "Settings/Online/Wire/BadKey",
            "Settings/Online/Wire/NoRecord", "Settings/Online/Wire/BadResume",
            "Settings/Online/Wire/PeerDone", "Settings/Online/Wire/PeerLeftMatch",
            "Settings/Online/Wire/NotYourTurn", "Settings/Online/Wire/RoomGone",
            // P6b `NetMatchmaking.cs`（+ `Settings/Online/{Save,CheckConnection}` 是 `PlayedVsBot` 的 `{1}`）
            "Settings/Online/Lobby/PeerLostHint", "Settings/Online/Lobby/PeerLost",
            "Settings/Online/Lobby/PeerLeftHint", "Settings/Online/Lobby/PeerLeft",
            //  🆕 **2026-10-19（P6d · A1079②）**：`DeferToBattleLayer` 那两个 **`what` 碎片**
            //    （喂 `Lobby/DeferToBattle` 的 `{0}`；⛔ 与上面 `PeerLostHint`/`PeerLeftHint` 的 `{0}` 是别的东西）
            "Settings/Online/Lobby/PeerLostFrag", "Settings/Online/Lobby/PeerLeftFrag",
            "Settings/Online/Lobby/MatchRevoked", "Settings/Online/Lobby/NotMatchingThisGame",
            "Settings/Online/Lobby/DeferToBattle", "Settings/Online/Lobby/BotNoLink",
            "Settings/Online/Lobby/BotSessionNotReady", "Settings/Online/Lobby/BotEmptyDeck",
            "Settings/Online/Lobby/PlayedVsBot", "Settings/Online/Lobby/LobbyRestored",
            "Settings/Online/Lobby/StartAfterCancel", "Settings/Online/Lobby/MissedCancel",
            "Settings/Online/Lobby/PeerCancelled", "Settings/Online/Lobby/ModeMismatch",
            "Settings/Online/Lobby/StartParseFailed",
            "Settings/Online/Cancel/WhyNoLink", "Settings/Online/Cancel/WhyNotMatching",
            "Settings/Online/Cancel/WhyStarted",
            "Settings/Online/Save", "Settings/Online/CheckConnection",
        };

        // ---- ① 三条必备之一：本批每条键**都在表里**（坑表 #18：原版有词条 ≠ 我们表里有键）----
        var miss = new List<string>();
        for (int i = 0; i < keys.Length; i++) if (!Loc.HasEntry(keys[i])) miss.Add(keys[i]);
        Eq(miss.Count, 0, $"（P6·双语）本批 **{keys.Length}** 条键**都在语言表里**"
                        + (miss.Count > 0 ? "（缺：" + string.Join(" · ", miss) + "）" : ""));

        // ---- ② 灭自证 C2（反档）+ C1（英文列不许有汉字）----
        var c2 = new List<string>(); var c1 = new List<string>();
        for (int i = 0; i < keys.Length; i++)
        {
            if (Loc.HasEntry(keys[i]) && Loc.T(keys[i]) == keys[i]) c2.Add(keys[i]);
            string en = Loc.EnOf(keys[i]);
            if (string.IsNullOrEmpty(en) || Loc.HasCjk(en)) c1.Add(keys[i]);
        }
        Eq(c2.Count, 0, "（P6·灭自证 C2）本批没有「键在表里、值却是键名本身」的空键"
                      + (c2.Count > 0 ? "（命中：" + string.Join(" · ", c2) + "）" : ""));
        Eq(c1.Count, 0, "（P6·灭自证 C1）本批**英文列一个汉字都没有**（`!Loc.HasCjk(Loc.EnOf(键))`）—— "
                      + "把「实现 + 期望值一起改回写死中文」那条路堵死"
                      + (c1.Count > 0 ? "（命中：" + string.Join(" · ", c1) + "）" : ""));

        // ---- ④ 🆕 2026-10-18（账 `A1057(f)`）：**全表**扫描 —— 英文列逐条都不许含 CJK（含 `U+3000`）----
        //   ⚠️ 上面 ② 的 C1 **只覆盖它自己那个白名单数组**（本批 P6 的键，见本方法开头），**不是全表**
        //      —— 一条本批没列到的键，英文列里塞了汉字，C1 照样绿。本条补的就是这个覆盖口径：
        //      **`Core/Loc.cs` 的整张 `Table`，一条不落**。
        //      📌 同一条断言 `A1025` 也在要（那是**另一笔**）；我这一条是 **`NetSelfTest` 版的表级扫描**，
        //         两者可以并存（各扫各的宿主），⛔ 不要拿它顶替那一笔。
        //
        //   🔴 **为什么走反射**：`Loc` 只开了 `EntryCount`（个数）与 `EnOf(键)`（**按键**取），
        //      **没有**「遍历所有键」的公开口；而 `static readonly Dictionary<string, Entry> Table`
        //      与 `struct Entry` 都是 `private`（`Core/Loc.cs`，那一段在**别的写手**手上、本批不许改）
        //      ⇒ 只能从**测试侧**反射读。本工程早有先例：本文件自己（读 `TcpTransport._state`）
        //      与 `Editor/BattleScene.cs`（读 `TMP_Text.m_fontSizeBase`）。
        //      `Entry` 是私有 struct，但**字段是 `public string Zh, En;`** ⇒ 反射读得到。
        //      判据函数仍用 **`Loc.HasCjk`** —— 本项目**唯一一份**汉字判据（区间含 `0x3000-0x303F`
        //      与 `0xFF00-0xFFEF`，所以全角空格 / 全角括号也会被抓到）。
        //
        //   🧨 **改坏法**：往表里**任意一条**的 EN 列塞一个汉字（或一个全角空格 `U+3000`）⇒ 本条红。
        //   ⛔ **红了不许放宽**：真扫出命中就是**真的漏译**（英文档下那一处会印汉字），如实报、如实修。
        //   ⚠️ **前置必须显式红**：反射拿不到 `Table`（改名 / 改可见性）时**当场红** ——
        //      否则「0 条命中 ≤ 0」会**假绿**（同族教训：量不到的四个 out 全 0 = 弱断言）。
        {
            var tf = typeof(Loc).GetField("Table",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var tbl = tf != null ? tf.GetValue(null) as System.Collections.IDictionary : null;
            Ok(tbl != null && tbl.Count > 0,
               "（P6·A1057(f) 前提）反射拿得到 `Loc` 的**整张** `Table`"
             + (tbl == null
                ? " —— **拿不到**（`Table` 改名 / 改可见性了？）⇒ 下面那条**等于没验**，所以这里当场红"
                : $"（{tbl.Count} 条）"));
            if (tbl != null)
            {
                var cjkEn = new List<string>();
                var blankEn = new List<string>();
                int nScan = 0;
                foreach (System.Collections.DictionaryEntry de in tbl)
                {
                    nScan++;
                    object ent = de.Value;
                    if (ent == null) { blankEn.Add((string)de.Key + "（条目是 null）"); continue; }
                    var ef = ent.GetType().GetField("En");
                    string en = ef != null ? ef.GetValue(ent) as string : null;
                    if (string.IsNullOrEmpty(en)) { blankEn.Add((string)de.Key); continue; }
                    if (Loc.HasCjk(en)) cjkEn.Add((string)de.Key + " = 「" + en + "」");
                }
                // 扫到的条数必须与**公开口** `EntryCount` 相等（两处不同源）⇒ 抓「反射只扫到一部分」
                Eq(nScan, Loc.EntryCount,
                   "（P6·A1057(f)）反射扫到的条数 = `Loc.EntryCount`"
                 + " —— ⛔ 不等就说明这一扫没盖全表（那时下面那两条的绿是假绿）");
                Eq(cjkEn.Count, 0,
                   "★（P6·A1057(f)）**全表**英文列**一个 CJK 字符都没有**"
                 + "（`Loc.HasCjk` 的区间含 `U+3000-303F` / `U+FF00-FFEF`）"
                 + " —— ② 的 C1 只覆盖本批白名单，这一条才覆盖整张表"
                 + (cjkEn.Count > 0 ? "（命中 " + cjkEn.Count + " 条：" + string.Join(" · ", cjkEn.ToArray()) + "）" : ""));
                Eq(blankEn.Count, 0,
                   "★（P6·A1057(f)）全表每条**都有英文列**（英文档下空串 = 那一处没字可显示）"
                 + (blankEn.Count > 0 ? "（命中：" + string.Join(" · ", blankEn.ToArray()) + "）" : ""));
            }
        }

        // ---- ③ 两语档：同一条键在两档下**取值不同且都非空**（灭自证主判据）----
        //    ⚠️ 先存后放：`Loc.Current` 是**跨进程持久**的 ⇒ 不许依赖「现在是哪一档」。
        var lang0 = Loc.Current; bool per0 = Loc.PersistOverride;
        string zhLost = null, enLost = null, zhHint = null, enHint = null;
        int zhHintLen = -1, zhLeftLen = -1;
        try
        {
            Loc.PersistOverride = true;
            Loc.SetLanguage(AvailableLanguages.Chinese);
            zhLost = Loc.T("Settings/Online/Lobby/PeerLost");
            zhHint = Loc.T("Settings/Online/Lobby/PeerLostHint");
            // P6 §⑤ 那条**额外要求**：两条提示行词条要能塞进 `SearchingMatchPopup` 的**提示行尺子**
            // （`HintLineWidth(句) > HintLineMaxWidth` ⇒ **80 个半宽字位**；超了它每次都 `LogWarning`；
            //  ⚠️ 下面这几条按**中文列字符数**算、比的是 `HintLineMaxChars`(= 40 个**汉字**) ——
            //   那是同一条尺子上**更严**的那半边（全宽字 = 2 位 ⇒ 40 字 = 80 位），写法同 `A933` 的注释）
            zhHintLen = Loc.T("Settings/Online/Lobby/PeerLostHint").Length;
            zhLeftLen = Loc.T("Settings/Online/Lobby/PeerLeftHint").Length;
            Loc.SetLanguage(AvailableLanguages.English);
            enLost = Loc.T("Settings/Online/Lobby/PeerLost");
            enHint = Loc.T("Settings/Online/Lobby/PeerLostHint");
        }
        finally
        {
            Loc.SetLanguage(lang0);              // 逐值放回（自检不许把玩家的语言改掉）
            Loc.PersistOverride = per0;
        }
        Ok(zhLost != null && enLost != null && zhHint != null && enHint != null
           && zhLost != enLost && zhLost.Length > 0 && enLost.Length > 0
           && zhHint != enHint && zhHint.Length > 0 && enHint.Length > 0,
           "（P6·灭自证）两条提示行词条**两档逐字不同且都非空**"
         + "（只断一种语档 = 半边绿；实现与期望值一起改回去 ⇒ 这一条当场红）");
        Ok(Loc.Current == lang0, $"（P6 收尾）语言**放回**本节进来时那一档（{lang0}）—— 盘上全程没动过");
        Ok(zhHintLen >= 0 && zhHintLen <= SearchingMatchPopup.HintLineMaxChars,
           $"（P6）`Lobby/PeerLostHint` 中文列 **{zhHintLen}** 字 ≤ 框宽 {SearchingMatchPopup.HintLineMaxChars}"
         + "（超了 `SearchingMatchPopup.ShowHint` 每次都 `LogWarning`）");
        Ok(zhLeftLen >= 0 && zhLeftLen <= SearchingMatchPopup.HintLineMaxChars,
           $"（P6）`Lobby/PeerLeftHint` 中文列 **{zhLeftLen}** 字 ≤ 框宽 {SearchingMatchPopup.HintLineMaxChars}");
        TestWireTextCodec();
    }

    // ==================================================================
    /// <summary>🆕 🔴 **2026-10-19（P6d · A1038）：走线文案的「收侧取词」必须真取到词。**
    ///
    /// <para>**为什么单开这一条**：P6d 之前，`MsgBye.reason` / `MsgReject.reason` / `MsgAck.reason`
    /// 装的是**发送方渲染好的中文整句** ⇒ 对端（英文档）照印中文。接完之后线上发的是**词条键**，
    /// 收侧 `NetWireText.Unpack` 取词 ⇒ **必须断「收侧印出来的是词条值、不是键名」** ——
    /// 本工程对「把键名当正文印出来」的容忍度是零（`Loc.T` 缺键回键名那条就是被自检钉住的）。</para>
    ///
    /// <para>⚠️ **`Wire/*` 这一族在今天之前【零条断言】查过**（`grep "Online/Wire" Editor/*.cs` = 0）——
    /// 上面那张键清单只管「键在不在表里」，**管不了「收侧有没有取词」**。这一条补上那一格。</para>
    ///
    /// <para>🧨 **改坏法**（两条不同源，各自只红一条）：
    ///   ① 把 `NetWireText.Unpack` 里的 `Loc.T(key)` 改成 `return s`（= 收侧不取词）⇒ **①③ 红**；
    ///   ② 把 `Unpack` 的兜底从「原样回显」改成「落一句本地固定话」⇒ **④ 红**
    ///      （那正是主对话明令⛔不许走的那条路）。</para></summary>
    static void TestWireTextCodec()
    {
        // ---- ① 单键：收侧印的是【本机语言】的词条值，**不是键名** ----
        const string kRoomGone = "Settings/Online/Wire/RoomGone";
        string got = NetWireText.Unpack(NetWireText.Pack(kRoomGone));
        Ok(got == Loc.T(kRoomGone) && got != kRoomGone,
           $"（P6d①）`Wire/*` 走线串在收侧被**取成了词条值**（键 `{kRoomGone}` → 「{got}」）"
         + " —— 断「≠ 键名」这一半是硬的：`Loc.T` 缺键时返回的就是键名本身，"
         + "只断 `== Loc.T(键)` 会在「键根本没进表」时也绿");

        // ---- ② 带参数那条：`键|参数1|参数2` 要按**收侧**的模板填回去 ----
        //   （`MsgAck.reason` 的 `St/VersionMismatch` 那一条：`{0}` = 对面的协议版本、`{1}` = 本机那份）
        const string kVer = "Settings/Online/St/VersionMismatch";
        string wire = NetWireText.Pack(kVer, 7, 2);
        string back = NetWireText.Unpack(wire);
        string want = string.Format(Loc.T(kVer), 7, 2);
        Ok(back == want && back.Contains("7") && back.Contains("2") && !back.Contains(kVer),
           $"（P6d②）**带参数**的走线串也取对了词（线上「{wire}」→ 收侧「{back}」）"
         + " —— ⛔ 若改成「线上发已渲染好的整句」，这一条与①会**同时**红（那正是本件要修的病）");

        // ---- ③ 参数个数对不上（对面伪造 / 原串被 `ClampPeerText` 钳断）⇒ **不许抛异常** ----
        bool threw = false; string safe = null;
        try { safe = NetWireText.Unpack(kVer); }        // 只有键、一个参数都没有
        catch (Exception e) { threw = true; safe = e.GetType().Name; }
        Ok(!threw && safe == Loc.T(kVer),
           $"（P6d③）参数个数对不上时**不抛异常**、退回模板本身（实得「{safe}」）"
         + " —— 🧨 这一格原来是 `string.Format` 裸调 ⇒ 会 `FormatException` **打死收包路径**（而且一个字都不说）");

        // ---- ④ 认不出的串（旧端的自然语言 / `RuleCodes.Describe` 那种本机整句）⇒ **原样回显** ----
        //   这条同时钉住主对话那条裁定：「⛔ 不许改成『不认识的串一律落固定本地句』」——
        //   那会让新端**丢掉对面那句话**，而且 M⑳ / NetBattleTest 的「对面离开」那两条会一起红。
        const string oldPeer = "对面结束了这一局";
        Ok(NetWireText.Unpack(oldPeer) == oldPeer,
           $"（P6d④）旧端发来的**自然语言**原样回显（「{oldPeer}」）—— 这是白赚的跨版本兼容；"
         + "🧨 改成「落一句本地固定话」⇒ 红（而且会丢掉对面报的理由）");
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
        // 🔴 **2026-10-19（P6 · 双语）**：这一句不再按中文子串断 —— 它**跟着语档**
        //    （`NetTransport.Describe` 改走 `Settings/Online/St/PortBusy`，前缀是 `St/Stack{Dual,V4Only}`）
        //    ⇒ 写死「占用」在英文档必红。
        string wantPortBusy = string.Format(Loc.T("Settings/Online/St/PortBusy"), p);
        Ok(b.LastError != null && b.LastError.Contains(wantPortBusy)
           && (b.LastError.Contains(Loc.T("Settings/Online/St/StackDual"))
            || b.LastError.Contains(Loc.T("Settings/Online/St/StackV4Only"))),
           $"B③ 起不来时给的是**人话**（键 `Settings/Online/St/PortBusy` + 双栈/仅 IPv4 前缀；"
         + $"实得「{b.LastError}」）");
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
        Ok(host.PeerName == ProfileData.PlayerName,
           $"C⑦ 主机的 `PeerName` = 对面的显示名（实际「{host.PeerName}」；"
           + "⚠️ 唯一的名字来源是 `ProfileData.PlayerName`，2026-09-28 起联机层也读它）");
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
        // 🔴 **2026-10-19（P6 · 双语）**：理由走 `Settings/Online/St/WrongPassword` ⇒ 随语档。
        //    ⚠️ 这条**经 `MsgAck.reason` 过线**（对端把本机的 `why` 原样回灌）⇒ 同一台进程里两档都对。
        Ok(checkWhy != null && checkWhy.Contains(Loc.T("Settings/Online/St/WrongPassword")),
           $"D③ 拒绝理由里有 `Settings/Online/St/WrongPassword` 那条词条（「{checkWhy}」）");
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
        // 🔴 **2026-10-19（P6 · 双语）**：改走 `Settings/Online/St/VersionMismatch`（随语档）。
        //    `{0}` = 对端报来的协议版本（这一格是 `Version + 1`）、`{1}` = 本机那份。
        string wantVer = string.Format(Loc.T("Settings/Online/St/VersionMismatch"),
                                       NetProtocol.Version + 1, NetProtocol.Version);
        Ok(host.LastError != null && host.LastError.Contains(wantVer),
           $"E④ 理由里点明是版本问题（键 `Settings/Online/St/VersionMismatch`；「{host.LastError}」）");
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
            // 🔴 **2026-10-19（P6 · 双语）**：整条走 `Settings/Online/Lobby/PlayedVsBot`（两处 `{0}/{1}`）
            //    + `Settings/Online/{Save,CheckConnection}`（`{1}` = 那颗钮的字）⇒ 断「随语档」而不是中文子串。
            Ok(got.Length == 1 && got[0].Contains(TermHead("Settings/Online/Lobby/PlayedVsBot"))
               && got[0].Contains(Loc.T("Settings/Online/Save")),
               "J③ ★ 那条话要说清「**打的是电脑**」+ 告诉玩家怎么办（不是只报个错）—— 断的是"
             + "`Settings/Online/Lobby/PlayedVsBot` 到 `{0}` 为止那半句 + `Settings/Online/Save`（随语档）");

            // ③ 客机那一支也要会说（`role` 决定提示他去点【检查连接】还是【保存】）
            cfg.role = (int)NetRole.Client;
            NetRuntime.DrainNoticesForTest();
            NetMatchmaking.ExplainNotTakingOver("还没握手完");
            var got2 = NetRuntime.DrainNoticesForTest();
            Ok(got2.Length == 1 && got2[0].Contains(Loc.T("Settings/Online/CheckConnection")),
               "J④ ★ 客机拿到的提示是让他去点【**检查连接**】（主机才是【保存】）—— 断的是"
             + "`Settings/Online/CheckConnection` 那条词条（随语档）");

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
    //  L. 🆕 2026-10-03：**取消这一局的匹配**（`项目任务.md` §三 第 29 条 **A1**）
    // ==================================================================
    /// <summary>原来点 `Cancel` **只关窗、不拆局** —— 配对已经成了，对面照样开局，
    /// `MsgStart` 一到还是会被拉进战场（`RankedEventWindow.CancelSearch` 里只是如实出声）。
    /// 判据 = `NetMatchmaking.Cancel`：**大厅阶段可取消 · 开局之后取消不了（不假装取消成功）**。
    /// ⚠️ **这条链是我们设计的、不是复刻**（原版那是服务端撤单 `MatchMakerManager.CancelSearch`）。
    /// ⚠️ 一个进程里两端各有自己的 `NetMatchmaking` 静态状态（真机上是两个进程）——
    ///    这里靠 `AttachForTest` 换会话来**依次扮演两端**，所以 `Reset()` 会连着调几次（每次都当"换了一台机器"）。</summary>
    static void TestMatchCancel(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        var rt = NetRuntime.Ensure();
        var keep = rt.Session;
        try
        {
            Ok(host.StartHost(Cfg(port, "")), "L① 主机起来了");
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
               "L② 两边握手到 `Lobby`");

            var d1 = new PlayerDeck { Name = "自检主机牌", WarlordId = "UM_WARLORD" };
            d1.CardIds.Add("UM1");
            var d2 = new PlayerDeck { Name = "自检客机牌", WarlordId = "UM_WARLORD" };
            d2.CardIds.Add("UM2");

            // ---- ① **大厅阶段**：取消得了，而且对面会收到 ----
            NetMatchmaking.Reset();
            rt.AttachForTest(host);
            Ok(NetMatchmaking.TryStart(d1, "Classic", "Ultramarines", out string _),
               "L③ 主机点 `Battle!` ⇒ 联机接管这一局");
            Ok(NetMatchmaking.Waiting, "L④ 主机在等对面交卡组（`Waiting`）");

            // 🆕 2026-10-10（`A985⑫`）：取消那一刻那条 `[BattleResult] Cancelled(5)` 记录 ——
            //   🔴 **抓的是日志流**（`Application.logMessageReceived`，本文件 §Q 已是这个抓法）⇒
            //   谁把那一句 `Debug.Log` 删掉，下面两条立刻红（不是读一个「实现自己会写的镜像变量」）。
            var cancelLogs = new List<string>();
            Application.LogCallback hCancel = (string m, string st, LogType ty) => cancelLogs.Add(m);
            bool cancelled;
            string w0;                       // ⚠️ 声明在 `try` **外面**（L⑥ 还要读它 —— `try` 里声明的出不来）
            Application.logMessageReceived += hCancel;
            try { cancelled = NetMatchmaking.Cancel("自检：大厅阶段取消", out w0); }
            finally { Application.logMessageReceived -= hCancel; }
            Ok(cancelled, "L⑤ ★ **大厅阶段取消得了**（原来只关窗、对面照样把你拉进战场）");
            Ok(cancelLogs.Exists(m => m != null && m.Contains("[BattleResult] Cancelled(5)")),
               "L⑤-b ★★ 取消那一刻记下了**理由码**（`[BattleResult] Cancelled(5)` —— 原版 `CancelMatch.c:29` 那一档）"
             + $"（实得 {cancelLogs.Count} 条，末条「{(cancelLogs.Count > 0 ? cancelLogs[cancelLogs.Count - 1] : "")}」）"
             + " —— 🧨 改坏法：把 `NetMatchmaking.Cancel` 里那句 `Debug.Log(ReplayStore.ResultLine(…))` 删掉 ⇒ 红"
             + "（**静默时必红**，⛔ 不是同义反复）");
            Ok(cancelLogs.Exists(m =>
               {
                   if (m == null) return false;
                   BattleResult rr; int ss;
                   return ReplayStore.TryParseResultLine(m, out rr, out ss)
                       && rr == BattleResult.Cancelled && ss == -1;
               }),
               "L⑤-c ★ …而且那一行**能被录像侧同一个解析器读回来**（码 = `Cancelled`、座位 = `-1` = **没写**，"
             + "那一刻还没有对局）—— 与 L⑤-b **不同源**：那条比字面，这条比「匹配层写的」与「录像层读的」是不是一套");
            Ok(w0 == null && NetMatchmaking.ICancelled, "L⑥ 本机记着「我取消过」（`ICancelled`）");
            Ok(!NetMatchmaking.Waiting, "L⑦ 本地状态已复位（不再显示「等待对手」）");

            PumpBoth(host, cli, 300);
            rt.AttachForTest(cli);
            // 🆕 2026-10-11（`A1297①`）：**收到 `match.cancel` 那一支**也要记一档码（原来一个字都没有）。
            //   抓法同 L⑤-b = `Application.logMessageReceived`（**日志流**，⛔ 不是实现顺手写的镜像变量）。
            var foeCancelLogs = new List<string>();
            Application.LogCallback hFoeCancel = (string m, string st, LogType ty) => foeCancelLogs.Add(m);
            Application.logMessageReceived += hFoeCancel;
            try { NetMatchmaking.PumpLobby(); }
            finally { Application.logMessageReceived -= hFoeCancel; }
            Ok(NetMatchmaking.FoeCancelled, "L⑧ ★ **对面收到了「取消」**（`match.cancel` 到得了）");
            var n1 = NetRuntime.DrainNoticesForTest();
            Ok(n1.Length == 1 && n1[0] == Loc.T("Settings/Online/Lobby/PeerCancelled"),
               "L⑨ ★ 对面那边**弹出人话**（红线：不许静默）—— 逐字 = `Settings/Online/Lobby/PeerCancelled`"
             + $"（随语档；实得「{(n1.Length > 0 ? n1[0] : "")}」）");
            // 🆕 2026-10-11（`A1297①`）：**这一局没了** ⇒ 收侧也要记一档码。
            //   🔴 **与 L⑤-b 是两条不同的代码路径**（那条是**发侧** `Cancel()` 自己记的、这条是**收侧**
            //   `switch (NetKind.MatchCancel)` 那支记的）⇒ 谁把哪一支删掉，只有对应那条红。
            //   码 = `Cancelled`(5)：⚠️ **原版无此路，码是我们挑的**（带码的调用点只有 `CancelMatch.c:29`
            //   一处、那是玩家自己撤；「对面撤」这个事件在服务端匹配的原版里不存在）。
            Ok(foeCancelLogs.Exists(m => m != null && m.Contains("[BattleResult] Cancelled(5)")),
               "L⑨-b ★★ **收侧也记了一档码**（`[BattleResult] Cancelled(5)`）"
             + $"（实得 {foeCancelLogs.Count} 条日志）"
             + " —— 🧨 改坏法：把 `NetMatchmaking` 那支 `else` 里那句 `Debug.Log(ReplayStore.ResultLine(…))` 删掉 ⇒ 红"
             + "（**静默时必红**，⛔ 不是同义反复）");
            Ok(foeCancelLogs.Exists(m =>
               {
                   if (m == null) return false;
                   BattleResult rr; int ss;
                   return ReplayStore.TryParseResultLine(m, out rr, out ss)
                       && rr == BattleResult.Cancelled && ss == -1;
               }),
               "L⑨-c ★ …而且那一行**能被录像侧同一个解析器读回来**（码 = `Cancelled`、座位 = `-1` = **没写**）"
             + " —— 与 L⑨-b **不同源**：那条比字面，这条比「匹配层写的那一行」与「录像层读的那一行」是不是一套"
             + "（⛔ 别在 `Net/` 里另拼字面量 —— 那正是「两处写同一条规则」）");

            // ---- ② **两边重新各点一次** ⇒ 开局；**开局之后取消不了** ----
            NetMatchmaking.Reset();
            rt.AttachForTest(host);
            Ok(NetMatchmaking.TryStart(d1, "Classic", "Ultramarines", out string _),
               "L⑩ 取消之后**还能重新打一局**（主机重新点一次 `Battle!`）");
            rt.AttachForTest(cli);
            Ok(NetMatchmaking.TryStart(d2, "Classic", "Ultramarines", out string _), "L⑪ 客机也点一次");
            PumpBoth(host, cli, 400);
            rt.AttachForTest(host);
            NetMatchmaking.PumpLobby();                       // 主机收到卡组 ⇒ 开局（`_started = true`）
            Ok(NetMatchmaking.HasOpponent, "L⑫ 主机收到对面卡组 ⇒ 这一局成立了");

            // ---- 🔴 反面（`A1297①` 的判别式）：**开局之后才到的** `match.cancel` ⇒ 这一局照旧开 ⇒
            //      ⛔ **一条码都不许记**（记了就是「静默说错一句话」）。
            //      ⚠️ 这一包**手工发**：`NetMatchmaking.Cancel()` 在 `_started` 时自己就返回 false（L⑬ 验的是它）
            //         ⇒ 只有自己发才能把「迟到的取消」喂进对面那一支。
            //      🔑 **为什么它是不自证**：正例（L⑨-b）与这条**结构上互斥** —— 两支都在同一个 `switch` 里，
            //         把记码从 `else` 支提到 `case` 支外面（= 两支都记）⇒ **这条立刻红、L⑨-b 照样绿**。
            var lateCancelLogs = new List<string>();
            Application.LogCallback hLate = (string m, string st, LogType ty) => lateCancelLogs.Add(m);
            cli.Send(NetKind.MatchCancel, new MsgMatchCancel { reason = "自检：迟到的取消" });
            PumpBoth(host, cli, 300);
            rt.AttachForTest(host);
            Application.logMessageReceived += hLate;
            try { NetMatchmaking.PumpLobby(); }
            finally { Application.logMessageReceived -= hLate; }
            Ok(!lateCancelLogs.Exists(m => m != null && m.Contains("[BattleResult]")),
               "L⑫-b ★★ **开局之后才到的 `match.cancel` ⇒ 一条码都不许记**（那一支是「这一局照旧开」、没有「没了」）"
             + $"（实得 {lateCancelLogs.Count} 条日志、带 token 的 "
             + $"{lateCancelLogs.FindAll(m => m != null && m.Contains("[BattleResult]")).Count} 条）");
            Ok(NetMatchmaking.HasOpponent, "L⑫-c …而且那一局**没有**被撤掉（`HasOpponent` 还在）");
            NetRuntime.DrainNoticesForTest();

            Ok(!NetMatchmaking.Cancel("自检：开局之后", out string w2),
               "L⑬ ★ **开局之后取消不了**（**不假装取消成功** —— 原来那句「取消的只是这扇窗」就是这么来的）");
            Ok(!string.IsNullOrEmpty(w2) && w2.Contains(Loc.T("Settings/Online/Cancel/WhyStarted")),
               $"L⑭ 而且要说清**为什么**、以及该怎么办（`Settings/Online/Cancel/WhyStarted`，随语档；「{w2}」）");

            // ---- ③ 没人连 / 没进匹配 ⇒ 取消不了，且理由是人话 ----
            NetMatchmaking.Reset();
            var off = NetSession.NewTcp();                    // 一台没连过的会话
            rt.AttachForTest(off);
            Ok(!NetMatchmaking.Cancel("自检：没连上", out string w3) && !string.IsNullOrEmpty(w3),
               $"L⑮ 没连上时说「{w3}」—— **不是静默的 `false`**");
        }
        finally
        {
            rt.AttachForTest(keep);
            NetMatchmaking.Reset();
            NetRuntime.DrainNoticesForTest();
            host.Close(false); cli.Close(false);
        }
    }

    // ==================================================================
    //  S. 🆕 2026-10-10（`A985⑬`）：录像头尾两条【只落盘】的 `[BattleResult]` 记录
    // ==================================================================
    /// <summary>判据 = `项目任务.md` 第 373 行 `A985` 尾巴 + §四·16（**走【最小设计】**）；
    /// 背景与「为什么不加在动作上」→ `Battle/ReplayStore.cs` 文件头那一节。
    ///
    /// <para>要钉住四件：① 那行字**与引擎同形**（比的是 `RuleCore.Forfeit` / `CheckWinner` 那两句
    /// `ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}")` 的**字面量**）；
    /// ② **读侧**认得引擎写的那种行（拿外来字面量喂它）；③ 它**真的落在盘上**（`Save` → `Load` 一轮回来了）；
    /// ④ 它**不在上网那个结构里**（`MsgAction` 的字段反射 —— 那正是「⛔ 别给它加字段」那条红线的牙口）。</para>
    ///
    /// <para>🔴 **本测试只碰临时目录**（`ReplayStore.OverrideDir`）—— ⛔ 不碰玩家的真录像
    /// （`ReplayStore.ResetForTest` 是删文件的，⛔ 还原目录之前绝不能调它，见 `finally`）。</para>
    ///
    /// <para>⚠️ **本测试跑的是「存储与读写」这一层**；「`PlayReplay` 重放 `RecKindForfeit` 时真的拿它喂引擎」
    /// 那一跳在 `BattleDriver.cs`（**本轮白名单外** ⇒ 没接、如实记在报告里）。</para></summary>
    static void TestReplayResultCodeA985()
    {
        string dir = Path.Combine(Path.GetTempPath(), "wf_replay_a985");
        var keepDir = ReplayStore.OverrideDir;
        try
        {
            ReplayStore.OverrideDir = dir;
            ReplayStore.ResetForTest();

            // ---- ① 书写端：与引擎那两句 `ctx.Log` 同形（字面量由**引擎侧**抄来，不是实现自己产出的）----
            Eq(ReplayStore.ResultLine(BattleResult.Forfeit, 1), "[BattleResult] Forfeit(2) seat=1",
               "S① ★ 录像那条记录**与引擎同形**（`RuleCore.Forfeit` 那句 `ctx.Log` 的形状）"
             + " —— 🧨 改坏法：把 `ResultLine` 的拼法改掉 ⇒ 红");
            Eq(ReplayStore.ResultLine(BattleResult.Cancelled, -1), "[BattleResult] Cancelled(5)",
               "S② ★★ …**没有座位**时**不写 `seat=`**（`seat < 0`）—— 这个字符串与 §L⑤-b 抓的那条**逐字相同**"
             + "（⛔ 别在 `Net/` 里另拼一份：那是「两处写同一条规则」）");

            // ---- ② 读端：拿**外来字面量**喂它（若用 `ResultLine` 的输出喂，就是自证）----
            BattleResult pr; int ps;
            Ok(ReplayStore.TryParseResultLine("[BattleResult] Disconnect(3) seat=1", out pr, out ps)
               && pr == BattleResult.Disconnect && ps == 1,
               "S③ ★★ 读侧认得**引擎写的那种行**（字面量直接喂；与 S①/S② 不同源：那两条看书写端、这条看解析端）");
            Ok(ReplayStore.TryParseResultLine("[BattleResult] Forfeit(2) seat=0 —— 某人 投降 —— 对面获胜", out pr, out ps)
               && pr == BattleResult.Forfeit && ps == 0,
               "S④ ★ **行尾的人话不影响解析**（引擎那两句 `ctx.Log`、以及匹配层那一条，都带着人话）");
            Ok(!ReplayStore.TryParseResultLine("这不是一条理由码记录", out pr, out ps),
               "S⑤（对照）**不是那个 token 的行 ⇒ `false`** —— S③/S④ 不是「反正都返回 true」的假象");
            Ok(!ReplayStore.TryParseResultLine("[BattleResult] NotARealOne(9) seat=0", out pr, out ps),
               "S⑥ ★ 码名认不出 ⇒ **`false`**（⛔ 不猜一档 —— 铁律 2）");

            // ---- ③ 真的落在盘上：写 → 存 → 读 ----
            var rec = new ReplayRecord { myHero = "自检我", foeHero = "自检对面", savedAt = "2026-10-10 00:00:00" };
            ReplayStore.WriteResultHead(rec, BattleResult.Undefined, -1);   // 开局那一刻：还没有结果
            ReplayStore.WriteResultTail(rec, BattleResult.Disconnect, 1);  // 结算那一刻：掉线判弃权
            int actsBefore = rec.actions.Count;
            string name = ReplayStore.Save(rec);
            var back = string.IsNullOrEmpty(name) ? null : ReplayStore.Load(name);
            Ok(back != null && ReplayStore.ReadResultTail(back) == BattleResult.Disconnect,
               "S⑦ ★★ 尾那条**真的落在盘上**（`Save` → `Load` 一轮回来还是 `Disconnect`(3)）"
             + $"（实得 `{back?.resultTail ?? "<null>"}`）");
            Ok(back != null && ReplayStore.HasResultHead(back)
               && ReplayStore.ReadResultHead(back) == BattleResult.Undefined,
               "S⑧ 头那条也在，而且是「**开局那一刻还没有结果**」= `Undefined(0)`（⛔ 不是没记）");
            Ok(back != null && ReplayStore.ReadResultCode(back) == BattleResult.Disconnect,
               "S⑨ ★ **「回放时读它」的那一个口**（`ReadResultCode`，尾优先）读出同一档");
            Eq(back == null ? -1 : back.actions.Count, actsBefore,
               "S⑩ ★ **理由码没有进 `actions`**（头尾两条是录像自己的两格，不是动作 —— 进了动作就是「也上网」）");
            Ok(ReplayStore.LastResultFromEvents(
                   new List<string> { "别的日志", "[BattleResult] Forfeit(2) seat=0", "又是一句别的日志" }, out int evSeat)
               == BattleResult.Forfeit && evSeat == 0,
               "S⑪ ★ **取码桥**（`LastResultFromEvents`）：从 `ctx.Events` 那样的串里**从尾往前**捞到最后一条"
             + " —— 码不在 `ctx` 的任何字段上，结算时**只能**这么捞");
            Ok(ReplayStore.LastResultFromEvents(new List<string> { "全是人话，没有码" }, out int evSeat2)
               == BattleResult.Undefined && evSeat2 == -1,
               "S⑫（对照）**一条都没有 ⇒ `Undefined` + 座位 `-1`**（如实说「未指定」，⛔ 不猜一档）");

            // ---- ④ 「不上网」的牙口 ----
            bool hasReasonField = false;
            foreach (var f in typeof(MsgAction).GetFields())
            {
                string fn = f.Name.ToLowerInvariant();
                if (fn.Contains("result") || fn.Contains("reason")) hasReasonField = true;
            }
            Ok(!hasReasonField,
               "S⑬ ★★ **理由码不在上网那个结构里**（`MsgAction` 的字段名里既没有 `result` 也没有 `reason`）"
             + " —— 🔴 这是「⛔ 不许给 `MsgAction` 加字段」那条红线的牙口（原版 `SendForfeit` 发的是空参数表、"
             + "码从来不过网）；🧨 改坏法：往 `MsgAction` 加一格 `resultCode` ⇒ 红");
            Ok(!NetProtocol.Pack(new MsgAction { seq = 0, kind = 0, actor = 0, marks = new int[0] })
                 .Contains("[BattleResult]"),
               "S⑭（对照）…而**真发出去的那一份**（`NetProtocol.Pack(MsgAction)` = `NetKind.Action` 发的东西）"
             + "里没有那个 token");

            // ---- ⑤ 老录像 / 坏行 ----
            var oldRec = new ReplayRecord { myHero = "老", foeHero = "录" };
            oldRec.resultHead = null; oldRec.resultTail = null;   // =「JSON 里没有那两个键」的两种落法之一
            Ok(!ReplayStore.HasResultHead(oldRec) && !ReplayStore.HasResultTail(oldRec)
               && ReplayStore.ReadResultCode(oldRec) == BattleResult.Undefined,
               "S⑮ ★ 老录像（两格都没有）⇒ **`Undefined` = 「没记/未指定」**、`HasResult*` 都是假"
             + "（⛔ 不冒充某一档 —— 那样「掉线判弃权」会被演成「投降」）");
            oldRec.resultTail = "[BattleResult] 这不是码(99)";
            Ok(ReplayStore.ReadResultTail(oldRec) == BattleResult.Undefined,
               "S⑯（坏行）**记过但认不出** ⇒ 当「未指定」并**出声**（不许静默）");
        }
        finally
        {
            ReplayStore.ResetForTest();          // 🔴 必须**还在临时目录上**时清（`ResetForTest` 是删文件的）
            ReplayStore.OverrideDir = keepDir;   // ⛔ 顺序反了 = 删掉玩家的真录像
        }
    }

    // ==================================================================
    //  T. 🆕 2026-10-11（`A1296`）：**两张表**（`ctx.Events` vs `ctx.ActionLog`）与录像黑匣子
    // ==================================================================
    /// <summary>账 `A1296`（第十二会话 `V1` 顺手查出、⛔ 当时没动手）：`ctx.Log` 写的是 `ctx.Events`，
    /// 而 `BattleDriver.EvtTail` / `ReplayRecord.traceLogTail` 读的是 `ctx.ActionLog` —— **两个不同的表**
    /// ⇒ 后果 = 黑匣子**永远抓不到** `[BattleResult]` 那一行（= `V1` 报告 §5 第 2 条）。
    ///
    /// <para>🔴 **2026-10-11 现读后判：有意设计，不是缺陷。** 三条判据（都在仓库里、逐处现读）：
    /// ① `Battle/ReplayStore.cs:190` 把那格就定义成「每条动作落地后 **`ActionLog`** 尾那一句」；
    /// ② `Battle/BattleDriver.cs:440` 的 `EvtTail` 摘要同样写的是 `ActionLog`，`:745` 打的那行字也叫「log 尾」；
    /// ③ `RuleCore/Core/RuleCore.cs:6587-6589` 把两条通道的分工写死了 —— 理由码只进 `ctx.Events`
    /// 那条（**我们自己的诊断**日志），**不是** `ctx.ActionLog` 那本**给玩家看的战斗日志**。
    /// ⇒ 两边各按自己的用途读自己的表，「抓不到」是**它的定义**，不是漏接线。</para>
    ///
    /// <para>🔴 **后果无害，两条证据**：① 黑匣子的判据是 `BattleDriver.DeepHash`（**不含 `ctx.Events`**）
    /// ⇒ 它的信号与它的读法**是同一份口径**，没有「看得见的问题说不出」这种盲区；
    /// ② 理由码的**存档通道**是录像的**头 / 尾两条只落盘记录**（`ReadResultCode`）—— 与黑匣子无关，
    /// 而且黑匣子**默认关**（`ReplayStore.VerboseTrace`，真打时一局 9 KB）⇒「靠黑匣子把码带出去」
    /// 这条路**本来就不成立**（`T④`/`T④-b` 钉的就是这一条）。</para>
    ///
    /// <para>🔴 **这一节要堵住的是「最顺手的那种修法」**：让 `ctx.Log` 也往 `ActionLog` 里写一条，
    /// 或把 `EvtTail` 改成读 `ctx.Events` —— 前者会**污染给玩家看的战斗日志**（那是玩法表现），
    /// 后者会**改掉黑匣子的口径**。`T①` / `T③-b` 是把这两条退路各自堵死的**结构**断言。</para>
    ///
    /// <para>⚠️ **本测试一行 UI / 一局对局都不建**（`new BattleContext(seed)` 足够 —— `Log`/`Emit` 都不碰
    /// `Players`）；只碰 `Path.GetTempPath()` 下的临时目录（`finally` 里**先清后还原** `OverrideDir`）。</para></summary>
    static void TestResultCodeBlackBoxA1296()
    {
        // ---- ① 结构事实：两张表就是两张表（`ctx.Log` 只碰 `Events`）----
        var ctx = new BattleContext(20261011);
        int ev0 = ctx.Events.Count, al0 = ctx.ActionLog.Count;
        ctx.Log("[BattleResult] Disconnect(3) seat=1");
        Ok(ctx.Events.Count == ev0 + 1 && ctx.ActionLog.Count == al0,
           "T① ★★ **`ctx.Log` 只写 `ctx.Events`**（`ActionLog` 一条都不多）—— 这就是 `A1296` 那条账的**结构前提**"
         + $"（Events {ev0}→{ctx.Events.Count} · ActionLog {al0}→{ctx.ActionLog.Count}）"
         + "；🧨 改坏法：让 `ctx.Log` 也往 `ActionLog` 里塞一条（= 最顺手的那种「修」）⇒ 红"
         + "（那是**给玩家看的战斗日志**，改它 = 静默改玩法表现）");

        // ---- ② 反向（对照）：引擎事件走 `ActionLog`、**不回流**进 `Events` ----
        ctx.Emit(new BattleEvent { Kind = EvtKind.Hit, Player = 0, Slot = 2,
                                   TargetPlayer = 1, TargetSlot = 3, CardId = "A1296_MARK" });
        Ok(ctx.ActionLog.Count == al0 + 1 && ctx.Events.Count == ev0 + 1,
           "T②（对照）**两条通道单向、不交叉**：引擎事件只进 `ActionLog`"
         + $"（Events {ctx.Events.Count} · ActionLog {ctx.ActionLog.Count}）"
         + " —— 没有这一条，T① 挡不住「两边互相抄一份」那种改法");

        // ---- ③ `EvtTail`（= 黑匣子那一格的数据源）读的确实是 `ActionLog` ----
        var mi = typeof(BattleDriver).GetMethod("EvtTail",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Ok(mi != null,
           "T③ 夹具：反射拿得到 `BattleDriver.EvtTail`（拿不到 = 它改名了 ⇒ 下面是【没验到】，⛔ 不是通过）");
        if (mi == null)
        {
            _warn++;
            Debug.LogWarning("[NetSelfTest] ⚠️ T③-a/T③-b **没验到**：拿不到 `BattleDriver.EvtTail`"
                           + "（被改名/改可见性了？）—— `A1296` 的「黑匣子读哪张表」这一格就没人看着了。");
        }
        else
        {
            string tail = mi.Invoke(null, new object[] { ctx }) as string;
            Ok(tail != null && tail.Contains("A1296_MARK"),
               "T③-a ★★ **黑匣子那一格读的是 `ActionLog` 尾**（解出来的正是刚发的那条引擎事件）"
             + $"（实得「{tail}」）");
            Ok(tail != null && !tail.Contains("[BattleResult]"),
               "T③-b ★★ …而且**看不到** `ctx.Events` 里的东西（那行 `[BattleResult]` 在它眼里不存在）"
             + " —— 🔴 这就是 `A1296` 记的那句「抓不到那一行」**如实钉住**（判为【有意设计、不是缺陷】："
             + "见本方法头那三条判据）；🧨 改坏法：把 `EvtTail` 改成读 `ctx.Events` ⇒ **本条红** ——"
             + " 那说明**口径变了**，要回头**重判这一档**并把结论一起改（⛔ 别只把这条断言改成绿的）");
        }

        // ---- ④ 码的存档通道与黑匣子**无关**（黑匣子关着也读得出码）----
        string dir = Path.Combine(Path.GetTempPath(), "wf_replay_a1296");
        var keepDir = ReplayStore.OverrideDir;
        bool keepVerbose = ReplayStore.VerboseTrace;
        try
        {
            ReplayStore.OverrideDir = dir;
            ReplayStore.ResetForTest();
            ReplayStore.VerboseTrace = false;                  // = **真打时的默认档**（自检里别继承上一条留下的值）
            var rec = new ReplayRecord { myHero = "黑匣子我", foeHero = "黑匣子对面" };
            ReplayStore.WriteResultTail(rec, BattleResult.Disconnect, 1);
            string name = ReplayStore.Save(rec);
            var back = string.IsNullOrEmpty(name) ? null : ReplayStore.Load(name);
            Ok(back != null && (back.traceLogTail == null || back.traceLogTail.Count == 0),
               "T④ ★ 黑匣子**关着**时那一格盘上没有任何内容"
             + "（⚠️ 只断「空」，不断「是 `null` 还是空表」—— `JsonUtility` 对空表的落法在本机没现核过）");
            Ok(back != null && ReplayStore.ReadResultCode(back) == BattleResult.Disconnect,
               "T④-b ★★ …**而理由码照样读得回来**（`Disconnect`）—— 码的存档通道 = 录像**头/尾两条只落盘记录**，"
             + "**与黑匣子无关**（黑匣子默认关 ⇒ 「靠黑匣子把码带出去」这条路本来就不成立）"
             + "；🧨 改坏法：把码改成「只在 `traceLogTail` 里记」⇒ 红");
        }
        finally
        {
            ReplayStore.ResetForTest();          // 🔴 必须**还在临时目录上**时清（`ResetForTest` 是删文件的）
            ReplayStore.OverrideDir = keepDir;   // ⛔ 顺序反了 = 删掉玩家的真录像
            ReplayStore.VerboseTrace = keepVerbose;
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
    //  M. 🆕 2026-10-17（B23·A902）：**大厅阶段（还没进对局）对面掉线 / 离开** —— 出声 + 撤这一局的匹配
    // ==================================================================
    /// <summary>原来 `NetMatchmaking` **根本不看会话状态** ⇒ 大厅阶段对面掉了，本机**什么反应都没有**
    /// （只有设置窗那行 `StatusText` 会变）—— 玩家会一直干等对面点 `Battle!`（**静默**，红线）。
    ///
    /// <para>判据（原版，全量反编译逐跳；全文 → `Net/NetMatchmaking.cs` 的 A902 那一节头部）：
    /// `BattleNetworkManager__EventDisconnected.c`（连接状态 **20 = `searchingRandomOponent`**）
    /// ⇒ `SearchOpponentManager__CancelSearchForDisconnect.c` ⇒ **弹窗**（文案键 `CustomErrors/InternetUnreachable`、
    /// 钮 `MainMenu/General/OK`）+ `MatchMakerManager.CancelSearch`（把这一局撤掉回大厅）。</para>
    ///
    /// <para>🔴 **闸 = `NetRuntime.LobbyHandled`** —— 大厅这一半与 `NetBattle`（对局那一半）**互斥**：
    /// 一台会话上只有一边说话（M⑬ 就是钉这一条的；对局那一半的断言在 `NetBattleTest` §9）。
    /// ⚠️ 一个进程里两端各有自己的 `NetMatchmaking` 静态状态（真机上是两个进程）——
    /// 这里靠 `AttachForTest` 换会话来依次扮演两端（同 `TestMatchCancel` 那一段的做法）。</para></summary>
    static void TestLobbyPeerGone(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        var rt = NetRuntime.Ensure();
        var keep = rt.Session;
        bool keepLobby = rt.LobbyHandled;
        try
        {
            Ok(host.StartHost(Cfg(port, "")), "M① 主机起来了");
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
               "M② 两边握手到 `Lobby`（还没点 `Battle!`）");

            NetMatchmaking.Reset();
            rt.LobbyHandled = true;            // 这一段的夹具前提 = **大厅阶段**（`NetBattle` 还没接管这台会话）
            rt.AttachForTest(host);            // 🔴 生产路径同一步：`NetRuntime` 把大厅那条接线挂到这台会话上
            Ok(!NetMatchmaking.PeerGone, "M③ 起手：「对面不在」那个边沿是清的");

            var d1 = new PlayerDeck { Name = "自检大厅牌", WarlordId = "UM_WARLORD" };
            d1.CardIds.Add("UM1");
            Ok(NetMatchmaking.TryStart(d1, "Classic", "Ultramarines", out string _), "M④ 主机点 `Battle!` ⇒ 进匹配");
            Ok(NetMatchmaking.Waiting, "M⑤ 主机在等对面交卡组（`Waiting`）");
            NetRuntime.DrainNoticesForTest();

            // ---- ① 对面**掉线**（心跳超时 / 连接断）⇒ 必须出声 ----
            // 🆕 2026-10-11（`A1297②`）：**这一局没了**那一跳（`RevokeMatchLocal`）也要记一档码 ——
            //   抓法同 §L（`Application.logMessageReceived` = **日志流**，⛔ 不是实现顺手写的镜像变量）。
            var dropLogs = new List<string>();
            Application.LogCallback hDrop = (string m, string st, LogType ty) => dropLogs.Add(m);
            cli.Transport.ClosePeer();
            bool dropped;
            Application.logMessageReceived += hDrop;
            try { dropped = PumpUntil(host, cli, () => host.State == NetState.WaitingReconnect, 6000); }
            finally { Application.logMessageReceived -= hDrop; }
            Ok(dropped,
               $"（大厅·掉线）主机发现对面没了（实际 {host.State}）");
            var n1 = NetRuntime.DrainNoticesForTest();
            Eq(n1.Length, 1,
               "M⑥ ★ **大厅阶段对面掉线要弹一条**（原来零接线 ⇒ 一条都没有）"
             + " —— 🧨 改坏法：删掉 `NetRuntime.Init`（或 `Reset` / `AttachForTest`）里那句 "
             + "`NetMatchmaking.WireLobby(…)` ⇒ 红");
            Ok(n1.Length >= 1 && n1[0].Contains(TermHead("Settings/Online/Lobby/PeerLost"))
               && n1[0].Contains(Loc.T("Settings/Online/Lobby/MatchRevoked")),
               "M⑦ ★ 那条话点明是**联机断开 / 对面掉线**，并把「这一局撤没撤」说清"
             + "（断 `Settings/Online/Lobby/PeerLost` 到 `{0}` 为止那半句 + `…/MatchRevoked`；"
             + $"实得「{(n1.Length > 0 ? n1[0] : "")}」）");
            Ok(NetMatchmaking.LastHint != null
               && NetMatchmaking.LastHint.Contains(TermHead("Settings/Online/Lobby/PeerLostHint")),
               $"M⑧ ★ **提示行**也说了（实得「{NetMatchmaking.LastHint}」）—— 断的是"
             + "`Settings/Online/Lobby/PeerLostHint` 到 `{0}` 为止那半句（随语档）；"
             + "自检读 `LastHint`；真 Play 里那行字归界面（订 `NetMatchmaking.OnHint`）");
            Ok(!NetMatchmaking.Waiting,
               "M⑨ ★ 这一局的匹配**被撤掉了**（= 原版那一刻 `MatchMakerManager.CancelSearch`：回大厅）"
             + " —— 与 M⑥ **不同源**：M⑥ 验的是「说不说」，这条验的是「局撤没撤」");
            Ok(NetMatchmaking.PeerGone, "M⑩ 「对面不在」那个边沿已置上");
            Ok(dropLogs.Exists(m => m != null && m.Contains("[BattleResult] Disconnect(3)")),
               "M⑩-b ★★ **掉线那一跳也记了一档码**（`[BattleResult] Disconnect(3)` —— `A1297②`）"
             + $"（实得 {dropLogs.Count} 条日志）"
             + " —— 🧨 改坏法：把 `RevokeMatchLocal` 里那句 `Debug.Log(ReplayStore.ResultLine(…))` 删掉 ⇒ 红"
             + "（**静默时必红**，⛔ 不是同义反复）");
            Ok(dropLogs.Exists(m =>
               {
                   if (m == null) return false;
                   BattleResult rr; int ss;
                   return ReplayStore.TryParseResultLine(m, out rr, out ss)
                       && rr == BattleResult.Disconnect && ss == -1;
               }),
               "M⑩-c ★ …而且那一行**能被录像侧同一个解析器读回来**（码 = `Disconnect`、座位 = `-1` = **没写**）"
             + " —— 与 M⑩-b **不同源**：那条比字面，这条比「匹配层写的那一行」与「录像层读的那一行」是不是一套；"
             + "⚠️ 码是 `Disconnect`(3) 而【不是】`Cancelled`(5) —— 这两笔**不是同一档**（取消那一档在 §L⑨-b）");

            // ---- ② 对面**回来** ⇒ 撤销（撤窗 + 提示行改口）----
            int hide0 = NetRuntime.HidePopupCallsForTest;
            cli.CheckConnection(Cfg(port, ""));      // 大厅阶段**没有**自动重连退避（`NetSession.Pump` 那条要 `_wasInBattle`）⇒ 手动重连
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 6000),
               "M⑪ 对面回来了（两边都回到 `Lobby`）");
            rt.AttachForTest(host);
            NetMatchmaking.PumpLobby();              // 真 Play 里由 `NetRuntime.Update` 每帧调
            Ok(!NetMatchmaking.PeerGone, "M⑫ ★ 那个边沿清掉了（只认边沿 ⇒ 不会每帧说一遍）");
            Ok(NetRuntime.HidePopupCallsForTest > hide0,
               "M⑬ ★ 回来 ⇒ **去撤窗了**（原版那一刻是 `CloseAllWindows`；我们只收弹窗那一颗 —— B13 已记）");
            Ok(NetMatchmaking.LastHint != null
               && NetMatchmaking.LastHint.Contains(TermHead("Settings/Online/Lobby/LobbyRestored")),
               $"M⑭ ★ 提示行**改口**成「回来了」（`Settings/Online/Lobby/LobbyRestored`，随语档；"
             + $"实得「{NetMatchmaking.LastHint}」）—— 与 M⑧ 不同源："
             + "M⑧ 验的是掉线那一下说不说，这条验的是**恢复之后会不会改口**");

            // ---- 🔴 ②-b **反面（`A1297②` 的判别式）**：**本机本来就没在匹配这一局**（`had == false`）
            //      ⇒ ⛔ **一条码都不许记**（没有「局」可没 —— 记了就是静默说错一句话）。
            //      🔑 **为什么它不自证**：与 M⑩-b **结构上互斥** —— 把记码从 `if (had)` 里提到
            //         `RevokeMatchLocal` 顶上（= 无条件记）⇒ **这条立刻红、M⑩-b 照样绿**。
            //      ⚠️ 这一步要一个**干净的掉线边沿**（`_peerGone` 得是清的）⇒ 先把对面接回来，
            //         跑完再接回来一次 —— ③ 起手要的正是「两边都在 `Lobby`」（与 M⑰ 那两句同形）。
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 6000),
               "M⑭-b 夹具：对面先接回来（下面那条要一个干净的掉线边沿）");
            rt.AttachForTest(host);
            NetMatchmaking.PumpLobby();              // 吃掉「回来了」那条边沿
            var noMatchLogs = new List<string>();
            Application.LogCallback hNoMatch = (string m, string st, LogType ty) => noMatchLogs.Add(m);
            cli.Transport.ClosePeer();
            Application.logMessageReceived += hNoMatch;
            try { PumpUntil(host, cli, () => host.State == NetState.WaitingReconnect, 6000); }
            finally { Application.logMessageReceived -= hNoMatch; }
            Ok(!noMatchLogs.Exists(m => m != null && m.Contains("[BattleResult]")),
               "M⑭-c ★★ **本机没在匹配这一局时对面掉了 ⇒ 一条码都不许记**"
             + "（没有「局」可没 —— `A1297②` 那个 `had` 闸）"
             + $"（实得 {noMatchLogs.Count} 条日志、带 token 的 "
             + $"{noMatchLogs.FindAll(m => m != null && m.Contains("[BattleResult]")).Count} 条）"
             + "；⚠️ 这一格**不是空转**：下面 M⑭-d 断的是**同一跳真跑了**"
             + "（`LastHint` 换成了 `NotMatchingThisGame` 那个 tail）⇒ 边沿确实走到了 `RevokeMatchLocal`");
            Ok(NetMatchmaking.LastHint != null
               && NetMatchmaking.LastHint.Contains(Loc.T("Settings/Online/Lobby/NotMatchingThisGame"))
               && !NetMatchmaking.LastHint.Contains(Loc.T("Settings/Online/Lobby/MatchRevoked")),
               $"M⑭-d …而且那行话如实说「本机本来就没在匹配这一局」、**不是**「这一局的匹配已经撤销」"
             + $"（实得「{NetMatchmaking.LastHint}」）—— 与 M⑭-c **不同源**：那条看「码记没记」，"
             + "这条看「话说得对不对」（两个 `tail` 的中文互不为子串 ⇒ 有鉴别力）");
            NetRuntime.DrainNoticesForTest();
            cli.CheckConnection(Cfg(port, ""));      // 接回来 —— ③ 起手要「两边都在 `Lobby`」
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 6000),
               "M⑭-e 夹具：又接回来了（③ 起手的状态就摆回原样）");
            rt.AttachForTest(host);
            NetMatchmaking.PumpLobby();              // 吃掉「回来了」那条边沿

            // ---- ③ 🔴 **对局中（大厅这一半已经交权）⇒ 一句都不许说** ----
            //   这是本件 ③ 那条要求（一台会话不许弹两次）：判据 = `NetRuntime.LobbyHandled`
            //   —— `NetBattle.Attach` 那一刻把它置 false **正是在干这件事**（对局那半边接管会话）。
            NetRuntime.DrainNoticesForTest();
            NetMatchmaking.Reset();
            rt.LobbyHandled = false;                 // = 进了对局（对局那一侧的断言在 `NetBattleTest` §9/§11）
            // 🆕 2026-10-11（`A1297②`）：**闸挡住时不光不许出声，也不许记码** —— 那一跳压根没进
            //   `RevokeMatchLocal`（这一格同时 `had == false`：上面 `Reset()` 把这一局的账清了）
            //   ⇒ 两条闸都该拦住，⛔ 一条 `[BattleResult]` 都不许有。
            var inBattleLogs = new List<string>();
            Application.LogCallback hInBattle = (string m, string st, LogType ty) => inBattleLogs.Add(m);
            cli.Transport.ClosePeer();
            Application.logMessageReceived += hInBattle;
            try { PumpUntil(host, cli, () => host.State == NetState.WaitingReconnect, 6000); }
            finally { Application.logMessageReceived -= hInBattle; }
            Ok(host.State == NetState.WaitingReconnect,
               $"（对局中·模拟）主机又发现对面没了（实际 {host.State}）");
            Eq(NetRuntime.DrainNoticesForTest().Length, 0,
               "M⑮ ★★ **对局中大厅这一半一个字都不说**（`LobbyHandled == false`）"
             + " —— 🧨 改坏法：把 `NetMatchmaking.LobbyOwnsSession` 那道闸去掉 ⇒ 这一条红，"
             + "而且**真机上玩家一局会被弹两次**（对局那半边还会再弹一条）");
            Ok(!NetMatchmaking.PeerGone, "M⑯ …而且也不许把那个边沿置上（没说话就没得撤）");
            Ok(!inBattleLogs.Exists(m => m != null && m.Contains("[BattleResult]")),
               "M⑯-b ★ **闸挡住时也一条码都不许记**（那一跳没进 `RevokeMatchLocal`）"
             + $"（实得 {inBattleLogs.Count} 条日志、带 token 的 "
             + $"{inBattleLogs.FindAll(m => m != null && m.Contains("[BattleResult]")).Count} 条）"
             + " —— 🧨 改坏法：把记码从 `RevokeMatchLocal` 里挪到闸**之前** ⇒ 红");

            // ---- ④ 对面**主动离开**（`bye`）⇒ 也要出声，而且带着他报的理由 ----
            rt.LobbyHandled = true;
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 6000),
               "M⑰ 对面又回来了（为了验 `bye` 那一路）");
            rt.AttachForTest(host);
            NetMatchmaking.PumpLobby();              // 把「回来了」那条边沿吃掉
            NetMatchmaking.Reset();
            Ok(NetMatchmaking.TryStart(d1, "Classic", "Ultramarines", out string _) && NetMatchmaking.Waiting,
               "M⑱ 主机重新点了 `Battle!`（这一局又在匹配里了）");
            NetRuntime.DrainNoticesForTest();
            // 🆕 2026-10-11（`A1297②`）：**「主动离开」与「掉线」是两条回调**（`OnClosed` vs `OnPeerLost`）
            //   ⇒ 两条都得各记一档码（同 §L 那种抓法：**日志流**）。
            var byeLogs = new List<string>();
            Application.LogCallback hBye = (string m, string st, LogType ty) => byeLogs.Add(m);
            cli.Close(true, "自检：对面离开了这一局");   // = 对面的 `NetRuntime.Reset` / 关台那条路
            bool closed;
            Application.logMessageReceived += hBye;
            try { closed = PumpUntil(host, cli, () => host.State == NetState.Closed, 6000); }
            finally { Application.logMessageReceived -= hBye; }
            Ok(closed,
               $"（大厅·离开）主机收到 `bye` ⇒ 会话关上（实际 {host.State}）");
            var n2 = NetRuntime.DrainNoticesForTest();
            Eq(n2.Length, 1, "M⑲ ★ **大厅阶段对面主动离开也要弹一条**（原来同样静默）"
                           + " —— 与掉线那条是**两个不同的回调**（`OnClosed` vs `OnPeerLost`），别合成一条");
            Ok(n2.Length >= 1 && n2[0].Contains("自检：对面离开了这一局"),
               $"M⑳ ★ 那条话里**带着对面报的理由**（实得「{(n2.Length > 0 ? n2[0] : "")}」）");
            Ok(!NetMatchmaking.Waiting, "M㉑ ★ 这一局的匹配也撤掉了（`bye` 这一路同一条落地）");
            Ok(byeLogs.Exists(m => m != null && m.Contains("[BattleResult] Disconnect(3)")),
               "M㉑-b ★★ **`bye`（主动离开）这一路也记了同一档码**（`[BattleResult] Disconnect(3)` —— `A1297②`）"
             + $"（实得 {byeLogs.Count} 条日志）"
             + " —— 🧨 改坏法：把 `RevokeMatchLocal` 里那句删掉 ⇒ 与 M⑩-b **一起红**"
             + "（两条回调走的是同一个出口 ⇒ 一条断言就能管住两条路，别在 `HandleLobbyPeer*` 里各写一份）");
            Ok(byeLogs.Exists(m =>
               {
                   if (m == null) return false;
                   BattleResult rr; int ss;
                   return ReplayStore.TryParseResultLine(m, out rr, out ss)
                       && rr == BattleResult.Disconnect && ss == -1;
               }),
               "M㉑-c ★ …而且那一行**能被录像侧同一个解析器读回来**（码 = `Disconnect`、座位 = `-1`）"
             + " —— 与 M㉑-b **不同源**：那条比字面，这条比「匹配层写的那一行」与「录像层读的那一行」是不是一套");
        }
        finally
        {
            rt.AttachForTest(keep);
            rt.LobbyHandled = keepLobby;
            NetMatchmaking.Reset();
            NetRuntime.DrainNoticesForTest();
            host.Close(false); cli.Close(false);
        }
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

    // ==================================================================
    //  N. 🆕 2026-10-17（B27·A925）：**「提示行」那行字的消费方**（原来只留了口、没人接）
    // ==================================================================
    /// <summary>账 `A925`：`NetMatchmaking.LastHint` / `OnHint` 是 B23 留好的口，但**一个消费方都没有**
    /// ⇒ 大厅阶段那几句人话（对面掉线 / 离开 / 回来）只活在日志与自检里，玩家看得到的**只有弹窗**。
    ///
    /// <para>🔴 **判据（原版，2026-10-17 现读；全文 → `Shell/SearchingMatchPopup.ShowHint` 上头那一节）**：
    /// 那一族窗口里**唯一一行**放得下状态的是本窗（四扇战斗入口窗共用的 `Searching Oponent Popup`）的
    /// `Main Search message`；原版**从不改这行字**（它由打字机独占），而「人少」那行 **5/5 份 prefab
    /// 都关着**（`useFewPlayerMessage = 0`）、出错走的是**弹窗**
    /// ⇒ 🔴 **把提示接在这一行上是【我们自己的口径】**（铁律 3，如实标）。</para>
    ///
    /// <para>⚠️ **本自检的宿主是纯逻辑的**（`NetSelfTest` 一行 UI 都不建）⇒ N⑤–N⑧ 那几条**要真建一扇窗**
    /// （`SearchingMatchPopup`）。建不出来时**如实说、不当失败**（同 `NetBattleTest.NoteProbeThrow` 那条先例）：
    /// 那说明这一格该挪到 `ShellScene` / `MainMenuScene` 那种宿主去验（它们本来就建这扇窗）。
    /// ⚠️ 这里靠 `AttachForTest` 换会话依次扮演两端（同 `TestMatchCancel` / `TestLobbyPeerGone`）。</para></summary>
    static void TestHintLine(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        var rt = NetRuntime.Ensure();
        var keep = rt.Session;
        bool keepLobby = rt.LobbyHandled;
        var seen = new List<string>();
        System.Action<string> probe = s => seen.Add(s);
        GameObject go = null;
        try
        {
            Ok(host.StartHost(Cfg(port, "")), "N① 主机起来了");
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
               "N② 两边握手到 `Lobby`");

            NetMatchmaking.Reset();
            rt.LobbyHandled = true;              // 夹具前提 = **大厅阶段**（对局那一半还没接管这台会话）
            rt.AttachForTest(host);
            var d1 = new PlayerDeck { Name = "自检提示行牌", WarlordId = "UM_WARLORD" };
            d1.CardIds.Add("UM1");
            Ok(NetMatchmaking.TryStart(d1, "Classic", "Ultramarines", out string _),
               "N③ 主机点 `Battle!` ⇒ 进匹配（下面那条提示才有「这一局的匹配已撤销」那半句）");

            // ---- N④：口是活的（订阅了就会响）----
            NetMatchmaking.OnHint += probe;
            seen.Clear();
            cli.Transport.ClosePeer();
            Ok(PumpUntil(host, cli, () => host.State == NetState.WaitingReconnect, 6000),
               $"（提示行）主机发现对面没了（实际 {host.State}）");
            Ok(seen.Count >= 1 && seen[seen.Count - 1] == NetMatchmaking.LastHint
               && NetMatchmaking.LastHint != null
               && NetMatchmaking.LastHint.Contains(TermHead("Settings/Online/Lobby/PeerLostHint")),
               $"★（提示行）N④ **那个口是活的**：`OnHint` 推来的那句与 `LastHint` 逐字相同、"
             + "且点明是掉线（断 `Settings/Online/Lobby/PeerLostHint` 到 `{0}` 为止那半句，随语档）"
             + $"（实得「{(seen.Count > 0 ? seen[seen.Count - 1] : "<没响>")}」）");

            // ---- N⑤–N⑧：**界面的消费方**（`SearchingMatchPopup` 那一行）----
            //   建不出来 ⇒ **如实说、不当失败**（见方法头上那一句）。
            Exception buildErr = null;
            SearchingMatchPopup pop = null;
            go = new GameObject("B27_HintLineProbe");
            try { pop = SearchingMatchPopup.Attach(go.transform, "Searching Oponent Popup"); }
            catch (Exception e) { buildErr = e; }

            if (pop == null)
            {
                _warn++;
                Debug.LogWarning("[NetSelfTest] ⚠️ N⑤–N⑧ **没验到**：本宿主建不出 UI 窗口（`SearchingMatchPopup`）—— "
                               + (buildErr != null ? (buildErr.GetType().Name + "：" + buildErr.Message) : "返回了 null")
                               + "。**这不是「通过」也不是「失败」** ⇒ 这一格要挪到 `ShellScene` / `MainMenuScene` "
                               + "那种宿主去验（它们本来就建这扇窗）。");
            }
            else
            {
                int subs0 = HintSubs();
                pop.Show();
                // 🔴 **2026-10-17（F4·RC3）：这一格原来是 N⑤「`Show()` ⇒ **消费方挂上了**（订阅者 +1）」。**
                //   判据链（→ `资料/普查产出_1017/D4_联机诊断.md` §③）：窗的订/摘**只挂在
                //   `OnEnable`/`OnDisable`**（`Shell/SearchingMatchPopup.cs:438-440` —— 设计上是对的：
                //   `Show()` 会重复调，挂 `Show()` 会订两次），而本自检跑在**编辑模式**
                //   ⇒ **编辑器不派生命周期消息** ⇒ `Attach()`（`AddComponent`+`SetActive(false)`）与
                //   `Show()`（`SetActive(true)`）**两条路都一次都不派** ⇒ 窗从来没订上
                //   （本轮日志实证：N⑤「订阅者 **1 → 1**」，那 1 就是 N④ 自己挂的探针）。
                //   ⛔ **不写成绿、也不许删**：本宿主这半分**改断「环境事实」**（订阅者数**原封不动**）+ 出声；
                //   🔴 **「`OnEnable` 自动订 ⇒ `OnDisable` 自动退」那一跳只有真 Play 跑得到** ⇒
                //      `资料/真Play待验清单.md` **D43**。可验的那半（提示画得出 / 收得回）在下面 ——
                //      改成**直接走窗自己的公开口** `ShowHint(...)`。
                _warn++;
                Debug.LogWarning("[NetSelfTest] ⚠️ N⑤ **本宿主里验不了「`OnEnable` 自动订」那一跳**："
                               + "编辑模式（`Application.isPlaying == false`）不派生命周期消息 ⇒ "
                               + "`Attach()` / `Show()` 两条路都不派 `OnEnable`。这一格改断**环境事实**"
                               + "（订阅者数原封不动）；真那一跳 = 真 Play ⇒ `资料/真Play待验清单.md` D43。");
                Ok(HintSubs() == subs0,
                   $"★（提示行）N⑤【本宿主的环境事实】`Show()` **不会**把消费方挂上（订阅者 {subs0} → {HintSubs()}）"
                 + " —— 编辑模式不派 `OnEnable`（`SearchingMatchPopup.cs:438`）⇒ 这一跳在本宿主里验不了"
                 + "（真那一跳 = 真 Play，D43）"
                 + "；🧨 改坏法：给窗加 `[ExecuteAlways]`（或改由 `Show()` 订）⇒ 订阅者 +1 ⇒ 本条红"
                 + " —— 那是「环境变了、好消息」，把这一档换成「订阅者 +1」那条断言即可");
                var line = FindLabel(pop.transform, "Main Search message");
                Ok(line != null, "（提示行）夹具：那一行字（`Main Search message`）建出来了");
                if (line != null)
                {
                    Ok(line.Text == SearchingMatchPopup.SearchingText.Substring(0, SearchingMatchPopup.SearchingText.Length - 3),
                       $"（提示行）夹具：起手是原版那句打字机的前缀（实得「{line.Text}」）");
                    // 提示是**掉线那一刻**推的，而这一扇窗是**之后**才开的 ⇒ 手动把那一句再推一遍
                    //（真 Play 里顺序正是「窗开着 → 掉线」；这里为了同一条用例里把 N④ 与 N⑤ 都验到，先后换了位置）。
                    // 🔴 本宿主里**推给谁**：生产路径是「`OnHint` 推 ⇒ 窗在 `OnEnable` 订的那根线收到」，
                    //    而这条投递在本宿主里不存在（见上）⇒ 这里**直接调窗自己的公开口**（同一个方法体）。
                    string hint = NetMatchmaking.LastHint;
                    pop.ShowHint(hint);
                    Ok(line.Text == hint,
                       $"★（提示行）N⑥ **提示落到了那一行字上**（实得「{line.Text}」）"
                     + " —— 🧨 改坏法：删掉 `ShowHint` 里那句 `_msg.SetText(text)` ⇒ 红");
                    pop.Tick(2f);
                    Ok(line.Text == hint,
                       $"★（提示行）N⑦ **打字机不许把提示顶掉**（推 2 秒之后那行字还是提示：「{line.Text}」）"
                     + " —— 🧨 改坏法：把 `Tick` 里 `_hint == null` 那道闸去掉 ⇒ 红"
                     + "（那一格正是「玩家正要读的那句话每 0.5 秒被顶回 `Sear…`」）");
                    // 那一行**收回去** ⇒ 打字机接着打（生产路径那个口是 `NetMatchmaking.Reset()`
                    // ⇒ 它推 `OnHint(null)`；本宿主里那一投递同上不存在 ⇒ 直接把同一条边沿交给窗）。
                    NetMatchmaking.Reset();
                    pop.ShowHint(null);
                    for (int i = 0; i < 8 && line.Text == hint; i++) pop.Tick(0.5f);
                    Ok(pop.HintText == null && line.Text != hint && line.Text != null && line.Text.Length > 0,
                       $"★（提示行）N⑧ `Reset()`（⇒ `OnHint(null)`）⇒ **那行字收回去、打字机接着打**（实得「{line.Text}」）"
                     + " —— 与 N⑥ **不同源**：N⑥ 验「来了会画」，这条验「走了会收」（不收的话下一局开局时"
                     + "台面上还挂着上一局那句「对面掉线了…」——说错话 = 另一种静默）");
                }
                // ---- N⑨：`Hide()` / 销毁 ⇒ **窗自己把那根线摘掉** ----
                //   🔴 **2026-10-17（F4·RC3）**：这一格原来是**恒真绿** —— 它先手动 `OnHint -= probe`、
                //     再断言「订阅者数 = 0」⇒ `0 == 0` **与窗有没有自己摘无关**（「灭自证」那一族）。
                //   ⇒ 改成**有鉴别力**的写法：① 本宿主不派生命周期 ⇒ 由自检**替它把那次派发补上**
                //     （手动 `+= pop.ShowHint` == `OnEnable` 里那一句；再用反射调私有的 `OnDisable` /
                //     `OnDestroy` == 编辑器本该派的那两条消息）；② 断言**订阅者数真的掉回去**。
                //   ⚠️ **只覆盖「窗自己那句 `-=` 干了活」**；「Unity 会派这两条消息」那一跳仍是真 Play（D43）。
                //   🧨 判别式：删掉 `SearchingMatchPopup.OnDisable`（或 `OnDestroy`）里那句 `-=` ⇒ 本条红。
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    var miDisable = typeof(SearchingMatchPopup).GetMethod("OnDisable", flags);
                    var miDestroy = typeof(SearchingMatchPopup).GetMethod("OnDestroy", flags);
                    if (miDisable == null || miDestroy == null)
                    {
                        _warn++;
                        Debug.LogWarning("[NetSelfTest] ⚠️ N⑨ **没验到**：拿不到 `SearchingMatchPopup.OnDisable` / "
                                       + "`OnDestroy`（被改名了？）—— 本格在本宿主里不可验（真那一跳 = 真 Play，D43）。");
                    }
                    else
                    {
                        int subsN9 = HintSubs();                       // = 只有 N④ 那根探针
                        NetMatchmaking.OnHint += pop.ShowHint;          // = `OnEnable` 那一句（本宿主不派 ⇒ 替它补）
                        Eq(HintSubs(), subsN9 + 1, "（提示行）N⑨ 夹具：补上 `OnEnable` 那句 `+=` ⇒ 多一根（窗）");
                        pop.Hide();
                        miDisable.Invoke(pop, null);                    // = 编辑器本该派的 `OnDisable`
                        Eq(HintSubs(), subsN9,
                           "★（提示行）N⑨a `OnDisable`（`Hide()` 那一刻）里那句 `-=` **真把那根线摘了**"
                         + " —— 🧨 改坏法：删掉 `SearchingMatchPopup.OnDisable` 里那句 `-=` ⇒ 红"
                         + "（不摘的话，窗一销毁提示再来就是 `MissingReferenceException`）");
                        NetMatchmaking.OnHint += pop.ShowHint;          // 再挂一次，验销毁那一条（`OnDestroy` 也得摘）
                        miDestroy.Invoke(pop, null);                    // = 编辑器本该派的 `OnDestroy`
                        Eq(HintSubs(), subsN9,
                           "★（提示行）N⑨b `OnDestroy`（窗被销毁那一刻）里那句 `-=` **也摘了**"
                         + " —— 与 N⑨a **不同源**：一条管「关窗」、一条管「销毁」，两处都得摘"
                         + "；🧨 改坏法：删掉 `SearchingMatchPopup.OnDestroy` 里那句 `-=` ⇒ 红");
                    }
                }
            }
        }
        finally
        {
            NetMatchmaking.OnHint -= probe;
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            rt.AttachForTest(keep);
            rt.LobbyHandled = keepLobby;
            NetMatchmaking.Reset();
            NetRuntime.DrainNoticesForTest();
            host.Close(false); cli.Close(false);
        }
    }

    // ==================================================================
    //  O2. 🆕 2026-10-18（A932）：排位那条路（全屏 `SearchingOpponentWindow`）的**提示行**
    // ==================================================================
    /// <summary>账 `A932`：排位走的是**全屏** `Shell/SearchingOpponentWindow.cs`，而它原来**一行放得下
    /// 状态话的节点都没有**（只有 `Title` · 两个 `Player Name` · 一颗 `Cancel Match`）⇒ 大厅阶段那几句提示
    /// （对面掉线 / 离开 / 回来）在那一扇窗上**一个字都看不见**（屏幕上在说假话 = 另一种静默）。
    ///
    /// <para>🔴 **判据 = 用户 2026-10-18 拍板「接入」，而原版没有这颗节点** ⇒ 按铁律 3 如实标成
    /// 「**我们自建的**」：几何（700×148 · `Title` 正下方 · 水平居中）与样式（照同族
    /// `SearchingMatchPopup` 的 `Main Search message`）**全是我们挑的** —— 全文 → 那扇窗类头那一段。</para>
    ///
    /// <para>🔴 **对端文本那一支（A961）**：那句话里可能夹着对端发来的文本（`MsgBye.reason` 那一支，
    /// `NetMatchmaking.HandleLobbyPeerClosed(why)` 把 `why` 拼进提示），而它**在源头就钳过**
    /// （`NetSession.ClampPeerText` 是**一处闸**、三个收包入口都在 `NetSession`）⇒ 本节点**不写第二份钳**。
    /// 下面 **A932⑤** 把「**本节点不做第二道钳**」钉成断言（推一句夹着 60 字「对端文本」的提示，看它原样照收）
    /// —— ⚠️ 那一条会**顺带触发** `ShowHint` 那条「超过 **80 个半宽字位**」（判据 = `HintLineWidth(句) >`
    /// `HintLineMaxWidth`，⛔ 不再是「超过 40 字」—— 那个 40 是 `A1083` 换尺子**之前**的口径）
    /// 的告警，**那是预期的**（它在出声，不是失败：
    /// 「这行按框放不下」与「钳对端文本」本来就是两件事，见那扇窗类头那一节）。</para>
    ///
    /// <para>⚠️ **本宿主是纯逻辑的**（`NetSelfTest` 一行 UI 都不建）⇒ 与 N⑤ 同一条先例：
    /// **建不出来 ⇒ 如实说、不当失败**（那说明这一格该挪到 `ShellScene` 那种宿主去验）。
    /// ⚠️ 编辑模式**不派生命周期消息** ⇒ 「`OnEnable` 自动订」那一跳在本宿主验不了（同 N⑤ / 真 Play **D43**）：
    /// 可验的那半 = **直接走窗自己的公开口** `ShowHint(...)` / `ShowHint(null)`。</para></summary>
    static void TestOpponentHintA932()
    {
        // 🔴 **基线要在建窗【之前】取** —— 取在建窗之后就成了一句同义反复（`HintSubs() == HintSubs()`）。
        int subsBefore = HintSubs();
        GameObject go = null;
        SearchingOpponentWindow win = null;
        Exception buildErr = null;
        try
        {
            go = new GameObject("A932_OppHintProbe");
            win = go.AddComponent<SearchingOpponentWindow>();
            win.Open();                      // = 生产那条路（`WindowsManager.OpenWindow` 会调它）；`Open()` 里就 `Build()`
        }
        catch (Exception e) { buildErr = e; win = null; }

        try
        {
            if (win == null)
            {
                _warn++;
                Debug.LogWarning("[NetSelfTest] ⚠️ A932 **没验到**：本宿主建不出 `SearchingOpponentWindow` —— "
                               + (buildErr != null ? (buildErr.GetType().Name + "：" + buildErr.Message) : "返回了 null")
                               + "。**这不是「通过」也不是「失败」** ⇒ 这一格该挪到 `ShellScene` 那种宿主去验"
                               + "（它本来就建这扇窗）。");
                return;
            }

            var line = FindLabel(win.transform, "Hint Line");
            Ok(line != null, "A932① 夹具：那颗**提示行**节点（`Hint Line`）建出来了"
                           + "｜🧨 改坏法：删掉 `Build()` 里那颗 `MenuDraw.Text(… \"Hint Line\" …)` ⇒ 红");
            if (line == null) return;

            Ok(string.IsNullOrEmpty(line.Text) && string.IsNullOrEmpty(win.HintText),
               $"A932② 出厂那行是**空的**（还没有任何提示；实得「{line.Text}」）"
             + "｜🧨 改坏法：给它编一句出厂文案（或让 `Build()` 不清 `HintText`）⇒ 红");

            // ---- ③ / ④：**提示画得出、收得回**（走窗自己的公开口，同 N⑧ 那条口径）----
            string hint = "对面掉线了，这一局的匹配已经撤销（两边回来各点一次 Battle!）";
            win.ShowHint(hint);
            Ok(line.Text == hint && win.HintText == hint,
               $"★ A932③ **提示落到了那一行上**（实得「{line.Text}」）"
             + "｜🧨 改坏法：删掉 `SearchingOpponentWindow.ShowHint` 里那句 `_hintLine.SetText(text)` ⇒ 红");

            win.ShowHint(null);
            Ok(string.IsNullOrEmpty(line.Text) && win.HintText == null,
               $"★ A932④ `ShowHint(null)`（= `NetMatchmaking.Reset()` 推 `OnHint(null)` 那一支）⇒ **那行收回去**"
             + $"（实得「{line.Text}」）—— 与 ③ **不同源**：③ 验「来了会画」，这条验「走了会收」"
             + "（不收 ⇒ 下一局开局时台面上还挂着上一局那句「对面掉线了…」= 说错话）"
             + "｜🧨 改坏法：删掉 `ClearHint()` 里那句 `SetText(\"\")` ⇒ 红");

            // ---- ⑤：**对端文本那一支**（A961）—— 钳只在 `NetSession` 一处，本节点**不补第二份** ----
            //   🔴 这一条**不是**在验「钳没钳」（那是 `NetSession` 的账，A961 另有一套自检）；
            //      它验的是**本节点不做第二道钳** —— 一句 60 字的「对端文本」推上去要**原样**落在那一行上。
            //      （第一道钳在源头已经做过 ⇒ 真跑到这里时字符串本就 ≤ `MaxPeerTextChars`。）
            string peerish = new string('长', NetProtocol.MaxPeerTextChars + 20);
            string withPeer = "联机结束：" + peerish + " —— 这一局的匹配已经撤销";
            win.ShowHint(withPeer);
            Ok(line.Text == withPeer,
               $"★ A932⑤ 提示行**原样照收**（不在本节点做第二道钳）：推 {withPeer.Length} 字 ⇒ 那行拿到 {line.Text.Length} 字"
             + $"｜🧨 改坏法：在 `ShowHint` 里加一句 `NetSession.ClampPeerText(text)` ⇒ 被截成 "
             + $"{NetProtocol.MaxPeerTextChars + 1} 字 ⇒ 红（对端文本的钳**只在 `NetSession` 一处**，⛔ 别抄第二份 —— "
             + "那会把自己写的中文提示也一起截掉）");
            win.ShowHint(null);

            // ---- ⑥：**环境事实**（同 N⑤）——编辑模式不派 `OnEnable` ⇒ 「开窗自动订」那一跳验不了 ----
            //   ⚠️ 基线 `subsBefore` 是**建窗之前**取的（见方法开头那道注释）—— 取在之后 = 同义反复。
            Ok(HintSubs() == subsBefore,
               $"★ A932⑥【本宿主的环境事实】`Open()` **不会**把本窗挂到 `OnHint` 上（订阅者 {subsBefore} → {HintSubs()}）"
             + " —— 编辑模式不派 `OnEnable`（`SearchingOpponentWindow.OnEnable`）⇒ 这一跳在本宿主里验不了"
             + "（真那一跳 = 真 Play，**D43**）"
             + "；🧨 改坏法：给窗加 `[ExecuteAlways]`（或改由 `Open()` 订）⇒ 订阅者 +1 ⇒ 本条红"
             + "（那是「环境变了、好消息」，把这档换成「订阅者 +1」那条断言即可）");
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==================================================================
    //  O. 🆕 2026-10-18（A943）：`TcpTransport` 的两个连接事实**由同一次写发布**
    // ==================================================================
    /// <summary>账 `A943`：`Setup` 原来先 `_connected = true;`（`NetTransport.cs:219`）**再**
    /// `AcceptedCount++`（`:220`）—— 两条**独立的**写。写它的是**接受线程**（`AcceptLoop` → `Setup`），
    /// 读它的是**主线程**（`NetSession.Pump`，每帧 / 自检里显式调）⇒ 存在一个
    /// 「**已连接、但计数还没加**」的中间态：读侧若在 `Pump:213` 读到旧计数（⇒ 不刷新静默基线）、
    /// 又在 `Pump:232` 读到连接位已立（⇒ 拿 `now - _lastRecvMs >= SilentTimeoutMs` 去判）
    /// ⇒ **把一条刚连上、一条包都还没发的连接掐掉**。
    ///
    /// <para>现在两个事实**同处一个 32 位字、由一次 CAS 一起翻**（`TcpTransport._state`：
    /// bit0 = 连接位、bit1..31 = 接受计数）⇒ **不存在半次发布**。
    /// 🔴 **顺序不能反**（把 `AcceptedCount++` 挪到前面**更糟**）：上层是**边沿触发**
    /// （`Pump:213-215` 看到计数变了就 `_lastAccepted = …` 并**立刻**发 `Challenge`），
    /// 那一刻 `Send` 会看到「还没连上」⇒ **握手包发不出去、边沿却被消费掉了** ⇒ 永久握不上。</para>
    ///
    /// <para>🔴 **为什么这条断言是【结构性】的**（反射读那个私有状态字）：那个中间态只有
    /// 「接受线程正好把两条写插在读侧两次读之间」才可观察，**量级是纳秒 ⇒ 自检里跑一万次也撞不上**；
    /// 写成行为断言只会是一条**永远绿**的假断言（比没有更糟）。所以这里钉的是**不变量本身** ——
    /// 连接位与计数是**同一个字**的两个位段。
    /// ⚠️ 这不是「测实现细节」：「两个事实一致」在本工程里**只可能**由「一次写」实现
    /// （读侧是两次读，任何两条独立的写都能被夹住）。
    /// 🧨 改坏法：把两件事拆回两个字段（或让 `IsConnected` 改去读别的东西）⇒ **N④/N⑤ 红**。</para></summary>
    static void TestStatePublishA943(int port)
    {
        var f = typeof(TcpTransport).GetField("_state",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Ok(f != null && f.FieldType == typeof(int),
           "N① `TcpTransport` 里有那个状态字 `_state`（int）—— 没有它就谈不上「两个事实同处一字」");
        if (f == null || f.FieldType != typeof(int)) return;   // 拿不到就到此为止（下面全靠它）

        var t = new TcpTransport();            // 只看那两个属性，**不开台、不连** ⇒ 零副作用
        Eq(t.AcceptedCount, 0, "N② 全新传输：接受计数 0");
        Ok(!t.IsConnected, "N③ 全新传输：连接位是 0");

        f.SetValue(t, (3 << 1) | 1);           // 状态字 = (计数 << 1) | 连接位
        Ok(t.IsConnected, "N④ 字里连接位是 1 ⇒ `IsConnected` 真（它**只**看这个字）");
        Eq(t.AcceptedCount, 3, "N⑤ 同一个字里的计数字段 = 3");

        f.SetValue(t, (3 << 1));               // 只清连接位
        Ok(!t.IsConnected, "N⑥ 清掉连接位 ⇒ `IsConnected` 假");
        Eq(t.AcceptedCount, 3, "N⑦ 而计数**不动**（它是「累计接受过几条」，不是「现在有几条」）");

        f.SetValue(t, (4 << 1) | 1);
        Eq(t.AcceptedCount, 4, "N⑧ 计数落在 bit1..31 —— 换个数值也跟着走");

        // ---- 再跑真的 socket：两个事实一起动，掉线时**只清连接位** ----
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        Ok(host.StartHost(Cfg(port, "")), "N⑨ 主机起来了");
        Eq(host.Transport.AcceptedCount, 0, "N⑩ 开台那一刻：计数还是 0");
        cli.CheckConnection(Cfg(port, ""));
        Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
           $"N⑪ 握手通（主机 {host.State} / 客机 {cli.State}）");
        Eq(host.Transport.AcceptedCount, 1, "N⑫ 接了一条 ⇒ 计数 1");
        Ok(host.Transport.IsConnected, "N⑬ 而且连接位立着 —— 同一个字里一起翻的");

        cli.Transport.ClosePeer();
        Ok(PumpUntil(host, cli, () => !host.Transport.IsConnected, 4000), "N⑭ 对面掐断 ⇒ 连接位清掉");
        Eq(host.Transport.AcceptedCount, 1, "N⑮ 掉线**不动**计数");
        cli.Close(false);     // 停掉这一头（它进不了自动重连：没 `EnterBattle` ⇒ `_wasInBattle` 是假）

        var cli2 = NetSession.NewTcp();
        cli2.CheckConnection(Cfg(port, ""));
        Ok(PumpUntil(host, cli2, () => host.Transport.AcceptedCount == 2, 5000),
           $"N⑯ 再连一条 ⇒ 计数 2（实际 {host.Transport.AcceptedCount}）");
        Ok(host.Transport.IsConnected, "N⑰ 连接位又立起来");
        host.Close(false); cli2.Close(false);
    }

    // ==================================================================
    //  P. 🆕 2026-10-18（A943 读侧）：静默超时判据**不许把连接位与计数分两次读下结论**
    // ==================================================================
    /// <summary>账 `A943` 的**读侧闭环**：`NetSession.Pump` 里那个判据是**两次读**
    /// （`:213` 读计数、`:247` 读连接位）。写侧虽然已经「一个字 + 一次 CAS」（§O），
    /// 但一次新发布**夹在这两次读之间**时，判据仍会凑出「**旧计数**（⇒ 上面那支没走、`_lastRecvMs`
    /// 没跟着刷新）**+ 新连接位**（⇒ 真）」⇒ **掐掉一条刚连上、一条包都还没发的好连接**。
    /// `:247` 补了「`Role != NetRole.Host ||` + `_t.AcceptedCount == _lastAccepted`」之后，这种夹缝
    /// **必须判不出超时** —— 而**客机**那侧（确认对它不适用）的超时**必须照旧判**。
    ///
    /// <para>🔴 **为什么用可注入的传输**：那个窗口在真 socket 上**量级是纳秒**，跑一万次也撞不上
    /// —— 写成真实 socket 的行为断言只会是**永远绿**的假断言（比没有更糟）。这里用 `NetSession(INetTransport)`
    /// 那个公开构造口把 `AcceptedCount` 摆成脚本（**读一次给一个值**）⇒ 那个夹缝**每次都必现**。
    /// 两个**负例**（P⑨ 计数两次读一样 / P⑫ 客机）各挡一头：
    /// 前者挡「**永远不超时**」也能把 P③ 蒙绿，后者挡「确认把**客机**的超时永久关掉」
    /// —— 客机的 `_lastAccepted` 停在 `-1`、而它的计数是 **1**（`Setup` 在客机侧也跑）⇒ `1 == -1` 恒假。</para>
    /// 🧨 改坏法：删 `NetSession.cs:247` 里的 `_t.AcceptedCount == _lastAccepted` ⇒ **P④/P⑦ 红**
    ///   （⚠️ **2026-10-18 订正**：原写「P③/P④ 红」—— **本夹具下 P③/P⑥ 是假断言**：它们断 `ClosePeerCalls==0`，
    ///   而 `:247` **第一个合取项** `_t.IsConnected` 一假就短路 ⇒ 把那半句整段删掉它们照样绿。
    ///   这是「**假断言掩护真断言**」的样本 —— 已随夹具一起修，判据见 `ScriptedTransport.Listen` 那段注释）；
    /// 删同一行的 `Role != NetRole.Host ||` ⇒ **P⑫ 红**（那半边**确实**被测到了）。</summary>
    static void TestSilentTimeoutReadSideA943()
    {
        long clock = 0;
        NetSession.SetClockForTest(() => clock);       // 不真睡 10 秒（进程级，finally 里还回来）
        try
        {
            // ---- ① 夹缝：开台读一次计数（0）→ 第一 Pump 的第一次读还是 0（⇒ 不认边沿）、
            //         第二次读变成 1（⇒ 新连接位已立，而基线还是开台那一刻的）----
            var t1 = new ScriptedTransport { Connected = true, Counts = new[] { 0, 0, 1 } };
            var s1 = new NetSession(t1);
            Ok(s1.StartHost(Cfg(0, "x")), "P① 脚本传输：主机开得起来（`StartHost` 在这里读走第一次计数）");
            // 🔴 **夹具自检（2026-10-18 加）**：正是它缺失，才让 P⑦/P⑨/P⑩ 三条红拖到全套实跑那天才暴露 ——
            //    `StartHost` 内部 `Close()` 会把桩置成「没连上」，而主机此刻确实**在监听**（= 链路活着）。
            Ok(t1.IsConnected, "★ 夹具自检：`StartHost` 之后桩**仍是连着的**（不连 ⇒ Send 会静默丢、超时判据会短路）");
            clock = NetSession.SilentTimeoutMs + 5000;          // 开台设的那条基线这时**已经算超时**
            s1.Pump();
            Eq(t1.CountOf(NetKind.Challenge), 0,
               "P② 这一次 `Pump` 没发 `Challenge` ⇒ `:213` 那支确实没认到边沿（**夹缝成立**）");
            Eq(t1.ClosePeerCalls, 0, "P③ 计数在两次读之间变了 ⇒ **不许**拿旧基线掐这条连接");
            Ok(s1.State == NetState.Listening, $"P④ 连接还在、状态没被改成等重连（实际 {s1.State}）");

            // ---- ② 下一帧：边沿那支必须把这次连接补上（是「推迟一帧」，不是「永远不判」）----
            s1.Pump();
            Ok(s1.State == NetState.Handshaking, $"P⑤ 下一帧认到新连接 ⇒ 走握手（实际 {s1.State}）");
            Eq(t1.ClosePeerCalls, 0, "P⑥ 而且仍然没掐（`:213` 那支已把静默基线刷新）");
            Eq(t1.CountOf(NetKind.Challenge), 1, "P⑦ 而且 `Challenge` 真发出去了（边沿没被白白吃掉）");

            // ---- ③ 负例（灭自证）：计数两次读都一样 ⇒ 超时**照旧要判** ----
            var t2 = new ScriptedTransport { Connected = true, Counts = new[] { 0, 0, 0 } };
            var s2 = new NetSession(t2);
            clock = NetSession.SilentTimeoutMs * 2;             // 这台开台时基线也设在此刻
            Ok(s2.StartHost(Cfg(0, "x")), "P⑧ 负例：主机开得起来");
            Ok(t2.IsConnected, "★ 夹具自检（同 P① 那条）：这一台也仍是连着的");
            clock += NetSession.SilentTimeoutMs + 5000;         // 真的静默超过阈值
            s2.Pump();
            Eq(t2.ClosePeerCalls, 1, "P⑨ 真静默（计数两次读一样）⇒ **照旧判超时、照旧掐** —— 这条挡「永远不超时」");
            Ok(s2.State == NetState.WaitingReconnect, $"P⑩ 而且进了等重连（实际 {s2.State}）");

            // ---- ④ 负例（客机）：那道确认**只对主机成立** ----
            //   客机侧 `_lastAccepted` **没人写**（停在 -1），而它的计数是 **1**
            //   （`Setup` 在客机 `Connect()` 里也跑 ⇒ `Publish(bumpCount: true)`）
            //   ⇒ 少了 `Role != NetRole.Host ||` 这半句，客机的静默超时会被**永久关掉**。
            var t3 = new ScriptedTransport { Connected = true, Counts = new[] { 1 } };
            var s3 = new NetSession(t3);
            clock += NetSession.SilentTimeoutMs * 2;
            s3.CheckConnection(Cfg(0, "x"));
            Ok(s3.State == NetState.Handshaking, $"P⑪ 客机连上了（实际 {s3.State}）");
            clock += NetSession.SilentTimeoutMs + 5000;          // 对面一条包都没来过
            s3.Pump();
            Eq(t3.ClosePeerCalls, 1, "P⑫ 客机照样判静默超时（`_lastAccepted` 停在 -1 ⇒ 确认必须对客机放行）");
            Ok(s3.State == NetState.WaitingReconnect, $"P⑬ 而且进了等重连（实际 {s3.State}）");
        }
        finally { NetSession.SetClockForTest(null); }
    }

    // ==================================================================
    //  Q. 🆕 2026-10-18：**对端可控文本有上限**（A961）+ **丢包必须出声**（账：「`Send()` 静默丢」）
    // ==================================================================
    /// <summary>两笔一起钉（都在 `Net/NetSession.cs` 这一族、都**不用 socket** —— 走 `ScriptedTransport.Inject`）。
    ///
    /// <para>🔴 **`A961` 的口径**（主对话拍板）：**统一钳 + 截断**，收口在 `NetSession` 的**收包段**
    /// —— `MsgBye.reason` · `MsgProof.name`（→ `PeerName`）· `MsgAck.reason` **三个入口一起钳**
    /// ⇒ 一处盖住**提示行 / 弹窗 / `StatusText`** 三个消费方。
    /// ⚠️ **原版没有对端文本进界面的先例**（掉线文案是固定词条，玩家名的长度约束在 PlayFab 服务端）
    /// ⇒「截断」与「干脆不带对端理由」**都是我们的选择**；取值与出处 → `NetProtocol.MaxPeerTextChars`。</para>
    ///
    /// <para>🧨 **灭自证**：每一条都用**尾巴标记**判，不是用长度本身判 —— 那串 = 200 个 `A` + `TAIL-MARK`
    /// ⇒ **删掉 `NetSession.ClampPeerText`，或去掉任一入口的那一次调用，对应断言立刻红**。
    /// 同理 `Q⑨` 那一条抓的是**日志流**（`Application.logMessageReceived`，本仓 `Editor/BattleScene.cs`
    /// 那一族已经是这个抓法）⇒ 把 `Send` 那声 `LogWarning` 去掉就红（**「静默」时必红**，不是同义反复）。</para>
    /// 🆕 **2026-10-18 续（主对话裁定「接着做完」）**：`A961` 的**另两个同族入口**也补齐了 ——
    /// `MsgReject.reason`（→ `NetBattle.Abort` → 提示行 + 弹窗）与 `MsgStart.myName/foeName`
    /// （→ `NetPendingBattle.FromStart`）⇒ `Q⑪~Q⑭`（⑤ 那一段）。
    /// 🆕 **2026-10-18 再续（裁定「封口」）**：**玩家看得见的那条路**也补上了 ——
    /// `NetMatchmaking.FoeName`（`NetMatchmaking.cs:487` 的 `MsgStart.myName`）⇒ `Q⑮/Q⑯`（⑥ 那一段）。
    /// 🆕 **2026-10-18 第三续（独立审查 R5）**：`Close(say:true)` 发不出 `bye` 时的**残留静默**也补出声 ⇒ `Q⑰/Q⑱`。
    /// ⚠️ ①~⑤ 那五段**不建 socket、不碰端口**（桩 `Port` 恒 0 ⇒ `UpnpPortMapper` 第一句就返回）；
    /// **⑥ 那一段要两个真 socket**（它走的是大厅那条真路 `PumpLobby`），所以它接一个 `port` 参数
    /// ——与 `TestMatchCancel(port)` 同一套 `Cfg`/`FreePort` 用法。</summary>
    static void TestPeerTextClampA961(int port)
    {
        // 尾巴标记放在离上限很远的地方 —— 只有「真的钳过」才会看不到它。
        string huge = new string('A', 200) + "TAIL-MARK";
        const string TAIL = "TAIL-MARK";

        // ---- ① `MsgBye.reason`（对端可控）⇒ 状态字那一份 + `OnClosed` 那一份都要钳 ----
        {
            var t = new ScriptedTransport { Connected = true };
            var s = new NetSession(t);
            string told = null; s.OnClosed = w => told = w;
            t.Inject(NetKind.Bye, NetProtocol.Pack(new MsgBye { reason = huge }));
            s.Pump();
            Ok(s.StatusText != null && s.StatusText.Length <= NetProtocol.MaxPeerTextChars + 1,
               "Q① `bye` 带一条 200+ 字的理由 ⇒ 状态字**被钳住**"
             + $"（实得 {s.StatusText?.Length ?? -1} 字，上限 {NetProtocol.MaxPeerTextChars + 1}）"
             + " —— 上限取值与出处见 `NetProtocol.MaxPeerTextChars`");
            Ok(s.StatusText != null && !s.StatusText.Contains(TAIL),
               $"Q② ★ 对端塞的**尾巴那一段根本没进界面**（实得「{s.StatusText}」）"
             + " —— 🧨 改坏法：删掉 `NetSession.cs` 收 `Bye` 那一句的 `ClampPeerText(...)` ⇒ 红");
            Ok(told != null && !told.Contains(TAIL) && told.Length <= NetProtocol.MaxPeerTextChars + 1,
               "Q③ ★ 经 `OnClosed` 出去的那一份**也钳过**（`NetMatchmaking` 的提示行+弹窗、"
             + $"`NetBattle.HandlePeerClosed` 吃的都是它；实得 {told?.Length ?? -1} 字）"
             + " —— 与 Q② **不同源**：Q② 看 `StatusText`（设置窗联机页那行），这一条看回调那条路");
        }

        // ---- ② `MsgProof.name` → `PeerName`（对端可控，经 `StatusText` 进设置窗那一行）----
        {
            var t = new ScriptedTransport { Connected = true };
            var s = new NetSession(t);
            t.Inject(NetKind.Proof, NetProtocol.Pack(new MsgProof
            {
                protoVer = NetProtocol.Version, gameVer = "自检", name = huge, proof = "",
            }));
            s.Pump();
            Ok(s.PeerName != null && !s.PeerName.Contains(TAIL)
               && s.PeerName.Length <= NetProtocol.MaxPeerTextChars + 1,
               $"Q④ ★ 对面握手时报的**超长显示名**同样被钳（实得 {s.PeerName?.Length ?? -1} 字）"
             + " —— 🧨 改坏法：去掉 `PeerName = ClampPeerText(...)` 那一跳 ⇒ 红");
            Ok(s.StatusText != null && !s.StatusText.Contains(TAIL),
               $"Q⑤ ★ …而它在状态字里也**一个字都没漏出去**（实得「{s.StatusText}」）");
        }

        // ---- ③ `MsgAck.reason`（对端可控，经 `StatusText` + `OnClosed` 出去）----
        {
            var t = new ScriptedTransport { Connected = true };
            var s = new NetSession(t);
            string told = null; s.OnClosed = w => told = w;
            t.Inject(NetKind.Ack, NetProtocol.Pack(new MsgAck { ok = false, reason = huge }));
            s.Pump();
            Ok(s.StatusText != null && !s.StatusText.Contains(TAIL)
               && s.StatusText.Length <= NetProtocol.MaxPeerTextChars + 1,
               $"Q⑥ ★ 对面拒绝握手时塞的超长理由也被钳（实得「{s.StatusText}」）"
             + " —— 🧨 改坏法：去掉 `NetSession.cs` 收 `Ack` 那一句的 `ClampPeerText(...)` ⇒ 红");
            Ok(told != null && !told.Contains(TAIL),
               "Q⑦ ★ …`OnClosed` 那一份同样钳过（与 Q⑥ 不同源）");
        }

        // ---- ④ 「`Send()` 静默丢」⇒ 必须**出声**（红线）----
        {
            var t = new ScriptedTransport();                    // **故意不连**（`Connected` 默认 false）
            var s = new NetSession(t);
            var caught = new List<string>();
            Application.LogCallback h = (string m, string st, LogType ty) => caught.Add(m);
            Application.logMessageReceived += h;
            bool sent;
            try { sent = s.Send(NetKind.Ping, new MsgPing { t = 1 }); }
            finally { Application.logMessageReceived -= h; }
            Ok(!sent, "Q⑧ 没连上时 `Send` **如实返回 `false`**（原来是 `void`，调用方无从得知 —— "
                    + "而握手包 / 开局包 / 指纹 / 投降**全走这里**）");
            Ok(caught.Count > 0,
               "Q⑨ ★★ **丢包必须出声**：这一跳原来是一条**空的 `return`**，丢了一条包日志里一个字都没有"
             + "（红线：不许静默失败）—— 🧨 改坏法：把 `NetSession.Send` 那句 `LogWarning` 去掉"
             + "（或把守卫改回裸 `return`）⇒ 红；这条抓的是**日志流**，所以「悄无声息」时它必红");
            Ok(caught.Exists(m => m != null && m.Contains("没发出去")),
               $"Q⑩ ★ …而且那句话**点明了是「包没发出去」**，不是一句泛泛的日志（实得 {caught.Count} 条："
             + $"「{(caught.Count > 0 ? caught[0] : "")}」）");
        }

        // ---- ④-b 🆕 2026-10-18（独立审查 R5）：`Close(say:true)` 发不出 `bye` 时**也要出声** ----
        //   原来那一句是 `if (say && _t != null && _t.IsConnected && State != Off) Send(…)` ——
        //   条件不成立就**一声不响**，而 `Send` 新加的那声警告**永远走不到**这一步 ⇒
        //   本账「丢包一定出声」**恰好在「我们最需要对面收到的那句话」上不成立**（审查抓的、作者原来没记）。
        {
            var t = new ScriptedTransport();               // **故意没连**
            var s = new NetSession(t);
            var caught = new List<string>();
            Application.LogCallback h = (string m, string st, LogType ty) => caught.Add(m);
            Application.logMessageReceived += h;
            // ⚠️ **reason 故意写得平平无奇**：`Close` 里 `SetState(Off, reason)` 自己也会打一条日志
            //    （那条**不是**判据）⇒ 若把 reason 写成含「bye」或「没发出去」的字样，下面两条会**自证**。
            try { s.Close(true, "自检：收工"); }
            finally { Application.logMessageReceived -= h; }
            // 抓的时候按 `bye` 过滤（只有新加的那句警告会提到它）。
            Ok(caught.Exists(m => m != null && m.Contains("bye")),
               "Q⑰ ★★ `Close(say:true)` 在**没有活连接**时**出声**说明这句 `bye` 没发出去"
             + $"（实得 {caught.Count} 条：「{(caught.Count > 0 ? caught[0] : "")}」）"
             + " —— 🧨 改坏法：把 `NetSession.Close` 那一句改回原来那个**静默**的"
             + " `if (say && … && _t.IsConnected && …) Send(…)` ⇒ 红");
            Ok(caught.Exists(m => m != null && m.Contains("没发出去")),
               "Q⑱ ★ …而且那句话点明了是**「没发出去」**，不是一句泛泛的收工日志"
             + " —— 与 Q⑰ **不同源**：上一条只看「有没有提 `bye`」，这一条看「有没有说清它没出去」");
        }

        // ---- ⑤ 🆕 2026-10-18（主对话裁定「接着做完」）：`A961` **另两个同族入口** ----
        //   · `MsgReject.reason`（`NetProtocol.cs:148`）→ `NetBattle.Abort(...)` → **提示行 + 弹窗**；
        //   · `MsgStart.myName / foeName`（`NetProtocol.cs:74`）→ `NetPendingBattle.FromStart`。
        //   🧨 判别式：删掉 `NetBattle.cs` 里那两处的 `ClampPeerText(...)`（`case NetKind.Reject` 与
        //      `FromStart`）⇒ Q⑪~Q⑭ 立刻红。
        //   ⚠️ Q⑪/Q⑫ 走的是**整条真路**（`Dispatch` → `Abort`），而 `Abort` **一定会 `Debug.LogError`**
        //      （「联机对局中止…」，`NetBattle.cs:199`）—— 那是**被测的负例本身**，不是自检失败
        //      （同族先例：`Editor/NetBattleTest.cs:443` 那条指纹负例）。判绿红看本文件末的「断言合计」。
        {
            // ⑤-a `MsgReject.reason`
            var t = new ScriptedTransport { Connected = true };
            var s = new NetSession(t);
            var h = new SayHost();
            var rt = NetRuntime.Instance;
            bool? lobbyWas = rt != null ? rt.LobbyHandled : (bool?)null;
            var nb = NetBattle.Attach(h, s, isHost: true);
            try
            {
                NetRuntime.DrainNoticesForTest();                        // 本段只认自己那几条
                t.Inject(NetKind.Reject, NetProtocol.Pack(new MsgReject { seq = 0, reason = huge }));
                s.Pump();                                                // 帧进会话的 `Inbox`
                nb.Tick();                                               // `Dispatch` 处理它 → `Abort`
                // 前缀 = 「联机对局中止：」7 + 「主机拒绝了我的动作：」10 = 17 ⇒ 上限给 17 + 41 + 2 的余量
                Ok(h.LastSay != null && !h.LastSay.Contains(TAIL)
                   && h.LastSay.Length <= NetProtocol.MaxPeerTextChars + 20,
                   "Q⑪ ★ 对面在 `reject` 里塞的超长理由**也钳过**才进提示行"
                 + $"（实得 {h.LastSay?.Length ?? -1} 字：「{h.LastSay}」）"
                 + " —— 🧨 改坏法：去掉 `NetBattle.cs` `case NetKind.Reject` 那一句的 `ClampPeerText(...)` ⇒ 红");
                var nRej = NetRuntime.DrainNoticesForTest();
                Ok(nRej.Length >= 1 && !nRej[0].Contains(TAIL),
                   "Q⑫ ★ …**弹窗正文**那一份同样一个字没漏出去"
                 + " —— 与 Q⑪ **不同源**：一个看提示行（`NetSay`），一个看 `NetRuntime.Notice` 队列");
            }
            finally
            {
                nb.Detach();
                if (rt != null && lobbyWas.HasValue) rt.LobbyHandled = lobbyWas.Value;
                NetRuntime.DrainNoticesForTest();
            }

            // ⑤-b `MsgStart.myName / foeName`（纯静态构造，不用会话）
            var pb = NetPendingBattle.FromStart(new MsgStart { myName = huge, foeName = huge }, isHost: true);
            Ok(pb != null && pb.MyName != null && !pb.MyName.Contains(TAIL)
               && pb.MyName.Length <= NetProtocol.MaxPeerTextChars + 1,
               "Q⑬ ★ `MsgStart.myName` 同样是**对端可控**的，进 `NetPendingBattle` 前先钳"
             + $"（实得 {pb?.MyName?.Length ?? -1} 字）"
             + " —— 🧨 改坏法：去掉 `FromStart` 里那两次 `ClampPeerText(...)` ⇒ Q⑬/Q⑭ 红");
            Ok(pb != null && pb.FoeName != null && !pb.FoeName.Contains(TAIL)
               && pb.FoeName.Length <= NetProtocol.MaxPeerTextChars + 1,
               $"Q⑭ ★ …`foeName` 那一份同理（实得 {pb?.FoeName?.Length ?? -1} 字）");
        }

        // ---- ⑥ 🆕 2026-10-18（主对话裁定「封口」）：`FoeName` —— **玩家看得见的那条路** ----
        //   `NetMatchmaking.FoeName` 落地两处、**来源不同**：
        //     · `NetMatchmaking.cs:453` **主机侧** = `s.PeerName`（**已经钳过** —— `NetSession` 收
        //       `MsgProof` 那一刻就钳了，见 `Q④`）；
        //     · `NetMatchmaking.cs:487` **客机侧** = `MsgStart.myName`（**本笔新钳**）。
        //   往下它走两条**真在界面上**的路：`Shell/SearchingOpponentWindow.cs:83`（「找到对手」那扇窗）
        //   与 `Battle/BattleDriver.cs:10303` / `:10530`（对局里的敌方名）—— ⛔ 那两份文件都**不在白名单**，
        //   我只**读**过、断言写在自己这个宿主里（本段）。
        //   🧨 判别式：删掉 `NetMatchmaking.cs:487` 那一行的 `ClampPeerText(...)` ⇒ **只有 Q⑯ 红**
        //      （Q⑮ 仍绿 —— 它走的是另一条来源，靠 `NetSession` 那次钳）。
        //   ⚠️ **2026-10-18 订正（独立审查 R2）**：原文写「反过来删 `NetSession.cs` 里 `PeerName` 那次钳
        //      ⇒ 两条一起红」—— **不成立**：`Q⑯` 读的 `FoeName` **不经过 `PeerName`**（客机侧
        //      `FoeName = ClampPeerText(st.myName)`，而 `st.myName = _myName = ProfileData.PlayerName`；
        //      `PeerName` 是 `MsgProof.name`）⇒ 删那次钳**只让 `Q⑮` 红**，`Q⑯` 仍绿。
        //   ⚠️ 这一段要**两个真 socket** + `Netmatchmaking.PumpLobby`（大厅那条真路），故本方法接一个 `port`。
        {
            var host6 = NetSession.NewTcp();
            var cli6 = NetSession.NewTcp();
            var rt6 = NetRuntime.Ensure();
            var keep6 = rt6.Session;
            var pbWas6 = NetPendingBattle.Current;      // 🔴 `GoToBattle` 会往这个**跨场景静态槽**里塞东西 —— 用完还回去
            string nameWas = ProfileData.PlayerName;
            try
            {
                Ok(host6.StartHost(Cfg(port, "")), "Q⑮-a 主机起来监听");
                // 🔴 **先**把本机显示名设成超长 —— 它在这条路上会当**两次**「对端可控文本」：
                //   ① 握手包 `MsgProof.name` ⇒ 主机的 `PeerName`（对主机来说「对面」是客机）；
                //   ② 开局包 `MsgStart.myName` ⇒ 客机的 `FoeName`（对客机来说「对面」是主机）。
                ProfileData.PlayerName = huge;
                cli6.CheckConnection(Cfg(port, ""));
                Ok(PumpUntil(host6, cli6, () => host6.State == NetState.Lobby && cli6.State == NetState.Lobby, 5000),
                   "Q⑮-b 两边握手到 `Lobby`（对面报上来的就是那条 200+ 字的名字）");

                var d1 = new PlayerDeck { Name = "自检主机牌", WarlordId = "UM_WARLORD" }; d1.CardIds.Add("UM1");
                var d2 = new PlayerDeck { Name = "自检客机牌", WarlordId = "UM_WARLORD" }; d2.CardIds.Add("UM2");

                NetMatchmaking.Reset();
                rt6.AttachForTest(host6);
                Ok(NetMatchmaking.TryStart(d1, "Classic", "Ultramarines", out string _),
                   "Q⑮-c 主机点 `Battle!`（`_myName` 取的就是那条超长名）");
                // 对面那台机器交来它的卡组 —— 直接发协议那一帧（它就是「另一台机器上的 `TryStart`」）。
                // 🔴 **不用 `TryStart(cli6)`**：`NetMatchmaking` 是同进程的**静态单例**，换会话扮演另一端会把
                //    主机的 `_myDeck` 冲掉（`TryStart` 开头 `Reset()`）⇒ 主机那一侧就开不了局、`start` 发不出去。
                cli6.Send(NetKind.Deck, new MsgDeck
                {
                    deckJson = JsonUtility.ToJson(d2), gameMode = "Classic", faction = "Ultramarines",
                });
                PumpBoth(host6, cli6, 400);
                rt6.AttachForTest(host6);
                NetMatchmaking.PumpLobby();          // 主机收 `deck` ⇒ `FoeName = s.PeerName`；两副齐 ⇒ `HostStartMatch()`
                Ok(NetMatchmaking.HasOpponent,
                   "（Q⑮ 夹具）主机收齐两副 ⇒ 这一刻真的开局了（下面那句 `start` 才发得出去）");
                Ok(NetMatchmaking.FoeName != null && !NetMatchmaking.FoeName.Contains(TAIL)
                   && NetMatchmaking.FoeName.Length <= NetProtocol.MaxPeerTextChars + 1,
                   "Q⑮ ★ **主机那一路**（`NetMatchmaking.cs:453` 的 `s.PeerName`）端到端也被钳"
                 + $"（实得 {NetMatchmaking.FoeName?.Length ?? -1} 字）"
                 + " —— 它靠的是 `NetSession` 收 `MsgProof` 时那一次钳（`Q④` 那条）；"
                 + "🧨 删掉 `NetSession.cs` 里 `PeerName` 那次 `ClampPeerText` ⇒ **只红这一条**"
                 + "（⚠️ 2026-10-18 订正 —— 原文写「Q⑮/Q⑯ 一起红」：**不成立**，`Q⑯` 走的是另一条"
                 + "来源、不经过 `PeerName`，见上面那段注释）");

                PumpBoth(host6, cli6, 400);
                rt6.AttachForTest(cli6);
                NetMatchmaking.PumpLobby();          // 客机收 `start` ⇒ `FoeName = st.myName`（本笔新钳那一行）
                Ok(NetMatchmaking.FoeName != null && !NetMatchmaking.FoeName.Contains(TAIL)
                   && NetMatchmaking.FoeName.Length <= NetProtocol.MaxPeerTextChars + 1,
                   "Q⑯ ★★ **客机那一路**（`NetMatchmaking.cs:487` 的 `MsgStart.myName`）也钳过了 ——"
                 + " 而 `FoeName` 就是**玩家看得见**的那一份（「找到对手」那扇窗 + 对局里的敌方名）"
                 + $"（实得 {NetMatchmaking.FoeName?.Length ?? -1} 字）"
                 + " —— 🧨 改坏法：删掉 `NetMatchmaking.cs:487` 那一行的 `ClampPeerText(...)` ⇒ **只有这一条**红");
            }
            finally
            {
                ProfileData.PlayerName = nameWas;        // 🔴 进程级静态：用完立刻还回去
                NetPendingBattle.Current = pbWas6;       // 🔴 同上（本段真的走到了 `GoToBattle`，它会写这个槽）
                rt6.AttachForTest(keep6);
                NetMatchmaking.Reset();
                NetRuntime.DrainNoticesForTest();
                host6.Close(false); cli6.Close(false);
            }
        }

        // ---- ⑦ 🆕 2026-10-18（第五轮 · 账 ③）：**`SessionToken` 不会被陈旧地带到下一局** ----
        //   这条撑着「`Close` **故意不清** `SessionToken`」那个结论（理由写在 `NetSession.Close` 的注释里）：
        //   它的两个写点都在**新连接的开头**（`StartHost` 的新 `NewToken()` / 收 `Ack` 时那句赋值）
        //   ⇒ **同一台会话重开一局会拿到新的一把** ⇒ 不清也安全。
        //   🧨 判别式：删掉 `StartHost` 里那句 `SessionToken = NewToken();`（或让重开沿用旧值）⇒ 下面那条红。
        //   ⚠️ **钥匙本身一个字符都不打**（`SessionToken` 的注释写着「不进日志」）—— 只报长度。
        {
            var h7 = NetSession.NewTcp();
            var c7 = NetSession.NewTcp();
            // ⚠️ **本段单独要一个空端口**：它是全仓**第一处**「同一个端口关掉再绑一次」的用例，
            //    不拿 `port` 去赌 Windows 的重绑时序（真绑不上就会红成假红）。
            int p7 = FreePort();
            try
            {
                Ok(h7.StartHost(Cfg(p7, "")), "Q⑦-a 第一局：主机起来监听");
                c7.CheckConnection(Cfg(p7, ""));
                Ok(PumpUntil(h7, c7, () => h7.State == NetState.Lobby && c7.State == NetState.Lobby, 8000),
                   "Q⑦-b 第一局握手走完");
                string t1 = c7.SessionToken;
                Ok(!string.IsNullOrEmpty(t1) && t1 == h7.SessionToken, "Q⑦-c 第一局那把钥匙两边一致");
                h7.Close(false); c7.Close(false);          // 关台 —— **故意不清 token**（见 `NetSession.Close` 的注释）

                Ok(h7.StartHost(Cfg(p7, "")), "Q⑦-d 第二局：**同一台会话对象**重新开台");
                c7.CheckConnection(Cfg(p7, ""));
                Ok(PumpUntil(h7, c7, () => h7.State == NetState.Lobby && c7.State == NetState.Lobby, 8000),
                   "Q⑦-e 第二局握手走完");
                Ok(c7.SessionToken != t1 && c7.SessionToken == h7.SessionToken,
                   "Q⑦ ★★ **第二局拿到的是【新的一把】钥匙**（不是 `Close` 时留在对象上的那一把）"
                 + $"（旧长度 {t1?.Length ?? -1} / 新长度 {c7.SessionToken?.Length ?? -1}；"
                 + "⚠️ 钥匙本身**不进日志**）"
                 + " —— 这正是「`Close` 不清 `SessionToken` 也安全」的那条不变式；"
                 + "🧨 删掉 `StartHost` 里那句 `SessionToken = NewToken();` ⇒ 红");
            }
            finally { h7.Close(false); c7.Close(false); }
        }

        // ---- ⑧ 🆕 2026-10-18（第五轮 · 账 ①②）：**传输层的两个残留静默** ----
        //   ① 「**过了守卫、底层真写失败**」那一档原来只写 `_lastError`、**生产路径没人读** ⇒
        //      现在消费点长在 `NetSession.Send` 里（调传输层前后各读一次，变了就出声 + 返回 `false`）。
        //   ② 重复 `Listen` 原来**一声不响**地被忽略（行为本身是对的）⇒ 现在出声，**行为不改**。
        {
            // ①-a 底层写失败 ⇒ `Send` 如实返回 `false` + 出声
            var tf = new ScriptedTransport { Connected = true, FailOnSend = "发送失败：自检假装的写失败" };
            var sf = new NetSession(tf);
            var c1 = new List<string>();
            Application.LogCallback h1 = (string m, string st, LogType ty) => c1.Add(m);
            Application.logMessageReceived += h1;
            bool sent1;
            try { sent1 = sf.Send(NetKind.Ping, new MsgPing { t = 1 }); }
            finally { Application.logMessageReceived -= h1; }
            Ok(!sent1, "Q⑲ ★ **过了守卫、底层真写失败** ⇒ `Send` 如实返回 `false`"
                      + "（原来那一档只写 `_lastError`、谁也不知道）");
            Ok(c1.Exists(m => m != null && m.Contains("传输层报失败了")),
               "Q⑳ ★ …而且**出声**（`_lastError` 现在有消费点了）"
             + $"（实得 {c1.Count} 条：「{(c1.Count > 0 ? c1[0] : "")}」）"
             + " —— 🧨 改坏法：删掉 `NetSession.Send` 里那段读 `LastError` 的代码 ⇒ Q⑲/Q⑳ 一起红");

            // ①-b **对照（灭自证）**：同一根桩**不制造失败** ⇒ 同一句必须**返回 true 且不出声**
            //   （没有这一条，「返回 false / 出声」可能只是「这条永假」的假象）。
            var tc = new ScriptedTransport { Connected = true };
            var sc = new NetSession(tc);
            var c2 = new List<string>();
            Application.LogCallback h2 = (string m, string st, LogType ty) => c2.Add(m);
            Application.logMessageReceived += h2;
            bool sent2;
            try { sent2 = sc.Send(NetKind.Ping, new MsgPing { t = 1 }); }
            finally { Application.logMessageReceived -= h2; }
            Ok(sent2 && !c2.Exists(m => m != null && m.Contains("传输层报失败了")),
               "Q㉑ ★（对照）**同一根桩没制造失败时**：`Send` 返回 `true` **且不报写失败**"
             + " —— 与 Q⑲/Q⑳ **互为反例**（不是「反正都返回 false」）");

            // ② 重复 `Listen` ⇒ 出声，**但行为不改**（仍然只监听一份）
            var raw = new TcpTransport();
            int p8 = FreePort();               // ⚠️ 同上：不拿别人刚关掉的 `port` 去重绑
            var c3 = new List<string>();
            Application.LogCallback h3 = (string m, string st, LogType ty) => c3.Add(m);
            try
            {
                raw.Listen(p8);
                bool wasListening = raw.IsListening;
                Application.logMessageReceived += h3;
                try { raw.Listen(p8); }                          // 第二次 ⇒ 应当出声
                finally { Application.logMessageReceived -= h3; }
                Ok(wasListening && raw.IsListening,
                   "Q㉒ 重复 `Listen` 之后**仍然在监听**（**行为一字未改** —— 只是不再闷着）");
                Ok(c3.Exists(m => m != null && m.Contains("被忽略")),
                   "Q㉓ ★ 重复 `Listen` **出声**了（原来一声不响）"
                 + $"（实得 {c3.Count} 条：「{(c3.Count > 0 ? c3[0] : "")}」）"
                 + " —— 🧨 改坏法：把 `TcpTransport.Listen` 那句 `LogWarning` 去掉 ⇒ 红");
            }
            finally { raw.Close(); }
        }
    }

    // ==================================================================
    //  R. 🆕 2026-10-19：**传输层的三条**（`F5_NetBattle夹具.md` §七 顺手发现 1/2/3）
    //      `A1267` `Fail()` 是静的 · `A1268` `Setup` 不停旧读线程 · `A1269` 掉线闸不挡 `Off`
    // ==================================================================
    /// <summary>判据 = `资料/普查产出_第十一会话/F5_NetBattle夹具.md` §三 B/§三 C·候选 (iii)/§七·3。
    ///
    /// <para>🔴 **`A1267`（`Fail` 静默）**：`TcpTransport.Fail` 原来只写 `_lastError`、**一行日志不打**，
    /// 而那个字段当时**全仓唯一**的消费点是 `NetSession.Send` 的前后对比 ⇒ **「为什么掉线」永远不进日志**
    /// （违反「不许静默失败」；`F5` 的 X2「`bye` 丢在哪一跳」就卡在这一格）。现在**出声**，
    /// 而且按「**给谁看**」分开：这一行是**给排查的**（自检 / `Player.log`）——
    /// ⛔ 玩家那一侧照旧只有**固定词条**的状态字（原版同：`Battle/HUD/WaitOpponentConnectionMsg`）。</para>
    ///
    /// <para>🔴 **`A1268`（旧读线程迟到）**：`Setup` 关旧 socket 会唤醒旧读线程，醒来时机**不可控**
    /// ⇒ 它的小 `Fail` 可能落在新连接的 `Publish(true)` **之后**，把**活着的**新连接标成「掉了」
    /// （还会写脏 `_lastError`）。修法是给连接编【代】（`Setup` +1），旧代一律不改共享状态。
    /// ⚠️ **那个交错是微秒级、且不可控** ⇒ 用真 socket 跑一万次也撞不稳，写成行为断言只会是
    /// 「永远绿（或永远红）的假断言」—— 同 §O 的 `_state`、§P 的 `ScriptedTransport` 那两条的理由。
    /// 这里用 `FailForTest(generation, …)` 把「哪一代」**当参数摆出来** ⇒ 每次都必现。</para>
    ///
    /// <para>🔴 **`A1269`（掉线闸不挡 `Off`）**：`NetSession.Pump` 那道闸原来只挡 `Closed`/`WaitingReconnect`
    /// ⇒ `Close()` 之后再被 `Pump` 一次（而 `_t.PeerLost` **还**是真 —— `ClosePeer` 不清它）
    /// 就会**再报一次** `OnPeerLost`，状态还从 `Off` **倒回** `WaitingReconnect`。</para>
    ///
    /// <para>⛔ **本段两个测试都要真 socket**（各接一个 `port`）；R⑬~R⑯ 那一段用一台**裸监听**
    /// 当对端（不是 `NetSession`）—— 这样「本机自己拆」与「对端关掉」两档都只有**一个**可能的发声方，
    /// 计数才有鉴别力。</para></summary>
    static void TestFailVoiceA1267A1268(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        var ht = (TcpTransport)host.Transport;      // ⚠️ 那两个 `ForTest` 口长在 `TcpTransport` 上（接口上没有）
        try
        {
            Ok(host.StartHost(Cfg(port, "")), "R① 主机起来了");
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
               "R② 两边握手到 `Lobby`（下面拿这条**活着的**连接当尺子）");
            Ok(ht.IsConnected && !ht.PeerLost, "R③ 起手：连接位立着、「掉过线」是假的");
            int gen = ht.GenerationForTest;
            Ok(gen >= 1, $"R④ 这条连接是**第 {gen} 代**（`Setup` 每开一条 +1）");

            // ---- ① A1268：**旧连接迟到的失败**一个字都不许碰共享状态 ----
            string errBefore = ht.LastError;
            var warn1 = new List<string>();
            var log1 = new List<string>();
            Application.LogCallback h1 = (string m, string st, LogType ty) =>
            {
                if (m == null || !m.Contains("[Net]")) return;
                if (ty == LogType.Warning) warn1.Add(m); else log1.Add(m);
            };
            Application.logMessageReceived += h1;
            try
            {
                ht.FailForTest(gen - 1, "自检：旧连接迟到的失败");
                ht.Pump(null);            // 主线程那个出口（`into == null` 也要把攒下的话说完）
            }
            finally { Application.logMessageReceived -= h1; }
            Ok(ht.IsConnected,
               "R⑤ ★★ **旧连接迟到的 `Fail` 之后，这条活着的连接位仍然是立的**"
             + " —— 🧨 改坏法：把 `Fail(int,string)` 里那道 `gen != _gen` 的闸删掉 ⇒ 红（这是 A1268 的正题）");
            Ok(!ht.PeerLost,
               "R⑥ ★★ …而且**没被标成「掉过线」**（`_peerLost` 一个字没动 ⇒ 上层不会把一条好连接判成掉线）");
            Eq(string.CompareOrdinal(ht.LastError ?? "", errBefore ?? ""), 0,
               "R⑦ ★ …`_lastError` 也没被写脏（它是**粘的**，写进去一个不属于当前连接的原因会一直挂着）");
            Ok(log1.Exists(m => m.Contains("旧连接的读线程退出了")),
               "R⑧ ★ …而且**出声**（`Debug.Log` 那一档）：那一次迟到被记下来了，"
             + "排查重连/丢帧时能直接看出「旧读线程是什么时候退的、为什么」"
             + $"（实得 {log1.Count} 条：「{(log1.Count > 0 ? log1[0] : "")}」）");
            Ok(!warn1.Exists(m => m.Contains("传输层掉线")),
               "R⑨ ★（灭自证）旧连接那一档**不许冒充「真掉线」** —— 它必须是 `Log` 级别、"
             + "文案里也不许带「传输层掉线」那个标记（否则排查时两档分不开）");

            // ---- ② A1267：**当前这一代真掉线** ⇒ 原因必须进日志 ----
            var warn2 = new List<string>();
            Application.LogCallback h2 = (string m, string st, LogType ty) =>
            { if (ty == LogType.Warning && m != null && m.Contains("传输层掉线")) warn2.Add(m); };
            Application.logMessageReceived += h2;
            try
            {
                ht.FailForTest(gen, "自检：当前这一代真掉线");
                ht.Pump(null);
            }
            finally { Application.logMessageReceived -= h2; }
            Ok(!ht.IsConnected && ht.PeerLost,
               "R⑩ 当前这一代报失败 ⇒ 连接位清掉、「掉过线」置上（**既有行为一字未改**）");
            Eq(warn2.Count, 1,
               "R⑪ ★★ **「为什么掉线」进日志了**（原来 `Fail` 一行都不打 ⇒ 那句原因全日志零命中）"
             + " —— 🧨 改坏法：删掉 `Fail` 里那句 `_noteWarn.Enqueue(...)`，或删掉 `Pump` 里的出队循环 ⇒ 红");
            Ok(warn2.Count == 1 && warn2[0].Contains("自检：当前这一代真掉线"),
               $"R⑫ ★ 而且**带的就是 `Fail` 收到的那个原因**（实得「{(warn2.Count > 0 ? warn2[0] : "")}」）"
             + " —— 与 `NetSession.Pump` 那条状态字**不同源**：那条是固定词条「对手掉线了，正在等他回来…」"
             + "（原版同口径），**不含原因**");

            // ---- ③ 灭自证：「本机自己拆的」**不许**冒充「真掉线」；同一台传输「对端关掉」**必须**出一条 ----
            //   ⚠️ 对端用一台**裸监听**（不是 `NetSession`）—— 这样两档都只有**一个**可能的发声方，
            //      「恰好 N 条」这种计数才有鉴别力。
            var lis = new TcpListener(System.Net.IPAddress.Loopback, 0);
            lis.Start();
            int rp = ((System.Net.IPEndPoint)lis.LocalEndpoint).Port;
            var raw = new TcpTransport();
            System.Net.Sockets.TcpClient peer = null;
            var w = new List<string>();
            Application.LogCallback hw = (string m, string st, LogType ty) =>
            { if (ty == LogType.Warning && m != null && m.Contains("传输层掉线")) w.Add(m); };
            try
            {
                Ok(raw.Connect("127.0.0.1", rp, 3000), "R⑬ 裸传输连上一台裸监听");
                peer = lis.AcceptTcpClient();
                Application.logMessageReceived += hw;

                // ③-a 本机主动拆（`ClosePeer`）⇒ 我们自己那个读线程那一次失败 = **收工**，不是掉线
                raw.ClosePeer();
                Thread.Sleep(150);                 // 让读线程真的失败一次（它一失败就退出）
                raw.Pump(null);                    // 主线程出口：攒下的话这会儿打出来
                Eq(w.Count, 0,
                   "R⑭ ★ **本机主动 `ClosePeer` ⇒ 一条「掉线」告警都不许有**（收工不是掉线）"
                 + " —— 不区分的话，每次正常收台（`NetSession.Close`/`Reset`/自检清理）都会留下假信号，"
                 + "而那正是排查「为什么掉线」时最不该有的噪声");

                // ③-b **对端关掉** ⇒ 同一台传输、同一段代码，**必须恰好出一条、且带原因**
                try { peer.Close(); } catch { }    // ③-a 那条对端句柄收掉（夹具里别漏 socket）
                Ok(raw.Connect("127.0.0.1", rp, 3000), "R⑮ 再连一条（同一台传输对象）");
                peer = lis.AcceptTcpClient();
                peer.Close();                      // 🔴 这一档才是真掉线（对面 FIN）
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 3000 && !raw.PeerLost) { Thread.Sleep(10); raw.Pump(null); }
                raw.Pump(null);
                Eq(w.Count, 1,
                   "R⑯ ★★（灭自证）**对端关掉**时**恰好一条**「掉线」告警 —— 与 R⑭ 互为反例"
                 + $"（不是「反正都不出声」，也不是「每帧刷一条」）（实得 {w.Count} 条："
                 + $"「{(w.Count > 0 ? w[0] : "")}」）");
                Ok(w.Count == 1 && (w[0].Contains(TermHead("Settings/Online/St/PeerClosed"))
                                    || w[0].Contains(TermHead("Settings/Online/St/ReadAbort"))),
                   "R⑰ ★ 而且那条里的原因就是**读线程亲眼看到的**那个（`St/PeerClosed`「对面关掉了连接」"
                 + "或 `St/ReadAbort`「读取中断：…」）—— 不是我另编一句");
            }
            finally
            {
                Application.logMessageReceived -= hw;
                try { if (peer != null) peer.Close(); } catch { }
                try { lis.Stop(); } catch { }
                raw.Close();
            }
        }
        finally { host.Close(false); cli.Close(false); }
    }

    /// <summary>🆕 🔴 **`A1269`（2026-10-19）**：`NetSession.Pump` 那道掉线闸**只挡 `Closed`/`WaitingReconnect`、
    /// 不挡 `Off`** ⇒ `Close()` 之后再被 `Pump` 一次（而 `_t.PeerLost` **还**是真 —— `ClosePeer` 只清连接位、
    /// 不清它）就会**再报一次** `OnPeerLost`，状态还从 `Off` **倒回** `WaitingReconnect`。
    /// （`F5` §七·3；它在生产路径上现在不现形 —— 只有一处 `Pump`（`NetRuntime.Update`）——
    ///  但这是一条「**谁再 `Pump` 一次就现形**」的边。）
    ///
    /// <para>🧨 **灭自证**：R㉑/R㉒ 要求那道闸**该响还得响**（真掉线时要响一次、要进等重连）——
    /// 没有这两条，把整条 `if` 删掉也能把 R㉔/R㉕ 蒙绿。</para></summary>
    static void TestPeerLostGateOffA1269(int port)
    {
        var host = NetSession.NewTcp();
        var cli = NetSession.NewTcp();
        int cliLost = 0;
        cli.OnPeerLost = () => cliLost++;
        try
        {
            Ok(host.StartHost(Cfg(port, "")), "R⑱ 主机起来了");
            cli.CheckConnection(Cfg(port, ""));
            Ok(PumpUntil(host, cli, () => host.State == NetState.Lobby && cli.State == NetState.Lobby, 5000),
               "R⑲ 两边握手到 `Lobby`");
            Eq(cliLost, 0, "R⑳ 起手：这台客机会话一次都没报过「对面掉线」");

            // ---- ① 真掉线 ⇒ 那道闸**该响就得响**（灭自证）----
            host.Transport.ClosePeer();
            Ok(PumpUntil(cli, null, () => cli.Transport.PeerLost, 4000),
               "R㉑ 客机这一侧认出「对面没了」（`PeerLost` 只能来自**传输层的读线程**）");
            cli.Pump();
            Eq(cliLost, 1, "R㉒ ★ 真掉线 ⇒ `OnPeerLost` **响一次**"
                         + " —— 🧨 改坏法：把 `NetSession.Pump` 那道闸整段删掉 ⇒ 这条红");
            Ok(cli.State == NetState.WaitingReconnect, $"R㉓ 而且状态进「等重连」（实际 {cli.State}）");

            // ---- ② `Close()` 之后再推：**不许**再响、**不许**把状态从 `Off` 倒回去 ----
            cli.Close(false);
            Ok(cli.State == NetState.Off, $"R㉔ 收工 ⇒ 状态 `Off`（实际 {cli.State}）");
            Ok(cli.Transport.PeerLost,
               "★ 夹具自检：**收工之后 `PeerLost` 还是真**（`ClosePeer` 只清连接位、不清它）"
             + " —— 这正是 A1269 的**前提**，没有它下面两条就是假断言");
            for (int i = 0; i < 5; i++) { cli.Pump(); Thread.Sleep(10); }
            Eq(cliLost, 1, "R㉕ ★★ **收工之后再推 5 次，`OnPeerLost` 一次都不许多响**（A1269 正题）"
                         + " —— 🧨 改坏法：把 `NetSession.cs` 那句 `State != NetState.Off` 删掉 ⇒ 红");
            Ok(cli.State == NetState.Off,
               $"R㉖ ★★ …而且状态**没有从 `Off` 倒回**「等重连」（实际 {cli.State}）"
             + " —— 与 R㉕ **不同源**：R㉕ 数的是「说不说」，这条看的是「状态动没动」");
        }
        finally { host.Close(false); cli.Close(false); }
    }

    /// <summary>🆕 2026-10-18（A961 续）：`§Q⑤-a` 用的**最小联机宿主** —— 只把「说给玩家听的那句话」记下来。
    /// `A961` 那一条要验的是 `Abort` 那条路（`NetBattle.Dispatch` → `Abort` → `NetSay` + `NetRuntime.Notice`），
    /// 而 `Abort` **一次都不碰引擎**（它里面没有 `Ctx`）⇒ 别的成员给空实现就够了。
    /// ⛔ 别拿它当「真宿主」的替身 —— 真对局那一路是 `BattleDriver`，同族先例见 `NetBattleTest.BareHost`。</summary>
    sealed class SayHost : INetBattleHost
    {
        public BattleContext Ctx { get { return null; } }
        public string LastSay;
        public int ApplyLoggedAction(MsgAction m) { return RuleCodes.OK; }
        public void NetSay(string s) { LastSay = s; }
        public void NetRemoteResign(BattleResult reason) { }
        /// <summary>🆕 2026-10-18（A914 第四续）：接口新加的「判**本机**负」那一口 ——
        /// 本段只验 `Abort` 那条路，它**一次都不碰引擎** ⇒ 空实现（同族的 `NetRemoteResign` 也是空的）。
        /// ⛔ 真实现那一份在 `BattleDriver`（`RuleCore.Forfeit(Ctx, _me, reason)` + 记账口）。
        /// ⚠️ 形参 `reason`（A913）在这里空着是对的 —— 本类一次都不落地那个码（没引擎可落）。</summary>
        public void NetSelfResign(BattleResult reason) { }
        public void NetReplayFromNet(MsgStart start, List<MsgAction> actions) { }
    }

    /// <summary>§P 用的**可注入传输**：`AcceptedCount` 按脚本「**读一次给一个值**」（用完一直是最后一个）
    /// —— 真机上那个夹缝是纳秒级，只有把它摆出来才必现。
    /// ⚠️ 只给 §P 用：它不接 socket、不收包，`Send` 只记 kind。`Port` 恒 0 ⇒ `UpnpPortMapper.MapAsync`
    /// 第一句（`port &lt;= 0`）就返回 ⇒ **自检对玩家的路由器零副作用**。</summary>
    sealed class ScriptedTransport : INetTransport
    {
        public int[] Counts = new[] { 0 };
        public bool Connected;
        public int ClosePeerCalls;
        public readonly List<string> Sent = new List<string>();
        int _i;
        bool _listening;

        public bool IsListening { get { return _listening; } }
        public bool IsConnected { get { return Connected; } }
        public bool PeerLost { get { return false; } }
        /// <summary>🆕 2026-10-18（第五轮 · 账 ①）：**这根桩的「最近一次失败原因」**（真实现 = `TcpTransport._lastError`）。
        /// 默认 `null`（= 一切正常），由 <see cref="FailOnSend"/> 在发送时置上。</summary>
        string _lastError;
        public string LastError { get { return _lastError; } }
        /// <summary>🆕 2026-10-18（第五轮 · 账 ①）：置成非空 ⇒ **每次 `Send` 都把 `LastError` 改成它**
        /// （模拟「**过了守卫、但底层真写失败**」那一档 —— 真 socket 上构造不出稳定的写失败，
        /// 只能由桩来摆，思路同 §P 那条「读一次给一个值」）。默认 `null` ⇒ 行为与原来逐字相同。</summary>
        public string FailOnSend;
        public int Port { get { return 0; } }
        public int AcceptedCount
        {
            get { int v = Counts[Math.Min(_i, Counts.Length - 1)]; _i++; return v; }
        }
        /// <summary>发出去过几条这个 `kind`（断言「边沿没被白白吃掉」用）。</summary>
        public int CountOf(string kind) { return Sent.FindAll(k => k == kind).Count; }

        /// <summary>主机侧唯一入口。🔴 **2026-10-18 修（§P 三条红的根因）**：`StartHost` 内部会先调 `Close(false)`
        /// ⇒ 桩那时被置成 `Connected = false`；而 `Listen` 原来**没把它置回来** ⇒ 夹具进入 `Pump` 时 `IsConnected` 恒假，
        /// 于是 `NetSession.Send` 的守卫（`NetSession.cs:450`，**静默丢**）吞掉 `Challenge`、`:247` 的第一个合取项
        /// 短路掉整条超时判据 ⇒ **P⑦/P⑨/P⑩ 三条红**（而客机侧因为 `Connect()` 会置回 true，P⑫/P⑬ 是绿的 ——
        /// 红绿分界**只由这一位决定**）。桩的语义就是「**这条链路一直活着**」，故在 `Listen` 里恢复它。</summary>
        public void Listen(int port) { _listening = true; Connected = true; }
        public bool Connect(string host, int port, int timeoutMs) { Connected = true; return true; }
        public void Send(string kind, string payloadJson)
        {
            Sent.Add(kind);
            // 🆕 2026-10-18（第五轮 · 账 ①）：见 `FailOnSend` 的注释。默认 null ⇒ 什么都不做。
            if (FailOnSend != null) _lastError = FailOnSend;
        }
        public int Pump(List<NetFrame> into)
        {
            // 🆕 2026-10-18（A961）：把 `Inject(...)` 塞进来的帧吐出去（默认空表 ⇒ 行为与原来逐字相同，
            //   §P 那一路一个字节都没变）。用它才能**不建 socket** 就喂一条「对端可控」的包。
            if (into == null || _inject.Count == 0) return 0;
            int n = _inject.Count;
            into.AddRange(_inject);
            _inject.Clear();
            return n;
        }
        public void ClosePeer() { ClosePeerCalls++; Connected = false; }
        public void Close() { _listening = false; Connected = false; }

        /// <summary>🆕 2026-10-18（A961 自检用）：要塞给 `NetSession` 的帧（下一次 `Pump` 时吐出去）。
        /// 🔴 为什么要它：`A961` 那三个入口（`MsgBye.reason` / `MsgProof.name` / `MsgAck.reason`）
        /// **只有「对面发来」这一条路才演示得出来**，而真 socket 上没法让对端发一条 200 字的理由
        /// （`NetSession.Close(say:true, reason)` 的 `reason` 是我们自己写的）。
        /// 桩的语义就是「这条链路上收到了这么一帧」。</summary>
        readonly List<NetFrame> _inject = new List<NetFrame>();
        public void Inject(string kind, string payloadJson)
        {
            _inject.Add(new NetFrame { kind = kind, payload = payloadJson });
        }
    }

    /// <summary>🆕 2026-10-17（B27·A925）：`NetMatchmaking.OnHint` 上**此刻挂了几根接线**（0 = 没人订）。
    /// 为什么要数它：`OnHint` 是**多播委托**，而这一段自己也订了一根**探针** ⇒
    /// 只看 `!= null` 分不清「窗挂上了」还是「只有我那根探针」。</summary>
    static int HintSubs()
    {
        return NetMatchmaking.OnHint == null ? 0 : NetMatchmaking.OnHint.GetInvocationList().Length;
    }

    /// <summary>按节点名递归找那一颗 `Label`（`Transform.Find` **只找直接子件**，这棵树有两层 ⇒ 自己走）。</summary>
    static Label FindLabel(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name)
        {
            var l = root.GetComponent<Label>();
            if (l != null) return l;
        }
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindLabel(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
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
