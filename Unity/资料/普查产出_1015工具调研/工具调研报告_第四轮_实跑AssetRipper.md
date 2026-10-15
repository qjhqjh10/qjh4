# 工具调研 · 第四轮：**实跑 AssetRipper**（2026-10-16 · 主对话亲自下场）

> 前三轮都是「读源码/读文档」得出的结论；**这一轮是把工具真的跑起来、拿产物说话**。
> 用户 2026-10-16 原话：「**可以导出的请你尝试**」。

---

## 一、怎么跑起来的（**可复现的完整链**，⛔ 别再重新摸一遍）

| 步 | 做了什么 | 关键点 |
|---|---|---|
| ① | **网络实况** | 🔴 `github.com/…/releases/download/…` 那条资产链 **IPv6 不通** ⇒ **必须 `curl -4`**（子代理连测 5 次 206，我们之前 `http=000` 两次全是这个原因）；`api.github.com` / `raw.githubusercontent.com` / `nuget.org` / `.NET` 三条 CDN **都通**；`archive.org`、`nuget.samboy.dev`、`dot.net`、`WebFetch` **全被挡** |
| ② | **本来想下预编译包** | 44 MB 那个 zip 在 IPv4 下也是**龟速**（~34 KB/s），续传多次只到 24 MB ⇒ **放弃，改自建** |
| ③ | **装 .NET 10 SDK**（不用系统安装） | `https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json` → `dotnet-sdk-10.0.402-win-x64.zip`（300 MB）⇒ **解压到 `d:/2/tools/dotnet10/`**（解压即用，⛔ 不碰系统 PATH/注册表）|
| ④ | **clone 源码** | `git clone --depth 1 https://github.com/AssetRipper/AssetRipper.git` ⇒ `d:/2/tools/assetripper_src/AssetRipper`（⭐ **github 与 gitcode 同一个 HEAD**）|
| ⑤ | **编译** | `d:/2/tools/dotnet10/dotnet.exe build Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -p:PublishAot=false` —— **29 秒、0 错**（⚠️ 它默认 `PublishAot=true`，那要 MSVC 工具链；我们不发布，关掉即可）|
| ⑥ | **无头启动** | `dotnet AssetRipper.GUI.Free.dll --headless --port 5599 --log --log-path …` |
| ⑦ | 🔴 **两个 HTTP 入口**（⚠️ 它**没有** `--input/--output` 这种命令行；`Arguments.cs` 里只有 `--port/--log/--headless/--local-web-file`）<br>· `POST /LoadFolder` 表单 `Path=<游戏目录>`<br>· `POST /Export/UnityProject` 表单 `Path=<输出目录>` | 表单字段名 = **`Path`**（`Pages/Commands.cs` 的 `PathFormData`）|

**实跑记录**：加载 `d:/2/unity_run_ref/`（4.4 GB）→ 处理完 → 导出 **17,791 个资产 / 5.9 GB / 约 3 分钟**。

---

## 二、🔴 产物实据（**这是本轮的结论，全部亲读**）

### 2·1 材质 —— ✅ **真的提得到，比前三轮说的好**
`ExportedProject/Assets/**/*.mat` = **1083 个**。样例 `Aeldari death trail.mat` 亲读：
- `m_Shader: {fileID: 4800000, guid: 647ee868ff…}` —— **指向真实 shader 资产（按 GUID 连上了那 218 个 `.shader`）**
- **恢复了 keyword**：`m_ValidKeywords: [_NOISECHANNEL_A, _SURFACE_TYPE_TRANSPARENT]`
- `m_CustomRenderQueue: 3000` · `stringTagMap: {RenderType: Transparent}` ·
  `disabledShaderPasses: [DepthOnly, SHADOWCASTER, MOTIONVECTORS]`
- `m_SavedProperties` **有值**（`m_TexEnvs: _MainTex:` …）

🔑 **对我们意味着什么**：这一族数据（**渲染队列 / keyword / pass 禁用 / 材质属性**）正是本工程**手工扒了几个月**的东西
（`mat_renderqueue.tsv` · `original_shader_blend.tsv` · `shader_renderqueue.tsv` · `资料/战斗规则与数值_出处.md`）。
⚠️ 但**它是另一个来源**，与我们的表**可能不一致** ⇒ 用法应当是**互相对账**，⛔ 不是直接替换（铁律 6）。

### 2·2 Shader —— ✅ 218 个，但 **属性表真、渲染体假**
亲读 `Custom_EditorIcon.shader`：`Properties { _MainTex ("Texture", 2D) = "white" {} }` **是真的**
（印证第三轮b 的判断：占位时用原 shader 的 `m_ParsedForm.m_PropInfo` 重建属性表）；
而 SubShader 体是 **`//DummyShaderTextExporter`** 生成的**通用 vert/frag pass**（uniform 名字对，**算法是假的**）。
⇒ **材质能对上名字/值，但渲不出来**。要真渲染仍得走我们那条「运行时取原件」或反编译 DXBC。

