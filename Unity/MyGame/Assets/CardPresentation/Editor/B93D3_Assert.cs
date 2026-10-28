// B93D3_Assert.cs — 执行代理 `B93_D3`（2026-10-28）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何宿主的 `RunCore` 正文）。
//
// ============================ 主对话怎么挂（⚠️ 本批没挂：宿主被人占着） ============================
// 🔴 **本批 `Editor/MainMenuScene.cs` 正被别的代理写**（开工时 mtime 每 60 秒还在变）⇒ 按简报的规矩
//    **不抢宿主**，断言写成这个独立文件，接线交给调度台。挂法（照 `ProfileWindowAssertE12` 的既有惯例，
//    放在 `RunCore` 里**任何一段窗断言之后**都行 —— 本节自带开窗/收窗，不依赖前文）：
//
//     B93D3Assert.Run();
//     _sink.Pass += B93D3Assert.Pass; _sink.Fail += B93D3Assert.Fail;
//
//     ⚠️ 三行加在 `MainMenuScene.cs` ⇒ 下游行号整体 +3；重同步点跑一次
//        `check_ledger_points --all-A --fix` 即可（本批不跑 Unity，故没替你跑）。
//
// ============================ 覆盖哪些账行（逐条对应账补丁 `账/_parts/B93_D3.tsv`） ============================
//  账 26  挑战钮 `OnButtonClick`：队列空 ⇒ **只隐藏自己**；有挂起 ⇒ 弹确认框（官方 `ReceiveChallenge` 文案 + OK/Cancel）
//  账 43  接受挑战 ⇒ 撤挂起 + 建局入口（联机不在 ⇒ false + 出声，⛔ 不假装开了一局）
//  账 44/50 `AcceptedFriendDuelNotification` / `OnRemoveFriendDuel` ⇒ **只撤那一个人**（灭自证：别的挂起项必须还在）
//  账 45/46 `CancelDueling` / `CancelPracticeInvitation` ⇒ 状态回 Idle
//  账 47  `CancelPracticeMatchAndRefresh`：命中/未命中两档都不抛，未命中返回 false
//  账 51  `OpenStartChallengeWindow` ⇒ 真的开出 `DuelPopupWindow` 且对手名灌进去了
//  账 53/52 `Start` / `SendPracticeInvitation` ⇒ provider 幂等 + 发出后状态 = `InviteSent(3)`
//  账 584/585 练习局收尾：只认 `OfflinePractice`/`OwnDeckTraining`（灭自证：`Classic` 不计）、
//            胜/负/平各 +1、**只有胜局**才解锁草稿（灭自证：只输一场不解锁）
//  账 546 活动钮红点类型登记：**只有 EVENT 那颗（idx ≥ 5）**带 `EventNavBadge`（灭自证：PLAY 那颗没有）
//  账 549 倒计时按秒泵：1 Hz 节流 + **真的把 `Timer Text` 改写了**（灭自证：先写一个假串，靠"被改写"证明，而不是只断纯函数）
//  账 557/571 `PlayEvent.GetEventDeck` 的 ②/③ 分岔口 + `StartMatchNoCheck` 的**公开单一入口**（结构断言，见报告）
// ==================================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using CardPresentation.Net;
using UnityEngine;

