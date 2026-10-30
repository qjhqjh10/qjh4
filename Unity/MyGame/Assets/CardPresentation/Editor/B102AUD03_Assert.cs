// B102AUD03_Assert.cs — 执行代理 `B102_AUD03`（第一百零二会话 · `AUD02` 普查查出的 **B** 档：排行榜行字距缺失）
//
// ============================ 它盯的是什么 ============================
//  判据全文 → `资料/完全复刻/报告/B101_P2_AUD02序列化非视觉参数.md` §3.4 / §4.2
//  逐条数据 → `资料/完全复刻/判据/AUD02_serialized_params.tsv`
//
//  · 原版 `AllianceRankingRow Variant ▸ Name Holder/{Name, Guild Name}` 两颗 TMP 的
//    **`m_characterSpacing = 1.2`**；我们 `Shell/LeaderboardRow.cs` 的 `Build` 只给了字号，**没调 `SetCharSpacing`**
//    ⇒ 那两行字是「挤着」画的（字距 0）。
//  · 🆕 2026-10-30 现读复核（`工具/menu_dump.py … "AllianceRankingRow Variant" --depth 6 --no-sprite`）：
//    `Name` 与 `Guild Name` 两行都印 `字距=1.2`；同一次 dump 的 `Ranking` / `Points` **没有这一列**（= 0）。
//  · 🔑 **四款行族逐值相同**：`PlayerRankingRow` / `PlayerRankingRow For Army` / `AllianceRankingRow Variant` /
//    `AllianceRankingRow Skulls Variant` 的这两颗**全是 1.2** ⇒ 唯一那份 builder 里**无条件**加（⛔ 不按行族分支）。
//  · 修法落点 = `Shell/LeaderboardRow.cs` 的 `Name` / `Guild Name` 两处：
//    `{ lb.SetCharSpacing(NameGuildCharSpacing); MenuDraw.AlignLeft(lb, …); }`（**同一行**，行数零变化）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · **A1（★ 期望值）** 读的是 `Label.CharSpacing`（= TMP `_tmp.characterSpacing` 本身），期望值写**字面量 1.2**
//    —— ⛔ **不读** `LeaderboardRow.NameGuildCharSpacing`（那是**被测实现自己的常量**，读了就是同式自证：
//    把常量改成 1.3 也照样绿）。判据出处 = 原版 prefab 字段（dump 实读）。
//  · **A2（对照 / 灭自证）**：用**同一条** `MenuDraw.Text`（同样十一个实参）建一颗**不调 `SetCharSpacing`** 的标签，
//    断它的字距 = **0**。这一条把「A1 的 1.2 可能只是 `Label` 的出厂默认值」这条假绿堵死：
//    ① 把 `LeaderboardRow` 里那句 `SetCharSpacing` **删掉** ⇒ A1 红、A2 仍绿（**结构上不可能同时满足**）；
//    ② 反过来若谁把 `Label` 的出厂字距改成 1.2（让 A1 无因也绿）⇒ A2 红。
//  · **A3（四族覆盖 / 判别式）**：四族各建一行、逐族断 1.2 —— 若谁把 `SetCharSpacing` 塞进「只对联盟族生效」的分支
//    ⇒ `Player` / `PlayerForArmy` 两族红（原版四族都是 1.2）。
//  · **A4（别加过头 / 判别式）**：同一次 dump 里 `Ranking` / `Points` **没有字距** ⇒ 断它们仍是 **0**；
//    谁「顺手给整行四颗都加上」⇒ A4 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 本件**不需要** `MenuRuntime` / `Canvas` / 任何窗：它自己建一个临时根（`RectTransform`）、
//  直接用**生产同一条** `LeaderboardRow.Build(...)` 建一行、读完就 `DestroyImmediate` 整个根
//  ⇒ 9 个 `XxxScene.cs` 宿主里**任何一个**都能跑（`TmpFont` 可用即可；点阵后端会由 A0 前提显性红，不是假绿）。
//  推荐接在 `CardPresentation/Editor/MainMenuScene.cs` 那条聚合行（排行榜那几段就在本宿主里）**同一物理行**追加（行数零变化）：
//      B102AUD03_Assert.Run(); pass += B102AUD03_Assert.Pass; fail += B102AUD03_Assert.Fail;
//  ⚠️ 若接 `ShellScene.cs` 同样成立（本件不读任何宿主状态）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_AUD03`：排行榜行 `Name Holder/{Name, Guild Name}` 的字距 = 1.2（`AUD02` B 档）。</summary>
public static class B102AUD03_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102AUD03] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B102_AUD03：排行榜行 `Name Holder/{Name, Guild Name}` 的**字距 1.2**"
                                + "（原版 `m_characterSpacing`；AUD02 §4.2）");
        A_AllianceRowSpacing();
        B_FourFamilies();
        C_NotOnRankAndPoints();
    }

    // ==================================================================
    //  夹具：用**生产同一条** builder 建一行（⛔ 不另写一份几何）
    // ==================================================================
    /// <summary>一行建出来的东西（只留本件要量的四颗字）。</summary>
    struct Probe
    {
        public Transform Root, Row;
        public Label Name, Guild, Ranking, Points;
    }

    /// <summary>建一行（行矩形 = 原版 **1200×100**，写字面量、⛔ 不读 `LeaderboardRow.RowW/RowH` ——
    /// 那是**被测实现自己的常量**，拿它当夹具等于自证的一部分）。</summary>
    static Probe Build(LeaderboardRowFamily fam, string guild)
    {
        var p = new Probe();
        p.Root = new GameObject("B102AUD03/" + fam, typeof(RectTransform)).transform;
        // 取图一律给 null：本件只量字距，图缺失会由 `MenuDraw.Rect/Nine` 自己返回 null（空子件不建），不影响文字。
        var ctx = new RowCtx { Art = _ => null, Q = 3000 };
        LeaderboardRow.Build(ctx, p.Root, new PxRect(0f, 0f, 1200f, 100f),
                             new LeaderboardRowData { Rank = 1, Name = "Lorem Ipsum", Guild = guild, Points = "4500" },
                             fam);
        bool alliance = fam == LeaderboardRowFamily.Alliance || fam == LeaderboardRowFamily.AllianceSkulls;
        p.Row = p.Root.Find(alliance ? "AllianceRankingRow" : "PlayerRankingRow");
        var holder = p.Row != null ? p.Row.Find("Name Holder") : null;
        p.Name = LabelOf(holder, "Name");
        p.Guild = LabelOf(holder, "Guild Name");
        p.Ranking = LabelOf(p.Row, "Ranking");
        p.Points = LabelOf(p.Row, "Points");
        return p;
    }

    /// <summary>`parent/child` 那颗字（`Transform.Find` **只找直接子件** —— 本处两步（`Name Holder` / 它下面那两颗）
    /// 都是直接子件）。
    /// ⚠️ 原版 `Guild Name` 出厂是 **inactive**，而我们的 `Build` 在公会名为空时**连节点都不建**（判据 ④）
    /// ⇒ 夹具一律给**非空**公会名，节点一定在（`GetComponent` 对 active/inactive 都能取到，与激活状态无关）。</summary>
    static Label LabelOf(Transform parent, string child)
    {
        if (parent == null) return null;
        var t = parent.Find(child);
        return t != null ? t.GetComponent<Label>() : null;
    }

    static void Kill(ref Probe p)
    {
        if (p.Root != null) UnityEngine.Object.DestroyImmediate(p.Root.gameObject);
        p = new Probe();
    }

    // ==================================================================
    //  A 段：联盟行 —— 两颗字的字距 = 1.2 + 灭自证对照
    // ==================================================================
    static void A_AllianceRowSpacing()
    {
        var p = Build(LeaderboardRowFamily.Alliance, "[WF] Warpforge");
        try
        {
            // ---- A0 前提（**硬断言**，⛔ 不写 `if (前提) Check(结论)` 那种静默跳过）----
            T(p.Row != null, "A0（前提）`LeaderboardRow.Build` 建出了行节点（节点名照原版 = `AllianceRankingRow`）");
            T(p.Name != null, "A0（前提）行里有 `Name Holder/Name`、且挂着 `Label`");
            T(p.Guild != null, "A0（前提）行里有 `Name Holder/Guild Name`、且挂着 `Label`（夹具给的是**非空**公会名）");
            // TMP 后端才有「字距」这回事（`Label.SetCharSpacing` 在点阵后端只出声、不生效）⇒ 必须先证明走的是 TMP，
            // 否则下面量出来的 0 是**测法问题**，不是原版差异。这一条也挡住「字体资产加载不到 ⇒ 假红」。
            T(p.Name != null && p.Name.CanRenderChinese,
              "A0（前提）`Name` 走的是 **TMP 后端**（点阵后端没有字距，量出来的 0 会是假红）");
            T(p.Guild != null && p.Guild.CanRenderChinese, "A0（前提）`Guild Name` 同上（TMP 后端）");
            if (p.Name == null || p.Guild == null) return;

            // ---- A1 ★ 期望值（判据 = 原版 prefab 字段，期望值写**字面量**）----
            N(p.Name.CharSpacing, 1.2f, 1e-3f,
              "★ A1 `Name` 的字距 = **1.2**（原版 `AllianceRankingRow Variant ▸ Name Holder/Name` 的 `m_characterSpacing`；"
              + "`工具/menu_dump.py … --depth 6 --no-sprite` 实读 `字距=1.2`）");
            N(p.Guild.CharSpacing, 1.2f, 1e-3f,
              "★ A1 `Guild Name` 的字距 = **1.2**（同一 prefab 的 `…/Name Holder/Guild Name`；同一次 dump 也是 `字距=1.2`）");

            // ---- A2 灭自证：同参对照标签（**不调** `SetCharSpacing`）必须还是 0 ----
            // 走的是 `Build` 里 `Name` 那一句**逐字同参**（框 = `NameR` 的绝对矩形、字号 40 / min 18 / max 40 / base 36 /
            // `LatinCardBody`、队列 = `c.Q + L_Text`）—— 唯一的差别就是**少了那句 `SetCharSpacing`**。
            var ctl = MenuDraw.Text(p.Name.transform.parent, new PxRect(290f, 6f, 1000.79f, 52f), "Lorem Ipsum",
                                    Color.white, "B102AUD03_Control", 40f, 3000 + 4, 710.79f, 18f, 40f, 36f,
                                    face: TmpFont.Face.LatinCardBody);
            T(ctl != null && ctl.CanRenderChinese,
              "A2（前提）同参对照标签建出来了（同一条 `MenuDraw.Text` · 同样那一组实参 · 只是不调 `SetCharSpacing`）");
            if (ctl != null)
            {
                N(ctl.CharSpacing, 0f, 1e-3f,
                  "A2【灭自证】同参对照的字距 = **0** —— 证明 A1 量的 1.2 **不是 `Label` 的出厂默认值**"
                  + "（把 `Shell/LeaderboardRow.cs` 那句 `SetCharSpacing` 删掉 ⇒ A1 红、本条仍绿；"
                  + "反之若谁把出厂字距改成 1.2 让 A1 无因也绿 ⇒ 本条红。两条**结构上不可能同时满足**）");
                T(Mathf.Abs(ctl.CharSpacing - 1.2f) > 0.5f,
                  "A2b【判别式】对照标签的字距**不是 1.2**（=`Label` 出厂档）—— 与 A1 是两个独立的量");
            }
        }
        finally { Kill(ref p); }
    }

    // ==================================================================
    //  B 段：四族都是 1.2（⛔ 别按行族分支）
    // ==================================================================
    static void B_FourFamilies()
    {
        var fams = new[] { LeaderboardRowFamily.Player, LeaderboardRowFamily.PlayerForArmy,
                           LeaderboardRowFamily.Alliance, LeaderboardRowFamily.AllianceSkulls };
        foreach (var fam in fams)
        {
            var p = Build(fam, "[WF] " + fam);
            try
            {
                T(p.Name != null, $"B0（前提）`{fam}` 族建出了 `Name`");
                if (p.Name == null) continue;
                N(p.Name.CharSpacing, 1.2f, 1e-3f,
                  $"A3 `{fam}` 族的 `Name` 字距 = **1.2** —— 原版四款行族这一颗**逐值相同**"
                  + "（`PlayerRankingRow` / `… For Army` / `AllianceRankingRow Variant` / `… Skulls Variant` 都是 1.2；"
                  + "⛔ 把 `SetCharSpacing` 塞进「只对联盟族」的分支 ⇒ 前两族红）");
            }
            finally { Kill(ref p); }
        }
    }

    // ==================================================================
    //  C 段：`Ranking` / `Points` **没有**字距（别加过头）
    // ==================================================================
    static void C_NotOnRankAndPoints()
    {
        var p = Build(LeaderboardRowFamily.Alliance, "[WF] Warpforge");
        try
        {
            T(p.Ranking != null, "C0（前提）行里有 `Ranking`、且挂着 `Label`");
            T(p.Points != null, "C0（前提）行里有 `Points`、且挂着 `Label`");
            if (p.Ranking != null)
                N(p.Ranking.CharSpacing, 0f, 1e-3f,
                  "A4 `Ranking` 的字距 = **0** —— 原版这一颗 dump 里**没有** `字距` 列（⛔ 别顺手给整行都加上）");
            if (p.Points != null)
                N(p.Points.CharSpacing, 0f, 1e-3f,
                  "A4 `Points` 的字距 = **0** —— 同上（原版只有 `Name` / `Guild Name` 两颗是 1.2）");
        }
        finally { Kill(ref p); }
    }
}
