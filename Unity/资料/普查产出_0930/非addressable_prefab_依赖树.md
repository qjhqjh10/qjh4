# 非 addressable prefab 依赖树 —— `Card 3D Death Explosion` / `Vanguard Frame Animated VAT`

> 2026-09-30 · 只读探针 `d:/4/Unity/工具/_probe_prefab_deps.py`（UnityPy 1.24.2 / py312）
> 源包 = `D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`
> （78,190,043 字节 · Unity **6000.2.6f2** · 包内 SerializedFile = `CAB-d47690319398b604c3bb5a35a8ed2499`，
> 另有 `…resS` / `…resource` 两个流文件）
> 原始数据（探针全量 JSON，含每个节点/每条边/每个外部目标）：`%TEMP%/wf_probe_all.json`

## 〇 一句话结论

**两件都在包里**（PathID 见 §二），**都在 `m_PreloadTable` 里、都不在 `m_Container` 里** ——
Unity 那两条枚举路（`GetAllAssetNames()` / `LoadAllAssets<GameObject>()`）**都是按容器走的**，
所以够不着；**「在预加载表里」什么都不能说明**（那张表覆盖了本包 64,301/64,302 个对象）。
依赖树本身不大（**35 / 127** 个内部对象），但**不自洽**：Card 有 **9** 个、Vanguard 有 **19** 个目标
落在**另外 7 个包**里（§五）——**重打包要自洽就得连那 7 个包的内容一起搬，或让运行时加载它们**。

---

## 一 探针怎么跑（可复现）

```bash
cd "d:/4/Unity/工具"
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe _probe_prefab_deps.py --stage all --json out.json
# 只想跑其中一段：
#   --stage index   名字普查 + AssetBundle 两张表（秒级）
#   --stage cab     扫 AA 目录 84 个包，建 CAB-xxx → 磁盘包 的映射（缓存在 %TEMP%/wf_cab_map.json）
#   --stage tree    依赖树（要 CAB 缓存；没缓存会现场建）
```

⚠️ **只读**：不写 `d:/2/`、不写 `MyGame/`；缓存与 JSON 只落 `%TEMP%`。
⚠️ 名字一律走 `read_typetree()` 或前 `4+n` 字节裸解析，**没有 `o.read()` 整包**（64,302 个对象，全程 < 2 分钟）。

**为什么需要 CAB 映射**：本包 `sf.externals` 有 **18 条**（17 条是
`archive:/CAB-<hash>/CAB-<hash>` 形式，第 5 条是 `Library/unity default resources`），
**都不是磁盘文件名** ⇒ 要答「指向哪个外部文件」必须把 `CAB-*` 反查成 `*.bundle`（84 个包 → 186 个 CAB 名，0 个加载失败）。

---

## 二 Q1：两件在不在这个包里？—— **都在**

| `m_Name` | PathID（signed int64） | 类型 | 所在 SerializedFile | 对象字节 | 同名份数 |
|---|---|---|---|---|---|
| `Card 3D Death Explosion` | **-4288998025730031456** | GameObject | `CAB-d47690319398b604c3bb5a35a8ed2499` | 111 B | **1** |
| `Vanguard Frame Animated VAT` | **-7405287611820500883** | GameObject | 同上 | 91 B | **1** |

- 判据：15,734 个 GameObject **逐个读 `m_Name`** 精确比对（探针 `stage_index`）。
- **交叉验证**：`-4288998025730031456` 与我们文档记的**一致** ✅；Vanguard 的 PathID `-7405287611820500883`
  是**本次新查出**的（此前文档没记过）。
- 顺带查了「近名」：含 `death explosion` 的 GameObject 有 8 个（另 7 个叫 `Death Explosion` / `Necrons death explosion`，
  与我们的目标不是同一件）；含 `vanguard frame` 的**只有它 1 个**。

---

## 三 Q2：AssetBundle 两张表 —— 这就是「枚举不到」的直接判据

