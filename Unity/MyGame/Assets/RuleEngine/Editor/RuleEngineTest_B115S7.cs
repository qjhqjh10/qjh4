// RuleEngineTest_B115S7.cs — 执行写手 `B115_S7`（2026-11-01 · 第一百一十五会话）
//
// ============================ 这件活是什么 ============================
// ENG-07 里判为「真缺口」的 `transient`（`DefinedTrait.transient = 455`）落了地
// （`RuleCore.cs:2312` 同物理行内联块），本文件给它**持久断言**。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · 第一权威：`d:/2/tools/decomp_full/CardScript__OnTurnStart.c:255-261`
//       `cardState ∈ {2, 0xf, 3, 0x11}`（在场）∧ `HasCurrentTrait(0x1c7 = transient)`
//       ⇒ `BattleManager.DestroyUnit(this, this, /*priority*/ 1, /*deathType*/ 0x14 = ability(20))`
//     ⚠️ 该支**没有侧别守卫**（不像 `:106` dropPod / `:113` stealth / `:120` stun / `:125` blind
//        那样查 `param_2 == *(char*)(card + 0x40)`），而 `OnTurnStart` 由
//        `BattleManagerSupport__BroadcastTurnStart` 对**全场**逐张调 ⇒ **双方都扫**。
//   · 位置：`:137+`（TurnStart 触发段）→ **`:257`（本跳）** → `:263`（Landing）
//     ⇒ 我们放在 `RuleCore.BeginTurn` 的 `ResolveAtTurn(ctx, "turn_start")` **之后**。
//   · 现读池内张数：`cards_engine.json` v47 / 1129 张，`keywords` + `cardTraits` 两列扫 `transient`
//     **0 命中** ⇒ 只能用夹具（真卡一进来这段就会自然被覆盖）。
//
// ============================ 🔴 灭自证（本组为什么这样写）============================
//   「带 `transient` 的单位回合开始后离场」这一条**单独**是过不了鉴别力的：
//   一个「无条件清空双方棋盘」的实现也能让它绿。所以本组同时钉三件事：
//     ① **对照**：不带 `transient` 的单位**仍在场**（B2）—— 清场式实现过不了它；
//     ② **两条读路**：`cardTraits` 列（CardDef.HasDefaultTrait 回退路，B1）**和**
//        运行时 `_keywords`（u.Has 路，B4）—— 只实现一条的实现过不了另一条；
//     ③ **侧别**：非行动方那一侧带 `transient` 的**也要离场**（B3）—— 只扫 `p.Board` 的实现过不了它。
//   ⇒ ①②③ 与「恒绿」结构上不可能同时成立。
//
// ============================ 宿主接线 ============================
//   `RuleEngineTest.cs` 的 `Run()` 里 `{ Step(CheckB96CBEngine); }` 那一行**同一物理行**追加
//   `Guard("CheckB115S7Transient", CheckB115S7Transient);`（行数零变化）。
//   ⚠️ 入口名**不以 `Test` / `Report` 打头** —— `VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>棋盘扫描（本文件私有，⛔ 与别处的同名助手合并会改到别人的夹具口径）。</summary>
    static UnitState BoardUnitB115S7(BattleContext ctx, int side, string name)
    {
        var b = ctx.Players[side].Board;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (b[s] != null && b[s].Name == name) return b[s];
        return null;
    }

    /// <summary>ENG-07 `transient`(455)：回合开始摧毁（原版 `CardScript__OnTurnStart.c:255-261`）。
    /// <para>夹具全走真实的 `NewBattle` + `DeployFree` 路径（不手拼 `BattleContext`），与同文件其它断言一致。</para></summary>
    public static void CheckB115S7Transient()
    {
        Section("🆕 B115_S7 · ENG-07 `transient`(455)：回合开始摧毁");

        // ---- 四张夹具：A=cardTraits 列 / B=运行时关键词 / C=对照 / E=非行动方 ----
        var defA = Unit("B115S7_traitData", 1, 1, 5);
        defA.AttachCardTraits(new[] { "transient" });          // 原版 `RawCardScript.CardTraits(+0x230)` 的忠实那一列
        var defB = Unit("B115S7_runtimeKw", 1, 1, 5);          // 关键词走运行时 `AddKeyword`（见下）
        var defC = Unit("B115S7_control", 1, 1, 5);            // 灭自证：什么都不带
        var defE = Unit("B115S7_enemySide", 1, 1, 5);
        defE.AttachCardTraits(new[] { "transient" });

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active, foe = 1 - me;

        int sA, sB, sC, sE;
        bool okA = RuleCore.DeployFree(ctx, me, defA, out sA);
        bool okB = RuleCore.DeployFree(ctx, me, defB, out sB);
        bool okC = RuleCore.DeployFree(ctx, me, defC, out sC);
        bool okE = RuleCore.DeployFree(ctx, foe, defE, out sE);

        // 前提一律写成**硬 Check**（⛔ 不用 `if (x != null) Check(...)` —— 前提不成立时那种写法会静默跳过）
        CheckTrue(okA && okB && okC && okE,
                  "A1 前提：四张夹具都部署成功（任意一张失败 ⇒ 本段后面的断言没有意义）");

        var uB = BoardUnitB115S7(ctx, me, "B115S7_runtimeKw");
        CheckTrue(uB != null, "A2 前提：运行时关键词那张在场上（拿不到就说明部署这条链变了）");
        if (uB != null) uB.AddKeyword("transient", 1);         // `keywords` 列的真值今天进不来（`CardDef.Prefixes` 未登记，账 `B-319`）

        CheckTrue(BoardUnitB115S7(ctx, me, "B115S7_traitData") != null
                  && BoardUnitB115S7(ctx, me, "B115S7_runtimeKw") != null
                  && BoardUnitB115S7(ctx, me, "B115S7_control") != null
                  && BoardUnitB115S7(ctx, foe, "B115S7_enemySide") != null,
                  "A3 前提：回合开始前四张**都在场**");

        // ---- 触发一次回合开始 ----
        RuleCore.BeginTurn(ctx);

        // B1：cardTraits 列那条读路
        CheckTrue(BoardUnitB115S7(ctx, me, "B115S7_traitData") == null,
                  "B1 带 `transient`（`cardTraits` 列）的单位在回合开始被摧毁"
                  + " —— 把 `RuleCore.BeginTurn` 里那块删掉 ⇒ 本条红");

        // B2：灭自证 —— 不带的不许受影响
        CheckTrue(BoardUnitB115S7(ctx, me, "B115S7_control") != null,
                  "B2 🧨 不带 `transient` 的单位**仍在场**（灭自证：一个「无条件清场」的实现过不了本条）");

        // B3：原版 `:257` 没有侧别守卫 ⇒ 非行动方那一侧也要被摧毁
        CheckTrue(BoardUnitB115S7(ctx, foe, "B115S7_enemySide") == null,
                  "B3 非行动方那一侧带 `transient` 的**也离场**（原版 `:257` 无 `param_2 == isPlayer` 守卫）"
                  + " —— 把扫场改成只扫 `p.Board` ⇒ 本条红");

        // B4：`u.Has(...)` 那条读路（运行时关键词）
        CheckTrue(BoardUnitB115S7(ctx, me, "B115S7_runtimeKw") == null,
                  "B4 运行时挂上去的 `transient` 同样生效（`UnitState._keywords` 那条读路）"
                  + " —— 只留 `HasDefaultTrait` 那一半 ⇒ 本条红");
    }
}
