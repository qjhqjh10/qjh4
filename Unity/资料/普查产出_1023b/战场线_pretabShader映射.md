# 战场线 · prefab 材质 shader 映射（Dummy → 本工程可解析）

> 执行代理报告 · 2026-10-23 · 任务：把 staging 里 13 场 prefab 闭包材质的 **Dummy shader guid**
> 换成本工程能用的 shader，产出**可 import、不品红**的一份副本。
> ⛔ 没碰 `Assets/**`、`d:/2`（只读）、git；没跑 Unity。判据只用了本仓既有的解析链。

---

## 〇、结论（数字先看）

| 项 | 数 | 备注 |
|---|---|---|
| 场次 | **13** 个 staging 目录 | 其中 **battlearena3 只有 prefab、0 个材质**（它的资产早就在 `Assets/WarpforgeVFX/ArenaOriginalPrefabs/battlearena3/`，不在 staging） |
| 材质 `.mat` | **239** | 12 个场有材质（battlearena3 除外） |
| 不同 shader guid | **30** | 29 个 Dummy + 1 个 Unity 内建 |
| 已换成本工程可解析 | **199 个材质 / 12 个 guid** | 104 → Assets 自建 shader（10 个 guid）· 95 → URP 包内 shader（2 个 guid） |
| 原样保留（Unity 内建） | **4 个材质 / 1 个 guid** | fileID `10720` ×3 · `10721` ×1 |
| unresolved | **17 个 guid / 36 个材质** | 逐条见副本里的 `_unresolved.md` |
| 自检 | ①②③④ 全过 | + prefab md5 对 manifest **13/13 PASS** + 重跑 **tree_hash 相同** |

**⚠️ 「不品红」的准确口径：239 个材质里 203 个解析得到（199 工程 shader + 4 Unity 内建），
剩余 36 个仍是 Missing / 品红** —— 那 36 个不是漏做，是**原版 shader 只以 bundle 形式存在、
没有工程资产可指**（清单与理由见 §三）。

**产物**

| 路径 | 内容 |
|---|---|
| `d:/4/_tmp_view/arena_prefabs_out_shaded/` | 副本（369 MB，与 staging 同构；**只有 199 个 `.mat` 的 shader guid 变了**） |
| `…/_shader_map.tsv` | 30 行映射表：原 guid / shader 名 / 我们的 guid / 解析来源 / unresolved / 是否在 UseOriginal 白名单 / fileIDs / 材质数 / 说明 |
| `…/_unresolved.md` | 17 条 unresolved：用它的材质逐条列出 + 怎么处理 |
| `…/_mat_changes.tsv` | 199 行逐材质改动（旧 guid → 新 guid） |

**工具**：`d:/4/Unity/工具/map_arena_prefab_shaders.py`（可重跑、字节幂等、`--dry-run`）

---

## 一、判据与做法（⛔ 没有自己发明映射）

### 1·1 判据链 = `WarpforgeShaderMap.TryResolve` 的「静态 import」等价裁剪

静态 `.mat` 里**只能写工程资产的 guid**，所以把运行时那条五步链裁成三步
（出处：`Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:548-613`）：

1. **`Replacements`（原版名 → 自建 shader）** —— 表里有就用它的目标。
   ⚠️ 其中 10 个名字同时在 `UseOriginal` 白名单里（正常态走 bundle 原件）；**bundle 里的 shader
   不是工程资产、静态材质指不过去**，所以只能落到那段注释写明的**兜底路**（`:574-594`
   「白名单取件失败才落 `Replacements`」）。`_shader_map.tsv` 用 `use_original_whitelist=1` 标了这些行。
2. **工程自带同名** —— `Shader.Find(originalName)` 的等价物：先扫 `Assets/**/*.shader`，
   再扫 `Library/PackageCache/**/*.shader`（URP 包住这儿）。
3. 都没有 ⇒ **unresolved**（⛔ 不拿「看起来像」的顶）。

### 1·2 三张表怎么建的（都是现读，不是手抄）

