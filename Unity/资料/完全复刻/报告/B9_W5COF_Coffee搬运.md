# B9_W5COF · Coffee.UIEffect 搬进工程（账 455）

> 2026-10-27 · 执行代理 `B9_W5COF` · **有写权限**
> ⛔ 未跑 Unity · 未动 git · 未改正本 / 别人的文件 / 账主表
> 判据 → `判据/J-12_Coffee前置判据.md` · 施工单 → `判据/B9_特效C_施工单.tsv` 行 455

---

## 一、结论

**搬完了、编得过，但「接上」只到「类型接上」，视觉本体仍然是 0。** 三句话：

1. **dll 已进工程**：`Assets/ThirdParty/Coffee/Coffee.UIEffect.dll`（329,216 B），
   来源 = `d:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll`（本机 5 份候选里**唯一有真方法体**的一份）。
   工程里原本**没有** asmdef（`find Assets -name "*.asmdef"` = **0**）⇒ **不建 asmdef**，dll 直接落进 `Assembly-CSharp`。
2. **类已 1:1 端口**：`UIEffectTogglerCoffeeEffects.cs` 现在用真 `Coffee.UIEffects.UIEffect` +
   `UIEffectTweener`，逐句照 `Toggle.c` 真体（`duration` 不接、不碰 `deactivateOnDisable`）。
   **编得过**：把 dll 手工加 `-r:` 之后，**运行时 0 / 编辑器 0**（见 §五·3）。
3. 🔴 **但三样东西仍然缺，缺了就出不来画面**：
   ① **载体**：原版 7 个实例全挂 **uGUI `Image`** 节点；我们对应节点是 `ImageQuad`（`MeshRenderer`）
   ⇒ Coffee 的 `UIEffect`（`IMaterialModifier`/`IMeshModifier`）**挂不上**。
   ② **shader**：`Hidden/UI/Default (UIEffect)` 只在原版包里、HLSL 源码被剥 ⇒ 落不成工程资产。
   ③ **dll 本身是残缺的重建产物**：特性全被剥、1,430 处 callee 靠猜、~800 条 IL 类型错乱
   （`UIEffectTweener.Play()` 反编译都坏）⇒ **能编译，跑起来不可信**。

⇒ **「能不能算复刻完了」= 不能。** 结构对了，实体没有。要真出画面，得在
「把对应节点改成 uGUI Image」与「给 ImageQuad 自写等价滤镜」之间做一次产品级选择（见 §三）。

---

## 二、证据（文件:行号 / 资产路径）

### 2·1 dll 与类型
| 事实 | 判据 |
|---|---|
| 原版以 3 个程序集引用 | `d:/2/tools/il2cpp_out/dump.cs` image 表：`Image 40: Coffee.UIEffect.dll - 20305` · `Image 68: Coffee.UIParticle.dll - 21783` · `Image 127: Coffee.UIEffect.R.dll - 22640` |
| `UIEffect` / `UIEffectTweener` 都在 image 40 | TypeDefIndex **20378**（`dump.cs:1067800`）/ **20392**（`dump.cs:1069562`），落在 20305–20417 区间 |
| `.R` 那个包是空壳 | image 127 只有 `22640 <Module>` + `22641 I25851843006f14186907b66589789599`（`dump.cs:1146990` / `:1146994`）⇒ **不需要** |
| `UIParticle` 用不到 | 本域无任何引用点；`Coffee.UIEffect.dll` 的引用表里也没有它 |
| 5 份候选只有 1 份能用 | 见 §六·2 |
| 引用能在本工程解析 | dll 字节里出现 `mscorlib` / `UnityEngine` / `UnityEngine.CoreModule` / `UnityEngine.UI` / `Unity.TextMeshPro`；`Library/ScriptAssemblies/Unity.TextMeshPro.dll` 与 `UnityEngine.UI.dll` 都在 |
| `Il2CppAssemblies/Il2CppCoffee.UIEffect.dll` **不可用** | 字节里是 `Il2Cppmscorlib` + `System.Runtime`（Il2CppInterop 代理，要 il2cpp 运行时在位）|

