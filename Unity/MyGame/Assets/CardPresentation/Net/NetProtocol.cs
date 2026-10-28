// NetProtocol.cs — 联机协议 v1：**帧格式 + 消息 DTO + 座位口径（绝对编号；`Seat()` 仅供自检） + 状态指纹**
//
// 为什么长这样（三条约束决定的，别绕开）：
//   ① **零依赖**（用户 2026-09-26 拍板）：所以用 `JsonUtility`（Unity 自带）+ 长度前缀自己组帧，
//      不引第三方 JSON/网络库。⚠️ `JsonUtility` **不支持多态** ⇒ 信封只放 `kind` + `payload`（内层再一串 JSON）。
//   ② **可自检**：批处理下没有帧循环 ⇒ 收发全是 `Pump()` 拉取式，自检里显式推进（见 `NetSelfTest`）。
//   ③ **座位是【绝对的】**：主机 = 0 · 客机 = 1，**两端跑同一套编号**（`BattleDriver._me` 只决定视图方向，
//      引擎不镜像）⇒ 协议里的 `targetP` / `slot` / `handIdx` **一律不用翻**，接收方也**不做任何翻译**。
//      ⚠️ **2026-10-19（`A1114`(a) 同族）就地订正（铁律 5）**：本行原来写「两端都以『自己 = 0 号位』跑引擎
//      （`BattleDriver._me` 焊死），所以协议里的座位一律是发送方视角，接收方过一次 `Flip`」—— 那是**镜像时代**
//      的话，**2026-09-26 已废弃**（引擎层面走不通：第 40 步 `Unstable` 当场分叉，见下面 `Fingerprint` 那段）。
//
// 判据与设计全文 → `资料/联机P2P_设计与交接.md` §五（**唯一正本，改动先去改那一份**）。
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RuleEngine;

namespace CardPresentation.Net
{
    /// <summary>联机里「我是谁」。</summary>
    public enum NetRole { Off = 0, Host = 1, Client = 2 }

    /// <summary>会话状态机。⚠️ 加状态时**同步加** `NetSession.StatusText` 里那句人话（不许静默）。</summary>
    public enum NetState
    {
        Off = 0,              // 没开
        Listening = 1,        // 主机：等客机连进来
        Connecting = 2,       // 客机：正在连
        Handshaking = 3,      // 连上了，正在验协议版本与密码
        Lobby = 4,            // 握手过了，等双方选完卡组
        InBattle = 5,         // 对局中
        WaitingReconnect = 6, // 对面掉了，等他回来（**不判负** —— 用户 2026-09-26 要重连）
        Closed = 7,           // 结束（看 LastError/Reason）
    }

    /// <summary>帧信封。`payload` 是**内层那个 DTO 的 JSON 串**（`JsonUtility` 不支持多态，只能这样套一层）。</summary>
    [Serializable]
    public class Envelope
    {
        public string kind;
        public string payload;
    }

    // ==================================================================
    //  消息载荷（每个 kind 一个 [Serializable] 类；字段只用 JsonUtility 认得的类型）
    // ==================================================================

    [Serializable] public class MsgChallenge { public string nonce; }                    // H→C
    [Serializable] public class MsgProof
    {
        public int protoVer; public string gameVer; public string name; public string proof;
    }
    [Serializable] public class MsgAck
    {
        public bool ok; public string reason;
        /// <summary>主机发的**对局钥匙** —— 重连时凭它认人（§5·6）。⚠️ **不进日志**。</summary>
        public string sessionToken;
    }
    [Serializable] public class MsgDeck
    {
        public string deckJson;     // JsonUtility 化的 PlayerDeck
        public string gameMode;     // "Classic" / "Skirmish"
        /// <summary>督军阵营 —— ⚠️ `PlayerDeck` **自己不带阵营字段**，而主机要用它定战场（`ArenaByArmy`）
        /// ⇒ 由**交牌那一方**报上来（它本来就查得到），别让主机去猜。</summary>
        public string faction;
    }
    [Serializable] public class MsgStart
    {
        public int seed;            // 🔴 **主机抽、两端同一枚**（原版也是这么做的，见正本 §四·4）
        public string mode;         // "Classic" / "Skirmish"（取值照 `GameMode` 枚举名）
        public int myDeckSeat;      // 🔴 **零消费方**（全仓只有 `BattleDriver` 赋 0、没有任何读）—— 座位从 2026-09-26 起是**绝对的**，⛔ 别照「发送方视角」理解它
        public string hostDeckJson;    // 主机那副（**绝对座位 0**）；客机也由 `NetBattle.FromStart` 按座位取，⛔ 不做任何「座位翻译」（那套已作废，见文件头）
        public string clientDeckJson;
        public string arena;        // 战场场景名（**由主机定** —— 正本 §二·5）
        public string myName, foeName;
        /// <summary>先手是**几号座位**（**绝对编号**：0 = 主机）。⚠️ **客机直接用这个值，不翻**
        /// （`NetBattle.cs` 的 `NetPendingBattle.FromStart`：`FirstSeat = s.hostFirst`）。
        /// ⛔ 原文这里写「客机那边 `firstSeat = 1 - hostFirst`」—— 那是**镜像时代**的话，已作废（§5·3）。</summary>
        public int hostFirst;
        public string hostFaction, clientFaction;
    }
    /// <summary>一条动作。字段是 <see cref="AiAction"/> 的**镜像**。
    /// 🔴 **2026-09-26 订正本篇注释**：原文写「`HandInst` 换成 `handId` ⇒ 引用哪一份牌**只能**用它、
    /// **不能**用下标」—— **与实际代码相反，别照它改**。真正跑的是 **`handIdx`**
    /// （`NetApply.Apply` → `RuleCore.PlayCard(ctx, actorSeat, m.handIdx, m.slot)`）。
    /// 为什么下标就够：**两端跑同一套绝对座位** + 动作**逐条同步**（一个回合只有一个行动方）
    /// ⇒ 两端手牌**逐位相同**，下标一样准。详见 `资料/联机P2P_设计与交接.md` §5·3。</summary>
    [Serializable]
    public class MsgAction
    {
        public int seq;             // **主机定序**的序号（客机的动作由主机回填）
        /// <summary>谁做的 —— **绝对座位**（`0` = 主机 · `1` = 客机；两端同一套编号）。</summary>
        public int actor;
        public int kind;            // `AiActionKind`（PlayCard=0 / AttackMelee=1 / AttackRanged=2 / EndTurn=4 / ActiveAbility=5 / CollectWaystone=6）
        /// <summary>🔴 **只写不读的死字段**（2026-09-26 核过：全仓 **0 个读取点**）。留着只是让这条消息
        /// 的形状和 `AiAction` 对得上；**引用哪一份牌请用 `handIdx`**（见类注释）。</summary>
        public int handId = -1;
        /// <summary>**这才是真正在用的那个**：出牌时的手牌下标（两端逐位相同，见类注释）。</summary>
        public int handIdx = -1;
        public int slot = -1;
        /// <summary>**绝对座位** —— ⛔ 原文写「发送方视角的座位」，那是**镜像时代**的话，已作废（§5·3）。</summary>
        public int targetP = -1;
        public int targetSlot = -1;
        public bool ranged;
        public string altKeyword;
        /// <summary>面板答案（`ctx.ChoosePicks`）—— **顺序必须和结算顺序一致**（少发一格会让后面每一处**全部错位且不报错**）。</summary>
        public int[] picks;
        /// <summary>选牌那一族的 `CardDef.Id`（`ctx.ChooseCardIds`，与 `picks` **同序同长**）。</summary>
        public string[] pickIds;
        /// <summary>换牌（`kind = 100`）用：换掉的手牌下标。</summary>
        public int[] marks;
        /// <summary>🔴 **接收方是不是已经本地落地过了**。乐观执行的那条路：
        /// 客机的动作**本机已经落过**（自己那一回合只有自己在动）⇒ 主机的广播要带 `true`，
        /// 客机收到就只认领 `seq`、**别做第二遍**。换牌那几条是主机定序的 ⇒ 一律 `false`。</summary>
        public bool preApplied;
        /// <summary>🆕 2026-09-30：**进攻卡 / 防御卡**那两条（`kind = 102/103`）用 —— 选中的槽号。
        /// 进攻卡 = 卡槽 `idx`（`&lt; 0` = 「不使用进攻卡」那张）；防御卡 = **闸过那份**
        /// `BattleDriver.DefensiveChoicesOwned(faction)` 里的下标。
        /// 🔴 **2026-10-13（`A1744`）就地订正**：原来这里写「防御卡 = `DefensiveChoices` 里的下标」—— **不再准确**。
        ///   那两份列表**长度可以不同**（未筛那份恒 3 张、筛过的按等级少几张 —— 见 `BattleDriver.DefensiveChoices` / `DefensiveChoicesOwned` 的成对 doc；⚠️ `BattleDriver.cs` 行号会漂，2026-10-19 现核 = `:5392-5398` / `:5520`），
        ///   而发送侧已经改成用**闸过那份**（弹面板 `OpenDefensiveChoicePanel`，现核 `:5320` · 发送 `OnDefensiveDone`，现核 `:5591`）。
        ///   ⚠️ **接收方并不拿这个下标反查卡**（`NetApply.cs:56` 只记槽号，卡走 `defId` —— 见 `:59-60`）
        ///   ⇒ 这一格填错**不会 desync**，只会让日志 / 录像里的槽号对不上。</summary>
        public int envSlot = -1;
        /// <summary>🆕 进攻卡那一条用：选中的环境 SO 名（`RuleCore.ChooseOffensiveCard` 的第四实参）。</summary>
        public string envSO;
        /// <summary>🆕 防御卡那一条用：选中那张卡的 `CardDef.Id` —— 两端各自从 `ctx.CardPool` 查回来
        /// （防御卡要**换进后手方手牌**，只同步下标的话重连重放会缺这一步）。</summary>
        public string defId;
    }

