// WhereToGetCardsPopup.cs — **卡牌来源窗**（原版 `WhereToGetCardsWindow : GameWindow`，prefab 根名 `WhereToGetCardsPopup`）
//
// ============================ 出处（唯一判据，全部本地现读 · 2026-10-17）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "WhereToGetCardsPopup" --depth 11 --relative`
//   （`--depth 5` 会把 `Section For Reference` 的 40 个节点截断 —— 本文件所有几何都取自 `--depth 11`）
// · 窗参（根 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6694678526659093809.json` 逐字段实读）：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1` · `openSound = {0,0}` · `closeSound = {0,0}`
//     字段 PPtr：`title = 7549437076281976113` · **`sectionReference = -2154387889982839503`**（= 本 prefab **树内**的模板节点，
//     不是外部 prefab）· `sectionsAnchor = 6988725310366967089` · `closeButton = -5865340436816829135` ·
//     `backgroundCloseButton = 2649394885390853425` · `layoutGroupContentFixer = -4340737945003399887`
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/`，方法体**都在**）：
//   · `WhereToGetCardsWindow__Open.c` 逐句：
//       ① `GameWindow.Open()`
//       ② `context != null && context.sections.Count > 0` ⇒ `Reset()`（销毁已建的段并清表）
//       ③ `sectionReference.gameObject.SetActive(false)` —— **模板节点原版一开工就关**
//       ④ `title.text = I2(context + 0x10)`（= `WhereToGetExpansionCardsData` 的标题词条）
//       ⑤ 逐条 `Instantiate(sectionReference, sectionsAnchor)` → **新段 `SetActive(true)`** →
//          `WhereToGetCardsSection.Initialize(段, context.sections[i], 事件回调)`
//   · `WhereToGetCardsSection__Initialize.c`：段标题 = I2(段数据 + 0x10)；逐条卡片引用
//       `Instantiate(cardDrawerReference, contentAnchor)` → `CardDrawer.SetCard(card, true)` →
//       `ToggleNewCardBadge` → `InventoryManager.HasItem` 计数 → `ToggleGreyScale(!owned)`；
//       末尾 `counter.text = string.Format(格式串, 已拥有数, 总数)`（prefab 出厂字是 `5/5` ⇒ 形如 `{0}/{1}`）。
//   · `WhereToGetCardsSection__Awake.c`：`toggle.onValueChanged += …` ⇒ `SetVisibility`（整段卡片区开/关）。
//   · `WhereToGetCardsWindow__Close.c`：`base.Close()`（= 播关窗音 + 置灰 + 摘表）→ 逐段 `Release()`。
//   · `WhereToGetCardsWindow__RefreshLayout.c`：`layoutGroupContentFixer.FixLayout()`。
//   🔴 **入口**（不是猜的）：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ExpansionPassEventTab.cs:25` 与
//      `GachaEventTab.cs:20` 各有一个 `private AssetReferenceTyped<WhereToGetCardsWindow> getCardsWindow;`
//      —— 原版由**事件页那两个 tab** 按这个 AssetReference 开出来。
//      🔴 **2026-10-17 就地订正（铁律 5）**：原文写「我们**没有建承载窗 `Parent Event Window`**」—— **已过期**：
//      承载窗当天已建（`Shell/ParentEventWindow.cs`，界面线 `T28`）。**但入口仍接不上**：
//      ① 现读 `T28` 建的那两页（`TwoSidesEvent_ChooseSideTab` / `TwoSidesEvent_OnGoingTab`）**没有 `getCardsWindow` 字段**；
//      ② 真正的消费者是 `ExpansionPassEventTab` / `GachaEventTab` —— 那两页**还没建**（活动页签树缺，见 `A1853②`）。
//      ⇒ 本窗**只建 + 注册**（`WindowsManager.OpenWhereToGetCards`），⛔ **不伪造入口**（同 `TutorialMessagePopup` / `T26` 口径）。
//
// ============================ 🔴 数据缺口（已查实，⛔ 不编来源）============================
// 「这张卡从哪些扩展包 / 活动能拿到」的**运行时数据本地没有**：
//   · `CardsSpecialConfig.WhereToGetCardsData` 是那扇门的属性（`CardsSpecialConfig__WhereToGetCardsData.c` 只有一句 `return *(this+0x28)`）；
//   · 全 `assets_full` 84 个 bundle 的 `MonoBehaviour/*.json` 逐字节搜 `CardsSpecialConfig` / `WhereToGetCardsData`
//     均 **0 命中**（判据 = 本任务简报的现核结论；`CardsSpecialConfig.WhereToGetCardsData` 那个属性体本身只剩一句
//     `return *(this+0x28)` ⇒ 数据来自运行时 SO，不在离线包体里）。
// ⇒ 本窗在**没有数据**时：段列表**留空**（只保留原版那个模板节点、并按原版 `SetActive(false)`）+ `Debug.LogWarning` 一次。
// ⛔ **不编来源、不拿别的表顶**。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **`Menu Dark Background` 的关闭语义 = 关窗**（`MenuDraw.ShadeHit`）。`BackgroundCloseButton.window` 的 PPtr 是 **0**、
//   `onClick` 的**序列化**监听表也空（MB `2649394885390853425` 实读）—— 但 `__OnPointerClick.c:27` **无条件** `onClick.Invoke()`，
//   而 `WhereToGetCardsWindow__Awake.c:12-24` 运行期往那张表里挂了监听器（`+0x1c0` = vtable 槽 `+0x1b8` 的 methodInfo，同槽 = `Close`）。
//   🔴 **2026-10-26 订正**：`N51③` 判「点暗底什么都不发生」只读了序列化监听表、漏了 `Awake` 那条 ⇒ 已改回关窗。
// · **段模板的卡片格**：原版那一格是 `Card Drawer`（`CardDrawer + ItemDrawerComponents`，ARF 高控宽 0.66）。
//   ✅ **2026-10-17（`N51②`）已画**：按 `CardRefs` 在本地卡池里查（`CardDatabase.Find`）→ `CardView`；
//   查不到的格如实计数 + 出声（⛔ 不假装画了）。
// · **折叠（`Toggle`）**：✅ **2026-10-17（`N51②`）已重排** —— 收起后下面的段按高度差上移、
//   滚动内容高同步（原版 `VerticalLayoutGroup` + `ContentSizeFitter` 的等价物）。
// · **垂直档**：TMP 的 `VerticalAlignmentOptions` = Top 0x100 · Middle 0x200 · Bottom 0x400 ·
//   **Baseline 0x800(2048)** · **Midline 0x1000(4096)** · **Capline 0x2000(8192)**。
//   原版 `Counter` = **4096 = Midline**、`Title` = **8192 = Capline**（旧注释把后者记成 Midline，2026-10-17 已订正）。
//   这两档我们都有 ⇒ 逐颗按值接。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;      // `CardDatabase` / `CardDef`（来源段的卡片格）

