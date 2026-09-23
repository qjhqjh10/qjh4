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

        // 🔴 渲染队列**都要在奖励窗之上**（奖励窗最高 `QOverlay = 3014`）—— 弹窗要能盖住任何菜单窗。
        public const int QShade = 3018, QPanel = 3020, QFill = 3021, QContent = 3022, QText = 3023;

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
            var fillRect = new PxRect(px1 - BgPad + MaskInsetX * 0.5f, py1 - BgPad + MaskInsetY * 0.5f,
                                      px2 + BgPad - MaskInsetX * 0.5f, py2 + BgPad - MaskInsetY * 0.5f);
            var fillGo = Node(root, "Background fill", fillRect);
            var fillTex = CardArt.MenuUi(ArtPopupFill);
            if (fillTex != null)
            {
                var t = ImageQuad.CreateTiled(fillGo, fillTex, FillTilePx, FillTilePx, Vector3.zero,
                                              LayoutSpace.Px(fillRect.W), LayoutSpace.Px(fillRect.H), "Tiles");
                foreach (var q in t.GetComponentsInChildren<ImageQuad>()) q.SetRenderQueue(QFill);
            }

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

        void OnMouseUpAsButton() { onClick?.Invoke(); }

        /// <summary>自检用：批处理里没有鼠标事件，直调这条路（**和 `OnMouseUpAsButton` 同一个 action**）。</summary>
        public void ClickForTest() { onClick?.Invoke(); }
    }
}
