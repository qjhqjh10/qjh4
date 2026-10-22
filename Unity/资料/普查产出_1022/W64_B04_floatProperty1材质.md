# W64_B04 — `Card Hand To Board` 的 `floatProperty1` 到底作用在哪张材质上

> 只读侦查（未跑 Unity、未动 git、未改源码/资产）。日期 2026-10-22。
> 判据一手来源 = `d:/2/新解包资源/assets_full/`（序列化 JSON）＋ `d:/2/tools/decomp_full/`（方法体）
> ＋ `d:/2/tools/assetripper_out/ExportedProject/`（GUID / 预制体 / 同名材质资产，供交叉验证）。

---

## 〇 一句话结论

**`floatProperty1` 不是 MeshRenderer 的材质槽，而是 `2DCard/Front/CardFrame` 这个 GameObject 上
`AnimateUIMaterialProperty`（MonoBehaviour，classID 114）的一个序列化字段**。
它运行时把 **`CardFrame` 的 UGUI `Image`** 的材质换成 **`SpriteUI Frame Dissolve`**
（shader `Everguild/FX/Sprite Dissolve Mask`），每帧 `SetFloat("_DissolveAmount", value)`。

⇒ **`floatProperty1` 作用的材质 = `SpriteUI Frame Dissolve`**
（`bundle_battleprefabs_vfxandmisc_assets_all` · PathID `-1050090670530995784` · GUID `7e63af721a390ca4fa2f4a3aba2d2ab8`）。
⚠️ 它**不是** `Card 3d WH40K Dissolve`（那是 3D 卡体那条曲线，见 §六）。

---

## 一、clip 原文（一手：`AnimationClip_-3614292623624764332.json`）

