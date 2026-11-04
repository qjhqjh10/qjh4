# B141 · W130 · 16-10 `Orbital 5 repeat.prefab` + 16-12 `GraphicsSettings.asset`

- 批次 B141 · 会话 128 · 执行写手 `B141-W21`
- 任务来源：`判据/B141_W130_对账_资源.md` §一 16-10 / 16-12（§二 包 W130-O4 / W130-O5）· `报告/B138_审计汇总_问题清单.md` §16 第 461 / 463 行
- 改动范围：**2 个资源文件**（纯 YAML，零 `.cs`） + 本报告
- ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表 / 活状态表 / CLAUDE.md

---

## 〇、一句话结果

| 编号 | 结果 | 改后值 | 差异 |
|---|---|---|---|
| 16-10 | ✅ 已改 | prefab 根 `m_Name: 'Orbital 5 repeat '`（带尾随空格） | **0** |
| 16-12 | ✅ 已改 | `m_VideoShadersIncludeMode: 1` + `m_ShaderDefinesPerShaderCompiler` 2 组（platform 4 / 18，Tier1-3 各 `[80070152, 64]`） | **0** |

两条都是 **纯资产改动**，不涉及 `.cs` ⇒ **零条自检**（未跑 typecheck，也未跑任何 Unity 宿主）。

---

## 一、16-10 `Orbital 5 repeat.prefab` 根节点名尾随空格

### 1.1 真身路径（现 `find` 确认，⛔ 未照抄简报）

```
d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Orbital 5 repeat.prefab
```
- 全工程**仅此一份**（`find ... -name "Orbital 5 repeat.prefab"` = 1 命中）
- 同目录 `.meta`：`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Orbital 5 repeat.prefab.meta`（本次**未动**）
- ⚠️ `git check-ignore` 命中 `.gitignore:26` 的 `/Unity/MyGame/Assets/WarpforgeVFX/*` ⇒ **该 prefab 不入 git**，改动只在磁盘上（见 §三·坑 3）

### 1.2 原版判据（现读，非只信报告）

| 判据源 | 现读值 |
|---|---|
| `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/GameObject/Orbital 5 repeat.json` | `m_Name = 'Orbital 5 repeat '`（尾随空格 1 个） |
| 同上 `…/Orbital 5 repeat_7859492807258470600.json` | `m_Name = 'Orbital 5 repeat '`（第二份路径标识，同值） |
| `d:/2/tools/assetripper_out/ExportedProject/Assets/Orbital 5 repeat.prefab:18` | `  m_Name: 'Orbital 5 repeat '`（YAML 因尾随空格而加单引号） |
| 同 prefab 其余 7 个节点名 | `Glow init` / `More Glow` / `Glow long` / `Fire` / `Smoke dark linger` / `Debris up` / `Shockwave (2)` —— **均无尾随空格** |

⇒ 需要带尾随空格的**只有根节点**；我们 8 个节点名里其余 7 个本来就一致。

### 1.3 根节点身份确认（⛔ 没有按“第一个 GameObject”猜）

- 我们的 prefab 共 **8 个 `GameObject`（`!u!1`）块**，文件里**不是**根在最前（第一块是子件 `Fire`）
- 按 **`!u!4` Transform 的 `m_Father: {fileID: 0}`** 定位根 Transform = `&523260545690070272`（第 4926 行起）
- 它的 `m_GameObject` = `&981841833556404387` ⟶ 该 GameObject 的 `m_Name` 在 **第 4920 行** ⇒ 改的就是它
- 改前全文件 `m_Name: Orbital 5 repeat` 精确命中 **1 处**（即根节点）

### 1.4 改动（1 行）

| 行 | 改前 | 改后 |
|---|---|---|
| 4920 | `  m_Name: Orbital 5 repeat` | `  m_Name: 'Orbital 5 repeat '` |

- 行号**未漂移**（改的是同一行内容，不加行）
- **文件名保持 `Orbital 5 repeat.prefab` 不变**，理由两条（都有判据）：
  ① **原版自己的导出件就是不带空格的文件名** —— `d:/2/tools/assetripper_out/ExportedProject/Assets/Orbital 5 repeat.prefab`（Windows 落不了尾随空格文件名，AssetRipper 也只能这么落）；bundle 侧 `GameObject/Orbital 5 repeat.json` 同样不带
  ② 工程里 `effect_index.json` 的键是 **Windows 文件名**，且 `EffectLibraryBuilder.JoinKey` 明确用 `Trim()` 对齐两侧（`WarpforgeArena1/Editor/EffectLibraryBuilder.cs:79`）⇒ 改文件名只会制造新的不匹配，而**不会**让 `m_Name` 更接近原版
