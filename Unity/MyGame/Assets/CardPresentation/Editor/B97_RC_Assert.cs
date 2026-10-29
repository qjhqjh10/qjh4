// B97_RC_Assert.cs — 执行代理 `B97_RC` 的四件（2026-10-29 · 第九十六会话）
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与
// `RuleEngineTest_EngineB95WBD.cs` 同一种形状）—— `Editor/RuleEngineTest.cs` 本批**不在白名单**
// ⇒ 测试体全写在这里，`RuleEngineTest.cs` 只需在 `Run()` 里加一行
//      Step(CheckB97RCGaps);
// 建议插在 `Step(CheckB93G1PlayedCardTail);`（`RuleEngineTest.cs` 里那一行）**之后**。
// ⚠️ 本方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 当成「有 Test* 但没 Step」判红。
//
// ============================ 它盯的是哪几条 ============================
//   A 账 `战斗逻辑驱动:500` —— `ResolveCardAutoSummoned` 的 **ambush ⇒ 面朝下**那一跳
//      判据 = `decomp_full/BattleManager._ResolveCardAutoSummoned_d__511__MoveNext.c:166-170`
//             （另有同形两处：`_ResolveSummonUnit_d__510__MoveNext.c:95`、`_ResolvePlayCardFromHand…:197`）
//      ⇒ 我们的落点 = `RuleCore.DeployFreeCore`（`RuleCore.cs` 里那句 `PlaysFaceDown(card)`）
//   B 账 `战斗逻辑驱动:476` —— `RecordAttackPendingDamage` 的 **Strike 闸用「待结算量」**
//      判据 = `CardScript__ResolveUnitAttacked.c:99-101` 的 `!EnoughPendingDamageToDie(attacker)`
//      ⇒ 我们的落点 = `RuleCore.DeclareAttack` 的 `attackerDiesFromPending`（改成 `WouldKillByEntries`）
//   C 账 `战斗逻辑驱动:1248` —— `RawCardScript.GetValidTargets` 的**枚举次序**（进场追加序）
//      外加顺手发现：`BattleManager.GetAvailableAttackTargets` 是**倒序**（`.c:23-33`）
//   D 账 `战斗逻辑驱动:816` —— `CardScript.OnTurnSetup` 的**逐条入队**（不是整批 `RevertBuffs`）
//
// ============================ 🔴 灭自证（每条带 🧨）============================
//    ① **前提断言与结论断言并列写**，⛔ 不写 `if (前提) Check(结论)`；
//    ② 期望值一律**字面量**，⛔ 不从被测实现反算；
//    ③ 会「两边一起改回去仍然绿」的地方，补一条**结构上不可能同时满足**的断言（B 的源码那条）。
using System.IO;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口。</summary>
    public static void CheckB97RCGaps()
    {
        Section("🆕 B97_RC：伏击面朝下(500) · Strike 闸(476) · 目标表次序(1248) · 回合开始逐条入队(816)");
        B97RCAutoSummonAmbush();
        B97RCStrikeGateUsesPendingDamage();
        B97RCTargetEnumerationOrder();
        B97RCTurnSetupPerEffect();
    }

    // ------------------------------------------------------------ A：自动召唤的 `ambush ⇒ FaceDown`
    /// <summary>
    /// `RuleCore.DeployFree(..., fromHand: true)`（= 原版 `_ResolveCardAutoSummoned`）与
    /// `DeploySource.Summon`（= `_ResolveSummonUnit`）**都要**让带 `ambush` 的单位面朝下；
    /// `DeploySource.Reanimate`（= `ResolveTransformFromRemnant`）**不要**（那条链没有 `SetAmbush`）。
    /// </summary>
    static void B97RCAutoSummonAmbush()
    {
        var amb = Unit("B97RCAmb", 1, 1, 2, "Ambush 3");
        var plain = Unit("B97RCPlain", 1, 1, 2);
        Check(amb.Has(KeywordTable.Ambush), true,
            "★（前提）伏击卡的卡面关键词里**确实有** `ambush`"
          + "｜🧨 关键词写法改成别的词 ⇒ 红（那样下面三条就没有判别力）");
        Check(plain.Has(KeywordTable.Ambush), false,
            "★（前提）普通卡**没有** `ambush` ｜🧨 夹具改成两张都带 ⇒ 红");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;
        CheckTrue(ctx != null && !ctx.IsOver,
            "★（前提）这一局还没结束（`DeployFree` 在对局结束后不落位 ⇒ 结论会假绿）"
          + "｜🧨 夹具改成已判胜的 ctx ⇒ 红");

        // ① 从手牌免费打出（原版 `_d__511`）
        int s1;
        bool ok1 = RuleCore.DeployFree(ctx, me, ctx.NewInstance(amb), out s1, fromHand: true);
        CheckTrue(ok1 && s1 >= 0,
            "★（前提）伏击卡从手牌免费打出**成功落位** ｜🧨 棋盘没空格 ⇒ 红");
        var u1 = (ok1 && s1 >= 0) ? ctx.Players[me].Board[s1] : null;
        CheckTrue(u1 != null,
            "★（前提）落位那一格上**真有单位** ｜🧨 `DeployFreeCore` 没写棋盘 ⇒ 红");
        CheckTrue(u1 != null && u1.FaceDown,
            "★★ `fromHand: true` + `ambush` ⇒ **面朝下**（原版 `_d__511:166-170` 的 `HasCurrentTrait(0x4b0) ⇒ SetAmbush(true)`）"
          + "｜🧨 把 `DeployFreeCore` 里那句 `PlaysFaceDown(card)` 删掉 ⇒ 红 —— 那正是本条修前的状态");

        // ② 同一入口、无 ambush ⇒ 不面朝下（防「无条件 FaceDown」）
        int s2;
        bool ok2 = RuleCore.DeployFree(ctx, me, ctx.NewInstance(plain), out s2, fromHand: true);
        CheckTrue(ok2 && s2 >= 0, "★（前提）普通卡从手牌免费打出**成功落位** ｜🧨 满场 ⇒ 红");
        var u2 = (ok2 && s2 >= 0) ? ctx.Players[me].Board[s2] : null;
        CheckTrue(u2 != null, "★（前提）那一格上真有单位 ｜🧨 同上 ⇒ 红");
        CheckTrue(u2 != null && !u2.FaceDown,
            "★★ 同一入口、卡面**没有** `ambush` ⇒ **不面朝下**（原版也是 `HasCurrentTrait` 为假才跳过）"
          + "｜🧨 把闸写成恒真 / 拿「所有免费部署」当判据 ⇒ 红");

        // ③ 再造档 ⇒ 不面朝下（原版 `ResolveTransformFromRemnant` 的回调清单里没有 `SetAmbush`）
        int s3;
        bool ok3 = RuleCore.DeployFree(ctx, me, amb, out s3, deploySource: DeploySource.Reanimate);
        CheckTrue(ok3 && s3 >= 0, "★（前提）再造档免费部署**成功落位** ｜🧨 满场 ⇒ 红");
        var u3 = (ok3 && s3 >= 0) ? ctx.Players[me].Board[s3] : null;
        CheckTrue(u3 != null, "★（前提）那一格上真有单位 ｜🧨 同上 ⇒ 红");
        CheckTrue(u3 != null && !u3.FaceDown,
            "★★ 再造（`DeploySource.Reanimate`）⇒ **不面朝下**"
          + "（原版 `BattleManager.__c__DisplayClass555_0.___ResolveTransformFromRemnant_b__0.c:7-31` 的清单里没有 `SetAmbush`，"
          + "见 `DeployFreeCore` 的 `reanimated` 段）"
          + "｜🧨 把 `deploySource != DeploySource.Reanimate` 那道闸去掉 ⇒ 红");
    }

    // ------------------------------------------------------------ B：Strike 闸用「待结算量」
    /// <summary>
    /// 判别式 = **幸存者**：`DamageAfterReduction(survivor, 5) >= Health(3)` 为真（旧口径判「必死」），
    /// 而 `WouldKillByEntries(survivor, [5])` 为假（幸存者吃掉这一击）。
    /// 两条一起断 ⇒ 证明「换公式」不是同义改写。
    /// </summary>
    static void B97RCStrikeGateUsesPendingDamage()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int foe = 1 - ctx.Active;
        var u = Place(ctx, foe, 2, Unit("B97RCSurv", 1, 1, 3, "Survivor 1"), exhausted: true);
        Check(u.Health, 3, "★（前提）夹具单位 **3 血** ｜🧨 血量改了 ⇒ 下面两条的判别式失效 ⇒ 红");
        Check(u.Survivor, 1,
            "★（前提）`survivor` 读出来是 **1** ｜🧨 关键词写法换了（`Survivor` 的大小写/数值格式）⇒ 读不到 ⇒ 红");

        CheckTrue(RuleCore.DamageAfterReduction(u, 5) >= u.Health,
            "★（前提）**旧口径**（`DamageAfterReduction >= Health`）把这一记判成「会死」—— 正是改前误判的那一格"
          + "｜🧨 伤害/血量/关键词改了 ⇒ 红");
        CheckTrue(!RuleCore.WouldKillByEntries(u, new[] { 5 }),
            "★★ **新口径**（`WouldKillByEntries` ≈ 原版 `EnoughPendingDamageToDie(attacker)`）判**不会死** —— 幸存者吃掉这一击"
          + "｜🧨 把 Strike 闸改回 `DamageAfterReduction(attacker, counterAtk) >= attacker.Health` ⇒ 静默少触发一次 Strike（账 476 · 结构差 #30）");

        // 结构断言：调用点真的换了（行为那半在 `DeclareAttack` 的私有体里，没有纯函数入口；
        // 这条**只证明「那一句还在」**，行为那半由上面两条 + 上面那条红色文案共同兜住）
        string src = B97RCSourceOf("RuleEngine/Core/RuleCore.cs");
        CheckTrue(src != null,
            "★（前提）读得到 `RuleEngine/Core/RuleCore.cs` 的源码文本（读不到 ⇒ 下一条红，⛔ 不静默跳过）"
          + "｜🧨 文件被挪走 / 改了名 ⇒ 红");
        CheckTrue(src != null && src.Contains("WouldKillByEntries(attacker, new[] { counterAtk })"),
            "★★ `RuleCore` 里 Strike 那道闸**这一句**用的是 `WouldKillByEntries(attacker, …)`"
          + "｜🧨 有人改回 `DamageAfterReduction(attacker, counterAtk) >= attacker.Health` ⇒ 红");
    }

    // ------------------------------------------------------------ C：目标表的两条枚举次序
    /// <summary>
    /// 判别式 = **格号序 ≠ 进场序**：先摆 8 号格、再摆 2 号格 ⇒ 进场序 `[8, 2]`、格号序 `[2, 8]`。
    /// `ValidTargetSlots`（= `RawCardScript.GetValidTargets`，**正序**）⇒ `[8, 2]`；
    /// `AvailableAttackTargetsDesc`（= `GetAvailableAttackTargets`，**倒序**）⇒ `[2, 8]`。
    /// </summary>
    static void B97RCTargetEnumerationOrder()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active, foe = 1 - ctx.Active;
        const int atkSlot = 5;
        var atk = Place(ctx, me, atkSlot, Unit("B97RCAtk", 1, 2, 5));
        var t8 = Place(ctx, foe, 8, Unit("B97RCT8", 1, 0, 5));
        var t2 = Place(ctx, foe, 2, Unit("B97RCT2", 1, 0, 5));

        CheckTrue(atk != null && t8 != null && t2 != null, "★（前提）三个单位都摆上了 ｜🧨 摆位失效 ⇒ 红");
        CheckTrue(t8.PlayOrder < t2.PlayOrder,
            "★（前提）**8 号格上那个先进场**（`PlayOrder` 更小）⇒ 进场序 = `[8, 2]`，与格号序 `[2, 8]` **不同**"
          + "｜🧨 把两次 `Place` 的顺序对调 ⇒ 判别式失效 ⇒ 红");

        // 🔴 **敌人 4 号格上永远是督军**（`PlayerState.cs:68`）⇒ 不处理的话它也会进目标表，
        //    结果集就成了 3 个、次序断言随之失真。原版有现成的一道闸能把督军挡掉：
        //    「攻击者带召唤病 ∧ 不带 `fast` ⇒ 打不了敌方督军」（`RuleCore.cs:5622`，判据 `IsValidAttackTarget.c:199-202`）。
        //    ⇒ 夹具把攻击者标成「本回合刚上场」，目标表就只剩那两个兵。
        atk.SummonSickness = true;
        CheckTrue(ctx.Players[foe].Board[BoardSpec.WarlordSlot] != null,
            "★（前提）敌人 4 号格上**确实是督军**（所以上面那句 `SummonSickness = true` 是必须的，不是装饰）"
          + "｜🧨 开局摆法改了（督军不上棋盘）⇒ 红（那时应把下面两条的期望权重算）");

        var asc = RuleCore.ValidTargetSlots(ctx, me, atkSlot, ranged: false);
        Check(asc.Count, 2,
            "★（前提）目标表里**只有那两个兵**（督军已被召唤病闸挡掉）"
          + "｜🧨 出了隐身/别的禁攻关键词、或督军闸失效 ⇒ 红（那时次序无从比较）");
        if (asc.Count == 2)
        {
            Check(asc[0].slot, 8,
                "★★ `ValidTargetSlots` 的第一项是 **8 号格**（= 先进场的那个）—— 原版 `RawCardScript__GetValidTargets.c:26-45` 按 `unitsInPlay` 进场追加序"
              + "｜🧨 改回「敌方格号升序」⇒ 首项是 2 ⇒ 红（`ResolveBerzerkAttacks` 的 `sc > bestScore` 这类平手判定会跟着变）");
            Check(asc[1].slot, 2,
                "★★ 第二项是 **2 号格**（后进场的）｜🧨 同上 ⇒ 红");
        }

        var desc = RuleCore.AvailableAttackTargetsDesc(ctx, me, atkSlot, ranged: false);
        Check(desc.Count, 2,
            "★（前提）倒序口也拿到两个目标（集合必须与正序口**相同**）｜🧨 集合不同 = 两处判据漂了 ⇒ 红");
        if (desc.Count == 2)
        {
            Check(desc[0].slot, 2,
                "★★ `AvailableAttackTargetsDesc` 的第一项是 **2 号格** —— 原版 `BattleManager__GetAvailableAttackTargets.c:23-33` 是**倒序**枚举（`iVar1 = count; iVar1--`）"
              + "｜🧨 把它写成与 `ValidTargetSlots` 同一个序 ⇒ 首项是 8 ⇒ 红（两个原版函数本来就不是同一个轴）");
            Check(desc[1].slot, 8,
                "★★ 第二项是 **8 号格**（倒序的第二个）｜🧨 同上 ⇒ 红");
        }
    }

    // ------------------------------------------------------------ D：回合开始的「逐条入队」
    /// <summary>
    /// 四档各一条 buff：只有「施放者==本回合方 ∧ `untilMyNextTurn`」（原版 `+0x33`）与
    /// 「施放者!=本回合方 ∧ `untilOpponentNextTurn`」（`+0x34`）两档到期入队；
    /// 且**入队后那条 buff 当场仍在**（= 逐条入队，不是整批撤）。
    /// </summary>
    static void B97RCTurnSetupPerEffect()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int seat = ctx.Active, other = 1 - seat;
        var a = Place(ctx, seat, 2, Unit("B97RCTA", 1, 1, 5));
        var b = Place(ctx, seat, 3, Unit("B97RCTB", 1, 1, 5));
        var c = Place(ctx, other, 2, Unit("B97RCTC", 1, 1, 5));
        var d = Place(ctx, seat, 6, Unit("B97RCTD", 1, 1, 5));

        a.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = seat, UntilMyNextTurn = true });       // 到期（+0x33）
        b.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = other, UntilMyNextTurn = true });      // 不到期（施放者不是本回合方）
        c.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = other, UntilOpponentNextTurn = true }); // 到期（+0x34）—— 🔴 2026-10-29 订正（`B97_RC2` 现核）：谓词是 `UntilOpponentNextTurn ⇒ Owner != turnPlayer`，这里本回合方是 `seat`，所以 `Owner` 必须是 `other`；写 `seat` 会让它**不到期**、`queued` 变 1（原断言 2 会红）
        d.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = seat });                               // 回合末到期 ⇒ 不归本相位

        Check(a.TempBuffs.Count, 1, "★（前提）A 身上有 1 条 buff ｜🧨 `AddTempBuff` 没生效 ⇒ 红");
        Check(c.TempBuffs.Count, 1, "★（前提）C 身上有 1 条 buff ｜🧨 同上 ⇒ 红");

        int queued = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, seat);
        Check(queued, 2,
            "★★ 只有「施放者==本回合方 ∧ `untilMyNextTurn`」与「施放者!=本回合方 ∧ `untilOpponentNextTurn`」**两档**入队"
          + "（原版 `CardScript__OnTurnSetup.c:21-102` 的 `+0x33` / `+0x34`）"
          + "｜🧨 谓词抄错（漏一档 / 两档都收 / 把回合末那档也收进来）⇒ 实得 1 或 3 ⇒ 红");
        Check(a.TempBuffs.Count, 1,
            "★★ 到期的那条 buff **当场还在** —— 本相位只是**入队**一条摘除动作（lane = `Instant`，原版 `globalVars+0xb8`）"
          + "｜🧨 改回 `UnitState.RevertBuffs(false, seat)` 整批撤 ⇒ 实得 0 ⇒ 红（那正是结构差 #62 要消掉的形状）");
        Check(c.TempBuffs.Count, 1,
            "★★ 同上（`+0x34` 那一档）｜🧨 同上 ⇒ 红");
        Check(b.TempBuffs.Count, 1,
            "★ 施放者不是本回合方的那条**原样留着** ｜🧨 谓词放宽成「凡 `untilMyNextTurn` 都收」⇒ 实得 0 ⇒ 红");
        Check(d.TempBuffs.Count, 1,
            "★ 「回合末到期」那档**不归回合开始相位** ｜🧨 把 `due` 判据抄成 `atTurnEnd: true` 那一式 ⇒ 实得 0 ⇒ 红");
    }

    // ------------------------------------------------------------ 共用：读源码（结构断言用）
    /// <summary>读 `Assets/` 下的源码文本；读不到 ⇒ `null`（断言会红，但**不抛**）。</summary>
    static string B97RCSourceOf(string assetsRelativePath)
    {
        try
        {
            string full = Path.Combine(UnityEngine.Application.dataPath,
                                       assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}
