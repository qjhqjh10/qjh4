# W58-BTFIX3 · `BattleScene.Run` 剩余 28 条 · 修复记录（2026-10-20）

> 输入判据 = `资料/普查产出_1020/W58_诊断_BattleScene剩余28.md`（下称「诊断」）。
> ⛔ 本轮**未跑 Unity / 未跑 `*.Run` / 未动 git** —— 下面所有结论都是**静态改动**，红/绿要下一次复跑才见分晓。
> 白名单内改了两个文件：`Editor/BattleScene.cs`（全部测试侧）· `Battle/BattleDriver.cs`（只新增 `PopOffsetWorldFor` 读口 + 一处调用点改读它）。

---

## 〇、结论

- **修了 24 条**（诊断 28 条里除 4 条 P2 γ 之外全部）；**留 4 条 γ 未修**（#18 语音灭自证 / #21 手牌数字框高 / #27 #28 联机 `SetMySeat`，依据见 §四）。
- 其中 **P0（5 条同源）** 用的是诊断给的第一修法：**点亮之后先真渲一次**（新助手 `RenderOnceForTest`，`BattleScene.cs:33234`），并加了 `Graphic.depth != -1` 的**前提断言**（A1 / A2 每颗 / A985 压暗层三处）。
- **本批唯一真缺陷（B4 敌方飘字缩放没同步到断言）** 已按铁律 6 收口成一个只读口 `BattleDriver.PopOffsetWorldFor(seat)`（`BattleDriver.cs:16742`），生产与断言都读它。
- `W58④` 整段已搬到 `DestroyImmediate(driver)` **之前**（现在 `BattleScene.cs:30264..30583`，销毁在 `:30585`）。⚠️ 副作用：这一整组（①②③④）此前因 `driver == null` **一次都没真正跑过**，本轮之后**第一次真跑**，下次复跑若冒出新的红，属「新暴露」而不是「回归」。
- typecheck：**运行时 0 / 编辑器 0**（跑了两遍，最后一次在所有改动之后）。

---

## 一、P0 逐条（T09 射线簇 5 条 + A985 2 条）

### 1. 新增渲一次助手 `RenderOnceForTest()`（`BattleScene.cs:33214` doc + `:33234` 实现）

- 渲法与 `Shot` 同一条：`FindBoardCamera()` 拿到 3D 战场相机（HUD 是它的 Overlay 子相机），渲进一张 `Screen.width × Screen.height` 的临时 RT，渲完还原 `targetTexture / RenderTexture.active`。**不读回像素、不写 PNG**。
- 依据：`Graphic.depth => canvasRenderer.absoluteDepth`（`Graphic.cs:393`）只在**真实渲染**时赋值；`Canvas.ForceUpdateCanvases()` 只跑几何重建。`GraphicRaycaster.cs:324` 跳过 `depth == -1`。

### 2. T09-A1③ / A1⑤（日志 96657 / 96820）

- `driver.SetHudButtonActiveForTest("offensive", true)` → `SyncRaycasters()` → `ForceUpdateCanvases()` → **`RenderOnceForTest()`** → **前提断言**（`BattleScene.cs:29759-29763`）：
  `Check(imgA1.depth != -1 && !imgA1.canvasRenderer.cull, "★ T09-A1③（前提）：这颗钮已被画布处理过…")`。
- 射线/`Contains` 那两条逻辑**一个字没动**（A1⑤ 仍要求 `Contains == Raycast`）。

### 3. T09-A2[cameraReset]⑤（日志 97560）

- A2 段把六颗全部点亮之后、打射线之前加 `RenderOnceForTest()`（`BattleScene.cs:29920`）。
- 逐颗加前提（`BattleScene.cs:30023-30027`）：`img2.depth != -1 && !img2.canvasRenderer.cull`。

### 4. A985③ 两条（日志 92524 / 92550）

