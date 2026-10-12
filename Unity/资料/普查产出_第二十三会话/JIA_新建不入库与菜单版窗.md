# 甲 · A1697（新建不入库）+ A1698（改开菜单版窗）

> 写手：甲（2026-10-12）。工程根 `D:/4`。**未跑任何 Unity 批处理、未动 git 写操作、未改正本。**
> 报告路径 = 本文件。所有行号 = 本轮改完之后的**现读**行号。

## 一、结论

1. **A1697**：`CollectionData.CreateDeck` 从「造 + 入库 + 落盘」改成**只造**（照原版 `GetEmptyDeck`），
   交接改成 `PendingNewDeck`；编辑器的 `Done` **过了 `Validate()` 才 `DeckLibrary.Insert`**；中途退出/丢弃**不留痕迹**。
   **用户能看到的变化**：点「新建卡组」⇒ 库里**不再多出一格卡背+「新卡组」**；它直接进编辑器；
   **没选战将就退出 ⇒ 什么都没有**（不再留一副非法卡组在列表里）。
2. **A1698**：`DeckRuntime.OpenCardWindow` 从**战斗版摆放**（`Battle/CardDisplayWindow`，`options` 恒 null）
   改成**菜单版**（`Shell/CardDetailPopup`，= 原版 `showOptions=1` 那一支）。
   **用户能看到的变化**：卡组编辑里点一张卡 ⇒ 右边那一列**回来了**（通配符条 / 计数条 / 异画面板）；
   创建副本/升级两块**仍然不建**（用户 2026-09-27 拍板停掉，不是本笔的范围）。

## 二、A1697

### 2·1 原版那条路我逐行读过（`D:/2/tools/decomp_full/`）

| 判据 | 行 | 内容 |
|---|---|---|
| `SelectDecksTab__CreateDeck.c` | `:17` | `lVar2 = CardDeck__GetEmptyDeck(0)` —— **只造** |
| 同上 | `:20-25` | 打 gameMode（`+0x70`）+ `syncedToServer=0`（脏） |
| 同上 | `:28` | `WindowsManager__OpenWindow<object>(lVar3, lVar2, 0, …)` —— **直接开窗**；**全链没有入库调用** |
| `CardDeck__GetEmptyDeck.c` | `:28-32` | `+0x40 = 0`（督军）· `+0x48 = 0`（防御）· `+0x50 = 0`（模式）|
| `DeckEditingWindow__TrySaveDeck.c` | `:80` | `cVar3 = DeckUtility__ValidateDeck(param_1[0x23], local_res8, 1, 0)` |
| 同上 | `:81-98` | 不合法 ⇒ `WindowsManager__ShowPopUp(ToRawLocalizationString(err), …, lVar5, lVar4, 0)` 然后 `return`（**绝不走到 `:100`**）|
| 同上 | `:100` | `DeckEditingWindow__UploadDeck(param_1, 0)` —— **入库的唯一入口** |
| `DeckUtility__ValidateDeck.c` | `:32-33` | `op_Equality(uVar1, 0)`（`uVar1 = *(deck + 0x40)` = 督军），**为假（非 null）才继续** |
| 同上 | `:86-89` | 督军为 null 那一支 ⇒ `*param_2 = 2; return 0;` = **错码 2 = 不合法** |
| `DeckEditingWindow__UploadDeck.c` | `:63` | `CardDeck__CopyDeck(*plVar7, *(param_1 + 0x118))`（库那份 ← 编辑中那份）|
| 同上 | `:74` | `PlayerDataManager__UploadUnsyncedDeck(lVar2, lVar1, uVar6, 0)` |
| `DeckEditingWindow__UpdateDoneButton.c` | `:24` | 同一条 `ValidateDeck` ⇒ 只切高亮（`:69 UIEffectTogglerController__Toggle`）、**不设 `interactable`** |

⚠️ **diagnosis 的行号订正（铁律 5）**：R1 报告把「没督军」那一支写成 `:49,52` —— 现读
**`:32-33` 是比较、`:86-89` 才是 `*param_2 = 2; return 0;`**（`:49` 是 `*param_2 = 0`，走的是**合法**那一支之前的复位）。
结论不变（没督军 = 错码 2），只是行号要按上面这张表。

### 2·2 改了哪几处（并排关键一行）

