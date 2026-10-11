# P 信箱与名牌 —— `InboxBtn` 被名牌压住 85% 该动谁

代理：`P-信箱与名牌`（只读诊断）。日期 2026-10-11（第十七会话）。只写结论与判据，⛔ 未改任何代码 / 正本。

## 结论（一句话：该动谁 / 判不了）

**该动名牌 —— 而且不是「把它改窄」，是【照原版根本不画它】**：
原版 `Player Profile/Background Mask` 那个 GameObject **出厂 `m_IsActive = 0`**，子件
`Planer Name Background` 因此 `activeInHierarchy = False` ⇒ **原版一个像素都不画那块暗紫板**；
原版实拍里那块「名牌」其实是**另一个节点** `Player Profile/Background`（`40k_main_player frame`，
右沿 **411**）。⇒ 我们 `Shell/TopBar.cs:251-252` 把它建成**活的 quad** 才是那 85% 的根因。

🔴 **⛔ 不要动 `InboxBtn`**：它的 **425.3..480.3 × 15.5..55.5** 是 `TopBarButtons` 的 HLG **算出来的**、
与原版实拍**逐像素吻合**（见下 §原版实拍）；而改它 = **重排整条 `TopBarButtons`**（见 §托管）。

## 原版两件的绝对矩形与重叠量（算式写全）

公式（uGUI，来自 `工具/menu_rect.py` 头部）：`refNorm = lerp(aMin,aMax,pivot)` ·
`refPx = parentTopLeft + (refNorm.x*pw, (1−refNorm.y)*ph)` · `pivotPx = refPx + (pos.x, −pos.y)` ·
`size = |aMax−aMin|*p + sizeDelta` · `rect = [pivotPx.x − pivot.x*w, pivotPx.x + (1−pivot.x)*w] × [pivotPx.y − (1−pivot.y)*h, pivotPx.y + pivot.y*h]`。
**父链**（逐级实读 `m_Father`）：`Upper bar`(RT **1092**) → {`TopBarButtons`(**1120**) → `InboxBtn`(**1561**) ·
`Player Profile`(**1125**) → `Background Mask`(**1116**) → `Planer Name Background`(**1090**)}
（`_1561.m_Father=1120` · `_1120=1092` · `_1125=1092` · `_1116=1125` · `_1090=1116`）。
1. **`Upper bar` 1092**：a(0,0)-(1,1) p(.5,.5) pos(0,490) sz(−2.96e−5,−980) ⇒ **0..1920 × 0..100**（1920×100）。
2. **`TopBarButtons` 1120**：a(0,1)-(0,1) p(.5,.5) pos(**581,−35.5**) sz(311.4,71.33)
   ⇒ refPx=(0,0) · pivotPx=(581,35.5) ⇒ **425.30..736.70 × −0.165..71.165**。
3. **`InboxBtn` 1561**：a(0,0)-(0,0) p(.5,1) pos(**0,0**) sz(**55,40**) —— **`pos` 是退化值，位置由 HLG 算**
   （`MonoBehaviour_1979.json`：`m_Padding` 全 0 · `m_ChildAlignment = 3`(MiddleLeft) · `m_Spacing = 9.75` ·
   `m_ChildForceExpand* = 0/0` · **`m_ChildControlWidth/Height = 0/0`（不控尺寸）** · `m_ReverseArrangement = 0`）。
   `TopBarButtons` 三个子件 = `InboxBtn`(55×40 · act **T**) · `Challenge button` 1391(47.46² · act **T**) ·
   `Feedback Button` 1562(50² · act **F**) —— **HLG 跳过 inactive** ⇒ 参与布局的只有前两颗。
   行内 x：`x0 = 425.30` → InboxBtn **425.30..480.30**；`425.30+55+9.75 = 490.05` → Challenge **490.05..537.51**。
   行内 y：内容高 = max(40,47.46) = 47.46 ⇒ 顶 = −0.165 + (71.33−47.46)/2 = **11.768**；
   InboxBtn 居中 ⇒ 顶 = 11.768 + (47.46−40)/2 = **15.50** ⇒ **15.50..55.50**；Challenge 顶 11.768 ⇒ 11.77..59.23。
   ✅ **与本仓权威表 `主菜单_原版规格.md:324,326` 的 425.3..480.3 / 15.5..55.5 与 490.1..537.6 逐值吻合**。
