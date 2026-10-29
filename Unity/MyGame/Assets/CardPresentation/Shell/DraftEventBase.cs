// DraftEventBase.cs —— **轮抽活动的基类**（原版 `DraftEventBase<TS,TD> : PlayEvent<TS,TD>`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `DraftEventBase<TS,`（C 5 条：P1 2 / P2 3）
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv` 那 5 行 **＋** `账/轮抽.tsv` 的 2 行
//     （`Expire` / `RefundRun` —— 同一批 B10 的 58 行里有它们）
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/DraftEventBase.cs`（5186 行，**有真方法体**）：
//   `GetMatchmakingFilter:4403` · `FreeEventEntrance:4700` · `RefundRun:4955` · `Expire:5030` ·
//   `OnMatchFound:2020` · `OnMatchProcessSuccess:2181`（**基类空方法体**）
//   字段表 = `dump.cs:127547`（类声明）/ `DraftEventSaveGame < PlayEventSaveGame`。
//
// 🔴 **`OnMatchProcessSuccess` 在基类就是空的**（`DraftEventBase.cs:2181` → `protected virtual void …{ }`）
//   ⇒ 本类也留空，⛔ 别自己发明一个「服务器确认后的落地」。
//
// 🔴 **用户口径（2026-10-24 拍板）：轮抽「不做奖励」** —— `RefundRun` 里**非付费档就直接回空表**
//   （判据见下：原版非 premium 那一支就是 `onComplete(Array.Empty<RewardInfo>())`）；
//   「模式本身要做」（同一次拍板）⇒ 本类**不是**「不做轮抽」。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版这 6 个方法里 **4 个打云脚本**（`DraftMatchFound` / `DraftMatchFinished` / `ExpireDraftEvent` /
//      退赛那条）。本地没有服务器 ⇒ 云脚本那一跳出声跳过，**本地落地那几跳照抄**（写存档 / 关窗 / 回调）。
//   ② `AlliancesManager.HasJoinedEvent(...)` / `PlayerDataManager.demoData.canEnterDraftMode` /
//      `GameAnalytics.SendDraftStart` / `MenuActions` 都没有等价物 ⇒ 各走一个可注入口或出声（见各处）。
//   ③ `CardDeck` / `eventDeck` 我们用 `DraftModeWindow` 那一份静态 run（`DraftRunSave`）当本局正本
//      —— 判据见 `Shell/DraftModeWindow.cs` 文件头「为什么用静态对象顶这一格」。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 数据 / 存档
    /// <summary>原版 `DraftEventData : PlayEventData` 的**本批用得到的那些格**。
    /// <para>⚠️ 原版还有 `matches`（胜/负场次表）、`maxTries`、`packCount`、`admisionPrices` 等；
    /// 本批 7 条账只读「有没有付费档 / 入场」这几格 ⇒ 只搬这几格，**没搬的不假装有**。</para></summary>
    [Serializable]
    public class DraftEventData : PlayEventData
    {
        /// <summary>原版 `maxTries`（这局最多打几场）。⚠️ 本地判据：`DraftModeWindow` 用它判收尾。</summary>
        public int maxTries;
        /// <summary>原版 `packCount`（一局选几个包）。</summary>
        public int packCount;
        /// <summary>入场价（原版 `PlayEventData.admisionPrices` 里那一档）。
        /// ⚠️ `admisionPrices` 在 `Shell/PlayEvent.cs` 里**没搬**（那属别的账），这里放一个裸值占位。</summary>
        public int entrancePrice;
    }

    /// <summary>原版 `DraftEventSaveGame : PlayEventSaveGame` 的**本批用得到的那些格**。
    /// <para>⚠️ 我们真正在用的那一份是 `DraftModeWindow.DraftRunSave`（静态 + 落盘）；
    /// 本类只是给泛型一个**类型位**，`IsPremium` 那一格是 `RefundRun` 唯一读它的地方。</para></summary>
    [Serializable]
    public class DraftEventSave : PlayEventSaveGame
    {
        /// <summary>原版 `DraftEventSaveGame.IsPremium`（`RefundRun` 用它决定「要不要问服务器要奖励」）。</summary>
        public bool isPremium;
        /// <summary>原版 `DraftModeState`（`dump.cs:126782`，值见 `DraftModeWindow.DraftModeState`）：
        /// `Intro=0` / `PayScreen=2` / `SelectWarlord=5` / `SelectPack=10` / `OngoingRun=15` / `Finished=20` /
        /// `Collected=22`。`Expire` 用它分两支（见那条注释）。</summary>
        public int draftState;
    }

    // ============================================================ 轮抽活动基类
    /// <summary>原版 `DraftEventBase&lt;TS,TD&gt; : PlayEvent&lt;TS,TD&gt;`
    /// （约束 `TS : PlayEventSaveGame` / `TD : DraftEventData`）。
    /// <para>本类只做账里那 7 件事：免费入场、匹配过滤串、匹配成功记对手、服务器确认后的落地（空）、
    /// 退赛退款、过期。</para></summary>
    public abstract class DraftEventBase<TS, TD> : PlayEvent<TS, TD>
        where TS : PlayEventSaveGame
        where TD : DraftEventData
    {
        // ============================================================ 可注入口（差异 ②）
        /// <summary>`(eventId) =&gt; 玩家是否已加入该活动`（原版 `AlliancesManager.Instance.HasJoinedEvent(id)`）。
        /// null ⇒ 视为**已加入**（让过期走得通，不把玩家卡在「什么都没发生」里）+ 出声一次。</summary>
        public static Func<string, bool> HasJoinedEvent;

        /// <summary>`(eventId) =&gt; 建好的一局新存档`（原版 `FreeDraftResult.Save` 那一格）。
        /// null ⇒ 用 <see cref="DraftModeWindow"/> 现成的 `BeginRun` 建一局 + 出声一次。</summary>
        public static Func<string, TS> CreateFreeRun;

        /// <summary>`(location, kind, id)` =&gt; 遥测（原版 `GameAnalytics.SendDraftStart` / `RecordTransaction`）。
        /// null ⇒ 只记日志（不静默：日志里写清原版打的是哪个事件）。</summary>
        public static Action<int, string, string> Telemetry;

        static readonly HashSet<string> warnedOnce = new HashSet<string>();
        static void WarnOnce(string key, string msg) { if (warnedOnce.Add(key)) Debug.LogWarning(msg); }

        /// <summary>自检用：清注入 + 告警去重。</summary>
        public static void ResetInjectionForTest()
        {
            HasJoinedEvent = null; CreateFreeRun = null; Telemetry = null; CanEnterDraftMode = null;
            warnedOnce.Clear();
        }

        // ============================================================ 免费入场（账 P1）
        /// <summary>原版 `public void FreeEventEntrance(Action onSuccess, Action onError)`
        /// （`DraftEventBase.cs:4700`；两个 lambda 的真身：`…_FreeEventEntrance_g__OnSuccess_007C1` /
        /// `…_g__OnFail_007C2` / `…_b__32_0`，**都可读**）。
        /// <para>可读到的骨架：
        /// <code>
        /// payload = { eventId = GetID(), deck = &lt;eventDeck 的卡表&gt; };      // b__32_0 = `new LoadableReference&lt;RawCardScript&gt;(x)`
        /// PlayfabWrapper.GenericCloudScriptHandler(&lt;免费入场 handle&gt;, payload, r =&gt; {
        ///     UpdateSaveGame(r.Save);                    // ★ 本地落地这一跳照抄
        ///     GameAnalytics.SendDraftStart(0, GetID());   // 遥测
        ///     onSuccess?.Invoke();
        /// }, e =&gt; {
        ///     WindowsManager.ShowPopUp("[TEMP] FreeEventEntrance Error msg", …主钮 =&gt; onError?.Invoke());
        /// });
        /// </code>
        /// 🔴 **成功支只做三件事：写存档 → 发遥测 → 回调**（没有别的本地动作）。
        /// ⚠️ 失败支原版**弹一个占位文案弹窗**（`[TEMP] FreeEventEntrance Error msg`）—— 那是开发期占位串，
        /// ⛔ 我们**不复刻这串占位文案**，改成「记日志 + 直接调 onError」（不假装弹窗、也不静默）。</para>
        /// <para>⚠️ 云脚本 handle 的名字本批**没解出**（`.c` 里是元数据指针）⇒ 出声说明「原版这一跳打云脚本」。</para></summary>
        public void FreeEventEntrance(Action onSuccess, Action onError)
        {
            if (!CanEnterDraft)
            {
                Debug.Log("[DraftEventBase] FreeEventEntrance：`CanEnterDraft=false`（原版 `canEnterDraftMode` 门）⇒ 不进轮抽；"
                        + "解锁条件 = 练习首胜（`PracticeEvent.OnMatchWon`，生产者见 `报告/B89_R2d_日常G6落点补齐.md` §L585）");
                if (onError != null) onError();
                return;
            }
            string id = GetID();
            Debug.Log("[DraftEventBase] FreeEventEntrance('" + (id ?? "<null>") + "')：原版这一跳打云脚本（handle 名本批未解出）"
                    + "，本地没有服务器 ⇒ 直接走本地落地（写存档 → 遥测 → onSuccess）");

            TS fresh = null;
            if (CreateFreeRun != null) fresh = CreateFreeRun(id);
            else
            {
                WarnOnce("createrun", "[DraftEventBase] `CreateFreeRun` 没接 ⇒ 免费入场不会建局"
                                    + "（原版这一跳用服务器回的 `FreeDraftResult.Save`）");
            }

            if (fresh == null)
            {
                if (onError != null) onError();
                else Debug.LogWarning("[DraftEventBase] FreeEventEntrance：没有建出存档，且没给 onError");
                return;
            }

            UpdateSaveGame(fresh);                                  // ★ 原版成功支第一句
            if (Telemetry != null) Telemetry(0, "SendDraftStart", id); // GameAnalytics.SendDraftStart(0, id)
            else Debug.Log("[DraftEventBase] 遥测：SendDraftStart(0, " + (id ?? "<null>") + ")（原版这一跳上报）");
            if (onSuccess != null) onSuccess();
        }

        // ============================================================ 匹配过滤串（账 P2）
        /// <summary>原版 `public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)`
        /// （`DraftEventBase.cs:4403`；**2026-10-28 真体已补导** → `_tmp_view/decomp_b91/DraftEventBase_object__object_.GetMatchmakingFilter__1810840.c`，ILSpy 同址**格式串逐字可读**）。逐句：
        /// <code>
        /// if (save == null) NRE;
        /// n   = tryCount % 3;
        /// max = save.numberOfMatchesWon(+0x48) + 2n;
        /// min = save.numberOfMatchesWon(+0x48) - 2n;
        /// s   = $"C1 &lt; {max} AND C1 &gt; {min}";
        /// s  += " AND C2 != \"" + PlayerDataManager.singletonManager.notMatchWithPlayer + "\" ";   // ★ 引号 + 尾空格都要留
        /// if (!FeatureConfig.ShouldDisableNewMatchMaking()) s += $" AND C5 == {gamesLost}";
        /// return s;
        /// </code>
        /// 🔴 **形参 `currentElo` 全程未使用**（.c 里它一个读点都没有）。
        /// 🔴 **与 2026-10-27 那次读数相反**：`tryCount` **有**使用点（取模 3 当半径），`gamesLost` **有**使用点（`C5` 的值），只有 `currentElo` 没有。
        /// 🔴 **2026-10-27 那份 🔶 自编整体退役**（铁律 5 就地订正）：原写「半径常量 `EloWindowRadius = 1000` + 中心用 `currentElo` +
        /// `C5FilterValue`（null 就不加）」—— 真体证明**三个参数全错**：中心是 `numberOfMatchesWon`、半径是 `2*(tryCount%3)`、`C5` 是 `gamesLost`。
        /// 那三个常量（`EloWindowRadius` / `EloWindowRadiusOverride` / `C5FilterValue`）**已删除**（退役前 `grep` 核过全仓无引用，⛔ 别加回来）。
        /// ⚠️ 两处本地等价（都不静默）：① `notMatchWithPlayer` → `RankedData.LastOpponentName`（原版是 `PlayerDataManager` 那格字符串）；
        /// ② `FeatureConfig.ShouldDisableNewMatchMaking()` 读**远端** FeatureConfig 的 bool（`+0x70`），服务器已关服 ⇒ 按「拿不到 config ⇒ false」那一支取常量，见 <see cref="DisableNewMatchMaking"/>。</summary>
        public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)
        {
            if (SaveGame == null)
            {
                Debug.LogError("[DraftEventBase] GetMatchmakingFilter：没有存档（原版这一跳 NRE）⇒ 返回空串");
                return string.Empty;
            }
            int n = tryCount % 3;                                   // ★ 原版：取模 3（不是 ELO 窗口半径，也不是 currentElo）
            int won = SaveGame.numberOfMatchesWon;                   // ★ 原版 `save+0x48`
            int max = won + 2 * n;                                   // ★ 原版 `iVar1 + n*2`
            int min = won - 2 * n;                                   // ★ 原版 `iVar1 + n*-2`
            string s = "C1 < " + max + " AND C1 > " + min;            // ★ 原版 `$"C1 < {max} AND C1 > {min}"`

            string avoid = RankedData.LastOpponentName;              // 原版 `PlayerDataManager.singletonManager.notMatchWithPlayer`
            s += " AND C2 != \"" + (avoid ?? string.Empty) + "\" ";  // ★ 引号 + **尾空格**都要留

            if (!DisableNewMatchMaking) s += " AND C5 == " + gamesLost;   // ★ 原版 `$"{text2} AND C5 == {gamesLost}"`（只在「没禁用新匹配」时加）
            return s;
        }

        /// <summary>原版 `FeatureConfig.ShouldDisableNewMatchMaking()`（`FeatureConfig.cs:851`）：
        /// `if (ConfigManager.GetConfig&lt;FeatureConfig&gt;() == null) return false;` 否则返回远端那个 bool（`+0x70`）。
        /// 🔶 自己设计（非原版）：那一格来自**远端配置**（服务器已关服 ⇒ 判据在远端）⇒ 按「拿不到 config」那一支恒取 `false`
        /// （= 新匹配启用 ⇒ **加** C5 段）；留一个静态可写位给「将来接上配置」用（⛔ 别在没有判据时改成 true）。A3 2026-10-28 裁定。</summary>
        public static bool DisableNewMatchMaking = false;

        // ⚠️ 本节原来还有三个 🔶 自编常量（`EloWindowRadius` / `EloWindowRadiusOverride` / `C5FilterValue`），2026-10-28 已随真体退役。
        // 🔴 别看漏：真体是**纯函数**（`numberOfMatchesWon` + `tryCount % 3` + `gamesLost`），没有任何「需要我们自己挑」的参数；将来若接远端 config，只改上面那格默认值，⛔ 别改方法体。

        // ============================================================ 匹配成功（账 P2）
        /// <summary>原版 `protected override void OnMatchFound(string otherPlayerName)`
        /// （`DraftEventBase.cs:2020`）：
        /// <code>
        /// payload = { eventId = GetID(), deck = eventDeck.cardLibrary };
        /// PlayfabWrapper.GenericCloudScriptHandler(CloudHandle.DraftMatchFound, payload);
        /// </code>
        /// ⚠️ 本地等价：**记下对手名到存档的 `lastOpponentInfo`**（原版由服务器回包写）+ 出声；
        /// 云脚本那一跳跳过。基类 `PlayEvent.OnMatchFound` 只打日志，这里补上「记进存档」。
        /// ⚠️ 父类同名方法是 `protected virtual`，所以这里必须 `protected override`。</summary>
        protected override void OnMatchFound(string otherPlayerName)
        {
            base.OnMatchFound(otherPlayerName);       // 父类的日志那一句（不重复写第二份）
            if (SaveGame == null)
            {
                WarnOnce("nomatchsave", "[DraftEventBase] OnMatchFound：没有存档（原版这一跳 NRE）⇒ 对手名没记");
                return;
            }
            if (SaveGame.lastOpponentInfo == null) SaveGame.lastOpponentInfo = new LastOpponentInfo();
            SaveGame.lastOpponentInfo.OpponentName = otherPlayerName;
            Debug.Log("[DraftEventBase] OnMatchFound：'" + (GetID() ?? "<null>") + "' 对手 = "
                    + (otherPlayerName ?? "<null>") + "（原版还会打云脚本 CloudHandle.DraftMatchFound）");
        }

        /// <summary>原版 `protected virtual void OnMatchProcessSuccess(OnlineMatchResult result)`
        /// （`DraftEventBase.cs:2181`）—— **基类方法体是空的**（判据：ILSpy 里就是 `{ }`）。
        /// ⇒ 照抄：留空。子类要落地就覆盖。</summary>
        protected virtual void OnMatchProcessSuccess(OnlineMatchResult result) { }

        // ============================================================ 退赛退款（账 P2 · 轮抽.tsv）
        /// <summary>原版 `public void RefundRun(Action&lt;RewardInfo[]&gt; onComplete = null, Action onError = null)`
        /// （`DraftEventBase.cs:4955`，**骨架可读**）：
        /// <code>
        /// if (SaveGame.IsPremium) {                                    // ★ 只有付费档才「要奖励回来」
        ///     PlayfabWrapper.GenericCloudScriptHandler(&lt;收轮抽奖励 handle&gt;, new { eventId },
        ///             r =&gt; …, e =&gt; …);                                // 两个 lambda：…_RefundRun_g__OnSuccess_0 / OnFail_1
        ///     return;
        /// }
        /// onComplete?.Invoke(Array.Empty&lt;RewardInfo&gt;());              // ★ 非付费档：**空表**
        /// </code>
        /// 🔴 **非付费档就是空表**（这正是用户「轮抽不做奖励」口径在原版里的落点 —— 免费档退赛没有奖励可退）。
        /// ⚠️ 付费档那一支要服务器 ⇒ 我们出声 + 走 `onError`（⛔ 不假装退了奖励，也不假装成功）。
        /// </summary>
        /// <param name="onComplete">退赛完成（拿到退回的奖励表；免费档 = 空表）。</param>
        /// <param name="onError">失败（付费档无服务器时走这里）。</param>
        public void RefundRun(Action<CampaignData.RewardSpec[]> onComplete = null, Action onError = null)
        {
            bool premium = SaveGame is DraftEventSave d && d.isPremium;
            if (premium)
            {
                Debug.LogWarning("[DraftEventBase] RefundRun：付费档退赛要服务器（原版这一跳打云脚本收奖励）"
                               + "⇒ 本地做不到，走 onError（⛔ 不假装退回奖励）");
                if (onError != null) onError();
                return;
            }
            Debug.Log("[DraftEventBase] RefundRun：非付费档 ⇒ 按原版回**空表**（轮抽不做奖励）");
            if (onComplete != null) onComplete(Array.Empty<CampaignData.RewardSpec>());
        }

        // ============================================================ 过期（账 P1 · 轮抽.tsv）
        /// <summary>原版 `public override void Expire(Action onComplete)`
        /// （`DraftEventBase.cs:5030`，**控制流可读**；两个 lambda `…_Expire_b__0` / `_b__1` 里能读到
        /// `EventComponentReferenceType.ExpirePopup` + `DraftModeExpirePopup` + `CloudHandle.ExpireDraftEvent`）：
        /// <code>
        /// WindowsManager.Instance.CloseWindow&lt;DraftModeMenuDemo&gt;();         // ★ 先关窗
        /// if (DraftState &lt; SelectWarlord(5)) {                              // 还没进到选战将
        ///     if (!AlliancesManager.Instance.HasJoinedEvent(GetID())) {
        ///         base.Expire(null);                                        // ★ 没加入过 ⇒ 直接基类过期
        ///         return;
        ///     }
        /// }
        /// … 取 ExpirePopup 组件（EventComponentReferenceType.ExpirePopup）→ 开 DraftModeExpirePopup …
        /// if (DraftState == OngoingRun(15)) { FinishRun(save =&gt; …); }       // 正在跑 ⇒ 先把这一轮收尾
        /// else { CustomDebug.Log("Trying to finish an unstarted run (this will not do anything)"); onComplete?.Invoke(); }
        /// </code>
        /// 🔴 **两处阈值照抄**：`&lt; 5`（`SelectWarlord`，判「还没真正加入」）与 `== 15`（`OngoingRun`）。
        /// ⚠️ 三处没有落点：`AlliancesManager.HasJoinedEvent`（走注入口）、`ExpirePopup` 组件（`LiveOpsAssetUtility`
        /// 属 B13）、云脚本 `ExpireDraftEvent`（服务器）⇒ 各自出声。
        /// <para>⚠️ `DraftState` 从哪来：原版读 `DraftEventBase + 0x28`；我们的本局正本是
        /// `DraftModeWindow.RunForTest`（`DraftRunSave`）—— 见 <see cref="CurrentDraftState"/>。</para></summary>
        public override void Expire(Action onComplete)
        {
            // ① 先关窗（原版 `CloseWindow<DraftModeMenuDemo>()`）
            var w = DraftModeWindow.LastOpened;
            if (w != null) w.Close();
            else Debug.Log("[DraftEventBase] Expire：当前没有打开的轮抽窗（原版这一跳是 CloseWindow<DraftModeMenuDemo>）");

            int state = CurrentDraftState;
            if (state < (int)DraftModeState.SelectWarlord)               // < 5
            {
                bool joined = HasJoinedEvent != null ? HasJoinedEvent(GetID()) : true;
                if (HasJoinedEvent == null)
                    WarnOnce("hasjoined", "[DraftEventBase] `HasJoinedEvent` 没接（原版读 AlliancesManager.HasJoinedEvent）"
                                        + " ⇒ 当成「已加入」，让过期继续往下走");
                if (!joined)
                {
                    base.Expire(null);                                  // ★ 没加入过 ⇒ 直接基类过期
                    return;
                }
            }

            Debug.Log("[DraftEventBase] Expire：'ExpirePopup' 组件（EventComponentReferenceType.ExpirePopup）"
                    + "属 B13 的 `LiveOpsAssetUtility`，本批没有落点 ⇒ 不弹过期窗");

            if (state == (int)DraftModeState.OngoingRun)                 // == 15
            {
                if (w != null) w.FinishRun();                            // 原版 `FinishRun(save => …)`
                else Debug.LogWarning("[DraftEventBase] Expire：正在跑但拿不到窗 ⇒ 这一轮没法收尾（原版 FinishRun 那一跳）");
                // 原版收尾回调里还会打云脚本 ExpireDraftEvent（`…_Expire_b__1`）⇒ 本地没有服务器
                Debug.Log("[DraftEventBase] Expire：收尾后原版打云脚本 CloudHandle.ExpireDraftEvent（本地跳过）");
            }
            else
            {
                Debug.Log("[DraftEventBase] Expire：Trying to finish an unstarted run (this will not do anything)");
                if (onComplete != null) onComplete();
            }
        }

        /// <summary>本局当前状态（原版 `DraftEventBase + 0x28` 那一格 = `DraftModeState`）。
        /// <para>⚠️ 我们的正本是 `DraftModeWindow` 的静态 run（`DraftRunSave`）—— 它没有存 `draftState` 这一格
        /// （页签状态由窗自己算）⇒ 这里**按 `DraftModeWindow` 的算法反推**：
        /// `joinState == Finished(2)` ⇒ `Finished(20)`；否则 `payState == NotPaid &amp;&amp; joinState == Joined` ⇒ `Intro(0)`；
        /// 其余 ⇒ `SelectWarlord(5)`（= 原版 `UpdateState` 那两条判据，见 `DraftModeWindow.UpdateState` 的注释）。
        /// 这是**反推**，不是原版那一格本身 —— 所以 `Expire` 里「`== 15` 正在跑」这一支**基本走不到**
        /// （原版那一格在跑对局时会被置 15，我们这边没有对应的写点）。</para></summary>
        public virtual int CurrentDraftState
        {
            get
            {
                var run = DraftModeWindow.RunForTest;
                if (run == null) return (int)DraftModeState.Intro;
                if (run.joinState == (int)DraftJoinState.Finished) return (int)DraftModeState.Finished;
                if (run.joinState == (int)DraftJoinState.Joined && run.payState == (int)DraftPayState.NotPaid)
                    return (int)DraftModeState.Intro;
                return (int)DraftModeState.SelectWarlord;
            }
        }

        // ============================================================ 2026-10-28（B89-R2d · 账 L585）草稿入口 gate
        /// <summary>原版 `PlayerDataManager.demoData.canEnterDraftMode` 的可注入替身（本类文件头「差异 ②」明列「无等价物」的那一格）。
        /// 生产者 = 练习首胜（`PracticeEvent.OnMatchWon`，`Everguild.LiveOps/PracticeEvent.cs:95` ⇒ `canEnterDraftMode = true`）——
        /// 本批落点**不在本类**（练习侧/`GameModesHandler` 归 E11b，见 `报告/B89_R2d_日常G6落点补齐.md` §L585）。
        /// null ⇒ 视为**允许**（⛔ 不因缺数据把轮抽锁死 —— 那是回归）。</summary>
        public static Func<bool> CanEnterDraftMode;

        /// <summary>现在能不能进轮抽（= 原版 `canEnterDraftMode` 的读取点）：注入为 null ⇒ **true**（允许，保持改前「轮抽恒可进」的行为）。</summary>
        public static bool CanEnterDraft { get { return CanEnterDraftMode == null || CanEnterDraftMode(); } }

        // ============================================================ 类型化读口（账 轮抽:9）
        /// <summary>原版 `public new DraftEventData GetBaseData()`（`out_cpp2il_dev_ilspy/Everguild.LiveOps/DraftEventBase.cs:4136`；
        /// **泛型类方法 ⇒ `decomp_full` 无独立产物**，读 ILSpy）：先取基类 `PlayEvent.GetBaseData()` → `null ⇒ null` →
        /// 判型可赋给 `DraftEventData` ⇒ 返回。**纯取值器**，没有任何副作用。
        /// <para>⚠️ 本地等价成立：`LiveOpsEvent&lt;TSave,TData&gt;.Data`（`Shell/LiveOpsEvent.cs:731`）本身就是 `TData`，
        /// 在此 = `DraftEventData`；父类那句 `GetBaseData()`（`Shell/PlayEvent.cs:445`）返回的也是**同一个字段**的向上转型
        /// ⇒ 直接返回 `Data` 与「走基类再判型」结果一致。与 `PlayEvent.GetBaseData()` 同址形态（`Shell/PlayEvent.cs:445`）。</para></summary>
        public new DraftEventData GetBaseData() { return Data; }

        /// <summary>原版 `public new DraftEventSaveGame GetBaseSave()`（`out_cpp2il_dev_ilspy/Everguild.LiveOps/DraftEventBase.cs:4171`；
        /// **泛型类方法 ⇒ `decomp_full` 无独立产物**，读 ILSpy）：与上面那行 `GetBaseData()` **同形**的类型化读口。
        /// <para>判据：同族三个兄弟都各有一个同名取口 —— `FastModeBaseEvent.cs:153`（`new FastModeSave`）·
        /// `GameNewsEvent.cs:103`（`new GameMessagePersistence`）· `ExpansionPassEvent.cs:343`（`ExpansionPassSave`），
        /// 只有本类漏了 ⇒ 2026-10-29 补齐（账 `轮抽:10`，调度台批准）。
        /// ⚠️ **返回类型写 `TS` 而不是具体的 `DraftEventSave`**：本类约束是 `where TS : PlayEventSaveGame`，
        /// 原版那个具体类型落到我们的泛型形参上就是 `TS`（实例化时 = `DraftEventSave`）⇒ 可观察行为相同，
        /// 且不必收紧约束（收紧会连带改子类的类型实参，风险大于收益）。</para></summary>
        public new TS GetBaseSave() { return SaveGame; }
    }

    // 🔴 本文件**不给具体子类**：原版的两个具体子类（`DraftEvent` / `TimedDraftEvent`）各有自己的
    // 入口与账行，都**不在本批**；而 `PlayEvent.EventPlayMode` 是抽象（每个活动声明自己打哪个模式），
    // 本批**没有判据**说轮抽对应 `PlayModes` 里的哪一个 ⇒ 硬写一个就是编。
    // ⇒ `DraftEventBase<TS,TD>` 保持抽象，具体子类由「轮抽模式」那条线落地时再写（那时才有 EventPlayMode 的判据）。
}
