# B117-WSHELL · `ShellScene.Run` 两条红的定性与修复

日期 2026-11-02 / 执行代理 `B117-WSHELL` / 独占文件 `Assets/CardPresentation/Editor/ShellScene.cs`

现读基线（2026-11-02 12:11 实跑，日志 `d:/4/_tmp_view/shell.log:1262065`）：`[Shell] === 合计：6091 通过 / 2 失败 ===`

```
失败 1：（前提）排位窗有可看的卡组（`DeckIndex >= 0` || 预选预组，实得 `DeckIndex = -1`）
失败 2：★ `ViewDeckButtonClick()` ⇒ 开 `DeckInfoPopup`（= 原版 `OpenWindow<DeckInfoPopup>`）
```

## 一、定性：α（断言前提本身不成立），不是 β、也不是 γ

### 1. 两条红是同一个根因

`ViewDeckButtonClick()` -> `LiveOpsEventWindow.OpenDeckInfo()`，首句是**空态闸**：

- `Shell/LiveOpsEventWindow.cs:1948-1958`：`LastDeckInfo = null;` -> `Manager == null` -> **`if (PickedPrebuilt == null && DeckIndex < 0) { 出声; return null; }`**

自检日志直接印证（`_tmp_view/shell.log:1184285`）：

```
[Event] 空态（没有本窗模式、本阵营的卡组）=> `View Deck Button` 无牌可看，不开窗
```

⇒ 第 1 条红的 `DeckIndex = -1` 是**同一个成因**；第 2 条红只是它的下游（闸按设计拒开窗）。

### 2. `DeckIndex = -1` 的来源：`Open()` 读的是「本阵营 ∧ 本窗模式」那张表，表空就给 −1

- `Shell/LiveOpsEventWindow.cs:556-563`（`Open()`）：`int cur = EventDeckIndex();` -> `_armyFactionSel` = **当前卡组督军的阵营**（当前卡组为空才回落 `Ultramarines`）-> `DeckIndex = FirstDeckInMode(cur);`
- `:370-383`（`FirstDeckInArmy`）：`n <= 0` 或表里 0 命中 => **返回 −1**（注释写明「表空 ⇒ −1 = 空态，禁止回落到别的模式/阵营」）
- `:263`：`RankedEventWindow` 用基类默认 `DeckGameMode = Classic(0)`
- 成员判据：`:347-354`（`InArmyDeckList` = 模式相同 ∧ 阵营相同）

### 3. 为什么现在是 −1：夹具直接吃玩家的真存档，而真存档今天变了

这一段的夹具**没有** `DeckStore.OverridePath`，所以 `CollectionData` 读的是
`Application.persistentDataPath/WarpforgeDecks.json`（`RuleEngine/Data/DeckStore.cs:28-32`）。

真存档 `C:/Users/qjh36/AppData/LocalLow/QJHWarpforge/WarpforgeReplica/WarpforgeDecks.json`
（mtime **2026-11-02 08:01**；中央档 `WarpforgeSave/PlayerDecks.json` 同一时刻、同一内容）现读：

```
current = 1
decks[0] 复仇者之刃  WarlordId ASH1  GameMode 0   (经典)
decks[1] 舰队之主    WarlordId UM47  GameMode 13  (遭遇/Skirmish)
```

同一次真机的日志印证（`.../WarpforgeReplica/Player.log:37949`）：

```
[Event] 阵营筛选：Ultramarines（该阵营 ∧ 本窗模式 1 套，选中第 1 套「舰队之主」）
```

⇒ `current = 1` => 选中阵营 = Ultramarines；排位窗模式 = 经典 ⇒ **「Ultramarines ∧ 经典」表为空**
⇒ `DeckIndex = -1` ⇒ 两条红。**入口接线本身没有坏**（同一段里 `ChangeDeckButtonClick` 那条是绿的，
日志 `:1184270`）。

### 4. 排除 β（实现缺陷）：原版在空态就是「不开窗」，那颗钮根本点不到

第一手反编译（`d:/2/tools/decomp_full/`）：

- `RankedDeckSelector__OnViewDeckButtonClick.c` 首句：
  `cVar2 = CardDeck__op_Equality(*(undefined8 *)(param_1 + 0x98),0,0); if (cVar2 == '\0') { ... OpenWindow<DeckInfoPopup> ... }`
  ⇒ **当前卡组(null) 时直接 return，不开窗**（我们的 `OpenDeckInfo` 空态闸逐条同义）。
- `RankedDeckSelector__ChangeArmy.c:109-117`：四颗圆钮按 **表.Count** 开关
  （`SetActive(1 < Count)` / `SetActive(0 < Count)`）⇒ 空表时 `viewDeckButton` 是**隐藏**的。
- `RankedDeckSelector__Initialize.c:15-22`：`currentArmy` = 当前卡组的 Army，没有卡组才 `10`(Ultramarines)。

⇒ 空态拒开窗 = 原版语义；把它「修好」（恒开窗）才是与原版不符。

### 5. 排除 γ（本波回归）：这条链的代码自第 111 会话起没动过

```
git log -3 -- Shell/CollectionData.cs RuleEngine/Data/DeckLibrary.cs RuleEngine/Data/DeckStore.cs Shell/RankedEventWindow.cs
=> 246eba25 (第 111 会话，当时 24/24 全绿) 之前
git log -5 -- Shell/LiveOpsEventWindow.cs
=> 最新 972983a1（第 109 会话）
```

