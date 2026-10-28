// RuleEngineTest_EngineB94B3.cs — 第九十四会话 · 波 2 · 执行代理 `B3`（引擎小件残留 B/C 深挖）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有一个写手改 `RuleEngineTest.cs`（归 F1）⇒ 本批断言全写在这里、**只交一个入口方法名**，
// 由调度台在 `Run()` 里加一行 `Step(B94B3EngineGaps)`（⛔ 本代理不改 F1 的文件）。
//
// 本件覆盖的原版方法与账行（判据全文 = `D:/2/tools/decomp_full/`，逐句现读 2026-10-29）：
//   ① `BattleManager.GetPremulliganScriptedData`（账 372）+ `IsPremulliganScriptAvailable`（账 422）
//      —— 本批【新建】`Core/TutorialPremulligan.cs` 落地这两个谓词（原账「没有谓词」⇒ 已建）。
//      `.c`：`preMulligan != null && scriptedTurn(+0x18) && scriptedActions(+0x20) != null && list.Count >= 1`。
//   ② `DamageDeltToEnemyUnit.OnDamageDelt`（账 1004）—— 记的是**扣甲前**的 `DamageInfo.damageValue`
//      ⇒ `AchievementTally.AddDamageRaw` 与 `AddDamage`（扣甲后）**两格并存**。
//   ③ `TriggerAbility.OnTriggerAbility`（账 1357）—— 原版 `ChallengeLogMgr.LogTriggerAbility` 把
//      20(`WhileInPlay`) / 240(`WhileInPlayVariable`) / 630(`TriggeredSynapse`) 三档**挡在信号外**
//      ⇒ `AchievementTally.AddTrigger` 也不记它们。
//   ④ `EnergySpentSignal.Raise`（账 1373）—— 花掉的能量按类型分格（CardCost 1 / AbilityCost 2）。
//   ⑤ `TroopsKilledCount.OnTroopKilled`（账 1360）—— `AchievementTally.AddTroopKilled`。
//   ⑥ `MinionManager.GetOwnPositionInBoard`（账 1124）—— `BoardSlots.OriginalIndexOfCard` ↔ `TryGetCardInPos` 往返。
//   ⑦ `MinionManager.GetClosestAvailableSlot`（账 1107）—— `BoardSlots.Resolve` 的侧/下标与「满侧换对侧」。
//   ⑧ `BattleManager.CanPlayCard`（账 233）的 `RuleCodes.TermKey` 词条映射一半。
//   ⑨ `EntityScript.HasEnchantment`（账 1042）—— `Enchantments.HasEnchantmentForTrigger`（空载体 ⇒ false）。
//   ⑩ `BoardAnalysis.MeetsCriteria`（账 676）—— `CountEval.MeetsBoardCriteria` 的 null 闸 / playerTurn 闸。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行 `Step(B94B3EngineGaps)`）——
    /// ⚠️ 方法名按本批简报要求**不以 `Test`/`Report` 打头**。</summary>
    public static void B94B3EngineGaps()
    {
        Section("🆕 B3（B94 波 2）：换牌前脚本谓词 · 成就计数三格 · BoardSlots 往返/落点 · 词条映射 · 附魔闸 · 棋盘条件闸");
        B94B3T1PremulliganPredicate();
        B94B3T2TallyDamageRawVsActual();
        B94B3T3TallyTriggerExclusionsAndEnergy();
        B94B3T4TallyTroopKilledAndHeal();
        B94B3T5BoardSlotRoundTrip();
        B94B3T6BoardResolveSideIndex();
        B94B3T7RuleCodesTermMap();
        B94B3T8EnchantmentGate();
        B94B3T9BoardCriteriaGate();
    }

    // ==============================================================================================
    //  ① 换牌前脚本谓词 + 取值器（账 372 / 422 · Core/TutorialPremulligan.cs）
    // ==============================================================================================
    static void B94B3T1PremulliganPredicate()
    {
        // ── (a) stage.preMulligan == null ⇒ null / false ───────────────────────────────────────
        {
            var st = new TutorialStageData();   // preMulligan 缺省 null
            CheckTrue(TutorialPremulligan.GetPremulliganScriptedData(st) == null,
                      "★★ ① `preMulligan == null` ⇒ `GetPremulliganScriptedData` = **null**"
                    + "｜判据 `BattleManager__GetPremulliganScriptedData.c`（`lVar2 == 0 ⇒ return 0`）"
                    + "｜🧨 不判 null 直接 `stage.preMulligan.scriptedTurn` ⇒ NRE ⇒ 红");
            CheckTrue(!TutorialPremulligan.IsPremulliganScriptAvailable(st),
                      "★★ ① `preMulligan == null` ⇒ `IsPremulliganScriptAvailable` = **false**"
                    + "｜🧨 谓词与取值器不一致（一个判一个不判）⇒ 红");
        }
        // ── (b) scriptedTurn == false（数据 6 关全这一档）⇒ null ─────────────────────────────────
        {
            var st = new TutorialStageData
            {
                preMulligan = new TutorialTurn { scriptedTurn = false, actions = new[] { new TutorialAction() } }
            };
            CheckTrue(TutorialPremulligan.GetPremulliganScriptedData(st) == null,
                      "★★ ① `scriptedTurn == false` ⇒ **null**（原版 `.c` 的 `+0x18 == '\\0' ⇒ return 0`）"
                    + "｜🧨 只看 actions 不看 scriptedTurn ⇒ 实得非 null ⇒ 红"
                    + "（🔴 6 关数据全是这一档 ⇒ 谓词今天恒假，休眠如实标）");
        }
        // ── (c) scriptedTurn == true 但 actions == null ⇒ null ──────────────────────────────────
        {
            var st = new TutorialStageData
            {
                preMulligan = new TutorialTurn { scriptedTurn = true, actions = null }
            };
            CheckTrue(TutorialPremulligan.GetPremulliganScriptedData(st) == null,
                      "★★ ① `actions == null` ⇒ **null**（原版 `.c` 的 `+0x20 == 0 ⇒ return 0`）"
                    + "｜🧨 改成 `actions?.Length ?? 0 >= 1` 而不判 null 会让空集合误过 ⇒ 视实现分支而定，"
                    + "本条锁「null 也算不可用」");
        }
        // ── (d) actions.Length == 0 ⇒ null ─────────────────────────────────────────────────────
        {
            var st = new TutorialStageData
            {
                preMulligan = new TutorialTurn { scriptedTurn = true, actions = new TutorialAction[0] }
            };
            CheckTrue(TutorialPremulligan.GetPremulliganScriptedData(st) == null,
                      "★★ ① `actions.Length == 0` ⇒ **null**（原版 `.c` 的 `list.Count < 1 ⇒ return 0`）"
                    + "｜🧨 用 `>= 0` 替 `>= 1` ⇒ 空列表误判为可用 ⇒ 红");
        }
        // ── (e) 三闸全过 ⇒ 返回**那一个对象** / true ───────────────────────────────────────────
        {
            var pre = new TutorialTurn { scriptedTurn = true, actions = new[] { new TutorialAction() } };
            var st = new TutorialStageData { preMulligan = pre };
            CheckTrue(ReferenceEquals(TutorialPremulligan.GetPremulliganScriptedData(st), pre),
                      "★★ ① 三闸全过 ⇒ 返回的是**同一个 `preMulligan` 对象**（原版返回 `stage + 0x50`）"
                    + "｜🧨 返回副本 / 返回 actions[0] ⇒ `ReferenceEquals` 红");
            CheckTrue(TutorialPremulligan.IsPremulliganScriptAvailable(st),
                      "★★ ① 三闸全过 ⇒ `IsPremulliganScriptAvailable` = **true**"
                    + "｜🧨 把 `>= 1` 写反成 `<= 1` ⇒ 红");
        }
        // ── (f) stage == null ⇒ null / false（不复制原版 NRE）─────────────────────────────────
        {
            CheckTrue(TutorialPremulligan.GetPremulliganScriptedData(null) == null
                      && !TutorialPremulligan.IsPremulliganScriptAvailable(null),
                      "★★ ① `stage == null` ⇒ null / false（原版那一格会走 `FUN_1803f47a0` NRE ⇒ 我们不复制崩溃语义）");
        }
    }

    // ==============================================================================================
    //  ② 成就计数：扣甲前(raw) vs 扣甲后(actual) 两格并存（账 1004 · DamageDeltToEnemyUnit）
    // ==============================================================================================
    static void B94B3T2TallyDamageRawVsActual()
    {
        var t = new AchievementTally();
        t.AddDamageRaw(0, DamageKind.Melee, 5);   // 原版 OnDamageDelt 记的 damageValue（扣甲前）
        t.AddDamage(0, DamageKind.Melee, 3);      // 我们现役口径（扣甲后 actual）
        Check(t.DamageRawOf(0, DamageKind.Melee), 5,
              "★★ ② `AddDamageRaw` 记 **5**（扣甲前名义伤害）"
            + "｜判据 `DamageDeltToEnemyUnit__OnDamageDelt.c:81`（`DamageInfo.damageValue(+0x18)` 在护甲扣除之前广播）"
            + "｜🧨 把 raw 与 actual 合并成一格 ⇒ 下面两条不能同时成立 ⇒ 红");
        Check(t.DamageOf(0, DamageKind.Melee), 3,
              "★★ ② `AddDamage` 记 **3**（扣甲后 actual），与 raw **各自独立**"
            + "｜🧨 AddDamageRaw 顺手改了 DamageByKind ⇒ 实得 5 ⇒ 红");

        t.AddDamageRaw(0, DamageKind.Unknown, 9);
        Check(t.DamageRawOf(0, DamageKind.Unknown), 0,
              "★★ ② `Unknown` 类**不计**（同 AddDamage 的闸）｜🧨 把 Unknown 当 Melee 兜底 ⇒ 实得 9 ⇒ 红");

        t.AddDamageRaw(2, DamageKind.Melee, 7);
        Check(t.DamageRawOf(2, DamageKind.Melee), 0,
              "★★ ② 非法座位（slot 2）**不计**｜🧨 去掉 `SeatOk` 闸 ⇒ 写坏数组 / 实得 7 ⇒ 红");

        t.AddDamageRaw(0, DamageKind.Melee, 0);
        Check(t.DamageRawOf(0, DamageKind.Melee), 5,
              "★★ ② `amount <= 0` **不计**（raw 仍是 5）｜🧨 去掉 `amount <= 0` 闸不影响；把 `<= 0` 写反 ⇒ 红");
    }

    // ==============================================================================================
    //  ③ 成就计数：三档触发被原版挡在信号外 + 能量按类型分格（账 1357 / 1373）
    // ==============================================================================================
    static void B94B3T3TallyTriggerExclusionsAndEnergy()
    {
        // ── (a) 20 / 240 / 630 三档**不记** ────────────────────────────────────────────────────
        {
            var t = new AchievementTally();
            t.AddTrigger(0, AchievementTally.TriggerWhileInPlay);            // 20
            t.AddTrigger(0, AchievementTally.TriggerWhileInPlayVariable);    // 240
            t.AddTrigger(0, AchievementTally.TriggeredSynapse);              // 630
            Check(t.TriggerOf(0, AchievementTally.TriggerWhileInPlay), 0,
                  "★★ ③ `WhileInPlay(20)` **不记**｜判据 `ChallengeLogMgr__LogTriggerAbility.c`（`param_3 != 0x14` 才发信号）"
                + "｜🧨 删掉 AddTrigger 里那三档 `return` ⇒ 实得 1 ⇒ 红");
            Check(t.TriggerOf(0, AchievementTally.TriggerWhileInPlayVariable), 0,
                  "★★ ③ `WhileInPlayVariable(240)` **不记**｜🧨 同上");
            Check(t.TriggerOf(0, AchievementTally.TriggeredSynapse), 0,
                  "★★ ③ `TriggeredSynapse(630)` **不记**｜🧨 同上（`Ach_11` 的 630 因此不数 —— 照抄原版）");

            t.AddTrigger(0, 400);   // 普通档照记
            Check(t.TriggerOf(0, 400), 1,
                  "★★ ③ 普通档（400）**照记 1**｜🧨 把「不记」写成「全都不记」⇒ 实得 0 ⇒ 红");
        }
        // ── (b) 能量按 costType 分格（1373）─────────────────────────────────────────────────────
        {
            var t = new AchievementTally();
            t.AddEnergySpent(0, 3, AchievementTally.EnergyCostCard);
            t.AddEnergySpent(0, 2, AchievementTally.EnergyCostAbility);
            Check(t.EnergySpentOf(0, AchievementTally.EnergyCostCard), 3,
                  "★★ ③ 卡费（CardCost=1）落 slot0 = **3**｜判据 `EnergySpentSignal` 四格 + `PayCardCost`/`PayActiveAbilityCostOath` 两个发起源"
                + "｜🧨 两类合并成一格 ⇒ 下面两条不能同时成立 ⇒ 红");
            Check(t.EnergySpentOf(0, AchievementTally.EnergyCostAbility), 2,
                  "★★ ③ 能力费（AbilityCost=2）落 slot1 = **2**｜🧨 同上");
            Check(t.EnergySpentTotalOf(0), 5,
                  "★★ ③ 合计 = **5**（两类相加）｜🧨 合计只取一类 ⇒ 实得 3/2 ⇒ 红");

            t.AddEnergySpent(0, 9, 5);   // 未知 costType ⇒ 不记
            Check(t.EnergySpentTotalOf(0), 5,
                  "★★ ③ 未知 `costType=5` **不记**（原版 `EnergyCostType` 只发 1/2）｜🧨 并进别的格 ⇒ 实得 14/7 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ④ 成就计数：击杀部队 / 治疗量（账 1360 / 373 的容器）
    // ==============================================================================================
    static void B94B3T4TallyTroopKilledAndHeal()
    {
        var t = new AchievementTally();
        t.AddTroopKilled(0);
        Check(t.TroopsKilledOf(0), 1,
              "★★ ④ `AddTroopKilled(0)` ⇒ `TroopsKilledOf(0)` = **1**｜判据 `TroopsKilledCount__OnTroopKilled.c`"
            + "｜🧨 记错座位 / 不累加 ⇒ 红");
        t.AddTroopKilled(2);   // 非法座位
        Check(t.TroopsKilledOf(2), 0,
              "★★ ④ 非法座位（slot 2）**不计**｜🧨 去掉 SeatOk 闸 ⇒ 红");

        t.AddHealDone(0, 4);
        Check(t.HealDoneOf(0), 4,
              "★★ ④ `AddHealDone(0,4)` ⇒ **4**（实际治疗量）｜🧨 记请求量而非净量 ⇒ 见 doc 的消费者公式，本条锁累加");
        t.AddHealDone(0, 0);
        Check(t.HealDoneOf(0), 4,
              "★★ ④ `amount <= 0` **不计**（仍 4）｜🧨 去掉闸 ⇒ 不影响；把 `<= 0` 写反 ⇒ 红");
    }

    // ==============================================================================================
    //  ⑤ BoardSlots：OriginalIndexOfCard ↔ TryGetCardInPos 往返（账 1124 · GetOwnPositionInBoard 的格号半）
    // ==============================================================================================
    static void B94B3T5BoardSlotRoundTrip()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var ps = ctx.Players[0];
        var uL = new UnitState(ctx.NewInstance(Unit("B94B3L", 1, 1, 3)), false);
        var uR = new UnitState(ctx.NewInstance(Unit("B94B3R", 1, 1, 3)), false);
        ps.Board[BoardSlots.SlotOf(BoardSlots.Left, 0)] = uL;    // 贴督军左格 ⇒ 有符号 -1
        ps.Board[BoardSlots.SlotOf(BoardSlots.Right, 0)] = uR;   // 贴督军右格 ⇒ 有符号 +1

        Check(BoardSlots.OriginalIndexOfCard(ps, uL), -1,
              "★★ ⑤ 左侧 index0 ⇒ 原版有符号格号 **-1**（`-(i+1)`）"
            + "｜判据 `MinionManager__GetOwnPositionInBoard.c`（左表命中 ⇒ `GetLeftSlotPos(i)` + 有符号 `-i-1`）"
            + "｜🧨 把符号写反 / 用 `slot - 4` 得 3 的绝对号 ⇒ 红");
        Check(BoardSlots.OriginalIndexOfCard(ps, uR), 1,
              "★★ ⑤ 右侧 index0 ⇒ 原版有符号格号 **+1**（`i+1`）｜🧨 同上");

        UnitState got;
        CheckTrue(BoardSlots.TryGetCardInPos(ps, -1, out got) && ReferenceEquals(got, uL),
                  "★★ ⑤ 往返：有符号 **-1** ⇒ 取回 **uL**（`TryGetCardInPos` 与 `OriginalIndexOfCard` 互逆）"
                + "｜🧨 两方向编码不一致（一边 `-i-1` 一边 `-i`）⇒ 取到错的卡 / false ⇒ 红");
        CheckTrue(BoardSlots.TryGetCardInPos(ps, 1, out got) && ReferenceEquals(got, uR),
                  "★★ ⑤ 往返：有符号 **+1** ⇒ 取回 **uR**｜🧨 同上");

        CheckTrue(!BoardSlots.TryGetCardInPos(ps, 0, out got),
                  "★★ ⑤ 有符号 **0**（督军格）⇒ **false**（原版 `-1 < index < 1` 那一支也当越界）"
                + "｜🧨 把 0 当成「督军格可读」⇒ 实得 true ⇒ 红");

        var uNone = new UnitState(ctx.NewInstance(Unit("B94B3None", 1, 1, 3)), false);
        Check(BoardSlots.OriginalIndexOfCard(ps, uNone), 0,
              "★★ ⑤ 不在棋盘上的卡 ⇒ **0**（= 原版「两条表都没有」）｜🧨 返回别的哨兵（如 -1）⇒ 红");
    }

    // ==============================================================================================
    //  ⑥ BoardSlots.Resolve：侧 / 下标 + 「请求侧满 ⇒ 换对侧最外」（账 1107）
    // ==============================================================================================
    static void B94B3T6BoardResolveSideIndex()
    {
        // ── (a) 空棋盘请求左 0 号格 ⇒ side=Left index=0 ─────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var ps = ctx.Players[0];
            int side, index;
            bool ok = BoardSlots.Resolve(ps, BoardSlots.SlotOf(BoardSlots.Left, 0), out side, out index);
            CheckTrue(ok, "★★ ⑥ 空棋盘请求左 0 号格 ⇒ 放得下（true）");
            Check(side, BoardSlots.Left,
                  "★★ ⑥ 落点 side = **Left**｜🧨 侧别判反 ⇒ 红");
            Check(index, 0,
                  "★★ ⑥ 落点 index = **0**（贴督军）｜🧨 IndexOf 少减 1 / 多减 1 ⇒ 红");
        }
        // ── (b) 左侧满（4 个）⇒ 请求左侧 ⇒ 换到右侧最外（index 0）──────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var ps = ctx.Players[0];
            for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                ps.Board[BoardSlots.SlotOf(BoardSlots.Left, i)] =
                    new UnitState(ctx.NewInstance(Unit("B94B3FillL" + i, 1, 1, 3)), false);
            int side, index;
            bool ok = BoardSlots.Resolve(ps, BoardSlots.SlotOf(BoardSlots.Left, 1), out side, out index);
            CheckTrue(ok, "★★ ⑥ 左侧 4 个（满）时请求左侧 ⇒ 仍放得下（换侧，不是拒绝）");
            Check(side, BoardSlots.Right,
                  "★★ ⑥ 请求侧满 ⇒ **换到对侧**（side = Right）"
                + "｜判据 `AdjustedSlot.c:64-72 / :104-107`（满侧 ⇒ 返回对侧最外）"
                + "｜🧨 满侧就 return false（旧错口径）⇒ ok 变 false / side 仍 Left ⇒ 红");
            Check(index, 0,
                  "★★ ⑥ 换到对侧时落在**对侧最外**（右 empty ⇒ index 0）｜🧨 落到对侧内侧 / 用请求下标 ⇒ 红");
        }
        // ── (c) 两侧都满 ⇒ false ──────────────────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var ps = ctx.Players[0];
            for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
            {
                ps.Board[BoardSlots.SlotOf(BoardSlots.Left, i)] =
                    new UnitState(ctx.NewInstance(Unit("B94B3FullL" + i, 1, 1, 3)), false);
                ps.Board[BoardSlots.SlotOf(BoardSlots.Right, i)] =
                    new UnitState(ctx.NewInstance(Unit("B94B3FullR" + i, 1, 1, 3)), false);
            }
            int side, index;
            bool ok = BoardSlots.Resolve(ps, BoardSlots.SlotOf(BoardSlots.Left, 0), out side, out index);
            CheckTrue(!ok,
                      "★★ ⑥ 两侧都满 ⇒ **false**（= 原版 `IsAvailableSlot` 为假 / 真的落不下）"
                    + "｜🧨 满盘仍返回 true（会让 Insert 越界）⇒ 红");
        }
    }

    // ==============================================================================================
    //  ⑦ RuleCodes：原版词条映射 TermKey + Describe（账 233 的映射一半）
    // ==============================================================================================
    static void B94B3T7RuleCodesTermMap()
    {
        Check(RuleCodes.TermKey(RuleCodes.ErrCost), "Battle/Tips/NotEnoughMana",
              "★★ ⑦ `ErrCost` ⇒ **`Battle/Tips/NotEnoughMana`**（`CanPlayCard.c:83-95`）"
            + "｜🧨 键拼错 / 映射到别的 Tips ⇒ 红");
        Check(RuleCodes.TermKey(RuleCodes.ErrNotEnoughRoom), "Battle/Tips/NotEnoughRoom",
              "★★ ⑦ `ErrNotEnoughRoom` ⇒ **`Battle/Tips/NotEnoughRoom`**（`.c:104-120`）｜🧨 红");
        Check(RuleCodes.TermKey(RuleCodes.ErrNoTargetAvailable), "Battle/Tips/NoTargetAvailable",
              "★★ ⑦ `ErrNoTargetAvailable` ⇒ **`Battle/Tips/NoTargetAvailable`**（`.c:142-156`）｜🧨 红");
        Check(RuleCodes.TermKey(RuleCodes.ErrSlot), (string)null,
              "★★ ⑦ `ErrSlot` **无词条**（⇒ null，原版没有对应 Tips）"
            + "｜🧨 硬塞一个键（会把参数非法说成别的因）⇒ 红");

        Check(RuleCodes.Describe(RuleCodes.ErrNoTargetAvailable), "这张战术卡没有可选的合法目标",
              "★★ ⑦ `Describe(18)` = 中文整句（诊断面用）｜🧨 删掉 Names 那一行 ⇒ 吐「未知错误码 18」⇒ 红");
        Check(RuleCodes.Describe(999), "未知错误码 999",
              "★★ ⑦ 未登记的码 ⇒ 兜底「未知错误码 N」｜🧨 兜底改成空串 ⇒ 静默失败 ⇒ 红");
        CheckTrue(RuleCodes.Describe(RuleCodes.ErrWaitingBlockingAction).Length > 0
                  && !RuleCodes.Describe(RuleCodes.ErrWaitingBlockingAction).StartsWith("未知"),
                  "★★ ⑦ `ErrWaitingBlockingAction(30)` **有中文名**（不吐「未知错误码 30」）"
                + "｜🧨 新码漏进 Names 表（18/19/21..30 都踩过这坑）⇒ 红");
    }

    // ==============================================================================================
    //  ⑧ 附魔闸：HasEnchantmentForTrigger（账 1042）
    // ==============================================================================================
    static void B94B3T8EnchantmentGate()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var inst = ctx.NewInstance(Unit("B94B3Ench", 1, 1, 3));
        CheckTrue(!Enchantments.HasEnchantmentForTrigger(inst, Enchantments.TriggerNameWhileInPlay),
                  "★★ ⑧ 身上**没有附魔** ⇒ `HasEnchantmentForTrigger(WhileInPlay)` = **false**"
                + "｜判据 `EntityScript__HasEnchantment.c`（`activeEnchantments(+0x110)` 空 ⇒ false）"
                + "｜🧨 把「判不了」读成 true / 空列表抛异常 ⇒ 红");
        CheckTrue(!Enchantments.HasEnchantmentForTrigger((CardInstance)null, Enchantments.TriggerNameWhileInPlay),
                  "★★ ⑧ `carrier == null` ⇒ **false**（不复制原版 NRE）｜🧨 不判 null 直接遍历 ⇒ NRE ⇒ 红");
        var u = new UnitState(inst, false);
        CheckTrue(!Enchantments.HasWhileInPlayGate(u),
                  "★★ ⑧ 闸① `HasWhileInPlayGate`（WhileInPlay ∨ WhileInPlayVariable）无附魔 ⇒ **false**"
                + "｜🧨 把两个 trigger 名写反 / 漏一个 ⇒ 本条不直接红，但接线时会静默错 ⇒ 锁「空载体恒 false」");
    }

    // ==============================================================================================
    //  ⑨ 棋盘条件闸：MeetsBoardCriteria 的 null 闸 / playerTurn 闸（账 676）
    // ==============================================================================================
    static void B94B3T9BoardCriteriaGate()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        string gap;

        bool? nullGate = CountEval.MeetsBoardCriteria(ctx, 0, null, (UnitState)null, out gap);
        CheckTrue(nullGate == true,
                  "★★ ⑨ `criteria == null` ⇒ **true**（原版 `.c:44-46`：`param_2 == 0 ⇒ return true`）"
                + "｜🧨 把 null 当「判不了」返回 null ⇒ 红");

        var gateNullSet = new CardDef.BoardGateSpec { CountSet = null };
        CheckTrue(CountEval.MeetsBoardCriteria(ctx, 0, gateNullSet, (UnitState)null, out gap) == true,
                  "★★ ⑨ `countCriteria == null` ⇒ **true**（原版 `.c:47-49`）｜🧨 红");

        // playerTurn == 10：要求「提问方就是当前行动方」⇒ owner != active ⇒ false
        int act = ctx.Active, other = 1 - act;
        var gate10 = new CardDef.BoardGateSpec
        { CountSet = new CardDef.CountSetSpec { PlayerTurn = 10 } };
        CheckTrue(CountEval.MeetsBoardCriteria(ctx, other, gate10, (UnitState)null, out gap) == false,
                  "★★ ⑨ `playerTurn == 10` 且 owner(座位 " + other + ") ≠ 当前行动方(座位 " + act + ") ⇒ **false**"
                + "｜判据 `BoardAnalysis__MeetsCriteria.c:50-64`（`pt==10 && IsPlayerTurn != card.isPlayer ⇒ return false`）"
                + "｜🧨 把 10/20 写反 / 不比较 ⇒ 实得 true/null ⇒ 红");

        // playerTurn == 20：要求「提问方不是当前行动方」⇒ owner == active ⇒ false
        var gate20 = new CardDef.BoardGateSpec
        { CountSet = new CardDef.CountSetSpec { PlayerTurn = 20 } };
        CheckTrue(CountEval.MeetsBoardCriteria(ctx, act, gate20, (UnitState)null, out gap) == false,
                  "★★ ⑨ `playerTurn == 20` 且 owner(座位 " + act + ") == 当前行动方 ⇒ **false**"
                + "（原版 `.c` 的第二段 `pt==0x14 ⇒ 相同时 return false`）｜🧨 红");
    }
}