| # | 文件:行 | 改前 | 改后 |
|---|---|---|---|
| ① | `RuleEngine/Data/DeckLibrary.cs:187` | （无） | `public PlayerDeck NewUnsaved(string name, int gameMode = 0)` —— **只造、不入库、不落盘**（= `GetEmptyDeck`）；重名去重仍走 `UniqueName`（只读 `_decks`） |
| ② | `RuleEngine/Data/DeckLibrary.cs:206` | （无） | `public bool Insert(PlayerDeck deck)` —— **入库的唯一入口**（`_decks.Add` + `_current` 指过去 + `return Save()`），照 `UploadUnsyncedDeck` |
| ③ | `Shell/CollectionData.cs:320` | `var d = Lib.Create(name, gameMode);` + `Lib.LastError` 判空串 | `PlayerDeck d = Lib.NewUnsaved(name, gameMode); PendingNewDeck = d; PendingEditDeck = -1; return d.Name;` |
| ④ | `Shell/CollectionData.cs:397,400` | （无） | `public static PlayerDeck PendingNewDeck;` + `TakePendingNewDeck()`（先取后清，同 `TakePendingReturn`）|
| ⑤ | `Deck/DeckRuntime.cs:990-1008` | 只读 `PendingEditDeck` → `Library.Select(want)` → `LoadDeck(Library.Current)` | **先读** `TakePendingNewDeck()` ⇒ `_newDeck = freshDeck`；非 null 时**跳过** `Library.Select`，`State.LoadDeck(freshDeck)` |
| ⑥ | `Deck/DeckRuntime.cs:6937-6961`（`CommitDeck`）| `bool ok = Library.CommitCurrent(State.Deck);` | **先分叉**：`_newDeck != null` ⇒ `Library.Insert(State.Deck)` + `_newDeck = null` + `State.LoadDeck(Library.Current)`（换回副本形态）+ `RefreshAll()`；否则原路 `CommitCurrent` |
| ⑦ | `Deck/DeckRuntime.cs:4499`（`RollBackEditingCopy`）| `State.LoadDeck(Library.Current);` | `State.LoadDeck(_newDeck != null ? _newDeck : Library.Current);` |
| ⑧ | `Deck/DeckRuntime.cs:4801-4810`（`TryImport`）| （无） | 换编辑对象之前 **放弃** `_newDeck`（`Debug.LogWarning` 出声）—— 不清它的话导入后按 `Done` 会把**导入那副的内容挂在「新卡组」名下**插一遍 |
| ⑨ | `Shell/CollectionWindow.cs:3561` | `CreateDeck()` = 造（入库）+ `RebuildDeckCells` | `CreateDeck()` = 造（不入库）+ **`GoNewDeck()` 直接进编辑器**（照原版 `:28`）|
| ⑩ | `Shell/CollectionWindow.cs:3534` | （无） | `public static void GoNewDeck()` —— 写回程意图 + 批处理闸 + `LoadScene("DeckEditor")` |
| ⑪ | `Shell/LiveOpsEventWindow.cs:1119` | `IndexOf(name)` → `Select(idx)` → `PendingEditDeck = idx`（+ 两条警告） | 整段**作废**：只 `PendingEditDeck = -1` + 日志（交接由 `CreateDeck` 写好了）—— **⚠️ 白名单偏离，见 §七** |

### 2·3 `A566` 那段裁定的订正痕

`Deck/DeckRuntime.cs:6927-6937`（`CommitDeck` 的 doc，紧随原 A566 那一段 —— 原段在 `:6914`）新增一段：

> 🔴 **2026-10-12（`A1697`）就地订正（铁律 5）：上面那条 A566 裁定【已作废】。**
> **错因 / 废止理由**：那条裁定的前提是「**新建卡组也走 `_current`**」……而**原版根本不是那样**：
> 新建的卡组**压根不在库里**……`Done` 那一拍走的是**入库**（`__TrySaveDeck.c:100 UploadDeck` →
> `UploadUnsyncedDeck`）、不是「写回当前选中」。⇒ 用户 2026-10-12 拍板**按原版复刻** ⇒ 本条裁定**作废**，
> `DeckLibrary` 这一笔**照改**（加了 `NewUnsaved` / `Insert` 两个口）……
> ⚠️ **旧裁定里仍然成立的那半**：「编辑期间 `_current` 不变 ⇒ `CommitCurrent` 写的就是开窗那一副」
> —— 那一半**照旧成立**，只是它现在**只覆盖「编辑库里已有的卡组」那一条路**。

