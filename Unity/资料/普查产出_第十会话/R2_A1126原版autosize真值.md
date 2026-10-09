# R2 · `A1126`·`A2` 59 处 + `A1179` 5 处 —— 原版 autoSize 真值普查

> 只读普查代理 R2 · 2026-10-19 · **一个源文件都没改**（本报告是本次唯一新增文件）。
> 红线：没跑 Unity · 没跑 typecheck · 没动 git · 没碰两张正本 · `d:/2/**` 只读。
>
> 🔴 **2026-10-09 就地订正（第十会话 · 执行代理 `P9` 现核 · 铁律 5）—— 本报告有【两处认错了原版件】**：
> ① **`§2` #44 / `§3` 注④ 的 `ItemDrawer:999`（`Price Display/text`）**：本报告按 `m_text == '300,00'` 全库扫出 **67 颗**，
>    **那不是这一颗** —— `:999` 在 `TextCentered` 里、只有 `SetConverted` / `SetEphemeral` 两个调用点，
>    原版件是**抽屉自己**的 `{Converted,Ephemeral} Drawer ▸ Price Display/text`（**六份抽屉 prefab 逐份现读** =
>    **`fs65 基准39 auto[13.46~65] 折行0`**，`max = 65`）。那 67 颗是**商店/礼包按钮**的同名节点
>    ⇒ ⛔ **别跨族抄 `max`**。（旁证：**我们 `Shell/ItemDrawer.cs` 文件头早就写着「字高 = `65 × k`」**。）
> ② **`§5·2` 的 `Army Name`「原版关着」不成立**：本报告引的 `Purchase Premium Window ▸ … ▸ Army Container ▸ Army Name`
>    （父件挂 `EverguildToggle,PuchasePremiumArmyContainer`）是那个**切换行**的标签、**另一个控件族**；
>    我们 `Shell/ItemDrawer.cs:1175` 在 `Wildcard()` 里（`WildcardDrawer` 那条路），原版只有**一颗**：
>    **`Wildcard Drawer ▸ Content ▸ Army`（`fs75 基准36 auto[12~75] 折行1`，六份抽屉全有）** ⇒ **它其实是【该接】**。
>    （旁证 = `d:/2/tools/decomp_full/WildcardDrawer__Draw.c:31-40` 把 `CardArmyToString` 写进 `+0x58` 那颗 TMP。）
> ③ 连带：**六个抽屉 prefab 里没有一颗 TMP 关着 auto** ⇒ 本报告里凡把 `ItemDrawer` 系判成「原版关着」的，**都值得回核**。
> 📄 现核全文 → `资料/普查产出_第十会话/P9_A1187A1188A1192.md` §2。

---

## §1 结论（先给数字）

**`A1126`·`A2` 59 处：该接 `22` / 不是缺口 `35` / 读不到 `2`（22+35+2 = 59 ✅）**

🔴 **最有价值的发现：59 处里 35 处「根本不用改」** —— 其中：
- **24 处原版那颗 TMP 本来就是 `m_enableAutoSizing = 0`** ⇒ 加了 autosize 反而是**主动制造偏离**（铁律 11 的反面）；
- **11 处我们这边其实【已经接上了】**（`P4` 判「不生效」是**它的静态求值器解析不出变量**，不是事实）——
  这 11 处**不能只按「有没有传 autoMinPx 字面量」判断**。

**`A1179` 5 处：确认原版全开 autosize**，四格真值见 §4。

---

## §2 逐处表（59 行 · 一行一处）

**读法**：`我们这处` = 现读的节点名（P4 的行号**已漂**，一律按**节点名 + 文案**认人）。
`原版那颗` 给 `<bundle> ▸ <节点路径>`。`auto` 列 = `min~max`（空 = 原版**没开**）。