| 表 | 来源 | 量 |
|---|---|---|
| 原版 guid → shader 名 | `ExportedProject/**/*.shader.meta` 的 `guid:` + 同目录 `.shader` 的**非注释** `Shader "…"` 行 | 218 个 `.shader.meta`，**0 个取不到名字** |
| 我们的 shader 名 → guid | `Assets/**/*.shader.meta`（同一套解析） | **46/46**（⚠️ `TMP_SDF SSD.shader` 带 UTF-8 BOM，第一版解析漏了它 —— 已修，**与本次战场材质无关**） |
| 包内 shader 名 → guid | `Library/PackageCache/**/*.shader.meta` | 114 个 |
| `Replacements` / `UseOriginal` | **直接解析 `WarpforgeShaderMap.cs` 的两张表** | 32 条 / 64 条 |

> 🔑 两张判据表是**从 C# 源码里读出来的**，不是另抄一份 —— 抄了就是第二个真相来源。
> 工具每次运行还会打印「`Replacements` 的目标里工程没有的」清单（本次为空）。

### 1·3 改写方式

只替换 `m_Shader` 里 **guid 那 32 个字符**：

```
m_Shader: {fileID: 4800000, guid: <Dummy>, type: 3}
                            ↓
m_Shader: {fileID: 4800000, guid: <我们>,  type: 3}
```

`fileID` / `type` / 空白 / 其余一切**逐字节不动**（自检 ④ 验的就是这条）。

### 1·4 URP 包 guid 为什么可以直接指（实据）

ExportedProject 里那两个 URP shader 的**文件名就是原工程用的 guid**，而我们本机 URP 17.3.0
包里的 `.meta` guid **逐字符相同**：

| 原版名 | ExportedProject 文件名 | 我们包里 `.meta` 的 guid | 我们包里的文件 |
|---|---|---|---|
| `Universal Render Pipeline/Particles/Unlit` | `Assets/0406db5a14f94604a8c57ccfbc9f3b46.shader` | `0406db5a14f94604a8c57ccfbc9f3b46` | `…/Shaders/Particles/ParticlesUnlit.shader` |
| `Universal Render Pipeline/Unlit` | `Assets/650dd9526735d5b46b79224bc6e94025.shader` | `650dd9526735d5b46b79224bc6e94025` | `…/Shaders/Unlit.shader` |

⇒ 这不是「挑了一个近似的」，是**同一个 shader 资产**（我们工程里早先搬进来的战场材质
`Assets/WarpforgeArena1/arenas/battlearena1/Materials/` 也是这么指的：12 条 Particles/Unlit + 5 条 Unlit）。

---

## 二、映射表（30 个 guid 全表）

> 完整 32 位 guid / 逐材质改动见副本里的 `_shader_map.tsv` / `_mat_changes.tsv`。
> `UO` = 原名在 `UseOriginal` 白名单里（= 正常态走原件、这里是兜底路）。

