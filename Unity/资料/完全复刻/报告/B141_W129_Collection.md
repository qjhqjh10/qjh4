# B141 · W129 · 写手 `B141-W32` 报告 —— 收藏/搜索/指针 5 条（3-13 · 3-28 · 3-10 · 4-07 · 4-15）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 波次 `W129` · 条目 **3-13 / 3-28 / 3-10 / 4-07 / 4-15(3961)**
> 判据来源：`判据/B141_W129_对账_卡组色彩.md` **P4**（3-10/3-13/3-28）· `判据/B141_W129_对账_UI动画.md` **W6**（4-07/4-15）·
> 审计 `报告/B138_审计汇总_问题清单.md` §3/§4。**行号一律现读**。
> ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表/活状态表/所有权表/CLAUDE.md。
> 白名单内改动 = `Shell/CollectionData.cs` · `Shell/CollectionWindow.cs` · `Shell/PointerLayer.cs` · 新建 `Editor/B141_W32_Assert.cs` · 本报告。

## 〇、五条一览

| # | 条目 | 原版判据（值/行为 + 出处） | 我们（改前 → 改后） | 落点 | 断言 |
|---|---|---|---|---|---|
| 1 | **3-13** 新建卡组默认名 | 新卡组 `deckName` = **空串**；名字由**首次 Done** 的 `ValidateDeck` 补成**督军本地化卡名**（`CardDeck__GetEmptyDeck.c:10-27` 从不给 `+0x10` 赋值 ⇒ `deck+0x10` 空；`DeckUtility__ValidateDeck.c:70-76`：`IsNullOrWhiteSpace` ⇒ `deckName = RawCardScript_GetLocalizedCardName(deckHero)`） | `Lib.NewUnsaved(「新卡组」)` 直接带上词条名 ⇒ `FillNameFromWarlord` **永远不可达** → `NewUnsaved` 之后 **`d.Name = ""`**（补名仍走既有 `DeckEditorState.Validate → FillNameFromWarlord`，本次未改那一处） | `Shell/CollectionData.cs:369`（`CreateDeck`）· 连带 `Shell/CollectionWindow.cs:5632`（`CreateDeck()` 的成败判定改读 `PendingNewDeck`） | A1–A8（9 条，含 A6a/A6b） |
| 2 | **3-28** 拖到视口外 | 🔴 **判据本身错**（见 §二）：原版 `OnBeginDrag` 把本体 `set_parentInternal(本体, transform.**root**)` ⇒ **跳出整条父链（含 ScrollRect 的 `RectMask2D`）**，`OnDrag` 每帧 `set_position(鼠标)` 无 clamp ⇒ **原版拖起来那一格也不被裁** | 我们**本来就不裁**（绝对摆位网格、不 reparent、只抬 `SetRenderQueue`）⇒ **等价，不改代码**；只把源码里那句错判据就地订正（铁律 5） | `Shell/CollectionWindow.cs:6026-6030`（注释订正，零行为改动） | E1–E5（5 条，钉住等价形态） |
| 3 | **3-10** 30°/150° 方向门 | `DragAndDropItem__OnBeginDrag.c:32-38`：`ang = 绝对值(atan2(dy,dx))·Rad2Deg`（`57.29578` @`0x1834b2e98`）→ 绝对值 → **`ang < 30 \|\| 150 < ang` 才拖格**（`30`@`0x1834b3168` · `150`@`0x1834b2d90`，`工具/read_literal.py` 现读）；**否则把 `BeginDrag` 转交 `ScrollRect`**（`:38`，虚槽 `0x3f8`），`OnDrag` 同转发（`__OnDrag.c:14-20`）。⚠️ 这道门**只对 `Initialize(父 ScrollRect)` 过的 `DragAndDropItem`** 存在 | 可拖件在**任意方向**都拖格、且可拖件无条件压过滚动区 → 新增 `PointerLayer.IsScrollDragAngle()`；**两条输入路**（`Update` / `MoveTo`）各一道门，越阈值那一拍按方向把这一下**转交 `HitScroll(按下点)`**；门只对 **`ScrollGate` 件**生效（= 原版那颗件的 `scrollRect` 那一档），收藏卡组格在 `AttachDeckDrag` 里 `RegisterDrag(..., true)` 打开 | `Shell/PointerLayer.cs:115`（`ScrollGate` 字段）· `:313/330`（`RegisterDrag` 新缺省形参）· `:473-476`（`Update` 门）· `:1462-1465`（`MoveTo` 门）· `:2032`（类尾 `IsScrollDragAngle`）· `Shell/CollectionWindow.cs:6140`（`true`） | D1–D14（14 条） |
| 4 | **4-07** 收藏 Deck 页搜索字号 | `…/Select Deck Tab/Deck Filters/Filters/Deck Name Filter/Input Field/Text Area/Text` = **`m_fontSize 35`** · auto[10~35] · base 26 | `30f` → **`35f`**（auto 三格 10/35/26 本来就对，未动） | `Shell/CollectionWindow.cs:4752` | B1–B3（3 条） |
| 5 | **4-15** 收藏窗关闭钮命中区 | 原版 `Collection Menu Variant.prefab` 的 **`Close Button`** Image `m_RaycastPadding = (-10,-10,-10,-10)`（负 = **外扩**，符号约定 → `MenuDraw.PaddedRect`）⇒ 命中区 **170×80** | `cr`（150×60）裸传 → `MenuDraw.PaddedHitRect(cr, ClosePad)`，`ClosePad = (-10,-10,-10,-10)`（新常量追加在**类尾**） | `Shell/CollectionWindow.cs:3961`（调用点）· `:7203-7209`（类尾常量 `ClosePad`，声明在 `:7209`） | C1–C3（3 条） |

