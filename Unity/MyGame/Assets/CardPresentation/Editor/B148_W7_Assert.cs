// B148_W7_Assert.cs — 执行写手 `B148-W7`（战斗表现 + 外壳 · 2026-11-05）
//
// ============================ 盯的是什么（三件活）============================
//  ① `TargetReticle.UpdateTrail` 的**相机分工**（真缺陷，最高优先）
//     原版 `.c` 逐句：`hudCamera(+0xA0).WorldToScreenPoint(target)` → **`boardCamera(+0xA8).ScreenPointToRay(screen)`**
//     → 与 floorPlane(+0x70) / enemyMinionPlane(+0x90) 各 `Plane.Raycast` 取近者。
//     （判据 = `d:/2/tools/decomp_full/TargetReticleController__UpdateTrail.c:58,70`；字段偏移 =
//      `d:/2/tools/il2cpp_out/dump.cs:30732-30733` = `hudCamera // 0xA0` / `boardCamera // 0xA8`。）
//     我们原来两台混成一台（出射也用 `hc`）⇒ `hc` 正交、射线方向恒 `(0,0,1)`、与地板法线 `(0,1,0)` 垂直
//     ⇒ **地板平面恒不命中**，落点 z 恒 = 敌方行。
//
//  ② `B146-PLAYEVENT-ENTRY`：`PlayEvent<,>.StartMatch` 的**第二跳**（防御卡两钮）接进真实入口
//     （`LiveOpsEventWindow.StartMatch` —— 三扇活动窗的 `Battle!` 走的就是它）。
//     判据 = `_tmp_view/decomp_b91/PlayEvent_object__object_.StartMatch__1127df0.c:43-114`：
//     `:51 HasDefensiveCards(deck,0)`、`:52` **只判 `== HaveItInInventory`**、`:112` 都走 `StartMatchNoCheck`。
//
//  ③ `B143-WINDOW-ORDER-BIAS`：窗口级**队列偏置**（原版 `PopUp Holder` 画布内「后开在上」的等价物）。
//     规则/覆盖面/天花板只写在 `GameWindow.QueueBias` 那一段（铁律 6：同一条规则只留一处）。
//
// ============================ 怎么接（⛔ 本文件不接线）============================
//  `public static void Run()`：**零前提**（自己建相机 / 临时 GameObject；不读存档、不写 PlayerPrefs）。
//  建议宿主（调度台定）：`Editor/BattleScene.cs`（准星那一节）或 `Editor/ShellScene.cs`（开窗那一节）的聚合行
//  **同一物理行**追加：
//      B148_W7_Assert.Run(); _sink.Pass += B148_W7_Assert.Pass; _sink.Fail += B148_W7_Assert.Fail;
//  判绿红：`grep "^B148W7"` 的**合计行**；逐条按行首 `[B148W7] ✓` / `[B148W7] ✗` 数（⛔ 别 `grep -c ✗`）。
//  ⚠️ A-6 / B-1 / B-2 / B-3 / B-5 是**源码层**判据（走 `Application.dataPath` 读 `.cs`）⇒ 只在编辑器宿主里成立。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A-1：把 `ResolveAim` 的出射相机改回 `hc` ⇒ 地板打不中 ⇒ `LastAimOnPlane == false` ⇒ 红。
//  · A-2（**结构性不可能同时满足**）：同一屏幕点，用 `hudCam` 出射打**同一个**地板平面 ⇒ 必须打空
//         （正交 + `forward` 精确 `(0,0,1)` ⇒ 与法线 `(0,1,0)` 点积恰 0）。若哪天有人把两台相机做成同构
//         （例如把 `hudCam` 也改成透视）⇒ A-2 红 ⇒ 提醒「你正在把 A-1 变成一条测不出东西的断言」。
//  · A-6：把出射那一句改回 `hc.ScreenPointToRay` ⇒ 源码里 `boardCam` 不在那一句 ⇒ 红。
//  · B-1：把 `StartMatch` 里那句 `TryDefensiveCardGate()` 删掉 ⇒ 红（= 这一整支又变回「生产上跑不到」）。
//  · B-3：把 B-1 那句挪到 `NetMatchmaking.TryStart` **之后** ⇒ 红（原版顺序 = 防御卡闸在 `StartMatchNoCheck` 之前）。
//  · C-1：把 `RaiseAbove` 改成空实现 ⇒ 红。
//  · C-3：把「抬不动就出声跳过」改成「少抬一截」⇒ 队列被改动 ⇒ 红（⚠️ 这是**故意**要挡的那种静默坏法）。
//  · C-4：把 `RaiseAbove` 改成遍历整个场景（而不是自己这一棵）⇒ A 窗的队列被一起抬高 ⇒ 红。