| # | 原 guid | 原版 shader 名 | 我们的 guid | 来源 | 材质数 | UO |
|---|---|---|---|---|---|---|
| 1 | `0000000000000000f000000000000000` | Unity 内建 `Mobile/Particles/Additive`（fileID 10720）/ `Mobile/Particles/Alpha Blended`（10721） | —（保留） | Unity内建(保留) | 4 | 0 |
| 2 | `020fbbbe9cc2bad419186c43fe0b1440` | `Everguild/Unlit Wind` | `7c3e1fcc0f472d84b8bebce0ca9b7d9f` | Replacements(自建) | 18 | 1 |
| 3 | `0c4c053ac011a0b4ab6b0660f1185767` | `Shader Graphs/Eclipse Tau` | `e405f4168dff44e89081ffceff8f4002` | Replacements(自建) | 1 | 1 |
| 4 | `142b4feafd0facd46b9dd8e1815f71b5` | `Everguild/FX/Specific/Tau Generator Energy` | — | **UNRESOLVED** | 2 | 0 |
| 5 | `1a8457d2860ffd74e9006a53748e8e11` | `Everguild/UnlitAmbient` | `7c3e1fcc0f472d84b8bebce0ca9b7d9f` | Replacements(自建) | 45 | 1 |
| 6 | `24c36b2bc92195c4daa293758b6778b4` | `Everguild/UnlitAmbient Emissive Flickker` | `7c3e1fcc0f472d84b8bebce0ca9b7d9f` | Replacements(自建) | 3 | 1 |
| 7 | `2ef9073a060cfd440ad4e1704fdc46c1` | `Everguild/Misc/Unlit shadows receiver` | — | **UNRESOLVED** | 9 | 0 |
| 8 | `3b9f9aa2bfa58844a9b42ebfaf71f3cd` | `Everguild/FX/Simple Fake Water` | — | **UNRESOLVED** | 1 | 0 |
| 9 | `3c75f7d88d9c88948877f459289cdc08` | `Everguild/FX/Floor Planar Reflections Grainny` | — | **UNRESOLVED** | 2 | 0 |
| 10 | `3d41d7880a586f24e91b0d86040050f9` | `Everguild/FX/Alpha Mask One Layer` | `b235a9d7b094e834d904def270353272` | Replacements(自建) | 5 | 1 |
| 11 | `41caa4227318ebf4b8ee81510b0b0f4a` | `Everguild/FX/Particle Distortion Affect Transparents` | `7692e4070c9d94944b24c6819272ed98` | Replacements(自建) | 3 | 1 |
| 12 | `4948ffca7445cf24b984734c9f605cef` | `Everguild/FX/Unlit UV scroll` | `23b6c6bb466831e469876b8d366bee63` | Replacements(自建) | 5 | 1 |
| 13 | `4eaec7c5878aecb4b89e3929e26ad4a5` | `Spine/Special/HiddenPass` | — | **UNRESOLVED** | 1 | 1 |
| 14 | `625e9c59bd367b544b5fe09c3a5bc96e` | `Universal Render Pipeline/Particles/Unlit` | `0406db5a14f94604a8c57ccfbc9f3b46` | 工程自带同名(Package) | 90 | 0 |
| 15 | `780a8a471750e85488225331270ff1dd` | `Everguild/FX/Tyranids/Pulsating Mesh` | — | **UNRESOLVED** | 6 | 0 |
| 16 | `793785ef47a351e4d931a8fb1aa4c8b5` | `Everguild/FX/Alpha Masks Two Layer` | `2e880a12fe9977b419319ca7dbbd666d` | Replacements(自建) | 2 | 1 |
| 17 | `85e3ade8c32548d4c9dd1f87e981a94f` | `Everguild/FX/Alpha Mask One Layer  Color Ramp` | — | **UNRESOLVED** | 1 | 0 |
| 18 | `8711183c3f7e8a649ab1dd317e85cf4e` | `Everguild/FX/Vortex` | — | **UNRESOLVED** | 1 | 1 |
| 19 | `9c6a6f6750d7ff94e8669f2fdea8a8f0` | `Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad` | — | **UNRESOLVED** | 1 | 0 |
| 20 | `ac7099a605e8d5446a77e7348e45af1c` | `Everguild/FX/Extra Color` | `730df9a37ffb4ea58c90acd909e35b26` | Replacements(自建) | 21 | 1 |
| 21 | `b99ac27799249534cb2a22fa7f772175` | `Everguild/FX/Burning` | — | **UNRESOLVED** | 1 | 1 |
| 22 | `c4e82c8093d53fb4ca9015d0b3c3d52e` | `Everguild/FX/Tyranids/Tyranid Tentacle` | — | **UNRESOLVED** | 4 | 0 |
| 23 | `c5062bee1d8d1c041a065a71297da43f` | `Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad` | — | **UNRESOLVED** | 1 | 0 |
| 24 | `d38980a103a14c64d9dfe14e705e2187` | `Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad` | — | **UNRESOLVED** | 1 | 0 |
| 25 | `d9ff19e6ac6ccf6459aa15abf82f8871` | `Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad` | — | **UNRESOLVED** | 1 | 0 |
| 26 | `f2642f618e317484b9b0a2930a6690b6` | `Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad` | — | **UNRESOLVED** | 2 | 0 |
| 27 | `fac3e7163a55d634f8074df2f2c308a6` | `Everguild/Matcap/Matcap With Texture` | `da6aa54c99769a0468dbdbcc4a63f286` | Replacements(自建) | 1 | 1 |
| 28 | `fbbb8c528cd7de443a749cb99924e307` | `Universal Render Pipeline/Unlit` | `650dd9526735d5b46b79224bc6e94025` | 工程自带同名(Package) | 5 | 0 |
| 29 | `fc85a45818972be43b7726d6897575fe` | `Everguild/FX/Alpha Mask One Layer  Color Ramp` | — | **UNRESOLVED** | 1 | 0 |
| 30 | `fe9ae5d8a5efaeb458d5907b09ee65d7` | `Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask` | — | **UNRESOLVED** | 1 | 0 |

