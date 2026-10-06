# WD1b · 编辑器编辑副本（A397）

> 执行代理报告（本批第 4 轮「清空 A 表」）· 2026-10-13 · **只读判据 + 动手改代码**
> 白名单：`Deck/DeckEditorState.cs` · `Deck/DeckRuntime.cs`（仅 `State.Deck` 语义相关）· `Editor/DeckScene.cs`（仅受影响的断言/夹具）
> ⛔ 没跑 Unity（本工程同一时刻只许一个实例、且本件禁跑）· ⛔ 没动 git · ⛔ 没改正本

---

## 一、结论

**A397 = 真差异，已按原版改掉。**

原版编辑的是**副本**（`DeckEditingWindow__TryOpen.c:41-45`：`new CardDeck` + 拷贝构造 → 存进窗的 `+0x118`），
真写回库里那份只在 `DeckEditingWindow__UploadDeck.c`（`CardDeck__CopyDeck(库那份 ← 编辑中那份)`）。
我们原来 `DeckEditorState.LoadDeck` 是**直接赋引用** ⇒ `State.Deck` **就是** `DeckLibrary.Current` 那个对象
⇒ 任何**库级 `Save()`**（`Create` / `Delete` / `Add` / `Duplicate` / `Rename` —— 它们序列化**整份库**）
会把编辑器里**还没 Done 的改动顺手写盘**（D1 §五·2 那笔账）。

**改法（核心 1 行）**：`LoadDeck` 装 `deck.Clone()` 的副本（+ 名字照抄一行，见 §二 的 ⚠️）。
连带：
- **4 处注释就地订正**（旧注释写着「`State.Deck` 与 `Library.Current` 是同一个对象」—— 从本件起**不成立**，铁律 5）；
- **1 个新断言段**（`Editor/DeckScene.cs:3168-3265`，4 条断言 + 1 条「提交后不共享卡表」守卫），宿主 = `DeckScene.Run`；
- 写回口**一行没改**（`CommitCurrent` 本来就是「整份拷进去」）—— 它现在才名副其实。

**引用面核实**：`State.Deck`（含 `state.Deck` / `s.Deck` / 3 处 `live` 别名 / `DeckEditorState` 内部 `Deck.…`）
实测 **102 处**：`Deck/DeckEditorState.cs` 内部 35 · `Deck/DeckRuntime.cs` 20（代码；另 4 处在注释里）·
`Editor/DeckScene.cs` 47（`_rt.State.Deck` 23〔其中 2 处在我加的注释里〕· `state.Deck` 15 · `s.Deck` 9）。
**逐处复核结论：没有一处需要改代码**；要改的只有 3 处注释的心智模型（已改）——逐处表见 §三、夹具前提见 §四。

