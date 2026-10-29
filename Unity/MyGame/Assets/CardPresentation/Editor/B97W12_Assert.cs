// B97W12_Assert.cs — 第九十七会话 · 波 1 补位 · 执行代理 `B97_W12`（2026-10-29）
//
// ============================ 它盯的是什么（账 `战斗逻辑驱动:1004`）============================
//   原版类 = `DamageDeltToEnemyUnit`（`MissionChallenge` 子类，成就 `Ach_1/2/3/4/7/53/86` 的计数源）。
//   判据（🥇 2026-10-29 逐句亲读，⛔ 别在这里抄第二份完整论证 → `资料/完全复刻/报告/B97_W12.md`）：
//     · 量的口径 = `d:/2/tools/decomp_full/DamageDeltToEnemyUnit__OnDamageDelt.c:81`
//       `MissionChallenge__UpdateProgress(param_1, *(int *)(damageInfo + 0x18), 0, 0)`
//       —— `DamageInfo.damageValue(+0x18)` = **扣甲前**（护甲在被伤害方的
//       `CardScript.ResolveDamageDealt` 里才减；`damageAbsorbed` 是**另一个 bool**，不参与算量）。
//     · 归属口径 = 同文件 `:39` 的闸 `DamageInfo__IsPlayer(...)`，而 `DamageInfo__IsPlayer.c` 全文只有
//       `return *(actingCard + 0x10 + 0x40)` = **施害卡自己的 `isPlayer`** ⇒ 记给**【攻方】**，
//       ⛔ **不是**「受害者座位取反」。差在「攻方与受害者同侧」那一档（自己打自己人）。
//
// ============================ 本件落地的两半 ============================
//   ① `Shell/ProfileData.cs:666`：结算取数从 `DamageOf`（扣甲后 `DamageByKind`）换成
//      `DamageRawOf`（扣甲前 `DamageByKindRaw`，写点 `RuleCore.ApplyDamageCore:8485`）。
//      🔴 两格**二选一**，同时读会双计 ⇒ `W12-2x` 那组断言钉的就是「取的是 raw 不是可见格」。
//   ② `RuleEngine/Core/AchievementTally.cs:150`（`AddDamage`）/ `:353`（`AddDamageRaw`）：
//      新增**可选形参** `int actingCardSeat = -1` —— 有就以它为准（= 原版 `actingCard.isPlayer`），
//      缺省 `-1` ⇒ 沿用调用方给的座位（老调用点**逐字不变**，含全部自检夹具）。
//
// ============================ ⛔ 本件**没有**做的那一半（跨文件）============================
//   消费侧真正把「攻方座位」传下来的那一句在 `RuleEngine/Core/RuleCore.cs:8977`
//   （`ctx.Tally.AddDamage(1 - _own, …)`）—— 那是别的写手的文件 ⇒ 本件只给**可粘贴补丁**（报告 §三）。
//   ⇒ 端到端归属断言（`CheckB97W12AttributionEndToEnd`）**必须等那条补丁落地之后再接线**，
//     否则它**必红**（故意的：它断的正是补丁的效果，⛔ 别为了变绿把它改成 `if (…) Check(…)` 跳过）。
//
// ============================ 灭自证（哪一处改动会让哪一条红）============================
//   · 把 `ProfileData.cs:666` 换回 `tally.DamageOf` ⇒ **`W12-2a` 红**（实得 7，期望 10）。
//   · 把 `AddDamage` 新加的那半句 `if (SeatOk(actingCardSeat)) dealerSeat = actingCardSeat;` 删掉
//     ⇒ **`W12-3b` / `W12-3c` 红**（6 点会留在座位 1 而不是 0）。
//   · 把 `AddDamage` 的 `SeatOk(actingCardSeat)` 闸去掉（非法座位也照收）⇒ **`W12-3d` 红**
//     （`actingCardSeat = 2` 会写成越界下标 / 或把 5 点记到座位 2）。
//   · 端到端那半边：把 `RuleCore.cs:8977` 的座位换回 `1 - _own` ⇒ **`W12-4b` 红**（4 点落到座位 1）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   本文件是 `public static partial class RuleEngineTest` 的分册（与同波兄弟
//   `CardPresentation/Editor/B97_W3_Assert.cs` 同形）—— 用它的 `Battle/Place/Unit/Check` 夹具，
//   ⛔ 不另抄一份（抄一份夹具 = 两份口径迟早不一致）。
//
//   宿主 `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一行**（建议紧跟在 `Step(TestEngineX2);` 之后）：
//
//   ```csharp
//               { CheckB97W12DamageRawAndAttribution(); }
//   ```
//   ⚠️ 方法名**不用 `Test`/`Report` 打头**、**故意缩进深一层** ⇒ 不会被 `VerifyRunCaseCoverage` /
//      `gen_diag_runner` 认成用例（与 `B93_G1` / `B94_F1` / `B97_W3` 既有做法同形）。
//
//   第二条（端到端）**等 `RuleCore.cs:8977` 补丁落地后**再加一行：
//
//   ```csharp
//               { CheckB97W12AttributionEndToEnd(); }
//   ```
//
//   ⚠️ `W12-2x` 要读 `ProfileData`（`Resources/profile_cosmetics.json`）⇒ 宿主必须能读到资源；
//      `RuleEngineTest.Run` 里 `TestEngineX2` 已经在同一宿主读它，所以插在那里可以。
using CardPresentation;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================================================
    //  ① 量的口径（原版：扣甲前的 `DamageInfo.damageValue`）+ 归属在容器里按 `actingCardSeat` 走
    // ==================================================================================================
    /// <summary>本件的**第一条**入口（宿主接线 → 文件头）。全部断言现在就应该是**绿**的。</summary>
    public static void CheckB97W12DamageRawAndAttribution()
    {
        Section("🆕 B97_W12：账 1004 `DamageDeltToEnemyUnit` —— 扣甲前取数（ProfileData）· 归属按攻方（AchievementTally）");
        B97W12T1RawVsVisible();
        B97W12T2RawConsumerIsProfileData();
        B97W12T3ActingCardSeatParam();
    }

    /// <summary>
    /// 一击 10 点打在 **3 护甲** 的单位上：
    /// 扣甲前 = 10（`DamageInfo.damageValue`），扣甲后 = `max(1, 10-3)` = 7（原版
    /// `CardScript.ResolveDamageDealt` 的 `Math.Max(1, damageValue - 护甲)`）。
    /// ⛔ **两格分开断言**（混着比就看不出是哪一格错了）。
    /// </summary>
    static void B97W12T1RawVsVisible()
    {
        // ---- (a) 我方（座位 0）打敌方（座位 1）：Raw 10 / 可见 7 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var armored = Place(ctx, 1, 3, Unit("W12 Armored", 1, 0, 40, "Armour 3"));
            var attacker = Place(ctx, 0, 5, Unit("W12 Attacker", 1, 3, 40));
            // ---- 前提（硬检查，⛔ 别写成 `if (前提) Check(结论)`）----
            CheckTrue(armored != null && attacker != null, "W12-1p（前提）敌方带甲单位与我方攻方都摆上场了");
            if (armored == null || attacker == null) return;
            Check(armored.Armor, 3, "W12-1p（前提）`Armour 3` 解析成护甲 3（否则下面的 7 不是「减过甲」算出来的）");
            CheckTrue(attacker.OwnerSeat == -1 || attacker.OwnerSeat == 0, "W12-1p（前提）攻方归属是座位 0（夹具摆上去时 `OwnerSeat` 允许是 `-1` 哨兵，靠棋盘扫描回落）");

            RuleCore.HurtWithSourceForTest(ctx, armored, 10, "W12 自检", actingCard: attacker, kind: DamageKind.Melee);

            Check(ctx.Tally.DamageRawOf(0, DamageKind.Melee), 10,
                  "W12-1a ★ 原版口径那格记 **10**（扣甲前 `DamageInfo.damageValue`）"
                + "｜🧨 写成记「实际扣血」⇒ 实得 7 ⇒ 红");
            Check(ctx.Tally.DamageOf(0, DamageKind.Melee), 7,
                  "W12-1b ★ 现役那格记 **7**（扣甲后 `actual` = `max(1, 10-3)`）"
                + "｜🧨 两格合并 / 把护甲也算进 raw ⇒ 两边相等 ⇒ 红");
            Check(ctx.Tally.DamageRawOf(1, DamageKind.Melee), 0,
                  "W12-1c ★ 灭自证：raw **只记在攻方**（座位 1 恒 0）｜🧨 归属取反 ⇒ 实得 10 ⇒ 红");
        }

        // ---- (b) 敌方（座位 1）打我方（座位 0）：Raw 10 / 可见 7，且都记给**敌方** ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var ours = Place(ctx, 0, 3, Unit("W12 Ours", 1, 0, 40, "Armour 3"));
            var foe = Place(ctx, 1, 5, Unit("W12 Foe", 1, 3, 40));
            CheckTrue(ours != null && foe != null, "W12-2p（前提）我方带甲单位与敌方攻方都摆上场了");
            if (ours == null || foe == null) return;
            Check(ours.Armor, 3, "W12-2p（前提）护甲解析成 3");

            RuleCore.HurtWithSourceForTest(ctx, ours, 10, "W12 自检", actingCard: foe, kind: DamageKind.Melee);

            Check(ctx.Tally.DamageRawOf(1, DamageKind.Melee), 10, "W12-2a ★ 敌方攻方打我方 ⇒ **敌方**的 raw 格 +10");
            Check(ctx.Tally.DamageOf(1, DamageKind.Melee), 7, "W12-2b ★ …同一侧可见格 +7（扣甲后）");
            Check(ctx.Tally.DamageOf(0, DamageKind.Melee), 0, "W12-2c ★ 灭自证：我方的可见格**不动**（按攻方记，⛔ 不是按受害者）");
            Check(ctx.Tally.DamageRawOf(0, DamageKind.Melee), 0, "W12-2d ★ 灭自证：我方的 raw 格也不动");
        }
    }

    /// <summary>
    /// 结算侧取数：`ProfileData.TallyDeltaOf` 走 `DamageRawOf`（扣甲前），⛔ 不是 `DamageOf`。
    /// 判据 = `DamageDeltToEnemyUnit__OnDamageDelt.c:81` 的 `UpdateProgress(damageValue)`。
    /// </summary>
    static void B97W12T2RawConsumerIsProfileData()
    {
        CheckTrue(ProfileData.Loaded, "W12-2up（前提）成就表读进来了（`Resources/profile_cosmetics.json`）");
        var a2 = ProfileData.AchievementById("Ach_2");   // `Deal Melee Damage`（`attackType=1`）
        CheckTrue(a2 != null, "W12-2up（前提）`Ach_2` 在成就表里");
        if (a2 == null) return;
        var f2 = ProfileData.FiltersOf(a2);
        CheckTrue(f2 != null, "W12-2up（前提）`Ach_2` 的过滤位解出来了（`attackType` 在 `DamageKindOf` 里映射成 `Melee`）");
        if (f2 == null) return;

        var t = new AchievementTally();
        t.AddDamageRaw(0, DamageKind.Melee, 10);   // 原版口径（扣甲前）
        t.AddDamage(0, DamageKind.Melee, 7);       // 现役口径（扣甲后）—— 同一次伤害的另一个量

        int delta = ProfileData.TallyDeltaOf(a2, t, 0, 0, 0, 0, GameMode.OfflinePractice, null, false);
        Check(delta, 10,
              "W12-2e ★ 结算取的是 **10**（扣甲前 `DamageRawOf`）"
            + "｜🧨 把 `ProfileData.cs:666` 换回 `tally.DamageOf` ⇒ 实得 7 ⇒ 红（这就是账 1004 的那一格）");
    }

    /// <summary>
    /// 容器那半：`AddDamage` / `AddDamageRaw` 的可选形参 `actingCardSeat` ——
    /// 有就以它为准（= 原版 `actingCard.isPlayer`），缺省 `-1` 沿用调用方座位（老调用点不变）。
    /// </summary>
    static void B97W12T3ActingCardSeatParam()
    {
        // ---- (a) 缺省形状：第 4 个形参不传 ⇒ 与老调用点**逐字同义** ----
        {
            var t = new AchievementTally();
            t.AddDamage(0, DamageKind.Melee, 4);            // 老调用点形状（3 参）
            t.AddDamageRaw(0, DamageKind.Melee, 6);
            Check(t.DamageOf(0, DamageKind.Melee), 4,
                  "W12-3a ★ 3 参调用 ⇒ 记在调用方给的座位（老调用点逐字不变）｜🧨 把缺省值写成 0 ⇒ 座位 1 的调用会被改记到 0 ⇒ 红");
            Check(t.DamageRawOf(0, DamageKind.Melee), 6, "W12-3a ★ `AddDamageRaw` 同上");
        }

        // ---- (b) 传了 `actingCardSeat` ⇒ **以它为准**（原版 `actingCard.isPlayer`）----
        {
            var t = new AchievementTally();
            t.AddDamage(1, DamageKind.Melee, 6, 0);         // 调用方手里只有「受害者取反 = 1」，但知道攻方是 0
            t.AddDamageRaw(1, DamageKind.Melee, 9, 0);
            Check(t.DamageOf(0, DamageKind.Melee), 6,
                  "W12-3b ★ 传了攻方座位（0）⇒ 6 点记给**攻方**，⛔ 不是调用方那个「取反」的 1"
                + "｜🧨 删掉 `if (SeatOk(actingCardSeat)) dealerSeat = actingCardSeat;` ⇒ 实得 0（6 落在座位 1）⇒ 红");
            Check(t.DamageOf(1, DamageKind.Melee), 0,
                  "W12-3c ★ 灭自证：座位 1 **一格都不许涨**｜🧨 同上 ⇒ 实得 6 ⇒ 红");
            Check(t.DamageRawOf(0, DamageKind.Melee), 9, "W12-3b ★ `AddDamageRaw` 同一条口径（raw 的 9 记给攻方）");
            Check(t.DamageRawOf(1, DamageKind.Melee), 0, "W12-3c ★ 灭自证：raw 那格的座位 1 同样是 0");
        }

        // ---- (c) 非法 `actingCardSeat` ⇒ **忽略它**，仍用调用方座位（⛔ 别写坏数组）----
        {
            var t = new AchievementTally();
            t.AddDamage(0, DamageKind.Melee, 5, 2);         // 2 不是合法座位
            t.AddDamageRaw(0, DamageKind.Melee, 5, -7);
            Check(t.DamageOf(0, DamageKind.Melee), 5,
                  "W12-3d ★ 非法 `actingCardSeat`（2）被忽略 ⇒ 仍记在调用方给的座位 0"
                + "｜🧨 去掉 `SeatOk(actingCardSeat)` 闸 ⇒ 越界下标（`2*9+k`）/ 抛异常 ⇒ 红");
            Check(t.DamageRawOf(0, DamageKind.Melee), 5, "W12-3d ★ `AddDamageRaw` 同上（`-7` 被忽略）");
        }
    }

    // ==================================================================================================
    //  ② 端到端归属（⛔ **要等 `RuleCore.cs:8977` 的补丁落地才能接线**，见文件头）
    // ==================================================================================================
    /// <summary>
    /// **攻方与受害者同侧**（自家单位打自家单位）—— 这是唯一能把「按攻方」与「受害者取反」
    /// 分开的夹具：取反写法会把 4 点记到**对面**，原版（`actingCard.isPlayer`）记给**攻方**。
    /// <para>🔴 现读 `RuleCore.cs:8977` 仍是 `AddDamage(1 - _own, …)` ⇒ 本方法**现在必红**。
    /// 补丁（报告 §三）落地后再把宿主那一行接上。</para>
    /// </summary>
    public static void CheckB97W12AttributionEndToEnd()
    {
        Section("🆕 B97_W12：账 1004 —— 归属端到端（攻方与受害者同侧 · 需 `RuleCore.cs:8977` 补丁）");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var myAtk = Place(ctx, 0, 3, Unit("W12 My Atk", 1, 3, 40));
        var myVic = Place(ctx, 0, 6, Unit("W12 My Vic", 1, 0, 40));
        CheckTrue(myAtk != null && myVic != null, "W12-4p（前提）我方攻方与被我方打的自家单位都摆上场了（同侧）");
        if (myAtk == null || myVic == null) return;

        RuleCore.HurtWithSourceForTest(ctx, myVic, 4, "W12 自检", actingCard: myAtk, kind: DamageKind.Spell);

        Check(ctx.Tally.DamageRawOf(0, DamageKind.Spell), 4,
              "W12-4a 扣甲前那格记给**攻方（我方，座位 0）**（这一条今天已经对 —— 写点 `RuleCore.cs:8485` 早就在用攻方座位）");
        Check(ctx.Tally.DamageRawOf(1, DamageKind.Spell), 0, "W12-4a 灭自证：座位 1 的 raw 恒 0");
        Check(ctx.Tally.DamageOf(0, DamageKind.Spell), 4,
              "W12-4b ★ 可见格也记给**攻方**（原版 `DamageInfo.IsPlayer()` = `actingCard.isPlayer`）"
            + "｜🧨 `RuleCore.cs:8977` 换回 `1 - _own` ⇒ 实得 0（4 落到座位 1）⇒ 红 —— 这就是那条补丁要修的东西");
        Check(ctx.Tally.DamageOf(1, DamageKind.Spell), 0,
              "W12-4c ★ 灭自证：对面那一格**一格都不许涨**（取反写法会在这里得 4）");
    }
}
