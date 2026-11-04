// B141_W76_Assert.cs — 执行代理 `B141_W76`（第一百二十八会话 · 2026-11-04）· CHK-05 战斗域 L933 / L1107
//
// ============================ 它盯的是什么 ============================
//  账 `账/战斗逻辑驱动.tsv` 两行（1-based 含表头）的「真缺口」结论**经现核已不成立** ——
//  两处补丁早在 B123 / B120 波就已落地，本件把**判据本身**钉成断言，防以后被「顺手改回去」。
//
//  L933 · `CardScript.Stun`（附魔路转调）
//    · 判据：`d:/2/tools/decomp_full/CardScript__Stun.c:44-48 / :98 / :125`
//            （`cardState ∈ IsInPlay()` → `unstunnable` 跳过 → `AddTraitSilently(100)` → 清 `+0x55` → 状态演出）
//            ＋ `AbilityLogic__PlayAbility.c:642-657`（`ca 130` 逐目标 `AddStun`，**没有 jam 闸**）。
//    · 我们的落点：`RuleEngine/Core/EnchantmentExecutor_B115.cs` 的 `RunStunRowOfEnchantment`
//      → `ApplyStunTo(ctx, t, by, op)`（共用件 = `EffectResolverPartial_B123E1.cs:52`，`DoStun` 循环体的唯一实现）。
//    · 🔴 关键差别 = **绕开 `DoStun` 里的 `AddEffectCasterGates`**（闸② jam / 闸④ 施放者不在场，
//      `EffectResolver.cs:4434`）。原版 `ca 130` 的 `AddStun` 路径上没有这道闸。
//      ⇒ 断言用「把 `ctx.ActingUnit` 摆成一张**不在场**的部队」来区分两条实现（见 A 组 🧨）。
//
//  L1107 · `MinionManager.GetClosestAvailableSlot`
//    · 判据：`d:/2/tools/decomp_full/MinionManager__GetClosestAvailableSlot.c:95-143`
//            ＋ `cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`（`B120-W21` 主判据）。
//            候选集 = 每侧「最内空格 + 内圈已占格」（满侧整条剔除、不含督军位）；
//            距离 = 2D 欧氏屏幕像素；无容差；平手留更早下标。
//    · 我们的落点：`CardPresentation/Board/ArenaSlots.cs` 的 `IsCandidate` / `PickNearest`。
//    · 🔴 接线 = `BattleDriver.cs` 的 `TutorialScript.ResolveDeploySlot` 那一处必须**传每侧已占数**
//      （`OccupiedOnSideForDeploy(seat, BoardSlots.Left/Right)`）；不传 ⇒ 候选退化成「两侧皆空」
//      ⇒ 只有两侧最内空格可落（原版能落的地方我们会落错）。
//
// ============================ 断言 ============================
//  A 行为（L933）：附魔 `ca 130` 在「施放者不在场」时**照样**晕 —— 证明走的是共用件、不是 `DoStun`。
//  B 结构（L933）：`RunStunRowOfEnchantment` 的**代码**（剥注释后）有 `ApplyStunTo(`、没有 `DoStun(`。
//  C 行为（L1107）：`ArenaSlots.IsCandidate` / `PickNearest` 的候选集随「每侧已占数」收窄。
//  D 结构（L1107）：`BattleDriver.cs` 里 `ResolveDeploySlot` 那一处真的喂了两个占位数。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · A2：把 `RunStunRowOfEnchantment` 的调用换回 `DoStun(...)` ⇒ `ctx.ActingUnit` 那张**不在场**的部队
//        会被闸④ 挡住 ⇒ 目标不晕、返回 0 ⇒ A2/A3 红。
//  · A4：把 `ApplyStunTo` 里的 `StunBlockedByTraits` 那一段删掉 ⇒ 「unstunnable 单位也晕」⇒ A4 红。
//  · B1：附魔执行体里出现 `DoStun(` ⇒ 红（哪怕行为上一时看不出来）。
//  · C1/C2/C3：把 `IsCandidate` 的 `idx <= count` 改成 `idx == count`（只留最内空格）⇒ C2 红；
//              改成恒 `true`（九格全算）⇒ C1/C3 红。
//  · C4：`PickNearest` 不把占位数传给 `IsCandidate`（等价于「两侧皆空」）⇒ C4 红。
//  · D1：`ResolveDeploySlot` 改回两参 `TryResolveSlot(pos, out hit)` ⇒ 红。
//
// ============================ 怎么接（宿主聚合由调度台做）============================
//  `public static void Run();` —— **零前提**（只建内存对局 + 读源码文本，不建窗口、不碰存档）。
//  挂法：`Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加
//      `B141_W76_Assert.Run(); _pass += B141_W76_Assert.Pass; _fail += B141_W76_Assert.Fail;`
//  （⚠️ 追加文本必须落在该行第一个 `//` **之前**；本文件白名单外，接线由调度台统一做。）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B141_W76`：CHK-05 战斗域 L933（附魔眩晕转调共用件）/ L1107（脚本出牌落点占位数）的回归断言。</summary>
public static class B141_W76_Assert
{
    const string Tag = "B141W76";
    const int Trig = 640;             // 模板行挂一个存在的 trigger（只测 `ca` 分派，不测派发闸）
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_L933_Behavior();
        GroupB_L933_SourceGate();
        GroupC_L1107_CandidateSet();
        GroupD_L1107_SourceGate();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · L933 行为：施放者不在场，附魔 `ca 130` 照样晕（= 走共用件、绕开 DoStun 的闸）
    // ==================================================================
    static void GroupA_L933_Behavior()
    {
        Debug.Log("--- A · L933 附魔 ca 130：转调 ApplyStunTo（绕开 DoStun 的 AddEffectCasterGates）---");

        // A0 对照：没有残骸施放者时也得晕（证明本组不是靠「异常路径」凑出来的）
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null || opp == null) { Report(false, "A0 前提不成立：对局 / 载体 / 目标没建起来"); return; }
            var tpl = Template("B141W76Tpl130Ctl", Trig, 130, 0, 10, Criteria(10));
            var e = Attach(ctx, carrier, tpl);
            if (e == null) { Report(false, "A0b 前提不成立：附魔没挂上"); return; }
            int n0 = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
            Check(n0, 1, "A0 ★★ 对照（无残骸施放者）：`ca 130` 晕了 1 个单位");
            CheckRaw(opp.IsStunned, "A0b ★★ 对照：目标真的带上了 stun（`AddKeyword(Stun)` 跑到了）");
        }

        // A1~A4：把 `ctx.ActingUnit` 换成一张**不在场**的部队 —— 这正是 `DoStun` 闸④ 的触发条件
        var ctx2 = MakeBattle(out var carrier2, out var opp2);
        if (ctx2 == null || carrier2 == null || opp2 == null) { Report(false, "A1 前提不成立：第二场没建起来"); return; }

        var ghost = new UnitState(ctx2.NewInstance(PlainUnit("B141W76Ghost")), false);
        CheckRaw(!BoardHas(ctx2, 0, ghost) && !BoardHas(ctx2, 1, ghost),
                 "A1 ★★ 前提：那张「施放者」**不在任何一方的棋盘上**（= 原版 `card no longer in play` 那一档）");
        ctx2.ActingUnit = ghost;
        try
        {
            var tpl2 = Template("B141W76Tpl130Ghost", Trig, 130, 0, 10, Criteria(10));
            var e2 = Attach(ctx2, carrier2, tpl2);
            if (e2 == null) { Report(false, "A1b 前提不成立：附魔没挂上"); return; }

            int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e2, carrier2, 0, 3, Trig, "EarnedSkull", null, null);

            Check(n, 1,
                  "A2 ★★★ **结论（L933）**：施放者不在场时，附魔 `ca 130` **照样**执行并返回 1"
                + "｜🧨 把 `RunStunRowOfEnchantment` 的调用换回 `DoStun(...)` ⇒ 闸④（`AddEffectCasterGates`）"
                + "会因 `ctx.ActingUnit` 这张不在场的部队挡住 ⇒ 返回 0 ⇒ 本行红");
            CheckRaw(opp2.IsStunned,
                     "A3 ★★★ **结论（L933）**：目标真的晕上了（原版 `PlayAbility.c:642-657` 的逐目标 `AddStun` 没有 jam/施放者闸）"
                   + "｜🧨 同上：换回 `DoStun` ⇒ 本行红");
            CheckRaw(!carrier2.IsStunned, "A3b ★★ 反证：载体自己没被晕（目标集 = `enemyUnits=1`，不是双方全场）");

            // A4 共用件内部的 `unstunnable` 闸仍在（证明「绕开 DoStun」没有把判据一起扔掉）
            var ctx3 = MakeBattle(out var carrier3, out var opp3);
            if (ctx3 != null && carrier3 != null && opp3 != null)
            {
                opp3.AddKeyword(RuleCore.Unstunnable, 1);     // 原版 `DefinedTrait.unstunnable = 250`（`RuleCore.cs:5217`）
                var tpl3 = Template("B141W76Tpl130Unstun", Trig, 130, 0, 10, Criteria(10));
                var e3 = Attach(ctx3, carrier3, tpl3);
                int n3 = e3 != null
                       ? RuleCore.RunEnchantmentLogicRowsForTest(ctx3, e3, carrier3, 0, 3, Trig, "EarnedSkull", null, null)
                       : -1;
                Check(n3, 0,
                      "A4 ★★★ **灭自证**：带 `unstunnable` 的目标**没被晕**（返回 0）"
                    + "｜🧨 把 `ApplyStunTo` 里 `StunBlockedByTraits` 那一段删掉 ⇒ 返回 1 ⇒ 红");
                CheckRaw(!opp3.IsStunned, "A4b ★★ 反证：`IsStunned` 字段也没被写上");
            }
            else Report(false, "A4 前提不成立：第三场没建起来");
        }
        finally
        {
            ctx2.ActingUnit = null;     // ⛔ 别把夹具状态漏给后面的组
        }
    }

    // ==================================================================
    //  B · L933 结构：附魔执行体只转调共用件
    // ==================================================================
    static void GroupB_L933_SourceGate()
    {
        Debug.Log("--- B · L933 源码闸：`RunStunRowOfEnchantment` 调 `ApplyStunTo(`、不调 `DoStun(` ---");
        string path = Path.Combine(Application.dataPath, "RuleEngine/Core/EnchantmentExecutor_B115.cs");
        if (!File.Exists(path)) { Report(false, "B0 前提不成立：读不到 " + path); return; }

        string code = StripComments(File.ReadAllText(path));
        int i0 = code.IndexOf("int RunStunRowOfEnchantment(", StringComparison.Ordinal);
        CheckRaw(i0 >= 0, "B0 前提：切得到 `RunStunRowOfEnchantment` 的方法头");
        if (i0 < 0) return;

        int i1 = code.IndexOf("int RunStealMinionSelfRowOfEnchantment(", StringComparison.Ordinal);
        CheckRaw(i1 > i0, "B0b 前提：切得到方法体尾（下一个方法头在它后面）");
        if (i1 <= i0) return;

        string body = code.Substring(i0, i1 - i0);
        CheckRaw(body.IndexOf("ApplyStunTo(", StringComparison.Ordinal) >= 0,
                 "B1 ★★★ **结论（L933）**：附魔 `ca 130` 的施加段是 `ApplyStunTo(`（共用件 = `CardScript__Stun.c` 的唯一实现）"
               + "｜🧨 删掉这一行转调 ⇒ 红");
        CheckRaw(body.IndexOf("DoStun(", StringComparison.Ordinal) < 0,
                 "B2 ★★★ **灭自证**：方法体（剥注释后）**没有** `DoStun(` —— 附魔路不走带 jam/施放者闸的那一支"
               + "｜🧨 改回 `DoStun(...)` ⇒ 红（行为面 A2/A3 同时红）");
    }

    // ==================================================================
    //  C · L1107 行为：候选集随「每侧已占数」收窄
    // ==================================================================
    static void GroupC_L1107_CandidateSet()
    {
        Debug.Log("--- C · L1107 落点候选集：每侧已占数决定哪些格可落 ---");

        // C1：两侧都空 ⇒ 只有两侧最内格（3 / 5）
        CheckRaw(SameSet(Candidates(0, 0), new[] { 3, 5 }),
                 "C1 ★★★ **结论（L1107）**：两侧皆空 ⇒ 候选 = {3,5}（每侧最内空格）"
               + "｜判据 `MinionManager__GetClosestAvailableSlot.c:95-143` 的 `start = 4−已占−1` / `end = 4+1+已占`");

        // C2：左侧已占 2 ⇒ 左半「最内空格 + 内圈已占格」都进候选
        CheckRaw(SameSet(Candidates(2, 0), new[] { 1, 2, 3, 5 }),
                 "C2 ★★★ **结论（L1107）**：左已占 2 ⇒ 左候选 = {1,2,3}（内圈已占格 + 最内空格）、右 = {5}"
               + "｜🧨 把 `IsCandidate` 的 `idx <= count` 改成 `idx == count` ⇒ 左只剩 {3} ⇒ 红");

        // C3：满侧整条剔除
        CheckRaw(SameSet(Candidates(4, 0), new[] { 5 }),
                 "C3 ★★★ **结论（L1107）**：左侧满（4）⇒ 左半整条剔除，候选只剩 {5}"
               + "｜🧨 去掉 `count >= SlotsPerSide` 那道闸 ⇒ 左侧仍出格 ⇒ 红");

        // C3b：督军位恒不是候选
        CheckRaw(!ArenaSlots.IsCandidate(ArenaSlots.WarlordSlot, 0, 0),
                 "C3b ★★ 督军位（4 号）**恒不是候选**（原版候选数组里根本没有它）");

        // C4：同一个指针位置，喂不喂占位数**读数不同** —— 这一条就是「不传占位数 ⇒ 退化」的证明
        var pts = new Vector2[ArenaSlots.SlotCount];
        for (int s = 0; s < pts.Length; s++) pts[s] = new Vector2(s * 100f, 0f);
        var nearInner = new Vector2(1 * 100f, 0f);      // 指针落在「左内圈第 3 格（= 格 1）」上

        int withOcc = ArenaSlots.PickNearest(pts, nearInner, 2, 0);
        int degenerate = ArenaSlots.PickNearest(pts, nearInner, -1, -1);   // `-1` = 数不出来 ⇒ 「该侧皆空」
        int bothEmpty = ArenaSlots.PickNearest(pts, nearInner, 0, 0);

        Check(withOcc, 1,
              "C4 ★★★ **结论（L1107）**：喂了「左已占 2」⇒ 指针压在最内那格上就落到**格 1**"
            + "｜🧨 `PickNearest` 不把占位数传给 `IsCandidate` ⇒ 落到格 3 ⇒ 红");
        Check(degenerate, 3,
              "C5 ★★★ **灭自证**：同一位置、**不喂占位数**（`-1,-1`）⇒ 落到 **格 3**（= 候选退化成「两侧皆空」）"
            + "⇒ 这两个读数必须**不同**，否则 C4 证明不了「占位数是承重的」");
        Check(bothEmpty, degenerate,
              "C5b ★★ `-1` 与 `0` 逐字同解（都按「该侧皆空」处理，⛔ 不静默猜成「满」）");

        // C6：两侧都满 ⇒ 无候选（原版同一情形返回 0 = 无）
        Check(ArenaSlots.PickNearest(pts, nearInner, 4, 4), -1,
              "C6 ★★ 两侧皆满 ⇒ `PickNearest` = -1（原版 `GetClosestAvailableSlot` 返回 0 = 无）");
    }

    static List<int> Candidates(int occL, int occR)
    {
        var l = new List<int>();
        for (int s = 0; s < ArenaSlots.SlotCount; s++)
            if (ArenaSlots.IsCandidate(s, occL, occR)) l.Add(s);
        return l;
    }

    static bool SameSet(List<int> got, int[] want)
    {
        if (got == null || want == null) return false;
        if (got.Count != want.Length) return false;
        for (int i = 0; i < want.Length; i++) if (got[i] != want[i]) return false;
        return true;
    }

    // ==================================================================
    //  D · L1107 结构：`TutorialScript.ResolveDeploySlot` 真的喂了占位数
    // ==================================================================
    static void GroupD_L1107_SourceGate()
    {
        Debug.Log("--- D · L1107 源码闸：`BattleDriver` 的脚本落点解析喂了两个占位数 ---");
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        if (!File.Exists(path)) { Report(false, "D0 前提不成立：读不到 " + path); return; }

        string code = StripComments(File.ReadAllText(path));
        int i0 = code.IndexOf("TutorialScript.ResolveDeploySlot = (c, seat)", StringComparison.Ordinal);   // 赋值处（⛔ 不是 `visit(...)` 那一行）
        CheckRaw(i0 >= 0, "D0 前提：找得到 `TutorialScript.ResolveDeploySlot` 的赋值处");
        if (i0 < 0) return;

        int i1 = code.IndexOf("BuildHud();", i0, StringComparison.Ordinal);
        CheckRaw(i1 > i0, "D0b 前提：切得到这段委托体的收尾（`BuildHud()` 在它后面）");
        if (i1 <= i0) return;

        string block = code.Substring(i0, i1 - i0);
        CheckRaw(block.IndexOf("TryResolveSlot(", StringComparison.Ordinal) >= 0,
                 "D1a ★★ 这段委托体里确实有 `TryResolveSlot(`（落点解析那一跳）");
        CheckRaw(block.IndexOf("OccupiedOnSideForDeploy(seat, BoardSlots.Left)", StringComparison.Ordinal) >= 0,
                 "D1 ★★★ **结论（L1107）**：左侧已占数真的喂进去了（`OccupiedOnSideForDeploy(seat, BoardSlots.Left)`）"
               + "｜🧨 改回两参 `TryResolveSlot(pos, out hit)` ⇒ 红（候选退化成「两侧皆空」）");
        CheckRaw(block.IndexOf("OccupiedOnSideForDeploy(seat, BoardSlots.Right)", StringComparison.Ordinal) >= 0,
                 "D2 ★★★ **结论（L1107）**：右侧已占数也喂进去了（成对；漏一侧 ⇒ 那一侧退化）"
               + "｜🧨 只传左侧 ⇒ 红");

        // D3：这条委托体里**不许**出现「两参」的裸调用 —— 正向判据 = `out hit` 之后**还有实参**（逗号）
        CheckRaw(block.IndexOf("out hit,", StringComparison.Ordinal) >= 0,
                 "D3 ★★★ **灭自证**：`out hit` 之后**还有实参**（= 走的是带占位数的那个重载）"
               + "｜🧨 改回两参 `TryResolveSlot(pos, out hit)` ⇒ 这里是 `out hit)` ⇒ 红");

        // D4：取值口本身在不在（它返回 -1 = 取不到该方引擎状态，消费方按「该侧皆空」处理）
        CheckRaw(code.IndexOf("int OccupiedOnSideForDeploy(int seat, int side)", StringComparison.Ordinal) >= 0,
                 "D4 ★★ 取值口 `OccupiedOnSideForDeploy(int seat, int side)` 在（`-1` 语义见它的 doc）");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张附魔模板卡：1 条 ability / 1 行 logic；`crit` 非 null 时一起挂 36 格。</summary>
    static CardDef Template(string id, int trigger, int ca, int pa, int ta, CardDef.CountSetSpec crit)
    {
        var c = new CardDef(id, id, "enchantment", "", "", "", 0, 0, 0, 0, null);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec
            { Ab = 0, Lg = 0, Trigger = trigger, TriggerValue = 0, Ca = ca, Ta = ta, Pa = pa, Fm = 0, Mult = 0 },
        });
        c.AttachAbilityTriggers(new[] { trigger });
        if (crit != null)
            c.AttachAbilityTargetCriteria(new[]
            {
                new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = trigger, Logic = 0, Criteria = crit },
            });
        return c;
    }

    /// <summary>一份**全格都已知**的 36 格 criteria（`-1` 会让 `TargetCriteriaEval` 返回「判不了」）。
    /// 缺省 = 「明确不筛」的全 0；调用方按需改那几格（本件只用来选一个敌方部队）。</summary>
    static CardDef.CountSetSpec Criteria(int ta)
    {
        var s = new CardDef.CountSetSpec();
        s.PlayerTurn = -1;                     // 结构上不适用（同生成器：`has_player_turn=False`）
        s.TargetsAffected = ta;
        s.ChosenArmy = 0; s.ArmyFilter = 0; s.KeywordFilter = 0; s.TraitsFilter = 0;
        s.MaxTargets = 0; s.TargetSelectMethod = 0;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = 0; s.ManaCostFilterOption = 0;
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.FriendlyUnits = 0; s.EnemyUnits = 1; s.IncludeMinions = 1;   // ← 本件唯一的筛：一个敌方部队
        s.IncludeHero = 0; s.IncludeSpells = 0; s.IncludeWhispers = 0;
        s.IncludeWeapons = 0; s.IncludeRemnants = 0; s.IncludeHeroParts = 0;
        s.IncludeSelf = 0; s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0; s.ExcludeRace = 0; s.ExcludeTrait = 0;
        s.SelectFromChooseResolution = 0;
        s.FilterSpecificCardIds = new string[0];
        s.ExcludeEnchantmentIds = new string[0];
        return s;
    }

    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>建一局：双方各 6 张填充卡 + 一张督军；0 号玩家格 3 放载体、1 号玩家格 3 放一个敌兵。</summary>
    static BattleContext MakeBattle(out UnitState carrier, out UnitState opp)
    {
        carrier = null; opp = null;
        var heroA = new CardDef("B141W76HeroA", "B141W76HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B141W76HeroB", "B141W76HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var carrierCard = PlainUnit("B141W76Carrier");
        var deckA = new List<CardDef> { heroA, carrierCard };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B141W76FillA" + i)); deckB.Add(PlainUnit("B141W76FillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        if (ctx == null) return null;
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        opp = PlaceUnit(ctx, 1, 3, PlainUnit("B141W76Opp"));
        return ctx;
    }

    static UnitState PlaceUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static bool BoardHas(BattleContext ctx, int p, UnitState u)
    {
        if (ctx == null || u == null || p < 0 || p > 1) return false;
        var b = ctx.Players[p].Board;
        for (int i = 0; i < b.Length; i++) if (ReferenceEquals(b[i], u)) return true;
        return false;
    }

    /// <summary>挂一件附魔（唯一写口 = `Enchantments.Attach`）。</summary>
    static ActiveEnchantment Attach(BattleContext ctx, UnitState carrier, CardDef tpl)
    {
        if (ctx == null || carrier == null || carrier.Instance == null || tpl == null) return null;
        var eff = Enchantments.Attach(ctx, carrier.Instance, tpl, Enchantments.BuffTypeSingleUse);
        return eff != null ? eff.Enchantment : null;
    }

    // ==================================================================
    //  源码文本处理
    // ==================================================================

    /// <summary>去掉 `//` 与 `/* */` 注释（保留字符串内容）—— 注释里的示例不算接线。</summary>
    static string StripComments(string s)
    {
        var sb = new StringBuilder(s.Length);
        bool line = false, block = false, str = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char n = i + 1 < s.Length ? s[i + 1] : '\0';
            if (line)
            {
                if (c == '\n' || c == '\r') { line = false; sb.Append(c); }
                continue;
            }
            if (block)
            {
                if (c == '*' && n == '/') { block = false; i++; }
                continue;
            }
            if (str)
            {
                sb.Append(c);
                if (c == '\\' && n != '\0') { sb.Append(n); i++; continue; }
                if (c == '"') str = false;
                continue;
            }
            if (c == '/' && n == '/') { line = true; i++; continue; }
            if (c == '/' && n == '*') { block = true; i++; continue; }
            if (c == '"') { str = true; sb.Append(c); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ==================================================================
    //  最小断言框架
    // ==================================================================

    /// <summary>值相等（泛型版）。</summary>
    static void Check<T>(T actual, T expected, string msg)
    {
        Report(EqualityComparer<T>.Default.Equals(actual, expected),
               msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    /// <summary>纯布尔（没有「期望值」可比的那一类）。</summary>
    static void CheckRaw(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }
}
