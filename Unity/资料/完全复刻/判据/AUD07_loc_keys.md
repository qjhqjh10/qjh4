# AUD07 · `Loc.HasEntry` 逐调用方判据（宽 / 窄）

> 产出人：只读查判据代理 `B103_R3` · 2026-10-30 · 逐条明细见同目录 `AUD07_loc_keys.tsv`
> 开工前读的所有权表 = `资料/完全复刻/文件所有权_当前.md`「当前状态：第一百零三会话 · 波 1」（本代理只写这两份文件，未改任何 `.cs` / 账 / 正本）。

---

## 一、结论（数字全部现读）

| 项 | 现读数 |
|---|---|
| `grep -rn "Loc.HasEntry"` 命中行（**含注释**） | **265** |
| 去掉注释后含 `HasEntry` 的行 | **318 − 139 注释 = 179 行**（另有 2 行是 `Loc.cs` 的**定义**与宽谓词本体，**不计入**） |
| **真实调用点** | **177 行 / 190 处调用表达式**（13 行一行两处，`CollectionScene:3745` 一行三处） |
| 判「该宽」（`HasAnyTerm`） | **138 处**（分布在 38 个文件；其中 **108 处**在 `Editor/*.cs` 自检宿主、**30 处**在运行时） |
| 判「该窄」（保留 `HasEntry`） | **39 处**（运行时 11 / 自检 28；清单见 §三，每条带理由） |
| 判不准 | **0**（没有一条是「信息不足」）；另有若干条属「语义上该宽、今天却零行为变化」（键已在 `Table`），已在 TSV「风险」列逐条写明 |

> ⚠️ **「宽」不等于「今天会变」**：138 处里只有 **4 处是今天就错**（§四），其余绝大多数今天**零行为变化**（键已在手写 `Table` 里）。它们该改的理由是**语义与取值口对齐**（`HasEntry` 答的是「手写表有没有」，不是「玩家看不看得到文案」——见 `Loc.cs:3572-3582` 自己那段订正）。
> ⚠️ **注释里的 237 处示例键**确实会把无脑 grep 变成假阳性：本轮按「源码行 / 注释行」逐行分离（块注释 `/* */` 与行注释 `//` 都跳过，字符串内的 `//` 不误判），**139 行注释全部排除**，例如 `CardDisplayWindow.cs:2632/2679` 那两句「⛔ 别改用 `Loc.HasEntry`」的告示不在清单里。

---

## 二、判断口径（复现用）

1. **目的判据**（不是看键今天在不在表里）：
   - 用途 = 「**有就给玩家看**」（取值口是 `Loc.T` / `CardText.Term` / `TermOr`，兜底是硬编码句、空串或英文原值）⇒ **该宽**。
   - 用途 = 「**只认手写表 / 区分来源 / 取 Table 的某一列**」（后面紧跟 `ZhOf` / `EnOf` / `HasBulk` 档链、或判「我们登记了没有」的哨兵）⇒ **该窄**。
2. **自检宿主的默认规则**：前提断言的形式是「词条在表里（⛔ 不在 ⇒ `Loc.T` 返回键名 / 两边一起退化成键名 = 假绿）」，而**比较基准是 `Loc.T`** ⇒ 真正的保证是「**取得到文案**」= `HasAnyTerm`，故判**宽**。
   - 例外一律判窄：① 后面用 `Loc.EnOf` / `Loc.ZhOf`（只读 Table，宽化会让那几条失去前提或直接取到 null）；② 删除 / 孤儿 / 契约哨兵；③ 覆盖度统计（宽化会把统计口径变成自我循环）；④ 注册表完备哨兵（宽化会放水成假绿）。
3. 数据面核对（判断「今天有没有真差别」）：
   - `official_loc_13lang.json` 是**列式**存储（`keys` / `en` / `es` … 各 10,862 条）⇒ 判成员先取 `keys`；`official_names.json` 是 `entries[].k`（3,240 条）。
   - 手写 `Table` 的边界现读 = `Core/Loc.cs:274-2934`，按 `{ "键", new Entry(` 解析出 **755 条**。
   - 核对过的族见 §五（这张表就是「哪一族会被窄谓词坑」的地图）。

