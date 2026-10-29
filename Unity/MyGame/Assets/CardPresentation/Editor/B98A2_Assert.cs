// B98A2_Assert.cs — 执行代理 `B98_A2`（第九十八会话 · 波 5a · 日常线）
//
// ============================ 它盯的是什么 ============================
//   账 `日常/奖励:155` = 原版 `EndBattleRewardHolder.SetReward(RewardInfo)` 的最后一格可做残差：
//     `Quantity` 那颗 TMP **开自适应**并对齐原版三档 `m_enableAutoSizing = 1` · min **18** / max **40** / base **36**。
//
// 🔴 **现读结论（先说清楚，免得下一个人再查一遍）**：**代码这一半已由 `B97_W41` 落地并提交**
//   （`Battle/EndPanel.cs:1062-1068` 的 `SetAutoFitBox(70, 45, 18·k, 40·k, 36·k)` + 类尾 `:1147-1151` 三个常量
//   `QuantityFsMin/Max/Base`），断言也已存在并接线（`Editor/B97W41_Assert.cs` 的 A1..A9 · 宿主 `Editor/BattleScene.cs:36521`）。
//   ⇒ **本文件不重复实现**，只补三件 `B97_W41` 没覆盖的（见下），外加 A 段一次独立复核。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · 原版四格：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3928.json`
//     `m_enableAutoSizing 1` · `m_fontSizeMin 18` · `m_fontSizeMax 40` · `m_fontSizeBase 36`（单位 = **TMP 的 fontSize**）
//   · 框：`RectTransform_3139` 锚 (0.5,0.5)-(1,0.5) + `m_SizeDelta (0,45)` ⇒ **70 × 45**
//   · 换算口：`Battle/Label.cs:1047-1085`（`FontSizeToPxNow`）· `:1442-1447`（`NominalPx`）· `:1293`（`SetAutoFitBox`）
//     `SetAutoFitBox` 内部是 `fontSizeX = cur × (xPx / NominalPx())`，而 `NominalPx() ≡ cur × FontSizeToPxNow(1, face)`
//     ⇒ **传 `x × FontSizeToPxNow(1, face)` 才落回原版那三个 fontSize**（`cur` 在式子里约掉，与建标签时的档无关）。
//
// ============================ 本文件**新增**（`B97_W41` 没覆盖的三点）============================
//   **B 段 · 换算承重（灭自证）**：把一个原版数字**不乘系数**直接灌进同一个口 ⇒ 读回 **18/qk ≠ 18**，再走一次生产口 ⇒ 回到 18。
//     W41 只断「结果 = 18」，**没断「不换算会错」** ⇒ 万一哪天有人把那乘号删掉、又刚好把常量改成 18/qk，W41 仍然全绿。
//   **C 段 · 今天生产路径的真实状态**：`SetRewards` 全仓**零生产调用点**（只有自检在调，`grep -rn "SetRewards(" Assets` 现核）
//     ⇒ `Create()` 之后奖励行**是藏的、清单是空的**。W8/W41 都是「先喂再断」，从没断过「一次都没喂」这一态。
//   **D 段 · 一位数**（`quantity = 1`）也断「渲染宽高 ≤ 框」：W41 只量了五位数 `12345`。
//
// ============================ 怎么用（宿主）============================
//   `B98A2_Assert.Run(); pass += B98A2_Assert.Pass; fail += B98A2_Assert.Fail;`
//   · 宿主 = `Editor/BattleScene.cs`（它本来就有真 `HudCanvas`）；接线由**调度台**做（本件不改宿主）。
//   · 本文件不建持久资产：临时 `BattleDoors`（含临时 `EndPanel`）在各自 `finally` 里 `DestroyImmediate`。
//   · ⚠️ 与 `B97W41Assert` / `B97W8Assert` 都会建拆临时面板 ⇒ 三条**串行**跑没问题，⛔ 别并发。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：删掉 `FillRewardRow` 里那句 `SetAutoFitBox(...)` ⇒ A1 红。
//   · A2/A3/A4：`QuantityFsMin/Max/Base` 任一改错 ⇒ 对应条红。
//   · B4/B5：把 `FillRewardRow` 里那个 `* qk` 删掉（直接传原数）⇒ B4/B5 的设计当场自证「会偏 5.2%/5.5%」；
//     若哪天 B4 变绿而 B2 仍绿 ⇒ 说明系数已经是 1（换面了），要回来重核 B2/B3。
//   · C2：`BuildRewardsRow` 末尾那句 `SetActive(false)` 被删掉 ⇒ 生产态奖励行亮着 ⇒ 红。
//   · C4：把 `ApplyRewardVisibility` 写成「恒藏」⇒ 红（C2 与 C4 互为灭自证）。
//   · D1/D2/D4/D5：关自适应或把 `max` 抬到 60 ⇒ 撑出框 ⇒ 红。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B98_A2`：账 `日常/奖励:155` 的 `Quantity` 三档自适应 —— 复核 + 换算承重 + 生产态 + 一位数。
/// 计数口径照 `CheckSink`（与 `B97W41Assert` / `B97W8Assert` 同一套）。</summary>
public static class B98A2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[A2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 期望值（全部来自原版字段；出处见文件头，⛔ 不是拿我们自己的常量反算）----
    /// <summary>`MonoBehaviour_3928`：`m_fontSizeMin` / `m_fontSizeMax` / `m_fontSizeBase`（单位 = TMP fontSize）。</summary>
    const float WantMin = 18f, WantMax = 40f, WantBase = 36f;
    /// <summary>`RectTransform_3139`：框 = 140×0.5 + 0 × 45 = **70×45**。</summary>
    const float BoxW = 70f, BoxH = 45f;
    /// <summary>量的宽容度（设计 px）—— 文字墨迹框与框边留这一点，别拿「恰好等于」当判据。</summary>
    const float Eps = 0.5f;
    /// <summary>fontSize 档的宽容度（TMP 内部按 1/20 取整 + 二分收敛）。</summary>
    const float FsEps = 0.01f;

    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_A2 账 `日常/奖励:155`：`Quantity` 自适应 [18,40] base 36 —— 复核 + 换算承重 + 生产态 + 一位数");

        SectionA_三档();
        SectionB_换算承重();
        SectionC_生产态未喂();
        SectionD_一位数与五位数();
    }

    // ============================================================ 公共：建一台临时面板（与 W41 同一套写法）
    static EndPanel MakePanel(out Camera madeCam)
    {
        madeCam = null;
        if (LayoutSpace.Cam == null)
        {
            Debug.LogWarning("[A2] 宿主没有 `LayoutSpace.Cam` ⇒ 现场补一台临时相机（⛔ 不是跳过断言："
                           + "本文件的断言前提是「有一块真画布」，补不上就让 A0/B0/C0/D0 红）。");
            var camGo = new GameObject("B98A2_Cam");
            madeCam = camGo.AddComponent<Camera>();
            madeCam.clearFlags = CameraClearFlags.SolidColor;
            madeCam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(madeCam);
        }
        return EndPanel.Create(null);
    }

    static void KillPanel(EndPanel p, Camera madeCam)
    {
        if (p != null)
        {
            var d = p.Doors;
            if (d != null) Object.DestroyImmediate(d.gameObject);
            else if (p.gameObject != null) Object.DestroyImmediate(p.gameObject);
        }
        if (madeCam != null)
        {
            Object.DestroyImmediate(madeCam.gameObject);
            LayoutSpace.Cam = null;      // 临时相机的登记也要撤
        }
    }

    // ============================================================ A 三档独立复核
    static void SectionA_三档()
    {
        MenuCheck.Section(_sink, "A `Quantity` 三档独立复核：`enableAutoSizing` · 18 / 40 / 36（= 原版 `MB_3928` 四格）");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "A0 **前提断言**：宿主里建得出 `EndPanel`（`EndPanel.Create` 非 null）"
                            + " —— 不成立 ⇒ A1..A4 **没跑**（⛔ 不是静默跳过）");
            if (panel == null) return;

            panel.SetRewards(new List<RewardInfo> { new RewardInfo("C2", 7) });

            var r0 = panel.RewardRowAt(0);
            Tr(r0 != null && r0.Quantity != null,
               "A0b **前提断言**：第 0 行与它的数量字都建出来了（拿不到 TMP 字体时是 null ⇒ 本段没跑）");
            if (r0 == null || r0.Quantity == null) return;
            C(panel.QuantityTextOf(0), 7.ToString(), "A0c 前提：数量文本 = 它自己的 `Quantity`（7）");

            var q = r0.Quantity;
            Tr(q.AutoSizing, "A1 ★ `enableAutoSizing` = **true**（原版 `m_enableAutoSizing = 1`）"
                           + "｜🧨 删掉 `FillRewardRow` 里那句 `SetAutoFitBox(...)` ⇒ 红");
            N(q.FontSizeMin, WantMin, FsEps,
              "A2 ★ `m_fontSizeMin` = **18**（原版字段原值；单位 = TMP 的 `fontSize`，⛔ 不是画布 px）");
            N(q.FontSizeMax, WantMax, FsEps,
              "A3 ★ `m_fontSizeMax` = **40**（同上；⚠️ 与 `m_fontSize` 相等 ⇒ 短文本会被涨到 40）");
            N(q.FontSizeBase, WantBase, FsEps,
              "A4 ★ `m_fontSizeBase` = **36**（自适应二分的起点 `Clamp(base,min,max)`；反射读的真字段）"
            + "｜🧨 `QuantityFsMin/Max/Base` 改错一个 ⇒ 对应条红");
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }

    // ============================================================ B 换算承重（灭自证）
    static void SectionB_换算承重()
    {
        MenuCheck.Section(_sink, "B 换算**承重**证明：`x × FontSizeToPxNow(1)` 这一乘是必须的（不乘 ⇒ 读回 18/qk ≠ 18）");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "B0 **前提断言**：`EndPanel.Create` 非 null（不成立 ⇒ B1..B6 没跑）");
            if (panel == null) return;

            var one = new List<RewardInfo> { new RewardInfo("C2", 7) };
            panel.SetRewards(one);

            var r0 = panel.RewardRowAt(0);
            Tr(r0 != null && r0.Quantity != null, "B0b **前提断言**：第 0 行与数量字取得到");
            if (r0 == null || r0.Quantity == null) return;
            var q = r0.Quantity;

            // ---- B1/B2 前提：本标签空间的「1 个 fontSize = 多少设计 px」量得出来、且确实不是 1 ----
            float qk = q.FontSizeToPxNow(1f, q.ActualFace);
            Tr(qk > 0f, $"B1 **前提断言**：`FontSizeToPxNow(1)` 量得出来（实测 {qk:F4} 设计 px / fontSize）");
            Tr(Mathf.Abs(qk - 1f) > 0.01f,
               $"B2 **前提断言**：这个系数**确实不等于 1**（实测 {qk:F4}）—— 否则 B4/B5 证明不了「换算承重」"
             + "（⚠️ 若哪天换面到系数 = 1，本段要回来重核，⛔ 别把绿当成永久成立）");

            // ---- B3 生产口落回来的下限（= 18 × qk ÷ qk）----
            N(q.FontSizeMin, WantMin, FsEps,
              "B3 生产口（`FillRewardRow`）落回来的 `m_fontSizeMin` = 18（= `18 × qk ÷ qk`，`cur` 约掉）");

            // ---- B4/B5 灭自证：把一个**原版数字不换算**直接灌进同一个口 ⇒ 必须偏 ----
            q.SetAutoFitBox(BoxW, BoxH, WantMin, WantMax, WantBase);
            float rawMin = q.FontSizeMin;
            float rawMax = q.FontSizeMax;
            N(rawMin, WantMin / qk, 0.05f,
              $"B4 ★ **换算承重（灭自证）**：直接传原版 **18**（不乘 {qk:F4}）⇒ 读回 {rawMin:F3} = 18 ÷ {qk:F4}（**不是 18**）");
            N(rawMax, WantMax / qk, 0.05f,
              $"B4b 同上，上限：直接传 **40** ⇒ 读回 {rawMax:F3} = 40 ÷ {qk:F4}（**不是 40**）");
            Tr(Mathf.Abs(rawMin - WantMin) > 0.1f,
               $"B5 ★ 不换算那一版**确实偏离原版**（|{rawMin:F3} − 18| = {Mathf.Abs(rawMin - WantMin):F3} 个 fontSize）"
             + " ⇒ `FillRewardRow` 里那个乘号是**必须的**，⛔ 不是装饰");

            // ---- B6 复原：再走一次生产口 ⇒ 回到 18（B4 只是临时改动，没污染状态）----
            panel.SetRewards(one);
            var r0b = panel.RewardRowAt(0);
            Tr(r0b != null && r0b.Quantity != null, "B6 **前提断言**：复原后第 0 行与数量字仍在");
            if (r0b != null && r0b.Quantity != null)
            {
                N(r0b.Quantity.FontSizeMin, WantMin, FsEps,
                  "B6b ★ 再走一次生产口 ⇒ 下限**回到 18**（证明 B4 那一下只是临时改动、生产路径本身没坏）");
                N(r0b.Quantity.FontSizeMax, WantMax, FsEps, "B6c ★ 上限也回到 40");
            }
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }

    // ============================================================ C 今天生产路径的真实状态（一次都没喂）
    static void SectionC_生产态未喂()
    {
        MenuCheck.Section(_sink, "C 生产态（`SetRewards` 零生产调用点）：`Create()` 之后奖励行藏着 · 喂 1 条才露（两态互为灭自证）");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "C0 **前提断言**：`EndPanel.Create` 非 null（不成立 ⇒ C1..C4 没跑）");
            if (panel == null) return;

            var row = panel.RewardsRow;
            Tr(row != null, "C1 **前提断言**：`Build()` 把 `RewardsHolder` 建出来了（建成但藏着）");
            if (row == null) return;

            Tr(!row.gameObject.activeSelf,
               "C2 ★ 今天生产路径（全仓 `SetRewards` **零生产调用点**，见文件头）⇒ `Create()` 之后奖励行**藏着**"
             + "（⚠️ 语义边界：`SetRewards(空)` 那一档由 `B97W8Assert` 的 B2 覆盖，本段只补「**一次都没喂**」这一态）"
             + "｜🧨 删掉 `BuildRewardsRow` 末尾那句 `SetActive(false)` ⇒ 生产态亮着 ⇒ 红");
            Tr(panel.Rewards.Count == 0,
               $"C3 ★ 生产态奖励清单是空的（实测 {panel.Rewards.Count} 条；原版这边来自服务器 `EndMatchResult.Rewards`）");

            // ---- C4 对照：喂 1 条 ⇒ 同一行露出来（证明 C2 不是「恒藏」）----
            panel.SetRewards(new List<RewardInfo> { new RewardInfo("C2", 7) });
            Tr(row.gameObject.activeSelf,
               "C4 ★ 喂 1 条 ⇒ 同一行**露出来**｜🧨 把 `ApplyRewardVisibility` 写成「恒藏」⇒ C2 仍绿而 C4 红"
             + "（C2 / C4 互为灭自证）");
            C(panel.QuantityTextOf(0), 7.ToString(), "C4b 喂进去的那条文本 = 7");
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }

    // ============================================================ D 一位数 / 五位数：渲染尺寸 ≤ 框
    static void SectionD_一位数与五位数()
    {
        MenuCheck.Section(_sink, "D 溢出：`1`（一位）与 `12345`（五位）的**渲染宽高**都要 ≤ 框 70×45（⛔ 只比字号会漏）");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "D0 **前提断言**：`EndPanel.Create` 非 null（不成立 ⇒ D1..D6 没跑）");
            if (panel == null) return;

            panel.SetRewards(new List<RewardInfo>
            {
                new RewardInfo("C2", 1),
                new RewardInfo("C3", 12345),
            });

            var r1 = panel.RewardRowAt(0);
            var r5 = panel.RewardRowAt(1);
            Tr(r1 != null && r1.Quantity != null && r5 != null && r5.Quantity != null,
               "D0b **前提断言**：两行与它们的数量字都取得到（拿不到 TMP 字体时是 null ⇒ 本段没跑）");
            if (r1 == null || r1.Quantity == null || r5 == null || r5.Quantity == null) return;

            // ---- 一位数 ----
            float w1 = r1.Quantity.WorldW, h1 = r1.Quantity.WorldH;
            Tr(w1 <= BoxW + Eps, $"D1 ★ 一位数 `1` 的**渲染宽** {w1:F2} ≤ 框宽 **{BoxW}**");
            Tr(h1 <= BoxH + Eps, $"D2 ★ 一位数 `1` 的**渲染高** {h1:F2} ≤ 框高 **{BoxH}**");
            Tr(r1.Quantity.FontSize <= WantMax + FsEps,
               $"D3 一位数实收字号 {r1.Quantity.FontSize:F2} ≤ 上限 **40**（原版天花板 = `m_fontSizeMax`；"
             + "⛔ 不是「不超过 base 36」—— 那与 `MB_3928` 的 40 > 36 相反）");

            // ---- 五位数 ----
            float w5 = r5.Quantity.WorldW, h5 = r5.Quantity.WorldH;
            Tr(w5 <= BoxW + Eps,
               $"D4 ★ 五位数 `12345` 的**渲染宽** {w5:F2} ≤ 框宽 **{BoxW}**｜🧨 关自适应 / `max` 抬到 60 ⇒ 撑出框 ⇒ 红");
            Tr(h5 <= BoxH + Eps,
               $"D5 ★ 五位数 `12345` 的**渲染高** {h5:F2} ≤ 框高 **{BoxH}**（⛔ 只看字号会漏掉「折成两行」这一种溢出）");
            Tr(r5.Quantity.FontSize < WantMax - 0.1f,
               $"D6 五位数实收字号 {r5.Quantity.FontSize:F2} < 40 ⇒ 自适应**确实在压**，不是摆着不干活");
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }
}