    /// <summary>线上动作的 `kind` 取值域（前 6 个是 `AiActionKind` 的原值，别改）。</summary>
    public static class NetActionKind
    {
        public const int PlayCard = 0, AttackMelee = 1, AttackRanged = 2, EndTurn = 4,
                         ActiveAbility = 5, CollectWaystone = 6;
        /// <summary>换牌（`marks` 里是换掉的手牌下标）—— **只能由主机定序后下发**（它掷 `ctx.Rng`）。</summary>
        public const int Mulligan = 100;
        /// <summary>换牌阶段结束（`RuleCore.EndMulligan` + 真正开打）。</summary>
        public const int MulliganDone = 101;
        /// <summary>🆕 2026-09-30：**先手方选进攻卡**（环境效果卡）。
        ///
        /// 🔴 **这是我们自己的设计，不是复刻** —— 原版两台**各弹各的面板、没有这条同步包**
        /// （判据 → `资料/普查产出_0930/进攻防御卡_面板语义.md` §八「本轮没读」）。
        /// 我们这边**必须有**：环境是按 `Ctx.OffensiveEnvSO` 应用的，两端不一致就会各切各的环境。
        /// 走**现成的动作流**（`Action`/`Applied` 那两条），所以顺带就带上了主机定序、重连重放、录像
        /// —— 不另开一路（`CLAUDE.md` §三：新增会改引擎状态的动作路径，必须走记账口）。
        /// 判据全在 `RuleCore.EmitBloodThirst` 那个文件邻居 `RuleCore.ChooseOffensiveCard` 上。</summary>
        public const int OffensivePick = 102;   // 🔶 自己设计（非原版）：后手方选进攻/防御卡的同步包（`OffensivePick = 102` / `DefensivePick = 103`） · 原版两台各弹各的面板、没有这条同步包，判据为空 · TODO(谁定的)
        /// <summary>🆕 2026-09-30：**后手方选防御卡**（同上一族，我们自己设计的同步）。
        /// 它改的是**后手方手牌**（换进一张）⇒ 不同步的话两端手牌会不同、指纹迟早不一致。</summary>
        public const int DefensivePick = 103;
    }
    [Serializable] public class MsgActionList { public List<MsgAction> items = new List<MsgAction>(); }   // JsonUtility 不能直接序列化 List<T> 根
    [Serializable] public class MsgReject { public int seq; public string reason; }
    [Serializable] public class MsgMulligan { public int[] indices; }
    /// <summary>换牌阶段的**定序结果**（主机下发）。座位是**主机视角**：0 = 主机、1 = 客机。
    /// 🔴 两端必须**按同一顺序**调 `RuleCore.Mulligan`（它掷 Rng）⇒ 主机先算完再发。</summary>
    [Serializable] public class MsgMulliganSync { public int[] seat0Marks; public int[] seat1Marks; }
    [Serializable] public class MsgResign { }
    /// <summary>🆕 **2026-10-26（批次 6 · G15）：「我发现这一局已经打岔了」**（原版
    /// `BattleCommsManager.SendDesync()` 那条 Photon RPC，`PUNReceiveEnemyDesync`）。
    ///
    /// <para>**为什么要有它**（判据 = 原版那一整条链，逐句）：原版 `BattleManager.CancelMatchWithVictory(errorLog)`
    /// 在**本机**解析/结算不出对面的动作时做四件 —— 记 `desyncCancelledGame` · 记「本站胜」·
    /// **`commsManager.SendDesync()`**（告诉对面）· 触发 `OnOpponentDesync(true)` ⇒ 弹窗
    /// `Battle/EnemySyncError` ⇒ 点 OK 走 `BattleManager.CancelMatch(true)`（= `DeadHero(对面, Cancelled=5)` ⇒ **本机胜**）。
    /// 收方 `PUNReceiveEnemyDesync` → `BattleManager.ReceiveEnemyDesync` → **只认账**
    /// （`RecordMatchWonByCancelEnemy` = 记「对面靠取消赢了我」），**不弹窗、不判自己**。
    /// ⇒ 这条消息就是那一下「告诉对面」。</para>
    ///
    /// <para>⚠️ **与原版的差别（如实记）**：原版收方只写服务器的那一笔账、**本地对局不动**（谁赢由服务端记）。
    /// 我们本地没有服务器 ⇒ 收方必须自己也把这一局收尾（否则这一局永远挂着）——
    /// 做法 = **判本机负**、理由码 `Cancelled`(5)（等价于原版 `DeadHero(本机, 5)` 的本地版），
    /// **不弹窗**（照原版收方那一档）。两端因此报的是**同一个弃权座位**（同 `A914` 那条口径）。</para>
    ///
    /// <para>⚠️ **兼容口径同 `Chat` / `MatchFinished`**：只**新增一个 kind**、不动任何既有消息格式
    /// ⇒ **不 +`NetProtocol.Version`**；旧端收到会走 `NetBattle.Dispatch` 末尾「不认识的联机消息」出声丢掉。</para>
    /// <para>载荷今天**为空**：原版的 RPC 也不带参数（`SendDesync` 的 `parameters = Array.Empty&lt;object&gt;()`）。
    /// ⛔ 别顺手把「对端的理由串」塞进来 —— 原版那条 RPC 就没有，理由只进本机日志。</para></summary>
    [Serializable] public class MsgDesync { }
    /// <summary>🆕 **取消这一局的匹配**（A1）。⚠️ **我们设计的，不是复刻** —— 见 `NetKind.MatchCancel`。
    /// `reason` 只进日志（给对面看的那句话固定，不渲染对面传来的任意文本）。</summary>
    [Serializable] public class MsgMatchCancel { public string reason; }
    /// <summary>🆕 **2026-10-25（执行代理 `E4` · 联机账 162）：两端模式不一致**。
    ///
    /// <para>**为什么要有它**（判据逐条现读 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SearchOpponentManager.cs`）：
    /// 原版 `PUNReceiveStartGame`（`:1700-1770`）收开局包时比 `text2 != text3`（收到的模式 vs 本机期望的模式），
    /// 不等就 `CustomDebug.LogError("Received start game with wrong playmode …")` 再
    /// **`photonView.RPC("PUNReceiveWrongPlayMode", PhotonTargets.All, …)`**（`:1766`）；
    /// 收到该 RPC 的一端（`:1915-1985`）打印一行之后 —— **若 `opponentDesired` 点名的是自己** ——
    /// 做 `BattleNetworkManager.LeaveBattle()` + `CancelBattleSearch()`。
    /// 🔴 **原版那一下【不拆连接】**（只离开战斗房间 + 取消搜索）⇒ 我们以前那套
    /// 「主机发现不一致就 `sess.Close(true,…)` 关掉整条会话、客机必须重新输房间码」**比原版重**，
    /// 而且客机只能看到一句通用的「对面离开了」，不知道是模式不对。</para>
    ///
    /// <para>**我们的形态**：一条独立消息（原版那是一条独立 RPC，不是 `start` 的一个分支）。
    /// `playMode` = 我方期望的模式 · `expected` = 我看到的对面模式（**只进日志与弹窗**，不参与判断）。</para>
    ///
    /// <para>⚠️ 与 `Chat` / `MatchFinished` / `CardSeen` 同一条兼容口径：**只新增一个 kind、
    /// 不动任何既有消息的格式 ⇒ 不 +`NetProtocol.Version`**；旧端收到会走 `PumpLobby` 的 `default:`
    /// 「大厅阶段收到不认识的消息」**出声**并丢掉（不崩、不影响这一局）。</para></summary>
    [Serializable] public class MsgModeMismatch { public string playMode; public string expected; }
    /// <summary>🆕 **2026-10-25（执行代理 `CH`）：战斗内「督军聊天」那一条**。
    ///
    /// <para>**形状照原版**：`BattleCommsManager__SendChatMessage.c:20-37` 发的是
    /// **一个 int（台词枚举号）装箱进 `object[]`** 的 Photon RPC ⇒ 语义上线上只有那一个数。
    /// 我们再带一格 <see cref="seat"/> = **发送方座位**（绝对编号），因为
    /// `PUNReceiveChatMessage.c:20-26` 那条「**是谁说的**」的过滤在原版是靠 Photon 的
    /// 发送者标识做的，P2P 这边没有那一层，必须自己在消息里带上（⛔ 别拿「收到就是对面说的」代替：
    /// 那条判据要在**测回显**时也成立）。</para>
    ///
    /// <para>⛔ **不带文本**（原版就不带：台词是**词条 + 语音资产**，两端各自按自己的语言取词）、
    /// ⛔ **不带时长**（动作流水里那个 clip 长度是**收侧自己那颗 clip** 的长度，
    /// 见 `NetChat` 文件头「④ 动作流水」那一格 —— 收侧按**它自己那颗 clip** 的长度记）、⛔ **不带 isEnemy**（= `seat != 本地座位`，一个数就够）。</para></summary>
    [Serializable] public class MsgChat
    {
        /// <summary>谁说的 —— **绝对座位**（`0` = 主机 · `1` = 客机；两端同一套编号，⛔ 不翻）。</summary>
        public int seat = -1;
        /// <summary>台词枚举号（原版 `ChatMessage`：5…10 = 6 颗钮那一档，见 `NetChat.FirstButtonChatId`）。</summary>
        public int chatId = -1;
    }

