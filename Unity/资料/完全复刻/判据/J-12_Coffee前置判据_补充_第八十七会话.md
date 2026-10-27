# J-12 · `Coffee.UIEffect` 前置判据 —— 补充（第八十七会话 · 只读代理 `R2`）

> 2026-10-27 · 只读调查。⛔ 未改任何 `.cs` / `.dll` / `.asmdef` / 账文件，未跑 Unity，未动 git。
> 上游 → `判据/J-12_Coffee前置判据.md`（第一轮，`E2UDW`）· `报告/B9_W5COF_Coffee搬运.md`（搬运取证）
> 本文件只补「前置三件」+ 顺带核 dll 事实。**用户 2026-10-27 已批准搬 dll**（本文件不改这个结论，只给施工事实）。

---

## ① 结论（三件各一行）

1. **shader**：全库**只有 1 只** Coffee shader —— `Hidden/UI/Default (UIEffect)`，唯一命中 = `assets_full/globalgamemanagers/Shader/Shader_77.json`；
   **没有** `UIDissolve` / `UIShiny` / `UITransitionEffect` 之类的独立 shader（Coffee 把全部滤镜塞进这 1 只 + **44 个关键字**）。
   源码被剥（`m_ParsedForm` 里无 `m_Script`）⇒ 只能手写/反编译，不能变成工程资产。**6 个随包 shader bundle 里都没有它**（UnityPy 实读）。
2. **7 个实例**：A/E `uiEffects` **空数组**；B/C/D/F/G 各 **1 颗 `UIEffect`**（全都挂在 toggler 同一个 GameObject 上）。
   **152 颗 `UIEffect`（menus 149 + 主菜单场景 3）100% 都只用 `TransitionFilter = 4 (Shiny)`**，其余 9 族滤镜全 `None`。
   B 的 prefab **全库 0 引用 ⇒ 孤儿 prefab**；D 的 prefab 被 5 个 **排行榜/每日奖励**弹窗引用（P2 域）。
3. **等价类**：`CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` **只覆盖 Shiny 一族**（且覆盖了 152/152 的真实用例）；
   **「换成真组件」在架构上不成立** —— Coffee 的 `UIEffectBase` 是 `UIBehaviour + IMeshModifier + IMaterialModifier + ICanvasRaycastFilter + ITimeControl`（必须挂在 uGUI `Graphic` + `Canvas` 上），
   而我们这 7 个宿主全是 `ImageQuad`（`MonoBehaviour` + `MeshRenderer`）⇒ 装不上去。且工程里那份 dll **一个特性都没有**（`[SerializeField]` 全丢，实测）。

---

## ② 逐条证据

### ②-1 Coffee shader：只有 1 只，44 个关键字

**搜法**（可复跑）：把 `d:/2/新解包资源/assets_full/*/Shader/*.json` 全 234 个文件的 `m_ParsedForm.m_Name` 取出来过一遍。

- **唯一命中**：`d:/2/新解包资源/assets_full/globalgamemanagers/Shader/Shader_77.json`
  - `m_ParsedForm.m_Name` = **`Hidden/UI/Default (UIEffect)`**
  - `m_AssetGUID.data[0..3]` = `3786818153 / 1548391642 / 1960798618 / 1897553079`
  - `m_ShaderIsBaked` = `True` · `platforms` = `[4, 18]` · `stageCounts` = `[2, 1]`
  - `compressedLengths` = `[[10558],[41900]]` · `decompressedLengths` = `[[182376],[263772]]`（字节码在，源码不在）
  - `m_ParsedForm` 的键只有 `m_PropInfo / m_SubShaders / m_KeywordNames / m_KeywordFlags / m_Name / m_CustomEditorName / m_FallbackName / m_Dependencies / …`
    ⇒ **`m_Script` 不存在 = HLSL 源码被剥**（与 `CLAUDE.md` §三「Bundle 里的 Shader 落不了工程资产」一致）
  - 1 个 SubShader（tags：`CanUseSpriteAtlas=true` · `IGNOREPROJECTOR=true` · `PreviewType=Plane` · `QUEUE=Transparent` · `RenderType=Transparent`）· 1 个 Pass
  - `m_PropInfo.m_Props` 只有 **9 条**（`_MainTex` `_Color` `_StencilComp` `_Stencil` `_StencilOp` `_StencilWriteMask` `_StencilReadMask` `_ColorMask` `_UseUIAlphaClip`）——
    其余滤镜参数**不在属性表里**（是编译进变体的常量/CBUFFER）

