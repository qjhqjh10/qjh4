// B107_VfxFallback_Assert.cs — 执行代理 `B107_W11`（第一百零七会话 · 2026-10-31）
//
// ============================ 它盯的是什么 ============================
//  `VfxMap.Events`（`Core/VfxMap.cs:3845`，现读 **19** 个）**全枚举**的兜底三档必须一一对应
//  （施工单 `判据/B107_特效线施工单_v2.md:91` 的 **VFX-1**）：
//    ① 原版无档（`IsOriginalNoGenericEvent`，`VfxMap.cs:308-326`，现读 **8** 个）⇒ `Resolve` 回 **null** 且 **不出声**；
//    ② 已知缺口（`IsKnownGapEvent`，`VfxMap.cs:344-347`，现读 **1** 个）⇒ null 且 **恰好出声一次**（每个 (事件, 阵营) 一次）；
//    ③ 有档（`ByEvent` / `ByFaction` 里配了，现读 **9** 个）⇒ **非 null**。
//  外加 **④ 抽卡 `draw`** —— **施工单 VFX-1 的「三档」没列它**，而它今天 **null 且出声**
//  （`ByFaction` 里 `draw` 那两行已按 `W64` B5 删除，`VfxMap.cs:567-578` ⇒ 走 `ResolveFallback` 的 A1731 留白出口）。
//  本件**单列**这一档，⛔ 不把它混进第 3 类（混进去会让「有档 ⇒ 非 null」直接红，而且看不出是谁的错）。
//
//  为什么要有这条常驻锁：`VfxMap.Events` 自己的注释写着「加了新事件、那份列表没跟上」这个 bug
//  **同一形状已经犯过五次**（`CollectWaystone` / `Draw`+`AmbushExit` / `UnitAttack`+`UnitDamaged` / `QuestSpent`，
//  见 `VfxMap.cs:3844-3881`）。今天 `Editor/BattleScene.cs:1083-1134` 只覆盖「`Unmapped()` 的**构成**」，
//  **没有一条盯出声计数**（`NoFactionVariantWarns`）—— 而「原版无档」与「已知缺口」**唯一的可观测差异**
//  就是「响不响」。本件把那个差异钉死，并且**独立于那个宿主上下文**可单独跑。
//
// ============================ 判据（⛔ 期望不从被测实现反算）============================
//  · 三份名单**逐名**写在下面（`NoGeneric` / `KnownGap` / `HasMapping`）。来源 = `VfxMap` 两张判据名单的注释
//    与它们引的原版审计：`资料/普查产出_1022/W64_兜底审计_特效.md` §一 B1–B8 ·
//    `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B · `VfxMap.cs:320-325` 的 `gain_spirit`
//   （13 场 × 2 份 `SpiritStoneHolder` 共 26 份 `ManaTypeHolder`，`useVfxOnValueChange` 全 0、两个 vfx 槽全空串）。
//  · 事件常量本身是**判据的键**（`VfxMap.PlayCard` = `"play"` …）⇒ 期望值取自名单，**不取自** `Resolve` 的返回值。
//  · 「出声」的唯一可观测口 = `VfxMap.NoFactionVariantWarns`（`VfxMap.cs:692`：只增不减、按 (东西, 阵营)
//    去重、上限 `NoFactionWarnCap = 32`）。⇒ 本件用 `VfxMap.ResetNoFactionWarnsForTest()`（`B107_W7` 本轮新加，
//    类尾 `VfxMap.cs:4286`）把计数清零再看增量 —— **必须清零**：不清零时只要集合已有 ≥32 条，
//    「该响」的全被上限挡掉，断言会**假绿**。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加一行 `B107W11_Assert.Run();` 再并进合计：
//      `B107W11_Assert.Run(); pass += B107W11_Assert.Pass; fail += B107W11_Assert.Fail;`
//  —— 建议 **`Editor/BattleScene.cs`**，插在那段 `Unmapped()` 事件表断言（现读 `:1083` 起）**之前**
//  （它那条路只读 `Unmapped()`，不读计数，先后都不受影响；放前面可读性更好：先断三档，再看构成）。
//  ⚠️ 本件会**清零** `NoFactionVariantWarns`（收尾也清回 0）。今天另有两处消费者，都写成
//  **「取基线 → 比增量」**（`BattleScene.cs:2762` / `BattleVfxAssertE3.cs:311`）⇒ 只要不插在它们
//  「取基线 … 比增量」那个窗口**中间**，就安全。
//  本件**不建任何 GameObject / 资产、不载卡表**（`cardId` 传 null ⇒ `TryResolvePerCardPicks` 立刻返回 false，
//  连 `EnsureLoaded()` 都不触发）⇒ 无副作用、无收尾销毁（⛔ 没有 `DestroyImmediate` 可调，因为没有建东西）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · **阳性对照**（§5-①）：先解一个**不存在**的探针事件 ⇒ 它必须走同一个兜底出口**响一声**。
//    这一条为 0 ⇒ 计数链此刻是坏的 / 根本没跑 ⇒ 上面所有「不出声」的结论**全部是没验到**，不是通过。
//  · **可复现**（§5-②）：清空出声集合 → 整轮 sweep 一遍记读数；再清空 → 再 sweep → 两次必须相等且都 = **3**。
//    「从来不响」会得到 0/0、「每次都多响」会得到 3/6 —— 两种都红。
//  · **分区不漏**（§0）：三份名单 + `draw` 的并集必须**恰好等于** `Events`（无重、无漏）。
//    谁把某个事件从名单里挪走又不给它配档 ⇒ 它掉进「未分类」⇒ §0 红。**这正是 `ambush_exit` 从
//    「已知缺口」被挪走时会现的形状**（它会变成「null 但只响不判」，在 §0 就红，而不是悄悄变成「有档」）。
//  · **反向**：把任何一件替代品挂回 `ByEvent[play/ability/return/attack_melee/unit_*]` ⇒ §1「必须 null」红；
//    把 `ambush_exit` 从 `IsKnownGapEvent` 摘掉 ⇒ §0 的**逐名**断言红；
//    把 `draw` 收编进第①档（很可能该收 —— 见报告 §"没查清"）⇒ §4 红，提醒同步本文件的四份名单。
//
// ============================ 判据 / 报告 ============================
//  报告：`资料/完全复刻/报告/B107_W11.md`（⛔ 别在这里抄第二份）
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B107_W11`：`VfxMap.Events` 全枚举的兜底三档（null / 出声 的对应关系）+ 抽卡 `draw` 那一档。
/// <para>⛔ 期望值来自 `VfxMap` 的两张**判据名单**与它们引的原版审计，**不从 `Resolve` 的返回值反算**。</para></summary>
public static class B107W11_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107W11] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ============================================================ 判据名单（逐名 · ⛔ 别只靠谓词，谓词也要逐名钉）
    /// <summary>① 原版**无这一档**（判据见 `VfxMap.cs:303-326` 的注释与其引的三份审计）。现读 **8** 个。</summary>
    static readonly string[] NoGeneric =
    {
        VfxMap.PlayCard,       // B1：`CardScript` 没有 play 特效槽 + `_Play*Card` 0 处 Instantiate
        VfxMap.Ability,        // B2：技能动画一律来自逐卡 `abilityLogic[].cardAnimations[]`
        VfxMap.Death,          // B3：真件 = `Card 3D Death Explosion`，播放口在 `CardFeel`（不进 ByEvent）
        VfxMap.Return,         // B4：`_ResolveRecallToHand` 0 处 Instantiate（回手是 Animation）
        VfxMap.AttackMelee,    // A2：1,195 张卡只有 2 张有 attackAnim、且都是空引用
        VfxMap.UnitAttack,     // W63：`UnitAttack(50)` 只有逐卡
        VfxMap.UnitDamaged,    // W63：`UnitDamaged(60)` 只有逐卡
        VfxMap.GainSpirit,     // 2026-10-31：13 场 26 份 `ManaTypeHolder` 的 `useVfxOnValueChange` 全 0
    };

    /// <summary>② 有真件、本机接不上（回 null **且出声**）。现读 **1** 个。</summary>
    static readonly string[] KnownGap =
    {
        VfxMap.AmbushExit,     // B7：`Ambush` 只有 End(1) → `AmbushEffect`，而这个 build 里 End(1) 无消费口
    };

    /// <summary>③ 有档（`ByEvent` 里现读 9 行；`AttackRanged` 另有 4 个阵营的 `ByFaction` 变体）。⇒ `Resolve` 必须非 null。</summary>
    static readonly string[] HasMapping =
    {
        VfxMap.Deploy, VfxMap.AttackRanged, VfxMap.Hit, VfxMap.Heal, VfxMap.Trigger,
        VfxMap.GainFaith, VfxMap.GainQuest, VfxMap.QuestSpent, VfxMap.CollectWaystone,
    };

    /// <summary>④ 施工单 VFX-1 没列的一种「null」：抽卡（`ByFaction` 两行已按 `W64` B5 删除 ⇒ 走留白出声出口）。</summary>
    const string Draw = VfxMap.Draw;

    /// <summary>哨兵阵营名 —— 生产永远不会传这两个名字 ⇒ 本件的出声去重键与真实播放**互不干扰**。</summary>
    const string F = "B107W11_SELFTEST_A";
    const string F2 = "B107W11_SELFTEST_B";

    /// <summary>不存在的探针事件（§5 的阳性对照：证明「出声」这条链此刻是活的）。</summary>
    const string ProbeEvent = "__b107w11_probe_event__";

    static int W { get { return VfxMap.NoFactionVariantWarns; } }

    // ==================================================================
    public static void Run()
    {
        Coverage();                 // §0 —— 前提：全枚举 + 三份名单逐名 + 分区不重不漏 + `Unmapped()` 构成
        NoGenericSilent();          // §1 —— 原版无档 8 个 ⇒ null 且 0 声
        KnownGapSpeaks();           // §2 —— 已知缺口 1 个 ⇒ null 且恰好 1 声（去重，粒度 = (事件, 阵营)）
        HasMappingResolves();       // §3 —— 有档 9 个 ⇒ 非 null 且 0 声
        DrawIsNullButSpeaks();      // §4 —— 抽卡 ⇒ null 且出声（施工单漏列的第四档）
        AntiSelfProof();            // §5 —— 阳性对照 + 可复现
        Cleanup();                  // §6 —— 计数清回 0
    }

    // ------------------------------------------------------------------
    //  §0 前提与覆盖
    // ------------------------------------------------------------------
    static void Coverage()
    {
        S("🆕 B107_W11 · §0 前提：`VfxMap.Events` 全枚举 19 个 + 三份判据名单逐名（分区不重不漏）");

        C(VfxMap.Events.Length, 19,
          "（前提）`VfxMap.Events` 现读 19 个 —— 加/删事件必须**同时**决定它属于哪一档；"
        + "这条红 ⇒ 回源核判据后**同步更新本文件的四份名单**（⛔ 不是改这个数字了事）");

        // ---- ① 原版无档：逐名 + 互斥 ----
        int noGen = 0;
        for (int i = 0; i < NoGeneric.Length; i++)
        {
            T(VfxMap.IsOriginalNoGenericEvent(NoGeneric[i]),
              "（前提）`" + NoGeneric[i] + "` 在 `IsOriginalNoGenericEvent` 里（判据 = `VfxMap.cs:303-326` 那段注释引的原版审计）");
            T(!VfxMap.IsKnownGapEvent(NoGeneric[i]),
              "（前提）`" + NoGeneric[i] + "` **不**在 `IsKnownGapEvent` 里（两张名单必须互斥，否则「响不响」没有唯一答案）");
            if (VfxMap.IsOriginalNoGenericEvent(NoGeneric[i])) noGen++;
        }
        C(noGen, 8, "（前提）原版无档 = **8** 个：play / ability / death / return / attack_melee / unit_attack_anim / unit_damaged_anim / gain_spirit");

        // ---- ② 已知缺口：逐名 + 互斥 ----
        int gap = 0;
        for (int i = 0; i < KnownGap.Length; i++)
        {
            T(VfxMap.IsKnownGapEvent(KnownGap[i]),
              "（前提）`" + KnownGap[i] + "` 在 `IsKnownGapEvent` 里（判据 = `VfxMap.cs:328-347`：`Ambush` 只有 End(1)，这个 build 无消费口）");
            T(!VfxMap.IsOriginalNoGenericEvent(KnownGap[i]),
              "（前提）`" + KnownGap[i] + "` **不**在「原版无档」名单里（它是有真件、本机接不上 —— 两种「空」别混）");
            if (VfxMap.IsKnownGapEvent(KnownGap[i])) gap++;
        }
        C(gap, 1, "（前提）已知缺口 = **1** 个：ambush_exit（`collect_waystone` 已于 2026-10-22 P6 接上真件、不在此列）");

        // ---- ③ 有档：两张名单之外（否则 §3 与 §1/§2 会互相抵消）----
        for (int i = 0; i < HasMapping.Length; i++)
        {
            T(!VfxMap.IsOriginalNoGenericEvent(HasMapping[i]),
              "（前提）有档事件 `" + HasMapping[i] + "` **不**在「原版无档」名单里");
            T(!VfxMap.IsKnownGapEvent(HasMapping[i]),
              "（前提）有档事件 `" + HasMapping[i] + "` **不**在「已知缺口」名单里");
        }
        T(!VfxMap.IsOriginalNoGenericEvent(Draw),
          "（前提）`draw` **不**在「原版无档」名单里（它是 A1731 留白出声出口的见证，见 `VfxMap.cs:575-578`）");
        T(!VfxMap.IsKnownGapEvent(Draw),
          "（前提）`draw` **不**在「已知缺口」名单里（它不是「有真件接不上」，是「原版 0 粒子」）");

        // ---- 分区覆盖：并集 == Events（无重、无漏）----
        var seen = new HashSet<string>();
        int dup = 0;
        dup += Fill(seen, NoGeneric);
        dup += Fill(seen, KnownGap);
        dup += Fill(seen, HasMapping);
        if (!seen.Add(Draw)) dup++;
        C(seen.Count, VfxMap.Events.Length,
          "（前提）三份名单 + `draw` 的并集 == `Events` 全集（" + seen.Count + " / " + VfxMap.Events.Length + "）"
        + " —— 少一个 = 有事件**没有任何一档的期望**（「加了新事件、列表没跟上」正是同一形状的第五次）");
        C(dup, 0, "（前提）四份名单**两两不重**（重了 = 一个事件有两种期望，两条断言会互相抵消、都得绿）");

        var stray = new List<string>();
        for (int i = 0; i < VfxMap.Events.Length; i++)
            if (!seen.Contains(VfxMap.Events[i])) stray.Add(VfxMap.Events[i]);
        C(stray.Count, 0, "（前提）`Events` 里**没有**任何一个落在四份名单之外"
          + (stray.Count > 0 ? "（漏：`" + string.Join("`、`", stray.ToArray()) + "`）" : ""));

        // ---- `Unmapped()` = 探测口径（不带阵营）⇒ 构成 10、且**一声都不许出** ----
        VfxMap.ResetNoFactionWarnsForTest();
        T(W == 0, "（前提）`VfxMap.ResetNoFactionWarnsForTest()` 之后出声计数 = 0（现读 " + W + "）"
                + " —— 它为 0 说明清零口是活的，下面每条的「+1 / 不涨」才有意义");
        var unmapped = new List<string>();
        foreach (var ev in VfxMap.Unmapped()) unmapped.Add(ev);
        C(unmapped.Count, 10,
          "`Unmapped()` 构成 = 8（原版无档）+ 1（已知缺口）+ 1（`draw`）= **10**（实得 " + unmapped.Count + "）"
        + " —— 这条与 `BattleScene.cs:1083-1134` 那条「unmapped == 0」是同一口径，本件独立复算一遍");
        C(W, 0, "★★ `Unmapped()` 是**枚举探针**（`faction` 为空）⇒ **一声都不许出**（现读 " + W + "）"
              + " —— 否则自检日志里会混进一串「某阵营没配」的假缺口；`ResolveFallback` 的出声口明确挡这一支");

        var unexpected = new List<string>();
        for (int i = 0; i < unmapped.Count; i++)
            if (!VfxMap.IsOriginalNoGenericEvent(unmapped[i]) && !VfxMap.IsKnownGapEvent(unmapped[i])
                && unmapped[i] != Draw)
                unexpected.Add(unmapped[i]);
        C(unexpected.Count, 0, "`Unmapped()` 里只允许出现那三族"
          + (unexpected.Count > 0 ? "（意外项：`" + string.Join("`、`", unexpected.ToArray()) + "`）" : ""));
    }

    // ------------------------------------------------------------------
    //  §1 原版无档 ⇒ null 且不出声
    // ------------------------------------------------------------------
    static void NoGenericSilent()
    {
        S("🆕 B107_W11 · §1 原版无档 8 个 ⇒ `Resolve` 回 **null** 且**一声都不出**");

        int w0 = ResetAndBaseline();
        for (int i = 0; i < NoGeneric.Length; i++)
        {
            string got = VfxMap.Resolve(NoGeneric[i], F);
            C(got, null, "★ `" + NoGeneric[i] + "`（原版无档）⇒ `Resolve` 回 **null**（实得 `" + (got ?? "null") + "`）"
                       + " —— 这是原版的形状，⛔ 不许硬凑一件替代品");
        }
        C(W, w0, "★★ 这 8 个**一声音都没出**（" + w0 + " → " + W + "）"
               + " —— 它们是原版没有这一档，喊成「漏配」就是假信号（`ResolveFallback` 的 ① 支明确挡掉）");
    }

    // ------------------------------------------------------------------
    //  §2 已知缺口 ⇒ null 且恰好出声一次
    // ------------------------------------------------------------------
    static void KnownGapSpeaks()
    {
        S("🆕 B107_W11 · §2 已知缺口 `ambush_exit` ⇒ null 且**恰好出声一次**（去重粒度 = (事件, 阵营)）");

        int w0 = ResetAndBaseline();
        C(VfxMap.Resolve(VfxMap.AmbushExit, F), null,
          "★ `ambush_exit` ⇒ **null**（有真件、本机接不上；⛔ 不静默留白、也不拿 `AmbushEffect` 顶）");
        C(W, w0 + 1, "★★ …而且**响了 1 声**（" + w0 + " → " + W + "）"
                   + " —— 这正是「已知缺口」与「原版无档」**唯一的可观测差异**");

        C(VfxMap.Resolve(VfxMap.AmbushExit, F), null, "★ 同一阵营再解一次仍 null");
        C(W, w0 + 1, "★★ …第二次**不再出声**（去重键 = (事件, 阵营)，不是每次调用都喊）");

        C(VfxMap.Resolve(VfxMap.AmbushExit, F2), null, "★ 换一个阵营再解");
        C(W, w0 + 2, "★★ …换阵营 ⇒ **再响 1 声**（粒度是 (事件, 阵营)；按 (事件) 只响一次、或每次调用都响，都不对）");
    }

    // ------------------------------------------------------------------
    //  §3 有档 ⇒ 非 null
    // ------------------------------------------------------------------
    static void HasMappingResolves()
    {
        S("🆕 B107_W11 · §3 有档 9 个 ⇒ `Resolve` **非 null**，且不出声");

        int w0 = ResetAndBaseline();
        for (int i = 0; i < HasMapping.Length; i++)
        {
            string got = VfxMap.Resolve(HasMapping[i], F);
            T(!string.IsNullOrEmpty(got), "★ `" + HasMapping[i] + "`（有档）⇒ 非 null（实得 `" + (got ?? "null") + "`）");
            T(VfxMap.Resolve(HasMapping[i]) != null,
              "★ …**不带阵营**（探测口径）也解析得出 —— 这才是 `Unmapped()` 里看不到它的原因"
            + "（`Trigger` 那条是**死条目**（无生产消费者）但解析仍非 null，⛔ 别当缺陷删）");
        }
        C(W, w0, "★ 有档事件**不出声**（" + w0 + " → " + W + "）—— 解析出来了还喊留白，同样是假缺口");
    }

    // ------------------------------------------------------------------
    //  §4 抽卡：施工单没列的「第四档」
    // ------------------------------------------------------------------
    static void DrawIsNullButSpeaks()
    {
        S("🆕 B107_W11 · §4 抽卡 `draw` ⇒ null 且**出声**（施工单 VFX-1 的「三档」没列它）");

        int w0 = ResetAndBaseline();
        C(VfxMap.Resolve(Draw, F), null,
          "★ `draw` ⇒ **null**（原版抽卡链 **0 粒子**，`W64` B5；⛔ 别拿 `UM_CardDraw` / `SAU_CardDraw` 顶上）");
        C(W, w0 + 1, "★★ …且**出声 1 次**（A1731 的留白见证：`ByEvent` / `ByFaction` 两边都没配 ⇒ 走兜底出声出口）"
                   + " —— 它**不在**两张名单里，本件单列；若谁把 `draw` 收编进第①档，本条会红（提醒同步本文件的四份名单）");
    }

    // ------------------------------------------------------------------
    //  §5 灭自证
    // ------------------------------------------------------------------
    static void AntiSelfProof()
    {
        S("🆕 B107_W11 · §5 灭自证：阳性对照 + 计数可复现");

        // ---- ① 阳性对照：同一个兜底出口的「必响」样本 ----
        ResetAndBaseline();
        C(VfxMap.Resolve(ProbeEvent, F), null, "（阳性对照）不存在的探针事件 `" + ProbeEvent + "` ⇒ null");
        C(W, 1, "★★ 阳性对照 ⇒ 计数 **+1**（现读 " + W + "）—— 证明「出声」这条链**此刻是活的**；"
              + "它为 0 ⇒ 上面所有「不出声」的结论**全部是没验到**，不是通过");

        // ---- ② 可复现：清空 → 整轮 sweep → 读数；再来一遍 ----
        VfxMap.ResetNoFactionWarnsForTest();
        int a = SweepWarns();
        VfxMap.ResetNoFactionWarnsForTest();
        int b = SweepWarns();
        C(a, 3, "★★ 整轮 sweep 的累计出声 = **3**（ambush_exit × 2 个阵营 + draw），实得 " + a);
        C(b, a, "★★ 清空出声集合再跑一遍，读数**可复现**（第一次 " + a + " / 第二次 " + b + "）"
              + " —— 「从来不响」会是 0/0、「每次都多响」会是 3/6，两种都红");

        VfxMap.ResetNoFactionWarnsForTest();
    }

    /// <summary>整轮 sweep（与 §1–§4 同一批调用）⇒ 返回累计出声数。⛔ 别把「期望的 3」从 `Resolve` 里反算。</summary>
    static int SweepWarns()
    {
        for (int i = 0; i < NoGeneric.Length; i++) VfxMap.Resolve(NoGeneric[i], F);
        VfxMap.Resolve(VfxMap.AmbushExit, F);       // 1 声
        VfxMap.Resolve(VfxMap.AmbushExit, F);       // 去重 ⇒ 0
        VfxMap.Resolve(VfxMap.AmbushExit, F2);      // 换阵营 ⇒ 1 声
        VfxMap.Resolve(Draw, F);                    // 1 声
        for (int i = 0; i < HasMapping.Length; i++) VfxMap.Resolve(HasMapping[i], F);
        return W;
    }

    // ------------------------------------------------------------------
    //  §6 收尾
    // ------------------------------------------------------------------
    static void Cleanup()
    {
        S("🆕 B107_W11 · §6 收尾：出声计数清回 0（本件不留「假缺口」痕迹）");
        VfxMap.ResetNoFactionWarnsForTest();
        C(W, 0, "（收尾）`NoFactionVariantWarns` = 0 —— 宿主后面若取 delta 基线，起点是干净的 0");
    }

    // ------------------------------------------------------------------
    static int ResetAndBaseline()
    {
        VfxMap.ResetNoFactionWarnsForTest();
        T(W == 0, "（前提）出声计数已清零（现读 " + W + "）—— 不清零时 ≥32 条就会撞 `NoFactionWarnCap`，"
                + "「该响」的全被上限挡掉 ⇒ 断言会假绿");
        return W;
    }

    static int Fill(HashSet<string> set, string[] arr)
    {
        int dup = 0;
        for (int i = 0; i < arr.Length; i++) if (!set.Add(arr[i])) dup++;
        return dup;
    }
}