### 2·2 原版 `Toggle` 真体（不是 ILSpy）
`d:/2/tools/decomp_full/UIEffectTogglerCoffeeEffects__Toggle.c:20-45`（Ghidra）：

```
char * (param_1 + 0x21) = param_2;                    // targetStateEnabled = option
lVar1 = *(param_1 + 0x28);                            // uiEffects
for (uVar4 = 0; ; uVar4++) { if (count <= uVar4) return;
  lVar3 = ((param_1+0x20) + uVar4*8);                 // uiEffects[i]
  if (UnityEngine_Object__op_Implicit(lVar3, 0)) {    // != null（含假 null）
    UnityEngine_Behaviour__set_enabled(lVar3, param_2);
    if (param_2) { tw = GetComponent<UIEffectTweener>(lVar3);
                   if (op_Implicit(tw)) Coffee_UIEffects_UIEffectTweener__Play(tw); } } }
```

- **`duration` 真身形参表里没有**（签名只有 `(param_1, param_2)`）⇒ 那个 `0.3f` 是 C# 默认值。
- **没有 `SetActive` / `deactivateOnDisable`** 那一跳。
- `DoUpdate` / `AnimationFinished` 的 RVA 都是 **`0x4B33B0`**（全局共享空桩，机器码 `c2 00 00` = `ret 0`）⇒ **空方法**。

### 2·3 shader（前置 ① 的产出）
**全库只有 1 只 Coffee shader**：

| 项 | 值 |
|---|---|
| 名字 | `Hidden/UI/Default (UIEffect)` |
| 位置 | `d:/2/新解包资源/assets_full/globalgamemanagers/Shader/Shader_77.json`（`m_ParsedForm.m_Name`；**顶层 `m_Name` 恒空**，别按它找） |
| GUID | 4 个 uint = `3786818153 / 1548391642 / 1960798618 / 1897553079`；按小端逐字展开 = `6942b6e1da944a5c9a69df74b75c1a71` |
| platforms | `[4, 18]`；`m_ShaderIsBaked = True`；`m_Dependencies = []` |
| SubShader tags | `CanUseSpriteAtlas=true` · `IGNOREPROJECTOR=true` · `PreviewType=Plane` · `QUEUE=Transparent` · `RenderType=Transparent` |
| pass | **1 个**（`m_Type=0`）：`zClip=1` · `zTest=unity_GUIZTestMode` · `zWrite=0` · `cull=Off`；blend **全部按属性驱动**（`Blend [_SrcBlend][_DstBlend], [_SrcBlend][_DstBlend]`，`rtSeparateBlend=false`）· colMask `[_ColorMask]` · stencil `comp [_StencilComp]` / `pass [_StencilOp]` / 正反面 comp 默认 8 |
| 属性表（**9 个**） | `_MainTex`(Tex,def `white`) · `_Color`(Color,1,1,1,1) · `_StencilComp`(Float,8) · `_Stencil`(Float,0) · `_StencilOp`(Float,0) · `_StencilWriteMask`(255) · `_StencilReadMask`(255) · `_ColorMask`(15) · `_UseUIAlphaClip`(0) |
| 关键字（**44 个**） | 立体/UI 基础：`STEREO_INSTANCING_ON` `UNITY_SINGLE_PASS_STEREO` `STEREO_MULTIVIEW_ON` `STEREO_CUBEMAP_RENDER_ON` `UNITY_UI_CLIP_RECT` `UNITY_UI_ALPHACLIP` `UIEFFECT_EDITOR` `SOFTMASK_EDITOR`；色调：`TONE_GRAYSCALE` `TONE_SEPIA` `TONE_NEGATIVE` `TONE_RETRO` `TONE_POSTERIZE`；颜色：`COLOR_FILTER`；采样：`SAMPLING_BLUR_FAST` `SAMPLING_BLUR_MEDIUM` `SAMPLING_BLUR_DETAIL` `SAMPLING_PIXELATION` `SAMPLING_RGB_SHIFT` `SAMPLING_EDGE_LUMINANCE` `SAMPLING_EDGE_ALPHA`；转场：`TRANSITION_FADE` `TRANSITION_CUTOFF` `TRANSITION_DISSOLVE` `TRANSITION_SHINY` `TRANSITION_MASK` `TRANSITION_MELT` `TRANSITION_BURN` `TRANSITION_PATTERN` `TRANSITION_BLAZE`；描边：`EDGE_PLAIN` `EDGE_SHINY`；细节：`DETAIL_MASKING` `DETAIL_MULTIPLY` `DETAIL_ADDITIVE` `DETAIL_SUBTRACTIVE` `DETAIL_REPLACE` `DETAIL_MULTIPLY_ADDITIVE`；目标色：`TARGET_HUE` `TARGET_LUMINANCE`；渐变：`GRADATION_GRADIENT` `GRADATION_COLOR2` `GRADATION_COLOR4`；软遮罩：`SOFTMASKABLE` |
| 真实 uniform（DXBC 扫出来的 **41** 个） | `_MainTex` `_MainTex_ST` `_MainTex_TexelSize` `_Color` `_TextureSampleAdd` `_ClipRect` `_UIMaskSoftnessX/Y` `_ScreenParams` `_CanvasToWorldMatrix` `_RootViewMatrix` `_MirrorRootViewMatrix` `Tone*`：`_ToneIntensity`；`Color*`：`_ColorFilter` `_ColorIntensity` `_ColorValue` `_ColorGlow`；`_SamplingScale`；`Transition*`：`_TransitionRate` `_TransitionColor` `_TransitionColorFilter` `_TransitionColorGlow` `_TransitionReverse` `_TransitionSoftness` `_TransitionWidth` `_TransitionAutoPlaySpeed` `_TransitionTex` `_TransitionTex_ST` `_TransitionTex_Speed`；`_Shadow*`：`_ShadowColor` `_ShadowColorFilter` `_ShadowColorGlow`；`Edge*`：`_EdgeColor` `_EdgeColorFilter` `_EdgeColorGlow` `_EdgeShinyAutoPlaySpeed` `_EdgeShinyRate` `_EdgeShinyWidth` `_EdgeWidth`；`_Time` + `UnityPerCamera` |
| 工具命令 | `dump_shader_blob.py "UIEffect"` 直接命中（该工具 2026-10-25 已补内置文件读取，注释里就点了这一只） |

