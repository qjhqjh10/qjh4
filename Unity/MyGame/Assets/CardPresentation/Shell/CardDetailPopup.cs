// CardDetailPopup.cs — 卡组线最后一件：**卡片详情窗**（原版菜单版 `CardDisplayWindow`）
//
// ============================ 出处（唯一正本） ============================
// 🔴 **`资料/阶段二_卡片详情窗_原版规格.md`**（2026-09-24 建，逐节点表 + 三块面板 + 计数条 + 入口链 + 查不到的）。
//
// 两条**与直觉相反**的实证（详见正本 §〇）：
//   ① 这扇窗**不是 `bundle_menus_assets_all` 里的 prefab，是 `mainmenuwarpforge` 场景里的场景对象**
//      （出厂 `m_IsActive=false`），所以「按 prefab 找」是找不到的；
//   ② **菜单版 ≠ 战斗版**：菜单卡 **523.25×832.75 px**（`CardUI (4)` scale **250**），
//      战斗 `CardUI Reference` scale 223.14 ⇒ 467×743。工程里已有的 `Battle/CardDisplayWindow.cs`
//      **是战斗那扇**（743px），**不能直接复用**。
//
// ============================ 结构（照正本 §一/§二/§三 抄）============================
//   · `Menu Dark Background` rect 比屏幕大（盖住任何画幅）· 色 **(0,0,0,.773)** → **点它关窗**
//     🔴 原版**全树没有关闭钮**（`Close*` 只命中 `BackgroundCloseButton`）⇒ 关 = 点遮罩 / ESC
//   · `Card Display` **584,106 → 1336,974**（752×868）· 卡本体在 `Cards/CardUI (4)/2DCard` = **523.25×832.75**
//   · `LowerSection` **300,921.34 → 1620,1058.34**
//     ├ `FlavourTextBG` 300,900.84→1620,1078.84（原版 `spr=0`、运行时由 `FlavourTextSO` 喂 ⇒ **本地没有**）
//     ├ `LoreText` 335,943.11→1585,1036.58 **fs35 hAlign=Right**
//     ├ `Voice Over Button` 1650,945.51→1738.66,1034.17（88.655²，`40k_UI_bt_voicelines`）
//     └ `Show Card Text` 181.34,945.51→270,1034.17（88.655²，`40k_UI_bt_eye`）
//   · `Panel`（VLG align4）**1294,146.37 → 1744,903.35**，三块 **x=[1294,1744] 宽 450**、y **146.71 / 411.12 / 673.42**
//     ├ `Crafting Panel`（创建副本）· `Upgrade Panel`（升级）· `Alternate Art Panel`（异画）
//   · `Card Counter` **825.07,829.81 → 1094.93,909.19**（三个 TMP：`x{n}` / `"/ "` / `{n}`）
//   · `Wildcard Segment`（0,71→1920,156）+ `WIldcard Counter`（1470,71→1870,156）
//
// ============================ 没建的 / 我们挑的（逐条出声）============================
//   · **`FlavourTextBG` 那张底图本地没有**（`sprite=0`，预设喂的是 `FlavourTextSO`）⇒ 我们只画字、不画底。
//   · **`Lore Text` 的内容**：原版是**背景故事**；我们引擎里**没有 lore 字段** ⇒ 这里画**卡面效果文字**
//     （= `项目任务.md` §三 第 13-E 条第 8 项那条老账的同一件事），**如实标**。
//   · **升级表数值是我们编的**（原版在服务端 `CardTierConfig`）—— 照实拍那个数据点
//     （等级 3 = 4 张 + 2000 + 500 锻造点）做梯度，见 `CardProgress`。**用户 2026-09-22 已裁决照办。**
//   · **不扣货币**：单机资源固定 9999（同上裁决）⇒ 升级/创建副本只改**本地拥有数与等级**，货币不动，**出声**。
//   · `Debug Add cards`（开发用）· `Item Information Panel`(act=F) · `Game Mode Icon` 那类 **不建**。
//   · `Alternate Art Panel` 的左右钮：**本地只有 7 张督军异画**（2 种风格）⇒ 不是这张卡就**没有异画可切**
//     （如实说，不给别的督军换 —— 异画**绑死在那张卡上**，见 `CardArt.AltArt` 的注释）。
using System.Collections.Generic;
using DG.Tweening;          // 换位那两条补间（`CardTween.ToPose` 返回 `Tween`，`OnComplete` 是它的扩展方法）
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>单机口径的卡牌进度（拥有数 / 等级）。**升级表数值是我们编的**，见文件头。</summary>
    public static class CardProgress
    {
        /// <summary>升到第 i 级**累计**要几张副本（下标 = 等级）。`MaxLevel = 4`。</summary>
        public static readonly int[] NeedCopies = { 0, 0, 2, 5, 9 };
        /// <summary>升到第 i 级要多少金币（**我们编的，不是原版**）。</summary>
        public static readonly int[] NeedGold = { 0, 0, 500, 1000, 2000 };
        /// <summary>升到第 i 级给多少锻造点（照实拍那个数据点做的梯度；**我们编的**）。</summary>
        public static readonly int[] Points = { 0, 0, 100, 250, 500 };
        public const int MaxLevel = 4;

        /// <summary>能放进卡组的张数上限（规则书：经典 **传说 ≤1**、普通/稀有/史诗 ≤2）。</summary>
        public static int DeckCap(string rarity) { return rarity == "legendary" ? 1 : 2; }

        static readonly Dictionary<string, int> _owned = new Dictionary<string, int>();
        static readonly Dictionary<string, int> _lvl = new Dictionary<string, int>();

        /// <summary>拥有数。**口径「给足」**（用户 2026-09-22 裁决）：够 `卡组上限 + 升满所需`。</summary>
        public static int Owned(string id, string rarity)
        {
            int v;
            if (_owned.TryGetValue(id, out v)) return v;
            v = DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1]);
            _owned[id] = v;
            return v;
        }
        public static void AddOwned(string id, string rarity, int n)
        {
            _owned[id] = Owned(id, rarity) + n;
        }
        public static int Level(string id) { int v; return _lvl.TryGetValue(id, out v) ? v : 1; }
        public static void SetLevel(string id, int l) { _lvl[id] = l; }

        /// <summary>多余副本数 = 拥有 − min(拥有, 卡组上限)（**原版 `CardCounterDisplay.Initialize` 的判据**）。</summary>
        public static int Spares(string id, string rarity)
        {
            int o = Owned(id, rarity), cap = DeckCap(rarity);
            return o - Mathf.Min(o, cap);
        }
        /// <summary>升到下一级够不够（**只吃多余副本**，同原版）。</summary>
        public static bool CanUpgrade(string id, string rarity)
        {
            int l = Level(id);
            return l < MaxLevel && Spares(id, rarity) >= NeedCopies[l + 1];
        }

        public static void ResetForTest() { _owned.Clear(); _lvl.Clear(); }
    }

    /// <summary>`CardDetailPopup : GameWindow` —— 点一张卡开的那扇详情窗（原版菜单版 `CardDisplayWindow`）。</summary>
    public class CardDetailPopup : GameWindow
    {
        // 层：**高于收藏窗那一整片**（它最高到 `CollectionWindow.QFltHit = 3043`）；
        //    低于 `Deck info Popup`(3120+) / `Deck Selection Popup`(3125+) / `PromptPopup`(3140+)
        //    —— 提示窗要能压在**所有**窗之上。
        //
        // 🔴 **2026-09-28 重排（原来画错了）**：原来是 `QCd = 3110` 而卡格在 `3009–3017`
        //    ⇒ **遮罩画在卡格【之上】⇒ 整叠卡被压暗了一半**（拿实拍量出来的：前台卡名峰值
        //    只有 131/255，而原版同一处是 232/255 —— 131 正好 = 白 ×(1−0.773) 在**线性空间**下
        //    编码回 sRGB；判据全文 → 正本 §十·5）。
        //    原版那棵树的**兄弟序**是 `Menu Dark Background`(0) → … → `Card Display`
        //    ⇒ **遮罩在最底下**。现在三段分明：
        //    **遮罩 3105 < 卡格 3106–3114 < 本窗的钮/字/面板 3115+**，整段仍在收藏窗与卡组编辑之上。
        // ✅ **2026-10-11（A252）可见性收窄**：这两行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   本件只留**本窗之外真被引用**的那一个 —— `QCdHit`（`Editor/CollectionScene.cs` 的卡片详情自检）。
        //   量法（可复跑）：脚本扫全工程 **301** 个 `.cs` 的限定名 `CardDetailPopup.<常量>`、**剔注释**、
        //   剔本文件；另核过两件会「看着对、其实编不过」的事：① 同文件里的另一个类 `CardProgress` **不**用它们；
        //   ② **没有任何类从本窗派生**（`class X : CardDetailPopup` 全 0）—— 所以「回 `const`」是安全的。
        const int QShade = 3105;            // 3105 整屏遮罩（`Menu Dark Background`）
        const int QCardStack = 3106;        // 3106 卡格那一叠（`Card Display`）
        const int QCd = 3115;               // 3115 本窗的钮 / 字 / 面板
        const int QCdRow = 3116;
        const int QCdText = 3117;
        public const int QCdHit = 3118;     // ✅ 留 `public`：`Editor/CollectionScene.cs` 的自检引用（1 处）

        // ---- 几何（**两处摆放共用一份** → `CardWinBox`；本文件只留别名，值不再各写一套）----
        public static readonly Color ShadeColor = CardWinBox.ShadeColor;
        public const float CardL = CardWinBox.CardL, CardT = CardWinBox.CardT;
        public const float CardR = CardWinBox.CardR, CardB = CardWinBox.CardB;   // `Card Display` 752×868
        public const float LowerT = CardWinBox.LowerT, LowerB = CardWinBox.LowerB;
        public const float LowerL = CardWinBox.LowerL, LowerR = CardWinBox.LowerR;
        public const float LoreBgT = CardWinBox.LoreBgT, LoreBgB = CardWinBox.LoreBgB;
        public const float LoreL = CardWinBox.LoreL, LoreT = CardWinBox.LoreT;
        public const float LoreR = CardWinBox.LoreR, LoreB = CardWinBox.LoreB, LorePx = CardWinBox.LorePx;
        /// <summary>两个圆钮各 **88.655²**（`pivot` 在 LowerSection 内水平两端 ±734.33）。</summary>
        public const float BtnS = CardWinBox.BtnS;
        public const float VoiceCx = CardWinBox.VoiceCx, EyeCx = CardWinBox.EyeCx, BtnCy = CardWinBox.BtnCy;
        /// <summary>`Panel`（VLG align4）与三块面板的 y（**布局组跑之前的模板位按 ±1px 抄**，正本 §二）。</summary>
        public const float PanL = 1294f, PanR = 1744f, PanT = 146.37f, PanB = 903.35f;
        public const float CraftT = 146.71f, CraftB = 410.44f;
        public const float UpgT = 411.12f, UpgB = 672.74f;
        public const float AltT = 673.42f, AltB = 903.01f;
        /// <summary>`Card Counter` 825.07,829.81→1094.93,909.19；`Duplicate Counter` 底 840,834.21→1080,904.79。</summary>
        public const float CcBgL = 840f, CcBgT = 834.21f, CcBgR = 1080f, CcBgB = 904.79f;
        public const float CcX1L = 869.41f, CcX1R = 942.69f, CcY1 = 846.61f, CcY2 = 917.19f, CcX1Px = 52.6f;
        public const float CcSlL = 942.69f, CcSlR = 970.71f, CcSlPx = 57f;
        public const float CcX2L = 970.71f, CcX2R = 1014.80f, CcX2T = 856.90f, CcX2B = 906.90f;
        public const float CcImL = 1014.80f, CcImR = 1056.74f, CcImT = 853f, CcImB = 903f;
        /// <summary>通配符条：`Segment` 0,71→1920,156 · 底 1550,91.5→1870,135.5 · 四组 30×44 图标 + 41×44 数字。</summary>
        public const float WcBgL = 1550f, WcBgT = 91.5f, WcBgR = 1870f, WcBgB = 135.5f;
        public const float WcStep = 75f, WcX0 = 1565f, WcPx = 32.6f;

        static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        const float BtnTexW = 410f, BtnTexH = 124f;

        /// <summary>正在展示的卡（`CardData` 是 struct ⇒ 另用一个 bool 判「有没有」）。</summary>
        public CardDef Card;
        public bool HasCard;

        // ============================================================ 卡片那一叠（1 主卡 + 8 相关卡）
        //
        // 🔴 **扇形位姿 / 分层 / 着色那三组数，判据只此一份 = `Core/CardFan.cs`**（2026-09-28 收口）。
        //    为什么：**原版是同一个脚本 `CardDisplayWindow`、两处摆放**（菜单版 = 本窗，
        //    战斗版 = `Battle/CardDisplayWindow.cs`），两组数两边都要用 ⇒ 写两份迟早不一致。
        //    真值来源（原版那条 legacy clip `Card Display Open`）、槽 5–8 的外推规则、
        //    分层为什么要「每格一个队列」、相关卡为什么要压暗到 0.65 —— **全在那个文件的注释里**。
        //    ⚠️ 顺带订正一处：本文件 2026-09-27 记的槽 3 转角 **12.648°** 是错的，clip 原文复算是 **12.750°**。

        /// <summary>卡位个数：**1 主卡 + 8 相关卡**（原版是 1+4；**8 这一档是我们挑的**，用户授权的冗余）。</summary>
        public const int Slots = CardFan.Slots;
        /// <summary>换位动画时长 —— 原版 `CardDisplayWindow.relatedCardSwapTime`（字段 `+0xC0`）。</summary>
        public const float SwapTime = CardFan.SwapTime;
        /// <summary>槽名（照原版命名：**前台那张不带后缀**，其余 `CardUI (1..8)`）。</summary>
        public static string SlotName(int i) { return CardFan.SlotName(i); }
        /// <summary>命中区所在的 z 阶梯（**只给命中区用** —— 它是 alpha=0 的透明件，不影响画面；
        /// `PointerLayer` 同队列时**取 z 小的那个**，所以前台那张要最小）。</summary>
        const float HitZStep = 0.08f;

        // 🔴 **「把整格卡搬到同一个队列」那段（含「为什么必须走 `.material`」那个坑）已收口到
        //    `CardFan.SetCardQueue`** —— 两扇窗共用一份。卡格的队列基数见 `QCardStack`（本窗）。

        /// <summary>每个位姿槽上的卡（下标 = **位姿槽**，0 = 前台位）。</summary>
        public readonly CardView[] SlotViews = new CardView[Slots];
        readonly CardDef[] _slotDefs = new CardDef[Slots];
        bool _swapping;

        /// <summary>位姿槽上有几张卡（= 1 + 相关卡数，最多 `Slots`）。</summary>
        public int SlotCount { get; private set; }
        /// <summary>**前台那张**（永远是位姿槽 0）。原版 `CardSwapFinished` 之后 lore / 语音认的就是它。</summary>
        public CardDef FrontDef { get { return _slotDefs[0]; } }
        /// <summary>换位动画在播没有（= 原版那个 `swappingCards` 闸）。</summary>
        public bool IsSwapping { get { return _swapping; } }
        /// <summary>位姿槽 `i` 上的那张卡（越界返回 null）。</summary>
        public CardView SlotView(int i) { return (i >= 0 && i < Slots) ? SlotViews[i] : null; }
        /// <summary>位姿槽 `i` 上那张卡的卡表项（越界返回 null）。</summary>
        public CardDef SlotDef(int i) { return (i >= 0 && i < Slots) ? _slotDefs[i] : null; }
        public int SlotIndexOf(string cardId)
        {
            for (int i = 0; i < Slots; i++) if (_slotDefs[i] != null && _slotDefs[i].Id == cardId) return i;
            return -1;
        }

        /// <summary>效果文字条（`LowerSection/FlavourTextBG` + `LoreText`）现在露着没有（原版 `Show Card Text` 切它）。</summary>
        public bool LoreVisible = true;
        /// <summary>下缘那两颗钮的**整组节点**（图 + 命中区一起开/关）—— 建不建由卡决定，
        /// 判据只此一份 → `CardButtons`（原版 `SetCardVoiceOver` / `showCardTextButton`）。</summary>
        Transform _voiceBox, _eyeBox;
        /// <summary>最近一次开的（自检用）。</summary>
        public static CardDetailPopup LastOpened;
        /// <summary>最近一次播的语音文件（自检用）。</summary>
        public string LastVoiceFile { get; private set; }

        public Transform ShadeHit { get { return Find(transform, "BackgroundHit"); } }
        public Transform VoiceHit { get { return Find(transform, "VoiceHit"); } }
        public Transform EyeHit { get { return Find(transform, "ShowTextHit"); } }
        public Transform CraftHit { get { return Find(transform, "CraftHit"); } }
        public Transform UpgradeHit { get { return Find(transform, "UpgradeHit"); } }
        public Transform Counter { get { return Find(transform, "Card Counter"); } }
        /// <summary>升级面板的 `No Upgrade Warning`（满级时才露）。</summary>
        public Transform NoUpgradeWarn { get { return Find(transform, "No Upgrade Warning"); } }
        /// <summary>三块面板的 `Title` 文本（自检用）。</summary>
        public string TitleOf(string which) { var t = Find(transform, which + " Title"); return t != null ? TextOf(t) : "(无)"; }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static string TextOf(Transform t)
        {
            var l = t != null ? t.GetComponent<Label>() : null;
            return l != null ? (l.Text ?? "") : "";
        }

        public static CardDetailPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Card Displayer Menu For Menu");
            var win = go.AddComponent<CardDetailPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            // ⚠️ 原版 MB 里 `windowsPlacement = 0 (None)`，但它的**实际父节点是 `3 - PopUp Holder`**
            //    （= 我们的 `WindowsPlacement.Popup`）⇒ 我们按**实际父链**挂，**不按那个字段**
            //    （同 `资料/阶段二_战斗入口_原版规格.md` 开头那条「别按 placement 去猜它挂哪」）。
            win.placement = WindowsPlacement.Popup;
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>开/换一张卡。**复用同一个窗**（原版 `ShowCard` 里 `WindowsManager.OpenWindow(this)` 就是这个意思）。</summary>
        public void ShowCard(CardDef card)
        {
            Card = card;
            HasCard = card != null;
            LoreVisible = true;                 // 原版每次 `ShowCard` 都把 `loreObjectBG` 复位
            if (Manager != null) { Manager.OpenWindow(this); return; }
            gameObject.SetActive(true);
            Build();
        }

        public override void Open() { Build(); }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(root.GetChild(i).gameObject);
            _voiceBox = null; _eyeBox = null;      // 旧的那两颗随子件一起没了 ⇒ 引用也要清
            if (!HasCard) return;

            // 1) 压暗 + 点遮罩关窗（原版唯一的「关」）
            //    🔴 队列 `QShade`（**3105**）—— **必须低于卡格**（`QCardStack = 3106`）：
            //    原版那棵树里 `Menu Dark Background` 是**第一个孩子**（画在最下），卡在它上面。
            MenuDraw.Rect(root, CardArt.Solid(),
                          new PxRect(CardWinBox.MaskL, CardWinBox.MaskT, CardWinBox.MaskR, CardWinBox.MaskB),
                          "Menu Dark Background", QShade, ShadeColor);
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QCdHit − 1`(3117) → `QShade`(3105)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3117 恰好是 `QCdText`（**文字那一档**）——「内容档 − 1」看着像派生，其实落在别的层上。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QShade, QCdHit, () => Close(), "BackgroundHit");

            // 2) 卡片那一叠（1 主卡 + 8 相关卡 · 扇形）—— 判据与真值 → `CardFan`
            var cardNode = MenuDraw.Node(root, "Card Display", new PxRect(CardL, CardT, CardR, CardB));
            BuildCardStack(cardNode);

            // 3) `LowerSection`：效果文字条 + 语音钮 + 「显示卡面文字」钮
            var lower = MenuDraw.Node(root, "LowerSection", new PxRect(LowerL, LowerT, LowerR, LowerB));
            // ⚠️ `FlavourTextBG` 那张底图**本地有**（13 张按阵营的 `40K_display_Flavortext <阵营>.png`，
            //    工程里还没导 —— §三 第 22 条 第 2 项）⇒ **现在只画字**
            var bg = MenuDraw.Node(lower, "FlavourTextBG", new PxRect(LowerL, LoreBgT, LowerR, LoreBgB));
            BuildLoreText(bg);

            // 两颗钮：**整组一个节点**（图 + 命中区一起开/关）—— 建不建由卡决定，见 `RefreshButtons`
            float vx = VoiceCx - BtnS * 0.5f, ex = EyeCx - BtnS * 0.5f, vy = BtnCy - BtnS * 0.5f;
            _voiceBox = MenuDraw.Node(lower, "Voice Over Button", new PxRect(vx, vy, vx + BtnS, vy + BtnS));
            var voiceQ = MenuDraw.Rect(_voiceBox, CardArt.Ui("40k_UI_bt_voicelines"),
                          new PxRect(vx, vy, vx + BtnS, vy + BtnS), "Image", QCdRow, null, true);
            // 🆕 A17：原版 `Card Displayer Menu For Menu>…>Voice Over Button` 是 SpriteSwap
            //（高亮 `40k_UI_bt_voicelines_hover`，普查 §块 1 那两条 ❌ 之一）
            Hit(_voiceBox, "VoiceHit", new PxRect(vx, vy, vx + BtnS, vy + BtnS), () => PlayVoice(), QCdHit,
                voiceQ, "40k_UI_bt_voicelines");
            _eyeBox = MenuDraw.Node(lower, "Show Card Text", new PxRect(ex, vy, ex + BtnS, vy + BtnS));
            // 🔴 **2026-10-03 就地更正**：这里**不变**常态图 —— 直接读原版 prefab
            //（`bundle_scenes_scenes_mainmenuwarpforge` → `Card Displayer Menu For Menu`）：
            // **`Show Card Text` = `40k_UI_bt_eye`（256×256）· HL = `40k_UI_bt_eye_hover` · P = `40k_UI_bt_eye_pressed`**
            // ⇒ 我们画的这张**本来就是对的**（普查 §块 1 第 14 行同值），只是**一直没接换图**。
            // ⚠️ 别和 **`40k_bt_eye`（88×87，另一张）** 混 —— 那张是 `LiveOpsEventWindow` 卡组四圆钮用的。
            var eyeQ = MenuDraw.Rect(_eyeBox, CardArt.Ui("40k_UI_bt_eye"),
                          new PxRect(ex, vy, ex + BtnS, vy + BtnS), "Image", QCdRow, null, true);
            Hit(_eyeBox, "ShowTextHit", new PxRect(ex, vy, ex + BtnS, vy + BtnS), ToggleLore, QCdHit,
                eyeQ, "40k_UI_bt_eye");
            RefreshLore();
            RefreshButtons();
            if (CardArt.Ui("40k_UI_bt_voicelines") == null)
                Debug.Log("[CardDetail] `40k_UI_bt_voicelines` 不在 `Resources/Art/ui/` ⇒ **语音钮画不出来**（不摆白方块）");

            // 4) 三块面板
            var panel = MenuDraw.Node(root, "Card Options Panel", new PxRect(0f, 0f, 1920f, 1080f));
            var vlg = MenuDraw.Node(panel, "Panel", new PxRect(PanL, PanT, PanR, PanB));
            // 🔴 **2026-09-27 用户拍板：本作【不做升级、不做合成】⇒ 两块面板都整块不建。**
            //   理由（用户原话）：「直接全部卡都是最高级别的卡框，这样就不用升级了。也不需要合成卡牌了。」
            //   · **升级**：`CardArt.TierOf` 已改成**所有卡一律最高档**（`MaxTier`）⇒ **没有可升的**；
            //     而且我们这侧升级**从来就没改过卡面**（`CardView` 全程不读等级）—— 留着就是个空按钮。
            //   · **合成**：本作全解锁（资源固定 9999、卡池全开、`CardProgress.Owned` 直接给足
            //     `卡组上限 + 升满所需`）⇒ **没有要合的**。
            //   ⚠️ **原版这两块是有的** —— 这是**用户明确要的偏离**（同「全解锁」那条边界）。
            //   ⚠️ **要恢复**：把下面那两行注释掉的条件去掉即可（`BuildCrafting` / `BuildUpgrade` 都还在）。
            //   📌 顺带：`Craftable()` / `WildcardIconFor()` 是上一轮为「创建副本」做的判据（含四档稀有度映射），
            //     面板停掉后**暂时没有调用点** —— **保留不删**，恢复那块面板时直接用。
            Debug.Log("[CardDetail] 本作不做升级/合成（用户 2026-09-27 拍板）⇒ 「创建副本」「升级」两块面板都不建");
            BuildAltArt(vlg);
            BuildCounter(panel);
            BuildWildcards(panel);
        }

        // ============================================================ 卡片那一叠（扇形 + 相关卡）

        // 🔴 **命中区的判据只此一份 → `CardFan`**（原版 `CardUI/2DCard/UI Collider` 的
        //    `m_SizeDelta(-0.2,-0.44)` + `m_AnchoredPosition.y=-0.02` ⇒ **两轴比例不同 + 中心下移 5px**）。
        //    ⚠️ **不要再在本文件里放一个 `HitRatio` 常量** —— 2026-10-07 之前那只 `0.9043` 是 **x** 那个比，
        //    双轴同用 + 居中 ⇒ y 轴多吃两条窄带（A156）。用 `CardFan.HitW/HitH/HitCy`。

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据与真值 → `CardFan`）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildCardStack(Transform cardNode)
        {
            var cards = new List<CardDef>();
            if (Card != null) cards.Add(Card);
            // 相关卡：判据只此一份（`RelatedCards.Find`）—— 与原版那 4 个 `relatedCard1..4` 字段的关系见那个文件头
            cards.AddRange(RelatedCards.Find(Card, Slots - 1));
            SlotCount = Mathf.Min(cards.Count, Slots);
            for (int i = 0; i < Slots; i++)
            {
                SlotViews[i] = null; _slotDefs[i] = null;
                if (i >= SlotCount) continue;
                var def = cards[i];
                float w = CardFan.Wpx(i), h = CardFan.Hpx(i);
                float cx = CardFan.Cx(i), cy = CardFan.Cy(i);           // 屏坐标（左上原点；原版 pos.y 向上 ⇒ 取负）
                var px = new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
                var node = MenuDraw.Node(cardNode, SlotName(i), px);

                // 🔴 **命中区【先建】、且【不看 `CardView.Create` 的返回值】**（A157，2026-10-07）：
                //   原来 `if (v == null) { …; continue; }` 排在下面 ⇒ `CardView.Create` 失败的那一格
                //   **连命中区一起没有**，点上去会穿透它落到压暗层 `BackgroundHit` 上**把窗关掉**。
                //   原版每一格都挂自己的 `UI Collider`（吃射线的是它，不是卡面那些图）——
                //   **「这一格点不点得到」与「这一格的卡面画不画得出来」是两件事**：
                //   卡面画不出来时，玩家的那一下仍然该被这一格吃掉（回调 `SwapToFront` 自己会闸掉），
                //   ⛔ 不能让一次渲染失败变成「点卡片 = 关窗」。⇒ 命中区与 `CardView` 解耦。
                // ⚠️ 卡面那张建不出来时**照旧出声**（`LogWarning`，不静默）—— 见下面那一支。
                BuildCardHit(node, i);

                var cd = BattleDriver.ToCardData(def, def.Faction);
                var v = CardView.Create(node, cd, SlotName(i));
                if (v == null)
                {
                    Debug.LogWarning("[CardDetail] 第 " + i + " 格（" + def.Name + "）的 `CardView` 建不出来"
                                   + " —— 那一格**没画卡面，但命中区照样在**（点它不会关窗，出声）");
                    continue;
                }
                v.gameObject.SetActive(true);
                v.SetPose(MenuDraw.Local(node, px.x1, px.y1, px.x2, px.y2), CardFan.Rot(i),
                          h / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                // **前台白 / 相关卡压暗到 0.65**（原版 `cardInBackGroundColorTint`）——
                // ⚠️ **必须排在 `SetHighlight` 之后**（那个自己也写 `SetTint`，谁后调谁生效）。
                CardFan.ApplySlotTint(v, i == 0);
                SlotViews[i] = v; _slotDefs[i] = def;
            }
            // 🔴 **每格一个独立的渲染队列**（越靠前台越高）—— 判据与坑（为什么必须走 `.material`）→ `CardFan`。
            //    ⚠️ **这一段必须落在【遮罩之上】**（`QShade = 3105`）：原来卡格在 `3009–3017`、遮罩在 `3110`
            //    ⇒ **整叠卡被压暗了一半**（2026-09-28 实拍量出来的，见本文件头部那段订正）。
            CardFan.ApplyQueues(SlotViews, SlotCount, QCardStack);
            _swapping = false;
            Debug.Log($"[CardDetail] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 扇形：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（正本 §8·7）");
        }

        /// <summary>第 `i` 格的点击区（原版 `CardUI/2DCard/UI Collider` —— **每格一个**），
        /// 矩形判据只此一份 → `CardFan.HitW/HitH/HitCy`。
        /// **前台那格也要**：它的回调在闸②直接 return，作用是「吃掉这一下」，
        /// 否则会落到 `BackgroundHit` 上**把窗关掉**；<b>与卡面有没有画出来无关</b>（A157）—— 见调用点的注释。
        /// 🔴 z 阶梯：`PointerLayer.HitButton` 同队列时**取 z 小的那个** ⇒ 越靠前台 z 越小。</summary>
        void BuildCardHit(Transform node, int i)
        {
            float hy = CardFan.HitCy(i);                       // 点击区中心**比卡心低**（原版 `ap.y = −0.02`）
            float hw = CardFan.HitW(i) * 0.5f, hh = CardFan.HitH(i) * 0.5f;
            float fx = CardFan.Cx(i);
            var hx = new PxRect(fx - hw, hy - hh, fx + hw, hy + hh);
            var hit = Hit(node, "CardHit " + i, hx, () => SwapToFront(i));
            if (hit != null)
                hit.localPosition = new Vector3(hit.localPosition.x, hit.localPosition.y, i * HitZStep);
        }

        // 🔴 **「相关卡是谁」那段（点名那一支 + 池子那一支）已收口到 `Core/RelatedCards.cs`**
        //    —— 两扇窗共用一份。那里也写着**原版其实走的是卡上自带的 `relatedCard1..4` 四个字段**
        //    （值在服务端，本地拿不到 ⇒ 我们这套是替身），判据全文 → 正本 §十·1。

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**；我们没有 lore 字段 ⇒ 画**效果文字**（出声）。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。
        /// 🔴 **2026-09-28：原版是【居中】不是右对齐** —— `LoreText` 的 TMP
        /// `m_HorizontalAlignment = 2`（Center；位标志 Left=1/Center=2/Right=4），实拍也一致
        /// （正本 §十·5 第 4 条）。`MenuDraw.Text` 默认居中 ⇒ **不要**再 `AlignRight`。</summary>
        void BuildLoreText(Transform bg)
        {
            if (bg == null) return;
            for (int i = bg.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(bg.GetChild(i).gameObject);
            var def = FrontDef ?? Card;
            // ① 底板：**按阵营**选图（原版 `FlavourTextSO.GetClanFlavorBackground`）—— 认的是**当前前台**那张；
            //    换位收尾会重建本方法 ⇒ 阵营跟着换。⚠️ 原版 `m_PreserveAspect = 0`（实读）⇒ **拉伸**，别开 keepAspect。
            var tex = CardArt.FlavorBg(def != null ? def.Faction : null);
            if (tex != null)
                MenuDraw.Rect(bg, tex, new PxRect(LowerL, LoreBgT, LowerR, LoreBgB), "Image", QCdRow, null, false);
            else
                Debug.LogWarning("[CardDetail] 阵营「" + (def != null ? def.Faction : "?")
                               + "」没有风味底图（`CardArt.FlavorBg` 取不到）⇒ **只画字**（不静默）");
            // ② 那行字
            string lore = def != null ? BattleDriver.FaceTextFull(def).body : "";
            var r = new PxRect(LoreL, LoreT, LoreR, LoreB);
            MenuDraw.Text(bg, r, lore, Color.white, "LoreText", LorePx, QCdText, LoreR - LoreL, 10f);
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`，
        /// 253 行逐行读过 —— 判据全文 → 正本 §9·6）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ 有 `Animation` 在播。
        /// ⚠️ 闸③ **本工程不适用** —— 原版那条摆位用的 `Animation` 我们没有（我们按 `CardFan` 的常量直接摆），
        /// 这一点是**如实说明的偏离**。
        /// ⚠️ 层级对调照原版是 `SetSiblingIndex` **两两互换**（**不是「提到最前」**）；
        /// 我们**另外**还要按新槽位重排**渲染队列**（这套的前后由队列决定，见 `CardFan`）。
        /// ⚠️ 收尾还要**跟着换着色**（原版 `ChangeCardPosition.c:203-221`）。</summary>
        public void SwapToFront(int idx)
        {
            if (_swapping)
            {
                Debug.Log("[CardDetail] 换位被挡：上一次还没播完（原版闸① `swappingCards`）");
                return;
            }
            if (idx <= 0 || idx >= Slots) return;                  // 闸②：点的是前台自己（位姿槽 0）
            var a = SlotViews[0]; var b = SlotViews[idx];
            if (a == null || b == null) return;
            _swapping = true;

            var pa = a.transform.position; var pb = b.transform.position;
            float ra = a.transform.eulerAngles.z, rb = b.transform.eulerAngles.z;
            float sa = a.transform.localScale.x, sb = b.transform.localScale.x;

            // 六条 tween 全用同一个时长（原版 `relatedCardSwapTime` = 0.25s）：
            // 被点那张 → 前台位；前台那张 → 被点卡的槽位。
            CardTween.ToPose(b.transform, pa, ra, sa, SwapTime, DG.Tweening.Ease.OutQuad).OnComplete(() =>
            {
                // 层级对调：**两两互换** sibling 序（照原版 `SetSiblingIndex`）
                int ia = a.transform.GetSiblingIndex(), ib = b.transform.GetSiblingIndex();
                a.transform.SetSiblingIndex(ib);
                b.transform.SetSiblingIndex(ia);
                // 记账：两个**位姿槽**上的卡对调（前台永远是位姿槽 0）
                var td = _slotDefs[0]; _slotDefs[0] = _slotDefs[idx]; _slotDefs[idx] = td;
                var tv = SlotViews[0]; SlotViews[0] = SlotViews[idx]; SlotViews[idx] = tv;
                // 🔴 **队列要按【新的槽位】重排**（sibling 序换了、我们这套的**前后由渲染队列决定**）
                //    —— 不重排的话「刚换到前台的那张」还带着原来那个较低的号、会被身后的卡盖住。
                CardFan.ApplyQueues(SlotViews, SlotCount, QCardStack);
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：开闸（上面已做）· 更新前台指针（上面已做）·
                // **重设 lore**（`SetCardLore`）· **重设语音 / 眼睛钮**（`SetCardVoiceOver` 那两条）。
                BuildLoreText(Find(transform, "FlavourTextBG"));
                RefreshLore();
                RefreshButtons();
                Debug.Log("[CardDetail] 换位完成：前台现在是「" + (FrontDef != null ? FrontDef.Name : "?")
                        + "」（收尾重设了 lore / 语音 / 眼睛钮 —— 原版 `CardSwapFinished`）");
            });
            CardTween.ToPose(a.transform, pb, rb, sb, SwapTime, DG.Tweening.Ease.OutQuad);
            // 着色跟着换（原版 `ChangeCardPosition.c:203-221` 那两条 tween，同一时长）：
            // 去前台那张 → 白，让出前台那张 → `cardInBackGroundColorTint`(0.65)。
            CardFan.TweenTint(b, CardFan.BackTint, CardFan.FrontTint, SwapTime);
            CardFan.TweenTint(a, CardFan.FrontTint, CardFan.BackTint, SwapTime);
            Debug.Log($"[CardDetail] 换位：第 {idx} 格 ⇄ 前台（{SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        /// <summary>这张卡**能不能「创建副本」（合成）** —— 决定那块面板建不建。
        ///
        /// 🔴 **2026-09-27 用户指出：`special` 那批不能合成**。而那 50 张**不是「第五档稀有度」**，
        ///   是 **防御卡 39 + 药剂卡 6（`Combat Elixir`）+ 秘仪 5（`Secret`）** 在源数据里
        ///   **没有稀有度被塞的占位值**（`cards_engine.json` 实测：`special` 全是 `type=defence/tactic`）。
        ///   ⇒ **判据 = 稀有度是不是那四档之一**（与 `WildcardIconFor` 同一张表）。
        ///
        /// ⚠️ **原版运行时长什么样，关服查不到**（可能整块隐藏、也可能是个灰掉的按钮）
        ///   ⇒ 这里选**整块不建**，**不造一个假的禁用态**（同 `CLAUDE.md`「宁可没有，不可错着显示」）。
        ///   真要改成「建出来但点不动」是一行的事 —— 先把口径记在这儿。</summary>
        static bool Craftable(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common": case "rare": case "epic": case "legendary": return true;
                default: return false;
            }
        }

        // ---- ① 创建副本 ----
        /// <summary>万能卡图标 —— **四档稀有度各一张**（原版图集 `40k_general_wildcard_*_small`）。
        /// 🔴 **稀有度是四档**（`common/rare/epic/legendary`）—— 卡池里那个 `special`
        ///   （**防御卡 39 + 战术卡 11**，`cards_engine.json` 实测）**不是第五档**，
        ///   是「这张卡没有稀有度」被源数据塞了个占位值（2026-09-27 用户指出、逐条核过）。
        /// 🔴 **判据只此一处** —— 通配符计数条（`BuildWildcards`）与「创建副本」按钮的图标（`BuildCrafting`）共用它。</summary>
        static readonly string[] WildcardIcons =
        {
            "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
            "40k_general_wildcard_epic_small",   "40k_general_wildcard_legendary_small",
        };

        /// <summary>按稀有度取万能卡图标。**认不出时返回原版 prefab 的默认那张** ——
        /// 原版 `Craft/WC icon` 的 `m_Sprite` 存的就是 `40k_general_wildcard_epic_small`，
        /// 而 `CraftingPanel` 上有 `wildcardIcon` / `wildcardSprites` 两个字段
        /// ⇒ **运行时是脚本按卡换的**；**防御卡那 39 张运行时画哪张，关服查不到**
        /// ⇒ 不猜，用 prefab 的原值，并如实说出来。</summary>
        static string WildcardIconFor(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common":    return WildcardIcons[0];
                case "rare":      return WildcardIcons[1];
                case "epic":      return WildcardIcons[2];
                case "legendary": return WildcardIcons[3];
                default:          return WildcardIcons[2];   // = prefab 原值（`_epic_small`）
            }
        }

        void BuildCrafting(Transform panel)
        {
            var p = PanelBox(panel, "Crafting Panel", CraftT, CraftB);
            Title(p, "Crafting Title", "Create a copy of this card", 1307.5f, 165.17f, 1726f, 244.29f, 42f);
            var b = new PxRect(1384f, 244.29f, 1654f, 304.95f);
            // ⚠️ `UI_Button_Mulligan` **不是九宫格图**（实读 `Sprite/UI_Button_Mulligan.json`：`m_Border = None`，
            //    图 410×124）—— 原版标 `type=Sliced` 但**没有 border 就等于拉伸** ⇒ 我们直接走 `Rect`。
            //    （走 `Nine` 会吐一串「border 比图还大，退回单块」的警告，而结果一样。）
            MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), b, "Craft Bg", QCdRow);
            MenuDraw.Text(p, new PxRect(b.x1, b.y1, b.x1 + 122.88f, b.y2), "1", Color.white, "Craft Text", 40f, QCdText);
            // 图标原版 `scl=1.5`、落在 1506.88..1654
            // 🔴 **2026-09-27 修：这里原来画的是 `40k_main_collection_icon`（卡池/收藏图标）—— 取错图了。**
            //    原版那颗叫 **`WC icon`**（万能卡图标），`menu_dump` 实读：
            //      `…/Crafting Panel/…/Craft/WC icon` = `40k_general_wildcard_epic_small` 41×51
            //      · **Simple + preserveAspect** · 框 135×50.66 · pivot(0,0.5) ⇒ 实绘 40.7×50.66
            //    同格下面那行字就是 `This will consume a wildcard`（消耗一张万能卡）⇒ 画万能卡图标才对。
            float ix = 1506.88f, iy = b.y1 + 10f, iw = 147.12f, ih = b.y2 - b.y1 - 20f;
            MenuDraw.Rect(p, CardArt.MenuUi(WildcardIconFor(Card.Rarity)), new PxRect(ix, iy, ix + iw, iy + ih),
                          "Craft Icon", QCdRow, null, true);
            MenuDraw.Text(p, new PxRect(1307.5f, 315.5f, 1726f, 378.79f), "This will consume a wildcard",
                          Color.white, "Craft Explanation", 40f, QCdText);
            MenuDraw.Rect(p, CardArt.MenuUi("40K_generic_bt_info"), new PxRect(1740.65f, 161.91f, 1782f, 203.26f),
                          "Craft Info Icon", QCdRow, null, true);
            Hit(p, "CraftHit", b, DoCraft, QCdHit);
        }

        // ---- ② 升级 ----
        void BuildUpgrade(Transform panel)
        {
            var p = PanelBox(panel, "Upgrade Panel", UpgT, UpgB);
            Title(p, "Upgrade Title", "Upgrade this card\nto level " + Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel),
                  1307.5f, 437.28f, 1726f, 515.77f, 41.4f);
            var b = new PxRect(1384f, 524.96f, 1654.38f, 585.31f);
            MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), b, "Upgrade Bg", QCdRow);
            // `Upgrade` 里那条：副本数（`cards` HLG）+ 金币（`cost` HLG）
            bool can = CardProgress.CanUpgrade(Card.Id, Card.Rarity);
            int need = CardProgress.NeedCopies[Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel)];
            MenuDraw.Text(p, new PxRect(b.x1, b.y1, 1443.83f, b.y2), need.ToString(), Color.white, "Upgrade Need", 40f, QCdText);
            MenuDraw.Rect(p, CardArt.MenuUi("40k_general_icon_card_amount"), new PxRect(1450f, b.y1 + 8f, 1490f, b.y2 - 8f),
                          "Upgrade Card Icon", QCdRow, null, true);
            MenuDraw.Text(p, new PxRect(1499.72f, b.y1, 1570f, b.y2),
                          CardProgress.NeedGold[Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel)].ToString(),
                          Color.white, "Upgrade Cost", 40f, QCdText);
            MenuDraw.Rect(p, CardArt.MenuUi("40k_topmarquee_currency_gold"), new PxRect(1578f, b.y1 + 8f, 1643f, b.y2 - 8f),
                          "Upgrade Gold Icon", QCdRow, null, true);
            MenuDraw.Text(p, new PxRect(1309f, 594.53f, 1729f, 659.33f),
                          "Will get: +" + CardProgress.Points[Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel)],
                          Color.white, "Upgrade Explanation", 40f, QCdText);
            MenuDraw.Rect(p, CardArt.MenuUi("40K_generic_bt_info"), new PxRect(1740.65f, 426.42f, 1782f, 467.77f),
                          "Upgrade Info Icon", QCdRow, null, true);
            Hit(p, "UpgradeHit", b, DoUpgrade, QCdHit);

            // `No Upgrade Warning`：**满级才露**（原版那件与 `Content` 同级、同 rect）
            var w = MenuDraw.Node(p, "No Upgrade Warning", new PxRect(PanL, UpgT, PanR, UpgB));
            MenuDraw.Text(w, new PxRect(1314f, 430.62f, 1724f, 644.92f), "Maximum card tier reached",
                          Color.white, "No Upgrade Title", 40f, QCdText, 410f, 25f);
            w.gameObject.SetActive(!can && CardProgress.Level(Card.Id) >= CardProgress.MaxLevel);
        }

        // ---- ③ 异画（Alternate Art）----
        void BuildAltArt(Transform panel)
        {
            var p = PanelBox(panel, "Alternate Art Panel", AltT, AltB);
            // ⚠️ 原版这一格的 `Title` 印的是**升级文案**（复制粘贴 bug）⇒ **我们不抄那个 bug**，
            //    照这一格自己的意思写（**出声**）。
            var has = CardArt.AltArt(Card.Id) != null;
            Title(p, "AltArt Title", has ? "Alternate art (owned)" : "Alternate art", 1307.5f, 690f, 1726f, 760f, 31.7f);
            MenuDraw.Text(p, new PxRect(1307.5f, 762f, 1726f, 800f),
                          has ? "1 of 1" : "0 of 0", Color.white, "Current Style", 28f, QCdText);
            // 🔴 **异画绑死在那张卡上**（`AlternateArtCard.GetIdForClonedCard` 对原卡 id 做一次 `String.Replace`）
            //    ⇒ 这张卡没有异画就是没有，不给别的督军换。本地只有 7 张督军异画。
            float bw = 328.71f, bh = 73.38f;
            float bx = PanL + (PanR - PanL - bw) * 0.5f, by = 820f;
            MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), new PxRect(bx, by, bx + bw, by + bh),
                          "Buy Bg", QCdRow);
            MenuDraw.Text(p, new PxRect(bx, by, bx + bw, by + bh), has ? "Alternate art" : "No alternate art",
                          Color.white, "Buy Original Card", 32f, QCdText);
            MenuDraw.Rect(p, CardArt.MenuUi("WF_Lock_Icon_Simple"), new PxRect(1310f, 690f, 1360f, 740f),
                          "Lock Icon", QCdRow, null, true);
        }

        // ---- 计数条（`Card Counter`，原版 §三）----
        void BuildCounter(Transform panel)
        {
            var c = MenuDraw.Node(panel, "Card Counter", new PxRect(825.07f, 829.81f, 1094.93f, 909.19f));
            // ⚠️ 原版 `Single Counter`（无副本时用）与这套**互斥**；判据 = `spares > 0`
            bool dup = CardProgress.Spares(Card.Id, Card.Rarity) > 0;
            var box = MenuDraw.Node(c, dup ? "Duplicate Counter" : "Single Counter", new PxRect(CcBgL, CcBgT, CcBgR, CcBgB));
            MenuDraw.Rect(box, CardArt.MenuUi("40K_main_deck_card_counter"), new PxRect(CcBgL, CcBgT, CcBgR, CcBgB),
                          "Background", QCdRow, null, true);
            int cap = CardProgress.DeckCap(Card.Rarity);
            int owned = CardProgress.Owned(Card.Id, Card.Rarity);
            int inDeck = Mathf.Min(owned, cap);
            int spares = owned - inDeck;
            MenuDraw.Text(box, new PxRect(CcX1L, CcY1, CcX1R, CcY2), "x" + inDeck, Color.white, "Counter",
                          CcX1Px, QCdText, 0f, 25f);
            if (dup)
            {
                MenuDraw.Text(box, new PxRect(CcSlL, CcY1, CcSlR, CcY2), "/ ", Color.white, "Slash", CcSlPx, QCdText, 0f, 25f);
                MenuDraw.Text(box, new PxRect(CcX2L, CcX2T, CcX2R, CcX2B), spares.ToString(), Color.white,
                              "Duplicates text", CcX1Px, QCdText, 0f, 18f);
                MenuDraw.Rect(box, CardArt.MenuUi("40k_general_icon_card_amount"),
                              new PxRect(CcImL, CcImT, CcImR, CcImB), "Duplicate image", QCdRow, null, true);
            }
        }

        // ---- 通配符条（`Wildcard Segment` + `WIldcard Counter`）----
        void BuildWildcards(Transform panel)
        {
            MenuDraw.Rect(panel, CardArt.MenuUi("40k_topmarquee_currency_display_BW"),
                          new PxRect(WcBgL, WcBgT, WcBgR, WcBgB), "Wildcard Bg", QCdRow);
            for (int i = 0; i < WildcardIcons.Length; i++)
            {
                float x = WcX0 + WcStep * i;
                MenuDraw.Rect(panel, CardArt.MenuUi(WildcardIcons[i]), new PxRect(x, WcBgT, x + 30f, WcBgB),
                              "Wildcard Icon " + i, QCdRow, null, true);
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `WIldcard Counter/Counters/*/Icon`
                //   4 件全是 PA=1 + Simple（RT1211/1354/1344/1583），贴图 **42×51 / 41×51** 塞进 30×44
                //   ⇒ 原版实绘 **30×36.4（37.3）**，我们拉伸成 30×**44** ⇒ **高 ×1.18~1.21**。
                //   ⚠️ 同一件在收藏窗（`CollectionWindow.cs:318`）与卡组编辑（`DeckRuntime.cs:359`）也是同一错，三处一起修。
                // 🔴 **2026-09-28 用户拍板：万能卡数字一律恒定 `99`**（原来这里写 `9999`）。
                //    原版这四个数是 `WildcardDisplay` 的库存直出、跟着**指针悬停那张卡**的阵营走；
                //    单机没有发放源 ⇒ 三处（详情窗 / 收藏窗 / 卡组编辑）统一写 99。
                //    判据 → `项目任务.md` §三 第 15 条 第 29 项。
                MenuDraw.Text(panel, new PxRect(x + 30f, WcBgT, x + 71f, WcBgB), "99", Color.white,
                              "Wildcard Count " + i, WcPx, QCdText);
            }
            var fac = DeckRuntime.FactionIcon(Card.Faction);
            if (!string.IsNullOrEmpty(fac))
                MenuDraw.Rect(panel, CardArt.MenuUi(fac), new PxRect(1470f, 71f, 1550f, 156f),
                              "Army Icon", QCdRow, null, true);
        }

        // ============================================================ 行为

        /// <summary>点遮罩以外的东西不该关窗 —— 这条给自检/上层用。</summary>
        public void ToggleLore()
        {
            LoreVisible = !LoreVisible;
            RefreshLore();
            Debug.Log("[CardDetail] `Show Card Text` ⇒ 效果文字条 " + (LoreVisible ? "显示" : "藏起"));
        }
        void RefreshLore()
        {
            var bg = Find(transform, "FlavourTextBG");
            if (bg != null) bg.gameObject.SetActive(LoreVisible);
        }

        /// <summary>下缘那两颗钮**按卡决定建不建**（原版 `ShowCard` 与 `CardSwapFinished` 里那两条
        /// `SetActive`）。判据只此一份 → `CardButtons`（那里写了原版出处与我们的等价物：
        /// **语音钮** = 这张卡有没有声音；**眼睛钮** = `type` 是 `unit`/`hero`）。
        /// ⚠️ 换位收尾要跟着换 —— 认的是**新前台**那张（原版 `CardSwapFinished` 同款）。</summary>
        void RefreshButtons()
        {
            if (_voiceBox != null) _voiceBox.gameObject.SetActive(HasCard && CardButtons.HasVoice(Card));
            if (_eyeBox != null) _eyeBox.gameObject.SetActive(HasCard && CardButtons.HasTextButton(Card));
        }

        /// <summary>创建副本：**拥有数 +1**。⚠️ 单机资源固定 9999 ⇒ **不扣万能卡**，出声。</summary>
        public void DoCraft()
        {
            CardProgress.AddOwned(Card.Id, Card.Rarity, 1);
            Debug.Log("[CardDetail] 创建副本：「" + Card.Name + "」拥有数 → "
                      + CardProgress.Owned(Card.Id, Card.Rarity)
                      + "（单机版**不扣万能卡**，出声；原版 `MenuDeck/Craft/*` 那套走服务端）");
            RebuildKeepingState();
        }

        /// <summary>升级：够副本才升（**只吃多余副本**，同原版 `拥有数 >= 卡组上限 + 需要张数`）。</summary>
        public bool DoUpgrade()
        {
            if (CardProgress.Level(Card.Id) >= CardProgress.MaxLevel)
            {
                Debug.Log("[CardDetail] 已经满级（" + CardProgress.MaxLevel + " 级）⇒ 升不了");
                return false;
            }
            if (!CardProgress.CanUpgrade(Card.Id, Card.Rarity))
            {
                Debug.Log("[CardDetail] 副本不够 ⇒ 升不了（多余副本 "
                          + CardProgress.Spares(Card.Id, Card.Rarity) + " < 需要 "
                          + CardProgress.NeedCopies[CardProgress.Level(Card.Id) + 1] + "）");
                return false;
            }
            CardProgress.SetLevel(Card.Id, CardProgress.Level(Card.Id) + 1);
            Debug.Log("[CardDetail] 升级：「" + Card.Name + "」→ 等级 " + CardProgress.Level(Card.Id)
                      + "（**不扣货币**：单机资源固定 9999；升级表数值**是我们编的**）");
            RebuildKeepingState();
            return true;
        }

        /// <summary>播这张卡的单位语音（复用 `VoiceLines` —— **只此一处**，不另写后缀表）。
        /// ⚠️ 认的是**前台那张**（`FrontDef`）—— 原版换位收尾会 `SetCardVoiceOver(新前台)`
        /// （判据 → 正本 §9·6 ⑤），所以点了相关卡之后这颗钮该播**新前台**的语音。</summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            var c = FrontDef ?? Card;
            if (c == null) return false;
            string key = !string.IsNullOrEmpty(c.Id) ? c.Id : c.Name;
            if (!VoiceLines.Has(key))
            {
                Debug.Log("[CardDetail] 「" + c.Name + "」没有单位语音（`VoiceLines.Has` 为假）—— 这次没声音");
                return false;
            }
            string file, text;
            if (!VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return false;
            WarpforgeVFX.WFSoundPlayer.Play(clip, true, 1f, 1f);
            LastVoiceFile = file;
            return true;
        }

        void RebuildKeepingState()
        {
            Build();
            LastOpened = this;
        }

        // ============================================================ 小工具

        Transform PanelBox(Transform panel, string name, float y1, float y2)
        {
            var p = MenuDraw.Node(panel, name, new PxRect(PanL, y1, PanR, y2));
            // 🔴 **原版这里确实是 `UI_Deck_Information_Back` + `type=Sliced`**（2026-09-24 实读
            //    `工具/menu_dump.py … "Crafting Panel"` 确认），但那块面板只有 **450 宽**，
            //    而这张图的 `m_Border=(42,363,655,81)` 左+右 = **697 > 450** ⇒ **九宫格根本放不下**
            //    （原版 Unity 会把它挤成「左右边框各占一半」，我们这套 `CreateNineSlice` 会**退回单块**）。
            //    ⇒ 我们**显式选「单块拉伸」**（不静默、也不吐一串警告），**如实记成一处偏差**，
            //    见 `项目资料` 那边的 §三 第 15 条。
            var tex = CardArt.MenuUi("UI_Deck_Information_Back");
            if (tex == null) Debug.LogWarning("[CardDetail] 面板底图取不到：UI_Deck_Information_Back");
            else MenuDraw.Rect(p, tex, new PxRect(PanL, y1, PanR, y2), "Background", QCdRow);
            return p;
        }

        void Title(Transform p, string nodeName, string text, float x1, float y1, float x2, float y2, float px)
        {
            // 限宽换行 + 自适应（原版这几处都是 `auto(min-max)` 的 TMP）
            MenuDraw.Text(p, new PxRect(x1, y1, x2, y2), text, Color.white, nodeName, px, QCdText, x2 - x1, 10f);
        }

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QCdHit,
                      ImageQuad target = null, string art = null, string hoverArt = null)
        {
            var hit = MenuDraw.Node(parent, name, r);
            var quad = ImageQuad.Create(hit, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(r.H),
                                        new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt);
            return hit;
        }
        Transform Hit(Transform parent, string name, PxRect r, System.Func<bool> onClick, int q)
        {
            return Hit(parent, name, r, () => { onClick(); }, q);
        }
    }
}
