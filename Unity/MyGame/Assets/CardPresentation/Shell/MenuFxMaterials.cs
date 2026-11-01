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
            // 🔴 缓存 / 取用的**唯一实现**在类尾 `LoadCached` —— 本方法只负责「取不到就出声」。
            //    `Proto`（带 shader 退路那一档）走的是同一个缓存，⛔ 别在这里另抄一份查表。
            var m = LoadCached(resPath);
            if (m != null) return m;
            if (_warned.Add("mat:" + resPath + "|"))
                Debug.LogWarning("[MenuFxMaterials] 取不到材质 `Resources/" + resPath + "`"
                               + " ⇒ 该处退回原静态图/静态色（出声，不静默）");
            return m;
        }

        /// <summary>挂到**一张** quad 上。⚠️ **每颗克隆一份**（见文件头那条 🔴）：
        /// 共享材质的 `renderQueue`/`_Color` 就是 quad 自己的队列与染色，共用 = 互相覆盖。
        /// 渲染队列由 `ImageQuad.SetMaterial` 从**旧材质**读出并带过去；这里只负责把 `_Color` 也带过去。</summary>
        public static void Apply(ImageQuad q, string resPath)
        {
            Apply(q, resPath, null);
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

        // ════════════════════════════════════════════════════════════════════════════════════════
        // 🆕 2026-11-01（第一百一十一会话 · `B111_S4` · 活状态表 `B-21` 收口）：下面这一块是**收口**。
        //   收口前 `Shell/` 下有 **3 份**同一套「`Resources.Load` → 逐颗 `new Material(proto)` → 带过 `_Color`」：
        //   本文件 · `ExpansionPassEventWindow.cs:2836`（`B110_M2` 的 #6 Distort 等价实现）·
        //   `ParentEventWindow.cs:2589`（`B110_D4` 的 #11/#7 等价实现）。铁律：「两处写同一条规则 = 迟早不一致」。
        //   ⇒ 路径常量 + 取用 + 逐颗克隆**只留这一处**；那两处的补丁见 `报告/B111_S4.md` §B-21。
        //   ⛔ 别在调用点再写 `"Art/vfx/WFUI_…"` 字面量（本类常量是唯一一份）。
        // ════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>#7 `UI Additive`（`Blend One One` 纯加色）—— 原版 `Material_-8464978790139598553.json`。
        /// 判据 → `判据/B109_R15_菜单13材质.md` §1·7；shader = `Resources/Shaders/WFUIAdditive.shader`。</summary>
        public const string Additive          = "Art/vfx/WFUI_Additive";

        /// <summary>#11 `UI Scan Lines Alpha Mask` —— 原版 `Material_-8529927314253658951.json`。
        /// 判据 → `判据/B109_R15_菜单13材质.md` §1·11；shader = `Resources/Shaders/WFUIScanLines.shader`。</summary>
        public const string ScanLines         = "Art/vfx/WFUI_ScanLines";

        /// <summary>#6 `UI Distort in Atlas Small objects` —— 原版 `Material_-6878048798577840027.json`。
        /// 判据 → `判据/B109_R15_菜单13材质.md` §1·6；shader = `Resources/Shaders/WFUIDistort.shader`。</summary>
        public const string DistortAtlasSmall = "Art/vfx/WFUI_Distort_AtlasSmall";

        /// <summary>`Shader.Find` 的结果缓存（`Resources/` 里还没落 `.mat` 时那条兜底路用）。</summary>
        static readonly Dictionary<string, Shader> _shaderCache = new Dictionary<string, Shader>();

        /// <summary>`Shader.Find` 现建的**原型**材质（按 `路径|shader 名` 缓存；⛔ 它不是挂到 quad 上的那一份）。</summary>
        static readonly Dictionary<string, Material> _madeProto = new Dictionary<string, Material>();

        /// <summary>取材质（**不告警**；缓存与本类其他入口共用）。取不到返回 `null`。</summary>
        static Material LoadCached(string resPath)
        {
            Material m;
            if (_cache.TryGetValue(resPath, out m) && m != null) return m;
            m = Resources.Load<Material>(resPath);
            _cache[resPath] = m;
            return m;
        }

        /// <summary>取**原型材质**：先 `Resources.Load`；取不到再按 shader 名现建一份
        /// （= 原版 shader 的默认参数，`ParentEventWindow` 那条「M2/M3 还没落盘也能用」的兜底路）。
        /// 两者都拿不到 ⇒ **只告警一次**并返回 `null`（调用方保留原静态图，⛔ 不静默）。
        /// <para>⚠️ 返回的是**原型**：挂到 quad 上必须先 `new Material(proto)`（见 `Apply`）。</para></summary>
        public static Material Proto(string resPath, string shaderName = null)
        {
            var m = LoadCached(resPath);
            if (m != null) return m;

            if (!string.IsNullOrEmpty(shaderName))
            {
                var madeKey = resPath + "|" + shaderName;
                Material made;
                if (_madeProto.TryGetValue(madeKey, out made) && made != null) return made;

                Shader sh;
                if (!_shaderCache.TryGetValue(shaderName, out sh) || sh == null)
                {
                    sh = Shader.Find(shaderName);
                    _shaderCache[shaderName] = sh;
                }
                if (sh != null)
                {
                    made = new Material(sh);
                    _madeProto[madeKey] = made;
                    return made;
                }
            }

            if (_warned.Add("mat:" + resPath + "|" + (shaderName ?? "")))
                Debug.LogWarning("[MenuFxMaterials] 取不到材质 `Resources/" + resPath + "`"
                               + (string.IsNullOrEmpty(shaderName) ? "" : " 与 shader `" + shaderName + "`")
                               + " ⇒ 该处退回原静态图/静态色（出声，不静默）");
            return null;
        }

        /// <summary>同 <see cref="Apply(ImageQuad,string)"/>，但多一档 **shader 兜底**
        /// （`Resources/` 里还没落 `.mat` 时按原版 shader 的默认参数现建）。
        /// 🔴 每颗 quad 仍然**各拿一份克隆** —— 理由见文件头那条（`renderQueue` / `_Color` 是 per-quad 的）。</summary>
        public static void Apply(ImageQuad q, string resPath, string shaderName)
        {
            if (q == null) return;
            var proto = Proto(resPath, shaderName);
            if (proto == null) return;
            Color tint = q.Tint;                       // 换之前那一份（`SetTint` 写的就是 sharedMaterial.color）
            var inst = new Material(proto);            // ⛔ 别把 `proto` 本体挂上去
            if (inst.HasProperty("_Color")) inst.color = tint;
            q.SetMaterial(inst);                       // 队列由 `ImageQuad.SetMaterial` 从旧材质读走（`:299-303`）
        }

        // ════════════════════════════════════════════════════════════════════════════════════════
        // 🆕 2026-11-01（`B-187` / `B-188`）：两件「原版 shader 取**原件**」的等价件。
        //   它们**不走**上面那套 `Resources/Art/vfx/WFUI_*.mat`，因为这两个原版 shader **都在随包
        //   `wf_shaders.bundle` 里**（判据双证：① `资料/完全复刻/资源/R4_Shader_VFX_清单.tsv` 两行都写
        //   `bundle_shaders_assets_all.bundle`；② 本机实读该包 45 个 Shader 对象的 PathID，见下两个常量），
        //   而 bundle 里的 shader **落不成工程资产**（`WarpforgeShaderLoader.cs` 文件头；Unity 拒收）
        //   ⇒ 参数只能写在**运行期材质**上，`.mat` 这条落不了。
        //   取件走 `WarpforgeShaderMap.TryResolve(原版名)` ⇒ 落「原版bundle」= **原件、零猜测**
        //   （与 `Shell/ForgeTab.cs:1143` 取 `UI Border Highlight Appear` 同一条路、同一个理由）。
        // ⚠️ 与原版材质 JSON 里的**死值**：`_Length` / `_MaskScale` / `_ScaleFromCenter` / `_Add` /
        //   `_Range` / `_RangeStartOffset` / `_NoiseStrength` 在 `Border Highlight Rotating Campaign`
        //   上有值，但**原版 shader 的属性表里没有它们**
        //   （判据 = `WarpforgeVFX/Runtime/OriginalShaderBlendTable.cs:389` 那一行）⇒ 一律**不写**
        //   （写了也是 no-op，抄进来只会让人以为它们在生效）。
        // ════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 shader 名（`B-187`）—— 材质 `Border Highlight Rotating Campaign` 用的那一支。
        /// 本工程对应 PathID = **-4463273156108355430**（`wf_shaders.bundle` 实读）。</summary>
        public const string ShaderBorderRotatingCampaign = "Everguild/UI/UI Border Highlight Appear Pulsating Scale";

        /// <summary>原版 shader 名（`B-188`）—— 材质 `UI Orange Button To Purple` 用的那一支。
        /// 本工程对应 PathID = **-1970915450731268797**（`wf_shaders.bundle` 实读）。</summary>
        public const string ShaderSpriteHueColorChange = "Shader Graphs/Sprite HUE Color change";

        static Material _borderRotatingCampaignProto;
        static Material _orangeToPurpleProto;

        /// <summary>`B-187`：把原版 `Border Highlight Rotating Campaign`
        /// （`bundle_menus_assets_all/Material/Material_1780489488266546679.json`）挂到一张 quad 上。
        /// 消费者 = `Campaign Node/Collectable Highlight`（`Shell/CampaignTab.cs` 的 `BuildNode`）。
        /// <para>⚠️ 原版那颗是 **shader 自走 `_Time`**（`_AUTOROTATION = 1`、`_AutoRotationSpeed = -0.5`）
        /// ⇒ 调用方**不要**再补间 `_Reveal`（那是 `UI Border Highlight Appear` 那一支的驱动方式）。</para>
        /// <para>⚠️ **不能**拿 `#4 Pulsating`（`WFUI_Pulsating_Campaign`）顶这一颗：那条是另一支 shader
        /// （`Everguild/UI/Pulsating`，属性 `_AlphaPulsating`/`_SizeRotation`），与这里毫无关系。</para></summary>
        public static void ApplyBorderHighlightRotatingCampaign(ImageQuad q)
        {
            ApplyProto(q, BorderRotatingCampaignProto());
        }

        /// <summary>`B-188`：把原版 `UI Orange Button To Purple`
        /// （`bundle_scenes_scenes_mainmenuwarpforge/Material/Material_6.json`，guid
        /// `b4649ed10edec2348bd3205ab1b94b83`，`_Hue_Offset = 0.679`）挂到一张 quad 上。
        /// 消费者 = 原版 `Toast Collection Level Up` / `Toast Collection Get XP` 两个 prefab 的
        /// **根 `UnityEngine.UI.Image`**（本仓对应物 = 这两条 toast 根下 `Background` 那颗九宫格的每一块）。
        /// <para>🔴 **这颗材质没有 `_Color`**（原版 shader 属性表 = `_Brightness/_Contrast/_Hue_Offset/
        /// _MainTex/_Saturation`，判据 = `OriginalShaderBlendTable.cs:422`）⇒ 挂在它上面的 quad
        /// **不能**再走 `ImageQuad.SetTint`（那是写 `_Color`，会报「Material doesn't have a color
        /// property '_Color'」并且整层跳过）—— 调用方要先把不可染色的那些剔掉，见
        /// `ToastNotifications.HueNoColorTintable`。</para></summary>
        public static void ApplyOrangeButtonToPurple(ImageQuad q)
        {
            ApplyProto(q, OrangeToPurpleProto());
        }

        /// <summary>把**原型材质**挂到一张 quad 上 —— 与 <see cref="Apply(ImageQuad,string)"/> 同一套纪律：
        /// 每颗**克隆一份**（`renderQueue` / `_Color` 是 per-quad 的），并把换之前那份可带的 `_Color` 带过去。
        /// ⚠️ 原型 shader **没有** `_Color` 时（`B-188` 那一支）**什么都不写** —— ⛔ 别让 `.color` 的 setter
        /// 去打一条 Unity 报错。</summary>
        static void ApplyProto(ImageQuad q, Material proto)
        {
            if (q == null || proto == null) return;
            Color tint = q.Tint;                       // 旧材质（Sprites/Default 那一档）上的那份
            var inst = new Material(proto);
            // 🔴 2026-11-01（`B112W_B08`）：原版 `Border Highlight *` / `Sprite HUE Color change` 这几支
            //    **属性表里没有 `_Color`** ⇒ 换材质会把「染色/α」整份丢掉，而且后续任何 `SetTint` 都会打
            //    `Material doesn't have a color property '_Color'`。改成**走顶点色通道**（`ImageQuad.SetVertexTint`，
            //    `B-190` 那条）—— 原版 shader 读的正是顶点 `COLOR`，语义反而更贴原版。
            //    ⚠️ 必须在 `SetMaterial` **之前**取 `q.Tint`（换完读到的就是新材质了）。
            if (inst.HasProperty("_Color")) inst.color = tint;
            else q.SetVertexTint(tint);
            q.SetMaterial(inst);                       // 队列由 `ImageQuad.SetMaterial` 从旧材质读走
        }

        /// <summary>`B-187` 的原型材质；参数逐字段抄 `Material_1780489488266546679.json`（2026-11-01 现读）。</summary>
        static Material BorderRotatingCampaignProto()
        {
            if (_borderRotatingCampaignProto != null) return _borderRotatingCampaignProto;
            Shader sh;
            string src;
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(ShaderBorderRotatingCampaign, out sh, out src) || sh == null)
            {
                Debug.LogWarning("[MenuFxMaterials] 取不到原版 shader `" + ShaderBorderRotatingCampaign + "`"
                               + " ⇒ `Campaign Node/Collectable Highlight` 只用静态图、不旋转（出声，不静默）");
                return null;
            }
            var m = new Material(sh);
            m.name = "Border Highlight Rotating Campaign (runtime)";
            void C(string n, Color c) { if (m.HasProperty(n)) m.SetColor(n, c); }
            void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
            // 颜色（JSON 的 `m_Colors`，逐值原样；`_ColorSecondary` 的 b = 2.27 是 HDR，别夹到 1）
            C("_ColorMain",       new Color(0f, 0.248011648f, 0.460784316f, 1f));
            C("_ColorSecondary",  new Color(0f, 0.136698350f, 2.270602941f, 1f));
            C("_Color_offsets",   new Color(0.92f, 1f, 0f, 0f));
            C("_Noise_Strength_X_Speed_Y_Scale_Z", new Color(0.02f, 0.0159154944f, 7.09f, 0f));
            C("_Scale_X_Speed_Y", new Color(0.9f, 3f, 0f, 0f));
            // 浮点（同 JSON 的 `m_Floats`）
            F("_Reveal", 1f);
            F("_Smoothness", 0.706f);
            F("_Rotation", 0f);
            F("_StartOffset", 0f);
            F("_AUTOROTATION", 1f);
            F("_AutoRotationSpeed", -0.5f);
            F("_FLIPDIRECTION", 1f);
            F("_DEBUG", 0f);
            F("_QueueControl", 0f);
            F("_QueueOffset", 0f);
            // 关键字（JSON 的 `m_ValidKeywords` = 这两条）
            m.EnableKeyword("_AUTOROTATION");
            m.EnableKeyword("_FLIPDIRECTION");
            // `_Noise_R` = pid `-8214795764591836323`；工程侧现成名字（`Shell/ForgeTab.cs:1169` 已在用）
            var noise = CardArt.MenuUi("UI_Dirt_And_Noise_skratches");
            if (noise != null && m.HasProperty("_Noise_R")) m.SetTexture("_Noise_R", noise);
            else if (noise == null)
                Debug.LogWarning("[MenuFxMaterials] 取不到 `UI_Dirt_And_Noise_skratches` ⇒ "
                               + "`Border Highlight Rotating Campaign` 的 `_Noise_R` 空着（出声，不静默）");
            _borderRotatingCampaignProto = m;
            return m;
        }

        /// <summary>`B-188` 的原型材质；参数逐字段抄 `UI Orange Button To Purple.mat`（2026-11-01 现读）。</summary>
        static Material OrangeToPurpleProto()
        {
            if (_orangeToPurpleProto != null) return _orangeToPurpleProto;
            Shader sh;
            string src;
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(ShaderSpriteHueColorChange, out sh, out src) || sh == null)
            {
                Debug.LogWarning("[MenuFxMaterials] 取不到原版 shader `" + ShaderSpriteHueColorChange + "`"
                               + " ⇒ `Toast Collection Level Up` / `Get XP` 的根底图保持原色（出声，不静默）");
                return null;
            }
            var m = new Material(sh);
            m.name = "UI Orange Button To Purple (runtime)";
            void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
            F("_Hue_Offset", 0.679f);
            F("_Contrast",   1f);
            F("_Saturation", 1f);
            F("_Brightness", 0f);
            _orangeToPurpleProto = m;
            return m;
        }

        // ════════════════════════════════════════════════════════════════════════════════════════
        // 🆕 2026-11-01（第一百一十二会话 · `B112W_B08` = `B-08` 的 Border Highlight 族）
        //   与上面 `B-187` 那两条**同一条路**（原件 shader 在 `wf_shaders.bundle` 里 ⇒ 运行期 `new Material`），
        //   差别只在「一支 shader 有 6 份材质」⇒ 参数做成实参、按 `key` 缓存原型。
        //   判据 → `资料/完全复刻/判据/B112W_B08_接线图.md` §表 2（逐材质 `m_SavedProperties` 全参数，
        //   2026-11-01 由 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/Material/*.json` 现读）。
        //   ⚠️ 只写**原版 shader 属性表里真有的**名字（`HasProperty` 兜底）；材质 JSON 里
        //   `_Length` / `_MaskScale` / `_Add` / `_Range` / `_RangeStartOffset` / `_NoiseStrength` 是**死值**，
        //   属性表里没有 ⇒ ⛔ 不抄（与 `BorderRotatingCampaignProto` 同一条纪律）。
        // ════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 shader 名 —— `Border Highlight Appear *` 那 6 份材质的 shader
        /// （`Shader_6773909295900798026.json` 实读，住 `bundle_shaders_assets_all`）。</summary>
        public const string ShaderBorderHighlightAppear = "Everguild/UI/UI Border Highlight Appear";

        /// <summary>原版 `Border Highlight Appear *` 那 6 份材质（`B-08` #1/#2/#4/#5/#6/#7）。
        /// 名字 = 原版材质名去掉 `Border Highlight Appear ` 前缀。</summary>
        public enum BorderHighlightAppear
        {
            ExpansionPass,   // #1 Material_-265774635809289417
            Green,           // #2 Material_-4286932454265387033
            PurpleNoSmooth,  // #4 Material_-1689040976176618117
            Raid,            // #5 Material_-5332349378384099707
            White,           // #6 Material_-3131437003372528232
            RotationYellow,  // #7 Material_5632552898289262306
        }

        static readonly Dictionary<string, Material> _borderAppearProtos = new Dictionary<string, Material>();

        /// <summary>把 `Border Highlight Appear <paramref name="kind"/>` 挂到**一张** quad 上。</summary>
        public static void ApplyBorderHighlightAppear(ImageQuad q, BorderHighlightAppear kind)
        {
            ApplyProto(q, BorderAppearProto(kind));
        }

        /// <summary>挂到一整个九宫格/子树（`MenuDraw.Nine` 返回 `GameObject`）—— 逐块克隆，见文件头 🔴。</summary>
        public static void ApplyBorderHighlightAppearAll(GameObject go, BorderHighlightAppear kind)
        {
            if (go == null) return;
            var proto = BorderAppearProto(kind);
            if (proto == null) return;
            var quads = go.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++) ApplyProto(quads[i], proto);
        }

        /// <summary>一份原版 `Border Highlight Appear *` 材质的全部有效参数（逐值抄 JSON）。</summary>
        struct BorderAppearSpec
        {
            public Color main, secondary, colorOffsets, noise, scaleXSpeed;
            public float reveal, smoothness, rotation, autoSpeed, startOffset;
            public bool autoRotation, smoothGradient, debug;
        }

        /// <summary>逐份抄 `m_SavedProperties`（2026-11-01 现读；值到 7 位有效数字）。</summary>
        static BorderAppearSpec BorderAppearSpecOf(BorderHighlightAppear k)
        {
            var s = new BorderAppearSpec
            {
                colorOffsets = new Color(0.8f, 1f, 0f, 0f),
                noise        = new Color(0.02f, -0.1f, 8.17f, 0f),
                scaleXSpeed  = new Color(0.9f, 3f, 0f, 0f),
                smoothness   = 0.032f,
                autoSpeed    = -180f,
            };
            switch (k)
            {
                case BorderHighlightAppear.ExpansionPass:
                    s.main = new Color(0.86162621f, 2.90294719f, 4.24801731f, 1f);
                    s.secondary = new Color(0f, 0.15004349f, 1f, 1f);
                    s.reveal = 1f; s.smoothness = 0.169f; s.rotation = 7.42f;
                    s.autoSpeed = -186.64f; s.autoRotation = true; s.smoothGradient = true; s.debug = true;
                    break;
                case BorderHighlightAppear.Green:
                    s.main = new Color(0.20028184f, 1.31950796f, 0f, 1f);
                    s.secondary = new Color(0f, 1f, 0.17037797f, 1f);
                    s.reveal = 1f;
                    break;
                case BorderHighlightAppear.PurpleNoSmooth:
                    s.main = new Color(1f, 0f, 0.88149357f, 1f);
                    s.secondary = new Color(0.15395403f, 0f, 1f, 1f);
                    s.reveal = 0.723f;
                    break;
                case BorderHighlightAppear.Raid:
                    s.main = new Color(3.44159079f, 0.92810374f, 0f, 1f);
                    s.secondary = new Color(1f, 0.04843266f, 0f, 1f);
                    s.reveal = 1f; s.smoothness = 0.169f; s.rotation = 7.42f;
                    s.autoSpeed = -186.64f; s.autoRotation = true; s.smoothGradient = true; s.debug = true;
                    break;
                case BorderHighlightAppear.White:
                    s.main = Color.white; s.secondary = Color.white;
                    s.reveal = 1f; s.smoothness = 0.156f; s.startOffset = 168f; s.smoothGradient = true;
                    break;
                case BorderHighlightAppear.RotationYellow:
                    s.main = new Color(337.793945f, 155.708496f, 0f, 1f);
                    s.secondary = new Color(1f, 0.08363296f, 0f, 1f);
                    s.colorOffsets = new Color(0.91f, 1f, 0f, 0f);
                    s.noise = new Color(0.005f, 0.005f, 15f, 0f);
                    s.reveal = 0.326f; s.smoothness = 0.345f; s.rotation = -2.06f;
                    s.autoSpeed = -180f; s.autoRotation = true; s.smoothGradient = true;
                    break;
            }
            return s;
        }

        /// <summary>`Border Highlight Appear *` 的原型材质（按 kind 缓存；参数逐字段抄原版 JSON）。</summary>
        static Material BorderAppearProto(BorderHighlightAppear kind)
        {
            string key = "appear:" + kind;
            Material cached;
            if (_borderAppearProtos.TryGetValue(key, out cached) && cached != null) return cached;

            Shader sh; string src;
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(ShaderBorderHighlightAppear, out sh, out src) || sh == null)
            {
                Debug.LogWarning("[MenuFxMaterials] 取不到原版 shader `" + ShaderBorderHighlightAppear + "`"
                               + " ⇒ `Border Highlight Appear " + kind + "` 那几处保持原静态图（出声，不静默）");
                return null;
            }
            var s = BorderAppearSpecOf(kind);
            var m = new Material(sh);
            m.name = "Border Highlight Appear " + kind + " (runtime)";
            void C(string n, Color c) { if (m.HasProperty(n)) m.SetColor(n, c); }
            void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }

            C("_ColorMain", s.main);
            C("_ColorSecondary", s.secondary);
            C("_Color_offsets", s.colorOffsets);
            C("_Noise_Strength_X_Speed_Y_Scale_Z", s.noise);
            C("_Scale_X_Speed_Y", s.scaleXSpeed);
            C("_ScaleFromCenter", new Color(1f, 1f, 0f, 0f));
            F("_Reveal", s.reveal);
            F("_Smoothness", s.smoothness);
            F("_Rotation", s.rotation);
            F("_StartOffset", s.startOffset);
            F("_AUTOROTATION", s.autoRotation ? 1f : 0f);
            F("_AutoRotationSpeed", s.autoSpeed);
            F("_FLIPDIRECTION", 0f);
            F("_SMOOTHGRADIENT", s.smoothGradient ? 1f : 0f);
            F("_DEBUG", s.debug ? 1f : 0f);
            F("_QueueControl", 0f);
            F("_QueueOffset", 0f);
            // 关键字 = 原版 `m_ValidKeywords`（逐份核过：只有这 3 个可能开）
            if (s.autoRotation) m.EnableKeyword("_AUTOROTATION");
            if (s.smoothGradient) m.EnableKeyword("_SMOOTHGRADIENT");
            if (s.debug) m.EnableKeyword("_DEBUG");
            var noise = CardArt.MenuUi("UI_Dirt_And_Noise_skratches");
            if (noise != null && m.HasProperty("_Noise_R")) m.SetTexture("_Noise_R", noise);
            else if (noise == null)
                Debug.LogWarning("[MenuFxMaterials] 取不到 `UI_Dirt_And_Noise_skratches` ⇒ `Border Highlight Appear "
                               + kind + "` 的 `_Noise_R` 空着（出声，不静默）");

            _borderAppearProtos[key] = m;
            return m;
        }

        /// <summary>`B-08` #9 `Border Gacha Button Glow`（`Material_-7161444275765033636.json`，
        /// shader = `… UI Border Highlight Appear Pulsating Scale`）。consumer = `Gacha Tab` 的
        /// `Price Display Button/Button Higlight border`。⚠️ 原版**没有** `UIBorderGlow`（`Image` + 材质自走 `_Time`）
        /// ⇒ 静态挂上即可，⛔ 别再补一个「hover 就播」的触发。</summary>
        public static void ApplyBorderGachaButtonGlow(ImageQuad q) { ApplyProto(q, GachaGlowProto()); }

        /// <inheritdoc cref="ApplyBorderGachaButtonGlow(ImageQuad)"/>
        public static void ApplyBorderGachaButtonGlowAll(GameObject go)
        {
            if (go == null) return;
            var proto = GachaGlowProto();
            if (proto == null) return;
            var quads = go.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++) ApplyProto(quads[i], proto);
        }

        static Material _gachaGlowProto;
        static Material GachaGlowProto()
        {
            if (_gachaGlowProto != null) return _gachaGlowProto;
            Shader sh; string src;
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(ShaderBorderRotatingCampaign, out sh, out src) || sh == null)
            {
                Debug.LogWarning("[MenuFxMaterials] 取不到原版 shader `" + ShaderBorderRotatingCampaign + "`"
                               + " ⇒ `Border Gacha Button Glow` 保持静态图（出声，不静默）");
                return null;
            }
            var m = new Material(sh);
            m.name = "Border Gacha Button Glow (runtime)";
            void C(string n, Color c) { if (m.HasProperty(n)) m.SetColor(n, c); }
            void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
            C("_ColorMain",      new Color(4.24801731f, 0.58109683f, 1.65621519f, 1f));
            C("_ColorSecondary", new Color(1f, 0.02358490f, 0.12372980f, 1f));
            C("_Color_offsets",  new Color(0.8f, 1f, 0f, 0f));
            C("_Noise_Strength_X_Speed_Y_Scale_Z", new Color(0.02f, 0.01f, 8.17f, 0f));
            C("_Scale_X_Speed_Y", new Color(0.8f, 3f, 0f, 0f));
            F("_Reveal", 1f);
            F("_Smoothness", 0.169f);
            F("_Rotation", 7.42f);
            F("_StartOffset", 0f);
            F("_AUTOROTATION", 1f);
            F("_AutoRotationSpeed", -186.64f);
            F("_FLIPDIRECTION", 0f);
            F("_DEBUG", 1f);
            F("_QueueControl", 0f);
            F("_QueueOffset", 0f);
            m.EnableKeyword("_AUTOROTATION");
            m.EnableKeyword("_DEBUG");
            var noise = CardArt.MenuUi("UI_Dirt_And_Noise_skratches");
            if (noise != null && m.HasProperty("_Noise_R")) m.SetTexture("_Noise_R", noise);
            _gachaGlowProto = m;
            return m;
        }

        // ── #13 `Border Highlight Pulse Blue`（原版 shader 不在随包 bundle 里 ⇒ 走「照 Ruri 重建」那条路）──────
        /// <summary>我们重建的 `Everguild/UI/UI Border Highlight Pulse Alpha`。
        /// 判据 = `d:/2/tools/ruri_out_1020_allshaders/…/Everguild_UI_UI Border Highlight Pulse Alpha/
        /// Sub0_Pass0_{Vertex_b1,Fragment_b7}.hlsl`（逐指令）；文件 = `WarpforgeVFX/Shaders/WFUIBorderPulseAlpha.shader`。</summary>
        public const string ShaderBorderHighlightPulseAlpha = "WarpforgeVFX/UI/UI Border Highlight Pulse Alpha";

        static Material _borderPulseAlphaProto;

        /// <summary>`B-08` #13 把 `Border Highlight Pulse Blue`（`Material_1603963957059948543.json`）
        /// 挂到一张 quad 上。consumer = 战令 `Claim Button/Highlight button claim`。
        /// <para>⚠️ shader **自走 `_Time`**（4 段脉冲）⇒ 调用方**不要**补间 `_Reveal`。</para></summary>
        public static void ApplyBorderHighlightPulseBlue(ImageQuad q) { ApplyProto(q, BorderPulseAlphaProto()); }

        /// <inheritdoc cref="ApplyBorderHighlightPulseBlue(ImageQuad)"/>
        public static void ApplyBorderHighlightPulseBlueAll(GameObject go)
        {
            if (go == null) return;
            var proto = BorderPulseAlphaProto();
            if (proto == null) return;
            var quads = go.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++) ApplyProto(quads[i], proto);
        }

        static Material BorderPulseAlphaProto()
        {
            if (_borderPulseAlphaProto != null) return _borderPulseAlphaProto;

            // 先 `Shader.Find`（我们重建那份）；找不到再问一遍「原版名」（万一以后把原件打进随包 bundle）
            Shader sh = Shader.Find(ShaderBorderHighlightPulseAlpha);
            if (sh == null)
            {
                string src;
                WarpforgeVFX.WarpforgeShaderMap.TryResolve("Everguild/UI/UI Border Highlight Pulse Alpha", out sh, out src);
            }
            if (sh == null)
            {
                Debug.LogWarning("[MenuFxMaterials] 取不到 shader `" + ShaderBorderHighlightPulseAlpha + "`"
                               + "（`WarpforgeVFX/Shaders/WFUIBorderPulseAlpha.shader`）⇒ 战令的领取钮高亮保持静态图"
                               + "（出声，不静默）");
                return null;
            }
            var m = new Material(sh);
            m.name = "Border Highlight Pulse Blue (runtime)";
            void C(string n, Color c) { if (m.HasProperty(n)) m.SetColor(n, c); }
            void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
            // 逐值抄 `Material_1603963957059948543.json` 的 `m_SavedProperties`
            C("_ColorMain",      new Color(0f, 112.166992f, 337.793915f, 1f));   // HDR 过曝，⛔ 别夹到 1
            C("_ColorSecondary", new Color(0f, 0.30313706f, 1f, 1f));
            C("_Color_offsets",  new Color(0.91f, 1f, 0f, 0f));
            C("_Noise_Strength_X_Speed_Y_Scale_Z", new Color(0.007f, 0.05f, 15f, 0f));
            C("_ScaleFromCenter", new Color(1f, 1f, 0f, 0f));
            C("_TimesParameters", new Color(0.5f, 1f, 0.5f, 0f));
            F("_AnimationSpeed", 1f);
            F("_Reveal", 0.254f);
            F("_Smoothness", 0.345f);
            F("_Rotation", -2.06f);
            F("_NOISE_CHANNEL", 0f);        // = `_NOISE_CHANNEL_R`（原版 `m_ValidKeywords`）
            F("_QueueControl", 0f);
            F("_QueueOffset", 0f);
            m.EnableKeyword("_NOISE_CHANNEL_R");
            var noise = CardArt.MenuUi("UI_Dirt_And_Noise_skratches");
            if (noise != null && m.HasProperty("_Noise")) m.SetTexture("_Noise", noise);
            else if (noise == null)
                Debug.LogWarning("[MenuFxMaterials] 取不到 `UI_Dirt_And_Noise_skratches` ⇒ "
                               + "`Border Highlight Pulse Blue` 的 `_Noise` 空着（出声，不静默）");
            _borderPulseAlphaProto = m;
            return m;
        }

        // ── 供 `Collect Highlight Effect` 那一族用（原版 `UIBorderGlow.DoAnimation` 的等价物）──────
        /// <summary>把节点下**每一块** `ImageQuad` 的材质 `_Reveal` 从 **−0.5** 推到 **1**（线性，时长 <paramref name="duration"/>）。
        /// <para>判据 = 原版 `UIBorderGlow.DoAnimation`（`SetFloat(_Reveal,-0.5f)` → `DOFloat(…,1f,animTime)`，`Ease.Linear`）
        /// 与 `Play()` 的两句。</para>
        /// <para>🔴 **为什么不用 `Shell/UIBorderGlow.cs`**：原版 `Border Highlight Appear *` 与
        /// `… Pulsating Scale` 这两支 shader 的**属性表里没有 `_Color`**（本机 `m_PropInfo.m_Props` 实读），
        /// 而 `UIBorderGlow.ChangeGraphicAlpha` 走的是 `ImageQuad.SetTint` → `sharedMaterial.color`，
        /// 那会**每次播都打一条** `Material doesn't have a color property '_Color'`；而且我们的
        /// `Collect Highlight Effect` 是**九宫多块**（原版是一颗 Image），组件挂在节点上根本找不到渲染件。
        /// 🔶 自己设计（非原版）：用协程替 `DOTween.DOFloat`、把「一颗 Image」铺成「九宫各块各写一份」·
        /// 为什么必须自己定：原版组件的两条前提（有 `_Color`、单件 Graphic）在我们的载体上都不成立 ·
        /// 调度台 2026-11-01（`B112W_B08`）指派。</para></summary>
        public static System.Collections.IEnumerator RevealSweep(Transform node, float duration, float delay = 0f)
        {
            if (node == null) yield break;
            var quads = node.GetComponentsInChildren<ImageQuad>(true);
            var mats = new List<Material>(quads.Length);
            int id = Shader.PropertyToID("_Reveal");
            for (int i = 0; i < quads.Length; i++)
            {
                var mr = quads[i] != null ? quads[i].GetComponent<MeshRenderer>() : null;
                var mat = mr != null ? mr.sharedMaterial : null;
                if (mat != null && mat.HasProperty(id)) mats.Add(mat);
            }
            if (mats.Count == 0)
            {
                Debug.LogWarning("[MenuFxMaterials] `RevealSweep`：`" + node.name
                               + "` 下没有任何带 `_Reveal` 的材质 ⇒ 这次高亮不会动（出声，不静默）");
                yield break;
            }
            const float from = -0.5f, to = 1f;
            for (int i = 0; i < mats.Count; i++) if (mats[i] != null) mats[i].SetFloat(id, from);
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f, dur = Mathf.Max(1e-4f, duration);
            while (t < dur)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                for (int i = 0; i < mats.Count; i++) if (mats[i] != null) mats[i].SetFloat(id, v);
                yield return null;
            }
            for (int i = 0; i < mats.Count; i++) if (mats[i] != null) mats[i].SetFloat(id, to);
        }
    }
}
