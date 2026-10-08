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
//    ⚠️ **2026-10-17（B27）补一句更硬的话**：本地**5 份** `SearchingMatchWindowDemo` 实例
//    （四扇战斗入口窗各一份 + 一份变体）**5/5 都是 0**（逐份读过，清单见下面 A925 那一节）
//    ⇒ 那行字**原版一次也没显示过**。
//
// 🆕 **2026-10-17（B27·A925）**：本窗那行 `Main Search message` **同时也是联机层的「提示行」的消费方**
//    （大厅阶段对面掉线 / 离开 / 回来时，`NetMatchmaking.OnHint` 说的那句话落在这一行上）——
//    判据、为什么接在**这一行**而不是别的节点、以及**这条链是我们的口径（不是复刻）**，
//    全部写在下面 `ShowHint` 上面那一节里。
// 🔴 **两处图集不同**：`40k_popup`（九宫格 169,160,169,160 · 359×336）与 `40k_popup_texture`
//    （**Tiled** · 128×128 · `m_PixelsPerUnitMultiplier = 2.0` ⇒ **一格 64px**）——
//    这两个数是**工程里已有的两处先例**（`PromptPopup.FillTilePx` / `Battle/WaitBanner.cs` 的 `FillTilePx`），照抄。
using UnityEngine;
using CardPresentation.Net;      // 🆕 2026-10-17（B27·A925）：提示行那个口在 `NetMatchmaking` 里

namespace CardPresentation
{
    /// <summary>「正在搜索对手」那个弹窗。**挂在宿主窗口根下**（照原版的层级），出厂是关着的。</summary>
    public class SearchingMatchPopup : MonoBehaviour
    {
        // 层：**高于**宿主窗自己的内容（`PracticeModePopup.QPr = 3100` · 活动窗 3120 档），**低于** `PromptPopup`（3140）
        // 🔴 **一层一个队列**（同队列「谁盖谁」不可控 —— 第一版把 6 层塞进一个 3130，
        //    整屏压暗的中心恰好是屏幕中心 ⇒ 一旦它赢，面板整块被压暗盖住、文字还在（2026-09-24 找茬抓到）。
        //    ⚠️ 本窗的**背板命中区必须低于 Cancel**（2026-10-04 起走 `MenuDraw.ShadeHit`，档 = `QSr`）。
        // ✅ **2026-10-11（A252）可见性收窄**：这行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   留 `public` 的那个有实测引用（脚本扫全工程 301 个 `.cs`、剔注释、剔本文件）：
        //   `QSr` 1 处（`Editor/MainMenuScene.cs` 的 `CheckShadeRule`/`CheckAbsorbRule` 档参）；
        //   `QSr1`/`QSr2`/`QSrText` **外部引用 = 0** ⇒ 回 `const`。
        //   ⚠️ 本类**没有嵌套类型、也没有子类**（`class X : SearchingMatchPopup` 全 0）⇒ 收窄安全。
        public const int QSr = 3130;        // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（1 处）
        const int QSr1 = 3131;              // 3131 面板底
        const int QSr2 = 3132;              // 3132 面板上的第二层（齿轮 / 骷髅）
        const int QSrText = 3133;           // 3133 文字层
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
            //    🆕 **2026-10-17（B27·A925）**：这一行**同时也是联机提示行**（见 `ShowHint` 上面那一节）
            //    ⇒ 参数补全成**原版那一颗 TMP 自己的**（`bundle_menus_assets_all/MonoBehaviour/
            //    MonoBehaviour_-6561808484806384939.json`，就是本窗 `searchingText` 指的那个 PathID）：
            //      `m_fontSize 50` · `m_enableAutoSizing 1` · `m_fontSizeMin 4` · `m_fontSizeMax 50`
            //      · `m_fontSizeBase 36` · `m_TextWrappingMode 1`（折行开）· `m_margin (0,0,0,0)`
            //      · `m_HorizontalAlignment 2`（Center）· `m_VerticalAlignment 512`（Middle）。
            //    ⇒ 折行宽 = 本框宽 **700**（`MsgR - MsgL` = 1310 − 610；margin 全 0 ⇒ 框宽就是折行宽）、
            //      自适应 **4~50**、base **36**。
            //    ⚠️ 原来只传了 `50f` ⇒ **既没折行、也没自适应**（九个字母的 "Searching" 看不出来，
            //      可一旦往这一行放一句人话就会溢出框外）。
            //    ⚠️ 对原来那台打字机**行为不变**："Searching" 在 50px 下只有约 225px 宽（< 700）、
            //      一行高 50（< 148）⇒ 自适应收敛到上限 = **还是 50px**（`SetAutoFitBox` 的搜索起点是
            //      base、收敛结果是「装得下的最大号」，上限就是原来那个 50）。
            _msg = MenuDraw.Text(win, new PxRect(MsgL, MsgT, MsgR, MsgB), "", Color.white,
                                 "Main Search message", 50f, QSrText,
                                 wrapPx: MsgR - MsgL, autoMinPx: 4f, autoMaxPx: 50f, autoBasePx: 36f);

