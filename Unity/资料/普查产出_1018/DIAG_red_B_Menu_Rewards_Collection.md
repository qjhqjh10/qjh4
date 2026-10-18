# 批次 B · 自检红诊断（MainMenu 4 / Rewards 7 / Collection 4）

> 只读诊断代理 · 2026-10-18 · **一个 `.cs` / 数据 / 正本都没改**（本文件是唯一产物）。
> 红线：⛔ 没跑 Unity · ⛔ 没动 git 写操作 · ⛔ `d:/2/**` 只读。
> 判据来源（逐条现核，**不采信断言文案里的自述**）：断言源码三份 `Editor/*.cs` ·
> 原版 prefab 字段（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 的 `RectTransform/` `MonoBehaviour/` `GameObject/`）·
> uGUI 源码 `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/` ·
> 历次日志（`_tmp_view/MainMenuScene_sync.log` 14:10 · `RewardsScene_sync*.log` 14:11–14:14 · `CollectionScene_sync*.log` 14:15–14:16 —— **全部 0 失败**）·
> HEAD = `4995622`（第五十三会话，2026-10-18 20:10）。

---

## §一 逐条判定

| # | 宿主:行 | 断言原话（短） | 判定 | 证据（文件:行 / 提交 / 原版字段） | 该照哪个判据 | 建议修法 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `MainMenuScene.cs:4854`（log `menu.log:23190`） | `Selected Item Panel > Avatar Name` **不折行**（原版 0）—— 实得 **1** | **γ 本批回归** | 根因 = HEAD 新增的 N55「面翻转重放」：`Battle/Label.cs:1433` `if (_fitValid && !_fitReplaying && _fitFace != ActualFace) ReplayFitForFace();` → `Label.cs:1224-1243` 重跑 `SetAutoFitBox`（`Label.cs:1124`）→ 其内部 `SetWrapWidth`（`Label.cs:522`）→ `TmpFont.cs:527` **无条件 `textWrappingMode = Normal`**。调用侧：`AvatarTab.cs:272-275` 建时 `face: LatinUi` + `wrap:false` + `SetWrapping(false)`；`AvatarTab.cs:307`/`:439` `Refresh()` → `SetText(中文头像名)`（`ProfileData` 走 `Loc.BulkOr("Avatars/…")`，`Resources/official_names.json` 的 `Avatars/Aggressor Sergeant = 侵略者士官`）⇒ 面 LatinUi→Cjk ⇒ 重放 ⇒ 折行被冲回 1。上一轮 14:10 `MainMenuScene_sync.log:28164` = ✓（现在 = 0） | 原版 `Avatar Tab > Selected Item Panel > Avatar Menu Item > Avatar Name` `m_TextWrappingMode = 0`（波 8 · A62 表 A，`资料/普查产出_1007/波8_Label折行族.md`） | **改实现**：`ReplayFitForFace` 里先记 `var mode = _tmp.textWrappingMode`，跑完 `SetAutoFitBox` 后写回并 `ForceRelayout()`（根子 = 重放不该改「调用方显式设过的折行档」）。⛔ 别在 5 个调用点后面再补 `SetWrapping` —— 那只治这 5 处 |
| 2 | `MainMenuScene.cs:5118`（`menu.log:24360`） | 改名窗输入框 `Text` = 原版**第三档 3**（`PreserveWhitespaceNoWrap`）—— 实得 **1** | **γ** | 同 #1 根因。`ProfileTab.cs:903` 建完即 `SetWrappingMode(3)`；`OpenNameWindow` 末尾 `ProfileTab.cs:1014` `RefreshNameField()` → `ProfileTab.cs:1028-1031` `SetText(玩家123)`（`Loc.cs:2004` `MainMenu/Profile/DefaultPlayerName = 玩家123`）⇒ LatinCardBody→Cjk ⇒ 重放 ⇒ **3 被冲成 1**；测试里 `MainMenuScene.cs:5099` 又 `UiSetName("Test Commander")`（Cjk→Latin 再翻一次） | 原版 `Choose Name Input Field > Text Area > Text` `m_TextWrappingMode = 3`（TMP 给单行输入框那一档；`Label.cs:632-665` 有判据链） | 同 #1（一处修，5 条一起回绿） |
| 3 | `MainMenuScene.cs:5133`（`menu.log:24534`） | `Info Section without Alliance > Player Name` **不折行**（原版 0）—— 实得 **1** | **γ** | 同 #1 根因。`ProfileTab.cs:555-556` 建时 `LatinUi` + `wrap:false` + `SetWrapping(false)`、文本是中文名（`_fitFace` 记 Cjk）；测试 `MainMenuScene.cs:5099` 改成 `"Test Commander"`（拉丁）⇒ Cjk→Latin ⇒ 重放 ⇒ 1；（`:5107` 再还原中文，又翻一次） | 原版同节点 `折行 = 0`（A62 子表 A · A10/A11） | 同 #1 |
| 4 | `MainMenuScene.cs:6874`（`menu.log:34791`） | 排行榜·阵营选择 滚动灵敏度 = 原版 **1.0** px/格 —— 实得 **120.000** | **α（附条件，见 §二·B）** | 实现：`LeaderboardWindow.cs:461` `MenuScroll.LeftAligned(ArmySelR, contentW, 120f)`，同行注释自陈「2026-10-18 主对话定 … ⛔ 不是原版值，是我们挑的」；W16 批（`资料/普查产出_1018/W16_横向滚轮步长.md` §一 #7）在 **HEAD `4995622`** 里把 `1f → 120f`。原版值：`D_N29_滚动灵敏度逐视图.md` 该视图 = **1.0**；语义 = **px/格**（uGUI `ScrollRect.cs:675-676` `position += delta * m_ScrollSensitivity`，Windows 一格 `delta = 1`；`MenuScroll.cs:93-108` 同口径，A1543 已按 100/50 验过）。⛔ **不是**「`DefaultNotchPx` 默认值压过逐视图值」—— `DefaultNotchPx` 仍是 **48**（`MenuScroll.cs:97`），120 是**逐处显式实参**。上一轮 `MainMenuScene_sync.log:39700` = 1.00 ✓ | 原版 **1.0**（D_N29）；120 = 我们挑的。**铁律 11**：偏离原版需用户拍板 —— 目前只有「主对话定」，无用户原话 | ① 若用户认可 120：改断言为 `120f`，并把文案改成「**我们挑的 120 px/格**（原版 `m_ScrollSensitivity=1.0`，照传 = 一格 1px 滚不动）」，同时按铁律 5 订正 `MenuScroll.cs:103` 那句「**唯一一处**非原版值」（现在是 **8 处**：主菜单模式卡 + 本批 7 处，其中 DailyRewardPopup 无断言）。② 若用户要原版：把 6 处实参回 `1f`（那 6 条断言本来就对 ⇒ 改判 β/γ） |
| 5 | `RewardsScene.cs:2228`（`rewards.log:6736`） | 领奖窗灵敏度 = 原版 1.0 —— 实得 **120.00** | **α（附条件）** | `RewardWindow.cs:1121-1122` `new MenuScroll(..., 120f)`（W16 #6，同一批注释口径）；原版 `Reward Window > Content > Scroll View` = 1.0（D_N29:49）。上一轮 `RewardsScene_sync3.log:6982` ✓ | 同 #4 | 同 #4 |
| 6 | `RewardsScene.cs:3459`（`rewards.log:14394`） | 锻造页·阵营条 = `Forge Army Selector` 1.0 —— 实得 **120.00** | **α（附条件）** | `ForgeTab.cs:443`（W16 #2）；原版 `Forge Tab > Forge Army Selector` = 1.0（D_N29:48 邻行 / W13 §三表 #4） | 同 #4 | 同 #4 |
| 7 | `RewardsScene.cs:3461`（`rewards.log:14409`） | 锻造页·奖励轨道 = `Rewards Scroll View` 1.0 —— 实得 **120.00** | **α（附条件）** | `ForgeTab.cs:399-401`（W16 #1）；原版 = 1.0（D_N29:48） | 同 #4 | 同 #4 |
| 8 | `RewardsScene.cs:5248`（`rewards.log:19939`） | 战役页·阵营条 = `Campaign Army Selector` 1.0 —— 实得 **120.00** | **α（附条件）** | `CampaignTab.cs:208`（W16 #3）；原版 = 1.0（D_N29 / W13 §三 #5） | 同 #4 | 同 #4 |
| 9 | `RewardsScene.cs:5250`（`rewards.log:19954`） | 战役页·轨迹 = `Campaign Track` 1.0 —— 实得 **120.00** | **α（附条件）** | `CampaignTab.cs:372`（W16 #4）；原版 = 1.0（D_N29:47） | 同 #4 | 同 #4 |
| 10 | `RewardsScene.cs:10901`（`rewards.log:42299`） | `Campaign Header/Title` 折行 = 0 —— 实得 **1** | **γ** | 同 #1 根因。`CampaignTab.cs:244-246` 建时 `face: LatinUi`、`:271` `SetWrapping(false)`；`:818` `_title.SetText(Loc.T("Armies/" + a))` → 中文阵营名 ⇒ 面翻 ⇒ 重放 ⇒ `Normal(1)`。上一轮 `RewardsScene_sync3.log:42545` ✓ | 原版 `Campaign Header/Title` `m_TextWrappingMode = 0`（`menu_dump … "Rewards Base Submenu Variant" --depth 6`，A303②） | 同 #1 |
| 11 | `RewardsScene.cs:10911`（`rewards.log:42340`） | `Campaign Header/Points` 折行 = 0 —— 实得 **1** | **γ** | 同 #1 根因。`CampaignTab.cs:277-288` 建时 `LatinCardBody`、`:293` `SetWrapping(false)`；`:834` `_points.SetText("战役点数：" + N)`（`Loc.cs:2165` `MainMenu/Campaign/Points = 战役点数：{0}`）⇒ 面翻 ⇒ 重放 ⇒ 1 | 原版同节点 `折行 = 0`（同一条 MB） | 同 #1 |
| 12 | `CollectionScene.cs:2772`（`collection.log:8953`） | 关闭钮命中区档 = `QDIHit` **3123**（字面量钉死，且要在压暗层 3120 之上）—— 实得 **3129** | **α** | HEAD 把 `DeckInfoPopup` 的队列梯子从**四档**重排成**十档**：`DeckInfoPopup.cs:207-209` `QDI=3120 … QDIPanel=3123 … QDIText=3128, QDIHit=3129`（旧值 `QDIHit=3123` 见 HEAD diff `- public const int QDI = 3120, QDIRow = 3121, QDIText = 3122, QDIHit = 3123;`）；理由 = 压暗/红底/Info Panel 三层全挤 3120、行内三层全挤旧 `QDIRow` ⇒ 谁盖谁随机（`DeckInfoPopup.cs:170-206` 的整段注释；另见 `资料/普查产出_1018/W_DeckInfoPopup_修复.md`、`R_DeckInfoPopup_对账.md`）。上一轮 `CollectionScene_sync2.log:8800` ✓。原版本窗**没有渲染队列**这一层（原版靠 `m_Children` 兄弟序），队列号是我们的等价复现 ⇒ 断言的本意是「**字面量钉死 + 命中区严格高于压暗层**」 | 「命中区 queue > 压暗层 `QDI`(3120)」这一条**仍然成立**（3129 > 3120）；字面量要跟当前梯子 | 改断言字面量为 **3129**（保留「⛔ 不写常量名」的灭自证写法）；顺手按铁律 5 订正残留的 3123 文案：`CollectionScene.cs:96`（`HitZFront` 注释）、`:2784`、`:3614`、`:5688`、`:5700`，以及 `DeckInfoPopup.cs:96` 引用 |
| 13 | `CollectionScene.cs:4594`（`collection.log:18423`） | 第 1 格中心 y = **347.9**（155.9 + 384/2）—— 实得 **328.76** | **α** | HEAD 的 W11 把**卡**从「整格 384」改成「格高 − 张数条 38.272」：`CollectionWindow.cs:225-242` `CardsCounterH = CardsCntMaxY(0.09966714) × CardsCellH(384) = 38.272`、`CardsCardRect`；`:1091` 缩放改按 345.728；`:1132-1134` `v.SetPose(Local(parent, cardR…))`。而 `win.CardsCells` 存的是 **CardView 的 transform**（`CollectionWindow.cs:1170` `CardsCells.Add(v.transform)`）⇒ 断言量到的是**卡块中心**：347.9 − 38.272/2 = **328.764**（与实得 328.76 逐位吻合；x 不变所以 `:4592` 仍绿）。W11 的判据 = 用户 2026-10-18 报「张数与卡牌重叠」（`资料/普查产出_1018/W11_卡组编辑计数重叠.md`）；原版 `/Counter` 上锚点 = `CardsCntMaxY`，卡只占它上面那一段 | 原版：卡块 = 格（384）减去 `/Counter` 占的 38.272；卡块中心 **328.764** | 改断言：期望 **328.76**（±1.5），文案改成「**卡块**中心 y（= 格心 347.9 − `CardsCounterH/2`）」；若要继续钉格心，另取格节点（`CardsCellRect(0)` 的常量）而不是 `CardsCells[0]` |
| 14 | `CollectionScene.cs:9196`（`collection.log:53712`） | `★ A1212/A1226 Cost Text`：框高 = **47.88px** —— 实得 **49.66** | **α** | 实现：`DeckInfoPopup.cs:1934-1937` `boxH: 49.66f`（2026-10-18 P1 批改，注释 `:1927-1933` 明写「改前 47.88 → 按报告口径 49.66」）。**原版链现核**（bundle_menus_assets_all）：`Deck List/Content` 的 GridLayoutGroup `cellSize = 360×58`（MB `-2408322734029764184`，GO `5907272997049108904`）⇒ 行高 **58** ⇒ `Background` 高 58（RT `6569929413234690472`，anchors y 0..1、sd 0）⇒ `Cost Image` 高 58+6=**64**（RT `4517749637251500456`，anchors y 0..1、`m_SizeDelta.y = 6`、ARF 1:1 高控宽）⇒ `Cost` TMP 高 `0.776 × 64 = 49.664`（RT `-3142486381871265368`，anchors y `0.112→0.888`、sd.y≈0）。47.88 的来源 = `RM_原版TMP框尺寸.md` 表 B 的 **55.7 行高**（= `Deck Editing Menu` 那一支：`DeckRuntime.cs:252-283` 的 `RowCostS = 61.7 = 55.7 + 6`）。2026-10-18 的 `R_DeckInfoPopup_对账.md:35` 也写 **49.66**。上一轮 `CollectionScene_sync2.log` 未红（旧框 40×40 时断言钉的是 `boxH` 字段 47.88） | 原版 **49.66**（本 popup 的 58 格）；47.88 属**另一棵树**（编辑窗的 55.7 格） | 改断言期望 **49.66**；并在 `RM_原版TMP框尺寸.md` 表 B #38 就地加订正注（「55.7 是 Deck Editing Menu 那一支；Deck info Popup 是 58 ⇒ 49.66」，铁律 5） |
| 15 | `CollectionScene.cs:9263`（`collection.log:54667`） | `★ A1212/A1226 Count`：框宽 = **原版有效框 44px** —— 实得 **22.87** | **α** | 实现：`DeckInfoPopup.cs:1978-1981` 框 `x+351.60−22.87 … x+351.60`（22.87 = 原版 **`LayoutElement.m_MinWidth = 22.87`**，MB `-8067466461408032344`；同 GO 的 `Count` TMP RT `-327133057421637208`：anchors 0,0、**`m_SizeDelta = (0,0)`** ⇒ 宽由布局组撑开，**不是固定框**）。**44 没有原版出处**：`RM_原版TMP框尺寸.md:91` 把 44 列在「**我们**」那一列（原版有效框列写 22.87），44 = 我们旧框 `x+308→x+352`（`R_DeckInfoPopup_对账.md:37`「框宽 44 vs 22.87」）。上一轮该条绿是因为旧实现就是 44 | 原版字段 = **`LayoutElement.m_MinWidth 22.87`**（`Count` 的框宽在 prefab 里**没有**固定值）；⛔ 「原版有效框 44」这个说法不成立 | 改断言期望 **22.87**，文案改成「= 原版 `LayoutElement.m_MinWidth`（**框由 `Text fill` 的 HorizontalLayoutGroup 撑开**，⛔ 不是 `m_SizeDelta`）」；真布局宽待 Unity 量（见 §三·1） |

