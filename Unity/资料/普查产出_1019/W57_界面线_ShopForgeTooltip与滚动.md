# W57 · 界面线执行代理 UI-3 — 商店/锻造/战役 tooltip 与滚动残余

> 执行代理 **UI-3** · 第五十七会话 · 2026-10-19。
> **只做不验**：⛔ 没跑 Unity / `*.Run` / player / `_run_8_checks.sh`，⛔ 没动 git，⛔ 没改正本与白名单外文件。
> 唯一跑过的检查 = **秒级类型检查**（`TMPDIR=/tmp/wf_ui3 bash d:/4/Unity/工具/typecheck.sh`，两次，均 **运行时 0 / 编辑器 0**）。
> 白名单 = `ShopTabPage.cs` · `ForgeTab.cs` · `CampaignTab.cs` · `GiveFeedbackPopup.cs` · `MenuScroll.cs` ·
> `BoosterInfoPopup.cs` · `WindowsManager.cs` · `CollectionData.cs` · `AllianceMemberTab.cs` ·
> `Editor/ShopScene.cs` · `Editor/MainMenuScene.cs`。

## 〇 · 结果一句话

**A 修好并补断言**（原版横向档 = `Center`，旧记录对、代码里的「hAlign Right」注释错）·
**B 三处接上官方词条**（锻造 / 商店 / 战役；战役的 `anchor/offset` 一并按原版改正），**第四处（布道所 = `MissionsTab`）在白名单外 ⇒ 停手，报告里给了逐字改法** ·
**C 代码侧已全部按原版**（`GiveFeedback` 早就是 50 · 没有一处生产调用点还在吃 48 的缺省），只补了 `MenuScroll` 的 doc 口径 ·
**D 白名单内 4 处润色已做**，其余 6 处在白名单外 **只报不改**（逐行给了现文）。

---

## 一 · 四件逐项结论

### A · `N72` 商店 `Available Counter` 对齐（**改代码 + 补断言**）

**判据（现跑原版字段，⛔ 不采信旧记录/旧注释任一方）**

```bash
python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Catalog Item Shop Container" --depth 4 --relative --no-sprite
# → Available Counter ... 'Available: 1/5' 字号=16.0 基准=39.0 auto[16.0~31.5] 对齐=Center/Midline 折行=0
```

`Catalog Item Shop Container` 同名**共 6 份变体**（`-7217504462698257110` + `…_-4936527347560911135` /
`…_-5786857933989083126` / `…_289869055517420590` / `…_3072966409823556505` / `…_4063768397534453934`），
**逐个现读全部 `对齐=Center/Midline`**（第一份字号是运行时 27.85，其余 16，但对齐全同）⇒ **不是「一个值 / 多套情况」的分叉**。

- **原版值 vs 我们值**：原版 `m_HorizontalAlignment = 2 (Center)` + `m_VerticalAlignment = 4096 (Midline)`；
  我们改前 = `MenuDraw.AlignRight(al, ar)`（`ShopTabPage.cs` 原 `:623`）⇒ 整串往右推 ≈ (315.33−18.87−文字宽)/2。
- **关键对位**：原版 `Available Counter` 的 x 是 **18.87..315.33**（0 高带 · y=476）；我们的 `CellAvail`
  （`Shell/ShopTabPage.cs:146`）= **`(18.87, 452, 315.33, 474)`** —— **x 逐值相同** ⇒ 「框中心」就是原版那个居中的落点。
- **改动**：
  - `Shell/ShopTabPage.cs:596` 注释：`hAlign Right` → **`Center`**，并写明判据与错因（旧施工图 `阶段二_商店_原版规格.md:143`
    的 `Right` 是**旧工具 hAlign 错位**留下的值）。
  - `Shell/ShopTabPage.cs:629-632`：**删掉 `MenuDraw.AlignRight(al, ar)`**（不调 `Align*` = `Label` 出厂按框中心摆整块，
    见 `Battle/Label.cs` 的 `RefreshBounds`）并留「⛔ 别改回 `AlignRight`」。
  - `Editor/ShopScene.cs:1585-1593`：新增断言「渲出来的中心 == `CellAvail` 中心」（容差 2px）。
    ⚠️ 断的是**原版档位**：`AlignRight` 会把中心推到 `x1+18.87+w/2`，与中心差 `(315.33−18.87−w)/2` ⇒ 咬得住。
