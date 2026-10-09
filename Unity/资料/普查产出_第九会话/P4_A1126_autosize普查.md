# P4 · `A1126` 普查：哪些 `MenuDraw.Text` / `TextBox` 调用点**不接 autosize**

> **执行**：第九会话 · P4 **只读普查代理** · 2026-10-18。
> **红线遵守**：没跑 Unity · 没跑 `typecheck.sh` · 没动 git · **一个源文件都没改**（本报告是本次唯一新增的文件；`git status` 里其余 20 余个改动**全是别的写手**的）· 没动三份正本。
> **口径**：本报告的子代理**无权改口径** —— §〇 那两条只做「现读推翻 + 写明」，**采纳与否由调度台拍**。
> ⚠️ **行号是快照**：普查期间另有 4 个写手在改源文件，我**至少实测到一次漂移**（`Shell/SettingsWindow.cs` 两个调用点从 `:3202/:3559` 漂到 `:3214/:3571`）。数据快照的 md5 见文末，复核时**先按 `grep` 模式重定位、⛔ 别信行号**。

---

## §〇 前提复核（简报里两条被现读推翻 / 收紧 —— ⛔ 我没有自行改口径，只是把现读钉下来）

#### 0·1 简报的「准确判据」**必要但不充分**（两条漏洞）

简报写：*「准确判据 = 调用方没传 `autoMinPx`（缺省 0）的调用点不接 autosize」*。现读 `Shell/MenuDraw.cs` 后，它漏了两条：

1. **`Text` 传了 `autoMinPx`、但没传 `wrapPx` ⇒ 照样不生效。** `TextCore` 现读是这样写的（`Shell/MenuDraw.cs`
   —— `Text` `:1809` · `TextCore` `:1843` · `TextBox` `:1876`；下面这个闸在 `:1851`、`SetAutoFitBox` 在 `:1856`）：

```csharp
if (wrapPx > 0f)
{
    lb.SetWrapWidth(LayoutSpace.Px(wrapPx));
    if (autoMinPx > 0f && fontPx > autoMinPx)
        lb.SetAutoFitBox(LayoutSpace.Px(wrapPx), LayoutSpace.Px(r.H), autoMinPx, …);
}
```
   （`Text` 的形参表 = `…, float fontPx, int q, float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f, PxRect? clip = null, Vector2 clipSoftness = default`；
   `TextBox` 的形参表次序**不同** = `…, float fontPx, float autoMinPx = 0f, int q = QText, float autoMaxPx = 0f, autoBasePx = 0f, …`，而且 `TextBox` **无条件** `SetWrapWidth(r.W)`。）
   ⇒ **`autoMinPx` 生效的最小条件 = `wrapPx > 0` ∧ `autoMinPx > 0` ∧ `fontPx > autoMinPx`**；`TextBox` 那一路 `wrapPx` 恒 = `r.W`。

2. 🔴 **调用之后另调 `SetAutoFitBox` 的有 45 处（生产 45 处全部在内）** —— 只看实参会把它们**误判成「不接」**。
   例（现读）：`Shell/LiveOpsEventWindow.cs:536` 的 `MenuDraw.Text(…)` **7 个实参、一个 autosize 参数都没传**，
   紧跟 `:549` 却是 `if (ndt != null) ndt.SetAutoFitBox(LayoutSpace.Px(NoDeckR-NoDeckL), LayoutSpace.Px(NoDeckB-NoDeckT), 18f, 45f, 36f);` ⇒ **它是接上的**。

⇒ **我用的判据（写清楚，供调度台裁）**：
**「一个调用点接上 autosize」= ① 实参生效（上式）** 或 **② 调用后对返回的那个 `Label` 调过 `SetAutoFitBox`**。
本报告把**三套口径的数字都列出来**（§一 的「有没有 `autoMinPx`」列把三者都写了），**没有偷偷换掉简报那一套**。

> ⚠️ ② 的识别是**文本扫描**（在掩掉注释的源码里找 `TGT.SetAutoFitBox(`，`TGT` = 该调用的赋值目标）。
> 代价：变量名在文件里被**赋值多次**的 7 处**指认可能不唯一**（`Shell/BaseOfferPopup.cs:1029/1047`、`Shell/MatchLogRow.cs:164`、
> `Shell/OfferContainer.cs:1369`、`Shell/PopUpGameWindow.cs:467`、`Shell/ReferralPopupWindow.cs:501/507`）—— 这 7 处的 ② 请人工核一遍。

#### 0·2 规模：简报给的启发式数（161 / 119）与现读不符

现读（我的脚本，见 §汇总数字的「怎么数的」）：**`MenuDraw.Text` 154 处 + `MenuDraw.TextBox` 53 处 = 207 处**（生产 **192** + `Editor/` **15**）。
简报那份启发式（7 参 114 …）是**按实参个数**数**未去注释**的源码，所以偏小/偏多都可能有；**本报告一律用现读这一份**。

---

## §一 逐调用点明细表

**读法**：
- 「有没有 `autoMinPx`」一列 = `有/无` + `生效 / 不生效`（逐处现读实参 + 现读 `FontPx`/`WrapPx` 的值）+ `调用后有 SetAutoFitBox`（§〇0·1 的 ②）。
- 「本普查判定」= `接上`（按 §〇 的合并判据）或档位 `A1/A2/A3/B/C`（定义见 §二）。
- 「框宽」列给的是**出处**（那个矩形的表达式）+ 本脚本能解析出的数值；`未量` = 解析不出（⛔ 我没猜）。

### 1·1 生产代码（`Assets/CardPresentation/` 去掉 `Editor/`）—— 192 处