**`m_KeywordNames` 全 44 条**（⚠️ `NOTICE.md` §5 写「46 个关键字」= **错 2 个**，本轮现读 = 44；铁律 5 供后续订正）：

```text
STEREO_INSTANCING_ON · UNITY_SINGLE_PASS_STEREO · STEREO_MULTIVIEW_ON · STEREO_CUBEMAP_RENDER_ON
UNITY_UI_CLIP_RECT · UNITY_UI_ALPHACLIP · UIEFFECT_EDITOR · SOFTMASK_EDITOR
TONE_GRAYSCALE · TONE_SEPIA · TONE_NEGATIVE · TONE_RETRO · TONE_POSTERIZE · COLOR_FILTER
SAMPLING_BLUR_FAST · SAMPLING_BLUR_MEDIUM · SAMPLING_BLUR_DETAIL · SAMPLING_PIXELATION
SAMPLING_RGB_SHIFT · SAMPLING_EDGE_LUMINANCE · SAMPLING_EDGE_ALPHA
TRANSITION_FADE · TRANSITION_CUTOFF · TRANSITION_DISSOLVE · TRANSITION_SHINY · TRANSITION_MASK
TRANSITION_MELT · TRANSITION_BURN · TRANSITION_PATTERN · TRANSITION_BLAZE
EDGE_PLAIN · EDGE_SHINY
DETAIL_MASKING · DETAIL_MULTIPLY · DETAIL_ADDITIVE · DETAIL_SUBTRACTIVE · DETAIL_REPLACE · DETAIL_MULTIPLY_ADDITIVE
TARGET_HUE · TARGET_LUMINANCE
GRADATION_GRADIENT · GRADATION_COLOR2 · GRADATION_COLOR4
SOFTMASKABLE
```

（⇒ **没有 `EDGE_GRADIENT`**、没有 `TRANSITION_*` 之外的独立滤镜 shader —— 想找 `UIShiny`/`UIDissolve` 那个「独立组件版」在这里**不存在**。
原版用的就是 **Coffee 4.x 的单组件版**：一切滤镜 = `UIEffect` 的枚举字段 + 关键字变体。）

**工程侧现状**：`Assets/CardPresentation/Resources/Shaders/WildcardShine.shader` = **只有 `TRANSITION_SHINY` 这一变体**的移植
（文件头 `:1-70` 逐条写明判据出处 = ruri 反编译出的 `Hidden_UI_Default (UIEffect)_0/Sub0_Pass0_Fragment_b16.hlsl`）。

**随包 shader bundle 里没有它**（UnityPy 1.25.3 实读，`Assets/StreamingAssets/WarpforgeVFX/`）：

| bundle | Shader 数 | `UIEffect` 命中 |
|---|---|---|
| `wf_shaders.bundle` | 45 | 0 |
| `wf_shaders_extra.bundle` | 42 | 0 |
| `wf_arena_shaders.bundle` | 7 | 0 |
| `wf_menus_extra.bundle` | 2 | 0 |
| `wf_builtin.bundle` | 15 | 0 |
| `wf_tmp_shaders.bundle` | 4 | 0 |

⇒ 走「真组件」路线**必须新打一只含 `Hidden/UI/Default (UIEffect)` 的随包 bundle**（要 Unity 批处理，见 ③-3）。

### ②-2 7 个实例（`uiEffects[]` 逐实例）

索引方式：`m_Script` PPtr 反查（`CLAUDE.md` 那条「一个 bundle 里的 MonoBehaviour 要按 `m_Script` 反查」）。
- `UIEffectTogglerCoffeeEffects` 的 MonoScript = `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-4976365502517319154.json`
  （`m_AssemblyName = Assembly-CSharp` · `m_Namespace = ""`）
- `Coffee.UIEffects.UIEffect` 的 MonoScript = `MonoScript_-8944329795305972251.json`（`m_AssemblyName = Coffee.UIEffect`）
- `Coffee.UIEffects.UIEffectTweener` 的 MonoScript = `MonoScript_-4281255610371527282.json`

**7 个实例全部落在 `bundle_menus_assets_all/MonoBehaviour/`**（全库 `grep -rl "-4976365502517319154"` = **7**，无遗漏）：