    /// <summary>🆕 2026-10-25：对局结束（无载荷）。原版 `PUNReceiveMatchFinished` 只带 `PhotonMessageInfo`。</summary>
    [Serializable] public class MsgMatchFinished { }

    /// <summary>🆕 2026-10-25：对面查看了某张牌。原版参数 = `uniqueInGameID`（**卡在局内的唯一 id**，
    /// 不是卡 id；与 `NetKind.Action` 里那套 id 同源）。</summary>
    [Serializable] public class MsgCardSeen { public int cardId = -1; }

    /// <summary>🆕 2026-10-25：对面手牌换位。原版参数 = `(uniqueInGameID, newIndex)`。</summary>
    [Serializable] public class MsgCardReorder { public int cardId = -1; public int index = -1; }
    [Serializable] public class MsgHash { public int turn; public int hash; }
    [Serializable] public class MsgPing { public long t; }
    [Serializable] public class MsgBye { public string reason; }
    [Serializable] public class MsgReconnect { public string sessionToken; public int lastSeq; }
    [Serializable] public class MsgResume
    {
        public MsgStart start;
        /// <summary>**权威动作流**（主机内存里那份 = 这一局的完整历史）。
        /// 客机收到后**丢掉本地状态、从种子重建、按序全量重放** —— 不许增量补（正本 §5·6 第 4 条）。</summary>
        public string actionsJson;
    }

