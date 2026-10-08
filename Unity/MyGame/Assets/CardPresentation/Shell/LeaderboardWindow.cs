// LeaderboardWindow.cs — 多人界面那一批 第 3 件：**四个排行榜**（`RankedRankingWindow` 那一族）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/排行榜_遭遇战.md` · `排行榜_经典.md` · `排行榜_轮抽.md` ·
// `排行榜_嵌入版与行族.md`（**行**那一段在 `Shell/LeaderboardRow.cs`）· `排行榜_入口与调用.md`（入口）。
// 骨架真值 → `资料/阶段二_多人界面_原版规格.md` §三（⚠️ 那一节原来只有**模板位**，本轮换成指针 + 真值摘要）。
//
// ---- 🔴 七条判据（读原始 JSON / 反编译定的）----
// ① **四个根**：`RankedSkirmishLeaderboardPopup`（遭遇，**3** 个页签）· `RankedClassicLeaderboardPopup Variant`
//    （经典，**2** 个）· `DraftLeaderboardPopup`（轮抽，**2** 个）· `Ranked Leaderboard Display`（**嵌入版**：
//    无页签 / 无关闭键 / 无暗底 / 无 `Army Selector`）。前三扇的根组件都是 **`RankedRankingWindow`**。
// ② 🔴 **页签互斥是【代码】接的，不是 `ToggleGroup`** —— 三个 toggle 的 `m_Group` 序列化全是空，
//    是 `RankedRankingWindow.Start` 逐个挂 `onValueChanged`；`ToggleTab` 里再手工把别的 `set_isOn(false)`。
//    而 `EverguildToggle.Awake` **不调** `Toggle.Awake` ⇒ **别照搬 uGUI 常识**。我们的页签自己管单选。
// ③ 🔴 **页签数按 `tabDefinitions` 算，不按树上的钮数** —— 轮抽树上有 `Armies`/`Alliances` 两个钮，
//    但 `tabDefinitions` **只登记了 `Alliances`**（`Armies` 那个在**原版里点不动**）⇒ 我们照原版：
//    钮建出来、点了**如实出声**、不切页（红线：不许静默失败）。
// ④ 🔴 **`Timer` 不建**（赛季倒计时：服务器数据 + 用户 2026-09-26 明确不要赛季倒计时）。
//    `Generic Simplified UI Button_updated`（'Last season'）与 `Last Season Text` **建**，
//    但**照原版的运行期规则关掉**：`UIRankingListController.Initialize` 末尾传 `showCurrent = true`
//    ⇒ `lastSeasonText.SetActive(!showCurrent)` = **关**；`AllowChangeSeason(有上一赛季榜?)`
//    ⇒ 本地**没有**上一赛季榜 ⇒ **按钮也关**（判据 → 正本 §A·1 的 `Initialize`/`ChangeSeasonRankingView`/`AllowChangeSeason`）。
// ⑤ **四棵榜都没有空态节点**（正本 §3·1 第 3 条）⇒ 本地没数据时**照原版留空**，
//    **不自己造一个空态**（要造就得先查 `战斗UI_原版对账表.md` §六「我们加的」）。
// ⑥ **入口**（`排行榜_入口与调用.md`）：`RankedEventWindowV2` 上**两颗** —— `rankingPrefab`（遭遇）与
//    `rankingPrefabClassic`（经典），按 `playMode == Classic(0)` 二选一；轮抽那扇由
//    `AlliancesEventScorePanel.LeaderboardButtonClick` / `DraftExpiringContent.OpenLeaderboard` 开。
// ⑦ 🔴 **嵌入版 `Ranked Leaderboard Display` 在原版里【零引用】**（84 个 bundle 解压后逐 SerializedFile 搜 guid，
//    只命中它自己的 `AssetBundle.m_Container`）⇒ **它没有入口**。任务文件把它列进「四个排行榜」⇒ 我们**建**它，
//    但**不编入口**（自检直接开它验）—— 同 `BattleLogPopup` 那条先例。
//
// ⚠️ **哪些是我们挑的（不是原版）**：嵌入版的 `Scroll View` 序列化高是 **0**（它没有 `LayoutElement`，
//    另三扇都有 `preferredHeight = 790`）⇒ 真实高取不到，我们**按父 `Content` 撑满**（见 `EmbListR`）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>四个排行榜。**枚举值是我们的**（原版没有「第几个榜」这种枚举，只有四个独立的 prefab）。</summary>
    public enum LeaderboardKind { Skirmish = 0, Classic = 1, Draft = 2, Embedded = 3 }

    /// <summary>页签。原版 `RankedRankingWindow.LeaderboardTab` 是**每格一个 provider** 的数据结构，
    /// 不是枚举；我们这三个名字取自三棵树上的钮名（`Player` / `Armies` / `Alliances`）。</summary>
    public enum LeaderboardTab { Player = 0, Armies = 1, Alliances = 2 }

    /// <summary>`RankedRankingWindow` 那一族 —— 排行榜。四扇共用一个类，差异只有页签表与行族。</summary>
    public class LeaderboardWindow : GameWindow
    {
        // 队列档：**弹窗那一档**（`BattleLogPopup` 用 3450；这一族再往上一段，互不重叠）
        public const int QBase = 3500;
        // ⚠️ `public`（2026-10-04 A47 接线批）：自检宿主要拿这两个档核「压暗命中区档 = 压暗层那一档
        //    且严格 < 本窗内容命中区最低档」这条不变量（`MenuDraw.ShadeRuleOk`）。
        // 🔴 **2026-10-08（A220）收窄**：脚本逐常量扫全工程 `.cs` 的**外部代码引用**（限定名
        //    `LeaderboardWindow.<档>`，已剔注释）—— `QPanel` **1 处** · `QHit` **2 处**
        //    （都在 `Editor/MainMenuScene.cs`「排行榜弹窗」那一段：`MenuDraw.CheckShadeRule` 一条 +
        //    `CheckAbsorbRule` 一条）⇒ 这两个留 `public`；同批一起放宽的 `QBg`/`QContent`/`QRow`/`QText`
        //    **外部 0 处** ⇒ 回 `const`（只有本类自用）。整段理由见 `Shell/BoosterPackOpenWindow.cs` 里那段常量收窄的理由注。
        //    ⚠️ **`QBase` 不在本次收窄范围**（它是本窗档位段的入口，且**当时外部真有 2 处**：
        //    `Editor/MainMenuScene.cs` 那条「顶栏档（`MainMenuRuntime.QBarPanel`）> **全工程最高的
        //    窗口档**」的断言）。
        //    🔴 **2026-10-11（A307 现读订正，铁律 5）**：那条断言**已被同日的 A283 反转掉** ——
        //    顶栏降到 **`2986–2998`**（用户当天裁定「照原版」⇒ **顶栏在每一扇窗之下**，判据 →
        //    `Shell/MainMenuRuntime.cs` 的 `QBarPanel` 那段）⇒「比全工程最高的窗档还高」这个方向**整个作废**。
        //    ⚠️ **2026-10-18 订正**：这一行原来写 `2994–2998` —— 2026-10-17 F5 把下沿从 `2994` 放宽到 `2986`
        //    （**唯一出处 = `Shell/TopBar.cs` 那张表**）；方向那条结论没变。
        //    **现读（2026-10-11）**：全树 `LeaderboardWindow.<档>` 的**代码**引用只剩 ——
        //    `QPanel` **1 处** · `QHit` **2 处**（都在 `Editor/MainMenuScene.cs`「排行榜弹窗」那一节：
        //    `MenuDraw.CheckShadeRule` 一条 + `CheckAbsorbRule` 一条）；**`QBase` 现在外部 0 处**。
        //    ⚠️ 本件（A307）只订正注释、**不动可见性** —— `QBase` 今天仍是 `public`（零行为影响，
        //    要按 A220 那条「外部 0 处 ⇒ 回 `const`」收窄的话请另开一件）。
        public const int QPanel = QBase;     // 3500 压暗层（`Menu Dark Background`）自己那一档
        public const int QHit = QBase + 20;  // 3520 窗内命中区的档（页签 / 行 / 关闭钮；**严格 > `QPanel`**）
        const int QBg = QBase + 1;           // 3501 窗底九宫 / 页签底 / 关闭钮底 / `Last season` 钮底
        const int QContent = QBase + 3;      // 3503 窗内那几件（`TopBar` · 分隔线 · 阵营图标 · 钮上图标）
        const int QRow = QBase + 5;          // 3505 每一行的底（`_rowCtx.Q`）
        const int QText = QBase + 15;        // 3515 文字（标题 / 钮上的字 / `Last Season Text`）

        public static LeaderboardWindow LastOpened { get; private set; }

        public LeaderboardKind Kind { get; private set; }
        public LeaderboardTab CurrentTab { get; private set; }
        /// <summary>这一页实际建了几行（自检用）。</summary>
        public int BuiltRows { get; private set; }
        /// <summary>这一棵建了几个页签钮（自检用）。</summary>
        public int TabCount { get; private set; }
        /// <summary>`Last season` 那颗钮**现在可不可见**（照原版规则算的，自检用）。</summary>
        public bool SeasonButtonVisible { get; private set; }
        public readonly List<string> MissingArt = new List<string>();

        Transform _listContent;
        MenuScroll _scroll;
        readonly RowCtx _rowCtx = new RowCtx();

        // ---- `Army Selector` 那一块（🆕 2026-10-03，A3）----
        MenuScroll _armyScroll;
        Transform _armyContent;
        /// <summary>当前选中的阵营（`null` = 没选）。自检用。</summary>
        public string SelectedArmy { get; private set; }
        /// <summary>这一轮真画出来几颗军种项（滚出视口的不建）。自检用。</summary>
        public int ArmyButtonCount { get; private set; }
        /// <summary>军种条那 13 颗**一共**该有几颗（`CampaignData.Armies.Length`）。自检用。</summary>
        public static int ArmyTotal { get { return CampaignData.Armies.Length; } }
        /// <summary>军种条的横向滚动区（自检用）。</summary>
        public MenuScroll ArmyScroll { get { return _armyScroll; } }
        /// <summary>榜单那一格的**纵向**滚动区（自检用 —— 同 `ArmyScroll` 那条理由：断言要能滚它）。
        /// 原版这一格是 `ScrollRect m_Horizontal=0 / m_Vertical=1 / m_MovementType=1`（= **Elastic**），内层 `Content` 挂
        /// `ContentSizeFitter m_VerticalFit=1`（实据 → `资料/普查产出_0927/排行榜_嵌入版与行族.md:38,40,61`）。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        // ============================================================ 真值（绝对画布像素）
        // ---- 三扇全屏弹窗共用那一套（三份普查逐位相同）----
        static readonly PxRect DarkBgR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkBgTint = new Color(0f, 0f, 0f, 0.773f);

        static readonly PxRect TabBarR = new PxRect(38.62f, 104.70f, 210.59f, 928.48f);
        const float TabL = 45.59f, TabR = 210.59f, TabTop = 104.70f, TabH = 157.684f;
        const float TabIconL = 45.59f, TabIconR = 205.59f, TabIconDy = 17.34f, TabIconH = 125.335f;
        /// <summary>`button_bg` 的色（正本 §A：`(1,0.427,0,1)`）。**选中/未选中是换图不换色**：
        /// `onSprite = 40K_settings_button_hover` · `offSprite = 40K_settings_button`。</summary>
        static readonly Color TabBgTint = new Color(1f, 0.427f, 0f, 1f);
        const string ArtTabOn = "40K_settings_button_hover", ArtTabOff = "40K_settings_button";

        static readonly PxRect PanelR = new PxRect(202.40f, 16.32f, 1717.60f, 1006.93f);
        static readonly Vector4 PanelBorder = new Vector4(42f, 363f, 655f, 81f);
        const string ArtPanel = "UI_Deck_Information_Back";
        static readonly PxRect TitleR = new PxRect(643.55f, 36.95f, 1276.45f, 146.52f);
        static readonly PxRect TopBarR = new PxRect(282.95f, 141.63f, 1637.05f, 147.63f);
        static readonly Vector4 LineBorder = new Vector4(80f, 0f, 80f, 0f);
        const string ArtLine = "40k_main_line";

        static readonly PxRect ContentR = new PxRect(248.99f, 147.64f, 1671.01f, 937.83f);
        static readonly PxRect ArmySelR = new PxRect(248.99f, 147.64f, 1671.01f, 258.59f);
        /// <summary>🆕 2026-10-04（§三第29条 A9 尾巴）：`Army Selector` 那个 `RectMask2D` 的
        /// **原版 `m_Softness` = (42,0)** —— 判据 = 逐处实读 `_tmp_view/q1_rm2d.txt`。
        /// ⚠️ **2026-10-05 更正（铁律 5，标签错、值没错）**：原来称它「156 个 `RectMask2D` 的全量 dump」
        /// 是错的 —— 那张表**只扫了 3 个菜单族包**（表头 `150 + 1 + 5 = 156`）；**全库真值 = 222**
        /// （菜单族 156 + `mainmenualwaysloaded` 1 + 通用弹窗 5 + `scenes_mainmenuwarpforge` 1
        /// + 13 个 arena 各 5 = 65）。🔑 两条复现判据（会再犯）：① 认的是 `m_Script` 的 PathID
        /// **`536591447201701790`**（`m_FileID = 1` → `bundle_Waprforge_monoscripts`）—— 拿工程本地
        /// `com.unity.ugui` 的 guid 去 grep 解包目录**命中 0**；② **必须限定 `MonoBehaviour/`**
        /// （整包 grep 会逐包多算 1）。逐包数字只留一处 → `MenuWindowBase.ClipSoftness` 的注释。
        /// 下面的值取自本窗 prefab，**不受这次标签订正影响**：
        /// `RankedClassicLeaderboardPopup Variant/Ranking Display/Content/Army Selector/Viewport`（:14-15）；
        /// 同族另两扇榜（`RankedSkirmishLeaderboardPopup` :206-207 · `DraftLeaderboardPopup` :40-41）**同一个值**。
        /// ⚠️ 同一扇窗里 `…/Content/Scroll View/Viewport` 是 **(0,0)**（硬边）⇒ **只接军种条这一处**（别顺手给榜单列表加）。
        /// ⚠️ 渐隐带按**我们这一格实际裁到的那条边**摆（本窗的 `Viewport` 与 `Army Selector` 同矩形 ——
        /// `menu_dump.py` 实读 `Army Selector` 与子件 `Viewport` 都是 249.0,147.6→1671.0,258.6 ⇒ 两个矩形一致）。
        /// ⇒ 带的内沿 = **290.99**（= 248.99 + 42）与 **1629.01**（= 1671.01 − 42）。
        /// 🆕 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768①）回原版复核（本次不是照抄上面那段注释）**：
        /// 直接读 prefab 的 MB 本体 —— `bundle_menus_assets_all/MonoBehaviour/`
        /// `MonoBehaviour_3897231031841831396.json`（遭遇）· `MonoBehaviour_-218206352525497374.json`（经典）·
        /// `MonoBehaviour_-3843768850543401621.json`（轮抽）三份**逐字相同**：
        /// `m_Padding (0,0,0,0)` · `m_Softness (42,0)` · `m_Enabled 1` ·
        /// `m_Script.m_PathID = 536591447201701790` = `bundle_Waprforge_monoscripts` 的 `UnityEngine.UI.RectMask2D`;
        /// 三颗的 `m_GameObject` 按 `m_Father` 父链上行分别是三扇榜的 `Ranking Display/Content/Army Selector/Viewport`。
        /// ⇒ **这个 `(42,0)` 是实读值**（`q1_rm2d.txt:205-207,14-15,39-40` 那三行同值，本次从原始 JSON 独立复核了一遍）。
        /// 🔴 它现在**同时**是「节点那两个字段的来源」与「逐件回落那一档的实参」（`BuildArmySelector` 的 `Hang`
        /// + `RebuildArmyButtons` 那三处），⛔ 别再写第二份数。</summary>
        public static readonly Vector2 ArmyClipSoft = new Vector2(42f, 0f);
        static readonly PxRect SepLineR = new PxRect(248.99f, 260.37f, 1671.01f, 266.37f);
        static readonly PxRect ScrollR = new PxRect(248.99f, 288.59f, 1671.01f, 937.83f);
        /// <summary>行容器（内层 `Content`）：**1200 宽**（360→1560，左右各留 111）。</summary>
        const float ListL = 360f, ListR = 1560f;

        // ---- `Army Selector` 里那些军种项（🆕 2026-10-03，§三 第 29 条 A3）----
        // 逐值出处：`python 工具/menu_dump.py bundle_menus_assets_all "Army Item Button" --depth 4`
        //   根 **136.36 × 121.59** · `HighlightBG` 136×122（`40K_settings_button_selected` 168×156 ·
        //   色 `(1,0.631,0.278,1)` · `ppuMul 0.92`）→ `Arrow` 102.38×30.71（`40K_ArmyTrack_chosen faction`
        //   84×21 · `ppuMul 0.92`）· `Icon` 115.36×102.59（**无图**，运行期 = `ArmyIconsSO.GetArmyIcon(army)` ·
        //   `preserveAspect`）· `Badge Highlight` 35×35（`40K_notification_number`）。
        // 🔴 **`HighlightBG` 只在【选中】时可见** —— 判据是**反编译的方法体**（第一权威）：
        //   `ArmyItemContainer__Initialize.c` 头一句 `highlightState.SetActive(false)`；
        //   `ArmyItemContainer__Click.c` 里 `SetActive(*(param_1+0x30), on)`（+0x30 = `highlightState`，
        //   字段顺序照签名桩 `Assembly-CSharp/ArmyItemContainer.cs:8-21`）。
        public const float ArmyBtnW = 136.36f, ArmyBtnH = 121.59f;
        /// <summary>`Army Content` 的 `HorizontalLayoutGroup.spacing` —— **负的**（13 颗会互相叠 14px）。</summary>
        public const float ArmyBtnSpacing = -14f;
        /// <summary>`Army Content` 的高（实读 130；比 `Army Selector` 的 110.95 **高** ⇒ 上下各溢出 ~5px，被裁）。</summary>
        public const float ArmyContentH = 130f;
        /// <summary>`Army Item Button` 里三个子件**相对根中心**的矩形（原版 prefab 局部坐标）。</summary>
        static readonly PxRect ArmyHLR = new PxRect(-68f, -61f, 68f, 61f);
        static readonly PxRect ArmyArrowR = new PxRect(-51.2f, 38.1f, 51.2f, 68.9f);
        static readonly PxRect ArmyIconR = new PxRect(-57.2f, -51.8f, 58.2f, 50.8f);
        /// <summary>🔴 `Army Item Button/Icon` 那颗 `Image` 的 **`m_RaycastPadding`** —— **负 = 外扩**。
        /// 判据（2026-10-18 现读）= `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Army Item Button" --depth 3`：
        /// `Icon  Image[spr=0 RT=1 pad={'x':-11.0,'y':-11.0,'z':-11.0,'w':-11.0}]`（同子树另 4 颗可射线件 pad 全 0）。
        /// ⛔ 外扩一律走 `MenuDraw.PaddedRect`（全工程唯一那一份「正值缩小、负值扩大」）—— 别另写算式。</summary>
        static readonly Vector4 ArmyIconPad = new Vector4(-11f, -11f, -11f, -11f);
        static readonly Color ArmyHLTint = new Color(1f, 0.631f, 0.278f, 1f);
        const string ArtArmyHL = "40K_settings_button_selected", ArtArmyArrow = "40K_ArmyTrack_chosen_faction";

        static readonly PxRect CloseR = new PxRect(1656.81f, 9.19f, 1731.19f, 84.80f);
        static readonly PxRect CloseInnerR = new PxRect(1664.96f, 17.17f, 1721.82f, 75.30f);
        /// <summary>🔴 关闭键两个吃射线的子件（`Background` / `Icon`，同一个矩形 56.86 × 58.13）那颗 `Image` 的
        /// **`m_RaycastPadding`** —— **负 = 外扩**。判据（2026-10-18 现读）=
        /// `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "RankedSkirmishLeaderboardPopup" --depth 3`：
        /// `Generic Close Button Orange/Background` 与 `/Icon` 各 `RT=1 pad={'x':-20.0,…}`（三扇弹窗逐位同）。
        /// ⛔ 外扩走 `MenuDraw.PaddedRect`（同 `ArmyIconPad`）。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);
        const string ArtCloseBg = "UI_Button_Round_background", ArtCloseCircle = "40k_general_bt_yellow",
                     ArtCloseIcon = "40k_general_bt_yellow_close";

        static readonly PxRect SeasonBtnR = new PxRect(264.74f, 62.21f, 509.74f, 121.26f);
        static readonly PxRect SeasonBtnTxR = new PxRect(276.44f, 68.00f, 497.25f, 115.48f);
        static readonly Vector4 SeasonBtnBorder = new Vector4(333f, 96f, 333f, 96f);
        const string ArtSeasonBtn = "UI_Button_Mulligan";
        static readonly PxRect SeasonTextR = new PxRect(1251.60f, 51.43f, 1660.40f, 132.04f);

        // ---- 嵌入版那一套（`Ranked Leaderboard Display`，根 rect 与三扇不同）----
        static readonly PxRect EmbPanelR = new PxRect(208.72f, 47.60f, 1727.48f, 1054.86f);
        static readonly PxRect EmbTitleR = new PxRect(643.55f, 73.06f, 1276.45f, 174.90f);
        static readonly PxRect EmbTopBarR = new PxRect(282.95f, 170.00f, 1637.05f, 176.00f);
        static readonly PxRect EmbBtnR = new PxRect(275.50f, 90.73f, 520.50f, 158.37f);
        static readonly PxRect EmbBtnTxR = new PxRect(287.20f, 97.35f, 508.01f, 151.74f);
        static readonly PxRect EmbSeasonTextR = new PxRect(1265.70f, 73.06f, 1674.50f, 176.03f);
        /// <summary>🔴 **推算值**（不是原版）：嵌入版的 `Scroll View` 序列化高是 **0**（没有 `LayoutElement`）
        /// ⇒ 我们按它的父 `Content`（`248.99,176.01→1671.01,1006.93`）撑满。见文件头「哪些是我们挑的」。</summary>
        static readonly PxRect EmbListR = new PxRect(248.99f, 176.01f, 1671.01f, 1006.93f);

        // ============================================================ 建

        /// <summary>开一扇。`kind` 选哪一棵（四个 prefab，一棵一个实例）。</summary>
        public static LeaderboardWindow Create(WindowsManager mgr, LeaderboardKind kind)
        {
            var go = new GameObject(NameOf(kind));
            var win = go.AddComponent<LeaderboardWindow>();
            win.Kind = kind;
            // 原版窗口字段（三扇全屏榜的根组件 `RankedRankingWindow` 是 `GameWindow` 子类）：
            // `type=1 Popup` · `windowsPlacement=15 Popup` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`
            win.type = WindowType.Popup;
            win.placement = WindowsPlacement.Popup;
            win.closeOnEsc = true;
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public static string NameOf(LeaderboardKind k)
        {
            switch (k)
            {
                case LeaderboardKind.Skirmish: return "RankedSkirmishLeaderboardPopup";
                case LeaderboardKind.Classic: return "RankedClassicLeaderboardPopup Variant";
                case LeaderboardKind.Draft: return "DraftLeaderboardPopup";
                default: return "Ranked Leaderboard Display";
            }
        }

        /// <summary>一个页签钮（**照各自的树**；`Wired` = 原版 `tabDefinitions` 有没有登记它）。</summary>
        struct TabEntry
        {
            public LeaderboardTab Tab; public string Name; public string Art; public bool Wired;
            public TabEntry(LeaderboardTab t, string n, string a, bool wired = true)
            { Tab = t; Name = n; Art = a; Wired = wired; }
        }

        static TabEntry[] TabsOf(LeaderboardKind k)
        {
            switch (k)
            {
                case LeaderboardKind.Skirmish:
                    return new[]
                    {
                        new TabEntry(LeaderboardTab.Player, "Player", "40K_Chat_icon_Global"),
                        new TabEntry(LeaderboardTab.Armies, "Armies", "40K_Profile_icon_title"),
                        new TabEntry(LeaderboardTab.Alliances, "Alliances", "40K_Chat_icon_Alliance_v2"),
                    };
                case LeaderboardKind.Classic:
                    return new[]
                    {
                        new TabEntry(LeaderboardTab.Player, "Player", "40K_Chat_icon_Global"),
                        new TabEntry(LeaderboardTab.Armies, "Armies", "40K_Profile_icon_title"),
                    };
                case LeaderboardKind.Draft:
                    // 🔴 判据 ③：原版 `tabDefinitions` **只登记了 `Alliances`** ⇒ `Armies` 那格点不动
                    return new[]
                    {
                        new TabEntry(LeaderboardTab.Armies, "Armies", "40K_Profile_icon_title", false),
                        new TabEntry(LeaderboardTab.Alliances, "Alliances", "40K_Chat_icon_Alliance_v2"),
                    };
                default:
                    return new TabEntry[0];      // 嵌入版没有页签
            }
        }

        /// <summary>页签 → 行族（正本 §A·4：每格一个 provider，各自的 `rankingRowPrefab`）。
        /// 遭遇的 Alliances 用 `AllianceRankingRow Variant`、轮抽的用 `Skulls Variant`（判据 ①）。</summary>
        public static LeaderboardRowFamily FamilyOf(LeaderboardKind k, LeaderboardTab t)
        {
            if (t == LeaderboardTab.Alliances)
                return k == LeaderboardKind.Draft ? LeaderboardRowFamily.AllianceSkulls : LeaderboardRowFamily.Alliance;
            if (t == LeaderboardTab.Armies) return LeaderboardRowFamily.PlayerForArmy;
            return LeaderboardRowFamily.Player;
        }

        /// <summary>开窗时默认选哪一格：**原版 `Open()` 开的是 `tabDefinitions[0]`**（经典榜实证
        /// `ToggleTab(tabDefinitions[0], true)`）。轮抽的 `tabDefinitions` 只有 `Alliances` ⇒ 以它为准。</summary>
        public static LeaderboardTab DefaultTabOf(LeaderboardKind k)
        {
            if (k == LeaderboardKind.Draft) return LeaderboardTab.Alliances;
            return LeaderboardTab.Player;      // 嵌入版的 `PlayerRankingDataProvider.playMode = 0 (Classic)`
        }

        public override void Open()
        {
            LastOpened = this;
            CurrentTab = DefaultTabOf(Kind);
            Build();
        }

        /// <summary>自检用：喂了数据之后重画（`LeaderboardData.InjectForTest` → 这个 → 断言 → `ClearForTest`）。</summary>
        public void RebuildForTest() { Build(); }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }
        /// <summary>🆕 2026-10-04（A9 尾巴）：`clipSoftness` 透传下去（原版 `RectMask2D.m_Softness`）。
        /// 🔴 纪律同别处：**谁设 `Clip` 谁顺手把 `ClipSoftness` 设对** —— 本文件只有军种条这一处带软边，
        /// 其余调用点不传 ⇒ 默认 `(0,0)`（硬边 = 原版那几处就是硬边，别给它们"顺手"加上）。</summary>
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false,
                       PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        { return MenuDraw.Rect(p, art == null ? CardArt.Solid() : Art(art), r, n, q, tint, keepAspect, clip, clipSoftness); }
        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q)
        { var t = Art(art); return t == null ? null : MenuDraw.Nine(p, t, r, b, t.width, t.height, q, null, true, n); }

        /// <summary>切页签（**唯一入口** —— 页签钮与自检都走它）。</summary>
        public void SelectTab(LeaderboardTab tab)
        {
            if (tab == CurrentTab) return;
            CurrentTab = tab;
            Build();
        }

        /// <summary>点了「原版没接线」的那一格（轮抽的 `Armies`）—— **如实出声，不假装切换**。</summary>
        void OnDeadTab(LeaderboardTab tab)
        {
            Debug.LogWarning("[Leaderboard] `" + NameOf(Kind) + "` 的 `" + tab + "` 那一格**点了不切换** —— "
                             + "这不是漏做：原版 `tabDefinitions` **只登记了 `Alliances`**（树上那个钮没接线）"
                             + "⇒ 我们照原版（判据 → 资料/普查产出_0927/排行榜_入口与调用.md §1）");
        }

        void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            BuiltRows = 0;
            TabCount = 0;

            if (Kind == LeaderboardKind.Embedded) BuildEmbedded();
            else BuildPopup();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Leaderboard] `" + NameOf(Kind) + "` ⚠️ 有 " + MissingArt.Count
                                 + " 张图取不到（**这些件没画**）：" + string.Join("、", MissingArt.ToArray()));
        }

        // ---------------------------------------------------------- 三扇全屏弹窗

        void BuildPopup()
        {
            var dark = Node(transform, "Menu Dark Background", DarkBgR);
            Rect(dark, null, DarkBgR, "Image", QPanel, DarkBgTint);
            // 🔴 **2026-10-04（A47 接线批）**：收口到公共件 `MenuDraw.ShadeHit`（档 = 压暗层自己那一档
            //   `QPanel` = 3500 < 内容命中区最低档 `QHit` = 3520）。原编码本来就合规矩。
            MenuDraw.ShadeHit(dark, DarkBgR, QPanel, QHit, () => Close(), "CloseHit");

            BuildTabs();

            var panel = Node(transform, "Ranking Display", PanelR);
            Nine(panel, ArtPanel, PanelR, PanelBorder, "Generic Window Red Background Big", QBg);
            // 🆕 **2026-10-06（A94）：榜单面板底图吸收点击**。判据 = 原版 prefab
            //   `RankedSkirmishLeaderboardPopup > Ranking Display > Generic Window Red Background Big`
            //   那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(transform, "AbsorbHit", PanelR, QPanel, QHit);
            // 🆕 **2026-10-12（A336③）**：`'TOP PLAYERS'` 那一族（4 颗弹窗 + 1 颗嵌入版共 5 份）实读
            //    = `fs 55.0 · auto[18~55] · base **36.0**` · 折行 0 ⇒ 上限 = 标称（A333 本来就对），base 36。
            MenuDraw.Text(panel, TitleR, "TOP PLAYERS", Color.white, "Title", 55f, QText, TitleR.W, 18f, 55f, 36f);
            MenuDraw.Rect(panel, Art(ArtLine), TopBarR, "TopBar", QContent, null, false);

            // `Content`（VLG：`Army Selector` + `Scroll View` 两块）
            var content = Node(panel, "Content", ContentR);
            BuildArmySelector(content);

            var sv = Node(content, "Scroll View", ScrollR);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：这颗 `Viewport` 是**裁切状态的载体**
            //    （= 原版那个 `UIMask`(a=0) + `RectMask2D`）—— 参数取原版实读的全 0
            //    ⇒ 与迁移前的 `_rowCtx.Clip = _scroll.Viewport`（同一矩形）逐位同值。
            var vpVc = ViewportClip.Hang(sv, "Viewport", ScrollR, Vector4.zero, Vector2Int.zero);
            var vp = vpVc.transform;
            _scroll = MenuScroll.TopAligned(ScrollR, 0f);
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`RebuildRows` 里
            //   `if (_scroll != null && !_scroll.Intersects(rr)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 就是上面那颗 `Viewport`。
            //   ⚠️ 今天两值同（节点框 = `ScrollR`、`padding` 全 0）⇒ **逐个位不变**。
            //   ⚠️ `_scroll` **每次 `Build()` 都新建**（`BuildPopup` / `BuildEmbedded` 各一处）
            //      ⇒ 赋值紧跟建它的那一行即可，**不存在**「节点被重建、字段停在已销毁组件上」那一档。
            _scroll.ClipNode = vpVc;
            // 档位 = 原版 `m_MovementType = 1` ⇒ **Elastic**（真值 `0 Unrestricted / 1 Elastic / 2 Clamped`）。
            // 判据 = 原始 JSON 实读：`python 工具/menu_dump.py bundle_menus_assets_all "RankedSkirmishLeaderboardPopup"`
            // ⇒ `Content/Scroll View` = `h=0 v=1 mode=1`（经典 / 轮抽两扇逐位相同）。⛔ 别套 `BattleLogPopup` 那一档（`mode=2`）。
            _scroll.Elastic = true;
            _scroll.Owner = gameObject;
            _scroll.OnChanged = () => RebuildRows();
            PointerLayer.RegisterScroll(_scroll);
            _listContent = Node(vp, "Content", new PxRect(ListL, ScrollR.y1, ListR, ScrollR.y1));
            RebuildRows();

            BuildSeasonPieces(false);
            BuildCloseButton();
        }

        // ---------------------------------------------------------- `Army Selector`（🆕 A3）

        /// <summary>`Army Selector`：**外壳 + 真的把军种项填进去**（🆕 2026-10-03，§三第29条 A3）。
        /// 原版那条链（**反编译方法体，第一权威**）：
        ///   `ArmySelector__Initialize.c` —— 清空 `contentAnchor` → 遍历传进来的 `List&lt;CardArmy>` →
        ///   `FeatureConfig.IsArmyHidden(army)` 为真就**跳过** → `Instantiate(armyItemButton, contentAnchor)` →
        ///   GO 改名 `"&lt;前缀>" + army` → `ArmyItemContainer.Initialize(item, army, toggleGroup, badgeType)` →
        ///   `item.OnSelected += SelectArmy` → 加进列表 → **`army == 传入的 selected` 时 `toggle.isOn = true`**。
        /// 🔴 **三处如实标注（原版判据拿不到）**：
        ///   ① **阵营清单**原版由调用方（`PlayerRankingDataProvider`）给，**本地读不到** ⇒
        ///      我们用**卡池那 13 个阵营**（`CampaignData.Armies`，与锻造厂/战役页同一份）；
        ///   ② `FeatureConfig.IsArmyHidden` 那张**隐藏阵营表在服务器** ⇒ 我们**不隐藏任何一个**；
        ///   ③ `Badge Highlight`（`40K_notification_number`）只在 `badgeType` 为 1/2 时由 `Initialize` 挂
        ///      —— 那是**活动角标**，我们**不建**（没有活动系统）。</summary>
        void BuildArmySelector(Transform content)
        {
            var sel = Node(content, "Army Selector", ArmySelR);
            // 原版这个 `Viewport` 上**只有 `RectMask2D`**（没有 Image）⇒ 不画（它的 `UIMask` 那颗 `Image` 是 `a=0`）。
            // 🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768①）**：这颗节点从今天起就是**本视口的裁切状态载体**
            //   （= 原版那个 `RectMask2D`）。**参数回原版逐字复核过（⛔ 不是照抄注释）**：
            //    · `assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3897231031841831396.json`
            //      = `m_Padding (0,0,0,0)` · `m_Softness (42,0)` · `m_Enabled 1`，
            //      且它的 `m_GameObject`（PathID `-2793685670958024220`）按 `m_Father` 父链上行 =
            //      `RankedSkirmishLeaderboardPopup/Ranking Display/Content/Army Selector/Viewport`（本件现走一遍）；
            //    · 同族另两扇 `RankedClassicLeaderboardPopup Variant`（MB `-218206352525497374`）·
            //      `DraftLeaderboardPopup`（MB `-3843768850543401621`）**逐位同值** `(42,0)`。
            //    ⚠️ 同一扇窗里 `…/Content/Scroll View/Viewport` 是 **(0,0)**（硬边）⇒ **只接军种条这一处**。
            //   ⛔ 别再退回「逐件显式传 `ArmySelR, ArmyClipSoft`」—— 形参非空 ⇒ `Resolve` 第 1 支 ⇒
            //      节点一个像素都不生效（`NodeShadowedByParam`，静默；迁移表 §二 通则那一句）。
            //  ⚠️ 软边那个数**只此一处**：`ArmyClipSoft` 既是节点字段的来源、也是回落那一档的实参
            //      （同 `Shell/PracticeModePopup.cs:852` 那条口径）。
            var vpVc = ViewportClip.Hang(sel, "Viewport", ArmySelR, Vector4.zero,
                                         new Vector2Int((int)ArmyClipSoft.x, (int)ArmyClipSoft.y));

            var armies = CampaignData.Armies;
            float contentW = armies.Length * ArmyBtnW + (armies.Length - 1) * ArmyBtnSpacing;
            // ⚠️ **内容从左缘起排**（`LeftAligned`）：原版 `Army Content` 出厂宽 0、pivot 居中 ⇒
            //    13 颗铺开之后 UGUI 会把它居中（首尾各溢出 ~91px），**运行时到底停在哪一侧静态读不到**
            //    ⇒ 我们取「左对齐 + 可横向滚动 182.66」，**这一条是我们挑的**（记在 §三第29条 A3）。
            if (_armyScroll == null)
            {
                _armyScroll = MenuScroll.LeftAligned(ArmySelR, contentW);
                // 🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768①）就地订正（铁律 5）**：这里原来留着 W-A435己
                //   的一段「**这处没接、只报不改**」的说明（当时那颗 `Viewport` 上还没有 `ViewportClip`）
                //   —— **那段已经不成立**：组件已由上面那句 `ViewportClip.Hang` 挂上。保留下来的判断只有一条：
                //   「挂上组件会**改行为**」（本子树里所有 `clip == null` 的件从「不裁」变成「按视口裁 + 带软边」）
                //   —— 那正是本件要做的事（原版那颗 `RectMask2D` 是 `m_Enabled = 1` ⇒ 原版就是裁的）。
                //   ✅ 与迁移前**逐位同值**：框 = 同一颗 `Viewport` 的 rect（本来就是 `ArmySelR`）、
                //      软边 (42,0) 与原来逐件传的 `ArmyClipSoft` 同一个数、pad 全 0。
                // 🔴 档位 = 原版 `Army Selector` 的 `m_MovementType = 1` ⇒ UGUI **Elastic**
                // （真值 `0 Unrestricted / 1 Elastic / 2 Clamped`，本地 UGUI 源码亲读）。
                // 判据 = 原始 JSON 实读（2026-10-05 逐扇复核；`python 工具/menu_dump.py bundle_menus_assets_all "<窗名>"`）：
                //   `RankedSkirmishLeaderboardPopup` · `RankedClassicLeaderboardPopup Variant` · `DraftLeaderboardPopup`
                //   三扇的 `Ranking Display/Content/Army Selector` **逐位相同**：
                //   `h=1 v=0 mode=1 inertia=1 elasticity=0.1 decel=0.135`（已沿父链认窗，不是按名字撞上的）。
                //   ⚠️ **第四扇 `Ranked Leaderboard Display`（嵌入版）整棵树里根本没有 `Army Selector`**
                //   （`Content` 下只有 `Scroll View`；与 `BuildEmbedded` 里那条 `subMenu` 的 PPtr = 0 互相印证）
                //   ⇒ 那一扇**不建它**，与本行无关。
                _armyScroll.Elastic = true;
                _armyScroll.Owner = gameObject;
                _armyScroll.OnChanged = RebuildArmyButtons;
                PointerLayer.RegisterScroll(_armyScroll);
            }
            else
            {
                _armyScroll.ContentX1 = ArmySelR.x1;
                _armyScroll.ContentX2 = ArmySelR.x1 + contentW;
                _armyScroll.Stop();
            }
            float cy = ArmySelR.CY;
            // 🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A769）结构缺口就地修掉**：`Army Content` 原来是
            //   `Army Selector` 的**子件**、与那颗 `Viewport` 是**兄弟**（原版是 `Viewport/Army Content` 那样的父子
            //   —— 父链实读：`bundle_menus_assets_all/GameObject/Viewport_-2793685670958024220.json` 的
            //   `m_Children` 里就是 `Army Content`）。兄弟放法下**即使挂上组件，条目也吃不到它**：
            //   `ViewportClip.FindAbove(条目)` 沿父链只经过 `Army Content → Army Selector`，**跳过 `Viewport`**。
            //   ⇒ 照原版**把父子关系摆对**（现在就挂在 `vpVc.transform` 下）。
            //   ✅ **世界位姿一点不动**（算过，不是猜的）：`MenuDraw.Node(parent, name, r)` 的 `r` 是
            //      **绝对设计 px**，节点世界位 = `RectCenter(r)`（`Local()` = `RectCenter(r) − PosInDesignSpace(parent)`，
            //      父的世界位再加回来 ⇒ 与 `parent` 是哪一颗**无关**）；而 `Viewport` 自己 `localScale = 1`、
            //      父链缩放两级相同 ⇒ 换父**逐位不变**（只差浮点加减的 ~1e-7）。
            //   ⚠️ 若把这一行改回 `sel`（兄弟）⇒ 条目**静默回到「不裁」**（本文件的自检段有独立判据盯着）。
            _armyContent = Node(vpVc.transform, "Army Content",
                                new PxRect(ArmySelR.x1, cy - ArmyContentH * 0.5f, ArmySelR.x1 + contentW, cy + ArmyContentH * 0.5f));
            // 🔴 **A465（W-A435己）那一行，本件补上**：构建循环（`RebuildArmyButtons` 里
            //   `if (_armyScroll != null && !_armyScroll.Intersects(r)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）。
            //   ⚠️ **赋值必须写在 `if/else` 之后**：`_armyScroll` 是**复用的**（`if (_armyScroll == null)`），
            //      而那颗 `Viewport` 节点**每次 `Build()` 都被 `ClearChildren` 销毁重建**（同 A762 那个形状）
            //      ⇒ 写在 `if` 那一支里 = 第二次构建之后 `ClipNode` 指向**已销毁**的组件
            //      （Unity 判它 `== null` ⇒ 静默回落到 `Viewport`）。
            //   ⚠️ `Build()` 会被重跑（`SelectTab` / `RebuildForTest` 都调它）⇒ 这一段每跑一次都要重喂。
            _armyScroll.ClipNode = vpVc;
            RebuildArmyButtons();

            MenuDraw.Rect(sel, Art(ArtLine), SepLineR, "Separator Line", QContent, null, false);
        }

        /// <summary>按当前滚动偏移铺那 13 颗（滚出视口的**不建**）。</summary>
        void RebuildArmyButtons()
        {
            if (_armyContent == null) return;
            MenuDraw.ClearChildren(_armyContent);
            ArmyButtonCount = 0;

            var armies = CampaignData.Armies;
            float cy = ArmySelR.CY;
            for (int i = 0; i < armies.Length; i++)
            {
                float cx = ArmySelR.x1 + i * (ArmyBtnW + ArmyBtnSpacing) + ArmyBtnW * 0.5f;
                cx -= _armyScroll != null ? _armyScroll.Offset : 0f;   // 内容左移 = 看到右边
                var r = new PxRect(cx - ArmyBtnW * 0.5f, cy - ArmyBtnH * 0.5f,
                                   cx + ArmyBtnW * 0.5f, cy + ArmyBtnH * 0.5f);
                if (_armyScroll != null && !_armyScroll.Intersects(r)) continue;

                string army = armies[i];
                var node = Node(_armyContent, army, r);
                bool on = army == SelectedArmy;

                // `HighlightBG` + `Arrow`：**只在选中时可见**（判据见上面 `ArmyHLR` 那段注释）
                // 🆕 A9 尾巴：这三层都吃软边（原版 `Army Selector/Viewport` 的 `m_Softness = (42,0)`）——
                //   压在左右两条渐隐带里的那几颗会被按剖面削 alpha（`MenuDraw.ApplySoftEdges` 的几何等效物）。
                // 🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768①）**：裁切框那个实参（原来是 `ArmySelR`）
                //   **一律换成 `null`** —— 硬裁那一刀（以及 `ArmyClipSoft (42,0)` 那道渐隐带）现在由
                //   上面那颗 `Army Selector/Viewport` 节点说了算（`MenuDraw.Rect` 内部自己 `Resolve`）。
                //   ⚠️ `clipSoftness` **照旧传 `ArmyClipSoft`**：它是**回落那一档**的实参
                //   （节点被挪走/没有节点时用的值），与节点字段同一个数（同 `PracticeModePopup` 那条口径）。
                //   ⛔ 别再传回 `ArmySelR` —— 形参非空 ⇒ 节点被盖住（`NodeShadowedByParam`，静默）。
                if (on)
                {
                    var hl = Node(node, "HighlightBG", Rel(r, ArmyHLR));
                    Rect(hl, ArtArmyHL, Rel(r, ArmyHLR), "Image", QContent, ArmyHLTint, true, null, ArmyClipSoft);
                    Rect(hl, ArtArmyArrow, Rel(r, ArmyArrowR), "Arrow", QContent, null, true, null, ArmyClipSoft);
                }
                // `Icon`：原版无图，运行期 `ArmyIconsSO.GetArmyIcon(army)` —— 我们走同一份阵营图标表
                var iconR = Rel(r, ArmyIconR);
                Rect(node, DeckRuntime.FactionIcon(army), iconR, "Icon", QContent, null, true,
                     null, ArmyClipSoft);
                // 🔴 **2026-10-18（波 2a · W1 · 归真值）**：命中区 = **`Icon` 那颗按它自己的
                //   `m_RaycastPadding = (-11)⁴` 外扩**（`ArmyIconPad`，**负 = 外扩**）⇒ **原版真值 137.36 × 124.59**。
                //   判据（第一权威 = 原版 prefab 实读）→ `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all
                //   "Army Item Button" --depth 3`：
                //   根 `Army Item Button` **没有任何 Graphic**（组件只有 `RectTransform · CanvasRenderer ·
                //   EverguildToggle · ArmyItemContainer`）⇒ **根矩形不是可射线区**；子树里 5 颗 `m_RaycastTarget = 1`
                //   的件 = `HighlightBG`(136.00×122.00) · `Arrow`(102.38×30.71) · **`Icon`(115.36×102.59 · pad `(-11)⁴`)**
                //   · `Badge Highlight`(35×35) · `OneText`(35×35)；其中 `HighlightBG`/`Arrow` **只在选中时可见**
                //   （判据见上面 `ArmyHLR` 那段）⇒ 常态可射线区 = **`Icon` 外扩后那颗**。
                //   🔴 **上一轮（E3）只拿了 `Icon` 的【裸】矩形** ⇒ 每边小 11px（那时量到 115.40 × 102.60）。
                //      而**它改前那版（根矩形 136.36 × 121.59）每边只小 0.5 / 1.5px** ⇒ E3 那次「从根矩形换成
                //      `Icon` 裸矩形」其实是把它**改小**了（E3 记的「用根矩形宽出 21.00 / 高 19.00」是拿
                //      `Icon` 与根之间那道**内缩**当成了射线差 —— 内缩不是射线真值）。
                //   ⛔ 外扩走 `MenuDraw.PaddedRect`（全工程唯一那一份口径），别另写一份。
                //   ⚠️ 连带撤销 E3 的一条前提：按真值算相邻两颗**恒叠 15.00px**（步进 122.36 = `ArmyBtnW + ArmyBtnSpacing`）
                //   —— **原版本来就叠** ⇒ 缺陷只在「可点区算错」，不在「重叠」本身。
                //   ⛔ `ArmyBtnSpacing = -14f`（本文件 :177）**一个字不改**：相邻两颗**版面**叠 14px 是原版
                //   （`Army Content` 的 `HorizontalLayoutGroup.spacing = -14.0`）⇒ 画出来的叠法照旧。
                MenuDraw.Hit(node, "Hit", MenuDraw.PaddedRect(iconR, ArmyIconPad), QHit, () => SelectArmy(army));
                ArmyButtonCount++;
            }
        }

        /// <summary>把「相对按钮中心」的原版矩形换成绝对矩形。</summary>
        static PxRect Rel(PxRect btn, PxRect inner)
        {
            float cx = btn.CX, cy = btn.CY;
            return new PxRect(cx + inner.x1, cy + inner.y1, cx + inner.x2, cy + inner.y2);
        }

        /// <summary>选中某个阵营（原版 = `ToggleGroup` 只亮一颗 + `HighlightBG.SetActive`）。
        /// 🔴 **本地的榜是空的**（原版读服务器）⇒ 这条筛选**现在筛不出任何变化**，如实出声、不假装。</summary>
        public void SelectArmy(string army)
        {
            SelectedArmy = army == SelectedArmy ? null : army;      // 再点一次 = 取消（Toggle 语义）
            RebuildArmyButtons();
            Debug.Log("[Leaderboard] 选中阵营：" + (SelectedArmy ?? "（取消，全部）")
                      + " —— ⚠️ **本地的榜是空的**（原版按这个阵营去服务器拉）⇒ 列表不会有变化，**如实说明**");
        }

        // ---------------------------------------------------------- 嵌入版

        void BuildEmbedded()
        {
            // 🔴 它**不是一扇窗**（原版当嵌件用，而且全库零引用）—— 没有暗底、没有页签、没有关闭键。
            //    ⚠️ 它的根上**没有 `RankedRankingWindow`**（只有 `UIRankingListController` + 三个 provider）
            //    ⇒ 窗口字段（`type`/`placement`/…）**原版没有可抄** —— 我们按弹窗开它**只是为了能看/能验**，
            //    **这一条是我们挑的**（自检直接 `Create()`）。
            Debug.Log("[Leaderboard] `Ranked Leaderboard Display` 是**嵌入版**（原版零引用、没有任何窗口字段）——"
                      + "我们建它、但**不编入口**；`type/placement` 取弹窗值**是我们挑的**。");
            var content = Node(transform, "Content", EmbListR);
            Nine(transform, ArtPanel, EmbPanelR, PanelBorder, "Generic Window Red Background Big", QBg);
            MenuDraw.Text(transform, EmbTitleR, "TOP PLAYERS", Color.white, "Title", 55f, QText, EmbTitleR.W, 18f,
                          55f, 36f);   // 🆕 A336③：同上面那颗（`fs 55 auto[18~55] base 36`）
            MenuDraw.Rect(transform, Art(ArtLine), EmbTopBarR, "TopBar", QContent, null, false);
            BuildSeasonPieces(true);

            var sv = Node(content, "Scroll View", EmbListR);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：嵌入版这一棵**同样**把裁切状态挂在 `Viewport` 上
            //    （与三扇弹窗那一棵逐位同形，只差矩形）。⚠️ 两棵是**各自的节点** —— 一扇窗里挂几个
            //    `ViewportClip` 正是这套机制的意义所在（原版 `Collection Menu Variant` 一扇窗实读 6 个）。
            var vpVc = ViewportClip.Hang(sv, "Viewport", EmbListR, Vector4.zero, Vector2Int.zero);
            var vp = vpVc.transform;
            _scroll = MenuScroll.TopAligned(EmbListR, 0f);
            _scroll.ClipNode = vpVc;      // 🔴 A465（W-A435己）：构建循环那一路读同一颗节点 —— 同 `BuildPopup` 那条
            // 同为 **Elastic**（原版 `mode=1`）—— 判据：`menu_dump.py … "Ranked Leaderboard Display"` 实读
            // `Scroll View` = `h=0 v=1 mode=1`。嵌入版与三扇弹窗**只差高度**，滚动档位相同。
            _scroll.Elastic = true;
            _scroll.Owner = gameObject;
            _scroll.OnChanged = () => RebuildRows();
            PointerLayer.RegisterScroll(_scroll);
            _listContent = Node(vp, "Content", new PxRect(ListL, EmbListR.y1, ListR, EmbListR.y1));
            RebuildRows();
        }

        // ---------------------------------------------------------- 页签 / 关闭键 / 赛季件

        void BuildTabs()
        {
            var bar = Node(transform, "Tab Buttons", TabBarR);
            var specs = TabsOf(Kind);
            for (int i = 0; i < specs.Length; i++)
            {
                float t = TabTop + TabH * i, b = t + TabH;
                var r = new PxRect(TabL, t, TabR, b);
                var node = Node(bar, specs[i].Name, r);

                bool on = specs[i].Tab == CurrentTab;
                // `button_bg`：**换图不换色**（`onSprite`/`offSprite`），色恒 `(1,0.427,0,1)`
                var bgQ = Rect(node, on ? ArtTabOn : ArtTabOff, r, "button_bg", QBg, TabBgTint);
                var iconR = new PxRect(TabIconL, t + TabIconDy, TabIconR, t + TabIconDy + TabIconH);
                Rect(node, specs[i].Art, iconR, "Icon", QContent, null, true);
                // `Label`（页签文字层）**出厂就 inactive、而且全子树没有一处引用它**（正本 §A·1）
                // ⇒ 照纪律**不建**。页签是**纯图标**的。

                var tab = specs[i].Tab;
                bool wired = specs[i].Wired;
                // 🆕 A17：原版 `Tab Buttons>{RankedSkirmish…}` 是 Toggle，`m_SpriteState` 的**悬停图 = `…_selected`**
                //（`onSprite` 的 `_hover` 是**选中态**，见 §一；`WindowButton` 那张表里就是这个映射）
                MenuDraw.Hit(node, "Hit", r, QHit,
                             () => { if (wired) SelectTab(tab); else OnDeadTab(tab); }, bgQ, ArtTabOff);
                TabCount++;
            }
        }

        void BuildCloseButton()
        {
            var close = Node(transform, "Generic Close Button Orange", CloseR);
            // 三件**都是 `preserveAspect`**（正本 §A·4·7）；`Image` 自己的底图 **m_Enabled 是开的**
            //（与 `BattleLogPopup` 那颗不同 —— 那边底图 m_Enabled=0、只画两个子件）。
            // 🔴 换图落在**圆底那一层**（原版 `trans=2` 换的是它自己的 Image；三层 = 圆底 + 黄面 + 叉）
            var closeBaseQ = Rect(close, ArtCloseBg, CloseR, "Image", QBg, null, true);
            Rect(close, ArtCloseCircle, CloseInnerR, "Background", QContent, null, true);
            Rect(close, ArtCloseIcon, CloseInnerR, "Icon", QContent, null, true);
            // 🆕 A17：原版 `…>RankedSkirmishLeaderboardPopup` 那颗 `Generic Close Button Orange` 是 SpriteSwap、
            // 高亮图 = `40k_general_bt_yellow_hover`（直接读 prefab 核过）
            // 🔴 **2026-10-18（波 2a · W1 · 归真值）**：命中区 = **吃射线那两颗子件按它们自己的
            //   `m_RaycastPadding = (-20)⁴` 外扩**（`ClosePad`，**负 = 外扩**）⇒ **原版真值 96.86 × 98.13**。
            //   判据（第一权威 = 原版 prefab 实读，三扇弹窗逐位相同）→ `python -I d:/tmp/wf_hit/rcunion.py
            //   bundle_menus_assets_all "RankedSkirmishLeaderboardPopup" --depth 3`：
            //   根 `Generic Close Button Orange` 上的 `Image`（`UI_Button_Round_background`）**`m_RaycastTarget = 0`**
            //   ⇒ **原版那颗圆底盘不接受射线**；吃射线的是两个子件 `Background`（`40k_general_bt_yellow`）与
            //   `Icon`（`40k_general_bt_yellow_close`）—— **同一个矩形（`CloseInnerR` = 56.86 × 58.13）
            //   且两颗都带 pad `(-20)⁴`** ⇒ 可射线区 = 那颗外扩 20。
            //   ⚠️ **上一轮（E3b）漏了 `m_RaycastPadding`**，只拿 `CloseInnerR` 裸矩形 ⇒ 每边小 20px
            //      （那一版写「原版可射线区 = 56.86 × 58.13」**这句是错的**，铁律 5 就地订正）。
            //   ⛔ 外扩走 `MenuDraw.PaddedRect`；⛔ 悬停换图那一层（`closeBaseQ`）不动 —— 那是另一类缺陷。
            MenuDraw.Hit(close, "Hit", MenuDraw.PaddedRect(CloseInnerR, ClosePad), QHit, () => Close(), closeBaseQ, null, "40k_general_bt_yellow_hover");
        }

        /// <summary>`Generic Simplified UI Button_updated`（'Last season'）+ `Last Season Text`。
        /// 🔴 **两件都建、但都关着**（判据 ④）。`Timer` **整块不建**。</summary>
        void BuildSeasonPieces(bool embedded)
        {
            // 照原版的运行期规则算出来（**不是我们挑的显隐**）：
            //   `AllowChangeSeason(!string.IsNullOrWhiteSpace(previousSeasonLeaderboardKey))`，本地没有上一赛季榜
            SeasonButtonVisible = false;

            var btnR = embedded ? EmbBtnR : SeasonBtnR;
            var txR = embedded ? EmbBtnTxR : SeasonBtnTxR;
            var textR = embedded ? EmbSeasonTextR : SeasonTextR;

            var btn = Node(transform, "Generic Simplified UI Button_updated", btnR);
            var seasonBg = Nine(btn, ArtSeasonBtn, btnR, SeasonBtnBorder, "Image", QBg);
            // 🆕 **2026-10-12（A336③）**：`…/Generic Simplified UI Button_updated/Button Text` 实读
            //    = `fs 36.0 · auto[10~36] · base **12.0**` · 折行 0（上限 = 标称 ⇒ A333 本来就对）。
            MenuDraw.Text(btn, txR, "Last season", Color.white, "Button Text", 36f, QText, txR.W, 10f, 36f, 12f);
            // 🆕 A17：原版这一颗是 SpriteSwap（普查 §块 5 第 2 行）；底图是**九宫格** ⇒ 九张一起换
            var seasonHit = MenuDraw.Hit(btn, "Hit", btnR, QHit, OnSeasonButton);
            var seasonWb = seasonHit != null ? seasonHit.GetComponent<WindowButton>() : null;
            if (seasonWb != null) seasonWb.BindNine(seasonBg, ArtSeasonBtn);
            btn.gameObject.SetActive(SeasonButtonVisible);

            var st = Node(transform, "Last Season Text", textR);
            // 🆕 **2026-10-12（A336③）**：`…/Last Season Text` 实读
            //    = `fs 40.0 · auto[18~40] · base **50.0**` · 折行 0 —— ⚠️ base **50 比标称 40 还大**，
            //    且**与同窗那颗 `Button Text`（12）完全不同** ⇒ 逐个读（铁律 5·c）。
            var lb = MenuDraw.Text(st, textR, "Last season", Color.white, "Last Season Text", 40f, QText,
                                   textR.W, 18f, 40f, 50f);
            if (lb != null) MenuDraw.AlignRight(lb, textR);
            st.gameObject.SetActive(false);     // 原版 `ChangeSeasonRankingView(true)` 把它关掉
        }

        void OnSeasonButton()
        {
            Debug.Log("[Leaderboard] `Last season` 键 —— 原版切到**上一赛季**的榜（服务器数据）。"
                      + "本地没有赛季、也没有上一赛季榜 ⇒ 如实说明。");
            if (Manager != null)
                // 🆕 2026-10-18（双语③ P2）：钮文案改走语言表；键 = 原版 mTerm `MainMenu/General/OK`
                //   （`Core/Loc.cs` 的表，大写 `OK`）。
                // 🆕 2026-10-18（波 1b · P2b）：正文也改走语言表 —— 键 `MainMenu/RankedWindow/LeaderboardOfflineNote`
                //   是波 0b 补进表的那 111 条之一（值**逐字 = 原来那两句** ⇒ 中文档零变化）。
                Manager.ShowPopUp(Loc.T("MainMenu/RankedWindow/LeaderboardOfflineNote"),
                                  Loc.T("MainMenu/General/OK"), null);
        }

        // ---------------------------------------------------------- 列表

        void RebuildRows()
        {
            if (_listContent == null) return;
            MenuDraw.ClearChildren(_listContent);
            BuiltRows = 0;

            var rows = LeaderboardData.Rows(Kind, CurrentTab);
            float top = _scroll != null ? _scroll.Viewport.y1 : 0f;
            // 🔴 2026-10-03（本件 ①）：**内容高要写进滚动区**。此前 `MenuScroll.TopAligned(ScrollR, 0f)`
            //    之后再没人设过 `ContentX2` ⇒ `ContentX1 == ContentX2 == Viewport.y1` ⇒ `ClampLo == ClampHi == 0`
            //    ⇒ **这一格根本滚不动**（滚轮/拖拽全被夹回 0）—— 而「整行滚出视口 ⇒ 不建」那条刚加上，
            //    第 7 行起就从「画到框外（至少看得见）」变成「**完全不存在**」⇒ 不修就是**丢数据**。
            //    判据 = 原版这一格是 `ScrollRect h=0 v=1 mode=1` + 内层 `Content` 挂 `ContentSizeFitter VerticalFit=1`
            //    （实据 → `资料/普查产出_0927/排行榜_嵌入版与行族.md:38,40,61`）⇒ 可滚范围 = **内容高 − 视口高**。
            //    形状照别的同类页（`BattleLogTab.cs:164` · `RankedTab.cs:317` · `CollectionWindow.cs:1788` …）。
            //    ⚠️ 空数据那一支也要写（写成 0 高）—— 否则上一次的内容高会留在区里，是个静默的脏值。
            if (_scroll != null)
                _scroll.ContentX2 = top + (rows.Count == 0 ? 0f
                                        : rows.Count * LeaderboardRow.RowH
                                          + (rows.Count - 1) * LeaderboardRow.RowGap);
            if (rows.Count == 0)
            {
                Debug.Log("[Leaderboard] `" + NameOf(Kind) + "` / `" + CurrentTab + "` —— 本地**没有榜单数据**"
                          + "（原版读服务器：`LeaderboardManager` / 各 `RankingDataProvider`）⇒ **照原版留空**"
                          + "（四棵榜都没有空态节点，我们**不自己造**）。");
                return;
            }

            _rowCtx.Art = Art;
            _rowCtx.Q = QRow;
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：`_rowCtx.Clip` **本窗不再写**（原来是
            //    `= _scroll.Viewport` / 末尾清 `null` 那一对）—— 裁切状态已经长在两棵 `Viewport` 节点上
            //    （`BuildPopup` 那颗 / `BuildEmbedded` 那颗），`LeaderboardRow` 传的 `null`
            //    会沿父链解析到**它自己那一棵的那颗**（⚠️ 两棵各自记名，别想成「一扇窗一份」）。
            var fam = FamilyOf(Kind, CurrentTab);
            for (int i = 0; i < rows.Count; i++)
            {
                float y = top + i * (LeaderboardRow.RowH + LeaderboardRow.RowGap);
                var rr = new PxRect(ListL, y, ListR, y + LeaderboardRow.RowH);
                if (_scroll != null) rr = _scroll.Shift(rr);
                // 🆕 2026-10-03：**整行滚出视口 ⇒ 连节点一起不建**。
                // 🔴 这才是「榜单行会画到视口外」的**根因** —— `RowCtx.Clip` 只管「压在视口边上」那一档
                //    （截矩形/截 uv），整行在外的那一档得在这里挡掉（顺带它的点击区也不存在）。
                // 形状照 `CollectionWindow` 卡组页那一份（`CollectionWindow.cs:1834`）：`_scroll` **可空** ⇒ 判空 + `Intersects`。
                // ⚠️ **2026-10-07 更正（铁律 5 / A12①）**：原文写「`Intersects` 只判**滚动轴**（纵向 = y），
                //    横向不判 —— 与其它页逐字一致，**别在这里加第二条判据**」—— **已过期**：`Intersects` 现在
                //    就是 `MenuDraw.Visible` 的转发（**两轴都判**，判据 = 原版 `RectMask2D` 四边都裁）。
                //    本行照旧不加第二条判据，但**理由换了**：不是「它不管横轴」，而是「两轴都由那一份管」。
                //    这里横轴恒相交（行 x = `ListL..ListR` = 360..1560 ⊆ 视口 248.99..1671.01，嵌入版同理）。
                if (_scroll != null && !_scroll.Intersects(rr)) continue;
                LeaderboardRow.Build(_rowCtx, _listContent, rr, rows[i], fam);
                BuiltRows++;                     // 现在 = **真建出来几行**（滚出视口的不算；断言用）
            }
        }
    }
}
