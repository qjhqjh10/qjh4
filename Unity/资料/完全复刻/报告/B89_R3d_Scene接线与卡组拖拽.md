# B89_R3d · `ShellScene` 接线 + 卡组拖拽（`DragAndDropItem` ×10）

> 2026-10-28 · 代理 `R3`（第三轮）
> 账补丁 = `账/_parts/B89_R3d.tsv`（**11 行 × 11 列**，`轮次 = B89-R3d`；其中 **1 行属 `未分类_其他.tsv`**）
> `typecheck` = **运行时 0 / 编辑器 1** —— 那 1 条在 `Assets\RuleEngine\Editor\EnchantmentTemplateCheck.cs`（**E13b 的活，⛔ 不归我**）；**我的 2 个文件 0 条**。
> 改动文件：`Editor/ShellScene.cs`（**+1 行**）· `Deck/DeckRuntime.cs`（**+51 / −4**，CRLF 未变：`lf 9191 == crlf 9191`、`bare_cr 0`）
> ⛔ 未跑 Unity / ruleprobe / git；未写 `.meta`。

---

## §1 逐条

### ① `Editor/ShellScene.cs` —— `RaycastFilter` 三条断言接线（账 `未分类_其他` 230）

| 项 | 内容 |
|---|---|
| 改哪 | `RunCore()` **末尾、合计行之前**：`MenuCheck.RaycastFilterAsserts(_sink);` + 一行注释 |
| 为什么放这儿 | 断言必须在 `Debug.Log("=== 合计 …")` **之前**跑，否则那 3 条不计进本次合计；`Run()` 本体只是 `try { RunCore(); } finally {…}` 的开关壳 |
| **行数变化** | **+1 行**（`ShellScene.cs` 10713 → 10714） |
| 落点核对（按你的要求） | 本文件在账里的最大落点 = **`ShellScene.cs:10338`**；插入点 ≈ **`:10644`** ⇒ **新增行之后没有任何账落点**（我逐个核了 `grep -oh "ShellScene\.cs:[0-9]*" 账/*.tsv` 的全部取值：10218 / 10220 / 10302 / 10303 / 10304 / 10332 / 10334 / 10338，**全部 < 10644**） |
| 观测口 | 跑 `ShellScene.Run` 后日志里多一段：`--- RaycastFilter（教程期命中白名单） ---` + 逐条 `✓`；那 3 条失败会计进 `_sink.Fail` 并进「失败重列」 |
| 前置 | 断言本体在 `Editor/MenuCheck.cs` 类尾 `RaycastFilterAsserts(CheckSink, string)`（R3c 落的那段） |

### ② `Deck/DeckRuntime.cs` —— `DragAndDropItem` ×10

**P1 那条（101 `CheckScrollDrag`）本轮落地**：

| 项 | 内容 |
|---|---|
| 判据 | 原版 `DragAndDropItem.cs:361`：`CheckScrollDrag` = 指针离滚动区边缘 **50px** 内 ⇒ 每帧滚动 **10px**；原版在 `Update()` 里每帧调一次 |
| 新增（**全部追加在类尾**） | `DragEdgeBandPx = 50f` · `DragEdgeSpeedPx = 10f` · `_dragSlotView` · 读口 `DragAutoScrollDeltaForTest` · 纯函数 `DragEdgeScrollFor(pointerY)` · `DragAutoScroll(pointerY, keepWorld)` |
| 接线（4 处，**全是 1 行替 1 行，净 0 行**） | ① 拖拽块里那一行末尾追加 `DragAutoScroll(px.y, wp);`（每帧）② `StartRowDrag` 记 `_dragSlotView = viewRow` ③ `RefreshDeckList` 里 `ClipRowToBand(...)` 前加闸 ④ `ClipRowLabels(...)` 前加闸 |
| 🔴 **结构差 + 等价物（必须知道）** | 原版被拖的那件在 `OnBeginDrag` 就**脱离** `ScrollRect`（拖拽层上的克隆）；我们这棵树的被拖行**仍挂在列表里**，每帧要被 `ClipRowToBand` 裁到视口内 ⇒ 一滚出视口就会被裁没。⇒ 加了闸「**正在拖的那一槽这一帧不裁**」（`_dragging && i == _dragSlotView`）。**原版靠「脱离」，我们靠「不裁这一槽」**——这是等价物，⛔ 别写成原版写法 |
| 数值 | 贴上沿（`y < 366+50`）⇒ `−10`/帧 · 贴下沿（`y > 1010.06−50`）⇒ `+10`/帧 · 中间 ⇒ 0；结果夹到 `[0, MaxDeckScrollPx]`（到顶/到底不刷新） |
| 观测口 | ① 纯函数四档可断言（`DragEdgeScrollFor(400) == -10`（上带 = y<416）· `(1000) == +10`（下带 = y>960.06）· `(600) == 0` · `(300) == -10`）② 读口 `DragAutoScrollDeltaForTest` ③ **手感只能真机验**（见 §3） |

