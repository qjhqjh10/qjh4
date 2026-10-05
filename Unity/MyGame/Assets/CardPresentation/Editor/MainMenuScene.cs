// MainMenuScene.cs — 主菜单场景（`MainMenu.unity`）的**建 / 自检 / 存盘**入口
//
// 用法：
//   … -executeMethod MainMenuScene.Run              自检（结构 + 截图），退出码 0 = 全过
//   … -executeMethod MainMenuScene.BuildAndSaveScene 建出场景存盘（给人打开按 Play 用）
//
// 施工图：`资料/主菜单_原版规格.md`（§二 层×参数 · **§五 绝对坐标** · §三② 出厂/运行时分界）。
// 🔴 每条断言都写了**它盯的是哪个原版值 + 出处** —— 不许拿我们自己写的常量断言我们自己写的常量。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;        // `DeckStore`/`DeckLibrary`/`CardDatabase`（战斗入口那一段要用）
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuScene
{
    const string P = "[Menu] ";
    const string ScenePath = "Assets/CardPresentation/Scenes/MainMenu.unity";
    const string ShotDir = "d:/4/_tmp_view/menu";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    static void Section(string t) { Debug.Log(P + $"--- {t} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want)) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
        else
        {
            _fail++;
            var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    static void CheckTrue(bool c, string msg) { Check(c, true, msg); }

    /// <summary>🆕 **2026-10-04（A47 接线批）**：压暗层（「点窗外关窗」）命中区那条不变量。
    /// 🔴 **2026-10-07（A77⑬⑥）这一份本文件里的副本已删** —— 全工程**唯一一份**在
    /// `MenuDraw.CheckShadeRule`（5 个宿主逐字各抄一份 = 「两处写同一条规则 = 迟早不一致」同族，审查点名收口）。
    /// ⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
    /// <para>🔴 **2026-10-07（A77⑬③）那条判据的期望值也换了**：**不再**比「调用方传进来的 `qShade` 常量」
    /// （那与 `ShadeHit` 的实参同一个符号 = 同义反复，A47/A48 独立审查指出 13/13 全中），
    /// 改成**量同一扇窗里「视觉压暗层」那颗 quad 的 `RenderQueue`** ⇒ 两个对象对不上必红。</para></summary>

    /// <summary>🆕 **2026-10-06（A94 相 2）**：窗内面板「吸收层」（`MenuDraw.Absorb`）那一组 ——
    /// **四条不变量 + 两条真能分辨的行为**。
    ///
    /// <para>语义（判据 → `Shell/MenuDraw.Absorb` 的注释）：原版窗内面板那颗 `Image` 的
    /// `m_RaycastTarget = 1`、而「点它关窗」那颗 `BackgroundCloseButton` **全库都挂在压暗层上**
    /// ⇒ 点窗内空白处**原版什么都不发生**；我们这边命中候选只收 `WindowButton` ⇒ 射线会**穿过面板**
    /// 落到压暗层那颗「点窗外关窗」上（这就是 A94 那个缺陷）。</para>
    ///
    /// <para>🔴 **期望值全是原版值**：矩形 = **原版 prefab 里那块面板 `Image` 的 rect 字面量**
    /// （⛔ 不写被测那份实现**传进去的实参** —— 那是最浅一档的同式自证）；
    /// 档 = 该窗自己的**原版档常量**（`qShade` / `qContentMin`；⚠️ 这两个是 `Absorb` 的入参来源，
    /// 与压暗层那条 `MenuDraw.CheckShadeRule` 不是同一套判据 —— 后者 2026-10-07 起改成量场景真值了）。</para>
    ///
    /// <para>🔴 **为什么两条行为必须一起断**：只断「点面板 ⇒ 不关」时，一个**根本关不掉的窗**也能绿；
    /// 只断「点面板外 ⇒ 关」时，把窗建小到「点哪儿都关」也绿。两条互为对照才分得出这两条路。</para>
    ///
    /// <para>⚠️ **点哪儿**：先试**原版矩形中心**，被窗内真件（按钮）盖住时沿一圈**固定的**候选点找
    /// 一个「命中是吸收层」的点。⛔ 「命中是谁」**不是期望值**，它只是**选点的条件**；
    /// 这一组断的是**窗的状态**（`state()`）。</para></summary>
    static void CheckAbsorbRule(string what, Transform winRoot, string nodeName,
                                float x1, float y1, float x2, float y2,
                                int qShade, int qContentMin, System.Func<WindowState> state)
    {
        // ① 节点在 ② 是公共件建的
        // ⚠️ **先按窗根的直接子件取**（相 1：20 个吸收层都是窗根的直接子件；只有 `RankedEventWindow`
        //   那个嵌在 `General Red Background` 底下）—— 直接子件取不到再退到递归查找。
        //   🔴 为什么不能一上来就递归找：`SkirmishEventWindow` 里**嵌着** `Searching Oponent Popup`，
        //   那扇自己也有一个 `AbsorbHit` ⇒ 递归找会按层级序先撞上谁不好说（本窗自己的那个排在前面，
        //   但那是**层级序的巧合**，不是判据）。
        var node = winRoot != null ? winRoot.Find(nodeName) : null;
        if (node == null) node = FindChild(winRoot, nodeName);
        CheckTrue(node != null,
                  $"{what}：吸收层节点 `{nodeName}` 在（`MenuDraw.Absorb` 建的 —— 原版面板那颗 `Image` 的等价物）");
        CheckTrue(MenuDraw.WasAbsorb(node),
                  $"{what}：它是**公共件 `MenuDraw.Absorb` 建的**（`MenuDraw.WasAbsorb`；哪扇窗自己再写一份就红）");
        // ③ 矩形 = 原版那块面板底图的 rect（量 `ImageQuad` 自己的渲染真值）
        float x1g, y1g, x2g, y2g;
        if (!HitQuadRect(node, out x1g, out y1g, out x2g, out y2g))
        {
            CheckTrue(false, $"{what}：吸收层下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这一层等于没建）");
        }
        else
        {
            CheckNear(x1g, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图");
            CheckNear(y1g, y1, 1.5f, $"{what}：…**上沿**");
            CheckNear(x2g, x2, 1.5f, $"{what}：…**右沿**");
            CheckNear(y2g, y2, 1.5f, $"{what}：…**下沿**");
            var q = node.GetComponentInChildren<ImageQuad>();
            int wantQ = qContentMin - 1;
            Check(q != null ? q.RenderQueue : -1, wantQ,
                  $"{what}：吸收层的档 = **内容命中区档 − 1**（{qContentMin} − 1 = {wantQ}）");
            CheckTrue(q != null && qShade < q.RenderQueue && q.RenderQueue < qContentMin,
                      $"{what}：**{qShade} < 吸收层档 < {qContentMin}** —— 严格夹在压暗层与内容命中区之间"
                      + "（同档时 `ImageQuad` 的世界 z 恒 0，谁吃到命中退化成枚举顺序）");
        }
        // 🔴 2026-10-09（A221④）：改成**按窗记账** —— 只认**这一颗**吸收层节点上的标记，
        //    不再读那个全局累积计数器（`MenuDraw.AbsorbTierWarns` 已删）。
        //    改坏法：把这一扇窗的档传错 ⇒ **只有本窗**红，且文案带**这一颗节点当时**的告警正文。
        bool aWarned = MenuDraw.AbsorbTierWarned(node, out string aw);
        CheckTrue(!aWarned,
                  $"{what}：`MenuDraw.Absorb` 对**这一颗**吸收层**没报过档位告警**（按窗记账 —— 只认这颗节点上的标记，"
                  + "不受别的窗影响）" + (aWarned ? "；⚠️ 实得告警：" + aw : ""));

        // ⑤⑥ 两条行为（互为对照）
        var pl = PointerLayer.Instance;
        CheckTrue(pl != null, $"{what}：场景里有指针层（没有的话下面两条等于没查）");
        if (pl == null) return;
        float ccx = (x1 + x2) * 0.5f, ccy = (y1 + y2) * 0.5f;
        // 候选点：**原版矩形中心**优先 → 中心外一圈(±80) → 最后**贴着四条边内缩的那一圈**
        // （面板的边框那一圈通常没有内容件；例：练习窗选卡组那一列中间**全被卡组格盖住**，
        //  只有左边距那 25px 是空的）。⛔ 候选是**固定**的（不扫描全图）⇒ 点了哪儿可复现。
        var cand = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(0f, -80f), new Vector2(0f, 80f), new Vector2(-80f, 0f), new Vector2(80f, 0f),
            new Vector2(-80f, -80f), new Vector2(80f, -80f), new Vector2(-80f, 80f), new Vector2(80f, 80f),
        };
        for (int k = 0; k < EdgeInset.Length; k++)
        {
            float e = EdgeInset[k];
            cand.Add(new Vector2(x1 + e - ccx, y1 + e - ccy)); cand.Add(new Vector2(x2 - e - ccx, y1 + e - ccy));
            cand.Add(new Vector2(x1 + e - ccx, y2 - e - ccy)); cand.Add(new Vector2(x2 - e - ccx, y2 - e - ccy));
            cand.Add(new Vector2(x1 + e - ccx, 0f));           cand.Add(new Vector2(x2 - e - ccx, 0f));
            cand.Add(new Vector2(0f, y1 + e - ccy));           cand.Add(new Vector2(0f, y2 - e - ccy));
        }
        float px = 0f, py = 0f; bool found = false;
        for (int i = 0; i < cand.Count && !found; i++)
        {
            float tx = ccx + cand[i].x, ty = ccy + cand[i].y;
            if (tx <= x1 + 3f || tx >= x2 - 3f || ty <= y1 + 3f || ty >= y2 - 3f) continue;   // 必须落在**原版**矩形里
            if (tx < 2f || tx > 1918f || ty < 2f || ty > 1078f) continue;                    // 而且**在屏幕里**（玩家点不到屏外的点）
            var hb = pl.ButtonAt(tx, ty);
            if (hb != null && hb.absorbOnly) { px = tx; py = ty; found = true; }
        }
        // 兜底：上面那圈**全都撞上内容件**时，按 **40px 固定步长**在矩形里走一遍（确定性 —— 不是随机），
        // 取第一个「命中是吸收层」的点。⚠️ 它只决定**点哪儿**，不参与任何期望值。
        for (float gy = y1 + 4f; gy <= y2 - 4f && !found; gy += 40f)
            for (float gx = x1 + 4f; gx <= x2 - 4f && !found; gx += 40f)
            {
                if (gx < 2f || gx > 1918f || gy < 2f || gy > 1078f) continue;
                var hbg = pl.ButtonAt(gx, gy);
                if (hbg != null && hbg.absorbOnly) { px = gx; py = gy; found = true; }
            }
        CheckTrue(found, $"{what}：**原版面板矩形以内找得到一个点、它的命中是吸收层**"
                         + "（找不到 ⇒ 窗内空白处没吃下这一下，射线会穿到压暗层上 ⇒ A94 那个缺陷还在）");
        if (!found) return;
        Check(state(), WindowState.Open, $"{what}：（前提）这一刻窗是开着的");
        CheckTrue(pl.ClickAt(px, py), $"{what}：点面板（真路径 `PointerLayer.ClickAt`，实点 ({px:F1},{py:F1})）");
        Check(state(), WindowState.Open,
              $"{what}：**点面板 ⇒ 窗不关**（原版面板那颗 `m_RaycastTarget = 1` 的 `Image` 吃掉了这一下）");
        // 点面板外：**钉死屏幕左上角 (5,5)**（本批 20 个吸收矩形都不覆盖它，相 1 逐条核过）。
        // ⛔ 不许改成「扫一圈找第一个命中压暗层的点」—— 命中区一旦又变得过大，搜索会从别的点**绕过去**、
        //   这条就再也查不出那个缺陷了（本工程那一族「弱断言分不出两种状态」；判据全文 → `CollectionScene.cs` 那一版）。
        const float OutX = 5f, OutY = 5f;
        var oHit = pl.ButtonAt(OutX, OutY);
        // 🔴 **判据 = 两条合起来**，⛔ 不许再退回「非吸收层 ∧ 属于本窗」那种**分不出两种状态**的弱条件
        //   （旧写法下**超大的内容命中区**三条全满足 ⇒ 照样绿，正是它把 A94 那个缺陷放过去了）：
        //     · `oHit.transform.IsChildOf(winRoot)` = **是这一扇自己的**（别家的窗顶掉它就红）；
        //     · `MenuDraw.WasShadeHit(oHit.transform)` = **是压暗层那一颗**（`ShadeHit` 建的，按节点上的**标记**认、
        //       ⛔ **不按名字认** —— 本族那颗节点有多个名字：`BackgroundHit` / `CloseHit`
        //       （名字是各调用点自己传的 `MenuDraw.ShadeHit(..., name)` 形参）⇒ 按名字写 `Find("BackgroundHit")`
        //       会把聊天窗那条**误判成红**）。
        //   ⛔ **别只写 `WasShadeHit`**：它认「是不是压暗层那颗」、**不认「是哪一扇的」**。
        CheckTrue(oHit != null && oHit.transform.IsChildOf(winRoot) && MenuDraw.WasShadeHit(oHit.transform),
                  $"{what}：**({OutX:F0},{OutY:F0}) 命中的就是这扇窗自己的压暗层那一颗**"
                  + "（命中区过大 / 吸收层 / 别家的窗把它顶掉时**这条红** —— 旧写法分辨不出，就是它放过了 A94）"
                  + "（实得 `" + (oHit != null ? oHit.name : "<null>") + "`"
                  + (oHit == null ? " = **什么都没命中**"
                     : !oHit.transform.IsChildOf(winRoot) ? " = **别家的窗**"
                     : !MenuDraw.WasShadeHit(oHit.transform) ? " = **本窗的，但不是压暗层那一颗**" : "")
                  + "）");
        CheckTrue(pl.ClickAt(OutX, OutY), $"{what}：点面板外 ({OutX:F0},{OutY:F0})（真路径）");
        Check(state(), WindowState.Closed, $"{what}：**点面板外 ⇒ 关窗**（两条互为对照才分得出）");
    }

    /// <summary>`CheckAbsorbRule` 贴边候选的**内缩**距离（px，固定三档；见那段注释）。</summary>
    static readonly float[] EdgeInset = { 6f, 20f, 40f };

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what)
    {
        int n; string bad = WindowButton.AuditHoverSwap(root, out n);
        CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
    }

    static void CheckNoMissingSwapArt(string what)
        => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                     what + "：**悬停图一张都不缺**（缺的会列在这里：" + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");
    /// <summary>文本比对 + 取一段文字（战斗入口那段要断文案）。</summary>
    static void CheckText(string got, string want, string msg)
        => CheckTrue(got == want, $"{msg} —— 实测「{got}」，期望「{want}」");
    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F3} ≈ {want:F3}±{tol:F3}）");

    /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1 px）。</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = MainMenuRuntime.Center(x1, x2, y1, y2);
        var got = t.localPosition;
        float d = Vector3.Distance(got, want);
        CheckTrue(d <= 0.01f,
                  $"{what} 在 §五 给的矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>同 `CheckAt`，但比**世界坐标**（`t.position`）—— 给「挂在别的层底下」的节点用。
    /// 🔴 `CheckAt` 比的是 `localPosition`，只对**窗口根的直接子件**成立（那些件的 local 恰好等于页坐标）；
    /// 子件在 `Deck info/General container` 这种层里时，`local` 是相对父节点的 ⇒ 必须走世界坐标。</summary>
    static void CheckAtWorld(Transform t, float x1, float x2, float y1, float y2, string what)    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = MainMenuRuntime.Center(x1, x2, y1, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f,
                  $"{what}（世界坐标差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>🆕 2026-10-03：取一段文字的**字距**（没有 `Label` 就给 `NaN`）。
    /// 原版有几处 TMP 带 `m_characterSpacing`（`Window Title` 5 · `DivisionText` −2.6 · 开关两行 −4），
    /// 以前 `Label` 没有接口、这几处一直没复刻；现在有了就必须钉住。</summary>
    static float CharSpacingOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.CharSpacing : float.NaN;
    }

    /// <summary>量一个节点**渲出来**的像素矩形（1920×1080 · 左上原点）。
    /// 用 `Label`/`ImageQuad` 自己算出来的 `WorldW/WorldH` + 节点世界坐标反算 ——
    /// **不抄源码常量**（§10·3 第 1 层那条：必须量渲染真值）。</summary>
    static bool RenderedRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        float w, h;
        var lb = t.GetComponentInChildren<Label>();
        if (lb != null) { w = lb.WorldW * 108f; h = lb.WorldH * 108f; }
        else
        {
            var q = t.GetComponentInChildren<ImageQuad>();
            if (q == null) return false;
            w = q.WorldW * 108f; h = q.WorldH * 108f;
        }
        float cx = LayoutSpace.PxX(t.position.x), cy = LayoutSpace.PxY(t.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
    }

    /// <summary>量**命中区那颗 quad 自己**的渲染矩形（不是承载它的节点）。
    /// 🔴 **为什么单开一个（2026-10-03 踩过）**：`MenuDraw.Hit` 的写法是「**节点摆在父原点**（`localPosition = 0`）、
    /// quad 摆在矩形中心」（照抄 `MainMenuSubmenuWindow.AddHit`，那边 1000+ 条断言盯着、不许改写法）。
    /// ⇒ `RenderedRect(t)` 拿 `t.position` 当中心，**只有「没被裁」时才恰好等于矩形中心**（所以一直没露）；
    /// 一被裁，quad 的中心移了、**节点没动** ⇒ 它报的是「以**整块**中心为中心、高 = **截后**高」的**假矩形**。
    /// 实据（2026-10-03）：排行榜压边行那颗 `Hit` 真值 y = 864.57..**937.83**，`RenderedRect` 报 **954.42**
    /// （= 整块中心 917.79 + 73.26/2）⇒ 一条**判对了实现、量错了东西**的假红。
    /// 判据 = **量渲染真值**：`ImageQuad` **自己的** `transform.position` + `WorldW/WorldH`
    /// （同上面「行底九宫格量子块、别量根节点」那条）。</summary>
    static bool HitQuadRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return false;
        float w = q.WorldW * 108f, h = q.WorldH * 108f;
        float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
    }

    // ============================================================ 建场景

    static MainMenuRuntime Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 相机（外壳那边也有一台，`DontDestroyOnLoad` 会带过来；这里建一台是为了
        // **单独打开本场景按 Play 也能看** —— 两台的参数一致，见 `ShellScene.Build`）
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;   // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        var rootGo = new GameObject("MainMenu");
        var rt = rootGo.AddComponent<MainMenuRuntime>();
        rt.Build();
        root = rootGo.transform;
        return rt;
    }

    static void Shoot(string file)
    {
        var cam = Camera.main;
        if (cam == null) return;
        const int W = 1920, H = 1080;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    // ============================================================ 自检

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);
        Debug.Log(P + "=== 主菜单自检 开始 ===");

        var menu = Build(out var root);
        // 🔴 **2026-09-26 加**：主菜单**刚建好、还没开任何弹窗**时先拍一张。
        //    原来只有末尾那张 `01_主菜单.png`，而那时**屏幕上压着一个提示弹窗**（自检跑完遗留的）
        //    ⇒ 拿它当「主菜单长什么样」的参照会**看错**（2026-09-26 就被它骗过一次）。
        Shoot("00_主菜单_无弹窗.png");

        Section("整屏背景（§五 A：`Image(sprite=0)` + 双色渐变，**不是图也不是 3D**）");
        var bg = menu.Find("Background");
        CheckTrue(bg != null, "`Background` 建了");
        if (bg != null)
        {
            var q = bg.GetComponentInChildren<ImageQuad>();
            CheckTrue(q != null && q.Texture != null && q.Texture.name.StartsWith("grad_"),
                      "背景用的是**运行时生成的双色渐变**（`CardArt.Gradient`，c1/c2/angle 照 `MB1931`）");
            if (q != null) CheckNear(q.WorldH, LayoutSpace.DesignHeight, 0.01f, "背景铺满可见高度（10 个世界单位）");
            // 🔴 **方向也要对**（2026-09-27 修：我们原来把轴画成 90° 歪的 —— 上暗下亮，原版是左暗右亮）。
            //    判据 = 原版实拍量出来的亮度场（左 9 → 右 20、上下几乎不变）→ `资料/主菜单_原版规格.md` §A。
            //    渐变贴图里 **索引 0 = 左下**（`SetPixels` 的约定）⇒ 比「右上 vs 左下」即可。
            var gt = q != null ? q.Texture as Texture2D : null;
            if (gt != null)
            {
                int N = gt.width;
                float tr = gt.GetPixel(N - 1, N - 1).r;    // 右上
                float bl = gt.GetPixel(0, 0).r;            // 左下
                CheckTrue(tr > bl,
                          $"★ 背景渐变**亮在右上、暗在左下**（右上 R={tr:F3} > 左下 R={bl:F3}）—— "
                        + "原版实拍就是「左暗右亮」（9→20、上下几乎不变）；我们把轴画歪 90° 时是反的");
            }
        }

        Section("左竖导航骨架（§五 C：`Navigation Panel` 0..191 · 分隔线 71..1080）");
        var nav = menu.Find("Navigation Panel");
        CheckTrue(nav != null, "`Navigation Panel` 建了");
        CheckAt(FindChild(nav, "Background"), -164.4f, 165.1f, 0.1f, 1145.9f, "侧栏 `Background`（`White Square`）");
        CheckAt(FindChild(nav, "Separators Left"), -2.5f, 0.3f, 71.0f, 1080.0f, "`Separators Left`");
        CheckAt(FindChild(nav, "Separators Right"), 164.0f, 166.8f, 71.0f, 1080.0f, "`Separators Right`");
        CheckAt(FindChild(nav, "Panel Shadow"), -762.5f, 1166.2f, 511.7f, 581.7f, "`Panel Shadow`");

        Section("5 个导航按钮（§五 C：**y 是按 VLG 算的**，spacing −16.35 / padTop −5）");
        CheckNavButton(menu, "Home",       143.4f, 313.1f, "40k_main_bt_play");
        CheckNavButton(menu, "Collection", 296.7f, 466.4f, "40k_main_bt_collection");
        CheckNavButton(menu, "Shop",       450.1f, 619.8f, "40k_main_bt_shop");
        CheckNavButton(menu, "Rewards",    603.4f, 773.1f, "40k_main_bt_rewards");
        CheckNavButton(menu, "Social",     756.7f, 926.4f, "40k_main_bt_friends");
        CheckAt(FindChild(menu.Find("Main Menu Navigation Button - Home"), "Selected highlight"),
                -0.3f, 164.0f, 143.4f, 313.1f, "**选中态高亮**在 `Home` 的整按钮矩形上（原版 5 个出厂都 active）");

        // ============================================================ 🆕 2026-10-10（A284 + A222）
        // ① **图标那一格的矩形**：原版五颗里**只有 `Home` 不是正方形** ——
        //    `Home` = **156.349 × 137.435**（`RT 1111` · `apos (83.095, −71.70)` · `anchor (0,1)`
        //    · `m_PreserveAspect = 0`；顶边距按钮顶 = 71.70 − 137.435/2 = **2.98**，我们传的 `iy1 = 146.4`
        //    对按钮顶 143.4 正是 3.0）；其余四颗 = **140 × 140**（`RT 1240/1241/1242/1243`）。
        //    我们原来把「宽」当「高」用 ⇒ `Home` 画成 156.3²（**高多 18.865px、中心低 9.45px**）。
        //    判据全文 → `资料/普查产出_1009/查证V3_口径三件.md` §五·1。
        //    ⚠️ 宽那一条：原版 **156.349**，我们的 `ix2 − ix1` = 160.9 − 4.6 = **156.3**（§五 C 表的取整）
        //    ⇒ 容差给 **0.5px**；**高那一条是硬判据**（改坏了的实现差 18.87px，怎么都红）。
        //    🔴 **五颗都要断**：只断 `Home` 一条是**弱断言** —— 把五颗一起改成 137.435 它照样绿。
        //    🔴 **改坏法**：`NavButton` 里把高写回 `ix2 − ix1`（今天那样）⇒ `Home` 那条红；
        //       把另外四颗也改成 137.435 ⇒ 下面那四条对照红。
        Section("★ 导航钮图标那一格（A284：**只有 `Home` 不是正方形**）+ 悬停色偏打在哪颗图形上（A222）");
        {
            CheckNavIconRect(menu, "Home",       4.6f,  160.9f, 146.4f, 283.835f, "★ `Home` 图标 = **156.349×137.435**"
                             + "（原版唯一那颗非正方形；正方形实现高多 18.87px）");
            CheckNavIconRect(menu, "Collection", 12.8f, 152.8f, 298.4f, 438.4f, "`Collection` 图标 = **140×140**");
            // 🔴 **2026-10-11（F2）就地订正（铁律 5）**：`Shop`/`Rewards`/`Social` 三条的 **y 各 −10**
            //    （461.8..601.8 → **451.8..591.8** · 605.1..755.1 → **605.1..745.1** · 768.4..908.4 → **758.4..898.4**）。
            //    错因：期望值抄的是错文档 `资料/主菜单_原版规格.md:345-347`（那三行各 +10，同一次一并订正）。
            //    ⚠️ `Rewards` 那条**只错 y2**（y1 抄的是对的 605.1）⇒ 期望矩形成了 **150 高**、
            //     与文案里的「140×140」自相矛盾 ⇒ 它是唯一红的那条；`Shop`/`Social` 两条
            //     因为**实现与期望一起错**而**假绿**（那正是「期望值从文档抄」的代价）。
            //    🔴 **期望值独立来源**（⛔ 不读 `MainMenuRuntime` 的常量）：原版
            //      `RectTransform_1241`(Shop) / `1243`(Rewards) / `1240`(Social) —— `anchorMin=anchorMax=(0,1)` ·
            //      `apos=(83.0949,−71.70)` · `pivot=(0.5,0.5)` · `sizeDelta=(140,140)`
            //      ⇒ 图标顶 = 钮顶 **+1.70**、高 **140**；钮顶按父 VLG（`spacing −16.35` · `UpperCenter` ·
            //      `m_Padding.m_Top −5` · `ctrlH=0` · 钮 RT 高 `169.68`）算 = **450.07 / 603.40 / 756.73**。
            CheckNavIconRect(menu, "Shop",       12.8f, 152.8f, 451.8f, 591.8f, "`Shop` 图标 = **140×140**");
            CheckNavIconRect(menu, "Rewards",    12.8f, 152.8f, 605.1f, 745.1f, "`Rewards` 图标 = **140×140**");
            CheckNavIconRect(menu, "Social",     12.8f, 152.8f, 758.4f, 898.4f, "`Social` 图标 = **140×140**");

            // ①·b 🔴 **2026-10-11（F2）补一条【相对】断言** —— 上面五条是**绝对量**，一旦有人再把某个
            //   「文档值」抄进期望里，错的那几条会**互相掩护**（本次就是这样：`Shop`/`Social` 跟着错文档
            //   一起 +10 ⇒ 假绿、只有 `Rewards` 因 y2 没跟着改才红）。
            //   本条只比**相邻图标顶之间的步进**，期望值 = 原版**钮 RT 高 `169.68`** + VLG **`spacing −16.35`**
            //   = **153.33**（两个数都是原版 prefab 字段，⛔ 不读实现里的任何常量）。
            //   `Home→Collection` 那一档要减掉顶距差：2.9825 − 1.70 = 1.2825 ⇒ 期望 **152.05**
            //   （Home 是唯一那颗非正方形，见 A284）。
            //   🔴 **改坏法**：`MainMenuRuntime.NavButton` 里把任一颗的 `iy1` 单独挪 ±10 ⇒ **相邻两步立刻差 10**
            //      （本次那个错法就是这么现形的）；⛔ 别把它写成「只比某一对的差」—— 四档全比才拦得住「整列挪」。
            CheckNavIconStep(menu, "Home", "Collection", 152.05f);
            CheckNavIconStep(menu, "Collection", "Shop", 153.33f);
            CheckNavIconStep(menu, "Shop", "Rewards", 153.33f);
            CheckNavIconStep(menu, "Rewards", "Social", 153.33f);

            // ② **色偏（悬停）要打在那颗【可见】的图标上**，颜色 = 原版 `m_Colors.m_HighlightedColor`
            //    = **浅蓝 `(0.7217, 0.8152, 1.0)`**（不是全库默认的 0.9608 灰；五颗逐值相同）。
            //    判据 = `bundle_scenes_scenes_mainmenuwarpforge` 那 5 个 GO 上的 `EverguildToggle`
            //    （`m_Transition = 1` · `m_TargetGraphic` = **各自的直接子件 `Image`** —— 逐条解父链，
            //     **5/5 都在这颗 `Selectable` 自己的子树里**，一次也没打到「另建的命中区」上）。
            //    🔴 **改坏法**：`MainMenuRuntime.NavButton` 里删掉 `wb.TintOn(iconQ)` ⇒ 色偏只落在那颗
            //       **透明**命中区 quad 上（`(0,0,0,0)`），可见图形**纹丝不动** ⇒ 下面两条红。
            CheckHoverTint(NavHitButton(menu, "Home"), NavIconQuad(menu, "Home"), MainMenuRuntime.NavHoverKey,
                           "★ `Home` 悬停");
            CheckHoverTint(NavHitButton(menu, "Social"), NavIconQuad(menu, "Social"), MainMenuRuntime.NavHoverKey,
                           "★ `Social` 悬停（第二颗对照 —— 五颗共用同一套判据）");
        }

        Section("顶栏（§五 B：`Upper bar` 0..100 · 齿轮 1803..1891 · 三个钮**按 HLG 算**）");
        var bar = menu.Find("Upper bar");
        CheckTrue(bar != null, "`Upper bar` 建了");
        CheckAt(FindChild(bar, "Background"), -11.7f, 1920f, 0f, 71.3f, "顶栏 `Background`（`UI_Main_Upper bar`）");
        // 🔴 **2026-10-11（A283）：顶栏那一带降到【所有窗 / 弹窗之下】** —— 用户当天裁定「照原版」，
        //    **推翻 2026-09-28 那次**（那次照的是一张**二手实拍**：顶栏盖在窗上）。
        //    四条独立判据（兄弟序 / `overrideSorting + PopUps` 排序层 / 专用 `CustomRaycaster` / 名字阶梯
        //    `10·5·15`）→ `MainMenuRuntime.QBarPanel` 那段注释；判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q3。
        //    ⚠️ 下面这些期望值**全部取自【别的文件里的既有档位常量】**（弹窗压暗层 / 窗口基类），
        //      ⛔ 不是从 `MainMenuRuntime` 自己读出来的（那是同式自证）。
        //    改坏法：把 `QBar*` 整条改回 3600 段 ⇒ **前两条红**（那正是「顶栏压住窗」那一版）。
        {
            CheckTrue(MainMenuRuntime.QBarOverlay < DailyStreakPopup.QShade,
                      $"顶栏那一带（{MainMenuRuntime.QBarPanel}~{MainMenuRuntime.QBarOverlay}）**整体低于全工程最低的窗档**"
                      + $"（`DailyStreakPopup.QShade = {DailyStreakPopup.QShade}`）"
                      + " —— 弹窗打开时**它的压暗层盖住顶栏**（原版如此：排序层 `PopUps` 压过 `Default`）");
            CheckTrue(MainMenuRuntime.QBarOverlay < MainMenuSubmenuWindow.QPanel,
                      $"…也低于窗口外壳的内容档（`MainMenuSubmenuWindow.QPanel = {MainMenuSubmenuWindow.QPanel}`）"
                      + " —— **连没有压暗层的那种窗也照样盖住顶栏**（原版如此，⛔ 别当缺陷修）");
            var barChat = menu.Find("ChatPreview");
            int inBand = 0, wrong = 0; string firstWrong = null;
            foreach (var q in menu.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q.RenderQueue <= 0) continue;
                bool underBar = q.transform.IsChildOf(bar) || (barChat != null && q.transform.IsChildOf(barChat));
                bool inBarBand = q.RenderQueue >= MainMenuRuntime.QBarPanel && q.RenderQueue <= MainMenuRuntime.QBarOverlay;
                if (underBar)
                {
                    if (inBarBand) inBand++;
                    else { wrong++; if (firstWrong == null) firstWrong = q.name + "（队列 " + q.RenderQueue + "）"; }
                }
                else if (inBarBand)
                { wrong++; if (firstWrong == null) firstWrong = "**非顶栏件** " + q.name + "（队列 " + q.RenderQueue + "）"; }
            }
            int labInBand = 0;
            foreach (var lb in menu.GetComponentsInChildren<Label>(true))
                if (lb.RenderQueue == MainMenuRuntime.QBarText
                    && (lb.transform.IsChildOf(bar) || (barChat != null && lb.transform.IsChildOf(barChat)))) labInBand++;
            CheckTrue(inBand >= 12, $"顶栏那条带子里有 {inBand} 张图排在 {MainMenuRuntime.QBarPanel}~{MainMenuRuntime.QBarOverlay} 档");
            CheckTrue(labInBand >= 3, $"顶栏的文字也跟着走（{labInBand} 段排在 `QBarText`）");
            CheckTrue(wrong == 0, $"顶栏带子**之外**的件一张都没排进 [{MainMenuRuntime.QBarPanel},{MainMenuRuntime.QBarOverlay}]"
                      + "（实测 " + wrong + " 处"
                      + (firstWrong == null ? "）" : "：" + firstWrong + "）"));
        }
        CheckAt(FindChild(FindChild(bar, "SettingsBtn"), "Image"), 1803.1f, 1890.9f, 4.6f, 66.4f, "`SettingsBtn` 齿轮");
        // 🔴 **2026-09-26 加的**：这颗齿轮从建出来那天起**点了没反应**（只建了图、没接点击 = 静默失败）。
        //    这一条钉住「它有点击区、而且点了真能开设置窗」—— 光有矩形断言抓不到这种缺陷。
        {
            var gear = FindChild(FindChild(bar, "SettingsBtn"), "Image");
            var wb = gear != null ? gear.GetComponent<WindowButton>() : null;
            CheckTrue(wb != null && wb.onClick != null, "`SettingsBtn` **接了点击**（原来没有 —— 点了什么都没发生）");
            if (wb != null && wb.onClick != null)
            {
                wb.onClick();
                var first = SettingsWindow.Instance;
                CheckTrue(first != null && first.CurrentState == WindowState.Open,
                          "点齿轮 ⇒ **真的开了设置窗**（`SettingsWindow`）");
                CheckTrue(first != null && FindChild(first.transform, "Tab Buttons") != null,
                          "开出来的那扇窗里有 `Tab Buttons`（三个页签：图像 / 音频 / 联机）");
                // 🔴 **2026-10-05（A104）**：**再点一次**。原版 `WindowsManager.OpenWindow` 第一件事是查
                //    `automaticallyLoadedWindows` 缓存（VA `0x180875990` 起、`call 0x1815caa30` = `TryGetValue`，
                //    命中就跳去复用）⇒ **同一扇窗点两次只有一个实例**，第二次只是重跑一遍 `TryOpen`。
                //    判据全文 → `MainMenuRuntime.OpenSettings` 的注释。
                //    ⚠️ 断言盯**行为**（同一实例 / 开着的窗里只有 1 扇），⛔ 不盯我们自己的常量。
                if (first != null)
                {
                    wb.onClick();
                    CheckTrue(SettingsWindow.Instance == first && first.CurrentState == WindowState.Open,
                              "再点一次齿轮 ⇒ **还是同一扇**（原版 `automaticallyLoadedWindows` 命中复用；原来会叠出第二扇）");
                    int nSet = 0;
                    if (WindowsManager.Instance != null)
                        foreach (var w in WindowsManager.Instance.openWindows)
                            if (w is SettingsWindow) nSet++;
                    CheckTrue(nSet == 1,
                              $"「开着的窗」里设置窗只有 **1** 扇（实测 {nSet}）—— 点两次齿轮原来会叠出第二扇");
                    first.Close();
                }
            }
        }
        // ============================================================ ★ 2026-10-11（A283）：顶栏 vs 弹窗 —— **两态**
        // 判据（用户 2026-10-11 裁定 + 四条独立证据）⇒ 原版那一刻**弹窗在顶栏之上**。
        // 做法：**同一个点**量两态，走 `PointerLayer`（全壳唯一那条「真鼠标 → 界面」的路径；
        // 批处理里没有输入事件 ⇒ 自检直调它的 `ButtonAt` / `ClickAt`，这是唯一入口）：
        //   · **没有弹窗** ⇒ 吃到的是**顶栏那颗齿轮**（顶栏照常能用）；
        //   · **开着弹窗** ⇒ 吃到的是**那扇窗的压暗层命中区**（`BackgroundHit`），**点它 = 关窗**。
        // ⛔ 期望值不是抄来的常量，而是**场景里那两颗节点的身份**（齿轮那颗 `Image` 自己 / `SettingsWindow.ShadeHit`）
        //   —— 比的都是「这一点上到底是谁吃到了」。
        // 改坏法：`QBar*` 改回 3600 段 ⇒ **第 3 条红**（吃到的是齿轮、窗也关不掉）；
        //   把顶栏那一带整个删掉 ⇒ **第 1 条红**（`ButtonAt` 返回 null）；把压暗层的「点窗外关窗」摘掉 ⇒ **第 3 条红**。
        {
            const float gearX = 1847.0f, gearY = 35.5f;   // = 齿轮那一格的中心（1803.1..1890.9 × 4.6..66.4，正本 §五 B）
            var pl = PointerLayer.Instance;
            var gearHit = FindChild(FindChild(bar, "SettingsBtn"), "Image");
            CheckTrue(pl != null && gearHit != null, "★ 两态探针的前置：指针层在、顶栏齿轮那颗在（否则下面几条等于没查）");
            if (pl != null && gearHit != null)
            {
                // ---- 态①：**没有弹窗** ⇒ 顶栏按钮照常吃得到（少了这条，把顶栏整个废掉也能绿）----
                var none = pl.ButtonAt(gearX, gearY);
                CheckTrue(none != null && none.gameObject == gearHit.gameObject,
                          "★ 态①（**没有弹窗**）：顶栏那一点吃到的仍是**齿轮那颗按钮**"
                          + "（实得 " + (none == null ? "<null>" : "`" + none.name + "`") + "）");
                // 用**真路径**（命中 → 派发）进态② —— 上面那一组走的是直调 `wb.onClick()`，这里是另一条路
                CheckTrue(pl.ClickAt(gearX, gearY), "★ …而且这一下走得通（`PointerLayer.ClickAt` ⇒ 命中 ⇒ 派发 `onClick`）");
                var sw = SettingsWindow.Instance;
                CheckTrue(sw != null && sw.CurrentState == WindowState.Open,
                          "★ 态②的前置：那一下**真的开出了一扇窗**（`SettingsWindow`）");
                if (sw != null && sw.CurrentState == WindowState.Open)
                {
                    var shade = sw.ShadeHit;                        // 本窗的压暗层命中区（`MenuDraw.ShadeHit` 建的）
                    var shadeBtn = shade != null ? shade.GetComponent<WindowButton>() : null;
                    CheckTrue(shadeBtn != null, "★ …那扇窗有**压暗层命中区**（`BackgroundHit` = 「点窗外关窗」）");
                    var over = pl.ButtonAt(gearX, gearY);
                    CheckTrue(over != null && shade != null && over.gameObject == shade.gameObject,
                              "★ 态②（**弹窗开着**）：**同一个点**吃到的**不再是顶栏齿轮**，而是那扇窗的**压暗层命中区**"
                              + "（实得 " + (over == null ? "<null>" : "`" + over.name + "`") + "）"
                              + " —— 这就是原版「弹窗在顶栏之上」（原版的渲染序 = 射线序，拆不开）");
                    CheckTrue(pl.ClickAt(gearX, gearY), "★ …而且点得动（真路径 `PointerLayer.ClickAt`）");
                    CheckTrue(sw.CurrentState == WindowState.Closed,
                              "★ 点在顶栏那一带 ⇒ **窗关了**（原版：那一点上就是本窗的「点窗外关窗」）");
                    // 收尾（幂等）：真的关掉了这句什么都不做 —— 万一上面那条红，别让它把后面几节一起带红
                    if (sw.CurrentState == WindowState.Open) sw.Close();
                }
            }
        }
        CheckAt(FindChild(FindChild(bar, "TopBarButtons"), "Image"), 425.3f, 480.3f, 15.5f, 55.5f,
                "`InboxBtn`（HLG 算的值，**不是 JSON 的 397.8**）");
        CheckAt(FindChild(FindChild(bar, "TopBarButtons"), "Challenge button"), 490.1f, 537.6f, 11.8f, 59.2f,
                "`Challenge button`（HLG 算的值）");
        CheckTrue(FindChild(bar, "Feedback Button") == null,
                  "`Feedback Button` **没建**（原版出厂 `activeSelf=False`，§二 表 #10）");
        var resBar = FindChild(bar, "Resources Bar");
        CheckTrue(resBar != null && resBar.GetComponentInChildren<ImageQuad>() == null,
                  "`Resources Bar` **没有背景图**（原版那一格没有 Image —— 我们第一版自加过一层浅灰药丸，已纠）");

        var prof = FindChild(bar, "Player Profile");
        CheckAt(FindChild(prof, "Background"), 23.0f, 411.0f, 11.6f, 135.6f, "玩家信息块底（`40k_main_player frame`）");
        CheckAt(FindChild(prof, "Player Name"), 136.9f, 401.9f, 13.7f, 61.7f, "`Player Name` 文字位");
        CheckAt(FindChild(prof, "Planer Name Background"), 25.6f, 472.1f, 14.9f, 60.5f, "名字条底");
        // 🆕 **2026-10-11（A219①）**：等级那一格原来建的节点名是 **`"Icon/Player Level"`** —— 那是**一个带
        //   斜杠的【字面】节点名**（Unity 里那**不是**一个名字、是**路径分隔符**：`transform.Find("Icon/Player Level")`
        //   会按两层路径走 ⇒ 永远取不到；而逐字比名字的 `FindChild` 又只在**整串相等**时才命中）。
        //   **原版判据（现读 `bundle_scenes_scenes_mainmenuwarpforge`，第一权威）**：
        //    · `GameObject/Player Level.json` 的 `m_Name` = **`Player Level`**（**不带任何前缀**）；
        //    · 它的 `RectTransform`（`RectTransform/RectTransform_1279.json`）`m_Father` = **1125** =
        //      `Player Profile` 那个 GO ⇒ 原版就**两级**（`Player Profile` → `Player Level`，中间**没有**
        //      `Icon` 那一层）—— 我们**也不缺层**，**只是名字写错了**；
        //    · 整包 `GameObject/` 的 `m_Name` 里**一个带 `/` 的都没有**。
        //   ⚠️ `资料/主菜单_原版规格.md` 的表里写的是 `Icon/Player Level` —— 那是 **dump 工具的显示串**
        //      （那个文件自己「换算时踩到的坑」第 3 条就写着「**名字一律不可信**」），**不是 prefab 里的名字**。
        //   **改坏法**：把 `New(p, "Player Level")` 写回 `New(p, "Icon/Player Level")` ⇒ 下面第一条红。
        {
            var lvlN = FindChild(prof, "Player Level");
            CheckTrue(lvlN != null,
                      "★★ A219① `Player Profile` **直接**挂着 `Player Level`（名字 = 原版 `m_Name` 的字面值）；"
                    + "写成带斜杠的 `Icon/Player Level` ⇒ 这里 null ⇒ 红");
            CheckTrue(FindChild(prof, "Icon/Player Level") == null,
                      "…而且**没有**那个带斜杠的字面节点名（原版整包 `GameObject/` 里没有一个 `m_Name` 含 `/`）");
            CheckAt(FindChild(lvlN, "Icon"), 117.5f, 170.6f, 54.3f, 107.4f,
                    "★ 等级圆底（`40k_topmarquee_currency_gold` · 53.12²）");
            CheckAt(FindChild(lvlN, "Player Level Text"), 124.3f, 163.7f, 61.1f, 100.5f, "`Player Level Text` 位");
        }
        CheckAt(FindChild(FindChild(prof, "Avatar Item Small"), "Border"), -10.0f, 165.5f, 9.0f, 139.1f,
                "头像金框（**§五 B 已把 scl 1.25 算进去**；`chain_rect` 给的是未缩放值）");
        // 🆕 2026-09-27：**顶栏那块头像立绘**（原版运行期由 `AvatarDisplay.ChangeAvatar` 赋图，
        //   出厂 `m_Sprite=0/m_Enabled=0` **不是设计**）—— 我们原来这一层根本没建，永远只有那面空盾。
        {
            var avNode = FindChild(prof, "Avatar Item Small");
            var topArt = FindChild(avNode, "Image");
            var topBd = FindChild(avNode, "Border");
            var aq = topArt != null ? topArt.GetComponentInChildren<ImageQuad>() : null;
            var bq = topBd != null ? topBd.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(aq != null && aq.Texture != null,
                      "★ 顶栏**头像立绘**建出来了（原版 `AvatarDisplay.avatarImage`；判据 → `资料/阶段二_多人界面_原版规格.md`）");
            CheckTrue(aq != null && bq != null && aq.RenderQueue > bq.RenderQueue,
                      "★ 顶栏立绘的渲染队列**比盾牌框高一档** —— 那张盾的中心是**不透明黑**，反了就是一块黑");
            CheckAt(topArt, -58.09f, 218.79f, -34.59f, 169.83f,
                    "顶栏立绘（**盒子比盾大，这是照原版 prefab 算的**：`Image` 的 `localScale=2` ⇒ 容器×2；"
                  + "立绘贴图实心部分只占 43%×60% ⇒ 露出来的只有人像。推导 → `BuildTopAvatar` 的注释）");
            // 🔴 **为什么立绘那一格要比盾大**（这条断言把那个理由钉住）：立绘贴图的**实心部分**只占
            //    **43%×60%**（`alpha>128` 的包围盒 220×306 / 512），而盾的**孔径**占边框的 **92%×93%**
            //    （近黑不透明区域的包围盒 236×266 / 256×286）⇒ **「实心的高」必须对得上「孔径的高」**，
            //    否则人像不是浮在一圈黑里（同格 ⇒ 矮 36%）、就是糊住整套框子。两个比例都是量出来的。
            {
                var aq2 = topArt != null ? topArt.GetComponentInChildren<ImageQuad>() : null;
                var bq2 = topBd != null ? topBd.GetComponentInChildren<ImageQuad>() : null;
                float artH = aq2 != null ? aq2.WorldH : 0f;
                float bdH = bq2 != null ? bq2.WorldH : 0f;
                float solidH = artH * 0.596f, holeH = bdH * 0.930f;
                // 🔴 **两格都必须【保宽高比】画**（原版 `m_PreserveAspect = 1`，13 个战场里 39/39 全是）——
                //    盾那一格我们原来按拉伸画（宽高比 1.349），**比实拍的 ≈107px 宽了 1.6 倍**。2026-09-27 修。
                CheckTrue(bq2 != null && Mathf.Abs(bq2.WorldW / bq2.WorldH - 256f / 286f) < 0.02f,
                          $"★ 盾那一格按**贴图宽高比**画（实得 {(bq2 == null ? 0f : bq2.WorldW / bq2.WorldH):F3}，"
                        + $"贴图 256/286 = {256f / 286f:F3}）—— 拉伸画会是 1.349，比原版实拍宽 1.6 倍");
                CheckTrue(Mathf.Abs(solidH - holeH) / holeH < 0.06f,
                          $"★ 立绘**实心部分**的高 ≈ 盾**孔径**的高（{solidH * 108f:F1}px vs {holeH * 108f:F1}px，"
                        + $"差 {100f * Mathf.Abs(solidH - holeH) / holeH:F1}%）—— 这一条就是「立绘为什么比盾大」的理由");
            }
            // 玩家在档案窗换了头像 ⇒ 这一层要跟着换（原版走 `PlayerAvatarDataManager.OnAvatarChanged`）
            int oldIdx = ProfileData.AvatarIndex;
            var before = aq != null ? aq.Texture : null;
            ProfileData.AvatarIndex = (oldIdx + 7) % Mathf.Max(1, ProfileData.Avatars.Count);
            menu.RefreshTopAvatarIfChanged();
            var after = aq != null ? aq.Texture : null;
            CheckTrue(after != null && after != before, "★ 换了头像 ⇒ 顶栏那一层**跟着换图**（不是一张死图）");
            ProfileData.AvatarIndex = oldIdx;
            menu.RefreshTopAvatarIfChanged();
            CheckTrue((aq != null ? aq.Texture : null) == before, "★ 换回来 ⇒ 又变回原来那张（`AvatarIndex` 是唯一一份状态）");
        }

        Section("右侧聊天预览（§五 D：1475..1875 × 85..145）");
        var chat = menu.Find("ChatPreview");
        CheckTrue(chat != null, "`ChatPreview` 建了");
        CheckAt(FindChild(chat, "Container"), 1474.7f, 1847.0f, 85f, 145f, "聊天底板（`Closed-Chat_background`）");
        CheckAt(FindChild(chat, "Button"), 1811.0f, 1879.0f, 81.8f, 148.3f, "聊天入口钮（`40K_icon_menu_chat`）");

        Section("模式卡区（§五 E 容器 + §九 卡结构）");
        var modes = menu.Find("GameModes");
        CheckTrue(modes != null, "`GameModes` 容器建了");
        var content = FindChild(modes, "Content");
        CheckTrue(content != null, "`Viewport/Content` 建了（原版出厂 0 子，靠 liveop 数据灌）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Tutorial", 205f, 535f, 414.4f, "Tutorial（1x1）");
        CheckCard(menu, "Draft Game Mode Container 1x2", 760f, 535f, 848.8f, "Draft（1x2）");

        // ============================================================ §A332 `sizeDelta`（2026-10-11 新增）
        //
        // 🔴 **这一节补的是 A218 的尾巴**：主菜单那个私有 `New`（`Shell/MainMenuRuntime.cs`）过去
        //    **只建节点、一个 `sizeDelta` 都没写**（`项目任务.md` 的 A218 那条点过名）⇒ 原版那些容器
        //    自己的矩形我们表达不出来 —— 与 A218 修掉的是**同一个缺陷**。A332 把它收口到公共件
        //    `MenuDraw.SetPxSize`（= A218 那一份换算，⛔ 不在别处再乘/除一次 108），并逐处断矩形宽高。
        //
        // 🔴 **期望值的来源**（⛔ 不是读被测实现、也不是读 `MainMenuRuntime` 自己那份常量）：
        //    2026-10-11 **现读** `bundle_scenes_scenes_mainmenuwarpforge` 里那一件自己的 `RectTransform`
        //    四个字段（`m_AnchorMin/Max` + `m_SizeDelta` + `m_Pivot` + `m_AnchoredPosition`），
        //    按整屏 1920×1080（左上原点、y 向下）逐级套出来的**绝对矩形**。
        //    **三处反证**（都与本文件里调用点原有的、2026-09 照**另一份表**写下的注释逐位吻合 ⇒ 这套取数可用）：
        //      `SettingsBtn` 1803.11,4.64→1890.89,66.36 · `Player Profile` 0,0→528.65,211.07 ·
        //      `ChatPreview` 1475,85→1875,145。
        //
        // 🔴 **两态可分辨**：同一个工厂、不同入参各一条 —— `Upper bar` **1920×100** vs `Player Level`
        //    **53.12²**（差三个数量级）；`Avatar Item Small` 138.42×139.568 vs `ChatPreview` 400×60
        //    （宽高比互为倒数）。下面每条都**分别断宽、断高** ⇒ 只断「`!= 0`」或只断一条的弱断言过不了。
        //
        // 🔴 **改坏法**：① `MainMenuRuntime.New` 里删掉 `MenuDraw.SetPxSize(...)`（或把工厂改回只建节点）
        //    ⇒ 下面**每一条**的 `rect.width/height` 都变 0 ⇒ 全红；② 逐处把一个调用点的两个实参改错
        //    ⇒ 只那一条红（这正是「每处都断」的意义）；③ 把 `GuessingWidth` 之类的常量抄到断言里当期望值
        //    ⇒ 工厂改坏也不会红（自证）—— 所以这里一律写**字面量 + 出处**。
        //
        // 🔴 **本工厂【只写尺寸、不写位置】**（与本文件其它 `New` 的用法不同，别照着改）：
        //    本文件所有器件都把**绝对**设计坐标当 `localPosition` 用（`Rect`/`Text`/`NavButton` 全走
        //    `Center(...)`），前提是**整棵树每个父级 `position` 恒零** ⇒ 把节点挪到矩形中心会让它
        //    整棵子树平移。判据 → `MainMenuRuntime.New` 的注释。所以这里**只断 `rect`，不断位置**。
        Section("§A332 主菜单：空节点工厂的 `sizeDelta`（`rect` 的宽高 = 原版矩形）");
        {
            System.Action<Transform, string, float, float, string> mbox = (t, what, wPx, hPx, src) =>
            {
                if (t == null) { CheckTrue(false, $"（前提）{what} 没建出来"); return; }
                var rt = t.GetComponent<RectTransform>();
                CheckTrue(rt != null, $"（前提）{what} 是 `RectTransform`（A92 那半）");
                if (rt == null) return;
                CheckNear(t.lossyScale.x, 1f, 1e-3f,
                          $"（前提·父链缩放）{what} 的 `lossyScale.x` = 1（= `rect` 与设计 px 同量纲那一档）");
                CheckNear(rt.rect.width, LayoutSpace.Px(wPx), 0.01f,
                          $"★ {what} 的 `rect.width` = **{rt.rect.width * 108f:F2}px**（原版 {wPx}px —— {src}）");
                CheckNear(rt.rect.height, LayoutSpace.Px(hPx), 0.01f,
                          $"★ …`rect.height` = **{rt.rect.height * 108f:F2}px**（原版 {hPx}px）");
            };
            mbox(menu.Find("Background"), "整屏背景 `Background`", 1920f, 1080f,
                 "原版 `MainMenu/Background`：`anchor (0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ 整屏矩形");
            mbox(nav, "左竖导航 `Navigation Panel`", 191f, 1080f,
                 "原版：`anchor (0,0)-(0,1)` + `sizeDelta (191,0)` ⇒ 191 × 1080（= §五 C 表那条「0..191」）");
            mbox(FindChild(nav, "Buttons Container"), "`Buttons Container`", 164.379f, 931.66f,
                 "原版：`sizeDelta (164.379, −148.34)` + 竖直 stretch ⇒ 高 = 1080 − 148.34");
            // 5 颗导航钮 **都断**（同一段代码、5 个调用点 —— 少断一颗就可能漏掉「只改对第一颗」那类错）
            var navNames = new[] { "Home", "Collection", "Shop", "Rewards", "Social" };
            for (int i = 0; i < navNames.Length; i++)
            {
                var nb = menu.Find("Main Menu Navigation Button - " + navNames[i]);
                mbox(nb, $"导航钮 `{navNames[i]}`", 164.31f, 169.68f,
                     "原版五颗 `Main Menu Navigation Button - *` 的 `m_SizeDelta` **逐值相同** = 164.31 × 169.68"
                     + "（⚠️ 与「选中态高亮」那一格 `−0.3..164.0` **不是同一个数**，别互推）");
                mbox(FindChild(nb, "Hit"), $"导航钮 `{navNames[i]}` 的命中区 `Hit`", 164f, 169.7f,
                     "⚠️ **我们自己的**（原版这 5 颗钮的子件是 `Selected highlight`/`Image`/`Text Background`/"
                     + "`Badge Highlight`，**没有 `Hit`** —— 原版靠钮自己挂的 `EverguildToggle` 收射线）"
                     + "；矩形 = 那颗透明 quad 那一格 `cx0±82` × `钮顶..钮底`（164 × 169.7）");
            }
            mbox(bar, "顶栏 `Upper bar`", 1920f, 100f,
                 "原版：`anchor (0,0)-(1,1)` + `sizeDelta.y −980` ⇒ 高 100（⚠️ **不是**下面子件 `Background` 那一格）");
            mbox(FindChild(bar, "SettingsBtn"), "顶栏齿轮 `SettingsBtn`", 87.78f, 61.73f,
                 "原版：`anchor (1,1)` + `sizeDelta (87.78, 61.73)`（= 齿轮那一格的框，与下面 `CheckAt` 同源）");
            mbox(FindChild(bar, "TopBarButtons"), "`TopBarButtons`", 311.4f, 71.33f,
                 "原版：`anchor (0,1)` + `sizeDelta (311.4, 71.33)`");
            mbox(FindChild(FindChild(bar, "TopBarButtons"), "InboxBtn"), "`InboxBtn`", 55f, 40f,
                 "原版：`sizeDelta (55,40)` —— 就是这个「框」（本文件上面那句注释里写的 55×40）");
            mbox(FindChild(bar, "Resources Bar"), "`Resources Bar`", 671.05f, 71.165f,
                 "原版：`anchor (1,1)` + `sizeDelta (671.05, 71.165)`");
            mbox(prof, "玩家信息块 `Player Profile`", 528.65f, 211.07f,
                 "原版：`anchor (0,1)` + `sizeDelta (528.65, 211.07)`（= 上面那行小字里的 0..528.7 / 0..211.1）");
            mbox(FindChild(prof, "Avatar Item Small"), "`Avatar Item Small`", 138.42f, 139.568f,
                 "原版：两个锚都重合 + `sizeDelta (138.42, 139.568)`");
            mbox(FindChild(prof, "Player Level"), "`Player Level`", 53.12f, 53.12f,
                 "原版：`sizeDelta (53.12, 53.12)` —— **正方形**（它里面 `Icon` 那一格 117.5..170.6 / 54.3..107.4"
                 + " 也是 53.1 见方，与此同值 —— 本文件已有的那条 `CheckAt` 用的就是这四个数）");
            mbox(chat, "聊天预览 `ChatPreview`", 400f, 60f,
                 "原版：`anchor (1,1)` + `sizeDelta (400,60)` ⇒ 绝对矩形 1475,85→1875,145");
            mbox(modes, "模式卡容器 `GameModes`", 1752.9606f, 1008.671f,
                 "原版：`anchor (0.087,0)-(1,0.934)` ⇒ 绝对矩形 167.04,71.33→1920,1080（0.087×1920 = 167.04）");
            mbox(FindChild(modes, "Viewport"), "`GameModes/Viewport`", 1752.9606f, 1008.671f,
                 "原版：`anchor (0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ 与父件 `GameModes` **同矩形**");
            mbox(content, "`GameModes/Viewport/Content`", 74f, 945.5651f,
                 "原版：`anchor.x` 重合 + `sizeDelta.x = 74`、`anchor.y` stretch 0.936 + `sizeDelta.y 1.449`"
                 + " ⇒ 74 × 945.5651 —— ⚠️ **宽只有 74 是原版 prefab 的模板位**（出厂 0 子，靠布局组长开），"
                 + "高 945.5651 就是本文件 `CardRow0Top` 那条注释里的「内容高 945.6」");
            {
                var tut = menu.Find("Base Game Mode Container 1x1 - Tutorial");
                mbox(tut, "模式卡 `Tutorial`", 535f, 414.4f,
                     "⚠️ **我们自己的**：原版 `Content` 出厂 0 子、卡是 liveop 运行时实例化的 ⇒ 没有可读的原版矩形");
                var prac = menu.Find("Base Game Mode Container 1x1 - Practice");
                mbox(prac, "模式卡 `Practice`", 535f, 414.4f, "同上（`CardW × CardH` = 535 × 414.4）");
                mbox(FindChild(prac, "Hit"), "模式卡 `Practice` 的命中区 `Hit`", 535f, 414.4f,
                     "⚠️ **我们自己的**：原版卡自己挂 `EverguildButton`、**没有** `Hit` 子件；矩形 = 卡那一格");
            }
        }

        // 🆕 2026-09-24「战斗入口」的三张模式卡 —— **用户拍板：模式卡就是入口**
        //    （「是直接点击这些卡片，然后就进去这些对应模式的界面的」）。
        //    🔴 原版这张「模式 → 卡图 → 窗」的映射在 **liveop 服务端**（本地查不到、正本也写着「别自己编」）
        //    ⇒ **这三张 + 它们点开哪扇窗，都是我们定的**（逐条记在 `资料/阶段二_战斗入口_原版规格.md` §〇/§五）。
        Section("战斗入口：三张模式卡（**入口是我们定的**，见 `资料/阶段二_战斗入口_原版规格.md`）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Practice", 205f + 2 * 555f, 535f, 414.4f, "Practice（1x1）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Skirmish", 205f + 3 * 555f, 535f, 414.4f, "Skirmish（1x1）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Ranked", 205f + 4 * 555f, 535f, 414.4f, "Ranked（1x1）");
        {
            var pc = menu.Find("Base Game Mode Container 1x1 - Practice");
            var ph = FindChild(pc, "Hit");
            var pwb = ph != null ? ph.GetComponent<WindowButton>() : null;
            CheckTrue(pwb != null && pwb.onClick != null, "练习卡有**点击区**（`WindowButton`）");
            CheckTrue(FindChild(menu.Find("Base Game Mode Container 1x1 - Tutorial"), "Hit") == null,
                      "Tutorial 卡**没有**点击区（它还不是入口 —— 那一扇窗还没建）");

            // ============================================================ 🆕 2026-10-10（A222）
            // **模式卡三张的悬停色偏也要打在那颗可见的图形上**（`Background Image` = 原版 `m_TargetGraphic`）。
            // 🔴 **判据（原版 prefab 现读，不是推测）**：`bundle_menus_assets_all` 的
            //    `Base Game Mode Container 1x1` 根上挂 `EverguildButton`（MB `4126037038486929469`）：
            //      `m_Transition = 1`(ColorTint) · **`m_TargetGraphic = 7827625613328400445`** —— 逐跳解出来
            //      = **`Background Image` 那颗 GO 上的 `UIParallaxImage`**（`Image` 的子类 ⇒ 它就是那格的 `Graphic`）；
            //      `m_Colors` 四格 = **全库默认那一组**（`HL/SEL` 0.9607843 · `PR/DIS` 0.7843137 · `NOR` 白）
            //      ⇒ 这三张**不用**覆盖 `HighlightKey`（与导航钮那五颗不同 —— 逐条实读，⛔ 别互推）。
            // 🔴 **改坏法**：`BuildModeCard` 里删掉 `wb.TintOn(cardArtQ)` ⇒ 色偏落在那颗透明 `Hit` 上、
            //    卡图纹丝不动 ⇒ 下面三条红。
            //    改断成「另建的某颗图形」也不行：量的是 `Background Image` 这一颗（原版指的就是它）。
            {
                var modeCases = new[] { "Practice", "Skirmish", "Ranked" };
                for (int mi = 0; mi < modeCases.Length; mi++)
                {
                    var card = menu.Find("Base Game Mode Container 1x1 - " + modeCases[mi]);
                    var artN = FindChild(card, "Background Image");
                    var hitN = FindChild(card, "Hit");
                    var mwb = hitN != null ? hitN.GetComponent<WindowButton>() : null;
                    var mq = artN != null ? artN.GetComponent<ImageQuad>() : null;
                    CheckHoverTint(mwb, mq, new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
                                   $"★ 模式卡 `{modeCases[mi]}` 悬停色偏打在 `Background Image` 上"
                                   + "（原版 `m_TargetGraphic` + 默认灰 0.9607843）");
                }
            }
            // ⚠️ **别碰玩家的真存档**：`DeckStore.OverridePath` 先指到临时文件（同 `CollectionScene` 的规矩）
            DeckStore.OverridePath = "d:/4/_tmp_view/menu/_menu_test_decks.json";
            try { System.IO.File.Delete(DeckStore.OverridePath); } catch { }
            CollectionData.ResetForTest();
            {
                var lib = DeckLibrary.Load();
                for (int i = 0; i < 3; i++) lib.Create("菜单测试卡组 " + (i + 1));
                // 给第 0 套塞督军 + 几张部队（`Battle!` 没有督军会**如实拒绝**，那样验不出开战那条）
                var pool = CardDatabase.Load();
                var d0 = lib.Decks[0];
                foreach (var c in pool) if (c.Type == "hero") { d0.WarlordId = c.Id; break; }
                int added = 0;
                foreach (var c in pool) if (c.Type == "unit" && added < 4) { d0.CardIds.Add(c.Id); added++; }
                // 🆕 2026-10-04（§三第29条 A65④①）：再建一副**合法**的经典卡组（30 张同阵营 unit、**一张一名**）。
                //   为什么必须有它：`Practice Deck` 的 `interactable = DeckUtility.ValidateDeck(context.Deck)`
                //   那一条在**两个夹具里全是假**（第 0 套只有 5 张卡）⇒ 没有这一副，那条断言只能是「恒 false」，
                //   分不出「按判据算出来的」与「写死的 `false`」（本工程第 N 次栽在弱断言上）。
                {
                    // 阵营取卡池里 unit 最多的那个（实测 `RuleEngine/Resources/cards_engine.json`：Ultramarines **69** 张）。
                    // ⚠️ 这里是**写死的**、不现算「哪个阵营够 30」—— 那种现算等于把判据搬进自检。
                    // 真凑不满 30 张时下面那条 `Check` 会**红**（不静默变绿）。
                    const string LegalFaction = "Ultramarines";
                    var legal = lib.Create("菜单测试卡组 合法30");
                    foreach (var c in pool)
                        if (c.Type == "hero" && RuleEngine.DeckRules.SameFaction(c.Faction, LegalFaction))
                        { legal.WarlordId = c.Id; break; }
                    foreach (var c in pool)
                    {
                        if (legal.CardIds.Count >= 30) break;
                        if (c.Type != "unit") continue;
                        if (!RuleEngine.DeckRules.SameFaction(c.Faction, LegalFaction)) continue;
                        if (legal.CardIds.Contains(c.Id)) continue;   // 一张一名 ⇒ 不碰同名上限
                        legal.CardIds.Add(c.Id);
                    }
                    Check(legal.CardIds.Count, 30,
                          "自检夹具：建出一副**合法**的 30 张卡组（原版 `GameStaticData.deckSize = 30`）"
                          + " —— A65④① 的阳性对照，缺了它那条断言等于没查");
                }
                lib.Save();
            }
            CollectionData.ResetForTest();
            // 主菜单自检原来**没有 `WindowsManager`**（主菜单原版也是挂在壳里跑的）——
            // 战斗入口这条路要开窗 ⇒ 这里补一个宿主（`EnsureHost` 会连**指针层**一起建好）
            WindowsManager.EnsureHost(menu.transform);
            if (pwb != null) pwb.Click();
            var pw = PracticeModePopup.LastOpened;
            CheckTrue(pw != null, "点练习卡 ⇒ **开出了 `Practice Mode Menu`**");
            if (pw != null)
            {
                // 🆕 A47：压暗层命中区 —— 档 = `QPr`(3100)（压暗层自己那一档），< 内容命中区档 `QPrHit`(3108)
                //    ⚠️ `QPrHit` 2026-10-07（A77-㉒）从 **3103** 挪到 **3108**（卡组格内景要五层各一个队列）——
                //      这里按常量名引用，值变了自动跟上；**只有这行注释里的字面量要跟着订正**。
                //    🔴 **2026-10-07（A77⑬③）`QPr` 那个实参没了** —— 期望值改成**量**本窗那块
                //      `Menu Dark Background`（视觉压暗层）的 quad 档（它是窗口自己那句 `Solid(...)` 建的）。
                MenuDraw.CheckShadeRule(CheckTrue, "练习窗", FindChild(pw.transform, "BackdropHit"),
                                        pw.transform.Find("Menu Dark Background"), PracticeModePopup.QPrHit);
                Check(pw.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                Check(pw.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                CheckNear(pw.extraScaleSmallScreen, 1.07f, 1e-4f,
                          "`extraScaleSmallScreen` = **1.07**（⚠️ 别的窗多是 1.0 —— **逐窗实测**）");
                CheckAt(FindChild(pw.transform, "Deck info"), 638.38f, 1842.38f, 78.79f, 863.77f, "`Deck info` 容器");
                // 🔴 **2026-09-24 结构订正（`项目任务.md` §三 第 53 条）** —— 从根节点实读出来的真结构，逐条断：
                //    ① 红底在**子件** `Generic Window Red Background Big` 上（`Deck info` 自己**没有图**）
                //    ② `Background Info` 是**【空容器】+ act=0**（正本把好几件挂在它下面是错的）
                //    ③ `Deck Name`/`Warlord Name`/`Army Image`/`General container`/`Deck List Drawer` 直挂 `Deck info`
                {
                    var info = FindChild(pw.transform, "Deck info");
                    var bgBig = FindChild(info, "Generic Window Red Background Big");
                    CheckAtWorld(bgBig, 761.63f, 1831.93f, 187.00f, 869.00f,
                                 "红底在 `Generic Window Red Background Big` 上（761.63,187.00→1831.93,869.00 —— **不是** `Deck info` 那整块 1204×784.98）");
                    CheckTrue(bgBig != null && bgBig.GetComponentInChildren<ImageQuad>() != null,
                              "红底那一层**真的画出来了**（有 `ImageQuad`，不是空节点）");

                    var bgInfo = FindChild(info, "Background Info");
                    CheckTrue(bgInfo != null, "`Background Info` 建了（照原版留着）");
                    CheckTrue(bgInfo != null && !bgInfo.gameObject.activeSelf,
                              "`Background Info` **出厂 act=0**（照原版）");
                    Check(bgInfo != null ? bgInfo.childCount : -1, 0,
                          "`Background Info` 是**【空容器】**（实读一条子件都没有 —— 它**不是** `Warlord Name` 那几件的父）");

                    foreach (var nm in new[] { "Deck Name", "Warlord Name", "Army Image",
                                               "General container", "Deck List Drawer" })
                    {
                        var t = FindChild(info, nm);
                        CheckTrue(t != null && t.parent == info, $"`{nm}` 是 **`Deck info` 的直接子件**");
                    }
                    var gen0 = FindChild(info, "General container");
                    var lore = FindChild(info, "Lore Text");
                    var chgT = FindChild(info, "Change Deck Text");
                    CheckTrue(lore == null,
                              "`Lore Text` **没画**（原版喂的是 `DemoDeckInfoSO.Lore` = **卡组**简介；我们的卡组没这个字段，"
                              + "拿督军的效果文字顶上去会**和 `Change Deck` 叠** —— 见 `BuildGeneralContainer` 的注释）");
                    CheckTrue(chgT != null && gen0 != null && chgT.parent == gen0,
                              "`Change Deck` 挂在 **`General container`** 下（不是 `Background Info`）");

                    // 🆕 2026-10-03（§三第29条 A14）：费用曲线那两件**补上了**
                    //   判据 → `资料/阶段二_战斗入口_原版规格.md` §二 A 那张表；画法与 `Deck info Popup` 共用 `CostCurveDrawer`
                    // ⚠️ 必须用 `CheckAtWorld` —— `Cost/balance text` 挂在 `General container` 下（不是窗口根的直接子件），
                    //    而 `CheckAt` 比的是 **`localPosition`**（只对直接子件成立）⇒ 第一版差 565.73px。
                    CheckAtWorld(FindChild(gen0, "Deck Information Cost/balance text"),
                                 1276.27f, 1540.27f, 326.95f, 381.27f, "`Cost/balance text`（fs34 auto[1-34] · 居中）");
                    CheckText(TextOf(FindChild(gen0, "Deck Information Cost/balance text")), "Card / Energy cost",
                              "…文案 = 原版的 `Card / Energy cost`");
                    Check(pw.CostRows.Count, 9, "费用曲线 **9 行**（费用 0..8）");
                    int csum = 0; for (int k2 = 0; k2 < pw.CostRows.Count; k2++) csum += pw.CostRows[k2];
                    CheckTrue(csum >= 0, $"…读数：{CostCurveDrawer.Dump(pw.CostRows.ToArray())}");
                    var cdr = FindChild(gen0, "Deck CostQuanityt Row Drawer");
                    CheckTrue(cdr != null, "第 1 行建了（原版把行名拼错成 `Quanityt`，**照抄别改**）");
                    if (cdr != null)
                    {
                        // 抽屉中心 (1408.275, 504.71)、scl 1.2 ⇒ 第 0 行行心 y = 504.71 + (−90.045×1.2) = 396.656
                        // 行 223.59×18.91 ×1.2 = 268.308 × 22.692
                        CheckAtWorld(cdr, 1274.12f, 1542.43f, 385.31f, 408.00f,
                                     "第 0 行（中心 **1408.28, 396.66** = 抽屉中心 − 90.045×**1.2** 那一档缩放）");
                        var cc = FindChild(cdr, "Card Cost");
                        CheckTrue(cc != null && FindChild(cdr, "Cards in deck") != null,
                                  "行里有 `Card Cost` + `Cards in deck` 两段字");
                        CheckTrue(FindChild(cdr, "Background") != null, "…还有滑块底 `40k_CardAmount_bar_bg`");
                    }
                }
                // **出厂态 = 总览**：原版 `DeckGeneralInfoDemo.SetContent` 末尾
                //   `generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`
                //   （`PracticeModePopup__DeckSelected.c` 是开窗/换卡组的必经之路）。
                {
                    var gen = FindChild(pw.transform, "General container");
                    var dl = FindChild(pw.transform, "Deck List Drawer");
                    CheckTrue(pw.ShowingGeneralInfo, "出厂显示的是**总览**（`General container` 开着）");
                    CheckTrue(gen != null && gen.gameObject.activeSelf && dl != null && !dl.gameObject.activeSelf,
                              "两个抽屉**互斥**：`General container` 开、`Deck List Drawer` 关（`SetContent` 那两句）");
                }
                Check(PracticeModePopup.ListCols, 2,
                      "卡列表列数 = **2** = floor((526.10 − 22 + 22) ÷ 253)（原版 `GridLayoutGroup`：`constraint=0/Flexible` ⇒ 按宽算）");
                CheckAt(FindChild(pw.transform, "Army Selector"), 69.42f, 246.54f, 182.18f, 880.17f, "`Army Selector`");
                CheckAt(FindChild(pw.transform, "Decks Scroll view"), 261.28f, 634.88f, 262.64f, 803.43f,
                        "`Decks Scroll view`");
                CheckAt(FindChild(pw.transform, "Back Bg"), 215.76f, 280f, 892.86f, 956.10f, "`Back` 圆钮");
                CheckText(TextOf(FindChild(pw.transform, "Battle Text")), "Battle!", "开战钮文案 = `Battle!`");
                CheckText(TextOf(FindChild(pw.transform, "tooltip")), "Select deck to play", "`tooltip` 文案");
                CheckText(TextOf(FindChild(pw.transform, "Toggle Label")), "Game mode", "`Game mode` 开关文案");
                CheckTrue(pw.DeckRows.Count > 0, $"卡组列表画了 {pw.DeckRows.Count} 格（原版是**两列**网格）");
                CheckTrue(pw.ArmyCells.Count > 0, $"阵营列画了 {pw.ArmyCells.Count} 格（13 个阵营，可纵向滚）");

                // ---------------- 🆕 2026-10-05（本轮）：两处**版式偏离**照原版改 ----------------
                // 判据（我这一轮自己重跑的，两处同一条命令）：
                //   `python 工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu" --depth 6 --md`
                // ① `Army Selector/Viewport`（挂 `RectMask2D` 的那一件）= **69.42,149.07→246.54,913.28**
                //    —— 比父件 `Army Selector`（182.18→880.17）**高 66.22px、上下各探出 33.11**。
                //    原来我们把**父件**那个矩形当视口 ⇒ 裁切/滚动范围少 66px、第一格下移 33.08px。
                // ② 同件里的格容器 `Filters` 是 `GridLayoutGroup`：`cellSize 82×82` · `spacing (0,26.38)` · `pad 0`
                //    · `m_ChildAlignment = 4`（横轴那半 = 居中）⇒ 第 1 列 = **116.98..198.98**
                //    （原来靠左 69.42..151.42，差 **47.56px**）；第一格顶 = 视口上沿 **149.07**（原来是 182.18）。
                // ③ `Decks Scroll view/Viewport/Content` 也是 `GridLayoutGroup`：`cellSize 180×180` ·
                //    `spacing (0,−21)` · `pad (4,0,0,0)` · `m_ChildAlignment = 1` · `m_Constraint = 0 (Flexible)`
                //    ⇒ 按 `Content` 宽 **372.04** 现算 = **2 列**、第 1 列 **270.08..450.08**、行距 **159**
                //    （我们原来是**单列 38px 行** —— 那句「原版没给格尺寸」是**错的**，已在 `PracticeModePopup` 就地更正）。
                // 🔴 **真红法**：`ArmyCellL` 改回 `ArmL` / `ArmVp*` 改回 `Arm*` / `DeckCols` 写死 1 /
                //    `DeckCellW·DeckCellH` 改回 38 —— 下面每一组立刻红。
                // ⚠️ 期望值全是**原版实读的字面量**（⛔ 不从 `PracticeModePopup` 的常量里读，否则常量一改这条跟着改 = 自证）。
                Section("练习窗两处版式照原版改（A2/A3）：`Army Selector` 的视口与格位 · `Decks Scroll view` 的两列网格");
                {
                    var armSel = FindChild(pw.transform, "Army Selector");
                    var aVp = FindChild(armSel, "Viewport");
                    var aFilters = FindChild(aVp, "Filters");
                    var deckSel = FindChild(pw.transform, "Decks Scroll view");
                    var dVp = FindChild(deckSel, "Viewport");
                    var dContent = FindChild(dVp, "Content");
                    CheckTrue(aVp != null && aFilters != null && dVp != null && dContent != null,
                              "原版那两层节点都建了（`Army Selector/Viewport/Filters` · `Decks Scroll view/Viewport/Content`）"
                              + " —— 原来格是直接挂在 `Army Selector` / `Decks Scroll view` 下面的");
                    CheckAtWorld(aVp, 69.42f, 246.54f, 149.07f, 913.28f,
                                 "★ `Army Selector/Viewport` = **69.42,149.07→246.54,913.28**（比父件高 66.22 —— 视口不是父件）");
                    CheckAtWorld(aFilters, 69.42f, 246.54f, 149.07f, 913.28f, "…`Filters` 与 `Viewport` 同矩形");
                    CheckAtWorld(dVp, 261.28f, 634.88f, 262.64f, 803.43f,
                                 "★ `Decks Scroll view/Viewport`（这一处**与父件同矩形**）"
                                 + " —— 与上面那处**正好相反**，两个视口逐处实读、⛔ 别互推");
                    CheckAtWorld(dContent, 262.06f, 634.10f, 262.64f, 262.64f,
                                 "…`Content` 出厂是**零高**的一行（原版 prefab 就是这样；高由 `ContentSizeFitter` 撑）");
                    Check(PracticeModePopup.DeckCols, 2,
                          "`Decks Scroll view` 的列数（照原版 `Content` 宽 372.04 · pad 4 · 格 180 现算）= **2**"
                          + "（原版 `m_ConstraintCount` 序列化也是 2，但 `m_Constraint = 0 Flexible` ⇒ 那个数不参与）");

                    // ---- 格位：量的都是**渲出来的矩形**（`Hit` 里那个 `ImageQuad`），不是源码常量 ----
                    // ⚠️ 两个 `if` 各配一条「不足就红」—— ⛔ 别让「格不够 ⇒ 静默跳过 ⇒ 全绿」蒙混过去。
                    if (pw.ArmyCells.Count >= 2)
                    {
                        CheckCellRect(pw.ArmyCells[0], 116.98f, 149.07f, 198.98f, 231.07f, 0.6f,
                                      "★ 阵营第 1 格 = **116.98,149.07→198.98,231.07**（横向居中 + 贴视口上沿）");
                        CheckCellRect(pw.ArmyCells[1], 116.98f, 257.45f, 198.98f, 339.45f, 0.6f,
                                      "★ 阵营第 2 格 = 上一格 y 加 **108.38**（82 + spacing 26.38）");
                    }
                    else CheckTrue(false, "阵营格只有 " + pw.ArmyCells.Count + " 个 ⇒ 上面那两条格位断言等于没查");
                    if (pw.DeckRows.Count >= 3)
                    {
                        CheckCellRect(pw.DeckRows[0], 270.08f, 262.64f, 450.08f, 442.64f, 0.6f,
                                      "★ 卡组第 1 格 = **270.08,262.64→450.08,442.64**（180×180 · 第 1 列）");
                        CheckCellRect(pw.DeckRows[1], 450.08f, 262.64f, 630.08f, 442.64f, 0.6f,
                                      "★ 卡组第 2 格 = **第 2 列**同一行（x 正好差 180 —— 单列的实现在这里红）");
                        CheckCellRect(pw.DeckRows[2], 270.08f, 421.64f, 450.08f, 601.64f, 0.6f,
                                      "★ 卡组第 3 格 = 回到第 1 列、下一行（y 差 **159** = 180 − 21）");
                    }
                    else CheckTrue(false, "卡组格只有 " + pw.DeckRows.Count + " 个 ⇒ 上面那三条格位断言等于没查");
                }

                // ============================================================ 🆕 2026-10-10（A178）
                // **阵营格 = 原版 item prefab `Practice Army Select Button`**（类 `PracticeArmySelectionButton`
                // + `EverguildButton`），**四个子件**：`Highlight` / `Background` / `Icon` / `Has Player Deck`。
                // 我们原来**只建了 `Icon` 一层**（+ 命中区 `Hit`）⇒ 整件漏建三层。
                // 判据链（可复查）→ `资料/普查产出_1009/查证V1_原版prefab四件.md` §二 +
                //   `python 工具/menu_dump.py bundle_menus_assets_all --rt 8940217906067473431 --root-size 82x82 --depth 3 --md`
                // 四层的格内矩形（**格 = 82²**，坐标相对格左上角）：
                //   `Highlight`      −6.83,−6.83 → 88.83,88.83（**95.65²，四周各外扩 6.83、伸出格框** —— 原版如此）
                //   `Background`     0,0 → 82,82          · `Icon` 同矩形（图是运行期喂的）
                //   `Has Player Deck` 54,45 → 81,102.25（**27.00×57.24**，右下那颗火漆印，下沿探出格 20.25px）
                // 图：`Highlight` = `UI_Deck_button_click`(169²) · `Background` = `UI_Button_Round_background`(237²)
                //     · `Has Player Deck` = 原版 sprite `Purity Seal_02`(128×256) ⇒ 落盘名 `Purity_Seal_02`
                //     （导入器 `MENU_IMAGES` 的规矩：**空格换下划线**；`CardArt.MenuUi` 自己不换算）。
                // 🔴 **期望值一律原版字面量**（⛔ 不读 `PracticeModePopup.Army*` 那些常量 —— 那是自证）。
                // 🔴 **改坏法**：`RebuildArmyCells` 退回「只建 `Icon`」⇒ ①②③ 三组全红；
                //    `Highlight` 内缩成 82² ⇒ ① 红；把 `Has Player Deck` 摆到格中央 ⇒ ① 红；
                //    三层共用同一个渲染队列 ⇒ ④ 红（同队列「谁盖谁」不可控）。
                //    ⚠️ `Highlight` 那一条量的是**第 2 格**：第 1 格那颗的上沿（149.07 − 6.83 = 142.24）
                //    **在视口之上**，原版 `RectMask2D` 就把它裁掉一条（我们照裁）⇒ 量第 1 格会得到截后值。
                Section("★ 阵营格四层（A178：`Highlight` / `Background` / `Icon` / `Has Player Deck`）");
                {
                    if (pw.ArmyCells.Count >= 2)
                    {
                        // ---- ① 节点 / 层级 / 矩形 ----
                        for (int k = 0; k < 2; k++)
                        {
                            var cell = pw.ArmyCells[k];
                            var hlN = FindChild(cell, "Highlight");
                            var bgN = FindChild(cell, "Background");
                            var icN = FindChild(cell, "Icon");
                            var seN = FindChild(cell, "Has Player Deck");
                            CheckTrue(hlN != null && bgN != null && icN != null && seN != null,
                                      $"★ 阵营第 {k + 1} 格底下**四个子件都在**"
                                      + "（`Highlight` / `Background` / `Icon` / `Has Player Deck` —— "
                                      + "原来只有 `Icon` 一层）");
                            if (hlN == null || bgN == null || icN == null || seN == null)
                            { CheckTrue(false, $"…第 {k + 1} 格缺件 ⇒ 下面的矩形/层级断言没查成"); continue; }
                            CheckTrue(hlN.parent == cell && bgN.parent == cell && icN.parent == cell && seN.parent == cell,
                                      $"★ 阵营第 {k + 1} 格：四层都是**格的直接子件**（原版那四颗都挂在 item 根下）");
                            CheckTrue(hlN.GetSiblingIndex() < bgN.GetSiblingIndex()
                                      && bgN.GetSiblingIndex() < icN.GetSiblingIndex()
                                      && icN.GetSiblingIndex() < seN.GetSiblingIndex(),
                                      $"★ 阵营第 {k + 1} 格：兄弟序照原版 **`Highlight` → `Background` → `Icon` → `Has Player Deck`**"
                                      + $"（实测 {hlN.GetSiblingIndex()}/{bgN.GetSiblingIndex()}/{icN.GetSiblingIndex()}/{seN.GetSiblingIndex()}）");
                            // ② 图名（走 `QuadOfInactiveToo`：`Has Player Deck` 出厂是**关着的**）
                            CheckText(QuadOfInactiveToo(hlN) != null && QuadOfInactiveToo(hlN).Texture != null
                                      ? QuadOfInactiveToo(hlN).Texture.name : "<无图>", "UI_Deck_button_click",
                                      $"★ 阵营第 {k + 1} 格 `Highlight` 的图 = 原版 `UI_Deck_button_click`");
                            CheckText(QuadOfInactiveToo(bgN) != null && QuadOfInactiveToo(bgN).Texture != null
                                      ? QuadOfInactiveToo(bgN).Texture.name : "<无图>", "UI_Button_Round_background",
                                      $"★ 阵营第 {k + 1} 格 `Background` 的图 = 原版 `UI_Button_Round_background`");
                            CheckText(QuadOfInactiveToo(seN) != null && QuadOfInactiveToo(seN).Texture != null
                                      ? QuadOfInactiveToo(seN).Texture.name : "<无图>", "Purity_Seal_02",
                                      $"★ 阵营第 {k + 1} 格 `Has Player Deck` 的图 = 原版 `Purity Seal_02`"
                                      + "（落盘名 `Purity_Seal_02`）");
                            CheckTrue(QuadOfInactiveToo(icN) != null && QuadOfInactiveToo(icN).Texture != null,
                                      $"★ 阵营第 {k + 1} 格 `Icon` 有图（运行期按阵营喂）");
                            // ④ 四层各一个渲染队列、且**严格从下到上**（同队列「谁盖谁」不可控）
                            int qHl = QuadOfInactiveToo(hlN) != null ? QuadOfInactiveToo(hlN).RenderQueue : -1;
                            int qBg = QuadOfInactiveToo(bgN) != null ? QuadOfInactiveToo(bgN).RenderQueue : -1;
                            int qIc = QuadOfInactiveToo(icN) != null ? QuadOfInactiveToo(icN).RenderQueue : -1;
                            int qSe = QuadOfInactiveToo(seN) != null ? QuadOfInactiveToo(seN).RenderQueue : -1;
                            CheckTrue(qHl < qBg && qBg < qIc && qIc < qSe,
                                      $"★ 阵营第 {k + 1} 格：四层队列**严格递增**（`Highlight` < `Background` < `Icon` < `Has Player Deck`）"
                                      + $"（实测 {qHl} < {qBg} < {qIc} < {qSe}）");
                        }
                        // ---- ③ 矩形（第 2 格：整格含外扩的 `Highlight` 都完整落在视口里）----
                        var c1 = pw.ArmyCells[1];      // 格 = 116.98,257.45 → 198.98,339.45（同上面 A2/A3 那条）
                        CheckQuadRectPx(QuadOfInactiveToo(FindChild(c1, "Highlight")),
                                        110.15f, 205.81f, 250.62f, 346.28f, 0.6f,
                                        "★ `Highlight` = **95.65²**、四周各外扩 **6.83**（−6.83→88.83；⛔ 别内缩成 82²）");
                        CheckQuadRectPx(QuadOfInactiveToo(FindChild(c1, "Background")),
                                        116.98f, 198.98f, 257.45f, 339.45f, 0.6f,
                                        "★ `Background` = **格那一格**（0,0→82,82）");
                        CheckQuadRectPx(QuadOfInactiveToo(FindChild(c1, "Has Player Deck")),
                                        170.98f, 197.98f, 302.45f, 359.70f, 0.6f,
                                        "★ `Has Player Deck` = **27.00×57.24** @ (54.00,45.00)（火漆印，下沿探出格 20.25px）");

                        // ---- ⑤ 显隐（**原版判据**，不是我们挑的 —— 见 `RebuildArmyCells` 那段注释）----
                        // ① `Highlight` 亮 ⟺ 这一格就是当前选中的阵营（`PracticeModePopup__ChangeSelectedArmy.c` 循环里
                        //    `SetActive(btn+0x28, btn.army == param_2)`）；
                        // ② `Has Player Deck` 亮 ⟺ 有选中卡组、且它的阵营 == 这一格的阵营（同一个循环里那三句）。
                        int savedArmy = pw.ArmyIndex, savedDeck = pw.DeckIndex;
                        float savedOff = pw.ArmyScroll.Offset;
                        Check(ArmyLit(pw, "Highlight").Count, 0,
                              "`ArmyIndex = -1`（本窗多出来的「不限」档）⇒ `Highlight` **一格都不亮**");
                        pw.ArmyIndex = 2; pw.ArmyScroll.OnChanged();
                        var hlLit = ArmyLit(pw, "Highlight");
                        Check(hlLit.Count, 1, "把当前阵营指到第 3 格 ⇒ `Highlight` **恰好亮一格**"
                              + "（一层都没建时恒 0 ⇒ 这条分得出两种状态）");
                        if (hlLit.Count == 1)
                            Check(hlLit[0], 2, "…而且亮的正是那一格");
                        pw.ArmyIndex = -1; pw.ArmyScroll.OnChanged();

                        // `Has Player Deck`：挑一副**有阵营**的卡组，把它那一格滚进视口再量
                        var facsA = CollectionWindow.CardsState.Factions();
                        int wantIdx = -1, wantDeck = -1;
                        for (int d = 0; d < CollectionData.DeckCount() && wantIdx < 0; d++)
                        {
                            string f = CollectionData.DeckAt(d).Faction;
                            int kk = string.IsNullOrEmpty(f) || facsA == null ? -1 : facsA.IndexOf(f);
                            if (kk >= 0) { wantIdx = kk; wantDeck = d; }
                        }
                        if (wantIdx < 0)
                            CheckTrue(false, "（前提）夹具里没有「有阵营的卡组」⇒ `Has Player Deck` 那条**没查成**");
                        else
                        {
                            pw.DeckIndex = wantDeck;
                            pw.ArmyScroll.Offset = wantIdx * 108.38f;   // 该格顶对齐视口上沿（可滚极值由 `MenuScroll` 管）
                            pw.ArmyScroll.OnChanged();
                            int i0 = Mathf.CeilToInt((pw.ArmyScroll.Offset - 82f) / 108.38f);
                            if (i0 < 0) i0 = 0;
                            int k = wantIdx - i0;
                            CheckTrue(k >= 0 && k < pw.ArmyCells.Count,
                                      $"（前提）滚到偏移 {pw.ArmyScroll.Offset:F2} 后，卡组阵营那一格（阵营下标 {wantIdx}）"
                                      + $"在建出来的 {pw.ArmyCells.Count} 格（i0={i0}）里");
                            if (k >= 0 && k < pw.ArmyCells.Count)
                            {
                                var seLit = ArmyLit(pw, "Has Player Deck");
                                Check(seLit.Count, 1, "`Has Player Deck` **恰好亮一格**（= 选中卡组的阵营那一格；"
                                      + "恒亮 ⇒ 8、恒灭 ⇒ 0，这条分得出两种状态）");
                                if (seLit.Count == 1) Check(seLit[0], k, "…而且亮的正是卡组阵营那一格");
                            }
                        }
                        pw.ArmyIndex = savedArmy; pw.DeckIndex = savedDeck;
                        pw.ArmyScroll.Offset = savedOff;
                        pw.ArmyScroll.OnChanged();      // 还原：后面的夹具/断言看到的还是刚开窗那一态
                    }
                    else CheckTrue(false, "阵营格只有 " + pw.ArmyCells.Count + " 个 ⇒ 上面那些断言等于没查");
                }

                // ============================================================ 🆕 2026-10-07（A77-㉒①）
                // **`Army Selector/Background` 那颗底图** —— 原版有、我们**原来整层没建**（账挂在 A77-㉒①）。
                // 判据（同一次 `menu_dump.py` 现读，逐条）：
                //   `Practice Mode Menu/Deck Selector/Army Selector`
                //     ├ **`Background`**  ← **第一颗子件**（排在 `Viewport` 前 ⇒ 画在格子**下面**）
                //     │    `Image` · sprite **`UI_Background faction buttons`**（54×420 · 九宫 `2,201,2,202` · `Sliced`）
                //     │    rect = **父件那一格**（69.42,182.18→246.54,880.17）—— ⛔ 不是 `Viewport` 那格（它上下各探出 33.11）
                //     └ `Viewport`(69.42,149.07→246.54,913.28 · `RectMask2D`) → `Filters` → 13 格
                // 🔴 **为什么「直接子件」这一条必须断**：底图若是挂在 `Viewport` 下面，就会被 `RectMask2D`
                //    **上下各裁掉 33px**（而原版它是兄弟件、**不被裁**）—— 两边的土黄色边条会少一截。
                // 🔴 **真红法**：把 `BuildArmySelector` 里那颗 `Nine(..., "Background")` 删掉 ⇒ 下面 6 条全红；
                //    改挂到 `Viewport` 下 ⇒ 「直接子件」那条红；`ArmL/ArmT/ArmR/ArmB` 换成 `ArmVp*` ⇒ 四条边全红。
                {
                    var armSelB = FindChild(pw.transform, "Army Selector");
                    var aVpB = FindChild(armSelB, "Viewport");
                    var bgN = FindChild(armSelB, "Background");
                    CheckTrue(bgN != null,
                              "★ `Army Selector/Background` **建了**（原版这颗底图一直缺 —— A77-㉒①）");
                    if (bgN != null)
                    {
                        CheckTrue(armSelB != null && bgN.parent == armSelB,
                                  "★ 它是 `Army Selector` 的**直接子件** ⇒ **不被 `RectMask2D` 裁**"
                                  + "（挂到 `Viewport` 下面这条立刻红）");
                        if (armSelB != null && aVpB != null)
                            CheckTrue(bgN.GetSiblingIndex() < aVpB.GetSiblingIndex(),
                                      "★ 它排在 `Viewport` **之前**（= 画在格子**下面**）");
                        float bx1, by1, bx2, by2;
                        if (UnionQuadRect(bgN, out bx1, out by1, out bx2, out by2))
                        {
                            CheckNear(bx1, 69.42f, 1.0f, "★ 底图左沿 = **父件 `Army Selector` 那一格**");
                            CheckNear(by1, 182.18f, 1.0f,
                                      "★ 底图上沿 = **182.18**（`Viewport` 那格是 149.07 —— 拿错那格立刻红）");
                            CheckNear(bx2, 246.54f, 1.0f, "…底图右沿");
                            CheckNear(by2, 880.17f, 1.0f, "…底图下沿 = **880.17**（`Viewport` 那格是 913.28）");
                        }
                        else CheckTrue(false, "底图节点底下**一块 `ImageQuad` 都没有**（`Nine` 没画出来）");
                        var bq = QuadOf(bgN);
                        CheckText(bq != null && bq.Texture != null ? bq.Texture.name : "<无图>",
                                  "UI_Background_faction_buttons",
                                  "★ 底图 sprite = 原版那颗 **`UI_Background faction buttons`**"
                                  + "（`Resources/Art/ui_menu/UI_Background_faction_buttons.png`）");
                        CheckTrue(QuadOf(bgN) != null && QuadOf(bgN).RenderQueue == PracticeModePopup.QPr,
                                  "★ 底图档 = 本窗「面板底」那一档 `QPr`(3100)"
                                  + "（⚠️ 2026-10-10（A178）起格子的四层内景排在 `QPrItemHL`(3102) 以上 ⇒ 画在格子下面；"
                                  + "原来那句写的是 `QPrRow`(3101)）");
                    }
                    var ab = FindChild(armSelB, "AbsorbHitArmy");
                    CheckTrue(ab != null && MenuDraw.WasAbsorb(ab),
                              "…而底图上那颗吸收层是**公共件 `MenuDraw.Absorb` 建的**"
                              + "（等价于原版那颗 `Image` 的 `m_RaycastTarget = 1`：那一下被吃掉、什么都不做）");
                }

                // ============================================================ 🆕 2026-10-07（A77-㉒②③）
                // **`Deck Selector Menu Item` 的内景** —— 原版那一件是**独立 prefab**
                // （`PracticeModePopup.deckSelectorMenuItem` 字段指的；根 **296×296**），我们原来**只画一层底 + 名字**。
                // 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Selector Menu Item" --depth 6 --md`
                //        （2026-10-07 现读）+ `MonoBehaviour_914357594831109544.json` 的 `yourDeckColor`。
                // 兄弟序（**从下到上**）与格内矩形（**本窗的格是 180×180**，原版 296 的模板被 `GridLayoutGroup`
                // 拉成 180 ⇒ 逐层按锚点现算，**不是**整体乘 180/296）：
                //   `Highlight`      `UI_Deck_button_click`     169×169 Simple      **2.08,10.79→180.54,195.14**
                //   `Button border`  `UI_Button_Round_background` 237×237 PA=1      17.87,27.81→164.36,174.30
                //   `Main image`     <运行期喂阵营图>            Simple              0,0→180,180
                //   `IsPlayerDeck`   `Purity Seal_02`            128×256 Simple     137.26,96.01→190.19,200.57
                //   `Text background` `40k_bt_underbutton`       Simple · `yourDeckColor`   0,138.60→180,169.20
                //     └ `Deck Name`  **fs29 · auto[8,29]** · 居中 · **折行 1** · 色 (1,1,1,1)  **2,138.60→174,169.20**（172×30.6）
                // 🔴 **真红法**：删掉 `RebuildDeckRows` 里对应的那几行 ⇒ 逐条红；把格内偏移整体乘 180/296 ⇒
                //    `IsPlayerDeck` 那条红（它是固定 `sizeDelta`、**不随父件缩**）；名字条改回「顶 38px」⇒ 名字四条全红。
                // ⚠️ 期望值全是**原版锚点算出来的字面量**（⛔ 不读 `PracticeModePopup.Item*` —— 那是自证）。
                {
                    var cell0 = FindChild(pw.transform, "DeckRow_0");
                    CheckTrue(cell0 != null, "（前提）`DeckRow_0` 在（下面这一组都挂在它身上）");
                    if (cell0 != null)
                    {
                        // 框心 = (270.08,262.64)（上面已断过格位），下面全是「框心 + 原版格内偏移」
                        var hl0 = FindChild(cell0, "Highlight");
                        var bd0 = FindChild(cell0, "Button border");
                        var mi0 = FindChild(cell0, "Main image");
                        var seal0 = FindChild(cell0, "IsPlayerDeck");
                        var tb0 = FindChild(cell0, "Text background");
                        var nm0 = FindChild(cell0, "Deck Name");
                        CheckTrue(hl0 != null && bd0 != null && mi0 != null && seal0 != null && tb0 != null && nm0 != null,
                                  "★ 原版那**五层**全建了（`Highlight`/`Button border`/`Main image`/`IsPlayerDeck`"
                                  + "`/Text background`+`Deck Name`）—— 原来只有一层底 + 名字");
                        CheckLayerRect("Highlight", hl0, 272.16f, 273.43f, 450.62f, 457.78f, 1.0f);
                        CheckLayerRect("Button border", bd0, 287.95f, 290.45f, 434.44f, 436.94f, 1.0f);
                        CheckLayerRect("Main image", mi0, 270.08f, 262.64f, 450.08f, 442.64f, 1.0f);
                        CheckLayerRect("IsPlayerDeck", seal0, 407.34f, 358.65f, 460.27f, 463.21f, 1.0f);
                        CheckLayerRect("Text background", tb0, 270.08f, 401.24f, 450.08f, 431.84f, 1.0f);
                        // 🆕 **2026-10-11（A232 连带的牙口）**：**越出视口的那一条真被裁掉了**
                        //   （判据 = 原版 `RectMask2D`「框外一个像素都不画」）。第 2 列那格的 `IsPlayerDeck`
                        //   右沿**本来探出视口右沿 5.39px**（**640.27** vs `Decks Scroll view/Viewport` 的
                        //   **634.88** —— 见下面第 ⑦ 段「数竖切线」那条的注释）⇒ A225① 起 `ApplySoftEdges`
                        //   入口会先 `ClipVisToClip` 硬裁 ⇒ **渲染并集右沿必须 ≤ 634.88**。
                        //   🔴 **为什么这条必须有**：A232 给 `ScanSoftCuts` 补了「落在 `clip` 边上的切线不算」
                        //   那一闸之后，**「数竖切线 = 0」不再能照出「硬裁丢了」这一档**（那时那两条假切口正好落在
                        //   `clip.x2` 上、会被闸滤掉）⇒ 这一条直接量**画出来的东西**，是那一档的新牙口。
                        //   **改坏法**：把 `MenuDraw.ApplySoftEdges` 入口那句 `ClipVisToClip` 拿掉 ⇒ 并集右沿回到
                        //   **640.27** ⇒ 这一条红（而切线那两条**照样绿** —— ⛔ 所以两条都要留）。
                        if (pw.DeckRows.Count >= 2)
                        {
                            var seal1 = FindChild(pw.DeckRows[1], "IsPlayerDeck");
                            CheckTrue(seal1 != null, "（前提）第 2 列那格（`DeckRows[1]`）的 `IsPlayerDeck` 在");
                            float sx1, sy1, sx2, sy2;
                            if (seal1 != null && UnionQuadRect(seal1, out sx1, out sy1, out sx2, out sy2))
                            {
                                CheckTrue(sx2 <= 634.88f + 0.5f,
                                          $"★ 它的**渲染并集右沿 ≤ 视口右沿 634.88**（实测 {sx2:F2}）"
                                        + " —— 未裁时它会一直画到 **640.27**（出框 5.39px）");
                                CheckTrue(sx1 >= 261.28f - 0.5f,
                                          $"★ ……左沿也落在视口内（实测 {sx1:F2} ≥ 261.28）—— 同一条判据的另一面");
                            }
                            else CheckTrue(false, "第 2 列那格的 `IsPlayerDeck` 量不到渲染并集（`UnionQuadRect`）");
                        }
                        else CheckTrue(false, "卡组格少于 2 个 ⇒ 上面那条「越界被裁」等于没查");
                        // 五层的**队列**必须互不相同 —— 同队列下「谁盖谁」不可控（`ImageQuad` 全是透明队列，
                        // 按到相机的 3D 距离排）。这一条同时把「按顺序摆对」钉死：Highlight < … < Text background。
                        var qs = new List<int>();
                        foreach (var nd in new[] { hl0, bd0, mi0, seal0, tb0 })
                        {
                            // 🔴 用 `QuadOfInactiveToo`（显式 `includeInactive: true`）—— `Highlight` 在**非当前**
                            //    那一格里是 `SetActive(false)` 的，默认重载对「整棵关着」的子树**返不返回没写死**
                            //    （见 `QuadOfInactiveToo` 头那条）；这里问的是「**建出来了没有**」。
                            var qq = QuadOfInactiveToo(nd);
                            if (qq != null) qs.Add(qq.RenderQueue);
                        }
                        Check(qs.Count, 5, "（前提）五层各取到一块 `ImageQuad`（取不到 ⇒ 下面两条等于没查）");
                        if (qs.Count == 5)
                        {
                            bool strict = true;
                            for (int k = 1; k < qs.Count; k++) if (qs[k] <= qs[k - 1]) strict = false;
                            CheckTrue(strict, "★ 五层的档**严格递增**（= 原版兄弟序 从下到上：" + string.Join(" < ", qs.ToArray()) + "）");
                            CheckTrue(qs[qs.Count - 1] < PracticeModePopup.QPrText,
                                      "★ 而**全部低于**文字档 `QPrText`(" + PracticeModePopup.QPrText + ") —— 名字画在最上面");
                        }
                        // `Highlight` = **只有当前这套**才在（原版 `SelectItem`/`ToggleHighlight` 的 `SetActive`）。
                        // 开窗时选中的是 `DeckLibrary.Current` = **第 4 套**（夹具里最后建的那副）⇒ 两态同时可断。
                        {
                            int cur = pw.DeckIndex;
                            int onCnt = 0, offCnt = 0;
                            foreach (var row in pw.DeckRows)
                            {
                                var h = FindChild(row, "Highlight");
                                if (h == null) continue;
                                if (h.gameObject.activeSelf) onCnt++; else offCnt++;
                            }
                            CheckTrue(onCnt == 1 && offCnt == pw.DeckRows.Count - 1,
                                      $"★ `Highlight` 在 {pw.DeckRows.Count} 个格里**恰好亮 1 个**"
                                      + $"（亮 {onCnt} / 灭 {offCnt}）—— 它才是原版「当前这套」的标记");
                            var curRow = cur >= 0 && cur < pw.DeckRows.Count ? pw.DeckRows[cur] : null;
                            var curHl = curRow != null ? FindChild(curRow, "Highlight") : null;
                            CheckTrue(curHl != null && curHl.gameObject.activeSelf,
                                      $"…而且亮的那一个正是 `DeckIndex` = {cur} 那一格（不是随便挑一个）");
                        }
                        // `Text background` 的色 = **`yourDeckColor`**（不是 prefab 自带那个 `m_Color`）
                        CheckTintOn(QuadOf(tb0), new Color(1f, 0.7951523f, 0f, 1f), 0.01f,
                                    "★ `Text background` 的色 = **`yourDeckColor`** `(1, 0.7952, 0, 1)`"
                                    + "（`DeckSelectorMenuItemDemo` 序列化值；⛔ 不是 prefab 里 Image 自带的 `(0.408,0.811,0.279,1)`）");
                        // ---- ㉒③：名字条 = 原版锚点 → 172×30.6 · 摆**格底** · `fs29 / auto[8,29]` · 居中 · 折行 ----
                        CheckTrue(nm0 != null, "（前提）`Deck Name` 在");
                        if (nm0 != null)
                        {
                            CheckNear(LayoutSpace.PxX(nm0.position.x), 270.08f + 88f, 1.0f,
                                      "★ 名字条**横向中心** = 格左 + (2+174)/2 = 88（宽 172 —— 原版锚点算出来的）");
                            CheckNear(LayoutSpace.PxY(nm0.position.y), 262.64f + 153.9f, 1.0f,
                                      "★ 名字条**纵向中心** = 格上 + (138.60+169.20)/2 = 153.90 ⇒ **在格的底部**"
                                      + "（我们原来是「格的顶 38px」= 格上 + 19 —— 改回顶条这条立刻红）");
                            var tmp0 = nm0.GetComponentInChildren<TMPro.TextMeshPro>();
                            CheckTrue(tmp0 != null, "（前提）名字是真 TMP（下两条要读它的框）");
                            if (tmp0 != null)
                            {
                                CheckNear(tmp0.rectTransform.sizeDelta.x * 108f, 172f, 1.0f,
                                          "★ 名字框**宽 172**（= 格宽 180 − 8，原版 `sizeDelta.x = −8`）");
                                CheckNear(tmp0.rectTransform.sizeDelta.y * 108f, 30.6f, 1.0f,
                                          "★ 名字框**高 30.6**（= 格高 180 × 0.17，原版锚 `(0,0.06)→(1,0.23)`）");
                            }
                            CheckFontWindow(nm0, 8f, 29f,
                                            "★ 名字的**自适应窗口** = 原版 `Deck Name` 的 `m_fontSizeMin/Max` **8 / 29**");
                            CheckWrapping(nm0, true, "★ 名字**折行**（原版 `Deck Name` 的 `m_TextWrappingMode = 1`）");
                            // **㉒③ 那一半**：长名字要**真的被缩进框里**（渲出宽 ≤ 框宽）—— 原来 fs26 无自适应会溢出。
                            CheckFits(nm0, 172f, "★ 名字渲出来放得进 172px 的条（接上自适应之后）");
                        }
                    }
                }

                // ---------------- 🆕 2026-10-04（§三第29条 **A9 尾巴**）：练习窗这**两处**软边 ----------------
                // 判据 = `_tmp_view/q1_rm2d.txt`（逐处实读）。
                //   ⚠️ **2026-10-05 更正（铁律 5，标签错、值没错）**：原来称它「156 个 `RectMask2D` 的
                //     **全量 dump**」是错的 —— 它**只扫了 3 个菜单族包**（表头 `150 + 1 + 5 = 156`）；
                //     **全库真值 = 222**（菜单族 156 + `mainmenualwaysloaded` 1 + 通用弹窗 5
                //     + `scenes_mainmenuwarpforge` 1 + 13 个 arena 各 5 = 65）。
                //   🔑 两条复现判据（会再犯）：① 认的是 `m_Script` 的 PathID **`536591447201701790`**
                //     （`m_FileID = 1` → `bundle_Waprforge_monoscripts`）—— 拿工程本地 `com.unity.ugui`
                //     的 guid 去 grep 解包目录**命中 0**；② **必须限定 `MonoBehaviour/`**（整包 grep
                //     会逐包多算 1）。逐包数字只留一处 → `MenuWindowBase.ClipSoftness` 的注释。
                //     下面那两条值取自各窗 prefab，**不受这次标签订正影响**：
                //   · `Practice Mode Menu/Deck Selector/Army Selector/Viewport`                  = **(0,50)**（:136-137）
                //   · `Practice Mode Menu/Deck Selector/Deck Buttons/Decks Scroll view/Viewport` = **(0,23)**（:208-209）
                // 带的内沿 = 那一格**实际裁到的那条边** ± 原版那个分量（`MenuDraw.ApplySoftEdges` 的几何等效物）。
                // 🔴 **2026-10-05 重算**：上面那两处版式改对之后，裁切矩形＝原版 `Viewport` 的矩形 ⇒
                //   切线的期望值可以直接按原版那两条边算（**两条边都要算** —— 视口变高之后，底下那一侧
                //   也会够到某一格，原来只写一条是因为旧视口矮、底边落在画出来的格之外）：
                //   · `Army Selector`（`m_Softness = (0,50)`，视口 149.07→913.28）：
                //       **上带内沿 149.07 + 50 = 199.07**（第 1 格 149.07..231.07 压着它）
                //       **下带内沿 913.28 − 50 = 863.28**（第 7 格 799.35..881.35 压着它）
                //       ⚠️ 阵营图全是 **256×256 方图**（`Resources/Art/ui_deck/40k_DeckSelection_icon_*.png` 13 张实测）
                //          ⇒ `keepAspect` 不缩，格 = quad 的矩形；两条切线都稳稳落在格内部。
                //   · `Decks Scroll view`（`m_Softness = (0,23)`，视口 262.64→803.43）：
                //       **上带内沿 262.64 + 23 = 285.64**（**没变** —— 这一处原版的 `Viewport` 与父件同矩形）
                //       下带内沿 780.43 要**第 4 行**才够得到，本夹具只有 4 套卡组（两列 ⇒ 2 行）⇒ 够不到。
                // 🔴 期望值一律写**原版字面量**（⛔ 别写成「我们的视口上沿 + 50」—— 那会跟着实现漂 = 自证）。
                {
                    var armyHolder = FindChild(pw.transform, "Army Selector");
                    CheckSoftCuts(ScanSoftCuts(armyHolder, false, new PxRect(
                                                     PracticeModePopup.ArmVpL, PracticeModePopup.ArmVpT,
                                                     PracticeModePopup.ArmVpR, PracticeModePopup.ArmVpB)),
                                  new[] { 199.07f, 863.28f }, 0.6f,
                                  "练习窗 `Army Selector`（原版 `m_Softness = (0,50)`；两条带的内沿 = 原版视口 "
                                  + "**149.07 + 50 = 199.07**（第 1 格压着）与 **913.28 − 50 = 863.28**（第 7 格压着））");
                    Check(ScanSoftCuts(armyHolder, true, new PxRect(
                                                                 PracticeModePopup.ArmVpL, PracticeModePopup.ArmVpT,
                                                                 PracticeModePopup.ArmVpR, PracticeModePopup.ArmVpB)).Count, 0,
                          "…而**一条竖切线都没有**（`(0,50)` 只渐变上下 —— 两轴写反成 `(50,0)` 的实现这里立刻冒竖切线）");

                    var deckHolder = FindChild(pw.transform, "Decks Scroll view");
                    CheckSoftCuts(ScanSoftCuts(deckHolder, false, new PxRect(
                                                     PracticeModePopup.DecksVpL, PracticeModePopup.DecksVpT,
                                                     PracticeModePopup.DecksVpR, PracticeModePopup.DecksVpB)),
                                  new[] { 285.64f }, 0.6f,
                                  "练习窗 `Decks Scroll view`（原版 `m_Softness = (0,23)`；带内沿 = 视口上沿 **262.64 + 23 = 285.64**"
                                  + " —— 第 1 格 y 262.64..442.64 正压着它；带下沿 780.43 那一侧要**第 4 行**才够得到，"
                                  + "本夹具只有 4 套卡组（两列 ⇒ 2 行）⇒ 够不到）");
                    Check(ScanSoftCuts(deckHolder, true, new PxRect(
                                                                 PracticeModePopup.DecksVpL, PracticeModePopup.DecksVpT,
                                                                 PracticeModePopup.DecksVpR, PracticeModePopup.DecksVpB)).Count, 0, "…这一处同样**一条竖切线都没有**（只渐变上下）");
                    // 原版 `RectMask2D` 对**文字**与图一视同仁 ⇒ 文字也要吃软边（`MenuDraw.ClipText` 那条路）。
                    // 🔴 只接图那一路的实现：上面两条**会绿**、这一条红（两条一起才算接全）。
                    // 🔴🔴 **2026-10-07（A77-㉒②③）就地订正（铁律 5）**：这一条原来验的是
                    //    「**第 1 格的 `Name`** 也吃软边」，它成立的**唯一前提**是「名字条摆在**格的顶 38px**」——
                    //    而那一档是**我们挑的**（原版 `Deck Selector Menu Item` 的名字条在**格的底部**）。
                    //    本轮照原版把名字摆回格底之后，**本夹具**（4 套 ⇒ 2 行 ⇒ 内容高 339 < 视口 540.79）
                    //    里**一格都滚不动**，也没有任何一行的名字碰得到上渐隐带
                    //    ⇒ 原来那条断言在这里**已经失去鉴别力**（⛔ 不许留着它冒充覆盖）。
                    //    ⇒ **换一份能滚动的夹具**（下面这一小段，写法照「边界夹具」那条：换一份临时存档、
                    //      断完**指回**）—— 顺便把 **㉒④「内容高跟着阵营筛选重算」** 一起断掉。
                    {
                        string keepPath = DeckStore.OverridePath;
                        string bigFac = null;      // 夹具用的「大阵营」（= `Factions()[0]`，见下）
                        try { System.IO.File.Delete("d:/4/_tmp_view/menu/_menu_scroll_decks.json"); } catch { }
                        DeckStore.OverridePath = "d:/4/_tmp_view/menu/_menu_scroll_decks.json";
                        CollectionData.ResetForTest();
                        {
                            var lib = DeckLibrary.Load();
                            var pool = CardDatabase.Load();
                            // ⚠️ **阵营必须取「列得出来的那一格」**：`Army Selector` 的视口只装得下 **前 8 格**
                            //    （13 × 108.38 = 1382.56 比视口 764.21 高；第 9 格起 `MenuDraw.Visible` 判不中
                            //    ⇒ **根本没建**、`pw.ArmyCells` 里没有它）—— 原来想用 `Ultramarines`
                            //    （字典序**第 13**）去点格子，那会**取不到节点**（假红）。
                            //    ⇒ 取 `Factions()[0]`（第 1 格一定在）。判据 = 上面那两组常量 + 本夹具的
                            //      `pw.ArmyCells.Count`（下面有一条前提断言把它钉死）。
                            var facs0 = CollectionWindow.CardsState.Factions();
                            bigFac = (facs0 != null && facs0.Count > 0) ? facs0[0] : null;
                            CheckTrue(bigFac != null, "（前提）阵营列非空（下面是按 `Factions()[0]` 做的夹具）");
                            string bigHero = null, otherHero = null;
                            foreach (var c in pool)
                            {
                                if (c.Type != "hero") continue;
                                bool big = bigFac != null && RuleEngine.DeckRules.SameFaction(c.Faction, bigFac);
                                if (big && bigHero == null) bigHero = c.Id;
                                if (!big && otherHero == null) otherHero = c.Id;
                            }
                            CheckTrue(bigHero != null && otherHero != null,
                                      "（前提）卡池里「" + bigFac + "」与别家各能取到一个督军 —— 下面那份夹具靠它们分阵营");
                            // **9 套 = 8 套「大阵营」 + 1 套别家**（两列 ⇒ **5 行** ⇒ 内容高 159×5+21 = 816）。
                            // 816 > 视口 540.79 ⇒ **滚得动**（可滚到底 = 816 − 540.79 = **275.21px**）。
                            // 🔴 **第 1 套故意用超长名字** —— 那是 **㉒③「长卡组名会溢出」** 的阳性对照
                            //    （原来的 fs26 + 无自适应画到框外；现在 fs29 + `auto[8,29]` 要把它缩进 172px 里）。
                            for (int k = 0; k < 9 && bigHero != null && otherHero != null; k++)
                            {
                                var d = lib.Create(k == 0
                                    ? "滚动夹具 很长的卡组名字 Test Deck With A Very Long Name 0123456789"
                                    : "滚动夹具 " + (k + 1));
                                if (d != null) d.WarlordId = k < 8 ? bigHero : otherHero;
                            }
                            lib.Save();
                        }
                        CollectionData.ResetForTest();
                        Check(CollectionData.DeckCount(), 9, "滚动夹具：**9 套**（8 套同阵营 + 1 套别家）");
                        pw.RebuildDeckListForTest();
                        CheckTrue(pw.ArmyCells.Count > 0,
                                  "（前提）阵营格列出来了 " + pw.ArmyCells.Count + " 个（只装得下前 8 个 —— 下面按第 1 个筛）");

                        // ---- ㉒③：**长卡组名不再溢出**（第 1 套那个超长名字就是阳性对照）----
                        // 判据 = 原版 `Deck Name` 的 `fs29 + auto[8,29]` + 框 172×30.6 ⇒ TMP 自适应该把它缩进去。
                        // 🔴 **两条一起才有鉴别力**（单看宽或单看高都会被「只接了折行」那一档蒙混过去）：
                        //    · **宽 ≤ 172**：`SetWrapWidth`/折行也能满足它；
                        //    · **高 ≤ 30.6**：这一条只有**字号真的缩了**才满足 —— 光折行不缩的话，
                        //      这个 60 字的名字在 fs29 下是 6 行 ≈ **210px** 高。
                        // 🔴 **真红法**：把 `RebuildDeckRows` 里那句 `nm.SetAutoFitBox(…, 8, 29)` 删掉 ⇒
                        //    字号回到名义 29（只折行）⇒ **高那条红**；再连 `SetWrapping` 一起去掉 ⇒ 两条都红。
                        {
                            var longName = FindChild(FindChild(deckHolder, "DeckRow_0"), "Deck Name");
                            CheckTrue(longName != null, "（前提）第 1 套是那个**超长名字**的卡组、`Deck Name` 在");
                            if (longName != null)
                            {
                                var lbL = longName.GetComponentInChildren<Label>();
                                CheckTrue(lbL != null, "（前提）那一段是真 `Label`");
                                if (lbL != null)
                                {
                                    CheckFits(longName, 172f, "★ ㉒③ 超长卡组名**渲出来不超过 172px 的条**");
                                    CheckTrue(lbL.WorldH * 108f <= 31.6f,
                                              $"★ ㉒③ …而且**高度也没溢出** 30.6px 的条（实得 {lbL.WorldH * 108f:F2}px）"
                                              + " —— 只接折行、没接自适应的那一档在这里是 ~210px");
                                    CheckTrue(lbL.FontPxNow < 28.5f,
                                              $"★ ㉒③ 字号确实被自适应**缩过**（实得 {lbL.FontPxNow:F2}px < 名义 29px）");
                                }
                            }
                        }

                        // ---- ㉒④：**内容高**（原版 `Content` 上那个 `ContentSizeFitter(MinSize)` 撑出来的）----
                        // 期望值 = 原版 `GridLayoutGroup` 算式 `padT+padB+(格高+spacing.y)×行数−spacing.y`
                        //        = `0 + 0 + (180−21)×行数 + 21`（⛔ 不从 `PracticeModePopup` 读 —— 自证）。
                        CheckNear(pw.DeckScrollContentH, 816f, 0.6f,
                                  "★ ㉒④ 不过滤：**9 套 ⇒ 5 行** ⇒ 内容高 = 159×5 + 21 = **816**");
                        {
                            // ⚠️ 筛**第 1 格**（`Factions()[0]` = 上面那个 `bigFac`）：只有前 8 格建出来了。
                            var aCell = pw.ArmyCells.Count > 0 ? pw.ArmyCells[0] : null;
                            var aHit = aCell != null ? FindChild(aCell, "Hit") : null;
                            var awb = aHit != null ? aHit.GetComponent<WindowButton>() : null;
                            CheckTrue(awb != null, "（前提）第 1 格（= `" + bigFac + "` 那格）有点击区");
                            if (awb != null)
                            {
                                awb.Click();                       // = `PickArmy` ⇒ 重建卡组列表
                                Check(pw.ArmyIndex, 0, "点第 1 格（`" + bigFac + "`）⇒ 阵营筛选生效");
                                CheckNear(pw.DeckScrollContentH, 657f, 0.6f,
                                          "★ ㉒④ **筛成 8 套 ⇒ 4 行** ⇒ 内容高**跟着重算** = 159×4 + 21 = **657**"
                                          + "（改回「只在建窗时算一次」⇒ 这里还是 816，这条立刻红）");
                                CheckNear(pw.DeckScroll.MaxOffset, 657f - (803.43f - 262.64f), 0.6f,
                                          "★ ㉒④ **可滚范围**也跟着收 = 657 − (视口下沿 803.43 − 内容顶 262.64) = **116.21**"
                                          + "（这是「内容高不重算」最直接的症状：剩 4 行却还能拖出 275px 的空白）");
                                awb.Click();                       // 再点一次 = 取消筛选（`PickArmy` 是 toggle）
                                Check(pw.ArmyIndex, -1, "再点一次 ⇒ 取消筛选");
                                CheckNear(pw.DeckScrollContentH, 816f, 0.6f, "…内容高回到 **816**（同一份实现两个方向都对）");
                            }
                        }

                        // ---- 文字吃软边：把**第 1 格**的名字条滚进上渐隐带再验 ----
                        // 名字条 = 格内 y **138.60..169.20**（原版锚点）；第 1 格 = 262.64..442.64
                        // ⇒ 不滚时在 401.24..431.84，离带（262.64..285.64）差 116px。下滚 **140** 之后到
                        // **261.24..291.84** —— 正压着整条带。
                        pw.DeckScroll.SetOffset(140f);
                        CheckNear(pw.DeckScroll.Offset, 140f, 0.6f, "（前提）卡组列表下滚 140px（本夹具滚得到，见上）");
                        var row0Name = FindChild(FindChild(deckHolder, "DeckRow_0"), "Deck Name");
                        CheckTrue(row0Name != null, "（前提）`DeckRow_0` 的 `Deck Name` 在（滚 140 之后它仍与视口相交）");
                        if (row0Name != null)
                        {
                            float nc = LayoutSpace.PxY(row0Name.position.y);
                            CheckNear(nc, 262.64f + 153.9f - 140f, 1.0f,
                                      "★ 名字条的纵向中心滚到 = 视口上沿 + (138.60+169.20)/2 − 140 = **276.54**"
                                      + "（⇒ 落在渐隐带 262.64..285.64 **里面** —— 下面那条才有鉴别力）");
                            CheckTrue(nc > 262.64f && nc < 285.64f,
                                      "★ …确认它在带内（不在带内 ⇒ 下面那条「削 alpha」等于没查）");
                            int nInBand, nBadProf; float minAlpha, worstErr;
                            int faded = CountSoftFadedTextVerts(row0Name, 262.64f, 803.43f, 23f,
                                                                out nInBand, out nBadProf, out minAlpha, out worstErr);
                            CheckTrue(faded > 0,
                                      "★ 卡组名**也吃软边**：滚进上渐隐带的 `Deck Name` 的 TMP 网格里有 **" + faded
                                      + "** 个顶点 alpha < 250（`MenuDraw.ClipText` 按同一条剖面削；"
                                      + "只接图那一路的实现这里是 0，-1 = 连 TMP 都没取到）"
                                      + $"｜诊断：带内（y > 285.64）**{nInBand}** 个顶点、最小 alpha **{minAlpha:F0}**"
                                      + "（带内 0 个 ⇒ 这段字不在带里 —— 那不是裁切的问题，是文字摆位）");
                            // 🔴 **第二条是「同一代 mesh 里裁两刀」的牙口**（2026-10-08 A225-② 加）：
                            //    现在那一刀会被裁**两次**（事件一刀 + `Label.RefreshBounds` 补的一刀）⇒
                            //    写入必须是**绝对值**（`MenuDraw.ClipTmpMesh` 的 `BaseCornerAlpha`）。
                            //    改回「在现值上再乘」⇒ 带内 alpha 被乘两遍（0.5 → 0.25）⇒ 这里立刻红。
                            //    期望值 = 原版剖面（uGUI `UI/Default` 的 `saturate(到最近边的距离 ÷ softness)`），
                            //    ⛔ 不从我们自己的实现读。带外（y ∉ 带）want = 255 ⇒
                            //    顺带把「带外的顶点一个都没被动」也钉住了。
                            Check(nBadProf, 0,
                                  $"★ 带内每个顶点的 alpha = **255 × 剖面**（`min((y−262.64), (803.43−y)) ÷ 23`，"
                                  + $"带内 {nInBand} 个顶点；最差差 **{worstErr:F1}**）"
                                  + " —— 写入若改回 `alpha × 剖面`（在现值上再乘），带内会被乘两遍 ⇒ 这条红");
                        }
                        pw.DeckScroll.SetOffset(0f);

                        // ---- 🆕 **A195②**：`MenuDraw.TextClipUnavailable` 的**读者**（原来 `MainMenuScene.Run` 里没有）----
                        // 谁写它：`MenuDraw.ClipTextNow` —— 两条后端（TMP / 点阵兜底）**都**拿不到渲染网格时 `++`，
                        //        并在头 3 次各打一条 `[MenuDraw] 「X」没有可裁的渲染网格…` 的警告。
                        // 为什么补：上面那条 `faded > 0` 只证明「这条路**带电**」（真削了 alpha）；而**落空**那一档
                        //        （这段字**根本没被裁**）原来**只进控制台、不进断言账** ⇒ 自检照样绿（= 静默门）。
                        // `Editor/RewardsScene.cs` 早有一条同样的 `== 0`（那 §三·b4-d-3 ③），这一边一直缺。
                        // ⚠️ 这是**全程累计**计数（不是只数上面那一格）⇒ 红了先看日志里那三行 `[MenuDraw] …`，
                        //    它直接点名是哪段字拿不到网格。
                        CheckTrue(MenuDraw.TextClipUnavailable == 0,
                                  $"★ A195② **没有一次「拿不到渲染网格」**（实测 {MenuDraw.TextClipUnavailable} 次）"
                                + " —— 那意味着某段字**悄悄没被裁**（原版 `RectMask2D` 会裁）");

                        // ---- 收尾：**指回原夹具**（下面几节还在用那 4 套：第 2 套必须是空卡组）----
                        DeckStore.OverridePath = keepPath;
                        CollectionData.ResetForTest();
                        Check(CollectionData.DeckCount(), 4, "存档已指回夹具那一份（4 套）");
                        pw.RebuildDeckListForTest();
                        CheckNear(pw.DeckScrollContentH, 339f, 0.6f, "…内容高也回到 **339**（4 套 ⇒ 2 行）");
                    }
                }
                // ⚠️ 开窗时选中的是 `DeckLibrary.Current`（= 新建的**第 4 套：合法 30 张那一副**；
                //    前 3 套里第 1 套带督军、第 2/3 套是空的）⇒ 这里先把第 1 套点上再断
                {
                    var r0a = FindChild(pw.transform, "DeckRow_0");
                    var h0a = r0a != null ? FindChild(r0a, "Hit") : null;
                    var w0a = h0a != null ? h0a.GetComponent<WindowButton>() : null;
                    CheckTrue(w0a != null, "第 1 套卡组那一行有点击区");
                    if (w0a != null) w0a.Click();
                    Check(pw.DeckIndex, 0, "点第 1 套 ⇒ 选中它");
                    CheckTrue(pw.CardRows.Count > 0,
                              $"卡列表画了 {pw.CardRows.Count} 行（第 1 套里塞了督军 + 4 张部队）");
                    // 🔴 实拍抓的：卡名会**超过 231px 的格宽**、撞进右边那一列（`Death Spinner Warp Spider`）
                    //    ⇒ 现在断「每一行的名字放得进格子里」（量的是 **Label 自己量出来的宽**）
                    int over = 0; float widest = 0f;
                    foreach (var row in pw.CardRows)
                    {
                        var nl = row.GetComponentInChildren<Label>();
                        if (nl == null) continue;
                        float wpx = nl.WorldW * 108f;
                        widest = Mathf.Max(widest, wpx);
                        if (wpx > PracticeModePopup.DlCellW + 0.5f) over++;
                    }
                    Check(over, 0, $"卡列表**每一行的卡名都放得进 {PracticeModePopup.DlCellW}px 的格**（最宽 {widest:F1}px；"
                                   + "判据是**原版格宽**，不是我们自己的常量 —— 第一版撞列就是这条没断）");

                    // 🆕 **2026-10-07（A77-⑤）**：卡名那格的**自适应窗口**照原版钉死 + **不折行**。
                    // 判据 = `Deck Selector Card Info button`（卡列表格子的原版 prefab，挂 `UICardInfoItem`）
                    //        的 `Content/Text fill/Card Name`：`m_fontSize = 38` · `m_fontSizeMin/Max = **2 / 38**`
                    //        （`auto[2.0~38.0]`）· 对齐 `Left/Capline` · **`m_TextWrappingMode = 0`（不折行）**。
                    // 出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Selector Card Info button"
                    //        --depth 5 --md`（2026-10-07 现读）。
                    // 🔴 **原来传的是 `10 / 20`**（= 我们挑的，注释自己写着「原版内部怎么排**没查**」）—— 真偏离。
                    // 🔴 **真红法**：把 `CardNameAutoMin/Max` 改回 `10/20` ⇒ 窗口那两条红
                    //    （`CheckFontWindow` 读的是 `m_fontSizeMin/Max` 本身，不是收敛出来的字号 ⇒ 改回去立刻现形）；
                    //    删掉 `nm.SetWrapping(false)` ⇒ 折行那条红（`SetAutoFitBox` 内部会**无条件**开折行）。
                    {
                        var cn0 = pw.CardRows.Count > 0 ? FindChild(pw.CardRows[0], "Name") : null;
                        CheckTrue(cn0 != null, "（前提）卡列表第 1 行的 `Name` 在（下面两条挂在它身上）");
                        if (cn0 != null)
                        {
                            CheckFontWindow(cn0, 2f, 38f,
                                            "★ 卡列表卡名的自适应窗口 = 原版 `Card Name` 的 `auto[2,38]`");
                            CheckWrapping(cn0, false,
                                          "★ 卡列表卡名**不折行**（原版 `Card Name` 的 `m_TextWrappingMode = 0`）");
                        }
                    }

                    // 🔴 **第 53 条的判据**（`项目任务.md` §三）：`Deck Name` / `Warlord Name` 与**卡列表**不叠。
                    //    量的是**渲出来的矩形**（`Label.WorldW/H` + 世界坐标反算 ⇒ 1920×1080 像素），**不是源码常量**。
                    //    原版这两个抽屉互斥、且 `Content` 起 y **318.42** —— 两个视图下这条都该成立。
                    // 🔴 **2026-10-07（A77-㉒②）这里必须加作用域**：原来写的是 `FindChild(pw.transform, "Deck Name")`
                    //    —— 全窗递归取**第一个**同名节点。本轮照原版补上 `Deck Selector Menu Item` 的内景之后，
                    //    卡组格里也有一颗 **`Deck Name`**（原版那一件本来就叫这名），而 `Decks Scroll view`
                    //    在 `Deck info` **之前**建 ⇒ 递归查找会先撞上**格子里的那颗**（假绿/假红都可能）。
                    //    ⇒ 一律从 **`Deck info`** 往下找（那才是第 53 条说的那一颗）。
                    {
                        var info53 = FindChild(pw.transform, "Deck info");
                        CheckTrue(info53 != null, "（前提）`Deck info` 在");
                        float a1, b1, a2, b2, c1, d1, c2, d2;
                        bool okName = RenderedRect(FindChild(info53, "Deck Name"), out a1, out b1, out a2, out b2);
                        bool okWl = RenderedRect(FindChild(info53, "Warlord Name"), out c1, out d1, out c2, out d2);
                        float rowTop = float.MaxValue, rowBot = float.MinValue;
                        foreach (var row in pw.CardRows)
                        {
                            float e1, f1, e2, f2;
                            if (RenderedRect(row, out e1, out f1, out e2, out f2))
                            { rowTop = Mathf.Min(rowTop, f1); rowBot = Mathf.Max(rowBot, f2); }
                        }
                        CheckTrue(okName && okWl && rowTop < float.MaxValue,
                                  "`Deck Name` / `Warlord Name` / 卡列表三者都能量到渲染矩形");
                        if (okName && okWl && rowTop < float.MaxValue)
                        {
                            float nameBot = Mathf.Max(b2, d2);
                            CheckTrue(nameBot <= rowTop + 0.5f,
                                      $"**第 53 条的判据**：`Deck Name`/`Warlord Name` 渲出来的底边 **{nameBot:F1}px** "
                                      + $"**不越过**卡列表顶边 **{rowTop:F1}px**（原版 `Content` 起 y=318.42）");
                            CheckTrue(rowBot <= PracticeModePopup.DlContentB + 0.5f,
                                      $"卡列表渲出来的底边 {rowBot:F1}px 不超过 `Content` 下沿 {PracticeModePopup.DlContentB}px");
                        }
                    }
                }
                // ============================================================ §A218 `sizeDelta`（2026-10-11 新增）
                //
                // 🔴 **为什么单开一节**：A92 只补了「类型」（`RectTransform`），`sizeDelta` 仍是默认值
                //    ⇒ 「空节点 + 原版像素矩形」的**宽高验收不了**（判据原文 = `资料/待办判据_1007.md` §A218）。
                //    本节把练习窗那几处**逐个**断掉：期望值 = **原版那一件自己的矩形**（⛔ 不读被测实现，
                //    也不拿 `rt.sizeDelta` 自己当期望值）；断的是 **`rect`**（本件的验收口径）。
                //    **两态可分辨**：同一对工厂、不同矩形各一条 ⇒ 弱断言（只断 `!= 0`）分不出来。
                // 🔴 **父链缩放核查**：本窗 `extraScaleSmallScreen = **1.07**`（≠ 1）—— 但小屏缩放器只在
                //    **开关开**（出厂关）时才把 `menuScale` 乘到**窗根**那一级，且 `WindowsManager.AttachToAnchor`
                //    把窗根写成 `localScale = one` ⇒ **今天父链是单位缩放**（下面每条都先断这条前置）。
                //    开关真开时：窗内每件的**世界尺寸**自动 ×1.07（= 原版「整扇窗放大」），`sizeDelta` **不该**再折算
                //    —— 判据/算式 → `MenuDraw.SetPxSize` 的注释。
                Section("§A218 练习窗：空节点工厂的 `sizeDelta`（`rect` 的宽高 = 原版矩形）");
                {
                    System.Action<Transform, string, float, float, string> box = (t, what, wPx, hPx, src) =>
                    {
                        if (t == null) { CheckTrue(false, $"（前提）{what} 没建出来"); return; }
                        var rt = t.GetComponent<RectTransform>();
                        CheckTrue(rt != null, $"（前提）{what} 是 `RectTransform`（A92 那半）");
                        if (rt == null) return;
                        CheckNear(t.lossyScale.x, 1f, 1e-3f,
                                  $"（前提·父链缩放）{what} 的 `lossyScale.x` = 1（= `rect` 与设计 px 同量纲那一档）");
                        CheckNear(rt.rect.width, LayoutSpace.Px(wPx), 0.01f,
                                  $"★ {what} 的 `rect.width` = **{rt.rect.width * 108f:F2}px**（原版 {wPx}px —— {src}）");
                        CheckNear(rt.rect.height, LayoutSpace.Px(hPx), 0.01f,
                                  $"★ …`rect.height` = **{rt.rect.height * 108f:F2}px**（原版 {hPx}px）");
                    };
                    box(pw.transform, "练习窗根 `Practice Mode Menu`", 1920f, 1080f,
                        "原版 `anchor (0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ 整屏矩形");
                    box(FindChild(pw.transform, "Deck List Drawer"), "卡列表抽屉 `Deck List Drawer`", 569.79f, 619.53f,
                        "抽屉那一块 = `DlL/DlT/DlR/DlB`（1240.38,207.65 → 1810.17,827.18）；原版这一件实读是 `RectTransform`（A92 那张类型表）");
                    box(pw.ArmyCells.Count > 0 ? pw.ArmyCells[0] : null, "阵营格 `Army_0`", 82f, 82f,
                        "原版 item prefab `Practice Army Select Button` 的根 **82×82**");
                    box(pw.ArmyCells.Count > 0 ? FindChild(pw.ArmyCells[0], "Hit") : null,
                        "阵营格的命中区（`HitOn` 那条路）", 82f, 82f, "= 传进去那个格矩形（82×82）");
                    box(pw.DeckRows.Count > 0 ? pw.DeckRows[0] : null, "卡组格 `DeckRow_0`", 180f, 180f,
                        "原版 `Deck Selector Menu Item` 的格（`GridLayoutGroup` 格尺寸 180×180）");
                    box(pw.CardRows.Count > 0 ? pw.CardRows[0] : null, "卡格 `CardRow_0`", 231f, 27.88f,
                        "原版卡列表格 `DlCellW × DlCellH` = **231 × 27.88**");
                    box(pw.TogHit, "命中区 `ToggleHit`", 293.52f, 57.84f,
                        "= 传进去那个矩形 `TogL/TogT/TogR/TogB`（813.24,906.26 → 1106.76,964.10）");
                }

                // ============================================================ §A332 `sizeDelta`（2026-10-11 新增）
                //
                // 🔴 **这一节补的是 A218 的尾巴**：本窗那个私有 `New`（`Shell/PracticeModePopup.cs`）
                //    过去 9 个调用点**清一色只写 `localPosition`、一个 `sizeDelta` 都没写**
                //    ⇒「空节点 + 原版像素矩形」的宽高验收不了（与 A218 修掉的是同一个缺陷）。
                //    A332 把工厂收口到 `MenuDraw.Node`（位置 `Local` + 尺寸 `SetPxSize`，**同一份换算**），
                //    下面把这 9 处逐个断掉。
                //    下面把这 9 处逐个断掉。
                //
                // 🔴 **期望值的来源**（⛔ 不是读被测实现 —— 本窗那一堆 `InfoL/DlL/ArmL/DecksL/ArmVpL/DecksVpL/DecksCL`
                //    常量**一个字都没引用**，一律写字面量 + 出处）：那些常量自己就是**原版 prefab 实读**来的
                //    （出处 → 本窗常量段的注释），本节的四个数 = 它们的右沿 − 左沿 / 下沿 − 上沿。
                //
                // 🔴 **两态可分辨（本节最硬的一条）**：`Army Selector/Viewport` 与 `Decks Scroll view/Viewport`
                //    **同名、同一个工厂、两对入参** —— **177.12×764.21** vs **373.60×540.79**。把 177.12/764.21
                //    写死进工厂（或只断一条）过不了另一条；再加 `Content` 的 **高 = 0**（原版靠
                //    `ContentSizeFitter` 长高、我们这条不会跟着长）⇒ 三态互斥，弱断言分不出来。
                //
                // 🔴 **改坏法**：① `PracticeModePopup.New` 里 `MenuDraw.Node` 那一转调改成「只建节点」
                //    （或删掉 `SetPxSize` 那一跳）⇒ 下面**每一条**的 `rect.width/height` 都变 0 ⇒ 全红；
                //    ② 只把某一处调用点的 `new PxRect(...)` 改错 ⇒ 只那一条红；③ 把 `Local3` 那一行删掉
                //    在别处补回 —— 位置不会变（`Local3` 与 `MenuDraw.Local` 逐字同式）⇒ **这一节不测位置**
                //    （位置由本窗既有的 `CheckAt`/`CheckAtWorld` 那几十条守着）。
                Section("§A332 练习窗：9 个空节点工厂调用点的 `sizeDelta`（`rect` 的宽高 = 原版矩形）");
                {
                    System.Action<Transform, string, float, float, string> pbox = (t, what, wPx, hPx, src) =>
                    {
                        if (t == null) { CheckTrue(false, $"（前提）{what} 没建出来"); return; }
                        var rt = t.GetComponent<RectTransform>();
                        CheckTrue(rt != null, $"（前提）{what} 是 `RectTransform`（A92 那半）");
                        if (rt == null) return;
                        CheckNear(t.lossyScale.x, 1f, 1e-3f,
                                  $"（前提·父链缩放）{what} 的 `lossyScale.x` = 1（= `rect` 与设计 px 同量纲那一档）");
                        CheckNear(rt.rect.width, LayoutSpace.Px(wPx), 0.01f,
                                  $"★ {what} 的 `rect.width` = **{rt.rect.width * 108f:F2}px**（原版 {wPx}px —— {src}）");
                        CheckNear(rt.rect.height, LayoutSpace.Px(hPx), 0.01f,
                                  $"★ …`rect.height` = **{rt.rect.height * 108f:F2}px**（原版 {hPx}px）");
                    };
                    var dInfo = FindChild(pw.transform, "Deck info");                    // 638.38,78.79 → 1842.38,863.77
                    pbox(dInfo, "`Deck info`", 1204f, 784.98f,
                         "= `InfoR − InfoL` / `InfoB − InfoT`（本窗常量段；原版那一件的绝对矩形 638.38,78.79→1842.38,863.77）");
                    pbox(FindChild(dInfo, "Background Info"), "`Background Info`", 531f, 484.42f,
                         "= 1258.08,316.03 → 1789.08,800.45（原版这一件实读的四个数）");
                    pbox(FindChild(dInfo, "General container"), "`General container`", 569.79f, 619.53f,
                         "= `DlR − DlL` / `DlB − DlT`（1240.38,207.65 → 1810.17,827.18，与 `Deck List Drawer` 同矩形）");
                    var armySel = FindChild(pw.transform, "Army Selector");              // 69.42,182.18 → 246.54,880.17
                    pbox(armySel, "`Army Selector`", 177.12f, 697.99f,
                         "= 69.42,182.18 → 246.54,880.17（**不是** 下面 `Viewport` 那一对 —— 它上下各探出 33.11）");
                    var armyVp = FindChild(armySel, "Viewport");
                    pbox(armyVp, "`Army Selector/Viewport`", 177.12f, 764.21f,
                         "= `ArmVpR − ArmVpL` / `ArmVpB − ArmVpT`（69.42,149.07 → 246.54,913.28）");
                    pbox(FindChild(armyVp, "Filters"), "`Army Selector/Viewport/Filters`", 177.12f, 764.21f,
                         "原版 `Filters` 与 `Viewport` **同矩形**（格容器，`GridLayoutGroup`）");
                    var decksSel = FindChild(pw.transform, "Decks Scroll view");          // 261.28,262.64 → 634.88,803.43
                    pbox(decksSel, "`Decks Scroll view`", 373.60f, 540.79f,
                         "= 261.28,262.64 → 634.88,803.43");
                    var decksVp = FindChild(decksSel, "Viewport");
                    pbox(decksVp, "`Decks Scroll view/Viewport`", 373.60f, 540.79f,
                         "原版这一件与父件**同矩形**；⚠️ **与上面那个同名 `Viewport` 不是一对数**（177.12×764.21 vs 373.60×540.79）");
                    pbox(FindChild(decksVp, "Content"), "`Decks Scroll view/Viewport/Content`", 372.04f, 0f,
                         "= `DecksCR − DecksCL` × **0** —— 原版靠 `ContentSizeFitter` 长高，我们这条出厂就是 0"
                         + "（`DecksVpT` 上下同值）⇒ 如实断 0，⛔ 别拿内容高顶替");
                }

                // 换一套卡组（验「选中态 + 卡列表跟着换」）
                //   ⚠️ 第 2 套是**空卡组**（只有名字、没督军没卡）⇒ 「卡列表 0 行 + `Battle!` 如实拒绝」**都对**，
                //      这正是本轮要验的两条**反面**判据；验完再点回第 1 套去开战。
                if (pw.DeckRows.Count > 1)
                {
                    var r1 = pw.DeckRows[1];
                    var h1 = FindChild(r1, "Hit");
                    var w1 = h1 != null ? h1.GetComponent<WindowButton>() : null;
                    if (w1 != null) w1.Click();
                    Check(pw.DeckIndex, 1, "点第 2 套卡组 ⇒ 选中的换成它");
                    Check(pw.CardRows.Count, 0, "换到**空卡组** ⇒ 卡列表 0 行（跟着换了，不是没刷新）");
                    var bh0 = pw.BtHit;
                    var bwb0 = bh0 != null ? bh0.GetComponent<WindowButton>() : null;
                    if (bwb0 != null) bwb0.Click();
                    CheckTrue(!pw.StartedBattle, "**没有督军的卡组 ⇒ `Battle!` 如实拒绝**（不许静默开局）");
                    // ⚠️ 那个提示窗是**模态**的，不关掉会把后面那张主菜单截图盖住（第一版就是这样）
                    foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None))
                        if (pp != null) pp.Close();
                    var r0 = FindChild(pw.transform, "DeckRow_0");
                    var h0 = r0 != null ? FindChild(r0, "Hit") : null;
                    var w0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                    if (w0 != null) w0.Click();
                    Check(pw.DeckIndex, 0, "点回第 1 套 ⇒ 选中回来");
                    CheckTrue(pw.CardRows.Count > 0, "卡列表也跟着回来了");
                }
                var bh = pw.BtHit;
                var bwb = bh != null ? bh.GetComponent<WindowButton>() : null;
                CheckTrue(bwb != null, "`Battle!` 有点击区");
                if (bwb != null) bwb.Click();
                // 🔴 **2026-09-24 起 `Battle!` 走原版那条链**：`StartMatch`（开 `Searching Oponent Popup` 等 12s）
                //    → 等不到真人 → `StartBotBattle`。等待秒数 = 原版 `GetTimeToWaitForOpponent` 的常量支
                //    （`DAT_1834b3160` 读出来 = 12；轮询间隔 `DAT_1834b2bb8` = 1s）。
                {
                    var sp = FindChild(pw.transform, "Searching Oponent Popup");
                    CheckTrue(sp != null, "`Searching Oponent Popup` 建了（四窗共用 `SearchingMatchPopup`）");
                    CheckTrue(sp != null && sp.gameObject.activeSelf,
                              "点 `Battle!` ⇒ **匹配窗先出来**（原版 `MatchMakerManager.StartMatch`）");
                    CheckTrue(!pw.StartedBattle, "12 秒**还没到** ⇒ 还没开局（不抢跑）");
                    pw.TickSearch(9f);          // 打字机「Searching」9 个字 × 0.5s = 4.5s，这里一并推过去
                    CheckTrue(!pw.StartedBattle, "推到第 9 秒 ⇒ 仍未开局");
                    pw.TickSearch(3f);          // 满 12 秒
                    CheckTrue(sp == null || !sp.gameObject.activeSelf, "等满 12 秒 ⇒ 匹配窗自己关掉");
                }
                CheckTrue(pw.StartedBattle, "点 `Battle!` + 等满 12 秒 ⇒ **开战成立**（真机上这一步 `LoadScene(\"Battle\")`）");
                Check(CollectionData.CurrentIndex(), pw.DeckIndex,
                      "开战前**把选中的那套交给 `DeckLibrary`**（`BattleDriver.PickSavedDeck` 读的就是它）");
                Shoot("02_练习模式窗.png");

                // ---------------- `Deck info Popup`（2026-09-24 实读订正的那条入口）----------------
                // 原版：`Show Deck Content Button` = `DeckGeneralInfoDemo.cardInDeckInfoButton`
                //   → `CardInDeckInfoButtonOnClick → WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`
                //   🔴 **它不是抽屉开关**（正本原来把它当开关，那是错的 —— 见 `PracticeModePopup` 文件头）。
                Section("`Deck info Popup`（练习窗 `Show Deck Content` 那条 · 原版 `DeckInfoContext(deck, 2, …)`）");
                var sdHit = FindChild(pw.transform, "ShowDeckHit");
                var sdBtn = sdHit != null ? sdHit.GetComponent<WindowButton>() : null;
                CheckTrue(sdBtn != null, "`Show Deck Content` 有点击区");
                if (sdBtn != null)
                {
                    sdBtn.Click();
                    CheckTrue(PracticeModePopup.LastDeckInfo != null, "点它 ⇒ **开出了 `Deck info Popup`**");
                }
                // ============================================================ 🆕 2026-10-04
                // **A65①**（这一行接线）+ **A65④**（原版 `Initialize` 末尾那三条接线层）
                //
                // ① 判据 = `DeckGeneralInfoDemo__CardInDeckInfoButtonOnClick.c`：
                //    `DeckInfoContext___ctor(uVar3, 卡组, **2** /*state*/, 0, 0, 0, 0)` ⇒ **View(2)**。
                //    显隐判据 = `DeckInfoControls__Initialize` 的 SetActive：
                //      `Edit Deck` ← `state < 2` · 四颗圆钮（Share / Share On Chat / Delete / Duplicate）
                //      ← `state == 0 && isPlayerDeck` · `Practice Deck` ← `isPlayerDeck && state ∈ {0,2}` ·
                //      `Switch Deck Info` ← **常显**。
                //    🔴 **真红法**：把 `PracticeModePopup.ShowDeckContent` 里那第三个实参删掉（回到默认 `Edit`）
                //      ⇒ 下面那四条（含两态对照）立刻红。
                // ④ 判据 = `DeckInfoControls__Initialize.c:201-243`（三条各自的出处写在
                //    `Shell/DeckInfoPopup.cs` 的 `ApplyControlStates()` 上）。
                //    🔴 **真红法**：删掉 `ApplyControlStates()` 里那几句赋值（或删掉 `Build()` 末尾那次调用）
                //      ⇒ 下面 `PracticeDeckInteractable` / `DeleteInteractable` / `DuplicateInteractable` /
                //      `DrawerToggleIsOn` 那几条全红；把 `Blocked` 那两道闸门去掉 ⇒ 两条「点了什么都不发生」红。
                var di = PracticeModePopup.LastDeckInfo;
                if (di != null)
                {
                    Check(di.State, DeckInfoPopup.DeckInfoState.View,
                          "★ 练习窗那扇 `Deck info Popup` 的 `state` = **View(2)**（原版 `DeckInfoContext(deck, 2, …)`）");
                    CheckTrue(!di.IsItemShown("Btn:Edit Deck"),
                              "★ state 2 ⇒ `Edit Deck` **藏起来**（原版 `state < 2`）—— 改回 state 0 这条立刻红");
                    CheckTrue(!di.IsItemShown("Opt:Share") && !di.IsItemShown("Opt:Share On Chat")
                              && !di.IsItemShown("Opt:Delete") && !di.IsItemShown("Opt:Duplicate"),
                              "★ state 2 ⇒ 4 颗圆钮（`Share`/`Share On Chat`/`Delete`/`Duplicate`）**全藏**（`state == 0 && isPlayerDeck`）");
                    CheckTrue(di.IsItemShown("Btn:Practice Deck"),
                              "…而 `Practice Deck` **仍露**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                    CheckTrue(di.IsItemShown("Opt:Switch Deck Info"), "…`Switch Deck Info` **常显**");
                    // 两态对照：**同一条实现**把 `State` 改回 0 再摆一次 ⇒ 那五颗必须回来（只断一态分不出「按判据」与「恒藏」）
                    di.State = DeckInfoPopup.DeckInfoState.Edit;
                    di.ApplyStateVisibility();
                    CheckTrue(di.IsItemShown("Btn:Edit Deck") && di.IsItemShown("Opt:Delete")
                              && di.IsItemShown("Opt:Duplicate") && di.IsItemShown("Opt:Share")
                              && di.IsItemShown("Opt:Share On Chat"),
                              "把 `State` 改回 **0** ⇒ `Edit Deck` 与 4 颗圆钮**都回来了**（两态对比）");
                    di.State = DeckInfoPopup.DeckInfoState.View;
                    di.ApplyStateVisibility();

                    // ---- A65④②：`Switch Deck Info` 建好即 `Toggle.SetIsOnWithoutNotify(true)` ----
                    CheckTrue(di.DrawerToggleIsOn,
                              "★ `Switch Deck Info` 的 toggle 出厂 **ON**（原版 `Toggle.SetIsOnWithoutNotify(true)`，"
                              + "`:210-211`）—— 删掉 `ApplyControlStates` 里那句 ⇒ 默认 `false`、这条红");
                    di.SwitchDrawer();
                    CheckTrue(!di.DrawerToggleIsOn, "…点一下（= uGUI 的 `isOn` 翻转 → `onValueChanged`）⇒ 翻成 `false`"
                                                    + "（**断它对** ⇒ 写死 `true` 的实现在这里红）");
                    di.SwitchDrawer();
                    CheckTrue(di.DrawerToggleIsOn, "…再点一下 ⇒ 翻回来");

                    // ---- A65④③：两颗圆钮（本夹具 4 套 ⇒ 两边都是真）----
                    CheckTrue(di.DeleteInteractable && di.DuplicateInteractable,
                              "4 套卡组 ⇒ `Delete`（`1 < 卡组数`）与 `Duplicate`（`卡组数 < 114`）**都可点**"
                              + "（两个边界另用临时存档验，见下面那一节）");

                    // ---- A65④①（阴性）：第 0 套只有 4 张卡 ⇒ 原版那颗 `Practice Deck` 是灰的、点了不生效 ----
                    CheckTrue(!di.PracticeDeckInteractable,
                              "★ 第 0 套只有 4 张卡 ⇒ `Practice Deck` 的 `interactable = false`"
                              + "（原版 `DeckUtility.ValidateDeck(deck, …)` —— 我们这一侧的同一份判据 = `DeckRules.Validate`）");
                    DeckInfoPopup.LastOpponentSelection = null;
                    {
                        var pd0 = di.Btn("Practice Deck");
                        var pd0b = pd0 != null ? pd0.GetComponent<WindowButton>() : null;
                        CheckTrue(pd0b != null, "`Practice Deck` 有点击区");
                        if (pd0b != null) pd0b.ClickForTest();
                        CheckTrue(di.CurrentState == WindowState.Open && DeckInfoPopup.LastOpponentSelection == null,
                                  "★ 点它 ⇒ **什么都没发生**（窗没关、挑对手那扇也没开）—— `interactable = false` 的语义"
                                  + "（原版 UGUI：`Selectable.OnPointerClick` 头一句就 `return`）");
                    }
                }
                // ---- A65④①（阳性）：**合法 30 张**那副（夹具第 4 套）⇒ 可点 ----
                //   没有这一条，上面那条「假」就是**恒假也全绿**（分不出「按判据算的」与「写死的 false」）。
                {
                    Check(CollectionData.DeckAt(3).Count, 30,
                          "夹具那副**合法**卡组在 3 号位（30 张同阵营 unit）");
                    var legalPop = DeckInfoPopup.Create(pw.Manager, 3, DeckInfoPopup.DeckInfoState.View);
                    pw.Manager.OpenWindow(legalPop);
                    CheckTrue(legalPop.PracticeDeckInteractable,
                              "★ **合法卡组** ⇒ `Practice Deck` **可点**（同一条判据的阳性那一半）");
                    legalPop.Close();
                }
                // ---- A65④③：两颗圆钮的**两个边界**（`1 < 卡组数` / `卡组数 < 114`）----
                //   🔴 为什么单开这一节：夹具那 4 套**两边都是真**，分不出「按判据算的」与「写死的 true」。
                //   做法 = 把 `DeckStore.OverridePath` 指到**另一个**临时存档，造 1 套 / 造 114 套各断一次，
                //   断完**指回夹具那一份**（后面几节还在用那 4 套）—— 玩家的真存档与夹具**一个字都没动**。
                {
                    string keepPath = DeckStore.OverridePath;
                    DeckStore.OverridePath = "d:/4/_tmp_view/menu/_menu_boundary_decks.json";
                    try { System.IO.File.Delete(DeckStore.OverridePath); } catch { }
                    CollectionData.ResetForTest();
                    DeckLibrary.Load().Create("边界：只剩这一套");
                    CollectionData.ResetForTest();
                    Check(CollectionData.DeckCount(), 1, "边界夹具：库里正好 **1** 套（下面几条的前提）");

                    var one = DeckInfoPopup.Create(pw.Manager, 0);
                    pw.Manager.OpenWindow(one);
                    CheckTrue(!one.DeleteInteractable,
                              "★ **卡组数 = 1** ⇒ `Delete` **不可点**（原版 `1 < 卡组数` —— 只剩一套不许删；`<` 不是 `<=`）");
                    CheckTrue(one.DuplicateInteractable, "…而 `Duplicate` 可点（1 < 114）");
                    var del = one.Opt("Delete");
                    var delb = del != null ? del.GetComponent<WindowButton>() : null;
                    CheckTrue(delb != null, "`Delete` 有点击区");
                    if (delb != null) delb.ClickForTest();
                    Check(CollectionData.DeckCount(), 1, "★ 点它 ⇒ **什么都没发生**（真删了 = 这道闸门没接上）");
                    Check(one.CurrentState, WindowState.Open, "…窗也没被关掉（`Close()` 只在真删成功时才走）");

                    var big = DeckLibrary.Load();
                    for (int k = 1; k < 114; k++) big.Create("边界 " + k);
                    CollectionData.ResetForTest();
                    Check(CollectionData.DeckCount(), 114,
                          "边界夹具：库里 **114** 套（= 上限。原版 `GameStaticData.totalCustomDecks`："
                          + "字段偏移 **0x250**，`GameStaticData__.cctor.c:308` 给的是 **`0x72` = 114**；"
                          + "邻居 `+0x248` = `maxItemsToShowInChat` = 150 · `+0x254` = `maxTranslateTaps` = 5 逐条对得上）");
                    Check(DeckInfoPopup.MaxCustomDecks, 114,
                          "…而实现里那个常量就是 **114**（⛔ 不许自己挑一个数 —— 出处同上）");
                    var full = DeckInfoPopup.Create(pw.Manager, 0);
                    pw.Manager.OpenWindow(full);
                    CheckTrue(!full.DuplicateInteractable,
                              "★ **卡组数 = 114 = 上限** ⇒ `Duplicate` **不可点**（原版 `卡组数 < totalCustomDecks`）");
                    CheckTrue(full.DeleteInteractable, "…而 `Delete` 可点（114 > 1）");
                    full.Close();
                    one.Close();

                    DeckStore.OverridePath = keepPath;     // **必须**：指回夹具那一份
                    CollectionData.ResetForTest();
                    Check(CollectionData.DeckCount(), 4, "存档已指回夹具（4 套：带督军的 / 两套空 / 一副合法 30 张）");
                }
                // ⚠️ 它是模态窗，留着会盖住后面的截图 —— 断完就关（同 `PromptPopup` 那条）
                if (PracticeModePopup.LastDeckInfo != null) PracticeModePopup.LastDeckInfo.Close();

                // ---------------- `Deck List Drawer`（原版出厂关着的那个抽屉）----------------
                // 开关只有 `DeckGeneralInfoDemo.Toggle(bool)` 一个入口，而它**在本地全量反编译里找不到调用者**
                //   （`grep -l DeckGeneralInfoDemo__Toggle *.c` 只命中它自己）⇒ 如实记着，别自己给玩家编一个入口。
                Section("`Deck List Drawer`（原版出厂关着的抽屉 · 开关 = `Toggle(bool)`）");
                CheckTrue(pw.ToggleDeckInfo() == false, "切一次 ⇒ 落到**卡列表**那一侧");
                {
                    var gen = FindChild(pw.transform, "General container");
                    var dl = FindChild(pw.transform, "Deck List Drawer");
                    CheckTrue(gen != null && !gen.gameObject.activeSelf && dl != null && dl.gameObject.activeSelf,
                              "切完两个抽屉还是**互斥**（总览关、卡列表开）—— 原版 `Toggle` 那两句");
                    CheckTrue(FindChild(dl, "ShowInfoHit") != null,
                              "卡列表那一侧有自己的钮（原版 `Show Deck General Info button`，点它切回总览）");
                    Shoot("02b_练习窗_卡列表抽屉.png");     // 给下个会话留一张「另一个抽屉」的实拍
                }
                CheckTrue(pw.ToggleDeckInfo(), "再切一次 ⇒ 回到**总览**（`Change Deck` 就在这一侧）");
                var cdHit = FindChild(pw.transform, "ChangeDeckHit");
                var cdBtn = cdHit != null ? cdHit.GetComponent<WindowButton>() : null;
                CheckTrue(cdBtn != null, "`Change Deck` 有点击区（在 `General container` 里）");
                if (cdBtn != null)
                {
                    cdBtn.Click();
                    var ds = PracticeModePopup.LastDeckSelection;
                    CheckTrue(ds != null, "点 `Change Deck` ⇒ **开出了 `Deck Selection Popup with Tabs`**");
                    if (ds != null)
                    {
                        Check(ds.type, WindowType.Popup, "`type` = **1 Popup**（A1 §3 原文）");
                        Check(ds.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**（原文）");
                        Check(ds.closeOnEsc, true, "`closeOnESC` = **1**（原文）");
                        Check(DeckSelectionPopup.Cols, 6,
                              "列数 = **6** = floor(1565 ÷ (225 + 20))（`_controlSegmentSize=1` ⇒ 按宽度算）");
                        CheckNear(DeckSelectionPopup.PadX, 57.5f, 0.1f,
                                  "内容居中左边距 = **57.5** = (1565 − (6×225 + 5×20)) / 2");
                        var svh = FindChild(ds.transform, "Deck Scroll View");
                        CheckAt(svh, 194.50f, 1759.50f, 208.63f, 986.69f,
                                "`Deck Scroll View`（RSR 视口 **1565 × 778.06**）");
                        // 🔴 **2026-09-26 起手页签归位**：原版 `DeckSelectionTabController.Start()` 唯一一句
                        //    就是 `ShowPrebuiltDecks(true)` ⇒ 出厂落在**预组**页。下面这一段原来是在「我的卡组」
                        //    页上验几何/抽签的 ⇒ **先切过去**再照旧。
                        Check(ds.OwnDecks, false, "起手落在**预组卡组**页（原版 `Start()` → `ShowPrebuiltDecks(true)`）");
                        ds.SwitchTab(true);
                        Check(ds.ShownCount, CollectionData.DeckCount(),
                              $"切到「我的卡组」⇒ 列出全部 {CollectionData.DeckCount()} 套");
                        if (ds.Cells.Count > 0)
                        {
                            var c0 = ds.Cells[0];
                            // 世界坐标 → 画布 px（同 `CollectionScene` 的 `PxOf/PxYOf`；本文件没有那两个助手）
                            CheckNear(c0.position.x * 108f + 960f, 364.5f, 0.7f,
                                      "第 1 格中心 x = **364.5**（194.50 + pad 57.5 + 225/2）");
                            CheckNear(540f - c0.position.y * 108f, 390.88f, 0.7f,
                                      "第 1 格中心 y = **390.88**（208.63 + 364.5/2）");
                        }
                        // 抽一套（原版 `RandomizeDeck`：从当前列表随机挑 ⇒ **选中并关窗**）
                        var rnd = ds.RandomHit;
                        var rndBtn = rnd != null ? rnd.GetComponent<WindowButton>() : null;
                        CheckTrue(rndBtn != null, "`Random` 有点击区");
                        if (rndBtn != null)
                        {
                            rndBtn.Click();
                            Check(ds.CurrentState, WindowState.Closed, "点 `Random` ⇒ **关窗**（原版 `Select` = 关窗 + 回调）");
                            CheckTrue(pw.DeckIndex >= 0 && pw.DeckIndex < CollectionData.DeckCount(),
                                      $"回调把选中的那套交回练习窗（现在是第 {pw.DeckIndex + 1} 套：「"
                                      + CollectionData.DeckAt(pw.DeckIndex).Name + "」）");
                        }
                        // 再开一次，验：起手页签 / 预组那一页 / 搜索 / 点一格选中 / 关闭钮
                        var ds2 = pw.OpenDeckSelection();
                        if (ds2 != null)
                        {
                            // ---- ① 起手页签：**原版出厂就是「预组」页**（`DeckSelectionTabController__Start.c:5`）----
                            Check(ds2.OwnDecks, false, "开窗**起手落在「预组卡组」页**（原版 `Start()` → `ShowPrebuiltDecks(true)`）");
                            // 🔴 **层序 —— 2026-10-11（A283）用户裁定「照原版」⇒ 已【反转】**：
                            //    **选卡组弹窗压在顶栏之上**（页面签条 y 35.07~107.25 与顶栏 y 0~100 重合，
                            //    现在**看得见、也点得到**的是页签；顶栏那一条被本弹窗的压暗层压暗）。
                            //    反面（2026-09-28 那一版「顶栏压住弹窗」）已按用户裁定推翻，
                            //    判据四条 → `MainMenuRuntime.QBarPanel` 那段注释 · `资料/普查产出_1010/V4b_三件口径.md` §Q3。
                            //    ⛔ 期望值是**弹窗自己的档常量**（`DeckSelectionPopup.QDs`），不是从主菜单那边读的。
                            CheckTrue(MainMenuRuntime.QBarOverlay < DeckSelectionPopup.QDs,
                                      $"**选卡组弹窗压住整条顶栏**（顶栏 {MainMenuRuntime.QBarPanel}~{MainMenuRuntime.QBarOverlay}"
                                      + $" < 弹窗 {DeckSelectionPopup.QDs}）");
                            // 光比常量还不够（同式自证）⇒ 现场量一次**这一点上谁吃到了**：
                            //   取顶栏那一点（齿轮中心），吃到的那一颗**必须在这扇弹窗的树里**、**不能是顶栏件**。
                            //   改坏法：`QBar*` 改回 3600 段 ⇒ 这里立刻返回顶栏那颗齿轮 ⇒ 两条都红。
                            {
                                var plDs = PointerLayer.Instance;
                                var hitDs = plDs != null ? plDs.ButtonAt(1847.0f, 35.5f) : null;
                                CheckTrue(hitDs != null && hitDs.transform.IsChildOf(ds2.transform),
                                          "★ 顶栏那一点（齿轮中心 1847,35.5）**吃到的在选卡组弹窗这棵树里**"
                                          + "（实得 " + (hitDs == null ? "<null>（这一点上没有任何命中区）"
                                                                     : "`" + hitDs.name + "`") + "）");
                                CheckTrue(hitDs != null && !hitDs.transform.IsChildOf(bar),
                                          "★ …而且**不再是顶栏件**（顶栏整条已降到弹窗之下）");
                            }

                            // ---- ② 预组页的内容：**两种模式都列** + 拼得齐 + 按原版难度序 ----
                            var tab = PrebuiltDecks.Tab;
                            CheckTrue(PrebuiltDecks.Available, "预组数据读到了（`Resources/prebuilt_decks.json`）");
                            Check(tab.Count, ds2.ShownCount, "预组页列出的条数 = `PrebuiltDecks.Tab` 的条数");
                            CheckTrue(tab.Count > 0, "预组页**不是空的**了（原来恒 0，数据没接）—— 现在 " + tab.Count + " 副");
                            int badMode = 0, badComplete = 0, badZh = 0, badSort = 0, badNoCbName = 0;
                            var ownCbMissing = new List<string>();     // 这副牌**自己的**卡背图没有
                            var noArtAtAll = new List<string>();       // 连「阵营默认卡背」兜底都没有 ⇒ 真画不出来
                            for (int i = 0; i < tab.Count; i++)
                            {
                                var d = tab[i];
                                // 🆕 2026-09-26：原来这条是「必须 == 0（只列经典）」——
                                //   现在**两种模式都列**（引擎支持遭遇模式之后照原版全列），
                                //   所以判据改成「只能是这两个合法值」，别的一个都不许混进来。
                                if (d.gameMode != 0 && d.gameMode != (int)GameMode.Skirmish) badMode++;
                                if (!d.complete) badComplete++;
                                if (string.IsNullOrEmpty(d.nameZh)) badZh++;
                                if (string.IsNullOrEmpty(d.cardback)) badNoCbName++;
                                if (CardArt.Cosmetic(d.cardback) == null) ownCbMissing.Add(d.deckId);
                                if (d.Cardback == null) noArtAtAll.Add(d.deckId);
                                if (i > 0)
                                {
                                    var p = tab[i - 1];
                                    if (p.difficulty > d.difficulty ||
                                        (p.difficulty == d.difficulty && p.armyOrder > d.armyOrder)) badSort++;
                                }
                            }
                            Check(badMode, 0, "预组页里只有两种合法模式（`gameMode` 0 = 经典 / 13 = 遭遇）"
                                            + "—— 12 张那批 2026-09-26 起**列出来了**（引擎支持了，不再是出声的偏离）");
                            Check(badComplete, 0, "预组页里**每个都拼得齐**（`complete`）—— 对应原版那句 `Where(!HasHiddenCards)`");
                            Check(badZh, 0, "每个都有**中文名**（没有的回落英文原名）");
                            Check(badNoCbName, 0, "每个都有**卡背名**（`cardback` 列 88 副 + 由 `cardbackId` 查表补的 15 副）");
                            Check(badSort, 0, "排序 = **`difficulty` 升序 → `CardArmy` 升序**（原版 `OrderBy`/`ThenBy` 两个键都升序）");
                            // 🔴 这两条**把已知缺口钉住**（不是断言 0 —— 那张图本地确实没有）。名单一变就红，必须回来看。
                            //    出处 → `资料/预组卡组_原版规格.md` §六 欠账那条。
                            // 🔴 2026-09-26：本页从 29 副（只经典）扩到 66 副（经典 + 遭遇）时，
                            //    原来躲在「不显示」后面的 6 条卡背命名缺口一起露了出来 —— 已全部解决
                            //    （5 条按命名规律接别名 + 1 条同阵营替身，见 `gen_prebuilt_decks.py` 的那两张表）。
                            //    ⇒ 现在这两条是**不许再有缺口**。
                            ownCbMissing.Sort(); noArtAtAll.Sort();
                            Check(ownCbMissing.Count, 0,
                                  $"{tab.Count} 副**统统**取得到自己那张卡背图 —— 实得 " +
                                  (ownCbMissing.Count == 0 ? "（一副都不缺）" : string.Join("、", ownCbMissing.ToArray())));
                            Check(noArtAtAll.Count, 0,
                                  "**画不出卡背的一副都没有** —— 实得 " +
                                  (noArtAtAll.Count == 0 ? "（一副都不缺）" : string.Join("、", noArtAtAll.ToArray())));
                            var subs = new List<string>();
                            for (int i = 0; i < tab.Count; i++) if (tab[i].cardbackFrom == "substitute") subs.Add(tab[i].deckId);
                            subs.Sort();
                            // ⚠️ 这一串**必须逐字对**（多一副少一副都说明替身表被动过）—— 4 副：
                            //    前两副是 09-26 用户拍板选的；后两副是「遭遇那批列出来」之后露出来的
                            //    （`ASH_SK_2` 的 `Warlord_Anvirr` 本地连相近的都没有 ⇒ 同阵营督军主题顶上）。
                            CheckTrue(string.Join(",", subs.ToArray()) == "ASH_SK_2,OrksDeck2,SpaceWolvesDeck5,SW_SK_3",
                                      "用**同阵营替身**的正好是那 4 副（`OrksDeck2`→Goff 督军主题 · `SpaceWolvesDeck5`→太空野狼「凶暴」·"
                                    + " `SW_SK_3`→凶暴 · `ASH_SK_2`→灵族另一张督军主题）—— 实得 " +
                                      (subs.Count == 0 ? "（一副都没有）" : string.Join("、", subs.ToArray())));
                            int dropped = PrebuiltDecks.NotListed;
                            PrebuiltDecks.CountByMode(out int cbc, out int sbc);
                            CheckTrue(cbc + sbc == tab.Count && sbc > 0,
                                      $"预组页**两种模式都列**（经典 {cbc} + 遭遇 {sbc} = {tab.Count}）—— "
                                    + "遭遇那批是 2026-09-26 引擎支持之后才列出来的");
                            CheckTrue(ds2.ScopeText.Contains("本页列 " + tab.Count),
                                      "红底板下沿那行小字**说清了列了多少 / 藏了多少**：" + ds2.ScopeText);
                            CheckTrue(ds2.EmptyText.Length == 0, "预组页非空 ⇒ **空态那行字不显示**");

                            // ---- ④ **搜索框不建**（2026-09-26 用户拍板「按照原版设计」）----
                            //    原版出厂 `act=N`，且四条证据都指向「没有任何代码打开它」（见 `DeckSelectionPopup.Search` 那段注释）
                            CheckTrue(ds2.SearchHit == null, "窗上**没有** `SearchHit`（搜索框不建）");
                            CheckTrue(ds2.transform.Find("Search Text") == null, "也没有 `Search Text` 那行字");
                            CheckTrue(ds2.transform.Find("InputFieldBackground") == null, "连输入框底板都没建（原版那 4 个节点一个不建）");
                            Check(ds2.ShownCount, tab.Count, "没有搜索 ⇒ 预组页恒列 " + tab.Count + " 副");

                            // ---- ⑤ 切到「我的卡组」⇒ 老的搜索断言照旧 ----
                            var tabOwn = ds2.TabHit(true);
                            var tabOwnBtn = tabOwn != null ? tabOwn.GetComponent<WindowButton>() : null;
                            CheckTrue(tabOwnBtn != null, "「我的卡组」页签有点击区");
                            if (tabOwnBtn != null)
                            {
                                tabOwnBtn.Click();
                                Check(ds2.OwnDecks, true, "点它 ⇒ 切到**我的卡组**那一页");
                                CheckTrue(ds2.ScopeText.Length == 0, "「我的卡组」页**不显示**那行范围小字（只预组页有）");
                                var c1 = ds2.Cells.Count > 0 ? ds2.Cells[0] : null;
                                CheckTrue(c1 != null && FindChild(c1, "DificultyLevel") == null,
                                          "「我的卡组」页**不画难度角标**（原版 `DeckCollectionDisplay.displayDifficultyLabel` 在这一页是 0）");
                                CheckTrue(c1 != null && FindChild(c1, "Game Mode Icon") == null,
                                          "「我的卡组」页**不画模式图标**"
                                          + "（⚠️ 2026-09-26 订正：**理由换了** —— 原来写「玩家自己的卡组没有 `gameMode` 这个概念」"
                                          + "**是错的**：原版 `CardDeck.gameMode` @0x70 就是玩家卡组的字段。"
                                          + "真正的理由是**那一格本来就不画**：全库没有 `ItemDrawer<CardDeck>`（只有 `DeckDrawer : ItemDrawer<PrebuiltDeck>` 带 `gameModeIcon`），"
                                          + "`DeckCollectionTab` 的字段里也没有图标。判据 → `资料/加时与冲突模式_原版规格.md` §2.7）");
                                Check(ds2.ShownCount, CollectionData.DeckCount(),
                                      $"「我的卡组」页列出全部 {CollectionData.DeckCount()} 套（没有搜索 ⇒ 不过滤）");
                                CheckTrue(ds2.transform.Find("DificultyLevel") == null
                                          || FindChild(c1, "DificultyLevel") == null,
                                          "（复查）「我的卡组」页仍不画难度角标");
                            }
                            Shoot("03_选卡组弹窗.png");

                            // ---- ⑥ 切回「预组」+ **点一副** ⇒ 回调拿到的是预组（原来这里会静默无事发生）----
                            ds2.SwitchTab(false);
                            Check(ds2.OwnDecks, false, "切回「预组卡组」页");
                            var first = PrebuiltDecks.Tab[0];
                            CheckTrue(ds2.Cells.Count > 0, "预组页**画出了格子**（" + ds2.Cells.Count + " 个）");

                            // ---- ⑥b 格子上的三层图标：阵营（**左下**）· 模式 · 难度角标 ----
                            var c0 = ds2.Cells.Count > 0 ? ds2.Cells[0] : null;
                            var facI = c0 != null ? FindChild(c0, "Faction") : null;
                            CheckTrue(facI != null, "格子上**画了阵营图标**");
                            if (facI != null)
                            {
                                // 世界坐标 → 画布 px（同 `CollectionScene` 的 `PxOf/PxYOf`）
                                float fx = facI.position.x * 108f + 960f, fy = 540f - facI.position.y * 108f;
                                CheckTrue(fx < 500f && fy > 450f,
                                          "阵营图标在**左下**（原版 `Faction Icon [-10.5,273.7]`，`资料/说明书/04_界面UI/卡组界面说明书.md:56` 原话「左下阵营图标」）" +
                                          " —— 实得中心 (" + fx.ToString("F1") + ", " + fy.ToString("F1") + ")；" +
                                          "🔴 原来我们画在**右上**且无断言，2026-09-26 更正");
                            }
                            var gmI = c0 != null ? FindChild(c0, "Game Mode Icon") : null;
                            var gq = gmI != null ? gmI.GetComponent<ImageQuad>() : null;
                            // 🔴 **图标名必须由那一格的 `gameMode` 推出来**（原版就是 `GetGameModeIcon(gameMode)`）——
                            //    原来这里**写死** `40k_gamemode_icon_classic`、还配了句「第一副是经典模式」；
                            //    2026-09-26 页里混进遭遇副之后第一副变成了 `ASH_SK_1`（也 diff 5、也 army 30），
                            //    写死就红了 —— **这正是「别把数据的一个快照写进断言」那条**。
                            string wantGmIcon = tab[0].gameMode == (int)GameMode.Skirmish
                                              ? "40k_gamemode_icon_skirmish" : "40k_gamemode_icon_classic";
                            CheckTrue(gq != null && gq.Texture != null && gq.Texture.name == wantGmIcon,
                                      $"格子上画了**模式图标** = `{wantGmIcon}`（第一副 `{tab[0].deckId}` 的 `gameMode` = "
                                    + $"{tab[0].gameMode}）—— 判据是**从数据推的**，不是写死某一档");
                            var dfI = c0 != null ? FindChild(c0, "DificultyLevel") : null;
                            var dq = dfI != null ? dfI.GetComponent<ImageQuad>() : null;
                            CheckTrue(dq != null && dq.Texture != null && dq.Texture.name == "Menu_Icon_Gallons_1",
                                      $"**难度角标**画了，第一副难度 {tab[0].difficulty} ⇒ `Menu_Icon_Gallons_1`（一条杠）"
                                    + "（节点名 `DificultyLevel` 是**照抄原版的拼写**，别改成 Difficulty）");

                            ds2.Pick(new DeckSelectionPopup.DeckPick
                            {
                                Prebuilt = true,
                                Info = DeckSelectionPopup.InfoOf(first),
                                OwnIndex = -1,
                                PrebuiltDeck = first,
                            });
                            CheckTrue(pw.PickedPrebuilt != null && pw.PickedPrebuilt.deckId == first.deckId,
                                      "点一副预组 ⇒ **回调把那一副交出去了**（`PickedPrebuilt` = " + first.deckId + "）");
                            // ---- ⑥b 「本局用这副牌」通道：写进去 = **开战链真能用它**（原版走 `SetPlayerDeck`，没有这条分支）----
                            var pend = PrebuiltDecks.PendingSource;
                            CheckTrue(pend != null && pend.deckId == first.deckId,
                                      "选中预组 ⇒ **写进了「本局用这副牌」通道**（`PendingSource`）—— 开战不再只认 `DeckLibrary.Current`");
                            var pdck = PrebuiltDecks.ToPlayerDeck(first);
                            CheckTrue(pdck.WarlordId == first.heroId,
                                      "搓出来的 `PlayerDeck` 督军 = 预组的督军（**督军不占 30 张位**）");
                            CheckTrue(pdck.DefensiveId == first.defensiveId,
                                      "防御卡 = **我们补的那张**（" + first.defensiveNameZh + " " + first.defensiveId +
                                      "）—— 原版预组那份是 null（反汇编证实），**加它是我们的选择**");
                            Check(pdck.CardIds.Count, first.cardIds.Length, "普通卡位 = 预组卡表长度（督军/防御卡都不在内）");
                            CheckTrue(pdck.CardbackId == first.cardback, "卡背 = 原版那副牌自己的卡背");
                            var took = PrebuiltDecks.TakePendingBattleDeck();
                            CheckTrue(took != null && took.Name == pdck.Name,
                                      "`TakePendingBattleDeck()` 拿得到 —— 开局那条路读的就是它");
                            CheckTrue(PrebuiltDecks.TakePendingBattleDeck() == null,
                                      "**读一次就清** —— 下一局不会再带上上一局挑的预组牌");
                            Check(ds2.CurrentState, WindowState.Closed, "选完 ⇒ **窗自己关上**（原版 `Select` 的两步）");

                            // ---- ⑦ 再开一次，验关闭圆钮 ----
                            var ds3 = pw.OpenDeckSelection();
                            if (ds3 != null)
                            {
                                // 🆕 A47：压暗层命中区 —— 档 = `QDs`(3125)，< 内容命中区档 `QDsHit`(3128)
                                //   （改前是 `QDsHit − 1` = 3127 = `QDsText` ⇒ 落在**文字那一档**上）
                                //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
                                MenuDraw.CheckShadeRule(CheckTrue, "选卡组窗", ds3.ShadeHit,
                                                        ds3.transform.Find("Menu Dark Background"),
                                                        DeckSelectionPopup.QDsHit);
                                CheckTrue(ds3.SearchHit == null, "（复查）**搜索框确实不建**（照原版 `act=N`，且无代码打开它）");
                                var cls = ds3.CloseHit;
                                var clsBtn = cls != null ? cls.GetComponent<WindowButton>() : null;
                                if (clsBtn != null) clsBtn.Click();
                                Check(ds3.CurrentState, WindowState.Closed, "点关闭圆钮 ⇒ 窗关上");

                                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒
                                //   原版什么都不发生）。期望矩形 = **原版 prefab**
                                //   `Deck Selection Popup with Tabs > Generic Window Red Background Big`
                                //   那颗 `Image` 的 rect（134.50,82 → 1839.50,1032）；⛔ 不写
                                //   `DeckSelectionPopup` 里那些常量（那是被测实现**传进去的实参**）。
                                var ds4 = pw.OpenDeckSelection();
                                CheckTrue(ds4 != null && ds4.CurrentState == WindowState.Open,
                                          "（A94 现场）又开出一扇选卡组窗");
                                if (ds4 != null)
                                    CheckAbsorbRule("选卡组窗", ds4.transform, "AbsorbHit",
                                                    134.50f, 82f, 1839.50f, 1032f,
                                                    DeckSelectionPopup.QDs, DeckSelectionPopup.QDsHit,
                                                    () => ds4.CurrentState);
                            }
                        }
                    }
                }
                // 🆕 **2026-10-06（A94 相 2）**：本窗有**两块互不相连**的面板底图 ⇒ 两层吸收层各断一次
                //   （点窗内空白处 ⇒ 原版什么都不发生）。期望矩形 = **原版 prefab** 里那两颗 `Image` 的 rect：
                //     ① `Deck Selector > Deck Buttons > Generic Window Red Background Small` = 242.94,208.83→657.98,861.25
                //     ② `Deck info > Generic Window Red Background Big`              = 761.63,187.00→1831.93,869.00
                //   ⛔ 不写 `PracticeModePopup.DbtnL…` / `BgBigL…` —— 那是被测实现**传进去的实参**（同式自证）。
                //   ⚠️ 这一组**结尾就把窗关掉**（「点面板外 ⇒ 关」那一步）⇒ 每条之间都要开回来；
                //      `PracticeModePopup.Open()` = `Build()` **重建**（不依赖 `Data`）⇒ 重开安全。
                CheckAbsorbRule("练习窗（选卡组那一列）", pw.transform, "AbsorbHit",
                                242.94f, 208.83f, 657.98f, 861.25f,
                                PracticeModePopup.QPr, PracticeModePopup.QPrHit, () => pw.CurrentState);
                CheckTrue(pw.TryOpen(null), "（A94）把练习窗开回来 —— 同一扇窗的第二块面板还要再断一次");
                CheckAbsorbRule("练习窗（右半那块红底）", pw.transform, "AbsorbHitDeckInfo",
                                761.63f, 187.00f, 1831.93f, 869.00f,
                                PracticeModePopup.QPr, PracticeModePopup.QPrHit, () => pw.CurrentState);
                CheckTrue(pw.TryOpen(null), "（A94 收尾）再开回来 —— 下面点 `Back` 那条要在**开着**的窗上点");
                pw.ToggleDeckInfo();      // 抽屉收回去（后面那张练习窗的截图不该带抽屉）
                var bk = pw.BackHit;
                var bkb = bk != null ? bk.GetComponent<WindowButton>() : null;
                if (bkb != null) bkb.Click();
                Check(pw.CurrentState, WindowState.Closed, "点 `Back` ⇒ 窗关上");
            }
        }

        // ============================================================ 玩家档案窗（2026-09-27 建 · 多人界面第 2 件）
        // 判据：骨架 → 正本 `资料/阶段二_多人界面_原版规格.md` §2·1；页签键那一层 → `Shell/PlayerProfileWindow.cs` 文件头。
        // 🔴 **断的全是「原版参数」**，不是我们自己的常量（10·3 第 3 层）。
        Section("玩家档案窗 `Player Profile Window`（入口 = 顶栏头像）");
        {
            var avatarBorder = FindChild(FindChild(prof, "Avatar Item Small"), "Border");
            var avb = avatarBorder != null ? avatarBorder.GetComponent<WindowButton>() : null;
            CheckTrue(avb != null && avb.onClick != null,
                      "顶栏头像**接了点击**（原来没有 —— 点了什么都没发生，同齿轮当初那个静默失败）");
            if (avb != null && avb.onClick != null)
            {
                avb.onClick();
                var pp = PlayerProfileWindow.LastOpened;
                CheckTrue(pp != null && pp.CurrentState == WindowState.Open, "点头像 ⇒ **真的开了玩家档案窗**");
                if (pp != null)
                {
                    // 🆕 A47：压暗层命中区 —— 档 = `QShade`(3150)，< 内容命中区档 `QHit`(3155)
                    //   （改前是 `QShade + 1` = 3151 = 本窗 `QPanel`（红底**内容层**）⇒ 不合规矩）
                    //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
                    MenuDraw.CheckShadeRule(CheckTrue, "玩家档案窗", FindChild(pp.transform, "BackgroundHit"),
                                            pp.transform.Find("Menu Dark Background"), PlayerProfileWindow.QHit);
                    Check(pp.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(pp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**（原文）");
                    CheckTrue(pp.closeOnEsc, "`closeOnESC` = **1**（原文）");
                    CheckNear(pp.extraScaleSmallScreen, 1.075f, 1e-4f,
                              "`extraScaleSmallScreen` = **1.075**（⚠️ 不是 1.07 —— 那是练习窗的值，**逐窗实测**）");
                    Shoot("09_档案窗_Profile页.png");    // 六页都建完了，但原来**一张截图都没有**

                    var t = pp.transform;
                    CheckAtWorld(FindChild(t, "Menu Dark Background"), -1327.3f, 3247.3f, -746.18f, 1826.18f,
                                 "压暗层 `Menu Dark Background`（纯色 (0,0,0,0.7725)，**原版就没图**）");
                    CheckAtWorld(FindChild(t, "Menu Area"), 0f, 1920f, 0f, 1080f, "`Menu Area`（满屏容器）");
                    // ⚠️ `Tab  Area` 名字里是**两个空格**（原版就这么拼）
                    CheckAtWorld(FindChild(FindChild(t, "Menu Area"), "Tab  Area"),
                                 273.48f, 1824.52f, 118f, 962f, "`Tab  Area`（**名字两个空格**）");
                    var tabArea = FindChild(FindChild(t, "Menu Area"), "Tab  Area");
                    CheckAtWorld(FindChild(tabArea, "Generic Window Red Background Big"),
                                 273.48f, 1824.52f, 118.92f, 962f, "红底（`UI_Deck_Information_Back`，Sliced 九宫格）");
                    CheckAtWorld(FindChild(tabArea, "Generic Close Button Orange"),
                                 1760.80f, 1835.19f, 118.92f, 194.52f,
                                 "右上橙色关闭钮（⚠️ 原版**右溢出窗框 ~10.7px** —— 别「对齐」掉）");
                    CheckAtWorld(FindChild(tabArea, "Tab Content"), 351.03f, 1746.97f, 118.92f, 962f, "`Tab Content`");

                    // 左栏：`Tab Buttons` + 六个键（**位置是 VerticalLayoutGroup 算出来的**，正本 §2·1）
                    var bar2 = FindChild(FindChild(t, "Menu Area"), "Tab Buttons");
                    CheckAtWorld(bar2, 95.48f, 273.48f, 180.24f, 885.752f,
                                 "`Tab Buttons`（`VLG` spacing 10 / align 5 MiddleRight / ctrlH+expandH）");
                    for (int i = 0; i < PlayerProfileWindow.Tabs.Length; i++)
                    {
                        var spec = PlayerProfileWindow.Tabs[i];
                        float top = PlayerProfileWindow.BarT + PlayerProfileWindow.KeyStep * i,
                              bot = top + PlayerProfileWindow.KeyH;
                        var key = FindChild(bar2, spec.Node);
                        CheckAtWorld(key, PlayerProfileWindow.KeyL, PlayerProfileWindow.KeyR, top, bot,
                                     $"键 {i} `{spec.Node}`（布局后：165 × 109.252、y 从 180.24 步进 119.252）");
                        if (key == null) continue;
                        CheckAtWorld(FindChild(key, "Icon"), spec.IconL, spec.IconR, spec.IconT, spec.IconB,
                                     $"键 {i} 的 `Icon` 框（**六个键各不相同** —— 原版没挂 AspectRatioFitter，别统一）");
                        var lab = FindChild(key, "Tab Toggle Title");
                        CheckAtWorld(lab, PlayerProfileWindow.LabL, PlayerProfileWindow.LabR, spec.LabT, spec.LabB,
                                     $"键 {i} 的 `Label`（155 宽 **裸文字** —— 这里**没有**名字条底图）");
                        CheckText(TextOf(lab), spec.Label,
                                  $"键 {i} 文案 = `{spec.Label}`（⚠️ **键名与文案不一致**：`Trophies`→`Achievements`、`Ranked`→`Ranking`）");
                        // 🆕 **2026-10-08（波 C3 · A212）**：六颗页签的 `折行` —— 原版 **6/6 全是 `0`**，
                        //   而 `BuildTabBar` 里 `SetAutoFitBox` 会**无条件**把模式开成 `Normal`
                        //   ⇒ 本批在它之后补了显式 `SetWrapping(false)`。
                        //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
                        //   的 `折行=` 列（`Tab Buttons/*/Label/Tab Toggle Title` 逐行，六个键各一行）。
                        //   **怎么改坏就红**：把 `Shell/PlayerProfileWindow.cs` 里新加的 `lbl.SetWrapping(false);`
                        //   删掉 ⇒ 这一条 6 次全红（期望 0、实得 1）。
                        CheckWrapMode(lab, 0, $"★ 键 {i} 的 `Tab Toggle Title` **不折行**（原版 `折行=0`）");
                        // 🔴 **断「渲染出来的字放得进框」**，不是断字号（`AutoFitBox` 那条教训：字号对而溢出，
                        //    自检照样全绿 —— 2026-09-22 踩过）。原版这六条 TMP 是 **NoWrap + Overflow + autosize(10→35)**。
                        {
                            var lb = lab != null ? lab.GetComponent<Label>() : null;
                            float wPx = lb != null ? lb.WorldW * 108f : -1f;
                            float hPx = lb != null ? lb.WorldH * 108f : -1f;
                            float bw = PlayerProfileWindow.LabR - PlayerProfileWindow.LabL, bh = spec.LabB - spec.LabT;
                            CheckTrue(wPx > 0f && wPx <= bw + 0.5f,
                                      $"键 {i} 的「{spec.Label}」**渲染宽度 {wPx:F1}px ≤ 框宽 {bw}px**（原版 NoWrap+Overflow ⇒ 超了就画到键外面）");
                            CheckTrue(hPx > 0f && hPx <= bh + 0.5f,
                                      $"键 {i} 的「{spec.Label}」**渲染高度 {hPx:F1}px ≤ 框高 {bh:F2}px**");
                        }
                    }

                    // 六个页根：`Avatar`/`Title` 两个**两侧各溢 16.33**（真值）
                    CheckAtWorld(FindChild(t, "Profile Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Profile Tab` 页根");
                    CheckAtWorld(FindChild(t, "Avatar Tab"), 318.37f, 1746.97f, 118.92f, 962f,
                                 "`Avatar Tab` 页根（**两侧各溢 16.33** —— 真值，别「对齐」掉）");
                    CheckAtWorld(FindChild(t, "Title Tab"), 318.37f, 1746.97f, 118.92f, 962f, "`Title Tab` 页根（同上）");
                    CheckAtWorld(FindChild(t, "Battle Log Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Battle Log Tab` 页根");
                    CheckAtWorld(FindChild(t, "Trophies Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Trophies Tab` 页根");
                    CheckAtWorld(FindChild(t, "Ranked Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Ranked Tab` 页根");

                    // 🔴 **出厂打开的是 `Title` 页（第 3 个键），不是第一页** —— 唯一信号 = 原版只有
                    //    `Title Tab` 是 `m_IsActive=true`（`m_IsOn` 六个键全 0，数据里看不出哪个键亮着）
                    Check(pp.CurrentTab, WindowTabType.ProfileTitle, "出厂落在 **`Title` 页**（不是第一页）");
                    CheckTrue(FindChild(t, "Title Tab") != null && FindChild(t, "Title Tab").gameObject.activeSelf,
                              "`Title Tab` 页根是 **active** 的");
                    CheckTrue(FindChild(t, "Profile Tab") != null && !FindChild(t, "Profile Tab").gameObject.activeSelf,
                              "`Profile Tab` 页根出厂 **inactive**");

                    // 选中态：**只换贴图、不换色**（`EverguildToggle.colorTintOnValueChange=0` /
                    // `changeSpriteOnValueChange=1`，判据见 `Shell/PlayerProfileWindow.cs` 文件头 ①）
                    var texOn = CardArt.MenuUi(PlayerProfileWindow.ArtTabOn);
                    var texOff = CardArt.MenuUi(PlayerProfileWindow.ArtTabOff);
                    CheckTrue(texOn != null && texOff != null, "页签底两张图都在工程里（`40K_settings_button` / `_hover`）");
                    for (int i = 0; i < pp.ButtonBgs.Length; i++)
                    {
                        var bgq = pp.ButtonBgs[i];
                        bool shouldOn = i == PlayerProfileWindow.DefaultTabIndex;
                        CheckTrue(bgq != null && bgq.Texture == (shouldOn ? (Texture)texOn : texOff),
                                  $"键 {i} 的 `button_bg` 贴图 = **{(shouldOn ? "选中" : "未选")}**那张"
                                  + $"（{(shouldOn ? PlayerProfileWindow.ArtTabOn : PlayerProfileWindow.ArtTabOff)}）");
                        // `m_Color` **逐键不同**（第 1 键 a=1、其余 0.7098039388656616；rgb 六键都是 (1,0.5723677,0)）
                        float wantA = i == 0 ? 1f : 0.7098039388656616f;
                        CheckNear(bgq != null ? bgq.Tint.a : -1f, wantA, 1e-3f,
                                  $"键 {i} 的 `button_bg` alpha = **{wantA:F4}**（原版逐键真值，见 `PlayerProfileWindow.cs` 文件头的存疑那条）");
                    }

                    // 点第 1 个键 ⇒ 切到 `Profile` 页（换页只切 activeSelf，不重建）
                    {
                        var k0 = FindChild(bar2, PlayerProfileWindow.Tabs[0].Node);
                        var h0 = k0 != null ? FindChild(k0, "Hit") : null;
                        var w0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                        CheckTrue(w0 != null && w0.onClick != null, "第 1 个键有点击区");
                        if (w0 != null) w0.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileInfo, "点第 1 个键 ⇒ 切到 `Profile` 页");
                        CheckTrue(FindChild(t, "Profile Tab") != null && FindChild(t, "Profile Tab").gameObject.activeSelf,
                                  "切页之后 `Profile Tab` 页根 **active**");
                        CheckTrue(FindChild(t, "Title Tab") != null && !FindChild(t, "Title Tab").gameObject.activeSelf,
                                  "切页之后 `Title Tab` 页根 **关掉**");
                        CheckTrue(pp.ButtonBgs[0] != null && pp.ButtonBgs[0].Texture == texOn,
                                  "切页之后第 1 个键的底色 **翻成选中那张**");
                    }

                    // ---- Title 页（**出厂就打开的那一页**）----
                    {
                        var k2 = FindChild(bar2, PlayerProfileWindow.Tabs[2].Node);
                        var h2 = k2 != null ? FindChild(k2, "Hit") : null;
                        var w2 = h2 != null ? h2.GetComponent<WindowButton>() : null;
                        if (w2 != null) w2.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileTitle, "点第 3 个键 ⇒ 切回 `Title` 页");

                        // 🔴 **锚一律从页根开始** —— `FindChild(t, "Selected Item Panel")` 会先撞上
                        //    **别的页**的同名节点（六页都有 `Selected Item Panel` / `Item Display Panel` /
                        //    `Scroll Rect`）⇒ 第一版就这么红了三条（Title 块的断言全打在 Avatar 页上）。
                        var tp = FindChild(t, "Title Tab");
                        CheckTrue(tp != null, "`Title Tab` 页根在");
                        var sel = FindChild(tp, "Selected Item Panel");
                        CheckAtWorld(sel, 318.37f, 587.32f, 292.02f, 758.89f, "`Selected Item Panel`");
                        CheckAtWorld(FindChild(sel, "Avatar Name"), 322.54f, 587.32f, 516.41f, 631.55f,
                                     "`Avatar Name`（⚠️ 这一行装的是**称号名**，不是玩家名）");
                        CheckAtWorld(FindChild(sel, "Select Avatar Button"), 343.49f, 562.21f, 670.43f, 736.48f,
                                     "`Select Avatar Button`");
                        CheckText(TextOf(FindChild(sel, "Button Text")), "Select",
                                  "按钮文案 = `Select`（⚠️ **我们挑的**：原版那条是**葡语占位串 `Selecionar`**、还没挂 `Localize`）");
                        CheckTrue(FindChild(sel, "Toggle borde") == null,
                                  "`Toggle borde` **不建**（出厂 act=F + 全包无 MonoBehaviour 指向它 + 无 Animation ⇒ 死节点）");

                        // 🆕 **2026-10-08（波 C3 · A212）**：`TitleTab` 那两处（A62 子表 A 的 A47~A49）。
                        //   `ProfilePage.Text` 从本批起 **`autoFit` 不再隐含折行**（见那一件的报告 §A212），
                        //   这两颗的原版档位因此**真的落到**了下面写的值上（改之前它们靠 `SetAutoFitBox`
                        //   的副作用 = `Normal`）。
                        //   判据（两条命令、**分属两棵树**）：
                        //     · `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
                        //       ⇒ `Title Tab/Selected Item Panel/Select Avatar Button/Button Text`（`'Selecionar'`）
                        //         `字号=36.0 auto[10.0~36.0] 对齐=Center/Capline` · **`折行=0`**；
                        //     · `python 工具/menu_dump.py bundle_menus_assets_all "Title Drawer Horizontal Variant" --depth 6 --md`
                        //       ⇒ `Content > Label > Name`（出厂 `'TITLE'`）`字号=19.0 auto[12.0~75.0] 对齐=Center/Midline` · **`折行=0`**。
                        //   ⚠️ **怎么改坏就红**：把 `Shell/TitleTab.cs` 那两处的 `Text(...)` 补上 `wrap: true`
                        //      （或把 `ProfilePage.Text` 里新加的那句 `if (!wrap) lb.SetWrapping(false);` 删掉）
                        //      ⇒ 下面这两条**各红一条**（`WrappingMode` 实得 1、期望 0）。
                        CheckWrapMode(FindChild(sel, "Button Text"), 0,
                                      "★ `Select Avatar Button > Button Text` **不折行**（原版 `折行=0`）");

                        var disp = FindChild(tp, "Item Display Panel");
                        CheckAtWorld(disp, 632.79f, 1701.49f, 210.69f, 868.61f, "`Item Display Panel`");
                        var bgBig = FindChild(disp, "Background");
                        CheckAtWorld(bgBig, 632.79f, 1701.49f, 210.69f, 868.61f,
                                     "面板底（`UI_Deck_Information_submenu_Back` · Sliced · 九宫 18,18,18,18）");
                        var ttl = FindChild(disp, "Select Item");
                        // 🔴 **别用 `CheckAtWorld` 断它** —— 原版这行是**左对齐**（`m_HorizontalAlignment = Left`、
                        //    垂直 Middle），我们照做了 `alignLeft` ⇒ `Label` 会被推到「左边缘落在 654.16」，
                        //    **不是**矩形中心（第一版就这么断的，报了 117.99px 的假失败：那个差刚好 = 标签宽的一半）。
                        {
                            var lbTtl = ttl != null ? ttl.GetComponent<Label>() : null;
                            float leftWorld = ttl != null ? ttl.position.x - (lbTtl != null ? lbTtl.WorldW * 0.5f : 0f) : -99f;
                            float wantLeft = MainMenuRuntime.Center(654.16f, 654.16f, 0f, 0f).x;
                            CheckTrue(Mathf.Abs(leftWorld - wantLeft) < 0.02f,
                                      $"`Select Item` **左对齐**到 x=654.16（原版 `m_HorizontalAlignment=Left`；实得 {leftWorld * 108f + 960f:F2}px）");
                            float wantCy = MainMenuRuntime.Center(0f, 0f, 147.51f, 210.70f).y;
                            CheckTrue(Mathf.Abs(ttl.position.y - wantCy) < 0.02f,
                                      $"`Select Item` 垂直居中在 y 147.51..210.70（实得 {(540f - ttl.position.y * 108f):F2}px）");
                        }
                        CheckText(TextOf(ttl), "Select your title", "小标题文案");
                        CheckTrue(ttl != null && bgBig != null && ttl.position.y > bgBig.position.y,
                                  "🔴 `Select Item` **溢在面板之上**（真值：它顶边 147.51 比面板顶 210.69 还高）—— 别「修正」成对齐");
                        var scr = FindChild(disp, "Scroll Rect");
                        CheckAtWorld(scr, 654.16f, 1680.12f, 210.69f, 855.46f,
                                     "`Scroll Rect` 视口（纵向 · Clamped · inertia=1 · elasticity=0.1）");
                        CheckAtWorld(FindChild(scr, "Item Drawer"), 654.16f, 1698.81f, 210.70f, 210.70f,
                                     "`Item Drawer` 内容容器（宽 1044.65 ⇒ **比视口宽 18.69、两侧各溢 9.35**）");
                        // 清单：**原版资产清单**（不是玩家存档）—— 由 `工具/gen_profile_cosmetics.py` 抽自
                        // `素材/Warpforge原版/装饰品/定义数据/` 的 462 个称号 SO。
                        CheckTrue(ProfileData.Loaded,
                                  "称号/头像清单**读进来了**（`Resources/profile_cosmetics.json`；读不到会 `LogError`）");
                        Check(TitleTab.Titles.Count, 462, "称号条数 = **462**（本地 SO 的实数，470 头像同理）");
                        CheckTrue(ProfileData.Avatars.Count == 469,
                                  $"头像条数 = **469**（实数；已排除 `Avatar_WF_*` 两张占位图）——实得 {ProfileData.Avatars.Count}");
                        CheckTrue(TitleTab.Titles.Count == 0 || TitleTab.Titles[0].Name.Length > 0,
                                  "称号有显示名（**我们是从资源名反推的**：`Title_UM_Premium_1` → `UM Premium 1`，"
                                  + "原版真名在远端 I2 语言表）");
                        // 表格按视口裁：462 条 × 3 列 = 154 行，一屏只该建出看得见的那几行
                        var grid = FindChild(scr, "Item Drawer");
                        int built = grid != null ? grid.childCount : -1;
                        CheckTrue(built >= 1 && built < 30,
                                  $"`Item Drawer` 底下**只建了看得见的格子**（实得 {built} 个；462 条全建会卡）");
                        // 🆕 **2026-10-08（波 C3 · A212）**：格子里那行称号名 = 原版 **`折行=0`** ——
                        //   判据 = 模板 prefab **`Title Drawer Horizontal Variant`** 的
                        //   `Content > Label > Name`（出厂 `'TITLE'`）实读 **`折行=0`**
                        //   （`python 工具/menu_dump.py bundle_menus_assets_all "Title Drawer Horizontal Variant" --depth 6 --md`；
                        //   ⚠️ 同名实例有多份，读的是工具取的**第一份** `-6140934185811029764`，其余没逐份核）。
                        //   ⚠️ **这一条同时钉住一件事**：`TitleTab` 的**格子版式是我们挑的**（原版预制体里
                        //   0 个实例、由 `ItemDrawer.Draw` 运行期装），但**折行这一格现在有判据了** —— 别把
                        //   两者混起来说「整格都是我们挑的」。
                        //   **怎么改坏就红**：给 `Shell/TitleTab.cs` 那一处补 `wrap: true` ⇒ 期望 0、实得 1。
                        CheckWrapMode(grid != null && grid.childCount > 0 ? FindChild(grid.GetChild(0), "Name") : null, 0,
                                      "★ 称号格里的 `Name` **不折行**（原版模板 `Title Drawer Horizontal Variant/Content/Label/Name` = `折行=0`）");
                        // 🔴 **滚轮真的会重画**（2026-09-27 修：原来 `OnChanged` 是空的 ⇒ 滚了什么都不动，
                        //    而 462 条只建得出前几行 ⇒ **后面的称号根本够不到**，还是静默的）
                        {
                            var pl = PointerLayer.Instance;
                            float y0t = grid != null && grid.childCount > 0 ? grid.GetChild(0).position.y : 0f;
                            int n0t = grid != null ? grid.childCount : 0;
                            bool hit = pl != null && pl.WheelAt(1167f, 533f, -120f);
                            float y1t = grid != null && grid.childCount > 0 ? grid.GetChild(0).position.y : 0f;
                            CheckTrue(hit, "滚轮落在称号网格上（`PointerLayer.WheelAt`）");
                            CheckTrue(y1t > y0t + 0.01f,
                                      $"滚一格 ⇒ **内容真的往上走了**（world y {y0t:F3} → {y1t:F3} = {(y1t - y0t) * 108f:F1}px）");
                            CheckTrue(grid != null && grid.childCount == n0t,
                                      $"滚动重建之后格子数不变（{n0t}）—— 重建是**先清再建**（不清会越滚越多）");
                            if (pl != null) pl.WheelAt(1167f, 533f, 120f);   // 滚回去，后面的断言要确定性
                        }
                    }

                    CheckTrue(pp.MissingArt.Count == 0,
                              "这一扇用到的图**一张都不缺**（缺的会列在 `MissingArt`："
                              + string.Join("、", pp.MissingArt.ToArray()) + "）");
                    CheckHoverSwap(pp.transform, "提示窗");
                    // ⚠️ **`pp.Close()` 挪到所有页断完之后**（原来在这儿，加了 Avatar 页之后它会先关窗）
                    // ---- Avatar 页 ----
                    // 🔴 **锚一律从页根开始**（`FindChild(t, "Selected Item Panel")` 会先撞上别的页的同名节点 ——
                    //    六页都有 `Selected Item Panel` / `Item Display Panel` / `Scroll Rect`）。
                    {
                        var k1 = FindChild(bar2, PlayerProfileWindow.Tabs[1].Node);
                        var h1 = k1 != null ? FindChild(k1, "Hit") : null;
                        var w1 = h1 != null ? h1.GetComponent<WindowButton>() : null;
                        if (w1 != null) w1.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点第 2 个键 ⇒ 切到 `Avatar` 页");

                        var pg = FindChild(t, "Avatar Tab");
                        CheckTrue(pg != null, "`Avatar Tab` 页根在");
                        var sel2 = FindChild(pg, "Selected Item Panel");
                        CheckAtWorld(sel2, 318.37f, 587.32f, 292.02f, 758.89f, "`Avatar Tab/Selected Item Panel`");
                        var mi = FindChild(sel2, "Avatar Menu Item");
                        CheckAtWorld(mi, 318.37f, 585.45f, 360.15f, 633.73f, "`Avatar Menu Item`（大图预览那块）");
                        var ic2 = FindChild(mi, "Image Container");
                        CheckAtWorld(ic2, 318.37f, 585.45f, 360.15f, 573.73f, "`Image Container`（267.08×213.58）");
                        CheckAtWorld(FindChild(ic2, "Border"), 318.37f, 585.45f, 381.51f, 595.09f,
                                     "头像边框（`Player Profile Border` 256×286 · preserveAspect）");
                        var bighl = FindChild(ic2, "Highlight");
                        CheckTrue(bighl != null && !bighl.gameObject.activeSelf,
                                  "大图的 `Highlight` **恒关**（原版两个调用点都写死 `highlight=false`）");
                        CheckTrue(FindChild(ic2, "Image") != null && FindChild(ic2, "Image").GetComponentInChildren<ImageQuad>() != null,
                                  "大图那一层**真的画出来了**（有 `ImageQuad`，不是空节点）");
                        CheckAtWorld(FindChild(mi, "Avatar Name"), 303.37f, 600.45f, 253.13f, 321.03f,
                                     "大图的 `Avatar Name`（⚠️ 比父件宽 —— 真值）");
                        CheckTrue(TextOf(FindChild(mi, "Avatar Name")).Length > 0,
                                  "大图底下显示的是**选中那张头像的名字**");
                        CheckAtWorld(FindChild(sel2, "Select Avatar Button"), 343.49f, 562.21f, 670.43f, 736.48f,
                                     "`Select Avatar Button`");
                        CheckText(TextOf(FindChild(sel2, "Toggle borde") != null
                                         ? FindChild(FindChild(sel2, "Toggle borde"), "Button Text") : null),
                                  "Toggle Border",
                                  "`Toggle borde` 在**这一页是活的**（⚠️ 同一名字在 Title 页是死节点）—— 文案 `Toggle Border`");

                        var disp2 = FindChild(pg, "Item Display Panel");
                        CheckAtWorld(disp2, 632.79f, 1701.49f, 210.69f, 868.61f, "`Avatar Tab/Item Display Panel`");
                        CheckText(TextOf(FindChild(disp2, "Select Item")), "Select your avatar", "小标题文案 = `Select your avatar`");
                        var scr2 = FindChild(disp2, "Scroll Rect");
                        CheckAtWorld(scr2, 654.16f, 1680.12f, 210.69f, 855.46f, "`Scroll Rect` 视口（与 Title 页同值）");
                        var grid2 = FindChild(scr2, "Item Drawer");
                        CheckAtWorld(grid2, 654.16f, 1698.81f, 210.70f, 210.70f, "`Item Drawer`（宽 1044.65）");
                        // 网格参数：`GridLayoutGroup` cell 180×180 · spacing (25,50) · pad L13 T40 ⇒ **5 列**
                        Check(AvatarTab.Columns, 5,
                              "头像网格 **5 列** = `floor((1044.65 − 13 + 25) ÷ (180 + 25))`（原版 `GridLayoutGroup` 柔性格）");
                        var av = pp.Page(WindowTabType.ProfileAvatar) as AvatarTab;
                        CheckTrue(av != null && av.BuiltCells > 0 && av.BuiltCells < 40,
                                  $"头像网格**只建了看得见的格子**（实得 {(av != null ? av.BuiltCells : -1)} 个；469 条全建会卡）");
                        Check(ProfileData.Avatars.Count, 469, "头像清单 **469** 条（原版可选头像 SO 的实数）");

                        // 🆕 2026-10-07（波 8 · Label 折行族）—— A62 子表 A · A28/A29/A30 + §③（A31 那一处「碰巧对」）
                        // ⛔ 新开一节，上面那些断言一条没动；判据全文 → `资料/普查产出_1007/波8_Label折行族.md`。
                        {
                            CheckWrapMode(FindChild(grid2, "Avatar Name"), 1,
                                          "★ 格子里 `Avatar Name` **折行**（原版 `折行=1`；原来靠 `SetAutoFitBox` 的副作用"
                                          + " = 「碰巧对」⇒ 现在补了**显式** `wrap:true`）");
                            CheckWrapMode(FindChild(mi, "Avatar Name"), 0,
                                          "★ `Selected Item Panel > Avatar Name` **不折行**（原版 `折行=0`）"
                                          + " —— 与格子里那一行**同名不同档**，别照搬");
                            var sBtn = FindChild(sel2, "Select Avatar Button");
                            CheckWrapMode(sBtn != null ? FindChild(sBtn, "Button Text") : null, 0,
                                          "★ `Select Avatar Button > Button Text` **不折行**（原版 `折行=0`）");
                            var tBtn = FindChild(sel2, "Toggle borde");
                            CheckWrapMode(tBtn != null ? FindChild(tBtn, "Button Text") : null, 0,
                                          "★ `Toggle borde > Button Text` **不折行**（原版 `折行=0`）");
                            var selItem = FindChild(FindChild(pg, "Item Display Panel"), "Select Item");
                            CheckWrapMode(selItem, 1, "★ `Item Display Panel > Select Item` **折行**（原版 `折行=1`，本来就对）");
                        }
                        // 🔴 **滚轮真的会重画**（2026-09-27 修，同称号页那一条）：
                        //    469 条只建得出前两行 ⇒ 不接 `OnChanged` 就等于「后面的头像根本够不到」。
                        {
                            var pl = PointerLayer.Instance;
                            var cell0 = grid2 != null && grid2.childCount > 0 ? grid2.GetChild(0) : null;
                            var img0 = cell0 != null ? FindChild(cell0, "Image") : null;
                            float y0a = cell0 != null ? cell0.position.y : 0f;
                            float iy0 = img0 != null ? img0.position.y : 0f;
                            int n0a = grid2 != null ? grid2.childCount : 0;
                            bool hit = pl != null && pl.WheelAt(1167f, 533f, -120f);
                            cell0 = grid2 != null && grid2.childCount > 0 ? grid2.GetChild(0) : null;
                            img0 = cell0 != null ? FindChild(cell0, "Image") : null;
                            float y1a = cell0 != null ? cell0.position.y : 0f;
                            float iy1 = img0 != null ? img0.position.y : 0f;
                            CheckTrue(hit, "滚轮落在头像网格上（`PointerLayer.WheelAt`）");
                            CheckTrue(y1a > y0a + 0.01f,
                                      $"滚一格 ⇒ **内容真的往上走了**（world y {y0a:F3} → {y1a:F3} = {(y1a - y0a) * 108f:F1}px）");
                            CheckTrue(grid2 != null && grid2.childCount == n0a,
                                      $"滚动重建之后格子数不变（{n0a}）—— 重建是**先清再建**（不清会越滚越多）");
                            CheckTrue(iy1 > iy0 + 0.01f,
                                      $"格子里那一层**跟着格子一起走**（world y {iy0:F3} → {iy1:F3}）"
                                      + " —— 原来子件用的是**未偏移**坐标（底板走了、图与字留在原地）");
                            if (pl != null) pl.WheelAt(1167f, 533f, 120f);   // 滚回去
                        }

                        // 🆕 **滚动区登记表不许「只增不减」/ 死条目不许还能被滚到**（2026-09-27 修的那颗雷）。
                        //    原来 `HitScroll` 那句是 `if (s.Owner != null && !s.Owner.activeInHierarchy) continue;`
                        //    —— 宿主**被销毁**时 Unity 的假 null 让它**判不出**，死条目照样命中。
                        //    判据 → `项目任务.md` §〇 A ②；实现 → `PointerLayer.PruneScrolls` / `UnregisterOwnedBy`。
                        {
                            var pl2 = PointerLayer.Instance;
                            if (pl2 != null)
                            {
                                var host = new GameObject("scroll_probe_host");
                                var vp = new PxRect(100f, 100f, 300f, 300f);
                                var probe = new MenuScroll(vp, 0f, 500f) { Owner = host };
                                PointerLayer.RegisterScroll(probe);
                                int n1 = PointerLayer.ScrollCountForTest;
                                CheckTrue(pl2.ScrollUnder(200f, 200f) == probe,
                                          "刚登记的滚动区**能被滚到**（这块是活的）");
                                // 宿主一销毁 ⇒ 这条就是死的：不许再命中，而且**要从登记表里落下去**
                                Object.DestroyImmediate(host);
                                CheckTrue(pl2.ScrollUnder(200f, 200f) != probe,
                                          "★ 宿主**销毁之后**那个区**不再吃滚轮**（原来会因为 Unity 假 null 判不出 ⇒ 死条目照样命中）");
                                CheckTrue(PointerLayer.ScrollCountForTest < n1,
                                          $"★ 死条目**当场从登记表里落下去**（{n1} → {PointerLayer.ScrollCountForTest}）—— 登记表不许只增不减");
                                // `UnregisterOwnedBy`：窗口重建时按宿主一次性撤（`PlayerProfileWindow.Setup` 就用它）
                                var host2 = new GameObject("scroll_probe_host2");
                                var s1 = new MenuScroll(vp, 0f, 500f) { Owner = host2 };
                                var s2 = new MenuScroll(vp, 100f, 600f) { Owner = host2 };
                                PointerLayer.RegisterScroll(s1);
                                PointerLayer.RegisterScroll(s2);
                                int n2 = PointerLayer.ScrollCountForTest;
                                PointerLayer.UnregisterOwnedBy(host2);
                                CheckTrue(PointerLayer.ScrollCountForTest <= n2 - 2,
                                          $"★ `UnregisterOwnedBy` 一次把这个宿主名下的区全撤了（{n2} → {PointerLayer.ScrollCountForTest}）");
                                Object.DestroyImmediate(host2);
                            }
                        }
                    }

                    // ---- Profile 页（2026-09-27 建 · 第 1 页）----
                    // 判据：`资料/普查产出_0927/档案窗_Profile页.md`（§A 层×参数 + §A.1 activeSelf 绑定表 + §C 查不到的）。
                    // 🔴 三类断言**都要**（10·3 第 3 层）：① 建起来了（对原版 rect）② **该藏的时候藏住了**
                    //    ③ 交互真的通（改名那条四跳链的终点）。
                    // 🔴 **锚一律从页根开始**（`FindChild` 是深度优先找**第一个**同名 —— 六页同名节点很多）。
                    {
                        var k0p = FindChild(bar2, PlayerProfileWindow.Tabs[0].Node);
                        var h0p = k0p != null ? FindChild(k0p, "Hit") : null;
                        var w0p = h0p != null ? h0p.GetComponent<WindowButton>() : null;
                        if (w0p != null) w0p.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileInfo, "点第 1 个键 ⇒ 切回 `Profile` 页");

                        var pf = FindChild(t, "Profile Tab");
                        CheckTrue(pf != null, "`Profile Tab` 页根在");
                        var tab = pp.Page(WindowTabType.ProfileInfo) as ProfileTab;
                        CheckTrue(tab != null, "`Profile` 页的组件是 `ProfileTab`（**类名照原版**）");

                        // ① 顶部三件（§A 三行）
                        var pidN = FindChild(pf, "PlayerId");
                        CheckAtWorld(pidN, 351.03f, 913.41f, 867.38f, 907.38f,
                                     "`PlayerId`（锚 (.5,.5) · pos (−416.777,−346.916) · 尺寸 562.38×40）");
                        CheckAtWorld(FindChild(pidN, "Image"), 350.67f, 377.85f, 867.88f, 906.87f,
                                     "`PlayerId/Image`（复制图标 `40k_profile_icon_copy` 27×39 · preserveAspect）");
                        CheckText(TextOf(FindChild(pidN, "playerIdText")), ProfileTab.IdLine,
                                  "PlayerId 那一行 = **空态**（ⓒ：原版拷的是服务器上的 PlayfabId，我们一条都没有）");
                        var clN = FindChild(pf, "Consecutive login days");
                        CheckTrue(clN != null && !clN.gameObject.activeSelf,
                                  "`Consecutive login days` **建成但关着**（prefab 出厂 F + 全包查不到激活点）");
                        var ivN = FindChild(pf, "Invite to alliance");
                        CheckTrue(ivN != null && !ivN.gameObject.activeSelf,
                                  "`Invite to alliance` **建成但关着**（我们不在联盟 ⇒ 走 `Initialize` 末尾那条 `SetActive(false)`）");

                        // ② `Player Info`：头像 + 名号 + 等级
                        var piN = FindChild(pf, "Player Info");
                        CheckAtWorld(piN, 351.03f, 1186.98f, 168.16f, 320.54f, "`Player Info`");
                        var avN = FindChild(piN, "Avatar Item Small");
                        CheckAtWorld(avN, 351.03f, 510.19f, 168.16f, 320.54f, "`Avatar Item Small`");
                        var avIc = FindChild(avN, "Image Container");
                        CheckAtWorld(FindChild(avIc, "Highlight"), 351.03f, 513.52f, 164.63f, 281.50f,
                                     "头像 `Highlight`（`Player_Avatar_selected` · 出厂 T）");
                        CheckAtWorld(FindChild(avIc, "Border"), 351.03f, 510.19f, 179.66f, 294.68f,
                                     "头像 `Border`（⚠️ **比容器还高** —— 真值，别「对齐」掉）");
                        var avImg = FindChild(avIc, "Image");
                        var avBd = FindChild(avIc, "Border");
                        var avIq = avImg != null ? avImg.GetComponentInChildren<ImageQuad>() : null;
                        var avBq = avBd != null ? avBd.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(avIq != null && avIq.Texture != null,
                                  "头像立绘那一层**真的画出来了**（取自全工程**唯一**那份选中状态 `ProfileData.AvatarIndex`）");
                        CheckTrue(avIq != null && avBq != null && avIq.RenderQueue > avBq.RenderQueue,
                                  "★ 立绘的渲染队列**比边框高一档** —— `Player_Profile_Border` 那张图的中心是"
                                + "**不透明黑**（实测 RGBA (0,0,0,255)），反了就把立绘压成黑块"
                                + "（2026-09-27 修；实据 = 原版兄弟序 `Highlight → Border → Image`）");
                        CheckTrue(FindChild(avN, "Avatar Name") != null && !FindChild(avN, "Avatar Name").gameObject.activeSelf,
                                  "`Avatar Name` **恒关**（原版 `AvatarDisplay` 只 set_text、从不 SetActive）");

                        var wa = FindChild(piN, "Info Section with Alliance");
                        CheckTrue(wa != null && !wa.gameObject.activeSelf,
                                  "`Info Section with Alliance` **关**（出厂态就是 with-OFF / without-ON）");
                        var na = FindChild(piN, "Info Section without Alliance");
                        CheckTrue(na != null && na.gameObject.activeSelf, "`Info Section without Alliance` **开着**");
                        var nb = FindChild(FindChild(na, "Name and Title Holder"), "NameHolder");
                        CheckAtWorld(FindChild(nb, "Edit Name Button"), 510.19f, 563.29f, 168.16f, 217.99f,
                                     "`Edit Name Button`（53.10×49.83 · 钮底+描边+图标三层）");
                        string name0 = ProfileData.PlayerName;
                        CheckText(TextOf(FindChild(nb, "Player Name")), name0,
                                  "`Player Name` 显示的是**我们的名字源**（`ProfileData.PlayerName`）");
                        CheckText(TextOf(FindChild(na, "Player Title")), ProfileTab.PlaceholderTitle,
                                  "`Player Title` = **照抄原版预制体的占位串**（我们没有这个数据）");
                        var lvN = FindChild(piN, "Player Level");
                        CheckAtWorld(lvN, 453.15f, 506.27f, 259.19f, 312.31f, "`Player Level` 圆底（237×237）");
                        CheckText(TextOf(FindChild(lvN, "Player Level Text")), "-",
                                  "`Player Level Text` = `-`（ⓐ **照抄原版自己的空态串**，不是我们编的）");

                        // ③ `Ranking`：两张卡 + 传奇那一份（互斥支）
                        var rkN = FindChild(pf, "Ranking");
                        CheckAtWorld(rkN, 351.02f, 1137.12f, 327.70f, 857.21f, "`Ranking`（786.10×529.51）");
                        var crN = FindChild(rkN, "Current Rank");
                        CheckAtWorld(crN, 351.03f, 776.28f, 327.71f, 857.22f, "`Current Rank`（425.26×529.51）");
                        CheckAtWorld(FindChild(crN, "Generic Window Red Background Small"), 351.03f, 776.28f, 327.71f, 857.22f,
                                     "卡片底（`UI_Deck_Selection_Back_simple` · 九宫 197,0,199,0）");
                        var crCt = FindChild(crN, "Content");
                        CheckAtWorld(FindChild(crCt, "Title"), 381.55f, 742.53f, 339.65f, 387.65f, "`Current Rank/Title`");
                        CheckText(TextOf(FindChild(crCt, "Title")), "Current Rank", "标题 = `Current Rank`");
                        CheckAtWorld(FindChild(crCt, "RankTitleBG"), 374.42f, 749.67f, 387.65f, 447.65f,
                                     "`RankTitleBG`（`40K_main_rank_display` · a=0.918）");
                        CheckTrue(FindChild(crCt, "Timer") != null && !FindChild(crCt, "Timer").gameObject.activeSelf,
                                  "`Current Rank/Timer` **关着**（原版 `RankingDisplay` 只填内容、从不 SetActive 它）");
                        var msN = FindChild(FindChild(FindChild(crCt, "footer"), "MainRating"), "Mission Milestones Progress");
                        CheckTrue(msN != null, "`Mission Milestones Progress` 挂在 **`MainRating` 下面**（出厂 T 的那一份才有）");
                        Check(FindChild(msN, "steps") != null ? FindChild(msN, "steps").childCount : -1, 4,
                              "四个 `RankedSealStep`（48.30×60.33）—— 原版那层 tint 是 (1,1,1,**0**)，我们**不点亮**（没有段位数据）");
                        var hrN = FindChild(rkN, "Highest Rank");
                        CheckAtWorld(hrN, 777.72f, 1137.12f, 327.71f, 857.22f, "`Highest Rank`（359.40×529.51）");
                        CheckAtWorld(FindChild(hrN, "Generic Window Red Background Small"), 777.72f, 1137.12f, 327.71f, 857.22f,
                                     "卡片底（`UI_Deck_Selection_Back` · 九宫 0,325,0,35）");
                        CheckAtWorld(FindChild(FindChild(hrN, "Content"), "RankTitleBG"), 727.24f, 1184.44f, 388.49f, 448.49f,
                                     "`Highest Rank/RankTitleBG`（⚠️ **比卡片还宽、右溢出 47.32** —— 真值）");
                        var hrMr = FindChild(FindChild(FindChild(hrN, "Content"), "footer"), "MainRating");
                        CheckTrue(hrMr != null && !hrMr.gameObject.activeSelf,
                                  "`Highest Rank/MainRating` **关**（出厂 F：`displaySeals=0` —— 两份卡逐项不同）");
                        var lgN = FindChild(rkN, "Legendary Display Profile");
                        CheckTrue(lgN != null && !lgN.gameObject.activeSelf,
                                  "`Legendary Display Profile` **关**（与 `Highest Rank` 互斥的两支；出厂两份都是 T ⇒ 出厂态不是运行态）");

                        // ④ `Events` 三格（⚠️ 第一格名字里是**两个空格**）
                        var evN = FindChild(pf, "Events");
                        CheckAtWorld(evN, 1047.88f, 1730.12f, 327.71f, 857.22f, "`Events`");
                        var wcN = FindChild(evN, "Warlord  Mastery Container");
                        CheckTrue(wcN != null, "`Warlord  Mastery Container`（**名字里两个空格** —— 原版真值）");
                        CheckAtWorld(wcN, 1175.12f, 1730.12f, 327.71f, 502.71f, "战将精通那格（555×175）");
                        CheckText(TextOf(FindChild(wcN, "Title")), "Highest Warlod Mastery",
                                  "标题**照抄预制体字面值**（含原版自己的拼写 `Warlod`）");
                        CheckAtWorld(FindChild(evN, "Forge Profile Container"), 1175.12f, 1730.12f, 507.71f, 682.71f,
                                     "锻造厂那格（底图 `40K_profile_ForgeLevel_bg` · Simple）");
                        CheckAtWorld(FindChild(evN, "Campaign Profile Container"), 1175.12f, 1730.12f, 687.71f, 862.71f,
                                     "战役那格");
                        CheckText(TextOf(FindChild(FindChild(evN, "Forge Profile Container"), "ArmyName")), "",
                                  "三格的**数据留空**（ArmyName / Level 都不编数字 —— 用户口径「数据可以空着」）");
                        CheckText(TextOf(FindChild(FindChild(evN, "Campaign Profile Container"), "Title")),
                                  "Current campaign",
                                  "战役那格标题照抄预制体（Forge 那格原版也是这句 —— **疑似原版复制粘贴**，照抄不擅自改）");

                        // ⑤ `ChooseNameWindow`（内嵌的**全屏**改名窗）
                        var cnw = FindChild(pf, "ChooseNameWindow");
                        CheckTrue(cnw != null && !cnw.gameObject.activeSelf,
                                  "`ChooseNameWindow` **出厂关**（`ProfileTab.Start` 显式 SetActive(false)）");
                        CheckAtWorld(cnw, -0.13f, 1920.13f, 0f, 1080f,
                                     "它**铺满整屏**（比页根还大 —— 真值；它是窗，不是子面板）");
                        CheckAtWorld(FindChild(cnw, "Generic Popup Background"), 519.49f, 1400.51f, 395.00f, 696.03f,
                                     "面板（`40k_popup` · 九宫 169,160,169,160）");
                        CheckAtWorld(FindChild(cnw, "Choose Name Input Field"), 540.13f, 1376.29f, 496.00f, 556.00f,
                                     "输入框（`40K_dropdown_bg` · 九宫 23,20,23,20）");
                        CheckText(TextOf(FindChild(cnw, "MessageText")), "Choose your player name", "文案 = `Choose your player name`");
                        CheckText(TextOf(FindChild(FindChild(cnw, "Change Name Button"), "Button Text")), "Free",
                                  "按钮文案 = `Free`（原版两套参数里的**首次改名**那一套）");
                        var pdN = FindChild(FindChild(cnw, "Change Name Button"), "Price Display");
                        CheckTrue(pdN != null && !pdN.gameObject.activeSelf,
                                  "`Price Display` **关**（原版 `TimesNameChange>=1` 才开）");
                        // 🆕 **2026-10-08（波 C3 · A214③）**：`ProfileTab.PdTxL/PdTxR` 那对常量**此前没有任何断言**
                        //   （A62 ⑫④ 把它们按修好的 `menu_dump.py` 从 `938.55/1030.77` 改成 `943.70/1035.92`，
                        //   而全仓 0 处引用 ⇒ 改回去也不会红）。这里**量渲出来的节点**把它钉死。
                        //   期望值 = **原版 dump 的字面量**（⛔ 不从 `ProfileTab.PdTxL` 读 —— 那是自证）：
                        //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
                        //   ⇒ `Change Name Button/Price Display > text` = **943.70,586.77 → 1035.92,638.24**。
                        //   🔴 **为什么这 5.15px 值得断**：那一格是 `HorizontalLayoutGroup` 里的**第二个子件**，
                        //   而前一件（`icon`）自带 `m_LocalScale = 1.2` ⇒ uGUI 的推进量按 `childSize × scaleFactor`
                        //   算、组内居中的起始偏移又按乘过缩放的 requiredSpace 折半 ⇒ 净位移
                        //   `51.47 × (1.2 − 1) ÷ 2` = **+5.15**（旧的 938.55 正是**丢了这个半格**的读数）。
                        //   **怎么改坏就红**：把 `PdTxL/PdTxR` 改回 `938.55/1030.77`（= 退回旧工具那套）
                        //   ⇒ 中心左移 5.15px ⇒ 这一条红。
                        //   ⚠️ 它**与折行无关**：`Price Display > text` 那条 `Text(...)` 是**居中**（`autoFit` 不带
                        //   `alignLeft`），所以中心 = 矩形中心，`CheckAtWorld` 直接可比。
                        CheckAtWorld(FindChild(pdN, "text"), 943.70f, 1035.92f, 586.77f, 638.24f,
                                     "`Price Display > text` 的矩形 = 原版 **943.70,586.77→1035.92,638.24**"
                                     + "（HLG 净位移 +5.15 = `51.47 × (1.2−1) ÷ 2`；旧值 938.55 是丢了这个半格的读数）");

                        // ⑥ 交互：改名那条四跳链的终点（判据 ③）
                        if (tab != null)
                        {
                            var ehN = FindChild(nb, "EditNameHit");
                            var ehb = ehN != null ? ehN.GetComponent<WindowButton>() : null;
                            CheckTrue(ehb != null && ehb.onClick != null, "`Edit Name Button` **接了点击**");
                            if (ehb != null) ehb.Click();
                            CheckTrue(tab.NameWindowOpen, "点 `Edit Name Button` ⇒ **改名窗开了**");
                            CheckTrue(cnw != null && cnw.gameObject.activeSelf, "改名窗那一层的 `activeSelf` 也真的翻了");
                            tab.UiSetName("Test Commander");
                            CheckTrue(!tab.NameWindowOpen, "提交之后**窗自己关掉**");
                            CheckText(TextOf(FindChild(nb, "Player Name")), "Test Commander",
                                      "`Player Name` **真的换了**（走的是全工程**唯一**那个写点）");
                            CheckText(ProfileData.PlayerName, "Test Commander",
                                      "名字源也换了（**联机层的显示名读的是同一个源** —— 两处写同一条规则就会不一致）");
                            tab.UiSetName("");
                            CheckText(ProfileData.PlayerName, "Test Commander", "空名字**不被接受**（原版那道校验）");
                            tab.UiSetName(name0);       // 还原（后面的断言还会看这个名字）
                            CheckText(ProfileData.PlayerName, name0, "名字已还原");
                        }

                        // 🆕 ============================================================ 2026-10-07（波 8 · Label 折行族）
                        //  A62 子表 A（ProfileTab 那 14 处）· A77 ①（第三档）· A77 ③（「碰巧对」补 `wrap:true`）。
                        //  判据全文 → `资料/普查产出_1007/波8_Label折行族.md`；⛔ 本节是**新开的一节**，上面那些断言一条没动。
                        //  🔴 读口用 `CheckWrapMode`（直接取该节点**自己**的组件）—— 改名窗整棵是 `SetActive(false)` 的，
                        //     既有那个 `CheckWrapping` 走 `GetComponentInChildren`（**只找激活的**）会误报成「节点不在」。
                        {
                            var inArea = FindChild(FindChild(cnw, "Choose Name Input Field"), "Text Area");
                            CheckWrapMode(inArea != null ? FindChild(inArea, "Text") : null, ProfileTab.InWrapMode,
                                          "★ 改名窗输入框 `Text` = 原版**第三档 `折行=3`**（`PreserveWhitespaceNoWrap`）"
                                          + " —— 旧口表达不了它，用 `false` 顶替 = 静默降级成 0");
                            CheckWrapMode(inArea != null ? FindChild(inArea, "Placeholder") : null, 0,
                                          "★ 同名那一对里的 `Placeholder` = 原版 **`折行=0`**（**同一格两个节点两个档**，别一刀切）");
                            CheckWrapMode(FindChild(cnw, "MessageText"), 1,
                                          "★ `MessageText` **折行**（原版 `折行=1`；判据 §③ ⇒ 补了显式 `wrap:true`）");
                            var avSmall = FindChild(FindChild(pf, "Player Info"), "Avatar Item Small");
                            CheckWrapMode(avSmall != null ? FindChild(avSmall, "Avatar Name") : null, 1,
                                          "★ `Avatar Item Small > Avatar Name` **折行**（原版 `折行=1`；判据 §③ 的「碰巧对」⇒ 显式 `wrap:true`）");
                            var wa2 = FindChild(FindChild(pf, "Player Info"), "Info Section with Alliance");
                            CheckWrapMode(wa2 != null ? FindChild(wa2, "Player Name") : null, 0,
                                          "★ `Info Section with Alliance > Player Name` **不折行**（原版 `折行=0`）");
                            CheckWrapMode(wa2 != null ? FindChild(wa2, "Player Title") : null, 0,
                                          "★ … `Player Title` **不折行**（原版 `折行=0`）");
                            CheckWrapMode(FindChild(nb, "Player Name"), 0,
                                          "★ `Info Section without Alliance > Player Name` **不折行**（原版 `折行=0`，显示的那一份）");
                            CheckWrapMode(FindChild(na, "Player Title"), 0,
                                          "★ … `Player Title` **不折行**（原版 `折行=0`）");
                            // A62 · A15：出厂关着的那棵子树模式也要对（判据是**字段**，不是「看不看得见」）
                            // ⚠️ 外层的 `Timer` 与里面那行字**同名**（照原版树），而 `FindChild` 会把**自己**也算进去
                            //    ⇒ 不能拿 `FindChild(外层, "Timer")`（那会返回外层），必须在**直接子件**里挑。
                            var tmOuter = FindChild(FindChild(crN, "Content"), "Timer");
                            Transform tmText = null;
                            if (tmOuter != null)
                                for (int ci = 0; ci < tmOuter.childCount; ci++)
                                    if (tmOuter.GetChild(ci).name == "Timer") { tmText = tmOuter.GetChild(ci); break; }
                            CheckWrapMode(tmText, 0,
                                          "★ `Current Rank/Timer` 那行字 **不折行**（原版 `折行=0`；那棵树出厂关着 ⇒ 只能靠**字段**钉）");
                        }
                    }
                    // ---- Battle Log 页（2026-09-27 建 · 第 4 页）----
                    // 判据：`资料/普查产出_0927/档案窗_BattleLog与页签按钮.md`（§A·1 层×参数 · §B 行模板 `logPrefab`）。
                    // 🔴 它的行模板**就是后面「对局历史」那件要用的同一个**（正本 §B·5：`BattleLogPopup` 共用）。
                    {
                        var k3 = FindChild(bar2, PlayerProfileWindow.Tabs[3].Node);
                        var h3 = k3 != null ? FindChild(k3, "Hit") : null;
                        var w3 = h3 != null ? h3.GetComponent<WindowButton>() : null;
                        if (w3 != null) w3.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileBattleLog, "点第 4 个键 ⇒ 切到 `Battle Log` 页");

                        var bl = FindChild(t, "Battle Log Tab");
                        CheckTrue(bl != null, "`Battle Log Tab` 页根在");
                        var tab3 = pp.Page(WindowTabType.ProfileBattleLog) as BattleLogTab;
                        CheckTrue(tab3 != null, "这一页的组件是 `BattleLogTab`（类名照原版）");

                        var mtN = FindChild(bl, "Matches");
                        CheckAtWorld(mtN, 326.03f, 1771.97f, 162.84f, 904.48f,
                                     "`Matches`（`ScrollRect` —— ⚠️ **比页根宽 25、两侧各溢**，`sizeDelta=(50,−101.441)`）");
                        var vpN = FindChild(mtN, "Viewport");
                        CheckAtWorld(vpN, 326.03f, 1771.97f, 162.84f, 887.48f,
                                     "`Viewport`（`Mask` + `showGraphic=0` ⇒ 只建节点、不画）");
                        CheckAtWorld(FindChild(vpN, "Content"), 326.03f, 1771.97f, 162.84f, 162.84f,
                                     "`Content`（`VerticalLayoutGroup` spacing 25 · UpperLeft · 锚在顶边）");

                        // 🔴 档位（2026-10-05 A28 尾巴）：原版 `Matches` 的 `m_MovementType = 1` ⇒ UGUI **Elastic**
                        //   （真值 `Unrestricted 0 / Elastic 1 / Clamped 2`，本地 UGUI 源码亲读）。
                        //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Battle Log Tab" --depth 5` 实读
                        //   `Matches` = `h=0 v=1 mode=1`（工具已沿 `m_Father` 爬到 `Tab Content` 认过窗）。
                        //   🔴 **真红法**：删掉 `BattleLogTab.Build` 里那句 `_scroll.Elastic = true;` ⇒ 这一条立刻红
                        //   （`MenuScroll.Elastic` 出厂 `false` = Clamped，正是这一族**另一档**）。
                        //   ⛔ 别套同族的另一档：`Battle Log Popup` 的 `Matches` 是 `mode=2`(Clamped)。
                        CheckTrue(tab3 != null && tab3.RowsScroll != null && tab3.RowsScroll.Elastic,
                                  "档位：**Elastic**（原版 `Matches` 的 `m_MovementType = 1`）");

                        // ---- ①b 🔴 **我们自己加的入口**（用户 2026-09-27 拍板「接在档案窗 `Battle Log` 页」）----
                        //  原版那扇 `Battle Log Popup` 的**打开点查不到**（只在 `WindowsManager` 预载表里，
                        //  `OpenWindow<BattleLogPopup>()` 的泛型调用产物缺失 ⇒ 我们**不编入口**了整整一轮）；
                        //  这一颗是用户拍板补的 ⇒ 断「**它在了 + 不压列表 + 点了真开**」，并**标明它不是复刻**。
                        {
                            var eb = FindChild(bl, "Open Log Popup Button");
                            CheckTrue(eb != null, "`Open Log Popup Button`（**这一颗是我们加的** —— 原版那一页没有它）");
                            CheckAtWorld(eb, 1506.97f, 1746.97f, 122.92f, 158.92f,
                                         "入口按钮的 rect（右上对齐内容区右缘 1746.97、落在**列表上方那条空带**里）");
                            // 🆕 **2026-10-08（波 C3 · A212）**：这颗钮的**折行判据是空的** —— 它**不在原版
                            //   那一页的节点表里**（原版打开点本地查不到），我们**没有**原版 `m_TextWrappingMode`
                            //   可对 ⇒ 按纪律「判据空 ⇒ 保持现状 + 标明是我们挑的」：`Shell/BattleLogTab.cs`
                            //   那一行**显式**写了 `wrap: true`（不写的话，`ProfilePage.Text` 从本批起会把
                            //   `autoFit` 的隐含折行去掉 ⇒ **静默**变档）。
                            //   ⚠️ 这条断言钉的是「**它被显式声明过**」这件事：把那一行 `wrap: true` 删掉 ⇒ 红。
                            //   ⛔ 它不是「原版就是折行」的证据 —— 原版没有这一颗。
                            CheckWrapMode(FindChild(eb, "Button Text"), 1,
                                          "★（**我们自建**的入口钮）`Button Text` = **折行**，是**显式声明**的"
                                          + "（原版这一页**没有这个节点** ⇒ 判据空，保持现状并标明）");
                            // 🔴 **最要紧的一条**：它**不许压到列表**（列表顶 162.84）—— 这是这颗钮唯一会犯的错
                            float bY2 = 0f;
                            {
                                var br = eb != null ? eb.GetComponentInChildren<ImageQuad>() : null;
                                if (br != null) bY2 = LayoutSpace.PxY(eb.position.y) + br.WorldH * 108f * 0.5f;
                            }
                            CheckTrue(eb != null && bY2 > 0f && bY2 <= 162.84f,
                                      $"入口按钮**下沿在列表之上**（下沿 {bY2:F2} ≤ 162.84，不压第一行）");
                            var ebHit = eb != null ? FindChild(eb, "Hit") : null;
                            var ebBtn = ebHit != null ? ebHit.GetComponent<WindowButton>() : null;
                            CheckTrue(ebBtn != null && ebBtn.onClick != null, "入口有点击、且绑了动作");
                            if (ebBtn != null) ebBtn.Click();
                            var pop = BattleLogPopup.LastOpened;
                            CheckTrue(pop != null && pop.CurrentState == WindowState.Open,
                                      "★ 点它 ⇒ **真的开了 `Battle Log Popup`**（原来界面里根本进不去）");
                            if (pop != null)
                            {
                                // 🆕 A47：压暗层命中区 —— 档 = `QPanel`(3450)（= 压暗层自己那一档），< `QHit`(3458)
                                //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档
                                //      （那颗 quad 是它子件 `Image`，`ShadeVisualQuad` 会认）。
                                MenuDraw.CheckShadeRule(CheckTrue, "战斗日志弹窗", FindChild(pop.transform, "CloseHit"),
                                                        pop.transform.Find("Menu Dark Background"), BattleLogPopup.QHit);
                                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒
                                //   原版什么都不发生）。期望矩形 = **原版 prefab**
                                //   `Battle Log Popup > Content > Background`（`UI_Deck_Information_Back`）
                                //   那颗 `Image` 的 rect（135,55 → 1785,1055）；⛔ 不写 `BattleLogPopup.ContentR`
                                //   —— 那是被测实现**传进去的实参**（同式自证）。
                                CheckAbsorbRule("战斗日志弹窗", pop.transform, "AbsorbHit",
                                                135f, 55f, 1785f, 1055f,
                                                BattleLogPopup.QPanel, BattleLogPopup.QHit,
                                                () => pop.CurrentState);
                                pop.Close();
                            }
                        }
                        Shoot("10_档案窗_BattleLog页.png");   // 给下个会话留一张：**那颗我们加的入口长什么样**

                        // ① 空态：本地没有对局记录（原版读服务器）⇒ 照原版**留空**，不造空态文案
                        Check(BattleLogData.Count, 0, "本地对局记录 **0 条**（原版在 `PlayerDataManager.battleLogData`）");
                        CheckTrue(FindChild(vpN, "Content") != null && FindChild(vpN, "Content").childCount == 0,
                                  "⇒ `Content` 底下**一行都不建**（原版也没有空态节点，`OnOpen` 只是清空）");

                        // ② 行模板：喂一条 ⇒ 逐件对 §B·2 那棵树（**这就是「对局历史」要复用的那一份**）
                        if (tab3 != null)
                        {
                            BattleLogData.Add(new BattleLogData.Match
                            {
                                Result = BattleLogData.Outcome.Victory,
                                OwnHeroName = "Uriel Ventris", EnemyHeroName = "Ghazghkull Thraka",
                                OwnName = "Test Commander", EnemyName = "Bot",
                                OwnSkulls = 3, EnemySkulls = 1,
                                OwnScore = "987 (+12)", EnemyScore = "Gold IV", Mode = "Skirmish mode",
                            });
                            tab3.RebuildRows();
                            Check(tab3.BuiltRows, 1, "喂一条 ⇒ **建出一行**（原版是 `Instantiate(logPrefab, holder)`）");
                            var row = FindChild(FindChild(vpN, "Content"), "Match Log");
                            CheckAtWorld(row, 326.03f, 1771.97f, 162.84f, 366.04f,
                                         "行矩形（行高 203.20、从 `Content` 顶边起）");
                            CheckText(TextOf(FindChild(row, "Result")), BattleLogData.ResultText(BattleLogData.Outcome.Victory),
                                      "结果那行 = `CardText.Phrase(...)`（**与结算面板同一个源**，不是另写一份）");
                            var pInfo = FindChild(row, "Player Info");
                            var eInfo = FindChild(row, "Enemy Info");
                            // 🔴 对齐**不能断中心**：原版这几行是 `Left`/`Right` 对齐 ⇒ `MenuDraw.AlignLeft/AlignRight`
                            //    会把整块字挪到矩形的那一条边上（Title 页那条教训），断中心会报假失败。
                            var ownHero = FindChild(pInfo, "Hero Name");
                            var ownHeroLb = ownHero != null ? ownHero.GetComponent<Label>() : null;
                            float ownRightPx = ownHero != null && ownHeroLb != null
                                ? (ownHero.position.x + ownHeroLb.WorldW * 0.5f) * 108f + 960f : -1f;
                            CheckTrue(Mathf.Abs(ownRightPx - 901f) < 0.5f,
                                      $"我方 `Hero Name` **右对齐到 x=901**（原版 `Right/Middle`；实得 {ownRightPx:F2}）");
                            var foeHero = FindChild(eInfo, "Hero Name");
                            var foeHeroLb = foeHero != null ? foeHero.GetComponent<Label>() : null;
                            float foeLeftPx = foeHero != null && foeHeroLb != null
                                ? (foeHero.position.x - foeHeroLb.WorldW * 0.5f) * 108f + 960f : -1f;
                            CheckTrue(Mathf.Abs(foeLeftPx - 1197f) < 0.5f,
                                      $"敌方 `Hero Name` **左对齐到 x=1197**（原版 `Left/Middle` —— 两份不是镜像，逐条不同；实得 {foeLeftPx:F2}）");
                            CheckAtWorld(FindChild(pInfo, "Score Icon"), 389.54f, 454.54f, 271.94f, 336.94f, "我方段位图标");
                            CheckAtWorld(FindChild(pInfo, "Skulls"), 887.52f, 987.52f, 254.44f, 354.44f, "我方骷髅（100×100）");
                            CheckText(TextOf(FindChild(pInfo, "skullCounter")), "x3", "骷髅数 `x3`");
                            CheckAtWorld(FindChild(row, "ReplayButton"), 1675.47f, 1740.47f, 188.86f, 253.86f,
                                         "`ReplayButton`（65×65 · `40k_general_bt_yellow`）");
                            CheckAtWorld(FindChild(row, "PinButton"), 1597.60f, 1662.60f, 188.86f, 253.86f, "`PinButton`");
                            CheckAtWorld(FindChild(FindChild(row, "Details"), "Sword"), 999.00f, 1099.00f, 254.44f, 354.44f,
                                         "中间那把剑（`40k_main_bt_play`）");
                            // 钉住色：原版两个常量（未钉 (1,1,1,1) / 已钉 (0,1,0,1)），实读自 DLL
                            var pinIc = FindChild(FindChild(row, "PinButton"), "pinicon");
                            var pinQ = pinIc != null ? pinIc.GetComponent<ImageQuad>() : null;
                            CheckTrue(pinQ != null && Mathf.Abs(pinQ.Tint.g - 1f) < 1e-3f && Mathf.Abs(pinQ.Tint.r - 1f) < 1e-3f,
                                      "未钉 ⇒ `pinicon` 是 **(1,1,1,1)**（原版 `SetPinState` 的常量）");
                            var pinHit = FindChild(FindChild(row, "PinButton"), "Hit");
                            var pinBtn = pinHit != null ? pinHit.GetComponent<WindowButton>() : null;
                            CheckTrue(pinBtn != null && pinBtn.onClick != null, "`PinButton` 接了点击");
                            if (pinBtn != null) pinBtn.Click();
                            CheckTrue(pinQ != null && pinQ.Tint.r < 1e-3f && Mathf.Abs(pinQ.Tint.g - 1f) < 1e-3f
                                      && pinQ.Tint.b < 1e-3f,
                                      "钉住 ⇒ **变绿 (0,1,0,1)**（真值，不是我们挑的颜色）");
                            // 两条必须出声的（原版那两条都要吃服务器/回放，我们都没有）
                            var repHit = FindChild(FindChild(row, "ReplayButton"), "Hit");
                            var repBtn = repHit != null ? repHit.GetComponent<WindowButton>() : null;
                            CheckTrue(repBtn != null && repBtn.onClick != null, "`ReplayButton` 接了点击（点了会**出声说回放没做**）");
                            var foeHit = FindChild(eInfo, "EnemyClickHit");
                            var foeBtn = foeHit != null ? foeHit.GetComponent<WindowButton>() : null;
                            CheckTrue(foeBtn != null && foeBtn.onClick != null,
                                      "敌方那块（`UIGenericEventCatcher`）也接了点击（点对手会出声说没有档案）");
                            // 清干净（后面的断言与别的自检不该看到这条假数据）
                            BattleLogData.ResetForTest();
                            tab3.RebuildRows();
                            Check(tab3.BuiltRows, 0, "清空之后**回到空态**（自检不留下假数据）");

                            // ---- 🆕 2026-10-03：**视口外的整行不建** ----
                            //   ⚠️ 这一页**本来就有**这道守卫（`BattleLogTab.cs:182` 的 `_scroll.Intersects`）——
                            //   本件是给 `BattleLogPopup` 那棵树补上同一道（那边一直缺）；这里把它**钉住**，
                            //   顺带钉「整行在视口外 ⇒ 连它的 `Hit` 一起不建」（等价原版 `RectMask2D` 裁掉的部分点不到）。
                            //   🔴 期望值**现算**（`RowsInViewport`）：视口 162.84..887.48（上面 `CheckAtWorld(vpN, …)`
                            //   刚按原版值钉过）、行距 = 原版 203.20 + 25。⛔ 不写死条数。
                            {
                                const int NF = 5;      // 现算：视口高 724.64、行距 228.2 ⇒ 第 5 行整行落在视口外
                                for (int i = 0; i < NF; i++)
                                    BattleLogData.Add(new BattleLogData.Match
                                    {
                                        Result = BattleLogData.Outcome.Victory,
                                        OwnHeroName = "Feed " + i, EnemyHeroName = "Bot",
                                        OwnName = "Feed " + i, EnemyName = "Bot",
                                        OwnSkulls = 1, EnemySkulls = 0, OwnScore = "1", EnemyScore = "2",
                                        Mode = "Skirmish mode",
                                    });
                                tab3.RebuildRows();
                                const float FTop = 162.84f, FBot = 887.48f;
                                float fpitch = OrigMatchRowH + OrigMatchRowGap;     // 原版：203.20 + 25
                                int wantF = RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, 0f);
                                Check(tab3.BuiltRows, wantF, $"喂 {NF} 行 ⇒ 建了与视口相交的那 {wantF} 行（现算）");
                                CheckTrue(wantF > 0 && wantF < NF,
                                          $"…而且**确实有整行落在视口外**（{NF - wantF} 行连节点一起不建 —— 它的 `Hit` 也不存在）");
                                var fc = FindChild(vpN, "Content");
                                int fn = 0, fout = 0; float fmin = float.MaxValue;
                                if (fc != null)
                                    foreach (var rt in fc.GetComponentsInChildren<Transform>(true))
                                    {
                                        if (rt.name != "Match Log") continue;
                                        fn++;
                                        float cy = LayoutSpace.PxY(rt.position.y);
                                        float y1 = cy - OrigMatchRowH * 0.5f, y2 = cy + OrigMatchRowH * 0.5f;
                                        fmin = Mathf.Min(fmin, y1);
                                        if (y2 <= FTop + 0.01f || y1 >= FBot - 0.01f) fout++;
                                    }
                                Check(fn, tab3.BuiltRows, "行节点个数 == `BuiltRows`（两者不许各说各的）");
                                CheckTrue(fout == 0, $"每一颗建出来的行都与视口相交（越界 {fout} 颗）");
                                CheckNear(fmin, FTop, 0.5f,
                                          "最上面那颗行的顶边 = 视口顶 162.84（顺带证明滚动偏移是 0 —— 上面的期望值按它现算）");
                                BattleLogData.ResetForTest();
                                tab3.RebuildRows();
                                Check(tab3.BuiltRows, 0, "清空 ⇒ 回到空态（不留假数据）");
                            }
                        }
                    }
                    // ---- Trophies 页（2026-09-27 建 · 第 5 页）----
                    // 判据：`资料/普查产出_0927/档案窗_Trophies页.md`（§A·1 层×参数 · §A·4 三个运行时生成的族 · §C3 102 条成就）。
                    {
                        var k4 = FindChild(bar2, PlayerProfileWindow.Tabs[4].Node);
                        var h4 = k4 != null ? FindChild(k4, "Hit") : null;
                        var w4 = h4 != null ? h4.GetComponent<WindowButton>() : null;
                        if (w4 != null) w4.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileTrophies, "点第 5 个键 ⇒ 切到 `Trophies` 页");

                        var tr = FindChild(t, "Trophies Tab");
                        CheckTrue(tr != null, "`Trophies Tab` 页根在");
                        var tab4 = pp.Page(WindowTabType.ProfileTrophies) as AchievementsMenu;
                        CheckTrue(tab4 != null, "这一页的组件是 `AchievementsMenu`（**原版类名**：键名叫 Trophies、文案是 Achievements）");

                        CheckAtWorld(FindChild(tr, "bg"), 635.16f, 1746.96f, 213.16f, 891.68f,
                                     "`bg`（`UI_Deck_Information_submenu_Back` · 九宫 18,18,18,18）");
                        var btns = FindChild(tr, "buttons");
                        CheckAtWorld(btns, 351.04f, 635.15f, 230.64f, 928.28f,
                                     "`buttons`（`ToggleGroup` + VLG spacing 20 · **UpperLeft**）");
                        // 🔴 **运行时是 4 个分类，不是预制体里烘焙的 5 个**（`Enum.GetValues` 按值升序 ⇒ Battle→Collection→Victories→Account）
                        Check(btns != null ? btns.childCount : -1, 4,
                              "四个分类页签（**不是预制体里那 5 个** —— 运行时 `DestroyAllChildren` 后按枚举重建，判据 ①）");
                        var texOnT = CardArt.MenuUi(AchievementsMenu.ArtTabOn);
                        var texOffT = CardArt.MenuUi(AchievementsMenu.ArtTab);
                        CheckTrue(texOnT != null && texOffT != null, "页签底两张图在工程里（复用设置窗那两张）");
                        var names4 = new[] { "Battle", "Collection", "Victories", "Account" };
                        for (int i = 0; i < 4; i++)
                        {
                            float top = 230.64f + 120f * i;
                            var tg = FindChild(btns, i == 0 ? "Achievement Type Toggle" : "Achievement Type Toggle (" + i + ")");
                            CheckAtWorld(tg, 351.04f, 635.15f, top, top + 100f,
                                         $"分类键 {i}（284.11×100、步进 120 —— 宽被布局组强制成父宽，**不是 prefab 自己那 333.301**）");
                            CheckText(TextOf(FindChild(tg, "Tab Toggle Title")), names4[i],
                                      $"分类 {i} 文案 = `{names4[i]}`（⚠️ **我们挑的**：原版是 I2 词条 `Achievements/Types/<名>`，译文在远端查不到；"
                                      + "预制体里那 4 个 `'Secret'` 是占位）");
                            var bgq = FindChild(tg, "button_bg");
                            var q = bgq != null ? bgq.GetComponent<ImageQuad>() : null;
                            bool on = i == 0;      // 出厂选中 `Battle`（ctor 写死 `filter = 1`）
                            CheckTrue(q != null && q.Texture == (on ? (Texture)texOnT : texOffT),
                                      $"分类键 {i} 底色贴图 = **{(on ? "选中" : "未选")}**那张（换图不换色，与左栏六键同一条判据）");
                            // 🆕 2026-10-08（波 C3 · A212）：原版 `Achievement Type Toggle/Label/Tab Toggle Title`
                            //   实读 **`折行=1`**（`字号=35 auto[23~35]`）⇒ 属「显式 `wrap: true`」那一档，
                            //   本条钉住「收口没把它带偏」。
                            CheckWrapMode(FindChild(tg, "Tab Toggle Title"), 1,
                                          $"★ 分类键 {i} 的 `Tab Toggle Title` **折行**（原版 `折行=1`）");
                        }
                        Check(tab4 != null ? tab4.Filter : 0, ProfileData.TypeBattle, "出厂选中 **Battle**");

                        // ⚠️ **2026-10-05 更正**：这一条的消息原来写 `` `mode=2 Elastic` `` —— **映射写反了**
                        //   （UGUI 枚举 `Unrestricted 0 / Elastic 1 / Clamped 2`）。`mode=2` 是 **Clamped**：
                        //   我们实现本来就是 Clamped（**对的**），**只有标签错** ⇒ 只改字，不动代码、也不必加断言。
                        //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Trophies Tab" --depth 5`
                        //   实读 `Scroll` = `h=0 v=1 mode=2`（工具已沿 `m_Father` 爬到 `Tab Content` 认过窗）。
                        //   同一处的实现侧早在 `Shell/AchievementsMenu.cs:76` 就订正过，这条消息是漏网的一句。
                        CheckAtWorld(FindChild(tr, "Scroll"), 635.14f, 1746.98f, 213.16f, 891.69f, "`Scroll`（`ScrollRect` v=1 · mode=2 Clamped）");
                        CheckAtWorld(FindChild(FindChild(tr, "Scroll"), "Viewport"), 635.14f, 1746.98f, 216.14f, 891.69f,
                                     "`Viewport`（**`RectMask2D`** —— 与 Battle Log 那页的 `Mask` 不同）");
                        var holder = FindChild(FindChild(FindChild(tr, "Scroll"), "Viewport"), "ContainerHolder");
                        CheckTrue(holder != null && Mathf.Abs(holder.position.y
                                  - MainMenuRuntime.Center(0f, 0f, 200.27f, 200.27f).y) < 0.02f,
                                  "`ContainerHolder` 顶 = **200.27**（⚠️ 比视口顶 216.14 还高 15.87 —— 真值）");
                        Check(AchievementsMenu.Columns, 2, "网格 **2 列** = ⌊(1111.82 + 10) ÷ (520 + 10)⌋（原版 `GridLayoutGroup` Flexible）");

                        // 数据：本地 **102 条**（`AllAchievements` + `ACH1..102` 两个 SO）；类型分布 36/28/30/8
                        Check(ProfileData.Achievements.Count, 102, "成就 **102 条**（原版资产实数）");
                        Check(ProfileData.OfType(ProfileData.TypeBattle).Count, 36, "Battle **36** 条");
                        Check(ProfileData.OfType(ProfileData.TypeCollection).Count, 28, "Collection **28** 条");
                        Check(ProfileData.OfType(ProfileData.TypeVictories).Count, 30, "Victories **30** 条");
                        Check(ProfileData.OfType(ProfileData.TypeAccount).Count, 8, "Account **8** 条");

                        if (tab4 != null)
                        {
                            CheckTrue(tab4.BuiltCells > 0 && tab4.BuiltCells < 12,
                                      $"一屏**只建看得见的格子**（实得 {tab4.BuiltCells} 个；36 条全建会卡）");
                            var c0 = FindChild(holder, "Achievement Container");
                            CheckAtWorld(c0, 666.05f, 1186.05f, 232.27f, 382.27f,
                                         "第一个格子（520×150 · 横向余量 61.82 ⇒ 左内缩 **30.91**）");
                            var a0 = ProfileData.OfType(ProfileData.TypeBattle)[0];
                            CheckText(TextOf(FindChild(c0, "title")), a0.Name + " 1/5",
                                      "`title` = `{名} {档}/{总档}`（原版格式串 `{0} {1}/{2}`；我们没进度 ⇒ 恒第 1 档）");
                            CheckText(TextOf(FindChild(c0, "description")), a0.Challenge,
                                      "`description` = **`challenge` 的 id**（真字符串；⚠️ 原版那格是 I2 词条，译文查不到 —— 我们拿它顶）");
                            CheckTrue(TextOf(FindChild(c0, "rewards")).EndsWith(" points"),
                                      "`rewards` 照预制体 `'2 points'` 那个形式（真词条 `Achievements/Points` 在远端）");
                            // 🆕 **2026-10-08（波 C3 · A212）**：这一页是 A62 子表 A 的 **A32~A37**（本批之前
                            //   一处都没核过 —— 它们**本来就对**，因为调用点写的是 `wrap: true`）。
                            //   本条把「对」钉住：`Shell/PlayerProfileWindow.cs` 的 `Text(...)` 从本批起
                            //   **`autoFit` 不再隐含折行**，若哪天有人把这几处的 `wrap: true` 删掉，
                            //   下面四条会**当场红**（而不是静默退回 `0`）。
                            //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window"
                            //   --depth 25 --md` ⇒ `Trophies Tab/Scroll/Viewport/ContainerHolder/Achievement Container`
                            //   的 `title` / `description` / `rewards` **全是 `折行=1`**
                            //   （`counter` 那行同值，见下面 `cntN`）。
                            CheckWrapMode(FindChild(c0, "title"), 1, "★ 成就格 `title` **折行**（原版 `折行=1`）");
                            CheckWrapMode(FindChild(c0, "description"), 1, "★ 成就格 `description` **折行**（原版 `折行=1`）");
                            CheckWrapMode(FindChild(c0, "rewards"), 1, "★ 成就格 `rewards` **折行**（原版 `折行=1`）");
                            var slN = FindChild(FindChild(c0, "Progress"), "Slider");
                            var cntN = slN != null ? FindChild(slN, "counter") : null;
                            CheckText(TextOf(cntN), "0/" + a0.Thresholds[0],
                                      "`counter` = `0/{该档阈值}`（格式串 `{0}/{1}`；阈值是资产里的真数）");
                            CheckWrapMode(cntN, 1, "★ 成就格 `counter` **折行**（原版 `折行=1`，同一条 dump）");
                            CheckAtWorld(slN != null ? FindChild(slN, "Background") : null,
                                         818.05f, 1038.74f, 334.82f, 361.00f,
                                         "进度条底（`40k_campaign_bar_bg` · 九宫 20,0,20,0）");
                            CheckTrue(FindChild(c0, "Image") != null
                                      && FindChild(c0, "Image").GetComponentInChildren<ImageQuad>() != null,
                                      "勋章图那一层**真的画出来了**（`40k_Achievements_icon_medal1`）");
                            // 计数条：**全量之和、不受筛选影响**（判据 ③）
                            CheckAtWorld(FindChild(tr, "Counter"), 1568.89f, 1703.96f, 161.10f, 202.22f, "`Counter` 计数条");
                            CheckText(TextOf(FindChild(FindChild(tr, "Counter"), "EverguildTextMeshPro")),
                                      AchievementsMenu.PointsText(),
                                      "计数条 = 全量成就积分之和（我们没进度 ⇒ `0`；原版 `ToString()` 无千分位）");

                            // 点第二个分类 ⇒ 换筛选 + 重建（原版 `NotifyToggleOn` → 写 filter → `Refresh()`）
                            var tg1 = FindChild(btns, "Achievement Type Toggle (1)");
                            var h1b = tg1 != null ? FindChild(tg1, "Hit") : null;
                            var w1b = h1b != null ? h1b.GetComponent<WindowButton>() : null;
                            CheckTrue(w1b != null && w1b.onClick != null, "第二个分类键接了点击");
                            if (w1b != null) w1b.Click();
                            Check(tab4.Filter, ProfileData.TypeCollection, "点了 `Collection` ⇒ `filter` 跟着变");
                            Check(holder != null ? holder.childCount : -1, tab4.BuiltCells,
                                  "切分类 ⇒ **重建成新筛的那批**（`Refresh()` 先清空再 Instantiate）");
                            var back = FindChild(btns, "Achievement Type Toggle");
                            var hb0 = back != null ? FindChild(back, "Hit") : null;
                            var wb0 = hb0 != null ? hb0.GetComponent<WindowButton>() : null;
                            if (wb0 != null) wb0.Click();      // 切回 Battle（后面不留状态）
                            Check(tab4.Filter, ProfileData.TypeBattle, "再点回 `Battle`");
                        }
                    }
                    // ---- Ranking 页（2026-09-27 建 · 第 6 页）----
                    // 判据：`资料/普查产出_0927/档案窗_Ranking页与图名表.md`（§A 层×参数 · **§A·4 两处偏差** · §A·6 乱序）。
                    {
                        var k5 = FindChild(bar2, PlayerProfileWindow.Tabs[5].Node);
                        var h5 = k5 != null ? FindChild(k5, "Hit") : null;
                        var w5 = h5 != null ? h5.GetComponent<WindowButton>() : null;
                        if (w5 != null) w5.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileRanking, "点第 6 个键 ⇒ 切到 `Ranking` 页");

                        var rk = FindChild(t, "Ranked Tab");
                        CheckTrue(rk != null, "`Ranked Tab` 页根在");
                        var tab5 = pp.Page(WindowTabType.ProfileRanking) as RankedTab;
                        CheckTrue(tab5 != null, "这一页的组件是 `RankedTab`（类名照原版）");

                        CheckAtWorld(FindChild(rk, "Profile Player Info"), 351.03f, 1186.98f, 168.16f, 320.54f,
                                     "`Profile Player Info`（与 Profile 页同构，但是**另一个实例**）");
                        CheckAtWorld(FindChild(FindChild(rk, "Profile Player Info"), "Player Level"), 453.15f, 506.27f, 259.19f, 312.31f,
                                     "`Player Level`（圆底 + `-`）");
                        CheckTrue(FindChild(FindChild(rk, "Profile Player Info"), "Info Section with Alliance") != null
                                  && !FindChild(FindChild(rk, "Profile Player Info"), "Info Section with Alliance").gameObject.activeSelf,
                                  "`Info Section with Alliance` **关**（与 Profile 页同一条互斥判据）");

                        var t4 = FindChild(rk, "Top4");
                        CheckAtWorld(t4, 351.03f, 1186.99f, 333.95f, 886.97f, "`Top4`（四格阵营分）");
                        CheckAtWorld(FindChild(t4, "bg"), 351.03f, 1186.99f, 333.95f, 886.97f, "`Top4/bg`");
                        var t4c = FindChild(t4, "content");
                        CheckAtWorld(t4c, 367.75f, 1170.27f, 345.01f, 875.91f, "`Top4/content`（`HorizontalLayoutGroup`）");
                        for (int i = 1; i <= 4; i++)
                        {
                            float top = (i == 1 || i == 3) ? 355.01f : 635.46f;
                            float left = (i <= 2) ? 374.36f + 10f : 953.66f + 10f;
                            var fsb = FindChild(rk, "#" + i + " FactionScoreBig");
                            CheckAtWorld(fsb, left, left + 190f, top, top + 230.448f,
                                         $"`#{i} FactionScoreBig`（宽 **190** = 原版 prefab 序列化 `m_SizeDelta.x`（190, 230.44754），"
                                         + "与 uGUI 那一步 `Clamp(210 − 2×10, 164, 210)` 两条独立路同值 —— §A·4；"
                                         + "⛔ 不是原来那个 164（那是**卡内子件**的宽，见下面三条））");
                            // 🆕 **2026-10-06（A146）**：卡内三件**此前一条断言都没有** ⇒ 只改实现不会被自检抓到，补上。
                            //   期望值 = **原版 prefab 的字面量**（三件 `m_SizeDelta.x = 164` · `m_AnchoredPosition.x = 95`；
                            //   横向 95 = `(190−164)/2 + 164×0.5` = 13 + 82 ⇒ 左缘 = 卡左 + **13**；
                            //   纵向由三件的序列化 `ap.y`（−81.34302 / −174.74382 / −208.62457）+ 各自 `sizeDelta.y` 反算
                            //   ⇒ 8.442 / 154.244 / 195.244 / 222.005，这里写四位小数的字面量）。
                            //   ⛔ 不写 `RankedTab.CardInnerOff` / `CardInnerW`（那是被测实现**传进去的实参**，同式自证）。
                            //   ⚠️ 四张卡各自断一遍（`#1`/`#3` 上排、`#2`/`#4` 下排；两列 left 不同）。
                            CheckAtWorld(FindChild(fsb, "icon"), left + 13f, left + 177f,
                                         top + 8.44f, top + 154.24f,
                                         $"`#{i}` 的 `icon`（164 宽 —— 左缘 = 卡左 + 13、右缘 = 卡左 + 177）");
                            CheckAtWorld(FindChild(fsb, "Alliance Rating Display"), left + 13f, left + 177f,
                                         top + 154.24f, top + 195.24f,
                                         $"`#{i}` 的 `Alliance Rating Display`（**值**那一行，164×41）");
                            CheckAtWorld(FindChild(fsb, "MaxRating"), left + 13f, left + 177f,
                                         top + 195.24f, top + 222.01f,
                                         $"`#{i}` 的 `MaxRating`（**最高分**那一行，164×26.7615）");
                        }
                        // 🔴 `top4Factions` 的**数组顺序不是树序**（§A·6）—— 我们按树上的名字摆，并把这件事记死
                        CheckTrue(RankedTab.Top4Order[1] == "#3" && RankedTab.Top4Order[2] == "#2",
                                  "`top4Factions` 数组顺序 = `#1,#3,#2,#4`（**不是树序** —— 照树序抄会张冠李戴，§A·6）");

                        var ctr = FindChild(t4c, "center");
                        CheckAtWorld(ctr, 584.36f, 953.66f, 345.01f, 875.91f, "`center` 那一列");
                        CheckText(TextOf(FindChild(ctr, "DivisionText")), "Global Rating",
                                  "⚠️ 节点名叫 `DivisionText`，**装的是大标题 `Global Rating`**（照抄预制体）");
                        CheckAtWorld(FindChild(ctr, "DivisionImage"), 584.36f, 953.66f, 599.34f, 732.62f, "`DivisionImage`（段位大图形位）");
                        var gr = FindChild(FindChild(FindChild(FindChild(ctr, "footer"), "MainRating"), "Global Rating"), "Main Icon");
                        CheckAtWorld(gr, 591.29f, 591.29f + 58.6f, 770.59f, 827.94f,
                                     "中间列 `Main Icon` 的 x = **591.29**（**§A·4 的修正值**：表里那个 651.29 把出厂 F 的 `Secondary Icon` 也算进主轴了）");
                        CheckText(TextOf(FindChild(FindChild(FindChild(FindChild(ctr, "footer"), "MainRating"), "Global Rating"),
                                                   "Individual rating value")), RankedTab.ScoreEmpty,
                                  "全局评分 = **空态串 `------`**（照原版预制体自己的空态；`'32'`/`'3000'` 是示例数字，不抄）");

                        var af = FindChild(rk, "AllFactions");
                        CheckAtWorld(af, 1302.77f, 1746.97f, 264.81f, 886.97f, "`AllFactions`（阵营排行榜）");
                        CheckText(TextOf(FindChild(af, "Faction Ranking Points")), "Faction Rating", "表头文案 = `Faction Rating`");

                        // 🆕 2026-10-07（波 8 · Label 折行族）—— A62 子表 A · A39~A41 / A44 / A46
                        // ⛔ 新开一节，上面那些断言一条没动；判据全文 → `资料/普查产出_1007/波8_Label折行族.md`。
                        {
                            CheckWrapMode(FindChild(rk, "Player Name"), 0,
                                          "★ `Ranking Tab > Profile Player Info > … > Player Name` **不折行**（原版 `折行=0`）");
                            CheckWrapMode(FindChild(rk, "Player Title"), 0,
                                          "★ … `Player Title` **不折行**（原版 `折行=0`）");
                            CheckWrapMode(FindChild(FindChild(rk, "Profile Player Info"), "Avatar Name"), 1,
                                          "★ … `Avatar Name` **折行**（原版 `折行=1`；§③ 的「碰巧对」⇒ 补了显式 `wrap:true`）");
                            CheckWrapMode(FindChild(ctr, "DivisionText"), 0,
                                          "★ `Top4 > center > DivisionText`（装的是大标题 `Global Rating`）**不折行**（原版 `折行=0`）");
                            CheckWrapMode(FindChild(af, "Faction Ranking Points"), 0,
                                          "★ `AllFactions > Faction Ranking Points` **不折行**（原版 `折行=0`）");
                        }
                        CheckAtWorld(FindChild(af, "info"), 1686.86f, 1737.87f, 205.89f, 254.37f, "表头那个 info 图标");
                        CheckAtWorld(FindChild(af, "scroll rect"), 1305.29f, 1746.97f, 288.62f, 864.38f, "`scroll rect`");
                        var vp5 = FindChild(af, "viewport");
                        CheckAtWorld(vp5, 1305.29f, 1746.97f, 288.62f, 864.38f, "`viewport`（`RectMask2D`）");
                        var cont5 = FindChild(vp5, "content");
                        CheckAtWorld(cont5, 1305.29f, 1735.56f, 288.62f, 288.62f, "`content`（`VerticalLayoutGroup` + `ContentSizeFitter`）");
                        if (tab5 != null)
                        {
                            Check(tab5.BuiltRows, 4, "四行阵营排行（预制体里烘焙了 4 个 `FactionScoreSmall` 实例）");
                            var row0 = FindChild(cont5, "FactionScoreSmall");
                            CheckAtWorld(row0, 1305.29f, 1744.03f, 288.62f, 406.33f,
                                         "第 1 行（438.74×117.71；`content` 宽 430.27 ⇒ **行比容器宽 8.47**，真值）");
                            // 🔴 **2026-10-03 订正**：这几件**冒到视口顶之上**（`icon` 顶上冒 10.79），
                            //    而 `MenuDraw.Rect` 现在**横纵两轴都截 uv**（等价于原版 `viewport` 上那个
                            //    `RectMask2D` —— 原版**本来就会把它们裁掉**）⇒ 再拿「未裁整块的中心」去比
                            //    会差好几像素。**改成量渲出来的矩形**：**没被裁的那几条边**（下边缘 / 左右）
                            //    仍等于原版值，另外单独钉一条「上边缘真的被裁住了」。
                            {
                                float ix1, iy1, ix2, iy2;
                                var icn = FindChild(row0, "icon");
                                CheckTrue(RenderedRect(icn, out ix1, out iy1, out ix2, out iy2)
                                          && Mathf.Abs(ix1 - 1305.29f) < 1.5f && Mathf.Abs(ix2 - 1448.39f) < 1.5f
                                          && Mathf.Abs(iy2 - 406.33f) < 1.5f,
                                          "行里的 `icon`：左右 1305.29..1448.39 + 下边缘 406.33（原版值）—— 实测 "
                                          + $"x {ix1:F2}..{ix2:F2} · y {iy1:F2}..{iy2:F2}");
                                CheckTrue(iy1 > 277.83f + 0.5f,
                                          $"…上边缘**真被视口裁住了**（原版块顶 277.83，实渲 {iy1:F2}）");
                            }
                            var ard = FindChild(row0, "Alliance Rating Display");
                            {
                                // `Main Icon` 开了 `keepAspect` ⇒ **渲出来的宽比框窄**，只能比中心与下边缘
                                float mx1, my1, mx2, my2;
                                var mi = FindChild(ard, "Main Icon");
                                CheckTrue(RenderedRect(mi, out mx1, out my1, out mx2, out my2)
                                          && Mathf.Abs((mx1 + mx2) * 0.5f - (1295.14f + 1446.96f) * 0.5f) < 1.5f
                                          && Mathf.Abs(my2 - 384.23f) < 1.5f,
                                          "行里 `Main Icon`：**中心 x = 1371.05**（§A·4 的修正值）+ 下边缘 384.23 —— 实测 "
                                          + $"x {mx1:F2}..{mx2:F2} · y {my1:F2}..{my2:F2}");
                            }
                            var mx = FindChild(row0, "Alliance Rating Display (1)");
                            CheckTrue(mx != null, "`Alliance Rating Display (1)`（**最高分**那一行）建了");
                            var mxIc = FindChild(mx, "Main Icon");
                            var mxQ = mxIc != null ? mxIc.GetComponent<ImageQuad>() : null;
                            CheckTrue(mxQ != null && mxQ.Texture == (Texture)CardArt.MenuUi("Menu_Icon_Galon"),
                                      "最高分那一行的图标是 **`Menu_Icon_Galon`**（值那一行是 `40k_UI_icon_ranked_Skirmish` —— **两张不一样**，§A·2 末）");
                            // ⚠️ 这一栏**滚不动**是对的：4 行 × 117.711 = 470.84 < 视口高 575.76（`MenuScroll` 会夹到 0）
                            var pl5 = PointerLayer.Instance;
                            var r0 = FindChild(cont5, "FactionScoreSmall");
                            float y0r = r0 != null ? r0.position.y : 0f;
                            if (pl5 != null) pl5.WheelAt(1526f, 576f, -120f);
                            var r0b = FindChild(cont5, "FactionScoreSmall");
                            float y1r = r0b != null ? r0b.position.y : 0f;
                            CheckTrue(Mathf.Abs(y1r - y0r) < 0.001f,
                                      "内容比视口矮（4×117.711 = 470.84 < 575.76）⇒ **滚轮不动**才是对的（`MenuScroll` 夹到 0）");
                        }

                        // 点头像 ⇒ **跳到 Avatar 页**（原版 `RankedTab.<Initialize>b__10_0` 就是这条，§C·1 第 4 条）
                        var avHit = FindChild(FindChild(rk, "Profile Player Info"), "AvatarHit");
                        var avBtn = avHit != null ? avHit.GetComponent<WindowButton>() : null;
                        CheckTrue(avBtn != null && avBtn.onClick != null, "本页头像接了点击");
                        if (avBtn != null) avBtn.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点本页头像 ⇒ **切到 `Avatar` 页**（原版行为）");
                        var back5 = FindChild(bar2, PlayerProfileWindow.Tabs[5].Node);
                        var hb5 = back5 != null ? FindChild(back5, "Hit") : null;
                        var wb5 = hb5 != null ? hb5.GetComponent<WindowButton>() : null;
                        if (wb5 != null) wb5.Click();      // 切回 Ranking 页（后面不留状态）
                        Check(pp.CurrentTab, WindowTabType.ProfileRanking, "再切回 `Ranking` 页");
                    }
                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `Player Profile Window > Menu Area > Tab  Area >
                    //   Generic Window Red Background Big` 那颗 `Image` 的 rect（273.48,118.917 → 1824.52,962）；
                    //   ⛔ 不写 `PlayerProfileWindow.AreaL/RedT/…`（那是被测实现**传进去的实参**，同式自证）。
                    CheckAbsorbRule("玩家档案窗", pp.transform, "AbsorbHit",
                                    273.48f, 118.917f, 1824.52f, 962f,
                                    PlayerProfileWindow.QShade, PlayerProfileWindow.QHit, () => pp.CurrentState);
                    // ⚠️ 上面那一组**结尾就把窗关掉了** ⇒ 这里开回来，下面那句 `Close()` 才是**真**在关
                    //   （`PlayerProfileWindow.Open()` = `Build()` 重建，不依赖 `Data`，重开安全）。
                    CheckTrue(pp.TryOpen(null), "（A94 收尾）把档案窗开回来 —— 下面那句 `Close()` 才不是空断");
                    pp.Close();          // 六页都断完了才关窗（见上面那条注释）
                }
            }
        }

        // ============================================================ 遭遇战 / 排位 活动窗（2026-09-24 建）
        // 正本 `资料/阶段二_战斗入口_原版规格.md` §二 B/C；几何 2026-09-24 又从根实读复核过一遍。
        Section("遭遇战 `SkirmishModeEventWindow`（正本 §二 B）");
        {
            var sc = menu.Find("Base Game Mode Container 1x1 - Skirmish");
            var sh = FindChild(sc, "Hit");
            var swb = sh != null ? sh.GetComponent<WindowButton>() : null;
            CheckTrue(swb != null, "遭遇战卡有点击区");
            if (swb != null) swb.Click();
            var sk = SkirmishEventWindow.LastOpened;
            CheckTrue(sk != null, "点遭遇战卡 ⇒ **开出了 `SkirmishModeEventWindow`**");
            if (sk != null)
            {
                // 🆕 A47：压暗层命中区（`BackdropHit`）—— 档 = `QBg`(3104)（**压暗层自己那一档**），
                //   严格低于内容命中区最低档 `QHit`(3116)。改前是 `QHitBackdrop`(3115)
                //   = 「内容档再往上留一档」的写法（`LiveOpsEventWindow.QHitBackdrop` 的注释已订正）。
                //   🔴 A77⑬③：`QBg` 那个实参没了 —— 期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
                MenuDraw.CheckShadeRule(CheckTrue, "遭遇战窗", FindChild(sk.transform, "BackdropHit"),
                                        sk.transform.Find("Menu Dark Background"), LiveOpsEventWindow.QHit);
                Check(sk.type, WindowType.Popup, "`type` = **1 Popup**（§一 原文）");
                Check(sk.placement, WindowsPlacement.Canvas,
                      "`windowsPlacement` = **5 Canvas**（§一 原文 —— ⚠️ **不是练习窗那个 15**）");
                Check(sk.closeOnEsc, true, "`closeOnESC` = **1**（原文）");

                // 背景三层（§二 B）—— 红底那件是**九宫格**，`Menu Vignette` 是纯色
                CheckAtWorld(FindChild(sk.transform, "Reward Background Get Reward"), 0f, 1920f, 121.80f, 1013.11f,
                             "`Reward Background Get Reward`（`40k_general_popup_simple red`）");
                CheckAtWorld(FindChild(sk.transform, "Menu Vignette"), 0f, 1920f, 0f, 1080f, "`Menu Vignette`");

                // 中间那一栏
                CheckAtWorld(FindChild(sk.transform, "Ranked Deck Selection"), 525.30f, 1394.70f, -0.10f, 1080.10f,
                             "`Ranked Deck Selection`");
                CheckAtWorld(FindChild(sk.transform, "Warlod Image"), 450.93f, 1549.07f, -95.07f, 1003.07f,
                             "`Warlod Image`（1098.14²）");
                CheckAtWorld(FindChild(sk.transform, "Warlord Darkening"), 529.97f, 1470.03f, 418.18f, 975.82f,
                             "`Warlord Darkening`（`Smooth background square` Sliced · col(0,0,0,0.816)）");
                CheckAtWorld(FindChild(sk.transform, "Faction Icon Image"), 711.01f, 1233.64f, 133.50f, 656.14f,
                             "`Faction Icon Image`（522.63²）");
                // 🔴 **2026-09-24 更正**：这两行原版 `hAlign` 是 **Center**（旧 `menu_dump.py` 的 hAlign 映射
                //    错位一位，把它印成了 `Right`）⇒ 现在**不调 `Align*`**，节点就落在矩形中心 ⇒ 用 `CheckAtWorld` 断。
                //    （教训：**自检可以替错口径背书** —— 这条断言当时是绿的，但盯的是错的轴。）
                CheckAtWorld(FindChild(sk.transform, "Deck Name"), 694.70f, 1216.70f, 702.14f, 779.06f, "`Deck Name`（居中）");
                CheckAtWorld(FindChild(sk.transform, "Deck Warlord"), 694.70f, 1216.70f, 779.06f, 833.74f,
                             "`Deck Warlord`（居中）");
                CheckText(TextOf(FindChild(sk.transform, "Deck Name")), "菜单测试卡组 1",
                          "卡组名画的是**当前选中那套**（自检里第 1 套塞了督军）");
                // 四颗圆钮（HLG spacing 0 · align MiddleCenter · 每颗 160.87×128）
                foreach (var nm in new[] { "Previous Deck Button", "Change Deck Button", "View Deck Button", "Next Deck Button" })
                    CheckTrue(FindChild(sk.transform, nm) != null, $"`{nm}` 建了（四颗圆钮之一）");
                {
                    // 世界坐标 → 画布 px（本文件没有 `PxOf` 那种助手，照 `Deck Scroll View` 那条的写法）
                    var pb = FindChild(sk.transform, "Previous Deck Button");
                    float leftPx = pb != null ? pb.position.x * 108f + 960f - SkirmishEventWindow.DbBtnW * 0.5f : 0f;
                    CheckNear(leftPx, 631.26f, 0.6f,
                              "第 1 颗钮的左边界 = **631.26** = 栏中心 953.00 − 4×160.87/2（照原版 HLG 算）");
                }
                // 🆕 2026-10-03：`Next Deck Button` 的箭头**水平翻转**（原版挂 `UIFlippable`）。
                //   判据 = **uv 宽为负**（`SetUvRect(1,0,-1,1)`）—— 工程里 `CollectionWindow` / `MatchLogRow` 同一招。
                {
                    var nd = FindChild(FindChild(sk.transform, "Next Deck Button"), "Icon");
                    var nq = nd != null ? nd.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(nq != null && nq.UvRect.width < 0f,
                              "`Next Deck Button` 的箭头**已水平翻转**（原版 `UIFlippable`）—— 实测 uv.w = "
                              + (nq != null ? nq.UvRect.width.ToString("F2") : "无 quad"));
                    var pv = FindChild(FindChild(sk.transform, "Previous Deck Button"), "Icon");
                    var pq = pv != null ? pv.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(pq != null && pq.UvRect.width > 0f, "…而 `Previous Deck Button` 那颗**不翻**（原版只有 Next 翻）");
                }
                CheckText(TextOf(FindChild(sk.transform, "Numer Of Army Decks")),
                          CollectionData.DeckCount() + "/20",
                          "`Numer Of Army Decks` = **当前几套/20**（原版分母写死 20）");
                CheckTrue(sk.ArmyCells.Count >= 13 || sk.ArmyCells.Count > 0,
                          $"阵营列画了 {sk.ArmyCells.Count} 格（原版 `Army Content` 是 GridLayoutGroup 168² · 3 列）");
                // ============================================================ 🆕 2026-10-07（A118②）
                // **格内结构 + 图层档案**：上面只断了「画了几格」——节点结构 / 四层各占一档 / 命中区绑了 `target`
                // 这三件此前**一条都没被断**（`Army` 格照原版 item prefab 重建过一遍，没有网就等于随时能退回去）。
                // 判据 = `bundle_menus_assets_all/GameObject/Ranked Army Selector Container V2.json` 逐节点实读；
                // item prefab 是谁 = `ArmySelectorRanked__Initialize.c:286` + 三处实例同指 pid `−2100390822107119323`。
                if (sk.ArmyCells.Count > 0 && sk.ArmyCells[0] != null)
                {
                    var cell0 = sk.ArmyCells[0];
                    var bgN = FindChild(cell0, "Background");
                    var onN = bgN != null ? FindChild(bgN, "On") : null;
                    var barN = FindChild(cell0, "ProgressBar");
                    var areaN = barN != null ? FindChild(barN, "Fill Area") : null;
                    var icN = FindChild(cell0, "Army Icon");
                    var ftN = FindChild(cell0, "Featured Icon");
                    CheckTrue(bgN != null && onN != null, "格内 `Background` > `On` 都在（原版 `Toggle.graphic` 指 `On`）");
                    CheckTrue(onN != null && !onN.gameObject.activeSelf,
                              "★ `On` **出厂关着**（原版 `m_IsActive = false`；uGUI `Toggle.PlayEffect` 只 cross-fade "
                            + "alpha、**从不 SetActive** ⇒ 原版它一个像素都不画 —— 别以为漏了一层）");
                    CheckTrue(barN != null && areaN != null && FindChild(areaN, "Fill") != null
                              && FindChild(areaN, "Separator") != null,
                              "格内 `ProgressBar` > `Fill Area` > { `Fill`, `Separator` } 都在");
                    CheckTrue(icN != null && ftN != null,
                              "`Army Icon`（149.96²）与 `Featured Icon`（122.81×123.53）都在");
                    CheckTrue(FindChild(cell0, "Icon") == null && FindChild(cell0, "Highlight") == null,
                              "⛔ 旧实现的 `Icon` / `Highlight`（那张错用的 `Highlight_Rounded_Square`）**不再存在**");
                    // 底图：本窗默认 `ArmyIndex = -1`（不限阵营）⇒ **每一格都是未选中那张**
                    //   （`EverguildToggle` 的 `offSprite`；选中态由 `WindowButton` 换贴图承担）
                    var bgQ2 = QuadOf(bgN);
                    CheckTrue(bgQ2 != null && bgQ2.Texture != null && bgQ2.Texture.name == "UI_Army_Selection_Back",
                              "格的底图 = **`UI_Army_Selection_Back`**（`offSprite`；`ArmyIndex = -1` ⇒ 没有选中格）");
                    // 四层**各占一档**：同档时透明物按「到相机的距离」排 ⇒ 谁盖谁不可控
                    var sepN = FindChild(areaN, "Separator");
                    var sepQ = sepN != null ? sepN.GetComponentInChildren<ImageQuad>() : null;
                    var icQ = icN != null ? QuadOf(icN) : null;
                    var ftQ = ftN != null ? QuadOf(ftN) : null;
                    int qB = bgQ2 != null ? bgQ2.RenderQueue : -1;
                    int qBar = sepQ != null ? sepQ.RenderQueue : -1;
                    int qIc = icQ != null ? icQ.RenderQueue : -1;
                    int qFt = ftQ != null ? ftQ.RenderQueue : -1;
                    CheckTrue(qB >= 0 && qBar >= 0 && qIc >= 0 && qFt >= 0,
                              $"四层的队列都读到了（否则下一条等于没查）—— 底 {qB} · 进度条 {qBar}"
                            + $" · 阵营图 {qIc} · 特色角旗 {qFt}");
                    CheckTrue(qB != qBar && qB != qIc && qB != qFt && qBar != qIc && qBar != qFt && qIc != qFt,
                              "★ 格内四层**各占一档**（同档时谁盖谁不可控 —— 透明物按到相机的距离排）");
                    // 命中区**必须绑 `target`**：`MenuDraw.Hit` 不传 target ⇒ `Bind` 直接 return ⇒ 两档无声消失
                    var hit0 = FindChild(cell0, "Hit");
                    var wb0 = hit0 != null ? hit0.GetComponent<WindowButton>() : null;
                    CheckTrue(wb0 != null && wb0.target != null,
                              "★ 格的命中区 `WindowButton.target` **非空**（空 = 悬停/按下两档**无声消失**）");
                    CheckTrue(wb0 != null && wb0.target == bgQ2,
                              "…而且 target 指的就是 `Background` 那张 Image（原版 `spriteToChange` 指它）");
                    // `Army Icon` 的几何（**相对格**）：原版 `8.68,6.20 → 158.64,156.16` ⇒ 中心 = 格心 + (−0.34, −2.82)
                    //   （149.96² 在 168² 格里**不居中** —— 别按「居中」摆）
                    // 🔴🔴 **2026-10-08（A225-③）就地订正（铁律 5）：量的是「所有块的并集」，⛔ 不是节点位置。**
                    //   原来量 `icN.position`（宿主节点），而这一层压在**上渐隐带**里（`VpSoft = (0,52)`，
                    //   带内沿 = 视口上沿 218.94 + 52 = **270.94**，正落在图标 245.14..395.10 内部）
                    //   ⇒ `ApplySoftEdges` 把它**沿带内沿切开**，`PlaceCell` 会把**宿主挪到「含矩形中心的那一格」**
                    //   （下半格 ⇒ 中心 (270.94+395.10)/2 = 333.02）⇒ 拿节点当中心量出来的是**那一格**，
                    //   不是这一层画出来的地方：实测 **+10.08** vs 期望 **−2.82**（差 12.90px = 带切口到矩形中心的距离）。
                    //   ⇒ 判据 = `MenuDraw.ApplySoftEdges` 头部的 ⚠️「返回后原 quad 的矩形可能不再等于 `vis`
                    //     （它是其中一格）—— 断言要按「所有块的并集」量」，与同文件 `CheckLayerRect`
                    //     （㉒② 卡组格内景，`:6241`）用的是**同一个助手**（`UnionQuadRect`）同一条纪律。
                    //   诚实记一笔：**建出来的几何一直是对的**（并集恒等于 `InCell(rr, CellIcon)` ∩ 视口），
                    //   错的是这条断言的量法 —— 但量法必须修，否则它会把「软边生效」误判成「图标摆错」。
                    if (icN != null)
                    {
                        float ix1, iy1, ix2, iy2;
                        bool got = UnionQuadRect(icN, out ix1, out iy1, out ix2, out iy2);
                        CheckTrue(got, "（前提）`Army Icon` 底下量得到 `ImageQuad`（量不到 ⇒ 下面那条等于没查）");
                        if (got)
                        {
                            float dcx = (ix1 + ix2) * 0.5f - LayoutSpace.PxX(cell0.position.x);
                            float dcy = (iy1 + iy2) * 0.5f - LayoutSpace.PxY(cell0.position.y);
                            CheckTrue(Mathf.Abs(dcx + 0.34f) < 0.8f && Mathf.Abs(dcy + 2.82f) < 0.8f,
                                      "`Army Icon` 的中心（**宿主 + 全部 `_soft` 子块的并集**）= 格心 + (−0.34, −2.82)"
                                    + "（原版 8.68,6.20→158.64,156.16）—— 实测 "
                                    + $"({dcx:F2}, {dcy:F2})；并集 {ix1:F2},{iy1:F2}→{ix2:F2},{iy2:F2}"
                                    + "（⛔ 别改成量 `icN.position`：那一层压着渐隐带、宿主被挪进下半格，"
                                    + "量节点会得到 +10.08 的假红）");
                        }
                    }
                }

                // 顶上标题栏 + 两颗按钮
                CheckText(TextOf(FindChild(sk.transform, "Window Title")), "Game mode", "`Window Title` 文案");
                // 原版 hAlign = **Left**（旧 dump 错印成 Center）⇒ 比**渲出来的左边缘** = HLG 的 padLeft **155**
                {
                    float t1, t2, t3, t4;
                    bool ok = RenderedRect(FindChild(sk.transform, "Window Title"), out t1, out t2, out t3, out t4);
                    CheckTrue(ok, "`Window Title` 建了");
                    if (ok) CheckNear(t1, 155f, 2f, "`Window Title` **渲出来**的左边缘 = HLG 的 **padLeft 155**");
                }
                // 🆕 2026-10-03：原版这行 TMP `charSpacing = 5`（以前 `Label` 没接口 ⇒ 没复刻）
                CheckNear(CharSpacingOf(FindChild(sk.transform, "Window Title")), 5f, 0.01f,
                          "`Window Title` 的 `characterSpacing` = **5**（原版 TMP 原文）");
                CheckAtWorld(FindChild(sk.transform, "Header Back Button"), -24.40f, 143.48f, 42.88f, 154.21f,
                             "`Header Back Button`");
                // 🔴 **2026-09-24 更正**：原来这条写「本地没有那两张图」——**是错的**（`Resources/Art/ui_menu/`
                //    里 `40k_gamemode_icon_skirmish` / `_classic` 早在，练习窗那个 Toggle 用的就是它们）
                //    ⇒ 遭遇战窗接 `skirmish` 那张，**按贴图名断**（不是断言「没建」）
                {
                    var gmi = FindChild(sk.transform, "Game Mode Icon");
                    var gmq = gmi != null ? gmi.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(gmq != null && gmq.Texture != null && gmq.Texture.name == "40k_gamemode_icon_skirmish",
                              "`Game Mode Icon` 用的是 **`40k_gamemode_icon_skirmish`**（HLG 里紧跟标题：左沿 529.86）");
                }

                // `To Battle Button` + 两颗图标（**遭遇战是金杯**）
                CheckAtWorld(FindChild(sk.transform, "To Battle Button"), 1376.76f, 1817.09f, 917.80f, 1038.40f,
                             "`To Battle Button`");
                // ⚠️ `Button Text` 这个名字在树里有**三处**（另一处在「无督军」那块）⇒ 必须**限定父节点**找
                CheckText(TextOf(FindChild(FindChild(sk.transform, "To Battle Button"), "Button Text")), "Battle!",
                          "开战钮文案 = `Battle!`");
                {
                    var tq = FindChild(sk.transform, "TrophyIcon");
                    var tQuad = tq != null ? tq.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(tQuad != null && tQuad.Texture != null && tQuad.Texture.name == "WF_UI_Trophy_Gold",
                              "`TrophyIcon` 用的是 **`WF_UI_Trophy_Gold`**（遭遇战那一张）");
                    var sq = FindChild(sk.transform, "ShieldIcon");
                    var sQuad = sq != null ? sq.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(sQuad != null && sQuad.Texture != null && sQuad.Texture.name == "UI_icon_shield",
                              "`ShieldIcon` 用的是 `UI_icon_shield`");
                    // 🔴 **2026-10-05 就地订正（铁律 5）+ 补断言**：`ButtonIcons` 的 HLG 是
                    //    **`m_ReverseArrangement = 1`**（spacing **−28.31** · align `MiddleRight` · `expandW = 0`）
                    //    ⇒ **树序 `[ShieldIcon, TrophyIcon]` 倒排** ⇒ 视觉左→右 = `TrophyIcon` → `ShieldIcon`。
                    //    **原文**写「HLG align = MiddleRight ⇒ 从右边往左排」—— **理由错了**：
                    //    `align` 只管**整排的起点偏移**，**不决定子件顺序**（决定顺序的是 `m_ReverseArrangement`）。
                    //    判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`（`startIndex = reverse ? Count−1 : 0`）；
                    //    跑后矩形 = `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                    //    "SkirmishModeEventWindow" --depth 12 --md`（`RankedEventWindowV2` 逐值相同）：
                    //    `TrophyIcon` **1285.97,928.10→1385.97,1028.10** · `ShieldIcon` **1357.66,928.10→1457.66,1028.10**。
                    //    🔴 这两条断的是**建出来的世界坐标**（不是名字对不上名字）—— 改回镜像摆 ⇒ 两条一起红。
                    CheckAtWorld(tq, 1285.97f, 1385.97f, 928.10f, 1028.10f,
                                 "★ `TrophyIcon` 在**左**格（`reverse=1` ⇒ 树序倒排；照 `align` 推「从右往左」"
                                 + "会把它摆到右边 —— 与 `ShieldIcon` 正好互换 71.69px）");
                    CheckAtWorld(sq, 1357.66f, 1457.66f, 928.10f, 1028.10f,
                                 "★ `ShieldIcon` 在**右**格（右缘 1457.66 贴 `ButtonIcons` 右沿 1457.66）");
                }

                // 左列 `Reward Display` + 只有遭遇战有的 `Timer` / `Banned card in deck`
                CheckText(TextOf(FindChild(sk.transform, "Reward Tile")), "Progression", "左列标题 = `Progression`");
                {
                    // ⚠️ `Timer` 是 HLG（图标的位置由「倒计时文字多宽」决定）而那段文字本地没有
                    //    ⇒ **整件都不画**（连图标），只留节点 + 出声（原来画了个位置错的图标：偏左 207px、下端出屏）
                    var tm = FindChild(sk.transform, "Timer");
                    CheckTrue(tm != null, "`Timer` 节点在（原版这一件在**遭遇战**有、**排位根上没有**）");
                    CheckTrue(tm != null && tm.GetComponentInChildren<ImageQuad>() == null,
                              "`Timer` 里**没有画任何东西**（连图标都没画 —— 位置由本地没有的倒计时文字决定，出声）");
                }
                {
                    var ban = FindChild(sk.transform, "Banned card in deck");
                    CheckTrue(ban != null && !ban.gameObject.activeSelf,
                              "`Banned card in deck` 建了但**关着**（本地没有禁用卡表 —— 出声）");
                }
                Check(sk.MissingArt.Count, 0,
                      "遭遇战窗**图一张都不缺**（`Tex()` 会记账 —— 取不到的层 `MenuDraw` 是**静默不画**的）");
                CheckHoverSwap(sk.transform, "遭遇战窗");
                CheckTrue(FindChild(sk.transform, "Scoring Bar Event Score Info") != null,
                          "记分条建了（那一族被 `scl 1.2563` + `0.8696` 两层包着 ⇒ rect 是**绕中心乘回去**算的）");
                Shoot("04_遭遇战窗.png");

                // 🆕 2026-09-26：**「玩家自建遭遇卡组」这一条路的验收**（甲/乙/丙/丁 四件）。
                //    在这一版之前整条路是断的：`DeckEditorState.Skirmish` **全仓没有任何赋值点**
                //    ⇒ 编辑器恒按经典 30 张算、模式也带不进对局。
                //    判据（模式为什么挂在卡组上 / 原版在哪定模式 / 选卡组按模式筛）→
                //    `资料/加时与冲突模式_原版规格.md` §2.7。
                {
                    // ① 建一副遭遇卡组：照原版 `SelectDecksTab.CreateDeck`「**建组那一刻定模式**」
                    int before = CollectionData.DeckCount();
                    CollectionData.PendingEditDeck = -1;
                    sk.CreateDeckInMode();
                    Check(CollectionData.DeckCount(), before + 1, "点 `Create deck` ⇒ 卡组库里多一套");
                    var made = CollectionData.Raw(CollectionData.DeckCount() - 1);
                    CheckTrue(made != null && made.GameMode == (int)GameMode.Skirmish,
                              "★ 建出来的那套 `GameMode` = **13（遭遇）**（实得 "
                            + (made == null ? -999 : made.GameMode) + "）—— **建组那一刻定死**，之后没有改的路径");
                    Check(CollectionData.PendingEditDeck, CollectionData.DeckCount() - 1,
                          "★ 交接下标指向新那套（切 `DeckEditor` 时编辑器打开它 ⇒ 编辑器按**遭遇那套规则**跑：上限 12 张）");
                    Check(CollectionData.CurrentIndex(), CollectionData.DeckCount() - 1,
                          "新卡组即选中（照原版 `DeckLibrary.Create` 的行为）");

                    // ② 模式不对的牌**开不了战、且出声**（不静默）—— 原版同款判据：
                    //    `SkirmishEventWindow.OnDeckSelected` → `RankedDeckSelector.HasValidDeckWithValidationMessage`
                    //    → `DeckUtility.ValidateDeck(deck, out err, isSkirmish)`
                    int classicIdx = -1;
                    for (int i = 0; i < CollectionData.DeckCount(); i++)
                    { var dd = CollectionData.Raw(i); if (dd != null && dd.GameMode == 0) { classicIdx = i; break; } }
                    CheckTrue(classicIdx >= 0, "库里有一套经典卡组（自检夹具 —— 用来验「遭遇窗不收经典牌」）");
                    CheckTrue(!sk.StartedBattle, "（前提）此刻还没开过战");
                    if (classicIdx >= 0)
                    {
                        sk.DeckIndex = classicIdx;
                        string whyBad;
                        CheckTrue(!sk.SelectedDeckFitsMode(out whyBad) && !string.IsNullOrEmpty(whyBad),
                                  "★ 遭遇窗里选中一副**经典**卡组 ⇒ 判为不合模式，并给出一句人话：" + whyBad);
                        sk.StartMatch();
                        CheckTrue(!sk.StartedBattle,
                                  "★ 点 `Battle!` **真的没开成**（挡在 `StartMatch` 里，弹窗说明原因）—— 不是静默放行");
                        var spBad = FindChild(sk.transform, "Searching Oponent Popup (1)");
                        CheckTrue(spBad == null || !spBad.gameObject.activeSelf,
                                  "★ 而且**匹配窗都没弹**（挡在匹配之前）—— 拦得足够早");
                    }

                    // 🔴 **2026-10-11（F2）修夹具一处回归**（`资料/普查产出_1010/D1_十二条红诊断.md` §B-2/B-3）：
                    //   `sk.StartMatch()` 上面那句被模式校验挡下时会走 `Manager.ShowPopUp` ⇒ 开出一扇
                    //   `PromptPopup`。**A217③ 之后**（照原版 `WindowsManager__OpenWindowCO.c` 第 50 行）弹窗支
                    //   **也写 `currentWindow`** ⇒ 那一刻 `sk.ToBackground()`（物体仍 active，所以截图看不出）。
                    //   而 `PointerLayer.PointerReachable` 对 `Background` 的窗**整批跳过** ⇒ `sk` 子树里
                    //   **一个点都命中不到** ⇒ 下面 `CheckAbsorbRule("匹配弹窗", …)` 与 `CheckAbsorbRule("遭遇战窗", …)`
                    //   的 `found` 假（两条红，各打两次）。
                    //   🔴 **裁定（⛔ 别重开）**：**A217③ 是对的（照原版）**、**`PointerReachable` 不许放宽**
                    //      （那是 A77⑮④ 的判据，一放宽就动全壳）—— **要改的是夹具**：
                    //      原版那条链里弹窗关掉时会走 `CloseWindowCO → ShowPreviousWindow` 把底窗**带回 `Open`**，
                    //      而自检里这扇弹窗**从没被关过** ⇒ 底窗停在 `Background` 没人救。
                    //      ⇒ 在这里显式关掉它（写法与 `:1403` / `:3465` 那两处「用完就关」同源，
                    //        只是这里**只关这一扇**：`ShowPopUp` 刚开的那颗就是「最上面那扇」= `TopWindow`）。
                    //   🔴 **改坏法**：把这两句删掉（= 把那个 `PromptPopup` 留在开着）⇒ 上面两条 `found` 又红。
                    {
                        var promptMode = sk.Manager != null ? sk.Manager.TopWindow as PromptPopup : null;
                        CheckTrue(promptMode != null, "（夹具）模式不对那一句真的弹了 `PromptPopup`（下面那句 `Close()` 才不是空断）");
                        if (promptMode != null) promptMode.Close();
                        Check(sk.CurrentState, WindowState.Open,
                              "（夹具）关掉弹窗 ⇒ `ShowPreviousWindow` 把 `sk` **带回 `Open`**"
                            + "（= 下面两组吸收层断言的前提：那一刻 `sk` 是顶窗、没被 `Background`）");
                    }

                    // ③ 给新那套补个督军（`CreateDeckInMode` 建的是**空牌**；没督军 `Battle!` 照样会如实拒绝），
                    //    然后重开一次窗口 —— 它会**吸附到本窗模式下能用的那一套**（原版是列表里只剩同模式的）
                    {
                        var libM = DeckLibrary.Load();
                        var mine = libM.Decks[libM.Count - 1];
                        foreach (var c in CardDatabase.Load())
                            if (c.Type == "hero") { mine.WarlordId = c.Id; break; }
                        libM.Save();
                        CollectionData.ResetForTest();
                    }
                    sk.Open();                        // 重开：按新模式吸附 + 重画（`_search` 也会重建）
                    var landed = CollectionData.Raw(sk.DeckIndex);
                    CheckTrue(landed != null && landed.GameMode == (int)GameMode.Skirmish,
                              "★ 重开窗 ⇒ **自动落在遭遇那套**上（默认选中那套模式不对的话，一进来点 Battle! 就会被挡）"
                            + "（实得 " + (landed == null ? "null" : landed.Name + " · mode " + landed.GameMode) + "）");
                    string whyOk;
                    CheckTrue(sk.SelectedDeckFitsMode(out whyOk), "★ 选中**遭遇**那套 ⇒ 合模式（可以开战）");

                    // ④ 选卡组弹窗的「我的卡组」页**按模式筛** —— 原版 `DeckSelectionPopup` 的 `TryOpen`
                    //    拿着 `DeckSelectionContext`，筛选 lambda = `候选.GameMode == context.Deck.GameMode
                    //    || 候选.GameMode == context.GameMode`（`DeckSelectionPopup___TryOpen_b__10_0.c:10-17`）
                    {
                        var dsp = sk.OpenDeckSelection();
                        CheckTrue(dsp != null, "遭遇窗里开得出 `Deck Selection Popup`");
                        if (dsp != null)
                        {
                            int skCount = 0, allCount = CollectionData.DeckCount();
                            for (int i = 0; i < allCount; i++)
                            { var dd = CollectionData.Raw(i); if (dd != null && dd.GameMode == (int)GameMode.Skirmish) skCount++; }
                            dsp.SwitchTab(true);
                            Check(dsp.ShownCount, skCount,
                                  $"★「我的卡组」页**只列遭遇那批**（{skCount} 套 / 全库 {allCount} 套）"
                                + " —— 经典那几套**不出现**（不是列出来再拦）");
                            dsp.Close();
                        }
                    }
                }

                // 开战链：`Battle!` → `StartMatch`（匹配窗 + 12 秒）→ `StartBotBattle`
                {
                    var hit = FindChild(sk.transform, "BattleHit");
                    var hb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    CheckTrue(hb != null, "`Battle!` 有点击区");
                    if (hb != null) hb.Click();
                    var sp = FindChild(sk.transform, "Searching Oponent Popup (1)");
                    CheckTrue(sp != null && sp.gameObject.activeSelf,
                              "点 `Battle!` ⇒ `Searching Oponent Popup` 出来（原版 `MatchMakerManager.StartMatch`）");
                    CheckTrue(!sk.StartedBattle, "12 秒还没到 ⇒ 不抢跑");
                    sk.TickSearch(12f);
                    CheckTrue(!(sp != null && sp.gameObject.activeSelf), "等满 12 秒 ⇒ 匹配窗自己关掉");
                    CheckTrue(sk.StartedBattle, "等满 12 秒 ⇒ **开战成立**（真机上 `LoadScene(\"Battle\")`）");
                }
                // 🆕 2026-10-03（§三 第 29 条 **A2**）：**联机等待态** —— 显示窗内那扇弹窗、
                //    **不跑那 12 秒 bot 倒计时**（等的是真人）。原来 P2P 那条路上三扇模式窗
                //    `Searching` 是 false 又不开全屏窗 ⇒ **屏幕上什么都没有**。
                {
                    var spn = FindChild(sk.transform, "Searching Oponent Popup (1)");
                    var pop = spn != null ? spn.GetComponent<SearchingMatchPopup>() : null;
                    CheckTrue(pop != null, "匹配弹窗上挂着 `SearchingMatchPopup`");
                    if (pop != null)
                    {
                        pop.Show();
                        // 🆕 A47：压暗层命中区（`BackdropHit`）—— 档 = `QSr`(3130)，< 内容命中区档 `QSrHit`(3135)
                        //   （改前是 `QSrHitBackdrop` = 3134 = 「内容档 − 1」的写法）
                        //   ⚠️ 放在 `Show()` **之后**：这一颗出厂是关着的（`Attach` 里 `SetActive(false)`），
                        //     而 `ShadeRuleOk` 查 `ImageQuad` 用的是**不含未激活**的 `GetComponentInChildren`。
                        //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
                        MenuDraw.CheckShadeRule(CheckTrue, "匹配弹窗", FindChild(pop.transform, "BackdropHit"),
                                                pop.transform.Find("Menu Dark Background"), SearchingMatchPopup.QSrHit);
                        // 🆕 **2026-10-07（A77⑬⑫）：这一处的 `RectMask2D.m_Padding` 该不该补 —— 结账 = 【不补】。**
                        //   原版那份 pad = `(0,9.69,0,9.69)`，路径 `…/Searching Oponent Popup/Window`
                        //   （逐处实读表 `_tmp_view/q1_rm2d.txt` 那 5 条里的一条；⚠️ 判据文件原写「只有锻造轨道
                        //   那两处」是**按「滚动区 Viewport」**定义的 ⇒ 没覆盖它，这一笔此前**没人记过账**）。
                        //   它内缩的是 **`Window` 那个框**（UGUI `Culling/Clipping.cs:26-30`），**不是钮自己的矩形**；
                        //   而 `Cancel` 那颗命中区**整块都在内缩后的框里** ⇒ 加不加 padding，**命中面一模一样**
                        //   （真去补 `hitPad` 会把 75 高的钮削成 **55.62** = 75 − 9.69×2 ⇒ 那是**引入**差异，不是复刻）。
                        //   期望值全是**原版字面量**：`Window` 的 rect（`560.00,234.07→1360.00,685.93`）与钮的 rect
                        //   （`720.83,569.93→1199.17,644.93`）都出自 `资料/阶段二_战斗入口_原版规格.md:134,138`；
                        //   pad 出自上面那张实读表。⛔ 不写 `SearchingMatchPopup.BtnT/BtnB/WinT/WinB`（那是被测实参）。
                        {
                            const float PadT = 9.69f, PadB = 9.69f, OT = 234.07f, OB = 685.93f;   // 原版 pad / Window rect
                            float bx1, by1, bx2, by2;
                            CheckTrue(HitQuadRect(FindChild(pop.transform, "CancelHit"), out bx1, out by1, out bx2, out by2),
                                      "`CancelHit` 的**渲染矩形**量得到（下面三条全靠它）");
                            CheckNear(by1, 569.93f, 1.5f, "`CancelHit` 上沿 = **原版那颗钮的** rect（没被 pad 削过）");
                            CheckNear(by2, 644.93f, 1.5f, "`CancelHit` 下沿 = 同上（补了 `hitPad` 会少 9.69）");
                            CheckTrue(by1 > OT + PadT && by2 < OB - PadB,
                                      $"★ 命中区整块在**内缩 9.69 之后**的 `Window` 框里（{OT + PadT} < {by1:F2} … {by2:F2}"
                                      + $" < {OB - PadB}）⇒ 那个 `RectMask2D` 的 padding 对这一颗**没有任何差异**"
                                      + "（⛔ 别给它补 `hitPad` —— 补了上面那两条立刻红，而且那是引入偏离）");
                        }
                        pop.BeginNetWait();
                        CheckTrue(pop.IsShowing, "联机等待态：**窗显示出来**（原来是屏幕上什么都没有）");
                        CheckTrue(!pop.Searching, "…而且**不跑**那 12 秒 bot 倒计时（条件 `Searching` 为假）");
                        pop.Tick(20f);          // 推进 20 秒 —— 单机那一支 12 秒就该转 bot 了
                        CheckTrue(pop.IsShowing && !pop.Searching, "推进 20 秒也**不会自己收掉**（等真人等到为止）");
                        CheckTrue(pop.TypedText.Length > 0, "打字机照跑（联机等待也显示原版那句 `Searching`）");
                        // 🆕 2026-10-03：**`Cog` 自转**（原版 `Skull` 上那条 legacy `Animation` + clip `SearchingOpponentCog`）
                        //   判据 = 那条 clip 的 37 个键（`AnimationClip_-6716633893720174568.json`）。抽 6 个点核。
                        var S = CardPresentation.SearchingMatchPopup.CogLoop;
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(0f), 0f, 0.01f,
                                  "`Cog` 曲线 t=0 ⇒ 0°（clip 第 1 键）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(0.41666666f), 20f, 0.01f,
                                  "t=0.4167 ⇒ 20°（第 2 键）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(0.55f), 20f, 0.01f,
                                  "t=0.55 ⇒ 仍 20°（**保持段** —— 曲线是「补间 + 保持」而不是匀速）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(2.65f), 100f, 0.01f, "t=2.65 ⇒ 100°");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(9.883333f), 360f, 0.01f,
                                  "t=9.8833 ⇒ 360°（一圈走满）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(S), 0f, 0.01f,
                                  "走到一圈末尾 ⇒ **回绕到 0°**（原版 `m_WrapMode = 2 Loop`）");
                        pop.Show(); pop.Tick(0.41666666f);          // 从头放 0.4167s
                        var cogN = FindChild(spn, "Cog");
                        CheckTrue(cogN != null
                                  && Mathf.Abs(Mathf.DeltaAngle(cogN.localRotation.eulerAngles.z, 20f)) < 0.5f,
                                  $"`Tick` 之后 `Cog` 节点**真的转到 20°**（实测 "
                                  + (cogN != null ? cogN.localRotation.eulerAngles.z.ToString("F2") : "无节点") + "）");
                        // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                        //   期望矩形 = **原版 prefab** `Searching Oponent Popup > Window >
                        //   Generic Popup Background` 那颗 `Image` 的 rect（560,234.07 → 1360,685.93）；
                        //   ⛔ 不写 `SearchingMatchPopup.WinL…`（那是被测实现**传进去的实参**，同式自证）。
                        //   ⚠️ 这一扇的状态不是 `GameWindow.CurrentState`（它是**普通 `MonoBehaviour`**）
                        //   ⇒ 用 `IsShowing`；它的压暗层那颗动作是 `Cancel()`（不是裸 `Close()`，别据此去改）。
                        CheckAbsorbRule("匹配弹窗", pop.transform, "AbsorbHit",
                                        560f, 234.07f, 1360f, 685.93f,
                                        SearchingMatchPopup.QSr, SearchingMatchPopup.QSrHit,
                                        () => pop.IsShowing ? WindowState.Open : WindowState.Closed);
                        pop.Hide();
                    }
                }
                // ⚠️ **用完还原**：这一节把选中的那套换成了**遭遇**牌，而下面排位窗是本窗的兄弟
                //    （`DeckGameMode` 默认经典）⇒ 不还回去的话排位那一节会被同样的关卡挡掉。
                CollectionData.Select(0);
                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                //   期望矩形 = **原版 prefab** `SkirmishModeEventWindow > Reward Background Get Reward`
                //   那颗 `Image` 的 rect（0,121.80 → 1920,1013.11）；⛔ 不写 `SkirmishEventWindow.RedL…`
                //   —— 那是被测实现**传进去的实参**（同式自证）。⚠️ 红底**之外**那两条窄边仍是「点外面关窗」
                //   （所以 (5,5) 正好落在那条窄边里）。
                CheckAbsorbRule("遭遇战窗", sk.transform, "AbsorbHit",
                                0f, 121.80f, 1920f, 1013.11f,
                                LiveOpsEventWindow.QBg, LiveOpsEventWindow.QHit, () => sk.CurrentState);
                // ⚠️ 上面那一组**结尾就把窗关掉了** ⇒ 开回来，下面那句才是**真**在断 `Close()`
                //   （`LiveOpsEventWindow.Open()` = `Build()` 重建，不依赖 `Data`，重开安全）。
                CheckTrue(sk.TryOpen(null), "（A94 收尾）把遭遇战窗开回来 —— 下面那句 `Close()` 才不是空断");
                sk.Close();
                Check(sk.CurrentState, WindowState.Closed, "关掉遭遇战窗");
            }
        }

        Section("排位 `RankedEventWindowV2`（正本 §二 C · **与遭遇战逐格相同的那一半靠继承**）");
        {
            var rc = menu.Find("Base Game Mode Container 1x1 - Ranked");
            var rh = FindChild(rc, "Hit");
            var rwb = rh != null ? rh.GetComponent<WindowButton>() : null;
            CheckTrue(rwb != null, "排位卡有点击区");
            if (rwb != null) rwb.Click();
            var rk = RankedEventWindow.LastOpened;
            CheckTrue(rk != null, "点排位卡 ⇒ **开出了 `RankedEventWindowV2`**");
            if (rk != null)
            {
                // 🆕 A47：压暗层命中区（`BackdropHit`）—— 档 = `QBg`(3104)（**压暗层自己那一档**），
                //   严格低于内容命中区最低档 `QHit`(3116)。改前是 `QHitBackdrop`(3115)。
                //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档 ——
                //      本窗那块的父是 `General Red Background`（见 `RankedEventWindow.BuildBackdrop`）⇒ 走路径找。
                MenuDraw.CheckShadeRule(CheckTrue, "排位窗", FindChild(rk.transform, "BackdropHit"),
                                        rk.transform.Find("General Red Background/Menu Dark Background"),
                                        LiveOpsEventWindow.QHit);
                Check(rk.placement, WindowsPlacement.Canvas, "`windowsPlacement` = **5 Canvas**（原文）");
                CheckAtWorld(FindChild(rk.transform, "General Red Background"), 0f, 1920f, 634.34f, 445.66f,
                             "`General Red Background`（容器本身没有图 —— 四个背景层才是画面）");
                CheckTrue(FindChild(rk.transform, "Noise") != null,
                          "`Noise` 建了（`UI Dirt And Noise skratches` Tiled —— **只有排位窗有这一层**）");
                CheckAtWorld(FindChild(rk.transform, "Menu Vignette"), -960f, 2880f, -28.23f, 1053.32f,
                             "`Menu Vignette`（⚠️ 排位窗的值与遭遇战**不一样**：−960,−28.23→2880,1053.32）");
                CheckText(TextOf(FindChild(rk.transform, "Rank Title")), "Rank", "左列标题 = `Rank`");
                Check(rk.MissingArt.Count, 0, "排位窗**图一张都不缺**（同遭遇战那条）");
                CheckHoverSwap(rk.transform, "排位窗");
                CheckText(TextOf(FindChild(FindChild(rk.transform, "LeaderboardButton"), "Button Text")), "Leaderboard",
                          "`LeaderboardButton` 文案（同样要限定父节点 —— `Button Text` 树里有三处）");
                CheckTrue(FindChild(rk.transform, "ChangeRankedToggle") != null, "`ChangeRankedToggle` 建了");
                // 🆕 2026-10-03：开关那两行原版 TMP 带 `charSpacing = -4`；排位窗的 `Game Mode Icon`
                //   取 **classic** 那张（原版由 `playMode` 喂图，本地只有 classic/skirmish 两张，
                //   而排位走的就是经典模式 ⇒ 映射到 classic，**属推断、代码里已如实标**）
                {
                    var tg0 = FindChild(rk.transform, "ChangeRankedToggle");
                    CheckNear(CharSpacingOf(FindChild(tg0, "UnrankedText")), -4f, 0.01f,
                              "`UnrankedText` 的 `characterSpacing` = **−4**（原版 TMP 原文）");
                    CheckNear(CharSpacingOf(FindChild(tg0, "RankedText")), -4f, 0.01f,
                              "`RankedText` 的 `characterSpacing` = **−4**");
                    var gmi2 = FindChild(rk.transform, "Game Mode Icon");
                    var gmq2 = gmi2 != null ? gmi2.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(gmq2 != null && gmq2.Texture != null && gmq2.Texture.name == "40k_gamemode_icon_classic",
                              "排位窗的 `Game Mode Icon` = **`40k_gamemode_icon_classic`**（按 `playMode = Classic` 推的）");
                }
                CheckTrue(FindChild(rk.transform, "Timer") == null,
                          "**排位窗根上没有 `Timer`**（实读；⚠️ 原版 `Ranked Division Info/Content` **里面**"
                          + "还有一个 `Timer`，那一整棵我们没建 —— 别把这句读成「排位窗没有倒计时」）");
                CheckTrue(FindChild(rk.transform, "Banned card in deck") == null,
                          "**排位窗没有 `Banned card in deck`**（同上，反向断言）");
                {
                    var tq = FindChild(rk.transform, "TrophyIcon");
                    var tQuad = tq != null ? tq.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(tQuad != null && tQuad.Texture != null
                              && tQuad.Texture.name == "40k_ranking_icon_trophy_Plus",
                              "`TrophyIcon` 用的是 **`40k_ranking_icon_trophy_Plus`**（§二 C：与遭遇战不同）");
                    // 🔴 **2026-10-05（A89）**：`ButtonIcons` 的倒排是**基类那一份**实现的
                    //    （`Shell/LiveOpsEventWindow.cs` 的 `BuildToBattle`；本窗只换 `TrophyIconArt`）
                    //    ⇒ 排位这边**也要钉一次**：`RankedEventWindowV2` 的 `ButtonIcons` 逐值相同。
                    //    判据 = `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                    //    "RankedEventWindowV2" --depth 12 --md`；`reverse=1` 的语义见上一条注释。
                    var sq = FindChild(rk.transform, "ShieldIcon");
                    CheckAtWorld(tq, 1285.97f, 1385.97f, 928.10f, 1028.10f, "★ 排位窗 `TrophyIcon` 同样在**左**格");
                    CheckAtWorld(sq, 1357.66f, 1457.66f, 928.10f, 1028.10f, "★ 排位窗 `ShieldIcon` 同样在**右**格");
                }
                // 🔴 2026-09-26 修一处**截图污染**：上面那扇「模式不对」的模态提示窗一直没关，
                //    把 `05_排位窗.png` 与 `06_找对手窗.png` 都盖住了（看图才发现 —— 断言全绿）。
                //    同 `:401` 那个口子。
                foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None))
                    if (pp != null) pp.Close();
                Shoot("05_排位窗.png");
                // 排位那条路比另外三扇多一步：`Battle!` ⇒ **先开全屏 `SearchingOpponentWindow`**（入口是我们定的）
                {
                    var hit = FindChild(rk.transform, "BattleHit");
                    var hb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    CheckTrue(hb != null, "`Battle!` 有点击区");
                    if (hb != null) hb.Click();
                    var so = SearchingOpponentWindow.LastOpened;
                    CheckTrue(so != null && so.gameObject.activeSelf,
                              "排位：`Battle!` ⇒ 开**全屏** `SearchingOpponentWindow`（**本地入口是我们定的** —— 原版查不到）");
                    if (so != null)
                    {
                        Check(so.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（§一 原文）");
                        Check(so.placement, WindowsPlacement.None,
                              "`windowsPlacement` = **0 None**（四扇窗里只有它这样）");
                        // 原版 `Title` 的 hAlign = **Center**（旧 dump 错印成 Right）⇒ 节点就在矩形中心
                        CheckAtWorld(FindChild(so.transform, "Title"), 741.12f, 1178.88f, 136f, 186f,
                                     "`Title`（\"Searching opponent\" fs36 · 居中）");
                        CheckAtWorld(FindChild(so.transform, "Cancel Match"), 785f, 1135f, 942f, 1018f, "`Cancel Match`");
                        var ply = FindChild(so.transform, "Searching Opponent Player Info Container");
                        var en = FindChild(so.transform, "Searching Opponent Enemy Info Container");
                        var pf = FindChild(ply, "Found Player Container");
                        var ef = FindChild(en, "Found Player Container");
                        CheckTrue(pf != null && pf.gameObject.activeSelf,
                                  "**玩家**那一格走 `Found`（立绘 + 名字）");
                        CheckTrue(ef != null && !ef.gameObject.activeSelf,
                                  "**对手**那一格走 `Not Found`（空态 —— 原版 `Initialize(null,…)` 那一支）");
                        CheckTrue(FindChild(en, "Not Found Container") != null
                                  && FindChild(en, "Not Found Container").gameObject.activeSelf,
                                  "`Not Found Container` 露出来（原版它**没有图**，100×100 空框）");
                        // ================================================================
                        //  🆕 「找到对手」那一态（联机那一支）—— 判据 → `资料/阶段二_多人界面_原版规格.md` §6·4
                        //  🔴 这一态的**时机/时长/名字**原版没有可抄（`OpponentFound` 零调用点，是死代码）
                        //     ⇒ 那些是**我们定的**；但**画出来的东西照原版**：督军立绘 = 对手本局那副牌的督军 ·
                        //     立绘 **uv 镜像** · 名字**不翻** · `Found` / `Not Found` **二选一**。
                        // ================================================================
                        {
                            CheckTrue(CardPresentation.Net.NetMatchmaking.HoldForPresentation == null,
                                      "**没配对时不注册**「切场景前那一口气」（单机那条 12 秒链行为一字不改）");
                            // 拿原版那张立绘的主人当例子：`UM3` = Uriel Ventris
                            //（原版 prefab 敌方那格填的就是 `UI_Deck_Warlord_Uriel Ventris`，正本 §6·2）
                            so.ShowOpponent("UM3", "青剑湖");
                            CheckTrue(ef != null && ef.gameObject.activeSelf, "配到人 ⇒ **对手那格翻成 `Found`**");
                            var nf2 = FindChild(en, "Not Found Container");
                            CheckTrue(nf2 != null && !nf2.gameObject.activeSelf,
                                      "同一条：`Not Found Container` 收起来（**二选一**，不是两个都亮）");
                            var aq = FindChild(ef, "Warlord Image") != null
                                     ? FindChild(ef, "Warlord Image").GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(aq != null && aq.Texture != null,
                                      "对手那格画出了督军立绘（`UM3` 的 `art_um3` 取得到）");
                            CheckTrue(aq != null && aq.UvRect.width < 0f,
                                      "🔴 立绘 **uv 镜像**（原版 `m_LocalScale=(-1,1,1)`）—— 量的是**真 uv**（宽为负）；"
                                      + "⚠️ 改父节点 scale 是另一回事：`MenuDraw` 先算世界坐标再减父位置，父一有 scale"
                                      + "子件位置会被再乘一次（`资料/已知的坑.md:306`）");
                            CheckTrue(TextOf(FindChild(ef, "Player Name")) == "青剑湖",
                                      "名字换成**对面的真名**（不再是 prefab 那行 `Player name`）");
                            CheckTrue(TextOf(FindChild(ef, "Player Name")) != SearchingOpponentWindow.PlaceholderName,
                                      "**占位名必须被覆盖掉** —— 否则玩家会把 `Player name` 当成对手的真名");
                            // 名字那行**不跟着翻**（原版只翻立绘、不翻文字）
                            var nm = FindChild(ef, "Player Name");
                            var nq = nm != null ? nm.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(nq == null || nq.UvRect.width > 0f, "名字那行**不跟着翻**（原版只翻立绘）");
                            // 「切场景前那一口气」：拦下 ⇒ 到点才放行
                            so.NetWatch = true;
                            so.PresentationHold = 1f;
                            bool done = false; so.OnPresentationDone = () => { done = true; };
                            CheckTrue(so.HoldSceneForPresentation(null),
                                      "配到人时 `HoldForPresentation` **拦下这一次切场景**（让界面先展示）");
                            so.Tick(0.5f); CheckTrue(!done, "展示中途（0.5s / 1s）**还不放行**");
                            so.Tick(0.6f); CheckTrue(done,
                                      "到时（1.1s / 1s）⇒ 放行切战场（1s 取原版 `StartBattleWithDelay` 的 `waitLoadTime`）；"
                                      + "⚠️ **拿它当展示时间是我们挑的**");
                            so.OnPresentationDone = null;
                            // 关窗 ⇒ **必须把那一口气放掉**（否则这一局卡在「切不了场景」，静默失败）
                            CardPresentation.Net.NetMatchmaking.HoldForPresentation = so.HoldSceneForPresentation;
                            so.Close();
                            CheckTrue(CardPresentation.Net.NetMatchmaking.HoldForPresentation == null,
                                      "🔴 关窗时**把那一口气放掉**（不然这一局永远切不了场景）");
                            so.gameObject.SetActive(true);      // 后面的 `Shoot` 还要拍它
                        }
                        Shoot("06_找对手窗.png");
                        // 取消 ⇒ 那扇窗自己关掉，**排位窗还在**（它是弹窗，不在 `currentWindow` 那个位上）
                        var ch = FindChild(so.transform, "CancelHit");
                        var cb = ch != null ? ch.GetComponent<WindowButton>() : null;
                        CheckTrue(cb != null, "`Cancel` 有点击区");
                        if (cb != null) cb.Click();
                        Check(so.CurrentState, WindowState.Closed, "点 `Cancel` ⇒ 找对手窗关上");
                        CheckTrue(rk.CurrentState != WindowState.Closed, "**排位窗还在**（没被那扇全屏窗带走）");
                    }
                    // 再走一遍：这回不取消，等满 12 秒 ⇒ 开战
                    if (hb != null) hb.Click();
                    rk.TickSearch(12f);
                    CheckTrue(rk.StartedBattle, "排位窗的 `Battle!` 走**同一条**开战链（等满 12 秒 ⇒ 开战）");
                }
                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                //   期望矩形 = **原版 prefab** `RankedEventWindowV2 > General Red Background >
                //   Reward Background Get Reward` 那颗 `Image` 的 rect（−960,93.57 → 2880,986.43）；
                //   ⛔ 不写 `RankedEventWindow.RedL…`（那是被测实现**传进去的实参**，同式自证）。
                //   ⚠️ 红底**之外**那条窄边仍是「点外面关窗」（(5,5) 就落在那条窄边上）。
                //   ⚠️ 本窗的吸收层**嵌在 `General Red Background` 底下**（不是窗根的直接子件 ——
                //      `Shell/RankedEventWindow.cs` 的 `BuildBackdrop` 传的是 `gen`）⇒ 节点名传**路径**。
                CheckAbsorbRule("排位窗", rk.transform, "General Red Background/AbsorbHit",
                                -960f, 93.57f, 2880f, 986.43f,
                                LiveOpsEventWindow.QBg, LiveOpsEventWindow.QHit, () => rk.CurrentState);
                // ⚠️ 上面那一组**结尾就把窗关掉了** ⇒ 开回来，下面那句才是**真**在断 `Close()`。
                CheckTrue(rk.TryOpen(null), "（A94 收尾）把排位窗开回来 —— 下面那句 `Close()` 才不是空断");
                rk.Close();
                Check(rk.CurrentState, WindowState.Closed, "关掉排位窗");
            }
        }

        // ============================================================ 四个排行榜 + 段位信息块
        //   （2026-09-27 建 · 多人界面那一批 第 3 件 + 第 5 件）
        // 判据：四棵树的层×参数 → `资料/普查产出_0927/排行榜_{遭遇战,经典,轮抽,嵌入版与行族}.md`；
        //       入口链 → `排行榜_入口与调用.md`（§1 `RankedEventWindowV2` 上两颗 prefab，按 playMode 二选一）；
        //       段位块 → `段位块_RankedDivisionInfo.md`（表 + 字段→节点 + 查不到的）。
        // 🔴 断的全是「原版参数」，不是我们自己的常量（§10·3 第 3 层）。
        Section("排行榜 ①：入口 —— 排位窗那颗 `LeaderboardButton` 按 `playMode` 二选一（**复刻，不是我们挑的**）");
        {
            var rc = menu.Find("Base Game Mode Container 1x1 - Ranked");
            var rh = rc != null ? FindChild(rc, "Hit") : null;
            var rwb = rh != null ? rh.GetComponent<WindowButton>() : null;
            CheckTrue(rwb != null, "排位卡还点得开（上一节刚把它关掉）");
            if (rwb != null) rwb.Click();
            // ⚠️ `LastOpened` 是 `LiveOpsEventWindow` 上的**静态**（排位与遭遇战共用一个字段）
            //    ⇒ 要断排位独有的 `LeaderboardKindForMode` 得转回 `RankedEventWindow`。
            var rk = RankedEventWindow.LastOpened as RankedEventWindow;
            CheckTrue(rk != null, "排位窗又开出来了");
            if (rk != null)
            {
                Check(rk.LeaderboardKindForMode, LeaderboardKind.Classic,
                      "本窗 `DeckGameMode` = 经典 ⇒ 那颗钮开的是**经典榜**"
                      + "（原版 `playMode == Classic(0)` ⇒ `rankingPrefabClassic`，否则 ⇒ `rankingPrefab`）");
                var lbh = FindChild(FindChild(rk.transform, "LeaderboardButton"), "Hit");
                var lbb = lbh != null ? lbh.GetComponent<WindowButton>() : null;
                CheckTrue(lbb != null, "`LeaderboardButton` 有点击区（**原来点了只 `NotBuilt` 打日志**）");
                if (lbb != null) lbb.Click();
                var lb0 = LeaderboardWindow.LastOpened;
                CheckTrue(lb0 != null && lb0.Kind == LeaderboardKind.Classic,
                          "点它 ⇒ 开 `RankedClassicLeaderboardPopup Variant`");
                CheckTrue(lb0 != null && lb0.CurrentState == WindowState.Open, "那一扇是**开着**的");
                Check(lb0 != null ? lb0.TabCount : -1, 2, "经典榜 **2 个页签**（没有 Alliances —— 三条独立证据）");
                if (lb0 != null)
                {
                    // 🆕 A47：压暗层命中区 —— 档 = `QPanel`(3500)（= 压暗层自己那一档），< `QHit`(3520)
                    //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
                    MenuDraw.CheckShadeRule(CheckTrue, "排行榜弹窗", FindChild(lb0.transform, "CloseHit"),
                                            lb0.transform.Find("Menu Dark Background"), LeaderboardWindow.QHit);
                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `RankedSkirmishLeaderboardPopup > Ranking Display >
                    //   Generic Window Red Background Big` 那颗 `Image` 的 rect（202.40,16.32 → 1717.60,1006.93）；
                    //   ⛔ 不写 `LeaderboardWindow.PanelR`（那是被测实现**传进去的实参**，同式自证）。
                    CheckAbsorbRule("排行榜弹窗", lb0.transform, "AbsorbHit",
                                    202.40f, 16.32f, 1717.60f, 1006.93f,
                                    LeaderboardWindow.QPanel, LeaderboardWindow.QHit, () => lb0.CurrentState);
                    lb0.Close();
                }
                rk.Close();
            }
        }

        Section("排行榜 ②：经典榜骨架（三扇全屏榜**共用那一套**，逐格照原版）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Classic);
            Check(lb.type, WindowType.Popup, "`type` = **1 Popup**（`RankedRankingWindow` 根上的字段）");
            Check(lb.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
            CheckTrue(lb.closeOnEsc, "`closeOnESC` = **1**");
            CheckNear(lb.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1.0**");
            var t = lb.transform;
            CheckAtWorld(FindChild(t, "Menu Dark Background"), -1327.30f, 3247.30f, -746.18f, 1826.18f,
                         "压暗层（**无图**、纯色 (0,0,0,0.773)）");
            var tabs = FindChild(t, "Tab Buttons");
            CheckAtWorld(tabs, 38.62f, 210.59f, 104.70f, 928.48f, "`Tab Buttons`（VLG spacing 0 · 首格顶 104.70）");
            CheckAtWorld(FindChild(tabs, "Player"), 45.59f, 210.59f, 104.70f, 262.38f,
                         "页签 1 `Player`（165×157.684）");
            CheckAtWorld(FindChild(tabs, "Armies"), 45.59f, 210.59f, 262.38f, 420.07f,
                         "页签 2 `Armies`（**紧挨着叠下去** —— 不是我们排的，是 VLG 算的）");
            {
                var pi = FindChild(FindChild(tabs, "Player"), "Icon");
                var pq = pi != null ? pi.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(pq != null && pq.Texture != null && pq.Texture.name == "40K_Chat_icon_Global",
                          "`Player` 页签图标 = `40K_Chat_icon_Global`（**本轮才导进来的那张**）");
                var ai = FindChild(FindChild(tabs, "Armies"), "Icon");
                var aq = ai != null ? ai.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(aq != null && aq.Texture != null && aq.Texture.name == "40K_Profile_icon_title",
                          "`Armies` 页签图标 = `40K_Profile_icon_title`");
            }
            CheckTrue(FindChild(tabs, "Label") == null,
                      "页签的 `Label` **不建**（原版出厂 inactive、且全子树零引用 ⇒ 死件；页签是**纯图标**的）");
            var panel = FindChild(t, "Ranking Display");
            CheckAtWorld(panel, 202.40f, 1717.60f, 16.32f, 1006.93f, "`Ranking Display`");
            CheckAtWorld(FindChild(panel, "Generic Window Red Background Big"), 202.40f, 1717.60f, 16.32f, 1006.93f,
                         "红底板（`UI_Deck_Information_Back` · 九宫 42,363,655,81）");
            CheckAtWorld(FindChild(panel, "Title"), 643.55f, 1276.45f, 36.95f, 146.52f, "`Title`");
            CheckText(TextOf(FindChild(panel, "Title")), "TOP PLAYERS", "四棵榜的标题**一律** `TOP PLAYERS`");
            CheckAtWorld(FindChild(panel, "TopBar"), 282.95f, 1637.05f, 141.63f, 147.63f,
                         "`TopBar`（6px 分隔线 —— 原版**没有列标题表头**，就是这一条）");
            var lbContent = FindChild(panel, "Content");
            CheckAtWorld(lbContent, 248.99f, 1671.01f, 147.64f, 937.83f, "`Content`");
            CheckAtWorld(FindChild(lbContent, "Army Selector"), 248.99f, 1671.01f, 147.64f, 258.59f,
                         "`Army Selector`（1422.02×110.95）");
            // 🆕 2026-10-03（§三 第 29 条 A3）：`Army Content` **真的填起来了**。
            //   判据 = **反编译方法体**（`ArmySelector__Initialize.c`：清空 → 遍历阵营 → `Instantiate(armyItemButton)`）
            //        + `python 工具/menu_dump.py bundle_menus_assets_all "Army Item Button" --depth 4`（逐节点几何）。
            //   🔴 三处是我们挑的（阵营清单 / 不隐藏任何阵营 / 不建角标）—— 见 `BuildArmySelector` 的注释。
            var armSel = FindChild(lbContent, "Army Selector");
            var armContent = FindChild(armSel, "Army Content");
            CheckTrue(armContent != null, "`Army Content` 建了");
            Check(LeaderboardWindow.ArmyTotal, 13, "军种项一共 **13** 颗（= `CampaignData.Armies` 那 13 个阵营）");
            CheckTrue(lb.ArmyButtonCount > 0 && lb.ArmyButtonCount <= 13,
                      $"这一轮真画出来 **{lb.ArmyButtonCount}** 颗（滚出 1422.02 视口的不建 —— 内容总宽 1604.68）");
            CheckTrue(lb.ArmyScroll != null && Mathf.Abs(lb.ArmyScroll.MaxOffset - (1604.68f - 1422.02f)) < 0.5f,
                      "军种条**横向可滚 182.66px**（= 内容 1604.68 − 视口 1422.02；原版 `ScrollRect h=1 v=0 mode=1`）");
            // 🔴 档位（2026-10-05 A28 尾巴）：原版 `Army Selector` 的 `m_MovementType = 1` ⇒ UGUI **Elastic**
            //   （真值 `Unrestricted 0 / Elastic 1 / Clamped 2`，本地 UGUI 源码亲读）。
            //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "<窗名>"` 逐扇实读：`RankedSkirmish` /
            //   `RankedClassic Variant` / `Draft` 三扇的 `Ranking Display/Content/Army Selector` **逐位相同**
            //   `h=1 v=0 mode=1 inertia=1 elasticity=0.1 decel=0.135`。
            //   （⚠️ 第四扇嵌入版 `Ranked Leaderboard Display` **根本没有 `Army Selector`** —— 上面 `CheckTrue(
            //   FindChild(t, "Army Selector") == null, …)` 断的就是这一条，别把这两条看矛盾。）
            //   🔴 **真红法**：删掉 `LeaderboardWindow.BuildArmySelector` 里那句 `_armyScroll.Elastic = true;`
            //   ⇒ 这一条立刻红（出厂 `false` = Clamped）。
            CheckTrue(lb.ArmyScroll != null && lb.ArmyScroll.Elastic,
                      "军种条的档位：**Elastic**（原版 `Army Selector` 的 `m_MovementType = 1`）");
            CheckTrue(lb.SelectedArmy == null, "起手**没有任何阵营被选中**（原版 `toggle.isOn` 只在 `army == selected` 时置 1）");
            var a0 = FindChild(armContent, CampaignData.Armies[0]);
            CheckTrue(a0 != null, "第 1 颗军种项在（**GO 名 = 阵营名** —— 原版 `Initialize` 里 `set_name(前缀 + army)`）");
            if (a0 != null)
            {
                // 按钮 136.36×121.59，中心 y = `Army Selector` 的中心 203.115；
                // 第 1 颗中心 x = 248.99 + 136.36/2（`LeftAligned`，见 `BuildArmySelector` 那条「我们挑的」）
                CheckAtWorld(a0, 248.99f, 385.35f, 142.32f, 263.91f,
                             "第 1 颗军种项（136.36×121.59 @ 中心 317.17,203.12 —— 比 110.95 的条**高**，上下被裁）");
                var ic0 = FindChild(a0, "Icon");
                var q0 = ic0 != null ? ic0.GetComponentInChildren<ImageQuad>() : null;
                string wantIcon = DeckRuntime.FactionIcon(CampaignData.Armies[0]);
                CheckTrue(q0 != null && q0.Texture != null && q0.Texture.name == wantIcon,
                          "`Icon` = `" + wantIcon + "`（该阵营的徽记 —— 原版无图、运行期 `ArmyIconsSO.GetArmyIcon`）");
                CheckTrue(FindChild(a0, "HighlightBG") == null,
                          "**没选中 ⇒ `HighlightBG` 不建**（判据 = `ArmyItemContainer__Initialize` 里 `SetActive(false)`）");
                var h0 = FindChild(a0, "Hit");
                var wb0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                CheckTrue(wb0 != null, "军种项有点击区");
                if (wb0 != null)
                {
                    wb0.ClickForTest();
                    Check(lb.SelectedArmy, CampaignData.Armies[0], "点它 ⇒ 选中");
                    var a0b = FindChild(armContent, CampaignData.Armies[0]);
                    CheckTrue(a0b != null && FindChild(a0b, "HighlightBG") != null,
                              "…**选中那一颗长出 `HighlightBG`**（原版 `Click(on)` 里 `SetActive(on)`）");
                    // ⚠️ `FindChild` 是**按名字精确匹配**的、不认识 `A/B/C` ⇒ 路径要走 `Transform.Find`
                    CheckTrue(a0b != null && a0b.Find("HighlightBG/Arrow") != null, "…`HighlightBG` 里那枚 `Arrow` 也在");
                    var h0b = a0b != null ? FindChild(a0b, "Hit") : null;
                    if (h0b != null) h0b.GetComponent<WindowButton>().ClickForTest();
                    CheckTrue(lb.SelectedArmy == null, "再点一次 ⇒ 取消选中（Toggle 语义）");
                }
            }
            // 🆕 2026-10-04（§三第29条 **A9 尾巴**）：军种条的软边（原版 `Army Selector/Viewport` 的 `m_Softness = (42,0)`）
            //   判据 = `_tmp_view/q1_rm2d.txt:14-15`（逐处实读；⚠️ **2026-10-05 更正（铁律 5）**：
            //   原来称它「156 个 `RectMask2D` 的全量 dump」是**标签错、值没错** —— 它**只扫了 3 个菜单族包**
            //   （表头 `150 + 1 + 5 = 156`）；**全库真值 = 222**。两条复现判据（`m_Script` 的 PathID
            //   `536591447201701790` ／ 必须限定 `MonoBehaviour/`）与逐包数字 → `MenuWindowBase.ClipSoftness` 的注释）；
            //   同族另两扇榜 **同一个值**（轮抽 `:40-41` · 遭遇 `:206-207`）；
            //   ⚠️ 同一扇窗里 `…/Content/Scroll View/Viewport` = **(0,0)** ⇒ **只接军种条这一处**。
            //   带的内沿 = 视口左右两条边 ± 42：**290.99**（= 248.99 + 42）与 **1629.01**（= 1671.01 − 42）
            //   （`menu_dump.py` 实读：`Army Selector` 与子件 `Viewport` **同矩形** 249.0,147.6→1671.0,258.6）。
            //   🔴 **真红法**：把 `LeaderboardWindow.RebuildArmyButtons` 里那三个 `Rect(...)` 的 `ArmyClipSoft`
            //     去掉（或改成 `(0,42)`）⇒ 第一条「一条切线都没有」/「必须落在带的内沿」立刻红。
            CheckSoftCuts(ScanSoftCuts(armContent, true, new PxRect(248.99f, 147.6f, 1671.01f, 258.6f)), new[] { 290.99f, 1629.01f }, 0.6f,
                          "排行榜军种条（原版 `m_Softness = (42,0)`）：第 1 颗的图标压着**左**带内沿（290.99）、"
                          + "第 12 颗（被视口右沿硬裁在 1671.01）压着**右**带内沿（1629.01）");
            Check(ScanSoftCuts(armContent, false, new PxRect(248.99f, 147.6f, 1671.01f, 258.6f)).Count, 0,
                  "…而**一条横切线都没有**（`(42,0)` 只渐变左右 —— 两轴写反成 `(0,42)` 的实现这里立刻冒横切线）");
            lb.ArmyScroll.ScrollBy(200f);           // 滚到底 ⇒ 最后一颗（第 13 个阵营）进视口
            CheckTrue(FindChild(armContent, CampaignData.Armies[12]) != null,
                      $"滚到最右 ⇒ 第 13 颗（{CampaignData.Armies[12]}）进视口（起手时它在 1717.31 之外）");
            lb.ArmyScroll.ScrollBy(-200f);
            CheckAtWorld(FindChild(lbContent, "Separator Line"), 248.99f, 1671.01f, 260.37f, 266.37f, "`Separator Line`");
            var sv = FindChild(lbContent, "Scroll View");
            CheckAtWorld(sv, 248.99f, 1671.01f, 288.59f, 937.83f, "`Scroll View`（649.24 高）");
            var vp = FindChild(sv, "Viewport");
            CheckAtWorld(vp, 248.99f, 1671.01f, 288.59f, 937.83f, "`Viewport`（原版是 `UIMask`(a=0)+`RectMask2D`）");
            CheckAtWorld(FindChild(vp, "Content"), 360f, 1560f, 288.59f, 288.59f,
                         "内层 `Content`（**1200 宽** —— 左右各留 111）");
            var cb = FindChild(t, "Generic Close Button Orange");
            CheckAtWorld(cb, 1656.81f, 1731.19f, 9.19f, 84.80f, "关窗钮（74.39×75.61）");
            CheckTrue(FindChild(cb, "Background") != null && FindChild(cb, "Icon") != null,
                      "它那两个可见子件（圆底 `40k_general_bt_yellow` + 叉）");
            CheckTrue(FindChild(t, "Timer") == null,
                      "**`Timer` 不建**（`Ends in: 23d 5h` = 服务器数据 + 用户明确不要赛季倒计时）");
            var seasonBtn = FindChild(t, "Generic Simplified UI Button_updated");
            CheckTrue(seasonBtn != null && !seasonBtn.gameObject.activeSelf,
                      "`Last season` 钮**建了但关着** —— 照原版 `AllowChangeSeason(有上一赛季榜)`"
                      + "（本地没有上一赛季榜 ⇒ 关）");
            var seasonTx = FindChild(t, "Last Season Text");
            CheckTrue(seasonTx != null && !seasonTx.gameObject.activeSelf,
                      "`Last Season Text` **建了但关着** —— 照原版 `Initialize` 传 `showCurrent=true`"
                      + " ⇒ `SetActive(!showCurrent)`");
            Check(lb.BuiltRows, 0, "本地 **0 条**榜单数据 ⇒ 一行都不建（**照原版留空**：四棵榜都没有空态节点）");
            Shoot("07_排行榜_经典.png");

            // ---- 喂一行 ⇒ 断**行族几何**（`PlayerRankingRow` 逐格）----
            // ⚠️ `RebuildForTest()` 会**整窗重画**（`Build()` 把子件全清掉）⇒ 上面取的 `tabs`/`vp` 全失效，
            //    下面一律**重新找**（拿旧引用会静默变 null）。
            LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player,
                new List<LeaderboardRowData>
                {
                    new LeaderboardRowData { Rank = 7, Name = "Test Commander", Guild = "Bando del Ventris",
                                             Points = "4500", Avatar = "Avatar_ASH_Howling Banshee", IsSelf = true },
                });
            lb.RebuildForTest();
            Check(lb.BuiltRows, 1, "喂一行 ⇒ **建出一行**（空数据那条路仍是原版那个样子）");
            var row = FindChild(FindChild(FindChild(FindChild(lb.transform, "Scroll View"), "Viewport"), "Content"),
                                "PlayerRankingRow");
            CheckAtWorld(row, 360f, 1560f, 288.59f, 388.59f, "行矩形（高 **100**，从视口顶边起）");
            CheckAtWorld(FindChild(row, "Ranking"), 372f, 472f, 302.63f, 374.54f, "行内 `Ranking`（名次）");
            CheckText(TextOf(FindChild(row, "Ranking")), "7", "名次文字");
            CheckAtWorld(FindChild(row, "border"), 495f, 615f, 289.57f, 396.01f,
                         "行内 `border`（头像格 —— **这一层才是 Button**，`target` = `Icon` 的 Image）");
            CheckAtWorld(FindChild(row, "Name Holder"), 650f, 1383.95f, 288.59f, 388.59f, "行内 `Name Holder`");
            CheckText(TextOf(FindChild(row, "Name")), "Test Commander", "行内 `Name`");
            // `Points` 是**左对齐**的（原版 `m_HorizontalAlignment = Left`）⇒ 不能用 `CheckAtWorld` 比节点中心
            //（`AlignLeftOn` 会把节点挪到「文字左缘 = 矩形左缘」那个位置）。**量它渲出来的左缘**才对。
            {
                float px1, py1, px2, py2;
                bool ok = RenderedRect(FindChild(row, "Points"), out px1, out py1, out px2, out py2);
                CheckTrue(ok && Mathf.Abs(px1 - 1420f) < 2f,
                          "行内 `Points`（右端）**左缘对齐 1420**（原版 hAlign = Left）—— 实测 " + px1.ToString("F1"));
                CheckTrue(ok && Mathf.Abs((py1 + py2) * 0.5f - 338.845f) < 2f,
                          "行内 `Points` 竖直居中于 299.86/377.83 那个框 —— 实测中心 " + ((py1 + py2) * 0.5f).ToString("F1"));
            }
            {
                // 🆕 2026-10-03：同上 —— 冒到视口之上/之下的那两截被真裁掉了（原版 `RectMask2D` 同）。
                // 🔴 实测这一件**上下都被裁**（原版块 y 257.45..423.12、实渲 288.59..394.44）
                // ⇒ 只剩**左右两条边**是原版值（实测 1301.70..1410.00 逐位吻合），另钉一条「真的被裁过」。
                float rx1, ry1, rx2, ry2;
                CheckTrue(RenderedRect(FindChild(row, "RankingIcon"), out rx1, out ry1, out rx2, out ry2)
                          && Mathf.Abs(rx1 - 1301.70f) < 1.5f && Mathf.Abs(rx2 - 1410f) < 1.5f,
                          "行内 `RankingIcon`：左右 1301.70..1410（原版值）—— 实测 "
                          + $"x {rx1:F2}..{rx2:F2} · y {ry1:F2}..{ry2:F2}");
                CheckTrue((ry2 - ry1) < 165.67f - 0.5f,
                          $"…上下都**真被视口裁过**（原版块高 165.67，实渲 {ry2 - ry1:F2}）");
            }
            {
                var bq = FindChild(row, "BackgroundHighlight");   // `IsSelf = true` ⇒ 走高亮那张
                var bgq = bq != null ? bq.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bgq != null && bgq.Texture != null && bgq.Texture.name == "Background",
                          "行底用的是 Unity **内置** `Background`（32×32 · 九宫 10,10,10,10）");

                // 🆕 2026-10-03（本件）：**行底九宫格被逐子块截到视口内** —— 这一行**天生就是压边态**：
                //   行 0 的顶 = 视口顶 288.59，而底块 `BgR` 局部 y = −3.22..103.22（**上下各溢出 3.22**）
                //   ⇒ 上面那 3.22px 必须被**截掉**（判据 = 原版 `RectMask2D` 只裁渲染、不挪 `RectTransform`）。
                // ⚠️ **量子块、别量根节点**：根节点位置按设计**一律不动**（量到根在视口外是**对的**，动了才是 bug）。
                // ⚠️ **必须用带 `true` 的重载** —— 整块在框外的子块走 `SetActive(false)`，默认重载看不到。
                // 写法照上面 `RankingIcon` 那两条（`RenderedRect` 量**渲出来**的矩形，不抄源码常量）。
                var blocks = bq != null ? bq.GetComponentsInChildren<ImageQuad>(true) : new ImageQuad[0];
                CheckTrue(blocks.Length > 0, $"行底九宫格建出来了（{blocks.Length} 块）");
                int nHidden = 0, nOutB = 0;
                float bTop = float.MaxValue, bBot = float.MinValue, bL = float.MaxValue, bR = float.MinValue;
                foreach (var bk in blocks)
                {
                    if (bk == null) continue;
                    // ⚠️ **先判 `activeSelf`、再量矩形** —— 反过来写的话，被 `SetActive(false)` 的那一块
                    //    会因为 `GetComponentInChildren`（不带 `true`）在未激活对象上拿不到自己而**被静默跳过**，
                    //    下面那条「一块都没被关掉」就等于没验。
                    if (!bk.gameObject.activeSelf) { nHidden++; continue; }   // 整块在框外 ⇒ 被关掉（节点还在）
                    float kx1, ky1, kx2, ky2;
                    if (!RenderedRect(bk.transform, out kx1, out ky1, out kx2, out ky2)) continue;
                    bTop = Mathf.Min(bTop, ky1); bBot = Mathf.Max(bBot, ky2);
                    bL = Mathf.Min(bL, kx1); bR = Mathf.Max(bR, kx2);
                    if (ky1 < 288.59f - 0.5f || ky2 > 937.83f + 0.5f
                        || kx1 < 248.99f - 0.5f || kx2 > 1671.01f + 0.5f) nOutB++;
                }
                CheckTrue(nOutB == 0,
                          $"每一块都落在视口 248.99..1671.01 × 288.59..937.83 内（越界 {nOutB} 块；"
                          + $"实测 x {bL:F2}..{bR:F2} · y {bTop:F2}..{bBot:F2}）");
                CheckTrue(nHidden == 0,
                          $"没有哪一块是「整块在框外、被 `SetActive(false)` 关掉」的（实得 {nHidden} 块 —— "
                          + "这一行只压了 3.22px，该走**截**那一档、不该走**关**那一档）");
                CheckNear(bTop, 288.59f, 0.5f,
                          "★ 最上面那一块**确实被截到视口顶 288.59**（没截的话它该在 285.37 = 行顶 − 3.22）"
                          + " —— 这就是「九宫格逐子块截」的判据");
            }
            {
                // 🔴 立绘**必须排在边框之后**：`Player_Profile_Border` 的中心是**不透明黑**
                //    （实测 RGBA=(0,0,0,255)）⇒ 同队列时谁盖谁由「到相机的距离」定，会把立绘压成黑块。
                var bImg = FindChild(FindChild(row, "border"), "Image");
                var iImg = FindChild(FindChild(row, "border"), "Icon");
                var bQuad = bImg != null ? bImg.GetComponentInChildren<ImageQuad>() : null;
                var iQuad = iImg != null ? iImg.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bQuad != null && iQuad != null && iQuad.RenderQueue > bQuad.RenderQueue,
                          "行内立绘的渲染队列**比边框高一档**（不然黑色边框心会把立绘盖掉）");
            }
            Check(lb.MissingArt.Count, 0, "建了行之后**还是一张图都不缺**（含内置 `Background`）");
            CheckHoverSwap(lb.transform, "排行榜窗");
            // 🆕 2026-10-03（§三 第 29 条 A3②）：**行被点 ⇒ 开那个玩家的档案窗**（原版 `profileButton`）。
            //   我们照做（开**同一扇** `PlayerProfileWindow`），但走 `CreateFor` 那一支 ——
            //   🔴 他的资料在服务器 ⇒ 六页换成 `StrangerProfilePage` 画如实说明，**不拿本地自己那一份冒充他**。
            {
                var hRow = FindChild(row, "Hit");
                var wbRow = hRow != null ? hRow.GetComponent<WindowButton>() : null;
                // ⚠️ 这一条同时是**命中区那道 `clip` 守卫的回归守**：行 0 完全在视口内 ⇒ 它的 `Hit` **必须还在**
                //    （`clip` 传反了 ⇒ 整颗不建 ⇒ 这条立刻红）。**压边的那一半**在下面「喂 7 行」那一段。
                CheckTrue(wbRow != null, "行有点击区（原版挂的是 `profileButton`）");
                if (wbRow != null)
                {
                    wbRow.ClickForTest();
                    var pw = PlayerProfileWindow.LastOpened;
                    CheckTrue(pw != null && pw.ViewedPlayer == "Test Commander",
                              "点行 ⇒ 开了**那一行那个玩家**的档案窗（`ViewedPlayer` = 行里的名字）");
                    if (pw != null)
                    {
                        var p0 = FindChild(pw.transform, "Profile Tab");
                        CheckTrue(p0 != null && p0.GetComponent<StrangerProfilePage>() != null,
                                  "六页换成了 `StrangerProfilePage`（原版这一页的数据全在服务器）");
                        CheckTrue(p0 != null && FindChild(p0, "Stranger Name") != null
                                  && TextOf(FindChild(p0, "Stranger Name")) == "「Test Commander」的档案",
                                  "页里画了那行**如实说明**（不是他的真资料 —— 本地没有）");
                        pw.Close();
                    }
                }
            }
            Shoot("07b_排行榜_经典_喂了一行.png");     // 空数据那张（`07`）看不出行族画得对不对
            LeaderboardData.ClearForTest();
            lb.RebuildForTest();
            Check(lb.BuiltRows, 0, "清空数据 ⇒ 行又没了");

            // ============================================================ 🆕 2026-10-03（本件 ①）
            // **滚出视口的整行不建** + **这一格真的滚得动**。
            // 🔴 原来 `LeaderboardWindow.RebuildRows` **从不设** `_scroll.ContentX2`（用的是
            //    `MenuScroll.TopAligned(ScrollR, 0f)`）⇒ `ContentX1 == ContentX2 == Viewport.y1`
            //    ⇒ `ClampLo == ClampHi == 0` ⇒ **滚不动**。而本批刚加了「视口外不建」的跳过
            //    ⇒ 第 7 行起从「画到框外（至少看得见）」变成「**完全不存在**」⇒ 不修就是丢数据。
            // 判据（原版）：这一格是 `ScrollRect m_Horizontal=0 / m_Vertical=1 / m_MovementType=1`，
            //    内层 `Content` 挂 `ContentSizeFitter m_VerticalFit=1`、行挂在它下面
            //    （实据 → `资料/普查产出_0927/排行榜_嵌入版与行族.md:38,40,61`）
            //    ⇒ 内容比视口高时就该滚得动；滚出视口的那部分**不画**（原版 `RectMask2D` 裁掉）。
            // 🔴 期望值一律**现算**（`RowsInViewport`）：视口 288.59..937.83（上面 `CheckAtWorld` 刚按原版值钉过）、
            //    行距 = 原版 100 + 15。⛔ **不写死条数**（写死 = 拿我们的常量断言我们的常量）。
            {
                const int N = 12;
                var many = new List<LeaderboardRowData>();
                for (int i = 1; i <= N; i++)
                    many.Add(new LeaderboardRowData { Rank = i, Name = "Runner " + i, Points = (1000 - i).ToString(),
                                                      IsSelf = i == 1 });
                LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player, many);
                lb.RebuildForTest();

                const float VpTop = 288.59f, VpBot = 937.83f;   // 原版值（= 上面那句 `CheckAtWorld(vp, …)` 钉过的两个数）
                float pitch = OrigLbRowH + OrigLbRowGap;   // 原版：行高 100 + 行距 15
                var vpN2 = FindChild(FindChild(lb.transform, "Scroll View"), "Viewport");
                CheckNear(vpN2 != null ? LayoutSpace.PxY(vpN2.position.y) : -9999f, (VpTop + VpBot) * 0.5f, 0.5f,
                          "`Viewport` 的**实测**中心 = 288.59..937.83 的中心（下面那些期望值就按这个矩形现算）");
                var ctn = FindChild(vpN2, "Content");

                int wantTop = RowsInViewport(N, VpTop, VpBot, pitch, OrigLbRowH, 0f);
                Check(lb.BuiltRows, wantTop,
                      $"★ {N} 行里**恰好建了与视口相交的那几行**（现算 {wantTop} 行；视口高 {VpBot - VpTop:F2}、"
                      + $"行距 {pitch:F2}）—— 整行在视口外的**连节点一起不建**（省 quad，顺带它的点击区也不存在）");
                CheckTrue(wantTop > 0 && wantTop < N,
                          $"…而且**确实有行被丢掉**（{N - wantTop} 行落在视口外）—— 否则这一条等于没验");

                // 断 C：内层 `Content` 下**每一颗**行的矩形都要与视口相交；节点个数 == `BuiltRows`
                CheckTrue(ctn != null, "内层 `Content` 在");
                if (ctn != null)
                {
                    int nRows = 0, nOut = 0; string worst = "";
                    float minTop = float.MaxValue;
                    foreach (var rt in ctn.GetComponentsInChildren<Transform>(true))
                    {
                        if (rt.name != "PlayerRankingRow") continue;
                        nRows++;
                        float cy = LayoutSpace.PxY(rt.position.y);
                        float y1 = cy - OrigLbRowH * 0.5f, y2 = cy + OrigLbRowH * 0.5f;
                        minTop = Mathf.Min(minTop, y1);
                        if (y2 <= VpTop + 0.01f || y1 >= VpBot - 0.01f) { nOut++; worst = $"y {y1:F2}..{y2:F2}"; }
                    }
                    Check(nRows, lb.BuiltRows, "建出来的行节点个数 == `BuiltRows`（两者不许各说各的）");
                    CheckTrue(nOut == 0,
                              $"每一颗建出来的行都与视口相交（越界 {nOut} 颗"
                              + (worst.Length > 0 ? "：" + worst : "") + "）");
                    CheckNear(minTop, VpTop, 0.5f,
                              "最上面那颗行的顶边 = 视口顶 288.59（顺带证明滚动偏移是 0 —— 上面的期望值是在这个前提下算的）");
                }

                // ---- 滚到最下 ⇒ **建的是另一批行**（本件 ① 的验收）----
                var rs = lb.RowsScroll;
                CheckTrue(rs != null, "榜单那一格有滚动区（`MenuScroll` —— 原版 = `ScrollRect h=0 v=1`）");
                if (rs != null)
                {
                    CheckTrue(rs.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                    // 🔴 档位（2026-10-05 A28 尾巴）：原版 `Scroll View` 的 `m_MovementType = 1` ⇒ UGUI **Elastic**。
                    //   判据 = 三扇弹窗逐扇实读（`menu_dump.py … "<窗名>"`）：`Ranking Display/Content/Scroll View`
                    //   = `h=0 v=1 mode=1`。🔴 **真红法**：删掉 `LeaderboardWindow.BuildPopup` 里那句
                    //   `_scroll.Elastic = true;`（或嵌入版那句）⇒ 这一条立刻红。
                    CheckTrue(rs.Elastic,
                              "档位：**Elastic**（原版 `Scroll View` 的 `m_MovementType = 1` —— ⛔ 不是 `BattleLogPopup` 那一档）");
                    // 🔴 **2026-10-04（A35②）**：这里原来算的是 `N * LeaderboardRow.RowH + …` ——
                    //   **拿实现常量当期望值**（同式自证：把 `LeaderboardRow.RowGap` 改坏也不会红）。
                    //   现在改用 `OrigLbRowH / OrigLbRowGap`（原版字面量 + 出处，见文件里那两个常量的注释）。
                    float contentH = N * OrigLbRowH + (N - 1) * OrigLbRowGap;
                    // 🔴 这一条就是本件那个 bug 的判据：修之前 `ContentX2` 从没设过 ⇒ `ClampLo == ClampHi == 0`
                    CheckNear(rs.MaxOffset, contentH - (VpBot - VpTop), 0.5f,
                              $"可滚范围 = 内容高（{N}×100 + {N - 1}×15 = {contentH:F0}）− 视口高 {VpBot - VpTop:F2}"
                              + " —— **修之前这里恒 0**（滚轮/拖拽全被夹回 0）");
                    CheckTrue(rs.MaxOffset > 1f, "★ 确实**滚得动**了");
                    rs.SetOffset(rs.MaxOffset);
                    CheckNear(rs.Offset, rs.MaxOffset, 0.01f, "滚到了最下（`SetOffset` 没被夹回去）");
                    int wantBot = RowsInViewport(N, VpTop, VpBot, pitch, OrigLbRowH, rs.Offset);
                    Check(lb.BuiltRows, wantBot,
                          $"滚到最下 ⇒ 仍然**恰好建了与视口相交的那几行**（现算 {wantBot} 行，偏移 {rs.Offset:F2}）");
                    CheckTrue(lb.BuiltRows > 0 && lb.BuiltRows < N, "…而且仍然有行落在视口外");
                    // 「换了一批」怎么证：按**建出来的行的名次文字**看（第 1 名该滚出去、第 N 名该进来）
                    var ranks = new List<string>();
                    foreach (var rt in ctn.GetComponentsInChildren<Transform>(true))
                    {
                        if (rt.name != "PlayerRankingRow") continue;
                        var rk = FindChild(rt, "Ranking");
                        var rl = rk != null ? rk.GetComponentInChildren<Label>() : null;
                        if (rl != null) ranks.Add(rl.Text);
                    }
                    CheckTrue(ranks.Count == lb.BuiltRows, "滚完之后行节点个数**仍然** == `BuiltRows`");
                    CheckTrue(!ranks.Contains("1") && ranks.Contains(N.ToString()),
                              $"★ 滚到最下建的是**另一批**行（第 1 名已滚出视口、第 {N} 名进来了；建出来的名次："
                              + string.Join("/", ranks.ToArray()) + "）");
                    rs.SetOffset(0f);
                }

                // ---- 压边那一行的**命中区**（`MenuDraw.Hit` 吃 `clip`）：喂 7 行 ⇒ 最后建出来的那颗压在视口下沿上 ----
                const int N7 = 7;
                var seven = new List<LeaderboardRowData>();
                for (int i = 1; i <= N7; i++)
                    seven.Add(new LeaderboardRowData { Rank = i, Name = "Edge " + i, Points = "1", IsSelf = i == 1 });
                LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player, seven);
                lb.RebuildForTest();
                int want7 = RowsInViewport(N7, VpTop, VpBot, pitch, OrigLbRowH, 0f);
                Check(lb.BuiltRows, want7, $"喂 {N7} 行 ⇒ 建了与视口相交的那 {want7} 行（现算）");
                CheckTrue(want7 > 0 && want7 < N7, $"…而且确实有整行落在视口外（{N7 - want7} 行不建）");
                {
                    var c7 = FindChild(FindChild(lb.transform, "Scroll View"), "Viewport");
                    c7 = FindChild(c7, "Content");
                    CheckTrue(c7 != null, "内层 `Content` 在（下面几颗逐行找）");
                    Transform edge = null, top1 = null;
                    float edgeY = float.MinValue, topY = float.MaxValue;
                    if (c7 != null)
                        foreach (var rt in c7.GetComponentsInChildren<Transform>(true))
                        {
                            if (rt.name != "PlayerRankingRow") continue;
                            float cy = LayoutSpace.PxY(rt.position.y);
                            if (cy > edgeY) { edgeY = cy; edge = rt; }
                            if (cy < topY) { topY = cy; top1 = rt; }
                        }
                    CheckTrue(edge != null && top1 != null && edge != top1,
                              $"建出来的最后一行（压在视口下沿）与第一行都找到了（共 {lb.BuiltRows} 行）");
                    float edgeRowBot = edgeY + OrigLbRowH * 0.5f;
                    CheckTrue(edgeRowBot > VpBot + 0.5f,
                              $"最后那颗行**压在视口下沿上**（行底 {edgeRowBot:F2} > 视口底 {VpBot:F2}）—— 「压边态」的前提");
                    float ex1, ey1, ex2, ey2, tx1, ty1, tx2, ty2;
                    var hitE = FindChild(FindChild(edge, "border"), "Hit");
                    var hitT = FindChild(FindChild(top1, "border"), "Hit");
                    // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来用的是 `RenderedRect` —— 它拿**节点**的
                    //    位置当矩形中心，而 `MenuDraw.Hit` 的节点**摆在父原点**（`border` 的中心没动）、
                    //    quad 才摆在矩形中心 ⇒ **一被裁就量歪**（报出来的是「整块中心 + 截后高」）。
                    //    ⇒ 这一处必须量**那颗 `ImageQuad` 自己**（`HitQuadRect`；同「量子块、别量根节点」那条）。
                    CheckTrue(HitQuadRect(hitE, out ex1, out ey1, out ex2, out ey2),
                              "压边那一行（最后建出来的那颗）的 `border/Hit` 建出来了");
                    CheckTrue(HitQuadRect(hitT, out tx1, out ty1, out tx2, out ty2),
                              "第一行的 `border/Hit` 建出来了（当「没被截」的参照 —— 它完全在视口内）");
                    CheckTrue(ey2 <= VpBot + 0.5f,
                              $"★ 压边行的 `border/Hit` **底边被截到视口内**（实得 {ey2:F2} ≤ {VpBot:F2} + 0.5）—— "
                              + "判据 = 原版 `RectMask2D` 的射线那一面（框外的点判不中任何东西）");
                    CheckTrue((ey2 - ey1) < (ty2 - ty1) - 0.5f,
                              $"…而且**真被截短过**（整行那颗高 {ty2 - ty1:F2} vs 压边这颗 {ey2 - ey1:F2}）"
                              + "—— 否则这一条等于没验");
                }

                LeaderboardData.ClearForTest();
                lb.RebuildForTest();
                Check(lb.BuiltRows, 0, "清空 + 重画 ⇒ 又回到空态（自检不留假数据，滚动偏移也回到 0）");
            }

            // ---- 切页签（`Armies` 那一格用 `PlayerRankingRow For Army`）----
            var armiesHit = FindChild(FindChild(FindChild(lb.transform, "Tab Buttons"), "Armies"), "Hit");
            var armiesBtn = armiesHit != null ? armiesHit.GetComponent<WindowButton>() : null;
            CheckTrue(armiesBtn != null, "`Armies` 页签有点击区");
            if (armiesBtn != null) armiesBtn.Click();
            Check(lb.CurrentTab, LeaderboardTab.Armies, "点 `Armies` ⇒ 切到那一格");
            CheckTrue(FindChild(FindChild(lb.transform, "Tab Buttons"), "Player") != null
                      && FindChild(FindChild(lb.transform, "Tab Buttons"), "Armies") != null,
                      "切页之后**两个页签都还在**（重画的，不是拆掉一个）");
            lb.Close();
            Check(lb.CurrentState, WindowState.Closed, "关掉经典榜");
        }

        Section("排行榜 ③：遭遇战榜 —— **3 个页签** + 联盟行族（`AllianceRankingRow Variant`）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Skirmish);
            Check(lb.TabCount, 3, "遭遇战榜 **3 个页签**（Player / Armies / Alliances）");
            var tabs = FindChild(lb.transform, "Tab Buttons");
            CheckAtWorld(FindChild(tabs, "Alliances"), 45.59f, 210.59f, 420.07f, 577.75f, "页签 3 `Alliances`");
            {
                var ai = FindChild(FindChild(tabs, "Alliances"), "Icon");
                var aq = ai != null ? ai.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(aq != null && aq.Texture != null && aq.Texture.name == "40K_Chat_icon_Alliance_v2",
                          "`Alliances` 页签图标 = `40K_Chat_icon_Alliance_v2`（本轮新导）");
            }
            LeaderboardData.InjectForTest(LeaderboardKind.Skirmish, LeaderboardTab.Alliances,
                new List<LeaderboardRowData>
                {
                    new LeaderboardRowData { Rank = 1, Guild = "[WF] Warpforge", Points = "12345" },
                });
            lb.SelectTab(LeaderboardTab.Alliances);
            Check(lb.CurrentTab, LeaderboardTab.Alliances, "切到 `Alliances`");
            var row = FindChild(FindChild(FindChild(FindChild(lb.transform, "Scroll View"), "Viewport"), "Content"),
                                "AllianceRankingRow");
            CheckTrue(row != null, "联盟行建出来了（**节点名照原版 `AllianceRankingRow`**）");
            CheckTrue(row != null && FindChild(row, "BadgeDrawer") != null,
                      "它那一格是 **`BadgeDrawer`**（不是 `border`）");
            CheckTrue(row != null && FindChild(row, "border") == null, "联盟行**没有头像格**（原版就没有）");
            CheckText(row != null ? TextOf(FindChild(row, "Guild Name")) : null, "[WF] Warpforge",
                      "联盟行的名字走 `Guild Name`（玩家行走 `Name`）");
            LeaderboardData.ClearForTest();
            lb.Close();
        }

        Section("排行榜 ④：轮抽榜 —— 默认 `Alliances`，`Armies` 那格**原版就没接线**（点了如实出声）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Draft);
            Check(lb.TabCount, 2, "轮抽榜 **2 个页签**（树上也是 2 个）");
            Check(lb.CurrentTab, LeaderboardTab.Alliances,
                      "**默认落在 `Alliances`** —— 原版 `tabDefinitions` 只登记了它（`Open()` 开 `tabDefinitions[0]`）");
            var tabs = FindChild(lb.transform, "Tab Buttons");
            var hit = FindChild(FindChild(tabs, "Armies"), "Hit");
            var btn = hit != null ? hit.GetComponent<WindowButton>() : null;
            CheckTrue(btn != null, "`Armies` 页签**建出来了**（照树）");
            if (btn != null) btn.Click();
            Check(lb.CurrentTab, LeaderboardTab.Alliances,
                  "点了 `Armies` ⇒ **不切换**（原版 `tabDefinitions` 没登记它 ⇒ 那个钮点不动；我们如实出声）");
            LeaderboardData.InjectForTest(LeaderboardKind.Draft, LeaderboardTab.Alliances,
                new List<LeaderboardRowData> { new LeaderboardRowData { Rank = 2, Guild = "[WF] X", Points = "900" } });
            lb.RebuildForTest();
            var row = FindChild(FindChild(FindChild(FindChild(lb.transform, "Scroll View"), "Viewport"), "Content"),
                                "AllianceRankingRow");
            var ri = row != null ? FindChild(row, "RankingIcon") : null;
            var rq = ri != null ? ri.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(rq != null && rq.Texture != null && rq.Texture.name == "40k_battle_Win_Skull",
                      "轮抽的联盟行 `RankingIcon` = **`40k_battle_Win Skull`**（与遭遇战那族的唯一差别）");
            LeaderboardData.ClearForTest();
            lb.Close();
        }

        Section("排行榜 ⑤：嵌入版 `Ranked Leaderboard Display`（🔴 **原版全库零引用** —— 没有入口，我们也不编）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Embedded);
            var t = lb.transform;
            CheckTrue(FindChild(t, "Menu Dark Background") == null, "**没有压暗层**（原版这一棵就没有）");
            CheckTrue(FindChild(t, "Tab Buttons") == null, "**没有页签**");
            CheckTrue(FindChild(t, "Generic Close Button Orange") == null, "**没有关闭键**");
            CheckTrue(FindChild(t, "Army Selector") == null, "**没有 `Army Selector`**（`subMenu` 的 PPtr = 0）");
            CheckAtWorld(FindChild(t, "Generic Window Red Background Big"), 208.72f, 1727.48f, 47.60f, 1054.86f,
                         "红底板（比三扇全屏榜那块大一圈 1518.76×1007.26）");
            CheckAtWorld(FindChild(t, "Title"), 643.55f, 1276.45f, 73.06f, 174.90f, "`Title`（同一句 `TOP PLAYERS`）");
            CheckAtWorld(FindChild(t, "TopBar"), 282.95f, 1637.05f, 170.00f, 176.00f, "`TopBar`");
            CheckAtWorld(FindChild(FindChild(t, "Content"), "Scroll View"), 248.99f, 1671.01f, 176.01f, 1006.93f,
                         "列表区（⚠️ **高是推算的**：原版这一棵的 `Scroll View` 序列化高就是 0、没有 `LayoutElement`）");
            Check(lb.BuiltRows, 0, "没有数据 ⇒ 一行都不建");
            // 🆕 2026-10-03（本件 ①）：**同一份 `RebuildRows` 换一个视口也得成立** —— 嵌入版的视口高
            //   是 **830.92**、三扇弹窗是 649.24 ⇒ 这里现算一遍，防「只对某一种情况调过」（铁律 5·c）。
            {
                const int NE = 12;
                var manyE = new List<LeaderboardRowData>();
                for (int i = 1; i <= NE; i++)
                    manyE.Add(new LeaderboardRowData { Rank = i, Name = "Emb " + i, Points = "1", IsSelf = i == 1 });
                LeaderboardData.InjectForTest(LeaderboardKind.Embedded, LeaderboardTab.Player, manyE);
                lb.RebuildForTest();
                int wantE = RowsInViewport(NE, 176.01f, 1006.93f, OrigLbRowH + OrigLbRowGap,
                                           OrigLbRowH, 0f);
                Check(lb.BuiltRows, wantE,
                      $"嵌入版：{NE} 行里建了与视口（176.01..1006.93）相交的那 {wantE} 行（现算，⛔ 不写死）");
                CheckTrue(wantE > 0 && wantE < NE,
                          "…而且**确实有行在视口外**（嵌入版视口高 830.92 ≠ 三扇弹窗那 649.24）");
                var rsE = lb.RowsScroll;
                CheckTrue(rsE != null && rsE.MaxOffset > 1f,
                          "嵌入版这一格**也滚得动**（可滚范围 "
                          + (rsE != null ? rsE.MaxOffset.ToString("F2") : "—") + "px；修之前这一格同样恒 0）");
                // 🔴 档位（2026-10-05 A28 尾巴）：嵌入版这一格**同为 Elastic** —— 原版
                //   `Ranked Leaderboard Display/Content/Scroll View` 实读 `h=0 v=1 mode=1`（与三扇弹窗相同的档）。
                //   🔴 **真红法**：删掉 `LeaderboardWindow.BuildEmbedded` 里那句 `_scroll.Elastic = true;` ⇒ 这条红。
                CheckTrue(rsE != null && rsE.Elastic,
                          "嵌入版那一格的档位：**Elastic**（原版这一棵的 `Scroll View` 同为 `m_MovementType = 1`）");
                LeaderboardData.ClearForTest();
                lb.RebuildForTest();
                Check(lb.BuiltRows, 0, "清完 ⇒ 回到空态（不留假数据）");
            }
            lb.Close();
        }

        Section("段位信息块 `Ranked Division Info/Content`（**实例 ③** —— `RankedEventWindowV2` 下那个）");
        {
            var rc = menu.Find("Base Game Mode Container 1x1 - Ranked");
            var rh = rc != null ? FindChild(rc, "Hit") : null;
            var rwb = rh != null ? rh.GetComponent<WindowButton>() : null;
            if (rwb != null) rwb.Click();
            var rk = RankedEventWindow.LastOpened;
            CheckTrue(rk != null, "排位窗开出来了");
            if (rk != null)
            {
                var col = FindChild(rk.transform, "Ranked Division Info");
                CheckAtWorld(col, 0f, 638f, 146.93f, 959.07f, "`Ranked Division Info`（638×812.13）");
                var dvContent = FindChild(col, "Content");
                CheckAtWorld(dvContent, 65.05f, 572.95f, 222.75f, 886.57f, "`Content`（整棵的根）");
                CheckAtWorld(FindChild(dvContent, "RankTitleBG"), 90.40f, 547.60f, 222.75f, 296.45f,
                             "`RankTitleBG`（段位名那一条）");
                CheckAtWorld(FindChild(dvContent, "DivisionText"), 141.30f, 496.70f, 225.61f, 293.59f, "`DivisionText`");
                CheckText(TextOf(FindChild(dvContent, "DivisionText")), "",
                          "🔴 段位名**留空**（`Division V` 是服务器数据 —— 用户口径：不编数字）");
                // 🆕 2026-10-03：原版这行 TMP `charSpacing = -2.6` —— **留空也把值设上**（将来填字就对了）
                CheckNear(CharSpacingOf(FindChild(dvContent, "DivisionText")), -2.6f, 0.01f,
                          "`DivisionText` 的 `characterSpacing` = **−2.6**（原版 TMP 原文）");
                var di = FindChild(dvContent, "DivisionImage");
                CheckAtWorld(di, 49.10f, 588.90f, 234.04f, 806.66f, "`DivisionImage`");
                CheckTrue(di != null && di.GetComponentInChildren<ImageQuad>() == null,
                          "🔴 段位大图**不画**（`RankedDivisionsSO` 本地没有 ⇒ 画任何一张都是**编一个段位**）");
                var footer = FindChild(dvContent, "footer");
                CheckAtWorld(FindChild(footer, "MainRating"), 114f, 524f, 717.95f, 785.75f,
                             "`footer/MainRating`（⚠️ 原版工具把它算成 0 高 —— 嵌套布局组不递归；按 `MainRating` 取）");
                var ms = FindChild(footer, "Mission Milestones Progress");
                CheckAtWorld(FindChild(ms, "Background"), 63.38f, 574.62f, 715f, 788.70f,
                             "`Mission Milestones Progress/Background`（**比父宽** 511.24 vs 410 —— 原版就那样，别「对齐」掉）");
                CheckTrue(FindChild(ms, "counter") == null,
                          "`counter` **不建**（里程碑计数：出厂 inactive + 全 bundle 零引用者）");
                var steps = FindChild(ms, "steps");
                CheckAtWorld(FindChild(steps, "RankedSealStep"), 221.55f, 321.55f, 708.59f, 795.10f,
                             "阶梯 1 `RankedSealStep`（100×86.51）");
                CheckAtWorld(FindChild(steps, "RankedSealStep (1)"), 316.45f, 416.45f, 708.59f, 795.10f, "阶梯 2");
                {
                    var empty = FindChild(FindChild(steps, "RankedSealStep"), "Empty");
                    var eq = empty != null ? empty.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(eq != null && eq.Texture != null && eq.Texture.name == "Rank_Skull_Empty",
                              "阶梯的 `Empty` 态 = `Rank_Skull_Empty`（**本轮才导进来的那张**）");
                    var fill = FindChild(FindChild(steps, "RankedSealStep"), "Fill");
                    CheckTrue(fill != null && fill.GetComponentInChildren<ImageQuad>() == null,
                              "🔴 `Fill`（已达成那一档）**不画** —— 进度是服务器数据（保留 `Empty` 态）");
                }
                CheckTrue(FindChild(dvContent, "Timer") == null, "**`Timer` 不建**（同上：赛季倒计时不做）");
                CheckTrue(FindChild(FindChild(dvContent, "Legendary Ratings"), "Position") != null
                          && FindChild(FindChild(dvContent, "Legendary Ratings"), "Global Rating") != null,
                          "`Legendary Ratings` 下那两支**建了结构**（原版预制体里就是 inactive ⇒ 里面不画东西）");
                Check(rk.MissingArt.Count, 0, "排位窗（含段位块）**图一张都不缺**");
                CheckHoverSwap(rk.transform, "排位窗（含段位块）");
                Shoot("08_排位窗_段位块.png");
                rk.Close();
            }
        }


        // ============================================================ 社交 / 聊天 / 好友挑战
        //   （2026-09-27 建 · 多人界面那一批 第 4 件）
        // 判据：社交 → `资料/普查产出_0927/社交_联盟与好友页.md`（§A·1 层×参数 · §A·2 行族独立根 · §B 判定）；
        //       聊天窗 + 挑战弹窗 → `资料/普查产出_0927/聊天窗与挑战弹窗.md`（§A / §B）；
        //       三条**入口链** → `资料/普查产出_0927/多人界面_入口与调用.md`（§① SOCIAL 键 · §② ChatPreview · §③）。
        // 🔴 **断的全是「原版参数」**，不是我们自己的常量（§10·3 第 3 层）。
        Section("社交窗 `Social Submenu Variant`（入口 = 左竖导航第 5 键 —— **这一条是复刻，不是我们挑的**）");
        {
            var socialBtn = menu.Find("Main Menu Navigation Button - Social");
            var sh = socialBtn != null ? FindChild(socialBtn, "Hit") : null;
            var swb = sh != null ? sh.GetComponent<WindowButton>() : null;
            CheckTrue(swb != null && swb.onClick != null, "SOCIAL 键**接了点击**（原来点了只打一句日志）");
            if (swb != null && swb.onClick != null)
            {
                swb.onClick();
                var sw = SocialWindow.LastOpened;
                CheckTrue(sw != null && sw.CurrentState == WindowState.Open, "点 SOCIAL ⇒ **真的开了社交窗**");
                if (sw != null)
                {
                    Check(sw.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（原文）");
                    Check(sw.placement, WindowsPlacement.Canvas, "`windowsPlacement` = **5 Canvas**（原文）");
                    CheckTrue(sw.closeOnEsc, "`closeOnESC` = **1**（原文）");
                    CheckNear(sw.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1.0**（原文）");

                    var t = sw.transform;
                    // 壳：与奖励窗/商店**同一份**（`Content Area` / `Tab Buttons` / `Tabs` 实测同值）
                    CheckAtWorld(FindChild(t, "Content Area"), 167.17f, 1920.00f, 70.94f, 1080f,
                                 "`Content Area`（**与奖励窗逐值相同** ⇒ 直接复用那套壳）");
                    var bar2 = FindChild(FindChild(t, "Content Area"), "Tab Buttons");
                    CheckAtWorld(bar2, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`（165 宽）");
                    // 🔴 **原版这条 `Shadow` 是 0 高**（`sz=(165,0)`）⇒ 我们**不建** ——
                    //    照奖励窗那个 47.64 画会**凭空多一条线**（这就是「一个值 ≠ 全部情况」那条铁律的活例子）。
                    CheckTrue(FindChild(bar2, "Shadow") == null,
                              "`Tab Buttons/Shadow` **不建**（原版 `sizeDelta=(165,0)` ⇒ 静态看不见）");
                    CheckAtWorld(FindChild(FindChild(t, "Content Area"), "Tabs"), 167.17f, 1920.00f, 70.94f, 1080f, "`Tabs`");

                    // 左栏两键：`Alliances` / `Friends`（**位置是 VLG 算出来的**，165×180，顶 190.94 / 370.94）
                    for (int i = 0; i < SocialWindow.Buttons.Length; i++)
                    {
                        float top = 70.94f + 120f + 180f * i, bot = top + 180f;
                        var br = FindChild(bar2, "SocialTabButton_" + i);
                        CheckAtWorld(br, 167.17f, 332.17f, top, bot, $"左栏第 {i + 1} 键（165×180）");
                        CheckText(TextOf(FindChild(br, "Text")), SocialWindow.Buttons[i].Label.ToUpperInvariant(),
                                  $"左栏第 {i + 1} 键文案 = `{SocialWindow.Buttons[i].Label}`（原版 TMP 是 UpperCase 款）");
                        // 🆕 **2026-10-08（波 C3 · A212 主表 #31 验收）：左栏键文案的【渲染】断言**
                        //   （四窗这一族原来一条都没有；本窗 = 第 4 扇，宿主就是本文件）。
                        //   判据 = 原版四窗左栏键文案的 `m_TextWrappingMode` **一律 `0`（`NoWrap`）**；
                        //   逐窗现读的四窗表只写一处：`Shell/MenuWindowBase.cs` 的 `BuildTabButton`（铁律 6）。
                        //   框宽 **155** = 原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`
                        //   （⛔ 不读 `BuildTabButton` 的 `labW` —— 那是被测实现里的数，读了就是自证）。
                        //   **改坏法**：删掉 `BuildTabButton` 末句 `txt.SetWrapping(false)` ⇒ 上面那条 `CheckWrapMode` 红
                        //   （`SetAutoFitBox` → `SetWrapWidth` 会把模式开回 `Normal`）。
                        CheckWrapMode(FindChild(br, "Text"), 0,
                                      $"★ 左栏第 {i + 1} 键 `{SocialWindow.Buttons[i].Label}`：**`折行=0`**"
                                    + "（原版四窗左栏键文案一律 0）");
                        {
                            var clb = FindChild(br, "Text") != null ? FindChild(br, "Text").GetComponent<Label>() : null;
                            if (clb != null)
                            {
                                Check(clb.LineCount, 1, $"★ …而且渲出来**就一行**（`Normal` 会把装不下的键名折行）");
                                CheckTrue(clb.WorldW * 108f <= 155f + 0.5f,
                                          $"★ …而且**渲出来的宽 {clb.WorldW * 108f:F1} ≤ 框宽 155**"
                                        + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
                            }
                        }
                        var iq = FindChild(br, "Icon") != null
                               ? FindChild(br, "Icon").GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(iq != null && iq.Texture != null && iq.Texture.name == SocialWindow.Buttons[i].Art,
                                  $"第 {i + 1} 键图标 = `{SocialWindow.Buttons[i].Art}`"
                                  + (i == 1 ? "（⚠️ 名字里是**空格 + v2**，落盘成下划线版）" : ""));
                        // 🆕 **2026-10-11（A219②）**：这一格原来自己又写了一遍「建 `Hit` 节点 + 建那颗透明
                        //   quad + 挂 `WindowButton`」（`Shell/MenuWindowBase.BuildTabButton`），而公共件里早就有
                        //   `MenuDraw.Hit` —— **同一条规则两处各写一遍** ⇒ 本件把它收口成转调。
                        //   本条钉的是收口之后**必须仍然成立**的两件事（⛔ 一个实现常量都不读）：
                        //    ① **节点下真有一颗 `ImageQuad`** —— `PointerLayer.CollectHits` 取的是「按钮下
                        //       **第一个 `ImageQuad`**」（`Shell/PointerLayer.cs:816` 那条取件；⚠️ A219 判据文档
                        //       里写的 `:492` **行号已漂** —— 按内容认，别按行号）⇒ **裸节点进不了命中表**、
                        //       **真鼠标点不动**，而自检直调 `wb.Click()` 照样绿（A26 那个真缺陷就是这个）；
                        //    ② 命中区**落在键那一格的矩形里**（原版这一层就是按钮自己的 `RectTransform`）。
                        //   **改坏法**：把 `MenuDraw.Hit(b, "Hit", new PxRect(cx − BarW/2, cy − TabBtnH/2,
                        //   cx + BarW/2, cy + TabBtnH/2), …)` 那个矩形写错（`BarW`/`TabBtnH` 互换、或把
                        //   y 写成别的格）⇒ ② 红；只建节点、不建 quad（A26 那个症状）⇒ ① 红。
                        {
                            var hN = FindChild(br, "Hit");
                            var hQ = hN != null ? hN.GetComponentInChildren<ImageQuad>(true) : null;
                            CheckTrue(hQ != null && hQ.Texture != null,
                                      $"★ A219② 左栏第 {i + 1} 键的 `Hit` 下**真有一颗 `ImageQuad`**"
                                    + "（`PointerLayer` 只认它；裸节点 ⇒ 真鼠标点不动、而 `wb.Click()` 直调照样绿）");
                            CheckAtWorld(hQ != null ? hQ.transform : null, 167.17f, 332.17f, top, bot,
                                         "★ …而且命中区那颗 quad 就摆在**键那一格的中心**（165×180 = 原版按钮自己的 rect）");
                            float hw = hQ != null ? hQ.WorldW * 108f : 0f, hh = hQ != null ? hQ.WorldH * 108f : 0f;
                            CheckTrue(Mathf.Abs(hw - 165f) < 1f && Mathf.Abs(hh - 180f) < 1f,
                                      $"★ …尺寸也是那一格（实测 {hw:F1}×{hh:F1}；原版这一层 = 键自己 **165×180**）");
                            var hWb = hN != null ? hN.GetComponent<WindowButton>() : null;
                            CheckTrue(hWb != null && hWb.onClick != null,
                                      "★ …那颗命中区**接了点击**（整键：原版是 `EverguildToggle`，我们只用它的点击语义）");
                        }
                    }
                    Check(sw.tabButtons.CurrentType, WindowTabType.SocialAlliances,
                          "**默认落在 `Alliances`**（原版 `Alliances Tab` act T / `Friends Tab` act **F**）");
                    CheckTrue(FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Friends Tab") != null
                              && !FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Friends Tab").gameObject.activeSelf,
                              "`Friends Tab` 出厂**是关的**");

                    // ---- 未入盟支（`AllianceSearchTab`）----
                    var aRoot = FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Alliances Tab");
                    var nmv = FindChild(aRoot, "AllianceNotMemberVariant");
                    CheckAtWorld(nmv, 332.17f, 1919.50f, 70.90f, 1080.02f, "`AllianceNotMemberVariant`（默认这一支）");
                    var hdr = FindChild(nmv, "Alliance Header Buttons");
                    CheckAtWorld(hdr, 331.67f, 1920.00f, 89.87f, 162.04f, "`Alliance Header Buttons`");
                    CheckAtWorld(FindChild(hdr, "Generic Tab UI Button Search"), 360.47f, 620.47f, 90.30f, 157.94f,
                                 "`Join` 键（`40K_tab_button_overwindow` 489×97 · 九宫 188,0,99,30）");
                    CheckText(TextOf(FindChild(hdr, "Generic Tab UI Button Search")), "Join",
                              "第一个键的文案是 **`Join`**（⚠️ 节点名叫 `…Button Search`）");
                    CheckAtWorld(FindChild(hdr, "Generic Tab UI Button Create"), 632.92f, 892.92f, 90.30f, 157.94f,
                                 "`Create` 键");
                    CheckText(TextOf(FindChild(hdr, "Generic Tab UI Button Create")), "Create", "第二个键的文案 `Create`");
                    var lv = FindChild(nmv, "List View");
                    CheckAtWorld(lv, 360.99f, 1902.59f, 162.04f, 1080.02f, "`List View`（`JoinAllianceMenu`）");
                    CheckAtWorld(FindChild(lv, "Search Field"), 1402.00f, 1798.57f, 172.90f, 229.86f, "搜索框");
                    CheckAtWorld(FindChild(lv, "Generic Round Button Variant"), 1800.78f, 1860.78f, 171.38f, 231.38f,
                                 "搜索圆钮（`40k_general_bt_yellow`）");
                    CheckAtWorld(FindChild(lv, "List Area"), 361.00f, 1874.90f, 252.29f, 1079.77f, "`List Area`");

                    // 🆕 **2026-10-08（波 C3 · A213）**：`SocialWindow.Text` 原来**恒折行**（它转调的
                    //   `MenuDraw.TextBox` 第一句就是 `SetWrapWidth`，那个无条件把模式设成 `Normal`），
                    //   而原版**逐件不同** ⇒ 本批给那条路加了显式 `wrap`，`AlliancesTab` 那 4 处原版为 `0` 的
                    //   传了 `wrap: false`。判据 = `python 工具/menu_dump.py bundle_menus_assets_all
                    //   "Social Submenu Variant" --depth 16 --md` 的 `折行=` 列（逐行）。
                    //   ⚠️ 行尾那颗 `Join`/`Reject` 的 `Button Text` **断不了** —— 本地三张表恒空
                    //   （`SocialData`，见上面那条）⇒ **一行都不建**；它的真值（`折行=0`）与改法
                    //   记在报告里，等有数据那天连同断言一起补。
                    //   **怎么改坏就红**：把 `Shell/SocialWindow.cs` 里新加的那句
                    //   `if (lb != null && !wrap) lb.SetWrapping(false);` 删掉（或把那三处的 `wrap: false` 去掉）
                    //   ⇒ 下面 3 条各红一条（期望 0、实得 1）。
                    {
                        CheckWrapMode(FindChild(FindChild(hdr, "Generic Tab UI Button Search"), "Button Text"), 0,
                                      "★ `Join` 页签键的 `Button Text` **不折行**（原版 `折行=0`，`字号=60 auto[12~60]`）");
                        CheckWrapMode(FindChild(FindChild(hdr, "Generic Tab UI Button Create"), "Button Text"), 0,
                                      "★ `Create` 页签键的 `Button Text` **不折行**（同上）");
                        var phF = FindChild(lv, "Search Field");
                        CheckWrapMode(phF != null ? FindChild(phF, "Placeholder") : null, 0,
                                      "★ 搜索框占位 `Search` **不折行**（原版 `折行=0`，`字号=50 auto[18~50]`）");
                    }
                    // 数据全空 ⇒ 三个列表**一行都不建**（原版也没有空态节点，留白即可）
                    Check(SocialData.Invitations.Count + SocialData.OpenAlliances.Count, 0,
                          "本地邀请 / 公开联盟 **0 条**（原版读服务器）");
                    Check(FindChild(FindChild(FindChild(lv, "Invitations"), "List"), "Invitation List Entry"), null,
                          "⇒ `Invitations/List` 底下**一行都不建**");

                    // ---- 建盟表：出厂 act F，点 `Create` 键才亮（**这个切换是纯本地的，能用**）----
                    var cav = FindChild(nmv, "Create Alliance View");
                    CheckTrue(cav != null && !cav.gameObject.activeSelf, "`Create Alliance View` 出厂 **act F**");
                    CheckAtWorld(cav, 368.48f, 1882.38f, 165.12f, 1080.02f, "`Create Alliance View` 矩形");
                    var createBtn = FindChild(hdr, "CreateHit");
                    var cb2 = createBtn != null ? createBtn.GetComponent<WindowButton>() : null;
                    CheckTrue(cb2 != null && cb2.onClick != null, "`Create` 键接了点击");
                    if (cb2 != null)
                    {
                        cb2.onClick();
                        CheckTrue(cav.gameObject.activeSelf && !lv.gameObject.activeSelf,
                                  "点 `Create` ⇒ **建盟表亮、`List View` 藏**（原版 `ShowCreateAllianceMenu`）");
                        var jb = FindChild(hdr, "JoinHit");
                        if (jb != null && jb.GetComponent<WindowButton>() != null) jb.GetComponent<WindowButton>().onClick();
                        CheckTrue(lv.gameObject.activeSelf && !cav.gameObject.activeSelf, "点 `Join` ⇒ 切回来");
                    }
                    // ⚠️ **左对齐的文字不能断中心**：`MenuDraw.AlignLeft` 会把整块字挪到矩形左边缘
                    // （`Title` 页那条教训）⇒ 这里断的是**左边缘 x**。
                    var nt = FindChild(cav, "Name input title");
                    var ntl = nt != null ? nt.GetComponent<Label>() : null;
                    float nlLeft = (nt != null && ntl != null) ? (nt.position.x - ntl.WorldW * 0.5f) * 108f + 960f : -1f;
                    CheckTrue(Mathf.Abs(nlLeft - 432.47f) < 1f,
                              $"建盟表「联盟名」标题**左对齐到 x=432.47**（原版 `Left/Middle`；实得 {nlLeft:F2}）");
                    CheckAtWorld(FindChild(cav, "Name Input"), 432.47f, 1332.47f, 307.95f, 367.35f,
                                 "`Name Input`（打不了字 —— 出声，不静默）");

                    // 🆕 **2026-10-08（波 C3 · A214③）**：`AlliancesTab` 建盟页那颗价格的矩形
                    //   （`AlliancesTab.cs` 里 `"text"` 那行）**此前没有任何断言** —— A62 ⑫④ 把它按修好的
                    //   `menu_dump.py` 从 `542.06→609.12` 改成 `546.75→613.81`，而全仓 0 处引用 ⇒ 改回去也不会红。
                    //   期望值 = **原版 dump 的字面量**（⛔ 不从 `AlliancesTab.cs` 的常量读 —— 那是自证）：
                    //   `python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`
                    //   ⇒ `…/Price Display Button/…/Price Display/text` = **546.75,730.73 → 613.81,777.65**
                    //   （`字号=40 auto[13.46~40]` · `Center/Capline` · **折行=0**）。
                    //   🔴 那 **+4.69** 的机理：该格是 `HorizontalLayoutGroup` 的**第二个子件**，前一件 `icon`
                    //   自带 `m_LocalScale = 1.2` ⇒ 推进量按 `childSize × scaleFactor`、组内居中的起始偏移又按
                    //   乘过缩放的 requiredSpace 折半 ⇒ 净位移 `46.91 × (1.2 − 1) ÷ 2` = **+4.69**。
                    //   🔴 **2026-10-11（A317）订正 —— 这一格已经不是「左对齐」那一档了**（原来写的是
                    //   「`alignLeft` 缺省是 `true` ⇒ 节点被推到**左边缘**」）：W5（A255）把 `AlliancesTab.cs`
                    //   那一行改成 `alignLeft: false`（原版这一格是 `Center/Capline`）⇒ 节点**不再被推到左边缘**，
                    //   而是留在 `MenuDraw.Local` 摆的**矩形中心** ⇒ 旧量法（左边缘 = 546.75）**过期**：
                    //   左边缘现在 = 中心 − **渲染字宽/2**，而字宽是 `SetAutoFitBox` 现算的
                    //   ⇒ 它**碰巧绿**（字刚好占满框）与**直接红**都可能 —— 两种都不该留。
                    //   ⇒ 改成**量中心**：`CheckAtWorld` 拿原版**矩形字面量**比节点世界坐标
                    //   （与 A214③ 那条**兄弟常量** `Change Name Button/Price Display > text` 同一写法）。
                    //   ⚠️ **鉴别力没变**：这一格在 `HorizontalLayoutGroup` 里是**纯平移**（宽不变）
                    //   ⇒ 断中心与断左边缘对那 **4.69px** 是等价检出；顺带 y 也被钉上了（旧写法只断 x）。
                    //   **怎么改坏就红**：把那对常量改回 `542.06/609.12` ⇒ 中心左移 4.69px ≈ 0.0434 世界单位
                    //   （`CheckAtWorld` 的阈值是 0.01 世界单位 = 1.08px）⇒ 这一条红。
                    {
                        var ptTx = FindChild(FindChild(cav, "Price Display Button"), "text");
                        CheckAtWorld(ptTx, 546.75f, 613.81f, 730.73f, 777.65f,
                                     "`Price Display Button … text` 的矩形 = 原版 **546.75,730.73→613.81,777.65**"
                                     + "（HLG 净位移 +4.69 = `46.91 × (1.2−1) ÷ 2`；旧值 542.06 是丢了这个半格的读数）");
                        // 🆕 **2026-10-08（波 C3 · A213）**：这一格的 `折行` 也是判例本体 ——
                        //   原版 **`折行=0 · auto[13.46~40] · Center/Capline`**（同一条 dump 的 `折行=` 列），
                        //   而 `SocialWindow.Text` 原来恒折行 ⇒ `AlliancesTab.cs` 那一行本批补了 `wrap: false`。
                        //   **怎么改坏就红**：把那一处 `wrap: false` 去掉 ⇒ 期望 0、实得 1。
                        CheckWrapMode(ptTx, 0,
                                      "★ `Price Display Button … text`（`'1000'`）**不折行**（原版 `折行=0`）");
                    }

                    // ---- 已入盟支（`AllianceMemberTab`）：本地**走不到**，但建出来了 ----
                    var mv = FindChild(aRoot, "AllianceMemberVariant");
                    CheckTrue(mv != null && !mv.gameObject.activeSelf,
                              "`AllianceMemberVariant` 出厂 **act F**（原版按 `AlliancesManager` 二选一 ⇒ 本地恒走不到）");
                    mv.gameObject.SetActive(true);
                    var mt = sw.PageAlliances.Member;
                    CheckTrue(mt != null && mt.GeneralView != null && mt.TrophiesView != null,
                              "建出来了：`GeneralDetails` + `TrophiesWindow` 两棵都在");
                    CheckTrue(mt.GeneralView.activeSelf && !mt.TrophiesView.activeSelf,
                              "默认 `General`（原版 `TrophiesWindow` act **F**）");
                    var trh = FindChild(FindChild(mv, "Alliance Header Buttons (1)"), "Generic Tab UI Button Trophies/Hit");
                    if (trh != null && trh.GetComponent<WindowButton>() != null)
                    {
                        trh.GetComponent<WindowButton>().onClick();
                        CheckTrue(mt.TrophiesView.activeSelf && !mt.GeneralView.activeSelf,
                                  "点 `Trophies` ⇒ 切到奖杯页（**这个切换是纯本地的，能用**）");
                        mt.ShowGeneral();
                    }
                    mv.gameObject.SetActive(false);

                    // ---- 好友页 ----
                    var tabsNode = FindChild(FindChild(t, "Content Area"), "Tabs");
                    sw.tabButtons.Click(1);
                    var fr = FindChild(tabsNode, "Friends Tab");
                    CheckTrue(fr != null && fr.gameObject.activeSelf, "点左栏第 2 键 ⇒ 切到 `Friends Tab`");
                    var hd = FindChild(fr, "Header");
                    // 🔴 **`Header` 比窗框宽**（右边界 2085 > 1920）—— 原版就这么摆，别「对齐」掉
                    CheckAtWorld(hd, 332.17f, 2085.00f, 70.94f, 300.54f, "`Header`（⚠️ **右边界超出窗框**，原版如此）");
                    var fp = FindChild(hd, "Find players panel");
                    CheckAtWorld(fp, 357.37f, 1100.86f, 147.14f, 256.53f, "`Find players panel`");
                    CheckAtWorld(FindChild(fp, "Search Field"), 398.77f, 925.42f, 173.35f, 230.31f, "搜索框");
                    CheckText(TextOf(FindChild(fp, "Search Field")), "Enter player name", "占位文案 `Enter player name`");
                    // 🆕 **2026-10-09（A263）**：搜索框占位那颗的**折行真值**（原版 **`折行=0`**）。
                    //   波 C3 + 收尾件 Y 已把 `wrap: false` 落进实现（`Shell/FriendsTab.cs:138`），
                    //   **但当时一条断言都没有** ⇒ 谁改回去都不会红（Y 报告 §4·4 明写这条缺口）。
                    //   判据 = `资料/普查产出_1008/Y_断言收尾批.md` §4·1（原版 `m_TextWrappingMode` 的逐条实读值）。
                    //   ⚠️ `CheckWrapMode` 读的是 TMP 的**真字段** `_tmp.textWrappingMode`（按原版那个 int 报出来），
                    //   ⛔ 不是 `wrap` 实参。**改坏法**：删掉 `FriendsTab.cs:138` 的 `wrap: false`
                    //   ⇒ `SetAutoFitBox`→`SetWrapWidth` 把它开回 `Normal` ⇒ 这里读 1 ≠ 0 ⇒ 红。
                    CheckWrapMode(FindChild(FindChild(fp, "Search Field"), "Placeholder"), 0,
                                  "★ 好友页搜索框占位 `Enter player name` **不折行**（原版 `折行=0`）");
                    CheckAtWorld(FindChild(fp, "Add Friend Button"), 953.12f, 1033.12f, 167.63f, 236.04f,
                                 "`Add Friend Button`（⚠️ **没有 Button Text**，纯图标钮）");
                    CheckAtWorld(FindChild(fp, "Instant duel Button"), 1045.70f, 1125.70f, 167.63f, 236.04f,
                                 "`Instant duel Button`");
                    CheckText(TextOf(FindChild(fp, "Search Player")), "Search player", "`Search Player` 那行字");
                    var fl = FindChild(fr, "Friends List");
                    CheckText(TextOf(FindChild(fl, "Friends Title")), "Your friends:", "`Your friends:`");
                    CheckAtWorld(FindChild(fl, "Divisor line"), 343.14f, 1898.89f, 311.12f, 314.80f, "分隔线");
                    var fcont = FindChild(fl, "Friends Container");
                    CheckAtWorld(fcont, 332.15f, 1875.80f, 314.80f, 1080.06f, "`Friends Container`（`ScrollRect`）");
                    // 好友表恒空 ⇒ 0 行；喂一条 ⇒ 建出一行、且尺寸 = 网格 cell
                    Check(SocialData.Friends.Count, 0, "本地好友 **0 条**（服务器源）");
                    Check(FindChild(FindChild(FindChild(fcont, "Viewport"), "Content"), "Friend Info Item"), null,
                          "⇒ 好友行**一行都不建**（原版出厂 0 子，行全是运行期 `Instantiate` 的）");
                    SocialData.Friends.Add(new SocialData.Friend { Name = "Test Friend", Online = false });
                    sw.PageFriends.RebuildForTest();
                    var frow = FindChild(FindChild(FindChild(fcont, "Viewport"), "Content"), "Friend Info Item");
                    CheckTrue(frow != null, "喂一条 ⇒ **建出一行**");
                    if (frow != null)
                    {
                        CheckAtWorld(frow, 348.15f, 1069.45f, 334.80f, 419.62f,
                                     "行 = **网格 cell 721.3×84.82**（`GridLayoutGroup` 的 cell，不是 prefab 自带的 670.16）");
                        CheckText(TextOf(FindChild(frow, "Friend name")), "Test Friend", "行里那行名字");
                        CheckTrue(FindChild(frow, "Disconnected") != null && FindChild(frow, "Connected Image") == null,
                                  "离线 ⇒ **只有 `Disconnected`** 那个点（在线才建 `Connected Image`）");
                        // 三颗右对齐的图标钮（`a=(1,.5) pos=(-224.771/-133.8/-42.829,0)`，各 68.644×69.315）
                        var rowR2 = new PxRect(348.15f, 334.80f, 1069.45f, 419.62f);
                        float cy = (rowR2.y1 + rowR2.y2) * 0.5f;
                        CheckAtWorld(FindChild(frow, "Challenge button"),
                                     rowR2.x2 - 133.8f - 34.322f, rowR2.x2 - 133.8f + 34.322f,
                                     cy - 34.6575f, cy + 34.6575f,
                                     "行内 `Challenge button`（**从右边沿往里 133.8**，不是从左边算）");
                    }
                    SocialData.ResetForTest();
                    sw.PageFriends.RebuildForTest();
                    CheckTrue(FindChild(FindChild(FindChild(fcont, "Viewport"), "Content"), "Friend Info Item") == null,
                              "清空数据 ⇒ 行又没了（原版 `OnOpen` 清空重填）");
                    sw.tabButtons.Click(0);

                    // ---- 🆕 2026-10-03（A25①）：`SocialPage.Clip` 这条路**真的带电了吗** ----
                    // 病根（改之前）：`Clip` 是 `protected`、**全仓一处赋值都没有** ⇒ `Rect` / `Text` / `Hit` /
                    //   `Cosmetic` 四处转发过去的 `clip` **恒为 null** ⇒ 社交页画的东西**一处都吃不到裁切**
                    //   （滚动内容越出视口照样画满）。现在补了写入口：`SocialPage.SetClip` / `SocialView.SetClip`。
                    // 🔴 **判据 / 矩形都取原版真值**：`Friends Container>Viewport` = 332.15,314.80→1875.80,1080.06
                    //   （普查 §A·1 第 396 行：它身上就是 `RectMask2D`；上面 `CheckAtWorld(fcont, …)` 刚钉过同一个数）。
                    // 🔴 **凭什么说这几条能真红**：下面每块探针都**跨在视口那条边上** ⇒ 只要 `Clip` 没传到
                    //   `MenuDraw`，量到的就是**整块**（下沿 1150 / 命中区照建 / 右边那块字照样建）——
                    //   把 `SetClip` 或任一处转发拆掉，这几条立刻红。
                    {
                        var fvp = new PxRect(332.15f, 314.80f, 1875.80f, 1080.06f);   // 原版 `Viewport` 真值
                        var probe = MenuDraw.Node(sw.transform, "ClipProbe", fvp);      // 探针的临时节点（断完就删）
                        var pg = sw.PageFriends;                                        // **真的页对象**，不是派生出来的假页

                        pg.SetClip(fvp);
                        CheckTrue(pg.ClipNow.HasValue && MenuDraw.SameRect(pg.ClipNow.Value, fvp),
                                  "★ `SetClip(视口)` 之后 `ClipNow` 就是那个视口（写入口真的通了）");
                        // 三块探针：跨下沿的图 / 整块在下沿以外 / 跨下沿的命中区（各自验一处转发）
                        var low = pg.Rect(probe, null, new PxRect(500f, 950f, 900f, 1150f), "ClipProbeLow", 0);
                        var outHit = pg.Hit(probe, "ClipProbeOut", new PxRect(500f, 1100f, 900f, 1200f), 0, () => { });
                        var edgeHit = pg.Hit(probe, "ClipProbeEdge", new PxRect(500f, 950f, 900f, 1150f), 0, () => { });
                        // `SocialPage.Text` 那一处**只判横轴**（判据就是它自己那一句）⇒ 拿「整块在右沿以外」来验
                        // 🆕 **2026-10-11（A317）**：`SocialPage.Text` 的 `autoMinPx` / `wrap` 已**去掉缺省、形参必填**
                        //   （判据 = `资料/普查产出_1010/调度台_口径裁定_1011.md` §A258）⇒ 这一条**必须补全实参**
                        //   （原来是 7 实参 ⇒ 删缺省之后全工程只剩它一处 `CS7036`）。
                        //   补的两个值 = **它原来的缺省**（`0f` / `true`）—— ⚠️ **这不是「跟着缺省值抄」**：
                        //   本件是**探针**（原版没有这个节点 ⇒ 没有「原版档位」可读），而且它**在 `ClipRect`
                        //   那一句就返回 `null`**（`x1=1900 > 视口右沿 1875.80`）⇒ 这两个实参对本条断言**零影响**，
                        //   只为编得过。`alignLeft` 那口仍带缺省（`true` = 今天的行为），照旧不传。
                        var outText = pg.Text(probe, new PxRect(1900f, 500f, 2000f, 600f), "x", Color.white,
                                              "ClipProbeText", 30f, 0, 0f, true);
                        // 九宫格（`Rect`/`Text`/`Hit`/`Cosmetic` 都传了 `Clip`，**九宫格这一路原来漏了**，同一批补上）：
                        // 先用**框内**那一次证明「图取得到」（否则下面那次 null 是自我实现、什么都验不到）
                        var nineIn = pg.Nine(probe, "40K_dropdown_bg", new PxRect(500f, 400f, 900f, 500f),
                                             new Vector4(23f, 20f, 23f, 20f), "ClipProbeNineIn", 0);
                        var nineOut = pg.Nine(probe, "40K_dropdown_bg", new PxRect(500f, 1100f, 900f, 1200f),
                                              new Vector4(23f, 20f, 23f, 20f), "ClipProbeNineOut", 0);
                        pg.SetClip(null);
                        CheckTrue(pg.ClipNow == null,
                                  "`SetClip(null)` 清掉了（**画完必须清** —— 不清的话后面画的件会继续吃这道裁切）");

                        float lx1 = 0f, ly1 = 0f, lx2 = 0f, ly2 = 0f;
                        CheckTrue(low != null && RenderedRect(low.transform, out lx1, out ly1, out lx2, out ly2),
                                  "跨在下沿上的那块图建出来了");
                        CheckNear(ly2, fvp.y2, 0.5f,
                                  "★ 它的**下边缘被截到视口下沿 1080.06**（没设 `Clip` 时这里会是 1150 —— 真红点）");
                        CheckNear(ly1, 950f, 0.5f, "…上边缘没被碰（只截越界的那一侧）");
                        CheckTrue(outHit == null,
                                  "★ 整块在视口外的命中区**连节点一起不建**（原版 `RectMask2D` 的射线那一面）");
                        CheckTrue(outText == null, "★ 整块在视口右沿以外的文字**不建**（`Text` 那一处转发）");
                        CheckTrue(nineIn != null, "框内的九宫格建出来了（`40K_dropdown_bg` 取得到图 —— 下面那条的对照组）");
                        CheckTrue(nineOut == null,
                                  "★ 整块在视口外的**九宫格也连节点一起不建**（`Nine` 那一处原来漏了传 `Clip`）");
                        float ex1 = 0f, ey1 = 0f, ex2 = 0f, ey2 = 0f;
                        CheckTrue(edgeHit != null && HitQuadRect(edgeHit, out ex1, out ey1, out ex2, out ey2),
                                  "跨在下沿上的命中区建出来了");
                        CheckNear(ey2, fvp.y2, 0.5f, "★ 它的 quad **也被截到同一条下沿**（命中区跟着裁）");

                        // 子视图那一层：`SocialView.SetClip` 转调宿主页 ⇒ 同一道裁切（`AllianceSearchTab` 是真的视图）
                        var view = sw.PageAlliances.Search;
                        CheckTrue(view != null, "联盟页那一支的子视图在（`AllianceSearchTab`）");
                        if (view != null)
                        {
                            view.SetClip(fvp);
                            var vq = view.Rect(probe, null, new PxRect(500f, 950f, 900f, 1150f), "ClipProbeView", 0);
                            view.SetClip(null);
                            float vx1 = 0f, vy1 = 0f, vx2 = 0f, vy2 = 0f;
                            CheckTrue(vq != null && RenderedRect(vq.transform, out vx1, out vy1, out vx2, out vy2),
                                      "子视图画的同一块也建出来了");
                            CheckNear(vy2, fvp.y2, 0.5f,
                                      "★ 子视图（`SocialView.SetClip` → 宿主页）**也吃到同一道裁切**");
                        }

                        SocialWindow.DestroySafe(probe.gameObject);   // 探针断完就删（别留给后面的断言与截图）
                    }

                    // ============================================================ 🆕 2026-10-03（A25④）：
                    // **四处滚动视口**（联盟页公开列表 / 好友页 / 已入盟支的成员列 / 🆕 2026-10-04 A30 的奖杯格）
                    // **真的接上滚动 + 裁切了吗**。
                    // 🔴 改之前：四个视口**一处 `MenuScroll` 都没有**（`grep MenuScroll Shell/{AlliancesTab,FriendsTab,
                    //   AllianceMemberTab}.cs` 零命中）⇒ 内容一多就**画到框外**（原版那四个 `RectMask2D`/`Mask` 没人等效）。
                    //   本批补了「整行滚出视口 ⇒ 不建」这道守卫 ⇒ **没有滚动区就会把后面的行彻底藏掉** ——
                    //   两件必须**一起**补（`BattleLogPopup` 上就是这么踩过来的）。下面每处都断三件：
                    //   ① 滚动区真的建了（且档位照原版）· ② 走**真路**（`PointerLayer.WheelAt(视口中心, ∓120)`）真的滚得动
                    //   · ③ 越界内容**不再画到框外**（量**渲出来那块**，不是节点 —— 裁剪会把 quad 挪走、节点不动）。
                    // 🔴 **档位先读原版那个 `ScrollRect` 的 `m_MovementType` 再定**：四件**全是 `m_MovementType = 1`**
                    //   （原始 JSON 实读：`bundle_menus_assets_all/MonoBehaviour/` 里 `…-992038356198235997.json`（Friends
                    //   Container）· `…-8780120914984378205.json`（Open Alliances，`RecyclableScrollRect : ScrollRect`）
                    //   · `…-2224558054710517597.json`（MemberList Scroll View）· 🆕 `922…198729379.json`
                    //   （`TrophiesWindow>Scroll Rect`，A30 那一格），四条都靠 `m_Content` 的 pid 认的）。
                    //   UGUI 的枚举是 `Unrestricted=0 / Elastic=1 / Clamped=2`（判据 = 本工程那份 UGUI 源码的
                    //   `ScrollRect.MovementType`）⇒ **四处都是 Elastic**。
                    // ⚠️ `Shell/MenuScroll.cs` 文件头与 `资料/阶段二_滚动与指针_原版规格.md` §1·1 那句
                    //   「1 = Clamped / 2 = Elastic」**是反的**（本批发现，已写进报告）：同一仓里 `BattleLogPopup.cs:19`
                    //   按 `2 (Clamped)` / `1 (Elastic)` 读、`资料/普查产出_0923/A3_Cards页.md:55` 写 `1(Elastic)`。
                    // ⚠️ 期望值一律**现算**（`RowsInViewport` / `CellsInViewport`），参数取**原版值**；⛔ 不写死条数。
                    // 🆕 **软边**（2026-10-04 A30）：四处里**只有奖杯那一格**的原版 `RectMask2D.m_Softness ≠ (0,0)`
                    //   （`(0,50)` —— `_tmp_view/q1_rm2d.txt:45`；另三处都是 `(0,0)`）⇒ ④ 那一块单独断它。
                    {
                        var pl3 = PointerLayer.Instance;
                        float qTol = 1.0f;      // 量「有没有画到框外」的容差（被截到边上的那块会正好落在边界上）

                        // ------------------------------------------------ ① `Open Alliances`（联盟页 · 未入盟支）
                        {
                            const int NA = 11;
                            for (int i = 1; i <= NA; i++)
                                SocialData.OpenAlliances.Add(new SocialData.AllianceListing
                                { Name = "Open " + i, Region = "Global", Members = i, MemberMax = 20, Rating = "" });
                            var search = sw.PageAlliances.Search;
                            search.RebuildForTest();          // = 原版 `FillOpenAlliances`（清空重填）那条路
                            var oaNode = FindChild(FindChild(lv, "List Area"), "Open Alliances");
                            var oaVp = FindChild(oaNode, "Viewport");
                            var oaList = FindChild(oaVp, "List");
                            CheckAtWorld(oaVp, 360.99f, 1874.90f, 337.29f, 1079.77f,
                                         "`Open Alliances>Viewport`（原版 `Image + RectMask2D`；下面滚动区的视口就是它）");
                            var os = search.OpenListScroll;
                            CheckTrue(os != null,
                                      "★ 联盟页 `Open Alliances` 那一格**有滚动区了**（原版 = `RecyclableScrollRect`；"
                                      + "改之前一处都没有）");
                            if (os != null && oaList != null)
                            {
                                const float VTop = 337.29f, VBot = 1079.77f;       // = 原版 `Viewport`（上面刚钉过）
                                // 🔴 **原版值，⛔ 不引用实现常量**（A35②）—— 而且**这两列的行距不是一个数**（A35①）：
                                //   · 行高 **110** = 行 prefab `Alliance List Entry` 的 `sizeDelta.y`
                                //     （§A·2·2 的独立根 `471114273799851884`）；
                                //   · 行距 **5** = `Open Alliances` 那一格 **`RecyclableScrollRect`** 的 `_spacingY`
                                //     （原始 JSON 实读：`bundle_menus_assets_all/MonoBehaviour/
                                //      MonoBehaviour_-8780120914984378205.json` 的 `_spacingY: 5.0` /
                                //      `_cellHeight: 110.0` / `IsGrid: 0`）⇒ **行距 = `_spacingY + _cellHeight` = 115**
                                //     （判据 = `PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c:255,270`
                                //      与 `…_InitCoroutine_d__19__MoveNext.c:57-63`；同仓 `A3_Cards页.md:78`）。
                                //   ⚠️ `Invitations` 那一列才是间距 **10**（它自己是普通 VLG、`spacing=10`，
                                //     普查 `社交_联盟与好友页.md:67`）—— **这里原来错抄了那个 10**：
                                //     11 行时可滚范围多 50px、第 6 行起 y 全偏，而断言与实现同式 ⇒ 必绿（①）。
                                const float RowH0 = 110f, SpacingY0 = 5f;
                                float pitchA = RowH0 + SpacingY0;
                                CheckNear(os.Viewport.y1, VTop, 0.5f,
                                          "滚动区视口 = `Viewport` 那个节点自己的矩形（**没有另挑一个**）");
                                CheckTrue(os.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(os.Elastic, "**Elastic**（原版 `m_MovementType=1`：拖出去有橡皮筋、松手回弹）");
                                float contentA = NA * RowH0 + (NA - 1) * SpacingY0;
                                CheckNear(os.MaxOffset, contentA - (VBot - VTop), 0.5f,
                                          $"可滚范围 = 内容高（{NA}×110 + {NA - 1}×5 = {contentA:F0}）− 视口高 {VBot - VTop:F2}"
                                          + " —— **补之前这里恒 0**（滚轮全被夹回 0，而「整行滚出视口 ⇒ 不建」"
                                          + "会把第 8 行起彻底藏掉）"
                                          + "；⚠️ 行距是 `RecyclableScrollRect._spacingY = 5`（**不是** `Invitations`"
                                          + " 那一列的 10 —— 2026-10-04 订正 ①）");
                                CheckTrue(os.MaxOffset > 1f, "★ 确实**滚得动**了");
                                int wantA = RowsInViewport(NA, VTop, VBot, pitchA, RowH0, 0f);
                                Check(CountChildren(oaList, "Entry"), wantA,
                                      $"★ 喂 {NA} 行 ⇒ **恰好建了与视口相交的那 {wantA} 行**（现算；"
                                      + $"视口高 {VBot - VTop:F2}、行距 {pitchA:F0}）—— 整行在视口外的连节点一起不建");
                                CheckTrue(wantA > 0 && wantA < NA,
                                          $"…而且**确实有整行落在视口外**（{NA - wantA} 行不建）—— 否则这一条等于没验");
                                float aTop0, aBot0, aTop1, aBot1;
                                CheckTrue(RowSpan(oaList, "Entry", RowH0, out aTop0, out aBot0),
                                          "建出来的行节点都在（量它们的位置，不量被裁过的 quad）");
                                CheckNear(aTop0, VTop, 0.5f, "最上面那行的顶边 = 视口顶 337.29（顺带证明偏移是 0）");
                                CheckTrue(aBot0 > VBot + 0.5f,
                                          $"…而最后一颗建出来的行**压在视口下沿上**（行底 {aBot0:F2} > 视口底 {VBot:F2}）"
                                          + "—— 这条是下面「没有 quad 画到框外」的**前提**（不满足说明这条测试自己失效了）");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1117.94f, 708.53f) == os,
                                          "视口中心（1117.94,708.53）上命中的滚动区**就是这一格**");
                                CheckTrue(pl3 != null && pl3.WheelAt(1117.94f, 708.53f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt` —— 与真鼠标同一条路）");
                                CheckTrue(os.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {os.Offset:F2}px）");
                                RowSpan(oaList, "Entry", RowH0, out aTop1, out aBot1);
                                CheckNear(aTop0 - aTop1, os.Offset, 0.5f,
                                          "★ **滚动之后行真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                Check(CountChildren(oaList, "Entry"),
                                      RowsInViewport(NA, VTop, VBot, pitchA, RowH0, os.Offset),
                                      "滚一格之后在建的行数 == 现算值（重建是**先清再建**，不清会越滚越多）");
                                var badA = QuadsOutside(oaList, new PxRect(360.99f, VTop, 1874.90f, VBot), qTol);
                                CheckTrue(badA.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（量的是**渲出来那块**：越界 " + badA.Count + " 颗"
                                          + (badA.Count > 0 ? "：" + string.Join(" / ", badA.ToArray()) : "")
                                          + "）—— 拆掉 `SetClip` 这一条立刻红");
                                os.SetOffset(os.MaxOffset);
                                int wantA2 = RowsInViewport(NA, VTop, VBot, pitchA, RowH0, os.Offset);
                                Check(CountChildren(oaList, "Entry"), wantA2,
                                      $"滚到最下 ⇒ 仍然**恰好建了与视口相交的那几行**（现算 {wantA2} 行）");
                                CheckTrue(!RowTextsOf(oaList, "Entry", "Title").Contains("Open 1")
                                          && RowTextsOf(oaList, "Entry", "Title").Contains("Open " + NA),
                                          "★ 滚到最下**看得见最后一行了**（第 1 行滚出视口、第 " + NA + " 行进来；"
                                          + "建出来的：" + string.Join("/", RowTextsOf(oaList, "Entry", "Title").ToArray())
                                          + "）—— 改之前这一行**永远看不到也点不到**");
                                os.SetOffset(0f);
                                SocialData.OpenAlliances.Clear();
                                search.RebuildForTest();
                            }
                        }

                        // ------------------------------------------------ ② 好友页（`GridLayoutGroup` 网格）
                        {
                            const int NF2 = 20;
                            for (int i = 1; i <= NF2; i++)
                                SocialData.Friends.Add(new SocialData.Friend { Name = "Friend " + i, Online = i % 2 == 0 });
                            sw.tabButtons.Click(1);           // 切到好友页 = `ChangeTab` → `OnOpen` → `BuildRows`（真路）
                            var fpg = sw.PageFriends;
                            var fsc = fpg.ListScroll;
                            CheckTrue(fsc != null,
                                      "★ 好友页 `Friends Container` 那一格**有滚动区了**（原版 = `ScrollRect`；"
                                      + "改之前连 `MenuScroll` 都没有 —— 文件里当时还写着「没有 `_scroll`」）");
                            var fvNode = FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Friends Tab");
                            fvNode = FindChild(FindChild(FindChild(fvNode, "Friends List"), "Friends Container"), "Viewport");
                            var fContent = FindChild(fvNode, "Content");
                            CheckAtWorld(fvNode, 332.15f, 1875.80f, 314.80f, 1080.06f,
                                         "`Friends Container>Viewport`（原版是 `RectMask2D` 那个节点）");
                            CheckTrue(fsc != null && fContent != null, "滚动区与它下面的 `Content` 都在");
                            if (fsc != null && fContent != null)
                            {
                                const float VTop = 314.80f, VBot = 1080.06f;      // = 原版 `Viewport`（上面刚钉过）
                                // 原版 `GridLayoutGroup`（`Content` 上）：cell 721.3×84.82 · spacing (11.2,12.7)
                                // · pad **(左 16, 右 0, 上 20, 下 0)**（普查 `社交_联盟与好友页.md:576`「§B·12 三处
                                // `GridLayoutGroup`」那张表，表头写明是「左,右,上,下」）—— 与上面
                                // `CheckAtWorld(frow, 348.15f, 1069.45f, 334.80f, 419.62f)` 用的是同一组数
                                // （那条钉的是**行本身**；`348.15 = 332.15 + 16`、`334.80 = 314.80 + 20`
                                // 也反证了左 16 / 上 20）。
                                const float CW = 721.3f, CH = 84.82f, GX = 11.2f, GY = 12.7f,
                                            PL = 16f, PR = 0f, PT = 20f;
                                // 🔴 列数式照 UGUI 那一份**独立复算一遍**（`Library/PackageCache/
                                //   com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/GridLayoutGroup.cs:184`）：
                                //   分子吃的是 `padding.horizontal = 左 + 右 = 16`（**不是 `2×左 = 32`**）、
                                //   末尾那个 **`+ 0.001f`** 也不能少 —— 两条原来都写错（2026-10-04 审查查出 ⑤）。
                                //   ⚠️ 今天两种写法都得 2 列（所以一直没现形）；`W ∈ [1469.8, 1485.8)` 时分岔。
                                int colsF = Mathf.Max(1, Mathf.FloorToInt(
                                    ((1875.80f - 332.15f) - (PL + PR) + GX + 0.001f) / (CW + GX)));
                                int rowsF = Mathf.CeilToInt(NF2 / (float)colsF);
                                // 🆕 2026-10-04（A35⑤）：**列数式的可真红判据** —— 这一屏（W = 1543.65）
                                //   两种写法同得 2 列、分不出来 ⇒ 拿**别的宽度**去咬它
                                //   （`FriendsTab.ColumnsFor` 就是生产路径上那个纯函数）。
                                //   期望值 = 照原版 `GridLayoutGroup.cs:184`
                                //   `Max(1, Floor((w − (16+0) + 11.2 + 0.001) / (721.3 + 11.2)))` **手算**出来的，
                                //   ⛔ 不调实现的任何东西算期望：
                                //     · w = 1543.65（这一屏）→ `1538.851 / 732.5 = 2.1008` ⇒ **2**
                                //     · w = 1480 → `1475.201 / 732.5 = 2.0140` ⇒ **2**
                                //       ⚠️ 旧写法（分子吃 `2×padLeft = 32`）在这里是 `1459.2 / 732.5 = 1.9921`
                                //       ⇒ **1 列** —— 所以这一条**改回旧写法立刻红**（= ⑤ 的可真红面）
                                //     · w = 1475 → `1470.201 / 732.5 = 2.0071` ⇒ **2**
                                //       （旧写法若把 `spacing.x` 漏在分子外 ⇒ `1459 / 732.5 = 1.9918` ⇒ 1 列）
                                //     · w = 700 → `695.201 / 732.5 = 0.9491` ⇒ 夹到 **1**
                                //   ⚠️ `+0.001f` 那一位**没法用宽度表咬住**（它只在 `w` 落进 0.001 宽的整除窗口
                                //   时才起作用，浮点噪声同量级）⇒ 那一处只靠代码 + 注释，已记进报告。
                                Check(FriendsTab.ColumnsFor(1543.65f), 2, "列数（这一屏 W = 1543.65）");
                                Check(FriendsTab.ColumnsFor(1480f), 2,
                                      "★ 列数（W = 1480：原版 **2 列**；旧写法 `− 2×padLeft` 只给 1 列 ⇒ 能真红）");
                                Check(FriendsTab.ColumnsFor(1475f), 2,
                                      "★ 列数（W = 1475：`spacing.x` 必须**加在分子上**；漏了就只给 1 列）");
                                Check(FriendsTab.ColumnsFor(700f), 1, "列数下限夹到 1（窄到一行放不下两格）");
                                CheckTrue(fsc.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(fsc.Elastic, "**Elastic**（原版 `m_MovementType=1`）");
                                // 内容高 = UGUI `GridLayoutGroup.cs:188` 的 MinSize
                                // `padding.vertical + (cell.y + spacing.y) × 排数 − spacing.y`（同一份判据，独立算一遍）
                                // = 原版 `Content` 上那个 `ContentSizeFitter(m_VerticalFit = 1 MinSize)` 的高。
                                float contentF = PT + rowsF * CH + Mathf.Max(0, rowsF - 1) * GY;
                                CheckNear(fsc.MaxOffset, contentF - (VBot - VTop), 0.5f,
                                          $"可滚范围 = 内容高（pad 20 + {rowsF} 排×84.82 + {rowsF - 1}×12.7 = {contentF:F2}）"
                                          + $" − 视口高 {VBot - VTop:F2} —— **补之前这里恒 0**（滚轮全被夹回 0）");
                                CheckTrue(fsc.MaxOffset > 1f, "★ 确实**滚得动**（可滚范围 > 0）");
                                int wantF2 = CellsInViewport(NF2, colsF, PT, CH + GY, CH, VTop, VBot, 0f);
                                Check(fpg.BuiltRows, wantF2,
                                      $"★ 喂 {NF2} 条好友（{colsF} 列 ⇒ {rowsF} 排）⇒ **恰好建了与视口相交的"
                                      + $"那 {wantF2} 格**（现算；视口高 {VBot - VTop:F2}、排距 {CH + GY:F2}）");
                                Check(CountChildren(fContent, "Friend Info Item"), fpg.BuiltRows,
                                      "建出来的格节点个数 == `BuiltRows`（两者不许各说各的）");
                                CheckTrue(wantF2 > 0 && wantF2 < NF2,
                                          $"…而且**确实有格落在视口外**（{NF2 - wantF2} 格不建）");
                                float fTop0, fBot0, fTop1, fBot1;
                                CheckTrue(RowSpan(fContent, "Friend Info Item", CH, out fTop0, out fBot0),
                                          "建出来的好友格都在");
                                CheckTrue(fBot0 > VBot + 0.5f,
                                          $"…而最后一排**压在视口下沿上**（格底 {fBot0:F2} > 视口底 {VBot:F2}）"
                                          + "—— 下面「没有 quad 画到框外」的前提");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1103.98f, 697.43f) == fsc,
                                          "视口中心（1103.98,697.43）上命中的滚动区**就是这一格**");
                                CheckTrue(pl3 != null && pl3.WheelAt(1103.98f, 697.43f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt`）");
                                CheckTrue(fsc.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {fsc.Offset:F2}px）");
                                RowSpan(fContent, "Friend Info Item", CH, out fTop1, out fBot1);
                                CheckNear(fTop0 - fTop1, fsc.Offset, 0.5f,
                                          "★ **滚动之后格子真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                Check(fpg.BuiltRows, CellsInViewport(NF2, colsF, PT, CH + GY, CH, VTop, VBot, fsc.Offset),
                                      "滚一格之后在建的格数 == 现算值");
                                var badF = QuadsOutside(fContent, new PxRect(332.15f, VTop, 1875.80f, VBot), qTol);
                                CheckTrue(badF.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（越界 " + badF.Count + " 颗"
                                          + (badF.Count > 0 ? "：" + string.Join(" / ", badF.ToArray()) : "") + "）");
                                fsc.SetOffset(0f);
                                SocialData.Friends.Clear();
                                fpg.RebuildForTest();
                                Check(fpg.BuiltRows, 0, "清空好友 ⇒ 格又没了（自检不留假数据）");
                            }
                        }

                        // ------------------------------------------------ ③ 已入盟支的成员列
                        //   ⚠️ 这一支本地**走不到**（原版按服务器的 `AlliancesManager` 二选一）⇒ 自检手动点亮，
                        //      并把**未入盟支关掉**：两个视口叠在一起会让 `ScrollUnder` 命中的不是这一格（假红）。
                        {
                            // 🆕 2026-10-04（A40）：**原版是两列** ⇒ 条数要够大，才还有「整排落在视口外」那一档
                            //   （视口高 586.42 ⇒ 一屏放得下 6 排 = 12 格；NM=14 ⇒ 7 排 14 格 ⇒ 第 7 排不建）。
                            const int NM = 14;
                            for (int i = 1; i <= NM; i++)
                                SocialData.Members.Add(new SocialData.Member
                                { Index = i, Name = "Member " + i, Role = "Alliance Master", Online = i % 2 == 0,
                                  DraftRating = "", RankedRating = "" });
                            sw.tabButtons.Click(0);           // 先在联盟页（上面切到好友页了）
                            bool nmWas = nmv.gameObject.activeSelf;
                            nmv.gameObject.SetActive(false);
                            mv.gameObject.SetActive(true);
                            var mt2 = sw.PageAlliances.Member;
                            mt2.RebuildMembersForTest();      // = 原版 `AllianceMemberList` 清空重填那条路
                            var ms = mt2.MemberScroll;
                            CheckTrue(ms != null, "★ 成员列那一格**有滚动区了**（原版 `MemberList>Scroll View` 的 `ScrollRect`）");
                            var mVp = FindChild(FindChild(mv, "GeneralDetails"), "MemberList");
                            mVp = FindChild(FindChild(mVp, "Scroll View"), "Viewport");
                            var mContent = FindChild(mVp, "Content");
                            CheckAtWorld(mVp, 369.67f, 1880.67f, 493.63f, 1080.05f,
                                         "`MemberList>Scroll View>Viewport`（原版 `Image + Mask`，`showGraphic=0`）");
                            // ==================================================== 🆕 2026-10-09（A263）
                            // **社交窗那 14 处「折行」真值 = 7 处 `0` + 7 处 `1`**。
                            //   波 C3 + 收尾件 Y 已经把 `wrap: false` 落进了实现
                            //   （`Shell/{FriendsTab,AllianceMemberTab}.cs`），**但那时一条断言都没有**
                            //   ⇒ 将来谁改回去都不会红（Y 报告 `资料/普查产出_1008/Y_断言收尾批.md` §4·2 / §4·4
                            //   明写这条缺口）。⛔ 本块**只补断言、不动实现**（那两个文件在白名单外）。
                            //   判据 = 上面那份 Y 报告 §4·2 的**逐条真值**（原版 dump 的 `折行=` 列）——
                            //   ⛔ 不自己 dump、不按 token 名猜。
                            // 🔴 读的是 `Label.WrappingMode` = **TMP 的真字段** `_tmp.textWrappingMode`
                            //   （按原版那个 int 原文报出来），⛔ 不是 `wrap` 实参（那是自证）。
                            // 🔴 **7 处 `1` 也要一起断** —— 只断那 7 个 `0` 的话，「一刀切全设 false」照样绿（弱断言）。
                            // **改坏法**：去掉任一处 `wrap: false` ⇒ 那一件被 `MenuDraw.TextBox`→`SetWrapWidth`
                            //   开成 `Normal` ⇒ 读 1 ≠ 0 ⇒ 红；把 7 处 `1` 里任一处改成 `false` ⇒ 读 0 ≠ 1 ⇒ 也红。
                            // ⚠️ 「哪一颗是哪个节点」一律**按名字/路径取**（⛔ 不按建出来的序号认种类）；
                            //   只有**成员行内**那 4 颗是运行期按数据长的 ⇒ 载体 = **第 0 行**
                            //   （`NthChild(…, "Alliance Member Entry", 0)`，与下面 `mFirst` 同一句法）。
                            {
                                var tro = FindChild(mv, "TrophiesWindow");
                                // ⚠️ 名字带 `a263` 前缀：`Run()` 体内**已经有一个** `chat`（主菜单那扇，
                                //    `:516`）⇒ 同名会 `CS0136`（嵌套作用域里重名 = 编不过）。
                                var a263Chat = FindChild(mv, "ChatPreview");
                                var tabRow = FindChild(FindChild(mv, "Alliance Header Buttons (1)"), "Tab buttons");
                                var gdM = FindChild(mv, "GeneralDetails");
                                var gdC = gdM != null ? FindChild(gdM, "Content") : null;
                                var mrow0 = NthChild(mContent, "Alliance Member Entry", 0);
                                // ① 那 7 处原版 **`折行 = 0`**
                                CheckWrapMode(tabRow != null ? FindChild(FindChild(tabRow, "Generic Tab UI Button Info"), "Button Text") : null, 0,
                                              "★ 二级页签键 `Button Text`（`General`）**不折行**（原版 `折行=0`）");
                                CheckWrapMode(tro != null ? FindChild(FindChild(tro, "CurrentActiveBadge Name"), "Text") : null, 0,
                                              "★ `CurrentActiveBadge Name > Text`（`Featured: Trophy Name`）**不折行**（原版 `折行=0`）");
                                CheckWrapMode(tro != null ? FindChild(FindChild(tro, "CurrentActiveBadge Count"), "Text") : null, 0,
                                              "★ `CurrentActiveBadge Count > Text`（`45 Trophies Achieved!`）**不折行**（原版 `折行=0`）");
                                CheckWrapMode(a263Chat != null ? FindChild(FindChild(FindChild(a263Chat, "Container"), "Message Preview"), "text") : null, 0,
                                              "★ `ChatPreview` 第 1 行 `MsgRow > text` **不折行**（原版 `折行=0`）");
                                CheckWrapMode(gdC != null ? FindChild(FindChild(gdC, "Alliance name text"), "Text") : null, 0,
                                              "★ `Alliance name text > Text`（盟名）**不折行**（原版 `折行=0`）");
                                CheckWrapMode(mrow0 != null ? FindChild(mrow0, "member index") : null, 0,
                                              "★ 成员行 `member index > Text`（名次数字）**不折行**（原版 `折行=0`）");
                                // ② 另外 7 处原版 **`折行 = 1`**（`extra_info` 在**未入盟支那一棵**上，见 `Variant.Search`）
                                var sv2 = sw.PageAlliances.Search;
                                var gdF2 = sv2 != null && sv2.GeneralDetailsView != null
                                         ? sv2.GeneralDetailsView.transform : null;
                                CheckWrapMode(gdF2 != null ? FindChild(FindChild(gdF2, "Config fields"), "extra_info") : null, 1,
                                              "★ `extra_info`（`English / Private`，在 **act F 那一棵**上）**要折行**（原版 `折行=1`）");
                                CheckWrapMode(gdC != null ? FindChild(FindChild(FindChild(gdC, "Description input text"), "description text"), "Text") : null, 1,
                                              "★ `description text`（联盟简介）**要折行**（原版 `折行=1`）");
                                CheckWrapMode(gdC != null ? FindChild(FindChild(FindChild(gdC, "MemberList"), "members label"), "Text") : null, 1,
                                              "★ `members label`（`Members: --/20`）**要折行**（原版 `折行=1`）");
                                CheckWrapMode(gdC != null ? FindChild(FindChild(FindChild(gdC, "Alliance Rating Display"), "Individual rating value"), "Text") : null, 1,
                                              "★ `Alliance Rating Display > Individual rating value` **要折行**（原版 `折行=1`）");
                                CheckWrapMode(mrow0 != null ? FindChild(mrow0, "Draft Rating/Individual rating value") : null, 1,
                                              "★ 成员行 `Draft Rating > Individual rating value` **要折行**（原版 `折行=1`）"
                                            + "（⚠️ 那一颗的节点名里**带斜杠** —— `FindChild` 是**按整名字精确比**，不是 `Transform.Find` 的路径语义）");
                                CheckWrapMode(mrow0 != null ? FindChild(mrow0, "member name") : null, 1,
                                              "★ 成员行 `member name` **要折行**（原版 `折行=1`）");
                                CheckWrapMode(mrow0 != null ? FindChild(mrow0, "member role") : null, 1,
                                              "★ 成员行 `member role` **要折行**（原版 `折行=1`）");
                            }
                            if (ms != null && mContent != null)
                            {
                                const float VTop = 493.63f, VBot = 1080.05f;      // = 原版 `Viewport`（上面刚钉过）
                                // 原版那个 `GridLayoutGroup`（原始 JSON 实读 `MonoBehaviour_6868526478606655651.json`，
                                // 挂在 `MemberList>Scroll View>Viewport>Content` 的 GO `6909745686555386019` 上）：
                                // cell 750×100 · spacing (10,**7.22**) · pad **(左0,右0,上9,下75)** ·
                                // `m_Constraint = 0 (Flexible)`。
                                const float CH2 = 100f, GY2 = 7.22f, PT2 = 9f, PB2 = 75f;   // spacing.x = 10 见下（列数/步进）
                                const int ColsM = 2;      // = `ColumnsFor(1511)`：Floor((1511−0+10.001)/760) = 2
                                // 🆕 2026-10-04（A40）：**列数式**单独钉两条 —— 期望值都是**手算的字面量**
                                //   （⛔ 不是从实现常量读来的）：视口宽 1511（上面 `CheckAtWorld(mVp, …)` 那个
                                //   1880.67−369.67）⇒ `Floor((1511 + 10.001)/760)` = **2**（`750+10+750 = 1510 ≤ 1511`）；
                                //   再窄 2px（1509）就掉到 **1** 列（`1519.001/760 = 1.998…`）—— 这一条是它能真红的地方。
                                Check(AllianceMemberRow.ColumnsFor(1511f), 2,
                                      "★ 列数式（UGUI `GridLayoutGroup.cs:184`）：视口宽 1511 ⇒ **2 列**"
                                      + "（`750+10+750 = 1510 ≤ 1511`）");
                                Check(AllianceMemberRow.ColumnsFor(1509f), 1,
                                      "…视口宽 1509 ⇒ 掉到 **1 列**（`(1509+10.001)/760 = 1.998`）—— 列数**真的在算**");
                                CheckNear(ms.Viewport.y1, VTop, 0.5f,
                                          "滚动区视口 = `Viewport` 那个节点自己的矩形（**没有另挑一个**）");
                                CheckTrue(ms.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(ms.Elastic, "**Elastic**（原版 `m_MovementType=1`）");
                                // 内容高 = UGUI `GridLayoutGroup.cs:188` 的 MinSize
                                // `padding.vertical + (cell.y + spacing.y) × 排数 − spacing.y`（**独立复算**）。
                                // 🔴 **2026-10-04（A40）改**：原来的「排数」取 **NM**（每行一条 = 当时的单列排法）；
                                //   原版那个 `GridLayoutGroup` 是两列（上面那两条）⇒ **排数 = `CeilToInt(NM / 2)`**。
                                //   NM = 14 ⇒ 7 排 ⇒ 内容高 = `(9+75) + 7×100 + 6×7.22 = 827.32`。
                                int rowsM = (NM + ColsM - 1) / ColsM;
                                float contentM = PT2 + PB2 + rowsM * CH2 + (rowsM - 1) * GY2;
                                CheckNear(ms.MaxOffset, contentM - (VBot - VTop), 0.5f,
                                          $"可滚范围 = 内容高（9 + {rowsM}×100 + {rowsM - 1}×7.22 + 75 = {contentM:F2}）"
                                          + $"− 视口高 {VBot - VTop:F2} —— **补之前这里恒 0**"
                                          + $"；⚠️ 排数按**两列**折半（{NM} 条 ⇒ {rowsM} 排）");
                                CheckTrue(ms.MaxOffset > 1f, "★ 确实**滚得动**了");
                                int wantM = CellsInViewport(NM, ColsM, PT2, CH2 + GY2, CH2, VTop, VBot, 0f);
                                Check(CountChildren(mContent, "Alliance Member Entry"), wantM,
                                      $"★ 喂 {NM} 个成员（{ColsM} 列 ⇒ {rowsM} 排）⇒ **恰好建了与视口相交的那"
                                      + $" {wantM} 格**（现算）—— 整排落在视口外的连节点一起不建");
                                CheckTrue(wantM > 0 && wantM < NM,
                                          $"…而且**确实有整排落在视口外**（{NM - wantM} 格不建）");
                                // 🆕 A40：两列的格位（第 1 排两格 = 369.67,502.63→1119.67,602.63 与
                                //   **+760**；原版那份单格实例的 rect 就是前者，见普查 §A·1 第 326 行）
                                var mFirst = NthChild(mContent, "Alliance Member Entry", 0);
                                CheckAtWorld(mFirst, 369.67f, 1119.67f, 502.63f, 602.63f,
                                             "第 1 格 = 视口左上 + (padLeft 0, padTop 9)（原版那份行实例的 rect）");
                                // 第 2 格：**同一排、x 步进 760**（`cellW 750 + spacing.x 10`）
                                var mSecond = NthChild(mContent, "Alliance Member Entry", 1);
                                CheckAtWorld(mSecond, 1129.67f, 1879.67f, 502.63f, 602.63f,
                                             "★ 第 2 格在**同一排右侧**（x = 369.67 + 750 + 10 = 1129.67，y 与第 1 格相同）");
                                float mTop0, mBot0, mTop1, mBot1;
                                CheckTrue(RowSpan(mContent, "Alliance Member Entry", CH2, out mTop0, out mBot0),
                                          "建出来的成员行都在");
                                CheckNear(mTop0, VTop + PT2, 0.5f, "最上面那行的顶边 = 视口顶 + padTop 9 = 502.63");
                                CheckTrue(mBot0 > VBot + 0.5f,
                                          $"…而最后一排**压在视口下沿上**（行底 {mBot0:F2} > 视口底 {VBot:F2}）"
                                          + "—— 下面「没有 quad 画到框外」的前提（不满足说明这条测试自己失效了）");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1125.17f, 786.84f) == ms,
                                          "视口中心（1125.17,786.84）上命中的滚动区**就是这一格**");
                                CheckTrue(pl3 != null && pl3.WheelAt(1125.17f, 786.84f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt`）");
                                CheckTrue(ms.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {ms.Offset:F2}px）");
                                RowSpan(mContent, "Alliance Member Entry", CH2, out mTop1, out mBot1);
                                CheckNear(mTop0 - mTop1, ms.Offset, 0.5f,
                                          "★ **滚动之后行真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                Check(CountChildren(mContent, "Alliance Member Entry"),
                                      CellsInViewport(NM, ColsM, PT2, CH2 + GY2, CH2, VTop, VBot, ms.Offset),
                                      "滚一格之后在建的格数 == 现算值（先清再建）");
                                var badM = QuadsOutside(mContent, new PxRect(369.67f, VTop, 1880.67f, VBot), qTol);
                                CheckTrue(badM.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（越界 " + badM.Count + " 颗"
                                          + (badM.Count > 0 ? "：" + string.Join(" / ", badM.ToArray()) : "") + "）");
                                ms.SetOffset(ms.MaxOffset);
                                var mNames = RowTextsOf(mContent, "Alliance Member Entry", "member name");
                                CheckTrue(!mNames.Contains("Member 1") && mNames.Contains("Member " + NM),
                                          "★ 滚到最下建的是**另一批**行（第 1 个滚出视口、第 " + NM + " 个进来；建出来的："
                                          + string.Join("/", mNames.ToArray()) + "）");
                                ms.SetOffset(0f);
                                // 🆕 2026-10-04（A35④）：**空表那一支的内容高** —— 原版那个 `GridLayoutGroup` 的
                                //   MinSize 在 0 子节点时 = `padding.vertical − spacing.y = (9 + 75) − 7.22 = **76.78**`
                                //   （与原版那份 **0 子节点** `Content` 的序列化 `sizeDelta.y` 逐值相同，
                                //   普查 `社交_联盟与好友页.md:232`，RT `-1825538911737290589`）
                                //   ⇒ 可滚范围 = `76.78 − 视口高 586.42` = **负数**（`ClampHi` 夹到 0 = 滚不动）。
                                //   🔴 这条**能真红**：把空表那一支改回 `0f` ⇒ 这里少 76.78（远超 0.5 的容差）。
                                //   ⚠️ 今天**看不见**（本地成员表恒空、这一支也走不到）—— 所以这是一条**口径断言**。
                                // 🔴 **2026-10-10（A269）就地订正 —— 原来这条量的是 `MaxOffset`、期望值是【负的】**
                                //   （`76.78 − 586.42` = **−509.64**）—— 那是 A269 之前「`MaxOffset` = 没经 `AdjustBounds`
                                //   调整过的裸值」那条口径。A269 起 `MaxOffset` 照 UGUI `AdjustBounds` 夹到 **≥ 0**
                                //   ⇒ 原来那条**会退化成恒真**（内容高 76.78 与 `0f` 都读 0）⇒ **改量内容高本身**，
                                //   判别力（「改回 `0f` ⇒ 少 76.78 ⇒ 红」）才还在。
                                SocialData.Members.Clear();
                                mt2.RebuildMembersForTest();
                                CheckNear(ms.ContentX2 - ms.ContentX1, 76.78f, 0.5f,
                                          "空表：内容高 = 原版 `GridLayoutGroup` 的 MinSize 值 **76.78**"
                                          + "（`(9+75) − 7.22`；与原版那份 0 子节点 `Content` 的序列化 "
                                          + "`sizeDelta.y` 相同）"
                                          + "（改坏法：空表那一支改回 `0f` ⇒ 这里少 76.78 ⇒ 红）");
                                CheckNear(ms.MaxOffset, 0f, 0.5f,
                                          "…而**可滚范围 = 0**（76.78 矮于视口 586.42 ⇒ 照 `AdjustBounds` 夹到 0，滚不动）");
                            }
                            SocialData.Members.Clear();
                            mt2.RebuildMembersForTest();
                            nmv.gameObject.SetActive(nmWas);
                            mv.gameObject.SetActive(false);   // 恢复：这一支本地**走不到**（出厂 act F）
                        }

                        // ------------------------------------------------ ④ 已入盟支的奖杯格（🆕 2026-10-04 A30）
                        //   ⚠️ 与 ③ 同一支（本地走不到）⇒ 自检手动点亮、把未入盟支关掉。
                        //   🔴 判据（普查 `社交_联盟与好友页.md:356-357` + 原始 JSON 实读
                        //   `MonoBehaviour_920958765198729379.json`（那个 `ScrollRect`）·
                        //   `MonoBehaviour_3577077230339809443.json`（`Item Drawer` 的 grid）·
                        //   `MonoBehaviour_-909385972349017949.json`（`ContentSizeFitter`，`m_VerticalFit = 1`）·
                        //   `_tmp_view/q1_rm2d.txt:45`（`RectMask2D.m_Softness = (0,50)`））：
                        //   `Scroll Rect` **366.97,378.51→1921.00,1079.84**（`m_Viewport` 指向它自己的 RT
                        //   ⇒ **它自己就是视口**）· `m_MovementType = 1`(Elastic) · grid cell **298×354** ·
                        //   spacing (0,**15**) · pad (左10,右10,上23,下0) · 视口宽 1554.03 ⇒ **5 列**。
                        {
                            const int NT = 12;
                            SocialData.AllianceTrophies = NT;      // ✅ 2026-10-04（A55④）起 `ResetForTest` **也清它**（下面那一句清理保留 = 双保险）
                            bool nmWas2 = nmv.gameObject.activeSelf;
                            nmv.gameObject.SetActive(false);
                            mv.gameObject.SetActive(true);
                            var mt3 = sw.PageAlliances.Member;
                            mt3.ShowTrophies();                   // `TrophiesWindow` 出厂 act F ⇒ 点 `Trophies` 才亮
                            mt3.RebuildTrophiesForTest();         // = 原版 `AllianceTrophiesView` 清空重填那条路
                            var ts = mt3.TrophyScroll;
                            CheckTrue(ts != null,
                                      "★ 奖杯那一格**有滚动区了**（原版 `TrophiesWindow>Scroll Rect` 的 `ScrollRect`；"
                                      + "改之前是一个空节点 —— 一处滚动都没有）");
                            var tScroll = FindChild(FindChild(mv, "TrophiesWindow"), "Scroll Rect");
                            CheckAtWorld(tScroll, 366.97f, 1921.00f, 378.51f, 1079.84f,
                                         "`Scroll Rect`（原版身上是 `ScrollRect + RectMask2D + Image`；**它自己就是视口** —— "
                                         + "`m_Viewport` 指向自己的 RT ⇒ 这一格没有另建 `Viewport` 子节点）");
                            var drawer = FindChild(tScroll, "Item Drawer");
                            CheckTrue(drawer != null, "`Item Drawer`（内容容器）在");
                            if (ts != null && drawer != null)
                            {
                                const float TVTop = 378.51f, TVBot = 1079.84f;   // = 原版 `Scroll Rect`（上面刚钉过）
                                // 原版那个 `GridLayoutGroup`（原始 JSON 实读，见上）：三个参数 + 列数式
                                const float TCellW = 298f, TCellH = 354f, TGX = 0f, TGY = 15f;
                                const float TPL = 10f, TPT = 23f;
                                const int TCols = 5;        // = `ColumnsFor(1554.03)`：Floor((1554.03−20+0.001)/298) = 5
                                Check(AllianceTrophyGrid.ColumnsFor(1554.03f), TCols,
                                      "★ 列数式（UGUI `GridLayoutGroup.cs:184`）：视口宽 1554.03 ⇒ **5 列**"
                                      + "（`(1554.03−20+0.001)/298 = 5.148`）");
                                Check(AllianceTrophyGrid.ColumnsFor(1900f), 6,
                                      "…视口宽 1900 ⇒ **6 列**（`(1900−20+0.001)/298 = 6.309`）—— 列数**真的在算**");
                                CheckNear(ts.Viewport.y1, TVTop, 0.5f,
                                          "滚动区视口 = `Scroll Rect` 那个节点自己的矩形（**没有另挑一个**）");
                                CheckTrue(ts.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(ts.Elastic,
                                          "**Elastic**（原版 `m_MovementType=1` —— ⛔ 不是 `BattleLogPopup` 那一档）");
                                // 内容高 = UGUI `GridLayoutGroup.cs:188` 的 MinSize（**独立复算**）
                                //   `= (padT+padB) + 排数×cellH + (排数−1)×spacing.y`；NT=12 / 5 列 ⇒ 3 排
                                int tRows = (NT + TCols - 1) / TCols;
                                float contentT = TPT + tRows * TCellH + (tRows - 1) * TGY;
                                CheckNear(ts.MaxOffset, contentT - (TVBot - TVTop), 0.5f,
                                          $"可滚范围 = 内容高（23 + {tRows}×354 + {tRows - 1}×15 = {contentT:F2}）"
                                          + $" − 视口高 {TVBot - TVTop:F2} —— **补之前这里恒 0**（滚轮全被夹回 0，"
                                          + "而「整格滚出视口 ⇒ 不建」会把第 2 排起彻底藏掉）");
                                CheckTrue(ts.MaxOffset > 1f, "★ 确实**滚得动**了");
                                int wantT = CellsInViewport(NT, TCols, TPT, TCellH + TGY, TCellH, TVTop, TVBot, 0f);
                                Check(CountChildren(drawer, "TrophyDisplay"), wantT,
                                      $"★ 喂 {NT} 个奖杯（{TCols} 列 ⇒ {tRows} 排）⇒ **恰好建了与视口相交的那"
                                      + $" {wantT} 格**（现算）—— 整格在视口外的连节点一起不建");
                                CheckTrue(wantT > 0 && wantT < NT,
                                          $"…而且**确实有整格落在视口外**（{NT - wantT} 格不建）");
                                // 格位：第 1 格 = 视口左上 + (padLeft 10, padTop 23)（= 原版那份 `TrophyDisplay`）
                                CheckAtWorld(NthChild(drawer, "TrophyDisplay", 0), 376.97f, 674.97f, 401.51f, 755.51f,
                                             "第 1 格 = 视口左上 + (padLeft 10, padTop 23)（原版那份实例的 rect）");
                                CheckAtWorld(NthChild(drawer, "TrophyDisplay", 1), 674.97f, 972.97f, 401.51f, 755.51f,
                                             "★ 第 2 格在**同一排右侧 +298**（原版 `spacing.x = 0` ⇒ 两格紧挨着）");
                                CheckAtWorld(NthChild(drawer, "TrophyDisplay", 5), 376.97f, 674.97f, 770.51f, 1124.51f,
                                             "★ 第 6 格 = **第 2 排**第 1 列（y 步进 369 = 354 + 15）");
                                // 两块 quad 位置的对照（软边断言的对照组 / 正组）：
                                //   · 第 1 格的进度条底（y 693.93..722.46）**整块在带外** ⇒ 角 alpha 不动
                                //   · 第 2 排第 1 格那条（y 1062.93..1091.46）**压在视口下沿的渐隐带** [1029.84,1079.84] 里
                                var qOut = QuadOf(FindChild(NthChild(drawer, "TrophyDisplay", 0), "Background"));
                                var qIn = QuadOf(FindChild(NthChild(drawer, "TrophyDisplay", 5), "Background"));
                                CheckTrue(qOut != null && qIn != null,
                                          "两块进度条底都建出来了（下面那条软边断言的对照组）");
                                CheckTrue(qOut != null && !AnyCornerAlphaBelow(qOut, 0.999f),
                                          "带外那块（第 1 排）四角 alpha **一点没动**（软边只压带内 —— 对照组）");
                                CheckTrue(qIn != null && AnyCornerAlphaBelow(qIn, 0.99f),
                                          "★ **软边真的接上了**：压在视口下沿那条 `(0,50)` 渐隐带里的件，"
                                          + "四角 alpha 被压过（实测最小角 alpha "
                                          + (qIn != null ? MinCornerAlpha(qIn).ToString("F3") : "—")
                                          + "）—— 拆掉 `clipSoftness` / `TrophyClipSoftness` 这一条立刻红");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1143.985f, 729.175f) == ts,
                                          "视口中心（1143.985,729.175）上命中的滚动区**就是这一格**");
                                // 🔴 两个视口**在屏幕上是重叠的**（成员列 369.67,493.63→1880.67,1080.05）
                                //   ⇒ 这一条同时验了 `Owner` 那条判据：`GeneralDetails` 已关（点 `Trophies` 切走），
                                //     它那一片**不该再吃滚轮命中**。
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1125.17f, 786.84f) != mt3.MemberScroll,
                                          "★ 成员列那一格**不再吃滚轮命中**（`Owner.activeInHierarchy` —— "
                                          + "两处视口在屏幕上重叠，全靠这条判据分开）");
                                // 🔴 **2026-10-04 首跑红了，就地订正：`tTop0` 必须在【滚之前】取** ——
                                //   它原来写在 `WheelAt` **之后** ⇒ 与 `tTop1` 量到的是同一批格 ⇒ 差恒 0.000
                                //   （自检当场报「0.000 ≈ 48.000」）。判据本身没错，是取的时机错了。
                                float tTop0, tBot0, tTop1, tBot1;
                                CheckTrue(RowSpan(drawer, "TrophyDisplay", TCellH, out tTop0, out tBot0),
                                          "建出来的格都在（量它们的位置，不量被裁过的 quad）");
                                CheckTrue(pl3 != null && pl3.WheelAt(1143.985f, 729.175f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt` —— 与真鼠标同一条路）");
                                CheckTrue(ts.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {ts.Offset:F2}px）");
                                // 滚一格之后在建的格数 == 现算值（**先清再建**，不清会越滚越多）
                                Check(CountChildren(drawer, "TrophyDisplay"),
                                      CellsInViewport(NT, TCols, TPT, TCellH + TGY, TCellH, TVTop, TVBot, ts.Offset),
                                      "滚一格之后在建的格数 == 现算值");
                                RowSpan(drawer, "TrophyDisplay", TCellH, out tTop1, out tBot1);
                                CheckNear(tTop0 - tTop1, ts.Offset, 0.5f,
                                          "★ **滚动之后格真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                var badT = QuadsOutside(drawer, new PxRect(366.97f, TVTop, 1921.00f, TVBot), qTol);
                                CheckTrue(badT.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（量的是**渲出来那块**：越界 " + badT.Count + " 颗"
                                          + (badT.Count > 0 ? "：" + string.Join(" / ", badT.ToArray()) : "")
                                          + "）—— 拆掉 `SetClip` 这一条立刻红"
                                          + "（⚠️ 软边会把件沿渐隐带内沿切成几块：**并集**仍要落在框内）");
                                ts.SetOffset(0f);
                                CheckNear(ts.MaxOffset, contentT - (TVBot - TVTop), 0.5f,
                                          "回到顶：可滚范围还是那个数（`SetOffset` 夹取没改内容高）");

                                // ============================================================ 🆕 2026-10-04（A55③）
                                // **奖杯格「本体」**（A30 只做了滚动区，一格里的件当时只有 `Progress` 那条）。
                                // 判据 = 普查 `社交_联盟与好友页.md:358-372`（`TrophyDisplay` 那一棵子树，
                                // 逐件的 rect / 图名 / 九宫 / 染色 / ppuMul 都在那 15 行里）。
                                // ⚠️ 格 0 的矩形 = `376.97,401.51→674.97,755.51`（= 视口左上 + (padL 10, padT 23)，
                                //   上面 `CheckAtWorld(NthChild(drawer,"TrophyDisplay",0), …)` 刚钉过同一个数）。
                                var cell0 = NthChild(drawer, "TrophyDisplay", 0);
                                CheckTrue(cell0 != null, "第 1 格在（下面断它身上的件）");
                                if (cell0 != null)
                                {
                                    // ---- `bg`：格子底板（`UI_Deck_Selection_Back_simple` 440×656 · 九宫 197,0,199,0）
                                    var cbg = FindChild(cell0, "bg");
                                    var cbgQ = QuadOf(cbg);
                                    CheckTrue(cbgQ != null && cbgQ.Texture != null
                                              && cbgQ.Texture.name == "UI_Deck_Selection_Back_simple",
                                              "★ `bg` 用的是 `UI_Deck_Selection_Back_simple`（普查 `:360`）"
                                              + "—— 改之前这一格**连底板都没有**");
                                    CheckAtWorld(cbg, 376.97f, 674.97f, 401.51f, 755.51f,
                                                 "`bg` = 整格（`(0,0)→(1,1)` · sizeDelta ≈ 0）");

                                    // ---- `Collectable Highlight`（act **F** · 八角描边 · `fillCenter=0`）
                                    var ch = FindChild(cell0, "Collectable Highlight");
                                    CheckTrue(ch != null && !ch.gameObject.activeSelf,
                                              "★ `Collectable Highlight` **建了但关着**（原版出厂 act F；"
                                              + "`AllianceTrophyEntry.Initialize` 第一句就是 "
                                              + "`highlight.SetActive(false)`；"
                                              + "🔴 点亮它的**只有 `IsFeatured` 那一枚** —— "
                                              + "`AllianceTrophiesView.Draw` 只在 `AllianceTrophy.IsFeatured` 为真时调 "
                                              + "`ToggleSelected`，而 `ToggleSelected` 才是把它打开的那一处"
                                              + "（反编译 `AllianceTrophiesView__Draw.c:192-196` / `…__ToggleSelected.c`））");
                                    //   矩形**比格子大一圈**（`(0,0)→(1,1)` + sizeDelta 62.46×72.74 ⇒ 四周外扩）
                                    CheckAtWorld(ch, 345.90f, 706.36f, 360.13f, 786.87f,
                                                 "`Collectable Highlight` 的矩形（四周各溢出一圈，原版如此）");
                                    // 🔴 **取法要显式含未激活**：这一整颗是 `SetActive(false)` 的
                                    //   （`AllianceMemberTab.cs` 里 `hl.SetActive(false)`）⇒ 它名下那 8 块角块
                                    //   **全部** `activeInHierarchy == false`；而 `QuadOf` 走的
                                    //   `GetComponentInChildren<ImageQuad>()`（不带 `true`）对「整棵关着」这种
                                    //   情形**语义没写死**（X5 审查 §断言 #26 也标了「定不了」）⇒ 换
                                    //   `QuadOfInactiveToo`（显式 `(true)` = 「含未激活」是有定义的那一种）。
                                    //   ⚠️ 这里要问的是「**建出来了没有**」，⛔ 不是「现在在不在渲」。
                                    var chQ = QuadOfInactiveToo(ch);
                                    CheckTrue(chQ != null && chQ.Texture != null
                                              && chQ.Texture.name == "OctagonUI_Border_SDF",
                                              "★ 描边图 = `OctagonUI_Border_SDF`（源码切片名 `OctagonUI Border SDF`；"
                                              + "⚠️ 库里另有一张 `…_2` = `OctagonUI Border SDF 2`，"
                                              + "日常奖励窗那处用的才是它 —— 别混）");
                                    CheckTintOn(chQ, new Color(1f, 0.545f, 0f, 1f), 0.01f,
                                                "描边染色 `(1,0.545,0,1)`（普查 `:359`）");
                                    // `m_FillCenter = 0` ⇒ 九宫的**中格那块根本不建**（`ImageQuad.CreateNineSlice`
                                    //   给三×三的每一块起的名字是 `<根名>_<i><j>`）⇒ 断 `…_11` 不在、`…_00` 在。
                                    // 🔴 **⛔ 别去数 quad 总数**：`ApplySoftEdges` 会把压在渐隐带上的块**再切成几块**
                                    //   （子块名 `…_soft<i><j>`，见 `MenuDraw.ApplySoftEdges:349-358`）——
                                    //   那个数随裁切带变化，不是「原版有没有这一块」的判据。
                                    CheckTrue(FindChild(ch, "Collectable Highlight_00") != null
                                              && FindChild(ch, "Collectable Highlight_11") == null,
                                              "★ `fillCenter=0` ⇒ 九宫的**中格不建**（`…_11` 那块没有）、"
                                              + "四角那块在（`…_00`）—— 原版 `m_FillCenter: 0`");

                                    // ---- `BadgeDrawer`（那一枚盟徽；图在服务器 ⇒ 只有 Frame + Badge 两个空节点）
                                    var cbd = FindChild(cell0, "BadgeDrawer");
                                    CheckAtWorld(cbd, 410.95f, 641.00f, 408.49f, 638.54f,
                                                 "`BadgeDrawer`（普查 `:361`）");
                                    CheckTrue(FindChild(cbd, "Frame") != null && FindChild(cbd, "Badge") != null,
                                              "`BadgeDrawer` 下 `Frame` / `Badge` 两个节点都在（照原版结构）");

                                    // ---- `title`（奖杯名：**服务器数据 ⇒ 留白**，但节点/字号/对齐照建）
                                    var cttl = FindChild(cell0, "title");
                                    CheckAtWorld(cttl, 400.91f, 649.97f, 638.54f, 679.34f,
                                                 "`title`（普查 `:364`）");
                                    CheckText(TextOf(cttl), "",
                                              "`title` 的**字留空**（奖杯名在服务器；⛔ 没拿 prefab 的样例串 "
                                              + "`Trophy Name ` 充数 —— 一格一个名字会被当成真数据）");

                                    // ---- 进度条：`Progress > ProgressBar > { Background > Fill Area > Fill > end,
                                    //      Outline, counter }`（⚠️ 缩进照普查 `:365-372`：`Outline`/`counter`
                                    //      与 `Background` **同层**，都挂在 `ProgressBar` 下）
                                    var cpr = FindChild(cell0, "Progress");
                                    CheckAtWorld(cpr, 401.97f, 649.97f, 700.58f, 726.05f, "`Progress`（普查 `:365`）");
                                    var cbar = FindChild(cell0, "ProgressBar");
                                    CheckAtWorld(cbar, 400.72f, 649.97f, 684.41f, 731.97f,
                                                 "`ProgressBar`（那是个 `Slider`，普查 `:366`）");
                                    var cback = FindChild(cbar, "Background");
                                    var cbackQ = QuadOf(cback);
                                    CheckTrue(cbackQ != null && cbackQ.Texture != null
                                              && cbackQ.Texture.name == "40k_campaign_bar_bg",
                                              "`Background` = `40k_campaign_bar_bg` 九宫 `(20,0,20,0)`");
                                    // 🔴 判「它**不**直接挂在 `Progress` 下」只能用**直接子节点**那支：
                                    //   `FindChild` 走整棵子树（`parent.GetComponentsInChildren<Transform>(true)`）
                                    //   ⇒ `FindChild(cpr,"counter")` 必然找得到（`ProgressBar` 是 `Progress` 的子、
                                    //   `counter` 又是 `ProgressBar` 的子）⇒ 那样写**恒假**（A55 那版就是这么红的）。
                                    CheckTrue(NthChild(cbar, "counter", 0) != null
                                              && NthChild(cpr, "counter", 0) == null,
                                              "★ `counter` 挂在 **`ProgressBar`** 下（普查 `:372` 的缩进 = 11 级，"
                                              + "与 `Background`/`Outline` 同层）—— A30 那版借挂在 `Progress` 上了，本批订正");
                                    CheckAtWorld(NthChild(cbar, "counter", 0), 414.56f, 638.58f, 696.38f, 721.94f,
                                                 "`counter`（原版 `Center/Middle` · 字号 26.95）");
                                    CheckText(TextOf(NthChild(cbar, "counter", 0)), "0/0",
                                              "`counter` 的**字**（进度在服务器 ⇒ 按零值摆）");

                                    // `Fill`：原版出厂 **宽 0**（`Slider` 的 value 系列化就是 0）
                                    //   ⇒ `MenuDraw.ClipRect:75` 对退化矩形（有裁切时 `W ≤ 0.01` / `H ≤ 0.01`）
                                    //     判「不可见」⇒ `MenuDraw.Nine` **连 quad 都不建**、退回一个纯占位节点。
                                    var cfill = FindChild(cell0, "Fill");
                                    CheckAtWorld(cfill, 400.72f, 400.72f, 696.35f, 720.04f,
                                                 "`Fill`（原版出厂 **零宽** —— 进度 0）");
                                    var cend = FindChild(cfill, "end");   // `Fill` **唯一**的子件（原版树就是 `Fill>end`）
                                    // 🔴 **只数 `Fill` 自己画的那些**：`end` 是挂在它**下面**的（原版树如此），
                                    //   数整棵子树会把 `end` 那 1 块算进来 —— A55 那一版就是这么写的（`GetComponentsInChildren`
                                    //   数到 1），而它想说的是「**`Fill` 自己**一块都没画」⇒ **是断言的前提写错了**，
                                    //   不是「有人多画了一块」（零宽那一支只剩 `Node()` 建的裸节点、没有任何渲染件）。
                                    //   ⚠️ 节点本身不在时给 **-1**（不拿 0 顶过去 —— 那会变成**空转断言**）。
                                    int fillOwn = cfill != null ? 0 : -1;
                                    if (cfill != null)
                                        foreach (var q in cfill.GetComponentsInChildren<ImageQuad>(true))
                                            if (q != null && q.transform != cend
                                                && (cend == null || !q.transform.IsChildOf(cend))) fillOwn++;
                                    Check(fillOwn, 0,
                                          "★ 零宽 ⇒ `Fill` **自己**一块 quad 都没画（不是画成一条线；"
                                          + "⚠️ 挂在它下面的 `end` 不算 —— 那一块是端帽、原版就有，"
                                          + "下面那条断言会证明它**确实画了**）");

                                    // `end`（端帽）—— `Simple` + **preserveAspect**（22×18 塞进 29.44×31.86）
                                    //   ⇒ 画出来是 29.44×**24.09**（上下各让 3.886）。
                                    var cendQ = QuadOf(cend);
                                    CheckTrue(cendQ != null && cendQ.Texture != null
                                              && cendQ.Texture.name == "40k_campaign_bar_end",
                                              "`end` = `40k_campaign_bar_end`（`Simple` · preserveAspect）");
                                    CheckAtWorld(cend, 376.98f, 406.42f, 697.1464f, 721.2336f,
                                                 "`end` 的位置（x 照普查 `:370`；y 是 **preserveAspect 之后**的"
                                                 + " 24.087 高 —— 22×18 塞进 29.44×31.86）");
                                    CheckTint(cend, new Color(1f, 1f, 1f, 0.698f), 0.01f,
                                              "`end` 的染色（**α 0.698**，普查 `:370`）");

                                    // `Outline`（那一圈描边）
                                    var cout = FindChild(cbar, "Outline");
                                    var coutQ = QuadOf(cout);
                                    CheckTrue(coutQ != null && coutQ.Texture != null
                                              && coutQ.Texture.name == "40k_campaign_bar_outline",
                                              "`Outline` = `40k_campaign_bar_outline` 九宫 `(20,0,20,0)`");
                                    CheckTint(cout, new Color(1f, 0.841f, 0f, 1f), 0.01f,
                                              "`Outline` 的染色 `(1,0.841,0,1)`（普查 `:371`）");

                                    // ---- 🔴 **层序 = 原版兄弟序**（判据 = **原始 JSON 的 `m_Children`**；
                                    //   ⛔ 不是「按种类」派队列 —— A55 那版就是按种类派的，两处正好反了）
                                    //   ① `bundle_menus_assets_all/RectTransform/RectTransform_-3094581417016303453.json`
                                    //      （`TrophyDisplay`）的 `m_Children` =
                                    //      `[Collectable Highlight, bg, BadgeDrawer, title, Progress]`
                                    //      —— uGUI 按兄弟序画（**后面的压前面的**）⇒ `bg` 压 `Collectable Highlight`；
                                    //   ② `…/RectTransform_8075696931376455843.json`（`ProgressBar`）的 `m_Children` =
                                    //      `[Background, Outline, counter]`，而 `end` 是
                                    //      `…/RectTransform_-1375540381073612637.json`（`Fill`）**唯一**的子
                                    //      ⇒ `Outline` 压 `end`（两者重叠 ≈ 5.7px）。
                                    //   ⚠️ 断的是**运行时队列号**（`ImageQuad.RenderQueue` / `Label.RenderQueue`）：
                                    //      「谁压谁」就是这两个数的大小关系。⛔ 不拿我们自己的队列常量当期望值
                                    //      （那是同式自证 —— 常量改错也照样绿）。
                                    {
                                        int QR(ImageQuad q) { return q != null ? q.RenderQueue : -1; }
                                        int QL(Transform t)
                                        {
                                            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
                                            return lb != null ? lb.RenderQueue : -1;
                                        }
                                        var qRing = QR(QuadOfInactiveToo(ch));   // 那颗是关着的 ⇒ 用「含未激活」的取法
                                        var qBack = QR(QuadOf(cbg));
                                        var qSlot = QR(cbackQ);
                                        var qEndQ = QR(cendQ);
                                        var qFrame = QR(coutQ);
                                        var qTitleQ = QL(cttl);
                                        var qCounterQ = QL(NthChild(cbar, "counter", 0));
                                        CheckTrue(qRing > 0 && qBack > 0 && qSlot > 0 && qEndQ > 0
                                                  && qFrame > 0 && qTitleQ > 0 && qCounterQ > 0,
                                                  "层序断言的前提：七件**都取到了队列号**（取不到一律给 -1）"
                                                  + $"—— 实得 ring={qRing} bg={qBack} 槽底={qSlot} end={qEndQ} "
                                                  + $"描边={qFrame} title={qTitleQ} counter={qCounterQ}");
                                        CheckTrue(qBack > qRing,
                                                  "★ **`bg` 压 `Collectable Highlight`**（原版 `TrophyDisplay` 的 "
                                                  + "`m_Children` 第一颗是 highlight、第二颗是 bg ⇒ 后画的压先画的；"
                                                  + "那圈描边只该露在格子**外**沿）"
                                                  + $"—— 实得 ring={qRing} < bg={qBack}"
                                                  + "（改之前 `hl=L_Art(2) > bg=L_Panel(0)`，**正好反的**）");
                                        CheckTrue(qSlot > qBack && qEndQ > qSlot && qFrame > qEndQ,
                                                  "★ **`Outline` 压 `end`**（`ProgressBar` 的 `m_Children` = "
                                                  + "`[Background, Outline, counter]`，而 `end` 是 `Fill` 唯一的子 ⇒ "
                                                  + "格子底板 < 槽底 < `end` < 描边）"
                                                  + $"—— 实得 bg={qBack} < 槽底={qSlot} < end={qEndQ} < 描边={qFrame}");
                                        CheckTrue(qCounterQ > qFrame,
                                                  "★ `counter` 的字**压在 `Outline` 之上**"
                                                  + "（`counter` 是 `ProgressBar` 的**最后一颗**子件 ⇒ 后画的压先画的）"
                                                  + $"—— 实得 描边={qFrame} < counter={qCounterQ}");
                                        CheckTrue(qTitleQ > qBack,
                                                  "`title` **压在格子底板 `bg` 之上**（`TrophyDisplay` 里它在 `bg` 之后 —— "
                                                  + "两者**是重叠的**：字摆在格子里）"
                                                  + $"—— 实得 bg={qBack} < title={qTitleQ}"
                                                  + "。⚠️ **它与进度条那一叠的相对次序不断**：原版 `title` 是 "
                                                  + "`Progress` **之前**的一颗（画在进度条**下面**），可两者**不重叠**"
                                                  + "（title 底边 277.83 < 进度条顶边 282.90）⇒ 那一段看不见、也不该硬断");
                                    }

                                    // ---- 「点格子开 `trophyInfoPopup`」那条命中路
                                    var chit = FindChild(cell0, "Hit");
                                    var chw = chit != null ? chit.GetComponent<WindowButton>() : null;
                                    CheckTrue(chw != null && chw.onClick != null,
                                              "★ 格子上**接了点击**（原版 `AllianceTrophyEntry.backgroundButton` "
                                              + "→ `HandleClick` → `AllianceTrophiesView.HandleTrophyClick` "
                                              + "→ `WindowsManager.OpenWindow(trophyInfoPopup, …)`）"
                                              + "—— 🆕 2026-10-04（A70）**那个弹窗已经建出来了**（`Shell/TrophyInfoPopup.cs`），"
                                              + "下面 ④c 会真点它一下把窗开出来");

                                    // ============================================================ ④b
                                    // 🆕 2026-10-04（**A74②**）：队列号**按格号错开** —— 原版是「后画的整格压先画的整格」。
                                    //   判据 = ① 原始 `m_Children` 兄弟序（每格 `TrophyDisplay` 是按序 Instantiate 的兄弟，
                                    //            uGUI 按兄弟序**整棵整棵**地画）+ ② **零间距**网格
                                    //            （`AllianceTrophyGrid.GapX = 0` ⇒ 两格紧挨着）
                                    //        ⇒ `Collectable Highlight` 比格子大一圈（−31.07 / −41.38 / +31.39 / +31.36），
                                    //          它的**左/上那一圈**该压在左邻 / 上邻身上（原版：**后画的整格**赢），
                                    //          **右/下那一圈**被右邻 / 下邻压住。
                                    //   ⛔ 改之前是「同类同号」⇒ 第 2 格的描边（3204）反被第 1 格的底板（3205）压住 = **反的**。
                                    //   ⚠️ 取队列时**排除 `Hit` 子树**：它是**透明命中区**（α=0），队列只决定
                                    //      **点击优先级**、不参与「谁盖谁」（原版那条「整棵压整棵」只在渲染件上成立）。
                                    {
                                        int BandQ(Transform cell, bool wantMax)
                                        {
                                            int r = wantMax ? -1 : int.MaxValue;
                                            if (cell == null) return r;
                                            var hit = cell.Find("Hit");
                                            foreach (var q in cell.GetComponentsInChildren<ImageQuad>(true))
                                            {
                                                if (q == null || (hit != null && q.transform.IsChildOf(hit))) continue;
                                                r = wantMax ? Mathf.Max(r, q.RenderQueue) : Mathf.Min(r, q.RenderQueue);
                                            }
                                            foreach (var l in cell.GetComponentsInChildren<Label>(true))
                                                if (l != null) r = wantMax ? Mathf.Max(r, l.RenderQueue) : Mathf.Min(r, l.RenderQueue);
                                            return r;
                                        }
                                        var cA = NthChild(drawer, "TrophyDisplay", 0);
                                        var cB = NthChild(drawer, "TrophyDisplay", 1);
                                        int aMax = BandQ(cA, true), bMin = BandQ(cB, false), bMax = BandQ(cB, true);
                                        CheckTrue(cA != null && cB != null && aMax > 0 && bMin > 0 && bMax > 0,
                                                  "层序断言的前提：前两格都取到了队列号（取不到一律给 -1）"
                                                  + $"—— 实得 第1格 max={aMax} · 第2格 min={bMin} / max={bMax}");
                                        CheckTrue(bMin > aMax,
                                                  "★ **按格号错开**：第 2 格的**每一件**都压在第 1 格的**每一件**之上"
                                                  + "（原版每格是按序 Instantiate 的兄弟 ⇒ 后画的整格压先画的整格）"
                                                  + $"—— 实得 第1格 max={aMax} < 第2格 min={bMin}；"
                                                  + "改回「同类同号」这一条立刻红");
                                        var cRingB = QuadOfInactiveToo(FindChild(cB, "Collectable Highlight"));
                                        var cBackA = QuadOf(FindChild(cA, "bg"));
                                        int rqB = cRingB != null ? cRingB.RenderQueue : -1;
                                        int bqA = cBackA != null ? cBackA.RenderQueue : -1;
                                        CheckTrue(rqB > 0 && bqA > 0 && rqB > bqA,
                                                  "★ …而**看得见的那一对**（零间距下真正重叠的一对）也对："
                                                  + "第 2 格的 `Collectable Highlight` 压在**第 1 格的底板**上"
                                                  + $"—— 实得 ring(第2格)={rqB} > bg(第1格)={bqA}");
                                        CheckTrue(bMin >= 3210 && bMax <= 3299,
                                                  "★ 每一格都落在它自己的带子里（**3210–3299** = 页带 3209 之后、聊天窗 3300 之前）"
                                                  + $"—— 实得 第2格 [{bMin},{bMax}]");
                                        int builtT = CountChildren(drawer, "TrophyDisplay");
                                        CheckTrue(builtT > 0 && builtT <= 15,
                                                  $"建出来的格数落在带子的容量里（≤ 15 = 5 列 × 3 排：视口高 701.33 ⇒ "
                                                  + $"最多 3 排；实得 {builtT}）—— 这一带只有 90 个号、一格 6 个");
                                    }

                                    // ============================================================ ④c
                                    // 🆕 2026-10-04（**A70**）：点格子开的那扇 **`TrophyInfoPopup`** —— 本批**建出来了**。
                                    //   判据 = 原版 prefab `Alliance Trophy Info Popup`（`menu_dump …--md --depth 12` 逐节点）
                                    //        + MB `-3823665489305576323`（窗参 + 七个字段的 pid）+ 反编译
                                    //        `TrophyInfoPopup__{Open,HandleFeatureTrophy}.c`。
                                    //   ⚠️ 这一扇**本地走不到**（奖杯数恒 0 ⇒ 没有可点的格子）⇒ 自检手动造出格子再点它。
                                    {
                                        TrophyInfoPopup popT = null;
                                        bool clicked = pl3 != null && pl3.ClickAt(525.97f, 578.51f);   // = 第 1 格中心
                                        popT = TrophyInfoPopup.LastOpened;
                                        CheckTrue(clicked && popT != null && popT.CurrentState == WindowState.Open,
                                                  "★ 点奖杯格 ⇒ **弹窗真的开出来了**（原版 `AllianceTrophiesView.HandleTrophyClick` "
                                                  + "→ `WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`）");
                                        if (popT != null)
                                        {
                                            CheckTrue(popT.type == WindowType.Popup && popT.placement == WindowsPlacement.Popup
                                                      && popT.closeOnEsc
                                                      && Mathf.Abs(popT.extraScaleSmallScreen - 1f) < 1e-6f,
                                                      "窗参照原版 MB 实读：`type=1`(Popup) · `windowsPlacement=15` · "
                                                      + "`closeOnESC=1` · `extraScaleSmallScreen=1.0`");
                                            var pRoot = popT.transform;
                                            var pShade = FindChild(pRoot, "Menu Dark Background");
                                            CheckAtWorld(pShade, -1327.30f, 3247.30f, -746.18f, 1826.18f,
                                                         "压暗整屏（全库统一那个矩形）");
                                            CheckTint(pShade, new Color(0f, 0f, 0f, 0.773f), 0.01f,
                                                      "压暗色 `(0,0,0,0.773)`（dump 实读）");
                                            CheckAtWorld(FindChild(pRoot, "window"), 395.72f, 1524.28f, 188.35f, 851.65f,
                                                         "`window`（dump 实读）");
                                            var pWb = FindChild(pRoot, "Generic Window Red Background Big");
                                            CheckAtWorld(pWb, 395.70f, 1547.30f, 178.35f, 895.80f,
                                                         "面板底（`UI_Deck_Information_Back` · 九宫 `(42,363,655,81)`）");
                                            var pWbQ = QuadOf(pWb);
                                            CheckTrue(pWbQ != null && pWbQ.Texture != null
                                                      && pWbQ.Texture.name == "UI_Deck_Information_Back",
                                                      "…那张图 = `UI_Deck_Information_Back`"
                                                      + "（与 `BattleLogPopup` 的面板同一张图、同一组 border）");

                                            // ---- 徽标（图在服务器 ⇒ 只建节点）----
                                            var pBd = FindChild(pRoot, "BadgeDrawer");
                                            CheckAtWorld(pBd, 435.87f, 944.13f, 267.52f, 744.48f, "`BadgeDrawer`");
                                            CheckTrue(FindChild(pBd, "Frame") != null && FindChild(pBd, "Badge") != null,
                                                      "└ `Frame` / `Badge` 两个节点都在（照原版结构）");
                                            CheckTrue(pBd != null && pBd.GetComponentsInChildren<ImageQuad>(true).Length == 0,
                                                      "★ 但**一块 quad 都没有** —— 徽标图要服务器来的 `GroupBadge`"
                                                      + "（`AllianceBadgeDrawer.Draw(GroupBadge)`），本地没有 ⇒ 不画占位图");

                                            // ---- 关窗钮（含悬停/按下换图）----
                                            var pCb = FindChild(pRoot, "Generic Close Button Orange");
                                            CheckAtWorld(pCb, 1487.08f, 1561.47f, 159.85f, 235.45f,
                                                         "关窗钮（圆底 `UI_Button_Round_background` · Simple preserveAspect）");
                                            CheckAtWorld(FindChild(pCb, "Background"), 1495.24f, 1552.10f, 167.83f, 225.96f,
                                                         "└ `Background`（`40k_general_bt_yellow`）");
                                            var pIco = FindChild(pCb, "Icon");
                                            CheckAtWorld(pIco, 1495.24f, 1552.10f, 167.83f, 225.96f,
                                                         "└ `Icon`（与 `Background` **同矩形**）");
                                            var pIcoQ = QuadOf(pIco);
                                            CheckTrue(pIcoQ != null && pIcoQ.Texture != null
                                                      && pIcoQ.Texture.name == "40k_general_bt_yellow_close",
                                                      "…图标 = `40k_general_bt_yellow_close`");
                                            CheckHoverSwap(pRoot, "`TrophyInfoPopup` 的悬停/按下换图");
                                            // ⚠️ **别在这儿调 `CheckNoMissingSwapArt`**：它查的是**全局静态表**
                                            //    `WindowButton.MissingSwapArt`（本文件前面几段悬停过那么多钮，
                                            //    别人的缺失会算到这一条头上）⇒ 这里改成**本地**查这两张图在不在
                                            //    （= dump 实读的 `HL` / `P`，也是「悬停换得动」的前提）。
                                            CheckTrue(CardArt.MenuUi("40k_general_bt_yellow_hover") != null
                                                      && CardArt.MenuUi("40k_general_bt_yellow_pressed") != null,
                                                      "关窗钮那两张换图**都在**（`40k_general_bt_yellow_hover` / "
                                                      + "`40k_general_bt_yellow_pressed` —— dump 的 `HL=` / `P=` 那两列实读）");
                                            // ⚠️ **本窗自己的缺图表**（不是全局那一个）：`MenuDraw.Rect/Nine` 对
                                            //    `tex == null` 是**静默返回 null**（那一件连节点都不会建）⇒ 本窗
                                            //    改成「谁取谁报」并把清单开出来给自检（同 `sw.MissingArt` 那条口径）。
                                            Check(popT.MissingArt.Count, 0,
                                                  "本窗用到的图**一张都不缺**（缺的会列在这里："
                                                  + string.Join("、", popT.MissingArt.ToArray())
                                                  + " —— 取不到的件**根本没建**，所以这条必须 0）");

                                            // ---- `RightSide`：标题 / 描述 / 下一级 ----
                                            // ⚠️ `Next Tier` **不在 `RightSide` 的直系子里**（它在 `Controls` › `Progress` 下，
                                            //    见下面「进度条那一叠」那节的直系子断言）—— 这里只是**同一次走查**顺手量它，
                                            //    用 `FindChild(pRs, …)` 是因为**整棵子树**够得着（`pRs` 是它的祖先）。
                                            var pRs = FindChild(pRoot, "RightSide");
                                            CheckAtWorld(pRs, 960.00f, 1508.28f, 204.35f, 835.65f, "`RightSide`");
                                            var pTtl = FindChild(pRs, "Title");
                                            CheckAtWorld(pTtl, 976.12f, 1492.84f, 307.85f, 359.85f,
                                                         "`Title`（奖杯名）的**框**；零值态（空串）时文字块停在**框心**——"
                                                         + "`Label.AlignLeftOn` 对量不出宽度的空串**不挪位置**"
                                                         + "（守卫见 `Label.HasMeasuredWidth`；不挡的话空串的 `WorldW` 是 TMP 的"
                                                         + "未定义值 4.29e9 ⇒ 节点会被扔到 2.1e9 世界单位外），有数据时 `Apply` 再按左沿对齐");
                                            CheckText(TextOf(pTtl), "",
                                                      "`Title` 的字**留空**（奖杯名在服务器；⛔ 没拿 prefab 的样例串 `Trophy Name` 充数）");
                                            CheckTrue(FindChild(pRs, "Category") == null,
                                                      "★ `Category`（`Sub-title` 那一条）**不建** —— 出厂 `act = F`（判据③）");
                                            var pDesc = FindChild(pRs, "Descripton");
                                            CheckAtWorld(pDesc, 976.00f, 1477.24f, 352.85f, 532.89f,
                                                         "`Descripton`（⚠️ 原版就这么拼）的框；零值态同 `Title` —— 停在框心");
                                            var pNext = FindChild(pRs, "Next Tier");
                                            // 🔴 **左对齐的文字块不能拿「节点在框心」当期望**：`MenuDraw.AlignLeft` 按
                                            //    **渲染宽度**把节点挪到「文字左边缘 = 框左沿」（`Label.AlignLeftOn`）
                                            //    ⇒ 节点中心 = 框左沿 + 半个字宽（这条原来就是**这么红的**：差 190.74px，
                                            //    正是「Next Tier:」在 35px 下的一半宽）。要钉的是**渲出来那块**的左沿。
                                            float ntx1, nty1, ntx2, nty2;
                                            CheckTrue(RenderedRect(pNext, out ntx1, out nty1, out ntx2, out nty2),
                                                      "`Next Tier` 渲出来了（下面量它的左沿与中线）");
                                            if (RenderedRect(pNext, out ntx1, out nty1, out ntx2, out nty2))
                                            {
                                                CheckNear(ntx1, 976.12f, 1.0f,
                                                          "★ `Next Tier` 的**文字左边缘** = 框左沿 `976.12`"
                                                          + "（原版这颗 `m_HorizontalAlignment = Left`）");
                                                CheckNear((nty1 + nty2) * 0.5f, 550.92f, 1.0f,
                                                          "…纵向中线 = 框的中线（`(532.80+569.04)/2 = 550.92`）");
                                            }
                                            CheckText(TextOf(pNext), "Next Tier:",
                                                      "…字 = prefab 里的**字面串**（它身上带 `Localize` ⇒ 原版走 I2 词条，"
                                                      + "而词条表在远端 CCD ⇒ 只能照抄那个串本身）");

                                            // ---- 进度条那一叠 ----
                                            var pProg = FindChild(pRs, "Progress");
                                            CheckAtWorld(pProg, 976.12f, 1477.12f, 569.04f, 623.46f,
                                                         "`Progress`（= `progressHolder` 字段指的那一颗）");
                                            CheckTrue(pProg != null && pProg.gameObject.activeSelf,
                                                      "零值态下它是**亮的**（原版 `progressHolder.SetActive(!IsFilled && !DontShowProgress)`"
                                                      + " 两个输入都在服务器 ⇒ 走 prefab 出厂态）");
                                            // 🆕 2026-10-05：`Next Tier` 的**父**（原版 `m_Children` 实读：
                                            //    `Progress`(rt -4716729376017064835) 的子 = [`ProgressBar`, `Next Tier`]）。
                                            // 🔴 **⛔ 这一条不能用 `FindChild` 写** —— 它走 `GetComponentsInChildren`
                                            //    （**整棵子树**，定义见本文件下方的 `FindChild`）⇒ 无论 `Next Tier`
                                            //    挂在 `RightSide` 还是 `Progress` 下，上面那句 `FindChild(pRs, "Next Tier")`
                                            //    **都非 null**。这正是这个父错能一路活下来的原因：**两件事叠加**
                                            //    —— ① `FindChild` 整棵子树能捞到、② 换父世界矩形逐位不变
                                            //    （`MenuDraw.Local` = `RectCenter − parent.position`；
                                            //     `Label.AlignLeftOn` 也是「世界 x − 父的世界 x」）⇒ **没有一条断言会红**。
                                            //    ⇒ 判据只能是**直系子**（`NthChild` 走 `parent.childCount` / `GetChild(i)`）。
                                            // 🔴 **两条都要**，只断前一半是**弱断言**：「`Progress` 底下有」这种写法
                                            //    挂回 `RightSide` 也照样绿 —— 因为 `Progress` 本身就在 `RightSide`
                                            //    的子树里（`RightSide` › `Controls` › `Progress`）。必须同时断
                                            //    「`RightSide` / `Controls` 的**直系子**里没有」。
                                            //    ⚠️ 期望值**从建出来的场景树里取**（`FindChild(pRs, …)`），
                                            //    ⛔ **不拿 `TrophyInfoPopup` 里的 `_progressHolder` 变量当期望**（那是自证）。
                                            CheckTrue(NthChild(pProg, "Next Tier", 0) != null
                                                      && NthChild(pRs, "Next Tier", 0) == null
                                                      && NthChild(FindChild(pRs, "Controls"), "Next Tier", 0) == null,
                                                      "★ `Next Tier` 挂在 **`Progress`** 下（**直系子**）—— 原版 `Progress` 的 "
                                                      + "`m_Children` = [`ProgressBar`, `Next Tier`]；⛔ 它**不在** `RightSide` / "
                                                      + "`Controls` 的直系子里（回到「父 = `RightSide`」那一态时这条会红）。"
                                                      + "⚠️ `FindChild` 走整棵子树 ⇒ **看不见**这个错，别拿它当判据");
                                            // 🔴 兄弟序也要钉住：原版 `Next Tier` 是 `Progress` 的**最后一个**子
                                            //    （排在 `ProgressBar` 之后）。⚠️ 我们这边的画序由**渲染队列**决定、
                                            //    不看兄弟序（见 `TrophyInfoPopup` 文件头）⇒ 这条**不代替**队列判据，
                                            //    它盯的是**结构**：别把 `Next Tier` 插到 `ProgressBar` 前面去
                                            //    （那会把原版 `Progress` 那一支的兄弟序打乱）。
                                            CheckTrue(pProg != null && pProg.childCount == 2
                                                      && pProg.GetChild(pProg.childCount - 1).name == "Next Tier",
                                                      "…而且是 `Progress` 的**最后一个**子（兄弟序 = [`ProgressBar`, `Next Tier`]，"
                                                      + "与原始 `m_Children` 逐位一致）");

                                            var pBar = FindChild(pProg, "ProgressBar");
                                            CheckAtWorld(pBar, 976.12f, 1477.12f, 560.78f, 629.38f,
                                                         "`ProgressBar`（原版是 `Slider` + `ProgressBar`，出厂 `value = 0`）");
                                            var pBarBg = FindChild(pBar, "Background");
                                            var pBarBgQ = QuadOf(pBarBg);
                                            CheckTrue(pBarBgQ != null && pBarBgQ.Texture != null
                                                      && pBarBgQ.Texture.name == "40k_campaign_bar_bg",
                                                      "`Background` = `40k_campaign_bar_bg` 九宫 `(20,0,20,0)` · `ppuMul 0.9`");
                                            // 🔴 直接子节点判据（`FindChild` 走整棵子树 ⇒ 那样写恒真 —— A73① 那条教训）
                                            CheckTrue(NthChild(pBar, "counter", 0) != null
                                                      && NthChild(pBarBg, "counter", 0) == null,
                                                      "`counter` 挂在 **`ProgressBar`** 下（与 `Background`/`Outline` 同层）");
                                            var pFillArea = FindChild(pBarBg, "Fill Area");
                                            CheckAtWorld(pFillArea, 976.12f, 1477.12f, 579.45f, 610.71f,
                                                         "`Fill Area` 的父是 **`Background`**（原版树就是这层嵌套，"
                                                         + "⚠️ 不是挂在 `ProgressBar` 下）");
                                            var pFill = FindChild(pFillArea, "Fill");
                                            CheckAtWorld(pFill, 976.12f, 976.12f, 579.45f, 610.71f,
                                                         "`Fill` **出厂零宽**（`Slider.value = 0`；与奖杯格那一支同一件事）");
                                            var pEnd = FindChild(pFill, "end");
                                            CheckAtWorld(pEnd, 952.38f, 981.82f, 595.78f, 627.64f,
                                                         "└ `end` 端帽（dump 实读；⚠️ 原版它被 `Background` 的 `Mask` 裁掉，"
                                                         + "我们**没有掩码体系** ⇒ 会画出来 —— 文件头 ③ 那条已知偏离）");
                                            CheckTint(pEnd, new Color(1f, 1f, 1f, 0.698f), 0.01f,
                                                      "`end` 染色 α **0.698**（dump 实读）");

                                            // ---- 勾选行 ----
                                            var pSel = FindChild(pRs, "selectButton");
                                            CheckAtWorld(pSel, 976.12f, 1477.12f, 663.00f, 717.42f,
                                                         "`selectButton`（= `toggle` 字段那一颗的父）");
                                            var pChk = FindChild(pSel, "Checkbox");
                                            CheckAtWorld(pChk, 990.39f, 1477.12f, 669.83f, 728.03f,
                                                         "`Checkbox`（原版那颗 `EverguildToggle` 就在它身上）");
                                            var pBox = FindChild(pChk, "Toggle");
                                            CheckAtWorld(pBox, 990.39f, 1048.59f, 669.83f, 728.03f,
                                                         "└ `Toggle` 方框（⚠️ **位置是我们挑的** —— dump 里那两个子件是"
                                                         + "「0 宽、贴右端」的退化值，因为布局组的首选宽度要字体度量）");
                                            // 🔴 **⛔ 别用 `QuadOf`（=「子树里第一张 quad」）来认这一块**：`Toggle` 底下有
                                            //    **两张** quad（`Image` = 方框、`CheckMark` = 勾），而 `GetComponentsInChildren`
                                            //    的**顺序没有保证** ⇒ 同步点实跑抓到的就是 `CheckMark`（这条断言**假红**）。
                                            //    ⇒ 改成**按贴图名在子树里找**，并把**子树里所有贴图名**印进消息（下次好定位）。
                                            var tglQuads = pBox != null ? pBox.GetComponentsInChildren<ImageQuad>(true)
                                                                        : new ImageQuad[0];
                                            ImageQuad boxQ = null;
                                            string tglTexNames = "";
                                            foreach (var tq in tglQuads)
                                            {
                                                if (tq == null) continue;
                                                string qn = tq.Texture != null ? tq.Texture.name : "<无贴图>";
                                                if (tglTexNames.Length > 0) tglTexNames += " / ";
                                                tglTexNames += qn;
                                                // 🔴 **2026-10-04 实跑订正**：这里原来用 `==` 比 **`40K_dropdown_bg`**（大写 K =
                                                //   dump 的 sprite 名 / 磁盘上的文件名），而**运行期那张 `Texture2D.name` 是小写的
                                                //   `40k_dropdown_bg`** —— 首跑的消息把它自己印出来了：
                                                //   「实得子树里的贴图：`40k_dropdown_bg` / `40k_general_bt_yellow_confirm`」
                                                //   ⇒ 恒不匹配、**假红**。名字的大小写**不是**这条判据要管的东西 ⇒
                                                //   改成**不区分大小写**（要钉名字就两种写法都认下来，别把它变成文字游戏）。
                                                if (boxQ == null && string.Equals(qn, "40K_dropdown_bg",
                                                                                 System.StringComparison.OrdinalIgnoreCase))
                                                    boxQ = tq;
                                            }
                                            CheckTrue(boxQ != null,
                                                      "★ `Toggle` 子树里**有**那块方框图（`40K_dropdown_bg`）—— 实得子树里的贴图："
                                                      + (tglTexNames.Length > 0 ? tglTexNames : "（一张 quad 都没有）"));
                                            if (boxQ != null)
                                            {
                                                // **Simple + preserveAspect** 的判据 = dump 那一列的 `Simple (1,1,1,1) preserveAspect`
                                                // ⇒ 画出来必须**等比**：原图 `40K_dropdown_bg` 是 **119×102**（PNG 头实读，aspect 1.1667），
                                                //   而外框是 58.2 的正方形 ⇒ 等比内接后 = **58.2 × 49.9**。
                                                //   ⚠️ 拉伸画会得到 aspect 1.0 —— 这一条钉的就是 PA 到底接没接。
                                                CheckNear(boxQ.WorldW * 108f, 58.20f, 0.5f,
                                                          "★ 方框画出来的**宽** = 58.20（等比内接：外框 58.2×58.2、原图更宽 ⇒ 宽顶满）");
                                                CheckNear(boxQ.WorldH * 108f, 58.20f * 102f / 119f, 0.5f,
                                                          "★ …**高** = 58.20 × 102/119 = **49.88**（`preserveAspect`；拉伸画会是 58.20）");
                                                CheckNear(boxQ.WorldW / Mathf.Max(1e-6f, boxQ.WorldH), 119f / 102f, 0.02f,
                                                          "★ …宽高比 = 原图 **119:102 = 1.1667**（`Simple` + `preserveAspect`，⛔ 不是 Sliced）");
                                            }
                                            var pMarkQ = QuadOf(FindChild(pBox, "CheckMark"));
                                            CheckTrue(pMarkQ != null && pMarkQ.Texture != null
                                                      && pMarkQ.Texture.name == "40k_general_bt_yellow_confirm",
                                                      "└ `CheckMark` = `40k_general_bt_yellow_confirm`");
                                            CheckTintOn(pMarkQ, new Color(0.575f, 0.209f, 0.209f, 0f), 0.01f,
                                                        "零值态（未选中）⇒ 勾的 **alpha = 0**"
                                                        + "（原版显隐走 `Toggle.graphic` 的 alpha，不是 `SetActive`）");
                                            CheckText(TextOf(FindChild(pChk, "Label")), "Alliance featured trophy",
                                                      "└ `Label` 的字 = prefab 里的字面串");

                                            // ---- 数据路（自检喂一条；产品路径上那份数据来自服务器）----
                                            popT.SetTrophy(new TrophyInfoPopup.TrophyView
                                            {
                                                Name = "自检奖杯", Description = "自检描述", Value = 5, Max = 10,
                                                Featured = true, ShowProgress = true,
                                            });
                                            CheckText(TextOf(pTtl), "自检奖杯",
                                                      "★ 喂一条数据 ⇒ 名字**真的铺上去了**（数据路是接好的）");
                                            CheckText(TextOf(pDesc), "自检描述", "★ …描述也是");
                                            // ★ 有字了要**重新对齐**（`Apply` 里那句 `MenuDraw.AlignLeft`）——
                                            //   空串时守卫拒绝挪、有字时才按左沿摆 ⇒ 这条刚好把那一跳钉住。
                                            float atx1, aty1, atx2, aty2;
                                            if (RenderedRect(pTtl, out atx1, out aty1, out atx2, out aty2))
                                                CheckNear(atx1, 976.12f, 1.0f,
                                                          "★ 喂了数据之后 `Title` **重新对齐到框左沿**（文字左边缘 976.12）"
                                                          + "—— 空串时守卫不挪、有字了才对齐，这一条是那一跳的判据");
                                            // ⚠️ 字号那一条**放在喂数据之后**：空串时 TMP 的自适应收敛到哪一档没定论
                                            //    （量它等于赌），有真字在里面才是「字号真的生效」的那一态。
                                            // 🔴 **这条要断的是哪一档，先说清**（本仓栽过：**dump 里 `字号` 那一列是
                                            //    【收敛结果】，不是输入档**）—— 本窗 `Title` 的原始 MB 实读：
                                            //    `m_fontSize **40**`（= 结果，恰好顶到上限）· **`m_fontSizeBase 36`**（设计者填的输入）
                                            //    · `m_fontSizeMin/Max **3 / 40**` · `m_enableAutoSizing 1`。
                                            //    ⇒ ① 我们**传进去那一档**（名义值）= 40 = 原版 `m_fontSizeMax`（判据 = MB 实读，
                                            //         也是本仓既有口径：「`fontPx` = 原版 TMP 的 `m_fontSize`」）；
                                            //       ② **收敛结果**由 TMP 按盒子算 ⇒ **只断它落在原版区间 [3,40] 里**，
                                            //          ⛔ **不断「等于 40」**（实跑 **34.292** —— 因为我喂的是 4 个**汉字**
                                            //          「自检奖杯」：汉字行盒 ≈1.3em = 52px 正好顶到框高 52 ⇒ TMP 让了一档；
                                            //          原版样例是拉丁字母 `Trophy Name`、行盒矮，所以才停在 40）；
                                            //       ③ 真正该守的是**渲出来那块不冲出框**（本仓 `AutoFitBox` 那条教训：
                                            //          「断渲染尺寸 ≤ 框，别只比字号」）。
                                            // ③ 真正该守的是**渲出来那块不冲出框**（本仓 `AutoFitBox` 那条教训：
                                            //    「断渲染尺寸 ≤ 框，别只比字号」）—— 直接用现成的 `CheckFits`，别写第二份。
                                            //    ⛔ 别为了让 34.292 变 40 去改 `SetAutoFitBox` 的参数（那是改实现迁就断言）。
                                            CheckNear(NominalFontPx(pTtl), 40f, 0.6f,
                                                      "★ `Title` 的**名义字号（我们传进去那一档）= 40**"
                                                      + "（= 原版 `m_fontSizeMax`；MB 实读 `m_fontSize 40` 是**收敛结果**、"
                                                      + "`m_fontSizeBase 36` 才是设计者填的输入）");
                                            CheckFontInRange(pTtl, 3f, 40f,
                                                             "★ …**收敛结果**落在原版 autosize 区间 `[m_fontSizeMin 3, m_fontSizeMax 40]` 里"
                                                             + "（开了 auto ⇒ 具体值由 TMP 按盒子算，⛔ 不断「等于 40」）");
                                            CheckFits(pTtl, 516.72f,
                                                      "★ …而且**渲出来那块没有冲出框**（框宽 = 原版 `Title` 矩形宽 516.72）——"
                                                      + " 本仓 `AutoFitBox` 那条教训：断「渲染尺寸 ≤ 框」，别只比字号");
                                            {
                                                float hw1, hh1, hw2, hh2;
                                                if (RenderedRect(pTtl, out hw1, out hh1, out hw2, out hh2))
                                                    CheckTrue(hh2 - hh1 <= 52f + 0.5f,
                                                              $"…高也不出框（{hh2 - hh1:F1} ≤ 框高 52）—— 汉字行盒 ≈1.3em，"
                                                              + "这一窗的收缩正是被框高顶出来的（TMP 让到 34.29 才装下）");
                                                CheckTrue(FontPxOf(pTtl) > 3.5f,
                                                          "★ …而且字号**没有被压到自适应下限**（> 3px）——"
                                                          + " 防「自适应把字压没了」那一族回归");
                                            }
                                            CheckText(TextOf(NthChild(pBar, "counter", 0)), "5/10",
                                                      "★ …进度字（原版格式串 `{0}/{1}`）");
                                            CheckTintOn(QuadOf(FindChild(pBox, "CheckMark")),
                                                        new Color(0.575f, 0.209f, 0.209f, 1f), 0.01f,
                                                        "★ …`Featured = true` ⇒ 勾的 alpha 变 1（同一个件、只改 α）");
                                            // `Fill` 的宽 = `Fill Area` 宽 501 × 5/10 = 250.5（原版由 `Slider` 改 `Fill` 的 anchors）
                                            var pFill2 = FindChild(pFillArea, "Fill");
                                            float fx1 = float.MaxValue, fx2 = float.MinValue;
                                            // ⚠️ **排除 `end` 子树**：端帽是 `Fill` 的**子件**（原版树如此），
                                            //    而它比 `Fill` 的右端还探出 5.7px（`pivot (1,0.5)` + `pos 5.7`）
                                            //    ⇒ 算进来会把宽度撑到 256.2（量错了东西，不是实现错）。
                                            var endNode = pFill2 != null ? FindChild(pFill2, "end") : null;
                                            if (pFill2 != null)
                                                foreach (var q in pFill2.GetComponentsInChildren<ImageQuad>(true))
                                                {
                                                    if (q == null) continue;
                                                    if (endNode != null && q.transform.IsChildOf(endNode)) continue;
                                                    float qcx = LayoutSpace.PxX(q.transform.position.x);
                                                    float qw = q.WorldW * 108f;
                                                    fx1 = Mathf.Min(fx1, qcx - qw * 0.5f);
                                                    fx2 = Mathf.Max(fx2, qcx + qw * 0.5f);
                                                }
                                            CheckTrue(fx1 < float.MaxValue, "★ `Fill` 真的有 quad 了（下面两条量的就是它）");
                                            if (fx1 < float.MaxValue)
                                            {
                                                CheckNear(fx2 - fx1, 250.5f, 1.0f,
                                                          "★ `Fill` 的宽 = `Fill Area` 宽 501 × 5/10 = **250.5px**"
                                                          + "（原版 `Slider` 就是这么改 `Fill` 的 anchors）");
                                                CheckNear(fx1, 976.12f, 1.0f, "…左端仍在 `Fill Area` 的左沿");
                                            }
                                            CheckAtWorld(FindChild(pFill2, "end"), 952.38f + 250.5f, 981.82f + 250.5f,
                                                         595.78f, 627.64f,
                                                         "★ 端帽跟着 `Fill` 的右端走（原版 `end` 是 `Fill` 的子）");

                                            // ---- 勾一下：原版是**服务器写**，我们不许让界面说谎 ----
                                            popT.HandleFeatureTrophy(false);      // = 点一下那一行
                                            CheckTrue(popT.ToggleIsOn,
                                                      "★ `HandleFeatureTrophy` **没有改状态**（原版发 `PlayFab…GenericCloudScriptHandler(0x439,…)`"
                                                      + " 写服务器；本地没有 ⇒ 勾**退回真实状态**，⛔ 不静默失败、也不让界面说谎）");

                                            // ---- 整棵的层带 ----
                                            int pMin = int.MaxValue, pMax = -1;
                                            foreach (var q in pRoot.GetComponentsInChildren<ImageQuad>(true))
                                                if (q != null) { pMin = Mathf.Min(pMin, q.RenderQueue); pMax = Mathf.Max(pMax, q.RenderQueue); }
                                            foreach (var l in pRoot.GetComponentsInChildren<Label>(true))
                                                if (l != null) { pMin = Mathf.Min(pMin, l.RenderQueue); pMax = Mathf.Max(pMax, l.RenderQueue); }
                                            // 🆕 **2026-10-06（A95）**：带子从 3310–3325 变成 **3310–3326**
                                            //   —— `Next Tier` 从 `QText`(3314) 挪去自成一档 `QNextTier`(3320)
                                            //   （原版兄弟序 ⇒ 它压在 `ProgressBar` 那一叠之上），其后各档 +1、`QHit` 3326。
                                            //   上界与上一档 3400（挑战弹窗）之间仍是空档 ⇒ 不撞别的窗带。
                                            CheckTrue(pMin >= 3310 && pMax <= 3326,
                                                      "★ 弹窗**整棵**都落在自己的带子里（**3310–3326** = 奖杯格 3299 之后、"
                                                      + "挑战弹窗 3400 之前 —— 层带不许重叠）"
                                                      + $"—— 实得 [{pMin},{pMax}]");

                                            // ---- ④d 🆕 2026-10-04（**A74①**）：tooltip 的层带 ----
                                            //   原来它取 **3199/3200/3201**，与社交页那一档 **3200–3209** **正面重叠**
                                            //   （哪天社交窗里出现 tooltip 就会被压在页面内容下面）⇒ 搬到全壳最高一档
                                            //   （**3605–3607**，判据与理由 → `Core/Tooltip.cs` 那段：原版 `Safe area Only Horizontal`
                                            //    的子件序里 `TooltipManager` 排在 `Upper bar` 与 `3 - PopUp Holder` **之后**）。
                                            //   ⚠️ 这条**不是**「读我们自己的常量比一遍」（那是同式自证）：现场开一条真 tooltip，
                                            //      拿**社交窗整棵树 + 这扇弹窗**里量到的**最大**队列当期望。
                                            Tooltip.Show("自检提示（层带）", new Vector3(0f, 0f, 0f));
                                            Tooltip.FinishFade();
                                            var tipGo = GameObject.Find("TooltipLayer");
                                            int tipMin = int.MaxValue, tipN = 0;
                                            if (tipGo != null)
                                            {
                                                foreach (var q in tipGo.GetComponentsInChildren<ImageQuad>(true))
                                                    if (q != null) { tipMin = Mathf.Min(tipMin, q.RenderQueue); tipN++; }
                                                foreach (var l in tipGo.GetComponentsInChildren<Label>(true))
                                                    if (l != null) { tipMin = Mathf.Min(tipMin, l.RenderQueue); tipN++; }
                                            }
                                            int socialMax = -1;
                                            foreach (var q in sw.transform.GetComponentsInChildren<ImageQuad>(true))
                                                if (q != null) socialMax = Mathf.Max(socialMax, q.RenderQueue);
                                            foreach (var l in sw.transform.GetComponentsInChildren<Label>(true))
                                                if (l != null) socialMax = Mathf.Max(socialMax, l.RenderQueue);
                                            socialMax = Mathf.Max(socialMax, pMax);
                                            CheckTrue(tipN > 0 && tipMin < int.MaxValue,
                                                      "tooltip 真建出来了（否则下面那条等于没查）");
                                            CheckTrue(socialMax > 0 && tipMin > socialMax,
                                                      "★ tooltip 的**每一块**都在**它解释的那扇窗的每一件之上**"
                                                      + "（期望值 = 现场量到的社交窗 + 弹窗的最大队列，⛔ 不是我们自己的常量）"
                                                      + $"—— 实得 tip={tipMin} > 窗={socialMax}"
                                                      + "；改回 3199/3200/3201 时这一条**红**（那一档正撞在社交页 3200–3209 上）");
                                            Tooltip.Hide();
                                            Tooltip.FinishFade();

                                            // ---- 收工：**关掉它**（留着会污染后面的断言 —— A66 那条教训）----
                                            // 🆕 **2026-10-08（A221②）**：本窗是 21 处 `MenuDraw.ShadeHit` 里**唯一没有**
                                            //   档位不变量断言的（原来只有下面那条 `CheckAbsorbRule` 覆盖到「命中是压暗层那颗」）
                                            //   —— 全工程 `ShadeHit(` 21 处 vs `CheckShadeRule(` 20 处，差额正是这里。
                                            //   取法：命中节点 = `Shell/TrophyInfoPopup.cs:250` 那颗 `BackgroundHit`
                                            //   （`ShadeHit(root, ShadeR, QShade, QHit, …, "BackgroundHit")`）；
                                            //   视觉压暗层 = 同文件 `:247` 那句 `Rect(root, …, "Menu Dark Background",
                                            //   QShade, ShadeCol)` 建的**那颗 quad 自己**（`ShadeVisualQuad` 的第 ① 种摆法）。
                                            //   ⛔ 最后那个档写 `TrophyInfoPopup.QHit` 是本窗**声明的内容命中区档**（与
                                            //   `ShadeHit` 同一个实参 —— 那一半本来就是「常量互锁」，见 `ShadeRuleOk` 的注释）；
                                            //   真正有鉴别力的是它内部第 ③ 条：**量同一扇窗那块 `Menu Dark Background`
                                            //   自己 quad 的档**（另一处代码建的另一个对象）⇒ 把 `:247` 的 `QShade` 换成别的档，
                                            //   或把 `:250` 的 `QShade` 换成 `QShade+1` / `QHit−1`，这一条都**必红**。
                                            MenuDraw.CheckShadeRule(CheckTrue, "奖杯详情弹窗",
                                                                    FindChild(popT.transform, "BackgroundHit"),
                                                                    popT.transform.Find("Menu Dark Background"),
                                                                    TrophyInfoPopup.QHit);
                                            // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处
                                            //   ⇒ 原版什么都不发生）。期望矩形 = **原版 prefab**
                                            //   `Alliance Trophy Info Popup > window > Generic Window Red Background Big`
                                            //   那颗 `Image` 的 rect（395.70,178.35 → 1547.30,895.80）；⛔ 不写
                                            //   `TrophyInfoPopup.WinBgR`（那是被测实现**传进去的实参**，同式自证）。
                                            CheckAbsorbRule("奖杯详情弹窗", popT.transform, "AbsorbHit",
                                                            395.70f, 178.35f, 1547.30f, 895.80f,
                                                            TrophyInfoPopup.QShade, TrophyInfoPopup.QHit,
                                                            () => popT.CurrentState);
                                            // ⚠️ 上面那一组**结尾就把窗关掉了** ⇒ 开回来，下面那句才是**真**在断 `Close()`
                                            //   （`TrophyInfoPopup.Open()` = `Build()` 重建，奖杯数据留在 `_view` 里）。
                                            CheckTrue(popT.TryOpen(null), "（A94 收尾）把奖杯弹窗开回来 —— 下面那句才不是空断");
                                            popT.Close();
                                            CheckTrue(popT.CurrentState == WindowState.Closed && !popT.gameObject.activeSelf,
                                                      "★ 收工前弹窗关掉了（`Close()` 把它 `SetActive(false)`）");
                                        }
                                    }
                                }
                            }
                            mt3.ShowGeneral();                    // 恢复：出厂就是 `General` 那一支
                            mv.gameObject.SetActive(false);
                            nmv.gameObject.SetActive(nmWas2);
                            SocialData.AllianceTrophies = 0;      // 手动清（双保险：`ResetForTest` 现在也会清它，见下面 ⑦）
                        }

                        // ------------------------------------------------ ⑤ `GeneralDetails` 取 act T 那一份（🆕 A29）
                        //   判据：原版挂着**两个同名不同 pid** 的 `GeneralDetails`（§B·5）——
                        //     · `AllianceMemberVariant>GeneralDetails`（RT `-4327119531760820061`，act **T**，
                        //       普查 `社交_联盟与好友页.md:264`）= **331.17,188.83→1920.00,1080.05**
                        //       —— **我们这一支该用的那一份**（页签 `General`/`Trophies`、`ChatPreview` 只在这支里）；
                        //     · `AllianceNotMemberVariant>GeneralDetails`（RT `-8680982849342087005`，act F，
                        //       普查 `:171`）= 332.67,162.04→1919.00,1080.02 ——「查看别的盟」那一态
                        //       （`AllianceSearchTab.HandleDisplayAlliance` 才点亮它）。
                        //   ⇒ 下面每个数都是**普查里 act T 那份的字面量**（⛔ 不读实现常量、⛔ 不读 act F 那份）。
                        //   🔴 改之前这棵树是「两份各取一半」（上半段 act F / 下半段 act T）⇒ 这些断言当时会红。
                        {
                            bool nmWas3 = nmv.gameObject.activeSelf;
                            nmv.gameObject.SetActive(false);
                            mv.gameObject.SetActive(true);
                            var mt4 = sw.PageAlliances.Member;
                            mt4.ShowGeneral();
                            var gd = FindChild(mv, "GeneralDetails");
                            CheckAtWorld(gd, 331.17f, 1920.00f, 188.83f, 1080.05f,
                                         "★ `GeneralDetails` 根 = **act T 那一份**（331.17,188.83→1920.00,1080.05；"
                                         + "act F 那份是 332.67,162.04→1919.00,1080.02 —— **上边差 26.79、左边差 1.5**）");
                            // 上半段（改之前用的是 act F 那份 ⇒ 这几条都能真红）
                            CheckAtWorld(FindChild(gd, "BadgeDrawer"), 358.90f, 609.45f, 197.27f, 432.39f,
                                         "`BadgeDrawer`（act T 值；act F 那份是 360.40,170.48→610.95,405.60）");
                            CheckAtWorld(FindChild(gd, "Alliance name text"), 613.38f, 1112.74f, 204.27f, 275.35f,
                                         "盟名（act T 值；act F 那份 y 从 177.48 起）");
                            CheckAtWorld(FindChild(gd, "Alliance Rating Display"), 613.38f, 1099.96f, 264.16f, 344.99f,
                                         "`Alliance Rating Display`（act T y=264.16；act F 那份是 237.38）");
                            CheckAtWorld(FindChild(gd, "Draft Rating Display"), 613.38f, 1099.96f, 344.99f, 425.82f,
                                         "`Draft Rating Display`（act T y=344.99；act F 那份是 318.21）");
                            CheckAtWorld(FindChild(gd, "Config fields"), 1080.54f, 1863.35f, 209.81f, 269.81f,
                                         "`Config fields`（act T 值；act F 那份是 1079.54,183.02→1862.35,243.02）");
                            // `Config fields` 那一行：**act T 里五个子件全亮、`extra_info` 灭**（与 act F **正好相反**）
                            CheckAtWorld(FindChild(gd, "LanguagesDropdown"), 1130.90f, 1380.90f, 210.41f, 269.81f,
                                         "★ `LanguagesDropdown`（act T 里 act=T ⇒ 建；act F 那份是 act F）");
                            CheckAtWorld(FindChild(gd, "Privacy Dropdown"), 1395.40f, 1645.40f, 210.41f, 269.81f,
                                         "★ `Privacy Dropdown`（act T 里 act=T）");
                            CheckAtWorld(FindChild(gd, "Edit button"), 1659.90f, 1718.05f, 209.81f, 269.81f,
                                         "★ `Edit button`（act T 里 act=T）");
                            CheckAtWorld(FindChild(gd, "Confirm button"), 1732.55f, 1790.70f, 209.81f, 269.81f,
                                         "★ `Confirm button`（act T 里 act=T）");
                            CheckAtWorld(FindChild(gd, "Cancel button"), 1805.20f, 1863.35f, 209.81f, 269.81f,
                                         "★ `Cancel button`（act T 里 act=T）");
                            CheckTrue(FindChild(gd, "extra_info") == null,
                                      "★ `extra_info`（`English / Private` 那行）**不建** —— 它在 act T 那份里是 "
                                      + "**act F**（普查 `:280`），只在未入盟支那一棵上亮（普查 `:187`）"
                                      + "⇒ 建在这棵树上 = **取错实例**（改之前这里画的正是它）");
                            // 五件的**相邻间距 = `HorizontalLayoutGroup.spacing 14.5`**（普查 `:279`）——
                            //   位置对不对，光看单个 rect 看不出来，这一条把「这一行是布局跑出来的」钉住。
                            CheckNear(1395.40f - 1380.90f, 14.5f, 0.01f,
                                      "两个下拉之间 = `Config fields` 的 `spacing 14.5`（`1380.90 → 1395.40`）");
                            CheckNear(1659.90f - 1645.40f, 14.5f, 0.01f, "隐私下拉 → `Edit` 之间 = 14.5");
                            CheckTint(FindChild(FindChild(gd, "Edit button"), "Image"),
                                      new Color(1f, 0.773f, 0.333f, 1f), 0.01f,
                                      "`Edit button` 底图染色（`40k_general_bt_yellow`，普查 `:307`）");
                            // 下半段**本来就是 act T** ⇒ 钉一条防回退（别被「换成另一份」的改动带偏）
                            CheckAtWorld(FindChild(FindChild(gd, "MemberList"), "Scroll View"),
                                         369.67f, 1880.67f, 493.63f, 1080.05f,
                                         "下半段 `Scroll View`（本来就是 act T 那份）");
                            CheckAtWorld(FindChild(FindChild(gd, "Description input text"), "description text"),
                                         1146.32f, 1863.33f, 291.64f, 481.71f,
                                         "下半段 `description text`（本来就是 act T 那份）");
                            mv.gameObject.SetActive(false);
                            nmv.gameObject.SetActive(nmWas3);
                        }
                        SocialData.ResetForTest();            // 上面几处喂的数据一律清掉（自检不留假数据）

                        // ------------------------------------------------ ⑥ 未入盟支那一棵 `GeneralDetails`（🆕 A55②）
                        //   原版**两颗 `GeneralDetails` 同名不同 pid**（§B·5）：
                        //     · `AllianceMemberVariant>GeneralDetails`（RT `-4327119531760820061`，act T）= 上面 ⑤ 那棵；
                        //     · `AllianceNotMemberVariant>GeneralDetails`（RT `-8680982849342087005`，act **F**）
                        //       = 「**点公开列表里某个盟 → 看它的详情**」那一态 —— 原版由
                        //       `AllianceSearchTab.HandleDisplayAlliance` 点亮（反编译 `AllianceSearchTab__HandleDisplayAlliance.c`：
                        //       两个 `EverguildToggle` 置 `isOn = 0` → 关 `join`/`create` 两个菜单 →
                        //       `SetActive(allianceView, true)` → `AllianceView.Draw(group)`）。
                        //   🔴 改之前**这一棵压根没建** ⇒ 下面每一条都会红（`GeneralDetailsView` 是 null）。
                        //   ⚠️ 期望值全是**普查里 act F 那一份的字面量**（`社交_联盟与好友页.md:171-240`）——
                        //      ⛔ 不引用实现常量，也⛔ 不拿 act T 那份的数去推（两份**不是纯平移**：右锚件 x 差 −1.0）。
                        {
                            var search = sw.PageAlliances.Search;
                            var gdF = search.GeneralDetailsView;
                            CheckTrue(gdF != null && !gdF.activeSelf && !search.ShowingDetails,
                                      "★ `AllianceNotMemberVariant>GeneralDetails` **建出来了、出厂 act F**"
                                      + "（原版 prefab 里这一棵就是 act F；改之前这里连节点都没有）");
                            CheckAtWorld(gdF != null ? gdF.transform : null, 332.67f, 1919.00f, 162.04f, 1080.02f,
                                         "★ 它的根 = **act F 那一份**（332.67,162.04→1919.00,1080.02）——"
                                         + "act T 那份是 331.17,188.83→1920.00,1080.05（上边差 26.79、左边差 1.5）");
                            var gdFt = gdF != null ? gdF.transform : null;
                            CheckAtWorld(FindChild(gdFt, "BadgeDrawer"), 360.40f, 610.95f, 170.48f, 405.60f,
                                         "`BadgeDrawer`（act F 值；act T 那份是 358.90,197.27→609.45,432.39）");
                            CheckAtWorld(FindChild(gdFt, "Alliance name text"), 614.88f, 1114.24f, 177.48f, 248.56f,
                                         "盟名（act F y=177.48；act T 那份是 204.27）");
                            CheckAtWorld(FindChild(gdFt, "Alliance Rating Display"), 614.88f, 1101.46f, 237.38f, 318.21f,
                                         "`Alliance Rating Display`（act F y=237.38；act T 那份是 264.16）");
                            CheckAtWorld(FindChild(gdFt, "Draft Rating Display"), 614.88f, 1101.46f, 318.21f, 399.04f,
                                         "`Draft Rating Display`（act F y=318.21；act T 那份是 344.99）");
                            CheckAtWorld(FindChild(gdFt, "Config fields"), 1079.54f, 1862.35f, 183.02f, 243.02f,
                                         "`Config fields`（act F 值；act T 那份是 1080.54,209.81→1863.35,269.81）");
                            // `Config fields` 那一行：**act F 这份里 `extra_info` 亮、五个可编辑件全灭**（与 act T **正好相反**）
                            // ⚠️ 原版这一行是 **`Right/Middle`**、我们接的是 `MenuDraw.AlignRight` ⇒ **它会把整块字
                            //    挪到右边**（`Label.AlignRightOn`）⇒ **不能拿 `position` 当矩形中心断**，
                            //    要断**右边缘**（同上面建盟表那条「左对齐的文字不能断中心」）。
                            var xinfo = FindChild(gdFt, "extra_info");
                            var xlb = xinfo != null ? xinfo.GetComponent<Label>() : null;
                            CheckTrue(xinfo != null && xlb != null,
                                      "★ `extra_info`（语言 / 隐私那行只读摘要）**建了** ——"
                                      + "它在 act F 那份里 act=**T**（普查 `:187`；act T 那棵上它是 F）");
                            if (xinfo != null && xlb != null)
                            {
                                float xR = LayoutSpace.PxX(xinfo.position.x + xlb.WorldW * 0.5f);
                                CheckNear(xR, 1862.35f, 1f,
                                          "★ 它**右对齐到 1862.35**（原版 `Right/Middle`，普查 `:187`）");
                                CheckNear(LayoutSpace.PxY(xinfo.position.y), 213.02f, 1f,
                                          "…纵向中心 = 213.02（`183.02,243.02` 那条的中线）");
                            }
                            CheckText(TextOf(xinfo), "English / Private",
                                      "`extra_info` 的字 = **资产里的字面样例串**（运行期那两个值在服务器上）");
                            CheckTrue(FindChild(gdFt, "LanguagesDropdown") == null
                                      && FindChild(gdFt, "Privacy Dropdown") == null
                                      && FindChild(gdFt, "Edit button") == null
                                      && FindChild(gdFt, "Confirm button") == null
                                      && FindChild(gdFt, "Cancel button") == null,
                                      "★ 那五个可编辑件在这棵上**一个都不建**（act F 那份里它们全是 act F；"
                                      + "而 act T 那棵上它们全亮 —— 两份正好相反，⛔ 别把一套显隐套到两棵树上）");
                            CheckAtWorld(FindChild(gdFt, "Description input text"), 1129.44f, 1878.21f, 262.06f, 455.29f,
                                         "`Description input text`（act F 值；act T 那份 y 从 288.85 起）");
                            CheckAtWorld(FindChild(FindChild(gdFt, "MemberList"), "Scroll View"),
                                         371.17f, 1882.17f, 466.85f, 1080.02f,
                                         "`MemberList>Scroll View`（act F 值；act T 那份是 369.67,493.63→1880.67,1080.05）");
                            // 这一棵的滚动区：视口 = act F 那个 `Scroll View`，档位照原版（`m_MovementType = 1`）
                            var ssc = search.DetailScroll;
                            CheckTrue(ssc != null && Mathf.Abs(ssc.Viewport.y1 - 466.85f) < 0.5f
                                      && Mathf.Abs(ssc.Viewport.x1 - 371.17f) < 0.5f,
                                      "★ 详情那一棵**也有自己的滚动区**，视口就是 act F 那个 `Scroll View`"
                                      + "（`371.17,466.85→1882.17,1080.02`）—— 两棵树的视口**不是同一个矩形**");
                            CheckTrue(ssc != null && ssc.Elastic && ssc.Vertical,
                                      "档位：**Elastic** + 纵向（原版两份 `Scroll View` 都是 `m_MovementType = 1`）");

                            // ---- 「点公开列表里某个盟 → 看它的详情」这条路真的走通吗 ----
                            SocialData.OpenAlliances.Add(new SocialData.AllianceListing
                            { Name = "Detail Target", Region = "Global", Members = 3, MemberMax = 20, Rating = "" });
                            search.RebuildForTest();
                            var dvp = FindChild(FindChild(FindChild(FindChild(lv, "List Area"), "Open Alliances"),
                                                          "Viewport"), "List");
                            var drow = NthChild(dvp, "Entry", 0);
                            var dHit = FindChild(drow, "InfoHit");
                            var dWb = dHit != null ? dHit.GetComponent<WindowButton>() : null;
                            CheckTrue(dWb != null && dWb.onClick != null,
                                      "公开联盟那一行**接了 `InfoHit`**（原版 `AllianceListEntry.HandleInfo`）");
                            if (dWb != null && dWb.onClick != null)
                            {
                                dWb.onClick();
                                CheckTrue(search.ShowingDetails && gdF != null && gdF.activeSelf,
                                          "★ 点它 ⇒ **切到「看详情」那一态**（原版 `HandleDisplayAlliance`："
                                          + "关 `join`/`create` 两个菜单 → 点亮 `allianceView`）");
                                CheckTrue(!FindChild(nmv, "List View").gameObject.activeSelf,
                                          "⇒ `List View` 藏起来（原版 `joinAllianceMenu.SetActive(false)`）");
                                CheckTrue(!search.CreateAllianceView.activeSelf,
                                          "⇒ `Create Alliance View` 也关着");
                                CheckText(TextOf(FindChild(gdFt, "Alliance name text")), "Detail Target",
                                          "★ 盟名填的是**你点的那一行**（本地唯一真拿得到的那个字段）");
                                // 两颗页签都灭（原版把两个 `EverguildToggle.isOn` 都置 0）——量**底图染色**
                                CheckTint(FindChild(nmv, "Generic Tab UI Button Search"),
                                          new Color(1f, 0.544f, 0f, 1f), 0.01f,
                                          "详情态：`Join` 键是 **off 色**（原版 `set_isOn(0)`）");
                                CheckTint(FindChild(nmv, "Generic Tab UI Button Create"),
                                          new Color(1f, 0.544f, 0f, 1f), 0.01f,
                                          "详情态：`Create` 键也是 **off 色**（两颗都灭）");
                                CheckText(TextOf(FindChild(FindChild(nmv, "Alliance Header Buttons"),
                                                           "Generic Tab UI Button Search")), "Back",
                                          "★ 详情态里 `Join` 那颗的**字换了**（原版 `joinButtonText ← "
                                          + "GetTranslation(另一个词条)`）—— ⚠️ `Back` 是**我们挑的兜底串**"
                                          + "（原版词条在远端本地化表，本地取不到；见 `AlliancesTab.LabelBack`）");
                                // ---- 退路：点 `Join` 键（原版 `ShowJoinAllianceMenu` 把 isOn 置回 1）----
                                var jHit = FindChild(FindChild(nmv, "Alliance Header Buttons"), "JoinHit");
                                var jWb = jHit != null ? jHit.GetComponent<WindowButton>() : null;
                                CheckTrue(jWb != null && jWb.onClick != null, "`Join` 键的命中区在");
                                if (jWb != null && jWb.onClick != null)
                                {
                                    jWb.onClick();
                                    CheckTrue(!search.ShowingDetails && !gdF.activeSelf,
                                              "★ 点 `Join` 键 ⇒ **退回公开列表**（详情那一棵关掉）");
                                    CheckTrue(FindChild(nmv, "List View").gameObject.activeSelf,
                                              "⇒ `List View` 又亮了");
                                    CheckText(TextOf(FindChild(FindChild(nmv, "Alliance Header Buttons"),
                                                               "Generic Tab UI Button Search")), "Join",
                                              "⇒ 那颗键的字**换回 `Join`**（`ShowJoinAllianceMenu` 里那一次 GetTranslation）");
                                    CheckTint(FindChild(nmv, "Generic Tab UI Button Search"),
                                              new Color(1f, 0.631f, 0f, 1f), 0.01f,
                                              "⇒ 它又回到 **on 色**（`onColor (1,0.631,0,1)`）");
                                }
                            }
                            SocialData.OpenAlliances.Clear();
                            search.RebuildForTest();
                            // 🔴 **无条件还原**：上面若在哪一条上早退（`dWb == null` / `jWb == null`），
                            //   这一页就会**停在详情态**、把后面的断言全带偏（那类失败很难查）。
                            search.ShowJoin();
                            CheckTrue(!search.ShowingDetails && !gdF.activeSelf,
                                      "收尾：详情那一棵已关、回到列表（自检不留状态）");
                        }

                        // ------------------------------------------------ ⑦ `SocialData.ResetForTest` 清不清那两个静态标量（🆕 A55④）
                        //   `AllianceName` / `AllianceTrophies` 是**静态标量**，原来不在 `ResetForTest` 里
                        //   （清的只有 `_friends` / `_invitations` / `_openAlliances` / `_members` / `_chat` 五张表）
                        //   ⇒ 自检喂完忘了手动清就会**漏到后面的断言里**（上面 ④ 那一段原本就是手动清的）。
                        //   判据 = `Shell/SocialData.cs` 表头那条语义「**默认全空**」。
                        //   🔴 这条**能真红**：把 `ResetForTest` 里那两行去掉 ⇒ 下面两条立刻报「Stale Alliance / 7」。
                        {
                            SocialData.AllianceName = "Stale Alliance";
                            SocialData.AllianceTrophies = 7;
                            SocialData.ResetForTest();
                            CheckTrue(SocialData.AllianceName == null,
                                      "★ `ResetForTest` 把 `AllianceName` 清成 null（改之前它不管这个静态标量）");
                            Check(SocialData.AllianceTrophies, 0,
                                  "★ `ResetForTest` 把 `AllianceTrophies` 清成 0（改之前它不管这个静态标量）");
                        }
                    }

                    Check(sw.MissingArt.Count, 0,
                          "社交窗**没有取不到的图**（取不到的件根本没画）");
                    CheckHoverSwap(sw.transform, "社交窗");
                }
            }
        }

        // ============================================================ 🆕 2026-10-06（A123）：入口「按引用复用」
        //   判据（三层，全部本地可复现）→ `Shell/WindowsManager.cs` 的 `OpenByRef` 那段
        //   （**2026-10-11（A177 尾巴）起机制本体在那里** —— `MainMenuRuntime` 那 8 条入口只剩一条转调）·
        //   `资料/普查产出_1007/波6判据核查.md` §2 · `资料/普查产出_1007/波6_A123_入口复用.md`：
        //     ① 原版缓存字段 = `WindowsManager.automaticallyLoadedWindows`（**键 = prefab 引用**）；
        //     ② `WindowsManager.OpenWindow` **第一件事就是查它**（`TryGetValue` 在 `Instantiate` 之前，
        //        命中 `jne` 跳复用块）⇒ **同一扇窗点两次只有一个实例**；
        //     ③ 🔴 **命中只在「窗还开着」时发生** —— 关窗会把缓存条目**删掉**（`CloseWindowCO:38-48`）
        //        ⇒ 关过之后再点，**原版走的是新建**。
        //   形状照 A104 那条（设置窗，本文件 `:427-461`）：**盯行为**（同一实例 / `openWindows` 里几扇），
        //   ⛔ 不盯我们自己的常量。
        //   ⛔ **别数锚点下的子节点**（`FindChild(anchor, "…")` 那种）：我们的 `Close()` 只 `SetActive(false)`、
        //   **不销毁对象** ⇒ 关掉的窗仍挂在那儿，计数**越数越多**（`MainMenuRuntime` 那段里的那个 ⛔ 同源）。
        Section("入口「按引用复用」（A123：点两次 = 同一扇 · 关掉再点 = 新建 · `closeAll` 收掉别的窗）");
        {
            var socialBtn2 = menu.Find("Main Menu Navigation Button - Social");
            var shN = socialBtn2 != null ? FindChild(socialBtn2, "Hit") : null;
            var swbN = shN != null ? shN.GetComponent<WindowButton>() : null;
            CheckTrue(swbN != null && swbN.onClick != null, "（前提）SOCIAL 键的点击在");

            if (swbN != null && swbN.onClick != null)
            {
                // ---- ① 点两次同一入口 ⇒ 同一实例 + `openWindows` 里只有 1 扇 ----
                swbN.onClick();
                var firstSW = SocialWindow.LastOpened;
                CheckTrue(firstSW != null && firstSW.CurrentState == WindowState.Open,
                          "（前提）点 SOCIAL ⇒ **真开出来一扇**（下面两条才有对象可断）");
                swbN.onClick();
                CheckTrue(SocialWindow.LastOpened == firstSW,
                          "★ 再点一次 SOCIAL ⇒ **还是同一扇**（原版 `automaticallyLoadedWindows` 命中复用 ⇒ "
                        + "`MainMenuRuntime.OpenByRef`；改回 `SocialWindow.Create(wm)` 直建 ⇒ 这条红）");
                int nSw = 0;
                if (WindowsManager.Instance != null)
                    foreach (var w in WindowsManager.Instance.openWindows)
                        if (w is SocialWindow) nSw++;
                CheckTrue(nSw == 1,
                          $"「开着的窗」里社交窗只有 **1** 扇（实测 {nSw}）—— 每次新建会叠出第二扇；"
                        + "⛔ 这里数的是 `WindowsManager.openWindows`，**不是**锚点子节点（`Close()` 不销毁对象）");

                // ---- ② 关掉再点 ⇒ 新建（**只有这一条区分得开「还开着才复用」与「实例还在就复用」**）----
                firstSW.Close();
                swbN.onClick();
                var secondSW = SocialWindow.LastOpened;
                CheckTrue(secondSW != null && secondSW != firstSW,
                          "★ 关掉之后再点 SOCIAL ⇒ **新建一扇**（原版关窗会删缓存条目；我们的 `Close()` 只 "
                        + "`SetActive(false)`、对象还在 ⇒ 实现写成「实例还在就复用」时**这条红**）");

                // ---- ③ `closeAll`（原版那颗导航钮 `closeOtherMenus = 1`）⇒ **别的窗被关掉** ----
                //   ⚠️ 两个入口同参：`OpenSocial` / `OpenRewards`（`MainMenuRuntime` 那两处都传 `true`）。
                var keepW = menu.OpenInbox();                       // 先开一扇别的（收件箱）
                CheckTrue(keepW != null && keepW.CurrentState == WindowState.Open, "（前提）收件箱开着");
                swbN.onClick();                                     // ← 这一下带 `closeAll`
                CheckTrue(keepW != null && keepW.CurrentState == WindowState.Closed,
                          "★ 点 SOCIAL ⇒ **别的窗被关掉**（原版 `closeOtherMenus = 1` ⇒ `OpenByRef(…, closeAll: true)`）"
                        + "；把那个参数丢掉 ⇒ 这条红");
            }

            // ---- ④ 收件箱入口（`MainMenuScene.Run` 全程**没盖过它**：原来全文件只有一条 `InboxBtn` 布局断言）----
            //   ⚠️ `InboxWindow` **没有 `LastOpened`** ⇒ 判据 = 方法返回值 + `openWindows` 计数（两条都 public）。
            //   ⚠️ `InboxBtn` 上那颗 `WindowButton` 的回调就是 `OpenInbox()`（`MainMenuRuntime.BuildUpperBar`）
            //      ⇒ 「点钮」与「调方法」走的是**同一份缓存**，两个返回值必须相等。
            // 🔴 **2026-10-07（自检红 → 修）**：`InboxBtn` 那颗 `WindowButton` **不在钮节点本身上**，而在它下面那颗
            //    图标节点上 —— `MainMenuRuntime` 的写法是 `inboxImg.gameObject.AddComponent<WindowButton>()`，
            //    而 `inboxImg = Rect(inbox, "40K_notification", …)`（节点名 **`Image`**、贴图 `40K_notification`）
            //    = 与齿轮**同一个写法**（`SettingsBtn/Image`）。⛔ 所以找组件必须**在子树里找**：
            //    按钮节点 `GetComponent` 会拿到 null ⇒ **假红**（功能其实是好的，只是挂载点在下一层）。
            var inboxBtn = FindChild(FindChild(bar, "TopBarButtons"), "InboxBtn");
            var ibb = inboxBtn != null ? inboxBtn.GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(ibb != null && ibb.onClick != null,
                      "`InboxBtn` **接了点击**（组件挂在它下面那颗图标节点上 —— 见上面那条；"
                    + "原来只有布局断言 ⇒ 点了没反应也查不出来）");
            if (ibb != null && ibb.onClick != null)
            {
                ibb.onClick();
                var in1 = menu.OpenInbox();
                CheckTrue(in1 != null && in1.CurrentState == WindowState.Open, "点 `InboxBtn` ⇒ **真的开了收件箱**");
                ibb.onClick();
                CheckTrue(menu.OpenInbox() == in1,
                          "★ 再点一次 `InboxBtn` ⇒ **还是同一扇**（入口方法的返回值就是缓存里那一扇）");
                int nIn = 0;
                if (WindowsManager.Instance != null)
                    foreach (var w in WindowsManager.Instance.openWindows)
                        if (w is InboxWindow) nIn++;
                CheckTrue(nIn == 1, $"「开着的窗」里收件箱只有 **1** 扇（实测 {nIn}）");
                in1.Close();
                var in2 = menu.OpenInbox();
                CheckTrue(in2 != null && in2 != in1, "★ 关掉之后再点 ⇒ **新建一扇**（原版关窗删缓存条目）");
                if (in2 != null) in2.Close();                       // 收尾：不留开着的收件箱
            }

            // ---- ⑤ 设置窗：「**关掉再点必须换实例**」最锋利的靶子（🆕 2026-10-06 追加）----
            //   🔴 为什么单独盯它：`SettingsWindow.Instance` 是**静态的**（`SettingsWindow.cs:299`，在 `Create()` 里赋值），
            //      而我们的 `Close()` **不销毁对象**（只 `SetActive(false)`）⇒ **关窗之后那个静态字段仍指着旧那一扇**。
            //      ⇒ 「把 `StillOpen` 写回 `Instance != null`」这种最自然的偷懒写法**只有这一条抓得住**
            //        （① / ② 两条走的是 `_openByRef` 缓存，实现退化时可能仍被缓存那一侧挡掉、蒙对）。
            //   ⚠️ `A104` 那两条旧断言（本文件 `:437/450`，同一个齿轮）在**两种口径下都绿** ⇒ 补的正是区分得开的那一条。
            var gearN = FindChild(FindChild(bar, "SettingsBtn"), "Image");
            var gwb = gearN != null ? gearN.GetComponent<WindowButton>() : null;
            CheckTrue(gwb != null && gwb.onClick != null, "（前提）齿轮的点击在");
            if (gwb != null && gwb.onClick != null)
            {
                gwb.onClick();
                var setA = menu.OpenSettings();                     // 与点钮同一份缓存（`Create()` 里赋值 `Instance`）
                CheckTrue(setA != null && setA.CurrentState == WindowState.Open, "（前提）点齿轮 ⇒ 开了一扇设置窗");
                gwb.onClick();
                CheckTrue(menu.OpenSettings() == setA, "（对照）**还开着**时再点齿轮 ⇒ 同一扇（命中复用那一支）");
                setA.Close();
                gwb.onClick();
                var setB = menu.OpenSettings();
                CheckTrue(setB != null && setB != setA,
                          "★ 设置窗**关掉之后再点齿轮 ⇒ 新建一扇**（原版关窗删缓存条目；`SettingsWindow.Instance` 是"
                        + "静态的、而 `Close()` 不销毁对象 ⇒ 实现退回「`Instance != null` 就复用」时**只有这条红**）");
                CheckTrue(SettingsWindow.Instance != setA,
                          "…`SettingsWindow.Instance` 也换到了新那一扇（它正是那种写法会读的静态字段）");
                if (setB != null) setB.Close();                     // 收尾：不留开着的设置窗
            }

            // ---- ⑥ 🆕 **A177 尾巴**（2026-10-11）：两份缓存**已合并成一份** ⇒ 跨入口去重 ----
            //   判据 = 原版那个缓存字段**只有一个**：`WindowsManager.automaticallyLoadedWindows`
            //   （键 = prefab 引用，`dump.cs` 偏移 0x68）⇒ **「从哪条入口点开」不影响命中**。
            //   合并前：`MainMenuRuntime` 自己一份 `_openByRef`、`WindowsManager` 自己一份 ⇒
            //   「社交窗里的聊天入口」与「主菜单右上角那颗 `ChatPreview`」各开一扇（**实测就是两扇**）。
            //   合并后：`MainMenuRuntime.OpenByRef` 只剩一条转调 `WindowsManager.OpenByRef` ⇒ 同一扇。
            //   🔴 **改坏法**：把 `MainMenuRuntime.OpenByRef` 写回它自己那份 `_openByRef`（副本）⇒
            //      下面第一条立刻红（两个返回值是两扇、`openWindows` 里 2 扇）。
            {
                WindowsManager.ClearReuseCacheForTest();     // 别让这一节之前的段留下的缓存漏进来
                var cp6 = menu.Find("ChatPreview");
                var ch6 = cp6 != null ? FindChild(cp6, "ChatHit") : null;
                var chatBtn6 = ch6 != null ? ch6.GetComponent<WindowButton>() : null;
                CheckTrue(chatBtn6 != null && chatBtn6.onClick != null, "（前提）主菜单右上那颗聊天钮接了点击");
                if (chatBtn6 != null && chatBtn6.onClick != null)
                {
                    chatBtn6.onClick();
                    var chatFromMenu = ChatPanel.LastOpened;
                    CheckTrue(chatFromMenu != null && chatFromMenu.CurrentState == WindowState.Open,
                              "（前提）点它 ⇒ 开出一扇聊天窗");
                    // 另一条入口 = 社交窗那份（`Shell/SocialWindow.cs` 的 `OpenChat`）——
                    // ⚠️ 它**只读那一份静态机制、不读实例状态** ⇒ 裸挂一个组件就够（同 `ShellScene` ⑤ 的写法）
                    var stubGo = new GameObject("A177 tail social stub");
                    var stub = stubGo.AddComponent<SocialWindow>();
                    var chatFromSocial = stub.OpenChat();
                    CheckTrue(chatFromSocial != null && chatFromSocial == chatFromMenu,
                              "★ A177 尾巴：**社交窗那条入口**与**主菜单那条入口**开出来的是**同一扇**"
                            + "（两份缓存已合并到 `WindowsManager` 那一份 —— 原版只有一个 "
                            + "`automaticallyLoadedWindows`；`MainMenuRuntime` 那 8 条入口也吃同一份）；"
                            + "改坏法：`MainMenuRuntime.OpenByRef` 写回自己那份 `_openByRef` ⇒ 两个返回值是两扇 ⇒ 红");
                    int nChat6 = 0;
                    if (WindowsManager.Instance != null)
                        foreach (var w in WindowsManager.Instance.openWindows)
                            if (w is ChatPanel) nChat6++;
                    CheckTrue(nChat6 == 1, $"…而且「开着的窗」里聊天窗只有 **1** 扇（实测 {nChat6}）");
                    chatFromMenu.Close();
                    var chatAfter6 = menu.OpenChat();
                    CheckTrue(chatAfter6 != null && chatAfter6 != chatFromMenu,
                              "★ 关掉之后再开 ⇒ **新建一扇**（原版关窗会删缓存条目 —— 合并之后这条语义不变；"
                            + "实现退回「实例还在就复用」⇒ 红）");
                    if (chatAfter6 != null) chatAfter6.Close();
                    Object.DestroyImmediate(stubGo);
                }
                WindowsManager.ClearReuseCacheForTest();     // 收尾：别把这一节的缓存留给后面的段
            }
        }

        Section("聊天窗 `ChatPanel`（入口 = 主菜单右上 `ChatPreview` 那颗钮 —— **原来没接点击**）");
        {
            var cp = menu.Find("ChatPreview");
            var ch = cp != null ? FindChild(cp, "ChatHit") : null;
            var cwb = ch != null ? ch.GetComponent<WindowButton>() : null;
            CheckTrue(cwb != null && cwb.onClick != null, "`ChatPreview` 那颗钮**接了点击**");
            if (cwb != null && cwb.onClick != null)
            {
                cwb.onClick();
                var chatWin = ChatPanel.LastOpened;
                CheckTrue(chatWin != null && chatWin.CurrentState == WindowState.Open, "点它 ⇒ **真的开了聊天窗**");
                if (chatWin != null)
                {
                    Check(chatWin.type, WindowType.Popup, "`type` = **1 Popup**（⚠️ 与社交窗相反）");
                    Check(chatWin.placement, WindowsPlacement.Canvas, "`windowsPlacement` = **5 Canvas**");
                    CheckNear(chatWin.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1**");
                    var ct = chatWin.transform;
                    CheckAtWorld(FindChild(ct, "Holder"), -78.12f, 1893.88f, -4f, 1076f, "`Holder`");
                    CheckAtWorld(FindChild(ct, "CloseBackground"), -2056.50f, 3872.26f,
                                 -651.18f, 1723.18f, "`CloseBackground`（纯色 (0,0,0,0.518)，点外关闭）");
                    var chol = FindChild(ct, "Chat");
                    CheckAtWorld(chol, 563.88f, 1863.88f, 146f, 1076f, "`Chat`（1300×930）");
                    CheckAtWorld(FindChild(chol, "ChatBackground"), 563.88f, 1863.88f, 146f, 1076f,
                                 "`ChatBackground`（`Chat_background` 九宫 138,113,137,107）");
                    // 两个频道键：VLG align UpperRight + reverse ⇒ 165×157.684，顶 182.82，从上往下
                    for (int i = 0; i < ChatPanel.Channels.Length; i++)
                    {
                        float top = 182.82f + 157.684f * i;
                        CheckAtWorld(FindChild(FindChild(chol, "Tab Buttons"), "Orange Tab Toggle " + i),
                                     425.71f, 590.71f, top, top + 157.684f, $"频道键 {i}（{ChatPanel.Channels[i]}）");
                    }
                    // ============================================================ 🆕 2026-10-07（A118①）
                    // **四态换图**：上面只断了矩形 —— 「底图常驻 + 选中/悬停/按下各换哪张」此前**一条都没被断**。
                    // 判据 = 原版那颗 `EverguildToggle` 的四个 sprite 字段（两份 prefab **逐字相同**）：
                    //   `offSprite 40K_settings_button` · `onSprite 40K_settings_button_hover` ·
                    //   `m_HighlightedSprite 40K_settings_button_selected` · `m_PressedSprite 40K_settings_button_pressed`；
                    //   `EverguildToggle__ToggleSprite.c` = `Image.set_sprite(isOn ? on : off)` ⇒ **选中换的是贴图、不是显隐**
                    //   （旧实现 `SetActive(i == sel)` 就是下面第 1 条的真红点）。
                    {
                        var tabCol = FindChild(chol, "Tab Buttons");
                        var tabBg = new ImageQuad[ChatPanel.Channels.Length];
                        var tabWb = new WindowButton[ChatPanel.Channels.Length];
                        for (int i = 0; i < tabBg.Length; i++)
                        {
                            var tog = FindChild(tabCol, "Orange Tab Toggle " + i);
                            var bgNode = tog != null ? FindChild(tog, "button_bg") : null;
                            tabBg[i] = QuadOf(bgNode);
                            CheckTrue(bgNode != null && bgNode.gameObject.activeSelf && tabBg[i] != null
                                      && tabBg[i].Texture != null,
                                      $"频道键 {i} 的底图 `button_bg` **两态都在**（原版换贴图、不换显隐；"
                                    + "旧实现 `SetActive(i == sel)` ⇒ 未选中那一层**根本不在** ⇒ 这条红）");
                            var hitN = tog != null ? FindChild(tog, "Hit") : null;
                            tabWb[i] = hitN != null ? hitN.GetComponent<WindowButton>() : null;
                            // 🔴 **防静默**：`MenuDraw.Hit` 不传 `target` 时 `Bind` **直接 return** ⇒
                            //    悬停 / 按下两档**无声消失**（画面错、`AuditHoverSwap` 也看不见它 —— 只有这条抓得住）。
                            CheckTrue(tabWb[i] != null && tabWb[i].target != null,
                                      $"频道键 {i} 的 `WindowButton.target` **非空**（空 = 悬停/按下两档无声消失）");
                            CheckTrue(tabWb[i] != null && tabWb[i].target == tabBg[i],
                                      $"…而且 target 指的就是那颗 `button_bg`（原版 `spriteToChange` 指同一张 Image）");
                        }
                        string tb0 = tabBg.Length > 0 && tabBg[0] != null && tabBg[0].Texture != null
                                   ? tabBg[0].Texture.name : "<null>";
                        string tb1 = tabBg.Length > 1 && tabBg[1] != null && tabBg[1].Texture != null
                                   ? tabBg[1].Texture.name : "<null>";
                        CheckTrue((tb0 == "40K_settings_button" && tb1 == "40K_settings_button_hover")
                               || (tb0 == "40K_settings_button_hover" && tb1 == "40K_settings_button"),
                                  "两颗底图 = **一颗 `40K_settings_button`（未选中）+ 一颗 `…_hover`（选中）**"
                                + $"（原版 `offSprite` / `onSprite`；⛔ 不写死「第 0 颗选中」—— 那是我们的初始态）"
                                + $" —— 实测 [{tb0}] / [{tb1}]");
                        // 切页 ⇒ **换图**（双向都换得回来）
                        if (tabBg.Length > 1 && tabBg[0] != null && tabBg[1] != null
                            && tabWb[0] != null && tabWb[0].onClick != null
                            && tabWb[1] != null && tabWb[1].onClick != null)
                        {
                            tabWb[1].onClick();
                            CheckTrue(tabBg[1].Texture != null && tabBg[1].Texture.name == "40K_settings_button_hover"
                                      && tabBg[0].Texture != null && tabBg[0].Texture.name == "40K_settings_button",
                                      "★ 点第 2 个频道键 ⇒ **底图真换过去了**（原版 `ToggleSprite` 那一刻换的是贴图）");
                            tabWb[0].onClick();
                            CheckTrue(tabBg[0].Texture != null && tabBg[0].Texture.name == "40K_settings_button_hover"
                                      && tabBg[1].Texture != null && tabBg[1].Texture.name == "40K_settings_button",
                                      "★ 再点回第 1 个 ⇒ **两张都换回来**（切页换图是双向的）");
                        }
                        // 🔴 切页换图之后**必须 `SetNormalTex`**（否则离开悬停会还原成**切页前**那张）——
                        //   `AuditHoverSwap` 比的是「调用前那张图」，顺手就能抓到它（本窗此前**没有**这张网）。
                        CheckHoverSwap(chol, "聊天窗（含两个频道键）");
                    }
                    // `Enter Text`：**运行期**矩形（序列化高是 0，真值 66.53 —— 判据 ④）
                    var ent = FindChild(chol, "Enter Text");
                    CheckAtWorld(ent, 613.88f, 1813.88f, 959.47f, 1026.00f, "`Enter Text`（**运行期** 66.53 高）");
                    CheckAtWorld(FindChild(ent, "Button"), 1750.38f, 1790.38f, 972.735f, 1012.735f, "发送钮（40×40）");
                    var po = FindChild(chol, "Player Options Panel");
                    CheckTrue(po != null && !po.gameObject.activeSelf, "`Player Options Panel` 出厂 **act F**");
                    var cb3 = FindChild(FindChild(chol, "Generic Close Button Orange"), "Hit");
                    CheckTrue(cb3 != null && cb3.GetComponent<WindowButton>() != null, "右上那颗圆钮**接了关闭**");

                    // ★ 挑战弹窗：从选项面板的 `Challenge` 钮开（原版 `ChallengeManager.OpenStartChallengeWindow`）
                    po.gameObject.SetActive(true);
                    var chal = FindChild(FindChild(FindChild(po, "Buttons"), "Challenge"), "Hit");
                    var chb = chal != null ? chal.GetComponent<WindowButton>() : null;
                    CheckTrue(chb != null && chb.onClick != null, "`Challenge` 键接了点击");
                    if (chb != null) chb.onClick();
                    var duel = DuelPopupWindow.LastOpened;
                    CheckTrue(duel != null && duel.CurrentState == WindowState.Open, "点 `Challenge` ⇒ **开好友挑战弹窗**");
                    if (duel != null)
                    {
                        // 🆕 A47：压暗层命中区 —— 档 = `QPanel`(3400)（= 压暗层自己那一档），< `QHit`(3405)
                        //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
                        MenuDraw.CheckShadeRule(CheckTrue, "好友挑战弹窗", FindChild(duel.transform, "CloseHit"),
                                                duel.transform.Find("Menu Dark Background"), DuelPopupWindow.QHit);
                        Check(duel.type, WindowType.Popup, "`type` = **1 Popup**");
                        Check(duel.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                        CheckNear(duel.extraScaleSmallScreen, 1.15f, 1e-4f,
                                  "`extraScaleSmallScreen` = **1.15**（⚠️ 逐窗实测，不是 1.07/1.0）");
                        var dt = duel.transform;
                        var dwin = FindChild(dt, "Window");
                        CheckAtWorld(dwin, 535f, 1385f, 245f, 675f, "`Window`（850×430）");
                        CheckAtWorld(FindChild(dwin, "Generic Popup Background"), 535f, 1385f, 245f, 675f,
                                     "`Generic Popup Background`（`40k_popup` 九宫 169,160,169,160）");
                        // `MessageText`：全文 + `52.5` 号（`{0}` 已被对手名替换）
                        CheckText(TextOf(FindChild(dwin, "MessageText")),
                                  string.Format(DuelPopupWindow.MessageFormat, "Everrookie2"),
                                  "`MessageText`（**原档那句的全文**，`{0}` 换成了被挑战者）");
                        var dbtns = FindChild(dwin, "Buttons");
                        CheckAtWorld(FindChild(dbtns, "Button Skirmish"), 591.15f, 941.15f, 567f, 643f,
                                     "`Button Skirmish`（HLG 排出来的位：x 591.15..941.15 —— 组左沿 572.30 + `offsetInCell` 18.85）");
                        CheckAtWorld(FindChild(dbtns, "Button Classic"), 978.85f, 1328.85f, 567f, 643f,
                                     "`Button Classic`（x 978.85..1328.85 —— 组右沿 1347.70 − 18.85；⚠️ **不是**「以窗心 960 对称」）");
                        CheckText(TextOf(FindChild(dbtns, "Button Classic")), "Continue",
                                  "⚠️ `Button Classic` 的文案是 **`Continue`**（节点名叫 `Classic` —— 原档如此）");
                        CheckAtWorld(FindChild(dwin, "Generic Rounded Button Green"), 1341.80f, 1416.80f,
                                     212.10f, 287.10f, "右上那颗 75×75 绿圆钮（**判为关闭钮**）");
                        // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                        //   期望矩形 = **原版 prefab** `MessagePopupWindowDuel > Window >
                        //   Generic Popup Background` 那颗 `Image` 的 rect（535,245 → 1385,675）；
                        //   ⛔ 不写 `DuelPopupWindow.WindowR`（那是被测实现**传进去的实参**，同式自证）。
                        CheckAbsorbRule("好友挑战弹窗", duel.transform, "AbsorbHit",
                                        535f, 245f, 1385f, 675f,
                                        DuelPopupWindow.QPanel, DuelPopupWindow.QHit, () => duel.CurrentState);
                        duel.Close();
                    }
                    // 🔴 **消息行的五级阶梯**（2026-09-28 从原版**兄弟序**读出 —— 这条以前记的是「原版关服、
                    //    没有消息行可看 ⇒ 没尺子」，其实**尺子在 bundle 里**）：
                    //    · 原版 `ChatMessageRow` 的 `m_Children`（`bundle_mainmenualwaysloaded_assets_all/
                    //      RectTransform/RectTransform_7760131448890879999.json:22-39`）＝
                    //        `RowBackground`(0) → `Player Header`(1) → `Friend Header`(2) → `Message`(3)
                    //    · 头部内（`RectTransform_-6649793586198712321.json`）＝ `Sender`(0) → `Time`(1) → `Profile border`(2)，
                    //      而**框是 `Profile border` 自己身上的 Image**、立绘（`Profile content`）是它**唯一**的子节点
                    //    ⇒ 画序 = **行底 < 信使名/时间 < 框 < 立绘 < 正文**（头像压文字、正文压过头像）。
                    //    ⚠️ 本窗默认 **0 条消息**（数据留空态）⇒ 自检自己塞一条（同 `LeaderboardData.InjectForTest`），验完清掉。
                    int Q(Transform p, string n)
                    {
                        var t = FindChild(p, n);
                        if (t == null) return -1;
                        // ⚠️ 这条行里**两种渲染后端都有**：底/框/立绘是 `ImageQuad`，信使名/时间/正文是 `Label`
                        //    —— 第一版只读了 `ImageQuad` ⇒ 三个文字节点全读到 -1（自检当场抓出来）。
                        var lb = t.GetComponentInChildren<Label>();
                        if (lb != null) return lb.RenderQueue;
                        var iq = t.GetComponentInChildren<ImageQuad>();
                        return iq != null ? iq.RenderQueue : -1;
                    }
                    SocialData.ChatMessages.Add(new SocialData.ChatMessage
                    {
                        Channel = "Global", Sender = "LadderProbe", Time = "0d 0h",
                        Text = "queue ladder probe", Mine = false, Height = 0f,
                        // ⚠️ **必须给一张真头像**：`MenuDraw.Rect` 在贴图为 null 时**连节点都不建**
                        //    ⇒ 不给的话「立绘」那一层整条不存在，断言读到 -1（第一版就是这么红的）。
                        AvatarArt = ProfileData.AvatarArt,
                    });
                    chatWin.RefreshMessages();
                    var cRow = FindChild(chol, "ChatMessageRow");
                    CheckTrue(cRow != null, "塞一条消息 ⇒ **消息行建出来了**（`Chat Tab Global/Viewport/Content` 下）");
                    if (cRow != null)
                    {
                        var qRowBg = Q(cRow, "RowBackground");
                        var qSender = Q(FindChild(cRow, "Friend Header"), "Sender");
                        var qTime = Q(FindChild(cRow, "Friend Header"), "Time");
                        var qBorder = Q(FindChild(cRow, "Profile border"), "Border");
                        var qAvatar = Q(FindChild(cRow, "Profile border"), "Profile content");
                        var qMsg = Q(cRow, "Message");
                        CheckTrue(qRowBg >= 0 && qSender > qRowBg && qTime > qRowBg,
                                  $"行底 **{qRowBg}** < 信使名/时间 **{qSender}/{qTime}**（原版 `RowBackground` 是 sibling 0）");
                        CheckTrue(qBorder > qSender && qAvatar > qBorder,
                                  $"信使名/时间 **{qSender}** < 框 **{qBorder}** < 立绘 **{qAvatar}**"
                                  + "（头像压文字：原版 `Profile border` 是头部第 3 个兄弟，框在立绘之前）");
                        CheckTrue(qMsg > qAvatar,
                                  $"正文 **{qMsg}** > 头像 **{qAvatar}**（原版 `Message` 是行里最后一个兄弟 ⇒ **正文压过头像**）");
                    }
                    SocialData.ChatMessages.Clear();
                    chatWin.RefreshMessages();
                    chatWin.Close();
                }
            }
        }

        Section("对局历史弹窗 `Battle Log Popup`（🔴 **界面里没有入口** —— 原版的打开点查不到，见下）");
        {
            // 🔴 这一扇**不是**从哪个钮点开的：普查 §E 把它只在 `WindowsManager` 预载表里这件事查实了，
            //    而那个 `OpenWindow<BattleLogPopup>()` 的产物缺失 ⇒ 我们**不编入口**，自检直接开。
            var blp = menu.OpenBattleLogPopup();
            CheckTrue(blp != null && blp.CurrentState == WindowState.Open, "开得起来");
            if (blp != null)
            {
                Check(blp.type, WindowType.Popup, "`type` = **1 Popup**");
                Check(blp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                CheckTrue(blp.closeOnEsc, "`closeOnESC` = **1**");
                CheckNear(blp.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1.0**");
                var bt2 = blp.transform;
                CheckAtWorld(FindChild(bt2, "Menu Dark Background"), -1327.30f, 3247.30f, -746.18f, 1826.18f,
                             "压暗层（纯色 (0,0,0,0.773)）");
                var bc = FindChild(bt2, "Content");
                CheckAtWorld(bc, 135f, 1785f, 55f, 1055f, "`Content`（1650×1000）");
                CheckAtWorld(FindChild(bc, "Background"), 135f, 1785f, 55f, 1055f,
                             "`Background`（`UI_Deck_Information_Back` 九宫 42,363,655,81）");
                var cb4 = FindChild(bc, "Close Button");
                CheckAtWorld(cb4, 1685f, 1815f, 25f, 155f, "`Close Button`（**130×130** —— 比常规那颗大）");
                CheckAtWorld(FindChild(cb4, "Background"), 1699.49f, 1798.59f, 39.06f, 138.74f,
                             "可见圆 `Background`（⚠️ **略偏左上**：左缝 14.49 / 右缝 16.41 —— 原版如此）");
                CheckTrue(FindChild(cb4, "Icon") != null, "`Icon`（`40k_general_bt_yellow_close`）");
                CheckAtWorld(FindChild(bc, "Matches"), 235f, 1710f, 130f, 980f, "`Matches`（`ScrollRect`）");
                CheckAtWorld(FindChild(FindChild(bc, "Matches"), "Viewport"), 235f, 1710f, 130f, 963f,
                             "`Viewport`（`UIMask` + `showGraphic=0`）");
                // 空态：本地 0 条 ⇒ 一行都不建（原版这扇窗也没有空态节点）
                Check(blp.BuiltRows, 0, "本地 **0 条** ⇒ 一行都不建");
                // 喂一条 ⇒ 建出一行，**几何与档案窗那一页同一份 `MatchLogRow`**
                BattleLogData.Add(new BattleLogData.Match
                {
                    Result = BattleLogData.Outcome.Victory, OwnHeroName = "Uriel Ventris",
                    EnemyHeroName = "Ghazghkull Thraka", OwnName = "Test Commander", EnemyName = "Bot",
                    OwnSkulls = 3, EnemySkulls = 1, OwnScore = "987 (+12)", EnemyScore = "Gold IV",
                    Mode = "Skirmish mode",
                });
                blp.RebuildForTest();
                Check(blp.BuiltRows, 1, "喂一条 ⇒ **建出一行**");
                var bvp = FindChild(FindChild(bc, "Matches"), "Viewport");
                var brow = FindChild(FindChild(bvp, "Content"), "Match Log");
                CheckAtWorld(brow, 235f, 1710f, 130f, 333.20f,
                             "行矩形（行高 **203.20**、从视口顶边起 —— 与档案窗那一页逐值相同）");
                CheckText(TextOf(FindChild(brow, "Result")),
                          BattleLogData.ResultText(BattleLogData.Outcome.Victory), "行里那行结果");
                BattleLogData.ResetForTest();
                blp.RebuildForTest();
                Check(blp.BuiltRows, 0, "清空 ⇒ 行又没了");

                // ---- 🆕 2026-10-03（本件 ②）：**视口外的整行不建** ----
                // 这一棵树上**原来没有**这道守卫（档案窗那一页有）⇒ 整行在视口外的那些照样被逐件建出来，
                // 每个 quad 各自靠 `RowCtx.Clip` 截 ⇒ 白建几十个节点，而且它们本来就画不到（原版 `RectMask2D`）。
                // 🔴 期望值**现算**（`RowsInViewport`）：视口 = `ViewportR` 130..963（上面 `CheckAtWorld` 刚按原版值钉过）、
                //    行距 = 原版 203.20 + 25。⛔ 不写死条数。
                {
                    const int NF = 6;
                    for (int i = 0; i < NF; i++)
                        BattleLogData.Add(new BattleLogData.Match
                        {
                            Result = BattleLogData.Outcome.Victory,
                            OwnHeroName = "Feed " + i, EnemyHeroName = "Bot",
                            OwnName = "Feed " + i, EnemyName = "Bot",
                            OwnSkulls = 1, EnemySkulls = 0, OwnScore = "1", EnemyScore = "2",
                            Mode = "Skirmish mode",
                        });
                    blp.RebuildForTest();
                    const float FTop = 130f, FBot = 963f;      // = `ViewportR`（原版值）
                    float fpitch = OrigMatchRowH + OrigMatchRowGap;    // 原版：203.20 + 25
                    int wantF = RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, 0f);
                    Check(blp.BuiltRows, wantF, $"喂 {NF} 行 ⇒ 建了与视口相交的那 {wantF} 行（现算）");
                    CheckTrue(wantF > 0 && wantF < NF,
                              $"…而且**确实有整行落在视口外**（{NF - wantF} 行连节点一起不建 —— "
                              + "原来它们是「逐件建出来、再被自己的 `Clip` 截掉」，白建还得画到框外）");
                    var fc = FindChild(bvp, "Content");
                    int fn = 0, fout = 0; float fmin = float.MaxValue;
                    if (fc != null)
                        foreach (var rt in fc.GetComponentsInChildren<Transform>(true))
                        {
                            if (rt.name != "Match Log") continue;
                            fn++;
                            float cy = LayoutSpace.PxY(rt.position.y);
                            float y1 = cy - OrigMatchRowH * 0.5f, y2 = cy + OrigMatchRowH * 0.5f;
                            fmin = Mathf.Min(fmin, y1);
                            if (y2 <= FTop + 0.01f || y1 >= FBot - 0.01f) fout++;
                        }
                    Check(fn, blp.BuiltRows, "行节点个数 == `BuiltRows`（两者不许各说各的）");
                    CheckTrue(fout == 0, $"每一颗建出来的行都与视口相交（越界 {fout} 颗）");
                    CheckNear(fmin, FTop, 0.5f, "最上面那颗行的顶边 = 视口顶 130（顺带证明偏移是 0）");

                    // ---- 🆕 2026-10-03（A25②）：**这一格补上了滚动区**（改之前连 `MenuScroll` 都没有）----
                    // 判据（原版）：`Matches` 上是 `ScrollRect` `h=0 v=1` · **`m_MovementType=2`(Clamped)** ·
                    //   灵敏度 1.0（普查 §B 表 · §D6 的对照表）；内层 `Content` 挂 `ContentSizeFitter
                    //   m_VerticalFit=1` ⇒ **可滚范围 = 内容高 − 视口高**。
                    // 🔴 **改之前**：一处滚动都没有 ⇒ 滚不动，而「整行在视口外 ⇒ 不建」那道守卫又把后面的行
                    //   **彻底藏掉** ⇒ **第 5 行起永远看不到也点不到**。下面三条就是它的验收。
                    {
                        var rs = blp.RowsScroll;
                        CheckTrue(rs != null, "★ 这一格**有滚动区了**（原版 = `ScrollRect`；改之前一处都没有）");
                        // 🆕 2026-10-04（A35⑦⑧）：**宿主必须设** —— 走 `SocialPage.RegisterScroll` 那一份登记
                        //   （原来这里是全批唯一一处直调 `PointerLayer.RegisterScroll`：那条路在指针层缺席时
                        //   `return` 得一声不响），而 `Owner` 空着会被 `PointerLayer.PruneScrolls` 当**死条目**
                        //   删掉（`PointerLayer.cs:218-227`）⇒ 滚轮永远落不上、画面却正常。这条钉住这一对不变量。
                        CheckTrue(rs != null && rs.Owner != null,
                                  "滚动区带宿主（`Owner` 为空 ⇒ 下一次 `PruneScrolls` 就把它删了，且原来不出声）");
                        CheckTrue(rs != null && rs.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                        CheckTrue(rs != null && !rs.Elastic,
                                  "**Clamped**（原版 `m_MovementType=2`）—— ⚠️ 别套档案窗那一页的 `1(Elastic)`（判据 ④）");
                        if (rs != null)
                        {
                            // 🔴 **2026-10-04（A35②）**：同上 —— 原来算的是 `NF * MatchLogRow.RowH + …`（同式自证），
                            //   现在改成从原版值重抄的 `OrigMatchRowH / OrigMatchRowGap`。
                            float contentH = NF * OrigMatchRowH + (NF - 1) * OrigMatchRowGap;
                            CheckNear(rs.MaxOffset, contentH - (FBot - FTop), 0.5f,
                                      $"可滚范围 = 内容高（{NF}×203.20 + {NF - 1}×25 = {contentH:F2}）− 视口高 {FBot - FTop:F2}"
                                      + " —— **补之前这里恒 0**（滚轮/拖拽全被夹回 0）");
                            CheckTrue(rs.MaxOffset > 1f, "★ 确实**滚得动**了");

                            // ① 滚一格走**真路**：指针层命中这一格 → `MenuScroll.Wheel` → `OnChanged` 重建
                            float topBefore = TopRowTop(fc);
                            var pl2 = PointerLayer.Instance;
                            // 先钉「视口中心命中的就是这一格」—— 免得滚轮落到别的窗口的滚动区上、把下面几条判成假红
                            CheckTrue(pl2 != null && pl2.ScrollUnder(972.5f, 546.5f) == rs,
                                      "视口中心（972.5,546.5）上命中的滚动区**就是这一格**");
                            bool wHit = pl2 != null && pl2.WheelAt(972.5f, 546.5f, -120f);   // 视口中心 = 235..1710 × 130..963
                            CheckTrue(wHit, "滚轮落在这一格上（`PointerLayer.WheelAt`）");
                            CheckTrue(rs.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {rs.Offset:F2}px）");
                            CheckNear(topBefore - TopRowTop(fc), rs.Offset, 0.5f,
                                      "★ **滚动之后行真的换了位置**：内容往上走的像素数 == 滚动偏移"
                                      + "（⚠️ 只看容器不够 —— 「底板走了、行没走」那一类只有这条抓得到）");
                            Check(blp.BuiltRows, RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, rs.Offset),
                                  "滚一格之后在建的行数 == 现算值（重建是**先清再建**，不清会越滚越多）");
                            rs.SetOffset(0f);
                            CheckNear(TopRowTop(fc), FTop, 0.5f, "滚回 0 ⇒ 行回到视口顶（下面的期望值按这个前提算）");

                            // ② 滚到最下 ⇒ 建的是**另一批**行（原来最后那几行根本够不到）
                            rs.SetOffset(rs.MaxOffset);
                            CheckNear(rs.Offset, rs.MaxOffset, 0.01f, "滚到了最下（`SetOffset` 没被夹回去）");
                            int wantBot = RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, rs.Offset);
                            Check(blp.BuiltRows, wantBot,
                                  $"滚到最下 ⇒ 仍然**恰好建了与视口相交的那几行**（现算 {wantBot} 行，偏移 {rs.Offset:F2}）");
                            // 🔴 2026-10-04（**A42 定案：红的是断言、不是实现**）：下面两条原来的期望值**写反了** ——
                            //   它们按「`Add` 是**追加**」写（以为 `Feed {NF-1}` 是列表最后一行）。实测：
                            //   `BattleLogData.Add` 是 **`Insert(0, …)` = 新的在前**（`Shell/BattleLogData.cs:69/77`），
                            //   而行按 `All` 的顺序**自上而下**排（`BuildRows`：`y = ViewportR.y1 + i*pitch`；
                            //   这里的 px 是**上小下大** —— 上面那两行常量自己写着 `FTop=130 / FBot=963`）
                            //   ⇒ **列表最后一行 = 最早那条 = 第一个喂进去的 `Feed 0`**；最新的 `Feed {NF-1}` 在列表**最上面**。
                            //   ⚠️ 所以实现与数据源那句「最近一局在最上面」自洽，**别去改 `BuildRows`**。
                            //   ✅ 顺带把这条从【弱断言】变成**真判据**：它现在**分得出「偏移 0」与「偏移最大」**
                            //   （两种情况下都建 4 行 ⇒ 老写法两种都绿；偏移 0 建的是 `Feed 5/4/3/2`，新写法会红）。
                            string newestFed = "Feed " + (NF - 1);   // 喂进去的**最后**一条 = 最新那局 = 列表的**首行**
                            string oldestFed = "Feed 0";             // 喂进去的**第一**条 = 最老那局 = 列表的**末行**
                            var feed = new List<string>();
                            Transform lastRow = null;
                            foreach (var rt in fc.GetComponentsInChildren<Transform>(true))
                            {
                                if (rt.name != "Match Log") continue;
                                var hn = FindChild(FindChild(rt, "Player Info"), "Hero Name");
                                var hl = hn != null ? hn.GetComponentInChildren<Label>() : null;
                                if (hl == null) continue;
                                feed.Add(hl.Text);
                                if (hl.Text == oldestFed) lastRow = rt;   // 「最后那一行」= 列表末行 = 最老那条
                            }
                            CheckTrue(feed.Count == blp.BuiltRows, "滚完之后行节点个数**仍然** == `BuiltRows`");
                            CheckTrue(!feed.Contains(newestFed) && feed.Contains(oldestFed),
                                      $"★ 滚到最下**看得见最后一行了**（最新那条 `{newestFed}` 已滚出视口上边、最早那条 `{oldestFed}` 进来；建出来的："
                                      + string.Join("/", feed.ToArray()) + "）—— 改之前这一行**永远看不到也点不到**");
                            // 「点得到」那一半：那一行的 `ReplayButton/Hit` 真的在（整行在视口外的连 `Hit` 都不建）
                            var lastHit = lastRow != null ? FindChild(FindChild(lastRow, "ReplayButton"), "Hit") : null;
                            var lastBtn = lastHit != null ? lastHit.GetComponent<WindowButton>() : null;
                            CheckTrue(lastBtn != null && lastBtn.onClick != null,
                                      "★ **列表末行**（= 最老那条 `" + oldestFed + "`）的 `ReplayButton` **命中区也在**"
                                      + "（原来它连节点都建不出来 ⇒ 点不到）");
                            rs.SetOffset(0f);
                        }
                    }

                    BattleLogData.ResetForTest();
                    blp.RebuildForTest();
                    Check(blp.BuiltRows, 0, "清空 ⇒ 行又没了（自检不留假数据）");
                }
                Check(blp.MissingArt.Count, 0, "弹窗**没有取不到的图**");
                CheckHoverSwap(blp.transform, "战斗日志弹窗");
                blp.Close();
            }
        }

        Section("图：一张都不能少");
        Check(menu.MissingArt.Count, 0, "没有取不到的图（取不到的件**根本没画**，所以这条必须 0）");
        // 🆕 A17：主菜单自己的那些键原版**全是 ColorTint**（普查 §块 1：5 个导航键/齿轮/收件箱/头像/ChatPreview 都 `trans=1`）
        // ⇒ 本窗**不该**出现换图按钮；这一条盯的是「别把色偏档也接上换图」+ 全局「悬停图一张都不缺」。
        CheckTrue(WindowButton.MissingSwapArt.Count == 0, "悬停图一张都不缺（缺的：" +
                  string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");
        Section("染色与字号（§七：**原版靠 `Image.m_Color` 把亮图染暗**，不补就会渲成白块）");
        CheckTint(FindChild(nav, "Panel Shadow"), new Color(0f, 0f, 0f, 0.46667f), 0.002f,
                  "`Panel Shadow` 染成半透明纯黑（`m_Color (0,0,0,0.4667)`）");
        var sep = FindChild(nav, "Separators Left");
        CheckTint(sep, new Color(0.64706f, 0.38039f, 0.26275f, 1f), 0.002f, "分隔线染成古铜色");
        CheckTint(FindChild(prof, "Planer Name Background"), new Color(0.39623f, 0.19251f, 0.30954f, 1f), 0.002f,
                  "名字条底染成暗紫红");
        var shadowInner = FindChild(nav, "Background");
        var navQuad = shadowInner != null ? shadowInner.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(navQuad != null && !IsAllWhiteVerts(navQuad),
                  "侧栏底板用了**四角顶点色**（原版 `MB1941`：只补 `m_Color` 补不出这块板）");
        Check(CountByName(root, "Selected highlight"), 1,
              "`Selected highlight` **只有 1 个**（= 选中态；原版 5 个出厂都 active，可见性由 Toggle 驱动）");
        // ⚠️ 开了 autosize 之后**具体多大是 TMP 自己算的**（原版那个 32 也只是「框里的存量」）⇒ 断言要比**区间**
        CheckFontInRange(FindChild(prof, "Player Name"), 10f, 32f, "`Player Name` 字号落在原版 autosize 区间 10→32 内");
        CheckFont(FindChild(chat, "Message Preview"), 18f, "聊天两行字号 = 18px（`auto=0`，不缩）");
        CheckFits(FindChild(prof, "Player Name"), 265f, "`Player Name` 放得进 265px 的框");
        CheckFits(FindChild(chat, "Message Preview"), 327.3f, "聊天行放得进 327.3px 的框");
        // 🆕 2026-10-07（波 8 · Label 折行族）—— A62 主表 #25 + A77⑩ 子表 E1/E2（调度台裁定选 (a)：**补 `SetWrapping(false)`**）。
        // 判据（第一手字段，不是转抄）= `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`：
        //   · `MonoBehaviour_1717.json`（顶栏 `Player Name`）：`m_TextWrappingMode = 0`
        //   · `MonoBehaviour_1795.json` / `_1814.json`（`Message Preview{, (1)} > text`）：
        //     `m_TextWrappingMode = 0` **且** `m_enableAutoSizing = 0`（我们原来**主动**给了折行宽 ⇒ **多折**）
        // 🔴 真红法：把 `MainMenuRuntime` 里那三条 `SetWrapping(false)` 删掉 ⇒ 前两条/第三条各自变 1 ⇒ 红。
        CheckWrapMode(FindChild(prof, "Player Name"), 0,
                      "★ 顶栏 `Player Name` **不折行**（原版 `m_TextWrappingMode = 0`）");
        CheckWrapMode(FindChild(chat, "Message Preview"), 0,
                      "★ 聊天预览第 1 行 **不折行**（原版 `Message Preview > text` = 0）");
        CheckWrapMode(FindChild(chat, "Message Preview (1)"), 0, "★ 聊天预览第 2 行同（原版 `Message Preview (1) > text` = 0）");
        // 导航标签：原版 autosize 18→33，框 146.9px（`COLLECTION` 靠自适应缩小 —— 原版存的就是 30.45）
        CheckFits(FindChild(menu.Find("Main Menu Navigation Button - Home"), "Text"), 146.92f, "`PLAY` 放得进 146.9px 的条");
        CheckFits(FindChild(menu.Find("Main Menu Navigation Button - Collection"), "Text"), 146.92f,
                  "`COLLECTION` 放得进 146.9px 的条（**这条就是 autosize 的判据**）");
        CheckFits(FindChild(menu.Find("Base Game Mode Container 1x1 - Tutorial"), "Event Title"), 513.7f,
                  "模式卡标题放得进 513.7px 的框");

        // 🆕 2026-09-25：标题的**竖直位置**（`项目任务.md` §三 第 15 条 第 63 行）——
        // 原来只断**宽度**，位置错了也照绿。
        // 原版那行归 `TextDarkening` 上的 **`VerticalLayoutGroup`** 排
        // （`bundle_menus_assets_all` 实读：`m_Padding.m_Left 11` · `m_ChildAlignment 3 (MiddleLeft)`
        //  · `m_Spacing −4.2` · `m_ChildControlHeight 0`）；两个孩子逐个量过尺寸：
        // `Event Title` 高 **55.708** · `Timer With Time Description` 高 **40.729**，暗带高 **105.866**
        // ⇒ 内容 = 55.708 + 40.729 − 4.2 = **92.237**，在暗带里**垂直居中** ⇒ 上下各留 **6.814**；
        // 标题是**第一个孩子** ⇒ 顶在内容最上面 ⇒
        // **标题上边缘距暗带顶 = 6.814** · **下边缘 = 6.814 + 55.708 = 62.522**。
        // 🔴 这几个数**硬写在测试里**（不引用 `MainMenuRuntime` 的常量）—— 引用就成了自证。
        // ⚠️ 轴向差点搞反（2026-09-25 实测量过）：`RenderedRect` 的 y 是**上到下**
        //    （`LayoutSpace.PxY = 540 − y×108`）⇒ **`y1` 是上边缘、`y2` 是下边缘**；
        //    而 `MainMenuRuntime` 里 `by1` 是**暗带顶**、`by2` 是**暗带底**。
        //    「拿 PxX 的式子去量 y」在 2026-09-23 就踩过一次（`LayoutSpace.PxY` 的注释）。
        {
            var mcard = menu.Find("Base Game Mode Container 1x1 - Tutorial");
            float bx1, by1, bx2, by2, tx1, ty1, tx2, ty2;
            if (RenderedRect(FindChild(mcard, "TextDarkening"), out bx1, out by1, out bx2, out by2)
                && RenderedRect(FindChild(mcard, "Event Title"), out tx1, out ty1, out tx2, out ty2))
            {
                // ⚠️ 量的是 `Label.WorldH`（**字形盒**，比 55.708 那个布局盒略小）⇒ 容差 1.5px
                CheckNear(ty1 - by1, 6.814f, 1.5f,
                          "★ 标题**上边缘距暗带顶 6.81px**（原版 VLG：105.866 − 92.237 的一半）");
                CheckNear(ty2 - by1, 62.522f, 1.5f,
                          "★ 标题**下边缘距暗带顶 62.52px**（= 6.814 + 标题高 55.708）");
            }
            else CheckTrue(false, "量不到模式卡的 `TextDarkening` / `Event Title` —— 上面两条断言的前提");
        }
        MeasureText(FindChild(prof, "Player Name"), "Player Name", 32f);
        MeasureText(FindChild(chat, "Message Preview"), "Player Name: Message", 18f);
        MeasureText(FindChild(menu.Find("Base Game Mode Container 1x1 - Tutorial"), "Event Title"),
                    "TUTORIAL", 58.8f);
        MeasureText(FindChild(menu.Find("Main Menu Navigation Button - Home"), "Text"), "PLAY", 33f);
        MeasureText(FindChild(menu.Find("Main Menu Navigation Button - Collection"), "Text"), "COLLECTION", 30.45f);

        // ============================================================ §A92 节点类型（2026-10-07 新增）
        //
        // 🔴 **判据 = 原版自己的节点类型**（不是我们的常量）：`bundle_scenes_scenes_mainmenuwarpforge`
        //    实读 **592 个 `RectTransform` / 105 个裸 `Transform`**，而那 105 个清一色是**卡框 3D 子锚与粒子件**
        //    （名字逐个核过：`Card Info`/`Tactic Container`/`EffectAnchor`/`Textbackgrounds`/`MinionOrWarlord Container`
        //    ＋ `Wave*`/`Sparks`/`Glow*`）—— **一个菜单容器都没有**。
        //    ⇒ `MainMenuRuntime.New`（本文件建的那 20 个名字全走它）建的必须是 `RectTransform`。
        //    「原来是什么样」：`new GameObject(name)` = **裸 `Transform`** ⇒ 这些节点连 `rect` 都没有。
        // ⚠️ **反面那一半**（原版**就是**裸 `Transform` 的件不许被顺手改齐）在 `RewardsScene` 的 §A92 那节，
        //    那边是**成对**断的（`Particle System nebula` **没有** `RectTransform` / 上面两级宿主**有**）。
        // ⚠️ **名字逐个点过名**（不是拿一个当代表）：`New` 有 20 个调用点，这 5 个覆盖它的三支
        //    （导航面板 / 顶栏 / 模式卡容器）。
        Section("§A92 节点类型：`MainMenuRuntime.New` 建的空节点都是 `RectTransform`");
        {
            var a92np = menu.Find("Navigation Panel");
            CheckTrue(a92np != null && a92np.GetComponent<RectTransform>() != null,
                      "`Navigation Panel` 是 **`RectTransform`**（原版实读同之；原来建的是裸 `Transform`）");
            var a92bc = menu.Find("Buttons Container");
            CheckTrue(a92bc != null && a92bc.GetComponent<RectTransform>() != null,
                      "`Buttons Container` 是 **`RectTransform`**（同一支里的第二级）");
            var a92ub = menu.Find("Upper bar");
            CheckTrue(a92ub != null && a92ub.GetComponent<RectTransform>() != null,
                      "`Upper bar` 是 **`RectTransform`**（顶栏那一支的根）");
            var a92gm = menu.Find("GameModes");
            CheckTrue(a92gm != null && a92gm.GetComponent<RectTransform>() != null,
                      "`GameModes` 是 **`RectTransform`**（模式卡那一支的根）");
            var a92nb = menu.Find("Main Menu Navigation Button - Home");
            CheckTrue(a92nb != null && a92nb.GetComponent<RectTransform>() != null,
                      "`Main Menu Navigation Button - Home` 是 **`RectTransform`**"
                      + "（原版这个名字的组件实读就是 `RectTransform`）");
        }

        Shoot("01_主菜单.png");
        Debug.Log(P + menu.Dump());
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "   ✗ " + f);
        EditorApplication.Exit(_fail > 0 ? 1 : 0);
    }

    static void CheckNavButton(MainMenuRuntime menu, string name, float y1, float y2, string art)
    {
        var b = menu.Find("Main Menu Navigation Button - " + name);
        if (b == null) { CheckTrue(false, $"导航钮 `{name}` 建了"); return; }
        // ⚠️ 按钮那层是**纯容器**（没有 rect 语义，位置在原点）；带 §五 那个矩形的是它的子件。
        //    而 `Selected highlight` **只有选中的那个按钮才画**（2026-09-22 起）⇒ 这里探**五个都有**的两件。
        CheckAt(FindChild(b, "Text Background"), 9.3f, 156.2f, y1 + 118.3f, y1 + 157.7f, $"导航钮 `{name}` 的文案条");
        CheckAt(FindChild(b, "Badge Highlight"), 117.8f, 152.8f, y1 + 91.7f, y1 + 126.7f, $"导航钮 `{name}` 的红点");
        var img = FindChild(b, "Image");
        var q = img != null ? img.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(q != null && q.Texture != null && q.Texture.name == art, $"`{name}` 用的是 `{art}`");
    }

    // ============================================================ 🆕 2026-10-10（A284 / A222）
    // 导航钮那两件：**图标那一格的 quad** 与**挂在 `Hit` 上的那颗 `WindowButton`**。
    static ImageQuad NavIconQuad(MainMenuRuntime menu, string name)
    {
        var b = menu.Find("Main Menu Navigation Button - " + name);
        var img = FindChild(b, "Image");
        return img != null ? img.GetComponent<ImageQuad>() : null;
    }
    static WindowButton NavHitButton(MainMenuRuntime menu, string name)
    {
        var b = menu.Find("Main Menu Navigation Button - " + name);
        var hit = FindChild(b, "Hit");
        return hit != null ? hit.GetComponent<WindowButton>() : null;
    }

    /// <summary>量一颗 `ImageQuad` **渲出来**的像素矩形并比原版字面量（逐边 ±`tol` px）。
    /// 判据 = **量渲染真值**：`WorldW/WorldH` + 那颗 quad **自己的**世界坐标（`QuadPxRect`）
    /// —— ⛔ 不读源码常量、⛔ 不比承载节点的 `localPosition`（被裁时那个位置不动、会报假矩形）。</summary>
    static void CheckQuadRectPx(ImageQuad q, float x1, float x2, float y1, float y2, float tol, string what)
    {
        float a1, b1, a2, b2;
        if (!QuadPxRect(q, out a1, out b1, out a2, out b2))
        { CheckTrue(false, what + "：量不到渲染矩形（节点不在 / 底下没有 `ImageQuad`）"); return; }
        CheckTrue(Mathf.Abs(a1 - x1) <= tol && Mathf.Abs(b1 - y1) <= tol
                  && Mathf.Abs(a2 - x2) <= tol && Mathf.Abs(b2 - y2) <= tol,
                  what + "（原版 " + x1.ToString("F2") + "," + y1.ToString("F2") + "→"
                  + x2.ToString("F2") + "," + y2.ToString("F2") + "；实测 "
                  + a1.ToString("F2") + "," + b1.ToString("F2") + "→" + a2.ToString("F2") + "," + b2.ToString("F2") + "）");
    }

    /// <summary>🆕 2026-10-10（A284）：一颗导航钮**图标那一格**的渲染矩形。
    /// 期望值 = 原版 `RT 1111/1240/1241/1242/1243` 的字面量（⛔ 不从 `MainMenuRuntime` 的常量读 —— 那是自证）。</summary>
    static void CheckNavIconRect(MainMenuRuntime menu, string name, float x1, float x2, float y1, float y2, string what)
        => CheckQuadRectPx(NavIconQuad(menu, name), x1, x2, y1, y2, 0.5f, $"导航钮 `{name}`：" + what);

    /// <summary>🆕 **2026-10-11（F2）**：相邻两颗导航钮**图标顶之间的步进**（相对量，拦「整列挪」这一类错）。
    /// 期望值 = 原版**钮 RT 高 `169.68`** + 父 VLG 的 **`spacing −16.35`**（两者都是 prefab 字段）
    /// ⇒ 非 Home 之间恒 **153.33**；`Home→Collection` 另减顶距差 1.2825（2.9825 − 1.70）⇒ **152.05**。
    /// ⚠️ 两个 rect 都**实测**（`QuadPxRect`）；容差沿用 `CheckNavIconRect` 的 **0.5px**。
    /// 🔴 为什么要有它：绝对量那五条会被「抄错文档的期望值」整批带偏（2026-10-11 的 `Shop`/`Social` 假绿即此）。</summary>
    static void CheckNavIconStep(MainMenuRuntime menu, string a, string b, float wantPx)
    {
        float a1, b1, a2, b2, c1, d1, c2, d2;
        // ⚠️ 两句**分开写**：写成 `ok = Quad(...a) && Quad(...b)` 的话第二条会短路，
        //    `c1..d2` 在「第一条为假」那条路径上**没被赋值** ⇒ 编译期 `CS0165`（2026-10-11 实测）。
        bool okA = QuadPxRect(NavIconQuad(menu, a), out a1, out b1, out a2, out b2);
        bool okB = QuadPxRect(NavIconQuad(menu, b), out c1, out d1, out c2, out d2);
        if (!okA || !okB)
        { CheckTrue(false, $"★ 相对：`{a}` / `{b}` 的图标量不到渲染矩形（节点不在 / 底下没有 `ImageQuad`）"); return; }
        CheckNear(d1 - b1, wantPx, 0.5f,
                  $"★ 相对：`{b}` 与 `{a}` 的**图标顶步进** = 原版 169.68 − 16.35 = 153.33"
                  + (a == "Home" ? " − 1.2825（Home 顶距 2.9825、其余 1.70）= 152.05" : ""));
    }

    /// <summary>🆕 **2026-10-10（A222）**：悬停一颗钮 ⇒ 量它**指定的那颗可见图形**染成什么色，移开再量还原。
    /// 判据 = 原版 `Selectable.DoStateTransition`：`ColorTint` 打的是 **`m_TargetGraphic`**
    /// （逐颗实读 —— **永远是这颗 `Selectable` 自己子树里的那颗图形**，10/10；一次也没打到「另建的命中区」上）。
    /// ⚠️ 选中态优先级在悬停之上（`Selectable.currentSelectionState`）⇒ 先把它摘掉再悬停，量完还回去。
    /// 🔴 **这条断的是「颜色」不是「调没调过某个 API」**（⛔ 自证）：不传目标时色偏只落在那颗**透明**的
    /// 命中区 quad 上 ⇒ 可见图形量出来还是纯白 ⇒ **红**；「悬停/移开」两态**都量**，分得出两种状态。</summary>
    static void CheckHoverTint(WindowButton wb, ImageQuad vis, Color want, string what)
    {
        if (wb == null || vis == null) { CheckTrue(false, what + "：钮或那颗可见图形不在"); return; }
        bool wasSel = wb.IsSelected;
        wb.SetSelected(false);
        CheckTintOn(vis, Color.white, 0.004f, what + " · 常态 = 原色（白）");
        wb.Enter();
        CheckTintOn(vis, want, 0.004f, what + " · 悬停 = 原版 `m_HighlightedColor`");
        wb.Exit();
        CheckTintOn(vis, Color.white, 0.004f, what + " · 移开 = 还原原色");
        wb.SetSelected(wasSel);
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>
    /// **量**一段文字渲出来到底多大（不猜）。原版给的是 `m_fontSize`（画布像素）——
    /// 我们想知道「按 `fontSize = px/108` 摆出来，实际占多少像素」。
    /// 判据：**大写高应当 ≈ `fontSize × TmpFont.WorldCapPerFontSize`**；宽按字符数自带。
    /// </summary>
    static void MeasureText(Transform t, string what, float origFontPx)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, $"量文字：`{what}` 节点不在"); return; }
        Debug.Log(P + $"  【量】`{what}`：原版 em {origFontPx}px ⇒ 我们实际 {lb.FontPxNow:F2}px"
                  + $" · 宽 {lb.WorldW * 108f:F1}px · (TMP fontSize {lb.FontSize:F4})");
        // ⚠️ 只信 `GlyphHeightWorld`（汉字高 ≈ 1 em，`TmpFont` 实测换算）与 `WorldW`（TMP 的 textBounds）。
        //    **`CapHeightWorld` 在只调过 `SetFontSize` 时是无效的**（走「按档位估」的兜底路）—— 第一版就被它骗了。
    }

    /// <summary>比 `ImageQuad` 的染色（原版 `Image.m_Color`）。
    /// ⚠️ 取法 = `QuadOf`（`GetComponentInChildren<ImageQuad>()`，**不带 `true`**）——
    /// 对「**整颗关着**」的件（奖杯格的 `Collectable Highlight`）语义没写死，
    /// 那种件走下面的 `CheckTintOn`（先把 quad 从 `QuadOfInactiveToo` 拿到手）。</summary>
    static void CheckTint(Transform t, Color want, float tol, string what)
        => CheckTintOn(QuadOf(t), want, tol, what);

    /// <summary>🆕 2026-10-04（FX-1）：`CheckTint` 的「**已经拿到 quad**」版 —— 给
    /// `QuadOfInactiveToo` 那条路用（那颗件是 `SetActive(false)` 的）。判据/容差一字不差。</summary>
    static void CheckTintOn(ImageQuad q, Color want, float tol, string what)
    {
        if (q == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var got = q.Tint;
        CheckTrue(Mathf.Abs(got.r - want.r) <= tol && Mathf.Abs(got.g - want.g) <= tol
                  && Mathf.Abs(got.b - want.b) <= tol && Mathf.Abs(got.a - want.a) <= tol,
                  $"{what}（实得 {got.r:F3},{got.g:F3},{got.b:F3},{got.a:F3}）");
    }

    /// <summary>比 TMP 的字号（世界单位；原版值是**画布像素** ⇒ 期望值传 `px/108f`）。</summary>
    static void CheckFont(Transform t, float wantPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        // 🔴 比 **`FontPxNow`（像素口径的实际生效字号）** —— 别用 `CapHeightWorld`/`GlyphHeightWorld`：
        //    那两个是**回读传入值**的伪测量（`已知的坑.md`「两个属性看着像测量」那条），拿它断言 = 自证。
        CheckNear(lb.FontPxNow, wantPx, 0.6f, what);
    }

    /// <summary>字号落在原版 autosize 的 [min,max] 里（开了 auto 之后**具体值由 TMP 算**，不能断言等于某个数）。</summary>
    static void CheckFontInRange(Transform t, float minPx, float maxPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        float px = lb.FontPxNow;
        CheckTrue(px >= minPx - 0.1f && px <= maxPx + 0.1f, $"{what}（实得 {px:F2}px）");
    }

    /// <summary>🆕 **2026-10-07（A77-㉒②③⑤）**：一段文字的**自适应窗口**（= 原版 `m_fontSizeMin/Max`，画布 px）。
    /// <para>🔴 **为什么不能拿 `CheckFontInRange` 代替**：那一条断的是 **`FontPxNow`（TMP 二分出来的收敛值）**——
    /// 「**根本没开自适应**、但字号本来就落在窗口里」那一档它**照样绿**（弱断言，分不出两种状态）。
    /// 这里读 `Label.FontSizeMin/Max`（= `_tmp.fontSizeMin/Max`，TMP 的 `fontSize` 单位）
    /// 再乘上**唯一那条换算**（`Label.FontSizeToPx`）落回 px ⇒ 断的是**窗口本身**。</para>
    /// 节点不在 / 点阵后端（`_tmp == null` ⇒ 两个口都返回 0）⇒ 两条都红（不静默）。</summary>
    static void CheckFontWindow(Transform t, float minPx, float maxPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        CheckNear(Label.FontSizeToPx(lb.FontSizeMin), minPx, 0.6f, what + "：窗口**下界**");
        CheckNear(Label.FontSizeToPx(lb.FontSizeMax), maxPx, 0.6f, what + "：窗口**上界**");
    }

    /// <summary>🆕 **2026-10-07（A77-⑤）**：一段文字**折不折行**（原版 `m_TextWrappingMode`）。
    /// 🔴 前置 = **真的取到 TMP**：`Label.Wrapping` 在点阵后端（`_tmp == null`）**恒 false** ——
    /// 不把这一条断出来，「`SetWrapping(false)` 被删掉」在没字体的机器上会**假绿**。
    /// 🔴 **2026-10-07 就地订正**：那一条前置原来是从外面按类型捞组件
    /// （`t.GetComponentInChildren<TMPro.TextMeshPro>()`）—— 它对**激活**的树碰巧成立，
    /// 但**单参那版只找激活对象**、而且**不是**全工程「这一段是不是真 TMP」的判据
    /// ⇒ 收口到 `Label.CanRenderChinese`（= `_tmp != null`，`Battle/Label.cs:46`；`BattleScene` /
    /// `DeckScene` 用的都是它）。同族那处真红见下面 `CheckWrapMode` 的订正注释。</summary>
    static void CheckWrapping(Transform t, bool want, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        CheckTrue(lb != null && lb.CanRenderChinese, what + "（前提）这一段是真 TMP —— 点阵后端没有「折行」这回事，断不了");
        CheckTrue(lb != null && lb.Wrapping == want,
                  what + $"（现在 = {(lb != null && lb.Wrapping ? "折行" : "不折行")}）");
    }

    /// <summary>🆕 **2026-10-07（波 8 · Label 折行族 / A62）**：一段文字的换行档 = **原版 `m_TextWrappingMode` 的原文**
    /// （`0` `NoWrap` · `1` `Normal` · `3` `PreserveWhitespaceNoWrap`）。
    /// <para>🔴 **与既有 `CheckWrapping(Transform,bool,…)` 的两点不同**：
    ///   ① 期望值传的是 **dump 那一列的原文（int）**，⛔ 不是 `bool` —— 第三档 `3` 与 `0` 在「折不折行」上同档、
    ///      **但不是一个值**（`Label.WrappingMode` 的头写着哪一半有判据、哪一半没有）；用 `bool` 断 = 逼着把 `3` 降级；
    ///   ② 读口取**该节点自己**上的组件（`GetComponent`）而不是 `GetComponentInChildren` ——
    ///      后者**只找激活的**，而本批要断的件里有两处**出厂就关着**（整棵 `ChooseNameWindow`、
    ///      `Info Section with Alliance`、`Current Rank/Timer`）⇒ 用那一版会**把「节点关着」误报成「这一段字不在」**。</para>
    /// 🔴 前置 = 真 TMP：点阵后端 `WrappingMode` 恒 **−1**，不前置会假绿。
    /// <para>🔴🔴 **2026-10-07 就地订正（波 8 首跑：22 处调用 · 22 处「前提」红）**：
    /// 原来这里写的是 `var tmp = t.GetComponent<TMPro.TextMeshPro>();` —— **它对任何 `Label` 节点恒为 null**：
    /// `Label` 与 TMP **不在同一颗节点上**（`Label.Create` 只把 `Label` 挂在自己身上，TMP 是
    /// `TmpFont.NewText` **另建一颗子件**、名字恒为 `"text"` 再挂上去 —— `Battle/Label.cs:552` →
    /// `Core/TmpFont.cs:151`）⇒ 那 22 条「前提」**全部**红，**而紧跟其后的真判据一条没错**
    /// （日志里逐条 `现在 = 1/0/3`，与期望全同）⇒ **那是探针读错，不是实现错**。
    /// 改法 = 前置改用 `Label.CanRenderChinese`（= `_tmp != null`，**全工程「这一段是不是真 TMP」的唯一出处**）。
    /// ⛔ **别把它换回 `GetComponentInChildren<TMPro.TextMeshPro>()`**：单参那版只找**激活**的对象，
    /// 上面 ② 那条「出厂关着」的顾虑正是冲着它来的（成立，照留）。</para></summary>
    static void CheckWrapMode(Transform t, int want, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var lb = t.GetComponent<Label>();          // ⚠️ 仍是**这一颗自己**上的 `Label`（见上面 ②）
        if (lb == null) { CheckTrue(false, what + "（前提）这一颗节点上没有 `Label`（读口只认它自己身上的那一颗）"); return; }
        CheckTrue(lb.CanRenderChinese,
                  what + "（前提）这一段是真 TMP —— 点阵后端没有「折行」这回事（`WrappingMode` 恒 −1），断不了");
        Check(lb.WrappingMode, want, what + $"（现在 = {lb.WrappingMode}）");
    }

    /// <summary>🆕 **2026-10-07（A77-㉒②）**：卡组格内景**某一层**的渲染矩形（px）对不对。
    /// <para>量的是那一层底下**全部 `ImageQuad` 的并集**（`UnionQuadRect`）——
    /// ⛔ 不量承载它的节点（`Nine` 那类把子块挂在根下），⛔ 也不能只量「第一块」：
    /// 压在软边带上的那几层会被 `MenuDraw.ApplySoftEdges` **切成主格 + 若干子块**
    /// （主格只占「含矩形中心」的那一段）⇒ 只量第一块会得到一条**判对了实现、量错了东西**的假红。</para>
    /// 🔴 **查 quad 时显式带 `includeInactive: true`**：`Highlight` 在非当前那一格里是 `SetActive(false)` 的，
    /// 默认重载对「整棵关着」的子树**返不返回没写死**（见 `QuadOfInactiveToo` 头那条）。
    /// 期望值 = 原版锚点算出来的**字面量**（⛔ 不从 `PracticeModePopup.Item*` 读 = 自证）。</summary>
    static void CheckLayerRect(string what, Transform layer, float x1, float y1, float x2, float y2, float tol)
    {
        if (layer == null) { CheckTrue(false, $"格内景 `{what}` 不在"); return; }
        float a1, b1, a2, b2;
        if (!UnionQuadRect(layer, out a1, out b1, out a2, out b2))
        { CheckTrue(false, $"格内景 `{what}` 底下没有 `ImageQuad`（这一层等于没画）"); return; }
        CheckTrue(Mathf.Abs(a1 - x1) <= tol && Mathf.Abs(b1 - y1) <= tol
                  && Mathf.Abs(a2 - x2) <= tol && Mathf.Abs(b2 - y2) <= tol,
                  $"★ 格内景 `{what}` 矩形 = 原版锚点算出来的 {x1:F2},{y1:F2}→{x2:F2},{y2:F2}"
                  + $"（实测 {a1:F2},{b1:F2}→{a2:F2},{b2:F2}）");
    }

    /// <summary>🆕 **2026-10-07（A77-㉒①）**：一颗节点底下**全部 `ImageQuad` 的并集矩形**（px）。
    /// <para>🔴 **为什么不能直接用 `QuadOf` / `HitQuadRect`**：九宫格（`MenuDraw.Nine`）返回的那颗**根**底下
    /// 挂的是**九块子 quad**（`ImageQuad.CreateNineSlice`）—— `HitQuadRect` 量到的是**第一块**（角块），
    /// 拿它跟整块面板比会得到一条**判对了实现、量错了东西**的假红（同 `HitQuadRect` 头那条）。</para>
    /// 至少一块 ⇒ 回 true（并集 = 整块，因为九块无缝铺满）。</summary>
    static bool UnionQuadRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
        if (t == null) return false;
        bool any = false;
        foreach (var q in t.GetComponentsInChildren<ImageQuad>(true))
        {
            float a1, b1, a2, b2;
            if (!HitQuadRect(q.transform, out a1, out b1, out a2, out b2)) continue;
            x1 = Mathf.Min(x1, a1); y1 = Mathf.Min(y1, b1);
            x2 = Mathf.Max(x2, a2); y2 = Mathf.Max(y2, b2);
            any = true;
        }
        if (!any) { x1 = y1 = x2 = y2 = 0f; return false; }
        return true;
    }

    /// <summary>一段文字**现在**的字号（画布像素）= `Label.FontPxNow`（开着 auto 就是**收敛结果**）。
    /// 读不到 ⇒ −1（断言会红）。</summary>
    static float FontPxOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.FontPxNow : -1f;
    }

    /// <summary>🆕 **2026-10-04**：一段文字的**名义字号**（画布像素）= 「我们传进去的是多少 px」。
    /// <para>🔴 **为什么不能拿 `Label.FontSize` / `FontPxNow` 代替**：那两个读的是 `_tmp.fontSize` ——
    /// **开着 auto 时 TMP 会把收敛结果写回它**（原版 dump 那一列 `字号` 也是这个语义：**是结果、不是输入**；
    /// 本窗 `Title` 的原始 MB 就是 `m_fontSize **40** / m_fontSizeBase **36** / auto[3,40]` ⇒ 40 是收敛到上限的结果）。
    /// 拿它断「原版 `m_fontSize = 40`」就会得到 34.292 这种值（同步点实跑踩过）。</para>
    /// <para>能读到标称值的只有 `Label.DumpSizes()` 的 `fontSize=`（= `TmpFontSize()`，源码注释写着「**不含自适应结果**」）
    /// ⇒ 读它、再乘上与 `FontPxNow` **同一个**换算常数（`TmpFont.WorldGlyphPerFontSize × 108`），落到同一量纲上比。
    /// 同族读口 → `Editor/ShopScene.cs` 的 `NominalFontSize`（那边还要一条参考条做单位校准，本窗不需要）。
    /// 读不到 ⇒ −1（断言会红，不静默）。</para></summary>
    static float NominalFontPx(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) return -1f;
        string s = lb.DumpSizes();
        int i = s.IndexOf("fontSize=", System.StringComparison.Ordinal);
        if (i < 0) return -1f;
        i += "fontSize=".Length;
        int j = s.IndexOf(' ', i);
        if (j < 0) j = s.Length;
        float f;
        if (!float.TryParse(s.Substring(i, j - i), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out f)) return -1f;
        return f * TmpFont.WorldGlyphPerFontSize * 108f;
    }

    /// <summary>**渲染宽度必须放得进框** —— 这条才是能抓住「autosize 没生效」的断言
    /// （第一版 `COLLECTION` 宽 212px 却摆在 146.9px 的条里，画面溢出而自检全绿）。</summary>
    static void CheckFits(Transform t, float boxPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        float w = lb.WorldW * 108f;
        CheckTrue(w <= boxPx + 1f, $"{what}（渲出 {w:F1}px ≤ 框 {boxPx:F1}px）");
    }

    // ================================================================ 断言用的**原版行几何**（判据源，独立于实现）
    //
    // 🔴 **2026-10-04（A35②）：断言里⛔ 不许再引用实现常量**（`MatchLogRow.RowH` / `LeaderboardRow.RowH` …
    //    —— 原来这几条就是这么写的）。那是**同式自证**：等号两边是**同一个表达式**，把实现常量改错也不会红
    //    （A35① 的 `Open Alliances` 行距就是这么漏过去的：断言拿 `110 + 10` 当「原版值」，而那个 `10` 正是
    //    实现里照 `Invitations` 抄错的那一个）。
    //    ✅ 做法 = **把原版值在断言处重抄一遍并注明出处**，与实现常量的对应靠**对账**、不靠共用
    //    （铁律：断言要盯「原版参数」，不是盯我们自己的常量）。
    /// <summary>对局历史那一族的行几何（实读原始 JSON）：
    /// 行 `Match Log` 的 `sizeDelta.y = 203.20`（普查 `资料/普查产出_0927/对局历史_行模板与弹窗.md:167`）·
    /// 内层 `Content` 那个 `VerticalLayoutGroup` 的 `spacing = 25.0`（同份 `:166`）；
    /// 内容高算式同份 `:197`（`N × 203.2 + (N−1) × 25`）。</summary>
    const float OrigMatchRowH = 203.20f, OrigMatchRowGap = 25f;
    /// <summary>排行榜那一族的行几何（实读原始 JSON）：
    /// 行 `PlayerRankingRow` 的 `sizeDelta.y = 100`（普查 `资料/普查产出_0927/排行榜_经典.md:68`）·
    /// 内层 `Content` 那个 `VerticalLayoutGroup` 的 `spacing = 15.0`（同份 `:67`；
    /// 嵌入版同值，见 `排行榜_嵌入版与行族.md:40`）。</summary>
    const float OrigLbRowH = 100f, OrigLbRowGap = 15f;

    /// <summary>🆕 2026-10-03：**现算**「按原版行距排下去，有几行的矩形与视口相交」——
    /// 这就是「滚出视口的整行不建」那几条断言的期望值。⛔ **别写死条数**（写死 = 拿我们的常量断言我们的常量）。
    /// 判据 = 原版 `RectMask2D` 的可见性语义（与 `MenuScroll.Intersects` 同一条式子，这里**独立算一遍**
    /// —— 不拿它自己算出来的结果当期望）。
    /// <para>`offset` = 滚动偏移（画布像素；正 = 内容上移 = 看到下面那些行）。
    /// `pitch` / `rowH` 一律传**原版参数**（榜单 100+15 · 日志行 203.20+25），不是我们的实现常量。</para></summary>
    static int RowsInViewport(int n, float vpTop, float vpBot, float pitch, float rowH, float offset)
    {
        int c = 0;
        for (int i = 0; i < n; i++)
        {
            float y1 = vpTop + i * pitch - offset, y2 = y1 + rowH;
            if (y2 > vpTop + 0.01f && y1 < vpBot - 0.01f) c++;
        }
        return c;
    }

    /// <summary>🆕 2026-10-03（A25④）：**网格**布局里「与视口相交」的格数（现算）—— 给好友页那种
    /// `GridLayoutGroup` 用（`RowsInViewport` 只管单列）。格 i 的顶边 = `vpTop + padTop + (i/cols)·pitch`。
    /// 判据与 `RowsInViewport` **同一条**（= `MenuDraw.ClipRect` 的交集判法），只是多一层 grid 分组。</summary>
    static int CellsInViewport(int n, int cols, float padTop, float pitch, float cellH,
                               float vpTop, float vpBot, float offset)
    {
        int c = 0;
        for (int i = 0; i < n; i++)
        {
            float y1 = vpTop + padTop + (i / cols) * pitch - offset, y2 = y1 + cellH;
            if (y2 > vpTop + 0.01f && y1 < vpBot - 0.01f) c++;
        }
        return c;
    }

    /// <summary>🆕 2026-10-03（A25④）：数一个节点下**直接子节点**里叫 `name` 的有几个
    /// （= 「这一屏真建出来几行」，与实现里的 `BuiltRows` 对账）。
    /// ⚠️ 只数**直接子节点** —— 行里面也有同名件（例如成员行里还有 `background`）时会数重。</summary>
    static int CountChildren(Transform parent, string name)
    {
        int c = 0;
        if (parent == null) return c;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) c++;
        return c;
    }

    /// <summary>🆕 2026-10-04（A40）：第 `n` 个（0 起）**直接子节点**里叫 `name` 的那个 ——
    /// 网格布局要按**建的顺序**量第 1 / 第 2 格（`FindChild` 只给第一颗，量不到同一排的第二个）。
    /// 没有就返回 null（调用方自己判）。</summary>
    static Transform NthChild(Transform parent, string name, int n)
    {
        if (parent == null) return null;
        int k = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name != name) continue;
            if (k == n) return c;
            k++;
        }
        return null;
    }

    /// <summary>🆕 2026-10-03（A25④）：把一棵子树里指定行节点的某段文字收集起来（按树的顺序）——
    /// 「滚动之后建的是不是**另一批**行」那几条靠它（比行数更能说明问题：第 1 行该滚出去、最后一行该进来）。</summary>
    static List<string> RowTextsOf(Transform content, string rowName, string textNode)
    {
        var list = new List<string>();
        if (content == null) return list;
        foreach (var rt in content.GetComponentsInChildren<Transform>(true))
        {
            if (rt.name != rowName) continue;
            var tn = FindChild(rt, textNode);
            var lb = tn != null ? tn.GetComponentInChildren<Label>() : null;
            if (lb != null) list.Add(lb.Text);
        }
        return list;
    }

    /// <summary>🆕 2026-10-03（A25④）：量子树里**建出来的行**的总跨度（最上沿 / 最下沿，画布像素 y）。
    /// `rowName` = 行节点的名字，`rowH` = 原版行高。
    /// ⚠️ 只读**行节点自己**的位置（`MenuDraw.Node` 按矩形中心摆）：**被裁过的 quad 位置会动**
    /// （`ClipNineChildren` 把它挪到截后那块的中心），拿它量位移会飘 —— 同 `TopRowTop` 那条注释
    /// （`资料/已知的坑.md` 2026-10-03「裁剪会移动 quad 的节点」）。没有行时返回 false。</summary>
    static bool RowSpan(Transform content, string rowName, float rowH, out float top, out float bot)
    {
        top = float.MaxValue; bot = float.MinValue;
        if (content == null) return false;
        foreach (var rt in content.GetComponentsInChildren<Transform>(true))
        {
            if (rt.name != rowName) continue;
            float cy = LayoutSpace.PxY(rt.position.y);
            top = Mathf.Min(top, cy - rowH * 0.5f);
            bot = Mathf.Max(bot, cy + rowH * 0.5f);
        }
        return top <= bot;
    }

    /// <summary>🆕 2026-10-03（A25④）：量子树里**每一颗 `ImageQuad` 渲出来的**那块，返回越出 `vp` 的那些
    /// （`名字 x1,y1..x2,y2`）—— 「越界内容不再画到框外」那几条断言的判据。
    /// 🔴 量的是**渲出来那块**（`q.WorldW/H` + **quad 自己**的位置），不是承载它的节点：
    /// 裁剪会把 quad 挪走、节点不动（`HitQuadRect` 那条注释里的坑，2026-10-03 踩过）。
    /// 🔴 判据 = 原版 `RectMask2D` 只裁**渲染**；**未激活的**（被 `ClipNineChildren` 判为整块在框外的）
    /// 不算 —— 它压根不画。
    /// ⚠️ 文字（`Label`）不是 `ImageQuad` ⇒ 不在量程内：**文字仍是「整块在框外才不建」**
    /// （那条缺口记在 `MenuWindowBase.Clip` 的注释 / `项目任务.md` §三 第 29 条 A9，本批没动它）。</summary>
    static List<string> QuadsOutside(Transform root, PxRect vp, float tol)
    {
        var bad = new List<string>();
        if (root == null) return bad;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null || !q.gameObject.activeInHierarchy) continue;
            float w = q.WorldW * 108f, h = q.WorldH * 108f;
            float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
            float x1 = cx - w * 0.5f, x2 = cx + w * 0.5f, y1 = cy - h * 0.5f, y2 = cy + h * 0.5f;
            if (x1 < vp.x1 - tol || x2 > vp.x2 + tol || y1 < vp.y1 - tol || y2 > vp.y2 + tol)
                bad.Add(q.name + " " + x1.ToString("F1") + "," + y1.ToString("F1")
                        + ".." + x2.ToString("F1") + "," + y2.ToString("F1"));
        }
        return bad;
    }

    /// <summary>🆕 2026-10-03：量**最上面那颗建出来的行**的顶边（画布像素 y）—— 给「滚动之后内容真的往上走了」
    /// 那两条用。⚠️ 只读**行节点自己**的位置（`RenderedRect` 那一类量的是被裁过的 quad，拿它比位移会飘
    /// —— 见 `资料/已知的坑.md` 2026-10-03「裁剪会移动 quad 的节点」那条）。
    /// 没有行时返回 `float.MaxValue`（调用方自己判）。</summary>
    static float TopRowTop(Transform content)
    {
        float top = float.MaxValue;
        if (content == null) return top;
        foreach (var rt in content.GetComponentsInChildren<Transform>(true))
            if (rt.name == "Match Log")
                top = Mathf.Min(top, LayoutSpace.PxY(rt.position.y) - OrigMatchRowH * 0.5f);
        return top;
    }

    /// <summary>🆕 2026-10-04（A30）：取一个节点底下的**第一颗 `ImageQuad`**（`Nine` 那种「一根节点 + 9 张
    /// 小 quad」的情形也吃 —— 返回第一张）。给「软边有没有压 alpha」那条断言用。</summary>
    static ImageQuad QuadOf(Transform t)
    {
        return t != null ? t.GetComponentInChildren<ImageQuad>() : null;
    }

    /// <summary>🆕 2026-10-04（FX-1）：取一颗节点底下的**第一颗 `ImageQuad`**，**连 `SetActive(false)` 的件一起找**
    /// —— `QuadOf` 的加固版，专给「**建了但关着**」的那类件（奖杯格的 `Collectable Highlight`）。
    /// <para>🔴 **为什么要有它**：`QuadOf` 走 `GetComponentInChildren&lt;ImageQuad&gt;()`（**不带 `true`**）——
    /// 那个重载对「**整棵关着**」的子树返不返回，Unity 的语义**没写死**
    /// （本仓 `MainMenuScene.cs:2381-2383` 记过同族的一条；X5 审查 §断言 #26 也标了「定不了」）。
    /// 而奖杯格那颗 `Collectable Highlight` **整颗是 `SetActive(false)` 的**
    /// （原版出厂态，`AllianceMemberTab.cs` 里 `hl.SetActive(false)`）⇒
    /// 它名下那 8 块角块**全部** `activeInHierarchy == false`。</para>
    /// <para>⚠️ **⛔ 别改成「只挑 `activeInHierarchy` 的那颗」**：这颗件**故意是关的**，
    /// 按「在不在渲」过滤 ⇒ 一颗都取不到、断言反倒会**假红**（`ImageQuad.CreateNineSlice` 把块挂在
    /// **根**下、`MenuDraw.Nine` 返回的就是那个根，见 `ImageQuad.cs:367-380` / `MenuDraw.cs:762-766`）。
    /// 这里要问的是「**建出来了没有**」，不是「现在在不在渲」。显式传 `true`（= 含未激活）是有定义的那一种。</para></summary>
    static ImageQuad QuadOfInactiveToo(Transform t)
    {
        if (t == null) return null;
        var qs = t.GetComponentsInChildren<ImageQuad>(true);
        return qs.Length > 0 ? qs[0] : null;
    }

    /// <summary>🆕 2026-10-04（A30）：四角顶点色里**最小的那个 alpha**（没设过顶点色 ⇒ 1 = 四角全白）。
    /// 🔴 判据 = `ImageQuad.CornerColors`（**只读**那个口是给自检开的）—— `IsAllWhiteVerts` 只看 **RGB**、
    /// **看不出 alpha 斜坡**，所以软边那一条不能用它。
    /// ⚠️ 这个数**只由软边那一套**（`MenuDraw.SetRamp`）压下来：带外为 1（`SetRamp` 早退）、带内 < 1。</summary>
    static float MinCornerAlpha(ImageQuad q)
    {
        if (q == null) return 1f;
        var c = q.CornerColors;
        if (c == null || c.Length < 4) return 1f;
        return Mathf.Min(Mathf.Min(c[0].a, c[1].a), Mathf.Min(c[2].a, c[3].a));
    }

    /// <summary>有没有哪一角的 alpha 被压到 `thr` 以下（软边断言的正/对照组都用它）。</summary>
    static bool AnyCornerAlphaBelow(ImageQuad q, float thr) { return MinCornerAlpha(q) < thr; }

    // ============================================================ 🆕 2026-10-04（A9 尾巴）：软边接线探针
    //
    // 判据 = `Shell/MenuDraw.cs` 的 `ApplySoftEdges`（原版 `RectMask2D.m_Softness` 的几何等效物）：
    //   非 0 时把一块**沿渐隐带的内沿切开**（原节点留含矩形中心的那一格、其余格建**子 quad**，名 `…_soft<i><j>`）
    //   ⇒ 父块与子块那条**共享边**就是带的内沿。
    // 🔴 **为什么这就是「接没接」的判据**：软边 = 0（硬边）时**一个子块都不会有** ⇒ 表空 = 这条软边没接上；
    //   而「切线该在哪」是拿**原版值 + 那一格实际的裁切矩形**现算的（⛔ 不是从被测实现里读常量）
    //   ⇒ 删掉接线 / 改数值 / 把 x、y 两轴写反，都会真红。
    // ⚠️ 本文件原来没有这两个助手（`CollectionScene` / `RewardsScene` 各有一套同名实现 ——
    //    「四个自检各自一套辅助函数」是本工程的一笔明账）。这里照 `CollectionScene` 那一份的语义抄。

    /// <summary>一个 `ImageQuad` **渲出来**的像素矩形（1920×1080 画布 · 左上原点 · y 向下）。
    /// ⚠️ 只用**这个组件自己**的 `transform.position` + `WorldW/H` —— 量切出来的每一块必须逐块量。</summary>
    static bool QuadPxRect(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (q == null) return false;
        float w = q.WorldW * 108f, h = q.WorldH * 108f;
        float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
    }

    /// <summary>🆕 2026-10-05：量一个**格子节点**（`Army_i` / `DeckRow_i`）的**渲染矩形**（px）。
    /// 走它下面 `Hit` 里那个 `ImageQuad`（每个格都有 —— `HitOn` 建的）——
    /// 节点自己的 `localPosition` 是**相对父容器**的（现在格挂在 `Filters` / `Content` 底下），量不出页坐标；
    /// `QuadPxRect` 用世界坐标反算，格子挂在哪一层都对。</summary>
    static bool CellPxRect(Transform cell, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var hit = FindChild(cell, "Hit");
        var q = hit != null ? hit.GetComponentInChildren<ImageQuad>() : null;
        return QuadPxRect(q, out x1, out y1, out x2, out y2);
    }

    /// <summary>🆕 **2026-10-10（A178）**：数练习窗阵营列里**亮着** `node`（`Highlight` / `Has Player Deck`）
    /// 的那几格 —— 返回它们在 `pw.ArmyCells` 里的下标。
    /// 节点的 `activeSelf` 就是原版那两个 `GameObject.SetActive`（判据见 `PracticeModePopup.RebuildArmyCells`）。
    /// ⚠️ 只扫**建出来的那几格**（视口装不下的格原版也不 instantiate）。</summary>
    static List<int> ArmyLit(PracticeModePopup pw, string node)
    {
        var res = new List<int>();
        if (pw == null) return res;
        for (int k = 0; k < pw.ArmyCells.Count; k++)
        {
            var n = FindChild(pw.ArmyCells[k], node);
            if (n == null) { CheckTrue(false, $"阵营第 {k + 1} 格底下没有 `{node}`"); continue; }
            if (n.gameObject.activeSelf) res.Add(k);
        }
        return res;
    }

    /// <summary>一个格的渲染矩形是不是**原版那个矩形**（逐边 ±`tol` px）。
    /// 🔴 期望值一律写**原版实读的字面量**（出自 `menu_dump.py` / 预制体字段），⛔ 不从 `PracticeModePopup` 的常量里读
    /// —— 否则「常量改了」这条就跟着改、永远绿（自证）。</summary>
    static void CheckCellRect(Transform cell, float wx1, float wy1, float wx2, float wy2, float tol, string what)
    {
        float x1, y1, x2, y2;
        if (!CellPxRect(cell, out x1, out y1, out x2, out y2))
        { CheckTrue(false, what + "：量不到渲染矩形（格子不在 / `Hit` 里没有 `ImageQuad`）"); return; }
        CheckTrue(Mathf.Abs(x1 - wx1) <= tol && Mathf.Abs(y1 - wy1) <= tol
                  && Mathf.Abs(x2 - wx2) <= tol && Mathf.Abs(y2 - wy2) <= tol,
                  what + "（原版 " + wx1.ToString("F2") + "," + wy1.ToString("F2") + "→"
                  + wx2.ToString("F2") + "," + wy2.ToString("F2") + "；实测 "
                  + x1.ToString("F2") + "," + y1.ToString("F2") + "→" + x2.ToString("F2") + "," + y2.ToString("F2") + "）");
    }

    /// <summary>扫 `root` 子树，回传里面**所有软边切线**的位置（同 `CollectionScene.ScanSoftCuts`）。
    /// <param name="vertical">true = 只看**竖切线**（渐隐的是左右，= `m_Softness.x`）；false = 看横切线。</param>
    /// <param name="clip">这一棵子树所在的那个视口（**设计 px**，= 原版 `Viewport` 上那颗 `RectMask2D` 的框；
    /// 传进来的一律是**原版字面量 / 已断言的视口局部量**，⛔ 不从被测实现里读）。
    /// 🔴 **2026-10-11（A232）：落在 `clip` **本轴**两条边上的切线【不算软边切口】** ——
    /// `MenuDraw.ApplySoftEdges` 的切刀位置 = `clip.边 ± softness`，**某个分量 = 0 时那两条刀口
    /// 正好落在框自己的两条边上**（那是**硬裁**边，不是渐隐带的内沿）⇒ 少了这一闸，「数竖切线 = 0」
    /// 那几条的鉴别力就**依赖「实现侧恰好会裁」这个偶然性质**（判据原文 →
    /// `资料/普查产出_1008/波A_A225_三条红.md` 顺手发现 §2；4 份同形副本一起补）。
    /// ⚠️ **它是潜伏闸**：今天 `ApplySoftEdges` 入口先 `ClipVisToClip` 硬裁 ⇒ `vis ⊆ clip` ⇒
    /// `SoftCuts` 的「严格落在 `vis` 内部」**已经**挡掉了这类切口 ⇒ **今天一条读数都不会变**；
    /// 它防的是「实现侧哪天不裁了」那一档（那时它会**静默**把硬裁边当成「软边接上了」）。
    /// ⚠️ 形参**必填**（⛔ 不给默认值）：漏传 = 这道闸静默失效 ⇒ 让编译期拦住。</param></summary>
    static List<float> ScanSoftCuts(Transform root, bool vertical, PxRect clip)
    {
        var cuts = new List<float>();
        if (root == null) return cuts;
        // 🔴 A232 的闸：切刀落在**本轴**两条 `clip` 边上 ⇒ 那是硬裁边、不算软边切口（容差同下面 0.5px）
        bool OnClipEdge(float v) { return vertical
            ? (Mathf.Abs(v - clip.x1) <= 0.5f || Mathf.Abs(v - clip.x2) <= 0.5f)
            : (Mathf.Abs(v - clip.y1) <= 0.5f || Mathf.Abs(v - clip.y2) <= 0.5f); }
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null) continue;
            float hx1, hy1, hx2, hy2;
            if (!QuadPxRect(q, out hx1, out hy1, out hx2, out hy2)) continue;
            for (int i = 0; i < q.transform.childCount; i++)
            {
                var c = q.transform.GetChild(i).GetComponent<ImageQuad>();
                // ⚠️ **只认软边切出来的子块**（`ApplySoftEdges` 的命名 `baseName + "_soft" + i + j`）
                if (c == null || c.name.IndexOf("_soft") < 0) continue;
                float cx1, cy1, cx2, cy2;
                if (!QuadPxRect(c, out cx1, out cy1, out cx2, out cy2)) continue;
                if (vertical)
                {
                    float vR = cx1;                       // 子块在**右** ⇒ 切线 = 子块左沿
                    float vL = cx2;                       // 子块在**左** ⇒ 切线 = 子块右沿
                    if (Mathf.Abs(vR - hx2) < 0.5f) { if (!OnClipEdge(vR)) cuts.Add(vR); }
                    else if (Mathf.Abs(vL - hx1) < 0.5f) { if (!OnClipEdge(vL)) cuts.Add(vL); }
                }
                else
                {
                    float vB = cy1;                       // 子块在**下** ⇒ 切线 = 子块上沿
                    float vT = cy2;                       // 子块在**上** ⇒ 切线 = 子块下沿
                    if (Mathf.Abs(vB - hy2) < 0.5f) { if (!OnClipEdge(vB)) cuts.Add(vB); }
                    else if (Mathf.Abs(vT - hy1) < 0.5f) { if (!OnClipEdge(vT)) cuts.Add(vT); }
                }
            }
        }
        return cuts;
    }

    /// <summary>🆕 2026-10-04（A9 尾巴）：数一段文字里**alpha 被压过的顶点数**（-1 = 取不到 TMP 网格）。
    /// 判据 = `MenuDraw.ClipText` → `ClipTmpMesh`：逐字把顶点夹进框内、**并按同一条软边剖面写 `colors32.a`**
    /// ⇒ 压在渐隐带里的字 alpha &lt; 255。**0 = 这段文字没吃软边**（`ClipText` 那条路没接上 ——
    /// 只接图那一路的实现在这里红）。
    /// <para>🔴 **2026-10-08（A225-②）：顺手把「剖面」也算出来**（同 `Editor/CollectionScene.cs` 那条成熟的
    /// 同族探针）—— 这一条红了要能一眼分出三种情形：① **没削 alpha**（`n` = 0）② **这段字根本不在带里**
    /// （`nInBand` = 0 ⇒ 不是裁切的问题，是文字摆位）③ **削过头/削两遍**（`nBadProfile` &gt; 0）。</para></summary>
    /// <param name="clipY1">视口的**上沿**（画布 px）。</param>
    /// <param name="clipY2">视口的**下沿**（画布 px）。</param>
    /// <param name="softY">纵向带宽。三个一起定出「原版剖面」
    /// `min((y−clipY1), (clipY2−y)) ÷ softY`（判据 = uGUI `UI/Default`，见 `MenuDraw.SoftAlpha`；
    /// ⛔ 这里是**按判据独立算一遍**，不从实现读）。</param>
    /// <param name="nInBand">顶点 y 落在带里的个数（只用来报数）。</param>
    /// <param name="nBadProfile">alpha 与剖面的差 &gt; 4 的顶点数（带外的那一半也算 —— 带外的期望值是 255，
    /// 于是「带外一个都没被动」也一并钉住）。</param>
    /// <param name="minA">全字最小 alpha。</param>
    /// <param name="worstErr">最差那个偏差。</param>
    static int CountSoftFadedTextVerts(Transform labelNode, float clipY1, float clipY2, float softY,
                                       out int nInBand, out int nBadProfile, out float minA, out float worstErr)
    {
        nInBand = 0; nBadProfile = 0; minA = 255f; worstErr = 0f;
        var tmp = labelNode != null ? labelNode.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null) return -1;
        var ti = tmp.textInfo;
        if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return -1;
        float bandY1 = clipY1 + softY;                       // 上带内沿（下面报数用）
        int n = 0, cn = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
        for (int ci = 0; ci < cn; ci++)
        {
            var ch = ti.characterInfo[ci];
            if (!ch.isVisible) continue;
            int mi = ch.materialReferenceIndex;
            if (mi < 0 || mi >= ti.meshInfo.Length) continue;
            var mesh = ti.meshInfo[mi];
            if (mesh.colors32 == null || mesh.vertices == null) continue;
            for (int k = 0; k < 4; k++)
            {
                int v = ch.vertexIndex + k;
                if (v < 0 || v >= mesh.colors32.Length) continue;
                int a = mesh.colors32[v].a;
                if (a < 250) n++;
                if (a < minA) minA = a;
                if (v >= mesh.vertices.Length) continue;
                float y = LayoutSpace.ToPixel(tmp.transform.TransformPoint(mesh.vertices[v])).y;
                if (y > bandY1) nInBand++;
                // 原版剖面（见 `param` 说明）；`softY <= 0` 时恒 1（= 硬边，不该被动过一个字节）
                float want = softY > 0f
                    ? Mathf.Min(Mathf.Clamp01((y - clipY1) / softY), Mathf.Clamp01((clipY2 - y) / softY)) * 255f
                    : 255f;
                float err = Mathf.Abs(a - want);
                if (err > 4f) nBadProfile++;
                if (err > worstErr) worstErr = err;
            }
        }
        return n;
    }

    /// <summary>切线清单的逐条判据：每条都必须落在 `want` 里（±`tol`），且 `want` 每一项**都出现过**。
    /// `what` 里写清每一侧的算式（判据要能在失败信息里一眼看懂）。语义同 `CollectionScene.CheckSoftCuts`。</summary>
    static void CheckSoftCuts(List<float> cuts, float[] want, float tol, string what)
    {
        var hit = new bool[want.Length];
        CheckTrue(cuts.Count > 0, what + "：**有层被软边切开**（切线实测 "
            + (cuts.Count > 0 ? string.Join("、", cuts.ConvertAll(v => v.ToString("F2")).ToArray()) : "一条都没有")
            + "）—— **空表 = 这条软边没接上**（`ClipSoftness` 留在 0）");
        for (int i = 0; i < cuts.Count; i++)
        {
            int k = -1;
            for (int j = 0; j < want.Length; j++)
                if (Mathf.Abs(cuts[i] - want[j]) <= tol) { k = j; break; }
            CheckTrue(k >= 0, what + "：切线 #" + (i + 1) + " 在 " + cuts[i].ToString("F2")
                + " ⇒ 必须是带的内沿（" + string.Join(" / ", System.Array.ConvertAll(want, v => v.ToString("F2"))) + "）");
            if (k >= 0) hit[k] = true;
        }
        for (int j = 0; j < want.Length; j++)
            CheckTrue(hit[j], what + "：**" + want[j].ToString("F2") + " 这条切线确实出现**（少一条就说明那侧的软边没生效）");
    }

    /// <summary>四角顶点色是不是全白（= 没用顶点色）。</summary>
    static bool IsAllWhiteVerts(ImageQuad q)
    {
        var mf = q.GetComponent<MeshFilter>();
        var m = mf != null ? mf.sharedMesh : null;
        if (m == null || m.colors == null || m.colors.Length < 4) return true;
        foreach (var c in m.colors)
            if (Mathf.Abs(c.r - 1f) > 0.01f || Mathf.Abs(c.g - 1f) > 0.01f || Mathf.Abs(c.b - 1f) > 0.01f) return false;
        return true;
    }

    static int CountByName(Transform root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    /// <summary>
    /// 一张模式卡：**只断言有原版出处的三项** —— 列的左边界（`205 + 555c`，§二·4 实证）、宽高（`itemSize` 535×414.4 实证）。
    /// 🔴 **行 y 不断言** —— 它是运行时算的、本地取不到（§五 E 的 `?`），我们取的值**没有原版出处**，断言它就等于自证。
    /// </summary>
    static void CheckCard(MainMenuRuntime menu, string name, float colLeft, float w, float h, string what)
    {
        var card = menu.Find(name);
        if (card == null) { CheckTrue(false, $"模式卡 {what} 建了"); return; }
        var bg = FindChild(card, "Background Image");
        var q = bg != null ? bg.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, $"{what} 的卡图建了"); return; }
        CheckNear(q.WorldW, w / 108f, 0.01f, $"{what} 卡宽 = {w}px（`itemSize.x` 实证）");
        CheckNear(q.WorldH, h / 108f, 0.01f, $"{what} 卡高 = {h}px（`itemSize.y` 实证）");
        var wantX = MainMenuRuntime.Center(colLeft, colLeft + w, 0f, 0f).x;
        CheckNear(q.transform.localPosition.x, wantX, 0.01f, $"{what} 在第 {colLeft}px 起的那一列（列左 = 205+555c 实证）");

        // 🔴 2026-09-24 探针：模式卡的卡图**实拍看着小于卡面**（§三 第 15 条 第 45 行）。
        //    已知：quad 的 `WorldW/WorldH` **是对的**（上面那两条），`SetUvRect` **不改尺寸**（读过源码），
        //    贴图 1024² 全不透明、裁的那块里也几乎没有 (86,86,86) 的平灰。
        //    ⇒ 那就把**这一格子树里每一层**的名字 / 渲出矩形 / 图 / 队列 / 染色打出来，别再靠肉眼猜。
        {
            var cardT = menu.Find(name);
            var sb = new System.Text.StringBuilder();
            sb.Append($"[Menu]   【探针】{name} 子树：\n");
            foreach (var ch in cardT.GetComponentsInChildren<Transform>(true))
            {
                var cq = ch.GetComponent<ImageQuad>();
                var cl = ch.GetComponent<Label>();
                if (cq == null && cl == null) continue;
                float cw = cq != null ? cq.WorldW * 108f : cl.WorldW * 108f;
                float chh = cq != null ? cq.WorldH * 108f : cl.WorldH * 108f;
                int qq = -1; string tex = "-";
                if (cq != null)
                {
                    var mr = cq.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null)
                    { qq = mr.sharedMaterial.renderQueue; tex = cq.Texture != null ? cq.Texture.name : "<无>"; }
                }
                sb.Append($"      {ch.name,-26} {cw,8:F1}×{chh,-8:F1} q={qq,-5} z={ch.localPosition.z:F3} 图={tex}\n");
            }
            Debug.Log(sb.ToString());
        }
        // 🔴 **分层顺序**（§10·3 找茬点 4）：卡图必须**大于**整屏渐变的队列 ——
        //    两者 z 都是 0，同队列时谁盖谁**不确定**（第一版 Tutorial 卡就是这么被渐变盖住的）。
        var bgQuad = menu.Find("Background") != null ? menu.Find("Background").GetComponentInChildren<ImageQuad>() : null;
        if (bgQuad != null)
        {
            int qa = q.GetComponent<MeshRenderer>().sharedMaterial.renderQueue;
            int qb = bgQuad.GetComponent<MeshRenderer>().sharedMaterial.renderQueue;
            CheckTrue(qa > qb, $"{what} 卡图的渲染队列（{qa}）**大于**整屏渐变的（{qb}）");
        }
    }

    // ============================================================ 存场景

    /// <summary>建出场景存盘，给人打开按 Play 用。**顺带把它加进 Build Settings** —— 壳要能 `LoadScene("MainMenu")`。</summary>
    public static void BuildAndSaveScene()
    {
        Directory.CreateDirectory(ShotDir);
        var menu = Build(out var root);

        if (root == null || root.GetComponent<MainMenuRuntime>() == null)
        {
            Debug.LogError(P + "✗ 场景里没挂 `MainMenuRuntime` —— 按 Play 会是一片空");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log(P + $"  场景 {ScenePath}");

        AddToBuildSettings(ScenePath);
        Shoot("00_主菜单.png");
        Debug.Log(P + menu.Dump());
    }

    /// <summary>把场景加进 `EditorBuildSettings`（幂等）—— 否则 `SceneManager.LoadScene("MainMenu")` 会抛。</summary>
    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) { Debug.Log(P + "  （已在 Build Settings 里）"); return; }
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log(P + "  已加进 Build Settings（壳要靠它 `LoadScene`）");
    }
}
