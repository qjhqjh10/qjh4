# W61 · 工具文档线 T10 尾巴 · TMP 第二跳的材质半边（`A1325` 第三跳）

> 范围：`EffectExporter.Export()` 里 TMP 的 **`m_sharedMaterial`**（= `fontSharedMaterial`）那一跳。
> ⛔ 本文件只记事实与判据；逐条待办以 `资料/任务线/工具文档线/任务总表.md` §一 为准（铁律 6）。
> 本轮**没跑 Unity**（重导由主对话在同步点跑），**不跑自检**（铁律 12）。
> 只改了 1 个 `.cs`：`Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`。

---

## 1. 结论（一句话）

代码已按 `BoosterPackExporter.cs:600-620` 的同一形状显式接上（逐条 `ImportMaterial` → `fontSharedMaterial`），
秒级 typecheck 0 错。**38 条里 34 条能逐条精确接**（8 种材质 · `TextMeshPro/Distance Field`），
**4 条只能近似接**（`Pragati-Regular White_thick Offset for 3D` · 原版 shader `TextMeshPro/Distance Field Offset`
本地工程没有）—— 近似只丢一个 pass 级 `Offset -1,-1`（多边形偏移），**66 个材质属性名逐条相同**。

---

## 2. 现状：这 38 条今天是怎么来的

### 2·1 病灶 = 靠 `LoadFontAsset()` 的**副作用**，而它是有条件的

`EffectExporter.Export()` 原来只做 `t.font = f`（接字体），`m_sharedMaterial` 从来没显式写过。
`TMP_Text.font` 的 setter 会调 `LoadFontAsset()`（`TMP_Text.cs:173-177`），而
**`TextMeshProUGUI.LoadFontAsset()` 是「有条件覆盖」**：

```
Runtime/TMP/TextMeshProUGUI.cs:1256-1263
if (m_sharedMaterial == null || m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex) == null
    || m_fontAsset.atlasTexture.GetInstanceID() != m_sharedMaterial.GetTexture(...).GetInstanceID())
        m_sharedMaterial = m_fontAsset.material;      // ← 塌成「字体自带的那一份」
```

原版那 9 份材质是**同一张字体图集的不同描边档**，但它们的 `_MainTex` 指向**包里的**图集对象；
工程字体资产的 `atlasTexture` 是**工程里**那份 ⇒ `GetInstanceID()` 不等 ⇒ **全部被换成字体自带材质**。

### 2·2 原版真实分布（判据 = 原包 prefab，不是截图）

判据文件：`D:\2\tools\assetripper_out\ExportedProject\Assets\CardPrefab.prefab`
（38 条 `m_fontAsset` / 38 条 `m_sharedMaterial`；与 `bundle_fonts_assets_all` 里的 9 份 `Material/*.json`
逐名核对过，9 份**都在那个包里**）。原包 shader 也在：`bundle_fonts_assets_all/Shader/Shader_2094616344858530525.json`
= `TextMeshPro/Distance Field Offset`。

| # | 原版材质（9 种） | 条数 | 字体 | 原 shader | 工程里有吗 |
|---|---|---|---|---|---|
| 1 | `Pragati-Regular Card Content` | 8 | Pragati | `TextMeshPro/Distance Field` | **没有** → 新建 |
| 2 | `Pragati-Regular Atlas Material Extra Thick Outline` | 7 | Pragati | 同上 | 有（与原件仅 `_GlowColor` 浮点末位差） |
| 3 | `Asar-Regular White-Card name` | 6 | Asar | 同上 | **没有** → 新建 |
| 4 | `Pragati-Regular White_thick Offset for 3D` | 4 | Pragati | **`TextMeshPro/Distance Field Offset`** | 有，但**是错的**（见 §5·2） |
| 5 | `Pragati-Regular Atlas Material Thick Outline` | 4 | Pragati | `TextMeshPro/Distance Field` | 有 |
| 6 | `Pragati-Regular Atlas Material Thin Outline` | 4 | Pragati | 同上 | **没有** → 新建 |
| 7 | `Pragati-Regular Card Unit type` | 3 | Pragati | 同上 | **没有** → 新建 |
| 8 | `Pragati-Regular Atlas Material` | 1 | Pragati | 同上 | 有（与原件 **diff = 0**） |
| 9 | `Asar-Regular Atlas Material` | 1 | Asar | 同上 | 有（与原件 **diff = 0**） |

