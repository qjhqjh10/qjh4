// B142_E36_Assert.cs — 批次 **B142** · 执行写手 `B142_E36`（2026-11-04 · 第一百二十九会话）
//
// ============================ 它钉的是什么 ============================
//   账 `B142-HAND-CTX-GATE`（判据 → `资料/完全复刻/判据/B142_R12_手牌监听覆盖复核.md` §一·闸 C / §四·④）：
//   **文本监听路原来缺 `abilityContext == Hand` 闸** —— 原版手牌里只有 `abilityContext == 10 (Hand)` 的
//   ability 能执行，而我们两条文本路（`BroadcastHandWhen` / `BroadcastHandTrapWhen`）只按「卡型 + 卡面文案形状」
//   放行 ⇒ 79 张 unit/hero 卡的 `When` 从句躺在手里也会**真的 `ResolveOps`**（例：`SOR38` 在手 + 获得信仰 ⇒
//   对敌督军造成 4 点，原版不会）。
//
//   本批在 `RuleEngine/Core/EffectResolver.cs` 的**两个逐张循环**里各补了一道
//   `!HandListenerContextAllows(c, kind)`；辅助方法在**文件末尾的新 partial 块**里（零行号漂移）。
//
//   🆕 2026-11-04（`B142_E47`）**D 组**：给同一个辅助方法补**第二刀** ——
//   `AbilityTriggerPresence(tn) == 0`（这张卡的 `abilityTriggers` 里**明确没有**该 trigger）⇒ 拒
//   （账 `B142-HAND-CTX-PRESENCE`；样本 `SOR10` / `UM81`）；并把 `BroadcastHandWhen` 的**早退预扫**
//   改成与逐张**同口径**（账 `B142-HAND-PRESCAN`；⛔ 只改预扫 = 真漏判）。
//
// ============================ 判据（全部现读，本轮核过）============================
//   · `CardAbility__IsRightContext.c:18-46` —— 六档：`0 Play` 要 `IsInPlay`（且无待死伤害、非残骸）；
//     `10 Hand` 要 `IsInHand`；`20` 牌库 · `30` 墓地 · `40`「在场或待死」· `50`「在场或墓地」；
//   · `CardScript__IsInPlay.c:5-11` vs `CardScript__IsInHand.c:5-8` —— 两个状态集合**不相交**
//     ⇒ 手牌区只对 `10 (Hand)` 成立；
//   · 链：`RawCardScript__OnTrigger.c:15` → `RawCardScript__TriggerAbilities.c:108/207`
//     → `CardAbility__CanTriggerAbility.c:87/115/126`；
//   · `cards_engine.json`（现读 v49 / 1142 张）：79 张 unit/hero 卡的 `When` 从句**没有一张**带 context 10；
//     `Jammed Communications` 的 `OtherCardPlayed(10)` 行 = context **10**（闸 B 的正例）。
//
// ============================ 🧨 灭自证（改哪一句会红）============================
//   · 删掉 `BroadcastHandWhen` 里那个 `|| !HandListenerContextAllows(c, kind)` ⇒ **C1 红 · B2 红**
//     （context=0 的卡又响了 —— 也就是这一笔要修的 over-fire 回来了）；
//   · 把 `BroadcastHandTrapWhen` 里那一条删掉 ⇒ **C2 红**；
//   · 把 `HandListenerContextAllows` 改成恒 `true` ⇒ **A1/A2/A3 分不开（A2/A3 红）**；
//   · 把 `HandListenerContextAllows` 改成恒 `false`（`-1` 也拒）⇒ **A3/B3 红**（旧表/未知被静默砍掉）；
//   · 把 A1/B1 的 context 行删掉 ⇒ A1/B1 红（「context=10 能执行」那一档塌掉）。
//
// ============================ 怎么接（宿主 · 调度台做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（必须落在该行第一个 `//` **之前**）：
//     `Guard("B142_E36_Assert", B142_E36_Assert.Run); _pass += B142_E36_Assert.Pass; _fail += B142_E36_Assert.Fail;`
//   （⚠️ 本件暂**未**接宿主；接之前一条都不会跑 —— 与 `B142_E1_Assert` 同一口径。）
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;