⚠️ 这份 shader 是 **Coffee v5 式「所有滤镜合并进一只」**（DISsolve / Shiny / Gradient 都成了关键字），
与 G12 在账 455 备注里的判断一致。

### 2·4 Coffee 怎么找 shader
`MaterialRepository.<Get>b__5_0` = `Shader.Find(x)` → `new Material(shader)`；
`UIEffectBase.GetModifiedMaterial`：

```
Shader shader = baseMaterial.shader;
if (registry != null) { var s2 = registry.FindOptionalShader(shader, "(UIEffect)", "Hidden/{0} (UIEffect)", name);
                        if (s2) material.shader = s2; }
```

⇒ **`Shader.Find("Hidden/UI/Default (UIEffect)")` 返回 null 时会安静地退回底材质**（= 关键字全不生效）。
这就是必须在工程里有一只同名 shader 的原因。

### 2·5 7 实例 × `uiEffects`（前置 ② 的产出）

7 个实例的 `m_Script` **全同** = `m_FileID 1 / m_PathID -4976365502517319154`；
全部落在 `bundle_menus_assets_all`（`grep -rl '"uiEffects"'` 全库 **7** 命中，无遗漏）。

| # | 节点路径（到 GameObject） | `uiEffects` 项数 | 指向的组件 | 组件 `enabled` | 该组件上的参数 | 同节点 `UIEffectTweener` |
|---|---|---|---|---|---|---|
| A | `Ranked Army Selector Container V2/Featured Icon` | **0（空）** | —— | —— | —— | 无 |
| B | `Army Item Button No Arrow Variant/Icon` | 1 | `MonoBehaviour_-1699783776568583856` = `Coffee.UIEffects.UIEffect` | **0** | 见 2·6 | 有（`-5298412877758637744`） |
| C | `Campaign Army Item Button/Icon` | 1 | `MonoBehaviour_-915853924510394678` = `UIEffect` | **1** | 见 2·6 | 有（`669576846626199242`） |
| D | `Army Item Button/Icon` | 1 | `MonoBehaviour_-1357910490681541691` = `UIEffect` | **0** | 见 2·6 | 有（`1842338237989878725`） |
| E | `Ranked Army Selector Container V2/Army Icon` | **0（空）** | —— | —— | —— | 无 |
| F | `Deck Editing Menu/Content Area/Sidebar/Footer/Done` | 1 | `MonoBehaviour_6027968899341807396` = `UIEffect` | **1** | 见 2·6（**有一处不同**） | 有（`-1237005647651475676`，**WrapMode=Loop**） |
| G | `Forge Army Item Button/Icon` | 1 | `MonoBehaviour_-1549575033197389911` = `UIEffect` | **0** | 见 2·6 | 有（`-6269761728555214935`） |

