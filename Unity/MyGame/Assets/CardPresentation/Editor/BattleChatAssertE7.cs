// BattleChatAssertE7.cs — 执行代理 `E7`（2026-10-24）的断言宿主
//
// 本文件**只放断言**（⛔ 不改任何产品行为、不建场景、不进 `Editor/BattleScene.cs`）。
// 调用方式（主对话在 `BattleScene.RunCore` 的收尾处加一行）：
//
//     BattleChatAssertE7.Run();
//     pass += BattleChatAssertE7.Pass; fail += BattleChatAssertE7.Fail;   // 想并进总数就加这句
//
// ⚠️ **需要 driver 实例**：C 节（聊天 / SmallTip）走 `Object.FindObjectOfType<BattleDriver>()`；
//    A 节（引擎召唤路）自己建 `BattleContext`，**不依赖 driver**（找不到 driver 时 A 节照跑）。
//
// ==================================================================
//  三件覆盖的内容（判据逐条写在每条断言的消息里）
// ------------------------------------------------------------------
//  A. **`ThisUnitSummoned(195)` 的「被效果召唤」途径**（判据 →
//     `资料/普查产出_1024b/C2_引擎战斗工具判据总表.md` 战斗1）；
//  B. **对手聊天「发言触发面」**：原版 `AIResponse` 的 `IsAgainstAI` 闸 + 「掷中之后还要等
//     `[min_reply_delay,max_reply_delay] + 0.3 s`」（判据 → `普查产出_1024b/E4_录像头与对手聊天.md`
//     · 本文件头部/BattleDriver 那一段注释里的机器码实读）；
//  C. **第 5 条之后的 `MuteEnemyChat` SmallTip**（判据 = `DisplayWarlordChatMessage.c` 末尾那一支）；
//  D. **跨局复位**：`_aiReplyPending` / `_aiEmotePending` 必须跟着 `Begin` 清
//     （本轮顺手查出并修掉的**跨局泄漏**：不清 ⇒ 之后每一局 AI 都不再说话，静默）。
// ==================================================================
using System.Collections.Generic;
using CardPresentation;
using CardPresentation.Net;      // `NetBattle`（B 节的联机闸要用它）
using RuleEngine;
using UnityEngine;

public static class BattleChatAssertE7
{
    const string P = "[E7] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进 `BattleScene` 的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        // ---------- A · 195 的「被效果召唤」途径（自己建上下文，不需要 driver） ----------
        RunSummon195();

        // ---------- B/C · 聊天触发面 + SmallTip（要 driver） ----------
        var driver = Object.FindObjectOfType<BattleDriver>();
        if (driver == null)
        {
            Ok(false, "C：找不到 `BattleDriver`（聊天 / SmallTip 那两节没法验 —— ⛔ 不许静默跳过）");
        }
        else
        {
            RunChatTriggerFace(driver);
            RunMuteEnemyChatTip(driver);
            RunCrossGameReset(driver);       // 放最后：它自己会再 `Begin` 一局（收尾停在「普通单机局」）
        }

        Debug.Log(P + $"=== E7 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  断言小工具（与 `E4` 同形：本类自己记一份 Pass/Fail）
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(EqualityComparer<T>.Default.Equals(got, want), msg + $"（实得 {got} / 期望 {want}）");
    }