**小计：γ 5 条（#1/#2/#3/#10/#11，同一根因）· α 10 条（#4–#9 同一批 · #12 · #13 · #14 · #15）· β/δ 0 条。**

---

## §二 根因归并（5 组）

### A. 5 条折行红 = 一个实现缺陷（γ）—— `Label` 的 N55 面翻转重放把「调用方显式设过的折行档」冲回 `Normal`

- **链条（三行代码）**：`Battle/Label.cs:1433`（`SetTextTmp` 发现面变了）→ `Label.cs:1224-1243` `ReplayFitForFace()` → `Label.cs:1124` `SetAutoFitBox()` → `Label.cs:522` `SetWrapWidth()` → `Core/TmpFont.cs:527` `t.textWrappingMode = TextWrappingModes.Normal;`（**无条件**）。
- **为什么以前没事**：全仓既有写法是「`SetAutoFitBox` → 紧跟 `SetWrapping(false/3)`」（`MenuScroll` 那条 A212 收口）。N55（HEAD `4995622`）新增的重放发生在**之后**的任意一次 `SetText` 上，于是把那次显式设置抹掉。**只有「建完之后文本会翻面（拉丁⇄汉字）」的件中招** —— 5 条红全是这一类：
  - 中→拉：头像名 `侵略者士官`（建时 face=LatinUi）· 玩家名 `玩家123`（改名窗/档案页）· 战役 `Title`（`Armies/*` 中文）· 战役 `Points`（`战役点数：`）。
  - 反方向（拉丁→中）也中招：测试 `UiSetName("Test Commander")` 后再还原。