**按来源汇总**：Replacements 10 个 guid / 104 个材质 · 工程自带同名(Package) 2 个 guid / 95 个材质 ·
Unity 内建 1 个 guid / 4 个材质 · unresolved 17 个 guid / 36 个材质。

### 2·1 顺带订正一条旧记录（铁律 5，主对话合并时请带进正本）

`资料/普查产出_1023b/战场线_prefabYAML试点_battlearena3.md` §六·5 写着
「`unity_builtin_extra` fileID **10720** 对应哪个 shader 名，**没查到**」。**现查到了**：

- **10720 = `Mobile/Particles/Additive`** · **10721 = `Mobile/Particles/Alpha Blended`**
  （三件材质 `Fire1` / `smokesoft_blend` / `bubble_light` / `MuzzleFlash_Side1` 就在用）
- 判据：2026-10-23 用 UnityPy 读
  `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/unity_builtin_extra` 的
  `Shader.m_ParsedForm.m_Name`（`m_Name` 是空的，**名字只在 `m_ParsedForm.m_Name` 里** —— 只看
  `m_Name` 会误报「没有名字」）。
- 这两个名字**都在 `WarpforgeShaderMap` 的 `Replacements`** 里（→ `WarpforgeVFX/Particles/Extra Color`）、
  `Mobile/Particles/Additive` 也在 `UseOriginal` 里（`:132`）。

---

## 三、unresolved 清单（17 个 guid / 36 个材质）

**共同的根因**：这些原版 shader **只在随包 bundle 里**（`资料/普查产出_1022/J-A_战场素材与材质诊断.md:50`
实读 20/20 全在 `wf_shaders` / `wf_shaders_extra` / `wf_arena_*`），**不是工程资产** ⇒
静态 `.mat` 指不过去。⛔ 不拿「看起来像」的自建 shader 顶（那会静默改变观感）。

| # | 原版 shader 名 | guid | 材质数 | 类型 / 为什么没映射 |
|---|---|---|---|---|
| 1 | `Everguild/Misc/Unlit shadows receiver` | `2ef9073a…` | **9** | 两张表都没有、工程无同名（原件在 bundle） |
| 2 | `Everguild/FX/Tyranids/Pulsating Mesh` | `780a8a47…` | **6** | 同上（leviathan 的「会搏动的肉」） |
| 3 | `Everguild/FX/Tyranids/Tyranid Tentacle` | `c4e82c80…` | **4** | 同上 |
| 4 | `Everguild/FX/Specific/Tau Generator Energy` | `142b4fea…` | 2 | 同上 |
| 5 | `Everguild/FX/Floor Planar Reflections Grainny` | `3c75f7d8…` | 2 | 同上（`WarpforgeShaderMap` 两张表都没它；`ArenaMirror` 是运行时取 bundle） |
| 6 | `Everguild/FX/Alpha Mask One Layer  Color Ramp` | `85e3ade8…` + `fc85a458…` | 2（两 guid 各 1） | 同上；最接近的是 `WarpforgeVFX/FX/AlphaMaskOneLayer`，但**属性表不同（多 `_SecondaryTex` 色带）** ⇒ 不顶 |
| 7 | `Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad` | `f2642f61…` | 2 | VFX Graph 运行时生成的 shader，工程里没有也不该有 |
| 8 | `Everguild/FX/Simple Fake Water` | `3b9f9aa2…` | 1 | 两张表都没有、工程无同名 |
| 9 | `Spine/Special/HiddenPass` | `4eaec7c5…` | 1 | Spine 运行时 shader；**我们工程没装 Spine 包** |
| 10 | `Everguild/FX/Vortex` | `8711183c…` | 1 | 只在 `UseOriginal`（走 bundle 原件） |
| 11 | `Everguild/FX/Burning` | `b99ac277…` | 1 | 只在 `UseOriginal` |
| 12 | `Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad` | `c5062bee…` | 1 | VFX Graph |
| 13 | `Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad` | `d9ff19e6…` | 1 | VFX Graph |
| 14 | `Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad` | `9c6a6f67…` | 1 | VFX Graph |
| 15 | `Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad` | `d38980a1…` | 1 | VFX Graph |
| 16 | `Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask` | `fe9ae5d8…` | 1 | 两张表都没有 |
| 17 | `Everguild/FX/Specific/Necrons Rays` | `6afe3c56…` | 0（**只在工程里那份 battlearena3 试点副本**，见 §五·2） | 只在 `UseOriginal` |