对象：`AssetBundle` **PathID = 1**，`m_Name = '91c07db2b174d3cccc4d267804cdb3c5.bundle'`
（注意：**这是 AB 内部名，不是磁盘名**），`m_IsStreamedSceneAssetBundle = False`。

| 表 | 条数 | 唯一 | 备注 |
|---|---|---|---|
| `m_Container` | **988** | 名 **983** / 目标 **988** | **5 个名字各出现两次，指向两个不同对象**（Addressables 重名） |
| `m_PreloadTable` | **86,348** | PathID **65,365** | 其中**存在于本文件的 64,301**；另有 **1,064** 个唯一 PathID **指向别的包** |

**容器目标的类型分布**（988 条**全部**存在、`m_FileID` 全 0，无悬空条目）：

| 目标类型 | 条数 |
|---|---|
| GameObject | **965** |
| AnimationClip | 13 |
| Texture2D | 5 |
| Sprite | 5 |

**两件在不在两张表里：**

| | 在 `m_Container`？ | 在 `m_PreloadTable`？ |
|---|---|---|
| `Card 3D Death Explosion` | ❌ **不在**（容器里也没有任何名字含 `death explosion` 的条目） | ✅ 在，被预加载 **1** 次 |
| `Vanguard Frame Animated VAT` | ❌ **不在**（容器里也没有任何名字含 `vanguard` 的条目） | ✅ 在，被预加载 **1** 次 |

**⇒ 与 Unity 侧实测数字逐一对上**：

- `GetAllAssetNames()` = **983** = 容器**唯一名**数 ✅
- `LoadAllAssets<GameObject>()` = **965** = 容器里 **GameObject 目标**数 ✅
  ⇒ 这两条路在这个 Unity 版本里**都是「按容器走」的**：`m_PreloadTable` **不是资产清单**
  （它覆盖本包 64,301/64,302 个对象 —— **唯一不在里面的就是 AssetBundle 对象自己，PathID=1**），
  所以「在预加载表里」**推不出**「枚举得到」。

> 🔴 **就地更正（2026-09-30）**：`资料/已知的坑.md` 那条
> 「要「按名字找、不管它 addressable 不 addressable」就用 `AssetBundle.LoadAllAssets<GameObject>()` ——
> **它把包里所有资产对象都给你**」**不成立** —— 实测 965 恰好等于容器里的 GameObject 数，
> 两件目标一个都拿不到。**正确说法**：`LoadAllAssets`/`GetAllAssetNames` **都只按容器走**，
> 「不在容器里 = 两条路都拿不到」（与 `资料/待办判据_战场与战斗视图.md:1240-1255` 的记载一致）。

**这两件为什么会被预加载 / 谁在用它**（裸字节扫 64,302 个对象找 `fid=0 + PathID` 的 PPtr，0.5 s）：

| 目标 | 本包内的引用者（除它自己的组件以外） |
|---|---|
| `Card 3D Death Explosion` | **`CardPrefab`**（PathID `-7895724578163876928`）上的 **`CardScript.cardDestroyFX`**（MonoBehaviour `1744609728290659264`，偏移 404/600）—— 与文档记的一致 ✅<br>（另 6 处命中都是它**自己的**组件：Transform / MeshFilter / MeshRenderer / Animator / 2 个 MonoBehaviour） |
| `Vanguard Frame Animated VAT` | **没有** —— 命中的 4 处**全是它自己的组件**（Transform `3057843263577561197` · MeshFilter `4991680539672680557` · MeshRenderer `-2628925776283789203` · MonoBehaviour `-6996853648599967635`） |

> 🔴 追加事实（2026-09-30）：**整个本地解包树**（`d:/2/新解包资源/assets_full/`，用 `rg -l "7405287611820500883"` 全树扫）
> 也只命中那 **4 个自身组件文件** + `AssetBundle/AssetBundle_1.json`。
> ⇒ **本地没有任何对象引用 `Vanguard Frame Animated VAT`**。若「它是 `vanguardFrame` 的源」是**按名字**得出的，
> 那属于**我们的口径**，不是包内接线证据（`待办判据_战场与战斗视图.md:1015` 的措辞也要按这条看）。