**其余 9 条：逐条对账做完，结论写进账（8 条仍 B · 1 条陪跑判 A）**

| 账行 | 原版 | 我们的等价物 | 结论 |
|---|---|---|---|
| 111 `Update()` | 每帧 `CheckScrollDrag()` | 与 101 是同一件的两半，已在同一调用点接上 | **A** |
| 100 `Awake()` | `autoInitialize`：找父 `ScrollRect` + 建**静态空位占位件**，找不到 `LogError` + `enabled=false` | 我们**搬真行**（`_dragQuad = _deckRowBg[viewRow]`），无占位件、无 ScrollRect | **B**（无对应物，≠ 原版没有） |
| 102 `CreateEmptyObject()` | 建静态空位占位 | 同上 | **B**（要照做需先把被拖行脱离列表，会连带改 `_btns` 命中表与「可见性归 `ClipRowToBand` 管」那条约定 ⇒ 风险 > 收益） |
| 105 `Initialize(ScrollRect)` | 绑定滚动区 | 不走 uGUI ScrollRect | **B** |
| 106 `OnBeginDrag` | 记起点偏移 + 开占位 | `StartRowDrag`（记 `_dragOrigin/_dragRow/_dragSlotView`） | **B**（只等价「记起点」一半） |
| 107 `OnDestroy` | 销毁静态占位 | 无占位 ⇒ `EndDrag` 里置空即可 | **B** |
| 108 `OnDrag` | 跟随指针（带起点偏移） | 拖拽块 `_dragQuad/_dragText` 同步到 `wp` | **B**（等价，但搬的是真行不是克隆） |
| 109 `OnEndDrag` | 回位 / **落到目标槽（重排）** | 拖出右缘 ⇒ **删除**；没动 ⇒ 开卡窗。**缺「列表内部松手 ⇒ 重排」** | **B（卡在别处）** |
| 110 `Toggle(bool)` | 开关组件 | `_dragging` 状态位 | **B**（语义等价，无可开关组件） |

🔴 **109 卡在哪（这是本轮最要紧的一条发现）**：原版 `DragAndDropItem` 的 **5 处实例父链全是 `Collection Deck`** ⇒ 它真正的用途是**卡组格重排**（用户 2026-10-25 明确允许做）。而重排要 `Deck/DeckEditorState.cs` 加一个「移动」API —— 那个文件只有 `TryAdd` / `TryRemove`（`DeckEditorState.cs:473/510`），**没有 Move/Insert**，且**不在本轮给我所有权**里。⇒ 重排这条链路**缺的不是判据，是那个文件的一次写权限**。

---

## §2 typecheck

| 次 | 读数 | 说明 |
|---|---|---|
| 1（`ShellScene.cs` 接线后） | 运行时 0 / 编辑器 0 | — |
| 2（`DeckRuntime.cs` 四处 1:1 替换后） | 运行时 0 / 编辑器 0 | 顺带确认 `DragAutoScroll` / `_dragSlotView` 在 `RefreshDeckList` 里可见（同一 partial/类内） |
| 3（类尾追加后） | 运行时 0 / 编辑器 0 | `Mathf.Clamp` / `MaxDeckScrollPx` / `ListY/ListH` 全在同文件 |
| 末次（收工复跑） | **运行时 0 / 编辑器 1** | 那 1 条 = `RuleEngine/Editor/EnchantmentTemplateCheck.cs`（**E13b**）。**我的文件 0 条** |