4. **`Player Profile` 1125**：a(0,1)-(0,1) p(**0,1**) pos(0,0) sz(528.65,211.07) ⇒ **0..528.65 × 0..211.07**
   （p(.5,.5) 时 refPx=(0,0)、pivotPx=(0,0)，与 pivot (0,1) 同 ⇒ 左右下都从 0 起）。
5. **`Background Mask` 1116 / `Planer Name Background` 1090**（同矩形）：
   a/p 全 (.5,.5) ⇒ refPx = (0.5×528.65, 0.5×211.07) = (**264.325, 105.535**)；
   1116 的 pos = (−15.46, **67.798**) ⇒ pivotPx = (248.865, 37.737)，sz(446.47,45.60)
   ⇒ x **25.630..472.100** · y **14.937..60.537**；
   1090 的 pos = (−2.19e−05, 2.29e−05) ⇒ 同矩形（**25.63..472.10 × 14.94..60.54**）。
   ✅ 与 `主菜单_原版规格.md:331` 的 25.6..472.1 / 14.9..60.5 吻合。
6. **重叠量**（若把这块板画出来）：
   `Δx = min(480.30, 472.10) − max(425.30, 25.63) = **46.80** px` ·
   `Δy = min(55.50, 60.54) − max(15.50, 14.94) = **40.00** px`
   ⇒ `面积比 = 46.80×40.00 / (55×40) = 1872 / 2200 = **85.09%**`（= 交接里那句「压住约 85%」）。
7. **实际可见的那一件**（`Player Profile/Background` 1104，`40k_main_player frame`）：
   a/p 全 (.5,.5) · pos(−47.326, 31.981) · sz(388,124) ⇒ pivotPx = (216.999, 73.554)
   ⇒ x **23.00..411.00** · y **11.55..135.55**。`Δx = min(480.30,411.00) − max(425.30,23.00) = **−14.30**`
   ⇒ **不重叠，净空 14.30 px**。
8. **原版实拍逐像素复核**（`资料/原版实拍/menu_topbar_0927/shot_MainMenu_Warpforge.png`，**1920×1080 原生**）：
   · y=35：x **410=(41,28,28) 暖褐**（= `Background` 右沿 411 ✅）→ **412..426 = (20,0,12)…(22,0,14)**（g 通道 **= 0**）
     → **432..473 亮灰 (182..218)**（= 信封）→ **476..496 = (12,0,8)…(31,1,19)**（= 条底 `UI_Main_Upper_bar`）。
   · 判据：那块板若画出来，其 **`m_Color` 的 g = 0.1925** ⇒ 板上像素 **g 必然 > 0**（我们在同一处渲的是 (37,14,27)）
     而 412..426 的 **g 恒为 0**；且 432..473 是**信封的亮灰**（板压在上面就该是暗紫）⇒ **原版这里没有那块板**
     （不是「板被裁窄了」，是**根本没画**）。
   · 信封格 425.3..480.3 亮度 max **226**/avg **145** ⇒ 完整画出；`Challenge` 格 490.1..537.6 max **30** ⇒ **空**。

## 谁被 LayoutGroup 托管（逐个：件 / 组 / 字段值 / 出处）