**合计断言 = 34 条**（A9 · B3 · C3 · D14 · E5）。

## 一、逐条细节与「灭自证」

### 3-13（A 组）
- **A2** 比的字面量是 `d.Name == ""`（⛔ 不从实现读回）。删掉 `CreateDeck` 里那句 `if (d != null) d.Name = "";` ⇒ 名字回到「新卡组」⇒ 红。
- **A5** 是前提断言：`LoadDeck` 装进来时名字**仍是空串** —— `PlayerDeck` 构造器会把空名替成「新卡组」（`Core/DeckRules.cs:335`），靠 `LoadDeck` 末尾那句 `Deck.Name = deck.Name`（`Deck/DeckEditorState.cs:307`）逐格照抄才保得住。删掉那一句 ⇒ 3-13 **静默失效**，A5 当场红。
- **A6b** 合法 + 空名 ⇒ `Validate()` 后名字 == `CardText.Name(督军那张卡)`。
- **A7/A8** 是**结构性判别式**：督军名 ≠ 另一张卡的名 ⇒ 拦住「填的是第一张卡」那种错。
- ⚠️ **`NewUnsaved` 会把空名替成「新卡组」**（`RuleEngine/Data/DeckLibrary.cs:191`，**不在本笔白名单**）⇒ 只能在 `CollectionData` 里清。**建议后续把那一格改成「空就是空」**（那一处与 `Create(...)` 同名替法共用，需另开一件）。已记录，未改。

### 3-28（E 组）—— 为什么「不改代码」
`.c` 级判据（三轮都读过）：
1. `DragAndDropItem__OnBeginDrag.c:33-36`：本体 `set_parentInternal(本体, UnityEngine_Transform__get_root(本体))` ⇒ **跑到整条父链的最上面**；
   `RectMask2D` 挂在 `Viewport`（父链**中间**）⇒ reparent 之后**不再受它约束**。
2. `DragAndDropItem__OnDrag.c:37`：`UnityEngine_Transform__set_position(本体, 鼠标世界坐标)` —— **没有任何 clamp / 视口约束**。
3. 被裁的那一件是 **ghost**（`CreateEmptyObject.c`：空 `GameObject` + 挂回**原父链** + 世界位置对齐 + `localScale` 抄一个常量 + `SetActive(0/1)`），
   它的用途是**撑住 scroll content 的兄弟位**；我们这套是**绝对摆位网格**（无 LayoutGroup）⇒ 原位不会被挤掉 ⇒ **不需要 ghost**。
⇒ 原版语义 = 「拖起来的一格**浮在视口之上、不被裁**」；我们「不 reparent ⇒ 不裁」**与之一致**。
E 组断言钉住的就是这个等价形态：`DeckDragMove` 体内 **无 `SetParent`**、**无任何裁切调用**、位移走 `localPosition + 起拖基准`，抬层仍靠 `DeckDragQueueBoost`。
🧨 谁要是「为了补视口裁」往里加 reparent 或重裁 ⇒ E2/E4 红（那才是与原版不符）。