| # | toggler MB（pathID） | 节点路径 | `uiEffects` | 指向的组件 | 组件 `m_Enabled` | 同节点 Tweener |
|---|---|---|---|---|---|---|
| A | `-4582627691824185051` | `Ranked Army Selector Container V2 / Featured Icon` | **0（空）** | —— | —— | 无 |
| B | `1098224782659508560` | `Army Item Button No Arrow Variant / Icon` | 1 | `UIEffect` `-1699783776568583856` | **0** | 1.0 / interval 0 / Once / PlayOnEnable 0 |
| C | `1119464182961805002` | `Campaign Army Item Button / Icon` | 1 | `UIEffect` `-915853924510394678` | **1** | 1.0 / 0 / Once / 0 |
| D | `2729166925368294341` | `Army Item Button / Icon` | 1 | `UIEffect` `-1357910490681541691` | **0** | 1.0 / 0 / Once / 0 |
| E | `4255057204549812517` | `Ranked Army Selector Container V2 / Army Icon` | **0（空）** | —— | —— | 无 |
| F | `4828778361430798116` | `Deck Editing Menu / Content Area / Sidebar / Footer / Done` | 1 | `UIEffect` `6027968899341807396` | **1** | 1.0 / 0 / **Loop** / 0 |
| G | `6572107548950042537` | `Forge Army Item Button / Icon` | 1 | `UIEffect` `-1549575033197389911` | **0** | 1.0 / 0 / Once / 0 |

7 个 toggler 自身序列化值全同：`m_Enabled = 1` · `deactivateOnDisable = 1` · `m_Name = ""`。

**5 颗 `UIEffect` 的字段（逐条现读，与 `B9_W5COF` §2·6 一致，本轮独立复核）**：

| 字段 | 值（5 颗） |
|---|---|
| `m_ToneFilter` / `m_ToneIntensity` | 0 / 1.0 |
| `m_ColorFilter` / `m_ColorIntensity` / `m_Color` / `m_ColorGlow` | 0 / 1.0 / (1,1,1,1) / 0 |
| `m_SamplingFilter` / `m_SamplingIntensity` / `m_SamplingWidth` / `m_SamplingScale` | 0 / 0.5 / 1.0 / 1.0 |
| **`m_TransitionFilter`** | **4 = `TransitionFilter.Shiny`**（枚举判据 = `d:/2/Warpforge_code/Scripts/Coffee.UIEffect/Coffee/UIEffects/TransitionFilter.cs`：`None=0,Fade=1,Cutoff=2,Dissolve=3,Shiny=4,Mask=5,Melt=6,Burn=7,Pattern=8,Blaze=9`） |
| `m_TransitionRate` | **0**（5 颗全是 0；`1` = 不亮） |
| `m_TransitionRotation` / `m_TransitionWidth` / `m_TransitionSoftness` | **120 / 0.25 / 0.5** |
| `m_TransitionKeepAspectRatio` | **1** |
| `m_TransitionRange` | **(0, 1)** |
| `m_TransitionColorFilter` / `m_TransitionColorGlow` / `m_TransitionReverse` | **6** / 0 / 0 |
| **`m_TransitionColor`** | **4 颗 = (1,1,1,1)；F（Done）= (3.4822021, 3.4822021, 3.4822021, 1)** ← 唯一实例级差异 |
| `m_TransitionAutoPlaySpeed` | 0 |
| `m_TransitionTex` | 外链 `m_FileID 14 / m_PathID -6023190078349834598`（= 既有注释里的 `Transition-Horizontal`，256×256 Alpha8；**Shiny 这一路不用它**，见 `WildcardShine.shader` 文件头 ③） |
| `m_CustomRoot` | **null（全 5 颗）** ⇒ 掩码坐标空间 = 自己那一件 Rect |
| `m_TargetMode` / `m_EdgeMode` / `m_DetailFilter` / `m_GradationMode` / `m_ShadowMode` | 0 / 0 / 0 / 0 / 0 |
| `m_BlendType` / `m_SrcBlendMode` / `m_DstBlendMode` | 1 / 1 / 10 |
| `m_Flip` | 0 |

**5 颗 `UIEffectTweener`**（同 GameObject，`GetComponent` 拿得到）：
`m_Enabled 1` · `m_CullingMask 367`（= Tone+Color+Sampling+Transition+GradationOffset+GradationRotation+EdgeShiny）· `m_Direction 0 (Forward)` ·
`m_Delay 0` · **`m_Duration 1.0`** · **`m_Interval 0`** · **`m_PlayOnEnable 0 (None)`** · `m_ResetTimeOnEnable 1` ·
`m_Curve` = **线性 (0,0)→(1,1)** · **`m_WrapMode 0 (Once)`，只有 F = `1 (Loop)`** · `m_UpdateMode 0 (Normal)`。
（枚举语义判据 = `d:/2/Warpforge_code/Scripts/Coffee.UIEffect/Coffee/UIEffects/UIEffectTweener.cs:12-57`；
 `m_Interval` 的 Tooltip = `"The interval in seconds between each loop."`）

