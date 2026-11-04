# B141 · W130 · 16-07 补 25 个内建 shader 到 `m_AlwaysIncludedShaders`

- 批次 **B141** · 会话 **128** · 写手 **W49**（第十六区 16-07）
- 改动文件：`Unity/MyGame/ProjectSettings/GraphicsSettings.asset`（唯一改动）
- 自检：**零条**（纯资产 YAML，未跑 Unity 批处理、未跑 typecheck）
- 结论：原版 35 个内建 shader 引用 **已全部补齐**（补 25 条，去重后内建仍是 35、无重复）；我们原有 48 条自建 shader **一条未动**。

## 一、改了什么

`m_AlwaysIncludedShaders:`（第 29 行）下，在原有 10 条内建项之后、48 条自建项之前，插入 25 条内建项（内建块变成连续的 35 条，顺序照原版 `GraphicsSettings_7.json` 的 original order）。

| | 补前 | 补后 |
|---|---|---|
| 条目总数 | **58** | **83** |
| 内建（guid `0000000000000000f000000000000000`, type 0） | **10** | **35** |
| 自建（type 3） | **48** | **48（未动）** |
| 行号范围 | `:30–:87` | `:30–:112`（第 29 行仍为 section 头，`:113` 仍是 `m_PreloadedShaders: []`） |

- 插入的 25 条，PPtr 写法与既有条目完全一致：`  - {fileID: <N>, guid: 0000000000000000f000000000000000, type: 0}`。
- 顺序按原版 json 的原始次序（注意原版是 **9100 → 9102 → 9101**，不是升序，已照抄）。
- ⚠️ `git diff` 对本文件显示 **47 insertions / 1 deletion**，其中**只有 25 行是本代理新增**；另 22 行 `m_ShaderDefinesPerShaderCompiler` 块与 `m_VideoShadersIncludeMode: 2→1` 是**本代理开工前工作树里就有的、别人未提交的改动**，本代理**未触碰**（是相对 HEAD 的累积 diff，不是本单产物）。

## 二、验证（二进制读回）

1. **YAML 可解析**：去掉 `%YAML`/`%TAG`/`--- !u!` 三个 Unity 指令行后用 `yaml.safe_load` 解析成功，`GraphicsSettings.m_AlwaysIncludedShaders` = **83 条**，每条都只有 `{fileID, guid, type}` 三个键。
2. **行尾未翻**：改前 `CRLF=0 / LF=141 / 裸CR=0` → 改后 `CRLF=0 / LF=166 / 裸CR=0`；文件 6621 → 8312 bytes（增量 1691 ≈ 25 条 × 67.6 B），**仍是纯 LF、无裸 CR**。
3. **条目数**：58 + 25 = **83**，内建 **10→35**，自建 **48→48**。
4. **无重复**：35 个内建 pathID 无重复；48 条自建 guid 无重复。
5. **既有 10 条内建顺序未变**：`7, 15104, 15105, 15106, 10753, 10770, 10782, 16000, 17000, 16001`。
6. **集合相等**：补后内建 id 集合 == 原版 `GraphicsSettings_7.json` 里 `m_FileID==3` 的 35 个 pathID 集合（脚本判 `set equal: True`）。

## 三、对照表：原版 35 内建 vs 我们补前/补后

内建 id 的名称取自**原版游戏自带**的 `D:/2/unity_run_ref/Warpforge_Data/Resources/unity_builtin_extra`（45 个 shader）；`❓` = 本机编辑器 6000.3.23f1 的 `unity_builtin_extra` 里没有该 pathID。

