# B141 · W129 · 卡组数据 4 条（P3 包）—— 执行写手 `B141-W31`

> 2026-11-04 · 第一百二十八会话 · 批 `B141` / 波次 `W129`。
> 判据来源 = `资料/完全复刻/判据/B141_W129_对账_卡组色彩.md` **§四 P3 包**（= `报告/B138_审计汇总_问题清单.md` §3 的 3-18 / 3-20 / 3-22 / 3-24），行号与结论**全部现读**（该判据写于 11-04 18:11，之后 `W15` 改过 `DeckLibrary.cs` ⇒ 以盘上为准）。
> 白名单内改动：`RuleEngine/Core/DeckRules.cs` · `RuleEngine/Data/DeckLibrary.cs` · 新建 `RuleEngine/Editor/B141_W31_Assert.cs` · 本文件。
> ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表 / 活状态表 / 所有权表。

---

## 〇、四条的状态

| 编号 | 状态 | 一句话 |
|---|---|---|
| **3-18**（`Describe` 键半） | ✅ 已复刻 | 三条映射全部落地：`CopyLimitExceeded ⇒ MaxCopiesReached` · `TooManyCards ⇒ MenuDeck/Tip/DeckFull` · `EffectOnlyCard ⇒ InvalidCard`。⚠️ `MaxCopiesReached` 的 `{0}` **未接线**（见 §六 ❓1） |
| **3-20** | ✅ 已复刻 | ④ 张数闸从「⑤ 阵营之前」搬到 **⑧ 之后**（= 全部逐张闸之后），「牌少 + 混阵营」现在报 `WrongFaction` |
| **3-22** | ✅ 已复刻 | `Validate` 的 `foreach (deck.CardIds)` 补 null 守卫 + 出声（`LogWarning`），不再 NRE |
| **3-24** | ✅ 已复刻 | ① 缺 `;` 照收（`gameMode` = 0 = Classic）② 多余的督军 / 防御卡**丢弃**，不再当普通卡带进卡表 |

`typecheck`（`TMPDIR=/tmp/wf_b141_w31 bash 工具/typecheck.sh`）：**运行时 0 错 / 编辑器 0 错（含新建的断言文件）**。
⚠️ 中途两轮报过 25 条 `CS0103`，全部在 `CardPresentation/Deck/DeckRuntime.cs`（别人正在写的 3-05/3-06 拖拽族）—— 那两轮编辑器那半边因运行时 DLL 没产出而 `CS0006` 级联；等那边落地后复跑即 **0/0**，本代理未碰该文件。

---

## 一、3-18 —— `Describe` 的三条键名映射

**判据（现读）**

| 档 | 原版键 | 判据出处 |
|---|---|---|
| 同名上限 | `MenuDeck/Error/MaxCopiesReached`（文案带 `{0}`） | `decomp_full/CardDeck__CanAddCard.c:58,65-71`（`GetMaxCopiesInDeck` 那支 → `DAT_1842cf7f0` + `String.Format`） |
| 整副满 | `MenuDeck/Tip/DeckFull`（**不在** `MenuDeck/Error/` 前缀下） | `CardDeck__CanAddCard.c:166-174`（`DAT_1842d0be0`，无 `Format`） |
| 准入（`inventoryOptions`） | `MenuDeck/Error/InvalidCard` | `CardDeck__CanAddCard.c:40-47`（`+0x4c != 0 && +0x48 != 5` → `DAT_1842cf4f0`） |

四条键在**官方 13 语言表**里现查都在：`Assets/CardPresentation/Resources/official_loc_13lang.json`（现读 **10,862** 键）与 `数据/游戏数据/官方本地化_en+zh-CN.json`（**10,854** 键）逐条命中；`MenuDeck/Error/EffectOnlyCard` / `CopyLimitExceeded` / `TooManyCards` **两张表里都是 0 命中**（= 我们自拟，⛔ 不能当上屏键）。

**改后**（`RuleEngine/Core/DeckRules.cs:596,600`）

```
EffectOnlyCard      → "MenuDeck/Error/InvalidCard"      （原：…/InvalidDeck）
CopyLimitExceeded   → "MenuDeck/Error/MaxCopiesReached" （原：…/CopyLimitExceeded，官方表 0 命中）
TooManyCards        → "MenuDeck/Tip/DeckFull"           （原：…/TooManyCards，官方表 0 命中）
```