- **同一根因的旁证**：`Placeholder`（同族、文本从不改）= 绿；网格 `Avatar Name`（期望 1）本来是 1、被冲成 1 ⇒ 也「绿」（**假绿**，没有鉴别力）。
- **修法（一处）**：`ReplayFitForFace` 保存 `_tmp.textWrappingMode` → 跑 `SetAutoFitBox` → 写回 + `ForceRelayout()`。改完 **5 条应同时回绿**；并建议补一条断言：面翻转后 `WrappingMode` 与翻转前一致。

### B. 6 条灵敏度红 = 同一批「120 px/格」刻意改动（W16），断言没跟（α，需用户确认偏离）

- 不是「统一默认值」泄漏：`MenuScroll.DefaultNotchPx` 仍是 **48**；120 是 **8 处逐处显式实参**（`MainMenuRuntime.cs:2200` N66 + W16 的 7 处，见 §一 #4–#9 与 `DailyRewardPopup.cs:257`）。
- 原版语义已核实 = **px/格**（uGUI `ScrollRect.cs:675`；`MenuScroll.cs:93-108` 同口径）。原版这些视图 = **1.0** ⇒ 真按 1.0 传，一格走 1px。
- 🔴 **6 台原版 ScrollRect 已逐台现读**（`bundle_menus_assets_all/MonoBehaviour/`，字段 `m_ScrollSensitivity` / `m_Horizontal` / `m_Vertical`）：
  `Forge Army Selector` = **1.0**（MB `-8481382965408668617`/`565539536240021149`）· `Campaign Army Selector` = **1.0**（MB `1901330447545511309`/`5671518170206902015`）·
  `Campaign Track` = **1.0**（MB `-7709185174799471219`）· `Rewards Scroll View`（锻造那台）= **1.0**（MB `-3238177283330437475` 等 4 台全 1.0）·
  `Reward Window > Content > Scroll View` = **1.0**（MB `6402722706516142449`，H=1 V=0）· 各 `Army Selector` = **1.0**。
  ⚠️ 订正一处引用：`D_N29_滚动灵敏度逐视图.md` **只列了** `Campaign Track` / `Forge Tab > Rewards Scroll View` / `Reward Window > Content > Scroll View` 三行（`:47-49`），
  **没有** `Forge Army Selector` / `Campaign Army Selector` 两行 —— W16 注释里「（D_N29）」对这两台是**借引**，真判据 = 上面这两份 MB。