⚠️ **残留差异（如实记，不修）**：建格时**压在视口边上的那一格**会被 `MenuDraw.DeckCell` 的 `clip` 预先切掉一角；把它拖起来时，被切掉的那一角**不会回来**（原版 reparent 后是完整一张）。
影响面 = 只有「边界那一格 + 拖起」这一格瞬间；裁掉的是**≤1 格宽度**的细条。不修的理由：本工程的裁切是**建期几何切**（`MenuDraw.Rect` 按交集建 quad），没有「原始几何」可还原 ⇒ 要修就得改成运行期 shader 裁，属于**另一件结构性工作**（不在本条判据内）。

### 3-10（D 组）
- `IsScrollDragAngle` 与另一条**窄带** `SupportMethods__IsScrollDragThreshold`（**60°~120°**，给「卡组编辑行拖」用，条目 3-05）**不是同一个门** ⇒ ⛔ 别合并（会判错 30°~60°、120°~150° 这四段）。
- 两条输入路（生产 `Update` / 自检 `MoveTo`）**各一道**，避免「批处理绿、真机不对」。
- 🔴 **只在 `ScrollGate` 件上生效**（`DragSource.ScrollGate`，`RegisterDrag` 新增**缺省 false** 的尾参）：原版这道门属于**`DragAndDropItem.Initialize(父 ScrollRect)` 过的那批件**，
  而本仓的可拖件里还有别家（`Editor/ShellScene.cs:10730` 那个竖直拖探针）⇒ 一律套门等于**改了那些件的语义**（把它们的竖直拖抢成列表滚动）。收藏卡组格在 `AttachDeckDrag` 里传 `true`。
- 转交目标 = **`HitScroll(按下点)` 现取**（≈ 那颗件自己的 `scrollRect`），⛔ **不在按下那一刻就抢滚动区** ⇒ `DraggingScroll` 在未拖起时仍恒 null（`Editor/ShellScene.cs` ⑤ 那条「可拖件优先 / `DraggingScroll == null`」断言**不受影响**）。
- ⚠️ 如实记：我们传的是**按下至今的累计位移**；原版是 `eventData.delta`（越阈值那一帧的**帧**位移）。方向语义相同、噪声更小（5px 帧位移算角度会抖）。

### 4-07 / 4-15
- 4-07 只动那一个字面量；`autoMin/Max/Base = 10/35/26` 早在（A1212），**未动**。
- 4-15 用的是**图形自己的 `m_RaycastPadding`** 那一支（`MenuDraw.PaddedHitRect`），**⛔ 不是** `maskPad`（`RectMask2D.m_Padding`，另一个字段）——两个 pad 别混（`MenuDraw.Hit` 的 doc 里那条 A188 订正已写死）。
- 原版该值**逐字段现读**：`d:/2/tools/assetripper_out/ExportedProject/Assets/Collection Menu Variant.prefab` 里 `Collection Menu Variant` 那 10 处非零 `m_RaycastPadding` 中，节点名 `Close Button` 的正是 `(-10,-10,-10,-10)`。

## 二、⛔ 判据本身错（铁律 5 就地订正）

| 条目 | 原判据（审计/对账） | 反证 | 处置 |
|---|---|---|---|
| **3-28** 「原版会裁」 | `判据/B141_W129_对账_卡组色彩.md:51`「原版 `SetParent(root)` + ghost + **视口裁**；我们只抬队列」· `CollectionWindow.cs` 原注释「拖到视口外不会被裁（**原版会**）」 | `DragAndDropItem__OnBeginDrag.c:33-36` reparent 到 **`Transform.root`**（跳出 `RectMask2D` 子树）· `__OnDrag.c:37` `set_position(鼠标)` 无 clamp | **代码注释已就地订正**（`CollectionWindow.cs:6027-6031`），并在报告 §一 写全判据链。**代码行为不改**（原就等价） |

## 三、行数变化（账闸要看这一格）

