# W58 · 工具·文档线 — `T11` 补做：`Ruri.RipperHook` 实测（.NET 10）

> 执行代理 **W58-RURI** · 第五十八会话 · 2026-10-20。
> 判据 = `资料/普查产出_1015工具调研/工具调研报告_第二轮_免费工具.md` §二（Ruri 能力）＋ §五 A2/A3（跑法）
> ＋ `工具调研报告_第四轮_实跑AssetRipper.md` §一③（.NET 10 装法）。
> 口径：**只做不验** —— ⛔ 没跑 Unity / `*.Run` / player，⛔ 没截图，⛔ 没动 git，⛔ 没改正本。
> 样本只读；工具与产物全部落在 `d:/2/tools/`。

---

## 〇 · 结论（一句话）

🔴 **能用，而且吃得下整个游戏安装树。**

- 编译：`Ruri.RipperHook.CLI` 用 **.NET 10 SDK 10.0.402** 编译成功，**40.68 秒 / 0 错 / 249 警告**。
- 单 bundle 实跑：`shaders_assets_all.bundle`（837 KB）**3.805 秒**，**45/45 个 shader** 全部导出，**0 个 Dummy 占位**。
- 全安装树实跑：`d:/2/unity_run_ref`（2.0 GB / 84 bundles）**25.065 秒**，加载 **507,366** 个对象，导出 **422 个 `.shader`**，**0 个 Dummy**。
- 覆盖率：工程自有 shader `Everguild/*` 85 ＋ `Shader Graphs/*` 9 ＋ `Hidden/Everguild/*` 2 = **96/96 全导出**。
- 🔑 **不需要 Warpforge 专属 GameHook**（`Source/Ruri.GameHook` 是私有 submodule，clone 不到）。CLI 自动探测出
  `product=Warpforge version=1.0 engine=6000.2.6f2 -> decoder=(none)`，走**通用 Unity 路径**照样反编译。
- 代价：① **必须**把 `DOTNET_ROOT` 指到本地 .NET 10（系统只有 SDK 8、且缺 AspNetCore 10，不指会 `exit 150`）；
  ② 7 个 URP/XR 内部 shader 导出失败（空 blob），**不在我们自用的 96 个里**；
  ③ 编译产物目录 **4.4 GB**。

---

## 一 · 装了什么（版本 / 来源 / 路径）

| 东西 | 版本 / 提交 | 来源 | 路径 |
|---|---|---|---|
| **.NET 10 SDK** | **10.0.402**（runtime 10.0.13） | 官方 `https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json` → `dotnet-sdk-10.0.402-win-x64.zip`（**2026-10-16 已由第四轮装好，本次只复验**；安装包留档 `d:/2/tools/dotnet10_dl/dotnet-sdk-10.0.402-win-x64.zip`，300,755,826 B） | `d:/2/tools/dotnet10/` |
| **Ruri.RipperHook** | `ed62f85af4e5da83dabd514b29ee93ef7d34e86b`（2026-10-04） | `https://github.com/FractalTools/Ruri.RipperHook.git`（2026-10-15 克隆） | `d:/2/tools/toolsurvey/Ruri.RipperHook/` |
| ↳ AssetRipper submodule | `813fed1597bac674def7256ce633a227e7b27497`（2026-09-15） | `https://github.com/AssetRipper/AssetRipper`（本次 `git submodule update --init` 补拉） | 同下 `AssetRipper/` |
| ↳ ShaderDecompiler submodule | `af13b5dbbc1b59ac8cb4528f1af113d0c2e36c84`（2026-10-04） | `https://github.com/ShiyumeMeguri/Ruri.ShaderDecompiler.git`（本次补拉） | 同下 `Source/Ruri.ShaderDecompiler/` |
| ↳ Ruri.GameHook | **未拉（私有，404）** | — | — |
| ↳ FModel | **未拉（UE 用，不需要）** | — | — |
| **CLI 产物** | `Ruri.RipperHook.CLI.exe` | 本次 `dotnet build` | `d:/2/tools/toolsurvey/Ruri.RipperHook/Source/0Bins/Release/`（4.4 GB） |

**编译命令（可复现）**：

```bash
cd d:/2/tools/toolsurvey/Ruri.RipperHook
export DOTNET_ROOT="D:/2/tools/dotnet10"
export PATH="/d/2/tools/dotnet10:$PATH"
dotnet build Source/Ruri.RipperHook.CLI/Ruri.RipperHook.CLI.csproj \
  -c Release -p:PureRelease=true --nologo
```

- `-p:PureRelease=true` **是必须的**：它跳过对所有 `Source/Ruri.GameHook/Unity/**/*.cs` 与 `RuriTypeTree.tpk` 的引用
  （那些在私有 submodule 里）；同时保留 `ShaderDecompiler` / `PrefabOutlining` / `StaticMeshSeparation` 这些
  与游戏无关的 `[RipperFeature]`。
