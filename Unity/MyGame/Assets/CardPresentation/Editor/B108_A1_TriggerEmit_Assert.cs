// B108_A1_TriggerEmit_Assert.cs — 第一百零八会话 · 执行代理 `B108_A1` 的断言
//
// ============================ 它盯的是什么 ============================
// 6 个「引擎无发射口」的 trigger 真正接通（`EvtKind.Trigger` 在这些拍子上真的发出来）：
//   81 `Unstable` · 700 `OnSecretCreated` · 695 `OnCollectQuestPoints` ·
//   647 `UnitLoseStealth` · 291 `UnitStunned` · 701 `OnSabotageCreated`
//
// 每个 trigger 的生产链（文件:行 → 本文件断言的就是这条链的**末端**）：
//   · `Unstable`(81)：`RuleCore.CleanupDeaths` 的 81 那一跳入队 →
//     队列体 `EmitUnstableDeathTrigger` → `EmitCardTrigger(..., "Unstable", explicitSlot: 死者格位)`
//     → `EvtKind.Trigger`（Keyword = **PascalCase `Unstable`**）。
//     判据 = `out_cpp2il_dev_ilspy/RawCardScript.cs:1954-1963` 的 `thisCard == deadCard` 支：
//     `OnTrigger(ThisUnitDeath)` 紧接 `OnTrigger(Unstable)`（`:1962`）。
//   · 700 / 695 / 701：事件源 `BroadcastCreatedCardWithWarlordPreCall`（Secret/Sabotage）与
//     `BroadcastWhen(GainQuest)`（任务点）→ 监听者 `FireBoardWhenListener` → `EmitListenerTrigger`
//     → `TriggerNameOfWhenKind` 的 `case WhenEventKind.CreatesSecret / GainQuest / CreatesSabotage`
//     → `EmitCardTrigger` → `EvtKind.Trigger`（PascalCase 原版枚举名）。
//     判据 = `CardScript__ReactToSecretCreated.c:7`（`OnTrigger(700)`）·
//     `CardScript__ReactToCollectQuestPoints.c:7`（`0x2b7 = 695`）·
//     `CardScript__ReactToSabotageCreated.c:7`（`0x2bd = 701`）。
//   · 647：事件源 `BroadcastKeywordEvent(LosesStealth)` → 监听者 → `TriggerNameOfWhenKind` 之后兜底
//     `WhenEvents.ExtraTriggerNameForKind` 的 `case LosesStealth` → `EmitCardTrigger`。
//     判据 = `CardScript__TriggerLoseStealth.c:5-9`（`OnTrigger(0x287 = 647)`）。
//   · 291：事件源 `BroadcastKeywordEvent(GetsStun)` → 监听者 → `TriggerNameOfWhenKind` 的
//     `case WhenEventKind.GetsStun` → `EmitCardTrigger`。
//     判据 = `CardScript__ReactToUnitStunned.c:7`（`OnTrigger(0x123 = 291)`）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**（建议紧接 `{ CheckB97ERAbilityGates(); }` 之后）：
//       { CheckB108A1TriggerEmits(); }
//   🔴 故意不用 `Step(...)`、入口也不带 `Test` 前缀（理由同 `B97_ER_Assert` / `B106_W57_Assert`：
//     `Step(...)` 会进 `_ranCases` 记账，而 `工具/gen_diag_runner.py` 只认 8 空格缩进的顶层语句）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · `Unstable`：把 `CleanupDeaths` 里 `() => { EmitUnstableDeathTrigger(...); ... }` 的
//     上半句删掉 / 把 `EmitCardTrigger` 的 `explicitSlot` 去掉 ⇒ 事件 0 条或落点 −1 ⇒ 红。
//   · 五个监听者族：删掉 `TriggerNameOfWhenKind` 对应的 `case`（或 `ExtraTriggerNameForKind` 的
//     `LosesStealth` 支）⇒ `tn == null` ⇒ `EmitListenerTrigger` 直接 return ⇒ 0 条 ⇒ 红。
//     **把这条路堵成形状**：同一套夹具 + 另一张**没有这条 ability** 的监听者（presence = 0）⇒ 仍然 0 条。
//   · 白名单：把这 6 个名字加回 `NoEmitTriggerNames`（或在 `IsRoutedTriggerName` 删掉 `case`）
//     ⇒ `SelfCheckRoutedTriggerNames()` 非空 ⇒ 红；两张表不同长 / 错序 ⇒ 逐名那两条红。
//   · 件级（末段）：生成器没重跑 ⇒ 那批行还在 `trigger_unmapped` ⇒ `SelfCheckRoutedTriggers()` 报漏播 ⇒ 红。
using System;
using System.Collections.Generic;
using System.Reflection;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一个 `{ … }` 块调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void CheckB108A1TriggerEmits()
    {
        Section("🆕 B108_A1：6 个 trigger 的 `EvtKind.Trigger` 发射口（81 / 700 / 695 / 647 / 291 / 701）");
        B108A1Unstable();
        B108A1ListenerBeat("OnCollectQuestPoints", 695,
                           "When you gain [honour], deal 2 damage to the enemy warlord",
                           WhenEventKind.GainQuest, 0, true, WhenEventKind.GainFaith);
        B108A1ListenerBeat("OnSecretCreated", 700,
                           "When you create a Secret, deal 2 damage to the enemy warlord",
                           WhenEventKind.CreatesSecret, 0, true, WhenEventKind.GainFaith);
        B108A1ListenerBeat("OnSabotageCreated", 701,
                           "When you create a Sabotage, deal 2 damage to the enemy warlord",
                           WhenEventKind.CreatesSabotage, 0, true, WhenEventKind.GainFaith,
                           subjectSide: 1);   // 🔴 2026-11-04（`B139_V5`）：`who` = 施放方(0)，`subject` = **收牌方**(1) —— 原版 `GetHero(造出来那张的 isPlayer)`，判据见 `BroadcastCreatedCardWithWarlordPreCall` 的 doc
        B108A1ListenerBeat("UnitLoseStealth", 647,
                           "When a friendly unit loses Stealth, deal 2 damage to the enemy warlord",
                           WhenEventKind.LosesStealth, 0, false, WhenEventKind.GainFaith);
        B108A1ListenerBeat("UnitStunned", 291,
                           "When an enemy receives a Stun, deal 2 damage to it",
                           WhenEventKind.GetsStun, 1, false, WhenEventKind.GainFaith);
        B108A1WhitelistTables();
        CheckB139V5SabotageSubject();   // 🆕 2026-11-04（`B139_V5`）：701 的 subject = 收牌方督军（真生产链 + 灭自证），断言全文在 `RuleEngine/Editor/B139_V5_Assert.cs`
    }

    // ==================================================================
    //  ① `Unstable`(81)：死者自己那一拍
    // ==================================================================

    /// <summary>带 `Unstable` 关键词 **且** 有 81 那条 ability 的单位（真卡 `GOF3` 的形状）。</summary>
    static CardDef B108A1Bomb(bool withAbility)
    {
        return new CardDef("B108A1Bomb" + (withAbility ? "A" : "N"), "B108A1Bomb" + (withAbility ? "A" : "N"),
                           "unit", "", "common", "Test", 1, 1, 1, 0,
                           new[] { "Unstable" }, subtype: "Infantry",
                           abilityTriggers: withAbility ? new[] { 81 } : new int[0],
                           abilityTriggersVersioned: true);   // 两档都 Versioned ⇒ presence = **1 / 0**（而不是 `-1` 未知）
    }

    /// <summary>找第一条 `EvtKind.Trigger` 且 keyword == 名字的事件（`null` = 没有）。</summary>
    static BattleEvent B108A1FirstTrigger(BattleContext ctx, string keyword)
    {
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.Trigger && e.Keyword == keyword) return e;
        return null;
    }

    static void B108A1Unstable()
    {
        // ---- 正例：带 `Unstable` + ability 81 的单位被打死 ⇒ 恰好一条 Trigger(Unstable) ----
        {
            var bomb = B108A1Bomb(true);
            CheckTrue(bomb.Has("unstable"), "★ Unstable ①（前提）合成炸弹认得出 `Unstable` 关键词");
            CheckTrue(bomb.AbilityTriggerPresence("Unstable") == 1,
                      "★ Unstable ①（前提）它的 `abilityTriggers` 里真有 81（`HasTriggerSource` 的第一道闸）");

            var kill = Tactic("T_B108A1Kill", 0, "Deal 99 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { bomb, kill }, new[] { Unit("B108A1Foe", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, bomb, exhausted: true);
            Place(ctx, 1, 3, Unit("B108A1Bystander", 1, 0, 30), exhausted: true);
            ctx.ClearSignals();

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_B108A1Kill"), 3), RuleCodes.OK,
                      "Unstable ① 把带 `Unstable` 的单位打死");
            CheckTrue(SlotOf(ctx, 0, "B108A1BombA") < 0, "Unstable ①（前提）它真的死了" + LogTail(ctx));

            int n = E6CountTriggerEvents(ctx, "Unstable");
            Check(n, 1,
                  "★★ Unstable：死者自己那一拍发**恰好一条** `EvtKind.Trigger(keyword = \"Unstable\")`"
                + "｜🧨 把 `CleanupDeaths` 81 队列体里那半句 `EmitUnstableDeathTrigger` 删掉 ⇒ 0 条 ⇒ 红"
                + "｜🧨 名字写成小写 `unstable`（走 `FireTriggerAt`）⇒ 本条按 PascalCase 比 ⇒ 红");
            var ev = B108A1FirstTrigger(ctx, "Unstable");
            CheckTrue(ev != null && ev.Slot == 3,
                      "★★ Unstable：事件的落点 = 死者**死前**的格号 3（`explicitSlot` 生效）"
                    + "｜🧨 去掉 `EmitCardTrigger` 的 `explicitSlot` ⇒ `FindSlot` 在 `BoardSlots.RemoveAt` 之后回 false ⇒ slot = −1 ⇒ 红");
            CheckTrue(ev != null && ev.CardId == "B108A1BombA",
                      "★ Unstable：`CardId` = 死者那张卡（原版 `OnTrigger(Unstable)` 的 `thisCard` 就是它）");
        }

        // ---- 反向 ①：带关键词但**没有** 81 那条 ability ⇒ 不发（与原版 `TriggerAbilities(81)` 找不到 ability 同形）----
        {
            var plain = B108A1Bomb(false);
            var kill = Tactic("T_B108A1Kill2", 0, "Deal 99 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { plain, kill }, new[] { Unit("B108A1Foe2", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, plain, exhausted: true);
            Place(ctx, 1, 3, Unit("B108A1Bystander2", 1, 0, 30), exhausted: true);
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_B108A1Kill2"), 3), RuleCodes.OK,
                      "Unstable ② 打死一个「有关键词、没有 81 ability」的单位");
            Check(E6CountTriggerEvents(ctx, "Unstable"), 0,
                  "★★ Unstable（反向）：**没有 81 这条 ability** ⇒ 一条 `EvtKind.Trigger(Unstable)` 都不发"
                + "｜🧨 把 `EmitCardTrigger` 换成手搓 `ctx.Emit`（绕过 `HasTriggerSource`）⇒ 这里会发 ⇒ 红");
        }

        // ---- 反向 ②：不带 `Unstable` 的单位死 ⇒ 不发 ----
        {
            var kill = Tactic("T_B108A1Kill3", 0, "Deal 99 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { Unit("B108A1Plain", 1, 0, 1), kill },
                                  new[] { Unit("B108A1Foe3", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, Unit("B108A1Plain", 1, 0, 1), exhausted: true);
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_B108A1Kill3"), 3), RuleCodes.OK,
                      "Unstable ③ 打死一个**不带** `Unstable` 的单位");
            Check(E6CountTriggerEvents(ctx, "Unstable"), 0,
                  "★★ Unstable（反向）：**别的拍子（普通死亡）不发 `Unstable`**"
                + "｜🧨 把自爆发射口挂到「所有死亡」上（不判 `u.Has(KeywordTable.Unstable)`）⇒ 红");
        }
    }

    // ==================================================================
    //  ② 五个监听者族：广播那一拍 ⇒ 监听者结算 ⇒ 一条按它自己 trigger 名的 Trigger
    // ==================================================================

    /// <summary>
    /// 正例 = 合成监听者（有 ability N）在场上，广播一次 <paramref name="kind"/> ⇒ 恰好一条
    /// `EvtKind.Trigger(keyword = triggerName)`。两条反向：
    ///   ① 广播**另一个** kind ⇒ 0 条（拍子不对不发）；
    ///   ② 同样的正文但**没有那条 ability**（presence = 0）⇒ 0 条（`HasTriggerSource` 第一道闸）。
    /// </summary>
    static void B108A1ListenerBeat(string triggerName, int triggerNumber, string listenerText,
                                   string kind, int eventWho, bool boardAndPersistentOnly,
                                   string otherKind, int subjectSide = -1)
    {
        // 🔴 2026-11-04（`B139_V5`）：**`subject` 放在哪一侧**与「`who` 是谁」是**两件事**，⛔ 不许再合成一个
        //   `eventWho`。原版 `OnSabotageCreated`(701) 的 `targetCard` = **收牌方**督军，而 `who`（`When you …`
        //   的归属）仍是**施放方** ⇒ 只有 701 那一族把 `subjectSide` 传成 `1`（施放方 = 0 时的敌方）。
        //   判据：`_ResolveCreateHandCard_d__512__MoveNext.c:95-98` 的 `GetHero(造出来那张的 isPlayer)`。
        //   `-1`（缺省）= 老规则（`subject` 跟 `eventWho` 同侧）—— 其余五族逐字不变。
        int subjectOn = subjectSide >= 0 ? subjectSide : (eventWho == 0 ? 0 : 1);
        // ---- 正例 ----
        {
            var listener = E6Listener("B108A1L" + triggerNumber, listenerText, triggerNumber, E6Criteria(1, 0));
            CheckTrue(HasWhenKind(listener, kind),
                      $"★ {triggerName} ①（前提）合成监听者解析出了 `{kind}` 监听器"
                    + "｜🧨 卡面文法变了 ⇒ 红（后面的断言就没有对象）");
            Check(RuleCore.TriggerNameOfWhenKindForTest(kind), triggerName,
                  $"★★ {triggerName} ① 映射：`TriggerNameOfWhenKind(\"{kind}\")` = `{triggerName}`"
                + "｜🧨 删掉那个 `case` / `ExtraTriggerNameForKind` ⇒ `EmitListenerTrigger` 直接 return ⇒ 红");

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, listener);
            var subj = Place(ctx, subjectOn, 1,
                             Unit("B108A1Sub" + triggerNumber, 1, 2, 9));
            ctx.ClearSignals();
            RuleCore.BroadcastWhen(ctx, kind, eventWho, subj.Card, subject: subj,
                                   boardAndPersistentOnly: boardAndPersistentOnly);
            int n = E6CountTriggerEvents(ctx, triggerName);
            Check(n, 1,
                  $"★★ {triggerName}：监听者结算 ⇒ **恰好一条** `EvtKind.Trigger(keyword = \"{triggerName}\")`"
                + "｜🧨 删掉映射 / 生成器没重跑（行还在 `trigger_unmapped`）⇒ 0 条 ⇒ 红");
            var ev = B108A1FirstTrigger(ctx, triggerName);
            CheckTrue(ev != null && ev.CardId == "B108A1L" + triggerNumber,
                      $"★ {triggerName}：`CardId` = **监听者**那张卡（原版 `OnTrigger` 的 `thisCard`）");
        }

        // ---- 反向 ①：广播另一个 kind ⇒ 0 条 ----
        {
            var listener = E6Listener("B108A1X" + triggerNumber, listenerText, triggerNumber, E6Criteria(1, 0));
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, listener);
            var subj = Place(ctx, subjectOn, 1,
                             Unit("B108A1SubX" + triggerNumber, 1, 2, 9));
            ctx.ClearSignals();
            RuleCore.BroadcastWhen(ctx, otherKind, eventWho, subj.Card, subject: subj);
            Check(E6CountTriggerEvents(ctx, triggerName), 0,
                  $"★★ {triggerName}（反向）：广播**另一个拍子**（`{otherKind}`）⇒ 0 条 `{triggerName}`"
                + "｜🧨 把映射写宽（任何 kind 都回这个名字）/ 发射口挂错拍子 ⇒ 红");
        }

        // ---- 反向 ②：同样的正文、**没有那条 ability** ⇒ 0 条 ----
        {
            var bare = new CardDef("B108A1B" + triggerNumber, "B108A1B" + triggerNumber, "unit",
                                   listenerText, "common", "Test", 1, 1, 5, 0, null,
                                   subtype: "Infantry",
                                   // 🔴 `abilityTriggers` 给**空数组** + `Versioned = true` ⇒ presence = **0**（明确没有），
                                   //    ⛔ 不写这两格 = `Versioned = false` ⇒ presence = **-1（未知）** ⇒ 下面那条前提断言会红。
                                   abilityTriggers: new int[0], abilityTriggersVersioned: true);
            CheckTrue(HasWhenKind(bare, kind),
                      $"★ {triggerName} ③（前提）裸卡（无 ability 列）也解析出 `{kind}` 监听器");
            Check(bare.AbilityTriggerPresence(triggerName), 0,
                  $"★ {triggerName} ③（前提）它的 `abilityTriggers` 不含 {triggerNumber}（presence = 0）");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, bare);
            var subj = Place(ctx, subjectOn, 1,
                             Unit("B108A1SubB" + triggerNumber, 1, 2, 9));
            ctx.ClearSignals();
            RuleCore.BroadcastWhen(ctx, kind, eventWho, subj.Card, subject: subj,
                                   boardAndPersistentOnly: boardAndPersistentOnly);
            Check(E6CountTriggerEvents(ctx, triggerName), 0,
                  $"★★ {triggerName}（反向）：监听者**没有这条 ability** ⇒ 不发 `EvtKind.Trigger`"
                + "｜🧨 把 `HasTriggerSource` 的 presence 第一道闸拆掉（只按动画行判）⇒ 红"
                + "（判据 `CardAbility__CanTriggerAbility.c:46-48`：ability 不在 ⇒ 连动画都不播）");
        }
    }

    // ==================================================================
    //  ③ 白名单三表联动（`NoEmit` / `IsRouted` / `MustRoute` × 名字与 keyword 两张）
    // ==================================================================

    static void B108A1WhitelistTables()
    {
        const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Static;
        var t = typeof(CardPresentation.VfxMap);
        var names = (string[])t.GetField("MustRouteTriggerNames", F)?.GetValue(null);
        var kws = (string[])t.GetField("MustRouteTriggerKeywords", F)?.GetValue(null);
        var noEmit = (string[])t.GetField("NoEmitTriggerNames", F)?.GetValue(null);
        CheckTrue(names != null && kws != null && noEmit != null,
                  "★ 白名单（前提）三张私有静态表都反射取得到"
                + "｜🧨 改名 ⇒ 取不到 ⇒ 红（⛔ 不静默放行）");
        if (names == null || kws == null || noEmit == null) return;

        Check(names.Length, kws.Length,
              "★★ 白名单：`MustRouteTriggerNames` 与 `MustRouteTriggerKeywords` **同长**（逐个对应）"
            + "｜🧨 只往一张表加 ⇒ 这里红（`SelfCheckRoutedTriggers` 也有一条同义闸）");

        string[] six =
        {
            "Unstable", "OnSecretCreated", "OnCollectQuestPoints",
            "UnitLoseStealth", "UnitStunned", "OnSabotageCreated",
        };
        for (int i = 0; i < six.Length; i++)
        {
            string n = six[i];
            int at = Array.IndexOf(names, n);
            CheckTrue(at >= 0,
                      $"★★ 白名单：`{n}` 在 `MustRouteTriggerNames` 里"
                    + "｜🧨 漏加 ⇒ `TryResolveTriggerPicks` 的 `target` 恒 null ⇒ 掉回 `trigger` 整桶播（错件）");
            CheckTrue(at >= 0 && at < kws.Length && kws[at] == n,
                      $"★★ 白名单：`{n}` 同一序号的 keyword **逐字相同**（= 引擎发的 PascalCase 原版枚举名）"
                    + "｜🧨 写成小写 / 两张表错序 ⇒ 件级自检拿错形态 ⇒ 红");
            CheckTrue(Array.IndexOf(noEmit, n) < 0,
                      $"★★ 白名单：`{n}` **不在** `NoEmitTriggerNames`（= 已核过发射口）"
                    + "｜🧨 加回去 ⇒ `SelfCheckRoutedTriggerNames` 反向闸红");
        }

        var bad = CardPresentation.VfxMap.SelfCheckRoutedTriggerNames();
        Check(bad.Length, 0,
              "★★ 白名单名字级自检（`SelfCheckRoutedTriggerNames`）**0 违规** —— "
            + "含「`NoEmitTriggerNames` 的不许进白名单」「`MustRouteTriggerNames` 的必须在（三态大小写都命中）」");
        for (int i = 0; i < bad.Length; i++) CheckTrue(false, "  白名单违规[" + i + "]: " + bad[i]);

        // ---- 件级：生成器重跑之后，这 6 个名字的动画行必须真取得到 ----
        // ⚠️ 本段**依赖数据侧重跑**：`工具/gen_card_vfx_by_card.py` 从 `VfxMap.cs` 读白名单，
        //    把这批行从 `trigger_unmapped` 挪进 `trigger` 桶。没重跑 ⇒ 这里报「漏播」是**预期**。
        CardPresentation.VfxMap.EnsureLoaded();
        var trigBad = CardPresentation.VfxMap.SelfCheckRoutedTriggers();
        Check(trigBad.Length, 0,
              "★★ 白名单件级自检（`SelfCheckRoutedTriggers`）**0 违规**（逐卡：取到的件 = 这张卡自己那批，不多不少）"
            + "｜🧨 **生成器没重跑** ⇒ 这批行还在 `trigger_unmapped` ⇒ 逐条报「漏播」⇒ 红"
            + "（重跑：`python -I -X utf8 d:/4/Unity/工具/gen_card_vfx_by_card.py`，输入 `VfxMap.cs` 白名单 + `cards_engine.json`，"
            + "输出 `MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` 与 `数据/游戏数据/card_vfx_by_card.json`）");
        for (int i = 0; i < trigBad.Length; i++) CheckTrue(false, "  件级违规[" + i + "]: " + trigBad[i]);
    }
}