⚠️ **一处如实标注（对象身份，观测不到）**：原版 `UploadDeck` 先把 `EditingDeck` 拷回 `window + 0x40` 那一副、
再上传**那一副**；我们 `Insert` 递进去的是 `State.Deck`（编辑器那份副本）。两者**入库的内容逐字节相同**、
库里那副对象**没人按身份引用** ⇒ **观测不到差别**；`_newDeck`（开窗时那一副）在成功那一刻被丢弃。
本笔**没有**为此再加一遍「逐字段拷回」（那会变成 `CommitCurrent` 的第二份拷贝实现，铁律 6）。

### 2·4 既有行为自查（逐条）

| 要求 | 结论 | 依据 |
|---|---|---|
| 在编辑器里**改到一半就退出** ⇒ **不留任何痕迹** | ✅ | 「造」那一拍**一个字节都不写盘**（`NewUnsaved`）；`Done` 之外**没有**第二个入库点（`CommitDeck` 全窗只由 `SaveAndSay` 调）；`TryClose`/`Discard` 走 `RollBackEditingCopy`（只回内存） + `BackToMenu`（只切场景）|
| **合法**卡组的保存 / 改名 / 删除照旧 | ✅ | `CommitCurrent` / `Rename` / `Delete` / `Duplicate` / `Add` / `ImportString` **一个字没改**；`_newDeck == null` 时 `CommitDeck` 走**原来那一行**（`Library.CommitCurrent`）|
| 「编辑库里已有卡组」那条路不受影响 | ✅ | `Build` 只有在 `PendingNewDeck != null` 时才跳过 `Library.Select`；`PendingEditDeck >= 0` 那一支原样保留（改成 `else if`）|
| 空库时那条「演示卡组」兜底 | ✅ | `DeckRuntime.cs:972-980` 未动（它在两条交接之前跑）|
| `Done` 重复按不会重复入库 | ✅ | `CommitDeck` 的 `_newDeck = null` 与 `ins` 的成败**无关**（`Insert` 已经把对象加进 `_decks` 了，与 `Create` 同一语义）；批处理断言见 `DeckScene.cs:794-797` |
| 🔴 **「新建顺手改掉「当前选中」并落盘」**（诊断 §五·2 那条：玩家什么都没选，选中项却变了）| ✅ **一并消失** | 新口 `NewUnsaved` **不碰 `_current`**；只有 `Insert`（= 入库那一刻）才把 `_current` 指到新那副 |
| 落盘失败不静默 | ✅ | `Insert` `return Save()`（不吞）；`SaveAndSay` 的失败支照旧 `Say("保存失败：" + …)`；`DeckDirty = !ins` ⇒ 下次 `Done` 再试 |

## 三、A1698

### 3·1 落点

`Deck/DeckRuntime.cs:6988-7003`：

```csharp
        CardDetailPopup _cardWindow;
        WindowsManager _cardWinHost;

        void OpenCardWindow(CardDef def)
        {
            if (def == null) return;
            if (_cardWinHost == null) _cardWinHost = WindowsManager.EnsureHost();
            if (_cardWindow == null)
            {
                _cardWindow = CardDetailPopup.Create(_cardWinHost);
                _cardWinHost.OpenWindow(_cardWindow);
            }
            _cardWindow.ShowCard(def);
        }
```

开窗写法**照同族先例**（`Shell/CollectionWindow.OpenCardDetail`：`Create` → `OpenWindow` → `ShowCard`），
宿主照 `ShowDiscardChangesPopUp` 那条先例（`WindowsManager.EnsureHost()`，⛔ **不建到 `_root` 底下**）。
⛔ **未改 `Shell/CardDetailPopup.cs` 一个字节**（另有写手），只调它既有的公开入口
（`Create` / `ShowCard` / `CurrentState` / `Card` / `Hide`），**没有改任何签名**。

### 3·2 两个自检桩 + 两条断言