    /// <summary>消息 kind 常量（字符串是为了日志能直读；⚠️ 改名 = 改协议，两端必须一起改）。</summary>
    public static class NetKind
    {
        public const string Challenge = "hello.challenge";
        public const string Proof     = "hello.proof";
        public const string Ack       = "hello.ack";
        public const string Deck      = "deck";
        public const string Start     = "start";
        /// <summary>🆕 **2026-10-03：取消这一局的匹配**（`项目任务.md` §三 第 29 条 **A1**）。
        /// ⚠️ **这是我们设计的，不是复刻** —— 原版的「取消」是 `MatchMakerManager.CancelSearch`
        /// （**服务端**撤单，`SearchingOpponentWindow__CancelMatchMatchmaking`），P2P 这边没有服务端
        /// ⇒ 只能自己发一条「我不打了」。**只在大厅阶段有效**（`MsgStart` 一发出去就算开局了）。</summary>
        public const string MatchCancel = "match.cancel";
        /// <summary>🆕 **2026-10-25（执行代理 `E4` · 联机账 162）：两端模式不一致**（原版
        /// `PUNReceiveWrongPlayMode` RPC，`SearchOpponentManager.cs:1915-1985` / 触发点 `:1766`）。
        /// 载荷 = <see cref="MsgModeMismatch"/>。**两端都会发**（谁先看出不一致谁发）：
        /// 主机在 `HostStartMatch` 比 `_foeMode`，客机在 `PumpLobby` 的 `case NetKind.Start` 比 `MsgStart.mode`
        /// —— 后者是**安全口径**（客机不许只信主机单方面声明）。收到就**撤本地这一局、连接留着**，
        /// ⛔ **不 `Close`**（原版只 `LeaveBattle` + `CancelBattleSearch`）。
        /// ⚠️ 同 `Chat`/`MatchFinished`：**新增 kind 不改既有消息格式 ⇒ 不 +`NetProtocol.Version`**。</summary>
        public const string ModeMismatch = "match.mode-mismatch";
        public const string Action    = "action";
        public const string Applied   = "applied";
        public const string Reject    = "reject";
        public const string Mulligan  = "mulligan";
        public const string MulliganSync = "mulligan.sync";
        public const string Resign    = "resign";
        /// <summary>🆕 **2026-10-26（批次 6 · G15）：本机发现「这一局已经打岔了」** —— 原版
        /// `BattleCommsManager.SendDesync()` 那条 RPC（`PUNReceiveEnemyDesync`）。载荷 = <see cref="MsgDesync"/>（空）。
        /// <para>语义（判据逐句 → <see cref="MsgDesync"/> 的 doc）：**谁是发现方、谁就 claim 胜利**
        /// （原版 `CancelMatchWithVictory`）；收方**认账判自己负**（原版 `ReceiveEnemyDesync` 的
        /// `RecordMatchWonByCancelEnemy` 的本地等价）。原版对端 desync 的**唯一**触发源就是它。</para>
        /// ⚠️ 与 `Chat`/`MatchFinished`/`ModeMismatch` 同一条兼容口径：**新增 kind、不改既有消息格式
        /// ⇒ 不 +`NetProtocol.Version`**。</summary>
        public const string Desync    = "desync";
        /// <summary>🆕 **2026-10-25（执行代理 `CH`）**：战斗内「督军聊天」（原版 `BattleCommsManager`
        /// 那条单 int RPC）。⚠️ **只在对局中发**，不落地、不改局面、不参与指纹。
        /// <para>🔴 **为什么不 +`NetProtocol.Version`**（照同族先例 `NetWireText` 那段的口径）：
        /// 我们没有改**任何既有消息**的格式，只是**新增一个 kind** —— 旧端收到它会走
        /// `NetBattle.Dispatch` 末尾那句「不认识的联机消息」**出声**并丢掉（不崩、不影响对局）。
        /// 为了一条**表现层**旁路把整条握手判死（版本不等时主机会直接拒绝）**代价不对等**。
        /// ⚠️ 若以后要给 `MsgChat` 加字段（改形状），那就**必须** +版本（那才是「改消息格式」）。</para></summary>
        public const string Chat      = "chat";
        /// <summary>🆕 **2026-10-25（执行代理 D）**：**对局结束**（原版 `BattleCommsManager.SendMatchFinished`
        /// → `PUNReceiveMatchFinished` → `BattleManager.ReceiveMatchFinished`）。**无载荷**。
        /// <para>语义：收尾方发它；**收方**据此把「等对手最后几条动作」的窗口打开（见
        /// `NetBattle.TickFinishWait` 的判据）。⚠️ 它**不参与局面**、**不改指纹**、**不判胜负**。</para>
        /// ⚠️ 与 `Chat` 同一条兼容口径：**新增 kind、不改任何既有消息格式 ⇒ 不 +`NetProtocol.Version`**；
        /// 旧端收到会走 `NetBattle.Dispatch` 末尾「不认识的联机消息」**出声**并丢掉（不崩、不影响对局）。</summary>
        public const string MatchFinished = "match.finished";
        /// <summary>🆕 2026-10-25：**对面在手里查看了某张牌**（原版 `SendCardSeen(uniqueInGameID)` →
        /// `BattleManager.ShowEnemyCardSeen`）。**纯表现**，不参与局面。</summary>
        public const string CardSeen   = "card.seen";
        /// <summary>🆕 2026-10-25：**对面手牌换位**（原版 `SendCardReorder(uniqueInGameID, newIndex)` →
        /// `BattleManager.ShowEnemyHandReorder`）。**纯表现**，不参与局面。</summary>
        public const string CardReorder = "card.reorder";
        public const string Hash      = "hash";
        public const string Ping      = "ping";
        public const string Pong      = "pong";
        public const string Bye       = "bye";
        public const string Reconnect = "reconnect";
        public const string Resume    = "resume";
    }

    /// <summary>🆕 🔴 **A1038 / P6d（2026-10-19）：走线文案的「词条键 ⇄ 本机语言」编解码。**
    ///
    /// <para>**为什么要它**：`MsgBye.reason` / `MsgReject.reason` / `MsgAck.reason` 是**穿过 TCP
    /// 到对端屏幕上**的串。P6d 之前它们装的是**已经渲染好的中文整句** ⇒ 等于把**发送方**的界面语言
    /// 灌到**接收方**的界面上（接收方是英文档也照印中文）。现在线上只发**词条键**
    /// （要带参数就再跟一段），**由收侧按它自己的语言取词**。</para>
    ///
    /// <para>**编码格式**：`键` 或 `键 + '|' + 参数1 + '|' + 参数2 …`。
    /// ⛔ **线上一个自然语言字符都不发**。分隔符取 `'|'`（ASCII 可打印 ⇒ `JsonUtility`/UTF-8
    /// 都不会动它；词条键里本来就不可能出现它 —— 键全是 `[A-Za-z/]`）。</para>
    ///
    /// <para>**为什么不 +`NetProtocol.Version`**（主对话 2026-10-19 裁定）：照**同族先例**
    /// `NetPendingBattle.PlayMode` + `PlayModeNames.Parse`（`Net/NetBattle.cs` + `RuleEngine/Core/GameplayVariables.cs`）
    /// —— 那一条也换了线上字符串的编码、也**没有** +版本号，靠**收侧出声兜底**。两端的兼容表现：
    ///   · **旧端收新键** ⇒ `HasEntry` 假 ⇒ **原样印出键名**（键名是纯 ASCII，长度 ≤ `MaxPeerTextChars`(40)
    ///     ⇒ 连 `ClampPeerText` 都不会截）、
    ///     不崩、不忽略（`NetSession` 的 `Bye`/`Ack` 与 `NetBattle` 的 `Reject` 三条收包路都验过）；
    ///   · **新端收旧中文串** ⇒ `HasEntry` 假 ⇒ **原样回显** ⇒ **显示恰好正确**（这是白赚的兼容）。
    /// ⛔ **别改成「不认识的串一律落一句本地固定话」** —— 那会让新端**丢掉对面那句话**，
    /// 而且 `Editor/NetSelfTest.cs` 的 M⑳ 与 `Editor/NetBattleTest.cs` 的「对面离开」那两条断言
    /// 断的正是「那条话里带着对面报的理由」，会当场红。</para>
    ///
    /// <para>⚠️ **钳位次序是硬的**：**先 `NetSession.ClampPeerText`（钳【对端可控的原串】）再 `Unpack`（取词）**。
    /// 反过来的话，取完词的那句是**我们自己的文案**（英文列可到 90+ 字），拿 `MaxPeerTextChars`(40)
    /// 去钳它会把我们自己的话截掉 —— 而那个 40 的判据本来是「**对端塞进来的**那一段不该比整行提示
    /// 还长」（判据的出处与「为什么恰好取 40」见下面 `MaxPeerTextChars` 的 doc）。</para>
    /// </summary>
    public static class NetWireText
    {
        /// <summary>参数分隔符。取值理由见类注释（键里不可能有它）。</summary>
        public const char Sep = '|';

        /// <summary>发送侧：词条键（可选带参数）→ 线上那一串。**⛔ 绝不发已渲染的文本。**</summary>
        public static string Pack(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (args == null || args.Length == 0) return key;
            var sb = new StringBuilder(key);
            for (int i = 0; i < args.Length; i++) { sb.Append(Sep); sb.Append(args[i]); }
            return sb.ToString();
        }