🔑 **5 个 `uiEffects[]` 指向的组件全都挂在「和 toggler 同一个 GameObject」上**（`m_GameObject` 逐条相等），
印证「`uiEffects[]` 只装本节点的那几件、不递归子件」。
🔑 **A / E 两处原版就是空数组** ⇒ 原版那两处 `Toggle` 什么都不做（上一版外壳会对它们报警，见 §七·2）。

### 2·6 5 个 `UIEffect` 实例的 81 个字段（逐实例比完，只有 2 处不同）

字段表 = `Coffee.UIEffects.UIEffect`（`dump.cs:1067800-1067900`，81 个序列化字段）。

| 字段 | 5 个实例的值 | 含义（枚举判据：`dump.cs:1067101-1067285`） |
|---|---|---|
| `m_ToneFilter` | 0 | `ToneFilter.None` |
| `m_ToneIntensity` | 1.0 | —— |
| `m_ColorFilter` / `m_ColorIntensity` / `m_Color` / `m_ColorGlow` | 0 / 1.0 / (1,1,1,1) / 0 | `ColorFilter.None` |
| `m_SamplingFilter` / `m_SamplingIntensity` / `m_SamplingWidth` / `m_SamplingScale` | 0 / 0.5 / 1.0 / 1.0 | `SamplingFilter.None` |
| **`m_TransitionFilter`** | **4（5 个全一样）** | **`TransitionFilter.Shiny`** ← **唯一真在用的滤镜** |
| `m_TransitionRate` | 0.0 | 由 `UIEffectTweener` 播 0→1 |
| `m_TransitionReverse` | 0 | —— |
| `m_TransitionTex` | 指向 `m_FileID 14 / m_PathID -6023190078349834598` | Shiny 用的贴图（外部位图，本批未解） |
| `m_TransitionTexScale/Offset/Speed` | (1,1) / (0,0) / (0,0) | —— |
| `m_TransitionRotation` | 120 | —— |
| `m_TransitionKeepAspectRatio` | 1 | —— |
| `m_TransitionWidth` / `m_TransitionSoftness` | 0.25 / 0.5 | —— |
| `m_TransitionRange` | `{m_Min:0, m_Max:1}` | —— |
| `m_TransitionColorFilter` | **6（5 个全一样）** | `ColorFilter.MultiplyAdditive` |
| **`m_TransitionColor`** | **4 个 = (1,1,1,1)；F（Done）那颗 = (3.4822, 3.4822, 3.4822, 1)** | **唯一的实例级差异** |
| `m_TransitionColorGlow` / `m_TransitionPatternReverse` / `m_TransitionAutoPlaySpeed` | 0 / 0 / 0.0 | —— |
| `m_TransitionGradient` | 4 色键全白 1 | —— |
| `m_TargetMode` / `m_TargetColor` / `m_TargetRange` / `m_TargetSoftness` | 0 / (1,1,1,1) / 0 / 0 | `TargetMode.None` |
| `m_BlendType` / `m_SrcBlendMode` / `m_DstBlendMode` | 0 / 0 / 0 | `BlendType.Custom` |
| `m_ShadowMode` / `m_Shadow*` | 0 / 默认 | `ShadowMode.None` |
| `m_GradationMode` / `m_Gradation*` | 0 / 默认 | `GradationMode.None` |
| `m_AllowToModifyMeshShape` | 0 | —— |
| `m_EdgeMode` / `m_Edge*` | 0 / 默认 | `EdgeMode.None` |
| `m_PatternArea` / `m_DetailFilter` / `m_Detail*` | 0 / 0 / 默认 | 全 None |
| `m_CustomRoot` | null | —— |
| `m_Flip` | 0 | —— |
| `m_Enabled` | B/D/G = **0**；C/F = **1** | 出厂态（toggler 会覆盖） |

