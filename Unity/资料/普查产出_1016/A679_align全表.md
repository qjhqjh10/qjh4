# A679 · `GameWindow.Text` 的 align 全表（只读普查 · 2026-10-16）

> 只读普查（子代理交回，**主对话代落盘** —— Explore 代理无写文件权限）。未跑 Unity、未动 git、未改 `d:/2` 任何文件（只读）。
> 枚举（逐节点实读，非照抄）：原版 TMP `m_HorizontalAlignment` 实读值 **`1=Left · 2=Center · 4=Right`**
> （枚举表出处 `d:/4/Unity/工具/menu_dump.py:100`；每颗都回 `MonoBehaviour_*.json` 读原字段）。
> ⇒ 对照：**我们 0 ↔ 原版 2 · 我们 1 ↔ 原版 1 · 我们 2 ↔ 原版 4**。

---

## 一 · 表一（我们侧）

### A) 收口口（全库只有这两个带 `int align`）

| 出处 | 接收者 | 实装 |
|---|---|---|
| `Shell/WindowsManager.cs:374-402` | `GameWindow` 实例方法 `Text` | `:393` 转 `MenuDraw.Text`（**无 align 口**）→ `:395/396` 按 align 补 `AlignLeft/Right` → `:400` 才 `ClipText` |
| `Shell/ItemDrawer.cs:1256-1269` | `ItemDrawer.ClippedText`（**A678，另一条账**） | `:1262` 转 `MenuDraw.Text` → `:1264/1265` 补对齐；唯一调用点 `:1233` 传 2 |

`WindowsManager.cs` 里**没有第二处**转发/包装（全文 `Text|align` 逐行看过：只有 `:374` 定义 + `:393` 转发 + 注释）。

### B) 真绑到 `GameWindow.Text` 的调用点 —— 逐处一行（**10 处**，折算 **16 次执行**）

| 文件:行号 | 接收者 | 传的 align | 那一格是什么 |
|---|---|---|---|
| `Shell/CampaignRewardWindow.cs:662` | `CampaignRewardWindow`(this) | **默认 0** | `Warning`(高级列) 的 `WarningText`（`BuildUnlockButton` 每列跑一次 ⇒ ×2） |
| `Shell/CampaignRewardWindow.cs:714` | 同上 | **1**（命名实参 `align: 1`） | `Point Count`（原版 '100'） |
| `Shell/CampaignRewardWindow.cs:719` | 同上 | **默认 0** | `Claimed Text`（原版 'Change Deck'） |
| `Shell/DailyStreakPopup.cs:577` | `DailyStreakPopup`(this) | **默认 0** | 奖格 `Reward Name` |
| `Shell/DailyStreakPopup.cs:586` | 同上 | **默认 0** | 奖格 `Collect Text` |
| `Shell/InboxWindow.cs:559`（`RowText` 薄包装；调用点 `:497/:499/:502`） | `InboxWindow`(this) | **字面 0**（对齐由 `:562/563` 事后补） | 消息行 `Title`/`Date`/`New`（3 次） |
| `Shell/RewardWindow.cs:1117` | `RewardWindow`(this) | **位置实参 0** | `Collect Button/Button Text` |
| `Shell/RewardWindow.cs:1146` | 同上 | **位置实参 2** | `Premium Disclaimer` |
| `Shell/RewardWindow.cs:1153` | 同上 | **位置实参 0** | `Tap To Continue/Tap Text` |
| `Shell/RewardWindow.cs:1475`（`GlowText` 薄包装；调用点 `:1104/:1106`） | 同上 | **默认 0** | `Title/Glow{Get,Preview} reward/Text *`（2 次） |

**条数结论**：10 处里「没传 align / 显式写 0」= **8 处**（`CRW:662`·`CRW:719`·`Streak:577`·`Streak:586`·`Inbox:559`·`Reward:1117`·`Reward:1153`·`Reward:1475`）；
真传非 0 = **2 处**（`CRW:714`=1 · `Reward:1146`=2）。折算执行次数 = **13 次默认 0 / 3 次非 0**。

### C) 其余 `.Text(` / `.TextBox(`（无法传 align —— 那些重载签名里没有这个形参），按接收者族（Shell，**非注释调用行**）