- ⚠️ R9 报告写的「+ 文件名」这一半**未做**，理由如上；这**不是**遗漏，是判据不支持（原版导出件也如此）

### 1.5 改后验证（二进制读回）

| 项 | 改前 | 改后 |
|---|---|---|
| 字节数 | 973,279 | **973,282**（+3 = 一对单引号 + 1 个空格） |
| `\n` 计数 | 40,420 | **40,420**（不变） |
| `\r\n` 计数 | 0 | **0**（行尾未被翻） |
| 裸 CR（`\r`−`\r\n`） | 0 | **0** |
| sha1 | `af099e93af06503f2698048eceda742b1410d079` | `7c1c3e8447d69632da164f08b962123cec2067e5` |
| 现读第 4920 行 | — | `  m_Name: 'Orbital 5 repeat '`（精确命中 1 处；trim 形态命中 **0** 处） |

### 1.6 行为影响（为什么不会回归）

三处消费方全部已经按「Trim 后比 / 逐字比原版名」处理，**名字变得与原版逐字一致只会更对**：

| 消费方 | 现读 | 影响 |
|---|---|---|
| `WarpforgeVFX/Runtime/WFEffectModule.cs:225-229` | 路径第一段 `n0 == root.name.Trim()`（两侧都 Trim） | 无变化（本来就能对上） |
| `WarpforgeArena1/Editor/EffectLibraryBuilder.cs:79` `JoinKey` | `name.Trim()` | 无变化 |
| `Resources/EnvBlendables.json:22170-22240, 28235, 29070` | 数据里本来就是 `"leaf": "Orbital 5 repeat "`、`"s": "Orbital 5 repeat "`（带空格） | **改后反而逐字命中** |
| `WarpforgeArena1/Editor/EffectExporter.cs:909` `ListedPrefabs` 条目 | 字面量就是 `"Orbital 5 repeat "`（注释「⛔ 别 Trim」） | 改后逐字命中 |

### 1.7 同类（举一反三 · 只读普查，⛔ 未改）

- 原版该 bundle 里 `m_Name` **带尾随空格的 GameObject 实例 94 个 / 去重 39 个名字**（`Axe ` · `Fire ` · `Impact ` · `BulletImpact ` · `Orbital 5 spawner ` …）
- 但其中**只有 `Orbital 5 repeat ` 一个是「独立 prefab 的根」**（按「我们的同名 prefab 文件是否存在」判）：**1 / 1，改后精确命中，剩 0**
- 其余 38 个是**别的 prefab 里的子节点名**，属另一类（子节点名保真），**本轮不在 16-10 判据内、未核未改** —— 若要收，需要按 prefab 逐份比对（可另开账，见 §四·发现 4）

---

## 二、16-12 `GraphicsSettings.asset`（VideoShaders 模式 + ShaderDefines）

### 2.1 真身路径

```
d:/4/Unity/MyGame/ProjectSettings/GraphicsSettings.asset
```

### 2.2 原版判据（现读三种源，互相印证）

| 判据源 | 现读 |
|---|---|
| `d:/2/新解包资源/assets_full/globalgamemanagers/GraphicsSettings/GraphicsSettings_7.json` | `m_VideoShadersIncludeMode = 1`；`m_ShaderDefinesPerShaderCompiler = [{shaderPlatform:4, defines_Tier1/2/3:[80070152,64]}, {shaderPlatform:18, defines_Tier1/2/3:[80070152,64]}]` |
| `d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/GraphicsSettings.asset:28` | `m_VideoShadersIncludeMode: 1`（AssetRipper 重新序列化时**丢掉了** `m_ShaderDefinesPerShaderCompiler`，因为它不认识该类型 ⇒ 不能拿这一份当第二组判据） |
| `d:/2/unity_run_ref/Warpforge_Data/globalgamemanagers` **原始字节**（偏移 226,812） | 实测 `[4, 2, 80070152, 64, 2, 80070152, 64, 2, 80070152, 64, 18, 2, 80070152, 64, …]` ⇒ 结构 = `shaderPlatform` + 三个 **带长度前缀的 uint32 向量**（长度=2），**不是**标量 |
| `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_player_nondevelopment_mono/UnityPlayer.dll` | 序列化字段名原样存在：`m_ShaderDefinesPerShaderCompiler`（偏移 31,219,890）、`shaderPlatform` / `defines_Tier1` / `defines_Tier2` / `defines_Tier3`（偏移 31,221,160 起）⇒ **Unity 6000.3 仍认这套名字** |