**5 个 `UIEffectTweener` 实例的参数（5 个完全一致，只有 F 的 WrapMode 不同）**：

| 字段 | 值 | 含义 |
|---|---|---|
| `m_CullingMask` | **367** = 1+2+4+8+32+64+256 | Tone · Color · Sampling · Transition · GradiationOffset · GradiationRotation · EdgeShiny |
| `m_Direction` | 0 | `Forward` |
| `m_Curve` / `m_ReverseCurve` | 两个关键帧 (t0,v0) → (t1,v1)，斜率 1/1 | **线性 0→1** |
| `m_SeparateReverseCurve` | 0 | —— |
| `m_Delay` / **`m_Duration`** / `m_Interval` | 0 / **1.0** / 0 | **播 1 秒** |
| `m_PlayOnEnable` / `m_ResetTimeOnEnable` | 0 / 1 | 不自动播；进场重置时间 |
| **`m_WrapMode`** | **B/C/D/G = 0（Once）；F = 1（Loop）** | **只有 Done 那颗循环** |
| `m_UpdateMode` | 0 | `Normal` |
| `m_OnComplete` / `m_OnChangedRate` | 无持久调用 | —— |

⇒ **一句话**：原版这 7 处 = 「按钮/图标上叠一层 **Shiny 扫光**，开的时候扫一次（Done 那颗一直扫）」。
**唯一真在用的滤镜 = `TRANSITION_SHINY`** ⇒ 将来若要自写等价 shader，**只需做这一个关键字**（G12 估 2–5 天是「全滤镜」的价，不是这个价）。

---

## 三、没查清的部分（「查不到 + 还差什么」）

1. **`m_TransitionTex` 是哪张图** —— `m_FileID 14 / m_PathID -6023190078349834598` 在 `bundle_menus_assets_all`
   的 externals 第 14 条上，本批**没解**（要按 externals 下标找到对应 CAB 再取）。**差什么**：一步 externals 解析。
   ⚠️ 它决定 Shiny 的「擦除形状」，**自写 shader 之前必须先解出来**。
2. **7 实例在「我们工程」里对应哪个节点** —— 只查了原版侧。我们侧对应窗（`DeckEditingWindow` / `CampaignTab` /
   `ForgeTab` / `CollectionWindow` / 排位选军队窗）**当前归 W6 / W7 写手**，本代理不越界 ⇒ 只读也留到同步点后。
3. **dll 到底能不能跑** —— 结构上能编能 new，但 IL 残缺（§六·3）。**差什么**：调度台的 Unity 批处理跑一次
   `AddComponent<UIEffect>()` + 设 `transitionFilter=Shiny` + `Play()`，看有没有 `InvalidProgramException`。
4. **`uiEffects` 字段是数组还是 `List<T>`** —— `dump.cs:…` 写 `UIEffect[] uiEffects`；但 `.c` 的访问形状
   （长度 +0x18、元素 +0x20+i*8）**数组与 `List<T>` 同形**，从机器码分辨不出。我们按本目录同族惯例用
   `List<…>`（`UIEffectTogglerImage` / `UIEffectTogglerCanvasGroupToggler` 都把原版数组写成 `List<GameObject>`）。
   语义无差（`Toggle` 只按索引遍历）。
5. **G12 说「引用 Coffee 的类是 9 个」** —— 本批只复核了 `UIEffectTogglerCoffeeEffects` 这一个（就是 7 实例那个）。
   其余 8 个**没核**（不在本域）。**差什么**：全库 `grep -l Coffee.UIEffects` 的交集。

---