**文件**：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3614292623624764332.json`
（`m_Name = "Card Hand To Board"` · `m_Legacy = True` · `m_SampleRate = 60` · `m_WrapMode = 1` · 14 条 `m_FloatCurves` ·
0 条 `m_PPtrCurves` · `m_ClipBindingConstant.genericBindings` = **空**）

**本任务那一条（`m_FloatCurves[8]`）原文**：

| 字段 | 值 |
|---|---|
| `path` | **`2DCard/Front/CardFrame`** |
| `attribute` | **`floatProperty1.value`** |
| `classID` | **`114`（= MonoBehaviour）** |
| `script` | `{ m_FileID: 1, m_PathID: **-1887485317901086766** }` |
| 关键帧 | `(0.0, 1.0)` · `(0.016666668, 1.0)` · `(0.69999999, 0.0)` |

**对照：同一 clip 里另外两条同一路径的曲线**

| # | path | attribute | classID | script | 关键帧 |
|---|---|---|---|---|---|
| 7 | `2DCard/Front/CardFrame` | **`m_Enabled`** | 114 | **同上 -1887485317901086766** | `(0,0) (0.0333,1) (0.75,0)`（阶跃） |
| 8 | `2DCard/Front/CardFrame` | `floatProperty1.value` | 114 | 同上 | 见上 |
| 4 | `Board Elements/3DBody/Card 3D` | **`material._DissolveAmount`** | **23（MeshRenderer）** | `{0,0}` | `(0.1667,1) (0.7,0)` |

🔴 **要点：`floatProperty1` 与 `m_Enabled` 的 `script` PPtr 完全相同** ⇒ 两者是**同一个组件**上的两个属性
（一个是组件的开关、一个是组件的序列化字段）。`m_Enabled` 那条把它**打开 0.0333 → 关 0.75**，正是溶解窗。

**`script` 落到哪**：`bundle_Waprforge_monoscripts/MonoScript/MonoScript_-1887485317901086766.json`
→ `m_Name = m_ClassName = **AnimateUIMaterialProperty**`（`m_Namespace` 空 · `m_AssemblyName = Assembly-CSharp`）。

**clip 里没有 `_DissolveAmount` 这个字符串**（除 3D 体那条 `material._DissolveAmount` 外）。
`_DissolveAmount` 只存在于**组件的序列化数据**里（§三）—— 这是上一轮「按字符串搜 clip / 按名字搜材质」搜不到的直接原因之一。

**其它曲线（同族对照用）**：`2DCard.m_IsActive` 1→0 @0.7 · `2DCard.m_Alpha`（CanvasGroup，classID 225）1→0 @0.3333→0.6667 ·
`2DCard.m_LocalPosition.z` → −0.043 @0.1667 · `CardImage` 位置/缩放（缩到 0.905 @0.55）·
`Board Elements` 位置 · `Board Elements/3DBody/Base Attack|Health Counters.alpha`（NestedFadeGroup）0→1 @0.7 ·
`ToBoardFX/Spawn Particles` 及 `…/Trail` 的 `m_IsActive`。
**动画事件**：`CardHandToBoardAnimationFinished` @ **0.55**（`m_Events[0]`）。

---

## 二、上一轮为什么「全没命中」（逐条，都有证据）

1. **读错了 `CardFrame`。** `GameObject/CardFrame.json` 有两处（全库 `GameObject/CardFrame*.json` 共 **153** 个）：
   - `bundle_menus_assets_all/GameObject/CardFrame.json` —— **4 个组件**（同目录还有 **40** 个 `CardFrame_<pid>.json` 变体，
     也全是 4 组件），父链是
     `Generic Card Selector Menu / Content / Viewport / Generic Select Card Container / CardUI / 2DCard / Front / CardFrame`
     （**菜单**的卡视图）。
   - `bundle_battleprefabs_vfxandmisc_assets_all/GameObject/CardFrame.json` —— **5 个组件**，父链
     `CardPrefab / 2DCard / Front / CardFrame` ✅ 这个才是本 clip 指的那个。
   「4 个 pathID」与 menus 那一份完全对得上（§三 表里的前 4 个），**第 5 个（本次要找的组件）只在 battleprefabs 那份里**。
   🔑 **全库普查**：153 个 CardFrame 按组件数分 = **5 个组件的 1 个 / 4 个组件的 152 个**，带
   `AnimateUIMaterialProperty` 的**只有那 1 个**（= battleprefabs 这个）⇒ 没有别的候选，不会认错。
2. **组件类型不是 `MeshRenderer`。** clip 给的是 `classID = 114 (MonoBehaviour)`；`CardFrame` 整个 GameObject
   **没有任何** `MeshRenderer` / `SkinnedMeshRenderer` / `ParticleSystemRenderer` / `MeshFilter` ——
   它是 **UGUI**（`RectTransform` + `CanvasRenderer` + `UnityEngine.UI.Image`）。
3. **UI `Image` 在这份 dump 里是 `MonoBehaviour`**（`m_Script` → `MonoScript_350208831926335389` = `UnityEngine.UI.Image`）——
   按「Image 组件」这种类型名去搜 `MonoBehaviour/` 是搜不出语义的。
4. **`m_Script` 是跨文件 PPtr**（`m_FileID: 1`）⇒ 只在 battleprefabs 包里按 pathID 找组件、或拿 `script` 号当组件号，
   都会落空；脚本本体在 `bundle_Waprforge_monoscripts` 里。
5. **`_DissolveAmount` 不在 clip 里**（见 §一末），按字符串搜 clip 只能命中 3D 体那条。

> 本轮实际搜过的范围（供复核）：
> **包**：`assets_full` 全部 90 个 bundle（含 `battleprefabs_vfxandmisc` · `menus` · `shaders` · `battlesharedresources` ·
> `duplicateassetisolation` · `scenes_scenes_mainmenuwarpforge` · 14 个 `*cardanims`）。
> **词**：`Card Hand To Board` · `floatProperty1` · `materialProperty` · `_DissolveAmount` · `SpriteUI Frame Dissolve` ·
> `Sprite Dissolve Mask` · `AnimateUIMaterialProperty` · `CardFrame` · `Noise Combined` · `-1050090670530995784` 等 pathID。
> **类型目录**：`GameObject/ Transform/ RectTransform/ MonoBehaviour/ MeshRenderer/ MeshFilter/ CanvasRenderer/ SkinnedMeshRenderer/
> ParticleSystemRenderer/ Material/ Shader/ Texture2D/ AnimationClip/ Animation/ Animator/ AnimatorController/ AssetBundle/ MonoScript/`。
> **反编译**：`d:/2/tools/decomp_full/` 的 `AnimateUIMaterialProperty__*` · `BattleCardUI__*` · `Card2DController__*` · `CardImageController__*`，
> 以及全库 grep `_DissolveAmount` / `SpriteUI Frame Dissolve` / `Sprite Dissolve Mask`（**0 命中** —— 字符串字面量不在 `.c` 里，属正常）。

---

## 三、沿路径找到的组件与材质（一手：bundle JSON）

### 3·1 层级（全部在本 bundle 内可解）

```
CardPrefab                     GameObject · Transform 5581755778927008704   ← prefab 根（普通 Transform）
├─ Board Elements              Transform  -4334370980763821120
│   └─ 3DBody / Card 3D …      （3D 卡体，MeshRenderer 走 Material `Card 3d Lvl1`）
├─ InBattleId                  RectTransform 3756633904034061248
├─ 2DCard                      RectTransform -8834377289179948096  ← Canvas（m_RenderMode=2 WorldSpace）+ CanvasGroup
│   └─ Front                   RectTransform -8828616577395090496  （6 个子件：Highlight/Shadow · CardImage ·
│       │                                                          Textbackgrounds · CardFrame · Card Info · EffectAnchor）
│       └─ CardFrame           RectTransform 4740678016171744192   ← **clip 的目标**（叶子，无子件）
└─ ToBoardFX                   Transform  1809300772220869568
```

`CardPrefab` 根组件：`Transform` · **`Animation` 6959176051360373696** · `CardScript` · `BattleCardUI` ·
5×`CardTextCountersController` · `CardHighlight`。
🔴 **`Animation` 组件的 `m_Animation` / `m_Animations` 都是 `AnimationClip_-3614292623624764332`（= 本 clip）**，
`m_PlayAutomatically = False`、`m_WrapMode = 0 (Once)`、`m_Enabled = 1` —— 这条链把 clip 与这个 prefab 钉死。

### 3·2 `CardFrame` 的 5 个组件（= 上一轮「4 个组件」的正解）

| # | PathID | 类型 | 备注 |
|---|---|---|---|
| 1 | `4740678016171744192` | RectTransform | `m_AnchoredPosition (0, -0.03)` · `m_SizeDelta (2.2452, 3.2572)` |
| 2 | `-1164999895110739008` | CanvasRenderer | |
| 3 | `7770400870796270528` | **MonoBehaviour：`UnityEngine.UI.Image`** | `m_Material` → `-5934618912107367960` = **`Card Frame and Cardback for combat`**；**`m_Sprite = null`**；`m_Type=0`、`m_RaycastTarget=0` |
| 4 | `1868992003668548544` | MonoBehaviour：`SoftMaskScript`（`UnityEngine.UI.Extensions`） | **`m_Enabled = 0`**（未启用，不会改材质）；`MaskArea` → `CardImage` 的 RT |
| 5 | **`6271754070396410816`** | **MonoBehaviour：`AnimateUIMaterialProperty`** | **本任务的主角**（见 3·3） |

### 3·3 主角组件全字段（`MonoBehaviour_6271754070396410816.json`）

```json
m_GameObject              : 7138976253573503936   (→ GameObject/CardFrame.json)
m_Enabled                 : 0                     （默认关；由 clip 的 m_Enabled 曲线打开）
m_Script                  : { 1, -1887485317901086766 }  → MonoScript `AnimateUIMaterialProperty`
image                     : 7770400870796270528   → 上面那个 UI Image
initializeOnEnable        : 1
floatProperty1            : { materialProperty: "_DissolveAmount", value: 0, useVector4: 0, vector4Value: (0,0,0,0) }
floatProperty2            : { materialProperty: "",                value: 0, useVector4: 0, vector4Value: (0,0,0,0) }
changeMaterialOnAnimation : -1050090670530995784   → Material **`SpriteUI Frame Dissolve`**
```

（与 AssetRipper 导出的 `CardPrefab.prefab:16174-16198` 逐字一致：`image` / `initializeOnEnable: 1` /
`floatProperty1.materialProperty: _DissolveAmount` / `floatProperty2` 空 / `changeMaterialOnAnimation: guid 7e63af72…`，
`m_Enabled: 0`。该 prefab 里卡框 `Image` 的 `m_Sprite: {fileID: 0}` 也是空的。）

### 3·4 卡框贴图从哪来（回答「Image 没 sprite 怎么画」）

- `2DCard` 上有 `Card2DController`（`MonoBehaviour_8218469410760203200`），其 **`frame` = `7770400870796270528`（就是这张 Image）**。
- 反编译 `Card2DController__SetFrame` = `Image.set_sprite(frame)` + `第二张 Image.set_sprite(highlightFrame)`。
- 贴图源 = `CardImageController.cardFramesSO` → **`CardFramesByArmy.asset`**（GUID `128d34144ce31c743b7ad12ccd0b72a3`）：
  `clanFrames[army].framesByTier[tier]` 下 `troopFrame` / `troopHighlightFrame` / `stratagemFrame` / `stratagemHighlightFrame`
  （全是 `UnityEngine.Sprite` 的 AssetGUID）。
⇒ **卡框 sprite 是运行时赋的**；UGUI 渲染时把它喂给材质的 `_MainTex`（shader 声明了 `_MainTex`）。
这条是「为什么材质里 `_MainTex` 是 null 但画面有框」的解释，也意味着**溶解 shader 采样的是当帧的卡框图**。

---

## 四、`AnimateUIMaterialProperty` 的运行时行为（判据：`d:/2/tools/decomp_full/AnimateUIMaterialProperty__*.c`）

| 时机 | 做什么 | 出处 |
|---|---|---|
| `OnEnable`（※ 由 clip 的 `m_Enabled` 曲线在 **0.0333 s** 触发） | `CreateInstanceMaterial()`，随后把实例材质赋给 `image.material` | `__OnEnable.c` |
| `CreateInstanceMaterial` | 先存 `originalMaterial = image.material`；**`base = changeMaterialOnAnimation != null ? changeMaterialOnAnimation : originalMaterial`**；`new Material(base)`；`name = base.name + 后缀`；`image.material = instancedMaterial` | `__CreateInstanceMaterial.c` |
| `Update`（每帧） | 对 `floatProperty1` / `floatProperty2`：名字非空时 —— `useVector4` ? `Material.SetVector(name, vector4Value)` : **`Material.SetFloat(name, value)`**（作用在 `instancedMaterial` 上） | `__Update.c` |
| `OnDisable`（※ 曲线 **0.75 s** 关掉组件时） | **`image.material = originalMaterial`**（还原成 `Card Frame and Cardback for combat`） | `__OnDisable.c` / `__ResetToOriginalMaterial.c` |
| `OnDestroy` | `Destroy(instancedMaterial)` | `__OnDestroy.c` |

⇒ **净效果**：0.0333 s 把卡框 Image 的材质换成 `SpriteUI Frame Dissolve` 的实例，0.0167→0.7 s 把
`_DissolveAmount` 从 1 推到 0（卡框被「溶解掉」），0.75 s 撤掉实例、还原原材质。
`floatProperty2` 名字为空 ⇒ 整条分支被跳过（**本 clip 没有第二条 float 曲线**）。
`changeMaterialOnAnimation` 本身**不被动画驱动**（clip 里没有它的曲线）。

---

## 五、目标材质 `SpriteUI Frame Dissolve`（一手 + 全量序列化值）

**位置**：`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1050090670530995784.json`
（AssetRipper 同名副本：`ExportedProject/Assets/Material/SpriteUI Frame Dissolve.mat`）

| 项 | 值 |
|---|---|
| `m_Name` | **`SpriteUI Frame Dissolve`** |
| GUID | **`7e63af721a390ca4fa2f4a3aba2d2ab8`** |
| `m_Shader` | PathID `7612461173376232267` → **`Everguild/FX/Sprite Dissolve Mask`**（GUID `4a10d30b57680ac409c035a973830259`） |
| `m_ValidKeywords` | **`["_DISSOLVE_CHANNEL_B"]`** |
| `_DissolveAmount` | **1.0** |
| `_DISSOLVE_CHANNEL` | **2**（= B 通道；与上面的 keyword 自洽） |
| `Vector1_2780b5644c1e463480f9eca2dba65f9a` | **0.07** ← shader 里叫 **Dissolve Borded Width**（原版拼写） |
| `Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d` | **(1.2, 1.2, 0, 0)** ← DissolveUVScale(XY) / Offset(ZW) |
| `_Color` | **(0.0588235, 0.3289232, 0.9058824, 1)** ← **Border Color**（蓝） |
| `BOOLEAN_D80F51B627624648A0B3B2D563AA7541` | 0 ← MultiplyEdgeColor |
| `Vector1_4e2dbe7bff184ada93bb896e70f4c06d` | 1.0 ← **不在 shader 属性表里 ⇒ 死值** |
| `_Disolve`（原版拼写） | 纹理 PathID `-8214795764591836323`（外部 `m_FileID 4`）→ **`Noise Combined.png`**（GUID `49e6466e856bfee4fae85f91e168b92c`） |
| `_MainTex` | **null**（运行时由 UGUI 的 Image sprite 喂） |
| 其它 | `_Surface 0 / _SrcBlend 1 / _DstBlend 0 / _ZWrite 1 / _Cull 2 / _Cutoff 0.5 / _Metallic 0 …` —— **shader 属性表里没有这些名字 ⇒ 全是死值**（CLAUDE.md §三 那条坑，⛔ 别拿它们当「原版参数」） |

**shader 属性表（唯一判据，`Shader_7612461173376232267.json` 的 `m_ParsedForm.m_PropInfo`）**：
`_MainTex`(MainTex, 2D) · `_Disolve`(DissolveTexture, 2D) · `Vector4_d8c31f…`(DissolveUVScale(XY)Offset(ZW)) ·
**`_DissolveAmount`(DissolveAmount, Range 0..1, 默认 0.43)** · `Vector1_2780…`(Dissolve Borded Width, 默认 0.04) ·
`_Color`(Border Color, HDR) · `_DISSOLVE_CHANNEL`(KeywordEnum R,G,B,A) · `BOOLEAN_D80F…`(MultiplyEdgeColor) · 三张 unity_*。
（`ExportedProject/Assets/Shader/Everguild_FX_Sprite Dissolve Mask.shader` 是 AssetRipper 的
`DummyShaderTextExporter` 桩，**只有属性声明、没有真算式** —— 见 §九。）

**引用方**：全库只有 **`MonoBehaviour_6271754070396410816`** 一处引用该材质（另加 `AssetBundle_1.json` 的 manifest 表）
⇒ 它是**这张卡框专用的溶解材质**，没有别的用户。

---

## 六、候选材质清单（"谁身上有 `_DissolveAmount`"）

全库 `Material/*.json` **1076** 个，其中 `m_SavedProperties.m_Floats` 含 **`_DissolveAmount`** 的 **39** 个。
**卡相关**的（其余是岩石/兽人武器/水波等特效族，与卡无关）：

| 材质 | `_DissolveAmount` 序列化值 | keyword | 所在 bundle | 用途 |
|---|---|---|---|---|
| **`SpriteUI Frame Dissolve`** | **1.0** | `_DISSOLVE_CHANNEL_B` | battleprefabs_vfxandmisc | **本任务：2D 卡框溶解** |
| `Card 3d WH40K Dissolve` | 0.0 | `_DISSOLVE_CHANNEL_B`(+`_EMISSION` 失效) | battleprefabs_vfxandmisc | 3D 卡体溶解（`BattleCardUI.dissolveMaterial`） |
| `Card 3d WH40K Dissolve Legendary` | 0.0 | 同上 | battleprefabs_vfxandmisc | 传说卡 3D 溶解（`dissolveMaterialLegendary`） |
| `Card 3d Lvl1` | 0.473 | — | battleprefabs_vfxandmisc | 3D 卡体**正常**材质（`BattleCardUI.defaultMaterial`，也带该属性） |
| `Card 3d WH40K Explosion` / `… Explosion Green` | 0.0 | `_DISSOLVE_CHANNEL_B` + `_SCREENSPACEUV` | battleprefabs_vfxandmisc | 爆炸 |
| `Card 3d Dissolve Blend Image Ambush` | 0.473 | `_USE_BLEND` | battlesharedresources | 伏击 |
| `3D Card Blend Image` | 0.0 | — | shaders | |
| `Border Glow Rectangle` / `Remnant Shatter` / `Vanguard_Frame VAT Dissolve` / `Rank Change` / `Shader Graphs_Division Change` | 见文件 | — | battleprefabs / menus | UI/特效，不是本 clip |

**3D 卡体那条（同 clip 的 `m_FloatCurves[4]`）**：`Board Elements/3DBody/Card 3D` 的 `MeshRenderer`
（`-6865217477711520832`）编辑期材质 = **`Card 3d Lvl1`**（`791121889026168482`）；
`BattleCardUI` 另有 `dissolveMaterial` = `Card 3d WH40K Dissolve`（`-6651017123104198842`）与
`dissolveMaterialLegendary`（`-1409023286357856989`），由 `BattleCardUI__ChooseDissolveMaterial` 挑、
`RestoreOriginalMaterial` 换回 `defaultMaterial`。
⇒ **这条 3D 曲线打的是「当前挂在 Renderer 上的那份材质」的 `_DissolveAmount`**，
和 §五 那份 UI 材质**没有任何关系**（两份材质各自 1 次/0 次被引用，别混）。

---

## 七、同族对照

1. **`floatProperty2`**：clip 里**没有**任何 `floatProperty2` 曲线；组件里它的 `materialProperty` 也是空串 ⇒ 全程不动。
2. **`m_Enabled` 与 `floatProperty1` 同对象**：`path`/`classID`/`script` 三项完全相同（§一）⇒ 是**同一个组件**的开关与被驱动字段。
3. **同一脚本的其它 clip**（全库扫描 `floatProperty1` 的**只有 6 条**，全部绑 `classID 114` + 同一个 MonoScript）：
   `bundle_menus_assets_all`：`Booster Window - Background Shake On Open`（path 空）· `Rank Up`（`Division Image/Rank Icon`）·
   `Division Up`（`Division Image`）；`bundle_scenes_scenes_mainmenuwarpforge`：`Level Up Card` ×2
   （`2DCard/Level Up Effect` 与 `…/Level Up Effect Down`）。
   **只有 `Card Hand To Board` 指 `2DCard/Front/CardFrame`**。
4. **同一 Image 还有第二条材质交换路径**：`2DCard` 上的 `UIImageGreyscaleController`
   （`imageMaterialPairs` 里就有这张 `CardFrame` Image，灰阶材质 `UI Greyscale`，guid `96253cf2…`）。
   ⚠️ 接的时候注意：**同一个 Image 会被两套逻辑换材质**，顺序/还原要各管各的。

---

## 八、结论：这张材质本地有没有、叫什么、怎么接

**有。** 名字 **`SpriteUI Frame Dissolve`**，shader **`Everguild/FX/Sprite Dissolve Mask`**，噪声图 **`Noise Combined`**。
（本机两份：`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1050090670530995784.json`，
以及 `d:/2/tools/assetripper_out/ExportedProject/Assets/Material/SpriteUI Frame Dissolve.mat`。）

**我们工程现状（已核）**：

- **shader 原件拿得到**：`资料/普查产出_0918/shader原件可用性_表.md:95`
  → `Everguild/FX/Sprite Dissolve Mask` ｜ 自建替代 **0** ｜ 主包 **无** ｜ **补充包 有**。
  解析链 `WarpforgeShaderMap.TryResolve("Everguild/FX/Sprite Dissolve Mask", …)`（`WarpforgeShaderMap.cs:509-573`）：
  `Replacements` 无此名 → `Shader.Find` 返回 null（Everguild 私有名）→ **`WarpforgeShaderLoader.TryGetShader` 从
  `wf_shaders_extra.bundle` 取原件** ✅（那个包正是从 `battleprefabs_vfxandmisc_assets_all` 抽的，本 shader 就在该源包里）。
  另外 `WarpforgeVFX/Runtime/OriginalShaderBlendTable.cs` 已经记了它实读出来的渲染态：
  `:52` **srcBlend/dstBlend/zWrite = 5 / 10 / 0** · `:153` **cull = 0（Off，烘死）** · `:252` **zTest = 4（LessEqual）** ·
  `:367` 该 shader 的属性名清单。
- **噪声图**：`Noise Combined.png`（GUID `49e6466e856bfee4fae85f91e168b92c`，**和 3D 卡体溶解 `_DissolveTex` 是同一张**）。
  我们工程里有**逐战场的副本** `MyGame/Assets/WarpforgeArena1/arenas/*/Textures/Noise Combined.png`，
  但 **`Resources/Art/card3d/DissolveNoise` 不存在** ⇒ `CardArt.DissolveNoiseTex()` 现在返回 null（`CardArt.cs:249-260`）。
  要接这条得先把 `Noise Combined` 收进 Resources（或复用战场那份）。
- **要灌的序列化值**（照 §五 抄，一个都不用改）：`_DissolveAmount`（动画 1→0，**0.0167→0.7 s**）·
  `_DISSOLVE_CHANNEL=2` + 关键字 `_DISSOLVE_CHANNEL_B` · `Vector1_2780…=0.07` · `_Color=(0.0588,0.3289,0.9059,1)` ·
  `Vector4_d8c31f…=(1.2,1.2,0,0)` · `BOOLEAN_…=0` · `_Disolve=Noise Combined`。
  ⚠️ **`_SrcBlend/_DstBlend/_ZWrite/_Surface/_Cull/_Cutoff` 等是死值**（shader 属性表里没有），按 CLAUDE.md §三 硬编码。
- **挂在哪一层**：原版是 **2D 卡框那层**（UGUI Image）。我们是 `CardView` 里的卡框层
  （占位支路的 `_frame` MeshRenderer / 卡面帧贴图 `CardArt.Frame(...)`，`CardView.cs:297/357/1038`）——
  等价位置 = **卡框贴图那份材质**，而**不是** 3D 卡体（`CardView.DissolveMaterialProto()` 那份是
  `Everguild/Cards/3D Card Dissolve`，已经是 3D 体那条，别复用到这儿）。
- 我们的时间轴常量已经在了：`CardFeel.HandToBoardFrameOn = 0.0333` / `HandToBoardDissolveEnd = 0.7`
  （`CardFeel.cs:385-393`），但注释把 0.7 只记成「3D 体那条的终点」—— 本份材料补上：**UI 卡框那条也在 0.7 结束，
  起点是 0.0167（不是 0.1667）**。

---

## 九、没查清 / 未验

1. **shader 的真算式没读到。** `ExportedProject` 里那份 `.shader` 是 `DummyShaderTextExporter` 桩；
   真行为要解 `Shader.compressedBlob` 的 DXBC（`工具/dump_shader_blob.py`，本轮未做）。
   ⇒ 只知道**属性名/范围/默认值**，不知道「`_DissolveAmount` 如何映射到 alpha / 边缘带怎么画」。
2. **UGUI 把 sprite 喂进 `_MainTex` 这一跳**是按 UGUI 机制 + shader 声明 `_MainTex` **推的**，
   没找到「谁 `SetTexture("_MainTex", …)`」的代码（也没有这个必要，原本就是 CanvasRenderer 喂）。
   ⚠️ 如果我们的实现不是 UGUI，就**必须自己把卡框图绑到 `_MainTex`**。
3. **卡框 sprite 的运行时赋值调用点没逐条追**：只知道 `Card2DController.SetFrame(sprite, highlightSprite)`
   （`Card2DController__SetFrame.c`）+ `CardFramesByArmy.asset` 提供逐阵营×档位×卡型的 sprite，
   **谁在战斗里调 `SetFrame`** 没查到。
4. **没跑实况/没截图**：本轮纯静态（红线段要求），「溶解看起来是不是这个效果」未验。
5. `Shader Pass 数与 cull/zTest` 等渲染态：`OriginalShaderBlendTable` 已有一份（`Sprite Dissolve Mask → {5,10,0}` /
   `0` / `4`），但**没和原版渲染对拍**。
