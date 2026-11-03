// B120_W21_Assert.cs — 执行写手 `B120-W21`（2026-11-02）· `B120-Q04c`：**拖牌落格链**按原版重写
//
// ============================ 它盯的是什么 ============================
// `Board/ArenaSlots.TryResolveSlot`（= 原版 `MinionManager.GetClosestAvailableSlot`）的四件事：
//   ① **候选集** = 每侧「最内空格 + 内圈已占格」，满侧整条剔除，**不含督军位**；
//   ② **距离** = **2D 欧氏屏幕像素**（⛔ 不是「先过 y 带、再比 |dx|」）；
//   ③ **没有容差**（原版只要还有空格就一定返回一个格）；
//   ④ **平手留更早下标**。
// 外加 ⑤ **投影基准 = 地面槽点**（`Position`，y = 0；⛔ 不是 `CardCenter`）。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`（逐句；本仓最可读的一份）
//   · `d:/2/tools/decomp_full/MinionManager__GetClosestAvailableSlot.c:35-145`（互验一致）
//   · 候选集 `start = 4 − 左已占 − 1` / `end = 4 + 1 + 右已占`（`:328-342`）· 距离 `:359-369` · 平手严格不等
//   · 投影基准 `GetSlots` = `TransformPoint(new Vector3(x, 0, 0))`（`:149-176`）
//   · `B120-Q04c` 差异表 / 溯源 → `资料/完全复刻/报告/B120_W18_未决三条.md` §一
//
// ============================ 怎么用 ============================
//   `B120_W21_Assert.Run();`   （静态类 + 无参 `Run()`；**本文件尚未接线**，宿主与聚合行由调度台决定）
//   探针相机在 `finally` 里 `DestroyImmediate`（不建持久资产）。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1/A3 ← 把候选集改回「9 格全算」⇒ A1 的计数变 9 ⇒ 红
//   · A2 ← 把督军位放回候选 ⇒ 红（25 种占位组合全扫）
//   · B1 ← 把距离改回「只比 |dx|」⇒ 两个方向各有一条 ⇒ 红
//   · B2 ← 把平手改成「后写的赢」⇒ 红
//   · B4 ← 把容差加回来 ⇒ 远处那一点变 -1 ⇒ 红
//   · C2/C3 ← 把容差（`tolY` / `0.55×pitch`）加回来 ⇒ 边界点从「落上」变「落不上」⇒ 红
//            （C3 还会先把**旧口径快照**跑一遍，证明这一点旧口径真的会拒 —— 否则本组证明不了任何事）
//   · C4 ← 不剔除满侧 ⇒ 指针压在槽 3 的地面点上会答 3 而不是 5 ⇒ 红
//   · C5 ← 把督军位放回候选 ⇒ 指针压在中线附近会答 4 ⇒ 红
//   · D1/D2 ← 容差写回 `ArenaSlots` / 把 `CardInteraction` 的四处落点解析改回不带占位数 ⇒ 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：C 段是编辑模式下的一次性投影探针（`Camera.WorldToScreenPoint` 在编辑模式可用，
//     用 `RenderTexture(1920×1080)` 把 `pixelWidth/Height` 钉死，免得批处理的屏幕尺寸把像素距离压成 ~0）。
//   · **不断言「真机手感」**：那要真 Play（`资料/真Play待验清单.md`）。
//   · **2D 兜底档不在本件范围**：`BoardLayout.TryResolveSlot` 没有 3D 相机时仍走我们自己的阈值判据（`B8_AR`，仍记 B）。
//   · **`GetTargetInPos`（射线拾取一张卡）是另一条链**，归 `B120-W13`。
using System;
using System.IO;
using System.Text;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B120-W21`：拖牌落格链（原版 `MinionManager.GetClosestAvailableSlot`）的判据断言。</summary>
public static class B120_W21_Assert
{
    const string Prefix = "[B120W21] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>只记录、不计分（「判别力」「前提读数」这类**不是断言**的量）。</summary>
    static void Tr(string what) { Debug.Log(Prefix + "· " + what); }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B120-W21：拖牌落格链（`GetClosestAvailableSlot`）开始 ===");
        A_Candidates();
        B_Nearest();
        C_EndToEnd();
        D_SourceGate();
        Debug.Log(Prefix + "B120_W21 合计：✓ " + _ok + " / ✗ " + _bad + "（共 " + (_ok + _bad) + " 条）");
    }

    // ==================================================================
    //  A · 候选集（纯函数 `ArenaSlots.IsCandidate`，不需要相机）
    //     判据 `MinionManager.cs:326-342`
    // ==================================================================
    static void A_Candidates()
    {
        // A1 —— 两侧皆空 ⇒ 只有两侧最内空格（⛔ 不是 9 格全算、也不是 8 格）
        string c00 = Cands(0, 0);
        Check(c00 == "3,5",
            "A1 ★ 两侧皆空 ⇒ 候选 = **槽 3 与槽 5**（两侧最内空格），实得 [" + c00 + "]"
            + "｜判据 `MinionManager.cs:328-342`：`start = 4−0−1 = 3` · `end = 4+1+0 = 5` ⇒ 拼接数组下标 3/4"
            + "｜🧨 改回「9 格全算」⇒ 这里变 `0,1,…,8` ⇒ 红");

        // A2 —— 督军位（4 号）**任何时候**都不是候选（25 种占位组合）
        bool anyWarlord = false;
        for (int l = 0; l <= 4; l++)
            for (int r = 0; r <= 4; r++)
                if (ArenaSlots.IsCandidate(ArenaSlots.WarlordSlot, l, r)) anyWarlord = true;
        Check(!anyWarlord,
            "A2 ★ 督军位（4 号）**恒不是候选**（左 0..4 × 右 0..4 共 25 种组合全扫）"
            + " —— 原版候选里根本没有督军格，返回值 `0` 是「无」不是督军格"
            + "｜🧨 把 4 号格放回候选 ⇒ 红");

        // A3 —— 逐档：最内空格 + 内圈已占格；**满侧整条剔除**
        var cases = new[]
        {
            new object[] { 0, 0, "3,5",       "两侧皆空" },
            new object[] { 1, 0, "2,3,5",     "左 1：最内已占 ⇒ 空位内移一格" },
            new object[] { 2, 0, "1,2,3,5",   "左 2" },
            new object[] { 3, 0, "0,1,2,3,5", "左 3：只剩最外一格空 ⇒ 四格全在候选" },
            new object[] { 4, 0, "5",         "⚠️ 左侧满 ⇒ **整条剔除**（原版 start=4）" },
            new object[] { 0, 4, "3",         "⚠️ 右侧满 ⇒ 整条剔除（原版 end=4）" },
            new object[] { 4, 4, "",          "两侧都满 ⇒ **一个候选都没有**（原版 return 0 = 无）" },
            new object[] { 2, 2, "1,2,3,5,6,7", "两侧各 2" },
            new object[] { -1, -1, "3,5",     "−1 = 调用方数不出来 ⇒ 按「两侧皆空」（⛔ 不是按「满」）" },
        };
        int badA3 = 0;
        foreach (var cs in cases)
        {
            int l = (int)cs[0], r = (int)cs[1];
            string want = (string)cs[2];
            string got = Cands(l, r);
            if (got != want)
            {
                badA3++;
                Debug.LogError(Prefix + "  ↳ A3 不符：左 " + l + " / 右 " + r + " 期望 [" + want + "] 实得 [" + got + "]");
            }
        }
        Check(badA3 == 0,
            "A3 ★ 候选集逐档（9 档：两侧皆空 / 左 1..3 / 左满剔除 / 右满剔除 / 双满 / 两侧各 2 / −1）"
            + " —— 全部符合「最内空格 + 内圈已占格、满侧整条剔除」；不符 " + badA3 + " 档"
            + "｜判据 `MinionManager.cs:328-342` · `decomp_full/MinionManager__GetClosestAvailableSlot.c:35-95`");

        // A4 —— 候选数上界 = 8（= 两侧各 4 − 督军位），⛔ 永远到不了 9
        int max = 0;
        for (int l = 0; l <= 4; l++)
            for (int r = 0; r <= 4; r++)
            {
                int n = 0;
                for (int s = 0; s < ArenaSlots.SlotCount; s++) if (ArenaSlots.IsCandidate(s, l, r)) n++;
                if (n > max) max = n;
            }
        Check(max == 8,
            "A4 ★ 候选数上界 = **8**（两侧各 4，督军位恒不在内），实得最大 " + max
            + "｜🧨 上界变 9 ⇒ 督军位混进来了");
    }

    /// <summary>这一组占位下的候选格号（升序、逗号分隔）—— 断言用读数口。</summary>
    static string Cands(int l, int r)
    {
        var sb = new StringBuilder();
        for (int s = 0; s < ArenaSlots.SlotCount; s++)
            if (ArenaSlots.IsCandidate(s, l, r))
            { if (sb.Length > 0) sb.Append(','); sb.Append(s); }
        return sb.ToString();
    }

    // ==================================================================
    //  B · 距离与平手（纯函数 `ArenaSlots.PickNearest`，喂**手搓的点集**）
    //     判据 `MinionManager.cs:359-369`
    // ==================================================================
    static void B_Nearest()
    {
        // B1 —— 2D 欧氏 ≠ 「只比 |dx|」：两个方向各一条，保证夹具真的有判别力
        {
            var pts = Blank();
            pts[3] = new Vector2(100f, 100f);
            pts[5] = new Vector2(400f, 900f);
            var p = new Vector2(390f, 100f);            // |dx|: 10（槽5）· 欧氏: 290（槽3）
            int eu = ArenaSlots.PickNearest(pts, p, 0, 0);
            int dxOnly = DxOnly(pts, p, 0, 0);
            Check(eu == 3 && dxOnly == 5,
                "B1 ★ 距离 = **2D 欧氏**：指针 (390,100) · 槽3=(100,100) · 槽5=(400,900) ⇒ 答 **3**（欧氏 290 < 800.06）"
                + "，实得 " + eu + "；旧的「只比 |dx|」口径会答 " + dxOnly + "（= 5）"
                + "｜判据 `MinionManager.cs:359-369`（`sqrt(dx²+dy²)`）"
                + "｜🧨 改回只比 |dx| ⇒ 这里答 5 ⇒ 红（前提：本夹具能分开两种口径）");
        }
        {
            var pts = Blank();
            pts[3] = new Vector2(100f, 100f);
            pts[5] = new Vector2(400f, 900f);
            var p = new Vector2(110f, 900f);            // |dx|: 10（槽3）· 欧氏: 290（槽5）
            int eu = ArenaSlots.PickNearest(pts, p, 0, 0);
            int dxOnly = DxOnly(pts, p, 0, 0);
            Check(eu == 5 && dxOnly == 3,
                "B2 ★ 反方向（灭「碰巧对」）：指针 (110,900) ⇒ 欧氏答 **5**（290 < 800.06）、只比 |dx| 会答 3"
                + "，实得 欧氏 " + eu + " / |dx| " + dxOnly
                + "｜🧨 距离写错方向/写成平方差 ⇒ 两条里至少一条红");
        }

        // B3 —— 平手留更早下标（原版严格 `>`；两个方向各试一次）
        {
            var pts = Blank();
            pts[3] = new Vector2(100f, 100f);
            pts[5] = new Vector2(300f, 100f);
            Check(ArenaSlots.PickNearest(pts, new Vector2(200f, 100f), 0, 0) == 3,
                "B3 ★ 平手 ⇒ 留**更早下标**（等距 100/100 ⇒ 答槽 3，⛔ 不是 5）"
                + "｜判据 `MinionManager.cs:365-369`：`if (best > d)` 严格不等 ⇒ 先遇到的那一个赢"
                + "｜🧨 改成 `<=` ⇒ 答 5 ⇒ 红");
        }

        // B4 —— **没有容差**：指针离所有候选都很远，照样返回一个格
        {
            var pts = Blank();
            pts[3] = new Vector2(100f, 100f);
            pts[5] = new Vector2(300f, 100f);
            int far = ArenaSlots.PickNearest(pts, new Vector2(5000f, -5000f), 0, 0);
            Check(far == 5,
                "B4 ★ **无容差**：指针在 (5000,−5000)（离两格都几千 px）照样答槽 **5**（两格里它更近），实得 " + far
                + "｜判据 `MinionManager.cs:365`（`bestIdx` 只因「一个候选都没有」才为 −1）"
                + "｜🧨 把容差加回来 ⇒ 这里变 −1 ⇒ 红");
        }

        // B5 —— 满侧整条剔除（行为层）：左侧满时，即使槽 3 距离为 0 也不该被选中
        {
            var pts = Blank();
            pts[3] = new Vector2(100f, 100f);
            pts[5] = new Vector2(300f, 100f);
            int s = ArenaSlots.PickNearest(pts, new Vector2(100f, 100f), 4, 0);
            Check(s == 5,
                "B5 ★ 左侧已满（4）⇒ 指针正压在槽 3 的点上（距离 0）也**只能答槽 5**，实得 " + s
                + "｜判据 `MinionManager.cs:331-334`（满侧 `start = 4` ⇒ 左半不在候选里）"
                + "｜🧨 不剔除满侧 ⇒ 答 3 ⇒ 红");
        }

        // B6 —— 两侧都满 ⇒ −1（原版 return 0 = 无）
        {
            var pts = Blank();
            pts[3] = new Vector2(100f, 100f);
            pts[5] = new Vector2(300f, 100f);
            int s = ArenaSlots.PickNearest(pts, Vector2.zero, 4, 4);
            Check(s == -1,
                "B6 ★ 两侧都满 ⇒ **−1**（原版同一情形 `return 0` = 无），实得 " + s
                + "｜🧨 全满时乱给一格 ⇒ 红（那会绕过引擎的 `NotEnoughRoom`）");
        }
    }

    /// <summary>非候选位填一个「不可能赢」的点（候选集变宽时它们会露馅 ⇒ 是刻意的）。</summary>
    static Vector2[] Blank()
    {
        var pts = new Vector2[ArenaSlots.SlotCount];
        for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(1e6f, 1e6f);
        return pts;
    }

    /// <summary>**旧口径快照**（`B120-W21` 删掉的那一份：先过 y 带、再比 |dx|、最后 `0.55×pitch` 截断）。
    /// ⚠️ 只用于证明「夹具分得开两种口径」，⛔ 不是被测实现。</summary>
    static int DxOnly(Vector2[] pts, Vector2 p, int l, int r)
    {
        int best = -1; float bestDx = float.MaxValue;
        for (int s = 0; s < ArenaSlots.SlotCount; s++)
        {
            if (!ArenaSlots.IsCandidate(s, l, r)) continue;
            float dx = Mathf.Abs(pts[s].x - p.x);
            if (dx < bestDx) { bestDx = dx; best = s; }
        }
        return best;
    }

    /// <summary>**旧实现快照**（含两道容差 + 卡心基准）—— 判据 = 2026-11-02 之前 `ArenaSlots.TryResolveSlot` 的原文。
    /// ⚠️ 只用于证明 C 段的边界点确实在「旧口径的拒绝区」里（否则那两条证明不了任何事），⛔ 不是被测实现。</summary>
    static bool OldToleranceResolve(Camera cam, Vector2 screenPos, bool enemy, out int slot)
    {
        slot = -1;
        float sc = ArenaSlots.CardScale(enemy);
        var rowMe = cam.WorldToScreenPoint(ArenaSlots.CardCenter(ArenaSlots.WarlordSlot, enemy, sc));
        var rowFoe = cam.WorldToScreenPoint(ArenaSlots.CardCenter(ArenaSlots.WarlordSlot, !enemy, ArenaSlots.CardScale(!enemy)));
        float tolY = Mathf.Abs(rowFoe.y - rowMe.y) * 0.5f;
        float pitchPx = Mathf.Abs(cam.WorldToScreenPoint(ArenaSlots.CardCenter(ArenaSlots.WarlordSlot + 1, enemy, sc)).x - rowMe.x);
        int bestS = -1; float bestDx = float.MaxValue;
        for (int s = 0; s < ArenaSlots.SlotCount; s++)
        {
            var sp = cam.WorldToScreenPoint(ArenaSlots.CardCenter(s, enemy, sc));
            if (Mathf.Abs(sp.y - screenPos.y) > tolY) continue;
            float dx = Mathf.Abs(sp.x - screenPos.x);
            if (dx < bestDx) { bestDx = dx; bestS = s; }
        }
        if (bestS < 0 || bestDx > pitchPx * 0.55f) return false;
        slot = bestS;
        return true;
    }

    // ==================================================================
    //  C · 端到端（真相机 + 地面槽点投影）—— 投影基准 / 无容差 / 满侧剔除 / 督军不含
    // ==================================================================
    static void C_EndToEnd()
    {
        var camGo = new GameObject("B120W21_cam");
        var rt = new RenderTexture(1920, 1080, 24);
        Camera cam = null;
        try
        {
            // 有俯仰的透视相机：两行投到**不同的屏幕 y** ⇒ 才分得开「2D 欧氏」与「只比 |dx|」
            camGo.transform.position = new Vector3(0f, 4f, -12f);
            camGo.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            cam.targetTexture = rt;          // 钉死像素尺寸：批处理里屏幕可能很小，像素距离会被压成 ~0

            Check(cam.pixelWidth == 1920 && cam.pixelHeight == 1080,
                "C0（前提）探针相机像素 **1920×1080**（实得 " + cam.pixelWidth + "×" + cam.pixelHeight
                + "）—— 像素尺寸靠 `RenderTexture` 钉死，否则批处理里像素距离不可信");

            // C1 —— 每一格的**地面槽点**喂回去都要解回自己（= 投影基准正确 + 候选集正确）
            int n = 0, bad = 0;
            var combos = new[] { Tuple.Create(0, 0), Tuple.Create(3, 2) };
            foreach (var cb in combos)
                for (int s = 0; s < ArenaSlots.SlotCount; s++)
                {
                    if (!ArenaSlots.IsCandidate(s, cb.Item1, cb.Item2)) continue;
                    n++;
                    var sp = ArenaSlots.GroundScreenPoint(cam, s, false);
                    int back;
                    if (!ArenaSlots.TryResolveSlot(cam, sp, false, cb.Item1, cb.Item2, out back) || back != s) bad++;
                }
            Check(n == 9 && bad == 0,
                "C1 ★ 往返：每格**地面槽点**投影后喂回解析 ⇒ 解回自己（" + (n - bad) + "/" + n
                + "；两组占位 左0右0 候选2格 + 左3右2 候选7格）"
                + "｜判据 `GetSlots` = `TransformPoint(x, 0, 0)`（`MinionManager.cs:149-176`）"
                + "｜🧨 投影基准改回卡心 / 候选集算错 ⇒ 至少一格解不回来 ⇒ 红");

            // C2 —— **无容差**（横向）：远远超出旧 `0.55×pitch` 截断的点，照样落到最近那一格
            {
                var p3 = ArenaSlots.GroundScreenPoint(cam, 3, false);
                var p5 = ArenaSlots.GroundScreenPoint(cam, 5, false);
                float pitchPx = Mathf.Abs(p5.x - p3.x);
                var far = new Vector2(p5.x + 3f * pitchPx, p5.y);
                int got;
                bool ok = ArenaSlots.TryResolveSlot(cam, far, false, 0, 0, out got);
                bool oldOk = OldToleranceResolve(cam, far, false, out _);
                Check(ok && got == 5,
                    "C2 ★ **横向无容差**：指针在槽 5 右边 **3×格距**（" + (pitchPx).ToString("F1") + " px/格 ⇒ 偏 "
                    + (3f * pitchPx).ToString("F0") + " px）仍答槽 **5**（实得 ok=" + ok + " 槽 " + got + "）"
                    + "｜🧨 把 `bestDx > pitchPx * 0.55f` 那道截断加回来 ⇒ ok=false ⇒ 红");
                Check(!oldOk,
                    "C2b（灭自证前提）同一点喂给**旧口径快照**（含 `0.55×pitch` 截断）⇒ **拒**（实得 ok=" + oldOk
                    + "）—— 否则 C2 证明不了任何事");
            }

            // C3 —— **无容差**（纵向）：旧 `tolY` 带之外的同一行点，照样落到最近那一格
            {
                var p3 = ArenaSlots.GroundScreenPoint(cam, 3, false);
                var p5 = ArenaSlots.GroundScreenPoint(cam, 5, false);
                var q3e = ArenaSlots.GroundScreenPoint(cam, 3, true);        // 敌方那一行（取两行间距）
                float rowPx = Mathf.Abs(q3e.y - p3.y);
                var up = new Vector2(p5.x, p5.y + 3f * rowPx);
                int got;
                bool ok = ArenaSlots.TryResolveSlot(cam, up, false, 0, 0, out got);
                bool oldOk = OldToleranceResolve(cam, up, false, out _);
                Check(ok && got == 5,
                    "C3 ★ **纵向无容差**：指针在槽 5 上方 **3×两行间距**（" + (3f * rowPx).ToString("F0")
                    + " px）仍答槽 **5**（实得 ok=" + ok + " 槽 " + got + "）"
                    + "｜🧨 把 `tolY = 两行投影间距 × 0.5` 那条 y 带加回来 ⇒ ok=false ⇒ 红");
                Check(!oldOk,
                    "C3b（灭自证前提）同一点喂给**旧口径快照**（含 `tolY` 带）⇒ **拒**（实得 ok=" + oldOk + "）");
            }

            // C4 —— 满侧整条剔除（端到端）：左侧满 ⇒ 指针正压在槽 3 地面点上也只能答 5
            {
                int got;
                bool ok = ArenaSlots.TryResolveSlot(cam, ArenaSlots.GroundScreenPoint(cam, 3, false), false, 4, 0, out got);
                Check(ok && got == 5,
                    "C4 ★ 左侧已满（4）⇒ 指针压在**槽 3 的地面点**上仍答槽 **5**（实得 ok=" + ok + " 槽 " + got + "）"
                    + "｜判据 `MinionManager.cs:331-334`"
                    + "｜🧨 不剔除满侧 ⇒ 答 3 ⇒ 红");
            }

            // C5 —— 不含督军位：指针压在中线偏左 ⇒ 答 3，⛔ 永远不答 4
            {
                var p4 = cam.WorldToScreenPoint(ArenaSlots.Position(ArenaSlots.WarlordSlot, false) + new Vector3(-0.05f, 0f, 0f));
                int got;
                bool ok = ArenaSlots.TryResolveSlot(cam, new Vector2(p4.x, p4.y), false, 0, 0, out got);
                Check(ok && got == 3,
                    "C5 ★ 指针压在**督军位地面点**（偏左 0.05 世界单位）⇒ 答槽 **3**，⛔ 不答 4（实得 ok=" + ok + " 槽 " + got + "）"
                    + "｜🧨 把督军位放回候选 ⇒ 答 4 ⇒ 红");
            }

            // C6 —— 两侧都满 ⇒ false（原版 return 0 = 无）
            {
                int got;
                bool ok = ArenaSlots.TryResolveSlot(cam, ArenaSlots.GroundScreenPoint(cam, 5, false), false, 4, 4, out got);
                Check(!ok && got == -1,
                    "C6 ★ 两侧都满 ⇒ **false / −1**（实得 ok=" + ok + " 槽 " + got + "）—— 原版同一情形 `return 0` = 无");
            }

            // C7 —— 投影基准可区分（前提读数）：地面槽点与卡心在这一台相机下投到**不同**的屏幕 y
            {
                float sc = ArenaSlots.CardScale(false);
                var g = ArenaSlots.GroundScreenPoint(cam, 5, false);
                var c = cam.WorldToScreenPoint(ArenaSlots.CardCenter(5, false, sc));
                float dy = Mathf.Abs(c.y - g.y);
                Check(dy > 5f,
                    "C7（前提）这台倾斜相机下「地面槽点 ↔ 卡心」的屏幕 y 差 **" + dy.ToString("F1")
                    + " px**（> 5）—— 所以「用哪个基准」是可观测的（太接近 ⇒ C1 区分不出基准）");
                Tr("C7 读数：槽 5 地面 (" + g.x.ToString("F1") + "," + g.y.ToString("F1")
                   + ") · 卡心 (" + c.x.ToString("F1") + "," + c.y.ToString("F1") + ")");
            }
        }
        finally
        {
            if (cam != null) cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(camGo);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    // ==================================================================
    //  D · 源码闸（形状；⛔ 不用我们的常量反推原版）
    // ==================================================================
    static void D_SourceGate()
    {
        string arenaPath = Path.Combine(Application.dataPath, "CardPresentation/Board/ArenaSlots.cs");
        string handPath = Path.Combine(Application.dataPath, "CardPresentation/Hand/CardInteraction.cs");
        Check(File.Exists(arenaPath), "D0a（前提）源码文件在：" + arenaPath + "（⛔ 读不到就红，别改成跳过）");
        Check(File.Exists(handPath), "D0b（前提）源码文件在：" + handPath);
        if (!File.Exists(arenaPath) || !File.Exists(handPath)) return;

        string arena = StripComments(File.ReadAllText(arenaPath));
        int i0 = arena.IndexOf("int occupiedLeft, int occupiedRight, out int slot)", StringComparison.Ordinal);
        Check(i0 >= 0, "D1a（前提）能在源码里切出**带占位数**的那个重载（6 参）");
        if (i0 >= 0)
        {
            string body = arena.Substring(i0);        // 它是本类**最后一个**方法 ⇒ 切到文件尾即可
            Check(body.IndexOf("tolY", StringComparison.Ordinal) < 0
                  && body.IndexOf("0.55", StringComparison.Ordinal) < 0
                  && body.IndexOf("pitchPx", StringComparison.Ordinal) < 0,
                "D1 ★ 6 参重载体里**没有** `tolY` / `pitchPx` / `0.55` —— 两道容差真的删了"
                + "｜🧨 把容差写回来 ⇒ 红（行为面另有 C2/C3 兜着）");
            Check(body.IndexOf("CardCenter(", StringComparison.Ordinal) < 0
                  && body.IndexOf("GroundScreenPoint(", StringComparison.Ordinal) >= 0,
                "D3 ★ 6 参重载体里**没有 `CardCenter(`**、有 `GroundScreenPoint(` —— 投影基准 = **地面槽点**"
                + "｜🧨 改回卡心 ⇒ 红");
            Check(body.IndexOf("PickNearest(", StringComparison.Ordinal) >= 0,
                "D2a ★ 6 参重载体走的是 `PickNearest(`（距离那一跳只此一处）"
                + "｜🧨 在重载里另写一份循环 ⇒ 红");
        }

        // D2/D4 —— 直接盯 `PickNearest` 的循环体（距离公式 + 候选集收窄）
        int j0 = arena.IndexOf("public static int PickNearest(", StringComparison.Ordinal);
        int j1 = arena.IndexOf("public static Vector2 GroundScreenPoint(", StringComparison.Ordinal);
        Check(j0 >= 0 && j1 > j0, "D2b（前提）能切出 `PickNearest` 的方法体（" + j0 + " → " + j1 + "）");
        if (j0 >= 0 && j1 > j0)
        {
            string pick = arena.Substring(j0, j1 - j0);
            Check(pick.IndexOf("Mathf.Sqrt(", StringComparison.Ordinal) >= 0,
                "D2 ★ `PickNearest` 里是 `Mathf.Sqrt(` —— **2D 欧氏**（原版 `Math.Sqrt`，只取 x/y）"
                + "｜🧨 改成只比 |dx| / 改成平方差 ⇒ 红（行为面 B1/B2 兜着）");
            Check(pick.IndexOf("IsCandidate(s, occupiedLeft, occupiedRight)", StringComparison.Ordinal) >= 0,
                "D4 ★ `PickNearest` 的循环用 `IsCandidate(s, occupiedLeft, occupiedRight)` 收窄候选"
                + "｜🧨 改成 `for (s = 0; s < SlotCount; s++)` 全算 ⇒ 红（行为面 A1/A3 兜着）");
        }

        string hand = StripComments(File.ReadAllText(handPath));
        int nSide = CountOf(hand, "SideCount(");
        Check(nSide >= 9 && (nSide - 1) % 2 == 0,
            "D5 ★ `CardInteraction` 的 `SideCount(` **成对出现**（每处落点解析 2 侧）+ 1 处定义 ⇒ `≥ 9` 且 `(n-1)` 为偶数"
            + "（实得 " + nSide + "）—— 产品路径真的把「每侧已占数」喂进去了"
            + "｜🔴 2026-11-03（`B124-W10`）：原来是**钉死 9**（4 处解析 × 2 + 1 定义），`B120-Q04c4b` 之后多了第 5 处解析 ⇒ 现读 11。"
            + "改成「成对 + 下限」：既能挡「删掉任一处（变 7/5/3）」，也不会因为**新增**解析点而假红"
            + "｜🧨 把任一处改回不带数的重载 ⇒ 落单 ⇒ 红（那会退化成「两侧皆空」的候选集）");
        int bare = CountOf(hand, ", out sWant);") + CountOf(hand, ", out _);") + CountOf(hand, ", out s) &&");
        Check(bare == 0,
            "D6 ★ `CardInteraction` 里**没有**不带占位数的落点解析（`TryResolveSlot(pos, out sWant);` / "
            + "`TryResolveSlot(pos, out _);` / `TryResolveSlot(pos, out s) &&` 各 0 处，实得 " + bare + " 处）"
            + "｜⛔ 产品路径不许走「两侧皆空」那条默认 ⇒ 否则候选集只剩两侧最内空格");
    }

    /// <summary>去掉 `//` 与 `/* */` 注释（保留字符串内容）—— 注释里的示例不算接线。</summary>
    static string StripComments(string s)
    {
        var sb = new StringBuilder(s.Length);
        bool line = false, block = false, str = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char n = i + 1 < s.Length ? s[i + 1] : '\0';
            if (line)
            {
                if (c == '\n' || c == '\r') { line = false; sb.Append(c); }
                continue;
            }
            if (block)
            {
                if (c == '*' && n == '/') { block = false; i++; }
                continue;
            }
            if (str)
            {
                sb.Append(c);
                if (c == '\\' && n != '\0') { sb.Append(n); i++; continue; }   // 转义
                if (c == '"') str = false;
                continue;
            }
            if (c == '/' && n == '/') { line = true; i++; continue; }
            if (c == '/' && n == '*') { block = true; i++; continue; }
            if (c == '"') { str = true; sb.Append(c); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    static int CountOf(string s, string what)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(what, i, StringComparison.Ordinal)) >= 0) { n++; i += what.Length; }
        return n;
    }
}
