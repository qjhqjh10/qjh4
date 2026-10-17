# T09 收藏窗性能 —— 只读诊断（2026-10-17，第四十六会话）

> 输入：只读诊断代理（未跑 Unity / 未动 git）。**结论口径**：实测只能证明「100% 在 `CardView.Create` 内、≈2.6 s/格线性」；
> 再往下必须补探针，现有日志给不出唯一答案。
> 关联：`A1628` · `A1655` · `A1813`（同一条病）· `A1543`（卡组编辑先例）

## 一、耗时排序

| # | 阶段 | 文件:行号 | 为什么贵 | 证据 |
|---|---|---|---|---|
| 1 | **`CardView.Create` 整体（唯一被实测钉死的大头）** | `Core/CardView.cs:862` → `Build:1986` | 一格的全部耗时都在这里；内部**没有**分段读数 | `_tmp_view/player/s27_shell.log:91`：18 格 **48868.0 ms（均 2714.9/格）**；`:105` 6 格 **17316.7 ms（均 2886.1/格）** ⇒ 斜率 **2629 ms/格**、截距 ≈1542 ms |
| 2 | 同格其它段 | `CollectionWindow.cs:1045/1058/1075/1080` | 不是 | `SetPose` 0.7 ms · `SetClipFromTree` 8.5 ms · `SetFace` 3.8 ms · 张数条+`AddHit` 7.0 ms（整页累计，均分每格远小于 1 ms） |
| 3 | 滚动回调 = 整屏重建 | `CollectionWindow.cs:952` `OnChanged = () => RebuildCardsCells(holder)`；`MenuScroll.cs:225/260/286/360`（阈值 **0.01 px**） | 每回调一次 = `DestroySafe` 全部子件（`:1013`）+ 重新 `Create` 18 张（`:1033`）⇒ **一次滚动 ≈ 40–49 s** | 代码路径 + 2.7 s/格 |
| 4 | `FrameUv` 回读 | `CardView.cs:5260-5298`（入口 `FrameMesh:5804` ← `Build:2121`） | 每个**不同卡框贴图**一次 `GetPixels32()`（1024²=4 MB）+ 104 万次 bbox 循环；`_uvCache` 按贴图缓存 ⇒ 每贴图只付一次 | `.meta:24 isReadable: 1` ⇒ 走成功分支；≈55 ms/格，**不是主因** |
| 5 | TMP / 字体 | `CardView.cs:2362/2644/2662`，`TmpFont.cs:99-114/366-406` | 每卡 4 段 TMP；Dynamic 图集按新字形增长，**不是每次建字都重建** | `s27_shell.log:96` 图集 2048²、207 字形；`:110` 297（+90/6 卡）；暖路径 `ForceMeshUpdate` 0.0 ms |
| 6 | 材质 | `CardView.cs:2768-2785`、`:2886`、`:3876`、`:5892` | ≈10 份 `new Material`/卡（9 份克隆静态模板） | `S2_CardViewCreate拆解.md:38-61`；`run_v2.log:93` 帧尾（首绘/上传/编译）=2.3 ms ⇒ GPU 上传已排除 |
| 7 | **每卡唯一资源加载（最大剩余嫌疑）** | `CardView.cs:6357 ArtTexture` → `CardArt.cs:359/746` | 立绘是唯一「每张卡一条新路径」的同步 `Resources.Load`（1.0–2.4 MB PNG、`isReadable:1`、无 mipmap） | 静态路径可见，**没有 ms 读数**；天然产生线性斜率 |
| 8 | `InfoTexture` 逐卡烘 | `CardView.cs:6005-6120`（`BakeInfo`） | 每张不同卡 256×407；CPU ~10 万像素，按量级应 ≤几 ms | 无读数 |

## 二、可复用边界

| 对象 | 可复用 | 复用键 | 风险 |
|---|---|---|---|
| `CardView` 实例 | **是（首选）** | `CardDef.Id`（先例 `DeckRuntime.cs:2875-2895`）；跨页签再加 `CardFace/CardUse/artOverride/语档` | 复用必须 `SetPose + SetData + SetClipFromTree + SetHighlight`；GO 名按当前槽号重写（先例 `:2909`） |
| 立绘/卡框/SDF/UI 贴图 | 已缓存（`CardArt._cache` 按路径） | 路径 | 无 |
| `InfoTexture` | 已缓存（`ContentCacheKey`） | 内容键 | 无（键含已渲染文本） |
| `FrameMesh` | 已缓存；键含立绘 ID ⇒ 每卡重建（4 顶点，便宜） | 贴图/立绘 ID | 无 |
| 张数条 / `AddHit` | 可复用但**不建议第一轮** | 槽号 | 闭包捕获 `def`（`CollectionWindow.cs:1082-1083`）⇒ 复用必须重绑 |
| 字体图集 | 全局资产 | — | — |

