// B97_W2_Assert.cs —— 执行代理 `B97_W2`（第九十七会话）的断言
//
// ============================ 它盯的是什么（三件活，各有断言）============================
//   ① **开箱视频 `renderMode`**（账 `商店:95` 的残差之一）：原版 prefab
//      `bundle_menus_assets_all/VideoPlayer/VideoPlayer_-1726031322328607103.json` 的 `m_RenderMode = 1`
//      = `UnityEngine.Video.VideoRenderMode.CameraNearPlane`（判据两处：
//      `资料/完全复刻/判据/B96_商店95_开箱视频.md` §五·1 ＋ 本 build 的枚举值
//      `d:/2/tools/il2cpp_out/dump.cs:1127184-1127188`：`CameraFarPlane=0 · CameraNearPlane=1 ·
//      RenderTexture=2 · MaterialOverride=3 · APIOnly=4`）+ `m_TargetCamera = null`（`Awake` 设成 `Camera.main`）。
//      ⚠️ 本 build 的**枚举顺序和 Unity 公开文档那份不一样** —— 所以断言比的是**枚举名**，⛔ 不比数字。
//   ② **开箱视频的容器 = 真 MP4**（`Gacha Crate Opening.mp4`，`ftyp mp42` / H.264 / 5,141,522 B /
//      md5 `fac0d41686dc00dcf4f055364c099895`）⇒ 落地名必须是 `.mp4`，⛔ 别照 `intro/defeat/draw/victory`
//      那四支改名成 `.webm`（判据 → 同上一份 §一/§二）。这一条**没有可断言的运行时面**
//      （`Resources.Load` 不带扩展名），只由注释订正承载 ⇒ 本节不设断言，见报告。
//   ③ **`FactionScore` ↔ `RankedTab` 的接线**（账 `未分类/其他:295`）：`FactionScore.Initialize` 从「只有断言在调」
//      变成 `RankedTab.BuildRow` 的**生产调用点**，且空分数档由 `InitializeOrEmpty` 落原版空态串 `------`。
//
// ============================ 怎么用（宿主）============================
// `B97W2Assert.Run();`
//   + 宿主里两行（照 `B94B17Assert` / `B94B5_Assert` 的先例）：
//     `_sink.Pass += B97W2Assert.Pass; _sink.Fail += B97W2Assert.Fail;`
//   · 本文件**不建持久资产**、不碰 `WindowsManager`（`RankedTab` 只按**反射**验它的接线口，
//     不建整页 —— 整页要宿主窗，属 `ProfileWindowAssertE12` 的地盘）。
//   · 建的 `GameObject` 全在 `finally` 里 `DestroyImmediate`；`RankedTab` 的两个静态注入点在 `finally` 里还原。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · C2：`renderMode` 不设 ⇒ Unity 出厂值是 `RenderTexture`（本 build = 2）⇒ C2 红。
//         （⛔ 别拿「字段存在」当断言 —— 那证明不了它被设过。）
//   · A5/A6：`InitializeOrEmpty` 若直接 `Initialize(faction, cur ?? 0, high ?? 0)` 而不落空态 ⇒ 文本是 `"0"` ⇒ 红。
//   · A9/A10：若把「任一颗为 null」写成「两颗一起跳过」⇒ A9 红（单边有值那档）。
//   · A3/A4：若 `Initialize` 把两颗写反（`current↔highest`）⇒ 两条一起红。
//   · B2：`FactionOf` 若直接 `(CardArmy)0` 而不查表 ⇒ `"Ultramarines"` 变 `Neutral` ⇒ 红。
//   · B5：`FactionScoreText` 若不再走 `FactionScoreValue`（两处各读一遍 provider）⇒ 改动 provider 后两条分叉 ⇒ 红。
using System.Reflection;
using CardPresentation;
using SB = CardPresentation.ScoreBars;
using UnityEngine;
using UnityEngine.Video;

