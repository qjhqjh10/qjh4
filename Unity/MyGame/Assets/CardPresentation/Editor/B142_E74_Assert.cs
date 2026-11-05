// B142_E74_Assert.cs — 执行写手 `B142-E74`（2026-11-04 · 完全复刻）
//
// ============================ 它钉的是什么 ============================
// 判据 = `资料/完全复刻/判据/B90_N2_小屏字号实例.tsv`（原版 `bundle_menus_assets_all` 里
//   `EverguildTextMeshPro.smallScreenMaxFontSize/MinFontSize > 0` 的 **79 颗**，2026-10-28 逐 MB 现读）。
// 落地模型（照原版）：`EverguildTextMeshPro.Awake` 末尾内联的那一段 —— 两格 > 0 才改
//   `fontSizeMax`（开自适应时）/ `fontSizeMin`，**只改字号、不碰 `sizeDelta` / 折行宽**，
//   且两道闸：`Application.isPlaying` ∧ `GameStaticData.smallScreenUI`（我们的等价物 = `SmallScreenUI.Enabled`）。
//
// 本波做的就是「77 颗实例接线」那一半：在各自**建点**的 `SetAutoFitBox` 之后落
//   `lb.SetSmallScreenFontSizes(maxPx, minPx)`（数值逐颗照 TSV）。
// 所以本断言分两组：
//   A 组（自包含 · 不依赖宿主）：`Label` 那一侧的**语义**是否照原版 ——
//     A1 两格原样记下（= 接线读口不是摆设）· A2 **`smallScreenUI=false` ⇒ 字号三格逐位不变**
//     （★ 本波的核心承诺：桌面逐像素不变）· A3 幂等 · A4 不碰框 · A5 两颗标签互不串值。
//   B 组（站点 · 宿主建好窗之后调）：每一颗**接线过**的标签 = 两格对得上 TSV **且只被接了一次**。
//
// ============================ 怎么用（宿主接线归调度台）============================
//   · 自带夹具、**可直接跑**（A 组不依赖任何窗）：
//       unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//         -projectPath "D:\4\Unity\MyGame" -executeMethod B142_E74_Assert.Run -logFile -
//   · B 组：宿主在某扇窗 `Build()` 之后调
//       B142E74_Assert.CheckStation(root, "<相对路径>", 45f, 0f, "<这一站是什么>");
//     或直接拿 `Label` 调 `CheckLabel(lb, 45f, 0f, "...")`。
//     逐窗表格见 `资料/完全复刻/报告/B142_E74.md` §三（79 颗 → 建点文件:行号）。
//   · 🔴 **计数纪律（2026-11-05 · B143-W4 加注）**：`Run()` 开头会 `_pass = 0; _fail = 0;`（`:69`）⇒
//     宿主若把 B 组站点断言跑在 `Run()` **之前**（`ShellScene` 就是这样：`:5511` / `:5540` / `:7831`），
//     **必须先把 `Pass`/`Fail` 抓下来再调 `Run()`**（先例 `ShellScene.cs:11301` 的 `int e74bP = …`）——
//     不抓就会把 B 组那几十条**静默清零**，而合计看着还是绿的。
//   · 全量搬树口径：`B142E74_Assert.Dump(root, "<窗名>")` 把这一棵树里**所有接了线的标签**
//     逐颗印出 `路径 → (max,min)`，用来人工对表。
//
// ============================ 判绿红 ============================
// 按**行首** `B142E74 ✓` / `B142E74 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
// `… 未验：` 单独计数、**不算通过**（真机 / 播放态才成立的那几条，在编辑器批处理里恒为未验）。
//
// ⚠️ 期望值**全部是字面量**（45/0 · 110/0 · 38/10 …照 TSV），⛔ 不用被测代码去算期望（同义反复）。
using UnityEngine;
using CardPresentation;