namespace CardPresentation
{
    /// <summary>原版 `WhereToGetCardsWindow` —— **卡牌来源窗**（标题 + 一列可折叠的「来源段」）。
    /// <para>🔴 **本机没有来源数据**（`CardsSpecialConfig` 0 命中）⇒ 无数据时**段列表留空 + 出声**，
    /// ⛔ 不编来源（判据 → 文件头）。</para>
    /// <para>🔴 **本地没有生产入口**（原版由事件页两个 tab 的 `getCardsWindow` AssetReference 开）⇒
    /// 本窗只提供「怎么开」，⛔ 不伪造调用点（判据 → 文件头）。</para></summary>
    public class WhereToGetCardsPopup : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3479–3487）
        // 相邻带：`RankedRewardEventWindow` 3460–3478 · `LeaderboardWindow` 3500–3520
        //（层带不许重叠；档位口径 → `Shell/BattleLogPopup.cs` 里那段同族注释）。
        public const int QShade = 3479,       // `Menu Dark Background`（含「点暗底关窗」那颗命中区）
                         QPanel = 3480,       // `Generic Window Red Background Big` 九宫底
                         QHeaderBg = 3481,    // 每个来源段的 `Header/Background` 渐变条
                         QText = 3482,        // 大标题 / 段标题 / 计数
                         QToggle = 3483,      // 折叠箭头
                         QClose = 3484;       // 关闭钮（圆底 + 两个子件）
        /// <summary>本窗**内容命中区**那一档（压暗层命中区必须严格低于它；吸收层由 `MenuDraw.Absorb` 取 `QHit − 1`）。
        /// 命中区当前只有两颗：关窗钮、段折叠箭头（模板节点关着 ⇒ 不建命中区）。</summary>
        public const int QHit = 3487;