| 接收者族（定义处） | 有无 `int align` | Shell 行数 | 主要文件:行 |
|---|---|---|---|
| `MenuDraw.Text`(`MenuDraw.cs:1628`) | 无（有 autoMax/autoBase/clip） | 129 | 全窗通用，例 `CardDetailPopup.cs:505,621,631…` |
| `MenuDraw.TextBox`(`:1695`) | 无 | 49 | `ReferralPopupWindow` 11 · `PurchasePremiumWindow` 8/9 · `TrophyInfoPopup` 5 |
| `MainMenuSubmenuWindow.Text/TextBox`(`MenuWindowBase.cs:284/:325`) | 无（`Text` 走 `x1,x2,y1,y2,int scale`） | 43 = `_win.` 26（ShopWindow 9 · CampaignTab 6 · ForgeTab 5 · RewardsWindow 4 · MissionsTab 2）+ 裸调用 17（CollectionWindow 16 + `MenuWindowBase.cs:487`） | — |
| `SocialView.Text`(`SocialWindow.cs:375`) / `SocialPage.Text`(`:505`) | 无（`bool wrap`/`bool alignLeft`） | 33 = `v.` 9（AllianceMemberTab）+ `Page.` 1（`SocialWindow.cs:508`）+ 裸 23（AlliancesTab 15 · AllianceMemberTab 4 · FriendsTab 4） | — |
| `ProfilePage.Text`(`PlayerProfileWindow.cs:613`) | 无（`bool autoFit/wrap/alignLeft`） | 52 | ProfileTab 28 · RankedTab 8 · AchievementsMenu 6 · AvatarTab 5 · TitleTab 4 · BattleLogTab 1 |
| `ChatPanel.Text`(`:140`) / `ChatTab.Text`(`:666`) | 无（`bool alignLeft`；`:666` 多 `PxRect? clip`） | 7 | ChatPanel `:251,289,303,379` · `:729,731,755` |
| `SettingsWindow.Text`(`:1754`) | 无（`PxRect? clip`） | 16 | `:566,614,616,780,1069,1097,1346,1385,1411,1418,1479,1497,1499,1520,1570,1789` |
| `MainMenuRuntime.Text`(`:103`) | 无 | 7 | `:423,936,987,1232,1396,1405,1545` |
| `MatchLogRow.Text`(`:141`，`bool alignLeft`) | 无 | 4 | `:202,208,277,294` |
| `MenuDraw.Text`（类内自调 `MenuDraw.cs:2734`） | 无 | 1 | — |
| **`GameWindow.Text`**（`WindowsManager.cs:374`） | **有** | **10** | 见表 B |

Shell 合计非注释调用行 **351**（另有 13 行是各重载的**定义**）。
⚠️ 前一位代理数到的「31 行」在此口径下**复不出**（既非 351、也非任一子族数）。

### D) `Editor/*.cs`（非注释 35 行；**0 处**绑 `GameWindow.Text`、**0 处**能传 align）

`MenuDraw.Text/TextBox` 18（SettingsScene 8 · MainMenuScene 6 · ShellScene 3 · ShopScene 1）·
`vpWin.`（= `RewardsWindow` → `MainMenuSubmenuWindow.Text/TextBox`）8（ShellScene `:1717,1722,1770,1814,1815,1816,1891,1892`）·
`win.` 7（RewardsScene `:4404,4420,4459,4463,9982` · ShopScene `:3521,3588`）·
`pg.`（= `SocialView.Text`）2（MainMenuScene `:5889,5987`）；裸 1 行是定义（`DeckScene.cs:70`）。

---

## 二 · 表二（原版侧真值）

出处前缀 **P** = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（`GameObject/<prefab>.json` → `MonoBehaviour/MonoBehaviour_<PathID>.json`，值 = 该 json 的 `m_HorizontalAlignment`，逐颗实读）。

