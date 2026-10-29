// B99G1_Assert.cs — 第九十九会话 · 波 1 · 执行代理 `B99_G1`
//
// ============================ 它盯的是什么 ============================
//  卡-2 `FitToBox` 从「我们自己的近似」换成**原版 TMP autosize**（`Core/CardView.cs`）：
//   · **A 段** `FaceDescBand01` 的 `showsArmy` 那一维（`B99_R4a` §1·5 的行为变化）：
//     手牌 / 场上**不印阵营行** ⇒ 效果文字可用高**恢复成原版矩形**（原版矩形下沿就压在阵营行那一条上，
//     因为原版从不印那一行）；Full 面（印阵营行）那档**逐位不变**；单位卡因「数值容器门」binding
//     而**两档相同**（没被顺手放大）。
//   · **F 段** `FitToBox` 的五条机械性质（确定性 TMP 夹具，不依赖卡表）：
//     上界来自 `m_fontSizeMax`（**能放大**）· 起点是 `min` 而不是名义字号 · 结果落 **0.005 网格** ·
//     缓存键按**档位**分开 · 装得下（行盒 ≤ 框高）。
//   · **G 段** 真卡回归：真卡面建出来之后，卡名/效果文字的字号**落在 0.005 网格上**、
//     且**卡名档位按节点分开**（单位 = 1/19 不放大 · 战术 = 1/24 会放大）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · 施工单 → `资料/完全复刻/判据/B99_R4a_近似改原版值_卡牌界面.md` §1（逐层 [min,max] 现读表、
//     换算「原版 pt ÷ 10」、真卡复算）
//   · 第一权威 → `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs`
//     （`:4537-4546` 缩 · `:4811-4828` 涨 · `:524` 迭代上限）＋ `TextMeshPro.cs:2147-2152`（起点 = clamp(base,min,max)）
//   · 原版逐层 `m_fontSizeMin/Max` → `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearenatauviorla/MonoBehaviour/*.json`
//   · 本件报告 → `资料/完全复刻/报告/B99_G1.md`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `Editor/CardBaseDemo.cs` 的 `Run()` 里加**一行块语句**（建议位置 = 现有 `AssertFaceTextFit(...)` 那一组**之后**）：
//       // 🆕 2026-10-29（调度台代接线）：`B99_G1` —— `FitToBox` 照原版 TMP autosize（含 `FaceDescBand01` 的 showsArmy 那一维）。
//       { B99G1_Assert.Run(); }
//   🔴 **故意不用 `Step(...)`**（同 `B97W1` / `B98G1` 的理由：`Step` 会让 `_ranCases` 记账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」，两者都得跟着改）。
//   ⚠️ 为什么接在 `CardBaseDemo`（卡面宿主）而不是别处：本件只碰卡面文字层，`B99G1` 的 F/G 两段
//     要真卡面 + 真字体资产，那是这个宿主已经在做的事（`CardBaseDemo.Run` 的既有夹具）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **A1**：把 `FaceDescBand01` 里 `up = showsArmy ? (center - topGuard) : float.MaxValue;` 改回
//     `up = center - topGuard;`（= 恒让位）⇒ 战术卡「不印阵营行」那档**恢复不出原版矩形** ⇒ **A1 红**。
//   · **A2**：把 `if (unitCard) botGuard = Mathf.Min(botGuard, StatBandTop01 - 0.01f);` 删掉
//     ⇒ 单位卡不印阵营行那档会涨到**原版矩形高**（0.2286）⇒ **A2 红**。
//   · **F1**：把 `FitToBox` 的「涨」那一支删掉（只留缩）⇒ 字号停在 `min`（0.10）⇒ **F1 红**。
//   · **F2**：把上界改成名义字号（旧写法 `hi = nominal`）⇒ 名义 0.5 那档只缩不放 ⇒ **F2 红**。
//   · **F3**：把 `OnGrid(...)` / `OnGridDown(...)` 换成原值（不量化）⇒ 结果**落不到 0.005 网格** ⇒ **F3 红**。
//   · **F4**：`FitKey` 里把 `bandMin/bandMax` 换回名义字号（旧键）⇒ 第二次带**更窄的档**会**命中旧缓存**
//     ⇒ 返回 2.40 而不是 1.00 ⇒ **F4 红**。
//   · **G1/G2**：把单位卡那一支的档位写成 `NameTacticBand*`（1/24）⇒ 单位卡卡名会被放大 ⇒ **G2 红**；
//     把战术卡那一支写成 `NameUnitBand*`（1/19）⇒ 战术卡卡名停在 1.90 ⇒ **G1 红**。
//   · **G3**：去掉量化 ⇒ 真卡字号落不到网格 ⇒ **G3 红**（除非碰巧）。
//
// ============================ 已知边界（⛔ 不是「已验收」）============================
//   1. **F 段是机械性质，不证明「与原版逐位相同」**：它钉的是「上界 = max / 起点 = min / 网格 / 键」这几条
//      **形状**，真机像素与 PnP 成品图的并排比不在本文件（那是真 Play 的事）。
//   2. **G1 依赖「卡池里最短的那个战术卡名装得下」这个前提**：本段自己挑**最短**的战术卡名当夹具，
//      并在前面写死一条前提断言（`TitleText != null`）。若哪天卡池里最短的战术卡名也长得离谱，
//      G1 会红 —— **红 = 前提变了**，去看日志里的实际字号，别当成缺陷去改判据。
//   3. **`FitToBox` 的纵向判据仍是行盒（`textBounds`）**：原版也只看纵向（`TextMeshPro.cs:3044`），
//      但我们**多看了一条宽度**（`FitsBox` 里 `b.x <= maxWidth * eps`）—— 那是 `FitToWidth` 那条自设计
//      留下的，本件没动（`B99_R4a` §1·4 第 2 条：登记、不实现）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B99_G1`：`FitToBox` 照原版 TMP autosize（含 `FaceDescBand01` 的 `showsArmy` 那一维）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B99G1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>「这个字号落在原版那 0.05 pt 网格上吗」—— 网格值从 `CardView.AutoSizeGrid` 来（⛔ 不另抄一份）。</summary>
    static bool OnGrid(float fs)
    {
        float g = CardView.AutoSizeGrid;                 // 0.005（原版 0.05 pt ÷ 10）
        float q = Mathf.Round(fs / g) * g;
        return Mathf.Abs(fs - q) < 1e-3f;
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B99_G1 · `FitToBox` 照原版 TMP autosize（`FaceDescBand01` 的 showsArmy 那一维）");

        SectionA_DescBand();
        SectionF_FitToBoxMechanics();
        SectionG_RealCards();

        Debug.Log(_sink.P + "B99G1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A `FaceDescBand01` 的 showsArmy 那一维
    static void SectionA_DescBand()
    {
        MenuCheck.Section(_sink, "A `FaceDescBand01(showsArmy)`：手牌/场上不印阵营行 ⇒ 恢复原版矩形（`B99_R4a` §1·5）");

        float c, bArmy, bNoArmy;

        // ---- 战术卡（没有数值格 ⇒ 数值门不参与）：不印阵营行那档必须**恢复成原版矩形** ----
        // 原版矩形高 = `DescTextTactic` 160 × 78.98 px × 0.01 = 0.7898 卡单位 ÷ 3.3313 = 0.2371 y01。
        CardView.FaceDescBand01(false, false, true, out c, out bArmy);
        CardView.FaceDescBand01(false, false, false, out c, out bNoArmy);
        float tacticRectY01 = CardView.DescBoxSize(false).y / CardView.CardUnitH;
        Tr(tacticRectY01 > 0.23f && tacticRectY01 < 0.24f,
           $"A0 **前提断言**：`DescTextTactic` 矩形高换算成 y01 = {tacticRectY01:F4}（期望 0.2371 = 0.7898 ÷ 3.3313）"
         + "｜🧨 矩形常量被改过 ⇒ 前提变了，别拿 A1 当通过");
        Tr(Mathf.Abs(bNoArmy - tacticRectY01) < 2e-3f,
           $"A1 ★ 战术卡**不印阵营行**（手牌/场上）⇒ 可用高 {bNoArmy:F4} = 原版矩形高 {tacticRectY01:F4}"
         + $"（差 {Mathf.Abs(bNoArmy - tacticRectY01):F4}，容差 0.002）"
         + "｜🧨 `FaceDescBand01` 里 `up = showsArmy ? … : float.MaxValue` 改回恒让位 ⇒ 落在 0.1272 ⇒ 红");
        Tr(bNoArmy > bArmy * 1.5f,
           $"A1b ★ 不印阵营行那档 {bNoArmy:F4} 明显大于印阵营行那档 {bArmy:F4}（> ×1.5）"
         + "｜🧨 两档相同 ⇒ `showsArmy` 这一维没接上 ⇒ 红");
        Tr(bArmy > 0.10f && bArmy < 0.15f,
           $"A1c 印阵营行那档 {bArmy:F4} ≈ 0.1272（`B99_R4a` §1·5 的实测值；区间 0.10–0.15 兜住四舍五入）"
         + "｜🧨 把一个绝对值写死在这一档上也能红 —— 它钉的是「Full 面那一档没被顺手改」");

        // ---- 单位卡（有数值格）：数值容器那道门 binding ⇒ **两档相同**，且**没有**涨到原版矩形 ----
        CardView.FaceDescBand01(true, true, true, out c, out bArmy);
        CardView.FaceDescBand01(true, true, false, out c, out bNoArmy);
        float unitRectY01 = CardView.DescBoxSize(true).y / CardView.CardUnitH;
        Tr(Mathf.Abs(bArmy - bNoArmy) < 1e-4f,
           $"A2 ★ 单位卡两档相同（{bArmy:F4} / {bNoArmy:F4}）—— 它的 binding 是**数值容器门**，与阵营行无关"
         + "｜🧨 `if (unitCard) botGuard = Mathf.Min(botGuard, StatBandTop01 - 0.01f);` 被删 ⇒ 不印阵营行那档涨到 "
         + $"{unitRectY01:F4} ⇒ 红");
        Tr(bNoArmy < unitRectY01 - 0.01f,
           $"A2b 单位卡不印阵营行那档 {bNoArmy:F4} 仍**明显小于**原版矩形高 {unitRectY01:F4}（数值门在起作用）"
         + "｜🧨 同 A2 —— 去掉数值门 ⇒ 两者相等 ⇒ 红");
        Tr(Mathf.Abs(bArmy - 0.1098f) < 1e-3f,
           $"A2c 单位卡（有兵种行）那档 = {bArmy:F4} ≈ 0.1098（`FaceDescBand01` 的 doc 实测值 / `B99_R4a` §1·5）"
         + "｜🧨 数值门被放松 ⇒ 变大 ⇒ 红");

        // ---- 两个重载 / `ForFace` 那一跳 ----
        float c1, b1, c2, b2;
        CardView.FaceDescBand01(true, true, out c1, out b1);          // 老签名（自检宿主那一档）
        CardView.FaceDescBand01(true, true, true, out c2, out b2);
        Tr(Mathf.Abs(b1 - b2) < 1e-6f && Mathf.Abs(c1 - c2) < 1e-6f,
           $"A3 老四参重载 = `showsArmy: true` 那档（{b1:F6} vs {b2:F6}）—— 既有自检宿主拿到的仍是**最保守**那一档"
         + "｜🧨 老重载改成放行 ⇒ 宿主与生产的期望值分叉 ⇒ 红");
        float c3, b3;
        CardView.FaceDescBand01ForFace(false, false, CardFace.Hand, out c3, out b3);
        CardView.FaceDescBand01(false, false, CardView.FaceShowsArmy(CardFace.Hand), out c2, out b2);
        Tr(Mathf.Abs(b3 - b2) < 1e-6f,
           $"A4 `FaceDescBand01ForFace(…, CardFace.Hand, …)` = {b3:F6} 与「显式传 `FaceShowsArmy(Hand)`」逐位相同"
         + "｜🧨 那个便利口把形态判反（例如恒 `true`）⇒ 手牌回到 0.1272 ⇒ 红");
        Tr(CardView.FaceShowsArmy(CardFace.Hand) == false && CardView.FaceShowsArmy(CardFace.Board) == false
           && CardView.FaceShowsArmy(CardFace.Full) == true,
           "A5 **前提断言**：阵营行只在 `Full` 面印（手牌/场上不印）—— 用户 2026-09-22 裁定，"
         + "`FaceShowsArmy` 的 doc 有实拍判据｜🧨 这一档被改 ⇒ A1/A4 量的是另一个态");
    }

    // ============================================================ F `FitToBox` 的机械性质
    static void SectionF_FitToBoxMechanics()
    {
        MenuCheck.Section(_sink, "F `FitToBox`：上界 = m_fontSizeMax（能放大）· 起点 = min · 结果落 0.005 网格 · 缓存键按档位");

        var root = new GameObject("B99G1_fit");
        var made = new List<TextMeshPro>();
        try
        {
            // 原版那三层的档位：`DescTextUnit` = 1/24 ⇒ ÷10 = 0.10 / 2.40（见 CardView 的档位常量表）。
            const float lo = 0.10f, hi = 2.40f;

            TextMeshPro NewFit(string tag, string text, float nominal)
            {
                var t = TmpFont.NewText(root.transform, tag, text, nominal, Color.white, TmpFont.Face.LatinCardBody);
                if (t != null) made.Add(t);
                return t;
            }

            // ---- F1 上界能放大：短文字放进大框 ⇒ 结果必须顶到 max（旧实现停在名义字号）----
            var a = NewFit("g1_grow", "Ab", 1.90f);
            Tr(a != null, "F0 **前提断言**：字体资产拿得到（`TmpFont.NewText` 非 null）—— ⛔ 取不到时下面几条**没验到**");
            if (a != null)
            {
                bool ok = CardView.FitToBoxForTest(a, 1.60f, 0.70f, false, lo, hi);
                Tr(ok, "F0b **前提断言**：`FitToBox` 量到了可用版面（返回真）—— 返回假时下面几条拿的是未拟合值");
                Tr(Mathf.Abs(a.fontSize - hi) < 1e-4f,
                   $"F1 ★★ 装得下 ⇒ 字号被**放大到上界** {a.fontSize:F4} = max {hi:F4}（名义 1.90 不再是天花板）"
                 + "｜🧨 删掉「涨」那一支（只缩不放）⇒ 停在 min 0.10 ⇒ 红；把 `hi` 写回名义字号 ⇒ 停在 1.90 ⇒ 红");
                Tr(OnGrid(a.fontSize),
                   $"F1b 结果落在原版那 0.05 pt 网格上（{a.fontSize:F4}，网格 = {CardView.AutoSizeGrid:F3}）");
            }

            // ---- F2 起点是 min、不是名义字号：名义给 0.5（远小于能装下的值）⇒ 结果仍是 max ----
            var b = NewFit("g1_baseline", "Ab", 0.50f);
            if (b != null)
            {
                bool ok = CardView.FitToBoxForTest(b, 1.60f, 0.70f, false, lo, hi);
                Tr(ok, "F2 **前提断言**：第二次量到了可用版面");
                Tr(Mathf.Abs(b.fontSize - hi) < 1e-4f,
                   $"F2 ★★ 名义字号 0.50 不影响结果（{b.fontSize:F4} = max {hi:F4}）—— 原版起点是 "
                 + "`clamp(m_fontSizeBase, min, max)`（`TextMeshPro.cs:2149`），名义字号**不参与**搜索"
                 + "｜🧨 把上界写成 `nominal`（旧写法）⇒ 停在 0.50 ⇒ 红");
            }

            // ---- F3 量化：框高故意卡在中间 ⇒ 结果必须**落网格**、且**装得下** ----
            var d = NewFit("g1_grid", "Ab", 1.90f);
            if (d != null)
            {
                bool ok = CardView.FitToBoxForTest(d, 1.60f, 0.20f, false, lo, hi);
                Tr(ok, "F3 **前提断言**：第三次量到了可用版面");
                float fs = d.fontSize;
                Tr(fs > lo + 1e-4f && fs < hi - 1e-4f,
                   $"F3a **前提断言**：这一档的结果在区间**内部**（{fs:F4} ∈ ({lo:F3}, {hi:F3})）—— "
                 + "否则「落网格」那条没有判别力（撞 min/max 的值天然在网格上）");
                Tr(OnGrid(fs),
                   $"F3b ★★ 结果落 0.005 网格：{fs:F4}（原版 `(int)(fontSize*20+0.5)/20` ÷ 10）"
                 + "｜🧨 去掉 `OnGrid` / `OnGridDown` ⇒ 结果是二分出来的带小数的值 ⇒ 红");
                Tr(d.textBounds.size.y <= 0.20f * 1.002f,
                   $"F3c 定下来的这一档**装得下**：行盒高 {d.textBounds.size.y:F4} ≤ 框高 0.20（容差 0.2%）"
                 + "｜🧨 量化时**向上**取（而不是 `OnGridDown`）⇒ 会顶出框 ⇒ 红");
            }

            // ---- F4 缓存键按档位分开：同文本同框、只换更窄的档 ⇒ 不许命中旧缓存 ----
            var e = NewFit("g1_key", "Ab", 1.90f);
            if (e != null)
            {
                CardView.FitToBoxForTest(e, 1.60f, 0.70f, false, lo, hi);      // 第一次：档 0.10..2.40 ⇒ 2.40
                float first = e.fontSize;
                CardView.FitToBoxForTest(e, 1.60f, 0.70f, false, lo, 1.00f);   // 第二次：档 0.10..1.00
                Tr(Mathf.Abs(first - hi) < 1e-4f, $"F4 **前提断言**：第一次拟合到了上界（{first:F4}）");
                Tr(Mathf.Abs(e.fontSize - 1.00f) < 1e-4f,
                   $"F4 ★★ 换窄档（max 1.00）之后结果 {e.fontSize:F4} = 1.00 —— 缓存键**按 [min,max] 分开**"
                 + "｜🧨 `FitKey` 把档位换回名义字号（旧键）⇒ 第二次命中第一次的 2.40 ⇒ 红");
            }

            // ---- F5 `[min,max]` 的**下界**也是真的：框小到连 min 都装不下 ⇒ 停在 min（原版同一支）----
            var f = NewFit("g1_min", "Ab", 1.90f);
            if (f != null)
            {
                CardView.FitToBoxForTest(f, 1.60f, 0.001f, false, lo, hi);
                Tr(f.fontSize >= lo - 1e-4f,
                   $"F5 ★ 装不下时**不会低于这一层的 min**（{f.fontSize:F4} ≥ {lo:F3}）—— 原版 `TMP_Text.cs:4546` "
                 + "的 `Mathf.Max(…, m_fontSizeMin)` 同义"
                 + "｜🧨 去掉 `OnGridDown` 之后的 `Mathf.Max(…, bandMin)` ⇒ 会掉到 min 以下 ⇒ 红");
            }
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ G 真卡回归
    static void SectionG_RealCards()
    {
        MenuCheck.Section(_sink, "G 真卡回归：字号落网格 · 卡名档位按节点分开（单位 1/19 不放大 · 战术 1/24 会放大）");

        var pool = RuleEngine.CardDatabase.Load();
        Tr(pool != null && pool.Count > 0, "G0 **前提断言**：卡表读得到（`CardDatabase.Load()` 非空）");
        if (pool == null || pool.Count == 0) return;
        Tr(TmpFont.Available, "G0b **前提断言**：`TmpFont.Available`（取不到字体时各层根本不建，下面全是空绿）");
        if (!TmpFont.Available) return;

        // 单位卡：挑**效果文字最长**的那张（拟合最可能真的缩过，网格那条才有判别力）。
        // 战术卡：挑**卡名最短**的那张（要「装得下 ⇒ 会被放大到 24pt 那一档」这个前提成立）。
        RuleEngine.CardDef unitDef = null, tacticDef = null;
        CardData unitData = default(CardData), tacticData = default(CardData);
        int bestDesc = -1, bestName = int.MaxValue;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            var dd = BattleDriver.ToCardData(c, c.Faction);
            if (c.IsUnit)
            {
                int len = dd.keywords != null ? dd.keywords.Length : 0;
                if (len > bestDesc) { bestDesc = len; unitDef = c; unitData = dd; }
            }
            else
            {
                int len = dd.title != null ? dd.title.Length : 0;
                if (len < bestName) { bestName = len; tacticDef = c; tacticData = dd; }
            }
        }

        var root = new GameObject("B99G1_cards");
        var made = new List<CardView>();
        try
        {
            // ---- 单位卡：卡名档 = `NameTextUnit` 1/19 ⇒ **上界就是名义 1.90，不许放大** ----
            Tr(unitDef != null, "G1 **前提断言**：卡池里挑得出一张单位卡");
            if (unitDef != null)
            {
                var v = CardView.Create(root.transform, unitData, "G1_unit");
                made.Add(v);
                var nt = v != null ? v.TitleText : null;
                Tr(nt != null, "G1b **前提断言**：单位卡卡名层建出来了（`TitleText` 非 null）");
                if (nt != null)
                {
                    float nom = v.NameFontSize;
                    Tr(nt.fontSize <= nom + 1e-3f,
                       $"G1 ★ 单位卡卡名**没被放大**：fs {nt.fontSize:F4} ≤ 名义 {nom:F4}"
                     + "（原版 `NameTextUnit` 的 `m_fontSizeMax` = 19 = 名义 ⇒ 上界就是它）"
                     + "｜🧨 卡名那支把 `NameTacticBand*`（1/24）当档位传进去 ⇒ 涨到 2.4 一档 ⇒ 红");
                    Tr(OnGrid(nt.fontSize),
                       $"G1c 单位卡卡名 fs {nt.fontSize:F4} 落 0.005 网格｜🧨 去掉量化 ⇒ 红");
                }
                var dt = v != null ? v.KeywordsText : null;
                if (dt != null)
                {
                    Tr(OnGrid(dt.fontSize),
                       $"G1d 单位卡效果文字 fs {dt.fontSize:F4} 落 0.005 网格（`DescTextUnit` 档 1/24；"
                     + $"名义 {CardView.KeywordFontSize:F3}）｜🧨 去掉量化 ⇒ 红");
                    Tr(dt.fontSize <= 2.40f + 1e-3f,
                       $"G1e 效果文字 fs {dt.fontSize:F4} ≤ 该层上界 2.40（`DescTextUnit m_fontSizeMax = 24` ÷ 10）"
                     + "｜🧨 上界写错（例如拿 2.60 那档 `DescTextUnit Big`）⇒ 越界 ⇒ 红");
                }
            }

            // ---- 战术卡：卡名档 = `NameTextTactic` 1/**24** ⇒ 装得下就会被放大到 1.90 以上 ----
            Tr(tacticDef != null, "G2 **前提断言**：卡池里挑得出一张战术/防御卡（`!IsUnit`）");
            if (tacticDef != null)
            {
                var v = CardView.Create(root.transform, tacticData, "G2_tactic");
                made.Add(v);
                var nt = v != null ? v.TitleText : null;
                Tr(nt != null, "G2b **前提断言**：战术卡卡名层建出来了（`TitleText` 非 null）");
                if (nt != null)
                {
                    float nom = v.NameFontSize;      // 名义（单位/战术同一个名义 = 1.90；`NameFontSize` 按 `CardUse` 分档）
                    Tr(nt.fontSize > nom + 0.05f,
                       $"G2 ★★ 战术卡卡名**被放大**了：fs {nt.fontSize:F4} > 名义 {nom:F4}"
                     + $"（夹具 = 卡名最短的 `{tacticDef.Name}`；原版 `NameTextTactic` 的 `m_fontSizeMax` = **24** "
                     + "⇒ 运行时 = 24 = 我们的 2.40 档）"
                     + "｜🧨 卡名那支把 `NameUnitBand*`（1/19）传进去 ⇒ 停在 1.90 ⇒ 红（= 改前那 21% 的差）");
                    Tr(nt.fontSize <= 2.40f + 1e-3f,
                       $"G2c 战术卡卡名 fs {nt.fontSize:F4} ≤ 2.40（`NameTextTactic m_fontSizeMax = 24` ÷ 10）"
                     + "｜🧨 上界写大 ⇒ 越界 ⇒ 红");
                    Tr(OnGrid(nt.fontSize),
                       $"G2d 战术卡卡名 fs {nt.fontSize:F4} 落 0.005 网格｜🧨 去掉量化 ⇒ 红");
                }
            }
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
