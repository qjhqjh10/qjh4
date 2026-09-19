# 卡面那层 SDF 软光/影（`Card Highlight And Shadow`）—— 查证定案（2026-09-19）

> **一句话**：原版卡面最底层那层 4.4281² 的软光/影，**不是运行时生成的、也不是 `CardHighlight` 组件管的** ——
> 它是**预生成的 SDF 贴图**（`40k_Cardframe_{troop|stratagem}_<阵营>_SDF_tier{1..4}`，**104 张**，本地解包资源里就有），
> 由一个 Shader Graph shader 渲，状态只改 `_Outline.rgb`（高亮）与 `_ShadowColor.a`（阴影），**补间 0.2s**。
>
> 关联：`资料/战斗UI_原版对账表.md:187`（原版节点数值）· `Core/CardHighlight.cs`（那 5 个**状态色**是**另一套东西**）。
> ⚠️ **本轮纠正了两处旧说法**（见 §五）。

---

## 一、那层是什么、谁在管

| 项 | 值 | 出处 |
|---|---|---|
| 节点 | `Card Highlight And Shadow`（父 = `Front`），**4.4281×4.4281** @ y = **−0.01265**，是 `Front` 的**最底层**（在立绘之下） | `bundle_battleprefabs_vfxandmisc_assets_all/GameObject/Card Highlight And Shadow.json` · `RectTransform/RectTransform_6990339727227853760.json:35-42` |
| 组件 | RectTransform + CanvasRenderer + `UnityEngine.UI.Image` + **`UIImageMaterialColorChanger`** | 同上 `:2-27` |
| 🔴 **没有的** | **该节点上没有 `CardHighlight` 组件** | 同上 |
| 谁给它 sprite | `CardTierUIController.SetTier` → `CardFramesSO.GetClanFrame(...)` 取 `(frame, highlight)` → 写 `Card2DController.frame`(0x38) 与 **`frameHighlightAndShadow`(0x40)** | `CardTierUIController__SetTier.c:12,18,20` · `BasicCardUI__SetRawCardData.c:88` |
| sprite 来源 | `CardFramesSO.TierCardFrameAddressableReference.troopHighlightFrame`(0x28) / `stratagemHighlightFrame`(0x18) —— **Addressables 资产**（按卡类型选：`cardTypeOptions ∈ {0,10}` → troop，`==20` → stratagem） | `dump.cs:50840-50850` · `CardFramesSO.TierCardFrameAddressableReference__GetSprite.c:22-46` |
| ⚠️ 判读坑 | `Card2DController.SetFrame(Sprite,Sprite)` **全库无调用者**（被内联进 `SetTier`）—— 别拿「没调用者」当「没在用」 | — |

## 二、sprite 与 shader

- **资产名 = `40k_Cardframe_{troop|stratagem}_<Faction>_SDF_tier{1..4}`**，共 **13 阵营 × 4 tier × 2 类型 = 104 张**
  （与 `CardFramesSO` 的 `clanFrames` 13×4 逐项吻合）。配置源 = `bundle_staticgeneralassets_assets_all/MonoBehaviour/CardFramesByArmy.json`。
- 规格（实读 `.../Sprite/40k_Cardframe_troop_BlackLegion_SDF_tier1.json`）：贴图 **128×128** · sprite 子矩形 **81×110**（BlackLegion 79×107）·
  `m_PixelsToUnits = 100` · pivot 0.5 ⇒ 世界尺寸 ≈ **0.81×1.10**（**不是** 682×998）。
- **另有一张通用的**：`bundle_battleprefabs_vfxandmisc_assets_all/Texture2D/**Card board frame SDF.png**`（128×128，白环黑底）—— 可先拿它顶上。
- **shader = `Everguild/FX/Card Highlight And Shadow`**（Shader Graph，PathID `−3135600060561652991`，`bundle_shaders_assets_all/Shader/`）。
  材质 `Card Frame SDF`（PathID `−3316280387615011577`，`bundle_duplicateassetisolation_assets_all/Material/`）。
