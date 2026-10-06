# RO · 文字半边与压暗层（A798 · A799 · A796 · 只读普查）

> ⚠️ **本文件由主对话代录**：跑这一趟的是**只读代理**（无 Write 工具）⇒ 它把全文写在交件消息里，**由主对话落盘**（结论/数字/站点**一字未改**，只压掉了重复的说明性套话与它自己的过程叙述）。
> **读数时刻 = 2026-10-06 13:46–13:55（本机）**，工程根 `d:/4/Unity/MyGame/Assets/CardPresentation`。
> 🔴 **读到的是「有别人在写」的工作区**：此刻 `git status` 有 31 个 `.cs` 为 `M`；普查期间 `Editor/MainMenuScene.cs` 的 mtime 从 13:35 变 13:51 ⇒ **所有行号都会漂**，锚点按**节点名 / 句子 / 段名**。
> 判据原文 = `资料/普查产出_1013/批次计划_1013.md` §六·B 的 A798/A799/A796 · 来源报告 = `WA781_文字半边.md` · `WA788_压暗层点击.md`。

---

## 一 · A798 `MenuDraw.Text` / `TextBox` 不做「整块在框外 ⇒ 不建」那道闸

### ① 现状
| 件 | 有没有闸 | 位置 |
|---|---|---|
| `MenuDraw.Text` | ⛔ **没有** | `Shell/MenuDraw.cs:1570`（签名）· `:1576` 建 · `:1581` 只裁 |
| `MenuDraw.TextBox` | ⛔ **没有** | `Shell/MenuDraw.cs:1623` · `:1632` 建 · `:1640` 只裁 |
| `MenuDraw.Rect` | ✅ 有 | `Shell/MenuDraw.cs:1294` → `ClipRect(...) == false ⇒ return null` |
| `MenuDraw.Nine` / `Tiled` / `Hit` / `DeckCell` | ✅ 有（同一条） | 转调 `ClipRect`（`:191`）；求交在 `:205` 调 `Visible` |
| `MenuWindowBase.Text` / `TextBox` | ✅ 有 | `Shell/MenuWindowBase.cs:308` / `:339` |
| `MenuDraw.Text`/`TextBox` 自认 | —— | 注释 `Shell/MenuDraw.cs:1563-1566`「本方法**不做**那道闸 …… 另立账，⛔ 别在这儿顺手加」 |

`MenuDraw.Text` 今天**也会返回 `null`**（`:1577` / `TextCore` `:1596`，只在 `Label.Create` 失败时）⇒ 契约**本来就是「可能 null」**。

### ② 结论 / 清单
**口径**：`grep -rn "MenuDraw\.\(Text\|TextBox\)(" --include=*.cs .` ⇒ **206 行命中**（4 处注释 + 3 处字符串字面量）⇒ **代码调用点 = 199 个**（`Text` **146** / `TextBox` **53**）· **45 个文件**（生产 41 · `Editor/` 4）。⚠️ WA781 记的「200 处」漂了 1 处。

**形态分布（199）**：**赋值 141** · **纯语句 52**（丢弃返回值） · **其它 6**。

**🔴 「其中多少处已经在判 null」= 141 个赋值点里，解引用前无守卫的：`0` 处。**
| 档 | 数 | 说明 |
|---|---|---|
| A · 返回值**从不**在作用域内解引用 | **72** | 丢弃 / 交给**自带 null 挡**的 `AlignLeft`(`:1651`)/`AlignRight`(`:1657`) |
| B · **先守卫后解引用** | **30** | 例 `Shell/SettingsWindow.cs:2105→2116` |
| C · **守卫与解引用同一行** | **39** | 例 `Shell/ItemDrawer.cs:1000` `lb != null ? … : 0f` |
| D · **解引用在前、守卫在后 / 无守卫** | **0** | —— |

非守卫非 Align 的**只有 4 组**，逐条都安全：`AllianceMemberOptionsPopup.cs:354`（读点 `:407` 带判）· `EnergySinglePlayerOnlyEventWindow.cs:491`（`_titleLabel` 全仓 0 消费者）· `Battle/CardDisplayWindow.cs:261/270`（`if (_fxWho[i] != null)`）· `Editor/MainMenuScene.cs:8756/8763`（`SpanOf` 首句自带判）。
最密文件：`CardDetailPopup` 15 · `BaseOfferPopup` 13 · `EnergySinglePlayerOnlyEventWindow` 12 · `ReferralPopupWindow` 11 · `DailyRewardPopup` 11 · `DailyStreakPopup` 10 · `PurchasePremiumWindow` 9。

