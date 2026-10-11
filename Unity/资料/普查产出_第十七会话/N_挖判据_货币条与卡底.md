# N · 挖判据 —— 货币条与卡底（只读诊断代理 · 2026-10-11）

> 只读：没改任何代码/正本，只写本文件。跑过的命令都是 `python -I` 读 JSON / 看图。
> 路径前缀：`AF = d:/2/新解包资源/assets_full` · `DF = d:/2/tools/decomp_full` · `MMC = AF/bundle_scenes_scenes_mainmenuwarpforge`
> · `MEN = AF/bundle_menus_assets_all` · `CP = d:/4/Unity/MyGame/Assets/CardPresentation`

## 结论（两条分着写）

- **③ 判据拿到了。** 原版 `Resource Counter Item` 的排布是 **图标在左、数字药丸在右**，而且**图标压根不参与布局** ——
  它的 `LayoutElement.m_IgnoreLayout = 1`，被 `HorizontalLayoutGroup` 整个跳过，所以它**保持出厂矩形留在左边**
  （左沿 = 格原点 **−8.5**，压在药丸左端上）；**药丸（`Resource Bar Background`）是原版件、不是我们自加的**，
  我们的 tint 也是对的。✅ **我们错的只有「次序」这一件**（外加由它带出的格宽/总宽偏大 60px/格）。
- **⑦ 只判得了一半，另一半判不了。** **尺寸档本身 100% 在本地**（烘在每个 prefab 的 `FlexibleLayoutSizeOption` + 根 RT 上，全表已取）；
  **「哪个事件用哪个 prefab」在事件自己的远程内容里**（`EventComponentReferenceType.MainMenuContainer = 35` → `LiveOpsAssetUtility.GetComponentReference(event, 35)`），
  本地 84 个 bundle **没有任何 liveop 事件数据**。但 prefab **名字**里带了三条**模式级**线索：**`Draft` / `Ranked` / `Skirmish Ranked` 都只有 `1x2`（满高）**。

---

## ③ 货币条 —— 子件序 / 背景图 / 分隔件

### ③·1 子件序：**图标在左（且不看布局），药丸在右**（判据三条，互不依赖）

| # | 判据 | 出处 / 实测读数 |
|---|---|---|
| a | `Resource Counter Item`（GO pid 224）**唯一的布局组**是 `HorizontalLayoutGroup`，`m_Children = [372, 371]`、`m_ReverseArrangement = 0`、`pad (L20,R1,T0,B0)`、`spacing 0`、`m_ChildAlignment = 0`(UpperLeft) | `MMC/RectTransform/RectTransform_359.json:22-31` · `MMC/MonoBehaviour/MonoBehaviour_206.json:12-26`（脚本 = **HorizontalLayoutGroup**，`bundle_Waprforge_monoscripts/MonoScript/MonoScript_-3229211799126679632.json`） |
| b | 🔴 **图标那一格带 `LayoutElement` 且 `m_IgnoreLayout = 1`** ⇒ uGUI 建 `rectChildren` 时**跳过它**（既不排位、也不算宽度） | `MMC/MonoBehaviour/MonoBehaviour_545.json:12`（`m_IgnoreLayout: 1`；脚本 = **LayoutElement**，`MonoScript_-8545018526213454877.json`）。语义判据（本仓已有）：`资料/已知的坑.md:2085-2090`（`!activeInHierarchy` **或** `ILayoutIgnorer.ignoreLayout` 都跳过）＋ 同族先例 `资料/历史/阶段二第3层_逐件记录.md:47`（`Clear filters` 就是被 `m_IgnoreLayout=1` 坑的） |
| c | **实测坐标**：药丸 `RT_372` `anchor(0,1) pivot(0,0.5) apos(**20**,−19) sz(141.51,38)` —— **恰好 = `pad.left` ⇒ 它就是布局排出来的**；图标 `RT_371` `anchor(0,0.5) pivot(0,0.5) apos(**−8.5**,0) sz(60,59)` —— **≠ 布局会算出的 161.51 ⇒ 它没被排**，左沿落在格原点**左边 8.5px**、右沿 51.5 ⇒ **与药丸左端(20..51.5)重叠** | `MMC/RectTransform/RectTransform_372.json:32-51` · `RectTransform_371.json:27-46` |
| d | **实况对照**（原版玩家实拍）：`[图标] (暗药丸+数字)`，图标在每个数字**左边** | `_tmp_view/play17/cmp_topright.png` / `cmp_topbar.png` 上半（用户参考视频帧） |
| e | **运行期确实会跑布局**（所以 (c) 的「没被排」不是「布局没跑」） | `LayoutGroupContentFixer.FixLayout` 逐个目标调 `LayoutRebuilder.ForceRebuildLayoutImmediate`（`DF/LayoutGroupContentFixer__FixLayout.c:45`）；该组件就在这一格上（`MonoBehaviour_516.json`，脚本 pid `5718195413707407283`） |

