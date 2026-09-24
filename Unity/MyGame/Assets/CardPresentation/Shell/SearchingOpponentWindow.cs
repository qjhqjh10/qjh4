// SearchingOpponentWindow.cs — 阶段二第 3 层第 6 件：**`SearchingOpponentWindow`（找对手那扇全屏窗）**
//
// ============================ 出处（唯一正本）============================
// `资料/阶段二_战斗入口_原版规格.md` **§一（窗口参数）+ §二 D（逐节点表）**；
// 2026-09-24 建它时**又从根实读了一遍**（`工具/menu_rect.py` / `menu_dump.py`）——
// 补上正本表里没有的：`Found Player Container` 的 1138.41² 矩形与 `Player Name` 的字号/位置、
// 敌方那一格的 **`scl=(-1,1)` 镜像**、`Cancel Match` / `Button Text` 的矩形。
//
// 🔴 **窗口参数**：`type=0 **Fullscreen**` · `windowsPlacement=0 **None**` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`
//    —— **四个战斗入口窗里只有它是 Fullscreen + 无锚点**（另三扇都是 `1 Popup`；§一 的表）。
//
// ============================ 空态就是这个窗的常态 ============================
// 原版 `UISearchingOpponentPlayerInfo.Initialize(data, name)`（反编译）：`data == null` 时
//   `notFoundPlayerContainer.SetActive(true)` + `foundPlayerContainer.SetActive(false)` ——
// **找不到人 = 空态**。我们本地**没有对手**（也没有服务器）⇒ **对手那一格永远走空态**，这是如实画。
// ⚠️ 玩家那一格：原版也是拿服务端下发的玩家档案去填（`DeckAndWarlordData` + 玩家名）。
//    我们**没有玩家档案** ⇒ 两样都按原版 prefab 里的**自有值**来：立绘用**当前选中卡组的督军**
//    （那是真数据），名字用 prefab 里那行字 `Player name`（**不编一个名字出来**）。
//
// ---- 本地入口（**我们定的**）----
// 原版谁开这扇窗**本地查不到**（全量反编译里 `SearchingOpponentWindow` 没有开它的调用点，
// 与那四扇窗的入口同一个原因：LiveOps/匹配管理器在服务端配置里）。⇒ 按 §五 那条口径：
// **入口由我们定并如实标出来** —— 挂在**排位窗**的 `Battle!` 上（排位本来就是「等真人」那个模式），
// 其余三扇窗继续用窗口内的 `Searching Oponent Popup`。**这一条映射不是复刻。**
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`SearchingOpponentWindow` —— 全屏「正在找对手」。</summary>
    public class SearchingOpponentWindow : GameWindow
    {
        // ⚠️ `Cancel Match` 的队列**要比立绘高一档**（两者矩形有重叠：785–885 与 1035–1135 × 942–1018）
        public const int QSr = 3120, QSrArt = 3121, QSrArt1 = 3122, QSrText = 3123, QSrHit = 3124;

        // ---- 几何（§二 D + 2026-09-24 实读）----
        public const float BgL = -964.67f, BgT = -541.74f, BgR = 2884.67f, BgB = 1621.74f;
        public const float TitleL = 741.12f, TitleT = 136.00f, TitleR = 1178.88f, TitleB = 186.00f;
        public const float PlyL = -0.01f, PlyT = 1.00f, PlyR = 631.45f, PlyB = 1079.00f;
        public const float PlyFoundL = -253.49f, PlyFoundT = -29.21f, PlyFoundR = 884.93f, PlyFoundB = 1109.21f;
        public const float PlyNameL = 53.84f, PlyNameT = 853.00f, PlyNameR = 491.60f, PlyNameB = 903.00f;
        public const float PlyNotFoundL = 265.72f, PlyNotFoundT = 490.00f, PlyNotFoundR = 365.72f, PlyNotFoundB = 590.00f;
        public const float EnL = 1288.55f, EnT = 1.00f, EnR = 1920.00f, EnB = 1079.00f;
        public const float EnFoundL = 1035.07f, EnFoundT = -29.21f, EnFoundR = 2173.48f, EnFoundB = 1109.21f;
        public const float EnNameL = 1342.40f, EnNameT = 853.00f, EnNameR = 1780.16f, EnNameB = 903.00f;
        public const float EnNotFoundL = 1554.28f, EnNotFoundT = 490.00f, EnNotFoundR = 1654.28f, EnNotFoundB = 590.00f;
        public const float CxlL = 785.00f, CxlT = 942.00f, CxlR = 1135.00f, CxlB = 1018.00f;
        public const float CxlTxL = 798.00f, CxlTxT = 941.39f, CxlTxR = 1122.00f, CxlTxB = 1017.39f;
        public const string ArtButton = "40K_button";
        public static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>玩家/敌人名那行字 —— **原版 prefab 里写的就是 `Player name`**（运行时才被真名覆盖）。</summary>
        public const string PlaceholderName = "Player name";

        /// <summary>点 `Cancel` 的回调（排位窗用它取消匹配）。</summary>
        public System.Action OnCancel;

        internal int DeckIndex;

        public static SearchingOpponentWindow Create(WindowsManager mgr, int deckIndex)
        {
            var go = new GameObject("SearchingOpponentWindow");
            var win = go.AddComponent<SearchingOpponentWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.None;            // 实证 windowsPlacement=0（**四窗里只有它这样**）
            win.closeOnEsc = true;                            // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            win.DeckIndex = deckIndex;
            WindowsManager.AttachToAnchor(win);               // 无锚点 ⇒ 原地不动（照原版）
            return win;
        }

        /// <summary>最近一次开出来的那扇（自检用）。</summary>
        public static SearchingOpponentWindow LastOpened;

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);

            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(BgL, BgT, BgR, BgB),
                          "Background", QSr, new Color(0f, 0f, 0f, 0.71f));
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(root, new PxRect(TitleL, TitleT, TitleR, TitleB), "Searching opponent",
                          Color.white, "Title", 36f, QSrText);

            // 玩家那一格：**有数据** ⇒ 走 `Found`（立绘 = 当前选中卡组的督军）
            BuildSide(root, "Searching Opponent Player Info Container",
                      new PxRect(PlyL, PlyT, PlyR, PlyB),
                      new PxRect(PlyFoundL, PlyFoundT, PlyFoundR, PlyFoundB),
                      new PxRect(PlyNameL, PlyNameT, PlyNameR, PlyNameB),
                      new PxRect(PlyNotFoundL, PlyNotFoundT, PlyNotFoundR, PlyNotFoundB),
                      true, false);
            // 对手那一格：**没有对手** ⇒ 走 `Not Found`（照原版 `Initialize(null, …)` 那一支）
            BuildSide(root, "Searching Opponent Enemy Info Container",
                      new PxRect(EnL, EnT, EnR, EnB),
                      new PxRect(EnFoundL, EnFoundT, EnFoundR, EnFoundB),
                      new PxRect(EnNameL, EnNameT, EnNameR, EnNameB),
                      new PxRect(EnNotFoundL, EnNotFoundT, EnNotFoundR, EnNotFoundB),
                      false, true);
            Debug.Log("[SearchingOpp] 对手那一格画的是**空态**（原版 `UISearchingOpponentPlayerInfo.Initialize(null,…)`"
                      + " = 显示 `Not Found Container`、关掉 `Found Player Container`）—— 本地没有对手，如实画");

            // `Cancel Match`：原版是 **`type=Simple` + `preserveAspect`**（**不是 Sliced**！）
            //   —— 正本 §二 D 注⑥ 原来写「照原版 type=Sliced 会退化」是**错的**，已就地更正。
            MenuDraw.Rect(root, CardArt.MenuUi(ArtButton), new PxRect(CxlL, CxlT, CxlR, CxlB),
                          "Cancel Match", QSrArt1, new Color(0.369f, 0.894f, 0.587f, 1f), true);
            // 原版 Button Text hAlign = **Center** ⇒ 不调 `Align*`
            MenuDraw.Text(root, new PxRect(CxlTxL, CxlTxT, CxlTxR, CxlTxB), "Cancel", Color.white,
                          "Button Text", 38f, QSrText);
            MenuDraw.Hit(root, "CancelHit", new PxRect(CxlL, CxlT, CxlR, CxlB), QSrHit, Cancel);
        }

        /// <summary>一格玩家信息：`Found`（立绘 + 名字）与 `Not Found`（100² 空框）**二选一**。
        /// ⚠️ 敌方那格的立绘原版挂 `m_LocalScale = (-1, 1)`（**左右镜像**，两人面对面）——
        ///    我们的 `ImageQuad` 没有翻转开关 ⇒ **这一格没镜像**，如实记着（出声）。</summary>
        void BuildSide(Transform root, string name, PxRect box, PxRect found, PxRect nameR, PxRect notFound,
                       bool hasData, bool mirrored)
        {
            var side = MenuDraw.Node(root, name, box);
            var f = MenuDraw.Node(side, "Found Player Container", found);
            var wl = CollectionData.Warlord(DeckIndex);
            // ⚠️ 用 `Portrait(wl.Id)` 而**不是** `PortraitByName`（后者按卡名取、同名卡会取错，
            //    `CardArt` 的注释里明确劝新代码别用它；这里 id 就在手上）
            var tex = hasData && wl != null ? CardArt.Portrait(wl.Id) : null;
            if (hasData && tex != null)
                MenuDraw.Rect(f, tex, found, "Warlord Image", QSrArt, null, true);
            else if (hasData)
                Debug.Log("[SearchingOpp] 督军立绘取不到（" + (wl != null ? wl.Name : "这套卡组没有督军") + "）—— 那一层不画，出声");
            // 名字：**用原版 prefab 自带的那行字**（运行时才被真名覆盖）—— 不编一个名字出来
            MenuDraw.Text(f, nameR, PlaceholderName, Color.white, "Player Name", 36f, QSrText);
            if (mirrored)
                Debug.Log("[SearchingOpp] 敌方那格的立绘**没镜像**（原版 `m_LocalScale = (-1,1)`；"
                          + "`ImageQuad` 没有翻转开关）—— 出声（只对 `Found` 那一支有意义）");

            // `Not Found Container`：原版**没有 Image**（只有 RectTransform，100×100）⇒ 照原版**空着**
            var nf = MenuDraw.Node(side, "Not Found Container", notFound);
            f.gameObject.SetActive(hasData);
            nf.gameObject.SetActive(!hasData);
        }

        /// <summary>点 `Cancel`（原版 `CancelMatchMatchmaking → MatchMakerManager.CancelSearch`）。</summary>
        public void Cancel()
        {
            Debug.Log("[SearchingOpp] `Cancel Match` —— 取消匹配（原版 `CancelMatchMatchmaking`）");
            if (OnCancel != null) OnCancel();
            Close();
        }
    }
}