| 件 | RT pid | 组 | 字段现读 | 出处 |
|---|---|---|---|---|
| `Upper bar` | 1092 | **无**（GO 只有 1 个组件 = RT 本身） | —— | `GameObject/Upper bar.json` |
| `TopBarButtons` | 1120 | **HorizontalLayoutGroup**（MB **1979**，`m_Enabled=1`，脚本 pid −3229211799126679632） | pad(0,0,0,0) · `ChildAlignment 3` · `Spacing 9.75` · ForceExpand(0,0) · **ChildControl(0,0)** · Scale(0,0) · `ReverseArrangement 0` | `MonoBehaviour/MonoBehaviour_1979.json` |
| → `InboxBtn` | 1561 | **被 HLG 托管**（a(0,0)-(0,0) pos(0,0) = 退化值） | sz(55,40) | `RectTransform_1561.json` |
| → `Challenge button` | 1391 | **被 HLG 托管** | sz(47.46,47.46) | `RectTransform_1391.json` |
| → `Feedback Button` | 1562 | **不参与**（`m_IsActive=0`，uGUI 建 `rectChildren` 时跳过）⇒ 它那格是**手动锚点** | a(0,1)-(0,1) pos(165.14,−10.665) ⇒ 565.44..615.44 × 10.50..60.50 | `GameObject/Feedback Button.json` |
| `Player Profile` | 1125 | **无**（GO 只有 2 个组件：RT 1125 + `PlayerProfileUIController` MB 1989） | a(0,1)-(0,1) p(0,1) pos(0,0) sz(528.65,211.07) | `GameObject/Player Profile.json` |
| `Background Mask` | 1116 | **无布局组**；但挂着一个**裁剪件**（MB **1975**，字段 = 仅 `{m_GameObject, m_Enabled=1, m_Script, m_Name, m_Padding(Vector4), m_Softness(Vector2Int)}`） | `m_Padding (61.5, 0, −46.41, 0)` · `m_Softness (71, 0)` | `MonoBehaviour/MonoBehaviour_1975.json` |

🔴 **两条硬结论**：① **`Player Profile` 完全不在任何 LayoutGroup / ContentSizeFitter 里** ⇒ 它「改宽」**不会牵动别处**；
② ⚠️ **但它一样不能改** —— 它 5 个会用到的子件（`Background`/`Background Mask`/`Player Name`/`Avatar Item Small`/
`Player Level`）**锚点全是 (.5,.5)** ⇒ 父宽 W 一变，`refPx.x` 跟着变 `ΔW/2` ⇒ **这 5 件整体平移**，而它们的绝对位置
**已经和原版实拍对上**（`Background` 右沿 411 ✅）。
③ **`TopBarButtons` 是 HLG 托管** ⇒ 动 `InboxBtn` 会**重排整条**（spacing 9.75，`Challenge` 跟着移）；
`Feedback Button` 那格是**手写死值**（565.44），条内一重排它就**对不上了**。

## 我们那两件（队列定义与调用点 · 为什么这么摆）

* 常量：`Shell/TopBar.cs:97-105` —— `QBarPanel 2986`(= `MainMenuRuntime.QBarPanel`) · **`QBarContent 2987`** · `QBarBadge 2988` ·
  `QBarPlayerFrame 2989` · **`QBarNamePlate 2990`**（本件自己开的档）· `QBarAvatarFrame 2991` · `QBarAvatar 2992` · `QBarLevel 2993` · `QBarText 2997`。
* **信封（`QBarContent 2987`）**：`TopBar.cs:148-151` —— `New(btns,"InboxBtn",55,40)` 再
  `Rect(inbox,"40K_notification", 425.3, 480.3, 15.5, 55.5, "Image", QBarContent, …)`。几何 **逐值照权威表**、没抄错。
* **名牌（`QBarNamePlate 2990`）**：`TopBar.cs:251-252` ——
  `Rect(p,"40k_topmarquee_currency_display_BW", 25.6, 472.1, 14.9, 60.5, "Planer Name Background", QBarNamePlate, Color(0.39623,0.19251,0.30954), …)`。
  **唯一调用点**（全库 `grep QBarNamePlate` 只命中 `TopBar.cs:101` 定义 + `:252` 这一处）。
