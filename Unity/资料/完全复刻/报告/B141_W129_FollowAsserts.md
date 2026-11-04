# B141 · W129 · W31 三处跟随断言（执行写手 `B141-W38`）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` / 波次 `W129`。
> 输入 = `报告/B141_W129_DeckData.md` §六 的跟随项 2 / 3 / 3b；判据 = 该报告 §一（3-18）、§二（3-20）、§四（3-24），
> 以及**现读**的 `RuleEngine/Core/DeckRules.cs:452-528`（闸序）、`:587-603`（`Describe`）、`:671-676`（`ErrorTextKey`）、
> `RuleEngine/Data/DeckLibrary.cs:457-527`（`ImportString`）。
> 白名单内改动：`CardPresentation/Editor/B121_WC_Assert.cs` · `RuleEngine/Editor/DeckRulesTest.cs` · 本文件。
> ⛔ 未跑 Unity、未动 git（只做了只读的 `git diff --numstat` / `git diff -U0`）、未改 `d:/2`、未改账主表 / 活状态表 / 所有权表。

---

## 〇、`typecheck`

`cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w38 bash 工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改完两轮都跑，两次都是 0/0）。

## 〇b、行数变化（两个文件都是**净 0**）

| 文件 | 行数 | 改了几行（`git diff --numstat`） |
|---|---|---|
| `CardPresentation/Editor/B121_WC_Assert.cs` | **不变（净 0）** | `4 / 4`（1 行注释 + 3 行断言块，全部就地改写） |
| `RuleEngine/Editor/DeckRulesTest.cs` | **不变（净 0）** | `4 / 4`（`:153` · `:419` 注释 · `:422` 三处） |

⇒ **两个文件下游行号零漂移**（所以账里那两条散文引用 `DeckRulesTest.cs:251` / `:574-602` 不用跟着改）。

---

## 一、`B121_WC_Assert.cs:91` —— 两档**同键**（选「改 `==`」，不删）

**改前**

```csharp
Check(DeckRules.ErrorTextKey(DeckError.InvalidCard) != DeckRules.ErrorTextKey(DeckError.EffectOnlyCard),
      "A5 ★★ 与「效果生成的卡」那档**不是同一个键**（两档原因不同、文案必须分开）"
    + " ｜🧨 把新档并回 `EffectOnlyCard` ⇒ 本句红");
```

**为什么必须改**（现读，不是照抄报告）：`Describe` 的 `EffectOnlyCard` 分支（`DeckRules.cs:596`）现在返回 `TermPrefix + "InvalidCard"`；
`HasOfficialNamedKey(EffectOnlyCard)` 为真（`:637`）⇒ `ErrorTextKey`（`:674`）走 `Describe` ⇒ 两档都出 `MenuDeck/Error/InvalidCard`。
原版 `CardDeck__CanAddCard.c:40-47` 的准入闸（`inventoryOptions`）就只给这一句具名键；`MenuDeck/Error/EffectOnlyCard` 在官方 13 语言表里 **0 命中**。

**改后**

```csharp
Check(DeckRules.ErrorTextKey(DeckError.InvalidCard) == DeckRules.ErrorTextKey(DeckError.EffectOnlyCard),
      "A5 ★★ 两档**同键** = `MenuDeck/Error/InvalidCard`（原版 `CardDeck__CanAddCard.c:40-47` 的准入闸只有这一句；官方 13 语言表里 `MenuDeck/Error/EffectOnlyCard` 0 命中）"
    + " ｜🧨 把 `EffectOnlyCard` 改回 `InvalidDeck` 兜底 ⇒ 本句红");   // ✅ 已复刻（2026-11-04 · B141 · 判据 `CardDeck__CanAddCard.c:40-47`，同 W31 `DeckRules.Describe:596`）
