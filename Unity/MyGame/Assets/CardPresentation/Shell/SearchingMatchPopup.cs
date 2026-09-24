// SearchingMatchPopup.cs — 四扇「战斗入口」窗**共用的** `Searching Oponent Popup`
//   （原版 `SearchingMatchWindowDemo : GameWindow`；在本版里它是**宿主窗口根下的一个子树**、出厂 `act=0`）
//
// ============================ 出处（逐条实读）============================
// · **几何** = `资料/阶段二_战斗入口_原版规格.md` §二 A 的「`Searching Oponent Popup` 的内部」表
//   ＋ `py 工具/menu_rect.py bundle_menus_assets_all -4201586819119755918 --depth 6`（2026-09-24 复核）。
// · **行为** = `工具/_probe_deckinfo.py … --class SearchingMatchWindowDemo` 解出的**序列化字段**：
//     `timeBetweenSearchingLetters = 0.5` · `timeToShowNoPlayersMessage = 15` · **`useFewPlayerMessage = 0`**
//     · `closeOnESC = 1` · `type = 1 Popup` · `windowsPlacement = 15 Popup` · `extraScaleSmallScreen = 1.0`
// · **`OnEnable`**（`SearchingMatchWindowDemo__OnEnable.c`）：`searchingText.alpha = 0` ·
//   `notEnoughPlayersText` 的 GO `SetActive(false)` · `StopAllCoroutines()` → 起**打字机**协程
//   （`_TypeWriteEffect_d__14`，每字 `timeBetweenSearchingLetters` 秒）→ 注册 `MatchMakerManager.OnSearchCancelled`。
// · **`CancelSearchButtonOnClick` → `CancelSearch`**：找 `MatchMakerManager` 调 `CancelSearch`。
//
// 🔴 **`useFewPlayerMessage = 0`（练习窗与遭遇战窗【都是 0】）⇒「人少」那句【不显示】** ——
//    两份 prefab 里那句话的文案还不一样（短句 / 长句），但既然开关是 0，**照原版就不画**，别自己点亮它。
// 🔴 **两处图集不同**：`40k_popup`（九宫格 169,160,169,160 · 359×336）与 `40k_popup_texture`
//    （**Tiled** · 128×128 · `m_PixelsPerUnitMultiplier = 2.0` ⇒ **一格 64px**）——
//    这两个数是**工程里已有的两处先例**（`PromptPopup.FillTilePx` / `Battle/WaitBanner.cs:109`），照抄。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>「正在搜索对手」那个弹窗。**挂在宿主窗口根下**（照原版的层级），出厂是关着的。</summary>
    public class SearchingMatchPopup : MonoBehaviour
    {
        // 层：**高于**宿主窗自己的内容（`PracticeModePopup.QPr = 3100` · 活动窗 3120 档），**低于** `PromptPopup`（3140）
        // 🔴 **一层一个队列**（同队列「谁盖谁」不可控 —— 第一版把 6 层塞进一个 3130，
        //    整屏压暗的中心恰好是屏幕中心 ⇒ 一旦它赢，面板整块被压暗盖住、文字还在（2026-09-24 找茬抓到）。
        //    ⚠️ 本窗的**背板命中区必须低于 Cancel**（同 `LiveOpsEventWindow.QHitBackdrop` 那条）。
        public const int QSr = 3130, QSr1 = 3131, QSr2 = 3132, QSrText = 3133;
        public const int QSrHitBackdrop = 3134, QSrHit = 3135;

        // ---- 几何（原版逐节点，1920×1080 · 左上原点 · y 向下）----
        public const float ShadeL = -1327.30f, ShadeT = -746.18f, ShadeR = 3247.30f, ShadeB = 1826.18f;
        public const float WinL = 560f, WinT = 234.07f, WinR = 1360f, WinB = 685.93f;
        public const float MaskL = 570.40f, MaskT = 243.51f, MaskR = 1350.13f, MaskB = 676.13f;
        public const float SkullL = 668.33f, SkullT = 176.33f, SkullR = 1235.67f, SkullB = 743.67f;
        /// <summary>`Cog` 比 `Skull` 高 0.14px（原版两个节点各自摆过）—— 照抄，别对齐。</summary>
        public const float CogT = 176.19f, CogB = 743.81f;
        public const float MsgL = 610f, MsgT = 293.10f, MsgR = 1310f, MsgB = 441.30f;
        /// <summary>`Buttons`（VLG spacing 22.24 align=MiddleCenter）—— 只有一个子件，位置就落在它的中心。</summary>
        public const float BtnL = 720.83f, BtnT = 569.93f, BtnR = 1199.17f, BtnB = 644.93f;
        public const float BtnTxL = 733.52f, BtnTxT = 561.23f, BtnTxR = 1185.87f, BtnTxB = 652.43f;
        /// <summary>「人少」那句（`useFewPlayerMessage = 0` ⇒ **不画**，坐标留着备查）。</summary>
        public const float FewL = 593.05f, FewT = 460f, FewR = 1326.95f, FewB = 560f;

        public const string ArtPopup = "40k_popup";
        public const string ArtFill = "40k_popup_texture";
        public const string ArtSkull = "40K_icon_searching_skull";
        public const string ArtCog = "40K_icon_searching_cog";
        public const string ArtButton = "40K_button";
        /// <summary>`40k_popup_texture` 的平铺格（画布 px）= 128 ÷ `m_PixelsPerUnitMultiplier 2.0`。</summary>
        public const float FillTilePx = 64f;
        public static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);
        public const float PopupTexW = 359f, PopupTexH = 336f;
        /// <summary>九宫格两条边的贴合尺寸（贴图 489×107）。</summary>
        public const float ButtonTexW = 489f, ButtonTexH = 107f;

        // ---- 行为（原版序列化字段）----
        /// <summary>`timeBetweenSearchingLetters = 0.5`（每个字母之间的等待）。</summary>
        public const float LetterInterval = 0.5f;
        /// <summary>`timeToShowNoPlayersMessage = 15`（配合 `useFewPlayerMessage`，我们的 prefab 里是 0 ⇒ 不用）。</summary>
        public const float NoPlayersMessageDelay = 15f;
        /// <summary>**等多久没人就换 bot** —— 原版是
        /// `SearchOpponentManager.GetTimeToWaitForOpponent`：不能匹配真人时返回一个常量，
        /// 用 `工具/read_literal.py` 读出 `DAT_1834b3160` = **12.0**（能匹配真人那一支走远端配置，本地没有）。
        /// 轮询间隔 = `DAT_1834b2bb8` = **1.0 秒**。</summary>
        public const float WaitForOpponentSeconds = 12f;
        /// <summary>被逐字打出来的那句话（原版 `searchTextKey = Demo/DeckSelectionDemo/Searching`，本地无术语表 ⇒ 用它的英文原文）。</summary>
        public const string SearchingText = "Searching";

        /// <summary>点取消 / 点背板时回调（宿主窗口用来把状态复位）。</summary>
        public System.Action OnCancel;
        /// <summary>**匹配等满了**（= 该去打 bot 了）。宿主窗口在这里接「开战」。</summary>
        public System.Action OnSearchDone;

        Transform _root, _skull, _cog;
        Label _msg;
        float _typed;

        public bool IsShowing { get { return _root != null && _root.gameObject.activeSelf; } }
        /// <summary>正在匹配中（自检用）。</summary>
        public bool Searching { get; private set; }
        /// <summary>还剩几秒（自检用）。</summary>
        public float SecondsLeft { get; private set; }
        /// <summary>🔴 **原版是 `maxVisibleCharacters` 的【循环】，不是「打一遍就停」**：
        /// `_TypeWriteEffect_d__14__MoveNext.c` —— 起手 `cur = len-3`，每 0.5s `cur++`，
        /// `cur > len` 就绕回 `len-3`，**无限循环**（4 档、周期 2.0s）。我们靠**改文字**等价复现
        /// （`Label` 没有 `maxVisibleCharacters` 接口）。</summary>
        public int TypedChars
        {
            get
            {
                int len = SearchingText.Length;
                int n = Mathf.Max(0, Mathf.FloorToInt(_typed / LetterInterval));
                return Mathf.Clamp(len - 3 + (n % 4), 0, len);
            }
        }

        /// <summary>在宿主窗口根下建一个（出厂关着 —— 照原版 `act=0`）。`nodeName` 用原版的节点名。</summary>
        public static SearchingMatchPopup Attach(Transform hostRoot, string nodeName)
        {
            var go = new GameObject(nodeName);
            go.transform.SetParent(hostRoot, false);
            var p = go.AddComponent<SearchingMatchPopup>();
            p.Build();
            go.SetActive(false);
            return p;
        }

        void Build()
        {
            _root = transform;
            _root.localPosition = MenuDraw.Local(_root, 0.50f, 0.50f, 1919.50f, 1079.50f);

            // 1) 整屏压暗（原版 `Menu Dark Background`，rect 比屏幕大是为了盖住任何画幅）+ 点它 = 取消搜索
            //    （原版这个 GO 上挂的就是 `BackgroundCloseButton`）
            MenuDraw.Rect(_root, CardArt.Solid(),
                          new PxRect(ShadeL, ShadeT, ShadeR, ShadeB), "Menu Dark Background", QSr,
                          new Color(0f, 0f, 0f, 0.773f));
            MenuDraw.Hit(_root, "BackdropHit", new PxRect(0f, 0f, 1920f, 1080f), QSrHitBackdrop, () => Cancel());

            // 2) `Window` —— 🔴 **原版挂 `RectMask2D`**，而且 `Skull`/`Main Search message`/`Buttons`/「人少」那句
            //    全是**它的子件**（2026-09-24 找茬按 `m_Children` 真读出来的；原来挂在了弹窗根上）
            var win = MenuDraw.Node(_root, "Window", new PxRect(WinL, WinT, WinR, WinB));
            //    → `Generic Popup Background`（`40k_popup` 九宫格；**名字是原版的**，别让它落在默认的 `"Nine"`）
            MenuDraw.Nine(win, CardArt.MenuUi(ArtPopup), new PxRect(WinL, WinT, WinR, WinB),
                          PopupBorder, PopupTexW, PopupTexH, QSr, null, true, "Generic Popup Background");
            //       → `Mask`（原版 `showGraphic = 0`）→ `Background fill`（`40k_popup_texture` Tiled）
            var mask = MenuDraw.Node(win, "Mask", new PxRect(MaskL, MaskT, MaskR, MaskB));
            MenuDraw.Tiled(mask, CardArt.MenuUi(ArtFill), new PxRect(MaskL, MaskT, MaskR, MaskB),
                           FillTilePx, QSr1, "Background fill");

            // 3) `Skull` + `Cog`（都染 col(1,1,1,0.176) —— 原版就是这么淡）
            //    ⚠️ `Skull` 的 rect（567.34²）**上下各超出 `Window` 约 58px**，原版靠 `Window` 的 `RectMask2D` 裁掉；
            //    我们的 `MenuDraw.Rect(..., clip:)` **只裁 x 不裁 y**（已知缺口）⇒ 这里会露出去 58px，**出声**。
            var dim = new Color(1f, 1f, 1f, 0.176f);
            _skull = MenuDraw.Node(win, "Skull", new PxRect(SkullL, SkullT, SkullR, SkullB));
            MenuDraw.Rect(_skull, CardArt.MenuUi(ArtSkull), new PxRect(SkullL, SkullT, SkullR, SkullB),
                          "Skull", QSr2, dim, true);
            _cog = MenuDraw.Node(_skull, "Cog", new PxRect(SkullL, CogT, SkullR, CogB));
            MenuDraw.Rect(_cog, CardArt.MenuUi(ArtCog), new PxRect(SkullL, CogT, SkullR, CogB),
                          "Cog", QSr2, dim, true);
            Debug.Log("[Searching] `Skull` 上下各 58px 超出面板 —— 原版由 `Window` 的 `RectMask2D` 裁掉，"
                      + "我们的 clip **只裁 x**（`MenuDraw.Rect` 的已知缺口）⇒ 这里露着");

            // 4) `Main Search message`（原版 hAlign = **Center**）—— 打字机的目标
            _msg = MenuDraw.Text(win, new PxRect(MsgL, MsgT, MsgR, MsgB), "", Color.white,
                                 "Main Search message", 50f, QSrText);

            // 5) `Buttons`（VLG）→ `Generic UI Button`（`40K_button` col(.369,.894,.587,1)）+ `Button Text` "Cancel"
            var btn = MenuDraw.Node(win, "Buttons", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var gub = MenuDraw.Node(btn, "Generic UI Button", new PxRect(BtnL, BtnT, BtnR, BtnB));
            MenuDraw.Rect(gub, CardArt.MenuUi(ArtButton), new PxRect(BtnL, BtnT, BtnR, BtnB),
                          "Bg", QSr2, new Color(0.369f, 0.894f, 0.587f, 1f), true);
            // 原版 Button Text hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(gub, new PxRect(BtnTxL, BtnTxT, BtnTxR, BtnTxB), "Cancel", Color.white,
                          "Button Text", 45f, QSrText);
            MenuDraw.Hit(gub, "CancelHit", new PxRect(BtnL, BtnT, BtnR, BtnB), QSrHit, () => Cancel());

            // 6) `Few players online message` —— **原版 `useFewPlayerMessage = 0` ⇒ 不画**（见文件头）
            Debug.Log("[Searching] `Few players online message` **没画** —— 原版 `useFewPlayerMessage` 两份 prefab 都是 **0**");
        }

        /// <summary>打开：照原版 `OnEnable` —— 文字清空（alpha 0 等价物）、从头开始打字。</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _typed = 0f;
            // 原版**不清空文字**：起手把可见字数设成 `len-3`（我们是把文字截到那一截）
            if (_msg != null) _msg.SetText(SearchingText.Substring(0, Mathf.Max(0, SearchingText.Length - 3)));
            Debug.Log("[Searching] 开始搜索对手（`maxVisibleCharacters` 循环 · 每 " + LetterInterval + "s 进一格；"
                      + "原版 `OnEnable` → `_TypeWriteEffect`）");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>点 `Cancel` / 点背板 —— 原版走 `MatchMakerManager.CancelSearch`（我们本地没有那个管理器）。</summary>
        public void Cancel()
        {
            Debug.Log("[Searching] 取消搜索（原版 `CancelSearchButtonOnClick → MatchMakerManager.CancelSearch`）");
            Searching = false;
            SecondsLeft = 0f;
            Hide();
            if (OnCancel != null) OnCancel();
        }

        /// <summary>**开始匹配** —— 照原版 `MatchMakerManager.StartMatch`：显示这扇窗、起打字机、等 12 秒。
        /// 🔴 **等待秒数与轮询间隔都是原版值**（见 `WaitForOpponentSeconds` 的注释）——
        ///    「等不到真人就打 bot」正是原版**离线时**自己的走法，不是我们编的。</summary>
        /// <param name="show">要不要把这扇弹窗**显示出来**。排位那条路走的是
        /// **全屏** `SearchingOpponentWindow`（本地入口是我们定的）⇒ 那边传 `false`：
        /// 倒计时还是这一份（**别写两遍**），只是画面由全屏那扇负责。</param>
        public void BeginSearch(bool show = true)
        {
            if (show) Show();
            Searching = true;
            SecondsLeft = WaitForOpponentSeconds;
            Debug.Log("[Searching] 匹配开始：最多等 " + WaitForOpponentSeconds + "s（原版 `GetTimeToWaitForOpponent` 的"
                      + "「不能匹配真人」分支 = 12）；轮询间隔原版 1s");
        }

        /// <summary>打字机 + 匹配倒计时。**`dt` 由调用方给**（批处理下没有帧循环 ⇒ 自检可以一次推进）。
        /// 🔴 **2026-09-24 订正**：原来这里写「转盘速度**没读**」——**是错的**，数据就在本地包里：
        /// `Skull` 上挂着一个 legacy `Animation`（`m_PlayAutomatically = 1`）+ clip
        /// **`SearchingOpponentCog`**（`AnimationClip_-6716633893720174568.json` · `m_Legacy=true` · `m_SampleRate=60`）——
        /// euler **Z 每档 +20°、共 37 个键、0 → 10.01667 秒走满 360°**、循环。
        /// ⚠️ 我们**没实现旋转**（`ImageQuad` 没有「转自身」的口子，得另做一个；**这一条是缺口，不是"查不到"**）。</summary>
        public void Tick(float dt)
        {
            if (_msg != null)
            {
                int before = TypedChars;
                _typed += dt;                       // ⚠️ 不封顶：原版是**无限循环**，不是打完就停
                int now = TypedChars;
                if (now != before) _msg.SetText(SearchingText.Substring(0, now));
            }
            if (!Searching) return;
            SecondsLeft -= dt;
            if (SecondsLeft > 0f) return;
            Searching = false;
            SecondsLeft = 0f;
            Debug.Log("[Searching] 等满了 " + WaitForOpponentSeconds + "s，没等到真人 ⇒ 转 bot 对局"
                      + "（原版 `_WaitForHumanOpponent → ChangeToBotBattle → StartBotBattle`）");
            Hide();
            if (OnSearchDone != null) OnSearchDone();
        }

        /// <summary>当前打出来的那截字（自检用）。</summary>
        public string TypedText { get { return SearchingText.Substring(0, TypedChars); } }
    }
}