**由 (b)(c) 推出来的几何（可直接照做）**：格宽 = `20 + 药丸宽 + 1`（**不含图标那 60**）；
- 药丸：格内 x **[20, 20+药丸宽]**，**竖直居中**（高 38，格高 38.066）
- 图标：格内 x **[−8.5, 51.5]**，**竖直居中**（高 59 ⇒ 上下各溢出格 10.47px，容器高 71.165 装得下）

🔴 **我们现在的写法**：`CP/Shell/TopBar.cs:388` `itemW = ItemPadL + pw + **IconW** + ItemPadR`（把图标当布局子件）＋
`:418-420` `icon` 摆在 `pR .. pR+IconW`（药丸**右**边）。
⇒ 后果两条：**次序反了** + **每格宽 60px、四条共 240px 偏大**（容器右对齐 ⇒ 整条往左多伸 240px）。
（按我们的常量算：第一颗的药丸左沿落在 canvas **x=775.5**，而原版算式给出 **1015.5** —— 差 240 = 4×60。）

### ③·2 数字有没有背景图 —— **有，而且是原版件（⛔ 不是我们自加的）**

| 项 | 原版字段值 | 出处 |
|---|---|---|
| 名字 / 脚本 | `Resource Bar Background` / `UnityEngine.UI.Image` | `MMC/GameObject/Resource Bar Background.json:16-18` |
| sprite / Type | `40k_topmarquee_currency_display BW` / **`m_Type 1`(Sliced)** · `m_FillCenter 1` · `m_PreserveAspect 0` · `m_PixelsPerUnitMultiplier 1.0` | `MMC/MonoBehaviour/MonoBehaviour_409.json:35-47` |
| tint | **(0.33019, 0.26010, 0.31567, 1)** | 同上 `:16-21` |
| 自带九宫 | 贴图 44×39，中枢实测 **中灰 (116,116,116)**、四角透明 | `CP/Resources/Art/ui_menu/40k_topmarquee_currency_display_BW.png`（`(22,19)=(116,116,116,255)` · `(2,2)=(255,255,255,1)`） |
| 它自己的布局 | `HLG pad(L29,R9) MiddleLeft`，唯一子件 = TMP `Resource QuantityText`（`RT_370`：`apos(80.755,−19) sz(103.51,38)` ⇒ 文字左沿 = 药丸内 29 = **`pad.L`**）| `MMC/MonoBehaviour/MonoBehaviour_207.json:12-18` · `RectTransform_370.json` |

🔴 **那 29px 的左内衬就是给图标留的位**（图标右沿 51.5 ≈ 药丸左沿20 + 29 + 一点）—— 两条判据自洽。
✅ **我们的 tint 是对的**（实测我们自己渲出来 (33,25,31)，= 116/255×(0.330,0.260,0.316)×255 ≈ (38,30,37)，同一档）——
`_tmp_view/play17/S1_menu.png`（2560×1440，画布 x1726 / y20..45 采样）。
⇒ **观感上「原版数字是浮在背景上」只是因为它够暗**（≈(38,30,37) 压在顶栏深红底上），**不是没有底框**。

### ③·3 四条之间有没有「分隔件」 —— **没有**

- `Resources Bar`（`RT_1187`）**只有 1 个子节点** = `Resources Container`（`RT_1427`）——`MMC/RectTransform/RectTransform_1187.json:22-27`
- `Resources Container` **0 子节点**（运行时实例化）；它自己的 `HLG` = `pad(L0,R5) spacing **44** MiddleRight` + `CSF HorizontalFit=2` ——`MMC/MonoBehaviour/MonoBehaviour_2279.json` / `_2106.json`
- 全包 `GameObject/` 里名字含 `separator|divider` 的**一个都不在货币条这棵树上**（只有导航栏的 `Separators Left/Right`）
⇒ **四条之间唯一的「分隔」就是那个 `spacing = 44`**（以及图标往左溢出 8.5 带来的视觉间隙）。⛔ 别去加一条分隔线。

---

## ⑦ 模式卡尺寸档 —— 映射能不能从本地判

### ⑦·1 能判的一半：**尺寸档烘在 prefab 上，全表本地可读**（`MEN` 全量扫描）

