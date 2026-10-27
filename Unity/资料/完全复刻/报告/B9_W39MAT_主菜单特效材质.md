# B9_W39MAT · 主菜单特效材质导入与接线

> 会话 2026-10-27。任务：把「主菜单特效材质」导进工程（升级动画零观感的真正根因），并把 `.mat` 接到 `CardDisplayEffects` / `RankedDivisionChangeWindow`。
> 边界：未跑 Unity（材质实际渲染无法验）；未动 git、Shaders/、场景、ProjectSettings、账主表、他人文件。

## 一、结论

1. **`Border Effect For Level up` 已导入并接线**（升级动画根因消除的那一半已落地）：
   - 导入成 `Resources/Art/vfx/Border Effect For Level up.mat`，shader = 工程等价 `WarpforgeVFX/UI/UI Border Highlight`（`WFUIBorderHighlight.shader`，guid `8d5ed6ff6f6883649b60650e625a9160`）。
   - `Shell/CardDisplayEffects.cs` 的 `AnimateUIMaterialProperty.Attach(...)` 从传 `null` 改成传 `BorderLevelUpMat()` ⇒ AMP 克隆的是**真材质**，`_Reveal` 现在写进真属性，不再静默写进 `Sprites/Default`。
   - ✅ 只搬**原版 shader 属性表（`m_PropInfo` 19 条）里真实存在的字段**；`_Add/_Range/_RangeStartOffset/_ColorMask/_NoiseStrength/_Length/_MaskScale/_ObjectScreenSpacePosition/_OffsetTest/_ScaleFromCenter/_TempOffset` 是内置 Standard shader 的**残留死值**，⛔ 未搬。
2. **`Rank Icon Glow` 同批处理，但结论推翻了 W28 的猜测**（铁律 5 就地订正）：
   - 它的 `Image.m_Material`（pid `-8464978790139598553`）真实材质名 = **`UI Additive`**，shader = **`Everguild/UI/Additive`**（属性表只有 `_MainTex/_Color/_Stencil*`）—— **不是**边框 shader。
   - 材质里那些 `_ColorMain 337.79…` / `_Reveal 0.723` / `_Noise_Strength_X_Speed_Y_Scale_Z` 是**上一颗 shader 的残留**。
   - ⇒ 它**不是**「靠 `_Reveal` 揭开的黄辉光」，而是「白图（`m_Sprite=0`）× 黄 `m_Color` × **加法混合**」的**加色块**，动画走 `Image.m_Color.a`（本文件 `ApplyChannels` 的 `alpha` 通道已驱动 `ImageQuad.SetTint`），**与 `_Reveal` 无关**。
   - 载体已按此重建：`MenuDraw.Rect(... , GlowTint)` 的 `ImageQuad` + 加法材质 `Resources/Art/vfx/UI Additive.mat`（工程等价 shader `WarpforgeVFX/Sprites/Additive`）。
3. **生成器改造**：`工具/import_mainmenu_effect_mats.py` 在原有「字节拷贝 4 张粒子材质」基础上，新增「按原版材质 JSON 新生成 2 张 UI 特效材质」；跑两遍逐字节相同（幂等）。

## 二、证据（文件:行号 / 资产路径）

**`Border Effect For Level up`**
- `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/Material/Material_4.json`：
  `m_Name = Border Effect For Level up` · `m_Shader = {m_FileID:10, m_PathID:-4463273156108355430}` · `m_CustomRenderQueue = -1`；
  `m_Floats._Reveal = -0.5` · `m_Colors._Noise_Strength_X_Speed_Y_Scale_Z = (0.02,-0.1,8.17,0)` · `m_TexEnvs._Noise_R = {m_FileID:8, m_PathID:-8214795764591836323}`。
- `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json`：
  `m_ParsedForm.m_Name = Everguild/UI/UI Border Highlight Appear Pulsating Scale`；
  `m_PropInfo.m_Props` = 19 条（`_ColorMain _ColorSecondary _Color_offsets _DEBUG _Noise_R _Noise_Strength_X_Speed_Y_Scale_Z _FLIPDIRECTION _Scale_X_Speed_Y _StartOffset _Reveal _Smoothness _Rotation _AUTOROTATION _AutoRotationSpeed _QueueOffset _QueueControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks`）。