**`HasOfficialNamedKey`**（`:638`）：加 `TooManyCards`（无占位符 ⇒ 可直接上屏）；**`CopyLimitExceeded` 故意不加** ——
它的文案是「卡组已有{0}张该牌的副本」，`{0}` = 同名份数上限，而 `Describe`/`ErrorTextKey` **拿不到那张卡**，
填不了就不许上屏（⛔ 绝不把 `{0}` 印给玩家）。⇒ `ErrorTextKey(CopyLimitExceeded)` 目前**退回** `MenuDeck/Error/InvalidDeck`
（= 改动前的行为，不变差；见 §六 ❓1）。

**为什么 `EffectOnlyCard` 与 `InvalidCard` 同键是对的**：原版 `CanAddCard` 里这两个判据本来就是**一句话**
（`+0x4c/+0x48` 那条闸给的就是 `InvalidCard`）；2026-10-17 `A1868` 把它摊到 `InvalidDeck` 是「不印键名」的兜底，不是原版那一支。

---

## 二、3-20 —— `Validate` 的闸序

**判据（现读 `decomp_full/DeckUtility__ValidateDeck.c`）**：
`:50` 先 `ValidateDeckCards(...)`（逐张闸：阵营 `:58` / 卡型 `:63` / `+0x4c` `:68` / `+0x48` `:73` / 禁卡 / 同名上限 `:111-121`），
返回真**之后**才在 `:69` 比 `cards.Count == iVar5`（否则 `*param_2 = 5` = `IncompleteDeck`）。

| | 改前 | 改后 |
|---|---|---|
| 顺序 | ①②③ **④张数** ⑤阵营 ⑥ ⑥b ⑦同名 ⑧传奇总数 | ①②③ ⑤阵营 ⑥ ⑥b ⑦同名 ⑧传奇总数 **④张数** |
| 「3 张 + 混一张异阵营」 | `TooFewCards` | **`WrongFaction`** ✅ |

**改后值**：`④` 挪到 `LegendaryTotalExceeded` 之后、`return DeckError.None` 之前（与 `CanAddCard` 那边「整副满**最后**」`.c:166` 同一条口径）。
两档（`TooManyCards` / `TooFewCards`）保留 —— 原版只有「正好 N 张」一条，两档都摊到 `IncompleteDeck`（`Describe` 里 `TooManyCards` 另有具名键，见 §一）。

---

## 三、3-22 —— `CardIds == null`

**改前**：`Validate` 的 `foreach (var id in deck.CardIds)`（`:462`）裸遍历；`PlayerDeck.CardIds` 只有字段初始化（`:722`），
`JsonUtility` 缺键 / 反序列化给 null 时**整条路径 NRE**。`RemoveInvalidCards` 早就有同一条守卫（`:434`），`Validate` 这条老路漏了。

**改后**（`:461`，同一物理行追加 ⇒ 行数零漂移）：`CardIds == null` ⇒ `Debug.LogWarning(...)` 出声 + `deck.CardIds = new List<string>()` ⇒ 按空表校验（有督军 ⇒ `TooFewCards`）。

---

## 四、3-24 —— `DeckLibrary` 导入串两处

### ① 缺 `;` 不再早退

**原版**（`decomp_full/CardDeck__DeserializeDeckString.c`）：
`:48` `Split(';')` → `:54-59` **只有** `Length > 1` 才 `Int32.Parse(split[1])`，否则 `gameMode` 保持 `0`（= `PlayModes.Classic`）；
`:61-69` 只取 `[0]` 继续解析。⇒ **没有 `;` 的串在原版是合法的**。

| | 改前 | 改后 |
|---|---|---|
| 无 `;` | `return null`（整条判死） | 正常解析，`GameMode = 0`，名字拿 `Split(':')[0]` |

### ② 多余的督军 / 防御卡**丢弃**

**原版**：`:113-160` 从尾到头扫卡表，每遇到一张 `cardType == 10`（督军，`:147`）或 `cardSubType == 0xd2`（防御卡，`:155`）就
`uVar7/uVar8 = 那一张` + `RemoveAt(i)` ⇒ 循环结束时只留**串里第一张**（倒序 ⇒ 最后赋值 = 下标最小），
**其余全部从卡表里摘掉**，`AddRange` 拿到的是「非督军、非防御卡」。

| | 改前 | 改后 |
|---|---|---|
| 第 2 张督军 / 防御卡 | `rest.Add(id)` ⇒ **当普通卡带进卡组** | 丢弃（`CardIds` 里不再出现） |

---

## 五、行数与账落点