**→ 与我们的等价层逐项对得上**：`WildcardShineLayer.Params.Icon`（120/0.25/0.5/白/filter 6/Once/1.0 s）与 `Params.DoneButton`（3.4822 + Loop）
正好是这两档，见 `Effects/UIEffectTogglerCoffeeEffects.cs:148-156`。

### ②-3 🆕 顺带查清的两件旧「未定」

**(a) B `Army Item Button No Arrow Variant` = 孤儿 prefab**（第一轮写「宿主未定」）：
- prefab 在 `d:/2/tools/assetripper_out/ExportedProject/Assets/Army Item Button No Arrow Variant.prefab`，`m_meta` guid = `36ba919855e657c418a4a0588b25bcf0`
- 该 guid 在**整个导出工程**（`*.prefab` / `*.unity` / 全部资产）里 **0 命中**（唯一命中是它自己的 `.meta`）
- 名字串在 `d:/2/tools/all_strings.txt` 里 **0 命中**（也 `grep` 过 `d:/2/tools/decomp_full` 与 `d:/2/Warpforge_code/Scripts` = 0）
⇒ **原版运行时不会实例化它**（至少不是按名字/按 GUID 载入）⇒ 我们**不必**为它找宿主。⛔ 别猜 `Cell_fac_*`。

**(b) D `Army Item Button` = 排行榜 / 每日奖励那族弹窗的行模板**（第一轮写「宿主未定」）：
- prefab `Army Item Button.prefab`，guid = `da958ae8466b90a458a216398e270d0f`
- 引用它的 prefab（GUID 反查，5 个）：`Daily Reward Popup` · `DraftLeaderboardPopup` · `RankedSkirmishLeaderboardPopup` ·
  `RankedClassicLeaderboardPopup Variant` · `Army Selector`
⇒ 属于 **P2（排行榜/活动）** 域，不是收藏左抽屉。

**(c) A/E 的 prefab**：`Ranked Army Selector Container V2.prefab` 被 `RankedEventWindowV2` / `SkirmishModeEventWindow` / `Ranked Army Selector` 引用；
它们的 `uiEffects` 本来就是**空数组** ⇒ 这两格「什么都不做」是原版行为（⛔ 别报警，别补层）。

### ②-4 🆕 全库口径：Coffee 在界面里的真实用量（比「7 个实例」大得多）

| 项 | 数 | 出处 |
|---|---|---|
| `Coffee.UIEffects.UIEffect` 实例 | **152**（`bundle_menus_assets_all` **149** + `bundle_scenes_scenes_mainmenuwarpforge` **3**） | 按 `m_Script = -8944329795305972251` 全 bundle 扫 |
| `Coffee.UIEffects.UIEffectTweener` 实例 | **221**（menus **218** + 主菜单场景 **3**）；⚠️ 218 个**全部**挂在「有 `UIEffect` 的同一节点」上，但 `UIEffect` 只有 149 颗 ⇒ **一个节点可有多个 Tweener**（各自 `m_CullingMask` 驱动不同子属性） | 按 `m_Script = -4281255610371527282` + 与 UIEffect 的 GO 交集实测 |
| `UIEffectTogglerCoffeeEffects` 实例 | **7**（全在 menus） | 同 ②-2 |
| **`m_TransitionFilter` 分布** | **4 (Shiny) = 152 / 152** | 见下 |
| `m_ToneFilter` / `m_ColorFilter` / `m_SamplingFilter` / `m_EdgeMode` / `m_DetailFilter` / `m_GradationMode` / `m_ShadowMode` / `m_TargetMode` | **全 0（152/152）** | 同上 |
| `m_BlendType` | 1（152/152） | 同上 |
| `m_Enabled` | 1 = 149 · 0 = 3（= B/D/G 那三颗） | 同上 |

**152 颗里只有 3 组参数**：

| 组合（rotation, width, softness, keepAspect, colorFilter, glow, autoPlaySpeed, color） | 颗数 | 是哪颗 |
|---|---|---|
| `120, 0.25, 0.5, 1, 6, 0, 0, (1,1,1,1)` | **150** | 默认那档 |
| `139, 0.213, 0.533, 1, 6, 0, 0, (2,2,2,1)` | 1 | `Message Container / Content`（menus） |
| `120, 0.25, 0.5, 1, 6, 0, 0, (3.4822021,3.4822021,3.4822021,1)` | 1 | `Deck Editing Menu / … / Footer / Done`（F） |

