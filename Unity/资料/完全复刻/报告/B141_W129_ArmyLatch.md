# B141 · W129 `B141-ARMY-LATCH` —— 阵营锁「闩」复刻 · 执行写手 `W74` 报告

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 波 `W129` · 执行写手 = `B141-W74`（本次唯一写入者）
> 判据输入 = `报告/B141_W129_DeckFilter.md` §四·❓1（W30 留下的「原版 `setted` 是闩、我们按当前状态重算」那一格）
> 原版判据 = `d:/2/tools/decomp_full/CardArmyFilter__SetFilterToDeck.c`（逐句现读）· `DeckEditingWindow__FilterToDeck.c` · `DeckEditingWindow__RefreshCards.c:7` · `DeckEditingWindow__TryOpen.c:134`
> 白名单（实际动了三份 + 新建报告）：`Core/FilterPanelModel.cs` · `Deck/DeckEditorState.cs` · 新建 `Editor/B141_W74_Assert.cs` · 本报告。
> ⛔ 未跑 Unity · 未动 git · 未动 `d:/2` · 未改账主表 / 活状态表 / 所有权表 / `CLAUDE.md` · **`Deck/DeckRuntime.cs` 一行未动**（理由见 §二·3）。
> ✅ 类型检查：改动前 `0/0`、改动后 **`0/0`**（`TMPDIR=/tmp/wf_b141_w74 bash 工具/typecheck.sh`，运行时 + 编辑器两个程序集各 0 错）。
> ✅ 账闸：`python -I -X utf8 工具/check_ledger_points.py` 的输出里**没有任何一条**落在 `DeckEditorState.cs` / `FilterPanelModel.cs`（其余 633 漂移 / 77 失败是本波之前就有的基线，⛔ 与本件无关）。

---

## 一、原版语义（逐句，`CardArmyFilter__SetFilterToDeck.c`）

| 行 | 原版代码 | 含义 |
|---|---|---|
| `:15-21` | `if (param_2 != 0) { uVar1 = *(deck + 0x40); … `<br>`if (deckHero == null && CardDeck.CardCount(deck, 0) <= 0) { …else 支… }` | **触发条件**：`deckHero(+0x40) != null` **或** `CardCount(deck, 0) > 0` ⇒ 走「锁」那支；两者皆空 ⇒ 走 else 支 |
| `:22-24` | `if ((char)param_1[8] != '\0') { return; }` | 🔴 **闩**：`param_1[8]` = `+0x40` = `setted` —— **锁过就不再重算**（`deckArmy` 后来变了也不动） |
| `:25-26` | `CardArmyFilter__FilterToArmy(param_1, deck + 0x50);`<br>`param_1[8] = 1;` | 把阵营筛选**强制**成当时的 `deckArmy`（`FilterToArmy.c` 末句还 `SetActive(gameObject, false)` ⇒ 整行隐藏）+ 打上 `setted` |
| `:29-35` | `param_1[8] = 0;` + `ClearArmyFilter` | **另一处复位**：卡组清空到「无督军 + 无牌」⇒ `setted = 0`（下一次有牌时会**重新**锁到那时的 `deckArmy`） |
| `:37-39` | `SetActive(gameObject, 1)` | 行重新露出来 |

**调用时机**（谁在调它，决定「闩在哪一拍生效」）：
- `DeckEditingWindow__TryOpen.c:134` —— **开窗**；
- `DeckEditingWindow__RefreshCards.c:7` —— **每次卡表刷新**（`FilterToDeck` → `CardCollectionFilterController__SetFiltersToDeck` → 每个 filter 的 `SetFilterToDeck`）。
- ⇒ 原版**不是**「一个窗口里只调一次」，而是「每拍都调；但 `setted` 一旦为真就早退」。所以「锁住之后换督军」那格，原版**停在旧阵营** —— 它自己的行为，照抄。

**复位时机（两处，缺一都会与原版不同）**：
1. **新窗口 = 新 `CardArmyFilter` 实例**（`setted` 从 0 起）⇒ 我们 = `DeckEditorState.LoadDeck` / `NewDeck`（开窗 / 换卡组 / 丢弃回滚都走它们）；
2. **卡组清空到「无督军 + 无牌」** ⇒ `.c:29-35` 的 else 支（懒复位，下一次刷新时生效 —— 与原版同拍）。

---

## 二、改法