- 已在 HEAD 里落地、且注释逐处自陈「不是原版值，是我们挑的」；W16 报告写明「派单 = 主对话已定的口径」。⇒ **红 = 断言期望值没跟着改**。
- ⚠️ **但这是一条对原版的偏离**：铁律 11 要求「全部复刻，只有先后之分」，偏离须用户拍板；目前仓里**只有「主对话定」，没有用户原话**（任务总表 N66 行也只写「✅ 已改」）。⇒ 判定记 α，但**主对话必须先向用户确认**：认可 120 → 改 6 条断言 + 订正 `MenuScroll.cs:103` 的「唯一一处」；不认可 → 6 处实参回 1f（那时这 6 条本来就是对的，红会自己消失）。

### C. 1 条队列红 = `DeckInfoPopup` 四档 → 十档重排（α）

- HEAD 把 `QDIHit` 从 3123 改到 3129（并把压暗/红底/立绘/面板/行/字各拆一档），断言仍写 3123。
- 断言的**语义**（字面量钉死 + 命中区 > 压暗层）不变；只需把字面量跟到 3129。
- 同类「旧 3123」文案散在 6 处注释/日志串里（见 #12 修法），按铁律 5 一起订正。

### D. 1 条网格红 = W11 把「卡」摆进「格 − 张数条」（α）