**Tweener 分布（152 颗 UIEffect 对应）**：`(duration, interval, wrapMode, playOnEnable)` 计数 ——
`1.0/5.0/Loop/Forward` = 70 · `1.0/1.5/Loop/Forward` = 57 · `1.0/0/Once/None` = 7 · `1.0/1.0/Loop/Forward` = 6 ·
`0.75/0/Once/None` = 3 · `1.0/0/Loop/Forward` = 3 · `1.0/0.65/Loop/Forward` = 2 · `1.0/0.5/Loop/Forward` = 1 ·
`1.0/0/Loop/None` = 1 · `2.75/3.5/Loop/Forward` = 1；**无 Tweener = 3**。

🔴 **两条对我们有直接意义的读数**：
1. **「滤镜本体」我们只需要 Shiny 一族** —— 152/152 都没开 Tone / Color / Sampling / Edge / Detail / Gradation / Shadow / Target。
   ⇒ 搬 dll 的收益只在这 1 族上体现，而这一族我们**已经有等价 shader**。
2. **绝大多数（148 颗）是「自动一直扫」**：`m_PlayOnEnable = Forward` + `m_Interval > 0`（1.5 / 5.0 s 的**两次扫光之间要停**）。
   我们现在的 `WildcardShineLayer` **既不自动播、也没有 interval 停顿**（`Tick` 里 `Mathf.Repeat(_time/d,1)`）——
   ⚠️ 对 7 个实例那 5 颗**无影响**（`PlayOnEnable 0`、`interval 0`），对商店/主菜单那 148 颗**是差异**。

**主菜单场景那 3 颗**（`bundle_scenes_scenes_mainmenuwarpforge`，供 P1/主菜单线接）：

| pathID | 路径 | Tweener |
|---|---|---|
| `1949` | `Game UI / Main Canvas / MainMenu / … / Navigation Panel / Buttons Container / Main Menu Navigation Button - Shop / Image` | **2.75 s / interval 3.5 / Loop / PlayOnEnable Forward** |
| `2036` | `… / 3 - PopUp Holder / Card Displayer Menu For Menu / Card Options Panel / Panel / Alternate Art Panel / Buy Original Card Button / Generic UI Button` | 无 |
| `2178` | `… / 3 - PopUp Holder / Card Displayer Menu For Menu / Card Options Panel / Wildcard Segment / WIldcard Counter / Background` | 无 |

### ②-5 dll 事实（搬运时校验用）

**5 份本机候选 + 1 份工程内**（大小 / md5）：

| 路径 | 字节 | md5 |
|---|---|---|
| 🟢 `d:/4/Unity/MyGame/Assets/ThirdParty/Coffee/Coffee.UIEffect.dll`（**已在工程里**） | 329,216 | `ca5d34bd12d1f740ec43dc66227f4391` |
| `d:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll`（**= 工程那份，逐字节相同**） | 329,216 | `ca5d34bd12d1f740ec43dc66227f4391` |
| `d:/2/unity_run_ref/MelonLoader/Il2CppAssemblies/Il2CppCoffee.UIEffect.dll`（Il2CppInterop 代理，**不可用**） | 348,672 | `4a29aa58bdd656c6914f32d89cfa2724` |
| `d:/2/unity_run_ref/MelonLoader/…/Cpp2IL/cpp2il_out/Coffee.UIEffect.dll` | 156,160 | `e75b53b0bff270e41f059c4881c27cfa` |
| `d:/2/tools/cpp2il/out_cpp2il/Coffee.UIEffect.dll`（方法体全空） | 53,248 | `f11949005e9fd7cd54bec5515094e48c` |
| `d:/2/tools/assetripper_out/AuxiliaryFiles/GameAssemblies/Coffee.UIEffect.dll`（= `ExportedProject/Assets/Plugins/` 那份，全空） | 60,928 | `0516c9ed1d0eba2d6c0770ec03787a55` |
| `d:/2/tools/il2cpp_out/DummyDll/Coffee.UIEffect.dll` | 150,016 | `9f4cb93057266392245f4f819a8ec6e7` |
| `d:/2/Warpforge_code/Assemblies/Coffee.UIEffect.dll` | 61,440 | `097a2be3171fb216530321b3788fcf06` |