| 文件 | 改前 → 改后（行） | `git diff --numstat` | 备注 |
|---|---|---|---|
| `Shell/CollectionData.cs` | 674 → **676**（+2） | `6 4` | 只在既有行上改 + 文档段 +2 行；**下游账行号 +2** |
| `Shell/CollectionWindow.cs` | 7239 → **7251**（+12） | `19 7` | 类尾新增 `ClosePad`（6 行）+ 3-10/3-28 注释 + `RegisterDrag(..., true)`；**下游账行号 +12** |
| `Shell/PointerLayer.cs` | 2017 → **2039**（+22） | `43 4` | `DragSource.ScrollGate` + `RegisterDrag` 尾参 + 方向门两处 + 类尾 `IsScrollDragAngle`（含判据注释）；**下游账行号 +22** |
| `Editor/B141_W32_Assert.cs` | 新建 **344** 行 | — | 断言件（34 条，零前提） |

> 🔴 **请调度台在「没有 `.cs` 写手 + 补丁合并完」时跑一次 `check_ledger_points --all-A --fix`** —— 这三个文件被账按 `文件:行号` 引用（`CollectionWindow.cs` ~200 行 · `PointerLayer.cs` 若干 · `CollectionData.cs` 5 行），本笔**必然带漂移**。
> ⚠️ 行尾：三份文件改后仍是 **纯 LF、无裸 CR**（`b.count(b'\r\n') == 0` · `bareCR == 0`，二进制现数）。

## 四、白名单外需要跟进的 3 处（**① ② 已由 `B141-W32b` 续做完毕，见 §八**；③ 判定「不用改」）

1. ✅ **已续做（§八 #1）** —— **`Editor/CollectionScene.cs:12058`**（A503 那一节控制组）：
   ```csharp
   string okCreate = CollectionData.CreateDeck();
   CheckTrue(!string.IsNullOrEmpty(okCreate), "（控制组）好路径 `CreateDeck` 回**名字**…");
   ```
   **改后 `CreateDeck` 返回空串**（= 原版「未命名」）⇒ 这一条会**红**。前提已变，建议改成：
   ```csharp
   string okCreate = CollectionData.CreateDeck();
   CheckTrue(CollectionData.PendingNewDeck != null && okCreate.Length == 0,
             "（控制组）好路径 `CreateDeck` 交出 `PendingNewDeck`，且照原版**未命名**（回空串）");
   ```
   ⚠️ 同一文件 `:1768/:1780` 那两节用的是 `win.CreateDeck()`（void）+ `PendingNewDeck`，**不受影响**。
2. ✅ **已续做（§八 #2）** —— **`Shell/LiveOpsEventWindow.cs:1756`**：`bool created = !string.IsNullOrEmpty(name);` ⇒ 原恒 `false`，会打一条**假**警告
   （「新建卡组没造出来」）但**行为不变**（后面照样开编辑器）。建议改成 `bool created = CollectionData.PendingNewDeck != null;`。
3. **`Editor/DeckScene.cs:1009`**（A1697 那节）：`Check(nrt.State.Deck.Name, nmN)` —— 两边现在**都是空串** ⇒ **仍绿**（名字相等），**不用改**；但如果有人把返回值改回非空串，请同步改这里。

## 五、typecheck

`TMPDIR=/tmp/wf_b141_w32c bash 工具/typecheck.sh`：**运行时 0 错 · 编辑器 0 错**（本笔三份改动文件 + `Editor/B141_W32_Assert.cs` 全部编过）。

- ⚠️ **过程如实记**：前两次跑的时候运行时程序集有 **20~25 条 `CS0103`，全在 `Deck/DeckRuntime.cs`**
  （`_armPrevPx` · `_rowDragArmed` · `_dragDetached` · `PassedDragStart` · `ArmScrollDrag` · `HandleScrollDragTick` · `TickRowDrag` · `DroppedOnDeckPanel` …），
  是 **P1 那一路（3-05/06/07/08/09）正在该文件里补字段/方法**（= `CLAUDE.md` §13·3 那条「错误全部集中在不是你负责的文件上 ⇒ 那不是你的问题」）。
  编辑器程序集当时因为 `CS0006`（`WFCheck.dll` 没生成）**没编出来**；**第三次跑（该路已交回）时 0/0**，断言件因此**已得到编译验证**。
- ⚠️ 仍未做：**没有跑任何 Unity 宿主**（本笔红线）⇒ 断言**一条都没执行过**；`B141W32_Assert.Run()` 已按 §七 接线（`CollectionScene.cs:13313` 合计行，零行数变化），等调度台起宿主读日志里那 34 行 `B141W32`。

## 六、❓ 查不到 / 不闭合

