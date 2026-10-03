// PromptPopup.cs — 通用提示窗（「暂无服务器」等的宿主）
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
    /// <summary>`PromptPopup : GameWindow` —— 原版唯一的通用提示窗。也是边界③「点了如实提示」的唯一宿主。</summary>
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

        /// <summary>面板最后算出来的高（自检用）。</summary>
        public float PanelH { get; private set; }
        public bool TwoButtons { get; private set; }

        public static PromptPopup Create(WindowsManager mgr, string text, string okText, System.Action onOk,
                                         string cancelText, System.Action onCancel)
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
            win._onOk = onOk; win._onCancel = onCancel;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);

            float cx = 960f, cy = 540f;                       // 屏幕中心（画布像素）

            // 1) 压暗整屏：`Menu Dark Background` —— **无 sprite**，UGUI 会回落到 `Graphic.OnPopulateMesh`
            //    画一块纯色矩形（同一个套路见 `RewardsWindow` 的背景与卡片 header）。
            Solid(root, cx, cy, ShadeW, ShadeH, ShadeColor, QShade, "Menu Dark Background");

            // 2) `MessageText`：先按 900 宽限制换行画出来（**量出它的真实高度**），再定面板高。
            TwoButtons = !string.IsNullOrEmpty(_cancelText);
            float msgW = PanelW - 40f;                        // ⚠️ 我们挑的左右留白（原版由 VLG 的 padding 决定，是 0）
            var msgLb = Label.Create(root, _text ?? "", new Vector3(0f, 0f, 0f), 6, Color.white,
                                     new Vector2(0.5f, 0.5f), "MessageText");
            float msgH = MsgMinH;
            if (msgLb != null)
            {
                msgLb.SetRenderQueue(QText);
                msgLb.SetGlyphHeight(LayoutSpace.Px(MsgFontPx));
                msgLb.SetWrapWidth(LayoutSpace.Px(msgW));
                msgLb.RefreshBounds();
                msgH = Mathf.Max(MsgMinH, msgLb.WorldH * 108f);
            }

            PanelH = msgH + BtnRowH;
            float px1 = cx - PanelW * 0.5f, px2 = cx + PanelW * 0.5f;
            float py1 = cy - PanelH * 0.5f, py2 = cy + PanelH * 0.5f;

            // 3) 背景：`Generic Popup Background`（九宫格 Sliced，外扩 50）
            var bgGo = Node(root, "Generic Popup Background", cx, cy, PanelW + BgPad * 2f, PanelH + BgPad * 2f);
            var bgTex = CardArt.MenuUi(ArtPopup);
            if (bgTex != null)
            {
                var g = ImageQuad.CreateNineSlice(bgGo, bgTex, new Vector4(169f, 160f, 169f, 160f), PopupTexW, PopupTexH,
                                                  Vector3.zero, LayoutSpace.Px(PanelH + BgPad * 2f),
                                                  LayoutSpace.Px(PanelW + BgPad * 2f), "Nine",
                                                  new Vector4(169f, 160f, 169f, 160f), true);
                if (g != null)
                    foreach (var q in g.GetComponentsInChildren<ImageQuad>())
                    { q.SetAspect((PanelW + BgPad * 2f) / (PanelH + BgPad * 2f)); q.SetRenderQueue(QPanel); }
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
                lb.SetRenderQueue(QText);
                lb.SetGlyphHeight(LayoutSpace.Px(BtnFontPx));
                lb.SetWrapWidth(LayoutSpace.Px((x2 - x1) - 26f));
                lb.transform.localPosition = Local(root, x1, y1, x2, y2);
            }
        }

        void Choose(bool ok)
        {
            var a = ok ? _onOk : _onCancel;
            Close();
            a?.Invoke();     // ⚠️ 先关再回调（照原版 `HidePopUp` 在 `OnPress` 之后）—— 回调里可能再开窗
        }

        // ---------------------------------------------------------- 画图小工具（与 MissionsTab 同一套换算）

        static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - (parent != null ? parent.position : Vector3.zero);

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
    }

    /// <summary>弹窗按钮的点击接收（原版 `GameWindowButton` 的最小等价物）。</summary>
    public class WindowButton : MonoBehaviour
    {
        public System.Action onClick;

        // ============================================================ 🆕 2026-10-03 悬停 / 按下（原版 UGUI 的按钮态）
        //
        // 🔴 **判据 = UGUI `Selectable.DoStateTransition` + 原版 prefab 里的实测值**（不是我们挑的）：
        //   · 原版 `bundle_menus_assets_all` 里 **1276 个按钮组件**逐个数过：`m_Transition` = 2(SpriteSwap) **630** ·
        //     1(ColorTint) **505** · 0(None) **141**。
        //     ⚠️ **2026-10-04 更正（铁律 5）**：下面这句原来写的是「`m_Colors` 全部是 UGUI 默认那组」——
        //     **不成立**：实测那 **1276 颗里 150 颗不是**（同一份数据独立复算过两遍）。
        //     分布：`EverguildToggle` 一族 **51** 颗 HL = 绿 `(0.292,0.953,0.682)` · **36** 颗浅蓝
        //     `(0.882,0.976,1)` · **19** 颗青 `(0.609,0.943,1)` · **13** 颗 `(0.6887,…)`（卡组行的两颗就在其中）· 其余零散。
        //     按 prefab 根分组最大的几族：`Give Feedback Popup menu` **41** · `Player Profile Window` **16** ·
        //     `Main Menu Settings Window` **14** · `Social Submenu Variant` 6 · `Collection Menu Variant` 5 …
        //     ⇒ **默认值只是「没被改过的那一批」**：`HighlightK` / `PressedK` 这两个常量**不能当全库判据**，
        //     要拿它比对必须**先逐颗读 `m_Colors`**（这就是本类只做「统一色偏兜底」时留下的那笔账）。
        //     卡组编辑窗里占了 2 颗：`Deck Selector Defensive Card Slot` 与 `Deck Selector Card Info button`
        //     的 HL = `(0.6887,0.6887,0.6887,1)` —— 那两颗的悬停**没走本类**，走 `DeckRuntime.RowHoverK`。
        //     ⚠️ 出处分界：**这一句不在** `按钮悬停图_普查.md` 里（那份没提 `m_Colors`）——
        //     数据出处 = `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md` §四②。
        //     默认那一组本身是：`m_HighlightedColor = 0.9607843` · `m_PressedColor = 0.7843137` ·
        //     `m_FadeDuration = 0.1` · `m_ColorMultiplier = 1.0`。
        //   · UGUI 的 `ColorTint` = `targetGraphic.CrossFadeColor(tintColor, fadeDuration)` —— 它是
        //     **乘**在顶点色上（`canvasRenderer.SetColor`），所以「悬停 = 原色 × 0.9608」。
        //   · `SpriteSwap` 那 630 个原版**换的是同一件 graphic 的图**（`m_SpriteState.m_HighlightedSprite`，
        //     **630/630 全非空**）⇒ 那一档要换图才叫对。**本版只做了统一的色偏兜底**，
        //     逐个换图的活**已记账**（判据 = 每个按钮的 `m_SpriteState`）——
        //     ⚠️ **这是一处如实标注的偏离**（见 `项目任务.md` §三 第 29 条 A15）。
        //   · `m_Transition = 0` 的 141 个原版**悬停什么都不变** ⇒ 那些地方我们这一层也不该变色，
        //     但**本地判不出「我们的哪一颗对应原版哪一颗」** ⇒ 留作上面那条账的一部分。
        //
        // ⚠️ **批处理下没有帧循环**（`Update` 不跑）⇒ 自检要看色偏就直调 `Enter/Exit/SetInstant`：
        //    `PointerLayer.HoverAt(...)` 在非 Play 时**直接落到目标色**（照 `ImageQuad` 那一族的规矩）。

        /// <summary>指针进入 / 离开时各调一次（原版就是 UGUI 的 `IPointerEnterHandler` / `IPointerExitHandler`）。</summary>
        public System.Action onEnter, onExit;

        /// <summary>原版 `m_Colors.m_HighlightedColor`（**全库同一个值**）。</summary>
        public const float HighlightK = 0.9607843f;
        /// <summary>原版 `m_Colors.m_PressedColor`。</summary>
        public const float PressedK = 0.7843137f;
        /// <summary>原版 `m_Colors.m_FadeDuration`（秒）。</summary>
        public const float FadeSeconds = 0.1f;

        /// <summary>关掉这一颗的色偏（原版 `m_Transition = 0` 的那 141 颗用得上）。</summary>
        public bool tintOnHover = true;

        // ============================================================ 🆕 2026-10-03 A17：悬停 / 按下【换图】
        //
        // 原版一颗 `Selectable` 只有**一种** transition（UGUI `DoStateTransition`）：
        //   `ColorTint` 的 505 颗 = 变暗（上面那套色偏就是它）· `SpriteSwap` 的 **630 颗 = 换图**。
        // ⇒ **换图那一档不该再叠色偏**（`Bind` 会自动把 `tintOnHover` 关掉，不然两种行为同时上）。
        // 判据正本 = `资料/普查产出_1003/按钮悬停图_普查.md`（12 条命名规律 + 5 块逐颗表）。

        /// <summary>要换图的那一层 —— 原版 `Selectable.m_TargetGraphic`。
        /// ⚠️ **它常常不是按钮自己那层**：原版 `Generic Close Button Orange` 指子件 `Background`、
        /// `WebShop Button` 指子件 `Button Image` ⇒ 由调用方传**真正画着常态图的那个 `ImageQuad`**。</summary>
        public ImageQuad target;
        Texture _normalTex, _hoverTex, _pressedTex;
        /// <summary>绑定时那张 quad 的宽高比 —— **换图后要拉回来**（见 `SwapTo` 的注释）。</summary>
        float _targetAspect;

        /// <summary>常态图 → 高亮图。表里没有的走 `<常态图>_hover` 后备（普查 §一：绝大多数是这个规律）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> HoverNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "40K_settings_button",            "40K_settings_button_selected" },   // ⚠️ 不是 `_hover`
            { "40K_dropdown_field_closed",      "40K_dropdown_field_opened" },      // ⚠️ 不是 `_hover`
            { "UI_Button_Menu_Back",            "UI_Button_Menu_Back_Hover" },      // ⚠️ 大写 H
            { "UI_Button_Organe_Square_Normal", "UI_Button_Organe_Square_Hover" },  // ⚠️ 大写 H
        };

        /// <summary>常态图 → 按下图（**只有这几张存在**，其余退回高亮图；取不到按下图**不算缺图**）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> PressedNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "40K_button",            "40K_button_pressed" },
            { "40k_general_bt_yellow", "40k_general_bt_yellow_pressed" },
            { "40k_bt_close",          "40k_bt_close_pressed" },
            { "40k_menu_bt",           "40k_menu_bt_pressed" },
            { "UI_Button_Mulligan",    "UI_Button_Mulligan_Pressed" },    // ⚠️ 大写 P
            { "UI_Button_Menu_Back",   "UI_Button_Menu_Back_Pressed" },   // ⚠️ 大写 P
        };

        /// <summary>**取不到的悬停图**（红线：不许静默失败）—— 每扇窗的自检断它为空。</summary>
        public static readonly System.Collections.Generic.List<string> MissingSwapArt =
            new System.Collections.Generic.List<string>();

        static string HoverNameFor(string art)
            => string.IsNullOrEmpty(art) ? null
             : (HoverNames.TryGetValue(art, out var h) ? h : art + "_hover");
        static string PressedNameFor(string art)
            => string.IsNullOrEmpty(art) ? null
             : (PressedNames.TryGetValue(art, out var p) ? p : art + "_pressed");

        /// <summary>把这一颗接到「悬停换图」上。`art` = **常态图的图名**（查表/后备用），
        /// `hoverArt` / `pressedArt` = **逐颗显式覆盖** —— 同一张常态图在不同按钮上配不同高亮图时必须用，
        /// 例：卡组四圆钮常态图都是 `UI_Button_Round_background`，高亮分别是 `back` / `deck_change` / `eye` / `back`。
        /// 🔴 **取不到高亮图就记进 `MissingSwapArt`**（自检会红），**不静默画成没反应**。</summary>
        public void Bind(ImageQuad t, string art, string hoverArt = null, string pressedArt = null)
        {
            target = t;
            if (t == null) return;                 // 没画出来的件没有图可换（调用点负责如实说明）
            _normalTex = t.Texture;
            _targetAspect = t.WorldH > 0f ? t.WorldW / t.WorldH : 0f;
            string hn = hoverArt ?? HoverNameFor(art);
            string pn = pressedArt ?? PressedNameFor(art);
            _hoverTex = CardArt.MenuUi(hn);
            _pressedTex = CardArt.MenuUi(pn);
            if (_hoverTex == null && !MissingSwapArt.Contains(art + " → " + hn))
                MissingSwapArt.Add(art + " → " + hn);
            // 原版一颗只有一种 transition ⇒ 换图那一档不再叠色偏
            tintOnHover = false;
        }

        void SwapTo(Texture tex)
        {
            if (tex == null) return;
            SetOn(target, tex);
            if (_nine != null) for (int i = 0; i < _nine.Length; i++) SetOn(_nine[i], tex);
        }

        void SetOn(ImageQuad q, Texture tex)
        {
            if (q == null) return;
            q.SetTexture(tex);
            // 🔴 **换完图必须把宽高比拉回来** —— `ImageQuad.SetTexture` 会把 `_aspect` 冲成**贴图自己的**比值，
            //    而我们这套 quad 的矩形是**按原版矩形定的**（`MenuDraw.Rect` 里那次 `SetAspect`）。
            //    2026-10-03 实测：不拉回来 ⇒ 那颗 300px 的钮 `WorldW` 变成 **329**（`SettingsScene.CheckRectS` 抓到的；
            //    **屏幕形状没变、只有逻辑宽度被改掉**，肉眼看不出来）。
            //    同族先例（同一句纪律，两处早就写了）：`BattleLogPanel:532` · `AlliancesTab:189`。
            if (_targetAspect > 0f) q.SetAspect(_targetAspect);
        }

        /// <summary>自检用：把一棵树里**所有接了换图的**按钮逐个悬停一遍，返回「没换 / 没还原」的描述
        /// （空串 = 全过），`n` = 查了几颗。⚠️ 批处理**没有帧循环** ⇒ 这里直调 `Enter/Exit`
        /// —— 它们正是 `PointerLayer.HoverAt` 会调的那两个（`onEnter/onExit` 目前**无人挂**，
        /// 所以直调没有副作用；将来谁挂了钩子，这条要改成走 `PointerLayer.HoverAt`）。</summary>
        public static string AuditHoverSwap(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || wb._hoverTex == null || wb.target == null) continue;
                n++;
                var before = wb.target.Texture;
                wb.Enter();
                if (wb.target.Texture != wb._hoverTex)
                    bad.Append("「").Append(wb.name).Append("」悬停**没换图**；");
                wb.Exit();
                if (wb.target.Texture != before)
                    bad.Append("「").Append(wb.name).Append("」离开**没还原**；");
            }
            return bad.ToString();
        }

        ImageQuad[] _nine;   // 九宫格那种「一颗按钮由 9 张小 quad 拼出来」的情形（见 BindNine）

        /// <summary>底色**由别人换掉之后**（例：设置窗页签选中态走 `SetTexture`）要调一次，
        /// 否则悬停退出时会把这一层恢复成**绑定时那一刻**的旧图。</summary>
        public void SetNormalTex(Texture t) { _normalTex = t; }

        /// <summary>九宫格按钮（`Image.Type = Sliced`）的换图 —— 原版换的是**同一个 `Image` 的 sprite**，
        /// 我们这边它被切成了 9 张小 quad ⇒ **每一张都要换**（只换中心那格 = 边框不跟着亮）。
        /// `40K_button` 与 `40K_button_hover` 实测**同尺寸 489×107**（2026-10-03 核对），uv 切分不变、只换纹理。</summary>
        public void BindNine(GameObject nine, string art, string hoverArt = null, string pressedArt = null)
        {
            if (nine == null) return;
            var qs = nine.GetComponentsInChildren<ImageQuad>();
            if (qs == null || qs.Length == 0) return;
            _nine = qs;
            Bind(qs[0], art, hoverArt, pressedArt);   // 常态图取第一张（九张同源）
        }

        /// <summary>便捷：**按钮自己就是那张图**时（`quad.gameObject.AddComponent&lt;WindowButton&gt;()` 那种写法）
        /// 直接绑自己身上那张，不必再传引用。</summary>
        public void BindSelf(string art, string hoverArt = null, string pressedArt = null)
            => Bind(GetComponent<ImageQuad>(), art, hoverArt, pressedArt);

        /// <summary>指针正压在这一颗上吗。</summary>
        public bool Hovered { get; private set; }
        /// <summary>左键正压在这一颗上吗（原版 `SelectionState.Pressed`）。</summary>
        public bool Pressed { get; private set; }

        ImageQuad[] _tq;
        Color[] _tqBase;
        float _k = 1f, _kTarget = 1f;

        /// <summary>指针进来（`PointerLayer` 唯一派发口）。</summary>
        public void Enter()
        {
            if (Hovered) return;
            Hovered = true;
            if (onEnter != null) onEnter();
            SetTarget(Hovered && Pressed ? PressedK : HighlightK);
            SwapTo(Pressed && _pressedTex != null ? _pressedTex : _hoverTex);
        }

        /// <summary>指针离开。</summary>
        public void Exit()
        {
            if (!Hovered) return;
            Hovered = false;
            if (onExit != null) onExit();
            SetTarget(Pressed ? PressedK : 1f);
            SwapTo(_normalTex);
        }

        /// <summary>左键按下（**不派发 `onClick`** —— 那只在「按下与抬起同一件」时才发生）。</summary>
        public void Press()
        {
            Pressed = true;
            SetTarget(PressedK);
            SwapTo(_pressedTex ?? _hoverTex);
            if (onDown != null) onDown();
        }

        /// <summary>左键抬起（不论抬在哪 —— 原版 `Pressed` 态在这一刻结束）。</summary>
        public void Release()
        {
            Pressed = false;
            SetTarget(Hovered ? HighlightK : 1f);
            SwapTo(Hovered ? _hoverTex : _normalTex);
            if (onUp != null) onUp();
        }

        public System.Action onDown, onUp;

        void SetTarget(float k)
        {
            if (!tintOnHover) return;
            _kTarget = k;
            if (!Application.isPlaying) { _k = k; ApplyTint(); }
        }

        /// <summary>色偏补间（照原版 `m_FadeDuration = 0.1s`）。
        /// ⚠️ 只在 Play 里跑得到（批处理下 `Update` 不执行）—— 自检看的是**目标色**，见类注释。</summary>
        void Update()
        {
            if (_k == _kTarget) return;
            _k = Mathf.MoveTowards(_k, _kTarget,
                                   Mathf.Max(0.0001f, Mathf.Abs(_kTarget - _k)) *
                                   Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.0001f, FadeSeconds)));
            if (Mathf.Abs(_k - _kTarget) < 0.001f) _k = _kTarget;
            ApplyTint();
        }

        /// <summary>这一颗名下**要跟着变色的 quad**。默认 = 自己这棵子树里的全部
        /// （直接挂在大图上的按钮就是这一档；`MenuDraw.Hit` 那种「另建一个透明命中区」的
        /// 子树里只有那个**透明** quad ⇒ 变色看不见 = 与原版 `m_Transition = 0` 等效）。</summary>
        void Collect()
        {
            if (_tq != null) return;
            _tq = GetComponentsInChildren<ImageQuad>(true);
            _tqBase = new Color[_tq.Length];
            for (int i = 0; i < _tq.Length; i++) _tqBase[i] = _tq[i] != null ? _tq[i].Tint : Color.white;
        }

        void ApplyTint()
        {
            Collect();
            for (int i = 0; i < _tq.Length; i++)
            {
                if (_tq[i] == null) continue;
                var b = _tqBase[i];
                _tq[i].SetTint(new Color(b.r * _k, b.g * _k, b.b * _k, b.a));
            }
        }

        /// <summary>自检用：当前色偏系数（1 = 原色 · 0.9608 = 悬停 · 0.7843 = 按下）。</summary>
        public float TintKForTest { get { return _k; } }

        /// <summary>自检用：**当前贴在 `target` 上的图**（换图那一档靠它验「悬停后确实换了 / 离开换回来了」）。</summary>
        public Texture CurrentTexForTest { get { return target != null ? target.Texture : null; } }
        /// <summary>自检用：绑定的常态图 / 高亮图 / 按下图（没接换图的三者都是 null）。</summary>
        public Texture NormalTexForTest { get { return _normalTex; } }
        public Texture HoverTexForTest { get { return _hoverTex; } }
        public Texture PressedTexForTest { get { return _pressedTex; } }

        // 🔴 **没有「登记表」**（2026-09-23 撤掉）：原来想用 `OnEnable/OnDisable` 维护一张静态表让指针层扫，
        //    但**自检跑在编辑模式**，而编辑模式下这两个回调**只对 `[ExecuteAlways]` 的脚本**才跑
        //    ⇒ 自检里表恒为空（实测「场景里 13 个 `WindowButton`、登记表 0 个」）。
        //    现在 `PointerLayer` **在真有输入事件时**才 `FindObjectsByType<WindowButton>()` 扫一遍
        //    （事件很少，代价可忽略），**不依赖任何生命周期回调**。

        /// <summary>点一下 —— **`PointerLayer` 唯一的派发口**。</summary>
        public void Click() { if (onClick != null) onClick(); }

        /// <summary>🔴 **老式的 `OnMouseUpAsButton`：在这个工程里一次也不会派发** ——
        /// ① 它要求同一物体上有 `Collider`，而本工程**零处**加过 collider；
        /// ② `ProjectSettings.asset:932 activeInputHandler = 1`（只用新 Input System）⇒ 老式 `OnMouseXxx` 不派发。
        /// 见 `项目任务.md` §三 第 15 条 **第 23 条**。**留着只为「原版是 UGUI 按钮」这条线索可查，
        /// 别再往它上面挂新逻辑**（新逻辑挂 `onClick`，由 `PointerLayer` 派发）。</summary>
        void OnMouseUpAsButton() { Click(); }

        /// <summary>自检用：批处理里没有输入事件，直调这条路（**和真点走同一个 `Click()`**）。</summary>
        public void ClickForTest() { Click(); }
    }
}