- 字体分布：**Pragati 31 条 / Asar 7 条**；材质分布：**Pragati 8 种 / Asar 2 种**。
- **字体与材质是配对的**（Pragati 材质只挂 Pragati 字体、Asar 只挂 Asar）—— 38 条逐条核过，无交叉。
- 9 种之间差的全是 TMP 的描边/发光档：`_FaceDilate` · `_OutlineWidth` · `_OutlineColor` ·
  `_GlowOffset` · `_GlowOuter` · `_GlowPower` · `_GlowColor` · `_ScaleRatioB/C` · `_StencilRead/WriteMask` ·
  `_FaceColor`（`Card Unit type` 那 3 条是橙字）+ 字体级的 `_GradientScale`/`_TextureWidth/Height`/`_WeightBold`。

### 2·3 盘上实测（今天 = 已塌）

`Unity/MyGame/Assets/WarpforgeVFX/Prefabs/CardPrefab.prefab`（639,219 B · 38 条 `m_sharedMaterial`）：

| guid | 指向 | 条数 |
|---|---|---|
| `3592247d…` | `Pragati-Regular Atlas Material.mat`（字体自带那份） | **31** |
| `87856da7…` | `Asar-Regular Atlas Material.mat`（字体自带那份） | **7** |

即：**字体那一跳是对的（31/7 恰好等于原版的字体分布），塌掉的只是「同一字体下的 8 种 / 2 种描边」。**
🔴 这个缺陷 `工具/check_prefab_refs.py` **看不出来** —— 它数的是「guid 全 0 的坏引用」，
而这里 38 条**都解析得到**，只是**指错了东西**。现读该工具仍是 `rc=0`（`m_Script` 50 / `m_Sprite` 57 /
`m_fontAsset` 38 / `m_sharedMaterial` 38 全 0 坏引用）。

---

## 3. 改动清单（只动 `EffectExporter.cs`）

| # | 位置 | 改了什么 | 为什么 |
|---|---|---|---|
| ① | `ShaderMap`（`:157-171`，9 张表项之后） | 新增两条：`TextMeshPro/Distance Field` → 同名；**`TextMeshPro/Distance Field Offset` → `TextMeshPro/Distance Field*`（带 `*` = 近似）** | 不写第一条也不坏（`ImportMaterial:2740` 会退到 `Shader.Find`，工程里确实有 `Assets/TextMesh Pro/Shaders/TMP_SDF.shader`）。**必须写的是第二条**：`Distance Field Offset` 在本地 TMP Essentials 的 13 张 shader 里**不存在**，不映射就会掉进兜底的 `Universal Render Pipeline/Unlit` —— 那是**文字**材质，套 Unlit 等于这一格画不出来。判据与运行时表**同一条** → `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:338-343` |
| ② | `Export()` 的 TMP 循环（`:2103-2219`） | 重写：**先读 `m_sharedMaterial` → 再设 `t.font` → 再 `ImportMaterial` + `fontSharedMaterial`**；沿用字体那一跳；新增「已是工程资产就复用」的短路；新增**图集守卫**；新增一行汇总日志 | 照 `BoosterPackExporter.cs:600-620` 的同一形状。🔴 **顺序不能反**：`t.font = f` 会把 `m_sharedMaterial` 覆盖掉（§2·1），反过来读到的永远是字体自带材质 —— 而且**看着完全正常**（这正是这一跳静默了三轮的原因） |

### 3·1 「图集守卫」是什么、为什么加