### 1. `Deck/DeckEditorState.cs` —— 闩的状态 + 唯一判据口（全部追加在**类尾**）

| 成员 | 作用 |
|---|---|
| `string _armyLatchFaction` / `bool _armyLatchSettled` | 闩本体（**实例字段** —— 原版的闩活在 `CardArmyFilter` 实例上，⛔ 不做静态） |
| `public string ArmyLatchFaction { get; }` | 只读：闩里存的那一档（`null` = 没锁过）—— 断言/诊断用 |
| `public bool ArmyLatchSettled { get; }` | 只读：是否已锁（原版 `setted`） |
| `public void ResetArmyLatch()` | 复位（= 新 window 新 filter 实例） |
| `public string ResolveArmyLatch(string currentArmy, bool lockable)` | 🔴 **唯一判据口**：`!lockable` ⇒ 清闩 + null（`.c:29-39`）；已锁 ⇒ **返回旧阵营**（`.c:22-24`）；未锁 ⇒ 锁成 `currentArmy`（`.c:25-26`） |

**接线**：
- `LoadDeck(...)` 与 `NewDeck(...)` 的**同一物理行**末尾（注释前）追加 `ResetArmyLatch();` —— 这两条是「开窗 / 换卡组 / 丢弃回滚」那一拍（`DeckRuntime.Build:1365` + `RollBackEditingCopy:5738`）；
- `VisibleCards()` 的 `armyLocked` 改为走 `FilterPanelModel.ArmyLockFaction(this)`（同一物理行改写），阵营比对用**闩里的那一档**（`armyLockFaction`）而不是**当前** `DeckArmyFaction()`（`:595` 同一物理行改写）。
  - ⚠️ 这一句**有副作用**（首次锁上会写闩）—— 与宿主原版同拍：`RefreshCards.c:7` 每拍调 `SetFilterToDeck`。
  - 收藏窗那一路（`WarlordGatedPool = false`）**根本不调它** ⇒ 不写闩（原版 `SetFiltersToDeck` 全库只有卡组编辑窗一个调用点）。

### 2. `Core/FilterPanelModel.cs` —— 判据分层（都在 `:1297` 之后 = 类尾，最大账落点 `:1223` 之前无变动）

| 成员 | 语义 |
|---|---|
| `ArmyLocked(state)`（**语义收窄，doc 就地改写**） | 只回答「这一拍**该不该**锁」（原版 `.c:16-21` 的两条）；⚠️ **不再等于「已经锁上」** |
| `ArmyRowVisible(state, deckEdit)`（未改） | 行显隐看的是**当前条件**（有督军/有牌就隐藏），与闩无关 —— 原版锁那支无论 `setted` 与否都 `SetActive(false)` |
| 🆕 `ArmyLockFaction(state)` | 🔴 锁住时**实际生效的阵营** = `state.ResolveArmyLatch(state.DeckArmyFaction(), ArmyLocked(state))` —— 消费者 = `VisibleCards` + 断言 |

`ArmyLocked` 原来那段「⚠️ 与原版的唯一差别（如实记）：原版的 `setted` 是闩……本轮不做」**已就地改写**（铁律 5）：现在写明分工（条件 vs 闩），并把「别在 `VisibleCards` 里塞缓存」那句换成「别每拍按当前值重算」。

### 3. `Deck/DeckRuntime.cs` —— **没动**（白名单允许，但不需要）

- 「开窗」=`Build()` 里 `State = NewState()`（`:1358`，**新实例** ⇒ 闩天然是 0）+ `State.LoadDeck(...)`（`:1365`）；
- 「换卡组 / 丢弃回滚」=`State.LoadDeck(...)`（`:5738`）；
- ⇒ 两条复位都落在 `DeckEditorState.LoadDeck/NewDeck` 里，宿主一个字节不用改（少一次行号漂移风险）。

---

## 三、行数变化（三文件都有账落点 ⇒ 只做「同物理行改写」或「类尾追加」）