    // ==================================================================
    //  A · `ThisUnitSummoned(195)`：**被效果召唤**的单位也要发
    // ==================================================================
    //  判据（第一权威，逐句）：
    //   · `BattleManagerSupport__BroadcastUnitSummoned.c:21` —— 广播**先**给被召唤的那个自己
    //     `CardScript.ResolveUnitSummoned(param_2, param_2, …)`（两个实参是**同一张卡**）；
    //   · `CardScript__ResolveUnitSummoned.c:40-53` —— `op_Equality(param_2, param_1)` ∧
    //     `get_cardType == 0 (Minion)` ∧ `cardState(+0x228) != 5` ⇒
    //     `OnTrigger(0xc3 = 195 ThisUnitSummoned)` + `OnTrigger(0x25d = 605 UseFaith)`；
    //     `:25-34` 是**另一支** `op_Inequality` ⇒ `0xbe = 190 OtherUnitSummoned`（**别人**被召唤）。
    //   · **谁调广播器**：全库**只有两个协程** ——
    //     `_ResolveCardAutoSummoned_d__511`（从手牌免费打出）与 `_ResolveSummonUnit_d__510`
    //     （**效果召唤**）。⇒ 「法术造兵」这一条路**同样**要发 195。
    //   · 我们这一侧：两条部署入口 `RuleCore.cs:3627`（`PlayCard`）/ `:4322`（`DeployFree`）
    //     都调 `PreCallBoardListener(…, selfOnlyOnly: true)`，而**所有**「效果召唤 / 复制 / 凭空造 /
    //     从牌库 / 墓地退路」都汇到 `DeployFree`（`EffectResolver.cs:3168/3217/3302/3369/3380/4274/13345`）
    //     ⇒ 判据上**已覆盖**；本节把它钉成断言。
    //   ⚠️ **不覆盖的一条**（如实记，不在这里假装覆盖）：`EffectResolver.cs:13273` 那条
    //     「残骸翻回来」直写棋盘、**不发**部署广播 —— 原版对应的是 `ResolveTransformFromRemnant`
    //     （全库 `BroadcastUnitSummoned` 只有那两处调用点 ⇒ 原版这一路**也不发**）⇒ 两边一致，不补。
    static void RunSummon195()
    {
        Debug.Log(P + "--- A · `ThisUnitSummoned(195)`：法术召唤的单位也发 ---");

        var pool = CardDatabase.Load();
        if (!Ok(pool != null && pool.Count > 0, "A0（前提）卡池载到了")) return;
        var sw10 = CardDatabase.FindById(pool, "SW10");
        if (!Ok(sw10 != null, "A0（前提）`SW10 Grey Hunter` 在卡池里（全池**唯一**带 195 的卡）")) return;
        Eq(sw10.AbilityTriggerPresence("ThisUnitSummoned"), 1,
           "A0（前提）`SW10` 的 v21 真值里真有 `ThisUnitSummoned`(195) —— 没有它这条断言恒 0");

        // 它自己那条 `When deployed, …` 必须是 **SelfOnly**（= 预调只跑这一半；非 SelfOnly 的
        // 「When you deploy a troop」走 190 且**排除主体**，判据 = `EffectResolver.cs:3617` 那段）。
        bool selfOnlyDeploy = false;
        foreach (var t in sw10.WhenTriggers)
            if (t.Ev != null && t.Ev.Kind == WhenEventKind.Deploy && t.Ev.SelfOnly) selfOnlyDeploy = true;
        if (!Ok(selfOnlyDeploy, "A0（前提）`SW10` 有一条 **SelfOnly** 的 `When deployed` 监听（195 的正文那一半）")) return;

        // ---- A1 · 法术召唤：`Deploy a Grey Hunter` 走真入口（`PlayTactic` → deploy op → `DeployFree`）----
        var summon = Tactic("E7SummonGreyHunter", 0, "Deploy a Grey Hunter");
        var ctx = NewProbeBattle(new[] { summon }, pool);
        if (!Ok(ctx != null, "A1（前提）对局建起来了")) return;
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);

        int hi = HandIndex(ctx, 0, summon.Name);
        if (!Ok(hi >= 0, "A1（前提）手里那张夹具战术在（`Deploy a Grey Hunter`）")) return;

        // 先摆一张**同一张卡**的夹具单位（直写棋盘 = 绕过部署入口）——
        // 它的实例号与「法术召唤出来的那张」不同 ⇒ 下面能把 195 **归因到具体那一张**。
        var planted = Plant(ctx, 0, 2, sw10);
        ctx.ClearSignals();

        int code = RuleCore.PlayTactic(ctx, 0, hi, -1);
        Eq(code, RuleCodes.OK, "A1 打出夹具战术 `Deploy a Grey Hunter`（真入口 `RuleCore.PlayTactic`）");

