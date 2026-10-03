// MissionRerollPopup.cs — 🆕 A23：每日任务行那颗「垃圾桶」开的重摇窗（原版 `MissionReRollPopup`）
//
// ============================ 出处（唯一正本） ============================
// **`资料/待办判据_阶段二与联机.md` §A23**（机制链 + `ReRollPopup Variant` 的逐节点实测表，2026-10-04 查实）。
// 复现命令（可复算本文里每一个数字）：
//     `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "ReRollPopup Variant" --depth 6`
//     （绝对矩形 · 1920×1080 · 左上原点 · y 向下）
//
// **窗口字段**（MB 实读，prefab 根 `ReRollPopup Variant` 上的 `MissionReRollPopup`）：
//   `type = 1 (Popup)` · `windowsPlacement = 15 (Popup)` · **`closeOnESC = 0`** ·
//   `extraScaleSmallScreen = 1.0` · `updateNavPanel = 0` · `useDefaultCloseSoundIfNull = 1` ·
//   `openSound` / `closeSound` = 空。
//   绑定字段（**全部运行期赋**）：`acceptButton` / `cancelButton` / `description`，
//   以及 `Price Display>Generic UI Button` 上的 `PriceDisplayButton`（`blinkEffect`/`button`/`priceDisplay`）。
//
// **图**（按 pid 反查**真包**定的名 —— 解包目录里这几个 pid 反查不到，走 `sprite_pid_map()`）：
//   `Generic Popup Background` / `Mask` = `40k_popup`（359×336 · 九宫 **(169,160,169,160)** · Sliced）·
//   `Background fill` = `40k_popup_texture`（128×128 · **Tiled** `ppuMul=2.0` ⇒ 一格 128÷2 = **64 画布 px**）·
//   两颗钮的底 = `40K_button`（489×107 · **Simple + preserveAspect** · 色 **(0.369,0.894,0.587,1)** 绿）。
//   🔴 **换图**：两颗钮的 `EverguildButton` 都是 `trans=2 (SpriteSwap)` ⇒
//   `m_SpriteState.m_HighlightedSprite = 40K_button_hover` · `m_PressedSprite = 40K_button_pressed`（实读）。
//
// ---- 点它干什么（原版链，反编译实证）----
//   `MissionReRollButton.__c__DisplayClass3_0___Setup_b__0.c`：
//     `WindowsManager.OpenWindow(<WindowsManager>.Instance, *(button+0x30), new MissionReRollPopupContext{…})`
//   `MissionReRollPopup__Open.c`：给 `description` 赋值 → `PriceDisplayButton.Setup(priceDisplay, price, 1, 0)`
//     → 把 `priceDisplay.OnClick` 接到 `acceptButton` 那个处理函数上。
//   `MissionReRollPopup.__c__DisplayClass3_0___Open_b__0.c`（= `Confirm`）：
//     `PriceDisplayButton.Interactable = false` → 发 `RerollChallenge`（**服务端**）。
//   `MissionReRollPopup__Close.c`：若是从带页签的窗里开的，先 `ChangeTab` 回那一页再 `Close`。
//
// ---- 🔴 四条「我们挑的 / 查不到」（铁律 3：逐条标出来，不许冒充原版）----
//   ① **换一条的规则全是我们定的**（原版走服务端 `MissionEvent…RerollChallenge` + `ReRollMissionResult`）
//      —— 规则与池子在 `DailyData.RerollDaily`（那里逐条标着）。**单机没有服务器**。
//   ② **不扣钱**：价钱格（`icon` 56² scl1.2 + `text` 占位 `300,00`）**照原版外观建**，
//      但点 `Confirm` **不扣任何资源** —— 用户 2026-09-17 拍板「**不做真实经济**：资源固定 9999」。
//      ⇒ 这是铁律 11 允许的「用户明确拍板不做」那一档，**不是我们偷懒**；价钱格**没删**（删了才是偏离）。
//      ⚠️ `MissionData.get_RerollPrice` 的方法体在 `d:/2/tools/decomp_full/` 里是**错桩**（读出来是别的函数）
//      ⇒ **真价钱本地读不到**；`300,00` 是 **prefab 出厂占位**（与 `BoosterInfoPopup` 同一个），**别当真实价钱**。
//   ③ **`icon` 只有节点、不画图**：原版那一格的 `Image.m_Sprite` 出厂就是**空**，
//      运行期由 `PriceDisplay__Setup` 按 `CurrencyExtensions.ToCurrency(price.Type)` 赋**那枚货币的图标** ——
//      价钱与货币本地都读不到 ⇒ **没有判据可画**。照 `BoosterInfoPopup.foreground` 那条先例：
//      **只建节点、不画**（不拿别的货币图冒充），并在日志里出声。
//   ④ **`closeOnESC = 0`（ESC 不关这扇窗）** —— MB 原文如此。⚠️ 派单里写的是「ESC ⇒ 关窗什么都不做」，
//      与 MB 实读**相反**；这里**按原版**（工程里 `closeOnEsc` 目前只是**记录值**，
//      全工程没有任何一处消费它：`grep closeOnEsc` 只有各窗 `Create()` 里的赋值 + 断言）。
//
// ---- ⚠️ 两处我们的引擎做不到、按等效做法处理（都出声）----
//   · `Mask`（`Image` + `Mask` **`showGraphic = 0`**）：原版这一件**不渲染**（只当模板用）。
//     我们的引擎没有 stencil mask ⇒ **只建节点、不画那张 `40k_popup`**（画了会变成**双层边框**）。
//     它底下 `Background fill` 的矩形与它**完全相等** ⇒ 几何上「裁到 `Mask` 框内」是恒等操作，视觉一致。
//   · `Buttons` / `Price Display` 两处 `HorizontalLayoutGroup` 的运行期排布：
//     我们不用布局系统，**直接照布局跑完的实测矩形摆**（`icon` 56² 与 `text` 左边缘 1202.8 是实测值；
//     `Confirm` 那一段的右边缘 = 价钱格左边缘 1141.2 − 间距 **12.5** = **1128.8**，与实测的 1128.8 吻合）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>开这扇窗时塞给它的东西（原版 `MissionReRollPopupContext` 的等价物）。
    /// 原版那两个字段是 `Mission` / `Challenge`（服务端对象）⇒ 我们**只带一个行号**，
    /// 另加一个「换完了叫我」的回调（宿主 `MissionsTab` 拿它重建整页）。</summary>
    public sealed class MissionRerollContext
    {
        /// <summary>要重摇的那一行（`Daily Missions Holder` 里第几行，0 起）。</summary>
        public int Index;
        /// <summary>换完之后的通知（可空 —— 自检会直调，没有宿主也能跑）。</summary>
        public System.Action OnRerolled;
    }

    /// <summary>原版 `MissionReRollPopup`（`ReRollPopup Variant`）—— 点每日任务行那颗垃圾桶开的「重摇」确认窗。</summary>
    public class MissionRerollPopup : GameWindow
    {
        // 渲染队列：**必须高于所有「页」**（奖励窗那一档最高 3014、锻造页到 3027、战役页到 3064）。
        // 3080–3099 是全工程的一段**空档**（`BoosterInfoPopup` 到 3079、练习窗从 3100 起、
        // `CardDetailPopup` 从 3105 起）⇒ 放这里既在页之上、又在别的弹窗之下。
        // ⚠️ 新增任何一页/一扇窗都要回头看这个数（`RewardsScene` 有一条断言钉住「弹窗 > 页 > 窗」）。
        public const int QBase = 3080;
        const int QShade = QBase,          // 压暗整屏
                  QPanel = QBase + 1,      // 窗底（九宫格）
                  QFill = QBase + 2,       // 内部平铺底纹
                  QText = QBase + 3,       // 文案
                  QBtn = QBase + 4,        // 两颗钮的底图
                  QBtnText = QBase + 5,    // 钮上的字
                  QPrice = QBase + 6,      // 价钱格的字
                  QHit = QBase + 8;        // 窗内命中区（压暗层那一档 = `QShade` 的注释见下）

        /// <summary>「点窗外关窗」那个命中区的档（= 压暗层自己那一档）。
        /// 🔴 **判据 = `BoosterInfoPopup.QShadeHit` 那条**（2026-10-03 实测踩出来的）：
        /// `ImageQuad` 的世界 z 恒为 0 ⇒ **同队列时 `PointerLayer` 只能靠枚举顺序挑赢家** ⇒
        /// 压暗层会把窗内的命中区抢走。要唯一只能靠**队列分档**：
        /// 压暗层**严格低于**窗内容（`QHit`）、同时**高于它下面那一层**（奖励窗那一档最高 3014）。</summary>
        const int QShadeHit = QShade;

        // ============================================================ 真值（§A23 二 那张逐节点表）

        static readonly PxRect DarkR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkTint = new Color(0f, 0f, 0f, 0.773f);      // `Menu Dark Background` 的色
        static readonly PxRect WindowR = new PxRect(535f, 245f, 1385f, 675f);   // **850 × 430**

        const string ArtPopup = "40k_popup";                 // 359×336 · 九宫 (169,160,169,160)
        const float PopupTexW = 359f, PopupTexH = 336f;
        static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);

        static readonly PxRect MaskR = new PxRect(545.4f, 254.4f, 1375.1f, 665.2f);
        const string ArtFill = "40k_popup_texture";          // 128×128 · Tiled ppuMul 2.0 ⇒ 一格 64
        const float FillTilePx = 64f;

        static readonly PxRect MsgR = new PxRect(575f, 273.8f, 1345f, 546.2f);          // 770 × 272.4
        /// <summary>`MessageText` 的原文（prefab 出厂文本；原版运行期会被 I2 词条覆盖，
        /// **词条表在远端 CCD、本地一个 value 都没有** ⇒ 照别处的做法用**出厂原文**，不自己编词）。</summary>
        public const string TxtMessage = "Discard this mission and receive a new one?";

        static readonly PxRect ButtonsR = new PxRect(572.3f, 534f, 1347.7f, 624f);     // HLG sp 0
        static readonly PxRect CancelR = new PxRect(610f, 541f, 960f, 617f);           // **350 × 76**
        static readonly PxRect CancelTxR = new PxRect(623f, 541f, 947f, 617f);         // `sz=(-26,0)`
        static readonly PxRect PriceBtnR = new PxRect(960f, 541f, 1310f, 617f);        // **350 × 76**
        /// <summary>`Confirm` 那段字的**右边缘** = 价钱格左边缘 1141.2 − `Generic UI Button` 那条 HLG 的
        /// 间距 **12.5** = **1128.8**（与 prefab 里 `Button Text` 的实测左边缘 1128.8 吻合 ⇒ 零宽框 = 右边缘）。</summary>
        const float ConfirmTextR = 1128.8f;
        /// <summary>价钱格（内层 `Price Display`，`HorizontalLayoutGroup` sp **5.5** · pad 0,0,10,10）。
        /// 它自己被 `ContentSizeFitter` 撑开 ⇒ 出厂矩形宽 0；**有尺寸的是它的两个子件**。</summary>
        static readonly PxRect PriceCellR = new PxRect(1141.2f, 541f, 1141.2f, 617f);
        static readonly PxRect IconR = new PxRect(1141.2f, 551f, 1197.2f, 607f);       // **56²** · `scl=1.2`
        static readonly PxRect PriceTextR = new PxRect(1202.8f, 551f, 1202.8f, 607f);  // = 1197.2 + 5.5
        /// <summary>价钱那格的**出厂占位串**（原版运行期按 `RerollPrice` 覆盖）。⚠️ **不是真价钱**（见文件头 ②）。</summary>
        public const string PricePlaceholder = "300,00";

        const string ArtButton = "40K_button";
        static readonly Color BtnTint = new Color(0.369f, 0.894f, 0.587f, 1f);         // 绿（两颗都一样）
        static readonly Color BtnTint2 = new Color(0.369f, 0.894f, 0.588f, 1f);        // `Generic UI Button` 的实测值

        // ============================================================ 状态

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static MissionRerollPopup LastOpened { get; private set; }

        /// <summary>要重摇的那一行（0 起）。</summary>
        public int Index { get; private set; }
        /// <summary>换完之后的通知（宿主 `MissionsTab.Build`）。</summary>
        System.Action _onRerolled;

        /// <summary>取不到的图（出声用 —— 红线：不许静默失败）。</summary>
        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        // 自检要按名字找的几个节点
        public Transform WindowNode { get; private set; }
        public Transform MsgNode { get; private set; }
        public Transform CancelNode { get; private set; }
        public Transform ConfirmNode { get; private set; }
        public Transform PriceCellNode { get; private set; }
        public Transform IconNode { get; private set; }
        public Transform PriceTextNode { get; private set; }
        /// <summary>最近一次 `Confirm` 换出来的新任务描述（自检用；没换过 = null）。</summary>
        public string RerolledTo { get; private set; }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        protected override void SetupData(object data)
        {
            base.SetupData(data);
            var ctx = data as MissionRerollContext;
            Index = ctx != null ? ctx.Index : 0;
            _onRerolled = ctx != null ? ctx.OnRerolled : null;
        }

        // ---------------------------------------------------------- 开

        public static MissionRerollPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("ReRollPopup Variant");
            var win = go.AddComponent<MissionRerollPopup>();
            win.type = WindowType.Popup;                       // 实证 type = 1
            win.placement = WindowsPlacement.Popup;            // 实证 windowsPlacement = 15
            // 🔴 实证 `closeOnESC = 0` —— ESC **不关**这扇窗（与 `PromptPopup` 那扇同值）。
            // ⚠️ 派单里写的是「ESC ⇒ 关窗」，与 MB 原文冲突 ⇒ **按原版**（文件头 ④）。
            win.closeOnEsc = false;
            win.extraScaleSmallScreen = 1f;                    // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        // ---------------------------------------------------------- 建

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();

            // ① 压暗整屏（原版 `Menu Dark Background`：**无图**、只有色 (0,0,0,0.773)）
            //    它上面挂着 `BackgroundCloseButton`（原版那个组件就是「点它关窗」）
            var dark = MenuDraw.Node(transform, "Menu Dark Background", DarkR);
            MenuDraw.Rect(dark, CardArt.Solid(), DarkR, "Image", QShade, DarkTint);
            MenuDraw.Hit(dark, "CloseHit", DarkR, QShadeHit, () => Close());   // 见 `QShadeHit` 的注释

            // ② `Window`（850×430）—— 原版这一件**没有 Image**（底在同名的子节点上）⇒ 只建节点
            WindowNode = MenuDraw.Node(transform, "Window", WindowR);
            var bg = MenuDraw.Node(WindowNode, "Generic Popup Background", WindowR);
            Nine(bg, ArtPopup, WindowR, PopupBorder, "Image", QPanel);

            // ③ `Mask`（`Image` + `Mask` **`showGraphic = 0`** ⇒ 原版**不渲染它**）+ 它的子件 `Background fill`
            //    ⚠️ 我们的引擎没有 stencil mask ⇒ 只建节点、**不画那张 `40k_popup`**（画了会变成双层边框）；
            //       `Background fill` 的矩形与 `Mask` **完全相等** ⇒ 「裁到 Mask 框内」是恒等操作，视觉一致。
            var mask = MenuDraw.Node(bg, "Mask", MaskR);
            MenuDraw.Tiled(mask, Art(ArtFill), MaskR, FillTilePx, QFill, "Background fill");

            // ④ 文案：fs40 · auto[4~40] · 居中/居中 · **折行 = 1**（原版这条 TMP 自己就是 `MessageText` 节点）
            var msg = MenuDraw.TextBox(WindowNode, MsgR, TxtMessage, Color.white, "MessageText", 40f, 4f, QText);
            if (msg == null) Debug.LogWarning("[MissionReroll] `MessageText` 没建出来（红线：不许静默失败）");
            else MsgNode = msg.transform;

            // ⑤ 两颗钮（原版 `Buttons` 是 HLG sp 0 ⇒ 两个矩形直接照实测摆）
            var btns = MenuDraw.Node(WindowNode, "Buttons", ButtonsR);
            CancelNode = BuildButton(btns, "ButtonLeft", CancelR, CancelTxR, "Cancel", BtnTint, 324f, 76f,
                                     () => Close());
            var priceDisplay = MenuDraw.Node(btns, "Price Display", PriceBtnR);   // 原版这一层是 `PriceDisplayButton`
            ConfirmNode = BuildButton(priceDisplay, "Generic UI Button", PriceBtnR,
                                      new PxRect(ConfirmTextR, 541f, ConfirmTextR, 617f), "Confirm ", BtnTint2,
                                      0f, 0f, Confirm, rightAlign: true);
            BuildPriceCell(ConfirmNode);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[MissionReroll] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            HintGaps();
        }

        /// <summary>一颗钮：底 `40K_button`（**Simple + preserveAspect** · 绿）+ `Button Text`
        /// （fs38 · auto[12~38] · 折行 0）+ 命中区（`EverguildButton` `trans=2` ⇒ **悬停换图**）。
        /// `rightAlign` 时 `tx` 只有右边界有意义（`Confirm` 那一颗的框是**零宽**的，见 `ConfirmTextR`）。</summary>
        Transform BuildButton(Transform parent, string nodeName, PxRect r, PxRect tx, string label, Color tint,
                              float autoBoxW, float autoBoxH, System.Action onClick, bool rightAlign = false)
        {
            var n = MenuDraw.Node(parent, nodeName, r);
            // 底图：`40K_button` 489×107 塞进 350×76 ⇒ 原版 `preserveAspect` ⇒ 画 **347.3×76**（居中）
            var tex = Art(ArtButton);
            ImageQuad q = null;
            if (tex != null) q = MenuDraw.Rect(n, tex, r, "Image", QBtn, tint, true);
            // 命中区：整颗钮；`target` = 底图那块 ⇒ 悬停换 `40K_button_hover`、按下 `40K_button_pressed`
            //（两张图名都是**实读 prefab 的 `m_SpriteState`**，不是我们按 `_hover` 规律猜的 —— 恰好同值）
            MenuDraw.Hit(n, "Hit", r, QHit, onClick, q, ArtButton);

            var lb = MenuDraw.Text(n, tx, label, Color.white, "Button Text", 38f, QBtnText);
            if (lb != null)
            {
                if (rightAlign) MenuDraw.AlignRight(lb, tx);
                else if (autoBoxW > 0f && autoBoxH > 0f)
                    lb.SetAutoFitBox(LayoutSpace.Px(autoBoxW), LayoutSpace.Px(autoBoxH), 12f, 38f);
            }
            return n;
        }

        /// <summary>价钱格（内层 `Price Display`）：`icon` 56² × `scl=1.2` + `text`（左边缘 1202.8）。
        /// 🔴 **`icon` 只有节点、不画图**（原版出厂 `m_Sprite` 空、运行期按货币赋图 —— 见文件头 ③）。</summary>
        void BuildPriceCell(Transform parent)
        {
            PriceCellNode = MenuDraw.Node(parent, "Price Display", PriceCellR);
            IconNode = MenuDraw.Node(PriceCellNode, "icon", IconR);      // `scl=1.2`：**画出来**是 67.2²（同中心）
            // ⚠️ 原版这一格是 `BlinkGraphic` 会呼吸的那一枚货币图标 —— 我们**没有那张图**
            //    （`MissionData.get_RerollPrice` 读不到、货币类型也读不到）⇒ 留空 + 出声。
            var pl = MenuDraw.Text(PriceCellNode, PriceTextR, PricePlaceholder, Color.white, "text", 40f, QPrice);
            if (pl == null) Debug.LogWarning("[MissionReroll] 价钱那格的字没建出来（红线：不许静默失败）");
            else { PriceTextNode = pl.transform; MenuDraw.AlignLeft(pl, PriceTextR); }
        }

        // ---------------------------------------------------------- 行为

        /// <summary>`Confirm`（原版 `acceptButton` → `RerollChallenge`）。**换一条新任务、关窗**，
        /// 然后通知宿主重建整页。⚠️ **不扣任何资源**（用户 2026-09-17 边界②）。</summary>
        void Confirm()
        {
            RerolledTo = DailyData.RerollDaily(Index);
            Close();                       // 先关再回调（照 `PromptPopup.Choose`）—— 回调里可能再开窗
            Debug.Log("[MissionReroll] `Confirm` ⇒ 换了第 " + (Index + 1) + " 条任务：「" + RerolledTo
                      + "」（⚠️ **不扣钱**：用户 2026-09-17 拍板「不做真实经济、资源固定 9999」；"
                      + "原版这一步会扣 `RerollPrice` 并走服务端）");
            if (_onRerolled != null) _onRerolled();
        }

        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q, Color? tint = null)
        {
            var t = Art(art);
            return t == null ? null : MenuDraw.Nine(p, t, r, b, PopupTexW, PopupTexH, q, tint, true, n);
        }

        /// <summary>把「原版有、我们没做 / 读不到」的几处**当场出声**（红线：不许静默失败）。</summary>
        void HintGaps()
        {
            Debug.Log("[MissionReroll] 四件事如实标注：① `icon`（价钱左边那枚货币图标）**只建节点、不画图** —— "
                      + "原版出厂 `m_Sprite` 空、运行期按 `RerollPrice` 的货币赋（`MissionData.get_RerollPrice` "
                      + "在本地反编译里是**错桩** ⇒ 价钱与货币都读不到）；② 价钱 `" + PricePlaceholder
                      + "` 是 **prefab 出厂占位**、**不是真价钱**；③ 点 `Confirm` **不扣钱**"
                      + "（用户 2026-09-17 边界②：「不做真实经济」）；④ `Mask` 那一件原版 `showGraphic = 0` "
                      + "**不渲染** ⇒ 我们只建节点、不画那张 `40k_popup`（画了会变双层边框）。");
        }

        public string Dump()
        {
            return "MissionReroll[行 " + (Index + 1) + "] 窗 " + WindowR.W + "×" + WindowR.H
                   + " · 换到「" + (RerolledTo ?? "（还没换）") + "」 · 取不到的图 " + MissingArt.Count + " 张";
        }
    }
}
