// B116W39_Assert.cs — 执行代理 `B116_W39`（2026-11-02 · 第一百一十六会话 · 引擎线 · 目标/属性三件）
//   文件所有权 = `RuleEngine/Core/{CardDef,RuleCore,EffectResolver,ActionPump}.cs`（**类尾追加 + 同一物理行内改**）
//                ＋ 本文件（新建）＋ `Editor/RuleEngineTest.cs`（**同一物理行追加**）。
//
// ============================ 它盯的是什么 ============================
//  ① 活状态表 `B-342`：**表支档位集合不含 `ta=140`** —— `RuleCore.AbilityTargetListFromCriteria` 的白名单
//     原来只有 `{10,20,30,110,150}`，而池内 `ta=140` 有 7 行 / 3 张（`BL55`×4 · `GSC29` · `GSC75`×2）。
//     140 与 150 的**建池是同一句**（原版 `AbilityLogic__GetTargets.c:849-866` / `:867-883`），
//     差的是**逐候选筛**（140 `Contains` 直接返回 · 150 在表里才继续走常规格）。
//  ② `ENG-16`①：`Strike` 补 `chosen: 被打者` 的**前置** —— 触发正文的 op 必须带上 `abilityTargets` 的 `ta`，
//     否则 `EffectResolver.ResolveTargetsCore` 的 `chosen` 兜底会把整张目标表换掉（实测净伤 25→0）。
//     落地 = `CardDef.ApplyUniformTriggerTa`（`CardDef.AddTriggerOp` 的 `Parse` 之后同一物理行调用）。
//  ③ 活状态表 `B-317`：**手牌陷阱的 ability 级阵营闸在「真死」时永远到不了「不满足」** ——
//     `CleanupDeaths` 在发死讯**之前**就 `BoardSlots.RemoveAt`，而 `TargetCriteriaEval.SideOf` **只扫棋盘**
//     ⇒ 判不出友/敌 ⇒ `Matches` 回 `null` ⇒ 放行（多触发）。修法 = 把候选的「侧」从广播点一路传到
//     `Matches` 现成的 `sideOverride`（`EnqueueAbility(abilityTargetSide:)` → `AbilityLevelCriteriaAllows(candidateSide:)`）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  · `decomp_full/AbilityLogic__GetTargets.c:849-866`（140：`AddRange` 后 `goto LAB_180818485`，**不筛**）
//    vs `:867-883`（150：`AddRange` 后**继续**落到 `:884` 起的逐候选筛）
//  · `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:495-510`（140：`return List.Contains(...)`）
//    vs `:172-183`（150：在表里才继续走常规格）
//  · `decomp_full/CardScript__ResolveUnitAttacked.c:99-101`（`OnTrigger(0x244, …, actingCard=param_1, targetCard=param_2)`）
//  · `CardScript.isPlayer`（`+0x40`）= **卡对象字段**，死后照样读得到（`dump.cs`）—— 我们的等价物 = `UnitState.OwnerSeat`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A 段：白名单不补 `140` ⇒ A1/A2 实得 `null` ⇒ 红；把 `140` 写成与 `150` 同一支（都过常规格）⇒ A2 实得 1 ⇒ 红。
//    **A2/A3 只差 `ta` 一个字段 ⇒ 两种错实现不可能同时满足。**
//  · B 段：把 `ApplyUniformTriggerTa` 的调用删掉 ⇒ B1 实得 -1 ⇒ 红；「各格不相等也照贴」⇒ B2 红；
//    「`ta` 比 op 少也照贴」⇒ B3 红。B4 是端到端：没有 `ta` ⇒ 8 次伤害**全落在 `chosen`** ⇒ B4 红。
//  · C 段：`abilityTargetSide` 不往下传 ⇒ C1 实得 1 条触发（放行）⇒ 红；
//    「恒拒绝」的实现 ⇒ C2 实得 0 条 ⇒ 红。C1/C2 只差「候选在哪一方」。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W39`：`B-342`（表支 `ta=140`）· `ENG-16`①（触发正文挂 `ta`）· `B-317`（真死时的阵营闸）。</summary>
public static class B116W39_Assert
{
    const string P = "[B116W39] ";

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

    static void Sec(string t) { Debug.Log(P + "── " + t); }

