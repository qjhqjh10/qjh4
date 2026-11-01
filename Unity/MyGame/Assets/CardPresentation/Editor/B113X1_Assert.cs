// B113X1_Assert.cs — 执行代理 `B113X1`（第一百一十三会话 · 第 2 波 · 引擎小组）
//
// ============================ 它盯的是什么（两条账）============================
//   `B-230` `EffectText.AssignAbilityTargets` 里那个 `if (ta != null && ta.Length == blk.Count)`：
//           **整卡块数对得上、但这一块**的 `ta` 长度不对时，改前**既不贴值、也不记 gap**
//           ⇒ 这一块 op 的 criteria 静默丢失（`ApplyTargetCriteriaFilter` 的 `set == null` 分支一个都不喊）。
//           现在记原因码 `ta-length-mismatch`（与整卡的 `block-mismatch` 分开），出声口仍是同一个
//           `ApplyTargetCriteriaFilter`（那个 `else if` 支由调度台合并补丁，本文件只断**字段值**）。
//   `B-212` `CardCriteria.FromTarget` **不拷 `SubtypeFilter`** ⇒ `… you deploy` / `… attacked by this unit`
//           那两支（`EffectResolver.cs` 的 `Deployed` / `AttackedBySelf`）拿到的 criteria **少了兵种这一维**
//           ⇒ 静默宽打。现池 0 例 ⇒ 补上 = 零行为变化（`play check` 差异集合改前/改后逐字相同）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B-230 A1 vs A0：**同一句 desc、只改 `ta` 长度**（`[10]` vs `[10,10]`）。
//       把 `EffectText` 新加的 `else if` 删回裸 `if` ⇒ A1 的 gap 变空 ⇒ 红；
//       把原因码写成 `block-mismatch`（混成同一条）⇒ A1 红（它要的是**分开记**）。
//     A0 同时钉住「长度对得上时 criteria 照常挂」⇒ 谁把整支改成「一律不挂」也会红。
//   · B-212 B2：**只有兵种筛**的 spec —— 改前 `FromTarget` 返回 `null`（`IsEmpty` 不含这一维）⇒ 红。
//     B3 是它的反例（不带兵种筛 ⇒ 该字段空、且**不**筛掉别的兵种）⇒ 谁把筛写成恒真/恒假都会红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里那一行聚合追加 `Step(B113X1Case);`
//   （本文件是 `RuleEngineTest` 的**分部** ⇒ 复用夹具 `Check` / `CheckTrue` —— ⛔ 另起一个类会抄第二份）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>B113X1 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(B113X1Case);`）。</summary>
    public static void B113X1Case()
    {
        Section("🆕 B113X1 —— B-230（单块 `ta` 长度不对要记原因码）/ B-212（`FromTarget` 补 `SubtypeFilter`）"
              + " / B-232（督军算出伤单位：原版 `inclHero = 1` ⇒ 算）");
        B113X1TaLengthMismatch();
        B113X1SubtypeFilterCopied();
        B113X1WarlordCountsAsShooter();
    }

    // ==============================================================================================
    //  ① `B-230` —— 单块 `ta` 长度不对 ⇒ 记 `ta-length-mismatch`（⛔ 不许静默）
    // ==============================================================================================
    static void B113X1TaLengthMismatch()
    {
        // 同一句正文（= 一个能力块），只换 `abilityTargets[0].ta` 的长度：
        //   `ok`  = `[10]`   ⇒ 块内 op 数 1 == ta 长度 1 ⇒ criteria 照常挂（对照）
        //   `bad` = `[10,10]`⇒ 整卡块数(1) == ability 数(1)，但**这一块**的长度对不上 ⇒ 走新加的那支
        var okCard = X1TaCard("X1TaOk", new[] { 10 });
        var badCard = X1TaCard("X1TaBad", new[] { 10, 10 });

        var okOps = EffectText.Parse(okCard.Desc, out _, out _, card: okCard);
        var badOps = EffectText.Parse(badCard.Desc, out _, out _, card: badCard);

        // ---- A0（硬前提 + 对照组）：长度对得上 ⇒ 照常挂 criteria ----
        CheckTrue(okCard.AbilityTargets.Count == 1 && okCard.AbilityTargets[0] != null
                  && okCard.AbilityTargets[0].TargetsAffected != null
                  && okCard.AbilityTargets[0].TargetsAffected.Length == 1,
                  "A0 （前提）对照卡的 `ta` 长度 = **1**（`abilityTargets` 也恰好 1 条 ⇒ 整卡 G1 成立）");
        CheckTrue(okOps != null && okOps.Count == 1 && okOps[0] != null,
                  "A0 （前提）`Deal 2 damage to all units` 解出**唯一一条** op（= 一个能力块）");
        if (okOps != null && okOps.Count == 1 && okOps[0] != null)
        {
            CheckTrue(okOps[0].TargetCriteriaSet != null,
                      "★ A0 对照：`ta` 长度对得上 ⇒ **criteria 照常挂**（`TargetCriteriaSet != null`）"
                    + "｜🧨 谁把整支改成「一律不挂」⇒ 本行红（闸必须只在长度不对时才拦）");
            CheckTrue(string.IsNullOrEmpty(okOps[0].TargetCriteriaGap),
                      "★ A0 对照：长度对得上 ⇒ **不记原因码**（gap 为空）");
        }

        // ---- A1（被测）：整卡块数对得上、但这一块 ta 长度不对 ⇒ 必须记原因码 ----
        CheckTrue(badCard.AbilityTargets.Count == 1 && badCard.AbilityTargets[0] != null
                  && badCard.AbilityTargets[0].TargetsAffected != null
                  && badCard.AbilityTargets[0].TargetsAffected.Length == 2,
                  "A1 （前提）被测卡的 `ta` 长度 = **2**，而整卡仍然只有 **1 条** ability ⇒ 走的是**单块**那一支"
                + "（不是整卡的 `block-mismatch`）");
        CheckTrue(badOps != null && badOps.Count == 1 && badOps[0] != null,
                  "A1 （前提）被测卡同样解出**唯一一条** op（块数 1 == ability 数 1）");
        if (badOps != null && badOps.Count == 1 && badOps[0] != null)
        {
            CheckTrue(badOps[0].TargetCriteriaSet == null,
                      "A1 （前提）长度不对 ⇒ **不挂** criteria（⛔ 不按位置猜，这条与原版「宁可不筛也不猜」一致）");
            CheckTrue(badOps[0].TargetCriteriaGap == "ta-length-mismatch",
                      "★★ A1 **单块 `ta` 长度不对 ⇒ 原因码 `ta-length-mismatch` 记上了**"
                    + "（改前这里既没 criteria 也没 gap ⇒ 下游 `ApplyTargetCriteriaFilter` 静默不筛）"
                    + "｜🧨 把 `EffectText.AssignAbilityTargets` 新加的 `else if` 删回裸 `if` ⇒ 本行红；"
                    + "把它写成 `block-mismatch`（与整卡那条混成一条）⇒ 本行红");
        }
    }

    /// <summary>夹具：一句正文（= 一个能力块）的战术卡，`abilityTargets[0].ta` 用调用方给的长度。
    /// <para>⚠️ 自带一份最小 `CountSetSpec`（不复用别批的夹具）—— 本文件要能在别批文件被改动时**独立编过**。</para></summary>
    static CardDef X1TaCard(string id, int[] ta)
    {
        var targets = new[]
        {
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = ta },
        };
        var rows = new[]
        {
            new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = 0, Logic = 0, Criteria = X1Criteria() },
        };
        return new CardDef(id, id, "tactic", "Deal 2 damage to all units", "common", "Test", 1, 0, 0, 0,
                           keywords: null,
                           abilityTriggers: new[] { 0 },
                           abilityTriggersVersioned: true,
                           abilityTargets: targets,
                           abilityTargetCriteria: rows,
                           abilityTargetCriteriaVersioned: true);
    }

    /// <summary>最小可用的 36 格 criteria（只求「挂得上、字段不是 `Unknown`」；本文件不拿它判人）。</summary>
    static CardDef.CountSetSpec X1Criteria()
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 10,               // allFiltered
            ChosenArmy = 0, ArmyFilter = 0,
            KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = 1, EnemyUnits = 0,
            IncludeHero = 0, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 0, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    // ==============================================================================================
    //  ② `B-212` —— `FromTarget` 要拷 `SubtypeFilter`，而且 `Matches` 真的要判它
    // ==============================================================================================
    static void B113X1SubtypeFilterCopied()
    {
        // ---- B0（硬前提）：三张卡只在 `subtype` 上有区别 ----
        var beast = X1Unit("X1Beast", "Beast");
        var infantry = X1Unit("X1Infantry", "Infantry");
        var unknownSub = X1Unit("X1NoSubtype", null);
        CheckTrue(string.Equals(beast.Subtype, "Beast", System.StringComparison.OrdinalIgnoreCase)
                  && string.Equals(infantry.Subtype, "Infantry", System.StringComparison.OrdinalIgnoreCase)
                  && string.IsNullOrEmpty(unknownSub.Subtype),
                  "B0 （前提）夹具三张卡的 `Subtype` 分别是 Beast / Infantry / 空 ⇒ 下面的判别式才有对象");

        // ---- B2（被测）：**只有兵种筛**的 spec —— 改前 `FromTarget` 直接返回 `null`（静默丢筛）----
        var specOnlySub = new EffectTargetSpec
        { Raw = "(夹具：某兵种)", Side = "own", Kind = "any", Count = 0, SubtypeFilter = "Beast" };
        var crit = CardCriteria.FromTarget(specOnlySub);
        CheckTrue(crit != null,
                  "★★ B2 **只有兵种筛的 spec 不再被 `FromTarget` 丢掉**（改前 `IsEmpty` 不含这一维 ⇒ 返回 `null` "
                + "⇒ `… you deploy` / `… attacked by this unit` 两支拿到的 criteria 少了兵种筛）"
                + "｜🧨 把 `FromTarget` 里新加的 `SubtypeFilter = spec.SubtypeFilter` 删掉 ⇒ 本行红");
        CheckTrue(crit != null && string.Equals(crit.SubtypeFilter, "Beast", System.StringComparison.OrdinalIgnoreCase),
                  "★★ B2' 拷过来的 `SubtypeFilter` = **Beast**（不是空）");
        if (crit != null)
        {
            CheckTrue(crit.Matches(beast), "B2'' 兵种筛对**同兵种**判 `true`");
            CheckTrue(!crit.Matches(infantry),
                      "★★ B2''' 兵种筛对**别的兵种**判 `false`"
                    + "｜🧨 把 `SubtypeMatches` 写成恒 `true`（只拷字段不判）⇒ 本行红");
            CheckTrue(crit.Matches(unknownSub),
                      "★★ B2'''' 候选卡的 `Subtype` **为空** ⇒ **保留**（与 `EffectResolver.ApplyTargetFilters` / "
                    + "`RuleCore.HandEffectFits` 同一条纪律：查不到兵种不排除）"
                    + "｜🧨 把这一支写成「空 ⇒ 淘汰」⇒ 本行红");
        }

        // ---- B3（反例）：不带兵种筛 ⇒ 该字段为空，而且**不会**筛掉别的兵种 ----
        var specNoSub = new EffectTargetSpec
        { Raw = "(夹具：任意兵种)", Side = "own", Kind = "any", Count = 0 };
        var critNoSub = CardCriteria.FromTarget(specNoSub);
        // ⚠️ 2026-11-01（实测订正）：`FromTarget` 在「任何一维都没筛」时返回 **null**（`c.IsEmpty ? null : c`）
        //   ⇒ 「不带 SubtypeFilter」的合法形状是 **null 或空串**，两者都算「没有这一维」。
        CheckTrue(critNoSub == null || string.IsNullOrEmpty(critNoSub.SubtypeFilter),
                  "★ B3 反例：spec **不带** `SubtypeFilter` ⇒ criteria 那一维**为空**（null 或空串都算；⛔ 不许凭空补一个筛）");
        CheckTrue(critNoSub == null || critNoSub.Matches(infantry),
                  "★ B3' 反例：没有兵种筛 ⇒ **别的兵种照样匹配**（与 B2''' 是结构上不可能同时满足的一对）");
    }

    /// <summary>夹具：一张只在 `subtype` 上有区别的单位卡。</summary>
    static CardDef X1Unit(string name, string subtype)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 1, 5, 0,
                           keywords: null, subtype: subtype);
    }

    // ==============================================================================================
    //  ③ `B-232` —— 原版督军**算出伤单位**（`includeHero = 1` ⇒ 算），我们这一侧必须对得上
    // ==============================================================================================
    //  判据（第一权威，2026-11-01 现读 `d:/2/tools/decomp_full/`）：
    //    · `BattleManager__GetUnitsInPlayList.c:28-29` 把 9 个 bool 原样转给
    //      `GetUnitsInPlayPooledList`；`il2cpp_out/dump.cs:32571` 的签名 =
    //      `GetUnitsInPlayPooledList(bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons,
    //                                bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying)`。
    //    · `AbilityLogic__GetTargets.c:985-989`（`targetsAffected == 10 allFiltered` 那一档，`UM82`/`ASH43` 都是 10）
    //      按 `TargetCriteria` 的**字段偏移**逐个传进去：`+0x16 includeHero` → `inclHero`、
    //      `+0x17 includeMinions` → `inclMinions`、`+0x1c includeHeroParts` → `inclHeroParts`、
    //      `+0x1a includeWeapons` → `inclWeapons`、`+0x1b includeRemnants` → `inclRemnants`
    //      —— 与 `TargetCriteria.cs:9-27` 的声明序**逐格 1:1 对上**（`+0x10 targetsAffected` 是那个 int）。
    //    · `BattleManager__GetUnitsInPlayPooledList.c:49-53`：`inclHero == 0` 时把 `cardType == 10 (Hero)` 剔掉；
    //      **`inclHero != 0` 就不过这道闸** ⇒ 督军留在池里。
    //    · `CardScript__TriggerOtherCardAbility.c` / `RawCardScript__OnTriggerEffectInCard.c` 对目标**不看卡种**
    //      ⇒ 池里的督军照样把这条效果重放一次（260 `triggerEffectInCard` 的语义）。
    //  ⇒ **原版算督军**。我们这一侧 `DoEachUnitDeal` 扫的是裸 `Board`（`BoardSpec.Size = 9`、督军在第 4 格），
    //     `TargetCriteriaEval` 的 ⑥ 卡种白名单对 `hero` 读的正是 `set.IncludeHero` ⇒ **两边一致，不是缺陷**。
    //     下面这对断言把这条口径**钉住**（⛔ 别让后人「顺手」把督军从出伤池里剔掉）。
    static void B113X1WarlordCountsAsShooter()
    {
        // 两个夹具只差 criteria 的 `includeHero`（1 / 0）。伤害是**定值 2** ⇒ 数字上看得见。
        var withHero = X1EachUnitDealCard("X1EachHeroOn", includeHero: 1);
        var withoutHero = X1EachUnitDealCard("X1EachHeroOff", includeHero: 0);

        int onBefore, offBefore;
        int onDealt = X1RunEachUnitDeal(withHero, out onBefore);
        int offDealt = X1RunEachUnitDeal(withoutHero, out offBefore);

        CheckTrue(onDealt > 0 && offDealt > 0, "C0 （前提）两局都真的打出了伤害（否则下面的数字是空转）");
        Check(onDealt, 4,
              "★★ C1 **原版 `inclHero = 1` ⇒ 督军算出伤单位**：己方「督军 + 1 个部队」共 **2** 个单位各打 2 点 ⇒ 敌方共掉 **4**"
            + "｜判据 = `dump.cs:32571` 的 `inclHero` ＋ `GetUnitsInPlayPooledList.c:49-53`"
            + "（`inclHero == 0` 才剔 `cardType == 10`）＋ `AbilityLogic__GetTargets.c:985-989`"
            + "｜🧨 谁把督军从出伤池里剔掉 ⇒ 本行变 2 ⇒ 红");
        Check(offDealt, 2,
              "★★ C2 **灭自证 · `includeHero = 0` ⇒ 只剩那 1 个部队开火**（2 点）"
            + "｜与 C1 是**同一次改动不可能同时弄绿**的一对：一刀切「督军永远算/永远不算」必红其一");
    }

    /// <summary>夹具：`Each of your units deals 2 damage to a random enemy`（`UM82` 那一族的**定值**写法），
    /// criteria 挂在 `ab=0/lg=0`，只有 `includeHero` 由调用方切。</summary>
    static CardDef X1EachUnitDealCard(string id, int includeHero)
    {
        var crit = X1Criteria();
        crit.IncludeHero = includeHero;      // 其余格照最小可用值
        var targets = new[]
        {
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 10 } },
        };
        var rows = new[]
        {
            new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = 0, Logic = 0, Criteria = crit },
        };
        return new CardDef(id, id, "tactic", "Each of your units deals 2 damage to a random enemy",
                           "common", "Test", 1, 0, 0, 0, keywords: null,
                           abilityTriggers: new[] { 0 },
                           abilityTriggersVersioned: true,
                           abilityTargets: targets,
                           abilityTargetCriteria: rows,
                           abilityTargetCriteriaVersioned: true);
    }

    /// <summary>摆一局「己方 = 督军 + 1 个部队、敌方 = 3 个 20/30 血靶子」，把那张开火卡打出去；
    /// 返回**敌方掉掉的总血量**（`IsAlive` 的那些，摆的大血 ⇒ 不会死人）。
    /// <para>`enemyHpBefore` = 出牌前的敌方总血（照 `W2EnemyHp` 同一口径，给报告用）。</para></summary>
    static int X1RunEachUnitDeal(CardDef card, out int enemyHpBefore)
    {
        var ctx = RuleCore.NewBattle(DeckOf("Test", card),
                                     DeckOf("Test", Unit("X1FoeA", 1, 0, 20)),
                                     seed: 0, shuffle: false);
        RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 20;
        Place(ctx, 1, 3, Unit("X1TgtA", 1, 0, 20));
        Place(ctx, 1, 5, Unit("X1TgtB", 1, 0, 20));
        Place(ctx, 0, 3, Unit("X1Ally", 1, 1, 5));
        var w = ctx.Players[0].Board[BoardSpec.WarlordSlot];
        CheckTrue(w != null && w.IsAlive && w.IsWarlord,
                  "C0 （前提）己方督军**就在棋盘第 4 格上**（`RuleCore.NewBattle` 的 `p.Board[BoardSpec.WarlordSlot] = p.Warlord`）"
                + "｜🧨 督军不在棋盘上 ⇒ 出伤循环天然扫不到它，C1/C2 全无对象");
        enemyHpBefore = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[1].Board[s];
            if (u != null && u.IsAlive) enemyHpBefore += u.Health;
        }
        int hi = HandIdx(ctx, 0, card.Id);
        CheckTrue(hi >= 0, "C0 （前提）那张战术卡在手里");
        if (hi < 0) { enemyHpBefore = 0; return 0; }
        CheckCode(RuleCore.PlayCard(ctx, 0, hi, 0), RuleCodes.OK, "C0 （前提）这张卡打得出去");
        int after = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[1].Board[s];
            if (u != null && u.IsAlive) after += u.Health;
        }
        return enemyHpBefore - after;
    }
}