        /// <summary>收侧：线上那一串 → **按本机语言**取词（认不出 = 原样回显，见类注释）。
        /// ⚠️ 调用方要**先 `NetSession.ClampPeerText`**（次序见类注释）。</summary>
        public static string Unpack(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int at = s.IndexOf(Sep);
            string key = at < 0 ? s : s.Substring(0, at);
            // 认不出 = 旧端发来的自然语言 / `RuleCodes.Describe` 那种本机整句 ⇒ **原样回显**（白赚的兼容）
            if (!Loc.HasEntry(key)) return s;
            string tpl = Loc.T(key);
            if (at < 0) return tpl;
            string[] parts = s.Substring(at + 1).Split(Sep);
            try { return string.Format(tpl, (object[])parts); }
            catch (FormatException e)
            {
                // 占位符个数对不上（对面伪造 / 原串被钳断）⇒ 退回模板 + **出声**，
                // ⛔ 不许让异常抛到收包路径上（那会把线程打死、而且一个字都不说）。
                Debug.LogWarning($"[Net] 走线文案 `{key}` 的参数个数对不上模板（{e.Message}）"
                               + $" ⇒ 只印模板本身。这一串来自对面：「{s}」");
                return tpl;
            }
        }
    }

    public static class NetProtocol
    {
        /// <summary>协议版本。🔴 **改消息格式就必须 +1** —— 两端版本不等时主机**拒绝**并说明理由（不许静默兼容）。
        /// 🔴 **2026-10-01：1 → 2** —— 不是因为消息格式变了，而是因为**引擎语义变了**：
        /// 棋盘从「固定 9 格、可留洞」改成**连续无洞**模型（`RuleEngine/Core/BoardSlots.cs`），
        /// 同一串动作在两版引擎里会落到**不同的格号** ⇒ 新旧版互通必然 desync，必须挡住。
        /// 🔴 **2026-10-25：2 → 3** —— 同样不是消息格式变了，是**引擎语义变了**：
        /// 属性模型 Stage 2 之后，`UnitState.Attack` / `RangedAttack` / `MaxHealth` 读的是**投影**
        /// （`CurrentMeleeAttack` / `CurrentRangeAttack` / `CurrentMaxHealthProcessed`），
        /// 而局面哈希（`PlayerHash` 那三行 `Warlord.Attack/RangedAttack/Armor`）读的正是它们
        /// ⇒ **同一串动作在两版引擎里可能算出不同的哈希**（`blind ⇒ 远程 0`、`setXxx` 覆盖、
        /// 负攻值 `clamp0` 这三处是新旧分界）⇒ 新旧版互通必然 desync，必须挡住。
        /// ⚠️ **值本身今天多半一样**（池内没有带 `blind`/`set` 修饰的督军局面）—— 但**语义边界**变了，
        /// 不能靠「碰巧一样」互通：那类差异只会在特定局面上现形，表现为**打到一半 desync**。
        /// ⛔ **别把版本号改回去**：它挡的是「新旧引擎混连」，不是某一个具体字段。</summary>
        public const int Version = 3;

        /// <summary>单帧上限（防对面塞个巨大帧把内存吃光）。1000 条动作的 `resume` 也就几十 KB。</summary>
        public const int MaxFrame = 4 * 1024 * 1024;
        public const int HeaderBytes = 4;

        /// <summary>🆕 🔴 **A961（2026-10-18）：对端可控的文本进任何一个界面之前，一律钳到这个长度**
        /// （超出 = 截断 + 一个 `…`）。闸只一处 = `NetSession.ClampPeerText`；四个入口：
        /// `MsgBye.reason`（`NetSession.Handle` 的 `case NetKind.Bye`）· `MsgProof.name` → `PeerName`
        /// （同文件 `case NetKind.Proof`）· `MsgAck.reason`（同文件 `case NetKind.Ack`）·
        /// `MsgReject.reason`（`NetBattle.Dispatch` 的 `case NetKind.Reject`，它复用同一个 `ClampPeerText`）。
        /// <para>🔴 **2026-10-19（P6d）这四条路都多了一步 `NetWireText.Unpack`（取词），次序是
        /// 【先钳、后取词】** —— 理由（反过来的话会截到我们自己的文案）见 <see cref="NetWireText"/> 的类注释。
        /// ⚠️ 原来本段写的是三处、且行号写死（`:428`/`:336`/`:362`）—— **那些行号早就漂了**
        /// （2026-10-19 现读已不是那三行）⇒ 就地改成**符号名索引**（铁律 5；⛔ 以后别在本文件写死行号）。
        /// 🔴 **2026-10-20（`A1123`）**：紧隔壁那段「取 40 的判据」当时**自己破了这个例** ——
        /// 还留着一条硬行号 `Shell/SearchingMatchPopup.cs:408`（也已落空）⇒ **已改成符号名**
        /// `Shell/SearchingMatchPopup.HintLineMaxChars`。**本文件剩余写死行号：0 处。**</para>
        ///
        /// <para>**取 40 的判据** = 提示行自己的那条预算：`Shell/SearchingMatchPopup.HintLineMaxChars`
        /// （= **40 个汉字**，即 `HintLineMaxWidth ÷ 2` —— 那把尺子的总预算是 **80 个半宽字位**；
        /// `B17` 已解；超长只 `LogWarning`、**照画**）——「对端塞进来的那一段」
        /// 不该比**整行提示**的预算还长。
        /// 🔴 **2026-10-19（第九会话 · `A1131`）就地补口径（铁律 5）**：提示行的尺子自 `A1083` 起已经不是
        ///   「字符数 ≤ 40」而是「`HintLineWidth(句) > HintLineMaxWidth` = **80 个半宽字位**」（中英混排按字算）。
        ///   这里取 40 是**照【汉字】那一档**取的（40 个全宽字 = 80 位，两档同值、所以数值不用改），
        ///   ⛔ 别把它读成「整行提示统共只放得下 40 个字位」。
        /// 那两个常量都落在 `Shell/`，这里不引它们（`Net/` 不该依赖 `Shell/`），
        /// **数值照抄、出处写在这儿**。</para>
        ///
        /// <para>⚠️ **这是我们自拟的口径，不是复刻**（铁律 3 要求如实标出）：原版**没有**对端可控文本
        /// 进界面的先例 —— 掉线文案全是**固定词条**（`Battle/HUD/WaitOpponentConnectionMsg`，
        /// 13 个战场场景各一份），玩家名的长度约束落在 **PlayFab 服务端**
        /// （`ChooseNamePopup__ClickChooseName.c:33` 拿到就直接 `SetPlayfabDisplayName`，客户端一次都不校验）。
        /// ⇒「截断」与「干脆不带对端理由」**都是我们的选择**；这里选**截断 + 省略号**（保留信息，又不给上限）。</para></summary>
        /// <para>🔴 **2026-10-17（`A1317` 裁定）：这个 40【不跟着提示行的尺子动】** ——
        /// 它管的是**线上那串原始文本**，而线上发的是 `NetWireText.Pack(键, 参数)`（见 `NetWireText`）：
        /// 键本身就有 **32 字符**（例 `Settings/Online/Wire/NotYourTurn`），再带参数往往 **35–40**。
        /// 把它压到提示行那 80 个半宽字位以内（≈ 10 个汉字）**会把键截断** ⇒ `Unpack` 认不出、
        /// 退化成「把键名原样印给玩家」（静默错）。⇒ **协议层保持 40；提示行那两条夹着对端 `body`
        /// 的句子照旧可能超 80 位 —— 那是个【诊断阈值】，不是硬上限：超了只 `LogWarning`、照画，
        /// 字会自动缩小（3 行框内），⛔ 不截断（截了就是把玩家该看的话吃掉）。
        /// 完整裁定与两侧理由 → `Shell/SearchingMatchPopup` 的 `HintLineMaxWidth` 那一节。</para>
        ///
        /// <para>🔴 **2026-10-17（`A1317` 同批）：名字与理由【拆成两个钳】** —— 名字不走
        /// `NetWireText.Pack`（就是玩家自己的显示名），拿 40 去钳它没有依据；原版把长度交给
        /// **PlayFab 服务端**（见下一条 `⚠️`），我们本机的改名框上限是
        /// `Shell/ProfileTab.MaxNameLen = 24` ⇒ 名字这一档取 **24** 与它对齐。</para></summary>
        public const int MaxPeerTextChars = 40;

