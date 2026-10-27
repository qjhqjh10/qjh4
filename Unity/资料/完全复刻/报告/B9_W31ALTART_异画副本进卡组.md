# B9_W31ALTART · `HUD:92/93`（战斗侧异画）的**卡组层那一半**

> 执行代理 `B9_W31ALTART` · 2026-10-27 · 本件**只做卡组数据层**（视图侧早就是好的，`Battle/BattleDriver.cs` 归 W29 ⇒ 一行没碰）。
> 类型检查：`TMPDIR=/tmp/wf_w31 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（0/0，最后一次在所有改动落盘之后跑）。
> 引擎对拍：`bash 工具/ruleprobe.sh check` ⇒ **rc=0**（全池 1121 张 · 解析差异 0 行）·
> `bash 工具/ruleprobe.sh play check` ⇒ **rc=1，但只有 1 张卡 `BL64`，且不是本件的**（归因见 §二·5，本件一行都没碰它）。

---

## 一、结论

**卡组层那一半做完了**：`PlayerDeck` 现在能表示「这一份是异画副本」，而且**校验 / 摘不合法卡 / 展开进对局 / 分享串进出 / 卡组编辑器**五个口**全都认它**，
解析后**一律当原卡那张**（张数、份数上限、阵营、引擎里那张 `CardDef` 一个都不变，只有立绘走异画）。
**但 `HUD:92/93` 仍不能转 A** —— 剩下的两件都**卡在本件白名单外**，见 §一表最后一行。

| 项 | 改前 | 改后 |
|---|---|---|
| `PlayerDeck.CardIds` / `WarlordId` 里能不能放 `#HB_SW1` | 放进去 = 垃圾 id（`Validate` ⇒ `UnknownCard`、`RemoveInvalidCards` **摘掉**、`FromDeck` **丢牌**、编辑器**少一行**） | ✅ 认；解析回原卡 |
| 一副「1 督军 + 30 张、其中 1 份是异画」的卡组 | 展开成 **30 张**（少一张，静默丢） | ✅ 展开成 **31 条**（数量与改前那份**非异画**卡组逐条相等） |
| 份数口径（`CountOf` / `RemoveCard`） | 普通 1 + 异画 1 数成 **1 份** | ✅ 数成 **2 份**（= 原版按 `GetOriginalId()` 比） |
| 分享串（`ExportString` → `ImportString`） | 异画那条会被**丢进 `LastDroppedIds`** | ✅ 往返后仍是异画（导出写原版克隆 id `HB_SW1`，导入归一成 `#HB_SW1`） |
| 卡组编辑器（打开一副带异画的卡组） | 那一行**不显示**、份数/类型计数少 1 | ✅ 正常显示（`Find` 走同一条解析） |
| 🆕 自检件 | —— | ✅ `AltArtDeckSelfCheck.Run(...)`（改前必红，见 §二·4）；**宿主还没接**（改动请求 ①） |
| **`HUD:92/93` 能否转 A** | B | **仍 B** —— ① `BattleDriver.CollectAltArtCopies` 只扫 `CardIds`，而**本地 7 张异画全是督军**（住在 `WarlordId` 槽）⇒ 登不上记；② 没有任何入口把异画条目放进卡组。**两件都不在本件白名单**（① = W29 的 `BattleDriver.cs`；② = 收藏窗异画页签，W16） |

---

## 二、证据（文件:行号 · 行号 = 本件改完后的现读）

### 2.1 原版到底怎么表示「异画副本」（**先查原版，没有按我们的 `#` 口径反推**）