**「改的话要动多少处」= 0 处调用点。「最小改法」= 两个入口各加一行**（`Text` 放 `TextCore` 调用**之前**、`TextBox` 同）：
```csharp
if (!Visible(r, _st.RenderClip)) return null;   // 整块在框外 ⇒ 连节点一起不建（同 Rect/Nine/Hit 那一档）
```
⛔ **别加进 `TextCore`**（内层；`TextBox` 必须在 `SetWrapWidth`/`SetAutoFitBox` 之后仍判一次，且内层被两处共用）。
语义代价 = 0 画面差（框外 ⇒ 零面积 ⇒ 今天也画不出像素）；收益 = 与同族四个静态件一致。
⚠️ **残留风险（不是 NRE）**：返回 null 会让「拿 label 的 `transform` 当父件」的点落到兜底分支（例 `DailyStreakPopup.cs:347`）⇒ **层级会变**（已有守卫，不炸）。

### ③ 判据齐不齐
✅ **齐**。账上「200 个调用点没逐个核 ⇒ 改动面没查清」**可结清**。⛔ 没算的只剩「加闸后有没有**既有断言**会红」（要跑 Unity；`Editor/MainMenuScene.cs` A781 那段的夹具矩形与 clip 有交集 ⇒ 那几条**不会被闸影响**）。

### ④ 派工
**1 个写手**，独占 **`Shell/MenuDraw.cs`**（+2 行 / 改 2 处 doc）。配断言 ⇒ 落 `Editor/MainMenuScene.cs` 的 A781 段旁（**热点文件，必须与在飞写手错开**）。

---

## 二 · A799 `clip == null` 时父链接管 —— ~145 处没解父链

### ① 现状 / 可复算的判定方法
**VC 挂点**（`grep -rn "ViewportClip\.Hang(\|AddComponent<ViewportClip>" ./Shell ./Battle ./Core ./Deck`）= **45 处 / 27 个生产文件**。
主要宿主：`CollectionWindow`(5) · `EnergySinglePlayerOnlyEventWindow`(2) · `CampaignTab`(2) · `ForgeTab`(2) · `LeaderboardWindow`(3) · `PracticeModePopup`(2) · `AllianceMemberTab`(2) · 其余各 1（`PurchasePremiumWindow:512` · `RankedRewardEventWindow:397` · `RewardWindow:1067` · `InboxWindow:292` · `ChatPanel:523` · `DailyStreakPopup:363` · `BattleLogPopup:161` · `BattleLogTab:89` · `SettingsWindow:665` · `AchievementsMenu:177` · `AvatarTab:158` · `TitleTab:131` · `ShopWindow:404` · `AlliancesTab:465` · `FriendsTab:219` · `DeckSelectionPopup:358` · `LiveOpsEventWindow:580` · `RankedTab:407` …）。

**三步复算法**（纯静态、可重跑）：① 逐文件建「VC 节点变量」表；② 逐调用点取 `MenuDraw.Text/TextBox` 的**第 1 实参 `parent`**，在同文件里递归解 `Node/Rect/Hang/Find/transform` 到根；③ 链上遇到 VC 变量 ⇒ 会改行为；链断在**窗根 / 包装器形参** ⇒ 不会（**VC 子树不含窗根** —— 结构论证）。
🔴 **第三层筛子**：链上有 VC **还不够** —— 若该处（或其包装器）**已显式 `ClipText`/已传 `clip`**，A781 那一刀**同框幂等** ⇒ 不算新被裁。现成「已裁」名单 = `grep -rn "MenuDraw\.ClipText(" ./Shell` ⇒ **16 处 / 10 文件**（`AllianceMemberTab` · `ChatPanel` · `CollectionWindow` · `ItemDrawer` · `MenuWindowBase` · `PlayerProfileWindow` · `PracticeModePopup` · `ReferralPopupWindow` · `SettingsWindow` · `WindowsManager`）。

**实跑结果**：VC 命中 **8 个调用点**（去 Editor 夹具后 **2 处**）· 链停在窗根 **181 处** · 链停在包装器形参 **16 组**。

### ② 抽样 20 处（逐条解链）——只列**会新改行为**与**账上记错**的
- ✅ **会（账上已点名 3 处）**：`PurchasePremiumWindow.cs:698`（`Army Name`）· `:711`（`Premium Text`）· `RankedRewardEventWindow.cs:530`（`Army Text`）。
- 🔴 **会，且是新查出（账里没有 / 或判错）**：`Shell/LeaderboardRow.cs:163`（Result）· `:206`（Name）· `:213`（Guild Name）· `:223`（Points）—— 父链经 `LeaderboardWindow._listContent`（VC）**且该文件 `grep ClipText` = 0**；`Shell/MatchLogRow.cs:153`（`Text`）—— 父链经 `BattleLogPopup._content`（VC）**且零 `ClipText`**。
  ⇒ **完整清单 = 8 处**（3 已点名 + **5 新**）。
