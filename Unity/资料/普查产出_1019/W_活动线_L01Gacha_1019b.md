# W · 活动线 L01 · Gacha 抽卡活动页 —— 写手报告（2026-10-19）

> 本轮只做 **L01**（⛔ 未做 Raid / Expansion Pass）。
> 验证口径：只跑了秒级类型检查（`TMPDIR=/tmp/wf_l01 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**）。
> ⚠️ 中途有一次运行报 6 条错，**全部在 `Shell/MenuTutorialFlow.cs`**（`PrefStage` / `PrefDone` 未定义）——
> 那是**新手引导代理正在写的文件、不属于本线**（按简报不改别人的文件）；约 45 秒后重跑已归零。
> **未跑** Unity 批处理、任何 `*.Run` 自检、player、真机点击；未动 git；未改任何正本。

---

## 一、结论（建了什么 / 原版判据）

### 1·1 建了什么

**新窗口 `GachaEventWindow`（承载窗外壳 = `Parent Event Window` 那一族 + 恰好一个 Gacha 页）**：

| 区块 | 内容 |
| --- | --- |
| 承载窗外壳 | `Menu Dark Background`（点暗底关窗）· `Content Area/Background`（沿用 `ParentEventWindow` 的渐变常量）· `Tab Buttons` 左栏（底板渐变 + 阴影 + 母版页签[关] + 唯一运行期页签「The Vault」）· `Tabs` 容器 · `Shadow (1)`（出厂关）· 吸收层 |
| 左页 `Chest panel` | `Mask`(RectMask2D → `ViewportClip`) + `Background`(`40k_shop_popup_info_bg`) + 两片旋转 `Crate Shadow` + `Highlight Crate` + `Crate` + `Price Display Button`（高亮框 `OctagonUI_Filled_SDF` + `40K_button` 开箱钮 + 票图标 + 价格 `--`）+ `Video Player`（空节点）+ `Header`（`The Vault` 标题 / `Ends in:` + `--` / 时钟）+ `Where get cards` 钮（**真接** `WindowsManager.OpenWhereToGetCards`） |
| 右栏 `Rewards Panel` | 边框 `40K_dropdown_bg` + 平铺纹理 + `Special Items` 标题 + `Ends in:` + `--` + 滚动区（`ViewportClip` + `Content` + `Gacha Drawer Holder` 模板[关]）+ `Reward count --/--` + `GuaranteeRewardInfo` + 进度条（0 填充 + `--/--` 计数）+ `Player has all items`[关] + `Completed`[关] |
| 开窗入口 | `WindowsManager.PrefabRefGachaEvent = "Gacha Tab"` + `WindowsManager.OpenGachaEvent()`（照 `OpenParentEvent` 的 `OpenByRef` 形状） |

### 1·2 原版判据（逐条可复跑）

- **类桩**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{GachaEventTab,GachaCratePanel,GachaRewardsPanel,GachaRewardContainer}.cs`（字段表逐条读过）。
- **行为（反编译方法体）**：
  - `BaseParentEventWindow__SetupTabs.c` / `BaseParentEventWindow__CreateEventTab.c` —— Gacha 是 `Parent Event Window` 里由 liveops 事件列表动态建出来的一个 tab，不是独立窗口。
  - `GachaEventTab__OnOpen.c` / `__Initialize.c` / `__GetCardsClick.c`（`Where get cards` → `WindowsManager.OpenWindow(getCardsWindow, config)`）。
  - `GachaCratePanel__Awake.c` / `__RefreshVisuals.c` / `__OpenCrateButtonClick.c`（开箱 = 开视频 + `ContainerOfferEvent.TryPurchaseOffer`）。
  - `GachaRewardsPanel__RefreshFooter.c` / `__RefreshContainers.c` / `GachaRewardContainer__Initialize.c`（进度 = `PlayerHasAllSpecialItems` + `CratesSinceLastLegendary`；奖励格 `current/max` → `Claimed`）。
- **prefab 节点树**：`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Gacha Tab" --depth 12 --relative`（落地副本 `d:/4/_tmp_view/gacha_tab_dump.txt`，120 行）+ 对 `RectTransform` / `MonoBehaviour` / `Sprite` 的文件级实读（根 `RectTransform_8763003151348586113.json`、根 MB `MonoBehaviour_-665041264658295167.json`）。
- **本地化键**（`Resources/official_loc_13lang.json` 的 `keys/en/zh` 三列逐条命中，**不是自拟**）：
  `Event_Description/Gacha/TabTitle`（The Vault / 金库）
  `Event_Description/General/EndsIn`
  `Event_Description/ExpansionPass/Vault/SpecialItems`
  `Event_Description/Gacha/GuaranteeRewardInfo`
  `Event_Description/Gacha/AllRewards`
  `Event_Description/ExpansionPass/Vault/{Completed,CompletedTip}`
  `Rewards Menu/Claimed`
  `Event_Description/BattlePass/WhereButton`