| 事实 | 出处 |
|---|---|
| `RawCardScript` 有两格：`alternateArts : AlternateArtCard[]`（+0x80）· `currentAlternateArt : AlternateArtCard`（+0x88）· `altArtFromId : string`（+0x178） | `d:/2/tools/il2cpp_out/dump.cs:23244-23310` |
| `IsAlternateArt()` = `currentAlternateArt != null`（**不是**「有没有 alternateArts 数组」） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:473-476`；Ghidra 同族 `decomp_full/RawCardScript__IsAlternateArt.c`（读的正是 +0x88） |
| `GetOriginalId()` = **原卡 id**：`currentAlternateArt` 为空 ⇒ 自己的 id，否则 ⇒ 它的 `originalCard`（+0x48）的 id | `d:/2/tools/decomp_full/RawCardScript__GetOriginalId.c`；与 ILSpy `RawCardScript.cs:2353` 那句 `altArtFromId ?? uniqueId` 同源（`altArtFromId` 只被这一族读） |
| **卡组 `CardDeck.cardLibrary` 是 `List<RawCardScript>`（+0x58）⇒ 存的是对象引用**，「这一份是异画」**天然带着，没有额外的列**（卡组数据层没有「id 前缀」也没有「另一列」） | `d:/2/tools/il2cpp_out/dump.cs` 的 `CardDeck` 字段表（`cardLibrary // 0x58`）· `CardDeck__GetLibraryInFull.c` |
| 「同一张卡」判据 = `GetOriginalId()`：份数 `CardCopiesInDeck`（督军那一支 + `Enumerable.Count` 的谓词）· 「在不在卡组里」`Has` 同一条 | `d:/2/tools/decomp_full/CardDeck__CardCopiesInDeck.c`（逐句）· ILSpy `CardDeck.cs:51-65`（`b__0`：`c.GetOriginalId() == card.GetOriginalId()`）· `CardDeck__Has` 同型 |
| 背包里那件物品是 `AlternateArtCard`，`uniqueId = "#HB_<原卡 id>"`（本地 7 条真值） | `d:/4/Unity/数据/索引/altart_index.json`（7 条：`#HB_AM2`/`#HB_BL34`/`#HB_DA1`/`#HB_SAU35`/`#HB_GOF28`/`#HB_SW1`/`#HB_UM76`）· 卡表那一列 = `RuleEngine/Resources/cards_engine.json` 现读 **7 张**带 `alternateArts` |
| 「物品 id → 卡资产 id」= `GetIdForClonedCard()` = `uniqueId.Replace("#", "")` ⇒ `HB_SW1`；原版**分享串**解析时按这个 id 去 `AssetLocator.GetAsset<RawCardScript>` | `d:/2/tools/decomp_full/AlternateArtCard__GetIdForClonedCard.c` · ILSpy `AlternateArtCard.cs:80-90` · `CardDeck.DeserializeDeckString`（ILSpy `CardDeck.cs:1737` `AssetLocator.GetAsset<RawCardScript>(array2[obj])`） |

**⇒ 原版表示法一句话**：异画副本 = **另一张 RawCardScript**（`currentAlternateArt` 非空、`altArtFromId` = 原卡 id），
卡组存的是**对象引用**；它在规则上**就是原卡**（比 `GetOriginalId()`），只在画面上换立绘。

### 2.2 我们的表示（🔶 自己设计，已在代码里打标）

我们的卡组存的是**字符串 id**（没有对象引用可用）⇒ **`#` 前缀 + `AlternateArtCard.uniqueId` 那一套**：`#HB_<原卡 id>`。