### 2·3 场景 —— ✅ **13 个战场全在，而且【连 HUD 一起】**
`Data/Scenes/Battle Scenarios/` 下 **13 个 `Battle Arena *.unity`**（+ `MainMenu Warpforge` / `Intro` / `Simple Transition` / `Reset Scene`，共 17 个）。
亲读 `Battle Arena 1.unity`（**6.4 MB**）：`BattleHud` ✓ · `Safe area FrontCanvas` ✓ · **`AboveShader`（提示条）** ✓ ·
`MulliganText` ✓ · `TurnBtn` ✓ · **`Anim Anchors`** ✓ · `ChatButton` ×8 ✓
⇒ 🔴 **这跟我们的 `battlearena1_manifest.json` 是两码事**：我们那份**只有 arena 的 mesh/粒子**（30 meshes / 34 particles），
**不含 HUD**；而 AssetRipper 这份是**整个战斗场景的完整层级**（U 是**内联 GameObject**，prefab 还原另说）。
⚠️ `EndBattleDoors` 在场景里 **0 命中** —— 待核（我们文档里它是每个场景都有的组件实例）。

### 2·4 粒子 —— ✅ **不是「没有」：作为 `ParticleSystem` 组件被完整导出**
`ParticleSystem` 出现在 **1018 个文件**（**1004 个 prefab** + 场景）；`Battle Arena 1.unity` 里有 **60 个**。
亲读第一块：`lengthInSec: 5` · `simulationSpeed: 0.8` · `cullingMode: 3` · `looping: 1` · `playOnAwake: 1` ·
`InitialModule.startLifetime: {minMaxState: 3, scalar: 5, minScalar: 7, …}` —— **都是真值**。
**模块覆盖**：`InitialModule` · `EmissionModule` · `ShapeModule` · `VelocityModule` · `ClampVelocityModule` ·
`ForceModule` · `ColorModule` · `ColorBySpaceModule` · `SizeModule` · `SizeBySpeedModule` · `RotationModule` ·
`RotationBySpeedModule` · `ExternalForcesModule` · `NoiseModule` · `CollisionModule` · `TriggerModule` ·
`LightsModule` · `TrailModule` · `CustomDataModule` · `UVModule` —— **20 个全在**。
**曲线**：那一块里 `m_Curve: []`（空）178 处、**非空 8 处** ⇒ **有曲线的没被丢**。
🔴 **这条更正前两轮的措辞**：前两轮写「粒子一件都没有」—— 那说的是「**没有专门的粒子导出器**」（对），
但**不等于「提不到粒子数据」**：作为场景/prefab 组件，它**提得到**。

### 2·5 🔴 一条**能直接量化的对比线索**（⚠️ 口径未对齐，⛔ 别当结论）
同一场 `battlearena1`：**我们的 manifest = 34 个 particles** vs **AssetRipper 场景里 = 60 个 `ParticleSystem` 组件**。
⚠️ **两者口径不同**（我们那份可能是**去重后的 prefab**，它数的是**组件实例**）⇒ **这是待查的线索，不是「它比我们多一倍」**。

---

## 三、⚠️ 本轮的边界（⛔ 别当已验证）

1. **没有在 Unity 里打开过**这个导出工程 —— 「能不能编过 / 材质渲出来对不对」**未验**。
2. **`MonoBehaviour` 字段**：第三轮b 说有内置 Cpp2IL、可用 `GameAssembly.dll` + `global-metadata.dat`；
   本轮**没逐条核 MonoBehaviour 的字段有没有填上**（场景里 `m_Name` 都在，但脚本字段未抽查）。
3. **prefab 还原**：免费版是**内联平铺**（付费的 `Prefab Outlining` 才有树）；本轮**没核** `.prefab` 是「真 prefab」还是平铺。
4. **13 场逐场对账没做**（只亲读了 arena1）。
5. `ParticleSystem` 的 **`SubEmittersModule`** 在那一块里**没出现**（可能该件本来没有子发射器，**也可能被丢**）—— **没查清**。

---

## 四、产物与工具落在哪（**都不在 `d:/4` 里**）

| 东西 | 路径 |
|---|---|
| .NET 10 SDK（解压即用） | `d:/2/tools/dotnet10/` |
| AssetRipper 源码 | `d:/2/tools/assetripper_src/AssetRipper/` |
| 编译产物 | `…/Source/0Bins/AssetRipper.GUI.Free/Release/` |
| **导出工程**（5.9 GB） | `d:/2/tools/assetripper_out/` |
| 运行日志 | `d:/2/tools/assetripper_run/ar.log` |

