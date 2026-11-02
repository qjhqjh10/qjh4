// B121_E03F_VFX_Assert.cs — 执行写手 `B121_E03F_VFX`（第一百二十一会话）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `B121-E03F`：**督军变身演出** —— 用户 2026-11-03 裁定「督军变身的特效/动画，按照你的建议做一个自我设计」。
//   本件钉五件事：① 事件**只在换人成功时**发（三条失败闸一条都不许发）
//                ② 三拍参数的**每一个拐点**（0 / 0.15 / 0.30 / 0.375 / 0.45 / 0.765 / 0.90）
//                ③ **不冻结输入** ④ 新督军终态（alpha 1 · 缩放 1）⑤ `🔶 自己设计` 与「查不到」的记录在位。
//
// ============================ 判据（路径，⛔ 别在这里抄第二份）============================
//   · 三拍参数 = `CardPresentation/Battle/TransformPresentation.cs` 的常量与 `PoseAt`（**唯一出处**）
//   · 事件字段 = `RuleEngine/Core/BattleEvent.cs` 的 `EvtKind.WarlordTransform` doc
//   · 发出点 = `RuleEngine/Core/RuleCorePartial_B96CB.cs` 的 `SwapWarlordCard`（`return true` 之前）
//   · 登记 = `CardPresentation/Core/EventTiming.cs` 的 `ZeroDurationKinds` + `SourceOf` 的 `case`
//   · 原版为什么**没有**这段演出 → `RuleCorePartial_B96CB.cs` 文件末「§变身」那一大段（本条不重抄）
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//   🧨 1：A 组「成功 ⇒ 1 条」与 A 组「失败 ⇒ 0 条」是**同一个方法**的两个出口 —— 把三条闸删掉（恒发）⇒
//         A2/A3/A4/A5 红；把发事件挪到闸前 ⇒ 那几条红。
//   🧨 2：B 组逐拐点采样 0/0.15/0.30/0.375/0.45/0.90 —— 把三拍改成「一拍到底」或改任一常量 ⇒ 至少两格红。
//   🧨 3：C 组「本档不挡输入」配一条**对照**（`Attack` 必须挡住）—— 把 `BlocksPlayerActions` 写成恒 false ⇒ 对照格红。
//   🧨 4：D 组「登记过」配一条「未登记计数不涨」—— 把 `ZeroDurationKinds` 那一格删掉 ⇒ 三格同时红。
//
// ============================ 怎么接 ============================
//   宿主聚合行末尾追加（**同一物理行** ⇒ 行数零变化）：
//      B121_E03F_VFX_Assert.Run(); pass += B121_E03F_VFX_Assert.Pass; fail += B121_E03F_VFX_Assert.Fail;
//   ⚠️ **零前提**：A 组自己 `RuleCore.NewBattle` 建局；B/C/D/E 组是纯函数 + 源码闸（不建 HUD、不跑帧）。
//      `Play` 的**冒烟**那一条故意传 `null` 视图 ⇒ 不需要 HUD；真机那一层由调度台在同步点验。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;
using CardPresentation;