using System.IO;
using System.Reflection;
using System.Text;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B148-W7` 的验收点（准星相机分工 / 防御卡两钮接生产 / 窗口级队列偏置）。</summary>
public static class B148_W7_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B148W7]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Section(string name) { Debug.Log(Tag + " ── " + name); }

    /// <summary>同 <see cref="Check"/>（沿用本仓断言文件里 `Tr` 的写法，两条名字都留着 —— 读代码的人不用猜）。</summary>
    static void Tr(bool ok, string msg) { Check(ok, msg); }

    // ================================================================================================
    //  A · `TargetReticle` 的相机分工（原版 = hud 投屏 → **board 出射**）
    // ================================================================================================
    static void SectionA_准星出射相机()
    {
        Section("A 准星：投屏 = hudCamera(+0xA0) · 出射 = boardCamera(+0xA8)（`TargetReticleController__UpdateTrail.c:58,70`）");

        var retGo = new GameObject("w7_reticle");
        var hudGo = new GameObject("w7_hudcam");
        var boardGo = new GameObject("w7_boardcam");
        try
        {
            var ret = retGo.AddComponent<TargetReticle>();
            ret.Build();                                   // 编辑模式 `AddComponent` 不跑 `Awake` ⇒ 显式补（同 `B94X2_Assert`）

            // HUD 相机 = **正交**、`forward` **精确** `(0,0,1)`（⛔ 不做任何旋转运算 ⇒ 不留 1e-8 残差）
            var hc = hudGo.AddComponent<Camera>();
            hc.transform.position = new Vector3(0f, 0f, -20f);
            hc.transform.rotation = Quaternion.identity;
            hc.orthographic = true;
            hc.orthographicSize = 5f;

            // 战场相机 = **透视**、从上方略俯视原点（生产同形：`Battle.unity` 的 BoardCamera 是 orthographic = 0）
            var bc = boardGo.AddComponent<Camera>();
            bc.transform.position = new Vector3(0f, 5f, -13.57f);
            bc.transform.LookAt(new Vector3(0f, 0f, 0f));
            bc.fieldOfView = 46.4f;
            bc.orthographic = false;

            ret.hudCam = hc;
            ret.boardCam = bc;
            ret.floorY = 0f;
            ret.enemyRowZ = 100f;                          // 敌兵平面放到很远处 ⇒ 这一段只验「地板能不能打中」

            var pointer = new Vector3(0f, -2f, -10f);      // HUD 相机前方 10 单位、屏幕中线下方 20%
            Tr(pointer.z - hc.transform.position.z > 0f, "A0（前提）指针在 HUD 相机前方（否则 `screen.z < 0` 会被早退）");

            int miss0 = ret.AimMisses;
            var aim = ret.ResolveAim(pointer);

            Tr(ret.LastAimOnPlane, "★ A-1 地板平面**必须能命中**（正交 HUD 相机出的射线与地板法线垂直 ⇒ 恒不命中；"
                                 + "判据 = 出射走 `boardCam`）｜🧨 把出射改回 `hc` ⇒ 本行红");
            Check(Mathf.Abs(aim.y - ret.floorY) < 1e-3f,
                  "A-1b 落点**真的落在地板平面上**（y 应 = " + ret.floorY + "，实测 " + aim.y.ToString("F4") + "）");
            Check(ret.AimMisses == miss0, "A-1c 这一条是成功路径 ⇒ 不计 `AimMisses`（实测 " + ret.AimMisses + "，期望 " + miss0 + "）");

            // ---- 灭自证：同一点、同一条「投屏」中间量，换成 hud 出射 ⇒ 地板**必须**打空
            Vector3 screen = hc.WorldToScreenPoint(pointer);
            var hudRay = hc.ScreenPointToRay(screen);
            var boardRay = bc.ScreenPointToRay(screen);
            var floor = new Plane(Vector3.up, new Vector3(0f, ret.floorY, 0f));
            float dHud, dBoard;
            bool hudHits = floor.Raycast(hudRay, out dHud);
            bool boardHits = floor.Raycast(boardRay, out dBoard);

            Tr(!hudHits, "★ A-2（灭自证）同一屏幕点用 `hudCam` 出射 ⇒ 地板**打空**（正交 + forward 精确 (0,0,1) ⇒ 与法线垂直）"
                       + "｜🧨 把 HUD 相机改成透视 / 加一点俯角 ⇒ 本行红 ⇒ 那时 A-1 也测不出东西了");
            Tr(boardHits, "A-2b 同一屏幕点用 `boardCam` 出射 ⇒ 地板**打得中**（这才是原版那一跳）");
            Check(Vector3.Angle(hudRay.direction, boardRay.direction) > 0.5f,
                  "A-3 两台相机出的射线**方向确实不同**（夹角 " + Vector3.Angle(hudRay.direction, boardRay.direction).ToString("F2") + "° > 0.5°）"
                + " —— 「混成一台」与「按原版分工」在几何上是两件事");

            // ---- 源码层：出射那一句用的必须是 `boardCam`
            string src = StripComments(ReadSourceOrNull("CardPresentation/Battle/TargetReticle.cs"));
            Tr(src != null, "A-5（前提）读得到 `TargetReticle.cs` 源码（⛔ 读不到 ⇒ 下面 A-6 不算数）");
            if (src != null)
            {
                int ix = src.IndexOf("ScreenPointToRay", System.StringComparison.Ordinal);
                Tr(ix >= 0, "A-5b（前提）源码里有 `ScreenPointToRay` 那一句");
                if (ix >= 0)
                {
                    int lineStart = src.LastIndexOf('\n', ix) + 1;
                    string line = src.Substring(lineStart, ix - lineStart);
                    Tr(line.Contains("boardCam"), "★ A-6 `ResolveAim` 的出射相机 = **`boardCam`**（原版 `+0xA8`）"
                                                + "｜🧨 改回 `hc.ScreenPointToRay` ⇒ 本行红（那一整支又变成「地板恒不命中」）");
                    Tr(!line.Contains("hc.ScreenPointToRay") && !line.Contains("hc .ScreenPointToRay"),
                       "A-6b 出射**不再**用 `hc`（HUD 相机）");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(retGo);
            Object.DestroyImmediate(hudGo);
            Object.DestroyImmediate(boardGo);
        }
    }

    // ================================================================================================
    //  B · `PlayEvent<,>.StartMatch` 的防御卡两钮 —— 接在生产入口 `LiveOpsEventWindow.StartMatch` 上
    // ================================================================================================
    static void SectionB_防御卡两钮接生产()
    {
        Section("B 活动窗开战：原版 `StartMatch` 第二跳（防御卡两钮）必须接在真实入口上（`B146-PLAYEVENT-ENTRY`）");

        // 反射：方法/成员在不在（改坏法 = 有人把它们删掉却以为「反正没调用」）
        Check(typeof(LiveOpsEventWindow).GetMethod("TryDefensiveCardGate",
                  BindingFlags.Instance | BindingFlags.NonPublic) != null,
              "B-0a `LiveOpsEventWindow.TryDefensiveCardGate()` 在（防御卡闸本体）");
        Check(typeof(LiveOpsEventWindow).GetMethod("DefensiveCardStateForStart",
                  BindingFlags.Instance | BindingFlags.NonPublic) != null,
              "B-0b `LiveOpsEventWindow.DefensiveCardStateForStart()` 在（三态取值口 = 原版 `HasDefensiveCards`）");

        string src = StripComments(ReadSourceOrNull("CardPresentation/Shell/LiveOpsEventWindow.cs"));
        Tr(src != null, "B-0c（前提）读得到 `LiveOpsEventWindow.cs` 源码");
        if (src == null) return;

        int sm = src.IndexOf("public virtual void StartMatch()", System.StringComparison.Ordinal);
        Tr(sm >= 0, "B-0d（前提）找得到 `public virtual void StartMatch()`");
        if (sm < 0) return;

        // 取 `StartMatch` 的体：从声明起 4000 字符（够覆盖整段，且远短于下一个成员）
        string body = src.Substring(sm, Mathf.Min(4000, src.Length - sm));

        int gate = body.IndexOf("TryDefensiveCardGate()", System.StringComparison.Ordinal);
        Tr(gate >= 0, "★ B-1 `StartMatch` **真的调用**了 `TryDefensiveCardGate()`（= 这一支在生产上跑得到）"
                    + "｜🧨 删掉那一句 ⇒ 红（防御卡两钮又变回「生产 0 调用」）");

        int net = body.IndexOf("NetMatchmaking.TryStart", System.StringComparison.Ordinal);
        Tr(net >= 0, "B-2（前提）`StartMatch` 体里找得到 `NetMatchmaking.TryStart`（联机那一段 = 本地 `StartMatchNoCheck` 的起点）");
        Tr(gate >= 0 && net >= 0 && gate < net,
           "★ B-3 防御卡闸在**联机/开打之前**（原版顺序 = `ValidateEventDeck` → 防御卡闸 → `StartMatchNoCheck`）"
         + "｜🧨 把它挪到 `NetMatchmaking.TryStart` 之后 ⇒ 红（点了才拦，已经开打了）");

        // 两钮：ShowPopUp 必须收**五参**（正文 / OK 文案 / OK 回调 / Cancel 文案 / Cancel 回调）
        int impl = src.IndexOf("bool TryDefensiveCardGate()", System.StringComparison.Ordinal);
        Tr(impl >= 0, "B-4（前提）找得到 `TryDefensiveCardGate` 的实现体");
        if (impl >= 0)
        {
            string ibody = src.Substring(impl, Mathf.Min(2500, src.Length - impl));
            Tr(ibody.Contains("MenuDeck/Error/HasNotAssignedDefensiveCard"),
               "★ B-5 文案键 = **`MenuDeck/Error/HasNotAssignedDefensiveCard`**（原版 `StartMatch` 那一跳逐字）");
            Tr(ibody.Contains("MainMenu/General/Cancel"),
               "★ B-6 第二颗钮挂上了 `Cancel` 文案（= 原版 `_StartMatch_b__13_1` 的「关当前窗」那一半）"
             + "｜🧨 只给 `okText` ⇒ 第二颗钮点了什么都不发生");
            Tr(ibody.Contains("HaveItInInventory"),
               "B-7 只在 `HaveItInInventory` 这一档拦（原版**只判这一档**；`HaveItAssigned` / `DontHave` 直接放行）");
        }
    }

    // ================================================================================================
    //  C · 窗口级队列偏置（「后开的压先开的」）
    // ================================================================================================
    static void SectionC_窗口队列偏置()
    {
        Section("C 窗口级队列偏置：后开的整棵抬到「压过别的已开窗」之上；只抬不降；不许顶破 toast");

        Check(typeof(GameWindow).GetProperty("QueueBias", BindingFlags.Instance | BindingFlags.Public) != null,
              "C-0a `GameWindow.QueueBias` 在（自检读口 / 累计偏置）");
        Check(typeof(GameWindow).GetMethod("RaiseAbove", BindingFlags.Instance | BindingFlags.Public) != null,
              "C-0b `GameWindow.RaiseAbove(topQ, ceiling)` 在");
        var ceilField = typeof(GameWindow).GetField("CeilingQueue", BindingFlags.Public | BindingFlags.Static);
        Check(ceilField != null && (int)ceilField.GetValue(null) == 4071,
              "C-0c 天花板 `GameWindow.CeilingQueue = 4071`（= `ToastNotifications.QShade` 4072 的前一格）");

        GameObject aGo = null, bGo = null, cGo = null, dGo = null, eGo = null;
        try
        {
            var wa = NewWindow("w7_winA", out aGo);
            var wb = NewWindow("w7_winB", out bGo);
            var qa = ImageQuad.Create(wa.transform, Texture2D.whiteTexture, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "qa");
            var qb = ImageQuad.Create(wb.transform, Texture2D.whiteTexture, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "qb");
            Tr(qa != null && qb != null, "C-1a（前提）两扇窗各建出一颗 `ImageQuad`");
            if (qa == null || qb == null) return;

            qa.SetRenderQueue(3400);   // 「先开的」那一扇（队列更高 —— 正是今天跨窗压错的那一类）
            qb.SetRenderQueue(3002);   // 「后开的」那一扇（原版它应该在上面）

            Tr(wa.TopQueue() == 3400 && wb.TopQueue() == 3002, "C-1b（前提）两扇窗各自最高队列 = 3400 / 3002");

            int raised = wb.RaiseAbove(wa.TopQueue(), GameWindow.CeilingQueue);
            Check(raised == 399 && wb.QueueBias == 399,
                  "★ C-1 后开的 `wb` 整棵抬 399 格（3002 → 3401；= 3400 − 3002 + 1）｜🧨 `RaiseAbove` 改成空实现 ⇒ 红");
            Check(wb.TopQueue() == 3401 && qb.RenderQueue == 3401 && qb.RenderQueue > wa.TopQueue(),
                  "★ C-2 抬完**真的压过**先开的那扇（3401 > 3400）—— 这就是「后开的压先开的」");

            int again = wb.RaiseAbove(wa.TopQueue(), GameWindow.CeilingQueue);
            Check(again == 0 && wb.QueueBias == 399,
                  "C-3 已经在上面 ⇒ **一格都不动**（只抬不降；返回 0、累计偏置不变）");

            Check(qa.RenderQueue == 3400 && wa.QueueBias == 0,
                  "★ C-4 只动**自己这一棵**：先开的那扇队列与偏置一点没变"
                + "｜🧨 把 `RaiseAbove` 改成扫全场景 ⇒ 红");

            // ---- 天花板：抬完会顶破 ⇒ **不抬、也不用别的办法「少抬一截」**
            var wd = NewWindow("w7_winD", out dGo);
            var we = NewWindow("w7_winE", out eGo);
            var qd1 = ImageQuad.Create(wd.transform, Texture2D.whiteTexture, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "qd1");
            var qd2 = ImageQuad.Create(wd.transform, Texture2D.whiteTexture, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "qd2");
            var qe = ImageQuad.Create(we.transform, Texture2D.whiteTexture, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "qe");
            // 🔴 2026-11-05（`B150-W-E`）：要测「抬完**顶破**天花板」⇒ **要抬的那扇窗自己的队列跨度必须 > 35**
            //   （`maxQ + (topQ − minQ + 1) > 4071`，见 `GameWindow.RaiseAbove:1010-1011`）。
            //   原来 `we` 只有一颗 3002（跨度 0）⇒ 抬 1034 只到 4036、**根本不会顶破** ⇒ 这一组测的不是天花板。
            //   ⛔ 别改成「把 qe 抬到 4040」—— 那是 `minQ > topQ` 的「已经在上面」分支（C-3），同样测不到天花板。
            var qe2 = ImageQuad.Create(we.transform, Texture2D.whiteTexture, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "qe2");
            Tr(qd1 != null && qd2 != null && qe != null && qe2 != null, "C-5a（前提）天花板那一组也建出来了");
            if (qd1 == null || qd2 == null || qe == null || qe2 == null) return;

            qd1.SetRenderQueue(3002); qd2.SetRenderQueue(4040);   // 跨度 1038（> 35 ⇒ 抬 1034 会顶到 5074）
            qe.SetRenderQueue(3002); qe2.SetRenderQueue(4040);    // `we` 自己也是跨度 1038 = 「这一扇窗本来就很宽」（原版 SocialWindow 401 格那一类）

            int refused = we.RaiseAbove(4035, GameWindow.CeilingQueue);
            Check(refused == 0 && we.QueueBias == 0,
                  "★ C-5 会顶破天花板 ⇒ **不抬**（返回 0）");
            Check(qe.RenderQueue == 3002,
                  "★ C-6 拒抬时队列**一格都没动**（⛔ 不静默「少抬一截」—— 那会让「谁压谁」变成中间态、更难查）"
                + "｜🧨 改成「抬到天花板为止」 ⇒ 本行红");
        }
        finally
        {
            if (aGo != null) Object.DestroyImmediate(aGo);
            if (bGo != null) Object.DestroyImmediate(bGo);
            if (cGo != null) Object.DestroyImmediate(cGo);
            if (dGo != null) Object.DestroyImmediate(dGo);
            if (eGo != null) Object.DestroyImmediate(eGo);
        }
    }

    static GameWindow NewWindow(string name, out GameObject go)
    {
        go = new GameObject(name);
        return go.AddComponent<GameWindow>();
    }

    // ================================================================================================
    //  入口
    // ================================================================================================
    public static void Run()
    {
        _pass = _fail = 0;
        SectionA_准星出射相机();
        SectionB_防御卡两钮接生产();
        SectionC_窗口队列偏置();
        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // ================================================================================================
    //  源码读取（A-5/A-6 · B-*）—— 与 `B1022SIDES_Assert.ReadSourceOrNull/StripComments` 同形
    // ================================================================================================
    static string ReadSourceOrNull(string rel)
    {
        var full = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
        try { return File.Exists(full) ? File.ReadAllText(full) : null; }
        catch (System.Exception e) { Debug.LogWarning(Tag + " 读源码失败：" + e.Message); return null; }
    }

    /// <summary>去掉 `//` 行注释后再判 —— 本仓踩过「代码被追加到行末 `//` 之后 ⇒ 看着接上了、断言一条没跑」
    /// （`资料/完全复刻/报告/B102_W1_断言接线被注释吞掉.md`）⇒ 源码类判据一律先去注释。</summary>
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