复现命令（三步）：
```bash
cd "d:/2/tools/assetripper_src/AssetRipper/Source/0Bins/AssetRipper.GUI.Free/Release"
"d:/2/tools/dotnet10/dotnet.exe" AssetRipper.GUI.Free.dll --headless --port 5599 --log --log-path "d:/2/tools/assetripper_run/ar.log" &
curl -4 -X POST -d "Path=d:/2/unity_run_ref"        http://127.0.0.1:5599/LoadFolder
curl -4 -X POST -d "Path=d:/2/tools/assetripper_out" http://127.0.0.1:5599/Export/UnityProject
```

---

## 五、🔴 与【我们自己的链】逐条对账（2026-10-16 主对话现做 · `battlearena1`）

> 方法：把我们 `battlearena1_manifest.json` 的条目名，与导出场景里**组件宿主 GameObject 名**做集合比较。
> 脚本 → `d:/4/_tmp_view/_b_ar_recon/recon_particles.py`。

| | 我们 manifest | AssetRipper 场景 | **两边都有** | **只在我们** | **只在它** |
|---|---|---|---|---|---|
| **粒子**（`ParticleSystem`） | 34 实例 / 28 名 | **60 实例 / 37 名** | **28（= 我们的全部）** | **0** | **9 名** |
| **网格**（`MeshFilter` 宿主） | 30 | 41 实例 / 38 名 | **30（= 我们的全部）** | **0** | **8 名** |

🔑 **结论（明确）：它不是我们这条链的替代品，增量是【另外两块】**：
1. **HUD 那半场我们本来就导** —— 只在它那边的 9 个粒子名**全是 HUD 能量区**：
   `Accumulated Energy` · `Energy` · `Energy Accumulation VFX On/Off` · `Energy Moving Left` ·
   `Glow Acummulated` / `(1)` / `Ring` · `Shrink glow`
   （📌 与 `T13` 普查那条「`Energy Accumulation` 是整组显隐、宿主在 `…/ManaHolder/Energy Accumulation`」**对得上**）
2. **我们【故意】排除的那些** —— 只在它那边的 8 个网格名：
   `CardLowBoardLimit`（🔴 **就是 `A2101` 判「不做」的那块挡板** —— 本 build 没有任何代码读它）·
   `Collision` / `Collision (1)`（碰撞体，不是可见网格）· `Cube`×4 · `TMP SubMesh […]`（TMP 内部件）
3. ⇒ **我们这条链在这两个维度上【一条都没漏】**（只在我们 = 0）。

**顺带核掉的两条**：
- `EndBattleDoors` 在场景里 **0 命中**，但 `EndBattlePanel` ×1 · `BattleDoors` ×1 · `victoryVideo` ×1 · `endBattleDoor` ×1
  ⇒ **组件数据在**，只是宿主节点名不叫 `EndBattleDoors`（与「它没有独立 prefab、但每个场景都有实例」这条既有结论**一致**）。
- 场景里的 MonoBehaviour：**870 个实例 / 130 种脚本** ⇒ **组件数据是真的在**（⚠️ 但脚本**字段**是否填全，本轮仍未抽查）。

### 5·1 那它到底给我们带来什么？—— 三条（按价值排）

1. 🔴 **材质/渲染状态那一族可以【互相对账】**：1083 个 `.mat` 里带着 `m_CustomRenderQueue` / `m_ValidKeywords` /
   `disabledShaderPasses` / `m_SavedProperties` —— 与我们的 `mat_renderqueue.tsv` / `original_shader_blend.tsv` /
   `shader_renderqueue.tsv` 是**同一个问题的两个独立答案** ⇒ **能对出「我们哪一格抄错了」**。
   ⚠️ 另起一份是**违铁律 6**（清单只留一处）⇒ 定位必须是**对账的旁证**，⛔ 不是新台账。
2. **HUD 那半场的一份完整 YAML**：我们战斗线到现在还是**逐节点读 JSON dump** 在对 HUD；
   这里有一份**整个战斗场景**（含 HUD）的 `.unity`，**可以当第二把尺子**。
3. **13 场齐全**（我们目前只建了 10 场 + 战场线**用户已拍板暂停**）—— 若将来要复核某场，它现成。

### 5·2 ⛔ 明确【不是】增量
- **不能替代建场链**（我们 30 网格 / 28 粒子名一条不漏；它的增量全在 HUD 与我们主动排除的那部分）
- **不能替代 shader**（属性表真、**渲染体是占位**，渲不出来）
- **不是新台账**（铁律 6）

---

## 六、🎯 **第一次用它对账我们手工扒的表**：`mat_renderqueue.tsv`（2026-10-16 主对话现做）

> 脚本 → `d:/4/_tmp_view/_b_ar_recon/recon_matqueue.py`。

