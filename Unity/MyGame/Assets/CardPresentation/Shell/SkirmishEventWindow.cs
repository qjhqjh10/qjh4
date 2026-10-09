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
        /// <summary>`Score Levels` VLG（spacing 0 · pad 0 · **align MiddleCenter**）⇒ 5 行 60.48 高居中叠。</summary>
        const float LevelRowH = 60.48f, LevelX1 = 151.19f, LevelX2 = 446.81f, LevelY0 = 465.73f;
        const float SkullDx = -46.4f, ScoreDx = -93.7f, ChestDx = 70.2f, HiCrateDx = 68.5f, HiCrateDy = -1.3f;
        const float SkullSide = 59.06f, ScoreW = 116.15f, ScoreH = 50f, ChestW = 91.03f, ChestH = 81.20f;
        const float HiCrateW = 150.86f, HiCrateH = 134.43f, HiCrateScale = 0.796f, ChestScale = 0.8696f;
        static readonly string[] CrateArt =
        { "40k_Crate_Tier1_Iron", "40k_Crate_Tier2_Copper", "40k_Crate_Tier3_Silver",
          "40k_Crate_Tier4_Gold", "40k_Crate_Tier5_Warp" };

        /// <summary>`Timer`（本窗有；⚠️ **排位窗没有这一件** —— 树里实测）。</summary>
        const float TmL = 648.33f, TmT = 1008.55f, TmR = 1271.67f, TmB = 1069.45f;
        const float TmIcL = 604.43f, TmIcT = 1047.50f, TmIcR = 648.33f, TmIcB = 1091.41f;
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
        ///    `资料/命令速查.md` 那条命令的注释里。下面两个换算函数就是补这一步。</summary>
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
        /// 摆位来自 `Score Levels` 的 VLG（**静态**）⇒ 这 5 行可以照画；
        /// ⚠️ **分数文字与「已高亮的那一档」不画**（`MilestoneScoreBar.Initialize(currentScore, scoringMilestones)`
        /// 要 event 数据，本地没有）—— 出声。箱子那 5 档（Iron/Copper/Silver/Gold/Warp）是**原版写死的**。</summary>
        void BuildMilestones(Transform bar)
        {
            var levels = MenuDraw.Node(bar, "Score Levels",
                                       ScoreRect(299f, (LevelY0 + LevelY0 + LevelRowH * 5f) * 0.5f, 100f, 0f));
            for (int i = 0; i < 5; i++)
            {
                float cy = LevelY0 + LevelRowH * (i + 0.5f);
                var row = MenuDraw.Node(levels, "Score Bar Line Level " + (i + 1),
                                        ScoreRect((LevelX1 + LevelX2) * 0.5f, cy, LevelX2 - LevelX1, LevelRowH));
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
            }
            Debug.Log("[Event] 记分条的**分数文字**与**已达成那一档的高亮**没画 —— "
                      + "原版 `MilestoneScoreBar.Initialize(currentScore, scoringMilestones)` 要活动数据，本地没有");
            Debug.Log("[Event] 记分条上那颗 `Collect` 钮没建 —— 同理（`RewardProgressBarPanel.HasMilestoneToCollect` 要活动数据）");
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

        /// <summary>`Timer`：图标照画，**倒计时文字不编**（原版读 liveop 事件的下发时间）。</summary>
        void BuildTimer(Transform root)
        {
            // ⚠️ **整件都不画**（连图标也不画）：`Timer` 是 `HorizontalLayoutGroup`（spacing 0 · align MiddleCenter）
            //    —— 图标的位置由**"倒计时文字有多宽"**决定，而那段文字（活动结束时间）本地没有
            //    ⇒ 单画一个图标只能**自己编一个位置**（照模板位会偏左约 207px、下端还出屏）。
            //    出声，别硬凑。
            MenuDraw.Node(root, "Timer", new PxRect(TmL, TmT, TmR, TmB));
            Debug.Log("[Event] `Timer` **一件都没画**（连图标）—— 原版那句 `Termina en: 23d 5h` 读的是 liveop "
                      + "事件的下发结束时间，本地没有；而图标的位置由那句话的宽度决定（HLG）⇒ 不编位置");
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
