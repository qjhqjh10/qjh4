# 查证 · `Cosmetic Drag Controller`（卡背拖拽预览）—— 是什么 / 在哪用 / 我们要做什么

> 执行：**只读调查代理**（2026-10-17）· 主对话代落盘。**全部现读**：反编译 `d:/2/tools/decomp_full/` + 签名桩 + 解包资源 `d:/2/新解包资源/assets_full/`。
> 触发：用户 2026-10-17 点名「检查『卡组编辑』里的『美容品』部分，这里可以更换卡背……你说的这个 `Cosmetic Drag Controller` 是什么，在哪里使用」。
> 账位：挂在 `项目任务.md` §三 **第 2 条**（不是 A 表新账）。上一轮把它记成「**有意不建**」，出处是 `资料/普查产出_1016/可玩性_卡组编辑.md:49` —— **那是我们自己写的判断，不是用户拍板** ⇒ 按**铁律 11**：既非「原版本身没有」、也非「用户明确不做」⇒ **要做**，只有先后。

---

## 一、结论（五问）

1. **它是什么组件**：**不叫 `Cosmetic Drag Controller`** —— 真类名是 **`CosmeticDraggingController : DraggableController<CosmeticItem>`，而且是个空类**（无自有成员）。「Cosmetic Drag Controller」只是**它挂的那个 GameObject 的名字**。
   同款成对存在的还有 **`CardDraggingController : DraggableController<RawCardScript>`**（节点名 `Card Drag Controller`）⇒ **它俩是同一套通用拖拽系统的两个实例**，卡背不是它的专属。
   出处：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CosmeticDraggingController.cs:1-3` · `decomp_full/CosmeticDraggingController__.ctor.c`（该类**唯一**落成的方法体，只有 ctor）。
2. **挂在哪**：`Deck Editing Menu > Content Area > Cosmetic Display > Cosmetic Drag Controller`
   - GameObject PathID `-4418684799642833116`；RectTransform `-3820370437395452124` = **100×100**，pivot/anchor 居中，屏位 **[994, 526]**
   - 其 MonoBehaviour `-4326558023866323164`，`m_Script` = MonoScript `5187419560979717525` = `CosmeticDraggingController`；序列化 `draggable` = `6500071757747449636`、`dragSound` = `-4308815958917459268`
   - **子节点 = 预览本体 `Collection Cosmetic`**（GO `-6967555497338671324`，**出厂 `m_IsActive: false`**），RectTransform `3595378309407108900` = **scale 0.6 / 250×405**
   - `DeckEditingWindow.cosmeticDrag` 就指向这个组件
3. **做什么**：**拖拽预览宿主** —— 默认预览子节点关着；**起拖时显示成卡背图、跟随指针**；**松手时把内容投给指针下的 `IDropHandler<T>`**。
   基类 `DraggableController<T> : MonoBehaviour, IBeginDragHandler/IDragHandler/IEndDragHandler`，字段 `draggable`(Draggable\<T\>) + `dragSound`(AudioCue) + `dragging`；松手投递由 `<OnEndDrag>b__6_2(IDropHandler<T> droppable)` 完成。
   预览图 = `CosmeticPreview.Initialize(item)`：`CurrentItem = item; cardbackImage.sprite = item.CosmeticSprite`。
4. **原版换卡背有两条路，终点是同一个 `Drop(CosmeticItem)`**：
   - **① 右键点选（主力路）**：`CollectionCosmetic` 格子原型 → `CollectionDisplay<CosmeticItemCardback>.OnItemSelected`（字段 `Action<CollectionItem<T>, InputButton>`）→ `DeckEditingWindow.OnCosmeticClick`：**`button == 1`（右键）才** `DeckEditingPanel.Drop(item)`；**左键什么都不做**。→ `Drop(CosmeticItem)`：`deck.cardbackId(+0x28) = item` · `deck.syncedToServer(+0x60) = false` · `UpdateDrawers()`。
   - **② 拖拽（`CosmeticDraggingController` 负责的那条）**：在 `Cosmetic Display` 格子上起拖 → `DeckEditingWindow.Start` 注册的 relay 监听被调 → `CheckCosmeticDrag(eventData, sourceGO)`：先 `IsScrollDragThreshold` 分「拖 vs 滚动」→ 取 source 上的 `CollectionItem<CosmeticItemCardback>` → `InventoryManager.HasItem` 判**拥有** → 通过则 `cosmeticDrag.SetDraggable(item)` + `cosmeticDisplay.eventRelay.SetTarget(cosmeticDrag.gameObject)`（不通过＝滚动拖 / 未拥有 ⇒ `ResetRelayTarget`）→ 松手 ⇒ `b__6_2(IDropHandler<T>)` ⇒ 上面那个 `Drop`。
   - **侧栏回显**：`DeckCosmeticDrawer.Initialize(deck)`；deck 无卡背时**先按阵营补默认**（`DefaultCarbackByArmySO.GetCardbackCosmeticItem`）再 `CardDeck.GetDeckCardback` → `Image.sprite`。
   - **切页**：`ToggleDeckCosmetic(true)` → `ToggleDisplay(false)`（关 cardDisplay / 开 cosmeticDisplay / 关 wildcards）+ `ToggleCosmetics()`（关 deckList(0x28)/counter(0x38)/cost(0x40) · 开 `DeckCosmeticDrawer`(0x30)）。
   - ⚠️ **与卡牌那条的不对称（实测）**：`CheckCardDrag` 里有两次 `ExecuteEvents.Execute` + 写 `eventData.pointerDrag`，**`CheckCosmeticDrag` 里一次都没有** ⇒ 卡背那条的「起拖」只靠 `SetDraggable` + `SetTarget`。
5. **我们的等价物**：**点击那条有，拖拽预览那条无**。
   - ✅ **有**：`Deck/DeckRuntime.cs` 的 `HandleCosmeticClick`（`if (right) EquipCardback(...)`，左键吃掉不做）· `EquipCardback` → `State.Deck.CardbackId` · 侧栏 `RefreshCosmeticDrawer` · 自检入口。
   - ❌ **无**：`Deck/DeckRuntime.cs` 明写「`Cosmetic Drag Controller`（拖拽预览，100×100 + `scl 0.6` 的 250×405 预览）⇒ **先不建**」；`Assets/CardPresentation` 全仓 `grep -rn "class .*Drag"` = **0 命中**（`StartRowDrag` / `EndDrag` 是卡组列表**行拖出删除**，与卡背无关）。
   - ⚠️ 盘点表引的 `DeckRuntime.cs:1325` **已漂到 `:1345`**。

## 二、证据（可复查坐标）

- **组件 / 基类**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{CosmeticDraggingController,DraggableController,Draggable,CosmeticPreview}.cs` · `dump.cs:73708 / 73800 / 99518 / 106124`
- **反编译方法体**：`d:/2/tools/decomp_full/` 的 `DeckEditingWindow__CheckCosmeticDrag.c` · `__OnCosmeticClick.c` · `__Start.c` · `__ToggleDisplay.c` · `DeckEditingPanel__Drop.c` · `__ToggleCosmetics.c` · `DeckEditingStateController__ToggleDeckCosmetic.c` · `DeckCosmeticDrawer__Initialize.c` · `CosmeticPreview__Initialize.c` · `EventRelay__{SetTarget,Raise_object_,RegisterHandlers_object_,RegisterListener_object_,GetHandlers}.c`
- **资源（bundle `menus_assets_all`）**：`GameObject/Cosmetic Drag Controller.json` · `GameObject/Collection Cosmetic.json` · `GameObject/Content Area_7981424114586545956.json` · `RectTransform/RectTransform_{-3820370437395452124, 3595378309407108900, 675656078588768036, -563818439605686492}.json` · `MonoBehaviour/MonoBehaviour_{-4326558023866323164, 6500071757747449636, 590874453470605092, 5918047927837519652, -2895006486255833308, -367558861613830364, …}.json`
- **布局旁证**：`d:/2/Warpforge_tools/data/ui_layout/菜单全树.md:9493`（`Cosmetic Drag Controller [994,526 100x100] script` + 子 `Collection Cosmetic (inactive) 250x405`）
- **旧口径（承前）**：`资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md:95,134` · `资料/说明书/04_界面UI/菜单全树.md:9503` · `资料/说明书/05_游戏数据/脚本定义.md:221`
- **搜索覆盖**（报「没有」前的记录）：词 `Cosmetic Dragging` / `CosmeticDragging` / `CosmeticPreview` / `Drag Controller` / `DraggableController` / `SetDraggable` / `OnCosmeticClick` / `IsScrollDragThreshold`；范围 `d:/2/tools/decomp_full`（目录 `ls|grep` + 内容 `grep -rl`）· `d:/2/tools/all_methods.txt` · `d:/2/tools/il2cpp_out/dump.cs` · `d:/2/Warpforge_code/Scripts/Assembly-CSharp` · `d:/2/新解包资源/assets_full/**`（含全体 bundle 的 `GameObject/`）· `d:/2/Warpforge_tools/data/{ui_layout,ui_scene}` · `d:/4/Unity/资料/**` · `d:/4/Unity/MyGame/Assets/CardPresentation`