| 文件 | 改前 | 改后 | 变化 | 是否漂移 |
|---|---:|---:|---:|---|
| `Core/FilterPanelModel.cs` | 1,325 | 1,342 | **+17**（`ArmyLocked` doc 由 6 行→8 行、新增 `ArmyLockFaction` 17 行，**全部在 `:1297` 之后**） | ❌ 不漂（最大账落点 `:1223` 未动，现读仍是 `BuildHeaderChips` 声明） |
| `Deck/DeckEditorState.cs` | 1,575 | 1,625 | **+50**（全部追加在 `DeckArmyFaction()` 之后 = 类尾）+ `:268` / `:302` / `:585` / `:595` **同物理行改写** | ❌ 不漂（`:1506 DeckCardCount` / `:1517 DeckHas` 现读逐字未变） |
| `Deck/DeckRuntime.cs` | 10,750 | 10,750 | **0** | — |
| 新建 `Editor/B141_W74_Assert.cs` | — | 230 | 新建（该文件**无账落点**） | — |

行尾：`FilterPanelModel.cs` 仍为纯 LF（1342/1342）· `DeckEditorState.cs` 仍为纯 CRLF（1625/1625 · 裸 CR 0）—— 两者都逐字节核过，**没有翻行尾**。

---

## 四、断言（新建 `Editor/B141_W74_Assert.cs`，**23 条 `Check`**）

| 组 | 条数 | 内容 |
|---|---:|---|
| **L**（闩本身） | 8 | 首次刷新锁在 A / **换督军到 B 后仍停在 A**（`U_B`/`H_B` 都不出现）/ A 的卡仍在 / 闩里仍是 A / 行仍隐藏 / **灭自证：刷新之前就换到 B ⇒ 锁在 B** |
| **R**（重开窗复位） | 5 | `LoadDeck` 后 `ArmyLatchSettled` = 假 / 之后按**新**督军 B 重锁 / `NewDeck` 同样复位 |
| **E**（`.c:29-39` else 支） | 4 | 清空整副 ⇒ 闩解除 + 行露出 / 之后重新加 B 的牌 ⇒ 锁成 **B**（不是旧 A） |
| **C**（收藏窗不受影响） | 3 | 不开 `WarlordGatedPool` ⇒ **不写闩** / 玩家点的阵营照常生效 / 刷新也不顺手写闩 |
| **G**（唯一判据口护栏） | 3 | 空卡组 ⇒ `null` / 连读两次（中间换督军）都返回旧 A / `state == null` 不抛 |

**灭自证（🧨 结构上不可能同时绿）**：
- **L4 × L5** —— 先刷新再换督军 ⇒ 停在 A；刷新前就换 ⇒ 锁在 B。「永远按当前值重算」⇒ L4 红；「闩在 `SetWarlord` 那一刻写死第一个督军」⇒ L5 红。
- **L4 × R3** —— 不重开窗 ⇒ 停在 A；`LoadDeck` ⇒ 按新督军重锁。`ResetArmyLatch` 每拍都调 ⇒ L4 红；忘了复位 ⇒ R3 红。
- **E2 × E3** —— 清空要解闩、之后要锁成新阵营。漏掉 else 支复位 ⇒ E3 红；把锁做成恒真 ⇒ E2 红。
- **C1 × C2** —— 收藏窗不写闩、且阵营筛选照常。把闩做成全局 ⇒ C1/C2 红；废掉 `FactionOk` ⇒ C2 红。

**怎么接线（我没接）**：`public static void Run()` 零场景前提（无外部数据依赖）。宿主聚合行**同一物理行**追加：
`B141_W74_Assert.Run(); _sink.Pass += B141_W74_Assert.Pass; _sink.Fail += B141_W74_Assert.Fail;`
判绿红按**行首标记**数（`^B141W74 ✓` / `^B141W74 ✗`），⛔ 别 `grep -c ✗`。

---

## 五、⚠️ 跨文件连带（不在我的白名单，⛔ 我没动 —— 请调度台分流给 `DeckScene.cs` 的写手）

闩落地后，**同一份 `DeckEditorState` 上连续换 13 个阵营督军**的那几个夹具会红 —— 它们模拟的是「一个 filter 实例里换 13 次阵营」，
而按原版（`.c:22-24`）那正是「锁住就不动」的场景。**改法 = 每次换督军后补一句 `s.ResetArmyLatch();`**（= 该夹具想表达的「每个阵营 = 一次新开窗」）：