**逐场分布（不含 4 个 Unity 内建）**

| 场 | unresolved 材质 | 明细 |
|---|---|---|
| battlearena1 | 1 | Unlit shadows receiver ×1 |
| battlearena2 | 3 | Unlit shadows receiver · Simple Fake Water · Alpha Mask One Layer Color Ramp |
| battlearenaaeldari | 7 | 5 × Hidden/VFX（Dust/Portal）· Vortex · Floor Planar Grainny |
| battlearenaastramilitarum | 1 | Unlit shadows receiver |
| battlearenablacklegion | 1 | Unlit shadows receiver |
| battlearenadarkangels | 3 | Unlit shadows receiver · Alpha Mask One Layer Color Ramp · Spine/Special/HiddenPass |
| battlearenaemperorschildren | 1 | Unlit shadows receiver |
| battlearenagenestealers | 1 | Unlit shadows receiver |
| battlearenaleviathan | **12** | Pulsating Mesh ×6 · Tyranid Tentacle ×4 · Hidden/VFX Dust ×1 · Unlit shadows receiver ×1 |
| battlearenasororitas | 2 | Burning · Floor Planar Grainny |
| battlearenaspacewolves | 1 | Unlit shadows receiver |
| battlearenatauviorla | 3 | Tau Generator Energy ×2 · Floor Planar Grainny Vertex color shadow mask |
| **合计** | **36** | |

> `Unlit shadows receiver` 在 **9 场**都有 —— 主对话若只修一类，先修它（但两边都是 bundle 原件，
> 静态 import 修不了；要修只能走运行时 binder 或 `--fallback-urp-unlit`）。

---

## 四、自检（四条 + 两条额外）

| # | 检查 | 结果 |
|---|---|---|
| ① | 副本里每个 `.mat` 的 shader guid 归属 | **Assets `.shader.meta` 104 · Packages `.meta` 95 · Unity 内建 4 · 解析不到 36(=预期 unresolved)** |
| ② | unresolved 清点 | **17 个 guid / 36 个材质**，全部在 `_unresolved.md` 里逐条列出 |
| ③ | **不改 prefab / 网格 / 贴图** | 非 `.mat` 文件 **1949 个逐字节一致，0 处不同**；13 个 prefab 的 md5 对 `_manifest.tsv` **13/13 PASS** |
| ④ | `.mat` 只差 shader guid | 把 guid 那 32 字符抹掉后逐字节相等 —— **239/239 PASS，0 处不同** |
| ⑤ | 重跑字节幂等 | 连跑两次 `tree_hash` 相同（`6983c250a5543839c56637300cdc5e8954046e75`） |
| ⑥ | 独立复核（不用工具自己的自检） | 另写脚本按 `Assets/**/*.meta` + `PackageCache/**/*.meta` 重算：104 / 95 / 4 / 36，**与工具报的一致**；104 条 Assets 引用**全部**落在 `.shader.meta`（0 条落到别的资产） |

**① 的口径说明（重要）**：任务书写的是「每个 guid 都能在 `Assets/**` 找到 `.meta`」。
**按字面只有 104/239 满足** —— 因为另外 95 条指向 **URP 包**（`Library/PackageCache/...`，
是工程可见资产但不是 `Assets/` 下的文件），4 条是 Unity 内建（没有 `.meta`）。
**我按「工程可解析」口径判**（Assets + Packages + 内建 = 203），理由是：
① URP shader 本来就不该拷进 `Assets/`；② 我们工程既有的战场材质
（`Assets/WarpforgeArena1/arenas/battlearena1/Materials/` 17 条）本来就是这么指的。
若主对话要求「字面只认 `Assets/**`」，那 95 条也得换指 —— **那是另一套口径、且与既有材质不一致**，需先拍板。

