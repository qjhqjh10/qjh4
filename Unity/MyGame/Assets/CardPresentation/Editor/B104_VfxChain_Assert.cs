// B104_VfxChain_Assert.cs — 第一百零四会话 · 执行代理 `B104_VfxMap`（2026-10-31）
//
// ============================ 它盯的是什么 ============================
// 本代理在 `CardPresentation/Core/VfxMap.cs` 落地的**唯一一件行为改动**：
//   · **`VfxMap.PreloadTraitEffect` / `PreloadTraitEffectsOfCard(s)`** —— 原版
//     `BattleManager.PreloadTraitEffect`（判据 → `d:/2/tools/decomp_full/BattleManager__PreloadTraitEffect.c`
//     全文 + `AnimInfo__PreloadAssets.c` 三句 = `Load()` → `Instantiate` → `Destroy`）。
//     ⇒ `P1` 那 10 条是它的断言（正例 / 幂等 / 反例 / 逐件对账 / 卡级）。
//
// 外加三组**已经查实、但账（`判据/B9_特效触发链.tsv`）没更新**的回归闸 —— 它们**不改代码**，
// 只把「现在的结论」钉成可执行断言，防止后人改回去而无人发现：
//   · `P2` **AN1·R3**：凡 `CardDef.AnimTimes` 有 `Trigger == -1`（= 原版 `activeAbility`）行的卡，
//     `CardAnimOrder.TryGetBeats` 必须 ≥ 1 拍（现读 93 卡全中；数据侧 `B99_A2` 2026-10-30 已补）。
//   · `P3` **AN1·R12**：同一批卡的 `AbilityAnimDurations.TryGet` 必须命中（不许再退 1.0 s 兜底）。
//   · `P4` **触发链「未接」第 4 条（Ambush End(1)）**：数据面就是「1 条 End(1)、无 FromCode(5)」，
//     而消费端只认 0/2/3/6 ⇒ **原版不可达**（铁律 11 例外①）⇒ 我们留空 + 出声是**原版形状**。
//   · `P5` **触发链「未接」第 1/2 条（CancelAnimsFromUnit / RemoveDestroyableAnims）**：
//     前者原版**死代码**（四路查证 0 调用点）、后者**已接线**（`BattleDriver:23019`）。
//     ⇒ 这里只做**结构闸**（符号还在），行为级断言在 `Editor/B97W3_Assert.cs` 与 `B9` 的执行代理报告里。
//
// ============================ ⛔ 本件**故意**没有的断言 ============================
//   · **预热的生产调用点是否还在**（`BattleDriver.Begin` 里那两行）：本代理白名单**不含** `BattleDriver.cs`
//     ⇒ 那一行是交给调度台的**补丁**（见报告 `B104_VfxMap.md` §四）。调用点在不在**只能靠审查**，
//     ⛔ 不假装断言盖住了它。
//   · **`CancelAnimsFromUnit` / `RemoveDestroyableAnims` 的行为**：不在本件（见上面 P5 的理由）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · 把 `PreloadTraitEffect` 改成空转 / 不 `Instantiate` ⇒ `P1-1` / `P1-3` 红。
//   · 删掉 `_traitPreloaded` 去重（同一件每次都实例化）⇒ `P1-4` 红（第二次调用 `InstantiatedTotal` 会再涨）。
//   · 把 `PrefabOf` 的阵营参数丢掉（预热错件）⇒ `P1-5` 的**逐件对账**红。
//   · 让 `AllRowsFor` 只返回一本字典的行（不是「整列」）⇒ `P1-5` 红（少件）。
//   · 把 `PreloadTraitEffectsOfCard` 改成不看 `CardTraits`（比如拿 keywords 顶替）⇒ `P1-7` / `P1-8` 红。
//   · 把 `card_anim_order.json` 退回 62 卡（R3 回退）⇒ `P2-2` 红。
//   · 把某张卡的 `tg == -1` 行从 `animTimes` 里删掉（两链分叉）⇒ `P3-2` 红（表里有、卡上没行）。
//   · 给 `ambush` 补一条 `FromCode(5)` 行 ⇒ `P4-1`（恰好 1 行）/ `P4-2`（没有 5 档）红。
//   · 删/改名 `RemoveDestroyable` / `CancelPendingAnimsOf` / `AnimFXController.DoDestroy` ⇒ `P5` 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主里加一行 `B104_VfxChain_Assert.Run();`，然后并进自己的合计（读 `Failed == 0` 即可）。
//   本件**无副作用**：预热只 `Instantiate` + 立刻销毁（编辑模式下 `DestroyImmediate`）；
//   会**清零** `CardAnimOrder` 的自检计数器（`ResetCounters`）—— 那本来就是自检口径的计数器。
using System.Collections.Generic;
using CardPresentation;      // VfxMap / TraitFrames / CardAnimOrder / AbilityAnimDurations
using RuleEngine;            // CardDatabase / CardDef
using UnityEngine;