- **旧记录状态**：`N72` 原文「我们 `AlignRight`」**成立**（改前 `:623`）；`ShopTabPage` 里那句「原版 hAlign Right」
  **不成立**（已就地订正）。施工图 `资料/阶段二_商店_原版规格.md:143` **仍写着 `Right`（白名单外，未改）**。

### B · `N73` 四个 tooltip 正文

| # | 落点 | 触发器 | 改前正文 | 原版判据 | 本批处置 |
|---|---|---|---|---|---|
| 1 | 锻造 `Help Icon`（`Shell/ForgeTab.cs`） | 在（`:598-604`） | 空白占位 `" "` | `EverguildTooltipTrigger.text = "MainMenu/Forge/Help"`（`localize=1`）· 该键**在 `Core/Loc.cs:2745` 的 `Table`**（中英都有值）· `tooltipAnchor=25` · `offset=(0,0,0)`（`MonoBehaviour_-4382542594159047011.json` 现读） | ✅ 改 `HelpTipBody` 为 **`Loc.T("MainMenu/Forge/Help")`**（`:568`，属性形式跟随 `Loc.Current`）；日志文案同步 |
| 2 | 商店 `BoosterInfo`（`Shell/BoosterInfoPopup.cs`） | 在（`BuildTooltipHit`） | 空白占位 `" "` | `text = "MenuShop/BoosterInfo/LegendaryTooltip"`（`localize=1`）· 该键**在 `Core/Loc.cs:2785` 的 `Table`** · `tooltipAnchor=0` · `offset=(0,0,0)`（`MonoBehaviour_-8635864616710430033.json` 现读） | ✅ 改 `TipBody` 为 **`Loc.T("MenuShop/BoosterInfo/LegendaryTooltip")`**（`:524`）；日志同步 |
| 3 | 战役 `Header info`（`Shell/CampaignTab.cs`） | 在（`AddInfoTooltip`） | `Tooltip.Show(" ", …)` | `text = "MainMenu/Campaign/Help"`（`localize=1`）· **`Table` 里没有**、但官方全表有（en+zh 10,854 条 / 13lang 资源 10,862 键，`Loc.T` 的第三档接住）· **`tooltipAnchor = 5（居中）`** · **`offset = (0,−80,0)`**（`MonoBehaviour_-4150665785838686835.json` 与 `…_6137946225118746367.json` 两颗同值） | ✅ 正文 `Loc.T("MainMenu/Campaign/Help")`；**anchor 25 → 5**、offset 零 → **`LayoutSpace.Px(-80)`（px→世界单位）**（`:1432-1434`）·「没有现读」那条注释已就地订正 |
| 4 | 布道所三卡 info（`Shell/MissionsTab.cs`） | 在（`AddInfoTooltip`，3 颗） | `Tooltip.Show(" ", …)` | 见下面 §三·1（该文件**不在白名单**） | ⛔ **未改**（停手 + 精确改法见 §三·1） |

**断言同步**

- ✅ `Editor/ShopScene.cs:2220-2222` + `:2279-2280`：把「`TipBody.Trim()==""` / 面板正文空」改成
  「`TipBody` 非空 = 官方词条」+「`Tooltip.ShownBody == TipBody`」。注释里「词条表在远端 CCD ⇒ 本地没有」已就地订正。
- ⛔ **`Editor/RewardsScene.cs:4403-4416` 与 `:4427` 现在会红**（白名单外，未改）：
  - `:4412` `CheckTrue(!string.IsNullOrEmpty(ForgeTab.HelpTipBody) && ForgeTab.HelpTipBody.Trim() == "", …)`
  - `:4427` `CheckTrue((Tooltip.ShownBody ?? "").Trim() == "", …)`
  ⇒ 两行都要改成「正文 == `ForgeTab.HelpTipBody` 且非空」（或等价的官方值断言）。**这是本批唯一留下的已知红**，
  同一批那两条注释（`:4403` / `:4408`）也一并过期。

**为什么能直接读词条**：官方 I2 表 2026-10-14 已落地（`数据/游戏数据/官方本地化_en+zh-CN.json`，
运行时 `CardPresentation/Resources/official_loc_13lang.json`）；本批现读 7 条键**全部有值**：