- `SyncRaycasters()` + `ForceUpdateCanvases()` 之后加 `RenderOnceForTest()`（`BattleScene.cs:27963`）。
- 压暗层加前提（`BattleScene.cs:27977-27982`）：`shadeGfx985.depth != -1`（`ShadeGO.GetComponent<Graphic>()`）。
- 「再开一次验点字」那条重开之后**再渲一次**（`BattleScene.cs:28019`）——`SetActive` 切换会让画布重建。
- ⚠️ 诊断 §九 的推论「`depth == -1`」本轮仍**没有直接读数**——但那三条前提断言会在下次复跑把 `depth` 打出来，天然定案。

### 5. 「加渲后仍不中」的待查项（**未改，按诊断留口**）

若复跑这 5 条仍红，按诊断 §1·1 的最后一段查**嵌套子画布的 `GraphicRaycaster.eventCamera`**：
`EnsureSubCanvas`（`Battle/HudCanvas.cs:529-543`）没设 `renderMode/worldCamera`；若嵌套 Canvas 的 `renderMode` 回落成 `Overlay`，`GraphicRaycaster.eventCamera` 会退成 `null`（`GraphicRaycaster.cs:299-309`），于是把屏幕点当世界点比矩形。**先打 `canvas.rootCanvas.renderMode / rootCanvas.worldCamera / canvas.renderMode` 三个字段再下结论**——成立的话那是**生产缺陷**（所有面板真机点不动），不是夹具问题。⛔ 本轮没猜着改。

---

## 二、P1 逐条

### 6. T09-A2[settings]⑤ / [endTurn]⑤（日志 97001 / 97937）

- 新增三态助手（`BattleScene.cs:29960-29973`，用在 `:30032-30038`）：
  `HitsA2At(...)` 在 `!LayoutSpace.IsInsideScreen(screen)` 时**返回 `null`（跳过并记读数）**，否则走原来的 `HitsA2`。
- 判定改成：内点 `!= false` / 外点 `!= true`（`BattleScene.cs:30032-30038`），并在两条断言文案里打出「屏外跳过 N 点」。
- 实测那两点正是 `settings 上内 1 = (613.95, 486.93) > 480` 与 `endTurn 右内 1 = (646.26, 274.89) > 640`（诊断 §1·2）。
- ⛔ 没有动 `raycastPadding`（原版判据），也没有把「内 1 px」放宽（那会放过「贴边点不中」）。

### 7. T16 挪出命中区 / 卡外松手（日志 63627 / 63640）

- 边界点改为**在 `SimulateHover` 之后**取（`BattleScene.cs:19920-19926`）：先用卡心（`HitLocalToWorldForTest(0, dy)`）把 hover 打上，再按**悬停后**的 transform 取 `inA/inB/outB`（`HitLocalToWorldForTest` 自带 1.3× 缩放 + 抬高 30 px）。
- 「真 collider 前提」「命中就是 cvT16」那两条顺延到 hover 之后跑（仍逐字保留判据）；`TickPressCandidate` 的判据口**没动**。

### 8. T03 手牌 / 场上 y 边界（日志 91431 / 91595）

- `edge03`（`BattleScene.cs:27107-27122`）与 `edgeU03`（`:27219-27234`）照 `edgeM03` 的写法加 `bool vertical`，返回 `vertical ? ly : lx`；四个调用点补 `false/true`。
- 命中判断本身没动（横向两条一直是绿的）。

### 9. T09-D① 文件名比较（日志 98059）

- `d1Hits` 从 `List<string>`（`"文件名:行号"`）改成 `List<(string file, int line)>`（`BattleScene.cs:30211`），判据比 `.file == "ErrorMessageBanner.cs"`，文案自己拼 `file:line`。

### 10. B2·偏差8 / B3·偏差9 夹具（日志 95010 / 95178）