---

## 四 Q3：依赖树（递归走**内部** PPtr，visited 去重）

### 4.1 `Card 3D Death Explosion`（PathID -4288998025730031456）

**规模**：节点 **35** · 边 **74**（其中外部 11 条）· 内部对象字节 **149,762** · `.resS` 流式 **16,384**

| 类型 | 个数 | | 类型 | 个数 |
|---|---|---|---|---|
| GameObject | 6 | | Shader | 1 |
| Transform | 6 | | AnimatorController | 1 |
| ParticleSystem | 5 | | Animator | 1 |
| ParticleSystemRenderer | 5 | | AnimationClip | 1 |
| Material | 3 | | Texture2D | 1 |
| MonoBehaviour | 2 | | MeshFilter / MeshRenderer / Mesh | 各 1 |

**层级**：根 + 5 个子（`Embers` · `Embers (1)` · `Glow` · `Minion Death` · `Smoke`）；
根上挂 MeshFilter/MeshRenderer + Animator + 2 个 MonoBehaviour，5 个子各一对 ParticleSystem(+Renderer)。

**材质（3 个，全内部）与它们的 Shader**（⚠️ Shader 的 `m_Name` 在文件里是**空串**，
真名在 `m_ParsedForm.m_Name`）：

| 材质 | 字节 | Shader（名 · 内部/外部） | 贴图槽 |
|---|---|---|---|
| `Card 3d WH40K Explosion` | 2,484 | **内部** `Everguild/Cards/3D Card Explosion`（pid 5181610620965656684, 41,816 B） | `_BaseMap`→外部 `WF 3D Card_Card 3D_BaseColor`；`_CardImage`→**空引用**（运行时填卡图）；`_DissolveTex` 与 `_var3DCardColor_…_Noise_768840626_Texture2D`→外部 `Noise Combined`；`_MatCap`→外部 `MatCap Polish` |
| `Embers 1` | 1,628 | **外部** `Universal Render Pipeline/Particles/Unlit`（pid 8918850207803687531, 135,456 B） | `_BaseMap`→外部 `Glow` |
| `Embers Sprite Sheet` | 1,796 | 同上一个外部 Shader | `_BaseMap`→**内部** `Embers` 128×128 fmt25（16,384 B 在 `.resS`） |

**渲染器 → 材质**（6 个渲染器）：MeshRenderer(根)→`Card 3d WH40K Explosion` ·
PSR `Embers`/`Embers (1)`→`Embers 1` · PSR `Minion Death`→`Embers Sprite Sheet` ·
PSR `Smoke`→外部 `Smoke Sprite Sheet Flipbook Blending` · PSR `Glow`→外部 `Glow Additive`

**网格**：`Card 3D WH40k`（-6960435115557275368，58,304 B）· **881 顶点** · 1 子网格 · 索引 4,998
**动画**：`AnimatorController` `Card 3D WH40K Explosion`（480 B）→ `AnimationClip` `Card Explosion`（2,608 B）
**MonoBehaviour（2 个，`m_Script` **全是外部**）**：
① → `DestroyByTime`（字段 `lifeTime`）
② → **`EditorDrawVertexNormals`**（字段 `normalDistance`/`normalColor`/`showNormals`）
—— 是个**编辑器脚本**，可正式包的这个 prefab 上**确实挂着用它**的组件（如实记）

### 4.2 `Vanguard Frame Animated VAT`（PathID -7405287611820500883）

**规模**：节点 **127** · 边 **289**（其中外部 33 条）· 内部对象字节 **488,409** · `.resS` 流式 **207,631**

| 类型 | 个数 | | 类型 | 个数 |
|---|---|---|---|---|
| GameObject | 27 | | Texture2D | 5 |
| Transform | 27 | | MeshFilter / MeshRenderer / Mesh | 各 1 |
| ParticleSystem | 25 | | Shader | 1 |
| ParticleSystemRenderer | 25 | | AnimationClip | 1 |
| Material | 6 | | Animation（组件） | 1 |
| MonoBehaviour | 6 | | | |