- **承载窗外壳**：直接复用 `Shell/ParentEventWindow.cs` 的 **public 常量**（矩形 / 渐变 / 页签几何 / 颜色 / art 名），⛔ 没有抄第二套判据。

### 1·3 有意偏离（照实记录，⛔ 不冒充原版）

1. **独立窗口 vs tab 页**：原版 Gacha 是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的一个 tab；本地**没有 liveops 事件列表** ⇒ 建「承载窗外壳 + 恰好一个 Gacha 页」的等价形态（= 只有 Gacha 一个活动时的样子）。根名沿用原版**页** prefab 的 `Gacha Tab`。
2. **空数据显示**：原版 `GachaEventTab.Initialize` 在 `GachaEvent.CurrentContainer == null` 时调 `WindowTabBase.CloseTab`（**没数据就没有这个 tab**）。本任务验收口径 = 「页面能打开 + 空态出声」⇒ 保留页面、数值 `--`，并在 `Open()` 里出声（见 `WarnOnce`）。
3. **数值占位**：价格 / 奖励计数 / 进度计数 / 倒计时一律 `--`（⛔ 不编数字/日期）。本地化键缺失时 `Loc.T` 自会返回键名并出声（本轮 9 条键全部命中，没有走到那一档）。
4. **两张图回退**（源图在本地、未导入 `Resources/`，均 `Debug.LogWarning` 出声）：
   - `40k_Crate_Tier5_Warp_premium`（`bundle_boosterpacks_assets_all/Texture2D/`）⇒ 回退 `40k_Crate_Tier5_Warp`；
   - `Glow UI sharp W40K`（`Assets/CardPresentation/Art/原版/去重资源/Glow_UI_sharp_W40K.png`）⇒ 回退 `Glow_UI_W40K`。
5. **视频/开箱动画未实现**：`Video Player` 只建空节点（本地没有 `Gacha Crate Opening.mp4`）；开箱点击只出声，不假装开箱。
6. **奖励格**：没有 `SpecialDropTable` ⇒ 只建模板 `Gacha Drawer Holder`（**关着**）+ `--/--`；不拿 prefab 里的示例当真实奖励。
7. **`ToBackground` 多关一次物体**：本壳是扁平 quad + 显式队列，没有原版独立 `Canvas.sortingOrder`；本窗 3630–3659，而 `WhereToGetCardsPopup` 在 3479–3487 ⇒ 只写 `state=Background` 会把弹窗盖住。处置 = 被压背景时 `gameObject.SetActive(false)`；关弹窗时 `ShowPreviousWindow` 的无参 `TryOpen()` Background 支会 `SetActive(true)` 把它带回来（**这是我们挑的处置，不是原版 base 的行为**）。

---

## 二、改动清单（文件:行号）

| 文件 | 行号 | 改动 |
| --- | --- | --- |
| `Unity/MyGame/Assets/CardPresentation/Shell/GachaEventWindow.cs` | **新建 · 1–881** | 整个 Gacha 活动页（文件头判据 1–86；队列档 91–115；根/窗参 118–135；页几何 137–204；图/色 206–240；本地化键 242–259；状态/自检读口 261–322；`Create` 325–341；`Open`/`ToBackground`/`WarnOnce` 343–406；`Build` 386；`BuildShell` 409；`BuildShellTail` 431；`BuildTabBar` 444；`BuildTabButton` 482；`BuildPage` 547；`BuildChest` 561；`BuildCrateShadow` 613；`BuildPriceButton` 626；`BuildHeader` 666；`BuildRewardsPanel` 713；`BuildRewardsHeader` 737；`BuildRewardsScroll` 767；`BuildRewardsFooter` 788；`BuildRewardsCompleted` 825；取图/回退 851–881） |
| `Unity/MyGame/Assets/CardPresentation/Shell/GachaEventWindow.cs.meta` | 新建 | `guid: a7a94d81ca3a404bb68c5c66e6dfcfea`（类名 == 文件名，避免 `!u!115` 存根） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:717-724` | 新增 `PrefabRefGachaEvent = "Gacha Tab"`（含「原版没有独立窗口」的说明） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:961-974` | 新增 `OpenGachaEvent()`（`OpenByRef` + `GachaEventWindow.Create`） |