**类型检查**：`TMPDIR=/tmp/wf_wd1b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（§八）。

⚠️ **如实记两条**：
① 本件**未跑** `DeckScene.Run`（禁跑 Unity）⇒ 上面那 4 条新断言**还没被执行过**，同步点由调度台跑；
② 改动落地的三个文件里**已经有别的代理的未提交改动**（`DeckRuntime.cs` 的 A502 ESC ③ 级 / A407 两处 `SetAutoFitBox`；
`DeckScene.cs` 的 A364/A396/A398/A399/A415/A502 那几段）—— 我的改动与它们**区域不相交**，
`git diff` 按 hunk 分得开（§二 末尾列了我这次新增的 hunk）。

---

## 二、改动清单

| # | 文件:行（现读） | 改前 | 改后 | 照的是原版哪条链 |
|---|---|---|---|---|
| 1 | `Deck/DeckEditorState.cs:136-176`（`LoadDeck` 方法体在 `:162-176`） | `Deck = deck ?? new PlayerDeck();`（**直接赋引用**） | `Deck = deck != null ? deck.Clone() : new PlayerDeck();` + `if (deck != null) Deck.Name = deck.Name;` | `DeckEditingWindow__TryOpen.c:41-45`：`new CardDeck(...)` → `CardDeck___ctor(lVar5, plVar8, 0)`（**拷贝构造**）→ `*(window+0x118) = lVar5` |
| 2 | `Deck/DeckEditorState.cs:136-161` | 无文档 | 新增方法头：判据（`__TryOpen.c:41-45` / `get_EditingDeck` 读 `+0x118` / `__UploadDeck.c` 的 `CopyDeck`）+ 改向记录 + **两条「调用方要知道的事」** | 同上 |
| 3 | `Deck/DeckRuntime.cs:473-475`（`Build`） | 无注释 | 标注「`LoadDeck` 装的是副本 ⇒ 编辑器改的不是 `Library.Current`；库里那份只在 `CommitDeck()` 那一拍被整份拷回」 | `__TryOpen` → `__UploadDeck` |
| 4 | `Deck/DeckRuntime.cs:2486-2487`（`TryImport`） | 只有「`Library.Add` 自己会落盘」 | 补一句：这里的 `Add` 的 `Save()` 写在 `LoadDeck` **之前** ⇒ 不存在「顺手把未 Done 的改动写盘」 | 同上 |
| 5 | `Deck/DeckRuntime.cs:2319-2334`（`DiscardChangesAndLeave` 方法头） | 🔴 **假话**：「真正会被留下的是内存那份 —— 而 `State.Deck` 与 `Library.Current` 是**同一个对象**……所以本方法不做内存回滚」 | 就地订正（铁律 5）：库里那份（内存+盘）**从头到尾没被动过**（与原版等价）；**剩下的差别只有一处**（原版 Discard = 关窗销毁副本，我们在批处理/不切场景的路径上副本仍在内存）⇒ 仍不做内存回滚，**理由与受影响夹具写全**，并指到本报告 §六 | `.<ConfirmDiscard>b__45_1` = `HidePopUp()` + 虚槽 `0x1b8`（`GameWindow.Close`） |
| 6 | `Deck/DeckRuntime.cs:4090-4097`（`CommitDeck` 方法头） | 只写了 A363 的改向 | 补：这里 = 原版**唯一写回点**（`__UploadDeck.c` 的 `CopyDeck(库那份 ← 编辑中那份)`，库那份 = 窗的 `+0x40`）；A397 起两边**真的是两个对象**；⛔ 别在突变点调它 | `__UploadDeck.c` |
| 7 | `Editor/DeckScene.cs:2711` | `var live = _rt.State.Deck;   // = `Library.Current` 那个对象` | `// 🔴 A397 起 = 编辑器那份**副本**（不再是 `Library.Current` 那个对象）` | 同上 |
| 8 | `Editor/DeckScene.cs:2822-2825`（A364 段） | 无 | 补 3 行：`live` = 副本；本节只改它；「盘上没动」靠的是**没人调 `Save()`**、不是靠同一个对象 | 同上 |
| 9 | `Editor/DeckScene.cs:3096-3098`（A363 五突变段） | 无 | 同上（措辞同上） | 同上 |
| 10 | `Editor/DeckScene.cs:3168-3265` | — | **新增 A397 断言段**（见 §五） | `__TryOpen.c:41-45` + `__UploadDeck.c` |

**⚠️ 第 1 条里那一行 `Deck.Name = deck.Name;` 为什么必须有**：`PlayerDeck.Clone()` 走的是
`PlayerDeck(string,string,string,IEnumerable<string>,int)` 构造器，而那个构造器**会把空/null 名字替成「新卡组」**
（`RuleEngine/Core/DeckRules.cs:335`）。照原版那个拷贝构造是**逐格照抄** ⇒ 这一行把名字还原。
少它 = 一副「名字被清空过」的卡组一进编辑器就**静默改名**（红线：不许静默）。**没改 `Clone()` 本身**
（那在 `RuleEngine/`、越了本件白名单）—— 见 §七·1。