**程序集引用**（工程那份，二进制里逐名字命中）：
`mscorlib` · `System.Core` · `System.Runtime` · `System.Collections` · `UnityEngine` · `UnityEngine.CoreModule` · `UnityEngine.UI` ·
`UnityEngine.UIModule` · `UnityEngine.TextRenderingModule` · `Unity.TextMeshPro` · `UnityEngine.DirectorModule` · `Unity.Timeline`
⇒ 工程侧齐：`Packages/manifest.json` 有 `com.unity.timeline 1.8.13` 与 `com.unity.ugui 2.0.0`（TMP 走 TMP Essentials）⇒ **可解析**。

**`.meta` 现状**（`Assets/ThirdParty/Coffee/Coffee.UIEffect.dll.meta`）：`guid: 09ad0e0aa7f340ae89ad474f30024412`，
`PluginImporter.platformData` = `Any: enabled 1` / **`Editor: enabled 0`**。
🔴 **这格是个雷**：Editor 平台被显式关掉 ⇒ 只要有任何 `.cs` 写 `using Coffee.UIEffects;`，**编辑器侧编译会引用不到**（而 standalone 能）。
现在没事，只因为 `UIEffectTogglerCoffeeEffects.cs` 已经**不引用** Coffee 类型了（见 ③-1）。真要接真组件时**必须把它改成 1**。

**dll 的缺陷（本轮独立复核，非转述 NOTICE）**：
- `ilspycmd -t Coffee.UIEffects.UIEffect` 输出头部 = `public class UIEffect : UIEffectBase` + 一串 `protected ToneFilter m_ToneFilter;` ——
  **一条特性都没有**（无 `[SerializeField]` / `[ExecuteAlways]` / `[DisallowMultipleComponent]` / `[Range]`）
  ⇒ 在 Unity 里**这 81 个字段不会被序列化**（Inspector 配不了、prefab 数据进不去）。
- 该类型内 `Cpp2ILHelpers.NoteDecompilerIssue(...)` **124 处**（整包 1,430 处，与 NOTICE 一致）。
- 好消息：**公开属性是完整可用的**（`public TransitionFilter transitionFilter { get; set; }` 这类，setter 体在）
  ⇒ 真要跑，只能**代码侧逐字段用属性赋**，不能靠 Inspector/序列化。
- 类型签名：`UIEffectBase : UIBehaviour, IMeshModifier, IMaterialModifier, ICanvasRaycastFilter, ITimeControl`
  （判据 = `d:/2/Warpforge_code/Scripts/Coffee.UIEffect/Coffee/UIEffects/UIEffectBase.cs:11`）⇒ **必须 uGUI `Graphic` + `Canvas`**。

---

## ③ 施工建议（下一批照着做）

### ③-1 先认清一件事：工程里**已经有** dll，缺的不是「搬」，是「能不能挂」

- `Assets/ThirdParty/Coffee/Coffee.UIEffect.dll`（329,216 B，= `out_cpp2il_dev` 那份）+ `NOTICE.md` **已就位**（2026-10-25/27 落的）。
  搬运动作**无需重做**；校验时比 md5 `ca5d34bd12d1f740ec43dc66227f4391` 或大小 329,216。
- `CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` **当前完全不用 Coffee 类型**：
  字段是 `List<WildcardShineLayer> shineLayers`（`:59`），`Toggle` 里 `SetEnabled/Play`（`:64-82`），`DoUpdate`/`AnimationFinished` 空桩（`:86,89`）。
- 全工程 `grep UIEffectTogglerCoffeeEffects` = **只有它自己**（另有 **4 条**别处注释提到它：`Deck/DeckRuntime.cs:9116` · `Shell/CollectionWindow.cs:6511` · `Shell/InboxWindow.cs:342,1228`）；
  真正接的是宿主**直接建** `WildcardShineLayer`（`Shell/CampaignTab.cs:804,857,861` · `Deck/DeckRuntime.cs:8916-8933, 9026-9053` · `Shell/ForgeTab.cs:727,1333-1340`）。

### ③-2 ⛔ 「接成真组件」为什么不成立（先读这条，别开工）

