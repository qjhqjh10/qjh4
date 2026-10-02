// DeckInfoPopup.cs — 卡组线 `Deck info Popup`（原版 **`DeckInfoPopup`**）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0923/A1_外壳与弹窗.md` **§2**（逐节点表：路径 / sprite / rect / 锚点五元组 / 字号 / act / 组件）。
// 窗口参数（同表表头）：`type=1 Popup` · `windowsPlacement=15` · `closeOnESC=1`。
//
// 🔴 **它接上了一个原本用「再点一下」顶着的缺口**：收藏窗 Deck 页里，点一格卡组 = 选中，
//    原版是「选中 ⇒ 开这扇窗 ⇒ 窗里点 `Edit Deck` 才进编辑」。我们上一轮没建这扇窗，
//    在 `CollectionWindow.SelectDeck` 里**用「再点一下同一格 = 进编辑」顶着**（当时就出声了）。
//    ⇒ 现在这扇窗建起来了；那条顶替路径**保留**（自检还断它），但玩家有了原版的那条路。
//
// ============================ 结构（照 A1 §2 抄，逐条有出处）============================
//   · `Menu Dark Background` 色 **(0,0,0,.773)**（原版 rect 比屏幕大，是为了盖住任何画幅）
//   · `Generic Window Red Background Big` = **`UI_Deck_Information_Back`**（Sliced，border 42/363/655/81）
//   · `Warlord Image`（**原版 sprite=0、运行时喂**）⇒ 我们用督军立绘
//   · `Deck Details` → `Game Mode Separator`（`40k_Generic Smooth line`，色 (.42,.157,.137,1)）
//     + `Army Icon` + `Deck Name`（fs44.5 居中）+ `Warlord Name`（fs40 居中）
//   · `Buttons`（HLG spacing **36** align **MiddleRight**）→ 三个 `UI_Button_Mulligan` **324.5×80.1**
//   · `Deck Options`（HLG spacing **−50** align MiddleRight **reverse**）→ 五个圆钮 **74.386×75.605**
//   · `Info Panel` = `UI_Deck_Information_submenu_Back`（Sliced，border 18）→ `Deck List`
//     （GridLayoutGroup **cell 360×58 · spacing 11/4.5 · pad 15/0/15/0**）
//   · `Generic Close Button Orange` = 圆钮 + `40k_general_bt_yellow_close`
//
// ---- 没建的（出声，不静默）----
//   ✅ **2026-10-03（§三第29条 A10）四件都补上了** —— 原来这四条里**两条的记录本身就是过期的**：
//   · `Game Mode Icon` —— ✅ **画上了**（两张图 `40k_gamemode_icon_{classic,skirmish}` **本来就在工程里**，
//     原来记的「本地没有那两张图」是**过期**的；按卡组的 `gameMode`（0 经典 / 13 遭遇）选图）
//   · `Deck Info` 抽屉（费用曲线 / 卡背）—— ✅ **建了**（`Switch Deck Info` 真的切两个抽屉；
//     逐值 → 本文件 `BuildDeckInfoDrawer` 的注释）。⚠️ `Lore Text` **仍不建**：原版出厂 `act=N` +
//     我们引擎**没有 lore 字段**（同 `CardDetailPopup` 那条老账）
//   · `Share` / `Share On Chat` —— ✅ **接了**：原版走平台/服务端，我们**给卡组串**（`DeckLibrary.ExportString`）
//     + 如实说明聊天那条发不出去（`ChatPanel` 自己就写着「没有服务器」）
//   · `Practice Deck` —— ✅ **接了**（选中/开练习窗）。🔴 **但有一层没复刻**：原版是
//     `SelectPracticeOpponentDeck` ⇒ 这一副当**【对手】**卡组（`enemyDeck = 刚选中的那副`，判据 →
//     `资料/预组卡组_原版规格.md` §五之二 第 4 行）；我们的练习窗只认**玩家自己**的卡组
//     ⇒ **已记进待办**（`项目任务.md` §三第29条 A10 的尾巴），别当已复刻。
//   · 🆕 同批补的：`Warlord Image` 那层的 `EverguildButton` 点击（原版开卡详情窗）—— 详情窗 09-24 就建好了，
//     原来那句「那扇窗还没建」也是过期的。
//   · 🆕 同批**就地订正两处真缺陷**（fresh dump 抓出来的，见常量那段）：`Deck Details` 整块原来用的是
//     **布局组跑之前的模板位**（`Army Icon` 画到容器外）· `Deck Options` 五颗的**左右顺序反了**。
//
// ---- 🔴 一处**原版数据本身就重叠**（照画，但出声）----
//   `Deck Details` 那两个文本的 rect **伸进 `Info Panel` 里**：
//   `Deck Name` y 170.05→224.55、`Warlord Name` y 222.30→272.30（x 468.10→955.10），
//   而 `Info Panel` 是 **(659,218.10)→(1799,868.10)**，`Deck List` 的第 1 行就从面板顶边起排。
//   ⇒ 重叠区 x∈[659,955]、y∈[218,272]，**两段字与第一行卡名压在一起**（截图 `05_收藏_DeckInfo弹窗.png`）。
//   · 我们**照 rect 画**（没挪），但把面板画在文本**下面**（原版兄弟序是 `Deck Details` 在前、
//     `Info Panel` 在后 ⇒ 面板会**盖掉**督军名右半）—— 盖掉更难解释，所以反过来。
//   ⚠️ **没跑实况核过**（原版这时长什么样，要么联网跑客户端、要么等 P2P 那条线，见 `项目任务.md` §三 第 15 条）。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>`DeckInfoPopup : GameWindow` —— 收藏窗 Deck 页点一格卡组开的那扇窗。</summary>
    public class DeckInfoPopup : GameWindow
    {
        // 层：**必须高于收藏窗那一整片**（它最高到 `CollectionWindow.QFltHit = 3043`）。
        // ⚠️ 但**低于** `PromptPopup`（3140+）—— 提示窗要能压在这扇窗之上。
        public const int QDI = 3120, QDIRow = 3121, QDIText = 3122, QDIHit = 3123;

        // ---- 几何（A1 §2 逐条）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        public const float RedL = 134.50f, RedT = 82f, RedR = 1839.50f, RedB = 1032f;
        public const float WarlordL = -108.98f, WarlordT = -33.99f, WarlordR = 999.02f, WarlordB = 1074f;
        public const float SepL = 759.0f, SepT = 106.7f, SepR = 767.9f, SepB = 216.7f;
        public const float DdIconL = 767.9f, DdIconT = 106.7f, DdIconR = 867.9f, DdIconB = 216.7f;
        public const float DdNameL = 872.9f, DdNameT = 114.4f, DdNameR = 1358.5f, DdNameB = 168.9f;
        public const float DdWlL = 872.9f, DdWlT = 166.7f, DdWlR = 1359.9f, DdWlB = 216.7f;
        // 🔴 **2026-10-03 就地订正（A10）**：上面四个常量原来抄的是 `A1 §2` 那张表的值
        //   （`SepL=659 / DdIconL=363.10 / DdNameL=468.10 / DdWlL=468.10`）—— **那是布局组跑【之前】的模板位**。
        //   实据：`python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 4` 末尾那行
        //   「⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：Deck Details → HorizontalLayoutGroup [ok]」，
        //   跑后 `Game Mode Icon` **659.0,106.7→759.0,216.7** · `Separator` **759.0..767.9** ·
        //   `Army Icon` **767.9..867.9** · `Deck Name` **872.9,114.4→1358.5,168.9** · `Warlord Name` **872.9,166.7→1359.9,216.7**
        //   （三者首尾相接 = 布局真的跑过）。**旧值把整块画到了容器左边之外**（`Army Icon` 363.10 落在 `Deck Details` 659..1329.8 之外）。
        //   ⚠️ 同批订正的还有两处**对齐**：`Deck Name` 是 **Left/Bottom**、`Warlord Name` 是 **Left/Middle**（原来都按 Center 画）。
        /// <summary>`Buttons` 行：三个 **324.5×80.1**、spacing **36**、右对齐到 **1770.70**、y **885.81**。</summary>
        public const float BtnL = 725.20f, BtnT = 885.81f, BtnW = 324.5f, BtnH = 80.1f, BtnGap = 36f;
        /// <summary>`Deck Options` 行：五个圆钮、spacing **−50**、右对齐到 **1783.62**、y **130.20**。</summary>
        public const float OptR = 1783.62f, OptT = 130.20f, OptW = 74.386f, OptH = 75.605f, OptStep = OptW - 50f;
        /// <summary>圆钮里 `Background`/`Icon` 那两层的上下边（A1 §2：1790.96,71.18→1847.82,129.30）。</summary>
        public const float OptIconT = 71.18f, OptIconH = 58.12f, OptIconW = 56.86f;
        public const float PanelL = 659f, PanelT = 218.10f, PanelR = 1799f, PanelB = 868.10f;
        public const float RowL = PanelL + 15f, RowT = PanelT, RowW = 360f, RowH = 58f, RowGapX = 11f, RowGapY = 4.5f;
        public const float CloseL = 1782.81f, CloseT = 63.20f, CloseR = 1857.19f, CloseB = 138.80f;

        static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        static readonly Vector4 PanelBorder = new Vector4(18f, 18f, 18f, 18f);
        const float PanelTexW = 69f, PanelTexH = 63f;

        // ============================================================ 🆕 2026-10-03（§三第29条 A10）
        // 补齐的四件（**逐值来自 2026-10-03 的 fresh dump**）：
        //   `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`
        // 🔴 **A1 §2 那张表里 `Game Mode Icon` 等几个给的是【布局组跑之前的模板位】**（y 差 55.6px）
        //    —— 那正是铁律 5·c 说的「一个值 ≠ 全部情况」；下面这几个一律按 **fresh dump 的跑后值**。

        /// <summary>`Deck Details/Game Mode Icon`：跑后 **659.0,106.7 → 759.0,216.7**（100×110 · `preserveAspect`）。
        /// 原版 `sprite = 0`，运行期按 `gameModeDeckIcon` 喂 —— 图在工程里（`40k_gamemode_icon_{classic,skirmish}`）。</summary>
        public const float DdGmL = 659.0f, DdGmT = 106.7f, DdGmR = 759.0f, DdGmB = 216.7f;
        /// <summary>`Deck Info/Deck Information Cost/balance text`：440 宽那条标题（原版是**葡语占位** `Cartas / Coste`）。</summary>
        public const float DiHeadL = 740.0f, DiHeadT = 305.6f, DiHeadR = 1213.8f, DiHeadB = 365.6f;
        /// <summary>`Deck Information cost drawer`：rect **160×200** 但 **`localScale = 1.8`** ⇒ 真画出来 **288×360**，
        /// 中心 **(969.40, 558.80)**。里面 `Content` 是 VLG（spacing 3.43）、**9 行**（费用 0..8）。</summary>
        public const float DiDrawerCX = 969.40f, DiDrawerCY = 558.80f, DiDrawerScl = 1.8f;
        public const float DiRowW = 223.59f, DiRowH = 18.91f, DiRowStep = 22.295f;
        public const int DiRowCount = 9;
        /// <summary>`Cardback`：rect **210×305** 但 **`localScale = 1.85`** ⇒ 真画 **388.5 × 564.25**，中心 **(1466, 552.6)**。
        /// ⚠️ 原版是**两层**（`Cardback` + 子 `Cardback Front` 209.56×302.33，子件中心比父**偏 (+11.2, −4.9)**、
        /// 再乘 1.85）—— 我们只画父那一块（同一张图，画两层只是重一遍）。</summary>
        public const float DiCbCX = 1466f, DiCbCY = 552.6f, DiCbScl = 1.85f;
        public const float DiCbW = 210f, DiCbH = 305f;

        /// <summary>现在展示的是哪个抽屉：`false` = `Deck List`（出厂）· `true` = `Deck Info`。</summary>
        public bool InfoDrawerShown { get; private set; }
        /// <summary>`Deck Info` 抽屉那 9 行的读数（自检用）。</summary>
        public readonly List<int> CostRowCounts = new List<int>();

        /// <summary>卡组列表列数 = `floor((1140 − 15 − 15 + 11) ÷ (360 + 11))` = **3**（照 GridLayoutGroup 那套算）。</summary>
        public static int ListCols
        {
            get { return Mathf.Max(1, Mathf.FloorToInt((PanelR - PanelL - 30f + RowGapX) / (RowW + RowGapX))); }
        }

        public int DeckIndex;
        /// <summary>画出来的卡行数（自检用）。</summary>
        public readonly List<Transform> Rows = new List<Transform>();
        /// <summary>三个大钮的点击区（自检按名字找）。</summary>
        public Transform Btn(string n) { return Find(transform, "Buttons/Btn_" + n); }
        /// <summary>五个圆钮的点击区。</summary>
        public Transform Opt(string n) { return Find(transform, "Deck Options/Opt_" + n); }

        static Transform Find(Transform root, string path)
        {
            if (root == null) return null;
            var parts = path.Split('/');
            var t = root;
            foreach (var p in parts)
            {
                t = t.Find(p);
                if (t == null) return null;
            }
            return t;
        }

        public static DeckInfoPopup Create(WindowsManager mgr, int deckIndex)
        {
            var go = new GameObject("Deck info Popup");
            var win = go.AddComponent<DeckInfoPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;
            win.DeckIndex = deckIndex;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            Rows.Clear();

            var info = CollectionData.DeckAt(DeckIndex);

            // 1) 压暗整屏（原版 rect 比屏幕大 ⇒ 我们直接铺满可见区；断言量的是「铺满」）
            Solid(root, root, 960f, 540f, 1920f, 1080f, ShadeColor, QDI, "Menu Dark Background");

            // 2) 红底（Sliced）
            Nine(root, root, "UI_Deck_Information_Back", RedBorder, RedTexW, RedTexH,
                 RedL, RedT, RedR, RedB, QDI, "Generic Window Red Background Big");

            // 3) 督军立绘（原版 sprite=0 运行时喂）
            var wl = CollectionData.Warlord(DeckIndex);
            var wlTex = wl != null ? CardArt.PortraitByName(wl.Name) : null;
            if (wlTex != null)
                Img(root, root, wlTex, WarlordL, WarlordT, WarlordR, WarlordB, "Warlord Image", QDI, true);
            else
                Debug.Log("[DeckInfo] 督军立绘取不到（" + (wl != null ? wl.Name : "没有督军") + "）—— 那一层不画，出声");
            // 🆕 A10：`Warlord Image` 那一层**本来就是 `EverguildButton`**（原版点了开卡详情窗）——
            //    原来记的「那扇窗还没建」是**过期**的（`CardDetailPopup` 2026-09-24 就建好了）⇒ 接上。
            Hit(root, root, "WarlordHit", new PxRect(WarlordL, WarlordT, WarlordR, WarlordB), OpenWarlordDetail);

            // 4) `Deck Details`（**跑后真值**，见常量那段的订正）
            //    HLG{align=MiddleLeft} ⇒ 视觉顺序 = 树序：`Game Mode Icon` → `Separator` → `Deck Details`(内层)
            // 🆕 A10：`Game Mode Icon` 补上了（原版 `sprite=0`、运行期按 `gameModeDeckIcon` 喂；
            //    两张图 `40k_gamemode_icon_{classic,skirmish}` **本来就在工程里** —— 原来记的「本地没图」是**过期**的）
            {
                string gmArt = info.GameMode == 13 ? "40k_gamemode_icon_skirmish" : "40k_gamemode_icon_classic";
                Img(root, root, CardArt.MenuUi(gmArt), DdGmL, DdGmT, DdGmR, DdGmB, "Game Mode Icon", QDIRow, true);
            }
            Nine(root, root, "40k_Generic_Smooth_line", new Vector4(55f, 15f, 55f, 15f), 113f, 32f,
                 SepL, SepT, SepR, SepB, QDIRow, "Game Mode Separator", new Color(0.42f, 0.157f, 0.137f, 1f));
            var facTex = string.IsNullOrEmpty(info.Faction) ? null : CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction));
            if (facTex != null)
                Img(root, root, facTex, DdIconL, DdIconT, DdIconR, DdIconB, "Army Icon", QDIRow, true);
            Txt(root, root, info.Name, DdNameL, DdNameT, DdNameR, DdNameB, 44.5f, Align.Left, "Deck Name", QDIText);
            Txt(root, root, wl != null ? wl.Name : "未选督军", DdWlL, DdWlT, DdWlR, DdWlB, 40f, Align.Left,
                "Warlord Name", QDIText);

            // 5) `Info Panel` + 两个抽屉（`Deck List` 出厂在前、`Deck Info` 出厂 **INACT** ⇒ 建了关着）
            Nine(root, root, "UI_Deck_Information_submenu_Back", PanelBorder, PanelTexW, PanelTexH,
                 PanelL, PanelT, PanelR, PanelB, QDI, "Info Panel");
            BuildDeckList(root);
            BuildDeckInfoDrawer(root);

            // 6) `Buttons`：右对齐到 1770.70、spacing 36 ⇒ 最左一格 = 1770.70 − (3×324.5 + 2×36)
            {
                var holder = new GameObject("Buttons");
                holder.transform.SetParent(root, false);
                holder.transform.localPosition = Local3(root, BtnL, BtnT, BtnL + 3f * BtnW + 2f * BtnGap, BtnT + BtnH);
                string[] btns = { "Practice Deck", "Edit Deck", "Select Deck" };
                for (int i = 0; i < btns.Length; i++)
                {
                    // ⚠️ **坐标一律是「页面绝对 px」、basis 给【实际父节点】** ——
                    //    `Local3(basis, …)` 算的是 `RectCenter(绝对px) − basis.position`，
                    //    所以它必须与 parent 一致，否则整块会**再叠一层父节点的偏移**。
                    //    🔴 2026-09-23 两种错法都踩过：① 「容器内坐标 + basis」→ 飞走；
                    //      ② 「绝对坐标 + basis=root 而 parent 是容器」→ 叠一层容器偏移
                    //      （按钮量出来 1175.40，期望 887.45，差的就是容器中心 287.95）。
                    float x1 = BtnL + i * (BtnW + BtnGap);
                    var r = new PxRect(x1, BtnT, x1 + BtnW, BtnT + BtnH);
                    // ⚠️ `UI_Button_Mulligan` **不是九宫格图** —— 实读 `Sprite/UI_Button_Mulligan.json`：
                    //    **`m_Border = None`**，图 410×124。原版标 `type=Sliced` 但**没有 border 就等于拉伸**
                    //    ⇒ 正确做法是走普通 `Img`（拉伸）。原来走 `Nine` + `border=(333,96,333,96)`：
                    //    **左+右 = 666 > 图宽 410** ⇒ 每建一次吐一条「Nine: border 比图还大，退回单块」，
                    //    画面一样但日志被刷脏（§三 第 15 条 **第 57 行**）。
                    //    卡片详情窗 2026-09-24 已经这么改过（`CardDetailPopup.cs` 的 `Craft Bg` 那条）。
                    Img(holder.transform, holder.transform, CardArt.MenuUi("UI_Button_Mulligan"),
                        r.x1, r.y1, r.x2, r.y2, "Bg " + btns[i], QDIRow, false);
                    Txt(holder.transform, holder.transform, btns[i], r.x1, r.y1, r.x2, r.y2, 34f, Align.Center,
                        "Text " + btns[i], QDIText);
                    string key = btns[i];
                    Hit(holder.transform, holder.transform, "Btn_" + key, r, () => OnButton(key));
                }
            }

            // 7) `Deck Options`：spacing **−50** · MiddleRight ⇒ 相邻两颗叠 50px
            //    🔴 **2026-10-03 就地订正（A10）**：顺序原来**反了**。原版左→右 = **树序**：
            //      `Switch Deck Info`(1611.7) → `Duplicate`(1636.1) → `Share`(1660.5) → `Share On Chat`(1684.8) → `Delete`(1709.2)
            //      （实据 = fresh dump 的跑后 x；步进 24.386 = 74.386 − 50）。
            //      我们原来是「`Delete` 最左」—— 那条注释猜的是「`reverse=1` ⇒ 与树序相反」，**实测不成立**。
            {
                var holder = new GameObject("Deck Options");
                holder.transform.SetParent(root, false);
                float total = OptW + (5 - 1) * OptStep;
                holder.transform.localPosition = Local3(root, OptR - total, OptT, OptR, OptT + OptH);
                string[][] opts =
                {
                    new[] { "Switch Deck Info", "40k_general_bt_yellow_seedeck" },
                    new[] { "Duplicate",        "40k_general_bt_yellow_duplicate" },
                    new[] { "Share",            "40k_general_bt_yellow_share" },
                    new[] { "Share On Chat",    "40k_general_bt_yellow_share in chat" },
                    new[] { "Delete",           "40k_general_bt_yellow_delete" },
                };
                float optX0 = OptR - total;
                for (int i = 0; i < opts.Length; i++)
                {
                    float x1 = optX0 + i * OptStep;
                    var r = new PxRect(x1, OptT, x1 + OptW, OptT + OptH);
                    Img(holder.transform, holder.transform, CardArt.MenuUi("UI_Button_Round_background"),
                        r.x1, r.y1, r.x2, r.y2, "Bg " + opts[i][0], QDIRow, true);
                    float fx1 = r.x1 + (OptW - OptIconW) * 0.5f;
                    float fy1 = OptT + (OptH - OptIconH) * 0.5f;
                    Img(holder.transform, holder.transform, CardArt.MenuUi("40k_general_bt_yellow"),
                        fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Face " + opts[i][0], QDIRow, true);
                    Img(holder.transform, holder.transform, CardArt.MenuUi(opts[i][1]),
                        fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Icon " + opts[i][0], QDIRow, true);
                    string key = opts[i][0];
                    Hit(holder.transform, holder.transform, "Opt_" + key, r, () => OnOption(key));
                }
            }

            // 8) 关闭圆钮
            {
                var r = new PxRect(CloseL, CloseT, CloseR, CloseB);
                Img(root, root, CardArt.MenuUi("UI_Button_Round_background"), r.x1, r.y1, r.x2, r.y2,
                    "Close Bg", QDIRow, true);
                float fx1 = r.x1 + (r.W - OptIconW) * 0.5f, fy1 = OptIconT;
                Img(root, root, CardArt.MenuUi("40k_general_bt_yellow"),
                    fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Close Face", QDIRow, true);
                Img(root, root, CardArt.MenuUi("40k_general_bt_yellow_close"),
                    fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Close Icon", QDIRow, true);
                Hit(root, root, "CloseHit", r, () => Close());
            }
        }

        /// <summary>`Deck List`：卡组里的每一张（**同名合并成 `xN`**）摆成 3 列 × 360×58 的格。</summary>
        void BuildDeckList(Transform root)
        {            var deck = CollectionData.Raw(DeckIndex);
            if (deck == null) return;

            var order = new List<string>();
            var count = new Dictionary<string, int>();
            if (deck.CardIds != null)
                foreach (var id in deck.CardIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    if (!count.ContainsKey(id)) { count[id] = 0; order.Add(id); }
                    count[id]++;
                }
            if (!string.IsNullOrEmpty(deck.WarlordId)) { count[deck.WarlordId] = 1; order.Insert(0, deck.WarlordId); }

            var holder = new GameObject("Deck List");
            holder.transform.SetParent(root, false);
            _listDrawer = holder.transform;
            int cols = ListCols;
            for (int i = 0; i < order.Count; i++)
            {
                var card = CollectionData.Card(order[i]);
                if (card == null) continue;
                int c = i % cols, rr = i / cols;
                float x1 = RowL + c * (RowW + RowGapX);
                float y1 = RowT + rr * (RowH + RowGapY);
                var node = new GameObject("Row_" + i);
                node.transform.SetParent(holder.transform, false);
                node.transform.localPosition = Local3(root, x1, y1, x1 + RowW, y1 + RowH);

                Solid(node.transform, node.transform, x1 + RowW * 0.5f, y1 + RowH * 0.5f, RowW, RowH,
                      new Color(1f, 1f, 1f, 0.08f), QDIRow, "Row Bg");
                Img(node.transform, node.transform, CardArt.MenuUi("Card_Frame_Cost_Icon"),
                    x1 + 6f, y1 + 9f, x1 + 46f, y1 + 49f, "Cost", QDIRow, true);
                Txt(node.transform, node.transform, card.Cost.ToString(), x1 + 6f, y1 + 9f, x1 + 46f, y1 + 49f, 26f, Align.Center,
                    "Cost Text", QDIText);
                Txt(node.transform, node.transform, card.Name, x1 + 52f, y1, x1 + RowW - 52f, y1 + RowH, 26f, Align.Left,
                    "Name", QDIText);
                Txt(node.transform, node.transform, "x" + count[order[i]], x1 + RowW - 52f, y1, x1 + RowW - 8f, y1 + RowH, 26f,
                    Align.Right, "Count", QDIText);
                Rows.Add(node.transform);
            }
        }

        // ============================================================ 🆕 2026-10-03（A10）：`Deck Info` 抽屉

        Transform _listDrawer, _infoDrawer;

        /// <summary>`Deck Info`（原版 `DeckInfoDrawer`，与 `Deck List` **同矩形**、出厂 `INACT`）。
        /// 逐值 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`（2026-10-03）。
        /// 🔴 **两处 `localScale`**：`Deck Information cost drawer` **1.8**、`Cardback` **1.85**
        /// —— 原版是把小图放大画的，所以真画出来的尺寸 = 序列化 rect × 那个倍数（**照乘**）。</summary>
        void BuildDeckInfoDrawer(Transform root)
        {
            var go = new GameObject("Deck Info");
            go.transform.SetParent(root, false);
            var b = go.transform;
            CostRowCounts.Clear();

            // ① 标题那条（原版是**葡语占位** `Cartas / Coste` · fs44 auto[10,44] · hAlign=Center）
            //    ⇒ 我们写英文 `Cards / Cost`（**这一处文案是我们挑的**，原版那份是占位串）
            Txt(b, b, "Cards / Cost", DiHeadL, DiHeadT, DiHeadR, DiHeadB, 44f, Align.Center,
                "Deck Information Cost/balance text", QDIText);

            // ② 费用曲线（9 行 = 费用 0..8）
            BuildCostDrawer(b);

            // ③ 卡背（`Cardback` 210×305 @ 中心 (1466,552.6) · **scl 1.85**）
            {
                var info = CollectionData.DeckAt(DeckIndex);
                var tex = CardArt.DeckCardback(info.CardbackId, info.Faction);
                float w = DiCbW * DiCbScl, h = DiCbH * DiCbScl;
                if (tex != null)
                    Img(b, b, tex, DiCbCX - w * 0.5f, DiCbCY - h * 0.5f, DiCbCX + w * 0.5f, DiCbCY + h * 0.5f,
                        "Cardback", QDIRow, false);
                else
                    Debug.Log("[DeckInfo] 卡背取不到（`" + (info.CardbackId ?? "") + "` / 阵营 "
                              + (info.Faction ?? "") + "）⇒ `Cardback` 那一层不画，出声");
            }

            // ④ `Lore Text`（出厂 **INACT**）⇒ 照纪律**不建**；而且我们**没有 lore 字段**（老账）
            Debug.Log("[DeckInfo] `Deck Info/Lore Text` **不建**：原版出厂 `act=N`，而且 lore 文本在服务端 —— "
                      + "我们引擎里**没有 lore 字段**（同 `CardDetailPopup` 那条老账）⇒ 如实说明，不编文案");

            go.SetActive(false);           // 出厂 INACT（只有 `Deck List` 那一面开着）
            _infoDrawer = b;
        }

        /// <summary>`Deck Energy Cost Drawer` 那 9 行。**画法只此一份** ⇒ 转调 `Core/CostCurveDrawer`
        /// （练习窗那扇走同一个函数，只是 `localScale` 是 1.2）。几何与判据全在那个文件里。</summary>
        void BuildCostDrawer(Transform b)
        {
            var counts = CostCurveDrawer.Counts(CollectionData.Raw(DeckIndex), CollectionData.Card);
            CostCurveDrawer.Build(b, DiDrawerCX, DiDrawerCY, DiDrawerScl, counts, QDIRow, QDIText);
            CostRowCounts.AddRange(counts);
        }

        // ============================================================ 交互

        void OnButton(string key)
        {
            if (key == "Edit Deck")
            {
                // 「进编辑」**只有一份实现**（`CollectionWindow.GoEdit`）—— 收藏窗那条路也走它
                CollectionWindow.GoEdit(DeckIndex);
                Close();
                return;
            }
            if (key == "Select Deck")
            {
                CollectionData.Select(DeckIndex);
                Debug.Log("[DeckInfo] 已选中「" + CollectionData.DeckAt(DeckIndex).Name + "」");
                Close();
                return;
            }
            if (key == "Practice Deck")
            {
                // 原版这一条 = `DeckInfoPopup.SelectPracticeOpponentDeck` → `StartPracticeMatch`：
                //   `StartMatch(OwnDeckTraining(0xc), …, playerDeck = 自己的卡组, **enemyDeck = 刚选中的那副**)`
                //   ⇒ **这一副是【对手】卡组**（判据 → `资料/预组卡组_原版规格.md` §五之二 第 4 行）。
                // 🔴 **我们做不到那一层**：练习窗只认「本局用哪副牌」= **玩家自己**的卡组，
                //   没有「指定对手卡组」这个入口（🆕 已记成待办 —— `项目任务.md` §三第29条 A10 的尾巴）。
                // ⇒ 这里**只开练习窗**，**不改玩家的选择**（原版也不会改），并**如实出声**。
                if (Manager == null) { Debug.LogWarning("[DeckInfo] 没有 `WindowsManager` ⇒ 开不了练习窗"); return; }
                var w = PracticeModePopup.Create(Manager);
                Manager.OpenWindow(w);
                Close();
                Debug.Log("[DeckInfo] `Practice Deck` ⇒ 开练习窗。⚠️ **原版是把这一副当【对手】卡组**"
                          + "（`enemyDeck = 刚选中的那副`）—— 我们的练习窗还没有「指定对手卡组」那个口子"
                          + "⇒ **这一层没复刻**，已记进 §三第29条 A10 的尾巴");
                return;
            }
            NotBuilt("`" + key + "` —— 见 `Shell/DeckInfoPopup.cs` 文件头「没建的」");
        }

        void OnOption(string key)
        {
            if (key == "Delete")
            {
                if (CollectionData.DeleteDeck(DeckIndex)) { Debug.Log("[DeckInfo] 已删除该卡组"); Close(); }
                else Debug.Log("[DeckInfo] 删不了（`DeckLibrary.Delete` 的规矩：只剩一套时不许删 / 下标越界）");
                return;
            }
            if (key == "Duplicate")
            {
                string nm = CollectionData.DuplicateDeck(DeckIndex);
                if (!string.IsNullOrEmpty(nm)) { Debug.Log("[DeckInfo] 已复制成「" + nm + "」"); Close(); }
                else Debug.Log("[DeckInfo] 复制失败（`DeckLibrary.Duplicate` 返回空）");
                return;
            }
            // 🆕 A10：`Switch Deck Info` —— 原版是 `DeckInfoControls.toggleDrawerButton`，
            // 在 `Deck List` 与 `Deck Info` 两个抽屉之间切（两个是互斥的，同矩形叠着）
            if (key == "Switch Deck Info") { SwitchDrawer(); return; }
            if (key == "Share" || key == "Share On Chat") { ShareDeck(key); return; }
            NotBuilt("`" + key + "` —— 见 `Shell/DeckInfoPopup.cs` 文件头「没建的」");
        }

        /// <summary>🆕 A10：点督军立绘 ⇒ 开**卡片详情窗**（原版 `Warlord Image` 上那个 `EverguildButton`）。</summary>
        void OpenWarlordDetail()
        {
            var wl = CollectionData.Warlord(DeckIndex);
            if (wl == null) { Debug.LogWarning("[DeckInfo] 这副没有督军 ⇒ 开不了详情窗"); return; }
            if (Manager == null) { Debug.LogWarning("[DeckInfo] 没有 `WindowsManager` ⇒ 开不了详情窗"); return; }
            var w = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(w);
            w.ShowCard(wl);
            LastWarlordDetail = w;
            Debug.Log("[DeckInfo] 点督军立绘 ⇒ 开卡片详情窗（「" + wl.Name + "」）");
        }

        /// <summary>🆕 最近一次由「点督军立绘」开出来的卡片详情窗（自检用）。
        /// ⚠️ **不能借 `CardDetailPopup.LastOpened`** —— 那个字段只在 `RebuildKeepingState()` 里赋值，
        /// `Create()`/`Open()` 都不设它（2026-10-03 自检在这里 NRE 过，已记）。</summary>
        public static CardDetailPopup LastWarlordDetail;

        /// <summary>`Switch Deck Info`：两个抽屉互斥切换（原版两个抽屉**同矩形叠着**、出厂只有 `Deck List` 开）。</summary>
        public void SwitchDrawer()
        {
            InfoDrawerShown = !InfoDrawerShown;
            if (_listDrawer != null) _listDrawer.gameObject.SetActive(!InfoDrawerShown);
            if (_infoDrawer != null) _infoDrawer.gameObject.SetActive(InfoDrawerShown);
            Debug.Log("[DeckInfo] 切到 " + (InfoDrawerShown ? "`Deck Info`（费用曲线 / 卡背）" : "`Deck List`")
                      + " 那一面（原版 `Switch Deck Info Button` 的语义）");
        }

        /// <summary>`Share` / `Share On Chat`：**原版走服务端 / 平台分享**，我们没有那两条。
        /// 能拿出来的、真正可分享的东西 = **卡组串**（`DeckLibrary.ExportString`，与卡组编辑那颗 `Share` 同一份）
        /// ⇒ 弹出来给用户看/抄（批处理与桌面都没法替用户按剪贴板），并**如实说明**聊天那条发不出去。</summary>
        void ShareDeck(string key)
        {
            var deck = CollectionData.Raw(DeckIndex);
            string s = deck != null ? RuleEngine.DeckLibrary.ExportString(deck) : "";
            if (string.IsNullOrEmpty(s))
            {
                Debug.LogWarning("[DeckInfo] `" + key + "`：卡组串导不出来（`ExportString` 返回空）⇒ 没东西可分享");
                return;
            }
            Debug.Log("[DeckInfo] `" + key + "` ⇒ 卡组串（" + s.Length + " 字符）：" + s);
            if (Manager != null)
                Manager.ShowPopUp(key == "Share On Chat"
                    ? "聊天窗**发不出消息**（原版走服务端，我们这条线没有网络）。\n\n这是这一副的卡组串，可以自己复制：\n" + s
                    : "原版是**平台分享**。\n\n这是这一副的卡组串，可以自己复制：\n" + s,
                    "知道了", null);
        }

        /// <summary>红线：**不许静默失败** —— 没做的必须说出来。</summary>
        void NotBuilt(string what)
        {
            Debug.LogWarning("[DeckInfo] `" + what + "` 还没实现（**出声**，见 `Shell/DeckInfoPopup.cs` 文件头「没建的」）");
        }

        // ============================================================ 画图小工具（照 PromptPopup 那套）
        // ⚠️ 每个都**同时收「画在谁下面」和「坐标以谁为基准」两个参数** ——
        // 两个写成一个（用 parent 当基准却画在别处）会让子件**整块偏走**，而矩形断言量不到。

        enum Align { Center, Left, Right }

        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
        {
            return LayoutSpace.RectCenter(x1, y1, x2, y2) - basis.position;
        }

        void Solid(Transform parent, Transform basis, float cx, float cy, float w, float h, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(parent, CardArt.Solid(),
                                        Local3(basis, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect(w / h);
            quad.SetTint(color);
            quad.SetRenderQueue(q);
        }

        ImageQuad Img(Transform parent, Transform basis, Texture2D tex, float x1, float y1, float x2, float y2,
                      string name, int q, bool keepAspect)
        {
            if (tex == null) { Debug.LogWarning("[DeckInfo] 图取不到，这一层不画：" + name); return null; }
            float w = x2 - x1, h = y2 - y1;
            if (keepAspect && tex.height > 0)
            {
                float sa = (float)tex.width / tex.height, ra = w / Mathf.Max(1e-6f, h);
                if (sa > ra) { float nh = w / sa, d = (h - nh) * 0.5f; y1 += d; y2 -= d; h = nh; }
                else { float nw = h * sa, d = (w - nw) * 0.5f; x1 += d; x2 -= d; w = nw; }
            }
            var quad = ImageQuad.Create(parent, tex, Local3(basis, x1, y1, x2, y2),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / Mathf.Max(1e-6f, h));
            quad.SetRenderQueue(q);
            return quad;
        }

        GameObject Nine(Transform parent, Transform basis, string art, Vector4 border, float texW, float texH,
                        float x1, float y1, float x2, float y2, int q, string name, Color? tint = null)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[DeckInfo] 九宫格图取不到：" + art); return null; }
            var g = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH, Local3(basis, x1, y1, x2, y2),
                                              LayoutSpace.Px(x2 - x1), LayoutSpace.Px(y2 - y1), name);
            if (g != null)
                foreach (var c in g.GetComponentsInChildren<ImageQuad>())
                {
                    c.SetRenderQueue(q);
                    if (tint.HasValue) c.SetTint(tint.Value);
                }
            return g;
        }

        Label Txt(Transform parent, Transform basis, string text, float x1, float y1, float x2, float y2,
                  float fontPx, Align align, string name, int q)
        {
            var lb = Label.Create(parent, text ?? "", Local3(basis, x1, y1, x2, y2), 5, Color.white,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            // 原版这几条 TMP 的对齐：`Deck Name`/`Warlord Name` 居中、行里的卡名左、数量右
            if (align == Align.Right) lb.AlignRightOn(LayoutSpace.FromPixel(x2, 0f).x);
            else if (align == Align.Left) lb.AlignLeftOn(LayoutSpace.FromPixel(x1, 0f).x);
            return lb;
        }

        Transform Hit(Transform parent, Transform basis, string name, PxRect r, System.Action onClick)
        {
            var hit = new GameObject(name);
            hit.transform.SetParent(parent, false);
            // 🔴 **节点本身也要摆**（2026-09-23 踩）：原来只摆了里面那个 quad ⇒ **节点全停在容器的 (0,0)**
            //    （= 容器中心）。自检按名字取节点量位置时，三个按钮**量出来是同一个 x**
            //    （1247.95 = 整行的中心），而 quad 画得是对的 ⇒ **量节点与量渲染是两回事**。
            //    凡「按名字取节点」的地方（自检 / 命中的 `GetComponentInChildren`）都要求节点摆对。
            hit.transform.localPosition = Local3(basis, r.x1, r.y1, r.x2, r.y2);
            var q = ImageQuad.Create(hit.transform, CardArt.Solid(), Vector3.zero,
                                     LayoutSpace.Px(r.H), new Vector2(0.5f, 0.5f), "Hit");
            if (q != null)
            {
                q.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                q.SetTint(new Color(0f, 0f, 0f, 0f));
                q.SetRenderQueue(QDIHit);
            }
            var wb = hit.AddComponent<WindowButton>();
            wb.onClick = onClick;
            return hit.transform;
        }
    }
}