| 文件 | 行数 | 账锚点 |
|---|---|---|
| `RuleEngine/Core/DeckRules.cs` | **1285 → 1285（净 0）** | `:409 RemoveInvalidCards` · `:452 Validate` · `:587 Describe` · `:634 HasOfficialNamedKey` · `:697 DescribeShort` · `:775 Clone` · `:781 AddCard` · `:788 RemoveCard` · `:796 CountOf` · `:936 SerializedPlayerDeckData` · `:1280 IsNotDeckable` —— **逐一回到原行号**（改动全部用「同一物理行追加 / 等量替换」，插入与删除各 3 行，位置都在 `:452`–`:528` 内） |
| `RuleEngine/Data/DeckLibrary.cs` | 66979 B → 改动 **2 行**（各 1 行换 1 行） | `:141 Decks` · `:187 NewUnsaved` · `:434 ExportString` · `:457 ImportString` · `:714 UpdateOrder` **均未漂移**（改的是 `:482` 与 `:547-548`，都是 1 换 1） |

---

## 六、❓ / 未接线（如实记）

1. **`MaxCopiesReached` 的 `{0}` 未接线（真缺口，白名单外）**
   `Describe(CopyLimitExceeded)` 已给对键，但上屏那条链是 `DeckRuntime.DeckErrorText(e)` → `Loc.T(ErrorTextKey(e))`，
   **没有 `string.Format`**，而 `{0}` = **该卡的同名份数上限**（`GetMaxCopiesInDeck`，`CanAddCard.c:65-71`），
   只有 `DeckEditorState.TryAdd(CardDef c, …)` 那一层手里才有这张卡。
   ⇒ 本批**故意**让 `ErrorTextKey(CopyLimitExceeded)` 退回 `InvalidDeck`（不印 `{0}`）。要接上需要动**两个白名单外文件**：
   ```
   DeckRuntime.cs:5578  public static string DeckErrorText(DeckError e, int copiesCap = -1)
   DeckRuntime.cs:5591      return Loc.T(DeckRules.ErrorTextKey(e)).Replace("{0}", copiesCap.ToString());   // copiesCap >= 0 时
   DeckEditorState.cs:476   why = e == DeckError.None ? "" : DeckRuntime.DeckErrorText(e, DeckRules.CopyLimit(c.Rarity, c.Type));
   + DeckRules.HasOfficialNamedKey 里补 || e == DeckError.CopyLimitExceeded
   ```
   **建议派给 `卡组编辑` 域的写手**（`B141-W31_Assert` 的 A9/A10 会在那一刻红，正好当验收口）。

2. **`CardPresentation/Editor/B121_WC_Assert.cs:91` 会变红（白名单外，需跟随更新）**
   它断 `ErrorTextKey(InvalidCard) != ErrorTextKey(EffectOnlyCard)`；按 3-18 两档现在**同键**（原版形状）⇒ 该断言失效。
   改法：把 `!=` 改 `==`（并改掉「两档必须两句不同文案」那句判语）或整条删掉。**该文件不在本代理白名单。**

3. **`RuleEngine/Editor/DeckRulesTest.cs:152-153` 会变红（白名单外，需跟随更新）**
   夹具 `bad.CardIds.Add(bad.CardIds[0])` —— `BuildLegalDeck` 的前两张本来就是**同一张卡的两份**（现读：`ASH83` ×2，上限 2），
   加到 3 张之后，闸序修正后**先撞 ⑦ 同名上限** ⇒ 实测会回 `CopyLimitExceeded` 而不是期望的 `TooManyCards`。
   这正是原版行为（`ValidateDeckCards` 的 `:111-121` 在 `:69` 之前）。
   改法（二选一）：把期望值改成 `CopyLimitExceeded` 并改判语；或夹具改成「多塞一张**不同**的同阵营普通卡」。
   **该文件不在本代理白名单。**
   ⚠️ 同一文件的 `:211`（30 张经典卡组拿去做遭遇校验 ⇒ `TooManyCards`）**不受影响** —— 现算那副牌只有 **3** 张传奇（≤ 4），⑧ 不先触发。

3b. **`RuleEngine/Editor/DeckRulesTest.cs:422` 会变红（白名单外，与 3-24 直接冲突）**
   原文：`CheckTrue(ImportString(b64("没有分号")) == null, "少了 `;gameMode` 尾巴 → null")` ——
   这条断言钉的正是**本次要修掉的那个 bug**（原版 `DeserializeDeckString.c:54-59` 缺 `;` 照收、`gameMode` = 0）。
   改法：期望改成「**非 null**，且 `GameMode == 0`、`Name == "没有分号"`、卡表为空」。
   ⚠️ 同文件 `:420`（空串 → null）与 `:421`（不是 Base64 → null）**仍然成立、不受影响**。
   **该文件不在本代理白名单。**