- 它的 18 个属性（默认值实读）：`_MainTex`(white) · `_ALPHACHANNEL`(R/A) · `_Scale` · `_Offset_Shadow` ·
  **`_ShadowColor`(0,0,0,0.604)** · `_SpriteAlphaEdge_Outer` · `_Outer_Edge` · `_Outer_Fallof` · `_Offset_Outline` ·
  **`_Outline`(材质里 alpha = 0)** · `_SpriteAlphaEdge_Inner` · `_Inner_Edge` · `_Inner_Fallof` · `_Noise` ·
  `_Noise_Speed_Scale_StepLow_StepHigh` · `_NoiseMaskScale` · `_NoiseIntensity` · `_NOISE_CHANNEL`(RGBA 四选一)。
  ⚠️ 材质上还带一堆 TMP 遗留属性（`_FaceColor`/`_GlowColor`/`_Bevel`…）⇒ **作者是拿 TMP SDF 模板改的**。`_MainTex` 在材质里是空的，运行时由 Image 的 sprite 供图。
- **卡背那层 `Cardback Shadow SDF` 用的是同一个 shader**（材质 `Card Backs SDF`）⇒ 卡面/卡背同一套机制，只有贴图不同。

## 三、状态怎么驱动（**与那 5 个状态色无关**）

- 属性 ID 是编译期常量：`cardShadowMaterialID`(0x80) = `Shader.PropertyToID("_ShadowColor")` ·
  `cardHighlightMaterialID`(0x84) = `Shader.PropertyToID("_Outline")`（`Card2DController..ctor.c:12,14`）。
- `ChangeHighlightColor(color, isCardback, instant)` → `UIImageMaterialColorChanger.DoColorChange(color, "_Outline", time, instant)` → `Material.SetColor`
  （`Card2DController__ChangeHighlightColor.c` · `UIImageMaterialColorChanger__SetColor.c`）；阴影同理走 `"_ShadowColor"`。
- **显隐**：`ToggleHighlight(bool on,…)` 取 `_Outline` 当前的 rgb、**只换 alpha** 再补间；`ToggleCardShadow` 对 `_ShadowColor` 同样处理。
- 补间：`UIImageMaterialColorChanger.Update` 按 `currentTime/timeToChange` 插值，播完自禁用；
  **时长常量 `HIGHLIGHT_COLOR_CHANGE_TIME = 0.2f`**。`DoColorChange` 首次会 `new Material(image.material)` 造实例（不污染共享材质）。
- 战斗里的调用者：`BattleCardUI.ChangeToHighlightColor()`（按 trait 分支选色）· `ChangeHighlightToSelectColor()`（按 rarity）·
  `SetObjectVisibility` 里的 `ToggleHighlight` · `CardScript.ShowCardLight()`。

## 四、我们这侧要复刻的话（清单）

1. **导 SDF 贴图**：104 张里按「我们卡池用到的阵营 × tier × 卡类型」导（或先只导通用那张 `Card board frame SDF.png` 顶上）。
   出处目录 `d:/2/新解包资源/assets_full/bundle_<阵营小写>cardassets_assets_all/{Sprite,Texture2D}/`。
2. **写等价 shader**：用到的属性 = `_ShadowColor` / `_Offset_Shadow` / `_Outline` / `_Offset_Outline` /
   `_Outer_*` / `_Inner_*` / `_SpriteAlphaEdge_*` / `_Scale` / `_ALPHACHANNEL`（噪声那 5 个可先不做）。
3. **接层**：UI/世界空间一块 **4.4281²** 的 quad，放在 `Front` 的**最底层**（立绘之下），sprite 按 (阵营, tier, 卡类型) 取。
4. **驱动**：状态只改 `_Outline.rgb`（高亮）与 `_ShadowColor.a`（阴影），**补间 0.2s**。
   ⚠️ **别拿 `Core/CardHighlight.cs` 那 5 个状态色去喂它** —— 那是原版**另一个组件**（`CardHighlight`）喂**另一个对象**
   （`FrameHighlight` SpriteRenderer，PathID 165624494667373504）的，两套互不相干。

## 四、✅ 已实现（2026-09-19 当晚接上）

