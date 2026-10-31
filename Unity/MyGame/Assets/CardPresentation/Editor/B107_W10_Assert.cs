// B107_W10_Assert.cs — 执行代理 `B107_W10`（第一百零七会话 · 特效线 T21-8）的断言
//
// ============================ 它盯的是什么 ============================
// 施工单 → `资料/完全复刻/判据/B107_特效线施工单_v2.md:90`（T21-8）；判据 → `判据/B107_T21四族目标列表.md`。
//  `TriggerTargetSink`（「广播那一拍先发事件、结算那一拍才选出目标」的唯一 arm 口）本轮扩到**监听者**与 **410**：
//   ① **监听者**：`EmitListenerTrigger` 返回 `BattleEvent`，6 个有队列体的发射点把它交给队列体的
//      `ResolveWithTriggerTargetSink` ⇒ `ResolveTargets` 把「监听者正文解析出的第一个目标」写回同一个对象。
//      **判据 = 原版 `option 2` = `possibleTargets` 里那个目标**（`AbilityLogic__GetPos.c:191-219`）——
//      ⛔ **不是**事件 targetCard（`CardScript__ResolveUnitAttacked.c:28` 那个是**被打者**）。
//      销点卡 = `UM70 Invictor Warsuit`「When an enemy attacks, deal 2 damage to it」（`it` = 攻击者）。
//   ② **410**：`RunFollowAfterResolutionRow` 把 `EmitFollowUpResolution` 的返回值交给
//      `RuleCore.FireTriggerByNumberWithSink` 的队列体 arm。销点卡 = `ASH73 Whirling Death`
//      （`FollowUpResolution` 那一拍的 `sp/ep = 2/2`，`cardanim_clips.json`）—— 留空 ⇒ 演在施放者。
//   ③ **天赋**（`EmitTalentTrigger` / trigger 46）：**不 arm**（原版这一拍是 trait 框、无目标；
//      `animTimes` 里 `tn = Talent` 全池 0 行·`TriggerOpsByNumber(46)` 0 调用点）⇒ 事件 `Target*` 必须为空。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里，把现读 `:1372` 那句改成**同一物理行**追加（零行数变化）：
//       B106W57(); B107W10();
// 🔴 入口**故意不叫** `Run` / 不带 `Test` 前缀（理由同 `B106_W57_Assert`：`Step(...)` 会进 `_ranCases` 记账，
//    而 `工具/gen_diag_runner.py` 只认 8 空格缩进的顶层语句）。本件 = `RuleEngineTest` 的分部 ⇒ 复用
//    `ProbeBattle` / `Place` / `ToP1Turn` / `Unit` / `Check` / `CheckTrue` / `Section`（⛔ 别另起一套夹具）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A（监听者）：把 arm 那一句从 `FireBoardListenerDeferred` 里删掉（或 `EmitListenerTrigger` 返回 `null`）
//    ⇒ 事件 `TargetSlot` 留在 -1 ⇒ A 红。**把 `TargetSlot` 填成事件 `target`（被打者）** ⇒ A 红（那是 mateTgt 的格）。
//  · B（410）：把 `RunFollowAfterResolutionRow` 里的 `triggerEvent: fuEvt` 删掉（回到 7 参版）
//    ⇒ 事件 `TargetSlot` 留在 -1 ⇒ B 红。**把目标填成宿主（施放者）自己** ⇒ B 红。
//  · C（天赋）：给 `EmitTalentTrigger` / `EmitCardTrigger` 的 Talent 那一拍填任何 `Target*` ⇒ C 红；
//    真发了两条 Talent 事件 ⇒ C 的「恰一条」红。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一句 `B107W10();` 调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void B107W10()
    {
        Section("🆕 B107_W10 · T21-8：`TriggerTargetSink` 扩到【监听者】+【410】，【天赋】不 arm");

        B107W10Listener();
        B107W10FollowUp();
        B107W10Talent();
    }

    // ==================================================================
    //  ① 监听者 —— `UM70 Invictor Warsuit`「When an enemy attacks, deal 2 damage to it」
    // ==================================================================
    static void B107W10Listener()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0,
                  "A0 （前提）卡池载到（`CardDatabase.Load()` 非空）—— 拿不到 ⇒ 本段后面的断言全部无对象");
        if (pool == null || pool.Count == 0) return;

        var um70 = CardDatabase.FindById(pool, "UM70");
        CheckTrue(um70 != null, "A0 （前提）卡池里有 `UM70`");
        if (um70 == null) return;
        Check(um70.Name, "Invictor Warsuit", "A0 （前提）`UM70` 就是 `Invictor Warsuit`");

        // ---- 前提：正文解析出 `Attack` 监听器，且它真的写着「to it」（= 正文目标）----
        bool hasAttack = false;
        for (int i = 0; i < um70.WhenTriggers.Count; i++)
        {
            var ev = um70.WhenTriggers[i].Ev;
            if (ev != null && ev.Kind == WhenEventKind.Attack) hasAttack = true;
        }
        CheckTrue(hasAttack,
                  "A0 （前提）`UM70` 的正文解析出 `Attack` 监听器（`When an enemy attacks, …`）"
                + "｜🧨 `EffectText` 不再认这句 ⇒ 本段全空转（后面的绿都是假的）");
        if (!hasAttack) return;

        // ---- 夹具：监听者在 P0（槽 4）· 攻击者在 P1（槽 5）· 被打者在 P0（槽 6）----
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var me = Place(ctx, 0, 4, um70);
        var foeAtk = Place(ctx, 1, 5, Unit("W10FoeAtk", 1, 3, 8));
        var mateTgt = Place(ctx, 0, 6, Unit("W10MateTgt", 1, 2, 8));
        int foeSlot = SlotOf(ctx, 1, foeAtk.Name);
        int mateSlot = SlotOf(ctx, 0, mateTgt.Name);
        int hpBefore = foeAtk.Health;
        CheckTrue(foeSlot >= 0 && mateSlot >= 0 && SlotOf(ctx, 0, me.Name) >= 0,
                  "A1 （前提）三个夹具都读回了格号（≥0）—— ⛔ 别拿 -1 去调（会变成「参数非法」的假红）");

        ctx.ClearSignals();
        // 判据（原版 `BattleManagerSupport__BroadcastUnitAttacked.c:22/:29` 两跳）：subject/actor = 攻击者 · target = 被打者。
        RuleCore.BroadcastWhen(ctx, WhenEventKind.Attack, 1, foeAtk.Card,
                               subject: foeAtk, actor: foeAtk, target: mateTgt);

        // ---- 前提：监听者**真的结算了**（「deal 2 damage to it」落到攻击者身上）----
        Check(foeAtk.Health, hpBefore - 2,
              "A2 （前提）监听者真的结算了：攻击者 -2 血（正文的 `it` = 攻击者；判据 `ta = 80 actingCard`）"
            + "｜🧨 监听器没被唤醒 / 正文目标解析错 ⇒ 这里先红（别把后面的绿当成「arm 对了」）");
        if (foeAtk.Health != hpBefore - 2) return;

        BattleEvent hit = null;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind == EvtKind.Trigger && e.CardId == um70.Name
                && e.Keyword != null && e.Keyword.StartsWith("UnitAttack", System.StringComparison.Ordinal))
            { hit = e; break; }
        }
        CheckTrue(hit != null,
                  "A3 （前提）真发出了一条 `EvtKind.Trigger`（`UM70` · `Keyword ≈ UnitAttack`）");
        if (hit == null) return;

        Check(hit.TargetPlayer, 1,
              "A4 ★★ 监听事件的 `TargetPlayer` = **攻击者那一侧**（P1）"
            + "｜判据 = 原版 `option 2` = `possibleTargets` 里那个目标（`AbilityLogic__GetPos.c:191-219`）");
        Check(hit.TargetSlot, foeSlot,
              "A5 ★★★ 监听事件的 `TargetSlot` = **攻击者格**（`UM70` 的 `BulletImpact_2shot_autocannon` = `sp/ep/pt 0/2/2`）"
            + "｜🧨 删掉 `FireBoardListenerDeferred` 的 arm / `EmitListenerTrigger` 返回 `null` ⇒ 留在 -1 ⇒ 红");
        Check(hit.TargetCardId, foeAtk.Name,
              "A6 ★★ `TargetCardId` = 攻击者卡名");
        CheckTrue(hit.TargetSlot != mateSlot,
              "A7 ★★★ 灭自证：目标**不是**事件 `targetCard`（被打者 = `" + mateTgt.Name + "` @槽 " + mateSlot + "）"
            + "｜🧨 拿 `ctx.EventTarget` / 事件 `target` 去填 ⇒ 实得 " + mateSlot + " ⇒ 红（`DA22` 那类会治错人）");
    }

    // ==================================================================
    //  ② 410 —— `ASH73 Whirling Death` 的 `FollowUpResolution` 那一拍（`sp/ep = 2/2`）
    // ==================================================================
    static void B107W10FollowUp()
    {
        // ---- 夹具：按 `ASH73` 的身份/卡面造一张**独立**的卡（⛔ 不改共享卡池对象 —— `CardDatabase.Load()` 是浅拷贝，
        //      改到的是缓存里同一个 `CardDef` ⇒ 会漏进别的断言）。`abilityTriggers` 给 `{0, 410}`：
        //      `HasTriggerSource` 的 presence 闸读它（= 原版真有这条 ability，判据 `B107_T21四族目标列表.md` §四①）。
        var ash73 = new CardDef("ASH73", "Whirling Death", "tactic",
                                "Deal 5 damage to a troop. If the target dies, deal 2 damage to adjacent units",
                                "common", "Aeldari", 3, 0, 0, 0, null,
                                abilityTriggers: new[] { 0, 410 }, abilityTriggersVersioned: true);
        var ops = EffectText.Parse("Deal 5 damage to a troop", out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "B0 （前提）夹具的 410 正文 `Deal 5 damage to a troop` 解得出 ≥1 条 op");
        if (ops == null || ops.Count == 0) return;
        // ⚠️ 数据半的 `TriggerOpsByNumber(410)` 命中率**只能在运行期读**（判据 §四③）⇒ 夹具显式挂表，
        //    让本段的期望值是**确定的**（= 「a troop」= 点定的那个 troop）。
        ash73.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 410, ops } });
        CheckTrue(ash73.HasTriggerOpsByNumber, "B0 （前提）夹具的 `triggerOpsByNumber` 表已挂上");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var host = Place(ctx, 0, 3, ash73);                          // 施放者（能力宿主）
        var chosen = Place(ctx, 1, 5, Unit("W10ChosenTroop", 1, 2, 9));   // 被指定的那个 troop
        int hostSlot = SlotOf(ctx, 0, host.Name);
        int chosenSlot = SlotOf(ctx, 1, chosen.Name);
        int hpBefore = chosen.Health;
        CheckTrue(hostSlot >= 0 && chosenSlot >= 0, "B1 （前提）宿主与目标都读回了格号（≥0）");

        ctx.ClearSignals();
        int rc = RuleCore.RunFollowAfterResolutionRowForTest(ctx, 0, host, chosen);
        RuleCore.PumpActionQueues(ctx);

        Check(rc, 1,
              "B2 （前提）410 正文**排进了行动队列**（返 1）"
            + "｜🧨 `TriggerOpsByNumber(410)` 拿不到 ⇒ 0 ⇒ 后面无对象");
        Check(chosen.Health, hpBefore - 5,
              "B2' （前提）410 正文**真的跑了**（点定的 troop -5 血）"
            + "｜🧨 队列体没跑 / 目标解析错 ⇒ 先红（别把后面的绿当成「arm 对了」）");

        BattleEvent fu = null;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind == EvtKind.Trigger && e.CardId == ash73.Name
                && e.Keyword != null && e.Keyword.StartsWith("FollowUpResolution", System.StringComparison.Ordinal))
            { fu = e; break; }
        }
        CheckTrue(fu != null, "B3 （前提）真发出了一条 `EvtKind.Trigger`（`ASH73` · `Keyword ≈ FollowUpResolution`）");
        if (fu == null) return;

        Check(fu.TargetPlayer, 1,
              "B4 ★★ `FollowUpResolution` 事件的 `TargetPlayer` = 目标那一侧（P1）");
        Check(fu.TargetSlot, chosenSlot,
              "B5 ★★★ `TargetSlot` = **点定的那个 troop 格**（`ASH73` 的 `Slash Repeating 2x` = `sp/ep 2/2`）"
            + "｜🧨 `RunFollowAfterResolutionRow` 不传 `triggerEvent:`（回到 7 参版）⇒ 留在 -1 ⇒ 红");
        Check(fu.TargetCardId, chosen.Name,
              "B6 ★★ `TargetCardId` = 点定的那个 troop 的卡名");
        CheckTrue(fu.TargetSlot != hostSlot,
              "B7 ★★★ 灭自证：目标**不是施放者自己**（宿主 @槽 " + hostSlot + "）"
            + "｜🧨 把目标填成 `u`（宿主）/ 留空让表现层退回 `self` ⇒ 这一条红 —— 那正是 `ASH73` 的 5 点伤害件会演错人的形态");
    }

    // ==================================================================
    //  ③ 天赋（trigger 46）—— **不 arm**：`Target*` 为空，且没有可播的 `animTimes` 件
    // ==================================================================
    static void B107W10Talent()
    {
        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0) return;   // A0 已经报过前提，这里不重复

        CardDef talent = null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].AbilityTriggerPresence("Talent") == 1) { talent = pool[i]; break; }
        CheckTrue(talent != null,
                  "C0 （前提）卡池里有带 `Talent`(46) ability 的卡（判据 §二：全池 96 张都有）");
        if (talent == null) return;

        // 判据 §二①：`animTimes` 里 `tn = Talent` 的行 = **0**（天赋那一拍走 trait 框 `TraitFrames`，不是 cardAnim）。
        int talentRows = 0;
        for (int i = 0; i < talent.AnimTimes.Count; i++)
        {
            var row = talent.AnimTimes[i];
            if (row != null && row.TriggerName == "Talent") talentRows++;
        }
        Check(talentRows, 0,
              "C1 ★★ 「不多播件」：`" + talent.Name + "` 的 `animTimes` 里 `Talent` 行 = **0**（天赋 = trait 框，不是 cardAnim 件）"
            + "｜🧨 生成器把 46 那批动画挂进 `animTimes` / 有人给 Talent 加一条动画行 ⇒ 红");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var u = Place(ctx, 0, 4, talent);
        int uSlot = SlotOf(ctx, 0, u.Name);
        CheckTrue(uSlot >= 0, "C2 （前提）天赋单位占到了格子");

        ctx.ClearSignals();
        // `EmitTalentTrigger` 是 `internal`（编辑器程序集调不到）⇒ 走它的公共等价口：
        //   同一句 `EmitCardTrigger(ctx, card, owner, unit, "Talent", "talent:turnStart")`（本方法体只此一句 + 日志）。
        var te = RuleCore.EmitCardTriggerForTest(ctx, talent, 0, u, "Talent", "talent:turnStart", 0);
        CheckTrue(te != null, "C3 （前提）`Talent` 这一拍真发了 `EvtKind.Trigger`（闸 = `hasTriggerSource`）");
        if (te == null) return;

        Check(te.TargetPlayer, -1,
              "C4 ★★★ 天赋事件**不带目标**：`TargetPlayer` = -1（原版这一拍 = trait 框、锚在这张卡自己身上）"
            + "｜🧨 给 `EmitTalentTrigger` / 它的队列体填任何目标 ⇒ 红");
        Check(te.TargetSlot, -1, "C5 ★★ 天赋事件 `TargetSlot` = -1（不 arm ⇒ 没有 `abilityTarget` 落点）");
        CheckTrue(te.TargetCardId == null, "C6 ★★ 天赋事件 `TargetCardId` = null（无目标）");
        CheckTrue(ctx.TriggerTargetSink == null,
              "C7 ★★ 天赋这一跳**没有 arm** `ctx.TriggerTargetSink`（收尾后必须还原成 null）");
        Check(te.Slot, uSlot,
              "C8 ★ 天赋事件锚在**这张卡自己**那一格（`option 0`；锚点是 trait 框的宿主）");
        Check(ctx.Signals.Count, 1,
              "C9 ★ 「恰一条」：整段只发 1 条事件（多一条 = 多播了原版没有的东西）");
    }
}