| # | 我们这处（文件 + 节点名 / 文案） | 原版那颗（bundle ▸ 路径） | min | max | base | 折行 | 裁定 |
|---|---|---|---|---|---|---|---|
| 1 | `Battle/CardDisplayWindow.cs:289` `EnchanterText` | `scenes_battlearena1 ▸ Card Display…/EnchanterText` | 0.1 | 0.3 | 36 | 0 | ⛔ **不是缺口（已接）** |
| 2 | `Battle/CardDisplayWindow.cs:305` `EffectText` | 同上 `EffectText` | 0.1 | 0.3 | 36 | 1 | ⛔ **不是缺口（已接）** |
| 3 | `Core/CostCurveDrawer.cs:86` `Card Cost` | `menus ▸ Deck info Popup…/Deck Information cost drawer…/Card Cost` | — | — | 25 | 1 | ⛔ **不是缺口（原版关着）** |
| 4 | `Core/CostCurveDrawer.cs:109` `Cards in deck` | 同上 `Cards in deck` | — | — | 25 | 1 | ⛔ **不是缺口（原版关着）** |
| 5 | `Shell/AllianceMemberOptionsPopup.cs:388` `Button Text` | `menus ▸ Member Options Panel…/Button Text` | 10 | 35 | 12 | 0 | ⛔ **不是缺口（已接）** |
| 6 | `Shell/AllianceMemberTab.cs:816` `TextSoft`（2 调用点） | `menus ▸ Alliance Trophy Info Popup…/counter`（`title` 未定位，见 §5） | 12 | 35 | 36 | 1 | ⛔ **不是缺口（已接：autoMin=12 生效）** |
| 7 | `Shell/AlliancePanelWindow.cs:345` `Txt`（7 调用点） | `scenes_battlearena1 ▸ Alliance Panel…/{PlayerLabel,Name Text,TitleLabel,Title Text,Alliance Label,Alliance Name,NotInaAllianceText}` | 18 | 35/40 | 36 | 0 | ✅ **该接** |
| 8 | `Shell/BaseOfferPopup.cs:931` `Timer Text` | `menus ▸ Base Offer Popup ▸ Timer/Timer Text` | — | — | 30 | 1 | ⛔ **不是缺口（原版关着）** |
| 9 | `Shell/BoosterInfoPopup.cs:589` `Button Text`（价格） | `menus ▸ Booster Info Popup…/Price Display/text` | 13.46 | 40 | 39 | 0 | ✅ **该接** |
| 10 | `Shell/CampaignRewardWindow.cs:427` `Text Get Reward` | `menus ▸ Campaign Reward Window ▸ Text Get Reward` | 25 | 50 | 36 | 1 | ⛔ **不是缺口（已接）** |
| 11 | `Shell/CampaignRewardWindow.cs:431` `Text Preview Reward` | 同上 `Text Preview Reward` | 25 | 50 | 36 | 1 | ⛔ **不是缺口（已接）** |
| 12 | `Shell/CardDetailPopup.cs:679` `Upgrade Need` | `mainmenuwarpforge ▸ Upgrade Panel/Content/Upgrade/cards/quantity` | 10 | 40 | 12 | 0 | ✅ **该接** |
| 13 | `Shell/CardDetailPopup.cs:682` `Upgrade Cost` | 同上 `…/Upgrade/cost/quantity` | 10 | 40 | 12 | 0 | ✅ **该接** |
| 14 | `Shell/CardDetailPopup.cs:687` `Upgrade Explanation` | 同上 `…/Upgrade/Explanation/Text Explanation` | 10 | 40 | 36 | 0 | ✅ **该接** |
| 15 | `Shell/CardDetailPopup.cs:730` `Current Style`（`1 of 1`） | ⚠️ 原版 `Alternate Art Panel/Current Style` 是 **Image、无 TMP 子件** | — | — | — | — | ❓ **读不到（无对应件）** |
| 16 | `Shell/CardDetailPopup.cs:738` `Buy Original Card` | `mainmenuwarpforge ▸ Alternate Art Panel/Buy Original Card Button/Generic UI Button/…` | 12 | 54 | 39 | 0 | ✅ **该接**（见 §3 注①） |
| 17 | `Shell/CardDetailPopup.cs:1007`（`Title` 助手，本行在 :1021） | 见 §3 注② | 10 | 42 | 36 | 1 | ⛔ **不是缺口（已接）** |
| 18 | `Shell/ChatPanel.cs:148`（`Text` 助手） | 见 §3 注③ | — | — | — | — | ⛔ **不是缺口** |
| 19 | `Shell/ChatPanel.cs:722`（`TextBox` 助手，固定 `autoMin=0`） | `mainmenualwaysloaded ▸ ChatMessageRow ▸ {Sender,Time,Message}` | — | — | 18/22 | 1 | ⛔ **不是缺口（原版关着）** |
| 20 | `Shell/DailyRewardPopup.cs:265` `Day Title` | `menus ▸ Daily Reward Popup Entry…/Day Title` | — | — | 48 | 1 | ⛔ **不是缺口（原版关着）** |
| 21 | `Shell/DailyRewardPopup.cs:284` `Counter` | 同上 `…/Personal Progression/Image/Counter` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 22 | `Shell/DailyRewardPopup.cs:319` `Name` | 同上 `…/NormalReward/EverguildTextMeshPro`（`Premium Reward` 同值） | 18 | 45 | 36 | 1 | ✅ **该接** |
| 23 | `Shell/DailyRewardPopup.cs:342` `Claimed Tex` | 同上 `…/Gacha Reward Claimed/Claimed Tex` | 15 | 200 | 36 | 1 | ✅ **该接** |
| 24 | `Shell/DailyRewardPopup.cs:383` `Title`（Free Track） | `menus ▸ …/Tracks Side Bar/BG/Free Track/Title` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 25 | `Shell/DailyRewardPopup.cs:387` `Title`（Premium Track） | 同上 `…/Premium Track/Title` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 26 | `Shell/DailyRewardPopup.cs:390` `Price` | 同上 `…/Premium Track/Price Display Button 2 Variant/Generic UI Button/Price Display/text` | 13.46 | 40 | 39 | 0 | ✅ **该接** |
| 27 | `Shell/DailyRewardPopup.cs:451` `Title`（Header） | `menus ▸ …/Header Header/Title` | 18 | 40 | 36 | 1 | ✅ **该接** |
| 28 | `Shell/DailyRewardPopup.cs:455` `Sub-Title` | 同上 `Header Header/Sub-Title` | 18 | 40 | 36 | 1 | ✅ **该接** |
| 29 | `Shell/DailyRewardPopup.cs:482` `EverguildTextMeshPro`（Timer） | `menus ▸ …/Timer/EverguildTextMeshPro` | — | — | 50 | 1 | ⛔ **不是缺口（原版关着）** |
| 30 | `Shell/DailyRewardPopup.cs:497` `EverguildTextMeshPro (1)` | 同上 `Timer/EverguildTextMeshPro (1)` | — | — | 50 | 1 | ⛔ **不是缺口（原版关着）** |
| 31 | `Shell/DailyStreakPopup.cs:335` `Current Streak` | `menus ▸ Daily Streak Popup ▸ Streak Successful/Current Streak` | — | — | 70 | 1 | ⛔ **不是缺口（原版关着）** |
| 32 | `Shell/DailyStreakPopup.cs:347` `Current Streak Value` | 同上 `…/Current Streak/Current Streak Value` | — | — | 80 | 1 | ⛔ **不是缺口（原版关着）** |
| 33 | `Shell/DailyStreakPopup.cs:382` `Info`（Successful） | 同上 `Streak Successful/Info` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 34 | `Shell/DailyStreakPopup.cs:395` `Next Rewards text` | 同上 `Streak Successful/Timer/Next Rewards text` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 35 | `Shell/DailyStreakPopup.cs:401` `Timer Text` | 同上 `…/Timer/Timer Text` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 36 | `Shell/DailyStreakPopup.cs:479` `Daily Streak Broken` | 同上 `Streak Failed/Daily Streak Broken` | — | — | 128.1 | 1 | ⛔ **不是缺口（原版关着）** |
| 37 | `Shell/DailyStreakPopup.cs:481` `Current Streak Lost count` | 同上 `Streak Failed/Current Streak Lost count` | — | — | 66.9 | 1 | ⛔ **不是缺口（原版关着）** |
| 38 | `Shell/DailyStreakPopup.cs:483` `Info`（Failed） | 同上 `Streak Failed/Info` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 39 | `Shell/DailyStreakPopup.cs:486` `Button Text` | 同上 `Streak Failed/Generic Simplified UI Button/Button Text` | 10 | 55 | 12 | 0 | ✅ **该接** |
| 40 | `Shell/DeckSelectionPopup.cs:413` `Instructions 2` | `menus ▸ Deck Selection Popup with Tabs ▸ Instructions 2` | 18 | 40 | 36 | 0 | ✅ **该接** |
| 41 | `Shell/DeckSelectionPopup.cs:467` `Warning` | 同上 `…/Empty Collection Warning/Warning` | — | — | 36 | 0 | ⛔ **不是缺口（原版关着）** |
| 42 | `Shell/DeckSelectionPopup.cs:561` `Tab Text Own/Pre` | 同上 `Alliance Header Buttons/Tab buttons/Generic Tab UI Button[ 1]/Button Text` | 12 | 60 | 12 | 0 | ✅ **该接** |
| 43 | `Shell/DuelPopupWindow.cs:170` `MessageText` | `menus ▸ MessagePopupWindowDuel ▸ Window/MessageText` | — | — | 52.5 | 1 | ⛔ **不是缺口（原版关着）** |
| 44 | `Shell/ItemDrawer.cs:999` `text`（Price Display） | `menus ▸ **全部 67 颗** `Price Display/text``（跨窗） | 13.46 | 40 | 39 | 0 | ✅ **该接**（见 §3 注④） |
| 45 | `Shell/ItemDrawer.cs:1262` `ClippedText` 助手（4 调用点） | 混合：`AlreadyOwned` 开 auto[12~75]，`Army Name` 关 —— 见 §5 | — | — | — | — | ❓ **读不到（判据不唯一）** |
| 46 | `Shell/LiveOpsEventWindow.cs:527` `Deck Name` | `menus ▸ SkirmishModeEventWindow ▸ Ranked Deck Selection/Deck Name` | 18 | 72 | 36 | 1 | ✅ **该接** |
| 47 | `Shell/LiveOpsEventWindow.cs:529` `Deck Warlord` | 同上 `…/Deck Warlord` | 18 | 72 | 36 | 1 | ✅ **该接** |
| 48 | `Shell/LiveOpsEventWindow.cs:589` `Text` | 同上 `…/Numer Of Army Decks` | 18 | 63.4 | 36 | 1 | ✅ **该接** |
| 49 | `Shell/MenuWindowBase.cs:340`（`TextBox` 转发口） | 无固定对应件（随调用点变） | — | — | — | — | ⛔ **不是缺口（转发口已具备）** |
| 50 | `Shell/PopUpGameWindow.cs:396` `MessageText` | `generalgamewindows ▸ MessagePopupWindow ▸ MessageText`（2 按钮版 `MessagePopupWindow2Buttons` **逐值相同**） | 4 | 40 | 36 | 1 | ⛔ **不是缺口（已接，见 §3 注⑦）** |
| 51 | `Shell/PopUpGameWindow.cs:467` `Button Text` | 同上 `…/Buttons/…/Button Text`（1 按钮版 `auto[12~40]` / 2 按钮版 `auto[12~38]`） | 12 | 40 / 38 | 12 | 0 | ⛔ **不是缺口（已接，见 §3 注⑦）** |
| 52 | `Shell/PurchasePremiumWindow.cs:686` `Button Text` | `menus ▸ Purchase Premium Window…/Price Display Button 2/Generic UI Button/Button Text` | 12 | 38 | 12 | 0 | ✅ **该接**（见 §3 注⑤） |
| 53 | `Shell/PurchasePremiumWindow.cs:735` `Army Name` | 同上 `Scroll View/…/Army Container/Army Name` | — | — | 36 | 0 | ⛔ **不是缺口（原版关着）** |
| 54 | `Shell/SearchingOpponentWindow.cs:198` `Player Name` | `menus ▸ SearchingOpponentWindow…/Player Name` | — | — | 36 | 1 | ⛔ **不是缺口（原版关着）** |
| 55 | `Shell/SettingsWindow.cs:3214`（`Text` 助手，本行在 :4046，30+ 调用点） | `menus ▸ Main Menu Settings Window` 逐节点（见 §3 注⑥） | 多数开 | — | — | — | ✅ **该接** |
| 56 | `Shell/SettingsWindow.cs:3571` `Text`（输入框真值，本行在 :4513） | 同上 `…/Text Area/Text` | 18 | 40 | 14 | 3 | ✅ **该接** |
| 57 | `Shell/SocialWindow.cs:446`（`TextBox` 转发口） | 无固定对应件（随调用点变） | — | — | — | — | ⛔ **不是缺口（转发口已具备）** |
| 58 | `Shell/TutorialModePopup.cs:655` `Completed Text` | `menus ▸ Tutorial Mode Menu ▸ Completed Text` | 10 | 54 | 12 | 0 | ✅ **该接** |
| 59 | `Shell/WindowsManager.cs:397`（`Text` 转发口） | 无固定对应件（随调用点变） | — | — | — | — | ⛔ **不是缺口（转发口已具备）** |