| 文件:行号 | 调用 | 参数个数 | 有没有 `autoMinPx` | 文案来源 | 该框的宽度从哪来 | 本普查判定 |
|---|---|---|---|---|---|---|
| `Battle/CardDisplayWindow.cs:254` | Text | 10 | 有 `19.3f` · 生效 | 【变量】`TitleText` | `tr` = **426.7** | **接上** |
| `Battle/CardDisplayWindow.cs:289` | Text | 10 | 有 `10f` · **不生效** | 【字面量】"" | `whoR` = 未量 | A2 |
| `Battle/CardDisplayWindow.cs:305` | Text | 10 | 有 `10f` · **不生效** | 【字面量】"" | `whatR` = 未量 | A2 |
| `Battle/CardDisplayWindow.cs:793` | Text | 10 | 有 `10f` · 生效 | 【变量】`body` | `r` = **1250.0** | **接上** |
| `Core/CostCurveDrawer.cs:86` | Text | 7 | **无** · **不生效** | 【拼接】i.ToString() | `new PxRect(ncx - 23f * scale, y1, ncx + 23…` = 未量 | A2 |
| `Core/CostCurveDrawer.cs:109` | Text | 7 | **无** · **不生效** | 【拼接】n.ToString() | `new PxRect(ccx - 23f * scale, y1, ccx + 23…` = 未量 | A2 |
| `Shell/AllianceEventScoreInfo.cs:137` | Text | 7 | **无** · **不生效** | 【字面量】"1256" | `sr` = 未量 | C |
| `Shell/AllianceEventScorePanel.cs:114` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`PlaceholderName` | `nmR` = 未量 | **接上** |
| `Shell/AllianceEventScorePanel.cs:133` | Text | 7 | **无** · **不生效** | 【词条】`MainMenu/RankedWindow/Leaderboard` | `vtx` = 未量 | C |
| `Shell/AllianceEventScorePanel.cs:152` | TextBox | 10 | 有 `18f` · 生效 | 【词条】`SocialMenu/Alliances/JointToEarnRewards` | `jtR` = 未量 | **接上** |
| `Shell/AllianceEventScorePanel.cs:166` | Text | 7 | **无** · **不生效** | 【词条】`MenuDeck/HUD/SearchFilter` | `Off(JoinBtnTxR, x1, y1)` = 未量 | C |
| `Shell/AllianceEventScorePanel.cs:176` | Text | 7 | **无** · **不生效** | 【词条】`MainMenu/RankedWindow/Leaderboard` | `Off(NoLbTxR, x1, y1)` = 未量 | C |
| `Shell/AllianceMemberOptionsPopup.cs:365` | TextBox | 10 | 有 `NameFontMin` · 生效 | 【变量】`_view.Name ?? ""` | `nameR` = 未量 | **接上** |
| `Shell/AllianceMemberOptionsPopup.cs:388` | TextBox | 10 | 有 `BtnTextFontMin` · **不生效** | 【变量】`BtnLabel(bt)` | `BtnTextRect(br)` = 未量 | A2 |
| `Shell/AllianceMemberTab.cs:816` | TextBox | 8 | 有 `autoMinPx` · **不生效** | 【变量】`text` | `r` = 未量 | A2 |
| `Shell/AlliancePanelWindow.cs:345` | Text | 7 | **无** · **不生效** | 【变量】`text` | `r` = 未量 | A2 |
| `Shell/BaseOfferPopup.cs:839` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`C.Name` | `G.Title` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:852` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`C.Type` | `G.Category` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:867` | TextBox | 10 | 有 `3f` · 生效 | 【变量】`DefDescText` | `G.Desc` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:875` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`C.Available` | `G.Avail` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:910` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`C.BadgeText` | `G.BadgeText` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:931` | Text | 7 | **无** · **不生效** | 【变量】`C.TimerText` | `G.TimerTextBox` = 未量 | A2 |
| `Shell/BaseOfferPopup.cs:951` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量】"" | `G.PriceBtnText` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:965` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`C.Price` | `G.PriceText` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:977` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`DefPrevPriceTitle` | `G.DiscTitle` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:979` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`DefPrevPriceValue` | `G.DiscPrice` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:995` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`DefWebShopText` | `G.WebText` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:1029` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`DefPreviewText` | `G.PreviewText` = 未量 | **接上** |
| `Shell/BaseOfferPopup.cs:1047` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量】"" | `AvatarHelpR` = **368.0** | **接上** |
| `Shell/BoosterInfoPopup.cs:422` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`ShownTitle` | `TitleR` = **516.7** | **接上** |
| `Shell/BoosterInfoPopup.cs:436` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`ShownCategory` | `CategoryR` = **516.7** | **接上** |
| `Shell/BoosterInfoPopup.cs:445` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`DescFor(o)` | `DescR` = **500.0** | **接上** |
| `Shell/BoosterInfoPopup.cs:459` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`CrateCounterText` | `CrateCounterR` = **485.3**（估算占比 **1.33**） | **接上** |
| `Shell/BoosterInfoPopup.cs:495` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`CounterSample` | `SliderCounterR` = **244.0**（估算占比 **0.38**） | **接上** |
| `Shell/BoosterInfoPopup.cs:589` | Text | 7 | **无** · **不生效** | 【变量】`ShownPrice` | `PriceR` = **232.2** | A2 |
| `Shell/BoosterInfoPopup.cs:607` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`WebShopText` | `WebTextR` = **132.0**（估算占比 **1.30**） | **接上** |
| `Shell/BoosterPackOpenWindow.cs:357` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`DiscoverText` | `HintR` = **708.3**（估算占比 **0.98**） | A3 |
| `Shell/BoosterPackOpenWindow.cs:366` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`CloseText` | `HintR` = **708.3**（估算占比 **0.42**） | B |
| `Shell/BoosterPackOpenWindow.cs:492` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`NewBadgeText` | `btR` = 未量 | C |
| `Shell/BoosterPackOpenWindow.cs:506` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`BannedText` | `banR` = 未量 | **接上** |
| `Shell/CampaignRewardWindow.cs:427` | Text | 11 | 有 `TitleAutoMin` · **不生效** | 【变量】`TxtGet` | `glowGetR` = 未量 | A2 |
| `Shell/CampaignRewardWindow.cs:431` | Text | 11 | 有 `TitleAutoMin` · **不生效** | 【变量】`TxtPreview` | `glowGetR` = 未量 | A2 |
| `Shell/CardDetailPopup.cs:535` | Text | 11 | 有 `10f` · 生效 | 【变量】`lore` | `r` = **1250.0** | **接上** |
| `Shell/CardDetailPopup.cs:651` | Text | 7 | **无** · **不生效** | 【字面量】"1" | `new PxRect(b.x1, b.y1, b.x1 + 122.88…` = **122.9**（估算占比 **0.16**） | B |
| `Shell/CardDetailPopup.cs:661` | Text | 7 | **无** · **不生效** | 【字面量】"This will consume a wildcard" | `new PxRect(1307.5f, 315.5f, 1726f, 3…` = **418.5**（估算占比 **1.34**） | A1 |
| `Shell/CardDetailPopup.cs:679` | Text | 7 | **无** · **不生效** | 【拼接】need.ToString() | `new PxRect(b.x1, b.y1, 1443.83f, b.y…` = **59.8** | A2 |
| `Shell/CardDetailPopup.cs:682` | Text | 7 | **无** · **不生效** | 【拼接】CardProgress.NeedGold[Mathf.Min(CardProg… | `new PxRect(1499.72f, b.y1, 1570f, b.…` = **70.3** | A2 |
| `Shell/CardDetailPopup.cs:687` | Text | 7 | **无** · **不生效** | 【拼接】"Will get: +" + CardProgress.Points[Math… | `new PxRect(1309f, 594.53f, 1729f, 65…` = **420.0** | A2 |
| `Shell/CardDetailPopup.cs:700` | Text | 11 | 有 `25f` · 生效 | 【字面量】"Maximum card tier reached" | `new PxRect(1314f, 430.62f, 1724f, 64…` = **410.0**（估算占比 **1.22**） | **接上** |
| `Shell/CardDetailPopup.cs:730` | Text | 7 | **无** · **不生效** | 【变量】`has ? "1 of 1" : "0 of 0"` | `new PxRect(1307.5f, 762f, 1726f, 800…` = **418.5** | A2 |
| `Shell/CardDetailPopup.cs:738` | Text | 7 | **无** · **不生效** | 【变量】`has ? "Alternate art" : "No altern…` | `new PxRect(bx, by, bx + bw, by + bh)` = **328.7** | A2 |
| `Shell/CardDetailPopup.cs:829` | Text | 11 | 有 `CcSgMin` · 生效 | 【拼接】"x" + inDeck | `rBox` = 未量 | **接上** |
| `Shell/CardDetailPopup.cs:840` | Text | 11 | 有 `25f` · 生效 | 【拼接】"x" + inDeck | `rC` = **73.3** | **接上** |
| `Shell/CardDetailPopup.cs:851` | Text | 11 | 有 `25f` · 生效 | 【字面量】"/ " | `rSl` = **28.0**（估算占比 **2.03**） | **接上** |
| `Shell/CardDetailPopup.cs:855` | Text | 11 | 有 `18f` · 生效 | 【拼接】spares.ToString() | `rX2` = **44.1** | **接上** |
| `Shell/CardDetailPopup.cs:885` | Text | 7 | **无** · **不生效** | 【字面量】"99" | `new PxRect(x + 30f, WcBgT, x + 71f, WcBgB)` = 未量 | C |
| `Shell/CardDetailPopup.cs:1007` | Text | 11 | 有 `10f` · **不生效** | 【变量】`text` | `new PxRect(x1, y1, x2, y2)` = 未量 | A2 |
| `Shell/ChatPanel.cs:148` | TextBox | 10 | 有 `autoMin` · **不生效** | 【变量】`s` | `r` = 未量 | A2 |
| `Shell/ChatPanel.cs:722` | TextBox | 8 | 有 `0f` · **不生效** | 【变量】`s` | `r` = 未量 | A2 |
| `Shell/DailyRewardPopup.cs:265` | Text | 7 | **无** · **不生效** | 【拼接】"Day " + (day + 1) | `dt` = 未量 | A2 |
| `Shell/DailyRewardPopup.cs:284` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.RewardDayCounter(day)` | `Offset(D_ProgCount, entry)` = 未量 | A2 |
| `Shell/DailyRewardPopup.cs:319` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.RewardName(day, prem)` | `R(D_Name)` = 未量 | A2 |
| `Shell/DailyRewardPopup.cs:342` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.RewardClaimedText()` | `R(D_ClaimedTex)` = 未量 | A2 |
| `Shell/DailyRewardPopup.cs:383` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.FreeTrackTitle()` | `FreeTitle` = **200.0** | A2 |
| `Shell/DailyRewardPopup.cs:387` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.PremiumTrackTitle()` | `PremTitle` = **200.0** | A2 |
| `Shell/DailyRewardPopup.cs:390` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.PremiumTrackPrice()` | `PremPrice` = **174.0** | A2 |
| `Shell/DailyRewardPopup.cs:451` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.HeaderArmyName()` | `HeaderTitle` = **234.8** | A2 |
| `Shell/DailyRewardPopup.cs:455` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.HeaderArmySubTitle()` | `HeaderSub` = **410.7** | A2 |
| `Shell/DailyRewardPopup.cs:482` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.MoreRewardsInText()` | `TimerMore` = **689.5** | A2 |
| `Shell/DailyRewardPopup.cs:497` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.RewardTimerText()` | `TimerText` = **463.8** | A2 |
| `Shell/DailyStreakPopup.cs:335` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakCurrentLabel()` | `S_CurLabel` = **490.9** | A2 |
| `Shell/DailyStreakPopup.cs:347` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakCurrentValue()` | `S_CurValue` = **37.9** | A2 |
| `Shell/DailyStreakPopup.cs:382` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakInfoText()` | `S_Info` = **1825.1** | A2 |
| `Shell/DailyStreakPopup.cs:395` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.MoreRewardsInText()` | `S_TimerNext` = **361.7** | A2 |
| `Shell/DailyStreakPopup.cs:401` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakTimerText()` | `S_TimerText` = **361.7** | A2 |
| `Shell/DailyStreakPopup.cs:479` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakBrokenText()` | `F_Broken` = **1825.1** | A2 |
| `Shell/DailyStreakPopup.cs:481` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakLostText()` | `F_Lost` = **1825.1** | A2 |
| `Shell/DailyStreakPopup.cs:483` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.StreakInfoText()` | `F_Info` = **1825.1** | A2 |
| `Shell/DailyStreakPopup.cs:486` | Text | 7 | **无** · **不生效** | 【变量】`DailyData.ResetStreakText()` | `F_ResetText` = **431.2** | A2 |
| `Shell/DeckSelectionPopup.cs:413` | Text | 7 | **无** · **不生效** | 【词条】键是变量 | `new PxRect(Ins2L, HdrT, Ins2R, HdrB)` = **534.1** | A2 |
| `Shell/DeckSelectionPopup.cs:429` | Text | 7 | **无** · **不生效** | 【词条】`MenuDeck/Button/Random` | `rr` = **212.2**（估算占比 **0.64**） | A3 |
| `Shell/DeckSelectionPopup.cs:467` | Text | 8 | **无** · **不生效** | 【变量】`_empty` | `SvRect` = **1565.0** | A2 |
| `Shell/DeckSelectionPopup.cs:561` | Text | 7 | **无** · **不生效** | 【变量】`label` | `r` = 未量 | A2 |
| `Shell/DuelPopupWindow.cs:170` | TextBox | 8 | 有 `0f` · **不生效** | 【拼接】string.Format(MessageFormat, OpponentNam… | `MsgR` = **770.0** | A2 |
| `Shell/DuelPopupWindow.cs:219` | TextBox | 10 | 有 `12f` · 生效 | 【变量】`text` | `txR` = 未量 | **接上** |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:343` | Text | 7 | **无** · **不生效** | 【字面量】"Progression" | `RTileR` = **594.8**（估算占比 **0.42**） | B |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:345` | TextBox | 10 | 有 `18f` · 生效 | 【字面量】"Win battles to progress in the event an… | `RHelpR` = **594.8**（估算占比 **1.79**） | **接上** |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:379` | Text | 7 | **无** · **不生效** | 【字面量】"Collect" | `CollectTxR` = **280.9**（估算占比 **0.69**） | A3 |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:456` | Text | 7 | **无** · **不生效** | 【字面量】"1256" | `sr` = 未量 | C |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:475` | Text | 7 | **无** · **不生效** | 【字面量】"Victories: " | `vtR` = **254.1**（估算占比 **1.16**） | A1 |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:480` | Text | 7 | **无** · **不生效** | 【字面量】"751" | `ttR` = **214.0**（估算占比 **0.54**） | B |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:541` | Text | 7 | **无** · **不生效** | 【字面量】"Termina en: 23d 5h" | `txR` = **289.7**（估算占比 **1.18**） | A1 |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:553` | TextBox | 10 | 有 `18f` · 生效 | 【变量】`TxtScore` | `InsScoreR` = **547.3** | **接上** |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:559` | TextBox | 10 | 有 `18f` · 生效 | 【变量】`TxtEnergy` | `InsEnergyR` = **547.3** | **接上** |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:578` | Text | 7 | **无** · **不生效** | 【字面量】"Factions" | `FacTitleR` = **563.6**（估算占比 **0.32**） | B |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:593` | TextBox | 10 | 有 `18f` · 生效 | 【字面量】"Play with any of these factions to take… | `FactionInsR` = **605.2**（估算占比 **2.06**） | **接上** |
| `Shell/GenericOptionsPanel.cs:377` | TextBox | 10 | 有 `NameFontMin` · 生效 | 【变量】`_ctx.Title ?? ""` | `nameR` = 未量 | **接上** |
| `Shell/GenericOptionsPanel.cs:390` | TextBox | 10 | 有 `BtnTextFontMin` · 生效 | 【字面量·经常量】`TemplateText` | `tTxtR` = 未量 | **接上** |
| `Shell/GenericOptionsPanel.cs:443` | TextBox | 10 | 有 `BtnTextFontMin` · 生效 | 【变量】`arr[i].Text ?? ""` | `txR` = 未量 | **接上** |
| `Shell/InboxWindow.cs:277` | Text | 10 | 有 `TitleAutoMin` · 生效 | 【变量】`DailyData.InboxTitle()` | `Title` = **250.0** | **接上** |
| `Shell/InboxWindow.cs:306` | Text | 10 | 有 `MdTitleAutoMin` · 生效 | 【变量】`DailyData.InboxMessageDisplayTitle…` | `MdTitle` = **959.8** | **接上** |
| `Shell/InboxWindow.cs:317` | Text | 10 | 有 `NoNewsAutoMin` · 生效 | 【变量】`DailyData.InboxNoNewsText()` | `NoNews` = **1420.7** | **接上** |
| `Shell/ItemDrawer.cs:999` | Text | 8 | **无** · **不生效** | 【变量】`s` | `row` = 未量 | A2 |
| `Shell/ItemDrawer.cs:1262` | Text | 8 | **无** · **不生效** | 【变量】`s` | `r` = 未量 | A2 |
| `Shell/LeaderboardRow.cs:173` | Text | 11 | 有 `RankMin` · 生效 | 【拼接】d.Rank > 0 ? d.Rank.ToString() : "" | `Abs(r, RankR)` = 未量 | **接上** |
| `Shell/LeaderboardRow.cs:230` | Text | 11 | 有 `NameMin` · 生效 | 【变量】`d.Name` | `nameAbs` = 未量 | **接上** |
| `Shell/LeaderboardRow.cs:242` | Text | 11 | 有 `GuildMin` · 生效 | 【变量】`d.Guild` | `guildAbs` = 未量 | **接上** |
| `Shell/LeaderboardRow.cs:257` | Text | 11 | 有 `PointsMin` · 生效 | 【变量】`d.Points ?? ""` | `pointsAbs` = 未量 | **接上** |
| `Shell/LeaderboardWindow.cs:385` | Text | 11 | 有 `18f` · 生效 | 【字面量】"TOP PLAYERS" | `TitleR` = **632.9**（估算占比 **0.48**） | **接上** |
| `Shell/LeaderboardWindow.cs:609` | Text | 11 | 有 `18f` · 生效 | 【字面量】"TOP PLAYERS" | `EmbTitleR` = **632.9**（估算占比 **0.48**） | **接上** |
| `Shell/LeaderboardWindow.cs:732` | Text | 11 | 有 `10f` · **不生效** | 【字面量】"Last season" | `txR` = 未量 | C |
| `Shell/LeaderboardWindow.cs:743` | Text | 11 | 有 `18f` · **不生效** | 【字面量】"Last season" | `textR` = 未量 | C |
| `Shell/LiveOpsEventWindow.cs:527` | Text | 7 | **无** · **不生效** | 【变量】`d.Name` | `new PxRect(DnL, DnT, DnR, DnB)` = **522.0** | A2 |
| `Shell/LiveOpsEventWindow.cs:529` | Text | 7 | **无** · **不生效** | 【变量】`wl != null ? wl.Name : ""` | `new PxRect(DwL, DwT, DwR, DwB)` = **522.0** | A2 |
| `Shell/LiveOpsEventWindow.cs:536` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【词条】`MenuDeck/HUD/NoWarlordText` | `new PxRect(NoDeckL, NoDeckT, NoDeckR…` = **685.7**（估算占比 **2.49**） | **接上** |
| `Shell/LiveOpsEventWindow.cs:571` | Text | 7 | **无** · **不生效** | 【词条】`MainMenu/Ranked/GoToCreateDeck` | `new PxRect(CreateTxL, CreateTxT, Cre…` = **317.7**（估算占比 **0.95**） | A3 |
| `Shell/LiveOpsEventWindow.cs:589` | Text | 7 | **无** · **不生效** | 【变量】`DeckCountText()` | `new PxRect(CntL, CntT, CntR, CntB)` = **88.8** | A2 |
| `Shell/LiveOpsEventWindow.cs:628` | Text | 7 | **无** · **不生效** | 【字面量】"Factions" | `new PxRect(FacTitleL, FacTitleT, Fac…` = **522.0**（估算占比 **0.35**） | B |
| `Shell/LiveOpsEventWindow.cs:901` | Text | 7 | **无** · **不生效** | 【字面量】"Battle!" | `new PxRect(BattleTxL, BattleTxT, Bat…` = **408.8**（估算占比 **0.64**） | A3 |
| `Shell/MatchLogRow.cs:164` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`s` | `r` = 未量 | **接上** |
| `Shell/MenuWindowBase.cs:340` | TextBox | 10 | 有 `autoMinPx` · **不生效** | 【变量】`text` | `r` = 未量 | A2 |
| `Shell/MenuWindowBase.cs:746` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`s.TitleText` | `s.TitleRect` = 未量 | **接上** |
| `Shell/MissionRerollPopup.cs:334` | TextBox | 10 | 有 `4f` · 生效 | 【字面量·经常量】`TxtMessage` | `MsgR` = **770.0**（估算占比 **1.12**） | **接上** |
| `Shell/MissionRerollPopup.cs:371` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`label` | `tx` = 未量 | **接上** |
| `Shell/MissionRerollPopup.cs:413` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`PricePlaceholder` | `PriceTextR` = **0（原版 CSF 锚点）** | C |
| `Shell/OfferContainer.cs:1369` | Text | 8 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`text` | `r` = 未量 | **接上** |
| `Shell/PlayerProfileWindow.cs:327` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`t.Label` | `labRect` = 未量 | **接上** |
| `Shell/PlayerProfileWindow.cs:656` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`text` | `r` = 未量 | **接上** |
| `Shell/PlayerProfileWindow.cs:775` | Text | 9 | 有 `23f` · 生效 | 【变量】`string.Format(Loc.T("MainMenu/Soci…` | `new PxRect(cx - 700f, cy - 70f, cx + 700f,…` = 未量 | **接上** |
| `Shell/PlayerProfileWindow.cs:780` | Text | 9 | 有 `18f` · 生效 | 【词条】`MainMenu/Social/ProfileOfflineNote` | `new PxRect(cx - 700f, cy + 20f, cx + 700f,…` = 未量 | **接上** |
| `Shell/PopUpGameWindow.cs:396` | Text | 10 | 有 `MsgAutoMinPx` · **不生效** | 【变量】`Term(_msgKey)` | `msgR` = 未量 | A2 |
| `Shell/PopUpGameWindow.cs:467` | Text | 10 | 有 `BtnAutoMinPx` · **不生效** | 【变量】`Term(key)` | `tr` = 未量 | A2 |
| `Shell/PurchasePremiumWindow.cs:479` | TextBox | 10 | 有 `TitleMin` · 生效 | 【字面量·经常量】`TxtTitle` | `Abs(TitleR)` = 未量 | **接上** |
| `Shell/PurchasePremiumWindow.cs:484` | TextBox | 10 | 有 `SubTitleMin` · 生效 | 【字面量·经常量】`TxtSubTitle` | `Abs(SubTitleR)` = 未量 | **接上** |
| `Shell/PurchasePremiumWindow.cs:669` | TextBox | 10 | 有 `InfoTitleMin` · 生效 | 【字面量·经常量】`TxtInfoTitle` | `Abs(DescTitleR)` = 未量 | **接上** |
| `Shell/PurchasePremiumWindow.cs:672` | TextBox | 10 | 有 `InfoBodyMin` · 生效 | 【变量】`TxtInfoBody` | `Abs(DescBodyR)` = 未量 | **接上** |
| `Shell/PurchasePremiumWindow.cs:686` | TextBox | 10 | 有 `PriceBtnTxtMin` · **不生效** | 【字面量】"" | `Abs(PriceBtnTxtR)` = 未量 | A2 |
| `Shell/PurchasePremiumWindow.cs:694` | TextBox | 10 | 有 `PriceTextMin` · 生效 | 【字面量·经常量】`TxtPrice` | `Abs(PriceTextR)` = 未量 | **接上** |
| `Shell/PurchasePremiumWindow.cs:707` | TextBox | 10 | 有 `PurchasedMin` · 生效 | 【字面量·经常量】`TxtPurchased` | `Abs(PurchasedR)` = 未量 | **接上** |
| `Shell/PurchasePremiumWindow.cs:735` | TextBox | 10 | 有 `0f` · **不生效** | 【变量】`nm` | `Sub(Abs(ContNameR))` = 未量 | A2 |
| `Shell/PurchasePremiumWindow.cs:754` | TextBox | 10 | 有 `PremiumMin` · 生效 | 【字面量·经常量】`TxtPremium` | `Sub(Abs(PremTextR))` = 未量 | **接上** |
| `Shell/RankedDivisionInfo.cs:88` | Text | 11 | 有 `10f` · 生效 | 【字面量】"" | `DivisionTextR` = **355.4** | **接上** |
| `Shell/RankedEventWindow.cs:86` | Text | 7 | **无** · **不生效** | 【字面量】"Rank" | `new PxRect(RankTitleL, RankTitleT, R…` = **522.0**（估算占比 **0.22**） | B |
| `Shell/RankedEventWindow.cs:100` | Text | 7 | **无** · **不生效** | 【字面量】"Leaderboard" | `new PxRect(LeaderTxL, LeaderTxT, Lea…` = **324.6**（估算占比 **0.61**） | A3 |
| `Shell/RankedEventWindow.cs:120` | Text | 7 | **无** · **不生效** | 【字面量】"Unranked" | `new PxRect(TgUnrankedL, TgUnrankedT,…` = **256.4**（估算占比 **0.70**） | A3 |
| `Shell/RankedEventWindow.cs:124` | Text | 7 | **无** · **不生效** | 【字面量】"Ranked" | `new PxRect(TgRankedL, TgRankedT, TgR…` = **256.4**（估算占比 **0.53**） | B |
| `Shell/RankedRewardEventWindow.cs:390` | TextBox | 10 | 有 `TitleMin` · 生效 | 【字面量·经常量】`TxtTitle` | `TitleR` = **1054.2**（估算占比 **0.42**） | **接上** |
| `Shell/RankedRewardEventWindow.cs:392` | TextBox | 10 | 有 `DescMin` · 生效 | 【字面量·经常量】`TxtDesc` | `DescR` = **1096.6**（估算占比 **1.40**） | **接上** |
| `Shell/RankedRewardEventWindow.cs:402` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`TxtTimer` | `TimerTextR` = **0（原版 CSF 锚点）** | C |
| `Shell/RankedRewardEventWindow.cs:410` | TextBox | 10 | 有 `BonusMin` · 生效 | 【字面量·经常量】`TxtBonus` | `BonusR` = **1054.2**（估算占比 **0.61**） | **接上** |
| `Shell/RankedRewardEventWindow.cs:570` | TextBox | 10 | 有 `CardNameMin` · 生效 | 【变量】`armyName` | `txR` = 未量 | **接上** |
| `Shell/ReferralPopupWindow.cs:411` | TextBox | 10 | 有 `RefTitleMin` · 生效 | 【字面量·经常量】`TxtRefTitle` | `RefTitleR` = **871.1**（估算占比 **0.48**） | **接上** |
| `Shell/ReferralPopupWindow.cs:416` | TextBox | 10 | 有 `InLabelMin` · 生效 | 【字面量·经常量】`TxtInLabel` | `InLabelR` = **802.4**（估算占比 **0.76**） | **接上** |
| `Shell/ReferralPopupWindow.cs:422` | TextBox | 10 | 有 `ErrorMin` · 生效 | 【字面量·经常量】`TxtError` | `ErrorR` = **408.6**（估算占比 **0.41**） | **接上** |
| `Shell/ReferralPopupWindow.cs:427` | TextBox | 10 | 有 `RefLabelMin` · 生效 | 【字面量·经常量】`TxtRefLabel` | `RefLabelR` = **211.1**（估算占比 **1.49**） | **接上** |
| `Shell/ReferralPopupWindow.cs:429` | TextBox | 10 | 有 `NameMin` · 生效 | 【字面量·经常量】`TxtName` | `RefNameR` = **155.4**（估算占比 **1.35**） | **接上** |
| `Shell/ReferralPopupWindow.cs:443` | TextBox | 10 | 有 `TitleMin` · 生效 | 【字面量·经常量】`TxtTitle` | `TitleR` = **871.1**（估算占比 **0.55**） | **接上** |
| `Shell/ReferralPopupWindow.cs:460` | TextBox | 10 | 有 `DescrMin` · 生效 | 【变量】`TxtDescr` | `DescrR` = **871.1** | **接上** |
| `Shell/ReferralPopupWindow.cs:465` | TextBox | 10 | 有 `CntNumMin` · 生效 | 【字面量·经常量】`TxtCounter` | `CntNumR` = **871.1**（估算占比 **0.62**） | **接上** |
| `Shell/ReferralPopupWindow.cs:501` | TextBox | 10 | 有 `PlaceholderMin` · 生效 | 【字面量·经常量】`TxtPlaceholder` | `TextAreaR` = **782.4**（估算占比 **0.03**） | **接上** |
| `Shell/ReferralPopupWindow.cs:507` | TextBox | 10 | 有 `InputTextMin` · 生效 | 【字面量·经常量】`TxtInputEmpty` | `TextAreaR` = **782.4**（估算占比 **0.03**） | **接上** |
| `Shell/ReferralPopupWindow.cs:528` | TextBox | 10 | 有 `BtnTextMin` · 生效 | 【字面量·经常量】`TxtBtn` | `BtnTextR` = **167.8**（估算占比 **0.86**） | **接上** |
| `Shell/SearchingMatchPopup.cs:238` | Text | 11 | 有 `4f` · 生效 | 【字面量】"" | `new PxRect(MsgL, MsgT, MsgR, MsgB)` = **700.0** | **接上** |
| `Shell/SearchingMatchPopup.cs:248` | Text | 7 | **无** · **不生效** | 【字面量】"Cancel" | `new PxRect(BtnTxL, BtnTxT, BtnTxR, B…` = **452.3**（估算占比 **0.30**） | B |
| `Shell/SearchingOpponentWindow.cs:198` | Text | 7 | **无** · **不生效** | 【变量】`string.IsNullOrEmpty(playerName) ?…` | `_foeNameRect` = 未量 | A2 |
| `Shell/SearchingOpponentWindow.cs:289` | Text | 7 | **无** · **不生效** | 【字面量】"Searching opponent" | `new PxRect(TitleL, TitleT, TitleR, T…` = **437.8**（估算占比 **0.74**） | A3 |
| `Shell/SearchingOpponentWindow.cs:295` | Text | 11 | 有 `4f` · 生效 | 【字面量】"" | `new PxRect(HintL, HintT, HintR, Hint…` = **700.0** | **接上** |
| `Shell/SearchingOpponentWindow.cs:322` | Text | 7 | **无** · **不生效** | 【字面量】"Cancel" | `new PxRect(CxlTxL, CxlTxT, CxlTxR, C…` = **324.0**（估算占比 **0.35**） | B |
| `Shell/SearchingOpponentWindow.cs:346` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`PlaceholderName` | `nameR` = 未量 | C |
| `Shell/SettingsWindow.cs:3214` | Text | 7 | **无** · **不生效** | 【变量】`s0` | `r` = 未量 | A2 |
| `Shell/SettingsWindow.cs:3571` | Text | 7 | **无** · **不生效** | 【变量】`f.Show()` | `new PxRect(r.x1 + pad, r.y1, r.x2 - pad, r…` = 未量 | A2 |
| `Shell/SkirmishEventWindow.cs:96` | Text | 7 | **无** · **不生效** | 【字面量】"Progression" | `new PxRect(RTileL, RTileT, RTileR, R…` = **522.0**（估算占比 **0.48**） | B |
| `Shell/SkirmishEventWindow.cs:98` | TextBox | 6 | **无** · **不生效** | 【字面量】"Win battles to progress in the event an… | `new PxRect(RHelpL, RHelpT, RHelpR, R…` = **592.8**（估算占比 **1.79**） | A1 |
| `Shell/SkirmishEventWindow.cs:106` | Text | 7 | **无** · **不生效** | 【字面量】"Victories: " | `new PxRect(VTitleL, VTitleT, VTitleR…` = **236.6**（估算占比 **1.12**） | A1 |
| `Shell/SkirmishEventWindow.cs:115` | Text | 7 | **无** · **不生效** | 【字面量】"0" | `new PxRect(VCountL, VCountT, VCountR…` = **180.8**（估算占比 **0.13**） | B |
| `Shell/SkirmishEventWindow.cs:205` | Text | 7 | **无** · **不生效** | 【字面量】"The deck has banned cards" | `new PxRect(BannedL, BannedT, BannedR…` = **678.3**（估算占比 **0.92**） | A3 |
| `Shell/SocialWindow.cs:446` | TextBox | 10 | 有 `autoMinPx` · **不生效** | 【变量】`text` | `r` = 未量 | A2 |
| `Shell/TrophyInfoPopup.cs:345` | TextBox | 10 | 有 `3f` · 生效 | 【字面量】"" | `TitleR` = **516.7** | **接上** |
| `Shell/TrophyInfoPopup.cs:349` | TextBox | 10 | 有 `15f` · 生效 | 【字面量】"" | `DescR` = **501.2** | **接上** |
| `Shell/TrophyInfoPopup.cs:376` | TextBox | 10 | 有 `12f` · 生效 | 【字面量】"0/0" | `CounterR` = **300.6**（估算占比 **0.17**） | **接上** |
| `Shell/TrophyInfoPopup.cs:412` | TextBox | 10 | 有 `3f` · 生效 | 【变量】`NextTierText` | `NextTierR` = **501.0** | **接上** |
| `Shell/TrophyInfoPopup.cs:441` | TextBox | 10 | 有 `29f` · 生效 | 【变量】`FeatureLabel` | `labelR` = **418.5** | **接上** |
| `Shell/TutorialModePopup.cs:481` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`StageTitle(StageIndex)` | `new PxRect(ITiL, ITiT, ITiR, ITiB)` = **503.0** | **接上** |
| `Shell/TutorialModePopup.cs:484` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`SubOf(StageIndex)` | `new PxRect(ISuL, ISuT, ISuR, ISuB)` = **504.1** | **接上** |
| `Shell/TutorialModePopup.cs:487` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`WarlordLine(StageIndex)` | `new PxRect(IWlL, IWlT, IWlR, IWlB)` = **503.0** | **接上** |
| `Shell/TutorialModePopup.cs:491` | Text | 8 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`DescOf(StageIndex)` | `new PxRect(IDsL, IDsT, IDsR, IDsB)` = **521.9** | **接上** |
| `Shell/TutorialModePopup.cs:603` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`StageTitle(i)` | `InCell(cell, ItemTi)` = 未量 | **接上** |
| `Shell/TutorialModePopup.cs:605` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【变量】`SubOf(i)` | `InCell(cell, ItemSu)` = 未量 | **接上** |
| `Shell/TutorialModePopup.cs:607` | Text | 7 | **无** · **不生效** · **调用后有 `SetAutoFitBox`** | 【字面量·经常量】`ItemDoneText` | `InCell(cell, ItemDone)` = 未量 | **接上** |
| `Shell/TutorialModePopup.cs:655` | Text | 7 | **无** · **不生效** | 【拼接】"Completed: " + CompletedCount() + "/" +… | `new PxRect(CompL, CompT, CompR, Comp…` = **658.3** | A2 |
| `Shell/TutorialModePopup.cs:676` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`PlayText` | `new PxRect(PlayTxL, PlayTxT, PlayTxR…` = **408.8**（估算占比 **1.18**） | A1 |
| `Shell/WindowsManager.cs:397` | Text | 11 | 有 `autoMinPx` · **不生效** | 【变量】`s` | `r` = 未量 | A2 |

### 1·2 `Editor/`（自检夹具，**不是产品代码**）—— 15 处

（列进来只为「全树普查」这条口径完整；**下一波切块派活时建议整块跳过**，它们不进游戏画面。）

| 文件:行号 | 调用 | 参数个数 | 有没有 `autoMinPx` | 文案来源 | 该框的宽度从哪来 | 本普查判定 |
|---|---|---|---|---|---|---|
| `Editor/MainMenuScene.cs:10974` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`L40` | `a781Cell` = **600.0**（估算占比 **1.00**） | — |
| `Editor/MainMenuScene.cs:10975` | TextBox | 8 | 有 `0f` · **不生效** | 【字面量·经常量】`L40` | `a781Cell` = **600.0**（估算占比 **1.00**） | — |
| `Editor/MainMenuScene.cs:10977` | TextBox | 8 | 有 `12f` · 生效 | 【字面量·经常量】`L40` | `a781Cell` = **600.0**（估算占比 **1.00**） | **接上** |
| `Editor/MainMenuScene.cs:11032` | Text | 12 | 有 `0f` · **不生效** | 【字面量·经常量】`L40` | `a781Cell` = **600.0**（估算占比 **1.00**） | — |
| `Editor/MainMenuScene.cs:11039` | TextBox | 11 | 有 `0f` · **不生效** | 【字面量·经常量】`L40` | `a781Cell` = **600.0**（估算占比 **1.00**） | — |
| `Editor/MainMenuScene.cs:11044` | Text | 7 | **无** · **不生效** | 【字面量·经常量】`L40` | `a781Cell` = **600.0**（估算占比 **1.00**） | — |
| `Editor/SettingsScene.cs:3564` | Text | 7 | **无** · **不生效** | 【字面量】"p1" | `new PxRect(0f, 0f, 300f, 60f)` = **300.0**（估算占比 **0.18**） | — |
| `Editor/SettingsScene.cs:3571` | Text | 7 | **无** · **不生效** | 【字面量】"p2" | `new PxRect(0f, 0f, 300f, 60f)` = **300.0**（估算占比 **0.17**） | — |
| `Editor/SettingsScene.cs:3593` | Text | 10 | 有 `10f` · 生效 | 【字面量】"pMax" | `new PxRect(0f, 0f, 300f, 60f)` = **300.0**（估算占比 **0.28**） | **接上** |
| `Editor/SettingsScene.cs:3606` | Text | 9 | 有 `10f` · 生效 | 【字面量】"pDef" | `new PxRect(0f, 0f, 300f, 60f)` = **300.0**（估算占比 **0.28**） | **接上** |
| `Editor/SettingsScene.cs:3622` | TextBox | 10 | 有 `10f` · 生效 | 【字面量】"pBase" | `new PxRect(0f, 0f, 300f, 60f)` = **300.0**（估算占比 **0.34**） | **接上** |
| `Editor/ShellScene.cs:1616` | Text | 7 | **无** · **不生效** | 【字面量】"WWWW WWWW WWWW WWWW WWWW" | `live` = **120.0**（估算占比 **3.00**） | — |
| `Editor/ShellScene.cs:1659` | Text | 7 | **无** · **不生效** | 【字面量】"WWWW WWWW WWWW WWWW WWWW" | `r8live` = **120.0**（估算占比 **3.00**） | — |
| `Editor/ShellScene.cs:1768` | Text | 7 | **无** · **不生效** | 【字面量】"WWWW WWWW WWWW WWWW WWWW" | `tFrame` = **120.0**（估算占比 **3.00**） | — |
| `Editor/ShopScene.cs:642` | Text | 7 | **无** · **不生效** | 【字面量】"5d 20h 15m" | `r` = **221.6** | — |

---

## §二 分档（**这一波最值钱的产出**）

**只对「确定没接 autosize」的 98 处分档**（生产 192 − 已接 94 = 98 处，涉及 **34 个文件**）。

### 判档规矩（现写在明处）

| 档 | 判据 |
|---|---|
| **A1** | 文案**算得出**、且**估算宽度 > 框宽**（算式 = `半个字宽 × 半宽字位数`；⛔ 是**估算**、不是实测 —— 见下面那段误差说明） |
| **A2** | 文案**会随语种 / 数据变**（词条表 / 拼接 / 变量 / 运行时空串后填），**或**框宽未量 ⇒ **排除不了溢出** |
| **A3** | 文案是**静态字面量**、框宽算得出，但**余量不足 40%**（占比 ≥ 0.6） |
| **B** | 文案是**静态字面量**、且**估算占比 < 0.6**（有算式证明余量 ≥ 40%） |
| **C** | **判不了**：框宽查不到（或框宽 = 0）、或文案宽度算不出 —— 每条都写了「还差什么」 |

> ⚠️ **估算算式**：`估算宽 = 半宽字位数 × 字号 ÷ 2`（半宽字位 = 本仓现成的尺子 `SearchingMatchPopup.HintLineWidth` 那一套语义：
> 汉字/全角 = 2 位、拉丁/数字/半角 = 1 位）。**汉字档准**（一个汉字 = 1 em = 字号 px）；**纯拉丁档偏大约 20–30%**
> （拉丁小写平均推进 ≈ 0.4 em，而算式给它算了 0.5 em）⇒ **A1 里纯拉丁那几条请按「可能只是边缘」看**。
> 🔴 **本报告没有一条写「一定溢出」** —— 全都是「按这个算式算出来放不下」，**实测要另跑**（量法见 §三）。

### 二·a **A1 档（必须先修）—— 按算式过界** ：6 处

> 🔴 **`TextBox` 那条的「占比」含义不同**（本档只有 `Shell/SkirmishEventWindow.cs:98` 是 `TextBox`）：
> `TextBox` **无条件折行**（`SetWrapWidth(r.W)`），所以「占比 1.79」= **要折成 2 行**；
> 那格的框是 **592.8 × 101.0**、字号 `38f` ⇒ 一行 ≈ `38 × 1.437`（本仓记的 Noto 行高 = **1.437 em**）≈ **54.6 px**，
> 两行 ≈ **109 px > 101** ⇒ **纵向差约 8 px**（估算、边缘）。**其余 5 条都是不折行的 `Text` ⇒ 横向溢出**。

| 文件:行号 | 框宽 | 估算文案宽 | 占比 | 字号 | 文案 | 算式 / 出处 |
|---|---|---|---|---|---|---|
| `Shell/SkirmishEventWindow.cs:98` | **592.8** | 1064 | **1.79** | `38f` | Win battles to progress in the event and… | 56 个半宽字位 × `38f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/CardDetailPopup.cs:661` | **418.5** | 560 | **1.34** | `40f` | This will consume a wildcard | 28 个半宽字位 × `40f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/TutorialModePopup.cs:676` | **408.8** | 483 | **1.18** | `74.25f` | Play Tutorial | 13 个半宽字位 × `74.25f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:541` | **289.7** | 342 | **1.18** | `38f` | Termina en: 23d 5h | 18 个半宽字位 × `38f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:475` | **254.1** | 294 | **1.16** | `53.5f` | Victories: | 11 个半宽字位 × `53.5f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/SkirmishEventWindow.cs:106` | **236.6** | 264 | **1.12** | `48f` | Victories: | 11 个半宽字位 × `48f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |

### 二·b **A2 档（文案会变 ⇒ 排除不了）** ：59 处 —— 文件归属

- **Shell/DailyRewardPopup.cs** —— 11 处（`265`、`284`、`319`、`342`、`383`、`387`、`390`、`451`、`455`、`482`、`497`）
- **Shell/DailyStreakPopup.cs** —— 9 处（`335`、`347`、`382`、`395`、`401`、`479`、`481`、`483`、`486`）
- **Shell/CardDetailPopup.cs** —— 6 处（`679`、`682`、`687`、`730`、`738`、`1007`）
- **Shell/DeckSelectionPopup.cs** —— 3 处（`413`、`467`、`561`）
- **Shell/LiveOpsEventWindow.cs** —— 3 处（`527`、`529`、`589`）
- **Battle/CardDisplayWindow.cs** —— 2 处（`289`、`305`）
- **Core/CostCurveDrawer.cs** —— 2 处（`86`、`109`）
- **Shell/CampaignRewardWindow.cs** —— 2 处（`427`、`431`）
- **Shell/ChatPanel.cs** —— 2 处（`148`、`722`）
- **Shell/ItemDrawer.cs** —— 2 处（`999`、`1262`）
- **Shell/PopUpGameWindow.cs** —— 2 处（`396`、`467`）
- **Shell/PurchasePremiumWindow.cs** —— 2 处（`686`、`735`）
- **Shell/SettingsWindow.cs** —— 2 处（`3214`、`3571`）
- **Shell/AllianceMemberOptionsPopup.cs** —— 1 处（`388`）
- **Shell/AllianceMemberTab.cs** —— 1 处（`816`）
- **Shell/AlliancePanelWindow.cs** —— 1 处（`345`）
- **Shell/BaseOfferPopup.cs** —— 1 处（`931`）
- **Shell/BoosterInfoPopup.cs** —— 1 处（`589`）
- **Shell/DuelPopupWindow.cs** —— 1 处（`170`）
- **Shell/MenuWindowBase.cs** —— 1 处（`340`）
- **Shell/SearchingOpponentWindow.cs** —— 1 处（`198`）
- **Shell/SocialWindow.cs** —— 1 处（`446`）
- **Shell/TutorialModePopup.cs** —— 1 处（`655`）
- **Shell/WindowsManager.cs** —— 1 处（`397`）

> 这一档的共性是：**框宽不跟着文案变**，而文案会变（换语种、换数据、或运行时 `SetText` 填进去）。
> 中文档今天看着正好、英文档就可能出框 —— 本仓那条现成的尺子（80 个半宽字位）也正是为这件事立的。
> **修法只有一条**：给这些调用点接上 autosize（传 `autoMinPx/autoMaxPx/autoBasePx`，或调用后补 `SetAutoFitBox`）。

### 二·c **A3 档（静态文案但余量不足 40%）** ：9 处

| 文件:行号 | 框宽 | 估算文案宽 | 占比 | 字号 | 文案 | 算式 / 出处 |
|---|---|---|---|---|---|---|
| `Shell/BoosterPackOpenWindow.cs:357` | **708.3** | 697 | **0.98** | `HintPx` | Tap on the cards to discover | 28 个半宽字位 × `HintPx` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/LiveOpsEventWindow.cs:571` | **317.7** | 302 | **0.95** | `55f` | 创建卡组 | 11 个半宽字位 × `55f` ÷ 2（文案含汉字 ⇒ 估算**准**） |
| `Shell/SkirmishEventWindow.cs:205` | **678.3** | 625 | **0.92** | `50f` | The deck has banned cards | 25 个半宽字位 × `50f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/SearchingOpponentWindow.cs:289` | **437.8** | 324 | **0.74** | `36f` | Searching opponent | 18 个半宽字位 × `36f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/RankedEventWindow.cs:120` | **256.4** | 180 | **0.70** | `45f` | Unranked | 8 个半宽字位 × `45f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:379` | **280.9** | 192 | **0.69** | `55f` | Collect | 7 个半宽字位 × `55f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/DeckSelectionPopup.cs:429` | **212.2** | 135 | **0.64** | `45f` | 随机 | 6 个半宽字位 × `45f` ÷ 2（文案含汉字 ⇒ 估算**准**） |
| `Shell/LiveOpsEventWindow.cs:901` | **408.8** | 260 | **0.64** | `74.25f` | Battle! | 7 个半宽字位 × `74.25f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/RankedEventWindow.cs:100` | **324.6** | 198 | **0.61** | `36f` | Leaderboard | 11 个半宽字位 × `36f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |

### 二·d **B 档（可放着：静态文案 + 算式证明余量 ≥ 40%）** ：12 处

| 文件:行号 | 框宽 | 估算文案宽 | 占比 | 字号 | 文案 | 算式 / 出处 |
|---|---|---|---|---|---|---|
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:480` | **214.0** | 116 | **0.54** | `77f` | 751 | 3 个半宽字位 × `77f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/RankedEventWindow.cs:124` | **256.4** | 135 | **0.53** | `45f` | Ranked | 6 个半宽字位 × `45f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/SkirmishEventWindow.cs:96` | **522.0** | 252 | **0.48** | `45.87f` | Progression | 11 个半宽字位 × `45.87f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:343` | **594.8** | 252 | **0.42** | `45.87f` | Progression | 11 个半宽字位 × `45.87f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/BoosterPackOpenWindow.cs:366` | **708.3** | 299 | **0.42** | `HintPx` | Tap to close | 12 个半宽字位 × `HintPx` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/SearchingOpponentWindow.cs:322` | **324.0** | 114 | **0.35** | `38f` | Cancel | 6 个半宽字位 × `38f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/LiveOpsEventWindow.cs:628` | **522.0** | 183 | **0.35** | `45.87f` | Factions | 8 个半宽字位 × `45.87f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:578` | **563.6** | 179 | **0.32** | `44.65f` | Factions | 8 个半宽字位 × `44.65f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/SearchingMatchPopup.cs:248` | **452.3** | 135 | **0.30** | `45f` | Cancel | 6 个半宽字位 × `45f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/RankedEventWindow.cs:86` | **522.0** | 115 | **0.22** | `57.7f` | Rank | 4 个半宽字位 × `57.7f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/CardDetailPopup.cs:651` | **122.9** | 20 | **0.16** | `40f` | 1 | 1 个半宽字位 × `40f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |
| `Shell/SkirmishEventWindow.cs:115` | **180.8** | 24 | **0.13** | `48f` | 0 | 1 个半宽字位 × `48f` ÷ 2（纯拉丁 ⇒ 估算**偏大**约 20–30%） |

### 二·e **C 档（判不了）** ：12 处 —— 每条的「还差什么」

| 文件:行号 | 调用 | 框宽出处 | 文案 | **还差什么 / 为什么判不了** |
|---|---|---|---|---|
| `Shell/AllianceEventScoreInfo.cs:137` | Text | `sr` | "1256" | **框宽未量** —— 该框由 `sr` 拼出，本脚本解析不出数值（差：人工查 `sr` 的定义） |
| `Shell/AllianceEventScorePanel.cs:133` | Text | `vtx` | Loc.T("MainMenu/RankedWindow/L… | **框宽未量** —— 该框由 `vtx` 拼出，本脚本解析不出数值（差：人工查 `vtx` 的定义） |
| `Shell/AllianceEventScorePanel.cs:166` | Text | `Off(JoinBtnTxR, x1, y1)` | Loc.T("MenuDeck/HUD/SearchFilt… | **框宽未量** —— 该框由 `Off(JoinBtnTxR, x1, y1)` 拼出，本脚本解析不出数值（差：人工查 `Off、JoinBtnTxR、x1、y1` 的定义） |
| `Shell/AllianceEventScorePanel.cs:176` | Text | `Off(NoLbTxR, x1, y1)` | Loc.T("MainMenu/RankedWindow/L… | **框宽未量** —— 该框由 `Off(NoLbTxR, x1, y1)` 拼出，本脚本解析不出数值（差：人工查 `Off、NoLbTxR、x1、y1` 的定义） |
| `Shell/BoosterPackOpenWindow.cs:492` | Text | `btR` | NewBadgeText | **框宽未量** —— 该框由 `btR` 拼出，本脚本解析不出数值（差：人工查 `btR` 的定义）；**字号未解析**（`27.7f * 0.01f * CardK`）；**文案宽度算不出**（来源 = const-str） |
| `Shell/CardDetailPopup.cs:885` | Text | `new PxRect(x + 30f, WcBgT, x + 71f, WcBgB)` | "99" | **框宽未量** —— 该框由 `new PxRect(x + 30f, WcBgT, x + 71f, WcBg…` 拼出，本脚本解析不出数值（差：人工查 `WcBgT、WcBgB` 的定义） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:456` | Text | `sr` | "1256" | **框宽未量** —— 该框由 `sr` 拼出，本脚本解析不出数值（差：人工查 `sr` 的定义） |
| `Shell/LeaderboardWindow.cs:732` | Text | `txR` | "Last season" | ⚠️ **它其实【传了】`autoMinPx`（`10f`）** —— 只是 `wrapPx`（= 框宽）未量 ⇒ 本脚本**确认不了它生效没有**（要人工核那个矩形）；**框宽未量** —— 该框由 `txR` 拼出，本脚本解析不出数值（差：人工查 `txR` 的定义） |
| `Shell/LeaderboardWindow.cs:743` | Text | `textR` | "Last season" | ⚠️ **它其实【传了】`autoMinPx`（`18f`）** —— 只是 `wrapPx`（= 框宽）未量 ⇒ 本脚本**确认不了它生效没有**（要人工核那个矩形）；**框宽未量** —— 该框由 `textR` 拼出，本脚本解析不出数值（差：人工查 `textR` 的定义） |
| `Shell/MissionRerollPopup.cs:413` | Text | `PriceTextR` | PricePlaceholder | **框宽就是 0**（原版 `ContentSizeFitter` 撑开的锚点，不是框）⇒ 没有「框」可比，判不了溢出，要另找「不该压到谁」的边界 |
| `Shell/RankedRewardEventWindow.cs:402` | Text | `TimerTextR` | TxtTimer | **框宽就是 0**（原版 `ContentSizeFitter` 撑开的锚点，不是框）⇒ 没有「框」可比，判不了溢出，要另找「不该压到谁」的边界 |
| `Shell/SearchingOpponentWindow.cs:346` | Text | `nameR` | PlaceholderName | **框宽未量** —— 该框由 `nameR` 拼出，本脚本解析不出数值（差：人工查 `nameR` 的定义） |

### 二·f **下一波怎么切块派活**（「一个文件一个写手」）

按文件归并（**A1 + A2 + A3 合起来才算「这一波要修的」**）：

- **Shell/DailyRewardPopup.cs** —— 11 处（`265`、`284`、`319`、`342`、`383`、`387`、`390`、`451`、`455`、`482`、`497`）
- **Shell/DailyStreakPopup.cs** —— 9 处（`335`、`347`、`382`、`395`、`401`、`479`、`481`、`483`、`486`）
- **Shell/CardDetailPopup.cs** —— 7 处（`661`、`679`、`682`、`687`、`730`、`738`、`1007`）
- **Shell/LiveOpsEventWindow.cs** —— 5 处（`527`、`529`、`571`、`589`、`901`）
- **Shell/DeckSelectionPopup.cs** —— 4 处（`413`、`429`、`467`、`561`）
- **Shell/EnergySinglePlayerOnlyEventWindow.cs** —— 3 处（`379`、`475`、`541`）
- **Shell/SkirmishEventWindow.cs** —— 3 处（`98`、`106`、`205`）
- **Battle/CardDisplayWindow.cs** —— 2 处（`289`、`305`）
- **Core/CostCurveDrawer.cs** —— 2 处（`86`、`109`）
- **Shell/CampaignRewardWindow.cs** —— 2 处（`427`、`431`）
- **Shell/ChatPanel.cs** —— 2 处（`148`、`722`）
- **Shell/ItemDrawer.cs** —— 2 处（`999`、`1262`）
- **Shell/PopUpGameWindow.cs** —— 2 处（`396`、`467`）
- **Shell/PurchasePremiumWindow.cs** —— 2 处（`686`、`735`）
- **Shell/RankedEventWindow.cs** —— 2 处（`100`、`120`）
- **Shell/SearchingOpponentWindow.cs** —— 2 处（`198`、`289`）
- **Shell/SettingsWindow.cs** —— 2 处（`3214`、`3571`）
- **Shell/TutorialModePopup.cs** —— 2 处（`655`、`676`）
- **Shell/AllianceMemberOptionsPopup.cs** —— 1 处（`388`）
- **Shell/AllianceMemberTab.cs** —— 1 处（`816`）
- **Shell/AlliancePanelWindow.cs** —— 1 处（`345`）
- **Shell/BaseOfferPopup.cs** —— 1 处（`931`）
- **Shell/BoosterInfoPopup.cs** —— 1 处（`589`）
- **Shell/BoosterPackOpenWindow.cs** —— 1 处（`357`）
- **Shell/DuelPopupWindow.cs** —— 1 处（`170`）
- **Shell/MenuWindowBase.cs** —— 1 处（`340`）
- **Shell/SocialWindow.cs** —— 1 处（`446`）
- **Shell/WindowsManager.cs** —— 1 处（`397`）

> 🔴 **派活前先看一眼这四个文件**：普查快照的同一时刻，`git status` 显示下面 4 个「没接 autosize」的文件**正被别的写手改**
> （`Shell/BoosterInfoPopup.cs` · `Shell/ChatPanel.cs` · `Shell/LeaderboardWindow.cs` · `Shell/SettingsWindow.cs`）——
> 它们的行号/内容**当场就是旧的**，派活前**必须重扫一遍**（`grep -n "MenuDraw\.Text\|MenuDraw\.TextBox" <文件>`）。
>
> ⚠️ **切块提醒**：
> 1. **A1/A3/B 三档的框宽**是本脚本从**常量表达式**解析出来的 —— 派活时**要让写手现读那个矩形**，⛔ 别抄本报告的数。
> 2. **A2 里有 7 个文件的整族文字走的是「包装层」**（见 §四·1）—— 那些文件的修法可能不是「改调用点」而是「给包装加形参」，**同一个文件别派两个写手**。
> 3. **`Shell/MenuWindowBase.cs` / `Shell/WindowsManager.cs` 是共用件**（全部子类都走它们）⇒ 谁动它们，自检要跑**全部受影响的宿主**。

---

## §三 量法：本仓现成能「量实绘宽度 / 框宽」的口（**全部现读，给符号名 + `文件:行`**）

| 口 | 位置（现读） | 量什么 | 能不能用来判溢出 |
|---|---|---|---|
| **`TmpRenderedRect(Transform t, out x1, out y1, out x2, out y2)`** | `Editor/CollectionScene.cs:293` | TMP 自己渲出来那块 `textBounds` 的**四角**，经 `LayoutSpace.PxX/PxY` 换成画布 px | ✅ **首选**（E11 的 `A1035` 用的就是它）· ⚠️ 空串时是 TMP 未定义值（哨兵 `4.29e9`） |
| **`LabelRenderedPx(Label lb)` → `Vector2(w,h)`** | `Editor/CollectionScene.cs:330` | 同上，只给宽高；点阵后端/没 TMP 时退回 `Label.WorldW*108` | ✅ 首选（宽高版） |
| **`MenuDraw.QuadRectPx(ImageQuad q)` → `PxRect`** / **`QuadRectPx(q, out …)`** | `Shell/MenuDraw.cs:690` / `:701` | 一颗 `ImageQuad` **建出来的顶点**在设计像素里的矩形 | ✅ **量框**（框是图/底板时）—— 它读的是**建顶点那一份**，不是缓存 |
| **`Label.WorldW` / `WorldH`** | `Battle/Label.cs:91` / `:93` | **字段缓存** `_tmpW/_tmpH` | 🔴 **⛔ 别用**：那份缓存**只由 `RefreshBounds()` 写**（= 被测实现自己），而 `SetFontSize`/`SetCharSpacing` 这一族**只重排 mesh、不刷缓存** ⇒ 实现与检测器共用一个口 = **自证** |
| **`SearchingMatchPopup.HintLineWidth(string)`** → `int` | `Shell/SearchingMatchPopup.cs:477` | 这句话占**多少个半宽字位**（纯函数、不读 `Loc.Current`） | ✅ **唯一能在「建之前」算的尺子**（§二 的估算就是它 + 字号）· ⚠️ 给的是**字位**不是 px |
| **`Label.HasMeasuredWidth()`** | `Battle/Label.cs:1295` | 宽度是不是**已量到真值**（没量到时 `WorldW` 是垃圾） | ✅ 当**前置守卫**（防「拿垃圾宽度当结论」） |
| **`Label.DumpSizes()`** | `Battle/Label.cs:983` | 一次打出 `tmp=` / `tmpW` / `fontSize` 等 | ✅ 诊断（CLAUDE.md §三「面板画出来了、字不在」那条点名要它） |
| **`Label.AutoSizing` / `FontPxNow` / `FontSizeMin/Max/Base`** | `Battle/Label.cs:458` / `:675` / `:682` `:683` `:695` | autosize **开没开**、**落到了多少 px** | ✅ **判「autosize 到底生效没有」的直接口**（比只看实参强——见 §〇0·1） |
| **`Label.Wrapping` / `WrappingMode`** | `Battle/Label.cs:423` / `:449` | 折行模式（原版 `m_TextWrappingMode` 原文） | ✅ 判「框宽到底管不管用」 |
| **`MenuDraw.PaddedRect(r, pad)`** | `Shell/MenuDraw.cs:421` | 框按 `m_RaycastPadding`/`ClipPad` 内缩后的矩形 | ✅ 框宽要按它算（⛔ 别拿裸矩形） |
| **`LayoutSpace.Px(px)` / `PxX` / `PxY` / `DesignPxW,H`** | `Core/LayoutSpace.cs:154` / `:184` / `:188` / `:151` | 画布 px ↔ 世界 | ✅ 量纲桥（108 px / 世界单位） |

🔴 **两条纪律**（照本仓既有口径，别违反）：
1. **⛔ 别用与被测实现同源的那个缓存口**（`Label.WorldW/H`）；**读建顶点/`textBounds` 的那两条可以**。
2. **`TMP` 在对象未激活时量不出尺寸**（`textBounds` 是垃圾、实测顶到 `4.29e9`）⇒ `ForceMeshUpdate()` 必须在 `SetActive(true)` **之后**；量不到时**报出来**，⛔ 别静默当 0。

---

## §四 顺手发现（⛔ 一个字没改）

### 四·1 🔴 **`MenuDraw.Text` 之外还有两层，本表的 192 处只是第一层**

| 层 | 位置（现读） | 形参里有没有 autosize | 说明 |
|---|---|---|---|
| `GameWindow.Text(parent, PxRect r, string s, Color, name, float fontPx, int q, float wrapPx = 0, float autoMinPx = 0, int align = 0, float autoMaxPx = 0, autoBasePx = 0)` | `Shell/WindowsManager.cs`（转调 `MenuDraw.Text`，现读 :~380–397） | ✅ **有**（透传） | 走它的调用点**没传 `autoMinPx`** 一样等于不接 |
| `MainMenuSubmenuWindow.Text(parent, string text, float x1, x2, y1, y2, int scale, Color, name, float fontPx = 0f)` | `Shell/MenuWindowBase.cs:284`（方法体 `:309-318` 直接 `Label.Create`） | ❌ **连形参都没有**（不调 `SetWrapWidth`、不调 `SetAutoFitBox`） | ⇒ 走这条路的文字**今天没有任何办法**接 autosize，**要先给这个包装加形参** |
| `MainMenuSubmenuWindow.TextBox(...)` | `Shell/MenuWindowBase.cs:325` | ✅ 有（转调 `MenuDraw.TextBox`） | —— |
| 各窗私有的 `Label Txt/LbTxt(...)`（直接 `Label.Create`） | `Shell/DeckInfoPopup.cs:1584` · `Shell/ImportDeckPopup.cs:288` / `:441` · `Shell/MainMenuRuntime.cs:114` · `Shell/PracticeModePopup.cs:1901` · `Shell/PromptPopup.cs:121` / `:249` · `Shell/TopBar.cs:494` · `Core/CardFeel.cs:1036` · `Core/Tooltip.cs:150` | ❌ **完全没有** | 这些是**另一族**「不接 autosize」——**不在本表 192 行里** |

> **口径说明**：简报把范围定在 `MenuDraw.Text`；上表这几族**我只报数、没有逐条普查**。
> 粗数（掩注释后按裸 `Text(` 计数、**含包装定义**，**仅供参考**）：`Shell/` 里走**包装层**的 `Text(...)` 调用点约 **154 处**
> （最多的几份：`ProfileTab` 28 · `SettingsWindow` 23 · `CollectionWindow` 17 · `AlliancesTab` 15 · `RankedTab` 8）；
> 直接 `Label.Create` 的落点 **16 处**（`Shell/` + `Core/`）。**这两族该另开一笔普查。**

### 四·2 ⚠️ **`MenuWindowBase.Text` 那条路今天「零 autosize 能力」**

`Shell/MenuWindowBase.cs:284` 的形参表里**没有** `wrapPx` / `autoMinPx`，方法体直接 `Label.Create` + `SetGlyphHeight`。
⇒ 走它的调用点（`ProfileTab` / `SettingsWindow` / `CollectionWindow` …）**不是「忘了传」，是「没地方传」**。
**要修得先动共用件**（给包装加形参）⇒ 影响全部子类，**这是下一波该单独立项的一件，不是「改几个调用点」**。

### 四·3 ℹ️ 覆盖不到的地方（如实说）
- **`TextBox` 的框宽我按 `r.W` 算** —— `MenuDraw.TextBox` 现读无条件是 `lb.SetWrapWidth(LayoutSpace.Px(r.W))`。框宽 = 0 时（本报告 2 处）**判不了**。
- **`hAlign` 我没查**：文字出框后往哪边溢（居中 / 左 / 右）由后面的 `MenuDraw.AlignLeft/Right` 决定，**本普查没逐处跟**。
- **`clip` 我没查**：有些调用点处在 `ViewportClip` 之下，字**会被裁掉而不是溢出去**（那是另一种缺陷，⛔ 不是「没问题」）。

---

## 汇总数字

| 项 | 数 | 怎么数的 |
|---|---|---|
| **总调用点**（`CardPresentation/` 全树） | **207** | 掩掉注释/字符串后，正则 `\bMenuDraw\s*\.\s*(Text|TextBox)\s*\(` 全树扫（210 个 `.cs`），再按括号配平切实参 |
| ├ `MenuDraw.Text` | **154** | 同上（生产 143 + `Editor/` 11） |
| └ `MenuDraw.TextBox` | **53** | 同上（生产 49 + `Editor/` 4） |
| **生产代码里** | **192** | 去掉 `Editor/` |
| 口径①：**传了** `autoMinPx` 的 | 80 / 192 | 数顶层实参个数（`Text` ≥9 / `TextBox` ≥7）+ 命名实参 |
| 口径②：**实参生效**的（wrap>0 ∧ autoMinPx>0 ∧ fontPx>autoMinPx） | **61** | 逐处把 `wrapPx`/`autoMinPx`/`fontPx`/`r` 的**常量表达式求值**后套判据 |
| 口径③：**调用后另调 `SetAutoFitBox`** 的 | **33** | 掩注释源码里找 `TGT.SetAutoFitBox(`，`TGT` = 该调用的赋值目标 |
| 口径④：**合并后确定接上** autosize 的 | **94** | ②或③ |
| 🔴 **确定没接 autosize 的** | **98** | 192 − 94，涉及 **34 个文件** |
| **A1 档** | **6** | 见 §二判档规矩 + §二·a 的算式 |
| **A2 档** | **59** | 文案会变 / 框宽未量 |
| **A3 档** | **9** | 占比 ≥ 0.6 |
| **B 档** | **12** | 占比 < 0.6 |
| **C 档** | **12** | 框宽 = 0 / 解析不出 |
| 涉及文件（没接的那些） | **34** | —— |

**怎么数的（可复现）**：五个脚本（都在仓外 `D:/tmp/p4work/`，**没有一行写进工程**）：
`p4_census.py`（切调用点 + 实参）→ `analyze.py`（常量/矩形求值：`float A = …, B = …` 链式声明、`var x = new PxRect(…)` 局部、
`X.W/.H/.x1/.x2` 成员、跨文件 `SomeClass.Const`、以及本仓三个矩形助手 `Off` / `InCell` / `CenterRect`）→
`final.py`（文案来源分类 + 条/常量字符串解析 + 估算宽）→ `postmutation.py`（调用后的 `Set*`）→ `tier.py`（档位）。
定位模式（复核用）：`grep -n "MenuDraw\.Text\|MenuDraw\.TextBox" <文件>`。**全部行号 = 我这一轮现读那一版**。

### 数据快照 md5（前 12 位，复核用 —— 行号漂了就按 `grep` 模式重定位）

| 文件 | md5 |
|---|---|
| `Battle/CardDisplayWindow.cs` | `30b1ad160d97` |
| `Core/CostCurveDrawer.cs` | `5d8133868cdf` |
| `Shell/AllianceEventScoreInfo.cs` | `936951089531` |
| `Shell/AllianceEventScorePanel.cs` | `a909504e775a` |
| `Shell/AllianceMemberOptionsPopup.cs` | `9ec2d99f90f8` |
| `Shell/AllianceMemberTab.cs` | `2d71883a833b` |
| `Shell/AlliancePanelWindow.cs` | `e40be2263e05` |
| `Shell/BaseOfferPopup.cs` | `4b22ae070b2c` |
| `Shell/BoosterInfoPopup.cs` | `aa0d983c0241` |
| `Shell/BoosterPackOpenWindow.cs` | `f2774ed3c8b6` |
| `Shell/CampaignRewardWindow.cs` | `9806f8668dd8` |
| `Shell/CardDetailPopup.cs` | `d34e30c409a9` |
| `Shell/ChatPanel.cs` | `f2c842eaac36` |
| `Shell/DailyRewardPopup.cs` | `36c11aa95a12` |
| `Shell/DailyStreakPopup.cs` | `007f65301a8c` |
| `Shell/DeckSelectionPopup.cs` | `6679a6371c39` |
| `Shell/DuelPopupWindow.cs` | `cdc42d78d9da` |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | `42ba83717a99` |
| `Shell/GenericOptionsPanel.cs` | `4216b5730956` |
| `Shell/InboxWindow.cs` | `d230832517e9` |
| `Shell/ItemDrawer.cs` | `851daaac912f` |
| `Shell/LeaderboardRow.cs` | `331a2b8c1d39` |
| `Shell/LeaderboardWindow.cs` | `d9b3a4387588` |
| `Shell/LiveOpsEventWindow.cs` | `d3752021469b` |
| `Shell/MatchLogRow.cs` | `774559bec6d6` |
| `Shell/MenuWindowBase.cs` | `ccdfb5bd0a1f` |
| `Shell/MissionRerollPopup.cs` | `3ada6ffacd22` |
| `Shell/OfferContainer.cs` | `3bfd636719a9` |
| `Shell/PlayerProfileWindow.cs` | `d92074570198` |
| `Shell/PopUpGameWindow.cs` | `2c5fada6dd78` |
| `Shell/PurchasePremiumWindow.cs` | `ce6d9836d465` |
| `Shell/RankedDivisionInfo.cs` | `f27b009c535c` |
| `Shell/RankedEventWindow.cs` | `fe12d251a303` |
| `Shell/RankedRewardEventWindow.cs` | `0cc4fd46cc80` |
| `Shell/ReferralPopupWindow.cs` | `45e56a60cf24` |
| `Shell/SearchingMatchPopup.cs` | `75ab9e2a31b1` |
| `Shell/SearchingOpponentWindow.cs` | `698a90072ff6` |
| `Shell/SettingsWindow.cs` | `7c31c85e6f26` |
| `Shell/SkirmishEventWindow.cs` | `3e50011d8a71` |
| `Shell/SocialWindow.cs` | `0a748c969e7a` |
| `Shell/TrophyInfoPopup.cs` | `415d7ab1af6c` |
| `Shell/TutorialModePopup.cs` | `8bc9d4bf2be4` |
| `Shell/WindowsManager.cs` | `1278e80d2f85` |

---

## 需要哪几条自检覆盖

**本件是纯只读普查，产品代码一行未动 ⇒ 一条自检都不用跑**（CLAUDE.md 铁律 12：纯文档 ⇒ 0 条）。
下一波真要动手时，按**改了哪些文件**定：`Shell/*` 的字号线 ⇒ `ShellScene.Run` + `MainMenuScene.Run`；
碰 `Battle/*` ⇒ `BattleScene.Run`；**碰共用件（`MenuDraw.cs` / `MenuWindowBase.cs` / `WindowsManager.cs`）⇒ 全套 12 条**。
