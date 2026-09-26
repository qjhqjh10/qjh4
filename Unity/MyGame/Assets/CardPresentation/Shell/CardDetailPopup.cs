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
        public const int QCd = 3110, QCdRow = 3111, QCdText = 3112, QCdHit = 3113;

        // ---- 几何（正本 §一～§三 逐条）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        public const float CardL = 584f, CardT = 106f, CardR = 1336f, CardB = 974f;          // `Card Display` 752×868
        /// <summary>卡本体 **523.25×832.75**（`CardUI (4)` scale 250 × 单位 2.093×3.331）。</summary>
        public const float CardW = 523.25f, CardH = 832.75f;
        public const float LowerT = 921.34f, LowerB = 1058.34f, LowerL = 300f, LowerR = 1620f;
        public const float LoreBgT = 900.84f, LoreBgB = 1078.84f;
        public const float LoreL = 335f, LoreT = 943.11f, LoreR = 1585f, LoreB = 1036.58f, LorePx = 35f;
        /// <summary>两个圆钮各 **88.655²**（`pivot` 在 LowerSection 内水平两端 ±734.33）。</summary>
        public const float BtnS = 88.655f;
        public const float VoiceCx = 1694.33f, EyeCx = 225.67f, BtnCy = 989.84f;
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
        // 🔴 **扇形参数**：槽 0–4 是**原版真值**，来自原版那条 legacy clip **`Card Display Open`**
        //    （`bundle_duplicateassetisolation_assets_all/AnimationClip/AnimationClip_1560242982351263260.json`，
        //    由 `CardDisplayWindow.displayAnimation`(Animation 623) 自动播放 —— 判据全文 → 正本 **§8·7**）。
        //    ⚠️ 这 5 个数**不在 prefab 里**（prefab 里 5 个槽是**完全重合**的），只在 clip 的关键帧里。
        // ⚠️ 槽 5–8 是**我们外推的**（原版只有 5 格；用户 2026-09-26 授权做 8 个相关卡槽）：
        //    走向照原版（继续往左、继续变斜、继续缩小），**步长取原版最后一档的一半**（否则跑出屏幕左缘）。
        //    单位 = 原版 `anchoredPosition`（x 右正 · y 上正，原点 = 屏心 (960,540)）；`scale` = 原版 `localScale`
        //    （250 = 卡高 832.75px ⇒ 我们的 `CardView` 缩放要再除以 108，见下面 `k`）。
        static readonly float[] FanX = { 0f, -121f, -232f, -332f, -432f, -482f, -532f, -582f, -632f };
        static readonly float[] FanY = { 60f, 53f, 49f, 45f, 27f, 24f, 21f, 18f, 15f };
        static readonly float[] FanRot = { 0f, 2.510f, 5.890f, 12.648f, 19.200f, 22.5f, 25.8f, 29.1f, 32.4f };
        static readonly float[] FanScale = { 250f, 232.954f, 221.593f, 204.546f, 181.815f, 170.8f, 159.8f, 148.8f, 137.8f };

        /// <summary>卡位个数：**1 主卡 + 8 相关卡**（原版是 1+4；**8 这一档是我们挑的**，用户授权的冗余）。</summary>
        public const int Slots = 9;
        /// <summary>换位动画时长 —— 原版 `CardDisplayWindow.relatedCardSwapTime`（`+0xC0`，`.ctor` 写 `0x3e800000` = **0.25s**）。</summary>
        public const float SwapTime = 0.25f;
        /// <summary>槽名（照原版命名：**前台那张不带后缀**，其余 `CardUI (1..8)`）。</summary>
        public static string SlotName(int i) { return i == 0 ? "CardUI" : "CardUI (" + i + ")"; }
        /// <summary>命中区所在的 z 阶梯（**只给命中区用** —— 它是 alpha=0 的透明件，不影响画面；
        /// `PointerLayer` 同队列时**取 z 小的那个**，所以前台那张要最小）。</summary>
        const float HitZStep = 0.08f;
        /// <summary>卡片那一叠的**队列基数**（第 0 格拿到最高那号；这段号是空的，见 `BuildCardStack`）。</summary>
        const int QCardBase = 3009;

        /// <summary>把**整格卡**的所有层搬到同一个队列 `q`（卡内层序靠 z 偏移，整体平移不破坏它）。
        /// 为什么要它：`CardView` 每层都写死 3000 ⇒ 多格叠加时只剩「到相机的距离」在排序，实测会错乱。
        ///
        /// 🔴 **一律走 `.material`（不是 `.sharedMaterial`）** —— Unity 的 `.material` getter
        ///    会**为这个渲染器实例化一份**，所以**绝不会写到共享材质上**。
        ///    2026-09-27 实测踩过：文字那层的 `sharedMaterial` **就是字体资产里那份材质**
        ///    （TMP 的 MeshRenderer 挂在 `Label` 的**子物体**上），直接写它会把
        ///    `Resources/Fonts/Warpforge Trait TextSprites.asset` 的 `m_CustomRenderQueue` 写成 `3009`
        ///    并**落盘**（`git status` 里挂出来）—— 而且**改共享材质等于改所有用同一字体的文字**。
        /// ⚠️ 点阵后端（`Label` 自己挂 MeshRenderer）交给 `Label.SetRenderQueue`（它也会先实例化）。</summary>
        static void SetCardQueue(CardView v, int q)
        {
            if (v == null) return;
            foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.GetComponent<Label>() != null) continue;   // 点阵文字：走下面那个（它自己实例化）
                if (mr.sharedMaterial != null) mr.material.renderQueue = q;
            }
            foreach (var lb in v.GetComponentsInChildren<Label>(true)) lb.SetRenderQueue(q);
        }

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
            if (!HasCard) return;

            // 1) 压暗 + 点遮罩关窗（原版唯一的「关」）
            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(-1327.3f, -746.18f, 3247.3f, 1826.18f),
                          "Menu Dark Background", QCd, ShadeColor);
            Hit(root, "BackgroundHit", new PxRect(0f, 0f, 1920f, 1080f), () => Close(), QCdHit - 1);

            // 2) 卡片那一叠（1 主卡 + 8 相关卡 · 扇形）—— 判据与真值 → 正本 §8·7
            var cardNode = MenuDraw.Node(root, "Card Display", new PxRect(CardL, CardT, CardR, CardB));
            BuildCardStack(cardNode);

            // 3) `LowerSection`：效果文字条 + 语音钮 + 「显示卡面文字」钮
            var lower = MenuDraw.Node(root, "LowerSection", new PxRect(LowerL, LowerT, LowerR, LowerB));
            // ⚠️ `FlavourTextBG` 的底图**本地没有**（原版 sprite=0、`FlavourTextSO` 运行时喂）⇒ 只画字
            var bg = MenuDraw.Node(lower, "FlavourTextBG", new PxRect(LowerL, LoreBgT, LowerR, LoreBgB));
            BuildLoreText(bg);
            RefreshLore();

            float vx = VoiceCx - BtnS * 0.5f, vy = BtnCy - BtnS * 0.5f;
            MenuDraw.Rect(lower, CardArt.Ui("40k_UI_bt_voicelines"), new PxRect(vx, vy, vx + BtnS, vy + BtnS),
                          "Voice Over Button", QCdRow, null, true);
            Hit(lower, "VoiceHit", new PxRect(vx, vy, vx + BtnS, vy + BtnS), () => PlayVoice(), QCdHit);
            float ex = EyeCx - BtnS * 0.5f;
            MenuDraw.Rect(lower, CardArt.Ui("40k_UI_bt_eye"), new PxRect(ex, vy, ex + BtnS, vy + BtnS),
                          "Show Card Text", QCdRow, null, true);
            Hit(lower, "ShowTextHit", new PxRect(ex, vy, ex + BtnS, vy + BtnS), ToggleLore, QCdHit);
            RefreshLore();

            // 4) 三块面板
            var panel = MenuDraw.Node(root, "Card Options Panel", new PxRect(0f, 0f, 1920f, 1080f));
            var vlg = MenuDraw.Node(panel, "Panel", new PxRect(PanL, PanT, PanR, PanB));
            BuildCrafting(vlg);
            BuildUpgrade(vlg);
            BuildAltArt(vlg);
            BuildCounter(panel);
            BuildWildcards(panel);
        }

        // ============================================================ 卡片那一叠（扇形 + 相关卡）

        /// <summary>命中区比卡面**小一圈**：原版点击接收器是 `CardUI/2DCard/UI Collider`
        /// —— 拉伸锚 + `sd(-0.2,-0.44)` ⇒ **473.18×722.83**，而卡本体是 523.25×832.75（正本 §8·7 表）。
        /// 比例 = 473.18/523.25。</summary>
        const float HitRatio = 0.9043f;

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据 → 正本 §8·7）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildCardStack(Transform cardNode)
        {
            var cards = new List<CardDef>();
            if (Card != null) cards.Add(Card);
            cards.AddRange(RelatedCards());
            SlotCount = Mathf.Min(cards.Count, Slots);
            for (int i = 0; i < Slots; i++)
            {
                SlotViews[i] = null; _slotDefs[i] = null;
                if (i >= SlotCount) continue;
                var def = cards[i];
                float k = FanScale[i] / FanScale[0];                    // 相对前台那张的比例
                float w = CardW * k, h = CardH * k;
                float cx = 960f + FanX[i], cy = 540f - FanY[i];         // 屏坐标（左上原点；原版 pos.y 向上 ⇒ 取负）
                var px = new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
                var node = MenuDraw.Node(cardNode, SlotName(i), px);
                var cd = BattleDriver.ToCardData(def, def.Faction);
                var v = CardView.Create(node, cd, SlotName(i));
                if (v == null)
                {
                    Debug.LogWarning("[CardDetail] 第 " + i + " 格（" + def.Name + "）的 `CardView` 建不出来 —— 那一格没画，出声");
                    continue;
                }
                v.gameObject.SetActive(true);
                v.SetPose(MenuDraw.Local(node, px.x1, px.y1, px.x2, px.y2), FanRot[i],
                          h / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                SlotViews[i] = v; _slotDefs[i] = def;
                // 🔴 **每格一个独立的渲染队列**（越靠前台越高）—— `CardView` 自己把每层硬编码成 3000，
                //    9 格叠在一起时「谁盖谁」就只剩「到相机的距离」在排，而那**不可控**：
                //    实测第一版**后面那张的卡名画到了前面那张的立绘之上**（同族坑 → `资料/已知的坑.md`）。
                //    ⚠️ 抬的是**整格所有层**（卡内的层序靠 z 偏移，整体平移不破坏它）；
                //    `3009–3019` 这段号是空的（不撞 `LiveOpsEventWindow` 3104-3116 与本窗 `QCd` 3110-3113）。
                SetCardQueue(v, QCardBase + (SlotCount - 1 - i));

                // 命中区：**每格一个**（前台那格也要 —— 它的回调在闸②直接 return，
                // 作用是「吃掉这一下」，否则会落到 `BackgroundHit` 上**把窗关掉**）。
                // 🔴 z 阶梯：`PointerLayer.HitButton` 同队列时**取 z 小的那个** ⇒ 越靠前台 z 越小。
                var hx = new PxRect(cx - w * 0.5f * HitRatio, cy - h * 0.5f * HitRatio,
                                    cx + w * 0.5f * HitRatio, cy + h * 0.5f * HitRatio);
                int idx = i;
                var hit = Hit(node, "CardHit " + i, hx, () => SwapToFront(idx));
                if (hit != null)
                    hit.localPosition = new Vector3(hit.localPosition.x, hit.localPosition.y, i * HitZStep);
            }
            _swapping = false;
            Debug.Log($"[CardDetail] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 扇形：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（正本 §8·7）");
        }

        /// <summary>相关卡（最多 `Slots-1` = 8 张）。两个来源（判据 → 正本 §八 / §九）：
        ///   ① **效果文本里点名的卡** —— `CreatePool.MentionedCards`（用户原话：「看效果文本的意思结合
        ///      部队卡牌名字这一关键词，**提到就是相关卡**」）；
        ///   ② **天赋是个池子**时，池子里那几张（`Choose a <子类型>` / `A random <阵营> <子类型>`）。
        /// ⚠️ 顺序 = 显示顺序：**先主卡、后相关卡**（用户 2026-09-26 裁的）；相关卡内部**先文本、后池子**。
        /// ⚠️ 池子那一支**只对有天赋的卡有意义**（`TalentName` 为空就跳过）—— 它同时也是
        ///    「原版相关卡 = 天赋」那个假设**本地唯一站得住的落点**（正本 §8·2 说本地证不出来）。</summary>
        List<CardDef> RelatedCards()
        {
            var outp = new List<CardDef>();
            if (Card == null) return outp;
            var pool = CardDatabase.Load();
            if (pool == null)
            {
                Debug.LogWarning("[CardDetail] 没有卡池 ⇒ 相关卡一张都算不了（**出声**，不许静默）");
                return outp;
            }

            string why;
            foreach (var r in CreatePool.MentionedCards(pool, Card, Card.Desc, out why, Slots - 1))
                AddUnique(outp, r);
            if (why != null)
                Debug.LogWarning("[CardDetail] 相关卡（点名那一支）没查成：" + why + " —— 不静默");

            // ② **池子**那一支 —— 三种来路都要认（判据 → 正本 §九；`PoolFromPhrase` 只认
            //    `ChooseSrc == "pool"` 与 `A random …`，所以不会把「从牌库里挑一个部队」误当池子）：
            //      · **这张卡自己的 desc** 就是池子（例：战术卡 `Master of Arcana`，
            //        它的 desc = `Choose an Ultramarines Psychic Power and put it in your hand`）；
            //      · `TalentName` **本身就是写法**（`Talent: A random Black Legion Psychic Power`）；
            //      · `TalentName` 是**一张天赋卡的名字**（`Talent: Master of Arcana` ⇒ 去解那张卡的 desc）
            //        —— 这正是引擎那条路（`RuleCore.SpawnTalents` 先 `FindByName` 再解）。
            var phrases = new List<string>();
            if (!string.IsNullOrEmpty(Card.Desc)) phrases.Add(Card.Desc);
            if (!string.IsNullOrEmpty(Card.TalentName))
            {
                phrases.Add(Card.TalentName);
                var talentCard = CreatePool.FindByName(pool, Card.TalentName);
                if (talentCard != null && !string.IsNullOrEmpty(talentCard.Desc)) phrases.Add(talentCard.Desc);
            }
            for (int pi = 0; pi < phrases.Count; pi++)
            {
                if (outp.Count >= Slots - 1) break;
                var list = CreatePool.PoolFromPhrase(pool, phrases[pi], false, out string what, out string pwhy);
                if (list == null)
                {
                    Debug.Log($"[CardDetail] 相关卡·池子那一支：第 {pi + 1} 个来路不是池子（{pwhy}）");
                    continue;
                }
                foreach (var r in list) { if (outp.Count >= Slots - 1) break; AddUnique(outp, r); }
                Debug.Log($"[CardDetail] 相关卡·池子那一支接上了：来路 {pi + 1}（筛选词 `{what}`，{list.Count} 张）");
            }            while (outp.Count > Slots - 1) outp.RemoveAt(outp.Count - 1);
            return outp;
        }

        static void AddUnique(List<CardDef> list, CardDef c)
        {
            if (c == null) return;
            foreach (var x in list) if (x.Id == c.Id) return;
            list.Add(c);
        }

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**；我们没有 lore 字段 ⇒ 画**效果文字**（出声）。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。</summary>
        void BuildLoreText(Transform bg)
        {
            if (bg == null) return;
            for (int i = bg.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(bg.GetChild(i).gameObject);
            var def = FrontDef ?? Card;
            string lore = def != null ? BattleDriver.FaceTextFull(def).body : "";
            var r = new PxRect(LoreL, LoreT, LoreR, LoreB);
            var lb = MenuDraw.Text(bg, r, lore, Color.white, "LoreText", LorePx, QCdText, LoreR - LoreL, 10f);
            if (lb != null) MenuDraw.AlignRight(lb, r);
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`，
        /// 253 行逐行读过 —— 判据全文 → 正本 §9·6）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ 有 `Animation` 在播。
        /// ⚠️ 闸③ **本工程不适用** —— 原版那条摆位用的 `Animation` 我们没有（我们按 §8·7 的常量直接摆），
        /// 这一点是**如实说明的偏离**。
        /// ⚠️ 层级对调照原版是 `SetSiblingIndex` **两两互换**（**不是「提到最前」**）。
        /// ⚠️ 本工程**显示的前后实际由「到相机的距离」决定** —— 卡片按 x 拉开 ⇒ 前台天然最近、画在最上；
        /// 所以这里换 sibling 是为了**与原版一致**，视觉上的前后不靠它。</summary>
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
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：开闸（上面已做）· 更新前台指针（上面已做）·
                // **重设 lore**（`SetCardLore`）· **重设语音**（`SetCardVoiceOver` —— 我们那边认 `FrontDef`）。
                BuildLoreText(Find(transform, "FlavourTextBG"));
                RefreshLore();
                Debug.Log("[CardDetail] 换位完成：前台现在是「" + (FrontDef != null ? FrontDef.Name : "?")
                        + "」（收尾重设了 lore / 语音认的卡 —— 原版 `CardSwapFinished`）");
            });
            CardTween.ToPose(a.transform, pb, rb, sb, SwapTime, DG.Tweening.Ease.OutQuad);
            Debug.Log($"[CardDetail] 换位：第 {idx} 格 ⇄ 前台（{SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        // ---- ① 创建副本 ----
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
            float ix = 1506.88f, iy = b.y1 + 10f, iw = 147.12f, ih = b.y2 - b.y1 - 20f;
            MenuDraw.Rect(p, CardArt.MenuUi("40k_main_collection_icon"), new PxRect(ix, iy, ix + iw, iy + ih),
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
            string[] ic = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                            "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            MenuDraw.Rect(panel, CardArt.MenuUi("40k_topmarquee_currency_display_BW"),
                          new PxRect(WcBgL, WcBgT, WcBgR, WcBgB), "Wildcard Bg", QCdRow);
            for (int i = 0; i < 4; i++)
            {
                float x = WcX0 + WcStep * i;
                MenuDraw.Rect(panel, CardArt.MenuUi(ic[i]), new PxRect(x, WcBgT, x + 30f, WcBgB),
                              "Wildcard Icon " + i, QCdRow);
                // ⚠️ 数字语义**与原版不同**（原版跟「指针悬停的那张卡」走）—— 我们只画**拥有数**，
                //    单人版没有发放源 ⇒ 如实画 9999 这一档并**出声**（同 `项目任务.md` §三 第 15 条 第 29 项）。
                MenuDraw.Text(panel, new PxRect(x + 30f, WcBgT, x + 71f, WcBgB), "9999", Color.white,
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

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QCdHit)
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
            return hit;
        }
        Transform Hit(Transform parent, string name, PxRect r, System.Func<bool> onClick, int q)
        {
            return Hit(parent, name, r, () => { onClick(); }, q);
        }
    }
}