**行数 = 59**（✅ 22 · ⛔ 35 · ❓ 2）。

---

## §3 注（每一条都影响判读，别跳过）

**① #16 `Buy Original Card`（`CardDetailPopup:738`）** —— 我们这颗的框 = `328.71 × 73.38`，与原版
`Alternate Art Panel/Buy Original Card Button` **逐位相同** ⇒ 就是它。原版那颗钮底下**有两颗候选 TMP**：
- `…/Generic UI Button/Button Text`（`INACT`，`''`）：`auto[12~38]` · base **12** · 折行 0；
- `…/Generic UI Button/Price Display/text`（`'300,00'`，**就是画面上那颗**）：`auto[12~54]` · base **39** · 折行 0。
⇒ 按**可见的那颗**（`text`）取值。两档**都开 auto**，所以裁定不受影响；写手照 `text` 那四格抄。
⚠️ 这颗字**是我们自己写的文案**（原版那格印价格）—— 面板在没有异画时整块关（`BuildAltArt` 末句）。

**② #17 `Title` 助手（`CardDetailPopup:1007/:1021`）** —— 它**本来就传了** `wrapPx = x2 - x1` ·
`10f` · `42f` · `36f`（11 个实参）⇒ **生效**（`x2 > x1` 在所有调用点恒真）。
`A333`/`A336③` 已经照原版逐颗核过：`'Create a copy of this card'` = `fs42 · auto[10~42] · base36`；
`'Upgrade this card\nto level {0}'` = `fs41.4 · auto[10~42] · base36`；异画那颗 `fs31.7 · auto[10~42] · base36`。
`P4` 判「不生效」是它**静态求值解析不出 `x2 - x1`**。⇒ **不用改。**