| # | pathID | shader 名（原版 builtin_extra） | 原版 | 我们补前 | 我们补后 |
|---|---|---|---|---|---|
| 1 | 7 | Legacy Shaders/Diffuse | ✅ | ✅ | ✅ |
| 2 | 15104 | Hidden/CubeBlur | ✅ | ✅ | ✅ |
| 3 | 15105 | Hidden/CubeCopy | ✅ | ✅ | ✅ |
| 4 | 15106 | Hidden/CubeBlend | ✅ | ✅ | ✅ |
| 5 | 10753 | Sprites/Default | ✅ | ✅ | ✅ |
| 6 | 10770 | UI/Default | ✅ | ✅ | ✅ |
| 7 | 10782 | UI/Default Font | ✅ | ✅ | ✅ |
| 8 | 16000 | Hidden/VideoComposite | ✅ | ✅ | ✅ |
| 9 | 17000 | Hidden/Compositing | ✅ | ✅ | ✅ |
| 10 | 16001 | Hidden/VideoDecode | ✅ | ✅ | ✅ |
| 11 | 19 | Hidden/Internal-StencilWrite | ✅ | ❌ | ✅（新增） |
| 12 | 65 | Hidden/Internal-CombineDepthNormals | ✅ | ❌ | ✅（新增） |
| 13 | 66 | Hidden/BlitCopy | ✅ | ❌ | ✅（新增） |
| 14 | 67 | Hidden/BlitCopyDepth | ✅ | ❌ | ✅（新增） |
| 15 | 109 | Hidden/BlitToDepth | ✅ | ❌ | ✅（新增） |
| 16 | 110 | Hidden/BlitToDepth_MSAA | ✅ | ❌ | ✅（新增） |
| 17 | 107 | Hidden/BlitCopyWithDepth | ✅ | ❌ | ✅（新增） |
| 18 | 111 | Hidden/BlitCopyHDRTonemap | ✅ | ❌ | ✅（新增） |
| 19 | 9000 | Hidden/Internal-GUITextureClip | ✅ | ❌ | ✅（新增） |
| 20 | 9001 | Hidden/Internal-GUITextureClipText | ✅ | ❌ | ✅（新增） |
| 21 | 9002 | Hidden/Internal-GUITexture | ✅ | ❌ | ✅（新增） |
| 22 | 9003 | Hidden/Internal-GUITextureBlit | ✅ | ❌ | ✅（新增） |
| 23 | 9004 | Hidden/Internal-GUIRoundedRect | ✅ | ❌ | ✅（新增） |
| 24 | 9007 | Hidden/Internal-GUIRoundedRectWithColorPerBorder | ✅ | ❌ | ✅（新增） |
| 25 | 9100 | Hidden/Internal-UIRDefault | ✅ | ❌ | ✅（新增） |
| 26 | 9102 | Hidden/Internal-UIRDefaultWorld | ✅ | ❌ | ✅（新增，⚠️ 见 §四.2） |
| 27 | 9101 | Hidden/Internal-UIRAtlasBlitCopy | ✅ | ❌ | ✅（新增） |
| 28 | 68 | Hidden/ConvertTexture | ✅ | ❌ | ✅（新增） |
| 29 | 10757 | Sprites/Mask | ✅ | ❌ | ✅（新增） |
| 30 | 15309 | Hidden/Internal-CubemapToEquirect | ✅ | ❌ | ✅（新增） |
| 31 | 15308 | Hidden/Internal-ODSWorldTexture | ✅ | ❌ | ✅（新增） |
| 32 | 19011 | Hidden/TextCore/Distance Field SSD | ✅ | ❌ | ✅（新增） |
| 33 | 15304 | Hidden/VR/BlitTexArraySlice | ✅ | ❌ | ✅（新增） |
| 34 | 15312 | Hidden/VR/BlitFromTex2DToTexArraySlice | ✅ | ❌ | ✅（新增） |
| 35 | 113 | Hidden/Internal-DebugPattern | ✅ | ❌ | ✅（新增） |

合计：原版 35 内建 → 我们补后 35 内建（差异 0 个 id）。

## 四、关键说明

### 1. 这些是 Unity **内置资源 id**，不是本地资产文件

- 这 25 个 `fileID` 是 **Unity 内置 shader 的 pathID（小整数）**，引用 guid 一律 `0000000000000000f000000000000000`、`type: 0`。
- 佐证（读原版 player 的 `globalgamemanagers` externals，UnityPy）：`m_FileID=1 → globalgamemanagers.assets` · **`m_FileID=3 → Resources/unity_builtin_extra`**，该 externals 的 guid 尾字节为 `0f` ⇒ 对应 `…f000000000000000`。所以 `m_FileID==3` 的 35 条全是 builtin_extra 里的内建 shader，`m_FileID==1` 的 5 条才是**原版工程自建**（见 §六.2）。
- 因此这 25 条**不会在 `Assets/` 下产生任何文件**，也不需要导入；它们的作用是**让这些内建 shader 进入打包时的「always included」清单**（影响 shader 变体剔除）。这不是「复刻某个素材」，而是复刻原版的**打包剔除配置**。

