# S2 · CardView.Create 的 2.7 s 拆解

> 2026-10-17 · **只读诊断代理** · ⛔ 没跑 Unity / 没跑 player / 没跑 typecheck ⇒ **本文件没有一个 ms 是我量的**；出现的秒数一律是**别处已记录的实测**，逐条标出处。
> 判据顺序：`BSC62/S_A1750进战斗卡顿.md`（**它的「已排除」照单接受**）→ `待办判据_第三十三会话.md` 的 `A1628`/`A1655` → `CardView.cs` / `TmpFont.cs` / `CardArt.cs` 现读。行号会漂，用前按**方法名**现核。

## 一、结论摘要

1. **那 2.7 s 是「纯 CPU」、不是 GPU/shader 编译**：`run_v2.log` 的 **③ 帧尾（含本帧渲染）= 2.3 ms**（`A1628` 原文），而「首绘 / shader 变体编译 / 贴图首次上传」都落在选一帧的**渲染**上 ⇒ **若编译是主因，③ 不可能是 2.3 ms**。⇒ 上一轮 `S_A1750` 的假设 ⑤（每个 `new Material` 触发变体编译）**被这条读数**（外加 §四的静态计数）**大幅削弱**。
2. **最像是 `CardView.FrameUv`**（`CardView.cs:5186-5225`）：它对**每张还没量过的卡框贴图**做一次 `tex.GetPixels32()`（1024² ⇒ **读回 4 MB**）**再跑一个 `W×H` = 104 万次的 C# 嵌套循环**。它由 `Build:2121 → FrameMesh:5730` 每张卡调到，**按 Texture2D 缓存**（同一阵营的框才免费）。置信度 **中**。
3. **第二像的是「TMP Dynamic 字体冷路径」**（`TmpFont.cs:100-115`）：全仓唯一字体 `NotoSerifCJK-Regular SDF` 是 **Dynamic + 图集 1×1 占位**（`A1655③`）⇒ **每个新汉字都得从 TTF 现烘**。探针量到的「单次重排 0.0 ms」量的是**已烘好**的字（它自己也注明没覆盖冷路径）⇒ **不能据此排除**。置信度 **中**。
4. ⚠️ 两点口径要记住：**(a)「每卡 2.7 s、线性」是从「累计总和 ÷ 格数」推出来的**（`CellPhase` 只累加，见 §六），两页独立读数支持线性但**没逐格量过**；**(b) 探针的「① `CardView.Create`」其实把 `ToCardData` 也算进去了**（`CellBegin` 在它之前，见 §七）。

## 二、逐段表（**按可疑度 × 单位成本排序**）