- ⚪ **不会新增（同框幂等，已裁过）**：`ItemDrawer.cs:999`（账上「最可疑」—— 同函数 `:1021` 已有 `ClipText`，**A773 已收**）· `ItemDrawer.cs:1262` · `ChatPanel.cs:677` · `AllianceMemberTab.cs:785` · `SettingsWindow.cs:1765/2105` · `CampaignTab.cs:516`。
- ❌ **链断在窗根 / 全文件零 VC**（不会）：`CampaignRewardWindow.cs:405/409` · `EnergySinglePlayerOnlyEventWindow` 12 处 · `LiveOpsEventWindow` 8 处 · `InboxWindow.cs:271/300/311` · **`LeaderboardWindow.cs` 自己的 4 处**（`:372/577/660/671`）· `ReferralPopupWindow` 11 处 · `DailyRewardPopup` 11 处 · `TrophyInfoPopup` 5 · `BaseOfferPopup` 13 · `SkirmishEventWindow` 5。

**🔴 与账上不符**：账写「Leaderboard 4 处不受影响」—— 那是 `LeaderboardWindow.cs` **自己**的 4 处（确不受影响）；但 `LeaderboardRow.cs` **另有 4 处**在 VC 之下、且零 `ClipText` ⇒ **A781 会给它们首次上裁**。`MatchLogRow.cs:153` 账里完全没提。⛔ **别去「修」这两处代码** —— **新裁是目的**（原版就裁）；要动的是**补断言/注释**。

### ③ 剩下怎么批量判
**甲类（链停窗根 181 处）**：判「不会」，依据是结构论证；要抽检就抽「链最后一跳不是 `root`/`transform` 的」。
**乙类（链停包装器形参 16 组）**：必须再解一跳找调用方。全量名单 = `ItemDrawer.TextCentered`/`ClippedText` · `ChatPanel.Text` · `MatchLogRow.Text`/`TextSide` · `LeaderboardRow.Build` · `AllianceMemberTab.TextSoft` · `SettingsWindow.Text`/`InputField` · `MenuWindowBase.Text`/`TextBox` · `OfferContainer.LabelFit` · `CostCurveDrawer.Build` · `SocialWindow.Text` · `WindowsManager.Text` · `PlayerProfileWindow.Text` · `MissionRerollPopup.BuildButton` · `RankedRewardEventWindow.BuildCard` · `CampaignTab.BuildNodeReward`。其中**已核**：`ItemDrawer` / `ChatPanel` / `MatchLogRow` / `LeaderboardRow` / `RankedRewardEventWindow.BuildCard` / `CampaignTab.BuildNodeReward` / `SettingsWindow` / `AllianceMemberTab.TextSoft` / `CostCurveDrawer.Build`（调用方两文件都零 VC ⇒ 不会）。**`MenuWindowBase.Text/TextBox` 是特例**：自己就调 `ClipText` ⇒ 子类在 A781 之前就已经裁，**幂等**。
**丙类**：对甲乙里链上命中 VC 的点，再看有没有显式 `ClipText`（16 处名单）。

### ④ 判据齐不齐
⚠️ **方法齐、抽样齐、全量清单不齐**：181 处甲类还没逐条打印成表（机械劳动，可用三步法脚本一次跑完）。
⚠️ **三步法的已知盲区**：① 同名变量跨作用域复用；② `P.Find("…")` 运行时才知道父；③ 循环里重建的节点 ⇒ 都要逐条打印链才能定案。

### ⑤ 派工
**只读 1 人**跑全量表（零独占）· 若要顺手改两处过期注释（`PurchasePremiumWindow.cs:585-589` · `CampaignRewardWindow.cs:867` = **A797**）⇒ 独占那 2 个文件。
⛔ **不要动 `LeaderboardRow.cs` / `MatchLogRow.cs` 的代码**；它们的断言宿主是 `Editor/RewardsScene.cs` / `MainMenuScene.cs`（热点，需错开）。

---

## 三 · A796 「点了会不会关」在自检里有没有站立点