---

## 三、必须保留窄谓词的 39 处（按理由分组）

### A. 后面取的是 **Table 的两列**（`ZhOf` / `EnOf`）—— 宽化会直接取到 null 或让断言失去前提（18 处）
| 位置 | 为什么必须窄 |
|---|---|
| `Core/CardText.cs:120 / 133 / 149` | `KeywordZh` / `KeywordDisplay` / `KeywordEn` 分别取 `ZhOf` / `Term`(内部仍靠 Table) / `EnOf`，三处都是「Table 的两列」 |
| `Core/CardText.cs:750` | **字体闸**：`HasEntry` 决定走「`Zh ? Loc.T : EnOf`」那一支；宽化会把 bulk/13lang 键送进 `EnOf`（**返回 null**）＝真回归 |
| `Core/CardText.cs:766` | `TermOr` 的第一档分派：后面 769 已显式走 `Lang13Current`、781-784 已显式走官方全表 ⇒ 三档本来就齐，宽化只是重复 |
| `Core/CardIcons.cs:705` | 来源选择器：前后已分别有 `Lang13Current(703)` 与 `HasBulk(710)`，这一支专取 Table 的中/英列 |
| `CardView.cs:5477` | 老键（卡表写法）是我们自己的历史键，只对手写表有意义；宽化会把前缀换代掩盖掉 |
| `DeckScene.cs:1310` | A1852：紧接两条 `Loc.EnOf` / `Loc.ZhOf` |
| `CardBaseDemo.cs:5450` | 被测实现 `CardView.RaceTerm` **不吃第三档**（bulk→Table→老键）⇒ 宽化会让「会印的都有词条」**假绿** |
| `B99A3_Assert.cs:311` | 别名键登记哨兵 + 紧跟 `KeywordZh`（只读 Table） |
| **A1025 族共 6 处**：`DeckScene:1266` · `CollectionScene:12283` · `MainMenuScene:16917` · `SettingsScene:5584` · `ShellScene:8249` · `ShopScene:5796` | 该族的 ③ 用 `Loc.EnOf` 断「英文列无 CJK」⇒ 前提必须窄；宽化会让 ③ 变成对空串求值 = 假绿 |
| `NetSelfTest.cs:165 / 173` | P6 双语族：② 用 `Loc.EnOf`，且 `Settings/Online/*` 135 条**本来只在 Table**（官方两档 0 条） |

### B. 后面已经有显式档链（宽化只是重复，且会打乱「谁优先」）（4 处）
| 位置 | 为什么必须窄 |
|---|---|
| `Shell/BaseEventPopup.cs:463` | chain = bulk(462) → Table(463) → `OfficialCurrent(472)`；顺序是有意的 |
| `Shell/LiveOpsLocalCatalog.cs:991` | chain = bulk(990) → Table(991) → `OfficialCurrent`(992) → `Lang13Current`(995) → `Lang13Of`(997) |
| `Shell/FTUESelectDeckWindow.cs:419` | else 分支已用 `Lang13Of` 覆盖第三档（`TutorialData/TutorialFTUEDeck/*` 4 条只在 13lang、Table 0 条） |
| `Shell/FTUESelectDeckWindow.cs:589` | 同族：`Lang13Of` → SO `deckName` 三级兜底（`PrebuiltDecks/*` 13lang 215 / bulk 179 / Table 0） |

### C. 删除 / 孤儿 / 契约哨兵 —— 它们问的**就是**「手写表里有没有」（14 处）
| 位置 | 判什么 |
|---|---|
| `BattleScene.cs:44833 / 44835` | 「我们自拟的 `Battle/Hint/*`（11）/ `Battle/Log/*`（16）已从手写表删掉」；**紧邻 44837-44842 已另断官方 13 语言表按前缀 0 条** ⇒ 两档是分开断的，别合并 |
| `BattleScene.cs:44845-44848` | 「W62 那四条配套键没被建回我们的手写表」 |
| `DeckScene.cs:6987` | 「我们那条自造键 `MenuDeck/Error/EffectOnlyCard` 不在手写表里」 |
| `DeckScene.cs:7010 / 7017` | 断的是 **`HasEntry` 自己的契约**（null 不抛且返 false / 虚构键返 false） |
| `SettingsScene.cs:1324-1326` | 「我们登记的是**原版 `Localize.mTerm` 键名**」的登记哨兵 |
| `SettingsScene.cs:4717` | 孤儿键哨兵：`Settings/Online/TitleNote` 还在我们的表里 → 据此断它没被画到屏上 |
| `MainMenuScene.cs:16977` | 覆盖度统计：只统计「官方有 ∧ 我们手写表也建了」的键，再比 `ZhOf` 与官方 zh 列；宽化 = 拿官方比官方（自我循环） |

