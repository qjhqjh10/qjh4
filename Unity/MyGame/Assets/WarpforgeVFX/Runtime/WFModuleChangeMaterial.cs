// WFModuleChangeMaterial.cs — 原版 `AnimFXModuleChangeMaterial` 的对应物
//   **3 实例 / 3 效果**：`AmbushEffect` · `StealthEffect` · `VanguardIdleEffect`
//
// 语义出处（**逐方法体**，不是按名字猜）
// ----------------------------------------------------------------
//   · `资料/AnimFX_18类方法体_块1.md` §8（字段表 + 6 个方法体 + 嵌套协程那 1 个）
//   · 方法体原件（**逐行读过**）：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`
//     `AnimFXModuleChangeMaterial__{Initialize,Exit,OnDestroy,ToggleMaterial,WaitForFinishMaterialChange}.c`
//     + `d:/2/tools/decomp_animfx_nested/AnimFXModuleChangeMaterial.WaitForFinishMaterialChange_d__16__MoveNext.c`
//
// 照方法体还原了什么
// ----------------------------------------------------------------
//   · `Initialize`：`if (actionStart == Initialize(0)) ToggleMaterial(true)` —— **这一类自己读
//     `actionStart`**（`__Initialize.c:7`），基类和 player 都不看它
//   · `Exit`：`if (actionStart == Exit(5)) ToggleMaterial(true)` **否则** `if (toggleMaterialOffOnExit)
//     ToggleMaterial(false)`（注意 Exit 那支是**开**，原版如此）
//   · `OnDestroy`：`if (forceRecoverMaterialOnDestroy && actingCard != null)` →
//     `actingCard.CardUI.RestoreOriginalMaterial()`
//   · `ToggleMaterial(on)` 的两条材质路：
//     · `isCardMaterial` → `materialInstance = actingCard.CardUI.SetCardMaterial(material,
//       initializeWithCardImage)`；**只有这条路**才处理 `changeTexture`
//       （`materialInstance.SetTexture(textureMaterialProperty, actingCard.rawCard.cardSprite.texture)`，
//       `__ToggleMaterial.c:154-163` 在 else 分支**内部**）
//     · 否则 `useCustomRenderers` → `materialInstance = new Material(material)`，
//       **material 为空时退到 `customRenderers[0].GetSharedMaterial()`**（`:120-134`），
//       然后 `foreach (r in customRenderers) r.SetMaterial(materialInstance)`
//   · **整条的开头那道闸**（`:36-37`）：`material 存在 || useCustomRenderers` 才往下走 ——
//     两者都不满足时**连「关」也不做**（不恢复材质）。照抄。
//   · **「关」分支**：`if (!isCardMaterial && useCustomRenderers) return;`（这种组合**不恢复**，
//     `:68-70` / `:102-103`），否则 `RestoreOriginalMaterial()`
//   · `.ctor` 的默认值：`isCardMaterial = true` · `initializeWithCardImage = true` ·
//     `toggleMaterialOffOnExit = true` · **`fade` 没赋值 = false**（块1 §8 末行）
//   · 数据（3 例逐条核过）：`AmbushEffect`(isCard=1, fade=1, changeTexture=1, "_TargetImage",
//     forceRecover=1) · `StealthEffect`(isCard=1, fade=1, toggleOff=1) ·
//     `VanguardIdleEffect`(**actionStart=15 Manual**, isCard=0, useCustomRenderers=1,
//     customRenderers[0]=`@node:MeshRenderer:…Vanguard Frame Animated VAT#1`, toggleOff=0)
//
// 🔴 我们自己定的（**原版不是这样**）
// ----------------------------------------------------------------
//   1. **卡材质那一层从哪来**：原版是 `actingCard.CardUI.SetCardMaterial(...)` /
//      `BattleCardUI.RestoreOriginalMaterial()`。**我们这边卡面材质在 `CardPresentation`
//      （`CardView`/`CardFeel`）里，VFX 层不认识它** ⇒ 三个静态回调（挂法一行）：
//        `WFModuleChangeMaterial.SetCardMaterial` / `.RestoreOriginalMaterial` / `.CardTexture`
//      没挂 `SetCardMaterial` = `isCardMaterial` 那条路**做不了** → **LogWarning**（不静默），
//      但 `useCustomRenderers` 那条路是**纯本地**的，照做。
//      ✅ **2026-10-01 起这三个回调已经挂上了**（`BattleDriver.HookAnimFxCards`）。
//      对应物在 `CardView` 里，**判据只写在那儿一份**（原版
//      `d:/2/tools/decomp_full/BattleCardUI__SetCardMaterial.c` / `…__RestoreOriginalMaterial.c`
//      **逐行读过**：全在**卡 3D 体那一个 Renderer** 上、原版 `+0x180`，我们 = `_body3D`）：
//        · `CardView.SetCardMaterial(m, initializeWithCardImage)` —— 返回换上去的那份（原版返回值）
//        · `CardView.RestoreOriginalMaterial()` —— 回建体时那份（原版 `+0x2c8` 那份的对应物）
//        · `CardView.CardImageTexture` —— 原版 `rawCard.cardSprite.texture`
//      `BattleDriver` 那边只做「`Transform` → `CardView`」一跳，**不重复实现**。
//   2. **`material` 资产**（`@asset:Material:<名字>`）：原版按资产找。我们这边要挂
//      `MaterialResolver`，或者在 Inspector 里直接给 `material`。
//      ~~⚠️ **这 3 张材质工程里没有**（2026-09-18 查过 `WarpforgeVFX/Materials/`：只有
//      `Vanguard_Frame VAT`，**没有** ……）。原因：导出器只导**渲染器上**的材质
//      （`WarpforgeArena1/Editor/EffectExporter.cs` 的 `GetComponentsInChildren<Renderer>`），
//      只被模块字段引用的材质不会被带上。⇒ 解析不到时**按原版「material 为空」的兜底走**
//      （自定义渲染器那条退到 `customRenderers[0].sharedMaterial`）并出声。~~
//
//      🔴 **2026-10-01 更正：这条已经修好了，原文那两句（「工程里没有」+「解析不到就兜底」）
//         说的是修之前的状态。** 三张材质**本来就在 bundle 里**（不是「本地没有」）——
//      `Vanguard_Frame VAT Dissolve` 在 battleprefabs 包 · `Card 3d Dissolve Blend Image Ambush` /
//      `Card 3d Stealth` 在 battlesharedresources 包。**真根因**是导出器只遍历渲染器。
//      **两条修法（都做了）**：
//        ① `EffectExporter.Export()` 新增一个 pass —— 按 `数据/游戏数据/module_materials.tsv`
//           （由 `工具/gen_animfx_modules.py` 生成）把**只被模块字段引用**的材质也 `DefIndex` 进
//           该效果的 binder（`WFMatDef`），并 `ImportMaterial` 落一张 `.mat` 占位。
//        ② 本文件的 `ResolveAssets()` 新增一条路：**从自己那个播放器的 `WarpforgeEffectBinder`
//           里按名字取**（`WarpforgeEffectBinder.BuildForProbe(WFMatDef)`）。
//      **三张材质的逐字段实读（贴图槽 / shader / PathID）+ 三个 shader 在不在我们包里的判据**
//      → `资料/普查产出_1001/资产导入路三件_侦察.md` §①。
//      ⚠️ **仍然成立的是卡材质那一层**：`isCardMaterial = true` 的两个（`AmbushEffect` /
//      `StealthEffect`）要走上面的静态回调，没挂照样只打警告（见下一条「我们自己定的」1）。
//   3. `Renderer.SetMaterial`（Unity 6 的 API，不实例化）→ 我们用 `renderer.sharedMaterial = `。
//      **等价**：我们那个 `new Material(...)` 本来就是这条模块私有的唯一实例。
//   4. **`fade` 这条我们不做**（见下「没还原」②）。
//
// 🔴 没还原的
// ----------------------------------------------------------------
//   ① ✅ **2026-10-14（`A1862`）就地订正（铁律 5）：原来这一段写的是错的，而且导出了一条错的待办。**
//      原文：「`materialAnimations`（`DynamicList<MaterialTweenBase>`）：这些**材质补间资产没导出来**
//      （`数据/游戏数据/tween/` 里 74 份全是 `UnitTweenSO`，0 份 `MaterialTween`）。**3 例的
//      `materialAnimations` 都是空的** ⇒ 空列表下原版的语义就是「不播补间」……」
//      🔴 **「3 例都是空的」不成立** —— **三条 `MaterialTween*` 都在**，只是**内联**在
//      `materialAnimations.list[0].rid → references.RefIds[].data` 里（**不在** `数据/游戏数据/tween/`
//      那 74 份 `UnitTweenSO` 目录里）。当年「找不到素材」是**按那份目录搜出来的**
//      ——同「翻过一个镜像目录就写『本地没有』」那个形状（CLAUDE.md 铁律 5 的反例）。
//      **原件实读**（`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/`，
//      类名/字段名逐条对过 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/MaterialTween*.cs`）：
//
//      | 效果（MB 文件） | 补间 → 属性 | duration | delay | initializeToValue | start → final |
//      |---|---|---|---|---|---|
//      | `AmbushEffect`（`…_6849782588473963486.json`） | `MaterialTweenFloat` `_Blend` | **1.0** | **0.35** | 1 | **0 → 1** |
//      | `StealthEffect`（`…_3932969127104665692.json`） | `MaterialTweenColor` `_Color` | **0.4** | 0 | 1 | (0,0,0,1) → **色常量一组** (0.1255,0.1765,0.2627,1) |
//      | 同上 | `MaterialTweenFloat` `_InteriorAlphaSpread` | **0.4** | 0 | 1 | **2.75 → 1.39** |
//      | `VanguardIdleEffect`（`…_-8300107518467925907.json`） | `MaterialTweenFloat` `_DissolveAmount` | **1.0** | 0 | 1 | **0 → 1** |
//
//      🔴 **三条各不相同 ⇒ 别拿一个值套全部**（铁律 5·c）：`AmbushEffect` 那条**带 0.35 的 delay**，
//      另两件 delay = 0；`VanguardIdleEffect` 走的是 `useCustomRenderers` 那条路（材质不是卡面的）。
//
//   ② ✅ **2026-10-14（`A1862`）已实现**（原来写的是「**我们不做**」。）语义照原版方法体逐句来：
//      · `MaterialTweenBase.DoTween(Renderer)` / 子类 `DoTween(Material, bool reverse)`（签名桩实读）；
//        子类方法体（`MaterialTween{Float,Color,Vector}__DoTween.c`）**同一套骨架**：
//          `from = reverse ? finalValue : startValue`（另一半就是 `to`）
//          → `if (initializeToValue) material.Set{...}(prop, from)`   ← **先snap到起点**
//          → `DO{T}(material, to, prop, duration)` + `SetDelay(delay)`。
//      · 模块 `ToggleMaterial` 的调用点（`__ToggleMaterial.c:90-91` = 「开」、`:215-216` = 「关」）：
//        **开 ⇒ `reverse = true`**、**关 ⇒ `reverse = false`**（⚠️ 顺序如此，别按直觉倒过来）。
//      · 原版那两条**没有 `SetEase`** ⇒ 吃 DOTween 的 `defaultEaseType`
//        （本工程 `Assets/Resources/DOTweenSettings.asset` = **6 = `Ease.OutQuad`**，先例 →
//         `CardPresentation/Battle/ErrorMessageBanner.cs:96-97`）。
//      · **「关」那条路**（`:38-99`）：`StopAllCoroutines()` + `DOTween.Kill(materialInstance)`
//        → 逐条 `DoTween(mat, reverse:false)` → **等「最长 duration」**（⚠️ **不含 delay** ——
//        取的是 `MaterialTweenBase.get_Duration` = 字段 `0x18`）→ `RestoreOriginalMaterial()`。
//      · 我们**不用 DOTween 也不用协程**，改在 `ModuleTick(dt)` 里手推同样的曲线
//        （理由同本仓库别的模块：**批处理下没有帧循环**，协程在那条路上永远不推进 = 静默失效）。
//      · ⚠️ **`initializeWithCardImage` 也是那三条的实读值**（`AmbushEffect` = **0**，
//        `StealthEffect` = **1**，`VanguardIdleEffect` = **0**）—— 见 `Configure()`。
//   ③ `actionStart == Manual(15)` 那 1 例（`VanguardIdleEffect`）：原版**谁在什么时候调
//      `ToggleMaterial` 查不到**（块1 §8 没记调用点）⇒ 我们把它暴露成公开方法
//      `ToggleMaterial(bool)`，等接线；**它自己什么都不会做**（Initialize/Exit 都不满足条件）。
//      ⚠️ 这一条的**「开」支仍然到得了** `fade` 那条路（`ToggleMaterial(true)` 会播 `_DissolveAmount`
//      0 → 1）⇒ 接上调用点之后它是活的。
//
//   ⚠️ **数据这一跳（2026-10-14 `A1882`）**：`materialAnimations` 的内容**现在已经在数据链上了**
//      —— `工具/dump_animfx.py` 的 `_depth_limit` 放行了 `references` 子树（那三条**内联**在
//      `materialAnimations.list[0].rid → references.RefIds[].data` 里），
//      `工具/gen_animfx_modules.py` 的 `flatten_material_animations()` 又把它拍平成了
//      `materialAnimations[i].{type,materialProperty,duration,delay,initializeToValue,startValue…}`。
//      **实测读数**（重跑两个工具后现读 `数据/游戏数据/animfx_modules.json`）：
//        `AmbushEffect.materialAnimationCount = 1`（`_Blend` 1.0 / delay 0.35 · 0→1）·
//        `StealthEffect = 2`（`_Color` 0.4 / 0 · `_InteriorAlphaSpread` 0.4 / 0）·
//        `VanguardIdleEffect = 1`（`_DissolveAmount` 1.0 / 0 · 0→1）。
//      🔴 **还剩第 ③ 步：Unity `EffectLibraryBuilder.Run`** —— 效果库是从 `animfx_modules.json`
//         建的（`Assets/Resources/WarpforgeVFX/`，已被 `.gitignore` 挡在仓库外）⇒ **不重跑它，
//         上面那些参数进不了库、`TweenCount` 仍是 0**。跑完的验法 = W12 报告里那段断言原文
//         （`Check(mod.TweenCount == 1, …)` / `_Blend` 1.0 / delay 0.35）。
//      ⚠️ **原来的记录（保留作对照）**：「`references.RefIds[].data` 被写成 `<深>`，`gen_animfx_modules.py`
//         又只抄了 `materialAnimations.list[n].rid`（一个 long）⇒ 库里没有那三条的参数」——
//         **这条 2026-10-14 起不成立**（`dump_animfx.py` 的 `REFERENCES_MAX_DEPTH` + 生成器那趟都做了）。
//         当年**还漏写了一处**：原样拍平出来的是 `materialAnimations.list[0].rid`，而
//         `WFModuleDef.CountList("materialAnimations")` 数的是 `materialAnimations[0]` 这种**前缀键**
//         ⇒ **`materialAnimationCount` 恒为 0**（不只是「读不到参数」）—— 这条 2026-10-14 现核订正。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleChangeMaterial")]
    public class WFModuleChangeMaterial : WFEffectModule
    {
        // ---- 原版字段（块1 §8 的字段表，偏移照 dump.cs `:48232` 核过）----
        [Tooltip("原版 `material`：@asset:Material:<名字>。手挂时可直接在下面给 `material`")]
        public string materialName = "";

        [Tooltip("手挂用：直接给材质。留空 = 问 MaterialResolver(materialName)")]
        public Material material;

        [Tooltip("原版 `isCardMaterial`（**默认 true**）：换的是卡面的材质（走回调），否则走下面的自定义渲染器")]
        public bool isCardMaterial = true;

        [Tooltip("原版 `fade`（**默认 false**，`.ctor` 没赋值）：见文件头「没还原」② —— 我们不做补间")]
        public bool fade;

        [Tooltip("原版 `initializeWithCardImage`（**默认 true**）：传给 SetCardMaterial 的第二个参数")]
        public bool initializeWithCardImage = true;

        [Tooltip("原版 `toggleMaterialOffOnExit`（**默认 true**）：Exit 时把材质换回去")]
        public bool toggleMaterialOffOnExit = true;

        [Tooltip("原版 `forceRecoverMaterialOnDestroy`：销毁时无条件恢复卡的原材质")]
        public bool forceRecoverMaterialOnDestroy;

        [Tooltip("原版 `changeTexture`：把卡图贴图设到 `textureMaterialProperty`（**只有 isCardMaterial 那条路**）")]
        public bool changeTexture;

        [Tooltip("原版 `textureMaterialProperty`（数据里是 `_TargetImage`）")]
        public string textureMaterialProperty = "";

        [Tooltip("原版 `useCustomRenderers`：改下面这批渲染器自己的材质")]
        public bool useCustomRenderers;

        [Tooltip("原版 `customRenderers:Renderer[]`（@node:MeshRenderer:…）")]
        public string[] customRenderers = new string[0];

        [Tooltip("手挂用：直接拖渲染器（否则按上面的引用值解析）")]
        public Renderer[] rendererTargets = new Renderer[0];

        /// <summary>数据里 `materialAnimations` 有几条（原版 `DynamicList` 的条数）。
        /// ⚠️ **条数 ≠ 参数**：参数要 `tweens` 真读到才有（见文件头「数据这一跳还没通」）。</summary>
        [HideInInspector] public int materialAnimationCount;

        /// <summary>原版 `materialAnimations` 里的补间（`MaterialTweenBase` 的三个子类）。
        /// ⚠️ 数据侧**现在还没接上**（见文件头「数据这一跳还没通」）；手挂时可以直接在这儿填。
        /// **键名约定**（`gen_animfx_modules.py` 拍平后要用的形状，⛔ 两边必须一致）：
        ///   `materialAnimations[i].type`（`MaterialTweenFloat`/`Color`/`Vector`）·
        ///   `.materialProperty` · `.duration` · `.delay` · `.initializeToValue` ·
        ///   `.startValue` / `.finalValue`（float）｜ `.startValue.r/.g/.b/.a`（color，两个都是）｜
        ///   `.startValue.x/.y/.z/.w`（vector，两个都是）。</summary>
        [Serializable]
        public class WFMatTween
        {
            [Tooltip("原版类名：`MaterialTweenFloat` / `MaterialTweenColor` / `MaterialTweenVector`")]
            public string type = "MaterialTweenFloat";
            [Tooltip("原版 `materialProperty`（实读：`_Blend` / `_Color` / `_InteriorAlphaSpread` / `_DissolveAmount`）")]
            public string materialProperty = "";
            [Tooltip("原版 `duration`（`.ctor` 默认 0.35）")] public float duration = 0.35f;
            [Tooltip("原版 `delay`")] public float delay;
            [Tooltip("原版 `initializeToValue`（默认 true）：开播前先把属性 snap 到起点")]
            public bool initializeToValue = true;
            public float startFloat, finalFloat;
            public Color startColor = Color.white, finalColor = Color.white;
            public Vector4 startVector, finalVector;
        }

        [Tooltip("原版 `materialAnimations`（见 WFMatTween 的注释）。空 = 按原版「空列表」的语义走（不播）")]
        public WFMatTween[] tweens = new WFMatTween[0];

        // ---- 运行时（原版 0x68）----
        [NonSerialized] public Material materialInstance;

        // ---- 🔑 下游钩子：卡材质那一层（原版 `actingCard.CardUI.…`）----
        /// <summary>`(卡, 材质, initializeWithCardImage)` → **实际用的材质实例**
        /// （原版 `BattleCardUI.SetCardMaterial` 的返回值）。没挂 = `isCardMaterial` 那条路做不了。</summary>
        public static Func<Transform, Material, bool, Material> SetCardMaterial;
        /// <summary>`(卡)` → 恢复卡的原材质（原版 `BattleCardUI.RestoreOriginalMaterial`）。</summary>
        public static Action<Transform> RestoreOriginalMaterial;
        /// <summary>`(卡)` → 卡图贴图（原版 `actingCard.rawCard.cardSprite.texture`），`changeTexture` 用。</summary>
        public static Func<Transform, Texture> CardTexture;
        /// <summary>`@asset:Material:&lt;名字>` → 材质（见文件头「我们自己定的」2）。</summary>
        public static Func<string, Material> MaterialResolver;

        // ---- 诊断 ----
        public static int MissingMaterial, MissingCardLayer, MissingCardContext, SkippedTweens;
        public static void ResetDiagnostics()
        {
            MissingMaterial = 0; MissingCardLayer = 0; MissingCardContext = 0; SkippedTweens = 0;
            _warnedMaterial = _warnedCardLayer = _warnedCardContext = _warnedTweens = false;
        }
        static bool _warnedMaterial, _warnedCardLayer, _warnedCardContext, _warnedTweens;

        bool _recovered;
        bool _warnedNoCardThis;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;

            materialName = def.GetString("material");
            // `material` 的值是 `@asset:Material:Card 3d Stealth` —— 名字在最后一段
            string kind, type, rest;
            WFModuleDef.SplitRef(materialName, out kind, out type, out rest);
            if (kind == "asset") materialName = rest;

            isCardMaterial = def.GetBool("isCardMaterial", true);
            fade = def.GetBool("fade");
            initializeWithCardImage = def.GetBool("initializeWithCardImage", true);
            toggleMaterialOffOnExit = def.GetBool("toggleMaterialOffOnExit", true);
            forceRecoverMaterialOnDestroy = def.GetBool("forceRecoverMaterialOnDestroy");
            changeTexture = def.GetBool("changeTexture");
            textureMaterialProperty = def.GetString("textureMaterialProperty");
            useCustomRenderers = def.GetBool("useCustomRenderers");
            customRenderers = def.GetList("customRenderers").ToArray();
            materialAnimationCount = def.GetList("materialAnimations").Count;
            tweens = ReadTweens(def, materialAnimationCount);
        }

        /// <summary>把拍平后的 `materialAnimations[i].*` 读成 <see cref="WFMatTween"/>。
        /// **键名与 `WFMatTween` 的注释逐字一致**（那一条是两边唯一的契约）。
        /// ⚠️ 读不到参数时返回**空数组**（= 原版「空列表」的语义），由 `WarnTweens()` 出声 —— ⛔ 不猜值。</summary>
        static WFMatTween[] ReadTweens(WFModuleDef def, int n)
        {
            if (def == null || n <= 0) return new WFMatTween[0];
            var outl = new System.Collections.Generic.List<WFMatTween>(n);
            for (int i = 0; i < n; i++)
            {
                string b = "materialAnimations[" + i + "]";
                // 参数一条都没拍平出来（数据链还没通）⇒ 这一条读不到，如实留空
                if (!def.Has(b + ".duration") && !def.Has(b + ".materialProperty")) continue;

                var tw = new WFMatTween();
                tw.type = def.GetString(b + ".type", "MaterialTweenFloat");
                tw.materialProperty = def.GetString(b + ".materialProperty");
                tw.duration = def.GetFloat(b + ".duration", 0.35f);
                tw.delay = def.GetFloat(b + ".delay");
                tw.initializeToValue = def.GetBool(b + ".initializeToValue", true);

                if (def.Has(b + ".startValue.r"))
                {
                    tw.startColor = new Color(def.GetFloat(b + ".startValue.r"), def.GetFloat(b + ".startValue.g"),
                                              def.GetFloat(b + ".startValue.b"), def.GetFloat(b + ".startValue.a", 1f));
                    tw.finalColor = new Color(def.GetFloat(b + ".finalValue.r"), def.GetFloat(b + ".finalValue.g"),
                                              def.GetFloat(b + ".finalValue.b"), def.GetFloat(b + ".finalValue.a", 1f));
                }
                else if (def.Has(b + ".startValue.x"))
                {
                    tw.startVector = new Vector4(def.GetFloat(b + ".startValue.x"), def.GetFloat(b + ".startValue.y"),
                                                 def.GetFloat(b + ".startValue.z"), def.GetFloat(b + ".startValue.w"));
                    tw.finalVector = new Vector4(def.GetFloat(b + ".finalValue.x"), def.GetFloat(b + ".finalValue.y"),
                                                 def.GetFloat(b + ".finalValue.z"), def.GetFloat(b + ".finalValue.w"));
                }
                else
                {
                    tw.startFloat = def.GetFloat(b + ".startValue");
                    tw.finalFloat = def.GetFloat(b + ".finalValue");
                }
                outl.Add(tw);
            }
            return outl.ToArray();
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            ResolveAssets();

            // 原版：**自己读 actionStart**，== Initialize(0) 才开
            if (actionStart == WFActionStart.Initialize) ToggleMaterial(true);
        }

        public override void Exit()
        {
            // 原版：== Exit(5) → **开**（照抄，不是笔误）；否则看 toggleMaterialOffOnExit
            if (actionStart == WFActionStart.Exit) { ToggleMaterial(true); return; }
            if (toggleMaterialOffOnExit) ToggleMaterial(false);
        }

        /// <summary>原版 `OnDestroy()`（`forceRecoverMaterialOnDestroy` 那条）。</summary>
        public override void DoDestroy() { RecoverOnDestroy(); }

        /// <summary>⚠️ 我们多挂了一处：**不经过 `DestroyNow` 的销毁**（切场景直接拆对象）不会广播
        /// `DoDestroy`，而原版的 `OnDestroy` 一定会响 ⇒ 这里补上，与 `DoDestroy` 共用一道去重闸。</summary>
        void OnDestroy() { RecoverOnDestroy(); }

        void RecoverOnDestroy()
        {
            if (_recovered) return;
            _recovered = true;
            // 原版靠「Unity 销毁协程」达到同样效果；我们显式停掉那两个泵
            // （`Kill()` 那条路不走 `Exit`，恢复计时可能还在飞）
            _tweenTime = -1f;
            _restoreAt = -1f;
            if (!forceRecoverMaterialOnDestroy) return;

            var card = ActingCard();
            if (card == null) return;                     // 原版：actingCard 为空就不做
            if (RestoreOriginalMaterial == null) { WarnNoCardLayer(); return; }
            RestoreOriginalMaterial(card);
        }

        // ---- 原版 `ToggleMaterial(bool on)` ----
        public void ToggleMaterial(bool on)
        {
            // 开头那道闸（`:36-37`）：`material 存在 || useCustomRenderers` —— 否则连「关」也不做
            if (material == null && !useCustomRenderers) return;

            if (on)
            {
                if (isCardMaterial) ToggleOnCardMaterial();
                else if (useCustomRenderers) ToggleOnCustomRenderers();

                // 「开」那一支的尾巴（`__ToggleMaterial.c:165-221`）：`fade` 时才播补间。
                // 原版在那里先 `DOTween.Kill(materialInstance)` + `StopAllCoroutines()`（把上一条打断），
                // 再逐条 `DoTween(materialInstance, **reverse: true**)`。
                if (fade)
                {
                    _restoreAt = -1f;                       // 别再恢复（上一次「关」的收尾作废）
                    if (materialInstance == null || tweens.Length == 0) WarnTweens();
                    else PlayTweens(reverse: true);
                }
            }
            else
            {
                // 原版：这种组合**不恢复**（自定义渲染器本来就是它自己的材质）
                if (!isCardMaterial && useCustomRenderers) return;

                // 「关」那一支的尾巴（`__ToggleMaterial.c:38-99`）：`fade` **且**已经换过材质时，
                // 逐条 `DoTween(mat, **reverse: false**)`，然后**等「最长 duration」**再恢复原材质。
                // 🔴 等的是 `MaterialTweenBase.get_Duration()` = 字段 `0x18` = **duration**
                //    （⚠️ **不含 delay**）；空列表 ⇒ 原版是 `WaitForSeconds(0)`（下一帧）恢复，
                //    我们**同步**恢复（老行为，如实标注：差一帧，三种情况都走这条）。
                if (fade && materialInstance != null && tweens.Length > 0)
                {
                    PlayTweens(reverse: false);
                    _restoreAt = LongestDuration();
                    return;                                  // 恢复交给 `ModuleTick`，到点再 `Restore()`
                }
                Restore();
            }
        }

        // ==================================================================
        //  材质补间（原版 `MaterialTweenBase` 三个子类 + DOTween）
        // ==================================================================

        float _tweenTime = -1f;      // <0 = 没在跑（原版是 DOTween 的 Tween 对象）
        bool _tweenReverse;
        float _restoreAt = -1f;      // >=0 = 倒数到 0 就 `Restore()`（原版那条 `WaitForFinishMaterialChange`）

        /// <summary>最长的一条 `duration`（原版 `MaterialTweenBase.get_Duration()`；⚠️ **不含 delay**）。</summary>
        float LongestDuration()
        {
            float m = 0f;
            for (int i = 0; i < tweens.Length; i++)
                if (tweens[i] != null && tweens[i].duration > m) m = tweens[i].duration;
            return m;
        }

        /// <summary>原版 `MaterialTweenXxx.DoTween(Material, bool reverse)`：**snap 起点 + 起播**。
        /// `reverse` = **开 → true**、**关 → false**（模块那两个调用点给的，⛔ 别按直觉倒过来）。</summary>
        void PlayTweens(bool reverse)
        {
            if (materialInstance == null) return;
            _tweenReverse = reverse;
            _tweenTime = 0f;
            for (int i = 0; i < tweens.Length; i++)
            {
                var tw = tweens[i];
                if (tw == null || string.IsNullOrEmpty(tw.materialProperty)) continue;
                // `if (initializeToValue) material.Set{...}(prop, from)` —— 三个子类的**第一句**
                if (tw.initializeToValue) ApplyValue(tw, EndValue(tw, FromIsStart));
            }
            ApplyTweens(0f);
        }

        /// <summary>`from` 那一端是不是 `startValue` —— **只有这一处判方向**。
        /// 原版子类里的三元是 `from = reverse ? finalValue : startValue`（`to` 取另一半）；
        /// **开 ⇒ `reverse = true`** ⇒ 开播时 snap 到的是 **`finalValue`**（⛔ 别按直觉倒过来，
        /// 判据 = `__ToggleMaterial.c:215-216` 与 `MaterialTweenFloat__DoTween.c` 那两句）。</summary>
        bool FromIsStart { get { return !_tweenReverse; } }

        /// <summary>某一条的一端：`start` = true 取 `startValue`，否则 `finalValue`。</summary>
        static object EndValue(WFMatTween tw, bool start)
        {
            if (tw.type == "MaterialTweenColor") return start ? (object)tw.startColor : tw.finalColor;
            if (tw.type == "MaterialTweenVector") return start ? (object)tw.startVector : tw.finalVector;
            return start ? (object)tw.startFloat : tw.finalFloat;
        }

        /// <summary>推进所有补间到 `elapsed` 秒（原版把这些交给 DOTween 的 `DOFloat/DOColor/DOVector`
        /// + `SetDelay`；我们手推 —— **批处理下没有帧循环**，DOTween 那条路在那条路上不推进）。</summary>
        void ApplyTweens(float elapsed)
        {
            if (materialInstance == null) return;
            for (int i = 0; i < tweens.Length; i++)
            {
                var tw = tweens[i];
                if (tw == null || string.IsNullOrEmpty(tw.materialProperty)) continue;
                float k = tw.duration > 1e-6f
                        ? Mathf.Clamp01((elapsed - tw.delay) / tw.duration)
                        : (elapsed >= tw.delay ? 1f : 0f);
                // ⚠️ `elapsed < delay` 时 `k` 被夹成 0 ⇒ 停在 snap 值上，正是原版
                //    「`DOFloat` 还没起步」的语义。
                ApplyValue(tw, LerpValue(tw, EaseOutQuad(k)));
            }
        }

        /// <summary>原版那三条**都没有 `SetEase`** ⇒ 吃 DOTween 的 `defaultEaseType`
        /// （本工程 `Assets/Resources/DOTweenSettings.asset` = **6 = `Ease.OutQuad`**；
        /// 同一条先例 → `CardPresentation/Battle/ErrorMessageBanner.cs:96-97`）。</summary>
        static float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }

        /// <summary>按 `reverse` 把某一条的起点 / 终点取出来，然后在两端之间插值 `k`。</summary>
        object LerpValue(WFMatTween tw, float k)
        {
            if (tw.type == "MaterialTweenColor")
                return Color.LerpUnclamped((Color)EndValue(tw, FromIsStart), (Color)EndValue(tw, !FromIsStart), k);
            if (tw.type == "MaterialTweenVector")
                return Vector4.LerpUnclamped((Vector4)EndValue(tw, FromIsStart), (Vector4)EndValue(tw, !FromIsStart), k);
            return Mathf.LerpUnclamped((float)EndValue(tw, FromIsStart), (float)EndValue(tw, !FromIsStart), k);
        }

        /// <summary>把一条补间的值写进材质（原版 `Material.SetFloat/SetColor/SetVector`）。
        /// ⚠️ 属性在不在**不判** —— 原版也不判（`Material.SetFloat` 对不存在的属性是**静默 no-op**）。</summary>
        void ApplyValue(WFMatTween tw, object v)
        {
            if (materialInstance == null || tw == null) return;
            if (v is Color) materialInstance.SetColor(tw.materialProperty, (Color)v);
            else if (v is Vector4) materialInstance.SetVector(tw.materialProperty, (Vector4)v);
            else if (v is float) materialInstance.SetFloat(tw.materialProperty, (float)v);
        }

        /// <summary>原版 `AnimFXModuleBase.Update` 各写各的；这里由控制器统一广播（见基类）。
        /// 「关」那条路的「等最长时长」也在这个泵上 —— 原版是 `WaitForSeconds` 协程。</summary>
        public override void ModuleTick(float dt)
        {
            if (_tweenTime >= 0f)
            {
                _tweenTime += dt;
                ApplyTweens(_tweenTime);
                if (_tweenTime >= LongestDuration()) _tweenTime = -1f;   // DOTween 播完自解
            }
            if (_restoreAt >= 0f)
            {
                _restoreAt -= dt;
                if (_restoreAt <= 0f) { _restoreAt = -1f; Restore(); }
            }
        }

        /// <summary>自检用：正在跑的补间到第几秒（< 0 = 没在跑）。</summary>
        public float TweenTime { get { return _tweenTime; } }
        /// <summary>自检用：还有多久恢复原材质（< 0 = 没有排恢复）。</summary>
        public float RestoreCountdown { get { return _restoreAt; } }
        /// <summary>自检用：现在读到了几条补间参数（数据链通了以后应等于 `materialAnimationCount`）。</summary>
        public int TweenCount { get { return tweens != null ? tweens.Length : 0; } }

        void ToggleOnCardMaterial()
        {
            var card = ActingCard();
            if (card == null) { WarnNoCardContext(); return; }
            if (SetCardMaterial == null) { WarnNoCardLayer(); return; }

            materialInstance = SetCardMaterial(card, material, initializeWithCardImage);

            // `changeTexture` **只有这条路**处理（`__ToggleMaterial.c:154-163`）
            if (!changeTexture) return;
            if (materialInstance == null) return;
            if (string.IsNullOrEmpty(textureMaterialProperty))
            {
                Debug.LogWarning($"[WarpforgeVFX] `changeTexture` 开着但 `textureMaterialProperty` 是空的"
                               + $"（效果 {EffectName}）—— 原版这时会拿空属性名去 SetTexture（无效）。");
                return;
            }
            if (CardTexture == null) { WarnNoCardLayer(); return; }
            var tex = CardTexture(card);
            if (tex == null)
            {
                Debug.LogWarning($"[WarpforgeVFX] `changeTexture` 要卡的贴图，但 `CardTexture` 返回了 null"
                               + $"（效果 {EffectName}）—— 跳过换贴图。");
                return;
            }
            materialInstance.SetTexture(textureMaterialProperty, tex);
        }

        void ToggleOnCustomRenderers()
        {
            // material 为空 → 退到 customRenderers[0].GetSharedMaterial()（原版 `:120-134`）
            Material mat = material;
            if (mat == null)
            {
                var rs = Renderers();
                if (rs.Length == 0 || rs[0] == null)
                {
                    Debug.LogError("[WarpforgeVFX] `useCustomRenderers` 开着，但 `material` 为空、"
                                 + $"`customRenderers[0]` 也拿不到（效果 {EffectName}）—— 换材质这条做不了。");
                    return;
                }
                mat = rs[0].sharedMaterial;
            }
            materialInstance = mat != null ? new Material(mat) { name = mat.name + " (module)" } : null;

            var arr = Renderers();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null) continue;
                // 原版是 `Renderer.SetMaterial`（不实例化）；`sharedMaterial =` 与我们那个
                // 「模块私有」的 new Material 等价（见文件头「我们自己定的」3）
                arr[i].sharedMaterial = materialInstance;
            }
        }

        void Restore()
        {
            var card = ActingCard();
            if (card == null) { WarnNoCardContext(); return; }
            if (RestoreOriginalMaterial == null) { WarnNoCardLayer(); return; }
            RestoreOriginalMaterial(card);
            materialInstance = null;
        }

        // ---- 小工具 ----
        void ResolveAssets()
        {
            if (material == null && !string.IsNullOrEmpty(materialName))
            {
                if (MaterialResolver != null) material = MaterialResolver(materialName);

                // 🆕 2026-10-01：**先问「自己这个效果」的 binder**，再谈「找不到」。
                //
                // 判据/做法：原版这个字段是组件上的 `AssetReferenceTyped<Material>` —— 材质**属于该效果**，
                // 不是全局按名字查表。所以我们这一侧同构的做法 = 从**本模块所在的那个播放器**身上
                // 的 `WarpforgeEffectBinder` 里按名字取（`EffectExporter.Export()` 2026-10-01 起会把
                // 只被模块字段引用的材质一并收进那个 binder，判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①）。
                // ⚠️ `Initialize` 里调本方法时 `Controller` **已经设好了**（`base.Initialize` 先跑）。
                if (material == null && Controller != null)
                {
                    var binder = Controller.GetComponent<WarpforgeEffectBinder>();
                    if (binder != null && binder.materials != null)
                        for (int i = 0; i < binder.materials.Length; i++)
                        {
                            var d = binder.materials[i];
                            if (d == null || d.name != materialName) continue;
                            material = WarpforgeEffectBinder.BuildForProbe(d);
                            break;
                        }
                }

                if (material == null)
                {
                    MissingMaterial++;
                    if (!_warnedMaterial)
                    {
                        _warnedMaterial = true;
                        Debug.LogWarning($"[WarpforgeVFX] 材质 `{materialName}` 取不到 —— 两条路都试过了："
                                       + "① `MaterialResolver`（挂钩子的全局查找）；"
                                       + "② **本效果 binder 里按名字找**（`EffectExporter` 会把它收进去，"
                                       + "2026-10-01 起）。都没命中 ⇒ 按原版「material 为空」的兜底走"
                                       + "（自定义渲染器那条退到它的 sharedMaterial）。"
                                       + "⚠️ 找不到时先确认**该效果重导过没有**（导出器是按效果名读 "
                                       + "`数据/游戏数据/module_materials.tsv` 的）。这条警告只报一次。");
                    }
                }
            }

            if ((rendererTargets == null || rendererTargets.Length == 0) &&
                customRenderers != null && customRenderers.Length > 0)
            {
                var list = new System.Collections.Generic.List<Renderer>();
                for (int i = 0; i < customRenderers.Length; i++)
                {
                    var r = ResolveNode<Renderer>(transform, customRenderers[i], name);
                    if (r != null) list.Add(r);
                }
                rendererTargets = list.ToArray();
            }

            if (materialAnimationCount > 0 && tweens.Length == 0) WarnTweens();
        }

        Renderer[] Renderers()
        {
            if (rendererTargets != null && rendererTargets.Length > 0) return rendererTargets;
            return new Renderer[0];
        }

        Transform ActingCard()
        {
            // 原版这里读的都是 **actingCard**（不是 objective 那张），见 `__ToggleMaterial.c:105-107`
            WFEffectCardContext ctx;
            if (WFEffectCards.TryGet(Controller, out ctx, false) && ctx.actingCard != null) return ctx.actingCard;
            return null;
        }

        void WarnNoCardContext()
        {
            MissingCardContext++;
            if (_warnedNoCardThis) return;
            _warnedNoCardThis = true;
            Debug.LogWarning($"[WarpforgeVFX] `{name}`（效果 {EffectName}）要改**卡面材质**，但拿不到"
                           + "『出手卡』（`WFEffectCards.Resolver` 没挂 / 返回的 actingCard 是 null）"
                           + "⇒ 这次不换材质。挂法见 `WFModuleTransformModifier.cs` 头注释。");
        }

        void WarnNoCardLayer()
        {
            MissingCardLayer++;
            if (_warnedCardLayer) return;
            _warnedCardLayer = true;
            Debug.LogWarning("[WarpforgeVFX] 🔴 卡材质层没接：`WFModuleChangeMaterial.SetCardMaterial` / "
                           + "`.RestoreOriginalMaterial` / `.CardTexture` 没挂 ⇒ 原版 `actingCard.CardUI."
                           + "SetCardMaterial / RestoreOriginalMaterial` 那条路做不了"
                           + "（受影响的 3 例：`AmbushEffect` / `StealthEffect` / `VanguardIdleEffect`）。"
                           + "这条警告只报一次。");
        }

        void WarnTweens()
        {
            SkippedTweens++;
            if (_warnedTweens) return;
            _warnedTweens = true;
            Debug.LogWarning("[WarpforgeVFX] 🔴 材质补间**播不了**：`fade` 开着（原版这三例的 `fade` 都是 1），"
                           + $"但一条参数都没读到（`materialAnimations` 数据里有 {materialAnimationCount} 条，"
                           + "读到 0 条）⇒ 按原版「空列表」的语义走 = **不播补间、直接切**。"
                           + "🔴 **这不是原版如此** —— 三条补间**都在原件里**（内联在 "
                           + "`materialAnimations.list[0].rid → references.RefIds[].data`）。"
                           + "⚠️ **2026-10-14（`A1882`）起那条链已经接通**（`dump_animfx.py` 的 "
                           + "`REFERENCES_MAX_DEPTH` + `gen_animfx_modules.py` 的 `flatten_material_animations`）"
                           + " ⇒ **本条警告再响 = 数据/库是旧的**：重跑 `工具/dump_animfx.py` → "
                           + "`工具/gen_animfx_modules.py` → Unity `EffectLibraryBuilder.Run`"
                           + "（效果库在 `Assets/Resources/WarpforgeVFX/`，进不了仓库、必须重建）。"
                           + "（`materialAnimationCount` 读到 **0** 也是同一个信号 —— 那是**旧数据**："
                           + "拍平成 `materialAnimations[i].…` 之后 `WFModuleDef.CountList` 才数得到。）"
                           + "这条警告只报一次。");
        }
    }
}
