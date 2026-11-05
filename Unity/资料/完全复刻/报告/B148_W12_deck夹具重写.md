# B148 · W12 — `DeckScene` 演示卡组夹具重写

> 执行写手：W12 · 2026-11-05 · 批次 `B148`
> 白名单：只改 `CardPresentation/Editor/DeckScene.cs`（**未动** `Deck/DeckRuntime.cs`，理由见 §五）。
> ⛔ **本报告不含「已通过」**：Unity 宿主由调度台跑，这里只有静态验收（typecheck + 账闸）。

---

## 一、日志取证（`D:/4/_tmp_view/deck.log`，只 grep `✗` / `Exception`）

一次性读数：`deck.log:716107` = **1254/1366 通过 · 112 条失败**，并且 `deck.log:716091` 记着
`RunCore 抛异常：System.ArgumentOutOfRangeException … List.get_Item … DeckScene.TestLayout … DeckScene.cs:6902`
⇒ **`TestLayout` 之后的所有断言段一条都没跑**（合计不完整）。

**夹具到底该长什么样 —— 从 `✗` 行反推（逐条带日志行号）：**

| 日志行 | 断言要的东西 | 当时实得 | 结论 |
|---|---|---|---|
| `:698690` | ★ W62「但编辑器里**有东西可编辑**（演示牌只当尚未入库的内存载体）」 | `0 张 / 督军 <无>` | **空库那一拍 `State.Deck` 必须非空** |
| `:698722` | ★ W62「而且是**合法满编**那一副」 | `0/30` | 必须**恰好 30 张** |
| `:698768` | （前提）开场那副演示牌**合法** ⇒ 按 `Done` 会放行 | `NoWarlord` | 必须有**督军**（`DeckRules.Validate` ②） |
| `:698896` | 盘上那套 = 刚按 `Done` 存下去那副（名字一致） | 期望 `[]` / 实得 `复仇者之刃` | 期望值是**空**卡组名 ⇒ 夹具链断在 `Done` |
| `:697044` | `B103A03` D4-前提③ 滚动真的动了 | `DeckScrollPx == 0` | 卡组行数要 **> 12**（一屏） |
| `:697196` `:698227` | `B108A3` A0/D0（前提）卡组里有**普通卡行** | False | 要有非督军卡 |
| `:697840` | `B108A3` B10（前提）有一格费用 **1..9 张** | False | 费用分布要「每档不超过 9 张」 |
| `:697180` `:698516` `:698533` | `B108A3` A1/G3/G4：督军行 `Middle(512)` · 名字 `auto[10~34]` | False / 实测 `38`、`2` | 有督军时才走 Hero 族样式（`2/38` 是 Card Info 族那套） |
| `:706835` `:706851` | ★ 整副牌里至少一行「**费用 ≠ 份数**」· 份数 > 1 的行印 `x`+份数 | 0 行 | 要有**重复卡**（同名多张） |
| `:707094` | （前提）列表第 0 行 = 督军 | False | 第 0 行必须是督军行 |

⇒ **夹具形态 = 一副「尚未入库」的合法满编 30 张：有督军、有防御卡、全是同阵营单位、含重复卡、费用跨度 1..6。**
**没有任何一条断言要求那一拍是 0 张**（`W62` 那两句括号里的 `0 张 / 督军 <无>` 是**实得值**，不是期望值）。

---

## 二、根因

1. `2026-11-04（B141）` 照原版删掉了「生产入口替你建演示卡组」：
   `Deck/DeckRuntime.cs:1342-1356` —— 空库那一拍改走 `CardDeck__GetEmptyDeck` 的**真·空卡组**（0 张 / 无督军 / 名字空）。
   生产侧这是**对的**（判据 `SelectDecksTab__CreateDeck.c:17,26,28`）。
2. 但 `DeckScene` 的自检夹具**一直白拿**那个副作用：`W62` 那 5 条 + `TestLibraryAfterDone` 的「前提：开场那副牌合法」都建立在
   「`Build` 之后 `State.Deck` 是满编演示牌」上。副作用一删，**夹具没了**。
3. 于是 `Done` 被 `NoWarlord` 挡（`DeckRuntime.cs:5544-5549`）⇒ 库里 0 套 ⇒ 下游一大片「（前提）」红；
   最后 `TestLayout` 在 `state.Deck.CardIds[0]`（原 `:6902`）**越界**，把 `RunCore` 打断。

---

## 三、改了什么（逐段：夹具现在给什么 / 断言要什么 / 依据）

### 改动 ①（真修复）`DeckScene.cs:840` —— `Build` 之前显式塞一副满编

```csharp
{ var demoSeed = NewState();
  CardPresentation.CollectionData.PendingNewDeck = DeckRuntime.PlayerDeckForDemo(demoSeed);
  CardPresentation.CollectionData.PendingEditDeck = -1; }
state = Build(DeckLibrary.Load(), out _root); …
```

