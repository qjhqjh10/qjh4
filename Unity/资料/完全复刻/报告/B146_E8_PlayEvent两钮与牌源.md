# B146-E8 · PlayEvent 两钮与牌源（2026-11-05）

## 一、四步各自改了什么（文件:行 + 摘录）

### ① 新建 `Shell/PlayEventDeckSource.cs`（67 行，类名 = 文件名；新增同名 `.cs.meta`）
`CardPresentation/Shell/PlayEventDeckSource.cs`
- `:26` `public int Count { get { return CollectionData.DeckCount(); } }`
- `:32` `IdAt(i)`：先 `CollectionData.DeckIdAt(i)`；为空（旧档没发过 id）⇒ `EnsureDeckId(i)`（现发 + 落盘）。
- `:40` `ArmyAt(i)`：`ArmyIcons.Id(CollectionData.DeckAt(i).Faction)` → `CardArmy`；`<=0` ⇒ `Neutral`。
- `:47` `GameModeAt(i)`：`(PlayModes)deck.GameMode`（两枚举同值，判据 `dump.cs:46188` / `GameplayVariables.cs:36`）。
- `:56` `IsPlayableAt(i)`：`DeckRules.Validate(deck, CardDatabase.DeckLookup(CardDatabase.Load(), faction), deck.GameMode == (int)PlayModes.Skirmish) == DeckError.None`。
- `:65` `IndexOfId(id) => CollectionData.IndexOfDeckId(id)`。
- 全成员**现读**不缓存 ⇒ `CollectionData.InvalidateLibrary()` 之后自动看到新库。

### ② 注入（`Shell/CollectionData.cs:686-689`，类尾追加）
```csharp
static CollectionData() { PlayEventDecks.Source = new PlayEventDeckSource(); }
```

### ③ 两钮弹窗（`Shell/PlayEvent.cs:552-561`）
`StartMatch()` 的 `HasDefensiveCards == HaveItInInventory` 支由 `PopupError(...)` 改为：
```csharp
PopupConfirm("MenuDeck/Error/HasNotAssignedDefensiveCard",
             () => StartMatchNoCheck(deckId),   // = 原版 b__0
             () => CloseCurrentWindow());       // = 原版 b__13_1
```
新增（`PlayEvent.cs` **文件末 partial 段**，`:1103-1165`；该类本来就是 `partial` ⇒ 上方行号零漂移）：
- `:1125` `public static Action<string, Action, Action> OnConfirmPopup;`
- `:1134` `protected void PopupConfirm(...)`：① 有注入口用它；② 否则 `WindowsManager.Instance.ShowPopUp(Loc.T(key), OK, onOk, Cancel, onCancel)`；③ 都没有 ⇒ `LogWarning` 且**不开打**（⛔ 不静默）。
- `:1153` `protected virtual void CloseCurrentWindow()`：`wm.currentWindow.Close()`（`WindowsManager.Instance` 为 null/无当前窗 ⇒ 出声）。做成 `virtual` 只为自检能观测。
- `InvalidDeck` 那一支**保持单钮** `PopupError`（原版那一条是单钮弹窗）。

### ④ 断言（`RuleEngine/Editor/B142_E62_Assert.cs`，B3 组 175-249 行）
见 §三。

## 二、注入点选择理由

- 全仓 `CollectionData` 的构造/初始化点**只有它自己**（`static DeckLibrary _lib` 懒加载，`Lib` 属性 `CollectionData.cs:66`）；没有别的「壳初始化单例」持有卡组库。
- 原版对位 = `InventoryManager.GetInventory<CardDeck>()` 单例 ⇒ 卡组库宿主初始化 = 最接近的同一拍。
- 时序：`MainMenuRuntime.Build()` **无条件**先调 `CollectionData.TakePendingReturn()`（`:608`）⇒ 壳一建好注入就完成，早于任何事件窗。构造**不读盘**（只有成员调用才读）⇒ 注入本身零 I/O。
- 生产侧**不需要**改 `MainMenuRuntime.cs`（不在白名单，也不需要）：`PopupConfirm` 自己走 `WindowsManager.Instance`，`OnConfirmPopup` 只给自检/无窗口层宿主。