| 桩 | 改前 | 改后 |
|---|---|---|
| `UiCardWindowVisible` | `_cardWindow != null && _cardWindow.Visible`（`CardDisplayWindow` 自己的字段）| `_cardWindow.CurrentState != WindowState.Closed`（`GameWindow` 的对外状态）|
| `UiCardWindowTitle` | `_cardWindow.ShownTitle` | `CardText.Name(_cardWindow.Card.Name, _cardWindow.Card.NameZh)`（与断言里那个期望值**同一个口**）|
| `UiCloseCardWindow` | `_cardWindow.Hide()` | `_cardWindow.Close()` —— 🔴 **必须**：`Hide()` **不摘表**，这扇窗现在在 `WindowsManager.openWindows` 里，后面任何一次 `ShowPreviousWindow`（别的窗关闭时发生）都会**把它又叫回来**（实测路径 → `DeckScene` 的 B25 那一节开/关一扇设置窗）|
| `UiCardWindowRightColumn`（**新增**）| —— | 数这三个节点：`Card Counter` / `Wildcard Count 0` / `AltArt Title` |

`Editor/DeckScene.cs`：
- `:5925` `UiCardWindowVisible` 那条**原文未改**（契约不变）；`:5926` `UiCardWindowTitle` 那条**原文未改**。
- `:5927-5937` **新增**一条：`CheckTrue(_rt.UiCardWindowRightColumn >= 3, "★ A1698：…右侧那一列在…")`
  —— **改坏法**：把 `OpenCardWindow` 改回 `CardDisplayWindow.Create(Root)` ⇒ 一件都不建 ⇒ 红。
- `:5938` `_rt.UiCloseCardWindow();` 原文未改。

### 3·3 那句错注释的订正

`Deck/DeckRuntime.cs:6969-6987`（`_cardWindow` 那段 doc，原文留痕后写清）：

> 这里原来写的是「复用**对战**的放大窗（查证：原版放大窗与战斗里那个是同一套 `CardDisplayWindow`）」。
> **那句话只对了一半，而且正是这次踩坑的源头**：原版确实是**同一个脚本**，但**两处摆放的 `options` 不同**
> —— 菜单版那扇的 `options` 非 null，而竞技场里 13/13 全是 `null`（我们这边 = `Battle/CardDisplayWindow.cs:16-22`）。
> 原版卡组编辑**硬编码**要走**有 options 的菜单版**：`DeckEditingWindow__OpenCardInformation.c:56-58` 那句
> `ShowCard(win, card, tier, **1**(= showOptions), 0, …)`；而 `CardDisplayWindow__ShowCard.c:159` 的守卫是
> `options != null && card.field_0x4c == 0` ⇒ `:163 SetActive(1)` + `:165 CardDisplayOptions__Initialize`（那一列……）。
> ⇒ **用户看到「右边那里没有了」的直接原因就在这一句**。

## 四、要调度台做的事

### 4·1 跑哪三条自检

`DeckScene.Run` · `CollectionScene.Run` · `MainMenuScene.Run`（**必跑**；全在**批处理串行**那一套里）。
`BattleScene.Run` **不在本笔覆盖面内**（没动 `Battle/CardDisplayWindow.cs`，§A860 那条断言照旧钉着战斗版没 options）。

### 4·2 会被碰到的断言（**逐条**；`⛔` 的两份宿主不在我白名单里，**要调度台改**）

**A. `Editor/DeckScene.cs` —— 我已改（新增，不改既有语义）**
- `:722-801` 新增一整节「A1697 那条链」：不入库 / 交接 / 空牌 / **Done 不合法 ⇒ 不入库** + 弹错误窗（点右钮收掉）/
  **中途退出不留痕** / **合法 ⇒ 入库** / **再按一次 Done 不重复入库（判别式）**。
- `:5927-5937` 新增菜单版断言（见 §三·2）。
- ⚠️ **风险**：这一节在 `try` 里**最早**调 `WindowsManager.EnsureHost()`（原先是 A364 那一节最早）。
  `EnsureHost` 幂等、`DestroyPopupScaffold` 收尾会拆掉 ⇒ 预期无影响，但**这是本轮唯一「执行顺序变了」的地方**，
  跑红时先看它。

**B. `Editor/CollectionScene.cs`（⛔ 我不许碰）—— 预计 3 处红（4 条断言）**