```
if (f != null) {
    var wantAtlas = f.atlasTexture;
    var haveAtlas = pm.GetTexture(TMPro.ShaderUtilities.ID_MainTex);
    if (wantAtlas != null && haveAtlas != wantAtlas) { pm.SetTexture(...); EditorUtility.SetDirty(pm); }
}
```

TMP 认这份材质的前提是 **`_MainTex` == 该字体的 `atlasTexture`**（就是 §2·1 那个判据）。
**今天两者通常是同一份资产**（`ImportTexture` 落到的路径 `WarpforgeVFX/Textures/<图集名>.png`
恰好就是字体资产 `m_AtlasTextures[0]` 指的那一份），所以这一支**一般是空转**。
写它的意义是**把「恰好」变成「保证」**：字体资产一改名、或图集导出路径一变，9 种描边就会在
**运行时**（`Awake`/`OnEnable` 会再走 `LoadFontAsset()`）重新塌成 2 种，**而且一声不吭**。

### 3·2 「已是工程资产就复用」这条短路

3D `TMPro.TextMeshPro` 的渲染器在 `:1769` 那圈里**已经导过**（材质池同一个）；它的 `m_sharedMaterial`
仍是包里的对象，所以正常会走 `ImportMaterial` → **命中 `MatCache`** → 拿回同一份工程材质（无害）。
加 `AssetDatabase.Contains(srcMat)` 是为了防「源**已经**是工程资产」时**自己导自己**（`RunListed` 的包列表里
**含我们重打的 `wf_prefabs_extra.bundle`**，理论上能碰到）。

### 3·3 出声（⛔ 不许静默失败）

- 导不出来：`LogWarning` 点名 + `**这一格留空**`，并**明确写出**「TMP 随后会按 `LoadFontAsset()` 回落成字体自带材质 —— 那是近似不是原版」。
- 近似 shader：`LogWarning` 点名（带原 shader 名）。
- 汇总一行：`A1325 <prefab> TMP m_sharedMaterial：接回 N 条（其中 X 条本来就是工程资产 · 其中近似 shader Y 条 · 图集改绑 Z 条）、留空 M 条`。

---

## 4. 逐条覆盖表（38 条全列，判据 = §2·2 的原包数据）

**接成哪种 / 近似 / 留空** —— 全部 38 条**都能接**（无一条留空）；其中 4 条是**近似**。