```

同时把该节标题（`:79`）从「与 `EffectOnlyCard` 的两句话**分得开**」改成「`EffectOnlyCard` 与它**同键**（原版那条准入闸只有一句话）」——
**保留断言、不删**，因为它反过来钉住了「`EffectOnlyCard` 不许再被摊回 `InvalidDeck` 兜底」。

**灭自证**：把 `DeckRules.cs:596` 的 `EffectOnlyCard → InvalidCard` 改回 `InvalidDeck` ⇒ 左 `MenuDeck/Error/InvalidCard`、右 `MenuDeck/Error/InvalidDeck` ⇒ `==` 为假 ⇒ **A5 红**。

---

## 二、`DeckRulesTest.cs:152-153` —— 31 张先撞**同名上限**（选「期望改成 `CopyLimitExceeded`」）

**改前**

```csharp
bad = legal.Clone(); bad.CardIds.Add(bad.CardIds[0]);
Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.TooManyCards, "31 张 → 超了");
```

**为什么必须改**（现读 `DeckRules.cs:452-527`）：W31 把 ④ 张数闸挪到 `:524-526`（= ⑤⑥⑥b⑦⑧ **之后**，照原版 `DeckUtility__ValidateDeck.c:50,69`）；
`BuildLegalDeck`（`:87-106`）把**每张卡按 `CopyLimit` 份整体加进去** ⇒ `CardIds[0]`/`[1]` 本来就是同一张卡（首张部队卡），
`Add(CardIds[0])` 之后那张卡是第 **3** 份 ⇒ ⑦ 同名上限（`:509-517`）先命中 ⇒ **`CopyLimitExceeded`**。
首张部队卡若恰是传说（上限 1）则只有 1 份，加一份 = 2 份 > 1 ⇒ 同样 `CopyLimitExceeded`。**两种情形都确定**。

**改后**

```csharp
bad = legal.Clone(); bad.CardIds.Add(bad.CardIds[0]);
Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.CopyLimitExceeded, "31 张（其中一张被加到第 3 份）⇒ **先报同名上限** ｜🧨 把 ④ 张数闸搬回 ⑤ 之前 ⇒ 本句红");   // ✅ 已复刻（2026-11-04 · B141 · W31/3-20 闸序 = …）
```

（另一条可选路「换一张不同卡」**没选**：它要依赖卡池里「还有一张同阵营、没到同名上限、过 ⑤⑥⑥b」的卡，虽然真池大概率有，
但会给自检加一条**池子形状前提**；而下面「覆盖影响」一节说明张数闸这一档并没有因此失去覆盖。）

**灭自证**：把 ④ 张数闸搬回 ⑤ 之前（= W31 之前的旧闸序）⇒ 同一个 31 张夹具先撞张数 ⇒ `TooManyCards ≠ CopyLimitExceeded` ⇒ **本句红**。

**覆盖影响（如实记）**：本句从此测的是「⑦ 在 ④ 之前」，**经典模式 31 张 ⇒ `TooManyCards`** 这一档不再由本句覆盖，但：
① 同文件 `:211` 仍在测同一分支的 `cards.Count > want`（30 张经典卡组拿去按遭遇 12 张校验 ⇒ `TooManyCards`）；
② 同文件 `:156`（29 张 ⇒ `TooFewCards`）仍在测 `cards.Count < want`；
③ `B141_W31_Assert.cs` 的 B3b 也覆盖张数闸（⚠️ 该文件**尚未接线**，见 W31 报告 §七）。

---

## 三、`DeckRulesTest.cs:422` —— 缺 `;` **照收**（3 条断言）

**改前**

```csharp
CheckTrue(RuleEngine.DeckLibrary.ImportString(b64("没有分号"), look) == null, "少了 `;gameMode` 尾巴 → null");
```

**为什么必须改**（现读 `DeckLibrary.cs:471-484`）：`semi = text.IndexOf(';')`，`semi < 0` ⇒ `head = 整串`、`gameMode` 保持 **0**（`int.TryParse` 那一支整段被跳过），
继续按 `head.Split(':')` 解析 —— 这就是原版 `CardDeck__DeserializeDeckString.c:54-59`（`Split(';').Length > 1` 才 `Int32.Parse`）+ `:61-69`（只取 `[0]`）。
⇒ `ImportString(b64("没有分号"), look)` 返回 **非 null** 的 `PlayerDeck`：`Name = "没有分号"`、`GameMode = 0`、`CardIds` 为空（`PlayerDeck` ctor `:768` 对非空名原样保留、`:771` 空表 ⇒ `new List<string>()`）。

**改后**（1 行 → 1 行，同一物理行内追加语句）

```csharp
var noSemi = RuleEngine.DeckLibrary.ImportString(b64("没有分号"), look);
CheckTrue(noSemi != null, "★ 少了 `;gameMode` 尾巴 ⇒ **照收**（原版 `CardDeck__DeserializeDeckString.c:54-59`：只有 `Split(';').Length > 1` 才解析尾巴，否则 `gameMode` 保持 0 并继续解析 `[0]`）｜🧨 恢复 `return null` ⇒ 本句红");
if (noSemi != null) { Check(noSemi.GameMode, 0, "……模式 = 经典（0）"); Check(noSemi.Name, "没有分号", "……名字 = `Split(':')[0]`"); Check(noSemi.CardIds.Count, 0, "……卡表为空（串里本来就没有卡）"); }
```

（上面为可读性拆成三行显示；**盘上是同一物理行**，所以行数零漂移。`if (noSemi != null)` 前面有**硬前提断言** `CheckTrue(noSemi != null, …)`
⇒ 不成立时**先红**，不是「静默跳过」。）

`:420`（空串 ⇒ null）与 `:421`（不是 Base64 ⇒ null）**未动**：空串走 `IsNullOrEmpty` 早退；`"只有一段"` 是中文，`Convert.FromBase64String` 抛 ⇒ `catch` 返回 null。

另外把上一行注释（`:419`，原文 `// ⑥ 不是这个格式的 ⇒ null`）就地订正为
`// ⑥ 不是这个格式的 ⇒ null（⚠️ **缺 `;` 的仍是这个格式** —— 原版 `Split(';').Length > 1` 才解析尾巴，见下一条）`（1 行换 1 行）。