| 键 | EN（截断） | ZH |
|---|---|---|
| `MainMenu/Forge/Help` | Upgrade cards in your Collection… | 在你的收藏中升级张牌… |
| `MenuShop/BoosterInfo/LegendaryTooltip` | A legendary card is guaranteed… | 在最多每30个打开的增强包中… |
| `MainMenu/Campaign/Help` | Complete Missions to progress through the Campaign… | 完成布道所以通过你选择的军队的战役进行进步… |
| `Missions/Info/DailyMissionsHelp` | Complete Missions to earn Campaign Points… | 完成布道所以赚取你所偏好的派系的战役点数… |
| `Missions/Info/DailySkulls` | Get Skulls in battle by damaging the enemy Warlord… | 通过在战斗中伤害敌方战将来获取头骨… |
| `Missions/Info/WeeklyChallenge` | Complete Daily Missions to upgrade your weekly reward… | 完成每日布道所以升级你的周奖励… |
| `Missions/Info/DailyLogin` | Log in every day to collect a free reward… | 每天登录以领取免费奖励…（⚠️ **无任何 prefab 触发器用这条键**，见 §三·1） |

### C · `N29` / `N46` 滚动灵敏度残余

- **`GiveFeedbackPopup` 已经是 50**：`Shell/GiveFeedbackPopup.cs:541` `MenuScroll.TopAligned(ScrollR, ContentR.H, 50f)`；
  原版 `Give Feedback Popup menu > Window > Scroll Rect` = **50**（`资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md:62`）⇒ **已做，无需改**。
  （顺手复核了 prefab：同一棵树下 `Scroll Rect` 的 `h=0 v=1 mode=1 inertia=1 elasticity=0.1 decel=0.135` 与代码注释一致。）
- **`DeckSelectionPopup.Scroll`**：`Shell/DeckSelectionPopup.cs:599` = `100f`，原版 `Deck Selection Popup > … > Deck Scroll View` = **100**
  （`D_N29:41-42`）⇒ **代码值正确**；「宿主不打开」= 自检宿主（练习窗/排位窗）从不打开该弹窗 ⇒ **只是断言覆盖缺口，不是代码缺陷**。
  该文件不在白名单 ⇒ 未动（要补断言得先改宿主，属于另一件活）。
- **社交/档案 `ProfilePage.NewScroll`**：`protected`（`Shell/ProfilePage.cs:214-221`），但**5 个实际调用页全部显式传了原版值**：
  `AchievementsMenu.cs:189`=1 · `AvatarTab.cs:217`=50 · `BattleLogTab.cs:96`=50 · `RankedTab.cs:426`=10 · `TitleTab.cs:165`=50
  （`D_N29:54-57` + 表格）。⇒ **代码无欠账**；缺的只是「自检能读到」的只读口（白名单外，未做）。
- **全壳没有一处生产调用点还吃缺省 48**：`grep DefaultNotchPx` 只命中 `MenuScroll` 自身与 `ProfilePage` 的形参默认值；
  所有 `TopAligned` / `new MenuScroll` / `NewScroll` 调用点都显式传了值。
- ✅ **`Shell/MenuScroll.cs:99-103` doc 已订正**：把「= 原版值」的歧义写法拆成
  「**字段口径** = 原版 `m_ScrollSensitivity`」+「**传进来的值不一定是原版值**（`120` 是我们挑的，见 2026-10-18 裁定）」。
- **仍「查不到」的**：`MissionsTab` 原版没有 `ScrollRect`（D_N29 §五·1；现读 `MissionsTab.cs` 也**没有任何 `MenuScroll`**）⇒ 维持「没查到」，未编值。

### D · `TOOL-2` 顺手发现的文案问题（`W57_工具线_行号落地_Shell.md` §六）

**白名单内已改（4 处）**

| 文件:行 | 改前 | 改后 |
|---|---|---|
| `Shell/AllianceMemberTab.cs:922` | `Shell/RankedTab.cs` 的 `ScoreEmpty` 的 `ScoreEmpty` | `Shell/RankedTab.cs` 的 `ScoreEmpty` |
| `Shell/WindowsManager.cs:646` | `… 的 `=> WindowsManager.OpenByRef(prefabRef, create, closeAll);` = `=> WindowsManager.OpenByRef(prefabRef, create, closeAll);`）` | 只留一次 |
| `Shell/WindowsManager.cs:670` | `… 的 `= WindowsManager.PrefabRefChat;` = `= WindowsManager.PrefabRefChat;`）` | 只留一次 |
| `Shell/WindowsManager.cs:512` | `… 的 `可见性照原版 = `private``（**反引号未配对**） | `` ``可见性照原版 = `private` `` ``（双反引号内嵌单反引号，合法 code span） |

> 顺手：`Shell/WindowsManager.cs:755` 还有一处更早的 **``` ``` `Expansion Pass Tab` ```**（三反引号，markdown 配对破），
> 同属「只改注释」，已一并修成单反引号。其余文件里的 `的 `X` 的 `X`` 已用正则复扫，**白名单内 0 命中**。

