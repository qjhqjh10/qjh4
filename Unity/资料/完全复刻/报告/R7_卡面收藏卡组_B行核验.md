# R7 · 卡面 / 收藏 / 卡组编辑 —— B 行核验（2026-10-27 · 第八十七会话）

> 只读代理 **R7**。范围 = 三份账里 **状态 = B** 的 **32 行**（`卡牌面_渲染.tsv` 8 · `收藏.tsv` 16 · `卡组编辑.tsv` 8）。
> 补丁 → `账/_parts/B87_R7_卡面收藏卡组核验.tsv`（**32 行、11 列、表头与主表逐字相同、LF、0 裸 CR**）。
> ⛔ 未合并主表、未改任何 `.cs`、未跑 Unity。合并命令由调度台跑：`merge_ledger_parts.py --only B87_R7_`。
> 自检：补丁里每个 `(原版类, 方法)` 在**全部**主表中**恰好命中 1 行**（跑过脚本核过，无重键、无新行）。

---

## ① 统计（32 行全覆盖）

| 域 | 行数 | **A** | **B** | **C** | **D** |
|---|---|---|---|---|---|
| 卡牌面/渲染 | 8 | **5** | 3 | 0 | 0 |
| 收藏 | 16 | **5** | 5 | **1** | 5 |
| 卡组编辑 | 8 | **1** | 2 | 0 | 5 |
| **合计** | **32** | **11** | **10** | **1** | **10** |

⇒ **B → A 11 条（34%）· B → D 10 条（31%）· 新判 C 1 条 · 仍 B 10 条**。B 的存量从 32 降到 10。

### 逐条去向

**卡牌面/渲染（8）**

| 行 | 方法 | 现 | 一句话理由 |
|---|---|---|---|
| 27 | `SetColliderNotInHand` | **B** | 进墓地那一半已接并对上（`CardView.cs:9370` / `BattleDriver.cs:25240`）；`removedFromGame(11)` 那一半**我们引擎没有这个状态**（唯一置位点在未建的手牌链上）⇒ 整方法不能判 A |
| 124 | `OnDeselect` | **B** | 选择态信道存在（`PointerLayer.Select` → `WindowButton.SetSelected` → `HideTip`），但只服务**按钮** tooltip 族；卡面文字这一族无选择态入口 |
| 126 | `OnDisable` | **A** | 共用谓词扫描：命中不到 ⇒ `Tooltip.Hide()`；节点/组件不活 ⇒ 扫描早退 |
| 127 | `OnEnable` | **A** | 原版那个 `isHovering` 布尔我们没有 ⇒ 无态可复位；每帧重算 |
| 129 | `OnPointerEnter` | **A** | `TickTraitTip`/`TickCardTip`/`HitTip` 命中即 Show（链接碰撞走 TMP 的 `FindIntersectingLink`） |
| 130 | `OnPointerExit` | **A** | 扫描全未命中 ⇒ Hide（等点目标「钉住」那一档原版也不收） |
| 131 | `OnSelect` | **B** | 同 124 |
| 134 | `Toggle(bool)` | **A** | 逐卡 `CardView.TooltipsEnabled` 消费；`text.raycastTarget` 那一半在我们这儿无对象（命中靠几何） |

> 7 个回调判 A 的合法性 = 方案 **§4.1 的共享落点例外**（同一段代码确实分别满足多个原版方法）：落点给的是**行号范围**（`BattleDriver.cs:32350-32459` + `Tooltip.cs:392-397`），且每条的对应关系写在补丁备注里。

**收藏（16）**