## 三、断言 B3 改动

`B142_E62_Assert.cs:181-188` 同时捕获 `OnErrorPopup` 与新增 `OnConfirmPopup`（`finally` 两处都还原，`:252-255`）。
- `B3-1 / B3-1b / B3-1c`（`:191-199`）：`DontHave` ⇒ 直接开打、**两个弹窗口都不走**。
- `B3-2` 重写（`:200-218`）：先不开打；`B3-2b` 两钮键逐字；`B3-2c` **不再走单钮 `OnErrorPopup`**；`B3-2d` 两回调**都非 null**；**实调 `onOk`** ⇒ `B3-2e Started`、`B3-2f LastDeckId == "B142E62_fake_deck"`、`B3-2g` 不关窗。
- 新增 `B3-2h~j`（`:219-230`）：新实例、**实调 `onCancel`** ⇒ 不开打、`CloseCalls == 1`（对应 `b__13_1` = `WindowsManager.CloseWindow(CurrentWindow)`）。
- `B3-3`（`HaveItAssigned` 直接开打）· `B3-4`（`InvalidDeck` 优先、单钮）**原样保留** —— 没有放宽任何一条。
- 夹具 `B142E62FakeEvent` 增 `LastDeckId` / `CloseCalls`，并 `override CloseCurrentWindow()`（`:396-405`）。

## 四、typecheck

`TMPDIR=/tmp/wf_e8 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（共跑 3 次，最后一次在全部改完之后）。
未跑 Unity（按纪律）。行尾：四个文件**全 LF、0 个 CR**（二进制读取数出来）。

## 五、顺手发现

1. **（已改，属本链闭合）`PlayEventDefensiveCards.Check` 原来按【名字】认卡组** —— `CollectionData.IndexOf(deckId)`。而 `GetEventDeck()` 现在返回的是 **id** ⇒ 不改就会「有防御卡也判 DontHave」，两钮弹窗永远不触发。已改成 **id 优先 + 名字回落**（`PlayEvent.cs:1081`，同一物理行 ⇒ 零行数漂移）。
2. **（已改）四处过期注释**「我们的卡组唯一 id = `CollectionData.DeckInfo.Name`」（`PlayEvent.cs:30 / :113 / :406 / :915` + `:1078` 的 `PlayerDeck.Name`）—— 与 `CollectionData.cs:181`「同名会选错」直接矛盾，按铁律 5 就地改（全为同一行内改写，行数不变）。
3. **B 报告的施工口径有一处不成立**：它写 `IsPlayableAt` 用 `id=>CardDatabase.Get(id)` —— **`CardDatabase.Get` 全仓不存在**。实际用 `CardDatabase.DeckLookup(CardDatabase.Load(), faction)`（唯一的卡引用解析口：先稳定 id、再旧存档的卡名）。
4. **`EnsureDeckId` 会写盘**：旧档里没有 `DeckId` 的卡组，被枚举一次就会补发 id 并 `Save()`。原版 `CardDeck.deckId` 由构造必写、不存在这一档，所以这是等价物；但它是**副作用**，谁在自检里全表枚举要留意（本轮断言不枚举）。
5. **`PlayEventDecks.Source` 是裸字段、没有惰性兜底**：注入靠 `CollectionData` 静态构造。若将来出现「先问 PlayEvent、后碰卡组」的入口，会拿到 null 并出声（不会静默）。今天 `MainMenuRuntime.Build` 第一跳就碰 `CollectionData` ⇒ 壳内无此风险。
6. **`PlayEvent<,>` 今天仍无生产实例调用 `StartMatch`**（`grep '\.StartMatch('` 命中的都是 `LiveOpsEventWindow.StartMatch`）⇒ 本轮接线是**为将来的事件-Battle 钮铺路**，不是「线上立刻可见的行为改变」。`DraftLocalEvent.cs:285 new Body()` 不改（它的 Battle 走活动窗那条链，不在本笔范围）。
7. `OnConfirmPopup` 是泛型类的 static ⇒ 按闭合类型各一份（与 `OnErrorPopup` 同坑）。生产不依赖它（走 `WindowsManager.Instance`），但自检/将来的注入方要按闭合类型各接一次。
