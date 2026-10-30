// B100F11_Assert.cs — 第一百会话 · 执行代理 `B100_F11`（两条实现真缺陷的钉子）
//
// ============================ 它盯的是什么 ============================
//  诊断档 = `资料/完全复刻/报告/B100_X3.md` 的 **#3 / #4**（同族）与 **#12 / #13 / #14**（三处同一根因）。
//
//  ① **`EvtKind.DiscardReveal` / `EvtKind.StealReveal` 没登记时长**（X3 #3 / #4）
//     · 枚举成员在 `RuleEngine/Core/BattleEvent.cs:432` / `:446`（`aa5a5dee` 2026-10-29 落地），
//       而 `CardPresentation/Core/EventTiming.cs` 的 `ZeroDurationKinds` / `TimedKinds` / `DurationOf` /
//       `SourceOf` **四处都没有这两档** ⇒ `DurationOf` 落 `default: 0`、`IsSourced` 回 false
//       ⇒ `BattleScene` 的「恰好 2 档没出处」变 4、`A2·J2③` 报「未登记 2 档」。
//     · 判据（两档**各有**时长，⛔ 别统一）：`CardPresentation/Core/CardFeel.cs:3544/3549`
//       `DiscardRevealStepTime = 0.2f` / `StealRevealStepTime = 0.4f` —— 它们自己带出处
//       （`.rdata 0x1834b2bb0` = 0.2f · `.rdata 0x1834b3158` = 0.4f，`工具/read_literal.py` 实读；
//        调用点 `BattleManager._ResolveDiscardFromHand_d__544__MoveNext.c:95` /
//        `BattleManager._ResolveStealFromDeck_d__547__MoveNext.c:243`，两处都是
//        `CardScript.ShowCardBeingDrawn(card, <那个常量>)`）。
//     · 修法 = 进 `TimedKinds` + `DurationOf` / `SourceOf` 各一个 `case`（**三处一起改**，见 `EventTiming` 那段 doc）。
//       ⚠️ 那两个常量是 `internal`，本文件在**编辑器程序集**里取不到 ⇒ 这里比的是**原版字面量 0.2 / 0.4**
//       （那才是尺子；拿我们的常量去比我们自己的实现 = 自证）。
//
//  ② 🔴 **2026-10-30 作废（铁律 5 · `B101_F1`）—— 下面这一整段 ② 的判据是错的，`SectionB` 已删**：
//     真判据 = `报告/B101_R4_演出与预览判据.md` §6·8 —— 原版 `Pray Enchantment` 的 trigger-60 ability
//     的 `cardAnimations` 里**就带 `Pray_Exit`**，走 `AbilityLogic.cardAnimations` → `PlayAnim` →
//     `AddCardAnim(actingCard = 载体)` ⇒ 原版**会播**（演在载体身上）。`B100_F11` 当年为了让自己那条
//     计数断言过，把原版真实行为从数据里删了 ⇒ 已由 `B101_F1` 撤销；`SectionA`（两档时长）**仍有效**。
//     （以下原文留痕）② **`UnitDamaged` 桶收进了附魔卡**（X3 #12 / #13 / #14，三处同一根因）
//     · 判据**已经写死在 C# 里**（`CardPresentation/Editor/BattleScene.cs:13423-13430` +
//       `CardPresentation/Core/VfxMap.cs:3521`，两处逐字同口径）：
//       「**附魔不是单位** —— 它的 trigger 60 是附魔能力（如 `Pray_Exit`），不算单位技能动画」
//       ⇒ 数 `UnitDamaged` 行时 `cd.Type == "enchantment"` 一律跳过。
//     · 缺的是**数据侧**：生成器 `工具/gen_card_vfx_by_card.py` 没做卡型过滤 ⇒ 逐卡表里
//       `9218576fd3e26554db5b4c239a36dbc7`（`Pray Enchantment`，`type = enchantment`，`cardType = 20`）
//       的 `hit` 桶还留着 `Pray_Exit`（`cardAbilities[0]` 的 `UnitDamaged(60)`，`chosenAbility = removeThisEnchantment`）。
//       而 `VfxMap.LoadTableFromJson` 把 `hit` 桶**无差别搬进** `UnitDamaged`（`VfxMap.cs:1218`）
//       ⇒ `CardTableUnitDamagedEntries` = 2，按卡池现算（跳过附魔）= 1 ⇒ 三个红。
//     ⚠️ **过滤键必须是 `type == "enchantment"`，⛔ 不是 `cardType == 20`**：实测 `cards_engine.json` 里
//       `cardType == 20` 有 **487 张**（tactic 446 + defence 39 + enchantment 2）—— 那是「非单位牌」的大类，
//       按它过滤会把 446 张战术卡的受击件一起删掉（静默大范围错）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · A1：把 `TimedKinds` 里那两档删掉 ⇒ A1 红（也是 X3 #4 那条 `A2·J2③` 的同一件事）。
//   · A2：把它们登记进 `ZeroDurationKinds`（「零时长」那一档）⇒ A2 红（`IsZeroDurationKind` 为真）+ A3 红（时长 0）。
//   · A3：`DurationOf` 的 `case` 删掉（**登记了没落地**）⇒ A3 红（时长回到 `default: 0`）。
//   · A4：`SourceOf` 的 `case` 删掉 ⇒ A4 红（回「登记在 `TimedKinds` 里但 `switch` 里没有 `case`」那句）+ A5 红（计数涨）。
//   · A6：把两档的时长写成同一个数（例如都 0.2）⇒ A6 红（0.2 ≠ 0.4，原版两条链**各用各的字面量**）。
//   · A5：**这一条是「登记了但没 `case`」那种病的唯一探针** —— 它不读返回值，只读
//         `EventTiming.UnregisteredKindHits` 有没有涨（`default:` 那条 `ReportMissingCase` 会加数）。
//   · B1/B2：**把生成器的卡型过滤删掉**（`Pray_Exit` 回到 `hit` 桶）⇒ B1（`SelfCheckUnitSkillAnims` 报违规）
//          与 B2（桶条数 2 ≠ 池现算 1）**同时**红。
//   · B3：**灭自证那一半** —— 若哪天数据侧把附魔那条 `AnimTimes` 行删了/改名了，B3 会红
//          （「含附魔 > 不含附魔」不再成立 ⇒ 说明 B2 那条比对**根本没在区分任何东西**，是空转绿灯）。
//   · B4：手工把附魔卡塞进 `unit_damaged_anim` 桶 ⇒ B4 红。
//   · B5：把 `notPlayed[]` 里那条 `enchantment-not-unit` 的记账删掉 ⇒ B5 红（⛔ 不静默丢件）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 里加（与前几批同形）：
//      B100F11_Assert.Run(); pass += B100F11_Assert.Pass; fail += B100F11_Assert.Fail;
//  ⚠️ 宿主那两个累加器叫 **`pass` / `fail`**（不是 `_pass`）。
//  推荐位置 = **同一物理行追加**到 `:46649` 那一整行（`B93F5Assert.Run(); … Debug.Log(P + $"=== 合计 …")`
//  —— 最近几批（`B100F9` / `B100F12` / `B99C1` …）都追加在这一行）⇒ **行数零变化**，账的落点不漂移（铁律 18·b）。
//  备选位置 = `:2627`（「抽卡按座位分档」那一块收尾的 `}` 之后、`Debug.Log(P + "   事件时序（出处）：")` 之前）
//  —— 它紧挨着 `EventTiming` 那一族断言，语义最近；但那要**新插一行**，会推后 :46649 之后的行号 ⇒ 不首选。
//  ⚠️ 本件会调 `VfxMap.EnsureLoaded()` / `SelfCheckUnitSkillAnims()`（会给 `UnpinnedPicks` 加数）
//     ⇒ `Run()` 首尾各 `ResetUnpinnedForTest()` 一次，**不把计数留给下游**。
//  ⚠️ 本件不建 GameObject、不建资产、不需要场景。
//
// ============================ 覆盖边界（如实记）============================
//   · ① 只验「时长登记 + 出处」—— **验不了**「演出真的按这个时长跑」（那要时间线宿主 + 帧循环）。
//   · ② 只验「桶里没有附魔卡 + 条数与池现算一致」—— **验不了**「原版那一拍到底播不播别的件」
//     （`EnchantmentRemoved` 那一拍今天没有任何「该播哪件」的判据 ⇒ 本件**不**把它搬过去，见生成器那条 `why`）。
using System;
using System.Collections.Generic;
using CardPresentation;   // `EventTiming` / `VfxMap`（`namespace CardPresentation`）
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B100_F11`：① `DiscardReveal` / `StealReveal` 的时长登记 · ② `UnitDamaged` 桶不许收附魔卡。</summary>
public static class B100F11_Assert
{
    static readonly CheckSink _sink = new CheckSink("[F11] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // 判据里的**原版字面量**（⛔ 别改成从 `CardFeel` 读 —— 那两个常量是 `internal`、编辑器程序集取不到；
    // 而且拿我们的常量比我们的实现 = 自证。值 = `.rdata` 实读，出处见文件头）。
    const float WantDiscardReveal = 0.2f;
    const float WantStealReveal = 0.4f;

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        SectionA();
    }

    // ------------------------------------------------------------ ① 两个揭示档的时长登记
    static void SectionA()
    {
        MenuCheck.Section(_sink, "B100_F11 · ① `EvtKind.DiscardReveal` / `StealReveal` 的时长登记"
                              + "（判据 = `CardFeel.DiscardRevealStepTime` 0.2 / `StealRevealStepTime` 0.4）");

        // A0 前提：枚举里真有这两档（改名/删档 ⇒ 下面几条会变成「比一个不存在的值」，先钉死）
        bool hasDiscard = Enum.IsDefined(typeof(EvtKind), "DiscardReveal");
        bool hasSteal = Enum.IsDefined(typeof(EvtKind), "StealReveal");
        Tr(hasDiscard && hasSteal,
           $"A0（前提）：`EvtKind` 里有 `DiscardReveal` / `StealReveal` 两档"
         + $"（实得 {hasDiscard} / {hasSteal}；判据 = `RuleEngine/Core/BattleEvent.cs` 枚举）");
        Tr((int)EvtKind.DiscardReveal != (int)EvtKind.StealReveal,
           $"A0-b（前提）：两档是**两个**枚举值（{EvtKind.DiscardReveal}={(int)EvtKind.DiscardReveal} / "
         + $"{EvtKind.StealReveal}={(int)EvtKind.StealReveal}）");

        // A1 登记表：两档都「登记过」，且走的是**有时间**那一张表（不是零时长表）
        Tr(EventTiming.IsRegisteredEventKind(EvtKind.DiscardReveal)
           && EventTiming.IsRegisteredEventKind(EvtKind.StealReveal),
           "A1：两档都进了 `EventTiming` 的登记表（`ZeroDurationKinds` 或 `TimedKinds`）"
         + " —— 🧨 改前它们**没登记**（`DurationOf` 落 `default: 0`、`IsSourced` 回 false）⇒ 本条红");
        Tr(EventTiming.IsTimedKind(EvtKind.DiscardReveal) && EventTiming.IsTimedKind(EvtKind.StealReveal),
           "A1-b：两档登记在 **`TimedKinds`**（它们**有**时长，⛔ 不是零时长那一档）");

        // A2 反档：不许被登记成「零时长」（那会让 `DurationOf` 走 `IsZeroDurationKind` 分支静默回 0）
        Tr(!EventTiming.IsZeroDurationKind(EvtKind.DiscardReveal)
           && !EventTiming.IsZeroDurationKind(EvtKind.StealReveal),
           "A2：两档**不在** `ZeroDurationKinds` 里 —— 🧨 把它们挪进零时长表 ⇒ 本条与 A3 同时红");

        // A3 时长：各用各的原版字面量
        float dDiscard = EventTiming.DurationOf(EvtKind.DiscardReveal);
        float dSteal = EventTiming.DurationOf(EvtKind.StealReveal);
        MenuCheck.Near(_sink, dDiscard, WantDiscardReveal, 1e-4f,
            "A3：`DurationOf(DiscardReveal)` = 弃牌揭示的 `stepTime` 0.2（`.rdata 0x1834b2bb0` 实读）"
          + " —— 🧨 `DurationOf` 的 `case` 删掉 ⇒ 回 `default: 0`、本条红");
        MenuCheck.Near(_sink, dSteal, WantStealReveal, 1e-4f,
            "A3-b：`DurationOf(StealReveal)` = 偷牌揭示的 `stepTime` 0.4（`.rdata 0x1834b3158` 实读）");

        // A4 出处：两档各自说得出去处（且不是「登记了没 case」那句病话）
        string srcDiscard = EventTiming.SourceOf(EvtKind.DiscardReveal) ?? "";
        string srcSteal = EventTiming.SourceOf(EvtKind.StealReveal) ?? "";
        Tr(srcDiscard.IndexOf("0.2", StringComparison.Ordinal) >= 0
           && srcDiscard.IndexOf("_ResolveDiscardFromHand", StringComparison.Ordinal) >= 0,
           "A4：`SourceOf(DiscardReveal)` 说得出**值（0.2）+ 调用点**（`_ResolveDiscardFromHand`）"
         + $"（实得「{srcDiscard}」）");
        Tr(srcSteal.IndexOf("0.4", StringComparison.Ordinal) >= 0
           && srcSteal.IndexOf("_ResolveStealFromDeck", StringComparison.Ordinal) >= 0,
           "A4-b：`SourceOf(StealReveal)` 说得出**值（0.4）+ 调用点**（`_ResolveStealFromDeck`）"
         + $"（实得「{srcSteal}」）");
        Tr(srcDiscard.IndexOf("登记在 `TimedKinds` 里", StringComparison.Ordinal) < 0
           && srcSteal.IndexOf("登记在 `TimedKinds` 里", StringComparison.Ordinal) < 0,
           "A4-c：两档的出处**不是**「登记在 `TimedKinds` 里但 `switch` 里没有 `case`」那句病话"
         + " —— 🧨 `SourceOf` 的 `case` 删掉 ⇒ 本条红");
        Tr(EventTiming.IsSourced(EvtKind.DiscardReveal) && EventTiming.IsSourced(EvtKind.StealReveal),
           "A4-d：两档都 `IsSourced` = true（改前它们因为「未登记」回 false ⇒ `unsourced == 2` 那条断言变 4）");

        // A5 「登记了但没 case」的唯一探针：不读返回值，只读 `default:` 那条 `ReportMissingCase` 加的数
        {
            EventTiming.ResetUnregisteredKindForTest();
            int before = EventTiming.UnregisteredKindHits;
            EventTiming.DurationOf(EvtKind.DiscardReveal);
            EventTiming.DurationOf(EvtKind.StealReveal);
            EventTiming.SourceOf(EvtKind.DiscardReveal);
            EventTiming.SourceOf(EvtKind.StealReveal);
            int hits = EventTiming.UnregisteredKindHits - before;
            C(hits, 0,
              "A5：两档在 `DurationOf` / `SourceOf` 里**都走到了自己的 `case`**（未登记/缺 `case` 计数 = 0）"
            + " —— 🧨 只往 `TimedKinds` 加名字、忘了加 `case` ⇒ 这条红（别的断言察觉不到那一种病）");
        }

        // A6 灭自证：两档**不同值**（原版两条链各传各的字面量）。写成一个数 ⇒ 红
        Tr(Mathf.Abs(dDiscard - dSteal) > 1e-3f,
           $"A6（灭自证）：两档时长**不是同一个数**（{dDiscard:F2} ≠ {dSteal:F2}）"
         + " —— 原版弃牌 0.2 / 偷牌 0.4 各用各的字面量；🧨 图省事统一成一个值 ⇒ 本条红");
        Tr(dDiscard > 0f && dSteal > 0f,
           $"A6-b（灭自证）：两档时长都 > 0（{dDiscard:F2} / {dSteal:F2}）"
         + " —— 🧨 退回 `default: 0`（= 改前那种「不占时间线」）⇒ 本条红");
    }

}
