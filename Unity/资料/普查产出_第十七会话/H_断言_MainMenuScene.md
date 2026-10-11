# H_断言_MainMenuScene —— 练习窗六处文案走词条 · 结构断言

文件：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（LF · 12931 行 · 动手前 12892 行，二进制核过行尾：`crlf 0 / lf 12892`，改完仍 `crlf 0 / lf 12931`）
本代理**只动了这一个文件**（`git diff --numstat` = **59 增 / 4 删**，其中的 4 删与另 20 余增是主对话本轮/上一轮就有的改动，见 §4）。

---

## 1. 加了/改了几条（哪一行 + 一句话）

**新增 2 条前提 + 2 条文案断言**（本批的全部产出）：

| 行 | 内容 | 一句话 |
|---|---|---|
| `:1697` | `CheckTrue(Loc.HasEntry("MainMenu/RankedWindow/ChangeDeck"), …)` | 🆕 前提：词条在表里（不在 ⇒ 下一条两边一起退化成键名 = 假绿） |
| `:1700` | `CheckText(TextOf(chgT), Loc.T("MainMenu/RankedWindow/ChangeDeck"), …)` | 🆕 **`Change Deck Text`** 的字 = 原版 `mTerm`（第 5 处） |
| `:1757` | `CheckTrue(Loc.HasEntry("MainMenu/MainButtons/ButtonLabel/Back"), …)` | 🆕 前提：同上 |
| `:1760` | `CheckText(TextOf(FindChild(pw.transform, "Back Text")), Loc.T("MainMenu/MainButtons/ButtonLabel/Back"), …)` | 🆕 **`Back Text`** 的字 = 原版 `mTerm`（第 6 处） |

**零改**：本文件原有的一切断言一字未动（含 §5 那三条**就该保持英文**的旧断言）。

**「已断」的四处是主对话本轮改的**（`A1501`），⛔ 我没重复改：`:1710`/`:1713`（`CardCostBalance`）· `:1763`/`:1764`（`ToBattle`）· `:1766`/`:1767`（`SelectDeck`）· `:1769`/`:1770`（`GameMode`）。

---

## 2. 每条「改回什么 ⇒ 这条红」

批量口径：**六处一律** `TextOf(那颗 TMP) == Loc.T(硬写在本文件里的键)`。
🧨 鉴别式 = **把实现改回「直接画资产里的 `m_text`」**（即 `Shell/PracticeModePopup.cs` 的那六个 `Txt(…, Loc.T(键), …, "节点名", …)` 各把第 2 实参换回英文原文字面量）：

| 处 | 改回什么（实现侧） | 中文档下实得 | `Loc.T(键)` | ⇒ |
|---|---|---|---|---|
| `CardCostBalance` | `"Card / Energy cost"` | `Card / Energy cost` | `卡牌 / 能量费用` | 红 |
| `ChangeDeck`（🆕） | `"Change Deck"` | `Change Deck` | `更换卡组` | 红 |
| `Back`（🆕） | `"Back"` | `Back` | `返回` | 红 |
| `SelectDeck` | `"Select deck to play"` | 英文 | `选择卡组开始游戏` | 红 |
| `GameMode` | `"Game mode"` | 英文 | `游戏模式` | 红 |
| `ToBattle` | `"Battle!"` | 英文 | `开战！` | 红 |

⛔ **键名逐颗写死在本文件**，**不读** `Shell/PracticeModePopup.cs` 的任何常量/表/字段 —— 读了 = 实现与检测器同一个口、两边一起改回去照样绿 = 自证。
⚠️ **判别力只在「有自己文案的语档」下成立**：`Loc.T` 按当前语档取列（`Core/Loc.cs:2310-2312`），而**六个键的英文列恰好逐字等于原版 `m_text`** ⇒ **英文档下这条等式不具鉴别力**（另四处同形）。跑批走的是出厂默认 `Loc.Default = Chinese`（`Core/Loc.cs:84`）⇒ 有鉴别力。**详见 §5 第 ① 条。**

---

## 3. 六处逐颗的状态表

| # | 节点（原版） | 键（硬口径） | 状态 | 出处 |
|---|---|---|---|---|
| 1 | `Deck Information Cost/balance text` | `Demo/CardCostBalance` | **已断**（主对话 `A1501`） | `:1710` 前提 + `:1713` 文案 |
| 2 | **`Change Deck Text`** | `MainMenu/RankedWindow/ChangeDeck` | **🆕 新加** | `:1697` + `:1700` |
| 3 | **`Back Text`** | `MainMenu/MainButtons/ButtonLabel/Back` | **🆕 新加** | `:1757` + `:1760` |
| 4 | `tooltip` | `Demo/DeckSelectionDemo/SelectDeck` | **已断** | `:1766` + `:1767` |
| 5 | `Toggle Label` | `Event_Description/Generic/GameMode` | **已断** | `:1769` + `:1770` |
| 6 | `Battle Text` | `Demo/ToBattle` | **已断** | `:1763` + `:1764` |

**六处全部断到了**，无一处落空。两处新增的**判据取自任务简报那张表**（代理 A 逐颗打印 `Practice Mode Menu` 全部 129 个后代的 `mTerm`/`m_text`），**我没有重查**。
六处的**实现侧**已全部就位（`Shell/PracticeModePopup.cs:610 / 641 / 804 / 824 / 838 / 848`，均 `Loc.T(键)`）⇒ 这六条应**全绿**。

**为什么 `Change Deck Text` 与 `Back Text` 原来没断**：它们当时只断了**结构**（`:1673`-`:1678`：`chgT` 挂在 `General container` 下；`Back` 只断了圆钮 `Back Bg` 的坐标 `:1747`）—— 文案那一步没人接。