| 做了什么 | 落在哪 |
|---|---|
| 导 104 张 SDF 贴图 + 通用那张进工程 | `工具/import_original_card_sdf.py` → `Resources/Art/card_sdf/`（**gitignore 的本地件**，与 `cards/frame_*` 同规矩） |
| `CardArt.Sdf(faction, rarity, tactic)` —— 与 `Frame()` **同一套命名与逐级退回** | `Core/CardArt.cs` |
| 卡面**最底层**加这层：**4.4281²** @ y **−0.0126**、z 比立绘还靠后 | `Core/CardView.cs`（`ShadowMesh` / `ShadowSize` / `ShadowY` / `ShadowZ`） |
| 材质用**原版 shader**（就在随包的 `wf_shaders.bundle` 里，跟 E 组那 8 个内置 shader 同一条路）+ **照抄原版材质 `Card Frame SDF` 的全部属性值** | `CardView.SdfMaterial()` |
| 🔴 这层**不参与整卡着色**（它的颜色是 `_ShadowColor`/`_Outline` 说了算），只跟卡的淡出 | `CardView.ApplyTint()` 里那段「同理」 |
| 断言 4 条（层在、4.4281²、原版 shader、`_Outline` alpha = 0） | `Editor/BattleScene.cs`（**612 通过 / 0 失败**） |

**踩到并修掉的一条**：第一版直接用 shader 默认值建材质 ⇒ shader 默认的 `_Outline` 是**不透明的白**，
每张卡多出一圈**白框**（截图里一眼可见）。原版**材质**把它的 alpha 设成 **0**（平时不描边）——
所以「用原版 shader」不等于「照原版的样子」，**材质上的值必须一起抄**（属性表见 §二）。

**还没做的**：`_Outline` 的**高亮描边**（原版由 `BattleCardUI.ChangeToHighlightColor()` 按 **trait**、
`ChangeHighlightToSelectColor()` 按 **rarity** 选色，`ToggleHighlight` 开关、补间 0.2s）——
我们**暂时仍用自己那圈羽化描边**（`SoftRimTexture`）表达状态色。
⇒ 下一步要做的是**把那个 trait/rarity → `_Outline` 颜色的映射查出来**，然后把我们的描边换成它。

## 五、本轮纠正的两处旧说法

| 旧说法（在哪） | 实际 | 错因 |
|---|---|---|
| 「它的 `Image` 没有 sprite（PathID 0）—— **sprite 运行时由 `CardHighlight` 组件生成**」（`战斗UI_原版对账表.md:187`） | **不是生成、是 Addressables 预生成的 SDF 资产**；该节点上**根本没有 `CardHighlight` 组件** | 当时只看到 Image 的 sprite 是空的，就归给了同名组件 |
| `Core/CardHighlight.cs` 头注释里「原版卡面最底层那层 SDF 软光/影 … 生成规则在查证中」 | 生成规则 = **没有生成规则**（离线烘好的 128² 灰度图），且**状态色与那 5 色无关** | 同上 |

## 六、出处

- 反编译：`CardTierUIController__SetTier.c` · `BasicCardUI__SetRawCardData.c` · `CardFramesSO.TierCardFrameAddressableReference__GetSprite.c` ·
  `Card2DController__{ChangeHighlightColor,ChangeShadowColor,ToggleHighlight}.c` · `Card2DController__.ctor.c` ·
  `UIImageMaterialColorChanger__{DoColorChange,SetColor,Update}.c` · `CardScript__ShowCardLight.c` · `BattleCardUI__ChangeToHighlightColor.c`
- 字段/常量：`D:/2/tools/il2cpp_out/dump.cs:21281-21295`（CardTierUIController）· `:50840-50850`（CardFramesSO）· Card2DController 段（`HIGHLIGHT_COLOR_CHANGE_TIME`）
- 资产：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`（节点 + 通用 SDF 贴图）·
  `bundle_<阵营>cardassets_assets_all/`（104 张 SDF）· `bundle_staticgeneralassets_assets_all/MonoBehaviour/CardFramesByArmy.json` ·
  `bundle_shaders_assets_all/Shader/Shader_-3135600060561652991.json` · `bundle_duplicateassetisolation_assets_all/Material/`
