# AUD07 · `Loc.HasEntry` 窄谓词逐调用方普查（`B105_R6` · 2026-10-31 现读）

> 只读侦查代理 `B105_R6` 产出。**未改任何 `.cs` / 账 / 正本 / 工具**；本件 + 同名 `.tsv` + `报告/B105_R6.md` 三份是全部产出。
> 逐条明细 → 同目录 `AUD07_窄谓词普查.tsv`（41 行，每行带 `文件:行 / 键 / 档位 / 调用形状 / 判定 / 理由`）。
> 前一版（`B103_R3` · 2026-10-30）的普查与五次执行记录 → `AUD07_loc_keys.md` / `AUD07_loc_keys.tsv`（**仍然有效，本件不重复它的判据推导**，只做**现读复核 + 对账**）。

---

## 一、结论（数字全部现读 · 扫描面 = `Unity/MyGame/Assets/**/*.cs` 共 1,020 个文件）

| 项 | 现读数 |
|---|---|
| `HasEntry` 原始命中行（含注释 / 断言消息串） | **174 行** |
| 其中**注释 + 字符串字面量**（已排除） | **120 行** |
| 落在**代码**里的行 | **54 行** |
| 去掉字符串内容后**真正的调用表达式** | **43 处 / 43 行** |
| 其中 `Core/Loc.cs` 内部（定义 `:3593` + `HasAnyTerm` 本体 `:3963`） | **2 处** |
| **★ 剩下需要判语义的外部调用点** | **41 处 / 18 个文件** |
| 现读 `Loc.HasAnyTerm`（代码） | **158 处 / 145 行** |
| 现读 `Loc.HasBulk(`（代码） | **15 处** |

### 判定统计（41 处）

| 判定 | 条数 | 说明 |
|---|---|---|
| **应换 `HasAnyTerm`** | **1** | `Core/CardText.cs:133`（`KeywordDisplay`）—— 见 §五 P0 |
| **该保持窄** | **38** | 全部是「Table 两列取值口 / 显式三档链 / 删除·孤儿·注册表哨兵 / 契约测试」 |
| **判不了** | **2** | `Shell/SettingsWindow.cs:2379 / 2410` —— 不是缺信息，是**哨兵口径要一句话裁定**（见 §四·C） |

### 按文件分布（41 处）

| 文件 | 处数 | 应换 / 窄 / 判不了 | 说明 |
|---|---|---|---|
| `Editor/BattleScene.cs` | 7 | 0 / 7 / 0 | 1 条 `Battle/Tips` 缺键计数 + 6 条 W62 删除哨兵 |
| `Core/CardText.cs` | 5 | **1** / 4 / 0 | 只有 `:133` 该换；`:120/:149` 取 `ZhOf/EnOf`，`:750` 是字体闸，`:766` 是三档链 |
| `Editor/DeckScene.cs` | 5 | 0 / 5 / 0 | A1025 + A1852 前提、删除哨兵、`HasEntry(null)` 契约、负例 |
| `Editor/SettingsScene.cs` | 5 | 0 / 5 / 0 | 登记哨兵 3 + 孤儿哨兵 1 + A1025 1 |
| `Editor/CardBaseDemo.cs` | 3 | 0 / 3 / 0 | 注册表完备哨兵（宽化会变空转） |
| `Editor/MainMenuScene.cs` | 2 | 0 / 2 / 0 | A1025 前提 + 覆盖度统计（官方比官方） |
| `Editor/NetSelfTest.cs` | 2 | 0 / 2 / 0 | P6 双语前提（`Settings/Online/*` 全在 Table） |
| `Shell/FTUESelectDeckWindow.cs` | 2 | 0 / 2 / 0 | 两条显式三档链 |
| `Shell/SettingsWindow.cs` | 2 | 0 / 0 / **2** | 快路径 + 回归哨兵（显示口已在末句宽化） |
| `Core/CardIcons.cs` · `Core/CardView.cs` · `Editor/B99A3_Assert.cs` · `Editor/CollectionScene.cs` · `Editor/ShellScene.cs` · `Editor/ShopScene.cs` · `Shell/BaseEventPopup.cs` · `Shell/LiveOpsLocalCatalog.cs` | 各 1 | 0 / 各 1 / 0 | 来源选择器 / 老键 / 哨兵 / A1025 / 显式链 |

