# W_G3 · 拖拽件（`DraggableController<T>` 两个实例）—— 写手报告

> 执行：**写手代理 G3**（2026-10-17）· 白名单：新建 `Core/DraggableController.cs`(+`.meta`) · `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs`（只补断言）· 本文件。
> 判据全文（五问 / 证据坐标 / 5 条没查清 / 施工草图）= `资料/普查产出_1017/查证_Cosmetic拖拽预览.md`。
> **没跑 Unity**（拖拽是交互）；断言**写好不跑**，由主对话在同步点跑 `DeckScene.Run`。

---

## ① 通用件：接口与字段 → 原版出处

`Assets/CardPresentation/Core/DraggableController.cs`（新建，LF，一个文件收原版三件）：

| 我们的 | 原版 | 出处 |
|---|---|---|
| `interface IDropHandler<T> { void Drop(T content); }` | `IDropHandler<T>`（**不是** uGUI 那个收 `PointerEventData` 的） | `Warpforge_code/.../IDropHandler.cs:1-4` |
| `abstract class Draggable<T>`：`canvasGroup` / `content` / `CurrentItem{get;protected set;}` / `abstract Initialize(T)` / `OnDrag()` / `OnRelease()` | 同名 | `Draggable.cs:5-18`；⚠️ `OnDrag/OnRelease` **无产物**，留空并标「没查清」 |
| `class DraggableController<T> : MonoBehaviour, IBeginDragHandler/IDragHandler/IEndDragHandler` | 同名 | `DraggableController.cs:4`；字段 `+0x20 draggable` · `+0x28 dragSound` · `+0x30 dragging`（`dump.cs` 与指令流逐条吻合） |
| `SetDraggable(T)` | `dragging = true; draggable.Initialize(content)` | **VA 0x1818153A0**（`all_methods.txt` 25252768） |
| `OnBeginDrag` | `if(!dragging) return; draggable.gameObject.SetActive(true); SoundManager.Instance.Play2D(dragSound)` | **VA 0x181814690** |
| `OnDrag` | `RectTransformUtility.ScreenPointToLocalPointInRectangle(draggable.transform.parent, e.position, e.pressEventCamera, out l); draggable.transform.localPosition = l`（用 `position` 不是 `delta`） | **VA 0x181814790**（常量：`0x144/0x148` = `position`、`get_pressEventCamera`） |
| `OnEndDrag` | `if(!dragging) return; dragging=false; SetActive(false); hovered.Where(≠null).SelectMany(g=>g.GetComponents<IDropHandler<T>>()).ToList().ForEach(d=>{ if(draggable&&CurrentItem!=null) d.Drop(CurrentItem); })` | **VA 0x181814E90** + `<OnEndDrag>b__6_2` **VA 0x1818155E0**（`b__6_0` = `Object.op_Implicit`、`b__6_1` = `GetComponents<T>`，两份 `.c` 都在） |
| `DropHandlersUnder(List<GameObject>)` | 那条 `SelectMany` | 同上 |
| `PlayDragSound()` | `SoundManager.Instance.Play2D` | 见下图：**cue 名 = `CardStartDrag`** |

**三处有意偏离**（都写进文件头）：① `dragSound` 存 **cue 名**（本工程声音的唯一载体，同 `Battle/AnimFXController.cs:80-86` 口径）②「跟指针」走世界坐标（本工程没有 RectTransform）③ 多一个 `Bind(...)`（界面是代码建的，原版靠 Inspector）。

## ② 两个实例：挂在哪 / 参数

