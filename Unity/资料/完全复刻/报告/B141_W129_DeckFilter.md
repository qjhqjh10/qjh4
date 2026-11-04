# B141 · W129 卡组筛选模型 7 条 —— 执行写手 `W30` 报告

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 波次 `W129` · 执行写手 = `B141-W30`（唯一写入者）
> 判据输入 = `判据/B141_W129_对账_卡组色彩.md` §一（P2 包）＋ `报告/B138_审计汇总_问题清单.md` §3/§0·4 #4 ＋ 原版 `decomp_full`（现读）
> 白名单（只动这四份）：`Core/FilterPanelModel.cs` · `Deck/DeckEditorState.cs` · 新建 `Editor/B141_W30_Assert.cs` · 本报告。
> ⛔ 未跑 Unity · 未动 git · 未动 `d:/2` · 未改账主表 / 活状态表 / 所有权表 / `CLAUDE.md`。
> ✅ 类型检查：改动前 `0/0`、改动后 `0/0`（`TMPDIR=/tmp/wf_b141_w30 bash 工具/typecheck.sh`，运行时 + 编辑器两个程序集各 0 错）。

---

## 〇、结论数字

| 口径 | 数 |
|---|---:|
| 本包负责条目 | **7**（3-01 模型半 · 3-02 · 3-03 · 3-04 · 3-18 返回码半 · 3-30 · 3-31） |
| `✅ 已复刻`（代码落地 + 断言） | **6**（3-01/3-02/3-03/3-04/3-30/3-31） |
| `🟠 半完成`（本文件内能做的全做完，剩一行在别人的文件里） | **1**（3-18：返回码已改；`Describe` 的两条**键名映射**属 `W31` 的 `RuleEngine/Core/DeckRules.cs`） |
| 新增断言 | **48 条 `Check`**（H 组 14 · L 组 9 · T 组 9（含 T5 的**互斥双分支**：前提不成立也记红）· R 组 12 · N 组 4）＋ 1 条 `Debug.Log` 登记行（不计通过数） |
| 行数变化 | `FilterPanelModel.cs` **+59 行（全部追加在类尾 ⇒ 既有行号一个不动）** · `DeckEditorState.cs` **+0 行（全部就地改写同一物理行）** · 新建断言件 421 行 |
| 跨文件待办（别人的白名单，⛔ 我没动） | **3 条**（见 §三：两条既有断言会被本包改红、一条判据要翻极性） |

---

## 一、逐条（原版值 → 改后值）

### 3-01（模型半）阵营筛选行：有督军 / 有牌 ⇒ **整行隐藏**并锁 `deckArmy`

| 项 | 内容 |
|---|---|
| 原版判据 | `decomp_full/CardArmyFilter__SetFilterToDeck.c:16-19` —— `deckHero(+0x40) != null` **或** `CardDeck.CardCount(deck,0) > 0` ⇒ 走锁那支；`.c:20-25` 锁那支 = `if (param_1[8] != '\0') return;` → `FilterToArmy(deck+0x50)` → `setted=1`；`CardArmyFilter__FilterToArmy.c` **末句 `SetActive(gameObject,false)`** ⇒ 整行（标题 + 13 格）一起不画。两者皆空 ⇒ `ClearArmyFilter` + `SetActive(true)` |
| 改前 | 行**恒显示**（`FilterPanelModel.cs:704` 无条件建格、`:1008` Army 标题无 `Hidden`） |
| 改后 | ① `FilterPanelModel.ArmyLocked(state)` = `HasWarlord ∥ DeckCardCount(true) > 0`（新增，**判据只此一处**）；② `ArmyRowVisible(state, deckEdit)` = `!deckEdit || !ArmyLocked`；③ `Build` 里 `armyHidden` 落到**每一颗 `$fac:` 格的 `Cell.Hidden`**、`BuildTitles` 里落到 **`Army` 小标题的 `Title.Hidden`**；④ 宿主**不需要改**：`DeckRuntime.RefreshFilterCells` / `RefreshFilterTitles` 对 `Hidden` 的兑现（`SetActive(false)` + 不登记点击区 + 小标题整条跳过）是**现成的**（费用 / 卡类两行同一条口径） |
| 落点 | `Core/FilterPanelModel.cs:658`（`Build` 算 `armyHidden`）· `:713`（格 `Hidden`）· `:1003`（`BuildTitles` 算）· `:1008`（标题 `Hidden`）· `:1292`/`:1300`（两个新谓词） |
| 窗别 | 与费用 / 卡类共用「这份卡组被下发给筛选栏了」那个按窗开关（`costRowNeedsWarlord`）：卡组编辑 `true` / 收藏窗两页缺省 `false`（原版 `SetFiltersToDeck` 全库只有一个调用点 = `DeckEditingWindow__FilterToDeck.c:24`）。卡背抽屉（`BuildCosmetics`）**不动** —— 那颗 `Cosmetic FIlter` 是另一个 controller，`SetFiltersToDeck` 只遍历 `+0x20` 那条注册表（`CardCollectionFilterController__SetFiltersToDeck.c:24-40`） |
| 行数 | `FilterPanelModel.cs` +59（全在类尾）· `DeckEditorState.cs` +0 |

