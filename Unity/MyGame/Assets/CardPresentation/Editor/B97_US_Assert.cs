// B97_US_Assert.cs — 第九十七会话 · 执行代理 `B97_US`
//
// ============================ 它盯的是什么 ============================
// 账 `战斗逻辑驱动.tsv:521`（`BattleManager.ResolveJamUnit(BattleAction)`）的 ② 那一格 ——
// `UnitState.RemoveAllAppliedEffectsForJam` 原版有、我们缺的两跳（判据 → `资料/完全复刻/判据/B97_可推A七条_审查.md` §一·5；
// 逐条证据 → `资料/完全复刻/报告/B97_US.md`）：
//   · **①`permanentBuff(+0x74)` 早退** —— 原版 `ReactToUnitJammed` 是**逐条** `RemoveEffect(e, fromJam: true)`
//     （`decomp_full/CardScript__ReactToUnitJammed.c:59-69`：倒序扫 `activeEffects(+0x108)`、`:67` 调它），
//     而 `RemoveEffect` 第一句就是 `if (effect.permanentBuff(+0x74) != 0) return;`
//     （`decomp_full/CardScript__RemoveEffect.c:43-45`；字段名/偏移 = `dump.cs:119100`，
//      Tooltip 原文 `Can't be jammed`）⇒ **带 permanent 的那条被 jam 时摘不掉**。
//     我们原来是 `_statMods.Clear()` 一把全清 ⇒ 会把它一起摘掉。落点 = `UnitState.StatMod.Permanent`
//     ＋ `UnitState.AddStatMod(..., permanent:)` ＋ ④ 的倒序逐条摘。
//   · **② 清完修饰要夹血** —— 原版每摘一条 `buffType==1 addStats ∧ healthChange(+0x60)!=0` 的记录，
//     都把 `currentHealth` 夹到**新的** `currentMaxHealthProcessed`
//     （`decomp_full/CardScript__RemoveEffect.c:149-175`：`iVar8=currentHealth`、`iVar7=currentMaxHealthProcessed`、
//      `if (iVar8 < iVar7) iVar7 = iVar8;` ⇒ `min`）。我们整批摘完不夹 ⇒ 生命会**高于**新上限。
//     落点 = ④ 末尾那句 `ClampHealthToMax()`（`UnitState.cs` 的既有零件，语义「只夹、不减」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 里加**一个块**（位置 = 紧接 `{ CheckB97W1Grants(); }` 那一行之后，
// 现读 = 第 `1294` 行；插完在它下面）：
//       // 🆕 2026-10-29（调度台代接线）：`B97_US` —— 账 521 `ResolveJamUnit` ② 的 `permanentBuff` 早退 ＋ 清修饰后夹血。
//       { CheckB97USJam(); }
//   🔴 **故意不用 `Step(...)`、方法名也不带 `Test`/`Report` 前缀** —— 理由与 `B96_CR` / `B97_W1` 两份同一条：
//     `Step(...)` 会让 `_ranCases`（`RuleEngineTest.cs:1379`）记账、`VerifyRunCaseCoverage()` 会拿**反射清单**对账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · ④ 里删掉 `if (_statMods[i].Permanent) continue;`（退回 `_statMods.Clear()`）⇒ 2 段首条红（上限变 5）。
//   · ④ 里删掉末尾 `ClampHealthToMax()` ⇒ 1 段末条红（生命实得 8 > 上限 5）。
//   · 把 permanent 保护**做成全局**（连 `ResetToValuesInHand` 也保护）⇒ 4 段红（原版那里是整表 Clear）。
//   · 摘除时**漏掉非 permanent 的那条**（只摘 permanent 或整表不摘）⇒ 2 段第 2/3 条红。
//   · `n` 把 permanent 也数进去 ⇒ 2 段第 3 条红（实得 2 而不是 1）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97USJam()
    {
        Section("🆕 B97_US：账 521 —— 被 jam 时 `permanentBuff` 那条摘不掉（`RemoveEffect.c:43-45`）· 清修饰后夹血（`RemoveEffect.c:149-175`）");
        B97USJamClampsHealthToNewMax();
        B97USJamKeepsPermanentStatMod();
        B97USResetToValuesInHandStillClearsPermanent();
    }

    // ==================================================================
    //  1 · 普通（非 permanent）的 `addStats +health` 被 jam 摘掉后，**当前生命夹到新上限**
    //  判据 = `decomp_full/CardScript__RemoveEffect.c:149-175`
    //        （`currentHealth = min(currentHealth, currentMaxHealthProcessed)`）
    //  原版路径 = `CardScript__ReactToUnitJammed.c:59-69` 逐条 `RemoveEffect(…, 1)`
    // ==================================================================
    static void B97USJamClampsHealthToNewMax()
    {
        var card = Unit("B97USJC", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97USJC2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        Check(u.MaxHealth, 5, "前提：卡面 5 血 ⇒ 上限 5（期望值照 `Unit(\"B97USJC\", 1, 2, 5)` 的实参写死，不向实现反算）");
        u.AddStatMod("health", 4);
        Check(u.MaxHealth, 9, "前提：`addStats +4 血` ⇒ 上限 9");
        u.Health = 8;
        Check(u.Health, 8, "前提：当前生命 8（≤ 上限 9 ⇒ 合法；`Health` 是裸字段，⛔ 没有 setter 帮我们夹）");

        bool dead = u.ApplyJam();
        Check(dead, false, "前提：jam 本身不致死（生命 8 > 0）");
        Check(u.MaxHealth, 5, "🧨 上限随那条普通修饰一起摘掉（+4 那条不是 permanent）");
        Check(u.Health, 5,
              "🧨 生命夹到新上限（原版 `CardScript__RemoveEffect.c:149-175`；删掉 ④ 末尾那句 `ClampHealthToMax()` ⇒ 实得 8 > 上限 5）");
    }

    // ==================================================================
    //  2 · 带 `permanentBuff` 的那条 **不清**，普通那批照常摘，计数只报真摘掉的
    //  判据 = `decomp_full/CardScript__RemoveEffect.c:43-45`（第一句 `if (permanentBuff != 0) return;`）
    //        字段 = `dump.cs:119100`（`public bool permanentBuff; // 0x74`，Tooltip `Can't be jammed`）
    // ==================================================================
    static void B97USJamKeepsPermanentStatMod()
    {
        var card = Unit("B97USJP", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97USJP2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        u.AddStatMod("health", 4, permanent: true);
        u.AddStatMod("attack", 3);
        Check(u.MaxHealth, 9, "前提：permanent 的 +4 血照常投影（上限 9）");
        Check(u.CurrentMeleeAttack, 5, "前提：普通的 +3 近战照常投影（卡面 2 ⇒ 5）");

        int n = u.RemoveAllAppliedEffectsForJam();
        Check(u.MaxHealth, 9,
              "🧨 带 `permanentBuff` 的那条**不清**（原版 `RemoveEffect.c:43-45` 第一句 return；退回 `_statMods.Clear()` ⇒ 实得 5）");
        Check(u.CurrentMeleeAttack, 2, "🧨 普通那条照常摘掉（不是把整本修饰表都留下）");
        Check(n, 1, "🧨 计数只报**真摘掉**的 1 条（把 permanent 也数进去 ⇒ 实得 2）");
        Check(u.StatMods.Count, 1, "前提：表里只剩那一条（permanent）");
        Check(u.StatMods[0].Permanent, true, "前提：留下的正是 permanent 那条（不是「留错了另一条」）");
    }

    // ==================================================================
    //  3 · 反向闸：**另一条清账路 `ResetToValuesInHand` 照旧整表清**（别把 permanent 保护做成全局）
    //  判据 = `decomp_full/CardScript__ResetToValuesInHand.c:26-33` —— 它对 `activeEffects(+0x108)`
    //        直接 `List.Clear()`（写 `+0x18 = 0` + `System_Array__Clear`），**全文不看 `permanentBuff`**
    //  ⇒ 原版里「permanent」只保护 **jam 那条逐条摘**的路，不保护整表重置。
    // ==================================================================
    static void B97USResetToValuesInHandStillClearsPermanent()
    {
        var card = Unit("B97USRA", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97USRA2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        u.AddStatMod("health", 4, permanent: true);
        Check(u.MaxHealth, 9, "前提：permanent 的 +4 血照常投影（上限 9）");

        u.ResetToValuesInHand(false);
        Check(u.MaxHealth, 5,
              "🧨 `ResetToValuesInHand` 是**整表 Clear**（原版 `CardScript__ResetToValuesInHand.c:26-33` 不看 permanent）⇒ 永久那条也一起没；把 permanent 保护做成全局 ⇒ 实得 9");
    }
}
