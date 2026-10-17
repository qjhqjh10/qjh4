# W_F · 战役 `Premium Panel` 三态 + Header 文案 + 阵营条配色（T20）

> 2026-10-17 · 写手代理（界面线 T20）· 只改 `Shell/CampaignTab.cs` 一个文件。
> 判据优先级照铁律 2：**反编译方法体 > prefab 实读 > 本仓施工图 > 用户实拍（只当验收）**。

## 一、结论 + 改动清单

三件全部落地，未跑 Unity（按调度台要求）：

| # | 件 | 结论 | 代码位置 |
|---|---|---|---|
| 1 | `Premium Panel` 三态 | ✅ 已接 `ApplyPremiumState`；本地口径**固定 state 2**（我们挑的，见 §二·1） | `Shell/CampaignTab.cs:1053-1097`（三个容器）· `:1126`（调用）· `:1161-1178`（开关） |
| 2 | Header `Title` / `Points` 文案 | ✅ `Title` 改阵营本地化词条（去掉 `ToUpperInvariant`）；`Points` 改 `Loc` + `{0}` 占位 | `:807-829` |
| 3 | 阵营条进度条配色（`A818`） | ✅ 三张图/九宫/染色/满档绿**全部按 prefab 与反编译实读值**重做 | `:86-100`（常量）· `:746-777`（`BuildArmySlider`） |

**关键判据（本轮新坐实，之前 R4 记为「pid 没解析出来」）**：

```
python 工具/menu_dump.py bundle_menus_assets_all "Campaign Army Item Button" --depth 5
```
逐行实读（原版 `Slider` 三件）：
- `Background` = `40k_campaign_bar_bg` 42×18 · 九宫 `(20,0,20,0)` · `Sliced` 色 `(0.299,0.289,0.689,1)` · `ppuMul=2.0`
- `Fill` = `40k_campaign_bar_fill` 42×18 · 九宫 `(10,0,10,0)` · 色 `(1,0.509,0,1)`（无 ppuMul）
- `Outline` = `40k_campaign_bar_outline` 46×22 · 九宫 `(20,0,20,0)` · 色 `(1,0.841,0,1)` · `ppuMul=2.0`

满档绿 = 原版代码：`CampaignArmyUIDecorator.CheckFillColor` 在「当前值 == 最大值 ∧ 最大值 ≥ 1」时
把 **`fill` 和 `outline` 两颗都设成 `completedColor`**（`CampaignArmyUIDecorator__CheckFillColor.c:26-44`），
prefab 值 = `(0.02002177, 0.8867924, 0.01254893, 1)`（`MonoBehaviour_5329563854910789322.json`）。
⇒ **满档时金描边也一起变绿**（用户实拍逐像素核过：绿条连边框都是绿的）。

实拍取样交叉验证（`%TEMP%\r4_crops\a_bars_L/R.png`）：
琥珀填充 ≈ `(195,101,10)` · 满档绿 ≈ `(14,168,10)` · 槽底 ≈ `(27,26,58)` ·
与「染色 × 贴图灰度（fill 平均 0.64）」的乘积一致。

## 二、三态表 / 文案对照 / 配色值

### 二·1 三态表（逐条出处 = 反编译 `CampaignPremiumPointsPanel__ChangeState.c`）

| state | 枚举名 | 点数行 `pointsObject` | 按钮 `collectButton` | 倒计时 `timerDisplay` | 按钮文字 | 出处 |
|---|---|---|---|---|---|---|
| 0 | `PremiumPurchased` | **开** | **开** | **关** | `MainMenu/General/Collect`（收集 / Collect） | `ChangeState.c:15-36` + `:87-95` |
| 1 | `PremiumNotPurchased` | **开** | **开** | **关** | `MainMenu/PurchasePremium/Premium`（高级 / Premium） | `ChangeState.c:66-86` |
| 2 | `AlreadyCollected` | **关** | **关** | **开** | （按钮整颗关，不取值） | `ChangeState.c:42-57` |

