// MissionsTab.cs — 「日常」任务页（`Missions Tab`）· 三种任务卡 + 头部 + 周常
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §三。**每一个矩形的锚点五元组都是原版 JSON 原文**，
// 由 `工具/menu_rect.py <pid> --cs` 机械吐出来、直接贴进来的（不手抄 —— 手抄锚点五元组出过一次静默的版面 bug）。
// 卡内版面按 `UguiRect.Child` 逐层算，**不是写死坐标**：所以卡被布局撑宽/撑窄时，内部会照原版规则重分布。
//
// 🔴 **四条纪律**：
//   ① **出厂 `activeSelf=false` 的件不建**（`title` / `ray target` / 各 `debug_buttons` / `body.description`…）
//      —— 原版是运行时按状态开的，见每行的 `// 出厂 inactive`。
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

            // 特殊任务区：两个子件都在 `Special Missions` 里，而它 scl **1.15** ⇒ 全部绕它的**左上角**缩放
            var loginRect = ScaleAbout(UguiRect.Child(sm, DL_A0, DL_A1, DL_P, DL_Pos, DL_Sz), sm.x1, sm.y1, SM_Scale);
            BuildLoginCard(smNode, loginRect);

            // `Daily Skulls Mission Container`（页内那份）尺寸是 0×0（靠 `FlexibleLayoutSizeOption` 运行时定）
            // ⇒ 用**独立预制体 `Daily Skulls Mission Container Small`** 的实尺 336×277.5（正本 §三·4）。
            var skullLocal = UguiLayout.HorizontalChild(sm, 336f, 277.5f, 1, 0f, 11.01f);   // HLG sp **11.01**
            BuildSkullsCard(smNode, ScaleAbout(skullLocal, sm.x1, sm.y1, SM_Scale));

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

            // `description`  N(1, 0,1, 0.986689,1, .5,.5, 68.4878,-43.873, -136.374,62.253)
            var desc = UguiRect.Child(row, new Vector2(0f, 1f), new Vector2(0.986689f, 1f), UguiRect.P50c,
                                      new Vector2(68.4878f, -43.873f), new Vector2(-136.374f, 62.253f));
            // 原版那条 TMP 实测：`m_fontSize 35 · m_TextWrappingMode 1 · m_enableAutoSizing 1 · min 15`
            _win.TextBox(parent, desc, DailyData.DailyDesc(index), Color.white, "description", 35f, 15f);

            // `timer`  N(1, 0,1, 1,1, .5,.5, 3.13226,-43.873, -267.097,62.253)   右对齐 · 白 α0.59
            var tim = UguiRect.Child(row, new Vector2(0f, 1f), new Vector2(1f, 1f), UguiRect.P50c,
                                     new Vector2(3.13226f, -43.873f), new Vector2(-267.097f, 62.253f));
            var tl = _win.TextBox(parent, tim, DailyData.DailyTimer(index),
                                  new Color(1f, 1f, 1f, 0.59f), "timer", 35f, 15f);
            if (tl != null) tl.AlignRightOn(LayoutSpace.FromPixel(tim.x2, 0f).x);
            // ⚠️ 原版 `MissionTimerDisplay(displayRule=2)` 说明这行是**运行时填**的剩余时间；我们填本地数据

            // `Separator Line`  N(1, 0,0, 0,1, 1,0.5, 122.062,-0.0370026, 1.60199,-3.049)  无 sprite，只有色
            var sep = UguiRect.Child(row, UguiRect.A00, new Vector2(0f, 1f), new Vector2(1f, 0.5f),
                                     new Vector2(122.062f, -0.0370026f), new Vector2(1.60199f, -3.049f));
            _win.Rect(parent, null, sep.x1, sep.x2, sep.y1, sep.y2, "Separator Line", RewardsWindow.QContent,
                      new Color(0.25f, 0.25f, 0.41f, 0.59f));

            // `Rewards`  N(1, 0,0, 0,0, 0,0, 3.05e-05,0, 126.334,150)  → `Reward Display Mission Vertical Variant`
            var rew = UguiRect.Child(row, UguiRect.A00, UguiRect.A00, UguiRect.P00,
                                     new Vector2(3.05176e-05f, 0f), new Vector2(126.334f, 150f));
            BuildRewardCell(parent, rew, index);

            // `Mission Milestones Progress Bar`  N(1, 0,0.5, 0.316,0.5, .5,.5, 98.02,-29.026, -77.3111,51.8301)
            var mmpb = UguiRect.Child(row, new Vector2(0f, 0.5f), new Vector2(0.316f, 0.5f), UguiRect.P50c,
                                      new Vector2(98.02f, -29.026f), new Vector2(-77.3111f, 51.8301f));
            //   └ `Progress Bar`  N(2, 0,0, 1,0.33, 0,1, 5.5,-5.5, -5.5,-5.5)  bg/fill 都是 `40k_generial_bar_*` 九宫格
            var pb = UguiRect.Child(mmpb, UguiRect.A00, new Vector2(1f, 0.33f), new Vector2(0f, 1f),
                                    new Vector2(5.5f, -5.5f), new Vector2(-5.5f, -5.5f));
            BuildBar(RewardsWindow.Node(parent, "Progress Bar", pb), pb, DailyData.DailyProgress01(index));
            //   └ `progress`  N(2, 0,0.33, 1,1, 0,0, 0,-3, 0,6)  文本 `52/500` fs35 色 (1,0.77,0.33,1)
            var pt = UguiRect.Child(mmpb, new Vector2(0f, 0.33f), UguiRect.A11, UguiRect.P00,
                                    new Vector2(0f, -3f), new Vector2(0f, 6f));
            _win.Text(parent, DailyData.DailyCounter(index), pt.x1, pt.x2, pt.y1, pt.y2, 4,
                      new Color(1f, 0.77f, 0.33f, 1f), "progress", 35f);

            // `Generic UI Button`  N(1, 1,0, 1,0, .5,.5, -145.3,40.7107, 254.611,56.4767)   `40K_button` 色 (1,0.53,0,1) type=1
            var btn = UguiRect.Child(row, UguiRect.A10, UguiRect.A10, UguiRect.P50c,
                                     new Vector2(-145.3f, 40.7107f), new Vector2(254.611f, 56.4767f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.53f, 0f, 1f), "Collect", 35f, "Generic UI Button",
                        () => DailyData.CollectDaily(index));

            // `Trash mission`  N(1, 1,0, 1,0, .5,.5, -307.44,40.711, 49.104,49.368)   `40k_general_bt_yellow` 色 (1,0.77,0.33,1)
            var trash = UguiRect.Child(row, UguiRect.A10, UguiRect.A10, UguiRect.P50c,
                                       new Vector2(-307.44f, 40.711f), new Vector2(49.104f, 49.368f));
            _win.Rect(parent, "40k_general_bt_yellow", trash.x1, trash.x2, trash.y1, trash.y2, "Trash mission",
                      RewardsWindow.QContent, new Color(1f, 0.77f, 0.33f, 1f));
            //   └ `Image` = `40k_general_bt_yellow_delete`（`Button Text` 'X' 出厂 inactive ⇒ 不建）
            var ti = UguiRect.Child(trash, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                    new Vector2(-1f, 0f), new Vector2(-2f, -2f));
            _win.Rect(parent, "40k_general_bt_yellow_delete", ti.x1, ti.x2, ti.y1, ti.y2, "Image", RewardsWindow.QOverlay);
        }

        /// <summary>`Daily Login Bonus Container`（竖卡 334×555）。</summary>
        public void BuildLoginCard(Transform parent, PxRect card)
        {
            parent = RewardsWindow.Node(parent, "Daily Login Container", card);
            _win.Rect(parent, "40K_missions_display_Daily_vertical", card.x1, card.x2, card.y1, card.y2,
                      "Daily Login Bonus Container", RewardsWindow.QPanel);
            BuildCardHeader(parent, card, "Daily Login Bonus", DailyData.LoginTitle(), 30f);

            // `body.image`  N(3, 0,0, 1,1, .5,0, 0,-29, 0,0)  → `40K_missions_icon_login bonus`
            var body = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-0.0018845f, 84.7947f), new Vector2(325f, 261.131f));
            var img = UguiRect.Child(body, UguiRect.A00, UguiRect.A11, UguiRect.P50, Vector2.zero, Vector2.zero);
            var q = _win.Rect(parent, "40K_missions_icon_login_bonus", img.x1, img.x2, img.y1, img.y2,
                              "image", RewardsWindow.QContent);
            if (q != null) { q.SetAspect(262f / 212f); }        // 图 262×212（正本 §九）

            // `footer.Rewards`  N(3, .5,.5, .5,.5, .5,.5, 0,52.108, 325,77.643)  两个奖励格（HLG sp 0）
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(-1.5201f, -153.84f), new Vector2(325f, 181.86f));
            var rw = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(0f, 52.108f), new Vector2(325f, 77.643f));
            for (int i = 0; i < 2; i++)
                BuildRewardCell(parent, new PxRect(rw.x1 + rw.W * 0.5f * i, rw.y1, rw.x1 + rw.W * 0.5f * (i + 1), rw.y2), i);

            // `footer.Generic UI Button`  N(3, …, 3.1692,-24.0231, 255.992,74.6201)  `40K_button` 色 (1,0.47,0.10,1)
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(3.1692f, -24.0231f), new Vector2(255.992f, 74.6201f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 35f, "Generic UI Button",
                        () => DailyData.CollectLogin());

            // `footer.TimerHolder`  N(3, 0,0.5, 1,0.5, .5,0, 0,-118.5, 0,57.167)   文本 'Resets in …' fs28 灰
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(0f, -118.5f), new Vector2(0f, 57.167f));
            var tl = _win.Text(parent, DailyData.ResetIn(), th.x1, th.x2, th.y1, th.y2, 4,
                               new Color(0.5686f, 0.5686f, 0.5882f, 1f), "Timer", 28f);
            
        }

        /// <summary>`Daily Skulls Mission Container Small`（336×277.5）—— 5 格里程碑 + 计数 + 领奖。
        /// ⚠️ 页内那份实例的 `body`/`progress` 尺寸与独立预制体**不同**（正本 §三·4 vs 页内实例）；
        /// 我们照**独立预制体 Small**（那套尺寸是确定的）。</summary>
        public void BuildSkullsCard(Transform parent, PxRect card)
        {
            parent = RewardsWindow.Node(parent, "Daily Skulls Mission Container", card);
            _win.Rect(parent, "40K_missions_display_Daily_vertical", card.x1, card.x2, card.y1, card.y2,
                      "Daily Skulls Mission Container", RewardsWindow.QPanel);
            BuildCardHeader(parent, card, "Daily Skulls", DailyData.SkullsTitle(), 36f);

            // `progress.milestones`  N(3, 0,0, 1,1, .5,.5, 0,0, ~0,~0)  → `steps` HLG **spacing 20** align 4(MiddleCenter)，每格 40×40
            var prog = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-0.0018959f, -21.313f), new Vector2(325f, 79.992f));
            var ms = UguiRect.Child(prog, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero, Vector2.zero);
            for (int i = 0; i < 5; i++)
                BuildMilestone(parent, UguiLayout.HorizontalChild(ms, 40f, 40f, i, 0f, 20f), DailyData.SkullsStepDone(i), true);

            // `footer.Rewards`  N(3, …, -103.7,14.204, 109.25,47.433)  → `40K_missions_icon_Daily skulls` + 'x160'
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(-1.5201f, -100.83f), new Vector2(325f, 75.84f));
            var rw = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(-103.7f, 14.204f), new Vector2(109.25f, 47.433f));
            BuildRewardCell(parent, rw, 1);
            // `footer.counter`  N(3, …, -79.2,150.3, 167.6,59.925)  → 阵营图标(60) + 骷髅(65) + `x160` fs26.8
            var cnt = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(-79.2f, 150.3f), new Vector2(167.6f, 59.925f));
            _win.Rect(parent, "40K_missions_icon_Daily_skulls", cnt.x1, cnt.x1 + 65f, cnt.y1, cnt.y2,
                      "skull", RewardsWindow.QContent);
            _win.Text(parent, DailyData.SkullsCounter(), cnt.x1 + 70f, cnt.x2, cnt.y1, cnt.y2, 4,
                      Color.white, "counter text", 26.8f);

            // `footer.Generic UI Button`  N(3, …, 58,13.548, 187.467,80.492)  `40K_button` 色 (1,0.47,0.10,1)
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(58f, 13.548f), new Vector2(187.467f, 80.492f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 34.05f, "Generic UI Button",
                        () => DailyData.CollectSkulls());

            // `footer.TimerHolder`  N(3, 0,0.5, 1,0.5, .5,0, 83.55,120.34, -167.1,59.926)  时钟 + 时间
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(83.55f, 120.34f), new Vector2(-167.1f, 59.926f));
            BuildClockRow(parent, th, 44.74f, DailyData.ResetIn(), 30.15f);
        }

        /// <summary>`Weekly Mission Container`（1518.99×227.51）· 4 个 70² 里程碑 + 进度条 + `Ends in`。</summary>
        public void BuildWeekly(Transform parent, PxRect card)
        {
            parent = RewardsWindow.Node(parent, "Weekly Mission Container", card);
            _win.Rect(parent, "40K_missions_display_Weekly", card.x1, card.x2, card.y1, card.y2,
                      "Weekly Mission", RewardsWindow.QPanel);

            // `header`  N(2, 0,1, 0.25,1, .5,1, 0,0, 0,55) → `name` 'Weekly Challenge' fs36
            var head = UguiRect.Child(card, new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0.5f, 1f),
                                      Vector2.zero, new Vector2(0f, 55f));
            _win.Text(parent, "Weekly Challenge", head.x1, head.x2, head.y1,
                      head.y1 + 50f, 5, Color.white, "name", 36f);

            // `progress.Mission Progress Bar`  N(3, 0,0.5, 1,0.5, 0,0.5, 30,-9.6, -60,22.766)
            var prog = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-185.175f, -20.537f), new Vector2(1068.43f, 158.59f));
            var bar = UguiRect.Child(prog, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                                     new Vector2(30f, -9.6f), new Vector2(-60f, 22.766f));
            BuildBar(parent, bar, DailyData.WeeklyProgress01());
            //   └ `Handle` 上有 counter `13/15` fs33.15 色 (0.92,0.77,0.48,1)
            var hd = UguiRect.Child(prog, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                    new Vector2(0f, -6.10352e-05f), Vector2.zero);
            _win.Text(parent, DailyData.WeeklyCounter(), hd.x1, hd.x2, hd.y1, hd.y2, 4,
                      new Color(0.92f, 0.77f, 0.48f, 1f), "counter", 33.15f);

            // `Mission Milestones Progress.steps`  N(4, 0,0, 1,1, 0,0.5, 0,47, 0,0)
            //   `EverguildLayoutGroup` spacing **262.81** align 4 ⇒ 4 格 70²，从容器左边起排
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
                        () => DailyData.CollectWeekly());

            // `TimerHolder.Timer`  N(4, 0,0.5, 1,0.5, .5,.5, 0,31.287, 100,57.167)  'Ends in …' fs38 灰
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(0f, -118.5f), new Vector2(0f, 57.167f));
            var tm = UguiRect.Child(th, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UguiRect.P50c,
                                    new Vector2(0f, 31.287f), new Vector2(100f, 57.167f));
            _win.Text(parent, DailyData.WeeklyEndsIn(), tm.x1, tm.x2, tm.y1, tm.y2, 4,
                      new Color(0.57f, 0.57f, 0.59f, 1f), "Timer", 38f);
        }

        /// <summary>`Mission Header`：标题条（`Daily Missions` 那一条）。
        /// `isSkulls` 只是文案不同（原版同一份 prefab 换了 `name`/`Refill Counter` 的文本）。</summary>
        public void BuildMissionHeader(Transform parent, PxRect hostRect, bool isSkulls)
        {
            // N(3, 0,1, 1,1, .5,1, 1.34,-2.2287, -2.6799,52.7713)
            var h = UguiRect.Child(hostRect, new Vector2(0f, 1f), UguiRect.A11, new Vector2(0.5f, 1f),
                                   new Vector2(1.34f, -2.2287f), new Vector2(-2.6799f, 52.7713f));
            // `name`  N(3, 0.03,0.5, 0.84,0.5, .5,.5, 0,0, ~0,50)   'Daily Missions' fs36
            var nm = UguiRect.Child(h, new Vector2(0.03f, 0.5f), new Vector2(0.84f, 0.5f), UguiRect.P50c,
                                    Vector2.zero, new Vector2(3.8147e-06f, 50f));
            _win.TextBox(parent, nm, isSkulls ? "Daily Skulls" : "Daily Missions", Color.white,
                         "name (Mission Header)", 36f, 12f);
            // `info`  N(3, 1,0.5, 1,0.5, 1,0.5, -10,0, 41,41)   `40K_generic_bt_info` 41²
            var inf = UguiRect.Child(h, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                     new Vector2(-10f, 0f), new Vector2(41f, 41f));
            _win.Rect(parent, "40K_generic_bt_info", inf.x1, inf.x2, inf.y1, inf.y2, "info", RewardsWindow.QContent);
            // `Refill Counter`  N(3, 0,0.5, 1,0.5, .5,.5, -26.93,0, -53.86,50)   '0 Disponible' fs36
            var rc = UguiRect.Child(h, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UguiRect.P50c,
                                    new Vector2(-26.93f, 0f), new Vector2(-53.86f, 50f));
            var rcT = _win.TextBox(parent, rc, DailyData.RefillText(), Color.white, "Refill Counter", 36f, 12f);
            if (rcT != null) rcT.AlignRightOn(LayoutSpace.FromPixel(rc.x2, 0f).x);
        }

        // ============================================================ 小件

        /// <summary>卡头：`header`（`40K_generic_bt_info` 42×42 在右）+ `name`。</summary>
        void BuildCardHeader(Transform parent, PxRect card, string what, string title, float fontPx)
        {
            // N(2, 0,1, 1,1, .5,1, 1.34,-2.2287, -2.6799,52.7713)
            var h = UguiRect.Child(card, new Vector2(0f, 1f), UguiRect.A11, new Vector2(0.5f, 1f),
                                   new Vector2(1.34f, -2.2287f), new Vector2(-2.6799f, 52.7713f));
            var nm = UguiRect.Child(h, new Vector2(0.03f, 0.5f), new Vector2(0.84f, 0.5f), UguiRect.P50c,
                                    Vector2.zero, new Vector2(3.8147e-06f, 50f));
            _win.TextBox(parent, nm, title, Color.white, what + " name", fontPx, 12f);
            var inf = UguiRect.Child(h, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                     new Vector2(-10f, 0f), new Vector2(41f, 41f));
            _win.Rect(parent, "40K_generic_bt_info", inf.x1, inf.x2, inf.y1, inf.y2, what + " info",
                      RewardsWindow.QContent);
        }

        /// <summary>进度条：两张图都是**九宫格 `(4,4,4,4)`**、12×12（正本 §九）。底色 `(1,0.59,0,1)` · 填充 `(1,0.77,0.33,1)`。</summary>
        void BuildBar(Transform parent, PxRect r, float t01)
        {
            BuildNine(parent, _win.Art("40k_generial_bar_empty"), r, 4, 12f, 12f,
                      new Color(1f, 0.59f, 0f, 1f), true);
            if (t01 <= 0.001f) return;
            var fill = new PxRect(r.x1, r.y1, r.x1 + r.W * Mathf.Clamp01(t01), r.y2);
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
        /// 色：已达成 **(28,235,26,1)** / 未达成 **(236,218,159,1)**（0–255 量级，正本 §三·7）。</summary>
        void BuildMilestone(Transform parent, PxRect r, bool done, bool small)
        {
            var art = done ? "40k_missions_milestone_on" : "40k_missions_milestone_off";
            var col = done ? new Color(28f / 255f, 235f / 255f, 26f / 255f, 1f)
                           : new Color(236f / 255f, 218f / 255f, 159f / 255f, 1f);
            _win.Rect(parent, art, r.x1, r.x2, r.y1, r.y2, "Milestone" + (done ? "_on" : "_off"),
                      RewardsWindow.QContent, col, true);
        }

        /// <summary>奖励格 `Reward Display Mission Vertical Variant`（`MissionRewardItem`）。
        /// ⚠️ 原版这一格是 `Icon Container Drawer Variant` + 1080² 的内容做 `UIScaleToFit`；
        /// 我们画**抽屉图标 + 数量**（正本 §三·8），不引入那套缩放机制。</summary>
        void BuildRewardCell(Transform parent, PxRect r, int index)
        {
            string icon = DailyData.RewardIcon(index);
            _win.Rect(parent, icon, r.x1 + r.W * 0.1f, r.x1 + r.W * 0.9f, r.y1 + r.H * 0.08f, r.y1 + r.H * 0.78f,
                      "Reward " + index, RewardsWindow.QContent, null, true);
            // `count`  N(7, 0,0, 1,0.337, 0.5,0, 0,0.6025, 0,0)  → 文本 fs40
            var c = UguiRect.Child(r, UguiRect.A00, new Vector2(1f, 0.337f), new Vector2(0.5f, 0f),
                                   new Vector2(0f, 0.6025f), Vector2.zero);
            _win.Text(parent, DailyData.RewardCount(index), c.x1, c.x2, c.y1, c.y2, 4, Color.white,
                      "count " + index, 40f);
        }

        /// <summary>`40K_button` 底的按钮。原版这两个 Image 是 **Simple + preserveAspect**（不是 Sliced）。</summary>
        void BuildButton(Transform parent, PxRect r, string art, Color tint, string label, float fontPx, string name,
                         System.Action onClick)
        {
            var q = _win.Rect(parent, art, r.x1, r.x2, r.y1, r.y2, name, RewardsWindow.QContent, tint);
            if (q != null)
            {
                var hit = q.gameObject.AddComponent<WindowButton>();
                hit.onClick = onClick;
            }
            // `Button Text`  N(2, 0,0, 1,1, .5,.5, 0,0, -14,0)  → 文本 fs35 白居中
            var t = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero,
                                   new Vector2(-14f, 0f));
            _win.Text(parent, label, t.x1, t.x2, t.y1, t.y2, 5, Color.white, name + " Text", fontPx);
        }

        /// <summary>「时钟 + 时间」一行（原版 `TimerHolder`：`WF_icon_clock` + 文本）。</summary>
        void BuildClockRow(Transform parent, PxRect r, float clockPx, string text, float fontPx)
        {
            float cy = r.CY;
            float cx1 = r.x1;
            _win.Rect(parent, "WF_icon_clock", cx1, cx1 + clockPx, cy - clockPx * 0.5f, cy + clockPx * 0.5f,
                      "clock", RewardsWindow.QContent);
            // ⚠️ 文字用 `TextBox`（限宽 + 自适应）—— 原版 `TimerHolder` 只有 157.9px 宽，
            //    而 'Resets in 12h 34 m'（fs30.15）≈250px ⇒ 不限宽就会**压到左边的计数格上**
            //    （2026-09-23 并排看图发现；断言量的是矩形，量不到字溢出）。
            var box = new PxRect(cx1 + clockPx + 6f, r.y1, r.x2, r.y2);
            _win.TextBox(parent, box, text, new Color(0.57f, 0.57f, 0.59f, 1f), "Timer", fontPx, 12f);
        }
    }
}