- **走的是生产既有的「新建卡组」交接口**（`CardPresentation/Shell/CollectionData.cs:407` 的 `PendingNewDeck` →
  `DeckRuntime.Build` 里 `CollectionData.TakePendingNewDeck()`，`DeckRuntime.cs:1313-1320` → `:1365 State.LoadDeck(...)`），
  也就是原版 `GetEmptyDeck` 那个位置：**尚未入库** ⇒ `_newDeck != null` ⇒ **不建库、不写盘**。
  （`PendingEditDeck = -1` 是两个交接口的互斥防呆，`CollectionData.cs:405-406` 明确要求。）
- **内容 = `DeckRuntime.PlayerDeckForDemo`**（`Deck/DeckRuntime.cs:8792`）—— `B141` 删的只是「生产入口替你建」，
  本体按注释明写保留给 `Editor/*` 夹具用。没有新写第二份造牌逻辑。
- **实测这副牌（按 `cards_engine.json` 逐条静态模拟 `PlayerDeckForDemo` 的算法）**：

  | 项 | 值 |
  |---|---|
  | 督军 | `ASH1` *Anvirr Keltoc*（SaimHann） |
  | 防御卡 | `ASH56` *Inspiring Legacy*（cost 0，同阵营里 cost 最低 → id 最小） |
  | 普通卡 | **30 张**（17 张不同卡，同名按 `CopyLimit`：传说 1 / 其余 2） |
  | 费用直方图 | `1→6 · 2→2 · 3→4 · 4→5 · 5→5 · 6→8` |
  | 传说张数 | 3（经典无上限，`LegendaryTotalLimit(Classic)=int.MaxValue`） |
  | `DeckRules.Validate` | **None**（逐条：id 全命中 / 督军是 hero / 防御卡是 defence / 全部同 `deckArmy` / 无 hero·defence 混入 / 无 `NotDeckable` / 无 `CantAddToDeck` / 份数 ≤ 上限 / 张数 == 30） |

  ⇒ 正好覆盖 §一 那张表要的全部形态（有督军、有普通行、有重复卡、费用跨度、行数 30+2 > 一屏 12）。
- `Build` 那一步**不会**摘掉任何一张：`DeckEditorState.LoadDeck` 里的 `DeckRules.RemoveInvalidCards`
  （`Deck/DeckEditorState.cs:323`）按 `deckArmy` 逐张判 —— 30 张全同阵营 ⇒ `LastLoadRemoved` 空。
- **为什么不用别的写法**：直接改 `DeckRuntime.Build` 加回演示牌 = 把 `B141` 刚按原版删掉的副作用又装回去（⛔ 铁律 18·0）；
  直接 `_rt.State.Deck.CardIds.AddRange(...)` = 改完还得重建视图，且「尚未入库」这个语义就假了。

### 改动 ②`DeckScene.cs:2943-2948` —— 兜底**挪到断言之后**（原来它掩盖了问题）

- 现在：`int libAfterDone = _rt.Library.Count; Check(libAfterDone, 1, "★ W62：按 Done ⇒ 这时才入库 …");`
  **先断**；`if (libAfterDone == 0)` 才走兜底（`Library.Insert(PlayerDeckForDemo(...))` + `Save()`），
  并把日志从 `Debug.Log` 改成 `Debug.LogWarning`、文案写明「这不是修复」。
- **依据**：原写法是「先 `if (Library.Count == 0) 插一条`，再 `Check(Count, 1)`」⇒ `Done` 这条路真坏了也照样绿
  （今天就是被它掩盖成 `:698896` 那条「名字不一致」）。断言与兜底**必须成对且有序**。
- 兜底**保留**（防回归）：它只保证「后面各节仍有夹具」，⛔ 不再承担「让断言变绿」。

### 改动 ③`DeckScene.cs:6902/6903/6910` —— 越界那一处

```csharp
string back0 = state.Deck.CardIds.Count > 0 ? state.Deck.CardIds[0] : null;
CheckTrue(back0 != null, "（前提）A365 反向那一块要「摘一张再补回」⇒ 卡组里得有牌…");
if (back0 != null) state.Deck.CardIds.RemoveAt(0);   // 30 → 29 ⇒ 不合法
…
if (back0 != null) state.Deck.CardIds.Insert(0, back0);
```

- **依据**：`deck.log:716091` 的栈顶就是这一行（`List.get_Item`）。**数据保证**由改动 ①给（30 张）；
  **前提断言**保证「将来夹具再退化」时是**一条红**而不是**整段静默不跑**（⛔ 不是静默跳过 —— 前提不成立时
  下面那两条 `TooFewCards` / 「不补名」会连带红，不会假绿）。