- **运行时（`Core/` + `Shell/`）= 13 处**，`Editor/` 自检宿主 = **28 处**。
- ⚠️ **判定不按「键今天在不在表里」**，按**用途**：见 §三。

---

## 二、对账：为什么现读是 41 处，而不是「198 处」

任务简报里的「约 198 处未看语义、只修了 3 处同一文件」**是过期的**（铁律 5）。现读证据链：

| 时间 | 事件 | 数字 / 出处 |
|---|---|---|
| 第一百零一会话收尾 | 待办里写下「`Loc.HasEntry` 其余 **198** 处（AUD07）」 | `调度台日志.md:10849`（**这句话就是 198 的来源**） |
| 第一百零三会话 · `B103_R3` | 做了**全量普查**：剥掉 139 行注释后 = **177 行 / 190 处调用表达式**；**138 该宽 / 39 该窄 / 0 判不准** | `AUD07_loc_keys.md` §一、`AUD07_loc_keys.tsv`（177 数据行，宽 138 / 窄 39） |
| 同会话 · 五次执行 | 138 处里逐批改成 `HasAnyTerm`：运行时 26（含 `BattleDriver` 5）+ 宿主 103 + 镜像断言 5 + `DeckRuntime` 1 | `AUD07_loc_keys.md` §七～§十一；提交 `fbf02b8f`（第一百零三会话） |
| 现读（2026-10-31） | 代码里 `HasEntry` = **43 处**（41 外部 + 2 `Loc.cs` 内部） | 本件 §一 |

**136 / 138 条「宽」行已原样落地**；唯一两行没按原样换的是 `Shell/SettingsWindow.cs:2379 / 2410`——见 §四·C。
**没有发现「改宽之后又被改回窄」的回归**（逐文件核对：每个文件的 `HasAnyTerm` 代码处数 ≥ 它在那张表里的「宽」行数；抽查 3 处落地形态 `MainMenuScene:8190` / `ProfileData:1650` / `CardView:5474` 均正确）。

---

## 三、判断口径（复现用 · 与 `B103_R3` 一致）

1. **用途判据**（不是看键今天在不在表里）：
   - 用途 = 「**有就给玩家看**」，取值口是 `Loc.T` / `CardText.Term` / `TermOr`，兜底是硬编码句 / 空串 / 英文原值 ⇒ **该宽**（三档一起算）。
   - 用途 = 「**取 Table 的某一列**」`ZhOf` / `EnOf`·「**判我们登记了没有**」的哨兵·「**区分来源**」的档链 ⇒ **该窄**。
2. **自检宿主默认规则**：前提断言若后面逐语档比 `Loc.T` 非空且 ≠ 键名 ⇒ 真正的保证是「取得到文案」= 宽。**四个例外判窄**：① 后面用 `EnOf/ZhOf`；② 删除 / 孤儿 / 契约哨兵；③ 覆盖度统计（宽化 = 官方比官方）；④ 注册表完备哨兵（宽化 = 放水成假绿）。
3. **数据面**：`official_loc_13lang.json` 是**列式**（先取 `keys`，现读 **10,862**）· `official_names.json` = `entries[].k`（现读 **3,263**）· 手写 `Table` 按 `{ "键", new Entry(` 解析（现读 **755** 条）· `HasAnyTerm` 第三档按 **en 列**判。
4. **扫描口径**：**二进制读**（裸 CR 会让按行号的一切工具算错）；先剥 `//` 与 `/* */`，**再把字符串内容置空**（否则断言消息里的 `Loc.HasEntry(...)` 会被算成调用点，实测多出 11 行）。

---

## 四、逐条判定的分组（41 处的归类；逐条见 TSV）