标题（`Title`）三态都**不动**（`ChangeState` 不碰它）⇒ state 2 = 只留标题 + 倒计时，
与用户实拍一致。两条按钮词条名 = prefab `MonoBehaviour_-4598054059081945715.json` 的 `mTerm` 原文。

**state 由什么驱动（查清了）**：`CampaignPremiumPointsPanel.Initialize` 里
`collected ? 2 : (campaign.IsPremium ? 0 : 1)`（`CampaignPremiumPointsPanel__Initialize.c:36-43`）——
两个输入**都是服务端状态**（`Campaign.IsPremium` + 当日 `collected`）。

**我们的口径（⛔ 不是原版，代码里已如实标注）**：
本地数据模型里没有这两个字段（`CampaignData.cs` 全文 0 个 `DateTime` / 0 个 `*Time*`，
也没有「高级已购 / 今日已领」）⇒ `LocalPremiumState()` 固定返回 **state 2**：
① 与用户实拍一致；② 不假装有「可领取」行为（原版 `Collect` 走 PlayFab 云脚本）。
将来接活动数据只需换这一个函数（`ApplyPremiumState` 三档都接好了）。

### 二·2 文案对照

| 位置 | 改前 | 改后 | 判据 |
|---|---|---|---|
| `Campaign Header/Title` | `CampaignData.Selected.ToUpperInvariant()` ⇒ `ULTRAMARINES` | `Loc.T("Armies/" + army)` ⇒ 中文「极限战士」/ 英文 `Ultramarines` | `CampaignHeaderDisplay__Initialize.c:72` = `ArmyUtilities.GetLocalizedArmyName(...)`，**无大写转换** |
| `Campaign Header/Points` | `"Points: " + 9999`（硬编码英文） | `Loc` 词条 `Points: {0}` / `积分：{0}` + `CampaignData.Points` | `CampaignHeaderDisplay__Initialize.c:84-94`：硬编码键 `Rewards Menu/Points`（该字面量在 `global-metadata.dat` 偏移 654216）；`String.Format` 填 `CampaignPersistence.ArmySpecificPoints` |
| `Premium Panel` 按钮 | `"Continue"`（prefab 开发占位串） | state 0 = `Collect` / state 1 = `Premium`；state 2 整颗关 | prefab `collectText` / `becomePremiumText` 的 `mTerm` |

⚠️ **`Points` 的 Loc 缺口（见 §四·1）**：`Rewards Menu/Points` 目前**不在 `Core/Loc.cs`**，
而 `Loc.cs` 不在本轮白名单 ⇒ 代码写成
`Loc.HasEntry("Rewards Menu/Points") ? Loc.T(...) : Loc.T("MainMenu/Campaign/Points")`。
**今天中文显示「战役点数：9999」**（借用的同格式既有键），英文 `Points: 9999` 与原版逐字一致；
主对话把真键补进 Loc 后，这一行**自动**变成官方「积分：9999」。

### 二·3 配色值（全部来自 prefab / 反编译，非取样猜测）

| 件 | 图 | 九宫 | 角块（ppuMul） | tint |
|---|---|---|---|---|
| 槽底 `Slider/Background` | `40k_campaign_bar_bg` 42×18 | `(20,0,20,0)` | `20 ÷ 2.0 = 10` | `(0.299, 0.289, 0.689, 1)` |
| 部分填充 `Slider/Fill` | `40k_campaign_bar_fill` 42×18 | `(10,0,10,0)` | `10`（无 ppuMul） | `(1, 0.509, 0, 1)` 琥珀 |
| 描边 `Slider/Outline` | `40k_campaign_bar_outline` 46×22 | `(20,0,20,0)` | `20 ÷ 2.0 = 10` | `(1, 0.841, 0, 1)` 金 |
| **满档**（fill **和** outline 同色） | 同上两张 | — | — | `(0.02002177, 0.8867924, 0.01254893, 1)` 绿 |

