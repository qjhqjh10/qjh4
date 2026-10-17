# W_N42 · 卡名 / 卡面正文 / 关键词 tooltip 改走官方 13 语言

> 日期：2026-10-17 · 写手：`W_N42` · 白名单见任务书（`Core/Loc.cs` `CardText` `CardIcons` `Tooltip` `CardPreview`
> `RelatedCards` `Battle/BattleDriver.cs` `Deck/DeckEditorState.cs` `Deck/DeckRuntime.cs` `Editor/SettingsScene.cs`）

## 结论

卡名 / 卡面正文 / 关键词 tooltip 已从「中/英二选一」改成**按 `Loc.Current` 取官方 13 语言**；
中/英两档走的是**与改前逐字节相同的那两条原路**（`Table.En/Zh` · `CardDef.NameZh/DescZh` · `CardDef.Name/Desc`），
非中英 10 档取 `Resources/official_loc_13lang.json` 的当前列，缺键/缺列一律**回退英文 + 出声一次**。

## 改了什么（逐文件）

### 1. `Core/Loc.cs` —— 新开唯一取值口

| 方法 | 行 | 行为 |
|---|---|---|
| `Loc.Lang13Current(string key)` | `:2869` | **按当前语档**取 13 语言那一列。**中/英档恒 `null`**（中英仍以手写 `Table` 为权威 ⇒ 调用方继续走老路）；非中英：命中返回，**缺键/缺列 ⇒ `null` + 出声一次/键**（去重表 `_warnedLangFallback`，与 `T()` 共用）。 |
| `Loc.Lang13Keys(string prefix)` | `:2888` | 列出官方表里以某前缀开头的键（序数排序、结果确定）。**唯一用途** = `TipText` 派生「规范键 → 官方枚举名」映射。⛔ 不参与任何显示取值。 |

### 2. `Core/CardText.cs`

| 位置 | 改前 | 改后 |
|---|---|---|
| `Term(term)` `:562` | 表里有键 ⇒ `Zh ? Loc.T : Loc.EnOf`（非中英**一律英文**） | **先** `Loc.Lang13Current(term)`（中/英恒 null）⇒ 非中英命中就返回；没命中才走原路（`!HasEntry ⇒ Loc.T` / `Zh ? Loc.T : EnOf`） |
| `TermOr(term, fallback)` `:583` | 表里没键 ⇒ 出声 + `fallback` | 表里没键时**先试** `Lang13Current`（命中就返回），再出声 + `fallback` |
| **新增** `Name(RuleEngine.CardDef c)` `:690` | —— | 中/英 ⇒ `Name(c.Name, c.NameZh)`（= 改前那两个实参，逐字节不变）；非中英 ⇒ 官方 `Card_Name/<c.Id>`，缺 ⇒ 英文 `c.Name`。⚠️ **我们自己那 26 张**（`FromOriginalPool == false`）直接回英文名，不去查表（查了只会白响 26 声） |
| `Keyword(norm)` `:714` | `!Zh ⇒ norm.ToUpperInvariant()` | `!Zh` 时**先** `Lang13Current(Card_Trait/<key>)`，缺才大写英文（中/英两档恒 null ⇒ 不变） |
| `KeywordSegment(keywords, zh, body, …)` `:234` | 非中文档一律 `KeywordEn`（英文） | 新增 `lang13` 判据：非中英档的词 = `Lang13Current("Card_Trait/"+key) ?? KeywordEn(key)`；**`other`（判「正文里已印过」的第二个写法）仍取英文名** —— 因为正文可能是「官方列缺 ⇒ 回退英文 `Desc`」那一档，只比当前语言的词会把英文正文里同一个词再补一遍（重复印）。中/英两档 `lang13` 恒 false ⇒ 逐字节不变 |

> ⚠️ `KeywordZh` / `KeywordEn` **一个字没动**（它们是「恒中/恒英」的判据口：`KeywordSegment` 的别名比对、
> 以及 `TipText` 里 tooltip 标题的英文括注）。

### 3. `Core/CardIcons.cs` —— `OfficialTerm(term, zh)` `:516`

**先** `Loc.Lang13Current(term)`（中/英恒 null ⇒ 下面逐字节不变），没命中才走原来的 `HasEntry ⇒ ZhOf/EnOf` / `HasBulk ⇒ BulkOr`。
⇒ `[[关键词]]`、`$$卡 id$$`、`((Modular…))` 三类嵌套词在非中英正文里一次修好（改前它们落回英/中列 = 半外半英）。
⚠️ 这一步**与 `zh` 形参无关**：当前语档是哪一列由 `Loc` 自己判（`zh` 只表示「这份正文是不是中文列」）。

### 4. `Core/Tooltip.cs` —— 关键词 tooltip 正文