- `Begin()` 后玩家先手 ⇒ 敌方棋盘必空。在「偏差 8」之前**显式摆一个敌方单位**（`BattleScene.cs:28764-28787`）：
  取敌方手牌里第一张 `unit` 卡的 `CardDef`（没有就回落 `CardByName(StarterCards.Tide(), "Tide Minion")`），
  落在 `BoardSlots.SlotOf(BoardSlots.Right, 0)`（= 5，**贴督军那格**，⛔ 不写死格号），再 `RefreshAll → DrainQueue → StepThrough → 排空闸`。
- B3 段复用同一局，因此 #14 一并解决（它读的就是这个 `b3slot`）。

### 11. E / F 落点（日志 99587 / 99639）

- 改读 `PlayCard(..., out int landedSlot)`（`BattleScene.cs:31929` / `:31956`），判据比 **实际落点那一格**；文案里打出实得格号。
- 连续无洞模型的「空盘请求 2 号格 ⇒ 夹到 3 号」**没动**（既定语义）。

### 12. W58④「找得到 driver」（日志 99950）

- 整段 W58（①②③④，原 `:31603..31922`）搬到 `UnityEngine.Object.DestroyImmediate(driver)` **之前**（现 `:30213..30532`，销毁 `:30534`），**整体 +4 缩进**以对齐外层块。
- 搬运用脚本做：按「`// ==== 🆕 2026-10-20（战斗线 \`W58\`）`」与「`=== 结束` 之前最后一个 `}`」定位，**断言过内容再切**；`CRLF` 逐字节保留（改前改后都是 33874 个 `\r\n`）。
- ⚠️ 见 §〇 的副作用说明：这组断言此前从未真正执行。

### 13. 飘字 2 条（日志 51758 / 51771）——本批唯一真缺陷

- 新增只读口 `BattleDriver.PopOffsetWorldFor(int seat)`（`BattleDriver.cs:16742`）：
  `PopOffset * (PopNumber.CardLocalToOursFor(seat != _me) / PopNumber.CardLocalToOurs)`（比值 0.9071 = 原版 59.48/65.57）。
- `PlayHitFeel` 改用同一个口（`BattleDriver.cs:16719`），**⛔ 不再内联那三目算式**。
- 断言：
  - 「飘在挨打那张卡上」→ `popBase + drv.PopOffsetWorldFor(popSeat)`（`BattleScene.cs:16286`；`const int popSeat = 1` —— 受击者由 `FreeSlot(drv.Ctx, 1)` 摆出）。
  - 「不是落在 2D 行线上」→ 第二个靶也乘**同一档**（`:16300`）。这一步是关键：不乘的话两者只差 `0.0929×|PopOffset| ≈ 0.031`，比不出「落在哪」；乘了之后差的才是**两个基准之间的距离**（≈0.47~0.74），这条重新有鉴别力（驱动层与断言若**一起**改回 `SlotPosition`，两靶重合 ⇒ 仍然红）。

### 14. 提示正文（日志 82366）

- 期望值按生产链现算（`BattleScene.cs:25295-25308`）：`Loc.OfficialCurrent("TutorialData/" + textReference)`，取不到回落 `arg`，记号过 `CardIcons.RewriteMarks(…, Loc.Current == AvailableLanguages.Chinese)` —— 与 `BattleDriver` 的 `SmallTip` 分支**逐句同链**。
- 旧注释「本地没有 I2 词条表」就地改成「官方表 2026-10-18 已并入」。⛔ 没改生产去直接印 `arg`。
- ⚠️ **没有**照任务单写的 `Loc.T(tipAct.arg)` —— 现核生产（`BattleDriver.cs:12193-12202`）走的是
  `Loc.OfficialCurrent("TutorialData/" + textReference)` + `CardIcons.RewriteMarks`，**不是** `Loc.T(arg)`
  （`arg` 只是取不到官方法时的兜底）。期望值按**生产实际那条链**算，否则会换成另一种假红。

### 15. 冻结 2 条（日志 87835 / 87887）