/// <summary>执行写手 `B121_E03F_VFX`：`B121-E03F` 督军变身演出（事件 / 三拍 / 不冻结输入 / 终态 / 自设计标记）。</summary>
public static class B121_E03F_VFX_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    const string P = "[B121E03F] ";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void Eq(int got, int want, string msg)
    {
        Check(got == want, msg + (got == want ? "" : $" —— 期望 [{want}]，实得 [{got}]"));
    }

    static void Near(float got, float want, float eps, string msg)
    {
        Check(Mathf.Abs(got - want) <= eps,
              msg + (Mathf.Abs(got - want) <= eps ? "" : $" —— 期望 [{want:F3}]，实得 [{got:F3}]"));
    }

    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log(P + "=== B121_E03F 开始（督军变身演出 · 🔶 自己设计 · 用户 2026-11-03 裁定）===");
        try
        {
            GroupA_Emit();
            GroupB_Beats();
            GroupC_NoInputFreeze();
            GroupD_RegistryAndWiring();
            GroupE_SelfDesign();
            GroupF_PlanAndPlay();
        }
        catch (Exception e)
        {
            _fail++;
            Debug.LogError(P + "✗ 未捕获异常（本组余下的断言不会执行）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        }
    }

    // ==================================================================
    //  A · 事件只在**换人成功**时发（同一个方法的两个出口，互为反证）
    // ==================================================================
    static void GroupA_Emit()
    {
        Debug.Log(P + "--- A · `SwapWarlordCard` 成功发一条 / 三条失败闸一条都不发 ---");
        var ctx = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);

        // 开局链（`CheckForHeroDeckTransform`）今天恒走「两列数据没有 ⇒ 不换」⇒ 一条都不该有
        var pre = Take(ctx);
        Eq(CountTransform(pre), 0, "★ A0 开局建局**不发**变身事件（`transformedWarlordId` / `deckCriteria` 两列本机没有 ⇒ 不换）");

        string oldName = ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
                       ? ctx.Players[0].Warlord.Card.Name : null;
        var hero = Hero("FixtureHeroB", 3, 25);
        bool ok = RuleCore.SwapWarlordCardForTest(ctx, 0, hero);
        var buf = Take(ctx);
        var evs = Only(buf);
        Check(ok, "[A1 前提] 换人成功（`SwapWarlordCardForTest` 回 true）");
        Eq(evs.Count, 1, "★ A1 换人成功 ⇒ **恰好一条** `EvtKind.WarlordTransform`｜🧨 恒不发 ⇒ 本行红；挪到闸前 ⇒ A3/A4 红");
        if (evs.Count != 1) { Check(false, "[A1b] 没拿到那一条事件 ⇒ 本组余下断言无法执行（先修 A1）"); return; }

        var e = evs[0];
        Check(e.Player == 0 && e.Slot == BoardSpec.WarlordSlot,
              $"★ A2 字段：`Player` = 0（变身那一方）· `Slot` = {BoardSpec.WarlordSlot}（督军格）；实得 {e.Player} / {e.Slot}");
        Check(string.Equals(e.CardId, "FixtureHeroB", StringComparison.Ordinal),
              "★ A2b `CardId` = **新**督军卡名（`FixtureHeroB`）");
        Check(string.Equals(e.Effect, oldName, StringComparison.Ordinal) && !string.IsNullOrEmpty(oldName),
              $"★ A2c `Effect` = **旧**督军卡名（「{oldName}」）—— 溶解那一段要靠它找卡");
        Check(e.InstanceId != 0 && ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Instance != null
              && e.InstanceId == ctx.Players[0].Warlord.Instance.Id,
              "★ A2d `InstanceId` = 新督军那一份的实例号（同名卡不会记错到别的一份上）");
        Check(ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
              && string.Equals(ctx.Players[0].Warlord.Card.Name, "FixtureHeroB", StringComparison.Ordinal),
              "★ A2e 引擎侧**真的换了人**（事件不是空转）");

        // 再换一次：`Effect` 必须跟着变成「上一次那位新督军」
        RuleCore.SwapWarlordCardForTest(ctx, 0, Hero("FixtureHeroC", 4, 20));
        var evs2 = Only(Take(ctx));
        Check(evs2.Count == 1 && string.Equals(evs2[0].Effect, "FixtureHeroB", StringComparison.Ordinal)
              && string.Equals(evs2[0].CardId, "FixtureHeroC", StringComparison.Ordinal),
              "★ A2f 第二次变身：`Effect` = 「FixtureHeroB」（上一次那位）、`CardId` = 「FixtureHeroC」"
            + " ⇒ 两格**语义各不相同**，不是同一个字段抄两遍");

        // ---- 三条失败闸（各自 0 条）----
        var c2 = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
        Take(c2);
        Check(!RuleCore.SwapWarlordCardForTest(c2, 0, null) && CountTransform(Take(c2)) == 0,
              "★ A3 **反证**：`hero == null` ⇒ false 且**不发**｜🧨 把 null 闸删掉 ⇒ 本行红（会建一个空督军）");
        var c3 = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
        Take(c3);
        Check(!RuleCore.SwapWarlordCardForTest(c3, 7, Hero("FixtureHeroD", 1, 1)) && CountTransform(Take(c3)) == 0,
              "★ A4 **反证**：座位越界（7）⇒ false 且**不发**（`seat >= ctx.Players.Length` 那道闸）");
        Check(!RuleCore.SwapWarlordCardForTest(null, 0, Hero("FixtureHeroE", 1, 1)),
              "★ A5 **反证**：`ctx == null` ⇒ false（不崩、不发）｜🧨 删掉 `ctx == null` 那道闸 ⇒ NRE");
    }

    // ==================================================================
    //  B · 三拍参数（纯函数逐拐点采样）
    // ==================================================================
    static void GroupB_Beats()
    {
        Debug.Log(P + "--- B · 三拍：0.30 / 0.45 / 0.90 的每个拐点 ---");
        Near(TransformPresentation.DissolveEnd, 0.30f, 1e-6f, "★ B0a 第一拍结束 = 0.30 s");
        Near(TransformPresentation.FlashEnd, 0.45f, 1e-6f, "★ B0b 第二拍结束 = 0.45 s");
        Near(TransformPresentation.Total, 0.90f, 1e-6f, "★ B0c 整条 = 0.90 s");
        Near(TransformPresentation.MinUnskippable, 0.45f, 1e-6f, "★ B0d **最小不可跳过 = 0.45 s**（溶解 + 闪白两拍）");

        var p0 = TransformPresentation.PoseAt(0f);
        Near(p0.OldAlpha, 1f, 1e-6f, "★ B1 t=0：旧督军 alpha = 1");
        Near(p0.OldScale, 1f, 1e-6f, "★ B1b t=0：旧督军缩放 = 1");
        Near(p0.OldRise, 0f, 1e-6f, "★ B1c t=0：旧督军上浮 = 0");
        Near(p0.NewAlpha, 0f, 1e-6f, "★ B1d t=0：新督军 **alpha = 0**（还没出现）");
        Near(p0.NewScale, 0.90f, 1e-6f, "★ B1e t=0：新督军缩放 = 0.90（materialize 起点）");
        Near(p0.FlashAlpha, 0f, 1e-6f, "★ B1f t=0：白光 = 0");

        var pMid = TransformPresentation.PoseAt(0.15f);
        Check(pMid.OldAlpha > 0.05f && pMid.OldAlpha < 0.95f,
              $"★ B2 t=0.15 溶解**在途中**（旧 alpha = {pMid.OldAlpha:F3}，既不是 1 也不是 0）");
        Check(pMid.OldScale < 1f && pMid.OldScale > 0.90f,
              $"★ B2b t=0.15 旧缩放落在 (0.90, 1) 之间（实得 {pMid.OldScale:F3}）—— 终点 {TransformPresentation.OldEndScale:F2}");
        Check(pMid.OldRise > 0f && pMid.OldRise < TransformPresentation.OldRiseY,
              $"★ B2c t=0.15 上浮在途中（{pMid.OldRise:F3} ∈ (0, {TransformPresentation.OldRiseY:F2})）");
        var pEarly = TransformPresentation.PoseAt(0.075f);
        Check(pEarly.OldAlpha > pMid.OldAlpha,
              "★ B2d 溶解**单调递减**（t=0.075 的 alpha 比 t=0.15 更大）｜🧨 换成单调递增 ⇒ 本行红");
        Near(pMid.NewAlpha, 0f, 1e-6f, "★ B2e 溶解段新督军始终不可见");

        var pEndOut = TransformPresentation.PoseAt(TransformPresentation.DissolveEnd - 1e-3f);
        Check(pEndOut.OldAlpha < 0.01f, $"★ B3 t→0.30⁻：旧督军**基本溶完**（alpha = {pEndOut.OldAlpha:F4}）");
        Check(TransformPresentation.PhaseAt(TransformPresentation.DissolveEnd) == TransformPresentation.Phase.Flash,
              "★ B3b 0.30 整 ⇒ 进第二拍（闪白）");

        var pFlashPeak = TransformPresentation.PoseAt((TransformPresentation.DissolveEnd + TransformPresentation.FlashEnd) * 0.5f);
        Near(pFlashPeak.FlashAlpha, TransformPresentation.FlashPeakAlpha, 1e-4f,
             "★ B4 闪白**峰值 0.85** 落在 0.375 s（Sin 包络的中点）");
        Near(TransformPresentation.PoseAt(TransformPresentation.DissolveEnd).FlashAlpha, 0f, 1e-5f,
             "★ B4b 闪白起点（0.30）alpha = 0");
        Near(TransformPresentation.PoseAt(TransformPresentation.FlashEnd).FlashAlpha, 0f, 1e-5f,
             "★ B4c 闪白终点（0.45）alpha = 0");
        Check(pFlashPeak.NewAlpha == 0f && pFlashPeak.NewScale == TransformPresentation.NewStartScale,
              "★ B4d 闪白段：新督军仍不可见、缩放仍在 0.90（**白光先于 materialize**）");

        var pIn0 = TransformPresentation.PoseAt(TransformPresentation.FlashEnd);
        Near(pIn0.NewAlpha, 0f, 1e-5f, "★ B5 materialize 起点（0.45）：新 alpha = 0");
        Near(pIn0.NewScale, TransformPresentation.NewStartScale, 1e-5f, "★ B5b materialize 起点缩放 = 0.90");

        // 超调：在 materialize 段扫一遍找峰值
        float maxScale = 0f, maxAt = -1f;
        for (int i = 0; i <= 450; i++)
        {
            float t = TransformPresentation.FlashEnd + (TransformPresentation.Total - TransformPresentation.FlashEnd) * (i / 450f);
            float sc = TransformPresentation.PoseAt(t).NewScale;
            if (sc > maxScale) { maxScale = sc; maxAt = t; }
        }
        Near(maxScale, TransformPresentation.NewOvershoot, 2e-3f,
             $"★ B6 缩放**超调峰值 = {TransformPresentation.NewOvershoot:F2}**（扫全段实得 {maxScale:F4}）");
        Near(maxAt, TransformPresentation.FlashEnd + (TransformPresentation.Total - TransformPresentation.FlashEnd) * TransformPresentation.OvershootAt,
             5e-3f, $"★ B6b 峰值出现在 {maxAt:F3} s（= 0.45 + 0.45 × {TransformPresentation.OvershootAt:F2}）");

        bool mono = true;
        float prevA = -1f;
        for (int i = 0; i <= 450; i++)
        {
            float t = TransformPresentation.FlashEnd + (TransformPresentation.Total - TransformPresentation.FlashEnd) * (i / 450f);
            float a = TransformPresentation.PoseAt(t).NewAlpha;
            if (a < prevA - 1e-6f) mono = false;
            prevA = a;
        }
        Check(mono && Mathf.Abs(prevA - 1f) < 1e-5f,
              "★ B7 materialize 的 alpha **单调不减**且终点 = 1｜🧨 改成往复补间 ⇒ 本行红");

        Check(TransformPresentation.PhaseAt(TransformPresentation.Total) == TransformPresentation.Phase.Done,
              "★ B8 t=0.90 ⇒ 进终态（`Phase.Done`）");
        var pd = TransformPresentation.PoseAt(1.7f);
        Near(pd.OldAlpha, 0f, 1e-6f, "★ B8b 终态之后**旧督军恒不可见**（t=1.7 采样）");
        Near(pd.FlashAlpha, 0f, 1e-6f, "★ B8c 终态之后白光恒 0");
        Near(pd.NewAlpha, 1f, 1e-6f, "★ B8d 终态之后新督军 alpha 恒 1");
        Near(pd.NewScale, 1f, 1e-6f, "★ B8e 终态之后新督军缩放恒 1（超调已收回）");

        bool finite = true;
        for (int i = 0; i <= 90; i++)
        {
            var p = TransformPresentation.PoseAt(i * 0.01f);
            if (float.IsNaN(p.OldAlpha) || float.IsNaN(p.NewAlpha) || float.IsNaN(p.NewScale) || float.IsNaN(p.FlashAlpha)) finite = false;
        }
        Check(finite, "★ B9 0～0.90 s 全程**无 NaN**（逐 0.01 s 采样）");
    }

    // ==================================================================
    //  C · 不冻结输入（配一条对照，防「这个函数恒 false」）
    // ==================================================================
    static void GroupC_NoInputFreeze()
    {
        Debug.Log(P + "--- C · 本档不挡玩家输入（配 Attack 的对照）---");
        var ev = new BattleEvent { Kind = EvtKind.WarlordTransform, Player = 0, Slot = BoardSpec.WarlordSlot, CardId = "X", Effect = "Y" };
        Check(!BattleDriver.BlocksPlayerActions(ev),
              "★ C1 `WarlordTransform` **不挡玩家动作**（`BlocksPlayerActions` = false ⇒ 演出期间照常可操作）");
        var atk = new BattleEvent { Kind = EvtKind.Attack, Player = 0, Slot = 1, TargetPlayer = 1, TargetSlot = 5 };
        Check(BattleDriver.BlocksPlayerActions(atk),
              "★ C1b **灭自证/对照**：同一次调用里 `Attack` 必须 = true —— 把 `BlocksPlayerActions` 写成恒 false ⇒ 本行红");
        Check(TransformPresentation.MinUnskippable >= TransformPresentation.DissolveEnd
              && TransformPresentation.MinUnskippable <= TransformPresentation.Total,
              "★ C2 最小时长落在 [溶解完, 整条] 之间（0.45 s：前两拍不可跳、第三拍可与操作并行）");
    }

    // ==================================================================
    //  D · 登记表 + 终态 + 接线（源码闸）
    // ==================================================================
    static void GroupD_RegistryAndWiring()
    {
        Debug.Log(P + "--- D · `EventTiming` 登记 / 终态 / 发出点与消费端接线 ---");
        var k = EvtKind.WarlordTransform;
        Check(Enum.IsDefined(typeof(EvtKind), "WarlordTransform"), "★ D1 枚举里真有 `EvtKind.WarlordTransform`");
        Check(EventTiming.IsRegisteredEventKind(k), "★ D2 已登记（`ZeroDurationKinds`）｜🧨 漏登记 ⇒ `BattleScene` A2·J2③ 与 `unsourced` 那两条红");
        Check(EventTiming.IsZeroDurationKind(k) && !EventTiming.IsTimedKind(k),
              "★ D2b 只在**一张**登记表里（零时长），⛔ 不是「有时间」（那会把下一条事件凭空推后 0.9 s）");
        Near(EventTiming.DurationOf(k), 0f, 1e-6f, "★ D2c `DurationOf` = 0（不占时间线）");
        Check(EventTiming.IsSourced(k), "★ D2d `IsSourced` = true（零时长档没有「我们挑的数值」）");
        string src = EventTiming.SourceOf(k);
        Check(!string.IsNullOrEmpty(src) && src.IndexOf("自己设计", StringComparison.Ordinal) >= 0,
              "★ D2e `SourceOf` 有话说，且写明「自己设计」（零时长档也允许解释串 —— 同 `AmbushExit` 的口径）");
        int before = EventTiming.UnregisteredKindHits;
        EventTiming.DurationOf(k); EventTiming.SourceOf(k); string why; EventTiming.IsSourced(k, out why);
        Eq(EventTiming.UnregisteredKindHits, before,
           "★ D2f 三处都不走 `default:` 的未登记分支（计数不涨）｜🧨 把登记表那一格删掉 ⇒ 本行与 D2 同时红");
        Check(string.IsNullOrEmpty(why), "★ D2g `IsSourced(…, out why)` 的 `why` 是空（有出处的一档不写原因）");

        var done = TransformPresentation.DonePose;
        Check(done.NewAlpha >= 1f - 1e-6f && Mathf.Abs(done.NewScale - 1f) <= 1e-6f
              && done.OldAlpha <= 1e-6f && done.FlashAlpha <= 1e-6f,
              "★ D3 终态：新督军 **alpha = 1 · 缩放 = 1**，旧督军与白光都归零（收尾不留下半透明的卡）");

        string root = Application.dataPath;
        string[] lines;
        if (!LoadLines(root + "/RuleEngine/Core/RuleCorePartial_B96CB.cs", out lines)) return;
        bool emit = false;
        foreach (var raw in lines)
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("ctx.Emit(EvtKind.WarlordTransform", StringComparison.Ordinal) >= 0) emit = true;
        }
        Check(emit, "★ D4 发出点在 `RuleCorePartial_B96CB.cs` 的 `SwapWarlordCard` 里（`ctx.Emit(EvtKind.WarlordTransform, …)`）");

        if (!LoadLines(root + "/CardPresentation/Battle/BattleDriver.cs", out lines)) return;
        bool routed = false, plays = false, mate = false;
        foreach (var raw in lines)
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("e.Kind == EvtKind.WarlordTransform", StringComparison.Ordinal) >= 0
                && t.IndexOf("PlayWarlordTransformFeel", StringComparison.Ordinal) >= 0) routed = true;
            if (t.IndexOf("TransformPresentation.Play(", StringComparison.Ordinal) >= 0) plays = true;
            if (t.IndexOf("TransformPresentation.MaterializeFx", StringComparison.Ordinal) >= 0) mate = true;
        }
        Check(routed, "★ D5 `PlaySignal` **真的路由**到 `PlayWarlordTransformFeel`（⛔ 不是只加了一个没人调的方法）");
        Check(plays, "★ D5b 消费端**真的**调 `TransformPresentation.Play(`");
        Check(mate, "★ D5c 第三拍（0.45 s）那件 materialize 粒子**接在 `onMaterialize` 上**（`TransformPresentation.MaterializeFx`）");

        if (!LoadLines(root + "/CardPresentation/Battle/TransformPresentation.cs", out lines)) return;
        bool setAlpha = false, setExtra = false, restoreOnKill = false;
        foreach (var raw in lines)
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("NewCard.SetAlpha(p.NewAlpha)", StringComparison.Ordinal) >= 0) setAlpha = true;
            if (t.IndexOf("NewCard.SetExtraScale(Vector3.one * p.NewScale)", StringComparison.Ordinal) >= 0) setExtra = true;
            if (t.IndexOf("NewCard.SetAlpha(1f)", StringComparison.Ordinal) >= 0) restoreOnKill = true;
        }
        Check(setAlpha, "★ D6 新督军的 alpha 由**真视图**的 `SetAlpha` 收尾（终态 1）");
        Check(setExtra,
              "★ D6b 缩放走 `SetExtraScale`（乘在 `_baseScale × _hlMul` 之上）—— 直接写 `transform.localScale` 会让后续"
            + "`SetPose` / `SetHighlightScale` 把卡**弹回旧尺寸**（`CardView.BaseScale` 的 doc 记着这个坑）");
        Check(restoreOnKill,
              "★ D6c **提前 `Kill` 的收场也写真卡终态**（alpha 1 / `SetExtraScale(one)`）—— 否则切局那一拍"
            + "新督军会停在 `alpha 0` 上**看不见**（静默，最糟的一种收场）");
    }

    // ==================================================================
    //  E · `🔶 自己设计` 与「查不到」的记录（铁律 18）
    // ==================================================================
    static void GroupE_SelfDesign()
    {
        Debug.Log(P + "--- E · `🔶 自己设计（非原版）` 与「查不到」都必须写在文件里 ---");
        string root = Application.dataPath;
        string[] lines;
        if (!LoadLines(root + "/CardPresentation/Battle/TransformPresentation.cs", out lines)) return;
        bool mark = false, notFound = false, pr = false, cue = false;
        foreach (var raw in lines)
        {
            if (raw.IndexOf("🔶 自己设计", StringComparison.Ordinal) >= 0) mark = true;
            if (raw.IndexOf("查不到", StringComparison.Ordinal) >= 0) notFound = true;
            if (raw.IndexOf("用户 2026-11-03 裁定", StringComparison.Ordinal) >= 0) pr = true;
            if (raw.IndexOf("invokeCUE", StringComparison.Ordinal) >= 0) cue = true;
        }
        Check(mark, "★ E1 `TransformPresentation.cs` 里有 `🔶 自己设计（非原版）` 标记（铁律 18：可 grep）");
        Check(pr, "★ E1b 标记里写明**谁定的 + 日期**（`用户 2026-11-03 裁定`）");
        Check(notFound, "★ E2 文件里写明哪些资源**查不到**（粒子 prefab / cue 名）—— ⛔ 不冒充原版");
        Check(cue, "★ E2b 用了库内**登场** cue（`invokeCUE`）并在注释里交代「原版没有 transform cue」");

        if (!LoadLines(root + "/RuleEngine/Core/BattleEvent.cs", out lines)) return;
        bool enumMark = false, enumWhy = false;
        foreach (var raw in lines)
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("WarlordTransform", StringComparison.Ordinal) < 0) continue;
            if (raw.IndexOf("自己设计", StringComparison.Ordinal) >= 0) enumMark = true;
            if (raw.IndexOf("原版", StringComparison.Ordinal) >= 0) enumWhy = true;
        }
        Check(enumMark && enumWhy,
              "★ E3 枚举成员那一行就带着 `自己设计` + `原版` 两个词（读枚举的人不必翻别处就知道这一段不是原版）");
    }

    // ==================================================================
    //  F · `PlanOf` 判别式 + `Play` 冒烟（null 视图，不需要 HUD）
    // ==================================================================
    static void GroupF_PlanAndPlay()
    {
        Debug.Log(P + "--- F · `PlanOf` 判别式 + `Play` 冒烟 ---");
        var e = new BattleEvent { Kind = EvtKind.WarlordTransform, Player = 1, Slot = BoardSpec.WarlordSlot,
                                  CardId = "NewHero", Effect = "OldHero", InstanceId = 77 };
        var plan = TransformPresentation.PlanOf(e, 0);
        Check(plan.Valid && plan.Seat == 1 && plan.NewCardId == "NewHero" && plan.OldCardId == "OldHero",
              "★ F1 合法事件 ⇒ `Valid`，新旧卡名各就各位");
        Near(plan.Seconds, TransformPresentation.Total, 1e-6f, "★ F1b 计划时长 = 整条 0.90 s");
        Check(!TransformPresentation.PlanOf(null, 0).Valid, "★ F2 **反证**：`null` ⇒ 不归本演出");
        Check(!TransformPresentation.PlanOf(new BattleEvent { Kind = EvtKind.Attack, Player = 0 }, 0).Valid,
              "★ F2b **反证**：别的 `EvtKind` ⇒ 不归本演出");
        Check(!TransformPresentation.PlanOf(new BattleEvent { Kind = EvtKind.WarlordTransform, Player = 7 }, 0).Valid,
              "★ F2c **反证**：座位越界 ⇒ 不归本演出");

        TransformPresentation.Playback pb = null;
        try
        {
            pb = TransformPresentation.Play(plan, null, null, null, Vector3.zero);
        }
        catch (Exception ex)
        {
            Check(false, "★ F3 `Play` 冒烟**不许抛**（null 视图 / 无 HUD 的场合）：" + ex.Message);
            return;
        }
        Check(pb != null, "★ F3 `Play` 生成了一条播放句柄");
        if (pb == null) return;
        pb.ApplyForTest(0f);
        Near(pb.Current.NewAlpha, 0f, 1e-6f, "★ F3b `ApplyForTest(0)` ⇒ 新督军仍不可见");
        pb.ApplyForTest(TransformPresentation.Total);
        Check(pb.CurrentPhase == TransformPresentation.Phase.Done && Mathf.Abs(pb.Current.NewScale - 1f) <= 1e-6f
              && Mathf.Abs(pb.Current.NewAlpha - 1f) <= 1e-6f,
              "★ F3c `ApplyForTest(0.90)` ⇒ 终态（alpha 1 · 缩放 1）");
        Check(pb.FlashQuad == null && pb.OldView == null,
              "★ F3d 没有 HUD / 没给旧卡 ⇒ 不建白光件、不建替身（⛔ 不静默造「猜的」对象）");
        pb.Kill();
        Check(pb.FlashQuad == null, "★ F3e `Kill()` 幂等收场（白光件已摘）");
    }

    // ==================================================================
    //  工具 / 夹具
    // ==================================================================
    static List<BattleEvent> Take(BattleContext ctx)
    {
        var buf = new List<BattleEvent>();
        ctx.DrainSignals(buf);
        return buf;
    }

    static List<BattleEvent> Only(List<BattleEvent> buf)
    {
        var re = new List<BattleEvent>();
        for (int i = 0; i < buf.Count; i++)
            if (buf[i] != null && buf[i].Kind == EvtKind.WarlordTransform) re.Add(buf[i]);
        return re;
    }

    static int CountTransform(List<BattleEvent> buf) { return Only(buf).Count; }

    static bool LoadLines(string path, out string[] lines)
    {
        lines = null;
        if (!System.IO.File.Exists(path))
        {
            Check(false, "[前提] 读得到 `" + path + "` —— 读不到时本组等于没验 ⇒ 当场红");
            return false;
        }
        lines = System.IO.File.ReadAllLines(path);
        return true;
    }

    /// <summary>1 张督军 + 20 张垫牌（`shuffle:false` ⇒ 抽牌 = 从列表末尾取；本件不抽牌，只要督军在位）。</summary>
    static List<CardDef> FixtureDeck()
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        return d;
    }

    static CardDef Hero(string name, int atk, int hp)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, atk, hp, 0, null);
    }

    static CardDef Unit(string name, int cost, int atk, int hp)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, null);
    }
}