- 新增 `LoadOfficialTraitMap()` `:659`：从**本地数据**派生「我们的规范键 → 官方 `DefinedTrait` 枚举名」，
  三条规则（逐条可复核，⛔ **没有手写猜的映射**）：
  ① `CardTraitDescription/<规范键>` 直取；
  ② 官方枚举名归一化（只留字母数字、小写）后 == 规范键（**唯一命中**才算）；
  ③ `Card_Trait/<枚举名>` 的英文（其次中文）与 `trait_tips.json` 同列归一化后相同（**唯一命中**才算）
     —— `destroyer→frenzied`、`penitence→rage` 走这条。
- `Trait(key)` `:756`：`useZh` 判据没动；**非中英档**先取 `Loc.Lang13Current("CardTraitDescription/<官方枚举名>")`，
  取不到（推不出映射 / 该列空）⇒ **回退 `bodyEn` + 出声一次**（`_warnedTraitMap` / `Loc` 的去重表）。
- 新增自检口 `OfficialTraitNameForTest(key)` `:705`（推不出 ⇒ null，⛔ 不改状态）。

**覆盖度（现读 62 条规范键）**：**53 条**推得出官方枚举名；**9 条推不出**（如实留空、回退英文）：
`ability` · `concussion` · `darkpact` · `ferocity` · `huntmark` · `oath` · `pack` · `quest` · `sentry`。
其中 `ferocity`/`huntmark`/`oath`/`pack`/`sentry`/`ability` 官方**本来就没有** `CardTraitDescription`；
`concussion`（官方叫 `concussive`）与 `darkpact`（官方叫 `markOfChaos`）是**改名对不上**，按任务书「查不到就保持英文 + 日志，⛔ 不猜」。

### 5. `Battle/BattleDriver.cs`

| 位置 | 改前 | 改后 |
|---|---|---|
| `CardDisplayName(CardDef, fallback)` `:7360` | `CardText.Name(c.Name, c.NameZh)` | `CardText.Name(c)`（按卡 id 取那一口） |
| `FaceTextFull(CardDef)` `:14148` | `body = zh ? c.DescZh : c.Desc` | 中/英**原样**；非中英 ⇒ 官方 `Card_Description/<c.Id>`，缺 ⇒ `c.Desc`。**后面的 `CardIcons.Rewrite` / `KeywordSegment` 链路一个字没改**（非中英正文里的 `[[…]]`/`((…))` 由 `RewriteMarks` 渲染） |
| `:9018` / `:9163`（技能面板 / 选择器提示行） | `CardText.Name(u.Name)`（只有名字） | `CardDisplayName(u.Card, u.Name)`（有 `CardDef` 就用卡 id） |
| `:14027`（场上单位卡面标题） | `CardText.Name(u.Name, u.Card?.NameZh)` | `u.Card != null ? CardText.Name(u.Card) : CardText.Name(u.Name)` |
| `:14107`（`ToCardData(CardDef, faction)`） | `CardText.Name(c.Name, c.NameZh)` | `CardText.Name(c)` |

### 6. 其余调用点

- `Core/CardPreview.cs:34` · `Deck/DeckEditorState.cs:227`（`FillNameFromWarlord`）—— 改走 `CardText.Name(CardDef)`。
- `Deck/DeckRuntime.cs`：**7 处**（`:3864` 起拖日志 · `:3897` 放牌日志 · `:4131-4132` 卡池排序 · `:4252` 卡组行名 ·
  `:5124` 移出提示 · `:6874` 显示名取值口 · `:8153` 加入提示）。
  ⚠️ 任务书只点了 `:3863` 那一处，另外 6 处是「先 grep 找同族漏网」找出来的（同一个文件、同一族显示点）。

### 7. `Editor/SettingsScene.cs` —— 新增断言（`Section("13 语言：…")` 起 `:1350`）

## 证据（数据 / 键样例）

| 事实 | 出处 |
|---|---|
| `official_loc_13lang.json` 9,447 键；族 `Card_Name` **1065** · `Card_Description` **1101** · `Card_Trait` **77** · `CardTraitDescription` **76** · `Armies` **19** | 现读该 json |
| 卡表 `1127` 张（现读 `cards_engine.json` 的 `count`）；`Card_Name/<id>` 命中 **1009**、`Card_Description/<id>` **1007** | 现读 + 逐 id 比对 |
| 键 = 卡的 `CardDef.Id`；列序 `en,es,ru,fr,de,pt,it,ko,zh,ja,pl,cs,tr` | `Loc.cs:2779` `Lang13Codes` / json 的 `langs` |
| 断言用的卡 `ASH55 Aspect Shrine`（13 列齐全、正文无 `[[`/`((`/`$$` 记号、`card_icon_plan` 里无条目 ⇒ 正文不会被图标层改写） | `cards_engine.json` + `official_loc_13lang.json` + `card_icon_plan.json` 现读 |
| 日语字面量：`Card_Name/ASH55` = `アスペクト・シュライン`；`Card_Description/ASH55` = `2つの魂縛石を獲得`；`Card_Trait/rally` = `ラリー`（韩 `집결`）；`CardTraitDescription/rally` 日 = `このユニットが手札から出されたときに効果を発動する` | 现读 json |
| 映射实测：`rally→rally`（直取）· `bloodthirst→bloodThirst`（规则②）· `destroyer→frenzied`、`penitence→rage`（规则③） | 离线逐条复算（见上「覆盖度」） |