| 行 | 方法 | 现 | 一句话理由 |
|---|---|---|---|
| 32 | `OnCollectionUpdate` | **A** | 信号层今天不响（详见 ④-12），但**唯一「加卡」生产者**必经理 Background→ToFocus 重灌 ⇒ 可见产出相同 |
| 47 | `ToFocus` | **A** | 全链现读：`GameWindow.cs:540` → `GameWindowWithTabs.cs:220` → `CollectionTabPage.cs:86` → `CollectionWindow.cs:6360` |
| 105 | `CollectionCard.OnDestroy` | **B** | 🔴 前提过期：我们**已池化**（`_cellPool`）；还差「驱逐/复用要不要 Kill 那条补间」 |
| 108 | `CollectionCard.Start` | **B** | 同上：复用一格时高亮/选中态要不要复位 |
| 142 | `b__9_0`（Toggle 闭包） | **A** | 闭包体 = 收起动画 OnComplete = `SetActive(false)`，落点 `DeckRuntime.cs:2731-2734` |
| 204 | `Release(PlayModes?)` | **A** | `CardArt.ReleaseModeIcon(int?)` 分支结构逐条同；接线在 `MenuDraw.cs:3635` |
| 205 | `ReleaseAll()` | **D** | 原版**全树 0 调用点**（自证见补丁备注）⇒ 死代码 |
| 207 | `TryRelease(...)` | **A** | 与 `ReleaseModeIcon(mode)` 1:1（另订正：它属于内嵌类 `GameModeIcon`） |
| 265 | `ItemDrawer.Draw(…amount…)` | **B** | **判据从「缺」翻案为「可读」**（155 行 / 0 NoteDecompilerIssue）；还差 2 处（"x" 前缀、两个复位槽） |
| 266 | `Draw(…quantity…)` | **D** | 抽象声明无体（§4.1 不单列，并入 265） |
| 267 | `Draw(T,int)` | **D** | 泛型抽象声明无体（行为在 23 个子类，见各抽屉条目） |
| 269 | `GetItemName(T)` | **B** | 基类体 = `item.LocalizedName`（可读）；待核 1 处 = 野牌格名字文本 |
| 272 | `ItemDrawer.OnDestroy` | **B** | 静态逐条已对上（Leash）；只差一次运行时读数（`HandleCount` −1） |
| 297 | `ToggleShowInfoOnClick` | **D** | abstract + 唯一实现是空体；行为由序列化字段承载且已接（`ShowInfoOnClickOf`） |
| 342 | `WhereToGetCardsSection.OnDestroy` | **D** | 🔴 判据要点**写错了**（不是 Release，是 `RemoveAllListeners`）；我们没有监听表 |
| 343 | `WhereToGetCardsSection.Release` | **C** | 关窗不放逐项美术（详见 ②） |

**卡组编辑（8）**

| 行 | 方法 | 现 | 一句话理由 |
|---|---|---|---|
| 52 | `SetPlayerData` | **B** | 7 格已灌；其余是 **P3**（`PlayerBattleData` 随匹配由服务端下发） |
| 54 | `SetupSerializedData` | **B** | ③④ 已接；①②⑤ 的 DTO 反序列化在 `RuleEngine/Core/DeckRules.cs`（别的包） |
| 183 | `ConfigPrefab` | **D** | 原版只把**行模板** `SetActive(false)`；我们无模板 prefab |
| 230 | `DeckSelectorMenuItemDemo.OnDestroy` | **D** | 原版只 `RemoveListener`；我们的格整棵销毁、点击是闭包 |
| 255 | `SetTutorialIndex` | **D** | 全树 0 调用点；离线用 `StageRow.Idx` 常量表 |
| 275 | `GetCardLibrary` | **A** | 原版「倒序循环 + `Reverse()`」= **原序**，我们生成期就是原序（详见 ④-11） |
| 279 | `IsIncompleteLibrary` | **D** | 0 调用点；等价事实 = `Deck.complete/missing` |
| 281 | `SetHero` | **D** | 0 调用点；督军在生成期由 `heroId` 定 |

---

## ② 真缺口（C）逐条

**C-1（新判）`WhereToGetCardsSection.Release()` —— 关窗不放那一窗的逐项美术**

- 原版逐句：窗 `Close()` → 每个来源段 `Release()` → `foreach (CardDrawer d in currentCards(+0x48)) d.Release()`（`WhereToGetCardsSection__Release.c` 现读 + ilspy `:192-208`）。
- 我们：`Shell/WhereToGetCardsPopup.cs` **全文件无 `Release`/`OnDestroy`**；`GameWindow.Close()` 只 `SetActive(false)`（不销毁、不放），`MenuDraw.ClearChildren` 只在 `Open()`（`:291`）与内容重建（`:401`）调 ⇒ 关窗后那批图仍挂在 `CardArt._cache`。
- 处方（一行级，与同族几十扇窗同形）：本窗 `override void Close()`（或 `OnDestroy`）里调 `MenuDraw.ClearChildren(transform)` —— `MenuDraw.ReleaseArtUnder`（`Shell/MenuDraw.cs:3633-3635`）会放逐项美术。⛔ **不是**「建抽屉 + `foreach Release`」（这扇不建抽屉；账里旧挂的那条改动请求不该做）。
- ⚠️ 可见效果 = 0（纯资源回收）⇒ 优先级低，但按铁律 11 属「该做」。