| prefab | 根 RT `sizeDelta` | `FlexibleLayoutSizeOption` | 挂的容器脚本 |
|---|---|---|---|
| `Base Game Mode Container 1x1` | 535×**414** | (1,1) | `LiveopMenuContainer` |
| `Base Game Mode Container 1x1 Claim` | 535×414 | (1,1) | `LiveopMenuContainer` |
| `Base Game Mode Container 1x1 Claim AutoCollect` | 535×414 | (1,1) | `MissionMenuContainer` |
| `Base Game Mode Container 1x2` | 535×**848.8** | (1,2) | `LiveopMenuContainer` |
| `Base Game Mode Container 1x2 No Timer` | 535×848.8 | (1,2) | `LiveopMenuContainer` |
| `Base Game Mode Container 1x2 Raid or Army Release` | 535×848.8 | (1,2) | `RaidOrArmyReleaseMenuContainer` |
| 🔴 **`Draft Game Mode Container 1x2`** | 535×848.8 | (1,2) | `DraftMenuContainer` |
| 🔴 **`Ranked Game Mode Container 1x2`** | 535×848.8 | (1,2) | `RankedMenuContainer` |
| 🔴 **`Skirmish Ranked Game Mode Container 1x2`** | 535×848.8 | (1,2) | `RankedMenuContainer` |
| `Reward Event Container 1x1` | 535×414 | (1,1) | `RankedArmyBonusContainer` |
| `Main Menu Offer Container 1x1` / `Carousel 1x1` / `Static Image 1x1` / `1x1 Premium_…` | 535×414 | (1,1) | `MainMenuOfferContainer` |
| `Main Menu Offer Container Static Image 1x2` | 535×848.8 | (1,2) | `MainMenuOfferContainer` |
| `Game mode deck selector`（`SelectDeckGameModeMenuContainer`） | **505.93×802.67** ⚠️ 不是 535×848.8 | **(1,2)** | `SelectDeckGameModeMenuContainer` |
| `Button Skirmish` | 350×76 | 无 | —（**不是卡**，是按钮；§九·2 已记） |

出处：逐个 GO → `m_Component` → `MEN/RectTransform/*.json` + `MEN/MonoBehaviour/*.json` 直读（脚本类名经
`AF/bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pid>.json` 的 `m_ClassName` 解出）。
✅ 尺寸与 `Content` 的 `FlexibleGridLayout itemSize = 535×414.4`（rows=2）**对得上**（1x2 = 占两行）。

### ⑦·2 🔴 **判不了的那一半：事件 → prefab 的绑定在远端**

- 链路（反编译实读）：`MainMenuWindow.InstantiateLiveOp` → `ContainerBuilder.MakeContainer(Event, **EventComponentReferenceType ContainerRef**, anchor)`
  —— `DF/MainMenuWindow__InstantiateLiveOp.c:18-20`（第 2 个实参 = `data+0x1c`）。
- `LiveopsMainMenuData` 的 `ContainerRef` **是枚举、不是资产引用**：`dump.cs:54187`（字段 0x1C，类型 `EventComponentReferenceType`）；
  `dump.cs:125613-125624` 里 `MainMenuContainer = 35`（**主菜单容器只有这一个槽**）。
- 那枚举值再经 `LiveOpsAssetUtility.GetComponentReference(event, 35)` **去事件自己的数据里取**：
  `DF/Everguild.LiveOps.LiveOpsAssetUtility__GetComponentReference.c:10-13`（读 `event+0x38` 里那张表）；
  取不到时 `GetAssetReference` 会 `Debug.LogWarning` 返回 null（`…__GetAssetReference.c:22-35`）⇒ **找不到就整颗不建**。
- **本地没有那张表**：`resources/TextAsset` 只有 5 个无关 txt（`BillingMode.txt` 等）；84 个 bundle 里名字含 `liveop` 的只有
  `bundle_liveopsicons_assets_all` 与 `bundle_liveopsmenuimages_assets_all`（**两个都只有 Sprite/Texture2D，没有数据**）。
- 事件本身也来自远端：`MainMenuWindow.RefreshLiveOpContainers` 从 `LiveOpsManager.GetEventsForDisplayingInMenu()` 拿
  （`DF/MainMenuWindow__RefreshLiveOpContainers.c:66,162`）；调试窗也只是读这个列表（`dump.cs:104239-104246`）。

### ⑦·3 结论与「还差什么」

- ✅ **能判**：每个 prefab 的**尺寸档**（上表）；**名字里带模式名的三条 = 硬判据** ⇒
  **`Draft` / `Ranked` / `Skirmish` 三个模式在首页上是满高 1x2**（与用户参考图上「小冲突是满高」**逐条吻合**）。
- ⛔ **判不了**：**Practice / Tutorial / Campaign 这类没有同名 prefab 的模式用哪一档**。
  证据方向 = **同一个模式会有两个尺寸的图**（`Container Image Practice 1x1` **和** `1x2` 都在，
  `AF/bundle_liveopsmenuimages_assets_all/Sprite/`）⇒ **尺寸是「这一次活动的配置」，不是模式的固有属性**。
