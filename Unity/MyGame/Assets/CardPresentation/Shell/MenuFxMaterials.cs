// MenuFxMaterials.cs —— 菜单侧「原版 UI 材质」的取用口（`B109_E33` 新建）
//
// 为什么单开一个小门面：这三条（#4 Pulsating / #5 Glow SDF Noise / #9 UI Clock）一共要挂到
// 10+ 个调用点上，而每处都写一遍 `Resources.Load` + 空值告警 = 10 份「静默失败」的机会。
// 这里把**取不到就出声**收成一处（铁律：不许静默失败）。
//
// 材质本体 = `Resources/Art/vfx/WFUI_*.mat`（与既有 `Resources/Art/vfx/UI Additive.mat` 同一条先例，
// 加载口写法也照 `Shell/RankedDivisionChangeWindow.cs:1355` 的 `Resources.Load<Material>("Art/vfx/…")`）。
// 参数一律逐字段抄原版材质 JSON，出处写在各 `.mat` 对应的 shader 文件头。
//
// ⚠️ **调用顺序纪律**（`Battle/ImageQuad.cs:296` 的 `SetMaterial` 会保留换之前的渲染队列）：
//    先 `SetRenderQueue`、**再** `SetMaterial`。本门面的两个 `Apply` 都在调用方**建完之后**才用，天然满足。
//
// 🔴🔴 **每一颗 quad 必须拿到【自己的】`Material` 实例（2026-10-31 `B109_E33` 续办 · `B109_D2` §3 的 5 条红）**：
//    `ImageQuad.SetTint` / `SetRenderQueue` 写的是 **`_mr.sharedMaterial.color` / `.renderQueue`**
//    （`ImageQuad.cs:400` / `:425`），也就是说「染色 / 队列」这两个**per-quad** 的状态**存在材质实例上**。
//    一旦把 `Resources.Load` 拿到的**那一份共享材质**直接挂给多颗 quad（本类第一版就是这么干的），
//    它们就共用同一个 `renderQueue` / `_Color` ⇒ **最后建的那颗盖掉全部**
//    （实测：联盟 10 颗奖杯描边共用队列 3263，染色被抹成白；时钟 4 处同病）。
//    ⇒ `Apply` / `ApplyAll` 一律 **`new Material(模板)` 逐颗克隆**，并把**换之前的 `_Color`（= 当时的 tint）带过去**
//      （渲染队列由 `ImageQuad.SetMaterial` 自己从旧材质读出来带过去，见 `ImageQuad.cs:299-303`）。
//    ⚠️ 克隆体不回收：这些窗口是「建一次用很久」，量级 = 每个窗口十几份，可接受；⛔ 但**别**在每帧调用的路径里挂。

using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class MenuFxMaterials
    {
        // ---- 资源路径（`Resources.Load` 用，不带扩展名）----
        public const string PulsatingForge        = "Art/vfx/WFUI_Pulsating_Forge";
        public const string PulsatingCampaign     = "Art/vfx/WFUI_Pulsating_Campaign";
        public const string Clock                 = "Art/vfx/WFUI_Clock";
        public const string GlowSdfBorderChest    = "Art/vfx/WFUI_GlowSdf_BorderChest";
        public const string GlowSdfBorderChestCollect = "Art/vfx/WFUI_GlowSdf_BorderChestCollect";
        public const string GlowSdfHighlightRed   = "Art/vfx/WFUI_GlowSdf_HighlightRed";
        public const string GlowSdfHighlightSmoothRed = "Art/vfx/WFUI_GlowSdf_HighlightSmoothRed";
        public const string GlowSdfAllianceTrophy = "Art/vfx/WFUI_GlowSdf_AllianceTrophy";

        static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();
        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>取材质；取不到**只告警一次**（同一个路径），并且不抛。</summary>
        public static Material Load(string resPath)
        {
            Material m;
            if (_cache.TryGetValue(resPath, out m) && m != null) return m;
            m = Resources.Load<Material>(resPath);
            _cache[resPath] = m;
            if (m == null && _warned.Add(resPath))
                Debug.LogWarning("[MenuFxMaterials] 取不到材质 `Resources/" + resPath + "`"
                               + " ⇒ 该处退回原静态图/静态色（出声，不静默）");
            return m;
        }

        /// <summary>挂到**一张** quad 上。⚠️ **每颗克隆一份**（见文件头那条 🔴）：
        /// 共享材质的 `renderQueue`/`_Color` 就是 quad 自己的队列与染色，共用 = 互相覆盖。
        /// 渲染队列由 `ImageQuad.SetMaterial` 从**旧材质**读出并带过去；这里只负责把 `_Color` 也带过去。</summary>
        public static void Apply(ImageQuad q, string resPath)
        {
            if (q == null) return;
            var proto = Load(resPath);
            if (proto == null) return;
            Color tint = q.Tint;                       // 换之前那一份（`SetTint` 写的就是 sharedMaterial.color）
            var inst = new Material(proto);            // ⛔ 别把 `proto` 本体挂上去
            if (inst.HasProperty("_Color")) inst.color = tint;
            q.SetMaterial(inst);
        }

        /// <summary>挂到一整个九宫格/子树（`MenuDraw.Nine` / `NineSoft` 返回的是 `GameObject`，下面挂着多块子 quad）。
        /// 🔴 **逐颗转调 `Apply`** —— 九宫格的每一块都是独立 quad，必须各拿一份材质实例。</summary>
        public static void ApplyAll(GameObject go, string resPath)
        {
            if (go == null) return;
            if (Load(resPath) == null) return;
            var quads = go.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++) Apply(quads[i], resPath);
        }
    }
}