    // ============================================================ 夹具

    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Troop(string id) => new CardDef(id, id, "unit", "", "common", "Test", 1, 2, 5, 0);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B116W39Warlord0") };
        var d1 = new List<CardDef> { Hero("B116W39Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>往棋盘摆一个单位。⚠️ `Exhausted = false` 必须显式写（`UnitState` 的 ctor 恒 `true`）；
    /// 🔴 `SetOwnerSeat` 也显式写 —— `B-317` 的修法读的就是它（真死之后棋盘扫不到）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool warlord = false)
    {
        var u = new UnitState(ctx.NewInstance(card), warlord) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        u.SetOwnerSeat(p);
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>一份**14 格全填**的 `ta` 行（`CountSetSpec` 的整数默认值是 `Unknown(-1)` 不是 0
    /// ⇒ 漏写一格 = 判不了）。本夹具口径：除判别式那几格外，其余一律「不筛」。</summary>
    static CardDef.CountSetSpec Set(int ta, int friendly = 1, int enemy = 0, int hero = 0, int minions = 1)
    {
        return new CardDef.CountSetSpec
        {
            TargetsAffected = ta,
            PlayerTurn = 0,
            ChosenArmy = 0, ArmyFilter = 0, KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendly, EnemyUnits = enemy,
            IncludeHero = hero, IncludeMinions = minions,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 1, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0, SelectFromChooseResolution = 0,
            FilterSpecificCardIds = new string[0], ExcludeEnchantmentIds = new string[0],
        };
    }

    static string Slots(List<BattleEventTarget> l)
    {
        if (l == null) return "(null)";
        var b = new System.Text.StringBuilder();
        for (int i = 0; i < l.Count; i++) { if (i > 0) b.Append(','); b.Append(l[i].Player).Append(':').Append(l[i].Slot); }
        return b.ToString();
    }

    // ============================================================ 入口

    public static void Run()
    {
        Sec("B116_W39：`B-342`（表支 `ta=140`）· `ENG-16`①（触发正文挂 `ta`）· `B-317`（真死阵营闸）");
        SectionA_Ta140Table();
        SectionB_TriggerTa();
        SectionC_RealDeathFactionGate();
        Debug.Log(P + "合计：" + Pass + " 通过 / " + Fail + " 失败");
    }

    // ============================================================ A · `B-342`
    static void SectionA_Ta140Table()
    {
        Sec("A · `B-342`：`AbilityTargetListFromCriteria` 认 `ta=140`，且 140 **不筛** / 150 **会筛**");

        // 棋盘：P0[4] = 督军（hero）、P0[3] = 部队；上一条 ability 的目标表 = 这两张。
        var ctx = NewCtx();
        var hero = Place(ctx, 0, BoardSpec.WarlordSlot, Hero("B116W39Hero"), warlord: true);
        var troop = Place(ctx, 0, 3, Troop("B116W39Troop"));
        var src = Place(ctx, 0, 5, Troop("B116W39Src"));
        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(hero));
        ctx.PreviousAbilityTargets.Add(CardRef.Of(troop));

        // 🔴 判据那一格 = `includeHero = 0`（督军候选要被 150 的常规格淘汰、被 140 的 `Contains` 保留）。
        var set140 = Set(140, friendly: 1, enemy: 0, hero: 0, minions: 1);
        var set150 = Set(150, friendly: 1, enemy: 0, hero: 0, minions: 1);

        var l140 = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, set140, src, null);
        var l150 = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, set150, src, null);

        Ok(l140 != null, "★ A1 `ta=140` **不再是「这一档没接」**（改前：白名单里没有 140 ⇒ 返回 `null` ⇒ 退回单数路）"
                        + $"｜实得 {(l140 == null ? "null" : l140.Count + " 个：" + Slots(l140))}");
        Eq(l140 == null ? -1 : l140.Count, 2,
           "★ A2 `ta=140` ⇒ **池 = 上一条 ability 的目标表、不筛** ⇒ 督军 + 部队 **2 个**全留（`GetTargets.c:849-866` 建完池就返回）");
        Eq(l150 == null ? -1 : l150.Count, 1,
           "★ A3 **同一份 criteria 只把 `ta` 换 140→150** ⇒ 150 落常规格、`includeHero=0` 把督军淘汰 ⇒ **1 个**"
         + "｜🧨 两种错实现（140 没接 / 140 照 150 的语义筛）不可能同时满足 A2 与 A3");

        // A4：池严格 = `PreviousAbilityTargets`，**不是**整张棋盘（`src` 在棋盘上但不在表里）。
        bool hasSrc = false;
        if (l140 != null) for (int i = 0; i < l140.Count; i++) if (l140[i].Slot == 5 && l140[i].Player == 0) hasSrc = true;
        Ok(!hasSrc, "★ A4 池来自 `ctx.PreviousAbilityTargets`（不是扫棋盘）：棋盘上的 `src`（P0:5）**不在表里** ⇒ 不在结果里");

        // A5：空表 ≠ 判不了 —— 表空时返回**空列表**（`null` 只留给「这一档没接」）。
        ctx.PreviousAbilityTargets.Clear();
        var empty140 = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, set140, src, null);
        Ok(empty140 != null && empty140.Count == 0,
           "★ A5 上一条 ability 的目标表为空 ⇒ `ta=140` 返回**空表**（不是 `null`）—— 两种三态不许混");