### 3-02 删掉「玩家显式点了阵营 ⇒ 绕过督军闸」那条越权路（`armyPicked`）

| 项 | 内容 |
|---|---|
| 原版判据 | 同 3-01 的 `.c:20-25` 早退句 + `FilterToArmy.c` 末句 `SetActive(false)` ⇒ **行都隐藏了、原版没有切换入口** |
| 改前 | `DeckEditorState.cs:585` 的 `armyPicked`（`B121_WC · COLL-5①` 那一版）让「筛选里点了阵营」直接绕过督军闸 |
| 改后 | `armyPicked` **删除**；锁住时阵营由 `deckArmy` **独占**（`DeckEditorState.cs:595`），`Filter.Factions` 不再参与（`:608`） |
| 铁律 5 | 原地在 `:585` 的注释里写清「旧口径已被 `.c:20` 早退句推翻」，不留两份说法 |
| 行数 | +0（同物理行改写） |

### 3-03 有牌、**没督军** ⇒ 卡池只列 `deckArmy` 那一阵营的战将

| 项 | 内容 |
|---|---|
| 原版判据 | 同 3-01：`CardCount(hero:1) > 0` 而无督军 ⇒ 仍然 `FilterToArmy(deckArmy)`（`deckArmy` 在 `AddCard` 里由第一张普通卡定，`CardDeck.cs:656-661`）；战将那一格由 `CardTypeFilter` 的 `ShowOnlyHero` 支负责 |
| 改前 | `DeckEditorState.cs:594` 只判 `c.Type != "hero"` ⇒ **13 个阵营的战将全列**（用户口径「没有督军时列各阵营督军」在**空卡组**时成立，在有牌时原版是收窄的） |
| 改后 | `:595` `if (armyLocked && !DeckRules.SameFaction(c.Faction, deckArmy)) continue;` —— 「只列战将」那一行**保留**，阵营收窄由同一条锁负责 |
| 行数 | +0 |

### 3-04 类型筛按**原版 `cardType`** ⇒ 点 Tactic 不再丢 39 张防御卡

| 项 | 内容 |
|---|---|
| 原版判据 | `CardTypeFilter.options` = `Hero 10 / Minion 0 / Tactic 20` 三档（**没有第四档**）；`dump.cs` 的 `RawCardScript`：`showInCollectionOptions +0x48` · `inventoryOptions +0x4C` · `cardType +0x94`；`cards_engine.json` 现读 **`type="defence"` 39 张全是 `cardType = 20`**（另有 2 张 `enchantment` 也是 20）⇒ **点 Tactic = 377**（我们改前 338，差 39） |
| 改前 | `DeckEditorState.TypeOk` 直接比 `c.Type` 字符串 ⇒ `defence ≠ tactic` |
| 改后 | 新增 `FilterPanelModel.CardTypeBucket(type, cardType = -1)`：**有 `CardDef.CardType` 真值就按真值分档**（10/0/20），真值缺失（`-1`）才回落到字符串折算（`defence → tactic`）；`TypeOk` 加第三参、`VisibleCards` 调用点传 `c.CardType` |
| 铁律 5 | `FilterPanelModel.cs:414-415` 那句「原版没有『防御卡』那一档 ⇒ 防御卡筛不到」**就地订正**（反证写在同一行注释里） |
| 落点 | `Core/FilterPanelModel.cs:415`（订正）· `:1316`（`CardTypeBucket`）· `Deck/DeckEditorState.cs:120/124/127`（`TypeOk`）· `:610`（调用点传 `c.CardType`） |
| 行数 | `DeckEditorState.cs` +0 · `FilterPanelModel.cs` 已在类尾那 +59 里 |

