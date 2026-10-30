// B97W5_Assert.cs — 执行代理 `B97_W5`（第九十七会话 · 波 1）的断言
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆，见
//   `RuleEngineTest_EngineB94*.cs` 的文件头纪律）—— 加在这个文件里，`RuleEngineTest.cs` 本体
//   **一个字节都不用动**（那边只由调度台插一行调用）。
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动:676` 的 (a)(b) 两格 ＋ 账 `:607` 的 ① 格 ＋ 账 `:850` 的生产端 ＋ 账 `:1254` 的消费端。
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·6 ＋
//   本轮报告 `资料/完全复刻/报告/B97_W5.md`。
//
// ============================ 宿主接线（调度台做）============================
//   `RuleEngineTest.cs` 的 `Run()` 里，与其它 `CheckB97…` 同一批的地方加一行：
//       CheckB97W5DataColumnsAndPoolFilters();
//   （本文件**不建持久资产**、不 `AssetDatabase`、不跑 Unity 批处理。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   每条断言的文案里都带 `🧨`（= 什么改动会让它红）。三条硬口径：
//     · 期望值一律**写死字面量**，⛔ 不从被测实现反算；
//     · `if (前提) Check(结论)` 一律拆成 `Check(前提) + Check(结论)`（前提不成立时**红**，不静默跳过）；
//     · 每条「正例」都配一条**结构上不可能同时满足**的「灭自证」反例（同族纪律 → `CLAUDE.md` §三）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一行调用；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB97W5DataColumnsAndPoolFilters()
    {
        Section("🆕 B97_W5：`conditionsFilter` / `startingCardInHand` 两列 ＋ `selectFromChooseResolution` 候选池"
              + " ＋ 单次变异去重 ＋ `discardFromHand` 生产端");
        B97W5A_GateConditionsFilter();
        B97W5B_StartingCardInHand();
        B97W5C_SelectFromChooseResolution();
        B97W5D_SingleUseMutation();
        B97W5E_DiscardFromHandRow();
    }

    // ==========================================================================================
    //  A. 账 `:676` (b) 格 —— `normalAbilityBoardCriteria.countCriteria.conditionsFilter`
    // ==========================================================================================
    //  判据 `decomp_full/BoardAnalysis__MeetsCriteria.c:110-113`：候选逐个过的是 **`countCriteria` 自己的**
    //  `conditionsFilter` ⇒ 这一格必须从 `BoardCriteria.countCriteria` 带到 `CardDef.BoardGateSpec.ConditionsFilter`。
    static void B97W5A_GateConditionsFilter()
    {
        // ⚠️ 合成卡会进全局名字索引 ⇒ 用完复原（同 `RuleEngineTest_EngineE6` 的先例）
        var realPool = CardDatabase.Load();
        CardDef withCond = null, missingKey = null, noGate = null;
        try
        {
            const string jsonCond =
                "{\"version\":40,\"count\":1,\"cards\":[{\"id\":\"B97W5GateA\",\"name\":\"B97W5 闸探针A\","
              + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
              + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
              + "\"keywords\":[],\"cardTraits\":[],"
              + "\"hasAlternativeAbility\":1,"
              + "\"normalAbilityBoardCriteria\":{\"countOptions\":0,\"triggerIf\":1,\"referenceValue\":0,"
              + "\"conditionsFilter\":331,\"countSet\":{\"targetsAffected\":10,\"friendlyUnits\":1}}}]}";   // 🔴 2026-10-30（`B100_F18` · G4）：原来结尾只有 `}}}`（关 countSet / boardCriteria / 卡对象），**少了关闭 `cards` 数组的 `]` 与根对象的 `}`** ⇒ `CardDatabase.Parse` 抛 `JSON parse error: Missing a comma or ']' after an array element`。对齐同段 `jsonNoGate` 的 `}]}`。
            const string jsonMissing =
                "{\"version\":40,\"count\":1,\"cards\":[{\"id\":\"B97W5GateB\",\"name\":\"B97W5 闸探针B\","
              + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
              + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
              + "\"keywords\":[],\"cardTraits\":[],"
              + "\"normalAbilityBoardCriteria\":{\"countOptions\":0,\"triggerIf\":1,\"referenceValue\":0,"
              + "\"countSet\":{\"targetsAffected\":10,\"friendlyUnits\":1}}}]}";   // ⛔ 故意不给 `conditionsFilter`（🔴 2026-10-30 `B100_F18` · G4：结尾补 `]}` —— 同 `jsonCond`，原来少了数组/根对象的闭合）
            const string jsonNoGate =
                "{\"version\":40,\"count\":1,\"cards\":[{\"id\":\"B97W5GateC\",\"name\":\"B97W5 闸探针C\","
              + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
              + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
              + "\"keywords\":[],\"cardTraits\":[]}]}";                              // 没有整条 criteria

            var la = CardDatabase.Parse(jsonCond);
            if (la != null && la.Count == 1) withCond = la[0];
            var lb = CardDatabase.Parse(jsonMissing);
            if (lb != null && lb.Count == 1) missingKey = lb[0];
            var lc = CardDatabase.Parse(jsonNoGate);
            if (lc != null && lc.Count == 1) noGate = lc[0];
        }
        finally
        {
            CreatePool.BuildNameIndex(realPool);
        }

        CheckTrue(withCond != null, "A0 （前提）合成卡 A 解析出 1 张");
        CheckTrue(missingKey != null, "A0' （前提）合成卡 B 解析出 1 张");
        CheckTrue(noGate != null, "A0'' （前提）合成卡 C 解析出 1 张");
        if (withCond == null || missingKey == null || noGate == null) return;

        var g = withCond.NormalAbilityBoardCriteria;
        CheckTrue(g != null, "A1 （前提）`normalAbilityBoardCriteria` 落成 `BoardGateSpec`（不是 null）");
        if (g == null) return;
        Check(g.ConditionsFilter, 331,
              "★ A2 `countCriteria.conditionsFilter = 331` 带到了 `BoardGateSpec.ConditionsFilter`"
            + "｜🧨 删掉 `CardDatabasePartial_B97W5.MapBoardGateWithConditions` 里那行 `s.ConditionsFilter = raw;`"
            + "（或把 `Parse` 那行改回直调 `MapBoardGate`）⇒ 实得 0 ⇒ 红");

        var g2 = missingKey.NormalAbilityBoardCriteria;
        CheckTrue(g2 != null, "A3 （前提）缺键那张也有 criteria");
        if (g2 != null)
            Check(g2.ConditionsFilter, -1,
                  "★ A4 **缺键 ⇒ -1（未知），⛔ 不是 0**（`0` 是合法值 = 不筛；两者混了就会把"
                + "「没落的列」静默当成「不筛」）｜🧨 把映射里 `GateConditionsFilterMissing` 那支改成 `s.ConditionsFilter = 0;` ⇒ 红");

        // 🔴 2026-10-30（`B100_F34` · 332744 · **α/δ**）：改前是 `CheckTrue(…NormalAbilityBoardCriteria == null)` ——
        //   那是一条**不可能成立**的设计：`CardDto.normalAbilityBoardCriteria` 是 `[Serializable]` 类字段，
        //   `JsonUtility.FromJson` 对**缺键会物化出一份默认对象、不是 null**（判据 → `报告/B100_X10.md` §一·A1/A3；
        //   现池 1127/1129 张全是这样，唯二例外是 `SW9`/`SW21`）。
        //   ⇒ 改断「物化出来的那棵树的形状」：三个整数格 = `Unknown(-1)`（⛔ 不是 0 = 不筛）、`CountSet = null`。
        //   🔴 **真修法是生成器侧落一个显式布尔**（如 `hasNormalAbilityBoardCriteria`，见 `报告/B100_X10.md` §一·A5
        //   与排队表）—— 那要动 `工具/gen_cards_engine.py` + 数据层契约（且 ⛔ 不能用「三格都 == 缺键哨兵」反推），
        //   不在本条断言写手的白名单里。本条在「生成器落布尔」之前**仍是一条真判别式**（盯住「别默认成 0」）。
        CheckTrue(noGate.NormalAbilityBoardCriteria != null,
                  "★ A5 （前提）缺整条 `normalAbilityBoardCriteria` 的卡，`CardDef` 侧**不是 null** —— "
                + "`JsonUtility` 把缺键的 `[Serializable]` 字段物化成了默认对象（`报告/B100_X10.md` §一·A1）"
                + "｜🧨 这条红 = 物化行为变了（真出 null）⇒ 那才轮到「`== null` 才是缺席」那套契约重新成立");
        if (noGate.NormalAbilityBoardCriteria != null)
        {
            var gC = noGate.NormalAbilityBoardCriteria;
            Check(gC.CountOption, CardDef.CountSetSpec.Unknown,
                  "★★ A5-a 「缺键物化」的 `countOptions` = **`Unknown(-1)`**（⛔ 不是 0 = 不筛）"
                + "｜🧨 把 `CountSetInt` 缺键那一支改成 `return 0;` ⇒ 实得 0 ⇒ 红（= 把「没落的列」静默当成合法值）");
            Check(gC.StatOption, CardDef.CountSetSpec.Unknown,
                  "★★ A5-b `triggerIf` 同理 = **`Unknown(-1)`**｜🧨 同上（缺键那一支返回 0 ⇒ 红）");
            Check(gC.ReferenceValue, CardDef.CountSetSpec.Unknown,
                  "★★ A5-c `referenceValue` 同理 = **`Unknown(-1)`**｜🧨 同上（缺键那一支返回 0 ⇒ 红）");
            CheckTrue(gC.CountSet == null,
                  "★★ A5-d 「缺键物化」的 `countSet` = **null**（`CountSetDto` 没被物化 ⇒ `MapCountSetDto` 落 null + 出声）"
                + "｜🧨 让 `MapCountSetDto` 在 null 时新建一个空 `CountSetSpec` ⇒ 红（那会把「没落的列」当成「空 criteria」）");
        }
    }

    // ==========================================================================================
    //  B. 账 `:607` ① 格 —— `RawCardScript.startingCardInHand`
    // ==========================================================================================
    //  判据 `decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c:213-247`：开局读**督军卡 SO**
    //  的 `startingCardInHand(+0x148)`，非空就 `AddNewCardToHand`（真值非空现读**只 3 张**）。
    static void B97W5B_StartingCardInHand()
    {
        // 直接建 `CardDef`（与宿主 `Unit(...)` 同一写法：构造后挂载，⛔ 不往构造函数加形参）
        var dataSays = Unit("B97W5StartA", 1, 1, 1);
        dataSays.AttachStartingCardInHand("Tyrnak and Fenrir");
        var dataEmpty = Unit("B97W5StartB", 1, 1, 1);
        dataEmpty.AttachStartingCardInHand("");
        var dataUnknown = Unit("B97W5StartC", 1, 1, 1);      // ⛔ 不挂 = 旧表/夹具

        CheckTrue(dataSays.HasStartingCardInHand, "B1 （前提）真值已知那张的 `HasStartingCardInHand == true`");
        Check(dataSays.StartWithNames().Count, 1, "★★ B2 真值给了 1 张 ⇒ 只发这 1 张");
        Check(dataSays.StartWithNames()[0], "Tyrnak and Fenrir",
              "★★ B3 发的是**真值那个名字**｜🧨 把 `StartWithNames` 里 `if (raw.Length > 0) outList.Add(raw);` 删掉 ⇒ 实得 0 条 ⇒ B2 先红");

        CheckTrue(dataEmpty.HasStartingCardInHand, "B4 （前提）`\"\"` 也算「真值已知」（明确没有）");
        Check(dataEmpty.StartWithNames().Count, 0,
              "★★ B5 真值说**没有** ⇒ 一个都不发｜🧨 把 `raw.Length > 0` 那道闸去掉（空串也 Add）⇒ 实得 1 条空名 ⇒ 红");

        CheckTrue(!dataUnknown.HasStartingCardInHand, "B6 （前提）没挂 = 未知");

        // **真值优先**：卡面文本说有、真值说没有 ⇒ 不发，但要**出声**（notes 非空）
        var contradictory = Unit("B97W5StartD", 1, 1, 1);
        contradictory.StartWithInHand.Add("ProbeBot");
        contradictory.AttachStartingCardInHand("");
        var notes = new List<string>();
        var names = contradictory.StartWithNames(notes);
        Check(names.Count, 0,
              "★★ B7 真值(`\"\"`)与卡面文本冲突时 **以真值为准**（原版 `.c:213-247` 只读 SO 字段）"
            + "｜🧨 把 `StartWithNames` 改成「真值为空时退回文本那份」⇒ 实得 1 条 ⇒ 红");
        CheckTrue(notes.Count > 0,
              "★★ B8 冲突必须**出声**（`notes` 非空 → 调用方 `ctx.Log`）｜🧨 把 `notes.Add(...)` 那行删掉 ⇒ 红");
        CheckTrue(dataUnknown.StartWithNames().Count == 0,
              "B9 （灭自证）未知那档在没有卡面文本时也回空表（证明 B5 不是「恒空」造成的巧合）");

        // 未知档要**退回改前行为**（文本解析那条路）—— 这是「旧表零变化」的正面证据
        var textOnly = Unit("B97W5StartE", 1, 1, 1);
        textOnly.StartWithInHand.Add("ProbeBot");
        var t = textOnly.StartWithNames();
        Check(t.Count, 1, "★★ B10 真值**未知**（旧表）⇒ 退回文本那条路，行为与改前一致");
        Check(t[0], "ProbeBot", "B10' 退回的是文本里那个名字");
    }

    // ==========================================================================================
    //  C. 账 `:676` (a) 格 —— `selectFromChooseResolution`（**候选池来源**，不是筛）
    // ==========================================================================================
    //  判据 `decomp_full/AbilityLogic__GetTargets.c:1101-1140`：筛成选中的那一张 → 截成 1 → 清
    //  `cardChosenInSelection(+0x2e8)`；`chosen == null` 时整段不动。
    static void B97W5C_SelectFromChooseResolution()
    {
        var a = Unit("B97W5PoolA", 1, 1, 1);
        var b = Unit("B97W5PoolB", 1, 1, 1);
        var ctx = Battle(new[] { a, b }, new[] { Unit("B97W5Foe", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B97W5PoolA"), 1), RuleCodes.OK, "C0 （前提）A 上场");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B97W5PoolB"), 2), RuleCodes.OK, "C0' （前提）B 上场");
        var ua = B97W5UnitByName(ctx, 0, "B97W5PoolA");
        var ub = B97W5UnitByName(ctx, 0, "B97W5PoolB");
        CheckTrue(ua != null && ub != null, "C1 （前提）两个单位都在场上");
        if (ua == null || ub == null) return;

        // ---- ① flag = 0 ⇒ 一个都不动、**也不清空** ----
        ctx.CardChosenInSelection = a;
        var pool0 = new List<UnitState> { ua, ub };
        var r0 = CountEval.ApplySelectFromChooseResolution(ctx, 0, pool0, "C①");
        Check(r0.Count, 2, "★ C2 `flag = 0` ⇒ 候选池原样（2 张）｜🧨 把方法首行 `flag == 0` 那道闸删掉 ⇒ 实得 1 ⇒ 红");
        CheckTrue(ReferenceEquals(ctx.CardChosenInSelection, a),
                  "★★ C3 `flag = 0` ⇒ **不动** `CardChosenInSelection`（原版那一支压根不执行）"
                + "｜🧨 把 `flag == 0` 那道闸删掉 ⇒ 走到末尾被清 null ⇒ 红");

        // ---- ② flag = 1 但没选过牌 ⇒ 原样 ----
        ctx.CardChosenInSelection = null;
        var pool1 = new List<UnitState> { ua, ub };
        var r1 = CountEval.ApplySelectFromChooseResolution(ctx, 1, pool1, "C②");
        Check(r1.Count, 2, "★ C4 `flag = 1` 且没选过牌 ⇒ 原样（⛔ 不收窄成空表）"
                         + "｜🧨 把 `if (chosen == null) return pool;` 删掉 ⇒ 实得 0 ⇒ 红");

        // ---- ③ 正例：筛成选中的那一张 + 截成 1 + 清空 ----
        ctx.CardChosenInSelection = a;
        var pool2 = new List<UnitState> { ua, ub };
        var r2 = CountEval.ApplySelectFromChooseResolution(ctx, 1, pool2, "C③");
        Check(r2.Count, 1, "★★ C5 选中 A ⇒ 候选池 2 → 1");
        CheckTrue(r2.Count == 1 && ReferenceEquals(r2[0], ua),
                  "★★ C6 留下的**正是选中那张**（A），不是 B｜🧨 把 `RemoveAll` 的条件取反（`!string.Equals` → `string.Equals`）⇒ 留住 B ⇒ 红");
        CheckTrue(ctx.CardChosenInSelection == null,
                  "★★ C7 用完**清空** `CardChosenInSelection`（原版 `.c:1130` 的 `*plVar1 = 0`）"
                + "｜🧨 删掉 `ctx.CardChosenInSelection = null;` ⇒ 红");

        // ---- ④ 选中的那张不在池里 ⇒ 空表（原版同样得到空表），且照样清空 ----
        ctx.CardChosenInSelection = Unit("B97W5NotInPool", 1, 1, 1);
        var pool3 = new List<UnitState> { ua, ub };
        var r3 = CountEval.ApplySelectFromChooseResolution(ctx, 1, pool3, "C④");
        Check(r3.Count, 0, "★ C8 选中的那张不在候选池 ⇒ 池子空（⛔ 不是「原样保留」）");
        CheckTrue(ctx.CardChosenInSelection == null, "C9 这一档同样清空");
        _ = b;   // 夹具留用（B 卡只用来凑第二个候选）
    }

    // ==========================================================================================
    //  D. 账 `:1254` —— `CardScript.HasSingleUseMutation` ＋ `GetTargets` 的 `traitsFilter == 780` 去重
    // ==========================================================================================
    //  判据 `CardScript__HasSingleUseMutation.c:1-60` / `AbilityLogic__GetTargets.c:155-172` +
    //  `:1370-1385`。数据：`IsCumulativeMutation` 只认 `mutationType == 20`。
    static void B97W5D_SingleUseMutation()
    {
        var single = Unit("B97W5MutSingle", 1, 1, 1);
        single.AttachAbilityMutations(new[] { new CardDef.AbilityMutationSpec { Ab = 0, Lg = 0, MutationType = 10 } });
        var cumulative = Unit("B97W5MutCumul", 1, 1, 1);
        cumulative.AttachAbilityMutations(new[] { new CardDef.AbilityMutationSpec { Ab = 0, Lg = 0, MutationType = 20 } });

        CheckTrue(!single.IsCumulativeMutation(), "D1 （前提）`mutationType = 10` **不是**累计型");
        CheckTrue(cumulative.IsCumulativeMutation(), "D1' （前提）`mutationType = 20` **是**累计型");

        var a = Unit("B97W5CasterA", 1, 1, 1);
        var ctx = Battle(new[] { a }, new[] { Unit("B97W5FoeD", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B97W5CasterA"), 0), RuleCodes.OK, "D0 （前提）施放者上场");
        var caster = B97W5UnitByName(ctx, 0, "B97W5CasterA");
        CheckTrue(caster != null, "D0' （前提）施放者拿到");
        if (caster == null) return;

        CheckTrue(!RuleCore.HasSingleUseMutation(caster.Instance, null),
                  "D2 `targetCard == null` ⇒ 假（原版 `.c:19-21`）");

        // 没挂过 ⇒ 假
        CheckTrue(!RuleCore.HasSingleUseMutation(caster.Instance, single),
                  "D3 施放者身上没挂这条变异 ⇒ 假");

        // 挂一条「单次变异」的附魔记录（= 原版 `activeEffects(+0x108)` 里 `enchantment(+0x88)` 指向它）
        caster.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = single, BuffType = 15 });
        CheckTrue(RuleCore.HasSingleUseMutation(caster.Instance, single),
                  "★★ D4 已经挂过**同一条单次变异** ⇒ 真（原版 `.c:33-56` 扫 `+0x108` 比 `+0x88`）"
                + "｜🧨 把 `ReferenceEquals` 改成 `false` / 删掉循环 ⇒ 红");

        // 累计型 ⇒ 恒假（即使挂过）
        caster.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = cumulative, BuffType = 15 });
        CheckTrue(!RuleCore.HasSingleUseMutation(caster.Instance, cumulative),
                  "★★ D5 **累计型变异恒假**（可以叠 —— 原版 `.c:22-27` 先判 `IsCumulativeMutation` 就 `return 0`）"
                + "｜🧨 把 `if (targetCard.IsCumulativeMutation()) return false;` 删掉 ⇒ 实得 true ⇒ 红");

        // ---- 池过滤：`traitsFilter == 780` 才动手 ----
        //   两张候选**按正常打牌链上场**（⛔ 不手写棋盘：`UnitState` 没有 `(inst, player, slot)` 构造，
        //   自己摆格会绕过部署链、夹具失真 —— 同 `B97W3` 的教训）
        var self = Unit("B97W5SelfMut", 1, 1, 1);
        var other = Unit("B97W5OtherMut", 1, 1, 1);
        caster.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = self, BuffType = 15 });
        // 🔴 2026-10-30 `B100_F25`（#50–#52）：这两张原来**只造了 `CardDef`、从没进过手牌** ⇒ `HandIdx` 回 −1
        //    ⇒ `PlayCard` 走 `ErrBadHand(1)`、`u1/u2` 拿到 null、D6/D6'/D6'' 三条一起红。
        //    `self`/`other` 是**白盒 `CardDef`**（不经牌库）⇒ 必须先 `ctx.NewInstance` 塞进手牌再打（⛔ 不改期望值）。
        ctx.Players[0].Hand.Add(ctx.NewInstance(self));
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B97W5SelfMut"), 2), RuleCodes.OK, "D6 （前提）候选一上场");
        ctx.Players[0].Hand.Add(ctx.NewInstance(other));
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B97W5OtherMut"), 3), RuleCodes.OK, "D6' （前提）候选二上场");
        var u1 = B97W5UnitByName(ctx, 0, "B97W5SelfMut");
        var u2 = B97W5UnitByName(ctx, 0, "B97W5OtherMut");
        CheckTrue(u1 != null && u2 != null, "D6'' （前提）两张候选都在场上");
        if (u1 == null || u2 == null) return;

        var poolA = new List<UnitState> { u1, u2 };
        var rA = RuleCore.FilterSingleUseMutationTargets(ctx, poolA, caster.Instance, 0, "D③");
        Check(rA.Count, 2, "★ D7 `traitsFilter != 780` ⇒ 一个都不剔（⛔ 别在别处无条件调它）"
                         + "｜🧨 把 `traitsFilter != MutationTrait` 那道闸删掉 ⇒ 实得 1 ⇒ 红");

        var poolB = new List<UnitState> { u1, u2 };
        var rB = RuleCore.FilterSingleUseMutationTargets(ctx, poolB, caster.Instance, 780, "D③");
        Check(rB.Count, 1, "★★ D8 `traitsFilter = 780` ⇒ 只剩没挂过的那一张");
        CheckTrue(rB.Count == 1 && ReferenceEquals(rB[0], u2),
                  "★★ D9 剔掉的是**已经挂过同一条**的那张（`self`），留下的不是它"
                + "｜🧨 把 `HasSingleUseMutation` 的返回值取反 ⇒ 留下的是反的那一张 ⇒ 红");

        // 施放者实例缺失 ⇒ 不剔（并出声）——「宁可少剔也不多剔」
        var poolC = new List<UnitState> { u1, u2 };
        var rC = RuleCore.FilterSingleUseMutationTargets(ctx, poolC, null, 780, "D④");
        Check(rC.Count, 2, "★ D10 拿不到施放者实例 ⇒ **不剔**并出声（原版回落 `thisCard`；我们宁可少剔）");
    }

    // ==========================================================================================
    //  E. 账 `:850` 的生产端 —— `chosenAbility 160 / 165` 的两支行分派
    // ==========================================================================================
    //  判据 `AbilityLogic__PlayAbility.c:577-603`（160）/ `:293-345`（165）。结算体复用既有
    //  `DoDiscardFromHand` / `DoDiscardWholeHand` → `DiscardHandCardCore` → `RuleCore.RemoveCostEffects`。
    //  🔴 两行都**先读 `playersAffected`**（`AbilityLogic.playersAffected`）再动：读不到 / `Both`（160）
    //     ⇒ 出声 + 不结算（⛔ 不猜哪一侧 —— 见 `RunDiscardFromHandRow` 的 doc）。
    static void B97W5E_DiscardFromHandRow()
    {
        // 夹具卡必须**带 `activeAbilityLogic` 那一列**（`pa` 就在里面）—— 手写 `CardDef` 拿不到它，
        // 所以走合成 JSON（与 `RuleEngineTest_EngineE6` 同一写法）。
        var realPool = CardDatabase.Load();
        CardDef casterCard = null, wholeCard = null;
        try
        {
            const string json160 =
                "{\"version\":40,\"count\":1,\"cards\":[{\"id\":\"B97W5Caster160\",\"name\":\"B97W5 弃牌施放者\","
              + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
              + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
              + "\"keywords\":[],\"cardTraits\":[],\"hasActiveAbility\":1,"
              + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
              + "\"ta\":[30],\"fm\":[0],\"ca\":[160],\"pa\":[10]}]}]}";
            const string json165 =
                "{\"version\":40,\"count\":1,\"cards\":[{\"id\":\"B97W5Caster165\",\"name\":\"B97W5 整手牌施放者\","
              + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
              + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
              + "\"keywords\":[],\"cardTraits\":[],\"hasActiveAbility\":1,"
              + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
              + "\"ta\":[30],\"fm\":[0],\"ca\":[165]}]}]}";     // ⛔ 故意不给 `pa`
            var la = CardDatabase.Parse(json160);
            if (la != null && la.Count == 1) casterCard = la[0];
            var lb = CardDatabase.Parse(json165);
            if (lb != null && lb.Count == 1) wholeCard = lb[0];
        }
        finally
        {
            CreatePool.BuildNameIndex(realPool);
        }

        CheckTrue(casterCard != null, "E0 （前提）160 夹具卡解析出 1 张");
        CheckTrue(wholeCard != null, "E0' （前提）165 夹具卡解析出 1 张");
        if (casterCard == null || wholeCard == null) return;

        var payload = Unit("B97W5DiscardMe", 1, 1, 1);
        var ctx = Battle(new[] { casterCard, payload }, new[] { Unit("B97W5FoeE", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, casterCard.Name), 1), RuleCodes.OK, "E1 （前提）施放者上场");
        var caster = B97W5UnitByName(ctx, 0, casterCard.Name);
        CheckTrue(caster != null, "E1' （前提）施放者拿到");
        if (caster == null) return;

        // 给那张手牌钉一条**实例级**费用修正（原版 = 挂在那张牌 `+0x108` 上的 `changeCost(2)`）
        var inst = B97W5HandByName(ctx, 0, "B97W5DiscardMe");
        CheckTrue(inst != null, "E2 （前提）那张手牌还在手里");
        if (inst == null) return;
        // 🔴 2026-10-30 `B100_F25`（#53）：`Battle(...)` 的 `Deck()` 塞 20 张 `filler`、起手 3 张 ⇒
        //    手牌里除了 `payload` 还有 **`filler19`**，弃牌命中的可能是它（那就不带这条费用修正）⇒
        //    E7 恒红。这里**只留被弃的那一张**，把「弃的是谁」钉死（⛔ 不改期望值、不改产品）。
        ctx.Players[0].Hand.RemoveAll(h => !ReferenceEquals(h, inst));
        ctx.CostMods.Add(new CostMod { Player = 0, Key = inst.Card.Id, Delta = -1, HandInstanceId = inst.Id });
        int costModsBefore = ctx.CostMods.Count;
        int handBefore = ctx.Players[0].Hand.Count;
        int discardBefore = ctx.Players[0].Discard.Count;
        int foeHandBefore = ctx.Players[1].Hand.Count;

        // ---- 160 + `pa = 10 (Self)`：弃自己 1 张 ----
        var prevActing = ctx.ActingUnit;
        ctx.ActingUnit = caster;      // 原版 `playersAffected` 读的就是「正在结算的那张能力」（`PlayersAffectedOf` 同口径）
        int r;
        try { r = RuleCore.RunDiscardFromHandRow(ctx, 0, caster, 0, 30, null); }
        finally { ctx.ActingUnit = prevActing; }
        Check(r, 1, "★★ E3 `chosenAbility 160` + `pa = Self` ⇒ **真的弃掉了一张**"
                  + "｜🧨 把 `RunDiscardFromHandRow` 里那次 `DoDiscardFromHand(...)` 调删掉（直接 `return 1`）⇒ 红");
        Check(ctx.Players[0].Hand.Count, handBefore - 1, "★★ E4 **自己**的手牌少 1 张（`pa = Self` 那一侧）");
        Check(ctx.Players[1].Hand.Count, foeHandBefore,
              "★★ E5 对面的手牌**没动**（⛔ 别按 `DiscardHandSeatFor` 的「非 own ⇒ 对面」缺省猜）"
            + "｜🧨 把 `RunDiscardFromHandRow` 里 `op.Target = new EffectTargetSpec { Side = … }` 那行删掉 ⇒ 弃错人 ⇒ 红");
        Check(ctx.Players[0].Discard.Count, discardBefore + 1, "★★ E6 **弃牌堆**多 1 张（去向 = 弃牌堆，不是移出游戏）");
        CheckTrue(ctx.CostMods.Count == costModsBefore - 1,
                  "★★ E7 那张牌**钉在自己身上的费用修正被摘掉**（`RuleCore.RemoveCostEffects` —— 账 `:850` 的"
                + "「以销毁方式离手」那一跳）｜🧨 把 `DiscardHandCardCore` 里 `RuleCore.RemoveCostEffects(ctx, inst);`"
                + " 那行删掉 ⇒ 实得 " + costModsBefore + " ⇒ 红");

        // ---- 165 + `pa` 读不到 ⇒ 一张都不动（⛔ 不猜成 Both）----
        var p2 = Unit("B97W5WholeHand", 1, 1, 1);
        var ctx2 = Battle(new[] { wholeCard, p2 }, new[] { Unit("B97W5FoeE2", 1, 0, 9) });
        ToP1Turn(ctx2, 2);
        CheckCode(RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, wholeCard.Name), 1), RuleCodes.OK, "E8 （前提）第二个施放者上场");
        var caster2 = B97W5UnitByName(ctx2, 0, wholeCard.Name);
        CheckTrue(caster2 != null, "E8' （前提）第二个施放者拿到");
        if (caster2 == null) return;
        int hand2Before = ctx2.Players[0].Hand.Count;
        int r2;
        var prev2 = ctx2.ActingUnit;
        ctx2.ActingUnit = caster2;
        try { r2 = RuleCore.RunDiscardWholeHandRow(ctx2, 0, caster2, 0, 30, null); }
        finally { ctx2.ActingUnit = prev2; }
        Check(r2, 0, "★★ E9 `playersAffected` 读不到 ⇒ 返回 0（**不结算**）"
                   + "｜🧨 把那道 `pa != 0 && pa != 10 && pa != 20` 的闸删掉 ⇒ 返回 1 ⇒ 红");
        Check(ctx2.Players[0].Hand.Count, hand2Before,
              "★★ E10 手牌**一张都不动**（⛔ 不猜成 `Both` 把两侧全弃）");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>按卡名在某一侧手牌里找那份实例（`null` = 不在手里）。</summary>
    static CardInstance B97W5HandByName(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++)
            if (hand[i] != null && hand[i].Card != null && hand[i].Card.Name == name) return hand[i];
        return null;
    }

    /// <summary>按卡名在某一侧棋盘上找那个单位（扫全场 −1 = 没找到）。</summary>
    static UnitState B97W5UnitByName(BattleContext ctx, int p, string name)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
            if (board[s] != null && board[s].Card != null && board[s].Card.Name == name) return board[s];
        return null;
    }
}