## 四、改动清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `Assets/ThirdParty/` | **新建** | 工程第一个 `ThirdParty` 目录（此前 `find Assets -name "*.asmdef"` = 0、无该目录） |
| `Assets/ThirdParty.meta` | 新建 | 目录 meta（GUID 新生成） |
| `Assets/ThirdParty/Coffee/` + `.meta` | **新建** | —— |
| `Assets/ThirdParty/Coffee/Coffee.UIEffect.dll` | **新增 329,216 B** | 来源见 §六 |
| `Assets/ThirdParty/Coffee/Coffee.UIEffect.dll.meta` | 新建 | `PluginImporter`，`Any: enabled 1` + `Editor: enabled 1`（照 `Plugins/Demigiant/DOTween/DOTween.dll.meta` 的格式） |
| `Assets/ThirdParty/Coffee/NOTICE.md` | 新建 | 来源 / 许可 / 已知缺陷 / 怎么删，给下一个人看 |
| `Assets/CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` | **重写**（94 → 106 行，LF 不变） | 外壳 → 1:1 端口；见 §五 |

**没有动**：任何 `*.unity` · `ProjectSettings/` · `Deployment`/`asmdef`（工程本来就没有）· 别人的 `.cs` · 账主表 · 正本。

---

## 五、验收（本代理能做的都做了）

1. **`TMPDIR=/tmp/wf_w5 bash d:/4/Unity/工具/typecheck.sh`** → **运行时 1 条 / 编辑器 62 条**。
   - 运行时那 1 条 = `UIEffectTogglerCoffeeEffects.cs(59,21): error CS0246: 找不到类型或命名空间名“Coffee”`
     ⇒ **纯属「csproj 还没登记新 dll」**（`gen_csc_rsp.py` 的引用清单来自 `Assembly-CSharp.csproj` 的
     `<HintPath>`，而 csproj 是**上次 Unity 生成**的，新 dll 要等 Unity 导入才会进去）。
   - 编辑器那 62 条 = **级联**：编辑器程序集拿 `$TMP/wfcheck/WFCheck.dll` 当 `Assembly-CSharp`，
     而运行时那一趟**没编成**（上面那条 CS0246）⇒ 用的是**上一次的旧 `WFCheck.dll`**，于是
     `CardDef` 里找不到 `CountSetSpec`、`VfxMap` 里找不到 `ResolvedVfx`（其实引擎线早就加过）。
     判据：把这些条目逐个看，**没有一条**提到本批的文件；§五·2 那一趟（重编 WFCheck 之后）编辑器 **0 错误**。
     ⇒ **不是本批的代码错**。
2. **手工补引用后复编（同一个 rsp，只多一条 `-r:`）**：
   ```
   dotnet ".../Roslyn/bincore/csc.dll" "@wf_csc.rsp"       -r:"D:\4\Unity\MyGame\Assets\ThirdParty\Coffee\Coffee.UIEffect.dll"  → 错误数: 0
   dotnet ".../Roslyn/bincore/csc.dll" "@wf_csc_editor.rsp" -r:"D:\4\Unity\MyGame\Assets\ThirdParty\Coffee\Coffee.UIEffect.dll"  → 错误数: 0
   ```
   ⇒ **我们的代码对这份 dll 是编得过的**，两边都是 0。
3. 🔴 **待调度台跑一次 Unity 批处理**（本代理不许跑 Unity）：
   - 目的 A：让 Unity 导入 dll 并**重生成 `.csproj`** ⇒ 之后 `typecheck.sh` 应回到 **0/0**（不需要再手工加 `-r:`）。
   - 目的 B：验 dll 的 IL 是否真能跑（§三·3）。**建议的最小探针**：
     `var go = new GameObject(); var g = go.AddComponent<UnityEngine.UI.Image>();`（要挂 Canvas）
     `var ue = go.AddComponent<Coffee.UIEffects.UIEffect>(); ue.transitionFilter = TransitionFilter.Shiny; ue.enabled = true;`
     ⇒ 不抛 `InvalidProgramException` / `TypeLoadException` 才算这份 dll 可用。
   - 目的 C：确认 shader 缺口的实际表现（`Shader.Find(...)` 为 null ⇒ 材质退回 `UI/Default`，画面无扫光）。