### 3-18（**返回码半**）同名上限 / 整副满 / 准入 三档分得开，准入那档回 `InvalidCard`

| 项 | 内容 |
|---|---|
| 原版判据 | `CardDeck__CanAddCard.c:40`（`inventoryOptions(+0x4c) != 0 && showInCollectionOptions(+0x48) != 5`）⇒ `DAT_1842cf4f0` = **`MenuDeck/Error/InvalidCard`**；`.c:48` 同名上限 ⇒ `0x42CF7F0 = MaxCopiesReached`；`.c:166` 整副满 ⇒ `DAT_1842d0be0` = **`MenuDeck/Tip/DeckFull`**（官方表 EN/ZH 都在，现读） |
| 改前 | 准入那一档回 `DeckError.EffectOnlyCard` —— 而 `MenuDeck/Error/EffectOnlyCard` **原版没有这条键**（`Loc.HasAnyTerm` = 假）⇒ 显示层摊成 catch-all `InvalidDeck`（「所选卡组无效」而不是「这张牌不能加」） |
| 改后 | `DeckEditorState.cs:445` 准入那一档改回 **`DeckError.InvalidCard`**（原版那一支的具名键，官方 EN/ZH 齐全 ⇒ **今天就能印出人话**）。同名上限仍 `CopyLimitExceeded`、整副满仍 `TooManyCards` —— 三档**互不相同**（断言 R4 钉住） |
| 剩给 `W31` 的半 | `RuleEngine/Core/DeckRules.cs` 的 `Describe` 加两条**键名映射**：`CopyLimitExceeded → MenuDeck/Error/MaxCopiesReached`、`TooManyCards → MenuDeck/Tip/DeckFull`（⚠️ 后者是 **Tip** 族，不能拼 `TermPrefix`）。两条目标键**已现读存在**（断言 R5/R5b 钉住），`W31` 落映射时不会踩空 |
| 🧨 | 断言 R7 同时钉「`InvalidCard` 与 `EffectOnlyCard` 的键不同」⇒ 把两档并回去会红 |
| 行数 | +0 |

### 3-30 锁住之后 `Clear filters` **不清阵营**

| 项 | 内容 |
|---|---|
| 原版判据 | `CardArmyFilter__ClearFilters.c:10-13` —— `setted(+0x40)` 为真 ⇒ **直接 return**（连清空都不清阵营） |
| 改前 | `DeckEditorState.cs:608` 无条件走 `FactionOk` ⇒ `Clear filters`（宿主 `DeckRuntime.ClearFilters` 把 `Filter` 换成 `DeckFilter.None`）会把阵营筛清掉 ⇒ 池子变 |
| 改后 | `:608` `if (!armyLocked && !DeckFilter.FactionOk(f, c.Faction)) continue;` —— 锁住时阵营那一层**只认 `deckArmy`**，`Filter.Factions` 再怎么被清都不影响结果 ⇒ 与原版「清了也不清阵营」**观测等价** |
| 为什么不在宿主改 | 改动点 `DeckRuntime.ClearFilters`（`:7328-7341`，`var f = DeckFilter.None;` 在 `:7334`）是 **`W29` 的文件**（`文件所有权_当前.md` `B141-W29`）⇒ 我把等价实现放在**状态层**（`VisibleCards`），宿主那一行**不需要动** |
| 残留（如实记） | 清空之后 `Filter.IsEmpty` 会变真（我们内部状态确实被清了）⇒ 页头 `Clear filters` 那颗钮的亮/灭可能与原版差一拍（原版 `filters[]` 仍非空）。**画面级、非池子结果**；要完全对齐得给 `DeckFilter` 加「锁住」位，属 3-01 宿主钩子那一层，本轮未做（❓见 §四·3） |
| 行数 | +0 |

### 3-31 `Neutral` 按原版枚举序（= 0）**排最前**