* **为什么牌子压在信封之上** ⇒ **是照原版兄弟序，不是我们挑的**（`TopBar.cs:79-94` 记着来路）：`Upper bar` 的
  `m_Children` 实读 = `Background(1101) → SettingsBtn(1560) → TopBarButtons(1120) → **Player Profile(1125)** →
  Resources Bar(1187)` ⇒ 后画者在上 ⇒ F5（2026-10-17）排成 `牌子 2990 > 信封 2987`。✅ **这个次序本身没错**
  （原版确实如此）—— **错的是我们画了那块原版不画的板**。
* 我们实拍复核（`_tmp_view/play17/S1_menu.png`，2560×1440，×4/3）：y=35 时 x 410..470 = (38,15,28)…(101,49,78)
  （**板**）→ x=475 = (170,162,160)（信封露出的窄条）⇒ **信封只剩 ~6px 可见 ≈ 11%**，与 85.09% 吻合。

## 状态 → 参数 表

| 状态 | 原版（判据） | 我们 |
|---|---|---|
| **有未读 / 无未读** | 红点只是 `UiBadgeNotification` 把 **alpha 0↔1**（`Hide()` **不是** `SetActive`）；**两者几何完全一样**：`RT_1561 sz(55,40)`、红点格 454.3..489.3 × 2.0..37.0 | `TopBar.cs:155-156` `BadgeAlpha(DailyData.InboxHasBadge)`（false→0 / true→1）；几何同 |
| **名字短（常态）** | `Player Name` RT_1091 = **136.93..401.93 × 13.74..61.74**（算式：pivotPx=(269.425,37.737) ∓ (132.5,24)）· 居中 hAlign=2 · autosize `[10,32]` 基 36 | `TopBar.cs:255-261` 同矩形 + `SetAutoFitBox(265/108,48/108,10,32,36)` + `SetWrapping(false)` |
| **名字长** | `m_TextWrappingMode=0`（不折行）+ `m_overflowMode=0`（**Overflow**）+ 居中 ⇒ **框不裁字**，右沿可 > 401.93；autosize 先缩到 min 10。⚠️ **能不能真越过 425.30 —— 没跑到实况（未查清）** | 同参数；我们 `Label` 的溢出/裁切行为**没核**（未查清） |
| 🔴 **名字条底板** | **恒不画**：`Background Mask` GO `m_IsActive=0` ⇒ 子 `Planer Name Background` `activeInHierarchy=False`（出厂 `GameObject/Background Mask.json` + **5 份运行期 dump 全是 `True/False`**）；且**全包 1134 个 MB 没有一个引用它**（GO pid 11 / RT 1090 的 PPtr 只出现在它自己身上）· 全量反编译 26,282 份里 `"Background Mask"` 字符串 **0 命中** ⇒ **没有任何控制器会打开它** | **恒画**（`TopBar.cs:251-252` 建的是活 quad）⇒ **与名字长短、有无未读全都无关，恒压 85.09%** |
| `Challenge button` | 实况 **关**（运行期 dump `runtime_ui_dump_MainMenu_Warpforge.tsv:76` = `False False`；实拍 490.1..537.6 亮度 max **30** = 条底 = 空） | **常显**（`TopBar.cs:157-158`） |
| `Feedback Button` | 出厂 `m_IsActive=0`；实拍 565.4..615.4 亮度 max 32 = 空 | **没建**（A1522，一致） |

## 搜过哪几个包 / 搜过哪些词

* **主菜单包** `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge`（全量：`RectTransform/` **592** ·
  `GameObject/` **697** · `MonoBehaviour/` **1134** 逐件扫过 PPtr）—— 词：`InboxBtn` · `Player Profile` ·
  `Background Mask` · `Planer Name Background` · `TopBarButtons` · `Challenge button` · `Feedback Button`。
* **全库文件名扫描** `find d:/2/新解包资源/assets_full -name "Planer Name Background.json"` / `-name "Background Mask.json"`
  ⇒ **各只 1 份，均在主菜单包**。
