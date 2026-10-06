// AllianceEventScorePanel.cs — A103：原版 prefab **`Alliance Event Score Panel`**（根组件类 `AlliancesEventScorePanel`）
//
// ============================ 出处（判据一律现读）============================
//   python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Event Score Panel" \
//          --depth 14 --relative --md          # **38 个节点**（根 + 37）
//   · 根组件字段（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AlliancesEventScorePanel.cs`，字段偏移逐条核过）：
//       `+0x20 scoreBar` · `+0x28 leaderboardButton` · `+0x30 leaderboardButtonNotInAlliance` ·
//       `+0x38 allianceNameText` · `+0x40 inAllianceGO` · `+0x48 notInAllianceGO` ·
//       `+0x50 joindAllianceButton` · `+0x58 leaderboardPopup`(ComponentReference<GameWindow>) ·
//       `+0x60 leaderboardToOpen`(string)
//   · 方法体：`AlliancesEventScorePanel__Initialize.c` · `…__JoinAllianceButtonClick.c` ·
//             `…__LeaderboardButtonClick.c`   —— 两态就是那两支 `SetActive`：
//       `AlliancesManager.IsInGroup == false` ⇒ `inAllianceGO.SetActive(false)` + `notInAllianceGO.SetActive(true)`；
//       反之 ⇒ `inAllianceGO` 开、`notInAllianceGO` 关，再 `allianceNameText.SetText(...)` + `scoreBar.Initialize(...)`。
//
// ⚠️ **这是「面板」不是窗**：根组件是 `MonoBehaviour`（`AlliancesEventScorePanel`）、**不是 `GameWindow`**
//    ⇒ **没有 `type` / `placement` / `closeOnESC` 可填、也**⛔**不注册进 `WindowsManager`**（如实记，
//    别为了「凑一条注册」给它编一个窗身份）。它的父件是 **`Alliance Detail View`**（现读 prefab 父链）。
//
// ============================ 🔴 6 处「自己 active、祖先 inactive」============================
//   现读 `No Alliance` 自己 **act = F**，而它下面 6 颗**自己 act = T**：
//     `Background` · `Join Alliance text` · `Join Alliances Button`(+它的 `Button Text`) ·
//     `Leaderboard Button`(+它的 `Button Text`)
//   uGUI 与渲染判的都是 **`activeInHierarchy`（整条父链）** ⇒ **原版出厂根本不画这 6 件**
//   （判据：`LayoutGroup.cs:60-79` 只收 `activeInHierarchy` 的子件）。
//   ⇒ 本件的做法 = **照 prefab 的兄弟序把那 6 颗建在 `No Alliance` 底下**、`No Alliance` 自己关着，
//     ⛔ **不照 `activeSelf` 把它们提到根上**（提到根上 = 出厂就画 6 件原版不画的东西），
//     ⛔ **也不逐颗 `SetActive(false)`**（那会把「自己 act = T」这条字段改掉、与 prefab 不一致）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 prefab `Alliance Event Score Panel`（`AlliancesEventScorePanel`）：联盟活动记分面板。</summary>
    public class AllianceEventScorePanel : MonoBehaviour
    {
        // 分层（渲染队列，⛔ 不是 z）—— 本件占 **3960–3968**
        //  ⚠️ 与 `EnergySinglePlayerOnlyEventWindow`（3935–3945）· `AllianceEventScoreInfo`（3975–3981）不重叠。
        public const int QBg = 3960;     // `No Alliance/Background`
        public const int QArt = 3961;    // 按钮底 / 进度条底
        public const int QArt1 = 3962;   // 压在底上的那一层
        public const int QText = 3963;
        public const int QHit = 3968;

        /// <summary>🔴 **2026-10-14（A812）**：「联盟名」那一格的**出厂占位串**（原版 prefab 的原文）。
        /// **两处共用这一份**（⛔ 别在别处再写一遍字面量 —— CLAUDE.md §三：两处写同一条规则 = 迟早不一致）：
        /// ① `Build()` 建那颗 TMP 时的初值 ② `SetAllianceName("")` 的**回写值**（见那个方法头）。</summary>
        public const string PlaceholderName = "Alliance Name";

        // ============================================================ 几何（现读 `--relative`，根在 0,0）
        const float RootW = 383.59f, RootH = 511.52f;
        static readonly PxRect InAllianceR = new PxRect(0f, -9.56f, 383.59f, 501.96f);   // act = T
        static readonly PxRect NameR       = new PxRect(8.28f, -9.56f, 375.31f, 38.23f);
        static readonly PxRect ViewLbR     = new PxRect(74.71f, 58.97f, 308.88f, 123.62f);
        static readonly PxRect ViewLbTxR   = new PxRect(85.89f, 63.80f, 296.94f, 118.78f);
        static readonly PxRect ScoreInfoR  = new PxRect(54.84f, 144.36f, 328.75f, 502.25f);  // 嵌套子 prefab
        static readonly PxRect NoAllianceR = new PxRect(8.47f, 11.30f, 375.11f, 500.22f);   // act = **F**
        static readonly PxRect NoBgR       = new PxRect(25.51f, 11.30f, 358.08f, 500.22f);
        static readonly PxRect JoinTxR     = new PxRect(8.47f, 11.30f, 375.50f, 242.05f);
        static readonly PxRect JoinBtnR    = new PxRect(74.71f, 252.34f, 308.88f, 316.99f);
        static readonly PxRect JoinBtnTxR  = new PxRect(85.89f, 257.16f, 296.94f, 312.15f);
        static readonly PxRect NoLbR       = new PxRect(74.90f, 324.02f, 309.08f, 388.67f);
        static readonly PxRect NoLbTxR     = new PxRect(86.09f, 328.85f, 297.14f, 383.83f);

        const string ArtMulligan = "UI_Button_Mulligan";                          // 410×124 · 九宫 (333,96,333,96)
        const string ArtNoBg     = "UI_Deck_Information_submenu_Back_opaque";     // 69×63 · 九宫 (18,18,18,18)
        static readonly Vector4 MullBorder = new Vector4(333f, 96f, 333f, 96f);
        static readonly Vector4 NoBgBorder = new Vector4(18f, 18f, 18f, 18f);
        static readonly Color NoBgTint = new Color(0f, 0f, 0f, 0.204f);           // 实读 (0,0,0,0.204)

        // ============================================================ 建出来的东西
        readonly List<string> _missArt = new List<string>();
        Transform _root, _inAlliance, _noAlliance, _name, _viewLb, _joinBtn, _noLb;
        AllianceEventScoreInfo _scoreInfo;
        bool _inAllianceState = true;      // = prefab 出厂那一档（`In Alliance` act T / `No Alliance` act F）

        public GameObject InAllianceGo { get { return _inAlliance != null ? _inAlliance.gameObject : null; } }
        public GameObject NoAllianceGo { get { return _noAlliance != null ? _noAlliance.gameObject : null; } }
        public Transform Root { get { return _root; } }
        public Transform AllianceName { get { return _name; } }
        public Transform ViewLeaderboardButton { get { return _viewLb; } }
        public Transform JoinAlliancesButton { get { return _joinBtn; } }
        public Transform NoAllianceLeaderboardButton { get { return _noLb; } }
        /// <summary>嵌在 `In Alliance` 里那一棵 `Alliance Event Score Info`（原版是**嵌套子 prefab**）。</summary>
        public AllianceEventScoreInfo ScoreInfo { get { return _scoreInfo; } }
        /// <summary>当前这一态（`true` = 已入盟那一支）。</summary>
        public bool IsInAlliance { get { return _inAllianceState; } }
        public List<string> MissingArt { get { return _missArt; } }

        // ============================================================ 建
        /// <summary>在 <paramref name="parent"/> 下建出这一棵，**根框原点** = (<paramref name="x1"/>, <paramref name="y1"/>)。
        /// ⚠️ 根框尺寸恒为原版的 **383.59 × 511.52**（所有子件坐标都是按它算的）。</summary>
        public static AllianceEventScorePanel Create(Transform parent, float x1, float y1,
                                                     string name = "Alliance Event Score Panel")
        {
            var go = new GameObject(name);
            var c = go.AddComponent<AllianceEventScorePanel>();
            c._root = go.transform;
            go.transform.SetParent(parent, false);
            // 根自己也带矩形（原版根框 **383.59 × 511.52**；子件坐标都是「面板帧 + (x1,y1)」的绝对 px）
            MenuDraw.ApplyPxRect(go.transform, parent, new PxRect(x1, y1, x1 + RootW, y1 + RootH));
            c.Build(x1, y1);
            return c;
        }

        void Build(float x1, float y1)
        {
            _missArt.Clear();

            // `In Alliance`（VLG · 出厂 act = T）
            _inAlliance = MenuDraw.Node(_root, "In Alliance", Off(InAllianceR, x1, y1));
            //   ① `Alliance Name`（TMP fs 40.85 · 基准 36 · auto[18~72] · Center/Midline · **折行=0**）
            var nmR = Off(NameR, x1, y1);
            var nm = MenuDraw.Text(_inAlliance, nmR, PlaceholderName, Color.white, "Alliance Name",
                                   40.85f, QText);
            // 折行=0 ⇒ 不走 `TextBox` 那条（`SetWrapWidth`）；自适应用的框仍然要给（原版 `m_enableAutoSizing`）。
            if (nm != null) nm.SetAutoFitBox(LayoutSpace.Px(nmR.W), LayoutSpace.Px(nmR.H), 18f, 72f, 36f);
            _name = nm != null ? nm.transform : null;

            //   ② `View Leaderboard Button`（`UI_Button_Mulligan` 九宫 · `EverguildButton` trans=2 ColorTint）
            var vr = Off(ViewLbR, x1, y1);
            _viewLb = MenuDraw.Node(_inAlliance, "View Leaderboard Button", vr);
            var vbg = MenuDraw.Rect(_viewLb, Art(ArtMulligan), vr, "Bg", QArt);
            var vtx = Off(ViewLbTxR, x1, y1);
            MenuDraw.Text(_viewLb, vtx, "Leaderboard", Color.white, "Button Text", 40f, QText);
            MenuDraw.Hit(_viewLb, "ButtonHit", vr, QHit, OnLeaderboardClick, vbg, ArtMulligan,
                         ArtMulligan + "_hover", ArtMulligan + "_Pressed");

            //   ③ `Alliance Event Score Info`（**嵌套子 prefab**，框与本件 standalone 那一份逐位相同，
            //      只是原点偏到 (54.84,144.36) —— 现读两处的子件坐标差**恰好**是这个偏移）
            _scoreInfo = AllianceEventScoreInfo.Create(_inAlliance, Off(ScoreInfoR, x1, y1));

            // `No Alliance`（出厂 **act = F**）—— 下面 6 颗**照 prefab 建在它底下**，⛔ 别提到根上
            _noAlliance = MenuDraw.Node(_root, "No Alliance", Off(NoAllianceR, x1, y1));
            MenuDraw.Nine(_noAlliance, Art(ArtNoBg), Off(NoBgR, x1, y1), NoBgBorder, 69f, 63f, QBg,
                          NoBgTint, true, "Background");
            var jtR = Off(JoinTxR, x1, y1);
            var jt = MenuDraw.TextBox(_noAlliance, jtR, "Join an alliance to gain additional rewards",
                                      Color.white, "Join Alliance text", 41.4f, 18f, QText, 41.4f, 36f);
            if (jt != null) { /* hAlign = Center/Middle ⇒ 不调 Align* */ }
            var jbR = Off(JoinBtnR, x1, y1);
            _joinBtn = MenuDraw.Node(_noAlliance, "Join Alliances Button", jbR);
            var jbg = MenuDraw.Rect(_joinBtn, Art(ArtMulligan), jbR, "Bg", QArt);
            MenuDraw.Text(_joinBtn, Off(JoinBtnTxR, x1, y1), "Search", Color.white, "Button Text", 36f, QText);
            MenuDraw.Hit(_joinBtn, "ButtonHit", jbR, QHit, OnJoinAllianceClick, jbg, ArtMulligan,
                         ArtMulligan + "_hover", ArtMulligan + "_Pressed");
            var lbR = Off(NoLbR, x1, y1);
            _noLb = MenuDraw.Node(_noAlliance, "Leaderboard Button", lbR);
            var lbg = MenuDraw.Rect(_noLb, Art(ArtMulligan), lbR, "Bg", QArt);
            MenuDraw.Text(_noLb, Off(NoLbTxR, x1, y1), "Leaderboard", Color.white, "Button Text", 36f, QText);
            MenuDraw.Hit(_noLb, "ButtonHit", lbR, QHit, OnLeaderboardClick, lbg, ArtMulligan,
                         ArtMulligan + "_hover", ArtMulligan + "_Pressed");

            // 出厂那一档 = `In Alliance` 开 / `No Alliance` 关（= prefab 实测值）
            SetInAlliance(true);
        }

        // ============================================================ 状态 → 参数（两态）
        /// <summary>两态开关 —— **原版那两条 `SetActive` 的等价物**
        /// （判据：`AlliancesEventScorePanel__Initialize.c`，条件来自 `AlliancesManager.IsInGroup`）。
        /// <para>`true` ⇒ `In Alliance` 开 / `No Alliance` 关（并给记分条喂名字与分数）；
        /// `false` ⇒ 反过来。⚠️ 原版 `IsInGroup` 要服务器；本仓没有 ⇒ 这是**显式状态口**
        /// （出厂 = `true`，照 prefab 实测值），⛔ 不假装我们从服务器读到了它。</para></summary>
        public void SetInAlliance(bool inAlliance)
        {
            _inAllianceState = inAlliance;
            if (_inAlliance != null) _inAlliance.gameObject.SetActive(inAlliance);
            if (_noAlliance != null) _noAlliance.gameObject.SetActive(!inAlliance);
        }

        /// <summary>联盟名那一格（原版 `allianceNameText`；`Initialize(string allianceName, …)` 里灌）。
        /// 出厂态 = **prefab 原文 `PlaceholderName`**（本地没有联盟名 ⇒ ⛔ 不编一个名字）。
        /// 🔴 **2026-10-14（A812）用户拍板**：**空串 ⇒ 回写占位串**（不是「什么都不做」）——
        /// 这条以前是**三方打架**：实现「不动」· 日志说「保持出厂原文」（= 与实现不符）· 断言
        /// （`Editor/MainMenuScene.cs`）说「回到出厂原文」。⚖️ **裁的是「回写」那一边**：它同时满足
        /// 日志文案与断言，且不需要给「空串」编一个新语义。⚠️ 原版语义**本地读不到**
        /// （`Initialize(string,…)` 直接灌串；空串怎么处置只有**远端 group service** 有真值）
        /// ⇒ **这是我们的口径、不是原版判据**。</summary>
        public void SetAllianceName(string name)
        {
            var lb = _name != null ? _name.GetComponent<Label>() : null;
            if (lb == null) return;
            if (string.IsNullOrEmpty(name))
            {
                Debug.Log("[Alliance] `SetAllianceName` 收到空串 ⇒ **回写 prefab 出厂原文 `'" + PlaceholderName
                        + "'`**（⛔ 不编一个名字）—— A812 的口径，见本方法头。");
                lb.SetText(PlaceholderName);
                return;
            }
            lb.SetText(name);
        }

        /// <summary>把分数与阈值转给嵌在里面的那条记分条（原版 `Initialize(…)` 的尾段就是
        /// `scoreBar.Initialize(currentScore, scoringThresholds)`）。</summary>
        public void SetScore(int currentScore, int[] thresholds)
        {
            if (_scoreInfo != null) _scoreInfo.SetMilestones(currentScore, thresholds);
        }

        // ============================================================ 两颗钮点什么
        // 原版：`JoinAllianceButtonClick()` 打开「找联盟」那一页；`LeaderboardButtonClick()` 按
        // `leaderboardToOpen` 打开 `leaderboardPopup`（一个 `GameWindow` 引用）。
        // 🔴 **两条都要服务器**（联盟与联盟榜全在远端的 group service）⇒ 照本工程边界③：
        //   **入口照做、点了如实说「暂无服务器」**，⛔ 不静默、也不造假数据。
        void OnJoinAllianceClick()
        {
            Debug.Log("[Alliance] `Join Alliances Button` 点了 ⇒ **如实提示「暂无服务器」**"
                    + "（原版 `JoinAllianceButtonClick()` 打开找联盟那一页；本地没有 group service）。");
            Offline("Alliances are not available offline.");
        }

        void OnLeaderboardClick()
        {
            Debug.Log("[Alliance] `Leaderboard Button` 点了 ⇒ **如实提示「暂无服务器」**"
                    + "（原版 `LeaderboardButtonClick()` 按 `leaderboardToOpen` 开 `leaderboardPopup`；"
                    + "本地没有联盟榜数据）。");
            Offline("The alliance leaderboard is not available offline.");
        }

        static void Offline(string msg)
        {
            if (WindowsManager.Instance != null)
                WindowsManager.Instance.ShowPopUp(msg, "OK", null);
        }

        // ============================================================ 助手
        static PxRect Off(PxRect r, float x1, float y1)
            => new PxRect(r.x1 + x1, r.y1 + y1, r.x2 + x1, r.y2 + y1);

        /// <summary>取图 + 记账（`MenuDraw.Rect/Nine` 取不到图会**静默 return null** ⇒ 整层消失）。</summary>
        public Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !_missArt.Contains(name)) _missArt.Add(name);
            return t;
        }
    }
}