| # | 原版节点 | 字体 | 原版材质 | 接成 | 近似？ |
|---|---|---|---|---|---|
| 1 | `TraitCounter` | Pragati | Extra Thick Outline | 同名工程材质 | 否 |
| 2 | `TraitCounter` | Pragati | Extra Thick Outline | 同上 | 否 |
| 3 | `TraitCounter` | Pragati | Extra Thick Outline | 同上 | 否 |
| 4 | `TraitCounter` | Pragati | Extra Thick Outline | 同上 | 否 |
| 5 | `TraitCounter` | Pragati | Extra Thick Outline | 同上 | 否 |
| 6 | `TraitCounter` | Pragati | Extra Thick Outline | 同上 | 否 |
| 7 | `TraitCounter` | Pragati | Extra Thick Outline | 同上 | 否 |
| 8 | `Range Attack Text` | Pragati | White_thick Offset for 3D | 同名工程材质（换 `Distance Field`） | **是** |
| 9 | `Melee AttackText` | Pragati | White_thick Offset for 3D | 同上 | **是** |
| 10 | `HealthText` | Pragati | White_thick Offset for 3D | 同上 | **是** |
| 11 | `Armour Text` | Pragati | White_thick Offset for 3D | 同上 | **是** |
| 12 | `DamageText` | Pragati | Atlas Material Thick Outline | 同名工程材质 | 否 |
| 13 | `HealText` | Pragati | Atlas Material | 同名工程材质 | 否 |
| 14 | `InBattleId` | Asar | Asar-Regular Atlas Material | 同名工程材质 | 否 |
| 15 | `HealthText` | Pragati | Atlas Material Thin Outline | 同名工程材质 | 否 |
| 16 | `Armour Text` | Pragati | Atlas Material Thick Outline | 同上 | 否 |
| 17 | `Range Attack Text` | Pragati | Atlas Material Thin Outline | 同上 | 否 |
| 18 | `Melee Attack Text` | Pragati | Atlas Material Thin Outline | 同上 | 否 |
| 19 | `CostText` | Pragati | Atlas Material Thick Outline | 同上 | 否 |
| 20 | `CostText` | Pragati | Atlas Material Thick Outline | 同上 | 否 |
| 21 | `NameTextUnit No description` | Asar | Asar-Regular White-Card name | 同名工程材质（新建） | 否 |
| 22 | `NameTextUnit No Description Big` | Asar | Asar-Regular White-Card name | 同上 | 否 |
| 23 | `Army No Desciption` | Pragati | Card Unit type | 同上（新建） | 否 |
| 24 | `NameTextUnit` | Asar | Asar-Regular White-Card name | 同上 | 否 |
| 25 | `NameTextUnit Big` | Asar | Asar-Regular White-Card name | 同上 | 否 |
| 26 | `DescTextUnit` | Pragati | Card Content | 同上（新建） | 否 |
| 27 | `DescTextUnit Big` | Pragati | Card Content | 同上 | 否 |
| 28 | `RaceText` | Pragati | Card Content | 同上 | 否 |
| 29 | `RaceText Big` | Pragati | Card Content | 同上 | 否 |
| 30 | `ArmyTextUnit` | Pragati | Card Unit type | 同上 | 否 |
| 31 | `DescTextTactic` | Pragati | Card Content | 同上 | 否 |
| 32 | `DescTextTactic Big` | Pragati | Card Content | 同上 | 否 |
| 33 | `DescTacticSmall` | Pragati | Card Content | 同上 | 否 |
| 34 | `NameTextTactic` | Asar | Asar-Regular White-Card name | 同上 | 否 |
| 35 | `NameTextTacticBig` | Asar | Asar-Regular White-Card name | 同上 | 否 |
| 36 | `ArmyTextTactc` | Pragati | Card Unit type | 同上 | 否 |
| 37 | `RaceText` | Pragati | Card Content | 同上 | 否 |
| 38 | `Created By Text` | Pragati | Atlas Material Thin Outline | 同上 | 否 |

**汇总：34 条精确 · 4 条近似（第 8–11，同一份材质）· 0 条留空。**

### 4·1 那 4 条「近似」到底差在哪（逐字段核过，不是估）

原版 `TextMeshPro/Distance Field Offset` vs 本地 `TextMeshPro/Distance Field`：

| 项 | 结果 |
|---|---|
| 属性表（`m_PropInfo.m_Props`） | **66 / 66，逐名相同** ⇒ 材质上每一个值都能搬过去（含 `_Glow*` 那几个） |
| 关键字表 `m_KeywordNames` | 相同 |
| subshader 标签 / pass 标签 / `m_ProgramMask` | 相同 |
| pass 状态 | 只差 **`offsetFactor: 0 → -1` 与 `offsetUnits: 0 → -1`**（外加 `gpuProgramID` 不同） |

⇒ **「Offset」= 那个 pass 级多边形偏移（`Offset -1,-1`）**，不是别的语义。
近似 = 只丢掉这个深度偏移；其余（描边/发光）**逐属性相同**。
⚠️ 是否逐字节同一份 DXBC **没核**（包里的 shader JSON 不含字节码）。

---

## 5. 没查清 / 顺手发现

### 5·1 ✅ 图集那条路是安全的（核过，不是猜）

- 工程字体资产：`CardPresentation/Resources/Fonts/Pragati-Regular SDF.asset` → `m_AtlasTextures[0]`
  = `WarpforgeVFX/Textures/Pragati-Regular SDF Atlas.png`（guid `1f88121c…`）；Asar 同名对应（`a9980722…`）。