---

## 五、没查清 / 要先拍板的（⛔ 别当成已验）

### 5·1 Unity import 没跑（本工具按红线不跑 Unity）

「能 import、不品红」目前只到**引用层面**：所有非 unresolved 的 guid 都能在工程里解析到
`.shader.meta` / 包 `.meta` / 内建资源。**真渲出来是什么样、属性名认不认，要主对话 import 后看**。
⚠️ 预判一类「能渲但参数不对」：我们的自建 shader 属性名与原版不完全同名时，
材质上的值会被丢掉（**不是品红、是「静默变默认」**）—— 本工具只换 guid、不迁属性（超出任务口径）。

### 5·2 🔴 试点副本（已在 `Assets/` 里）**还是 Dummy**，本工具没覆盖它

`Assets/WarpforgeVFX/ArenaOriginalPrefabs/battlearena3/` 的 **22 个材质**（2026-10-23 现读）：
**19 个仍指着 ExportedProject 的 Dummy guid**（10 × URP Particles/Unlit · 3 × Extra Color ·
2 × Unlit UV scroll · 2 × UnlitAmbient · 1 × `Everguild/FX/Specific/Necrons Rays` · 1 × `Everguild/FX/Floor Planar Reflections Grainny`）
＋ 3 个 Unity 内建。
⇒ **它现在在工程里就是 19 个 Missing / 品红**（不是本工具造成的，是试点那次按设计没改）。
按本报告 §二的同一张表改一遍即可：`625e9c59→0406db5a` · `ac7099a6→730df9a3` ·
`4948ffca→23b6c6bb` · `1a8457d2→7c3e1fcc`；`Necrons Rays` / `Floor Planar Grainny` / 内建 3 条维持不动
（2 个 unresolved + 3 个内建 = 5 个不变，17 个可换）。
⚠️ 我不写 `Assets/**`（红线），**这 22 个材质要主对话另派/自己动手**。

### 5·3 Unity 内建的 4 个材质：**默认保留**，换不换要拍板

- 现读 `10720 = Mobile/Particles/Additive` ×3（`Fire1` / `bubble_light` / `MuzzleFlash_Side1`）、
  `10721 = Mobile/Particles/Alpha Blended` ×1（`smokesoft_blend`）。
- **保留的理由**：它不是 Dummy（任何 Unity 工程都解析得到，不会 Missing）；
  **试点副本也是这么处理的**（`citclr_light` / `default_001_add` / `treasure_ray`）。
- **要换的理由**：`WarpforgeShaderMap.cs:370-371` 写着「内置管线老粒子 shader 在 URP 工程里会渲染成粉色」
  —— ⚠️ **这条对「引擎自带的那一份」从没实测过**（该文件 `:119-131` 自己说那批 unlit 粒子在 URP 下能渲）。
- ⚠️ **直接换会掉混合**：这 4 个材质用的是 Mobile/Particles 的属性名 **`SrcMode/DstMode/CullMode`**
  （不是 `_SrcBlend`）；换到 `WarpforgeVFX/Particles/Extra Color` 后这些是**未知属性**，
  混合会掉回 shader 默认（`Extra Color` 默认 **1/0 = 不透明**，见 `WarpforgeShaderMap.cs:423`）。
  真要换，得同时注入 `_SrcBlend=5 / _DstBlend=1 / _ZWrite=0 / _Cull=0`（判据 = 同一段 `InferBlend`/`InferCull`）。
  **本工具没做属性迁移** ⇒ `--remap-builtin` 只换 guid，**验证前别直接用**。
- 现状 = **原样保留**，要改跑 `--remap-builtin`（或在报告批准后我再补一版带混合注入的）。

### 5·4 VFX Graph 组件（顺手发现，铁律 13·4⑧）