**③ #18 `ChatPanel:148`（`Text` 助手）** —— 它的调用点分两族：
- `ChatPanel.cs:327` `Name` —— **已经接上且与原版逐值吻合**（原版 `ChatPanel ▸ …/Name` = `fs40 · 基准21 · auto[10~40]`；我们传 `10f, false, 40f, 21f`）；
- `ChatPanel.cs:262` `Placeholder` —— 原版 `ChatPanel ▸ …/Placeholder` = `fs28 · 基准28`，**无 auto**。
⇒ **不是缺口**。（`:341` / `:417` 两处我**没有逐点定位**，但同族同窗，未发现任何一颗原版开 auto 的反例。）

**④ #44 `ItemDrawer:999`（`Price Display/text`）** —— 我**按 `m_text == '300,00'` 全库扫了 `bundle_menus_assets_all`**：
**67 颗，`m_enableAutoSizing` 全部 = 1**，`base` 全部 = **39.0**，折行全部 = **0**；
只有 `min/max` 分两族：**`13.46~40`**（多数）与 **`12~54`**（钉在 `Button Text` 那族旁的）。⇒ 该接无疑。
写手按**我们自己那扇窗**里对应的那一颗取 `min/max`（我不想替你挑 —— 见 §5·5）。

**⑤ #52 `PurchasePremiumWindow:686`（`Button Text`）** —— 它**不是「没传」而是「传了但不生效」**：
现读 `:221` = `PriceBtnTxtFont = 12f, PriceBtnTxtBase = 12f, PriceBtnTxtMin = 12f, PriceBtnTxtMax = 38f`，
而 `MenuDraw.TextBox` 那道闸是 `autoMinPx > 0f **&& fontPx > autoMinPx**` ⇒ `12 > 12` = **假** ⇒ **不生效**。
原版那一格 `fs12 · auto[12~38] · base12 · 折行0` —— 参数**逐值已对**，只差「`fontPx > autoMinPx` 这条闸太紧」。
⇒ **要么给这道闸补一支 `fontPx == autoMinPx` 的例外，要么把 `PriceBtnTxtMin` 调到 `< 12`** —— **由调度台裁**（我不改口径）。