| 行 | 原断言 | 现在为什么红 | 建议改法 |
|---|---|---|---|
| `:1358-1360` | 「点 `Create` ⇒ 卡组数 +1」 | 库里**不再** +1（照原版）| 改成三条：库数**不变** / `CollectionData.PendingNewDeck != null` / `PendingEditDeck == -1`；读完清掉 `PendingNewDeck`（别污染后面的节）|
| `:8083-8088` | A503 ②：「写盘失败 ⇒ `CreateDeck` 回空串」+「内存里建了 1 套」 | 本口**一个字节都不写盘** ⇒ 没有「写盘失败」这一支可判；库数也不变 | 整段搬到**「Done 那一拍」**：`DeckStore.OverridePath` 指坏路径 ⇒ 编辑器按 `Done` ⇒ 断 `DeckRuntime.UiLastSay` 说「保存失败」且库里那份**没落盘**（`LastError` 有串）|
| `:8303-8305` | 「先建一套临时卡组当靶子」= `CreateDeck` + `IndexOf` | `IndexOf(name)` 恒 −1（不入库）⇒ `DeckInfoPopup.Create(win.Manager, -1)` 后面必红 | 换成 `CollectionData.DuplicateDeck(0)`（**那个口仍然入库**，语义与「造一套临时卡组」等价）|

⚠️ **`:8063-8065` 控制组那条（`okCreate` 非空）仍会绿**（本口照旧回名字）；但 `:8072-8074`
「删最后一套」那一步删的会是 `ImportDeck` 造的那套（`CreateDeck` 不再加）—— 逻辑自洽，**人工确认一眼**。

**C. `Editor/MainMenuScene.cs:6006-6079`（⛔ 我不许碰）—— 预计 4 处红**

| 行 | 原断言/夹具 | 现状 |
|---|---|---|
| `:6011` | `Check(DeckCount(), before + 1, "点 `Create deck` ⇒ 卡组库里多一套")` | **反了** ⇒ 改成「库数不变」 |
| `:6012-6015` | 从库里最后一套读 `GameMode` | 那一套不在库里 ⇒ 改读 `CollectionData.PendingNewDeck` |
| `:6016-6017` | `PendingEditDeck == DeckCount()-1` | 改成 `PendingEditDeck == -1` + `PendingNewDeck != null` |
| `:6018-6019` | `CurrentIndex() == DeckCount()-1`（「新卡组即选中」）| **不成立**（没入库就没有 `_current`）⇒ 删 |
| `:6072-6079` | 夹具「给新那套补个督军」= `libM.Decks[libM.Count-1]` | 那一套不在库里 ⇒ **建议夹具直接造**：`DeckLibrary.Load().Create(name, (int)GameMode.Skirmish)`（那一节真正要验的是「遭遇窗只收同模式卡组」，与新建那条路无关）|

`Editor/ShellScene.cs`：**无影响**（它自己 `CardDetailPopup.Create(...)`，我没改那个文件）。

**D. `RuleEngineTest.Run`**：无影响（`DeckRulesTest` 用的是 `DeckLibrary.Create`，那个口没动）。

## 五、没查清的部分（如实）

1. **`Insert` 落盘失败之后**原版说什么 —— `UploadUnsyncedDeck` 的回调/失败支**没读**（网络链）。我们这一侧
   现在是「内存里有 + 页脚说保存失败 + 下次 `Done` 再试」，**这是我们挑的**（判据只有 `Save()` 的返回值）。
2. **真机鼠标那一路没验**：`UiClickDeckRow` 走的虽是生产同一段代码，但「点遮罩关窗」在真机上要靠
   `PointerLayer` 派发到 shell 那个压暗层 —— **本笔只静态核到「宿主与 A364 那扇模态窗是同一条路」**（能跑起来的是那一扇），
   **没有跑真机**（`工具/wf_click.py` 那一步留给调度台/用户）。
3. **`CardDisplayWindow__ShowCard.c:159` 守卫里 `card.field_0x4c != 0` 那一项是什么字段** —— 仍然没查
   （R4 §六·2 也记着这条）。若它常非 0，原版那一刻 `options` 也不出；**本笔按 `== 0` 能过**（卡组编辑开出来确实有右侧那一列，
   由新断言观测）。
4. **`_newDeck` 与 `State.Deck` 的对象身份差**（§二·3 那条如实标注）：**观测不到**，但与原版逐句比，多了一处「不拷回」。
   若将来有人按身份引用 `PendingNewDeck`，这条要重看。