**灭自证**：把 `DeckLibrary.cs:482` 的注释换回 `else return null;` ⇒ `noSemi == null` ⇒ **`CheckTrue(noSemi != null, …)` 红**（后面三条因 `if` 不进入而不跑，但前提那条已经红了，不是静默）。

---

## 四、顺手核对：W31 的改动**没有**打红这两份文件里别的断言（逐条现读）

| 断言 | 现读结论 |
|---|---|
| `B121_WC_Assert` §C `C1`（30 张同名 + `notDeckable` ⇒ `InvalidCard`） | ⑥b（`DeckRules.cs:498-499`）在 ⑦ 之前 ⇒ 仍回 `InvalidCard`，不受闸序影响 |
| `B121_WC_Assert` §D `D3`（异阵营 ⇒ `InvalidArmy`） | 走 `CanAdd`，W31 未动 |
| `B121_WC_Assert` §E/§F/§G | 与本批无关 |
| `DeckRulesTest` `:133/:136/:139/:146/:150/:156` | 都只看单条闸，未被闸序影响（`:156` 29 张 ⇒ 27+2 份不越限 ⇒ 仍 `TooFewCards`） |
| `DeckRulesTest` `:183`（传说 2 张 ⇒ `CopyLimitExceeded`） | 20/21 张不变 ⇒ 与闸序无关，仍绿 |
| `DeckRulesTest` `:206`（非传说正好 2 张 ⇒ `None`） | 不受影响 |
| `DeckRulesTest` `:211`（经典 30 张按遭遇校验 ⇒ `TooManyCards`） | 该副牌 3 张传说 ≤ 4 ⇒ ⑧ 不先触发；张数闸仍是唯一命中 ⇒ 绿 |
| `DeckRulesTest` `:387/398-417`（导出/导入往返、丢池外卡） | 都带 `;0` 尾巴，与 3-24 无关；3-24② 只改「多余督军/防御卡丢弃」，这些串各只有 1 张督军 + 1 张防御卡 |
| `DeckEditorState` 自检（`⑦` / `⑦b` / `⑦c` 分享串往返） | `ExportString` 一律带 `;gameMode`，3-24 不影响 |

⚠️ **未改、也未验的一处**：`B121_WC_Assert.cs:131-132` 的注释已过期（「原来只放 1 张 ⇒ ④ 张数档回 `TooFewCards`」—— ④ 现在在最后），
但那段**代码行为仍成立**（⑥b 在 ⑦ 之前 ⇒ 30 张同名不撞同名上限）；按「只改白名单内该改的」没动它，**留给卡组编辑域写手一并订正**（W31 报告 §六 ❓4 已记）。

---

## 五、没做的事（如实）

1. **没有跑 Unity**（红线）⇒ 三条断言**没有实际执行过**；上面的「改后会绿 / 改回会红」都是**现读代码 + 判据的静态推导**。
2. **没有动 `DeckRuntime.cs` / `DeckEditorState.cs` 的 `{0}` 补丁**（`CopyLimitExceeded` 的 `MaxCopiesReached` 上屏接线）—— 白名单外，按 W31 报告 §六 ❓1 由调度台另派。
3. **没有改 `B141_W31_Assert.cs` 的接线**（`RuleEngineTest.cs` 不在白名单）。