| | 数 |
|---|---|
| AssetRipper 导出的 `.mat` | **972**（⚠️ 有 **47 组重名**） |
| 我们的 `mat_renderqueue.tsv` | **977 行** |
| **两边都有这个名字** | **972（= 它的全部）** |
| 🔴 **`m_CustomRenderQueue` 不一致** | **1 条** —— 而且**已查明不是我们错**（见下） |
| 只在【我们表】里 | **5 个**（`Default-Particle` · `Default-Skybox` · `Font Material` · `Sprites-Default` · `Sprites-Mask` —— **全是 Unity 内置材质**，本来就不是原版资产） |

### 6·1 那 1 条「不一致」的真相：**同名不同材质**

原版里有**两个都叫 `Campaign Points` 的材质**（同名、**同 shader** guid `53486222…`、**队列不同**）：
`Campaign Points.mat` = **−1** vs `Campaign Points_0.mat` = **3000**。我们表里只有一行 `-1`。
⇒ **不是抄错，是两边各有各的那一个**。

### 6·2 🔴 真正的发现：**我们那张表【按材质名索引】，而原版有一批同名材质**
把 47 组重名逐组比一遍：

| | 组数 | 含义 |
|---|---|---|
| 组内队列**相同** | **45** | 无害 |
| 组内队列**不同** | **2** | 🔴 **我们那格只能记一个值 ⇒ 表达不了** |

那 2 组 = **`Campaign Points`：`[-1, 3000]`** · **`SandParticle`：`[3001, 3000]`**。
⇒ **做法**：这两格**加注**（或改成「名字 + bundle」双键），⛔ 不必推翻整张表 ——
**921 组不重名 + 45 组重名同值 = 我们那 976 行逐条对得起 972 个材质**。

### 6·3 这条对账意味着什么（**本轮最有价值的产出**）
🔑 `mat_renderqueue.tsv` 是**手工扒出来的**，此前**从来没有第二来源**。
现在有了 AssetRipper 这条**完全独立**的路（它读的是同一个 bundle、但是另一套实现）——
**972 个材质逐条比完，只有 1 条对不上，而那 1 条已查明是同名不同资产**
⇒ **这张表可以放心用**（这是它第一次被独立验证），同时也**第一次暴露了它的结构边界**（名字键）。

---

## 七、再对一张表：`original_shader_blend.tsv`（混合/深度/cull/colMask）—— ❌ **它帮不上忙**

> 脚本：直接扫 218 个导出 `.shader` 的**行首指令**。

| 检查 | 结果 |
|---|---|
| 218 个导出 `.shader` 里含**真** `Blend`/`ZWrite`/`ZTest`/`Cull`/`ColorMask` **指令**的 | **0 个** |
| 亲读 `Everguild_3D Card Dissolve.shader` 的 `Pass` 体 | **只有** `HLSLPROGRAM` + 通用 vert/frag —— **一个状态指令都没有**（连 `Tags` 都是 `Opaque` 兜底） |
| 为啥 grep 会看到 40 个「Blend」 | 那是**属性名里的字**（`_USE_BLEND ("Use Blend", Float)`），⛔ **不是指令** —— 差点被它骗过去 |

⇒ 🔴 **pass 状态（`SerializedPass.m_State`）在导出时被丢掉了**。
这正是本工程那条硬限制「**bundle 里的 shader 落不了工程资产**」的**直接后果**，
也正是我们当年要**手工扒** `original_shader_blend.tsv`（108 行）的原因。

⚠️ **不构成增量**：那一族字段我们**本来就能从解包资产的 `m_ParsedForm` 里直读**（`工具/dump_shader.py`）。
（AssetRipper 的 web API 另有 `/Asset/…/Yaml|Json` 端点，理论上能取到原始 `m_ParsedForm` —— 但**那是我们已有的路**，不是新能力。）

### 7·1 于是「它到底能给我们什么」的最终答案（三类逐条）

| 我们要的 | AssetRipper 能给吗 | 说明 |
|---|---|---|
| **材质属性 / keyword / `m_CustomRenderQueue` / `disabledShaderPasses`** | ✅ **能给** | 已验证（§六：972 个逐条比对，只有 1 条同名不同资产的差异）|
| **场景 / HUD 的完整层级** | ✅ **能给** | 13 场全在、含 HUD（§二·3）—— 可当**第二把尺子** |
| **粒子组件参数** | ✅ **能给** | 但**我们没漏**（§五：只在我们 = 0）|
| **shader 的 pass 状态**（blend/ztest/cull/colMask） | ❌ **给不了** | 0/218（本节）|
| **shader 的渲染算法** | ❌ **给不了** | 占位 pass（§二·2）；付费版才有反编译 |
| **替代我们的建场链** | ❌ **不是替代品** | §五：我们 30 网格 / 28 粒子名**一条不漏** |
