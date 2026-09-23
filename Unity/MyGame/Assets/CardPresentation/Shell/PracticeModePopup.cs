// PracticeModePopup.cs — 第 3 层第 6 件「战斗入口」：**`Practice Mode Menu`**（原版 **`PracticeModePopup : GameWindow`**）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_战斗入口_原版规格.md` **§一（窗口参数）+ §二 A（逐节点表）**。
// 窗口参数（实测）：`type=1 Popup` · `windowsPlacement=15 Popup` · `closeOnESC=1` · **`extraScaleSmallScreen=1.07`**。
//
// 🔴 **它是「点模式卡 → 进对应界面」那条路的第一个落地窗**（用户 2026-09-24 拍板：
//    「是直接点击这些卡片，然后就进去这些对应模式的界面的」）。
//    ⚠️ 原版这张「哪个模式 → 哪张卡 → 哪扇窗」的映射在 **liveop 服务端**（本地查不到、也不许自己编）——
//    本地入口是**我们定的**，逐条记在 `资料/阶段二_战斗入口_原版规格.md` §〇/§五。
//
// ============================ 开战链（§三）============================
// 原版：`Battle!` → `MatchMakerManager.StartMatch` →（无人应答）→ `StartBotBattle`
//       → `SearchOpponentManager.StartBattle` → `GetBattleArena(army)` → `LoadScene(场景名)`
//       （`SearchOpponentManager__StartBattle.c:57/61`，全二进制**唯一**的 `LoadScene` 点）。
// 我们：**练习模式本来就有 AI**（用户 2026-09-22 的裁决）⇒ `Battle!` = **选定卡组 + 切 `Battle.unity`**，
//       战斗场景自己会 `DeckLibrary.Load().Current`（`BattleDriver.PickSavedDeck`）。
//       arena 那张 13 条表**不在本地** ⇒ 继续用 `ArenaBuilder.DefaultArena = "battlearena1"`（§三）。
//
// ---- 没建的（出声，不静默）----
//   · `GameModeText`（"Game mode: Multiplayer"）—— 原版**出厂 act=0** ⇒ 照纪律不建
//   · `Character Image`（905×905）—— 原版 **`m_Enabled=0`** ⇒ 不建
//   · `Background Info`（`Deck info` 里那层，含 `cost drawer` / `Lore Text` 所在的抽屉）—— 原版 **act=0** ⇒ 不建；
//     ⚠️ 但 `Lore Text` 那一支我们**照 rect 画了**（它标着 act=1）—— 与「父层 act=0」冲突，**这条记在正本里待核**
//   · `Searching Oponent Popup` —— 原版 **act=0** ⇒ 不建（它由开战流程运行时打开；我们点 `Battle!` 直接切场景）
//   · `cost drawer`（`DeckEnergyCostDrawer`，费用曲线柱）—— **不建**（那套柱子还没接）
//   · 卡组行的**行高**原版没给 ⇒ **我们自己挑的**（38），标出来
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>`PracticeModePopup : GameWindow` —— 练习模式的模式窗。</summary>
    public class PracticeModePopup : GameWindow
    {
        // 层：高于所有「页」（最高 `CampaignTab` 3064）与卡组线那几个弹窗（3120…），**低于** `PromptPopup`（3140）
        public const int QPr = 3100, QPrRow = 3101, QPrText = 3102, QPrHit = 3103;

        // ---- 几何（§二 A 逐条）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        public const float BackL = 215.76f, BackT = 892.86f, BackR = 280f, BackB = 956.10f;
        public const float BackIcL = 224.19f, BackIcT = 901.15f, BackIcR = 271.58f, BackIcB = 947.81f;
        public const float BackTxL = 289.07f, BackTxR = 594.40f;
        public const float DsL = 77.10f, DsT = 165.33f, DsR = 610.90f, DsB = 858.19f;
        public const float DbtnL = 242.94f, DbtnT = 208.83f, DbtnR = 657.98f, DbtnB = 861.25f;
        public const float TipL = 283.76f, TipT = 170.83f, TipR = 617.16f, TipB = 208.83f;
        public const float ArmyTxL = 279f, ArmyTxT = 223.01f, ArmyTxR = 621.92f, ArmyTxB = 272.01f;
        public const float DecksL = 261.28f, DecksT = 262.64f, DecksR = 634.88f, DecksB = 803.43f;
        public const float ArmL = 69.42f, ArmT = 182.18f, ArmR = 246.54f, ArmB = 880.17f;
        /// <summary>阵营格 **82×82**、**纵向** spacing **26.38**（原版 `Filters` GridLayoutGroup）。</summary>
        public const float ArmyCell = 82f, ArmyGapY = 26.38f;
        public const float InfoL = 638.38f, InfoT = 78.79f, InfoR = 1842.38f, InfoB = 863.77f;
        public const float DlL = 1240.38f, DlT = 207.65f, DlR = 1810.17f, DlB = 827.18f;
        /// <summary>`Deck List Drawer/Content` 的格：**231×27.88**、spacing **(22, 3.6)**（原版字段）。</summary>
        public const float DlCellW = 231f, DlCellH = 27.88f, DlGapX = 22f, DlGapY = 3.6f;
        public const float DnL = 1371.64f, DnT = 224.13f, DnR = 1726.64f, DnB = 271.82f;
        public const float WnL = 1373.78f, WnT = 269.62f, WnR = 1726.64f, WnB = 303.62f;
        public const float LoreL = 1280.27f, LoreT = 647.41f, LoreR = 1770.27f, LoreB = 791.41f;
        public const float ShowBtnL = 1729.25f, ShowBtnT = 238.17f, ShowBtnR = 1793.49f, ShowBtnB = 301.41f;
        public const float ChgL = 1398f, ChgT = 680.75f, ChgR = 1652.54f, ChgB = 764.08f;
        public const float TogL = 813.24f, TogT = 906.26f, TogR = 1106.76f, TogB = 964.10f;
        public const float ContL = 1384.90f, ContT = 906.26f, ContR = 1837.98f, ContB = 956.10f;
        public const float BtTxL = 1385.08f, BtTxR = 1672.41f, BtTxT = 911.25f, BtTxB = 956.10f;
        public const float CircL = 1678.74f, CircT = 886.18f, CircR = 1768.74f, CircB = 976.18f;

        static readonly Vector4 InfoBorder = new Vector4(42f, 363f, 655f, 81f);
        const float InfoTexW = 1100f, InfoTexH = 701f;
        static readonly Vector4 SelBorder = new Vector4(0f, 0f, 0f, 0f);
        const float SelTexW = 1f, SelTexH = 1f;

        /// <summary>卡列表列数 = `floor((569.79 + 22) ÷ (231 + 22))` = **2**（照 GridLayoutGroup 那套算）。</summary>
        public static int ListCols
        {
            get { return Mathf.Max(1, Mathf.FloorToInt((DlR - DlL + DlGapX) / (DlCellW + DlGapX))); }
        }
        /// <summary>⚠️ **卡组行的行高是我们挑的** —— 原版 `Decks Scroll view` 里的格没给尺寸（§二 A 只给了视口）。</summary>
        public const float DeckRowH = 38f, DeckRowGap = 6f;

        /// <summary>当前选中的卡组（默认 = `DeckLibrary` 的当前那套）。</summary>
        public int DeckIndex;
        public int ArmyIndex = -1;              // -1 = 不限阵营
        public readonly List<Transform> DeckRows = new List<Transform>();
        public readonly List<Transform> ArmyCells = new List<Transform>();
        public readonly List<Transform> CardRows = new List<Transform>();
        public MenuScroll DeckScroll, ArmyScroll;
        /// <summary>`Battle!` 真的切了场景没有（自检用 —— 批处理下不切）。</summary>
        public bool StartedBattle { get; private set; }

        public Transform Hit(string name) { return transform.Find(name); }
        public Transform BtHit { get { return transform.Find("BattleHit"); } }
        public Transform BackHit { get { return transform.Find("BackHit"); } }
        public Transform TogHit { get { return transform.Find("ToggleHit"); } }
        public Transform ChgHit { get { return transform.Find("ChangeDeckHit"); } }

        public static PracticeModePopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Practice Mode Menu");
            var win = go.AddComponent<PracticeModePopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1.07f;           // 实证 1.07（**别抄 1.0**）
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>最近一次开出来的那扇（自检用）。</summary>
        public static PracticeModePopup LastOpened;

        public override void Open()
        {
            LastOpened = this;
            DeckIndex = CollectionData.CurrentIndex();
            ArmyIndex = -1;
            Build();
        }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            DeckRows.Clear(); ArmyCells.Clear(); CardRows.Clear();

            // 1) 压暗整屏 + 点背景关（原版 `BackgroundCloseButton`）
            Solid(root, 960f, 540f, 1920f, 1080f, ShadeColor, QPr, "Menu Dark Background");
            HitOn(root, root, "BackdropHit", new PxRect(0f, 0f, 1920f, 1080f), () => Close(), QPrHit - 1);

            // 2) `Back`（圆钮 + 箭头 + 文案）
            Img(root, "UI_Button_Round_background", BackL, BackT, BackR, BackB, "Back Bg", QPrRow, true);
            Img(root, "40k_UI_bt_back", BackIcL, BackIcT, BackIcR, BackIcB, "Back Icon", QPrRow, true);
            Txt(root, "Back", BackTxL, BackTxR, BackT, BackB, 45f, Align.Right, "Back Text", QPrText);
            Hit(root, root, "BackHit", new PxRect(BackL, BackT, BackR, BackB), () => Close());

            // 3) 选卡组那一列
            Img(root, "UI_Deck_Selection_Back", DbtnL, DbtnT, DbtnR, DbtnB, "Deck Buttons", QPr, true);
            Txt(root, "Select deck to play", TipL, TipR, TipT, TipB, 38f, Align.Right, "tooltip", QPrText);
            BuildArmySelector(root);
            BuildDeckRows(root);

            // 4) 右半：卡组信息（红底 + 卡组名/督军名 + lore + 卡列表 + 两个钮）
            Nine(root, root, "UI_Deck_Information_Back", InfoBorder, InfoTexW, InfoTexH,
                 InfoL, InfoT, InfoR, InfoB, QPr, "Deck info");
            BuildDeckInfo(root);

            // 5) 底下那一条：`Game mode` 开关 + `Battle!`
            Img(root, "40_main_bt_toggle_off", TogL, TogT, TogR, TogB, "Toggle Bg", QPrRow, true);
            Txt(root, "Game mode", TogL, TogR, TogT, TogB, 36f, Align.Center, "Toggle Label", QPrText);
            HitOn(root, root, "ToggleHit", new PxRect(TogL, TogT, TogR, TogB), () =>
                NotBuilt("`Game mode` 开关（原版切 Classic/Skirmish 两种赛制；本地只有一个卡池口径）"));

            Img(root, "40k_bt_underbutton", ContL, ContT, ContR, ContB, "Continue Button", QPrRow, true,
                new Color(0.369f, 0.894f, 0.587f, 1f));
            Txt(root, "Battle!", BtTxL, BtTxR, BtTxT, BtTxB, 45f, Align.Center, "Battle Text", QPrText);
            Img(root, "40k_UI_bt_play", CircL, CircT, CircR, CircB, "CircleButton", QPrRow, true);
            HitOn(root, root, "BattleHit", new PxRect(ContL, ContT, CircR, CircB), () => StartBattle(),
                  QPrHit);
        }

        /// <summary>左栏：13 个阵营格子（**82×82、纵向 spacing 26.38**，可纵向滚）。点一个 ⇒ 只显示该阵营的卡组。</summary>
        void BuildArmySelector(Transform root)
        {
            var vp = new GameObject("Army Selector");
            vp.transform.SetParent(root, false);
            vp.transform.localPosition = Local3(root, ArmL, ArmT, ArmR, ArmB);
            var view = new PxRect(ArmL, ArmT, ArmR, ArmB);

            var facs = CollectionWindow.CardsState.Factions();
            ArmyScroll = MenuScroll.TopAligned(view, facs.Count * (ArmyCell + ArmyGapY));
            ArmyScroll.Owner = gameObject;
            ArmyScroll.OnChanged = () => RebuildArmyCells(vp.transform);
            PointerLayer.RegisterScroll(ArmyScroll);
            _facs = facs;
            RebuildArmyCells(vp.transform);
        }
        List<string> _facs;

        void RebuildArmyCells(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            ArmyCells.Clear();
            var facs = _facs ?? new List<string>();
            var view = new PxRect(ArmL, ArmT, ArmR, ArmB);
            for (int i = 0; i < facs.Count; i++)
            {
                float y1 = ArmT + i * (ArmyCell + ArmyGapY);
                var r = new PxRect(ArmL, y1, ArmL + ArmyCell, y1 + ArmyCell);
                var rr = ArmyScroll.Shift(r);
                if (!Inside(view, rr)) continue;      // ⚠️ **只画完整落在视口里的行**（`GameWindow` 没有 `Clip`）
                var cell = new GameObject("Army_" + i);
                cell.transform.SetParent(holder, false);
                cell.transform.localPosition = Local3(holder, rr.x1, rr.y1, rr.x2, rr.y2);
                Img(cell.transform, DeckRuntime.FactionIcon(facs[i]), rr.x1, rr.y1, rr.x2, rr.y2,
                    "Icon", QPrRow, true);
                int idx = i;
                HitOn(cell.transform, cell.transform, "Hit", rr, () => PickArmy(idx), QPrHit);
                ArmyCells.Add(cell.transform);
            }
        }

        /// <summary>卡组列表（纵向滚）。⚠️ **行高是我们挑的**（原版没给格尺寸）。</summary>
        void BuildDeckRows(Transform root)
        {
            var holder = new GameObject("Decks Scroll view");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = Local3(root, DecksL, DecksT, DecksR, DecksB);
            var view = new PxRect(DecksL, DecksT, DecksR, DecksB);
            int n = FilteredDeckCount();
            DeckScroll = MenuScroll.TopAligned(view, Mathf.Max(n, 1) * (DeckRowH + DeckRowGap));
            DeckScroll.Owner = gameObject;
            DeckScroll.OnChanged = () => RebuildDeckRows(holder.transform);
            PointerLayer.RegisterScroll(DeckScroll);
            RebuildDeckRows(holder.transform);
        }

        Transform _deckHolder;
        void RebuildDeckRows(Transform holder)
        {
            _deckHolder = holder;
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            DeckRows.Clear();
            var view = new PxRect(DecksL, DecksT, DecksR, DecksB);
            int row = 0;
            int total = CollectionData.DeckCount();
            for (int i = 0; i < total; i++)
            {
                var info = CollectionData.DeckAt(i);
                if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count && info.Faction != _facs[ArmyIndex])
                    continue;
                float y1 = DecksT + row * (DeckRowH + DeckRowGap);
                var r = new PxRect(DecksL, y1, DecksR, y1 + DeckRowH);
                var rr = DeckScroll.Shift(r);
                row++;
                if (!Inside(view, rr)) continue;
                var cell = new GameObject("DeckRow_" + i);
                cell.transform.SetParent(holder, false);
                cell.transform.localPosition = Local3(holder, rr.x1, rr.y1, rr.x2, rr.y2);
                bool cur = i == DeckIndex;
                Solid(cell.transform, rr.x1, rr.y1, rr.x2, rr.y2,
                      cur ? new Color(1f, 0.773f, 0f, 0.55f) : new Color(1f, 1f, 1f, 0.10f), QPrRow, "Row Bg");
                Txt(cell.transform, info.Name, rr.x1 + 10f, rr.x2 - 10f, rr.y1, rr.y2, 26f, Align.Left,
                    "Name", QPrText);
                int idx = i;
                HitOn(cell.transform, cell.transform, "Hit", rr, () => PickDeck(idx), QPrHit);
                DeckRows.Add(cell.transform);
            }
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
        }

        /// <summary>矩形**完整**落在视口里没有。
        /// 🔴 `GameWindow`（本窗的基类）**没有 `Clip`** —— 那是 `MenuWindowBase` 才有的（x 方向 uv 裁）。
        /// ⇒ 这里退一步：**只画完整落在视口里的行**，部分越界的整行不画（滚动时行会「整行进出」）。
        /// 与 `资料/阶段二_滚动与指针_原版规格.md` 里已声明的那条缺口同性质（「文字不裁 · 九宫格那条路不裁」）。</summary>
        static bool Inside(PxRect view, PxRect r)
        {
            return r.y1 >= view.y1 - 0.5f && r.y2 <= view.y2 + 0.5f
                && r.x1 >= view.x1 - 0.5f && r.x2 <= view.x2 + 0.5f;
        }

        int FilteredDeckCount()
        {
            if (ArmyIndex < 0 || _facs == null || ArmyIndex >= _facs.Count) return CollectionData.DeckCount();
            int n = 0;
            for (int i = 0; i < CollectionData.DeckCount(); i++)
                if (CollectionData.DeckAt(i).Faction == _facs[ArmyIndex]) n++;
            return n;
        }

        string SelectedArmyName()
        {
            if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count)
            {
                var c = CollectionWindow.CardsState;
                return _facs[ArmyIndex].ToUpperInvariant();
            }
            var d = CollectionData.DeckAt(DeckIndex);
            return string.IsNullOrEmpty(d.Faction) ? "（未选阵营）" : d.Faction.ToUpperInvariant();
        }

        Label _txtArmy, _txtDeckName, _txtWarlord;
        Transform _bgInfo;
        void BuildDeckInfo(Transform root)
        {
            _txtArmy = Txt(root, SelectedArmyName(), ArmyTxL, ArmyTxR, ArmyTxT, ArmyTxB, 36f, Align.Right, "Selected Army Title", QPrText);
            BuildCardRows(root);

            // 🔴 **2026-09-24 修（实拍抓）**：下面这几件在 `Deck info/Background Info` **里面**，
            //    而那一层**出厂 `act=0`** —— 子件虽然各自 act=1，但**父层关着 ⇒ Unity 里就是不显示**。
            //    第一版照着子件的 act 画出来 ⇒ `Deck Name`/`Warlord Name`/`Lore Text`/`Change Deck`
            //    **和可视的 `Deck List Drawer` 挤在同一块矩形上**，字全叠在一起（截图一眼可见）。
            //    ⇒ 现在照原版**建出来但挂在关着的容器下**（不是不建 —— 数据还在，将来接 `Show Deck Content` 就能翻）。
            var bg = new GameObject("Background Info");
            bg.transform.SetParent(root, false);
            bg.transform.localPosition = Local3(root, 1258.08f, 316.03f, 1789.08f, 800.45f);
            _bgInfo = bg.transform;

            var info = CollectionData.DeckAt(DeckIndex);
            _txtDeckName = Txt(_bgInfo, info.Name, DnL, DnR, DnT, DnB, 36f, Align.Center, "Deck Name", QPrText);
            var wl = CollectionData.Warlord(DeckIndex);
            _txtWarlord = Txt(_bgInfo, wl != null ? wl.Name : "未选督军", WnL, WnR, WnT, WnB, 35f, Align.Left, "Warlord Name", QPrText);
            Txt(_bgInfo, wl != null && !string.IsNullOrEmpty(wl.Desc) ? wl.Desc : "（没有 lore）",
                LoreL, LoreR, LoreT, LoreB, 30f, Align.Center, "Lore Text", QPrText);
            Img(_bgInfo, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Bg", QPrRow, true);
            Img(_bgInfo, "40k_UI_bt_deck", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Icon", QPrRow, true);
            HitOn(_bgInfo, _bgInfo, "ShowDeckHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => NotBuilt("`Show Deck Content`（原版切 `Deck List` / `Deck Info` 两个抽屉）"));
            Img(_bgInfo, "UI_Button_Mulligan", ChgL, ChgT, ChgR, ChgB, "Change Deck Bg", QPrRow, true);
            Txt(_bgInfo, "Change Deck", ChgL, ChgR, ChgT, ChgB, 45f, Align.Center, "Change Deck Text", QPrText);
            HitOn(_bgInfo, _bgInfo, "ChangeDeckHit", new PxRect(ChgL, ChgT, ChgR, ChgB),
                  () => NotBuilt("`Change Deck`（原版开 `Deck Selection Popup with Tabs` —— 那扇窗还没建）"));

            bg.SetActive(false);            // 实证父层 `act=0`
        }

        Transform _cardHolder;

        /// <summary>建（或**重建**）卡列表。🔴 重建前**先把旧的那个销毁** —— 否则每换一次卡组就多留一棵孤儿树。</summary>
        void BuildCardRows(Transform root)
        {
            var old = root.Find("Deck List Drawer");
            if (old != null) RewardsWindow.DestroySafe(old.gameObject);
            CardRows.Clear();
            _cardHolder = new GameObject("Deck List Drawer").transform;
            _cardHolder.SetParent(root, false);
            _cardHolder.localPosition = Local3(root, DlL, DlT, DlR, DlB);

            var deck = CollectionData.Raw(DeckIndex);
            if (deck == null) return;
            var order = new List<string>();
            if (!string.IsNullOrEmpty(deck.WarlordId)) order.Add(deck.WarlordId);
            if (deck.CardIds != null)
                foreach (var id in deck.CardIds)
                    if (!string.IsNullOrEmpty(id) && !order.Contains(id)) order.Add(id);

            int cols = ListCols;
            for (int i = 0; i < order.Count; i++)
            {
                var card = CollectionData.Card(order[i]);
                if (card == null) continue;
                int c = i % cols, rr = i / cols;
                float x1 = DlL + c * (DlCellW + DlGapX);
                float y1 = DlT + rr * (DlCellH + DlGapY);
                var cell = new GameObject("CardRow_" + i);
                cell.transform.SetParent(_cardHolder, false);
                cell.transform.localPosition = Local3(_cardHolder, x1, y1, x1 + DlCellW, y1 + DlCellH);
                var nm = Txt(cell.transform, card.Name, x1, x1 + DlCellW, y1, y1 + DlCellH, 20f, Align.Left,
                             "Name", QPrText);
                // ⚠️ 卡名会超过 **231px** 的格宽（实测 "Death Spinner Warp Spider" 会撞进右边那一列）
                //    ⇒ 照原版那套开**自适应字号**把它缩进格子里（原版 `UICardInfoItem` 内部怎么排**没查**，出声）
                if (nm != null) nm.SetAutoFitBox(LayoutSpace.Px(DlCellW), LayoutSpace.Px(DlCellH), 10f, 20f);
                CardRows.Add(cell.transform);
            }
        }

        // ============================================================ 交互

        void PickArmy(int i)
        {
            ArmyIndex = (ArmyIndex == i) ? -1 : i;
            if (DeckScroll != null) DeckScroll.SetOffset(0f);
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            Debug.Log("[Practice] 阵营筛选：" + (ArmyIndex < 0 ? "不限" : _facs[ArmyIndex]));
        }

        void PickDeck(int i)
        {
            DeckIndex = i;
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            BuildCardRows(transform);          // 重建卡列表（卡组换了）
            var info = CollectionData.DeckAt(i);
            if (_txtDeckName != null) _txtDeckName.SetText(info.Name);
            var wl = CollectionData.Warlord(i);
            if (_txtWarlord != null) _txtWarlord.SetText(wl != null ? wl.Name : "未选督军");
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
            Debug.Log("[Practice] 选中卡组：「" + info.Name + "」");
        }

        /// <summary>点 `Battle!` —— **选定卡组 + 切 `Battle.unity`**（= 原版 `StartBattle` 的等价物，见文件头）。</summary>
        public void StartBattle()
        {
            var info = CollectionData.DeckAt(DeckIndex);
            if (string.IsNullOrEmpty(info.WarlordId))
            {
                Debug.LogWarning("[Practice] 这套卡组**没有督军**，开不了局 —— 如实说，不静默。");
                if (Manager != null) Manager.ShowPopUp("这套卡组还没有选督军，开不了局。", "知道了", null);
                return;
            }
            CollectionData.Select(DeckIndex);      // `BattleDriver.PickSavedDeck` 读的就是 `DeckLibrary.Current`
            StartedBattle = true;
            Debug.Log("[Practice] 开战：「" + info.Name + "」→ 切 `Battle.unity`"
                      + "（原版走 `StartMatch → StartBotBattle → StartBattle → LoadScene`，唯一 LoadScene 点；"
                      + " arena 表不在本地 ⇒ 用 `ArenaBuilder.DefaultArena`）");
            if (Application.isBatchMode) { Debug.Log("[Practice] （批处理：不切场景，只记账）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
        }

        void NotBuilt(string what)
        {
            Debug.LogWarning("[Practice] `" + what + "` 还没实现（**出声**，见 `Shell/PracticeModePopup.cs` 文件头「没建的」）");
        }

        // ============================================================ 画图小工具（同 `DeckInfoPopup` 那一套）
        // ⚠️ **坐标一律页面绝对 px；`parent` 与 `basis` 给同一个节点**（第三种错法见 `已知的坑.md`）。

        enum Align { Center, Left, Right }

        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - basis.position;

        void Solid(Transform parent, float x1, float y1, float x2, float y2, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(parent, CardArt.Solid(), Local3(parent, x1, y1, x2, y2),
                                        LayoutSpace.Px(y2 - y1), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect((x2 - x1) / Mathf.Max(1e-6f, y2 - y1));
            quad.SetTint(color); quad.SetRenderQueue(q);
        }

        ImageQuad Img(Transform parent, string art, float x1, float y1, float x2, float y2,
                      string name, int q, bool keepAspect, Color? tint = null)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[Practice] 图取不到，这一层不画：" + art); return null; }
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
            quad.SetAspect(w / Mathf.Max(1e-6f, h));
            quad.SetRenderQueue(q);
            if (tint.HasValue) quad.SetTint(tint.Value);
            return quad;
        }

        GameObject Nine(Transform parent, Transform basis, string art, Vector4 border, float texW, float texH,
                        float x1, float y1, float x2, float y2, int q, string name, Color? tint = null)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[Practice] 九宫格图取不到：" + art); return null; }
            var g = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH, Local3(basis, x1, y1, x2, y2),
                                              LayoutSpace.Px(x2 - x1), LayoutSpace.Px(y2 - y1), name);
            if (g != null)
                foreach (var c in g.GetComponentsInChildren<ImageQuad>())
                { c.SetRenderQueue(q); if (tint.HasValue) c.SetTint(tint.Value); }
            return g;
        }

        Label Txt(Transform parent, string text, float x1, float x2, float y1, float y2, float fontPx,
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

        Transform Hit(Transform parent, Transform basis, string name, PxRect r, System.Action onClick, int q = QPrHit)
            => HitOn(parent, basis, name, r, onClick, q);

        Transform HitOn(Transform parent, Transform basis, string name, PxRect r, System.Action onClick, int q = QPrHit)
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
            return hit.transform;
        }
    }
}
