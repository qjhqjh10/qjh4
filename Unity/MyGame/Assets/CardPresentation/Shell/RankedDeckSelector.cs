// RankedDeckSelector.cs — 排位活动窗内的「**卡组轮播**」组件（原版 `RankedDeckSelector`）
//
// ======================= 出处（判据全部现读，2026-10-27 · B88·E13）=======================
// · 方法体：`d:/2/tools/decomp_full/RankedDeckSelector__*.c`（`ChangeArmy` / `ChangeDeck` /
//   `OnEnable` / `OnDisable` / `Initialize` / 6 个按钮回调 / 3 个 lambda）· 交叉 `out_cpp2il_dev_ilspy/RankedDeckSelector.cs`（753 行）
// · 序列化字段（13 个 PPtr 全对上）：
//   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-5043253567395493968.json`
//   （`previousDeckButton` / `nextDeckButton` / `selectDeckButton` / `viewDeckButton` / `deckQuantity` /
//    `warlordName` / `deckName` / `noDeckText` / `goToCreateDeckButton` / `collectionWindow` / `shopWindow` /
//    `warlordImage` / `factionIconImage`）
// · **几何**：`工具/menu_dump.py bundle_menus_assets_all -5324570929900320848 --depth 3`
//   → `RankedEventWindowV2 ▸ Ranked Deck Selection`（**绝对矩形 · 1920×1080 · 左上原点 · y 向下**，与本类的 `PxRect` 同一套）。
//   ⚠️ 这一步是本轮才做通的：组件 MonoBehaviour 在 `bundle_menus_assets_all` 里，而**它的 GameObject 资产没被导出**
//   （`GameObject_-5807527989194618960` 全库不存在）⇒ 只能拿 pid 去 `menu_dump.py` 现解，⛔ 别在导出的 JSON 树里找。
//
// ==================== ⚠️ 本类是【重复实现】—— 运行期已有等价实现（2026-10-28 · B4 现核）====================
// 🔴 原版这一件是 `RankedEventWindowV2` 的子节点 `Ranked Deck Selection`；而**运行期真正在用的**是
// `Shell/LiveOpsEventWindow.cs` 的**内联实现**（`BuildDeckSelection` / `PickArmy` / `StepDeck` /
// `OpenDeckSelection` / `OpenDeckInfo` / `CreateDeckInMode`）—— `RankedEventWindow : LiveOpsEventWindow`
// 直接继承它，`Open()` 就建这一栏 ⇒ **玩家看得到、点得到**（逐条对位 → `报告/B88_B4_战役线.md` §三）。
// ⇒ 本类**零调用方**；要么删、要么按那份报告的差异清单改完再接线。⛔ 别再写「玩家看不到这个功能」。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedDeckSelector`：排位窗里那套「上一副 / 换一副 / 看一眼 / 下一副」的卡组轮播 +
    /// 无可用卡组时的「去建卡组 / 去商店」引导位。
    /// <para>🔴 **它选的是「本阵营 + 本模式 + 通过 `DeckUtility.ValidateDeck`」的那一批**（判据 = `ChangeArmy`），
    /// 不是「所有卡组」。轮播只在这批里转（`currentArmyCardDeckList`）。</para></summary>
    public class RankedDeckSelector
    {
        // ============================================================ 几何（menu_dump 现读，绝对设计 px）
        const float BoxL = 525.3f, BoxT = -0.1f, BoxR = 1394.7f, BoxB = 1080.1f;      // `Ranked Deck Selection`
        const float FactionL = 711.0f, FactionT = 133.5f, FactionR = 1233.6f, FactionB = 656.1f;   // `Faction Icon Image` 522.63²
        const float WarlordL = 450.9f, WarlordT = -95.1f, WarlordR = 1549.1f, WarlordB = 1003.1f;  // `Warlod Image` 1098.14²
        const float DarkL = 530.0f, DarkT = 418.2f, DarkR = 1470.0f, DarkB = 975.8f;              // `Warlord Darkening`
        // `Deck Buttons`（HLG·spacing 0·align 4·ctrlH 0·expandH 1）排完之后的四格（160.87×128，y 861..989）
        const float PrevL = 640.8f, BtnT = 861.0f, BtnB = 989.0f;
        const float BtnW = 160.87f;
        const float SelectL = 801.7f;   // `Change Deck Button`（= `selectDeckButton`）
        const float ViewL = 962.6f;     // `View Deck Button`（= `viewDeckButton`）
        const float NextL = 1123.4f;    // `Next Deck Button`（= `nextDeckButton`）
        const float DeckNameL = 694.7f, DeckNameT = 702.1f, DeckNameR = 1216.7f, DeckNameB = 779.1f;
        const float WarlordNameL = 694.7f, WarlordNameT = 779.1f, WarlordNameR = 1216.7f, WarlordNameB = 833.7f;
        const float NoDeckL = 617.2f, NoDeckT = 685.0f, NoDeckR = 1302.8f, NoDeckB = 851.5f;
        const float CreateBtnL = 789.0f, CreateBtnT = 865.7f, CreateBtnR = 1131.0f, CreateBtnB = 968.7f;
        const float QtyL = 949.2f, QtyT = 625.2f, QtyR = 1038.0f, QtyB = 702.1f;
        const float QtyIconL = 866.6f, QtyIconT = 630.2f, QtyIconR = 933.6f, QtyIconB = 697.2f;

        /// <summary>`CardArmy.Ultramarines` —— 原版 `ChangeDeck` 在「没传阵营且没有卡组」时的兜底
        /// （`RankedDeckSelector__ChangeDeck.c` 的 `uVar9 = 10`；枚举每档 +10，判据 → `Shell/PracticeModePopup.cs` 那段注释）。</summary>
        public const int ArmyUltramarines = 10;

        // ============================================================ 状态（字段名照原版）
        Transform _root;
        ImageQuad _prevQ, _selectQ, _viewQ, _nextQ;
        Label _deckQuantity, _warlordName, _deckName, _noDeckText;
        Transform _goToCreate;            // `goToCreateDeckButton` 那颗（原版 `EverguildButton`）
        ImageQuad _goToCreateBg;
        Label _goToCreateLabel;
        ImageQuad _warlordImage, _factionIcon;

        /// <summary>原版 `m_OnDeckSelected`（`Action&lt;CardDeck, CardArmy&gt;`）。**每次 `ChangeDeck` 末尾都广播**
        /// （包括「没有卡组 ⇒ 第一参数是 null」那一支 —— 判据 `ChangeDeck.c` 的两个 `LAB_1806c4e02` 出口）。</summary>
        public Action<PlayerDeck, int> OnDeckSelected;

        /// <summary>原版 `currentEvent.EventPlayMode` —— `ChangeArmy` 的**第二个**过滤条件。</summary>
        public int EventPlayMode;

        public PlayerDeck CurrentDeck { get; private set; }
        public int CurrentArmy { get; private set; }
        public int CurrentDeckIndexInTempList { get; private set; }
        public List<PlayerDeck> CurrentArmyCardDeckList { get { return _armyDecks; } }
        readonly List<PlayerDeck> _armyDecks = new List<PlayerDeck>();

        /// <summary>原版 `OnEnable`/`OnDisable` 那两颗钩子的等价物：四颗钮的监听在 `Build` 时就挂成**闭包**，
        /// 用这个旗标做「挂/摘」（= 原版 `AddListener`/`RemoveListener` 的可观测行为）。
        /// ⚠️ 原版 `OnDisable` **比 `OnEnable` 多摘两颗**（`goToCreateDeckButton` 上那两个回调，
        /// 它们是 `ChangeDeck` 里动态 `AddListener` 的）—— 这里同样由 `ChangeDeck` 负责换。</summary>
        public bool Hooked { get; private set; }

        public bool HasValidDeck
        {
            get { return CurrentDeck != null && DeckRules.Validate(CurrentDeck, CollectionData.Card) == DeckError.None; }
        }

        /// <summary>原版 `HasValidDeckWithValidationMessage()`：没卡组 ⇒ `(false, InvalidDeck)`。</summary>
        public void HasValidDeckWithValidationMessage(out bool ok, out DeckError err)
        {
            if (CurrentDeck == null) { ok = false; err = DeckError.InvalidDeck; return; }
            err = DeckRules.Validate(CurrentDeck, CollectionData.Card);
            ok = err == DeckError.None;
        }

        /// <summary>原版 `get_DeckName()`（= `deckName` 那颗 TMP）。</summary>
        public Label DeckNameLabel { get { return _deckName; } }

        // ============================================================ 建树
        /// <summary>建这颗组件（几何全在文件头的常量里；`tex` = 「sprite 名 → 贴图」的取图口，
        /// 由宿主给，同 `RankedDivisionInfo.Build` 那一路）。</summary>
        public void Build(Transform parent, Func<string, Texture2D> tex, int qArt, int qArt1, int qText)
        {
            _root = MenuDraw.Node(parent, "Ranked Deck Selection", new PxRect(BoxL, BoxT, BoxR, BoxB));

            _factionIcon = MenuDraw.Rect(_root, tex(ArmyIconsSprite(CurrentArmy)), new PxRect(FactionL, FactionT, FactionR, FactionB),
                                         "Faction Icon Image", qArt, null, true);
            _warlordImage = MenuDraw.Rect(_root, null, new PxRect(WarlordL, WarlordT, WarlordR, WarlordB),
                                          "Warlod Image", qArt, null, true);   // ⚠️ 原版节点名就是拼错的 `Warlod`
            MenuDraw.Rect(_warlordImage != null ? _warlordImage.transform : _root,
                          tex("Smooth background square"), new PxRect(DarkL, DarkT, DarkR, DarkB),
                          "Warlord Darkening", qArt1, new Color(0f, 0f, 0f, 0.816f), true);

            // ---- `Deck Buttons`（四颗圆钮；原版是 HLG 排的，这里用排完的绝对矩形）----
            var btns = MenuDraw.Node(_root, "Deck Buttons", new PxRect(PrevL, 816.0f, NextL + BtnW, 1034.0f));
            _prevQ = MenuDraw.Rect(btns, tex("UI_Button_Round_background"), Round(PrevL), "Previous Deck Button", qArt1, null, true);
            MenuDraw.Hit(btns, "Previous Deck Hit", Round(PrevL), qText,
                         () => { if (Hooked) OnPreviousDeckButtonClick(); }, _prevQ,
                         hoverArt: "40k_UI_bt_back_hover", pressedArt: "40k_UI_bt_back_pressed");
            _selectQ = MenuDraw.Rect(btns, tex("UI_Button_Round_background"), Round(SelectL), "Change Deck Button", qArt1, null, true);
            MenuDraw.Hit(btns, "Change Deck Hit", Round(SelectL), qText,
                         () => { if (Hooked) OnSelectDeckButtonClick(); }, _selectQ,
                         hoverArt: "40k_UI_bt_deck_change_hover", pressedArt: "40k_UI_bt_deck_change_pressed");
            _viewQ = MenuDraw.Rect(btns, tex("UI_Button_Round_background"), Round(ViewL), "View Deck Button", qArt1, null, true);
            MenuDraw.Hit(btns, "View Deck Hit", Round(ViewL), qText,
                         () => { if (Hooked) OnViewDeckButtonClick(); }, _viewQ,
                         hoverArt: "40k_bt_eye_hover", pressedArt: "40k_bt_eye_pressed");
            _nextQ = MenuDraw.Rect(btns, tex("UI_Button_Round_background"), Round(NextL), "Next Deck Button", qArt1, null, true);
            MenuDraw.Hit(btns, "Next Deck Hit", Round(NextL), qText,
                         () => { if (Hooked) OnNextDeckButtonClick(); }, _nextQ,
                         hoverArt: "40k_UI_bt_back_hover", pressedArt: "40k_UI_bt_back_pressed");

            // ---- 三行字（字号/对齐照 dump：62.85 auto[18~72] Center·Midline 折行 / 44.65 / 45 auto[18~45]）----
            _deckName = MenuDraw.Text(_root, new PxRect(DeckNameL, DeckNameT, DeckNameR, DeckNameB), "DECK NAME HERE",
                                      Color.white, "Deck Name", 62.85f, qText, autoMinPx: 18f, autoMaxPx: 72f);
            _warlordName = MenuDraw.Text(_root, new PxRect(WarlordNameL, WarlordNameT, WarlordNameR, WarlordNameB), "WARLORD NAME",
                                         Color.white, "Deck Warlord", 44.65f, qText, autoMinPx: 18f, autoMaxPx: 72f);
            _noDeckText = MenuDraw.Text(_root, new PxRect(NoDeckL, NoDeckT, NoDeckR, NoDeckB), "",
                                        Color.white, "No Deck Text", 45f, qText, autoMinPx: 18f, autoMaxPx: 45f);

            // `goToCreateDeckButton`（原版是 `No Deck Text` 的子件：`Generic Simplified UI Button`）
            _goToCreate = MenuDraw.Node(_root, "Generic Simplified UI Button",
                                        new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB));
            _goToCreateBg = MenuDraw.Rect(_goToCreate, tex("UI_Button_Mulligan"),
                                          new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB), "Bg", qArt1, null, true);
            _goToCreateLabel = MenuDraw.Text(_goToCreate, new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB),
                                             Loc.T("MainMenu/Ranked/GoToCreateDeck"), Color.white, "Button Text", 45f, qText,
                                             face: TmpFont.Face.LatinCardBody);
            MenuDraw.Hit(_goToCreate, "Hit", new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB), qText,
                         () => { if (Hooked) OnGoToCreateButtonClick(); }, _goToCreateBg,
                         hoverArt: "UI_Button_Mulligan_hover", pressedArt: "UI_Button_Mulligan_Pressed");

            // `Numer Of Army Decks`（'1/20'）+ 那颗 `40k_UI_icon_deck`
            MenuDraw.Rect(_root, tex("40k_UI_icon_deck"), new PxRect(QtyIconL, QtyIconT, QtyIconR, QtyIconB), "Deck Quantity Icon", qArt1, null, true);
            _deckQuantity = MenuDraw.Text(_root, new PxRect(QtyL, QtyT, QtyR, QtyB), "0/0", Color.white,
                                          "Numer Of Army Decks", 60.5f, qText, autoMinPx: 18f, autoMaxPx: 63.4f);
            if (_deckQuantity != null) _deckQuantity.SetAlignLeft();     // 原版对齐 = Left/Midline（dump 现读）

            CurrentArmy = ArmyUltramarines;
        }

        static PxRect Round(float x) { return new PxRect(x, BtnT, x + BtnW, BtnB); }

        static string ArmyIconsSprite(int army)
        {
            ArmyIcons.Entry e;
            return ArmyIcons.TryGet(army, out e) ? e.Sprite : null;
        }

        // ============================================================ 原版的 12 个方法
        /// <summary>原版 `Initialize(IPlayEvent rankedEvent)`：存事件 → 取「上次那副卡组」的阵营 → `ChangeArmy`。
        /// ⚠️ 没卡组 ⇒ **`10`（Ultramarines）**（`Initialize.c`：`uVar1 = 10`）。</summary>
        public void Initialize(int eventPlayMode, PlayerDeck preferredDeck)
        {
            EventPlayMode = eventPlayMode;
            ChangeArmy(preferredDeck != null ? ArmyOfDeck(preferredDeck) : ArmyUltramarines);
        }

        /// <summary>原版 `ChangeArmy(CardArmy newSelectedArmy)`。
        /// <para>**四跳**（`RankedDeckSelector__ChangeArmy.c` 逐句）：
        /// ① 列出卡组 ⇒ ② 过两道 `Where`（`deck.deckHero.cardArmy == 目标阵营 &amp;&amp; deck.GameMode == currentEvent.EventPlayMode`，
        /// 再 `DeckUtility.ValidateDeck`）⇒ ③ **选哪一副**：上次那副若不属于本阵营就取 `FirstOrDefault` ⇒ ④ 四颗钮的
        /// `SetActive` = `next/prev: Count &gt; 1` · `view/select: Count &gt; 0`，然后 `ChangeDeck(选中那副, 目标阵营)`。</para></summary>
        public void ChangeArmy(int newSelectedArmy)
        {
            var all = AllDecks();
            _armyDecks.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null) continue;
                if (ArmyOfDeck(d) != newSelectedArmy) continue;          // ① 阵营
                if (d.GameMode != EventPlayMode) continue;               // ② 模式（原版 `deck.GameMode == currentEvent.EventPlayMode`）
                if (DeckRules.Validate(d, CollectionData.Card) != DeckError.None) continue;   // ③ 有效性
                _armyDecks.Add(d);
            }
            // ④ 选中哪一副：原版 = 「上次那副」→ 若它不属于本阵营则 `FirstOrDefault(list)`；这里「上次那副」= 库里的 Current
            PlayerDeck pick = null;
            var remembered = RememberedDeck();
            if (remembered != null && ArmyOfDeck(remembered) == newSelectedArmy) pick = remembered;
            if (pick == null || !_armyDecks.Contains(pick)) pick = _armyDecks.Count > 0 ? _armyDecks[0] : null;
            int n = _armyDecks.Count;
            SetButtonActive(_prevQ, n > 1);
            SetButtonActive(_nextQ, n > 1);
            SetButtonActive(_selectQ, n > 0);
            SetButtonActive(_viewQ, n > 0);
            ChangeDeck(pick, newSelectedArmy);
        }

        /// <summary>原版 `ChangeDeck(CardDeck cardDeck, CardArmy? army)`。
        /// <para>**两半**（`RankedDeckSelector__ChangeDeck.c` 逐句）：有卡组 ⇒ 亮「卡名 / 战将名 / 战将图 / 3/7 计数」，
        /// 熄「无卡组那三件」，并把选中的那副**记回事件**（`SetDeck`）；没有卡组 ⇒ 反过来：亮阵营徽记 + 无卡组提示 +
        /// 那颗引导钮（**该阵营一张卡组都没有 ⇒ 「建卡组」；有但都不合法 ⇒ 「去商店」**）。
        /// 两条路的末尾都广播 `OnDeckSelected`（无卡组那一支第一个实参是 **null**）。</para></summary>
        public void ChangeDeck(PlayerDeck cardDeck, int? army = null)
        {
            CurrentDeck = cardDeck;
            CurrentArmy = army.HasValue ? army.Value : (cardDeck != null ? ArmyOfDeck(cardDeck) : ArmyUltramarines);
            bool hasDeck = cardDeck != null;

            SetActive(_deckName, hasDeck);
            SetActive(_deckQuantity, hasDeck);
            SetActive(_warlordName, hasDeck);
            SetActiveTf(_warlordImage, hasDeck);
            SetActiveTf(_factionIcon, !hasDeck);
            SetActive(_noDeckText, !hasDeck);

            if (hasDeck)
            {
                if (_warlordName != null) _warlordName.SetText(WarlordNameOf(cardDeck));
                if (_deckName != null) _deckName.SetText(cardDeck.Name);
                if (_warlordImage != null)
                {
                    var art = CardArt.DeckHero(cardDeck.WarlordId);
                    if (art != null) _warlordImage.SetTexture(art, true);
                }
                CurrentDeckIndexInTempList = _armyDecks.IndexOf(cardDeck);
                if (_deckQuantity != null)
                    _deckQuantity.SetText((CurrentDeckIndexInTempList + 1).ToString() + "/" + _armyDecks.Count.ToString());
                RememberDeck(cardDeck);           // = 原版 `currentEvent.SetDeck(cardDeck)`
            }
            else
            {
                if (_factionIcon != null)
                {
                    var tex = ArmyIconsSprite(CurrentArmy);
                    if (!string.IsNullOrEmpty(tex)) _factionIcon.SetTexture(CardArt.Ui(tex), true);
                }
                // 该阵营**所有**卡组数（原版 `Count(deck => deck.deckHero.cardArmy == currentArmy)`）——
                // ⚠️ 与 `_armyDecks` 不是一回事：这里**不看**模式、也**不看**合法性（原版就是这样）
                int n = CountDecksOfArmy(CurrentArmy);
                SetActiveTf(_goToCreate, true);
                if (n < 1)
                {
                    if (_noDeckText != null) _noDeckText.SetText(Loc.T("MainMenu/Ranked/GoToCreateDeck"));
                    if (_goToCreateLabel != null) _goToCreateLabel.SetText(Loc.T("MainMenu/Ranked/GoToCreateDeck"));
                    _goToAction = OnCreateDeckButtonClick;
                }
                else
                {
                    if (_noDeckText != null)
                        _noDeckText.SetText(string.Format(Loc.T("MenuDeck/Error/NoDeckForMode"), ArmyIcons.LocalizedName(CurrentArmy)));
                    if (_goToCreateLabel != null) _goToCreateLabel.SetText(Loc.T("MainMenu/Ranked/GoToBoosterStore"));
                    _goToAction = OnGoToStoreButtonClick;
                }
            }
            var cb = OnDeckSelected;
            if (cb != null) cb(CurrentDeck, CurrentArmy);
        }

        public void OnEnable() { Hooked = true; }
        public void OnDisable() { Hooked = false; _goToAction = null; }

        /// <summary>原版 `OnPreviousDeckButtonClick()`：下标 −1，**越界回末**（`< 0 ⇒ Count − 1`）。</summary>
        public void OnPreviousDeckButtonClick()
        {
            if (_armyDecks.Count == 0) return;
            CurrentDeckIndexInTempList--;
            if (CurrentDeckIndexInTempList < 0) CurrentDeckIndexInTempList = _armyDecks.Count - 1;
            ChangeDeck(_armyDecks[CurrentDeckIndexInTempList], null);
        }

        /// <summary>原版 `OnNextDeckButtonClick()`：下标 +1，**越界回 0**（`>= Count ⇒ 0`）。</summary>
        public void OnNextDeckButtonClick()
        {
            if (_armyDecks.Count == 0) return;
            CurrentDeckIndexInTempList++;
            if (CurrentDeckIndexInTempList >= _armyDecks.Count) CurrentDeckIndexInTempList = 0;
            ChangeDeck(_armyDecks[CurrentDeckIndexInTempList], null);
        }

        /// <summary>原版 `OnSelectDeckButtonClick()`：开 **`Deck Selection Popup with Tabs`**
        /// （判据 = `Shell/DeckSelectionPopup.cs` 文件头那 4 个原版调用点里的第一个），上下文 =
        /// `DeckSelectionContext(null, d => ChangeDeck(d, null), null, d => d.Army == currentArmy, EventPlayMode)`。
        /// <para>⚠️ 我们 `DeckSelectionPopup.Create(mgr, onPicked, modeFilter)` 只有「模式」那一个口 ⇒
        /// 「阵营谓词」只能在**回调层**挡（列表层筛不掉），见 `ApplyPickedDeck` 的 ⚠️ 两处。</para></summary>
        public void OnSelectDeckButtonClick()
        {
            int army = CurrentArmy;
            WindowsManager.OpenByRef("DeckSelectionPopup",
                m => DeckSelectionPopup.Create(m, pick => ApplyPickedDeck(pick, army), EventPlayMode));
        }

        /// <summary>原版 `OnViewDeckButtonClick()`：开 `DeckInfoPopup`（Edit 态）；**没有卡组就什么都不做**（`op_Equality` 那一跳）。</summary>
        public void OnViewDeckButtonClick()
        {
            if (CurrentDeck == null) return;
            int idx = LibraryIndexOf(CurrentDeck);
            WindowsManager.OpenByRef("DeckInfoPopup", m => DeckInfoPopup.Create(m, idx, DeckInfoPopup.DeckInfoState.Edit));
        }

        /// <summary>原版 `OnCreateDeckButtonClick()`：开收藏窗（原版 `WindowsManager.OpenWindow(collectionWindow, 参数…)`）。</summary>
        public void OnCreateDeckButtonClick()
        {
            WindowsManager.OpenByRef("CollectionWindow", m => CollectionWindow.Create(m));
            Debug.Log("[Ranked] 本阵营**一张卡组都没有** ⇒ 那颗钮走「建卡组」（原版 `OnCreateDeckButtonClick`：开收藏窗）");
        }

        /// <summary>原版 `OnGoToStoreButtonClick()`：开商店的 **booster 页**
        /// （判据 `RankedDeckSelector__OnGoToStoreButtonClick.c:28-33`：`+0xa0` 那个枚举实参直接喂窗口上下文）。</summary>
        public void OnGoToStoreButtonClick()
        {
            WindowsManager.OpenByRef("ShopWindow", m => ShopWindow.Create(m));
            Debug.Log("[Ranked] 有卡组但**都不合法** ⇒ 那颗钮走「去商店」（原版：开 `shopWindow` 的 booster 页）");
        }

        // ============================================================ 内部
        Action _goToAction;

        /// <summary>那颗引导钮**当前**该走哪一条（= 原版在 `ChangeDeck` 里 `RemoveAllListeners` + `AddListener` 的结果）。
        /// 用闭包旗标而不是「换监听」，是本项目一贯的等价写法（同一个洞见见 `ChangeDeck` 的两次挂载）。</summary>
        void OnGoToCreateButtonClick()
        {
            var a = _goToAction;
            if (a != null) a();
        }

        static void SetActive(Label lb, bool on) { if (lb != null) lb.gameObject.SetActive(on); }
        static void SetActiveTf(Component c, bool on) { if (c != null) c.gameObject.SetActive(on); }

        /// <summary>原版 `EverguildButton.gameObject.SetActive(...)`。⚠️ 只熄**按钮本体**，
        /// 命中区是我们另建的一颗裸节点（`… Hit`）⇒ 这里连命中一起熄（否则看不见却点得动）。</summary>
        void SetButtonActive(ImageQuad q, bool on)
        {
            if (q == null) return;
            var go = q.transform.parent;
            if (go != null)
                for (int i = 0; i < go.childCount; i++)
                {
                    var ch = go.GetChild(i);
                    if (ch.name.StartsWith("Previous Deck Hit") || ch.name.StartsWith("Change Deck Hit")
                        || ch.name.StartsWith("View Deck Hit") || ch.name.StartsWith("Next Deck Hit"))
                    {
                        // 命中区按钮名一一对应（`… Hit` 的父是 `Deck Buttons`）—— 由下面那句按名字挑
                        if ((q.name.StartsWith("Previous") && ch.name.StartsWith("Previous"))
                            || (q.name.StartsWith("Change") && ch.name.StartsWith("Change"))
                            || (q.name.StartsWith("View") && ch.name.StartsWith("View"))
                            || (q.name.StartsWith("Next") && ch.name.StartsWith("Next")))
                            ch.gameObject.SetActive(on);
                    }
                }
            q.gameObject.SetActive(on);
        }

        static string WarlordNameOf(PlayerDeck d)
        {
            var hero = string.IsNullOrEmpty(d.WarlordId) ? null : CollectionData.Card(d.WarlordId);
            return hero != null ? hero.Name : "";
        }

        static int ArmyOfDeck(PlayerDeck d)
        {
            var hero = (d == null || string.IsNullOrEmpty(d.WarlordId)) ? null : CollectionData.Card(d.WarlordId);
            var fac = hero != null ? hero.Faction : null;
            if (string.IsNullOrEmpty(fac)) return ArmyUltramarines;
            foreach (var e in ArmyIcons.All) if (e.Faction == fac) return e.Army;
            return ArmyUltramarines;
        }

        static int CountDecksOfArmy(int army)
        {
            var all = AllDecks(); int n = 0;
            for (int i = 0; i < all.Count; i++) if (ArmyOfDeck(all[i]) == army) n++;
            return n;
        }

        // 🔶 自己设计（非原版）：卡组来源 · 原版 `Inventory<CardDeck>.GetItemInstances(true)` 走 PlayFab 库存，
        //   我们本地只有存档里那一份 · 由调度台 2026-10-27 裁定（判据：`Shell/CollectionData.cs:61` 那条同族写法）
        static List<PlayerDeck> AllDecks()
        {
            var lib = RuleEngine.DeckLibrary.Load();
            var list = new List<PlayerDeck>();
            for (int i = 0; i < lib.Count; i++) list.Add(lib.Decks[i]);
            return list;
        }

        // 🔶 自己设计（非原版）：原版 `currentEvent.GetDeck()`（`IPlayEvent` 上的「本活动上次选的那副」）——
        //   我们的事件对象没有这个槽，等价物取 `DeckLibrary.Current`（= 玩家上次用的那副）· 调度台 2026-10-27 裁定
        static PlayerDeck RememberedDeck() { var lib = RuleEngine.DeckLibrary.Load(); return lib.Current; }

        // 🔶 自己设计（非原版）：原版 `currentEvent.SetDeck(deck)` · 同上，落到 `DeckLibrary.Select(index)`
        static void RememberDeck(PlayerDeck d)
        {
            int i = LibraryIndexOf(d);
            if (i >= 0) RuleEngine.DeckLibrary.Load().Select(i);
        }

        /// <summary>`PlayerDeck` 在存档库里的下标（找不到 = −1）。原版那边一切都是以**对象引用**为键
        /// （`currentArmyCardDeckList.IndexOf` / `Inventory` 里那批实例），我们的库是「每次 `Load()` 一份新的」
        /// ⇒ 只能按**名字**回找（`DeckLibrary` 保证卡组名唯一，见 `DeckLibrary.UniqueName`）。</summary>
        static int LibraryIndexOf(PlayerDeck d)
        {
            if (d == null) return -1;
            var lib = RuleEngine.DeckLibrary.Load();
            for (int k = 0; k < lib.Count; k++) if (lib.Decks[k].Name == d.Name) return k;
            return -1;
        }

        // ============================================================ 追加区 · 2026-10-28（B88·B4）
        // ⚠️ **一律追加在类尾**（铁律：新成员插进成员之间会让下游 `文件:行号` 落点整体漂移）。

        /// <summary>`OnSelectDeckButtonClick` 那条回调（= 原版 lambda `b__32_0`：`ChangeDeck(d, null)`）。
        /// <para>⚠️ **两处如实记**（都不是「静默」）：
        /// ① 原版 `b__32_1` 的谓词 `d.Army == currentArmy` 是**列表级**筛选；我们的
        ///    `DeckSelectionPopup.Create(mgr, onPicked, modeFilter)` **没有这个口**（该文件不在本批白名单）
        ///    ⇒ 只能在回调这一层挡：阵营不符则**出声 + 不改当前选择**；
        /// ② 原版 `CardDeck` 同时覆盖**预组**与自建；本类只吃 `PlayerDeck`（预组不在 `DeckLibrary` 里）
        ///    ⇒ 选到预组时同样**出声 + 不动**。</para>
        /// <para>🔴 运行期真正在用的那一路是 `Shell/LiveOpsEventWindow.cs:1883`（`OpenDeckSelection`），
        /// 本方法属**零调用方**的重复实现（见文件头）。</para></summary>
        void ApplyPickedDeck(DeckSelectionPopup.DeckPick pick, int army)
        {
            if (pick.Prebuilt)
            {
                Debug.LogWarning("[Ranked] 选中的是**预组卡组**「"
                               + (pick.PrebuiltDeck != null ? pick.PrebuiltDeck.DisplayName : "?")
                               + "」—— 本组件只有 `PlayerDeck` 通道（预组不在 `DeckLibrary` 里）"
                               + "⇒ 不改变当前选择（出声，不静默）");
                return;
            }
            var d = CollectionData.Raw(pick.OwnIndex);
            if (d == null)
            {
                Debug.LogWarning("[Ranked] 选卡组回调的下标 " + pick.OwnIndex
                               + " 在 `CollectionData` 里取不到卡组 ⇒ 不改变当前选择");
                return;
            }
            int picked = ArmyOfDeck(d);
            if (picked != army)
            {
                Debug.LogWarning("[Ranked] 选中的卡组属于阵营 " + picked + "，而本栏筛的是 " + army
                               + "（原版这一层是列表谓词 `b__32_1`；我们的弹窗没有谓词口 ⇒ 在这里挡）"
                               + "⇒ 不改变当前选择");
                return;
            }
            ChangeDeck(d, null);
        }
    }
}