        // ============================================================ 根 / 节点名
        /// <summary>prefab 根节点名（= `Create()` 里 `new GameObject(...)` 用的那个名字，**两处同源**）。</summary>
        public const string RootName = "WhereToGetCardsPopup";

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px · 左上原点）
        /// <summary>`Menu Dark Background`（4574.60×2572.36）。⚠️ 它的中心在 x = 997.0（**不是 960**）——
        /// 直接照 dump 四边写，⛔ 别按「居中」反推（同族别的窗居中、这一扇不是）。</summary>
        public static readonly PxRect ShadeR = new PxRect(-1290.30005f, -746.17761f, 3284.30005f, 1826.18250f);
        /// <summary>`Menu Dark Background/Image.m_Color`（实读 (0,0,0,0.772549)）。</summary>
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.773f);

        /// <summary>`Generic Window Red Background Big`（1510.6368×990.6129）· 九宫 `UI_Deck_Information_Back` 1100×701 · border (42,363,655,81)。</summary>
        public static readonly PxRect PanelR = new PxRect(204.68158f, 16.32612f, 1715.31842f, 1006.93902f);
        public const string ArtPanel = "UI_Deck_Information_Back";
        public const float PanelTexW = 1100f, PanelTexH = 701f;
        public static readonly Vector4 PanelBorder = new Vector4(42f, 363f, 655f, 81f);

        /// <summary>`Title`（1354.0997×109.57 · fs72 · 基准 36 · auto[18~72] · Center/Midline · 折行）。</summary>
        public static readonly PxRect TitleR = new PxRect(282.95013f, 36.957523f, 1637.04987f, 146.52752f);
        public const float TitleFont = 72f, TitleFontMin = 18f, TitleFontMax = 72f, TitleFontBase = 36f;
        /// <summary>prefab 出厂文案（原版运行期用 `context + 0x10` 的词条覆盖它；MB `m_text` 实读）。</summary>
        public const string PrefabTitle = "Where to get Beast Snaggas cards";

        /// <summary>`TopBar`（1354.10×6 · `40k_main_line` 171×6 · 九宫 (80,0,80,0)）。</summary>
        public static readonly PxRect TopBarR = new PxRect(282.95001f, 141.6326f, 1637.04999f, 147.6326f);
        public const string ArtLine = "40k_main_line";
        public const float LineTexW = 171f, LineTexH = 6f;
        public static readonly Vector4 LineBorder = new Vector4(80f, 0f, 80f, 0f);

        /// <summary>`Scroll View`（1276.942×803.7156 · ScrollRect `h=0 v=1` · `mode=1` Elastic · inertia 1 ·
        /// elasticity 0.1 · decel 0.135 · **灵敏度 1.0**）。⚠️ 它自己的 `Image.m_Enabled = 0` ⇒ **底图不画**。</summary>
        public static readonly PxRect ScrollR = new PxRect(304.52881f, 159.2637f, 1581.47083f, 962.97934f);
        public const float ScrollNotchPx = 1.0f;
        /// <summary>`Viewport`（`Image` α=0 + `RectMask2D`，padding 0 · softness (0,0)）—— 与 `Scroll View` 同矩形。</summary>
        public static readonly PxRect ViewportR = ScrollR;

        /// <summary>`Content`（VLG）：**比视口宽**（x 248.9487..1637.0487 = 1388.10，左右各溢出 55.6）—— 原版如此
        /// （childAlignment = 4 MiddleCenter；段宽 1250.4928 在正中，段左缝 317.7523 / 右缝 1568.2451）。
        /// 纵向：spacing 50 · padding (0,0,0,50) · CSF `v:MinSize=50`。</summary>
        public const float ContentL = 248.94873f, ContentR = 1637.04871f, ContentTop = 159.2637f;
        public const float ContentSpacing = 50f, ContentPadBottom = 50f, ContentMinH = 50f;

        /// <summary>段（`Section For Reference`）：宽 **1250.4928** · padding (0,0,2,5) · spacing 0 ·
        /// CSF `v:MinSize = 59.496`（= 2 + 52.496 + 5，模板无卡片时就是这个高度）。</summary>
        public const float SectionX1 = 317.75232f, SectionX2 = 1568.24512f;
        public const float SectionPadTop = 2f, SectionPadBottom = 5f;
        public const float SectionMinH = 59.496f;
        /// <summary>段内 `Header`：**52.496** 高（= `Background` / `Title` / `Counter` / `Toggle` 的那一行）。</summary>
        public const float HeaderH = 52.496f;

        /// <summary>`Header/Background` 的 `Gradient2`（MB `-7264030875359877839` 实读）：
        /// `_gradientType = 0`（**横向**）· `_blendMode = 2`（乘）· `_modifyVertices = 1` · `_offset = 0` · `_zoom = 1` ·
        /// 色键 key0 = (0.556863, 0.110495, 0.098039, 1) · key1 = (0,0,0,0)（`numColorKeys = numAlphaKeys = 2`）。
        /// ⇒ **左端暗红不透明 → 右端透明**（`Gradient2.ModifyMesh` 的 t = (坐标 − bounds.min)/尺寸，t=0 在 min 一侧）。
        /// ⚠️ 那颗 `Image` 的 `m_Sprite = 0`（纯白面板被顶点色染）⇒ 我们用 `CardArt.Solid()` + `SetCornerColors`。</summary>
        public static readonly Color HeaderGradLeft = new Color(0.556863f, 0.110495f, 0.098039f, 1f);
        public static readonly Color HeaderGradRight = new Color(0f, 0f, 0f, 0f);

        /// <summary>`Header/Title`（`Booster packs` · fs42 · Left/Midline · 折行；dump 宽 **1219.69**）。
        /// ⚠️ 它的框右沿 1594.84 **超出段右沿 1568.25**（原版 prefab 就这么写的，文字左对齐从 375.15 起排
        /// ⇒ 可见字仍落在段内）。</summary>
        public const float HeaderTitleX1 = 375.1523f, HeaderTitleX2 = 1594.8423f;
        /// <summary>`Header/Counter`（`5/5` · fs42 · Right/**Midline**；dump 宽 **1366.64**）。
        /// 🔴 **2026-10-17（第四十八会话 · `N51④`）就地订正（铁律 5）**：原账把 `m_VerticalAlignment = 4096`
        /// 记成 **Baseline** —— **错**，TMP 枚举里 **`0x800 = Baseline`、`0x1000(=4096) = Midline`**。
        /// 我们**已经有 `Midline` 这一档** ⇒ 按它接。
        /// ⚠️ 框左沿 201.60 在段外，右对齐到 1568.245（= 段右沿）。</summary>
        public const float CounterX1 = 201.605f, CounterX2 = 1568.245f;
        public const float HeaderFont = 42f;
        public const string TemplateSectionTitle = "Booster packs";
        public const string TemplateCounterText = "5/5";

        /// <summary>`Header/Toggle`（42.2985×30.5648；实读矩形 x 324.0523..366.3508 —— 纵向在 Header 里**居中**：
        /// 原版 raw `m_AnchoredPosition.y` 给的中心恰好 = Header 中心）。`EverguildToggle`（MB `6144913215790212401`）实读：
        /// `m_IsOn = 1` · `onSprite = 40K_dropdown_arrow_opened` · `offSprite = 40K_dropdown_arrow_closed` ·
        /// `onColor = 白` · `offColor = (0.75,0.75,0.75,1)` · `m_onClickSound = ButtonClick`（R3 §二 实读）。</summary>
        public const float ToggleX1 = 324.0523f, ToggleX2 = 366.3508f, ToggleH = 30.5648f;
        public const string ArtToggleOn = "40K_dropdown_arrow_opened";
        public const string ArtToggleOff = "40K_dropdown_arrow_closed";
        public static readonly Color ToggleOnTint = Color.white;
        public static readonly Color ToggleOffTint = new Color(0.75f, 0.75f, 0.75f, 1f);

        /// <summary>段内 `Content`（`EverguildGridLayoutGroup` 实读）：cell 206.667×290.4 · spacing (25,35) ·
        /// padding (0,0,40,0) · `m_Constraint = 0`(Flexible) · `m_ConstraintCount = 2`（Flexible 下不生效）·
        /// childAlignment 0(UpperLeft)。模板无卡片 ⇒ 高 **0**。</summary>
        public const float GridCellW = 206.667f, GridCellH = 290.4f;
        public const float GridSpaceX = 25f, GridSpaceY = 35f, GridPadTop = 40f;

        /// <summary>`Generic Close Button Orange`（**74.386×75.605**）· 根图 `UI_Button_Round_background`（237×237，Simple，
        /// preserveAspect，**`m_RaycastTarget = 0`**）· 两个子件同矩形 **56.8636×58.1283**（`40k_general_bt_yellow` /
        /// `40k_general_bt_yellow_close`，**`m_RaycastTarget = 1` + `m_RaycastPadding = (-20)⁴`**）。</summary>
        public static readonly PxRect CloseR = new PxRect(1656.80701f, 9.19994f, 1731.19300f, 84.80494f);
        public static readonly PxRect CloseChildR = new PxRect(1664.95831f, 17.17841f, 1721.82193f, 75.30669f);
        public static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);
        public const string ArtCloseRound = "UI_Button_Round_background";
        public const string ArtCloseBg = "40k_general_bt_yellow";
        public const string ArtCloseIcon = "40k_general_bt_yellow_close";

        // ============================================================ 数据（= 原版 WhereToGetExpansionCardsData 的对位）
        /// <summary>一个「来源段」（原版 `WhereToGetExpansionCardsData/sections[i]`）。
        /// <para>原版的段数据：`+0x10` = 标题 I2 词条 · `+0x18` = `List&lt;LoadableReference&lt;Card&gt;&gt;`；
        /// 计数 = `InventoryManager.HasItem` 命中数（**服务端**）。</para></summary>
        public struct SectionData
        {
            /// <summary>原版 `+0x10` 的 I2 词条键（空 ⇒ 用 <see cref="TitleFallback"/>）。</summary>
            public string TitleTerm;
            /// <summary>词条取不到时显示的字（⛔ 别拿它冒充「原版文案」）。</summary>
            public string TitleFallback;
            /// <summary>原版 `+0x18` 列表长度（= 这一段一共有几张卡）。</summary>
            public int TotalCards;
            /// <summary>原版 `InventoryManager.HasItem` 命中数（= 已拥有几张）。</summary>
            public int OwnedCards;
            /// <summary>原版 `LoadableReference&lt;Card&gt;` 的原始 id / 名（本机**解析不了** —— 见文件头的数据缺口）。</summary>
            public string[] CardRefs;
        }

        /// <summary>整窗内容（原版 `WhereToGetCardsWindow.context`）。</summary>
        public struct Context
        {
            /// <summary>原版 `+0x10`：大标题的 I2 词条键（空 ⇒ prefab 出厂文案）。</summary>
            public string TitleTerm;
            /// <summary>原版 `+0x18`：来源段列表（空 / null ⇒ **无数据那一档**：0 段 + 出声）。</summary>
            public SectionData[] Sections;
        }

        /// <summary>出厂值 = prefab 那一份（标题文案照 MB 原文，段列表**空**）。</summary>
        public static Context DefContext()
        {
            return new Context { TitleTerm = null, Sections = null };
        }

        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        /// <summary>最近一次开出来的那一扇（自检读口，同 `ItemDisplayWindow.LastOpened`）。</summary>
        public static WhereToGetCardsPopup LastOpened { get; private set; }

        Context _ctx = DefContext();
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public Context Ctx { get { return _ctx; } }

        /// <summary>当前真的建出了几段（模板节点不算）。</summary>
        public int SectionsShown { get; private set; }
        /// <summary>「来源数据本地没有」那一档（= `Sections` 空 / null）。</summary>
        public bool DataMissing { get; private set; }
        /// <summary>数据缺口那句话（自检读口；空 = 没缺口）。</summary>
        public string DataNote { get; private set; }

        // 自检读口（节点 / 组件引用）
        Transform _shade, _panel, _titleNode, _topBar, _scrollNode, _viewport, _content, _sectionTemplate, _close, _closeHit;
        Transform _toggleNode;          // 模板段的折叠箭头
        ImageQuad _toggleQuad;
        Label _title;
        MenuScroll _scroll;
        ViewportClip _clip;
        readonly List<Transform> _sectionNodes = new List<Transform>();
        readonly List<SectionView> _views = new List<SectionView>();

        /// <summary>每一段自己的折叠状态（模板段不在此列）。</summary>
        class SectionView
        {
            public Transform Node, Grid;
            public ImageQuad ToggleQuad;
            public bool On = true;
        }

        // ============================================================ 开
        /// <summary>建一扇（`WindowsManager.OpenWhereToGetCards` 的 `create` 那一跳）。</summary>
        public static WhereToGetCardsPopup Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);                 // 节点名照原版（`OpenByRef` 的键同源）
            var win = go.AddComponent<WhereToGetCardsPopup>();
            win.type = WindowType.Popup;                  // 实读 `type = 1`
            win.placement = WindowsPlacement.Popup;       // 实读 `windowsPlacement = 15`
            win.closeOnEsc = true;                        // 实读 `closeOnESC = 1`
            win.updateNavPanel = false;                   // 实读 `updateNavPanel = 0`
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            // `openSound` / `closeSound` 实读都是空 PPtr、`useDefaultCloseSoundIfNull = 1`
            //   ⇒ 一个 clip 都不赋，关窗音走 `GameWindow.Close()` 的家族默认（`ButtonClick Back`）。
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[WhereToGetCards] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        /// <summary>换一份内容（复用同一扇窗时用；同 `ItemDisplayWindow.Show`）。
        /// <para>⚠️ `GameWindow.TryOpen` 在 `CurrentState == Open` 时照原版**早退**（不重建），
        /// 所以复用那一支必须显式调本方法。</para></summary>
        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            Build();
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            PointerLayer.UnregisterOwnedBy(gameObject);       // 重建 ⇒ 旧的滚动条目是死条目
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _shade = _panel = _titleNode = _topBar = _scrollNode = _viewport = _content = null;
            _sectionTemplate = _close = _closeHit = _toggleNode = null;
            _toggleQuad = null;
            _title = null;
            _scroll = null;
            _clip = null;
            _sectionNodes.Clear();
            _views.Clear();
            SectionsShown = 0;

            // ---- 1) `Menu Dark Background`（压暗 + 「点窗外关窗」）----
            _shade = MenuDraw.Node(transform, "Menu Dark Background", ShadeR);
            MenuDraw.Rect(_shade, CardArt.Solid(), ShadeR, "Image", QShade, ShadeTint);
            //   原版 prefab 的 `BackgroundCloseButton.window` 是**空 PPtr**、`onClick` 的**序列化**监听表也空 ——
            //   **但那不是「什么都不发生」**：`__OnPointerClick.c:24-27` = `if (window != null) window.Close(); onClick.Invoke();`
            //   （`onClick` 是序列化 `UnityEvent`、永不为 null ⇒ `Invoke()` **无条件跑**），而 `WhereToGetCardsWindow__Awake.c:12-24`
            //   运行期挂了监听器：`*param_1 + 0x1c0` = vtable 槽 **`+0x1b8` 的 methodInfo**（同槽 = `Close`，`OnPointerClick` 自己调 `window.Close()` 也走它）
            //   ⇒ **点暗底 = 关窗**。🔴 2026-10-26 订正 `N51③`：它只读了序列化监听表、漏了 `Awake` 那条。
            MenuDraw.ShadeHit(_shade, ShadeR, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `Generic Window Red Background Big`（九宫底；`Image.m_RaycastTarget = 1` ⇒ 吃点击）----
            _panel = MenuDraw.Node(transform, "Generic Window Red Background Big", PanelR);
            var panelTex = Tex(ArtPanel, "`Generic Window Red Background Big` 的九宫底");
            MenuDraw.Nine(_panel, panelTex, PanelR, PanelBorder, PanelTexW, PanelTexH, QPanel,
                          Color.white, true, "Image");
            MenuDraw.Absorb(transform, "AbsorbHit", PanelR, QShade, QHit);

            // ---- 3) `Title`（fs72 · auto[18~72] · Center/Midline）----
            _titleNode = MenuDraw.Node(transform, "Title", TitleR);
            _title = MenuDraw.TextBox(_titleNode, TitleR, ResolvedTitle(), Color.white, "Text",
                                      TitleFont, TitleFontMin, QText, TitleFontMax, TitleFontBase, face: TmpFont.Face.LatinUi);
            // 原版 `m_VerticalAlignment = 8192` = **Capline**（不是 Midline · TMP 枚举见文件头）· 横向 2（Center）
            MenuDraw.SetVAlign(_title, Label.VAlign.Capline, TitleR);

            // ---- 4) `TopBar`（`40k_main_line` 九宫）----
            _topBar = MenuDraw.Node(transform, "TopBar", TopBarR);
            MenuDraw.Nine(_topBar, Tex(ArtLine, "`TopBar` 的分隔线"), TopBarR, LineBorder,
                          LineTexW, LineTexH, QPanel, Color.white, true, "Image");

            // ---- 5) `Scroll View` → `Viewport`（RectMask2D）→ `Content`（VLG）----
            //   ⚠️ `Scroll View` 自己的 `Image.m_Enabled = 0` ⇒ **底图不画**（只建节点）。
            _scrollNode = MenuDraw.Node(transform, "Scroll View", ScrollR);
            _clip = ViewportClip.Hang(_scrollNode, "Viewport", ViewportR, Vector4.zero, Vector2Int.zero);
            _viewport = _clip.transform;
            //   原版 `ScrollRect`：`h = 0` · `v = 1` · `mode = 1`(Elastic) · inertia 1 · elasticity 0.1 · decel 0.135
            //   · `m_ScrollSensitivity = 1.0`（判据 → `资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md`）。
            _scroll = MenuScroll.TopAligned(ViewportR, ContentMinH, ScrollNotchPx);
            _scroll.Owner = gameObject;
            _scroll.ClipNode = _clip;
            _scroll.Elastic = true;                       // `m_MovementType = 1`（UGUI: Elastic = 1）
            _scroll.Inertia = true;                       // `m_Inertia = 1`
            _scroll.OnChanged = BuildSections;            // 滚轮只改 Offset、画是调用方的事
            SocialPage.RegisterScroll(_scroll);           // 走会出声的那条（`PointerLayer.Instance` 不在时告警）
            _content = MenuDraw.Node(_viewport, "Content", ContentRect(ContentMinH));
            BuildSections();

            // ---- 6) `Generic Close Button Orange`（圆底 + 底 + 叉；命中区 = 两个子件外扩 20）----
            _close = MenuDraw.Node(transform, "Generic Close Button Orange", CloseR);
            MenuDraw.Rect(_close, Tex(ArtCloseRound, "关窗钮的圆底"), CloseR, "Image", QClose, null, true);
            var closeBg = MenuDraw.Rect(_close, Tex(ArtCloseBg, "关窗钮的黄色底"), CloseChildR, "Background",
                                        QClose, null, true);
            MenuDraw.Rect(_close, Tex(ArtCloseIcon, "关窗钮的叉"), CloseChildR, "Icon", QClose + 1, null, true);
            // 命中区 = 两个 `m_RaycastTarget = 1` 子件的并集，各自按 `m_RaycastPadding = (-20)⁴` 外扩
            //   （口径 → `资料/普查产出_1017/UI/*` 命中区族那条；算式只走 `MenuDraw.PaddedRect`）。
            _closeHit = MenuDraw.Hit(_close, "Hit", MenuDraw.PaddedRect(CloseChildR, ClosePad), QHit,
                                     () => Close(), closeBg, ArtCloseBg, null, null,
                                     clickCue: WindowSounds.DefaultCloseCue);

            // ---- 7) 数据缺口（红线：不许静默失败）----
            DataMissing = _ctx.Sections == null || _ctx.Sections.Length == 0;
            if (DataMissing)
            {
                DataNote = "本机**没有**「这张卡从哪些包/活动能拿到」的数据："
                         + "`CardsSpecialConfig.WhereToGetCardsData` 是全 `assets_full` 的 0 命中"
                         + "（`MonoBehaviour/*.json` 逐字节搜 `CardsSpecialConfig` / `WhereToGetCardsData`）"
                         + " ⇒ 段列表**留空**（只保留原版模板节点、按原版 `SetActive(false)`），⛔ 不编来源。";
                Debug.LogWarning("[WhereToGetCards] " + DataNote);
            }
            else if (DataNote != null)
            {
                DataNote = null;
            }

            if (MissingArt.Count > 0)
                Debug.LogWarning("[WhereToGetCards] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>整窗标题：原版 `title.text = I2(context + 0x10)`；没给词条 / 取不到 ⇒ prefab 出厂文案
        /// （`Loc.BulkOr` 在键缺失时会自己出声）。</summary>
        string ResolvedTitle()
        {
            if (string.IsNullOrEmpty(_ctx.TitleTerm)) return PrefabTitle;
            return Loc.BulkOr(_ctx.TitleTerm, PrefabTitle);
        }

        // ============================================================ 段列表（滚动重建）
        /// <summary>`Content`（VLG）的矩形：宽 1388.10 固定、高按内容算（CSF `v:MinSize = 50`）。</summary>
        static PxRect ContentRect(float h)
        {
            return new PxRect(ContentL, ContentTop, ContentR, ContentTop + h);
        }

        /// <summary>重建段列表（原版 `Reset()` + 逐条 `Instantiate` 的等价物）。
        /// 🔴 **模板节点永远建、永远关着** —— 原版 `Open()` 第 ③ 句 `sectionReference.gameObject.SetActive(false)`。</summary>
        void BuildSections()
        {
            if (_content == null) return;
            MenuDraw.ClearChildren(_content);
            _sectionNodes.Clear();
            _views.Clear();
            SectionsShown = 0;

            // 🔴 **先算总高、把 `Content` 的矩形定下来，再建子件** —— `MenuDraw.Local` 用的是**父节点的当前世界位置**
            //   （`MenuDraw.cs`）⇒ 先建子件、后改 `Content` 的高度会让父节点中心移动，
            //   而子件的 `localPosition` 已经按旧中心写死 ⇒ **整列段会整体偏移**（空数据时恰好 50 = 50 看不出来）。
            var secs = _ctx.Sections;
            float totalH = 0f;
            if (secs != null)
            {
                for (int i = 0; i < secs.Length; i++)
                {
                    if (i > 0) totalH += ContentSpacing;      // VLG: spacing 50
                    totalH += SectionHeight(secs[i]);
                }
            }
            float contentH = Mathf.Max(ContentMinH, totalH + ContentPadBottom);
            MenuDraw.ApplyPxRect(_content, _viewport, ContentRect(contentH));
            _scroll.ContentX1 = ContentTop;                  // 纵向：ContentX1/X2 装的是**上下两端**
            _scroll.ContentX2 = ContentTop + contentH;

            _sectionTemplate = BuildSection(null, ContentTop, out _);
            _sectionTemplate.gameObject.SetActive(false);     // = 原版 `sectionReference.SetActive(false)`

            if (secs != null && secs.Length > 0)
            {
                float y = ContentTop;
                for (int i = 0; i < secs.Length; i++)
                {
                    float h = SectionHeight(secs[i]);
                    var node = BuildSection(secs[i], y, out var view);
                    node.name = "Section " + (i + 1);        // 原版克隆体叫 `Section For Reference(Clone)`；我们按序号命名（如实标注）
                    _sectionNodes.Add(node);
                    if (view != null) _views.Add(view);
                    SectionsShown++;
                    y += h + ContentSpacing;                 // VLG: spacing 50
                }
            }
        }

        /// <summary>段高 = padding(2 + 5) + Header 52.5 + 卡片栅格高；无卡片时照 CSF 的 `MinSize = 59.496`。</summary>
        public static float SectionHeight(SectionData s)
        {
            float gridH = GridHeight(s.TotalCards > 0 ? s.TotalCards : (s.CardRefs != null ? s.CardRefs.Length : 0));
            if (gridH <= 0f) return SectionMinH;
            return SectionPadTop + HeaderH + gridH + SectionPadBottom;
        }

        /// <summary>栅格高（`EverguildGridLayoutGroup` 实读）：padding top 40 + 行数 × 290.4 + (行数−1) × 35。
        /// 列数 = 按段宽自适应（`m_Constraint = 0` Flexible）→ 本段宽 1250.49 / cell 206.667 + 间距 25 ⇒ **5 列**。</summary>
        public static float GridHeight(int cards)
        {
            if (cards <= 0) return 0f;
            int cols = Columns();
            int rows = (cards + cols - 1) / cols;
            return GridPadTop + rows * GridCellH + (rows - 1) * GridSpaceY;
        }

        /// <summary>栅格列数（原版 Flexible：`floor((段宽 − padding + spacing) / (cell + spacing))`）。</summary>
        public static int Columns()
        {
            const float padL = 0f, padR = 0f;
            float inner = (SectionX2 - SectionX1) - padL - padR;
            return Mathf.Max(1, Mathf.FloorToInt((inner + GridSpaceX) / (GridCellW + GridSpaceX)));
        }

        /// <summary>建一段（`data == null` = prefab 里那个模板节点）。</summary>
        Transform BuildSection(SectionData? data, float secTop, out SectionView view)
        {
            view = null;
            float h = data.HasValue ? SectionHeight(data.Value) : SectionMinH;
            var node = MenuDraw.Node(_content, "Section For Reference",
                                     Sh(new PxRect(SectionX1, secTop, SectionX2, secTop + h)));
            if (data.HasValue) { view = new SectionView { Node = node }; }

            // ---- Header（52.5 高；VLG pad top = 2）----
            float hdrTop = secTop + SectionPadTop;
            var header = MenuDraw.Node(node, "Header",
                                       Sh(new PxRect(SectionX1, hdrTop, SectionX2, hdrTop + HeaderH)));

            // `Header/Background`：`Image`(sprite=0) + `Gradient2`（横向暗红 → 透明）
            var bgR = Sh(new PxRect(SectionX1, hdrTop, SectionX2, hdrTop + HeaderH));
            var bgNode = MenuDraw.Node(header, "Background", bgR);
            var bgQuad = MenuDraw.Rect(bgNode, CardArt.Solid(), bgR, "Image", QHeaderBg, null);
            if (bgQuad != null)
                bgQuad.SetCornerColors(HeaderGradLeft, HeaderGradRight, HeaderGradRight, HeaderGradLeft);

            // `Header/Title`（Left/Midline · fs42）
            string titleText = data.HasValue
                ? Loc.BulkOr(data.Value.TitleTerm, data.Value.TitleFallback ?? "")
                : TemplateSectionTitle;
            var titleR = Sh(new PxRect(HeaderTitleX1, hdrTop, HeaderTitleX2, hdrTop + HeaderH));
            var title = MenuDraw.TextBox(header, titleR, titleText, Color.white, "Title", HeaderFont, 0f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(title, titleR);
            MenuDraw.SetVAlign(title, Label.VAlign.Capline, titleR);   // 原版 8192 = Capline（见文件头）

            // `Header/Counter`（Right/Midline · fs42）—— 格式串**已解出** = `"{0}/{1}"`
            //   （`WhereToGetCardsSection__Initialize.c:64` 的 `System_String__Format(DAT_18426de28,…)`，
            //    `stringliteral.json @0x426DE28 = "{0}/{1}"`）⇒ `Owned + "/" + Total` **逐字一致**。
            string counterText;
            if (data.HasValue)
            {
                int total = data.Value.TotalCards > 0 ? data.Value.TotalCards
                          : (data.Value.CardRefs != null ? data.Value.CardRefs.Length : 0);
                counterText = data.Value.OwnedCards + "/" + total;
            }
            else counterText = TemplateCounterText;
            var counterR = Sh(new PxRect(CounterX1, hdrTop, CounterX2, hdrTop + HeaderH));
            var counter = MenuDraw.TextBox(header, counterR, counterText, Color.white, "Counter", HeaderFont, 0f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(counter, counterR);
            // 🔴 **`N51④`**：原版 `m_VerticalAlignment = 4096` = **Midline**（不是 Baseline）。
            MenuDraw.SetVAlign(counter, Label.VAlign.Midline, counterR);

            // `Header/Toggle`（`EverguildToggle`：on/off 两个 sprite + 两档 tint）
            //   纵向**在 Header 里居中**（原版 raw `m_AnchoredPosition.y` 给的中心恰好 = Header 中心）。
            float toggleY1 = hdrTop + (HeaderH - ToggleH) * 0.5f;
            var toggleR = Sh(new PxRect(ToggleX1, toggleY1, ToggleX2, toggleY1 + ToggleH));
            var toggleNode = MenuDraw.Node(header, "Toggle", toggleR);
            var toggleQuad = MenuDraw.Rect(toggleNode, Tex(ArtToggleOn, "段折叠箭头（展开）"), toggleR,
                                           "Image", QToggle, ToggleOnTint, true);
            if (!data.HasValue)
            {
                // 模板节点整颗关着（原版 `sectionReference.SetActive(false)`）⇒ **不建命中区**
                //   （`MenuDraw.Hit` 的 `onClick == null` 会被 `PointerLayer.LogHit` 报成「忘了绑动作」的真告警）。
                _toggleNode = toggleNode;
                _toggleQuad = toggleQuad;
            }

            // ---- 卡片区 `Content`（GridLayoutGroup；模板无卡片 ⇒ 高 0）----
            int cards = data.HasValue
                ? (data.Value.TotalCards > 0 ? data.Value.TotalCards
                   : (data.Value.CardRefs != null ? data.Value.CardRefs.Length : 0))
                : 0;
            float gridH = GridHeight(cards);
            var gridNode = MenuDraw.Node(node, "Content",
                Sh(new PxRect(SectionX1, hdrTop + HeaderH, SectionX2, hdrTop + HeaderH + gridH)));
            if (view != null) view.Grid = gridNode;

            if (data.HasValue)
            {
                var v = view;
                MenuDraw.Hit(toggleNode, "Hit", toggleR, QHit, () => ToggleSection(v), null, null, null, null, null,
                             clickCue: WindowSounds.DefaultButtonCue);   // 原版 `m_onClickSound = ButtonClick`
                v.ToggleQuad = toggleQuad;

                if (cards > 0)
                    DrawCardCells(gridNode, hdrTop, data.Value, cards);
            }
            return node;
        }

        static List<CardDef> _pool;
        static List<CardDef> Pool()
        {
            if (_pool == null) _pool = CardDatabase.Load();
            return _pool;
        }

        /// <summary>🆕 **2026-10-17（第四十八会话 · `N51②`）：把来源段的卡片格画出来。**
        /// <para>数据：`SectionData.CardRefs`（原版 `LoadableReference&lt;Card&gt;` 的 id/名）。
        /// 本机**拿不到那个引用对象**，但**能按 id/名在我们的卡池里查**（`CardDatabase.Find`）——
        /// 查得到的照原版画 `CardView`，查不到的如实计数 + 出声（⛔ 不假装画了）。</para>
        /// <para>网格：原版 `EverguildGridLayoutGroup`（cell 206.667×290.4 · spacing 25/35 · padTop 40）
        /// ⇒ 按 `Columns()` 的列数逐格摆；卡按格高等比缩（同 Collection 卡池那条 `CardView.SetPose`）。</para></summary>
        void DrawCardCells(Transform grid, float hdrTop, SectionData d, int cards)
        {
            int cols = Columns();
            int drawn = 0, unresolved = 0;
            float scale = GridCellH / (CardView.Height * 108f);
            for (int i = 0; i < cards; i++)
            {
                string id = (d.CardRefs != null && i < d.CardRefs.Length) ? d.CardRefs[i] : null;
                var def = string.IsNullOrEmpty(id) ? null : CardDatabase.Find(Pool(), id);
                if (def == null) { unresolved++; continue; }

                int col = i % cols, row = i / cols;
                float x1 = SectionX1 + col * (GridCellW + GridSpaceX);
                float y1 = hdrTop + HeaderH + GridPadTop + row * (GridCellH + GridSpaceY);
                var cell = Sh(new PxRect(x1, y1, x1 + GridCellW, y1 + GridCellH));

                var cv = CardView.Create(grid, BattleDriver.ToCardData(def, def.Faction), "SourceCard_" + (i + 1));
                if (cv == null) continue;
                cv.SetPose(MenuDraw.Local(grid, cell.x1, cell.y1, cell.x2, cell.y2), 0f, scale);
                drawn++;
            }
            if (unresolved > 0)
                Debug.LogWarning("[WhereToGetCards] 这一段有 " + unresolved + "/" + cards + " 张卡的引用"
                               + "**在本机卡池里查不到**（`CardRefs` 是原版 `LoadableReference<Card>` 的 id/名）"
                               + "⇒ 那几格**没画**（已画的 " + drawn + " 张是能对上的）。");
        }

        /// <summary>段折叠（原版 `WhereToGetCardsSection.SetVisibility`）：卡片区开/关 + 箭头换图/换色
        /// + 🔴 **2026-10-17（`N51②`）重排**：原版靠 `VerticalLayoutGroup` + `ContentSizeFitter`
        /// 重算整列（收起后下面的段会**向上挪**）—— 我们现在按段高差逐段平移后面的节点、并同步滚动内容高。</summary>
        void ToggleSection(SectionView v)
        {
            if (v == null) return;
            v.On = !v.On;
            if (v.Grid != null) v.Grid.gameObject.SetActive(v.On);
            if (v.ToggleQuad != null)
            {
                var tex = Tex(v.On ? ArtToggleOn : ArtToggleOff, v.On ? "段折叠箭头（展开）" : "段折叠箭头（收起）");
                if (tex != null) v.ToggleQuad.SetTexture(tex, true);
                v.ToggleQuad.SetTint(v.On ? ToggleOnTint : ToggleOffTint);
            }

            // ---- 重排（展开高度 ↔ 收起高度 = `SectionMinH`）----
            var secs = _ctx.Sections;
            int idx = _views.IndexOf(v);
            if (secs == null || idx < 0 || idx >= secs.Length) return;
            float full = SectionHeight(secs[idx]);
            float delta = v.On ? (full - SectionMinH) : (SectionMinH - full);   // 正 = 后面的段往下
            if (Mathf.Abs(delta) < 0.01f) return;
            for (int j = idx + 1; j < _views.Count; j++)
            {
                var n = _views[j] != null ? _views[j].Node : null;
                if (n == null) continue;
                var p = n.localPosition;
                p.y -= delta / 108f;        // 屏幕 y 向下、世界 y 向上
                n.localPosition = p;
            }
            if (_scroll != null) _scroll.ContentX2 += delta;
        }

        // ============================================================ 滚动坐标
        /// <summary>内容坐标 → 屏幕坐标（只做偏移、不裁）。</summary>
        PxRect Sh(PxRect r) { return _scroll != null ? _scroll.Shift(r) : r; }

        // ============================================================ 取图（**取不到必须出声**）
        readonly List<string> _missArt = new List<string>();
        /// <summary>本窗取不到的图（自检读口 —— 非空就是「有件根本没画」）。</summary>
        public List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[WhereToGetCards] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 自检读口
        public Transform ShadeNode { get { return _shade; } }
        public Transform PanelNode { get { return _panel; } }
        public Transform TitleNode { get { return _titleNode; } }
        public Label TitleLabel { get { return _title; } }
        public Transform TopBarNode { get { return _topBar; } }
        public Transform ScrollNode { get { return _scrollNode; } }
        public Transform ViewportNode { get { return _viewport; } }
        public ViewportClip Clip { get { return _clip; } }
        public Transform ContentView { get { return _content; } }
        public Transform SectionTemplate { get { return _sectionTemplate; } }
        public Transform ToggleNode { get { return _toggleNode; } }
        public ImageQuad ToggleQuad { get { return _toggleQuad; } }
        public Transform CloseNode { get { return _close; } }
        public Transform CloseHitNode { get { return _closeHit; } }
        public MenuScroll Scroll { get { return _scroll; } }
        /// <summary>建出来的段节点（兄弟序；不含模板节点）。</summary>
        public List<Transform> SectionNodes { get { return _sectionNodes; } }

        /// <summary>当前标题实际用的字（自检读口）。</summary>
        public string TitleText { get { return _title != null ? _title.Text : null; } }

        /// <summary>自检用：把当前铺出来的东西摊开。</summary>
        public string DebugDump()
        {
            return "title=\"" + TitleText + "\""
                 + " sections=" + SectionsShown
                 + " template=" + (_sectionTemplate != null ? "on" : "<null>")
                 + " dataMissing=" + DataMissing
                 + " scrollMax=" + (_scroll != null ? _scroll.MaxOffset.ToString("F2") : "-")
                 + " missingArt=" + _missArt.Count;
        }

        /// <summary>关窗收尾 = 原版那一条**逐项放句柄**的链（账 `收藏:343` `WhereToGetCardsSection.Release`）。
        /// <para>判据（逐跳）：`WhereToGetCardsWindow.Close()` → 每个来源段 `WhereToGetCardsSection.Release()`
        /// → `foreach (CardDrawer d in currentCards(+0x48)) d.Release()`
        /// （`decomp_full/WhereToGetCardsSection__Release.c` 现读 + ilspy `WhereToGetCardsSection.cs:192-208` 同形）。
        /// 我们这扇**不建 `ItemDrawer`**（卡片格直接 `CardView.Create`，`:583` 那一带）⇒ 等价物 =
        /// `MenuDraw.ClearChildren(transform)`（它逐个子件走 `MenuDraw.ReleaseArtUnder`，把所有逐项美术放掉，
        /// 判据 → `Shell/MenuDraw.cs:143`）。</para>
        /// <para>🔴 **为什么放在 `base.Close()` 之后**：基类首句是「本来就没开 ⇒ 直接 return」（含那条脏现场出声），
        /// 先调基类才能保住那两档语义；之后清子件只是**提前**做 `Build()` 下次开窗反正要做的事（幂等）。
        /// ⚠️ 三条读口/登记跟着归零（`SectionsShown` / `_sectionNodes` / `_views` / 指针层登记）——
        /// 不清的话「节点已经销毁、读口还数得出段 / 死命中去处还在」= 静默说谎。</para>
        /// <para>🧨 改坏法：把本方法删掉 ⇒ 反复开关这扇窗后 `CardArt` 的句柄数单调上涨
        /// （关窗只 `SetActive(false)`、既不销毁也不放）。</para></summary>
        public override void Close()
        {
            base.Close();
            PointerLayer.UnregisterOwnedBy(gameObject);      // 同 `Build()` 首句：旧滚动条目是死条目
            MenuDraw.ClearChildren(transform);               // = ReleaseArtUnder + 销毁（原版那串 `Release()`）
            SectionsShown = 0;
            _sectionNodes.Clear();
            _views.Clear();
        }
    }
}
