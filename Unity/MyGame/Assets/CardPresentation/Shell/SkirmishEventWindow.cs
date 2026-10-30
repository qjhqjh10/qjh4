// SkirmishEventWindow.cs — 阶段二第 3 层第 6 件：**`SkirmishModeEventWindow`（遭遇战）**
//   （原版 `SkirmishEventWindow : LiveOpsEventWindow<IFastModeEvent>`）
//
// ============================ 出处（唯一正本）============================
// `资料/阶段二_战斗入口_原版规格.md` **§一（窗口参数）+ §二 B（逐节点表）+ §三（开战链）**。
// 逐节点几何**共用部分**在 `LiveOpsEventWindow`（基类，见那边的文件头）；这里只写**遭遇战独有**的：
//   · 背景那一族（`Menu Dark Background` / `Reward Background Get Reward` / `Menu Vignette`）
//   · 左列 `Reward Display`（`RewardProgressBarPanel`：标题 + 进度条 + 胜利数）
//   · `Timer`（`TimerDisplay`）· `Banned card in deck`
//
// 🔴 **窗口参数**：`type=1 Popup` · `windowsPlacement=5 Canvas` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`
//    （与正本 §一 的表一致；⚠️ **`placement` 是 5 不是 15** —— 别照练习窗抄）。
//
// ============================ 🆕 2026-10-24（E15）：左列分两版 ============================
// 用户 2026-10-24 指出：「活动进度 / 说明 / 胜利数 / 5 档里程碑 / 收集 —— **那是以前活动的时候才有的**；
//   平常的时候，**应该和经典大概一样**」。判据（反编译 + 资源 + 关服前实机 log）→
//   `资料/普查产出_1024b/S1_小冲突非活动期界面.md`，三条硬事实：
//   ① 原版 `SkirmishEventWindow : LiveOpsEventWindow<IFastModeEvent>`（`dump.cs:63944`）**本身就是活动窗**，
//      `LiveOp` = 一场 FastMode 活动；**没有活动就没有这扇窗**（`TryOpen/RefreshScore` 拿空实例 = 抛）；
//   ② 它左列出厂就是 `Reward Display`，运行时**没有任何一句 `SetActive` 开关它**（只有活动期才有它）；
//   ③ 常驻/排名版走的是 `RankedEventWindowV2` 的**经典式左列** `Ranked Division Info`
//      （`RankingDisplay`，638×812 @0,146.9 —— 与本窗 `Reward Display` **同一个槽**）。
// ⇒ 本窗现在按 <see cref="HasActiveSkirmishEvent"/> 分叉：
//   · **true**（活动在期）⇒ 照旧 `BuildRewardDisplay`（= 原版 prefab 那一棵，一字未改）；
//   · **false**（本地现况）⇒ `BuildClassicLeftColumn`：`Ranked Division Info` 结构
//     （`Rank Title` + `DivisionText`/`DivisionImage` + 结束于 + `LeaderboardButton` + 未排名⟷排名 开关）。
//     ⚠️ **数值本地没有**（段位/名次/分数/结束时刻在服务器）⇒ 结构齐、留空、出声一次（同 R1/E12 口径）。
using TMPro;                       // 🆕 A1248：量 `GetPreferredValues()`（复刻原版 CSF）要 TMP 的类型
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`SkirmishModeEventWindow` —— 遭遇战活动窗。</summary>
    public class SkirmishEventWindow : LiveOpsEventWindow
    {
        // ---- 背景（本窗实测）----
        const float ShadeL = -1327.30f, ShadeT = -746.18f, ShadeR = 3247.30f, ShadeB = 1826.18f;
        const float RedL = 0f, RedT = 121.80f, RedR = 1920f, RedB = 1013.11f;
        const float VigL = 0f, VigT = 0f, VigR = 1920f, VigB = 1080f;
        static readonly Vector4 RedBorder = new Vector4(0f, 26f, 0f, 26f);   // 21×81 · border(0,26,0,26)
        const float RedTexW = 21f, RedTexH = 81f;

        // ---- 左列 `Reward Display` ----
        const float RDL = 0f, RDT = 146.93f, RDR = 638f, RDB = 959.07f;
        const float RTileL = 58f, RTileT = 164.25f, RTileR = 580f, RTileB = 218.94f;
        const float RHelpL = 22.61f, RHelpT = 215.93f, RHelpR = 615.39f, RHelpB = 316.92f;
        const float VictL = 21.60f, VictT = 316.92f, VictR = 568.23f, VictB = 407.37f;
        const float VTitleL = 15.20f, VTitleT = 337.15f, VTitleR = 251.83f, VTitleB = 387.15f;
        const float VSkullL = 258.22f, VSkullT = 325.45f, VSkullR = 331.61f, VSkullB = 398.84f;
        const float VCountL = 338.79f, VCountT = 337.15f, VCountR = 519.63f, VCountB = 387.15f;

        // ---- 记分条（`Scoring Bar Event Score Info`，scl=1.2563）----
        const float SBarL = 155.71f, SBarT = 467.85f, SBarR = 442.29f, SBarB = 842.28f;
        const float SBarScale = 1.2563f;
        /// <summary>`Scoring Bar` 的中心（换算是绕着**它**缩的）。</summary>
        const float SBarCx = 299f, SBarCy = 655.065f;
        /// <summary>`Progress Bar` 自己的 `m_LocalScale`（0.8696）与它的中心（与上面几乎重合）。</summary>
        const float ProgrScale = 0.8695655f, ProgrCy = 655.26f;
        /// <summary>`Score Levels` VLG（spacing 0 · pad 0 · **align MiddleCenter** · 🔴 **`reverse=1`**）——
        /// 5 行**从下往上**排：树序第一颗（`Level 1`）在最**下**、最后一颗（`Level 5`）在最**上**。
        /// <para>🔴 **2026-10-17（N31）就地订正（铁律 5）**：这里原来写「5 行 60.48 高居中叠」、并把
        /// `LevelRowH` **同时当行高与行距**用 ⇒ 两处都错：
        /// ① **镜像**：`reverse=1` = 主轴按树序倒排（uGUI `HorizontalOrVerticalLayoutGroup.SetChildrenAlongAxis`
        ///    `startIndex = reverse ? Count−1 : 0` · `increment = reverse ? −1 : 1`）—— 原来 `i=0` 画在最上
        ///    ⇒ **整条记分条上下颠倒**（Iron…Warp 五档箱子反了）。
        /// ② **行距 ≠ 行高**：现读五行中心在屏上相距 **81.375**、行框高 **75.98** ⇒ 设计 px 行距 **64.773**、
        ///    行高 **60.48**（差 4.29 = 布局给每行的槽比行框高，⛔ 别拿行高当行距）。
        /// 判据 = `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow"
        /// --depth 10 --relative --no-sprite` 现读：`Level 1` = `y 731.9..807.9`、`Level 5` = `406.4..482.4`
        /// （两行的 x 逐位相同 = `113.3..484.7`）。
        /// 换算（`ScoreRect` 绕 `Scoring Bar` 中心乘 1.2563）：`Level 1` 中心（设计）
        /// = `655.065 + (769.9 − 655.065) ÷ 1.2563 = **746.47**`；行距 = `81.375 ÷ 1.2563 = **64.773**`
        /// （行高 = `75.98 ÷ 1.2563 = 60.48`）。同形写法见兄弟件 `Shell/EnergySinglePlayerOnlyEventWindow.cs`
        /// 的 `LvlTop0/LvlStep` 与 `Shell/AllianceEventScoreInfo.cs` 的 `LvlCy0/LvlStep`。</para></summary>
        const float LevelRowH = 60.48f, LevelStep = 64.773f, LevelX1 = 151.19f, LevelX2 = 446.81f;
        /// <summary>树序第一颗（`Level 1`，排在最**下**那一档）的行中心 y（设计 px —— `ScoreRect` 那一帧）。
        /// ⚠️ 行号越**大**越靠**上**（y 越小，见 `ScoreLevelRowCy`）。</summary>
        const float LevelCy1 = 746.47f;
        const float SkullDx = -46.4f, ScoreDx = -93.7f, ChestDx = 70.2f, HiCrateDx = 68.5f, HiCrateDy = -1.3f;
        const float SkullSide = 59.06f, ScoreW = 116.15f, ScoreH = 50f, ChestW = 91.03f, ChestH = 81.20f;
        const float HiCrateW = 150.86f, HiCrateH = 134.43f, HiCrateScale = 0.796f, ChestScale = 0.8696f;
        /// <summary>🆕 **2026-10-17（A1221）**：`Score` 那 5 颗的**祖先刻度积** = `Scoring Bar` 1.2563 ×
        /// `Skull` 0.8695655 = **1.0924350**（判据 → 本文件 `BuildMilestones` 上方「A1197」那一段，
        /// 那一节当时只落了注释、没建字；本次按它把字建上）。四格字号**一起乘**、框不动（`ScoreRect` 已是屏值）。</summary>
        const float ScoreFontScale = 1.0924350f;
        /// <summary>🆕 **2026-10-17（A1221）**：`Score` 那一颗在**行内**的相对矩形（屏值，左上原点）。
        /// 判据 = `menu_dump bundle_menus_assets_all "SkirmishModeEventWindow" --depth 8` 现读
        /// （Level 1 / Level 5 两行逐位相同 ⇒ 行内偏移是常量）：
        /// · 行 `Score Bar Line Level k` = `113.3..484.7 × 731.9..807.9`（Level 1）；
        /// · 行内 `Skull/Score` = `74.9..201.8 × 742.6..797.2`；Level 5 = `74.9..201.8 × 417.1..471.8`
        ///   （行 `113.3..484.7 × 406.4..482.4`）
        /// ⇒ 相对行左上角：`x −38.43 … +88.48`、`y +10.70 … +65.30`（两行逐位可验）。</summary>
        const float ScoreOffL = -38.43f, ScoreOffR = 88.48f, ScoreOffT = 10.70f, ScoreOffB = 65.30f;
        static readonly string[] CrateArt =
        { "40k_Crate_Tier1_Iron", "40k_Crate_Tier2_Copper", "40k_Crate_Tier3_Silver",
          "40k_Crate_Tier4_Gold", "40k_Crate_Tier5_Warp" };

        /// <summary>`Timer`（本窗有；⚠️ **排位窗没有这一件** —— 树里实测）。</summary>
        const float TmL = 648.33f, TmT = 1008.55f, TmR = 1271.67f, TmB = 1069.45f;
        /// <summary>🆕 **2026-10-17（A1248）**：`Timer` 组的 HLG 子件几何。
        /// 判据 = `menu_dump bundle_menus_assets_all "SkirmishModeEventWindow" --depth 6`（**本 prefab 跑过布局**，
        /// 所以这两行是**运行期真值**，不是模板位）：`Timer Icon 813.8..857.7 × 1019.5..1063.5`（**43.91 宽**）·
        /// `Timer`（TMP，`'Termina en: 23d 5h'`）`857.7..1106.2 × 1013.5..1069.5`（**248.44 宽**）。
        /// HLG 实读：`spacing 0 · align 4 (MiddleCenter) · pad (0,0,5,0)` ⇒ `start = 648.33 + (623.34 − 43.91 − 文字宽)/2`
        /// （代入 248.44 ⇒ **813.83**，与现读逐位吻合）。</summary>
        const float TmIconW = 43.91f, TmIconT = 1019.55f, TmIconB = 1063.45f;
        const float TmTextT = 1013.55f, TmTextB = 1069.45f;
        /// <summary>原版字体的 preferred width（prefab 跑过布局那一份）。**只当量不到时的兜底**。</summary>
        const float TmPrefabTextW = 248.44f;

        /// <summary>🆕 **2026-10-17（A1221）**：记分条上那颗 `Collect` 钮（`Generic Simplified UI Button`）。
        /// 判据 = 同一份 dump：节点 `155.4..442.6 × 902.8..989.3`、`Button Text 165.4..432.7 × 911.2..980.8`
        /// （`⇲ls=1.256` ⇒ 这两组已是屏值；字号四格见 `BuildMilestones` 末尾那段，要 × `SBarScale`）。</summary>
        static readonly PxRect CollectR   = new PxRect(155.4f, 902.8f, 442.6f, 989.3f);
        static readonly PxRect CollectTxR = new PxRect(165.4f, 911.2f, 432.7f, 980.8f);
        const float BannedL = 1241.78f, BannedT = 1024.60f, BannedR = 1920.08f, BannedB = 1078.54f;

        // ---- 🆕 2026-10-24（E15）：经典式左列 `Ranked Division Info`（非活动期那一版）----
        // 🔑 **本窗的 `Reward Display` 与 V2 的 `Ranked Division Info` 是同一个槽**
        //    （`0,146.93 → 638,959.07`，S1 报告 §①·3 现读 `RankingDisplay` = 638×812 @0,146.9；
        //     本文件 `RDL/RDT/RDR/RDB` 逐位同值）⇒ **V2 那一批绝对画布坐标可以逐位复用**。
        //    判据（V2 侧）= `Shell/RankedEventWindow.cs` 的 `RankTitle*` / `Leader*` / `Tg*` / `Tmr*` 那几组常量
        //    （那边写明了 `menu_dump bundle_menus_assets_all -5324570929900320848 --depth 6 --md` 的现读，
        //     且 `Content/Timer` 那三行是**跑过布局之后**的真值）。
        //    ⚠️ 那一批常量在 `Shell/RankedEventWindow.cs` 里是 `private` ⇒ 这里**抄数值、不抄第二份语义**：
        //     任何一处要改，两扇窗一起改（本文件这几行的注释就是交叉索引）。
        const float RankTitleL = 58f, RankTitleT = 161.33f, RankTitleR = 580f, RankTitleB = 216.02f;
        const float LeaderL = 140.46f, LeaderT = 965.97f, LeaderR = 497.54f, LeaderB = 1040.82f;
        const float LeaderTxL = 156.14f, LeaderTxT = 973.30f, LeaderTxR = 480.71f, LeaderTxB = 1033.48f;
        /// <summary>`ChangeRankedToggle`（`ToggleDisplay`）—— 【Unranked 字】【开关】【Ranked 字】三件并排。</summary>
        const float TgL = 191.45f, TgT = 868.12f, TgR = 446.55f, TgB = 921.97f;
        const float TgRankedL = 415.10f, TgRankedT = 868.12f, TgRankedR = 671.50f, TgRankedB = 920.52f;
        const float TgUnrankedL = 12.30f, TgUnrankedT = 868.12f, TgUnrankedR = 268.70f, TgUnrankedB = 920.52f;
        const float TgKnobL = 296.30f, TgKnobT = 875.04f, TgKnobR = 385.30f, TgKnobB = 915.04f;
        /// <summary>`Ranked Division Info/Content/Timer`（= `RankingDisplay.timerDisplay` 那一支）。
        /// ⚠️ 三行都是 **HLG 排完之后**的落点（`Content` 是 VLG），⛔ 别拿 `menu_rect` 的模板位。</summary>
        const float TmrL = 7.33f, TmrT = 785.75f, TmrR = 630.67f, TmrB = 846.65f;
        const float TmrIcL = 172.83f, TmrIcT = 796.75f, TmrIcR = 216.73f, TmrIcB = 840.65f;
        const float TmrTxL = 216.73f, TmrTxT = 790.75f, TmrTxR = 465.17f, TmrTxB = 846.65f;
        /// <summary>`40k_menu_bt`：**47×47 · border (10,10,10,10)**（`Sprite/*.json` 实测，同 V2）。</summary>
        static readonly Vector4 MenuBtBorder = new Vector4(10f, 10f, 10f, 10f);
        const float MenuBtTexW = 47f, MenuBtTexH = 47f;
        const string ArtMulligan = "UI_Button_Mulligan";   // 410×124 · 九宫 (333,96,333,96)

        public static SkirmishEventWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("SkirmishModeEventWindow");
            return Init(go.AddComponent<SkirmishEventWindow>(), mgr);
        }

        // ============================================================ 背景
        protected override void BuildBackdrop(Transform root)
        {
            // ① 整屏压暗（原版 `Menu Dark Background`，rect 比屏幕大）+ 点它关窗（`BackgroundCloseButton`）
            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(ShadeL, ShadeT, ShadeR, ShadeB),
                          "Menu Dark Background", QBg, new Color(0f, 0f, 0f, 0.773f));
            // 🔴 **背板那一下必须比其他命中区低**（同队列时点 `Battle!` 会被判成点背景 ⇒ 直接关窗）
            // 🆕 **2026-10-04（A47 接线批）订正档号 + 收口公共件**：原来用 `QHitBackdrop`(3115)，
            //   那是「内容档再往上留一档」的写法；规矩是**压暗层的命中区落在压暗层自己那一档**
            //   （`QBg` = 3104），且严格低于本窗内容命中区最低档（`QHit` = 3116）
            //   ⇒ 改走 `MenuDraw.ShadeHit`（判据 → 它的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QBg, QHit, () => CloseButtonClick(), "BackdropHit");
            // ② `Reward Background Get Reward`（`40k_general_popup_simple red` Sliced）—— **比压暗高一档**
            MenuDraw.Nine(root, Tex(ArtPopupRed), new PxRect(RedL, RedT, RedR, RedB),
                          RedBorder, RedTexW, RedTexH, QBg1, null, true, "Reward Background Get Reward");
            // 🆕 **2026-10-06（A94）：红底那块整幅底图吸收点击**。判据 = 原版 prefab
            //   `SkirmishModeEventWindow > Reward Background Get Reward` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读，rect = 0,121.80→1920,1013.11）
            //   —— 射线打到它自己、父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）
            //   ⇒ 原版点红底那一带**什么都不做**（只有红底**之外**的上下两条窄边才是「点外面关窗」）。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(RedL, RedT, RedR, RedB), QBg, QHit);
            // ③ `Menu Vignette`（`sprite=0` + `type=Sliced` ⇒ 纯色块，0.58 黑）
            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(VigL, VigT, VigR, VigB),
                          "Menu Vignette", QBg3, new Color(0f, 0f, 0f, 0.58f));
        }

        // ============================================================ 🆕 E15：活动在期判据（左列分两版）
        /// <summary>🆕 **2026-10-24（E15）断言注入口**（⛔ 只给 `Editor/SkirmishWindowAssertE15.cs` 用）：
        /// 非 null ⇒ **覆盖**判据，让自检能分别验「活动期 / 非活动期」两棵树是**互斥**的。
        /// <para>产品路径**永远不设它**（`null` ⇒ 走 <see cref="LiveOpsLocalCatalog.HasActiveFastModeEvent"/>）。
        /// 跑完调用 <see cref="ResetEventProbeForTest"/> 还原，⛔ 别让它漏到别的自检宿主里。</para></summary>
        public static bool? ForceActiveEventForTest;

        static bool _warnedNoActiveEvent;

        /// <summary>自检用：清掉注入 + 复位「已出声」闩（同族先例 `LiveOpsLocalCatalog.ResetWarnedForTest`）。</summary>
        public static void ResetEventProbeForTest()
        {
            ForceActiveEventForTest = null;
            _warnedNoActiveEvent = false;
        }

        /// <summary>🆕 **2026-10-24（E15）**：「现在有没有一场**在期的 FastMode（遭遇战）活动**」——
        /// 本窗左列画哪一版的**唯一判据**（`BuildLeftColumn` 与 `BuildExtras` 都读它）。
        ///
        /// <para>🔴 **它是真判据、不是写死 false**：落到 `LiveOpsLocalCatalog.TrySelectActiveFastMode`
        /// ⇒ 扫 `_fastModeEvents` 那张表（`Selectable && IsInPeriod`），与 Parent / Scoring / PremiumItemOffer
        /// **同一个挑选函数**（`TryPickEventIn`）。本地那张表**当前 0 条**（FastMode 的 SO 原版由服务端下发、
        /// 本地 13 份里没有）⇒ 现在恒 false —— 那是**数据事实**，不是分支被短路。
        /// 自检 `D1/D2` 把这两层都钉住（D2 = 同一函数在 `_events` 上能选出 Scoring ⇒ 这条链是活的）。</para>
        ///
        /// <para>判据出处 → `资料/普查产出_1024b/S1_小冲突非活动期界面.md` §①·2（`dump.cs:63944`：
        /// `SkirmishEventWindow : LiveOpsEventWindow&lt;IFastModeEvent&gt;`）+ §②（`LiveOpsEventData.IsActive`）。</para>
        ///
        /// <para>⚠️ **判不出就出声一次**（`_warnedNoActiveEvent`，每进程一次）：本地永远判不出「小冲突活动在期」
        /// （13 份 SO 全过期、没有 FastMode 类）—— 这条 warning 就是「屏幕上是**非活动期形态**」的可见凭据，
        /// ⛔ 不许静默换成经典式左列。</para></summary>
        public static bool HasActiveSkirmishEvent
        {
            get
            {
                if (ForceActiveEventForTest.HasValue) return ForceActiveEventForTest.Value;

                LiveOpsLocalEvent ev;
                string why;
                if (LiveOpsLocalCatalog.HasActiveFastModeEvent(LiveOpsLocalCatalog.NowUtc, out ev, out why))
                    return true;

                if (!_warnedNoActiveEvent)
                {
                    _warnedNoActiveEvent = true;
                    Debug.LogWarning("[Event] **小冲突当前不在活动期** ⇒ 本窗左列画**经典式** `Ranked Division Info`"
                        + "（⛔ 不画 `Reward Display`：活动进度 / 说明 / 胜利数 / 5 档里程碑 / 收集 —— 那些是活动期形态；"
                        + "用户 2026-10-24 口径）。判据 → " + why);
                }
                return false;
            }
        }

        // ============================================================ 左列
        /// <summary>🔴 **2026-10-24（E15）分叉**：活动期 ⇒ `Reward Display`（原版 prefab 那一棵，一字未改）；
        /// 非活动期 ⇒ **经典式** `Ranked Division Info`（见 <see cref="BuildClassicLeftColumn"/>）。</summary>
        protected override void BuildLeftColumn(Transform root)
        {
            if (HasActiveSkirmishEvent) { BuildRewardDisplay(root); return; }
            BuildClassicLeftColumn(root);
        }

        /// <summary>活动期那一版左列 = **原版 `SkirmishModeEventWindow ▸ Reward Display`**（`RewardProgressBarPanel`：
        /// 标题 + 说明 + 胜利数 + 5 档记分条 + `Collect`）。判据 / 逐颗几何见本文件上方那一大段注释。
        /// <para>⚠️ 这一棵**只在 `HasActiveSkirmishEvent == true` 时建**（原版没有非活动期的这棵）。</para></summary>
        void BuildRewardDisplay(Transform root)
        {
            var col = MenuDraw.Node(root, "Reward Display", new PxRect(RDL, RDT, RDR, RDB));

            // `Reward Tile`（"Progression" fs45.87 R）· `Reward Help`（fs38 居中）
            // 原版 hAlign：`Reward Tile` = Center（不调）· `Reward Help` = **Left**
            // 🔴 **2026-10-19（A1179）**：这一处原来**一个 autosize 实参都没传** ⇒ 固定 45.87px。
            //   判据 = 逐颗现读原版那一颗（`python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
            //   "SkirmishModeEventWindow" --depth 6 --relative --no-sprite --no-layout`）：
            //   `Reward Display/Reward Tile` · `'Progression'` · 字号 **45.87** · 基准 **36.0** ·
            //   **`m_enableAutoSizing = 1` · `auto[18.0~45.869999…]`** · 对齐 `Center/Midline` ·
            //   **折行 = 1** · 框 **522.01 × 54.68**（与本文件 `RTile*` 四个常量同值）。
            //   ⚠️ 原版**折行 = 1** ⇒ 传 `wrapPx`（= 本框宽）与它同档，⛔ **不要**再补 `SetWrapping(false)`
            //   （那是给原版 `折行=0` 的件的成对写法，见 A34-F4 那一族）。
            //   实参顺序照 `MenuDraw.Text(parent, r, text, color, name, fontPx, q, wrapPx, autoMinPx,
            //   autoMaxPx, autoBasePx, …)`；三道闸（`wrapPx > 0` ∧ `autoMinPx > 0` ∧ `fontPx > autoMinPx`）
            //   在 `MenuDraw.TextCore` 里（45.87 > 18 ⇒ 过闸）。
            // 🔴 **2026-10-19（界面线 P05）**：文案改走官方词条 —— 这颗 TMP 挂 `Localize`，
            //   `mTerm = Ranked/Skirmish/ProgressionTitle`（`bundle_menus_assets_all/MonoBehaviour/
            //   MonoBehaviour_-8647844292480714063.json` 现读）；官方值 en `Progression` / zh `进步`。
            //   ⚠️ 英文档返回值与改前写死的 `"Progression"` 逐字相同（`Editor/MainMenuScene.cs` 那条
            //   字面量断言在英文档下仍然绿；中文档显示官方中文）。
            MenuDraw.Text(col, new PxRect(RTileL, RTileT, RTileR, RTileB),
                          Loc.T("Ranked/Skirmish/ProgressionTitle"),
                          Color.white, "Reward Tile", 45.87f, QText,
                          RTileR - RTileL, 18f, 45.87f, 36f, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**一个 autosize 实参都没传** ⇒ 固定字号 38px。
            //   判据 = 逐颗现读原版那一颗（`工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow" --md`）：
            //   `Reward Display/Reward Help` = `'Win battles …unlock rewards.'` · 字号 **38.0** · 基准 **36.0** ·
            //   **`auto[18.0~38.0]`**（= `m_enableAutoSizing = 1` · `m_fontSizeMin 18` · `m_fontSizeMax 38`）·
            //   对齐 `Left/Top` · **折行 = 1** · 框 **592.774 × 100.99**（与本文件 `RHelp*` 那四个常量逐位同值）。
            //   ⚠️ 本处是 `TextBox`（**无条件折行**，`SetWrapWidth(r.W)`）⇒ 「38px × 1.437 行高 ≈ 54.6px、
            //   两行 ≈ 109 > 框高 101」这一定量**只有开了 autosize 才收得住**。
            //   实参顺序照 `MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, q, autoMaxPx, autoBasePx, …)`。
            // 🔴 **2026-10-19（界面线 P05）**：文案改走官方词条 —— 这颗 TMP 挂 `Localize`，
            //   `mTerm = Ranked/Skirmish/Progression`（`MonoBehaviour_3263968066473119090.json` 现读）；
            //   官方值 en `Win battles to progress in the event and unlock rewards.` /
            //   zh `赢得战斗以在活动中取得进步并解锁奖励。`（英文档逐字同改前）。
            var help = MenuDraw.TextBox(col, new PxRect(RHelpL, RHelpT, RHelpR, RHelpB),
                                        Loc.T("Ranked/Skirmish/Progression"), Color.white,
                                        "Reward Help", 38f, 18f, QText, 38f, 36f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(help, new PxRect(RHelpL, RHelpT, RHelpR, RHelpB));

            // `Player victories`：`Vicotries title`(fs48) + `Skull Victories`(Rank Skull) + `Total Victories`(fs48)
            var vic = MenuDraw.Node(col, "Player victories", new PxRect(VictL, VictT, VictR, VictB));
            // 原版 hAlign：`Vicotries title` = **Right**（它右对齐到那条线上）
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 48px。
            //   判据 = 逐颗现读原版那一颗（同上一处那条命令）：`…/Player victories/Vicotries title`
            //   = `'Victories: '` · 字号 **48.0** · 基准 **36.0** · **`auto[18.0~48.0]`** · 对齐 `Right/Capline` ·
            //   **折行 = 1** · 框 **236.63 × 50.00**（与本文件 `VTitle*` 四个常量逐位同值）。
            //   ⚠️ 原版**折行 = 1** ⇒ 传 `wrapPx`（= 本框宽）与它同档，⛔ **不要**再补 `SetWrapping(false)`
            //   （那是给原版 `折行=0` 的件的成对写法，见 A34-F4 那一族）。
            // 🔴 **2026-10-19（界面线 P05）**：文案改走官方词条 —— 这颗 TMP 挂 `Localize`，
            //   `mTerm = Ranked/Victories`（`MonoBehaviour_8193199729745612146.json` 现读）；
            //   官方值 en `Victories: `（**带尾空格**，与改前写死的逐字相同）/ zh `胜利：`。
            var vt = MenuDraw.Text(vic, new PxRect(VTitleL, VTitleT, VTitleR, VTitleB), Loc.T("Ranked/Victories"),
                                   Color.white, "Vicotries title", 48f, QText,
                                   VTitleR - VTitleL, 18f, 48f, 36f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(vt, new PxRect(VTitleL, VTitleT, VTitleR, VTitleB));
            // ⚠️ `Total Victories` 原版是 `Skull Victories` 的**子件**（实读 depth 4）
            var skull = MenuDraw.Node(vic, "Skull Victories", new PxRect(VSkullL, VSkullT, VSkullR, VSkullB));
            MenuDraw.Rect(skull, Tex("Rank_Skull"), new PxRect(VSkullL, VSkullT, VSkullR, VSkullB),
                          "Skull", QArt, null, true);
            //   ⚠️ 原版这个数是**活动里赢了几局**（liveop 事件数据）—— 我们没有事件 ⇒ 印 `0`（= 本地事实），出声；
            //   原版 hAlign = **Left**
            // 🔴 **2026-10-19（A1179）**：补 autosize 四格（原来没传 ⇒ 固定 48px）。
            //   判据 = 同一颗现读（命令见上面 `Reward Tile` 那一段）：
            //   `…/Player victories/Skull Victories/Total Victories` · `'125'` · 字号 **48.0** · 基准 **36.0** ·
            //   **`m_enableAutoSizing = 1` · `auto[18.0~48.0]`** · 对齐 `Left/Midline` ·
            //   **折行 = 1** · 框 **180.84 × 50.00**（与本文件 `VCount*` 四个常量逐位同值）。
            //   ⚠️ 原版**折行 = 1** ⇒ 传 `wrapPx` 与它同档，⛔ 不要补 `SetWrapping(false)`。
            var tv = MenuDraw.Text(skull, new PxRect(VCountL, VCountT, VCountR, VCountB), "0", Color.white,
                                   "Total Victories", 48f, QText,
                                   VCountR - VCountL, 18f, 48f, 36f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(tv, new PxRect(VCountL, VCountT, VCountR, VCountB));
            Debug.Log("[Event] `Total Victories` 印的是 **0** —— 原版读 liveop 事件里的胜利数，"
                      + "我们本地没有事件（**不编数字**，如实印 0）");

            BuildScoreBar(col);
        }

        // ============================================================ 🆕 E15：经典式左列（非活动期）
        /// <summary>🆕 **2026-10-24（E15）**：**非活动期**那一版左列 = 原版 `RankedEventWindowV2` 的
        /// `Ranked Division Info`（`RankingDisplay`）。逐件照 S1 报告 §①·3 那六样：
        /// `Rank Title` + `Content`（`DivisionText` / `DivisionImage` / 结束于 / 里程碑）+ `LeaderboardButton`
        /// + `ChangeRankedToggle`（未排名⟷排名）。
        ///
        /// <para>🔴 **为什么复用 `RankedDivisionInfo.Build` 而不是另抄一棵**：那是段位信息块的**唯一一份** builder
        /// （`Shell/RankedDivisionInfo.cs`，判据正本 `资料/普查产出_0927/段位块_RankedDivisionInfo.md`）。
        /// 再抄一棵 = 两份同族实现迟早不一致（`CLAUDE.md` 三·「两处写同一条规则 = 迟早不一致」）。
        /// ⚠️ 两扇窗的左列**同一个槽**（`0,146.93 → 638,959.07`）⇒ 它里面那批绝对画布坐标在这里逐位成立。</para>
        ///
        /// <para>🔴 **数值全部留空**（段位名 / 两个评分 / 里程碑进度 / 段位大图 / 结束时刻都在服务器）——
        /// 这是 `RankedDivisionInfo` 里已经成文的处置（用户 2026-09-26 口径「不编数字」），本窗照用、**没有第二条口径**。
        /// 本地拿不到的只有两样，都由 builder 自己出声：段位名留空 · `Timer` 印 `--`。</para>
        ///
        /// <para>🆕 **判据（本窗为什么要这棵）**：`RankedEventWindowV2__LeaderboardButtonClick` 按 `playMode` 二选一
        /// （`0 Classic → rankingPrefabClassic`，否则 → `rankingPrefab` = 遭遇榜）⇒ **V2 确实也服务小冲突玩法**；
        /// 非活动期的小冲突**没有活动窗可开**，`Reward Display` 那一套（活动进度/胜利数/5 档/收集）就无从谈起。
        /// ⚠️ 「哪场活动绑哪扇窗」在远端 `EventComponents[MainWindow]` 里，本地读不到 ⇒ 这一步是**推断（中高）**，
        /// 不是直读（S1 报告 §①·4 / §⑥·1 如实标过）。</para></summary>
        void BuildClassicLeftColumn(Transform root)
        {
            var col = MenuDraw.Node(root, "Ranked Division Info", new PxRect(RDL, RDT, RDR, RDB));

            // ---- ① `Rank Title`（原版这颗 TMP 挂 `Localize`，`mTerm = PlayerProfile/Rank`，en `Rank` / zh `排名`）----
            //   判据 = `Shell/RankedEventWindow.cs` 同处（`MonoBehaviour_5930028326761236400.json` 现读）；
            //   原版 hAlign = Center ⇒ 不调 `Align*`。
            MenuDraw.Text(col, new PxRect(RankTitleL, RankTitleT, RankTitleR, RankTitleB),
                          Loc.T("PlayerProfile/Rank"), Color.white, "Rank Title", 57.7f, QText,
                          face: TmpFont.Face.LatinCardBody);

            // ---- ② `LeaderboardButton`（`UI_Button_Mulligan` + 文字 fs36；`type=Simple` ⇒ 拉伸铺满）----
            //   ⚠️ 文字必须挂在**按钮节点底下**（原版树里就是它的子件）。
            var lbn = MenuDraw.Node(col, "LeaderboardButton", new PxRect(LeaderL, LeaderT, LeaderR, LeaderB));
            var lbQ = MenuDraw.Rect(lbn, Tex(ArtMulligan), new PxRect(LeaderL, LeaderT, LeaderR, LeaderB), "Bg", QArt);
            MenuDraw.Text(lbn, new PxRect(LeaderTxL, LeaderTxT, LeaderTxR, LeaderTxB),
                          Loc.T("MainMenu/RankedWindow/Leaderboard"), Color.white, "Button Text", 36f, QText,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Hit(lbn, "Hit", new PxRect(LeaderL, LeaderT, LeaderR, LeaderB), QHit,
                         OpenClassicLeaderboard, lbQ, ArtMulligan, clickCue: WindowSounds.PositiveCue);

            // ---- ③ 段位信息块 `Content`（唯一一份 builder；含 `DivisionText` / `DivisionImage` / `footer` / 里程碑 / `steps`）----
            RankedDivisionInfo.Build(col, Tex, QArt, QText, QArt1);

            // ---- ④ `Content/Timer`（= `RankingDisplay.timerDisplay`，那一行「结束于：…」）----
            //   ⚠️ 必须建在 `RankedDivisionInfo.Build` **之后**（它才建出 `Content`），且挂在 `Content` 下
            //      ——原版 `timerDisplay` 指的正是 `Ranked Division Info ▸ Content ▸ Timer`。
            BuildClassicTimer(col);

            // ---- ⑤ `ChangeRankedToggle`（未排名 ⟷ 排名）----
            BuildClassicToggle(col);

            // 出声一次（⛔ 不静默换成经典式；同 R1/E12 口径：结构齐、数值留空、说清为什么）
            Debug.Log("[Event] 遭遇战窗左列 = **经典式 `Ranked Division Info`**（本机不在 FastMode 活动期）"
                    + " —— 段位名 / 两个评分 / 里程碑进度 / 段位大图 / 结束时刻**全部留空**："
                    + "它们是服务端数据（`RankingDisplay.Initialize` 那一支），本地没有 ⇒ 不编数字。");
        }

        /// <summary>`Ranked Division Info/Content/Timer` —— 「结束于：…」那一行（**本地无截止时刻 ⇒ 整棵不建**）。🔴 **2026-10-29（`B99_W2`）就地订正（铁律 5 + §18·0）**：原文这里写的是「**本地无截止时刻 ⇒ 印 `--`**」——**已过期**：原版 `TimerDisplay.UpdateTimer` 取不到目标时刻那一支是 **`return;`（静默、一个字都不印）**（我们的 `Shell/TimerDisplay.cs:375` 注释自己就写着这句），先例 = `X4` 裁定「无倒计时 ⇒ 整行不建」⇒ 印 `--` 属「我们做了原版没有的」。判据 → `判据/B99_R3b_施工单_其余按文件.md` §2·4。
        /// <para>几何/判据：见本文件 `Tmr*` 那三组常量的注释（= V2 prefab **跑过布局之后**的真值）。
        /// 文案 = 官方词条 `Event_Description/General/EndsInPlusOrangeTime`（值带 `{0}` ⇒ 走 `TimerDisplay.Compose`，
        /// ⛔ 别自己拼「结束于：」）—— 与活动期那颗 `Timer`、与排位窗 `BuildRankTimer` **同一条口径**。</para></summary>
        void BuildClassicTimer(Transform col)
        {
            var content = col.Find("Content");
            if (content == null)
            {
                // ⛔ 不静默跳过：这一棵的结构来自 `RankedDivisionInfo.Build`，它没建出 `Content` = 那件被人改过。
                Debug.LogWarning("[Event] 经典式左列的 `Content/Timer` **没建** —— 找不到 `Ranked Division Info/Content`"
                                 + "（`RankedDivisionInfo.Build` 的产出变了？）");
                return;
            }
            if (!ClassicTimerDeadline.HasValue)   // 🔴 **2026-10-30（`B100_F5` · 闸写反了就地订正）**：原读 `if (ClassicTimerDeadline.HasValue)` = **有**才不建（意图相反），而它恒 null ⇒ 那条「整棵不建」一次都没走过 ⇒ 非活动期一直多画一行 `结束于：`。原版 `TimerDisplay.UpdateTimer` **取不到目标时刻那一支是 `return;`**（静默、⛔ 不印 `--`）⇒ 本地恒无（类尾 `ClassicTimerDeadline`）⇒ **整棵不建**；将来有数据源时把本行换成那个真判据即可恢复下面那一支（§2·4「留开关，不硬删」）
            {   // ⛔ 不建 `Timer` / 不建 `Timer Icon` / 一个字都不印 —— 「没有数据」不是「印个占位」
                Debug.Log("[Event] 经典式左列的 `Content/Timer` **整棵不建** —— 原版 `TimerDisplay.UpdateTimer` 在取不到目标时刻时是 `return;`（静默、不印 `--`）；本窗的经典式左列只在**没有 FastMode 活动**时建 ⇒ 根本没有结束时刻（判据 → `判据/B99_R3b_施工单_其余按文件.md` §2·4）。"); return; }
            var tm = MenuDraw.Node(content, "Timer", new PxRect(TmrL, TmrT, TmrR, TmrB));
            MenuDraw.Rect(tm, Tex("WF_icon_clock"), new PxRect(TmrIcL, TmrIcT, TmrIcR, TmrIcB), "Timer Icon", QArt);
            string prefix = Loc.T("Event_Description/General/EndsInPlusOrangeTime");
            var txR = new PxRect(TmrTxL, TmrTxT, TmrTxR, TmrTxB);
            var tx = MenuDraw.Text(tm, txR, TimerDisplay.Compose(prefix, ""), Color.white, "Timer", 38f, QText,
                                   face: TmpFont.Face.LatinCardBody);
            if (tx == null) return;                     // 建不出来 ⇒ `TimerDisplay.Attach` 会自己出声，这里不再补一句
            // 字号四格 = 原版 `38 / 10 / 38 / 38`（本窗与 V2 同档）。⚠️ `SetAutoFitBox` 内部会 `SetWrapWidth` ⇒ 紧跟一句还原**原版折行 0**
            tx.SetAutoFitBox(LayoutSpace.Px(txR.W), LayoutSpace.Px(txR.H), 10f, 38f, 38f); tx.SetWrapping(false);
            TimerDisplay.Attach(tx, () => ClassicTimerDeadline, prefix, "遭遇战窗（非活动期）经典式左列 Timer",
                                "小冲突的结束时刻由服务端下发，本地没有");
        }

        /// <summary>本窗「是排位还是非排位」。**本地恒 false（非排位）** —— 没有服务器、也没有分段数据；
        /// 点了**如实说明**，不假装切（同 `Shell/RankedEventWindow.cs` 的口径，那边是同一颗 `ChangeRankedToggle`）。</summary>
        static bool ClassicRankedMode { get { return false; } }

        /// <summary>`ChangeRankedToggle`（`ToggleDisplay`）：底图 `40k_menu_bt`（col a=0 ⇒ 透明）+ 开关粒
        /// `40_main_bt_toggle_on` + 两行 45px 文字（字距 −4，原版实测）。命中区 = **底图 ∪ 两颗文字**（并集）。</summary>
        void BuildClassicToggle(Transform col)
        {
            var tg = MenuDraw.Node(col, "ChangeRankedToggle", new PxRect(TgL, TgT, TgR, TgB));
            MenuDraw.Nine(tg, Tex("40k_menu_bt"), new PxRect(TgL, TgT, TgR, TgB),
                          MenuBtBorder, MenuBtTexW, MenuBtTexH, QBg3, new Color(1f, 1f, 1f, 0f), true, "Bg");
            // 原版 hAlign：`UnrankedText` = **Right** · `RankedText` = **Left**（各自贴中间那颗开关）
            // ⚠️ 字距会改文字宽度 ⇒ 放在 `Align*` **之前**（同族顺序纪律）。
            var un = MenuDraw.Text(tg, new PxRect(TgUnrankedL, TgUnrankedT, TgUnrankedR, TgUnrankedB),
                                   Loc.T("MainMenu/RankedWindow/Unranked"), Color.white, "UnrankedText", 45f, QText,
                                   face: TmpFont.Face.LatinCardBody);
            if (un != null) un.SetCharSpacing(-4f);
            MenuDraw.AlignRight(un, new PxRect(TgUnrankedL, TgUnrankedT, TgUnrankedR, TgUnrankedB));
            var rk = MenuDraw.Text(tg, new PxRect(TgRankedL, TgRankedT, TgRankedR, TgRankedB),
                                   Loc.T("MainMenu/RankedWindow/Ranked"), Color.white, "RankedText", 45f, QText,
                                   face: TmpFont.Face.LatinCardBody);
            if (rk != null) rk.SetCharSpacing(-4f);
            MenuDraw.AlignLeft(rk, new PxRect(TgRankedL, TgRankedT, TgRankedR, TgRankedB));
            MenuDraw.Rect(tg, Tex("40_main_bt_toggle_on"),
                          new PxRect(TgKnobL, TgKnobT, TgKnobR, TgKnobB), "Image", QArt1, null, true);
            // 命中区 = 底图 ∪ 两颗 TMP（判据 = 原版 `ChangeRankedToggle` 子树的可射线件并集，
            //   旧口径只传底图 = 左右各漏 ~180/225px ⇒ 点在字上不出反应）。
            MenuDraw.Hit(tg, "Hit", MenuDraw.Union(
                             new PxRect(TgL, TgT, TgR, TgB),
                             new PxRect(TgRankedL, TgRankedT, TgRankedR, TgRankedB),
                             new PxRect(TgUnrankedL, TgUnrankedT, TgUnrankedR, TgUnrankedB)),
                         QHit, ToggleClassicRankedMode, clickCue: WindowSounds.SelectToggleCue);
            ApplyClassicToggleState(un, rk);
        }

        void ApplyClassicToggleState(Label unranked, Label ranked)
        {
            // 本地真实状态 = 非排位 ⇒ 亮的那一颗就是当前模式（⛔ 不是随便挑一个颜色）。
            var on = Color.white;
            var off = new Color(1f, 1f, 1f, 0.45f);
            if (unranked != null) unranked.SetColor(ClassicRankedMode ? off : on);
            if (ranked != null) ranked.SetColor(ClassicRankedMode ? on : off);
        }

        void ToggleClassicRankedMode()
        {
            // 原版切的是「打排位还是非排位」。本地没有排位赛（没有服务器与分段数据）⇒ **如实说明**。
            Debug.LogWarning("[Event] `ChangeRankedToggle`（经典式左列）点了 —— 本地**没有排位赛**"
                             + "（没有服务器与分段数据），如实说明、不假装切换");
            var wm = Manager != null ? Manager : WindowsManager.Instance;
            if (wm != null)
                wm.ShowPopUp(Loc.T("MainMenu/Ranked/OfflineNote"), Loc.T("MainMenu/General/OK"), null);
        }

        void OpenClassicLeaderboard()
        {
            // 原版 `RankedEventWindowV2.LeaderboardButtonClick`：按 `playMode` 二选一 ——
            //   `Classic(0)` ⇒ `rankingPrefabClassic`（经典榜）；否则（本窗 `Skirmish(13)`）⇒ `rankingPrefab`（遭遇榜）。
            // 判据 → `资料/普查产出_0927/排行榜_入口与调用.md` §1 + `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二。
            var kind = DeckGameMode == (int)RuleEngine.GameMode.Classic
                     ? LeaderboardKind.Classic : LeaderboardKind.Skirmish;
            WindowsManager.OpenByRef(LeaderboardWindow.NameOf(kind), m => LeaderboardWindow.Create(m, kind));
            Debug.Log("[Event] `LeaderboardButton`（经典式左列）⇒ 开 `" + LeaderboardWindow.NameOf(kind)
                      + "`（按 `DeckGameMode` 二选一：经典 ⇒ 经典榜，其余 ⇒ 遭遇榜）");
        }


        /// <summary>`Scoring Bar Event Score Info`（`MilestoneScoreBar`）：竖条 + 5 个里程碑箱子。
        /// 🔴 **这一族被两层 `localScale` 包着**（`Scoring Bar` 1.2563 → `Progress Bar` 0.8696），
        ///    所以 rect 不能照 `menu_rect.py` 打的本地值用 —— 要**绕各自的中心**乘回去（pivot 全是 (.5,.5)，实测）。
        ///    `menu_rect.py` **不套 scale**（`rect_of` 的 `scale` 形参收了没用）—— 这一条记在
        ///    `资料/命令速查.md` 那条命令的注释里。下面两个换算函数就是补这一步。
        /// <para>🔴 **2026-10-19（A1197）现读登记：这棵子树里有两颗 TMP，而本窗【一件都没建】**
        /// —— 所以这一笔在本窗的**改动 = 0 处**（⛔ 不是我漏了）。逐颗现读
        /// （`python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow"
        /// --depth 14 --relative --no-sprite` 行末 `⇲ls=`，`m_LocalScale` 用 `_tmp_view/r4scratch/chain2.py` 逐层读）：
        /// · `…/Score Bar Line Level k/Skull/Score`（TMP `'1256'`，现读字号 48 · 基准 36 · auto[18~48]）
        ///   链 = `Score`(1.0) ← `Skull`(**0.8695654273033142**) ← `Score Bar Line Level k`(1.0)
        ///        ← `Score Levels`(1.0) ← `Scoring Bar Event Score Info`(**1.2562999725341797**)
        ///   ⇒ 祖先刻度积 = **1.0924350**（⛔ 那个 0.8696 长在 `Skull` 上、`Score` 是它的**子件**）。
        /// · `…/Scoring Bar Event Score Info/Generic Simplified UI Button/Button Text`（TMP `'Collect'`，
        ///   现读字号 55 · 基准 12 · auto[10~55]）⇒ 祖先刻度积 = `Scoring Bar Event Score Info` 那一颗 = **1.2563**。
        /// ⇒ **将来补这两颗时（它们现在被有意不建，见 `BuildMilestones` 末尾两条 `Debug.Log`）**：
        ///   `fontPx` / `autoMinPx` / `autoMaxPx` / `autoBasePx` **四格一起乘**上面那个积（框不动 ——
        ///   `ScoreRect()` 出来的已是屏值）。判据（TMP 的自适应**在本地单位里二分** ⇒ 原版屏上字号 = 本地字号 ×
        ///   祖先刻度积）全文 → 姊妹件 `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的「A1197」那一段。
        /// 🔴 **逐窗取值**：同一个子 prefab 在能量窗里是 **1.32**（`Score` 的积 = 1.1478264），**⛔ 别一刀切**。</para></summary>
        void BuildScoreBar(Transform col)
        {
            var bar = MenuDraw.Node(col, "Scoring Bar Event Score Info", new PxRect(SBarL, SBarT, SBarR, SBarB));

            // `Progress Bar`（Slider）：本地的 261.00,447.98→337.00,862.54，再被两层 scale 缩放
            var pr = ProgressRect(299f, ProgrCy, 76f, 414.56f);
            var pn = MenuDraw.Node(bar, "Progress Bar", pr);
            MenuDraw.Rect(pn, Tex("MiniBar_01"),
                          ProgressRect(299f, ProgrCy, 38f, 414.56f), "Background", QArt, null, true);
            MenuDraw.Rect(pn, CardArt.Solid(), ProgressRect(298.68f, 654.07f, 26.48f, 368.42f),
                          "Fill Area", QArt1, new Color(1f, 0.228f, 0.0142f, 0f));   // 进度 = 0 ⇒ alpha 0

            // 🆕 2026-10-29（`B97_MINI2` · 账 `未分类_其他:430`）：原版 `MilestoneScoreBar.progressBar` 就挂在这颗
            //   `Progress Bar` 上（判据 = `menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow"` 现读：
            //   `Scoring Bar Event Score Info` 的组件 = `MilestoneScoreBar`（字段 `progressBar,scoringLines`）·
            //   `Progress Bar` 的组件 = **`Slider`** · 5 行 `Score Bar Line Level k` 的组件 = `GenericScoreBarLine`）。
            //   本工程的渲染线**不画 uGUI 滑块** ⇒ 这颗 `Slider` 只承载 `maxValue / normalizedValue`
            //   （原版 `SetProgressBar` 写的就是这两格），⛔ 别指望它把 Fill 画出来（Fill 仍由上面那颗 quad 表示）。
            var barSlider = pn.gameObject.AddComponent<UnityEngine.UI.Slider>();

            BuildMilestones(bar);
            WireScoreBar(bar, barSlider);
        }

        /// <summary>5 个里程碑（`Score Bar Line Level 1..5`）：左→右 = 分数 · 骷髅标记 · 竖条 · 箱子。
        /// 摆位来自 `Score Levels` 的 VLG（**静态**）⇒ 这 5 行可以照画。
        /// 箱子那 5 档（Iron/Copper/Silver/Gold/Warp）是**原版写死的**。
        /// <para>🆕 **2026-10-17（A1221）**：**`Score`（分数文字）与 `Collect` 钮补建了** —— 原来这里用
        /// 「`MilestoneScoreBar.Initialize` 要活动数据」当理由不建，但**同一颗件在 `Energy` 那一扇窗里
        /// 是照 prefab 出厂原文建的**（`EnergySinglePlayerOnlyEventWindow.cs` 的 `BuildMilestones` / `BuildScoreBar`）
        /// ⇒ 那条理由站不住（两扇窗都没有活动数据）。现在两扇窗同处置：
        /// · `Score` 印 **prefab 出厂原文 `'1256'`**（原版运行时由 `Initialize(currentScore, …)` 改）；
        /// · `Collect` 照出厂建、点了**如实说「暂无服务器」**（原版 `CollectClicked` 要走 LiveOps 领奖）。
        /// ⚠️ `Highlight Crate` 仍是「哪一档都不亮」（`readyToCollect` 由活动数据定），那一条**保留**。</para></summary>
        void BuildMilestones(Transform bar)
        {
            _scoreLines.Clear();                    // 每次建树重建组件表（⛔ 别让上一次开窗的悬空组件留在表里）
            var levels = MenuDraw.Node(bar, "Score Levels",
                                       ScoreRect(299f, LevelCy1 - LevelStep * 2f, 100f, 0f));
            for (int i = 0; i < 5; i++)
            {
                // i = 树序下标（0 = `Level 1`）⇒ 它排在**最下**：`reverse=1` ⇒ 往上每走一档 **−LevelStep**
                float cy = ScoreLevelRowCy(i);
                var rowR = ScoreRect((LevelX1 + LevelX2) * 0.5f, cy, LevelX2 - LevelX1, LevelRowH);
                var row = MenuDraw.Node(levels, "Score Bar Line Level " + (i + 1), rowR);
                var skullQ = MenuDraw.Rect(row, Tex("Rank_Skull"),
                              ScoreRect(299f + SkullDx, cy, SkullSide * ChestScale, SkullSide * ChestScale),
                              "Skull", QArt, null, true);
                var chestQ = MenuDraw.Rect(row, Tex(CrateArt[i]),
                              ScoreRect(299f + ChestDx, cy, ChestW * ChestScale, ChestH * ChestScale),
                              "Chest", QArt, null, true);
                //   `Highlight Crate`（`Crate Border Highlight` 金色 + `UIBorderGlow`）—— 原版是**在「可领取」时**才点亮那一档。
                //   ⚠️ **实读的字段是 col(0.98,0.801,0.0588,1)**（不透明）——
                //   **我们按「没有活动数据 ⇒ 哪一档都不亮」画 alpha 0，这一条是我们挑的**（不是原版字段值）。
                var hiQ = MenuDraw.Rect(row, Tex("Crate_Border_Highlight"),
                              ScoreRect(299f + HiCrateDx, cy + HiCrateDy, HiCrateW * HiCrateScale, HiCrateH * HiCrateScale),
                              "Highlight Crate", QArt1, new Color(0.98f, 0.801f, 0.0588f, 0f));
                // 🆕 **2026-10-17（A1221）**：`Score`（原版 `…/Skull/Score`）。判据（同文末命令现读）：
                //   `'1256'` · 字号 **48** · 基准 **36** · **`auto[18.0~48.0]`** · 对齐 `Right/Midline` ·
                //   **折行 = 1** · 祖先刻度积 **1.0924350**（见 `ScoreFontScale` 那条 doc）⇒ 四格**一起乘**、框不动。
                var sr = new PxRect(rowR.x1 + ScoreOffL, rowR.y1 + ScoreOffT,
                                    rowR.x1 + ScoreOffR, rowR.y1 + ScoreOffB);
                var sc = MenuDraw.Text(row, sr, "1256", Color.white, "Score",
                                       48f * ScoreFontScale, QText, sr.W,
                                       18f * ScoreFontScale, 48f * ScoreFontScale, 36f * ScoreFontScale, face: TmpFont.Face.LatinCardBody);
                MenuDraw.AlignRight(sc, sr);           // 原版 `Right/Midline`（折行 1 ⇒ ⛔ 不补 SetWrapping(false)）
                // 🆕 2026-10-29（`B97_MINI2` · 账 430）：这一行就是原版那颗 `GenericScoreBarLine`
                //   （prefab 现读：`Score Bar Line Level k` 挂它，字段 `score/chestReward/chestOpen/readyToCollect/resourceIcon`
                //    正好指向 `Score` / `Chest` / 出厂开箱图 / `Highlight Crate` / `Skull` 这五件）。
                //   ⇒ 组件挂上去、把**已经建好的这五件**绑给它（几何/颜色/父子链一个像素都没动）。
                var line = row.gameObject.AddComponent<CardPresentation.ScoreBars.GenericScoreBarLine>();
                line.Bind(sc, chestQ, Tex(CrateArt[i] + "_open"),
                          hiQ != null ? hiQ.gameObject : null, skullQ);
                _scoreLines.Add(line);
            }

            // 🆕 **2026-10-17（界面线 `N37②`）：文字走官方词条** —— `Button Text` 那颗挂了 `Localize`，
            //   现读 `mTerm = MainMenu/General/Collect`（同 GameObject 的 TMP `m_text = 'Collect'`；
            //   `bundle_menus_assets_all/MonoBehaviour_5462748652185210226.json` = TMP，
            //   `MonoBehaviour_-213610246681571982.json` = `Localize`）⇒ 官方值 `Collect` / `收集`。
            //   ⇒ 上屏 `Loc.T("MainMenu/General/Collect")`；⛔ 别再写死 `"Collect"`（中文档下会印英文）。
            //   判据（同前）：`Button Text` = `'Collect'` · fs **55** · base **12** · `auto[10~55]` ·
            //   对齐 `Center/Capline` · **折行 = 0** · 框 267.32×69.64（= `CollectTxR`）· 祖先刻度积 **1.2563**
            //   ⇒ 四格一起乘；折行 0 ⇒ 建完立刻 `SetWrapping(false)`（`SetAutoFitBox` 会把它开成 Normal）。
            var collect = MenuDraw.Node(bar, "Generic Simplified UI Button", CollectR);
            var cb = MenuDraw.Rect(collect, Tex("UI_Button_Mulligan"), CollectR, "Bg", QArt1, null, true);
            // 🔴 **2026-10-17（`A1753` #12 的另一半）**：`Collect` 的 `Button Text` 材质 =
            //   `Pragati-Regular Off White Cold`，`_FaceColor = (0.9117, 0.9811, 0.9607)`（白顶点 ⇒ 屏上就是这个）。
            //   与 `EnergySinglePlayerOnlyEventWindow` / `RewardWindow` 同一档（判据 → `R_T07_剩余四项.md` §4·2）。
            var collectInk = new Color(0.9117f, 0.9811f, 0.9607f, 1f);
            var ct = MenuDraw.Text(collect, CollectTxR, Loc.T("MainMenu/General/Collect"), collectInk, "Button Text",
                                   55f * SBarScale, QText, CollectTxR.W,
                                   10f * SBarScale, 55f * SBarScale, 12f * SBarScale, face: TmpFont.Face.LatinCardBody);
            if (ct != null) ct.SetWrapping(false);
            var ch = MenuDraw.Hit(collect, "CollectHit", CollectR, QHit, CollectClicked, cb, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            // 原版 `RewardProgressBarPanel.Initialize`：`collectButton.interactable = HasMilestoneToCollect(milestones)`（见类尾那两条）
            if (ch != null) { var cw = ch.GetComponent<WindowButton>(); if (cw != null) cw.Interactable = HasMilestoneToCollect(ForceMilestoneStatesForTest); }

            Debug.Log("[Event] 记分条的 `Score` 照 **prefab 出厂原文 `1256`**、`Collect` 照出厂建 —— "
                      + "原版 `MilestoneScoreBar.Initialize(currentScore, scoringMilestones)` 会按活动分数改它们，"
                      + "本地没有活动数据（⛔ 不编数字）");
        }

        /// <summary>🆕 **2026-10-17（N31）自检用**：第 `i` 行（**树序下标**，0 = `Level 1`）的行中心 y
        /// （`ScoreRect` 那一帧的设计 px）—— `BuildMilestones` 与自检**共用这一处**（先例 =
        /// `Battle/WfSlider.cs` 的 `HandleHitRect`：算式只写一份，自检拿同一份判）。
        /// <para>🔴 **`reverse=1` ⇒ 下标越大越靠上**（y 越小）。灭自证断言：`ScoreLevelRowCy(0) > ScoreLevelRowCy(4)`
        /// （即屏上 `Level 1` 在 `Level 5` **下面**）—— 改回旧公式 `LevelY0 + LevelRowH*(i+0.5)`（下标越大越靠下）
        /// 立即红。⛔ 别把这里的 `−` 写成 `+`。</para></summary>
        public static float ScoreLevelRowCy(int i) { return LevelCy1 - LevelStep * i; }

        /// <summary>🆕 **2026-10-17（A1221）**：`Collect` 点了 ⇒ **如实提示「暂无服务器」**（= 原版 `CollectClicked`）。
        /// 原版 `RewardProgressBarPanel.CollectClicked` 向 LiveOps 领奖（= `Energy` 窗那一颗同一条链）。
        /// <para>🔶 自己设计（非原版）：`Collect` 的离线提示窗与那句文案 · 为什么必须自定：领奖走 LiveOps 服务端
        /// （`IFastModeEvent.TryCollect`，已关服）+ 官方客户端**没有「离线」这个分支的文案**（官方 13 语言表 10,862 键 0 命中）
        /// ⇒ 只能自拟一句上屏 · 调度台 2026-10-17 裁定（同族同一句在 `EnergySinglePlayerOnlyEventWindow`）。
        /// ⛔ 别硬套官方键、也别把这句话当成原版文案。</para></summary>
        void CollectClicked()
        {
            Debug.Log("[Event] `Collect` 点了 ⇒ **如实提示「暂无服务器」**"
                    + "（原版要 LiveOps 活动数据领奖；本地没有活动、也没有服务器 —— 照边界③不静默）。");
            if (WindowsManager.Instance != null)
                WindowsManager.Instance.ShowPopUp("Event rewards are not available offline.", "OK", null);
        }

        // ============================================================ 计时器 + 禁用卡提示
        /// <summary>两条**只有遭遇战窗有**的件（排位窗的树里没有）：`Timer` 与 `Banned card in deck`。</summary>
        protected override void BuildExtras(Transform root)
        {
            // 🆕 **2026-10-24（E15）**：这颗 `Timer` = **活动倒计时**（原版 `TimerDisplay` 喂的是活动结束时刻）。
            //   非活动期**不建** —— 否则屏幕上会同时出现两行「结束于：」（经典式左列自带
            //   `Ranked Division Info ▸ Content ▸ Timer`，那才是常驻形态）；原版任何一扇窗都没有两个倒计时。
            TimerDeadline = ActiveEventDeadline(); if (HasActiveSkirmishEvent && TimerDeadline.HasValue) BuildTimer(root);   // 🔴 2026-10-30（`B100_F5`）· **赋值必须写在本行**（插行会让本文件下游账落点漂移，铁律 18·b）：原版 `SkirmishEventWindow__TryOpen.c` 开窗时就把 `活动.GetEndDate()` 喂 `TimerDisplay__Initialize`（目标**定死**在 `+0x78`）⇒ 本字段按**在期 FastMode 活动的 `endDate`** 取值，本机无活动数据时才为 null（原来是恒 null 的字段 ⇒ 活动期永远没有倒计时）。🔴 2026-10-30（B99_A4 · 反向清单 §五 #1）：原版 `TimerDisplay__UpdateTimer.c` 取不到目标时刻是 `return;` 静默 ⇒ 无截止时刻**整棵不建**，⛔ 不印 `--`
            else Debug.Log("[Event] `Timer`（活动倒计时）**没建** —— 缺截止时刻（`TimerDeadline` 为 null，原版那一支是 `return;` 静默、⛔ 不印 `--`）"
                         + "或本窗不是活动期；非活动期那一行由经典式左列的 `Ranked Division Info ▸ Content ▸ Timer` 承担（它同样受 `ClassicTimerDeadline` 闸）");
            BuildBanned(root);
            // ⚠️ 原版两扇窗都还有一层 `DEBUG`（`DebugAutoDisabler` + 红字 `EventId` / `DebugBattle` 那几件，出厂 act=1）。
            //    **不建**：🔴 **2026-10-19（界面线 P05 · 铁律 5 就地订正）** —— 这里原来写「`DebugAutoDisabler`
            //    的方法体在本地反编译里**读不到**（同名 `.c` 里是别的函数）」，**已过期**：
            //    `d:/2/tools/decomp_full/DebugAutoDisabler__OnEnable.c` 的**函数符号被错配**成
            //    `BattleAlliancePanel__CloseButtonClick`，但**函数体可读** = `Component.get_gameObject(this)`
            //    → `GameObject.SetActive(false)` ⇒ **原版这个组件在 `OnEnable` 就把自己关掉**。
            //    ⇒ 我们「不建 DEBUG」与原版运行时一致（不是「判不出」）。
            Debug.Log("[Event] `DEBUG` 那一层**没建** —— 原版挂 `DebugAutoDisabler`，其 `OnEnable` 实读 = "
                      + "`gameObject.SetActive(false)`（自关；反编译函数符号错配，函数体可读）⇒ 运行时本来就不显示");

            WireHeaderComponent(root);   // 🔴 2026-10-28（B89-E29）窗头组件接线（方法在类尾）
        }

        /// <summary>`Timer`：图标 + 倒计时文字（**照 `Energy` 窗那一套**，A1248）。
        /// <para>🔴 **2026-10-17（A1248）**：原来这里只建一个**空节点** + 一条 `Debug.Log`
        /// （理由 = 「图标的位置由倒计时文字的宽度决定，本地没有活动结束时间 ⇒ 不编位置」）。
        /// 那个理由是**半对**：位置确实由 HLG 决定，但**本 prefab（`SkirmishModeEventWindow`）跑过布局**，
        /// 已经把那份结果序列化回去了 ⇒ 图标 / 文字的落点**有真值可抄**（见 `TmIconW` 那条 doc）；
        /// 而且文字本身照**官方词条** `Event_Description/General/EndsInPlusOrangeTime`（原版 prefab 出厂
        /// 占位是 `'Termina en: 23d 5h'`，运行时同样被 `TimerDisplay` 覆盖），🔴 **2026-10-30（`B99_A4`）就地订正**：原句「截止时刻未知时由 `TimerDisplay` 显示 `--`」**已过期** ——
        /// 原版那一支是 `return;` **静默**（`TimerDisplay__UpdateTimer.c` 第一条），而本方法现在只在 `TimerDeadline.HasValue` 时被调用（§调用点 `BuildExtras` 那道闸）⇒ 本地恒无截止时刻 ⇒ **整棵不建**（⛔ 不印 `--`、⛔ 不编日期）。</para>
        /// <para>⚠️ 判据补充：`Vicotries title` 那一颗**已经是对的**（`本文件` 起那四格自适应），⛔ 别顺手改它
        /// —— 本窗的 `Vicotries title` 是**定宽 236.63×50、没有 CSF**（`menu_dump … --depth 6` 现读），
        /// 与 `Energy` 那一颗（宽 0 + CSF）**不是同一档**。</para></summary>
        void BuildTimer(Transform root)
        {
            var tm = MenuDraw.Node(root, "Timer", new PxRect(TmL, TmT, TmR, TmB));
            // 图标先按**跑过布局那一份 prefab 的真值**摆（813.8 起），量到文字宽之后随组居中重排。
            float fbStart = TmL + ((TmR - TmL) - (TmIconW + TmPrefabTextW)) * 0.5f;
            var icR0 = new PxRect(fbStart, TmIconT, fbStart + TmIconW, TmIconB);
            var iconQ = MenuDraw.Rect(tm, Tex("WF_icon_clock"), icR0, "Timer Icon", QArt);

            // 文字：**官方词条**（原版 `TimerDisplay.extraText` 的 `mTerm = Event_Description/General/EndsInPlusOrangeTime`，
            //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-800057299095106190.json` 现读；
            //   官方值 en `Ends in:  <color=#E98A00FF>{0}</color>` / zh `结束于：<color=#E98A00FF>{0}</color>`）。
            //   ⚠️ 值里带 `{0}` ⇒ **不能**用 `+` 拼（会印出花括号）—— 走 `TimerDisplay.Compose`（原版
            //   `TimerDisplay.SetTimerDescriptionText` 的 `String.Contains("{0}") ? Format : Concat` 同一条路）。
            //   ⚠️ 出厂占位时长仍照 prefab 原文 `23d 5h`（原版 prefab 里那一串就是它；运行时被真实剩余换掉）。
            //   字号四格 = 原版 `38 / 10 / 38 / 38`（**本窗根缩放 = 1** ⇒ 不乘刻度）。
            //   ⚠️ 原版 **折行 = 0** ⇒ 传 `wrapPx` 会让 `SetWrapWidth` 静默开成 `Normal` ⇒ 紧跟一句还原。
            string TimerPrefix = Loc.T("Event_Description/General/EndsInPlusOrangeTime");
            var txR0 = new PxRect(icR0.x2, TmTextT, icR0.x2 + TmPrefabTextW, TmTextB);
            var tx = MenuDraw.Text(tm, txR0, TimerDisplay.Compose(TimerPrefix, "23d 5h"), Color.white, "Timer", 38f, QText,
                                   txR0.W, 10f, 38f, 38f, face: TmpFont.Face.LatinCardBody);
            if (tx != null) tx.SetWrapping(false);
            float txW = PreferredWidthPx(tx, "Timer");

            // HLG 重排：`content = 图标宽 + 文字宽`（`spacing 0` · `align 4 MiddleCenter` · `pad (0,0,5,0)`）。
            // 量不到（helper 已出声）⇒ 保留上面那套 **prefab 真值**兜底，⛔ 不拿 0 当宽去排。
            if (txW > 0f)
            {
                float x1 = TmL + ((TmR - TmL) - (TmIconW + txW)) * 0.5f;
                var icR = new PxRect(x1, TmIconT, x1 + TmIconW, TmIconB);
                var txR = new PxRect(icR.x2, TmTextT, icR.x2 + txW, TmTextB);
                if (iconQ != null)
                    iconQ.transform.localPosition = MenuDraw.Local(tm, icR.x1, icR.y1, icR.x2, icR.y2);
                if (tx != null)
                {
                    // 原版 `Center/Capline`（框 = 字宽 ⇒ 居中与左对齐同解）⇒ 摆到新框中心。
                    tx.transform.localPosition = MenuDraw.Local(tm, txR.x1, txR.y1, txR.x2, txR.y2);
                    tx.SetAutoFitBox(LayoutSpace.Px(txR.W), LayoutSpace.Px(txR.H), 10f, 38f, 38f);
                    tx.SetWrapping(false);       // `SetAutoFitBox` 内部会 `SetWrapWidth` ⇒ 还原原版折行 0
                }
            }

            // 挂全工程唯一的倒计时口径（🔴 **2026-10-30（`B99_A4`）就地订正**：原来写「截止时刻本地没有 ⇒ 出声 + `<EndsInPlusOrangeTime>--`」**已过期** —— 原版那一支是 `return;` 静默，`--` 已删；目标取自类尾 `TimerDeadline`，⛔ 不编日期）。
            // 🔴 **2026-10-19（界面线 P05）**：补上原版那两档描述串 ——
            //   常档 `extraText` = `Event_Description/General/EndsInPlusOrangeTime`；
            //   最后 5 分钟档 `lastMinutesText` = `Event_Description/Generic/EndsSoon`
            //   （`MonoBehaviour_-800057299095106190.json` 现读；该件 `useLastMinutesText` 见同文件）。
            //   ⚠️ 目标未知时这一档走不到（`TimerDisplay.UpdateTimer` 在取不到目标时刻时**静默 `return;`**、一个字都不写），但两档都要接全。
            if (tx != null)
                TimerDisplay.Attach(tx, () => TimerDeadline, TimerPrefix, "遭遇战活动窗 Timer",
                                    "活动截止时刻由服务端 LiveOps 事件下发，本地没有",
                                    lastMinutesPrefix: Loc.T("Event_Description/Generic/EndsSoon"));
        }

        /// <summary>🆕 **2026-10-17（A1248）**：量 TMP 自己的 `GetPreferredValues().x`（= 原版 CSF
        /// `h:PreferredSize` 走的那条路）。**逐字照抄** `EnergySinglePlayerOnlyEventWindow.PreferredWidthPx`
        /// （那一份是 `private`、本件与它没有共同基类成员 ⇒ 复制一份；⛔ 别为此去动 `LiveOpsEventWindow`）。
        /// ⚠️ 量不到就返回 0 并**出声**（点阵后端 / 取不到 TMP）—— 调用方保留兜底框，⛔ 不静默。
        /// ⚠️ 量的是**我方字体**的宽，不是原版 `Pragati-Regular SDF` 那个数（残差同 Energy 那一份的说明）。</summary>
        static float PreferredWidthPx(Label lb, string who)
        {
            if (lb == null) return 0f;
            var t = lb.GetComponentInChildren<TextMeshPro>(true);
            if (t == null)
            {
                Debug.LogWarning("[Event] `" + who + "` 量不出 preferred width（这颗没有 TMP —— 字体资产缺失时"
                        + " `Label` 退回点阵后端）⇒ 框宽停在 prefab 真值那一档，**不是**原版 CSF 的行为。");
                return 0f;
            }
            float wWorld = t.GetPreferredValues().x;
            if (float.IsNaN(wWorld) || float.IsInfinity(wWorld) || wWorld <= 1e-6f)
            {
                Debug.LogWarning("[Event] `" + who + "` 的 preferred width 量出 " + wWorld
                        + "（NaN/Inf/非正）⇒ 框宽停在 prefab 真值那一档，**不是**原版 CSF 的行为。");
                return 0f;
            }
            return wWorld * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
        }

        /// <summary>`Banned card in deck`：原版**出厂 act=1**、运行时由 `OnDeckSelected` 开关并**改写文案**。
        /// <para>🔴 **2026-10-25（`E` · 完全复刻）**：这一行原来只按 prefab 词条静态建出来、**恒关**（「没有禁用表」）。
        /// 原版判据（一手，`cpp2il/SkirmishEventWindow.cs:488-524` 的 `OnDeckSelected`）：
        /// `(valid, err) = rankedDeckSelector.HasValidDeckWithValidationMessage()` →
        /// `battleButton.interactable = valid` · **`bannedCardInDeckText.text = DeckUtility.ToShortLocalizedString(err)`**
        /// （`DeckUtility.cs:92-118` = `MenuDeck/Error/&lt;枚举名&gt;/Short`、缺键回 `MenuDeck/Error/InvalidDeck/Short`）
        /// ⇒ **这一格印的是【短档】校验原因**，**不是** prefab 那条固定词条。
        /// 我们照它：`RefreshBannedLine()`（见下）。⚠️ 我们没有「禁用卡表」这一档枚举
        /// （原版 `InvalidDeckBannedCards`）⇒ 今天能产出的只有 `InvalidDeck` 等通用档。</para></summary>
        void BuildBanned(Transform root)
        {
            var b = MenuDraw.Node(root, "Banned card in deck", new PxRect(BannedL, BannedT, BannedR, BannedB));
            // 原版 hAlign = Center ⇒ 不调 `Align*`
            // 🔴 **2026-10-19（A1190）**：这一颗原来**没传 autosize 实参** ⇒ 固定 50px。
            //   判据 = 逐颗现读原版那一颗（`工具/menu_dump.py bundle_menus_assets_all
            //   "SkirmishModeEventWindow" --depth 8 --md`）：
            //   `…/Banned card in deck/Text` = `'The deck has banned cards'` · 字号 **50.0** ·
            //   基准 **36.0** · **`auto[18.0~50.0]`**（`m_enableAutoSizing = 1`）·
            //   对齐 `Center/Midline` · **折行 = 1** · 框 **678.30 × 53.93**（= `Banned*` 四个常量逐位同值）
            //   ⇒ 传 `wrapPx` 与原版同档（折行 1 ⇒ ⛔ 不补 `SetWrapping(false)`，那是折行 0 那族的成对写法）。
            // 🔴 **2026-10-19（界面线 P05）**：文案改走官方词条 —— 这颗 TMP 挂 `Localize`，
            //   `mTerm = MenuDeck/Error/InvalidDeckBannedCards`（`MonoBehaviour_1490397487827668338.json` 现读）；
            //   官方值 en `The deck has banned cards` / zh `卡组有禁用的张牌`（英文档逐字同改前）。
            // ⚠️ 那只是**prefab 出厂串**；运行时原版会把它改成短档校验原因（见上面那段）。
            _bannedLine = MenuDraw.Text(b, new PxRect(BannedL, BannedT, BannedR, BannedB),
                          Loc.T("MenuDeck/Error/InvalidDeckBannedCards"),
                          Color.white, "Text", 50f, QText, BannedR - BannedL, 18f, 50f, 36f, face: TmpFont.Face.LatinCardBody);
            // 🔴 2026-10-26（调度台裁定）：**容器恒开**（原版 prefab `act=1`、`OnDeckSelected` 只 SetActive 那颗 TMP）⇒ 显隐全交给下面这行开关 `Text`
            RefreshBannedLine();       // 建完就按当前卡组刷一次（原版这一行也是随 `OnDeckSelected` 刷的）
        }

        Label _bannedLine;

        /// <summary>刷「这副卡组为什么不可用」那一行（原版 `SkirmishEventWindow.OnDeckSelected` 的后半段）。
        /// <para>**文案 = 短档**：`DeckRules.DescribeShort(err)`（A 的 `DeckRules`；只有 `InvalidDeck` /
        /// `InvalidDeckBannedCards` 两档有 `/Short`，其余回长档 —— 与官方 15 条表一致）。</para>
        /// <para>🔴 **兜底（本工程红线：不许把键名印到屏幕上）**：`Loc.T(键)` 在**缺词条时返回键名本身**
        /// ⇒ 若取到的串 == 键名，就**回退长档**；长档也缺 ⇒ **出声并整行关掉**（⛔ 不印键名、不静默）。
        /// ⛔ 别为此去改 `Loc.T` 的全局行为（会影响别人）。</para>
        /// <para>⚠️ **刷新时机**（🔴 **2026-10-26（`X4`）就地订正**：原来这里写「窗内换卡组后**不立即**变、
        /// 如实记在报告里」—— 那是「基类 `RefreshDeckColumn` 不是虚方法、子类挂不上」时的遗留限制，**已解除**）：
        /// 现在挂在基类的注入点 `LiveOpsEventWindow.OnDeckColumnChanged` 上（空实现虚钩子，`RefreshDeckColumn` 收尾调）
        /// ⇒ **换阵营 / 上下一套 / 选卡组弹窗挑完，这一行同一拍就刷**；<see cref="Open"/> 与 <see cref="StartMatch"/> 那两处保留兜底。</para></summary>
        public void RefreshBannedLine()
        {
            if (_bannedLine == null) return;
            // 🔴 **2026-10-28（`B93_D1` · 账 `B93_Q2 :724`）**：判据收口到基类 `TryValidateCurrentDeck`
            //   （= 原版 `RankedDeckSelector.HasValidDeckWithValidationMessage()` 的 `valid`）——
            //   同一个布尔**同时**喂下面这一行提示与战斗钮 `interactable`（原版 `OnDeckSelected` 就这两句）。
            //   ⛔ 别在这里再写第二份「张数规则」：它按**这副牌自己的模式**判（`deck.GameMode`），
            //   写死 `skirmish: true` 会把「经典预组」误报成张数不对。
            RuleEngine.DeckError err;
            RuleEngine.PlayerDeck deck;
            bool valid = TryValidateCurrentDeck(out err, out deck);
            SetBattleInteractable(valid);      // = 原版 `battleButton.interactable = valid`（`OnDeckSelected` 第一句）
            if (valid) { _bannedLine.gameObject.SetActive(false); return; }
            // 空态：原版 `currentDeck == null` ⇒ `interactable = false` 且这一行**不写**（`HasValidDeckWithValidationMessage` 的 null 支）
            if (deck == null) { _bannedLine.gameObject.SetActive(false); return; }   // 到这里 = 有卡组但校不过 ⇒ 下面写短档原因

            string key = RuleEngine.DeckRules.DescribeShort(err);
            string text = string.IsNullOrEmpty(key) ? null : Loc.T(key);
            if (!string.IsNullOrEmpty(text) && text == key) text = null;      // 缺词条 ⇒ 回退长档
            if (text == null)
            {
                string longKey = RuleEngine.DeckRules.Describe(err);
                string longText = string.IsNullOrEmpty(longKey) ? null : Loc.T(longKey);
                if (!string.IsNullOrEmpty(longText) && longText != longKey) text = longText;
                if (text == null)
                {
                    Debug.LogWarning("[Event] 「卡组不可用」那一行：短档 `" + key + "` 与长档 `" + longKey
                                   + "` 都取不到词条 ⇒ **整行关掉**（⛔ 不把键名印到屏幕上）");
                    _bannedLine.gameObject.SetActive(false);
                    return;
                }
            }
            _bannedLine.SetText(text);
            _bannedLine.gameObject.SetActive(true);
            // 🔴 2026-10-26（调度台裁定）：⛔ 不动容器 —— 原版 `OnDeckSelected` 只 SetActive 这颗 TMP（= `Text` 子件），容器出厂 `act=1` 恒开
            Debug.Log("[Event] 「卡组不可用」提示 = " + text + "（短档 " + err + "）");
        }

        /// <summary>开窗时刷一次「卡组不可用」提示（见 <see cref="RefreshBannedLine"/> 的刷新时机）。</summary>
        public override void Open()
        {
            base.Open();
            RefreshBannedLine();
        }

        /// <summary>点 `Battle!` 之前再刷一次（这一行必须是「当前这副」的原因；原版在换卡组时就刷）。</summary>
        public override void StartMatch()
        {
            RefreshBannedLine();
            base.StartMatch();
        }

        protected override string TrophyIconArt { get { return "WF_UI_Trophy_Gold"; } }

        /// <summary>🆕 2026-09-26：**本窗 = 遭遇模式（`PlayModes.Skirmish = 13`）** ——
        /// 点 `Create deck` 建的卡组带着这个模式（12 张规则），`Battle!` 也按它挑 `GameplayVariables`。
        /// 判据 → `资料/加时与冲突模式_原版规格.md` §2.7。</summary>
        protected override int DeckGameMode { get { return (int)RuleEngine.GameMode.Skirmish; } }

        /// <summary>🆕 **2026-10-15（A383）**：本窗的 **`PlayModes`** = `Skirmish 13` ——
        /// 判据 = 原版 `FastModeBaseEvent.get_EventPlayMode` 返回 `13`
        /// （`RankedFastMode : FastModeBaseEvent`；读数 → `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二 ①·B·6）。
        ///
        /// <para>🔴 **为什么挂在这个钩子上**：基类真正切场景的那一步（`LiveOpsEventWindow.StartBotBattle`）
        /// 不在本轮的改动范围里，而**基类紧接着调 `StartBotBattle` 的就是本方法** ——
        /// `_search.OnSearchDone = () => { OnSearchFinished(); StartBotBattle(); }`
        /// ⇒ 这是本窗能拿到的、**离「开战」最近**的那个点。
        /// ⛔ **别提前到 `StartMatch()`**（点 `Battle!` 那一刻）：那之后还有 12 秒搜索，
        /// 玩家取消、再换一扇窗点一次，模式号就串了。</para>
        /// <para>⚠️ 联机那一支**不走这里**（`NetTookOver` 时基类不调 `StartBotBattle`）——
        /// 那条路的模式号随开局包走（`NetPendingBattle.PlayMode`），不经过这个静态通道。</para></summary>
        protected override void OnSearchFinished()
        {
            base.OnSearchFinished();
            BattleDriver.SetPendingPlayMode(RuleEngine.GameMode.Skirmish);
            Debug.Log("[Event] 本局模式号 = **Skirmish(13)** ⇒ 已放进 `BattleDriver.SetPendingPlayMode` 通道"
                      + "（原版 `FastModeBaseEvent.get_EventPlayMode` = 13）");
        }

        /// <summary>遭遇战那张（名字对得上，图也在工程里 —— 练习窗那个 `Toggle` 用的就是它）。</summary>
        protected override string GameModeIconArt { get { return "40k_gamemode_icon_skirmish"; } }

        // ============================================================ 🆕 2026-10-17（`A1853②`）：事件说明弹窗的词条
        /// <summary>`Help` 开出来的那一份 = **记分活动（Scoring Event）的说明弹窗**。
        /// 判据 = 本地 4 个 Scoring Event SO 的 `drawData.EventLabels`
        /// （`d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/` 下
        ///  `Expansion Orks1 Scoring Event` / `Expansion UM1 …` / `Release Emperors Children …` / `Release Space Wolves …`）：
        /// <list type="bullet">
        /// <item>key **0 `Title`** → `Event_Description/ExpansionPass/Title`（ORK1/UM1）· `Event_Description/BattlePass/Title`（EC/SW）</item>
        /// <item>key **12 `Help`** → `Event_Description/ExpansionPass/GeneralHelp`（ORK1/UM1）· `Event_Description/BattlePass/GeneralHelp`（EC/SW）</item>
        /// <item>key 4 `NavigationBarTitle` / 10 `EndTitle` / 11 `EndDescription` → 页签标题 / 结束弹窗（**本窗没有这两个面**）</item>
        /// </list>
        /// 我们取**基版那一对**（`ExpansionPass/*`）；`BattlePass/*` 是同一形状的后一代（EC/SW 那两场）。
        /// 🔴 这两条都在 `Resources/official_names.json` 的 bulk 表里（`Loc.BulkOr` 取得到，2026-10-17 现读）。
        /// <para>🔴 **2026-10-19（界面线 P05 · 铁律 5 就地订正）**：这里原来写「原版『用哪份 popup prefab』=
        /// 事件 `EventComponents[30]` 的 assetGUID（4 个 Scoring Event 全是 `2d7ec1a46a19245798bf28a5d1a030bb`）
        /// —— 本地**解不出它对应哪个 prefab 名** ⇒ 变体仍用基类缺省（`Liveop Event Help Variant`）」。
        /// **「解不出」已过期**：该 GUID 在 `bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的
        /// `m_Container` 里就是它，落到 asset `m_PathID 5459663361566770346` → GameObject
        /// **`Event Help Popup Variant`**（其根 MB `titleTextTag=0 · descriptionTextTag=12`，与 Scoring Event
        /// 的 `EventLabels` key 0/12 正好对上）⇒ 本窗覆写 <see cref="HelpVariant"/> 钉成 `EventHelp`。
        /// ⚠️ 仍成立的是「FastMode 事件本体在远端」那半句（它决定的是**哪一场活动**，不是变体名）。
        /// 标签 tag 与变体不是同一组（本变体是 `WelcomeTitle(5)` / `Help(12)`，而 Scoring Event **没配 key 5**）——
        /// 我们这两个值是**直接喂文本**的、不按 tag 解析，所以这里只决定「取哪条词」。</para></summary>
        protected override EventPopupVariant HelpVariant { get { return EventPopupVariant.EventHelp; } }
        protected override string EventHelpTitleTerm { get { return "Event_Description/ExpansionPass/Title"; } }
        /// <inheritdoc cref="EventHelpTitleTerm"/>
        protected override string EventHelpDescriptionTerm { get { return "Event_Description/ExpansionPass/GeneralHelp"; } }

        /// <summary>`GeneralHelp` 的官方值里带 **`+{0}%`** —— 原版由事件 `EventLabel.Args` 填，
        /// 4 个 Scoring Event SO 逐个现读都是 `{ argType: 0, valueArg: "20" }`
        /// （`Expansion Orks1` / `Expansion UM1` / `Release Emperors Children` / `Release Space Wolves` 的
        /// `drawData.EventLabels[key=12].Args`）⇒ 实参 = **"20"**。
        /// ⛔ 不传的话界面上会印出 `+{0}%`（与「印键名」同一种坏法）。</summary>
        protected override string[] EventHelpDescriptionArgs { get { return new[] { "20" }; } }

        // ============================================================ scale 换算
        /// <summary>「记分条子系统」的本地坐标 → **画布矩形**（绕 `Scoring Bar` 的中心乘 1.2563）。</summary>
        static PxRect ScoreRect(float cx, float cy, float w, float h)
            => RectOf(SBarCx + (cx - SBarCx) * SBarScale, SBarCy + (cy - SBarCy) * SBarScale,
                      w * SBarScale, h * SBarScale);

        /// <summary>再往下一层（`Progress Bar` 自己的 0.8696，绕 **它** 的中心）→ 画布矩形。</summary>
        static PxRect ProgressRect(float cx, float cy, float w, float h)
        {
            float x = SBarCx + (cx - SBarCx) * SBarScale;
            float y = SBarCy + (cy - SBarCy) * SBarScale;
            return RectOf(x, y, w * ProgrScale * SBarScale, h * ProgrScale * SBarScale);
        }

        static PxRect RectOf(float cx, float cy, float w, float h)
            => new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);

        // ============================================================ 2026-10-26（`X4`）：换卡组 ⇒ 「卡组不可用」那一行同一拍刷
        /// <summary>基类 `RefreshDeckColumn()` 收尾的注入点 ⇒ 原版 `OnDeckSelected` 里
        /// 「换卡组那一刻就重写 `bannedCardInDeckText`」那半句在这里落地
        /// （判据 `d:/2/tools/decomp_full/SkirmishEventWindow__OnDeckSelected.c`；文案/兜底见 <see cref="RefreshBannedLine"/>）。
        /// <para>⛔ **别以为 <see cref="BuildBanned"/> / <see cref="Open"/> / <see cref="StartMatch"/> 那三处就够了** ——
        /// 它们只保证「开窗」「点 Battle 前」是对的；窗内换阵营 / 上下箭头 / 选卡组弹窗挑完时，
        /// 这一行会停在**上一副牌**的校验结论上（原版没有这种残留）。</para></summary>
        protected override void OnDeckColumnChanged()
        {
            RefreshBannedLine();
        }

        // ============================================================ 窗头组件接线（B89-E29 · 追加于类尾）
        /// <summary>与 `RankedEventWindow` 同一口径 —— 表头建在基类 `LiveOpsEventWindow.Build()` 里
        /// （`BuildHeader` 是 private），本子类在 `BuildExtras` 里按路径补线。
        /// 原版 `SkirmishEventWindow.header` 就是那颗组件（`SkirmishEventWindow__Open.c:28` 调
        /// `WindowHeaderWithBackButton.Initialize(header, ev, onBack)`）；标题 = 本模式名（小冲突）。
        /// <para>⚠️ 放在类尾是照「新成员追加类尾」的纪律（⛔ 插进已有成员之间会让账的行号整体漂移）。</para></summary>
        void WireHeaderComponent(Transform root)
        {
            ParentEventWindow.EnsureHeaderTitleResolver();
            WindowHeaderWithBackButton.WireByPath(root, WindowHeader.RootNameGameMode, () => CloseButtonClick(),
                                                  Loc.T("MainMenu/GameMode/Skirmish"));
        }

        // ================================================================================================
        //  🆕 2026-10-28（`B93_D1` · 账 `B93_Q2 :671/:722/:728`）—— `RewardProgressBarPanel.Initialize` 那一半。
        //  **追加在类尾**（⛔ 中段加行会让账按 `文件:行号` 的落点整体漂移）。
        // ================================================================================================

        /// <summary>自检注入口（⛔ 产品路径永不设它）：5 档里程碑各自的状态
        /// （`GenericScoringMilestone.State`：`0 Locked` / `1 Unlocked` / `2 Collected`）。
        /// <para>生产侧恒 `null` —— 本地**没有** FastMode（小冲突）活动数据，一档都不存在
        /// （`LiveOpsLocalCatalog.HasActiveFastModeEvent` 现读 = false）⇒ `Collect` 与原版「一档都没解锁」那一刻同值：**不可点**。</para></summary>
        public static int[] ForceMilestoneStatesForTest;

        /// <summary>原版 `RewardProgressBarPanel.HasMilestoneToCollect(GenericScoringMilestone[])` ——
        /// 判据 `cpp2il/out_cpp2il_dev_ilspy/RewardProgressBarPanel.cs:114-133`（`Enumerable.Any(milestones, m =&gt; m.state - 1 == 0)`）
        /// + 同一份的 `.c`（`state == State.Unlocked`）。
        /// <para>`null`（= 本地无活动数据）⇒ `false`，与「数组里没有任何一档解锁」是同一个值。</para></summary>
        public static bool HasMilestoneToCollect(int[] states)
        {
            if (states == null) return false;
            for (int i = 0; i < states.Length; i++) if (states[i] == 1) return true;   // 1 = Unlocked
            return false;
        }

        /// <summary>原版 `SkirmishEventWindow.RefreshScore` 里那条**状态投影**（`_003CRefreshScore_003Eb__0`）：
        /// 逐档算出 `GenericScoringMilestone`（score + state）。
        /// <para>判据 = `decomp_full/SkirmishEventWindow.__c__DisplayClass18_0___RefreshScore_b__0.c:22-30`：
        /// `if (currentScore &lt; r.milestone) state = Locked;
        ///  else state = (i &lt; collectedRewards) + 1;`
        /// （`i` = 该档在**里程碑表里的下标**、`collectedRewards` = 事件数据里已领档数 —— 后者的来源见下一段）。</para>
        /// <para>🔴 **`collectedRewards` 不是 0**：ILSpy 把 `DisplayClass.collectedRewards` 印成字面 `0`，
        /// 而 `.c` 现读是 **`*(int*)(ev+0x70)`**（`RefreshScore.c` 里 `*(undefined4 *)(lVar5 + 0x14) = *(undefined4 *)(lVar7 + 0x70);`
        /// —— `+0x10` 是 `currentScore`、`+0x14` 就是它）⇒ **ILSpy 丢了这个赋值**（「丢 `= true`」同族）。
        /// ⛔ 别照 ILSpy 把已领数写死 0，那会把「已领档」全算成「可领」。</para>
        /// <para>⚠️ 本方法是**纯函数**、不接数据源：`collectedRewards` 由调用方给（原版从 `IFastModeEvent` 取）。
        /// 本窗今天**不调它**（无活动数据 ⇒ 一档都没有）；判据留在这里是为了将来接上数据时不用回头再推一遍。</para></summary>
        public static int[] MilestoneStates(int currentScore, int collectedRewards, int[] milestoneScores)
        {
            if (milestoneScores == null) return null;
            var st = new int[milestoneScores.Length];
            for (int i = 0; i < st.Length; i++)
                st[i] = currentScore < milestoneScores[i] ? 0 : (i < collectedRewards ? 2 : 1);
            return st;
        }

        // ================================================================================================
        //  🆕 2026-10-29（`B97_MINI2` · 账 `未分类_其他:430`）—— **`MilestoneScoreBar` 组件接线**。
        //  **追加在类尾**（⛔ 中段加行会让账按 `文件:行号` 的落点整体漂移）。
        //  判据：原版这扇窗的 `Scoring Bar Event Score Info` 挂的就是 `MilestoneScoreBar`
        //  （`progressBar` = `Progress Bar` 上那颗 `Slider`；`scoringLines` = 5 行 `GenericScoreBarLine`），
        //  数据由 `MilestoneScoreBar.Initialize(currentScore, GenericScoringMilestone[], icon)` 灌。
        // ================================================================================================

        /// <summary>本窗建出来的 5 行组件（`scoringLines` 那一格）。</summary>
        readonly System.Collections.Generic.List<CardPresentation.ScoreBars.GenericScoreBarLine> _scoreLines
            = new System.Collections.Generic.List<CardPresentation.ScoreBars.GenericScoreBarLine>();
        CardPresentation.ScoreBars.MilestoneScoreBar _scoreBar;
        UnityEngine.UI.Slider _scoreSlider;

        /// <summary>自检读口：本窗的 `MilestoneScoreBar`（没建 ⇒ null）。</summary>
        public CardPresentation.ScoreBars.MilestoneScoreBar ScoreBarForTest { get { return _scoreBar; } }
        /// <summary>自检读口：`progressBar` 那一格（`Progress Bar` 上的 `Slider`）。</summary>
        public UnityEngine.UI.Slider ScoreBarSliderForTest { get { return _scoreSlider; } }

        /// <summary>把 `MilestoneScoreBar` 挂到已经建好的记分条上，并喂**本地模拟表**（见 `Shell/MilestoneLocalSimulation.cs`）。
        /// <para>🔴 这一步**不建任何新节点** —— 行/箱子/骷髅/高亮/分数全是 `BuildMilestones` 已经按 prefab 逐值建好的那五件，
        /// 这里只是把 `<see cref="CardPresentation.ScoreBars.GenericScoreBarLine"/>` 绑上去，再由
        /// `MilestoneScoreBar.Initialize` 按原版写一遍（`score.text` / 箱子图 / 高亮开关 / 进度条两格）。</para>
        /// <para>⚠️ 模拟表的取值口径 = **镜像 prefab 出厂态** ⇒ 这一步**不改变任何可见像素**
        /// （`score` = `1256` 与出厂 TMP 原文同字、`state = Unlocked` 与出厂「高亮亮着」同态、`currentScore = 0` ⇒ 进度 0）。
        /// ⛔ 真数据到齐时把这里换成服务端那份 `GenericScoringMilestone[]` 即可。</para></summary>
        void WireScoreBar(Transform bar, UnityEngine.UI.Slider slider)
        {
            _scoreSlider = slider;
            _scoreBar = bar.gameObject.AddComponent<CardPresentation.ScoreBars.MilestoneScoreBar>();
            _scoreBar.Bind(slider, _scoreLines.ToArray());
            CardPresentation.ScoreBars.MilestoneLocalSimulation.LogOnce("Event");
            _scoreBar.Initialize(CardPresentation.ScoreBars.MilestoneLocalSimulation.CurrentScore,
                                 CardPresentation.ScoreBars.MilestoneLocalSimulation.CreateTable(), null);
        }

        // ============================================================ 界面 #4（B99_W2 · 2026-10-29 · 追加在类尾）
        /// <summary>经典式左列 `Content/Timer` 的**结束时刻** —— **本地恒 null**（= 没有这条数据，不是「我们挑的 0」）。
        /// <para>为什么恒 null（判据，⛔ 不是猜）：`BuildLeftColumn` 按 <see cref="HasActiveSkirmishEvent"/> 分叉 ——
        /// 经典式那一棵**只在没有 FastMode 活动时**建，而没有活动就**没有结束时刻**（结束时刻 = 活动实例的 `endDate`，
        /// 由服务端下发；本地 13 份活动 SO 里 FastMode 类 **0 份**）。</para>
        /// <para>原版对照：`SkirmishEventWindow.TryOpen` 拿 `ev.endDate` 喂 `timerDisplay.Initialize(endDate)`；
        /// `TimerDisplay.UpdateTimer` 在取不到目标时刻时**静默 `return;`**（我们自己的 `Shell/TimerDisplay.cs:375`
        /// 注释就写着这句）⇒ 那一行**整棵不建**（先例 = `X4`：`Editor/MainMenuScene.cs` 的「无倒计时 ⇒ 整行不建」）。
        /// ⇒ ⛔ 印 `--` 是**我们做了原版没有的**，本批已去掉。判据 → `判据/B99_R3b_施工单_其余按文件.md` §2·4。</para>
        /// <para>将来接上 FastMode 活动数据时，**只改这一格**（返回那个 `endDate`）⇒ 下面那一支自动恢复建行。</para></summary>
        static System.DateTime? ClassicTimerDeadline { get { return null; } }

        // ============================================================ 界面 #5（B99_A6 · 2026-10-30 · 追加在类尾）
        /// <summary>**活动期**那颗 `Timer`（`BuildTimer`）的结束时刻 = **在期 FastMode 活动实例的 `endDate`**；
        /// 取不到（本机活动表为空 / 不在期）⇒ `null` ⇒ 按 §2·4「留开关，不硬删」**整棵不建**（调用点 `BuildExtras` 那道闸）。
        /// <para>判据（⛔ 不是猜）：原版 `SkirmishEventWindow__TryOpen.c` —— `TimerDisplay__Initialize(*(param_1+0xc0),
        /// <活动实例 `*(param_1+0x70)` 的 GetEndDate()>, key, 0)`；同一方法里 `SetActive(timerDisplay, active != 0)`。
        /// 即「有在期活动 ⇒ 按**它的**结束时刻建；活动不 active 才关」。`TimerDisplay__UpdateTimer.c` **第一条**
        /// `if (*(char *)(this+0x78) == '\0') return;` ⇒ 取不到目标时刻是**静默**（不写字、不动 GameObject）。</para>
        /// <para>🔴 **2026-10-30（`B100_F5` · 铁律 5 + §18·0 就地订正）**：原来这里是**恒 `null` 的字段**（`B99_A6`）——
        /// 后果 = **活动期永远没有倒计时**（闸恒不成立，E15 的 B3 红）。按 §18·0「需要拍板默认复刻原版」改成**开窗时按在期活动赋值**
        /// （<see cref="ActiveEventDeadline"/>，调用点 = `BuildExtras` 那道闸**同一行**）；数据表空（= 本机没有 FastMode 活动实例、
        /// 服务端才下发）时仍为 `null`，不是「写死不给」。⚠️ 它是**静态字段** ⇒ 每次建窗都必须重赋一次（否则上一场的时刻会漏过来）。</para>
        /// <para>⚠️ 追加在**类尾**（⛔ 别插中段 —— 本文件在账里被按 `文件:行号` 引用 26 处，最大落点 `:954`）。</para></summary>
        public static System.DateTime? TimerDeadline;

        /// <summary><see cref="TimerDeadline"/> 的**取值口** —— `BuildExtras` 每次建窗都调一次（同一行写在那道闸前面）。
        /// <para>为什么是**开窗时取一次**、而不是让 `TimerDisplay` 每秒现取：原版 `SkirmishEventWindow__TryOpen.c` 就是
        /// 开窗时 `TimerDisplay__Initialize(timerDisplay, 活动.GetEndDate(), …)` 把目标**定死**在 `+0x78`
        /// ⇒ 到点走 `remain <= 0` 那一支（清字 + `SetActive(false)` + `OnRunOutOfTime`）。若改成每秒按「当前有没有在期活动」现取，
        /// 活动一过期目标就变 `null` ⇒ 落到「目标未知」那一支（`return;`）⇒ 屏上**冻住最后一格倒计时**，与原版不符。</para>
        /// <para>⚠️ `ToLocalTime()`：`EndUtc` 是 `DateTimeKind.Utc`，而 `TimerDisplay` 拿本机 `DateTime.Now` 相减（**不认 `Kind`**）
        /// ⇒ 不转的话整整差一个时区（本机 UTC+8 = 少 8 小时）。</para></summary>
        public static System.DateTime? ActiveEventDeadline()
        {
            if (!HasActiveSkirmishEvent) return null;      // 判据与本窗左列 / 建树那道闸**同一处**（含自检注入口）
            LiveOpsLocalEvent ev;
            return LiveOpsLocalCatalog.TrySelectActiveFastMode(LiveOpsLocalCatalog.NowUtc, out ev, out _) && ev != null
                 ? ev.EndUtc.ToLocalTime() : (System.DateTime?)null;
        }
    }
}