| 事实 | 出处 |
|---|---|
| 完整判据 / 我们为什么这么挑 / 与原版的差异与「没查清」 | `RuleEngine/Core/DeckRules.cs:800-847`（`PlayerDeck` 类尾那段注） |
| 识别 · 剥原卡 id · 份数键 · 解析 · 外来 id 归一 | `DeckRules.cs:853`（`IsAltArtEntry`）· `:861`（`AltArtBaseIdOf`）· `:871`（`SameCardKey`）· `:881`（`ResolveEntry`）· `:899`（`NormalizeEntryId`） |
| 份数 / 删一张（= 原版 `CardCopiesInDeck`/`Has` 的 `GetOriginalId` 口径） | `DeckRules.cs:785`（`RemoveCard`）· `:793`（`CountOf`） |
| 校验 / 摘不合法卡（督军·防御卡·卡位三处 + 逐张那一轮） | `DeckRules.cs:456`/`:457`/`:461`（`Validate` ①②）· `:414`/`:435`（`RemoveInvalidCards`） |
| 展开进对局（`#` 条目不再丢 `skipped`；命中时先按整串、再按剥出来的原卡 id） | `RuleEngine/Data/DeckBuilder.cs:226`（`FromDeck`）· `:400`（`LookupAny`，异画感知的查卡口） |
| 分享串：导出写**原版克隆 id**（`HB_SW1`，去掉 `#`）· 导入把 `HB_SW1` 归一成 `#HB_SW1` | `RuleEngine/Data/DeckLibrary.cs:429`（`ExportString` → `:586` `RawEntry`）· `:527`（`SplitByType`） |
| 卡组编辑器：一行都靠 `Find`，所以只改它一处就全覆盖（行/份数/费用曲线/类型计数/督军校验） | `CardPresentation/Deck/DeckEditorState.cs:251`（`Find` → `:1087` `FindPool`）· `:510-514`（`TryRemove` 按同一张卡认）· `:492`（`SetWarlord` 保住异画标记） |
| 视图侧（早就是好的，本件未动） | `Battle/BattleDriver.cs:34547`（`CollectAltArtCopies`）· `:34536`（`AltArtOverrideOf`）· 两处建视图 `:27105` / `:27804` |
| 本地 7 张异画的卡 id 与「全是督军」 | `cards_engine.json` 现读 7 条 `alternateArts`，宿主卡 = `AM2/BL34/DA1/SAU35/GOF28/SW1/UM76`，7 张 `type == hero`（`altart_index.json` 的 `name` 全是 `AA_HB_<阵营>_war_<督军名>`） |

### 2.3 账的落点（本件没有制造行号漂移）

`DeckRules.cs` 的所有改动**行数中性**（`Validate`/`RemoveInvalidCards` 只在原行内换调用）；
`DeckLibrary.cs` 只在 `ExportString` 体内换行（3→3）并在**最后一个成员之后**追加；
`DeckEditorState.cs` 同上（新增 `FindPool` 与 `AltArtDeckSelfCheck` 都在**类尾/文件尾**）。
**逐条复核过**：`DeckRules.cs` 406/449/584/694/762/772/778/785/793 · `DeckLibrary.cs` 141/187/297/429/452 · `DeckEditorState.cs` 251/385/398/407/473/492/510/857/983 —— **全部仍在原行号上**。

### 2.4 断言（自检件已写，宿主待接）

`CardPresentation/Deck/DeckEditorState.cs:1113`（`public static class AltArtDeckSelfCheck`，`Run(check, pool, lookup, warlordId)`），11 条，**改前必红**的 4 条各自独立：

1. 前提①：卡池里有那张督军、同阵营有单位卡（**前提不成立就出声返回**，⛔ 不让结论句空转变绿）。
2. 前提②（**灭自证**）：`#HB_SW1` / `#HB_<单位卡>` **本身不在卡池里** —— 否则「解析成功」可能只是撞上了池里的同名 id。
3. `RemoveInvalidCards` **保留**异画条目 / 保留**异画督军**（改前：按 `UnknownCard` 摘掉 / 清空督军槽）。
4. `Validate`：异画督军与普通督军**给同一个错误码**，且都**不是** `UnknownCard`（改前就是它）。
5. 份数：`CountOf` 两个 id 都给 2；`RemoveCard` 删一张只少一份（改前 1 / 删不掉）。
6. `FromDeck`：**张数不变** · 异画那一份展开成**原卡**（张数相同且都是 2）· 异画条目**不进 `skipped`** · 督军那一格是原卡。
7. 分享串往返后异画条目**仍是异画**（`ExportString` → `ImportString`）。

> **接线（改动请求 ①）**：在 `DeckScene` / `RuleEngineTest` 的断言段加一行
> `AltArtDeckSelfCheck.Run(CheckTrue, pool, id => CardDatabase.FindById(pool, id), "SW1");`
> —— 本件白名单不含任何 `Editor/*.cs`，所以**没有自己接**（"写了断言但永远不跑" 比没有更糟，故单独列成请求）。

### 2.5 `ruleprobe play check` 的那 1 张红卡 `BL64` = **不是本件**（逐张归因）