**⑥ #55 `SettingsWindow:3214`（`Text` 助手）** —— 它有 **30+ 调用点**，我现读到的原版节点里
**开 auto 的是压倒多数**：`Tab Title` `auto[4~55]` · `VersionText` `auto[1~28]` · `Label` `auto[18~40]` ·
`SelectLanguageText` `auto[29~42]` · `EmailText`/`PasswordText` `auto[10~37]` · `Error Message` `auto[29~37]` ·
`Item Label` `auto[18~40]` · `Tab Toggle Title`、音频页三颗 `Label` `auto[18~42]`。
⇒ 裁定「该接」，但**这个助手要接就必须逐调用点传四格**（它今天连形参都没有 —— `Text(p, n, s0, x1, x2, y1, y2, fs, c, q, …)`）。
⚠️ **这是本表里最贵的一条**（一条改动撬动 30+ 个落点），建议**单独开一件**，别塞进字号线的顺手批。

**⑦ #50 / #51 `PopUpGameWindow` 的两颗** —— 我**第一版表里把 prefab 写成了 `menus ▸ GenericPromptWindow`，那是错的**
（`GenericPromptWindow` 是 `PromptPopup` 那一族，带输入框）。现核：本窗的 prefab 在
**`bundle_generalgamewindows_assets_all/GameObject/{MessagePopupWindow, MessagePopupWindow2Buttons}`**
（现读与 `Shell/PopUpGameWindow.cs` 文件头 ① 一致）。两颗的 `MessageText` 都是
`fs40 · 基准36 · auto[4~40] · 折行1`；`Button Text` 是 `fs40(1 钮)/fs38(2 钮) · 基准12 · auto[12~40] / auto[12~38] · 折行0`。
我们传的是 `MsgFontPx=40, MsgAutoMinPx=4, autoMax=MsgFontPx=40` 与 `BtnAutoMinPx=12, autoMax=fontPx(40/38)`
⇒ **`min/max/折行` 三格逐值吻合、闸也过得去**（`40>4`、`40>12`）⇒ **不是缺口**。
⚠️ **但 `base` 那一格我们传的是缺省 `0`**（= `SetAutoFitBox` 的「退回调用方那档」），原版是 **36 / 12**
—— **不在本账的裁定里**（`A1126` 问的是「接不接」），**如实登记**给调度台另立一笔。