⚠️ **没有**用 `m_TierSettings: []` 那套 Unity 6 模板当判据 —— Unity 6 的空工程根本不写这个字段（`Data/Resources/PackageManager/ProjectTemplates/com.unity.template.3d-high-end-17.0.7.tgz` 里那份 `GraphicsSettings.asset` 通篇无该键），所以「我们文件里没有」**不等于**「Unity 6 不支持」，只说明**空数组时它不落盘**。

### 2.3 改动（1 处替换 + 1 段新增 21 行）

| 位置 | 改前 | 改后 |
|---|---|---|
| 第 28 行 | `  m_VideoShadersIncludeMode: 2` | `  m_VideoShadersIncludeMode: 1` |
| 第 97–117 行（新增，紧随 `m_TierSettings: []` 之后） | 无 | 见下 |

```yaml
  m_ShaderDefinesPerShaderCompiler:
  - shaderPlatform: 4
    defines_Tier1:
    - 80070152
    - 64
    defines_Tier2:
    - 80070152
    - 64
    defines_Tier3:
    - 80070152
    - 64
  - shaderPlatform: 18
    defines_Tier1:
    - 80070152
    - 64
    defines_Tier2:
    - 80070152
    - 64
    defines_Tier3:
    - 80070152
    - 64
```

- 结构判据：每元素 = `shaderPlatform` + 三个 `defines_TierN`，后者是 **uint32 向量的块序列**（与 §2.2 原始字节的「长度前缀 + 2 个字」逐一对应）
- 位置判据：放在 `m_TierSettings: []` 之后（原版 JSON 的字段序也是 `…Tier1/2/3 → m_ShaderDefinesPerShaderCompiler → …`）；Unity YAML 按字段名读取，顺序不影响
- **只动这两处**：`git diff --numstat` = `22 1`（22 增 / 1 删，正好 = 21 行新块 + 1 行替换）
- ⛔ **未做**「顺手发现 #3」的 25 个内建 shader id（`判据…§四·3`）—— 不在本任务的两条判据内，且它们在 Unity 6 是否仍可解析**未验证**，按原样留给 W130-O5 的负责方（见 §四·发现 2）

### 2.4 改后验证

| 项 | 改前 | 改后 |
|---|---|---|
| 字节数 | 6,282 | **6,621**（+339） |
| `\n` 计数 | 120 | **141**（+21 = 新增行数，无额外空行） |
| `\r\n` 计数 | 0 | **0**（行尾未被翻） |
| 裸 CR | 0 | **0** |
| sha1 | `4bdb6bcde69af16d6e6688a27b30e4be8309edb8` | `c2a5adc25e8f43b42148f8c7cfd7b25a4dc62749` |
| 整文件 YAML 解析（pyyaml，剥掉 `%TAG`/`!u!` 头） | — | **解析通过**，现读 `m_VideoShadersIncludeMode = 1`、`m_ShaderDefinesPerShaderCompiler = 2 组 / platform 4,18 / Tier1-3 各 [80070152, 64]`，`m_TierSettings = []`、`m_AlwaysIncludedShaders = 58` 条（未被波及） |

### 2.5 原版值 vs 改前 vs 改后（差异归零）

| 字段 | 原版 | 改前（我们） | 改后（我们） | 差异 |
|---|---|---|---|---|
| `m_VideoShadersIncludeMode` | 1 | 2 | **1** | **0** |
| `m_ShaderDefinesPerShaderCompiler` | 2 组：platform `4`、`18`；各 `defines_Tier1/2/3 = [80070152, 64]` | **整块缺失** | **2 组同值** | **0** |
| 组数 / 顺序 | 4 → 18 | — | 4 → 18 | **0** |

### 2.6 ⚠️ 必须 Unity 才能确认的一步（未做，如实记）

R9 已标注、本报告复核后**仍然成立**：`serializedVersion: 16` 下我们**手写**了这块 YAML，而 Unity 6 默认不落盘空数组。
⇒ **需要一次 Unity 存盘 + 回读**，确认：
① `m_ShaderDefinesPerShaderCompiler` **没有被 Unity 丢掉**（若被丢，说明 Unity 6 只读不写 / 或字段已废弃，届时按「原版值仍然正确、只是编辑器不落盘」记录，⛔ 不要反向改判据）；
② 预置值 `80070152 / 64` 没有被重算（这两个数是原版 build 的 define 位图，Unity 只应在**用户改设置**时重算）。
本条**不阻塞**本次改动：盘上文件已是原版逐字值。