### D. 注册表完备哨兵 —— 宽化会把断言放水成空转（3 处）
| 位置 | 判什么 / 为什么不能宽 |
|---|---|
| `CardBaseDemo.cs:4969` | 「起始卡每张都建了 `Card_Name/*` 词条」；官方表 **1198 条**在 bulk+13lang ⇒ 宽化后几乎恒真（`Card_Name/*` 在 Table 只有 26 条） |
| `CardBaseDemo.cs:4978` | 「16 个阵营都建了 `Armies/*` 词条」（Table 16 / 13lang 20） |
| `BattleScene.cs:7869` | 「24 条里只缺 `CantAttackCoward` / `CantAttackTainted`」——期望值写死为「官方也没有那两条」，判的是登记面 |

---

## 四、★ 今天就在错的 4 处（真缺陷，数据实证）

| 位置 | 病灶 | 现读数据 | 影响 |
|---|---|---|---|
| `Shell/ProfileData.cs:1650` | `LocalizedText()`：Table 没有就**把键名当文案返回** | `Modular_Description/*`：Table **0** / bulk **342** / 13lang **342** | 带 `((模板,参数))` 的饰品描述会**把键名印到界面**（原版 I2 是有值的） |
| `Shell/ReferralPopupWindow.cs:688` | 错误词条闸查 Table | `CustomErrors/*`：Table **1** / 13lang **41** | 40 条真错误全部错落 `UnhandledError` |
| `Shell/ReferralPopupWindow.cs:692` | 兜底键自己也查 Table | `CustomErrors/UnhandledError`：Table **无**、13lang **有** | **错误文案恒为空串**（`HasEntry` 假 ⇒ `""`）—— 与 `Loc.cs` 实例注释那一条同源 |
| `Shell/ShopTabPage.cs:72` | 商品类型行闸查 Table | `MenuShop/ShopItemType/*`：Table **5** / 13lang **15** | **10 类**（`Blackstone` / `BoosterPack(s)` / `Boosters` / `CardStyle` / `Crystals` / `Currency` / `Energy` / `EventMedals` / `GachaTicket(s)` / `Style` …）印原始英文 type |

**外加 1 处「断言空转」**：`Editor/MainMenuScene.cs:8190` —— 闸是 `Loc.HasEntry("Achievements/"+id)`，而 `Achievements/*` **212 条一条都不在 Table**（只在 bulk + 13lang）⇒ 这道闸**恒假**，里面那条「官方词条在表里时，描述取的是它」的判别式断言**从未执行过**。改宽（`HasAnyTerm`）才让它活过来。

**改宽时的实现注意（重要）**：`HasAnyTerm` 只回答「有没有」、**不出声**。现有很多写法是 `if (HasEntry(k)) return Loc.T(k); Loc.T(k); return 兜底;`（第二句 `Loc.T` 专门用来出声/记账，见 `LogTerm`、`KeywordZh`）。改宽后**必须保留 else 里的那次 `Loc.T`**，否则「缺键要出声」那条红线会静默丢掉。

---

## 五、按文件分组（改哪几个文件收益最大）