**层级**：根 + `VanguardIdleEffect` + 25 个特效子物体
（`Sparks 1..4`/`Sparks Top 1..2`/`Embers`/`Embers (1)`/`Embers Side`/`Embers Side 2`/`Glow side L`/`Glow Side R`/
`Glow bottom L`/`Glow bottom L narrow`/`Glow bottom R`/`Glow bottom R narrow`/`Glow Top`/`Back Glow`/
`Smoke 1..4`/`DustExplosion`/`DustExplosion (1)`/`Trait Icon`）。

**材质（6 个，全内部）与它们的 Shader**：

| 材质 | 字节 | Shader | 贴图槽 |
|---|---|---|---|
| `Vanguard_Frame VAT` | 1,688 | **内部** `Everguild/Matcap/Matcap Full Options VAT`（pid 6331056250094909814, 35,820 B） | `_EmissionTex`→内部 `Vanguard_emission`；`_MainTex`→内部 `Vanguard_tex2`；`_MatCap`→外部 `MatCap Polish`；`_Noise`→外部 `Noise Combined` |
| `Vanguard_Frame VAT Dissolve` | 1,748 | 同上一个内部 Shader | 同上（四个槽一模一样） |
| `Embers 1` | 1,628 | **外部** `Universal Render Pipeline/Particles/Unlit` | `_BaseMap`→外部 `Glow` |
| `Chestrays` | 1,960 | 同一个外部 Shader | `_BaseMap`/`_EmissionMap`→外部 `chestrays_mat` |
| `Particles Generic Blend` | 1,692 | 同一个外部 Shader | 无贴图（全空） |
| `GlowPalet Add` | 196 | **外部** `Mobile/Particles/Additive`（pid 2855819893914472268, 4,956 B） | `_MainTex`→**内部** `GlowPalet` 32×32 |

**网格**：`Vanguard Frame Animated_Mesh`（-5697040799452051110，234,540 B）· **4,438 顶点** · 1 子网格 · 索引 21,060
**动画**：`Animation` 组件 + `AnimationClip` `Vanguard Frame Animation`（3,488 B）
**内部贴图（5 张，全部走 `.resS`）**：

| 名 | 尺寸 / fmt | 对象字节 | 流式字节 |
|---|---|---|---|
| `Vanguard_tex2` | 512×512 / fmt10 | 216 | 174,776 |
| `Vanguard_emission` | 128×128 / fmt12 | 220 | 21,872 |
| `GlowPalet` | 32×32 / fmt3 | 212 | 4,095 |
| `Vanguard Frame Animation VAT_PositionTex` | 28×41 / fmt4 | 240 | 4,592 |
| `Vanguard Frame Animation VAT_RotationTex` | 14×41 / fmt4 | 240 | 2,296 |

（后两张 = **VAT 顶点动画数据**，由 MonoBehaviour `Vanguard Frame Animation VAT` 的 **`PositionsTex` / `RotationsTex`** 字段引用，
不是材质槽 —— 判据：边 `PositionsTex/RotationsTex @ MonoBehaviour`。）

**MonoBehaviour（6 个，`m_Script` **全是外部**，都在 `Waprforge_monoscripts.bundle`）**：

| 脚本（外部 MonoScript） | 挂在哪 | 本组件字节 |
|---|---|---|
| `VATAnimation` | GameObject `Vanguard Frame Animation VAT` | 164 |
| `VATGPUPlayer` | （根上） | 52 |
| `AnimFXController` | （根上） | 124 |
| `AnimFXModuleDestroyInTime` | 某子物体 | 40 |
| `AnimFXModuleChangeMaterial` | 某子物体 | 212 |
| `AnimFXModuleEvent` | 某子物体 | 364 |

（另有 `AnimationClip` 的 `m_FloatCurves[0].script` → `VATGPUPlayer`，即**动画事件**也指向同一个外部脚本。）

---

## 五 Q4：外部引用（**最重要的一条**）

「外部」= PPtr 的 `m_FileID ≠ 0`（指向 `sf.externals`，即**本包之外**）。