            // 5) `Buttons`（VLG）→ `Generic UI Button`（`40K_button` col(.369,.894,.587,1)）+ `Button Text` "Cancel"
            var btn = MenuDraw.Node(win, "Buttons", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var gub = MenuDraw.Node(btn, "Generic UI Button", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var bgQ = MenuDraw.Rect(gub, CardArt.MenuUi(ArtButton), new PxRect(BtnL, BtnT, BtnR, BtnB),
                          "Bg", QSr2, new Color(0.369f, 0.894f, 0.587f, 1f), true);
            // 原版 Button Text hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(gub, new PxRect(BtnTxL, BtnTxT, BtnTxR, BtnTxB), "Cancel", Color.white,
                          "Button Text", 45f, QSrText);
            // 🆕 A17：原版 `Searching Oponent Popup>Window>Buttons>Generic UI Button` 是 SpriteSwap（普查 §块 5 第 26 行）
            // 🆕 **2026-10-18（A1053）**：**命中区 = 可射线件的并集** —— 这颗钮的子树里底 `40K_button`（478.34×75）
            //   与 **`Button Text`（452.34×91.20，`RT=1`）两颗都吃射线**，文字**上下各凸 8.1**、左右内缩
            //   ⇒ 并集 = **478.34 × 91.20**（⛔ 不是按钮那 75 高）。
            //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "Searching Oponent Popup"
            //   --depth 8 --substr "Generic UI Button"`（实读 `478.34 x 91.20`）；
            //   ⚠️ 我们画的那两颗（`BtnL..BtnB` / `BtnTxL..BtnTxB`）与它**逐位同矩形同偏移**
            //   ⇒ 并集 = 「按钮的 x 两边 + 文字的 y 两边」（文字比按钮窄 ⇒ 左右两边由按钮定）。
            MenuDraw.Hit(gub, "CancelHit", new PxRect(BtnL, BtnTxT, BtnR, BtnTxB), QSrHit, () => Cancel(),
                         bgQ, ArtButton);

            // 6) `Few players online message` —— **原版 `useFewPlayerMessage = 0` ⇒ 不画**（见文件头）
            Debug.Log("[Searching] `Few players online message` **没画** —— 原版 `useFewPlayerMessage` 两份 prefab 都是 **0**");
        }

        /// <summary>打开：照原版 `OnEnable` —— 文字清空（alpha 0 等价物）、从头开始打字。</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _typed = 0f;
            // 🆕 2026-10-17（B27·A925）：**新的一次搜索 ⇒ 上一局那句提示作废**（否则台面上会挂着
            //   上一次掉线那句「对面掉线了…」——说错话）。⚠️ 只清**本窗**这一份；
            //   `NetMatchmaking` 那一份由它的 `Reset()` 清（`TryStart` 会调）。
            _hint = null;
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

            if (_msg != null && _hint == null)
            {
                int before = TypedChars;
                _typed += dt;                       // ⚠️ 不封顶：原版是**无限循环**，不是打完就停
                int now = TypedChars;
                if (now != before) _msg.SetText(SearchingText.Substring(0, now));
            }
            // 🆕 2026-10-17（B27·A925）：**提示还在那行字上 ⇒ 打字机歇着**
            //   （不然每 0.5 秒就把玩家正要读的那句话顶回「Sear…」—— 见 `ShowHint` 那一节）。
            //   ⚠️ 连着 `_typed` 一起停（播放头**冻在**提示出现那一刻），收回提示之后**从原处接着打** ——
            //   不是「提示期间偷偷往前跑、收回来时跳一格」。
            //   ⚠️ `Cog` 自转**不受这条闸影响**（它在上面，与打字机/倒计时各走各的 `if`）。
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