- 工程等价 shader：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`（`Shader "WarpforgeVFX/UI/UI Border Highlight"`，属性名同名；guid `8d5ed6ff6f6883649b60650e625a9160`）。
- `_Noise_R` 贴图：原版 pathID `-8214795764591836323` 与同族 `Border Highlight Appear Yellow`（`bundle_menus_assets_all`/`menusharedresources`）的 `_Noise_R` **同一个** ⇒ 工程里那张 `Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/UI_Dirt_And_Noise_skratches.png`（guid `69d5e963143e38e448fb8dca30bd8984`）。参考 `Shell/ForgeTab.cs:1156`（同 shader 用 `CardArt.MenuUi("UI_Dirt_And_Noise_skratches")`）。

**`Rank Icon Glow`**
- `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-8635674026998525618.json`（= `GameObject/Rank Icon Glow.json` 的组件 pid `-8635674026998525618`）：
  `m_Enabled=1` · `m_Material={m_PathID:-8464978790139598553}` · `m_Color=(1,0.9167996,0,1)` · `m_Sprite={m_PathID:0}` · `m_Type=0` · `m_PreserveAspect=0`。
- `d:/2/新解包资源/assets_full/bundle_menus_assets_all/Material/Material_-8464978790139598553.json`：
  `m_Name = UI Additive` · `m_Shader = {m_PathID:4921318991789134610}`（⚠️ **文件名是 pid、`m_Name` 才是 `UI Additive`**）。
- `d:/2/新解包资源/assets_full/bundle_menus_assets_all/Shader/Shader_4921318991789134610.json`：
  `m_ParsedForm.m_Name = Everguild/UI/Additive` · `m_Props = _MainTex _Color _StencilComp _Stencil _StencilOp _StencilWriteMask _StencilReadMask _ColorMask _UseUIAlphaClip`
  ⇒ 材质里的 `_Reveal/_ColorMain/_Noise_Strength_X_Speed_Y_Scale_Z/_Length/_MaskScale/…` **不在**属性表 ⇒ 残留死值。
- 动画：`Resources/division_clips.json` 三条 clip 的 `curves` 里有 `{path:"Division Image/Rank Icon/Rank Icon Glow", channel:"alpha", property:"m_Color.a"}`（0 关键帧）⇒ 驱动的是 `Image.m_Color.a`，**不是** `_Reveal`。消费口 = `Shell/RankedDivisionChangeWindow.cs:887-903`（`alphas` → `ImageQuad.SetTint`）。

**代码落点**
- `Shell/CardDisplayEffects.cs:44`（`MatDir = "Art/vfx/"`）· `:322`（`Attach(..., BorderLevelUpMaterial())`）· 类尾新增 `BorderLevelUpMaterial()`（缓存 + 取不到时**出声一次**、退回传 null）。
- `Shell/RankedDivisionChangeWindow.cs:294`（`_rankGlow = BuildRankIconGlow()`）；类尾新增 `BuildRankIconGlow()`（`MenuDraw.Rect(..., GlowTint)` + `SetMaterial(new Material(srcMat))` + `SetTint(GlowTint)`；取不到材质出声）；`:121`（`GlowTint = (1,0.9168,0,1)`）。

## 三、材质属性对照表（原版 → 我们 → 判据）

### A. `Border Effect For Level up`（`Resources/Art/vfx/Border Effect For Level up.mat`）

| 属性 | 原版 `Material_4.json` 值 | 我们 `.mat` 值 | 判据 |
|---|---|---|---|
| `_ColorMain` (Color) | (0.989295959, 0, 1, 1) | {r:0.98929596, g:0, b:1, a:1} | 原版 m_Colors 实读 |
| `_ColorSecondary` (Color) | (0.153954029, 0, 1, 1) | {r:0.15395403, g:0, b:1, a:1} | 同上 |
| `_Color_offsets` (Vector) | (0.8, 1, 0, 0) | {r:0.8, g:1, b:0, a:0} | 同上 |
| `_Noise_Strength_X_Speed_Y_Scale_Z` (Vector) | (0.02, -0.1, 8.17, 0) | {r:0.02, g:-0.1, b:8.17, a:0} | 同上 |
| `_Scale_X_Speed_Y` (Vector) | (0.9, 3, 0, 0) | {r:0.9, g:3, b:0, a:0} | 同上 |
| `_Reveal` (Float) | -0.5 | -0.5 | m_Floats 实读 |
| `_Smoothness` (Float) | 0.032 | 0.032 | 同上 |
| `_StartOffset` (Float) | 0 | 0 | 同上 |
| `_Rotation` (Float) | 0 | 0 | 同上 |
| `_AUTOROTATION` (Float) | 0 | 0 | 同上 |
| `_AutoRotationSpeed` (Float) | -180 | -180 | 同上 |
| `_FLIPDIRECTION` (Float) | 0 | 0 | 同上 |
| `_DEBUG` (Float) | 0 | 0 | 同上 |
| `_QueueOffset` (Float) | 0 | 0 | 同上 |
| `_QueueControl` (Float) | 0 | 0 | 同上 |
| `_Noise_R` (Tex) | pathID `-8214795764591836323` | `UI_Dirt_And_Noise_skratches.png`（guid 69d5e963…） | 同族材质同 pathID |
| `m_CustomRenderQueue` | -1（用 shader 的 Transparent 队列） | -1 | 原版 Material_4 实读 |

**⛔ 故意不搬（原版 shader `m_PropInfo` 里没有 = Standard 残留死值）**：`_Add 0.09` · `_Range 138.4` · `_RangeStartOffset 196` · `_ColorMask 15` · `_NoiseStrength 0.04` · `_Length` · `_MaskScale` · `_ObjectScreenSpacePosition` · `_OffsetTest` · `_ScaleFromCenter` · `_TempOffset`。
（我们 shader 里 `_Add` 有默认 0.09、`_Range` 默认 89，是 shader Properties 的默认，不是从这张材质的死值抄的。）

### B. `UI Additive`（`Resources/Art/vfx/UI Additive.mat`）

| 属性 | 原版 `Material_-8464978790139598553.json` | 我们 `.mat` 值 | 判据 |
|---|---|---|---|
| shader | `Everguild/UI/Additive`（加法） | 工程等价 `WarpforgeVFX/Sprites/Additive`（`WFSpritesAdditive.shader`，guid `c73bc767f6b789c43a14f857f298032d`，`Blend One One`） | 原版 shader `m_PropInfo`；等价 shader 同为加法白图×色 |
| `_Color` | (1,1,1,1) | (1,1,1,1)（运行时 `SetTint(GlowTint)` 改黄） | m_Colors 实读 |
| `_RendererColor` / `_Flip` | （原版非 sprite shader，无此属性） | (1,1,1,1) 默认 | 等价 shader 默认 |
| `_MainTex` | `{m_PathID:0}`（空，`Image.m_Sprite=0`⇒白） | 空（运行时 `SetMaterial` 置 `_tex`=白） | 原版 m_TexEnvs 实读 |
| `_Reveal`/`_ColorMain`/`_Noise_Strength_…` | 有值（残留） | **不搬** | 原版 shader 属性表没有 = 残留 |

> 🔶 自己设计（等价替身，非原版 shader 名）：原版 `Everguild/UI/Additive` 本机**未单独重建**；本批用工程内同为「加法、白图 × `_Color`」的 `WarpforgeVFX/Sprites/Additive`。对「无 mask/stencil 的纯色块」两者视觉一致；若将来单独重建 `UI/Additive`，把 `.mat` 的 `m_Shader` 换 GUID 即可，属性不用动。

## 四、没查清的部分

1. **材质实际渲染效果未验**（红线：本批不跑 Unity）：`.mat` 能否被 Unity 正常导入、`Resources.Load` 能否命中、`_Reveal` 补间出来的画面像不像原版 —— 全部留到同步点（见第五节）。
2. **`Image.m_Color.a` 驱动的那半是「空曲线」**：`division_clips.json` 里 `Rank Icon Glow` 的 alpha 曲线 **0 关键帧**（三条 clip 都是）。所以即便载体建好，`Rank Icon Glow` 目前也**不会自己显现**（alpha 恒 0、且节点出厂 `SetActive(false)`）。这条是生成器（`gen_division_clips.py`）的数据现状，**不在本批改**（归生成器/该线）；本批只保证「载体 + 加法材质 + 通道消费口」到位、不再报「没有 ImageQuad」。
3. **`Level Up Effect` 到底由谁推 `floatProperty1.value`**：本批只负责「材质在位」（`changeMaterialOnAnimation` 非空 ⇒ AMP 克隆真材质）。那条把 `_Reveal` 从 -0.5 推到 1 的**曲线来自哪条 clip、是否已播放**，是 `B9_W28MOUNT`/`B9_W20WIRE` 已建立的挂载链，本批未重查（任务「别重复查」）。
4. `WFSpritesAdditive` 与 `Everguild/UI/Additive` 的**逐像素**等价未证（只证了「同为加法、白图×色」这一层；原版 UI shader 可能有 rect-clip/stencil 行为，在无 mask 的纯色块上不体现）。

## 五、改动清单

| 文件 | 改动 |
|---|---|
| `Unity/工具/import_mainmenu_effect_mats.py` | 新增 `GENERATED`（`Border Effect For Level up` · `UI Additive` 两张 `.mat` 的生成函数）+ `main()` 写生成物；原 4 张字节拷贝不变。**跑两遍逐字节相同（幂等）**。 |
| `MyGame/Assets/CardPresentation/Resources/Art/vfx/Border Effect For Level up.mat`（生成物） | 新生成（`.gitignore:93` 忽略 `Resources/Art/`，靠生成器幂等复现） |
| `MyGame/Assets/CardPresentation/Resources/Art/vfx/UI Additive.mat`（生成物） | 新生成 |
| `MyGame/Assets/CardPresentation/Shell/CardDisplayEffects.cs` | 第 322 行 `Attach(... , null)` → `Attach(... , BorderLevelUpMaterial())`（1→1 行，零漂移）；类尾新增 `BorderLevelUpMaterial()`（缓存 + 取不到出声一次）。 |
| `MyGame/Assets/CardPresentation/Shell/RankedDivisionChangeWindow.cs` | 第 294 行 `MenuDraw.Node(...)` → `BuildRankIconGlow()`（1→1 行，`账` 在 `:475/:1163` 的落点不漂）；类尾新增 `BuildRankIconGlow()`。 |
| `Unity/资料/完全复刻/账/_parts/B9_W39MAT.tsv` | 对 `AnimateUIMaterialProperty.Update()` 那行的 `备注` 追加本批说明（11 列、无 CRLF、无 BOM，与原表一致）。 |

**自检**：`TMPDIR=/tmp/wf_w39 bash 工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0。导入器连跑两遍：第 2 遍 6 条全 `same`。

