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
//   2. **`material` 资产**（`@asset:Material:<名字>`）：原版按资产找。我们这边要挂
//      `MaterialResolver`，或者在 Inspector 里直接给 `material`。
//      ⚠️ **这 3 张材质工程里没有**（2026-09-18 查过 `WarpforgeVFX/Materials/`：只有
//      `Vanguard_Frame VAT`，**没有** `Vanguard_Frame VAT Dissolve` / `Card 3d Dissolve Blend
//      Image Ambush` / `Card 3d Stealth`）。原因：导出器只导**渲染器上**的材质
//      （`WarpforgeArena1/Editor/EffectExporter.cs:290` 的 `GetComponentsInChildren<Renderer>`），
//      只被模块字段引用的材质不会被带上。⇒ 解析不到时**按原版「material 为空」的兜底走**
//      （自定义渲染器那条退到 `customRenderers[0].sharedMaterial`）并出声。
//   3. `Renderer.SetMaterial`（Unity 6 的 API，不实例化）→ 我们用 `renderer.sharedMaterial = `。
//      **等价**：我们那个 `new Material(...)` 本来就是这条模块私有的唯一实例。
//   4. **`fade` 这条我们不做**（见下「没还原」②）。
//
// 🔴 没还原的
// ----------------------------------------------------------------
//   ① `materialAnimations`（`DynamicList<MaterialTweenBase>`）：这些**材质补间资产没导出来**
//      （`数据/游戏数据/tween/` 里 74 份全是 `UnitTweenSO`，0 份 `MaterialTween`）。3 例的
//      `materialAnimations` **都是空的** ⇒ 空列表下原版的语义就是「不播补间」：
//      「关」那条路里原版是「等最长时长（= 0 秒）再 `RestoreOriginalMaterial`」
//      （`__ToggleMaterial.c:52-95`，`DynamicList.list` 为空时 `MoveNext` 立刻为 false）
//      ⇒ 我们**同步**恢复（原版晚一帧，因为走了 `WaitForSeconds`，`MoveNext.c:19-30`）。
//      **有素材时这条要补上**（`materialAnimationCount > 0` 会 `LogWarning`）。
//   ② `fade`：原版是把 `materialAnimations` 里的补间播出来做淡入淡出（DOTween）。素材没有
//      ⇒ 我们只做「换材质 / 换贴图 / 恢复」，不做补间（3 例的 `fade` 都是 1，所以这 3 个
//      效果在原版里是有过渡的、我们这边是**硬切**）。
//   ③ `actionStart == Manual(15)` 那 1 例（`VanguardIdleEffect`）：原版**谁在什么时候调
//      `ToggleMaterial` 查不到**（块1 §8 没记调用点）⇒ 我们把它暴露成公开方法
//      `ToggleMaterial(bool)`，等接线；**它自己什么都不会做**（Initialize/Exit 都不满足条件）。
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

        /// <summary>数据里 `materialAnimations` 有几条（我们**不播**，见文件头「没还原」①）。</summary>
        [HideInInspector] public int materialAnimationCount;

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
        /// <summary>`@asset:Material:<名字>` → 材质（见文件头「我们自己定的」2）。</summary>
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
            }
            else
            {
                // 原版：这种组合**不恢复**（自定义渲染器本来就是它自己的材质）
                if (!isCardMaterial && useCustomRenderers) return;
                Restore();
            }

            // `fade`：见文件头「没还原」②（补间资产没导出来 ⇒ 硬切）
            if (fade) WarnTweens();
        }

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
                if (material == null)
                {
                    MissingMaterial++;
                    if (!_warnedMaterial)
                    {
                        _warnedMaterial = true;
                        Debug.LogWarning($"[WarpforgeVFX] 材质 `{materialName}` 在工程里找不到"
                                       + "（`WarpforgeVFX/Materials/` 里查过：这 3 张卡材质**没导出来** —— "
                                       + "导出器只导渲染器上的材质，只被模块字段引用的带不上）⇒ 按原版"
                                       + "「material 为空」的兜底走（自定义渲染器那条退到它的 "
                                       + "sharedMaterial）。要真换材质：导一张进来 + 挂 "
                                       + "`WFModuleChangeMaterial.MaterialResolver`（或 Inspector 里给 "
                                       + "`material`）。这条警告只报一次。");
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

            if (materialAnimationCount > 0) WarnTweens();
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
            Debug.LogWarning("[WarpforgeVFX] 🔴 未还原：`fade` / `materialAnimations` —— 原版用 "
                           + "`MaterialTweenBase` 资产做材质补间（淡入淡出），那些资产**没导出来**"
                           + "（`数据/游戏数据/tween/` 里 74 份全是 `UnitTweenSO`）⇒ 我们**硬切**。"
                           + $"数据里这 3 例的 `fade` 都是 1。这条警告只报一次。");
        }
    }
}
