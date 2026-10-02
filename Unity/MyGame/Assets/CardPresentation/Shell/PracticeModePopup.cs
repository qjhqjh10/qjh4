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
//       ✅ **2026-09-25 更正**：arena 那张表**在本地、已实读**（`ArenaByArmy` 运行时表；
//       判据 → `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六）。
//       ⚠️ **但还没接上** —— `Battle.unity` 建场时就把战场几何烘死了，换场要先定架构，见 `ArenaByArmy` 头注释。
//
// ---- 没建的（出声，不静默）----
//   · `GameModeText`（"Game mode: Multiplayer"）—— 原版**出厂 act=0** ⇒ 照纪律不建
//   · `Character Image`（905×905）—— 原版 **`m_Enabled=0`** ⇒ 不建
//   ✅ **2026-10-03（§三第29条 A14）**：~~`Deck Information cost drawer` + `Deck Information Cost/balance text`
//     —— **不建**（那套 `DeckEnergyCostDrawer` 费用曲线柱还没接）~~ ⇒ **两件都建了**：标题那条按原版
//     `Card / Energy cost`（fs34），曲线 9 行走 **`Core/CostCurveDrawer`**（与 `Deck info Popup` 那扇**共用一份画法**；
//     这一扇的 `localScale` 是 **1.2**，那扇是 1.8）。
//   · `Lore Text` —— **仍不建**（原版喂的是 `DemoDeckInfoSO.Lore` = **卡组**简介）。🔴 **2026-10-03 查清了卡在哪**：
//     `DemoDeckInfoSO`（`bundle_menus_assets_all/MonoBehaviour/Demo DeckInfo *.json`，**82 份**）里存的是
//     **`loreLocalizationKey`**（例 `Demo/AeldariDeck1`）—— **正文在远端 CCD 的词条表里**（原版客户端根本没有那份表）
//     ⇒ **判据（文本）本地就是空的**，按铁律 11 的第 ① 种挂着，**不编文案**。
//     上一版拿督军的效果文字顶上去 ⇒ 内容不对，而且它的框套着 `Change Deck`、实拍里两行字叠在一起。
//     见 `BuildGeneralContainer` 里那段注释）
//   · `Searching Oponent Popup` —— ✅ **2026-09-24 建了**（`Shell/SearchingMatchPopup.cs`，四窗共用）。
//     🔴 原来这行写「原版出厂 act=0 ⇒ 不建（由开战流程运行时打开；我们点 `Battle!` 直接切场景）」——
//        **下半句是错的**：原版点 `Battle!` 也要先过 `MatchMakerManager.StartMatch`，这扇窗就是那 12 秒里显示的。
//   · 卡组行的**行高**原版没给 ⇒ **我们自己挑的**（38），标出来
//
// ============================ 🔴 2026-09-24 结构订正（第 53 条）============================
// **原来那份挂错了父**：把 `Deck Name`/`Warlord Name`/`Lore Text`/`Change Deck` 塞进
// `Deck info/Background Info`（那一层实读是**【空容器】+ act=0**），于是它们要么不显示、
// 要么和卡列表挤在同一块矩形上（实拍：字叠成一团）。**从根节点实读**（`工具/menu_rect.py` +
// `工具/menu_dump.py` + `工具/_probe_deckinfo.py` 三条互证）后的真结构：
//
//   `Deck info`（**自己没图**）
//     ├ `Generic Window Red Background Big`  761.63,187.00→1831.93,869.00  `UI_Deck_Information_Back`
//     ├ `Character Image`（m_Enabled=0，不建）
//     ├ `Background Info`（**【空容器】act=0**，照原版留空）
//     ├ `Deck Name`（act=1）· `Warlord Name`（act=1）· `Army Image`（act=1）  ← 直挂，本来就该看得见
//     ├ `General container`（act=1）→ lore · cardback · `Show Deck Content Button` · `Change Deck`
//     └ `Deck List Drawer`（act=1）→ `Content`（起 y **318.42**）· `Show Deck General Info button`
//
// **两个抽屉互斥**（`DeckGeneralInfoDemo.Toggle(bool)`），**出厂 = 总览**（`SetContent` 末尾
// `generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`）。
// 🔴 卡列表那个抽屉在**本地这份包里打不开**：`Toggle(false)` 在全量反编译里**找不到调用者**
//    （`grep -l DeckGeneralInfoDemo__Toggle *.c` 只命中它自己）⇒ **如实记着，别自己给它编一个入口**。
// `Show Deck Content Button` 原版**不是**抽屉开关 —— 它走
//    `CardInDeckInfoButtonOnClick → WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`，
//    **开的是「卡组详情」窗**（= 我们已建的 `DeckInfoPopup`）。
// ⚠️ 正本 `阶段二_战斗入口_原版规格.md` §二 A 那张表的缩进与「红底写在 `Deck info` 那行」两处**与实读不符**，
//    已就地更正（`项目任务.md` §三 第 53 条）。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;
using CardPresentation.Net;      // 🆕 联机（N3：`Battle!` 走 P2P 还是走 12 秒 bot 链）

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
        /// <summary>🔴 **2026-09-24 实读订正**：`Deck info` **自己身上没有图** —— 红底在它的子件
        /// `Generic Window Red Background Big` 上，矩形是 **761.63,187.00→1831.93,869.00**
        /// （不是 `Deck info` 那整块 1204×784.98）。两处出处互证：
        /// `menu_dump.py … 7119707400320339772`（`Deck info  MB[DeckGeneralInfoDemo]` 无 Img、子件带 `Img[UI_Deck_Information_Back] type=Sliced`）
        /// 与 `_probe_deckinfo.py`（`Deck info` 的组件只有 RectTransform + DeckGeneralInfoDemo）。
        /// ⚠️ 正本 `阶段二_战斗入口_原版规格.md` §二 A 把这张图写在 `Deck info` 那一行 —— 与实读不符。</summary>
        public const float BgBigL = 761.63f, BgBigT = 187.00f, BgBigR = 1831.93f, BgBigB = 869.00f;
        public const float DlL = 1240.38f, DlT = 207.65f, DlR = 1810.17f, DlB = 827.18f;
        /// <summary>`Deck List Drawer/Content` 的矩形（原版 LayoutGroup 节点本身）。
        /// 🔴 我们原来把卡列表起在 `Deck List Drawer` 自己的左上角（1240.38, 207.65）——
        /// 正好压在 `Deck Name`(224.13→271.82) / `Warlord Name`(269.62→303.62) 上（第 53 条那条「字叠成一团」）。</summary>
        public const float DlContentL = 1261.27f, DlContentT = 318.42f, DlContentR = 1787.37f, DlContentB = 793.87f;
        /// <summary>格：**231×27.88**、spacing **(22, 3.6)**、pad **(22, 0, 4, 0)**（原版 `GridLayoutGroup` 字段，逐条抄）。</summary>
        public const float DlCellW = 231f, DlCellH = 27.88f, DlGapX = 22f, DlGapY = 3.6f;
        public const float DlPadL = 22f, DlPadT = 4f;
        public const float DnL = 1371.64f, DnT = 224.13f, DnR = 1726.64f, DnB = 271.82f;
        /// <summary>⚠️ 原版 `Warlord Name` 的矩形**宽 0**（`ContentSizeFitter` + `RectSizeLimiter` 运行时算）
        /// ⇒ **右沿是我们挑的**（取 `Deck Name` 的 `DnR`）。**不是复刻**。</summary>
        public const float WnL = 1373.78f, WnT = 269.62f, WnR = 1726.64f, WnB = 303.62f;
        /// <summary>`Army Image` **105.26×104**（原版 sprite = 该阵营的 `40k_DeckSelection_icon_Faction*`）。</summary>
        public const float ArmImgL = 1258.85f, ArmImgT = 210.08f, ArmImgR = 1364.11f, ArmImgB = 314.08f;
        /// <summary>`Cardback`（原版 `sprite=0`、运行时喂 `CardDeck.GetDeckCardback`）。</summary>
        public const float CbL = 1542.35f, CbT = 329.63f, CbR = 1751.91f, CbB = 631.96f;
        public const float LoreL = 1280.27f, LoreT = 647.41f, LoreR = 1770.27f, LoreB = 791.41f;

        // 🆕 2026-10-03（A14）：`General container` 里补的两件（判据 → `阶段二_战斗入口_原版规格.md` §二 A）
        /// <summary>`Deck Information Cost/balance text`：**1276.27,326.95→1540.27,381.27** · fs34 auto[1-34] · 居中。</summary>
        public const float CostTxtL = 1276.27f, CostTxtT = 326.95f, CostTxtR = 1540.27f, CostTxtB = 381.27f;
        /// <summary>`Deck Information cost drawer`：rect **158.15×199.06**、**`localScale = 1.2`** ⇒ 真画 189.78×238.87。
        /// 中心 **(1408.275, 504.71)**。里面 9 行的画法与 `Deck Info Popup` 那扇**共用** `Core/CostCurveDrawer`。</summary>
        public const float CostDrwCX = 1408.275f, CostDrwCY = 504.71f, CostDrwScl = 1.2f;
        /// <summary>费用曲线 9 行的读数（自检用）。</summary>
        public readonly System.Collections.Generic.List<int> CostRows =
            new System.Collections.Generic.List<int>();
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

        /// <summary>卡列表列数 = `floor((526.10 − 22 + 22) ÷ (231 + 22))` = **2**
        /// （照原版 `GridLayoutGroup`：`constraint=0/Flexible` ⇒ 按宽度算，pad 左 22 右 0）。</summary>
        public static int ListCols
        {
            get
            {
                float usable = (DlContentR - DlContentL) - DlPadL;
                return Mathf.Max(1, Mathf.FloorToInt((usable + DlGapX) / (DlCellW + DlGapX)));
            }
        }

        static Transform New(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        Label _txtArmy, _txtDeckName, _txtWarlord;
        Transform _info, _bgInfo, _general, _deckList;
        ImageQuad _armyIcon, _cardback;

        /// <summary>🔴 **两个抽屉是互斥的**（原版 `DeckGeneralInfoDemo.Toggle(bool)`：
        /// `generalInfoContainer.SetActive(opt)` + `cardsInDeckPanel.gameObject.SetActive(!opt)` —— 反编译
        /// `DeckGeneralInfoDemo__Toggle.c`，两个字段的 pid 由 `工具/_probe_deckinfo.py` 从序列化数据解出：
        /// `generalInfoContainer` = GO `General container` · `cardsInDeckPanel` = GO `Deck List Drawer`）。
        ///
        /// **出厂态 = 总览（`Toggle(true)`）**：原版 `SetContent` 末尾就是
        /// `generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`，而
        /// `PracticeModePopup.DeckSelected → DeckGeneralInfoDemo.SetContent` 是开窗/换卡组的必经之路
        /// （`PracticeModePopup__DeckSelected.c`）。
        ///
        /// 挂父也一并订正（正本 §二 A 那张表的缩进是错的）：
        /// `Deck Name` / `Warlord Name` / `Army Image` / `Background Info` / `General container` / `Deck List Drawer`
        /// **全是 `Deck info` 的直接子件**；`Background Info` 实读是**空容器**。</summary>
        void BuildDeckInfo(Transform root)
        {
            _txtArmy = Txt(root, SelectedArmyName(), ArmyTxL, ArmyTxR, ArmyTxT, ArmyTxB, 36f, Align.Center, "Selected Army Title", QPrText);

            // `Deck info` —— **容器本身没有图**（红底在子件 `Generic Window Red Background Big` 上，见常量段注释）
            _info = New(root, "Deck info");
            _info.localPosition = Local3(root, InfoL, InfoT, InfoR, InfoB);

            Nine(_info, _info, "UI_Deck_Information_Back", InfoBorder, InfoTexW, InfoTexH,
                 BgBigL, BgBigT, BgBigR, BgBigB, QPr, "Generic Window Red Background Big");

            // `Background Info` —— 原版实读：**【空容器】+ 出厂 act=0**（一条子件都没有）。
            // ⚠️ 它**不是** `Deck Name`/`Lore Text` 那几件的父（正本 §二 A 那么写是错的 —— 第 53 条）。
            //    照原版**留空**：建出来、关着，让别人一眼看得出「这里原版就是空的」。
            _bgInfo = New(_info, "Background Info");
            _bgInfo.localPosition = Local3(_info, 1258.08f, 316.03f, 1789.08f, 800.45f);
            _bgInfo.gameObject.SetActive(false);

            // ---- 直挂 `Deck info` 的三件（原版 act=1 ⇒ **本来就该看得见**）----
            BuildDeckInfoHead();

            // ---- `General container`（**出厂可见**那个抽屉）----
            _general = New(_info, "General container");
            _general.localPosition = Local3(_info, DlL, DlT, DlR, DlB);
            BuildGeneralContainer();

            // ---- `Deck List Drawer`（原版 act=1，但出厂被 `SetContent` 关掉；内容见 `BuildCardRows`）----
            BuildCardRows(_info);
            Toggle(true);                       // 出厂 = 总览（`SetContent` 那两句）
        }

        /// <summary>`Deck Name`(fs36 居中) / `Warlord Name`(fs35 **左对齐**) / `Army Image`(105.26×104)。
        /// ⚠️ `Warlord Name` 原版矩形**宽 0** 且 **`pivot=(0, 0.5)`**（实测）⇒ `ContentSizeFitter`
        /// 从 **x=1373.78 向右撑开**；TMP 那边虽然写 `hAlign=Center`，但框是贴合文本的 ⇒ 视觉上就是**左起**。
        /// 我们给它一个固定框（右沿取 `Deck Name` 的 `DnR`，**这一条是我们挑的**）并按**左对齐**画，
        /// 起点与原版一致（用 Center 会右移约 176px）。</summary>
        void BuildDeckInfoHead()
        {
            var d = CollectionData.DeckAt(DeckIndex);
            var wl = CollectionData.Warlord(DeckIndex);
            _txtDeckName = Txt(_info, d.Name, DnL, DnR, DnT, DnB, 36f, Align.Left, "Deck Name", QPrText);
            _txtWarlord = Txt(_info, wl != null ? wl.Name : "未选督军", WnL, WnR, WnT, WnB, 35f,
                              Align.Left, "Warlord Name", QPrText);
            _armyIcon = Img(_info, DeckRuntime.FactionIcon(d.Faction), ArmImgL, ArmImgT, ArmImgR, ArmImgB,
                            "Army Image", QPrRow, true);
        }

        /// <summary>`General container` 里的几件：cardback · `Show Deck Content Button` · `Change Deck`。
        /// **没建的**（出声，见文件头）：`Deck Information cost drawer` + `Deck Information Cost/balance text`。
        /// 🔴 **2026-10-03 就地更正（A20）**：这里原来写着「我们没有『卡组装备了哪张卡背』这份数据 ⇒
        /// 退回 `CardArt.CardBack(阵营)`……**这条是我们挑的，不是复刻**」—— **那句话现在作废**：
        /// `CollectionData.DeckInfo` **有 `CardbackId`**（`Shell/CollectionData.cs:30`，卡组编辑器那页能装备），
        /// 而原版这条链要的就是它（`DeckGeneralInfoDemo.ShowPayerDeckInfo → CardDeck.GetDeckCardback()`）
        /// ⇒ 改成走**判据那一处** `CardArt.DeckCardback(卡背 id, 阵营)`（选了用选的、没选用阵营默认背）。</summary>
        void BuildGeneralContainer()
        {
            var d = CollectionData.DeckAt(DeckIndex);

            // 🔴 **`Lore Text` 不画**（2026-09-24）：原版它喂的是 `DemoDeckInfoSO.Lore`（**卡组**的简介，
            //   `ShowPayerDeckInfo` 里那句 `DemoDeckInfoSO__get_Lore` + `set_text`）。
            //   我们的卡组**没有这个字段**，上一版拿**督军的效果文字**顶上去 ⇒ 两宗错：
            //   ① 内容不对（效果文字 ≠ 卡组简介）· ② 它的框 1280.27,647.41→1770.27,791.41
            //      **套着** `Change Deck`（1398.00,680.75→1652.54,764.08）⇒ 实拍里两行字叠在一起。
            //   按「宁可没有，不可错着显示」**不画**，并把这条记在这里与文件头。
            Debug.Log("[Practice] `Lore Text` **没画** —— 原版喂的是 `DemoDeckInfoSO.Lore`（卡组简介），"
                      + "我们的卡组没有这个字段（拿督军的效果文字顶上去会和 `Change Deck` 叠）。**出声，不静默**");

            // 🆕 2026-10-03（§三第29条 A14）：`Cost/balance text` + `Deck Information cost drawer` **补上了**。
            //   判据：`资料/阶段二_战斗入口_原版规格.md` §二 A 那张表（`python 工具/menu_dump.py … "Practice Mode Menu"`）
            //   · `Cost/balance text` **1276.27,326.95→1540.27,381.27** · fs34 auto[1-34] · 居中 · 原文 `Card / Energy cost`
            //   · `cost drawer` **1329.20,405.18→1487.35,604.24** · **`localScale 1.2`**（`Deck Info Popup` 那份是 1.8）
            //   🔴 **画法不在这儿**：与 `Deck Info Popup` 那扇共用 `Core/CostCurveDrawer`
            //      （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            Txt(_general, "Card / Energy cost", CostTxtL, CostTxtR, CostTxtT, CostTxtB, 34f, Align.Center,
                "Deck Information Cost/balance text", QPrText);
            {
                var counts = CostCurveDrawer.Counts(CollectionData.Raw(DeckIndex), CollectionData.Card);
                CostCurveDrawer.Build(_general, CostDrwCX, CostDrwCY, CostDrwScl, counts, QPrRow, QPrText);
                CostRows.Clear();
                CostRows.AddRange(counts);
                Debug.Log("[Practice] 费用曲线画了 9 行（" + CostCurveDrawer.Dump(counts) + "）");
            }

            _cardback = ImgTex(_general, CardArt.DeckCardback(d.CardbackId, d.Faction), "Cardback(这副牌的卡背)", CbL, CbT, CbR, CbB,
                               "Cardback", QPrRow, true);

            // ⚠️ 图标那层用**原版 `Icon` 子件自己的矩形**（1737.68,246.46→1785.07,293.12 = 47.39×46.66），
            //    不是父件的 64.24×63.24（用父的会把图标放大 1.36 倍、而且不居中）
            Img(_general, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Bg", QPrRow, true);
            Img(_general, "40k_UI_bt_deck", 1737.68f, 246.46f, 1785.07f, 293.12f, "Show Icon", QPrRow, true);
            // ✅ **2026-09-24 订正**：原版这颗钮**不是**翻开卡列表抽屉 —— 它是
            //    `DeckGeneralInfoDemo.cardInDeckInfoButton`（pid 由序列化字段解出），
            //    点下去的 `CardInDeckInfoButtonOnClick` 走 `WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`
            //    ⇒ **开的是「卡组详情」那扇窗**（`Deck info Popup`，我们已建）。
            HitOn(_general, _general, "ShowDeckHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => ShowDeckContent());

            Img(_general, "UI_Button_Mulligan", ChgL, ChgT, ChgR, ChgB, "Change Deck Bg", QPrRow, true);
            Txt(_general, "Change Deck", ChgL, ChgR, ChgT, ChgB, 45f, Align.Center, "Change Deck Text", QPrText);
            // ✅ 原版这条就是 `DeckGeneralInfoDemo.ChangePlayerDeckButton`
            //    → `WindowsManager.OpenWindow(new DeckSelectionContext(...))`（`…__ChangePlayerDeckButton.c`）。
            HitOn(_general, _general, "ChangeDeckHit", new PxRect(ChgL, ChgT, ChgR, ChgB), () => OpenDeckSelection());
        }

        /// <summary>`Show Deck Content` —— **开「卡组详情」窗**（原版 `DeckInfoContext(deck, 2, …)`）。
        /// 🔴 2026-09-24 实读订正：这一件**不是**抽屉开关。两个抽屉的开关只有 `Toggle(bool)` 一个入口，
        /// 而它在本地的全量反编译里**找不到任何调用者**（`grep -l DeckGeneralInfoDemo__Toggle *.c` 只命中它自己）——
        /// 也就是说卡列表那个抽屉在本地这份包里**打不开**。**如实记着**，别自己给它编一个入口。</summary>
        public DeckInfoPopup ShowDeckContent()
        {
            if (Manager == null)
            {
                Debug.LogWarning("[Practice] 没有 `WindowsManager`，开不了 `Deck info Popup`");
                return null;
            }
            var w = DeckInfoPopup.Create(Manager, DeckIndex);
            Manager.OpenWindow(w);
            LastDeckInfo = w;
            Debug.Log("[Practice] `Show Deck Content` ⇒ 开 `Deck info Popup`"
                      + "（原版 `cardInDeckInfoButton → OpenWindow(new DeckInfoContext(deck, 2, …))`）");
            return w;
        }

        /// <summary>最近一次开出来的卡组详情窗（自检用）。</summary>
        public static DeckInfoPopup LastDeckInfo;

        /// <summary>`Toggle(bool)` —— 原版 `DeckGeneralInfoDemo.Toggle`：**总览 / 卡列表两个抽屉互斥**。
        /// `generalInfo == true` ⇒ 显示 `General container`、关掉 `Deck List Drawer`（反之亦然）。</summary>
        public void Toggle(bool generalInfo)
        {
            if (_general != null) _general.gameObject.SetActive(generalInfo);
            if (_deckList != null) _deckList.gameObject.SetActive(!generalInfo);
        }

        /// <summary>切到「另一个抽屉」（自检/将来接入口用）。返回切换后**是不是总览**。</summary>
        public bool ToggleDeckInfo()
        {
            bool general = _general == null || !_general.gameObject.activeSelf;
            Toggle(general);
            return general;
        }

        /// <summary>现在显示的是不是总览（= `General container` 开着）。</summary>
        public bool ShowingGeneralInfo { get { return _general != null && _general.gameObject.activeSelf; } }

        /// <summary>当前选中的卡组（默认 = `DeckLibrary` 的当前那套）。</summary>
        public int DeckIndex;
        /// <summary>在「预组卡组」那一页选中的那副（空 = 用的是「我的卡组」）。
        /// ⚠️ **还不能拿去开战** —— 原因与出处见 `PrebuiltDecks.WarnNotPlayableYet`。</summary>
        public PrebuiltDecks.Deck PickedPrebuilt;
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

        /// <summary>⚠️ **卡组行的行高是我们挑的** —— 原版 `Decks Scroll view` 里的格没给尺寸（§二 A 只给了视口）。</summary>
        public const float DeckRowH = 38f, DeckRowGap = 6f;

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
            // 🔴 **2026-09-24 修**：原来传的是 `960,540,1920,1080` —— 那是**右下四分之一**的角色！
            //    `Solid(parent,x1,y1,x2,y2,…)` 是**矩形语义**（原版 = -1327.30,-746.18→3247.30,1826.18）。
            //    （`DeckInfoPopup` 那个 `Solid` 是另一种签名 `(cx,cy,w,h)`，两处的调用串了。）
            Solid(root, -1327.30f, -746.18f, 3247.30f, 1826.18f, ShadeColor, QPr, "Menu Dark Background");
            HitOn(root, root, "BackdropHit", new PxRect(0f, 0f, 1920f, 1080f), () => Close(), QPrHit - 1);

            // 2) `Back`（圆钮 + 箭头 + 文案）
            Img(root, "UI_Button_Round_background", BackL, BackT, BackR, BackB, "Back Bg", QPrRow, true);
            var backIc = Img(root, "40k_UI_bt_back", BackIcL, BackIcT, BackIcR, BackIcB, "Back Icon", QPrRow, true);
            Txt(root, "Back", BackTxL, BackTxR, BackT, BackB, 45f, Align.Left, "Back Text", QPrText);
            // 🆕 A17：原版 `Practice Mode Menu` 的 `Back button` 是 SpriteSwap（普查 §块 5 第 21 行）
            var backHit = Hit(root, root, "BackHit", new PxRect(BackL, BackT, BackR, BackB), () => Close());
            var backWb = backHit != null ? backHit.GetComponent<WindowButton>() : null;
            if (backWb != null) backWb.Bind(backIc, "40k_UI_bt_back");

            // 3) 选卡组那一列
            // ⚠️ 原版这一件是 **Sliced**（贴图 439×664 · border (0,325,0,35)）—— 原来按 Simple+keepAspect 拉的
            Nine(root, root, "UI_Deck_Selection_Back", new Vector4(0f, 325f, 0f, 35f), 439f, 664f,
                 DbtnL, DbtnT, DbtnR, DbtnB, QPr, "Deck Buttons");
            Txt(root, "Select deck to play", TipL, TipR, TipT, TipB, 38f, Align.Center, "tooltip", QPrText);
            BuildArmySelector(root);
            BuildDeckRows(root);

            // 4) 右半：卡组信息（`Deck info` 容器 → 红底 + 卡组名/督军名/阵营图 + 两个互斥抽屉）
            //    ⚠️ 红底**不是** `Deck info` 自己那一层画的（见 `BuildDeckInfo` 的注释）
            BuildDeckInfo(root);

            // 5) 底下那一条：`Game mode` 开关 + `Battle!`
            Img(root, "40_main_bt_toggle_off", TogL, TogT, TogR, TogB, "Toggle Bg", QPrRow, true);
            Txt(root, "Game mode", TogL, TogR, TogT, TogB, 36f, Align.Center, "Toggle Label", QPrText);
            HitOn(root, root, "ToggleHit", new PxRect(TogL, TogT, TogR, TogB), () =>
                NotBuilt("`Game mode` 开关（原版切 Classic/Skirmish 两种赛制；本地只有一个卡池口径）"));

            Img(root, "40k_bt_underbutton", ContL, ContT, ContR, ContB, "Continue Button", QPrRow, true,
                new Color(0.369f, 0.894f, 0.587f, 1f));
            Txt(root, "Battle!", BtTxL, BtTxR, BtTxT, BtTxB, 45f, Align.Right, "Battle Text", QPrText);
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
                // 🔴 **2026-10-03 就地更正（A17 顺带查出的偏离）**：原来这里画的是**一块纯色**（我们自建），
                // 而**原版 `Practice Deck` 的根就是一张 `UI_Button_Mulligan`**（实测：324.5×80.1 Simple、
                // `trans=2` → HL `UI_Button_Mulligan_hover`；见普查 §块 5 第 22 行）。
                // ⇒ 改成画那张图，**选择态仍用我们原来那层色**（黄色 = 当前这套；原版怎么标当前套本地判据不足）。
                var rowBg = Img(cell.transform, "UI_Button_Mulligan", rr.x1, rr.y1, rr.x2, rr.y2, "Row Bg", QPrRow, false);
                if (rowBg != null)
                    rowBg.SetTint(cur ? new Color(1f, 0.773f, 0f, 0.55f) : new Color(1f, 1f, 1f, 0.10f));
                Txt(cell.transform, info.Name, rr.x1 + 10f, rr.x2 - 10f, rr.y1, rr.y2, 26f, Align.Left,
                    "Name", QPrText);
                int idx = i;
                var rowHit = HitOn(cell.transform, cell.transform, "Hit", rr, () => PickDeck(idx), QPrHit);
                var rowWb = rowHit != null ? rowHit.GetComponent<WindowButton>() : null;
                if (rowWb != null) rowWb.Bind(rowBg, "UI_Button_Mulligan");
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

        /// <summary>开 `Deck Selection Popup with Tabs`（原版 `DeckGeneralInfoDemo.ChangePlayerDeckButton` 那条）。</summary>
        public DeckSelectionPopup OpenDeckSelection()
        {
            LastDeckSelection = null;
            if (Manager == null)
            {
                Debug.LogWarning("[Practice] 没有 `WindowsManager`，开不了 `Deck Selection Popup`");
                return null;
            }
            var w = DeckSelectionPopup.Create(Manager, pick =>
            {
                // 回调 = 选中那一套（原版 `DeckSelectionPopup.Select` 的两步：关窗 + 回调）。
                // 🔴 原版的回调是 `Action<CardDeck>`，**预组与自己的卡组走同一条**；我们这边必须分开 ——
                //    预组**不在** `DeckLibrary` 里，拿名字回查 `CollectionData.IndexOf` 必然给 −1，
                //    那就是**静默无事发生**（撞红线）。判据 → `资料/预组卡组_原版规格.md` §五之二 末。
                if (pick.Prebuilt)
                {
                    PickPrebuilt(pick.PrebuiltDeck);
                    return;
                }
                int idx = CollectionData.IndexOf(pick.Info.Name);
                if (idx >= 0) PickDeck(idx);
            }, (int)GameMode.Classic);   // 🆕 2026-09-26：练习 = 经典 ⇒「我的卡组」页只列经典那批
            // ⚠️ 原版练习窗里有个 `Game mode` 开关（经典/冲突）我们**没建**（见本文件头那条 `NotBuilt`），
            //    所以这里**写死经典**；真要支持「练习里也能打遭遇」得先把那个开关做出来。
            Manager.OpenWindow(w);
            LastDeckSelection = w;
            Debug.Log("[Practice] 开 `Deck Selection Popup with Tabs`");
            return w;
        }

        /// <summary>最近一次开出来的选卡组窗（自检用）。</summary>
        public static DeckSelectionPopup LastDeckSelection;

        Transform _cardHolder;

        /// <summary>建（或**重建**）卡列表 —— 挂在 `Deck info/Deck List Drawer` 下（原版那一件的真父是 `Deck info`）。
        /// 🔴 重建前**先把旧的那个销毁** —— 否则每换一次卡组就多留一棵孤儿树。
        ///
        /// **格原点** = `Content` 左上角 + `GridLayoutGroup` 的 pad **(22, 4)**：
        /// 第一格左上 = **(1261.27 + 22, 318.42 + 4) = (1283.27, 322.42)**。
        /// 我们原来起在 `Deck List Drawer` 自己的左上角 (1240.38, 207.65) —— 正好压在
        /// `Deck Name`(224.13→271.82) 与 `Warlord Name`(269.62→303.62) 上（第 53 条那条「字叠成一团」）。</summary>
        void BuildCardRows(Transform parent)
        {
            var old = parent.Find("Deck List Drawer");
            if (old != null) RewardsWindow.DestroySafe(old.gameObject);
            CardRows.Clear();
            _cardHolder = new GameObject("Deck List Drawer").transform;
            _cardHolder.SetParent(parent, false);
            _cardHolder.localPosition = Local3(parent, DlL, DlT, DlR, DlB);
            _deckList = _cardHolder;

            // 抽屉自己那颗钮（原版 `Show Deck General Info button`：`UI_Button_Round_background` + `40k_UI_bt_back`）
            //   ⇒ 原版 `deckGeneralInfoButton → DeckGeneralInfoButtonOnClick`：`General container.SetActive(true)`
            //     + `Deck List Drawer.SetActive(false)`（**切回总览**）。
            Img(_cardHolder, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Info Bg", QPrRow, true);
            Img(_cardHolder, "40k_UI_bt_back", 1737.68f, 246.46f, 1785.07f, 293.12f, "Show Info Icon", QPrRow, true);
            HitOn(_cardHolder, _cardHolder, "ShowInfoHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => Toggle(true));

            // 原版 `Content` 是布局组节点；我们的 `Deck List Drawer` 的 localPosition 已经把它摆到 (1240.38,207.65)，
            // 而 `Content` 的绝对矩形是 1261.27,318.42→1787.37,793.87 ⇒ 子件的坐标**一律用页面绝对 px**，
            // `Local3(_cardHolder, …)` 会自己换算（这是本工程画图小工具的约定）。
            var deck = CollectionData.Raw(DeckIndex);
            if (deck == null) return;
            var order = new List<string>();
            if (!string.IsNullOrEmpty(deck.WarlordId)) order.Add(deck.WarlordId);
            if (deck.CardIds != null)
                foreach (var id in deck.CardIds)
                    if (!string.IsNullOrEmpty(id) && !order.Contains(id)) order.Add(id);

            float oL = DlContentL + DlPadL, oT = DlContentT + DlPadT;
            int cols = ListCols;
            for (int i = 0; i < order.Count; i++)
            {
                var card = CollectionData.Card(order[i]);
                if (card == null) continue;
                int c = i % cols, rr = i / cols;
                float x1 = oL + c * (DlCellW + DlGapX);
                float y1 = oT + rr * (DlCellH + DlGapY);
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
            PickedPrebuilt = null;      // 选了「我的卡组」⇒ 预组那次选中作废（两个是互斥的）
            PrebuiltDecks.ClearPendingBattleDeck();
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            bool wasList = !ShowingGeneralInfo;             // 换卡组时**别把抽屉状态翻掉**
            BuildCardRows(_info);                          // 重建卡列表（卡组换了）
            Toggle(wasList ? false : true);
            var info = CollectionData.DeckAt(i);
            if (_txtDeckName != null) _txtDeckName.SetText(info.Name);
            var wl = CollectionData.Warlord(i);
            if (_txtWarlord != null) _txtWarlord.SetText(wl != null ? wl.Name : "未选督军");
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
            if (_armyIcon != null) _armyIcon.SetTexture(CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)));
            if (_cardback != null) _cardback.SetTexture(CardArt.DeckCardback(info.CardbackId, info.Faction));
            Debug.Log("[Practice] 选中卡组：「" + info.Name + "」");
        }

        /// <summary>
        /// 选中一副**预组卡组**（原版这条是「登记」型：`DeckGeneralInfoDemo.DeckChanged` → 记下用哪副牌，不立刻开战）。
        ///
        /// 我们这边多做一件：把它写进 `PrebuiltDecks` 的**「本局用这副牌」**通道 —— 因为
        /// `StartBotBattle` 原先只认 `DeckLibrary.Current`，而**预组不在玩家的卡组库里**。
        /// </summary>
        public void PickPrebuilt(PrebuiltDecks.Deck d)
        {
            if (d == null) return;
            PickedPrebuilt = d;
            PrebuiltDecks.SetPendingBattleDeck(d);
            // 面板上要显示「选的是哪副」—— 卡列表那是「我的卡组」的，这里只把身份显示对
            if (_txtDeckName != null) _txtDeckName.SetText(d.DisplayName);
            if (_txtWarlord != null)
            {
                var wl = CollectionData.Card(d.heroId);
                _txtWarlord.SetText(wl != null ? wl.Name : "未选督军");
            }
            if (_armyIcon != null) _armyIcon.SetTexture(CardArt.MenuUi(d.FactionIcon));
            if (_cardback != null) _cardback.SetTexture(d.Cardback);
            Debug.Log("[Practice] 选中**预组卡组**「" + d.DisplayName + "」(" + d.deckId + ") ⇒ 本局就用它"
                      + "（防御卡 = 我们补的那张「" + d.defensiveNameZh + "」" + d.defensiveId + "）");
        }

        /// <summary>点 `Battle!` —— 照原版 `PracticeModePopup__BattleButtonOnClick → MatchMakerManager.StartMatch`：
        /// **先开 `Searching Oponent Popup` 等 12 秒**（离线时「不能匹配真人」那一支的常量，见 `SearchingMatchPopup`），
        /// 等不到真人再打 bot。真正的开战在 `StartBotBattle`。</summary>
        public void StartBattle()
        {
            var info = CollectionData.DeckAt(DeckIndex);
            if (string.IsNullOrEmpty(info.WarlordId))
            {
                Debug.LogWarning("[Practice] 这套卡组**没有督军**，开不了局 —— 如实说，不静默。");
                if (Manager != null) Manager.ShowPopUp("这套卡组还没有选督军，开不了局。", "知道了", null);
                return;
            }
            // 🆕 2026-09-26（N3）：**联机已连上 ⇒ 走 P2P，不跑那 12 秒 bot 链**
            //    （判据 → `资料/联机P2P_设计与交接.md` §六 N3）。没接管时照旧 —— 单机行为一字不改。
            {
                var pre0 = PrebuiltDecks.PendingSource;
                var pd = pre0 != null ? PrebuiltDecks.ToPlayerDeck(pre0) : CollectionData.Raw(DeckIndex);
                if (NetMatchmaking.TryStart(pd, "Classic",
                                            pre0 != null ? pre0.faction : info.Faction, out string netWhy))
                {
                    Debug.Log($"[Practice] 这一局走**联机**（本机交了卡组「{info.Name}」）—— 不跑 12 秒 bot 链");
                    // 🆕 2026-10-03（A2）：**把「在等对面」显示出来** —— 原来是屏幕上什么都没有。
                    //    练习窗是 `currentWindow` ⇒ 用**窗内**那扇 `Searching Oponent Popup`（不开全屏那扇）。
                    NetTookOver = true;
                    if (_search == null) AttachSearch();
                    _search.OnCancel = CancelSearch;
                    _search.BeginNetWait();
                    return;
                }
                Debug.Log($"[Practice] 联机没接管（{netWhy}）⇒ 照旧走「等 12 秒再打 bot」那条链");
                // 🔴 **红线**（`项目任务.md` §三 第 14 条 表 第 5 条）：配了联机却没连上要说一声。
                //    ⚠️ 判定「该不该说」在那一处（单机玩家不打扰）—— 别在这儿再写一遍。
                NetMatchmaking.ExplainNotTakingOver(netWhy);
            }
            if (_search == null) AttachSearch();
            _search.OnSearchDone = StartBotBattle;
            _search.BeginSearch();
        }

        /// <summary>建窗内那扇 `Searching Oponent Popup`（出厂关着）并把取消接到 `CancelSearch`。</summary>
        void AttachSearch()
        {
            _search = SearchingMatchPopup.Attach(transform, "Searching Oponent Popup");
            _search.OnCancel = CancelSearch;
        }

        /// <summary>🆕 2026-10-03（A1/A2）：这一局**交给联机了**（`StartBattle` 里 `TryStart` 返回真）。</summary>
        public bool NetTookOver { get; private set; }

        /// <summary>取消匹配（窗内那扇 `Cancel` / 点背板 / ESC 都走它）。
        /// 🆕 2026-10-03：**联机那一支要真拆局**（原来只关窗 ⇒ 对面照样把你拉进战场）；
        /// ⚠️ **这条链是我们设计的、不是复刻** —— 判据 → `NetMatchmaking.Cancel`。</summary>
        public void CancelSearch()
        {
            Debug.Log("[Practice] 取消匹配（原版 `MatchMakerManager.CancelSearch`）");
            if (!NetTookOver) return;
            if (NetMatchmaking.Cancel("对局发起方点了取消", out string why))
            {
                NetTookOver = false;
                NetRuntime.Notice("已经取消这一局的联机匹配 —— 对面会收到通知，**双方都没有开局**。\n"
                                + "想再打一次：两边各自重新点一次 `Battle!`。");
            }
            else
            {
                NetRuntime.Notice("取消不了这一局：" + why);   // **不假装取消成功**
            }
        }

        /// <summary>推进匹配（`Update` 与自检都走它 —— 批处理没有帧循环）。</summary>
        public void TickSearch(float dt) { if (_search != null) _search.Tick(dt); }

        void Update() { TickSearch(Time.deltaTime); }

        SearchingMatchPopup _search;

        /// <summary>等满 12 秒 ⇒ 选定卡组 + 切该阵营那份对战场景（= 原版 `StartBattle → LoadScene` 的等价物）。</summary>
        public void StartBotBattle()
        {
            var info = CollectionData.DeckAt(DeckIndex);
            // 🔴 **本局用哪副牌**：在选卡组窗里挑过**预组**就走预组那条（`PrebuiltDecks` 那条通道），
            //    否则照旧用「我的卡组」（`DeckLibrary.Current`）。两条路都经过 `BattleDriver.Begin(myDeck:)`。
            var pre = PrebuiltDecks.PendingSource;
            string faction = pre != null ? pre.faction : info.Faction;
            string deckName = pre != null ? pre.DisplayName : info.Name;
            if (pre != null)
            {
                Debug.Log("[Practice] 本局用**预组卡组**「" + deckName + "」(" + pre.deckId + ")"
                          + "（防御卡用我们补的「" + pre.defensiveNameZh + "」" + pre.defensiveId
                          + "；原版预组那份是 null，见 `资料/预组卡组_原版规格.md` §五之七）");
            }
            else
            {
                CollectionData.Select(DeckIndex);  // `BattleDriver.PickSavedDeck` 读的就是 `DeckLibrary.Current`
            }
            StartedBattle = true;
            // 🔴 2026-09-30（§27）：`BattleSceneNameFor` 现在恒为 `Battle`；
            //    「哪一场」由运行时按 `SceneFor(faction)` 取 prefab（见 `ArenaRuntimeLoader`）。
            var scene = ArenaByArmy.BattleSceneNameFor(faction);
            Debug.Log("[Practice] 开战：「" + deckName + "」→ 切 `" + scene + ".unity`"
                      + "（原版走 `StartMatch → StartBotBattle → StartBattle → LoadScene`，唯一 LoadScene 点；"
                      + " 照原版查表，督军阵营「" + faction + "」该去 `" + ArenaByArmy.OriginalNameFor(faction)
                      + "`（我们的键 `" + ArenaByArmy.SceneFor(faction) + "`）"
                      + (scene == "Battle"
                         ? " —— ⚠️ **该场那份场景还没建，这一局用的是兜底 `Battle`（战场 = " + ArenaByArmy.DefaultScene + "）**"
                         : "）"));
            if (Application.isBatchMode) { Debug.Log("[Practice] （批处理：不切场景，只记账）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
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
            => ImgTex(parent, CardArt.MenuUi(art), art, x1, y1, x2, y2, name, q, keepAspect, tint);

        /// <summary>同 `Img`，但**直接给图**（原版不少件的图是运行时喂的，比如 `Army Image` / `Cardback`）。</summary>
        ImageQuad ImgTex(Transform parent, Texture2D tex, string what, float x1, float y1, float x2, float y2,
                         string name, int q, bool keepAspect, Color? tint = null)
        {
            if (tex == null) { Debug.LogWarning("[Practice] 图取不到，这一层不画：" + what); return null; }
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