| 项 | 内容 |
|---|---|
| 原版判据 | `dump.cs:45638` `CardArmy.Neutral = 0`（`CardArmyOrdered.Neutral = 0` 同，`:45659`），13 个真阵营是 10…130 |
| 改前 | `Factions()` 的 comparer 把「不在 `ArmyOrder` 里的」一律 `int.MaxValue` ⇒ `Neutral` 排**最后** |
| 改后 | 那两行改成 `IsNeutralFaction(x) ? -1 : int.MaxValue` ⇒ **表首** |
| ⛔ 设计选择 | **没有**把 `"Neutral"` 塞进 `ArmyOrder` 数组 —— 那是原版 `options` 的 13 条，而且 `Editor/DeckScene.cs:1777` 有一条断言拿 `ArmyOrder[0]` 当 `Ultramarines`（塞表首会让那条红、且改的是别人文件的语义）。断言 N4 把这条选择钉住 |
| 可观测 | 现池 13 阵营**无 Neutral** ⇒ 今天零画面差（判据原话）；N2 用合成池验行为 |

---

## 二、断言（新建 `Editor/B141_W30_Assert.cs`，421 行 / 48 条 `Check`）

| 组 | 内容 | 条数 |
|---|---|---:|
| **H**（3-01） | 空卡组露出来 / 有督军隐藏+锁 / **有牌没督军也隐藏** / **同一个 state 收藏窗那一档仍显示** / `Build` 的格与标题 `Hidden` / 格子**照建不删** / 清空后恢复 | 14 |
| **L**（3-02/03/30） | 有督军时残留 B 筛选不生效 / **收藏窗对照（B 看得见）** / 有牌没督军只列 `deckArmy` 战将 / **空卡组对照（各阵营战将都在）** / 锁住时「加 B 筛选 → `Clear filters`」张数三次一致 / **空卡组对照（清空前后的张数差）** | 9 |
| **T**（3-04） | `defence` 落进 Tactic（字符串 + 真值两条）/ `unit` 不落 / 反向不单向放行 / **真实卡池：`cardType==20` 的每一张都命中、`0/10` 的一张都不许** / 两种口径的差集登记 | 9 |
| **R**（3-18） | 同名上限 = `CopyLimitExceeded` / 准入 = **`InvalidCard`** / 防御卡不被准入挡 / 满 12 张 = `TooManyCards` / **三档互不相同** / 两条目标词条键存在 / `InvalidCard` 有真词条 / 与 `EffectOnlyCard` 键不同 | 12 |
| **N**（3-31） | `Neutral` 排首 / 其余仍按 `ArmyOrder` / **`ArmyOrder` 本身不含 Neutral** | 4 |

**灭自证（成对断言，结构上不可能同时绿）**：
- H3/H5（卡组编辑要隐藏）× H6/H12（同一 state 的收藏窗档要显示）—— 只按 state 判 ⇒ 后者红；永不隐藏 ⇒ 前者红。
- L1（有督军时 B 看不见）× L2（收藏窗下 B 看得见）—— 废掉 `FactionOk` ⇒ L2 红；锁不生效 ⇒ L1 红。
- L3b（有牌没督军只列 `deckArmy`）× L4（空卡组各阵营都在）—— 「有牌」判据写成恒真 ⇒ L4 红。
- L5（锁住时清空无变化）× L6（没锁时清空有变化）—— 把锁做成恒真 ⇒ L6 红。
- T1（`defence` 命中 Tactic）× T3（`unit` 不命中）—— 「匹配一切」⇒ T3 红。
- T5（`cardType==20` 全命中）× T6（`0/10` 零命中）—— 用 `type` 字符串硬编码或放宽都会破一边。
- R4（三档不同）× R2b/R6（准入那档是真 `InvalidCard` 且取得到人话）。
- N2（Neutral 排首）× N4（`ArmyOrder` 不许动）—— 「把 Neutral 塞表首」的偷懒做法会破 N4。

**怎么接线（我没接）**：`public static void Run()` 零场景前提。宿主聚合行**同一物理行**追加：
`B141_W30_Assert.Run(); _sink.Pass += B141_W30_Assert.Pass; _sink.Fail += B141_W30_Assert.Fail;`
判绿红按**行首标记**数（`^B141W30 ✓` / `^B141W30 ✗`），⛔ 别 `grep -c ✗`。
⚠️ **唯一外部依赖** = `CardDatabase.Load()`（只用 T5/T6/T7/T8 那一块）；拿不到时那一条**直接记红**（「前提不成立 —— 这一条没有验到」），⛔ 不静默跳过。

---

## 三、跨文件待办（⛔ 不在我的白名单，我没动 —— 请调度台分流）