| 树 | 外部**边**数 | 去重后**目标**数 | 目标类型分布 | 目标对象字节（去重） |
|---|---|---|---|---|
| `Card 3D Death Explosion` | 11 | **9** | MonoScript 2 · Texture2D 4 · Material 2 · Shader 1 | **139,868** |
| `Vanguard Frame Animated VAT` | 33 | **19** | MonoScript 6 · Texture2D 4 · Material 4 · Shader 2 · Sprite 2 · MonoBehaviour 1 | **150,648** |

### 5.1 `Card 3D Death Explosion` —— 9 个外部目标

| 引用它的字段 | 源对象 | 目标类型 | 目标名 | **目标所在包** | 字节 |
|---|---|---|---|---|---|
| `m_Script` | MonoBehaviour | MonoScript | `DestroyByTime` | `Waprforge_monoscripts.bundle` | 84 |
| `m_Script` | MonoBehaviour | MonoScript | `EditorDrawVertexNormals` | `Waprforge_monoscripts.bundle` | 100 |
| `_BaseMap` | Material `Card 3d WH40K Explosion` | Texture2D | `WF 3D Card_Card 3D_BaseColor` | `battlesharedresources_assets_all.bundle` | 228 |
| `_DissolveTex` + `_var3DCardColor_…_Noise_768840626_Texture2D`（引 2 次） | 同上 | Texture2D | `Noise Combined` | `duplicateassetisolation_assets_all.bundle` | 216 |
| `_MatCap` | 同上 | Texture2D | `MatCap Polish` | `battlesharedresources_assets_all.bundle` | 216 |
| `_BaseMap` | Material `Embers 1` | Texture2D | `Glow` | `duplicateassetisolation_assets_all.bundle` | 204 |
| `m_Shader`（引 2 次：`Embers 1` + `Embers Sprite Sheet`） | Material | Shader | `Universal Render Pipeline/Particles/Unlit` | `shaders_assets_all.bundle` | **135,456** |
| `m_Materials[0]` | ParticleSystemRenderer `Smoke` | Material | `Smoke Sprite Sheet Flipbook Blending` | `duplicateassetisolation_assets_all.bundle` | 1,692 |
| `m_Materials[0]` | ParticleSystemRenderer `Glow` | Material | `Glow Additive` | `duplicateassetisolation_assets_all.bundle` | 1,672 |

### 5.2 `Vanguard Frame Animated VAT` —— 19 个外部目标

