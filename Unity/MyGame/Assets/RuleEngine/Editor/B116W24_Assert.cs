// B116W24_Assert.cs — 执行代理 `B116_W24`（2026-11-02 · 第一百一十六会话 · 引擎线）
//   文件所有权 = `RuleEngine/Core/EffectResolverPartial_B97W2.cs` ＋ 本文件（新建）＋ `Editor/RuleEngineTest.cs`（**只加同一物理行**）
//
// ============================ 它盯的是什么 ============================
//  活状态表 `ENG-11` 剩下的两档：`12 enchantment`（13 行）与 `15 singleUseEnchantment`（5 行）。
//  数据面（`B-328` / `B116_W18`）已经把 `abilityBuffs` 每行的 `enchantmentId` / `enchantmentPathId` 补齐；
//  本批把**执行器**接上 —— `RuleCore.ApplyAbilityBuff` 的 `default:` 里那两档改成真挂。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CardScript__AddEffect.c:826-829`：
//       `case 0xc:` / `case 0xf:` **两个 label 共用一句** `CardScript__AddEnchantment(param_1, param_2)`。
//   · `d:/2/tools/decomp_full/CardScript__AddEnchantment.c` **全文**：
//       `:25` 读 `newEffect.enchantment(+0x88)` → `:29-33` 取不到 ⇒ `LogWarning` + return（不挂）→
//       `:44/48` 流水号 → `:50-67` `+0x110 activeEnchantments` → `:68-90` `+0x108 activeEffects` →
//       `:91` `ActivateTraitsOnSummonOrEnchantment`。
//   · 等价位 = `Core/Enchantment.cs` 的 `Enchantments.Attach(...)`（判据写在它自己的 doc 上）。
//   · 数据分布（2026-11-02 现读）= `enchantment_templates.json`：18 行 = 12×13 + 15×5 + 16×0；
//     非零 PPtr **16** 行（解出 16 / 解不出 0）+ `m_PathID: 0` **2** 行（`Daemonic Feast`）；
//     其中 **2 行指向** `Z-Sororitas-Raging Storm_enchantment1` —— 🔴 2026-11-02（`B116_W28`）收录闸放宽后
//     它**已在第二张表里**（不再「表外」，G7 的分母 2 → **0**）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A/B 段：把 `ApplyAttachEnchantmentBuff` 改回「只出声、不改状态」（= 本批改之前的样子）⇒ A1/A2/B1 红。
//   · A/B 段：把挂的模板写死成一张（不读 `b.EnchantmentId`）⇒ A2″ 或 B3 红（两张不同模板）。
//   · C 段：把 `m_PathID: 0` 那一档当成「随便挂一张」⇒ C3 红（`ActiveEnchantments` 不许涨）。
//   · D 段：把「模板 id 查不到」那一档硬挂（`FindEnchantmentTemplate` 失败就拿池里第一张顶）⇒ D3 红。
//   · E 段（**灭自证的核心**）：同一张卡、同一行，只把 PPtr 两列抹掉 ⇒ **必须不挂**；
//     而带 PPtr 时必须挂 —— 两条必须一真一假，同真同假说明断言没咬住（或被测实现忽略了这一列）。
//   · F 段：把 `16 choiceEnchantment` 也顺手挂上（只做 `AddEnchantment` 那一句）⇒ F1/F2 红。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W24`：`ENG-11` —— `buffType 12/15` 的附魔挂载执行器。</summary>
public static class B116W24_Assert
{
    const string P = "[B116W24] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    // ============================================================ 数据面常量（2026-11-02 现读）
    //  ⚠️ 这些是**真值**（原版模板 id / PPtr），不是我们挑的参数 —— 表变了就该红（提醒重算口径）。
    /// <summary>`T2 BlackLegion-Spreading Corruption_enchantment`（`15` 那 3 行**自挂**；`owners[]` 里 `BL74.inPool = true`）。</summary>
    const string SPREADING_ID = "875eedc90a1608d49a95b34a863ca104";
    /// <summary>`Warp Storm` 模板（它的 `12` 行指向 `Night`）。</summary>
    const string WAR_STORM_ID = "1c7892a2816a53d4388d6dc5272853b6";
    /// <summary>`Z-NC Goff-Night_enchantment`（`12` 那 11 行的目标）。</summary>
    const string NIGHT_ID = "77af39fae01dff044ac948424e5479c1";
    /// <summary>`Daemonic Feast` 模板（它有 **2 行 `m_PathID: 0`**）。</summary>
    const string DEMONIC_FEAST_ID = "ba33eab5a0587d34d8809cfae2f14058";
    /// <summary>`Raging Storm` 模板（它的 `15` 两行指向 `_enchantment1`）。</summary>
    const string RAGING_STORM_ID = "ef43a2362ee779f429aad3d0a5097964";
    /// <summary>`Z-Sororitas-Raging Storm_enchantment1` —— 真模板（`cardType 20` / `spellType 200`）；`B116_W28` 起已在表里。</summary>
    const string RAGING_STORM_OUT_ID = "2ef404c4fa412f840b8b6fa5ea6469a2";

