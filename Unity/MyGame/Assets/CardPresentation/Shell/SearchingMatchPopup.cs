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
        //    ⚠️ 本窗的**背板命中区必须低于 Cancel**（2026-10-04 起走 `MenuDraw.ShadeHit`，档 = `QSr`）。
        public const int QSr = 3130, QSr1 = 3131, QSr2 = 3132, QSrText = 3133;
        /// <summary>窗内命中区那一档（`Cancel` 钮）。🔴 **2026-10-04（A47 接线批）：`QSrHitBackdrop`(3134) 已删** ——
        /// 它原来是「内容档 − 1」的写法，按规矩是错的：压暗层的命中区必须落在**压暗层自己那一档**
        /// （`QSr` = 3130），且严格低于本窗内容命中区最低档（本常量 = 3135）。现在那条路走
        /// `MenuDraw.ShadeHit`（档传 `QSr` / `QSrHit`）。判据 → `MenuDraw.ShadeHit` 与
        /// `资料/待办判据_阶段二与联机.md` §A25·补（一）。</summary>
        public const int QSrHit = 3135;

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

        // ---- 🆕 2026-10-03：`Cog` 的**自转**（原版那条 legacy `Animation` + clip `SearchingOpponentCog`）----
        //
        // 判据（**逐键照抄，不是近似**）：
        //   `d:/2/新解包资源/assets_full/bundle_menus_assets_all/AnimationClip/AnimationClip_-6716633893720174568.json`
        //   —— `m_EulerCurves[0].path = **"Cog"**` · `m_SampleRate 60` · `m_WrapMode 2(Loop)` ·
        //   **37 个键、Z 从 0 → 360°、总长 10.0166667s**（帧量化后 = 每档 +20°，补间 ≈25 帧 + 保持 ≈8 帧）。
        // ⚠️ 原来这里写着「没实现旋转（`ImageQuad` 没有『转自身』的口子）」—— **那句是错的**：
        //    `ImageQuad` 确实没有自转 API，但**宿主 Transform 有**（`CampaignTab.BuildLine` 早就在用
        //    `q.transform.localRotation`）⇒ 这一条一直是**能做的**，只是没做。
        static readonly float[] CogKeyT =
        {
            0f, 0.41666666f, 0.55f, 0.96666664f, 1.1166667f, 1.5333333f, 1.6666666f, 2.0833333f,
            2.2333333f, 2.65f, 2.7833333f, 3.2f, 3.3333333f, 3.75f, 3.9f, 4.3166666f, 4.45f, 4.8666667f,
            5f, 5.4333334f, 5.5666666f, 5.983333f, 6.1166666f, 6.5333333f, 6.6833334f, 7.1f, 7.233333f,
            7.65f, 7.7833333f, 8.2f, 8.35f, 8.766666f, 8.9f, 9.316666f, 9.466666f, 9.883333f, 10.016666f,
        };
        static readonly float[] CogKeyZ =
        {
            0f, 20f, 20f, 40f, 40f, 60f, 60f, 80f, 80f, 100f, 100f, 120f, 120f, 140f, 140f, 160f, 160f,
            180f, 180f, 200f, 200f, 220f, 220f, 240f, 240f, 260f, 260f, 280f, 280f, 300f, 300f, 320f,
            320f, 340f, 340f, 360f, 360f,
        };
        /// <summary>一圈的时长（`m_StopTime`；`m_WrapMode = 2` ⇒ 到点回绕）。</summary>
        public const float CogLoop = 10.016666f;

        float _cogTime;   /// <summary>Cog 自转的播放头（秒，0..CogLoop）。</summary>

        /// <summary>`Cog` 此刻该转到的角度（度）。**自检用** —— 它比的就是那条 clip 的键。
        /// 曲线是**逐段线性**的（原版是 60fps 采样的 legacy 曲线，`m_UseHighQualityCurve = 1` 但键之间仍是直线段）。</summary>
        public static float CogAngleAt(float t)
        {
            t = Mathf.Repeat(t, CogLoop);
            for (int i = CogKeyT.Length - 2; i >= 0; i--)
            {
                if (t < CogKeyT[i]) continue;
                float span = CogKeyT[i + 1] - CogKeyT[i];
                float f = span <= 0f ? 1f : Mathf.Clamp01((t - CogKeyT[i]) / span);
                return Mathf.Lerp(CogKeyZ[i], CogKeyZ[i + 1], f);
            }
            return CogKeyZ[0];
        }


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
            // 🔴 **2026-10-04（A47 接线批）订正档号 + 收口公共件**：`QSrHitBackdrop`(3134) 是
            //   「内容档 − 1」的写法；规矩是**压暗层的命中区落在压暗层自己那一档**（`QSr` = 3130），
            //   且严格低于本窗内容命中区最低档（`QSrHit` = 3135）⇒ 改走 `MenuDraw.ShadeHit`
            //   （判据 → 它的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            MenuDraw.ShadeHit(_root, new PxRect(0f, 0f, 1920f, 1080f), QSr, QSrHit, () => Cancel(), "BackdropHit");

            // 2) `Window` —— 🔴 **原版挂 `RectMask2D`**，而且 `Skull`/`Main Search message`/`Buttons`/「人少」那句
            //    全是**它的子件**（2026-09-24 找茬按 `m_Children` 真读出来的；原来挂在了弹窗根上）
            var win = MenuDraw.Node(_root, "Window", new PxRect(WinL, WinT, WinR, WinB));
            //    → `Generic Popup Background`（`40k_popup` 九宫格；**名字是原版的**，别让它落在默认的 `"Nine"`）
            MenuDraw.Nine(win, CardArt.MenuUi(ArtPopup), new PxRect(WinL, WinT, WinR, WinB),
                          PopupBorder, PopupTexW, PopupTexH, QSr, null, true, "Generic Popup Background");
            // 🆕 **2026-10-06（A94）：弹窗面板底图吸收点击**。判据 = 原版 prefab
            //   `Searching Oponent Popup > Window > Generic Popup Background` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（取消搜索那颗挂在压暗层 `Menu Dark Background` 上）⇒ 原版**什么都不做**。
            //   ⚠️ 原版这一层还有 `RectMask2D`（`Skull` 超出上下边被裁）—— 命中那一面它等价于截到 `Window` 框内，
            //   本矩形本来就是 `Window` 框，同值。
            MenuDraw.Absorb(_root, "AbsorbHit", new PxRect(WinL, WinT, WinR, WinB), QSr, QSrHit);
            //       → `Mask`（原版 `showGraphic = 0`）→ `Background fill`（`40k_popup_texture` Tiled）
            var mask = MenuDraw.Node(win, "Mask", new PxRect(MaskL, MaskT, MaskR, MaskB));
            MenuDraw.Tiled(mask, CardArt.MenuUi(ArtFill), new PxRect(MaskL, MaskT, MaskR, MaskB),
                           FillTilePx, QSr1, "Background fill");

            // 3) `Skull` + `Cog`（都染 col(1,1,1,0.176) —— 原版就是这么淡）
            //    ⚠️ `Skull` 的 rect（567.34²）**上下各超出 `Window` 约 58px**，原版靠 `Window` 的 `RectMask2D` 裁掉。
            //    🔴 **2026-10-03：现在真裁得住了** —— `MenuDraw.Rect` 补了**纵向 uv 裁剪**（原来只裁 x），
            //       这里把 `Window` 的矩形当 `clip` 传下去即可（原来只能出声说「露着」）。
            var dim = new Color(1f, 1f, 1f, 0.176f);
            var winClip = new PxRect(WinL, WinT, WinR, WinB);        // = 原版 `Window` 上那个 `RectMask2D`
            _skull = MenuDraw.Node(win, "Skull", new PxRect(SkullL, SkullT, SkullR, SkullB));
            MenuDraw.Rect(_skull, CardArt.MenuUi(ArtSkull), new PxRect(SkullL, SkullT, SkullR, SkullB),
                          "Skull", QSr2, dim, true, winClip);
            _cog = MenuDraw.Node(_skull, "Cog", new PxRect(SkullL, CogT, SkullR, CogB));
            MenuDraw.Rect(_cog, CardArt.MenuUi(ArtCog), new PxRect(SkullL, CogT, SkullR, CogB),
                          "Cog", QSr2, dim, true, winClip);

            // 4) `Main Search message`（原版 hAlign = **Center**）—— 打字机的目标
            _msg = MenuDraw.Text(win, new PxRect(MsgL, MsgT, MsgR, MsgB), "", Color.white,
                                 "Main Search message", 50f, QSrText);

            // 5) `Buttons`（VLG）→ `Generic UI Button`（`40K_button` col(.369,.894,.587,1)）+ `Button Text` "Cancel"
            var btn = MenuDraw.Node(win, "Buttons", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var gub = MenuDraw.Node(btn, "Generic UI Button", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var bgQ = MenuDraw.Rect(gub, CardArt.MenuUi(ArtButton), new PxRect(BtnL, BtnT, BtnR, BtnB),
                          "Bg", QSr2, new Color(0.369f, 0.894f, 0.587f, 1f), true);
            // 原版 Button Text hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(gub, new PxRect(BtnTxL, BtnTxT, BtnTxR, BtnTxB), "Cancel", Color.white,
                          "Button Text", 45f, QSrText);
            // 🆕 A17：原版 `Searching Oponent Popup>Window>Buttons>Generic UI Button` 是 SpriteSwap（普查 §块 5 第 26 行）
            MenuDraw.Hit(gub, "CancelHit", new PxRect(BtnL, BtnT, BtnR, BtnB), QSrHit, () => Cancel(),
                         bgQ, ArtButton);

            // 6) `Few players online message` —— **原版 `useFewPlayerMessage = 0` ⇒ 不画**（见文件头）
            Debug.Log("[Searching] `Few players online message` **没画** —— 原版 `useFewPlayerMessage` 两份 prefab 都是 **0**");
        }

        /// <summary>打开：照原版 `OnEnable` —— 文字清空（alpha 0 等价物）、从头开始打字。</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _typed = 0f;
            // 🆕 2026-10-03：`Cog` 的播放头也归零（原版 `OnEnable` → `Play()` ⇒ 那条 `Animation` 从头放）
            _cogTime = 0f;
            if (_cog != null) _cog.localRotation = Quaternion.Euler(0f, 0f, CogAngleAt(0f));
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

        /// <summary>🆕 **2026-10-03：联机等待态** —— 只显示这扇窗（含打字机），**不跑那 12 秒 bot 倒计时**
        /// （等的是**真人**，等到为止；「等不到就换 bot」那一支只给单机）。
        /// 判据 → `项目任务.md` §三 第 29 条 **A2**（原来 P2P 那条路上三扇模式窗**屏幕上什么都没有**）。
        /// ⚠️ 原版匹配真人时走的是**远端配置**的等待时长，本地没有那个值 ⇒ **「一直等」是我们的落地**。</summary>
        public void BeginNetWait(bool show = true)
        {
            if (show) Show();
            Searching = false;                    // ⚠️ false ⇒ `Tick` 那一支不跑倒计时
            SecondsLeft = 0f;
            Debug.Log("[Searching] **联机等待对手**：不跑 12 秒 bot 链（那一支只给单机）—— 一直等到对面也点 `Battle!`");
        }

        /// <summary>打字机 + 匹配倒计时。**`dt` 由调用方给**（批处理下没有帧循环 ⇒ 自检可以一次推进）。
        /// 🔴 **2026-09-24 订正**：原来这里写「转盘速度**没读**」——**是错的**，数据就在本地包里：
        /// `Skull` 上挂着一个 legacy `Animation`（`m_PlayAutomatically = 1`）+ clip
        /// **`SearchingOpponentCog`**（`AnimationClip_-6716633893720174568.json` · `m_Legacy=true` · `m_SampleRate=60`）——
        /// euler **Z 每档 +20°、共 37 个键、0 → 10.01667 秒走满 360°**、循环。
        /// ⚠️ 我们**没实现旋转**（`ImageQuad` 没有「转自身」的口子，得另做一个；**这一条是缺口，不是"查不到"**）。</summary>
        public void Tick(float dt)
        {
            // 🆕 2026-10-03：**`Cog` 自转**（原版 `Skull` 上那条 legacy `Animation` + clip `SearchingOpponentCog`
            //   —— `m_PlayAutomatically = 1`、循环）。⚠️ 它**独立于打字机与倒计时**（那两条各有各的 `if`）。
            _cogTime += dt;
            if (_cog != null) _cog.localRotation = Quaternion.Euler(0f, 0f, CogAngleAt(_cogTime));

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