| 段（方法/行号） | 一张卡调用几次 | 为什么可能贵 | 正向证据 | 怎么证实 | 置信度 |
|---|---|---|---|---|---|
| **`FrameUv`** `CardView.cs:5186-5225`（读回 + 1M 循环） | **每个还没量过的卡框贴图 1 次**（同框免费）；入口 `FrameMesh:5730` ← `Build:2121` | `GetPixels32()` = **1024²×4 B = 4 MB 的 GPU→CPU 回读**；再 `for y<H for x<W` = **104 万次** C# 循环 | `A1655②` 原文：「对每张新卡框贴图做一次 `GetPixels32()`（1024² ⇒ **104 万像素的 C# 循环** + 一次 GPU→CPU 回读）……**保守 1 秒量级**」 | 在 `FrameUv` 首尾夹 `Time.realtimeSinceStartup`；或加 `static int _uvPx` 累加 `W*H` 数出来 | **中** |
| **TMP 冷路径** `TmpFont.cs:100-115 / 329-368` + `CardView.Fill:2612-2622` | 4 段 TMP（title/keywords/army/race） | 字体 **Dynamic + 图集 1×1 占位**⇒**每个新字形从 TTF 现烘** + 图集长大后**整页重传** | `A1655③` 原文：「全仓唯一字体是 Dynamic + 图集 1×1 占位 ⇒ 每个字形都得从 TTF 现烘」；探针 `s27_shell.log:97` 明注「两次都小 ⇒『字体现烘』那条假设**不成立**，别往字体上查」**但那只量了已烘字** | 专门量「**第一张卡**」的 `Fill`：`GC.GetAllocatedBytes` + 分段计时；对比第二页同卡 | **中** |
| **`new Material` ×≈10** `AddLayer:2777` / `RimMaterial:5818` | ≈10（§四） | **绝大部分是「克隆缓存模板」**（`new Material(BaseMaterial())`），真正 `new Material(shader)` 的**只有 1 个**（rim）⇒ 一般不贵；但若 Unity 在材质创建期**同步编 GPU 程序**，它就在这一段 | §四计数；③ 帧尾 2.3 ms 反证（见 §一·1） | 加 `static int` 数「**从裸 shader 新建**」次数（应=1/卡）；再单独夹一次 rim | 低-中 |
| **立绘 `Resources.Load`** `CardArt.cs:743` `Get` ← `Portrait:359` ← `Build:2024` `ArtTexture` | 每张**新立绘** 1 次（18 张池子 ⇒ 最多 18 次） | 未缓存时同步读盘 + 解码（单张 ≈610 KB，`maxTextureSize 2048`）；**18 个不同卡 = 18 次未命中** | `Get` 按路径缓存（`:746-748`）⇒ 只**首次**付账 | 数 `Resources.Load` 命中/未命中次数 | 低-中 |
| **`ToCardData` + `FaceTextFull`** `BattleDriver.cs:13815 / 13861` | 1 | 字符串拼接 / `KeywordSegment` / `UnicodeNotes` / `CardIcons.Rewrite`；**它在探针 ① 的计时区间内**（见 §七） | `CollectionWindow.cs:1019`（CellBegin）在 `:1024`（ToCardData）之前 | 拆分 CellBegin/CellPhase 两处 | 低（属「量错归属」） |
| **结构开销**：`Build:1986` 建 ≈21 GO / ≈19 MR / 4 TMP / `Quad()` | 每卡 1 趟 | 每个 `AddComponent` / `new GameObject` 都便宜，量是 378 个 GO；`StampTmpQueue:767` 每次 `GetComponentsInChildren` **分配数组** | `A1628` 原文「每张卡 ≈21 个 GO / ≈19 个 MR / ≈15 份材质 + 4 段 TMP」 | `GC.GetAllocatedBytesForCurrentThread()` 夹一张卡 | 低 |

## 三、最可疑的那一条 —— 展开论证（含反证/排除到哪一步）

**`FrameUv`（`CardView.cs:5186-5225`）—— 置信度中。**

- **它在 `CardView.Create` 的调用链上**：`Create:862 → Build:1986 → :2121 _frame…sharedMesh = FrameMesh(frameTex, artTex) → FrameMesh:5730 var uv = FrameUv(tex)`。
- **一次的量**：`tex.GetPixels32()`（`tex.width×tex.height` 个 `Color32`；卡框是 1024² ⇒ **4 MB**，且是一次 **GPU→CPU 回读**），紧接着 `:5205-5216` 的双层 `for` 扫全图找不透明 bbox = **W×H 次**（1024² = **1,048,576 次**）。
- **缓存边界**：`_uvCache` 是 **`Dictionary<Texture2D, Rect>`**（`:5182`）⇒ **同一个卡框贴图只付一次**。收藏池 18 格若跨 15 个阵营 ⇒ **≈15 次**；`A1655②` 给的量级 = **保守 1 秒**。
- **反证 / 排除到哪一步**：
  - ✅ **`FrameMesh` / `ArtMeshInFrame` 自身有网格缓存**（`_frameMeshCache:5772` / `_artMeshCache:5227`）⇒ 贵的只可能是它**内部那次 `FrameUv`**，不是建网格。
  - ⚠️ **可被证伪的一步**：`GetPixels32` 在**未开 Read/Write** 的贴图上**抛异常**（`:5193-5194 catch`）⇒ 退回 `Rect(0,0,1,1)`、**几乎不花时间**。`A1655②` 说「贴图全以未压缩 RGBA32 打进包」，而 `build` 侧 `ArtBaker.ApplyImportSettings` 是否真开了 Read/Write **我没查**（⛔ 不猜）—— **这条决定它成不成立**，先验它。
  - ❓ **量级**：1 秒 / 18 格 ≈ 55 ms/格，**离 2.7 s/格 差一个数量级** ⇒ 即便成立，**它只是 2.7 s 里的一部分**，不是全部。

## 四、材质与 shader 专项

**逐卡材质清单（收藏池一档：`CardFace.Full`、有原版卡框、TMP 可用、非 `Board`）—— 全部出自 `Build:1986-2182`：**