/// <summary>执行代理 `B104_VfxMap` 的断言集合（触发链 4 条「未接」+ AN1 R3/R12 的回归闸）。
/// <para>⛔ 期望值全是**从原版判据/数据现算**的，不从被测实现里反算。</para></summary>
public static class B104_VfxChain_Assert
{
    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    public static void Run()
    {
        Passed = 0; Failed = 0;
        Debug.Log("BT B104 --- 特效触发链：VfxMap 预热（B9 未接③）+ AN1 R3/R12 + Ambush End(1) 回归闸 ---");

        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0,
              "(前提) 卡池载得到（`CardDatabase.Load()`）—— 载不到的话下面 P2/P3 每条都**没验到**，别读成通过");
        Check(TraitFrames.BindingsLoaded && TraitFrames.BindingRowCount > 0,
              "(前提) `Resources/trait_effect_bindings.json` 载得到且有行（现读 " + TraitFrames.BindingRowCount + " 行）"
            + " —— 载不到的话 P1/P4 每条都**没验到**");

        Preload(pool);      // P1 —— 本代理唯一的代码改动
        R3(pool);           // P2 —— AN1·R3 回归
        R12(pool);          // P3 —— AN1·R12 回归
        AmbushEnd();        // P4 —— 触发链「未接」④：留空 = 原版形状
        WiredSymbols();     // P5 —— 触发链「未接」①/② 的结构闸

