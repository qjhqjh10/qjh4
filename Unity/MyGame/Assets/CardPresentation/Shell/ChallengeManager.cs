// ChallengeManager.cs —— 「练习邀约族」的**本地等价层**（原版 `Everguild.UI.ChallengeManager`，2026-10-28 · B93_D3）
//
// ============================ 判据（唯一来源，全部已现读） ============================
// `d:/2/tools/decomp_full/` 里这一族 11 个方法体：
//   · `ChallengeManager__Start.c`（33 行）—— 往一个单例（`ChatGlobalManager`）上挂两条回调：
//       `+0xa0 = <收到练习挑战 / AcceptedFriendDuel>` · `+0xa8 = <对方撤了 / OnRemoveFriendDuel>`。
//   · `ChallengeManager__SendPracticeInvitation.c`（39 行）—— `this+0x28 = 3`（状态=已发出邀约）→
//       `ChatGlobalManager.SendPrivateMessage(对方, new PrivateMessageData(type=1, mode, playFirst))`。
//   · `ChallengeManager__CancelPracticeInvitation.c`（30 行）—— 同一通道发 `PrivateMessageData(type=2)`（撤回）。
//   · `ChallengeManager__AcceptedFriendDuelNotification.c` / `__OnRemoveFriendDuel.c`（各 23 行）——
//       `ChatGlobalManager.RemovePracticeMatchPending(sender)` → `CancelDueling(fromClick=false)`。
//   · `ChallengeManager__CancelDueling.c`（74 行）—— 状态机：`state ∈ {1,2}` ⇒ 一条词条、`state ∈ {3,4}` ⇒
//       先发 `PrivateMessageData(type=2)` 再另一条词条；`fromClick == false` ⇒ `WindowsManager.HidePopUp()`；
//       然后 `CancelSearch()`；最后若词条非空 ⇒ `WindowsManager.ShowPopUp(词条)`。
//   · `ChallengeManager__CancelPracticeMatchAndRefresh.c`（61 行）—— `RemovePracticeMatchPending` + 刷新 UI。
//   · `ChallengeManager__OpenStartChallengeWindow.c`（19 行，**全文只有两句**）——
//       `SingletonBehaviour<WindowsManager>.Instance` → `WindowsManager.OpenWindow(this+0x20, data)`。
//   · `ChallengeManager__AcceptPracticeChallenge.c`（245 行）—— 撤挂起 → 开确认窗 → 建局。
//   · `ChallengeManager__TryChallengeMatchStart.c`（251 行）—— 按模式取 `IPlayEvent` → 建 `RoomOptions`
//       （`CustomRoomProperties` 塞 playFirst 等）→ `MatchMakerManager.FindMatch(...)`。
//   · `ChallengeManager__<StartChallengeWithUser>b__8_0.c`（8 行）—— 一个 lambda：`CancelDueling(fromClick: 1)`。
//   · `ChallengeButton__OnButtonClick.c`（110 行）—— `challenges.Count > 0` ⇒ 用
//       `Demo/FriendsMenu/ReceiveChallenge` 格式化确认框（OK = 接受 / Cancel = 撤回）→ **把自己 `SetActive(false)`**。
//
// ============================ 为什么本文件是「本地等价」而不是逐句复刻 ============================
// 🔴 原版这整条链的**传输层是 PlayFab 私聊**（`ChatGlobalManager.SendPrivateMessage` /
//    `RemovePracticeMatchPending`，消息体是 `PrivateMessageData{type=1/2}`），而**建局**那一跳是
//    `MatchMakerManager.FindMatch`（服务器匹配）。**关服后这两段在本地不存在** ⇒ 按 `CLAUDE.md` 铁律 18·0
//    （**只有与服务器有关的地方才允许自己设计**）落到本地等价：
//      · 传输层 ⇒ 本进程的**状态机 + 事件**（`InvitationSent` / `InvitationReceived` / `DuelCancelled`）；
//      · 挂起队列 ⇒ 既有的 `Shell/ChallengeData.cs`（`PendingPracticeMatches`，唯一正本）；
//      · 建局 ⇒ 既有的联机/练习入口（`NetMatchmaking` / 练习窗），本文件**不新造一条开局链**。
//    ⛔ 每一处「服务器那一跳没做」都**出声**（红线：不许静默失败），⛔ 不编造「对方已在线/已接受」。
//
// ============================ 与既有件的关系（⛔ 别再建第二份） ============================
//   · 待处理队列 / 计数 / 红点 provider = `Shell/ChallengeData.cs`（唯一正本；本文件只调它的 API）。
//   · 挑战钮的显隐模型 = `Shell/ChallengeEngine.cs`（`Initialize` / `OnSignalRaise` / `Toggle`）。
//   · 好友挑战弹窗（= 原版 `MessagePopupWindowDuel`）= `Shell/DuelPopupWindow.cs`。
//   · 两条词条串本地**有**（官方表实测）：`Demo/FriendsMenu/ReceiveChallenge` = `{0} 在 {1} 模式下向你发起了挑战！\n玩友谊赛吗？`、
//     `Demo/MainMenu/OK` = `好的`、`Demo/MainMenu/CancelButton` = `取消`。
//     ⚠️ `CancelDueling` 里那三条**提示词条键**（`DAT_18425d6a8` / `18425d7a8` / `1842b80e0`）**没解出来**
//     （是 Il2Cpp 元数据指针，不是字面量）⇒ 本文件**不编提示文案**，只打日志 + 走调用方给的默认（如实记在报告里）。

