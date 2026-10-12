// GenericOptionsPanel.cs — **通用选项面板**（原版 `GenericOptionsPanel : GameWindow`）
//
// ============================ 出处（唯一正本）============================
// prefab：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 根 GO `Generic Options Panel`
//   · 窗口参数那颗 MB = `MonoBehaviour_2699463439353286802`（字段实读见下面「窗参」）；
//   · 小屏缩放器 = `TransformScalerBySmallScreenUI`（**挂在窗体根 GO 上**，`menuScale = 1.35`）；
//   · 逐节点几何 = **`menu_dump.py` 现读**（绝对框与 `--relative` 框两份都核过）：
//     `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Generic Options Panel" --depth 12 --md`
//   · 组件字段（`m_Padding` / `m_ChildAlignment` / `m_fontSize{,Base,Min,Max}` / `m_Color` / `m_Border`）
//     = 本文件头那条命令 + `工具/probe` 式的逐 MB 原文实读（**每个数都标了出处**）；
// 行为 = 反编译 `d:/2/tools/decomp_full/GenericOptionsPanel__{Start,Open,SetTitle,SetButtons,SetPosition}.c`
//   ＋ `GenericOptionsPanel.Context` / `.Button` 两个值类型的字段表（`dump.cs` TypeDefIndex 2075/2076）。
//
// ============================ 窗参（MB 逐字段实读）============================
// `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `openSound/closeSound = null` ·
// `useDefaultCloseSoundIfNull = 1` · 🔴 **`closeOnESC = 0`**（**这扇是 0**，同族的 `TrophyInfoPopup` 是 1
// —— 铁律 5·c：一个值 ≠ 全部情况，逐扇实读）· `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0`。
// 🔴 **`extraScaleSmallScreen = 1.0` 的含义是「不覆盖」**：小屏下真正生效的是**烤在 prefab 里**的
// `menuScale = 1.35`（窗口根上带成品的全库只有 3 扇：`Alliance Trophy Info Popup` /
// `Member Options Panel` / `Generic Options Panel` —— 判据全文 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头③）。
//    ⇒ 我们建窗时要**显式补那颗组件 + `Initialize()`**（同 `TrophyInfoPopup` 那条先例，见 `MenuScale`）。
// 三个字段各指哪个节点（pid 反查，3/3 逐个对上）：
//   `titleText → Name` · `buttonTemplate → Template` · `buttonHolder → Buttons`。
//
// ============================ 🔴 两条「照原版自己的分支」 ============================
// ① **`Template` 运行期是关着的**（判据 = `GenericOptionsPanel__Start.c` 第一句：
//    `buttonTemplate.gameObject.SetActive(false)`）。prefab 出厂 `act = T`，`Start()` 把它关掉
//    —— 我们用 **false** 建（照**运行期**那一档），并在断言里两态都钉住。
// ② **模板被关掉之后，布局会重算**（⇒ 面板高度不是 prefab 那个 126.6）。判据两条，缺一不可：
//    · uGUI 的 `LayoutGroup.CalculateLayoutInputHorizontal`（本地源码
//      `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs:53-62`）
//      **只收 `activeInHierarchy` 的子件** —— 关掉的 `Template` 不再参与；
//    · 布局重建跑在 `Canvas.willRenderCanvases`（**`Start` 之后**）⇒ 第一次建出来的就是「不含 `Template`」那一档。
//    ⇒ **运行期几何 = `Name` + `Buttons` 两件**（见 `RootH`），prefab 那档（126.6）只作对照记录。
//    ⚠️ **本条是「我们算的、不是逐字照抄」的唯一一处**（其余全是 dump 现读），已写进报告 §九。
//
// ============================ 布局模型（把 uGUI 的三句算式摊开） ============================
// 根 = `VerticalLayoutGroup`：`m_Padding(0,0,10,10)` · `m_ChildAlignment = 1 (UpperCenter)` · `spacing = 5` ·
//   `childControlWidth = 0` · `childControlHeight = 1` · `childForceExpandWidth = 1` · `childForceExpandHeight = 0`
//   ＋ `ContentSizeFitter(m_VerticalFit = 2 = PreferredSize)` ⇒ 根高 = 10 + 高(`Name`) + 5 + 高(`Buttons`) + 10。
// `Name` 的 `LayoutElement.m_PreferredHeight = 36.6` ⇒ 高 36.6（dump 实测 10.00→46.60 ✓）。
// `Buttons` = 自己的 `VerticalLayoutGroup`（`m_Padding(15,15,0,0)` · `m_ChildAlignment = 4 (MiddleCenter)` ·
//   spacing 5 · `childControlHeight = 1`）⇒ 高 = `60n + 5(n−1)`（n≥1）、**n=0 时 uGUI 直接返回 0**。
//   ⚠️ `Buttons` 自己的 `sizeDelta.x = 0`（锚点 (0,1)-(0,1)）——但它的**子件宽 357.3** 来自
//   `Template` 自带的 `sizeDelta.x = 357.3`（`childControlWidth = 0` ⇒ 子件宽不由布局写）。
//   我们的 `MenuDraw` 是绝对框 ⇒ 直接记**布局跑完的实测值**，⛔ 不需要、也不该复刻这套算法。
// 根的位置：锚 `(0.5,0.5)`、pivot `(0.5,1)`、**`anchoredPosition = (0,-96.7)`**
//   ⇒ 画布（y 向下）里 **pivot 点 = (960, 540 − (−96.7)) = (960, 636.70)**；宽 387.3 ⇒ x 766.35→1153.65。
//   🔴 **`SetPosition`**（`GenericOptionsPanel__SetPosition.c`）给的是另一档：
//   `transform.position = anchor.position` 之后 `anchoredPosition.y −= rect.height × 0.5`
//   （那个 `0.5` 我**从二进制里读出来**了：`GameAssembly.dll` 的 RVA `0x34B2BB4` 处 4 字节 = `0.5f`
//    —— `RVA = 0x1834B2BB4 − ImageBase(0x180000000)`，节表 `.rdata` vaddr `0x34B0000` / raw `0x34B0000+0x3800`）
//   ⇒ 净效果 = **面板竖直方向居中到锚点上**（pivot 在顶/底都会落到同一个结果）。见 `SetPosition`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `GenericOptionsPanel`（`GameWindow` 子类）——**一列按钮 + 一个标题**的通用小面板
    /// （原版那套「点了某个东西、在它旁边弹一排选项」的载体）。
    /// <para>数据由调用方给（原版是 `GenericOptionsPanel.Context`）：`Title` + `Buttons[]` + `LocalizeText`
    /// + `AnchorPosition`。</para>
    ///
    /// <para>🔴 **2026-10-12（`A1045`）就地订正（铁律 5）**：本节 + `Open()` 那条日志 + `WindowsManager`
    /// 上那两处注释原来都写「**本地没有任何调用方**（全量反编译里 `GenericOptionsPanel` 只命中它自己的
    /// 那 20 个 `.c`；跨 CAB 的 UnityEvent 开启点没排除）⇒ 我们只提供「怎么开」」—— **前半句是错的**。
    /// 调用方**就在全量反编译里**：`DeckInfoPopup.ShareDeckOnChat(Transform optionsMountPoint)`
    /// （`d:/2/tools/decomp_full/DeckInfoPopup__ShareDeckOnChat.c`，逐句实读）：建两颗 `Button`
    /// （`ShareOnAlliance` / `ShareOnGlobal`）+ 一个 `Context` → `WindowsManager.OpenWindow(panel, ctx)`。
    /// **错因** = 当年按**文件名** `grep GenericOptionsPanel` 去找调用者，而那个文件叫
    /// `DeckInfoPopup__ShareDeckOnChat.c`（文件名里没有 `GenericOptionsPanel`）⇒ 一次 `grep -l` 就漏了；
    /// 判据其实一直躺在**方法体内部**（`GenericOptionsPanel_Button___ctor` / `Context___ctor` 两个调用点）。
    /// ⇒ **首例入口已接**：`Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`（两键：群组 / 全局）。</para>
    ///
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3330–3337**（`TrophyInfoPopup` = 3310–3326 之上、
    /// 挑战弹窗 3400 之下 —— 层带不许重叠）。</para></summary>
    public class GenericOptionsPanel : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3330–3337）
        //   逐层顺序 = **原版兄弟序**（uGUI 按兄弟序画 ⇒ 后面的压前面的；判据 = prefab 的 `m_Children`）：
        //   根 = [Menu Dark Background, bg shadow, bg, Name, Template, Buttons]。
        //   `Template` 在运行期是关的（`Start()`）⇒ 不占档；它若被放出来，位置与第 1 颗按钮重合。
        public const int QShade = 3330,        // 压暗/吃点击的整屏底（`Menu Dark Background`）
                         QShadeHit = 3331,     // 上面那件的命中区（`BackgroundCloseButton`）
                         QBgShadow = 3332,     // `bg shadow`（八边形投影）
                         QBg = 3333,           // `bg`（面板底，**不透明**）
                         QName = 3334,         // `Name`（标题）
                         QBtnBg = 3335,        // 按钮底（`Template` / 每一颗实例化的钮）
                         QBtnText = 3336;      // `Button Text`
        /// <summary>本窗**内容命中区**那一档（压暗层命中区必须**严格低于**它 —— `MenuDraw.ShadeHit` 现场核）。</summary>
        public const int QHit = 3337;

        // ============================================================ 冻结点（全部 dump / MB 实读 · 画布 px）
        /// <summary>面板宽（根 `sizeDelta.x`，dump 实测 387.30）。</summary>
        public const float W = 387.30f;
        /// <summary>根 VLG 的 `m_Padding.m_Top` / `.m_Bottom`（左右都是 0）。</summary>
        public const float PadTop = 10f, PadBottom = 10f;
        /// <summary>根 VLG 的 `m_Spacing`。</summary>
        public const float Spacing = 5f;
        /// <summary>`Name` 的 `LayoutElement.m_PreferredHeight`（`m_MinHeight` 同值）。</summary>
        public const float NameH = 36.60f;
        /// <summary>`Template` 的 `LayoutElement.m_PreferredHeight`。</summary>
        public const float TemplateH = 60f;
        /// <summary>按钮宽（`Template` 的 `sizeDelta.x`；`childControlWidth = 0` ⇒ 实例化出来的每颗同宽）。</summary>
        public const float BtnW = 357.30f;
        /// <summary>按钮竖直步长 = 60（`m_PreferredHeight`）+ 5（`Buttons` 的 `m_Spacing`）。</summary>
        public const float BtnStep = 65f;
        /// <summary>`Button Text` 的框：宽 = 357.3 × (0.9612619 − 0.0355112) = **330.77**；
        /// 高由 `AspectRatioFitter(m_AspectMode = 1 宽控高, m_AspectRatio = 3.8386404514312744)` 定 ⇒ **86.17**。</summary>
        public const float BtnTextW = 330.77f, BtnTextH = 86.17f;
        /// <summary>`Button Text` 左沿相对按钮左沿的偏移 = 357.3 × 0.0355112 = **12.6881**。</summary>
        public const float BtnTextDx = 12.6881f;
        /// <summary>🔴 根在画布 y 向下坐标里的 **pivot 点 y** = `540 − anchoredPosition.y` = 540 − (−96.7)。
        /// ⚠️ 这是「**没有** `AnchorPosition` 时」那一档（原版 `Open()` 里判 `AnchorPosition == null` ⇒ 不调
        /// `SetPosition`、保留序列化值）。</summary>
        public const float DefPivotY = 540f - (-96.7f);
        /// <summary>`bg shadow` 比面板大出来的半量：`sizeDelta(63.1725, 78.9843)` ÷ 2。</summary>
        public const float ShadowHalfW = 31.58625f, ShadowHalfH = 39.49215f;
        /// <summary>`bg shadow` 的 `anchoredPosition.x`（实测 −0.8671875；两份面板略有差：另一扇是 −0.8670044）。</summary>
        public const float ShadowDx = -0.8671875f;
        /// <summary>`Menu Dark Background` 的尺寸（4574.60 × 2572.36），**以面板中心为中心**。
        /// ⚠️ 它**不是**「铺满全屏」那一个矩形（同族 `TrophyInfoPopup` 的是铺满屏的 4574.6×2572.36 居中在
        /// 屏幕中心；这里居中在**面板**中心 —— 同一个尺寸、不同的锚）⇒ 面板一动它跟着动。逐份实读。</summary>
        public const float ShadeW = 4574.60f, ShadeH = 2572.36f;
        /// <summary>`bg shadow` 的 sprite 名（`OctagonUI Filled SDF`，128×128，`m_Border = 52,52,52,52`，Sliced）。
        /// 🔴 **工程里没有这张图**（`Resources/Art/{ui_menu,ui_deck,ui}/` 三处都没有；源在
        /// `bundle_duplicateassetisolation_assets_all/Sprite/OctagonUI Filled SDF.json`）⇒ 节点照建、**这一格不画** + 出声。</summary>
        public const string ArtShadow = "OctagonUI_Filled_SDF";
        /// <summary>`bg` 的 sprite（`40k_topmarquee_currency_display BW`，44×39，`m_Border = 15,15,15,15`，Sliced）。</summary>
        public const string ArtBg = "40k_topmarquee_currency_display_BW";
        /// <summary>按钮底（`UI_Button_Mulligan`，410×124，`m_Border = 333,96,333,96`，`ppuMul = 3.0`，
        /// `m_SpriteState` 的 HL/P 两张 = dump 那一列的 `HL=… P=…` 实读）。</summary>
        public const string ArtBtn = "UI_Button_Mulligan",
                            ArtBtnHover = "UI_Button_Mulligan_hover",
                            ArtBtnPressed = "UI_Button_Mulligan_Pressed";
        /// <summary>`EverguildTextController` 的 `maxFontSize` / `minFontSize`（**与 TMP 的 `m_fontSizeMax/Min` 同值**）。</summary>
        public const float BtnTextFontMax = 35f, BtnTextFontMin = 10f;
        /// <summary>`Name` 的 TMP 实读：`m_fontSize 38.6 · base 36 · min 18 · max 72`。</summary>
        public const float NameFont = 38.6f, NameFontBase = 36f, NameFontMin = 18f, NameFontMax = 72f;
        /// <summary>`Button Text` 的 TMP 实读：`m_fontSize 35 · base 12 · min 10 · max 35`。</summary>
        public const float BtnTextFont = 35f, BtnTextFontBase = 12f;
        /// <summary>`bg shadow` 的 `Image.m_Color`（实读 0.4470588266849518）。</summary>
        public static readonly Color ShadowTint = new Color(0f, 0f, 0f, 0.44705883f);
        /// <summary>`bg` 的 `Image.m_Color`（实读 0.31132078170776367 / 0.20118370652198792 / 同 / 1）。</summary>
        public static readonly Color BgTint = new Color(0.31132078f, 0.20118371f, 0.20118371f, 1f);
        /// <summary>🔴 **`Menu Dark Background` 的 `Image.m_Color` 是 `(0,0,0,0)` —— `a = 0`，整块透明。**
        /// 它**不是**「压暗整屏」那一个（同族别的窗是 `(0,0,0,0.773)`）—— 这两个小面板是**上下文菜单**，
        /// 只借它**吃点击**（uGUI 的 `Graphic.Raycast` **不看 alpha**，`m_RaycastTarget = 1` 就够）。
        /// 两扇逐份实读，值相同。⛔ 别顺手改成 0.773。</summary>
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0f);

        /// <summary>🆕 **烤在 prefab 里**的那颗小屏缩放器带的倍数（`TransformScalerBySmallScreenUI.menuScale`）。
        /// 判据 = 那 3 扇窗口根上带成品的窗（见文件头「窗参」那一段）。</summary>
        public const float MenuScale = 1.35f;

        // ============================================================ 数据（= 原版 `GenericOptionsPanel.Context`）
        /// <summary>一颗按钮（= 原版 `GenericOptionsPanel.Button`，`dump.cs` TypeDefIndex 2076）。
        /// 字段名逐字照原版；`ColorOverride` 对应原版那个 `Nullable&lt;Color&gt;`（`null` = 不覆盖）。</summary>
        public struct OptButton
        {
            public string Text;
            /// <summary>🔴 **2026-10-12（`A1045`）新增 = 原版 `Button.OnClick`**（那颗 `UnityAction`，
            /// 调用方挂、`Open()` 里由 `SetButtons` 逐颗 `AddListener`）。
            /// **缺了它 = 点了什么都不发生 = 静默失败**（本仓红线）⇒ `OnButton` 在它为空时**出声**。
            /// <para>首例调用方 = `Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`（群组 / 全局两键各挂一个）。</para></summary>
            public System.Action OnClick;
            /// <summary>原版 `Interactable`。
            /// <para>✅ **2026-10-12（`A1045`）就地订正（铁律 5）**：本节原来写「置 `false` 时把按钮建成灰的
            /// **并出声**」—— 那时**只有「建成灰的」这一半**（`RebuildButtons` 里换成
            /// `m_DisabledColor` 那个 tint），**没有任何闸、也没有出声**：置了 `false` 的钮**照样会触发
            /// `OnClick`**（与原版 `Selectable.OnPointerClick` 头一句相反）。本轮补上**第二道闸**。
            /// 现行为（三半，各自有出处）：
            /// · **视觉①** = 底图 tint 换成 `m_DisabledColor`（**实读** `(0.7843137,0.7843137,0.7843137,0.5019608)`
            ///   —— 出处 = `Template` GO 上的 `EverguildButton` MB `-7346004311159172974` 的 `m_Colors`）；
            /// · **视觉②** = 🆕 **A1726**：`WindowButton.Interactable = 这个值` ⇒ 九宫格那 **9 块全灰**
            ///   （材质换原版 `Everguild/UI/Greyscale`，同 `EverguildButton.DoStateTransition(Disabled)` 那一下）
            ///   —— 🔴 **判据确凿**：那颗钮的 `colorTintGreyOnDisable = 1`（实读 MB `-7346004311159172974`），
            ///   而它正是 `EverguildButton.SoftDisable` / `SwitchMaterial` 的第一句守卫；
            /// · **行为** = **两道闸**：第一道 `WindowButton.Click()` 头一句（`!Interactable ⇒ 出声 + return`，
            ///   = 原版那句话），兜底第二道 = `OnButton` 里**动作照进来、立刻返回 + 出声** —— 与
            ///   `Shell/DeckInfoPopup.cs` 的 `Blocked(key, interactable)` **同一口径**。
            /// ⚠️ **原来这里写着「第一道闸本扇【故意不用】……要按原版那一条得先修 `WindowButton.GrayTargets`
            /// 的九宫格档（不在本轮白名单）⇒ 本轮走第二道闸」** —— **已过期**（**留着更正痕**）：
            /// `A1719`（2026-10-13）把 `GrayTargets()` 的第三组 `_nine` 补上了、`A1725` 又补了第四组
            /// （`TintOn` 那几颗），**`WindowButton.cs` 本轮就在白名单里** ⇒ 第一道闸**已接**
            /// （落在 `RebuildButtons` 的 `wb.Interactable = arr[i].Interactable`）。</para></summary>
            public bool Interactable;
            /// <summary>原版 `AutoClose`：这一颗被点之后**顺带把窗关掉**（`SetButtons` 里多挂一个
            /// `AddListener(this.Close)`）。我们同义：回调里 `Close()`。</summary>
            public bool AutoClose;
            /// <summary>原版 `ColorOverride`（`Nullable&lt;Color&gt;`）：非空 ⇒ `image.color = 它`。</summary>
            public Color? ColorOverride;
        }

        /// <summary>面板内容（= 原版 `GenericOptionsPanel.Context`，`dump.cs` TypeDefIndex 2075）。</summary>
        public struct Context
        {
            public string Title;
            /// <summary>原版 `LocalizeText`：`true` ⇒ 标题与每颗按钮的字都过 I2 词条。
            /// 🔴 **我们过不了**（词条表在远端 CCD、本地一个 value 都没有，同 `TrophyInfoPopup` 那条口径）
            /// ⇒ 置 `true` 时**出声说明**、字串原样用（不静默）。</summary>
            public bool LocalizeText;
            public OptButton[] Buttons;
            /// <summary>原版 `AnchorPosition`（一个 `Transform`）：非空 ⇒ `SetPosition(它)`。
            /// 我们收**画布像素坐标**（y 向下 · 左上原点），语义 = 面板要**居中到**哪个点。</summary>
            public Vector2? AnchorPosition;
        }

        /// <summary>原版 `Context` 的出厂值：标题用 prefab 出厂原文、0 颗按钮、不定位。</summary>
        public static Context DefContext()
        {
            return new Context { Title = DefTitle, LocalizeText = false, Buttons = new OptButton[0], AnchorPosition = null };
        }

        /// <summary>`Name` 的 prefab 出厂字串（**资产里的真字符串**，不是我们编的 —— 它身上带 `Localize`
        /// ⇒ 原版走 I2 词条、而词条表在远端 CCD ⇒ 只能照抄 prefab 里那个串本身，同 `TrophyInfoPopup` 口径）。</summary>
        public const string DefTitle = "Pepito el de siempre";
        /// <summary>`Template/Button Text` 的 prefab 出厂字串（模板专用，运行期永远看不到 —— `Template` 是关的）。</summary>
        public const string TemplateText = "Template";

        /// <summary>最近一次开出来的那一扇（自检用，同 `TrophyInfoPopup.LastOpened` 那条先例）。</summary>
        public static GenericOptionsPanel LastOpened { get; private set; }

        Context _ctx = DefContext();
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public Context Ctx { get { return _ctx; } }
        /// <summary>上一次真正铺过的按钮数（`Apply` 写；自检读口）。</summary>
        public int BuiltButtons { get; private set; }

        readonly System.Collections.Generic.List<Transform> _btnNodes = new System.Collections.Generic.List<Transform>();
        Label _title;
        Transform _template, _buttons;
        /// <summary>`Template` 节点自己的那颗九宫底（原版 `buttonTemplate` 上 `EverguildButton.m_TargetGraphic`
        /// 指的那颗 `Image`）。⚠️ 存 `GameObject` —— `MenuDraw.Nine` 返回的是**节点**（一颗九宫底有 9 个子块），
        /// `MenuDraw.Rect` 才返回 `ImageQuad`。</summary>
        GameObject _templateImg;

        // ============================================================ 开

        /// <summary>建一扇（原版 `WindowsManager.OpenWindow(genericOptionsPanel, context)` 那一步的等价物）。</summary>
        public static GenericOptionsPanel Create(WindowsManager mgr, Context? ctx = null, Vector2? anchor = null)
        {
            var go = new GameObject("Generic Options Panel");     // 节点名照原版（`WindowsManager` 复用的键同源）
            var win = go.AddComponent<GenericOptionsPanel>();
            win.type = WindowType.Popup;                  // 实读 `type = 1`
            win.placement = WindowsPlacement.Popup;       // 实读 `windowsPlacement = 15`
            win.closeOnEsc = false;                       // 🔴 **实读 `closeOnESC = 0`**（同族的 TrophyInfoPopup 是 1）
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= **不覆盖**；真正生效的是烤着的 1.35）
            // 烤在 prefab 里的那颗小屏缩放器（见 `MenuScale`）。⚠️ `AddComponent` 那一刻 `OnEnable` 已经跑过
            // （那时 `menuScale` 还是 1）⇒ 赋完值必须**显式 `Initialize()`**（同 `TrophyInfoPopup` 那条）。
            var sc = go.AddComponent<TransformScalerBySmallScreenUI>();
            sc.menuScale = MenuScale;
            sc.Initialize();
            win.Manager = mgr;
            win._ctx = FillDef(ctx, anchor);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[OptionsPanel] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        /// <summary>逐格补出厂值：传进来的哪一格是空就拿出厂值顶上（**已赋的值一个字不动**）。
        /// `anchor` 单独一个参数：原版 `Context.AnchorPosition` 是个 `Transform`，我们收画布点。</summary>
        public static Context FillDef(Context? src, Vector2? anchor = null)
        {
            var c = src ?? DefContext();
            if (c.Title == null) c.Title = DefTitle;
            if (c.Buttons == null) c.Buttons = new OptButton[0];
            if (anchor.HasValue) c.AnchorPosition = anchor;
            return c;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();                 // `Build` 里会 `Apply()`
            Debug.Log("[OptionsPanel] 开了 `Generic Options Panel`（原版 `GenericOptionsPanel`）。"
                    + "· 标题 = prefab 出厂原文（原版走 I2 词条，词条表在远端 CCD）。"
                    + "· `Template` 照 `Start()` 建成**关着**的。"
                    + "· 运行期几何**不含 `Template`**（uGUI 只收 active 子件，见文件头②）。"
                    + "· 首例入口 = `Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`（见类文档那条订正）。");
        }

        /// <summary>换一份内容（同 `BaseOfferPopup.Show` 那条先例）。`c == null` ⇒ 用**出厂值**。</summary>
        public void Show(Context? c = null, Vector2? anchor = null)
        {
            _ctx = FillDef(c, anchor);
            Build();
        }

        // ============================================================ 几何（**唯一一份算式**）

        /// <summary>`Buttons` 的高度（原版那是 uGUI 跑出来的；n=0 时 uGUI 直接返回 0）。
        /// `60n + 5(n−1)` = `65n − 5`（n≥1）。</summary>
        public static float ButtonsH(int n) { return n <= 0 ? 0f : BtnStep * n - Spacing; }

        /// <summary>面板高度 = `PadTop + NameH + Spacing + ButtonsH(n) + PadBottom`。
        /// 🔴 **不含 `Template`**（它在运行期是关的 ⇒ 进不了布局组的 `rectChildren`）—— 见文件头②。
        /// ⚠️ prefab 出厂那一档（`Template` 还开着）= `126.6 + max(0, ButtonsH(n))`，**我们不用它**，
        /// 只在 `Dump()` 里作为对照打出来。</summary>
        public static float RootH(int n) { return PadTop + NameH + Spacing + ButtonsH(n) + PadBottom; }

        /// <summary>面板矩形。`pivotY` = 原版根节点的 pivot 点（画布 y 向下）：
        /// 没给 `AnchorPosition` ⇒ 用 `DefPivotY`（= 636.70，pivot 在**顶**，高度向下长）；
        /// 给了 ⇒ 等价于原版 `SetPosition`（**竖直居中到锚点**）。
        /// <para>🔴 原版 `SetPosition.c` 的净效果（逐句读出来的）：`pivot 点` 先落在锚点上、再下移 `H/2`
        /// ⇒ 面板**中心**在锚点上。`pivot ∈ {顶,底}` 两种写法都收敛到同一个结果（代数上都能验）。</para></summary>
        public PxRect RootRect(int n, Vector2? anchor = null)
        {
            float h = RootH(n);
            if (anchor.HasValue)
                return new PxRect(anchor.Value.x - W * 0.5f, anchor.Value.y - h * 0.5f,
                                  anchor.Value.x + W * 0.5f, anchor.Value.y + h * 0.5f);
            float y1 = _ctx.AnchorPosition.HasValue ? _ctx.AnchorPosition.Value.y - h * 0.5f : DefPivotY;
            return new PxRect(960f - W * 0.5f, y1, 960f + W * 0.5f, y1 + h);
        }

        /// <summary>第 `i` 颗按钮的矩形（= `Buttons` 顶 + `i × 65`，宽 357.3、高 60）。</summary>
        public static PxRect BtnRect(PxRect root, int i)
        {
            float top = root.y1 + PadTop + NameH + Spacing + BtnStep * i;
            return new PxRect(960f - BtnW * 0.5f, top, 960f + BtnW * 0.5f, top + TemplateH);
        }

        /// <summary>`Button Text` 的矩形（`AspectRatioFitter` 跑完那一档：宽 330.77 / 高 86.17，**竖直居中**在按钮上）。</summary>
        public static PxRect BtnTextRect(PxRect btn)
        {
            float x1 = btn.x1 + BtnTextDx, cy = (btn.y1 + btn.y2) * 0.5f;
            return new PxRect(x1, cy - BtnTextH * 0.5f, x1 + BtnTextW, cy + BtnTextH * 0.5f);
        }

        /// <summary>压暗层 / 吃点击那一整块：**以面板中心为中心**的 4574.60×2572.36（逐份实读）。</summary>
        public static PxRect ShadeRect(PxRect root)
        {
            float cx = (root.x1 + root.x2) * 0.5f, cy = (root.y1 + root.y2) * 0.5f;
            return new PxRect(cx - ShadeW * 0.5f, cy - ShadeH * 0.5f, cx + ShadeW * 0.5f, cy + ShadeH * 0.5f);
        }

        /// <summary>`bg shadow`：面板框向外各撑 `ShadowHalfW/H`、整体再平移 `ShadowDx`（`m_SizeDelta` +
        /// `m_AnchoredPosition` 的净效果，两扇面板逐份核过）。</summary>
        public static PxRect ShadowRect(PxRect root)
        {
            return new PxRect(root.x1 - ShadowHalfW + ShadowDx, root.y1 - ShadowHalfH,
                              root.x2 + ShadowHalfW + ShadowDx, root.y2 + ShadowHalfH);
        }

        /// <summary>`Name` 的矩形：`0.5` pivot 在面板水平中心、`sizeDelta = (355, 36.6)`、顶沿 = 面板顶 + 10。
        /// 355 = `m_SizeDelta.x` 实读；dump 实测 782.50→1137.50 ✓。</summary>
        public static PxRect NameRect(PxRect root)
        {
            float y1 = root.y1 + PadTop;
            return new PxRect(960f - 355f * 0.5f, y1, 960f + 355f * 0.5f, y1 + NameH);
        }

        /// <summary>`Template` 的矩形（**关着的那颗**）：锚 `(0,1)` + `anchoredPosition (193.65, −81.6)`
        /// ⇒ 顶沿 = 面板顶 + 81.6 —— 它在出厂布局里占的就是「`Name` 下面那一格」（`10 + 36.6 + 5 = 51.6`
        /// 是它的**框顶**、`81.6` 是它的 **pivot 点**（pivot 在 (0.5,0.5) ⇒ 51.6 + 60/2 = 81.6 ✓））。
        /// <para>🔴 它**不参与布局**（关着）⇒ 这个矩形是**序列化值**、uGUI 不会去动它；运行期 `Buttons`
        /// 会**上移到它那一格**（两者在屏幕上重叠，但 `Template` 不画）。</para></summary>
        public static PxRect TemplateRect(PxRect root)
        {
            float y1 = root.y1 + 51.6f;
            return new PxRect(960f - BtnW * 0.5f, y1, 960f + BtnW * 0.5f, y1 + TemplateH);
        }

        /// <summary>`Buttons` 容器：锚 `(0,1)`、`sizeDelta.x = 0` ⇒ **零宽**、竖直中心在面板水平中心线上；
        /// 顶沿 = 面板顶 + 10 + 36.6 + 5 = +51.6（运行期档）。</summary>
        public static PxRect ButtonsRect(PxRect root, int n)
        {
            float top = root.y1 + PadTop + NameH + Spacing;
            return new PxRect(960f, top, 960f, top + ButtonsH(n));
        }

        // ============================================================ 建

        /// <summary>照原版 prefab 逐节点搭（**先清空重来** —— 换内容就是重建，同 `BoosterInfoPopup.Build`）。
        /// 末尾调 `Apply()` 把 `_ctx` 铺上去。</summary>
        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);
            MissingArt.Clear();
            _btnNodes.Clear();
            _title = null; _template = null; _buttons = null; _templateImg = null;

            int n = _ctx.Buttons != null ? _ctx.Buttons.Length : 0;
            var R = RootRect(n);
            var S = ShadeRect(R);

            // ---- 1) `Menu Dark Background`：**a = 0 的整屏底**，只吃点击（见 `ShadeTint`）----
            MenuDraw.Rect(root, CardArt.Solid(), S, "Menu Dark Background", QShade, ShadeTint);
            // 命中区走公共件（它自带「`qShade < qContentMin`」现场核）。
            MenuDraw.ShadeHit(root, S, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `bg shadow` → 3) `bg` ----
            var shadowTex = Tex(ArtShadow, "`bg shadow` 的八边形投影");
            if (shadowTex != null)
                MenuDraw.Rect(root, shadowTex, ShadowRect(R), "bg shadow", QBgShadow, ShadowTint);
            else
                MenuDraw.Node(root, "bg shadow", ShadowRect(R));      // 图缺 ⇒ 节点照建、这一格不画（出声在 Tex）
            var bgTex = Tex(ArtBg, "面板底 `bg`");
            if (bgTex != null)
                MenuDraw.Nine(root, bgTex, R, new Vector4(15f, 15f, 15f, 15f), 44f, 39f, QBg, BgTint, true, "bg");
            else
                MenuDraw.Node(root, "bg", R);

            // ---- 4) `Name`（标题）----
            var nameR = NameRect(R);
            _title = MenuDraw.TextBox(root, nameR, _ctx.Title ?? "", Color.white, "Name",
                                      NameFont, NameFontMin, QName, NameFontMax, NameFontBase);
            // 原版 hAlign = **Center(2)** ⇒ 不调 `AlignLeft`（`Label` 默认就是水平居中）。

            // ---- 5) `Template`（**关着的那颗模板** —— `Start()` 明文 SetActive(false)）----
            _template = MenuDraw.Node(root, "Template", TemplateRect(R));
            var tImg = Tex(ArtBtn, "`Template` 的按钮底");
            if (tImg != null)
                _templateImg = MenuDraw.Nine(_template, tImg, TemplateRect(R),
                                             new Vector4(333f, 96f, 333f, 96f), 410f, 124f, QBtnBg, Color.white,
                                             true, "Image",
                                             new Vector4(333f / 3f, 96f / 3f, 333f / 3f, 96f / 3f));
            var tTxtR = BtnTextRect(TemplateRect(R));
            MenuDraw.TextBox(_template, tTxtR, TemplateText, Color.white, "Button Text",
                             BtnTextFont, BtnTextFontMin, QBtnText, BtnTextFontMax, BtnTextFontBase);
            _template.gameObject.SetActive(false);      // 🔴 判据 = `GenericOptionsPanel__Start.c` 第一句

            // ---- 6) `Buttons`（容器；运行期**空的**，按钮由 `Apply()` 按 `_ctx` 填）----
            _buttons = MenuDraw.Node(root, "Buttons", ButtonsRect(R, n));

            Apply();
        }

        /// <summary>把 `_ctx` 铺到已建好的件上（= 原版 `Open()` 里那三跳：
        /// `SetTitle(Context.Title, Context.LocalizeText)` → `SetButtons(Context.Buttons, …)` →
        /// `SetPosition(Context.AnchorPosition)`）。</summary>
        public void Apply()
        {
            if (_title != null) _title.SetText(_ctx.Title ?? "");
            if (_ctx.LocalizeText)
                Debug.LogWarning("[OptionsPanel] `Context.LocalizeText = true` —— 原版这两行字要过 I2 词条，"
                               + "而**词条表在远端 CCD**（本地一个 value 都没有）⇒ 字串**原样用**、没过词条。"
                               + "⚠️ **调用方若要中文**：本仓那两套文案在 `Core/Loc.cs`，把 `Loc.T(键)` 的结果喂进"
                               + "`Context` 即可（首例 = `Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`，"
                               + "它传的就是 `Loc.T(\"MenuDeck/Share/…\")`）—— 本条**照旧出声**，⛔ 不压掉。");
            RebuildButtons();
            if (_ctx.AnchorPosition.HasValue)
                Debug.Log("[OptionsPanel] `SetPosition(" + _ctx.AnchorPosition.Value.x.ToString("F2") + ","
                        + _ctx.AnchorPosition.Value.y.ToString("F2") + ")` —— 面板**居中到该点**"
                        + "（原版 `SetPosition.c`：`position = 锚点` 之后 `anchoredPosition.y −= 高度 × 0.5`）。");
        }

        /// <summary>按 `_ctx.Buttons` 重建 `Buttons` 里的那一列（= 原版 `SetButtons`：
        /// `DestroyAllChildren(buttonHolder)` → 逐颗 `Instantiate(buttonTemplate, buttonHolder)`
        /// → 写字 → 挂回调 → `SetActive(true)` → `interactable`）。
        /// <para>⚠️ 原版**不**把 `buttonHolder` 自己隐藏：0 颗按钮时它就是**空容器**（高 0）。
        /// ⚠️ 我们**不**用 `Instantiate` —— 本仓的渲染是自建 mesh、没有预制体系统（同全仓口径）：
        /// 逐颗按 `Template` 的同一份几何现建。</para></summary>
        void RebuildButtons()
        {
            if (_buttons == null) return;
            MenuDraw.ClearChildren(_buttons);
            _btnNodes.Clear();
            var arr = _ctx.Buttons ?? new OptButton[0];
            int n = arr.Length;
            var R = RootRect(n);
            for (int i = 0; i < n; i++)
            {
                var br = BtnRect(R, i);
                var bn = MenuDraw.Node(_buttons, "Button " + i, br);
                var col = arr[i].ColorOverride ?? Color.white;
                // 视觉①（原版 `m_DisabledColor`）；视觉② = 下面那句 `wb.Interactable`（🆕 A1726，九宫格 9 块全灰）。
                if (!arr[i].Interactable) col = new Color(0.784f, 0.784f, 0.784f, 0.502f);   // 原版 `m_DisabledColor`
                var art = Tex(ArtBtn, "按钮底");
                GameObject bgo = null;
                if (art != null)
                    bgo = MenuDraw.Nine(bn, art, br, new Vector4(333f, 96f, 333f, 96f), 410f, 124f, QBtnBg, col,
                                        true, "Image",
                                        new Vector4(333f / 3f, 96f / 3f, 333f / 3f, 96f / 3f));
                var txR = BtnTextRect(br);
                var tx = MenuDraw.TextBox(bn, txR, arr[i].Text ?? "", Color.white, "Button Text",
                                          BtnTextFont, BtnTextFontMin, QBtnText, BtnTextFontMax, BtnTextFontBase);
                if (tx != null && (tx.Text == null || tx.Text.Length == 0))
                    Debug.LogWarning("[OptionsPanel] 第 " + i + " 颗按钮的文本是空的 ⇒ 底图上没有字。");
                var item = arr[i];
                // 换图那一跳走 `BindNine`（九宫格切成 9 张 ⇒ 只换中心那格 = 边框不跟着亮；见 `WindowButton.BindNine`）。
                // 🆕 **2026-10-18（A1053）**：**命中区 = 可射线件的并集** —— 原版这颗钮的子树里
                //    底 `UI_Button_Mulligan`（357.30×**60**）与 **`Button Text`（330.77×**86.17**，`RT=1`）
                //    两颗都吃射线**，文字**上下各凸 13.1**（`AspectRatioFitter(1, 3.83864)` 撑到
                //    330.77/3.83864 = 86.17、**竖直居中**在钮上）⇒ 并集 = **357.30 × 86.17**
                //    （⛔ 不是按钮那 60 高）。判据（原版 prefab 亲读；`Button Text` 静态 `m_SizeDelta` 是 0、
                //    高度由那条 `AspectRatioFitter` 算式定）=
                //    `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "Generic Options Panel" --depth 8`。
                //    ⚠️ 我们画的文字本来就按 `BtnTextRect(br)`（330.77×86.17 居中）画 ⇒ 并集 = 「按钮的
                //    x 两边 + 文字矩形那两条 y 边」。
                var hit = MenuDraw.Hit(bn, "Hit", new PxRect(br.x1, txR.y1, br.x2, txR.y2),
                                       QHit, () => OnButton(item, i));
                if (hit != null && bgo != null)
                {
                    var wb = hit.GetComponent<WindowButton>();
                    if (wb != null)
                    {
                        wb.BindNine(bgo, ArtBtn, ArtBtnHover, ArtBtnPressed);
                        // 🆕 **2026-10-13（A1726）：第一道闸（`WindowButton.Interactable`）【接上了】。**
                        //    判据 = 原版那一刻走的就是 `Selectable.set_interactable` —— **两半一起**：
                        //      ① `DoStateTransition(4 /*Disabled*/)` ⇒ 图形件材质换 `Everguild/UI/Greyscale`
                        //         （我们这一侧 = `RefreshGray`，见 `WindowButton` 头部那段 + `OptButton.Interactable` 的 doc）；
                        //      ② `Selectable.OnPointerClick` 头一句 `!IsInteractable() ⇒ return`（连派发都没有）。
                        //  🔴 **本扇那颗钮的原版字段逐值实读**（2026-10-13 · `bundle_menus_assets_all`；
                        //    沿 `m_Children` 走完整棵 `Generic Options Panel`，GO 根 pid `-688117970605141870`）：
                        //     `Template` GO（pid `1476224195404599442`）上的 **`EverguildButton` MB `-7346004311159172974`**
                        //     ⇒ **`colorTintGreyOnDisable = 1`** · `colorTintGreyOnDisableAlpha = 1.0` · `softDisabled = 0` ·
                        //     `m_Transition = 2`(SpriteSwap) · `m_DisabledColor = (0.7843137, …, a = 0.5019608)`。
                        //     ⚠️ 这一读把**两个前提**都坐实了，⛔ 不是「看着像」：
                        //       · `colorTintGreyOnDisable = 1` ⇒ 原版**真的会灰**（`EverguildButton.SoftDisable/ SwitchMaterial`
                        //         的第一句就是 `if (*(char *)(this + 0x17a) != '\0')`；`DoStateTransition.c`
                        //         的 `bVar4 = state != 4 && !softDisabled` ⇒ `SetToStateActiveOrDisabled`）；
                        //       · `m_DisabledColor` 逐位 = 我们 `RebuildButtons` 里那个常量（**这一格原版也真采**
                        //         —— `DoStateTransition.c` 头那句 `if (m_Transition == 1 ‖ …) goto skip` 对
                        //         `m_Transition = 2` **不成立** ⇒ 那个按状态上色的块**会跑**，Disabled 取 `+0x94` 那一格）。
                        //    ⚠️ **必须排在 `BindNine` 【之后】**（同 `Shell/CampaignRewardWindow.cs:844` 那条纪律）：
                        //       `GrayTargets()` 里的 `target` 是 `Bind` 才设成 `_nine[0]` 的，排在前面的话
                        //       头一次 `RefreshGray()` 会灰错一组、而 `GrayedForTest` 随即为真 ⇒ 后面再设也早退。
                        //    ✅ **原来本扇「故意不接」**（那时 `GrayTargets()` 只认「命中区子树 + 一颗 `target`」
                        //       ⇒ 九宫格只会灰 1/9）—— **`A1719` 已把那个档修好**（第三组 `_nine`）、
                        //       **`A1725`** 又补上第四组（`TintOn` 那几颗）⇒ 现在置 `Interactable = false`
                        //       灰的是**全部 9 块**。旧措辞的更正痕 → `OptButton.Interactable` 的 doc。
                        //    ⚠️ 置 `true` 时是**恒等变换**（`RefreshGray` 头一句 `want == GrayedForTest ⇒ return`）⇒ 老站点零影响。
                        wb.Interactable = arr[i].Interactable;
                    }
                    else Debug.LogWarning("[OptionsPanel] 第 " + i + " 颗按钮的命中区上没有 `WindowButton`"
                                        + " ⇒ 悬停/按下**不换图**。");
                }
                _btnNodes.Add(bn);
            }
            BuiltButtons = n;
        }

        /// <summary>原版每颗按钮的回调：**先过 interactable 闸 → 再跑 `Button.OnClick`**，`AutoClose` 为真时
        /// **最后**关窗（= 原版 `SetButtons` 给每颗多挂一个 `AddListener(this.Close)` 那一跳）。
        /// <para>🔴 **2026-10-12（`A1045`）两条新闸**（本轮之前这里**只打一行日志** —— 调用方挂的动作
        /// **根本没有地方跑**，等于「点了什么都不发生」）：
        /// ① `Interactable == false` ⇒ **动作照进来、立刻返回**（原版那一刻连派发都没有）；② `OnClick == null`
        /// ⇒ **出声**（缺的是原版那颗 `UnityEvent`）。两条都出声 = 本仓红线「不许静默失败」。</para>
        /// <para>🆕 **2026-10-13（`A1726`）**：**第一道闸接上了**（`RebuildButtons` 里那颗
        /// `wb.Interactable = arr[i].Interactable`，见 `OptButton.Interactable` 的 doc）⇒
        /// 门关着时**先**在 `WindowButton.Click()` 头一句就 `return`（并出声）—— ① 这一条**正常走不到**了。
        /// **保留**（口径同 `Shell/DeckInfoPopup.cs` 的 `Blocked`）：它花的是**同一份布尔**，
        /// 万一第一道闸被别处放开，这里仍然挡得住 + 出声 —— 「两处写同一条规则」的判据是**判据只有一份**，
        /// 不是「不许有第二道兜底」。</para></summary>
        void OnButton(OptButton b, int i)
        {
            // ① 第二道闸（口径同 `Shell/DeckInfoPopup.cs` 的 `Blocked`）——
            //     ⚠️ **不连 `AutoClose` 一起跑**：原版没派发 ⇒ 也就没有 `Close` 那一下。
            if (!b.Interactable)
            {
                Debug.LogWarning("[OptionsPanel] 点了第 " + i + " 颗按钮 `" + (b.Text ?? "") + "`，"
                               + "但它 `Interactable = false` ⇒ **这一下什么都不做**"
                               + "（原版 `Selectable.OnPointerClick` 头一句就返回 ⇒ 连派发都没有）。"
                               + "⚠️ 出声是因为玩家点了没反应 —— 红线：不许静默失败。");
                return;
            }
            Debug.Log("[OptionsPanel] 点了第 " + i + " 颗按钮 `" + (b.Text ?? "") + "`"
                    + (b.OnClick != null ? "（回调跑起来了）" : "")
                    + (b.AutoClose ? "（`AutoClose = true` ⇒ 顺带关窗 —— 照原版 `SetButtons.c` 那一跳）" : "")
                    + "。");
            // ② 调用方挂的那颗 `UnityEvent`（原版 `Button.OnClick`）。
            if (b.OnClick != null) b.OnClick();
            else
                Debug.LogWarning("[OptionsPanel] 第 " + i + " 颗按钮 `" + (b.Text ?? "") + "` **没有挂回调**"
                               + "（原版那颗 `UnityEvent` 是调用方挂的，见 `OptButton.OnClick`）"
                               + "⇒ 这一下除了关窗**什么都没发生**（出声，不静默）。");
            if (b.AutoClose) Close();
        }

        // ============================================================ 取图（**取不到必须出声**）
        readonly System.Collections.Generic.List<string> _missArt = new System.Collections.Generic.List<string>();
        /// <summary>本窗**取不到的图**（自检读口 —— 非空就是「有件根本没画」，同 `TrophyInfoPopup.MissingArt`）。
        /// ⚠️ `MenuDraw.Rect/Nine` 对 `tex == null` 是**静默返回 null**（连节点都不建）⇒ 必须谁取谁报。</summary>
        public System.Collections.Generic.List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[OptionsPanel] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        /// <summary>自检用：把当前铺出来的东西摊开（同 `TrophyInfoPopup.DebugDump` 那条先例）。</summary>
        public string DebugDump()
        {
            var R = RootRect(BuiltButtons);
            return "title=\"" + (_title != null ? _title.Text : "-") + "\""
                 + " buttons=" + BuiltButtons
                 + " root=" + R.x1.ToString("F2") + "," + R.y1.ToString("F2") + "→"
                 + R.x2.ToString("F2") + "," + R.y2.ToString("F2")
                 + " h=" + R.H.ToString("F2")
                 + " templateActive=" + (_template != null && _template.gameObject.activeSelf)
                 + " missingArt=" + _missArt.Count;
        }

        /// <summary>自检用：建出来的 `Buttons` 直系子件名（**直系**，不是整棵子树 —— 同 `NthChild` 那条纪律）。</summary>
        public System.Collections.Generic.List<string> ButtonNames()
        {
            var l = new System.Collections.Generic.List<string>();
            if (_buttons != null)
                foreach (Transform c in _buttons) l.Add(c.name);
            return l;
        }

        /// <summary>`_buttons` 容器（自检读口）。</summary>
        public Transform ButtonsNode { get { return _buttons; } }
        /// <summary>`Template` 节点（**应当是关着的**；自检读口）。</summary>
        public Transform TemplateNode { get { return _template; } }
        /// <summary>`Template` 那颗按钮底（自检读口）。</summary>
        public GameObject TemplateImage { get { return _templateImg; } }

        // ============================================================ 对账表（**纯数据 · 不参与渲染**）
        /// <summary>一个节点的**冻结事实**：路径 / **相对根左上角的框** / 出厂 `activeSelf`。
        /// <para>🔴 **本表是「照抄」那一侧的记录**：每一条都逐格对着
        /// `python 工具/menu_dump.py bundle_menus_assets_all "Generic Options Panel" --depth 12 --relative --md`
        /// 的现读值抄（**相对框**那一档，所以不需要知道根被摆在画布哪里）。
        /// 对账脚本 `_tmp_view/wl2/check_table.py` 会把它与现读**逐格比**（不符必须是 0）。</para>
        /// <para>⚠️ **根那一行（`0,0→387.30,126.60`）是 prefab 出厂那一档**（`Template` 还开着）；
        /// **运行期**我们建的是 61.6 高那一档（见文件头②）⇒ 断言**不拿这一行比我们建出来的根**，
        /// 而是比 `RootH(0) == 61.6`（另有一条独立断言）。⛔ 别把两档搅在一起。</para></summary>
        public struct ReconRow
        {
            public string Path;
            public PxRect R;
            public bool On;
            public ReconRow(string p, PxRect r, bool on = true) { Path = p; R = r; On = on; }
        }

        /// <summary>逐节点对账表（**8 条 = 原版 8 个节点**，逐条 `--relative` 现读）。</summary>
        public static readonly ReconRow[] Recon =
        {
            new ReconRow("Generic Options Panel",       new PxRect(0f,          0f,        387.30f,    126.60f)),
            new ReconRow("Menu Dark Background",        new PxRect(-2093.65f,  -1222.88f, 2480.95f,  1349.48f)),
            new ReconRow("bg shadow",                   new PxRect(-32.45f,    -39.49f,   418.02f,    166.09f)),
            new ReconRow("bg",                          new PxRect(0f,          0f,        387.30f,    126.60f)),
            new ReconRow("Name",                        new PxRect(16.15f,      10.00f,    371.15f,    46.60f)),
            new ReconRow("Template",                    new PxRect(15.00f,      51.60f,    372.30f,    111.60f)),
            new ReconRow("Template/Button Text",        new PxRect(27.69f,      38.46f,    358.46f,    124.62f)),
            new ReconRow("Buttons",                     new PxRect(193.65f,     116.60f,   193.65f,    116.60f)),
        };
    }
}