| | 节点（原版 RectTransform / 绝对矩形） | 预览 | 判据 |
|---|---|---|---|
| 卡背 | `Content Area > Cosmetic Display > Cosmetic Drag Controller`：GO `−4418684799642833116` · RT `−3820370437395452124` = **100×100 · [993.59,525.50]–[1093.59,625.50]** | 子节点 `Collection Cosmetic`：GO `−6967555497338671324` · RT `3595378309407108900` = **250×405 · `m_LocalScale` 0.6 · anchor(0,1) · pos(75,−121.5)** ⇒ 绝对 **[943.59,444.50]–[1193.59,849.50]** · **视觉框 150×243** · **出厂 `m_IsActive: false`** | `menu_rect.py … "Cosmetic Drag Controller" --depth 2` · `menu_rect.py … -1322417011089150172` · 逐字段读 RT JSON |
| 卡牌 | `Content Area > Card Drag Controller`（**是 `Card Display` 的兄弟**）：同矩形 | 子件 `Deck Selector Card Info button` **287.9 × 55.7** · 出厂 INACT | 同上 + `菜单全树.md:9493` |
| **落点栏** | `Sidebar/Deck Details`：GO `−1322417011089150172` · 绝对 **[0.25,360.97]–[335.56,1010.03]**（335.31×649.06）—— **原版那个 `IDropHandler<RawCardScript>+<CosmeticItem>` 的 `DeckEditingPanel`（MB `−2895006486255833308`）就挂在这一件上** | | 用字段集反查（`dump.cs:72263-72290` 的 `draggable/deckList/deckCosmeticDrawer/…` 与 MB JSON 逐条吻合） |
| **起拖音** | 两个实例**共用**一条 `AudioCue`（`m_FileID 6 / m_PathID −4308815958917459268`）⇒ 经 `数据/索引/anim_address_map.json` 的 `guid_to_asset` 反查：**名 = `CardStartDrag`** | | `bundle_soundcollection_assets_all/MonoBehaviour/CardStartDrag.json` |

⚠️ 卡行子件那条 `[414,555 288×56]` **落在节点外** ⇒ 是「布局跑之前的模板位」（铁律 10 第 3 条）⇒ 我们摆在节点中心，**这是我们挑的**（代码里标了）。

## ③ 接线链（含「拖 vs 滚动」那一跳）

原版：格子起拖 → relay → `CheckCosmeticDrag` → **① `IsScrollDragThreshold` 分「拖 vs 滚动」** → ② 取内容 →（`InventoryManager.HasItem` 判拥有）→ 通过 ⇒ `SetDraggable` + `SetTarget` → 松手 `b__6_2` ⇒ `DeckEditingPanel.Drop`。

我们（`DeckRuntime`，本仓没有 UGUI `EventSystem` ⇒ 指针层合成 `PointerEventData` 调**同三个方法**）：
`HandlePointer` 按下（`HandleCosmeticClick` 左键 / `HandlePoolClick` 左键）**只记起点** → 位移 > `ArmPxSlop`(=16.2px，与行拖出同一条 `0.15` 世界单位) → **`IsScrollDragThreshold(delta)`** → 过则 `BeginXxxDragAt`（`SetDraggable` + `OnBeginDrag`）→ 每帧 `OnDrag`（贴指针）→ 松手 `EndXxxDragAt`（关预览 + `hovered` → `Drop`）。