> 本轮**没有**新增其它 C。下面 4 条是「疑似 C、但还差一步论证」的，一律留在 B 并写清了差什么（见 ③）：`GetItemName` 的野牌名字、`ItemDrawer.Draw` 的数量前缀、`CollectionCard` 的两条池化生命周期、`TextTooltipController` 的两个选择态回调。

---

## ③ 还差什么（仍 B 的 10 条，逐条给「回来条件」）

| 行 | 还差什么 | 回来条件 |
|---|---|---|
| 卡面 27 | `removedFromGame(11)` 那一半没有源状态 | 引擎建了 `RemoveFromGameCard` 链（或证明「手牌移除 = 视图整棵摘掉」已覆盖）⇒ 后者成立即可判 **A**（行为等价） |
| 卡面 124 / 131 | 原版**卡面文字**会不会被 EventSystem 选中 | 一次定论：会 ⇒ **C**（把 `ShowTip/HideTip` 同款接到 `CardView` 选中态）；不会 ⇒ **D** |
| 收藏 105 | 池化复用/驱逐时**要不要 Kill** 那条补间 | 读 `Core/CardTween.cs` 的句柄模型 + 一次「驱逐一张正在播补间的卡」的自检（**不跑 Unity 做不了最后一步**） |
| 收藏 108 | 复用一格时**高亮/选中态**要不要复位 | 同上；若收藏窗确认永不开高亮 ⇒ 可 **D**（原版这一跳的对象我们不存在） |
| 收藏 265 | ① 数量文本原版是 `amount.ToString()`（无前缀），我们 `"x"+qty`（`ItemDrawer.cs:1318`）② `+0x1d8/+0x1e8` 两个复位槽目标未坐实 | ① 读 Wildcard/Deck 抽屉 prefab 的数量节点默认文本（有没有独立 `x` 子件）；② VA 反汇编那两个调用点取目标 |
| 收藏 269 | 野牌格名字那格：原版是 `GetItemName` 的稀有度档名，我们画阵营名 | 读 Wildcard Drawer prefab 的名字节点 + 原版实拍一张野牌格 |
| 收藏 272 | 运行时读一次 `ItemDrawer.HandleCount`（销毁一格应 −1） | 一次真机或自检（**不跑 Unity 做不了**） |
| 卡组 52 | 服务器那 6 格无数据源 | **P3**：等远端/匹配载荷（`Net/NetPendingBattle*` 加字段要双端同版） |
| 卡组 54 | ①②⑤ 的 DTO 反序列化在 `RuleEngine/Core/DeckRules.cs` | 由引擎域那条账收口 |

**顺带口径提醒**：卡面 126/127/129/130/134（A）与收藏 32/47/142/204/207（A）并**没有**跑运行时观测 —— 全是静态逐链核。铁律 12 自检暂停期间这是本轮能做到的上限；若调度台要求 G3 观测，这几条要各补一次 Unity 读数。

---

## ④ 顺手发现（只报不改）