- 9 份原版材质的 `_MainTex` **全部**指向这两张图集之一（8 份 Pragati / 1 份 Asar），**没有**第三种。
- `ImportTexture` 会按 `{TexDir}/{Sanitize(名字)}.png` 重写那两张 PNG —— 实测**逐像素相同**：
  `bundle_fonts_assets_all/Texture2D/*.png` vs 工程里那两张，`maxdiff = 0`、不同像素 **0**（1024×2048 / 1024×1024）。
  包里的原图 `m_MipCount = 1`（无 mip），与工程 `.meta` 的 `enableMipMap: 0` 一致 ⇒ `ImportTexture` 不会把 mip 打开。
  ⇒ **重导后这两张 PNG / 它们的 `.meta` 应该 `git diff` 为空**（只可能 mtime 变）。⚠️ 同步点仍请对一眼。

### 5·2 🔴 顺手发现：工程里那份 `Pragati-Regular White_thick Offset for 3D.mat` **现在是错的**

现读它的 `m_Shader` = guid `650dd952…` = **`Universal Render Pipeline/Unlit`** ——
就是「表里没有 ⇒ 掉进兜底」的产物：原件的 **51 个 float + 10 个 color 一个都没搬过来**
（工程那份只剩 16 个 URP/Unlit 自己的 float，`_FaceDilate`/`_OutlineWidth`/`_Glow*` 全缺）。
本轮的 `ShaderMap` 新增之后，**它会在一模一样的路径上被原地覆盖**成 `TextMeshPro/Distance Field`（近似档），
guid 不变 ⇒ 引用它的地方（若 Booster 那几扇窗也用它）**自动跟着修好**。**没有额外动作**，但同步点值得留意。

### 5·3 🟡 `TextMeshPro/Distance Field Offset` **原件就在本机**（现在没用上）

`bundle_fonts_assets_all/Shader/Shader_2094616344858530525.json` 就是它（4 张 TMP shader 在那个包里）。
而 `EffectExporter.CopyShaderBundle()` 只拷 `shaders_assets_all.bundle`（`:51`），**fonts 包没拷**；
运行时表 `WarpforgeShaderMap` 的注释（`:338`）说「我们的 TMP Essentials 里不存在」——
**那句对「工程里」成立，对「原包里有」不成立**。
⇒ **要把这 4 条从「近似」升成「精确」，办法是现成的**：把 `TextMeshPro/Distance Field Offset`
拆出来打进一个 shader 包 + `WarpforgeShaderMap.UseOriginal` 走原件；或者照它做一份带 `Offset -1,-1` 的
自建 shader。**本报告不动手**（超出本代理白名单：要动 shader 资产 / StreamingAssets 包，且必须跑 Unity）。
按铁律 11 记在这里：**要做，只是排在后面**。

### 5·4 🟡 另外两件有 TMP 的 prefab（同一条路会一起走，无副作用）

`WarpforgeVFX/Prefabs/HuntMark_IdleEffect.prefab` / `MarkerlightIdle.prefab` 各有 **1** 条 TMP
（3D `TextMeshPro`，带 `MeshRenderer`）。它们走的是同一段代码：渲染器那圈先导材质、TMP 这圈再核一次
（`MatCache` 命中 ⇒ 拿回同一份），**不会**产生第二份材质。

### 5·5 ⚠️ 没查清的

- `Distance Field Offset` 的**字节码**是否与 `Distance Field` 同一份（JSON 无字节码）—— 不影响本轮结论（属性表相同、pass 状态只差 `Offset`）。
- `ImportMaterial` 的 `MatFields`/撞名守卫对 TMP 这一族**实测没触发过**（9 个名字互不相同、且不与本 prefab 的渲染器材质重名）；若将来原版再冒出同名不同内容的 TMP 材质，会走既有的 `A845(b)` 指纹改名路（**未实测**）。

---

## 6. 主对话同步点要跑什么

### 6·1 跑的入口

```
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectExporter.RunListed \
  -logFile "d:/4/_tmp_view/export_t10_tmpmat.log"
```