4. **`B121_WC_Assert.cs:131` 的注释已过期**（不是断言，只是注释）：它写着「原来只放 1 张 ⇒ `Validate` 在 ④ 张数档就回 `TooFewCards`」，
   现在 ④ 在最后 ⇒ 那句「所以要补满 30 张」的**理由**不成立了（补满仍无害，§C 断言照过）。

5. **未验的部分**：上屏**文字**（`Loc.T` 取到的人话）不在本文件断言范围内 —— `RuleEngine/Editor` 程序集引用不到 `CardPresentation.Core.Loc`。
   键的存在性已由写手现读官方两表核过（§一），但「取到的是哪一列」这条**没有**在这里断。

6. **3-24 还有一格我们与「逐字照抄」不同，本批**没动**（如实记）**：尾巴**不是整数**时，原版是 `Int32.Parse` **抛异常**，
   我们是 `int.TryParse` 失败 ⇒ `return null`。3-24 只点名了「缺 `;`」那一支，这一格没在判据里，
   且我们这条是「不猜」的既有约定 ⇒ **保留现状**。要彻底照抄需另立一条（并想清楚调用方怎么接 `FormatException`）。

---

## 七、断言清单（`RuleEngine/Editor/B141_W31_Assert.cs`）

`public static void Run()`；零前提（只读卡池，不建窗口不写盘）。**未接线**（`RuleEngineTest.cs` 不在白名单），
挂法写在文件头：同一物理行 `Guard("B141_W31_Assert", B141_W31_Assert.Run); _pass += …Pass; _fail += …Fail;`

| 组 | 条数 | 覆盖 | 灭自证（🧨 什么坏法会让哪条红） |
|---|---:|---|---|
| A | 11 | 3-18：三条映射 + 键存在性口径 + `{0}` 过渡态 | 退回 `TermPrefix + 枚举名` ⇒ A5/A6 红；`EffectOnlyCard` 改回 `InvalidDeck` ⇒ A2 红；`CopyLimitExceeded` 塞进 `HasOfficialNamedKey` ⇒ A9 红（带 `{0}` 上屏） |
| B | 5 | 3-20：闸序（`WrongFaction` vs `TooFewCards`）+ 张数闸仍生效 | 把 ④ 搬回 ⑤ 之前 ⇒ B1 红；B2 是对照（证明 B1 的结论来自闸序、不是别的原因）；B3b 证明张数闸没被删掉 |
| C | 3 | 3-22：`CardIds == null` 不抛 / 补空表 / 回 `TooFewCards` | 摘掉 null 守卫 ⇒ C1 红（`try/catch` 判「有没有抛」，不是「返回值碰巧对」） |
| D | 9 | 3-24：无 `;` 解析 · 带 `;` 回归 · 多余督军丢弃 · 多余防御卡丢弃 | `else return null;` 加回去 ⇒ D1 红；`rest.Add(id)` 加回去 ⇒ D3c/D4c 红 |
| **合计** | **28** | | |

⚠️ 卡池前提（督军 / 两张同阵营准入干净普通卡 / 一张异阵营普通卡 / 第二张督军 / 两张防御卡）拿不到时**一律报红**，不静默跳过。

---

## 八、改动清单（逐行）

| 文件:行 | 改了什么 |
|---|---|
| `RuleEngine/Core/DeckRules.cs:461` | 追加 `CardIds == null` 守卫 + `LogWarning` 出声（3-22） |
| `RuleEngine/Core/DeckRules.cs:485`（现） | 删掉原 ④ 张数三行，只留 `deckArmy` 声明（3-20，−3 行） |
| `RuleEngine/Core/DeckRules.cs:524-526` | 在 ⑧ 之后补 ④ 张数闸（3-20，+4 行；另在上一条里多删 1 行 ⇒ 净 0） |
| `RuleEngine/Core/DeckRules.cs:590-600` | `Describe`：`EffectOnlyCard → InvalidCard`；补 `CopyLimitExceeded → MaxCopiesReached`、`TooManyCards → Tip/DeckFull`；注释写全判据与 `{0}` 的过渡态（3-18） |
| `RuleEngine/Core/DeckRules.cs:637-638` | `HasOfficialNamedKey` 加 `TooManyCards`；注释订正 `EffectOnlyCard` 的映射目标（3-18） |
| `RuleEngine/Data/DeckLibrary.cs:482` | `else return null;` → 注释（缺 `;` 照收）（3-24①） |
| `RuleEngine/Data/DeckLibrary.cs:547-548` | 多余督军 / 防御卡 `rest.Add(id)` → 丢弃（3-24②） |
| `RuleEngine/Editor/B141_W31_Assert.cs` | 新建，28 条断言 |