**白名单外 6 处（⛔ 只报不改，逐行现文）**

| 文件:行 | 现文（片段） |
|---|---|
| `Shell/CardDetailPopup.cs:1531` | `… `Shell/DeckInfoPopup.cs` 的 `CheckOwnership` 的 `CheckOwnership`、` |
| `Shell/CardDetailPopup.cs:1568` | `（`Shell/CollectionWindow.cs` 的 `leftStyleButton` 的注释：…` —— ⚠️ 这处是「X 的注释」，**不是**重复词，按语义可保留 |
| `Shell/DeckInfoPopup.cs:1961` | `` … `DeckRuntime.cs` 的 `bool showX = n > 1;` `bool showX = n > 1;`）—— `` |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:457` | `… `Shell/AlliancePanelWindow.cs` 的 `lb.SetWrapping(false)` 的 `lb.SetWrapping(false)`）。` |
| `Shell/TopBar.cs:69` | `… `Editor/DeckScene.cs` 的 `TestLayout` 的 `TestLayout`）。` |
| `Shell/SettingsWindow.cs:1939` | `… `MainMenuRuntime.cs` 的 `Show()/Hide()` 的判据）` —— 语义通顺，**不属重复词**，可不动 |

> §六·2 的「同一个文件名出现两遍」全在白名单外（`DeckSelectionPopup.cs:57/355/477/1000` · `ChatTab.cs:170`），未动。

---

## 二 · 改动清单（本批 = 8 个文件，全部白名单内）

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/ShopTabPage.cs` | `:596` 注释订正（hAlign Center + 错因）；`:629-632` 删 `AlignRight`、留 Center 说明 |
| `Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs` | `:1585-1593` 新增「`Available Counter` 横向居中」断言；`:2212-2222` / `:2279-2280` 两条 tooltip 断言与注释改为「官方词条 / 非空」 |
| `Unity/MyGame/Assets/CardPresentation/Shell/ForgeTab.cs` | `:555-568` `HelpTipBody` → `Loc.T("MainMenu/Forge/Help")`（属性）+ doc 订正；`:600-601` 日志 |
| `Unity/MyGame/Assets/CardPresentation/Shell/BoosterInfoPopup.cs` | `:518-524` `TipBody` → `Loc.T("MenuShop/BoosterInfo/LegendaryTooltip")`（属性）+ doc 订正；`:578` 日志 |
| `Unity/MyGame/Assets/CardPresentation/Shell/CampaignTab.cs` | `:1412-1435` 正文接 `Loc.T("MainMenu/Campaign/Help")`；`anchor 25→5`、`offset → (0,−80/108,0)`、doc 订正 |
| `Unity/MyGame/Assets/CardPresentation/Shell/MenuScroll.cs` | `:99-104` `NotchPx` doc 口径订正（「字段口径」≠「我们传的值」） |
| `Unity/MyGame/Assets/CardPresentation/Shell/AllianceMemberTab.cs` | `:922` 去掉重复的 `ScoreEmpty` |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:512` 反引号配对；`:646` / `:670` 去重复串；`:755` 三反引号 |

**行尾**：全部 `Edit` 工具改，未用 `sed -i`；8 个文件 `git diff --numstat` 增删数都很小（≤18），**无整篇翻行尾**。

---

## 三 · 没查清的部分 / 白名单外还欠的

### 1. `MissionsTab`（布道所）四个 info —— **本批最大的一块外溢**

`Shell/MissionsTab.cs` **不在白名单**，⛔ 未改。现读代码只有 **3 颗** info（`AddInfoTooltip` 定义在 `:1582`）：

| 落点 | 我们的调用 | 原版触发器实读（`bundle_menus_assets_all/MonoBehaviour/*.json`） |
|---|---|---|
| 页头 `Mission Header > info`（`MissionsTab.cs:1463`） | `AddInfoTooltip(parent, inf, "info")` | `text = Missions/Info/DailyMissionsHelp` · `localize=1` · **anchor 15** · **offset (−100, 0, 0)** |
| 每日登录卡 `header > info`（经 `BuildCardHeader`，`:1571`） | `AddInfoTooltip(…, "Daily Login Bonus info")` | ⚠️ **不是词条**：`text = "Get Skulls By damaging the enemy warlord in battle. You will get a reward for every milestone you complete!"` · **`localize = 0`** · **anchor 10** · **offset (55, 0, 0)**（原版自己写死的一句英文，内容其实是骷髅卡那段 —— 疑为原版作者态拷贝，但**这就是原版行为**） |
| 每日骷髅卡 `header > info`（经 `BuildCardHeader`，`:1571`） | `AddInfoTooltip(…, "Daily Skulls info")` | `text = Missions/Info/DailySkulls` · `localize=1` · **anchor 10** · **offset (55, 0, 0)**（独立 prefab；页内 Small 变体同值） |
| 周常卡（`BuildWeekly`） | **我们没建 info 图标** | ⚠️ 原版 `Weekly Mission Container` **有** `header > info`：`text = Missions/Info/WeeklyChallenge` · `localize=1` · **anchor 10** · **offset：页内副本 40 / 独立 prefab 55**（铁律 5·c：先判用途再取值） |

**建议改法（给主对话/下一手）**：`AddInfoTooltip` 加两个形参（`term`/`literal`、`anchor`、`offset`），
三处调用各传上表的值；正文走 `Loc.T(term)`（官方全表兜底已具备）。⚠️ **登录卡那一颗按原版应是「写死英文 + `localize=0`」**，
若按词条 `Missions/Info/DailyLogin` 接，**等于自行「修正」原版**（该键在整个 `assets_full` 里**零触发器使用**）——
按铁律 11 建议照原版（写死那句），但这条**要主对话裁**。
**另外**：周常卡的 info 图标是我们**树里缺的节点**（不是正文空），需照原版补建。

### 2. `Editor/RewardsScene.cs` 两条断言会红（锻造 tooltip）

见 §一·B 末尾。`RewardsScene.cs` 不在白名单 ⇒ 未改；另 `:4403`/`:4408` 的注释也写「本地没有」。

### 3. `资料/阶段二_商店_原版规格.md:143` 仍写 `Right`

旧工具 hAlign 错位留下的错值；同一份文件 `:157` / `:409` 的其它说明仍然有效。白名单外未改（铁律 5 应由主对话订正）。

### 4. `Core/Loc.cs:2777` 与 `Shell/MissionsTab.cs:1578` 的过期注释

两处都还写着「`ForgeTab.HelpTipBody = " "`（故意留空）」，与现在的实现相反。白名单外未改。

### 5. 战役 info 的 `tooltipAnchor/offset` 判据来源

现读两颗 `MainMenu/Campaign/Help` 触发器**字段相同**（anchor 5 / offset (0,−80)），但我**没有**再把它们逐颗认领到
「哪一颗是 Campaign Tab 的 Header Info Button」——因为两颗同值，结论不受影响；若将来发现 Campaign Premium 面板上也有 info，需要再分一次。

---

## 四 · 顺手发现（只报不改）

1. 🔴 **`Shell/ExpansionPassEventWindow.cs:1387-1388` 的 tooltip offset 量纲疑似错了 108 倍**：
   调 `Tooltip.Show(body, LayoutSpace.FromPixel(...), 10, new Vector3(55f, 0f, 0f))` —— `Tooltip.Show` 的 `offset`
   与 `worldPos` **同量纲 = 世界单位**（`Core/Tooltip.cs` 的 `_pos = worldPos + offset`；`Pivot` 里 uGUI 档再 `× PxPerUnit`），
   画布 px 必须 ÷108（先例 = `BattleDriver.cs:21293-21296` 的 `/ TipPx`）。现传 `55` ⇒ 面板会被推 **≈5940 px** 到屏幕外。
   该文件白名单外，未改。
2. `Shell/WindowsManager.cs:755` 三反引号（已在本批顺手修，见 §一·D）。
3. `Weekly Mission Container` 的 info 图标：原版有、我们没建（见 §三·1）。
4. `Missions/Info/DailyLogin` 这条官方词条**全 `assets_full` 零触发器引用**（用
   `grep -rho '"text": "[^"]*Missions/Info[^"]*"'` 全表枚举：只出现 `DailyMissionsHelp`(2) / `DailySkulls`(6) / `WeeklyChallenge`(3)）。

---

## 五 · typecheck 结果

```
TMPDIR=/tmp/wf_ui3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

两次（`CampaignTab` 的 anchor/offset 改动之前与之后各一次）**均 0 / 0**，无「错误集中在别人文件」的情况。
