// RuleEngineTest_EngineB96CB.cs — 第九十五会话 · 战斗线 C21 · 执行代理 `B96_CB`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
//   本件只放本批新增/接线那四件的断言；宿主 `RuleEngineTest.cs` **本笔没动**（它不在本批白名单）
//   ⇒ ⚠️ **本件的入口 `CheckB96CBEngine()` 还没有被宿主调用**，这是**已知的接线待办**（如实记，
//   不在报告里假装已接通）。接法照 `B94_B8` 那一件：宿主 `Run()` 里加 `{ CheckB96CBEngine(); }`。
//
// 覆盖（判据 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   ① 账 921  `CardScript.ShowCardBeingDrawn` 的两档发射（`EvtKind.DiscardReveal` / `StealReveal`）
//             —— 判据 `CardScript__ShowCardBeingDrawn.c:9-14` + 两个调用点的 `.rdata` 常量（0.2 / 0.4）
//   ② 账 1037 `EntityScript.GetThisCardPlayedDamage`「手牌那一档」的取数口
//             —— 判据 `EntityScript__GetThisCardPlayedDamage.c:1-106`（+ 本工程结构差异见方法头）
//   ③ 账 372/422 preMulligan 脚本的执行那一半 `RuleCore.RunPremulliganScriptedTurn`
//             —— 判据 `BattleManager._StartBattleSequence_d__331:33` / `__GetPremulliganScriptedData.c`
//   ④ 账 248  `BattleManager.CheckForHeroDeckTransform` 的换牌库写口
//             —— 判据 `BattleManager__CheckForHeroDeckTransform.c` + `BoardAnalysis__DeckMeetsCriteria.c`
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（⚠️ 宿主还没接线 —— 见文件头）。⛔ 不用 `Test` 前缀（避开 `VerifyRunCaseCoverage`）。</summary>
    public static void CheckB96CBEngine()
    {
        Section("🆕 B96_CB：ShowCardBeingDrawn 两档（921）· ThisCardPlayedDamage 手牌档（1037）"
              + " · preMulligan 执行（372/422）· CheckForHeroDeckTransform（248）");
        B96CBT1RevealEvents();
        B96CBT2ThisCardPlayedDamageFromHand();
        B96CBT3PremulliganScript();
        B96CBT4HeroDeckTransform();
    }

    /// <summary>① 921：`RuleCore.EmitCardBeingDrawn` 两档 ⇒ 两条不同的 `EvtKind`，字段按契约填。
    /// 判据 = `CardScript__ShowCardBeingDrawn.c:9-14` 与两个调用点（0.2 / 0.4）。
    /// <para>⚠️ 本件只断**引擎发出的那一条事件**；「表现层怎么演」在 `CardFeel` / `BattleDriver.PlayCardBeingDrawn`
    /// （那两条要 Unity 视图，`RuleEngineTest` 是纯引擎宿主 ⇒ 这里的边界如实记）。</para></summary>
    static void B96CBT1RevealEvents()
    {
        var a = Unit("B96RevealA", 1, 1, 1);
        var ctx = Battle(new[] { a }, System.Array.Empty<CardDef>());
        var inst = ctx.Players[0].Hand[0];
        int before = ctx.Signals.Count;

        RuleCore.EmitCardBeingDrawn(ctx, 0, inst, isSteal: false);
        Check(ctx.Signals.Count, before + 1, "弃牌揭示：发**一条**事件");
        var e0 = ctx.Signals[ctx.Signals.Count - 1];
        CheckTrue(e0.Kind == EvtKind.DiscardReveal, "🧨 弃牌揭示的 Kind = `DiscardReveal`（0.2 那一档）");
        Check(e0.Player, 0, "`Player` = 原持有方（原版 `isPlayer`）");
        Check(e0.Slot, -1, "`Slot` 恒 -1（不在场上）");
        Check(e0.CardId, "B96RevealA", "`CardId` = 卡名");
        Check(e0.InstanceId, inst.Id, "`InstanceId` = 这一份的实例号");

        RuleCore.EmitCardBeingDrawn(ctx, 1, inst, isSteal: true);
        var e1 = ctx.Signals[ctx.Signals.Count - 1];
        CheckTrue(e1.Kind == EvtKind.StealReveal, "🧨 偷牌揭示的 Kind = `StealReveal`（0.4 那一档，⛔ 别与弃牌那一档合并）");
        Check(e1.Player, 1, "偷牌：`Player` = **被偷那一方**（牌彼时还在他的牌库里）");

        // 空实参 ⇒ 一条都不发（并且出声）—— 原版那个方法永远有一张 CardScript 实参
        int n = ctx.Signals.Count;
        RuleCore.EmitCardBeingDrawn(ctx, 0, null, isSteal: false);
        Check(ctx.Signals.Count, n, "🧨 `inst == null` ⇒ **一条都不发**（⛔ 别发一条空卡名的事件）");
    }

    /// <summary>② 1037：手牌那一档的取数口 —— 只收「打在**玩家点选的那个敌人**身上」的 `deal`。
    /// 判据 = `EntityScript__GetThisCardPlayedDamage.c:1-106`（本工程结构等价物见方法头）。</summary>
    static void B96CBT2ThisCardPlayedDamageFromHand()
    {
        var ctx = Battle(System.Array.Empty<CardDef>(), System.Array.Empty<CardDef>());

        var deal = new CardDef("B96Deal", "B96Deal", "tactic", "Deal 3 damage to an enemy unit",
                               null, "Test", 1, 0, 0, 0, null);
        var entries = RuleCore.ThisCardPlayedDamageEntries(ctx, deal);
        Check(entries.Count, 1, "「Deal 3 damage to an enemy unit」⇒ **一条** deal");
        if (entries.Count == 1) Check(entries[0], 3, "金额 = 卡面那两个数字（3）");
        Check(RuleCore.ThisCardPlayedDamage(ctx, deal), 3, "求和形态 = 3");

        // 没有卡面正文 ⇒ 一条都不收（原版那 183 张之外的那些）
        var none = new CardDef("B96NoText", "B96NoText", "tactic", "", null, "Test", 1, 0, 0, 0, null);
        Check(RuleCore.ThisCardPlayedDamageEntries(ctx, none).Count, 0, "🧨 没有正文 ⇒ 空表（⛔ 别返回 whitelist 里的常量）");
        Check(RuleCore.ThisCardPlayedDamageEntries(ctx, null).Count, 0, "`def == null` ⇒ 空表");
    }

    /// <summary>③ 372/422：preMulligan 脚本「执行那一半」。
    /// 判据 = `BattleManager._StartBattleSequence_d__331:33`（判门 ⇒ 起脚本 ⇒ 跑完才进换牌）
    /// / `BattleManager__IsPremulliganScriptAvailable.c:1-33`。
    /// <para>⚠️ **现网数据 6/6 关 `scriptedTurn=false`** ⇒ 真实对局里恒不跑；这里用**合成数据**钉住「一旦开闸就会跑」。</para></summary>
    static void B96CBT3PremulliganScript()
    {
        // (a) 没有教程 ⇒ 0（原版 `IsTutorialStageAvailable` 为假那一支）
        var plain = Battle(System.Array.Empty<CardDef>(), System.Array.Empty<CardDef>());
        Check(RuleCore.RunPremulliganScriptedTurn(plain), 0, "🧨 非教程局 ⇒ 0 条（⛔ 别在普通局里跑脚本）");
        Check(RuleCore.RunPremulliganScriptedTurn(null), 0, "`ctx == null` ⇒ 0");

        // (b) 有教程关但 `scriptedTurn=false`（= **现网 6 关的真实形状**）⇒ 0
        var offStage = new TutorialStageData
        {
            stage = 1,
            preMulligan = new TutorialTurn { turnName = "pre", scriptedTurn = false, actions = new TutorialAction[0] },
        };
        var off = Battle(System.Array.Empty<CardDef>(), System.Array.Empty<CardDef>());
        off.Tutorial = new TutorialScript(offStage);
        Check(RuleCore.RunPremulliganScriptedTurn(off), 0, "🧨 `scriptedTurn=false` ⇒ 0 条（现网 6 关即此档）");

        // (c) 开闸：两条 `SmallTip`（= 80，演出档 ⇒ 不改局面）⇒ 2 条交给执行器
        var onStage = new TutorialStageData
        {
            stage = 1,
            preMulligan = new TutorialTurn
            {
                turnName = "pre", scriptedTurn = true, actionCount = 2,
                actions = new[]
                {
                    new TutorialAction { index = 0, data = new[] { new TutorialActionData { type = (int)ScriptedActionType.SmallTip, typeName = "SmallTip" } } },
                    new TutorialAction { index = 1, data = new[] { new TutorialActionData { type = (int)ScriptedActionType.SmallTip, typeName = "SmallTip" } } },
                },
            },
        };
        var on = Battle(System.Array.Empty<CardDef>(), System.Array.Empty<CardDef>());
        on.Tutorial = new TutorialScript(onStage);
        int ran = RuleCore.RunPremulliganScriptedTurn(on);
        Check(ran, 2, "🧨 门开 + 2 条动作 ⇒ 交给执行器 **2** 条（⛔ 别只跑第一条）");
        // 前提：这两条是**演出档**（`StageActions` 记账），不是「未实现」
        var tut = on.Tutorial;
        CheckTrue(!tut.Unhandled.ContainsKey(ScriptedActionType.SmallTip),
                  "前提：`SmallTip` 不该被记成「引擎侧没接」（`Unhandled`）");
    }

    /// <summary>④ 248：换牌库写口。
    /// 判据 = `BattleManager__CheckForHeroDeckTransform.c`（`rawBattleData != null` ∧ 关卡给了替换卡 ∧
    /// `BoardAnalysis.DeckMeetsCriteria(牌表, 替换卡)` ⇒ 换牌库）。
    /// <para>⚠️ **那两格关卡数据本机不存在**（`CampaignHeroTransformCard/Deck` 恒 `null`）⇒ 这一条今天
    /// 在生产里恒走「不换」那一支；这里用**合成数据**钉住写口本身。</para></summary>
    static void B96CBT4HeroDeckTransform()
    {
        var hero = HeroOf("B96Hero", "Test", 2, 30);
        var d0 = new List<CardDef> { hero, Unit("B96Old1", 1, 1, 1), Unit("B96Old2", 1, 1, 1) };
        var ctx = RuleCore.NewBattle(d0, new List<CardDef> { HeroOf("B96Hero2", "Test", 2, 30) },
                                     seed: 0, shuffle: false);
        int deckBefore = ctx.Players[0].Deck.Count;

        // (a) 数据缺 ⇒ 不换（**这就是今天的生产路径**）
        CheckTrue(!RuleCore.CheckForHeroDeckTransform(ctx, 0, null, null),
                  "🧨 替换卡/牌表为空 ⇒ **不换**（原版那两道闸也不过）");
        Check(ctx.Players[0].Deck.Count, deckBefore, "不换的时候牌库一张不动");

        // (b) 合成数据：替换卡在牌表里 ⇒ 换
        var newDeck = new List<CardDef> { hero, Unit("B96New1", 1, 2, 2) };
        int oldFirstId = ctx.Players[0].Deck[0].Id;
        CheckTrue(RuleCore.CheckForHeroDeckTransform(ctx, 0, hero, newDeck),
                  "🧨 判据满足 ⇒ 换牌库（`DeckMeetsCriteria` 命中替换卡）");
        Check(ctx.Players[0].Deck.Count, 2, "换完 = 替换牌表的张数（2）");
        CheckTrue(ctx.Players[0].Deck[0].Id != oldFirstId,
                  "🧨 换的是**新的一份实例**（原版换的是整副牌的对象，⛔ 不是复用旧实例）");

        // (c) 判据不满足（牌表里没有那张替换卡）⇒ 不换
        var noMatch = new List<CardDef> { Unit("B96X", 1, 1, 1) };
        CheckTrue(!RuleCore.CheckForHeroDeckTransform(ctx, 1, hero, noMatch),
                  "🧨 牌表里没有那张替换卡 ⇒ **不换**（`DeckMeetsCriteria` 为假）");
    }
}