| # | 调用点 | 生成式 | shader |
|---|---|---|---|
| 1 | `:2050` shadow | `new Material(sdfMat)`（**克隆**） | `Everguild/FX/Card Highlight And Shadow`（**原版 bundle，白名单**） |
| 2 | `:2061` art（`opaque:true`） | `new Material(OpaqueMaterial())`（**克隆**） | `CardPresentation/ArtOpaque`（自建） |
| 3 | `:2084` frame | `new Material(BaseMaterial())`（**克隆**） | `Sprites/Default`（引擎内建） |
| 4 | `:2102` textBg | 同上（克隆） | `Sprites/Default` |
| 5 | `:2130` costBg（`ShowsCost`） | 同上（克隆） | `Sprites/Default` |
| 6 | `:2145` armourIcon（单位卡且 `armor>0`） | 同上（克隆） | `Sprites/Default` |
| 7 | `:2160` info | 同上（克隆） | `Sprites/Default` |
| 8 | `:2167` gem | 同上（克隆） | `Sprites/Default` |
| 9 | `:2175` ephemeral（有该图标才建） | 同上（克隆） | `Sprites/Default` |
| 10 | `:2221` rim | **`new Material(sh)`（唯一「从裸 shader 新建」）** | `Everguild/FX/Card Highlight And Shadow`（**原版 bundle**，**不带关键字**） |

⇒ **≈10 份 `new Material` / 卡**（含徽标位再 +1 底板 +1 图标：`BuildBadgeSlot:1731 / AddBadgeIconLayer:1797`）；`A1628` 记的「≈15 份」含徽标等另算。

**三个关键答案：**

- **是同一个 shader 吗？** **不是 15 个不同 shader —— 是 3 个**：`Sprites/Default`（引擎内建，≈7 份）· `CardPresentation/ArtOpaque`（自建，1 份）· `Everguild/FX/Card Highlight And Shadow`（**原版 bundle**，2 份：shadow + rim）。
- **每次 `new` 吗？** **是**（`AddLayer:2777` 每层都新建）；但**其中 ≈9 份是「克隆一份缓存模板材质」**（`BaseMaterial()`→ `_quadMat` 静态缓存 `:3876-3880`；`OpaqueMaterial()`→ `_artOpaqueMat` `:2811-2826`；`SdfMaterial()`→ `_sdfMat` `:2886-2923`）。**从裸 shader `new Material(sh)` 的一卡只有 1 个**（rim，`:5830`）。
- **`Shader.Find` / 取 shader 的调用**：`BaseMaterial:3878` · `OpaqueMaterial:2815` · `TraitDisabledMaterial:3888` **都是静态缓存后只跑一次**；`SdfMaterial:2890` 与 `RimMaterial:5821` 走 `WarpforgeShaderMap.TryResolve`（`WarpforgeShaderMap.cs:414`）—— **SdfMaterial 有缓存（`:2888` 早退），RimMaterial 每次 `Build` 都调一次 TryResolve**（但 TryResolve 内部是字典查 + `WarpforgeShaderLoader._byName` 字典查，**不贵**）。
- **在不在那 117 个里？** 只有 SDF 那一支走 bundle（日志 `s27_shell.log:89/98` 自报 `Everguild/FX/Card Highlight And Shadow ← 原版bundle（白名单）`）；`Sprites/Default` 是**引擎内建**（不在 117 里）。
- 🔑 **结论**：材质**不是 15 个不同 shader 各编一次**；真正的「从原版 bundle shader 新建材质」**一卡只 1 份**（rim）。**若编译是主因，它只会在第一张卡编一次 ⇒ 解释不了「每卡 2.7 s 线性」**。这与 §一·1（③ 帧尾 2.3 ms）**双向指向同一结论：编译不是那 2.7 s**。

## 五、原版对照

- 🔴 **原版建一张卡花多久：本地查不到实测**（原版已关服、跑不到可比读数）⇒ **写「查不到」，不猜**。
- **能站住的两点**（不涉及 ms）：
  1. 原版**把 shader 编译产物打进 build**、开局就有；我们**运行时从 4 组 bundle 现取**（`WarpforgeShaderLoader.EnsureLoaded:85`）——但这条**对「每卡 2.7 s」不解释**（§一·1、§四）。
  2. 原版卡面是**预制体 + 静态材质 + 预烘字体图集**，我们是**每次 `new` 层 / 每次 `Fill` 重排**（`Fill:2612`、`AddLayer:2771`）——结构性差异在这里，不在 shader。

## 六、怎么量（两栏）