`git log -8 -- Editor/ShellScene.cs` 显示本段自 `6877d563`（第 93 会话）起就没改过。
⇒ 变的只有**存档数据**（真机 08:01 那次把当前卡组切成了遭遇战那一套），不是代码。
第 116 会话 04:03 收口时是 `5,941 / 0`，早于 08:01 这次真机改写（无备份可证当时 `current` 的取值，
按机制推是 `0` = 经典那套 ⇒ 表非空 ⇒ 前提成立）。

## 二、修法（`Editor/ShellScene.cs` · 只改夹具与断言，不动实现）

`B93_D1` 第 ② 段（新行号 `:10993-11071`）改成**自造卡组库**夹具，照本文件既有套路
（`:5995-6568` / `:11033-11067`：`DeckStore.OverridePath` 指临时文件 + `CollectionData.ResetForTest()`）：

1. `DeckStore.OverridePath = "d:/4/_tmp_view/shell/_b93d1_decks.json"` -> 删文件 -> `DeckLibrary.Load()`
   -> `Create("B93D1夹具·经典", GameMode.Classic)` + `WarlordId = "ASH1"`（SaimHann 督军；
   0 张卡 = 故意不合法，`FirstDeckInArmy` 只把合法性当**排序键**、不过滤）-> `Save()` -> `ResetForTest()`
   ⇒ `Open()` 的「选中阵营」= SaimHann、模式 = 经典 ⇒ 表里就是这一套（`DeckIndex = 0`）。
2. 新增前提断言：`DeckIndex == 0` 且 `CollectionData.Raw(0).Name == 夹具名`
   （证明选中的**是夹具那一套**，不是真档里碰巧剩下的）。
3. `finally` 里 `DeleteFile()` -> 还原 `OverridePath` -> `ResetForTest()`
   （临时库作废，后面的段落照旧读它们自己的库；玩家真档一个字节没碰）。
4. **灭自证（正向）**：`LastDeckInfo.CurDeck.Name == 夹具名` —— 随便开一扇空窗不算数。
5. **灭自证（反向，结构上不可能与正向同时满足）**：把临时库删空再开一扇排位窗，
   断言 `DeckIndex < 0` 且 `ViewDeckButtonClick()` 之后 `LastDeckInfo == null`。
   ⇒ 把空态闸删掉/改成恒开 => 反向那条红，而正向四条照样绿；把入口改成空体 =>
   正向四条红、反向照样绿。两半合起来才钉死「按空态开 / 不开」。

### 为什么不改成「放宽前提」或「恒开窗」
- 放宽 / 跳过 = 空态闸与 `ViewDeckButtonClick` 那条链**没有覆盖**，且违反「禁止 `if (前提) Check(结论)` 静默跳过」。
- 恒开窗 = 与 `OnViewDeckButtonClick.c` 首句和 `ChangeArmy.c:109-117` 相反（改动会落在 `Shell/` 属别人名下）。

## 三、验证

- `TMPDIR=/tmp/wf_wshell bash d:/4/Unity/工具/typecheck.sh` => 运行时 0 / 编辑器 0。
- **没有跑 Unity**（子代理禁止 `-executeMethod`；且本波要求不占全量自检的窗口）⇒
  `ShellScene.Run` 的实跑复验留给调度台同步点。

## 四、影响面与账

- `git diff --numstat`：`+76 / −25`（净 +51 行，11,517 -> 11,568），纯 LF 未被翻（CRLF 0 / 裸 CR 0）。
- 账里没有任何 A 行落在 `Editor/ShellScene.cs` 被改动的范围（账中指向该文件的 18 处全部在 10995 之前，
  或属于 11023 / 11092 两处**散文引用**）。
- 那两处散文引用（`菜单_外壳.tsv:876` 的 `ShellScene.cs:11023`、`战斗逻辑驱动.tsv:1360/:1373` 的
  `ShellScene.cs:11092`）**本来就已过期**（真正接线在旧 `:11163`），现在分别落在非空行
  （`:11023` = 新块里的注释行、`:11092` = 块 ③ 的 `lib3.Save();`）⇒ 闸只出 SECOND 类告警、不判失败。
  新的真正接线行 = **`:11214`**（旧 `:11163`，+51）；同步点可顺手把这两处散文引用刷成现值。

## 五、顺手发现

1. `ShellScene.Run` 里只有这一段**吃玩家真存档**；`MainMenuScene.Run` 在开头就
   `DeckStore.OverridePath = "d:/4/_tmp_view/menu/_menu_test_decks.json"`（`Editor/MainMenuScene.cs:2741-2743`），
   所以那边的排位/遭遇段没这个问题。要不要给 `ShellScene.Run` 也加一份全局夹具，交调度台裁（本轮按最小改动只修这一段）。
2. `_tmp_view/shell/_shell_a754_decks.json` 是 `:5995-6568` 那段夹具留下的临时文件（收尾只还原
   `OverridePath`、不删文件）。写在 `_tmp_view` 下，无害；如要清爽，可在那段 `finally` 里补一句
   `DeckStore.DeleteFile()`（该段属本文件，本轮未动，避免扩大改动面）。