**本次新增的 hunk（`git diff -U0` 可核；其余 hunk 是同期别的代理的未提交改动）**：
```
Deck/DeckEditorState.cs    @@ -133,9 +133,43 @@                    （LoadDeck 文档 + 方法体）
Deck/DeckRuntime.cs        @@ -471 / -2317,10 / -2474 / -4031     （4 处，均为注释）
Editor/DeckScene.cs        @@ -2707 +2711                         （live 注释）
                           @@ -2806,0 +2822,3                    （A364 段注释）
                           @@ -3047,0 +3096,2                    （A363 五突变段注释）
                           @@ -3117,0 +3168,99                   （新增 A397 断言段）
```
`git diff --numstat`：`DeckEditorState.cs 35/1` · `DeckRuntime.cs 78/15`（含同期别人的 hunk）·
`DeckScene.cs 259/15`（含同期别人的 hunk）—— 与文件行数（471 / 4478 / 3836）相差两个数量级 ⇒ **行尾没被翻**（全 CRLF）。

---

## 三、`State.Deck` 引用逐处复核表

> 命令（可复跑）：
> `grep -n "State\.Deck\b" Deck/DeckRuntime.cs` ·
> `grep -n "_rt\.State\.Deck\b\|state\.Deck\b\|s\.Deck\b" Editor/DeckScene.cs` ·
> `grep -n "[^a-zA-Z_]Deck\.[A-Za-z]" Deck/DeckEditorState.cs`
> 口径：**「跟着改」= 语义/心智模型变了**（代码不一定要动）；**「不受影响」= 逐字读到的内容与改前相同，或它本来就不碰这个对象**。

### 3.1 `Deck/DeckEditorState.cs`（35 处内部 `Deck.…`）—— **全部「跟着改」，但代码一行不用动**

这一族**就是**「编辑器改的是副本」这句话的落点：`Deck` 这个字段指向谁，它们就改谁。

| 行 | 干什么 | 复核结论 |
|---|---|---|
| `119` `Skirmish` 读 `Deck.IsSkirmish` | 派生只读（模式从这副牌来） | **跟着改**：读的是副本，而副本的 `GameMode` 是 `Clone()` 照抄的 ⇒ 值不变；派生链不变 |
| `179-184` `FillNameFromWarlord` 读/写 `Deck.Name` | 补名（A365） | **跟着改**：写的是副本。原版 `ValidateDeck` 改的也是 `EditingDeck`（副本）⇒ **方向完全一致** |
| `190/193/194` `DeckCount`/`SlotsLeft`/`MaxDeckCount` | 读 `Deck.CardIds.Count` | **跟着改**（读副本）· 值不变 |
| `217-236` `CanAdd`/`TryAdd` 读/写 `WarlordId`/`DefensiveId`/`CardIds` | 突变 | **跟着改**：只动副本 ⇒ 这正是原版（`DeckEditingPanel.Drop` 改的是 `editingDeck`） |
| `251-260` `SetWarlord` 读/写 | 突变 + 按阵营清卡 | **跟着改**：只动副本 |
| `268-274` `TryRemove`/`ClearWarlord`/`ClearDefensive` | 突变 | **跟着改**：只动副本 |
| `279-281` `ClearCards` | 突变 | **跟着改**：只动副本 |
| `300` `VisibleCards` 读 `Deck.WarlordId` | 卡池按督军分流 | **跟着改**（读副本）· 值不变 |
| `432-433` `SetDeckName` 读/写 `Deck.Name` | 改名 | **跟着改**：只动副本（原版 `DeckEditingPanel__ChangeName.c:6` 改的也是 `editingDeck`） |
| `461` `CostCurve` 遍历 `Deck.CardIds` | 读 | **跟着改**（读副本）· 值不变 |
| `133` `NewDeck` 造新对象 | 与 `LoadDeck` **无关** | **不受影响**（它直接 `new PlayerDeck`，本来就不共享） |

### 3.2 `Deck/DeckRuntime.cs`（20 处代码 + 4 处注释）