## 三、没查清的部分（如实 —— ⛔ 别拿这些当结论用）

1. **`DraggableController<T>` 的四个方法体没有反编译**：`all_methods.txt` 有地址（`SetDraggable` = 25252768 · `OnBeginDrag` = 25249424 · `OnDrag` = 25249680 · `OnEndDrag` = 25251472 · `<OnEndDrag>b__6_2` = 25253344），但 `decomp_full` 里**没有对应 `.c`**（`ls | grep -iE "^DraggableController"` 只有 `.__c__.` 与 `__ctor`）。⇒ **「预览怎么跟指针走 / 拖拽在哪一刻真正起」没查清**，目前只能靠 `CosmeticPreview.Initialize` + 子节点 100×100 / 0.6 / 250×405 反推。
   ⚠️ 这几段**可以用 VA 反汇编补**（先例：`资料/战斗资源_覆盖清单_0929.md` §三 的 8a/8b/8d，以及 il2cpp 虚调用扫描那两把钥匙）。
2. **`CheckCosmeticDrag` 是否就是 `Start` 里注册的那个回调 = 推断、不是直证**：依据是 `DeckEditingWindow` 里只有 `CheckCardDrag`/`CheckCosmeticDrag` 两个私有 `(PointerEventData, GameObject)` 方法、且两处 `RegisterListener` 的方法 token 相邻（`DAT_1842d17c0` / `DAT_1842d18c0`）；token 是 `MethodInfo*` 数据指针，**没解出它指向谁**。
3. **`CollectionItem<T>.OnBeginDrag / OnDrag / OnEndDrag` 方法体没反编译**（泛型，`decomp_full` 无 `CollectionItem*` 文件）⇒ **格子那端怎么把事件送进 `display.EventRelay` 没查清**；`EventRelay` 只读到 `SetTarget / Raise / RegisterHandlers / RegisterListener / GetHandlers` 骨架，`eventTarget` 与 `eventDictionary` / `handlerDictionary` 两个方向的具体语义**未证实**。
4. **`EventRelay` 到底在哪**：`CardbackCollectionDisplay.eventRelay` = PathID `-367558861613830364`，其 `m_GameObject` = `565187784913121060` = **`Cosmetic Display` 自己**、`eventTarget: 0`（Awake 时自填）—— 已核；但**「为什么 `SetTarget` 要改指到 `Cosmetic Drag Controller`」的机制没查清**。
5. `DeckEditingPanel__Drop.c` 的**归属有重载撞名风险**：`all_methods.txt` 里 `DeckEditingPanel$$Drop` 有三个地址（7213472 / 7213328 / 7213616）；该文件方法体写的是 `cardbackId(+0x28)` ⇒ 内容上只能是 `Drop(CosmeticItem)`（Slot 5），但**文件名不足以单独证明**。

## 四、要做什么（施工草图 —— 判据齐了才动手，⛔ 不许自己发明口径）

1. **通用拖拽件**：`DraggableController<T> : MonoBehaviour, IBeginDragHandler / IDragHandler / IEndDragHandler`（字段 `draggable` / `dragSound` / `dragging`；松手投 `IDropHandler<T>`）
2. **预览件**：子节点出厂 **inactive**；起拖 `SetActive(true)` 并贴 `CosmeticSprite`；跟指针；松手关回去
3. **两个实例**：卡背（`CosmeticItem`）· 卡牌（`RawCardScript`）—— 卡牌那条**我们有没有**要先核（别默认没有）
4. **接线**：`Cosmetic Display` 格子上起拖 → 阈值分流（拖 vs 滚动）→ `HasItem` 判拥有 → `SetDraggable` + relay 改指 → 松手 `Drop`
5. **不要丢**：**右键点选那条路我们已经有了**，⛔ 别在实现拖拽时把它改坏

> ⚠️ **3 与 5 是先要现核的两件事**，别照抄本条当施工单。