---

## §4 `A1179` 那 5 处的四格真值（确认原版全开 autosize）

| # | 我们这处 | 原版那颗（bundle ▸ 路径） | `enableAutoSizing` | min | max | base | 折行 |
|---|---|---|---|---|---|---|---|
| 1 | `Shell/SkirmishEventWindow.cs:116` `Total Victories` | `menus ▸ SkirmishModeEventWindow ▸ …/Player victories/Skull Victories/Total Victories` | **1** | **18** | **48** | **36** | **1** |
| 2 | `Shell/EnergySinglePlayerOnlyEventWindow.cs` `Total Victories` | `menus ▸ EnergySinglePlayerOnlyEventWindow ▸ …/Player Victories/Total Victories` | **1** | **18** | **77** | **36** | **1** |
| 3 | `Reward Tile`（同窗） | 同上 `Reward Tile` | **1** | **18** | **45.87** | **36** | **1** |
| 4 | `Scoring Instructions`（同窗） | 同上 `Scoring Instructions` | **1** | **18** | **50** | **36** | **1** |
| 5 | `Energy Instructions`（同窗） | 同上 `Energy Instructions` | **1** | **18** | **72** | **50** | **1** |

（1 与 2 的 `max` **不同**（48 vs 77），**base 相同**；3/4/5 的 `max` 也各是各的 —— ⛔ 别一刀切。）
⚠️ #2/#3/#4/#5 的框在原版是 `ContentSizeFitter`/`CSFMinMax` 撑开的（**框宽跟文字走**）——
与 `W2` 报的同一族。写手照四格抄即可，**框宽那一格不是本件范围**。
⚠️ `Reward Tile` 在全库有 3 个同名 GO（`Reward Tile` / `_4350015992750624114` / `_6937917655567973041`），
**三颗字段逐值相同**（`fs45.87 · auto[18~45.87] · base36 · wrap1`）⇒ 不歧义。

---

## §5 读不到的那 2 处 + 还差什么

1. **`Shell/CardDetailPopup.cs:730` `Current Style`（`1 of 1`）** ——
   原版 `Alternate Art Panel` 底下**确实有一颗叫 `Current Style` 的件，但它是 `Image`、`m_Component` 只有
   `Transform + CanvasRenderer + Image` 三个、`m_Children` 空**（现读 `GameObject/Current Style.json`）。
   ⇒ **原版没有对应的 TMP ⇒ 没有 `min/max/base/折行` 可抄**；我们这颗（`fs28`，框 `418.5×38`）是**自造的**。
   **还差什么**：调度台裁定「自造件要不要给它 autosize」—— 判据不在原版 prefab 里（`Current Style` 是那张**风格缩略图**的空间）。
   ⚠️ 这颗所在面板在「没有异画」时整块关（`BuildAltArt` 末句 `p.gameObject.SetActive(has)`），
   **今天它只在有异画时可见，且那两句「没有异画」的文案是死支**。

2. **`Shell/ItemDrawer.cs:1262` `ClippedText` 助手** —— 4 个调用点**原版判据不唯一**：
   - `:961` `AlreadyOwned` → 原版那颗 **开** `auto[12~75] · base36`
     （实据 `mainmenuwarpforge ▸ Upgrade Panel/…/AlreadyOwned` 与 `menus ▸ …/AlreadyOwned`）；
   - `:1149` `Army Name` → 原版那颗 **关**（`menus ▸ Purchase Premium Window/…/Army Container/Army Name` = `fs36·基准36` 无 auto）；
   - `:1196` `Item Name` / `:1233` `quantity` → **我没定位到**这两颗的原版件（`ItemDrawer` 是**被多扇窗实例化**的抽屉，节点归属随宿主窗变）。
   ⚠️ **这个助手连 `autoMinPx` 形参都没有**（签名 = `(node, r, s, color, name, fontPx, q, wrapPx, st, align)`）⇒
   **要接必须先给它加形参** ⇒ **动共用件**。
   **还差什么**：① 逐调用点定位原版件（要先定「哪一个宿主窗的抽屉」）；② 调度台裁「一个助手撑 4 个语义不同的落点」怎么切。

