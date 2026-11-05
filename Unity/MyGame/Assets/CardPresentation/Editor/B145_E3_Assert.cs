// B145_E3_Assert.cs — 执行代理 `B145-E3`（第一百三十一会话）· `BattleDriver.cs` 的 A-09 / A-12 / A-13 三条
//
// ============================ 它盯的是什么 ============================
//   A 组（A-09）：**敌我名牌文字色 = 纯白**，而手牌行 / 牌堆行那三处的蓝灰 `dim` **一个字没动**。
//      判据 = `Battle Arena 1.unity` 的 `PlayerNameText`(MB3747) / `EnemyNameText`(MB3834)
//             `m_fontColor = (1,1,1,1)`，材质 `_FaceColor = (1,1,1,1)`。
//      落点 = `BattleDriver.PlateInk` / `BattleDriver.DimInk`（`BuildHud` 的两处 `Hud(...)` 实参）。
//   B 组（A-12）：**伤害被压到 0 也要弹 `0`**。
//      判据 = `decomp_full/CardScript._ReceiveDamage_d__381__MoveNext.c:54`（`+0x35 = 1` 默认弹）
//             ＋ `:313`（`if (+0x35 != 0) BattleCardUI__DamageReceived(amount)`）⇒ `amount` 不参与判断。
//      落点 = `BattleDriver.ShowHitDamagePop`（`PlayHitFeel` 的唯一决策口；改前是 `if (e.Amount != 0)`）。
//   C 组（A-13）：**开攻击选择器前两道闸** —— 禁攻 trait（`cantAttack` 150 / `nonCombatant` 880）
//             ＋ 攻方 `cardState ∈ {2, 3, 17}`。
//      判据 = `decomp_full/BattleManager__CanAttackCard.c:17-43`（`.c:24/:35` 两个 trait、`:37-40` 的
//             `*(+0x228) == 2 || IsAttacking()`，而 `CardScript__get_IsAttacking.c` = `3 || 0x11`）。
//      落点 = `BattleDriver.AttackSelectorAllowed`（`OpenCommand` 里只把 `melee/ranged` 置 false）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：把 `PlateInk` 改成 `new Color(0.86f, 0.88f, 0.93f)`（= 顺手把名牌也弄蓝灰）⇒ 红。
//   · A3：把 `DimInk` 一起改白 ⇒ 红（证明「名牌白」不是靠把两档都改白换来的）。
//   · A4：`PlateInk == DimInk` ⇒ 红（两档必须**分得开**）。
//   · B1：把 `ShowHitDamagePop` 改回 `amount != 0`（旧闸）⇒ B1/B3 同时红。
//   · B2：把 `ShowHitDamagePop` 写成 `amount > 0`（自以为「0 没伤害」）⇒ B1 红。
//   · C2/C3：把 `AttackSelectorAllowed` 里的 `cantAttack` / `nonCombatant` 任一删掉 ⇒ 对应那条红。
//   · C5/C6：把 `cardState` 那一格写成 `IsInPlayStateOf(...)`（**含 15**）⇒ C6 红；删掉整格 ⇒ C4/C6 红。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   ⚠️ 宿主接线**不在本写手白名单**（`Editor/BattleScene.cs` 不可改）⇒ 待调度台在
//      `BattleScene.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//     `B145_E3_Assert.Run(); _pass += B145_E3_Assert.Pass; _fail += B145_E3_Assert.Fail;`
//   （本类在**全局命名空间**、类名 == 文件名 ⇒ 也可直接 `-executeMethod B145_E3_Assert.Run`。）
//   ⚠️ 本文件**零前提**：A/B 只读常量与纯函数；C 只建 `BattleContext` + `UnitState`（与
//      `RuleEngine/Editor/B144_R2_AI_Assert.cs` 同一套夹具），不建窗口、不碰存档、不跑表现层。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B145-E3：名牌文字色（A-09）/ 0 伤害也飘字（A-12）/ 攻击选择器两道闸（A-13）。</summary>
public static class B145_E3_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    const string Tag = "[B145E3]";

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("--- 🆕 B145-E3：BattleDriver A-09 / A-12 / A-13 ---");
        GroupA_NamePlateInk();
        GroupB_ZeroDamagePop();
        GroupC_AttackSelectorGates();
        Debug.Log("--- B145-E3 合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ============================================================ 工具
    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("F4") + " / 期望 " + want.ToString("F4") + "）");
    }

    // ============================================================ A：名牌文字色（A-09）
    static void GroupA_NamePlateInk()
    {
        Color plate = BattleDriver.PlateInk;
        Color dim = BattleDriver.DimInk;

        // 前提：两档都**不是**零色（Alpha 0 会让「白/蓝灰」都看不见 —— 那是另一档缺陷，先钉住）
        Check(plate.a > 0.5f, "A0a 前提：`PlateInk` 的 alpha = " + plate.a.ToString("F2") + "（> 0.5，不是全透明）");
        Check(dim.a > 0.5f, "A0b 前提：`DimInk` 的 alpha = " + dim.a.ToString("F2") + "（> 0.5，不是全透明）");

        // 名牌 = 纯白（原版 MB3747 / MB3834 的 `m_fontColor = (1,1,1,1)`）
        Near(plate.r, 1f, 1e-4f, "A1a ★ 名牌文字 **R = 1**（判据 `PlayerNameText`/`EnemyNameText` `m_fontColor=(1,1,1,1)`）");
        Near(plate.g, 1f, 1e-4f, "A1b ★ 名牌文字 **G = 1**");
        Near(plate.b, 1f, 1e-4f, "A1c ★ 名牌文字 **B = 1**");
        Near(plate.a, 1f, 1e-4f, "A1d ★ 名牌文字 **A = 1**");

        // `dim` 那三处（HandLabel / pileLabel / foePileLabel）**一个字没改**
        Near(dim.r, 0.86f, 1e-4f, "A2a ★ 手牌行/牌堆行的蓝灰 **R = 0.86 没动**｜🧨 顺手把它也改白 ⇒ 红");
        Near(dim.g, 0.88f, 1e-4f, "A2b ★ 蓝灰 **G = 0.88 没动**");
        Near(dim.b, 0.93f, 1e-4f, "A2c ★ 蓝灰 **B = 0.93 没动**");

        // 两档必须分得开（灭自证：两边都改白 ⇒ 这条红）
        Check(plate.r > dim.r + 0.01f || plate.g > dim.g + 0.01f || plate.b > dim.b + 0.01f,
              "A3 ★★ `PlateInk` 与 `DimInk` **不是同一个颜色**（名牌白 / 行文字蓝灰）"
            + "｜🧨 把 PlateInk 直接写成 DimInk（或把两处都改白）⇒ 红");
    }

    // ============================================================ B：0 伤害也飘字（A-12）
    static void GroupB_ZeroDamagePop()
    {
        // ★ 关键：0 也弹（原版 `+0x35` 默认 1、`amount` 不参与判断）
        Check(BattleDriver.ShowHitDamagePop(0),
              "B1 ★★ **`Amount == 0` 仍然飘字**（原版 `_ReceiveDamage…MoveNext.c:54/:313` —— `+0x35` 默认 1、"
            + "`DamageReceived(amount)` 不看 amount）｜🧨 改回 `amount != 0`（旧闸）⇒ 红");

        Check(BattleDriver.ShowHitDamagePop(7),
              "B2 正常伤害档（7）照旧飘字｜🧨 写成 `amount > 0` 只挡 0 ⇒ B1 仍红，但把整条写成恒 false ⇒ 本条红");

        // 负档 = 幸存者广播标记（调用点前面已有 `if (e.Amount < 0) return;`，这里只把规则表达式钉住）
        Check(!BattleDriver.ShowHitDamagePop(-1),
              "B3 负档（幸存者广播标记，原版 `__UseSurvivor` 不进 `_ReceiveDamage`）**不飘**"
            + "｜🧨 把决策口写成恒 `true` ⇒ 本条红");
    }

    // ============================================================ C：攻击选择器两道闸（A-13）
    //  夹具与 `RuleEngine/Editor/B144_R2_AI_Assert.cs` 同形（`CardDef` 5 参 ctor + `RuleCore.NewBattle`）。
    static CardDef Hero(string id)
        => new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, (IEnumerable<string>)null);

    static CardDef Unit(string id, int cost, int atk, int hp)
        => new CardDef(id, id, "unit", "", "common", "Test", cost, atk, hp, 0, (IEnumerable<string>)null);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B145E3Hero0") };
        var d1 = new List<CardDef> { Hero("B145E3Hero1") };
        for (int i = 0; i < 10; i++)
        {
            d0.Add(Unit("B145E3Filler0_" + i, 1, 0, 1));
            d1.Add(Unit("B145E3Filler1_" + i, 1, 0, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // `UnitState` ctor 恒 `Exhausted = true` ⇒ 显式写 false（与 `B144_R2_AI_Assert.Place` 同形）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static void GroupC_AttackSelectorGates()
    {
        var ctx = NewCtx();

        // C1 正常在场单位 ⇒ 放行（前提 + 结论一起断，⛔ 不写 if 包住）
        var ok = Place(ctx, 0, 3, Unit("B145E3_OK", 2, 2, 5));
        Check(ok.CardStateOf(ctx) == CardStateOptions.inPlay, "C0a 前提：正常单位的 `cardState` = 2（inPlay）");
        Check(BattleDriver.AttackSelectorAllowed(ok, ctx),
              "C1 ★ 正常在场单位**开得出**攻击选择器（原版 `CanAttackCard.c` 的默认档）");

        // C2 禁攻 trait ①：cantAttack(150)
        var cant = Place(ctx, 0, 4, Unit("B145E3_CantAttack", 2, 2, 5));
        cant.AddKeyword(KeywordTable.CantAttack, 1);
        Check(cant.Has(KeywordTable.CantAttack), "C2a 前提：`cantAttack`(150) 关键词已在");
        Check(!BattleDriver.AttackSelectorAllowed(cant, ctx),
              "C2 ★★ 带 `cantAttack`(150) 的单位**开不出**近战/远程"
            + "｜🧨 删掉 `AttackSelectorAllowed` 里 `KeywordTable.CantAttack` 那一项 ⇒ 红");

        // C3 禁攻 trait ②：nonCombatant(880)
        var nonc = Place(ctx, 0, 5, Unit("B145E3_NonCombatant", 2, 2, 5));
        nonc.AddKeyword(RuleCore.Noncombatant, 1);
        Check(nonc.Has(RuleCore.Noncombatant), "C3a 前提：`nonCombatant`(880) 关键词已在");
        Check(!BattleDriver.AttackSelectorAllowed(nonc, ctx),
              "C3 ★★ 带 `nonCombatant`(880) 的单位**开不出**近战/远程"
            + "｜🧨 删掉 `RuleCore.Noncombatant` 那一项 ⇒ 红");

        // C4 cardState 17（inPlayAminingAttack）⇒ 放行（原版 `:37-40` 的 `IsAttacking` 含 17）
        var s17 = Place(ctx, 0, 6, Unit("B145E3_State17", 2, 2, 5));
        Check(s17.MarkCardState(CardStateOptions.inPlayAminingAttack), "C4a 前提：`MarkCardState(17)` 成功");
        Check(BattleDriver.AttackSelectorAllowed(s17, ctx),
              "C4 攻方 `cardState = 17`（正在宣告攻击）⇒ **放行**（原版 `CardScript__get_IsAttacking.c` = 3 或 0x11）");

        // C5 cardState 15（inPlayTransforming）⇒ 拦住（原版这一格**不含 15**）
        var s15 = Place(ctx, 0, 7, Unit("B145E3_State15", 2, 2, 5));
        Check(s15.MarkCardState(CardStateOptions.inPlayTransforming), "C5a 前提：`MarkCardState(15)` 成功");
        Check(!BattleDriver.AttackSelectorAllowed(s15, ctx),
              "C5 ★★ 攻方 `cardState = 15`（变身中）⇒ **拦住**（原版那三档不含 15）"
            + "｜🧨 把这一格写成 `IsInPlayStateOf(...)`（含 15）⇒ 红");

        // C6 cardState 3（inPlayAttacking）⇒ 放行（IsAttacking 的另一档）
        var s3 = Place(ctx, 1, 3, Unit("B145E3_State3", 2, 2, 5));
        Check(s3.MarkCardState(CardStateOptions.inPlayAttacking), "C6a 前提：`MarkCardState(3)` 成功");
        Check(BattleDriver.AttackSelectorAllowed(s3, ctx),
              "C6 攻方 `cardState = 3`（正在攻击）⇒ **放行**（`CardScript__get_IsAttacking.c` 的第一档）");

        // C7 null 防御（原版 `.c:7` 的 `param_2 == 0` 走 `FUN_1803f47a0()` 抛异常；我们选「不放行」）
        Check(!BattleDriver.AttackSelectorAllowed(null, ctx),
              "C7 `null` 单位 ⇒ **不放行**（防御口，不给 NRE）");
    }
}