---

## 三、坑与红线说明

1. **`WarpforgeVFX/*` 被 gitignore**（`.gitignore:26`）⇒ 16-10 的改动**不会进 git**，只在磁盘上；且这批 prefab 由 `EffectExporter` 生成（保存口 `EffectExporter.cs:1754 PrefabUtility.SaveAsPrefabAsset`）。
   ⚠️ **重跑 `EffectExporter` 后要复查根名是否又被打回 trim**（Unity 可能把 prefab 根名同步成文件名）。若复现，正解是改**导出器**（本任务白名单外），不是在 YAML 里反复手改。
2. **没有按「文件里第一个 GameObject」当根** —— 本 prefab 的根在第 4906 行块（第 3 个块），第一块是子件 `Fire`。判据是 `Transform.m_Father: {fileID: 0}` → `m_GameObject`。
3. **两份 `GraphicsSettings` 判据不等价** —— 原版 `globalgamemanagers` JSON 有该字段，AssetRipper 重新序列化的 `ProjectSettings/GraphicsSettings.asset` **没有**（它不认识该类型）。只拿 AssetRipper 那份核会得出「原版也没有」的**假结论**。
4. **YAML 行尾**：两份文件本来就是 **LF（CRLF=0）**，改后仍 LF；已按二进制（`io.open(p,'rb')`）复核 `\r\n` 与裸 `\r` 计数。
5. 本次**未**在文件里打 `✅ 已复刻` 注释行：`ProjectSettings/*.asset` 与 prefab YAML 由 Unity 重写/生成，加注释行会被抹掉且会让 `GraphicsSettings.asset` 行号漂移。标记以本条为准：

> ✅ 已复刻（2026-11-04 · B141 · `判据/B141_W130_对账_资源.md` §一 16-10 / 16-12 + `GraphicsSettings_7.json` + `Orbital 5 repeat.json`）

---

## 四、顺手发现（⛔ 未改，均在本任务白名单之外）

1. **R9 的 `m_AlwaysIncludedShaders` 数字自相矛盾** —— 报告写「54 条 = 10 内建 + 48 自建」，而现读是 **58 条 = 10 内建 + 48 自建**（10+48=58）。数字来源处写错，不影响 16-07 的结论（前 10 个内建 id 确实逐 id 相同），但交接/账里引用这组数会继续传错。
2. **「顺手发现 #3」的 25 个内建 shader id 未做**（`判据…§四·3`）：原版 `m_AlwaysIncludedShaders` = 40 条（35 内建 + 5 工程自建），我们 58 条（10 内建 + 48 自建）。缺的 25 个 id 在 Unity 6 是否解析得到**未验证** ⇒ 需 Unity 读回确认，属独立活（W130-O5 的「顺带项」）。
3. **16-10 的根名改动只存在于磁盘**（见 §三·坑 1）：prefab 目录被 gitignore，`git status` 看不到它；任何「按 git 判有没有做」的复核都会把它判成「没做」。
4. **尾随空格是系统性的（39 个原名 / 94 个实例）**：本次只修了唯一一个「独立 prefab 根」。其余 38 个是别的 prefab 的**子节点名**，我们导出时可能同样被 trim。若要把「节点名保真」做全，应做一次「原版 GameObject 名 vs 我们 prefab 节点名」的全量比对（建议单独立账，别塞进 16-10）。
5. `WFEffectModule.cs:216-229` 的注释里写着「工程里那件 prefab 叫 `"Orbital 5 repeat"`（Windows 文件名落不了尾随空格）」—— 本次改后**对象名**已带空格，但注释描述的是「当时的状态」，不构成错误；若后续会话读它会以为「名字还是 trim 的」，可考虑下次改到该文件时补一句现读状态（本任务白名单外，未动）。

---

## 五、本次改动的文件清单

| 文件 | 改动 |
|---|---|
| `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/Orbital 5 repeat.prefab` | 第 4920 行 `m_Name` 加尾随空格（1 行；行数与行尾不变） |
| `d:/4/Unity/MyGame/ProjectSettings/GraphicsSettings.asset` | 第 28 行 `m_VideoShadersIncludeMode: 2→1`；第 97–117 行新增 `m_ShaderDefinesPerShaderCompiler`（21 行） |
| `d:/4/Unity/资料/完全复刻/报告/B141_W130_OrbitalGraphics.md` | 本报告（新建） |
