// BoosterPackOpenWindow.cs — §三 第 29 条 **A7**：商店的「Booster Pack Open Window」（**开包窗**）
//
// ============================ 出处（唯一正本） ============================
// **几何/结构**：`资料/阶段二_商店_原版规格.md` **§五·三**（+ 本节下面那张「层 × 场景 × 出现条件」表）。
//   复核命令：`python 工具/menu_dump.py bundle_menus_assets_all "Booster Pack Open Window" --depth 9 --relative`
//             （绝对矩形 1920×1080 · 左上原点 · y 向下）
// **窗口字段**：MB `assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_9012570135841684515.json`
//   —— 原样抄进 `Create()`：`type = 0 (Fullscreen)` · `windowsPlacement = 5 (Canvas)` ·
//   `closeOnESC = 0` · `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` ·
//   `timeToShowHelpText = 10.0` · `useQuickOpenProtection = 1` · `closeAnimation = "Booster Window Close"`。
//   ⚠️ **这一扇是 `Canvas(5)` 不是弹窗 `Popup(15)`** —— 与 `BoosterInfoPopup` 的 `World(10)` 也不同，三扇各自实测、别互推。
// **行为**：**原版反编译方法体**（第一权威，`d:/2/tools/decomp_full/`）：
//   `BoosterPackOpenWindow__{Initialize,OnEnable,BoosterPackOpen,CardOpened,ToggleTextHelper,Update,CloseButtonClick}.c` ·
//   `CardInBoosterPack__{Initialize,ChangeState,UiColliderOnClick,TryHighlightCardback}.c` ·
//   `CardInBoosterPack._WaitForSpinAnimationEnd_d__26__MoveNext.c` · `BasicCardUI__{SetRawCardData,ToggleNewCardBadge,ToggleCarUpgradeIfNeeded}.c` ·
//   `BoosterPackBackground__Initialize.c`。逐条落地见下面每个方法的注释。
//
// ============================ 🔴 5 张卡的位姿：**从原版 clip 里读出来的** ============================
// `Booster Window Open`（0.617 s，`menus_assets_all` 的 `AnimationClip_5911697182262325120`）**末帧**：
//   · 5 张 `Booster Animation Parent/Cards/CardInBoosterPack UI n` 的 **localScale = 151**
//     （`m_ScaleCurves`，t=0.217 时是 157.5815、t=0.617 收到 151）；
//   · 它们的 **x = −730 / −360 / 0 / +360 / +730**、**y = 0**、**z = 129**（`m_FloatCurves`，t=0.617 那批单键）。
// `Booster Window Close` 里同样 5 条 scale=151（起手就是停住的位姿）⇒ **两处互证**。
// 换算到画布 px：`Booster Animation Parent` 的 `m_LocalScale = **0.876259982585907**`（RT 实测）
//   ⇒ 卡心 x = 960 + {−730,−360,0,360,730} × 0.87626 = **320.33 / 644.55 / 960.00 / 1275.45 / 1599.67**，
//     卡心 y = **540**（`Booster Animation Parent` 矩形 910,490→1010,590 的中心）。
//   ⇒ 卡单位 → px 的系数 `CardK = 151 × 0.87626 = **132.3153**`。
// 🔴 **原版那 5 张卡的 `m_SizeDelta` 是 2.5437×3.3686**（模板位、**动画驱动**）；卡本体是它子节点 `2DCard` 的
//   **2.0927×3.3313** —— 与 `CardView.Width/Height` **逐位相同**（那也正是工程里 `CardView` 的尺寸来源）。
//   ⇒ 画出来 **276.90 × 440.77 px**。**不要**拿 2.5437 去算（那是容器，不是卡）。
//
// ============================ 层 × 场景 × 出现条件（**每一格都有出处**） ============================
// | # | 层（原版节点名） | 矩形（根 1920×1080 · 左上原点） | 什么时候出现 |
// |---|---|---|---|
// | ① | `Booster pack Background` | −100,−100 → 2020,1180（**2120×1280**，四周出血 100） | 恒。`Image.m_Sprite = **0**`（运行期赋图）⇒ 原版画出来就是**一块纯色**（`m_Color` = 白）。有传奇 ⇒ `BoosterPackBackground.Initialize(true)` 把颜色换成 `thereIsALegendaryCardbackgroundColor` = **(1.51358,0.45948,0,1)** |
// | ② | `Booster Animation Parent` | 910,490 → 1010,590（100×100） | 恒（`scl 0.87626` · `z −202`） |
// | ③ | `Cards` | 同 ② | 恒（自有 `Canvas`：`m_OverrideSorting = 1` · `m_SortingOrder = 2` ⇒ **画在背景之上**） |
// | ④ | `CardInBoosterPack UI 1..5` | 动画末帧见上 | 恒 |
// | ④a | `Cardback Shadow SDF` | 2.92×3.81 卡单位 | `ChangeState(1)` ⇒ **翻之前** |
// | ④b | `Cardback` | 2.17×3.14 卡单位 | 同上（`ToggleCardBack(true)`） |
// | ④c | `2DCard`（卡面） | 2.0927×3.3313 卡单位 | **翻开之后** |
// | ④d | `Card Ready for level up` | 1.78×2.74 @ (0,+0.02)（父 = `CardUI`） | 出厂 **INACT**；翻开后 = `BasicCardUI.ToggleCarUpgradeIfNeeded` → `CardServices.CheckForCardUpgrade(card)` |
// | ④e | `New Card Badge`（+ 子 `Text`） | 1.21×0.39 @ (0.32,1.14)、锚 (0,0.5) | 出厂 active **但 `SetRawCardData` 一进来就关**；翻开后 = `ToggleNewCardBadge(GetOwnedCount(card) **== 1**)` |
// | ④f | `Ban Icon`（+ 子 `Banned Text`） | 锚 (.1,.5)-(.9,.5)、`sd (0,2.23)`、`preserveAspect` | **恒不显示**：`BasicCardUI.SetRawCardData` 末尾 `SetActive(bannedIcon, **false**)`，而翻牌链**从不** `ToggleBanned` |
// | ⑤ | `Tap to discover` | 1156.67,977 → 1865,1080 | **开窗先关**；停手 **10 s** 后淡入（`Update`） |
// | ⑥ | `Tap to close` | 同 ⑤ | **5 张全翻开**后出现（`CardOpened` 里 `cardsLeftToOpen` 归零 ⇒ `SetActive(true)` + `DOScale ×1.1` 0.4 s OutBack） |
// | ⑥b | `Tap to close/Collider` | −534.74,−204.55 → 3318.35,2027.98（**盖满整屏**） | 跟 ⑥ 一起 ⇒ **点哪儿都关** |
//
// ============================ 🔴 我们挑的 / 缺口（逐条出声，红线：不许静默失败） ============================
//   · **卡包内容是我们编的**：原版是服务端 drop table（`Booster Pack Ultramarines` SO 的
//     `dropTableItems` 指着 `DT Ultramarines All R2+` / `DT Ultramarines All` 两张**没导出来**的掉落表）。
//     我们按**同一份 SO 的出厂文案**定规则：「Contains 5 cards for the Sautekh army.」
//     + 「At least one of the cards is guaranteed to be Rare or better」
//     ⇒ **5 张 + 至少 1 张 rare/epic/legendary**，阵营按商品主图对（见 `ArmyOfArt`）。
//     🔴 **哪 5 张是我们挑的**（用 `System.Random(seed)` 定种子可复现；原版 `Initialize` 里那句 `ListExtensions.Shuffle` 是**无种子**的）。
//   · **翻牌动画没做**：原版是 `Content` 上播 `Booster Opening - Card {Idle,Open Normal,Rare,Legendary}`
//     （`CardInBoosterPack.ChangeState(3)` 里 `cardAnimation.CrossFade(..., 0.2f)`）—— 我们**直接换层**
//     （关卡背、开卡面），没有 spin / 没有 0.2 s 交叉淡入。**出声**。
//   · **相机抖动没做**：`contentByRarities[i].shakes` 那几条 `OverwriteCameraShakePreset`（rarity 2 延迟 0.65 幅度 5 ·
//     rarity 3 延迟 1.0 幅度 40 · rarity 4 两条：延迟 0 频率 0.25 + 延迟 1.15 幅度 20 方向 z 10 衰减 0.5）——
//     菜单层没有相机抖动设施。**出声**。
//   · **音效没做**：`clickSound` / `newCardSound` / `openSound` / `closeSound` / 两段音频 cue 全在远端音频表里，
//     本地没有 ⇒ **不播**（不拿别的音顶）。**出声**。
//   · **`useQuickOpenProtection`（60 s 内连开就加速，`CalculateReOpenProtectionDelay`）没做** —— 时间源在批处理里不可靠。**出声**。
//   · **`OpenCardbacks` 那条 clip 不接** —— 判据弱（见 `BoosterPackExporter.ClipNames` 的最后一条），用途未查清。
//   · **垂直对齐近似**：`Tap to discover` / `Tap to close` 两条 TMP 原版是 `m_VerticalAlignment = **8192 (Capline)**`
//     + `m_HorizontalAlignment = 4 (Right)`；我们统一按**框内居中**画（全工程口径）。
//     差约四分之一行高，**没实拍可对** ⇒ 标成近似。**字号 49.82 · 颜色 (0.5566,0.5566,0.5566,1) 是原版值**。
//   · **`New Card Badge` / `Banned Text` 的文案**：原版 prefab 里是**俄语占位**（`Новинка!` / `Запрещено`），
//     运行期由远端 I2 词条覆盖。按 `Daily Shop Tab` 的先例（葡语 `Atualiza em:` → 英文 `Refreshes in:`）
//     我们用英文并**记在这里**（`NewBadgeText` / `BannedText`）。
//   · **`Card Ready for level up` 的判据是我们的替身**：原版 `CardServices.CheckForCardUpgrade` 是服务端数据
//     ⇒ 用工程既有的 `CardProgress.CanUpgrade`（与「卡片详情窗」同一份）。⚠️ 我们的拥有数口径是**「给足」**
//     ⇒ 这一条**恒真**、5 张卡都会亮那对箭头（原版由服务端决定）。**如实标**。
//   · **`New Card Badge` 恒不显示**：判据 `GetOwnedCount(card) == 1`（这是你**第一张**）。我们的
//     `CardProgress.Owned` 默认就给足（≥ 卡组上限 + 9）⇒ 这一条**恒假**。层照建、如实说，**不是忘了做**。
//   · **粒子的位置/缩放是按推导来的**：原版把 `contentByRarities[i].particles` 实例化成 **卡的子物体**
//     （继承卡节点 `151 × 0.87626` 那个缩放）⇒ 我们给一个同比例的 `VfxAnchor`（`CardK / 108`）。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `BoosterPackOpenWindow`（商店买完卡包弹的那扇**开包窗**）。</summary>
    public class BoosterPackOpenWindow : GameWindow
    {
        // 队列档：**在全屏/弹窗那一整段（3100–3160）之上、`Tooltip`（3199）之下**。
        // 🔴 为什么落在这里：原版这一扇是 `windowsPlacement = 5 (Canvas)` = 挂在
        //    `2 - Canvas Holder Above upper bar` 上（**在顶栏之上**）。我们的壳把顶栏定死在 `QBar* = 3600~3604`
        //    （2026-09-28 那条口径：**全工程只有顶栏排在所有窗口之上**）⇒ 本窗**不越顶栏**，落在 3169–3197。
        //    这是一处**口子上的偏离**：原版它压在顶栏上、我们压在顶栏下（见 `项目任务.md` §〇 那条顶栏层序口径）。
        public const int QBase = 3170;
        const int QShade = QBase;                 // 3170 整屏背景（纯色）
        const int QCloseSurface = QBase - 1;      // 3169 「点哪儿都关」那一层（**在卡命中区之下** ⇒ 卡还能点）
        const int QCard = QBase + 1;              // 3171 起，**每张卡 5 档**（见 CardStride）
        const int CardStride = 5;                 //   +0 卡影SDF · +1 卡背 · +2 卡面 · +3 角标 · +4 命中区
        const int QHint = QCard + 5 * CardStride; // 3196 两段提示字
        const int QHintHit = QHint + 1;           // 3197 `Tap to close` 那个节点自己的命中区

        // ============================================================ 真值（MB / RT / clip）

        /// <summary>`Booster pack Background`（四周出血 100px）。</summary>
        static readonly PxRect BgR = new PxRect(-100f, -100f, 2020f, 1180f);
        /// <summary>常态背景色 = 那张 `Image` 的 `m_Color`（实读 `(1,1,1,1)`）。</summary>
        static readonly Color BgNormal = new Color(1f, 1f, 1f, 1f);
        /// <summary>有传奇时的背景色 = `BoosterPackBackground.thereIsALegendaryCardbackgroundColor`。
        /// ⚠️ 红分量 **&gt;1**（原版就是 HDR 值，不是抄错）。</summary>
        static readonly Color BgLegendary = new Color(1.513579f, 0.459480f, 0f, 1f);

        /// <summary>`Booster Animation Parent`（100×100，中心 = 屏心）。</summary>
        static readonly PxRect AnchorR = new PxRect(910f, 490f, 1010f, 590f);
        /// <summary>`m_LocalScale` 实测。</summary>
        const float AnchorScale = 0.876259982585907f;

        /// <summary>`Booster Window Open` 末帧：5 张卡的 x（父级局部单位）。</summary>
        static readonly float[] CardX = { -730f, -360f, 0f, 360f, 730f };
        /// <summary>`Booster Window Open` 末帧：5 张卡的统一 `localScale`。</summary>
        const float CardScale = 151f;
        /// <summary>卡单位 → 画布 px（= `CardScale × AnchorScale`）。**别在别处再乘**。</summary>
        public static float CardK { get { return CardScale * AnchorScale; } }     // 132.3153

        /// <summary>卡本体（`2DCard`）—— 与 `CardView.Width/Height` 逐位相同。</summary>
        const float CardW = 2.0927f, CardH = 3.3313f;
        /// <summary>`CardUI` 那个容器（**不是卡本体**，别拿它算卡面）。</summary>
        const float ContainerW = 2.5437f, ContainerH = 3.3686f;
        /// <summary>`Cardback` / `Cardback Shadow SDF` 的尺寸（卡单位）。</summary>
        const float BackW = 2.17f, BackH = 3.14f, BackSdfW = 2.92f, BackSdfH = 3.81f;
        /// <summary>`Card Ready for level up`：1.78×2.74 @ (0, +0.02)（父 = `CardUI`）。</summary>
        const float UpW = 1.78f, UpH = 2.74f, UpDy = 0.02f;
        /// <summary>`New Card Badge`：1.21×0.39 · 锚 (0,0.5) · `anchoredPosition (0.32, 1.14)`（父 = `CardUI`）。</summary>
        const float BadgeW = 1.21f, BadgeH = 0.39f, BadgeAx = 0.32f, BadgeAy = 1.14f;
        /// <summary>`Ban Icon`：锚 (.1,.5)-(.9,.5) ⇒ 宽 = 父宽 × 0.8；`sd (0, 2.23)`；`preserveAspect`。</summary>
        const float BanW = ContainerW * 0.8f, BanH = 2.23f;

        /// <summary>两段提示字的矩形与字号/颜色（原版 TMP 实测）。</summary>
        static readonly PxRect HintR = new PxRect(1156.67f, 977f, 1865f, 1080f);
        const float HintPx = 49.81999969f;
        static readonly Color HintColor = new Color(0.5566f, 0.5566f, 0.5566f, 1f);
        /// <summary>`Tap to discover/Text` 的出厂文本（**原版就是这句、英文**）。</summary>
        public const string DiscoverText = "Tap on the cards to discover";
        /// <summary>`Tap to close` 的出厂文本（**原版就是这句、英文**）。</summary>
        public const string CloseText = "Tap to close";
        /// <summary>`Tap to close/Collider`：盖满整屏那块（`NonDrawingGraphic`）的矩形。</summary>
        static readonly PxRect CloseSurfaceR = new PxRect(-534.74f, -204.55f, 3318.35f, 2027.98f);

        /// <summary>`timeToShowHelpText`（MB 原文 **10.0**）：静止这么久 ⇒ `Tap to discover` 淡入。</summary>
        public const float HelpDelay = 10f;
        /// <summary>悬停放大系数（`CardInBoosterPack.TryHighlightCardback` 里的 `x1.1` 常量，
        /// 从 `GameAssembly.dll` 的 `_DAT_1834b31f8` 读出 = **1.1**）。</summary>
        public const float HoverScale = 1.1f;
        /// <summary>悬停补间时长（同族常量 `_DAT_1834b2dc8` = **0.3 s**）。</summary>
        public const float HoverTime = 0.3f;

        // ---- 角标文案（**原版出厂是俄语占位**，见文件头）----
        /// <summary>`New Card Badge/Text` 的出厂原文 = `Новинка!`（俄语「新品」）。</summary>
        public const string NewBadgeFactoryText = "Новинка!";
        /// <summary>我们显示的（英文）。</summary>
        public const string NewBadgeText = "NEW";
        /// <summary>`Ban Icon/Banned Text` 的出厂原文 = `Запрещено`（俄语「禁止」）。</summary>
        public const string BannedFactoryText = "Запрещено";
        /// <summary>我们显示的（英文；**这一层恒不显示**，见文件头）。</summary>
        public const string BannedText = "BANNED";

        // ============================================================ 状态

        public int Page { get; private set; }
        public int Index { get; private set; }
        /// <summary>这一包装的是哪个阵营（空 = 全卡池；判据 → `ArmyOfArt`）。</summary>
        public string Army { get; private set; }

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static BoosterPackOpenWindow LastOpened { get; private set; }

        /// <summary>取不到的图（出声用 —— 红线：不许静默失败）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        /// <summary>这一包的 5 张卡（**我们摇的**，见文件头）。</summary>
        public CardDef[] Cards { get; private set; }
        /// <summary>哪几张已经翻开。</summary>
        public readonly bool[] Opened = new bool[5];
        /// <summary>还没翻开的张数（原版 `cardsLeftToOpen`，`CardOpened` 里自减）。</summary>
        public int CardsLeft { get; private set; }
        /// <summary>开窗到现在过了多少秒（原版 `Update` 里那个累加器）。</summary>
        public float TimeAlive { get; private set; }
        /// <summary>提示文字现在藏着吗（原版 `_helperTextHidden`）。</summary>
        public bool HelperHidden { get; private set; }

        /// <summary>自检用：每一格的节点 / 卡背 / 卡面 / 三个角标 / 命中区。</summary>
        public readonly Transform[] SlotNodes = new Transform[5];
        public readonly GameObject[] BackNodes = new GameObject[5];
        public readonly GameObject[] FaceNodes = new GameObject[5];
        public readonly GameObject[] UpBadges = new GameObject[5];
        public readonly GameObject[] NewBadges = new GameObject[5];
        public readonly GameObject[] BanIcons = new GameObject[5];
        public readonly WindowButton[] SlotHits = new WindowButton[5];
        /// <summary>每格的粒子挂点（带 `CardK / 108` 的缩放，见 `BuildSlot` ④g）。</summary>
        public readonly Transform[] VfxAnchors = new Transform[5];
        /// <summary>自检用：翻牌时真正播出去的那 4 个粒子 prefab 名（按稀有度）。</summary>
        public readonly List<string> PlayedFx = new List<string>();
        /// <summary>自检用：`Tap to discover` / `Tap to close` 两个节点。</summary>
        public Transform DiscoverNode { get; private set; }
        public Transform CloseNode { get; private set; }
        /// <summary>自检用：整屏那块「点哪儿都关」的命中区。</summary>
        public WindowButton CloseSurfaceHit { get; private set; }
        /// <summary>自检用：背景那张 quad（量颜色用）。</summary>
        public ImageQuad BgQuad { get; private set; }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        // ============================================================ 开

        public static BoosterPackOpenWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Booster Pack Open Window");
            var win = go.AddComponent<BoosterPackOpenWindow>();
            win.type = WindowType.Fullscreen;                  // 实证 type = 0
            win.placement = WindowsPlacement.Canvas;           // 实证 windowsPlacement = 5（**不是 10 / 15**）
            win.closeOnEsc = false;                            // 实证 closeOnESC = **0**
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

        /// <summary>开哪一件（照 `BoosterInfoPopup.Show` 那条先例：先 `OpenWindow` 再喂数据）。</summary>
        public void Show(int page, int index)
        {
            Page = page; Index = index;
            Build();
        }

        // ============================================================ 建

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            PlayedFx.Clear();
            TimeAlive = 0f;
            HelperHidden = true;                 // `OnEnable`：一进来把 `Tap to discover` 关掉
            for (int i = 0; i < 5; i++) { Opened[i] = false; SlotNodes[i] = null; BackNodes[i] = null;
                                          FaceNodes[i] = null; UpBadges[i] = null; NewBadges[i] = null;
                                          BanIcons[i] = null; SlotHits[i] = null; VfxAnchors[i] = null; }
            DiscoverNode = null; CloseNode = null; CloseSurfaceHit = null; BgQuad = null;

            var offers = ShopData.Offers(Page);
            if (offers == null || Index < 0 || Index >= offers.Length)
            {
                Debug.LogWarning("[BoosterPack] 商品下标越界（page=" + Page + " idx=" + Index + "）⇒ 不铺内容");
                return;
            }
            var o = offers[Index];
            Army = ArmyOfArt(o.Art);
            Cards = RollPack(Army, unchecked(Page * 1000 + Index));
            CardsLeft = Cards.Length;

            // ① 背景：**纯色块**（原版 `backgroundImage` 的 `m_Sprite = 0`）——
            //    有传奇就换成 `thereIsALegendaryCardbackgroundColor`。
            var hasLegendary = false;
            for (int i = 0; i < Cards.Length; i++) if (RarityInt(Cards[i]) == 4) hasLegendary = true;
            BgQuad = MenuDraw.Rect(transform, CardArt.Solid(), BgR, "Booster pack Background", QShade,
                                   hasLegendary ? BgLegendary : BgNormal);
            if (BgQuad == null)
                Debug.LogError("[BoosterPack] 背景没建出来（`CardArt.Solid()` 取不到？）—— 这一扇会没有底色");

            // ② `Booster Animation Parent`（`m_LocalScale = 0.87626`）+ ③ `Cards`
            // 🔴 **不要在节点上真的设那个 0.87626** —— 我们的摆放全走「绝对像素矩形」，
            //    而 `MenuDraw.Local` 是「世界坐标 − 父节点位置」（**没除父级缩放**）⇒ 父级一带缩放，
            //    子件的 localPosition 就不再等于那个差值，整排卡会一起错位。
            //    ⇒ 0.87626 **已经折进 `CardK`**（`CardX[i] * AnchorScale` 那一处），节点保持 scale 1。
            var anchor = MenuDraw.Node(transform, "Booster Animation Parent", AnchorR);
            var cards = MenuDraw.Node(anchor, "Cards", AnchorR);

            for (int i = 0; i < 5; i++) BuildSlot(cards, i);

            // ⑤ `Tap to discover`（原版**开窗是关着的**）
            DiscoverNode = MenuDraw.Node(transform, "Tap to discover", HintR);
            var dl = MenuDraw.Text(DiscoverNode, HintR, DiscoverText, HintColor, "Text", HintPx, QHint);
            if (dl != null) MenuDraw.AlignRight(dl, HintR);
            DiscoverNode.gameObject.SetActive(false);

            // ⑥ `Tap to close`（+ 子 `Collider` 盖满整屏）—— 同样先关着
            CloseNode = MenuDraw.Node(transform, "Tap to close", HintR);
            var cl = MenuDraw.Text(CloseNode, HintR, CloseText, HintColor, "Text", HintPx, QHint);
            if (cl != null) MenuDraw.AlignRight(cl, HintR);
            // 整屏那块（原版 `Collider` 的 `NonDrawingGraphic`）：**必须是带 `ImageQuad` 的**，
            // 否则 `PointerLayer` 收不到（`Shell/MenuDraw.DeckCell` 那颗裸节点就是栽在这上面，已记账）。
            var surf = MenuDraw.Node(CloseNode, "Collider", CloseSurfaceR);
            var surfQ = ImageQuad.Create(surf, CardArt.Solid(),
                                         MenuDraw.Local(surf, CloseSurfaceR.x1, CloseSurfaceR.y1,
                                                        CloseSurfaceR.x2, CloseSurfaceR.y2),
                                         LayoutSpace.Px(CloseSurfaceR.H), new Vector2(0.5f, 0.5f), "Image");
            if (surfQ != null)
            {
                surfQ.SetAspect(CloseSurfaceR.W / CloseSurfaceR.H);
                surfQ.SetTint(new Color(0f, 0f, 0f, 0f));
                surfQ.SetRenderQueue(QCloseSurface);
                CloseSurfaceHit = surf.gameObject.AddComponent<WindowButton>();
                CloseSurfaceHit.onClick = Close;
            }
            CloseNode.gameObject.SetActive(false);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[BoosterPack] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            Debug.Log("[BoosterPack] 开了「" + o.Name + "」的开包窗：5 张卡 · 阵营 `"
                      + (string.IsNullOrEmpty(Army) ? "<全卡池>" : Army) + "` · 有传奇 " + hasLegendary
                      + " · 背景色 " + (hasLegendary ? "thereIsALegendaryCardbackgroundColor" : "常态 (1,1,1,1)"));
            HintGaps();
        }

        /// <summary>一格 `CardInBoosterPack UI n`。
        /// 🔴 层序照原版 `CardUI` 的**兄弟序**：`2DCard`（卡背) → `Card Ready for level up` → `New Card Badge` → `Ban Icon`。</summary>
        void BuildSlot(Transform parent, int i)
        {
            float cx = 960f + CardX[i] * AnchorScale;
            float cy = 540f;
            float w = CardW * CardK, h = CardH * CardK;                 // 卡本体 276.90 × 440.77
            float bx = ContainerW * CardK * 0.5f;                        // 容器半宽（角标锚点用）

            var slot = MenuDraw.Node(parent, "CardInBoosterPack UI " + (i + 1),
                                     new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f));
            SlotNodes[i] = slot;
            int q0 = QCard + i * CardStride;

            // ④a/④b 卡背（原版 `2DCard/Cardback Container` 的两件；`ChangeState(1)` 时开着）
            var backGo = new GameObject("Cardback Container");
            backGo.transform.SetParent(slot, false);
            BackNodes[i] = backGo;
            var sdf = MenuDraw.Rect(backGo.transform, CardArt.CardBackSdf(Army),
                                    new PxRect(cx - BackSdfW * CardK * 0.5f, cy - BackSdfH * CardK * 0.5f,
                                               cx + BackSdfW * CardK * 0.5f, cy + BackSdfH * CardK * 0.5f),
                                    "Cardback Shadow SDF", q0);
            if (sdf == null)
                Debug.Log("[BoosterPack] 第 " + (i + 1) + " 格的 `Cardback Shadow SDF` 没画"
                          + "（`CardArt.CardBackSdf(" + (Army ?? "null") + ")` 取不到 —— 那份 `_SDF` 掩码图没导）");
            var back = MenuDraw.Rect(backGo.transform, CardArt.CardBack(Army),
                                     new PxRect(cx - BackW * CardK * 0.5f, cy - BackH * CardK * 0.5f,
                                                cx + BackW * CardK * 0.5f, cy + BackH * CardK * 0.5f),
                                     "Cardback", q0 + 1);
            if (back == null)
                Debug.LogWarning("[BoosterPack] ⚠️ 第 " + (i + 1) + " 格的**卡背图取不到**（`CardArt.CardBack("
                                 + (Army ?? "null") + ")`）⇒ 那一格翻之前是**空的**。阵营 `" + (Army ?? "<空>")
                                 + "` 的默认卡背在 `Resources/Cardbacks.json` 里对不上，或图没跑 `import_original_art.py`");

            // ④c 卡面：`2DCard`（= `CardView`）。**先关着**，翻开才开（`ChangeState(1)` 是 ToggleCardBack(true)）。
            var faceGo = new GameObject("2DCard");
            faceGo.transform.SetParent(slot, false);
            FaceNodes[i] = faceGo;
            if (Cards != null && i < Cards.Length)
            {
                var cd = BattleDriver.ToCardData(Cards[i], Cards[i].Faction);
                var v = CardView.Create(faceGo.transform, cd, "CardUI");
                if (v != null)
                {
                    v.gameObject.SetActive(true);
                    v.SetPose(MenuDraw.Local(faceGo.transform, cx - w * 0.5f, cy - h * 0.5f,
                                             cx + w * 0.5f, cy + h * 0.5f),
                              0f, h / (CardView.Height * 108f));
                    v.SetData(cd);
                    v.SetFace(CardFace.Full);
                    // 🔴 队列要**覆盖 CardView 内部那些写死的 3000**（它照原版材质来的）
                    //    —— 收口到 `CardFan.SetCardQueue`（和卡片详情窗同一份，别各写一遍）。
                    CardFan.SetCardQueue(v, q0 + 2);
                }
                else Debug.LogError("[BoosterPack] 第 " + (i + 1) + " 格的 `CardView` 建不出来");
            }
            faceGo.SetActive(false);

            // ④d `Card Ready for level up`（1.78×2.74 @ (0,+0.02)，父 = `CardUI`）
            float uw = UpW * CardK, uh = UpH * CardK, uy = cy - UpDy * CardK;
            var upR = new PxRect(cx - uw * 0.5f, uy - uh * 0.5f, cx + uw * 0.5f, uy + uh * 0.5f);
            var up = MenuDraw.Node(slot, "Card Ready for level up", upR);
            // ⚠️ 图名用**导入后的文件名**（`CardArt.MenuUi` 不做「空格 → 下划线」转换）
            MenuDraw.Rect(up, Art("Card_Ready_For_Level_Up"), upR, "Image", q0 + 3);
            UpBadges[i] = up.gameObject;
            up.gameObject.SetActive(false);

            // ④e `New Card Badge`：锚 (0,.5)、pos (0.32,1.14) ⇒ 中心 = (容器左边 + 0.32, 容器中线 + 1.14)
            float bcx = cx - bx + BadgeAx * CardK, bcy = cy - BadgeAy * CardK;
            float bw = BadgeW * CardK, bh = BadgeH * CardK;
            var badgeR = new PxRect(bcx - bw * 0.5f, bcy - bh * 0.5f, bcx + bw * 0.5f, bcy + bh * 0.5f);
            var badge = MenuDraw.Node(slot, "New Card Badge", badgeR);
            // `WF_Special offer_Value` 324×87 · 九宫 162,0,162,0 · Sliced · 色 (0.547,0.0876,0.0542,1)
            var badgeTex = Art("WF_Special_offer_Value");
            if (badgeTex != null)
            {
                // ⚠️ `MenuDraw.Nine` 的 `borderOutPx` 传**画布 px**（`ImageQuad.PixelsPerUnit` 已经是 108，别自己乘除）
                var nb = MenuDraw.Nine(badge, badgeTex, badgeR, new Vector4(162f, 0f, 162f, 0f),
                                       badgeTex.width, badgeTex.height, q0 + 3,
                                       new Color(0.547f, 0.0876f, 0.0542f, 1f));
                if (nb == null) Debug.LogWarning("[BoosterPack] `New Card Badge` 的九宫格没建出来");
            }
            // 子 `Text`：`sizeDelta 115.15×21.43` × **`localScale 0.01`**、`m_fontSize 27.7`（同样是缩放前的字号）
            // ⇒ 画出来的字高 = `27.7 × 0.01 × CardK` ≈ **36.65 px**（**别直接把 27.7 当画布 px 传**）
            float btw = 1.1515f * CardK, bth = 0.2143f * CardK;
            var btR = new PxRect(bcx - btw * 0.5f, bcy - bth * 0.5f, bcx + btw * 0.5f, bcy + bth * 0.5f);
            var bt = MenuDraw.Text(badge, btR, NewBadgeText, new Color(1f, 0.78f, 0f, 1f), "Text",
                                   27.7f * 0.01f * CardK, q0 + 3);
            if (bt != null) MenuDraw.AlignLeft(bt, btR);          // 原版 `m_HorizontalAlignment = 1 (Left)`
            NewBadges[i] = badge.gameObject;
            badge.gameObject.SetActive(false);   // `SetRawCardData` 一进来就关（见文件头那张表）

            // ④f `Ban Icon`（**恒不显示**：`SetRawCardData` 末尾 SetActive(false)，翻牌链从不 ToggleBanned）
            //     锚 (.1,.5)-(.9,.5) ⇒ 宽 = 容器宽 × 0.8；`preserveAspect` ⇒ 实际画成正方形。
            //     `m_fontSize` 实读 **0.25**（`auto[0.25~72]`），节点**没有**缩放 ⇒ 画布 px = 0.25 × CardK ≈ 33.08。
            float banW = BanW * CardK, banSide = Mathf.Min(banW, BanH * CardK);
            var banR = new PxRect(cx - banSide * 0.5f, cy - banSide * 0.5f,
                                  cx + banSide * 0.5f, cy + banSide * 0.5f);
            var ban = MenuDraw.Node(slot, "Ban Icon", banR);
            MenuDraw.Rect(ban, Art("40k_Cross_icon_cross_big_Banned_card"), banR, "Image", q0 + 3);
            var bt2 = MenuDraw.Text(ban, banR, BannedText, Color.white, "Banned Text", 0.25f * CardK, q0 + 3);
            if (bt2 != null) bt2.SetAutoFitBox(LayoutSpace.Px(banSide), LayoutSpace.Px(banR.H),
                                               0.25f * CardK, 72f * CardK);
            BanIcons[i] = ban.gameObject;
            ban.gameObject.SetActive(false);

            // ④g 粒子挂点：原版把 `contentByRarities[i].particles` 实例化成**卡的子物体**，
            //     于是继承卡节点 `151 × 0.87626` 那个缩放 ⇒ 这里给一个同比例的挂点
            //     （`CardK / 108`：我们的世界是 108 px/单位，而卡那边是 `CardK` px/单位）。
            var vfx = new GameObject("EffectAnchor");
            vfx.transform.SetParent(slot, false);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localScale = Vector3.one * (CardK / (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight));
            VfxAnchors[i] = vfx.transform;

            // 命中区：**必须有 `ImageQuad`**（`PointerLayer.CollectHits` 取的是「按钮下第一个 ImageQuad」）
            float hitW = w * HitRatio, hitH = h * HitRatio;
            var hit = MenuDraw.Hit(slot, "Hit",
                                   new PxRect(cx - hitW * 0.5f, cy - hitH * 0.5f, cx + hitW * 0.5f, cy + hitH * 0.5f),
                                   q0 + 4, null);
            if (hit != null)
            {
                int captured = i;
                var wb = hit.GetComponent<WindowButton>();
                SlotHits[i] = wb;
                if (wb != null)
                {
                    wb.onClick = () => Reveal(captured);
                    wb.onEnter = () => { if (!Opened[captured]) Hover(captured, true); };
                    wb.onExit = () => { if (!Opened[captured]) Hover(captured, false); };
                }
            }
        }

        /// <summary>卡的点击接收器比卡面**小一圈** —— 原版 `2DCard/UI Collider` 是拉伸锚 + `sd (−0.2,−0.44)`
        /// ⇒ 473.18×722.83 / 523.25×832.75 = **0.9043 / 0.8679**（`资料/阶段二_卡片详情窗_原版规格.md` 记过同一件，
        /// 那张是菜单尺寸，比值与这里同源）。取**较小的那个**做统一比例，宁可小一圈也别伸出卡外。</summary>
        const float HitRatio = 0.8679f;

        // ============================================================ 交互

        /// <summary>翻一张（原版 `CardInBoosterPack.UiColliderOnClick` **只在该卡 state==2（可交互）时**生效；
        /// state 3 = 播翻牌动画 + `BoosterPackOpenWindow.CardOpened(this)`）。
        /// 🔴 我们**不做 spin 动画与相机抖动**（见文件头），只做「换层 + 记数 + 播粒子」。</summary>
        public void Reveal(int i)
        {
            if (i < 0 || i >= 5 || Opened[i] || CardsLeft <= 0) return;
            Opened[i] = true;
            if (BackNodes[i] != null) BackNodes[i].SetActive(false);
            if (FaceNodes[i] != null) FaceNodes[i].SetActive(true);
            if (SlotHits[i] != null) SlotHits[i].gameObject.SetActive(false);   // 翻开的卡不再吃点击
            ApplyCardBadges(i);
            PlayCardFx(i);
            // `CardOpened`：把 `Tap to discover` 淡出（我们直接关）+ 计数自减
            if (DiscoverNode != null) DiscoverNode.gameObject.SetActive(false);
            CardsLeft--;
            if (CardsLeft <= 0 && CloseNode != null)
            {
                CloseNode.gameObject.SetActive(true);      // 原版还会 DOScale ×1.1 / 0.4s OutBack（我们直接设值）
                Debug.Log("[BoosterPack] 5 张全翻开 ⇒ `Tap to close` 出现（原版 `CardOpened` 里 `cardsLeftToOpen` 归零那条）");
            }
            Debug.Log("[BoosterPack] 翻开第 " + (i + 1) + " 张：" + (Cards != null && i < Cards.Length ? Cards[i].Name : "?")
                      + "（稀有度档 " + (Cards != null && i < Cards.Length ? RarityInt(Cards[i]) : -1)
                      + " · 还剩 " + CardsLeft + " 张）");
        }

        /// <summary>翻开之后那两个角标（判据逐条见文件头那张表）。</summary>
        void ApplyCardBadges(int i)
        {
            if (Cards == null || i >= Cards.Length) return;
            var c = Cards[i];
            bool canUp = ReadyForLevelUp(c);
            if (UpBadges[i] != null) UpBadges[i].SetActive(canUp);
            bool isNew = IsNewCard(c);
            if (NewBadges[i] != null) NewBadges[i].SetActive(isNew);
        }

        /// <summary>`BasicCardUI.ToggleCarUpgradeIfNeeded` 的判据是 `CardServices.CheckForCardUpgrade(card)`
        /// —— **服务端数据，本地没有** ⇒ 用工程既有的 `CardProgress.CanUpgrade`（与卡片详情窗同一份）。
        /// ⚠️ 我们的拥有数口径是「给足」⇒ 这一条**恒真**（见文件头）。</summary>
        public static bool ReadyForLevelUp(CardDef c)
        {
            return c != null && CardProgress.CanUpgrade(c.Id, c.Rarity);
        }

        /// <summary>`BasicCardUI.ToggleNewCardBadge(GetOwnedCount(card) == 1)` —— 这是你**第一张**。
        /// ⚠️ 我们的 `CardProgress.Owned` 默认就给足（≥ 卡组上限 + 9）⇒ 这一条**恒假**（见文件头）。</summary>
        public static bool IsNewCard(CardDef c)
        {
            return c != null && CardProgress.Owned(c.Id, c.Rarity) == 1;
        }

        /// <summary>`CardInBoosterPack.TryHighlightCardback(bool)`：悬停 ⇒ 整卡 ×**1.1**，补间 **0.3 s**。
        /// ⚠️ 批处理里没有帧循环（DOTween 补间跑不完）⇒ **直接设值**，把原版的补间时长记在这。
        /// ⚠️ 缩的是**卡那一层**（`2DCard` 及三个角标 + 卡背），**不缩命中区**（缩了指针一抖就掉出去）。</summary>
        public void Hover(int i, bool on)
        {
            if (i < 0 || i >= 5) return;
            float s = on ? HoverScale : 1f;
            var one = Vector3.one * s;
            if (BackNodes[i] != null) BackNodes[i].transform.localScale = one;
            if (FaceNodes[i] != null) FaceNodes[i].transform.localScale = one;
            if (UpBadges[i] != null) UpBadges[i].transform.localScale = one;
            if (NewBadges[i] != null) NewBadges[i].transform.localScale = one;
            if (BanIcons[i] != null) BanIcons[i].transform.localScale = one;
        }

        /// <summary>播这一档的粒子（原版 `ChangeState(3)` 里 `Instantiate(contentByRarities[i].particles, card.transform)`）。
        /// 🔴 **只出声、不静默**：效果库没生成 / 里没有这一条时打警告并**记下来**（自检也看这个列表）。</summary>
        void PlayCardFx(int i)
        {
            if (Cards == null || i >= Cards.Length) return;
            string fx = CardFxFor(RarityInt(Cards[i]));
            var player = WarpforgeVFX.WarpforgeEffectPlayer.Play(fx, VfxAnchors[i], Vector3.zero, 1f, -1f);
            if (player == null)
            {
                Debug.LogWarning("[BoosterPack] 第 " + (i + 1) + " 张的翻牌粒子 `" + fx + "` **没播出来**"
                                 + " —— 要么效果库没生成（`-executeMethod EffectLibraryBuilder.Run`），"
                                 + "要么 `BoosterPackExporter.Run` 还没跑过。这一格会**只有卡、没有粒子**。");
                return;
            }
            PlayedFx.Add(fx);
        }

        /// <summary>4 个开卡包粒子 prefab 的名字。**判据 = `CardInBoosterPack.contentByRarities[]` 逐条实读**
        /// （`0/1 → Rarity 1` · `2 → Rarity 2` · `3 → Rarity 3` · `4/5 → Rarity 4`），名字本身出自
        /// `boosterpacks_assets_all` 里那 4 个 GameObject。
        /// 🔴 **表只此一份**：`WarpforgeArena1/Editor/BoosterPackExporter.cs` 的根表**转调这里**
        /// （编辑期程序集引用运行期程序集是允许的、反过来不行 ⇒ 表必须放在这一侧）。
        /// ⚠️ `工具/extract_missing_shaders.py` 的 `BOOSTER_GROUPS.roots` 里也有同 4 个名字 —— 那是 python，
        ///    只能各写一份（重打包要用），两处的注释互相指认。</summary>
        public static readonly string[] CardFxNames =
        {
            "Boosterpack Open Card Rarity 1",
            "Boosterpack Open Card Rarity 2",
            "Boosterpack Open Card Rarity 3",
            "Boosterpack Open Card Rarity 4",
        };

        /// <summary>稀有度档 → 粒子 prefab 名（分组见 `CardFxNames`）。</summary>
        public static string CardFxFor(int rarity)
        {
            if (rarity <= 1) return CardFxNames[0];
            if (rarity == 2) return CardFxNames[1];
            if (rarity == 3) return CardFxNames[2];
            return CardFxNames[3];
        }

        // ============================================================ 时间

        /// <summary>原版 `BoosterPackOpenWindow.Update`：累加 `timeAlive`；到 `timeToShowHelpText`（**10 s**）
        /// 且提示还藏着 ⇒ 把 `Tap to discover` 打开并淡入（我们直接开）。
        /// ⚠️ 批处理里没有帧循环 ⇒ 自检**直调本方法**（和 `WindowButton.Enter/Exit` 同一条口径）。</summary>
        public void AddTime(float dt)
        {
            if (dt <= 0f) return;
            if (TimeAlive >= HelpDelay) return;
            TimeAlive += dt;
            if (TimeAlive < HelpDelay || !HelperHidden) return;
            HelperHidden = false;
            if (DiscoverNode != null) DiscoverNode.gameObject.SetActive(true);
            Debug.Log("[BoosterPack] 静止 " + HelpDelay.ToString("F0") + " s ⇒ `Tap to discover` 出现"
                      + "（原版 `Update` 里 `timeToShowHelpText` 那条）");
        }

        void Update() { AddTime(Time.deltaTime); }

        // ============================================================ 数据：这一包是哪 5 张

        /// <summary>商品主图 → 阵营。**判据 = 原版 SO 自己的字段**：`Booster Pack Ultramarines.json` 的
        /// `containerPreviewImage.m_SubObjectName = "40K_shop_offer_booster_UM"` ⇒ **那张图就是那个阵营的**。
        /// ⚠️ 这份映射**是我们把两张表对起来的**（我们的商品表里没有阵营字段）——
        ///    名字对不上时不算错、但要**出声**（`ArmyOfArt` 返回 null ⇒ 全卡池）。
        /// </summary>
        public const string ArtUM = "40K_shop_offer_booster_UM";
        public const string ArtSautekh = "40K_shop_offer_booster_Sautekh";
        public const string ArtSpaceWolves = "40K_shop_offer_booster_Space_Wolves";
        public const string ArtLeviathan = "40K_shop_offer_booster_leviathan";
        public const string ArtGeneric = "40K_shop_offer_booster_generic";

        public static string ArmyOfArt(string art)
        {
            switch (art)
            {
                case ArtUM: return "Ultramarines";
                case ArtSautekh: return "Sautekh";
                case ArtSpaceWolves: return "SpaceWolves";
                case ArtLeviathan: return "Leviathan";
                default: return null;                    // 含 `_generic`：**没有阵营** ⇒ 全卡池
            }
        }

        /// <summary>稀有度串 → 原版那个整数档（`contentByRarities[].rarity` / `card.rarity`）。
        /// 判据：`contentByRarities` 的分组是 `0/1 · 2 · 3 · 4/5`，
        /// 而 `BoosterPackOpenWindow.Initialize` 判「包里有传奇」用的是 `card.rarity == **4**`
        /// ⇒ **4 = legendary**；`CardRarityColorsSO.colors[]` 里 rarity 4 是橙色、3 紫、2 绿，逐档对上。</summary>
        public static int RarityInt(CardDef c)
        {
            if (c == null) return 0;
            switch (c.Rarity)
            {
                case "rare": return 2;
                case "epic": return 3;
                case "legendary": return 4;
                default: return 0;                       // common / special（special 是**卡框档位**、不是稀有度）
            }
        }

        /// <summary>摇这一包。规则来自**原版自己的出厂文案**（`Booster Pack Ultramarines` SO 的
        /// `itemDescriptionKey = Item_Description/BoosterPackExplanationUltramarines`，文案原文见
        /// `BoosterInfoPopup.DescSample`）：
        ///   · 「Contains **5** cards for the … army.」（与 prefab 里正好 **5** 个 `CardInBoosterPack` 互证）
        ///   · 「At least one of the cards is guaranteed to be **Rare or better**」
        /// 🔴 **哪 5 张是我们摇的**（原版走服务端 drop table，那两张 `DT *` 本地没导出来）。
        /// ⚠️ 用 `System.Random(seed)` **可复现**（原版 `Initialize` 里那句 `ListExtensions.Shuffle` 是无种子的）。
        /// 阵营池不够 5 张 / 没有 rare+ 时**出声**并退让，不静默。</summary>
        public static CardDef[] RollPack(string army, int seed)
        {
            var pool = CardDatabase.Load();
            var list = string.IsNullOrEmpty(army) ? pool : CardDatabase.OfFaction(pool, army);
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("[BoosterPack] ⚠️ 阵营 `" + army + "` 在卡池里一张都没有 ⇒ 退回全卡池");
                list = pool;
            }
            var rare = new List<CardDef>();
            var rest = new List<CardDef>();
            for (int i = 0; i < list.Count; i++)
                (RarityInt(list[i]) >= 2 ? rare : rest).Add(list[i]);
            if (rare.Count == 0)
                Debug.LogWarning("[BoosterPack] ⚠️ 阵营 `" + army + "` 里**没有 rare 及以上的卡** ⇒ "
                                 + "「至少一张 Rare or better」这条**这一包满足不了**（如实说，不换别的卡凑）");

            var rnd = new System.Random(seed);
            var pick = new List<CardDef>();
            if (rare.Count > 0) pick.Add(rare[rnd.Next(rare.Count)]);
            // 剩下 4 张从「除了已选那张以外的全部」里抽
            var others = new List<CardDef>();
            for (int i = 0; i < list.Count; i++) if (list[i] != pick[0]) others.Add(list[i]);
            for (int k = 0; pick.Count < 5 && others.Count > 0; k++)
            {
                int j = rnd.Next(others.Count);
                pick.Add(others[j]);
                others.RemoveAt(j);
            }
            while (pick.Count < 5) pick.Add(list[rnd.Next(list.Count)]);   // 池子太小才会走到这（上面已出声）
            // 原版 `Initialize` 里那句 `Shuffle` ⇒ 顺序也打乱
            for (int i = pick.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var t = pick[i]; pick[i] = pick[j]; pick[j] = t;
            }
            return pick.ToArray();
        }

        /// <summary>把「原版有、我们没做」的几处**当场出声**（红线：不许静默失败）。</summary>
        void HintGaps()
        {
            Debug.Log("[BoosterPack] ⚠️ 五处如实标注：① **卡包内容是我们摇的**（原版走服务端 drop table，"
                      + "`DT Ultramarines All(R2+)` 那两张本地没导出来）；② **翻牌动画没做**"
                      + "（原版 `Content` 上播 `Booster Opening - Card *`）—— 我们直接换层；③ **相机抖动没做**"
                      + "（原版 `contentByRarities[].shakes` 那几条）；④ **音效没做**（cue 在远端音频表）；"
                      + "⑤ **`useQuickOpenProtection`（60 s 内连开加速）没做**。详见 `Shell/BoosterPackOpenWindow.cs` 文件头");
        }

        public string Dump()
        {
            return "BoosterPack[" + Page + "/" + Index + "] 阵营 " + (string.IsNullOrEmpty(Army) ? "<全池>" : Army)
                   + " · 5 张 · 已翻 " + (5 - CardsLeft) + " · 粒子 " + PlayedFx.Count + " 次"
                   + " · 取不到的图 " + MissingArt.Count + " 张";
        }
    }
}