| # | 位置 | 为什么会红 |
|---|---|---|
| 1 | `Editor/DeckScene.cs:1478` 与 `:1498`（`TestWarlordGatedPool`，方法起 `:1427`） | `:1447` 第一次 `SetWarlord` + `:1448 VisibleCards()` 把闩锁在第一个阵营；`:1478`（EmperorsChildren）/ `:1498`（DarkAngels）换到别阵营后 `VisibleCards()` **仍是第一阵营的池子** ⇒ `:1500-1512` 的「DarkAngels 5 张秘仪都在」**必红**（`shown` 会是 0） |
| 2 | `Editor/DeckScene.cs:1676`（`TestPoolSortAndVisibility`，方法起 `:1665`） | `foreach (var fac in ArmyOrder)` 每轮 `SetWarlord(hw.Id)` + `VisibleCards()`；第 2 轮起闩停在第一阵营 ⇒ 「同阵营」「战将段在最前」「按费用」那些逐轮断言**全红** |
| 3 | `Editor/DeckScene.cs:1825`（`TestDefensiveCardsInEditMode` 的 `countDefence` λ，方法起 `:1812`） | 同样 13 阵营循环 ⇒ 只有第一阵营的防御卡被数到，`defReachable` / `defNotFull` 的分子分母**都错** |

> 这三处**不是我改坏的**，而是「旧夹具按『每次重算』写的」。补 `ResetArmyLatch()` 后它们回到原来的语义（每个阵营一次新窗口），
> 而且**顺带钉住**「复位口真的有用」（R 组断言已经在此钉住同一件事）。
> ⚠️ 另：`Editor/DeckScene.cs:3945` 那条 N14 ② 反向控制（Army 行要隐藏）是 **W30 报告 §三 #1** 已记的连带项，**与闩无关**，
> 本件落地前后**都**按「有牌 ⇒ 隐藏」判，不因闩改变。

---

## 六、❓ 如实记

1. **今天的可观测面（如实说）**：正常玩家路径上 `deckArmy` 只在三处变 —— ① `AddCard` 且 `CardCount(hero:1)==0`（锁住后不可能）、② 加督军（`CanAddHero` 挡异阵营 ⇒ 同阵营换将不改 `deckArmy`）、③ `ClearCards`（空卡组 ⇒ else 支复位）。⇒ **闩在正常 UI 里几乎观测不到**；它真正改变行为的是「**跨阵营换督军**」那一格（`SetWarlord` 直调；玩家路径被 `InvalidArmy` 挡着）。这**不是**本件没做，而是原版这个闸本来就只管这一格 —— 照抄即达成（任务书原话：「只在『锁住之后又换督军』这一格不同」）。
2. **`currentArmy` 为空的边角**（督军解析不出来 / 阵营是 `Neutral`）：原版会 `FilterToArmy(0)`（按 Neutral 筛）；我们**不锁**（返回 null）—— 与改前 `!string.IsNullOrEmpty(deckArmy)` 那一闸同口径。现池 13 阵营**无 Neutral**、督军解析失败 0 例 ⇒ 今天零可观测；⛔ 没有为此加断言（无数据可钉）。
3. **`setted` 的「懒复位」**：清空整副后，`ArmyLatchSettled` 要等**下一次卡池刷新**才会变假（原版也一样：`setted = 0` 写在 `SetFilterToDeck` 里）。断言 E2 因此**先刷新再断言** —— 这不是我们的偏差。
4. **`VisibleCards()` 现在有副作用**（首次锁上写闩）。它是**只读名义**的方法，这一格必须写进 doc（已写在 `ResolveArmyLatch` 与 `ArmyLockFaction` 的 `<summary>` 里），给后来人提防「为什么两次调用之间状态会变」。
5. **`DeckRuntime` 未改**：如果将来出现「同一个 `DeckEditorState` 实例换卡组却不走 `LoadDeck`」的新路径，那句话就失效 —— 目前 `State` 只有 `NewState()`（`:1428`）创建、只有 `LoadDeck`（`:1365` / `:5738`）装内容。

---

## 七、出处（现读）

- 原版：`d:/2/tools/decomp_full/CardArmyFilter__SetFilterToDeck.c:15-39` · `CardArmyFilter__FilterToArmy.c`（末句）· `CardArmyFilter__ClearFilters.c:10-13` · `DeckEditingWindow__FilterToDeck.c:18-27` · `DeckEditingWindow__RefreshCards.c:7` · `DeckEditingWindow__TryOpen.c:134`
- 本件改动：
  - `Deck/DeckEditorState.cs:268`（`NewDeck` 复位）· `:302`（`LoadDeck` 复位）· `:585`（`armyLockFaction` 走闩）· `:595`（比闩里那一档）· `:1575-1625`（类尾：闩字段 / `ArmyLatchFaction` / `ArmyLatchSettled` / `ResetArmyLatch` / `ResolveArmyLatch`）
  - `Core/FilterPanelModel.cs:1286-1292`（`ArmyLocked` doc 就地改写）· `:1305-1320`（新增 `ArmyLockFaction`）
  - 新建 `Editor/B141_W74_Assert.cs`（230 行 / 23 条 `Check`）