/// <summary>执行代理 `B97_W2`：开箱视频 `renderMode`（`CameraNearPlane`）+ `FactionScore` ↔ `RankedTab` 的接线。
/// <para>⛔ **期望值全是字面量**（`"1500"` / `"------"` / `CardArmy.Ultramarines` / `CameraNearPlane`），
/// 不从被测实现里读回来 —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B97W2Assert
{
    static readonly CheckSink _sink = new CheckSink("[W2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W2 开箱视频 `renderMode` / `FactionScore`↔`RankedTab` 接线");
        SectionA_FactionScore接线口();
        SectionB_RankedTab接线口();
        SectionC_开箱视频RenderMode();
        Debug.Log(_sink.P + "B97_W2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A `FactionScore.Bind` / `InitializeOrEmpty`
    static void SectionA_FactionScore接线口()
    {
        MenuCheck.Section(_sink, "A `FactionScore.Bind` / `InitializeOrEmpty`：三句的唯一实现 + 「没有分数」那一档");

        var root = new GameObject("W2_FS", typeof(RectTransform));
        try
        {
            var at = new PxRect(0f, 0f, SB.FactionScore.SmallSize.x, SB.FactionScore.SmallSize.y);
            var fs = SB.FactionScore.Create(root.transform, at, SB.FactionScoreVariant.Small);
            var ard1 = fs.RatingDisplay;
            var ard2 = fs.HighestRatingDisplay;
            Tr(ard1 != null && ard2 != null, "A1 前提：`Create` 建出了两颗 `AllianceRatingDisplay`（不然下面全空转）");

            // 宿主那一半：两颗评级各自的文字件（生产里由 `RankedTab.RatingRow` 建，这里按同名同形建）
            var l1 = ard1 != null
                ? MenuDraw.Text(ard1.transform, new PxRect(0f, 0f, 200f, 40f), "x", Color.white,
                                "Individual rating value", 40f, 0)
                : null;
            var l2 = ard2 != null
                ? MenuDraw.Text(ard2.transform, new PxRect(0f, 0f, 200f, 40f), "x", Color.white,
                                "Individual rating value", 40f, 0)
                : null;
            Tr(l1 != null && l2 != null, "A2 前提：两颗的 `Individual rating value` 建出来了（`Label` 是真文字件）");
            if (l1 == null || l2 == null) return;
            ard1.Bind(l1, null);
            ard2.Bind(l2, null);

            // ---- 原版那三句的**本体**（不加壳）
            fs.Initialize(CardArmy.Ultramarines, 1500, 2000);
            C(l1.Text, "1500", "A3 ★ `Initialize` 第 2 句：第 1 颗 = `currentScore`（原版 `ratingDisplay.Initialize(currentScore, …)`）");
            C(l2.Text, "2000", "A4 ★ `Initialize` 第 3 句：第 2 颗 = `highestScore`");

            // ---- 没有分数那一档：必须落**原版空态串**，⛔ 不是 "0"
            fs.InitializeOrEmpty(CardArmy.Ultramarines, null, null, "------");
            C(l1.Text, "------", "A5 ★ 两颗都没有分数 ⇒ 空态串 `------`（⛔ 不是 `0` —— 那是「有数据且为 0」）");
            C(l2.Text, "------", "A6 …第 2 颗同（空态那一档必须**两颗都**落）");

            // ---- 有值 ⇒ 覆盖回真数字（证明 A5/A6 那条不是「一律印空态」）
            fs.InitializeOrEmpty(CardArmy.Ultramarines, 42, 99, "------");
            C(l1.Text, "42", "A7 ★ 有值 ⇒ 写真分数（⛔ 别把有值的一起盖成空态）");
            C(l2.Text, "99", "A8 …第 2 颗同");

            // ---- 单边缺失：两颗**各自**判，不联动
            fs.InitializeOrEmpty(CardArmy.Ultramarines, 7, null, "------");
            C(l1.Text, "7", "A9 ★ 只有当前分 ⇒ 第 1 颗 = 7（把「任一颗 null ⇒ 两颗都跳」写出来 ⇒ 这条红）");
            C(l2.Text, "------", "A10 …第 2 颗独立落空态");

            // ---- `Bind` 真的能把宿主建的那几件灌进去（不是只写字段没人读）
            var node3 = MenuDraw.Node(root.transform, "W2_ARDRaw", new PxRect(0f, 130f, 448.89f, 240.91f));
            var ard3 = node3.gameObject.AddComponent<SB.AllianceRatingDisplay>();
            var l3 = MenuDraw.Text(node3, new PxRect(0f, 0f, 448.89f, 110.91f), "x", Color.white,
                                   "Individual rating value", 90f, 0);
            Tr(l3 != null, "A11 前提：另建一颗独立评级件（验 `Bind` 换手之后写的就是新的那颗）");
            if (l3 != null)
            {
                ard3.Bind(l3, null);
                fs.Bind(fs.IconNode != null ? fs.IconNode.GetComponentInChildren<ImageQuad>() : null, ard1, ard3);
                fs.InitializeOrEmpty(CardArmy.Ultramarines, 11, 22, "------");
                C(l1.Text, "11", "A12 ★ `Bind` 之后 `Initialize` 写的就是**宿主那一颗**（第 1 颗 = 11）");
                C(l3.Text, "22", "A13 …第 3 颗 = 22（`Bind` 换了 `highestRatingDisplay` 之后，写的是**新绑的那颗**）");
                C(l2.Text, "------", "A14 …**解绑**的那颗不再被写（`l2` 停在上一档的 `------`）");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ------------------------------------------------------------ B `RankedTab` 的三个接线口
    static void SectionB_RankedTab接线口()
    {
        MenuCheck.Section(_sink, "B `RankedTab` 接线口：`FactionOf` / `ArdOf` / `FactionScoreValue`（`BuildRow` 靠这三个）");

        // ---- B1..B3 `FactionOf(string)` → `CardArmy`
        var mFactionOf = typeof(RankedTab).GetMethod("FactionOf", BindingFlags.NonPublic | BindingFlags.Static);
        Tr(mFactionOf != null, "B1 `RankedTab.FactionOf(string)` 在（`BuildRow` → `FactionScore.Initialize` 的实参转换）");
        if (mFactionOf != null)
        {
            C((CardArmy)mFactionOf.Invoke(null, new object[] { "Ultramarines" }), CardArmy.Ultramarines,
              "B2 ★ 阵营名 → `CardArmy`（`" + "Ultramarines" + "` ⇒ `Ultramarines` 10；⛔ 别拿 0 兜底）");
            C((CardArmy)mFactionOf.Invoke(null, new object[] { "NotAnArmy" }), CardArmy.Neutral,
              "B3 认不出的阵营名 ⇒ `Neutral`(0)（= 原版 `ArmyIconsSO` 里没有那一格 ⇒ 取图返回 null、⛔ 不顶替别人的徽记）");
            C((CardArmy)mFactionOf.Invoke(null, new object[] { null }), CardArmy.Neutral,
              "B4 …空串同（`null` 不允许炸）");
        }

        // ---- B5/B6 `FactionScoreValue` ⇄ `FactionScoreText` 同源
        var mValue = typeof(RankedTab).GetMethod("FactionScoreValue", BindingFlags.NonPublic | BindingFlags.Static);
        Tr(mValue != null, "B5 `RankedTab.FactionScoreValue(int,bool)` 在（`FactionScoreText` 的唯一来源）");
        try
        {
            RankedTab.ResetFactionScoreForTest();
            if (mValue != null)
            {
                C((int?)mValue.Invoke(null, new object[] { 0, false }), (int?)null,
                  "B6 ★ 没有数据源 ⇒ `null`（= 生产态：服务器分数本地拿不到）");
            }
            C(RankedTab.FactionScoreText(0, false), (string)null,
              "B7 `FactionScoreText` 没有数据源 ⇒ `null`（`B93S3Assert` G1 钉的就是这条，⛔ 别改回 `\"0\"`）");

            RankedTab.FactionScoreProvider = r => 1500 + r;
            RankedTab.FactionHighestProvider = r => 2000 + r;
            if (mValue != null)
            {
                C((int?)mValue.Invoke(null, new object[] { 0, false }), (int?)1500,
                  "B8 ★ 有 provider ⇒ `1500`（行下标原样传下去）");
                C((int?)mValue.Invoke(null, new object[] { 3, true }), (int?)2003,
                  "B9 …第 2 颗吃 `FactionHighestProvider`、行下标 3 ⇒ 2003");
            }
            C(RankedTab.FactionScoreText(3, true), "2003",
              "B10 ★ `FactionScoreText` 与 `FactionScoreValue` **同源**（两处各读一遍 ⇒ 这条与 B9 会分叉）");
        }
        finally { RankedTab.ResetFactionScoreForTest(); }
        C(RankedTab.FactionScoreText(0, true), (string)null, "B11 收尾：注入点已还原（`ResetFactionScoreForTest`）");

        // ---- B12..B14 `ArdOf`：把 `RatingRow` 建的那两件包成组件
        var mArdOf = typeof(RankedTab).GetMethod("ArdOf", BindingFlags.NonPublic | BindingFlags.Static);
        Tr(mArdOf != null, "B12 `RankedTab.ArdOf(Transform,string)` 在（`BuildRow` 用它把 `RatingRow` 的节点包成组件）");
        if (mArdOf == null) return;

        var root = new GameObject("W2_ARD", typeof(RectTransform));
        try
        {
            // 仿 `RatingRow` 建出来的形状：一个同名节点 + `Main Icon`(ImageQuad) + `Individual rating value`(Label)
            var node = MenuDraw.Node(root.transform, "Alliance Rating Display", new PxRect(0f, 0f, 448.89f, 110.91f));
            var q = MenuDraw.Rect(node, CardArt.Solid(), new PxRect(0f, 0f, 151.82f, 110.91f), "Main Icon", 0, null);
            var lbl = MenuDraw.Text(node, new PxRect(151.82f, 0f, 448.89f, 110.91f), "------", Color.white,
                                    "Individual rating value", 90f, 0);
            Tr(q != null && lbl != null, "B13 前提：仿 `RatingRow` 那两个子件建出来了");

            var ard = (SB.AllianceRatingDisplay)mArdOf.Invoke(null, new object[] { root.transform, "Alliance Rating Display" });
            Tr(ard != null, "B14 ★ `ArdOf` 认得出那颗节点并挂上 `AllianceRatingDisplay`");
            if (ard != null)
            {
                Tr(ard.RatingText != null, "B15 ★ …`ratingText` 绑到了 `Individual rating value`（原版 `+0x20`）");
                Tr(ard.RatingIcon != null, "B16 ★ …`ratingIcon` 绑到了 `Main Icon`（原版 `+0x28`）");
                ard.Initialize(321, null, false, false, true);
                C(lbl.Text, "321", "B17 …绑对了：`Initialize` 真写到那颗文字件上");
            }
            // 找不到的节点 ⇒ null（⛔ 不是「建一颗新的」）
            C(mArdOf.Invoke(null, new object[] { root.transform, "No Such Node" }), (object)null,
              "B18 节点名对不上 ⇒ 返回 `null`（⛔ 别兜底建一颗 —— 那会多出一颗同名节点）");
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ------------------------------------------------------------ C 开箱视频 `renderMode`
    static void SectionC_开箱视频RenderMode()
    {
        MenuCheck.Section(_sink, "C 开箱视频 `VideoPlayer.renderMode` = 原版 prefab 的 `m_RenderMode = 1`（`CameraNearPlane`）");

        var go = new GameObject("W2_Crate");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            PointerLayer.SetInputEnabled(true);
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception ex)
            {
                Tr(false, "C1 前提：`GachaEventWindow.Build()` 跑得完（炸了：「" + ex.GetType().Name + " " + ex.Message + "」）");
            }
            if (!built) return;

            var vp = win.CrateVideoPlayer;
            Tr(vp != null, "C2 前提：`Video Player` 上真挂了 `VideoPlayer` 组件");
            if (vp == null) return;

            C(vp.renderMode, VideoRenderMode.CameraNearPlane,
              "C3 ★ `renderMode = CameraNearPlane`（原版 `VideoPlayer_-1726031322328607103.json` 的 `m_RenderMode = 1`；"
            + "本 build 的枚举名↔值判据 `dump.cs:1127184-1127188`）｜🧨 不设 ⇒ Unity 出厂 `RenderTexture` ⇒ 这条红");
            Tr(vp.renderMode != VideoRenderMode.RenderTexture,
              "C4 ★ 不是出厂默认的 `RenderTexture`（灭自证：只比「字段存在」证明不了它被设过）");
            C(vp.enabled, false,
              "C5 原版 `Awake` 第 ① 句 `videoPlayer.enabled = false` 仍然成立（本批没把它碰坏）");
        }
        finally
        {
            PointerLayer.SetInputEnabled(true);      // 收尾还输入层（同 `ShopFamilyAssertE13` 的口径）
            Object.DestroyImmediate(go);
        }
    }
}