3. **（不算「读不到」但必须出声）`Shell/AllianceMemberTab.cs:816` 的 `title` 那一格** ——
   它的**另一个**调用点 `counter` 已确证（原版 `Alliance Trophy Info Popup…/counter` = `auto[12~35]·base36·wrap1`），
   但 `title` 那颗**我没定位到原版件**（`Alliance Trophy Entry` / `Alliance Trophies View` 这两个 GO 名
   在 `bundle_menus_assets_all` 与 `mainmenuwarpforge` 里都**不存在**；我搜过 `bundle_menus_assets_all` ·
   `bundle_mainmenualwaysloaded_assets_all` · `bundle_scenes_scenes_mainmenuwarpforge` 三个包的 `GameObject/` 全名单）。
   ⇒ 我**没有**把它算成 ❓ —— 因为**该站点已经生效**（`TextSoft` → `MenuBox.TextBox` 无条件 `SetWrapWidth(r.W)`，
   而两个调用点都传 `autoMinPx = 12f`、`fontPx = 38f / 26.95f` 都 `> 12`）⇒ **这一行今天不缺东西**，
   `title` 那颗只是**参数是否与原版一致**这一层没核（**不影响本表的裁定**）。

4. **`P4` 的三处口径问题（我全表按现读订正，逐条在此说明）**：
   - **「不生效」这一列有 11 处是假的**（表里那 11 条我标了「已接」）—— `P4` 的静态求值器解析不出
     `whoR.W` / `x2 - x1` / `glowGetR.W` / `tw` 这类**变量实参**时，一并把 `wrapPx` 当成 0。
     而 `MenuDraw.Text` 的自适应闸是 `if (wrapPx > 0f)` **∧** `autoMinPx > 0f` **∧** `fontPx > autoMinPx` ——
     三个都真才生效。**变量实参不是「不生效」，是「静态判不了」**。
   - **`MenuWindowBase:340` / `WindowsManager:397` / `SocialWindow:446` 三处是「转发口」**，
     `P4` 把「这一行」当成一个站点，但它**本身没有可判的原版对应件**（对应件随调用点变）。
     三个口**都已经在转发** `wrapPx / autoMinPx / autoMaxPx / autoBasePx` ⇒ **这三个口不欠东西**；
     真欠的是那些**传 0 的调用点**（那属于各调用点自己的账）。
   - **`PurchasePremiumWindow:686` 是唯一一条「传了参数却不生效」的真缺口**（见 §3 注⑤）——
     其余「不生效」都是解析问题。

5. **`ItemDrawer:999` 的 `min/max` 我故意没替你固定**：67 颗里 `13.46~40` 与 `12~54` 两族并存。
   要定死得先知道**我们那一扇窗对应哪一颗**（`ItemDrawer` 是共用件）。`base 39 / 折行 0` 两格**全库一致**。

---

## §6 搜过的 bundle / 名字（纪律 1：报「没有」之前先打出搜过什么）

**bundle（`d:/2/新解包资源/assets_full/` 下）**：
`bundle_menus_assets_all` · `bundle_scenes_scenes_mainmenuwarpforge` · `bundle_scenes_scenes_battlearena1` ·
`bundle_generalgamewindows_assets_all`（`MessagePopupWindow` / `MessagePopupWindow2Buttons`）·
`bundle_mainmenualwaysloaded_assets_all`（`ChatMessageRow`）· `bundle_battlesharedresources_assets_all`（查过、无命中）。
⚠️ `d:/2/新解包资源/assets_full/bundle_menus_assets_all/GameObject/` 有 **16768** 个文件（去重后 **1667** 个名字）。

**根部名（`menu_dump.py <bundle> "<根名>"`，全部跑通）**：
`Daily Reward Popup` · `Daily Streak Popup` · `SkirmishModeEventWindow` · `EnergySinglePlayerOnlyEventWindow` ·
`Deck info Popup` · `Base Offer Popup` · `Booster Info Popup` · `Campaign Reward Window` ·
`Deck Selection Popup with Tabs` · `Purchase Premium Window` · `SearchingOpponentWindow` · `Tutorial Mode Menu` ·
`MessagePopupWindowDuel` · `Member Options Panel` · `Alliance Trophy Info Popup` · `Alliance Detail View` ·
`ChatMessageRow`（`bundle_mainmenualwaysloaded_assets_all`）· `Social Submenu Variant` ·
`Upgrade Panel` / `Alternate Art Panel` / `Main Menu Settings Window`（`scenes_mainmenuwarpforge`）·
`Alliance Panel`（`scenes_battlearena1`）· `Item Drawer` / `Deck Name`（**命中不唯一，已放弃这条路**）。

**按 `m_text` 直读 `MonoBehaviour/*.json` 的**（比 `menu_dump` 更准，绕开 200 字截断）：
`'Will get: +350'` · `'Upgrade this card\nto level {0}'` · `'Create a copy of this card'` ·
`'Maximum card tier reached'` · `'300,00'`（**67 颗全扫**）· `Scoring Instructions` / `Energy Instructions` /
`Reward Tile` / `Total Victories`（按 **GO 名**反查组件）。

