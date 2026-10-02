// ImportDeckPopup.cs — 卡组线 ④b 的第二个弹窗：**`Import Deck Popup`**（原版 `ImportDeckPopup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0923/A1_外壳与弹窗.md` **§4**（逐节点表 + 根脚本字段）。
// 窗口参数：`type=1 Popup` · `windowsPlacement=15`（表头）。
//
// 🔴 **它接上了 Deck 页那个 `Import` 钮** —— 在此之前点它是 `NotifyNotBuilt`（不是导入）。
//
// ============================ 结构（照 A1 §4 抄）============================
//   · 根 **不是全伸展**（`sz=(1919,1079)`）；`Background` 色 **(0,0,0,.396)**、**点背景就关**（`backgroundCloseButton`）
//   · `Window` **560,234.07 → 1360,685.93** · `Generic Popup Background` = `40k_popup` Sliced（border 169/160）
//   · `Mask`（四边内缩 ~10.4/9.44）+ 子 `Background fill` = `40k_popup_texture` **Tiled**（128 一格、`ppuMultiplier=2` ⇒ 64）
//   · `Main Search message` TMP **"Paste your deck"** **fs50**、hAlign=Right
//   · `Input Field` = `40K_dropdown_bg` Sliced（border 23/20）、色 **(.29,.953,.682,1)**（绿）
//     → `Text Area`（`RectMask2D`）→ `Placeholder` **"Enter text..." fs32** col(.67,.67,.67,.5) ·
//       `Text` fs32 col(.858,.858,.858,1) —— 两条都是 **hAlign=Center**
//   · `Error msg` TMP **fs28**、hAlign=Right
//   · `Buttons`（VLG）**只有一个** `Generic UI Button` = `40K_button`（478.343×75）+ 字 **"Confirm" fs45**
//   · `Generic Close Button Green` = 圆钮 + **`40k_bt_close`**（56.37×54.50）
//
// ---- 与卡组编辑那边的关系（**一条行为、两处入口**）----
//   校验那一步**只留一份**：`CollectionData.ImportDeck`（内部走 `DeckLibrary.ImportString`，
//   错误文案与 `DeckRuntime.TryImport` **逐字一致**）。本窗只负责画 + 转调。
//   键盘走 `PointerLayer` 的文本焦点（外壳唯一那条键盘路，2026-09-23 加的）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`ImportDeckPopup : GameWindow` —— 粘贴卡组串那扇窗。</summary>
    public class ImportDeckPopup : GameWindow
    {
        // 层：高于收藏窗（最高 3043）、与 `Deck info Popup` 同段；**低于** `PromptPopup`（3140+）
        public const int QImp = 3130, QImpRow = 3131, QImpText = 3132, QImpHit = 3133;

        // ---- 几何（A1 §4 逐条）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.396f);
        public const float WinL = 560f, WinT = 234.07f, WinR = 1360f, WinB = 685.93f;
        public const float MsgL = 610f, MsgT = 280f, MsgR = 1310f, MsgB = 340f, MsgFontPx = 50f;
        public const float InL = 610f, InT = 370f, InR = 1310f, InB = 511.06f;
        public const float TxtL = 620f, TxtT = 377f, TxtR = 1300f, TxtB = 505.06f, TxtFontPx = 32f;
        public const float ErrL = 593.05f, ErrT = 527.50f, ErrR = 1326.95f, ErrB = 562.50f, ErrFontPx = 28f;
        /// <summary>`Buttons` 容器（VLG · align=MiddleCenter · 只有一个钮）。</summary>
        public const float BtnsL = 593.05f, BtnsT = 562.43f, BtnsR = 1326.95f, BtnsB = 652.43f;
        public const float OkW = 478.343f, OkH = 75f;
        public const float CloseL = 1317.30f, CloseT = 202.10f, CloseR = 1392.30f, CloseB = 277.10f;
        /// <summary>`Mask` 相对 `Window` 四边各内缩一半（`sd=(−20.268,−19.245)`）。</summary>
        public const float MaskInX = 20.268f, MaskInY = 19.245f;

        static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);
        const float PopupTexW = 359f, PopupTexH = 336f;
        static readonly Vector4 DropBorder = new Vector4(23f, 20f, 23f, 20f);
        const float DropTexW = 119f, DropTexH = 102f;
        static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>`40k_popup_texture` 的平铺格 = 128 ÷ `ppuMultiplier 2.0`（同 `PromptPopup.FillTilePx`）。</summary>
        public const float FillTilePx = 64f;

        string _error = "";
        /// <summary>当前输入串（自检用）。</summary>
        public string InputText { get { return _text ?? ""; } }
        string _text = "";
        /// <summary>错误行现在显示什么（自检用）。</summary>
        public string ErrorText { get { return _error; } }

        public Transform OkHit { get { return transform.Find("Buttons/OkHit"); } }
        public Transform CloseHit { get { return transform.Find("CloseHit"); } }
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

        /// <summary>导入成功之后要做什么（收藏窗用它重画卡组列表）。**没有就只打日志。**</summary>
        public System.Action OnImported;

        public static ImportDeckPopup Create(WindowsManager mgr, System.Action onImported = null)
        {
            var go = new GameObject("Import Deck Popup");
            var win = go.AddComponent<ImportDeckPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            win.OnImported = onImported;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { _text = ""; _error = ""; Build(); }

        /// <summary>自检用：直接喂一串（等同键盘打完）。</summary>
        public void SetTextForTest(string s) { _text = s ?? ""; }


        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);

            // 1) 压暗 + 点背景关（原版 `backgroundCloseButton`）
            Solid(root, 960f, 540f, 1920f, 1080f, ShadeColor, QImp, "Background");
            Hit(root, "BackgroundHit", new PxRect(0f, 0f, 1920f, 1080f), () => Close(), QImpHit - 1);

            // 2) 面板（九宫格 `40k_popup`）
            var win = new GameObject("Window");
            win.transform.SetParent(root, false);
            win.transform.localPosition = Local3(root, WinL, WinT, WinR, WinB);
            Nine(win.transform, win.transform, "40k_popup", PopupBorder, PopupTexW, PopupTexH, WinL, WinT, WinR, WinB,
                 QImp, "Generic Popup Background");
            // `Mask`（`showGraphic=0`，我们不做真 mask —— 只在里面铺那层 Tiled 纹理）
            {
                var m = new GameObject("Mask");
                m.transform.SetParent(win.transform, false);
                m.transform.localPosition = Local3(root, WinL + MaskInX * 0.5f, WinT + MaskInY * 0.5f,
                                                   WinR - MaskInX * 0.5f, WinB - MaskInY * 0.5f);
                Tiled(m.transform, m.transform, "40k_popup_texture",
                      WinL + MaskInX * 0.5f, WinT + MaskInY * 0.5f, WinR - MaskInX * 0.5f, WinB - MaskInY * 0.5f,
                      QImpRow, "Background fill");
            }

            // 3) 文案 + 输入框（+ 错误行）
            Txt(root, "Paste your deck", MsgL, MsgT, MsgR, MsgB, MsgFontPx, Align.Right, "Main Search message", QImpText);
            Nine(win.transform, win.transform, "40K_dropdown_bg", DropBorder, DropTexW, DropTexH, InL, InT, InR, InB,
                 QImpRow, "Input Field", new Color(0.29f, 0.953f, 0.682f, 1f));
            RefreshInputText();
            Txt(root, _error, ErrL, ErrT, ErrR, ErrB, ErrFontPx, Align.Right, "Error msg", QImpText);
            AddHitOn(win.transform, win.transform, "InputHit", new PxRect(InL, InT, InR, InB), () => BeginTyping());

            // 4) `Confirm`（VLG 只有一个钮 ⇒ 容器内居中）
            {
                float x1 = BtnsL + (BtnsR - BtnsL - OkW) * 0.5f;
                float y1 = BtnsT + (BtnsB - BtnsT - OkH) * 0.5f;
                var r = new PxRect(x1, y1, x1 + OkW, y1 + OkH);
                var b = new GameObject("Buttons");
                b.transform.SetParent(root, false);
                var confirmBg = Nine(b.transform, b.transform, "40K_button", BtnBorder, BtnTexW, BtnTexH, r.x1, r.y1, r.x2, r.y2,
                     QImpRow, "Confirm Bg");
                Txt(b.transform, "Confirm", r.x1, r.y1, r.x2, r.y2, 45f, Align.Center, "Confirm Text", QImpText);
                // A17：原版 `Window>Buttons>Generic UI Button` 是 SpriteSwap（普查 §块 3 第 11 行）
                var okHit = HitOn(b.transform, b.transform, "OkHit", r, () => TryImport());
                var okWb = okHit != null ? okHit.GetComponent<WindowButton>() : null;
                if (okWb != null) okWb.BindNine(confirmBg, "40K_button");
            }

            // 5) 关闭圆钮（绿的那一颗）
            Img(root, "UI_Button_Round_background", CloseL, CloseT, CloseR, CloseB, "Close Bg", QImpRow, true);
            float ix1 = CloseL + (CloseR - CloseL - 56.37f) * 0.5f;
            float iy1 = CloseT + (CloseB - CloseT - 54.50f) * 0.5f;
            var closeIconQ = Img(root, "40k_bt_close", ix1, iy1, ix1 + 56.37f, iy1 + 54.50f, "Close Icon", QImpRow, true);
            // A17：原版 `Window>Generic Close Button Green` 是 SpriteSwap，`40k_bt_close` → `40k_bt_close_hover`（普查 §块 3 第 12 行）
            var clHit = Hit(root, "CloseHit", new PxRect(CloseL, CloseT, CloseR, CloseB), () => Close());
            var clWb = clHit != null ? clHit.GetComponent<WindowButton>() : null;
            if (clWb != null) clWb.Bind(closeIconQ, "40k_bt_close");
        }

        /// <summary>输入框里那行字（空 ⇒ 显示占位符）。**原版 `Text`/`Placeholder` 都是 hAlign=Center**。</summary>
        void RefreshInputText()
        {
            var holder = transform.Find("Window");
            var old = holder != null ? holder.Find("Input Text") : null;
            if (old != null) RewardsWindow.DestroySafe(old.gameObject);
            bool empty = string.IsNullOrEmpty(_text);
            // ⚠️ `basis` 必须是**实际父节点**（这里是 `Window`，不是 root）—— 见文件头第 2 条
            var lb = Label.Create(holder, empty ? "Enter text..." : _text, Local3(holder, TxtL, TxtT, TxtR, TxtB),
                                  5, empty ? new Color(0.67f, 0.67f, 0.67f, 0.5f) : new Color(0.858f, 0.858f, 0.858f, 1f),
                                  new Vector2(0.5f, 0.5f), "Input Text");
            if (lb == null) return;
            lb.SetRenderQueue(QImpText);
            lb.SetGlyphHeight(LayoutSpace.Px(TxtFontPx));
        }

        /// <summary>点输入框 ⇒ 交给 `PointerLayer` 的文本焦点（外壳唯一那条键盘路）。</summary>
        void BeginTyping()
        {
            var pl = PointerLayer.Instance;
            if (pl == null) return;
            pl.BeginText(_text, 4096,
                         s => { _text = s ?? ""; RefreshInputText(); },
                         () => RefreshInputText(),
                         s => { _text = s ?? ""; RefreshInputText(); });
        }

        /// <summary>点 `Confirm`：**校验只走 `CollectionData.ImportDeck` 那一份**。</summary>
        public bool TryImport()
        {
            var pl = PointerLayer.Instance;
            if (pl != null && pl.TextEditing) pl.EndText(true);     // 键盘焦点先收（不然它还在改缓冲）
            string why;
            string name = CollectionData.ImportDeck(_text, out why);
            if (string.IsNullOrEmpty(name))
            {
                _error = why;
                Debug.LogWarning("[ImportDeck] 导入失败：" + why);
                RebuildErrorLine();
                return false;
            }
            Debug.Log("[ImportDeck] 导入成功：「" + name + "」");
            if (OnImported != null) OnImported();
            Close();
            return true;
        }

        void RebuildErrorLine()
        {
            var root = transform;
            var old = root.Find("Error msg");
            if (old != null) RewardsWindow.DestroySafe(old.gameObject);
            Txt(root, _error, ErrL, ErrT, ErrR, ErrB, ErrFontPx, Align.Right, "Error msg", QImpText);
        }

        // ============================================================ 画图小工具
        // ⚠️ **坐标一律页面绝对 px、basis 给【实际父节点】**（`Local3` 算的是 `RectCenter(绝对) − basis.position`）
        //    —— 两种错法都在 `DeckInfoPopup` 那轮踩过，见 `项目任务.md` §三 第 15 条 第 38 项。

        enum Align { Center, Left, Right }

        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - basis.position;

        void Solid(Transform parent, float cx, float cy, float w, float h, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(parent, CardArt.Solid(), Local3(parent, cx - w * 0.5f, cy - h * 0.5f,
                                                                       cx + w * 0.5f, cy + h * 0.5f),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect(w / h); quad.SetTint(color); quad.SetRenderQueue(q);
        }

        ImageQuad Img(Transform parent, string art, float x1, float y1, float x2, float y2,
                      string name, int q, bool keepAspect)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[ImportDeck] 图取不到：" + art); return null; }
            float w = x2 - x1, h = y2 - y1;
            if (keepAspect && tex.height > 0)
            {
                float sa = (float)tex.width / tex.height, ra = w / Mathf.Max(1e-6f, h);
                if (sa > ra) { float nh = w / sa, d = (h - nh) * 0.5f; y1 += d; y2 -= d; h = nh; }
                else { float nw = h * sa, d = (w - nw) * 0.5f; x1 += d; x2 -= d; w = nw; }
            }
            var quad = ImageQuad.Create(parent, tex, Local3(parent, x1, y1, x2, y2),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / Mathf.Max(1e-6f, h)); quad.SetRenderQueue(q);
            return quad;
        }

        GameObject Nine(Transform parent, Transform basis, string art, Vector4 border, float texW, float texH,
                        float x1, float y1, float x2, float y2, int q, string name, Color? tint = null)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[ImportDeck] 九宫格图取不到：" + art); return null; }
            var g = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH, Local3(basis, x1, y1, x2, y2),
                                              LayoutSpace.Px(x2 - x1), LayoutSpace.Px(y2 - y1), name);
            if (g != null)
                foreach (var c in g.GetComponentsInChildren<ImageQuad>())
                { c.SetRenderQueue(q); if (tint.HasValue) c.SetTint(tint.Value); }
            return g;
        }

        void Tiled(Transform parent, Transform basis, string art, float x1, float y1, float x2, float y2, int q, string name)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) return;
            var g = ImageQuad.CreateTiled(parent, tex, FillTilePx, FillTilePx, Local3(basis, x1, y1, x2, y2),
                                          LayoutSpace.Px(x2 - x1), LayoutSpace.Px(y2 - y1), name);
            if (g != null) foreach (var c in g.GetComponentsInChildren<ImageQuad>()) c.SetRenderQueue(q);
        }

        Label Txt(Transform parent, string text, float x1, float y1, float x2, float y2, float fontPx,
                  Align align, string name, int q)
        {
            var lb = Label.Create(parent, text ?? "", Local3(parent, x1, y1, x2, y2), 5, Color.white,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            if (align == Align.Right) lb.AlignRightOn(LayoutSpace.FromPixel(x2, 0f).x);
            else if (align == Align.Left) lb.AlignLeftOn(LayoutSpace.FromPixel(x1, 0f).x);
            return lb;
        }

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QImpHit)
            => HitOn(parent, parent, name, r, onClick, q);

        Transform AddHitOn(Transform parent, Transform basis, string name, PxRect r, System.Action onClick,
                           ImageQuad target = null, string art = null,
                           string hoverArt = null, string pressedArt = null)
            => HitOn(parent, basis, name, r, onClick, QImpHit, target, art, hoverArt, pressedArt);

        Transform HitOn(Transform parent, Transform basis, string name, PxRect r, System.Action onClick,
                        int q = QImpHit, ImageQuad target = null, string art = null,
                        string hoverArt = null, string pressedArt = null)
        {
            var hit = new GameObject(name);
            hit.transform.SetParent(parent, false);
            hit.transform.localPosition = Local3(basis, r.x1, r.y1, r.x2, r.y2);   // **节点本身也要摆**
            var quad = ImageQuad.Create(hit.transform, CardArt.Solid(), Vector3.zero,
                                        LayoutSpace.Px(r.H), new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt, pressedArt);
            return hit.transform;
        }
    }
}