| 我们的那一格（文件:行号） | 我们的 align | 原版节点路径 | 原版值（实读） | 出处（P/…） |
|---|---|---|---|---|
| CRW:662 `WarningText` | 0 | `Campaign Reward Window/…/Premium Rewards/Rewards/Unlock Button/Warning` | **2 = Center**（`m_text`='Warning or Tip'） | MonoBehaviour_3074452337041579142.json |
| CRW:714 `Point Count` | 1 | `…/Base Rewards/…/Unlock Button/Point Count` · `…/Premium Rewards/…/Unlock Button/Point Count` | **1 = Left**（两列同值，'100'） | -4261334616534705018 · -8183754553003893626 |
| CRW:719 `Claimed Text` | 0 | 同上两列 `/Unlock Button/Claimed Text` | **2 = Center**（'Change Deck'） | 8176012911259524230 · 3423181592951037062 |
| Streak:577 `Reward Name` | 0 | `Daily Streak Reward Popup Entry/NormalReward/Reward Holder/Reward Name` | **2 = Center**（'1 Booster Pack'） | -6745572749593994111 |
| Streak:586 `Collect Text` | 0 | `Daily Streak Reward Popup Entry/Collect/Collect Text` | **1 = Left**（'Claim'） | -3990580155211047807 |
| Inbox:559（参数恒 0，事后对齐） | Title/Date→1 · New→2 | `Message Container/Content/{Title,Date,New}` | **1 · 1 · 4**（Left/Left/Right） | -2428977095636472911 · -7998370107097994319 · 1276296025495645105 |
| Reward:1117 `Button Text` | 0 | `Reward Window/Content/Collect Button/Button Text` | **2 = Center**（'Collect'） | 3472230255756962161 |
| Reward:1146 `Disclaimer Text` | 2 | `Reward Window/Content/Premium Disclaimer`（TMP 就在该节点自己身上） | **4 = Right** | -6244232454014899855 |
| Reward:1153 `Tap Text` | 0 | `Reward Window/Content/Tap To Continue` | **2 = Center** | -4710645167467075215 |
| Reward:1475 `GlowText` | 0 | `Reward Window/Content/Title/Glow Get reward/Text Get Reward` · `…/Glow Preview reward/Text Preview Reward` | **2 · 2**（Center） | 701106173098715505 · 402533371091937649 |

---

## 三 · 不一致清单（**要做** —— 铁律 11）

1. 🔴 `Shell/DailyStreakPopup.cs:586`（`Collect Text`）：**我们 0（居中）vs 原版 1（Left）**。
   出处：`P/GameObject/Daily Streak Reward Popup Entry.json` → `P/MonoBehaviour/MonoBehaviour_-3990580155211047807.json`（`"m_HorizontalAlignment": 1`，`m_text` = "Claim"）。
   同族 `Reward Name`(:577) 原版是 2 ⇒ 那一处没问题。候选修法：该行补 `align: 1`（⛔ 用**命名实参**，见 `WindowsManager.Text` 尾注）。
2. 🔴 `Shell/CampaignRewardWindow.cs:410-412` 与 `:414-416`（**不走 align 形参**：`MenuDraw.Text` + `MenuDraw.AlignRight`）——
   两颗 Title 我们**右对齐**，原版是 **2（Center）**：
   出处 `MonoBehaviour_-7292721294353392506.json`（'Campaign Rewards'）· `MonoBehaviour_-3653034741740364666.json`（'Available rewards'），两值都是 2。
   ⇒ 与 A635 修掉的 `Warning` **同一个错因**（"原版是 Right" 的旧结论），**A635 只扫了 `Warning` 那处**。**未在源码/注释里找到任何为 `AlignRight` 辩护的判据**。
3. 其余 9 处（表二逐行）**一致**（含 Inbox 的 1/1/4 —— 它绕开 `align` 形参、用事后 `AlignLeft/Right`，结果对得上）。

---

## 四 · 没查清的

- `Campaign Reward Window` prefab 里还有一颗 `Tap To Continue`（`MonoBehaviour_-8061794835568943994.json`，H=2 Center，fs=55）：**我们这扇窗不建它**（该文件只有 3 个 `GameWindow.Text` 调用点；`Tap To Continue` 只出现在 `RewardWindow`）——是「原版有、我们没建」还是「建在别处」**没查**（超出 align 这一笔）。
- Daily Streak Popup 窗级那 7 颗（`Streak Successful/Failed` 里的 `Current Streak`/`Timer Text`/`Button Text`…）走 `MenuDraw.Text` + 事后 `AlignLeft/AlignRight`，**不在这条账**（align 形参够不到）⇒ 本轮**未逐颗核**（已知 `Window Title` A493#6 已核过 Left）。
- Editor 侧的字全是夹具探针字（"TNode"/"a297 label"/"A266WrapProbe"…），**没有原版对应格** ⇒ 不进表二。
- 搜过：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（`GameObject/` 名全表扫 `inbox`、`message`、`reward window`、`streak`；词 = `Campaign Reward Window` / `Reward Window` / `Daily Streak Popup` / `Daily Streak Reward Popup Entry` / `Inbox Menu` / `Message Container`）。
  **没搜**的同族包：`bundle_generalgamewindows_assets_all` · `bundle_mainmenualwaysloaded_assets_all` · `bundle_menusharedresources_assets_all`（A679 涉及的这几扇窗都在 `menus` 里，另三包未展开）。