    static IReadOnlyList<CardDatabase.AbilityBuffSpec> RowsOf(List<CardDef> tmpls, string id)
    {
        if (tmpls == null) return null;
        for (int i = 0; i < tmpls.Count; i++)
        {
            var t = tmpls[i];
            if (t != null && t.Id == id) return CardDatabase.AbilityBuffsOf(t);
        }
        return null;
    }

    static CardDatabase.AbilityBuffSpec Row(IReadOnlyList<CardDatabase.AbilityBuffSpec> rows, int ab, int lg)
    {
        if (rows == null) return null;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r != null && r.Ab == ab && r.Lg == lg && !r.IsHandBuff) return r;
        }
        return null;
    }

    /// <summary>复制一份 spec（`AbilityBuffSpec` 是普通类、字段全 public）—— 用于「只改一列」的对照实验。</summary>
    static CardDatabase.AbilityBuffSpec Clone(CardDatabase.AbilityBuffSpec s)
    {
        return new CardDatabase.AbilityBuffSpec
        {
            Ab = s.Ab, Lg = s.Lg, IsHandBuff = s.IsHandBuff, BuffType = s.BuffType,
            Trait = s.Trait, TraitValue = s.TraitValue, NewRace = s.NewRace,
            MeleeAttackChange = s.MeleeAttackChange, MeleeAttackValueOptions = s.MeleeAttackValueOptions,
            RangeAttackChange = s.RangeAttackChange, RangeAttackValueOptions = s.RangeAttackValueOptions,
            ArmourChange = s.ArmourChange, ArmourValueOptions = s.ArmourValueOptions,
            HealthChange = s.HealthChange, HealthValueOptions = s.HealthValueOptions,
            CostChange = s.CostChange, CostValueOptions = s.CostValueOptions,
            RandomValueChange = s.RandomValueChange,
            PermanentBuff = s.PermanentBuff, DoNotStack = s.DoNotStack,
            MarkOfChaosType = s.MarkOfChaosType, DaemonHostType = s.DaemonHostType,
            MutationType = s.MutationType,
            Extrinsic = s.Extrinsic, ExtrinsicVariable = s.ExtrinsicVariable,
            UntilEndOfTurn = s.UntilEndOfTurn, UntilPlayerTurnStart = s.UntilPlayerTurnStart,
            UntilEnemyTurnStart = s.UntilEnemyTurnStart,
            UntilFollowingPlayerTurnStart = s.UntilFollowingPlayerTurnStart,
            UntilNextAttack = s.UntilNextAttack,
            PlayersAffected = s.PlayersAffected, LimitedUses = s.LimitedUses,
            NumberOfUses = s.NumberOfUses, Criteria = s.Criteria,
            EnchantmentId = s.EnchantmentId, EnchantmentPathId = s.EnchantmentPathId,
        };
    }

    // ============================================================ 夹具（与 `B116W8_Assert` 同口径）
    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B116W24Warlord0", "SpaceWolves") };
        var d1 = new List<CardDef> { Hero("B116W24Warlord1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`（同 `B102R1_Assert.Place`）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static CardDef Body(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 2, 5, 0);

    static int EnchCount(UnitState u) => u != null && u.Instance != null ? u.Instance.ActiveEnchantments.Count : -1;

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            CardDatabase.Load();                                   // 装 `BattleContext.EnchantmentTemplatesProvider`
            var tmpls = CardDatabase.LoadEnchantmentTemplates();
            Ok(tmpls != null && tmpls.Count > 0, "前提：第二张表读到了（`CardDatabase.LoadEnchantmentTemplates`）");
            if (tmpls == null || tmpls.Count == 0) return;

            SectionA_SingleUse15(tmpls);
            SectionB_Enchantment12(tmpls);
            SectionC_NullPptr(tmpls);
            SectionD_OffTable(tmpls);
            SectionE_AntiSelfProof(tmpls);
            SectionF_Choice16(tmpls);
            SectionG_Distribution(tmpls);
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B116W24 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================
    //  A · `15 singleUseEnchantment` —— `Spreading Corruption` **自挂**那 3 行
    // ============================================================
    static void SectionA_SingleUse15(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- A · `case 0xf(15)`：`CardScript__AddEffect.c:826-828` → `AddEnchantment` ---");
        var rows = RowsOf(tmpls, SPREADING_ID);
        Ok(rows != null && rows.Count > 0, "A0 **前提**：`Spreading Corruption` 模板的 `abilityBuffs` 读到了");
        var r = Row(rows, 1, 0);
        Ok(r != null, "A0′ **前提**：它有 (ab 1, lg 0, buffEffect) 这一行");
        if (r == null) return;
        Eq(r.BuffType, 15, "A0″ **前提**：这一行的 `buffType = 15 singleUseEnchantment`");
        Ok(r.EnchantmentId == SPREADING_ID, "A0‴ **前提**：它指向自己（`B116_W18` 补的那一列）");

        var ctx = NewCtx();
        var u = Place(ctx, 0, 3, Body("B116W24A"));
        Eq(EnchCount(u), 0, "A1 **前提**：载体身上本来没有附魔");
        int used0 = ctx.EnchantmentsUsed;
        var unresolved = new List<string>();

        bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, r, "B116W24:A", unresolved);

        Ok(ok, "A2 🧨 `singleUseEnchantment(15)` 落地（改前这一步走 `default:` ⇒ false 且一张不挂）");
        Eq(EnchCount(u), 1, "A2′ 🧨 `+0x110 activeEnchantments` 涨了 1 条（原版 `AddEnchantment.c:50-67`）");
        Ok(u.Instance.ActiveEnchantments.Count == 1
           && u.Instance.ActiveEnchantments[0].Card != null
           && u.Instance.ActiveEnchantments[0].Card.Id == SPREADING_ID,
           "A2″ 🧨 挂上去的就是 `Spreading Corruption` 那张模板（⛔ 不是随便挑的一张）");
        Eq(u.Instance.ActiveEffects.Count, 1, "A2‴ 🧨 `+0x108 activeEffects` **同时**也追加了一份（原版 `:68-90`）");
        Eq(ctx.EnchantmentsUsed - used0, 1,
           "A3 🧨 流水号 +1（原版 `:44` 取 `bm.enchatmentsUsed(+0x4e4)` → `:48` `BattleManager__UsedEnchantment`）");
        Eq(u.Instance.ActiveEnchantments[0].EnchantmentCardId, used0,
           "A3′ 记录的 id = 挂载前的流水号值（原版 `:44` 是**先取值再加一**）");
        Eq(unresolved.Count, 0, "A4 没有未解析项");
    }

    // ============================================================
    //  B · `12 enchantment` —— `Warp Storm` 的 (ab 0, lg 0) → `Night`
    // ============================================================
    static void SectionB_Enchantment12(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- B · `case 0xc(12)`：与 15 **同一句**（`.c:826-828` 两个 label 共用）---");
        var r = Row(RowsOf(tmpls, WAR_STORM_ID), 0, 0);
        Ok(r != null, "B0 **前提**：`Warp Storm` 有 (ab 0, lg 0, buffEffect) 这一行");
        if (r == null) return;
        Eq(r.BuffType, 12, "B0′ **前提**：这一行的 `buffType = 12 enchantment`");

        var ctx = NewCtx();
        var u = Place(ctx, 0, 4, Body("B116W24B"));
        var unresolved = new List<string>();
        bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, r, "B116W24:B", unresolved);

        Ok(ok, "B1 🧨 `enchantment(12)` 落地（改前 = false）");
        Eq(EnchCount(u), 1, "B2 🧨 挂上 1 条");
        Ok(u.Instance.ActiveEnchantments.Count == 1
           && u.Instance.ActiveEnchantments[0].Card != null
           && u.Instance.ActiveEnchantments[0].Card.Id == NIGHT_ID,
           "B3 🧨 挂的是 `Night`（`12` 那 11 行的目标；写死模板 / 读错列 ⇒ 这里红）");
        Eq(unresolved.Count, 0, "B4 没有未解析项");
    }

    // ============================================================
    //  C · `m_PathID: 0` —— `Daemonic Feast`（原版 `AddEnchantment.c:29-33` 的 LogWarning + 不挂）
    // ============================================================
    static void SectionC_NullPptr(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- C · `m_PathID: 0`：原版 `.c:25-33` 取不到 ⇒ LogWarning + return ---");
        var r = Row(RowsOf(tmpls, DEMONIC_FEAST_ID), 0, 0);
        Ok(r != null, "C0 **前提**：`Daemonic Feast` 有 (ab 0, lg 0, buffEffect) 这一行");
        if (r == null) return;
        Ok(r.EnchantmentPathId == 0L, "C0′ **前提**：它的 `enchantmentPathId == 0`（原版 PPtr 的 null）");
        Ok(string.IsNullOrEmpty(r.EnchantmentId), "C0″ **前提**：`enchantmentId` 为空");

        var ctx = NewCtx();
        var u = Place(ctx, 0, 5, Body("B116W24C"));
        var unresolved = new List<string>();
        bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, r, "B116W24:C", unresolved);

        Ok(!ok, "C1 🧨 **返回 false**（原版 `.c:29-33` 不挂；⛔ 不许『随便挑一张顶上』）");
        Eq(EnchCount(u), 0, "C2 🧨 一张都没挂（`ActiveEnchantments` 不涨）");
        Eq(unresolved.Count, 1, "C3 🧨 **出声**（进 `unresolved`；静默 = 红线）");
    }

    // ============================================================
    //  D · 取不到模板 —— 原「表外」那张（`_enchantment1`）已被 `B116_W28` 的收录闸② 收进表里
    //      ⇒ 本段改测「两张表里**确实没有**这个 id」那条闸（原版 `.c:29-33`：取不到 ⇒ 不挂 + 出声）
    // ============================================================
    static void SectionD_OffTable(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- D · 模板查不到 ⇒ 不挂 + 出声（原「表外」那张现已收进表）---");
        var r = Row(RowsOf(tmpls, RAGING_STORM_ID), 0, 0);
        Ok(r != null, "D0 **前提**：`Raging Storm` 有 (ab 0, lg 0, buffEffect) 这一行");
        if (r == null) return;
        Ok(r.EnchantmentId == RAGING_STORM_OUT_ID,
           "D0′ **前提**：它指向 `_enchantment1`（`B116_W18` 解出来了）");
        Ok(r.EnchantmentPathId != 0L, "D0″ **前提**：PPtr 非零（不是 `m_PathID: 0` 那一档）");

        var ctx = NewCtx();
        Ok(ctx.FindEnchantmentTemplate(RAGING_STORM_OUT_ID) != null,
           "D1 🔴 收录闸②（被非零 PPtr 指向）已把它收进第二张表 —— 这一条就是闸放宽的落点");
        // 用「两张表里**确实**都不存在」的 id 走 `.c:29-33` 那条取不到闸（其余 26 格照抄真行）
        var ghost = Clone(r);
        ghost.EnchantmentId = "no-such-enchantment-template";
        var u = Place(ctx, 0, 6, Body("B116W24D"));
        var unresolved = new List<string>();
        bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, ghost, "B116W24:D", unresolved);

        Ok(!ok, "D2 🧨 **返回 false**（模板 id 两张表都查不到 ⇒ 不挂，原版 `.c:29-33` 同一条）");
        Eq(EnchCount(u), 0, "D3 🧨 一张都没挂（⛔ 不许拿池里别的一张顶）");
        Eq(unresolved.Count, 1, "D4 🧨 **出声**");
    }

    // ============================================================
    //  E · **灭自证** —— 同一张卡、同一行，只抹掉 PPtr 两列 ⇒ 必须不挂
    // ============================================================
    static void SectionE_AntiSelfProof(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- E · 灭自证：带 PPtr ⇒ 挂；抹掉 PPtr ⇒ 不挂（两条必须一真一假）---");
        var real = Row(RowsOf(tmpls, SPREADING_ID), 1, 0);
        Ok(real != null, "E0 **前提**：拿到 `Spreading Corruption` 那一行");
        if (real == null) return;

        // E1：原样（带 PPtr）⇒ 挂
        var ctxA = NewCtx();
        var uA = Place(ctxA, 0, 3, Body("B116W24E_on"));
        bool okA = RuleCore.ApplyAbilityBuff(ctxA, 0, uA, real, "B116W24:E_on");
        Ok(okA && EnchCount(uA) == 1, "E1 🧨 **带 PPtr** ⇒ 挂上 1 条");

        // E2：只把 `enchantmentId` / `enchantmentPathId` 抹掉，**其余 26 格逐字不变** ⇒ 不挂
        var stripped = Clone(real);
        stripped.EnchantmentId = null;
        stripped.EnchantmentPathId = 0;
        var ctxB = NewCtx();
        var uB = Place(ctxB, 0, 3, Body("B116W24E_off"));
        var unresolved = new List<string>();
        bool okB = RuleCore.ApplyAbilityBuff(ctxB, 0, uB, stripped, "B116W24:E_off", unresolved);
        Ok(!okB, "E2 🧨 **抹掉 PPtr** ⇒ 返回 false（⛔ 不许因为别的原因『猜』挂一张）");
        Eq(EnchCount(uB), 0, "E2′ 🧨 抹掉 PPtr 后 **一条都没挂**（同真同假 ⇒ 断言没咬住这一列）");
        Eq(unresolved.Count, 1, "E2″ 抹掉 PPtr 后**出声**");

        // E3：两条的差别**只有那两列**（其余 26 格逐字相同）—— 证明 E1/E2 的分叉来自这一列本身
        bool sameOther = stripped.BuffType == real.BuffType && stripped.Ab == real.Ab
                      && stripped.Lg == real.Lg && stripped.Trait == real.Trait
                      && stripped.TraitValue == real.TraitValue
                      && stripped.Extrinsic == real.Extrinsic
                      && stripped.PermanentBuff == real.PermanentBuff
                      && stripped.UntilEndOfTurn == real.UntilEndOfTurn;
        Ok(sameOther, "E3 🧨 对照组与被测组**只在 PPtr 两列上不同**（其余关键格逐字相同）");
    }

    // ============================================================
    //  F · `16 choiceEnchantment` —— **仍不结算**（原版那一支多出四个 spellType 旁支）
    // ============================================================
    static void SectionF_Choice16(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- F · `case 0x10(16)`：整档不做（四个 spellType 旁支未建）---");
        var baseRow = Row(RowsOf(tmpls, SPREADING_ID), 1, 0);
        Ok(baseRow != null, "F0 **前提**：拿到一行真实载荷当底座");
        if (baseRow == null) return;
        var s = Clone(baseRow);
        s.BuffType = 16;                                            // 只改这一格
        s.EnchantmentId = SPREADING_ID;                             // 故意给一个**能查到**的模板：仍不许挂
        s.EnchantmentPathId = -259810654571001761L;

        var ctx = NewCtx();
        Ok(ctx.FindEnchantmentTemplate(SPREADING_ID) != null,
           "F1 **前提**：`Spreading Corruption` 模板**查得到**（所以 F2 的红不是因为查不到）");
        var u = Place(ctx, 0, 3, Body("B116W24F"));
        var unresolved = new List<string>();
        bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, s, "B116W24:F", unresolved);

        Ok(!ok, "F2 🧨 `choiceEnchantment(16)` **返回 false**（原版 `.c:830-932` 还有四支没建 ⇒ 只挂一半 = 做一半）");
        Eq(EnchCount(u), 0, "F2′ 🧨 一张都没挂");
        Eq(unresolved.Count, 1, "F2″ **出声**（不许静默）");
    }

    // ============================================================
    //  G · 数据分布 —— 钉住本批的分母（表一变就红，提醒重算「还剩几行」）
    // ============================================================
    static void SectionG_Distribution(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- G · 分布：18 行 = 12×13 + 15×5 + 16×0；PPtr 16 / null 2 / 表外 0 ---");
        var tids = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < tmpls.Count; i++) if (tmpls[i] != null) tids.Add(tmpls[i].Id);

        int n12 = 0, n15 = 0, n16 = 0, withPtr = 0, nullPtr = 0, unresolvable = 0, offTable = 0;
        for (int i = 0; i < tmpls.Count; i++)
        {
            var t = tmpls[i];
            if (t == null) continue;
            var rows = CardDatabase.AbilityBuffsOf(t);
            if (rows == null) continue;
            for (int j = 0; j < rows.Count; j++)
            {
                var r = rows[j];
                if (r == null) continue;
                if (r.BuffType != 12 && r.BuffType != 15 && r.BuffType != 16) continue;
                if (r.BuffType == 12) n12++;
                else if (r.BuffType == 15) n15++;
                else n16++;
                if (r.EnchantmentPathId == 0) { nullPtr++; continue; }
                withPtr++;
                if (string.IsNullOrEmpty(r.EnchantmentId)) { unresolvable++; continue; }
                if (!tids.Contains(r.EnchantmentId)) offTable++;
            }
        }
        Eq(n12, 13, "G1 `12 enchantment` 13 行（**本批接上**）");
        Eq(n15, 5, "G2 `15 singleUseEnchantment` 5 行（**本批接上**）");
        Eq(n16, 0, "G3 `16 choiceEnchantment` 0 行（本批仍不结算 —— 0 行 = 零可观测）");
        Eq(withPtr, 16, "G4 非零 PPtr **16** 行（另 2 行 `m_PathID: 0`）");
        Eq(nullPtr, 2, "G5 `m_PathID: 0` **2** 行（`Daemonic Feast`）");
        Eq(unresolvable, 0, "G6 解不出的 **0** 行");
        Eq(offTable, 0, "G7 指向表外模板的 **0** 行（`B116_W28` 收录闸② 已把 `_enchantment1` 收进表）");
        Eq(withPtr + nullPtr, 18, "G8 合计 18 行（与 `B116W18_Assert.B1` / `B116W8_Assert.C1` 同一组分母）");
    }
}