        /// <summary>🆕 **2026-10-17（`A1317`）：对端**名字**的钳**（只用于 `MsgProof.name` → `PeerName`）。
        /// 取值 **25** = 我们本机改名框的上限（`Shell/ProfileTab.MaxNameLen`；原版真值在 PlayFab 服务端，
        /// 客户端一次都不校验 —— 判据见 `MaxPeerTextChars` 末段那条 `⚠️`）。⛔ 别拿理由那个 40 来钳名字。
        /// <para>🔴 **2026-10-24 就地订正（铁律 5 · 执行代理 `E12` 现核）**：这里原来是 **24**，
        /// 依据是当时的 `MaxNameLen = 24`。E12 按反编译真值把改名框改成 **25**
        /// （`ChangeNameWindow__ValidateInput.c`：`Substring(0, 0x19)` = 25、`len < 3` 拒），
        /// 两处**必须同值** —— 否则 25 字的名字过网会被静默截成 24。</para></summary>
        public const int MaxPeerNameChars = 25;

        // ---- 组帧 / 拆帧：4 字节小端长度 + UTF8(JSON) ----

        public static byte[] Frame(string kind, string payloadJson)
        {
            string json = JsonUtility.ToJson(new Envelope { kind = kind, payload = payloadJson ?? "" });
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] buf = new byte[HeaderBytes + body.Length];
            buf[0] = (byte)(body.Length & 0xFF);
            buf[1] = (byte)((body.Length >> 8) & 0xFF);
            buf[2] = (byte)((body.Length >> 16) & 0xFF);
            buf[3] = (byte)((body.Length >> 24) & 0xFF);
            Buffer.BlockCopy(body, 0, buf, HeaderBytes, body.Length);
            return buf;
        }

        public static int FrameLength(byte[] header, int offset)
        {
            return header[offset] | (header[offset + 1] << 8) | (header[offset + 2] << 16) | (header[offset + 3] << 24);
        }