### A. 该保持窄 · 取 Table 两列 / 字体闸（7 处）
`CardText:120`（`KeywordZh`→`ZhOf`）· `CardText:149`（`KeywordEn`→`EnOf`）· `CardText:750`（字体闸：宽化会把 bulk 键送进 `EnOf` 返 null）· `CardText:766`（`TermOr` 第一档，后面 `Lang13Current→OfficialCurrent` 已覆盖）· `CardIcons:705`（来源选择器，`Lang13Current`/`HasBulk` 已分层）· `CardView:5477`（老键）· `B99A3_Assert:311`（别名登记哨兵 + 紧跟 `KeywordZh`）。

### B. 该保持窄 · 显式三档链 / 删除·孤儿·注册表哨兵 / 契约测试（32 处）
- **档链已写全**：`BaseEventPopup:463`（bulk→Table→OfficialCurrent）· `LiveOpsLocalCatalog:991`（bulk→Table→Official→Lang13）· `FTUESelectDeckWindow:419 / 589`（`Lang13Of` / SO `deckName` 收尾）。
- **删除哨兵**：`BattleScene:44868 / 44870 / 44880 / 44881 / 44882 / 44883` · `DeckScene:6992`（这 8 条的键**三档皆 0**，宽窄同真值，但问的是「我们删干净没有」）。
- **契约测试**：`DeckScene:7015`（`HasEntry(null)` 不抛 + 返 false）· `DeckScene:7022`（虚构键返 false）。
- **注册表 / 缺键计数**：`BattleScene:7876`（24 条里只缺官方也没有的那两条）· `CardBaseDemo:4969 / 4978 / 5450`（`Card_Name/*` 手写 26 vs 官方 1198、`Armies/*` 16 vs 20、`Card_Race` 会印的 19 种）。
- **A1025 族 6 处**：`DeckScene:1266` · `CollectionScene:12305` · `MainMenuScene:16916` · `SettingsScene:5584` · `ShellScene:8275` · `ShopScene:5796` —— 逐条核过**键 100% 在 Table**（37 / 50 / 22 / 80 / 89 / 6 条），且②紧接着用 `EnOf/ZhOf` ⇒ 前提必须窄。
- **其它**：`DeckScene:1310`（A1852 前提 + `EnOf/ZhOf`）· `MainMenuScene:16976`（覆盖度统计）· `NetSelfTest:165 / 173`（`Settings/Online/*` 91 条全在 Table，`EnOf` 断 CJK）· `SettingsScene:1324 / 1325 / 1326`（登记哨兵）· `SettingsScene:4717`（孤儿键哨兵）。

### C. 判不了 · 哨兵口径需裁定（2 处）
`Shell/SettingsWindow.cs:2379`（`AcTerm`）与 `:2410`（`UiTerm`）—— 这两个函数**各有两处谓词**：

```csharp
if (Loc.HasEntry(key)) return Loc.T(key);                       // ← 被点名的这行，仍是窄
if (!_acMissingKeys.Contains(key)) { _acMissingKeys.Add(key); /* warning */ }
return Loc.HasAnyTerm(key) ? Loc.T(key) : origEn;               // ← 显示口在这里，已经是宽
```

- **显示面今天不会印键名**：末句宽，`Loc.T` 现在也吃 bulk / 13lang。
- 被点名的这行剩下的**唯一职能**是「**我们手写表缺这条键**」的**回归哨兵**（doc：`A1185` 之后运行时应当是**空表**）。
- ⇒ **改宽 = 关掉哨兵**；**不改 = 旧 tsv 那两行「宽」标签失真**。这是口径选择、不是事实缺失，需要调度台一句话。
- 现读数据：运行时实际引用的 6 条 `Settings/Account/*` + 8 条 `Settings/Support/*` + `Settings/Media/*` 5 + `CardTextInHand/*` 3 + `Graphics/*` 17 **全在 Table** ⇒ **今天两选零可见差异**。
- ⚠️ 但 `Settings/Account/*` 在官方全表里有 **7 条我们没建的**（`ConfirmSwitchAccount` / `ForgotPasswordPrompt` / `NewPassword` / `NewPasswordConfirmation` / `PlayerId` / `ResetEmailSent` / `ResetPasswordPrompt`）：**哪天有人在运行时引用它们，哨兵就会亮**。这是「保持窄」唯一要付的代价。

---

## 五、优先施工清单（按「会不会把键名/裸名印到屏幕」排序）

