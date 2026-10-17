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
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QBg, QHit, () => Close(), "BackdropHit");
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

        // ============================================================ 左列
        protected override void BuildLeftColumn(Transform root)
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
            MenuDraw.Text(col, new PxRect(RTileL, RTileT, RTileR, RTileB), "Progression",
                          Color.white, "Reward Tile", 45.87f, QText,
                          RTileR - RTileL, 18f, 45.87f, 36f);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**一个 autosize 实参都没传** ⇒ 固定字号 38px。
            //   判据 = 逐颗现读原版那一颗（`工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow" --md`）：
            //   `Reward Display/Reward Help` = `'Win battles …unlock rewards.'` · 字号 **38.0** · 基准 **36.0** ·
            //   **`auto[18.0~38.0]`**（= `m_enableAutoSizing = 1` · `m_fontSizeMin 18` · `m_fontSizeMax 38`）·
            //   对齐 `Left/Top` · **折行 = 1** · 框 **592.774 × 100.99**（与本文件 `RHelp*` 那四个常量逐位同值）。
            //   ⚠️ 本处是 `TextBox`（**无条件折行**，`SetWrapWidth(r.W)`）⇒ 「38px × 1.437 行高 ≈ 54.6px、
            //   两行 ≈ 109 > 框高 101」这一定量**只有开了 autosize 才收得住**。
            //   实参顺序照 `MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, q, autoMaxPx, autoBasePx, …)`。
            var help = MenuDraw.TextBox(col, new PxRect(RHelpL, RHelpT, RHelpR, RHelpB),
                                        "Win battles to progress in the event and unlock rewards.", Color.white,
                                        "Reward Help", 38f, 18f, QText, 38f, 36f);
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
            var vt = MenuDraw.Text(vic, new PxRect(VTitleL, VTitleT, VTitleR, VTitleB), "Victories: ",
                                   Color.white, "Vicotries title", 48f, QText,
                                   VTitleR - VTitleL, 18f, 48f, 36f);
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
                                   VCountR - VCountL, 18f, 48f, 36f);
            MenuDraw.AlignLeft(tv, new PxRect(VCountL, VCountT, VCountR, VCountB));
            Debug.Log("[Event] `Total Victories` 印的是 **0** —— 原版读 liveop 事件里的胜利数，"
                      + "我们本地没有事件（**不编数字**，如实印 0）");

            BuildScoreBar(col);
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

            BuildMilestones(bar);
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
            var levels = MenuDraw.Node(bar, "Score Levels",
                                       ScoreRect(299f, LevelCy1 - LevelStep * 2f, 100f, 0f));
            for (int i = 0; i < 5; i++)
            {
                // i = 树序下标（0 = `Level 1`）⇒ 它排在**最下**：`reverse=1` ⇒ 往上每走一档 **−LevelStep**
                float cy = ScoreLevelRowCy(i);
                var rowR = ScoreRect((LevelX1 + LevelX2) * 0.5f, cy, LevelX2 - LevelX1, LevelRowH);
                var row = MenuDraw.Node(levels, "Score Bar Line Level " + (i + 1), rowR);
                MenuDraw.Rect(row, Tex("Rank_Skull"),
                              ScoreRect(299f + SkullDx, cy, SkullSide * ChestScale, SkullSide * ChestScale),
                              "Skull", QArt, null, true);
                MenuDraw.Rect(row, Tex(CrateArt[i]),
                              ScoreRect(299f + ChestDx, cy, ChestW * ChestScale, ChestH * ChestScale),
                              "Chest", QArt, null, true);
                //   `Highlight Crate`（`Crate Border Highlight` 金色 + `UIBorderGlow`）—— 原版是**在「可领取」时**才点亮那一档。
                //   ⚠️ **实读的字段是 col(0.98,0.801,0.0588,1)**（不透明）——
                //   **我们按「没有活动数据 ⇒ 哪一档都不亮」画 alpha 0，这一条是我们挑的**（不是原版字段值）。
                MenuDraw.Rect(row, Tex("Crate_Border_Highlight"),
                              ScoreRect(299f + HiCrateDx, cy + HiCrateDy, HiCrateW * HiCrateScale, HiCrateH * HiCrateScale),
                              "Highlight Crate", QArt1, new Color(0.98f, 0.801f, 0.0588f, 0f));
                // 🆕 **2026-10-17（A1221）**：`Score`（原版 `…/Skull/Score`）。判据（同文末命令现读）：
                //   `'1256'` · 字号 **48** · 基准 **36** · **`auto[18.0~48.0]`** · 对齐 `Right/Midline` ·
                //   **折行 = 1** · 祖先刻度积 **1.0924350**（见 `ScoreFontScale` 那条 doc）⇒ 四格**一起乘**、框不动。
                var sr = new PxRect(rowR.x1 + ScoreOffL, rowR.y1 + ScoreOffT,
                                    rowR.x1 + ScoreOffR, rowR.y1 + ScoreOffB);
                var sc = MenuDraw.Text(row, sr, "1256", Color.white, "Score",
                                       48f * ScoreFontScale, QText, sr.W,
                                       18f * ScoreFontScale, 48f * ScoreFontScale, 36f * ScoreFontScale);
                MenuDraw.AlignRight(sc, sr);           // 原版 `Right/Midline`（折行 1 ⇒ ⛔ 不补 SetWrapping(false)）
            }

            // 🆕 **2026-10-17（A1221）**：`Collect`（`Generic Simplified UI Button`）。
            //   判据（同前）：`Button Text` = `'Collect'` · fs **55** · base **12** · `auto[10~55]` ·
            //   对齐 `Center/Capline` · **折行 = 0** · 框 267.32×69.64（= `CollectTxR`）· 祖先刻度积 **1.2563**
            //   ⇒ 四格一起乘；折行 0 ⇒ 建完立刻 `SetWrapping(false)`（`SetAutoFitBox` 会把它开成 Normal）。
            var collect = MenuDraw.Node(bar, "Generic Simplified UI Button", CollectR);
            var cb = MenuDraw.Rect(collect, Tex("UI_Button_Mulligan"), CollectR, "Bg", QArt1, null, true);
            var ct = MenuDraw.Text(collect, CollectTxR, "Collect", Color.white, "Button Text",
                                   55f * SBarScale, QText, CollectTxR.W,
                                   10f * SBarScale, 55f * SBarScale, 12f * SBarScale);
            if (ct != null) ct.SetWrapping(false);
            MenuDraw.Hit(collect, "CollectHit", CollectR, QHit, OnCollectClick, cb, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);

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

        /// <summary>🆕 **2026-10-17（A1221）**：`Collect` 点了 ⇒ **如实提示「暂无服务器」**。
        /// 原版 `RewardProgressBarPanel.CollectClicked` 向 LiveOps 领奖（= `Energy` 窗那一颗同一条链）。</summary>
        void OnCollectClick()
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
            BuildTimer(root);
            BuildBanned(root);
            // ⚠️ 原版两扇窗都还有一层 `DEBUG`（`DebugAutoDisabler` + 红字 `EventId` / `DebugBattle` 那几件，出厂 act=1）。
            //    **不建**：`DebugAutoDisabler` 的方法体在本地反编译里**读不到**（同名 `.c` 里是别的函数）
            //    ⇒ 判不出它运行时会不会自关，**照「不建 + 出声」办**，别把调试件当正式界面画上去。
            Debug.Log("[Event] `DEBUG` 那一层**没建** —— 原版挂 `DebugAutoDisabler`（方法体本地读不到，"
                      + "大概率 release 自关），我们按「不建调试件」办");
        }

        /// <summary>`Timer`：图标 + 倒计时文字（**照 `Energy` 窗那一套**，A1248）。
        /// <para>🔴 **2026-10-17（A1248）**：原来这里只建一个**空节点** + 一条 `Debug.Log`
        /// （理由 = 「图标的位置由倒计时文字的宽度决定，本地没有活动结束时间 ⇒ 不编位置」）。
        /// 那个理由是**半对**：位置确实由 HLG 决定，但**本 prefab（`SkirmishModeEventWindow`）跑过布局**，
        /// 已经把那份结果序列化回去了 ⇒ 图标 / 文字的落点**有真值可抄**（见 `TmIconW` 那条 doc）；
        /// 而且文字本身照 prefab 出厂原文 `'Termina en: 23d 5h'`（同 `Energy` 那一颗），
        /// 截止时刻未知时由 `TimerDisplay` 显示 `--` —— **不需要编位置、也不需要编日期**。</para>
        /// <para>⚠️ 判据补充：`Vicotries title` 那一颗**已经是对的**（`:132` 起那四格自适应），⛔ 别顺手改它
        /// —— 本窗的 `Vicotries title` 是**定宽 236.63×50、没有 CSF**（`menu_dump … --depth 6` 现读），
        /// 与 `Energy` 那一颗（宽 0 + CSF）**不是同一档**。</para></summary>
        void BuildTimer(Transform root)
        {
            var tm = MenuDraw.Node(root, "Timer", new PxRect(TmL, TmT, TmR, TmB));
            // 图标先按**跑过布局那一份 prefab 的真值**摆（813.8 起），量到文字宽之后随组居中重排。
            float fbStart = TmL + ((TmR - TmL) - (TmIconW + TmPrefabTextW)) * 0.5f;
            var icR0 = new PxRect(fbStart, TmIconT, fbStart + TmIconW, TmIconB);
            var iconQ = MenuDraw.Rect(tm, Tex("WF_icon_clock"), icR0, "Timer Icon", QArt);

            // 文字：prefab 出厂原文；字号四格 = 原版 `38 / 10 / 38 / 38`（**本窗根缩放 = 1** ⇒ 不乘刻度）。
            // ⚠️ 原版 **折行 = 0** ⇒ 传 `wrapPx` 会让 `SetWrapWidth` 静默开成 `Normal` ⇒ 紧跟一句还原。
            const string TimerPrefix = "Termina en: ";
            var txR0 = new PxRect(icR0.x2, TmTextT, icR0.x2 + TmPrefabTextW, TmTextB);
            var tx = MenuDraw.Text(tm, txR0, TimerPrefix + "23d 5h", Color.white, "Timer", 38f, QText,
                                   txR0.W, 10f, 38f, 38f);
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

            // 挂全工程唯一的倒计时口径（截止时刻本地没有 ⇒ 出声 + `Termina en: --`，⛔ 不编日期）。
            if (tx != null)
                TimerDisplay.Attach(tx, null, TimerPrefix, "遭遇战活动窗 Timer",
                                    "活动截止时刻由服务端 LiveOps 事件下发，本地没有");
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

        /// <summary>`Banned card in deck`：**建出来但关着** —— 「这副牌里有禁用卡」要一份禁用表，本地没有。</summary>
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
            MenuDraw.Text(b, new PxRect(BannedL, BannedT, BannedR, BannedB), "The deck has banned cards",
                          Color.white, "Text", 50f, QText, BannedR - BannedL, 18f, 50f, 36f);
            b.gameObject.SetActive(false);
            // 🔴 **原版出厂 act=1**，是**运行时**按「这副牌里有没有禁用卡」关掉的；我们**没有禁用卡表**
            //    ⇒ 只能主动关掉，**出声**（别让它静默消失）
            Debug.Log("[Event] `Banned card in deck` 建了但**我们主动关着** —— 原版出厂 act=1、"
                      + "运行时按「这副牌有没有禁用卡」开关；本地没有那份禁用表");
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
    }
}