- 构建日志：`d:/2/tools/ruri_build_1020.log`。

⛔ 没有改全局 PATH、没有写注册表；所有调用都是**局部 env**。

---

## 二 · 实跑命令与产物

### 2·1 统一入口

```bash
cd d:/2/tools/toolsurvey/Ruri.RipperHook/Source/0Bins/Release
export DOTNET_ROOT="D:/2/tools/dotnet10"
./Ruri.RipperHook.CLI.exe <args>
```

> 🔴 **不设 `DOTNET_ROOT` 会失败**：系统只装了 SDK 8 / AspNetCore 8，而 CLI 的 `runtimeconfig.json` 要
> `Microsoft.AspNetCore.App 10.0.0` ⇒ `exit 150`（"To install missing framework…"）。本地 `d:/2/tools/dotnet10`
> 同时带 Core 10.0.13 / AspNetCore 10.0.13 / WindowsDesktop 10.0.13。

### 2·2 五次实跑

| # | 目的 | 命令要点 | 耗时 | 加载 | 导出 | failed | 产物 |
|---|---|---|---:|---:|---:|---|---|
| 1 | 列 Hook | `--list-hooks` | <1 s | — | 9 个 Hook id | — | stdout（exit **3** 是设计值） |
| 2 | **样本：shader bundle** | `--hook ShaderDecompiler --load shaders_assets_all.bundle --export … --types Shader --no-scripts` | **3.805 s** | 405 | **45** | `[]` | `d:/2/tools/ruri_out_1020/`（12 MB） |
| 3 | Prefab Outlining（菜单 bundle） | `--hook ShaderDecompiler --hook PrefabOutlining --load menusharedresources_assets_all.bundle …` | **2.281 s** | 355 | 21 | `[]` | `d:/2/tools/ruri_out_1020_prefab/` |
| 4 | Prefab Outlining（有 GameObject 的 bundle） | `--hook PrefabOutlining --load generalgamewindows_assets_all.bundle …` | **1.844 s** | 3,003 | 71（含 **65 个 `.prefab`**） | `[]` | `d:/2/tools/ruri_out_1020_prefab2/` |
| 5 | 全部 bundle 目录只导 Shader | `--load …/StandaloneWindows64 --types Shader` | **14.220 s** | 246,045 | 148 | `[]` | `d:/2/tools/ruri_out_1020_allshaders/`（23 MB） |
| 6 | **整个游戏根目录只导 Shader** | `--load d:/2/unity_run_ref --types Shader` | **25.065 s** | 507,366 | **422** | `[]`（⚠️ 见 §四.6） | `d:/2/tools/ruri_out_1020_rootshaders/`（73 MB） |
| 7 | 平台钉死复测 | `RURI_SHADER_PLATFORM=Vulkan --load …/globalgamemanagers` | 未计时 | 7,577 | 62 | `[]`（实际 8 条失败） | `d:/2/tools/ruri_out_1020_gm_vk/` |

- `--list-hooks` 的输出（9 个，**全是与游戏无关的宿主能力**）：
  `DisassemblyExporter` · `HumanoidToGeneric` · `Il2CppMethodDump` · `PrefabOutlining` · `ShaderDecompiler` ·
  `SkipProcessingAnimation` · `SkipStreamingAssetsCopy` · `StaticMeshSeparation` · `TypeFilterExport`。
- 每次运行的 stdout（JSON 摘要）与 stderr（详细日志）都在 `d:/2/tools/ruri_run_1020*.out/.err`。

### 2·3 产物形态

```text
d:/2/tools/ruri_out_1020_rootshaders/
├─ AuxiliaryFiles/path_id_map.json
└─ ExportedProject/
   ├─ Assets/                 ← 422 个 .shader + 6,644 个 .hlsl + .meta
   ├─ Packages/
   └─ ProjectSettings/
```

- **`.shader` 是真反编译体，不是占位**：`//DummyShaderTextExporter` 在 45 样本与 422 全量里 **均为 0 条**。
  单文件 **1,439 B ~ 125,387 B**（中位 8,242 B）。
- 每个 shader 旁边/子目录里还有**逐变体 HLSL**：全量 **6,644 个 `.hlsl`**。
  例（`Universal Render Pipeline/Particles/Unlit`）日志：`done (183/183 passes, 183 variant files)`。
- 反编译链与调研一致：DXBC/DXIL → SPIR-V → HLSL（所以生成代码里能看到 `gl_Position`、`register(b0, space0)`）。
- 命名：**从整个游戏根加载时**多数落在 `Assets/Shader/<可读名>.shader`（304/422），
  另有 `Assets/Resources/shaders/`（20）和 `Assets/<hash>.shader`（98，重复资产带 `_0` 后缀）；
  ⚠️ **只喂单个 bundle 时**全部是 `<hash>.shader`（没有路径上下文）——生产用法应从**游戏根**加载。