| 判据 | 事实 | 后果 |
|---|---|---|
| `UIEffectBase : UIBehaviour, IMeshModifier, IMaterialModifier, ICanvasRaycastFilter, ITimeControl`（`UIEffectBase.cs:11`） | 只能在 uGUI `Graphic` + `Canvas` 下工作 | 我们的 7 个宿主是 `ImageQuad`（`CardPresentation/Battle/ImageQuad.cs:13` = `MonoBehaviour` + `MeshRenderer`）⇒ **`AddComponent<UIEffect>()` 拿不到 Graphic，`GetModifiedMaterial` 永远不被 Canvas 调** ⇒ 装了也是死的 |
| dll 里 `UIEffect` **零特性**（实测） | 81 个字段不序列化 | prefab 里存不了原版那套参数；只能代码赋属性 |
| `Hidden/UI/Default (UIEffect)` **不在任何随包 bundle**（UnityPy 实读 6 个） | `Shader.Find` 取不到 | 就算装上了，`UIEffectBase.GetModifiedMaterial` 会退回底材质 ⇒ **一个关键字都不生效** |
| `m_ParsedForm` 无 `m_Script` | 源码被剥 | 只能新打 bundle 带原件（需 Unity 批处理）或手写 shader |

⇒ **「搬 dll + 接成真组件」的实际工作量 = 新打一只 shader bundle（Unity 批处理）+ 把 7 个宿主改成 uGUI `Image`（含 Canvas 层级）+ 用公开属性逐字段赋值 + 真机验**。
**这是改造 7 个宿主节点的结构性改动**，不是「接线」。而且换来的是**我们已经有等价物的同一族滤镜**（152/152 都只用 Shiny）。

### ③-3 建议的三条路（按代价排序）

**路 A（推荐 · 保持现状 + 补三处）**：不挂真组件，继续用 `WildcardShineLayer`。
理由现在是**证据级**的：原版 152 颗 100% 只用 Shiny，我们的等价层已覆盖该变体的 `colorFilter 6/0`、`colorGlow 0/1`、`reverse 0/1`（`WildcardShine.shader:48-55` 自述）。
补三处（都小、可独立做）：
1. **shader 注释订正**：`WildcardShine.shader:56-58` 与 `UIEffectTogglerCoffeeEffects.cs:92-103` 都写「它不在 `Resources/` 下 ⇒ 靠 GraphicsSettings AlwaysIncluded」——
   **实际路径就是 `Assets/CardPresentation/Resources/Shaders/WildcardShine.shader`** ⇒ `Shader.Find` 在编辑器/player 都能取到，AlwaysIncluded 那一跳**不需要**。
   （改注释即可；`WarnIfVisualBodyMissing` 那句 warning 文案一并订正。）
2. **`UIEffectTweener.m_Interval` 与 `m_PlayOnEnable` 补语义**（只影响 148 颗常驻扫光，不影响这 5 颗）：
   `interval` = 两次循环之间的停顿时长（`UIEffectTweener.cs:88-91` 的 Tooltip）；`PlayOnEnable = 1 (Forward)` ⇒ 启用即自播。
   现在 `Tick` 是 `Mathf.Repeat(_time/d,1)`（无停顿）⇒ 商店那批会因为「一直扫」与原版观感不同。
3. **把 `UIEffectTogglerCoffeeEffects` 接上**（现在 0 调用者，宿主自己建层）：
   若要「1:1 保持原版形状」，宿主应持有一个 toggler 实例（`shineLayers` 填 1 个），调 `Toggle(bool)` 而不是直接 `layer.Play()`。
   ⚠️ 这是**形状**统一，不改画面；做之前先确认 `targetStateEnabled` 那一格有没有别处读。

**路 B（= 真组件 · 最小可行）**：只给**其中 1 颗**做试验田（建议 F `Deck Editing Menu/…/Done`，因为它 Loop、参数最好认）。
1. 新打一只 bundle：把 `globalgamemanagers/Shader/Shader_77` 的 Shader 资产整只搬进 `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle`
   （套路 = `NOTICE.md` §5 提的 `extract_mirror_shaders.py`；**这步要 Unity 批处理**）。
2. 给那颗节点加一个 uGUI `Image`（同 RectTransform 尺寸）+ 一条 `Canvas`（或复用现有；我们 shell 没有 Canvas ⇒ 要新建一层 Screen-Space Canvas，
   ⚠️ 会引入「uGUI 在正交相机世界空间怎么摆」的新坐标问题 = 新的一摊活）。