- `Begin()` 之后先排空：`if (driver.InMulligan) SimulateMulliganDone(); StepThrough; while (WaitingForBlockingAction) Step(1/30)`（`BattleScene.cs:26004-26011`）。
- 依据：`FrozenNow = _frozenForTest || Frozen()` 而 `Frozen = PlayerInputFrozen || WaitingForBlockingAction`（`CardInteraction.cs:200` / `BattleDriver.cs:3726`）。

### 16. G-2 教程跳过 2 条（日志 88789 / 88802）

- 原靶是**敌方督军视图**，教程开局它被藏着（⑤ 段自己验过 `!activeSelf`）⇒ `Physics2D.OverlapPoint` 打不到停用 collider ⇒ 被判成「空处」。
- 改成：优先**我方督军视图**，拿不到就扫 `MyUnits` / `FoeUnits` 找**任意一张 `activeInHierarchy`** 的场上视图（`BattleScene.cs:26139-26150`），并当场把前提钉成 `g2Self != null && activeInHierarchy`。
- ⛔ 没有往命中表里加「不活跃视图也算」（原版 `Physics.RaycastNonAlloc` 同样打不到停用件）。

---

## 三、没改的（P1 里没有的）

- 诊断里提到的 **B5 红没有出现在本轮 28 条**，按诊断不按条数写死，本轮不动。

---

## 四、P2 未修依据（γ，4 条）

| # | 条目 | 依据 |
|---|---|---|
| 18 | 精确语音 ≠ 兜底（灭自证）`exDiff=0` | 既有：`git diff HEAD -- Core/VoiceLines.cs` 空、`cards[].audio` 696 条 HEAD/工作区相同。诊断建议「降成读数或另找结构性见证者」——那是**判据口径的裁断**（会改变这条断言的性质），按铁律 13·8 属主对话/正本层级；且不在白名单文件里。**未改**。 |
| 21 | 手牌数渲染高 ≤64 | 生产侧量：框 64 px < 我们的最小行盒（50 px 字号 × 1.437 ≈ 72 px），结构上装不下。两条修法都要**动生产常量 `HandLabelFsMin`（BattleDriver.cs:1573）或换字体资产**——本轮白名单对 `BattleDriver.cs` 只允许新增 `PopOffsetWorldFor`，且诊断把它归 P2/γ。**未改**（⛔ 也没有把断言放宽成 ≤80）。 |
| 27/28 | 联机「主机视角两边视图都在 / 槽 1 是我的兵」 | 既有生产限制：`SetMySeat` 中间翻座位不重建身份表/不清 `DeployPending`（`BattleDriver.cs:2811-2816`），HEAD 已如此；修复要动 `SetMySeat` + `RefreshAll` 的生产逻辑，超出本轮白名单。**未改**。 |

---

## 五、typecheck

```
$ TMPDIR=/tmp/wf_btfix3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```
（最后一次在所有改动之后跑；中途修过一个自己的符号错：`e` 不在作用域、`pass` 名字冲突，均已改正。）

## 六、行尾 / 改动范围自检

- `BattleScene.cs` / `BattleDriver.cs` 改动前后都是**纯 CRLF**（`b.count(b'\r\n') == b.count(b'\n')`：22882 / 33916），**没有翻行尾**。
- 只用 Edit 工具与一次二进制读写脚本（`BATTLE_CRLF` 保留）；⛔ 没有 `sed -i`。
- 未碰白名单外任何文件；未改正本（`CLAUDE.md` / `项目任务.md`）。

## 七、下次复跑时要盯的三件事

1. **T09 五条的 `depth` 读数**（前提断言现在会打出来）：若仍 `-1` ⇒ 查 §一·5 的嵌套子画布 `eventCamera`。
2. **W58④ 整组第一次真跑**：可能冒出新红（与「修没修好」无关，是之前根本没跑到）。
3. **B2 夹具**：若敌方手牌没有单位卡，会回落 `Tide Minion`；若那条夹具仍红，先看它的读数（卡名 / 格号 / `IsDeployable`）。