| 文件 | 调用点 | 宽 / 窄 | 说明 |
|---|---|---|---|
| `Editor/MainMenuScene.cs` | 36 | 34 / 2 | 33 条前提断言（零风险批量）+ **:8190 真缺陷** |
| `Editor/BattleScene.cs` | 29 | 22 / 7 | 其中 4 条与 `BattleDriver` 两态谓词**同刻改**（:7245/7273/27362/27369），否则红 |
| `Editor/DeckScene.cs`、`Editor/CollectionScene.cs` | 24 / 24 | 19/5、23/1 | 主要是「前提断言」批量；窄的 6 条都在 A1025 / 哨兵 |
| `Shell/*.cs`（运行时，17 个文件共 22 处） | 22 | 18 / 4 | **真缺陷 4 处全在这里**（ProfileData / ReferralPopup ×2 / ShopTabPage） |
| `Core/CardText.cs`（5）、`Core/CardIcons.cs`（1）、`Core/CardView.cs`（3） | 9 | 2 / 7 | **CardText 5 处全窄**（字体闸 / 列取值）；`CardView:5474` 可宽 |
| `Editor/SettingsScene.cs`（7）、`NetSelfTest.cs`（2）、`CardBaseDemo.cs`（3） | 12 | 4 / 8 | 窄为主（`EnOf` 与哨兵） |

> **收益排序**：① `Shell/ReferralPopupWindow.cs` + `Shell/ProfileData.cs` + `Shell/ShopTabPage.cs`（真缺陷，4 处，改完即修）→ ② `Editor/MainMenuScene.cs:8190`（断言空转）→ ③ `Shell/MatchLogRow.cs:89`（**谓词查 Table、取值走 `Loc.OfficialCurrent`**，最典型的一处错配）→ ④ `Shell/SettingsWindow.cs:2379/2410`（连带改哨兵口径）→ ⑤ 其余 `Shell/*` 显示口（零风险统一）→ ⑥ `Editor/*` 的 **108 条**前提断言（按 §二 的默认规则批量改，必须剔除 §三 的 28 条窄）。

---

## 六、附录 · 顺手发现（不在本件结论内，供调度台分流）

1. **`Card_Trait/*`：官方 83 条里有 31 条不在手写 `Table`**（其中 4 条只在官方全表：`bloodThirst`/`cantAttack`/`huntMark`/`spiritStone`；其余 27 条在 bulk）。按 `CardText.TraitTerm` 的 `.ToLowerInvariant()` 口径复核：那 4 条的**全小写形式在 `Table` 里存在**（能取到），但另外 **27 条的全小写形式也查不到**（`markOfKhorne` / `markOfChaos` / `jam` / `large` / `sneak` / `rage` / `resolution` / `support` …）。⇒ 若引擎会产出这些 canonical 键，`KeywordZh` / `KeywordDisplay` / `KeywordEn` 会返回 `null`（卡面判「认不出」）。**这不是换谓词能修的**（要改取值路：`Loc.OfficialCurrent`/`Lang13Current` + 字体闸），且「影响多少张卡」本件**没查** ⇒ 建议另立账。另：`CardDisplayWindow.MaskKey` 用的 `Card_Trait/secretOrder` / `classified` / `questClassified` **三档皆无**（`quest` 只在 Table）⇒ 那三格今天必然留空 + 出声一次。
2. **同族窄口不止 `HasEntry`**：`Loc.HasBulk(` 全仓 13 处，`Loc.EnOf/ZhOf` 直接取值也不少。按 §五 那张族表，`MenuShop/*`（Table 13 / 13lang 69）、`SocialMenu/*`（23 / 78）、`Missions/*`（12 / 181）、`Item_Description/*`（1 / 78）、`CustomErrors/*`（1 / 41）、`PrebuiltDecks/*`（0 / 215）、`Achievements/*`（0 / 212）都是「手写表远小于官方表」的族 —— **凡「先 HasEntry 再 Loc.T」的口都可能踩同一个坑**，建议按前缀扫一遍（本件只覆盖 `HasEntry`）。
3. `TutorialData/TutorialFTUEDeck/*` 4 条**只在 13lang**（Table 0）⇒ `FTUESelectDeckWindow:419` 全靠 else 分支的 `Lang13Of` 才取得到；哪天有人删那条 else 就会印空。
4. `MenuDeck/Error/{2,4,5}` **三档皆无**（与 `DeckScene:7184` 的「A1872：官方一条数字键都没有」一致）⇒ `DeckRuntime:6022` 宽化后仍落兜底键 `InvalidDeck`，**行为零变化**。
5. `MenuDeck/Share/ExportSuccesful` / `Rewards Menu/Points` 现读**已在手写 Table 里** ⇒ 那两处注释（「表里还没有这条键」）已经过期，改的时候顺手订正注释（铁律 5）。
6. `HasEntry(null)` 会打一条去重 warning（`_warnedNullKey`），`HasAnyTerm` 对 null/空串**静默返回 false**。`DeckScene:7010` 测的正是前者的契约 —— 那一条不要跟着改宽。
7. 口径提醒：`Loc.cs:3960-3964` 的 `HasAnyTerm(key)` = `HasEntry || HasBulk || Lang13Of(key,"en") != ""`（第三档按 **en 列**判，与官方全表「en 恒有值」一致）。**判「会不会印键名」用它**；判「我们登记了没有」仍用 `HasEntry`。
8. **宽化后 `ProfileData.LocalizedText` 仍会印键名的一族**：`Cosmetic_Description/*` **三档皆 0 条**（bulk/13lang/Table 全无）⇒ 那条路照旧落到「返回 `term` 本身」。`Modular_Description/*`（342 条在 bulk/13lang）这次已经修好。原版 I2 里到底有没有 `Cosmetic_Description/*` 这一族**本件没查** ⇒ 另立账，⛔ 别凭空造键。

