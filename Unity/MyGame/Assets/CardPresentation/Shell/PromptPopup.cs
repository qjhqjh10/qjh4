// PromptPopup.cs — 通用提示窗（照原版 prefab `GenericPromptWindow` 搭的**自己那扇**）
//
// 🔴 **2026-10-12 就地订正（铁律 5 · A455①）**：本行原来写「（「暂无服务器」等的宿主）」—— **那句已过期**：
//   A416（2026-10-12）把 `WindowsManager.ShowPopUp` **收编**到了原版那扇宿主
//   （`PopUpGameWindow` = prefab `MessagePopupWindow{,_2Buttons}`，见 `Shell/PopUpGameWindow.cs`；
//   判据 → `资料/普查产出_1012/H17_ShowPopUp收编.md`）⇒ `ShowPopUp` **不再**开本窗。
//   ⚠️ 本类**仍在用，别删**：有一批站点**直建**它（`Shell/MainMenuRuntime.cs` 的 `EscPressed` 的「退出游戏」弹窗 ·
//   `Editor/ShellScene.cs` 起那几条版面探针）—— 存续理由与清单同 H17 §④。
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §七。**类名照原版**：`PromptPopup : GameWindow`（MB `-8455344173513121409`），
// prefab 根 `GenericPromptWindow`（`RT/-2582785123254017665`，`bundle_menus_assets_all`）。
//
// 🔴 **2026-09-23 换版**：这个文件以前叫 `PopUpGameWindow`（**我们自建**的版面，注释写着
//   「原版那两个 popup prefab 本地没有」）—— **那条已经不成立了**：`GenericPromptWindow`
//   在 `bundle_menus_assets_all` 里参数齐全（21 个节点），现在照它重做（铁律 5：就地改掉错记录）。
//
// ---- 照原版的部分（逐条有出处）----
//   `type=1(Popup)` · `windowsPlacement=15` · `closeOnESC=0` · `extraScaleSmallScreen=1.0`
//   根 `sz=(1919 × 1079)` · `Menu Dark Background` `sz=(4574.6 × 2572.36)` 无 sprite、色 **(0,0,0,0.7725)**
//   `Window` `sz=(900 × 0)`（**宽是定的、高由 VLG+CSF 算**）
//   `Generic Popup Background` = `40k_popup` **Sliced**，九宫格 **(169,160,169,160)**、359×336
//   `Mask` `sz=(−20.268, −19.245)` · `Background fill` = `40k_popup_texture` **Tiled**、`ppuMultiplier=2.0`（⇒ 128/2 = **64 一格**）
//   `MessageText` fs=**50** 居中 · **最小高 100**；`Buttons` **最小高 110** · HLG **spacing 40** · **LowerCenter**
//   `CancelButton` / `OkButton` = `40K_button`（489×107）色 **(0.3686,0.8941,0.5874,1)** · `Simple + PreserveAspect` · `sz=(0,80)`
//   `Button Text` fs=**50**（autosize 12→50）居中
//
// ---- 🔴 三处**我们挑的**（原版查不到，按铁律 3 标出来，不许冒充原版）----
//   ① **面板高**：原版是 `VerticalLayoutGroup`（align=4 · sp=0 · ctlH=1）+ `ContentSizeFitter(V=PreferredSize)`
//      在**运行时**算出来的，序列化里只有 `sz=(900, 0)`。我们的引擎没有布局系统 ⇒
//      按「`MessageText` 的实际渲染高（下限 100）+ `Buttons` 的 110」自己算（**同一套语义，不是抄的值**）。
//   ② **只有 Ok 时**：原版 prefab **只有 2 按钮版**，运行时会不会藏 `Cancel` 静态查不到
//      （`STUB:PromptPopup.cs` 是空体签名桩）⇒ 我们**藏 Cancel、把 Ok 居中**，并在日志里说明。
//   ③ **`inputs` / `Error Message` 不建**：它们在 prefab 里出厂 `active=1`，但显然由脚本按状态开关
//      （`PromptPopup` 的字段表里有 `inputHolder` / `inputPrefab` / `errorMessage`）——
//      一个「暂无服务器」提示里出现密码输入框与 `INVALID PASSWORD ERROR` 是不可能的。
//      我们的提示窗没有输入/校验这两种流程 ⇒ **按「脚本字段引用的节点＝代码控制」处理**，不建。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`PromptPopup : GameWindow` —— 通用提示窗（照原版 prefab `GenericPromptWindow` 搭的那扇）。
    /// <para>🔴 **2026-10-12 就地订正（铁律 5 · A455①）**：本行原来写「原版唯一的通用提示窗。**也是**边界③
    /// 「点了如实提示」的**唯一宿主**」—— **后半句不成立了**：A416（2026-10-12）把 `WindowsManager.ShowPopUp`
    /// **收编**到原版那扇宿主（`PopUpGameWindow` = `MessagePopupWindow{,_2Buttons}`，`Shell/PopUpGameWindow.cs`）
    /// ⇒ 本窗**不再是** `ShowPopUp` 的宿主（⚠️ 2026-09-23 起它一度是 —— 那是在「原版那两扇 prefab 本地没有」
    /// 这个**已被推翻**的前提下挑的；判据 → `资料/普查产出_1012/H17_ShowPopUp收编.md`）。</para>
    /// <para>本类**仍被「直建」它的站点开着**（`Shell/MainMenuRuntime.cs` 的 `EscPressed` 的「退出游戏」弹窗 ·
    /// `Editor/ShellScene.cs` 起的版面探针）⇒ ⛔ 别删、别当成「已废弃」。</para></summary>
    public class PromptPopup : GameWindow
    {
        // ---- 出处：正本 §七 的节点表 ----
        public const float ShadeW = 4574.6f, ShadeH = 2572.36f;
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.7725f);
        public const float PanelW = 900f;
        /// <summary>`Generic Popup Background` 的 `sz=(100,100)`（`a=(0,0)-(1,1)`）⇒ 四周各外扩 **50**。</summary>
        public const float BgPad = 50f;
        /// <summary>`Mask` 的 `sz=(−20.268,−19.245)` ⇒ 相对背景四边各内缩一半。</summary>
        public const float MaskInsetX = 20.268f, MaskInsetY = 19.245f;
        public const float MsgMinH = 100f, BtnRowH = 110f, BtnH = 80f, BtnGap = 40f;
        public const float MsgFontPx = 50f, BtnFontPx = 50f;
        /// <summary>🔴 **2026-10-18（W3）补两格（铁律 5）**：`MessageText` 的 TMP 实读四格 =
        /// `m_fontSize 50` · **`m_fontSizeMin 4`** · `m_fontSizeMax 50` · **`m_fontSizeBase 36`** ·
        /// `m_enableAutoSizing = 1` · `m_TextWrappingMode = 1`（dump 原文：`字号=50.0 基准=36.0 auto[4.0~50.0] 对齐=Center/Middle 折行=1`）。
        /// 我们原来只 `SetWrapWidth`、**没接自适应** ⇒ 长文更早折行。判据 →
        /// `资料/普查产出_1018/P2_弹窗排版_通用活动奖励.md` §1（P2-6）。</summary>
        public const float MsgAutoMinPx = 4f, MsgAutoBasePx = 36f;
        public static readonly Color BtnColor = new Color(0.3686f, 0.8941f, 0.5874f, 1f);

        public const string ArtPopup = "40k_popup";              // 359×336 · 九宫格 (169,160,169,160)
        public const string ArtPopupFill = "40k_popup_texture";  // 128×128 · Tiled · ppuMultiplier 2 ⇒ 64 一格
        public const string ArtButton = "40K_button";            // 489×107
        public const float PopupTexW = 359f, PopupTexH = 336f;
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>`40k_popup_texture` 的平铺格（画布 px）= 128 ÷ `ppuMultiplier 2.0`。</summary>
        public const float FillTilePx = 64f;

        // 🔴 渲染队列**必须高于所有「页」**。原来写的是 3018…3023、理由写「奖励窗最高 3014」——
        //    **2026-09-23 更正（铁律 5）：那条前提过期了**。第 3 层那几页的底板用到
        //    `ForgeTab` 3027 / `CampaignTab` 3064 ⇒ 3018 的弹窗**画在页底板下面**，
        //    在战役页/锻造页上弹一个提示会被页盖住，而**矩形断言量不到**（同族于「量矩形量不到被盖住」）。
        //    ⇒ 整段抬到 **3140+**（高于 `CampaignRewardWindow` 3110…3123；`Tooltip` 3199+ 仍在最上）。
        //    ⚠️ 新增任何一页都要回头看这个数 —— `ShellScene` 里有一条断言钉住「弹窗 > 所有已建页」。
        public const int QShade = 3140, QPanel = 3141, QFill = 3142, QContent = 3143, QText = 3144;

        string _text, _okText, _cancelText;
        System.Action _onOk, _onCancel;

        // ---------------------------------------------------------- 🆕 2026-10-27（E9b · 账 545）Error Message 行
        /// <summary>原版 `GenericPromptWindow/Window/Error Message` 的**实测**矩形（`menu_dump.py`
        /// `bundle_menus_assets_all "GenericPromptWindow" --depth 12 --md` 现读）：
        /// `510.00,730.00→1410.00,820.00` = **900 × 90**（VLG 里紧跟 `MessageText` 之后、`Buttons` 之前）。
        /// TMP 实读：`'INVALID PASSWORD ERROR'` · 字号 **40** · 基准 **40** · **无自适应** · `Center/Middle` · `折行=1`。</summary>
        public const float ErrRowH = 90f, ErrFontPx = 40f;
        /// <summary>原版 `PromptPopup.errorColor`（**prefab 序列化字段**，`MonoBehaviour_-8455344173513121409.json` 实读
        /// = **`(1, 0, 0, 1)`**）。⚠️ 别拿 `Error Message` 那颗 TMP 自己作者填的 `(0.84,0,0.127,1)` 当判据 ——
        /// `SetError` 每次都会用 `errorColor` **覆盖**它（`PromptPopup__SetError.c` 头四句读 `this+0xa8..0xb4` 再
        /// `set_color`；那四格就是 `errorColor` 的四个分量 —— 与 `AlliancePanelWindow` 里 `errorColor` 同族的布局）。</summary>
        public static readonly Color ErrColor = new Color(1f, 0f, 0f, 1f);

        /// <summary>当前错误串（空 = 没有错误行）。</summary>
        string _error;
        /// <summary>自检读口：现在有没有错误行。</summary>
        public bool HasError { get { return !string.IsNullOrEmpty(_error); } }

        /// <summary>= 原版 `PromptPopup.SetError(string error, bool localize)`（**逐句**，判据
        /// `PromptPopup__SetError.c` 全 38 行）：① 把 `errorColor` 写进 `Error Message` 那颗 TMP 的 `color`；
        /// ② `localize` 为真 ⇒ `error` 先过 `LocalizationManager.GetTranslation`；③ `SetText`。
        /// <para>我们这扇窗的版面是**建时算好**的（没有 VLG）⇒ 错误行出现/消失会改面板高，
        /// 所以这里改完状态**重建一次**（`Build()` 本来就是「先清空再建」，幂等）。</para>
        /// <para>⚠️ 原版错误行在 prefab 里 `act=T`（出厂就亮、内容是样例串 `INVALID PASSWORD ERROR`）；
        /// 我们**没有错误时整行不建** —— 那是同一件事的另一种表达（原版由脚本在 `Open` 前把内容清空/关掉，
        /// 静态查不到是哪一种），⛔ 别把样例串画出来。</para></summary>
        public void SetError(string error, bool localize)
        {
            string s = error;
            if (localize && !string.IsNullOrEmpty(s)) s = Loc.T(s);
            _error = string.IsNullOrEmpty(s) ? null : s;
            Build();                      // 面板高变了 ⇒ 重建（幂等：Build 首句清空所有子件）
        }

        /// <summary>面板最后算出来的高（自检用）。</summary>
        public float PanelH { get; private set; }
        public bool TwoButtons { get; private set; }

        /// <summary>压暗层那颗**吸收**命中区（`MenuDraw.Absorb` 建的；自检拿 `MenuDraw.WasAbsorb` 核它）。
        /// ⚠️ 它是**吸收**层、不是「点背景关窗」的 `ShadeHit` —— 判据见 `Build()` 第 1 步那段注释。</summary>
        public Transform ShadeHitNode { get { return transform.Find("AbsorbHit"); } }

        public static PromptPopup Create(WindowsManager mgr, string text, string okText, System.Action onOk,
                                         string cancelText, System.Action onCancel, PromptContext.InputData input = null, System.Action<string> onSubmitInput = null)
        {
            var go = new GameObject("GenericPromptWindow");
            var win = go.AddComponent<PromptPopup>();
            win.type = WindowType.Popup;                  // 实证 type=1
            win.placement = WindowsPlacement.Popup;       // 实证 windowsPlacement=15
            win.closeOnEsc = false;                       // 实证 closeOnESC=0
            win.extraScaleSmallScreen = 1f;               // 实证 1.0
            win._text = text;
            win._okText = okText;
            win._cancelText = cancelText;
            win._onOk = onOk; win._onCancel = onCancel; win._input = input; win._onSubmitInput = onSubmitInput;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); BuildInputRowAfterBuild(); }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);

            float cx = 960f, cy = 540f;                       // 屏幕中心（画布像素）

            // 1) 压暗整屏：`Menu Dark Background` —— **无 sprite**，UGUI 会回落到 `Graphic.OnPopulateMesh`
            //    画一块纯色矩形（同一个套路见 `RewardsWindow` 的背景与卡片 header）。
            Solid(root, cx, cy, ShadeW, ShadeH, ShadeColor, QShade, "Menu Dark Background");

            // 🔴 **2026-10-19（`N117` 顺手）**：压暗层的**吃射线命中区** —— 原来**零命中区**
            //   ⇒ 指针穿过压暗层落到**下面那扇窗**的钮上（我们的命中候选只收 `WindowButton`，
            //   见 `MenuDraw.Absorb` 的注释），而原版那块 `Image` 是 `m_RaycastTarget = 1` ⇒ 这一下被吃掉。
            //   🔴 **判据不是「按 `window={0,0}` 推」**（那条已知不可靠，见 `资料/已知的坑.md` 第 15 条）——
            //   这里把**另一半**也核了：
            //     · prefab `GenericPromptWindow/Menu Dark Background` 上确实挂着 `BackgroundCloseButton`
            //       （`MonoBehaviour_627461394584887679.json`）—— 但它的 `window = {0,0}`、`onClick.m_Calls = []`；
            //     · **`PromptPopup` 类里没有任何 `backgroundCloseButton` 字段**（`dump.cs:87347-87368`），
            //       类的方法只有 `Open / InstantiateInput / InstantiateDropdown / SetError / SetMessage /
            //       OnSubmit / OnCancel`（**没有 `Start` / `OnEnable`**）⇒ **没有任何一处在运行时给它接线**；
            //       `PromptPopup__Open.c` 逐句实读：只给 `okButton`(+0x78) / `cancelButton`(+0x80) 挂监听。
            //   ⇒ **原版点这一下什么都不发生**（不会关窗）。同族先例 = `Shell/PopUpGameWindow.cs`
            //     （`MessagePopupWindow` 的两颗 `BackgroundCloseButton` 同为空 ⇒ 用 `Absorb`，⛔ **不是** `ShadeHit`）。
            //   ⚠️ 档由公共件算（`qContentMin − 1` = 3142，严格夹在压暗档 3140 与内容命中区 3143 之间）。
            MenuDraw.Absorb(root, "AbsorbHit",
                            new PxRect(cx - ShadeW * 0.5f, cy - ShadeH * 0.5f,
                                       cx + ShadeW * 0.5f, cy + ShadeH * 0.5f),
                            QShade, QContent);

            // 2) `MessageText`：先按 **900** 宽限制换行画出来（**量出它的真实高度**），再定面板高。
            TwoButtons = !string.IsNullOrEmpty(_cancelText);
            // 🔴 **2026-10-18（W3）就地订正（铁律 5）**：原版 `MessageText` = **900×100**（`Window` 的 VLG
            //   `pad = 0,0,0,0`、`ctrlW = 1` ⇒ 正文列**吃满 900**）· fs50 · **auto[4~50] base36** · 折行1。
            //   我们原来按 `PanelW − 40`（= **860**，自选的左右留白）画、**没接自适应** ⇒ 长文更早折行、面板更高。
            //   判据 → `资料/普查产出_1018/P2_弹窗排版_通用活动奖励.md` §1（P2-6）。
            float msgW = PanelW;                              // = 原版 `MessageText` 的 900 宽（VLG padding = 0）
            var msgLb = Label.Create(root, _text ?? "", new Vector3(0f, 0f, 0f), 6, Color.white,
                                     new Vector2(0.5f, 0.5f), "MessageText");
            float msgH = MsgMinH;
            if (msgLb != null)
            {
                // 🔴 **2026-10-17（A1024③ 第 3/4 批）：原版 `GenericPromptWindow/Window/MessageText` = **Pragati**。
                //    必须在 `SetGlyphHeight` **之前**（字号按 `ActualFace` 的 em 换算）。
                // 🔴 **2026-10-18（W3）**：`SetRenderQueue` 排在 `SetFace` **之后** —— 与 `MenuDraw.TextCore`
                //    的次序约定一致（换字体材质会重放队列；`Label` 自 2026-10-17 R3 起会把队列记下来重放，
                //    这里按约定排、不依赖那条兜底）。
                msgLb.SetFace(TmpFont.Face.LatinCardBody);
                msgLb.SetRenderQueue(QText);
                msgLb.SetGlyphHeight(LayoutSpace.Px(MsgFontPx));
                // ✅ **判据（2026-10-07 · A77⑩ 子表 E3）**：原版 `GenericPromptWindow > Window > MessageText`
                //   是 **`折行=1 auto[4~50]`** —— 这一处本来就对（⛔ 别顺手给它补 `SetWrapping(false)`）。
                // 🆕 **2026-10-18（W3）**：`SetAutoFitBox` = 原版 `auto[4~50]`（顺带设折行宽，框高 = 原版 100）。
                msgLb.SetAutoFitBox(LayoutSpace.Px(msgW), LayoutSpace.Px(MsgMinH),
                                    MsgAutoMinPx, MsgFontPx, MsgAutoBasePx);
                msgLb.RefreshBounds();
                msgH = Mathf.Max(MsgMinH, msgLb.WorldH * 108f);
            }

            // 🆕 2026-10-27（E9b · 账 545）：有错误串时 VLG 里多一行 `Error Message`（900×90）
            //   ⇒ 面板高 +90（原版那个 `ContentSizeFitter(V=PreferredSize)` 就是这么涨的）。
            float errH = HasError ? ErrRowH : 0f;
            PanelH = msgH + (_input != null ? InRowH : 0f) + errH + BtnRowH;
            float px1 = cx - PanelW * 0.5f, px2 = cx + PanelW * 0.5f;
            float py1 = _py1 = cy - PanelH * 0.5f, py2 = cy + PanelH * 0.5f;

            // 3) 背景：`Generic Popup Background`（九宫格 Sliced，外扩 50）
            var bgGo = Node(root, "Generic Popup Background", cx, cy, PanelW + BgPad * 2f, PanelH + BgPad * 2f);
            var bgTex = CardArt.MenuUi(ArtPopup);
            if (bgTex != null)
            {
                // 🔴 2026-10-05 修（真 bug：**第 7/8 实参写反**）：`CreateNineSlice` 第 7/8 形参是
                //   `worldW` / `worldH`（`Battle/ImageQuad.cs` 的 `CreateNineSlice` 第 7/8 形参），这里原来把 `Px(PanelH+…)` 喂给了
                //   `worldW`、`Px(PanelW+…)` 喂给了 `worldH` ⇒ 底板按「**面板高+100 宽 × 1000 高**」建
                //   （最小高时 = **310 × 1000**，应为 **1000 × 310**）。
                //   判据 = 全工程其余 `CreateNineSlice` 直调点（`grep -rn "ImageQuad.CreateNineSlice("` 实测；
                //   ⚠️ 条数随并发写手会漂、别当定值）**一律「宽在前、高在后」**
                //   （共用件 `Shell/MenuDraw.Nine` 里那一句是最典型的一处）；`Editor/RewardsScene.cs` 的探针更是
                //   **同一张 `40k_popup`**（同 border 169/160）塞进 `1000×90` 建过 —— 本条是孤例。
                //
                // 🔴 **2026-10-06（A50③）：这一处收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
                //   与旧代码**逐项等价**（三样都别改，判据同 `Battle/WfSlider.cs` 里那条收口到 `MenuDraw.Nine` 的注 那条第一处收口）：
                //    ① **矩形** = 面板四周各外扩 `BgPad`(50)：`px1−50 → px2+50` / `py1−50 → py2+50`，
                //       就是上面 `bgGo` 那个 `PanelW+100 × PanelH+100`（`MenuDraw.Nine` 内部按 `LayoutSpace.Px`
                //       折世界尺寸，与旧代码那两个 `LayoutSpace.Px(…)` 是同一个换算）；
                //    ② **落位** = `Local(bgGo, …)` 的局部位移 —— `bgGo` 的中心**正是**这个矩形的中心
                //       ⇒ 位移恒 `Vector3.zero`（与旧代码显式传的那个零向量同值）；⚠️ 别把 `bgGo` 挪走；
                //    ③ **队列 = `QPanel`**（旧代码建完逐个子块设的就是这一档，`MenuDraw.Nine` 会替我们设）；
                //       tint 仍不传（旧代码也没传）。
                // ⚠️ **别再给子块 `SetAspect`**：`CreateNineSlice` 建每一块时已按
                //   **真九宫格**把该块自己的长宽比算好（`Battle/ImageQuad.cs` 的 `CreateNineSlice` 里那句 `q.SetAspect(w / h)`）。
                //   原来这里还把**面板的**长宽比套给每一块（`SetAspect((PanelW+100)/(PanelH+100))`）⇒
                //   九块各自被拉成面板的形状、互相重叠/留缝，**并集永远不等于面板矩形**
                //   （算式：`PanelH`=300 时并集 = **1231 宽** × 400 高，应为 **1000** × 400）—— 2026-10-05 已删。
                //   全工程所有 `CreateNineSlice` 调用点（含 `MenuDraw.Nine`）对子块**只设 tint / 队列**：
                //   `grep -A 10 "ImageQuad.CreateNineSlice(" | grep SetAspect` 实测**一处都没有**
                //   （唯一命中的是本条注释自己）；`Editor/ShellScene.cs` 的 ⑤b 段有「九块并集 = 面板矩形」的断言盯住它。
                var bgRect = new PxRect(px1 - BgPad, py1 - BgPad, px2 + BgPad, py2 + BgPad);
                MenuDraw.Nine(bgGo, bgTex, bgRect, new Vector4(169f, 160f, 169f, 160f), PopupTexW, PopupTexH,
                              QPanel, null, true, "Nine");
            }
            else Debug.LogWarning($"[Prompt] 取不到 `{ArtPopup}`（面板底板没画）—— 导入器：`工具/import_original_art.py`");

            // 4) `Mask` → `Background fill`（Tiled，64 一格）。⚠️ 我们的引擎没有 Mask ⇒ 直接按 Mask 的矩形铺，
            //    不裁子件（这里子件只有它自己，等价）。
            // 🔴 **2026-10-04（A25⑤）**：原来在这里直调 `ImageQuad.CreateTiled` —— 那是**绕开公共件**的
            //    第四条路（`MenuDraw.Tiled` 才带 `clip` / `clipSoftness`），已收口。
            //    ⚠️ 摆位/尺寸/队列逐项等价：`MenuDraw.Tiled` 把根摆在 `Local(parent, r)`，
            //    而这里的 `fillGo` 本来就是按 `fillRect` 建的空节点 ⇒ 局部位移恒为 0（与 `Vector3.zero` 同）。
            var fillRect = new PxRect(px1 - BgPad + MaskInsetX * 0.5f, py1 - BgPad + MaskInsetY * 0.5f,
                                      px2 + BgPad - MaskInsetX * 0.5f, py2 + BgPad - MaskInsetY * 0.5f);
            var fillGo = Node(root, "Background fill", fillRect);
            var fillTex = CardArt.MenuUi(ArtPopupFill);
            if (fillTex != null)
            {
                MenuDraw.Tiled(fillGo, fillTex, fillRect, FillTilePx, QFill, "Tiles");
            }
            else Debug.LogWarning($"[Prompt] 取不到 `{ArtPopupFill}`（面板填充没铺）—— 导入器：`工具/import_original_art.py`");

            // 5) 文案：摆在**上半区**（`MessageText` 是 VLG 的第一个内容件，占 msgH 高；下面才是 110 的按钮行）
            if (msgLb != null)
            {
                float mTop = py1, mBot = py1 + msgH;
                msgLb.transform.localPosition = Local(root, cx - msgW * 0.5f, mTop, cx + msgW * 0.5f, mBot);
            }

            // 5·b) 🆕 2026-10-27（E9b · 账 545）`Error Message` 行：紧跟正文之后、占 90 高（原版 VLG 的第 3 个子件；
            //   第 2 个是 `inputs`，我们**不建**输入层 —— 见文件头 ③ 与账 541）。
            //   四格照 prefab 实读：宽吃满 900 · 字号 40/**无自适应** · `Center/Middle` · 折行 1 · 色 = `errorColor`。
            if (HasError)
            {
                var errR = new PxRect(cx - PanelW * 0.5f, py1 + msgH + (_input != null ? InRowH : 0f), cx + PanelW * 0.5f, py1 + msgH + (_input != null ? InRowH : 0f) + ErrRowH);
                var errLb = Label.Create(root, _error, Vector3.zero, 6, ErrColor,
                                         new Vector2(0.5f, 0.5f), "Error Message");
                if (errLb != null)
                {
                    errLb.SetFace(TmpFont.Face.LatinCardBody);      // 同 `MessageText`（Pragati）
                    errLb.SetRenderQueue(QText);
                    errLb.SetGlyphHeight(LayoutSpace.Px(ErrFontPx));
                    // 原版 `m_enableAutoSizing = 0` ⇒ ⛔ 不调 `SetAutoFitBox`；折行 1 ⇒ 只给框宽。
                    errLb.SetWrapWidth(LayoutSpace.Px(PanelW));
                    errLb.transform.localPosition = Local(root, errR.x1, errR.y1, errR.x2, errR.y2);
                }
            }

            // 6) 按钮：`Buttons` 行 110 高、**LowerCenter**（align=7）⇒ 按钮贴行底；HLG spacing 40、**等分**
            float rowBot = py2;
            float btnY2 = rowBot, btnY1 = rowBot - BtnH;      // LowerCenter ⇒ 底对齐
            if (TwoButtons)
            {
                float halfW = (PanelW - BtnGap) * 0.5f;
                MakeButton(root, px1, px1 + halfW, btnY1, btnY2, _cancelText, false);
                MakeButton(root, px1 + halfW + BtnGap, px2, btnY1, btnY2, _okText, true);
            }
            else
            {
                // ⚠️ **我们挑的**（原版只有 2 按钮版）：只给 Ok 时把它摆中间，宽度按 AspectRatioFitter 的比例 5.1406
                //    （`Button Text` 上的 ARF：`mode=1` WidthControlsHeight、`ratio=5.1405730`）
                float w = Mathf.Min(PanelW, BtnH * 5.1405730f);
                MakeButton(root, cx - w * 0.5f, cx + w * 0.5f, btnY1, btnY2, _okText, true);
                Debug.Log("[Prompt] 只给了 Ok ⇒ **藏掉 Cancel、Ok 居中**（⚠️ 我们挑的：原版 prefab 只有 2 按钮版，" +
                          "运行时是否隐藏 Cancel 静态查不到，见 `资料/日常_原版规格.md` §七）");
            }
        }

        void MakeButton(Transform root, float x1, float x2, float y1, float y2, string text, bool isOk)
        {
            var go = Node(root, isOk ? "OkButton" : "CancelButton", new PxRect(x1, y1, x2, y2));
            // `40K_button` · `Simple + PreserveAspect` ⇒ 等比放进框（框 430×80 vs 源图 489×107 ⇒ 宽受限）
            var tex = CardArt.MenuUi(ArtButton);
            if (tex != null)
            {
                float w = x2 - x1, h = y2 - y1, aspect = BtnTexW / BtnTexH;
                if (w / h > aspect) w = h * aspect; else h = w / aspect;
                float mx = (x1 + x2) * 0.5f, my = (y1 + y2) * 0.5f;
                var q = ImageQuad.Create(go, tex, Local(root, mx - w * 0.5f, my - h * 0.5f, mx + w * 0.5f, my + h * 0.5f),
                                         LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), "Image");
                if (q != null)
                {
                    q.SetAspect(aspect);
                    q.SetTint(BtnColor);
                    q.SetRenderQueue(QContent);
                    var hit = q.gameObject.AddComponent<WindowButton>();
                    hit.onClick = () => Choose(isOk);
                    // 🆕 2026-10-03 A17：原版 `GenericPromptWindow>Buttons>{Cancel,Ok}Button` 是 SpriteSwap
                    //（实测 `trans=2` · HL = `40K_button_hover` · P = `40K_button_pressed`；
                    //  普查那 5 块表**漏了这扇窗**，是接线时按 `HasHit` 反查出来的）
                    hit.BindSelf(ArtButton);
                }
            }
            else Debug.LogWarning($"[Prompt] 取不到 `{ArtButton}`（按钮底没画）");

            // `Button Text` `sz=(−26,0)` fs=50 居中白
            var lb = Label.Create(go, text ?? "", Vector3.zero, 6, Color.white, new Vector2(0.5f, 0.5f), "Button Text");
            if (lb != null)
            {
                // 🆕 2026-10-17（A1024③ 第 3/4 批）：原版 `GenericPromptWindow/Window/Buttons/…/Button Text` = **Pragati**。
                //    必须在 `SetGlyphHeight` **之前**（字号按 `ActualFace` 的 em 换算）。
                // 🔴 **2026-10-18（W3）**：`SetRenderQueue` 排在 `SetFace` **之后**（同 `MessageText` 那一处；
                //    与 `MenuDraw.TextCore` 的次序约定一致 —— 换字体材质会把队列重放，这里不依赖那条兜底）。
                lb.SetFace(TmpFont.Face.LatinCardBody);
                lb.SetRenderQueue(QText);
                lb.SetGlyphHeight(LayoutSpace.Px(BtnFontPx));
                lb.SetWrapWidth(LayoutSpace.Px((x2 - x1) - 26f));
                // 🔴 **2026-10-07（A77⑩ · 子表 E4）**：原版 `GenericPromptWindow > Window > Buttons > … > Button Text`
                //   （出厂 `Cancel` / `CONFIRM`）是 **`折行=0 auto[12~50]`**（判据 = `python 工具/menu_dump.py
                //   bundle_menus_assets_all "GenericPromptWindow" --depth 12 --md`：两个 `Button Text 折行=0`）
                //   ⇒ 原版靠 `auto 12~50` **缩字号**、不折行；我们无条件开了折行（`SetWrapWidth`）⇒ 照原版关掉。
                //   ⚠️ 留着 `SetWrapWidth` 只为那一份框宽；模式按原版走。
                lb.SetWrapping(false);
                lb.transform.localPosition = Local(root, x1, y1, x2, y2);
            }
        }

        void Choose(bool ok)
        {
            var a = ok ? _onOk : _onCancel;
            // 🆕 2026-10-28（B89-R3b · 账 541/543）原版 `OnSubmit()` 先 `SetError("")` 清错、再回调、再关窗；
            //   `OnCancel()` 直接回调 + 关窗（判据 = `decomp_full/PromptPopup__OnCancel.c` 全 16 行；
            //   `PromptPopup__OnSubmit.c` 的委托链是**跳转表**、反编译读不全 ⇒ 这里只做「等价那半」：
            //   取值 → 关窗 → 回调，顺序与 `Choose` 既有约定一致，⛔ 不冒充原版逐句）。
            string submitted = _inputField != null ? _inputField.Value : null;
            if (ok && _inputField != null) _inputField.EndEdit(true);
            Close();
            if (ok && _input != null)
            {
                _submitted = submitted;
                var cb = _onSubmitInput;
                if (cb != null) cb(submitted);       // 先关再回调（同下面那条既有约定）
            }
            a?.Invoke();     // ⚠️ 先关再回调（照原版 `HidePopUp` 在 `OnPress` 之后）—— 回调里可能再开窗
        }

        // ---------------------------------------------------------- 画图小工具（与 MissionsTab 同一套换算）

        /// <summary>🔴 **2026-10-11（A306①）**：父的位置先换算进**设计空间**再减
        /// （`MenuDraw.PosInDesignSpace` —— 原来写的是 `− parent.position`，**少除了一次父级 `lossyScale`**，
        /// 与 A294 / A297 修掉的 `MenuDraw.Local` / `MainMenuSubmenuWindow.Local` 是**同一个病**，
        /// 本处是那份算式的同形副本）。除的是**被摆的那个节点上面那一级**的缩放，与 `MenuDraw.Local`
        /// **逐字同一份口径** ⇒ 现在两者在 `k == 1` 与 `k ≠ 1` 两种情况下**都**同值。
        /// · `parent == null` 两支同值（`PosInDesignSpace(null)` 首句就返回 `Vector3.zero`）。
        /// 📌 **小屏缩放开关出厂关着 ⇒ `k == 1` ⇒ 与改前【逐位相同】** ⇒ 今天零可观测差异；
        /// 🔴 **改坏法**：换回裸 `parent.position` ⇒ **今天一条现有断言都不会红**（不是「有断言挡着」，
        /// 是这条**还没有断言**）⇒ 要补的两态断言（宿主 + 断言什么）写在
        /// `资料/普查产出_1011/W4_子3.md` §四，由调度台安排。</summary>
        static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - MenuDraw.PosInDesignSpace(parent);

        static Transform Node(Transform root, string name, PxRect r)
            => Node(root, name, r.CX, r.CY, r.W, r.H);

        static Transform Node(Transform root, string name, float cx, float cy, float w, float h)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = Local(root, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
            return t;
        }

        static ImageQuad Solid(Transform root, float cx, float cy, float w, float h, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(root, CardArt.Solid(),
                                        Local(root, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / h);
            quad.SetTint(color);
            quad.SetRenderQueue(q);
            return quad;
        }

        // ============================================================ 🆕 2026-10-28（B89-R3b · 账 541）`inputs` 行
        // 🔴 成员**一律追加到类尾**（铁律 18·b）：`PromptPopup.cs` 在账里有 `文件:行号` 落点
        //    （`菜单_外壳.tsv` 542/543 指 `Choose`、546 指 `Build`）⇒ 往中间插行会让它们漂。
        /// <summary>`inputs/template` 的行高 = **90**。判据 = prefab 直读
        /// （`d:/2/tools/assetripper_out/ExportedProject/Assets/GenericPromptWindow.prefab`：
        /// `inputs/template` 的 `m_SizeDelta = (0, 90)`；`Window` 的 VLG `m_Spacing = 0` ⇒ 行间**没有**额外间距）。
        /// 占位字/正文的字号与内缩 → `Shell/TextInputField.cs` 文件头（那里逐条列了出处）。</summary>
        public const float InRowH = TextInputField.RowH;

        PromptContext.InputData _input;
        System.Action<string> _onSubmitInput;
        TextInputField _inputField;
        string _submitted;
        /// <summary>`Build()` 算出来的面板顶（设计 px）；只有 `BuildInputRowAfterBuild()` 用。</summary>
        float _py1;

        /// <summary>= 原版 `PromptPopup.InstantiateInput(PromptContext.InputData)`（账 541）。
        /// <para>原版：`Instantiate(inputPrefab, inputHolder)` → `contentType` · `text = DefaultValue` → 占位字过词条。
        /// 我们：建 `<see cref="TextInputField"/>` 一格（版面/键盘/掩码见那个文件），并**重建一次**（面板高会变）。</para>
        /// <para>幂等：`data == null` ⇒ 清掉输入行并重建。</para></summary>
        public void InstantiateInput(PromptContext.InputData data)
        {
            _input = data;
            _inputField = null;                 // 旧那颗随 `Build()` 首句一起销毁
            Build();
            BuildInputRowAfterBuild();
        }

        /// <summary>把 `inputs` 那一行建出来。⚠️ `Build()` 里**不插行**（只改表达式），这一跳由 `Open()` 调 ⇒ 行号不漂。</summary>
        void BuildInputRowAfterBuild()
        {
            if (_input == null) return;
            // 行顶 = 面板顶 + 正文高；正文高由 `PanelH` 的算式反推（`Build()` 里那三格逐字同源）
            float msgH = PanelH - InRowH - (HasError ? ErrRowH : 0f) - BtnRowH;
            float rowTop = _py1 + msgH;
            var r = new PxRect(960f - PanelW * 0.5f, rowTop, 960f + PanelW * 0.5f, rowTop + InRowH);
            _inputField = TextInputField.Create(transform, r, _input, 0, QFill, SubmitFromInput);
            if (_inputField == null)
                Debug.LogWarning("[Prompt] 输入行**没建出来**（`TextInputField.Create` 返回 null）—— 如实说，不静默");
        }

        void SubmitFromInput(string value)
        {
            _submitted = value;
            var cb = _onSubmitInput;
            if (cb != null) cb(value);
        }

        /// <summary>自检读口：现在有没有输入行（原版 `Window/inputs` 在不在）。</summary>
        public bool HasInputRow { get { return _inputField != null; } }
        /// <summary>自检读口：输入行那一格（没有 ⇒ `null`）。</summary>
        public TextInputField InputField { get { return _inputField; } }
        /// <summary>自检读口：最近一次提交出去的值（没提交过 ⇒ `null`）。</summary>
        public string SubmittedValue { get { return _submitted; } }
    }

    // ✅ **`WindowButton` 已拆到独立文件 `Shell/WindowButton.cs`**（2026-10-11 · `A1506`）。
    //
    // 🔴 **本处原来有一段「留在原处」的定案（`A65②`，2026-10-05）—— 它的前提已被推翻，⛔ 别再照它做**
    //    （铁律 5：就地改掉、保留更正痕迹）。它当时的理由是：
    //      「全工程 **40 个文件**引用它，而 **0 处**是**序列化引用**」——
    //    那条只在**「引用只能通过 guid」**这个前提下成立，**它漏掉了「内联 `!u!115` 存根」这一种引用方式**。
    //
    // **实测反例（判据）**：`CardPresentation/Scenes/DeckEditor.unity` 里有 **12 个 `WindowButton`
    //   确实是序列化进场景的** —— 它们在第十六会话那份手工修复**之前**是内联存根
    //   （`--- !u!115 &274566038`，`m_ClassName: WindowButton`），修复时被改成指**本文件**的 guid
    //   （`4ce92109334f68348ad62b57f14b03d2`），于是**解析成了本文件的【主类】`PromptPopup`** —— 那是错的。
    //
    // ⇒ **真问题不是「观感上的整洁」**：`WindowButton` 没有自己的文件 ⇒ **Unity 建不出 `MonoScript` 资产**
    //   ⇒ 存场景时 `m_Script` 指不到资产 ⇒ Unity **当场烘一块内联存根**写进 level
    //   ⇒ player 加载 `level2` 时 `Position out of bounds!` → `The file '.../level2' is corrupted!` → 崩溃
    //   （`A1481` / `A1506`）。同一族的还有 `Shade` / `BlockingOverlay` / `WindowHolder` /
    //   `CardPreview` / `CardDraggingController` / `CosmeticPreview` / `CosmeticDraggingController` —— 都已拆出。
    //   📌 判据全文 → `资料/交接_第十七会话.md` §三 · `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
}