| 行 | 干什么 | 复核结论（跟着改 / 不受影响 + 为什么） |
|---|---|---|
| `1237` `1238` `PoolCounterText` 读 `WarlordId`/`CountOf` | 卡池格底部那条「已有/上限」 | **不受影响**：读副本，内容与改前逐字相同（副本是照抄的）；唯一差别是「库级 `Save()` 之后不再跟着库变」—— 而那正是本件要的 |
| `1465` `1484` `1485` 读 `CardbackId`/`WarlordId` | 卡背抽屉那两张 | **不受影响**（同上，纯读） |
| `1503` `1504` `EquipCardback` 读/写 `CardbackId` | 装备卡背 | **跟着改**：写的是副本 ⇒ 只有 `Done`/`ESC` 才进库。**这正是 A363 已经定下的口径**（该处注释：「只标脏」）⇒ 不冲突 |
| `1602-1604` `DeckEntries` 读三个槽 | 卡组列表那几行 | **不受影响**（纯读） |
| `1632` 行计数 `CountOf` | 同上 | **不受影响** |
| `1839` `1840` 页头名字/占位提示 | 纯读 | **不受影响** |
| `2453` `ShareDeckString` → `ExportString(State.Deck)` | 分享（写剪贴板） | **不受影响**（纯读；原版同一处读的也是 `EditingDeck`） |
| `2919` `UiShareString`（自检口） | 纯读 | **不受影响** |
| `2943` `EquippedCardback`（自检口） | 纯读 | **不受影响** |
| `3097` `BeginNameEdit` 读 `Deck.Name` | 进改名态时把现名填进输入框 | **不受影响**（纯读） |
| `3148` 改名后那句提示读 `Deck.Name` | 纯读 | **不受影响** |
| `4099` `CommitDeck` → `Library.CommitCurrent(State.Deck)` | **唯一写回口** | 🔴 **这就是 A397 的落点**：`CommitCurrent` 逐字段把副本拷进库里那份（`DeckLibrary.cs:134-152`）⇒ **代码不用改**，但它现在**名副其实**（改前它拷的是「同一个对象自己」，等于空操作） |
| `4451` `ArmyFaction` 读 `WarlordId` | 纯读 | **不受影响** |
| `473` / `2486` 两处 `State.LoadDeck(...)` | 装机点 | 🔴 **改的就是它们**（现在装副本）：`Build`（:473）与 `TryImport`（:2486） |
| `2328` `2330` `2331` `4093` 注释里的 `State.Deck` | 文档 | **已订正**（§二 #5 #6） |
| `1916` 用的是 `State.DeckCount`（不是 `State.Deck`） | 页头 `n/30` | **不受影响** |
| `4459+` `PlayerDeckForDemo(DeckEditorState)` | 空库时造演示卡组 | **不受影响**：它**造**一副新牌交给 `Library.CommitCurrent`（建库那一拍），不走 `LoadDeck` |

**`Library.Current` 的两处**（`:468` 日志 · `:473` 装机）—— 复核结论：`:468` 只打日志；`:473` 是装机点（见上）。

### 3.3 `Editor/DeckScene.cs`（47 处；`state` 与 `_rt.State` **是同一个对象**）

| 行 | 族 | 复核结论 |
|---|---|---|
| `719` `723` `758` `761` `768` `770` `869` `882` `883` | `s.Deck`（`TestEditing` / `TestSkirmishMode` 里的**纯逻辑 state**，`NewState()` 造、**从不 `LoadDeck`**） | **不受影响**：那些 `s` 的 `Deck` 是构造器里新建的空 `PlayerDeck`，与卡组库无任何关系；`s.LoadDeck(new PlayerDeck(...))`（`:842` `:861`）现在装副本，而用例只读 `Skirmish`/`MaxDeckCount`/`CardIds` ⇒ 值逐字相同（`:882-883` 那副 `over` 本来就是**另造**一副） |
| `1190` `1194` `1208` `2440` | 界面态读 `_rt.State.Deck.WarlordId/CountOf` | **不受影响**（纯读；`1208` 那条「把督军放回去」走的是 `SetWarlord`，动的是副本 —— 与它断言的目标一致） |
| `2612` `2626` `2627` `2630` `2634` `2642` `2643` `2647` `2650` `2651` `2665` `2674` `2688` `2696` | `state.Deck.*`（A363/A365/A223 段）**读 + 直接改内存** | **跟着改**（改的是副本）。逐条复核：这些用例断的是「名字/张数变成了什么」「ESC/Done 之后盘上是什么」——**都不依赖对象身份**（盘上的判据一律走 `DeckLibrary.Load()` **从盘重读**）⇒ 全部仍然成立 |
| `2711` `2825` `3098` | 3 处 `var live = _rt.State.Deck;` 别名 | **跟着改（注释已改）**：`live` 捕获的是「编辑器那份」。改后它仍是**同一个**「编辑器那份」，所以**别名不会失效** —— 前提是「捕获之后没人再调 `LoadDeck`」，本仓 `LoadDeck` 只有 2 个调用点（`DeckRuntime:473/2486`），都不在这些段落中间 ⇒ **成立**（§四） |
| `3190-3253`（我新加的段） | 新断言 | **新增**（§五） |
| `3295` `3302` `3308` `3311` `3333` `3357` | `_rt.State.DeckCount` 读 | **不受影响**（读副本张数；拖着删/补回走的是 `State.TryRemove`/`TryAdd` ⇒ 副本） |
| `3433` `3488` `3489` `3490` | A399 段 `snapA` + 收尾 `Clear()/AddRange()` | **跟着改（行为不变）**：动的是副本；收尾随后 `EscPressed()` 提交 ⇒ 盘上与内存仍一致（该段原有断言一字未改，§四·4 复核过） |
| `3514` `3520` `3556` | A502 段读/写 `_rt.State.Deck.Name` | **跟着改，而且判据更强**：`"A502·ESC 不该落盘"` 只落内存 —— A397 之后它**连库那份对象都碰不到**（改前它会改到库里那个对象的内存态）⇒ 该段「盘上没动」的鉴别力**只增不减** |
| `3928` | `BuildAndSaveScene` 里打印 `state.Deck.Name` | **不受影响**（纯读；那条路用玩家真存档 Build，打印的是副本名字 —— 与改前逐字相同） |