`battlearenaaeldari` 的 prefab 里有 **2 个 `VisualEffect`**（type id `2083052967`）、
`battlearenaleviathan` **1 个**。我们工程 `Packages/manifest.json` **没装 VFX Graph 包**、
`Assets/Plugins` 里也没有相关 DLL ⇒ import 时这些组件可能报 unknown class / 被丢弃。
`Hidden/VFX/*` 那 6 个材质本来就是 VFX Graph 运行时生成的 shader，**不会有工程资产**
⇒ 就算组件保留，那 6 个材质也仍会 Missing。

### 5·5 URP 包 guid 的长期稳定性

本次用的是 URP **17.3.0** 的 `.meta` guid，与原版工程（6000.2.6f2 / URP 17.x）**同名 shader 的 guid 完全一致**。
**升级 URP 大版本后要复核**（`.meta` 在包内随版本走；同一大版本内未变）。

### 5·6 本报告没动的两类「近似开关」

- `--fallback-urp-unlit`（默认关）：把 17 个 unresolved 兜底到 `Universal Render Pipeline/Unlit`。
  这是本仓对「取不到原版 shader」的**既有做法**（`资料/已知的坑.md:427`：
  leviathan 那 32 个网格当年就是这么兜的），⚠️ 但会**改变观感**、且 unresolved 标记保留。
  命令：`python -I -X utf8 d:/4/Unity/工具/map_arena_prefab_shaders.py --fallback-urp-unlit`
- `--remap-builtin`（默认关）：见 §5·3。

### 5·7 一个与本任务无关、但顺带看到的旧记录

`ExportedProject` 的 218 个 `.shader` 全是 `//DummyShaderTextExporter`（本报告 §1·2 的解析全靠
`.shader` 第一行 + `.meta` 的 guid；**Dummy 的 Properties 段是真的，pass 是空的**）。
⇒ 不要指望从 ExportedProject 拷 shader 进工程。

---

## 六、复跑 / 验收（一条命令）

```bash
# 重新生成（幂等；先删旧产物再写，防手滑只删自己那一个目录）
python -I -X utf8 d:/4/Unity/工具/map_arena_prefab_shaders.py

# 只看解析结果、不写盘
python -I -X utf8 d:/4/Unity/工具/map_arena_prefab_shaders.py --dry-run

# 主对话验收（import 前后都能跑）：
python -I -X utf8 - <<'PY'
import re,os,collections
ref=re.compile(rb'm_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})')
root=r'd:/4/_tmp_view/arena_prefabs_out_shaded'
c=collections.Counter()
for dp,_,fn in os.walk(root):
    for f in fn:
        if f.endswith('.mat'):
            g=ref.search(open(os.path.join(dp,f),'rb').read()).group(1).decode().lower()
            c[g]+=1
print(len(c),'guids /',sum(c.values()),'refs')
PY
```

**主对话怎么验（三步）**

1. **引用层（不用 Unity）**：跑上面那段 —— 30 个 guid / 239 refs；再对 `_shader_map.tsv` 看
   `unresolved=0` 的 13 个 guid 是否都在 `Assets/**/*.shader.meta` 或 URP 包 `.meta` 里。
2. **import 层**：把 `arena_prefabs_out_shaded/<arena>/` 拷进
   `Assets/WarpforgeVFX/ArenaOriginalPrefabs/<arena>/`（⛔ 别和 staging 混），开 Unity 看 Console：
   预期 **0 条 "missing shader"**（unresolved 那 36 个会是 Missing —— 这是已知、已列的）；
   再开 prefab 看 hierarchy / 材质槽。
3. **画面对不对**：本报告**没有**做「与原版实拍并排」—— 那要等 import + 渲染；且 §5·1 的
   「属性名不认」只有渲一次才看得见。

---

## 七、本轮没做的事（明确边界）

- ⛔ 没跑 Unity（红线）⇒ 没验 import / 没渲图。
- ⛔ 没写 `Assets/**` ⇒ 试点 battlearena3 那 22 个材质仍未修（§5·2）。
- ⛔ 没迁材质属性（`_SrcBlend` / `_Cull` / 关键字…）—— 只换 guid，按任务书口径。
- ⛔ 没改 `.meta`（含 `assetBundleName` 那些）—— 逐字节照拷。
- ⛔ 没动 git、没改 `CLAUDE.md` / `项目任务.md` / 其他线台账。