/// <summary>B142-E36 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B142_E36_Assert
{
    const string Tag = "B142E36";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Predicate();
        GroupB_EndToEnd();
        GroupC_Structure();
        GroupD_PresenceCut();
        if (_fail == 0) Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ 0");
        else Debug.LogError(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 判定函数三档（context=10 放 / context=0 拒 / -1 放）+ 池内数据事实
    // ==================================================================
    static void GroupA_Predicate()
    {
        Section("A · `HandListenerContextAllows`：手牌区只放 `abilityContext == 10 (Hand)`");

        // A1 正例：显式 context = 10 (Hand) ⇒ 放行
        var hand10 = Watcher("B142E36Hand10", new[]
        {
            new CardDef.AbilityContextSpec { Trigger = 665, Name = "OnCollectFaith", Context = CardDef.AbilityContextHand },
        }, versioned: true);
        CheckTrue(RuleCore.HandListenerContextAllowsForTest(hand10, WhenEventKind.GainFaith),
                  "A1 ★★★ **context=10 放行**（`OnCollectFaith(665)` 显式 `Hand`）"
                + "｜🧨 把判定改成恒 `false` ⇒ 红");

        // A2 反例：有这条 ability、但**没有 context 行** ⇒ v14 缺行 = Play(0) ⇒ 手牌里拒
        var play0 = Watcher("B142E36Play0", null, versioned: true);
        Check(RuleCore.AbilityContextOfAbilityV14(play0, "OnCollectFaith", null), CardDef.AbilityContextPlay,
              "A2-前提 （前提）缺行按 v14 口径读成 `Play(0)`（⛔ 不是「未知」）");
        CheckTrue(!RuleCore.HandListenerContextAllowsForTest(play0, WhenEventKind.GainFaith),
                  "A2 ★★★ **context=0 拒绝**（`Play` 要 `IsInPlay`，手牌区不成立）"
                + "｜🧨 删掉那道闸 ⇒ 红（这正是 `SOR38` 那类 over-fire）");

        // A3 `-1`（未知：旧卡表 / 这张卡没有该列）⇒ **不判、放行**（= 改前口径，⛔ 不静默砍监听）
        var unknown = Watcher("B142E36Unknown", null, versioned: false);
        Check(RuleCore.AbilityContextOfAbilityV14(unknown, "OnCollectFaith", null), CardDef.AbilityContextUnknown,
              "A3-前提 （前提）`abilityContextsVersioned = false` ⇒ v14 回 `-1`（未知）");
        CheckTrue(RuleCore.HandListenerContextAllowsForTest(unknown, WhenEventKind.GainFaith),
                  "A3 ★★★ **`-1` 放行**（未知不判 —— 与 `FireHandAbilityTrigger` 的 `-1` 契约分工相反：那边把命交给文本路，本方法就是文本路）"
                + "｜🧨 改成 `actx != Hand ⇒ 拒`（含 `-1`）⇒ 红");

        // A4 边界：卡为空 ⇒ 拒（调用点 `c == null` 已经先挡，这里是「不 NRE」的钉子）
        CheckTrue(!RuleCore.HandListenerContextAllowsForTest(null, WhenEventKind.GainFaith),
                  "A4 边界：`card == null` ⇒ `false`（⛔ 不 NRE）");

        // A5 池内数据事实（R12 §二·表 2a / 2d 的复算）
        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B142_E36] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "A5-前提 前提：卡池读得到且规模正常（现读 = " + (pool != null ? pool.Count : -1) + "）");
        if (pool == null || pool.Count <= 500) return;

        int pop = 0, withHandCtxRow = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || !c.CanListenForEvents || c.WhenTriggers.Count == 0) continue;   // = 闸 A 的放行面
            pop++;
            var ctxs = c.AbilityContexts;
            if (ctxs == null) continue;
            for (int k = 0; k < ctxs.Count; k++)
                if (ctxs[k] != null && ctxs[k].Context == CardDef.AbilityContextHand) { withHandCtxRow++; break; }
        }
        // ✅ 已复刻（2026-11-05 · B150 · #16）：按 R12 §二·表 2a 的**同一条定义**（`CanListenForEvents`∧`WhenTriggers.Count>0`）
        //   离线重数（`ruleprobe layers` 的 `ch.when`，现池 1142 张）：**83 张**（unit 78 + hero 5）。
        //   与 R12 当时那 79 张逐张对差：**只多 4 张、一张没少** ——
        //   `BL12 Aspiring Champion`（After receiving a Dark Pact, gain +1 [Ranged] and +1 Health）·
        //   `BL3 Chaos Legionary`（After receiving a Dark Pact, deal 2 damage to a random enemy）·
        //   `BL69 Meltagun Legionary`（Blast 2. After receiving a Dark Pact, …）·
        //   `TAU15 Sniper Drone`（Gains Long Range when an enemy gains Markerlight）
        //   ⇒ 四张都真的带「监听句」（分别是 `After …` 与句尾 `when …` 两种句式）⇒ 放行面口径没变，是卡池长了。
        //   ⛔ 别把这个数当成可以随手改的常数：它 = 该定义在现池上的**读数**，卡池再变要重新对差。
        Check(pop, 83, "A5 ★★ 闸 A 的放行面 = **83 张** unit/hero 卡（R12 表 2a 定义重数；R12 当时 79 = 现 83 − 新增 4）"
                     + "｜🧨 卡表/注册闸变了 ⇒ 这条会先红（那时先复核 R12 §二）");
        Check(withHandCtxRow, 0, "A5b ★★ 这 83 张里**有 `abilityContext == 10` 行的 = 0 张**"
                              + "（⇒ 补了闸之后，池内没有一张 unit/hero 的 `When` 能从手牌执行 —— 这正是原版的口径）");

        var sor38 = CardDatabase.Find(pool, "Paragon Warsuit");
        CheckTrue(sor38 != null, "A5c-前提 前提：真卡 `Paragon Warsuit`（SOR38）在池里");
        if (sor38 != null)
            CheckTrue(!RuleCore.HandListenerContextAllowsForTest(sor38, WhenEventKind.GainFaith),
                      "A5c ★★★ **真卡 `SOR38` 在手牌里被拒**（`abilityTriggers` 含 665、无 context 行 ⇒ `Play(0)`）"
                    + "｜🧨 这就是备份账里那条「躺手里多打 4 点」的根因");

        var jc = CardDatabase.Find(pool, "Jammed Communications");
        CheckTrue(jc != null, "A6-前提 前提：真卡 `Jammed Communications` 在池里");
        if (jc != null)
        {
            Check(jc.AbilityTriggerPresence("OtherCardPlayed"), 1,
                  "A6-前提2 （前提）它的 `abilityTriggers` 真有 `OtherCardPlayed(10)`");
            CheckTrue(RuleCore.HandListenerContextAllowsForTest(jc, WhenEventKind.Play),
                      "A6 ★★★ **闸 B 的正例不被一起关死**：`Jammed Communications` 的 `OtherCardPlayed(10)` = context **10** ⇒ 放行"
                    + "｜🧨 把闸 B 也写成「非 10 一律拒」而不管 `-1`/数据 ⇒ 这张卡的手牌陷阱会静默失效");
        }
    }

    // ==================================================================
    //  B · 端到端（`BroadcastWhen` → 手牌监听 → 敌督军掉血）
    // ==================================================================
    static void GroupB_EndToEnd()
    {
        Section("B · 端到端：同一句 `When you gain Faith, deal 4 damage to the enemy warlord` 三档");

        var hand10 = Watcher("B142E36E2EHand10", new[]
        {
            new CardDef.AbilityContextSpec { Trigger = 665, Name = "OnCollectFaith", Context = CardDef.AbilityContextHand },
        }, versioned: true);
        var play0 = Watcher("B142E36E2EPlay0", null, versioned: true);
        var unknown = Watcher("B142E36E2EUnknown", null, versioned: false);

        // 前提：三张卡都在手牌里**注册成了监听器**（否则「0 点伤害」是假绿）
        Check(hand10.WhenTriggers.Count, 1, "B0-前提 （前提）夹具卡的 `When …` 被登记成 1 条监听器");
        CheckTrue(hand10.CanListenForEvents, "B0-前提2 （前提）夹具卡是「会听」的类型（`IsUnit`）");

        // B1 context=10 ⇒ 真的执行（敌督军 −4）
        {
            var ctx = MakeHandBattle(hand10, out var foe);
            if (ctx == null || foe == null) { Report(false, "B1 前提不成立：对局没建起来"); }
            else
            {
                int hp0 = foe.Health;
                RuleCore.BroadcastWhen(ctx, WhenEventKind.GainFaith, 0, hand10, subject: null);
                Check(hp0 - foe.Health, 4,
                      "B1 ★★★ **context=10 能执行**：手牌监听器对敌督军造成 **4** 点（原版 `IsRightContext` 的 `10 Hand` 支）"
                    + "｜🧨 判据写反（例如「手牌里一律不执行」）⇒ 实得 0 ⇒ 红");
            }
        }

        // B2 context=0 ⇒ 被拒（0 点伤害）—— 这一笔要修的 over-fire
        {
            var ctx = MakeHandBattle(play0, out var foe);
            if (ctx == null || foe == null) { Report(false, "B2 前提不成立：对局没建起来"); }
            else
            {
                int hp0 = foe.Health;
                RuleCore.BroadcastWhen(ctx, WhenEventKind.GainFaith, 0, play0, subject: null);
                Check(hp0 - foe.Health, 0,
                      "B2 ★★★ **context=0 被拒**：手牌里那张 `When you gain Faith, …` **一点血都不掉**（原版 `0 Play` 要 `IsInPlay`）"
                    + "｜🧨 删掉 `BroadcastHandWhen` 里那个 `!HandListenerContextAllows(c, kind)` ⇒ 实得 4 ⇒ 红");
            }
        }

        // B3 `-1`（未知）⇒ 放行（保持改前行为，别把旧表/未知一起砍掉）
        {
            var ctx = MakeHandBattle(unknown, out var foe);
            if (ctx == null || foe == null) { Report(false, "B3 前提不成立：对局没建起来"); }
            else
            {
                int hp0 = foe.Health;
                RuleCore.BroadcastWhen(ctx, WhenEventKind.GainFaith, 0, unknown, subject: null);
                Check(hp0 - foe.Health, 4,
                      "B3 ★★★ **`-1` 放行**：旧表/unknown 仍按改前行为执行（⛔ 不静默砍监听）"
                    + "｜🧨 把 `-1` 也拒掉 ⇒ 实得 0 ⇒ 红");
            }
        }
    }

    // ==================================================================
    //  C · 结构（两道闸真的接在两个循环里 —— 只加辅助方法不接线 = 死代码）
    // ==================================================================
    static void GroupC_Structure()
    {
        Section("C · 结构：两条文本路的逐张循环里各有一道闸");

        string core = ReadSource("RuleEngine/Core/EffectResolver.cs");
        CheckTrue(!string.IsNullOrEmpty(core), "C0-前提 前提：读得到 `RuleEngine/Core/EffectResolver.cs`");
        if (string.IsNullOrEmpty(core)) return;

        CheckRaw(core.IndexOf("static bool HandListenerContextAllows(CardDef card, string kind)", StringComparison.Ordinal) >= 0,
                 "C0 ★★ 辅助方法 `HandListenerContextAllows` 存在"
               + "｜🧨 删掉它 ⇒ C1/C2 的调用点编不过（这行先红垫底）");

        string handWhen = Cut(core, "static void BroadcastHandWhen(", "static void HandTrapListenerDeferred(");
        CheckTrue(handWhen.Length > 0, "C1-前提 前提：切得出 `BroadcastHandWhen` 的方法体");
        if (handWhen.Length > 0)
            CheckRaw(handWhen.IndexOf("!HandListenerContextAllows(c, kind)", StringComparison.Ordinal) >= 0,
                     "C1 ★★★ **闸 A 真的在 `BroadcastHandWhen` 的逐张循环里**（手牌单位卡那一支）"
                   + "｜🧨 删掉那一句 ⇒ B2 变红、本行红");

        string handTrap = Cut(core, "static void BroadcastHandTrapWhen(", "static void BroadcastHandWhen(");
        CheckTrue(handTrap.Length > 0, "C2-前提 前提：切得出 `BroadcastHandTrapWhen` 的方法体");
        if (handTrap.Length > 0)
            CheckRaw(handTrap.IndexOf("!HandListenerContextAllows(c, kind)", StringComparison.Ordinal) >= 0,
                     "C2 ★★★ **闸 B 真的在 `BroadcastHandTrapWhen` 的逐张循环里**（非单位卡手牌陷阱那一支）"
                   + "｜🧨 删掉那一句 ⇒ 本行红（`Jammed Communications` 那一档会静默失效）");
    }

    // ==================================================================
    //  D · 第二刀 `AbilityTriggerPresence == 0`（2026-11-04 · `B142_E47`）
    //     + 预扫与逐张同口径（`B142-HAND-PRESCAN`）
    // ==================================================================
    static void GroupD_PresenceCut()
    {
        Section("D · `presence == 0` 拒 / `presence == -1` 放行 / 预扫与逐张同口径");

        // ── D1 · 隔离实验：第一刀（context）放行，只有第二刀（presence）能拒 ──
        //   夹具 = 显式 `Hand` 的 context 行 **+** `abilityTriggers` 里**没有** 665。
        var noPresence = WatcherNoPresence("B142E47NoPresence");
        Check(noPresence.AbilityTriggerPresence("OnCollectFaith"), 0,
              "D1-前提 （前提）夹具的 `abilityTriggers` **明确没有** `OnCollectFaith(665)` ⇒ presence = 0");
        Check(RuleCore.AbilityContextOfAbilityV14(noPresence, "OnCollectFaith", null), CardDef.AbilityContextHand,
              "D1-前提2 （前提）它**有**一条显式 `Hand` 的 context 行 ⇒ **第一刀单独看是放行的**（两刀隔离得开）");
        CheckTrue(!RuleCore.HandListenerContextAllowsForTest(noPresence, WhenEventKind.GainFaith),
                  "D1 ★★★ **presence = 0 ⇒ 拒**（原版 `RawCardScript__TriggerAbilities.c:108/207` 先在 `cardAbilities` 里"
                + "筛该 trigger 的行；筛不到 ⇒ 一条都不问 `CanTriggerAbility`、**什么都不做**）"
                + "｜🧨 删掉 `if (card.AbilityTriggerPresence(tn) == 0) return false;` ⇒ 本行红（实得 true = 又回到过宽）");

        // D1b 对照：同一形状，只把 665 补进 `abilityTriggers` ⇒ 放行（证明 D1 的红不是因为别的东西）
        var withPresence = WatcherWithPresence("B142E47WithPresence");
        Check(withPresence.AbilityTriggerPresence("OnCollectFaith"), 1,
              "D1b-前提 （前提）对照卡 `abilityTriggers` 含 665 ⇒ presence = 1");
        CheckTrue(RuleCore.HandListenerContextAllowsForTest(withPresence, WhenEventKind.GainFaith),
                  "D1b ★★ 对照：同形状 + presence = 1 + context `Hand` ⇒ **放行**（⇒ D1 的「拒」确实来自第二刀，不是夹具坏了）");

        // ── D2 · `-1`（未知：旧表 / 缺 `abilityTriggers` 列）**必须放行** ──
        var unknownPresence = WatcherUnknownPresence("B142E47UnknownPresence");
        Check(unknownPresence.AbilityTriggerPresence("OnCollectFaith"), -1,
              "D2-前提 （前提）`abilityTriggersVersioned = false` ⇒ presence = **-1（未知）**");
        Check(RuleCore.AbilityContextOfAbilityV14(unknownPresence, "OnCollectFaith", null), CardDef.AbilityContextHand,
              "D2-前提2 （前提）它同样有显式 `Hand` context 行（第一刀放行）⇒ 只剩第二刀在判");
        CheckTrue(RuleCore.HandListenerContextAllowsForTest(unknownPresence, WhenEventKind.GainFaith),
                  "D2 ★★★ **presence = `-1`（未知）放行** —— 旧卡表 / 缺列不许被当成「明确没有」"
                + "｜🧨 写成 `!= 1 ⇒ 拒` ⇒ 本行红（会把旧卡表的监听静默砍光）");

        // ── D3 · 结构：预扫与逐张**同一口径**（⛔ 只改预扫 = 真漏判；只改逐张 = 预扫白建快照）──
        string core = ReadSource("RuleEngine/Core/EffectResolver.cs");
        CheckTrue(!string.IsNullOrEmpty(core), "D3-前提 前提：读得到 `RuleEngine/Core/EffectResolver.cs`");
        if (string.IsNullOrEmpty(core)) return;
        string handWhen = Cut(core, "static void BroadcastHandWhen(", "static void HandTrapListenerDeferred(");
        CheckTrue(handWhen.Length > 0, "D3-前提2 前提：切得出 `BroadcastHandWhen` 的方法体");
        if (handWhen.Length > 0)
        {
            string prescan = Cut(handWhen, "bool any = false;", "if (!any) return;");
            CheckTrue(prescan.Length > 0, "D3-前提3 前提：切得出早退预扫那一段（`bool any = false;` … `if (!any) return;`）");
            if (prescan.Length > 0)
                CheckRaw(prescan.IndexOf("HandListenerContextAllows(c0, kind)", StringComparison.Ordinal) >= 0,
                         "D3a ★★★ **预扫与逐张同口径**：预扫也带 `HandListenerContextAllows`（context 闸 + presence 第二刀）"
                       + "｜🧨 只改逐张、不改预扫 ⇒ 本行红；反过来说，**预扫比逐张严 = 真漏判**，所以两处必须一起改");
            CheckRaw(handWhen.IndexOf("HandListenerContextAllows(c, kind)", StringComparison.Ordinal) >= 0,
                     "D3b ★★★ **逐张闸还在**（预扫只是省算力，真正挡的是这一句）"
                   + "｜🧨 只改预扫、删掉这一句 ⇒ 本行红 + D4a 实得 8（= 真漏判/过宽同时回来）");
        }

        // ── D4 · 端到端：混合手牌只有「合法那一张」响 ──
        //   ⚠️ 这一条同时是「只改预扫」的灭自证：只改预扫 ⇒ 预扫照样放行（合法那张让它 any=true），
        //      而逐张没有闸 ⇒ 两张都响 ⇒ 实得 8 而不是 4。
        {
            var ctx = MakeHandBattleMulti(out var foe, noPresence, withPresence);
            if (ctx == null || foe == null) { Report(false, "D4 前提不成立：对局没建起来"); }
            else
            {
                int hp0 = foe.Health;
                RuleCore.BroadcastWhen(ctx, WhenEventKind.GainFaith, 0, withPresence, subject: null);
                Check(hp0 - foe.Health, 4,
                      "D4a ★★★ **混合手牌：只有 presence 合法的那一张响**（敌督军 −**4**，不是 −8）"
                    + "｜🧨 只改预扫不改逐张 ⇒ 实得 8 ⇒ 红；🧨 把第二刀写成 `!= 1` ⇒ 两张都拒 ⇒ 实得 0 ⇒ 红");
            }
        }
        {
            var ctx = MakeHandBattleMulti(out var foe, noPresence);
            if (ctx == null || foe == null) { Report(false, "D4 前提不成立：对局没建起来"); }
            else
            {
                int hp0 = foe.Health;
                RuleCore.BroadcastWhen(ctx, WhenEventKind.GainFaith, 0, noPresence, subject: null);
                Check(hp0 - foe.Health, 0,
                      "D4b ★★★ **手牌里只有 presence = 0 的监听者 ⇒ 一点血都不掉**（预扫早退 + 逐张闸两条路都挡住）"
                    + "｜🧨 删掉任意一处闸 ⇒ 实得 4 ⇒ 红（本行同时钉「预扫改了、逐张没改」这一档）");
            }
        }

        // ── D5 · 池内事实：这一刀不是空转（有真卡落在上面）──
        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B142_E47] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "D5-前提 前提：卡池读得到（现读 = " + (pool != null ? pool.Count : -1) + "）");
        if (pool == null || pool.Count <= 500) return;

        int nGateA = 0, nPresence0 = 0, nAnyReject = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || !c.CanListenForEvents || c.WhenTriggers.Count == 0) continue;   // = 闸 A 的放行面
            nGateA++;
            bool presence0 = false, reject = false;
            for (int k = 0; k < c.WhenTriggers.Count; k++)
            {
                var t = c.WhenTriggers[k];
                if (t == null || t.Ev == null) continue;
                string tn = RuleCore.TriggerNameOfWhenKindForTest(t.Ev.Kind);
                if (c.AbilityTriggerPresence(tn) == 0) presence0 = true;
                if (!RuleCore.HandListenerContextAllowsForTest(c, t.Ev.Kind)) reject = true;
            }
            if (presence0) nPresence0++;
            if (reject) nAnyReject++;
        }
        Check(nGateA, 83, "D5 ★★ 闸 A 的放行面仍是 **83 张**（与 A5 同口径；R12 表 2a 定义重数，R12 当时 79 + 新增 4）");
        CheckTrue(nPresence0 >= 1,
                  "D5b ★★★ **这一刀在池内不是空转**：落在「presence = 0」上的卡 = **" + nPresence0 + " 张**"
                + "（现读；含 `SOR10` / `UM81`）｜🧨 若为 0 ⇒ 先复核 `TriggerNameOfWhenKind` 的映射，别当它已对齐");
        Debug.Log("[B142_E47] 池内：闸 A " + nGateA + " 张 · presence=0 命中 " + nPresence0
                  + " 张 · 任意一条 When 被拒（两刀合计） " + nAnyReject + " 张");

        var sor10 = CardDatabase.Find(pool, "Sister Repentia");
        CheckTrue(sor10 != null, "D5c-前提 前提：真卡 `Sister Repentia`（SOR10）在池里");
        if (sor10 != null)
        {
            Check(sor10.AbilityTriggerPresence("Slay"), 0,
                  "D5c-前提2 （前提）SOR10 的 `abilityTriggers` = `[90,130]` ⇒ 卡面 `Kills`（→`Slay(120)`）**presence = 0**");
            Check(RuleCore.HandListenerContextAllowsForTest(sor10, WhenEventKind.Kills), false,
                  "D5c ★★★ 真卡 SOR10：`When this unit kills an enemy, …` 在手牌里 **被拒**（原版筛不到 `Slay` 那条 ability）"
                + "｜🧨 删掉第二刀 ⇒ 实得 true ⇒ 红");
        }

        var um81 = CardDatabase.Find(pool, "Eliminator Sergeant");
        CheckTrue(um81 != null, "D5d-前提 前提：真卡 `Eliminator Sergeant`（UM81）在池里");
        if (um81 != null)
        {
            Check(um81.AbilityTriggerPresence("OtherUnitSummoned"), 0,
                  "D5d-前提2 （前提）UM81 的 `abilityTriggers` = `[10]` ⇒ `Deploy`（→`OtherUnitSummoned(190)`）**presence = 0**");
            Check(RuleCore.HandListenerContextAllowsForTest(um81, WhenEventKind.Deploy), false,
                  "D5d ★★★ 真卡 UM81：`When you deploy an Eliminator give it Stealth` 在手牌里 **被拒**"
                + "（原版这条文字**没有对应的 ability 行** ⇒ 什么都不做）"
                + "｜🧨 删掉第二刀 ⇒ 实得 true ⇒ 红（真局里「手里攥着 UM81 + 部署另一张 Eliminator」会白给 Stealth）");
        }
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>手牌监听夹具：一张单位卡，卡面写 SOR38 那句 `When you gain Faith, deal 4 damage to the enemy warlord`。
    /// `abilityTriggers` 恒含 `OnCollectFaith(665)`；<paramref name="versioned"/> 决定 `abilityContext` 那一列算不算数。</summary>
    static CardDef Watcher(string id, IEnumerable<CardDef.AbilityContextSpec> contexts, bool versioned)
    {
        return new CardDef(id, id, "unit", "When you gain Faith, deal 4 damage to the enemy warlord",
                           "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry",
                           abilityContext: contexts, abilityContextsVersioned: versioned,
                           abilityTriggers: new[] { 665 }, abilityTriggersVersioned: true);
    }

    /// <summary>D 组夹具（第一刀放行 / 第二刀拒）：显式 `Hand` 的 context 行 **+** `abilityTriggers` 里**没有** 665。</summary>
    static CardDef WatcherNoPresence(string id)
    {
        return new CardDef(id, id, "unit", "When you gain Faith, deal 4 damage to the enemy warlord",
                           "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry",
                           abilityContext: new[]
                           {
                               new CardDef.AbilityContextSpec { Trigger = 665, Name = "OnCollectFaith", Context = CardDef.AbilityContextHand },
                           },
                           abilityContextsVersioned: true,
                           abilityTriggers: new[] { 90, 130 }, abilityTriggersVersioned: true);
    }

    /// <summary>D1b 对照：形状同上，但 `abilityTriggers` **含** 665（presence = 1 ⇒ 两刀都放行）。</summary>
    static CardDef WatcherWithPresence(string id)
    {
        return new CardDef(id, id, "unit", "When you gain Faith, deal 4 damage to the enemy warlord",
                           "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry",
                           abilityContext: new[]
                           {
                               new CardDef.AbilityContextSpec { Trigger = 665, Name = "OnCollectFaith", Context = CardDef.AbilityContextHand },
                           },
                           abilityContextsVersioned: true,
                           abilityTriggers: new[] { 665, 90, 130 }, abilityTriggersVersioned: true);
    }

    /// <summary>D2 夹具：`abilityTriggers` 列**来自旧表**（`abilityTriggersVersioned: false`）⇒ presence 恒 `-1`（未知）。</summary>
    static CardDef WatcherUnknownPresence(string id)
    {
        return new CardDef(id, id, "unit", "When you gain Faith, deal 4 damage to the enemy warlord",
                           "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry",
                           abilityContext: new[]
                           {
                               new CardDef.AbilityContextSpec { Trigger = 665, Name = "OnCollectFaith", Context = CardDef.AbilityContextHand },
                           },
                           abilityContextsVersioned: true,
                           abilityTriggers: null, abilityTriggersVersioned: false);
    }

    /// <summary>D 组用：一局里 P0 手里**依次只有**这些卡（其余全清空），P1 那侧留一张督军当靶子。</summary>
    static BattleContext MakeHandBattleMulti(out UnitState foeWarlord, params CardDef[] handCards)
    {
        foeWarlord = null;
        if (handCards == null || handCards.Length == 0) return null;
        var heroA = new CardDef("B142E47HeroA", "B142E47HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B142E47HeroB", "B142E47HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var deckA = new List<CardDef> { heroA };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B142E47FillA" + i)); deckB.Add(PlainUnit("B142E47FillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        if (ctx == null) return null;
        ctx.Players[0].Hand.Clear();
        for (int i = 0; i < handCards.Length; i++)
            if (handCards[i] != null) ctx.Players[0].Hand.Add(ctx.NewInstance(handCards[i]));
        foeWarlord = ctx.Players[1].Warlord;
        return ctx;
    }

    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>建一局：P0 手里**只有**这一张夹具卡（其余全清空），P1 那侧留一张督军当靶子。</summary>
    static BattleContext MakeHandBattle(CardDef handCard, out UnitState foeWarlord)
    {
        foeWarlord = null;
        if (handCard == null) return null;
        var heroA = new CardDef("B142E36HeroA", "B142E36HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B142E36HeroB", "B142E36HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var deckA = new List<CardDef> { heroA };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B142E36FillA" + i)); deckB.Add(PlainUnit("B142E36FillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        if (ctx == null) return null;
        // ⚠️ 手牌清空再放 —— 起手那几张会干扰「谁响了几遍」的读数（与 `RuleEngineTest` 的夹具同形）
        ctx.Players[0].Hand.Clear();
        ctx.Players[0].Hand.Add(ctx.NewInstance(handCard));
        foeWarlord = ctx.Players[1].Warlord;
        return ctx;
    }

    // ==================================================================
    //  源码文本处理（与 `B142_E24_Assert` 同形）
    // ==================================================================
    static string ReadSource(string rel)
    {
        string path = Path.Combine(Application.dataPath, rel);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    /// <summary>从 `start` 切到 `end`（切不到返回空串 —— 调用方必须断前提）。</summary>
    static string Cut(string s, string start, string end)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int i0 = s.IndexOf(start, StringComparison.Ordinal);
        if (i0 < 0) return "";
        int i1 = s.IndexOf(end, i0 + start.Length, StringComparison.Ordinal);
        return i1 > i0 ? s.Substring(i0, i1 - i0) : s.Substring(i0);
    }

    // ==================================================================
    //  最小断言框架（本文件独立类 ⇒ 不借用 `RuleEngineTest` 的 private 助手）
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        object a = actual, e = expected;
        Report(ok, msg + "（实际 = " + (a == null ? "null" : a.ToString())
                        + "，期望 = " + (e == null ? "null" : e.ToString()) + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }
    static void CheckRaw(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }
}
