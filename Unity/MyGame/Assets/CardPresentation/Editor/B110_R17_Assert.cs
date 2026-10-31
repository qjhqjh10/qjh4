// B110_R17_Assert.cs — 执行代理 `B110_D1`（2026-10-31）的断言：**环境件的 501 挪层**（判据 §1 批 A + A′）
//
// ============================ 它盯的是什么 ============================
// 判据 `判据/B109_R17_挪层分片.md` §0 汇总 / §1 分片表：
//   · **批 A**：`EnvironmentApplier.Apply` 里那句 `Instantiate(prefab, pos, rot)`（根、无父）建出来的
//     **环境件**，原来**完全不过** `GrabScreen501.FinalLayer` ⇒ 停在 prefab 自带层 6，开 `WF_GRAB501=1`
//     时既不搬层、也不被 501 重画。本批把那一句接到 `EnvironmentApplier.RelocateGrabLayer`（类尾）。
//   · **批 A′**：`ParticleSystemAreaSpawner.CreatePooledItem` 的池件同理（模板里**确实有**失真材质——
//     现读 2 条，见那条改动处的注释）。它与批 A 走**同一个函数**（⛔ 不是第二份判据）。
//
// ============================ 口径（⛔ 别拿我们自己的常量证明我们自己的实现）============================
//  · 「哪个 prefab 算失真件」= 生产判据本身 `GrabScreen501.UsesDistortion`（反射读到**同一个方法**，
//    本文件不另写一份 shader 名单）。
//  · 「期望层」= `GrabScreen501.LayerIndex`（现读 9，🔶 层号本身是我们挑的：原版那份
//    `UniversalRendererData` 资产本地没解出来 —— 见 `GrabScreen501` 的 doc）。
//  · 前提断言与结论断言**分开写**：⛔ 不用 `if (x != null) Check(...)`（前提不成立会静默跳过 —— 铁律）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 正样例（`Environmental Condition Black Legion Warp Storm`，在 233 名单内）：
//      · 开档时把 `RelocateGrabLayer` 的 `SetLayerRec` 删掉 ⇒ 根层仍是 6 ⇒ 红；
//      · 只设根不设子树（把 `SetLayerRec` 换成 `go.layer = layer`）⇒「整棵子树同层」那条红。
//  · **反样例**（`Environmental Condition Astra Militarum Dawn Attack`，**不在**名单内）：
//      · 把 `FinalLayer` 的 `UsesDistortion` 闸去掉（无条件搬层）⇒ 它的层也变 ⇒ 红；
//      · 把 `RelocateGrabLayer` 写成无条件 `go.layer = LayerIndex` ⇒ 它同样变 ⇒ 红。
//  · 默认档（不设 `WF_GRAB501`）：`FinalLayer` 返回入参 `-1` ⇒ 两件**层一个都不许变**（逐位等价）。
//  · A′：模板子树**确实**带失真材质（硬前提，没有它就等于没查）＋ 池件跟着搬层 / 默认档不动。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进任一宿主的聚合行（同一物理行、`//` 之前）：
//      B110R17_Assert.Run(); _sink.Pass += B110R17_Assert.Pass; _sink.Fail += B110R17_Assert.Fail;
//  ⚠️ **本件不驱动 `EnvironmentApplier.Apply`**：那会改 `RenderSettings`（雾/环境光）并给场景里真实对象
//     挂 blendable 组件（`EnsureSceneBlendables`）⇒ 污染同一宿主里跑在后面的断言，而且 `Battle*Scene.Run`
//     会把这个场景存盘。本件只 `Instantiate` 一份 prefab + 调**生产同一个函数** `RelocateGrabLayer`。
//     ⇒ 已知边界：**`Apply` 里那个调用点被删掉时，本件仍会绿**（函数还在）。那一跳由批 C 真机验（判据 §3-6）。
//  ⚠️ 开档分支（「整棵树搬到第 9 层」）只在宿主以 `WF_GRAB501=1` 起时才会跑；没开时本件**出声说明没跑**，
//     ⛔ 不算通过（见 `Debug.LogWarning` 那一行）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;