* **反编译** `d:/2/tools/decomp_full`（26,282 份）—— 词：`Background Mask`（**0 命中**）· `nameBackground` · `NamePlate` ·
  `Planer` · `PlayerProfile`（`PlayerProfileUIController` 全 12 个方法在；字段 = `avatar` / `playerNameText` / `isPlayer` /
  `titleGO` / `titleText` / `showPlayerLevel` / `playerLevelGO` / `playerLevelText` —— **没有名字条底板那一格**）。
* **桩** `d:/2/Warpforge_code/Scripts/Assembly-CSharp` —— 词：`softness`（**0 命中**）· `backgroundmask`（0）·
  `MainMenuWindow` / `PlayerProfileUIController`（命中）；**实拍** = `shot_MainMenu_Warpforge.png`（1920×1080 原生，
  同批同目录）· 我们的 `_tmp_view/play17/S1_menu.png`。
* **运行期 dump（真·原版）**：`资料/原版实拍/menu_topbar_0927/runtime_ui_dump_MainMenu_Warpforge.tsv:78-83` ·
  `…/menu_forge_1016/runtime_ui_dump_menu_{forge,forge_forced,open,open_b}.tsv:80-81` ·
  `…/menu_forge_1016/menu_open_images.tsv:195`（**5 份、4 个不同状态，全部 `False/False`**）。

## 顺手发现

1. 🔴 **`Challenge button` 原版实况是【关】的，我们建成了常显**（`TopBar.cs:157-158`）。判据两条：运行期 dump `:76`
   `activeSelf=False / activeInHierarchy=False`；实拍 490.1..537.6 区间最大亮度 **30**（= 条底）。原因在
   `ChallengeButton.Initialize()` = `challengeManager.challenges.Count > 0`（服务端数据）。⚠️ 与我们「`Feedback Button`
   出厂 False ⇒ 不建」**不一致**（同样出厂 False，一颗不建、一颗常显）。
2. **`Player Level` 那格的图原版运行期会换**：prefab 那件是 `40k_topmarquee_currency_gold`，而运行期 dump 给的是
   **`UI_Button_Round_background`**；我们照**出厂值**建的（`TopBar.cs:270-271`）。⚠️ **谁换的、什么条件 —— 未查清**。
3. **`40k_topmarquee_currency_display BW` 不是「名字条专用板」**：同一张图在我们工程里被当**通用面板底**用在 6 处以上
   （`Shell/GenericOptionsPanel.cs:118-119` · `Shell/CollectionWindow.cs:854` · `Shell/ChatPanel.cs:323` ·
   `Shell/CardDetailPopup.cs:912` · `Deck/DeckRuntime.cs:1101`）——**别按「名字条那张图」去动它**。
4. **删板会红两条既有断言**（`Editor/MainMenuScene.cs`）：`:1320` `CheckAt(…,"Planer Name Background"…)` —— `CheckAt`
   传 null 走 `CheckTrue(false,…"（节点不在）")`（`:164-166`）⇒ **红**；`:10668` `CheckTint(…, (0.39623,0.19251,0.30954),…)`
   —— `CheckTint→QuadOf` 得 null ⇒ `CheckTintOn` 走 `CheckTrue(false,…"（节点不在）")`（`:11812-11814`）⇒ **红**。
   ⇒ 落地时**这两条要一并改**。（`Deck/DeckScene.cs:1660 TestLayout` 是**通用**扫描、**不看名字** ⇒ 少一张图不会红。）
5. `Background Mask` 的组件（MB 1975）字段与 `UnityEngine.UI.RectMask2D` 的序列化形态**逐字段吻合**（`m_Padding` Vector4 +
   `m_Softness` **Vector2Int**）。⚠️ **类名未能独立确证**（脚本在该包 externals 里，本地没有带类名的映射）—— 但
   「**它自己不激活**」这一条**与该组件是什么类无关**，独立成立。它不激活时「板会被裁到哪」**没查**（也不需要）。