## 六、同步点要验什么（材质渲染无法在本批验）

1. **Unity 导入**：`Border Effect For Level up.mat` / `UI Additive.mat` 生成 `.meta`、无导入报错（重点看 shader 能否解析、`_Noise_R` 贴图引用是否命中）。
2. **`Resources.Load` 命中**：进 `CardDisplayWindow`（有 `Level Up Effect` 的那扇）触发一次 → 不应出现 `[CardDisplayEffects] 取不到 …Border Effect For Level up…`；`UIBorderGlow` 也不应报「材质上没有 `_Reveal`」。
3. **观感**：升级动画时，`Level Up Effect` / `…Down` 那块应是**沿边框揭开/旋转的噪声亮带**（紫/品红主色），不是静止白块、不是纯色块；`_Reveal` 补间 -0.5→1。
4. **Rank Icon Glow**：进位窗（RankedDivisionChangeWindow）不应再报 `「Division Image/Rank Icon/Rank Icon Glow」这一颗没有 ImageQuad`；因曲线空 + 出厂关，画面仍无辉光 —— 若要看到，需生成器补 `m_Color.a` 关键帧（另账）。
5. **帧/队列**：两材质都是透明队列（`m_CustomRenderQueue -1`）；确认没被压到卡体/底图之下。

## 七、顺手发现