- `win.CardsCells[0]` 是 **CardView 根**，不是格节点；W11 之后它居中在 345.728 高的卡块里 ⇒ 期望值应该是 **328.76**，不是格心 347.9。

### E. 2 条 A1212/A1226 红 = 2026-10-18 的「框」重核（α）

- #14：47.88 → **49.66**（两个不同 prefab 的行高 55.7 vs 58 被混用；已用 RT 链 + GridLayoutGroup 字段坐实 58）。
- #15：44（**我们旧值**）→ **22.87**（原版 `LayoutElement.m_MinWidth`）。
- 两条都在 HEAD 的 P1/A1226 批里改过实现，断言没同步。

---

## §三 没查清 / 顺手发现（都写「没查到」或「推导，未实机」）

1. **`Count` 的「真布局宽」没查到（推导值 ≈ 83.16）** —— 原版 `Count` 的 rect 是**布局组驱动**（anchors 0,0 · `m_SizeDelta=(0,0)` · `LayoutElement.m_MinWidth=22.87 / preferredWidth=0 / flexibleWidth=-1`），父 `Text fill` 有 `HorizontalLayoutGroup`（`ctrlW=1 · expandW=1 · align=3 MiddleRight`，MB `8718433945985976744`）。按 uGUI `HorizontalOrVerticalLayoutGroup.cs:144-219` + TMP 度量（Pragati `m_PointSize=95`、`Card Name` 9 字 = 366.08 单位 ⇒ fs38 = **146.43px**，`x2` = 75.53 单位 ⇒ fs32 = **25.44px**）：
   `Text fill` 宽 = `0.86 × 339.9 − 5 = 287.31` ⇒ `mult=(287.31−171.87)/2=57.72` ⇒ **`Count` ≈ 83.16px**（不是 22.87，也不是 44）。⚠️ **这是推导、没有实机量过**；`menu_dump.py` 明说 TMP 的 pref 是 `LG_UNK`（`:782-798`、`:1146-1157`），所以工具和 RM 表给的 22.87 都是「min 宽兜底」，不是运行时框。要钉死需真机 `GetPreferredValues` 或 Unity 里量原版 prefab。