## 断言（都在 `Editor/SettingsScene.cs` 新增那一节）

1. **口径**：中文档 / 英文档下 `Loc.Lang13Current` 恒 `null`（中英仍以手写表为权威）。
2. **中/英逐字节不变**：中文卡名 == `CardDef.NameZh`、中文正文含 `DescZh`；英文卡名 == `CardDef.Name`、正文含 `Desc`。
   （前提断言 `CardText.Zh` —— 字体闸 + 语档闸都对才成立。）
3. **其余 10 档逐档**：卡名 == `Loc.Lang13Of("Card_Name/ASH55", <语言码>)`、正文含 `Card_Description/ASH55` 同列；
   外加一条合计断（10/10 · 10/10）。
4. **字面量灭自证**（期望值不从被测实现里读）：日语卡名 `アスペクト・シュライン`、日语正文 `2つの魂縛石を獲得`。
5. **关键词段**：日 `ラリー` · 韩 `집결` · 英 `Rally` · 中 `集结`（中/英两条断「一个字不变」）。
6. **关键词 tooltip**：日语标题含 `ラリー`、正文 = 官方日文字面量；英文档正文 = `trait_tips.json` 的 `bodyEn` 原文。
7. **映射**：`rally→rally` · `destroyer→frenzied`；`darkpact == null`（**已知缺口如实钉住**，⛔ 不猜）。
8. 收尾把语言放回本段进来时那一档、`PersistOverride` 逐值还原。

## 没查清 / 没接的（如实记）

1. **`Core/CardView.cs:1498`（创建者卡名）没改** —— 该文件**不在本笔白名单**里，仍是 `CardText.Name(creator.Name, creator.NameZh)`
   ⇒ 非中英档下这里印英文。要改就是同一个替换（`CardText.Name(creator)`），一行。
2. **`Deck/DeckEditorState.cs:458` 卡池搜索没接 13 语言** —— `Hit(c.Name) || Hit(c.NameZh) || Hit(c.Desc) || Hit(c.DescZh)`；
   非中英档下按本地语言搜不到。这不在任务书要求里（是**搜索**不是显示），如实记。
3. **关键词 tooltip 映射 9 条推不出**（见上清单）⇒ 这 9 个词的正文在非中英档**回退英文**。
   `concussion`/`darkpact` 的官方枚举名分别是 `concussive`/`markOfChaos`（**数据侧改名**），
   但它们的字面量与 `trait_tips` 那两行**对不上**（`Concussive`≠`Concussion`、`Dark Pact`≠`Dark Pacts`）
   ⇒ 按任务书**不猜**。我搜过：`Card_Trait/*` 77 条全表、`CardTraitDescription/*` 76 条全表、`trait_tips.json` 62 条全表。
4. **`KeywordDisplay` / `KeywordZh` / `KeywordEn` 对「不在手写表、只在官方表」的键仍返回 null**
   （例 `markOfChaos`：手写表键是小写规范键 `darkpact`，官方键是驼峰）—— 这是**改前就有的键名口径缺口**，
   不在本笔范围；它导致这类词在 tooltip 里只有图标没有标题（`TipText.Trait` 那条 `t == null` 分支）。
5. **日语 / 韩语字形覆盖未验** —— 全仓今天仍是「一个 Noto（`Face.Cjk`）打天下」（`Core/TmpFont.cs:48-53`），
   `EnsureCjkFallback()` 只给拉丁面挂兜底。**没有实跑、没有截图**（任务书禁跑自检 / 禁起 player），
   ⇒ **不许声称已验**。要验得开一次真机看日/韩卡面。
6. **`RelatedCards` 的「相关卡」匹配**仍用英文 `card.Desc`（`:133`）—— 那是**匹配**不是显示，
   非中英档下相关卡集合可能比中文档宽/窄（与改前的英文档同口径）。未动。
7. 中/英两档「逐字节不变」是**按代码路径推的**（两条原路一个字节没改）+ 上面那组断言钉住；
   **没有跑 `SettingsScene.Run`**（任务书禁跑 `*.Run`）⇒ 这条结论**待自检跑一次才算数**。

## typecheck 结果（最后一次原文）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