跨页签：Cards 与 Styles 的 `artOverride` 不同 ⇒ **各自一个池**。窗口关闭时 `WindowsManager` 会 `Destroy(gameObject)`
（`WindowsManager.cs:622`）⇒ 池随窗销毁；跨「开窗」复用风险高，列为第二步可选。

## 三、修复方案

**第 1 步（必做，低风险）：滚动不再重建整屏**
- 改 `Shell/CollectionWindow.cs:1009-1087`：加 `Dictionary<string,CardView> _cellById` + 本趟 `used` 集 + `parked` 列表；
  循环里先按 `list[i].Id` 找视图，找到就 `SetPose`（`:1045`）→ `SetData` → `SetClipFromTree`（`:1058`）→ `SetHighlight`；
  找不到才 `Create`（`:1033`）。不可见视图 `SetActive(false)` 入 park，LRU 上限 ~36（2 屏）；`RebuildCardsPage:1005` 清池。
- 预期：滚一格/拖拽从 **~49 s/次** 降到 **~1 ms 级**；只在跨行时新建 6 张（仍需第 2 步）。

**第 2 步（首次 41.5 s：先量再切，别盲改）**
- 2a. 在 `CardView.Build` 四刀计时（`Application.isPlaying` 闸）：`:2024 ArtTexture` 前后、`:2121 FrameMesh` 前后、
  `:2227 BuildTextLayers` 前后、`:2221 rim new Material` 前后 ⇒ 唯一能把「立绘加载」定成结论/推翻的办法。
- 2b. 低风险：把 `FrameUv` 的 bbox 离线烘成「贴图名→Rect」表进 Resources（`CardView.cs:5260` 改读表），
  运行时不再 `GetPixels32`；随后可关 `isReadable` 并上压缩（`A1655②④`）。预期 ≤1 s/次开窗。
- 2c. 若 2a 指向立绘加载：开窗时协程分批加载/建格（或先建占位卡逐帧替换）；若指向字体冷路径：启动时预灌字形。

**第 3 步（可选）：跨开窗复用** —— 池挂常驻节点或关窗只 `SetActive(false)`；风险最高，前两步见效后再定。

## 四、没查清

1. `Create` 内 2.6 s/格到底是哪一步 —— 现有日志没有这一层读数；「立绘 `Resources.Load`」与「材质克隆/`InfoTexture`」**无法区分**。
   搜过：`CardView.cs` 逐方法 · `CardArt.cs` · `TmpFont.cs` · `CardIcons.cs` · `PragatiDigits.cs` ·
   `BattleDriver.ToCardData/FaceTextFull` · `_tmp_view/**` 全部日志 · `S2_CardViewCreate拆解.md` · `S_A1750进战斗卡顿.md`。
2. 第二次开窗是否更快 —— 无日志（`run_v2.log`、`s27_shell.log` 都是进程内首次开窗）；`A1655④` 的判别实验**仍未跑**。
3. Standalone 平台块是否生效 —— 帧图 `.meta` 默认块 `compression 0/1024`、Standalone 块 `compression 1/2048` 但 `overridden: 0`；
   按 Unity 语义应走默认块，**未在 player 验证**。
4. `ArtBaker` 仍对非 backdrop 设 `Uncompressed`（`Editor/ArtBaker.cs:155-157`），是**导入期**设置；运行时只有 `Resources.Load`。
5. `GetPixels32` **不是每格** —— `_uvCache` 按 `Texture2D` 缓存（`CardView.cs:5256`）。
6. 字体图集**不是每次建字重建** —— 只有新字形入库（207→297）；暖路径 0.0 ms；冷路径单字代价未量。

## 五、2026-10-17 落地进度（第四十六会话）

- ✅ **第 1 步**：`Shell/CollectionWindow.RebuildCardsCells` 已改成按 `CardDef.Id + 语档` 池化 `CardView`
  （只重建张数条/命中区；屏外 park、上限 48）。滚动不再整屏重建。
- ✅ **第 2a 步（只加计时，不跑）**：`Core/CardView.Build` 已加分段计时（`BuildTiming`，**只在真 Play 出声**）：
  `图加载 / FrameMesh / 其他建层 / rim / 文字 / 收尾`。下一次真 Play 看 `[CardView.Build]` 日志即可定位 2.6 s 在哪一段。
- ✅ **第 2b 步（离线表）**：`Resources/frame_uv.json`（**150 张卡框**，23.8 KB）+ 生成器 `工具/gen_frame_uv.py`；
  `CardView.FrameUv` 先查表、查不到才退回运行时 `GetPixels32` 扫描。**公式与 C# 逐字同一套**（alpha<=8 / (x0+0.5)/W…）。
  ⇒ 开窗时那 ≈55 ms/格的 `GetPixels32` 成本已消掉。
- ⏭ **还欠**：真 Play 收一次 `[CardView.Build]` 分段（确认「立绘加载 vs 材质克隆/InfoTexture」谁是主因）；
  之后按 2a 结果决定 2c（协程分批 / 预灌字形）。