### 3.4 其它宿主 —— **一处都不受影响**（为什么）

`grep -rn "DeckEditorState"` 全仓的构造点共 7 处（`DeckRuntime:527`、`DeckScene:565/573`、
`Shell/CollectionData.cs:44`（当**卡表索引**用）、`Shell/CollectionWindow.cs:557/570/1126`），
而 **`LoadDeck` 全仓只有 2 个调用点**（`DeckRuntime:473` / `:2486`）
⇒ 收藏窗 / 收藏页那几份 `DeckEditorState` **从不装牌**（它们的 `Deck` 一直是构造器新建的空 `PlayerDeck`）
⇒ **A397 对它们零影响**。
`DeckLibrary`（`RuleEngine/Data/DeckLibrary.cs`）**一个字节没改**：`Current` 照旧返回库里那个对象；
`CommitCurrent` 照旧逐字段拷 + `new List<string>(…)`（⇒ 提交后两边**不共享卡表**，见 §五 第 5 条）。

---

## 四、夹具前提复核（`Editor/DeckScene.cs`）

> 铁律：「照原版把实现改对了」会把**老夹具的前提**推翻 ⇒ 逐处问「它靠『同一对象』活着吗」。

| # | 夹具（现读行） | 原来靠什么活着 | 现在怎么活 | 结论 |
|---|---|---|---|---|
| 1 | A330 段 `:2711`（`live` 别名 + `live.CardIds.RemoveAt(0)` + 「盘上仍是满编」） | 「`live` = `Library.Current` 那个对象」 | `live` = 编辑器那份**副本**；`RemoveAt` 只动内存；「盘上仍是满编」靠的是**其间没人调 `Save()`**（该段确实没有） | ✅ 仍绿（注释已改） |
| 2 | A364 段 `:2825`（同上，且 `live` 一路用到 ⑦ 的补回） | 同上 | 同上；且该段**中途不调 `LoadDeck`** ⇒ `live` 引用不会悬空 | ✅ 仍绿（注释已改） |
| 3 | A363 五突变段 `:3098`（起手前提「`ExportString(live)` == `ExportString(磁盘那份)`」） | 同上 | 每节收尾都按过 `Done` / `ESC` ⇒ **副本与库逐字节一致**（`CommitCurrent` 是全字段拷）⇒ 起手前提仍成立 | ✅ 仍绿（注释已改） |
| 4 | A399 段 `:3432-3491`（`snapA` / 收尾 `Clear()+AddRange()`） | 同上 | 收尾逐字节还原**副本**，再 `EscPressed()` 提交 ⇒ 盘上复原（该段原有断言一字未动） | ✅ 仍绿 |
| 5 | A502 段 `:3514-3560` | 「只改内存 ⇒ 盘上不该动」 | 同前，且隔离更强（副本连库对象的**内存态**都碰不到） | ✅ 仍绿（判据更强） |
| 6 | A364 ⑥⑦（`:3000-3050`：点 Discard 之后**靠 `live` 仍是那副 29 张、不合法**） | 编辑器那份**内容不变**（只有脏标记被清） | **本件没做「Discard 时回滚内存」** ⇒ 内容确实不变 ⇒ 前提**依然成立** | ✅ 仍绿（也是 §六·1 那条账没做的原因之一） |
| 7 | `TestLibraryWiring` `:890` / 交接 smoke `:450` / 遭遇 smoke `:506` | 各自 `DeckLibrary.Load()`（**另一份库实例**）+ `rt.Build(lib)` | `Build` 现在给编辑器一份副本；这三个 smoke 只读 `State.Skirmish` / `MaxDeckCount` / 界面文字 | ✅ 仍绿（smoke 里 `lib.Create/Delete` 动的是**它们自己那份** lib，与编辑器那份副本无关） |
| 8 | 拖出删除 + A415 段 `:3295-3360` | 读 `_rt.State.DeckCount`；删走 `State.TryRemove` | 动副本 ⇒ 张数逐字相同 | ✅ 仍绿 |
| 9 | 卡背段 `:2470-2495`（`EquippedCardback` / `CommitCurrent` 逐字段） | 读 `State.Deck.CardbackId`；`Done` 后盘上有 `names[8]` | 写副本 → `Done` 提交 ⇒ `CommitCurrent` 有 `dst.CardbackId = deck.CardbackId`（六格之一） | ✅ 仍绿 |

