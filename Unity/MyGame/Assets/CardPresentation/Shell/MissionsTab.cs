// MissionsTab.cs — 「日常」任务页（`Missions Tab`）· 三种任务卡 + 头部 + 周常
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §三。**每一个矩形的锚点五元组都是原版 JSON 原文**，
// 由 `工具/menu_rect.py <pid> --cs` 机械吐出来、直接贴进来的（不手抄 —— 手抄锚点五元组出过一次静默的版面 bug）。
// 卡内版面按 `UguiRect.Child` 逐层算，**不是写死坐标**：所以卡被布局撑宽/撑窄时，内部会照原版规则重分布。
//
// 🔴 **四条纪律**：
//   ① **出厂 `activeSelf=false` 的件不建**（`title` / `ray target` / `body.description`…）
//      —— 原版是运行时按状态开的，见每行的 `// 出厂 inactive`。
//      ⚠️ **2026-10-04 更正（Y2 实读，铁律 5）**：这一行原来还把「**各 `debug_buttons`**」列在里面 ——
//      真包实读该节点的 **`m_IsActive = True`**（每日行那份 MB `-2794962970128279344`，同名 **9 个实例全是 True**），
//      `资料/日常_原版规格.md:222` 那张表最后一列也写 **T** ⇒ **两份说法打架**。
//      我们**仍然不建它**（它是调试件），但 **理由不是「出厂 inactive」** ⇒ 见 `项目任务.md` §三 **A75-④**（**未定**）。
//   ② **`Special Missions` 带 `localScale=1.15`** ⇒ 它子树的位置与尺寸都要**绕它的 pivot 缩放**；
//      不算这一步，两张特殊卡会比原版小一圈、还会偏左（正本 §三·1）。见 `ScaleAbout`。
//   ③ **被布局组排的子节点，矩形是「布局跑之前的模板位」**（四键、三行、里程碑 steps、`Daily Skulls`）
//      ⇒ 一律用 `UguiLayout` 按原版的 padTop / spacing / align 算，**别抄 JSON 的 pos**。
//   ④ **里程碑的图不在 Image 上、在脚本字段里**（`activeSprite`/`disabledSprite`，正本 §三·7）
//      ⇒ 照字段画，别照 Image 的 `sprite` 画（那个是空的）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>任务页。原版 `MissionsTab : WindowTabBase<MainMenuRewardsWindow>`。</summary>
    public class MissionsTab : WindowTabBase
    {
        public override WindowTabType Type { get { return WindowTabType.Missions; } }

        RewardsWindow _win;
        Transform _root;

        // ---- 出处：正本 §三（机械走链 `工具/menu_rect.py`；父链 Rewards 根 → Content Area → Tabs）----
        static readonly PxRect RootRect = new PxRect(RewardsWindow.ContentL, RewardsWindow.ContentT,
                                                     RewardsWindow.ContentR, RewardsWindow.ContentB);
        // `Missions Tab`      N(0,"Missions Tab", 0,0, 1,1, .5,.5, -0.244019,0.869873, 0.477783,1.74103)
        static readonly Vector2 MT_A0 = UguiRect.A00, MT_A1 = UguiRect.A11, MT_P = UguiRect.P50c,
                                MT_Pos = new Vector2(-0.244019f, 0.869873f), MT_Sz = new Vector2(0.477783f, 1.74103f);
        // `Normal Missions`   N(1, 0,1, 0,1, 0,0.5, 205.68,-388.55, 1519,723.8)
        static readonly Vector2 NM_A0 = UguiRect.A01, NM_A1 = UguiRect.A01, NM_P = new Vector2(0f, 0.5f),
                                NM_Pos = new Vector2(205.68f, -388.55f), NM_Sz = new Vector2(1519f, 723.8f);
        // `Special Missions`  N(2, 0,1, 0,1, 0,1, 0,0, 779.21,556.223)   scl 1.15
        static readonly Vector2 SM_A0 = UguiRect.A01, SM_A1 = UguiRect.A01, SM_P = UguiRect.P01,
                                SM_Pos = Vector2.zero, SM_Sz = new Vector2(779.21f, 556.223f);
        const float SM_Scale = 1.15f;
        // `Daily Login Container`  N(3, 0,1, 0,1, 0.5,0.5, 167,-282.5, 334,565)
        static readonly Vector2 DL_A0 = UguiRect.A01, DL_A1 = UguiRect.A01, DL_P = UguiRect.P50c,
                                DL_Pos = new Vector2(167f, -282.5f), DL_Sz = new Vector2(334f, 565f);
        // `Daily Missions`    N(2, 0,1, 0,1, 0,1, 898.933,0, 539.188,555.87)   scl 1.15
        static readonly Vector2 DM_A0 = UguiRect.A01, DM_A1 = UguiRect.A01, DM_P = UguiRect.P01,
                                DM_Pos = new Vector2(898.933f, 0f), DM_Sz = new Vector2(539.188f, 555.87f);
        const float DM_Scale = 1.15f;
        // `Daily Missions Holder`  N(3, 0,0, 1,0.5, 0.5,0.5, 0,111.75, 0.0001,223.5)   VLG sp 18.55 align 7
        static readonly Vector2 DH_A0 = UguiRect.A00, DH_A1 = new Vector2(1f, 0.5f), DH_P = UguiRect.P50c,
                                DH_Pos = new Vector2(0f, 111.75f), DH_Sz = new Vector2(0.0001f, 223.5f);
        const float DH_Spacing = 18.55f, RowH = 150f;
        // `Weekly Mission Holder`  N(1, 0,1, 0,1, 0,0, 205.68,-918.1, 1518.99,227.51)
        static readonly Vector2 WM_A0 = UguiRect.A01, WM_A1 = UguiRect.A01, WM_P = UguiRect.P00,
                                WM_Pos = new Vector2(205.68f, -918.1f), WM_Sz = new Vector2(1518.99f, 227.51f);

        /// <summary>`Daily Missions Holder` 的 `VerticalLayoutGroup`：**padTop 0 / spacing 18.55 / align 7 = LowerCenter**。
        /// 三行 150 高 + 2×18.55 = 487.1，容器 501.43 ⇒ 靠**下**对齐（正本 §三·1）。</summary>
        public static PxRect RowRect(PxRect holder, int index)
        {
            float top = holder.y2 - (RowH * 3f + DH_Spacing * 2f) + (RowH + DH_Spacing) * index;
            return new PxRect(holder.x1, top, holder.x2, top + RowH);
        }

        // ============================================================ 建

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }

        public override void Setup() { Build(); }

        public override void OnOpen() { }

        /// <summary>建整页。**自检与运行时同一条路**。</summary>
        public void Build()
        {
            if (_win == null || _root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_root.GetChild(i).gameObject);

            var mt = UguiRect.Child(RootRect, MT_A0, MT_A1, MT_P, MT_Pos, MT_Sz);
            var nm = UguiRect.Child(mt, NM_A0, NM_A1, NM_P, NM_Pos, NM_Sz);
            var sm = UguiRect.Child(nm, SM_A0, SM_A1, SM_P, SM_Pos, SM_Sz);
            var dm = UguiRect.Child(nm, DM_A0, DM_A1, DM_P, DM_Pos, DM_Sz);
            var dh = UguiRect.Child(dm, DH_A0, DH_A1, DH_P, DH_Pos, DH_Sz);
            var wm = UguiRect.Child(mt, WM_A0, WM_A1, WM_P, WM_Pos, WM_Sz);   // ⚠️ 父是 Missions Tab，与 Normal Missions **同级**

            // 容器节点（**有真实位置** —— 原版每个节点都有自己的 rect；自检要按它的位置量）
            var nmNode = RewardsWindow.Node(_root, "Normal Missions", nm);
            var smNode = RewardsWindow.Node(nmNode, "Special Missions", sm);
            var dmNode = RewardsWindow.Node(nmNode, "Daily Missions", dm);
            var dhNode = RewardsWindow.Node(dmNode, "Daily Missions Holder", dh);
            var wmNode = RewardsWindow.Node(_root, "Weekly Mission", wm);

            // 特殊任务区：`Special Missions` 的 `localScale = 1.15` **缩放的是整棵子树**（原版行为）
            // ⇒ 卡内每一件都要绕它的 **pivot = `(0,1)` 左上角**缩放一次。
            // 🔴 **2026-09-23 修**：原来只把**两张卡的矩形**过了 `ScaleAbout`，卡内一律用**未缩放**的 sizeDelta
            //    ⇒ 卡内每件都比原版小 15%（像素实测：骷髅卡 5 个里程碑格的**间距我们 60px、原版应是 69px**）。
            //    代码注释②当时写的就是「它子树的位置与尺寸都要绕它的 pivot 缩放」—— **意图对、实现漏了子树**。
            // **做法（本文件此后一律遵守）**：卡内按**设计空间**（未缩放）算矩形，
            //    只在**建对象的那一刻**过 `R()` 换成最终矩形（`Draw` / `Txt` / `NodeD` / `Node`+`R`）。
            _s = SM_Scale; _so = new Vector2(sm.x1, sm.y1);
            BuildLoginCard(smNode, UguiRect.Child(sm, DL_A0, DL_A1, DL_P, DL_Pos, DL_Sz));

            // `Daily Skulls Mission Container`（页内那份）尺寸是 0×0（靠 `FlexibleLayoutSizeOption` 运行时定）
            // ⇒ 用**独立预制体 `Daily Skulls Mission Container Small`** 的实尺 336×277.5（正本 §三·4）。
            BuildSkullsCard(smNode, UguiLayout.HorizontalChild(sm, 336f, 277.5f, 1, 0f, 11.01f));   // HLG sp **11.01**
            _s = 1f; _so = Vector2.zero;

            BuildMissionHeader(dmNode, dm, false);
            for (int i = 0; i < 3; i++) BuildDailyRow(dhNode, RowRect(dh, i), i);

            BuildWeekly(wmNode, ScaleAbout(wm, wm.x1, wm.y1, 1f));   // 周常没有额外缩放（1.0 是显式的，便于以后改）

            // ⚠️ `Daily Missions Holder` 里的行**是布局组排的**，上面按 LowerCenter + spacing 18.55 算；
            //    这一条进了自检（`RewardsScene`），别只靠肉眼。
        }

        /// <summary>绕 (px,py) 把矩形缩放 s 倍（原版 `localScale` 的几何效果）。</summary>
        public static PxRect ScaleAbout(PxRect r, float px, float py, float s)
            => new PxRect(px + (r.x1 - px) * s, py + (r.y1 - py) * s,
                          px + (r.x2 - px) * s, py + (r.y2 - py) * s);

        // ============================================================ 卡片缩放（原版 `Special Missions` 的 localScale）
        //
        // 约定：**卡内一律按「设计空间」（未缩放）算矩形**，只在建对象的那一刻过 `R()`。
        // 理由：原版 `localScale` 缩放的是**整棵子树**，位置与尺寸**都要**乘 —— 只缩放外框会得到
        // 「框对了、里面的东西小一圈且偏位」这种**看着像对的**错（2026-09-23 实测就是它）。

        float _s = 1f;
        Vector2 _so;

        /// <summary>设计空间矩形 → **最终（已缩放）**矩形。</summary>
        public PxRect R(PxRect r) { return _s == 1f ? r : ScaleAbout(r, _so.x, _so.y, _s); }

        /// <summary>画一件（`r` 是**设计空间**矩形）。</summary>
        ImageQuad Draw(Transform parent, string art, PxRect r, string name, int q,
                       Color? tint = null, bool keepAspect = false)
        {
            var f = R(r);
            return _win.Rect(parent, art, f.x1, f.x2, f.y1, f.y2, name, q, tint, keepAspect);
        }

        /// <summary>画一段字（`r` 是**设计空间**矩形）。</summary>
        Label Txt(Transform parent, PxRect r, string text, Color color, string name, float fontPx, float autoMinPx = 0f)
        {
            var f = R(r);
            return _win.TextBox(parent, f, text, color, name, fontPx, autoMinPx);
        }

        /// <summary>建一个有矩形语义的容器节点（`r` 是**设计空间**矩形）。</summary>
        Transform NodeD(Transform parent, string name, PxRect r)
        {
            return RewardsWindow.Node(parent, name, R(r));
        }

        /// <summary>画一段**不换行**的字（原版 `m_TextWrappingMode = 0` 的那些：按钮文案、计数…）。
        /// `Txt` 走的是 `TextBox`（**限宽换行**），窄框里会把 `13/15` 拆成两行（2026-09-23 踩到）。</summary>
        Label Txt1(Transform parent, PxRect r, string text, Color color, string name, float fontPx)
        {
            var f = R(r);
            return _win.Text(parent, text, f.x1, f.x2, f.y1, f.y2, 4, color, name, fontPx);
        }

        /// <summary>把一段字**左对齐**到设计空间矩形 `r` 的左边缘。
        /// 出处：这批 TMP 的 `m_HorizontalAlignment` 实测 **`H=1 (Left)`**
        /// （`Mission Header` 与三张卡的 `name` · 每日行的 `description`/`timer`/`progress` ·
        /// 周常的 `counter`）。⚠️ 卡片上的 `Timer` 例外，它是 `H=2 (Center)`。</summary>
        void AlignL(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignLeftOn(LayoutSpace.FromPixel(R(r).x1, 0f).x);
        }

        /// <summary>把一段字**右对齐**到设计空间矩形 `r` 的右边缘（原版 `timer` 那一行的用法）。</summary>
        void AlignR(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignRightOn(LayoutSpace.FromPixel(R(r).x2, 0f).x);
        }

        // ============================================================ 三种卡
        //
        // 卡内五元组出处：`工具/menu_rect.py <pid> --cs --root-size <显示尺寸>`。

        /// <summary>`Daily Mission Container`（每日任务行）· 作者尺寸 **787.973×150**，被布局撑到**容器宽**。
        /// 内部锚点按父宽重分布（实测：539.19 宽时 `description` 是 395.64 宽）。</summary>
        public void BuildDailyRow(Transform parent, PxRect row, int index)
        {
            // 卡自己建一个**有矩形语义的节点**，行内所有件挂在它下面（原版每个节点都有自己的 rect；
            // 挂在页级父节点上的话，`FindChild(行, "description")` 找不到 —— 自检当场报出来过）。
            parent = RewardsWindow.Node(parent, "Daily Mission Container (" + index + ")", row);
            // 整卡底：`40K_missions_display_Daily horizontal` ·
            // `MissionBackgroundHighlighter.normalColor` = (0.4941,0.5686,0.9176,1)（**可领取**时换 (1,0.6667,0.3451,1)）
            _win.Rect(parent, "40K_missions_display_Daily_horizontal", row.x1, row.x2, row.y1, row.y2,
                      "Background", RewardsWindow.QPanel, DailyData.RowTint(index));

            // 🔴 **`description` 与 `timer` 是互斥的**（2026-09-23 取证 · 2026-10-04 定性）：
            //    原版 `MissionInfoDisplay.DisplayRule` 是 `[Flags]` 枚举
            //    **`WhenActive = 1` · `WhenComplete = 2`**（类桩 `Assembly-CSharp/MissionInfoDisplay.cs:6-13`），
            //    实现在 `DF:MissionInfoDisplay__Initialize.c:10-27`：
            //      `show = (IsComplete() && WhenComplete) || (!IsComplete() && WhenActive)`
            //    （亲读指令流：`(rule >> 1) & IsComplete` 与 `(rule & 1) && !IsComplete` 两条或起来
            //      → `SetActive(show)`，**`if (show)` 才调虚方法 `Setup`**）。
            //    本行的 `description` 是 **1**、`timer` 是 **2** ⇒ **永远不会同时出现**；
            //    这两条 TMP 的矩形本来就是**重叠**的（`description` 136.67..532.31 · `timer` 136.68..408.77，
            //    两者都是 `H=Left`）—— 原版靠这条规则保证不打架，**我们原来两条都画 ⇒ 文字叠成一团**。
            //    🔴 **2026-10-04（A36-①）把 `IsComplete()` 的语义坐实了 =「奖励已领取/已结算」，不是「进度到顶」**
            //      （两条独立证据链 → `DailyData.DailyClaimed` 的注释）⇒ 这一行该显示哪一条：
            //        · **未领取 ⇒ `description`**（`description` 是 `WhenActive`）
            //        · **已领取 ⇒ `timer`**（`timer` 是 `WhenComplete`）
            //      ⚠️ 原来取的是「进度到顶」，**方向反了** ——「10/10 未领取」那一态原版显示的是**说明文字**，
            //      不是倒计时（`progress` 那个 `MissionCounterDisplay` 此时正显示 `Missions/Completed` ——
            //      ✅ **那一件就是 A63，2026-10-04 已经做了**，判据 + 实现在 `DailyData.DailyCounterText`
            //      与下面 `progress` 那一行；**别把 `Missions/Completed` 读成「还没做」**）。
            bool claimed = DailyData.DailyClaimed(index);

            // 🆕 **2026-10-04（A44 甲）：本行**五个** `MissionInfoDisplay` 系的件全靠这一条 `claimed` 决定画不画** ——
            //    `description`（`MissionInfoDisplay` dr1）· `Rewards`（`MissionRewardsDisplay` dr1）·
            //    `Mission Milestones Progress Bar/Progress Bar`（`MissionProgressBarDisplay` dr1）·
            //    `progress`（`MissionCounterDisplay` dr1）· `Trash mission`（`MissionReRollButton` dr1）
            //    ⇒ **未领取全画、已领取全藏**；`timer` 是这一行**唯一** dr=2 的件（**已领取才画**）。
            //    判据三份、互相独立：
            //      ① 反编译 `DF:MissionInfoDisplay__Initialize.c`：`show = ((dr>>1) & IsComplete) || ((dr & 1) && !IsComplete)`
            //         → `SetActive(show)`（**`if (show)` 才调虚方法 `Setup`**）—— 四个类都 `: MissionInfoDisplay`
            //         （类桩 `Assembly-CSharp/MissionRewardsDisplay.cs:3` / `MissionProgressBarDisplay.cs:4` /
            //          `MissionCounterDisplay.cs:8` 各自 `protected override void Setup`）。
            //      ② **真包 MB 实读**（`assets_full/bundle_menus_assets_all/MonoBehaviour/`，按 `m_Father` 父链认行；
            //         下表 = 独立预制体 `Daily Mission Container` 那一份，另两份实例（`Missions Tab/…` 与
            //         `Rewards Base Submenu Variant/…`）逐件同值）：
            //           `Daily Mission Container/Rewards`                                  MB 5084072559339706576 **dr=1**
            //           `Daily Mission Container/Mission Milestones Progress Bar/Progress Bar` MB -204080914756200240 **dr=1**
            //           `Daily Mission Container/Mission Milestones Progress Bar/progress`     MB 3476392019656054992 **dr=1**
            //           `Daily Mission Container/Generic UI Button`                        MB -2345300010676315952 **dr=1**
            //           （`description` MB -4812140206590256944 与 `Trash mission` MB 5779676786542647504 也各 dr=1；
            //             `timer` MB 4188647697997699280 是这一行唯一的 **dr=2**）
            //      ③ **`IsComplete()` = 「奖励已领取」**（不是「进度到顶」）—— 两条独立证据链见 `DailyData.DailyClaimed` 的注释。
            //    ⇒ 实现 = 用上面那个 `claimed` 把这五件包起来；**别用「进度到顶」**（1.0 版方向反了，见 `DailyData.cs:98`）。
            //    ⚠️ 藏 = 原版的 `SetActive(false)`（静默），不是我们偷懒 —— 所以不出声。

            // `description`  N(1, 0,1, 0.986689,1, .5,.5, 68.4878,-43.873, -136.374,62.253)
            var desc = UguiRect.Child(row, new Vector2(0f, 1f), new Vector2(0.986689f, 1f), UguiRect.P50c,
                                      new Vector2(68.4878f, -43.873f), new Vector2(-136.374f, 62.253f));
            // 原版那条 TMP 实测：`m_fontSize 35 · m_TextWrappingMode 1 · m_enableAutoSizing 1 · min 15` · `H=Left`
            // 显示条件 = **`!IsComplete`**（`displayRule = 1 (WhenActive)`）⇒ **未领取**才画
            if (!claimed)
                AlignL(Txt(parent, desc, DailyData.DailyDesc(index), Color.white, "description", 35f, 15f), desc);

            // `timer`  N(1, 0,1, 1,1, .5,.5, 3.13226,-43.873, -267.097,62.253)   白 α0.59 · `H=Left`
            var tim = UguiRect.Child(row, new Vector2(0f, 1f), new Vector2(1f, 1f), UguiRect.P50c,
                                     new Vector2(3.13226f, -43.873f), new Vector2(-267.097f, 62.253f));
            // 显示条件 = **`IsComplete`**（`displayRule = 2 (WhenComplete)`）⇒ **已领取**才画
            // ⚠️ 本行 MB 是 `MissionTimerDisplay`（`MissionInfoDisplay` 的派生类），`dr = 2` 是**独立复读**到的
            //    （MB `9196887547440731903`）—— 别拿 `description` 那条 `dr=1` 当通例（同一个类在不同行 dr 可以不同）。
            if (claimed)
                AlignL(Txt(parent, tim, DailyData.DailyTimer(index), new Color(1f, 1f, 1f, 0.59f), "timer", 35f, 15f), tim);

            // `Separator Line`  N(1, 0,0, 0,1, 1,0.5, 122.062,-0.0370026, 1.60199,-3.049)  无 sprite，只有色
            var sep = UguiRect.Child(row, UguiRect.A00, new Vector2(0f, 1f), new Vector2(1f, 0.5f),
                                     new Vector2(122.062f, -0.0370026f), new Vector2(1.60199f, -3.049f));
            _win.Rect(parent, null, sep.x1, sep.x2, sep.y1, sep.y2, "Separator Line", RewardsWindow.QContent,
                      new Color(0.25f, 0.25f, 0.41f, 0.59f));

            // `Rewards`  N(1, 0,0, 0,0, 0,0, 3.05e-05,0, 126.334,150)  → `Reward Display Mission Vertical Variant`
            var rew = UguiRect.Child(row, UguiRect.A00, UguiRect.A00, UguiRect.P00,
                                     new Vector2(3.05176e-05f, 0f), new Vector2(126.334f, 150f));
            // 显示条件 = **`!IsComplete`**（`MissionRewardsDisplay : MissionInfoDisplay`、实例 `displayRule = 1`）⇒ 未领取才画
            // 🔴 **这一格画的是「这条任务自己」的奖励**（🆕 2026-10-05 **B4**）—— 与 `CollectDaily` 发的那一份**同源**：
            //    → `DailyData.DailyRewardArt(index)` / `DailyRewardText(index)`。
            //    ⛔ 别再换回「按下标查表」：那张表在第 3 行画「骷髅 ×150」、而实发的是这条任务的**金块 ×200**
            //    （图标 / 数量 / 发放三者不一致），而且**重摇换了任务之后格子里那个数不会跟着变**。
            if (!claimed)
                BuildRewardCell(parent, rew, DailyData.DailyRewardArt(index), DailyData.DailyRewardText(index),
                                index.ToString());

            // `Mission Milestones Progress Bar`  N(1, 0,0.5, 0.316,0.5, .5,.5, 98.02,-29.026, -77.3111,51.8301)
            var mmpb = UguiRect.Child(row, new Vector2(0f, 0.5f), new Vector2(0.316f, 0.5f), UguiRect.P50c,
                                      new Vector2(98.02f, -29.026f), new Vector2(-77.3111f, 51.8301f));
            // 🔴 里面**两件都吃 `displayRule = 1 (WhenActive)`**（`MissionProgressBarDisplay` / `MissionCounterDisplay`，
            //    实例实读各 dr=1）⇒ 与 `description` 同一条规则：**未领取才画**。⚠️ 外面那个
            //    `Mission Milestones Progress Bar` 节点**自己没有脚本**（纯容器），所以只需管这两个子的。
            if (!claimed)
            {
                //   └ `Progress Bar`  N(2, 0,0, 1,0.33, 0,1, 5.5,-5.5, -5.5,-5.5)  bg/fill 都是 `40k_generial_bar_*` 九宫格
                var pb = UguiRect.Child(mmpb, UguiRect.A00, new Vector2(1f, 0.33f), new Vector2(0f, 1f),
                                        new Vector2(5.5f, -5.5f), new Vector2(-5.5f, -5.5f));
                BuildBar(RewardsWindow.Node(parent, "Progress Bar", pb), pb, DailyData.DailyProgress01(index));
                //   └ `progress`  N(2, 0,0.33, 1,1, 0,0, 0,-3, 0,6)  文本 `52/500` fs35 色 (1,0.77,0.33,1)
                var pt = UguiRect.Child(mmpb, new Vector2(0f, 0.33f), UguiRect.A11, UguiRect.P00,
                                        new Vector2(0f, -3f), new Vector2(0f, 6f));
                // 🆕 **A63（2026-10-04）：到顶换文案** —— 原来这一行恒写 `DailyCounter`（`52/500`），
                //   而原版 `MissionCounterDisplay__Setup.c:24-26,68-79` 在 **`currentValue >= MaxValue`**
                //   时改显示 `completedMessage`（出厂 `"Missions/Completed"`、`displayCompletedMessage=1`；MB 实读）。
                //   🔴 **判据只此一处**：`DailyData.DailyCounterText` —— 它同时管着「那个串是原版的 I2 词条【键】、
                //   本地没有语言表 ⇒ 照抄键本身 + 出声」那条口径；本行只负责画。
                AlignL(Txt1(parent, pt, DailyData.DailyCounterText(index), new Color(1f, 0.77f, 0.33f, 1f), "progress", 35f), pt);
            }

            // `Generic UI Button`  N(1, 1,0, 1,0, .5,.5, -145.3,40.7107, 254.611,56.4767)   `40K_button` 色 (1,0.53,0,1) type=1
            var btn = UguiRect.Child(row, UguiRect.A10, UguiRect.A10, UguiRect.P50c,
                                     new Vector2(-145.3f, 40.7107f), new Vector2(254.611f, 56.4767f));
            // 显示条件 = **`!IsComplete`**（`MissionInfoDisplay`、实例 `displayRule = 1`）⇒ **已领取才藏 `Collect`**。
            // ✅ 于是「藏了 Collect ⇒ 玩家领不了奖」**不成立**：领不到奖那一态（`Collectable`）它是**在**的
            //    （`CollectDaily` 只在 `St == Collectable` 时才真的发奖 —— 见 `DailyData.CollectDaily`）。
            if (!claimed)
                BuildButton(parent, btn, "40K_button", new Color(1f, 0.53f, 0f, 1f), "Collect", 35f, "Generic UI Button",
                            () => CollectThenRebuild(index));

            // `Trash mission`  N(1, 1,0, 1,0, .5,.5, -307.44,40.711, 49.104,49.368)   `40k_general_bt_yellow` 色 (1,0.77,0.33,1)
            var trash = UguiRect.Child(row, UguiRect.A10, UguiRect.A10, UguiRect.P50c,
                                       new Vector2(-307.44f, 40.711f), new Vector2(49.104f, 49.368f));

            // 🆕 2026-10-04（A23）：这颗垃圾桶**不是「删除任务」，是【重摇任务】** —— 原来这里只画了图、
            //   **没有命中区** ⇒ 玩家点了没反应（缺口记在 `项目任务.md` §三 第 29 条 A23）。
            //   判据：原版那个节点挂的是 `MissionReRollButton`（字段 `button` / `displayRule` / `reRollPopup`），
            //   点它的链 = `WindowsManager.OpenWindow(<MissionReRollPopup>, ctx)`（反编译：
            //   `MissionReRollButton.__c__DisplayClass3_0___Setup_b__0.c`；见 `资料/待办判据_阶段二与联机.md` §A23 一）。
            //   🔴 **别照着图标猜语义**（「垃圾桶」在本工程也有过别的含义）。
            //   🔴 悬停换图：原版那颗 `EverguildButton` 是 `trans=2 (SpriteSwap)`，`m_SpriteState` 实读
            //   `HL = 40k_general_bt_yellow_hover` · `P = 40k_general_bt_yellow_pressed`
            //   （`python 工具/menu_dump.py bundle_menus_assets_all "Missions Tab" --depth 6`，pid 回真包反查）
            //   ⇒ 两个图名**逐颗显式传**（不靠 `<常态图>_hover` 那条后备规律 —— 虽然这次恰好同值）。
            //
            //   🔴🔴 **显隐（2026-10-04 A36-① 修两轮：先解出「按状态」，再订正「是哪个状态」）**：
            //     完整规则 = **`(!IsComplete) && (Price.amount > 0)`**，两半的判据都在本地：
            //       · **前半（`!IsComplete` 才显示）**：`MissionReRollButton : MissionInfoDisplay`（类桩
            //         `Assembly-CSharp/MissionReRollButton.cs:4`），而基类 `displayRule` 是
            //         `[Flags]{ WhenActive = 1, WhenComplete = 2 }`、**全包 8 个实例实测全是 1 (WhenActive)**；
            //         基类 `DF:MissionInfoDisplay__Initialize.c` 的 `show = (WhenComplete && IsComplete)
            //         || (WhenActive && !IsComplete)`（亲读指令流：`(b >> 1) & IsComplete` 与
            //         `(b & 1) && !IsComplete` 两条或起来 → `SetActive(show)`，**`if (show)` 才调 `Setup`**）
            //         ⇒ **原版把「已领取」那一行的垃圾桶藏起来**。**我们照这条做**：用上面那个 `claimed`。
            //         🔴 **注意是「已领取」不是「进度到顶」** —— 1.0 版取的是后者（方向反了，等于删掉原版一个能力：
            //         「10/10 未领取」那一行原版**是给重摇的**）。判据见 `DailyData.DailyClaimed` 的注释。
            //       · **后半（`Price.amount > 0`）**：`MissionReRollButton.Setup` 末段那个整数二次查实 = `Price.amount`
            //         （`Price` 桩 = `currency` + `amount`）—— **数值本身本地读不到**（服务端下发 +
            //         `MissionData.get_RerollPrice` 是错桩）⇒ **这一半如实恒真**（不假装它判过）。
            //     判据正本 = `资料/待办判据_阶段二与联机.md` §A23 三·1；自检 = `Editor/RewardsScene.cs` 的
            //     「已领取那行的垃圾桶不建 / 点它开不开得出重摇窗 / 退回未领取那行在」三条。
            //     🔴 2026-10-04 顺手查出：这一行**还有四个件也吃同一条 displayRule**（`Rewards` = `MissionRewardsDisplay`、
            //        `Progress Bar` = `MissionProgressBarDisplay`、`progress` = `MissionCounterDisplay`、
            //        `Generic UI Button`（Collect）= `MissionInfoDisplay`，**四个实测 displayRule 全是 1**）——
            //        即原版在「已领取」那一行会把它们**一起藏起来**。
            //     ✅ **2026-10-04（A44 甲）已经照着补齐**（`if (!claimed)` 各包一处，判据见本方法开头那段）；
            //        ⚠️ 原来那句「已报调度台、别在这里顺手改」说的正是这件事 —— 现在它做完了，别再读成「还没做」。
            //   ⚠️ 隐藏是**原版行为**（静默 `SetActive(false)`），不是我们在偷懒 —— 所以这里不出声。
            //   🔴 **用 `if (!claimed)` 包住而不是提前 `return`** —— 这一行以后要再加件时，早退会**静默漏掉**。
            if (!claimed)
            {
                // ⚠️ 原版这一件是 `Simple + preserveAspect`（源图 71×71 塞进 49.104×49.368 的框）⇒ 画出来是 **49.104²**
                var trashQ = Draw(parent, "40k_general_bt_yellow", trash, "Trash mission", RewardsWindow.QContent,
                                  new Color(1f, 0.77f, 0.33f, 1f), true);
                //   └ `Image` = `40k_general_bt_yellow_delete`（`Button Text` 'X' 出厂 inactive ⇒ 不建）
                var ti = UguiRect.Child(trash, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                        new Vector2(-1f, 0f), new Vector2(-2f, -2f));
                Draw(parent, "40k_general_bt_yellow_delete", ti, "Image", RewardsWindow.QOverlay, null, true);
                //   命中区的队列放 **`QOverlay`**（行内最高一档）⇒ 不会被同行任何件抢走
                //   （`BoosterInfoPopup.QShadeHit` 那条：同队列时 `ImageQuad` 的 z 恒为 0，谁吃到命中不可控）。
                int ri = index;
                var trashHit = _win.AddHit(parent, "Hit", R(trash), RewardsWindow.QOverlay,
                                           () => OpenReroll(ri), trashQ, "40k_general_bt_yellow",
                                           "40k_general_bt_yellow_hover", "40k_general_bt_yellow_pressed");
                if (trashHit == null)
                    Debug.LogWarning("[Missions] 第 " + (index + 1) + " 行垃圾桶的**命中区没建出来**（`AddHit` 返回 null）"
                                     + " —— 玩家会点不动它（红线：不许静默失败）");
            }
        }

        /// <summary>🆕 **A64（2026-10-04）**：点每日任务那一行的 `Collect` ⇒ 领奖，**领到了就把整页重建一次**。
        /// <para>**原版那条链（六环，逐环都有本地判据 → `资料/普查产出_1004/X2审查_A44甲.md` §一·附）**：
        /// `MissionContainer.SetChallenge` 给 `collectButton` 挂监听（`RemoveAllListeners` + `AddListener`）→
        /// 回调 = `Missions.CollectChallenge(mission, challenge, onComplete)` → 成功之后调 `onComplete`
        /// = **`MissionContainer.OnCollect`** → `WindowsManager.GetOpenWindow()` 非空 ⇒
        /// `GameWindowWithTabs.ChangeTab&lt;MissionsTab&gt;()` → `ChangeTabCO` → `WindowTabBase.TryOpenTab`（slot 7）→
        /// `OnOpen()` → **`CreateMissions()`**（三个 `ContainerHolder.Clear()` + 逐条重新实例化）= **整页重建**。</para>
        /// ⚠️ 换到的页签**正好是当前这一页**时，`ChangeTabCO` 走的是「不 `CloseTab`、但**照样调 `TryOpenTab`**」
        /// 那一支（`.c` 开头：`op_Equality(old, new)` 为真且 old 非空 ⇒ 直接调 slot 7）⇒ **同一页也会重建**。
        /// ⇒ 我们这边等价于**再 `Build()` 一次**（`Build` 既是自检入口也是运行时入口，同一份实现）。
        /// <para>🔴 **只在真的领到时重建**：没达成时原版那颗钮**根本点不动**
        /// （`MissionContainer__SetChallenge.c`：`Selectable.set_interactable(collectButton, CanCollect(challenge))`）
        /// ⇒ 不能无条件重建（`DailyData.CollectDaily` 因此返回「领没领到」）。</para>
        /// 🔴 **重建会销毁旧的整棵子树** ⇒ 重建之后任何**跨重建持有**的节点/列表句柄都作废
        /// （本仓踩过：自检里的 `rows` 列表必须重收 —— 见 `Editor/RewardsScene.cs` 那一段的注释）。
        /// <para>✅ **2026-10-05（A75①）**：另外三张卡（登录 / 骷髅 / 周常）的 `Collect` 原版走的**是同一条链**
        /// —— `Daily Login Container`（GO `-4858811403846176071` · `MissionContainer` MB `7589217681052316345`）·
        /// `Daily Skulls Mission Container`（GO `6434331890599441081` · MB `2376002841178321593`）·
        /// `Weekly Mission Container`（MB `7948715324918747914`）—— 三个实例的 `collectButton` **都非空**
        /// （真包 MB 实读）⇒ 那三颗**同样会重建整页**。**现在四条路都接了**
        /// （见下面 `CollectThenRebuild(string, Func&lt;bool&gt;)`）。</para>
        /// </summary>
        void CollectThenRebuild(int index)
        {
            CollectThenRebuild("第 " + (index + 1) + " 行", () => DailyData.CollectDaily(index));
        }

        /// <summary>🆕 **A75①（2026-10-05）**：三张**单例卡**（登录 / 骷髅 / 周常）的 `Collect` 走的是
        /// **同一条**链 —— 真包实读 `bundle_menus_assets_all` 里共 **21 个 `MissionContainer` MB**，
        /// 本页四张卡的根各挂一个、**`collectButton` 全部非空**
        /// （每日行 MB `1982546340298365136` · 登录卡 MB `7589217681052316345` ·
        ///  骷髅卡 MB `2376002841178321593` · 周常 MB `7948715324918747914`）
        /// ⇒ 那三颗同样 `OnCollect` ⇒ **整页重建**（我们原来只在每日行那一条路上接了）。
        /// 🔴 **只在这一下真的领到时才重建**：没达成时原版那颗钮 `interactable = CanCollect()` = false，
        /// 点了连派发都没有 ⇒ `collect()` 必须回传「领没领到」。
        /// ⚠️ 那三张卡的 `Collect` **显隐不走 `displayRule`**（它们的 `infoDisplays` 里根本没有 `Collect`
        /// 这一件、`dr` 几乎全是 `-1` = 恒可见）⇒「藏不藏它」由 `interactable` 管，**本件不动它**，
        /// 也**别**把 A44 甲「四个件同吃 `displayRule`」那条口径推广过来。</summary>
        void CollectThenRebuild(string what, System.Func<bool> collect)
        {
            if (!collect()) return;   // 未达成 / 已领过：原版那一下连派发都没有，我们什么都不做
            Debug.Log("[Missions] " + what + " 的 `Collect` **领到了** ⇒ **重建整页**"
                      + "（原版链路：`CollectChallenge` 的 `onComplete` = `MissionContainer.OnCollect` → "
                      + "`ChangeTab<MissionsTab>` → `TryOpenTab` → `OnOpen` → `CreateMissions`；"
                      + "判据 `资料/普查产出_1004/X2审查_A44甲.md` §一·附）");
            Build();
        }

        /// <summary>点垃圾桶 ⇒ 开「重摇任务」窗（原版 `MissionReRollButton` 的点击链，见上面那段注释）。
        /// 弹窗里 `Confirm` 回来时会**重建本页**（新任务要立刻看得见）。</summary>
        void OpenReroll(int index)
        {
            var mgr = _win != null ? _win.Manager : null;
            if (mgr == null)
            {
                Debug.LogWarning("[Missions] 点重摇时拿不到 `WindowsManager`（`_win.Manager` 为空）"
                                 + " ⇒ **窗开不出来**（红线：不许静默失败）");
                return;
            }
            var pop = MissionRerollPopup.Create(mgr);
            mgr.OpenWindow(pop, new MissionRerollContext { Index = index, OnRerolled = Build });
            Debug.Log("[Missions] 第 " + (index + 1) + " 行的垃圾桶 ⇒ 开 `MissionReRollPopup`"
                      + "（**重摇任务**，不是删除；原版链路见 `资料/待办判据_阶段二与联机.md` §A23）");
        }

        /// <summary>`Daily Login Bonus Container`（竖卡 334×555）。`card` 是**设计空间**矩形（见 `R`）。</summary>
        public void BuildLoginCard(Transform parent, PxRect card)
        {
            parent = NodeD(parent, "Daily Login Container", card);
            Draw(parent, "40K_missions_display_Daily_vertical", card, "Daily Login Bonus Container", RewardsWindow.QPanel);
            BuildCardHeader(parent, card, "Daily Login Bonus", DailyData.LoginTitle(), 30f, false);

            // `body.image`  N(3, 0,0, 1,1, .5,0, **0,−29**, 0,0)  → `40K_missions_icon_login bonus`
            var body = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-0.0018845f, 84.7947f), new Vector2(325f, 261.131f));
            // 🔴 **2026-09-23 修**：原来第 5 个参数传的是 `Vector2.zero`，把 `pos=(0,−29)` 丢了。
            //    判据（`工具/menu_rect.py "Daily Login Bonus Container" --depth 4 --relative` 实算）：
            //    `body` = 4.50..329.50 × **62.14..323.27**，`image` = 4.50..329.50 × **91.14..352.27** ⇒ **下移 29.00**。
            //    `pos.y` 是 **up-positive**（UGUI），所以「下移 29」写 **−29**。
            var img = UguiRect.Child(body, UguiRect.A00, UguiRect.A11, UguiRect.P50,
                                     new Vector2(0f, -29f), Vector2.zero);
            var q = Draw(parent, "40K_missions_icon_login_bonus", img, "image", RewardsWindow.QContent);
            if (q != null) { q.SetAspect(262f / 212f); }        // 图 262×212（正本 §九）

            // `footer.Rewards`  N(3, .5,.5, .5,.5, .5,.5, 0,52.108, 325,77.643)  两个奖励格
            // 该组实测 `m_ChildAlignment=4(MiddleCenter) · ctlW/H=1 · expW/H=1` ⇒ 两格**等分** 325（我们照此）
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(-1.5201f, -153.84f), new Vector2(325f, 181.86f));
            var rw = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(0f, 52.108f), new Vector2(325f, 77.643f));
            // 🔴 **两格画的是「登录卡这一份奖励」**（🆕 2026-10-05 **B4**）—— 与 `CollectLogin` 发的**同源**
            //    （`DailyData.LoginRewardArt/Count`，逐格发）。⛔ 别再走「按下标查的公共表」：
            //    那张表原来在这一卡上画的是「金块 ×150 + 封印点 ×20」，而 `CollectLogin` 只发金块 ×100
            //    ⇒ 图标 / 数量 / 发放三者不一致。
            for (int i = 0; i < DailyData.LoginRewardCells; i++)
                BuildRewardCell(parent, new PxRect(rw.x1 + rw.W * 0.5f * i, rw.y1, rw.x1 + rw.W * 0.5f * (i + 1), rw.y2),
                                DailyData.LoginRewardArt(i), DailyData.LoginRewardCount(i).ToString(), i.ToString());

            // `footer.Generic UI Button`  N(3, …, 3.1692,-24.0231, 255.992,74.6201)  `40K_button` 色 (1,0.47,0.10,1)
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(3.1692f, -24.0231f), new Vector2(255.992f, 74.6201f));
            // 🆕 **A75①**：这一颗也走 `OnCollect` ⇒ **领到就重建整页**（判据见 `CollectThenRebuild(string,…)`）。
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 35f, "Generic UI Button",
                        () => CollectThenRebuild("登录卡", () => DailyData.CollectLogin()));

            // `footer.TimerHolder`  N(3, 0,0.5, 1,0.5, .5,0, 0,-118.5, 0,57.167)   文本 'Resets in …' fs28 灰
            // 🔴 **显示条件 = 已领取**（🆕 **A75②**）：原版这一件是 **`MissionTimerDisplay` · `displayRule = 2
            //    (WhenComplete)`** ⇒ 与每日行的 `timer` 同一条规则（`description`/`timer` 互斥那条）。
            //    判据（真包实读，2026-10-05 复核）：登录卡 `MissionContainer` MB `7589217681052316345` 的
            //    `infoDisplays` 第 2 项 = MB `3730529517176468153`（**`displayRule = 2`**），
            //    其 `m_GameObject` → `GameObject/Timer_-4321384230747458887.json`；规则本体 =
            //    `DF:MissionInfoDisplay__Initialize.c:10-27`。**我们原来恒画它**（`Resets in 12h 34 m`）。
            //    ⚠️ 与每日行同源：谓词是 `IsComplete` = **奖励已领取**（不是「进度到顶」），见 `DailyData.LoginClaimed`。
            //    ⚠️ 文案照旧是 **prefab 出厂那个串** `'Resets in 12h 34 m'`（§3·3 #16）—— 原版运行期按 I2 词条本地化，
            //    本地没有语言表（`DailyData.ResetIn`）。
            if (DailyData.LoginClaimed())
            {
                var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                        new Vector2(0f, -118.5f), new Vector2(0f, 57.167f));
                var tl = Txt(parent, th, DailyData.ResetIn(), new Color(0.5686f, 0.5686f, 0.5882f, 1f), "Timer", 28f);
                if (tl == null) Debug.LogWarning("[Rewards] 登录卡的 `Timer` 没建出来（红线：不许静默失败）");
            }
        }

        /// <summary>自检用：每日骷髅卡 `counter/icons` 那块「图标区」的**左边缘**（画布 px）。
        /// 判据 → `MissionCounterDisplay__Setup.c:51-63`（`Army == Neutral` 时那一格不显示 ⇒
        /// 这个值就是 `counter` 自己的左边缘，**没有那 60px**）。</summary>
        public static float SkullIconLeftPx { get; private set; }

        /// <summary>`Daily Skulls Mission Container Small`（336×277.5）—— 5 格里程碑 + 计数 + 领奖。
        /// ⚠️ 页内那份实例的 `body`/`progress` 尺寸与独立预制体**不同**（正本 §三·4 vs 页内实例）；
        /// 我们照**独立预制体 Small**（那套尺寸是确定的）。`card` 是**设计空间**矩形（见 `R`）。</summary>
        public void BuildSkullsCard(Transform parent, PxRect card)
        {
            parent = NodeD(parent, "Daily Skulls Mission Container", card);
            Draw(parent, "40K_missions_display_Daily_vertical", card, "Daily Skulls Mission Container", RewardsWindow.QPanel);
            BuildCardHeader(parent, card, "Daily Skulls", DailyData.SkullsTitle(), 36f, false);

            // `progress.milestones`  N(3, 0,0, 1,1, .5,.5, 0,0, ~0,~0)  → `steps` HLG **spacing 20** align 4(MiddleCenter)，每格 40×40
            var prog = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-0.0018959f, -21.313f), new Vector2(325f, 79.992f));
            var ms = UguiRect.Child(prog, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero, Vector2.zero);
            // 🔴 **2026-09-23 修**：`steps` 的实测布局组参数是
            //    `align=4 (MiddleCenter) · sp=20 · ctlW=0 · ctlH=0 · expW=0 · expH=1`
            //    ⇒ 5 格各 40 + 4×20 = **280 宽放进 325 的容器**，`MiddleCenter` ⇒ **左右各留 22.5**。
            //    原来 `UguiLayout.HorizontalChild` **不做水平对齐**（从容器左边起排）⇒ 整排偏左 22.5px。
            float contentW = 5f * 40f + 4f * 20f;
            float padL = (325f - contentW) * 0.5f;
            for (int i = 0; i < 5; i++)
                BuildMilestone(parent, UguiLayout.HorizontalChild(ms, 40f, 40f, i, padL, 20f),
                               DailyData.SkullsStepDone(i), true);

            // `footer.Rewards`  N(3, …, -103.7,14.204, 109.25,47.433)  → `40K_missions_icon_Daily skulls` + 'x160'
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(-1.5201f, -100.83f), new Vector2(325f, 75.84f));
            var rw = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(-103.7f, 14.204f), new Vector2(109.25f, 47.433f));
            // 🔴 **这一格画的是「骷髅卡这一份奖励」**（🆕 2026-10-05 **B4**）—— 与 `CollectSkulls` 发的**同源**。
            //    原来走的是按下标的公共表 ⇒ 这一卡画的是**封印点 ×20**（而 `CollectSkulls` 发 0 个骷髅）
            //    = 图标 / 数量 / 发放三者全对不上。判据 = 原版 prefab §3·4 #6：`CampaignPointDrawer`、count **'200'**。
            BuildRewardCell(parent, rw, DailyData.SkullsRewardArt(), DailyData.SkullsRewardCount().ToString(), "1");
            // `footer.counter`  N(3, …, -79.2,150.3, 167.6,59.925)
            //   `counter` 自己也有布局组；`icons` 那条 HLG 的**两个格子**实测是
            //   `Army`（60 宽，模板占位图 `40k_DeckSelection_icon_FactionBlackLegion`）+ `skull`（65 宽）⇒ 图标区共 **125 宽**。
            // 🔴 **2026-10-03 查实并改对**（`项目任务.md` §三 第 29 条 **B1**）：
            //   真机制 = `d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:51-63` ——
            //   图 = `ArmyUtilities.GetArmyIcon(challenge.Army)`，**且 `army == Neutral(0)` 时
            //   那个 `Army` 整格 `SetActive(false)`**（HLG 会跳过它 ⇒ skull 与计数文字**整体左移 60**）。
            //   prefab 里那个 `40k_DeckSelection_icon_FactionBlackLegion` **只是模板占位**，不是真值。
            //   ⚠️ **我们这份 daily 数据里没有阵营维度**（`Army` 由服务端下发、`grep anyArmy` 只命中静态成就）
            //   ⇒ 按 **Neutral** 走 —— 也就是**不画那一格、也不给它留位**（此前是留了 60px 空槽，**是错的**）。
            var cnt = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(-79.2f, 150.3f), new Vector2(167.6f, 59.925f));
            const float armyW = 60f, skullW = 65f;
            float iconL = cnt.x1;
            string army = DailyData.SkullsArmy();                 // 我们的 mock 恒 null = Neutral
            if (!string.IsNullOrEmpty(army))
            {
                // 有阵营才画那 60 宽（图走 `DeckRuntime.FactionIcon` —— 全工程唯一一份阵营徽记）
                Draw(parent, DeckRuntime.FactionIcon(army),
                     new PxRect(iconL, cnt.y1, iconL + armyW, cnt.y2), "Army", RewardsWindow.QContent,
                     null, true);
                iconL += armyW;
            }
            else
            {
                Debug.Log("[Missions] 每日骷髅任务的 `counter/Army` 那一格**不建**（原版 `army == Neutral(0)` 时 "
                          + "`SetActive(false)`；我们这份 daily 没有阵营维度 ⇒ 走 Neutral 分支，**也不给它留 60px**）");
            }
            SkullIconLeftPx = R(new PxRect(iconL, cnt.y1, iconL, cnt.y2)).x1;   // 自检用（转成**画布 px**）
            Draw(parent, "40K_missions_icon_Daily_skulls",
                 new PxRect(iconL, cnt.y1, iconL + skullW, cnt.y2), "skull", RewardsWindow.QContent);
            // ⚠️ 计数用 `Txt1`（**不换行**）：`icons` 占掉 125 宽后剩下的框只有 ~38 宽，
            //    走 `TextBox` 会把 `x160` 折成 `x1`+`60` 两行（2026-09-23 渲染图就是这个）。
            Txt1(parent, new PxRect(iconL + skullW + 5f, cnt.y1, cnt.x2, cnt.y2),
                 DailyData.SkullsCounter(), Color.white, "counter text", 26.8f);

            // `footer.Generic UI Button`  N(3, …, 58,13.548, 187.467,80.492)  `40K_button` 色 (1,0.47,0.10,1)
            // 🆕 **A75①**：这一颗也走 `OnCollect` ⇒ **领到就重建整页**（同一条链，见 `CollectThenRebuild`）。
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(58f, 13.548f), new Vector2(187.467f, 80.492f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 34.05f, "Generic UI Button",
                        () => CollectThenRebuild("骷髅卡", () => DailyData.CollectSkulls()));

            // `footer.TimerHolder`  N(3, 0,0.5, 1,0.5, .5,0, 83.55,120.34, -167.1,59.926)  时钟 + 时间
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(83.55f, 120.34f), new Vector2(-167.1f, 59.926f));
            BuildClockRow(parent, th, 44.74f, DailyData.ResetIn(), 30.15f);
        }

        /// <summary>`Weekly Mission Container`（1518.99×227.51）· 4 个 70² 里程碑 + 进度条 + `Ends in`。</summary>
        public void BuildWeekly(Transform parent, PxRect card)
        {
            parent = NodeD(parent, "Weekly Mission Container", card);
            Draw(parent, "40K_missions_display_Weekly", card, "Weekly Mission", RewardsWindow.QPanel);

            // `header`  N(2, 0,1, 0.25,1, .5,1, 0,0, 0,55) → `name` 'Weekly Challenge' fs36
            // 🔴 **2026-09-23 补**：`header` 自带 **`Gradient2`（灰蓝那套，5 个 alpha 键）** —— 见 `HeaderGradient`
            var head = UguiRect.Child(card, new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0.5f, 1f),
                                      Vector2.zero, new Vector2(0f, 55f));
            DrawTex(parent, HeaderGradient(true), head, "header bg", RewardsWindow.QPanel);
            Txt(parent, new PxRect(head.x1, head.y1, head.x2, head.y1 + 50f),
                "Weekly Challenge", Color.white, "name", 36f);

            // `progress.Mission Progress Bar`  N(3, 0,0.5, 1,0.5, 0,0.5, 30,-9.6, -60,22.766)
            var prog = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-185.175f, -20.537f), new Vector2(1068.43f, 158.59f));
            var bar = UguiRect.Child(prog, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                                     new Vector2(30f, -9.6f), new Vector2(-60f, 22.766f));
            BuildBar(parent, bar, DailyData.WeeklyProgress01());

            // 🔴 **2026-09-23 补：`Handle` 与骑在它上面的 `counter`**
            //   实测（`工具/menu_rect.py "Weekly Mission Container" --depth 6 --relative --root-size 1518.99x227.51`）：
            //   `Mission Progress Bar` = `Handle Slide Area` = 70.10..1078.54（容器），
            //   `Handle` = 4.14 × 50.60、**无 sprite、色 (0.941,0.725,0.314,1)** ⇒ UGUI 画**一块实心矩形**；
            //   `counter` 是 **`Handle` 的子节点**（模板位 62.53 × 35.01，**底边 = Handle 顶边**）。
            //   原版 `Slider` 的值由 `MissionProgressBarDisplay` 运行时设 ⇒ **把手跟着进度走、数字骑在把手上**。
            //   ⚠️ 每日任务行那份的 `Handle Slide Area` 出厂 **`activeSelf=false`** ⇒ 那一处**不画**才对（我们没画，对）。
            float t = DailyData.WeeklyProgress01();
            const float hw = 4.141f, hh = 50.60f, cw = 62.53f, ch = 35.01f;
            float hx = bar.x1 + t * bar.W;
            var handle = new PxRect(hx - hw * 0.5f, bar.CY - hh * 0.5f, hx + hw * 0.5f, bar.CY + hh * 0.5f);
            Draw(parent, null, handle, "Handle", RewardsWindow.QContent, new Color(0.941f, 0.725f, 0.314f, 1f));
            // ⚠️ 用 `Txt1`（**不换行**）：这个框只有 62.53 宽，走 `TextBox` 会把 `13/15` 折成两行
            //    （2026-09-23 渲染图上就是 `13/` + `15`）。
            Txt1(parent, new PxRect(hx - cw * 0.5f, handle.y1 - ch, hx + cw * 0.5f, handle.y1),
                 DailyData.WeeklyCounter(), new Color(0.92f, 0.77f, 0.48f, 1f), "counter", 33.15f);

            // `Mission Milestones Progress.steps`  N(4, 0,0, 1,1, 0,0.5, 0,47, 0,0)
            //   `EverguildLayoutGroup` spacing **262.81** align 4 ⇒ 4 格 70²，从容器左边起排
            //   （4×70 + 3×262.81 = **1068.43 = 容器宽** ⇒ 对齐方式无关，左右刚好占满）
            var mp = UguiRect.Child(prog, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                    new Vector2(0f, -56.1377f), Vector2.zero);
            var steps = UguiRect.Child(mp, UguiRect.A00, UguiRect.A11, new Vector2(0f, 0.5f),
                                       new Vector2(0f, 47f), Vector2.zero);
            for (int i = 0; i < 4; i++)
                BuildMilestone(parent, UguiLayout.HorizontalChild(steps, 70f, 70f, i, 0f, 262.81f),
                               DailyData.WeeklyStepDone(i), false);

            // `footer`（HLG sp 0）→ `Generic UI Button`  N(3, …, 3.1692,0, 294.29,74.62)
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(582.18f, -29.3f), new Vector2(300.631f, 102.049f));
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(3.1692f, 0f), new Vector2(294.29f, 74.62f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 44f, "Generic UI Button",
                        () => CollectThenRebuild("周常卡", () => DailyData.CollectWeekly()));
            // ⚠️ 周常**没有「奖励格」这一件**（原版 `Rewards` 出厂 `activeSelf = false`，正本 §3·5 #8）
            //    ⇒ B4 改的是「每日行 / 登录卡 / 骷髅卡」三处；周常那份奖励只在 `CollectWeekly` 里（500 金块，我们挑的）。

            // `TimerHolder.Timer`  N(4, 0,0.5, 1,0.5, .5,.5, 0,31.287, 100,57.167)  'Ends in …' fs38 灰
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(0f, -118.5f), new Vector2(0f, 57.167f));
            var tm = UguiRect.Child(th, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UguiRect.P50c,
                                    new Vector2(0f, 31.287f), new Vector2(100f, 57.167f));
            Txt(parent, tm, DailyData.WeeklyEndsIn(), new Color(0.57f, 0.57f, 0.59f, 1f), "Timer", 38f);
        }

        /// <summary>`Mission Header`：标题条（`Daily Missions` 那一条）。
        /// `isSkulls` 只是文案不同（原版同一份 prefab 换了 `name`/`Refill Counter` 的文本）。</summary>
        public void BuildMissionHeader(Transform parent, PxRect hostRect, bool isSkulls)
        {
            // N(3, 0,1, 1,1, .5,1, 1.34,-2.2287, -2.6799,52.7713)
            var h = UguiRect.Child(hostRect, new Vector2(0f, 1f), UguiRect.A11, new Vector2(0.5f, 1f),
                                   new Vector2(1.34f, -2.2287f), new Vector2(-2.6799f, 52.7713f));
            // 🔴 **2026-09-23 补**：这条的底是 `Image(sprite=null, type=Sliced)` + **`Gradient2`**（紫→棕那套），
            //    见 `HeaderGradient` 的注释（`Image` 没 sprite 也会渲染成一块纯色矩形）。
            DrawTex(parent, HeaderGradient(false), h, "Mission Header bg", RewardsWindow.QPanel);
            // `name`  N(3, 0.03,0.5, 0.84,0.5, .5,.5, 0,0, ~0,50)   'Daily Missions' fs36
            var nm = UguiRect.Child(h, new Vector2(0.03f, 0.5f), new Vector2(0.84f, 0.5f), UguiRect.P50c,
                                    Vector2.zero, new Vector2(3.8147e-06f, 50f));
            // ⚠️ 原版这条实测 **`H=Left`**；居中写会和右边右对齐的 `Refill Counter` **叠在一起**
            //    （第一版渲染图上是 `Daily Mis0Disponible`）
            AlignL(Txt(parent, nm, isSkulls ? "Daily Skulls" : "Daily Missions", Color.white,
                       "name (Mission Header)", 36f, 12f), nm);
            // `info`  N(3, 1,0.5, 1,0.5, 1,0.5, -10,0, 41,41)   `40K_generic_bt_info` 41²
            var inf = UguiRect.Child(h, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                     new Vector2(-10f, 0f), new Vector2(41f, 41f));
            Draw(parent, "40K_generic_bt_info", inf, "info", RewardsWindow.QContent);
            // `Refill Counter`  N(3, 0,0.5, 1,0.5, .5,.5, -26.93,0, -53.86,50)   '0 Disponible' fs36
            var rc = UguiRect.Child(h, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UguiRect.P50c,
                                    new Vector2(-26.93f, 0f), new Vector2(-53.86f, 50f));
            var rcT = Txt(parent, rc, DailyData.RefillText(), Color.white, "Refill Counter", 36f, 12f);
            if (rcT != null) rcT.AlignRightOn(LayoutSpace.FromPixel(rc.x2, 0f).x);
        }

        // ============================================================ 小件

        /// <summary>卡片 `header` 的**渐变条**（原版 `Image(sprite=null)` + `Gradient2`）。
        /// 实测两套值（`bundle_menus_assets_all` 的 `_effectGradient`，逐个读出来的）：
        /// · **紫→棕**（`greyBlue=false`）：颜色 (0.247,0.188,0.380)→(0.475,0.306,0.153)，
        ///   alpha 键 **0.2706→1.0 · 0.9059→0.098**（也就是左起 27% 之前**全不透明**、到 91% 才降到 0.098）。
        ///   用在：每日登录卡 · 每日骷髅卡 · `Mission Header`（**三处同值**）。
        /// · **灰蓝**（`greyBlue=true`）：颜色**恒定** (0.227,0.286,0.325)，**5 个 alpha 键**
        ///   0 / 0.3706 / 0.5941 / 0.7176 / 0.8588 → 1.0 / 0.773 / 0.498 / 0.463 / 0.098。用在周常卡。
        /// 轴向 `_gradientType = 0` = **Horizontal**
        /// （`Gradient2.Type { Horizontal=0, Vertical=1, Radial=2, Diamond=3 }`，出处
        /// `Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/Gradient2.cs`）。
        /// 🔴 **`Image` 没有 sprite 也会渲染** —— UGUI 在 `activeSprite == null` 时回落到
        /// `Graphic.OnPopulateMesh`、画一块**纯色矩形**，再被 `Gradient2`（`_modifyVertices=1`）改顶点色
        /// ⇒ 原版每张卡的头都**有一条看得见的渐变条**；我们**一条都没画**（2026-09-23 取证）。</summary>
        static Texture2D HeaderGradient(bool greyBlue)
        {
            if (greyBlue)
                return CardArt.GradientKeys(new Color(0.227f, 0.286f, 0.325f), new Color(0.227f, 0.286f, 0.325f),
                                            0f, new float[] { 0f, 0.3706f, 0.5941f, 0.7176f, 0.8588f, 1f },
                                            new float[] { 1f, 0.773f, 0.498f, 0.463f, 0.098f, 0.098f });
            return CardArt.GradientKeys(new Color(0.247f, 0.188f, 0.380f), new Color(0.475f, 0.306f, 0.153f),
                                        0f, new float[] { 0f, 0.2706f, 0.9059f, 1f },
                                        new float[] { 1f, 1f, 0.098f, 0.098f });
        }

        /// <summary>画一块**运行时生成的**贴图（渐变走这条）。`r` 是**设计空间**矩形。</summary>
        void DrawTex(Transform parent, Texture2D tex, PxRect r, string name, int q)
        {
            if (tex == null) return;
            var f = R(r);
            var quad = ImageQuad.Create(parent, tex, RewardsWindow.Local(parent, f.x1, f.y1, f.x2, f.y2),
                                        LayoutSpace.Px(f.H), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect(f.W / Mathf.Max(1e-6f, f.H));
            quad.SetRenderQueue(q);
        }

        /// <summary>卡头：`header` 渐变条 + `40K_generic_bt_info`（42×42 在右）+ `name`。</summary>
        void BuildCardHeader(Transform parent, PxRect card, string what, string title, float fontPx, bool greyBlue)
        {
            // N(2, 0,1, 1,1, .5,1, 1.34,-2.2287, -2.6799,52.7713)
            var h = UguiRect.Child(card, new Vector2(0f, 1f), UguiRect.A11, new Vector2(0.5f, 1f),
                                   new Vector2(1.34f, -2.2287f), new Vector2(-2.6799f, 52.7713f));
            DrawTex(parent, HeaderGradient(greyBlue), h, what + " header bg", RewardsWindow.QPanel);
            var nm = UguiRect.Child(h, new Vector2(0.03f, 0.5f), new Vector2(0.84f, 0.5f), UguiRect.P50c,
                                    Vector2.zero, new Vector2(3.8147e-06f, 50f));
            // ⚠️ 原版这条 TMP 实测 **`H=Left`**（`m_HorizontalAlignment=1`）⇒ 标题贴左边起，**不是居中**
            AlignL(Txt(parent, nm, title, Color.white, what + " name", fontPx, 12f), nm);
            var inf = UguiRect.Child(h, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                     new Vector2(-10f, 0f), new Vector2(41f, 41f));
            Draw(parent, "40K_generic_bt_info", inf, what + " info", RewardsWindow.QContent);
        }

        /// <summary>进度条：两张图都是**九宫格 `(4,4,4,4)`**、12×12（正本 §九）。底色 `(1,0.59,0,1)` · 填充 `(1,0.77,0.33,1)`。
        /// `r` 是**设计空间**矩形（`R()` 在这里过一次就够，九宫格内部按最终矩形算）。</summary>
        void BuildBar(Transform parent, PxRect r, float t01)
        {
            var f = R(r);
            BuildNine(parent, _win.Art("40k_generial_bar_empty"), f, 4, 12f, 12f,
                      new Color(1f, 0.59f, 0f, 1f), true);
            if (t01 <= 0.001f) return;
            var fill = new PxRect(f.x1, f.y1, f.x1 + f.W * Mathf.Clamp01(t01), f.y2);
            BuildNine(parent, _win.Art("40k_generial_bar_fill"), fill, 4, 12f, 12f,
                      new Color(1f, 0.77f, 0.33f, 1f), true);
        }

        void BuildNine(Transform parent, Texture2D tex, PxRect r, float border, float texW, float texH, Color tint, bool fillCenter)
        {
            if (tex == null) return;
            var go = ImageQuad.CreateNineSlice(parent, tex, new Vector4(border, border, border, border), texW, texH,
                                               RewardsWindow.Local(parent, r.x1, r.y1, r.x2, r.y2),
                                               LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), "Nine",
                                               new Vector4(border, border, border, border), fillCenter);
            if (go == null) return;
            foreach (var q in go.GetComponentsInChildren<ImageQuad>())
            {
                q.SetTint(tint);
                q.SetRenderQueue(RewardsWindow.QContent);
            }
        }

        /// <summary>里程碑格。🔴 图在**脚本字段**里：`activeSprite`/`disabledSprite` = `40k_missions_milestone_on`/`_off`；
        /// 色：已达成 **(28,235,26,1)** / 未达成 **(236,218,159,1)**（0–255 量级，正本 §三·7）。
        /// ⚠️ 原版这两格的 `Image` 实测 `Simple + PreserveAspect=1` ⇒ **等比**（67×66 的圆不会被拉成蛋）。</summary>
        void BuildMilestone(Transform parent, PxRect r, bool done, bool small)
        {
            var art = done ? "40k_missions_milestone_on" : "40k_missions_milestone_off";
            var col = done ? new Color(28f / 255f, 235f / 255f, 26f / 255f, 1f)
                           : new Color(236f / 255f, 218f / 255f, 159f / 255f, 1f);
            Draw(parent, art, r, "Milestone" + (done ? "_on" : "_off"), RewardsWindow.QContent, col, true);
        }

        /// <summary>奖励格 `Reward Display Mission Vertical Variant`（`MissionRewardItem`）。
        /// ⚠️ 原版这一格是 `Icon Container Drawer Variant` + 1080² 的内容做 `UIScaleToFit`；
        /// 我们画**抽屉图标 + 数量**（正本 §三·8），不引入那套缩放机制（图标按 `keepAspect` 等比放进去）。
        /// 🔴 **2026-10-05（B4）**：`art` / `countText` 改成**由调用方显式传**（原来传的是 `index`，
        /// 落在一张**按下标查**的公共表 `DailyData.RewardIcon/RewardCount` 上）。那张表**已删** ——
        /// 它和**领取**用的「任务那一份」对不上（第 3 行画骷髅 ×150、实发金块 ×200）。
        /// 现在三个调用点各自传**它自己那一份**：每日行 = 那条任务 · 登录卡 / 骷髅卡 = 各自的奖励源。
        /// `key` 只用于节点命名（`Reward &lt;key&gt;` / `count &lt;key&gt;`，名字沿用旧口径 —— 自检按名字找）。</summary>
        void BuildRewardCell(Transform parent, PxRect r, string art, string countText, string key)
        {
            // `drawerHolder`  N(…, a=(0,0)-(1,1) p=(.5,1) pos=(0,0) sz=(**−35.685, −42.369**))
            // 🔴 **2026-09-23 修**：原来这里用的是**我们自己挑的百分比**（`0.1/0.9` 与 `0.08/0.78`）——
            //    铁律 3 明令不许用「我们挑的」冒充原版。实测
            //    （`menu_rect.py "Daily Mission Container" --depth 4 --relative --root-size 539.188x150`）：
            //    格 126.334×150 里 `drawerHolder` = **17.84..108.49 × 0..107.63**（原来我们画的是 12.63..113.70 × 12..117）。
            var dh = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, new Vector2(0.5f, 1f),
                                    Vector2.zero, new Vector2(-35.685f, -42.369f));
            Draw(parent, art, dh, "Reward " + key, RewardsWindow.QContent, null, true);
            // `count`  N(7, 0,0, 1,0.337, 0.5,0, 0,0.6025, 0,0)  → 文本 fs40（实算 0..126.33 × 98.85..150 ✓ 与我们一致）
            var c = UguiRect.Child(r, UguiRect.A00, new Vector2(1f, 0.337f), new Vector2(0.5f, 0f),
                                   new Vector2(0f, 0.6025f), Vector2.zero);
            Txt(parent, c, countText, Color.white, "count " + key, 40f);
        }

        /// <summary>`40K_button` 底的按钮。🔴 实测这几处的 `Image` 都是 **`m_PreserveAspect = 1`**
        /// （`40K_button` 源图 489×107；骷髅卡那个框 187.47×80.49 ⇒ 原版画出来只有 **187.47×41.02**，我们原来画满 80.49）。
        /// ⚠️ 每日行那个是 `type=Sliced`（源图 border 已是半图宽 ⇒ 等价于四象限拉伸，与整体拉伸几乎同形），
        /// 三张卡上是 `type=Simple`；**两者我们都按「等比放进框、居中」处理** —— 前者记在
        /// `资料/日常_画面逐项对_0923.md` 的「还没查清的」里。</summary>
        void BuildButton(Transform parent, PxRect r, string art, Color tint, string label, float fontPx, string name,
                         System.Action onClick)
        {
            var q = Draw(parent, art, r, name, RewardsWindow.QContent, tint, true);
            if (q != null)
            {
                var hit = q.gameObject.AddComponent<WindowButton>();
                hit.onClick = onClick;
                // 🆕 2026-10-03 A17：原版这几颗 `Generic UI Button` 都是 SpriteSwap（普查 §块 4 第 1、3、4、5 行）
                // —— 按钮就挂在那张图上 ⇒ `BindSelf` 直接绑自己（`40K_button` → `40K_button_hover`）
                hit.BindSelf(art);
            }
            // `Button Text`  N(2, 0,0, 1,1, .5,.5, 0,0, -14,0)  → 文本 fs35 白居中
            var t = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero,
                                   new Vector2(-14f, 0f));
            Txt(parent, t, label, Color.white, name + " Text", fontPx);
        }

        /// <summary>「时钟 + 时间」一行（原版 `TimerHolder`：`WF_icon_clock` + 文本）。</summary>
        void BuildClockRow(Transform parent, PxRect r, float clockPx, string text, float fontPx)
        {
            float cy = r.CY;
            float cx1 = r.x1;
            Draw(parent, "WF_icon_clock",
                 new PxRect(cx1, cy - clockPx * 0.5f, cx1 + clockPx, cy + clockPx * 0.5f), "clock",
                 RewardsWindow.QContent);
            // ⚠️ 文字用 `TextBox`（限宽 + 自适应）—— 原版 `TimerHolder` 只有 157.9px 宽，
            //    而 'Resets in 12h 34 m'（fs30.15）≈250px ⇒ 不限宽就会**压到左边的计数格上**
            //    （2026-09-23 并排看图发现；断言量的是矩形，量不到字溢出）。
            var box = new PxRect(cx1 + clockPx + 6f, r.y1, r.x2, r.y2);
            Txt(parent, box, text, new Color(0.57f, 0.57f, 0.59f, 1f), "Timer", fontPx, 12f);
        }
    }
}