public static class B142E74_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B142E74";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, string msg)
    {
        Check(Mathf.Abs(got - want) <= 0.01f,
              msg + "（期望 " + want.ToString("F2") + "；实得 " + got.ToString("F2") + "）");
    }

    static void MarkUnverified(string msg) { _unverified++; Debug.Log(Tag + " … 未验：" + msg); }

    static void Section(string s) { Debug.Log(Tag + " ---- " + s + " ----"); }

    // ==================================================================
    //  A 组 —— `Label` 语义（自包含 · 自建夹具）
    // ==================================================================
    /// <summary>自包含的一组。⚠️ 编辑器批处理里 `Application.isPlaying == false` ⇒
    /// `ApplySmallScreenTextSize` 第一道闸就返回（**原版同此**）⇒ A2 断的就是「桌面 / 非播放态逐像素不变」。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0; _unverified = 0;
        Section("B142_E74 A 组：`Label.SetSmallScreenFontSizes` 语义（判据 `B90_N2_小屏字号实例.tsv`）");

        bool wasPersist = SmallScreenUI.PersistOverride;
        SmallScreenUI.PersistOverride = true;              // 自检不动玩家的真设置
        bool wasEnabled = SmallScreenUI.Enabled;
        GameObject root = null;
        try
        {
            SmallScreenUI.Set(false);                      // 小屏 UI **关**（出厂档）
            Check(!SmallScreenUI.Enabled, "（前提）`SmallScreenUI.Enabled == false`（出厂档；自检注入不落盘）");

            root = new GameObject("B142E74 root");
            var lb = Label.Create(root.transform, "45", Vector3.zero, 5, Color.white,
                                  new Vector2(0.5f, 0.5f), "b142e74 probe");
            bool tmpOk = lb != null && lb.CanRenderChinese;
            Check(tmpOk, "（前提）`Label.Create` 建出了 TMP 后端（点阵后端下小屏档整支不生效、下面几条无从谈起）");
            if (!tmpOk) return;

            float fs0 = lb.FontSize, fsMin0 = lb.FontSizeMin, fsMax0 = lb.FontSizeMax;
            var box0 = (lb.transform as RectTransform)?.sizeDelta ?? Vector2.zero;   // 🔴 2026-11-05（`B148`）：原来硬转 `(RectTransform)lb.transform`，非 RectTransform 时抛 InvalidCast 把整个 `ShellScene.RunCore` 打断（`:11301`）。改 `as` + `??` 兜住。
            Check(Mathf.Approximately(lb.SmallScreenMaxRequestedPx, 0f) && Mathf.Approximately(lb.SmallScreenMinRequestedPx, 0f),
                  "A0（灭自证前提）**没接线**的标签两格都是 0（= 站点漏了时 B 组会红的那种形状）");
            Check(lb.SmallScreenSetCountForTest == 0, "A0b（灭自证前提）**没接线**的标签计数 = 0");

            // ---------------- A1 两格原样记下（接线读口） ----------------
            Section("A1 两格原样记下（接线读口 OBSERVABLE）");
            Label.ResetSmallScreenSetCountForTest();
            lb.SetSmallScreenFontSizes(45f, 10f);
            Near(lb.SmallScreenMaxRequestedPx, 45f, "A1a `SmallScreenMaxRequestedPx` = 传进去的 45");
            Near(lb.SmallScreenMinRequestedPx, 10f, "A1b `SmallScreenMinRequestedPx` = 传进去的 10");
            Check(lb.SmallScreenSetCountForTest == 1, "A1c ★ 这一颗**只被接了一次**（`SmallScreenSetCountForTest == 1`）");
            Check(Label.SmallScreenSetCount == 1, "A1d 进程内总计数 = 1（配 `ResetSmallScreenSetCountForTest` 给宿主用）");

            // ---------------- A2 ★ `smallScreenUI=false` ⇒ 字号逐位不变 ----------------
            Section("A2 ★ `smallScreenUI=false` ⇒ 字号三格 + 框逐位不变（本波「桌面逐像素不变」的承诺）");
            Check(Mathf.Approximately(lb.FontSize, fs0),
                  "A2a ★ **`fontSize` 不变**（实得 " + lb.FontSize.ToString("F4") + " / 调前 " + fs0.ToString("F4") + "）");
            Check(Mathf.Approximately(lb.FontSizeMin, fsMin0), "A2b ★ `fontSizeMin` 不变");
            Check(Mathf.Approximately(lb.FontSizeMax, fsMax0), "A2c ★ `fontSizeMax` 不变");
            var box1 = (lb.transform as RectTransform)?.sizeDelta ?? Vector2.zero;   // 🔴 2026-11-05（`B148`）：同 `:93`，硬转会在非 RectTransform 时抛 InvalidCast 打断整个 ShellScene.RunCore。
            Check(Mathf.Approximately(box1.x, box0.x) && Mathf.Approximately(box1.y, box0.y),
                  "A2d ★ 框（`sizeDelta`）不变 —— 原版只改字号、⛔ 不动行盒");
            Check(!lb.SmallScreenSizeAppliedForTest,
                  "A2e `SmallScreenSizeAppliedForTest == false`（编辑器批处理 / 小屏关 ⇒ 没落下去，不是「落了但没变」）");
            if (!Application.isPlaying)
                MarkUnverified("A2 的「播放态 + 小屏开 ⇒ 真的改 `fontSizeMax`」那一半（批处理 `isPlaying == false`；"
                             + "真机验法 = 设置→图形页开 Small Screen UI 后进壳看那几颗字号）");

            // ---------------- A3 幂等 ----------------
            Section("A3 幂等");
            lb.SetSmallScreenFontSizes(45f, 10f);
            Check(lb.SmallScreenSetCountForTest == 2 && Label.SmallScreenSetCount == 2,
                  "A3a 再调一次 ⇒ 计数 +1（调用被记下）");
            Near(lb.SmallScreenMaxRequestedPx, 45f, "A3b ★ 两格仍是 45/10（同值重调不改结果）");
            Check(Mathf.Approximately(lb.FontSize, fs0) && Mathf.Approximately(lb.FontSizeMax, fsMax0),
                  "A3c ★ 同值重调 ⇒ 字号三格仍逐位不变（幂等）");

            // ---------------- A4 两格都为 0 ⇒ 整支不做 ----------------
            Section("A4 两格都为 0 ⇒ 什么都不做");
            lb.SetSmallScreenFontSizes(0f, 0f);
            Near(lb.SmallScreenMaxRequestedPx, 0f, "A4a 记下 0/0（= 原版「两格都没配」那一档）");
            Check(Mathf.Approximately(lb.FontSizeMax, fsMax0), "A4b 字号照旧不动");

            // ---------------- A5 两颗标签互不串值 ----------------
            Section("A5 两颗标签互不串值（站点各记各的）");
            var root2 = new GameObject("B142E74 root2");
            var lb2 = Label.Create(root2.transform, "110", Vector3.zero, 5, Color.white,
                                   new Vector2(0.5f, 0.5f), "b142e74 probe2");
            if (lb2 != null && lb2.CanRenderChinese)
            {
                lb2.SetSmallScreenFontSizes(110f, 0f);
                Near(lb2.SmallScreenMaxRequestedPx, 110f, "A5a 第二颗记下 110/0");
                Near(lb.SmallScreenMinRequestedPx, 0f, "A5b 第二颗的 min = 0");
                Near(lb.SmallScreenMaxRequestedPx, 0f, "A5c ★ 第一颗**没被串成** 110（读口是逐实例的）");
                Near(lb.SmallScreenMinRequestedPx, 0f, "A5d 第一颗仍是 0/0");
            }
            else MarkUnverified("A5（第二颗夹具没建出 TMP 后端）");
            if (root2 != null) Object.DestroyImmediate(root2);
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            SmallScreenUI.Set(wasEnabled);
            SmallScreenUI.PersistOverride = wasPersist;
            if (!wasEnabled) SmallScreenUI.ResetForTest();
        }
        // 🆕 2026-11-05 `B143-W8`（调度台裁定 ③）：`未验` 单独印出来 —— 它**不计入通过**，但也⛔ 不许从合计里消失（宿主只加 Pass/Fail）。
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail + "（未验 " + _unverified + " 条，不计入通过）");
    }

    // ==================================================================
    //  B 组 —— 站点（宿主建好窗之后逐颗查）
    // ==================================================================
    /// <summary>逐颗查：这一颗标签**接过线**、两格对得上 TSV、且**只接了一次**。
    /// <para>🧨 灭自证：把某个建点上那一行 `SetSmallScreenFontSizes` 删掉 ⇒ 两格回 0、计数回 0 ⇒ 本条红。</para></summary>
    public static void CheckLabel(Label lb, float maxPx, float minPx, string what)
    {
        Check(lb != null, "（前提）`" + what + "` 的标签在（节点/树没建出来 ⇒ 本条红）");
        if (lb == null) return;
        Near(lb.SmallScreenMaxRequestedPx, maxPx, "★ `" + what + "` 的 smallScreenMax = " + maxPx.ToString("F2"));
        Near(lb.SmallScreenMinRequestedPx, minPx, "★ `" + what + "` 的 smallScreenMin = " + minPx.ToString("F2"));
        Check(lb.SmallScreenSetCountForTest == 1,
              "★ `" + what + "` **只被接了一次**（实得 " + lb.SmallScreenSetCountForTest + " 次）");
    }

    /// <summary>按相对路径取一颗标签并走 <see cref="CheckLabel"/>。
    /// <para>`path` 相对 `root`（例：`"Ranking Tab/Tab Content/Tab  Area/…"` 那一族太长 ⇒ 宿主自己给
    /// 它手上最靠近那段子树的 `Transform`，路径写短的那一段即可）。</para></summary>
    public static void CheckStation(Transform root, string path, float maxPx, float minPx, string what)
    {
        Transform t = root != null && !string.IsNullOrEmpty(path) ? root.Find(path) : null;
        Label lb = t != null ? t.GetComponent<Label>() : null;
        if (lb == null && t != null) lb = t.GetComponentInChildren<Label>(true);
        if (t == null)
        {
            Check(false, "（前提）`" + what + "`：`" + path + "` 在树上找不到（宿主给的根 / 路径不对）");
            return;
        }
        CheckLabel(lb, maxPx, minPx, what);
    }

    /// <summary>把这一棵树里**所有接了线**的标签逐颗印出来（`路径 → (max,min)` × 次数）。
    /// ⚠️ 只印、**不断言**：人工对 `报告/B142_E74.md` §三那张表用。</summary>
    public static void Dump(Transform root, string tag)
    {
        if (root == null) { Debug.LogWarning(Tag + " Dump：root 为 null（" + tag + "）"); return; }
        var all = root.GetComponentsInChildren<Label>(true);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var lb = all[i];
            if (lb == null) continue;
            if (!(lb.SmallScreenMaxRequestedPx > 0f) && !(lb.SmallScreenMinRequestedPx > 0f)) continue;
            n++;
            Debug.Log(Tag + " dump " + tag + " ▸ " + PathOf(root, lb.transform)
                    + " → (" + lb.SmallScreenMaxRequestedPx.ToString("F2") + ", "
                    + lb.SmallScreenMinRequestedPx.ToString("F2") + ") × " + lb.SmallScreenSetCountForTest);
        }
        Debug.Log(Tag + " dump " + tag + "：接了线的标签共 **" + n + "** 颗（总标签 " + all.Length + " 颗）");
    }

    static string PathOf(Transform root, Transform t)
    {
        var s = t.name;
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }

    /// <summary>宿主用的**前提**断言（2026-11-05 `B143-W8` · 调度台裁定 ①）：条件不成立 ⇒ 显式红，⛔ 不用 `if (前提) Check(结论)` 静默跳过。计数与 <see cref="Check"/> 同源（进 Pass/Fail）。</summary>
    public static void Premise(bool ok, string msg) { Check(ok, msg); }

    /// <summary>E74 站点「建节点 + 藏」那一档：`root.Find(path)` 必须**在**、且 `activeSelf == false`
    /// （原版 `SetActive(false)` 那一支；先例 `LeaderboardWindow.cs:1007` / `SkirmishEventWindow__TryOpen`）。</summary>
    public static void CheckHidden(Transform root, string path, string what)
    {
        Transform t = root != null && !string.IsNullOrEmpty(path) ? root.Find(path) : null;
        Check(t != null, "（前提）`" + what + "`：`" + path + "` **应该在树上**（无值只是藏、⛔ 不是不建）—— 找不到 ⇒ 红");
        if (t == null) return;
        Check(!t.gameObject.activeSelf,
              "★ `" + what + "`：无值/无活动 ⇒ 节点在、`activeSelf == false`（原版 `SetActive(false)`）｜实得 activeSelf=" + t.gameObject.activeSelf);
    }

    /// <summary>「有值」半的**同组件**探针：`TimerDisplay.Attach(Now+3h)` ⇒ `activeSelf == true` 且文本真的被写出来。
    /// <para>⚠️ Skirmish 窗级「有值」本机不可达（`ClassicTimerDeadline` 是恒 null 的只读口；`TimerDeadline` 被 `ActiveEventDeadline()` 覆写），
    /// 所以这一半钉在**同一个 `TimerDisplay` 组件**上（判据同 `B99A4Assert` ①d）。</para></summary>
    public static void CheckTimerPositiveControl(string what)
    {
        var go = new GameObject("B142E74_timer_probe");
        try
        {
            var lb = Label.Create(go.transform, "BUILD", Vector3.zero, 5, Color.white, new Vector2(0.5f, 0.5f), "b142e74_timer");
            Check(lb != null, "（前提）`" + what + "`：计时探针 Label 建出来了");
            if (lb == null) return;
            TimerDisplay.Attach(lb, () => System.DateTime.Now.AddHours(3), "P: ", what, null);
            Check(lb.gameObject.activeSelf, "★ `" + what + "`：**有值 ⇒ 节点 active**（`TimerDisplay` 的 `remain > 0` 支 `SetActive(true)`）");
            string txt = lb.Text ?? "";
            Check(txt.StartsWith("P: ", System.StringComparison.Ordinal) && txt.IndexOf("BUILD", System.StringComparison.Ordinal) < 0
                  && System.Text.RegularExpressions.Regex.IsMatch(txt, "[0-9]"),
                  "★ `" + what + "`：**有值 ⇒ 文本真的被写出来**（描述串 + 带数字的时间串，⛔ 不是出厂占位）｜实得「" + txt + "」");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