1. 🔴 **收藏窗早就池化了** —— `_cellPool`（`Shell/CollectionWindow.cs:1267`，上限 `CellPoolCap=48`，复用 `:1610-1625`，超限驱逐 `:1713-1721`），2026-10-17 `T09` 落地。账里凡是「我们不做 cell 池/复用，每格重建」的话（本轮 105/108 两条的判据要点都建立在它上面）**全部过期**。
2. **`CardArt.ReleaseModeIcon` 0 调用点** —— 真实释放走 `MenuDraw.cs:3635` 的 `IsModeIconArt + Release` 直放，绕开了它。要么让 3635 改调它，要么承认它是死 helper（⛔ 别两边都留）。
3. **落点行号成批漂移**（本轮已就地订正 6 处）：`BattleDriver.cs` 34237→**34672** / 34089→**34523** / 24811→**25240**；`CollectionWindow.cs` 2262→**1501**；`PracticeModePopup.cs` 2423→**2456** / 1922→**1945**。⇒ 主表其它域很可能同源漂移，建议同步点上跑一次 `check_ledger_points.py`（⚠️ 按 CLAUDE.md：只能在没有 `.cs` 写手、补丁合并完之后跑）。
4. 🔴 **`WhereToGetCardsSection.OnDestroy` 的判据要点原来是错的** —— 原写「`OnDestroy` → `Release()`（把抽屉句柄放掉）」，现读两源都是 **`toggle.onValueChanged.RemoveAllListeners()`**。逐抽屉释放是**另立一条 `Release()`**（已判 C）。
5. **又一处「文件名≠内容」** —— `d:/2/tools/decomp_full/DemoDeckInfoSO__SetTutorialIndex.c` 的内容是 `RoomInfo__set_PlayerCount`。账里把它当「🥇 判据」引过一次（本轮已改成 ilspy）。
6. **`TextTooltipController` 的三个 `.c` 体互相重复**：`__OnDisable.c` / `__OnPointerExit.c` / `__OnDeselect.c` 的体完全相同（= `OnDeselect` 那段）。⇒ 引用这三个文件前**先认体**（ICF 折叠，体可信、名字不可引用）—— 与「首行名不是自己」那条坑同族。
7. **ItemDrawer 泛型族的「判据缺」被推翻**：`decomp_generic_trial/out/ItemDrawer_object___Draw__1ae9cc0.c` = 155 行、**0** NoteDecompilerIssue（ILSpy 那份 168 处是噪声）。同目录里 **`CollectionFilterController<object>__Toggle`（62 行）** 也在 —— 本轮就是靠它把 `b__9_0` 那条从 B 判成 A 的。⇒ 账里凡是「泛型 ⇒ 判据缺」的行都该先翻这个目录。
8. **`ItemDrawer` 数量文本前缀疑点**：原版 `set_text(amount.ToString())`（**无前缀**），我们 `"x"+qty`。若原版 prefab 没有独立的 `x` 子件，那我们就多印一个字符（低风险但属「看着像对的」那一类）。
9. **`SetDrawerInteractive` 的「滑动期间点不到」确证是自设计**：`CollectionFilterController<object>__Toggle` 全篇没有 raycast / blocksRaycasts 调用（原版只 `SetActive`）。代码里已如实标注（`DeckRuntime.cs:2746-2754`），现在可以补一句判据把它钉死（⛔ 别当原版行为）。
10. **「我们的 UI 没有 EventSystem 选择路径」这个说法不准确** —— 壳里**有**选择态：`PointerLayer.Select`（`:799-815`，注释明写 = `EventSystem.SetSelectedGameObject`）→ `WindowButton.SetSelected`（`:143-149`）→ `ShowTip/HideTip`（`:622/:637`）。只有**卡面文字**这一族没接。⇒ 别的账里若有人用「我们不吃选择态」当判 D 的理由，要按这条重核。
11. **`PrebuiltDeck.GetCardLibrary` 那个 `Reverse()` 是陷阱**：原版是「`i` 从末位倒序遍历 + 收尾 `Reverse()`」，两个反序抵消 = **原序**；我们保持原序**正是等价**。⚠️ 别与 `RuleCore.Draw` 取列表末尾那条坑（CLAUDE.md 那条 `DrawCardFromDeck` 取 `[0]`）串在一起判 —— 一个说牌序、一个说抽牌口，混起来会得出「我们整副牌反了」的假结论。
12. **卡牌收藏的「信号层」形同虚设** —— `CardInventory.CollectionUpdated`（`Shell/Inventory.cs:1344`）**0 订阅者**，且 `CardInventory.UpdateItemCollection()`（唯一 Raise 在 `:1357`）**0 生产调用点**（只有 `Inventories.Wildcards.UpdateItemCollection()` 在被调）。今天不出问题是因为「窗在前台时集合不会变」；一旦出现前台加卡（任务发卡 / 奖励窗同宿主），收藏窗会停在旧数据上而且**静默**。
13. **tooltip 那几条账里反复交叉引用的「同族真差异 = C 行 L120（tooltip 锚在关键词上、不跟鼠标）」已经不存在** —— `BattleDriver.cs:32412-32434` 2026-10-25 已改成 `ShowAtMouse`（判据 = 原版 `GetTooltipPosition` 的 `+10px`）。⇒ 那 7 条记录里的交叉引用是过期指引（本轮已在新备注里删掉，别再去追 L120）。