- 前置报告：`报告/B141_W129_DeckFilter.md` §四·❓1（本件就是它留下的那一格）

---

## 八、续做 `B141-W74b`：`Editor/DeckScene.cs` 三处夹具补复位（2026-11-04 · 同会话）

> 白名单 = `Editor/DeckScene.cs` **只这四处物理行**（§五 列的 3 个夹具）· 仍 ⛔ 未跑 Unity / 未动 git / 未改账主表。
> ✅ 类型检查：改动后 **`0/0`**（`TMPDIR=/tmp/wf_b141_w74b bash 工具/typecheck.sh`，运行时 + 编辑器各 0 错）。

**改法**：每处都是「在 `SetWarlord(...)` 那一句**同物理行**追加 `s.ResetArmyLatch()`（`countDefence` 里是 `st.`）+ 行尾 `// ✅ 已复刻（2026-11-04 · B141 · W129 \`B141-ARMY-LATCH\`…）`标记 ⇒ **行数零变化**、行号零位移。

| # | 现读行 | 所在夹具 | 改后该行（摘） |
|---|---:|---|---|
| 1 | `:1478` | `TestWarlordGatedPool`（起 `:1427`）· EmperorsChildren 探针 | `s.SetWarlord(hw.Id); s.ResetArmyLatch();` |
| 2 | `:1498` | 同上 · DarkAngels 探针（`else` 块内） | `s.SetWarlord(hw.Id); s.ResetArmyLatch();` |
| 3 | `:1676` | `TestPoolSortAndVisibility`（起 `:1665`）· 13 阵营逐轮 | `s.SetWarlord(hw.Id); s.ResetArmyLatch();` |
| 4 | `:1825` | `TestDefensiveCardsInEditMode`（起 `:1812`）· `countDefence` λ | `st.SetWarlord(wl.Id); st.ResetArmyLatch();` |

**行数变化**：`Editor/DeckScene.cs` **10,584 → 10,584（+0）**；四处改后行号仍是 `1478 / 1498 / 1676 / 1825`（现读复核）；行尾 CRLF 未翻（10584/10584 · 裸 CR 0）；账落点最大写 `:4739`，**零漂移**。

**灭自证（去掉 `ResetArmyLatch()` ⇒ 下列断言必红，均已按代码现读核对）**：
- 去掉 #1/#2 ⇒ `TestWarlordGatedPool` 的「★ 5 张暗黑天使秘仪（`inv=10` 且 `show=5`）**都在卡池里显示**」那条红（闩停在第一个阵营 ⇒ `shown = 0` ≠ 5）；同组的 `eff == 0` 那条**不会**红（第一阵营恰好也没有「不能进卡组」的卡）—— 如实写，⛔ 不夸大。
- 去掉 #3 ⇒ `TestPoolSortAndVisibility` 从第 2 轮起「同阵营 / 战将段在最前 / 战将段后按费用」逐轮断言红。
- 去掉 #4 ⇒ `TestDefensiveCardsInEditMode` 的「★ B-12 字段 true（遭遇）⇒ `defReachable` 张防御卡一张不少」与「★ B-12 ……复原字段之后又回来了」两条红（`countDefence` 只数得到第一个阵营的防御卡）。

**没补的两处（现读确认不需要）**：
- `:1447`（`TestWarlordGatedPool` 的第一次 `SetWarlord`）—— 它前面 `:1432` 的 `VisibleCards()` 跑在**空卡组**上（`.c:29-39` else 支 ⇒ 闩本来就是 0），不需要复位；
- `Editor/DeckScene.cs:2257/2265/2276`（纯逻辑用例，全节不调 `VisibleCards`）· `:3677/3768/3950`（运行时用例**放回同一个督军**、同阵营 ⇒ `deckArmy` 不变、闩不变）—— 两族都与闩无关。