**没有被本件改动碰到的夹具**：我**只**改了 3 处注释 + 新增 1 段（§二 的 hunk 表）—— 其余断言一个字没动。

---

## 五、断言清单（落点 `Editor/DeckScene.cs:3168-3265`，宿主 `DeckScene.Run`）

段头写了判据与三条「各断一件事」的分工。逐条：

| # | 断什么 | 怎么分辨两种状态 | 改坏法（怎么改会红） | 行 |
|---|---|---|---|---|
| 1 | **结构**：`State.Deck` **不是** `Library.Current` 那个对象 | 两个对象用 `ReferenceEquals` 直接比（两者独立取得） | `DeckEditorState.LoadDeck` 改回 `Deck = deck ?? new PlayerDeck();` ⇒ 红 | `:3196` |
| 2 | **内存**：编辑器摘一张牌，**库里那个对象在内存里也没被改** | 库里那份的 `CardIds.Count` 仍是满编（同一个 `List` 时会是 29） | 同上（直接赋引用）⇒ 红 | `:3204-3211` |
| 3 | **行为·未写盘**：走一次**库级**写盘路（`DeckLibrary.Rename` ⇒ 内部 `Save()` 序列化整份库），**盘上仍是满编** | 从**盘上重读**（`DeckLibrary.Load()`），比 `CardIds.Count`；并另有一条「（前提）它确实写盘了」（比盘上名字）挡住「写盘失败 ⇒ 假绿」 | 直接赋引用 ⇒ 盘上会变成 29 张 ⇒ 红 | `:3216-3230` |
| 4 | **行为·写回**（正向对照）：`Done`/`ESC` 那一拍**照样**把副本整份拷回库里 + 落盘 | 比**库对象内存态**的名字与**盘上**的名字 | 把 `CommitDeck` 拆掉 / 不再调 `CommitCurrent` ⇒ 红（挡住「干脆不写回」的假绿） | `:3241-3249` |
| 5 | **提交后仍不共享卡表**：`CommitCurrent` 是整份拷（`new List<string>(…)`） | 提交后再摘一张，库里那份仍是 30 张 | `DeckLibrary.CommitCurrent` 里 `new List<string>(…)` 去掉 `new` ⇒ 红（否则「副本隔离」在**第一次 Done 之后**失效） | `:3252-3258` |