- 读数：`①-a 1 张卡变了`，只有 `BL64`：`playRc 18 → 0` · `played 0→1` · `mech 0→3` · `other 0→5` · `energyDelta 0→-4` · `dmg 0→2`。
- `BL64 Murderous Desires` 的 `playRc 18` 是**已知真偏离**：它 `unitConditions` 的 `{role:target, cond:341 canUseDuty, ex:1}` 被 `ConditionEval` 的 `!u.DutyUsed` 配 `ex=1` 取反后**把目标池全排除** ⇒ 打不出去
  （判据 → `资料/完全复刻/报告/B9_E5ENG_引擎三条真偏离.md:287-291` + `报告/B9_R1DUTY_cond341.md`）。
- 而 `ConditionEval.cs` / `TargetCriteriaEval.cs` / `RuleCore.cs` / `EffectResolver.cs` **是 W27 的文件**，本件开工时它们刚被改过（mtime `18:44:58`–`18:46:36`，我跑对拍是 `18:55`）⇒ **BL64 现在能打了是因为 W27 修了 `cond=341`**，基线还没在同步点重钉。
- **反证（本件不可能影响这个读数）**：`工具/ruleprobe/Program.cs` 与 `PlayProbe.cs` 里 `DeckBuilder.FromDeck` / `DeckRules.Validate` / `RemoveInvalidCards` / `PlayerDeck` / `CountOf` / `RemoveCard` **grep 命中 0**（探针直接摆棋盘，不走卡组层）。
- ⇒ **没有改基线**（基线归调度台的同步点重钉）；`check` 那一条 rc=0、差异 0 行，可作本件的旁证。

---

## 三、没查清的部分

1. **克隆卡资产 `HB_<id>` 本地不存在**：`资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 行）里 `altArtFromId` **全空**、`uniqueId` 没有一个带 `HB_`/`AA_`。
   ⇒「原版**分享串**里写的是 `HB_SW1`」是**两条反汇编推出来的**（`GetIdForClonedCard` = `Replace("#","")`；`DeserializeDeckString` 拿这个 id 去 `AssetLocator.GetAsset<RawCardScript>`），**不是直接读到一条真的卡组串**。
   ⇒ 我按「两种写法**都认**」实现（`#HB_SW1` 与 `HB_SW1` 都归一成 `#HB_SW1`），导出时写 `HB_SW1`；**没有把这条当硬判据**去改卡表。
2. **`CardDeck.Serialize` 每张卡写的是哪个 id**（`uniqueId` 还是 `GetOriginalId`）**没定死**：Ghidra 里那两处都表现为同一个 vtable 槽 `+0x188` 的间接调用，ILSpy 在 `GetOriginalId` 上直接崩了（`IndexOutOfRangeException`）。
   我按「写 `uniqueId`」实现（否则 `DeserializeDeckString` 的 `GetAsset` 永远拿回原卡、异画在导出后静默消失 —— 那不合理）。
3. **我们**不校验「这张卡真的有异画」：`#HB_<任意合法卡 id>` 都会被接受（只有取不到 `Art/altarts/alt_<id>.png` 时由视图侧 `AltArtOverrideOf` **出声**）。
   要做严只能把 `cards_engine.json.alternateArts` 读进 `CardDef`（`Core/CardDef.cs` + `Data/CardDatabase.cs`，**两件都在本件白名单外**）⇒ 见改动请求 ③。
4. **原版卡组编辑器里「异画副本」是不是单独一张卡**（能不能与普通那张同时存在于一副牌里）：`CardCopiesInDeck` 比的是 `GetOriginalId()` ⇒ 两者**合计**占同名上限；但「编辑器列表里显示几行」我没读到判据（`DeckListDrawer` 比的是 `行.CurrentCard == 这张`，那是**对象**比较 ⇒ 原版**会显示两行**，而 `CountOf`/份数是 2）。
   我们这边 `DeckEntries` 按 `Id` 去重 ⇒ **只显示一行、份数 2**。⇒ **这一处我们的观感与原版可能不同**，判据不足，未动（不猜）。

---

## 四、改动清单（4 个代码文件 + 报告 + 账补丁）