---

## 三 · 保真度实测（拿样本逐项对原版解包 JSON）

判据 = `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/*.json` 的 `m_ParsedForm`。

| 检查项 | 结果 |
|---|---|
| Shader 名字（`Shader "…"` vs `m_ParsedForm.m_Name`） | **45/45 全等，缺 0 多 0** |
| Properties 数量 | **45/45 数量相等** |
| Properties 名字集合 | **45/45 原版每个属性名都在** |
| 全量 422 个的 SubShader 数量 | **422/422 与原版相等** |
| 全量 422 个的 Pass 数量 | **422/422 与原版相等** |
| 渲染状态抽样（`Everguild/Sprites/Sprite Additive`） | 原版 `srcBlend=1/destBlend=1, zWrite=0, culling=0` → 导出 `Blend One One, One One` / `ZWrite Off` / `Cull Off` ✅ |
| 常量缓冲符号完整度（45 样本） | `packoffset` 464 处，其中 **248 处（53.4%）标成 `_Unmapped`** —— 没拿到引擎侧符号名，但**算法是真的** |
| Dummy 占位 | **0**（45 样本与 422 全量都是 0） |

> 结论：**结构（SubShader/Pass/属性/名字/渲染状态）与原版逐项对齐；HLSL 是真实反编译结果。**
> 唯一明显缺口是**部分常量缓冲槽没有符号名**（`UnityPerMaterial_Unmapped`），属于「没有游戏专属 metadata」的预期损失，
> 不影响读懂算法。

---

## 四 · 与期望的差异

1. ✅ **期望「shader 从 CompressedBlob 反编译，不再 Dummy」** —— 达成。45 样本 0 Dummy；全量 422 个 0 Dummy。
2. ✅ **期望「headless CLI 直出 Unity 工程」** —— 达成：`Assets/` + `Packages/` + `ProjectSettings/` + `.meta` 齐全。
3. ✅ **期望「Prefab Outlining 把平铺 GameObject 还原成 prefab」** —— 达成：`generalgamewindows` 一场里
   日志逐条 `Recreating prefab for …`，最终落盘 **65 个 `.prefab`**（YAML 真层级，不是内联）。
4. ⚠️ **`--export-scene` 对我们不通** —— 与调研预判一致：CLI 自己要求
   `--load <gameRoot>（含 Endfield_Data）+ VFS-game --hook`；Warpforge 不是 VFS 游戏，**没试**。
5. ⚠️ **StaticMeshSeparation 没跑** —— 本批样本没有需要它的场景；`battleprefabs_vfxandmisc` 78 MB，未纳入小样本。
6. 🔴 **JSON 摘要的 `"failed":[]` 不可信** —— 第 6 次跑 stderr 里实际有 **7 个 shader** 打出
   `Failed to export '…' (ShaderExportCollection)`（`ClusterDeferred` / `LensFlareScreenSpace` / `StencilDeferred` /
   `XR/XRMirrorView` / `XR/XRMotionVector` / `XR/XROcclusionMesh` / `BlitHDROverlay`），JSON 仍是 `failed:[]`。
   第 7 次 `RURI_SHADER_PLATFORM=Vulkan` 反而变成 **8 个失败**（多 `DBufferClear`）。
   ⇒ **自检必须 grep stderr，不能只看 JSON 的 failed 数组。**
7. ⚠️ **23 个解包 shader 名字没出现在全量导出里**，拆开是：
   - **7 个** = 上面 URP/XR 内部件（平台 blob 只有 5 字节 / 无法读出 pass）；
   - **16 个** = Unity 自带内置 shader（`UI/Default`、`Sprites/Default`、`Skybox/*`、`Legacy Shaders/*`、`Mobile/*`、
     `Particles/Standard Unlit`）—— AssetRipper 识别为引擎内置、**根本不进导出队列**（日志里连 `Exporting` 都没有）。
   - ✅ **工程自有的 96 个（`Everguild/*` + `Shader Graphs/*` + `Hidden/Everguild/*`）缺 0**。
8. ⚠️ **没在 Unity 里编译过**（红线不跑 Unity）。生成的 HLSL 带 `#pragma use_dxc`、`register(bN, spaceN)`、
   SPIRV-Cross 风格的 `gl_Position` —— **能不能被 Unity 6000 的 shader compiler 接受，未验**。
9. ✅ **不需要 GameHook** —— 调研 §六.1 的「私有子模块缺席 / 自定义容器」这条担心，在**读取**上不成立；
   CLI 打印 `decoder=(none)` 但通用 Unity 路径照常工作。