### 2. ⚠️ `9102`（`Hidden/Internal-UIRDefaultWorld`）在现 Unity 版本里 ❓ 解析不到

- 原版 Unity 的 `unity_builtin_extra`（`Warpforge_Data/Resources/unity_builtin_extra`，45 个 shader）里 **有** `9102 = Hidden/Internal-UIRDefaultWorld`。
- 本机编辑器 **6000.3.23f1** 的 `Editor/Data/Resources/unity_builtin_extra`（206 个 shader）里 **没有 9102**：`9000–9007 / 9100 / 9101 / 9105` 都在，唯独缺 `9102`（连 `Hidden/Internal-UIRDefaultWorld` 这个名字也没有）。
- ⇒ 该 id 是**原版 Unity 版本（6000.2.6f2）有、6.3 已移除/改号**的内建 shader。已按原版 PPtr 原样写入，但在本机 Unity 里**解析不到**，下一次 Unity 存盘时**可能被静默剔除**（这正是 B141 判据里预警的「加了之后要读回」那一格）。
- 影响评估：这是 **UIR（UI Renderer）的世界空间默认 shader**，我们的 UI 不走 Unity UIR 渲染路径（我们的界面全走自建 `ImageQuad`/`Label`/TMP），**功能影响 ≈ 0**；但按铁律 11「与原版不符全部要记录」——记录为 ❓ 而非「不做」。

### 3. 生效条件

- 纯资产（`ProjectSettings/*.asset`），**无需 typecheck**（未改任何 `.cs`）。
- 需**一次 Unity 打开工程存盘或构建**才把它写进实际打包剔除表；本代理**没有跑 Unity**（红线）。
- **建议下次 Unity 存盘后复读**：若内建条目数 < 35，缺的就是 `9102`（预期现象，不是回退）。
- **不会**影响任何现有渲染：只增「always included」项，等于放宽剔除，不会引入编译错误或变体缺失。

## 五、判据源 / 与旧文档口径的出入（就地订正）

1. **`判据/B141_W130_对账_资源.md` §五 #3 写「我们是 54 条 = 10 内建 + 48 自建」** —— 现读实际是 **58 条**（10 + 48）。旧文的 **54 是笔误**（48+10=58，不是 54）。本轮不另立活，只在此说明。
2. **`报告/B138_审计汇总_问题清单.md` §16 的 16-07 写「内建 7 个」** —— 现读补前是 **10 个内建**。B138 那格是更早口径（可能按「能解析到的内建」数的，7 应是当时口径）。本报告以现读为准：补前 10、补后 35。
3. 原版 `GraphicsSettings_7.json` 的 40 条 = **35 内建（m_FileID==3）+ 5 工程自建（m_FileID==1）**，与 `判据` §五 #3 一致。

## 六、顺手发现（不属本单范围，交调度台分流）

1. **原版那 5 个工程自建 shader 我们没在 always-included 里复刻**（原版 `m_FileID==1`、pathID `52/60/74/93/98`，在 `globalgamemanagers.assets` 里）。按 guid 反查 AssetRipper 导出工程，它们的名字是：
   - `Everguild/FX/Sprite Greyscale`（guid `3c9018b9…`）
   - `Everguild/UI/Greyscale`（`8623861a…`）
   - `Hidden/Everguild/PlanarReflectionsBlit`（`6c1c5242…`）
   - `Hidden/LUTBlender`（`0c9b33e2…`）
   - `Hidden/Everguild/GrainyBlurForPlanarReflections`（`51722234…`）

   我们工程里有**近似同名/同功能的自建件**（`WFUIGreyscaleAdd.shader`、`WFLUTBlender.shader`、`CardSprite.shader` 等），但「原版 5 条自建 shader 是否都有对应物、要不要也塞进 always-included」是**另一单**（属 shader 覆盖/剔除对账），本单只处理内建 25 条，未动我们 48 条自建。
2. **本文件相对 HEAD 另有 2 处非本代理的未提交改动**（`m_VideoShadersIncludeMode: 2→1`、`m_ShaderDefinesPerShaderCompiler` 22 行块）。本代理**未触碰**，只提醒：这两处若无人认领，收尾时应确认归属再一并提交。