| 文件 | 改了什么 | 行号（改后） |
|---|---|---|
| `RuleEngine/Core/DeckRules.cs` | ① `PlayerDeck` 新增 5 个静态口（`IsAltArtEntry` / `AltArtBaseIdOf` / `SameCardKey` / `ResolveEntry` / `NormalizeEntryId`）+ `AltArtPrefix` + 类尾整段判据注；② `CountOf` / `RemoveCard` 改走 `SameCardKey`；③ `Validate` ①②、`RemoveInvalidCards` ② 三处查卡改走 `ResolveEntry` | `:800-847`（注）· `:853`/`:861`/`:871`/`:881`/`:899` · `:785`/`:793` · `:456`/`:457`/`:461` · `:414`/`:435` |
| `RuleEngine/Data/DeckBuilder.cs` | `FromDeck`：督军 / 防御卡 / 卡位三处查卡改走异画感知口；异画条目**不再进 `skipped`**、单独一句日志；`MigrateDeckVerified` 也换成异画感知的 lookup（否则 `MigrateDefensive` 会把异画防御卡当垃圾清掉） | `:226`（`FromDeck`）· `:400`（`LookupAny`） |
| `RuleEngine/Data/DeckLibrary.cs` | `ExportString` 写**原版克隆 id**（`#HB_SW1` → `HB_SW1`）；`SplitByType` 把外来 id 归一（`HB_SW1` → `#HB_SW1`）后再按原卡分类 | `:429`（+`:586` `RawEntry`）· `:527` |
| `CardPresentation/Deck/DeckEditorState.cs` | `Find` 加第二段（异画条目按原卡查）；`TryRemove` 督军/防御卡按「同一张卡」比；`SetWarlord` 保住异画标记；类尾 `FindPool`；文件尾自检件 `AltArtDeckSelfCheck` | `:251`·`:510-514`·`:492`·`:1087`·`:1113` |
| `资料/完全复刻/报告/B9_W31ALTART_异画副本进卡组.md` | 本报告 | —— |
| `资料/完全复刻/账/_parts/B9_W31ALTART.tsv` | 账补丁 4 行（11 列 · 带表头）：`HUD:92`/`HUD:93` 两行（差异+落点+备注，**状态仍 B**）+ `卡组编辑` 的 `CardCopiesInDeck`/`Has` 两行（备注补份数口径） | —— |

**没碰**：`Battle/BattleDriver.cs`（W29）· `RuleEngine/Core/{RuleCore,EffectResolver,UnitState,ConditionEval,TargetCriteriaEval}.cs`（W27）· `CardPresentation/Deck/DeckRuntime.cs`（**实测 W34FEED 正在写**：18:53:49 还在变，`SetupDoneHighlight` 那段）· 任何 `*.unity` / `ProjectSettings/` / `CLAUDE.md` / `项目任务.md` / 任务总表 / 账主表 / 基线文件 · `工具/gen_cards_engine.py`（**没改**，所以**不会改池指纹**）。

### 改动请求（都不在本件白名单；按优先级）

1. **接线自检件**（调度台一行，首选 `DeckScene.Run`，也可 `RuleEngineTest`）：
   `AltArtDeckSelfCheck.Run(CheckTrue, pool, id => CardDatabase.FindById(pool, id), "SW1");`
2. **W29 · `Battle/BattleDriver.cs`（`HUD:92/93` 转 A 的关键）**：
   - `CollectAltArtCopies`（`:34547`）现在只遍历 `d.CardIds` ⇒ **加扫 `d.WarlordId` 与 `d.DefensiveId`**（7 张异画全是督军，不扫就等于永远不触发）。
   - 4 处**裸查 `WarlordId`** 的调用点（异画督军会查不到）——各换一个词就行，用 `PlayerDeck.SameCardKey(...)`：
     `:13032`（`WarlordFaction`，直接影响「开局用哪个阵营」）· `:5448`/`:5450`（`FirstSeatRules.WarlordInitiative`，现数据下都平局所以今天不显）· `:34649`（`AddTierOfDeckWarlord`）· `:34627`（`deckWarlord` 那行日志）。
   - 建议补一条**战斗侧**断言：带异画督军的卡组进战 ⇒ 那张 `CardView` 的 `artOverride` 非空且 ≠ 基础立绘；不带 ⇒ `null`（本件的自检只到「引擎层把条目认成原卡 + 保留异画标记」，**立绘那一跳在 `CardArt.AltArt`，归视图侧**）。