        // ==================================================================
        //  🆕 2026-10-17（B27·A925）：**联机「提示行」在这一扇窗上的消费方**
        //
        //  账 `A925`：`NetMatchmaking.LastHint` / `OnHint` 是**留好的口**（B23·A902 加的），
        //  但**一个消费方都没有** ⇒ 大厅阶段那几句人话（对面掉线 / 离开 / 回来）只活在日志与自检里，
        //  玩家看得到的**只有弹窗**（`NetRuntime.Notice`）+ 设置窗联机页那行 `StatusText`。
        //
        //  🔴 **原版判据（2026-10-17 现读，逐条；这就是「那行字」的答案）**：
        //   · 本窗 = 原版 `SearchingMatchWindowDemo`，全量反编译里它**只有两个** TMP 文本节点
        //     （字段表 `d:/2/tools/il2cpp_out/dump.cs:108646-108695`：`searchingText`(0x90) /
        //      `notEnoughPlayersText`(0xA0)）：
        //       ① **`Main Search message`** = `searchingText`：`m_text = "Searching"`（键
        //          `Demo/DeckSelectionDemo/Searching`；本地无词条表 ⇒ 用英文原文，同 B13 先例），
        //          由 `SearchingMatchWindowDemo._TypeWriteEffect_d__14__MoveNext.c` **无限循环**打字
        //          （起手 `len-3`，每 `timeBetweenSearchingLetters = 0.5s` 进一格，`cur > len` 绕回）
        //          —— **原版从头到尾不改这行字**。
        //       ② **`Few players online message`** = `notEnoughPlayersText`：由
        //          `_ShowPlayersMessage_d__15__MoveNext.c` 等 `timeToShowNoPlayersMessage`（= **15**）秒
        //          后 `SetActive(true)` + `DOFade` 淡入 —— 但它**被 `useFewPlayerMessage` 挡着**：
        //          本地 5 份实例（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`
        //          的 `MonoBehaviour_-7876346491111824683` /
        //          `MonoBehaviour_-4104094843614359207` / `MonoBehaviour_2027333971220400048` /
        //          `MonoBehaviour_3238362926157418300` / `MonoBehaviour_6077665188785245554`）
        //          **5/5 都是 `useFewPlayerMessage = 0`** ⇒ **原版一次也没显示过那行字**。
        //   · ⇒ **原版没有「一条会变的搜索状态行」**。它报这类事（匹配出错 / 连接断）用的是**弹窗**：
        //     `Everguild.MatchMakerManager__MatchMakingError.c` → `WindowsManager.ShowPopUp(文案, …, 按钮)`
        //     （键走 `I2_Loc_LocalizedString`，本地无词条表），按钮回调
        //     `MatchMakerManager__OnMatchMakingErrorScreenCloseButtonPress.c` = `HidePopUp` + `ShowLoading`；
        //     搜索阶段连接断那一条更早查过（B23 报告 §①：`SearchOpponentManager.CancelSearchForDisconnect`
        //     = 弹窗 + 撤搜索）—— **那一条我们早就有**（`NetRuntime.Notice`）。
        //
        //  ⇒ 🔴 **接在 `Main Search message` 上 —— 这是【我们的口径】、不是复刻**（铁律 3，如实标）：
        //     · **why this line**：搜索一旦被撤（对面掉线/离开），这行还在打「Searching」就是**说假话**
        //       （红线：不许静默 / 不许说错话）⇒ 这行字正是该改口的那一行；
        //     · **why not `Few players online message`**：那一个原版 5/5 都关着，点亮它 = 把原版**关着**
        //       的功能打开（而且它说的是「人少」，语义也对不上「对面掉线了」）。
        //  ⚠️ 那一行字的**几何 / 字体参数全部照原版 prefab 读**（`Build()` 里那颗 TMP：
        //     `MonoBehaviour_-6561808484806384939.json`），**没有一个是猜的**。
        //  ⛔ **排位那条路（全屏 `Shell/SearchingOpponentWindow.cs`）没接** —— 那扇窗只有 `Title`
        //     （437.76×50 · **无 auto**）、两个 `Player Name` 与一颗 `Cancel Match`，
        //     **没有任何一行放得下一句状态话**（`阶段二_多人界面_原版规格.md` §6 的逐节点表）
        //     ⇒ 如实留在报告里，**不硬塞一行假的进去**。
        // ==================================================================