2. **`Count` 的框高：我们 58、原版应为 0.86×58 = 49.88** —— 断言 `CollectionScene.cs:9264` 的 `boxH: 58f` 现在钉的是 **58**（绿的，所以不在红名单里）。`Count` 是 `Text fill` 的布局子件（`ctrlH=1 · expandH=1`）⇒ 高 = 父内高 49.88。**这是一条未记账的小偏离**（字是 `Midline`，观感约差 4px），建议立账。
3. **120 的用户拍板状态没查到** —— 只有 `W16_横向滚轮步长.md:3`「派单 = 主对话已定的口径」与代码注释「2026-10-18 主对话定」；**没有用户原话**。`任务线/界面线/任务总表.md` 里只有 `N66`（主菜单模式卡）标了「✅ 已改」，**W16 这 7 处没有独立任务行**（`N106` 在 W13 就已标 ✅）⇒ 账目缺口。
4. **`MenuScroll.cs:99-108` 的 doc 已过期**（铁律 5）：仍写「**唯一一处**不是原版值的传参 = 主菜单模式卡 120」，实际 8 处（`MainMenuRuntime.cs:2200` · `LeaderboardWindow.cs:461` · `RewardWindow.cs:1121` · `ForgeTab.cs:399/443` · `CampaignTab.cs:208/372` · `DailyRewardPopup.cs:257`）。
5. **`RM_原版TMP框尺寸.md` 表 B 的 #38/#40 需要就地订正**（55.7 是 `Deck Editing Menu` 的行高；`Deck info Popup` 是 58）。同表 #42/#52/#53（`DeckRuntime` 的 48.82×47.88）我**没有核实**其父行高是否真是 55.7 —— 不在本批 15 条里，留给主对话决定要不要查。
6. **#12 的「10 档梯子」是否符合原版没查** —— 原版根本没有渲染队列这层（它靠 `m_Children` 兄弟序），队列号是我们的等价复现 ⇒ 只能核「相对次序是否与原版兄弟序一致」，本报告只核到「命中区仍严格高于压暗层」，**没有逐层核完十档**。
7. **本报告没有跑 Unity、没有实机** —— 所有结论来自 `.cs` 源码 / 解包 prefab JSON / uGUI 源码 / 历次日志 / git diff。

---

## §四 一句话给主对话

- **先修 A 组（一处代码：`Label.ReplayFitForFace` 保住折行档）⇒ 一次回 5 条红**，且是唯一「真缺陷」；
- B 组 6 条要先问用户「120 vs 原版 1.0」，再决定改断言还是回滚；
- C/D/E 三组 4 条都是「实现刻意改了、断言没跟」，按上表改期望值即可（#15 顺带把「原版有效框 44」这句错话改掉）。