---

## 七、执行记录（2026-10-30 · `B103_R3` · 调度台授权后）

**已改（白名单内，15 个文件 / 20 处宽化；全部是同一行谓词替换，未新增成员）：**

| 文件 | numstat | 改动点 |
|---|---|---|
| `Shell/ProfileData.cs` | 2 / 2 | `LocalizedText` 的闸 + 它上面那行 doc 措辞 |
| `Shell/ReferralPopupWindow.cs` | 5 / 5 | `:688` / `:692` 两处 + doc 与 fallback warning 措辞 |
| `Shell/ShopTabPage.cs` | 3 / 3 | `TypeText` 的闸 + doc |
| `Shell/MatchLogRow.cs` | **3 / 1** | `ModeText` 的闸 + 两行说明注释（**唯一行数变化的文件，+2 行**） |
| `Shell/SettingsWindow.cs` | 5 / 5 | `AcTerm` / `UiTerm`：**显示口宽化**（末句 ternary），**哨兵仍守手写 Table**（`if (!_acMissingKeys.Contains(key))` 那一段原样）＋ warning 措辞诚实化 |
| `Core/CardView.cs` | 4 / 4 | `CreatedByLine`（`:4840`）与 `RaceTerm` 第 ② 档（`:5474`）；`:5477` 的老键**保持窄** |
| `Shell/AlliancePanelWindow.cs` | 2 / 2 | `LocOr` |
| `Shell/AvatarTab.cs` | 1 / 1 | `AvatarButtonTerm` |
| `Shell/CampaignTab.cs` | 3 / 3 | `Refresh` 的积分模板 + `PremiumTerm`（含 doc 一行） |
| `Shell/DeckInfoPopup.cs` | 1 / 1 | 分享成功 toast |
| `Shell/DeckSelectionPopup.cs` | 3 / 3 | `Select Deck` 钮文案（含 doc 两行） |
| `Shell/ItemDisplayWindow.cs` | 2 / 2 | 卡背 Description + 缺键 warning 措辞 |
| `Shell/MenuTutorialFlow.cs` | 2 / 2 | `Official()`（含 doc 一行） |
| `Shell/TutorialMessagePopup.cs` | 2 / 2 | `Tap To continue` + `OfficialTerm` |
| `Shell/WelcomePopup.cs` | 1 / 1 | `Tap To continue` |