        // A6：对照 —— 一个真的没接的档仍然返回 `null`（白名单没被放宽成「全放行」）。
        var l65 = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(65), src, null);
        Ok(l65 == null, "★ A6 对照：`ta=65`（未接档）仍返回 `null` ⇒ 白名单是**逐档列**、不是「全放行」");
    }

    // ============================================================ B · `ENG-16`①
    /// <summary>`Strike` 形状的卡（与真卡 `TL30 Tyrannofex` 同形）：正文塌成 **1 条 op + `repeatTimes=8`**、
    /// `abilityTargets` 的 `ta` 有 **8 格**。</summary>
    static CardDef StrikeCard(string id, int[] ta, string desc)
    {
        return new CardDef(id, id, "unit", desc, "common", "Test",
                           cost: 10, attack: 5, health: 14, rangedAttack: 10,
                           keywords: new[] { "Strike" },
                           abilityTriggers: new[] { 580 },
                           abilityTriggersVersioned: true,
                           abilityTargets: new[] { new CardDef.AbilityTargetSpec { Trigger = 580, TargetsAffected = ta } });
    }

    static void SectionB_TriggerTa()
    {
        Sec("B · `ENG-16`①：触发正文的 op 挂上 `abilityTargets` 的 `ta`（`Strike` 接 `chosen:` 的前置）");

        // B1：真卡同形 —— `ta = [10×8]`、正文 1 条 op（`repeatTimes=8`）⇒ 必须挂到 `10`。
        var tl = StrikeCard("B116W39TL", new[] { 10, 10, 10, 10, 10, 10, 10, 10 },
                            "Strike: Deal 1 damage to a random enemy, 8 times");
        var ops = tl.TriggerOps("strike");
        Ok(ops != null && ops.Count == 1, "🧪 B1 前提：`Strike: … , 8 times` 解析成 **1 条 op**"
                                        + $"（实得 {(ops == null ? "null" : ops.Count.ToString())}；≥2 ⇒ 本条不成立，下面的判据要换形状）");
        if (ops == null || ops.Count != 1) return;
        Eq(ops[0].SubjectTargetsAffected, 10,
           "★ B1 触发正文的 op **拿到 `ta=10`**（改前 = `-1` ⇒ `ResolveTargetsCore` 的 `chosen` 兜底会把整张目标表换掉）");

        // B2：各格不相等 ⇒ **不猜**（一行都不同、而正文塌成 1 条 ⇒ 归不到某一行）。
        var mixed = StrikeCard("B116W39Mixed", new[] { 10, 30, 10 },
                               "Strike: Deal 1 damage to a random enemy, 3 times");
        var mops = mixed.TriggerOps("strike");
        Ok(mops != null && mops.Count == 1 && mops[0].SubjectTargetsAffected == -1,
           "★ B2 `ta` 各格**不相等**（`[10,30,10]`）⇒ 拒绝补值（留 `-1`，⛔ 不按位置猜）"
         + $"｜实得 {(mops == null || mops.Count == 0 ? "?" : mops[0].SubjectTargetsAffected.ToString())}");

        // B3：`ta` 比 op **少/相等** ⇒ 不补（多出来的 op 在数据里没有对应 logic 行）。
        var shortTa = StrikeCard("B116W39Short", new[] { 10 },
                                 "Strike: Deal 1 damage to a random enemy. Draw a card");
        var sops = shortTa.TriggerOps("strike");
        bool allNeg = sops != null && sops.Count >= 2;
        if (allNeg) for (int i = 0; i < sops.Count; i++) if (sops[i].SubjectTargetsAffected != -1) allNeg = false;
        Ok(allNeg, "★ B3 `ta` 只有 1 格、正文解出 **≥2 条 op** ⇒ **一条都不补**（`ta.Length < op 数` = 数据里没有对应的 logic 行）"
                 + $"｜op 数 = {(sops == null ? -1 : sops.Count)}");

        // B4：端到端（**灭自证的核心**）—— 8 次随机伤害不许全落在 `chosen` 身上。
        //     判据链：op 有 `ta=10` ⇒ `ResolveTargetsCore` 的 `chosen` 兜底（`EffectResolver.cs:1966` 的
        //     `op.SubjectTargetsAffected < 0`）不命中 ⇒ 目标池 = 全部敌人（随机各抽）。
        {
            var ctx = NewCtx();
            var striker = Place(ctx, 0, 3, StrikeCard("B116W39Striker", new[] { 10, 10, 10, 10, 10, 10, 10, 10 },
                                                      "Strike: Deal 1 damage to a random enemy, 8 times"));
            var foeA = Place(ctx, 1, 3, Troop("B116W39FoeA"));
            var foeB = Place(ctx, 1, 5, Troop("B116W39FoeB"));
            // ⚠️ 敌方督军也算「random enemy」的候选（`spec.Kind == any`）⇒ 它也要计入总伤。
            var foeW = ctx.Players[1].Warlord;
            int hpA0 = foeA.Health, hpB0 = foeB.Health, hpW0 = foeW != null ? foeW.Health : 0;
            bool fired = RuleCore.FireTriggerAtForTest(ctx, striker, KeywordTable.Strike, 0, 3, chosen: foeA);
            RuleCore.PumpActionQueues(ctx);
            int lostA = hpA0 - foeA.Health, lostB = hpB0 - foeB.Health;
            int lostW = foeW != null ? hpW0 - foeW.Health : 0;
            Ok(fired, "🧪 B4 前提：`Strike` 真的触发了");
            Eq(lostA + lostB + lostW, 8, $"★ B4 总共打了 **8** 点（8 次 × 1；A -{lostA} / B -{lostB} / 督军 -{lostW}）");
            Ok(lostB > 0 || lostW > 0, $"★ B4 **`chosen` 之外的敌人也吃到伤害**（A -{lostA} / B -{lostB} / 督军 -{lostW}）"
                        + "⇒ 目标池 = 全部敌人、`chosen` 没有顶掉整张表"
                        + "｜🧨 删掉 `ApplyUniformTriggerTa` ⇒ `chosen` 兜底命中 ⇒ 8 点**全在 A**、B/督军实得 -0");
        }

        // B5：**传参闸的三档**（2026-11-02 单变量实测查出 `GSC17` 那条之后加的闸）——
        //     `StrikeChosenOrNull` 只在「这条 `strike` 的**每一条 op 都有已知 `ta`**」时把 `chosen` 放行。
        {
            var multi = new CardDef("B116W39Multi", "B116W39Multi", "unit",
                                    "Stealth. Strike: Deal 1 damage to a random enemy, 8 times", "common", "Test",
                                    cost: 3, attack: 3, health: 4, rangedAttack: 0, keywords: new[] { "Strike" },
                                    abilityTriggers: new[] { 410, 580 }, abilityTriggersVersioned: true,
                                    abilityTargets: new[]
                                    {
                                        new CardDef.AbilityTargetSpec { Trigger = 580, TargetsAffected = new[] { 20 } },
                                        new CardDef.AbilityTargetSpec { Trigger = 410, TargetsAffected = new[] { 10 } },
                                    });
            var mops2 = multi.TriggerOps("strike");
            Ok(mops2 != null && mops2.Count > 0 && mops2[0].SubjectTargetsAffected < 0,
               "🧪 B5 前提：2 条 ability 的卡 ⇒ 片段支**不给挂 `ta`**（`subjTa < 0`，与真卡 `GSC17` 同形）"
             + $"｜实得 {(mops2 == null || mops2.Count == 0 ? "null" : mops2[0].SubjectTargetsAffected.ToString())}");
            if (mops2 != null && mops2.Count > 0)
            {
                var ctx = NewCtx();
                var striker = Place(ctx, 0, 3, multi);
                var foeA = Place(ctx, 1, 3, Troop("B116W39FoeA2"));
                var foeB = Place(ctx, 1, 5, Troop("B116W39FoeB2"));
                var foeW = ctx.Players[1].Warlord;
                Ok(RuleCore.StrikeChosenOrNullForTest(striker, foeA) == null,
                   "★ B5a `ta` 未知 ⇒ 闸**不放行** `chosen`（返回 `null` = 不传）"
                 + "｜🧨 无条件放行 ⇒ 卡面「a **random** enemy」会被换成「被打者」（真卡 `GSC17` 实测就是这个变化）");
                var tl2 = Place(ctx, 0, 5, StrikeCard("B116W39Striker2", new[] { 10, 10, 10, 10, 10, 10, 10, 10 },
                                                    "Strike: Deal 1 damage to a random enemy, 8 times"));
                Ok(ReferenceEquals(RuleCore.StrikeChosenOrNullForTest(tl2, foeA), foeA),
                   "★ B5b `ta=10`（已知）⇒ 闸放行、原样返回那个 `chosen`（`TL30` 那一档）");
                var tauChk = Place(ctx, 0, 6, StrikeCard("B116W39TauChk", new[] { 30 },
                                                         "Strike: If the target survives, give it Markerlight 2"));
                Ok(ReferenceEquals(RuleCore.StrikeChosenOrNullForTest(tauChk, foeA), foeA),
                   "★ B5c `ta=30`（已知）⇒ 闸放行（`TAU18` 那一档）");

                // 🧨 端到端（**走生产接线** `RuleCore.DeclareAttack` ⇒ 7658 那一跳自己调闸）：
                //     攻击**敌方督军**，量两个敌方部队有没有吃到 Strike 的随机伤害。
                //     🧨 把 7058 的 `StrikeChosenOrNull(...)` 换成裸 `target` ⇒ 8 点全砸在督军、两支部队 -0 ⇒ 本行红。
                {
                    var ctx2 = NewCtx();
                    var atk = Place(ctx2, 0, 3, multi);
                    var e1 = Place(ctx2, 1, 3, Troop("B116W39FoeB2"));
                    var e2 = Place(ctx2, 1, 5, Troop("B116W39FoeC2"));
                    int h1 = e1.Health, h2 = e2.Health;
                    int rc = RuleCore.DeclareAttack(ctx2, 0, 3, 1, BoardSpec.WarlordSlot);
                    Ok(rc == RuleCodes.OK, "🧪 B5d 前提：`DeclareAttack` 打得出去（`rc == OK`）");
                    int l1 = h1 - e1.Health, l2 = h2 - e2.Health;
                    Ok(l1 > 0 || l2 > 0, $"★ B5d 端到端：Strike 的「a random enemy」**打到了部队**（B -{l1} / C -{l2}）"
                                        + "｜🧨 7058 无条件传 `chosen` ⇒ 8 点全砸在被攻击的那个督军、两支部队实得 -0");
                }
            }
        }
    }

    // ============================================================ C · `B-317`
    /// <summary>手牌陷阱：`When a unit dies, gain 2 Quest Points` + ability 级 `targetCriteria`（阵营闸）。
    /// 与 `RuleEngineTest_Priority.ENG14HandTrapWithGate` 同形（那一份测「候选在棋盘上」的两档）。</summary>
    static CardDef HandTrapWithGate(string id, int friendlyUnits, int enemyUnits)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        {
            new CardDef.AbilityTargetSpec { Trigger = 90, TargetsAffected = new[] { 10 } },
        };
        var rows = new List<CardDef.TargetCriteriaRowSpec>
        {
            new CardDef.TargetCriteriaRowSpec
            {
                Ability = 0, Trigger = 90, Logic = -1,
                Criteria = Set(10, friendly: friendlyUnits, enemy: enemyUnits, hero: 1, minions: 1),
            },
        };
        return new CardDef(id, id, "tactic", "When a unit dies, gain 2 Quest Points", "common", "Test",
                           cost: 1, attack: 0, health: 0, rangedAttack: 0, keywords: null,
                           abilityTriggers: new[] { 90 },
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           conditionsVersioned: true,
                           abilityLevelCriteria: rows.ToArray(),
                           abilityLevelCriteriaVersioned: true);
    }

    static int CountTrapTriggers(BattleContext ctx, int from, CardDef trap)
    {
        int n = 0;
        for (int i = from; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && e.CardId == trap.Name) n++;
        }
        return n;
    }

    static void SectionC_RealDeathFactionGate()
    {
        Sec("C · `B-317`：**真死之后**手牌陷阱的阵营闸仍然判得出来（候选已从棋盘摘掉）");

        var trap = HandTrapWithGate("B116W39Trap", friendlyUnits: 1, enemyUnits: 0);
        Ok(trap.HandTrapWhens.Count > 0, "🧪 C 前提：那句 `When a unit dies, …` 登记成手牌陷阱");
        string why0;
        Ok(trap.AbilityLevelCriteriaForTrigger("OtherUnitDeath", out why0) != null,
           "🧪 C 前提：ability 级 criteria 认得出（trigger 90 → ability 0）");

        // ---- C1：**敌人真死** ⇒ 阵营闸必须判「不满足」⇒ 0 条触发 ----
        {
            var foe = Troop("B116W39Foe");
            var ctx = NewCtx();
            var dead = Place(ctx, 1, 3, foe);
            ctx.Players[0].Hand.Add(ctx.NewInstance(trap));
            // 🔴 **模拟 `CleanupDeaths` 的真死那一拍**：先把单位从棋盘摘掉，**再**发死讯
            //    （`RuleCore.cs:10064` 的 `BoardSlots.RemoveAt` 早于 `:10218` 的 `BroadcastWhen(Die, …)`）。
            ctx.Players[1].Board[3] = null;
            Ok(dead.SeatKnown, "🧪 C1 前提：候选的座位还读得到（`UnitState.OwnerSeat` = 原版 `isPlayer` 的等价物，离场后仍保留）");
            int qp = ctx.Players[0].QuestPoints, sig = ctx.Signals.Count, warn = RuleCore.AbilityLevelCriteriaWarningsForTest;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 1, dead.Card, subject: dead);
            Eq(RuleCore.AbilityLevelCriteriaWarningsForTest - warn, 1,
               "★ C1 它**真走到闸、且被闸拒**（`AbilityLevelCriteriaWarningsForTest` +1）"
             + "｜🧨 不改 ⇒ 闸判不了（`SideOf` 扫不到棋盘）⇒ 计数不动、下面那条实得 1");
            Eq(CountTrapTriggers(ctx, sig, trap), 0,
               "★ C1 **敌方真死 ⇒ 0 条 `EvtKind.Trigger`**（`enemyUnits = 0` 不满足）"
             + "｜🧨 没有 `abilityTargetSide` ⇒ 闸回 `null` ⇒ 放行 ⇒ 实得 1");
            Eq(ctx.Players[0].QuestPoints, qp, "★ C1 陷阱正文**没结算**（任务点不动）");
        }

        // ---- C2：**友方真死** ⇒ 闸满足 ⇒ 照样触发（灭自证：「恒拒绝」的实现必须在这里红）----
        {
            var mine = Troop("B116W39Mine");
            var ctx = NewCtx();
            var dead = Place(ctx, 0, 3, mine);
            ctx.Players[0].Hand.Add(ctx.NewInstance(trap));
            ctx.Players[0].Board[3] = null;
            int qp = ctx.Players[0].QuestPoints, sig = ctx.Signals.Count;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, dead.Card, subject: dead);
            RuleCore.PumpActionQueues(ctx);
            Eq(CountTrapTriggers(ctx, sig, trap), 1, "★ C2 友方真死 ⇒ 发 **1 条** `EvtKind.Trigger`"
                                                    + "｜🧨 恒拒绝的实现 ⇒ 实得 0");
            Eq(ctx.Players[0].QuestPoints, qp + 2, "★ C2 陷阱正文真的结算（任务点 +2）");
        }

        // ---- C3：同一个闸、同一个离场候选，**只换 `candidateSide`** ⇒ 三态必须不同 ----
        {
            var foe = Troop("B116W39Foe2");
            var ctx = NewCtx();
            var dead = Place(ctx, 1, 3, foe);
            ctx.Players[1].Board[3] = null;
            string w1, w2;
            bool? withSide = RuleCore.AbilityLevelCriteriaAllows(ctx, 0, null, trap, "OtherUnitDeath", dead,
                                                                   out w1, ignoreRandom: false, candidateSide: 1);
            bool? noSide = RuleCore.AbilityLevelCriteriaAllows(ctx, 0, null, trap, "OtherUnitDeath", dead,
                                                                 out w2, ignoreRandom: false, candidateSide: null);
            Ok(withSide == false, "★ C3 给了侧（敌）⇒ 闸判**明确不满足**（`false`）"
                                + $"｜实得 {(withSide == null ? "null" : withSide.ToString())}");
            Ok(noSide == null, "★ C3 不给侧、候选又不在棋盘上 ⇒ 仍是 `null`（**判不了**）—— 三态没有被压成两种");
        }
    }
}