⛔ 未碰：`ParentEventWindow.cs` / `LiveOpsEventWindow.cs` / `MainMenuRuntime.cs` / `MenuTutorial*.cs` / `TutorialPointerUi.cs` / `Battle/**` / `RuleEngine/**` / `WarpforgeVFX/**` / `Core/CardView.cs` / `Scenes/*.unity` / 正本 / `d:/2` / git。

---

## 三、没查清（远端数据缺什么）

以下全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里**没有**（逐条已确认，⛔ 未猜）：

1. `GachaEvent.CurrentContainer` / `CurrentOffer` / `Offers` —— 箱子价格、票数、掉落表、`CratesSinceLastLegendary` 的上限与当前值。
2. `SpecialDropTable` 的 Special Items 列表 —— 奖励格要逐件 `GachaRewardContainer.Initialize(reward, current, max)`；本地没有 ⇒ 奖励池为空。
3. 活动截止时刻 —— `TimerDisplay` 的目标时间（两处 `Ends in:` 都只能 `--`）。
4. 页签图标 —— `BaseParentEventWindow.CreateEventTab` 里 `LiveOpsAssetUtility.LoadAsset(事件, 0)` 的 sprite（本地没有 ⇒ 图标留空）。
5. 开箱视频 `Gacha Crate Opening.mp4` —— 在 `bundle_menus_assets_all/VideoClip/`，**未导入工程**；原版由 `VideoPlayer` 播。
6. 开箱本身 —— `GachaCratePanel.OpenCrateButtonClick` → `ContainerOfferEvent.TryPurchaseOffer`（服务器）；本地没有后端 ⇒ 点击只出声。
7. `GachaEventTab.Initialize` 的后续刷新（`scheduledRefresh` / `SetEventTab` 的 `CurrentOffer` 到期调度）—— 没有事件对象 ⇒ 未接线（页面是静态空态）。

---

## 四、顺手发现（交调度台分流）

1. 🔴 **`WhereToGetCardsPopup`（队列 3479–3487）比整个活动窗族都低**（`ParentEventWindow` 3610–3629 · 本窗 3630–3659 · `EnergySinglePlayerOnlyEventWindow` 3935–3945）—— 任何活动窗若**按原版**打开它，弹窗都会被自己盖住（可视上「点了没反应」）。本窗用 `ToBackground` 物理让位兜住了；**`ParentEventWindow` / `EnergySinglePlayerOnlyEventWindow` 将来接 `Where get cards` 时会撞同一面墙**（需要在各自窗口处置，或统一给弹窗更高一档）。
2. ⚠️ **本地化键名跨域**：Gacha 的「Special Items / Complete! / CompletedTip」三条词条在 `Event_Description/ExpansionPass/Vault/*` 下（不是 `Gacha/*`）—— 这是**原版 prefab 的 `Localize.mTerm` 原文**，⛔ 别「修正」成 Gacha 前缀。
3. ⚠️ **两张 `40k_campaign_bar_*` 图**：原版 prefab 用的是 `40k_campaign_bar_fill color`（白色），本地导入的是 `40k_campaign_bar_fill`（灰色）；本轮进度填充是 **0 宽**（无数据）所以看不出差别，**将来数据接上时若颜色不对，先查这一处**（源图在 `bundle_atlasgroup_assets_all/Sprite/40k_campaign_bar_fill color.json`）。
4. ⚠️ `GachaCratePanel.buyTicketsButton` 在 prefab 里是 **null**（没有这个节点），`GachaEventTab.getCardsWindow` 是 `AssetReferenceTyped<WhereToGetCardsWindow>` ⇒ 说明「买票」入口在别的容器链、这一页本身没有。
5. ⚠️ `UIMask`（Viewport 的 Image）本地未导入，但它的 `m_Color.a = 0`（不可见）⇒ 本轮**不画、不出声**；裁切由 `ViewportClip` 承担（已如实写在代码注释里）。
6. ⚠️ `ProgressBar` 的 `Slider` 出厂值 `m_MaxValue=6 / m_Value=4.15` 与旁边的 counter 出厂字 `5/10` **对不上** —— 两者都是运行期覆盖的（`ProgressBar.SetProgress(current,max)`），⛔ 不能拿任何一组当真实数据。
7. ⚠️ `Header/Background`、`Army Icon`、`Help Icon Player`、`BackgroundTop`、`Player has all items`、`Completed`、`Video Background` 在 prefab 里**出厂 INACT**（本轮逐一按出厂态建出来、关着）；⛔ 别因为「看见了节点」就默认它们该显示。