**中文口径（任务第 4 点）**：**要转吗 → 不要。** 现读结论：
- 这六颗走 `PracticeModePopup.Txt(...)`（`Shell/PracticeModePopup.cs:1955`）→ `Label.Create`，**路径里没有任何 `ToUpperInvariant`**。
- 本文件的 `ToUpperInvariant` 只有 `Shell/MenuWindowBase.cs:564` 那处（**页签**那一族）—— 与练习窗六处**无关**。
- `Shell/PracticeModePopup.cs` 里那两处 `ToUpperInvariant`（`:1332`/`:1337`）转的是**阵营名**，不是这六处。
⇒ **六处期望值不加 `.ToUpperInvariant()`**（与社交窗那两颗**形状不同**，那两颗是页签款、基类真的会转）。
⇒ 顺带：中文没有大小写，`Loc.T` 的中文档原样印出（`返回` / `更换卡组`）；英文档原版印的也是**首字母大写**的 `Back` / `Change Deck`，**不是** `BACK`。

**基线（任务第 3 点）**：这六条**全部跑在「开窗后的出厂态」里** —— 练习窗是 `:1629` 的 `pwb.Click()` **当场新开**的（动手前 `grep` 过：`:1629` 之前全文件只有 `:100` 一句 doc 注释提到 `PracticeModePopup`，**没有更早的开窗**），而六条断言都在 `pw.ToggleDeckInfo()`（`:2876`）**之前**，所以 `Deck info ▸ General container` 一定开着（那组互斥断言就在 `:1740`）⇒ **无需额外摆回基线**。
🔴 **这条我写进了注释里当护栏**：⛔ 别把这批挪到 `ToggleDeckInfo()` 之后 —— 那会把 `General container` 关掉，而 `FindChild` **含 inactive**（`Editor/MenuCheck.cs:144` 的 `GetComponentsInChildren<Transform>(true)`）⇒ 会「找得到、字也在」、**看着仍绿**（`CardCostBalance` / `ChangeDeck` 两处受影响）。

---

## 4. 没做到的（为什么）

无。本批两处全部加上、六处全部有断言、类型检查两读数 0。

**附：`git diff --numstat` = 59/4 的构成**（说明我**没有**多改）——
本文件在本会话开头就已经是 `M`（主对话的改动）：四处 `Loc.T` 期望值（`:1710-1713` · `:1763-1770` 四组）· 社交窗那条 `activeSelf` 补断言（`:7273` 附近）· 末尾 `SceneStubAudit.Run(ScenePath)`（`:12914`）。**这四笔都是别人的，我未触碰**；我只贡献了 §1 那 4 条（+39 行）。

---

## 5. 顺手发现（⛔ 我没改，交调度台分流）

**① 🔴 最有价值的一条：这六条（含主对话那四条）的抗伪能力**取决于**跑批时的语档**，而**没有人钉语档**。**
`Loc.Current` 从 `PlayerPrefs["Language"]` 载入（`Core/Loc.cs:2459-2472`），`Editor/MainMenuScene.cs` 的 `Run()` **全程不设语档**（全文件只有 `:11563-11577` 那条 `A1025` 借了 `RestoreForTest` 又还回去）。
⇒ **本机 `PlayerPrefs` 若被设成 English**，`Loc.T(键)` 就返回英文列 = **原版 `m_text` 原文** ⇒ 「实现改回写死英文」**六条全绿、看不出任何区别**（= 恒绿）。
**判据**：六个键的英文列逐字等于原版 `m_text`（`Core/Loc.cs:399-403 / 580`）。
**建议**（⛔ 我不自行发明口径）：要根治得在这六条外面**钉一次中文档 + 重建**（`Loc.RestoreForTest(Chinese)` 必须在 `pwb.Click()` **之前**，因为字是建窗时画的），或改断「切语档后字**跟着变**」——后者才是语档无关的真鉴别式。**两种都动到别人的地盘，请调度台裁。**

**② 六条（+ 主对话那四条）都是 `FindChild` 取件 ⇒ 没有一条断 `activeSelf`。**
同族先例就在本文件 `:7273` 附近（社交窗两键的 `br.gameObject.activeSelf`，`A1498` 补的）。
**本批这六处**：`CardCostBalance` / `ChangeDeck` 两处**真的会被关**（父链 `_general`，`:680` 的 `SetActive(generalInfo)`）；另四处是窗根的直接子件、**实现里从不 `SetActive(false)`**（`grep SetActive Shell/PracticeModePopup.cs` 逐条核过）。
⇒ 若要补，**只有前两处有意义**，且必须**断在出厂态那一格**（关掉之后再断 `activeSelf` 会红）。⛔ 我没顺手加。

**③ 「同根因但没改」的三处站点**：本文件里对应它们的旧断言**我逐条核过、确认是对的、没动**：
`:3471`（`tut` = `TutorialModePopup ∷ "Game mode"`，源在 `Shell/TutorialModePopup.cs:148` 的 `TitleText` 常量）· `:5325`/`:5420`（`sk` = `SkirmishEventWindow : LiveOpsEventWindow`，源在 `Shell/LiveOpsEventWindow.cs:912` 的 `MenuDraw.Text(…, "Battle!", …)`）—— 两处都**硬编码英文**、**没走词条**，与简报的裁定一致。

**④ 六处的键在两份文档间无冲突**：`Core/Loc.cs:399-403 / 580` 的键名与简报那张表**逐字一致**（含 `Demo/CardCostBalance` 的拼法、`MainMenu/RankedWindow/ChangeDeck` 的路径）—— 没有「两处打架」。

---

## 6. 类型检查读数

```
TMPDIR=/tmp/wf_h2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
**两读数 0**（无「错误全在别人在写的文件上」的情况）。
⛔ 未跑 Unity · 未动 git —— 按红线。