---

## §3 真机待验（这条断言测不出，必须人去点）

**验什么**：拖一行卡组行**贴着列表上/下沿**停住，列表应该**持续滚动**（10px/帧 ≈ 600px/秒），而且**被拖的那一行始终跟着鼠标、不会被裁掉**。

**怎么点**（`-wfscene Deck` 起 player，`工具/wf_click.py`）：
1. 进卡组编辑页，卡组里放 **> 12 张**（一屏放不下，右栏能滚）。
2. 按住一行（不松），把指针移到**右栏列表最下方 50px 以内**并**停住 2 秒** —— 期望：列表向下滚、内容持续上移、被拖行贴在指针上不消失。
3. 移到**最上方 50px 以内**停 2 秒 —— 期望：向上滚，滚到顶就停（**不抖、不越界**）。
4. 全程松手在列表外 ⇒ 仍是「拖出删除」（旧行为不变）。

**看到什么算不对**：① 到了边缘列表纹丝不动（= 没接上）② 被拖的那一行**突然消失**（= 那道「不裁这一槽」的闸没生效）③ 滚到底还在继续请求（= 没夹 `MaxDeckScrollPx`）④ 滚动时整屏闪/行错位（= `RefreshDeckList` 的槽位复用被打乱）。

**读数**（不走手感也能验一半）：`DragAutoScrollDeltaForTest` 在边缘带内应等于 `∓10`；`DragEdgeScrollFor` 四档纯函数。

---

## §4 没查清

1. **原版 `CheckScrollDrag` 的「10」是 px/帧还是 px/秒**：我按**每帧 10px**（`Update` 里调）实现；若原版实际乘了 `deltaTime`，60fps 下差 60 倍。判据正文我只读到「阈值 50px / 速度 10」（账 101 差异列），**量纲未核实**。
2. **`OnEndDrag` 的重排落点判定**（松手时落在哪一槽）没读 —— 它依赖 `DropHandler`（我们没有）。
3. **`_dragSlotView` 与 `RowVisible = 12` 槽位复用的交互**：拖拽期若 `RefreshDeckList` 把 `firstRow` 挪了，`_dragSlotView` 指向的槽会对应到**另一张卡**（显示是旧卡、逻辑仍是旧卡 —— 因为 `_dragRow` 是**数据行号**不是槽号）。⇒ 滚过一整屏后可能出现「拖的是 A、松手删的是 A（数据对）」但「视觉上贴指针的那一行已经换了名字」的错觉。**真机看第 2 步时留意**。

---

## §5 交接

| 事项 | 状态 | 归谁 / 下一步 |
|---|---|---|
| `RaycastFilter` 断言接线 | ✅ 完成（`ShellScene.cs` +1 行） | 同步点跑 `ShellScene.Run` 看新段 |
| 101 / 111 边缘自动滚 | ✅ 代码落地 | **真机验**（§3 四步） |
| 100/102/105/106/107/108/110 | B（无对应物 / 只等价一半） | 要真做需「被拖件脱离列表」的重构（会动 `_btns` / `ClipRowToBand` 可见性归属） |
| **109 重排（= 原版真正的用途）** | ❌ 卡在 `Deck/DeckEditorState.cs` 没有 Move/Insert | **请给那个文件一次写权限**（或另派写手）：加 `TryMove(int from, int to)`，再在 `EndDrag` 里补「列表内部松手 ⇒ 落槽」 |
| 施工单 G2+G2b（13 条）· G6（733 处对表）· 822 接线 · 761 判据 | 未做 | 见 `报告/B89_R3c_…` §5 与 `判据/B89_R3_菜单外壳施工单.md` |
