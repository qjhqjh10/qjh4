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
            // 🔴 **2026-10-04（A25⑥）**：档从 `QImpHit − 1`（= 3132，**从内容档派生**出来的，
            //    而且正好撞上 `QImpText`）改成 **压暗层自己那一档 `QImp`(3130)** —— 统一到
            //    `BoosterInfoPopup.QShadeHit` 那条正确编码，并改走公共件 `MenuDraw.ShadeHit`
            //    （它现场核「压暗档 **严格低于** 本窗内容命中区档」，不满足就告警）。收口理由见那边注释。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QImp, QImpHit, () => Close(), "BackgroundHit");

            // 2) 面板（九宫格 `40k_popup`）
            var win = new GameObject("Window");
            win.transform.SetParent(root, false);
            win.transform.localPosition = Local3(root, WinL, WinT, WinR, WinB);
            Nine(win.transform, win.transform, "40k_popup", PopupBorder, PopupTexW, PopupTexH, WinL, WinT, WinR, WinB,
                 QImp, "Generic Popup Background");
            // 🆕 **2026-10-06（A94）：面板底图吸收点击**。判据 = 原版 prefab
            //   `Import Deck Popup > Window > Generic Popup Background` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `Generic Close Button Green` 与整屏那颗
            //   `Background`(`EverguildButton`) 是**别的件**）⇒ 原版点这里**什么都不做**。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(WinL, WinT, WinR, WinB), QImp, QImpHit);
            // `Mask`（`showGraphic=0`，我们不做真 mask —— 只在里面铺那层 Tiled 纹理）
            // 🔴 **2026-10-04（A25⑤）**：原来走本文件自己那个 `Tiled(...)` 包装（**绕开公共件**
            //    ⇒ 拿不到 `clip` / `clipSoftness`），已收口到 `MenuDraw.Tiled`。摆位逐项等价：
            //    `Mask` 节点本来就建在这个矩形中心 ⇒ `MenuDraw.Local` 算出来恒为 0。
            {
                var m = new GameObject("Mask");
                m.transform.SetParent(win.transform, false);
                m.transform.localPosition = Local3(root, WinL + MaskInX * 0.5f, WinT + MaskInY * 0.5f,
                                                   WinR - MaskInX * 0.5f, WinB - MaskInY * 0.5f);
                var fillTex = CardArt.MenuUi("40k_popup_texture");
                if (fillTex == null)
                    Debug.LogWarning("[ImportDeck] 平铺图取不到：`40k_popup_texture`（面板填充没铺）"
                                     + " —— 导入器：`工具/import_original_art.py`");
                else
                    MenuDraw.Tiled(m.transform, fillTex,
                                   new PxRect(WinL + MaskInX * 0.5f, WinT + MaskInY * 0.5f,
                                              WinR - MaskInX * 0.5f, WinB - MaskInY * 0.5f),
                                   FillTilePx, QImpRow, "Background fill");
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
        // ⚠️ **坐标一律页面绝对 px、basis 给【实际父节点】**（`Local3` 算的是
        //    `RectCenter(绝对) − MenuDraw.PosInDesignSpace(basis)` —— 🆕 2026-10-11（A306③）起
        //    位置项走设计空间，与 `MenuDraw.Local` **逐字同源**；`k == 1` 时与旧写法逐位相同）
        //    —— 两种错法都在 `DeckInfoPopup` 那轮踩过，见 `项目任务.md` §三 第 15 条 第 38 项。

        enum Align { Center, Left, Right }

        /// <summary>🔴 **2026-10-11（A306③）**：`basis` 的**位置**先换算进**设计空间**再减
        /// （`MenuDraw.PosInDesignSpace`）—— 改前写的是 `− basis.position`，**少除了一次 `basis` 上面
        /// 那一级的 `lossyScale`**（与 A294 / A297 修掉的 `MenuDraw.Local` / `MainMenuSubmenuWindow.Local`
        /// 是**同一个病**，本处 = 那份算式的同形副本）。小屏缩放开关一开（窗根 ×M），
        /// `basis.position` 是**已放大**的世界坐标，而 `RectCenter` 给的是**设计坐标** ⇒ 两者不同量纲。
        ///
        /// <para>⚠️ **首参是 `basis`（坐标基准）不是树父 `parent`** ⇒ 这里**只改量纲、不动 `basis` 语义**：
        /// ⛔ 别换成 `MenuDraw.Local(parent, …)`（`Local3` 另有 `basis != parent` 的调用形状 —— `HitOn`
        /// 就是分开收这两个参数的）；收口成 `MenuDraw.Local(basis, …)` 只在 `basis == parent` 时逐字等价，
        /// 而那正是本文件 `Nine`（上面那条守卫）管的事。</para>
        ///
        /// <para>📌 `k == 1`（缩放开关出厂关着）时与改前**逐位相同**。
        /// · `basis == null`：旧写法 NRE，新写法返回零分量（`PosInDesignSpace` 首句）——
        ///   **实读本文件没有这种调用点**，这一条只是行为边界、不是放宽。</para>
        ///
        /// <para>🔴 **改坏法**：换回裸 `basis.position` ⇒ **今天一条现有断言都不会红**
        /// （`k == 1` 时两式逐位相同 ⇒ 这是**潜伏缺陷**）⇒ 要补的两态断言写在
        /// `资料/普查产出_1011/W4_子3.md` §四，由调度台安排。</para></summary>
        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - MenuDraw.PosInDesignSpace(basis);

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
            // 🔴 **2026-10-06（A50③）：改走公共件 `MenuDraw.Nine`**（旧写法直调 `ImageQuad.CreateNineSlice`
            //   ⇒ 绕开公共件、**拿不到 `clip` / `clipSoftness`**）。与旧代码**逐项等价**：
            //    ① **矩形** = `(x1,y1)-(x2,y2)`（旧代码喂的 `LayoutSpace.Px(x2-x1)` / `(y2-y1)`
            //       就是公共件内部的 `LayoutSpace.Px(r.W)` / `Px(r.H)`）；
            //    ② **落位** = `Local3(basis, …)` 与 `MenuDraw.Local(parent, …)` **是同一份算式**
            //       （`LayoutSpace.RectCenter(…) − 基准的**设计空间**位置`，逐字相同 ——
            //        🆕 2026-10-11（A306③）起两边都走 `MenuDraw.PosInDesignSpace`）⇒ 收口只在 `basis == parent`
            //       时才等价 —— 公共件**只认 `parent` 一个基准**（树父与坐标基准是同一个参数）。
            //       本文件 3 个调用点**全都传同一个对象**（`:109` / `:133` 的 `Nine(win.transform, win.transform, …)`、
            //       `:146` 的 `Nine(b.transform, b.transform, …)`），而这不等于「以后也一定」
            //       ⇒ 不等就**是位置画错**，⛔ 不许静默（下面出声）。
            //    ③ **队列 = `q`** + **tint 传 `tint`**（旧代码建完逐块设的就是这两样，公共件会替我们设；
            //       `tint` 没传时两边**都不设**，同一条退化）。九宫格切边不用我们管：块数与每块的
            //       `SetAspect` 都由 `CreateNineSlice` 按真九宫格算好（⛔ 别再给子块套整个面板的比例）。
            if (!ReferenceEquals(basis, parent))
                Debug.LogWarning("[ImportDeck] 九宫格 `Nine` 的 `basis` 必须等于 `parent`"
                                 + "（`MenuDraw.Nine` 只按 `parent` 定位，两个不同就会摆错位置）");
            return MenuDraw.Nine(parent, tex, new PxRect(x1, y1, x2, y2), border, texW, texH, q, tint, true, name);
        }

        // 🗑 **2026-10-04（A25⑤）删掉了本文件自己那份 `Tiled(...)` 包装** —— 它是「绕开
        //    `MenuDraw.Tiled` 的第四条平铺路」（拿不到 `clip`）。唯一的调用点已改走公共件。
        // 🔴 **2026-10-06（A50③）：同族的 `Nine(...)` 也收口了**（旧注释写「保留 … 收口留给下一次」，
        //    那一次就是现在）—— 包装**保留**（调用方那 12 个实参的写法不动），但体内已改调
        //    `MenuDraw.Nine`，于是 `clip` / `clipSoftness` 这条路**已经通到本窗**。

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