---

## 六、来源与合规

### 6·1 是什么
`Coffee.UIEffect` = 原版引用的第三方 uGUI 材质滤镜包（作者 **mob-sakai / Coffee**，
Asset Store 商品；不是 MIT 那个老开源仓库）。原版只用到其中的 `UIEffect` + `UIEffectTweener`。

### 6·2 本机 5 份候选，为什么选这一份

| 路径 | 大小 | 引用 | 方法体 | 判定 |
|---|---|---|---|---|
| `d:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll` | 329,216 | `mscorlib`+Unity | **有**（ILSpy 能吐 C#，带 Cpp2IL 噪声） | ✅ **采用** |
| `…/MelonLoader/Il2CppAssemblies/Il2CppCoffee.UIEffect.dll` | 348,672 | **`Il2Cppmscorlib`**+`System.Runtime` | 有（代理调用原生） | ❌ 离开 il2cpp 运行时跑不了 |
| `…/MelonLoader/Dependencies/…/Cpp2IL/cpp2il_out/Coffee.UIEffect.dll` | 156,160 | `mscorlib`+Unity+`Cpp2ILInjected` | 未验（`OnEnable` 没匹配到，大小只有采用那份的一半） | ❌ 不优于采用那份 |
| `d:/2/tools/cpp2il/out_cpp2il/Coffee.UIEffect.dll` | 53,248 | `mscorlib`+Unity | **全空** | ❌ |
| `d:/2/tools/assetripper_out/AuxiliaryFiles/GameAssemblies/Coffee.UIEffect.dll`（与 `ExportedProject/Assets/Plugins/` 同一份） | 60,928 | `mscorlib`+Unity | **全空**（`UIEffect.OnEnable(){}`、`OnDisable(){}`，`uiEffects` 丢空） | ❌ |

⚠️ **两处 MelonLoader 目录都不是原作者发行物**：它们都是 **MelonLoader / Cpp2IL 从 `GameAssembly.dll` 重建**出来的。

### 6·3 许可
- 本机 Coffee 相关目录下**没有 `LICENSE` / `NOTICE`**（`find d:/2/unity_run_ref/MelonLoader -iname "*licen*" -o -iname "*notice*"` 只命中 MelonLoader 自己的两份）。
- 商品包 ⇒ **默认不可再分发**。本项目**个人学习用途** + **用户 2026-10-27 明确批准「要搬」** ⇒ 保留。
- ⛔ **将来要对外发布 / 上传，这条必须重新评估**（和 `Resources/Art/` 那条一起）。

### 6·4 这份 dll 的残缺（**必读**）
Cpp2IL 的 IL 恢复在它身上**大面积失败**：

- **自定义特性全被剥**：dll 字节里搜 `SerializeField` / `ExecuteAlways` / `DisallowMultipleComponent` /
  `RequireComponent` / `Serializable` / `AddComponentMenu` = **0 命中**
  ⇒ `UIEffect` 的 81 个 `protected/private` 字段**不会被 Unity 序列化**（Inspector 里配不了）；
  `[RequireComponent(typeof(UIEffectBase))]`（`UIEffectTweener`）也没了。
- **自启钩子随之失效**：`ImageProxy.InitializeOnLoad` / `TmpProxy.InitializeOnLoad` /
  `TmpProxy.RuntimeInitializeOnLoadMethod` 本体在，但**没有 `[RuntimeInitializeOnLoadMethod]`** ⇒ 永不执行
  ⇒ `GraphicProxy.Register(...)` 不跑 ⇒ Coffee 认不出 `Image` / `TMP_Text`。
  （**好消息**是：因此这份 dll 落进工程**不会**在开机时自动跑坏代码。）
- **IL 类型错乱**：88 个反编译文件里 **1,430 处** `Cpp2ILHelpers.NoteDecompilerIssue(...)`（callee 靠猜）
  + **~800 条** `//IL_xxxx: Expected Obj/I/F4/VT …`。