队列：`Background = QArmySlider(3036)` → `Fill = 3037` → `Outline = 3038`（原版兄弟序）。
⚠️ 填充值仍是我们挑的：`ClaimedCount ÷ NodeCount`，只给本地有内容的阵营填；
原版 `Slider.value` 由哪个服务端字段写**仍未查到**。

## 三、没做的 / 没查清的

1. **`Premium Panel` 底图换图 + 辉光未做（本轮范围外，但我们连件都没建）**：
   原版 `ChangeState` 还会
   `panelBackground.sprite = collectedSprite / notCollectedSprite`（`ChangeState.c:27-28` / `:52-54`）
   并 `backgroundShinyEffect.enabled = 0/1`（`:55-56` / `:91-92`）。
   我们的底图是单图 `WF_UI_Ranked_Background_Gold`，**没有 `UIEffect` 辉光**。
   两个 sprite 的 PPtr 名字本轮**没解析**（未查清）。
2. **`Premium Panel` 的 `Quantity` 值 `"200"`** 仍是占位：原版是 `Initialize` 的 `pointReward`
   参数（服务端下发）。state 2 下整行隐藏，当前不可见。
3. **`Premium Panel` 标题** 仍是 prefab 出厂串 `Premium Campaign daily bonus`
   （`ChangeState` 不碰标题；运行期是否有别处替换**未查**）。
4. **`Campaign Header` 的 `premiumButton` / `alreadyPremium`**：原版按 `IsPremium` 二选一显示
   （`CampaignHeaderDisplay__Initialize.c:42-48`），我们**这两件都没建**（本轮范围外，见 §五）。
5. **`13 vs 可见 9`** 与 **地图「可领」标记**：判据未定（U2 §五·1/2）⇒ 按指令**没碰**。
6. 进度条的 `end` 端帽：prefab 里 `Campaign Army Item Button/Slider` **没有** `end` 子件
   ⇒ 本轮**正确地不加**（不要照 AllianceMemberTab 那套抄）。

## 四、typecheck 结果

| 轮次 | 结果 |
|---|---|
| 第 1 次（本文件改完后） | 运行时 0 · 编辑器 0 ✅ |
| 第 2/3 次（别的写手正在改 `Shell/SettingsWindow.cs`） | 运行时 13 / 11 错，**全部集中在 `SettingsWindow.cs`**（`_ddQuality`/`_ddWindowMode`/`_ddTextInHand`/`TihTermWhere`/`UiTerm`），编辑器 0；`CampaignTab.cs` **零错** |
| 末次（等待 ~2 分钟后重跑） | 运行时 0 · 编辑器 0 ✅ |

未跑 Unity（遵守「⛔ 不要跑 Unity」）。

## 五、顺手发现（⛔ 均未改）

1. **`Core/Loc.cs:2044` 的 `MainMenu/Campaign/Points` 是孤儿键**（全仓 0 调用点），
   且它的键名在 `global-metadata.dat` 里**不存在** ⇒ 不是原版键。
   原版真键 = `Rewards Menu/Points`（"Points: {0}" / "积分：{0}"）。
   **建议主对话在 Loc 补三条**：`Rewards Menu/Points`（`积分：{0}`）、
   `MainMenu/General/Collect`（`收集`）、`MainMenu/PurchasePremium/Premium`（`高级`）——
   本文件三条都已写成「有就走 Loc」。
2. `CampaignHeaderDisplay` 的 `premiumButton` / `alreadyPremium` 未建（同 §三·4）；
   若要补，判据就在 `CampaignHeaderDisplay__Initialize.c:42-48`。
3. 原版 `CampaignArmyUIDecorator.OnObjectUpdated` 还会 `shinyEffect.Play(...)`（进度变化时闪一下），
   我们没建这个效果（`CampaignArmyUIDecorator__OnObjectUpdated.c:21-24`）。
4. `CampaignPremiumPointsPanel` 的两份 prefab 实例
   （`MonoBehaviour_-4598054059081945715.json` / `MonoBehaviour_5564476211423155967.json`）
   的 `collectText` / `becomePremiumText` **逐字相同**，本轮取任一即可。