- **`Rank Icon Glow` 的材质文件名 ≠ 材质名**：`Material_-8464978790139598553.json` 的 `m_Name` 是 `UI Additive`（与「Rank Icon Glow」毫无字面关系）。任何「按文件名/按 pid 猜材质名」都会错；判材质身份必须读 `m_Name`（与 CLAUDE.md「文件大小写不敏感」那条同族：别只看路径）。
- **`UI Additive` 是「材质复用 + shader 换过」的典型残留样本**：`m_SavedProperties` 里一整批边框 shader 的默认值还在，而 shader 已换成 `Everguild/UI/Additive`。这正是「原版材质带内置 Standard shader 残留值」那条坑的最近实例 —— 直接照 `m_SavedProperties` 抄会把死值当活值。
- **`division_clips.json` 的 `Rank Icon Glow` alpha 曲线 0 关键帧**：这说明「段位窗 → Rank Icon Glow 辉光」这条链在生成器侧还是**半成品**（有轨道、没键）。不是本批范围，但下个碰段位窗/生成器的代理值得顺手补键（判据在原版那三条 clip 的 `m_Color.a` 曲线，本地有）。
- 任务简报里「`Rank Icon Glow` 是靠 `_Reveal` 揭开的黄辉光」与「W28 明说判据不足」——**实测判据是够的**，只是结论与猜测相反（见结论 2）。W28 若按「找 `_Reveal` 材质」去找，会一直找不到，因为根本就是 `UI/Additive`。

---

### 交接给下一个代理（若本件之后还有）
- 已完成：两材质导出 + 两处接线 + 生成器 + 账补丁；`typecheck 0/0`；导入器幂等。
- 待办（不在本报告范围内）：① `division_clips.json` 的 `Rank Icon Glow` alpha 曲线补关键帧（判据在原版三条 clip）；② 若将来单独重建 `Everguild/UI/Additive`，把 `UI Additive.mat` 的 `m_Shader` 换 GUID；③ 同步点按第六节验渲染。