        /// <summary>此刻那行字显示的是不是「提示」（`null` = 走原版那台打字机）。自检读它。</summary>
        public string HintText { get { return _hint; } }
        string _hint;

        // ==================================================================
        //  🔴 **2026-10-19（`A1083`）：这一行的字数预算重算 —— 按【字形宽度】算，不再按【字符数】一刀切**
        //
        //  **改前**：`text.Length > 40`。那 40 是这么算出来的（原注释）：「框 700×148（原版
        //    `Main Search message` 的 rect）· 自适应 4~50 ⇒ **50px 时一行约 14 字 × 约 3 行 ≈ 41 字**，
        //    留余量取 40」。⚠️ 那个「14 字」= 700 ÷ 50 ⇒ 它量的是**中文（全宽）**的进位数
        //    ⇒ **这条账只对中文档成立**（原注释末尾那句「拉丁字更窄 ⇒ 实际更多」当时没落成数）。
        //
        //  🔴 **为什么今天必须把它算准**：第六会话 `P6d` 把走线文案改成「**收侧先钳、再取词**」之后，
        //    走到这一行的整句**变成了我们自己的文案**（`Core/Loc.cs` 的 EN 列），而 EN 列比 ZH 列长得多
        //    ⇒ 英文档下**每一条**都 `LogWarning`。一个「只会喊、且喊得不准」的告警 = 谁都开始忽略它
        //    （与「静默」是同一个病的两面）。
        //
        //  **重算 = 与那 40 用【同一套算法】，只把「中文字宽」换成「拉丁字宽」**（⛔ 没有另立一套）：
        //    · 中文字宽 = **1 em**（全宽）⇒ 50px 时 700 ÷ 50 = **14 字/行** × 3 行 = 42 ⇒ 取 **40**（留约 5%）；
        //    · 拉丁字宽 ≈ **0.5 em**（`Label` 用的是 `NotoSerifCJK-Regular SDF`；⚠️ `m_fontSize` **就是 em**，
        //      ⛔ 别按 0.72 折 —— 那是本工程踩过的坑）⇒ 50px 时 700 ÷ 25 = **28 字/行** × 3 行 = **84**
        //      ⇒ 按同一比例（42 → 40，约 5%）留余量 ⇒ **80**。
        //    ⇒ 预算统一成「**半宽字位**」：中文/日文/全角标点一个字 = **2 位**，拉丁/数字/半角标点 = **1 位**，
        //      上限 **80 位**。**中文档的上界与改前等价**（40 个全宽字 = 80 位）；含半角字符的串
        //      （空格 / `Battle!` / 反引号 / `—`）会**略微宽松** —— 但方向是单向的：
        //      `位 ≤ 2 × 字符数` ⇒ **「新判据会喊」必然推出「旧判据也会喊」** ⇒ **中文档一条新告警都不会多**，
        //      只有 41~42 字那种**擦边**的串可能从「喊」变成「不喊」（= 少喊一句，是**选定的方向**）。
        //      ⇒ `Editor/NetSelfTest.cs` 那条按 `HintLineMaxChars`（= 40）断中文档的检查**一字不用改**。
        //    ⛔ **为什么不「按当前语档」分支**（`Loc.Current == Chinese ? 40 : 80`）：词条表**只有中、英两列**
        //      （其余 10 档一律**回退英文**，见 `Loc` 文件头）⇒ 「是不是中文档」分不出字形宽度；
        //      而且一句话里可以**中英混排**（`{0}` 里塞的就是对端转述的那一段）⇒ **只有看字本身才准**。
        //      顺带也是躲开「别按语档写死」那条纪律。
        //    ⛔ **为什么选「改预算」而不是「改截断」**：这一行的整句本来就是「告诉玩家刚才发生了什么」，
        //      截了 = **把该玩家看的话吃掉**；而「超限」只是个**诊断阈值** ⇒ 正确的修法是**把阈值算准 + 照旧出声**。
        //
        //  🔴 **英文档下最长那条：实测 140 字**（2026-10-19 逐条算过；`SetHint` 是私有的 ⇒ 全仓
        //    **唯一**写口就是 `NetMatchmaking` 那三处，下面这张表就是全部能走到本行的句子）：
        //      | 路径（`NetMatchmaking`）                          | EN 字符数 | 中文档字符数 |
        //      | `_started` 时掉线 `DeferToBattle{PeerLostFrag}`    |   106     |    29       |
        //      | `_started` 时离开 `DeferToBattle{PeerLeftFrag+body}`| **140 ← 最长** | 42 ~ 70 |
        //      | 未开局掉线 `PeerLostHint{tail}`                    | 110 ~ 124 | 36 ~ **40** |
        //      | 未开局离开 `PeerLeftHint{body, tail}`              |   105     | 35 ~ 63     |
        //      | `Reset` 后对面回来 `LobbyRestored`                 |    91     |    39       |
        //      （`body` = 对端转述那一段，收侧先 `NetSession.ClampPeerText` **钳到 40** 再取词 ⇒ 上界 40；
        //        `tail` = `Lobby/MatchRevoked`(35/10) 或 `Lobby/NotMatchingThisGame`(49/14)。）
        //  ⇒ ✅ **改完「英文档那几条」仍然超**（140 > 80，英文档**一条都不止 80 位**）—— **这是如实结论、
        //    不是没改完**：阈值算准之后这条告警的含义才明确 =「**这一行在满字号（50px）下装不下它**」，
        //    而不是原来那个「拿中文算法去量英文文本」的假告警。**照旧出声 + 照旧照画**（⛔ 不截断、⛔ 不静默）。
        //    ⚠️ **没查清的一条**（不许跑 Unity ⇒ 量不到，如实记着）：140 个英文字符在这个 700×148 里
        //    究竟被压到多少 px、那个字号还看不看得清 —— **没有渲染就没有读数**。
        // ==================================================================