3. **卡表带一列 `CardDef.AlternateArts`**（`Core/CardDef.cs` + `Data/CardDatabase.cs`，生成器那边 `gen_cards_engine.py` **已经烘好了**）：让「这张卡真的有异画」可判，把「任意 `#HB_<合法 id>` 都接受」收紧成「必须在表里」。⚠️ 只改读侧即可；**别动生成器**（重生成会改池指纹，必须由调度台在无账写手窗口做）。
4. **入口**（今天唯一的进法是「导入原版分享串」）：收藏窗的**异画页签**（`Shell/CollectionWindow.cs`，W16）里把「当前风格下拥有的异画」做成可放进卡组的一张卡（对应原版 `AlternateArtCardCollectionTab`）。这条不落地，玩家就没有正常途径把异画放进卡组 —— `HUD:92/93` 也就只能靠导入触发。
5. **两个显示侧小口**（异画条目会**少算 1**，都靠 `CollectionData.Card(id)` 裸查）：
   `Shell/DeckInfoPopup.cs:922-924`（`CurWarlord`，异画督军 ⇒ 立绘位空）+ `:1801-1806`（卡表行，异画那一条被 `continue` 掉）；`Core/CostCurveDrawer.cs:113`（费用曲线少 1）。
   改法都一行：`find(id) ?? find(PlayerDeck.SameCardKey(id))`。

---

## 五、顺手发现（不改，只报）

1. 🔴 **`DeckRuntime.cs` 本波有人正在写**（`B9_W34FEED` 的 `SetupDoneHighlight` / `DoneShineProgress` 那段在 `:8947-8964`，mtime `18:53:49`）。本件所有卡组编辑器的改动都落在 `DeckEditorState.cs`（另一个文件），**没有碰 `DeckRuntime.cs`** —— 但后续谁要改编辑器显示（例如 §三·4 那条「异画显示几行」），得先跟 W34FEED 排文件所有权。
2. **`DeckStore` 的读档迁移不看异画**（`RuleEngine/Data/DeckStore.cs:187` 用的是 `LegacyCardIds.PoolLookup()` 裸口）：`MigrateDefensive` 会把「查不到、映射表里也没有」的 `DefensiveId` **当无法判定、原样保留**（`:1280-1290`，本来就不清），所以**今天无害**；但 `DeckRules.Validate` 在它内部已经是异画感知的（`:456-461`），所以带异画的存档**能过 `MigrateByDualSolution` 的双解闸**。⇒ 不需要改，只是记一笔：**将来若要「防御卡也能是异画」，`MigrateDefensive` 会把它清空**（`Report` 已写在这里）。
3. **`cards_engine.json.alternateArts` 这一列到现在为止「一条消费者都没有」**（生成器注释 `gen_cards_engine.py:4873` 自己写着「目前没有消费者」）。本件**也没有**直接读它 —— 我走的是 `#` 前缀 + 池内解析（理由 = 不新增 `CardDef` 字段，见改动请求 ③）。⇒ 那句注释今天仍然成立，别以为本件改了它。
4. `PlayerDeck.Clone()` 会原样带走 `#` 条目（`CardIds` 是 `new List<string>(cards)`，`:772`）⇒ **打开编辑器 / 存档 / 联机下发都不会掉异画标记**；这一条我核过（也是自检件 ⑦ 的前提之一）。
5. 本件新增的两个 `public static` 口（`PlayerDeck.ResolveEntry` / `NormalizeEntryId`）**是给卡组层以外的调用点唯一的入口**：⛔ 后续别在别处再写「`id.StartsWith("#")` 剥前缀」—— 指数级分叉就是这么来的（本件已在 `DeckRules.cs:846-849` 列了调用点清单）。