10. ⚠️ **第一次 `--list-hooks` 的退出码是 3**（设计如此），不是失败。

---

## 五 · 怎么复用 / 自检

### 5·1 复现（三步）

```bash
# ① 确保 SDK 10（系统 dotnet 不行，必须指本地）
export DOTNET_ROOT="D:/2/tools/dotnet10"

# ② 进 CLI 目录
cd d:/2/tools/toolsurvey/Ruri.RipperHook/Source/0Bins/Release

# ③ 全量导 shader（从游戏根，才有可读文件名）
./Ruri.RipperHook.CLI.exe \
  --hook ShaderDecompiler \
  --load "d:/2/unity_run_ref" \
  --export "d:/2/tools/ruri_out_YYYYMMDD_rootshaders" \
  --types Shader --no-scripts
```

单 bundle 试水：

```bash
./Ruri.RipperHook.CLI.exe --hook ShaderDecompiler \
  --load "d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/shaders_assets_all.bundle" \
  --export "d:/2/tools/ruri_out_YYYYMMDD_sample" \
  --types Shader --no-scripts
```

Prefab：

```bash
./Ruri.RipperHook.CLI.exe --hook PrefabOutlining \
  --load "d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/generalgamewindows_assets_all.bundle" \
  --export "d:/2/tools/ruri_out_YYYYMMDD_prefab" --no-scripts
```

### 5·2 自检判据（本次的「绿」长什么样）

| 判据 | 期望 |
|---|---|
| 样本 bundle `.shader` 数 | **45** |
| 其中含 `//DummyShaderTextExporter` | **0** |
| 45 个 `Shader "…"` 名字 vs 解包 `m_ParsedForm.m_Name` | **差集为空（双向）** |
| 45 个 Properties 数量 | 与原版 **45/45 相等** |
| 全游戏根 `.shader` 数 | **422**（本次 build）；工程自有 96 个 **缺 0** |
| stderr 的 `Failed to export` | 本次 **7 条**（全是 URP/XR 内部件） |
| stdout JSON `failed` | ⚠️ **恒为 `[]`，不能当判据** |

一条现成自查（Python，只读）：

```python
import glob, re
files = glob.glob(r'd:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/**/*.shader', recursive=True)
names = set()
for p in files:
    s = open(p, encoding='utf-8-sig').read(400)
    m = re.search(r'Shader\s+"([^"]+)"', s)
    if m: names.add(m.group(1))
print(len(files), len(names), sum('DummyShaderTextExporter' in open(p, encoding='utf-8-sig').read() for p in files))
```

### 5·3 复用注意

- **AGPL-3.0**：本项目是个人学习用途、内部当工具用不受分发条款约束；**将来对外发布要重新评估**（调研 §六.7 同一口径）。
- **产物别落 `d:/4`**（本次全部在 `d:/2/tools/`）。
- 单 bundle 模式文件名是 hash；**要可读名就用游戏根**。
- 构建产物 4.4 GB（`Source/0Bins/` 已被仓库 `.gitignore` 忽略，不影响 git 状态）。

---

## 六 · 没查清的部分（⛔ 不许当结论）

1. **Unity 是否能编译 / 渲染一致** —— 完全没验（红线不跑 Unity）。这是下一步最该做的：拿 1~2 个 `Everguild/*`
   的 `.shader` 丢进独立 Unity 工程，`ShaderUtil` 编译 + 与 `WarpforgeShaderLoader` 的原件并排比。
2. **静态网格拆分（StaticMeshSeparation）** —— 没跑。
3. **Prefab 里的 ParticleSystem / SpriteMask 还原度** —— 只确认了 65 个 `.prefab` 能输出、YAML 层级是真的；
   没有逐模块核对（这是第二轮报告 §三.3 的最大未知）。
4. **7 个 URP/XR 内部 shader 的失败根因** —— 只看到「没有可用 pass / blob 极短」；`RURI_SHADER_PLATFORM=Vulkan`
   没救回来，反而多失败 1 个。没读源码定位。
5. **ComputeShader** —— 加载了 24~56 个，但 `--types Shader` 没导；未单独试。
6. **4.4 GB 解包树 vs 2.0 GB 安装树** —— Ruri 吃的是**游戏安装目录**（`d:/2/unity_run_ref`，2.0 GB），
   **不是** `d:/2/新解包资源/assets_full` 那棵 4.4 GB 解包树；后者是 UnityPy 的产物，Ruri 不直接读。
   → 「能不能吃 24.7 万文件树」这条改写成「**能吃 2.0 GB 安装树 / 50 万对象，25 秒**」，已经实测。
7. **`--load` 目录里 168 个 AssetBundle 与 84 个 bundle 的差异** —— JSON 里 `AssetBundle: 168`，`ls` 只有 84 个
   `*.bundle`；可能有重复计数或额外文件，未深究。