        /// <summary>那一行字**放得下多少【半宽字位】**：中文 / 日文 / 全角标点一个字 = **2 位**，
        /// 拉丁 / 数字 / 半角标点 = **1 位**。**80 位** ≈ 中文 **40 字** ≈ 英文 **80 字**。
        /// 推导（与改前那 40 同一套算法、只换字宽）与「英文档最长实际 140 字」见上面那一节。</summary>
        public const int HintLineMaxWidth = HintLineMaxChars * 2;

        /// <summary>那一行字**放得下多少【汉字】**（= `HintLineMaxWidth ÷ 2`，**中文档**的上限）。
        /// 🔴 **名字与取值都保留**（`Editor/NetSelfTest.cs` 拿它当中文档的上限读；`SearchingOpponentWindow`
        /// 原来也读它）—— ⛔ 别改成别的语义。**判断「这一行会不会被压小」请用
        /// `HintLineWidth(text) > HintLineMaxWidth`**，⛔ 别再用 `text.Length > 40`（那只对中文档成立）。</summary>
        public const int HintLineMaxChars = 40;

        /// <summary>这句话在那一行上占**多少个半宽字位**（判据 / 推导见上面那一节）。
        /// **纯函数**（⛔ 不读 `Loc.Current` —— 看字本身，中英混排才算得对）。
        /// ⚠️ 「歧义宽度」的标点（`—` `–` `…` `·`、弯引号…）**按半宽（1 位）算** —— 这是**故意的**：
        ///    它们在中文字体里其实按全宽画，算 1 位会让阈值**略微宽松**（宁可少喊一句，
        ///    也不把中文档那条「40 字」的账推翻）。
        /// 🔴 代理对（`surrogate pair`，emoji 之类）**按一个全宽字算**，⛔ 别把低位当半宽再数一遍。</summary>
        public static int HintLineWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int w = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    w += 2; i++;
                }
                else w += WideGlyph(c) ? 2 : 1;
            }
            return w;
        }

        /// <summary>这个字符是不是**全宽**字形（东亚宽 / 全角区）—— `HintLineWidth` 用。
        /// 范围照 `Unicode East Asian Width = W/F` 那几段抄（含谚文、假名、汉字、全角 ASCII）。</summary>
        static bool WideGlyph(char c)
        {
            return (c >= 'ᄀ' && c <= 'ᅟ')     // 谚文字母
                || (c >= '⺀' && c <= '〾')     // 部首扩展 ~ 中日韩符号（含全角逗号句号）
                || (c >= 'ぁ' && c <= '㏿')     // 假名 ~ 中日韩兼容（含全角方括号式符号）
                || (c >= '㐀' && c <= '䶿')     // 扩展 A
                || (c >= '一' && c <= '鿿')     // 基本汉字
                || (c >= 'ꀀ' && c <= '꓏')     // 彝文
                || (c >= '가' && c <= '힣')     // 谚文音节
                || (c >= '豈' && c <= '﫿')     // 兼容汉字
                || (c >= '︰' && c <= '﹏')     // 中日韩兼容形式
                || (c >= '＀' && c <= '｠')     // 全角 ASCII
                || (c >= '￠' && c <= '￦');    // 全角符号
        }

        /// <summary>联机层要对玩家说一句（大厅阶段的掉线 / 离开 / 回来）⇒ **在那行字上说**，并**顶掉打字机**
        /// （理由与判据全文见本节头部那一段）。传空的 = 收回提示。
        /// 🔴 **只由 `NetMatchmaking.OnHint` 推**（`OnEnable` 订、`OnDisable` 摘）—— ⛔ 别在这儿自己判状态。
        /// 🔴 **超限只出声、⛔ 不截断**（`A1083`）：截了就是把该玩家看的话吃掉，而超限只是个诊断阈值。</summary>
        public void ShowHint(string text)
        {
            if (string.IsNullOrEmpty(text)) { ClearHint(); return; }
            int width = HintLineWidth(text);
            if (width > HintLineMaxWidth)
                Debug.LogWarning($"[Searching] 提示行那句话占 {width} 个半宽字位（{text.Length} 个字符），"
                               + $"超过这一行放得下的 {HintLineMaxWidth} 位（= 中文 {HintLineMaxChars} 字 / "
                               + "英文约 80 字；框 700×148 · 自适应 4~50px，与同族那颗 `Main Search message` 同一档）"
                               + "—— 会被压到比 50px 更小的字号，请把这一句写短"
                               + "（详细的那半句留给弹窗，两处本来就是两个口）：「" + text + "」");
            _hint = text;
            if (_msg != null) _msg.SetText(text);
            Debug.Log("[Searching] 提示行改口（顶掉打字机）：「" + text + "」");
        }

        /// <summary>收回提示 ⇒ **打字机接着打**（从当前进度继续，不从头来）。</summary>
        public void ClearHint()
        {
            if (_hint == null) return;
            _hint = null;
            Debug.Log("[Searching] 提示行收回 ⇒ 打字机接着打（原版 `_TypeWriteEffect` 那条无限循环）");
        }

        // ---- 订/摘 `NetMatchmaking.OnHint`（**只在真的显示着的时候**订）----
        //  🔴 为什么挂在 `OnEnable`/`OnDisable` 而不是 `Show()`/`Hide()`：`Show()` 可以**重复调**
        //     （实测 `Editor/MainMenuScene.cs:4713,4733` 就是 `BeginNetWait()` 之后再 `Show()` 一次），
        //     挂在 `Show()` 上会**订两次**（`OnHint` 是多播委托 ⇒ 同一条提示画两遍、还要摘两次）。
        //     ⛔ `OnDestroy` 也必须摘（不摘的话，窗销毁之后提示一来就 `MissingReferenceException`）。
        void OnEnable() { NetMatchmaking.OnHint += ShowHint; }
        void OnDisable() { NetMatchmaking.OnHint -= ShowHint; }
        void OnDestroy() { NetMatchmaking.OnHint -= ShowHint; }
    }
}