| # | 位置 | 为什么会红 / 要做什么 | 建议改法 |
|---|---|---|---|
| 1 | `Editor/DeckScene.cs:3945-3948` | **N14 ② 的「反向控制」**：那一拍 `_rt.State.ClearWarlord()` 但**卡组仍有 30 张牌** ⇒ 按原版 `.c:16-19` 是「有牌」⇒ **Army 行也该隐藏**。它现在断「`$fac:` 首格 `activeSelf` = 真」，本包落地后**必红**（而且它断的那条前提本身就不合原版：原版这一拍确实会隐藏 Army 行） | 把「反向控制」换成同拍**仍然活着**的那一族（`$rar:` 首格，稀有度行不受 `deckHero`/牌数影响），或把 `ClearWarlord()` 之后**再清空卡组**（那就变成「空卡组」基线，需同步改 N14 ② 的语义） |
| 2 | `Editor/DeckScene.cs:4499` | `CheckTrue(draw.Count >= 30, "抽屉里量到 N 张可见图…")` —— `draw` 取的是 `GetComponentsInChildren<ImageQuad>(false)`（**不含 inactive**），本包落地后卡组编辑窗默认有督军 ⇒ **13 颗阵营格 `SetActive(false)`** ⇒ 计数掉 13 ⇒ **必红** | 拆成两条：① `draw.Count` 的健全检查改用 `true`（含 inactive，仍是「建出来了」那条判据）；② 新增一条「`Hidden` 的格子确实 inactive」（可用 `UiFilterCellQuad(key).gameObject.activeSelf` 逐格断） |
| 3 | `Editor/B121_WC_Assert.cs:143-160`（`D_PoolSplitEdge`） | `D2` 断的正是**本包删掉的那条越权路**（「玩家显式点了别的阵营 ⇒ 卡池跟着筛过去」）⇒ 本包落地后**必红**，且它引用的 `B121_WC · COLL-5①` 判据已被 `CardArmyFilter__SetFilterToDeck.c:20-25` 的早退句推翻 | **翻极性**：改为断「有督军 + 残留阵营筛选 ⇒ 别的阵营**仍然看不见**（行已隐藏、无切换入口）」，并把 D3（`InvalidArmy` 挡住）保留；文件头那行 `COLL-5①` 描述同步订正 |

> 这三条都是**判据驱动**的连带改动（不是本包改坏），负责人不是 `W30`。本包已把「新行为」写进 49 条断言，改完这三处后 `DeckScene.Run` / `B121_WC` 才可能与本包一致。

---

## 四、❓ 残留 / 如实记

1. **`setted` 是【闩】，我们按当前状态重算** —— 原版锁过一次就 `return`（`.c:20`），于是「锁住之后又换督军/阵营」这一格原版会**保留旧 `deckArmy` 的筛选**（看起来是原版自己的 bug）。要照抄它必须先有「开窗那一拍」的宿主钩子（`DeckEditingWindow__FilterToDeck` 的等价入口），且在 `VisibleCards` 里塞缓存 —— 本轮**不做**，已在 `ArmyLocked` 的 doc 里写清。**今天的行为差**：换督军后池子立刻跟新阵营（原版会停在旧阵营）。
2. **`deckArmy` 为 null 的边角**：若督军解析不出来 / 督军阵营是 `Neutral`（现池 0 例）⇒ `armyLocked = false` ⇒ 不再按 `wl.Faction` 收窄（旧代码会）。今天零可观测；如实记在这里而不是静默。
3. **3-30 的画面残差**：清空后 `Filter.IsEmpty` 变真 ⇒ 页头 `Clear filters` 钮的亮/灭可能差一拍（池子结果已对齐）。要完全对齐需要给筛选状态加一个「锁住」位（= 原版 `setted`），那是 §四·1 的同一条钩子。
4. **`enchantment` 那 2 张**（`Reconnaissance Mission` / `Pray Enchantment`，`cardType = 20`）现在按真值也会落进 Tactic 档 —— 这是原版判据的正确结果；它们今天是否出现在卡池由上游 `CardDatabase` 的过滤决定（`X20` 记着「不在这里返回」），本轮未改、也未验。
5. **`CardTypeBucket` 只认 10/0/20** —— `Whispers(40)` 现池 0 张；将来若有，按原版应**不落进任何一档**（原版 `CardTypeFilter` 三档里没有它）。我们回落成「按 `type` 字符串」= 也不命中三档之一，行为一致，但**没有断言钉它**（无数据可钉）。

---

## 五、出处（现读）