| 引用它的字段 | 源对象 | 目标类型 | 目标名 | **目标所在包** | 字节 |
|---|---|---|---|---|---|
| `m_Script` / `m_FloatCurves[0].script`（引 2 次） | MonoBehaviour / AnimationClip | MonoScript | `VATGPUPlayer` | `Waprforge_monoscripts.bundle` | 104 |
| `m_Script` | MonoBehaviour `Vanguard Frame Animation VAT` | MonoScript | `VATAnimation` | `Waprforge_monoscripts.bundle` | 104 |
| `m_Script` | MonoBehaviour | MonoScript | `AnimFXModuleDestroyInTime` | `Waprforge_monoscripts.bundle` | 108 |
| `m_Script` | MonoBehaviour | MonoScript | `AnimFXModuleChangeMaterial` | `Waprforge_monoscripts.bundle` | 108 |
| `m_Script` | MonoBehaviour | MonoScript | `AnimFXModuleEvent` | `Waprforge_monoscripts.bundle` | 92 |
| `m_Script` | MonoBehaviour | MonoScript | `AnimFXController` | `Waprforge_monoscripts.bundle` | 84 |
| `_MatCap`（引 2 次） | Material `Vanguard_Frame VAT(_Dissolve)` | Texture2D | `MatCap Polish` | `battlesharedresources_assets_all.bundle` | 216 |
| `_Noise`（引 2 次） | 同上 | Texture2D | `Noise Combined` | `duplicateassetisolation_assets_all.bundle` | 216 |
| `_BaseMap` | Material `Embers 1` | Texture2D | `Glow` | `duplicateassetisolation_assets_all.bundle` | 204 |
| `_BaseMap`/`_EmissionMap`（引 2 次） | Material `Chestrays` | Texture2D | `chestrays_mat` | `duplicateassetisolation_assets_all.bundle` | 216 |
| `UVModule.sprites[0].sprite` | ParticleSystem | Sprite | `Up Rays` | `duplicateassetisolation_assets_all.bundle` | 788 |
| `UVModule.sprites[0].sprite` | ParticleSystem | Sprite | `Atlas_trait_icon_vanguard` | `atlasindividual_assets_40ktraiticonatlas.bundle` | 448 |
| `m_Materials[0]`（引 2 次） | PSR `Glow bottom L/R` | Material | `Glow Additive Extra Color Soft` | `duplicateassetisolation_assets_all.bundle` | 1,936 |
| `m_Materials[0]`（引 2 次） | PSR `Glow side L` / `Glow Side R` | Material | `Glow Additive Extra Color` | `duplicateassetisolation_assets_all.bundle` | 1,912 |
| `m_Materials[0]` | PSR `Glow Top` | Material | `Glow Rays Additive Extra Color` | `duplicateassetisolation_assets_all.bundle` | 1,896 |
| `m_Materials[0]`（**引 6 次**） | PSR `Smoke 1..4` / `DustExplosion` / `DustExplosion (1)` | Material | `Smoke Sprite Sheet Soft` | `battlesharedresources_assets_all.bundle` | 1,720 |
| `m_Shader`（引 3 次：`Embers 1`/`Chestrays`/`Particles Generic Blend`） | Material | Shader | `Universal Render Pipeline/Particles/Unlit` | `shaders_assets_all.bundle` | **135,456** |
| `m_Shader` | Material `GlowPalet Add` | Shader | `Mobile/Particles/Additive` | `Warpforge_unitybuiltinassets.bundle` | 4,956 |
| `sounds[0].sound` | MonoBehaviour（`AnimFXController`） | MonoBehaviour | `Trait Vanguard` | `soundcollection_assets_all.bundle` | 84 |

### 5.3 7 个外部包**都在本地磁盘上**（判据：`os.path.exists`，均在同一个 AA 目录）

| 包 | 磁盘字节 |
|---|---|
| `Waprforge_monoscripts.bundle` | 36,564 |
| `battlesharedresources_assets_all.bundle` | 48,638,164 |
| `duplicateassetisolation_assets_all.bundle` | 4,319,535 |
| `shaders_assets_all.bundle` | 836,914 |
| `Warpforge_unitybuiltinassets.bundle` | 106,581 |
| `soundcollection_assets_all.bundle` | 32,964,297 |
| `atlasindividual_assets_40ktraiticonatlas.bundle` | 339,112 |

**⇒ 对「重打包能不能自洽」的结论**：
**只搬包内那 35/127 个对象是不够的** —— **两棵树的并集 = 24 个去重外部目标 / 154,424 字节**
（**MonoScript 8 · Material 6 · Texture2D 5 · Shader 2 · Sprite 2 · MonoBehaviour 1**；
Card 9 个 + Vanguard 19 个，**其中 4 个是两棵共用的**：`MatCap Polish` · `Noise Combined` · `Glow` ·
`Universal Render Pipeline/Particles/Unlit`），分布在 **7 个包**里：

| 外部包 | 目标个数 | 字节 |
|---|---|---|
| `duplicateassetisolation_assets_all.bundle` | 9 | 10,532 |
| `Waprforge_monoscripts.bundle` | 8 | 784 |
| `battlesharedresources_assets_all.bundle` | 3 | 2,164 |
| `shaders_assets_all.bundle` | 1 | **135,456** |
| `Warpforge_unitybuiltinassets.bundle` | 1 | 4,956 |
| `atlasindividual_assets_40ktraiticonatlas.bundle` | 1 | 448 |
| `soundcollection_assets_all.bundle` | 1 | 84 |