- **`typecheck` = 运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_b103_r3 bash Unity/工具/typecheck.sh`）。
- **账漂移**：14 个文件**行数不变**（numstat 两列相等）⇒ 零漂移；`MatchLogRow.cs` **+2 行**，但全仓账表**没有任何一行**落在 `MatchLogRow.cs` 上（已 grep `账/*.tsv` 复核）⇒ **零漂移**。另逐条核过被引用的邻近落点未被移动/改写：`CardView.cs:5463`（RaceTerm）· `SettingsWindow.cs:2418`（D 档，本地模拟登录）· `ShopTabPage.cs:62`（SetHost）· `MenuTutorialFlow.cs:845/851` · `ReferralPopupWindow.cs:699`（SubmitReferrer）。
- **未动（白名单外，已列入补丁文件 §四 等授权）**：`Battle/CardDisplayWindow.cs:2531` · `Battle/ChatPopupPanel.cs:167` · `Battle/TipPrebattle.cs:199` · `Net/NetProtocol.cs:363`。
- **补丁另存**：`判据/AUD07_补丁_BattleDriver_DeckRuntime_宿主.md`（`BattleDriver` 5 处 + 4 条镜像断言 · `DeckRuntime:6022` + `DeckScene:7184` · 宿主 108 宽 / 28 窄，按锚文本给，⛔ 无行号）。
- **改宽之后没有新暴露的缺键出声**：本次被宽化的键今天全都在 `Table ∪ bulk ∪ 13lang` 里（`SettingsWindow` 那两族 `Settings/Online/*` 135 条、`Settings/Media/*`+`CardTextInHand/*`+`Graphics/*` 全在 Table）⇒ 哨兵 `_acMissingKeys` 不会因此长出东西。

---

## 八、执行记录 · 第二批（同一授权 · 白名单外 4 处，调度台第二次点名）

> 本批 4 个文件是调度台**第二次**明确授权（「这 4 个文件此刻都没人占，已授权」）。改动与前批同形：**同一行谓词替换 + 同段 doc/告示措辞诚实化**，⛔ 未新增成员、⛔ 未插行。

| 文件 | numstat | 改动点 | 今天的行为变化 |
|---|---|---|---|
| `Battle/CardDisplayWindow.cs` | 3 / 3 | `MaskKey` 的闸 + doc + 告示措辞 | **零**（`Card_Trait/secretOrder` / `classified` / `questClassified` 三档皆 0 条，`quest` 在 Table） |
| `Battle/ChatPopupPanel.cs` | 2 / 2 | `LabelFor` 的闸 + doc 一句 | **零**（`Battle/Chat/*` 6 条在 Table + 13lang） |
| `Battle/TipPrebattle.cs` | 1 / 1 | `GetNextTip` 的三元 | **零**（`PrebattleTips/*` 三档皆 0 条） |
| `Net/NetProtocol.cs` | 1 / 1 | `Unpack` 的「认不出就原样回显」闸 | 仅当对端发来**只存在于官方表**的键时才从「回显原串」变成「按本机语言取词」；我们自己的 `Settings/Online/*` 全在 Table ⇒ 现网行为不变 |

- **账漂移 = 0**：4 个文件 numstat 两列全相等（行数不变）。邻近落点复核：`CardDisplayWindow.cs:2527`（B 档 `BuyOriginalCard`）内容与行号均未变；`ChatPopupPanel.cs` / `TipPrebattle.cs` / `NetProtocol.cs` 在其编辑区域内**账里一行落点都没有**。
- **`typecheck`**：本批改完后**我的 19 个文件在编译错误里一条都没有**。当前读数 `运行时 2 / 编辑器 11`，**13 条 error 全部落在别人的在飞文件**：`Editor/B103R1_Assert.cs`(4，引用 `DeckRuntime.UiSetBuildResidueForTest/UiSetDragStateForTest` 尚未落地) · `Editor/B103R5_Assert.cs`(2，引用 `CardFeel.CardCenterBlockingSeconds`) · `Editor/B103W6_Assert.cs`(5，引用 `SceneInitializer.IsSettledForTest/AllSettledForTest`) · `Shell/RewardWindow.cs`(2，`OnLanguageChanged` 静态类里当实例成员覆盖)。**上一批（15 文件）改完那一刻的 `typecheck` 是 0/0**；这批 error 是并发写手在我两次运行之间落下的（`CardFeel.cs` / `SceneInitializer.cs` / `RewardWindow.cs` 当时都在 `git diff --stat` 里显示被别人改过）⇒ 按「错误全部集中在不是你负责的文件」处置，⛔ 没去动别人的文件。
- **有没有新暴露的缺键开始出声**：**没有**（四个键族要么在 `Table`，要么三档皆 0 条；`Unpack` 对认不出的键照旧直接 `return s`、不调 `Loc.T` ⇒ 不多打 warning）。

---

## 九、执行记录 · 第三批（自检宿主 · 调度台第三次点名）

> 授权范围 = 宿主里 **108 处「该宽」**；**28 处「该窄」一个字没动**；**5 处「镜像断言」按指令留待与 `BattleDriver`/`DeckRuntime` 同刻改**。

| 宿主 | 改了几处 | numstat（对 HEAD） | 我的改动（对备份逐行 diff） |
|---|---|---|---|
| `Editor/MainMenuScene.cs` | 34 | 35 / 35 | **34 行**（含 `:8190` 同行追加的那句说明；行数不变） |
| `Editor/CollectionScene.cs` | 23 | 23 / 23 | 23 行 |
| `Editor/BattleScene.cs` | 18 | 19 / 19 | 18 行 |
| `Editor/DeckScene.cs` | 18 | 18 / 18 | 18 行 |
| `Editor/ShopScene.cs` | 3 | 3 / 3 | 3 行 |
| `Editor/RewardsScene.cs` | 2 | **17 / 3** ⚠️ | **4 行（2 处同行替换）** —— 见下 |
| `Editor/SettingsScene.cs` | 2 | 2 / 2 | 2 行 |
| `Editor/ShellScene.cs` | 2 | 3 / 3 | 2 行 |
| `Editor/B94B9_Assert.cs` | 1 | 1 / 1 | 1 行 |
| **合计** | **103** | —— | —— |

- **⚠️ `RewardsScene.cs` 的 `17 / 3` 不是我造成的**：该文件开工前就是 `M`（调度台当时正在修 `:7092` 崩溃）。判据 = 改前备份 `_tmp_view/scene_bak/b103r3_hosts_before/` 与改后逐行 diff：**只有 2 行变化（共 4 个 +/− 行）**，且全部是谓词替换；脚本在写盘前对每个文件断言了「`\n` 总数不变 + `\r\n` 总数不变」，九个文件全部通过。同理 `MainMenuScene` 的 35/35、`BattleScene` 的 19/19 里也含别人的既有改动。
- **`Editor/MainMenuScene.cs:8190`**：按要求照改并写明原因 —— 但**没有插行**（会破「行数不变」），说明文字**追加在该行行末**（该行原文没有 `//`，所以不触发「注释吞代码」那个坑）：`if (Loc.HasAnyTerm(achKey))   // 🔴 2026-10-30（B103_R3）宽化：Achievements/* 手写表 0 条、官方表 212 条 ⇒ 原来这道闸恒假、里面那条断言从未执行；换 HasAnyTerm 后才真正生效`。
- **改完后宿主里残留的 `Loc.HasEntry` = 33 行**，逐条核对 = **28 条窄 + 5 条镜像**（`B99A3:311` · `BattleScene:7869/44833/44835/44845-44848` · `CardBaseDemo:4969/4978/5450` · `CollectionScene:12283` · `DeckScene:1266/1310/6987/7010/7017` · `MainMenuScene:16917/16977` · `NetSelfTest:165/173` · `SettingsScene:1324-1326/4717/5584` · `ShellScene:8249` · `ShopScene:5796`；镜像 = `BattleScene:7245/7273/27362/27369` + `DeckScene:7184`）。
- **`typecheck` = 运行时 0 / 编辑器 0**（本批改完时，别人那 13 条在飞错误已收干净）。**九个宿主一条错都没有。**
- **行数有没有被逼着变**：**没有**。唯一需要写说明的 `:8190` 用了「同行行末注释」而不是插行；其余 102 处都是纯谓词替换。（补丁库的后续状态见 §十：`BattleDriver` 5 处与 4 条 `BattleScene` 镜像**已落地**；仍未做 = `DeckRuntime:6022` + `DeckScene:7184`。）

---

## 十、执行记录 · 第四批（`BattleDriver` 5 处 + `BattleScene` 4 条镜像 · 调度台第四次点名）

> `B103_R1` 交回 `BattleDriver.cs` 后由本代理接手；`DeckRuntime.cs` 仍归 `B103_R2`（⚠️ 本批**没动**它）。

| 文件 | 改动 | numstat（对 HEAD，含别人的既有改动） |
|---|---|---|
| `Battle/BattleDriver.cs` | **5 处代码宽化**：`HandFullText` · `LogTerm`（`if` 下面那句 `Loc.T(key);` **原样保留**）· `HintForCodeWithKey` · `DamageFatigueTextWithKey` · `PlateName`；另把 3 行**doc 注释**里的谓词名与「键在表里」措辞就地订正（同行、未插行） | **11 / 11**（= R1 的 3 行 + 本批 8 行；**行数不变**） |
| `Editor/BattleScene.cs` | **4 条镜像断言**：`bool costInTable = …` · `Check(Loc.HasAnyTerm(keyInTable), "（前提）…有值 ⇒ 能验有键态")` · `Check(Loc.HasAnyTerm(keyInTable), "★ 前提：词条 … 在语言表里")` · `Check(!Loc.HasAnyTerm("Battle/Tips/__SelftestNotInTable__") …)` | **24 / 24**（本批 4 行 + 前批 18 行 + 别人的既有 2 行；**行数不变**） |

- **同刻性**：`HintForCodeWithKey` ↔ `costInTable`/第一条 `Check`；`DamageFatigueTextWithKey` ↔ 第二条 `Check`/虚构键负例 ⇒ 实现与断言**同一批**改完，不存在一红一绿的中间态。
- **`BattleDriver` 现存 `Loc.HasEntry` = 0**（5 处代码 + 3 行 doc 全部改完）。
- **`BattleScene` 现存 `Loc.HasEntry` = 9**：7 条该窄（`7869` · `44833` · `44835` · `44845-44848`）+ 2 处**非代码**（`:7213` 注释 · `:7251` 断言**消息文本**里那句「`Loc.HasEntry(键)` = {costInTable}」）。后者按指令「不改文案里的其它字」**未动**，如需美化请一句话即可（改成 `Loc.HasAnyTerm` 即可，无行为影响）。
- **账漂移**：两个文件 `numstat` 两列相等 ⇒ **行数不变 ⇒ 零漂移**。
- **`typecheck` = 运行时 0 / 编辑器 0**。
- **仍未做**：`Deck/DeckRuntime.cs:6022` + `Editor/DeckScene.cs:7184`（等 `DeckRuntime.cs` 放出来，两处同刻改；补丁见 `AUD07_补丁_…md` §二/§五）。→ **✅ 已于第五批落地，见 §十一。**

---

## 十一、执行记录 · 第五批（`DeckRuntime` + `DeckScene` 那对「必须同刻」的补丁 · 最后一次）

| 文件 | 改动 | numstat（对 HEAD） |
|---|---|---|
| `Deck/DeckRuntime.cs` | `MenuDeckErrorKey` 末句：`Loc.HasEntry(key)` → `Loc.HasAnyTerm(key)`（**同行纯替换，1 行**） | **50 / 13** —— **不是我造成的**：`B103_R2` 的 A/B/D 三份补丁就在这个文件里（净 +37 行）。本代理这一处经 `git diff` 核对 = **恰好 `-1/+1` 一行**，不增行。 |
| `Editor/DeckScene.cs` | `CheckTrue(!Loc.HasEntry(numKey5), …)` → `!Loc.HasAnyTerm(numKey5)`（**同行纯替换，1 行**） | **20 / 20**（= 前批 18 + 本批 1 + 别人的 1）⇒ 行数不变、零漂移 |

- 两处**同一批**改完：`MenuDeckErrorKey` 选「用哪个键」、`DeckScene:7184` 是它的「真前提」（数字键三档皆无）⇒ 口径一致。
- **`typecheck` = 运行时 0 / 编辑器 0**。
- **AUD07 的宽化工作到本批为止全部落地**：`HasEntry` 侧 = 运行时 26 处（含 `BattleDriver` 5）+ 宿主 103 处 + 5 条镜像 + 本批 1 处；**该窄的 39 处一条没动**。