using System;
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>练习邀约/友谊对局的本地状态机（原版 `ChallengeManager` 的 `+0x28` 那一格）。
    /// <para>⚠️ 原版那格的**四个具体数值**只解出 3 个（`SendPracticeInvitation` 写 `3`；`CancelDueling` 用
    /// `1/2/3/4` 分两支）—— 语义标注如下，⛔ 别把猜测写死成原版名：`1`/`2` = 收到别人的挑战那两支，
    /// `3` = 我发出邀约（原版明写），`4` = 原版另一支（未解，按「邀约已过期/待回」近似）。</para></summary>
    public enum DuelState
    {
        Idle = 0,
        ReceivedInvite = 1,
        ReceivedInviteAlt = 2,
        InviteSent = 3,          // ★ 原版 `SendPracticeInvitation` 明写 `= 3`
        InviteSentAlt = 4,
    }

    /// <summary>原版 `Everguild.UI.ChallengeManager` 的本地等价层（见文件头）。
    /// <para>⚠️ 全静态：原版是 MonoBehaviour 单例，但本机这条链**没有挂点**（`ChatGlobalManager` 那一侧不存在）
    /// ⇒ 用静态类避免再造一个空壳 MonoBehaviour（同 `ChallengeData` / `ChallengeEngine` 的先例）。</para></summary>
    public static class ChallengeManager
    {
        /// <summary>本机这一侧的对局状态（原版 `this+0x28`）。</summary>
        public static DuelState State { get; private set; }

        /// <summary>当前正在与之交流的对手名（原版 `this+0x30` = `targetPlayerName`）。</summary>
        public static string TargetPlayerName { get; private set; }

        /// <summary>模式词条 / 模式键（原版 `PrivateMessageData` 里带的那一格；本地 = `PracticeChallenge.ModeKey`）。</summary>
        public static string TargetModeKey { get; private set; }

        /// <summary>原版 `PrivateMessageData` 里那个 `playFirst` 位。</summary>
        public static bool TargetPlayerFirst { get; private set; }

        // ============================================================ ① 事件（= 原版挂在那两条回调上的转发）

        /// <summary>我发出一份邀约（原版 = `ChatGlobalManager.SendPrivateMessage` 成功那一拍）。</summary>
        public static event Action<string, string, bool> InvitationSent;
        /// <summary>我撤回一份邀约。</summary>
        public static event Action<string> InvitationCancelled;
        /// <summary>这一局被撤（原版 `CancelDueling` 的对外可观测结果）。参数 = 是不是玩家点的。</summary>
        public static event Action<bool> DuelCancelled;

        static string _netCancelWhy;

        /// <summary>自检/换局复位。</summary>
        public static void Reset()
        {
            State = DuelState.Idle;
            TargetPlayerName = null; TargetModeKey = null; TargetPlayerFirst = false;
        }

        // ============================================================ ② 原版 `Start()`（33 行）

        /// <summary>≡ 原版 `ChallengeManager.Start()`：把两条回调挂到那台单例上。
        /// <para>🔴 原版挂的是 `ChatGlobalManager` 的 `+0xa0`（收到练习挑战）与 `+0xa8`（对方撤了）两格。
        /// 我们这一侧没有那台单例（PlayFab 私聊通道）⇒ 等价物 = <see cref="ChallengeData"/> 的队列变化：
        /// 这里**不做装配**，只把「这一族已经就绪」讲清楚（⛔ 不假装挂了一个不存在的回调）。</para></summary>
        public static void Start()
        {
            ChallengeData.RegisterProviders();     // 红点那条总线（= `ChallengeButton.Awake` 的等价物，幂等）
            ChallengeEngine.Attach();              // 挑战钮的显隐模型（幂等）
            UnityEngine.Debug.Log("[Challenge] `Start()`：本机没有 `ChatGlobalManager`（PlayFab 私聊通道）⇒ 原版那两条回调"
                      + "（`+0xa0` 收到挑战 / `+0xa8` 对方撤回）**挂不上**；等价物 = `ChallengeData.PendingPracticeMatches`"
                      + "（本地队列，生产者为 P2P 邀约 / 测试）。");
        }

        // ============================================================ ③ 发/撤邀约（原版两个 SendPrivateMessage）

        /// <summary>≡ 原版 `ChallengeManager.SendPracticeInvitation()`：
        /// `state = 3` → `ChatGlobalManager.SendPrivateMessage(target, PrivateMessageData(type=1, mode, playFirst))`。
        /// <para>🔴 传输层是 PlayFab 私聊 ⇒ **P3**。本地等价 = 状态置 `inviteSent` + 发本进程事件
        /// <see cref="InvitationSent"/>（联机线将来可把它接到 P2P 通道上），并**出声**说明没走服务器。</para></summary>
        public static void SendPracticeInvitation()
        {
            if (string.IsNullOrEmpty(TargetPlayerName))
            {
                UnityEngine.Debug.LogError("[Challenge] `SendPracticeInvitation`：没有对手名（原版这一格为 null 时 "
                               + "`SendPrivateMessage` 会 NRE）⇒ 不发送");
                return;
            }
            State = DuelState.InviteSent;                 // ★ 原版 `*(this+0x28) = 3`
            var h = InvitationSent;
            if (h != null) h(TargetPlayerName, TargetModeKey, TargetPlayerFirst);
            UnityEngine.Debug.Log("[Challenge] 已发出练习邀约 → " + TargetPlayerName + "（模式 = " + (TargetModeKey ?? "<空>")
                      + " · 先手 = " + TargetPlayerFirst + "）。⚠️ 原版这一跳是 `ChatGlobalManager.SendPrivateMessage("
                      + "PrivateMessageData(type=1))` = **PlayFab 私聊（P3）**，本地没有服务器 ⇒ 只走本进程事件。");
        }

        /// <summary>≡ 原版 `ChallengeManager.CancelPracticeInvitation()`：
        /// `ChatGlobalManager.SendPrivateMessage(target, PrivateMessageData(type=2))`（撤回邀约）。
        /// <para>同样 P3 ⇒ 本地 = 发 <see cref="InvitationCancelled"/> + 状态回 `Idle` + 出声。</para></summary>
        public static void CancelPracticeInvitation()
        {
            string who = TargetPlayerName;
            if (string.IsNullOrEmpty(who))
            {
                UnityEngine.Debug.LogWarning("[Challenge] `CancelPracticeInvitation`：没有对手名（原版这一格为 null 时那台单例是 null ⇒ 走 `FUN_1803f47a0` = 崩溃支）⇒ 只清状态");
                Reset();
                return;
            }
            var h = InvitationCancelled;
            if (h != null) h(who);
            Reset();
            UnityEngine.Debug.Log("[Challenge] 撤回练习邀约 → " + who + "（原版 = `PrivateMessageData(type=2)` 私聊 = P3；本地只走事件）");
        }

        // ============================================================ ④ 收到对方的回应（原版那两条 23 行）

        /// <summary>≡ 原版 `ChallengeManager.AcceptedFriendDuelNotification(senderId)`（23 行）：
        /// `ChatGlobalManager.RemovePracticeMatchPending(senderId)` → `CancelDueling(fromClick: false)`。
        /// <para>⚠️ 语义照抄（**对方接受了 ⇒ 把挂起项撤掉并收掉决斗态**）—— 这条链里「谁撤谁」容易被读反。</para></summary>
        public static void AcceptedFriendDuelNotification(string senderId)
        {
            ChallengeData.RemovePending(senderId);       // = `RemovePracticeMatchPending(sender)`
            CancelDueling(false);                        // ★ 原版第二句（fromClick = 0）
            UnityEngine.Debug.Log("[Challenge] 对方（" + senderId + "）接受了友谊对局 ⇒ 已从挂起队列撤掉并收掉决斗态"
                      + "（判据 = `ChallengeManager__AcceptedFriendDuelNotification.c` 那两句）");
        }

        /// <summary>≡ 原版 `ChallengeManager.OnRemoveFriendDuel(senderId)`（23 行）：与上一支**逐句相同**
        /// （`RemovePracticeMatchPending` + `CancelDueling(0)`）—— 原版就是两份同体（不同消息类型各挂一个回调）。</summary>
        public static void OnRemoveFriendDuel(string senderId)
        {
            ChallengeData.RemovePending(senderId);
            CancelDueling(false);
            UnityEngine.Debug.Log("[Challenge] 对方（" + senderId + "）撤了这一局 ⇒ 从挂起队列撤掉并收掉决斗态（同上一支，原版也是两份同体）");
        }

        // ============================================================ ⑤ 收决斗态（原版 74 行的状态机）

        /// <summary>≡ 原版 `ChallengeManager.CancelDueling(bool fromClick)`（74 行）：
        /// ① `state ∈ {3,4}` ⇒ 先发 `PrivateMessageData(type=2)`（撤回）；② `fromClick == false` ⇒ `HidePopUp()`；
        /// ③ `CancelSearch()`；④ 若那两条词条之一非空 ⇒ `ShowPopUp(词条)`。
        /// <para>本地等价：① 走 <see cref="CancelPracticeInvitation"/> 那套事件（P3 那一跳出声）；
        /// ② 关掉 `DuelPopupWindow`（我们这条链的弹窗宿主）；③ 取消搜索 = `NetMatchmaking.Cancel`（联机线既有口，
        /// ⛔ 本文件不新造）；④ **提示词条键没解出来** ⇒ 不编文案、只打日志（报告已记）。</para></summary>
        public static void CancelDueling(bool fromClick)
        {
            // 🔴 **原版那道总闸**（`decomp_full/ChallengeManager__CancelDueling.c:21`）：`if (*(int*)(this+0x28) != 0) { … }`
            //   —— `state == 0` 时**整条 no-op**（连 `HidePopUp` / `CancelSearch` 都不跑）。改前我们无条件跑完全程。
            if (State == DuelState.Idle) return;

            // 原版 `.c:30-33` 那句三元：`state == 1` ⇒ `CustomErrors/PracticeUserNotFound` ·
            //   `state == 2` ⇒ `CustomErrors/PracticeUserNotOnline` · 其余（3/4/5…）⇒ 空串。
            //   三条键**已经解出**（`il2cpp_out/stringliteral.json`：RVA `0x425d6a8` / `0x425d7a8` / `0x42b80e0`=空串），
            //   官方 13 语言表两键都有正文（en `User not found` / zh `找不到用户`；en `User is not online` / zh `用户不在线`）。
            // ⚠️ 必须**在本方法清状态之前**采：`CancelPracticeInvitation` / `Reset` 都会把 `State` 打回 `Idle`。
            string key = State == DuelState.ReceivedInvite ? "CustomErrors/PracticeUserNotFound"
                       : State == DuelState.ReceivedInviteAlt ? "CustomErrors/PracticeUserNotOnline"
                       : "";

            bool wasInviting = State == DuelState.InviteSent || State == DuelState.InviteSentAlt;   // 原版 `state == 3 || 4`
            if (wasInviting) CancelPracticeInvitation();     // 原版这一支先发 type=2 的私聊
            var wm = WindowsManager.Get();                   // 原版末尾 `ShowPopUp` 用的是同一个单例（`.c:60`）
            if (!fromClick)                                  // 原版 `param_2 == 0` ⇒ `WindowsManager.HidePopUp(0)`
            {
                // 原版那句是 **关弹窗**（`WindowsManager__HidePopUp.c`）而不是「关某一扇具体窗」。
                if (wm != null) wm.HidePopUp();              // = 原版 `WindowsManager.HidePopUp(manager, 0)`（没有弹窗时它是空动作）
                else UnityEngine.Debug.LogWarning("[Challenge] `CancelDueling`：取不到 `WindowsManager`（原版这一跳 NRE）⇒ 弹窗那一步跳过（出声）");
            }
            else key = "";                                   // 原版 `.c:50-56`：只有 `fromClick == false` 才把 state 推出的 key 交给末尾
            CancelSearchStep();                              // 原版 `.c:57` 那句 `ChallengeManager.CancelSearch()`
            // 原版 `.c:58-70`：`if (!string.IsNullOrEmpty(key)) WindowsManager.ShowPopUp(key, localize:1, …)`
            if (!string.IsNullOrEmpty(key))
            {
                if (wm != null) wm.ShowPopUp(Loc.T(key), Loc.T("MainMenu/General/OK"), null);   // 原版 `localize:1`
                else UnityEngine.Debug.LogWarning("[Challenge] `CancelDueling`：取不到 `WindowsManager`（原版这一跳 NRE）⇒ 提示词条 '"
                                                + key + "' 没弹出来（出声）");
            }
            Reset();
            var h = DuelCancelled;
            if (h != null) h(fromClick);
            UnityEngine.Debug.Log("[Challenge] `CancelDueling(fromClick: " + fromClick + ")`：状态已清、弹窗已收、搜索已取消"
                      + (string.IsNullOrEmpty(key) ? "（原版末尾没有提示要弹）" : "（已弹出提示词条 `" + key + "`）"));
        }

        /// <summary>≡ 原版 `ChallengeManager.CancelSearch()`（四句：`CustomDebug.Log("Cancel Search")` →
        /// `FindObjectOfType&lt;SearchOpponentManager&gt;()` → `m.CancelBattleSearch()` → `duelState = 0`；管理器不在时 NRE）。
        /// <para>🔴 **本仓那四句已经有一份**：`B93_N5` 落在 `Shell/SearchingMatchPopup.cs:715`
        /// （`CancelSearch()`：Log → 管理器判空 → `AbortSearch()`）⇒ 按铁律 6 **不写第二份**，
        /// 这里只做**转调**（形状也忠于原版的 `FindObjectOfType` 那一跳）；
        /// 窗不在（没有活着的搜索界面）时退到**传输层**取消（`NetMatchmaking.Cancel`，原版那一格是 NRE，我们出声）。</para></summary>
        static void CancelSearchStep()
        {
            var ui = UnityEngine.Object.FindObjectOfType<SearchingMatchPopup>();   // = 原版 `FindObjectOfType<SearchOpponentManager>()` 那一跳
            if (ui != null) { ui.CancelSearch(); return; }                          // 转调既有实现（不复制逻辑）
            UnityEngine.Debug.Log("[Challenge] 取消搜索：没有活着的 `SearchingMatchPopup`（原版这一格是 NRE）"
                              + "⇒ 退到传输层取消（`NetMatchmaking.Cancel`）");
            Net.NetMatchmaking.Cancel("挑战取消", out _netCancelWhy);
        }

        /// <summary>≡ 原版 `<StartChallengeWithUser>b__8_0`（8 行）：一个 lambda，体就是
        /// `CancelDueling(fromClick: true)`。原版用它当「建局超时/失败」的兜底回调。</summary>
        public static void StartChallengeWithUserFailed() { CancelDueling(true); }

        // ============================================================ ⑥ 撤一场挂起的练习挑战（原版 61 行）

        /// <summary>≡ 原版 `ChallengeManager.CancelPracticeMatchAndRefresh(GameObject source)`：
        /// `ChatGlobalManager.RemovePracticeMatchPending(对方)` → 刷 UI；`source` 是**点的那颗钮**
        /// （原版用它找窗口去关）。本地等价 = `ChallengeData.RemovePending`（已有）+ 关掉邀请弹窗。
        /// <paramref name="who"/> = 要撤的那一条的玩家名（原版从挂起项里取）。</summary>
        public static bool CancelPracticeMatchAndRefresh(string who)
        {
            bool removed = ChallengeData.RemovePending(who);
            // 🔴 **原版尾部那一句**（`ChallengeManager__CancelPracticeMatchAndRefresh.c:44-46`）：
            //   `ChatGlobalManager.RemovePracticeMatchPending(sender)` 之后再 `ChallengeManager.CancelDueling(this, 0)`。
            //   ⛔ 本代理改前这里**自己关了一次 `DuelPopupWindow`** —— 那是 `CancelDueling` 里已有的同一支（同一条规则写两处），已删；
            //   关窗那一下改由 `CancelDueling(false)` 做（它的 `fromClick == false` 支）。
            if (!removed)
                UnityEngine.Debug.LogWarning("[Challenge] `CancelPracticeMatchAndRefresh(" + (who ?? "<null>") + ")`：挂起队列里没有这一条"
                                 + "（原版 `RemovePending` 那一跳同样不会命中）");
            ChallengeEngine.Initialize();          // 刷新挑战钮显隐（原版 `Refresh` 那一半）
            CancelDueling(false);                  // ★ 原版尾部那一句（上面那段注释）
            UnityEngine.Debug.Log("[Challenge] 撤掉挂起挑战 `" + (who ?? "<null>") + "` ⇒ " + (removed ? "命中并撤掉" : "未命中")
                      + "（本地队列 = `ChallengeData.PendingPracticeMatches`；原版走 `ChatGlobalManager.RemovePracticeMatchPending`）");
            return removed;
        }

        // ============================================================ ⑦ 开「发起挑战」窗（原版 19 行，全文两句）

        /// <summary>≡ 原版 `ChallengeManager.OpenStartChallengeWindow(PlayerDuelPopUpData data)`：
        /// **全文只有两句** —— 取 `WindowsManager` 单例 → `WindowsManager.OpenWindow(this+0x20, data)`。
        /// <para>本地等价 = `DuelPopupWindow.Create(mgr, 对手名)` + `Open()`（那扇窗 = 原版 `MessagePopupWindowDuel`）。
        /// ⚠️ 原版 `data` 里还带「模式 / 是否先手」（`PlayerDuelPopUpData`），我们这扇窗不吃这些格 ⇒ 只传对手名
        /// （那扇窗自己的 doc 已记「Skirmish/Classic 两颗钮要服务器」）。</para></summary>
        public static DuelPopupWindow OpenStartChallengeWindow(string opponentName, WindowsManager mgr = null)
        {
            // 🔴 **2026-10-28 转调（铁律 5）**：真身已由 `B93_N6` 落在
            //   `Shell/DuelPopupWindow.cs:391`（同名方法）—— 它走 的是 `WindowsManager.OpenWindow(win, data, false)`，
            //   才会登记 `openWindows` / `currentWindow` 并调 `TryOpen(data)`；而本文件改前那一版走
            //   `Create + win.Open()` **绕过了管理器** ⇒ `GetOpenWindow<DuelPopupWindow>()` 取不到它、`closeAll` 也关不掉它。
            //   ⛔ 两条路同时活着 = 同一条规则写两处（负责人不同、迟早不一致）。
            var wm = mgr != null ? mgr : WindowsManager.Get();
            if (wm == null)
            {
                UnityEngine.Debug.LogError("[Challenge] `OpenStartChallengeWindow`：取不到 `WindowsManager`（原版这一跳 NRE）⇒ 不开窗");
                return null;
            }
            return DuelPopupWindow.OpenStartChallengeWindow(wm, new DuelPopupWindow.DuelPopUpData(opponentName, null));
        }

        // ============================================================ ⑧ 接受挑战 → 建局（原版 245 + 251 行）

        /// <summary>≡ 原版 `ChallengeManager.AcceptPracticeChallenge(GameObject source)` 的**本地那一半**：
        /// 撤挂起 → 建局。原版 245 行里的大头是「确认窗 + 组 `PlayerDuelPopUpData` + 等对方在线」，
        /// 而那些数据（对方在线态 / 模式 / 先手）来自 PlayFab ⇒ **P3**。
        /// <para>本地等价：`ChallengeData.RemovePending` + <see cref="TryChallengeMatchStart"/>。</para></summary>
        public static bool AcceptPracticeChallenge(PracticeChallenge incoming)
        {
            if (incoming == null) { UnityEngine.Debug.LogWarning("[Challenge] `AcceptPracticeChallenge(null)`（原版这一格 NRE）⇒ 不做"); return false; }
            ChallengeData.RemovePending(incoming.PlayerName);
            TargetPlayerName = incoming.PlayerName;
            TargetModeKey = incoming.ModeKey;
            TargetPlayerFirst = incoming.PlayerFirst;
            bool ok = TryChallengeMatchStart(incoming.ModeKey, incoming.PlayerFirst);
            UnityEngine.Debug.Log("[Challenge] 接受练习挑战（" + incoming.PlayerName + " / 模式 " + (incoming.ModeKey ?? "<空>")
                      + "）⇒ 建局" + (ok ? "已交给开局面" : "**没开成**（见上一行出声）"));
            return ok;
        }

        /// <summary>≡ 原版 `ChallengeManager.TryChallengeMatchStart(IPlayEvent playEvent, bool playerFirst)` 的本地那一半。
        /// <para>🔴 原版 251 行 = 按模式取 `IPlayEvent` → 组 `RoomOptions{ playFirst, 双方存档 }` →
        /// `MatchMakerManager.FindMatch(...)`（**服务器匹配 = P3**）。本地唯一等价的「开一局」入口是
        /// `NetMatchmaking.TryStart(...)`（P2P；对面必须已连上）⇒ 它在没有活着的联机会话时返回 false 并给出原因，
        /// 我们**如实转述**（⛔ 不假装开了一局、也不静默）。</para></summary>
        public static bool TryChallengeMatchStart(string modeKey, bool playerFirst)
        {
            string mode = string.IsNullOrEmpty(modeKey) ? RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OfflinePractice) : modeKey;
            string why;
            if (Net.NetMatchmaking.TryStart(null, mode, null, out why))
            {
                UnityEngine.Debug.Log("[Challenge] `TryChallengeMatchStart`：已交给 P2P 开局（模式 = " + mode + " · 先手 = " + playerFirst
                          + "）。原版走 `MatchMakerManager.FindMatch`（服务器匹配 = P3），我们走既有的联机入口。");
                return true;
            }
            UnityEngine.Debug.LogWarning("[Challenge] `TryChallengeMatchStart`：**没有开成局** —— " + why
                             + "（原版这一跳是 `MatchMakerManager.FindMatch` = 服务器匹配 = P3；本地只有 P2P，"
                             + "对面没连上就开不了。⛔ 不假装开了一局）");
            return false;
        }

        // ============================================================ ⑨ 挑战钮（原版 `ChallengeButton.OnButtonClick`，110 行）

        /// <summary>≡ 原版 `ChallengeButton.OnButtonClick()`（110 行）的**本地那一半**：
        /// ① `challenges.Count > 0` ⇒ 用 `Demo/FriendsMenu/ReceiveChallenge` 格式化一句
        ///   （`{0}` = 对方名、`{1}` = 模式词条），弹**两颗钮**的确认框（OK = 接受 / Cancel = 撤回）；
        /// ② 不管弹没弹，最后都 `button.SetActive(false)`（原版最后那两句）。
        /// <para>⚠️ 三串词条**本地都有**（官方表实测）：`Demo/FriendsMenu/ReceiveChallenge` / `Demo/MainMenu/OK` /
        /// `Demo/MainMenu/CancelButton`；模式那一格 `{1}` 原版是 `#bde20 / #bdf20` 两条词条之一（按 `+0x21` 位选）——
        /// 我们这条链的模式词条来自 `PracticeChallenge.ModeKey`，取不到就用 `Loc.T` 的原键兜底并出声。</para>
        /// <param name="buttonRoot">挑战钮的根节点（原版最后那句 `SetActive(false)` 就作用在它身上）。</param></summary>
        public static void OnChallengeButtonClick(UnityEngine.GameObject buttonRoot)
        {
            var pending = ChallengeData.PendingPracticeMatches;
            if (pending.Count > 0)
            {
                var c = pending[0];
                string modeText = string.IsNullOrEmpty(c.ModeKey) ? null : Loc.T(c.ModeKey);
                string msg = string.Format(Loc.T("Demo/FriendsMenu/ReceiveChallenge"),
                                           c.PlayerName ?? string.Empty, modeText ?? string.Empty);
                var wm = WindowsManager.Get();
                if (wm == null)
                    UnityEngine.Debug.LogError("[Challenge] 挑战钮：取不到 `WindowsManager`（原版这一跳 NRE）⇒ 确认框弹不出来");
                else
                {
                    // 🔴 **左 = Cancel / 右 = OK**（原版实参序）：`ChallengeButton__OnButtonClick.c` 那句
                    //   `ShowPopUp(text, 0, 1, lVar2/*先构造的 Cancel*/, lVar5/*OK*/, 0)`；签名桩
                    //   `WindowsManager.cs:335` 第 4 参名 = `primaryButton`；`PopUpGameWindow.cs:228/500`：
                    //   `primary = LiveButtons[0] = ButtonLeft`。⇒ 本仓 `ShowPopUp(text, okText, onOk, cancelText, onCancel)`
                    //   的 `okText/onOk` 就是**左**那颗 ⇒ 把（文案, 动作）整组互换（本代理 2026-10-28 就地订正）。
                    wm.ShowPopUp(msg,
                                 Loc.T("Demo/MainMenu/CancelButton"),
                                 () => CancelPracticeMatchAndRefresh(c.PlayerName),   // 左 = Cancel = 撤回那一条
                                 Loc.T("Demo/MainMenu/OK"),
                                 () => AcceptPracticeChallenge(c));                  // 右 = OK = `AcceptPracticeChallenge`
                    UnityEngine.Debug.Log("[Challenge] 挑战钮：弹出「" + msg.Replace("\n", " / ") + "」的确认框（OK=接受 / Cancel=撤回）");
                }
            }
            else
            {
                UnityEngine.Debug.Log("[Challenge] 挑战钮被点了，但挂起队列是空的（原版这一支：`challenges.Count == 0` ⇒ "
                          + "不弹确认框，只把自己隐藏）");
            }
            if (buttonRoot != null) buttonRoot.SetActive(false);   // ★ 原版最后那句 `set_gameObject().SetActive(false)`
        }
    }
}