- **核心方法已坏**：`UIEffectTweener.Play()`（8 条类型错乱）· `ImageProxy.IsValid`（`if (0 == (nint)typeof(Image)) return true;`）·
  `UIExtraCallbacks.InitializeOnLoad`（`Canvas.willRenderCanvases += x` 变成 `-= x`，两个分支都置 `false`）。

⇒ 结论：**能编译、能 reference、能 new，但跑起来不可信。** 已在 `NOTICE.md` 里写死。

---

## 七、顺手发现

1. **上一版外壳有一个语义错**（本批已改）：`q.enabled = option` 打在 **`ImageQuad`** 上 ⇒ 「关」= 把图标整个隐藏，
   而原版关的是 **Coffee 的滤镜组件**（图还在）。这条不是「缺视觉」，是**画错了**。
2. **上一版外壳的告警会误报**：`uiEffects` 为空时 `WarnMissingVisualBody("（uiEffects 是空的）")` ——
   而原版 **7 个实例里有 2 个（`Featured Icon` / `Army Icon`）本来就是空数组** ⇒ 那两处是**正常**。
   本批已去掉（改为「shader 缺」才出声，且一次性）。
3. **账 453 / 454 的落点早就漂了**：主表写 `UIEffectTogglerCoffeeEffects.cs:37` / `:41`，
   而**改动前**的文件里这两个方法已经在 **76 / 79**；本批重写后在 **86 / 89**。
   补丁里已把落点写成新行号（`_parts/B9_W5COF.tsv`），调度台合并后请跑一次
   `check_ledger_points --all-A --fix` 收尾（⚠️ 只能在「没有 `.cs` 写手」时跑）。
4. **`Coffee.UIEffect.R.dll` 是空壳**（image 127 只有 2 个 TypeDefIndex：`<Module>` + 一个空接口）
   ⇒ 以后别再为它花时间。
5. **`UIEffectTogglerCoffeeEffects` 目前零调用者**（`grep -rn` 全工程只命中它自己）⇒
   本批重写**运行时零风险**，同时也说明这条线此前一直没接上。
6. **只用到一个滤镜**：5 个实例的 `m_TransitionFilter` 全是 **4 = Shiny**，其余滤镜全 None
   ⇒ 将来若走「自写等价 shader」，**只需要 `TRANSITION_SHINY` 一支**（不是全 9 种）。
7. **shader 只此一只**：`assets_full` 234 个 shader 里，名字含 uieffect/uidissolve/uishiny/uitransition/uigradient/coffee
   的**只有 `Hidden/UI/Default (UIEffect)`** ⇒ Coffee v5 已经把 Dissolve/Shiny/Gradient 合并成关键字了，
   以后不要再按 v3 时代「一堆 `Hidden/UI/Default (UIDissolve)` 之类」去找。
8. **7 个实例的宿主全在 `bundle_menus_assets_all`**（不是 battleprefabs）⇒ 这批是**纯界面**效果，
   与战斗 HUD 无关；Battle 那批 uGUI 反而没有 Coffee 实例。

---

## 八、给下一个代理（交接）

- 本件已做完、无半成品：`UIEffectTogglerCoffeeEffects.cs` 编得过（手工 `-r:` 0/0）。
- **下一件**（按价值排序）：
  ① 调度台跑一次 Unity：重生成 csproj（让 typecheck 回 0/0）+ dll 可用性探针（§五·3）。
  ② 解 `m_TransitionTex`（§三·1，一步 externals 解析）。
  ③ **产品级决定**：对应节点要不要改成 uGUI `Image`（能直接用 Coffee），还是给 `ImageQuad` 自写等价滤镜
     （只需 `TRANSITION_SHINY`）。**这是决定这条线能不能落地的分叉点，⛔ 别在没定之前动 7 个宿主文件。**
  ④ shader 载体：照账 455 备注里 G12 的 `extract_mirror_shaders.py` 套路把原件烘进 `wf_*_shaders*.bundle`。
- 踩过的：**别按 `m_Name`（顶层）找内置 shader** —— 它恒空，真名在 `m_ParsedForm.m_Name`；
  `dump_shader.py` **扫不到** `globalgamemanagers` 里的 shader（它的 bundle 名单里没有），要用 `dump_shader_blob.py`。