        UnitState summoned = null;
        var board = ctx.Players[0].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u == null || u.Card == null) continue;
            if (u.InstanceId == planted.InstanceId) continue;      // 跳过预先摆的那张
            if (u.Card.Id == sw10.Id) { summoned = u; break; }
        }
        if (!Ok(summoned != null, "A1（前提）**法术真的把 `Grey Hunter` 召唤上场了**"
                                + "（否则下面「195 发了 1 条」是假绿 —— 灭自证第一层）")) return;
        Ok(summoned.InstanceId != planted.InstanceId,
           "A1（前提）被召唤的那张与预摆的那张是**两个实例**（下面才能按实例号归因）");

        // 监听器**真的跑了**（日志是「被叫醒」的唯一观测口；⛔ 不是在断文案）
        Ok(LogCount(ctx, "的监听器") >= 1,
           "A1（前提）「`SW10` 的监听器」那行日志出现了（= 预调那一跳真的把正文监听跑了）");

        // ★★ 主断言：195 **恰 1 条**，且**记在被召唤的那张的实例号上**
        Eq(CountTrigger(ctx, "ThisUnitSummoned", sw10.Name, summoned.InstanceId), 1,
           "★★ A1 **法术召唤 ⇒ 被召唤的那张自己发 `ThisUnitSummoned`(195)**（按实例号归因，恰 1 条）"
         + "｜🧨 把 `RuleCore.cs:4322` 那句 `PreCallBoardListener(…, selfOnlyOnly: true)` 删掉、"
         + "或让效果召唤绕开 `DeployFree` ⇒ 本行 0（红）");
        Eq(CountTrigger(ctx, "ThisUnitSummoned", sw10.Name, planted.InstanceId), 0,
           "★★ A1 反向：**预先摆在场上的那张同一张卡一条都不发**（195 只发给「刚被召唤的那个」）"
         + "｜🧨 把 195 发进棋盘循环（`selfOnlyOnly:false` 那一跳）⇒ 这张也会收到 ⇒ 本行非 0（红）");
        Eq(CountTrigger(ctx, "OtherUnitSummoned", sw10.Name), 0,
           "★★ A1 反向：`SW10` **没有** `OtherUnitSummoned`(190) —— 两支不许互相顶替"
         + "（判据 = `ResolveUnitSummoned.c` 里 `op_Equality` / `op_Inequality` **互斥**）");
        Eq(CountTrigger(ctx, "ThisUnitSummoned", sw10.Name), 1,
           "★★ A1 合计：本场 `ThisUnitSummoned` **总共 1 条**（不多不少）"
         + "｜🧨 两条部署入口被同一张卡连走两遍（双预调）⇒ 2（红）");

        // ---- A2 · 与「从手牌打出」同一口径（回归闸：别把效果召唤那条改坏了，也别把打出那条改坏）----
        //   判据：两条原版协程都调**同一个** `BroadcastUnitSummoned` ⇒ 195 各 1 条、行为同形。
        var ctx2 = NewProbeBattle(new CardDef[0], pool);
        if (ctx2 != null)
        {
            if (ctx2.Turn == 0) RuleCore.BeginTurn(ctx2);
            ctx2.Players[0].Hand.Add(ctx2.NewInstance(sw10));      // 手牌里塞一张（夹具：只为走到真入口）
            int hi2 = HandIndex(ctx2, 0, sw10.Name);
            if (Ok(hi2 >= 0, "A2（前提）`SW10` 进手牌了"))
            {
                ctx2.ClearSignals();
                int c2 = RuleCore.PlayCard(ctx2, 0, hi2, 3);
                Eq(c2, RuleCodes.OK, "A2 从手牌真打出 `SW10`（`RuleCore.PlayCard`）");
                Eq(CountTrigger(ctx2, "ThisUnitSummoned", sw10.Name), 1,
                   "★★ A2 从手牌打出 ⇒ **同样恰 1 条** 195（两条入口同一个原版广播器）"
                 + "｜🧨 只给「效果召唤」那条补、把打出的那条删掉 ⇒ 本节两条一起露馅");
            }
        }
    }

    // ==================================================================
    //  B · 对手聊天「发言触发面」：`IsAgainstAI` 闸 + 延迟泵
    // ==================================================================
    //  判据（第一权威，逐句）：
    //   · `VoiceLinesController__AIResponse.c` 开头：`BattleManager.IsAgainstAI` 为假 ⇒ **整支不做**
    //     （联机局对面是**人**；原版同一支里先 `BattleCommsManager.SendChatMessage` 把话送出去，
    //     由**对方那台机器**决定回不回）⇒ 我们的等价物 = `AiShouldDriveOpponent`（`_net == null`）。
    //   · `AIResponse` **先**掷概率、**再** `StartCoroutine(_ChatAIResponse)`；`_ChatAIResponse` 里
    //     `WaitForSeconds(delay + DAT_1834b2dc8)`（= **+0.3 s**，机器码/常量实读见 `BattleDriver` 那段），
    //     之后才 `DisplayWarlordChatMessage(isLocal: 0, force: 0)`。
    //   ⇒ 「掷中就立刻说」是**偏离**；本节钉「排期 → 到点才说」。
    static void RunChatTriggerFace(BattleDriver driver)
    {
        Debug.Log(P + "--- B · 聊天触发面：联机闸 + 「排期 → 到点才说」---");

        bool recSave = BattleDriver.RecordReplays;
        bool mulSave = driver.mulliganEnabled;
        try
        {
            BattleDriver.RecordReplays = false;      // 这一节只碰表现层，不必留录像
            driver.mulliganEnabled = false;
            driver.Begin("Ultramarines", "Goff", 20261024);
            if (!Ok(driver.Ctx != null && driver.UnitChat != null, "B0（前提）新开一局、`UnitChat` 在位")) return;

            // 开场独白期间 `CanAiChat` 是关的（我们加的本地护栏）⇒ 先把它播完
            for (float t = 0f; t < 40f && driver.IntroStage >= 0; t += 0.1f) driver.AdvanceTimeline(0.1f);
            if (!Ok(driver.CanAiChatForTest, $"B0（前提）AI 发言闸开着（`IntroStage`={driver.IntroStage}）")) return;

            // ---- B1 · 自发发言**先排期**，到点才说（原版 `WaitForSeconds(delay + 0.3)`）----
            driver.ResetAiEmoteForTest();
            int emote0 = driver.AiChatEmoteCount;
            bool sched = driver.ScheduleAiEmoteForTest();
            Ok(sched, "★ B1 自发发言**排上了**（`ScheduleAiEmoteForTest` 以必中绕开概率）"
                    + "｜🧨 把 `MaybeAiEmote` 改回「立刻 `SpeakAiChat`」⇒ 本行 false（红）");
            if (sched)
            {
                float d = driver.LastAiEmoteDelayForTest;
                Ok(d >= BattleDriver.AiReplyDelayMin + BattleDriver.AiResponseExtraDelay - 1e-3f
                   && d <= BattleDriver.AiReplyDelayMax + BattleDriver.AiResponseExtraDelay + 1e-3f,
                   $"★ B1 排的延迟落在 `[min,max]+0.3`（实得 {d:F2}s；"
                 + $"原版 = `Random.Range(min_reply_delay, max_reply_delay) + DAT_1834b2dc8(=0.3f)`）"
                 + "｜🧨 漏掉那个 `+0.3` 或改成 `Range(0,N)` ⇒ 越界（红）");
                Eq(driver.AiChatEmoteCount, emote0,
                   "★★ B1 **排期那一刻【还没说】**（原版那一段是 `WaitForSeconds` 之后才 `DisplayWarlordChatMessage`）"
                 + "｜🧨 改回「掷中就立刻说」⇒ 本行实得 +1（红）");
                driver.AdvanceTimeline(Mathf.Max(0f, d - 0.05f));
                Eq(driver.AiChatEmoteCount, emote0,
                   "★★ B1 …差 50 ms 到点时**仍然没说**（钉住「真的等了」，不是同帧就开口）");
                driver.AdvanceTimeline(0.12f);
                Eq(driver.AiChatEmoteCount, emote0 + 1,
                   "★★ B1 到点之后**说了**（`AiChatEmoteCount` +1）"
                 + "｜🧨 延迟泵挂到 `Update` 上（批处理没有帧循环）⇒ 本行 0（红）");
                Ok(!driver.AiEmotePendingForTest, "★ B1 排期标志在到点后清掉（不会卡住下一条）");
            }
            driver.ResetAiEmoteForTest();

            // ---- B2 · 联机闸（原版 `IsAgainstAI`）：本机**不替真人说话** ----
            //   差分：**同一次调用**，只变「有没有联机层」这一个变量。
            for (float t = 0f; t < 5f && driver.AiReplyPendingForTest; t += 0.1f) driver.AdvanceTimeline(0.1f);
            Ok(!driver.AiReplyPendingForTest, "B2a（前提）此刻**没有悬挂的回话**（否则下面那条会因为错的原因变绿）");
            driver.ScheduleAiReplyForTest();           // 单机（`_net == null`）⇒ 应该排上
            Ok(driver.AiReplyPendingForTest,
               "★ B2a（对照）单机局 ⇒ **回话排上了**（`AiShouldDriveOpponent` 为真）");
            for (float t = 0f; t < 4f; t += 0.1f) driver.AdvanceTimeline(0.1f);   // 排空，别让它悬挂
            driver.ResetAiEmoteForTest();

            var dummy = new NetBattle();               // 只用它的存在性（`_net != null`）；不接会话
            driver.AttachNet(dummy);
            try
            {
                Ok(!driver.AiShouldDriveOpponent, "B2b（前提）挂上联机层之后 `AiShouldDriveOpponent == false`");
                driver.ScheduleAiReplyForTest();
                Ok(!driver.AiReplyPendingForTest,
                   "★★ B2b **联机局不排回话**（原版 `AIResponse` 的 `IsAgainstAI` 闸：对面是真人，"
                 + "那句话由**他的机器**决定回不回）"
                 + "｜🧨 把 `ScheduleAiReplyToPlayerChat` 开头那句 `if (!AiShouldDriveOpponent) return;` 删掉 ⇒ 本行 true（红）");
                driver.ResetAiEmoteForTest();
                Ok(!driver.ScheduleAiEmoteForTest(),
                   "★★ B2b 联机局也**不排自发发言**（同一条原版闸）"
                 + "｜🧨 删掉 `MaybeAiEmote` 开头那句 ⇒ 本行 true（红）");
            }
            finally
            {
                driver.AttachNet(null);                // 还原：后面的节要的是单机局
                driver.ResetAiEmoteForTest();
            }
            Ok(driver.AiShouldDriveOpponent, "B2（收尾）联机层摘掉了、回到单机判据");
        }
        finally
        {
            BattleDriver.RecordReplays = recSave;
            driver.mulliganEnabled = mulSave;
        }
    }

    // ==================================================================
    //  C · 第 5 条之后的 `MuteEnemyChat` SmallTip（`+0x6c` / `+0x68` / `+0x40`）
    // ==================================================================
    //  判据 = `VoiceLinesController__DisplayWarlordChatMessage.c` 末尾那一支：
    //    `isLocal == 0 ∧ tipShowed(+0x68) == 0 ∧ msg != 0 ∧ chatMuted(+0x40) == 0`
    //    ⇒ `count(+0x6c)++`；**count > 4** ⇒ `count = 0` · `+0x68 = 1` ·
    //    建 `SmallTipData(L("Battle/Tips/MuteEnemyChat"), 4.0)` 并 **`ShowChatBox(panel, 2 /*RadioChat*/, …)`**
    //    （立绘/音源 = **敌方**督军，`GenerateVoiceLineData(audio: null)` ⇒ 有字无声）。
    //  计数器每回合清零：`BattleManager._NextTurn_d__395:181` → `ResetChatsReceived()`。
    static void RunMuteEnemyChatTip(BattleDriver driver)
    {
        Debug.Log(P + "--- C · 第 5 条之后弹一次 `MuteEnemyChat` SmallTip（一辈子一次 + 静音闸 + 每回合清零）---");

        if (driver.Ctx == null || driver.UnitChat == null) { Ok(false, "C0（前提）对局与语音条都在位"); return; }

        // 静音读口可能被设置窗拉了线（`ChatMutedSource` 优先）⇒ 本节改用**推式**那一格验，事后还原。
        var mutedSourceSave = driver.ChatMutedSource;
        driver.ChatMutedSource = null;
        try
        {
            driver.SetEnemyChatMuted(false);
            driver.ResetEnemyChatTipForTest();
            driver.UnitChat.HideAll();

            // 前 4 条：只计数、**不弹** SmallTip
            //   ⚠️ 不假定「每次 `AiEmoteForTest` 都说得出来」：台词是**随机**挑的（6 句均匀 + `default` 兜底），
            //     某一句在那位督军身上没有音频时会返回 false（那不算失败，是数据）。⇒ 按**成功次数**收口，
            //     并给一个尝试上限（凑不够就红，⛔ 不静默跳过）。
            int said = 0, tries = 0;
            for (; tries < 24 && said < 4; tries++)
            {
                if (!driver.AiEmoteForTest()) continue;
                said++;
                Eq(driver.EnemyChatsThisTurnForTest, said, $"C1 第 {said} 条（第 {tries + 1} 次尝试）⇒ 本回合计数 = {said}");
            }
            if (!Ok(said == 4, $"C1（前提）凑够 4 条敌方督军聊天（试了 {tries} 次；不够 ⇒ 下面那条前置不成立）")) return;
            Ok(!driver.EnemyChatTipShownForTest,
               "★★ C1 **前 4 条不弹** SmallTip（原版闸是 `count > 4`，不是 `>= 4`）"
             + "｜🧨 写成 `>= 4` ⇒ 本行 false（红）");

            // 第 5 条：弹一次 + 计数归零 + 闩打开
            bool said5 = false;
            for (; tries < 48 && !said5; tries++) said5 = driver.AiEmoteForTest();
            Ok(said5, "C2（前提）第 5 条也说出去");
            Eq(driver.EnemyChatsThisTurnForTest, 0,
               "★★ C2 第 5 条之后**计数归零**（原版 `*(+0x6c) = 0`，不是继续累加）");
            Ok(driver.EnemyChatTipShownForTest,
               "★★ C2 第 5 条 ⇒ **SmallTip 弹了、闩打开**（原版 `+0x68 = 1`，一辈子一次）"
             + "｜🧨 删掉 `NoteEnemyWarlordChat` 里那句 `ShowMuteEnemyChatTip()` ⇒ 本行 false（红）");
            Eq(driver.UnitChat.LastChatTerm, BattleDriver.MuteEnemyChatTerm,
               "★★ C2 那颗提示真的画上了：**第 3 颗 `RadioChat`（下标 2）上的文本 = 官方词条**"
             + "（`Battle/Tips/MuteEnemyChat`；原版 `ShowChatBox(panel, 2, …)`）"
             + "｜🧨 把载体改成 0/1 号气泡、或改成 `TutorialOverlay` 那套 SmallTip ⇒ 本行红");

            // 第 6/7 条：闩打开之后**不再计数**（原版 `+0x68` 打开后那一支整段进不去）
            int c6 = driver.EnemyChatsThisTurnForTest;
            driver.AiEmoteForTest();
            Eq(driver.EnemyChatsThisTurnForTest, c6,
               "★★ C2 闩打开之后**不再计数**（原版那一支以 `tipShowed == 0` 为前提）");

            // ---- C3 · 静音闸（原版 `+0x40`）：静音时**连计数都不进**、也不说话 ----
            driver.SetEnemyChatMuted(true);
            try
            {
                Ok(!driver.CanAiChatForTest, "★ C3 静音 ⇒ `CanAiChat` 关（原版 `CanChat(isLocal:0)` 要求 `!chatMuted`）");
                int cBefore = driver.EnemyChatsThisTurnForTest;
                bool saidMuted = driver.AiEmoteForTest();
                Ok(!saidMuted, "★★ C3 静音时**不说话**（气泡 + 语音一起没有）");
                Eq(driver.EnemyChatsThisTurnForTest, cBefore,
                   "★★ C3 静音时**计数器也不动**（原版 tip 分支以 `+0x40 == 0` 为前提）");
            }
            finally { driver.SetEnemyChatMuted(false); }

            // ---- C4 · 每回合清零（原版 `_NextTurn_d__395:181` → `ResetChatsReceived`）----
            //   ⚠️ 直接拨 `Ctx.Turn` 再 `RefreshAll()`：`RefreshAll` 是「新回合」之后**共同的**那一句
            //      （开局 / 结束回合 / 联机同步 / 重连都过它），清零判据 = **回合号变了**。
            //      拨完立刻还原，⛔ 不把假回合留给后面的节。
            int savedTurn = driver.Ctx.Turn;
            try
            {
                driver.ResetEnemyChatTipForTest();     // 闩关上，才能重新数
                for (int t = 0; t < 24 && driver.EnemyChatsThisTurnForTest < 1; t++) driver.AiEmoteForTest();
                int before = driver.EnemyChatsThisTurnForTest;
                Ok(before >= 1, $"C4（前提）本回合先攒了 {before} 条");
                driver.Ctx.Turn = savedTurn + 1;
                driver.RefreshAll();
                Eq(driver.EnemyChatsThisTurnForTest, 0,
                   "★★ C4 **回合号一变就清零**（原版 `ResetChatsReceived`，每回合从头数）"
                 + "｜🧨 删掉 `RefreshAll` 里那段 `Ctx.Turn != _chatTurnSeen` ⇒ 本行仍 ≥1（红）");
            }
            finally
            {
                driver.Ctx.Turn = savedTurn;
                driver.RefreshAll();
                driver.ResetEnemyChatTipForTest();
            }
        }
        finally
        {
            driver.ChatMutedSource = mutedSourceSave;      // 还原设置窗那条拉式接线
            driver.UnitChat.HideAll();
        }
    }

    // ==================================================================
    //  D · 跨局泄漏：两个「已排期」标志必须跟着 `Begin` 复位
    // ==================================================================
    //  🔴 **2026-10-24（执行代理 `E7` 顺手查出并修掉的缺陷）**：`_aiReplyPending` / `_aiEmotePending`
    //  是**本局的账**，而排进去的那条 cue 活在 `_timeline` 里（`Begin` 会 `_timeline.Clear()`）
    //  ⇒ 上一局**排了但没到点**的那一条被清走之后，标志**永远停在 true**
    //  ⇒ 之后每一局 `ScheduleAiReplyToPlayerChat` / `MaybeAiEmote` 第一句就 return
    //  ⇒ **AI 再也不回话、也不自发说话**（静默；只在同一台 driver 连着开好几局时才现形）。
    //  埋点变化点 = `BattleDriver.Begin` 的本局复位块（与 `_enemyChatsThisTurn` 同一处）。
    static void RunCrossGameReset(BattleDriver driver)
    {
        Debug.Log(P + "--- D · 跨局复位：排期标志不能留到下一局 ---");

        driver.ResetAiEmoteForTest();
        bool sched = driver.ScheduleAiEmoteForTest();
        Ok(sched && driver.AiEmotePendingForTest,
           "D0（前提）排上一条自发发言、**故意不推进时间线**（模拟「上一局没等到点」）");
        driver.ScheduleAiReplyForTest();
        Ok(driver.AiReplyPendingForTest, "D0（前提）回话也排上一条、同样不推进");

        driver.Begin("Ultramarines", "Goff", 20261025);      // 同一台 driver 再开一局

        Ok(!driver.AiEmotePendingForTest && !driver.AiReplyPendingForTest,
           "★★ D **新一局把两个「已排期」标志一起清掉**（队列里的 cue 已被 `_timeline.Clear()` 清走 ⇒ 标志必须同步清）"
         + "｜🧨 不在 `Begin` 的本局复位块里清 ⇒ 标志永远 true ⇒ **之后每一局 AI 都不再说话**（静默）");
        Ok(driver.Ctx != null && driver.Ctx.Turn >= 0, "D（收尾）driver 停在「普通单机局」（新开、未打）");
    }

    // ==================================================================
    //  夹具小工具（**本文件私有一份** —— `RuleEngineTest` 里那些是 private，跨文件调不到）
    // ==================================================================
    static CardDef Tactic(string name, int cost, string desc)
    {
        return new CardDef(name, name, "tactic", desc, "common", "Test", cost, 0, 0, 0, null);
    }

    static CardDef HeroOf(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>一副「手里就是 hand 这几张」的牌库（抽牌是 `pop_back` ⇒ 要倒着放）。</summary>
    static List<CardDef> DeckOf(string warlordFaction, CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("E7Warlord", warlordFaction) };
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static BattleContext NewProbeBattle(CardDef[] hand0, IList<CardDef> pool)
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves", hand0), DeckOf("Goff", new CardDef[0]),
                                  seed: 0, shuffle: false, cardPool: pool);
    }

    static int HandIndex(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++)
            if (hand[i] != null && hand[i].Card != null && hand[i].Card.Name == name) return i;
        return -1;
    }

    /// <summary>把一个单位**直写**到棋盘（绕过部署入口 —— 只为造「场上已经有一张同一张卡」这个前提）。</summary>
    static UnitState Plant(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = true };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>`ctx.Signals` 里 `EvtKind.Trigger` + 指定 keyword（可选卡名 / 实例号）的条数。</summary>
    static int CountTrigger(BattleContext ctx, string keyword, string cardId = null, int instanceId = int.MinValue)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.Trigger && e.Keyword == keyword
                && (cardId == null || e.CardId == cardId)
                && (instanceId == int.MinValue || e.InstanceId == instanceId)) n++;
        return n;
    }

    /// <summary>`ctx.Events`（引擎日志）里含 <paramref name="needle"/> 的行数。</summary>
    static int LogCount(BattleContext ctx, string needle)
    {
        int n = 0;
        foreach (var e in ctx.Events)
            if (!string.IsNullOrEmpty(e) && e.IndexOf(needle, System.StringComparison.Ordinal) >= 0) n++;
        return n;
    }
}