- 无整条 `❓`。两处**已如实标注**的未闭合：
  1. **4-15 的「两框是否已互相覆盖」**（审计自留）—— 本笔只按 `m_RaycastPadding` 外扩，未验证「关闭钮命中区与相邻钮是否重叠」；那需要 `runtime_ui_dump`，⛔ 本笔不猜。
  2. **3-28 边界那一格被预切**（见 §一残留差异）—— 不修，理由已写。

## 七、断言接线（✅ `B141-W32b` 已接）

宿主 = `CardPresentation/Editor/CollectionScene.cs` 的**合计行（`:13313`）**，追加在**该行第一个 `//` 之前**（现读：第一个 `//` 在列 1100、插入点在列 993 ⇒ 落在代码区）：

```csharp
… _sink.Fail += B121_COLLFIX_Assert.Fail; B141W32_Assert.Run(); _sink.Pass += B141W32_Assert.Pass; _sink.Fail += B141W32_Assert.Fail; int total = …
```

- **同一物理行追加 ⇒ 行数零变化**（13842 → 13842）。
- ⚠️ 这条纪律来自本仓 6 次「代码落进行末注释 ⇒ typecheck 过、账绿、断言一条没跑」的先例；本次**插入点在第一个 `//` 之前**（二进制现量过列号）。
- 本件**零前提**：不建 GameObject、不切场景、不写盘；`CreateDeck` 不入库不落盘，且结束把 `PendingNewDeck` / `PendingEditDeck` **原样还回去**（宿主 A503 那一节的 `PendingNewDeck` 因此不受影响）。

## 八、续做 `B141-W32b`（2026-11-04 · 第一百二十八会话）

调度台派单：把 §四 点名的两处**白名单外**调用改掉 + 接线。

| # | 文件:行 | 改前 | 改后 | 行数 |
|---|---|---|---|---|
| 1 | `Editor/CollectionScene.cs:12058-12059`（A503 控制组） | `CheckTrue(!string.IsNullOrEmpty(okCreate), "（控制组）好路径 `CreateDeck` 回**名字**…—— 恒返空串的实现 ⇒ 这条红")` | `CheckTrue(CollectionData.PendingNewDeck != null && okCreate.Length == 0, "（控制组）好路径 `CreateDeck` 交出 `PendingNewDeck` 且照原版**未命名**（回空串…）—— 🔴 就地改判据（铁律 5）：空串现在是**正确值**，不是失败")` | 13842 → **13842（零变化）** |
| 2 | `Shell/LiveOpsEventWindow.cs:1756` | `bool created = !string.IsNullOrEmpty(name);` | `bool created = CollectionData.PendingNewDeck != null;`（原来恒 `false` ⇒ 打一条**假**「新建卡组没造出来」警告；行为不变：照样开编辑器） | 2511 → **2511（零变化）** |
| 3 | `Editor/CollectionScene.cs:13313`（合计行） | 行尾是 `…B121_COLLFIX_Assert.Fail; int total = …` | 在 `int total` **之前**插入 `B141W32_Assert.Run(); _sink.Pass += B141W32_Assert.Pass; _sink.Fail += B141W32_Assert.Fail;` | 同上，**零变化** |

**验证**：`TMPDIR=/tmp/wf_b141_w32b bash 工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**。
**灭自证**：① 把 `CreateDeck` 里的 `d.Name = ""` 删掉 ⇒ `okCreate.Length == 0` 失败 **且** `B141_W32_Assert` 的 A2 红（两处不同源：一处查宿主口径、一处查名字值本身）；
② `LiveOpsEventWindow` 若改回名字判据 ⇒ 恒 `false` ⇒ 那条假警告回来（日志面可观测，`Command` 见 `[Event] 新建卡组…` 那行）；
③ 接线若落在第一个 `//` 之后 ⇒ 编译过、账绿，但 `B141W32 ✓` 一条都不会出现在日志里（本仓已有 6 次先例 ⇒ 判据 = **看日志里有没有 `B141W32` 那 34 行**）。

> ⚠️ **顺手发现（不属本笔）**：`Shell/LiveOpsEventWindow.cs` 里还有 **`B141-W23` 的 4-15 改动**（`:1488` 的 `HelpHitPad` 与类尾常量）—— 那是另一个写手在同一文件里的活，⛔ 别当成本笔的。