| 位次 | 位置 | 现状后果 | 改法 | 风险 / 配套 |
|---|---|---|---|---|
| **P0** | `Core/CardText.cs:133`（`KeywordDisplay`） | **31 条 `Card_Trait` 不在手写 Table**（27 条在 bulk+13、4 条只在 13lang）⇒ 该函数返 `null`，调用方 `CardDisplayWindow.TraitRowText:2460` / `RemoveTraitRowText:2473` 回落成 `e.Trait` ⇒ **卡面/效果行印裸 canonical 名**（如 `concussive`）。卡池里 `concussive` 出现在 **6 张卡**（`ASH75 / GSC32 / GSC28 / GOF68 / GOF63 / GOF26`）。 | 该行 `Loc.HasEntry(t)` → `Loc.HasAnyTerm(t)`，**同行替换、不插行**。`Term()` 自己三档齐（`Lang13Current` → `Table` → `Loc.T` 兜 bulk/13）。 | 已核：① 已 grep 全仓，**无断言钉「`KeywordDisplay` 必须返 null」**（唯一相关是 `CardBaseDemo:5025` 钉 Table 键 `armour` = `Armour`，不受影响）；② 这 31 条的官方值是**纯文本**（实测 `Card_Trait/concussive` = `震荡的`/`Concussive`，`sneak`/`jam`/`rage` 同），**不带 `[[ ]]`/sprite 记号** ⇒ 不触发 `BulkOr` 那套渲染闸；③ 三档皆无的键仍返 null，与今天一致。⚠️ 边界：`Term():750` 对「非 Table 键」**绕过字体闸**，于是「中文档 + 中文字体资产缺失」时会回中文（潜在方块）—— 但该边界**对今天所有走 `Term` 的 bulk 键已经存在**（同 `TermOr:777-780` 记录的那一条），**不是本次改宽新引入的类**。 |
| **P1** | `Shell/SettingsWindow.cs:2379 / 2410` | 不会印键名（末句已宽）。代价 = 回归哨兵可能误报 + 旧 tsv 标签失真。 | **不改代码**，或**改宽**——二选一。 | 需要调度台裁定（§四·C）。若选择「不改」，至少把 `AUD07_loc_keys.tsv` 这两行的「宽」订正成「显示口宽 / 首句守哨兵」（铁律 5）。 |
| **P2** | 其余 **38 处** | **零行为变化**（键要么全在 Table，要么三档皆无；且多为自检哨兵）。 | **⛔ 不要批量改**。 | 批量宽化会把 A1025 前提 / 注册表哨兵 / 契约测试**放水成假绿**，是本件最需要防的错。 |

---

## 六、覆盖边界（哪些没查到判据 / 本件没做的事）

1. **⛔ 没有跑 Unity、没有运行时观测** —— 41 条全部是**静态读代码 + 数据面核对**得出的。凡是「运行时会不会真走到」的问题，本件只写「可达性未证」。
2. **动态键**：`CardIcons:705` / `CardText:120,133,149,750,766` / `CardView:5477` / `CardBaseDemo:*` 的键是**变量**，我按**可产生的键族**（`Card_Trait/*` / `Card_Race/*` / `Card_Name/*` / `Armies/*`）核的档位；**逐张卡逐条实例**没有枚举。
3. **`KeywordDisplay` 的实际命中面**：只证到 `concussive` 出现在**卡池 `cardTraits`**（6 张卡）；**其余 30 条 not-in-Table 的 trait 是否会被引擎塞进 `e.Trait`（效果清单）本件没查**，也没枚举 `buffType` 表里的关键词字符串。⇒ 「印裸名」的**当前真实条数**只写「≥1 类（`concussive`）」，不写「31 条都印」。
4. **前 137 处宽化的复核方式** = 「逐文件 `HasAnyTerm` 代码处数 ≥ 旧表『宽』行数」+ 3 处落地形态抽查（`MainMenuScene:8190` 已宽且带说明、`ProfileData:1650` 已宽、`CardView:5474` 已宽）。**没有**把 138 行逐条重推语义（那是 `B103_R3` 已做的，重推 = 重复劳动）。
5. **`Shell/SettingsWindow` 哨兵该不该守 Table** 是**口径问题**，本件给证据不给结论（§四·C）。
6. 行号是**当前工作树**（2026-10-31）的；当日多文件有未提交改动，行号会随后续编辑漂移 ⇒ 施工前**现读一次**。