public static class B93D3Assert
{
    const string P = "[B93D3] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static void Ok(bool cond, string what)
    {
        if (cond) { Pass++; Debug.Log(P + "✓ " + what); }
        else { Fail++; Debug.LogError(P + "✗ " + what); }
    }
    static void Eq<T>(T got, T want, string what)
    {
        bool ok = EqualityComparer<T>.Default.Equals(got, want);
        Ok(ok, what + "（期望 `" + want + "`，实得 `" + got + "`）");
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            RunChallengeFamily();
            RunEventButtonBadgeAndTimer();
            RunPracticeSettlement();
            RunPlayEventShape();
        }
        catch (Exception ex)
        {
            Fail++; Debug.LogError(P + "✗ 本组断言抛了异常（后面的没跑完）：" + ex);
        }
        Debug.Log(P + "合计：通过 " + Pass + " / 失败 " + Fail);
    }

    // ============================================================ ① 挑战族（账 26/43/44/45/46/47/50/51/52/53）
    static void RunChallengeFamily()
    {
        // ---------- 干净起点（⛔ 不依赖前面跑过什么）----------
        ChallengeManager.Reset();
        ChallengeData.ClearPending();
        Eq(ChallengeData.PendingCount, 0, "（前提）挂起队列清空");

        // ---------- 账 26：队列空 ⇒ 只隐藏自己，不弹任何窗 ----------
        var btnRoot = NewNode("B93D3 Challenge Button");
        btnRoot.SetActive(true);
        var wmBefore = WindowsManager.Get();
        ChallengeManager.OnChallengeButtonClick(btnRoot);
        Ok(!btnRoot.activeSelf, "账 26：挂起队列为空时，点挑战钮 ⇒ **把自己隐藏**（原版 `OnButtonClick.c` 最后那句 `SetActive(false)`）");
        Ok(wmBefore == null || !(wmBefore.popUpWindow is PopUpGameWindow),
           "账 26 灭自证：队列为空时**不弹确认框**（`challenges.Count == 0` 那一支）");

        // ---------- 账 26 另一半：有挂起 ⇒ 弹确认框（文案来自官方词条）----------
        var pendingA = new PracticeChallenge { PlayerName = "B93D3-Alpha", ModeKey = null, PlayerFirst = true };
        ChallengeData.AddPending(pendingA);
        Eq(ChallengeData.PendingCount, 1, "（前提）挂起 1 条");
        btnRoot.SetActive(true);
        ChallengeManager.OnChallengeButtonClick(btnRoot);
        var wm = WindowsManager.Get();
        Ok(wm != null, "（前提）`WindowsManager` 在（否则下面几条没法验）");
        var pop = wm != null ? wm.popUpWindow as PopUpGameWindow : null;
        Ok(pop != null, "账 26：有挂起挑战时，点挑战钮 ⇒ **弹出确认框**（原版 `WindowsManager.ShowPopUp` 那一跳）");
        if (pop != null)
        {
            // 期望串取**官方词条表**（⛔ 不是拿我们自己的常量回读 —— 那样是自证）
            string fmt = Loc.OfficialCurrent("Demo/FriendsMenu/ReceiveChallenge");
            string expect = fmt == null ? null : string.Format(fmt, "B93D3-Alpha", "");
            Ok(!string.IsNullOrEmpty(pop.MessageShown) && (expect == null || pop.MessageShown == expect),
               "账 26：确认框正文 = 官方 `Demo/FriendsMenu/ReceiveChallenge` 用「对方名 + 模式词条」格式化"
             + "（正文实得 `" + (pop.MessageShown ?? "<null>") + "`）");
            Ok(pop.TwoButtons && pop.HasSecondary,
               "账 26：确认框是**两颗钮**那一档（原版 `ShowPopUp(text, …, source, source)` 两粒回调）");
            // 🔴 **左 = Cancel / 右 = OK**（原版实参序：`ChallengeButton__OnButtonClick.c` 那句
            //   `ShowPopUp(text, 0, 1, lVar2/*Cancel*/, lVar5/*OK*/, 0)` + `WindowsManager.cs:335` 第 4 参 = `primaryButton`
            //   + `PopUpGameWindow.cs:228/500`：`primary = LiveButtons[0] = ButtonLeft`）
            string okText = Loc.OfficialCurrent("Demo/MainMenu/OK");
            string cancelText = Loc.OfficialCurrent("Demo/MainMenu/CancelButton");
            if (cancelText != null) Eq(pop.PrimaryShown, cancelText, "账 26：确认框**左钮**文案 = 官方 `Demo/MainMenu/CancelButton`（原版左 = Cancel）");
            if (okText != null) Eq(pop.SecondaryShown, okText, "账 26：确认框**右钮**文案 = 官方 `Demo/MainMenu/OK`（原版右 = OK）");
        }
        Ok(!btnRoot.activeSelf, "账 26：弹完确认框之后挑战钮**仍然是隐藏的**（原版最后那句在弹窗之后）");
        if (wm != null) wm.HidePopUp(true);

        // ---------- 账 47：撤一条不存在的 ⇒ false，且不抛 ----------
        Ok(!ChallengeManager.CancelPracticeMatchAndRefresh("B93D3-不存在"),
           "账 47：`CancelPracticeMatchAndRefresh` 撤一条不在队列里的 ⇒ 返回 false（原版 `RemovePending` 同样不命中）");
        Eq(ChallengeData.PendingCount, 1, "账 47 灭自证：未命中的那一次**不能顺手把别的挂起项清掉**");

        // ---------- 账 44/50：只撤那一个人（灭自证：别的还在）----------
        ChallengeData.AddPending(new PracticeChallenge { PlayerName = "B93D3-Beta" });
        Eq(ChallengeData.PendingCount, 2, "（前提）挂起 2 条");
        ChallengeManager.AcceptedFriendDuelNotification("B93D3-Alpha");
        Eq(ChallengeData.PendingCount, 1, "账 44：`AcceptedFriendDuelNotification(Alpha)` ⇒ 只撤 Alpha 那一条");
        Ok(ChallengeData.PendingPracticeMatches[0].PlayerName == "B93D3-Beta",
           "账 44 灭自证：剩下的那条还是 **Beta**（不是「清空队列」也不是「撤错人」）");
        Eq(ChallengeManager.State, DuelState.Idle, "账 44：收掉决斗态（`CancelDueling(0)` 的第二句）");
        ChallengeManager.OnRemoveFriendDuel("B93D3-Beta");
        Eq(ChallengeData.PendingCount, 0, "账 50：`OnRemoveFriendDuel(Beta)` ⇒ 撤掉 Beta（与上一支同体）");

        // ---------- 账 52/53/45/46：邀约状态机 ----------
        ChallengeManager.Reset();
        ChallengeManager.SendPracticeInvitation();
        Eq(ChallengeManager.State, DuelState.Idle,
           "账 52 灭自证：**没有对手名**时 `SendPracticeInvitation` 不发（原版那一格为 null ⇒ 私聊那跳 NRE）⇒ 状态停在 Idle");
        // 有对手名时：先用反射把 `TargetPlayerName` 灌进去（生产由 `AcceptPracticeChallenge` / 邀约接收侧写）
        SetStaticProp(typeof(ChallengeManager), "TargetPlayerName", "B93D3-Gamma");
        ChallengeManager.SendPracticeInvitation();
        Eq(ChallengeManager.State, DuelState.InviteSent, "账 52：发出邀约后状态 = `InviteSent(3)`（原版明写 `+0x28 = 3`）");
        ChallengeManager.CancelPracticeInvitation();
        Eq(ChallengeManager.State, DuelState.Idle, "账 46：撤回邀约 ⇒ 状态回 `Idle`");
        ChallengeManager.SendPracticeInvitation();
        bool threw = false;
        try { ChallengeManager.CancelDueling(true); } catch (Exception ex) { threw = true; Debug.LogError(P + "`CancelDueling` 抛了：" + ex); }
        Ok(!threw, "账 45：`CancelDueling(fromClick: true)` 在联机层没有活着的匹配时**不抛**（那是一条有闸的轻动作）");
        Eq(ChallengeManager.State, DuelState.Idle, "账 45：`CancelDueling` 后状态 = Idle");

        // ---------- 账 51：真的开出好友挑战弹窗 ----------
        var duel = ChallengeManager.OpenStartChallengeWindow("B93D3-Delta");
        Ok(duel != null, "账 51：`OpenStartChallengeWindow` ⇒ 建出 `DuelPopupWindow`（原版那两句 `WindowsManager.OpenWindow`）");
        if (duel != null)
        {
            Eq(duel.OpponentName, "B93D3-Delta", "账 51：对手名灌进了那扇窗");
            Eq(DuelPopupWindow.LastOpened, duel, "账 51：`DuelPopupWindow.LastOpened` 指向刚开的那扇");
            Ok(duel.CurrentState == WindowState.Open, "账 51：窗真的开着了（`Open()` 跑过）");
            duel.Close();
        }

        // ---------- 账 43：接受挑战 ⇒ 建局入口（联机不在 ⇒ false + 出声）----------
        string why;
        bool started = NetMatchmaking.TryStart(null, RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OfflinePractice), null, out why);
        Ok(!started && !string.IsNullOrEmpty(why),
           "账 43 的前提：批处理里**没有联机会话** ⇒ P2P 开局返回 false 且有原因（`" + why + "`）");
        ChallengeManager.Reset();
        bool acc = ChallengeManager.AcceptPracticeChallenge(
            new PracticeChallenge { PlayerName = "B93D3-Epsilon", ModeKey = null, PlayerFirst = false });
        Ok(!acc, "账 43：没有联机会话时 `AcceptPracticeChallenge` ⇒ **不假装开成**（返回 false，原因已出声）");

        // ---------- 账 53：`Start()` 幂等（红点 provider 只登记一次）----------
        int pc0 = UiBadgeNotificationManager.ProviderCount;
        ChallengeManager.Start();
        int pc1 = UiBadgeNotificationManager.ProviderCount;
        ChallengeManager.Start();
        int pc2 = UiBadgeNotificationManager.ProviderCount;
        Ok(pc1 >= pc0 && pc2 == pc1, "账 53：`Start()` 幂等（provider 数 " + pc0 + " → " + pc1 + " → " + pc2 + "）");

        // 收尾：不留脏（后面几组与挑战无关，但挂起队列会牵动红点）
        ChallengeData.ClearPending();
        ChallengeManager.Reset();
        if (btnRoot != null) UnityEngine.Object.DestroyImmediate(btnRoot);
    }

    // ============================================================ ② 活动钮：红点类型登记 + 倒计时按秒泵（账 546 / 549）
    static void RunEventButtonBadgeAndTimer()
    {
        Ok(MainMenuRuntime.NavBadgeHasEventBadgeForTest(5),
           "账 546：EVENT 那颗（idx 5）的红点定义里有 `EventNavBadge`（= 原版 `OpenEventButton.Initialize` 运行期 "
         + "`AddNotificationType` 那一句；判据 `OpenEventButton__Initialize.c`）");
        Ok(!MainMenuRuntime.NavBadgeHasEventBadgeForTest(0),
           "账 546 灭自证：PLAY/Home 那颗 **没有** `EventNavBadge`（原版那 5 颗导航钮的定义是烘死的，⛔ 不是「全部加一条」）");

        // provider：条数 == 本地活动目录里在期 Parent 事件数（今天 13 份 SO 全过期 ⇒ 0 条）
        // 🔴 期望值在**断言侧独立重算**（手写谓词，不回读被测实现的 `ActiveParentEventsForNav`）
        int activeN = 0;
        var evTable = LiveOpsLocalCatalog.Events;
        for (int i = 0; i < evTable.Count; i++)
        {
            var e = evTable[i];
            if (e != null && e.Kind == LiveOpsLocalKind.Parent && e.IsCurrentlyActive(System.DateTime.UtcNow) && e.Selectable) activeN++;
        }
        var prov = new EventNavBadgeProvider();
        int yielded = 0; foreach (var _ in prov.CheckNotification()) yielded++;
        Eq(yielded, activeN, "账 546：`EventNavBadgeProvider` 发出的条数 == 在期 Parent 活动数（今天 = 0 条，不是写死 false）");

        // ---------- 账 549：格式串 + 按秒泵 ----------
        var t0 = new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        Eq(MainMenuRuntime.NavTimerTextForTest(t0.AddHours(26)), "1d 2h 0m", "账 549：`26h` ⇒ `1d 2h 0m`（prefab 样例串 `5d 20h 15m` 那一族）");
        Eq(MainMenuRuntime.NavTimerTextForTest(t0.AddMinutes(90)), "1h 30m", "账 549：`90m` ⇒ `1h 30m`（无天时省掉 `d` 段）");

        // 泵：造一颗假活动钮（带 `Timer Text` 子件），先把字写成一个假串，再推一次 ⇒
        //   ① 被改写成**期望串**（不是「与假串不同」那种弱断言）；② 同一秒内再推 ⇒ 0 次改写（1 Hz 节流）。
        var btn = NewNode("B93D3 Event Nav Button");
        var lbl = MenuDraw.Text(btn.transform, new PxRect(0f, 0f, 200f, 30f), "STALE", Color.white,
                                "Timer Text", 26.35f, 0);
        Ok(lbl != null, "（前提）假活动钮的 `Timer Text` 建出来了");
        if (lbl != null)
        {
            var end = System.DateTime.UtcNow.AddMinutes(95);
            MainMenuRuntime.RegisterEventNavTimerForTest(btn.transform, end);
            int n1 = MainMenuRuntime.StepEventNavTimersForTest(1.0f);
            string want = MainMenuRuntime.NavTimerTextForTest(end);
            Ok(n1 >= 1 && lbl.Text == want,
               "账 549：按秒泵**真的把 `Timer Text` 改写了**（`STALE` → `" + want + "`，改写数 " + n1 + "）");
            int n2 = MainMenuRuntime.StepEventNavTimersForTest(0.2f);
            Eq(n2, 0, "账 549 灭自证：同一秒内再推一次 ⇒ **0 次改写**（1 Hz 节流；⛔ 不是每帧刷 TMP）");
        }
        if (btn != null) UnityEngine.Object.DestroyImmediate(btn);
    }

    // ============================================================ ③ 练习局收尾（账 584 / 585）
    static void RunPracticeSettlement()
    {
        GameModesHandler.ResetPracticeForTest();
        Eq(GameModesHandler.PracticeSave.numberOfMatchesWon, 0, "（前提）练习存档从 0 起");
        Ok(!GameModesHandler.DraftModeUnlocked,
           "账 585：草稿闸的**默认值 = false**（原版 `DemoData.canEnterDraftMode` 首胜前为 false；2026-10-28 由兜底 true 翻正）");

        // ---------- 只认练习的两个模式号（灭自证：Classic 不计）----------
        var log = new List<BattleLogData.Match>
        {
            new BattleLogData.Match { Mode = RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.Classic),        Result = BattleLogData.Outcome.Victory },
            new BattleLogData.Match { Mode = RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OfflinePractice), Result = BattleLogData.Outcome.Victory },
            new BattleLogData.Match { Mode = RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OwnDeckTraining), Result = BattleLogData.Outcome.Defeat },
            new BattleLogData.Match { Mode = RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OfflinePractice), Result = BattleLogData.Outcome.Draw },
        };
        var outs = GameModesHandler.PracticeOutcomes(log);
        Eq(outs.Count, 3, "账 584 灭自证：4 条记录里只有 **3 条**是练习局（`Classic` 那条不算）");
        Ok(outs.Count == 3 && outs[0] == BattleWinner.Player && outs[1] == BattleWinner.Enemy && outs[2] == BattleWinner.Draw,
           "账 584：胜负平三档逐条映射对（`Victory→Player / Defeat→Enemy / Draw→Draw`）");

        var gm = LiveOpsManager.GetHandler<GameModesHandler>();
        Ok(gm != null, "（前提）取得到 `GameModesHandler`（`LiveOpsManager.GetHandler<GameModesHandler>()`）");
        if (gm != null)
        {
            int n = gm.IngestPracticeResults(log);
            Eq(n, 3, "账 584：回灌吃了 3 场");
            Eq(GameModesHandler.PracticeSave.numberOfMatchesWon, 1, "账 584：胜 1（= 原版 `PlayEvent.OnMatchWon` 的 `+0x48`）");
            Eq(GameModesHandler.PracticeSave.numberOfMatchesLost, 1, "账 584：负 1（`+0x4C`）");
            Eq(GameModesHandler.PracticeSave.numberOfMatchesDraw, 1, "账 584：平 1（`+0x50`）");
            Ok(GameModesHandler.DraftModeUnlocked, "账 585：**有胜局** ⇒ 草稿闸解锁（原版 `OnMatchWon` 的 `canEnterDraftMode = true`）");
            Eq(gm.IngestPracticeResults(log), 0, "账 584：同一份记录再吃一次 ⇒ 0 场（游标不重复记）");
        }

        // ---------- 灭自证：只输不解锁 ----------
        GameModesHandler.ResetPracticeForTest();
        var onlyLoss = new List<BattleLogData.Match>
        {
            new BattleLogData.Match { Mode = RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OfflinePractice), Result = BattleLogData.Outcome.Defeat },
        };
        if (gm != null) gm.IngestPracticeResults(onlyLoss);
        Eq(GameModesHandler.PracticeSave.numberOfMatchesLost, 1, "账 584：只输一场 ⇒ 负 1");
        Ok(!GameModesHandler.DraftModeUnlocked,
           "账 585 灭自证：**只输一场不解锁**草稿（⛔ 若闸被写成恒 true / 恒在 FirstIngest 时打开，这里必红）");
        GameModesHandler.ResetPracticeForTest();
    }

    // ============================================================ ④ PlayEvent 的形状（账 557 / 571）
    //   ⚠️ 只做**结构断言**：这两个方法要真正跑到，需要一场活动实例 + 一副卡组 + 存档（那是 `RuleEngineTest`/
    //      联机自检的夹具），本批（不跑 Unity 批处理）没有那条夹具 ⇒ 这里只钉「入口的形状」，
    //      行为那一半已在账里写明缺口/另由 `LiveOps` 那批的夹具覆盖。
    static void RunPlayEventShape()
    {
        var t = typeof(PlayEvent<PlayEventSaveGame, PlayEventData>);
        var m = t.GetMethod("StartMatchNoCheck", BindingFlags.Public | BindingFlags.Instance);
        Ok(m != null,
           "账 571：`StartMatchNoCheck` 是**公开单一入口**（原版 `public void StartMatchNoCheck(CardDeck)`；"
         + "2026-10-28 由 `private` 放开 —— 让「传卡组跳过校验」有一条可调的公开路）");
        var gate = t.GetMethod("GetEventDeckRankedOrNull", BindingFlags.NonPublic | BindingFlags.Instance);
        Ok(gate != null,
           "账 557：②/③ 那个分岔口独立成了 `GetEventDeckRankedOrNull`（原版：**有 handler ⇒ ② 的结果就是终值，不进 ③**）");
    }

    // ============================================================ 小工具
    static GameObject NewNode(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        return go;
    }

    static void SetStaticProp(Type t, string name, object value)
    {
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        if (p != null && p.CanWrite) { p.SetValue(null, value); return; }
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f != null) f.SetValue(null, value);
    }
}