**为什么这几条不是自证 / 不是弱断言**：
- 检测器是**独立通道**：① 是对象身份；③④ 是 `DeckLibrary.Load()`（**从盘重读**，与内存里那份是两个对象）；
  ② ⑤ 是**库里那个对象**本身（与 `_rt.State.Deck` 是不同引用）。
- ⑤ 是**灭自证型**：它挡的是「实现和检测器用同一个口」的一种回退 —— 只把 `LoadDeck` 改对、却让
  `CommitCurrent` 变成共享 `List`，那样的隔离会在第一次 `Done` 之后失效，而 ①②③ 都测不出（它们发生在提交之前）。
- 段首有两条前提（起手「编辑器那份与库里那份内容一致」+ 「库级写盘确实成功」）—— 少了它们，
  ③ 会变成平凡真。改坏法：把 `Rename` 换成不写盘的动作（例如不调 `Library` 直接改内存）⇒ 前提那条红。

**收尾**：名字复原 + 再按一次 `Done` 落盘 + 「仍是满编 / 不脏」三条，保证后面几节的前提不被动。

---

## 六、没查清 / 没做的（**不是「不做」，是「另开一条账」**）

1. 🔴 **Discard（「丢改动」左钮）之后不做内存回滚** —— 与 A397 **同源但另一件事**：
   - **判据**：原版 `.<ConfirmDiscard>b__45_1` = `HidePopUp()` + 虚槽 `0x1b8`（`GameWindow.Close`）
     ⇒ **关窗**，窗一关那份 `EditingDeck` 副本随之销毁；库里那份**从头到尾没被动过**。
   - **我们的现状**：`DeckRuntime.DiscardChangesAndLeave` 只清脏标记 + `BackToMenu()`。
     真机上 `BackToMenu()` 会切场景 ⇒ **与原版等价**；但**批处理 / 不切场景**的路径上，
     `State.Deck` 仍是「被丢弃」的那份内容（脏标记已清）⇒ 此时按一次 `Done` 会把它写回库。
   - **修法**（已想清，2 行）：`State.LoadDeck(Library.Current); RefreshAll();`
     ——⚠️ **但它会把现有夹具的前提推翻**：`Editor/DeckScene.cs` 的 **A364 ⑥⑦** 与 **A399 ③** 捕获了
     `var live = _rt.State.Deck;`（A364 那处 `:2825`）并靠「Discard 之后 `live` 仍是那副 **29 张、不合法**」活着
     （A364 ⑦ 起手就是 `EscPressed()` ⇒ 仍不合法 ⇒ 弹第 3 扇窗）。若回滚，需在同一节里**重新造脏**
     （如 `live.CardIds.RemoveAt(0); _rt.UiScrollPool(0f);`，且回滚**必须原地改内容**、不能换对象，否则 `live` 悬空）——
     **改动落点已定位、风险可枚举，但本件不能跑 Unity 验证** ⇒ 建议**单独一条账**，由调度台排一位写手 + 一次 `DeckScene.Run`
     （判据、修法、受影响夹具就是这三行；段头注释 `Deck/DeckRuntime.cs:2319-2334` 已按铁律 5 如实写成现状）。
   - ⛔ **不是「影响小不做」**：它在真机不可观测（离场即销毁），但在批处理/自检里可观测，属**待复刻的差异**。
2. **「写回哪一副」的耦合没钉死**（如实记）：原版写回的是**开窗时传进来那一副**（窗的 `+0x40`），
   我们写回的是 `DeckLibrary.CommitCurrent` 的「**当前选中**那一套」（`_decks[_current]`）。
   两者等价的**前提**是「编辑期间 `_current` 不变」——`_current` 只可能被
   `Library.Select/Create/Duplicate/Add/Delete` 改，而 `DeckRuntime` 只在 `Build`（`Select`）与
   `TryImport`（`Add`，且紧接着重装 `LoadDeck`）里调 ⇒ **当前无可达路径**。
   但要真钉死，得在 `CommitDeck` 里加一条「`_current` 与开窗时不同就出声」的守卫（或让 `DeckLibrary` 支持按下标提交）——
   `DeckLibrary` 不在本件白名单 ⇒ **只报不做**，请调度台裁是否立账。