### ① 现状
**`MenuDraw.CheckShadeRule`**（`Shell/MenuDraw.cs:2128`）= 3 个 `chk(...)`（`:2133`/`:2137`/`:2142`），第一条内含 4 条子判据 ⇒ **合计 6 条**：
① 命中区**节点在** · ② 它下面真挂 `ImageQuad` · ③ 档号 == 视觉压暗层那颗 quad 的档号（量场景真值）· ④ 档号严格 < `qContentMin` · ⑤ 是公共件 `MenuDraw.ShadeHit` 建的（`WasShadeHit`）· ⑥ `ShadeHit` 对这颗没报过档位告警。调用点 **24 处**（`CollectionScene` 4 · `MainMenuScene` 11 · `RewardsScene` 5 · `ShopScene` 3 · `SettingsScene` 1）。
⇒ ✅ 账上「**6 条一条都不问点击**」成立。

**🔴 但「11 条自检零站立点」不成立**：`CheckAbsorbRule`（5 宿主副本：`MainMenuScene.cs:73` · `CollectionScene.cs:78` · `RewardsScene.cs:191` · `SettingsScene.cs:100` · `ShopScene.cs:837`；**26 个调用点**）**已在做真点击**（`pl.ClickAt` ⇒ 断 `state() == Closed`）⇒ **「点了会关」有 26 个站立点**。

**缺口真实形状**：(a) `CheckShadeRule` 的 24 个调用点里**没配 `CheckAbsorbRule` 的窗**（卡包详情 / 开包 / 卡片详情）只有几何断言；(b) **「点了**不会**关」这一方向全仓零站立点**（今天没有一扇窗需要它）；(c) 6 条里**没有一条认过「这颗命中区绑的是哪个动作」** ⇒ A788 那一类「按字段推行为」的错抓不住。

### ② 结论 / 落点 / 现成读口
**读口（全 public，无需新增）**：`WindowButton.Click()` / `ClickForTest()`（`Shell/PromptPopup.cs:1095` / `:1119`；`absorbOnly` 在 `:1100` 早退）· `PointerLayer.Instance`/`ClickAt`/`ButtonAt`（`Shell/PointerLayer.cs:56`/`:926`/`:935`）· `MenuDraw.WasShadeHit`/`WasAbsorb`（`:1893`/`:1973`）· 窗的 `CurrentState`（`WindowState`）。
**推荐落点 (a)**：在 `Shell/MenuDraw.cs` 新增 `CheckShadeClickRule(chk, what, winRoot, darkHit, Func<WindowState> state)`，两条**互为对照**：① 点**前** `state() == Open`（防「两边一起改回去」）② 对 `darkHit` 取 `WindowButton` 调 `Click()`（或 `ClickAt` 一个**钉死**的点）⇒ 断 `state() == Closed`。**改坏法**：把 `ShadeHit(..., () => Close())` 换成 `Absorb` ⇒ `absorbOnly` 早退 ⇒ ②**必红**。
⚠️ 代价：`CheckShadeRule` 现有 24 个调用点签名不动（新开一口），但**要逐个补调用**（需要窗对象的 `state`）。
（备选 (b)：落各宿主 —— 零签名改动，但会长成 5 份副本，正是收口反对的形状。）
顺带：`CheckAbsorbRule` 的 5 份副本也该收进 `MenuDraw`（**另一笔账**，A796 里只记不做）。

### ③ 判据齐不齐
✅ **齐**。⚠️ 需调度台先裁：补断言要动宿主文件（`Editor/{MainMenuScene,RewardsScene,CollectionScene,ShopScene,SettingsScene}.cs` —— **全是多写手共用宿主**）。

### ④ 派工
**1 个写手**做 (a) 的**新口**（独占 `Shell/MenuDraw.cs`；⚠️ 与 A798 那 2 行**同一文件** ⇒ **两账排同一批、串行**）· **1 个写手**补调用点（先只补 `MainMenuScene` 11 处 + `RewardsScene` 5 处，需与在飞写手错开）。

---

## 四 · 总表
| 账 | 判据齐不齐 | 关键数字 | 建议写手 | 独占文件 |
|---|---|---|---|---|
| **A798** | ✅ 齐（可结清「改动面没查清」） | 199 调用点 / 141 赋值 / **0 处无守卫解引用** / **改 0 处** | 1 | `Shell/MenuDraw.cs` |
| **A799** | ⚠️ 方法齐 + 抽样 20，全量表未跑 | VC 挂点 45 / 27 文件；**会新裁 = 8 处**（3 已点名 + **5 新**） | 1（只读） | 无 |
| **A796** | ✅ 齐 | 6 条断言 / 24 调用点 / **已有 26 个真点击站立点** | 1–2 | `Shell/MenuDraw.cs`（与 A798 串行）+ 5 个 `Editor/*Scene.cs` |

> ⛔ 原报告自陈：**没跑 Unity、没改任何 `.cs`、没动 git**；所有结论都是**静态实读**。
