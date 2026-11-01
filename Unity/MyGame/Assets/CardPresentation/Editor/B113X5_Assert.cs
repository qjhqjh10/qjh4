// B113X5_Assert.cs — 第一百一十三会话 · 第 2 波 · 执行代理 `X5`（2026-11-01）· 账 `菜单_外壳.tsv:43-45`
//
// ============================ 本件挡什么 ============================
//  任务 = 「`BoostTimerPanel` 原版到底怎么用、我们该不该接线」。
//  **结论 = 不接线**（原版有类、但整族在原版 build 里**挂不到任何对象上**；数据源在 PlayFab、关服后恒空）。
//  本件把**这个裁决的两条反证事实**钉成断言，防将来有人「顺手接上线」把它变回第二条给原版没有的入口：
//    ① `S` 组 —— **我们侧零接线**：除定义文件外，全 `Assets/**/*.cs` 里 `BoostTimerPanel.Build(` /
//       `new BoostTimerPanel(` / `BoostStore.` **0 处**（⚠️ 这正是 `AUD08` 点名的「休眠件变雷」之处）。
//    ② `D` 组 —— **数据源恒空且出声**：`BoostStore.Snapshot()` 空表返回 0 条、首次问 `[Boost]` 出声一次；
//       喂进去的条目按原版语义「未到期保留 / 到期摘掉」。
//  另加 `P` 组（前提硬断言）与 `F` 组（面板本体仍逐句照原版 —— 「不接线」≠「逻辑可以不验」）。
//
// ============================ 判据（2026-11-01 现读，⛔ 不是从旧文档抄的）============================
//  · 驱动链（原版）：`BoostMenu.ShowAllBoosters` 里 `BoostManager.OnTimerUpdated += UpdateAllPanelsTimers`
//      （`d:/2/tools/decomp_full/BoostMenu__ShowAllBoosters.c:46-69`；ILSpy `BoostMenu.cs:86-96`），
//      `UpdateAllPanelsTimers` 逐个 `BoostTimerPanel.UpdateTimer()`、返 true 的 `_boostPanels.Remove` + `Destroy`
//      （`decomp_full/BoostMenu__UpdateAllPanelsTimers.c:30-98`）；**不是每帧** —— 挂在那个 static 事件上。
//  · 但那根事件**永不触发**：`BoostManager.UpdateTimer`（private，`CancelInvoke("UpdateTimer")` 说明本应有人在
//      `InvokeRepeating`）**全树 0 调用点**、`BoostManager` 也**没有 `Awake/Start`**（`il2cpp_out/dump.cs`
//      的 `BoostManager` 成员表逐行核过）；`SetupDefaultTimer` 同样 0 调用点。
//  · 格式三档 = `decomp_full/BoostTimerPanel__UpdateTimer.c`（本件 `F` 组逐档钉死）。
//  · 宿主不可达（pathID 级，⛔ 不是按名字搜）：`BoostMenu` / `BoostTimerPanel` / `BoostManager` 的 MonoScript
//      pathID = **2818 / 4135 / 3930**（UnityPy 读 `WarpforgePlayer_Data/globalgamemanagers.assets` 的 4,453 个
//      MonoScript）⇒ `d:/2/新解包资源/assets_full` 全库 **0 个 `m_Script` 指向它们**（控制实验：`BoosterInfoPopup`
//      = `-3149289102854863022` 在 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_7872967592023106223.json`
//      命中 1 个 ⇒ 扫法有效）；AssetRipper 1,389 prefab + 17 场景 0 处引用两个脚本的 guid；远端安卓 1.35 的
//      722 个 addressable MonoScript 里三个类全不在。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · `S1-S3` 的期望值是 **0** —— 那是「恒真」高危值 ⇒ 用三道护法：
//      S4 正控（同一台扫描器在定义文件上必须命中 > 0）· S5 合成串（代码行必须数成 1、注释行必须数成 0）
//      · S6 语料非空（文件数 > 0，防「根目录读不到 ⇒ 三条恒 0」）。
//    🧨 谁把 `BoostTimerPanel.Build(...)` 接进任何窗 ⇒ S1 红；把 `BoostStore` 塞进运行时 ⇒ S3 红。
//  · `D2` 用 `Application.logMessageReceived` 抓 `[Boost]` 那句 —— 🧨 把 `Snapshot` 里的 `Debug.Log` 去掉 ⇒ 红。
//  · `F` 组期望值是**字面量**（照 `.c` 手算的，⛔ 不是拿 `Format` 自己算出来再比）：
//    🧨 把 `00:mm:ss` 改成 `mm:ss` ⇒ F1 红；把 `floor` 丢掉 ⇒ F6 红；天位补零 ⇒ F5 红。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（不建窗、不依赖 UI）。建议接在 `Editor/ShellScene.cs` 的聚合行最末尾（`//` **之前**、同一物理行）：
//      B113X5_Assert.Run(); _sink.Pass += B113X5_Assert.Pass; _sink.Fail += B113X5_Assert.Fail;
//  ⚠️ 追加前先看那一行已有的 `//` 在哪（2026-10-30 踩过 6 次「代码落进行末注释」）。
//  判绿红看「[B113X5] === 合计：X 通过 / Y 失败 ===」那一行，⛔ 不看退出码。
//
// ============================ 副作用/恢复 ============================
//  · 只读 `Assets/**/*.cs` 源码文本 + `d:/2` 三份反编译产物（存在性/子串）。
//  · `BoostStore.Active` 会被 `D` 组临时塞条目 ⇒ **`finally` 里 `ResetForTest()`** 复原（含出声标记）。
//  · 不建任何 GameObject、不写盘、不改存档。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>写手代理 `X5`：`BoostTimerPanel` 该不该接线 —— 本件钉住「不接线」的两条反证事实 + 面板本体照原版。</summary>
public static class B113X5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B113X5] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // ============================================================ 常量

    /// <summary>定义文件（相对 `Assets/`）—— `S` 组要**排除**它（它本来就该有 `new BoostTimerPanel()`）。</summary>
    const string DefRel = "CardPresentation/Shell/BoostTimerPanel.cs";
    /// <summary>本件自身（相对 `Assets/`）—— 排除，否则 `S1-S3` 会被自己的字符串字面量数中（自证）。</summary>
    const string SelfRel = "CardPresentation/Editor/B113X5_Assert.cs";

    /// <summary>原版产物目录（只读）。</summary>
    const string DecompDir = "d:/2/tools/decomp_full";

    // ============================================================ 扫法（S 组与它的自证共用）
    /// <summary>数「**非注释行**」里 <paramref name="needle"/> 的出现次数。
    /// <para>注释行判据 = 去掉前导空白后以 `//` / `///` / `/*` / `*` 开头（行尾注释不剥离 —— 本仓那种极少，
    /// 且真接上了写法也不会藏在行尾注释里）。<paramref name="codeLines"/> 回传实际参与统计的行数（喂合成串用）。</para></summary>
    public static int CountCodeHits(string text, string needle, out int codeLines)
    {
        codeLines = 0;
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0;
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            // 空行不是「代码行」（否则合成串末尾的 `\n` 会多算一行 —— 2026-11-01 实测）
            if (t.Length == 0) continue;
            if (t.StartsWith("//", StringComparison.Ordinal)
             || t.StartsWith("*", StringComparison.Ordinal)
             || t.StartsWith("/*", StringComparison.Ordinal)) continue;
            codeLines++;
            int i = 0;
            while ((i = line.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        }
        return n;
    }

    /// <summary>`Assets/` 绝对路径（`\` → `/`）。</summary>
    static string AssetsRoot()
    {
        string p = Application.dataPath;
        return string.IsNullOrEmpty(p) ? null : p.Replace('\\', '/');
    }

    static bool IsExcluded(string full, string root)
    {
        if (full.Length <= root.Length) return false;
        string rel = full.Substring(root.Length + 1);
        return rel == DefRel || rel == SelfRel;
    }

    // ============================================================ 编排
    public static void Run()
    {
        _sink.Pass = 0;
        _sink.Fail = 0;
        _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B113X5 · BoostTimerPanel 不接线（原版有类、整族 0 实例）+ 面板本体仍照原版");
        P_Premises();
        S_NotWired();
        D_DataSourceEmpty();
        F_Format();

        Debug.Log(_sink.P + $"=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===");
    }

    // ============================================================ P · 前提硬断言（先写前提，再写结论）
    static void P_Premises()
    {
        string root = AssetsRoot();
        Tr(!string.IsNullOrEmpty(root) && Directory.Exists(root), "P1 前提：`Application.dataPath` 可用（" + (root ?? "null") + "）");

        string def = (root ?? "") + "/" + DefRel;
        string defText = (root != null && File.Exists(def)) ? ReadText(def) : null;
        Tr(!string.IsNullOrEmpty(defText), "P2 前提：定义文件 `" + DefRel + "` 存在且非空");

        // 原版成员表（缺一个 ⇒ 我们的等价物就不是复刻了）
        var t = typeof(CardPresentation.BoostTimerPanel);
        Tr(t.GetField("BoostDescText") != null, "P3a 前提：`BoostTimerPanel.BoostDescText` 在（原版 `dump.cs:76743`）");
        Tr(t.GetField("BoostTimerText") != null, "P3b 前提：`BoostTimerPanel.BoostTimerText` 在（原版 `dump.cs:76744`）");
        Tr(t.GetField("Item") != null, "P3c 前提：`BoostTimerPanel.Item` 在（原版 `BoostTimerPanel.BoostItem` `dump.cs:76745`）");
        Tr(t.GetMethod("SetDescription", new[] { typeof(string) }) != null, "P3d 前提：`SetDescription(string)` 在");
        Tr(t.GetMethod("SetTimer", new[] { typeof(string) }) != null, "P3e 前提：`SetTimer(string)` 在");
        Tr(t.GetMethod("Init", new[] { typeof(BoostItem) }) != null, "P3f 前提：`Init(BoostItem)` 在");
        Tr(t.GetMethod("UpdateTimer", Type.EmptyTypes) != null, "P3g 前提：无参 `UpdateTimer()` 在");

        Tr(typeof(BoostStore).GetField("Active") != null, "P4a 前提：`BoostStore.Active` 在（数据侧读口）");
        Tr(typeof(BoostStore).GetMethod("Snapshot", new[] { typeof(DateTime) }) != null, "P4b 前提：`BoostStore.Snapshot(DateTime)` 在");

        // 原版那条驱动链的**产物**在盘上（「原版有这条链」这一半的事实来源）
        string drv = DecompDir + "/BoostMenu__UpdateAllPanelsTimers.c";
        string drvText = File.Exists(drv) ? ReadText(drv) : null;
        Tr(!string.IsNullOrEmpty(drvText), "P5a 前提：原版 `BoostMenu__UpdateAllPanelsTimers.c` 读得到");
        Tr(drvText != null && drvText.IndexOf("BoostTimerPanel__UpdateTimer", StringComparison.Ordinal) >= 0,
           "P5b 前提：那份产物里**确实**在调 `BoostTimerPanel__UpdateTimer`（= 驱动关系，不是猜的）");
        Tr(File.Exists(DecompDir + "/BoostTimerPanel__UpdateTimer.c"), "P5c 前提：原版 `BoostTimerPanel__UpdateTimer.c` 在（F 组的判据源）");
    }

    // ============================================================ S · 反证①：我们侧零接线
    static void S_NotWired()
    {
        string root = AssetsRoot();
        var files = new List<string>();
        if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
        {
            // ⚠️ 枚举失败（权限/坏符号链接）⇒ 空语料，由 S6 报红，⛔ 不许抛异常把宿主整条打断
            try
            {
                foreach (var f in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                    files.Add(f.Replace('\\', '/'));
            }
            catch (Exception ex) { Debug.LogWarning("[B113X5] 枚举 `*.cs` 失败：" + ex.Message); }
        }

        Tr(files.Count > 0, "S6 语料非空：`Assets/**/*.cs` 数出 " + files.Count + " 份（= 0 则 S1-S3 恒真，⛔ 不许放过）");

        int buildHits = 0, newHits = 0, storeHits = 0, excluded = 0;
        var where = new List<string>();
        foreach (var f in files)
        {
            if (IsExcluded(f, root)) { excluded++; continue; }
            string text = ReadText(f);
            if (text == null) continue;
            int cl;
            int a = CountCodeHits(text, "BoostTimerPanel.Build(", out cl);
            int b = CountCodeHits(text, "new BoostTimerPanel(", out cl);
            int c = CountCodeHits(text, "BoostStore.", out cl);
            if (a + b + c > 0)
            {
                where.Add(f.Substring(root.Length + 1) + $"（Build={a} new={b} Store={c}）");
                buildHits += a; newHits += b; storeHits += c;
            }
        }

        string tail = where.Count == 0 ? "" : " —— 命中：" + string.Join(" / ", where.ToArray());
        C(buildHits, 0, "S1 除定义文件外**没有** `BoostTimerPanel.Build(` 调用点（" + excluded + " 份被排除：定义件 + 本件）" + tail);
        C(newHits, 0, "S2 除定义文件外**没有** `new BoostTimerPanel(`（⛔ 别在别处手搓一份）" + tail);
        C(storeHits, 0, "S3 除定义文件外**没有** `BoostStore.` 引用（= 加成数据没有被任何运行时接到 UI 上）" + tail);

        // ---- 正控：同一台扫描器在**真实语料**上必须能命中（否则 S1-S3 恒 0 是假绿）
        string defText = ReadText((root ?? "") + "/" + DefRel);
        int cl2;
        int pos = CountCodeHits(defText ?? "", "BoostTimerPanel", out cl2);
        Tr(pos > 0, "S4 正控：同一台扫描器在定义文件上命中 `BoostTimerPanel` " + pos + " 次（> 0 ⇒ S1-S3 的 0 不是「扫不到」）");

        // ---- 扫描器自证：合成输入（代码行必须数成 1、注释行必须数成 0）
        const string Trick = "// BoostTimerPanel.Build(parent, a, b, 0, 12f, 12f, c);\n"
                           + "/// new BoostTimerPanel()\n"
                           + "   BoostTimerPanel.Build(parent, a, b, 0, 12f, 12f, c);\n";
        int trickCode;
        int hitTrick = CountCodeHits(Trick, "BoostTimerPanel.Build(", out trickCode);
        C(hitTrick, 1, "S5a 扫描器：合成串里 `BoostTimerPanel.Build(` 只数**代码行**那 1 处（注释行不算）");
        C(trickCode, 1, "S5b 扫描器：合成串 3 行里有 2 行是注释 ⇒ 参与统计的行数 = 1");
    }

    // ============================================================ D · 反证②：数据源恒空且出声
    static void D_DataSourceEmpty()
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        try
        {
            BoostStore.ResetForTest();

            var logs = new List<string>();
            Application.LogCallback cb = (cond, stack, type) => logs.Add(cond);
            Application.logMessageReceived += cb;
            int n0;
            try { n0 = BoostStore.Snapshot(now).Count; }
            finally { Application.logMessageReceived -= cb; }

            C(n0, 0, "D1 空表时 `Snapshot` 返回 0 条（加成条目来自 PlayFab，本机无来源 —— 面板没有可显示的东西）");
            bool said = false;
            foreach (var l in logs) if (l != null && l.IndexOf("[Boost]", StringComparison.Ordinal) >= 0) said = true;
            Tr(said, "D2 首次问数据源时**出声一次**（日志含 `[Boost]`；⛔ 不许静默失败）");

            BoostStore.Active.Add(new BoostItem("test-xp", BoostItem.BoostType.XP, 50, 2)
                                  { ExpirationTime = now.AddHours(2) });
            C(BoostStore.Snapshot(now).Count, 1, "D3 喂一条**未到期**（+2h）⇒ 保留 1 条（原版 `CheckBoosterExpirations` 只摘过期的）");

            BoostStore.ResetForTest();
            BoostStore.Active.Add(new BoostItem("test-gold", BoostItem.BoostType.Gold, 25, 1)
                                  { ExpirationTime = now.AddMinutes(-1) });
            C(BoostStore.Snapshot(now).Count, 0, "D4 喂一条**已过期**（-1min）⇒ 被摘掉、剩 0 条");
        }
        finally
        {
            BoostStore.ResetForTest();     // 复原（含出声标记），⛔ 不给后面的宿主留脏状态
        }
    }

    // ============================================================ F · 面板本体仍照原版（三档格式 + 过期语义）
    static void F_Format()
    {
        bool e;

        C(BoostTimerPanel.Format(TimeSpan.FromMinutes(30), out e), "00:30:00", "F1 30 分钟 ⇒ `00:mm:ss`（**短于一小时也带小时位**，不是 `mm:ss`）");
        Tr(!e, "F1b 30 分钟**没有**过期");

        C(BoostTimerPanel.Format(TimeSpan.Zero, out e), "00:00", "F2 恰好 0 ⇒ `00:00`");
        Tr(e, "F2b 恰好 0 ⇒ **expired = true**（原版那一支 `return 1`）");

        C(BoostTimerPanel.Format(TimeSpan.FromMinutes(-5), out e), "00:00", "F3 负 5 分钟 ⇒ 仍是 `00:00`");
        Tr(e, "F3b 负值 ⇒ expired = true");

        C(BoostTimerPanel.Format(new TimeSpan(2, 3, 4), out e), "02:03:04", "F4 2h3m4s ⇒ `hh:mm:ss`（`< 4 天`那支）");
        Tr(!e, "F4b 2h3m4s 没有过期");

        C(BoostTimerPanel.Format(TimeSpan.FromDays(5), out e), "5 d", "F5 5 天 ⇒ `n d`（**天位不补零**）");

        // 3 天 23 小时：`Days = 3 < 4` ⇒ 走 `floor((float)TotalHours)` = 95（**不是** 23）
        C(BoostTimerPanel.Format(new TimeSpan(3, 23, 0, 0), out e), "95:00:00", "F6 3d23h ⇒ `95:00:00`（那支用的是 `TotalHours` 取整，⛔ 不是 `TimeSpan.Hours`）");

        C(BoostTimerPanel.Format(TimeSpan.FromHours(1), out e), "01:00:00", "F7 恰好 1 小时 ⇒ **不走短支**（判据是 `(float)TotalHours < 1f`）");
    }

    // ============================================================ 读文件（容错）
    static string ReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : null; }
        catch (Exception ex) { Debug.LogWarning("[B113X5] 读不到 `" + path + "`：" + ex.Message); return null; }
    }
}