---

## 七、顺手发现（不在本件结论内，供调度台分流）

1. **`Card_Trait/*` 31 条不在手写 Table**（现读：Table 65 / bulk 27 / 13lang 83；其中 **27 条在 bulk+13**、**4 条只在 13lang**：`bloodThirst` / `cantAttack` / `huntMark` / `spiritStone`）。
   - `KeywordZh`（`ZhOf`）与 `KeywordEn`（`EnOf`）对这些**恒返回 null**，**不是换谓词能修的**（要改取值路：`Loc.OfficialCurrent` / `Lang13Current` + 字体闸）。⇒ 建议另立账。
   - 与 P0 的关系：`KeywordDisplay` 换宽**能修显示名**（走 `Term`），但 `CardText.KeywordSegment`（`:343-345`）那条**行内关键词**仍走 `KeywordZh/KeywordEn` ⇒ 那条路今天对 31 条仍会缺名。**两件事别混**。
2. **旧 tsv 的两行标签与实现不符**（`SettingsWindow:2379/2410` 标「宽」、代码仍窄，且同函数末句已宽）。`AUD07_loc_keys.md` §七 有刻意说明，但 **tsv 本身没订正** ⇒ 下个会话读 tsv 会重新查一遍。
3. **`_acMissingKeys` 的「应当空表」是脆弱断言**：官方 13 语言表里有 **7 条 `Settings/Account/*` 我们没建**（见 §四·C）。今天运行时只引用 6 条 const（全在 Table）⇒ 不触发；但这条 doc 的成立依赖「没人引用那 7 条」。
4. **`Cosmetic_Description/*` 三档 0 条，但不构成缺陷**：`ProfileData.GetLocalizedDescription` **零消费者**，而且原版 `GetTranslation` 的兜底就是「回键名」⇒ 我们的行为与原版一致（`ProfileData.cs:1614-1617` 已写明）。
5. **`HasBulk` 窄口（15 处）**：`BaseEventPopup:462` / `LiveOpsLocalCatalog:990` / `CardIcons:710` / `CardView:5469` 都是**显式链的一档**，正确；**`ProfileData:911-918`** 的 `r→n→art` 三级兜底**只用 `HasBulk`、没有 13lang 档** —— 键族是 cosmetic id，**是否该补第三档本件没核**（另立账）。
6. **注释里的示例键确实是假阳性源**：`CardDisplayWindow:2632/2679`、`ChatPopupPanel:1506-1509`、`ErrorMessageBanner:189/664`、`SkillPanel:2132/2215` 都是「⛔ 别改用 `Loc.HasEntry`」的告示 —— **方向与本件一致**，但**不是调用点**，无脑 grep 会把它们数进去（120 行里的代表）。

---

## 八、复现方法

1. **扫描**（跳过注释 + 字符串内容）：本轮用的一次性五态字符机（**未入仓**；状态 = `code` / `//` / `/* */` / `"…"` / `@"…"` / `'…'`，逐字符推进；字符串内容整体置空后再数 `HasEntry`）。
2. **数据面**：`official_loc_13lang.json` 取 `keys`（列式）· `official_names.json` 取 `entries[].k` · `Loc.cs` 按 `\{\s*"…"\s*,\s*new Entry\(` 解析（现读 755 条）。
3. **对账**：把 `AUD07_loc_keys.tsv` 的 138 条「宽」行按文件分组，与现读 `HasAnyTerm` 处数逐文件比；再对每条「宽」行查 ±12 行内是否仍有 `HasEntry`（只剩 `SettingsWindow:2379/2410` 两处）。
4. **⚠️ 三条口径**：① 二进制读文件（裸 CR 会偏行）；② **必须先剥字符串**，否则断言消息里的 `Loc.HasEntry(...)` 会被多数 11 行；③ `HasEntry` 的**定义行**（`Loc.cs:3593`）与 `HasAnyTerm` 本体（`:3963`）不算调用点。
