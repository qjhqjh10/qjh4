// EventWindowDetailAssertE19.cs — 模式窗（经典 / 小冲突）五处细节的断言（执行代理 E19，2026-10-24）
//
// ============================ 它盯的是什么（判据出处）============================
// 用户拿原版实拍（`C:/Users/qjh36/Desktop/warpforge参考图/经典模式参考图.png`）逐点比我们的「经典 / 小冲突」窗，
// 指出五处不符。只读判据代理 S4 全部查实 —— **全文 → `资料/普查产出_1024b/S4_模式窗细节与徽章.md`**：
//   ① 段位徽章：`DivisionImage` 539.79×572.62 @ (49.10,234.04)，图 = `01-Rook…07-Legend`（512²）+ `Roman I–VI`（128²）
//   ② 左上角圆标：原版 `Window Title` 挂 `ContentSizeFitter m_HorizontalFit=2` ⇒ 标题盒宽 = **文本实宽**，
//      圆标左沿 = 155 + 实宽 + spacing 5.5（参考图实测 ≈299；我们原来写死 369.36 ⇒ 529.86）
//   ③ 中央徽记：原版中心 y 394.8，参考图同位置（督军位）中心 y **454.0** ⇒ 空态整体下移 59.2
//   ④ 战斗钮那两颗图标：原版**默认全灭**、按事件数据 flags `SetActive`
//      （`d:/2/tools/decomp_full/RankedEventWindowV2__ArmySelectedOnSelectArmy.c:69-100`）
//   ⑤ 能量条：每格 **10 格**、**9 根 `Separator`**（`max − 1`），填充按 `useWholeNumbers:1`
//
// ============================ 怎么用（宿主）============================
// `EventWindowDetailAssertE19.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；只自建两台临时窗口（跑完 `DestroySafe`）。
//   · 存档指向临时文件（`DeckStore.OverridePath` → `_tmp_view/e19/`），**不碰玩家真卡组**，跑完复原。
//   · 宿主建议挂 `Editor/MainMenuScene.cs` 或 `ShellScene`（那两处本来就有同族窗口的现场）。
//
// 🔴 **灭自证（每组的改坏法都写在文案里）**：
//   ④ 把 `ApplyBattleBoostFlags` 里那两句 `SetActive` 删掉 ⇒ C1/C2 红；把出厂态改成「常亮」⇒ C1/C2 红；
//   ② 把 `iconL` 改回 `titleL + 369.36f + 5.5` ⇒ B1 红（差 239 px）；
//   ③ 把 `FacIconDropY` 改回 0（或顺手把 `Warlod Image` 也挪 59.2）⇒ D1 / D2 交替红；
//   ⑤ 把 9 根缝改回 1 根 ⇒ E1 红；把 10 格删成 1 条连续 Fill ⇒ E2 红；
//   ① 把段位图那一路删掉（`DivisionImage` 不出图）⇒ A1 红；换错段位号 ⇒ A2 红。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 E19：模式窗五处细节（②圆标 · ③徽记落点 · ④两颗图标 · ⑤能量条 · ①段位徽章）。
/// <para>计数口径照 `CheckSink`（`Editor/MenuCheck.cs`，⛔ 不用全局 static）。</para></summary>
public static class EventWindowDetailAssertE19
{
    static readonly CheckSink _sink = new CheckSink("[E19] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Check<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void CheckTrue(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void CheckNear(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ------------------------------------------------------------ 小工具
    static Transform Find(Transform root, string name) { return MenuCheck.FindChild(root, name); }

    /// <summary>只看直接子件（同名件在别处还有 —— `Seg` / `Bg` 这种名字尤其）。</summary>
    static Transform DirectChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c != null && c.name == name) return c;
        }
        return null;
    }

    static ImageQuad QuadOf(Transform t)
    {
        if (t == null) return null;
        var qs = t.GetComponentsInChildren<ImageQuad>(true);   // **含关着的**（默认灭的两颗图标也要读得到材质）
        return qs.Length > 0 ? qs[0] : null;
    }

    static string TextOf(Transform t)
    {
        if (t == null) return null;
        var lb = t.GetComponent<Label>();
        if (lb == null) lb = t.GetComponentInChildren<Label>(true);
        return lb != null ? lb.Text : null;
    }

    static int CountNamed(Transform root, string name)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    /// <summary>节点渲出来的设计帧矩形（画布 px，左上原点）。量不到 ⇒ false（⛔ 不拿 0 当真值）。</summary>
    static bool RectOf(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        return MenuCheck.NodeRectPx(t, MenuCheck.RectCenterMode.Node, out x1, out y1, out x2, out y2);
    }

    static bool ColorNear(Color got, Color want, float tol)
    {
        return Mathf.Abs(got.r - want.r) <= tol && Mathf.Abs(got.g - want.g) <= tol
            && Mathf.Abs(got.b - want.b) <= tol && Mathf.Abs(got.a - want.a) <= tol;
    }

    static string CStr(Color c) { return "(" + c.r.ToString("F3") + ", " + c.g.ToString("F3") + ", " + c.b.ToString("F3") + ", " + c.a.ToString("F3") + ")"; }

    // 判据字面量（⛔ 不读实现常量 —— 那会变成「拿我们的实现证明我们的实现」）
    /// <summary>`Header Background` 的 HLG：padLeft **155** / spacing **5.5**（S4 §② 的 HLG 实读）。</summary>
    const float HdrPadL = 155f, HdrGap = 5.5f;
    /// <summary>参考图实测圆标图形左沿 ≈ **299**（S4 §②：prefab 算 290.5、参考图 299 —— 差在「图形边缘 vs 节点边缘」）。</summary>
    const float IconLeftRefMax = 350f;      // 写死 369.36 那一版会给出 529.86 ⇒ 这条与上一条一起必红
    /// <summary>参考图 `Warlod Image` 中心 y = **454.0**（S4 §③；空态徽记要对齐到它）。</summary>
    const float CenterTargetY = 454.0f;

    // ============================================================ 入口
    public static void Run(WindowsManager mgr = null)
    {
        MenuCheck.Section(_sink, "E19 模式窗细节 —— 圆标贴紧模式名 / 徽记落点 / 两颗图标 / 能量条 / 段位徽章");

        string keepPath = DeckStore.OverridePath;
        string tmp = "d:/4/_tmp_view/e19/_e19_decks.json";
        var keepLast = LiveOpsEventWindow.LastOpened;
        SkirmishEventWindow sk = null;      // 空态那台（③ 要 `Faction Icon Image` 真的显示着）
        RankedEventWindow rk = null;        // 有卡组那台（②④⑤① 不依赖空态）
        try
        {
            System.IO.Directory.CreateDirectory("d:/4/_tmp_view/e19");
            DeckStore.OverridePath = tmp;
            try { System.IO.File.Delete(tmp); } catch { }
            CollectionData.ResetForTest();

            // ---------------------------------------------------------- 夹具：一套**经典**卡组
            string umHero = null;
            foreach (var c in CardDatabase.Load())
            {
                if (c.Type != "hero") continue;
                if (DeckRules.SameFaction(c.Faction, "Ultramarines")) { umHero = c.Id; break; }
            }
            CheckTrue(umHero != null, "（夹具）卡池里有 Ultramarines 督军（下面两台窗要靠它才有卡组 / 才有阵营）");
            if (umHero == null) return;
            var lib = DeckLibrary.Load();
            var dClassic = lib.Create("E19 经典-Ultramarines", (int)GameMode.Classic);
            dClassic.WarlordId = umHero;
            lib.Save();
            CollectionData.ResetForTest();
            CheckTrue(CollectionData.Select(0), "（夹具）当前选中 = 第 0 套（经典 / Ultramarines）");

            // ========================================================== 窗口
            sk = SkirmishEventWindow.Create(mgr);
            if (mgr != null) mgr.OpenWindow(sk); else sk.Open();     // ⚠️ 不调 base ⇒ 状态要由管理器置 Open
            // 🔴 **2026-10-24（调度台 · 首跑红后订正）**：`WindowsManager` 同一时刻只让一扇全屏窗开着 ——
            //   先开 `rk` 会把 `sk` 关掉 ⇒ 两条前提**不能都放在两次 Open 之后**。改成「开一扇、当场断一扇」。
            CheckTrue(sk != null && sk.CurrentState == WindowState.Open, "（前提）小冲突窗开起来了");
            rk = RankedEventWindow.Create(mgr);
            if (mgr != null) mgr.OpenWindow(rk); else rk.Open();

            CheckTrue(rk != null && rk.CurrentState == WindowState.Open, "（前提）排位（经典式）窗开起来了");
            CheckTrue(rk.ArmyCells.Count > 0, "（前提）排位窗画出了阵营格（" + rk.ArmyCells.Count + " 格）");

            // ========================================================== A · ① 段位徽章
            MenuCheck.Section(_sink, "E19 · A 段位徽章（DivisionImage）");
            {
                var divN = Find(rk.transform, "DivisionImage");
                CheckTrue(divN != null, "★ A0 段位大图节点 `DivisionImage` 在（原版 `Ranked Division Info/Content` 下那颗）");
                var dq = QuadOf(divN);
                CheckTrue(dq != null && dq.Texture != null,
                          "★ A1 `DivisionImage` **真的有图**（sprite 为空 ⇒ 红；改坏法 = 把那句 `MenuDraw.Rect` 删掉）");
                Check(dq != null && dq.Texture != null ? dq.Texture.name : null, "07-Legend",
                      "★ A2 演示段位 7 ⇒ **`07-Legend`**（与参考图同款；「号→图名」= 按图名前缀推断，"
                    + "见 `RankedDivisionInfo.DivisionArt` 的 doc —— 映射表 `RankedDivisions` 本地只有脚本没有实例）");
                float dx1, dy1, dx2, dy2;
                bool okR = RectOf(divN, out dx1, out dy1, out dx2, out dy2);
                CheckTrue(okR, "（前提）`DivisionImage` 的矩形量得到（量不到 ⇒ 下一条等于没查）");
                if (okR)
                    CheckNear(dx2 - dx1, 539.79f, 1.5f,
                              "★ A3 段位图框宽 = **539.79**（原版 `DivisionImage` 49.10→588.90；"
                            + "图是 512² + `m_PreserveAspect=1` ⇒ 实际画出来是 539.79 方形、居中）");
                // 段位名 = 官方词条（⛔ 不从实现常量取期望值）
                string wantName = Loc.T("Ranked/Division/" + 7);
                string gotName = TextOf(Find(rk.transform, "DivisionText"));
                CheckTrue(!string.IsNullOrEmpty(wantName) && wantName != "Ranked/Division/7"
                          && gotName == wantName,
                          "★ A4 `DivisionText` = 官方词条 `Ranked/Division/7`（zh `传奇` / en `Legendary`）"
                        + "；实得「" + (gotName ?? "<无>") + "」"
                        + "｜🧨 编一个中文名 / 留空 ⇒ 红");
                CheckTrue(Find(divN, "RankImage") != null,
                          "★ A5 名次图 `RankImage` 节点**照建**（原版有这一颗）—— 但**不出图**："
                        + "名次号也在服务器，且参考图里罗马数字压在骷髅脸上、「Legend 叠不叠名次」判不了");
                // 层序：徽记必须压在 banner / footer **之下**（原版兄弟序 `RankTitleBG → DivisionImage → footer`，
                // uGUI 后建的画在上面；参考图里「1600」压在徽记下缘上）。同档时谁盖谁不可控 ⇒ 必须**严格**小。
                var bannerQ = QuadOf(Find(rk.transform, "RankTitleBG"));
                int qBadge = dq != null ? dq.RenderQueue : -1;
                int qBanner = bannerQ != null ? bannerQ.RenderQueue : -1;
                CheckTrue(qBadge > 0 && qBanner > 0 && qBadge < qBanner,
                          "★ A6 段位徽记的队列 **严格小于** `RankTitleBG` 的队列（" + qBadge + " < " + qBanner + "）"
                        + " —— 徽记在下、banner/footer 在上"
                        + "｜🧨 把徽记改成 `qArt`（同档）⇒ 红（同档 = 谁盖谁不可控，会把「传奇」或「1600」盖掉）");
            }

            // ========================================================== B · ② 左上角圆标贴紧模式名
            MenuCheck.Section(_sink, "E19 · B 左上角圆标（标题实宽决定落点）");
            {
                var tl = Find(rk.transform, "Window Title");
                var tlb = tl != null ? tl.GetComponent<Label>() : null;
                CheckTrue(tlb != null, "（前提）`Window Title` 底下量得到 `Label`");
                float titleWpx = tlb != null ? tlb.WorldW * 108f : 0f;    // `WorldW` 是 Unity 单位 ⇒ ×108 回设计 px
                CheckTrue(titleWpx > 0.1f, "（前提）标题实宽量出来了 = " + titleWpx.ToString("F2") + " px");
                var icon = Find(rk.transform, "Game Mode Icon");
                float ix1, iy1, ix2, iy2;
                bool okI = RectOf(icon, out ix1, out iy1, out ix2, out iy2);
                CheckTrue(okI, "（前提）`Game Mode Icon` 量得到（这一窗接的是 `40k_gamemode_icon_classic`）");
                if (okI && titleWpx > 0.1f)
                {
                    // 量的是**节点心**：图 256² 在 100² 框里 `keepAspect` ⇒ 实画 100²，左右各差 50；
                    // 用「心」比就不用管 `keepAspect` 那一档会不会把框内缩（判据仍是「心 = 框左 + 50」）。
                    float cx = (ix1 + ix2) * 0.5f;
                    float want = HdrPadL + titleWpx + HdrGap + 50f;
                    CheckNear(cx, want, 3f,
                              "★ B1 圆标**框心** = **155 + 标题实宽 + 5.5 + 50** = " + want.ToString("F2")
                            + " px（实得 " + cx.ToString("F2") + "；框左 = " + (cx - 50f).ToString("F2") + "）"
                            + "｜🧨 把 `iconL` 改回 `titleL + 369.36 + 5.5`（= 529.86）⇒ 差 239 px、红");
                    CheckTrue((cx - 50f) < IconLeftRefMax,
                              "★ B2 圆标框左 < " + IconLeftRefMax.ToString("F0") + " px（参考图实测 ≈ 299）"
                            + "｜🧨 这条堵另一种改坏法：把标题宽写死成大值 ⇒ 左沿立刻 ≥ 350、红"
                            + "（实得 " + (cx - 50f).ToString("F2") + "）");
                }
                var gq = QuadOf(icon);
                Check(gq != null && gq.Texture != null ? gq.Texture.name : null, "40k_gamemode_icon_classic",
                      "★ B3 圆标图仍是 `40k_gamemode_icon_classic`（排位窗走经典模式；⛔ 别为了挪位置换了图）");
            }

            // ========================================================== C · ④ 战斗钮那两颗图标
            MenuCheck.Section(_sink, "E19 · C 战斗钮两颗图标（默认全灭 + 按 flags 开关 + 染色）");
            {
                // 先断**出厂态**（这一窗没灌任何数据 ⇒ flags = 0）
                Check(rk.CurrentBattleBoostFlags, 0,
                      "★ C0 本机拿不到事件数据 ⇒ 出厂 flags = **0**（`IRankedEvent` 那一跳的数据在服务器）");
                var troph = Find(rk.transform, "TrophyIcon");
                var shield = Find(rk.transform, "ShieldIcon");
                CheckTrue(troph != null && shield != null,
                          "★ C1 两颗节点**都在**（原版确有这两颗 —— `ButtonIcons` 下 100² × 2、spacing −28.31）");
                CheckTrue(troph != null && !troph.gameObject.activeSelf,
                          "★ C2 出厂 ⇒ `TrophyIcon` **关着**（原版 `SetActive(flags & 1)`；参考图那一带逐像素无亮块）"
                        + "｜🧨 删掉 `ApplyBattleBoostFlags` 里那句 `SetActive`（= 常亮）⇒ 红");
                CheckTrue(shield != null && !shield.gameObject.activeSelf,
                          "★ C3 出厂 ⇒ `ShieldIcon` **关着**（原版 `SetActive((flags & 2) != 0)`）");
                var battleBg = DirectChild(Find(rk.transform, "To Battle Button"), "Bg");
                var bgq = QuadOf(battleBg);
                CheckTrue(bgq != null, "（前提）战斗钮底图 `To Battle Button/Bg` 量得到（染色要读它的材质 color）");

                // flags = 1 ⇒ 只有奖杯 + 黄底
                rk.ApplyBattleBoostFlags(1);
                CheckTrue(troph.gameObject.activeSelf && !shield.gameObject.activeSelf,
                          "★ C4 flags=**1** ⇒ 只有 `TrophyIcon` 亮（bit0 = 奖杯）");
                CheckTrue(bgq != null && ColorNear(bgq.Tint, new Color(1f, 0.92156863f, 0.015686275f, 1f), 0.01f),
                          "★ C5 …且战斗钮底涂 **`boostedColor`** = (1, 0.9216, 0.0157, 1)（黄；prefab 字段现读）"
                        + "，实得 " + (bgq != null ? CStr(bgq.Tint) : "<无>"));
                // flags = 2 ⇒ 只有盾 + 青底
                rk.ApplyBattleBoostFlags(2);
                CheckTrue(!troph.gameObject.activeSelf && shield.gameObject.activeSelf,
                          "★ C6 flags=**2** ⇒ 只有 `ShieldIcon` 亮（bit1 = 盾）");
                CheckTrue(bgq != null && ColorNear(bgq.Tint, new Color(0f, 1f, 1f, 1f), 0.01f),
                          "★ C7 …且底涂 **`shieldedColor`** = (0, 1, 1, 1)（青），实得 "
                        + (bgq != null ? CStr(bgq.Tint) : "<无>"));
                // flags = 3 ⇒ 两颗都亮、盾色优先
                rk.ApplyBattleBoostFlags(3);
                CheckTrue(troph.gameObject.activeSelf && shield.gameObject.activeSelf,
                          "★ C8 flags=**3** ⇒ 两颗都亮");
                CheckTrue(bgq != null && ColorNear(bgq.Tint, new Color(0f, 1f, 1f, 1f), 0.01f),
                          "★ C9 …两位都置时**盾色优先**（反编译那一段：先看 bit1 取 `shieldedColor`，否则才 `boostedColor`）");
                // 复位
                rk.ApplyBattleBoostFlags(0);
                CheckTrue(!troph.gameObject.activeSelf && !shield.gameObject.activeSelf
                          && bgq != null && ColorNear(bgq.Tint, Color.white, 0.01f),
                          "★ C10 flags=0 复位 ⇒ 两颗都灭、底色回到原样（白）");
            }

            // ========================================================== D · ③ 中央徽记落点
            MenuCheck.Section(_sink, "E19 · D 中央徽记（空态整体下移 59.2 ⇒ 中心 454.0）");
            {
                var fac = Find(sk.transform, "Faction Icon Image");
                CheckTrue(fac != null && fac.gameObject.activeSelf,
                          "（前提）小冲突窗当前只有**经典**卡组 ⇒ 本窗空态 ⇒ `Faction Icon Image` 显示着"
                        + "（原版 `ChangeDeck.c:80-83` 的 `SetActive(deck == null)`）");
                float fx1, fy1, fx2, fy2;
                bool okF = RectOf(fac, out fx1, out fy1, out fx2, out fy2);
                CheckTrue(okF, "（前提）`Faction Icon Image` 的矩形量得到");
                if (okF)
                {
                    float cy = (fy1 + fy2) * 0.5f;
                    CheckNear(cy, CenterTargetY, 2f,
                              "★ D1 **空态徽记中心 y = 454.0 ± 2**（原版 394.8 + 用户口径 59.2 = 参考图督军位中心）"
                            + "；实得 " + cy.ToString("F2")
                            + "｜🧨 把 `FacIconDropY` 改回 0 ⇒ 394.8、红");
                    CheckNear(fy2 - fy1, 522.63f, 1.5f,
                              "★ D1b 徽记**高仍 522.63**（只挪位置、没缩图）");
                }
                var wl = Find(sk.transform, "Warlod Image");
                // ⚠️ 空态下 `Warlod Image` **底下没有图**（原版 `SetActive(deck != null)`，我们连 quad 都不建）
                //   ⇒ 不能用 `NodeRectPx`（它要子树里有 Label/ImageQuad）—— 节点心就是矩形中心，直接量它。
                CheckTrue(wl != null, "（前提）`Warlod Image` 节点在");
                if (wl != null)
                    CheckNear(MenuCheck.DesignPx(wl).y, CenterTargetY, 2f,
                              "★ D2 **督军立绘没被一起挪**：它的中心本来就在 454.0（原版 450.93→1549.07 的 −95.07→1003.07）"
                            + "｜🧨 把 `FacIconDropY` 顺手加到 `Warlod Image`（或改 `WlT/WlB`）⇒ 红");
            }

            // ========================================================== E · ⑤ 能量条（10 格 / 9 根缝）
            MenuCheck.Section(_sink, "E19 · E 阵营格能量条（10 格轨道 + 9 根 Separator）");
            {
                var cell0 = rk.ArmyCells.Count > 0 ? rk.ArmyCells[0] : null;
                var area = cell0 != null ? Find(cell0, "Fill Area") : null;
                CheckTrue(area != null, "（前提）第 0 格的 `ProgressBar/Fill Area` 在");
                // 9 根缝：名字是 `Separator` / `Separator (1)`…（Unity 的实例递增名）⇒ **按前缀数**，
                // ⛔ 别用 `CountNamed(area,"Separator")`（那是**精确名**，只会数到 1 根 ⇒ 假红）。
                var sepXs = new List<float>();
                if (area != null)
                    for (int i = 0; i < area.childCount; i++)
                    {
                        var ch = area.GetChild(i);
                        if (ch != null && ch.name.StartsWith("Separator"))
                            sepXs.Add(MenuCheck.DesignPx(ch).x);
                    }
                sepXs.Sort();
                Check(sepXs.Count, 9,
                      "★ E1 每格 **9 根** `Separator`（原版 `max − 1`；10 格 ⇒ 9 条边界）"
                    + "｜🧨 只画 prefab 那一根（= 1）⇒ 红");
                // 9 根本身还要**摆对**：它们落在 10 格的边界上（缝心 = 相邻两格心的中点）
                var fillForX = area != null ? DirectChild(area, "Fill") : null;
                float pitch = 0f;
                if (fillForX != null && fillForX.childCount >= 2)
                    pitch = MenuCheck.DesignPx(fillForX.GetChild(1)).x - MenuCheck.DesignPx(fillForX.GetChild(0)).x;
                CheckTrue(sepXs.Count == 9 && pitch > 0.1f,
                          "（前提）9 根缝都量得到、格距也量得到（实得 " + sepXs.Count + " 根 / 格距 "
                        + pitch.ToString("F2") + " px）");
                if (sepXs.Count == 9 && pitch > 0.1f)
                {
                    CheckNear(sepXs[8] - sepXs[0], pitch * 8f, 0.5f,
                              "★ E1b 缝是**均分**的：首末两根间距 = 8 × 格距（" + (pitch * 8f).ToString("F2")
                            + " px；实得 " + (sepXs[8] - sepXs[0]).ToString("F2") + "）"
                            + "｜🧨 9 根全叠在 prefab 那一处（26.18）⇒ 间距 0、红");
                    CheckNear(sepXs[0], MenuCheck.DesignPx(fillForX.GetChild(0)).x + pitch * 0.5f, 0.5f,
                              "★ E1c 第一根缝落在**第一条边界**上（第 0 格心 + 半个格距；⛔ 不是 prefab 模板位 26.18）");
                }
                var fillN = area != null ? DirectChild(area, "Fill") : null;
                CheckTrue(fillN != null, "★ E2 轨道宿主 `Fill` 在（原版树里这一层就叫 `Fill`）");
                CheckTrue(fillN != null && fillN.childCount == 10,
                          "★ E3 轨道 = **10 格**（`Fill` 底下 10 颗 `Cell`；实得 "
                        + (fillN != null ? fillN.childCount : -1) + "）"
                        + "｜🧨 删成「一条连续 Fill」⇒ 0 颗 Cell、红");
                int lit = 0;
                if (fillN != null)
                    for (int i = 0; i < fillN.childCount; i++)
                        if (fillN.GetChild(i).gameObject.activeSelf) lit++;
                Check(lit, 0,
                      "★ E4 本机没有赛季数据 ⇒ 进度 0 ⇒ **一格都不亮**（⛔ 不许编满格）"
                    + "｜🧨 把 `ArmyProgress` 写成 1（满格）⇒ 10、红");
            }

            // ========================================================== F · 空态 / 有卡组两态没被这条改坏
            MenuCheck.Section(_sink, "E19 · F 两窗的图一张都不缺");
            Check(sk.MissingArt.Count, 0, "★ F1 小冲突窗 `MissingArt` = 0（`Tex()` 会记账；取不到的层 MenuDraw 静默不画）");
            Check(rk.MissingArt.Count, 0, "★ F2 排位窗 `MissingArt` = 0（段位图那 7 张资源名要真能取到 —— 取不到会记一条）");
        }
        catch (System.Exception ex)
        {
            // 🔴 不许让异常静默跳过一整节（同「不许静默失败」那条红线）
            CheckTrue(false, "★ E19 断言自身抛异常（这一条算**红**、⛔ 不是跳过）："
                             + ex.GetType().Name + " · " + ex.Message);
        }
        finally
        {
            if (sk != null) RewardsWindow.DestroySafe(sk.gameObject);
            if (rk != null) RewardsWindow.DestroySafe(rk.gameObject);
            LiveOpsEventWindow.LastOpened = keepLast;
            DeckStore.OverridePath = keepPath;
            try { System.IO.File.Delete(tmp); } catch { }
            CollectionData.ResetForTest();
            Debug.Log(_sink.P + "合计：通过 " + _sink.Pass + " / 失败 " + _sink.Fail
                      + (keepPath == null ? "" : "（存档指向已还原）"));
        }
    }
}