3. `AddComponent<UIEffect>()` + 逐字段用**公开属性**赋（`transitionFilter = TransitionFilter.Shiny` 等 20 余个）；`AddComponent<UIEffectTweener>()` 用同样方式赋 `m_Duration/m_WrapMode/…`（⚠️ Tweener 的字段是 `private` + 特性丢了 ⇒ **Tweener 配不了**，只能自己写一个等价播放器，或反射改私有字段 —— 后者不建议）。
4. `.meta` 把 `Editor: enabled` 改成 1，再让 `.cs` `using Coffee.UIEffects;`。
5. 验：`Shader.Find("Hidden/UI/Default (UIEffect)")` 非 null + 材质 `shader.name` 命中 + 真机看一眼。
⇒ **不建议先做这条**：第 2 步（uGUI Canvas 载体）与第 3 步（Tweener 无序列化）会各带出一摊新问题，而收益为 0（Shiny 已有等价物）。

**路 C（= 折中 · 只补视觉差距，不动 dll）**：
把 148 颗常驻扫光的 **interval 停顿 + 自动播**补进 `WildcardShineLayer`（= 路 A-2），并在商店/主菜单那几处接上（P1/P2 域，按各线任务表排）。
⇒ 这是「完全复刻」真正缺的那一块，而且**不需要 dll**。

### ③-4 施工后怎么验（不用 Unity 也能查的先查）

1. `bash d:/4/Unity/工具/typecheck.sh`（改 `.cs` 就必跑；本轮 0 处改动，故未跑）。
2. 断言（建议加在对应宿主自检里，期望值**取原版字段**，⛔ 别回读 `WildcardShineLayer.Params`）：
   - 5 颗目标：`layer.ShaderMaterial.GetFloat("_TransitionWidth") == 0.25f` / `_TransitionSoftness == 0.5f` / `_TransitionColorFilter == 6f` / `_TransitionRate` 初始 `1`；
   - `Play()` 后 `Rate == 0`，`StepAll(0.5f)` 后 `≈0.5`，`StepAll(1.0f)` 后 `==1` 且 `Playing == false`（Once）；
   - F 那颗 `Loop`：`StepAll(1.0f)` 后仍 `Playing == true`（且若补了 interval，要按 `duration 1.0 + interval 0` 验）；
   - A/E：`Toggle(true)` **不得**新建任何层（空数组 = 原版行为）。
3. 结构判据（本轮已做，可复跑）：7 个 toggler 实例 = `grep -rl "-4976365502517319154" assets_full/*/MonoBehaviour/*.json | wc -l` ⇒ **7**。

---

## ④ 还查不清 / 未核（写清搜过哪里）

1. **Coffee 的 `UpdateViewMatrix` 逐句原文仍拿不到** —— Cpp2IL 重建的 dll 里局部变量被打散（`ilspycmd -il` 只有 `IL_xxxx` + `NoteDecompilerIssue`），
   `d:/2/tools/decomp_full` 里也没有它（只有 `UIEffect.AvailableFilters` 那 4 个方法）。⇒ `WildcardShineLayer` 里 θ 之后那一步继续挂着 `🔶 自己设计`。
2. **`m_TransitionTex` 外链 `CAB-34ccb9…` 的这张 256×256 Alpha8 贴图**，我是**转述** `WildcardShine.shader:33-38` 的既有实测，**没有独立复核像素**
   （本轮它在 `bundle_menus_assets_all/Texture2D` 里按 pathID 找不到 ⇒ 属于另一个 CAB）。Shiny 不用它 ⇒ 不影响本域。
3. **148 颗常驻扫光的真实观感没验** —— 需要真 Play（`-wfscene Shell` 进商店/主菜单看扫光停顿节奏）。本轮只读，没进游戏。
4. **`Coffee.UIEffect.R`（image 127）与 `Coffee.UIParticle`（image 68）** 仍未查用途；本轮只确认主菜单/菜单里**没有任何 `.R` / UIParticle 的 MonoScript 被引用**
   （`bundle_Waprforge_monoscripts` 里 Coffee 系只有 3 个 MonoScript：`UIEffect` / `UIEffectTweener` / `UIParticle`；`.R` 一个都没有）。
   ⇒ `UIEffect.R` 大概是**死引用**（与 NOTICE 判断一致），但没有逐类证据。
5. **工程那份 dll 到底能不能被 Unity 加载**（`PluginImporter` 的 `Editor: 0` + il2cpp 重建产物的元数据形状）—— 需要一次 Unity 批处理实跑才算；
   现在的 `csc` 0 错只证明「Roslyn 能解析」，**不证明 Unity 的 AssetDatabase 能导入**。
6. **把 7 个宿主改成 uGUI `Image` 的回归面**（命中区 / 队列分层 / 悬停换图 / 缩放）**完全没评估** —— 路 B 的真实成本还是个未知数。