**「拖 vs 滚动」整条算出来了**（不是推测）：`SupportMethods.IsScrollDragThreshold(Vector2 dragInput)`
= **`Vector2.Angle(dragInput, Vector2.right) ∈ (60°, 120°)`** ⇒「竖向为主 = 滚动」。
逐条证据：`decomp_full/SupportMethods__IsScrollDragThreshold.c` 一条不差；
`read_literal.py` 直读 = `60` / `120` / `57.2958`(`Rad2Deg`) / `−1` / `1` / `1.0000000037e-15`；
那个 1e-15 = **`Vector2.kEpsilonNormalSqrt`**（`dump.cs:537620`）且正是 `Vector2.Angle` 内部那道守卫；
`DAT_1842d9aa8 + 0xb8 → static + 0x28` = **`Vector2.rightVector`**（`dump.cs:537615`：Vector2 的 statics 依次 zero/one/up/down/left/**right**@0x28）⇒ 直接用 `Vector2.Angle` 公共件，逐行等价。

**`hovered` 的等价物**（`CollectHovered`）：uGUI 填的是「射线命中那一件 **+ 整条祖先链**」（`BaseInputModule.HandlePointerExitAndEnter` · `…/InputModules/BaseInputModule.cs:291`，`m_SendPointerHoverToParent` 默认 true，同文件 `:45`）⇒ 等价于「落点在 `Deck Details` 那一栏的矩形里」⇒ 落在别处**空表**（原版同：卡池那边松手不装备）。

⚠️ **与卡牌那条的不对称（实测，照原样实现）**：`CheckCardDrag` 里两次 `ExecuteEvents.Execute` + 写 `eventData.pointerDrag`；`CheckCosmeticDrag` **一次都没有**（卡背那条只有 `SetDraggable`+`SetTarget`）。

## ④ 改动清单

1. **新建** `Core/DraggableController.cs`（323 行）+ `.meta`（guid `4c63349aeeed422a9d642323407e28b9`，全库唯一，逐字同形于同目录既有 `.meta`）。
2. `Deck/DeckRuntime.cs`（+~600 行）：
   - 类声明 → `MonoBehaviour, IDropHandler<string>, IDropHandler<CardDef>`（= 原版 `DeckEditingPanel` 那两个接口的等价物）· 加 `using UnityEngine.EventSystems;`
   - 新段「拖拽」：常量 + `BuildCosmeticDrag` / `BuildCardDrag` + `IsScrollDragThreshold` + `DragEvent` / `PxToScreen` / `CollectHovered` + `Begin/EndXxxDragAt` + 两个 `Drop` + 一批 `Ui*` 自检口
   - `HandlePointer`：两条「拖拽中」分支 + 一支「按下了等位移」（**排在重建/派发之前**）
   - `HandleCosmeticClick`：左键**记起点**（右键仍照原版按下就装备）· `HandlePoolClick`：左键**改成抬起才弹放大窗**（uGUI click 本来就是抬起；这样「按下→拖出=加牌」才落得地）
   - 🔴 **顺手修一处真缺陷**：卡背格「铺格筛了阵营、命中没筛」⇒ **开着 `Army Filter` 时右键装备的是另一张**。判据收成一份 `CosmeticList()`，铺格 / 命中 / `UiCosmoShownCount` / `MaxCosmoScrollPx` 四处共用（`CosmeticNameAt` 里有留痕）。
3. `Editor/DeckScene.cs`（+159 行，**只加**）：`TestCosmeticDrag()` + `Run()` 里一句 `Section`/调用。断言 **60 条**：
   - **原版参数**：节点 100×100@[993.59,525.50] · 预览布局框 250×405@[943.59,444.50] · **预览画出来 150×243**（`UnionQuadsPx` 量渲染矩形）· 落点栏 [0.25,360.97] 335.31×649.06 · cue 名 `CardStartDrag` · 拖影卡行画出来 **287.9×55.7**
   - **出厂态**：预览 / 拖影**都关着**（原版 `m_IsActive: false`）
   - **阈值四条边界** 59°/61°/119°/121° + 纯水平 + 零位移（换一组常量必红）
   - **判别式**：落在栏里⇒装备 / 落在栏外⇒**不**装备；**竖向位移⇒根本不起拖**（一条「不看方向总起拖」或「不看落点松手就装备」的实现必红一条）
   - **跟指针**：预览中心 = 指针点（±0.5px）
   - **④ 右键装备那条路仍然好**（原版主力路，先断它再断拖拽）
   - **卡牌那一支**：起拖⇒拖影显形 + 量 287.9×55.7 · 落栏里⇒`Drop`⇒**卡组正好 +1**（夹具先按 `CanAdd` 筛一张）· 落栏外⇒不投
4. ⛔ 没碰：`Core/CardView.cs` · `Shell/**` · `RuleEngine/**` · `项目任务.md` · `CLAUDE.md`。

## ⑤ 那 5 条「没查清」：补上了哪几条

| # | 原结论 | 本轮 |
|---|---|---|
| 1 | `DraggableController<T>` 四个方法体没产物 ⇒「预览怎么跟指针 / 拖拽在哪一刻真正起」没查清 | ✅ **全补**：VA 反汇编读出四个方法体 + `b__6_2`（`SetDraggable` 就 `dragging=true`+`Initialize`；**起拖那一刻** = `OnBeginDrag` 里 `SetActive(true)`+`Play2D`；跟指针 = `OnDrag` 用 `position`+`pressEventCamera`；松手 = `OnEndDrag` 里 `SetActive(false)`+`hovered→Drop`） |
| 2 | `CheckCosmeticDrag` 是否就是 `Start` 里注册的那个回调 = 推断 | ⚠️ **仍是推断**（token 依旧没解）。**但不再挡路**：能力上两条候选都只在「起拖」那一拍起作用，我们按等价物接线并把这条如实留在原处 |
| 3 | `CollectionItem<T>.OnBeginDrag/…` 没产物 ⇒ 格子那端怎么把事件送进 relay | ⚠️ **没补**。本轮把「relay 那一跳」整体**替换**成指针层直调（本仓没有 UGUI，relay 无对应物）⇒ 它不再是我们的缺口，但**原版那条机制仍没查清** |
| 4 | 为什么 `SetTarget` 要改指到 `Cosmetic Drag Controller` | ⚠️ **没补**（同上，被替换） |
| 5 | `DeckEditingPanel__Drop.c` 归属有重载撞名风险 | ✅ **补上**（旁证变直证）：该 MB 的**字段集**与 `dump.cs:72263-72290` 的 `DeckEditingPanel` 逐条吻合（`draggable/deckList/deckCosmeticDrawer/deckCounterDrawer/deckCostDrawer/deckCounter/scroll/deckName/eventRelay/emptyWarning` 全中、全库唯一命中）；且它的 RT 就是 `Sidebar/Deck Details [0.25,360.97]–[335.56,1010.03]` ⇒ `Drop(CosmeticItem)` 那条链的落点**坐实** |

**本轮新解出来的**（原来没在清单里）：① 落点 = `Deck Details` 那一栏（含 `hovered` 含祖先链这条机制）② 起拖音 cue 名 = **`CardStartDrag`** ③ `IsScrollDragThreshold` **整条**（60/120/`Vector2.right`/1e-15）。

**还差 / 仍是我们的选择**：卡行起手位置（原版是模板位，量不到）· 预览渲染队列 `QDragPreview = 3050`（我们挑的一档，原版靠 Canvas 层级）· `dragSound` 取不到时**不出声**（好在该 cue 也**不在**本工程的声音库里 ⇒ 首次起拖会打一条一次性告警，不是静默）。

## ⑥ 类型检查（原样贴）

```
$ TMPDIR=/tmp/wf_g3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（上述是本轮**最后一次**运行 = 收工时那一拍，两行都是 **0**。）

⚠️ **中途撞过一次别人的半成品**，如实记：同一命令在那之前跑出过
```
--- 编辑器程序集 ---
Assets\CardPresentation\Editor\NetBattleTest.cs(79,13): error CS0103: 当前上下文中不存在名称「_clk」
Assets\CardPresentation\Editor\NetBattleTest.cs(80,39): error CS0103: 当前上下文中不存在名称「FakeClock」
编辑器错误数: 2
```
更早还撞过一次 `Net/NetBattle.cs`（`StartReconnectCountdown` / `ClockPaused` / `ReconnectHint` / `_countdownLeft` / `ReconnectPopup` 五条）。
两次报错**全部集中在联机线的文件上、我负责的三个文件一条都没有** ⇒ 按简报那句「报错全在不是你负责的文件上 ⇒ 那不是你的问题」，**我一份都没碰**（那条线的写手改完就自己清了 —— 最后一次运行两行 0 就是证据）。