        /// <summary>把一帧 body 解成信封。解不出来返回 null（**调用方要出声**）。</summary>
        public static Envelope Parse(byte[] body, int len)
        {
            try { return JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(body, 0, len)); }
            catch (Exception e) { Debug.LogWarning("[Net] 帧解不出来：" + e.Message); return null; }
        }

        public static string Pack<T>(T msg) { return JsonUtility.ToJson(msg); }
        public static T Unpack<T>(string payload) where T : class
        {
            if (string.IsNullOrEmpty(payload)) return null;
            try { return JsonUtility.FromJson<T>(payload); }
            catch (Exception e) { Debug.LogWarning("[Net] 载荷解不出来（" + typeof(T).Name + "）：" + e.Message); return null; }
        }

        // ---- 座位翻译（**唯一一份规则**，别在两处各判一次 —— 正本 §5·3）----

        /// <summary>把「发送方视角」的座位翻过来。
        /// 🔴 **2026-09-26 起联机对局不再用它**（改成了「两端同一套**绝对座位**」——
        /// 见下面 `Fingerprint` 的注释：镜像做法在引擎层面**不成立**，实测栽在 `Unstable` 上）。
        /// 留着是因为协议层它本身没错，将来若有别的用途别重写一份。</summary>
        public static int Seat(int pFromSender) { return pFromSender < 0 ? -1 : 1 - pFromSender; }

        /// <summary>`AiAction` → 线上格式（🔴 **座位是绝对编号**：`actor/slot/targetP` 一律照原值，⛔ 不翻 —— 镜像那套已作废）。</summary>
        public static MsgAction ToWire(AiAction a, BattleContext ctx, int seq)
        {
            var m = new MsgAction
            {
                seq = seq,
                kind = (int)a.Kind,
                handIdx = a.HandIdx,
                handId = a.HandInst != null ? a.HandInst.Id : -1,
                slot = a.Slot,
                targetP = a.TargetP,
                targetSlot = a.TargetSlot,
                ranged = a.Ranged,
                altKeyword = a.AltKeyword,
            };
            if (ctx != null)
            {
                m.picks = ctx.ChoosePicks.Count > 0 ? ctx.ChoosePicks.ToArray() : null;
                m.pickIds = ctx.ChooseCardIds.Count > 0 ? ctx.ChooseCardIds.ToArray() : null;
            }
            return m;
        }

        /// <summary>`MsgAction` → `AiAction`（**座位不翻** —— 两端跑的是同一套**绝对**座位，见 §5·3）。
        /// 🔴 **2026-09-26 订正**：原文写「**接收方视角的座位翻译在这里做，只此一处**」，而且真的调
        /// `Seat(m.targetP)` 去翻 —— 那是**镜像时代**的做法，**已作废**（镜像在引擎层面不成立，
        /// 见 `Fingerprint` 的注释）。⚠️ 真实的落地路径是 `NetApply.Apply`（它直接用 `m.handIdx` /
        /// `m.targetP`）；**本方法只被 `NetSelfTest` 用**。⇒ **两条路径的语义必须一致，改一条就得改另一条。**</summary>
        public static AiAction FromWire(MsgAction m)
        {
            if (m == null) return null;
            return new AiAction
            {
                Kind = (AiActionKind)m.kind,
                HandIdx = m.handIdx,
                HandInst = null,                       // **没人用它**：真正落地走 `handIdx`（`NetApply.Apply`）
                Slot = m.slot,
                TargetP = m.targetP,                   // **绝对座位，不翻**（`Seat` 已退出运行路径）
                TargetSlot = m.targetSlot,
                Ranged = m.ranged,
                AltKeyword = m.altKeyword,
            };
        }

        // ---- 状态指纹（正本 §5·4；**内容变了要同步改文档**）----

        /// <summary>一局的可复现状态指纹（**两端直接比，不做任何镜像归一**）。
        ///
        /// 🔴 **为什么可以直接比**（2026-09-26 大改）：联机对局**两端跑的是同一套绝对座位编号**
        ///   （主机 = 0、客机 = 1；客机那边的视图靠 `BattleDriver._me = 1` 翻，**引擎不镜像**）。
        ///   ⇒ 同一个种子 + 同一串动作 ⇒ 两边**逐位相同**，`Turn`/`Active`/`Winner` 这些座位号也直接可比。
        ///
        /// ⚠️ **反面教训（留在这儿，别再走回头路）**：第一版是「两端都把自己当 0 号位」（镜像），
        ///   自检跑到第 40 步就分叉 —— 根因是 `Unstable`（随机自爆）那张卡的候选单位表**是按座位顺序拼的**，
        ///   镜像之后**同一个随机下标在两边选中了不同的单位**。这类「按座位顺序建候选表再抽随机」
        ///   在引擎里到处都是 ⇒ **镜像这条路根本走不通**（不是打个补丁的事）。
        ///
        /// **不含** `ShowRng`/`AiRng`（表现与 AI 那两路两端本来就不一样），也**不含**
        /// `System.Random` 的内部状态（读不出来 —— 已知盲区，见正本 §5·4）。
        /// **不含 `CardInstance.Id`**：实例号按座位顺序发，但既然座位是绝对的那就两端一致……
        /// 仍然不含它，因为**号码本身不承载语义**，进指纹只会让「换一张同名牌」这种真差异被掩盖。</summary>
        public static int Fingerprint(BattleContext ctx)
        {
            if (ctx == null) return 0;
            unchecked
            {
                int h = 17;
                h = h * 31 + ctx.Turn;
                h = h * 31 + ctx.Active;
                h = h * 31 + ctx.Winner;
                h = h * 31 + ctx.Events.Count;
                h = h * 31 + PlayerHash(ctx, 0);
                h = h * 31 + PlayerHash(ctx, 1);
                return h;
            }
        }

        /// <summary>🆕 2026-09-27：**只看局面**的哈希 —— 与 <see cref="Fingerprint"/> 唯一的差别是
        /// **不含 `ctx.Events.Count`**。
        ///
        /// 🔴 为什么要多这一个：`ctx.Events` 是**待播事件队列**，表现层（`PlaySignals` / `DropSignals`）
        /// 随时在抽干它 ⇒ **同一个局面在「刚做完动作」与「表现层跑过一轮」两个时刻，`Fingerprint` 不相等**。
        /// 联机那边没关系（两端在同一约定点比），但**本地录像的逐条对账**会被它骗：
        /// 录制时是「一回合跑完才 drain」（`PlayTurn` 一口气跑好几条），回放时是**每条都 drain**
        /// ⇒ 逐条比指纹会**从第一条起就假红**（2026-09-27 实测：分叉点报到第 6 条，其实局面完全一致）。
        /// ⇒ **录像的轨迹与终局对账一律用这一个**，别用 `Fingerprint`。
        /// </summary>
        public static int StateHash(BattleContext ctx)
        {
            if (ctx == null) return 0;
            unchecked
            {
                int h = 17;
                h = h * 31 + ctx.Turn;
                h = h * 31 + ctx.Active;
                h = h * 31 + ctx.Winner;
                // 🆕 **2026-10-23（引擎线「新账-88」· 深审 B 的 `D2`）**：对局级那几格状态。
                //  · 进攻卡（环境效果）那一次选择 —— 它是**引擎状态**（`ApplyEnvEffect` 按它切环境），
                //    只在「换牌之后、开打之前」发生一次；两端 / 两次跑必然相同。
                h = h * 31 + (ctx.OffensiveChosen ? 1 : 0);
                h = h * 31 + ctx.OffensiveSlotIdx;
                h = h * 31 + Hash(ctx.OffensiveEnvSO);
                //  · 附魔计数（原版 `BattleManager.enchatmentsUsed // +0x4e4`，每挂一件 +1）
                h = h * 31 + ctx.EnchantmentsUsed;
                h = h * 31 + PlayerHash(ctx, 0);      // ⚠️ 与 `Fingerprint` 的差别就在这里：
                h = h * 31 + PlayerHash(ctx, 1);      //    **没有** `ctx.Events.Count` 这一项
                return h;
            }
        }

        /// <summary>某一个玩家的状态子哈希（**内容是双方真该一致的东西**）。
        ///
        /// <para>🔴 **2026-10-23（引擎线「新账-88」· 深审 B 的 `D2`）：维度补全。**
        /// 原来只数「张数 / 能量 / 督军四围 / 手牌 ≤12 张 / 牌库**每 7 张取 1** + 棋盘每格四围」——
        /// 于是**明明演错了也可能判「同一局」**（牌库里第 1 张和第 2 张对调、弃牌堆里换了一批牌、
        /// 信仰/灵魂石/任务点/疲劳/费用修正/抢来的单位/手牌记录/隐秘计数/附魔 全看不见）。
        /// 现在补上的是**引擎状态**，进这一格的唯一判据 = **它只由动作流决定**
        /// （两端 / 两次跑必然相同）⇒ ⛔ 表现层的东西（`ctx.Events` 那一类）一个都不进。</para>
        ///
        /// <para>⚠️ **刻意【不】进的**（逐条给理由，⛔ 别当漏了）：
        /// ① `CardInstance.Id`（实例号）—— 与 <see cref="Fingerprint"/> 的既有口径一致：号码本身不承载语义，
        ///    进指纹只会让「换一张同名牌」这种真差异被号码盖掉（`Fingerprint` 的注）。
        ///    ⚠️ **唯一例外** = `HandEffect.OnlyInstanceId`：它**不是**牌的序号，而是「这条记录只补给哪一份」
        ///    的**语义字段**（`CardInstance.HandEffect` 的注），而且号跟着 `ctx` 走、两端必然相同 ⇒ 进；
        /// ② `ctx.Events` / `ActionLog` —— 表现层随时抽干（`StateHash` 的注）；
        /// ③ `CostMod.Criteria` 的**整对象 / 引用身份** —— 它是卡池里的共享筛选器对象，
        ///    没有规范化序列化 ⇒ 只取它那几个**字符串判据**（`KindWord`/`Keyword`/`Name`）+ `WarlordOnly`；
        /// ④ `TempControls[i].Unit` 的**对象引用** —— 只取它的归属/回合/原格号（引用身份跨进程不可比）
        ///    （⚠️ 这个列表自 2026-10-28 起**既无生产者也无消费者** = 休眠维，处置与理由见 `PlayerHash` 里那一段的注）；
        /// ⑤ `PlayerState.Name` / 玩家名 / `ProfileData` —— 那是**档案**，不是对局状态。</para>
        ///
        /// <para>🧨 **改坏法（灭自证）**：把下面任意一维删掉 ⇒ `Editor/BattleScene.cs` 第 21·2 节
        /// 对应的那条「只改那一格 ⇒ 哈希必须不同」当场红（13 维各一条）；
        /// 其中 `QuestMilestone` / `TurnCount` / `ManaCarry` 三条**没有单独立断言**
        /// （它们是同一族的派生量，由「改一格 ⇒ 哈希变」那一族间接覆盖 —— 如实标注，别当已经逐条验过）。</para></summary>
        static int PlayerHash(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];
            unchecked
            {
                int h = 7;
                if (ps == null) return h;
                h = h * 31 + ps.Deck.Count;
                h = h * 31 + ps.Hand.Count;
                h = h * 31 + ps.Discard.Count;
                h = h * 31 + ps.Energy;
                h = h * 31 + ps.MaxEnergy;
                // —— 🆕 2026-10-23（`D2`）：阵营资源 / 疲劳 / 计数类 ——
                h = h * 31 + ps.Faith;
                h = h * 31 + ps.SpiritStones;
                h = h * 31 + ps.QuestPoints;
                h = h * 31 + ps.QuestMilestone;
                h = h * 31 + ps.Fatigue;
                h = h * 31 + ps.SecretsPlayed;
                h = h * 31 + ps.TurnCount;
                h = h * 31 + ps.ManaCarry;
                if (ps.Warlord != null)
                {
                    h = h * 31 + Hash(ps.Warlord.Card != null ? ps.Warlord.Card.Id : null);
                    h = h * 31 + ps.Warlord.Health;
                    h = h * 31 + ps.Warlord.Attack;
                    h = h * 31 + ps.Warlord.RangedAttack;
                    h = h * 31 + ps.Warlord.Armor;
                }
                // 手牌：**身份 + 这一份自己的降/加费**（`CostDelta`；同名两张可以不一样）
                for (int i = 0; i < ps.Hand.Count; i++)
                {
                    var c = ps.Hand[i];
                    h = h * 31 + Hash(c != null && c.Card != null ? c.Card.Id : null);
                    h = h * 31 + (c != null ? c.CostDelta : 0);
                }
                // 牌库：**全量**滚动（原来是「每 7 张取 1」⇒ 中间那些被换掉了也看不出来）
                for (int i = 0; i < ps.Deck.Count; i++)
                    h = h * 31 + Hash(ps.Deck[i] != null && ps.Deck[i].Card != null ? ps.Deck[i].Card.Id : null);
                // 弃牌堆：原来**一张都没进**（只数了张数）
                for (int i = 0; i < ps.Discard.Count; i++)
                    h = h * 31 + Hash(ps.Discard[i] != null && ps.Discard[i].Card != null ? ps.Discard[i].Card.Id : null);
                // 手牌效果记录表（原版 `PlayerHand.activeEffects // +0x48`）—— 它**不跟着手牌走**
                //（载体离手之后记录还在，后来进手牌的牌照样吃得上）⇒ 必须单独进指纹。
                h = h * 31 + ps.HandEffectRecords.Count;
                for (int i = 0; i < ps.HandEffectRecords.Count; i++)
                {
                    var e = ps.HandEffectRecords[i];
                    if (e == null) { h = h * 31 + 0; continue; }
                    h = h * 31 + e.RecordId;
                    h = h * 31 + Hash(e.Source);
                    h = h * 31 + (int)e.Expire;
                    h = h * 31 + e.ExpireTurn;
                    h = h * 31 + e.OnlyInstanceId;
                    if (e.Uses != null) { h = h * 31 + e.Uses.Left; h = h * 31 + (e.Uses.Limited ? 1 : 0); }
                }
                for (int s = 0; s < ps.Board.Length; s++)
                {
                    var u = ps.Board[s];
                    if (u == null) { h = h * 31 + 0; continue; }
                    h = h * 31 + Hash(u.Card != null ? u.Card.Id : null);
                    h = h * 31 + u.Health;
                    h = h * 31 + u.Attack;
                    h = h * 31 + u.RangedAttack;
                    h = h * 31 + u.Armor;
                    h = h * 31 + (u.Exhausted ? 1 : 0);
                    // 🆕 附魔层（原版 `EntityScript.activeEnchantments // +0x110`）
                    var ench = u.Instance != null ? u.Instance.ActiveEnchantments : null;
                    h = h * 31 + (ench != null ? ench.Count : 0);
                    if (ench != null)
                        for (int k = 0; k < ench.Count; k++)
                        {
                            var a = ench[k];
                            h = h * 31 + (a != null ? a.EnchantmentCardId : 0);
                            h = h * 31 + Hash(a != null && a.Card != null ? a.Card.Id : null);
                        }
                }
                // 🆕 费用修正（`ctx.CostMods` 是**对局级**列表，按 `Player` 归属本玩家）
                int cm = 0;
                for (int i = 0; i < ctx.CostMods.Count; i++)
                {
                    var m = ctx.CostMods[i];
                    if (m == null || m.Player != p) continue;
                    cm++;
                    h = h * 31 + Hash(m.Key);
                    h = h * 31 + m.Delta;
                    h = h * 31 + m.ExpireTurn;
                    h = h * 31 + m.HandOf;
                    h = h * 31 + m.HandInstanceId;
                    h = h * 31 + (m.Once ? 1 : 0);
                    h = h * 31 + Hash(m.Tag);
                    var cr = m.Criteria;
                    h = h * 31 + (cr == null ? 0 : 1);
                    if (cr != null)
                    {
                        h = h * 31 + Hash(cr.KindWord);
                        h = h * 31 + Hash(cr.Keyword);
                        h = h * 31 + Hash(cr.Name);
                        h = h * 31 + (cr.WarlordOnly ? 1 : 0);
                    }
                }
                h = h * 31 + cm;
                // 🔴 2026-10-28（`B91` · A1 + A2）：**这一维今天是【休眠维】** —— `TempControls` 既无生产者
                //   （`EffectResolver.DoTakeControl` 已不再登记），也无消费者（`RuleCore.EndTurn` 的归还段已停用、
                //   不再 `Clear()`）⇒ 列表恒空、本段只贡献一个常量 `tc = 0`。
                //   **处置 = 留着，并且【不对它做任何假设】**（⛔ 不假设它空、也⛔ 不假设它会被清空）：来什么就把
                //   原件（`Turn` / `Slot`）滚进指纹。理由：两端跑同一份代码 ⇒ 去/留都一致；留着可让**将来一旦
                //   恢复生产者**时指纹不丢维度。⚠️ 那天必须同时决定「谁清空它」—— 否则列表只增不清、指纹会带上
                //   过期条目（这就是 A2 停用 `Clear()` 之后留下的隐患，如实记着）。⛔ 别单独删掉本段。
                int tc = 0;
                for (int i = 0; i < ctx.TempControls.Count; i++)
                {
                    var t = ctx.TempControls[i];
                    if (t == null || t.Owner != p) continue;
                    tc++;
                    h = h * 31 + t.Turn;
                    h = h * 31 + t.Slot;
                }
                h = h * 31 + tc;
                // 🆕 阵亡表（「自你上个回合之后死的」那个窗口全靠它）
                int du = 0;
                for (int i = 0; i < ctx.DeadUnits.Count; i++)
                {
                    var d = ctx.DeadUnits[i];
                    if (d == null || d.Owner != p) continue;
                    du++;
                    h = h * 31 + Hash(d.Card != null ? d.Card.Id : null);
                    h = h * 31 + d.DeathTurn;
                }
                h = h * 31 + du;
                return h;
            }
        }

        /// <summary>字符串 → **跨进程稳定**的整数（`string.GetHashCode` **不保证跨进程一致** ⇒ 自己算一个）。
        ///
        /// <para>🔴 **2026-10-23（引擎线「新账-88」· 深审 B 的 `D1`）：这是全工程【唯一】那份稳定哈希，
        /// ⛔ 不许在别处再写第二份**。判据 = 本项目自己的红字（`EffectResolver.cs` 里那句
        /// 「**不能用 `string.GetHashCode`** —— .NET 里按进程随机化，同一局面两次跑会不同」）：
        /// 那份随机化会让**换一个进程放同一份录像**从第 0 条就报分叉。</para>
        /// <para>调用点（现读）：`PlayerHash`（本文件）· `BattleDriver.DeepHash`（录像轨迹）。
        /// 算法 = 31 进制滚动（`h = 23; h = h*31 + c`），**只由字符内容决定** ⇒ 跨运行时/跨进程可复现。
        /// ⚠️ 它**不是** FNV —— 任务口径里那句「`NetProtocol.Hash()` 的 FNV」是笔误（有一条
        /// `资料/普查产出_1023d/引擎_新账88_回放自证_执行报告.md` 的「顺手发现」指出来了）；
        /// 稳定性的判据是「有没有读运行时随机化的东西」，不是具体哪一套折叠。</para>
        /// <para>⛔ **别改成 `string.GetHashCode()` 的转发**：那会把 `D1` 原样放回来，而且
        /// **同进程自检照样全绿**（见 `Editor/BattleScene.cs` 那两条字面量锚点）。
        /// 空串 / `null` 都返回 `0`。</para></summary>
        public static int Hash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            unchecked
            {
                int h = 23;
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h;
            }
        }

        /// <summary>把一串整数折成一个数 —— **录像轨迹的跨进程对账**用
        /// （`ReplayRecord.trace` 逐条哈希 → 一个摘要，两个独立进程必须逐位相同）。
        /// ⚠️ 折叠式与 <see cref="Hash"/> 同一族（`h*31 + v`），⛔ 别换成 `GetHashCode`。</summary>
        public static int HashOfInts(IList<int> values)
        {
            if (values == null) return 0;
            unchecked
            {
                int h = 29;
                for (int i = 0; i < values.Count; i++) h = h * 31 + values[i];
                return h;
            }
        }
    }
}