        Summary();
    }

    // ------------------------------------------------------------------
    //  P1 · `VfxMap.PreloadTraitEffect`（原版 `BattleManager.PreloadTraitEffect`）
    // ------------------------------------------------------------------
    static void Preload(List<CardDef> pool)
    {
        // 找一条**带真件**的绑定行当样本（三种 trigger 档都试；优先 Trigger(3)，因为它件最多）
        var sample = PickSampleRow();
        Check(sample.HasValue,
              "P1-0 (前提) 找到「某 trait 名下至少 1 件、且这些件在 `WarpforgeEffectLibrary` 里都取得到」的样本行"
            + "（拿不到 = 效果库 SO 没生成 / 绑定表为空 ⇒ 下面每一条都**没验到**）");
        if (!sample.HasValue) return;

        string trait = sample.Value.Trait;
        string samplePrefab = TraitFrames.PrefabOf(sample.Value, -1);
        Check(!string.IsNullOrEmpty(samplePrefab),
              $"P1-0b (前提) 样本 trait `{trait}` 的默认件解析得出（拿到 `{samplePrefab ?? "null"}`）");
        if (string.IsNullOrEmpty(samplePrefab)) return;

        // ---- P1-1 正例：至少预热到 1 件 ----
        VfxMap.ResetTraitPreloadForTest();
        int n1 = VfxMap.PreloadTraitEffect(trait, -1);
        Check(n1 >= 1, $"P1-1 `PreloadTraitEffect(\"{trait}\")` 解析出的件数 = {n1}（必须 ≥ 1）"
                     + "｜🧨 改成空转 / 不实例化 ⇒ 本条红");

        // ---- P1-2 计数自洽：不同件数 == 预热件数 ----
        Check(VfxMap.PreloadedTraitPrefabCount == n1,
              $"P1-2 去重后的预热件数 {VfxMap.PreloadedTraitPrefabCount} == 解析件数 {n1}（同一件只算一次）");

        // ---- P1-3 真实例化过（不是只记账） ----
        Check(VfxMap.TraitPrefabsInstantiatedTotal == n1,
              $"P1-3 真正 `Instantiate` 的次数 {VfxMap.TraitPrefabsInstantiatedTotal} == {n1}"
            + "｜🧨 只记名字不实例化 ⇒ 本条红");

        // ---- P1-4 幂等（灭自证：删掉去重会红） ----
        int instBefore = VfxMap.TraitPrefabsInstantiatedTotal;
        int n2 = VfxMap.PreloadTraitEffect(trait, -1);
        Check(n2 == n1, $"P1-4a 第二次调用解析件数不变（{n1} → {n2}）");
        Check(VfxMap.TraitPrefabsInstantiatedTotal == instBefore,
              $"P1-4b 第二次调用**不再实例化**（{instBefore} → {VfxMap.TraitPrefabsInstantiatedTotal}）"
            + "｜🧨 删掉 `_traitPreloaded` 去重 ⇒ 本条红");

        // ---- P1-5 逐件对账：预热的集合 == 该 trait 全部行的默认件（去重） ----
        var expect = new List<string>();
        var rows = TraitFrames.AllRowsFor(trait);
        for (int i = 0; i < rows.Length; i++)
        {
            string nm = TraitFrames.PrefabOf(rows[i], -1);
            if (string.IsNullOrEmpty(nm)) continue;
            if (!expect.Contains(nm)) expect.Add(nm);
        }
        expect.Sort(System.StringComparer.Ordinal);
        var got = VfxMap.PreloadedTraitPrefabsForTest();
        bool same = expect.Count == got.Length;
        if (same)
            for (int i = 0; i < expect.Count; i++)
                if (!string.Equals(expect[i], got[i], System.StringComparison.Ordinal)) { same = false; break; }
        Check(same,
              $"P1-5 预热件逐件对账：期望 [{string.Join(",", expect.ToArray())}] vs 实际 [{string.Join(",", got)}]"
            + "｜🧨 `AllRowsFor` 只给一本字典的行（不是「整列」）/ `PrefabOf` 丢阵营参数 ⇒ 本条红");

        // ---- P1-6 反例：不存在的 trait ⇒ 0 件、零副作用 ----
        VfxMap.ResetTraitPreloadForTest();
        int bad = VfxMap.PreloadTraitEffect("__b104_no_such_trait__", -1);
        Check(bad == 0 && VfxMap.PreloadedTraitPrefabCount == 0 && VfxMap.TraitPrefabsInstantiatedTotal == 0,
              $"P1-6 不存在的 trait ⇒ 解析 0 / 预热 0 / 实例化 0（实际 {bad} / {VfxMap.PreloadedTraitPrefabCount} / {VfxMap.TraitPrefabsInstantiatedTotal}）"
            + "｜🧨 把「查不到」写成「拿全库兜底」⇒ 本条红");

        // ---- P1-7 卡级正例：池里真卡（`CardTraits` 里有绑定得上的 trait） ----
        bool poolOk = pool != null && pool.Count > 0;
        CardDef hit = null; int hitRows = 0;
        if (poolOk)
            for (int i = 0; i < pool.Count && hit == null; i++)
            {
                var c = pool[i];
                if (c == null || c.CardTraits == null || c.CardTraits.Count == 0) continue;
                int army = TraitFrames.ArmyOfFaction(c.Faction);   // 与 `PreloadTraitEffectsOfCard` 用同一个阵营口径
                int rowsOfCard = 0;
                for (int j = 0; j < c.CardTraits.Count; j++)
                {
                    var te = c.CardTraits[j];
                    if (te == null || string.IsNullOrEmpty(te.Name)) continue;
                    var rr = TraitFrames.AllRowsFor(te.Name);
                    for (int k = 0; k < rr.Length; k++)
                        if (!string.IsNullOrEmpty(TraitFrames.PrefabOf(rr[k], army)) && AllPrefabsAvailable(te.Name, army)) { rowsOfCard++; break; }
                }
                if (rowsOfCard > 0) { hit = c; hitRows = rowsOfCard; }
            }
        int nc = 0;
        if (hit != null)
        {
            VfxMap.ResetTraitPreloadForTest();
            nc = VfxMap.PreloadTraitEffectsOfCard(hit);
        }
        // ⛔ 前提与结论**各一条、都无条件执行**（⛔ 不写成 `if (hit != null) Check(...)` —— 那会静默跳过）
        Check(poolOk && hit != null,
              "P1-7 (前提) 池里找得到「`CardTraits` 至少有一条能绑到真件」的卡（池 " + (pool == null ? -1 : pool.Count) + " 张）");
        Check(hit != null && nc >= 1 && VfxMap.TraitPrefabsInstantiatedTotal >= 1,
              $"P1-7b 卡级预热 {(hit == null ? "（无样本卡）" : hit.Id + " " + hit.Name)}（{hitRows} 个带件的 trait）"
            + $"⇒ 解析 {nc} 件、实例化 {VfxMap.TraitPrefabsInstantiatedTotal} 次（都必须 ≥ 1）"
            + "｜🧨 不看 `CardTraits`（比如拿 keywords 顶替）⇒ 本条红");

        // ---- P1-8 卡级反例：没有 trait / 没喂 trait 的卡都不能凭空预热 ----
        CardDef noTrait = null;
        if (poolOk)
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c != null && c.CardTraits != null && c.CardTraits.Count == 0) { noTrait = c; break; }
            }
        VfxMap.ResetTraitPreloadForTest();
        int nzNull = VfxMap.PreloadTraitEffectsOfCard(null) + VfxMap.PreloadTraitEffectsOfCards(null);
        int nzEmpty = noTrait != null ? VfxMap.PreloadTraitEffectsOfCard(noTrait) : 0;
        Check(nzNull == 0, $"P1-8a `null` 卡 / `null` 列表 ⇒ 0（实际 {nzNull}）");
        Check(poolOk && noTrait != null,
              "P1-8b (前提) 池里应有一张 `cardTraits` 为空的卡（用来断「没有 trait 就不预热」）");
        Check(nzNull == 0 && nzEmpty == 0 && VfxMap.PreloadedTraitPrefabCount == 0,
              $"P1-8c 没有 trait 的卡 {(noTrait == null ? "（无样本）" : noTrait.Id + " " + noTrait.Name)}"
            + $" ⇒ 0 件、预热表仍空（实际 解析 {nzEmpty} / 表 {VfxMap.PreloadedTraitPrefabCount}）"
            + "｜🧨 把「没有 trait」当成「全库兜底」⇒ 本条红");

        VfxMap.ResetTraitPreloadForTest();   // 收尾：别把计数留给下一批断言
    }

    /// <summary>挑一条带真件的绑定行（Trigger(3) 优先，其次 2 / 0 / 6 / 1 / 5）。
    /// ⚠️ 额外要求：**这个 trait 名下所有解析得出的 prefab 都在效果库里** —— 否则 P1-2/3/5 会被
    /// 「库缺一件」搅成红，那不是被测实现的错（缺件本身由 `TraitPreloadFailures` 计入并出声）。</summary>
    static TraitFrames.TraitEffectBinding? PickSampleRow()
    {
        int[] order = { 3, 2, 0, 6, 1, 5 };
        for (int oi = 0; oi < order.Length; oi++)
        {
            var rr = TraitFrames.RowsOfTriggerType(order[oi]);
            for (int i = 0; i < rr.Length; i++)
                if (AllPrefabsAvailable(rr[i].Trait, -1)) return rr[i];
        }
        return null;
    }

    /// <summary>这个 trait 名下**每一件**解析得出的 prefab 都能在 `WarpforgeEffectLibrary` 里取到（且至少 1 件）。</summary>
    static bool AllPrefabsAvailable(string trait, int army)
    {
        var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
        if (lib == null) return false;
        var rows = TraitFrames.AllRowsFor(trait);
        int ok = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            string nm = TraitFrames.PrefabOf(rows[i], army);
            if (string.IsNullOrEmpty(nm)) continue;
            WarpforgeVFX.WFEffectEntry e;
            if (!lib.TryGet(nm, out e) || e == null || e.prefab == null) return false;
            ok++;
        }
        return ok > 0;
    }

    // ------------------------------------------------------------------
    //  P2 · AN1·R3：`animTimes` 里 `Trigger == -1` 的卡必须在演出拍表里
    // ------------------------------------------------------------------
    static void R3(List<CardDef> pool)
    {
        if (pool == null || pool.Count == 0) return;
        var buf = new List<CardAnimBeat>();
        CardAnimOrder.ResetCounters();
        int want = 0, bad = 0; string firstBad = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || !HasActiveAbilityAnimRow(c)) continue;
            want++;
            buf.Clear();
            if (CardAnimOrder.TryGetBeats(c.Name, buf) < 1)
            {
                bad++;
                if (firstBad == null) firstBad = c.Id + " " + c.Name;
            }
        }
        CardAnimOrder.ResetCounters();      // 把上面这轮累积的 FallbackHits / 出声去重清掉

        Check(want > 0, $"P2-0 (前提) 池里至少有一张卡带 `tg == -1` 的动画行（现读 {want} 张）—— 0 张的话本条空转");
        Check(bad == 0, $"P2-1 凡 `AnimTimes` 有 `Trigger == -1` 行的卡，`CardAnimOrder.TryGetBeats` 都必须 ≥ 1 拍"
                      + $"（{want} 张里不中 {bad} 张" + (firstBad != null ? "，第一张 " + firstBad : "") + "）"
                      + "｜🧨 把 `card_anim_order.json` 退回 62 卡（AN1·R3 原状）⇒ 本条红（现读 93 卡全中）");

        // 反例：不存在的卡必须 0（证明这条判据会红，不是恒真）
        buf.Clear();
        int bogus = CardAnimOrder.TryGetBeats("__b104_no_such_card__", buf);
        CardAnimOrder.ResetCounters();
        Check(bogus == 0, $"P2-2 不存在的卡 ⇒ `TryGetBeats` = 0（实际 {bogus}）｜证明 P2-1 不是恒真");
    }

    // ------------------------------------------------------------------
    //  P3 · AN1·R12：同一批卡的能力时长不许退 1.0 s 兜底
    // ------------------------------------------------------------------
    static void R12(List<CardDef> pool)
    {
        if (pool == null || pool.Count == 0) return;
        int want = 0, miss = 0; string firstMiss = null;
        CardDef noRowUnique = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            bool has = HasActiveAbilityAnimRow(c);
            if (has)
            {
                want++;
                float s;
                if (!AbilityAnimDurations.TryGet(c.Name, out s))
                {
                    miss++;
                    if (firstMiss == null) firstMiss = c.Id + " " + c.Name;
                }
            }
            else if (noRowUnique == null && CountSameName(pool, c.Name) == 1)
            {
                noRowUnique = c;            // 没有 tg=-1 行、且卡名全池唯一 ⇒ 必不在时长表里（两链现读一致）
            }
        }

        Check(want > 0, $"P3-0 (前提) 同一批「有 `tg == -1` 行」的卡非空（现读 {want} 张）");
        Check(miss == 0, $"P3-1 这些卡的 `AbilityAnimDurations.TryGet` 必须全部命中（{want} 张里退兜底 {miss} 张"
                       + (firstMiss != null ? "，第一张 " + firstMiss : "") + "）"
                       + "｜🧨 时长表与卡表分叉（AN1·R12 原状）⇒ 本条红");

        // 反例两条**无条件执行**（⛔ 不写成 `if (noRowUnique != null) Check(...)` —— 那会静默跳过）
        float s2 = -1f;
        bool bogusHit = noRowUnique != null && AbilityAnimDurations.TryGet(noRowUnique.Name, out s2);
        Check(noRowUnique != null,
              "P3-2 (前提) 池里有一张「没有 `tg == -1` 行、且卡名全池唯一」的卡（用来断反例）");
        Check(noRowUnique != null && !bogusHit,
              "P3-3 反例："
            + (noRowUnique == null ? "（无样本卡）" : "没有 `tg == -1` 行的卡 `" + noRowUnique.Id + " " + noRowUnique.Name + "`")
            + " 不在时长表里（实际命中=" + bogusHit + "）｜证明 P3-1 不是恒真");
    }

    static bool HasActiveAbilityAnimRow(CardDef c)
    {
        var at = c.AnimTimes;
        if (at == null) return false;
        for (int i = 0; i < at.Count; i++)
            if (at[i] != null && at[i].Trigger == -1) return true;
        return false;
    }

    static int CountSameName(List<CardDef> pool, string name)
    {
        int n = 0;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && string.Equals(pool[i].Name, name, System.StringComparison.Ordinal)) n++;
        return n;
    }

    // ------------------------------------------------------------------
    //  P4 · 触发链「未接」④：Ambush 只有一条 End(1)，原版**没有消费口**
    // ------------------------------------------------------------------
    static void AmbushEnd()
    {
        var rows = TraitFrames.AllRowsFor("ambush");
        int n = rows == null ? 0 : rows.Length;
        // ⛔ 五条**都无条件执行**（⛔ 不写成「前提不成立就整段 return」—— 那样后面几条会静默不跑）
        Check(n == 1,
              "P4-1 `Ambush` 名下恰好 1 条 `TraitEffect`（实际 " + n + " 条）"
            + "｜🧨 给人造一条 FromCode(5) ⇒ 本条红（那等于编原版没有的播放时机）");
        Check(n >= 1 && rows[0].TriggerType == 1,
              "P4-2 那一条的 `TriggerType` = 1（End），实际 " + (n >= 1 ? rows[0].TriggerType.ToString() : "（没有行）"));
        Check(n >= 1 && rows[0].TriggerType != 5,
              "P4-3 它不是 FromCode(5)（= 原版没有任何代码按『触发那一刻』播它）");
        Check(VfxMap.IsKnownGapEvent(VfxMap.AmbushExit),
              "P4-4 `VfxMap.IsKnownGapEvent(\"ambush_exit\")` = 真（留空 + 出声一次，⛔ 不拿替代品顶）");
        Check(VfxMap.Resolve(VfxMap.AmbushExit) == null,
              "P4-5 `VfxMap.Resolve(\"ambush_exit\")` = null（= 原版不可达那档的正确输出）");
    }

    // ------------------------------------------------------------------
    //  P5 · 触发链「未接」①/②：一个原版死代码、一个早已接线 —— 只钉符号
    // ------------------------------------------------------------------
    static void WiredSymbols()
    {
        Check(typeof(BattleDriver).GetMethod("CancelPendingAnimsOf",
                  new[] { typeof(int), typeof(int) }) != null,
              "P5-1 `BattleDriver.CancelPendingAnimsOf(int,int)` 还在（= `CancelAnimFromUnitFromQueue` 的等价物；"
            + "原版 `CancelAnimsFromUnit` 本身是死代码，四路查证 0 调用点）");
        Check(typeof(BattleDriver).GetMethod("RemoveDestroyableAnims",
                  new[] { typeof(int), typeof(int), typeof(string) }) != null,
              "P5-2 `BattleDriver.RemoveDestroyableAnims(int,int,string)` 还在（原版 `RemoveDestroyableAnims` 的表现层入口）");
        Check(typeof(TraitFrames).GetMethod("RemoveDestroyable", new[] { typeof(string) }) != null,
              "P5-3 `TraitFrames.RemoveDestroyable(string)` 还在（两本字典逐个 `DoDestroy`）");
        Check(typeof(AnimFXController).GetMethod("DoDestroy", System.Type.EmptyTypes) != null,
              "P5-4 `AnimFXController.DoDestroy()` 还在（判据 `AnimFXController__DoDestroy.c`）");
        Check(typeof(RuleEngine.EvtKind).GetField("EnchantmentRemoved") != null,
              "P5-5 `EvtKind.EnchantmentRemoved` 还在（= 驱动 `RemoveDestroyableAnims` 的那条表现信号）");
    }

    // ------------------------------------------------------------------
    static void Check(bool ok, string msg)
    {
        if (ok) { Passed++; Debug.Log("  ✓ " + msg); }
        else { Failed++; Debug.LogError("  ✗ " + msg); }
    }

    static void Summary()
    {
        Debug.Log($"BT B104 --- 合计 {Passed + Failed} 条：通过 {Passed} / 失败 {Failed} ---");
    }
}
