// RankedDivisionInfo.cs — 排位窗左列 **`Ranked Division Info` 里面那一整棵**（段位信息块）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/段位块_RankedDivisionInfo.md` —— §A 是「层 × 参数」表（55 个节点，工具逐行）·
// §A·1 是 **`RankingDisplay` 的字段 → 节点名对照表**（三路互证：序列化引用 + 反编译偏移 + 类声明）·
// §A·2 出场 `activeSelf` · §A·4 哪些值算不准 · §B 查不到的。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all "RankedEventWindowV2" --depth 10 --md`
//
// ---- 🔴 六条判据（读原始 JSON / 反编译定的）----
// ① 🔴 **同名节点有三个实例，我们用的是【实例 ③】**（`RankedEventWindowV2` 下那个，
//    `0,146.93→638,959.07`）—— 另两个是顶层根（`:4930`）与旧 `RankedEventWindow` 下的（`:10183`）。
//    判据 → 普查 §0（pid 反查 + `m_Father` 爬链 + `menu_rect` 复算）。
// ② 🔴 **正本 §4·2 那张表是【实例 ①】的**（层级有出入，本轮已就地订正）：
//    `MainRating` 的父是 **`footer`**（不是 `Content`）；`Global Rating`/`Position` 的父是
//    **`Legendary Ratings`**（不是 `MainRating`）。
// ③ 🔴 **`RankingDisplay` 的字段绑定**（§A·1，**照字段猜会猜反**）：`ratingHolder` → **`Content/footer/MainRating`**
//    （不是 `footer`）· `ratingDisplay` → `…/Legendary Ratings/Global Rating` · `positionDisplay` → `…/Position` ·
//    `sealCountDisplay` → `…/MainRating/Mission Milestones Progress` · `divisionText`/`divisionImage`/`timerDisplay`
//    各指自己那一支。
// ④ 🔴 **序列化 bool = `displayRating=0` / `displayPosition=1` / `displaySeals=1`**（而 `.ctor` 默认是
//    `1 / false / 1`）—— **照序列化值做**：`Global Rating` 那一支**运行时也不会被打开**。
// ⑤ 🔴 **这一整棵全是服务器数据**（段位名 / 两个评分 / 里程碑进度 / 段位大图）⇒ 照用户 2026-09-26 的口径
//    **留空、不编数字**：`DivisionText` 留空 · `RankImage` 与 `DivisionImage` 不画（那是段位图）·
//    评分两处留空 · `RankedSealStep` **保留 `Empty` 态、不画 `Fill`**（进度未知）。
//    ⚠️ 段位图**不是「我们没导」**—— 是「段位号 → 图名」那张对照表（`RankedDivisionsSO`）**本地就没有**
//    （判据 → `资料/普查产出_0927/档案窗_Ranking页与图名表.md` §D·1，全盘搜过）。
// ⑥ **`Timer` 整块不建**（赛季倒计时：服务器数据 + 用户明确不要赛季倒计时）；`counter`（里程碑计数）
//    **出厂 inactive 且全 bundle 零引用者** ⇒ 不建；两个 `Main Icon` 是 `m_Enabled=0` 的件 ⇒ 不画。
//
// ⚠️ **哪些是我们推的（不是原版）**：`footer` 的真高 —— 工具对**嵌套布局组**不递归，把 `footer` 与 `Timer`
//    都算成 `717.95→717.95`（高 0）。按 uGUI（`footer` VLG `ctrlH=0`、active 子节点只有 `MainRating`、
//    `sizeDelta.y = 67.799`）**真高 ≈ 67.8**（普查 §A·4 第 ①）⇒ 我们按 `MainRating` 自己的 rect 摆，
//    **不用那个 0**。footer 子树内部的相对位置不受影响。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Ranked Division Info/Content` 那一整棵（段位信息块）的**唯一一份** builder。
    /// 由 `Shell/RankedEventWindow.cs` 调（左列那段）。</summary>
    public static class RankedDivisionInfo
    {
        // ---- 真值（绝对画布像素；正本 §A 表）----
        static readonly PxRect ContentR = new PxRect(65.05f, 222.75f, 572.95f, 886.57f);
        static readonly PxRect RankTitleBgR = new PxRect(90.40f, 222.75f, 547.60f, 296.45f);
        static readonly PxRect DivisionTextR = new PxRect(141.30f, 225.61f, 496.70f, 293.59f);
        static readonly PxRect DivisionImageR = new PxRect(49.10f, 234.04f, 588.90f, 806.66f);
        static readonly PxRect RankImageR = new PxRect(265.02f, 413.82f, 372.98f, 463.09f);
        static readonly PxRect SpacerR = new PxRect(240.20f, 296.45f, 397.80f, 717.95f);
        static readonly PxRect MainRatingR = new PxRect(114.00f, 717.95f, 524.00f, 785.75f);
        static readonly PxRect MilestonesR = new PxRect(114.00f, 708.59f, 524.00f, 795.10f);
        static readonly PxRect MsBackgroundR = new PxRect(63.38f, 715.00f, 574.62f, 788.70f);
        static readonly PxRect StepsR = new PxRect(44.13f, 708.59f, 593.87f, 795.10f);
        /// <summary>两个 `RankedSealStep`：各 **100×86.51**，x 起点 221.55 与 316.45（相邻 −5.1）。
        /// ⚠️ 原版是 `MilestonesStepDisplay.stepPrefab` 按里程碑数实例化的 ⇒ **两个是预制体里烘的样板**；
        /// 本地没有里程碑数据，我们照预制体建**两个**。</summary>
        const float StepW = 100f, StepH = 86.51f, StepY = 708.59f;
        static readonly float[] StepX = { 221.55f, 316.45f };
        static readonly PxRect LegendaryR = new PxRect(114.00f, 723.17f, 524.00f, 780.52f);
        static readonly PxRect PositionR = new PxRect(196.62f, 723.17f, 258.42f, 780.52f);
        static readonly PxRect PositionHashR = new PxRect(197.88f, 723.17f, 237.88f, 780.52f);
        static readonly PxRect PositionValueR = new PxRect(237.88f, 723.17f, 257.17f, 780.52f);
        static readonly PxRect GlobalRatingR = new PxRect(258.42f, 723.17f, 441.38f, 780.52f);
        static readonly PxRect GlobalTrophyR = new PxRect(258.42f, 723.17f, 313.42f, 780.52f);
        static readonly PxRect GlobalValueR = new PxRect(313.42f, 723.17f, 441.38f, 780.52f);

        const string ArtBanner = "40K_main_rank_display";
        const string ArtSkullEmpty = "Rank_Skull_Empty";     // `RankedSealStep/Empty`
        const string ArtTrophy = "WF_UI_Trophy_Gold";        // `Global Rating/Secondary Icon`

        /// <summary>`RankTitleBG` 的色（正本 §A：`(1,1,1,0.918)`）—— 同一张图四处的 `a` **各不相同**
        /// （0.918 / 1 / 1 / 0.647），**别统一**（普查 §A·3）。</summary>
        static readonly Color BannerTint = new Color(1f, 1f, 1f, 0.918f);
        static readonly Color MsBgTint = new Color(1f, 1f, 1f, 0.647f);
        static readonly Color DivisionInk = new Color(0.961f, 0.914f, 0.737f, 1f);
        static readonly Color ValueInk = new Color(1f, 1f, 1f, 1f);

        /// <summary>建这一棵。`art` = 宿主的取图函数（取不到会记进宿主的 `MissingArt`）。
        /// `qArt/qInk/qTop` = 三档渲染队列（由宿主给，照排位窗那一套）。</summary>
        public static void Build(Transform parent, System.Func<string, Texture2D> art, int qArt, int qInk, int qTop)
        {
            var content = MenuDraw.Node(parent, "Content", ContentR);

            // ---- ① 段位名那一条 `RankTitleBG` → `DivisionText` ----
            MenuDraw.Rect(content, art(ArtBanner), RankTitleBgR, "RankTitleBG", qArt, BannerTint, false);
            // 🔴 段位名（原版样板是 `Division V`）**是服务器数据** ⇒ 留空、不编（判据 ⑤）。
            //    文字层照样建出来（有它的 rect 与字号，将来有数据就填）。
            var dtLbl = MenuDraw.Text(content, DivisionTextR, "", DivisionInk, "DivisionText", 42f, qInk,
                                      DivisionTextR.W, 10f);
            // 🆕 2026-10-03：原版这行 TMP `charSpacing = -2.6`（`资料/阶段二_战斗入口_原版规格.md:175`）。
            //    ⚠️ 这一段**本来就留空**（段位名是服务器数据、用户口径「不编数字」）⇒ 现在设了也看不见，
            //    但**值照原版设上**，将来一旦填字就是对的（别改成「反正看不见就不设」）。
            if (dtLbl != null) dtLbl.SetCharSpacing(-2.6f);

            // ---- ② 段位大图 `DivisionImage` → `RankImage`：**两张都不画**（判据 ⑤）----
            //    原版这一支的 sprite 是 `RankedDivisions.GetDivisionData(n).Image` 运行期塞的，
            //    而那张 SO **本地没有** ⇒ 画任何一张都是**编一个段位**。
            var divImage = MenuDraw.Node(content, "DivisionImage", DivisionImageR);
            MenuDraw.Node(divImage, "RankImage", RankImageR);

            // ---- ③ `Spacer`（纯 `LayoutElement`，没有图）----
            MenuDraw.Node(content, "Spacer", SpacerR);

            // ---- ④ `footer` → `MainRating` → `Mission Milestones Progress` ----
            //    ⚠️ `footer` 的**真高按 `MainRating` 取**（判据 ⚠️：工具对嵌套布局组不递归，算成 0）
            var footer = MenuDraw.Node(content, "footer", MainRatingR);
            MenuDraw.Rect(footer, art(ArtBanner), MainRatingR, "MainRating", qArt, null, false);

            var ms = MenuDraw.Node(footer, "Mission Milestones Progress", MilestonesR);
            // `Background`：**比父宽**（511.24 vs 410，左右各溢 49.38）—— 原版就那样，**别「对齐」掉**（§A·4 第 ③）
            MenuDraw.Rect(ms, art(ArtBanner), MsBackgroundR, "Background", qArt, MsBgTint, false);
            // `counter`（里程碑计数 `16`）**出厂 inactive + 全 bundle 零引用者** ⇒ 不建
            var steps = MenuDraw.Node(ms, "steps", StepsR);
            for (int i = 0; i < StepX.Length; i++)
            {
                var sr = new PxRect(StepX[i], StepY, StepX[i] + StepW, StepY + StepH);
                var step = MenuDraw.Node(steps, i == 0 ? "RankedSealStep" : "RankedSealStep (" + i + ")", sr);
                // `RankedSealStep` 自己那张 `Rank Skull` 的 **alpha = 0**（正本 §A 第 84 行）⇒ 不画；
                // 看得见的是子节点 `Empty`（`Rank Skull Empty`）。
                MenuDraw.Rect(step, art(ArtSkullEmpty), sr, "Empty", qTop, null, true);
                // `Fill`（`Rank Skull`）**不画**：那是「这一档达成了」的态，而进度是服务器数据（判据 ⑤）。
                MenuDraw.Node(step, "Fill", sr);
            }

            // ---- ⑤ `Legendary Ratings`（`Position` / `Global Rating` 两支）----
            var leg = MenuDraw.Node(footer, "Legendary Ratings", LegendaryR);
            MenuDraw.Node(leg, "Position", PositionR);
            MenuDraw.Node(leg, "Global Rating", GlobalRatingR);
            // 🔴 判据 ④ + ⑤：这两支在**预制体里出厂就是 inactive**，而 `displayPosition=1` / `displayRating=0`
            //    说的是**运行期**（有数据时才 `SetActive`）。本地一条数据都没有 ⇒ **两支都保持关着**。
            //    ⇒ 里面那些静态件（`#` 号、金奖杯）**不画** —— 画了就是「有分数」的样子。
            //    它们各自的 `Main Icon` 还是 `m_Enabled = 0` 的件（全 bundle 唯一两处），本来就不该画。
            //    （`Position`/`Global Rating` 的 rect 已建：将来有数据时按 `RankingDisplay.Initialize` 填。）

            Debug.Log("[RankedDivisionInfo] 段位块 `Content` 建完 —— **数据全留空**（段位名 / 两个评分 / 里程碑进度 / 段位大图"
                      + "都是服务器数据，用户 2026-09-26 口径：不编数字）。`Timer` **不建**（赛季倒计时）；"
                      + "`counter` 出厂 inactive 不建；`Fill` 与两个 `Main Icon` 不画。");
        }

        /// <summary>这一棵里**我们建出来的**节点名（自检按它逐条数结构，**别让断言去猜**）。
        /// 与 `Build` 里 `MenuDraw.Node` 的调用**一一对应**（`Content` 那一支的全部；`Rank Title` 与两个钮
        /// 在 `RankedEventWindow.BuildLeftColumn` 里建，不在本表）。</summary>
        public static readonly string[] StaticNodes =
        {
            "Content", "RankTitleBG", "DivisionText", "DivisionImage", "RankImage", "Spacer",
            "footer", "MainRating", "Mission Milestones Progress", "Background", "steps",
            "RankedSealStep", "RankedSealStep (1)", "RankedSealStep/Empty", "RankedSealStep/Fill",
            "Legendary Ratings", "Position", "Global Rating",
        };

        /// <summary>`RankedSealStep` 的个数（照预制体：**2**）。</summary>
        public static int StepCount { get { return StepX.Length; } }
    }
}