### 保留的护栏（调度台临时加的，按简报保留）

- `TestLayout` 开头 `state == null || state.Deck == null` 前提闸（`DeckScene.cs:2995-2999`）；
- `:6894` 的 `wl0` null 安全。
两者现在都是**防回归**，不再是遮丑布（夹具已给真数据）。

---

## 四、静态验收（本代理只做到这一步）

| 项 | 命令 | 结果 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_w12 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 账闸（只读） | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | A 5983 条 · 漂移 4 · 失败 1 —— **全部在 `Battle/SettingsPanel.cs`，`DeckScene.cs` 0 漂移 0 失败** |
| 行数守恒 | `DeckScene.cs` = **10602 行**（改前改后一致）· CRLF 10602 / 裸 CR 0 | 所有改动都写在**已有物理行**上 |

> ⚠️ 过程中踩了一次本工程的老坑并当场修掉：第一版把说明写成 `//` 注释、`state = Build(...)` 落在了注释**后面**
> ⇒ `CS0165 使用了未赋值的局部变量 state`（typecheck 当场抓到）。现在那段说明是 `/* … */` 块注释，代码在注释**之前**。

---

## 五、没动 `DeckRuntime.cs` 的理由

`PlayerDeckForDemo` 本体完好（`DeckRuntime.cs:8792`，`B141` 只在生产入口那一处停用，注释明写「Editor/* 的夹具还在用它」）；
`Build` 的「空库 ⇒ `GetEmptyDeck`」是真·原版行为（`DeckRuntime.cs:1350-1356`），**不该为自检开一条后门**。
⇒ 夹具在 **Editor 侧**用**既有交接口**解决，生产代码一行未改。

---

## 六、下次跑 Unity 应该看到什么

**应当消失的（今天都是被夹具带红的）：**
- `★ W62：……但编辑器里有东西可编辑` / `……而且是合法满编` / `（前提）开场那副演示牌合法` / `盘上那套就是刚按 Done 存下去的这一副`（`deck.log:698690/698722/698768/698896`）；
- 全部 `（前提）` 型红：卡组行数 / 督军行 / 普通卡行 / 费用档 / 份数 > 1 的行（`deck.log:701170`、`:706787-707334`、`:707094`、`:713276` 等约 40 条）；
- `B103A03 D4-前提③ DeckScrollPx == 0`（`:697044`）—— 30+2 行 ⇒ 滚得动；
- `B108A3` 的 `A0/A1/B10/D0/G3/G4`（`:697180/697196/697840/698227/698516/698533`）—— 督军行回来之后按 Hero 族样式（`auto[10~34]`）；
- `AUD02 卡组编辑 85 条` 那条汇总（`:698583` 失败 7 → 预计降到 1 或 0）。

**预计仍然红、且与本夹具无关（留给调度台分流，我没动）：**
- `:709260` `B141_W53 G8 对照标签仍是出厂面 Cjk`（`MenuDraw.Text` 缺省字体面 = `LatinCardBody`）—— 字体线；
- `:709917` `B141W74 E3 ★★ 清空后重新加 B 的牌 ⇒ 锁成 B（闩里实得 null）`—— 阵营闩（有督军后行为可能变化，**不确定**）；
- `:698566` `B108A3 G9 卡背 Army Filter/Title 的 m_margin = 22.7（实测 0.21）`—— 卡背筛选标题边距，与卡组内容无关，**看起来是真缺陷**。

**期望的整体数字**：`RunCore` **跑到底**（不再抛异常）；总断言数 **> 1366**（`TestLayout` 之后那几段终于会跑）；
失败数 **< 112**（预计落在 3~5 条，即上面那三条 + 两条不确定项）。
⛔ 这不是「已通过」—— 以调度台那次实跑为准；若 `TestLayout` 之后暴露出**新**红，那是今天第一次跑到那里，属新账。

---

## 七、顺手发现（不在本笔白名单 / 由调度台分流）

1. `DeckScene.cs:2938-2948` 原来的「兜底在前、断言在后」是一种**通用反模式**（本项目别处可能也有同形）：
   凡是「先修补再断言」，都会把「被修补的那个缺陷」验成绿的。建议按本条口径抽查同族写法。
2. `W62` 那句断言的**文案**容易误读：括号里印的是**实得值**（`0 张 / 督军 <无>`），不是期望值 ——
   建议将来改成「实得 ……」前缀（本笔不改，避免动断言语义）。
3. `TestLibraryAfterDone` 的②③两段（重开不写盘 / 落盘往返）依赖「库里恰好 1 套」；改动 ①之后这条前提是**真的**成立，
   但若将来有人再让 `Build` 建库，这里会连锁红 —— 已有 `W62` 三条钉着，暂不重复。
