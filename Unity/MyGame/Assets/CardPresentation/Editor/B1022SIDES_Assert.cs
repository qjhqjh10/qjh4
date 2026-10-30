// B1022SIDES_Assert.cs — 执行代理 `B102_R4`（第一百零二会话 · 日常/奖励线）的断言
//
// ============================ 它盯的是什么 ============================
// 账 `日常_奖励:772`（`TwoSidesEvent_OnGoingTab.InitializeMilestones`）
// 与 `日常_奖励:774`（`TwoSidesEvent_OnGoingTab.OnOpen`）剩下的 **4 段 + S5 两格**：
//   · **S7** `Team Bonus` 显隐 —— 判据 = `OnOpen:153-165` 的 `scoringBoostMultiplier != 1f`（🔴 **不是** `!= 0`）
//   · **S8** 比分条 `scoringSliders.value` —— `OnOpen:166-176`，🔴 **原版整数除法**（`int num7 = num6 / num2`）
//   · **S10** `backgroundImage` —— `OnOpen:178-186`（`LoadAsset<Sprite>(ev, BackgroundImage)`）
//   · **S9 的调用点** —— `OnOpen:177` 那句 `InitializeMilestones()`：`InitializeMilestones` 原来**只有自检在调**
//   · **S5 两格** —— `teamIcon`（→ `Team A Image` / `Team B Image`）已接；`scoringItem` **原版队伍卡上就没有这一格** ⇒ 不建
// 施工单 → `资料/完全复刻/判据/B102_R4_C类大工程两条.md` §②（2·2 段表 / 2·4 分段施工单）。
//
// ============================ 怎么用（宿主）============================
// `B1022SidesAssert.Run(pw);`            // `pw` = 本宿主手上那扇 `ParentEventWindow` 实例
//   + 宿主里两行：`_sink.Pass += B1022SidesAssert.Pass; _sink.Fail += B1022SidesAssert.Fail;`
//   · 推荐位置 = `Editor/MainMenuScene.cs` 的**第 2 页（On Going）那一节的最后一句**
//     （同族先例 = 同文件 `CheckTeamDrawerSlots(pw)` 建在第 1 页那一节的最后）。
//   · ⚠️ 本函数**会改这扇窗的运行期状态**（`Team Bonus` 的显隐 / 比分条填充 / 里程碑行文字 / Collect 闸），
//     结束时会尽量还原（比分条回 `0/0`、`Team Bonus` 回出厂可见、背景图回空态、Collect 闸回调用前的值）；
//     **里程碑行文字还原不了**（原件是 prefab 出厂串 `1256`，而这里没有「重跑出厂」的口）
//     ⇒ 所以**必须排在那一节其它断言之后**（⛔ 别排在前面，否则后面的断言行文字会读到 `10/20/30`）。
//   · 只读源码那一条（H0）走 `Application.dataPath` ⇒ **只在编辑器宿主里成立**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · F1/F3：把判据写成 `multiplier != 0f` ⇒ **F3 红**（0f 那一档原版是**显示**，`!= 0` 会藏掉）；
//            写成 `== 1f` 取反 ⇒ F1/F2 双双红。
//   · F4：把 `_p2TeamBonus[side]` 写成 `_p2TeamBonus[0]`（两格共用一颗）⇒ F4 红。
//   · G1：把 `(float)(scoreB / total)` 写成 `(float)scoreB / total`（浮点除法）⇒ **G1 红**
//          （1/2 会得 0.3333，而原版整数除法得 1.0）。
//   · G2：忽略 `value` 直接画满条 ⇒ G2 红（0.5 档量到的宽会等于满条）。
//   · G3：`value <= 0` 时不销毁旧载体（留着上一条的填充）⇒ G3 红。
//   · I1：建树时就挂一颗空 `ImageQuad`（而不是「没数据不留载体」）⇒ I1 红。
//   · I3：`SetOnGoingBackground(null)` 不清空 ⇒ I3 红。
//   · H1：`ApplyOnGoingMilestones` 里不调 `InitializeMilestones`（自己重写一遍）⇒ **H0 红**（源码里那一段找不到这次调用）；
//          调了但参数顺序传错（collected ↔ currentScore）⇒ H2 的档位文字红。
using System.Collections.Generic;
using System.IO;
using System.Text;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_R4`：账 `日常/奖励:772` / `:774` 的 S5 / S7 / S8 / S9 / S10 落地断言。
/// <para>⛔ 期望值全是字面量（`1f` / `0f` / `10` / `0.6666667` / 节点名），**不从被测实现里读回来**。</para></summary>
public static class B1022SidesAssert
{
    static readonly CheckSink _sink = new CheckSink("[B1022SIDES] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    const string SrcRelPath = "CardPresentation/Shell/ParentEventWindow.cs";

    // ============================================================ 入口
    public static void Run(ParentEventWindow pw)
    {
        MenuCheck.Section(_sink, "B102_R4 账 `日常:772`/`:774`：S7 Team Bonus · S8 比分条 · S9 里程碑调用点 · S10 背景图 · S5 队伍图");

        Tr(pw != null, "Z0 **前提**：拿到 `ParentEventWindow` 实例（拿不到 ⇒ 下面全部空转）｜本文件不静默跳过");
        if (pw == null) { Debug.Log(_sink.P + "B1022SIDES 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败"); return; }

        bool wasOnGoing = pw.CurrentPageIndex == 1;
        pw.OpenPage(ParentEventWindow.Page.OnGoing);        // 第 2 页（On Going）才算「建树那一半活着」
        var page = pw.OnGoingPage;
        Tr(page != null && page.gameObject.activeSelf,
           "Z1 **前提**：第 2 页 `Event On Going Tab` 在且已激活（`OpenPage(OnGoing)`）｜不在 ⇒ 下面全部空转");

        try
        {
            if (page != null)
            {
                SectionF_TeamBonus(pw, page);
                SectionG_ScoringSlider(pw, page);
                SectionI_Background(pw, page);
                SectionH_MilestoneCallPoint(pw, page);
            }
        }
        finally
        {
            if (!wasOnGoing) pw.OpenPage(ParentEventWindow.Page.ChooseSide);   // 还原宿主原来停的那一页
        }

        Debug.Log(_sink.P + "B1022SIDES 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ F · S7 `Team Bonus` 显隐
    static void SectionF_TeamBonus(ParentEventWindow pw, Transform page)
    {
        MenuCheck.Section(_sink, "F S7 `Team Bonus` 显隐：`scoringBoostMultiplier != 1f`（🔴 不是 `!= 0`）");

        var bA = pw.TeamBonusNode(0);
        var bB = pw.TeamBonusNode(1);
        // A0 前提（硬断言，⛔ 不许 `if (x != null) Check(...)`）
        Tr(bA != null && bB != null,
           "F0a **前提**：两格 `Team Bonus` 都建出来了（`TeamBonusNode(0)` / `(1)` 非 null）");
        if (bA == null || bB == null) return;
        Tr(bA.name == "Team Bonus" && bB.name == "Team Bonus",
           "F0b 节点名 = **`Team Bonus`**（原版 `teamABonusObject` / `teamBBonusObject` 那两颗 GO 的 `m_Name` 实读）");
        Tr(bA.IsChildOf(page) && bB.IsChildOf(page),
           "F0c 两颗都挂在本页里（`Team A Name/Team Bonus` / `Team B Name/Team Bonus`）");

        bool showAt1 = pw.SetOnGoingTeamBonus(0, 1f);
        Tr(!showAt1 && !bA.gameObject.activeSelf,
           "F1 ★ `scoringBoostMultiplier = 1f` ⇒ **隐藏**（原版 `!= 1f` 取假）｜判据 = ILSpy `TwoSidesEvent_OnGoingTab.cs:203` 逐字 `!= 1f`");

        bool showAt2 = pw.SetOnGoingTeamBonus(0, 2f);
        Tr(showAt2 && bA.gameObject.activeSelf,
           "F2 ★ `scoringBoostMultiplier = 2f` ⇒ **显示**（`2 != 1`）");

        // 🧨 这一条专杀 `!= 0f` 那种写法：0 倍率在原版里 `0 != 1` ⇒ **显示**；`!= 0` 会把它藏掉 ⇒ 本行红。
        bool showAt0 = pw.SetOnGoingTeamBonus(0, 0f);
        Tr(showAt0 && bA.gameObject.activeSelf,
           "F3 ★ **灭自证**：`scoringBoostMultiplier = 0f` ⇒ **仍然显示**（原版判据是 `!= 1f`，`0 != 1` 为真）"
         + "｜🧨 写成 `!= 0f` 的那一版在这里必红（它会把 0 倍率藏掉），也⛔ 别把 `0` 当成「没有加成」");

        pw.SetOnGoingTeamBonus(0, 2f);
        pw.SetOnGoingTeamBonus(1, 1f);
        Tr(bA.gameObject.activeSelf && !bB.gameObject.activeSelf,
           "F4 ★ 两格**各管自己**（A = 2f 显示 / B = 1f 隐藏）｜🧨 把下标写成常量 0（两格共用一颗）⇒ 本行红");

        // 还原成 prefab 出厂态（两颗都 active · `m_IsActive = true` 实读）
        pw.SetOnGoingTeamBonus(0, 2f);
        pw.SetOnGoingTeamBonus(1, 2f);
        Tr(bA.gameObject.activeSelf && bB.gameObject.activeSelf,
           "F5（还原）两格都回到 prefab 出厂态（`Team Bonus` 的 `m_IsActive = true`）");
    }

    // ============================================================ G · S8 比分条 value（整数除法）
    static void SectionG_ScoringSlider(ParentEventWindow pw, Transform page)
    {
        MenuCheck.Section(_sink, "G S8 比分条：`value = total == 0 ? 0.5f : 1f - (float)(scoreB / total)`（🔴 整数除法照抄）");

        var fill = pw.ScoringFillNode();
        var slider = pw.ScoringSliderNode();
        var bg = slider != null ? MenuCheck.FindChild(slider, "Background") : null;
        // A0 前提（硬断言）
        Tr(slider != null && slider.name == "Scoring slider",
           "G0a **前提**：`Scoring slider` 那颗在（`ScoringSliderNode()` 非 null 且名对；= 原版 `scoringSliders`）");
        Tr(fill != null && bg != null,
           "G0b **前提**：`Background` / `Fill Area/Fill` 两格都在（`ScoringFillNode()` 非 null）");
        if (fill == null || bg == null) return;
        float bgW = WidthOf(bg);
        Tr(bgW > 100f,
           "G0c **前提**：`Background` 量得到宽（基准量尺；实得 " + bgW.ToString("F2") + "px，原版 `Scoring slider` 宽 1230.36）");
        if (bgW <= 100f) return;

        // 出厂：`Slider` MB 的 `m_Value = 0.5`，而 `total == 0` 时原版算出来也是 0.5
        N(pw.LastScoringSliderValue, 0.5f, 1e-4f,
          "G1 出厂 `LastScoringSliderValue` = **0.5**（`Slider` MB `m_Value = 0.5` 实读；`total == 0` 那一支也是 0.5）");

        // 🔴 整数除法：1 / 2 ⇒ total 3 ⇒ 2/3 = **0** ⇒ value 1.0（浮点写法会得 0.3333）
        float v12 = pw.SetTeamScores(1, 2);
        N(v12, 1.0f, 1e-5f,
          "G2 ★ `SetTeamScores(1, 2)` ⇒ `2 / 3 = 0`（整数截断）⇒ value = **1.0**"
        + "｜🧨 改成浮点除法会得 0.3333 ⇒ 本行红（🔴 整数除法是**原版自己的 bug**，照抄、⛔ 别「修正」）");

        float w12 = WidthOf(fill);
        N(w12, bgW, 1.0f,
          "G3 ★ value = 1.0 ⇒ 填充条宽 = 满条（`Fill Area` 宽 1230.1 ≈ `Background` 宽 1230.36，差 ≤ 1px）"
        + "｜🧨 忽略 value 直接画满条的实现在上一条就红了，这里钉的是「真的按 value 长」");

        // 全给 B ⇒ 3/3 = 1 ⇒ value = 0 ⇒ 一个像素都不画（原版宽 0）
        float v03 = pw.SetTeamScores(0, 3);
        N(v03, 0.0f, 1e-5f, "G4 ★ `SetTeamScores(0, 3)` ⇒ `3 / 3 = 1` ⇒ value = **0.0**（全给 B 队 ⇒ 填充清零）");
        Tr(QuadOf(fill) == null || !QuadOf(fill).gameObject.activeSelf,
           "G5 ★ value = 0 ⇒ **填充条不画**（原版 `Fill` 宽 0 ⇒ 一个像素都不画）"
         + "｜🧨 不清掉旧载体的那一版会留着上一条的满条 ⇒ 本行红");

        // 全给 A ⇒ 0/3 = 0 ⇒ value = 1（反向那一档，确认不是「谁分高谁清零」）
        float v30 = pw.SetTeamScores(3, 0);
        N(v30, 1.0f, 1e-5f, "G6 ★ `SetTeamScores(3, 0)` ⇒ `0 / 3 = 0` ⇒ value = **1.0**（全给 A 队 ⇒ 满条）");

        // 半条：0/0 ⇒ 0.5 —— 与满条**宽度比 = 2**（比例判据，与父链缩放/偏移无关）
        pw.SetTeamScores(0, 0);
        float wHalf = WidthOf(fill);
        Tr(wHalf > 1f, "G7 **前提**：0/0 之后量到填充条（不在 ⇒ 下面那条比例断言空转）｜实得 " + wHalf.ToString("F2") + "px");
        if (wHalf > 1f)
            N(w12 / wHalf, 2.0f, 0.02f,
              "G8 ★ 满条 : 半条 = **2.0**（`Fill` 宽按 value 线性；原版由 `Slider` 改 `Fill` 的 `anchorMax.x = value`）");
    }

    // ============================================================ I · S10 背景图
    static void SectionI_Background(ParentEventWindow pw, Transform page)
    {
        MenuCheck.Section(_sink, "I S10 `backgroundImage`（`Battle Background`）：出厂空 · 注入后换图 · 置空回空态");

        var node = pw.OnGoingBackgroundNode();
        // A0 前提（硬断言）
        Tr(node != null, "I0a **前提**：`Battle Background` 那一格建出来了（`OnGoingBackgroundNode()` 非 null）");
        if (node == null) return;
        C(node.name, "Battle Background", "I0b 节点名 = **`Battle Background`**（原版 `backgroundImage` 的宿主 GO 实读）");
        Tr(node.IsChildOf(page), "I0c 它挂在本页里");

        Tr(QuadOf(node) == null,
           "I1 ★ 出厂**没有任何载体**（= 原版 `Image.m_Sprite = null` 的同一可观测结果；一个像素都不画）"
         + "｜🧨 建树时就挂一颗空 `ImageQuad` ⇒ 本行红");

        var tex = CardArt.Solid();
        Tr(tex != null, "I2 **前提**：探针贴图取得到（`CardArt.Solid()`）");
        if (tex == null) return;

        bool ok = pw.SetOnGoingBackground(tex);
        var q = QuadOf(node);
        Tr(ok && q != null && q.gameObject.activeSelf && q.Texture == tex,
           "I3 ★ `SetOnGoingBackground(tex)` ⇒ 长出载体、填上那张图（= 原版 `backgroundImage.sprite = LoadAsset<Sprite>(ev, BackgroundImage)` 那一跳）");

        bool ok2 = pw.SetOnGoingBackground(null);
        var q2 = QuadOf(node);
        Tr(ok2 && (q2 == null || !q2.gameObject.activeSelf),
           "I4 ★ `SetOnGoingBackground(null)` ⇒ **回到空态**（旧图不被留着）｜🧨 只 `return` 不关载体 ⇒ 本行红");
    }

    // ============================================================ H · S9 里程碑调用点（`InitializeMilestones` 的生产入口）
    static void SectionH_MilestoneCallPoint(ParentEventWindow pw, Transform page)
    {
        MenuCheck.Section(_sink, "H S9 里程碑：`OnOpen:177` 的 `InitializeMilestones()` 那一跳（+ 三档 state 落到行上）");

        // ---- H0 源码：`ApplyOnGoingMilestones` 体里**真的调** `InitializeMilestones`（不是自己抄一份）----
        string code = StripComments(ReadSourceOrNull());
        Tr(code != null, "H0a **前提**：读得到 `" + SrcRelPath + "`（读不到 ⇒ H0b 空转；本文件不静默跳过）");
        if (code != null)
        {
            int at = code.IndexOf("ApplyOnGoingMilestones", System.StringComparison.Ordinal);
            Tr(at >= 0, "H0b **前提**：`ApplyOnGoingMilestones` 在源码里（改名了 ⇒ 这条要跟着改，⛔ 别空转绿）");
            string body = at >= 0 ? code.Substring(at, System.Math.Min(700, code.Length - at)) : string.Empty;
            Tr(body.IndexOf("InitializeMilestones(", System.StringComparison.Ordinal) >= 0,
               "H0c ★ 它的体里**真的调** `InitializeMilestones(`（= 原版 `OnOpen:177` 那一跳的生产调用点；"
             + "改前这个方法**只有自检在调**、生产 0 调用点）｜🧨 改成「自己重写一遍档位逻辑」⇒ 本行红");
        }

        var lv1 = MenuCheck.FindChild(page, "Score Bar Line Level 1");
        var lv2 = MenuCheck.FindChild(page, "Score Bar Line Level 2");
        var lv3 = MenuCheck.FindChild(page, "Score Bar Line Level 3");
        // A0 前提（硬断言）
        Tr(lv1 != null && lv2 != null && lv3 != null,
           "H1a **前提**：`Score Bar Line Level 1..3` 三行都在（`BuildRewardDisplay` 建的五档行）");
        if (lv1 == null || lv2 == null || lv3 == null) return;

        var milestones = new List<ScoringMilestone>
        {
            new SimpleScoringMilestone(10, 0),      // 第 1 档门槛 10
            new SimpleScoringMilestone(20, 0),      // 第 2 档门槛 20
            new SimpleScoringMilestone(30, 0),      // 第 3 档门槛 30
        };
        int n = pw.ApplyOnGoingMilestones(20, 1, milestones);
        C(n, 3, "H2 ★ `ApplyOnGoingMilestones(20, 1, 三档表)` 返回 **3**（= 传进去的档数）");

        // 三档 state：score 20 · collected 1 ⇒ [Collected(0<1) · Unlocked(1<1 假) · Locked(20<30)]
        C(TextOf(MenuCheck.FindChild(lv1, "Score")), "10", "H3 ★ 第 1 档 `Score.text` = **10**（原版 `score.text = milestone.score.ToString()`）");
        C(TextOf(MenuCheck.FindChild(lv2, "Score")), "20", "H4 ★ 第 2 档 `Score.text` = **20**");
        C(TextOf(MenuCheck.FindChild(lv3, "Score")), "30", "H5 ★ 第 3 档 `Score.text` = **30**"
            + "｜🧨 把 `ApplyOnGoingMilestones` 的两个 int 实参写反（collected ↔ currentScore）⇒ 上面三行的 state 全变、箱子图/高亮跟着错");

        // 归一化进度：thresholds = [10,20,30]、score = 20 ⇒ 落在第 2/3 档之间 ⇒ 2/3
        N(pw.LastMilestoneProgress, 0.6666667f, 1e-4f,
          "H6 ★ `LastMilestoneProgress` = **2/3 ≈ 0.6666667**（`InterpolateMilestones(20, [10,20,30])`；"
        + "= 原版 `Slider.normalizedValue` 那一格）");

        // Collect 闸：三档里第 2 档是 `Unlocked` ⇒ `Any(state == 1)` 为真
        var collectNode = MenuCheck.FindChild(page, "Generic Simplified UI Button");
        var wb = collectNode != null ? collectNode.GetComponentInChildren<WindowButton>(true) : null;
        Tr(wb != null, "H7 **前提**：Collect 钮上有 `WindowButton`（没有 ⇒ H8/H9 空转）");
        if (wb != null)
        {
            bool before = wb.Interactable;
            Tr(wb.Interactable,
               "H8 ★ 有一档 `Unlocked`（可领）⇒ Collect 钮**可交互**（= 原版 `Any(m => m.state == 1)` 那句 `collectButton.interactable`）");

            // 🔴 反向那一档（灭自证）：分数 0 ⇒ 三档全 `Locked` ⇒ 闸必须**关**。
            //    只断「真」是自证 —— 闸恒真的实现照样过；两条一起才把「闸由 state 决定」钉死。
            pw.ApplyOnGoingMilestones(0, 0, milestones);
            Tr(!wb.Interactable,
               "H9 ★ **灭自证**：`currentScore = 0` ⇒ 三档全 `Locked` ⇒ Collect 钮**不可交互**"
             + "｜🧨 闸恒真 / 恒沿用上一次的值 ⇒ 本行红");

            pw.ApplyOnGoingMilestones(20, 1, milestones);   // 回到「有一档可领」那一态
            wb.Interactable = before;                        //（还原）不给宿主留脏状态
        }
    }

    // ============================================================ 量 / 读 小工具
    /// <summary>一棵子树里第一颗 `ImageQuad` 的**渲染宽**（px）。量不到 ⇒ 返回 0。</summary>
    static float WidthOf(Transform t)
    {
        var q = QuadOf(t);
        float x1, y1, x2, y2;
        if (q == null || !MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2)) return 0f;
        return x2 - x1;
    }

    static ImageQuad QuadOf(Transform t)
    {
        return t != null ? t.GetComponentInChildren<ImageQuad>(true) : null;
    }

    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponent<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    static string ReadSourceOrNull()
    {
        var full = Path.Combine(Application.dataPath, SrcRelPath.Replace('/', Path.DirectorySeparatorChar));
        try { return File.Exists(full) ? File.ReadAllText(full) : null; }
        catch (System.Exception e) { Debug.LogWarning("[B1022SIDES] 读源码失败：" + e.Message); return null; }
    }

    /// <summary>去掉 `//` 行注释（够用：本文件里没有含 `//` 的字符串字面量）。
    /// <para>🔴 为什么要它：本仓踩过「代码被追加到行末 `//` 之后 ⇒ 看着接上了、断言一条没跑」的坑
    /// （`资料/完全复刻/报告/B102_W1_断言接线被注释吞掉.md`）⇒ 源码类判据一律先去注释。</para></summary>
    static string StripComments(string src)
    {
        if (src == null) return null;
        var sb = new StringBuilder(src.Length);
        foreach (var raw in src.Split('\n'))
        {
            string line = raw;
            int ix = line.IndexOf("//", System.StringComparison.Ordinal);
            if (ix >= 0) line = line.Substring(0, ix);
            sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }
}