3. **真 Play 没验**：`_current` 在真机上会不会被别的窗改（主菜单/收藏是**另一次 `DeckLibrary.Load()`**，不同实例 ⇒ 不会）
   —— 结论有推理无实拍，且本件禁跑 Unity。
4. **老报告仍写着「同一个对象」**（本件无权改，⛔ 不动别人的报告）：
   `资料/普查产出_1012/D1_卡组脏标记与补名.md` §五·2（「我们编辑的是【库里的那个对象本身】」）、
   `资料/普查产出_1012/H44_Deck一族三笔.md:164`（引了旧 Discard 注释）、
   **`资料/普查产出_1013/A表现核_块2.md` §A397（「现状：仍开」）** —— 请调度台在销账时一并加更正横幅。
   已复查：**没有任何正本**（`资料/阶段二_卡组线_原版规格.md` / `资料/卡组编辑界面_查证_0920.md` 等）写着这个身份断言（`grep` 过）。

---

## 七、顺手发现（⛔ 只报不改）

1. 🔴 **`PlayerDeck.Clone()` 不是「逐格照抄」** —— 它走构造器，而构造器把**空/null 名字**替成「新卡组」
   （`RuleEngine/Core/DeckRules.cs:335`）。今天两处调用者都侥幸没事：
   `DeckLibrary.Duplicate` 随后覆盖名字（`:110`）、本件 `LoadDeck` 补了一行还原。
   **将来任何「拿 `Clone()` 当拷贝构造用」的地方都会静默改名** ⇒ 建议（另一条账）把 `Clone()` 本身改成逐格拷
   （`RuleEngine/` 不在本件白名单）。
2. **卡背不进分享串 —— 这次现读核过，是照原版的，不是漏做**（顺手留个凭证，免得下一个人立假账）：
   `PlayerDeck` 六格字段里，`DeckLibrary.ExportString` / `ImportString` 只管**五格**（**没有 `CardbackId`**）。
   原版 `CardDeck__Serialize.c` 逐句实读：`name`（`+0x10`，`:` → `%3A`）+ `GetLibraryInFull()` 的每一项
   （`:` 分隔）+ `;` + `gameMode`（`+0x70` 那格 `Nullable<int>`，null 写 0）——**方法体里没有任何卡背字段**
   ⇒ 我们的格式与它逐字对齐，**卡背不进串是对的**（分享给别人时卡背留在自己这边）。
   `DeckRules.cs:305` 那句「改字段要同时补**四处**拷贝点」也与实际一致（`Clone` / `CommitCurrent` / `ExportString` / `ImportString`）。
3. **`DeckLibrary.Add(deck)` 把**调用方那个对象**直接放进 `_decks`**（不拷）—— `TryImport` 里 `Add(deck)` 之后紧跟
   `State.LoadDeck(Library.Current)`：改前编辑器直接引用**那个对象**，改后是副本 ✓。今天无害（`deck` 是
   `ImportString` 刚造的、没有第三方持有），但这个「传入对象被库收编」的语义值得记一笔
   （与 A397 是同一类问题：**谁持有谁**）。
4. **`DeckScene.cs` 的 A364 ⑥⑦ 与本件强耦合**（见 §六·1）：任何人将来做「Discard 回滚内存」都会先撞上它，
   建议在那一节留一句指向本报告的注释（未改，避免与 A364/A502 的在场写手撞车 —— 那两段最近刚被别人改过）。

---

## 八、类型检查结果

```
$ TMPDIR=/tmp/wf_wd1b bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（跑了 **2 次**：改完 `DeckEditorState`/`DeckRuntime`/`DeckScene` 的注释与断言段后各一次，都 0。
⚠️ 期间**没有**并发跑别的 Unity/类型检查。改完 `git diff --numstat` 核对，**行尾没被翻**
（三个文件都是 CRLF，改动量 35/1 · 78/15 · 249+15，与文件行数相差三个数量级）。）

**未跑**：`DeckScene.Run`（本件禁跑；它会跑我新加的那 4 条断言）—— 同步点请调度台跑它（宿主只有它一个）。
其余 10 条自检与本件**无关**（`grep` 过：`LoadDeck` 只有 2 个调用点、其余宿主从不 `LoadDeck`，见 §3.4）。