public static class B110R17_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110R17] ");

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>判据 §1 批 A 的 10 件名单（`判据/B109_R17_挪层分片.md:57-62`，逐字照抄，⛔ 别改）。</summary>
    static readonly string[] BatchA = new string[]
    {
        "Environmental Condition Astra Militarum Planetary Invasion",
        "Environmental Condition Black Legion Daemonic Feast",
        "Environmental Condition Black Legion Helfire Outburst",
        "Environmental Condition Black Legion Warp Storm",
        "Environmental Condition Emperor's Children 1 Green",
        "Environmental Condition Emperor's Children 1 Pink REJECTED",
        "Environmental Condition GSC Sump Overspill",
        "EnvironmentalCondition Tau Electro-Static Interference",
        "EnvironmentalCondition Tau Radiation Storm",
        "EnvironmentalCondition Ultramarines Bombardment",
    };

    /// <summary>正样例：在 233 名单内（清单 §三 第 110 行）。</summary>
    const string PosSample = "Environmental Condition Black Legion Warp Storm";
    /// <summary>反样例（灭自证）：**不在**名单内 ⇒ 开档后层必须不变。</summary>
    const string NegSample = "Environmental Condition Astra Militarum Dawn Attack";
    /// <summary>A′ 的样例：它的 `Far explosion` 模板子树里有 `RippleSubtle Distort`（判据 §1 批 A′）。</summary>
    const string APrimeEnv = "Environmental Condition Astra Militarum Planetary Invasion";
    /// <summary>A′ 的模板对象名（`EnvBlendables.json` 的 `standalone` 里那条 `particleSystemPrefab` 的末段）。</summary>
    const string APrimeTemplate = "Far explosion";

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B110_D1：环境件 501 挪层（批 A 的 10 件 + 批 A′ 的池件）");

        // ---------------- 前提（硬检查，⛔ 不许「前提不成立就跳过」）----------------
        MenuCheck.True(_sink, Array.IndexOf(BatchA, PosSample) >= 0,
                       $"前提：正样例 `{PosSample}` 在批 A 的 10 件名单里");
        MenuCheck.True(_sink, Array.IndexOf(BatchA, NegSample) < 0,
                       $"灭自证前提：负样例 `{NegSample}` **不在**名单里（否则它就不是反样例）");
        MenuCheck.True(_sink, Array.IndexOf(BatchA, APrimeEnv) >= 0,
                       $"前提（A′）：`{APrimeEnv}` 也在批 A 的 10 件名单里");

        var lib = WarpforgeEffectLibrary.Instance;
        MenuCheck.True(_sink, lib != null,
                       "前提：`WarpforgeEffectLibrary` 取得到（取不到 ⇒ 下面每一条都无从谈起）");
        var posPrefab = Prefab(lib, PosSample);
        var negPrefab = Prefab(lib, NegSample);
        MenuCheck.True(_sink, posPrefab != null, $"前提：正样例 prefab `{PosSample}` 在效果库里");
        MenuCheck.True(_sink, negPrefab != null, $"前提：负样例 prefab `{NegSample}` 在效果库里");
        // ⚠️ 上面两条是**硬断言**（红就是红）；这两个 `return` 只是「已经报过红了，别在下面抛 NRE」。
        if (posPrefab == null || negPrefab == null) return;

        // ---------------- 批 A：`EnvironmentApplier.RelocateGrabLayer` 的两半 ----------------
        // 与 `Apply` 里同形：`Instantiate(prefab)` = 根、无父 ⇒ 层 = prefab 自带层（现读 6）。
        var pos = UnityEngine.Object.Instantiate(posPrefab);
        var neg = UnityEngine.Object.Instantiate(negPrefab);
        try
        {
            pos.name = "B110R17_Pos";
            neg.name = "B110R17_Neg";

            // 前提（硬检查）：「是不是失真件」= 整条链的唯一输入，用**生产判据本身**读（反射，不另写一份）。
            MenuCheck.True(_sink, IsDistortion(pos),
                           "前提（硬检查）：正样例实例 `GrabScreen501.UsesDistortion` = 真（这条不成立 ⇒ 下面全是空转）");
            MenuCheck.True(_sink, !IsDistortion(neg),
                           "灭自证前提（硬检查）：负样例实例 `UsesDistortion` = 假");

            int posLayer0 = pos.layer, negLayer0 = neg.layer;
            var posBefore = LayersOf(pos);
            var negBefore = LayersOf(neg);
            MenuCheck.True(_sink, posBefore.Count > 1 && negBefore.Count > 1,
                           $"前提：两件都是**多节点**子树（正 {posBefore.Count} 个 / 负 {negBefore.Count} 个）"
                           + "—— 只有多节点，「整棵子树」才不是空话");

            int posBack = EnvironmentApplier.RelocateGrabLayer(pos);
            int negBack = EnvironmentApplier.RelocateGrabLayer(neg);

            if (!GrabScreen501.Enabled)
            {
                // 默认档：逐位等价 —— 不设层、不搬子树、返回 -1（连日志都不打，判据见 `GrabScreen501` doc）
                MenuCheck.Check(_sink, posBack, -1,
                                "默认档（不设 `WF_GRAB501`）：正样例 `RelocateGrabLayer` 返回 -1（= 不设层）");
                MenuCheck.Check(_sink, pos.layer, posLayer0, "默认档：正样例根层**不变**");
                MenuCheck.Check(_sink, SameLayers(posBefore, pos), true, "默认档：正样例**整棵子树**层逐个不变");
                MenuCheck.Check(_sink, negBack, -1, "默认档：负样例同样返回 -1");
                MenuCheck.Check(_sink, neg.layer, negLayer0, "默认档：负样例根层不变");
                MenuCheck.Check(_sink, SameLayers(negBefore, neg), true, "默认档：负样例整棵子树层逐个不变");
                Debug.LogWarning("[B110R17] ⚠️ **开档分支本次没跑**（本进程 `WF_GRAB501` 未开）—— "
                               + "「整棵树搬到第 " + GrabScreen501.LayerIndex + " 层」那几条**不算通过**。"
                               + "要跑它们请以 `WF_GRAB501=1` 起宿主（判据 §3-6 另有真机验收）。");
            }
            else
            {
                MenuCheck.Check(_sink, posBack, GrabScreen501.LayerIndex,
                                "开档：正样例 `RelocateGrabLayer` 返回 `LayerIndex`");
                MenuCheck.Check(_sink, pos.layer, GrabScreen501.LayerIndex,
                                "开档：正样例**根层** = `LayerIndex`（现读 9）");
                MenuCheck.Check(_sink, AllAt(pos, GrabScreen501.LayerIndex), true,
                                $"开档：正样例**整棵子树**同层（{pos.transform.GetComponentsInChildren<Transform>(true).Length} 个节点）");
                MenuCheck.Check(_sink, negBack, -1, "灭自证：负样例（非失真环境件）返回 -1");
                MenuCheck.Check(_sink, neg.layer, negLayer0, "灭自证：负样例根层**必须不变**");
                MenuCheck.Check(_sink, SameLayers(negBefore, neg), true, "灭自证：负样例整棵子树层逐个不变");
                MenuCheck.Check(_sink, neg.layer != GrabScreen501.LayerIndex, true,
                                "灭自证：负样例**没有**被搬到 501 层（正/负走同一条链，只差 `UsesDistortion`）");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(pos);
            UnityEngine.Object.DestroyImmediate(neg);
        }

        RunAPrime(lib);
    }

    /// <summary>批 A′：`ParticleSystemAreaSpawner.CreatePooledItem` 建出来的**池件**也要过 501 挪层。</summary>
    static void RunAPrime(WarpforgeEffectLibrary lib)
    {
        var prefab = Prefab(lib, APrimeEnv);
        MenuCheck.True(_sink, prefab != null, $"前提（A′）：`{APrimeEnv}` 取得到");
        if (prefab == null) return;

        var host = UnityEngine.Object.Instantiate(prefab);   // 模板是它的子件（`EnvBlendables.json` 记的是层级路径）
        try
        {
            var tr = host.transform.Find(APrimeTemplate);    // 路径：`<env root>/Far explosion`
            MenuCheck.True(_sink, tr != null, $"前提（A′）：模板对象 `{APrimeTemplate}` 在环境 prefab 根下");
            if (tr == null) return;
            var tpl = tr.GetComponent<ParticleSystem>();
            MenuCheck.True(_sink, tpl != null, $"前提（A′）：`{APrimeTemplate}` 上真的有 `ParticleSystem`");
            if (tpl == null) return;
            MenuCheck.True(_sink, IsDistortion(tpl.gameObject),
                           "前提（A′，硬检查）：这个模板子树里**确实有**失真材质（没有它 ⇒ A′ 这条等于没查，"
                           + "判据 §4-② 就该结案成「0 件」）");

            int tplLayer0 = tpl.gameObject.layer;
            var spGo = new GameObject("B110R17_Spawner");
            try
            {
                var sp = spGo.AddComponent<ParticleSystemAreaSpawner>();
                // `autoSpawn = false`：只建池、不起循环（本件不需要时间推进，也避免随机粒子污染）
                sp.Configure(Vector3.one, tpl, 2, false, 1f, 1f);
                var pooled = sp.Pool.Get();
                MenuCheck.True(_sink, pooled != null, "A′：池件建出来了（`CreatePooledItem` 走完整条）");
                if (pooled != null)
                {
                    if (!GrabScreen501.Enabled)
                    {
                        MenuCheck.Check(_sink, pooled.gameObject.layer, tplLayer0,
                                        "默认档（A′）：池件层 = 模板层（**不动**）");
                    }
                    else
                    {
                        MenuCheck.Check(_sink, pooled.gameObject.layer, GrabScreen501.LayerIndex,
                                        "开档（A′）：池件根层 = `LayerIndex`");
                        MenuCheck.Check(_sink, AllAt(pooled.gameObject, GrabScreen501.LayerIndex), true,
                                        "开档（A′）：池件**整棵子树**同层");
                    }
                    sp.Pool.Release(pooled);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(spGo);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    // ---------------- 小工具 ----------------

    static GameObject Prefab(WarpforgeEffectLibrary lib, string name)
    {
        if (lib == null || string.IsNullOrEmpty(name)) return null;
        WFEffectEntry e;
        if (!lib.TryGet(name, out e) || e == null) return null;
        return e.prefab;
    }

    static readonly MethodInfo _usesDistortion =
        typeof(GrabScreen501).GetMethod("UsesDistortion", BindingFlags.NonPublic | BindingFlags.Static);

    /// <summary>生产判据本身（`GrabScreen501.UsesDistortion`）—— ⛔ 本文件不另写一份 shader 名单。
    /// `null` = 那条私有判据改名/没了 ⇒ **必须红**（前提断言会读到假）。</summary>
    static bool IsDistortion(GameObject go)
    {
        if (_usesDistortion == null || go == null) return false;
        return (bool)_usesDistortion.Invoke(null, new object[] { go });
    }

    /// <summary>整棵子树（含 inactive）的层快照 —— 逐节点比「改前/改后」。</summary>
    static List<int> LayersOf(GameObject go)
    {
        var list = new List<int>();
        if (go == null) return list;
        var all = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++) list.Add(all[i].gameObject.layer);
        return list;
    }

    static bool SameLayers(List<int> before, GameObject after)
    {
        var now = LayersOf(after);
        if (before.Count != now.Count) return false;
        for (int i = 0; i < before.Count; i++) if (before[i] != now[i]) return false;
        return true;
    }

    static bool AllAt(GameObject go, int layer)
    {
        if (go == null) return false;
        var all = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++) if (all[i].gameObject.layer != layer) return false;
        return true;
    }
}