**找过但**（在 `bundle_menus_assets_all` + `bundle_scenes_scenes_mainmenuwarpforge` 的 `GameObject/` 全名单里）**确实不存在**的名字：
`DeckEditor` · `Deck Editor` · `Deck Editing Window`（**这三个本来就不是我要找的**，顺手复核了 CLAUDE.md 那条更正）·
`Alliance Trophy Entry` · `Alliance Trophies View` · `PlayerLabel`（**作为 GO 名不存在** —— `PlayerLabel` 只在
`scenes_battlearena1 ▸ Alliance Panel` 的**子树**里，不在顶层名单；我是沿 `Alliance Panel` 走树走到的）。

**工具**：`d:/4/Unity/工具/menu_dump.py`（`--no-sprite --no-layout --depth N`，跑 **21** 次）。
`--no-layout` 是**故意的** —— 我要的是 **TMP 组件字段**（`auto[min~max]` / `基准=` / `折行=`），
不是布局后的矩形，走布局反而慢且会改写 RT 表（工具头注 ② 那条）。

---

## §7 主对话落地清单：**那 22 条该接的，四格都在这张表里，写手照抄即可**

| 落点 | min | max | base | 折行 |
|---|---|---|---|---|
| `Shell/AlliancePanelWindow.cs:345`（`Txt` 助手，**7 调用点参数不同**，逐点取） | 18 | 35 / **40**（只 `Name Text` 是 40） | 36 | 0 |
| `Shell/BoosterInfoPopup.cs:594` | 13.46 | 40 | 39 | 0 |
| `Shell/CardDetailPopup.cs:693` | 10 | 40 | 12 | 0 |
| `Shell/CardDetailPopup.cs:696` | 10 | 40 | 12 | 0 |
| `Shell/CardDetailPopup.cs:701` | 10 | 40 | 36 | 0 |
| `Shell/CardDetailPopup.cs:752` | 12 | 54 | 39 | 0 |
| `Shell/DailyRewardPopup.cs:319` | 18 | 45 | 36 | 1 |
| `Shell/DailyRewardPopup.cs:342` | 15 | 200 | 36 | 1 |
| `Shell/DailyRewardPopup.cs:390` | 13.46 | 40 | 39 | 0 |
| `Shell/DailyRewardPopup.cs:451` | 18 | 40 | 36 | 1 |
| `Shell/DailyRewardPopup.cs:455` | 18 | 40 | 36 | 1 |
| `Shell/DailyStreakPopup.cs:486` | 10 | 55 | 12 | 0 |
| `Shell/DeckSelectionPopup.cs:413` | 18 | 40 | 36 | 0 |
| `Shell/DeckSelectionPopup.cs:574` | 12 | 60 | 12 | 0 |
| `Shell/ItemDrawer.cs:999` | 13.46 **或** 12（见 §5·5） | 40 **或** 54 | 39 | 0 |
| `Shell/LiveOpsEventWindow.cs:527` | 18 | 72 | 36 | 1 |
| `Shell/LiveOpsEventWindow.cs:529` | 18 | 72 | 36 | 1 |
| `Shell/LiveOpsEventWindow.cs:589` | 18 | 63.4 | 36 | 1 |
| `Shell/PurchasePremiumWindow.cs:698` | 12 | 38 | 12 | 0 |
| `Shell/SettingsWindow.cs:4046`（`Text` 助手，**30+ 调用点逐点取**） | 逐点 | 逐点 | 逐点 | 逐点 |
| `Shell/SettingsWindow.cs:4513` | 18 | 40 | 14 | 3 |
| `Shell/TutorialModePopup.cs:655` | 10 | 54 | 12 | 0 |

⚠️ **改完别忘**：`MenuDraw.Text` 那三道闸（`wrapPx > 0` ∧ `autoMinPx > 0` ∧ **`fontPx > autoMinPx`**）——
**`PurchasePremiumWindow` 那条就是被第三道闸挡住的**；`ItemDrawer` / `AllianceMemberTab` 那类**助手没有 autosize 形参**，
要接得先动签名（**动共用件 ⇒ 自检按覆盖面跑**）。

---

## §8 我没做的事（如实登记）

- ⛔ 没跑任何 Unity / `_run_8_checks.sh` / `typecheck.sh`；⛔ 没动 git（只 `git status` 环境快照）。
- ⛔ **没改任何 `.cs` / 任何文档**（除本报告）；⛔ 没碰 `d:/2/**`。
- ⛔ 没有把任何「查不到」写成猜测 —— §5 三处全部标了「还差什么」。
- ⚠️ **两处我按住没猜**：`ItemDrawer:999` 的 `min/max`（两族并存）· `ItemDrawer:1262` 的 4 个调用点里
  有 2 个我没定位到原版件。**都由调度台裁，我不发明口径。**