- 原版：`d:/2/tools/decomp_full/CardArmyFilter__SetFilterToDeck.c:16-25` · `CardArmyFilter__FilterToArmy.c`（末句）· `CardArmyFilter__ClearFilters.c:10-13` · `CardCollectionFilterController__SetFiltersToDeck.c:24-40` · `DeckEditingWindow__FilterToDeck.c:7-26` · `CardDeck__CanAddCard.c:29-40,48,166`
- 枚举/字段：`d:/2/tools/il2cpp_out/dump.cs:45634-45652`（`CardArmy`）· `:45655-45673`（`CardArmyOrdered`）· `RawCardScript` 字段表（`+0x48/+0x4C/+0x90/+0x94`）
- 数据：`MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`type=defence` 39 张全 `cardType=20`）
- 词条：`数据/游戏数据/官方本地化_en+zh-CN.json`（`MenuDeck/Error/MaxCopiesReached` · `MenuDeck/Tip/DeckFull` · `MenuDeck/Error/InvalidCard` 三条都在）
- 本包改动：`Core/FilterPanelModel.cs:415,658,713,1003,1008,1275-1325`（新谓词与 `CardTypeBucket` 在类尾）· `Deck/DeckEditorState.cs:119-127,445,585,595,608,885-891` · 新建 `Editor/B141_W30_Assert.cs`

---

## 六、续做 `B141-W30b`：`B121_WC_Assert.cs` 的 `D_PoolSplitEdge` 翻极性

> 2026-11-04 · 同会话续做 · 白名单扩到 `CardPresentation/Editor/B121_WC_Assert.cs`（⛔ 仍不碰 `DeckScene.cs` —— 那两条（`N14 ②` 反向控制、`draw.Count >= 30`）等 `W29b` 释放后由它收）。

| 项 | 内容 |
|---|---|
| 为什么必红 | `D2` 断的是本包删掉的那条越权路（「有督军 + 显式点别的阵营 ⇒ 卡池跟着筛过去」）⇒ 本包落地后 `VisibleCards` 把阵营锁成 `deckArmy`，B 阵营看不见 ⇒ 旧 `D2`（`Check(seesB, …)`）**必红**，且它引用的 `B121_WC · COLL-5①` 判据已被 `CardArmyFilter__SetFilterToDeck.c:20-25` 的早退句推翻 |
| 改法 | ① `D2` **翻极性** → `Check(!seesB2, …)`：「有督军 + 筛选里残留阵营 B ⇒ **别的阵营照旧看不见**」（判据 `SetFilterToDeck.c:20-25` + `FilterToArmy.c` 末句），🧨 注「把 `armyPicked` 写回来 ⇒ 本句红」；② 新增 `D2a`（A 阵营自己的卡**在** —— 锁的是阵营不是清空池子）；③ 新增 `D2b`（**灭自证对照**：同一份筛选换成**收藏窗那一路**（不开 `WarlordGatedPool`）⇒ B **看得见**）——🧨「废掉 `FactionOk` / 把锁做成全局 ⇒ D2b 红」，与 `D2` 成对；④ `D3` 措辞由「看得见 ≠ 加得进」改为「**看不见 ≠ 加得进**」（`InvalidArmy` 那条路未变）；⑤ **文件头** `COLL-5①` 那一行按铁律 5 就地订正（写清旧口径被哪句 `.c` 推翻、`armyPicked` 已删）；⑥ 灭自证清单里 `D2/D3` 那两行改写为 `D1/D2 × D2b` 的成对关系；⑦ D 段头加 `// ✅ 已复刻（2026-11-04 · B141 · …）` |
| 行数变化 | `Editor/B121_WC_Assert.cs` **220 → 251 行（+31）**；该文件**没有任何账落点**（`grep` 全部 `账/*.tsv` = 0 命中）⇒ 行号漂移无影响 |
| 断言数 | D 段 3 条 → **4 条**（D1 / D2 / D2a / D2b / D3 = 5 条；原为 3 条） |
| 类型检查 | 改后 `0/0`（运行时 + 编辑器） |
| 未同步的地方 | `Editor/B141_W30_Assert.cs` **不动** —— 它自己的 `L1/L2` 已经是同一对灭自证（锁住看不见 × 收藏窗看得见），两处不是同一份判据的重复实现（一个是 `B121_WC` 的 `COLL-5①` 验收段、一个是本包的 `3-02` 验收段），各自钉自己的回归对象 |