两条路：① **连这些目标一起搬进新包**（那就要**重映射 `m_FileID`**，且 shader 的 135 KB 我们已经有一份原件
——`工具/extract_missing_shaders.py` 就是干这个的）；② **保持 `m_FileID` 不动、让运行时先加载那 7 个包**
（前提是这些包在我们运行时也在，`Waprforge_monoscripts` / `Warpforge_unitybuiltinassets` 我们本来就在用）。
⚠️ 注意：**8 个 MonoScript 引用是跨包的**——重打包时若把它指向我们自己工程的脚本，得逐条改 `m_Script`（不是搬字节）。

---

## 六 Q5：保留这棵树要占多少字节

| | 内部对象序列化字节 | `.resS` 流式字节 | 内部小计 | 外部目标（去重） | **合计** |
|---|---|---|---|---|---|
| `Card 3D Death Explosion` | 149,762 | 16,384 | **166,146** | 139,868（9 个） | **≈ 306 KB** |
| `Vanguard Frame Animated VAT` | 488,409 | 207,631 | **696,040** | 150,648（19 个） | **≈ 847 KB** |

- 口径：内部 = 递归到的那 35/127 个对象的 `ObjectReader.byte_size` 之和（**不含** `.resS` 里的像素/顶点流）；
  外部 = §五 每个去重目标**在本包里的对象本身**的字节（不含它们在各自包里的 `.resS` 流数据）。
- 量级结论：**两件加起来 ≈ 1.0 MB**（内部 + 流式 = 862,186 B ≈ 0.82 MB；再并上 24 个外部目标 154,424 B → **1,016,610 B ≈ 0.97 MB**）。
  大头是网格（58 KB / 235 KB）、两个内置 shader（41 KB / 36 KB）与两个外部 shader（合计 140 KB，其中 135 KB 那份我们已有原件可复用）。

---

## 七 Q6：核对 `15734 / 983`

| 我们文档写的 | 实测 | 结论 |
|---|---|---|
| GameObject 对象总数 **15734** | **15,734** ✅ | 数字对 |
| 「AssetBundle 容器条数 **983**」 | `m_Container` **988 条**；**唯一名 983** | **两个口径都对，但要写清哪一个** —— Unity 的 `GetAllAssetNames()` 返回的是**唯一名 983**；**容器条目本身是 988**（5 个重名条目各指向两个不同对象） |

补一条背景数：**本包共 64,302 个对象，其中只有 988 个在容器里 ⇒ 63,314 个对象（含这两件）不是 addressable**；
而 `m_PreloadTable` **覆盖 64,301 个**（唯一不在里面的就是 AssetBundle 对象自己）。

---

## 八 出处

| 结论 | 出处（可复查坐标） |
|---|---|
| 两件的 PathID / 所在 SerializedFile / 名字 | 本包 `sf.objects`，探针 `stage index`；`%TEMP%/wf_probe_all.json` |
| 容器 988 / 唯一名 983 / 目标类型 965+13+5+5 / 预加载 86,348 | `AssetBundle` 对象 PathID=1 的 `m_Container` / `m_PreloadTable` 字段 |
| 依赖树每个节点、每条边、每个外部目标 | 探针 `stage tree`；`wf_probe_all.json` 的 `trees[<pathid>]` |
| Shader 真名（`m_Name` 为空串） | `m_ParsedForm.m_Name`（本包 pid 5181610620965656684 / 6331056250094909814；外部的 8918850207803687531 / 2855819893914472268） |
| `cardDestroyFX` 引用者 | MonoBehaviour pid `1744609728290659264`（挂在 GameObject `CardPrefab` `-7895724578163876928`）的 `cardDestroyFX` 字段 |
| 「本地无人引用 Vanguard prefab」 | `rg -l "7405287611820500883" d:/2/新解包资源/assets_full/` → 只命中它自己的 5 个组件 + `AssetBundle_1.json` |
| CAB → 磁盘包 映射 | 探针 `stage cab`；缓存 `%TEMP%/wf_cab_map.json`（84 包 / 186 CAB / 0 失败） |