⚠️ **`RunListed` 导的是整份 `ListedPrefabs`（现读 36 件）** —— 里面除 `CardPrefab` 外还有别的代理这两轮加的
OrkBuff / OrkBuff 同族 / 环境效果等。跑一次会把它们**一起重导**（`Export()` 幂等、固定路径覆盖写、guid 不变）。
本批只需要 `CardPrefab` 的话，**别自作主张改名单**（那是别人的账）。

### 6·2 看哪几行日志

```bash
grep "A1325" "d:/4/_tmp_view/export_t10_tmpmat.log"
```

期望在 `CardPrefab` 那一段看到（数字是判据，不是大概）：

| 日志片段 | 期望 |
|---|---|
| ``A1325 `CardPrefab` TMP `m_fontAsset`：接回 **38** / 留空 **0**`` | 38 / 0 |
| ``A1325 `CardPrefab` TMP `m_sharedMaterial`：接回 **38** 条`` | **38**，且带 `其中近似 shader 4 条`，`留空 0` |
| ``**是按近似 shader 替代的**`` | **恰好 4 条**，全部点名 `Pragati-Regular White_thick Offset for 3D` |
| ``TMP `m_sharedMaterial` … 导不成工程材质`` | **0 条** |
| `图集改绑回字体资产 N 条` | 期望 **0**（= `_MainTex` 本来就是字体图集；出现 N>0 说明名字/路径变了，要查） |
| `Image.m_Material：接回 5` / `Image.m_Sprite：接回 6` | 不变（回归对照） |

### 6·3 产物判据（`check_prefab_refs.py` **查不出这个缺陷**，别拿它当验收）

```bash
python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py        # 期望 rc=0（但它对本次改动是空转）
```

真正的判据 = **产物里 `m_sharedMaterial` 的不同 guid 个数 = 9**，且逐条对得上 §2·2 那张表：

```bash
python -I -X utf8 - <<'PY'
import io,re,collections
p=r'D:\4\Unity\MyGame\Assets\WarpforgeVFX\Prefabs\CardPrefab.prefab'
t=io.open(p,encoding='utf-8',errors='replace').read()
m=collections.Counter(re.findall(r'm_sharedMaterial: \{fileID: -?\d+, guid: ([0-9a-f]{32})',t))
print('distinct =', len(m), '（期望 9）· 合计', sum(m.values()), '（期望 38）')
for g,n in m.most_common(): print(f'  {n:2d}  {g}')
PY
```

期望分布（条数 → 材质名，用 `.meta` 的 guid 反查）：
`8 → Pragati-Regular Card Content` · `7 → … Extra Thick Outline` · `6 → Asar-Regular White-Card name` ·
`4 → … White_thick Offset for 3D` · `4 → … Thick Outline` · `4 → … Thin Outline` ·
`3 → … Card Unit type` · `1 → Pragati-Regular Atlas Material` · `1 → Asar-Regular Atlas Material`。

### 6·4 顺带对一眼（成本极低）

- `git status --porcelain` 看 `WarpforgeVFX/Materials/`：应新增 **4** 份 `.mat`
  （`Pragati-Regular Card Content` · `Asar-Regular White-Card name` · `… Thin Outline` · `… Card Unit type`），
  另外 **5** 份原地覆盖（guid 不变）。
- `WarpforgeVFX/Textures/Pragati-Regular SDF Atlas.png(.meta)` / `Asar-…`：**内容应无变化**（§5·1）。

---

## 7. 本轮做过的检查

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_m bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 错 · 编辑器 0 错** |
| `git diff --numstat EffectExporter.cs` | `262 / 25`（全是本轮 + 别人已落未提交的 W61 改动；行尾 `CRLF 4614 / LF 4614` = **一致，没翻行尾**） |
| `check_prefab_refs.py`（只读） | `rc=0`（现状：4 个字段全 0 坏引用）—— 但见 §6·3，它**看不见**本次缺陷 |
| Unity / 自检 | **没跑**（铁律 12 + 简报要求；重导在同步点做） |