- **还差什么**（写清楚，别猜）：**那份 liveop 事件内容**（每个事件 `assetReferences[35]` 指向哪个 prefab）。
  落点 = CCD 远端（关服）⇒ 与 `资料/主菜单_原版规格.md` §9·3 记的「映射在服务端」**同一条**，本件只是把它**从「感觉」升级成逐行判据**。

---

## 搜过哪几个包 / 搜过哪些词（⛔ 报「没有」之前先有这张表）

| 报的「没有」 | 搜过哪几个包 | 搜过哪些词 |
|---|---|---|
| 货币条四条之间**没有分隔件** | `AF/bundle_scenes_scenes_mainmenuwarpforge`（GO 697 个 + RT 592 个全名单）· `AF/bundle_menus_assets_all` · `AF/bundle_menusharedresources_assets_all` | 文件名 `separator` · `divider` · `line` · `resource` · `currency` · `counter`；并顺 `RT_1187.m_Children`（1 条）+ `RT_1427.m_Children`（0 条） |
| `raidMedals` 的币种小图标**本地不存在** | `AF/*/Sprite/`（**全部 bundle**，按文件名） | `topmarquee`（共 6 张：`blackstone` `crystal` `display BW` `energy` `gold` `ticket`）· `medal`（只有 `40k_Achievements_icon_medal1..5`）· `currency` · `raid` · `league` · `rank` |
| 本地**没有 liveop 事件数据** | `AF/resources`（**唯一有 `TextAsset` 的包**）· 84 个 bundle 的**目录名**逐个 · `AF/**/*.json` 全文 | 目录名 `liveop`（只有 icons / menuimages 两个，均纯图）· `SharedEventData` · `handlerData` · `liveOpsEvent`（只在 `globalgamemanagers/MonoScript/MonoScript_3810.json` 命中一次 = **脚本定义**，不是数据） |
| 模式卡的 prefab 变体**就这些** | `AF/**/GameObject/`（所有包） | 正则 `(Game Mode Container\|Offer Container\|Container 1x)` |

---

## 顺手发现（看见但没查完 / 建议调度台分流）

1. ✅ **W374 §六·1 那条「本地缺 4 张币种小图标」已经关掉了**（不是我这轮干的，是别的批补的）：
   `CP/Resources/Art/ui_menu/` 里 `40k_topmarquee_currency_{blackstone,crystal,energy,ticket}.png` **四张都在**，
   而 W374 说的「只有 2 张」已经不成立 ⇒ 那条「`Section("图：一张都不能少")` 会红」的预期**应当重核**（我没跑 Unity，只能报事实）。
2. 🔴 **`raidMedals` 的图标**照 W374 那条「同族命名规律」**在本包里根本不存在**（§搜过哪几个包那张表）——
   不是「没找到名字」，是**这个精灵资产不在客户端**（只剩 6 张 `topmarquee`）。
   ⛔ 建议把 W374 §六·2 的措辞从「没查到」升级成「**命名族穷举后不存在**」，并保留「那只画药丸+数字 + `Debug.LogWarning`」的做法。
3. 🟠 **我们的资源条比原版宽 240px**（③·1 末段）—— 这条**不是观感问题**，它会连带把四条整体左移；
   修次序时必须同时把 `itemW` 里的 `IconW` 去掉，否则改完还是对不上（`CP/Shell/TopBar.cs:388`）。
4. ℹ️ `Resource Counter Item` 的出厂图标精灵是 `UI_Button_Round_background`（`MB_399.m_Sprite`，
   `m_FileID 7 / pathID 2856960761010138393`），运行期必被 `UIResourceCounter.Initialize` 换成币种图
   （`DF/UIResourceCounter__Initialize.c:37` 紧随实例化）—— W374 §七·5 的结论（不画它）**复核仍成立**。
5. ⚠️ `Game mode deck selector` 这一格**查到了但存疑**（已填进 ⑦·1 表）：它 `FlexibleLayoutSizeOption = (1,2)`，
   可是根 RT 是 **505.93×802.67**，**不是** 535×848.8 ⇒ **它多半不是首页 `Content` 那个格子里的件**（是选牌组界面自己那套布局）。
   ⛔ **「为什么不是 535」没查清**，别拿它当首页模式卡的尺子。
6. ℹ️ **③ 那条「次序」是我这轮唯一一条实打实的偏差**，且**判据全在本地**（不需要真 Play 也不需要远端）——
   可以立刻派活改：`CP/Shell/TopBar.cs:388`（去掉 `IconW`）+ `:418-420`（图标改摆药丸**左边**、格内 x `[−8.5,51.5]`、竖直居中）。