**批处理可量的（结构量 —— ⛔ 我给方案，不跑、不改自检）**
1. **调用计数**：给 `FrameUv` 加 `public static int` 计「调用次数」与「累计扫过的像素数」（只自增，不改行为）；给 `TmpFont.NewText`、`CardView.AddLayer` 同类计数 ⇒ 一局/一页数出来。
2. **每卡分配**：`System.GC.GetAllocatedBytesForCurrentThread()` 夹住「建一张卡」（纯 .NET，`-batchmode` 也跑）—— 只能做**回归式**（没跑过，给不出基线）。
3. **判 `FrameUv` 走没走 `GetPixels32` 成功那支**：加一句只在成功分支自增的计数器（失败分支 = 贴图没开 Read/Write ⇒ 便宜）。

**只能真机的**
1. 🔴 **在 `Create` 内部插分段计时**（本仓唯一范式 = `Shell/CollectionWindow.cs:3796-3930` 的 `BuildWatch`：`Application.isPlaying` 闸 + `Time.realtimeSinceStartup`）。要切四刀：**(a) `:2121` 那行 `FrameMesh` 前后** · **(b) `:2227 BuildTextLayers` 前后** · **(c) 每次 `new Material` 前后（至少 rim 那一处 `:2221`）** · **(d) `ToCardData` 前后**。⛔ **不是 Profiler**（`Editor/PlayerBuild.cs` ≈`:190` = `BuildOptions.None`）。
2. **判别实验（最省事）**：
   - 判 `FrameUv`：把 `_uvCache` 预填成 `Rect(0,0,1,1)`（临时）⇒ **读数塌了 = 是它**。
   - 判 TMP 冷路径：**同一张卡连建两次**、或**第二页同卡**，看比第一张快多少（`A1655④` 的原话：「判别实验只有一个：**第二次开收藏窗是不是明显快**」）。

## 七、已排除的（查了、没有 —— 别人不用再查）

| 项 | 结论 | 出处 |
|---|---|---|
| **shader 变体编译 / 首绘 / 首传贴图** | ❌ **大概率不是**：这些落在**帧尾渲染**上，而它 = **2.3 ms** | `A1628`（`_tmp_view/run_v2.log` ③）；§四（一卡仅 1 份裸 shader 新建） |
| **`SetPose` / `SetClipFromTree` / `SetFace` / 命中区** | ✅ 探针已排除（全 ≤ 个位数 ms） | `s27_shell.log:92-95`；`S_A1750` 照单接受 |
| **TMP 单次重排（已烘字）** | ✅ = **0.0 ms**（但**只覆盖暖路径**，见 §二） | `s27_shell.log:97` |
| **`Resources.LoadAll` 大爆炸** | ✅ battle 入口路径上没有 | `S_A1750` §三 |
| **arena 载入** | ❌ 实测 52.00 ms | `S_A1750` §三 |
| **材质是「15 个不同 shader」** | ❌ **是 3 个**（见 §四） | `CardView.cs` 现读 |
| **`Shader.Find` 每卡调用** | ❌ 静态缓存后**只跑一次**（`BaseMaterial:3878` 等） | `CardView.cs:3876-3880/2815/3888` |

## 八、还没查清的（⛔ 不猜）

1. **「每卡 2.7 s」到底是不是【每卡】而不是【第一卡】？** —— `CellPhase`（`CollectionWindow.cs:3847-3855`）**只累加**，日志给的是**总和 ÷ 格数**（`s27_shell.log:91` `48868.0 ms（均 2714.9 / 格）`）。两页独立读数（18 格 48868 / 6 格 17316.7 ≈ 3:1）**支持线性**，但**没逐格量过** ⇒ **未证**。
2. **`FrameUv` 的 `GetPixels32()` 到底走没走成功分支**（贴图 Read/Write 开没开）—— 决定 §三那 1 秒成不成立。**没查**。
3. **TMP 冷路径（现烘字形）一张卡多少 ms** —— **没量**。探针只量了暖路径。
4. **`ToCardData` 占 ① 里多少** —— 没拆（探针把两者并在一格）。
5. **原版可比读数** —— **本地查不到**（同上 §五）。
6. **搜过的词/路径**（别重搜）：`CardView.cs`（逐方法现读）· `TmpFont.cs` · `CardArt.cs` · `WarpforgeShaderLoader.cs` · `WarpforgeShaderMap.cs` · `BattleDriver.cs:13815-13884` · `CollectionWindow.cs:1001-1055 / 3796-3930` · `待办判据_第三十三会话.md` 的 `A1628`/`A1655` · `S_A1750进战斗卡顿.md` · `s27_shell.log:80-104`。
