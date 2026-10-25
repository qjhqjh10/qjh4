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
// ⑤ 🔴 **这一整棵的数据都是服务器数据**（段位名 / 两个评分 / 里程碑进度 / 段位大图）⇒ 照用户 2026-09-26 的口径
//    **不编数字**：两个评分留空 · `RankedSealStep` **保留 `Empty` 态、不画 `Fill`**（进度未知）。
//    ⚠️ 段位图**不是「我们没导」**—— 是「段位号 → 图名」那张对照表（`RankedDivisionsSO`）**本地就没有**
//    （判据 → `资料/普查产出_0927/档案窗_Ranking页与图名表.md` §D·1，全盘搜过）。
//    🔴 **2026-10-24（E19）就地订正**：上面那句原文还写着「`DivisionText` 留空 · `RankImage` 与 `DivisionImage` 不画」——
//       **那一半已过期**（用户点名要段位徽章）：现在按**演示段位号 7**画 `DivisionImage`（`07-Legend`）
//       + 官方词条 `Ranked/Division/7`（zh `传奇`）。**段位号是演示值、号码→图名是推断**（两张落点都在
//       `DivisionArt` / `DemoDivisionNumber` 的 doc 里）；`RankImage`（罗马数字名次）**仍然不画**
//       —— 名次号也在服务器，且「最高段位叠不叠名次」参考图判不了（S4 §缺数据）。
//       🔴 `Build` 里那颗节点现在是 **`SetActive(false)`**（原版本子口径见 `Build` ③ 那段的注释）。
// ⑥ 🔴 **2026-10-24（E19）就地订正**：原文写「**`Timer` 整块不建**（赛季倒计时：服务器数据 + 用户明确不要）」——
//    **已过期**：`Timer` 自 **2026-10-22（M1）** 起由宿主的 `RankedEventWindow.BuildRankTimer` 建在
//    `Content` 底下（倒计时文案走官方词条 `EndsInPlusOrangeTime`，截止时刻本地没有 ⇒ 印 `--`）。
//    本类**不建它**（建法/判据在宿主那边，⛔ 别在这儿再写第二份）。
//    `counter`（里程碑计数）**出厂 inactive 且全 bundle 零引用者** ⇒ 不建；两个 `Main Icon` 是 `m_Enabled=0` 的件 ⇒ 不画。
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

        // ------------------------------------------------------------ 段位徽章（2026-10-24 · E19）
        /// <summary>段位大图那 7 张的**资源名**（`Resources/Art/ui_menu/`，导入时已把空格换成下划线）。
        /// <para>🔴🔴 **2026-10-25（执行代理 D · 铁律 5 就地订正）：本表【不再是前缀推断】，每一行都有第一手字节证据。**
        /// 上一条注释写的是「`RankedDivisions` 全盘一个实例都没有」—— **那条已作废**：SO 就在
        /// `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/RankedDivisionsSO.asset`
        /// （`S4` 当时只扫了 `assets_full` 那 84 个 bundle，**没扫这份 AssetRipper 导出** ⇒ 记成了「没有」）。</para>
        /// <para>**证据链（可逐字节复算）**：
        /// ① `RankedDivisionsSO.asset` 的 `divisions` = **7 条** `{ divisionNumber: 1..7, image.m_AssetGUID: &lt;guid&gt; }`
        ///    （`ranks` = 6 条，`divisionNumber: 1..6`，同形）；
        /// ② 每张 Sprite 的 YAML 里 `m_RenderDataKey: { &lt;guid&gt;: 21300000 }` —— 那个 guid **就是它自己的源资产 GUID**，
        ///    与 ① 的 `m_AssetGUID` 逐条相等 ⇒ `divisionNumber → Sprite` 是**查表直读**，不是按名字排序推的；
        /// ③ 该 Sprite 的 `m_Name` 就是下表那一串（`06-Galactic Threat` 原名带空格，进 `Resources` 时换成下划线）。</para>
        /// <para>逐号对应（GUID 前 8 位 → 资产名）：
        /// 1=`541a2e07…`→`01-Rook` · 2=`cfab7727…`→`02-Veteran` · 3=`80afab2f…`→`03-Commander` ·
        /// 4=`c8be32f4…`→`04-Admiral` · 5=`225606b2…`→`05-Conqueror` · 6=`52405d32…`→`06-Galactic Threat` ·
        /// 7=`b075dd3b…`→`07-Legend`。</para>
        /// <para>旁证（与上表**互不依赖**，2026-10-24 `A1` 的逐块像素比对，仍然成立）：
        /// **4 = `04-Admiral`** —— 用户参考图「当前排名 **金 II**」那颗徽记 NCC **0.707**（次优 0.577）；
        /// **7 = `07-Legend`** —— 「多人模式-经典」参考图左列 NCC **0.939**（次优 0.751）。
        /// ⇒ 两条独立信号与查表结果**同指**。逐行证据表 → `资料/普查产出_1024c/A1_段位图案映射.md`。</para>
        /// <para>⚠️ 反编译口径不变：`GetDivisionData(n)` = `Math.Max(n,1)` + `FirstOrDefault(divisionNumber == n)`，
        /// 查不到退回 `Last()`（`RankedDivisions__GetDivisionData.c`）⇒ 越界号会被夹到 1 / 落到最后一条。
        /// 本表只表达 1..7，越界由 <see cref="DivisionArtName"/> 返回 null（调用方出声、不画）。
        /// `RankedDivisionData` 只有 `divisionNumber(+0x10)` + `image(+0x18)` 两个字段（dump 现读）。</para></summary>
        static readonly string[] DivisionArt =
        {
            "01-Rook", "02-Veteran", "03-Commander", "04-Admiral",
            "05-Conqueror", "06-Galactic_Threat", "07-Legend",
        };

        /// <summary>段位号的**演示值**。🔴 **这是我们挑的，不是原版的真实段位**：
        /// 玩家真实段位来自服务器（`RankedEventWindowV2__SetDivision.c:67-79`：
        /// `LeaderboardManager.GetCachedLeaderboard(…)` 拿到的项 `+0x20` 里的 divisionNumber
        /// → `RankingDisplay.Initialize(…, divisionNumber, …)`），本地一条都没有。
        /// 取 **7（Legend）** 的唯一理由：与用户参考图 `C:/Users/qjh36/Desktop/warpforge参考图/经典模式参考图.png`
        /// 那一块**同款**（S4 逐像素认定 = `07-Legend`）。⚠️ 真数据到了只改这一个数。</summary>
        public const int DemoDivisionNumber = 7;

        /// <summary>段位号 → 图名（越界 / 表里没有 ⇒ null，调用方出声、不画）。</summary>
        public static string DivisionArtName(int divisionNumber)
        {
            int i = divisionNumber - 1;
            return (i >= 0 && i < DivisionArt.Length) ? DivisionArt[i] : null;
        }

        // ------------------------------------------------------------ 名次（罗马数字）· 2026-10-24（A1）
        /// <summary>**名次（小档）**那 6 张 —— ⛔ **不是段位**，是「同一段位内的第几小档」。
        /// <para>🔴 **2026-10-25（执行代理 D）：本表的「号 → 图」也已升级成字节证据**（与 `DivisionArt` 同一条链）：
        /// `RankedDivisionsSO.asset` 的 `ranks` = 6 条 `{ divisionNumber: 1..6, image.m_AssetGUID }`，
        /// 与每张 Sprite 自己的 `m_RenderDataKey.guid` 逐条相等 ⇒
        /// 1=`46c79894…`→`Roman I` · 2=`f4304ddc…`→`Roman II` · 3=`ee72f98c…`→`Roman III` ·
        /// 4=`bbe7723d…`→`Roman IV` · 5=`06f63a9c…`→`Roman V` · 6=`49724dc8…`→`Roman VI`
        /// （原名带空格，进 `Resources` 时换成下划线）。</para>
        /// <para>其余判据（2026-10-24 · A1，仍然成立）：
        /// ① 反编译 `DivisionImageDisplay__Initialize.c` —— 最高段位那一支里
        ///   `RankedDivisions.GetRankData(rankNumber)` 取返回项 `+0x18` 的 `AssetReference` → `Load` →
        ///   `Image.set_sprite(rankImage)`；而 `RankedDivisions__GetRankData.c` 与 `GetDivisionData` **同构**
        ///   （`RankedDivisions` 两张表 = `divisions`(+0x18) / `ranks`(+0x20)，元素**同一个类** `RankedDivisionData`）。
        /// ② 官方词条 `Ranked/Rank/1..6` = `I…VI`（**6 条，与 6 张图同数**；`官方本地化_en+zh-CN.json` 现读）。
        /// ③ 用户参考图 `账户-个人资料参考图.png`：徽记中央印着罗马数字（低置信，被骷髅压住），
        ///   而同一块的标题写的就是「**金 II**」⇒ 名次数字与段位名是**并存的两层**。</para>
        /// ⚠️ **本表只记录结论，今天不画外部**（`RankImage` 仍关着 —— 名次号来自服务器，且「最高段位叠不叠名次」
        /// 参考图判不了，S4 §缺数据）。将来有真数据只改调用点，⛔ 别动这张表。</summary>
        static readonly string[] RankArt =
        {
            "Roman_I", "Roman_II", "Roman_III", "Roman_IV", "Roman_V", "Roman_VI",
        };

        /// <summary>名次号 → 图名（越界 ⇒ null）。⚠️ 今天**没有画它的调用点**（见 `RankArt` 的文档）。</summary>
        public static string RankArtName(int rankNumber)
        {
            int i = rankNumber - 1;
            return (i >= 0 && i < RankArt.Length) ? RankArt[i] : null;
        }

        /// <summary>段位表长度（= **7**，原版 7 大段位）。自检读它，⛔ 别在断言里写死 7。</summary>
        public static int DivisionArtCount { get { return DivisionArt.Length; } }

        /// <summary>名次表长度（= **6**，官方 `Ranked/Rank/1..6`）。</summary>
        public static int RankArtCount { get { return RankArt.Length; } }

        /// <summary>段位名 = 官方词条 `Ranked/Division/&lt;号&gt;`（zh `传奇` / en `Legendary`；现读）。
        /// ⚠️ **不自己编中文名** —— 词条取不到（`Loc.T` 缺键会**返回键名本身**）⇒ 返回空串 + 出声。</summary>
        public static string DivisionName(int divisionNumber)
        {
            string key = "Ranked/Division/" + divisionNumber;
            string v = Loc.T(key);
            if (string.IsNullOrEmpty(v) || v == key)
            {
                Debug.LogWarning("[RankedDivisionInfo] 官方词条 `" + key + "` **取不到** ⇒ 段位名留空"
                                 + "（⛔ 不编名字；官方表现读 1..7 = Iron/Bronze/Silver/Gold/Platinum/Diamond/Legendary）");
                return "";
            }
            return v;
        }

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
            // 🔴 段位名（原版样板是 `Division V`）**是服务器数据**（`RankingDisplay.Initialize` 从
            //    `RankedDivisions` 那条约里取 `LocalizedString` 塞进这颗 TMP）。
            //    🔴 **2026-10-24（E19）**：改前**恒留空**；现在按**演示段位号**取官方词条
            //    （`Ranked/Division/<号>`，zh `传奇`）—— 号本身是**我们挑的演示值**（见 `DemoDivisionNumber`），
            //    真值在服务器。词条取不到时 `DivisionName` 出声并返回空串（⛔ 不编名字）。
            var dtLbl = MenuDraw.Text(content, DivisionTextR, DivisionName(DemoDivisionNumber), DivisionInk,
                                      "DivisionText", 42f, qInk,
                                      DivisionTextR.W, 10f,
                                      // 🆕 **2026-10-12（A336③）**：上限 = 原版 `m_fontSizeMax` = **42**（= 标称，本来就对）·
                                      //   base = 原版 `m_fontSizeBase` = **23.0**。
                                      //   判据 = 全库唯一那颗 `m_characterSpacing = -2.6` + `fs 42.0` 的 TMP
                                      //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_7569753382547457968.json`
                                      //   = `fs 42.0 · auto[10.0~42.0] · base 23.0`（「-2.6 字距」是本节点唯一的名片，
                                      //   见上面那句 `SetCharSpacing(-2.6f)`；`menu_dump.py` 不印 base 这一列）。
                                      42f, 23f, face: TmpFont.Face.LatinUi);
            // 🆕 2026-10-03：原版这行 TMP `charSpacing = -2.6`（`资料/阶段二_战斗入口_原版规格.md:175`）。
            //    ⚠️ 这一段**本来就留空**（段位名是服务器数据、用户口径「不编数字」）⇒ 现在设了也看不见，
            //    但**值照原版设上**，将来一旦填字就是对的（别改成「反正看不见就不设」）。
            if (dtLbl != null) dtLbl.SetCharSpacing(-2.6f);

            // ---- ② 段位大图 `DivisionImage` → `RankImage` ----
            // 🔴 **2026-10-24（E19 · 用户问「什么等级用什么徽章」）**：改前这里**两张都不画**
            //   （「画任何一张都是编一个段位」）。现在按**演示段位号**画 —— 段位号是演示值（`DemoDivisionNumber`），
            //   「号 → 图」是**按图名前缀推断**的（`DivisionArt` 那段 doc 里写清了这是推断 + 为什么）。
            //   `keepAspect = true`：prefab 里这颗 Image 的 **`m_PreserveAspect = 1`**、`m_Type = 0`（Simple）——
            //   8 处同名 `DivisionImage` 逐颗现读都是这两个值（`bundle_menus_assets_all/MonoBehaviour/…`）。
            //   ⚠️ 下图 512²、框 539.79×572.62 ⇒ 按比例内接成 **539.79²** 居中（不是拉满 572.62）。
            var divImage = MenuDraw.Node(content, "DivisionImage", DivisionImageR);
            string divArtName = DivisionArtName(DemoDivisionNumber);
            // 🔴 **队列 = `qArt − 1`（不是 `qArt`）**：原版兄弟序是 `RankTitleBG` → **`DivisionImage`** → `footer`
            //   （`menu_dump` 那张表的行序 = uGUI 的画序），而 uGUI 是**后建的画在上面** ⇒ 徽记**压在 banner 与
            //   footer 之下**（参考图 `经典模式参考图.png` 里「1600」就压在徽记下缘上 ⇒ 与这条一致）。
            //   我们这把梯子在 `qArt`(3111) 与下一档(3110) 之间**没有整数空档**（同队列谁盖谁不可控 ——
            //   透明物按到相机的距离排）⇒ 徽记只能落 `qArt − 1`。
            //   ⚠️ **已知代价（如实记）**：与中栏 `Warlord Darkening`(3110) 同档，只在徽记最右 ~59px
            //   （529.97…588.90）与它重叠；那一处是柔和渐变的左沿（参考图同一带也是暗的）⇒ 影响止于此。
            var divQ = string.IsNullOrEmpty(divArtName)
                     ? null
                     : MenuDraw.Rect(divImage, art(divArtName), DivisionImageR, "Image", qArt - 1, null, true);
            if (divQ == null)
                Debug.LogWarning("[RankedDivisionInfo] 段位大图**没画** —— `DivisionArt` 里没有段位 "
                                 + DemoDivisionNumber + " 对应的图（表长 " + DivisionArt.Length + "）"
                                 + "；⛔ 不拿别的段位顶替");
            // `RankImage`（罗马数字 I–VI，128²）：**不画**。判据两条都指向「本地判不了」：
            //   ① 名次号（`RankedDivisionData`/leaderboard 项里的 rank）也在服务器
            //      （`DivisionImageDisplay__Initialize.c:62` 的 `RankedDivisions.GetRankData(rank)`）；
            //   ② `DivisionImageDisplay` 只在**最高段位**那一支才 `SetActive(RankImage, true)`，而参考图里
            //      罗马数字正压在骷髅脸上 ⇒ **Legend 到底叠不叠名次无法判定**（S4 §缺数据 明确存疑）。
            //   ⇒ 节点照建（原版有这一颗），但不出图 —— 不拿一个猜的名次去画。
            // 🔴 **2026-10-24（第七十三会话 E12 · 账 `A1 B·3b`）**：「不出图」光靠 `MenuDraw.Node`（只建
            //   RectTransform、没有 `ImageQuad`）凑不住 —— **`activeSelf` 仍然是真的**，谁来给这颗挂一张图
            //   就会默认显示。与排名页同一口径：`RankedTab.cs` 建同名字点后 **`SetActive(false)`**
            //   （`RankedTab.cs:440-441`），此处照办（两扇事件窗 `RankedEventWindow` / `SkirmishEventWindow`
            //   都走这个 builder ⇒ 一句同时收口）。已核不会误伤 `EventWindowDetailAssertE19.cs:177`：
            //   那里只用 `Find(...) != null` 断「节点照建」，而 `MenuCheck.FindChild` 走
            //   `GetComponentsInChildren<Transform>(true)` ⇒ **含 inactive**。
            var ri = MenuDraw.Node(divImage, "RankImage", RankImageR);
            ri.gameObject.SetActive(false);

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

            Debug.Log("[RankedDivisionInfo] 段位块 `Content` 建完 —— 两个评分 / 里程碑进度 / 名次图"
                      + "**留空**（都是服务器数据，用户 2026-09-26 口径：不编数字）；"
                      + "段位名与段位大图**按演示段位号 " + DemoDivisionNumber + " 画**"
                      + "（号是我们挑的演示值；号→图 **4/7 两档已实测钉死**、其余五档按图名序号 —— 见 `DivisionArt` 的 doc）。"
                      + "`Timer` 由宿主的 `BuildRankTimer` 另建；`counter` 出厂 inactive 不建；"
                      + "`Fill` 与两个 `Main Icon` 不画。");
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