## 六、顺手发现（只报不改）

1. 🔴 **`DeckEditorState.NewDeck(name, gameMode)`**（`Deck/DeckEditorState.cs:131`）—— 全仓**没有生产调用点**
   （只有定义）。`A1697` 之后它是「造不入库的卡组」的**第二份实现**（第一份 = `DeckLibrary.NewUnsaved`）——
   ⚠️ **两份写同一条规则，将来会漂**（铁律 6）。本笔**没动它**（`Deck/DeckEditorState.cs` 不在白名单里），
   建议调度台定一个方向（合并 / 或就地写清「它是给谁用的」）。
2. 🔴 **`Shell/CollectionWindow.cs` 里那颗 `Create` 的日志**（`[Collection] 新建卡组「…」`）与
   `Shell/LiveOpsEventWindow.cs` 的 `[Event] 新建卡组「…」` 现在**语义相同、措辞不同**（两条来路各一套）——
   本来就是这样，只是 `A1697` 之后两边都变成「不入库」，**更容易被误读成两件事**。
3. ✅ **核过一条「看着像缺陷、其实不是」的**：`CollectionData.SaveFailReason()` 的 doc 头写着
   「**上面四个口共用这一句**」—— `A1697` 把 `CreateDeck` 那一处调用点删掉之后，现读**恰好还是四个**
   （`:161 Select` · `:215 DeleteDeck` · `:255 DuplicateDeck` · `:285 ImportDeck`）⇒ **那句话仍然成立**，
   ⛔ 不用改（本笔原本怀疑它过期，现核**不成立**，如实记在这里）。
4. ⚠️ **`DeckScene.cs:5927-5937` 那条新断言依赖 `CardDetailPopup` 的三个节点名**
   （`Card Counter` / `Wildcard Count 0` / `AltArt Title`）—— **那个文件另有写手**（正在做 `A1699` 异画钮）。
   若他们把节点改名，这条会红。**这是只读依赖**（我只读节点名，没改那个文件）。

## 七、改动清单

| 文件 | 现读行号（关键处）| 行尾 | `git diff --numstat` |
|---|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Data/DeckLibrary.cs` | `:187 NewUnsaved` · `:206 Insert` | CRLF | `42 0` |
| `Unity/MyGame/Assets/CardPresentation/Shell/CollectionData.cs` | `:320 CreateDeck` · `:397 PendingNewDeck` · `:400 TakePendingNewDeck` | LF | `56 14` |
| `Unity/MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `:3534 GoNewDeck` · `:3561 CreateDeck` | LF | `50 13` |
| `Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEventWindow.cs` | `:1119-1146 CreateDeckInMode` | LF | `15 26` |
| `Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | `:990 Build 交接口` · `:4499 RollBack` · `:4801 TryImport` · `:5695/:5699/:5713/:5731 四个桩` · `:6937 CommitDeck` · `:6988 OpenCardWindow` | CRLF | `147 13` |
| `Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` | `:722-801 新节` · `:5927-5937 新断言` | CRLF | `92 0` |

- **行尾复核**：6 个文件逐个二进制数过，CRLF 仍是 `CRLF==LF`、LF 仍是 `CRLF==0` ⇒ **没有翻行尾**。
- **秒级类型检查**：`TMPDIR=/tmp/wf_jia bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完每步都跑过，最后一次在**全部改完之后**）。
- ⛔ **未跑** Unity 批处理；⛔ **未动** git；⛔ **未改** `CLAUDE.md` / `项目任务.md` / `资料/**.md`（本报告除外）。

### 白名单偏离（1 处）

| 文件 | 为什么必须改 | 改动形状 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEventWindow.cs` | 它是**第二个**走同一个口（`CollectionData.CreateDeck`）的入口（判据 → `R1_没选战将的卡组.md` §二·1）。若不改：`created` 仍为真、`IndexOf(name)` 恒 −1 ⇒ `Select(-1)` 失败 ⇒ `PendingEditDeck = -1` ⇒ 编辑器**打开库里「当前选中」那一套（别人的卡组）**—— **静默开错牌**，正是红线要挡的 | 删掉 24 行「下标交接」块、换成 12 行（`PendingEditDeck = -1` + 一条警告 + 日志）。**没有改任何签名、没有加形参、没有动别的语义** |
