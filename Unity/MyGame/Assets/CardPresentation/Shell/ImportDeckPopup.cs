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
//   · `Main Search message` TMP **"Paste your deck"** **fs50**、HA=**2**(Center) —— 🆕 **走词条 `TitleTerm`**（A891）
//   · `Input Field` = `40K_dropdown_bg` Sliced（border 23/20）、色 **(.29,.953,.682,1)**（绿）
//     → `Text Area`（`RectMask2D`）→ `Placeholder` **"Enter text..." fs32** col(.67,.67,.67,.5) ·
//       `Text` fs32 col(.858,.858,.858,1) —— 两条都是 **HA=1(Left) / VA=256(Top)**
//   · `Error msg` TMP **fs28**、HA=**2**(Center)
//
// 🔴 **2026-10-17 订正（D46 那条顺带查出来的 · 铁律 5）**：上面这三处的对齐原写作
//   「`Main Search message` hAlign=Right · `Text`/`Placeholder` 两条都是 hAlign=Center ·
//    `Error msg` hAlign=Right」—— **三条里两条是错的**（那是照 A1 §4 那张表抄的，而 A1 那两行没实读 TMP）。
//   逐字段实读 `bundle_menus_assets_all` 的 `Import Deck Popup` 全树（本工程自己的尺子，
//   `工具/menu_rect.Bundle` 遍历 + 逐颗 TMP 的 `m_HorizontalAlignment`/`m_VerticalAlignment`）：
//     `Main Search message` **HA=2 VA=512** · `Error msg` **HA=2 VA=512** ·
//     `Placeholder`/`Text` **HA=1 VA=256**（`fs32` · rect 620,377→1300,505.06）。
//   ⇒ ① 输入框那两颗 = **Left/Top** ⇒ **已改**（`RefreshInputText`，见那里的注释）；
//      ② 另两处（`Main Search message` / `Error msg`）= **Center/Middle** ⇒
//         🔴 **2026-10-17（B11）已改**：本文件两处消费点（`Build` 与 `RebuildErrorLine`，
//         ⛔ 别按行号找，它们会漂）原来的 `Align.Right` 全部换成 `Align.Center`。
//         改前那两句「hAlign=Right」是**照 A1 §4 那张表抄的、没实读 TMP**（铁律 5 那条错）。
//         断言 → `Editor/ShellScene.cs` 的 **B11/A883** 那一节（HA=2 / VA=512 **逐字面量** +
//         一条「改回右对齐必红」的**判别式**）。
//   · `Buttons`（VLG）**只有一个** `Generic UI Button` = `40K_button`（478.343×75）+ 字 **"Confirm" fs45**
//     —— 🆕 **走词条 `ConfirmTerm`**（A891；`Button Text` 那颗挂着 `Localize = MainMenu/General/Confirm`）
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
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那颗 `Icon` 子件**自己的** `m_RaycastPadding`
        /// （原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）⇒ 命中区 = 子件矩形（56.37×54.50）外扩 20
        /// = **96.37 × 94.50**（⛔ 不是根矩形 `CloseL..CloseB` 的 75×75）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);
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

        /// <summary>输入框**占位符**的词条键 —— **原版 prefab 上那颗 `Localize` 的 `mTerm` 原文**
        /// （⛔ 不许自拟，见 `Core/Loc.cs` 文件头）。
        /// <para>出处 = 逐字段实读 `bundle_menus_assets_all/MonoBehaviour_6085227748672536722.json`：
        /// 它挂的 GameObject 是 `Import Deck Popup/Window/Input Field/Text Area/**Placeholder**`
        /// （兄弟 `Text` 那颗**没有** `Localize` —— 它装的是玩家打进去的字，本来就不该翻）；
        /// 同一颗上的 TMP `m_text = "Enter text..."` 就是该词条的**英文列**（照抄，不译）。
        /// 🔴 **全库只此一颗用这个键**（`grep -rl MenuDeck/HUD/EnterText` 扫
        /// `d:/2/新解包资源/assets_full/` 的 24.7 万文件 ⇒ 命中 **1**）。</para>
        /// <para>⚠️ 本窗**另两颗** TMP 的词条**不在本批范围**、也**不是**这一条：
        /// `Main Search message` 挂的是 `MenuDeck/Share/PasteDeck`（`MonoBehaviour_8528767437303251090.json`）、
        /// `Error msg` 那颗**一个 `Localize` 组件都没有**（那条文案是引擎写进去的）。</para></summary>
        public const string PlaceholderTerm = "MenuDeck/HUD/EnterText";

        /// <summary>**提示行 / 标题**（`Main Search message`）的词条键 —— 原版 prefab 上那颗 `Localize` 的 `mTerm` 原文。
        /// <para>出处 = 逐字段实读 `bundle_menus_assets_all/MonoBehaviour_8528767437303251090.json`：它挂的 GameObject 是
        /// `Import Deck Popup/Window/**Main Search message**`，同一颗上的 TMP `m_text = "Paste your deck"`
        /// 就是该词条的**英文列**（`python 工具/menu_dump.py bundle_menus_assets_all "Import Deck Popup" --depth 8` 实读，
        /// 与 `Build()` 那行实参 `610,280 → 1310,340` 逐位对上）。</para>
        /// <para>🔴 **2026-10-17（A891）已接**：原来写死英文 ⇒ 中文档也印英文。词条在 `Core/Loc.cs`（中文列 = 我们译的，
        /// 源 `数据/本地化/i18n/zh_CN.csv:143`）。断言 → `Editor/CollectionScene.cs` ⑨ 那一段（两语档各断一个字面量 + 判别式）。</para></summary>
        public const string TitleTerm = "MenuDeck/Share/PasteDeck";

        /// <summary>`Confirm` 钮（原版节点名 `Button Text`）的词条键 = 那颗 `Localize.mTerm` 原文。
        /// <para>出处 = 同一份实读：`…/Window/Buttons/Generic UI Button/**Button Text**` 上同时有
        /// TMP `m_text = "Confirm"`（`fs45`，= 该词条的**英文列**）与 `Localize.mTerm`。
        /// ⚠️ 这个键**不在 `MenuDeck/` 族**里 —— 原版自己复用了主菜单那条通用按钮词条
        /// （全仓另一处记着它：`Shell/ReferralPopupWindow.cs` 的 `TxtBtn` 注释）。</para>
        /// <para>🔴 **2026-10-17（A891）已接**：原来写死 `"Confirm"`。词条在 `Core/Loc.cs`（中文列 = 我们译的，
        /// 源 `数据/本地化/i18n/zh_CN.csv:83`）。断言 → 同上那一节。</para></summary>
        public const string ConfirmTerm = "MainMenu/General/Confirm";

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
            // 🔴 **2026-10-17（B11 · A883）**：对齐 = `Align.Center`（**原版 `HA=2` / `VA=512`**）——
            //   原来传的是 `Align.Right`（右对齐），那是**照 A1 §4 那张表抄的错**。判据 = 逐字段实读
            //   `bundle_menus_assets_all/MonoBehaviour_7476776257758560402.json`：那颗 TMP 的
            //   `m_HorizontalAlignment = 2`(Center) / `m_VerticalAlignment = 512`(Middle)。
            //   断言 → `Editor/ShellScene.cs` 的 B11/A883 那一节。
            // 🔴 **2026-10-17（A891）**：提示行走**词条**（`TitleTerm`，见那颗常量的 doc）—— 原版那颗挂着 `Localize`
            //   （`mTerm = "MenuDeck/Share/PasteDeck"`）⇒ 它**跟着语言变**，写死就永远是英文。
            //   ⚠️ 与占位符同一个坑：**建的时候取一次**，换语言不会回头改这扇已建的窗（见 `RefreshInputText` 的注释）。
            Txt(root, Loc.T(TitleTerm), MsgL, MsgT, MsgR, MsgB, MsgFontPx, Align.Center, "Main Search message", QImpText);
            Nine(win.transform, win.transform, "40K_dropdown_bg", DropBorder, DropTexW, DropTexH, InL, InT, InR, InB,
                 QImpRow, "Input Field", new Color(0.29f, 0.953f, 0.682f, 1f));
            RefreshInputText();
            // 🔴 **2026-10-17（B11 · A883）**：`Error msg` 的对齐 = `Align.Center` ——
            //   原版那颗是 **Center/Middle**（逐字段实读 `bundle_menus_assets_all/MonoBehaviour_
            //   -2174597011030277998.json`：`m_HorizontalAlignment = 2` / `m_VerticalAlignment = 512`），
            //   我们原来传的是 `Align.Right`（照 A1 §4 那张表抄的错）。
            //   ⚠️ 这一颗节点**有两个出生入口**（`Build` 与 `RebuildErrorLine`，⛔ 别按行号找）
            //   —— `CLAUDE.md` §10 第 5 条：多入口的状态要在**每个入口**都设对 ⇒ **两处都传 `Align.Center`**。
            Txt(root, _error, ErrL, ErrT, ErrR, ErrB, ErrFontPx, Align.Center, "Error msg", QImpText);
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
                // 🔴 **2026-10-17（A891）**：钮上的字走**词条**（`ConfirmTerm`，见那颗常量的 doc）——
                //   原版那颗 `Button Text` 挂着 `Localize`（`mTerm = "MainMenu/General/Confirm"`）⇒ 跟着语言变。
                var okLb = Txt(b.transform, Loc.T(ConfirmTerm), r.x1, r.y1, r.x2, r.y2, 45f, Align.Center, "Confirm Text", QImpText);
                // 🔴 **2026-10-18（A892）：纵向档显式落成 `Midline`。** 判据 = 原版那颗 TMP 按 pid 亲读
                //   （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6959005812579893394.json`）：
                //   `m_text='Confirm'` · **`m_VerticalAlignment = 4096`（= `Midline`）** · `m_HorizontalAlignment = 2`（Center）·
                //   `m_fontSize = 45`（上面那个 `45f` 就是它）。我方 `Battle/Label.cs` 的出厂档是
                //   `_vTier = VAlign.Middle`（= **512**）⇒ 不显式设就是**另一档**。
                //   ⚠️ 这批**只在这两处设档、不动 `Label` 的出厂值**（改出厂值会牵动全工程所有宿主
                //   —— `MenuDraw.SetVAlign` 的 doc 写着）。框高按**这颗钮自己的矩形**给。
                //   ⚠️ 与 `Deck/DeckRuntime.cs` 建的那扇同款窗（`imp_ok_t`）是**同一颗原版节点**、两处都设。
                if (okLb != null) MenuDraw.SetVAlign(okLb, Label.VAlign.Midline, r);
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
            var clHit = Hit(root, "CloseHit",
                            MenuDraw.PaddedRect(new PxRect(ix1, iy1, ix1 + 56.37f, iy1 + 54.50f), ClosePad),
                            () => Close());
            var clWb = clHit != null ? clHit.GetComponent<WindowButton>() : null;
            // 🆕 **2026-10-18（A1058 · 第六会话批 2）**：把高亮图**显式写出来**。
            //   ⚠️ **本处本来就是对的**（层 = 子件 `Icon` 画 `40k_bt_close`、`art` 也传的是常态图名 ——
            //   `Bind` 的表外后备 `+_hover` 与 `PressedNames` 已经推出 `40k_bt_close_hover` /
            //   `40k_bt_close_pressed`）⇒ 这一行是**写明白、不改行为**（普查 §② 把本处记成「没传 `hoverArt` ⇒ 要改」，
            //   现读订正：**推得出来，不是缺口**）。
            //   原版判据（亲读）=
            //   `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Import Deck Popup" --depth 6`：
            //   `Window/Generic Close Button Green` 那颗 `EverguildButton` 的
            //   **`m_TargetGraphic` = pid1558372262366281874** ⇒ **所属 GO 名 = `Icon`** · 贴图 = `40k_bt_close` ·
            //   `m_HighlightedSprite` = `40k_bt_close_hover`、`m_PressedSprite` = `40k_bt_close_pressed`；
            //   根自己那颗 `UI_Button_Round_background` 带 `m_RaycastTarget=0`。
            if (clWb != null) clWb.Bind(closeIconQ, "40k_bt_close", "40k_bt_close_hover");
        }

        /// <summary>输入框里那行字（空 ⇒ 显示占位符）。
        /// 🔴 **对齐 = `Left/Top`（原版 `align 1/256`）** —— 逐字段实读原版 prefab：
        ///   `Import Deck Popup/Window/Input Field/Text Area/{Placeholder,Text}` 两颗 TMP 都是
        ///   `m_HorizontalAlignment = **1**`(Left) · `m_VerticalAlignment = **256**`(Top)
        ///   （`256 = 0x100` = TMP 的 `Top`）；两颗的 rect 都是 **620,377 → 1300,505.06**。
        /// ⚠️ **2026-10-17 就地订正（铁律 5）**：本行原来写「原版 `Text`/`Placeholder` 都是
        ///   hAlign=**Center**」—— **与本 JSON 冲突、那句是错的**（D46）。当时大概是照着
        ///   「本工程 `TmpFont.NewText` 把所有 TMP 统一建成 Center」那条默认档写的，不是实读。
        /// 🔴 **同一批还有两条同族的**（同一次实读查出的）：
        ///   `Main Search message`（"Paste your deck"）与 `Error msg` 在那份 prefab 里两颗都是
        ///   `m_HorizontalAlignment = **2**`(Center) / `m_VerticalAlignment = 512`(Middle)，
        ///   而本文件那两处**消费点**（⛔ 别按行号找，它们会漂）原来传的是 `Align.Right`
        ///   ⇒ **我们右对齐、原版居中**。🔴 **2026-10-17（B11）两处都已改成 `Align.Center`** ——
        ///   「改没改对」的判据不在本文件，在 `Editor/ShellScene.cs` 的 **B11/A883** 那一节。</summary>
        void RefreshInputText()
        {
            var holder = transform.Find("Window");
            var old = holder != null ? holder.Find("Input Text") : null;
            if (old != null) RewardsWindow.DestroySafe(old.gameObject);
            bool empty = string.IsNullOrEmpty(_text);
            // 🆕 **2026-10-17（B11 · A884）**：占位符走**词条**（`PlaceholderTerm`，见那颗常量的 doc），
            //   ⛔ 不再写死 `"Enter text..."` —— 原版那颗 TMP 上挂着 `Localize`
            //   （`mTerm = "MenuDeck/HUD/EnterText"`）⇒ 它**跟着语言变**，写死就永远是英文。
            //   ⚠️ 逐次重算（本函数每次 `SetText`/重排都会再进来一次），⛔ 别缓存进字段：
            //   换语言之后**同一个窗**要能取到新的一行（`Loc` 不发事件，调用方自己重画，见 `Loc.SetLanguage`）。
            // ⚠️ `basis` 必须是**实际父节点**（这里是 `Window`，不是 root）—— 见文件头第 2 条
            var lb = Label.Create(holder, empty ? Loc.T(PlaceholderTerm) : _text, Local3(holder, TxtL, TxtT, TxtR, TxtB),
                                  5, empty ? new Color(0.67f, 0.67f, 0.67f, 0.5f) : new Color(0.858f, 0.858f, 0.858f, 1f),
                                  new Vector2(0.5f, 0.5f), "Input Text");
            if (lb == null) return;
            lb.SetRenderQueue(QImpText);
            lb.SetGlyphHeight(LayoutSpace.Px(TxtFontPx));
            // ---- 🆕 **2026-10-17（D46 第二处）：输入框那行字的对齐 = `Left/Top`** ----
            //   ⚠️ **顺序是死的**：`SetAlignLeft()` 给 HA=1（`TextAlignmentOptions.Left` **自带 V=Middle**）
            //     ⇒ `SetVAlign(Top, 框高)` 必须排在它**后面**，反过来会被那次赋值覆盖回 `Middle`。
            //   ⚠️ 框 = **原版那颗节点的 rect**（620,377 → 1300,505.06，就是上面 `TxtL/TxtT/TxtR/TxtB`）
            //     —— `Top` 那一档要**框高**才算得出目标位置（拿不到会出声并退回 `Middle`）。
            //   ✅ 参照物 = **卡组编辑窗那份已经改对的同款**（`Deck/DeckRuntime.cs` 的 `BuildImportPopup`
            //     里 `_impInputTx` 那三句，同一对调用、同一个理由）。
            lb.SetAlignLeft();
            MenuDraw.SetVAlign(lb, Label.VAlign.Top, new PxRect(TxtL, TxtT, TxtR, TxtB));
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
            // 🔴 **2026-10-17（B11 · A883）**：`Error msg` 的对齐 = `Align.Center` ——
            //   原版那颗是 **Center/Middle**（逐字段实读 `bundle_menus_assets_all/MonoBehaviour_
            //   -2174597011030277998.json`：`m_HorizontalAlignment = 2` / `m_VerticalAlignment = 512`），
            //   我们原来传的是 `Align.Right`（照 A1 §4 那张表抄的错）。
            //   ⚠️ 这一颗节点**有两个出生入口**（`Build` 与 `RebuildErrorLine`，⛔ 别按行号找）
            //   —— `CLAUDE.md` §10 第 5 条：多入口的状态要在**每个入口**都设对 ⇒ **两处都传 `Align.Center`**。
            Txt(root, _error, ErrL, ErrT, ErrR, ErrB, ErrFontPx, Align.Center, "Error msg", QImpText);
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
