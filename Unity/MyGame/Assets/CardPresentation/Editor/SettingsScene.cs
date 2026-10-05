// SettingsScene.cs — 主菜单**设置窗**（第 4 层，随联机页一起建）的自检入口
//
// 用法：… -executeMethod SettingsScene.Run      退出码 0 = 全过
//
// 🔴 **每一条断言的期望值都盯「原版值」**，出处 = `工具/menu_rect.py bundle_menus_assets_all
//    "Main Menu Settings Window"` + 原始 JSON 实读（逐条写在 `Shell/SettingsWindow.cs` 文件头）。
// 🔴 **根上有 `m_LocalScale = 0.9`（只这一扇窗）** ⇒ 世界坐标 = 屏幕中心 + 0.9 ×（原版矩形 − 屏幕中心）：
//    每一条几何断言都过 `SettingsWindow.Screen(...)`，**别直接拿原版矩形当世界坐标**（那会差 11%，
//    而且差得「看起来像对」—— 见 `资料/已知的坑.md` 那类「断言量不到」的教训）。
// 🔴 **2026-10-06（A131②）**：`Screen()`（正向）/ `OrigPxY()`（逆向）**既是建窗的、又是算期望的** ⇒
//    那张映射**自己**写错时，走它们的那一片会**一起假绿**。所以本文件里另有一条**不过它们**的
//    **锚断言**（`CheckRectPx`，期望值是**原版字段手算的字面量**，见 `Run()` 里那节
//    「🔴 ②（A131）映射锚断言」）—— 改版面时**别删它**：删了这几十条几何断言就失去唯一的独立尺子。
//    通则（期望值不许与被测实现共用同一个函数/常量）→ `资料/普查产出_1006/A131_自证通则.md`。
// 🔴 **这一轮修掉的那条真缺陷**：齿轮点了没反应（静默失败）⇒ 这里有一条断言盯着它的点击区。
//
// ⚠️ 不碰玩家的真设置（`NetConfig.OverridePath` 指到临时文件）；不联网（「检查连接」打的是**没人听的端口**，
//    验的就是「如实失败」这条）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using CardPresentation.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;              // 🆕 A176：`GraphicsSettings.currentRenderPipeline`
using UnityEngine.Rendering.Universal;    // 🆕 A176：`UniversalRenderPipelineAsset.renderScale`

public static class SettingsScene
{
    const string P = "[Settings] ";
    const string ShotDir = "d:/4/_tmp_view/settings";

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

    /// <summary>🆕 **2026-10-06（A83② —— A81 的尾巴）**：压暗层（「点窗外关窗」）命中区那条不变量。
    /// 🔴 **2026-10-07（A77⑬⑥）本文件里那份副本已删**（它就是第 5 份）—— 唯一一份在
    /// `MenuDraw.CheckShadeRule`。⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
    /// <para>🔴 **2026-10-07（A77⑬③）那条判据的期望值也换了**：不再比「调用方传进来的常量」
    /// （与 `ShadeHit` 的实参同一个符号 = 同义反复），改成**量同一扇窗里「视觉压暗层」那颗 quad 的
    /// `RenderQueue`**。🔴 **为什么仍要问 `WasShadeHit`**：档本来就对的那几扇窗，走不走公共件
    /// **没有任何可见行为差异** ⇒ 只有那一句能分出两种状态（改回自己那份 `MenuDraw.Hit` 就红）。</para></summary>

    /// <summary>🆕 **2026-10-06（A94 相 2）**：窗内面板「吸收层」（`MenuDraw.Absorb`）那一组 ——
    /// **四条不变量 + 两条真能分辨的行为**。
    ///
    /// <para>语义（判据 → `Shell/MenuDraw.Absorb` 的注释）：原版窗内面板那颗 `Image` 的
    /// `m_RaycastTarget = 1`、而「点它关窗」那颗 `BackgroundCloseButton` **全库都挂在压暗层上**
    /// ⇒ 点窗内空白处**原版什么都不发生**；我们这边命中候选只收 `WindowButton` ⇒ 射线会**穿过面板**
    /// 落到压暗层那颗「点窗外关窗」上（这就是 A94 那个缺陷）。</para>
    ///
    /// <para>🔴 **期望值全是原版值**：矩形 = **原版 prefab 里那块面板 `Image` 的 rect 字面量**
    /// （⛔ 不写被测那份实现**传进去的实参** —— 那是最浅一档的同式自证）。
    /// ⚠️ 本窗传进来的那四个数**已经是画布 px**（= prefab 实读值，那层 0.9 已经烘在里面了，
    /// 见文件头）⇒ 这里**不要再过 `Screen()`**（那会把 391.29 再缩一次）。
    /// 档 = 该窗自己的**原版档常量**（`qShade` / `qContentMin`，与本文件已有的 `CheckShadeRule` 同一个来源）。</para>
    ///
    /// <para>🔴 **为什么两条行为必须一起断**：只断「点面板 ⇒ 不关」时，一个**根本关不掉的窗**也能绿；
    /// 只断「点面板外 ⇒ 关」时，把窗建小到「点哪儿都关」也绿。两条互为对照才分得出这两条路。</para>
    ///
    /// <para>⚠️ **点哪儿（两个点，判据不同）**：
    /// · **面板内**：先试**原版矩形中心**，被窗内真件（按钮）盖住时沿一圈**固定的**候选点找一个
    /// 「命中是吸收层」的点。那一处「命中是谁」**不是期望值**，它只是**选点的条件** ——
    /// 断的是**窗的状态**（`state()`）。
    /// · **面板外**：⛔ **不扫、钉死屏幕左上角 (5,5)**，而且「命中是谁」**就是期望值**
    /// （必须是**本窗压暗层那一颗**：`oHit.transform.IsChildOf(winRoot)` ∧ `MenuDraw.WasShadeHit`）。
    /// 扫一圈会让「某颗命中区过大、把压暗层吃掉一半」这类缺陷从别的候选点上绕过去。
    /// 🔴 **2026-10-06（A141）**：这两句原来在同一段里打架（「不是期望值」管的是面板内那一个点）；
    /// 面板外那条当时写的是「**非吸收层 ∧ 属于本窗**」—— 那是**分不出两种状态**的弱条件
    /// （命中的是**窗内任何别的命中区**它也绿）⇒ 照 `Editor/CollectionScene.cs` 那份收紧。</para></summary>
    static void CheckAbsorbRule(string what, Transform winRoot, string nodeName,
                                float x1, float y1, float x2, float y2,
                                int qShade, int qContentMin, System.Func<WindowState> state)
    {
        // ⚠️ 传进来的矩形**已经是画布 px**（那层 0.9 烘在里面）⇒ 这里不过 `Screen()`（见上面那条注释）。
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
        var q = node != null ? node.GetComponentInChildren<ImageQuad>() : null;
        if (q == null)
        {
            CheckTrue(false, $"{what}：吸收层下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这一层等于没建）");
        }
        else
        {
            float w = q.WorldW * 108f, h = q.WorldH * 108f;
            float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
            CheckNear(cx - w * 0.5f, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图（经 0.9 缩放）");
            CheckNear(cy - h * 0.5f, y1, 1.5f, $"{what}：…**上沿**");
            CheckNear(cx + w * 0.5f, x2, 1.5f, $"{what}：…**右沿**");
            CheckNear(cy + h * 0.5f, y2, 1.5f, $"{what}：…**下沿**");
            // ④ 档 = 内容命中区档 − 1，且**严格夹在**压暗层与内容命中区之间
            int wantQ = qContentMin - 1;
            Check(q.RenderQueue, wantQ,
                  $"{what}：吸收层的档 = **内容命中区档 − 1**（{qContentMin} − 1 = {wantQ}）");
            CheckTrue(qShade < q.RenderQueue && q.RenderQueue < qContentMin,
                      $"{what}：**{qShade} < {q.RenderQueue} < {qContentMin}** —— 严格夹在压暗层与内容命中区之间"
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
        // 点面板外：本批 20 个吸收矩形**全都不覆盖 (5,5)**（相 1 逐条核过）
        const float OutX = 5f, OutY = 5f;
        var oHit = pl.ButtonAt(OutX, OutY);
        // 🔴 **2026-10-06（A141，口径与 `Editor/CollectionScene.cs` 逐字同源）**：判据 = **两条合起来**，
        //   ⛔ 不许再写成「非吸收层 ∧ 属于本窗」那种**分不出两种状态**的弱条件（旧写法下，命中的
        //   只要是**本窗的任意别的命中区**它就绿 —— 那正是 A94 那个缺陷能溜过去的原因）：
        //     · `oHit.transform.IsChildOf(winRoot)` = **是这一扇自己的**命中区（别家的窗顶掉它就红）；
        //     · `MenuDraw.WasShadeHit(oHit.transform)` = **是压暗层那一颗**（`ShadeHit` 建的）。
        //   ⚠️ **按节点上的标记认、不按名字认** —— 那颗节点的名字是各调用点自己传的形参
        //   （本窗叫 `BackgroundHit`，聊天窗叫 `CloseHit`，见 `Shell/ChatPanel.cs`）⇒ 按名字写会误判。
        //   ⛔ **别只写 `WasShadeHit`**：它认的是「是不是压暗层那颗」、**不认「是哪一扇的」**。
        CheckTrue(oHit != null && oHit.transform.IsChildOf(winRoot) && MenuDraw.WasShadeHit(oHit.transform),
                  $"{what}：**({OutX:F0},{OutY:F0}) 命中的就是这扇窗自己的压暗层那一颗**"
                  + "（窗内别的命中区 / 吸收层 / 别家的窗把它顶掉时**这条红** —— 旧写法分辨不出）"
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

    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");

    /// <summary>🆕 音频页那几条比的是**原版 px**，容差 0.3px 且**打印三位小数**
    /// （`CheckNear` 打到小数点后两位 —— 0.3 的差在那种精度下看不出差多少，等于弱断言）。
    /// 🔴 为什么是三行各自比：三行**行高不相等**（105/106/106），差 1px 就是真缺陷。</summary>
    static void CheckNearPx(float got, float want, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= 0.3f, $"{msg} —— 实得 {got:F3}，原版 {want:F3}（容差 0.3px）");

    /// <summary>抓 `act` 跑的那一下里打出来的 **LogError / Exception**（`Application.logMessageReceived`
    /// 在批处理里照常回调 —— 本仓先例：`Editor/BattleScene.cs` 的 `dwCounter` / `hWarn`）。
    /// 用来验 A166 那条「忘了赋 `placement` 就出声」：**必须真的能从日志里分辨**，不是「代码里写了注释」。</summary>
    static List<string> CaptureErrors(System.Action act)
    {
        var got = new List<string>();
        Application.LogCallback cb = (msg, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception) got.Add(msg);
        };
        Application.logMessageReceived += cb;
        try { act(); } finally { Application.logMessageReceived -= cb; }
        return got;
    }

    /// <summary>**世界 y** → 画布 px（左上原点、y 向下），**并把本窗根那层 0.9 缩放去掉**
    /// （原版 px = 540 + (画布 px − 540) / 0.9）。
    /// <para>`540 − world.y×108` 就是 `LayoutSpace.ToPixel` 的 y（本文件别处已这么用：`CheckRectS` /
    /// `CheckLeftS` 里的 `-q.transform.position.y * 108f + 540f`）；再除 `RootScale` 才是原版值
    /// —— 🔴 `SettingsWindow.Screen()` 正好是它的逆（那边是「原版 px → 世界」，这边是「世界 → 原版 px」）。
    /// ⚠️ **节点是摆在矩形中心的**（`MenuDraw.Node` / `MenuDraw.Text` 都传矩形中心）⇒ 要拿「顶」得自己
    /// 再减掉**原版的**半个高（`WfSlider.WorldPos` 会减 0：它本来就是轨道中心）。</para></summary>
    static float OrigPxY(float worldY)
        => 540f + ((540f - worldY * 108f) - 540f) / SettingsWindow.RootScale;

    static float OrigPxY(Transform t) { return OrigPxY(t.position.y); }

    /// <summary>节点位置 = **原版矩形过 `Screen()`（含 0.9 缩放）**之后的中心。</summary>
    static void CheckAtS(Transform t, float x1, float y1, float x2, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        var want = LayoutSpace.RectCenter(s.x1, s.y1, s.x2, s.y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形（经 0.9 缩放）的中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>量一棵子树**渲出来**的像素矩形（`WorldW/H` = 渲染真值；画布 px · 左上原点）。
    /// ⚠️ **九宫格/平铺是一堆子 quad**（`CreateNineSlice` 建 9 个）⇒ 取**所有 active 子 quad 的并集**，
    /// 只取第一个的话量到的是某个角块（2026-09-26 实测：弹窗底量成 182×173）。
    /// 返回 **false = 一个 active 的 `ImageQuad` 都没有** ⇒ 调用方**必须报红**（否则那两条等于没验）。
    /// 🔴 两条矩形断言（`CheckRectS` / `CheckRectPx`）**共用这一份**量法 —— 别各写一遍
    /// （「两处写同一条规则 = 迟早不一致」）。⚠️ `CheckAbsorbRule` 那一条**不走这里**：它只要
    /// 一个**单张** quad 的矩形（吸收层底就是一张平图），所以直接取 `GetComponentInChildren&lt;ImageQuad>()`。</summary>
    static bool RectOf(Transform t, out float lx, out float ty, out float rx, out float by)
    {
        lx = float.MaxValue; ty = float.MaxValue; rx = float.MinValue; by = float.MinValue;
        var qs = t != null ? t.GetComponentsInChildren<ImageQuad>(true) : null;
        if (qs == null || qs.Length == 0) return false;
        bool any = false;
        for (int i = 0; i < qs.Length; i++)
        {
            var q = qs[i];
            if (q == null || !q.gameObject.activeInHierarchy) continue;
            any = true;
            float wpx = q.WorldW * 108f, hpx = q.WorldH * 108f;
            float wx = q.transform.position.x * 108f + 960f, wy = -q.transform.position.y * 108f + 540f;
            lx = Mathf.Min(lx, wx - wpx * 0.5f); rx = Mathf.Max(rx, wx + wpx * 0.5f);
            ty = Mathf.Min(ty, wy - hpx * 0.5f); by = Mathf.Max(by, wy + hpx * 0.5f);
        }
        return any;
    }

    /// <summary>一张图**渲出来**的像素矩形 —— ⚠️ 期望值**过 `Screen()`**（本窗把根那层 0.9 烘进矩形的那个换算）。
    /// <para>🔴 **这条写法自带一个盲区**：`Screen()` 本身写错时它照样绿（期望值与建窗是同一张映射）
    /// ⇒ 同一个宿主里必须另有一条**不过 `Screen()`** 的**锚断言**钉住那张映射 —— 见 `Run()` 里
    /// 那节「🔴 ②（A131）映射锚断言」。**只加断言、不加锚 = 这一片可以一起假绿**（A125/A131）。</para></summary>
    static void CheckRectS(Transform t, float x1, float y1, float x2, float y2, string what)
    {
        float lx, ty, rx, by;
        if (!RectOf(t, out lx, out ty, out rx, out by))
        { CheckTrue(false, what + "（没有 active 的 `ImageQuad` ⇒ 量不到渲染矩形）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        CheckTrue(Mathf.Abs((lx + rx) * 0.5f - (s.x1 + s.x2) * 0.5f) <= 1.5f
                  && Mathf.Abs((ty + by) * 0.5f - (s.y1 + s.y2) * 0.5f) <= 1.5f
                  && Mathf.Abs(rx - lx - s.W) <= 1.5f && Mathf.Abs(by - ty - s.H) <= 1.5f,
                  $"{what} 渲出来 = {rx - lx:F1}×{by - ty:F1} @({(lx + rx) * 0.5f:F1},{(ty + by) * 0.5f:F1})"
                  + $"（应为 {s.W:F1}×{s.H:F1} @({(s.x1 + s.x2) * 0.5f:F1},{(s.y1 + s.y2) * 0.5f:F1})）");
    }

    /// <summary>🆕 **2026-10-06（A131②）**：**不过任何换算函数**的矩形断言 —— 期望值是**字面量**
    /// （= 原版 prefab 字段手算出来的数，算式写在调用点那一段注释里）。
    /// <para>🔴 它存在的唯一理由 = 治「**同一张映射既建窗又算期望**」那种**整片一起假绿**：
    /// `Screen()`（正向）/ `OrigPxY()`（逆向）一旦写错，走它们的那几十条会**一起**绿；
    /// 这一条**不过它们**，映射一错就红。</para>
    /// <para>⛔ 别把它改成「期望值 = 某个常量 ± 某个函数」—— 那就又变成自证了。</para></summary>
    static void CheckRectPx(Transform t, float x1, float y1, float x2, float y2, string what)
    {
        float lx, ty, rx, by;
        if (!RectOf(t, out lx, out ty, out rx, out by))
        { CheckTrue(false, what + "（没有 active 的 `ImageQuad` ⇒ 量不到渲染矩形）"); return; }
        CheckTrue(Mathf.Abs(lx - x1) <= 1.5f && Mathf.Abs(ty - y1) <= 1.5f
                  && Mathf.Abs(rx - x2) <= 1.5f && Mathf.Abs(by - y2) <= 1.5f,
                  $"{what} 渲出来 = [{lx:F2},{ty:F2}]–[{rx:F2},{by:F2}]（{rx - lx:F1}×{by - ty:F1}）"
                  + $"，应落在 [{x1:F2},{y1:F2}]–[{x2:F2},{y2:F2}]（{x2 - x1:F1}×{y2 - y1:F1}）±1.5px"
                  + " —— ⛔ 这一条的期望值**不经过 `Screen()` / `OrigPxY()`**（那张映射写错时，"
                  + "本窗其余几何断言会跟着一起错、只有它会红）");
    }

    /// <summary>`AlignLeft` 会把 Label 的节点挪走（`MainMenuWindowBase` 的注释里写着）⇒
    /// **不能**拿它的位置去比矩形中心，要比**左边缘**。</summary>
    static void CheckLeftS(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（没有 Label）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        float leftPx = lb.transform.position.x * 108f + 960f - lb.WorldW * 108f * 0.5f;
        CheckTrue(Mathf.Abs(leftPx - s.x1) <= 2f, $"{what} 左边缘 = {leftPx:F1}（应为 {s.x1:F1}）");
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>🔴 **2026-10-10（F4）：`Menu Area` / `Tab Buttons` 一律【现取】，⛔ 别存进局部变量。**
    /// <para>**为什么要单开一对助手**：`SettingsWindow.Open()`（= `TryOpen` / `WindowsManager.OpenWindow`
    /// 那两条路都走它）里第一句就是 `Build()`，而 `Build()` 的头一句是
    /// `RewardsWindow.DestroySafe(root.GetChild(i).gameObject)` —— **把窗根的子件全部销毁重建**
    /// （批处理下 = `DestroyImmediate`，旧引用**当场**变假 null）。本文件 A176 那一段
    /// （`win.Close()` + 两次 `win.Manager.OpenWindow(win)`）就重跑了两次 ⇒ 在 `Run()` 开头抓的那些
    /// `Transform` **全部作废**。</para>
    /// <para>**踩过的代价**（2026-10-09 那轮 `settings.log` = 通过 344 / **失败 7**）：`bar`（`Tab Buttons`）
    /// 只在 A176 之前抓过一次 ⇒ 重建后它是 `null` ⇒ `FindChild(null, …)` 又被 `FindChild` 的
    /// `parent == null` 守卫静静地变成 `null` ⇒ **音频页 / 联机页一次都没切过去**
    /// （2 条「点击区 `?`」+ 5 条「没有 active 的 `ImageQuad` ⇒ 量不到渲染矩形」= 同一个根因）。</para>
    /// <para>⚠️ **要缓存的只有 `root`**（= 窗根自身，`Build()` **不**销毁它，只销毁它的子件）——
    /// 这也是这两个助手唯一收的东西。**用一次取一次**，重建多少次都不会拿到旧树。</para></summary>
    static Transform Area(Transform root) { return FindChild(root, "Menu Area"); }
    static Transform Bar(Transform root) { return FindChild(Area(root), "Tab Buttons"); }

    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }
    /// <summary>点一个命中区。**行为**：取到 `WindowButton` 就 `onClick()`，取不到就记一条失败。
    /// <para>🔴 **2026-10-10（F4）就地改掉的**：失败文案原来只打一个 `?`（`t == null` 与
    /// 「节点在但没挂 `WindowButton`」混在一起）—— 分不出「**节点没建**」与「**手里是重建前的旧树**」，
    /// 而那两条红的定位**正耽误在这一步**（诊断原话）。现在拆开说，并另给了
    /// `Click(parent, name)` 那个**能判出是哪一种**的重载。</para></summary>
    static void Click(Transform t)
    {
        if (t == null)
        {
            CheckTrue(false, "点击区**没取到节点**（`FindChild` 返回 null）—— 两种可能，别默认是 ①："
                           + "① 这个节点**没建**；② 调用点手里是**重建前的旧树**"
                           + "（`Open()` = `Build()` 把窗根子件全销毁重建 ⇒ 旧 `Transform` 当场变假 null，"
                           + "又经 `FindChild` 的 `parent == null` 守卫变成 `null`）。"
                           + "⚠️ 想分清就用 `Click(parent, name)` 那个重载。");
            return;
        }
        var b = t.GetComponentInChildren<WindowButton>();
        if (b == null || b.onClick == null)
        { CheckTrue(false, "点击区 `" + t.name + "` **取到了**，但那里没挂 `WindowButton`（或 `onClick` 是空的）"); return; }
        b.onClick();
    }

    /// <summary>`Click` 的「**父 + 名字**」重载：**失败时说清是哪一种**（⛔ 不改行为，只是把文案拆细）。
    /// <para>判据：`parent` 现取（`Bar(root)` / `win.HostBlock` 这种恒活的节点）⇒ 它 `== null` 只可能是
    /// **节点没建**；`parent` 是**存下来的旧引用** ⇒ `== null` = 重建后的**假 null**。</para></summary>
    static void Click(Transform parent, string name)
    {
        if (parent == null)
        {
            CheckTrue(false, "点击区 `" + name + "`：**父节点是 null** —— "
                           + "若调用点传的是**现取的**父（`Bar(root)` / `win.HostBlock`）⇒ 父自己没建；"
                           + "若传的是**存下来的**旧引用 ⇒ 那是 `Build()` 重建后的假 null"
                           + "（⛔ 不是「`" + name + "` 没建」—— 这就是 2026-10-09 那 7 条红的形状）");
            return;
        }
        var t = FindChild(parent, name);
        if (t == null)
        {
            CheckTrue(false, "点击区 `" + name + "` **不在**（父 `" + parent.name + "` 是活的、"
                           + "子树里确实没有这个节点 ⇒ **真的没建**，不是旧树）");
            return;
        }
        Click(t);
    }

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);

        string tmp = Path.Combine(Path.GetTempPath(), "wf_settings_selftest.json");
        NetConfig.OverridePath = tmp;
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        NetConfig.Load();

        // 🔴 **A165**：自检**不许动玩家的真设置** —— 这颗「Small Screen UI」开关的持久化先关掉
        //    （同族先例：`NetConfig.OverridePath` 指到临时文件 · `SettingsWindow.QualitySetterOverride`），
        //    并把内存态放回**出厂值**（原版 `GameStaticData.cctor` 写 0）——
        //    下面「这一格出厂画的是关的图」那条前提全靠它（玩家真存档里可能是开的）。
        SmallScreenUI.PersistOverride = true;
        SmallScreenUI.ResetForTest();
        // 🔴 **A172** 同理：新补的 `Auto Zoom` 那颗也是「点一下就写 PlayerPrefs」⇒ 自检期间同样掐掉持久化、
        //    内存态放回出厂值（原版 `GameStaticData__.cctor` **没写** `+0x125` ⇒ 出厂 = 关）。
        AutoZoom.PersistOverride = true;
        AutoZoom.ResetForTest();
        // 🔴 **A176** 同理，而且它还多一样东西要护住：那颗开关**会写一个真实的工程资产**
        //    （`Assets/Settings/PC_RPAsset.asset` 的 `renderScale`）⇒ ① 自检期间掐掉 `PlayerPrefs`；
        //    ② **当前画质档注入成可控值** —— 那一行的**显隐与生效**都看它，而自检**不许**真去
        //    `QualitySettings.SetQualityLevel`（会把 `QualitySettings.asset` 写脏，见 `QualitySetterOverride`）。
        //    ③ 注入初值取 **`Mobile`(0)**：本文件里那几节的 y 字面量都是**那一行不在**的那支（原版
        //    VeryLow–High 排法，`Vsync` 在第 2 格 / `FPS` 在第 3 格 / 内容高 346.923）—— A176 那一节
        //    再把两态**都**断一遍（见那里）。
        SuperSampling.PersistOverride = true;
        SuperSampling.ResetForTest();
        int ssQuality = 0;                                  // 0 = `Mobile`（不允许超采样）；A176 那节改成 `PC`
        SuperSampling.QualityLevelGetter = () => ssQuality;

        Debug.Log(P + "=== 「设置窗」自检 开始 ===");
        var win = Build(out var root, out var canvasAnchor);
        try
        {
            // ---------------- 窗口参数 ----------------
            Section("窗口参数");
            Check(win.type, WindowType.Popup, "`type` = Popup");
            // 🔴 **2026-10-06（A154）**：这一条原来断的是 **15 Popup**（上面还挂着一条「原版是 5、本轮只记录不改」的
            //    注释）—— **原版写的是 5**，本批**已改** ⇒ 断言跟着翻过来，并且**期望值写成原版那个字面量 `5`**：
            //    · 判据 = 原版那颗 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2730265326332837798.json`
            //      （挂在窗体根 GO `-9019961019057471578` 上，与根 RT `-7066813013973172314` 的 `m_GameObject` 互校过）
            //      的 **`windowsPlacement: 5`**；枚举值 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsPlacement.cs`
            //      （`None=0 / Canvas=5 / World=10 / Popup=15`）。
            //    ⛔ **不写 `WindowsPlacement.Canvas`**（拿我们的枚举名证我们的枚举名），⛔ 也不写 `Create` 里的实参。
            //    🔴 **怎么改坏就红**：把 `SettingsWindow.Create` 那行改回 `WindowsPlacement.Popup` ⇒ 实得 15 ⇒ 红。
            //    ⚠️ **别把这一条推广成「大家都该是 5」**：原版 141 个实例里 15(Popup) 占 **68%**、5(Canvas) 只占 **7%**；
            //      我们能对上的 26 扇窗里**只有设置窗这一扇**不一致（逐窗表 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §①-a/①-b）。
            Check((int)win.placement, 5, "`windowsPlacement` = **5**（原版 MB 原文；5 = Canvas）");
            // 上面那条断「值」、这条断「值**被用上了**」——`AttachToAnchor` 只认 `placement` 去找 Holder。
            // 🔴 **怎么改坏就红**：`AttachToAnchor` 改成忽略 `placement`、一律挂 `Popup` ⇒ 这条红（上面那条照样绿）。
            CheckTrue(canvasAnchor != null && win.transform.parent == canvasAnchor,
                      "本窗**真的挂在 Canvas(5) 那一档的锚点下**（`2 - Canvas Holder Above upper bar`；"
                    + "改坏法：`AttachToAnchor` 不看 `placement` ⇒ 这条红）");
            Check(win.closeOnEsc, true, "`closeOnESC` = 1");
            // 🔴 **2026-10-06（A165）顺手核出的一处【抄错】**：原来 `Create` 里写的是 `1f`（注释还写着「实证 1.0」）——
            //    原版那颗 MB 逐字段实读是 **`extraScaleSmallScreen: 1.2000000476837158`**（本批**已改成 1.2**）。
            //    期望值 = 原版字面量 **1.2**（⛔ 不写 `SettingsWindow` 里的实参）。🔴 **怎么改坏就红**：改回 `1f` ⇒ 红。
            CheckNear(win.extraScaleSmallScreen, 1.2f, 1e-4f,
                      "`extraScaleSmallScreen` = **1.2**（原版 MB 原文；1.2 家族：`BaseOfferPopup`×21 / `BoosterInfoPopup` 等）");
            // 🆕 A83②（A81 的尾巴）：压暗层（「点窗外关窗」）那条不变量 —— 档 = **压暗层自己那一档**
            //   `QShade`(3130)，**严格低于**本窗内容命中区档 `QOverlay`(3135)；并核「这节点确实是
            //   公共件 `MenuDraw.ShadeHit` 建的」。期望值全是本窗自己的**原版档常量**（⛔ 不从被测实现里读）。
            //   逐窗档位 → `Shell/SettingsWindow.cs:277-292`；公共件规矩 → `Shell/MenuDraw.ShadeHit` 的注释。
            //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档 ——
            //      它由 `Shell/SettingsWindow.cs:419-421` 的 `Node(...)` + `Solid(root, "Menu Dark Background", …)`
            //      建（**另一个对象、另一处代码**），⛔ 不再传 `SettingsWindow.QShade`（那与实参同源 = 同义反复）。
            MenuDraw.CheckShadeRule(CheckTrue, "设置窗", win.ShadeHit,
                                    win.transform.Find("Menu Dark Background"), SettingsWindow.QOverlay);
            CheckNear(root.localScale.x, 1f, 1e-4f,
                      "🔴 根节点 **scale 保持 1**（小屏开关**关**着）—— 原版那个 `m_LocalScale = 0.9` 是**烘进坐标**的"
                      + "（见 `SettingsWindow.Screen()` 的订正注释：我们的量测/命中都只认 scale 1 那一帧）");
            var sPop = SettingsWindow.Screen(SettingsWindow.PopL, SettingsWindow.PopT,
                                             SettingsWindow.PopR, SettingsWindow.PopB);
            CheckNear(sPop.W, 1146.95f, 0.5f,
                      "`Screen()` 把原版矩形按 0.9 缩过（弹窗 1274.39 → **1146.95**，不缩就是错的）");

            // ---------------- 🆕 A166：`placement` 忘了赋 ⇒ **出声** ----------------
            // 判据：原版 `windowsPlacement` 在 prefab 里是**必填**的（全库 141 个带该字段的实例**逐个都有值**）
            // ⇒ 我们这边「忘赋」必须**能从日志里分辨**（红线：不许静默失败）。
            // 做法：`GameWindow.placement` 的默认值 = 哨兵 `GameWindow.UnsetPlacement`（= −1；⛔ **不是枚举成员**，
            // 枚举值照原版 `None=0/Canvas=5/World=10/Popup=15`），由 `AttachToAnchor` 拦下 + LogError。
            // （本条属 `WindowsManager` 这一层的不变量，宿主只有 `Editor/{Settings,Shell}Scene.cs` 两个可写白名单 —— 挂在这儿。）
            Section("A166：`placement` 忘了显式赋值 ⇒ 出声（不许静默落成某一档）");
            Debug.Log(P + "  ⚠️ 下面这一条会**故意**打一行 `[Win] …` 的 LogError（就是「出声」本身）—— 那不是失败");
            {
                var probeGo = new GameObject("probe window (placement 未赋)");
                var probeWin = probeGo.AddComponent<GameWindow>();
                CheckTrue(!probeWin.HasPlacement,
                          "裸 `GameWindow` 的 `placement` 出厂是**哨兵**（= 还没显式赋过值）");
                var errs = CaptureErrors(() => WindowsManager.AttachToAnchor(probeWin));
                CheckTrue(errs.Count > 0,
                          "★ **忘了赋 `placement` ⇒ 出声**（改坏法：把默认值改回 `WindowsPlacement.Popup` ⇒ 一声不吭 ⇒ 这条红）"
                        + "；实得 " + errs.Count + " 条：" + (errs.Count > 0 ? errs[0] : "**一条都没有**（静默落成了某档锚点）"));
                CheckTrue(errs.Count > 0 && errs[0].Contains("没有显式赋值"),
                          "★ 出声的内容**点明了「没有显式赋值」**（不是别的错、也不会被别的报错冒充）");
                // 反面（互为对照）：显式赋过值 ⇒ **一声不吭** —— 否则上面那条只是「反正有日志」，分不出两种状态。
                probeWin.placement = WindowsPlacement.Popup;
                CheckTrue(probeWin.HasPlacement, "显式赋过值之后 `HasPlacement` = true（哨兵不是「合法档位」）");
                var errs2 = CaptureErrors(() => WindowsManager.AttachToAnchor(probeWin));
                CheckTrue(errs2.Count == 0, "显式赋过值的窗**不报警**（实得 " + errs2.Count + " 条；上面那条才分得出两种状态）");
                Object.DestroyImmediate(probeGo);
            }

            // ---------------- 几何（全部过 Screen() = 含 0.9）----------------
            Section("弹窗几何（原版矩形 → 经 0.9 缩放）");
            // 🔴 **2026-10-10（F4）**：本片一律 `Area(root)` **现取**（⛔ 不再存成局部变量 `area`）——
            //   本窗根的子件会被 `Build()` **整棵销毁重建**（A176 那一段重跑了两次），存下来的引用会变假 null。
            //   判据 / 踩过的代价 → `Area(root)` 那个助手的注释。
            CheckAtS(Area(root), SettingsWindow.PopL, SettingsWindow.PopT, SettingsWindow.PopR, SettingsWindow.PopB,
                     "`Menu Area`（弹窗本体）");
            CheckRectS(FindChild(Area(root), "Generic Popup Background"), SettingsWindow.PopL, SettingsWindow.PopT,
                       SettingsWindow.PopR, SettingsWindow.PopB, "`Generic Popup Background`（九宫格 `40k_popup`）");
            CheckRectS(FindChild(Area(root), "Background fill"), SettingsWindow.FillL, SettingsWindow.FillT,
                       SettingsWindow.FillR, SettingsWindow.FillB, "`Background fill`（`40k_popup_texture` 平铺）");
            CheckRectS(FindChild(Area(root), "Separators"), SettingsWindow.BarSepL, SettingsWindow.BarSepT,
                       SettingsWindow.BarSepR, SettingsWindow.BarSepB, "`Separators`");
            CheckRectS(FindChild(Area(root), "Generic Close Button"), SettingsWindow.CloseL, SettingsWindow.CloseT,
                       SettingsWindow.CloseR, SettingsWindow.CloseB, "`Generic Close Button`（75×75）");
            CheckRectS(FindChild(FindChild(Area(root), "Generic Close Button"), "Icon"),
                       SettingsWindow.CloseIconL, SettingsWindow.CloseIconT,
                       SettingsWindow.CloseIconR, SettingsWindow.CloseIconB, "关闭钮的 `Icon`");

            // ---------------- 🔴 ②（A131）：**映射锚断言** ----------------
            // 上面那一片（`CheckAtS` / `CheckRectS`）的期望值**整条过 `Screen()`**，而**建窗用的也是它**
            // ⇒ 那张映射本身错了，**这一整片会一起跟着错**（不是某一条假绿）。所以这里补一条
            // **不过 `Screen()` / `OrigPxY()`** 的锚断言：量的是**渲出来的世界矩形**，期望值是**字面量**。
            //
            // 判据 = **原版 prefab 的字段字面量**（⛔ 不是我们的常量、也不是 `Screen()` 的输出）。
            // 原版那张映射 = **画布中心 + 0.9 ×（原版矩形 − 画布中心）**，两半各有出处：
            //  ① **缩放因子 0.9**：根 `Main Menu Settings Window`（`RectTransform_-7066813013973172314`，
            //     `bundle_menus_assets_all`）`m_LocalScale = (0.9,0.9,0.9)` —— 全库**只这一扇窗**是 0.9
            //     （`Rewards Base Submenu Variant` / `Shop Menu Variant` 都是 1）。
            //  ② **缩放中心 = 画布中心**：同一颗 RT `m_AnchorMin = (0,0)` · `m_AnchorMax = (1,1)` ·
            //     `m_AnchoredPosition = (0,0)` · `m_SizeDelta = (0,0)` ⇒ **铺满父矩形**；
            //     `m_Pivot = (0.5,0.5)` ⇒ 它的**轴心 = 父矩形的中心**（Unity：world = 父轴心 + lossyScale × 局部坐标）。
            //     运行期父链（菜单场景 `bundle_scenes_scenes_mainmenuwarpforge` 逐颗实读，
            //     四颗**全是** `anchors (0,0)-(1,1)` · `pos (0,0)` · `sd (0,0)` · `pivot (0.5,0.5)` · `scale 1`）：
            //       `3 - PopUp Holder` ← `Safe area Only Horizontal` ← `MainMenu` ← `Main  Canvas`
            //     ⇒ 父矩形 = 画布矩形、父轴心 = 画布中心 (960,540)。
            //     （⚠️ 原版那颗 MB 写的是 `windowsPlacement = 5`(Canvas) ⇒ 父其实是
            //      `2 - Canvas Holder Above upper bar`；三颗 Holder 的字段逐颗相同 ⇒ 取哪一颗结论一样。）
            //  ③ **手算**（全部来自原版字段，⛔ 没有一步经过 `Screen()`）：
            //     · `Menu Area`（RT `-8564182181658067034`）锚 (0.5,0.5) · `ap (5.2994,−4.647)` ·
            //       `sd (1274.3934, 843.084)` ⇒ 绝对 [328.10,123.11]–[1602.50,966.19]（与 `PopL..PopB` 吻合）；
            //     · `Generic Close Button`（RT `4762046380401655718`）锚 (1,1) · `ap (−6,−6)` · `sd (75,75)`
            //       ⇒ 绝对 [1559.00,91.61]–[1634.00,166.61]；
            //     · 过那张映射：x1 = 960+0.9×(1559.00−960) = **1499.10** · x2 = **1566.60** ·
            //       y1 = 540+0.9×(91.61−540) = **136.44** · y2 = **203.94**（75×0.9 = **67.5**）。
            //  🔴 **为什么挑「关闭钮」当锚**：它离画布中心最远（最近那条边也差 x 599 · y 448；中心差 636.5/410.9）。
            //     映射的「中心」那一半写错时，**贴着中心的件几乎不动** —— 弹窗本体中心离画布中心只有 (5.30, 4.65)
            //     ⇒ 那一条分辨不出两种状态（`屏幕中心 + k×(x−c)` 对贴在 c 上的 x 几乎不敏感）。一条锚要同时
            //     管住「缩放因子」与「缩放中心」两半，就得挑**离不动点最远**的那一件。
            //  ④ **怎么改坏就红**（三个数都是上面那个算式代进去算的）：
            //     `RootScale` 0.9→0.8 ⇒ 实测变 [1439.20…1499.20]（x1 差 59.90px）；
            //     `Screen()` 的缩放中心从画布中心改成 (0,0) ⇒ 变 [1403.10…1470.60]；
            //     整条映射漏掉那 0.9 ⇒ 变 [1559.00…1634.00]（= 未缩放的矩形）。三种都会红。
            Section("🔴 ②（A131）映射锚断言：期望值是原版字段手算的字面量（⛔ 不过 `Screen()` / `OrigPxY`）");
            CheckRectPx(FindChild(Area(root), "Generic Close Button"),
                        1499.10f, 136.44f, 1566.60f, 203.94f,
                        "（②锚）关闭钮渲染矩形 = 原版 [1559.00,91.61]–[1634.00,166.61] 经「0.9 + 画布中心」缩放");
            var bgQ = FindChild(Area(root), "Generic Popup Background").GetComponentInChildren<ImageQuad>();
            CheckTrue(bgQ != null && bgQ.Texture != null && bgQ.Texture.name == "40k_popup",
                      "弹窗底图 = `40k_popup`");

            // ---------------- 左栏三个页签 ----------------
            Section("左栏页签（原版这一列是 VLG：padTop 30 · 每键 178.42×157.68 · 从 y=153.10 起）");
            // 🔴 **2026-10-10（F4）**：`bar` **不存**（原来这里是 `var bar = FindChild(area, "Tab Buttons");`，
            //   而 A176 那一段的 `Close()` + 两次 `OpenWindow` 会把整棵树重建 ⇒ 存下来的 `bar` 变假 null
            //   ⇒ 音频页 / 联机页**一次都没切过去**、并连带 5 条矩形断言假红）。改走**现取** `Bar(root)`。
            CheckAtS(Bar(root), SettingsWindow.BarL, SettingsWindow.BarT, SettingsWindow.BarR, SettingsWindow.BarB, "`Tab Buttons` 列");
            var names = new[] { "Graphics", "Audio", "Online" };
            for (int i = 0; i < 3; i++)
            {
                float t = SettingsWindow.BarT + SettingsWindow.BarPadTop + i * SettingsWindow.TabBtnH;
                var n = FindChild(Bar(root), names[i]);
                CheckTrue(n != null, $"第 {i + 1} 个键 `{names[i]}` 建出来了（我们只建 3 个 —— 原版 5 个，见文件头 ③）");
                if (n == null) continue;
                CheckAtS(n, SettingsWindow.BarL, t, SettingsWindow.BarR, t + SettingsWindow.TabBtnH,
                         $"`{names[i]}` 键在 VLG 算出来的位置（第 {i + 1} 个）");
                CheckTrue(TextOf(n) == names[i], $"`{names[i]}` 的页签文字");
            }
            CheckTrue(FindChild(Bar(root), "General") == null && FindChild(Bar(root), "Account") == null,
                      "原版的 `General`/`Account`/`Support` 三个键**不建**（那几页没做，不摆假键）");

            // 🆕 A17：本窗的换图（关闭钮的圆底 → `40k_bt_close_hover` · 三个页签 → `…_selected` · 画质下拉 → `…_opened`
            //   · 动作钮 → `40K_button_hover`）逐个悬停验一遍；顺带盯 A21「选中态用 `_hover`」
            CheckHoverSwap(win.transform, "设置窗");
            CheckTrue(SettingsWindow.ArtTabBgSel == "40K_settings_button_hover",
                      "A21：页签**选中态**用的是 `…_hover`（原版 `EverguildToggle.onSprite`），**不是** `…_selected`");
            CheckNoMissingSwapArt("设置窗");

            // ---------------- 切页 ----------------
            // 🔴 **2026-10-10（F4）**：切页一律走 `Click(Bar(root), 名字)` —— **父节点现取**
            //   （原来传的是开头抓的那个 `bar`，A176 重建后它是假 null ⇒ 这两处「点不着」
            //   而**只报一句 `点击区 `?``**：分不出「没建」与「旧树」，见 `Click` 的注释）。
            //   ⛔ 别退回 `Click(FindChild(存下来的父, 名字))`。
            Section("切页（只切 activeSelf）");
            var pages = new[] { "Graphics Tab", "Media Tab", "Online Tab" };
            for (int i = 0; i < 3; i++)
            {
                Click(Bar(root), names[i]);
                Check(win.Current, (SettingsTab)i, $"点 `{names[i]}` ⇒ 切到第 {i + 1} 页");
                for (int j = 0; j < 3; j++)
                {
                    var pg = FindChild(root, pages[j]);
                    CheckTrue(pg != null && pg.gameObject.activeSelf == (i == j),
                              $"`{pages[j]}` {(i == j ? "开着" : "关着")}");
                }
                CheckAtS(FindChild(root, pages[i]), SettingsWindow.TabsL, SettingsWindow.TabsT,
                         SettingsWindow.TabsR, SettingsWindow.TabsB, $"`{pages[i]}` 的根矩形 = `Tab Content`");
                var title = FindChild(FindChild(root, pages[i]), "Tab Title");
                CheckTrue(title != null && !string.IsNullOrEmpty(TextOf(title)), $"`{pages[i]}` 有页标题");
                CheckLeftS(title, SettingsWindow.TitleL, SettingsWindow.TitleR, SettingsWindow.TitleT,
                           SettingsWindow.TitleB, "页标题**左对齐**到原版矩形左边缘（fs55 那条）");
            }

            // ---------------- 图像页 ----------------
            Section("图像页：画质档 + VSync（**点了真去改设置**；自检用注入点，不改工程设置）");
            Click(Bar(root), "Graphics");
            int q0 = QualitySettings.GetQualityLevel();
            int qAsked = -1, vAsked = -1;
            SettingsWindow.QualitySetterOverride = lv => qAsked = lv;
            Click(FindChild(root, "Graphics Tab"), "QualityHit");
            SettingsWindow.QualitySetterOverride = null;
            int qWant = (q0 + 1) % Mathf.Max(1, QualitySettings.names.Length);
            Check(qAsked, qWant, $"点画质行 ⇒ 要求切到**下一档**（{q0} → {qAsked}，共 {QualitySettings.names.Length} 档）");
            CheckTrue(win.Flash != null && win.Flash.Contains("画质"), $"点完**有话说**（「{win.Flash}」）");
            CheckTrue(win.QualityLabel != null && !string.IsNullOrEmpty(win.QualityLabel.Text),
                      "画质行上显示了当前档名");
            CheckNear(QualitySettings.GetQualityLevel(), q0, 0.01f,
                      "🔴 **自检没有把工程的画质档改掉**（`QualitySettings.asset` 不许被自检写脏）");

            int v0 = QualitySettings.vSyncCount;
            SettingsWindow.VSyncSetterOverride = c => vAsked = c;
            Click(FindChild(FindChild(root, "Graphics Tab"), "VSync"), "Hit");
            SettingsWindow.VSyncSetterOverride = null;
            Check(vAsked, v0 > 0 ? 0 : 1, $"点 `VSync` 行 ⇒ 要求翻转（{v0} → {vAsked}）");
            CheckNear(QualitySettings.vSyncCount, v0, 0.01f, "🔴 `vSyncCount` 也没被自检改掉");
            Debug.Log(P + "  （图像页建了 `Quality`/`Small Screen UI`/`Auto Zoom`/`Use super sampling`/`VSync`/`FPS limit` 六件 ——"
                    + " `FPS limit` 自 A168 起是**滑块**；那一列自 A170 起是**真的 `Scroll View`**"
                    + "（原版 `RectMask2D` 视口），**A172 起行位回正、整列塞得进视口 ⇒ 滚不动**；"
                    + "原版那几行里 `Text In Hand Selector` / `Hi FPS` / `Android extra compatibility` 运行时**都不在**；"
                    + "`Use super sampling` **A176 起建了**（原版：只在允许超采样的档出现；"
                    + "🔴 旧记录写「我们没超采样能力 ⇒ 不建」——**已作废**，见 `SuperSampling` 那个类））");

            // ---------------- 🆕 A170 + 2026-10-07 A172：图像页那一列 = 真的 `Scroll View`（原版 `ScrollRect` + `Viewport(RectMask2D)` + VLG `Content`）----------------
            // 判据（2026-10-06/07 **逐字段实读** `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`，⛔ 不是二手表）：
            //  · `Scroll View` RT `-7750568603365769306`（父 = `Graphics Tab`）：`m_SizeDelta = (987.37, 521.5072)`；
            //    它挂的 `ScrollRect` `-4278272035212787802`：`m_Horizontal 1` · `m_Vertical 1` ·
            //    **`m_MovementType 1`(Elastic)** · `m_Elasticity 0.1` · `m_Inertia 1` · `m_DecelerationRate 0.135` ·
            //    `m_ScrollSensitivity 1`；`m_Content` / `m_Viewport` 分别指下面那两个。
            //    ⚠️ 它序列化的 `m_AnchoredPosition` 是**模板位**（父级 VLG 会覆盖）⇒ 落位后的绝对矩形
            //    取 `menu_dump --relative` 排完的那份：**[551.52,432.50]–[1538.89,954.00]**。
            //  · `Viewport` RT `6815893749579022246`：`m_AnchorMin (0,0)` / `m_AnchorMax (1,1)` ·
            //    **`m_SizeDelta.x = −24`**（⇒ 左右各内缩 **12**）· `m_AnchoredPosition (0,0)`（⇒ 上下与 `Scroll View` 齐）；
            //    挂 `RectMask2D` `-6600671332037066842`：`m_Padding (0,0,0,0)` · `m_Softness (0,0)`（硬边）
            //    ⇒ 绝对 **[563.52,432.50]–[1526.89,954.00]**（高 **521.5072**）。
            //  · `Content` RT `-5664032482510274650`：VLG `-5468938573918535770`（`m_Spacing 5` · `m_Padding 0` ·
            //    `m_ChildAlignment 0`(UpperLeft) · `m_ChildControlHeight 0` · `m_ChildForceExpandHeight 0`）
            //    + CSF **`m_VerticalFit 0`**（不撑高；🔴 「一个值 ≠ 全部情况」：横向那半是 `m_HorizontalFit 2`
            //      ⇒ 宽度是 CSF 撑的、序列化的 `−508.16` 只是模板值，宽真值 = 963.37 − 508.16 = **455.21**）。
            //  · 🔴 **运行时那几行**（判据全文 → `资料/普查产出_1007/审查_A170两条前提.md` §③）：
            //    `OnSetup` 尾部那段链式 `SetActive` **无条件跑** ⇒ `Hi FPS` / `Android extra compatibility` 都不在；
            //    `Use super sampling` 只在**允许超采样的档**（`allowSuperSampling` 五档 = 0/0/0/0/1）⇒ 两种排法：
            //      VeryLow–High：`Small Screen(0)·Auto Zoom(1)·Vsync(2)·FPS(3)` ⇒ 内容高 **346.923**
            //      Ultra       ：中间插 `Use super sampling(2)` ⇒ `Vsync(3)·FPS(4)` ⇒ 内容高 **427.564**
            //      🔴 **2026-10-10（A176）订正**：本行原来写的是 **508.205** —— 那个数 =「**5 个勾选行** + FPS」
            //      = **6 行**，而运行时那一档只多出**一行**（可见行 = SmallScreen · AutoZoom · Use super sampling ·
            //      Vsync · FPS = **4 勾选 + FPS**）⇒ 4 × 80.641 + 105 = **427.564**。两条已知值交叉自洽：
            //      4 行 ⇒ 346.923（A172 实装）、7 行（prefab 全在）⇒ 588.84（A168/A170 那个旧值）。
            //    两支**都比视口高 521.51 矮** ⇒ **这一列滚不动**
            //    （`Content.m_SizeDelta.y = 300` + CSF 不撑高 ⇒ `GetBounds()` 只取 Content 自己的矩形 ⇒ `CalculateOffset` 恒 0；
            //     ⚠️ 精确说法：Elastic 下**能抖、有橡皮筋，但停不住**）。
            // 🔴 期望值**两套口径分开**：**节点位置**走 `CheckAtS`（过 `Screen()` —— 那张映射本身另有一条
            //    **不过它**的锚断言钉着，见 A131② 那节）；`MenuScroll` 那几个数 = **画布 px 的字面量**
            //    （= 原版设计值 × 0.9，算式逐条写在消息里）。⛔ 别把两套混起来当同一把尺。
            Section("A170+A172：图像页那一列 = 真的 `Scroll View`（原版 `RectMask2D` 视口；**没有可停留的滚动范围**）");
            {
                var gTab = FindChild(root, "Graphics Tab");
                var svN = FindChild(gTab, "Scroll View");
                CheckTrue(svN != null, "那一列建出来了（节点名 `Scroll View` —— 原版那一格 GO 名就是它）");
                var vpN = svN != null ? FindChild(svN, "Viewport") : null;
                CheckTrue(vpN != null, "`Viewport` 在（原版挂 `RectMask2D` 的就是它；没有它 = 没裁切 ⇒ 红）");
                var ctN = vpN != null ? FindChild(vpN, "Content") : null;
                CheckTrue(ctN != null, "`Content` 在（原版带 VLG 的那一格，四行的父节点）");
                if (svN != null)
                    CheckAtS(svN, 551.52f, 432.50f, 1538.89f, 954.00f,
                             "`Scroll View` 落在原版 [551.52,432.50]–[1538.89,954.00]"
                           + "（改坏法：照它序列化的模板位摆 ⇒ 差几百 px ⇒ 红）");
                if (vpN != null)
                    CheckAtS(vpN, 563.52f, 432.50f, 1526.89f, 954.00f,
                             "`Viewport` 落在原版 [563.52,432.50]–[1526.89,954.00]（左右各内缩 12）"
                           + "（改坏法：不内缩 ⇒ 与 `Scroll View` 同宽 ⇒ 红）");
                var gsc = win.GfxRowsScroll;
                CheckTrue(gsc != null, "这一列接的是 `MenuScroll`（**全壳唯一一份滚动实现** —— 另写一套偏移/夹取 ⇒ 红）");
                if (gsc != null)
                {
                    CheckTrue(gsc.Vertical, "纵向滚动区（原版 `m_Vertical 1`；给成横的 ⇒ 红）");
                    CheckTrue(gsc.Elastic,
                              "档位 = **Elastic**（原版 `m_MovementType 1`；`MenuScroll.Elastic` 默认 `false` = Clamped ⇒ 少写那一句就红）");
                    // 🔴 四个数 = 原版设计值 × 0.9：563.52→**603.17** · 432.50→**443.25** · 1526.89→**1470.20** · 954.00→**912.60**
                    CheckNearPx(gsc.Viewport.x1, 603.17f, "视口左沿 = 原版 563.52 × 0.9（`Scroll View` 左沿 551.52 + 12 内缩）");
                    CheckNearPx(gsc.Viewport.y1, 443.25f, "视口上沿 = 原版 432.50 × 0.9（`m_SizeDelta.y = 0` ⇒ 与 `Scroll View` 齐）");
                    CheckNearPx(gsc.Viewport.x2, 1470.20f, "视口右沿 = 原版 1526.89 × 0.9（改坏法：不内缩 ⇒ 1481.00 ⇒ 红）");
                    CheckNearPx(gsc.Viewport.y2, 912.60f, "视口下沿 = 原版 954.00 × 0.9");
                    // 🔴 A172：内容高 = **运行时那 4 行**（VeryLow–High 那一档）346.923 设计 px，**不是** A170 那 588.84
                    //    ⚠️ **A176 起这一节量的是「超采样那一行不在」的那一支**（本文件把当前画质档注入成 `Mobile`，
                    //    见 `Run()` 开头那条注释）—— 两支的对照与订正见下面 A176 那一节。
                    CheckNearPx(gsc.ContentX2 - gsc.ContentX1, 312.23f,
                                "内容高 = 原版运行时 4 行合计 346.923 设计 px × 0.9（3 × 75.641 + 105 + 3 × 5）"
                              + "（改坏法：写成旧的「7 行 588.84」或「3 行」⇒ 红）");
                    // ★★ A172 的核心：**这一列滚不动**（内容高 346.92 < 视口高 521.51 ⇒ 可滚范围 = 0）
                    // 🔴 **2026-10-09（A269）就地订正**：期望值从 **−157.12** 改成 **0**。
                    //    判据 = UGUI 真源码（`MyGame/Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs`）：
                    //    `AdjustBounds`（`:1332-1352`，原版注释 *"Scrolling is **only** possible when content is **larger** than view"*）
                    //    ⇒ 内容比视口小时 content bounds 被**撑到 view 大小**；随后 `InternalCalculateOffset`（`:1386-1426`）
                    //    又夹一道 `if (maxOffset > 0.001f)` ⇒ **这个偏移恒 0**。
                    //    ⇒ `MenuScroll.MaxOffset/MinOffset` 现在就是**照 `AdjustBounds` 调整过**的值
                    //    （原来给的是**没调整过的裸值**；本仓那条「`MaxOffset` 是负的、别拿它当能滚多远」的口径随之作废）。
                    //    改坏法 ①：`GfxContentH(...)` 的勾选行数写回 6（= 旧的「7 行」那一档、内容高 588.84）
                    //      ⇒ 这里变 +217.73 ⇒ 红；
                    //    改坏法 ②：把 `MenuScroll.MaxOffset` 退回裸值 `ContentX2 - ViewHi` ⇒ 这里变 −157.12 ⇒ 红。
                    CheckNearPx(gsc.MaxOffset, 0f,
                                "内容高 346.923 矮于视口高 521.5072 ⇒ **可滚范围 = 0**"
                              + "（照原版 `AdjustBounds`：内容矮于视口时 content bounds 被撑到 view 大小 ⇒ 滚不动）");
                    // 🔴 **2026-10-09（A269）**：原来这里还有一条 `CheckNearPx(gsc.ClampHi, 0f, …)` ——
                    //    A269 之后 `ClampHi` **就**是 `MaxOffset` ⇒ 那一条成了**同义反复**（拿同一个数比它自己），
                    //    按本仓「断言不许自证」的纪律**换成行为断言**：推它一把也动不了。
                    //    判据 = 原版 `InternalCalculateOffset` 的 `if (maxOffset > 0.001f)` 夹法（`ScrollRect.cs:1419`）；
                    //    改坏法 ③：把 `MenuScroll.SetOffset` 里那句夹取删掉 ⇒ `Offset` 变 9999 ⇒ 红。
                    //    ⚠️ 安全：夹到 0 = 与当前 `Offset` 相同 ⇒ `SetOffset` 早退、**不触发 `OnChanged`**（不会重建页面）。
                    float offBefore = gsc.Offset;
                    gsc.SetOffset(9999f);
                    CheckNearPx(gsc.Offset, offBefore,
                                "★★ **推一把也动不了**：`SetOffset(+9999)` 被夹回原处（可滚范围为空 —— 这条是「滚不动」的**行为**判据）");
                    gsc.SetOffset(-9999f);
                    CheckNearPx(gsc.Offset, offBefore, "★ 反方向同样夹回原处");
                    CheckNearPx(gsc.Offset, 0f, "开页时停在第 0 位（原版 `Content.m_AnchoredPosition.y = 0`：不滚）");
                }
                CheckTrue(ctN != null && FindChild(ctN, "Small Screen UI") != null
                          && FindChild(ctN, "Auto Zoom") != null
                          && FindChild(ctN, "VSync") != null && FindChild(ctN, "FPS Limit") != null,
                          "四行都建在 `Content` 底下（原版那几行的父节点 —— 挂到页面上就滚不动了 ⇒ 红）");
                // ★ A172：四行**全在视口里** ⇒ 打开这一页就看得见滑块（A168/A170 那版整根滑块在视口外）
                var fps0 = ctN != null ? FindChild(ctN, "FPS Limit") : null;
                var sl0 = fps0 != null ? FindChild(fps0, "FPS Slider") : null;
                CheckTrue(sl0 != null && FindChild(sl0, "Background") != null,
                          "★★ **不滚就看得见滑块**：`FPS Slider` 的轨道（`Background`）建出来了 —— "
                        + "第 3 行行顶 674.42 + 84.2 = 758.62、底 771.62 都 < 视口底 954.00"
                        + "（A168/A170 那版把它摆在「第 6 行」（916.10）⇒ 整根落在视口外 ⇒ 才有那个 67.34 的滚动，**那是错的格位号**）");
                CheckTrue(fps0 != null && FindChild(fps0, "Hit") != null,
                          "★ 同一条的**射线那一面**：命中区也在（`MenuDraw.Hit` 整块在视口外才会返回 null）");
                // `VSync` = 第 2 行：行顶 432.50 + 2 × 80.641 = **593.78**、行底 669.42（design px）
                var vsR = ctN != null ? FindChild(ctN, "VSync") : null;
                if (vsR != null)
                    CheckAtS(vsR, 563.52f, 593.78f, 1018.73f, 669.42f,
                             "`VSync` 落在**第 2 行**（432.50 + 2 × 80.641 = 593.78）"
                           + "（改坏法：写回旧序号 5 ⇒ 每行低 3 格 ≈ 241.9px ⇒ 红）");
                // `Auto Zoom` = 第 1 行（A172 新补的那颗）：行顶 513.14、行底 588.78
                var azR = ctN != null ? FindChild(ctN, "Auto Zoom") : null;
                if (azR != null)
                    CheckAtS(azR, 563.52f, 513.14f, 1018.73f, 588.78f,
                             "`Auto Zoom` 落在**第 1 行**（432.50 + 1 × 80.641 = 513.14）—— 原版运行时一直在的那一行"
                           + "（改坏法：不建它 / 序号错 ⇒ 红）");
                // ★ 机制：把内容**人为撑高** ⇒ 这一列又能滚（原版到不了这一档；验的是我们那套「偏移 + 视口外不建」没坏死）
                if (gsc != null && ctN != null)
                {
                    float keepX2 = gsc.ContentX2;
                    gsc.ContentX2 = gsc.ContentX1 + 1000f;      // 内容高 1000 > 视口 469.35 ⇒ `MaxOffset` = 530.65 画布 px
                    gsc.SetOffset(gsc.MaxOffset);
                    CheckNearPx(gsc.MaxOffset, 530.65f,
                                "（机制）内容撑到 1000 画布 px ⇒ `MaxOffset` = 1000 − 469.35 = **530.65**");
                    CheckTrue(Mathf.Abs(gsc.Offset - gsc.MaxOffset) <= 0.3f,
                              $"（机制）这时才滚得动：`Offset` = {gsc.Offset:F2}（= `MaxOffset`）");
                    var fpsS = FindChild(ctN, "FPS Limit");
                    CheckTrue(fpsS != null && FindChild(FindChild(fpsS, "FPS Slider"), "Background") == null,
                              "（机制）滚到 530.65 之后 FPS 那一行整根跑到视口**上方** ⇒ 轨道**不建**"
                            + "（等价原版 `RectMask2D` 裁掉；改坏法：画这一列不给 `clip` ⇒ 照样建 ⇒ 红）");
                    var vsS = FindChild(ctN, "VSync");
                    CheckTrue(vsS != null && FindChild(vsS, "Toggle") == null,
                              "（机制）同上，`VSync` 那一行的方框也**不建**了（行顶 593.78 − 589.6 = 4.2 设计 px，在视口上沿之外）");
                    // 还原：内容高 + 位移都放回去（下面 A168 那节量的是**默认那一帧**）
                    gsc.ContentX2 = keepX2;
                    gsc.SetOffset(0f);
                    CheckNearPx(gsc.ClampHi, 0f, "（还原）内容高放回去 ⇒ `ClampHi` 又回到 0（滚不动）");
                    CheckNearPx(gsc.Offset, 0f, "（还原）位移回到 0");
                }
            }

            // ---------------- 🆕 A168：图像页第 3 行 = 「FPS 上限」**滑块**（原版 `GraphicsTab.fpsLimit`）----------------
            // 判据（2026-10-06 实读）：
            //  · 反编译桩 `GraphicsTab.cs`：`private Slider fpsLimit` + `FPSLimitValueChanged(float)`；
            //  · 解包 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5646828332852936614.json`：
            //    `m_MinValue 0` · `m_MaxValue 2` · `m_WholeNumbers 1` · `m_Direction 0`(LeftToRight) ·
            //    `m_FillRect`=Fill · `m_HandleRect`=Handle · `m_TargetGraphic`=Handle 那颗 Image；
            //    子件：`Background`(Sliced `Volume_bar_inactive` 400×31 border 184,0,184,0 **ppuMul 2**) ·
            //          `Fill`(Sliced `Volume_bar_active` 64×31 border 30,0,30,0 ppuMul 2) ·
            //          `Handle`(Simple `Volume_button` 110×110 **preserveAspect**，框 46.811×35.406) ·
            //          三个刻度 `30 FPS`/`60 FPS`/`Unlimited`（框 228.02×62 · fs 42 · 灰 (0.745,0.745,0.745,1) · 居中）；
            //  · 取值去向 `PlayerDataManager__ApplySettingsOptions.c`（与 `__ApplyGraphicsQuality.c` 同形）：
            //      0→30 · 1→60 · 2→−1(无限) · **其它任何非 0 → 60**；末尾 `Application.targetFrameRate = 值`。
            // 🔴 期望值一律是**原版 prefab 手算的绝对画布 px 字面量**（⛔ 不过 `Screen()` —— 它是被测实参，同 A131②/A165 口径）。
            //    **A172 起这一节量的是「页面刚打开」那一帧**（行顶 = 第 3 行 674.42、**不滚**）——
            //    旧字面量（893.66/905.36/899.51）是「A168 那条固定上移 60.61」叠出来的，已整体订正（y 差 −156.90）。
            Section("A168：图像页 `FPS Limit` 那一行 = **滑块**（原版是 `Slider`，不是勾选行）");
            {
                var fpsRow = FindChild(FindChild(root, "Graphics Tab"), "FPS Limit");
                CheckTrue(fpsRow != null, "那一行建出来了（节点名 `FPS Limit`）");
                CheckTrue(fpsRow != null && FindChild(fpsRow, "Toggle") == null,
                          "★ 这一行**不再是勾选行**（旧实现的 `Toggle` 方框不在了；改回 `BuildCheckRow` ⇒ 这条红）");
                var slNode = fpsRow != null ? FindChild(fpsRow, "FPS Slider") : null;
                CheckTrue(slNode != null,
                          "`FPS Slider` 滑块节点在（原版那颗 Slider 的 GO 名就叫 `FPS Slider`）");
                if (slNode != null)
                {
                    // ① 轨道：矩形 + 图（期望值 = 原版 491.18×13 经 0.9；左沿 842.57 是原版绝对 x，**横向没动过**）
                    var bgN = FindChild(slNode, "Background");
                    CheckRectPx(bgN, 842.57f, 736.76f, 1284.63f, 748.46f,
                                "（A168）轨道渲出来 = 原版 491.18×13 经 0.9 ⇒ **442.06 × 11.70**"
                              + "（左沿 842.57 = 原版绝对 x；上沿 = **第 3 行**那一帧：行顶 674.42 + 84.2 = 758.62"
                              + " ⇒ × 0.9 = 736.76）"
                              + "；改坏法：`FpsSliderW` 传未缩放的 491.18 ⇒ 宽差 44px ⇒ 红；"
                              + "行顶写成「第 6 格」（A168/A170 那版）⇒ 整根掉到视口外、这里量不到 ⇒ 红");
                    var fpsBgQ = bgN != null ? bgN.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(fpsBgQ != null && fpsBgQ.Texture != null && fpsBgQ.Texture.name == "Volume_bar_inactive",
                              "轨道的图 = `Volume_bar_inactive`（原版 `Background.m_Sprite`）");
                    // ② 端帽 = `m_Border ÷ ppuMul`（184 ÷ 2 = 92 ⇒ 屏上 82.8）—— 量九宫格**左边那一块**的宽
                    float capW = 0f;
                    if (bgN != null)
                    {
                        var kq = bgN.GetComponentsInChildren<ImageQuad>(true);
                        for (int k = 0; k < kq.Length; k++)
                            if (kq[k] != null && kq[k].WorldW * 108f < 200f) capW = Mathf.Max(capW, kq[k].WorldW * 108f);
                    }
                    CheckNear(capW, 82.8f, 2f,
                              "轨道九宫格的**左端帽**宽 = 原版 `m_Border 184 ÷ m_PixelsPerUnitMultiplier 2` = 92 设计 px × 0.9 ⇒ **82.8**"
                            + $"（实得 {capW:F2}；改坏法：端帽传 184（贴图 px 原样，音频页那三根现在就是这么画的）⇒ 184；"
                            + "或只过 0.9 没除 ppuMul 2 ⇒ 165.6 —— 两种都 ⇒ 红）");
                    // ③ 手柄：图 + 渲出来的边长
                    var hN = FindChild(slNode, "Handle");
                    var hq = hN != null ? hN.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(hq != null && hq.Texture != null && hq.Texture.name == "Volume_button",
                              "手柄的图 = `Volume_button`（原版 `Handle.m_Sprite`）");
                    CheckNearPx(hq != null ? hq.WorldW * 108f : -1f, 31.87f,
                                "手柄**渲出来的边长** = 原版 35.406（框 46.811×35.406 + 110×110 方图 preserveAspect ⇒ 取短边）× 0.9"
                              + "（改坏法：按**旧的 22.406** 画 ⇒ 20.17 ⇒ 红。"
                              + "🔴 **2026-10-10 措辞订正（A202②）**：原文写「跟**音频页那根一样**按 22.406 画」——"
                              + "音频页 2026-10-07（A197）起**已经是 31.87**，那句不再成立 ⇒ 只说 22.406 这个旧值）");
                    // ④ 三档：写进去的帧率 + Fill 的宽 + 手柄中心 x
                    var fillN = FindChild(slNode, "Fill");
                    string[] tier = { "档 0（`30 FPS`）", "档 1（`60 FPS`）", "档 2（`Unlimited`）" };
                    float[] wantFillW = { 0f, 221.03f, 442.06f };            // 值/2 × 491.18 × 0.9（原版 Fill 锚点驱动）
                    float[] wantHandleCx = { 853.37f, 1069.90f, 1286.43f };  // 轨道左 + (12 + 值/2×481.18) × 0.9
                    float[] wantFps = { 30f, 60f, -1f };
                    float trackL = 842.57f;
                    int f0 = Application.targetFrameRate;
                    for (int i = 0; i < 3; i++)
                    {
                        win.SetFpsIndex(i, true);
                        Check(Application.targetFrameRate, (int)wantFps[i],
                              $"{tier[i]} ⇒ `Application.targetFrameRate` = {wantFps[i]}"
                            + "（判据 = 原版 `ApplySettingsOptions`：0→30 · 1→60 · 2→−1）");
                        float flx = 0f, fty = 0f, frx = 0f, fby = 0f;
                        bool hasFill = fillN != null && RectOf(fillN, out flx, out fty, out frx, out fby);
                        if (i == 0)
                            CheckTrue(!hasFill, "档 0 的 `Fill` **不画**（原版 `anchorMax.x = 值/2 = 0` ⇒ 宽 0；改坏法：画成满宽 ⇒ 红）");
                        else
                        {
                            CheckTrue(hasFill, $"{tier[i]} 的 `Fill` 画出来了");
                            if (hasFill) CheckNearPx(frx - flx, wantFillW[i],
                                $"{tier[i]} 的 `Fill` 宽 = 原版 值/2 × 491.18 经 0.9 ⇒ {wantFillW[i]} 画布 px");
                            if (hasFill) CheckNearPx(flx, trackL, $"{tier[i]} 的 `Fill` 左沿 = 轨道左沿");
                        }
                        if (hq != null)
                            CheckNearPx(hq.transform.position.x * 108f + 960f, wantHandleCx[i],
                                        $"{tier[i]} 的手柄中心 x = 轨道左 + (12 + 值/2 × 481.18) × 0.9 ⇒ {wantHandleCx[i]}"
                                      + "（12 = 原版 `Handle.m_AnchoredPosition.x`：`Slider` 只驱动手柄**锚点**、不动这个偏移）");
                    }
                    // ⑤ 点 / 拖的取值规则（原版 `Slider.UpdateDrag`：`clamp01((x − 滑区左)/滑区宽)` → 按 `m_WholeNumbers` 取整）
                    float areaW = (491.18f - 10f) * 0.9f;                    // 滑区 481.18 × 0.9 = 433.06 画布 px
                    win.SetFpsFromCanvasX(trackL - 50f);
                    Check(win.FpsIndex, 0, "点轨道**左端之外** ⇒ 夹到档 0（原版 `Mathf.Clamp01`）");
                    win.SetFpsFromCanvasX(trackL + 0.30f * areaW);
                    Check(win.FpsIndex, 1, "点在 0.30 处 ⇒ 档 1（0.60 → `Mathf.Round` ⇒ 1）");
                    {
                        float gx1 = 0f, gy1 = 0f, gx2 = 0f, gy2 = 0f;
                        bool ok2 = fillN != null && RectOf(fillN, out gx1, out gy1, out gx2, out gy2);
                        CheckTrue(ok2 && Mathf.Abs((gx2 - gx1) - 221.03f) <= 0.3f,
                                  $"★ 那一「点」之后 `Fill` 的宽 = 221.03（档 1）—— **忘了按整数取整**的话会是 0.60 × 442.06 = 265.24 ⇒ 红"
                                + $"（实得 {(ok2 ? gx2 - gx1 : -1f):F2}）");
                    }
                    win.SetFpsFromCanvasX(trackL + 0.51f * areaW);
                    Check(win.FpsIndex, 1, "点在 0.51 处 ⇒ 仍是档 1（1.02 也取整成 1）");
                    win.SetFpsFromCanvasX(trackL + 0.80f * areaW);
                    Check(win.FpsIndex, 2, "点在 0.80 处 ⇒ 档 2（1.60 → 2）");
                    Check(Application.targetFrameRate, -1, "…而且真的写成了 **−1**（无限）");
                    win.SetFpsFromCanvasX(trackL + areaW + 50f);
                    Check(win.FpsIndex, 2, "点滑区**右端之外** ⇒ 夹到档 2");
                    // ⑥ 档 → 帧率 的映射（原版 `ApplySettingsOptions` 那段 if 链，逐条）
                    Check(SettingsWindow.FpsOfIndex(0), 30, "档 0 ⇒ 30（原版 `SetTargetFramerate(0x1e)`）");
                    Check(SettingsWindow.FpsOfIndex(1), 60, "档 1 ⇒ 60（原版 `SetTargetFramerate(0x3c)`）");
                    Check(SettingsWindow.FpsOfIndex(2), -1, "档 2 ⇒ −1 = 不限帧（原版 `0xffffffff`）");
                    Check(SettingsWindow.FpsOfIndex(3), 60,
                          "🔴 **滑块给不出来的档 3 也走 60** —— 原版那条是：`iVar1 != 0` 里**只有 `== 2` 走 −1、其余全 60**"
                        + "（⛔ 写成「不是 1 就给无限」⇒ 这条红）");
                    Check(SettingsWindow.FpsOfIndex(-5), 60, "同上，负值也一样（走的是 `iVar1 != 0` 那一段）");
                    // ⑦ 反查（开窗时把滑块摆到当前帧率上）
                    Check(SettingsWindow.FpsIndexOfTarget(30), 0, "`targetFrameRate 30` ⇒ 档 0（开窗初值）");
                    Check(SettingsWindow.FpsIndexOfTarget(60), 1, "…60 ⇒ 档 1");
                    Check(SettingsWindow.FpsIndexOfTarget(-1), 2, "…−1（不限帧）⇒ 档 2");
                    Check(SettingsWindow.FpsIndexOfTarget(0), 2, "…0（本工程没设过时的默认）也归到档 2（= 旧勾选行显示 unlimited 的口径）");
                    win.SetFpsIndex(SettingsWindow.FpsIndexOfTarget(f0), false);
                    Application.targetFrameRate = f0;
                    Check(Application.targetFrameRate, f0, "🔴 自检没把进程的 `targetFrameRate` 留在别的值上");
                    CheckTrue(win.Flash != null && win.Flash.Contains("帧率上限"), $"改档**有话说**（「{win.Flash}」）");
                    // ⑧ 三个刻度：节点名 / 文字 / 中心 x（期望值 = 原版框中心，行内 x + 114.01，经 0.9）
                    string[] tickTxt = { "30", "60", "Unlimited" };
                    float[] tickCx = { 852.48f, 1069.29f, 1281.78f };
                    for (int i = 0; i < 3; i++)
                    {
                        var tk = FindChild(slNode, SettingsWindow.FpsTickName[i]);
                        CheckTrue(tk != null, $"刻度 {i + 1} 节点在（名字照原版的 GO 名 `{SettingsWindow.FpsTickName[i]}`）");
                        if (tk == null) continue;
                        CheckTrue(TextOf(tk) == tickTxt[i], $"刻度 {i + 1} 的字 = `{tickTxt[i]}`（原版 TMP 的 `m_text`；第 3 个原版是本地化词条）");
                        var tkl = tk.GetComponentInChildren<Label>();
                        CheckNear(tkl != null ? tkl.transform.position.x * 108f + 960f : -999f, tickCx[i], 1.5f,
                                  $"刻度 {i + 1} 的**中心 x** = 原版框中心（行内 x {SettingsWindow.FpsTickX[i]} + 114.01）经 0.9");
                    }
                    // ⑨ 行标题（原版 `Title`：框左沿在行内 +16）
                    var ttl = fpsRow != null ? FindChild(fpsRow, "Title") : null;
                    CheckTrue(ttl != null && TextOf(ttl) == "FPS limit",
                              "行标题 = `FPS limit`（原版 TMP 的 `m_text` 是西语 `'Límite de FPS'`、没挂 I2 词条 ⇒ 照文件头 ② 写英文）");
                    var tlb = ttl != null ? ttl.GetComponentInChildren<Label>() : null;
                    CheckNear(tlb != null ? tlb.transform.position.x * 108f + 960f - tlb.WorldW * 108f * 0.5f : -999f,
                              617.57f, 2f,
                              "行标题**左沿** = 原版 `Title` 框左沿（行左 563.52 + 16 ⇒ 绝对 617.57 画布 px）");
                    // ⑩ 🔴 「整块在弹窗里」—— A170 起它由**视口裁切**保证（原版那一列那条 `RectMask2D` 的矩形
                    //    就是弹窗内的一块，见原版 `Viewport` = [563.52,432.50]–[1526.89,954.00]）
                    float bx1, by1, bx2, by2;
                    bool ok3 = RectOf(slNode, out bx1, out by1, out bx2, out by2);
                    CheckTrue(ok3 && by2 <= 923.57f + 1.5f,
                              $"★ FPS 那一行画出来的东西（轨道/手柄/填条）都在**弹窗里**：底沿实得 {(ok3 ? by2 : -1f):F2}"
                            + " ≤ 弹窗底 **923.57**（= 原版 `Menu Area` 底 966.19 经 0.9）"
                            + "；A172 起整行（含手柄，实得底沿 ≈ **758.54**）本来就在视口里（视口底 912.60）"
                            + " ⇒ 这一条现在是「没画到弹窗外」的基础守卫，不再是「靠上移救回来」那条");
                    // ⑪ 🔴 **手柄整根都在视口里** —— A172 把行位回正之后，`Handle`（设计 747.42–782.83）
                    //    离视口下沿 954.00 还差 171 px ⇒ **不该被切**。这一条同时盯住两件事：
                    //    ① 行位没被写回「第 6 格」；② `PlaceFps` 那套「按视口缩块 + 截 uv」的裁切在**不越界时是恒等**的
                    //    （⛔ 别让它把没越界的手柄也缩了 —— 那会把 35.406 的手柄画成别的尺寸）。
                    if (hq != null)
                        CheckNearPx(-hq.transform.position.y * 108f + 540f + hq.WorldH * 108f * 0.5f, 758.54f,
                                    "手柄**渲出来的底沿** = 原版第 3 行那一帧的 842.83 × 0.9 = **758.54**"
                                  + "（设计：手柄中心 = 轨道中心 765.12、半高 17.70 ⇒ 底 782.83）"
                                  + "；改坏法：行位写回「第 6 格」⇒ 底沿掉到视口外又被裁 ⇒ 红");
                    // ⑫ 真鼠标点得到（A26 那个坑：裸节点进不了命中表）
                    var hitN = fpsRow != null ? FindChild(fpsRow, "Hit") : null;
                    CheckTrue(hitN != null && hitN.GetComponentInChildren<ImageQuad>() != null,
                              "滑块的命中区里有 `ImageQuad`（裸节点 `PointerLayer` 命中表里没有它 —— A26 那个坑）");
                    var pl = PointerLayer.Instance;
                    float hcx = (trackL + 1284.63f) * 0.5f, hcy = (736.76f + 748.46f) * 0.5f;
                    CheckTrue(pl != null && hitN != null && pl.ButtonAt(hcx, hcy) == hitN.GetComponent<WindowButton>(),
                              $"★ 轨道正中央 ({hcx:F1},{hcy:F1}) 命中的是**这根滑块**（不是吸收层 / 别家的窗）");
                    // ⑬ 🆕 2026-10-07（波 8 · **A193**）：**手柄探出轨道的那一块也点得到**（命中区 = 轨道 ∪ 手柄）。
                    //   判据：原版 `Background`（轨道）与 `Handle`（手柄）两颗 Image 的 `m_RaycastTarget` 都是 **1**
                    //   （九根 + FPS 那根逐颗实读）⇒ 两块都冒泡到父件的 `Slider`（`OnPointerDown` 的 else 分支
                    //   = 跳到点的那个位置）。而手柄中心 = 轨道左 + `m_AnchoredPosition.x`(**12**) + 值/2 × 481.18
                    //   ⇒ **2 档时手柄探出轨道右端 19.7 设计 px**（0 档探出左端 5.7）。
                    //   🔴 A193 前我们只把**轨道**那一块做成命中区 ⇒ 探出那一块**静默点不到**（原版点得到）。
                    //   期望值全是**原版字面量**（842.57 / 1284.63 / 1286.43 / 35.406 / 736.76 / 748.46），
                    //   ⛔ 不引用 `SettingsWindow.FpsSliderW…`（那是被测实参 ⇒ 同式自证）。容差靠**探针离边 6px**
                    //   给（⛔ 不精确比浮点：探针不落在矩形边缘上）。
                    {
                        int f0b = Application.targetFrameRate;
                        win.SetFpsIndex(2, false);          // ⚠️ `fire: false` —— 自检不改进程帧率 / 不写盘
                        var hitB = hitN != null ? hitN.GetComponentInChildren<WindowButton>() : null;
                        CheckTrue(pl != null && hitB != null,
                                  "（前提）FPS 滑块的命中区 `WindowButton` 拿得到 —— 拿不到 = 下面两条等于没验");
                        if (pl != null && hitB != null)
                        {
                            // 2 档：手柄中心 x = 轨道左 + (12 + 481.18) × 0.9 = **1286.43**、
                            // 半宽 = 35.406 × 0.9 ÷ 2 = **15.93** ⇒ 手柄右缘 1302.36、轨道右沿 1284.63
                            // ⇒ 探出 **17.73 画布 px**（= 19.7 设计 px）。y 用轨道中心（两件同中心线）。
                            float fy = (736.76f + 748.46f) * 0.5f;
                            float inX = 1284.63f + 6f;                       // 轨道右沿**之外** 6px —— 仍在手柄里
                            var inHit = pl.ButtonAt(inX, fy);
                            CheckTrue(inHit == hitB,
                                      $"★ 值 = 2 档时**手柄探出轨道右端的那一块**（{inX:F1},{fy:F1}）打得中"
                                    + $"（手柄中心 1286.43 + 半宽 15.93 ⇒ 右缘 1302.36 > 轨道右沿 1284.63；"
                                    + $"实得 `{(inHit != null ? inHit.name : "<null>")}`）"
                                    + "；改坏法：命中区退回「只有轨道」那一块 ⇒ 这一点落到吸收层 ⇒ 红");
                            float outX = 1286.43f + 35.406f * 0.9f * 0.5f + 6f;  // 手柄**右缘之外** 6px
                            var outHit = pl.ButtonAt(outX, fy);
                            CheckTrue(outHit != hitB,
                                      $"★ …而手柄**右缘之外** 6px（{outX:F1},{fy:F1}）**打不中这根滑块**"
                                    + $"（负例：上面那条不是恒真 —— 命中区若铺成一大块「来者都认」就会红；"
                                    + $"实得 `{(outHit != null ? outHit.name : "<null>")}`）");
                        }
                        win.SetFpsIndex(SettingsWindow.FpsIndexOfTarget(f0b), false);   // 还原档位（不 fire）
                    }
                    Check(Application.targetFrameRate, f0,
                          "（A193 收尾）档位摆弄完又把 `Application.targetFrameRate` 归还成进本节之前的值");

                    // ⑭ 🆕 **2026-10-10（A202①）**：「按下这一下算不算点在滑块上」的判据**收口到 `WfSlider.HitBand`**。
                    //   缺陷：`UpdateFpsDrag` 里原来手写的
                    //   `px < s.x1-24 || px > s.x2+24 || py < s.y1-16 || py > s.y2+16`
                    //   是**第二份几何**（硬编码的 `±(24, 16)` 画布 px 余量），与唯一那份判据
                    //   （`WfSlider.HitBand` = **轨道 ∪ 手柄**）**并不等价**（旧带左端宽 19px、纵向半高宽 5.9px）
                    //   ⇒ 早晚分叉。判据全文（两套值的逐维对照）→ `Shell/SettingsWindow.cs` 的 `FpsPressAtCanvas` 注释。
                    //   🔴 **怎么打进去**：批处理里 `Mouse.current` 是 null、`Update` 也不跑 ⇒ 直调
                    //   `win.FpsPressAtCanvas(画布 px, 画布 px)`（= `UpdateFpsDrag` 的按下分支**本身**那段代码），
                    //   断的是**状态** `win.FpsDragging`（⛔ 不是那个方法的返回值 —— 状态才是运行时真被读的东西）。
                    //   🔴 期望值全是**原版字面量**手算：轨道画布 px [842.57,736.76]–[1284.63,748.46]（同 ① 那条）、
                    //   手柄**实画** 35.406 设计 px ⇒ 31.87 画布 px（半 **15.93**）、`m_AnchoredPosition.x = 12`、
                    //   滑区让位 10 ⇒ 各档手柄中心 x = 842.57 + (12 + 值/2 × 481.18) × 0.9。
                    //   ⛔ 不引 `SettingsWindow.FpsSliderW…`（那是被测实参 ⇒ 同式自证）。
                    {
                        int f0c = Application.targetFrameRate, i0c = win.FpsIndex;
                        float cyF = (736.76f + 748.46f) * 0.5f;        // 轨道中心 y（手柄与它同中心线）
                        const float hcx2 = 1286.43f, hcx1 = 1069.90f, hcx0 = 853.37f;   // 三档的手柄中心 x
                        // ① **正例**：值 = 2 档时手柄**探出轨道右端**那一小块（原版 `Handle` 那颗图也是 raycast 目标）
                        win.SetFpsIndex(2, false);                     // 不 fire：自检不改进程帧率
                        win.FpsPressAtCanvas(1284.63f + 6f, cyF);      // 轨道右沿**之外** 6px、手柄右缘之内（1302.36）
                        CheckTrue(win.FpsDragging,
                                  $"★（A202①）2 档时**轨道右端之外 6px、手柄之内**（{1284.63f + 6f:F1},{cyF:F1}）按下 ⇒ 开始拖"
                                + $"（手柄中心 {hcx2:F2} ± 半宽 15.93 ⇒ 这一段直到 1302.36，比轨道右沿 1284.63 探出 17.73）"
                                + "；改坏法：命中带退回「**只有轨道**」那一块 ⇒ 这一点落在带外 ⇒ 红");
                        // ② **正例（纵向）**：手柄比轨道**高** —— 点在手柄上、却在轨道那条横带**之外**也该算命中
                        //    （`HitBand` 的纵向半高 = `max(轨道半高 6.5, 手柄半高 17.703)`）
                        win.SetFpsIndex(1, false);
                        win.FpsPressAtCanvas(hcx1, cyF + 10f);         // 离中心线 10px：轨道半高只有 5.85
                        CheckTrue(win.FpsDragging,
                                  $"★（A202①）1 档时**手柄中心线上方 10px**（{hcx1:F1},{cyF + 10f:F1}）按下 ⇒ 也认"
                                + "（轨道半高只有 5.85 画布 px、手柄半高 15.93 ⇒ 命中带取后者）"
                                + "；改坏法：纵向半高写成**轨道那一档**（= 漏掉 `HitBand` 的 `max(…, 手柄半高)`）⇒ 红"
                                + "（⚠️ 旧的 `±16` 也会认这一点 ⇒ 这一条**不**分辨新旧几何，那是下面 ③ 的活）");
                        // ③ 🔴 **负例 —— 唯一能分辨「新旧两套几何」的那一点**：
                        //    这一点**既不在轨道里、也不在手柄里**（0 档手柄左缘 = 853.37 − 15.93 = **837.44**），
                        //    可它**落在旧的硬编码带里**（842.57 − 24 = **818.57**）⇒ 换回 `±(24,16)` 就会开始拖。
                        //    ⚠️ 它离两侧都有 8px 以上（新带外 9.44px / 旧带内 8.43px）—— 不是浮点边界。
                        win.SetFpsIndex(0, false);
                        win.FpsPressAtCanvas(828f, cyF);
                        CheckTrue(!win.FpsDragging,
                                  $"★（A202①）**(828.0,{cyF:F1})** —— 轨道与手柄**都不在那儿**（0 档手柄左缘 {hcx0 - 15.93f:F2}、"
                                + $"轨道左沿 842.57）⇒ **不该开始拖**"
                                + "（上一条刚把 `FpsDragging` 置成真 ⇒ 这一条不是「一直为假」）"
                                + "；🔴 **改坏法：把命中带换回硬编码 `±(24, 16)`** ⇒ 旧带左端 818.57 会认下这一点 ⇒ 红"
                                + "（**这是唯一能分辨新旧两套几何的探针**）");
                        // ④ **负例（真的在外面）**：带不能铺成「来者都认」
                        win.SetFpsIndex(2, false);
                        win.FpsPressAtCanvas(1284.63f + 40f, cyF);
                        CheckTrue(!win.FpsDragging,
                                  $"★（A202①）轨道右沿**之外 40px**（{1284.63f + 40f:F1},{cyF:F1}）⇒ 也不该开始拖"
                                + "（新带右端 1302.36、旧带右端 1308.63 —— 两边都在外）"
                                + "；改坏法：命中带铺成一大块（例如 `±(60,40)`）⇒ 红");
                        win.SetFpsIndex(i0c, false);                   // 还原档位（不 fire）
                        Check(Application.targetFrameRate, f0c,
                              "（A202① 收尾）探针只碰「开始拖」那件事 —— 进程帧率一个字节没动（四条探针都不改档位）");
                    }
                }
            }

            // ---------------- 🆕 A165（一）：图像页第 0 行那颗「Small Screen UI」开关 ----------------
            Section("A165：图像页**第 0 行** = Small Screen UI 开关（原版 `GraphicsTab.smallScreenToggle`）");
            var ssNode = FindChild(FindChild(root, "Graphics Tab"), "Small Screen UI");
            CheckTrue(ssNode != null, "第 0 行的开关建出来了（节点名 `Small Screen UI`）");
            if (ssNode != null)
            {
                // ① **落在原版第 0 行那个矩形里** —— 期望值是**原版 dump 的绝对字面量**：
                //    `Scroll View > Viewport > Content`(VLG) 第 0 行 `Small Screen Size Toggle` 相对 `Graphics Tab`
                //    左上角 = [51.3,278.5]–[461.0,346.5]、其子件 `Toggle` = [51.3,278.5]–[158.4,346.5]（107.10×68.08）；
                //    `Graphics Tab` 的绝对左上 = (551.87,164.79) ⇒ **Toggle 绝对 [603.17,443.29]–[710.27,511.29]**。
                //    出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Graphics Tab" --depth 6 --relative`。
                //    ⛔ 不过 `Screen()`、⛔ 不引用 `SettingsWindow.ChkL/ChkT…`（那是被测实参 —— 同式自证）。
                //    🔴 **怎么改坏就红**：把这颗开关换到别的行（例如跟 `VSync` 一样给 2）⇒ y 差 161.28px ⇒ 红。
                CheckRectPx(FindChild(ssNode, "Toggle"), 603.17f, 443.29f, 710.27f, 511.29f,
                            "（A165）第 0 行那个方框 = 原版 `Small Screen Size Toggle > Toggle` 的矩形");
                // ② 点它 ⇒ ①原版 `smallScreenUI` 那一半翻转 ②`smallUIChosenManually` 那一半也置 1 ③方框换图
                //    （判据 = `GraphicsTab__SmallScreenToggleClick.c`：**两颗一起写**；三者一起断，少一样都能假绿）
                var ssBox = FindChild(ssNode, "Toggle").GetComponentInChildren<ImageQuad>();
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOff,
                          "（前提）出厂这一格画的是**关**的图（" + SettingsWindow.ArtToggleOff + "）");
                Click(ssNode);
                CheckTrue(SmallScreenUI.Enabled, "★ 点一下 ⇒ 原版 `GameStaticData.smallScreenUI` 那一半**开了**");
                CheckTrue(SmallScreenUI.ChosenManually,
                          "★ 点一下 ⇒ 原版 `smallUIChosenManually` 那一半**也置了**（只写一个 = 跟原版不一样；改坏就红）");
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOn,
                          "★ …而且方框**换成了开的图**（" + SettingsWindow.ArtToggleOn + "）");
                CheckTrue(win.Flash != null && win.Flash.Contains("Small Screen UI"),
                          "★ 点完**有话说**（「" + win.Flash + "」）");
                Click(ssNode);
                CheckTrue(!SmallScreenUI.Enabled, "★ 再点一下 ⇒ 又关回来（两态都翻得动）");
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOff,
                          "★ …方框也换回**关**的图");
            }

            // ---------------- 🆕 A172：图像页**第 1 行** = Auto Zoom（原版 `GameStaticData.useCombatAutoZoom` +0x125）----------------
            // 判据（2026-10-07 实读；**2026-10-12 订正了两处字段名**，见下）：
            //  · `GraphicsTab__AutoZoomClick.c`：**同时**写 `+0x125` 与 `+0x12f` —— 与旁边那颗
            //    `SmallScreenToggleClick` 同一形状。🔴 **字段名订正**（原来这里写的是 `GameStaticData.autoZoom` /
            //    `autoZoomChosenManually`，**那两个名字在原版 `GameStaticData` 里不存在** —— 该类 341 个字段逐条核过）：
            //    `dump.cs` 的 `GameStaticData` 把 **+0x125 落在 `useCombatAutoZoom`**、**+0x12f 落在
            //    `autoCombatChosenManually`**（对照：`SmallScreenToggleClick` 写的是 +0x11c `smallScreenUI` /
            //    +0x12e `smallUIChosenManually`，逐条吻合）。⇒ 名字改对了，**偏移与结论一字未动**。
            //  · 有消费者：`BattleSettingsWindow__OnAutoZoomChanged.c` 先写同一个字段、再
            //    `CombatAutoZoom.ResetCameraZoomUIAction()` ⇒ 原版 = **战斗相机的自动缩放**
            //    （🔴 **2026-10-12 订正（A175）**：原来这里写「我们**没做** `CombatAutoZoom` ⇒ 这一格目前不产生效果」
            //    —— ✅ 那个消费者**已经做出来了**：`Battle/CombatAutoZoom.cs`（挂在 `BattleDriver` 上），
            //    它每次场上人数变化都现读 `AutoZoom.Enabled` ⇒ 关着 = 不缩放、开着 = 按原版 `unitsZoomCurve` 缩放；
            //    菜单那颗开关还照 `BattleSettingsWindow` 那一跳当场 `ForceRefresh()` 一次。
            //    断言 = `BattleScene.Run` 的 A175 那一节（`Editor/BattleScene.cs`），不在本文件）；
            //  · 出厂默认 = **关**（`GameStaticData__.cctor` 里没写 +0x125 ⇒ 零初始化；对照同一段里明写了
            //    `+0x11c = 0` / `+0x127 = 1` / `+0x128 = 2` / `+0x120 = 3`）；
            //  · 节点的 `Label` 文案 = 原版 TMP 的 `m_text`，**就是英文** `'Auto zoom'`（不是西语）⇒ 照抄。
            Section("A172：图像页**第 1 行** = `Auto Zoom`（原版运行时一直在的那一行）");
            {
                var azN = FindChild(FindChild(root, "Graphics Tab"), "Auto Zoom");
                CheckTrue(azN != null, "第 1 行那颗开关建出来了（节点名 `Auto Zoom`）");
                if (azN != null)
                {
                    // ① 文字 = 原版卡面原文（这一颗原版印的就是英文）
                    CheckTrue(TextOf(azN) == "Auto zoom",
                              $"行文字 = `Auto zoom`（原版 TMP 的 `m_text`；实得「{TextOf(azN)}」）");
                    // ② 出厂 = 关（cctor 没写 +0x125）
                    var azBox = FindChild(azN, "Toggle") != null
                              ? FindChild(azN, "Toggle").GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(azBox != null && azBox.Texture != null && azBox.Texture.name == SettingsWindow.ArtToggleOff,
                              "（前提）出厂这一格画的是**关**的图（" + SettingsWindow.ArtToggleOff + " —— cctor 没写 +0x125）");
                    // ③ 点它 ⇒ 两半一起写 + 换图 + **有话说**
                    Click(azN);
                    CheckTrue(AutoZoom.Enabled, "★ 点一下 ⇒ 原版 `GameStaticData.useCombatAutoZoom` 那一半**开了**");
                    CheckTrue(AutoZoom.ChosenManually,
                              "★ 同一下 ⇒ 原版 `autoCombatChosenManually`(+0x12f) 那一半**也置了**（只写一个 = 跟原版不一样 ⇒ 红）");
                    CheckTrue(azBox != null && azBox.Texture != null && azBox.Texture.name == SettingsWindow.ArtToggleOn,
                              "★ …而且方框**换成了开的图**（" + SettingsWindow.ArtToggleOn + "）");
                    CheckTrue(win.Flash != null && win.Flash.Contains("Auto Zoom"),
                              "★ 点完**有话说**（「" + win.Flash + "」）");
                    Click(azN);
                    CheckTrue(!AutoZoom.Enabled, "★ 再点一下 ⇒ 又关回来");
                    CheckTrue(azBox != null && azBox.Texture != null && azBox.Texture.name == SettingsWindow.ArtToggleOff,
                              "★ …方框也换回**关**的图");
                }
                CheckTrue(AutoZoom.PersistOverride,
                          "🔴 自检期间 `AutoZoom.PersistOverride` 是开的（不许动玩家的真设置 —— 同 `SmallScreenUI` 那条）");
            }

            // ---------------- 🆕 A176：那一行 `Use super sampling`（显隐两态）+ **真超采样 = URP `renderScale` 1.0↔2.0** ----------------
            // 判据（2026-10-10 逐句实读 `d:/2/tools/decomp_full/`；全文 → `资料/普查产出_1009/查证V3_口径三件.md` §一）
            //  · **显隐** = `GraphicsTab__ConfigureSuperSamplingVisibility.c:27-29`：
            //    `SetActive(superSampling.gameObject, !Application.isMobilePlatform && 该档 allowSuperSampling(+0x2d))`
            //    —— ⚠️ **只看这两层，不看开关值本身**（`allowSuperSampling` 五档 = 0/0/0/0/1，只 Ultra 为真）；
            //  · **点击** = `GraphicsTab__SuperSamplingToggleClick.c:13-22`：**只**写 `GameStaticData+0x124`
            //    与存盘脏位 `+0xc0`（⛔ 不当场改分辨率、⛔ 不碰 `m_MSAA`）；
            //  · **真正生效** = `QualitySettingsManager.QualityDefinition__ChangeResolution.c:37-55`：
            //    `fVar4 = 1.0`；三层门全真 ⇒ `2.0`；然后 `UniversalRenderPipelineAsset.set_renderScale(fVar4)`；
            //    调用点 = `SettingsMenu__Close.c:18-23`（**脏位在时**）· `…__ApplyGraphicsQuality.c:66` ·
            //    `QualitySettingsManager__Initialize.c:18`（启动）；
            //  · **落点** = `Assets/Settings/PC_RPAsset.asset` 的 `m_RenderScale`（出厂 1；URP 允许 [0.1,3.0]，
            //    且 URP 原生把 >1 当真超采样）。⛔ **不是 `m_MSAA`**（我们那份 `m_MSAA: 1` 保持原样）。
            // 🔴 期望值一律是**原版字面量 1.0 / 2.0**（`.rdata` 读出来的那两个常量），⛔ **不是** `SuperSampling.RenderScaleOff/On`
            //    —— 从被测实现里读常量 = 自证（本仓红线）。
            // 🔴 我们挑的等价物（如实标注，⛔ 不冒充原版）：显隐那一层门用「当前画质档 == `PC`」当「非移动平台」的等价物
            //    （调度台 2026-10-10 裁的**案 (a)**：`PC` 档 ≡ 原版「非移动」那档）；「场景切换」那个时机本地**没有对应物**
            //    （全仓没有场景切换事件）⇒ 落地用「启动 + 关窗 + 应用画质」三处，见 `SuperSampling` 类注释 ③。
            Section("A176：图像页 **`Use super sampling`** 那一行 + 真超采样（URP `renderScale` **1.0 ↔ 2.0**）");
            {
                // 打在哪份资产上：**独立解一次**（`UrpAsset()` 走 `QualitySettings.renderPipeline` = 工程设置里
                // 当前那一档的资产），并核它确实是那份文件（不然下面的读数就量错了对象）。
                var urp = UrpAsset();
                var urpFile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                                  "Assets/Settings/PC_RPAsset.asset");
                CheckTrue(urp != null && urpFile != null && urp == urpFile,
                          "（前提）当前画质档用的 URP 资产 = `Assets/Settings/PC_RPAsset.asset`"
                        + "（工程设置里 `GraphicsSettings.m_CustomRenderPipeline` 与 `PC` 档的 `customRenderPipeline` 都指它）");
                CheckNear(urp != null ? urp.renderScale : -1f, 1f, 1e-4f,
                          "（前提）那份资产的 `m_RenderScale` 出厂 = **1**（原版常态字面量；⛔ 不是从 `SuperSampling` 里读的）");

                var gTab3 = FindChild(root, "Graphics Tab");
                var gsc3 = win.GfxRowsScroll;

                // ① **当前档不允许 ⇒ 那一行藏着**（原版 `SetActive(go, false)`）
                //    ⚠️ 它**一直在**（原版 prefab 里那颗节点就在）—— 所以断的是 `activeSelf`，不是「找不找得到」。
                ssQuality = 0;                                       // = `Mobile` 档
                win.RebuildGfxRows();
                var ssOff = FindChild(gTab3, "Use super sampling");
                CheckTrue(ssOff != null,
                          "① 那一行的节点**在**（原版 prefab 里它一直在，`GraphicsTab.superSampling`；⛔ 别改成「不建」）");
                CheckTrue(ssOff != null && !ssOff.gameObject.activeSelf,
                          "①★ 当前档**不允许** ⇒ 那一行**藏着**（= 原版 `SetActive(toggleGO, false)`；"
                        + "改坏法：写成恒显 ⇒ 红）");
                // ★ 后面两行**自己往上挪一格**（原版那层 VLG 跳过 inactive 子件）
                CheckAtS(FindChild(gTab3, "VSync"), 563.52f, 593.78f, 1018.73f, 669.42f,
                         "① …`VSync` 这时落在第 **2** 格（432.50 + 2 × 80.641 = 593.78）");
                CheckAtS(FindChild(gTab3, "FPS Limit"), 563.52f, 674.42f, 1018.73f, 779.42f,
                         "① …`FPS Limit` 这时落在第 **3** 格（674.42）");
                if (gsc3 != null)
                    CheckNearPx(gsc3.ContentX2 - gsc3.ContentX1, 312.23f,
                                "① …内容高 = **4 行** 346.923 设计 px × 0.9 = 312.23（3 × 80.641 + 105）");

                // ② **当前档允许 ⇒ 那一行在**，并且后面两行整体 +1 格（+80.641）、内容高 +80.641
                //    （同一份代码、只换了注入的档位 ⇒ ① 与 ② 合起来才**分得出两态**；只断一态是弱断言）
                ssQuality = SuperSampling.PcQualityIndex;             // = `PC` 档
                win.RebuildGfxRows();
                var ssOn = FindChild(gTab3, "Use super sampling");
                CheckTrue(ssOn != null && ssOn.gameObject.activeSelf,
                          "②★ 当前档**允许** ⇒ 那一行**在**（原版 `SetActive(toggleGO, true)`）");
                if (ssOn != null)
                    CheckAtS(ssOn, 563.52f, 593.78f, 1018.73f, 669.42f,
                             "② …它落在第 **2** 格（432.50 + 2 × 80.641 = **593.78**；"
                           + "⛔ 别与上面 `Auto Zoom` 的第 1 格 513.14 搞混）");
                CheckAtS(FindChild(gTab3, "VSync"), 563.52f, 674.42f, 1018.73f, 750.06f,
                         "② …`VSync` 被挤到第 **3** 格（**+80.641** —— 「VLG 跳过 inactive 子件」那条的等价物；"
                       + "改坏法：格位号写成常数 ⇒ 与 ① 撞在同一格 ⇒ 红）");
                CheckAtS(FindChild(gTab3, "FPS Limit"), 563.52f, 755.06f, 1018.73f, 860.06f,
                         "② …`FPS Limit` 被挤到第 **4** 格");
                if (gsc3 != null)
                {
                    CheckNearPx(gsc3.ContentX2 - gsc3.ContentX1, 384.81f,
                                "② …内容高 = **5 行** 427.564 设计 px × 0.9 = 384.81（4 × 80.641 + 105）"
                              + "（🔴 旧文档里的 **508.205** 是「5 个勾选行 + FPS」= **6 行**，运行时没有那一档 —— "
                              + "见 `SettingsWindow.GfxContentH(bool)` 那条就地订正）");
                    CheckNearPx(gsc3.MaxOffset, 0f,
                                "② …**两支都矮于视口** 521.5072 ⇒ 加了这一行照样**滚不动**（原版同）");
                }
                CheckTrue(ssOn != null && TextOf(ssOn) == "Use super sampling",
                          $"② …行文字 = `Use super sampling`（实得「{TextOf(ssOn)}」；"
                        + "原版 TMP 印的是西语 `Sobremuestreo` ⇒ 英文是**我们挑的**，同文件头 ② 那条口径）");

                // ③ 点它 ⇒ **只写 flag**（原版 `SuperSamplingToggleClick`：写 +0x124 + 置脏），**不当场改分辨率**
                var ssBox = ssOn != null && FindChild(ssOn, "Toggle") != null
                          ? FindChild(ssOn, "Toggle").GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOff,
                          "（前提）出厂这一格画的是**关**的图（原版 cctor 没写 +0x124 ⇒ 零初始化）");
                Click(ssOn);
                CheckTrue(SuperSampling.Enabled, "③ 点一下 ⇒ 原版 `GameStaticData.superSampling`(+0x124) 那一半**开了**");
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOn,
                          "③ …方框也换成了**开**的图");
                CheckTrue(win.Flash != null && win.Flash.Contains("super sampling"),
                          $"③ …点完**有话说**（「{win.Flash}」）");
                // ★★ 时机：**点一下不等于生效**（原版那一句只在关窗 / 换档 / 切场景时才跑）
                CheckNear(urp != null ? urp.renderScale : -1f, 1f, 1e-4f,
                          "③★ 点完**还没有生效**：`renderScale` 仍是 **1.0**（原版 `SuperSamplingToggleClick` 只写 flag；"
                        + "改坏法：点一下就直接写 ⇒ 红 —— 那与 `SettingsMenu__Close.c:18-23` 那条时机不一致）");

                // ④ **关窗那一刻**才真写（原版 `SettingsMenu.Close`：脏位在 ⇒ `ApplyGraphicsQuality` → `ChangeResolution`）
                win.Close();
                CheckNear(urp != null ? urp.renderScale : -1f, 2f, 1e-4f,
                          "④★★ **关窗的时候**写进去了：URP `renderScale` = **2.0**（原版字面量；URP 把 >1 当真超采样）");

                // ⑤ 再开窗 → 关掉它 → 再关窗 ⇒ 回到 1.0（**两态都写得动**，只验一态是弱断言）
                win.Manager.OpenWindow(win);                     // = `WindowsManager.OpenWindow`（本工程唯一的开窗入口）
                var gTab4 = FindChild(root, "Graphics Tab");
                var ssOn2 = FindChild(gTab4, "Use super sampling");
                CheckTrue(ssOn2 != null && ssOn2.gameObject.activeSelf, "⑤ 重开窗 ⇒ 那一行还在（档位没变）");
                if (ssOn2 != null) Click(ssOn2);
                CheckTrue(!SuperSampling.Enabled, "⑤ 再点一下 ⇒ 那一半**关回去**");
                win.Close();
                CheckNear(urp != null ? urp.renderScale : -1f, 1f, 1e-4f,
                          "⑤★ 再关窗 ⇒ `renderScale` 回到 **1.0**（两个方向都真的写到那份资产上了）");

                // ⑥ = 原版 `ApplyGraphicsQuality.c:73-78`：应用到一个**不允许**超采样的档 ⇒ 把开关**强制清 0**
                //    （走**产品那条路**：点画质行 ⇒ `CycleQuality` → `ApplyQuality` → `SuperSampling.Apply()`）
                win.Manager.OpenWindow(win);                     // ⑤ 关掉了 ⇒ 重开（`Click` 找的是 active 子树里的按钮）
                SuperSampling.Set(true);
                CheckTrue(SuperSampling.Enabled, "（前提）先把开关打开，再看换档会发生什么");
                ssQuality = 0;                                   // 换到不允许的那一档（注入，不真改工程设置）
                int qlBefore = QualitySettings.GetQualityLevel();
                SettingsWindow.QualitySetterOverride = lv => { };   // 挡住真 `SetQualityLevel`（会把 `QualitySettings.asset` 写脏）
                Click(FindChild(root, "Graphics Tab"), "QualityHit");
                SettingsWindow.QualitySetterOverride = null;
                CheckTrue(!SuperSampling.Enabled,
                          "⑥★ 换到**不允许**超采样的档 ⇒ 那颗开关被**强制清 0**"
                        + "（原版 `ApplyGraphicsQuality.c:73-78` 那一句；改坏法：不写这段 ⇒ 开关还留着 ⇒ 红）");
                var ssAfter = FindChild(FindChild(root, "Graphics Tab"), "Use super sampling");
                CheckTrue(ssAfter != null && !ssAfter.gameObject.activeSelf,
                          "⑥ …而且那一行**当场藏起来**（原版 `GraphicsQualityDropdownChange.c:58` 尾部就调 "
                        + "`ConfigureSuperSamplingVisibility`；改坏法：换档不重建 ⇒ 那一行还挂着 ⇒ 红）");
                CheckNear(QualitySettings.GetQualityLevel(), qlBefore, 0.01f,
                          "⑥ …🔴 自检**没有真去切工程的画质档**（注入点挡住了 `SetQualityLevel`，同上面那条）");

                // 收尾：把注入的档位与那份资产放回原样（`finally` 里还会再兜一次）
                ssQuality = 0;
                win.RebuildGfxRows();
                RestoreRenderScale();
                CheckNear(urp != null ? urp.renderScale : -1f, 1f, 1e-4f,
                          "（收尾）那份 URP 资产放回出厂值 **1.0**（并清脏位 —— 自检不许把工程资产留在改过的状态）");
                CheckTrue(!SuperSampling.Enabled && !SuperSampling.Dirty,
                          "（收尾）开关回到出厂值（关）+ 脏位清掉");
            }

            // ---------------- 🆕 A165（二）：缩放器本体（原版 `TransformScalerBySmallScreenUI`）----------------
            // 判据全文 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头（反编译 + 解包实读）。等价的算法：
            //   `GameWindow.Open()`：开关开 **且** `extra != 1` ⇒ `(GetComponent ?? AddComponent).SetScale(extra)`
            //   组件：`enabled = (menuScale != 1) && 开关`；`LateUpdate` 里 `localScale ×= menuScale`，
            //         且「与**上次自己设过的值**逐分量差的平方和 < 0.0001」时**跳过**（防重复乘）。
            // ⇒ **倍数 M = `extra != 1` ? `extra` : **烤在 prefab 里的 `menuScale`**；只有「开关开 且 M != 1」才乘。
            // ⚠️ 批处理**没有帧循环** ⇒ 自检直调 `Tick()`（= `LateUpdate` 的同一段，同族先例：粒子要手动 `Simulate`）。
            Section("A165：小屏缩放器 —— 四条（开关关 / extra 覆盖 / 烤 menuScale / 开关关+烤值）");
            {
                // ① 开关**关** + `extra = 1.2` ⇒ 连组件都不挂（原版第一层判据：与窗口宽/屏宽**无关**）
                SmallScreenUI.Set(false);
                var p1 = new GameObject("probe-1 (flag off, extra 1.2)");
                var w1 = p1.AddComponent<GameWindow>();
                w1.extraScaleSmallScreen = 1.2f;
                w1.TryOpen(null);
                CheckTrue(w1.GetComponent<TransformScalerBySmallScreenUI>() == null,
                          "① 开关**关**着 ⇒ **连缩放器都不挂**（照原版 `Open()` 的第一层判据）");
                CheckNear(w1.transform.localScale.x, 1f, 1e-4f, "① …而且窗口根 `localScale` 停在 1");

                // ② 开关开 + `extra = 1.2` ⇒ 乘 1.2；**再 `Tick` 一次不重复乘**（守卫）
                SmallScreenUI.Set(true);
                var p2 = new GameObject("probe-2 (flag on, extra 1.2)");
                var w2 = p2.AddComponent<GameWindow>();
                w2.extraScaleSmallScreen = 1.2f;
                w2.TryOpen(null);
                var s2 = w2.GetComponent<TransformScalerBySmallScreenUI>();
                CheckTrue(s2 != null, "② 开关开 + `extra != 1` ⇒ 挂上缩放器（原版 `GetComponent ?? AddComponent`）");
                CheckNear(s2 != null ? s2.menuScale : -1f, 1.2f, 1e-4f,
                          "② …`menuScale` = `extraScaleSmallScreen`（那个窗自己的值，1.2）");
                CheckTrue(s2 != null && s2.enabled, "② …`enabled`（= `menuScale != 1` **且** 开关开）");
                if (s2 != null) s2.Tick();
                CheckNear(w2.transform.localScale.x, 1.2f, 1e-4f,
                          "② `Tick` 一次 ⇒ **窗口根** `localScale` = 1.2（乘在整扇窗的根上，不是内层容器）");
                if (s2 != null) s2.Tick();
                CheckNear(w2.transform.localScale.x, 1.2f, 1e-4f,
                          "② **再 `Tick` 一次不重复乘**（守卫：与上次设过的值差 < 0.0001 ⇒ 跳过；"
                        + "改坏法：删掉那句守卫 ⇒ 变 1.44 ⇒ 红）");

                // ③ 开关开 + `extra = 1.0`（= **不覆盖**）+ 烤 `menuScale = 1.35` —— **`TrophyInfoPopup` 那一档**
                //    （原版窗口根上带成品的只有 3 扇：`Alliance Trophy Info Popup` / `Member Options Panel` / `Generic Options Panel`）
                var p3 = new GameObject("probe-3 (flag on, extra 1.0, baked 1.35)");
                var w3 = p3.AddComponent<GameWindow>();
                w3.extraScaleSmallScreen = 1f;
                var baked3 = p3.AddComponent<TransformScalerBySmallScreenUI>();
                baked3.menuScale = 1.35f; baked3.Initialize();      // = prefab 里烤着的那颗（`m_Enabled: 1`）
                w3.TryOpen(null);
                var s3 = p3.GetComponent<TransformScalerBySmallScreenUI>();
                CheckNear(s3 != null ? s3.menuScale : -1f, 1.35f, 1e-4f,
                          "③★ `extra = 1.0` 的含义是**「不覆盖」** ⇒ `menuScale` 仍是烤着的 **1.35**"
                        + "（改坏法：无条件 `SetScale(extra)` ⇒ 变 1.0 ⇒ 红）");
                if (s3 != null) s3.Tick();
                CheckNear(w3.transform.localScale.x, 1.35f, 1e-4f,
                          "③★ `Tick` ⇒ 窗口根乘 **1.35**（**只读 `extraScaleSmallScreen` 的实现在这一档停在 1.0**）");

                // ④ 开关关 + 烤 1.35 ⇒ `enabled == false`（Unity 那一刻**根本不会调** `LateUpdate`）
                SmallScreenUI.Set(false);
                var p4 = new GameObject("probe-4 (flag off, baked 1.35)");
                var w4 = p4.AddComponent<GameWindow>();
                w4.extraScaleSmallScreen = 1f;
                var baked4 = p4.AddComponent<TransformScalerBySmallScreenUI>();
                baked4.menuScale = 1.35f; baked4.Initialize();
                w4.TryOpen(null);
                CheckTrue(!baked4.enabled,
                          "④ 开关**关**着 ⇒ `enabled == false`（原版 `Initialize`：`menuScale != 1` **且** 开关开）"
                        + "；改坏法：少判开关那一半 ⇒ 1.35 会照样乘上 ⇒ 红");
                CheckNear(w4.transform.localScale.x, 1f, 1e-4f, "④ …`localScale` 停在 1");

                Object.DestroyImmediate(p1); Object.DestroyImmediate(p2);
                Object.DestroyImmediate(p3); Object.DestroyImmediate(p4);

                // ⑤ **前提**（原版没有这一步，但我们这套渲染的语义必须钉住）：**根一缩放，子件【渲出来】也跟着缩**。
                //    判据 = **渲染真值** `MeshRenderer.bounds`（世界空间 ⇒ 父链的缩放已经算进去了）——
                //    ⛔ **不能拿 `ImageQuad.WorldW` 判**：它只是「传进去的那个数」，**不含父链缩放**
                //    （`Shell/SettingsWindow.cs` 的 `Screen()` 注释里那句「父节点的缩放对它不起作用」正是这么量出来的
                //     —— 本件报告 §顺手发现 ② 记了它；同族反例：`Battle/WfSlider.cs:158` 的填充条宽度就是靠 `localScale` 实现的）。
                //    🔴 **怎么改坏就红**：若哪天 `ImageQuad` 改成把网格直接建在**世界空间**（父链缩放失效），
                //      这条会红 —— 而那正是本缩放器（把倍数乘在窗口根上）**会静默失效**的那一刻。
                var scaleProbeRoot = new GameObject("scale-probe root");
                var spQuad = MenuDraw.Rect(scaleProbeRoot.transform, CardArt.Solid(),
                                           new PxRect(-100f, -50f, 100f, 50f), "scale-probe quad", 3000);
                var spMr = spQuad != null ? spQuad.GetComponent<MeshRenderer>() : null;
                float spW0 = spMr != null ? spMr.bounds.size.x * 108f : -1f;
                CheckNear(spW0, 200f, 2f, "（前提）探针 quad 未缩放时**渲出来**的宽 = 200px（`MeshRenderer.bounds`）");
                scaleProbeRoot.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                float spW1 = spMr != null ? spMr.bounds.size.x * 108f : -1f;
                CheckNear(spW1, 240f, 2f,
                          "★ 根 `localScale = 1.2` ⇒ **渲出来的宽也 ×1.2**（= 本缩放器乘在窗口根上是**有效**的）");
                Object.DestroyImmediate(scaleProbeRoot);

                // ---------------- 🆕 A228：小屏缩放 × `Label.Align*On`（**两态**：开关关 / 开关开）----------------
                //   判据全文 → `资料/普查产出_1010/V4a_壳与共用件口径.md` §Q1 · `资料/调度台_口径裁定_1011.md` §A228。
                //   机制：`Align*On` 收的实参是**设计空间**的 x（调用侧一律 `LayoutSpace.FromPixel(...)`，
                //   同 `MenuDraw.AlignLeft/Right`），而 `transform.parent.position.x` 是**已缩放**的世界 x
                //   ⇒ 位移那一项必须除父链 `lossyScale`（`Label.ParentXInDesignSpace`，A228 的选项 (a)）。
                //   🔴 **为什么必须两态**：开关**出厂是关的** ⇒ 窗根没接缩放器 ⇒ `k == 1` ⇒ 只断那一态的话
                //   这条断言**改坏了也照样绿**（本工程那条系统性毛病：弱断言分不出两种状态）。
                //   ⚠️ 期望值**不从实现里读**：窗根乘 M ⇒ 整扇窗的设计点 x **渲出来就在 `M·x`**（与同窗
                //   其它件同一条规矩，判据 = 上面第 ⑤ 条那半「根一缩放、渲出来的也跟着缩」）。
                Section("A228：小屏缩放 × `Label.Align*On`（关 = 逐值不变 / 开 = 设计点 x 渲出来在 M·x）");
                {
                    // 量的都是**渲出来的**边缘（世界单位）= 节点世界 x ± 半个**渲染**宽；渲染宽 = `WorldW` ×
                    // **父链缩放**（`WorldW` 自己只是**局部**长度、不含父链 —— 同族先例 = `Editor/RewardsScene.cs`
                    // 的 `TextLeftPx`，那边整棵树没缩放所以没乘这一下）。
                    float Left(Label l) => l.transform.position.x - l.WorldW * l.transform.lossyScale.x * 0.5f;
                    float Right(Label l) => l.transform.position.x + l.WorldW * l.transform.lossyScale.x * 0.5f;

                    float wantX = LayoutSpace.FromPixel(1400f, 0f).x;   // 实参长相与 `MenuDraw.AlignLeft` 一致
                    // 父节点摆在**离窗根 2 个设计单位**处：不除 `k` 时的偏差 = `(1−k)·nl`（nl = 父到窗根的距离）
                    // ⇒ 这个非零偏移**就是**让两态分得开的那一格（摆在 0 处两种实现都对 ⇒ 断言退化成假绿）。
                    const float parentDesignX = 2f;

                    // ---- 态一：开关**关**（出厂态）⇒ 窗根不缩放 ⇒ k == 1 ⇒ 与旧写法**逐值相同** ----
                    SmallScreenUI.Set(false);
                    var a228r1 = new GameObject("a228 probe (flag off)");
                    var a228p1 = new GameObject("a228 parent").transform;
                    a228p1.SetParent(a228r1.transform, false);
                    a228p1.localPosition = new Vector3(parentDesignX, 0f, 0f);
                    var a228l1 = Label.Create(a228p1, "A228 probe", Vector3.zero, 5, Color.white,
                                              new Vector2(0.5f, 0.5f), "a228 label");
                    CheckTrue(a228l1 != null && a228l1.CanRenderChinese,
                              "（前提）TMP 后端在 —— 走点阵兜底时 `Align*On` **首句就 return**（空操作），下面四条无从谈起");
                    if (a228l1 != null && a228l1.CanRenderChinese)
                    {
                        CheckNear(a228l1.transform.lossyScale.x, 1f, 1e-4f,
                                  "（前提）态一：父链**没有**缩放 ⇒ 下一条那句「逐值不变」才有意义");
                        a228l1.AlignLeftOn(wantX);
                        CheckNear(a228l1.transform.localPosition.x,
                                  wantX - a228p1.position.x + a228l1.WorldW * 0.5f, 1e-4f,
                                  "① 关：`localPosition` 与**旧式**（`worldX − 父世界 x + W/2`）逐值相同 —— "
                                + "这一态钉的是「开关关着时**逐值不变**」（k=1 ⇒ 新旧两式恒等）；"
                                + "改坏法：把「减父世界位置」那一项整个丢掉（= 2026-09-23 那个旧缺陷，字会飞到屏幕外）"
                                + "或把 `W/2` 的符号弄反 ⇒ 这一条红");
                        CheckNear(Left(a228l1), wantX, 0.03f,
                                  "① 关：`AlignLeftOn` 之后**渲出来**的左缘 = 传进去的那个 x");
                        // 右对齐那一条：`localPosition` 的期望 = 旧式，且**与左对齐不同**（差一个整宽）
                        a228l1.AlignRightOn(wantX);
                        CheckNear(a228l1.transform.localPosition.x,
                                  wantX - a228p1.position.x - a228l1.WorldW * 0.5f, 1e-4f,
                                  "① 关：`AlignRightOn` 走的是**自己那一式**（`− W/2`，与左对齐差一个整宽）"
                                + "；改坏法：两个方法互相抄错符号 ⇒ 红");
                        CheckNear(Right(a228l1), wantX, 0.03f,
                                  "① 关：……而且**渲出来**的右缘落在传进去的那个 x 上");
                    }
                    Object.DestroyImmediate(a228r1);

                    // ---- 态二：开关**开** + `extra = 1.2` ⇒ 窗根乘 1.2，父节点的世界 x = 1.2 × 设计值 ----
                    SmallScreenUI.Set(true);
                    var a228r2 = new GameObject("a228 probe (flag on)");
                    var a228w2 = a228r2.AddComponent<GameWindow>();
                    a228w2.extraScaleSmallScreen = 1.2f;
                    a228w2.TryOpen(null);                                 // = 生产那条路（挂缩放器 + SetScale）
                    var a228s2 = a228r2.GetComponent<TransformScalerBySmallScreenUI>();
                    if (a228s2 != null) a228s2.Tick();                    // 批处理没有帧循环 ⇒ 手动推一次
                    CheckNear(a228r2.transform.localScale.x, 1.2f, 1e-4f,
                              "（前提）态二：开关开 + `extraScaleSmallScreen = 1.2` ⇒ 窗根 `localScale` = 1.2");
                    // 🔴 **落地前必须核的那一格**（调度台裁定 §A228）：选项 (a) **只在「窗根在世界原点」时精确** ——
                    //    根一旦有偏移 `Rx`，正确式是 `worldX − (P − Rx)/k`，而 `Align*On` 拿不到 `Rx`。
                    //    生产侧同档（现读）：`ShellRuntime` 的根 = `new GameObject("Shell")`（**无父** ⇒ 原点），
                    //    三个 `WindowHolder` 也是 `SetParent(root, false)`、`AttachToAnchor` 还把窗根归到 `localPosition = 0`
                    //    ⇒ 窗口根就停在原点。**这条前提要是哪天红了，就该改选 (c)**（改成 `parent.InverseTransformPoint`）。
                    CheckNear(a228r2.transform.position.x, 0f, 1e-4f,
                              "（前提）「窗根」在世界原点 —— A228 选项 (a) 的适用范围（不在原点 ⇒ 改选 (c)，停下来报）");
                    var a228p2 = new GameObject("a228 parent").transform;
                    a228p2.SetParent(a228r2.transform, false);
                    a228p2.localPosition = new Vector3(parentDesignX, 0f, 0f);
                    CheckNear(a228p2.position.x, 1.2f * parentDesignX, 1e-3f,
                              "（前提）父节点的**世界** x = 设计值 × 1.2（**≠** 设计值 ⇒ 两态的量纲差真的存在，"
                            + "后面两条不是在断一个恒等式）");
                    var a228l2 = Label.Create(a228p2, "A228 probe", Vector3.zero, 5, Color.white,
                                              new Vector2(0.5f, 0.5f), "a228 label");
                    if (a228l2 != null && a228l2.CanRenderChinese)
                    {
                        a228l2.AlignLeftOn(wantX);
                        CheckNear(Left(a228l2), 1.2f * wantX, 0.03f,
                                  "★② 开：`AlignLeftOn` 之后**渲出来**的左缘 = **设计 x × 1.2**（整扇窗一起缩的那一档）。"
                                + "改坏法：去掉 `ParentXInDesignSpace` 里那个 `/k` ⇒ 偏 `(1−1.2)×2` 世界单位"
                                + "= **−0.4（= −43.2px）** ⇒ 红 —— 这正是 A228 报的那条潜伏缺陷");
                        a228l2.AlignRightOn(wantX);
                        CheckNear(Right(a228l2), 1.2f * wantX, 0.03f,
                                  "★② 开：`AlignRightOn` 同理（右缘 = 设计 x × 1.2）—— 与上一条合起来才盖住两个方法"
                                + "（它们各自算自己那一式，改一个漏一个时只有这一条红）");
                    }
                    Object.DestroyImmediate(a228r2);

                    SmallScreenUI.Set(false);       // 放回出厂态（下面「收尾」那一句还会再兜一次）
                }

                // 🔴 **收尾**：开关放回**出厂值**（原版 cctor = 0）—— 下面那几十条断言与最后那张截图
                //    都该在「原版出厂态」下跑（`PersistOverride` 仍是 true ⇒ 玩家的真设置一个字节都没动）。
                SmallScreenUI.Set(false);
                CheckTrue(!SmallScreenUI.Enabled, "（收尾）自检跑完把开关放回**出厂值 关**");
            }

            // ---------------- 🆕 A167：命中区 / 滚动视口要吃【父链缩放】（两态：关 = 设计矩形 / 开 = 设计矩形 × M） ----------------
            //   病灶 / 判据 / 「同族三处」的逐条结论 → **`Shell/PointerLayer.cs` 的 `HitBoxPx` 那一段注释**
            //   （那里是唯一一份正本，⛔ 这里不抄第二遍）。
            //   🔴 **为什么必须两态**：开关**出厂是关的**（原版 `GameStaticData` cctor = 0）⇒ `M == 1`
            //   ⇒ 只断那一态的话「尺寸项不乘 `lossyScale`」照样绿 —— A167 记的就是这种**潜伏**形状。
            //   ⚠️ 期望值**独立算一遍**（⛔ 不从 `PointerLayer` 读任何数）：窗根乘 M ⇒ 设计点 d 渲出来在
            //   `M·d` ⇒ 它的画布 px = `c + (ToPixel(d) − c)·M`（`c` = 画布中心）—— 与
            //   `Shell/SettingsWindow.cs` 的 `Screen()` 把固定 0.9 烘进矩形**同一个形状**。
            Section("A167：命中区 / 滚动视口吃父链缩放（关 = 设计矩形 / 开 = 设计矩形 × 1.2）");
            {
                // 探针矩形**离画布中心够远**：乘 M 时每条边外移「半宽 × (M−1)」——
                // 摆在中心附近时两态几乎重合 ⇒ 断言退化成假绿（同 A297 / A228 那两条的前提）。
                // 判别带宽度 = 半宽 × (M−1)（本例 x 60px / y 50px）⇒ 判别点取在带的正中。
                var a167R = new PxRect(500f, 300f, 1300f, 800f);
                const float a167M = 1.2f;
                float cX = LayoutSpace.DesignPxW * 0.5f, cY = LayoutSpace.DesignPxH * 0.5f;
                float Sx(float v) { return cX + (v - cX) * a167M; }
                float Sy(float v) { return cY + (v - cY) * a167M; }
                float a167cx = (a167R.x1 + a167R.x2) * 0.5f, a167cy = (a167R.y1 + a167R.y2) * 0.5f;
                float a167Cx = Sx(a167cx), a167Cy = Sy(a167cy);              // 态二：设计中心 → 世界 px
                float a167HW = (a167R.x2 - a167R.x1) * 0.5f, a167HH = (a167R.y2 - a167R.y1) * 0.5f;
                // 🔴 判别带 = 「`设计半宽`（改坏后的边）」与「`设计半宽 × M`（修好后的边）」之间那一条
                //    （宽 = `半宽 × (M−1)`，本例 x 80px / y 50px）—— 判别点取在**带的正中**。
                float a167XL = a167Cx - a167HW * (1f + (a167M - 1f) * 0.5f);   // 左
                float a167XR = a167Cx + a167HW * (1f + (a167M - 1f) * 0.5f);   // 右
                float a167YT = a167Cy - a167HH * (1f + (a167M - 1f) * 0.5f);   // 上
                float a167YB = a167Cy + a167HH * (1f + (a167M - 1f) * 0.5f);   // 下
                float a167XNeg = a167Cx - a167HW * 1.4f;                       // 负控制：连放大后的左沿都够不到
                CheckTrue(a167XL < a167Cx - a167HW && a167XL > a167Cx - a167HW * a167M,
                          "（前提）判别点落在两态各自的左沿**之间**（带的正中）—— 不在带里 ⇒ 两态同结论"
                        + " ⇒ 下面那几条会退化成「什么都中 / 什么都不中」的假绿");

                var a167pl = PointerLayer.Instance;
                WindowButton a167btn1 = null, a167btn2 = null;
                MenuScroll a167sc1 = null, a167sc2 = null;

                // ---- 态一：开关**关**（出厂态）⇒ 窗根不缩放 ⇒ k == 1 ⇒ 与改前**逐值相同** ----
                SmallScreenUI.Set(false);
                var a167r1 = new GameObject("a167 probe (flag off)");
                var a167w1 = a167r1.AddComponent<GameWindow>();
                a167w1.extraScaleSmallScreen = a167M;
                a167w1.TryOpen(null);
                CheckNear(a167r1.transform.localScale.x, 1f, 1e-4f,
                          "（前提）态一：开关**关**着 ⇒ 窗根 `localScale` 停在 1（连缩放器都不挂）");
                {
                    var a167h1 = MenuDraw.Hit(a167r1.transform, "A167 Hit", a167R, 9000, null);
                    a167btn1 = a167h1 != null ? a167h1.GetComponent<WindowButton>() : null;
                    CheckTrue(a167btn1 != null,
                              "（前提）态一：命中探针建起来了（`MenuDraw.Hit` —— `PointerLayer` 只认它那颗 `ImageQuad`）");
                    // 态一：命中区 = **设计矩形**（这一态钉「开关关着时逐值不变」）
                    CheckTrue(a167pl.ButtonAt(a167cx, a167cy) == a167btn1,
                              "① 关：设计矩形中心命中 —— 这一态钉的是「`k == 1` 时与改前**逐值相同**」");
                    CheckTrue(a167pl.ButtonAt(a167R.x1 + 5f, a167cy) == a167btn1, "① 关：设计矩形左沿内 5px 命中");
                    CheckTrue(a167pl.ButtonAt(a167R.x1 - 5f, a167cy) != a167btn1,
                              "① 关：设计矩形左沿外 5px **不**命中（尺寸项在这一态就是设计矩形，⛔ 不是 ×1.2）");
                    CheckTrue(a167pl.ButtonAt(a167XL, a167Cy) != a167btn1,
                              "★（相对那一断）**同一个点**在态一**漏** —— 它到态二会变成「中」（下面那条），"
                            + "两态合起来才证明「命中区真的跟着窗根缩放走」（⛔ 这条不读我们自己的常量）");

                    a167sc1 = MenuScroll.TopAligned(a167R, 400f);
                    a167sc1.Owner = a167r1;                       // 缩放取自 `Owner` 的父链
                    PointerLayer.RegisterScroll(a167sc1);
                    CheckTrue(a167pl.ScrollUnder(a167cx, a167cy) == a167sc1, "① 关：滚动视口（设计矩形）中心命中");
                    CheckTrue(a167pl.ScrollUnder(a167XL, a167Cy) != a167sc1,
                              "★（相对那一断）同一颗判别点**也漏**在滚动视口上（态一）");
                }
                PointerLayer.UnregisterOwnedBy(a167r1);            // 登记表里的条目要撤（`Owner` 是这个根）
                Object.DestroyImmediate(a167r1);

                // ---- 态二：开关**开** + `extra = 1.2` ⇒ 窗根乘 1.2（= 生产那条路） ----
                SmallScreenUI.Set(true);
                var a167r2 = new GameObject("a167 probe (flag on)");
                var a167w2 = a167r2.AddComponent<GameWindow>();
                a167w2.extraScaleSmallScreen = a167M;
                a167w2.TryOpen(null);                             // 挂缩放器 + `SetScale`
                var a167s2 = a167r2.GetComponent<TransformScalerBySmallScreenUI>();
                if (a167s2 != null) a167s2.Tick();                // 批处理没有帧循环 ⇒ 手动推一次
                CheckNear(a167r2.transform.localScale.x, a167M, 1e-4f,
                          "（前提）态二：开关开 + `extraScaleSmallScreen = 1.2` ⇒ 窗根 `localScale` = 1.2");
                CheckNear(a167r2.transform.position.x, 0f, 1e-4f,
                          "（前提）态二：探针窗根在世界原点（= `DesignToPtrPx` 那条仿射的适用范围）");
                {
                    var a167h2 = MenuDraw.Hit(a167r2.transform, "A167 Hit", a167R, 9000, null);
                    a167btn2 = a167h2 != null ? a167h2.GetComponent<WindowButton>() : null;
                    CheckTrue(a167btn2 != null, "（前提）态二：命中探针建起来了");
                    CheckTrue(a167pl.ButtonAt(a167Cx, a167Cy) == a167btn2, "★② 开：设计中心 ×1.2 处命中");
                    CheckTrue(a167pl.ButtonAt(a167XL, a167Cy) == a167btn2,
                              "★② 开：**左沿外 40px 也命中** —— 命中区 = **设计矩形 × 1.2**（渲出来那一块）。"
                            + "**改坏法**：`HitBoxPx` 的 `half` 去掉 `ScaleAbs(ls.x/y)` 这两个因子 ⇒ 命中区"
                            + "只剩 `设计矩形 × 1` ⇒ 这一点落在区外 ⇒ **这一条立刻红**"
                            + "（四条边各一条；只改 x 不改 y 时只有左/右这两条红）");
                    CheckTrue(a167pl.ButtonAt(a167XR, a167Cy) == a167btn2, "★② 开：右沿外 40px 同理");
                    CheckTrue(a167pl.ButtonAt(a167Cx, a167YT) == a167btn2, "★② 开：上沿外 25px 同理（y 分量）");
                    CheckTrue(a167pl.ButtonAt(a167Cx, a167YB) == a167btn2, "★② 开：下沿外 25px 同理（y 分量）");
                    CheckTrue(a167pl.ButtonAt(a167XNeg, a167Cy) != a167btn2,
                              "★② 开：**负控制**（连放大后的左沿都够不到）**不**命中 —— 没有它，上面那四条"
                            + "「什么都中」的实现在这里照样全绿");

                    a167sc2 = MenuScroll.TopAligned(a167R, 400f);
                    a167sc2.Owner = a167r2;
                    PointerLayer.RegisterScroll(a167sc2);
                    CheckTrue(a167pl.ScrollUnder(a167Cx, a167Cy) == a167sc2, "★② 开：滚动视口中心（设计中心 ×1.2）命中");
                    CheckTrue(a167pl.ScrollUnder(a167XL, a167Cy) == a167sc2,
                              "★② 开：**判别点也落在滚动视口里** —— 视口同样按 ×1.2 换算。"
                            + "**改坏法**：`HitScroll` 直接比 `s.Viewport`（不换算）⇒ 这一点在视口外 ⇒ 红");
                    CheckTrue(a167pl.ScrollUnder(a167XNeg, a167Cy) != a167sc2,
                              "★② 开：滚动视口的**负控制**不命中（同上一条的理由）");
                }
                PointerLayer.UnregisterOwnedBy(a167r2);
                Object.DestroyImmediate(a167r2);

                SmallScreenUI.Set(false);       // 放回出厂态（下面还有几十条断言与截图）
                CheckTrue(!SmallScreenUI.Enabled, "（收尾）A167：自检跑完把开关放回**出厂值 关**");
            }

            // ---------------- 🆕 A165（三）：`TrophyInfoPopup` 那一扇**烤着的 1.35** 真接上了 ----------------
            // 原版那颗组件挂在**窗体根 GO** 上（`MonoBehaviour_-3185861090812863363.json`：`m_Enabled: 1`、
            // `menuScale = 1.350000023841858`）—— 我们代码建窗 ⇒ 必须在 `Open()` 里补挂，⛔ 不能只在注释里写「1.35」。
            Section("A165：`TrophyInfoPopup` 的烤值 1.35（原版那 3 扇中的一扇）真挂上了吗");
            {
                var tro = TrophyInfoPopup.Open(null);       // mgr=null ⇒ 只建窗、不进窗口管理器（会在日志里如实出声）
                var troSc = tro != null ? tro.GetComponent<TransformScalerBySmallScreenUI>() : null;
                CheckTrue(troSc != null,
                          "★ `TrophyInfoPopup` 的窗口根上**有** `TransformScalerBySmallScreenUI`"
                        + "（改坏法：把 `Open()` 里那两行删掉 ⇒ 红；小屏下它会**静默不放大**）");
                CheckNear(troSc != null ? troSc.menuScale : -1f, 1.35f, 1e-4f,
                          "★ 烤的倍数 = **1.350000023841858**（原版 `MonoBehaviour_-3185861090812863363.json` 字面量）"
                        + "；⛔ 不是 `extraScaleSmallScreen`（那颗 MB 写的是 1.0 = 「不覆盖」）");
                if (tro != null) Object.DestroyImmediate(tro.gameObject);
            }

            // ---------------- 音频页 ----------------
            Section("音频页：三根滑块（`WfSlider` —— 工程里唯一一份滑块实现）");
            Click(Bar(root), "Audio");
            var sl = win.AudioSliders;
            CheckTrue(sl != null && sl.Length == 3 && sl[0] != null && sl[1] != null && sl[2] != null,
                      "三根滑块都建出来了（Music / Sound Effects / Voice-overs）");
            // 🆕 A125②：行名数组**提上来一份**（原来只在「行顶」那一段里有）—— 轨道高那一段
            //    也要按行名去 `FindChild` 拿**那一根滑块**的 `Fill` 层（见下面那两条断言）。
            var auNames = new[] { "Music", "Sound Effects", "Voice-overs" };
            if (sl != null && sl[0] != null)
            {
                CheckTrue(sl[0].HasArt, "滑块的三张图都在（`Volume_bar_inactive` / `_active` / `Volume_button`）");
                float before = WarpforgeAudio.Music;
                sl[0].SetValue(0.42f, true);
                CheckNear(WarpforgeAudio.Music, 0.42f, 0.01f,
                          $"拖第一根 ⇒ **真的写进 AudioMixer**（Music {before:F2} → {WarpforgeAudio.Music:F2}）");
                sl[0].SetValue(before, true);
                // 轨道宽取的是**本页容器宽**（不是战斗内那根的 561.08）
                var rowN = sl[0].WorldPos;
                float left = sl[0].LeftWorld.x * 108f + 960f, right = sl[0].RightWorld.x * 108f + 960f;
                // 🔴 **2026-10-06（A131①）就地改掉第二处【自证】**：这里原来写的是
                //   `SettingsWindow.AuTrackW * SettingsWindow.RootScale` —— 而被测的那三根
                //   `WfSlider.Create(... trackW: AuTrackW * RootScale)` 传的**是同一个表达式**
                //   （与下面那条轨道高逐字同形）⇒ **把 `AuTrackW` 改成别的数，这条照样绿**。
                //   ⇒ 期望值写成**字面量 `615.77f`**。出处**本轮从头核过**（A125-1 只核了 `11.7`）：
                //     ① `684.19` = 原版 `Audio Settings` 组（RT `-1942814961158094938`）的**未缩放**宽：
                //        锚 `(0,0.5)→(0.75,0.5)` · `m_SizeDelta.x = -90`，父 `Tab Content` 宽 **1032.26**
                //        ⇒ 宽 = 0.75×1032.26 − 90 = **684.195**（⛔ 它**不是**「我们的常量」，
                //        是逐字段从原版 prefab 算出来的）。
                //     ② 三根 `… Slider` 的宽**不是**自己的 `sizeDelta`（`sd.x = 0`）—— 它锚 `(0,0.5)→(1,0.5)`
                //        ⇒ **撑满行容器**；行容器 `Music/FX/Voiceovers Container` 的 `m_SizeDelta = (0, 105/106/106)`、
                //        组上的 VLG 是 `ctrlW=1 · expandW=1 · pad=0` ⇒ **每行也撑满组宽**
                //        ⇒ **滑块宽 = 行宽 = 组宽 = 684.195**（未缩放）。
                //     ③ 过根那层 0.9 ⇒ 屏幕上 = 684.195 × 0.9 = **615.7755** ⇒ 断言写 `615.77f`。
                //     ④ 独立第二路复核：`python 工具/menu_dump.py bundle_menus_assets_all "Audio Settings"
                //        --depth 3` ⇒ 该组 / 三行 / 三根滑块的「宽」列**全是 615.77**。
                CheckNear(right - left, 615.77f, 2f,
                          "轨道宽 = 原版本页容器 684.19 × 根 0.9 = **615.77**（⛔ 别引用 `SettingsWindow` 的常量 —— "
                          + "那是被测实参；⚠️ 战斗内那根是 561.08×1.0，两者不同）");

                // ---- 🆕 A96（2026-10-05）：轨道【高】（三根逐个量**渲出来的世界高**）----
                // 判据（原始 JSON 实读，`bundle_menus_assets_all`）：三根 `… Slider`（`RectTransform_
                //   -5607048967920844890` / `3300864807740932006` / `6026471101496917926`）的
                //   `m_SizeDelta.y = **13**`、锚 `(0,0.5)→(1,0.5)` —— 那是**未缩放**的设计值；
                //   父链一直爬到根 `Main Menu Settings Window`（`RectTransform_-7066813013973172314`）
                //   `m_LocalScale = 0.9`，中间那些节点全是 1 ⇒ **屏幕上 13 × 0.9 = 11.7 画布 px**
                //   （本窗把 0.9 **烘进矩形**，见 `SettingsWindow.Screen()` 的注释）。
                // 🔴 ⛔ 期望值**不许写成裸 13**：那是把未缩放的设计值当屏幕值用，会**大 11%**
                //   （`SettingsWindow` 文件头那条坑：别直接拿原版矩形当世界坐标）。
                // 🔴 **2026-10-06（A125①）就地改掉一种【自证】写法**：这里原来写的是
                //   `SettingsWindow.AuTrackH * SettingsWindow.RootScale` —— 而被测的那三根
                //   `WfSlider.Create(... trackH: AuTrackH * RootScale)` 传的**是同一个表达式**
                //   ⇒ 期望值与被测实参同源，**常量本身错了也不会红**（把 `AuTrackH` 改成 `12f`，
                //   这条照样绿，而轨道会比原版小 10%）。
                //   ⇒ 期望值写成**字面量 `11.7f`**（= 原版 13 × 0.9，两个因子都是原版实测值），
                //   与 `Editor/BattleScene.cs` 战斗侧那条**硬编码 `12f`** 同一种写法（两边口径统一）。
                // 🔴 量的是 `TrackWorldH`（Background 那几块 quad 的**并集高**），不是把参数念一遍 ——
                //   「常量改了、`TrackRectPx` 忘了跟着 `trackH` 走」那种改法这条也会红。
                // 复核：`python 工具/menu_dump.py bundle_menus_assets_all "Audio Settings" --depth 3 --no-sprite`
                //   → 三行的「宽×高」列 = **11.70**（= 13 × 0.9）。
                for (int i = 0; i < 3; i++)
                {
                    if (sl == null || i >= sl.Length || sl[i] == null)
                    { CheckTrue(false, $"第 {i + 1} 根滑块在（量不到轨道高）"); continue; }
                    CheckNearPx(sl[i].TrackWorldH * 108f, 11.7f,
                                $"第 {i + 1} 根滑块的**轨道高** = 原版 13 × 0.9 = 11.7"
                                + "（⚠️ 战斗内那根是 12 × 1.0，两边**不是一个数**）");
                    // ---- 🆕 A218（2026-10-11）：**滑块【根节点自己的 `rect`】**（不是从 quad 量出来的那一层）----
                    //  判据 = 原版那三根 `… Slider` 的矩形：宽 = 行容器 684.195 × 根 0.9 = **615.77**、
                    //  高 = `m_SizeDelta.y = 13` × 0.9 = **11.7**（上面那两长段的逐字段推导，本轮没动）。
                    //  ⚠️ 与上面 `TrackWorldH` 那条**互补**：那条量的是**画出来的** quad，这条量的是**节点自己的
                    //     `rect`**（A218 的验收口径）—— 两条同时绿才说明「节点矩形」与「画出来的层」一致。
                    //  🔴 **父链缩放核查**：本窗那 0.9 是**烘进矩形**的（不是乘在根上，见 `SettingsWindow.Screen()`）
                    //     ⇒ 这些节点的 `lossyScale == 1`，`rect` 与画布 px 同量纲（下面现断一次）。
                    //  改坏法：删掉 `WfSlider.Create` 里那句 `MenuDraw.SetPxSize` ⇒ 这条红（`rect` 回到默认值）。
                    var sldN = FindChild(FindChild(root, auNames[i] + " Container"), "slider_vol" + i);
                    CheckTrue(sldN != null && sldN.GetComponent<RectTransform>() != null,
                              $"（前提）第 {i + 1} 根滑块的根节点 `slider_vol{i}` 在、且是 `RectTransform`（A92 那半）");
                    if (sldN != null && sldN.GetComponent<RectTransform>() != null)
                    {
                        var srt = sldN.GetComponent<RectTransform>();
                        CheckNear(sldN.lossyScale.x, 1f, 1e-3f,
                                  $"（前提·父链缩放）`slider_vol{i}` 的 `lossyScale.x` = 1（0.9 是烘进矩形的，不在链上）");
                        CheckNearPx(srt.rect.width * 108f, 615.77f,
                                    $"★ A218：第 {i + 1} 根滑块**根的 `rect` 宽** = 615.77px"
                                    + "（原版行容器 684.195 × 根 0.9；⛔ 战斗内那根是 561.08，两边不是一个数）");
                        CheckNearPx(srt.rect.height * 108f, 11.7f,
                                    $"★ A218：…`rect` 高 = 11.7px（原版 `m_SizeDelta.y` 13 × 0.9）");
                    }
                    // ---- 🆕 A125②（2026-10-06）：**`Fill` 那一层也量** ----
                    // 🔴 原来两处宿主都只量 Background（`TrackWorldH`），而 `Battle/WfSlider.cs` 里
                    //   bg 与 fill **同源于同一个 `TrackRectPx(trackW, trackH)`**（那两处 `MenuDraw.Nine`）
                    //   ⇒ **只改其中一层不会红**（审查代理报出来的欠断言）。
                    //   判据：原版那三根 `… Slider`（`bundle_menus_assets_all`）的子件
                    //   `Background` / `Fill` / `Handle` **同父、同高**（`Fill` 与 `Background` 同源于
                    //   `Handle Slide Area` 那一档高）⇒ **两层必须同高**。
                    //   量法照 Background 那一条：取该层九宫格子 quad 的**并集高**（`ImageQuad.WorldH`），
                    //   ⛔ 不是把参数念一遍；期望值写**字面量 `11.7f`**（同上，⛔ 别引用窗自己的常量）。
                    // ⚠️ 靠**层名**找节点（`slider_fill` = `WfSlider.Create` 建它时用的 `name:`）——
                    //   那一层在 `WfSlider` 上**没有对外访问器**，而 `Battle/WfSlider.cs` 不在本件白名单
                    //   ⇒ 不改它，用名字取（这也是本仓库既有的量法，见 `Editor/CollectionScene.cs` 量 `Unlock`）。
                    var fillN = FindChild(FindChild(root, auNames[i] + " Container"), "slider_fill");
                    if (fillN == null)
                        CheckTrue(false, $"第 {i + 1} 根滑块的 `Fill` 那一层在（`slider_fill`）"
                                         + " —— 不在 = 下面两条等于没验");
                    else
                    {
                        float fh = 0f;
                        var fqs = fillN.GetComponentsInChildren<ImageQuad>(true);
                        for (int k = 0; k < fqs.Length; k++)
                            if (fqs[k] != null) fh = Mathf.Max(fh, fqs[k].WorldH);
                        CheckNearPx(fh * 108f, 11.7f,
                                    $"第 {i + 1} 根滑块的 **`Fill` 层高** = 原版 13 × 0.9 = 11.7"
                                    + "（与 `Background` 同源 ⇒ 两层必须同高）");
                        CheckNear(fh * 108f, sl[i].TrackWorldH * 108f, 0.05f,
                                  $"第 {i + 1} 根滑块**两层同高**（`Fill` {fh * 108f:F3}px ≈ `Background` "
                                  + $"{sl[i].TrackWorldH * 108f:F3}px）—— 只改其中一层这条就红");
                    }
                }

                // ---------------- 🆕 A169（2026-10-07）：`WfSlider` 那四处「逐实例不同却写死」的值 ----------------
                // 判据 → `资料/待办判据_1006.md` §A169 · `资料/普查产出_1006/A168_FPS滑块.md` §顺手发现 ①②③④。
                // 🔴 **补它的原因**：上面那几条**只量几何**（宽 / 高 / 两层同高），一条都没盯**队列** ——
                //    那三根原来画在 `WfSlider` 内部硬编码的 **3000**，而本窗的压暗层 **3130** / 面板 3131 /
                //    填色 3132 / 内容 3133 全在它上面 ⇒ **整根被压暗一层**，而这里的断言全绿（= 无效断言）。
                Section("A169：音频页三根滑块的**队列 / 端帽 / 手柄**（`WfSlider` 刚被参数化）");
                {
                    // ① 队列。基准取**本窗真实的那几层**：把窗里所有 quad 扫一遍，分两档量 ——
                    //    `winChromeQ` = 压暗 3130 / 面板 3131 / 填色 3132 那三档的最高者；
                    //    `winContentQ` = **内容**那一档（3133）。
                    //    ⛔ 期望值**不许**写成 `SettingsWindow.QContent` —— 那是被测实参
                    //    （同 A125① 「别拿被测的常量当期望」那条口径）。
                    int winChromeQ = -1, winContentQ = -1;
                    var allQ = root.GetComponentsInChildren<ImageQuad>(true);
                    for (int k = 0; k < allQ.Length; k++)
                    {
                        var q = allQ[k];
                        if (q == null) continue;
                        // ⛔ **把滑块自己那棵树排除掉** —— 它们的轨道就落在 3133（正是 3000→3133 那次改正的
                        //    目标档），混进基准里就成了「拿滑块自己证明滑块自己」（基准会恒 = 从它自己身上量到的值）。
                        bool mine = false;
                        for (var t = q.transform; t != null && t != root; t = t.parent)
                            if (t.name.StartsWith("slider_")) { mine = true; break; }
                        if (mine) continue;
                        int rq = q.RenderQueue;
                        if (rq >= 3130 && rq <= 3132 && rq > winChromeQ) winChromeQ = rq;      // 压暗/面板/填色
                        if (rq == 3133 && rq > winContentQ) winContentQ = rq;                 // 内容
                    }
                    CheckTrue(winChromeQ > 0,
                              "（前提）本窗量得到压暗 / 面板 / 填色那三档（实得最高档 "
                            + $"{winChromeQ}）—— 量不到 = 下面「轨道高于它」那两条等于没验");
                    CheckTrue(winContentQ > 0,
                              $"（前提）本窗量得到**内容**档（实得 {winContentQ}）"
                            + " —— 量不到 = 下面「轨道 ≥ 内容档」那条等于没验");
                    for (int i = 0; i < 3; i++)
                    {
                        if (sl == null || i >= sl.Length || sl[i] == null)
                        { CheckTrue(false, $"第 {i + 1} 根滑块在（量不到队列/端帽/手柄）"); continue; }
                        var s = sl[i];
                        string who = $"第 {i + 1} 根滑块";
                        var rowN2 = FindChild(root, auNames[i] + " Container");
                        var bgL = FindChild(rowN2, "slider_bg");
                        var flL = FindChild(rowN2, "slider_fill");
                        var hdL = FindChild(rowN2, "slider_handle");
                        if (bgL == null || flL == null || hdL == null)
                        {
                            CheckTrue(false, $"{who} 的三层都在（`slider_bg` / `slider_fill` / `slider_handle`）"
                                             + " —— 不在 = 下面几条等于没验");
                            continue;
                        }
                        var hdq = hdL.GetComponent<ImageQuad>();
                        int qBg = -1, qFl = -1;
                        foreach (var q in bgL.GetComponentsInChildren<ImageQuad>(true)) if (q != null) qBg = Mathf.Max(qBg, q.RenderQueue);
                        foreach (var q in flL.GetComponentsInChildren<ImageQuad>(true)) if (q != null) qFl = Mathf.Max(qFl, q.RenderQueue);
                        int qHd = hdq != null ? hdq.RenderQueue : -1;
                        // 🔴 三条关系（**只能靠队列**：同队列时透明物体按「到相机的距离」排，填条中心偏左
                        //    ⇒ 离相机更远 ⇒ 会被轨道盖住，`CLAUDE.md` §三 那条）：
                        //    · 轨道**严格高于**压暗层/面板/填色（低一档就被压暗）；
                        //    · 轨道**不低于**内容档（同档 = 同属页面内容 —— 本窗那几颗内容件（关闭钮底 /
                        //      画质下拉框）与滑块**不重叠**；A168 那根 FPS 滑块的轨道也取同一档）；
                        //    · 填条 > 轨道 > …、手柄 > 填条（三层递进）。
                        CheckTrue(qBg > winChromeQ,
                                  $"{who} 的**轨道队列 {qBg} > 本窗压暗/面板/填色最高档 {winChromeQ}**"
                                + "（改坏法：退回 `WfSlider` 里那个硬编码 3000 ⇒ 被压暗层 3130 盖住 ⇒ 红）");
                        CheckTrue(qBg >= winContentQ,
                                  $"{who} 的**轨道队列 {qBg} ≥ 本窗内容档 {winContentQ}**"
                                + "（低于它 = 落在内容层之下、会被内容件盖住；A168 那根 FPS 滑块同档）");
                        CheckTrue(qFl > qBg, $"{who} 的**填条队列 {qFl} > 轨道 {qBg}**（同队列时填条会被轨道盖住）");
                        CheckTrue(qHd > qFl, $"{who} 的**手柄队列 {qHd} > 填条 {qFl}**（手柄要在最上层）");

                        // ② 端帽 = `m_Border ÷ m_PixelsPerUnitMultiplier(2) × 0.9`（A169 修：原来是 184 / 30
                        //    **贴图 px 原样**画 ⇒ 比原版宽 2.2 倍、中段短了一半）。量法同 A168 那根：
                        //    取该层里**窄于 200px** 的那几块子 quad 的宽（九宫格的左右端帽）。
                        float capBg = 0f;
                        foreach (var q in bgL.GetComponentsInChildren<ImageQuad>(true))
                            if (q != null && q.WorldW * 108f < 200f) capBg = Mathf.Max(capBg, q.WorldW * 108f);
                        CheckNear(capBg, 82.8f, 2f,
                                  $"{who} 轨道九宫格的**端帽**宽 = 原版 `m_Border 184 ÷ ppuMul 2` = 92 设计 px × 0.9 ⇒ **82.8**"
                                + $"（实得 {capBg:F2}；改坏法：端帽传 184（= A169 前的做法）⇒ 184；只除以 2 没过 0.9 ⇒ 92 ⇒ 都红）");
                        float handleV0 = s.Value;
                        s.SetValue(1f, false);          // ⚠️ `fire: false` —— 自检**不许**改总线/存档（只摆值）
                        float capFl = 0f;
                        foreach (var q in flL.GetComponentsInChildren<ImageQuad>(true))
                            if (q != null && q.WorldW * 108f < 200f) capFl = Mathf.Max(capFl, q.WorldW * 108f);
                        CheckNear(capFl, 13.5f, 1.5f,
                                  $"{who} 填条的**端帽**宽 = 原版 `30 ÷ ppuMul 2` = 15 设计 px × 0.9 ⇒ **13.5**"
                                + $"（实得 {capFl:F2}，值拉到 1 ⇒ 填条不缩放的那一帧；改坏法：传 30 ⇒ 30；只除 2 ⇒ 15 ⇒ 都红）");

                        // ③ 手柄：**实画边长** = **运行时框短边 × 0.9**。
                        //    🔴 **2026-10-07（波 8 · A197）**：原版那个 22.406 是手柄的**序列化**
                        //    `m_SizeDelta.y`，**不是**运行时的框高 —— uGUI `Slider.UpdateVisuals` 把手柄的
                        //    `anchorMin.y/anchorMax.y` 写成 **0 / 1**
                        //    （本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs:616-623`）⇒ 运行时框高 =
                        //    滑区高（本窗 **13**）+ 22.406 = **35.406**；110×110 方图 + `preserveAspect` 取短边
                        //    ⇒ 实画 = 35.406 × 0.9 = **31.87**（= 同窗那根 FPS 滑块的 `31.87`，A168 已在盯）。
                        //    A197 前这里期望的是 20.17（= 序列化值 × 0.9）⇒ 音频三根比 FPS 那根**小 37%**。
                        // ④ 手柄中心 = **轨道左端 + `m_AnchoredPosition.x`(11.99988) × 0.9 + 值 × 滑区宽**
                        //    （A169 前**少了中间那一项** ⇒ 整体偏左 10.8 画布 px）。
                        float hx0 = (s.HandleWorldPos.x - s.LeftWorld.x) * 108f;     // 值 **1** 那一帧的中心（相对轨道左端；值仍是上面 `SetValue(1f, false)` 摆的）
                        float hxAt0;                                                  // 值 0 那一帧
                        s.SetValue(0f, false);
                        hxAt0 = (s.HandleWorldPos.x - s.LeftWorld.x) * 108f;
                        s.SetValue(handleV0, false);                                  // 还原（不 fire）
                        if (hdq != null)
                        {
                            CheckNearPx(hdq.WorldW * 108f, 31.87f,
                                        $"{who} 手柄的**实画宽** = 原版 35.406（滑区 13 + 序列化 22.406）× 0.9"
                                      + "（改坏法：传回序列化的 22.406 ⇒ 20.17 ⇒ 红；这是 A197 修的那处）");
                            CheckNearPx(hdq.WorldH * 108f, 31.87f,
                                        $"{who} 手柄的**实画高** = 原版 35.406 × 0.9（方图 ⇒ 宽高相等）");
                        }
                        CheckNear(hxAt0, 10.8f, 0.6f,
                                  $"{who} 值 0 时手柄中心 = 轨道左端 + 原版 `m_AnchoredPosition.x` 12 × 0.9 ⇒ **+10.8**"
                                + $"（实得 +{hxAt0:F2}；改坏法：漏掉那一项 ⇒ 0 ⇒ 红）"
                                + " —— 🔴 波 8 核过：⛔ **不是** `+17 × 0.9 = 15.3`（那是 A169 附录把"
                                + " `Handle Slide Area` 的 `m_AnchoredPosition.x = −4.99988` 误读成「居中」算出来的；"
                                + " 拉伸轴上它是从**锚矩形中心**量起的 ⇒ 滑区左沿与轨道左沿重合）；"
                                + " 怎么改坏就红：把调用点的 `handleOffset` 改成 `17 × RootScale` ⇒ 15.3 ⇒ 红");
                        // 行程：值 0 → 值 1 走完整根滑区（原版滑区 = 684.195 − 10 = 674.195 设计 px）。
                        // 🔴 **2026-10-07（波 8）**：A169 起本件让出的那个 10 写死成 **10 画布 px**，而原版那 10 是
                        //    **设计** px（本窗 ⇒ 9）⇒ 实得会少 1 画布 px；当时用容差 2 盖住、并记在报告附录·3。
                        //    波 8 已把它并进 `capScale`（`WfSlider` 的 `_slideInsetU`）⇒ 实得 = (684.195 − 10) × 0.9
                        //    = **606.7755** 画布 px ⇒ 容差收到 **0.4**：**这条从此能分辨那 1 画布 px 的单位错**
                        //    （改回「10 画布 px」⇒ 605.78，差 1.00 > 0.4 ⇒ 红），同时照旧钉着「行程 = 整根滑区」
                        //    （改成 0 / 半根 ⇒ 差几百 px ⇒ 红）。⚠️ 本窗是**唯一**验得出这个单位错的一侧 ——
                        //    战斗那条父链无缩放（`capScale = 1`）⇒ 两种写法同值、那边看不出来。
                        CheckNear(hx0 - hxAt0, 606.78f, 0.4f,
                                  $"{who} 手柄的**行程**（值 0 → 值 1）= 原版滑区 (684.195 − 10) × 0.9 ⇒ **606.78**"
                                + $"（实得 {hx0 - hxAt0:F2}；⛔ 让位写成「10 画布 px」⇒ 605.78 ⇒ 红）");
                    }
                }
            }

            // ---------------- 音频页：三行的行顶 / 标签 / 滑块（🆕 2026-10-05 补） ----------------
            // 🔴 **补它的原因**：原来一条都没盯行顶 —— 三行一直按「组顶 + 序号 × 105」推，而原版三行
            //    行高**不相等**（`… Container` 的 `sizeDelta.y` = 105 / 106 / 106），且 VLG
            //    （`ctrlH=0` + `expandH=1`，组高 350.799988、三格 317）把 33.799988 的余量**每格均摊
            //    11.26666** ⇒ 行顶步进 = 行高 + 11.26666。旧摆法第 2/3 行**偏上 11.26 / 23.53px**，
            //    而滑块中心那条「行顶 + 63.5」**正好抵消**掉一部分 ⇒ 看着像对的（同族教训：
            //    「断言量不到」比「断言红了」更坏 —— `AutoFitBox` 那条与此同源）。
            //
            // 期望值 = **独立复读原版**：`python 工具/menu_dump.py bundle_menus_assets_all "Audio Settings"
            //   --depth 3 --md`（该工具的 VLG 仿真带 `--verify-layout`）+ 原始 `RectTransform` JSON 实读。
            // 🔴 ⛔ **不从 `SettingsWindow.AuRowTops` / `AuRowHs` 读期望值** —— 拿被测实现的常量再算一遍
            //    和自己比就是**同义反复**：把行顶换回 `AuT + i*105` 也照样绿（这条是本件的核心验收，
            //    见 `报告 块4_设置窗音频页.md` 的「怎么改坏就会红」表）。
            Section("音频页：三行的行顶 / 标签 / 滑块 —— 与原版逐值比（**三行行高不相等**）");
            {
                float[] auTop = { 280.146985f, 396.413648f, 513.680310f };   // 原版行顶（逐行给，别推）
                float[] auH = { 105f, 106f, 106f };                           // 原版三行行高（**不相等**）
                float[] auLblH = { 62f, 63f, 63f };                           // 原版三行 `Label` 的高
                var auBox = FindChild(FindChild(root, "Media Tab"), "Audio Settings");
                CheckTrue(auBox != null, "音频页的 `Audio Settings` 组在（下面 9 条都要它）");
                for (int i = 0; i < 3; i++)
                {
                    var auRow = FindChild(auBox, auNames[i] + " Container");
                    if (auRow == null) { CheckTrue(false, $"第 {i + 1} 行的 `{auNames[i]} Container` 在"); continue; }
                    // 容器节点摆在矩形中心 ⇒ 中心 y − 行高/2 = **行顶**（行高取的是**原版值**，不是我们的）
                    CheckNearPx(OrigPxY(auRow) - auH[i] * 0.5f, auTop[i],
                                $"第 {i + 1} 行 `{auNames[i]} Container` 的**行顶**（旧摆法给 {280.15f + i * 105f:F3}）");
                    var auLb = FindChild(auRow, "Label");
                    if (auLb == null) CheckTrue(false, $"第 {i + 1} 行的 `Label` 在");
                    else
                        CheckNearPx(OrigPxY(auLb) - auLblH[i] * 0.5f, auTop[i] - 13.5f,
                                    $"第 {i + 1} 行 `Label` 的**顶** = 行顶 − 13.5"
                                    + $"（旧摆法把它摆在行顶 {auTop[i]:F3} ⇒ 差 13.5px）");
                    if (sl == null || sl.Length != 3 || sl[i] == null)
                        CheckTrue(false, $"第 {i + 1} 行滑块在（量不到中心就没法比）");
                    else
                        CheckNearPx(OrigPxY(sl[i].WorldPos.y), auTop[i] + auH[i] * 0.5f + 11.1f,
                                    $"第 {i + 1} 行滑块的**中心** = 行中心 + 11.1"
                                    + $"（旧式「行顶 + 63.5」在第 {i + 1} 行差 {auTop[i] + auH[i] * 0.5f + 11.1f - (280.15f + i * 105f + 63.5f):F2}px）");
                }
            }

            // ---------------- 联机页 ----------------
            Section("联机页（**这一页是我们新增的设计**，用户规格逐条）");
            Click(Bar(root), "Online");
            CheckTrue(win.HostBlock != null && win.ClientBlock != null, "主机块与客机块都建了");
            Check(win.Role, NetRole.Host, "出厂是「主机」那一块（用户规格：勾选主机或客机）");
            CheckTrue(win.HostBlock.gameObject.activeSelf && !win.ClientBlock.gameObject.activeSelf,
                      "出厂只显示「主机」块");

            // ---------------- 🆕 A208（2026-10-10）：两个输入框的**矩形也要过 `Screen()`** ----------------
            // 缺陷：`MenuInputField.Create` 收的矩形**没过 `Screen()`**（裸设计值直接交给 `MenuDraw.Node/Rect/Hit`）
            //   —— 而**同一处**的字号 A171 起已经过了（`InputFontPx = 40 × 0.9 = 36`）⇒ 底板/命中区比字大 **11%**、
            //   而且**位置也偏外**（同一列的标签走 `Text` 漏斗、缩过；框没缩）＝ **本窗内部不自洽**。
            //   ⇒ 现在 `Create` 进门第一行过 `SettingsWindow.Screen()`（**唯一**那一处换算，见它的注释）。
            // 🔴 **怎么断（这条最容易写成自证）**：量的是**渲出来的**矩形（`RectOf`：quad 的 `WorldW/H` + 世界位置），
            //   期望值是**设计值 × 0.9 手算出来的字面量** —— ⛔ 不过 `Screen()`（这条抓的正是「矩形没缩」，
            //   期望值再过一遍那条换算就分辨不出来了）、⛔ 不引 `OnFieldT / OnFieldW / OnFieldH` 那几个实参。
            //   设计矩形（**联机页是我们自己的设计** ⇒ 这四个数是**规格**、不是原版判据）：
            //     IP 框 [596.52,400]–[1096.52,460]（= `TitleL` / `OnFieldT` / `+OnFieldW` / `+OnFieldH`）
            //       ⇒ x: 960 + (596.52−960)×0.9 = **632.868** · 960 + (1096.52−960)×0.9 = **1082.868**
            //       ⇒ y: 540 + (400−540)×0.9 = **414.0** · 540 + (460−540)×0.9 = **468.0**（高 54 = 60 × 0.9）
            //     密码框 [596.52,510]–[1096.52,570] ⇒ y: 540 + (510−540)×0.9 = **513.0** · 540 + (570−540)×0.9 = **567.0**
            var ipFieldN = FindChild(win.HostBlock, "IP Field");
            var pwdFieldN = FindChild(win.HostBlock, "Password Field");
            CheckTrue(ipFieldN != null && pwdFieldN != null,
                      "（A208 前提）联机页「主机」块那两个输入框节点在（`IP Field` / `Password Field`）");
            CheckRectPx(ipFieldN, 632.868f, 414f, 1082.868f, 468f,
                        "★（A208）IP 输入框**渲出来的矩形** = 设计 [596.52,400]–[1096.52,460] × `RootScale`0.9"
                      + " ⇒ [632.87,414.00]–[1082.87,468.00]（450×54）"
                      + "；改坏法：把 `MenuInputField.Create` 里那句 `SettingsWindow.Screen(...)` 去掉"
                      + "（= A208 之前的样子）⇒ 渲出来还是 [596.52,400]–[1096.52,460] ⇒ 差 11% ⇒ 红");
            CheckRectPx(pwdFieldN, 632.868f, 513f, 1082.868f, 567f,
                        "★（A208）密码输入框渲出来 = 设计 [596.52,510]–[1096.52,570] × 0.9"
                      + " ⇒ [632.87,513.00]–[1082.87,567.00]（450×54）"
                      + "；两个框必须**同一个左边**（同一列）+ 同一个宽（改坏法同上一条）");
            // ③ **对照**：框的左沿 = **同一列那颗标签**（`IP Label`，走的是 `Text` 那个漏斗 ⇒ A171 起就缩过）的左沿
            //    —— 「半缩半不缩」是这条缺陷的另一半（只把框缩了、标签没缩，或反过来），②那两条抓不住。
            //    ⚠️ 这是**跨两条代码路径**的一致性（`Text`+`AlignLeft` ↔ `MenuInputField.Create`），
            //    量的是**渲染真值**（`Label.WorldW` + 节点位置，同 `CheckLeftS` 那份算法）。
            var ipLbN = FindChild(win.HostBlock, "IP Label");
            var ipLb = ipLbN != null ? ipLbN.GetComponentInChildren<Label>() : null;
            CheckTrue(ipLb != null && ipFieldN != null,
                      "（A208 ③ 前提）`IP Label` 与 `IP Field` 都拿得到（下面那条才不是空断）");
            if (ipLb != null && ipFieldN != null)
            {
                float lbLeft = ipLb.transform.position.x * 108f + 960f - ipLb.WorldW * 108f * 0.5f;
                float fx1, fy1, fx2, fy2;
                bool okBox = RectOf(ipFieldN, out fx1, out fy1, out fx2, out fy2);
                CheckTrue(okBox && Mathf.Abs(fx1 - lbLeft) <= 2f,
                          $"★（A208）输入框左沿 = 同列标签左沿（框 {fx1:F2} vs 标签 {lbLeft:F2}，容差 2px）"
                        + " —— 两条**不同**的建树路径（`MenuInputField.Create` ↔ `Text`+`AlignLeft`）必须落在同一个 x 上"
                        + "；改坏法：只缩一半（框缩了、标签没缩 = 照旧）⇒ 差 36.3px ⇒ 红");
            }

            // 【刷新】填本机 IP
            // 🔴 **2026-09-26 加严**：原来只断「填了个**合法 IP**」—— 而 `LocalIPv4()` 失败时会**回落
            //    `127.0.0.1`**，那**也是**合法 IP ⇒ **功能坏着、断言照样绿**（实测就是这么坏的：
            //    `Dns.GetHostAddresses` 在批处理里抛 `Illegal byte sequence` ⇒ 【刷新】永远填回环）。
            //    现在改成断：① 枚举网卡这条路**找得到地址**；② 有真网卡时**不能填回环**；③ 多网卡能**换下一个**。
            var addrs = NetConfig.LocalAddresses();
            CheckTrue(addrs != null && addrs.Count > 0,
                      $"★ 枚举网卡找得到地址（实得 {(addrs == null ? 0 : addrs.Count)} 个）—— "
                    + "原来走 `Dns.GetHostAddresses`，在批处理里直接抛异常");
            var usable = addrs.FindAll(x => x.Usable);
            CheckTrue(usable.Count > 0, $"★ 至少有一个**能给对面填**的地址（实得 {usable.Count} 个）");
            // 🆕 网卡名是新加的 —— 多网卡/虚拟网卡时玩家**全靠它认**是哪一块
            CheckTrue(usable.TrueForAll(x => !string.IsNullOrEmpty(x.nic)),
                      "★ 每个候选地址都带**网卡名**（例如「WLAN」）—— 多网卡时靠它认");

            Click(win.HostBlock, "Refresh");
            CheckTrue(!string.IsNullOrEmpty(win.IpField.Text), $"点【刷新】⇒ IP 框里填上了本机地址（{win.IpField.Text}）");
            CheckTrue(System.Net.IPAddress.TryParse(win.IpField.Text, out _), "填进去的是个合法 IP");
            CheckTrue(win.IpField.Text != "127.0.0.1",
                      $"★ 填的**不是回环**（实得 {win.IpField.Text}）—— 把回环给对面等于没填；"
                    + "原来失败时正是回落它，而断言只判「是不是合法 IP」⇒ **照绿**");

            // 多网卡（有线 + 无线 + VPN）是**真实场景**：再点一次该换下一个候选
            // ⚠️ 循环**只在「能用的」地址里转** —— 回环（`127.0.0.1` / `::1`）与 v6 链路本地
            //    （`fe80::`）**填给对面等于没填**，不该出现在循环里。
            if (usable.Count > 1)
            {
                string first = win.IpField.Text;
                Click(win.HostBlock, "Refresh");
                CheckTrue(win.IpField.Text != first,
                          $"★ 再点一次【刷新】⇒ **换到下一个地址**（{first} → {win.IpField.Text}）；"
                        + $"本机共 {usable.Count} 个能用的候选");
            }
            for (int k = 0; k <= usable.Count; k++)     // 转满一圈
            {
                Click(win.HostBlock, "Refresh");
                string got = win.IpField.Text;
                string low = got.ToLowerInvariant();
                CheckTrue(got != "127.0.0.1" && got != "::1",
                          $"★ 循环第 {k + 1} 下**落不到回环**（实得 {got}）");
                // ⚠️ 还有三类「看着像地址、其实出不去」：IPv4 链路本地 `169.254.x.x`（网线没插时会有）、
                //    IPv6 链路本地 `fe80::`、IPv6 唯一本地 `fc..`/`fd..`（**最像公网地址的那个坑**）
                CheckTrue(!got.StartsWith("169.254.") && !low.StartsWith("fe80")
                          && !low.StartsWith("fc") && !low.StartsWith("fd"),
                          $"★ 循环第 {k + 1} 下也**不会填「出不去」的地址**（169.254 / fe80 / fc-fd）—— 实得 {got}");
            }

            // 🆕 2026-09-27：【Test Public IP】那颗钮（用户问「我在 IPv6 测试网站上明明看得到 IPv6，
            //    你这里为什么看不到」⇒ 加一颗把【外网看到的地址】与【本机网卡上的】摆在一起对照）。
            //    判据全文 → `资料/联机P2P_设计与交接.md` §11·4。
            var onlineTab = FindChild(root, "Online Tab");
            var echoBtn = FindChild(onlineTab, "Echo Button");
            CheckTrue(echoBtn != null, "★ 建了【Test Public IP】钮（外网地址探测）");
            if (echoBtn != null)
            {
                // 它在动作钮（Save/Check，只占左边 `OnBtnW` = 300）**右边**那片空位上，中间留 40 不压
                CheckRectS(echoBtn, SettingsWindow.TitleL + SettingsWindow.OnBtnW + 40f, SettingsWindow.OnBtnT,
                           SettingsWindow.TitleL + SettingsWindow.OnBtnW + 40f + SettingsWindow.OnEchoW,
                           SettingsWindow.OnBtnT + SettingsWindow.OnBtnH,
                           "【Test Public IP】落在 Save/Check 右边那片空位上");
                var saveBtn = FindChild(win.HostBlock, "Save Button");
                CheckTrue(saveBtn == null || echoBtn.position.x > saveBtn.position.x + 0.2f,
                          "★ 它**在 Save 的右边**、两颗不叠（实测两钮中心差 "
                        + (saveBtn == null ? "?" : ((echoBtn.position.x - saveBtn.position.x) * 108f).ToString("F1") + "px") + "）");
                var eb = echoBtn.GetComponentInChildren<WindowButton>(true);
                CheckTrue(eb != null && eb.onClick != null, "★ 那颗钮**绑了动作**（不是只有图的死钮）");
                // ⚠️ **故意不点它**：点了会**真联网**（后台线程去问回显站，超时最长 ~16 秒），
                //    结果还依赖当时网络 ⇒ 那就成了「看网速的自检」。**纯函数那半边在 `NetSelfTest` 里验**
                //    （`FirstIpIn` / `V6Routable` / CGNAT 判定都要么纯、要么有死数据）。
            }

            // 切角色
            Click(FindChild(root, "Online Tab"), "Role Client");
            Check(win.Role, NetRole.Client, "点「Client」⇒ 角色切成客机");
            CheckTrue(!win.HostBlock.gameObject.activeSelf && win.ClientBlock.gameObject.activeSelf,
                      "切成客机 ⇒ 块也跟着换（只有一块可见）");
            // 🆕 A208：客机块那两个框是**同一条 `Create` 路径**建的 ⇒ 这里抽查一个当代表
            //   （期望值同上面那三条的算法：`TitleL`596.52 / `OnFieldT`400 / `OnFieldW`500 / `OnFieldH`60 ⇒ ×0.9）
            CheckRectPx(FindChild(win.ClientBlock, "IP Field"), 632.868f, 414f, 1082.868f, 468f,
                        "（A208）客机块那个 `IP Field` 也过 `Screen()`（抽查；改坏法同主机块那两条）");
            Check((int)NetConfig.Current.role, (int)NetRole.Client, "角色**落盘**了（`NetConfig`）");

            // 【检查连接】打一个没人听的端口 ⇒ 必须**如实失败**
            win.IpField.SetText("127.0.0.1");
            var c = NetConfig.Current; c.port = 1;              // 端口 1 不会有人听
            Click(win.ClientBlock, "Check Button");
            CheckTrue(win.Flash != null && win.Flash.Length > 0, $"点【检查连接】⇒ 有反馈（「{win.Flash}」）");
            // 会话要有人泵（批处理里没有帧循环）
            var sess = NetRuntime.Instance != null ? NetRuntime.Instance.Session : null;
            CheckTrue(sess != null, "`NetRuntime` 建起来了（会话宿主）");
            if (sess != null)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 6000 && sess.State != NetState.Closed) { sess.Pump(); System.Threading.Thread.Sleep(10); }
                Check(sess.State, NetState.Closed, $"连一个没人听的端口 ⇒ 会话**如实结束**（{sess.LastError}）");
                CheckTrue(sess.LastError != null && sess.LastError.Length > 0, "失败原因是**人话**（不是空的）");
            }

            // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
            //   期望矩形 = **原版 prefab** `Main Menu Settings Window > Menu Area >
            //   Generic Popup Background` 那颗 `Image` 的 rect（391.29,164.80 → 1538.21,923.57 —— 画布 px，
            //   0.9 已烘在里面，2026-10-06 实读与 `Screen(PopL…)` 逐位吻合）；⛔ 不写 `SettingsWindow.PopL…`
            //   （那是被测实现**传进去的实参**，同式自证）。
            CheckAbsorbRule("设置窗", win.transform, "AbsorbHit",
                            391.29f, 164.80f, 1538.21f, 923.57f,
                            SettingsWindow.QShade, SettingsWindow.QOverlay, () => win.CurrentState);
            // ⚠️ 上面那一组**结尾就把窗关掉了** ⇒ 开回来，下面那句「点关闭钮 ⇒ 关」才是**真**在断。
            //   🔴 重开走 `Open()` = `Build()` **重建** ⇒ 重建前抓过的一切 `Transform` 都已销毁（Unity 假 null）。
            //   **2026-10-10（F4）这里的写法改了**：原来写的是 `area = FindChild(root, "Menu Area");`
            //   （**重抓一次**）—— 那条坑当时只堵住了 `area` 一个变量，`bar` 没堵 ⇒ A176 那一段的两次重建
            //   把音频页 / 联机页的切页点击全废掉了（7 条红）。现在**不留任何跨重建的缓存**：
            //   本片一律现取 `Area(root)` / `Bar(root)`（见那两个助手的注释）⇒ 这一行 `area` 重抓随之删掉。
            //   ⚠️ 知识照旧成立：**重开一次 = 整棵树换新**，`FindChild(旧树, …)` 会静静地拿到 null（那是假红）。
            CheckTrue(win.TryOpen(null), "（A94 收尾）把设置窗开回来 —— 下面那句 `Close()` 才不是空断");

            // ---------------- 🆕 A171：本窗文字字号 = 原版字面量 × 根上那层 0.9 ----------------
            //
            // 缺陷（2026-10-06 A168 写手顺手查出 · 2026-10-07 波 8 本批修）：**全窗文字都比原版大 11%** ——
            //   `MenuDraw.Text` 内部按**画布 px** 折算世界（`SetGlyphHeight(LayoutSpace.Px(fontPx))`），
            //   而调用点传进去的是**原版未缩放的** `m_fontSize`（如 `Tab Title` = 55）；本窗根那层
            //   **`m_LocalScale = 0.9`**（**只这一扇窗**，见 `Shell/SettingsWindow.cs` 文件头）是**烘进坐标**的
            //   ⇒ 原版屏幕上量到的字号 = `m_fontSize × 0.9`。位置/尺寸缩了、字没缩（55 画成 55、应画 49.5）。
            //
            // 判据（第一权威 = 原版 prefab 实读 `bundle_menus_assets_all` 的 `Main Menu Settings Window`）：
            //   · 根 `RectTransform_-7066813013973172314`：`m_LocalScale = (0.9,0.9,0.9)`
            //   · 各级 TMP 的 `m_fontSize` 原文：`Tab Title` **55** · FPS 标题与三个刻度 **42**（`FpsFont`）
            //     · 按钮 / 输入框 **40**（`FontButton` / `MenuInputField`）· 页签 **35** · 小字 **34**（`FontSmall`）
            //   🔴 **2026-10-10 订正（A207）**：本行原写「常规 **40**（`FontLabel`/`FontButton`）」——
            //      **「行标签」那一族（开关行 / 音轨行 / `Quality selector text`）的原版是 42、不是 40**，
            //      已改用新常量 `SettingsWindow.FontRowLabel`（亲读 `Vsync/Label` = `m_fontSize 42` 作证）。
            //      `FontLabel`（40）**原地留着**，它现在只服务两个判据未定的站（`Quality Value` + 我们自己的联机页）。
            //   ⇒ 屏幕上只可能是 {49.5 · 37.8 · 36 · 31.5 · 30.6}。
            //
            // 🔴 **期望值全写字面量**：⛔ 不写 `SettingsWindow.RootScale` / `PageTitleFontPx` / `FontSmall`
            //    —— 那是**被测实现里的常量**，拿它算期望就是同式自证（通则 → `A131_自证通则.md`）。
            // 🔴 量的是 `Label.FontPxNow`（TMP **实际生效**的 `fontSize` 折成画布 px），⛔ 不是
            //    `GlyphHeightWorld`/`CapHeightWorld`（那两个是**回读传入值**的伪测量，见 `已知的坑.md`）。
            Section("A171：本窗文字字号 = 原版 `m_fontSize` × 根上那层 0.9（**全窗一起缩**）");
            {
                // 原版设计字号 55 / 42 / 40 / 35 / 34 ⇒ × 0.9（原版根的 `m_LocalScale`）= 下面这 5 个值
                float[] wantPx = { 49.5f, 37.8f, 36f, 31.5f, 30.6f };
                const float TolPx = 0.35f;

                // ① 页标题（原版 `Tab Title`，`m_fontSize = 55`）—— 逐条点名的那一条
                var titleNode = FindChild(root, "Tab Title");
                var titleLb = titleNode != null ? titleNode.GetComponentInChildren<Label>() : null;
                CheckTrue(titleLb != null, "页标题 `Tab Title` 在（下面那条才有对象可量）");
                float titlePx = titleLb != null ? titleLb.FontPxNow : -1f;
                CheckNear(titlePx, 49.5f, TolPx,
                          "★ 页标题字号 = **原版 55 × 0.9 = 49.5 px**（`FontPxNow` = TMP 实际生效的 `fontSize`"
                        + " 折成画布 px；改坏法：把 `SettingsWindow.Text` 里的 `fs * RootScale` 去掉 ⇒ 实得 55.00 ⇒ 红）");
                // ② 反面（互为对照）：**没缩**的话就是 55.00 —— 这条证明①那个读数**分得出两种状态**
                //    （不然「怎么量都是 49.5 附近」时①也只是个巧合）
                CheckTrue(Mathf.Abs(titlePx - 55f) > 1f,
                          $"…而且它**不是修前那个 55**（实得 {titlePx:F2}；差 {Mathf.Abs(titlePx - 55f):F2}px）"
                        + " —— ①+② 合起来才说明 49.5 是**缩过**的结果");

                // ③ **全窗扫一遍**（验收原文：「要修就**全窗一起修**」）：
                //    窗根下**每一个** `Label` 的字号都必须落在那 5 个值里（±0.35px）。
                //    ⛔ **故意不点名节点**：点名只盖得住点到的那些；扫全树才抓得住「新加一段字忘了缩」——
                //      `MenuInputField`（联机页两个输入框）就是**绕过** `SettingsWindow.Text` 漏斗的第二个入口，
                //      本批也在它自己那边过了 `RootScale`（`MenuInputField.InputFontPx`）。
                var allLb = root.GetComponentsInChildren<Label>(true);
                int badN = 0, zeroN = 0; string badList = "";
                for (int i = 0; i < allLb.Length; i++)
                {
                    float px = allLb[i].FontPxNow;
                    bool ok = false;
                    for (int k = 0; k < wantPx.Length; k++)
                        if (Mathf.Abs(px - wantPx[k]) <= TolPx) { ok = true; break; }
                    if (ok) continue;
                    if (px <= 0f) { zeroN++; continue; }   // 点阵兜底 / TMP 没起来（`FontPxNow` 恒 0）—— 与「没缩」分开报
                    badN++;
                    if (badList.Length < 300) badList += $"{allLb[i].name}={px:F2} ";
                }
                CheckTrue(allLb.Length >= 15,
                          $"窗根下扫到 **{allLb.Length}** 个 `Label`（≥ 15 这一扫才有意义 —— 扫不到就等于没扫；"
                        + "⛔ 别把这个门槛删掉）");
                CheckTrue(badN == 0,
                          $"★ **全窗 {allLb.Length} 个 `Label` 的字号都 = 原版值 × 0.9**"
                        + $"（允许的 5 个：{wantPx[0]}／{wantPx[1]}／{wantPx[2]}／{wantPx[3]}／{wantPx[4]}，±{TolPx}px）"
                        + (badN > 0 ? $" —— **有 {badN} 个不在里面**：{badList}" : "")
                        + (zeroN > 0 ? $"；另有 {zeroN} 个 `FontPxNow` ≤ 0（TMP/字体资产没起来 —— 那是另一回事，"
                                     + "`FontPxNow` 在点阵兜底后端恒 0）" : "")
                        + "；改坏法：把 `SettingsWindow.Text` 的 `fs * RootScale` 去掉（或新加一段字直接调"
                        + " `MenuDraw.Text`、没自己过 0.9）⇒ 那一批实得回到 55/42/40/35/34 ⇒ 这条红");

                // 🆕 **2026-10-10（A207）点名钉那一族「行标签」= 原版 42** ——
                //    ③ 那条全窗扫描**同时允许 36（= 40×0.9）与 37.8（= 42×0.9）** ⇒
                //    「把 `SettingsWindow.FontRowLabel` 合并回 `FontLabel`（40）」这种错**它抓不住**（会静默绿）。
                //    判据 = 原版 `Vsync/Label` 亲读 **`m_fontSize = 42`**（`m_fontSizeMin 29` / `m_fontSizeMax 42` / 折行 1）
                //    —— `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2205799620510384038.json`。
                var vsN = FindChild(root, "VSync");
                var vsLb = vsN != null ? vsN.GetComponentInChildren<Label>() : null;
                CheckTrue(vsLb != null, "`VSync` 那一行在（下面两条才有对象可量）");
                float vsPx = vsLb != null ? vsLb.FontPxNow : -1f;
                CheckNear(vsPx, 37.8f, TolPx,
                          "★ 「**行标签**」族字号 = **原版 42 × 0.9 = 37.8 px**（`VSync` 作证）"
                        + "（改坏法：把调用点改回 `FontLabel`（40）⇒ 实得 36.00 ⇒ 红）");
                CheckTrue(Mathf.Abs(vsPx - 36f) > 0.5f,
                          $"…而且它**不是** 40 那一档缩出来的 36（实得 {vsPx:F2}）"
                        + " —— 这一条与上一条合起来，才把「42 族」与「40 族」**分开**");

                // ④ **别的窗零变化**（共用件那条默认路径）：
                //    本批**没有改** `Shell/MenuDraw.cs`（`git diff --numstat` 里它那两列是空的）⇒
                //    另外那 113 处 `MenuDraw.Text` 调用（`grep -rn "MenuDraw\.Text("` 实测 27 个文件 / 115 处，
                //    其中 2 处在本窗）拿到的是**逐字节相同**的代码。
                //    这条断言钉的是**将来**：谁把 0.9 硬写进共用件（**错的做法** —— 别的窗根上没有这层缩放；
                //    2026-10-07 实扫 `bundle_menus_assets_all` + `generalgamewindows` + `mainmenuwarpforge`
                //    + 13 个战场包的全部 `RectTransform`：`m_LocalScale` 恰为 0.9 的**只有 2 颗**，其中一颗
                //    是 `GameObject/Main Menu Settings Window.json` 这个**窗体根**、另一颗是名为 `Image` 的节点），
                //    这里当场红。
                var probeGo = new GameObject("A171 probe (MenuDraw.Text 默认路径)");
                var p1 = MenuDraw.Text(probeGo.transform, new PxRect(0f, 0f, 300f, 60f), "p1", Color.white, "p1",
                                       55f, MenuDraw.QText);
                CheckNear(p1 != null ? p1.FontPxNow : -1f, 55f, TolPx,
                          "★ 共用件默认路径：`MenuDraw.Text(...55f...)` ⇒ 实画 **55.00 px（不缩）**"
                        + " —— 别的窗都按原版 `m_fontSize` 原样传 ⇒ 因此一字未变；"
                        + "改坏法：把 `×0.9` 硬写进 `MenuDraw.Text` ⇒ 实得 49.5 ⇒ 这条红、而本窗那几条照样绿");
                //    对照：同一处**按本窗的规矩**传 49.5 ⇒ 画 49.5（否则上面那条只是「怎么传都是 55」）
                var p2 = MenuDraw.Text(probeGo.transform, new PxRect(0f, 0f, 300f, 60f), "p2", Color.white, "p2",
                                       49.5f, MenuDraw.QText);
                CheckNear(p2 != null ? p2.FontPxNow : -1f, 49.5f, TolPx,
                          "…（对照）同一处传 49.5 ⇒ 实画 49.5 —— 本窗漏斗干的正是这一下（两条互为对照才分得出状态）");

                // ================================================================
                // 🆕 **A333 / A336（2026-10-12）**：`MenuDraw.Text` / `TextBox` 的 `autoMaxPx` / `autoBasePx`
                //    **真的落进 TMP 的两个字段**。判据口径（`F1_字号线.md` §三 两条 + §三 末那两条通则）：
                //    ① 一律**反射直读 TMP 真字段**（`Label.FontSizeMax` / `Label.FontSizeBase`，`Battle/Label.cs`
                //       的现成口）—— ⛔ 不是读我们自己的账本；
                //    ② 期望值取自**原版资产字段**（这里是探针自己传进去的那两个数，就是原版那一档的值），
                //       ⛔ **不比我们自己的常量**。
                //    ⚠️ `FontSizeMax` / `FontSizeBase` 都是 **TMP 的 `fontSize` 单位**（不是 px）⇒
                //       一律经 `Label.FontSizeToPx` 折回画布 px 再比（**唯一那条 px 口径**）。
                // ================================================================
                Section("A333/A336：`autoMaxPx` / `autoBasePx` 真的落进 TMP 的 `fontSizeMax` / `m_fontSizeBase`");
                {
                    // 标称 41.4 ≡「我们原来那一档」（= 原版 `Card Detail Popup` 的 `Title` 面板标题那一颗的
                    // 收敛值）—— **故意让它与上限 42 不等**，否则「上限写错」这一档分不出来。
                    const float NominalPx = 41.4f, WantMaxPx = 42f, WantBasePx = 45.2f;

                    // ① 传 `autoMaxPx: 42f` ⇒ `m_fontSizeMax` 折回 px 应 = 42
                    var pMax = MenuDraw.Text(probeGo.transform, new PxRect(0f, 0f, 300f, 60f), "pMax",
                                             Color.white, "pMax", NominalPx, MenuDraw.QText,
                                             wrapPx: 300f, autoMinPx: 10f, autoMaxPx: WantMaxPx);
                    CheckTrue(pMax != null, "★ 探针 `pMax` 建出来了（下面那条才有对象可量）");
                    float maxPx = pMax != null ? Label.FontSizeToPx(pMax.FontSizeMax) : -1f;
                    CheckNear(maxPx, WantMaxPx, TolPx,
                              $"★ `MenuDraw.Text(..., autoMaxPx: {WantMaxPx})` ⇒ TMP 的 `m_fontSizeMax` 折回 = "
                            + $"**{WantMaxPx} px**（实得 {maxPx:F2}；标称那一档是 {NominalPx}）"
                            + "。改坏法：把 `MenuDraw.Text` 里 `autoMaxPx > 0f ? autoMaxPx : fontPx` 改回 `fontPx`"
                            + $" ⇒ 量出 {NominalPx:F2} ⇒ 红");

                    // ② 对照：**不传** `autoMaxPx` ⇒ 上限仍是调用方那一档（旧行为）
                    //    ⚠️ 这一条与①**合起来**才说明「42 是那个实参给的」而不是「怎么量都是 42」
                    var pDef = MenuDraw.Text(probeGo.transform, new PxRect(0f, 0f, 300f, 60f), "pDef",
                                             Color.white, "pDef", NominalPx, MenuDraw.QText,
                                             wrapPx: 300f, autoMinPx: 10f);
                    CheckTrue(pDef != null, "★ 对照探针 `pDef` 建出来了");
                    float defPx = pDef != null ? Label.FontSizeToPx(pDef.FontSizeMax) : -1f;
                    CheckNear(defPx, NominalPx, TolPx,
                              $"★ 对照：**不传** `autoMaxPx` ⇒ 上限 = 调用方那一档 = **{NominalPx} px**"
                            + $"（实得 {defPx:F2}；①+② 差 {Mathf.Abs(maxPx - defPx):F2}px ⇒ 两档分得开）"
                            + "。改坏法：把 `autoMaxPx > 0f` 写成 `>= 0f` ⇒ 上限被写成 0 —— "
                            + "而 `SetAutoFitBox` 的守卫 `maxPx <= 0f` 直接 `return` ⇒ 这个字段停在 TMP 的出厂值"
                            + "（40 个 fontSize 单位 ≈ 409 px）⇒ 这条红"
                            + "（**这正是「缺省 0」必须与「真的是 0」分开的原因**：0 是合法值）");

                    // ③ 传 `autoBasePx: 45.2f` ⇒ 反射读回的 `m_fontSizeBase` 折回 px 应 = 45.2
                    //    ⚠️ 这是个**没有公开访问器**的 `protected` 字段（`TMP_Text.cs:473`）⇒ `Label.FontSizeBase`
                    //       是反射读的；`_tmp == null`（点阵兜底后端）时它恒返回 **−1** ⇒ 这条会红、不静默。
                    var pBase = MenuDraw.TextBox(probeGo.transform, new PxRect(0f, 0f, 300f, 60f), "pBase",
                                                 Color.white, "pBase", NominalPx, 10f, MenuDraw.QText,
                                                 WantMaxPx, WantBasePx);
                    CheckTrue(pBase != null, "★ 探针 `pBase` 建出来了（`MenuDraw.TextBox` 那一口）");
                    float basePx = pBase != null ? Label.FontSizeToPx(pBase.FontSizeBase) : -1f;
                    CheckNear(basePx, WantBasePx, TolPx,
                              $"★ `MenuDraw.TextBox(..., autoBasePx: {WantBasePx})` ⇒ TMP 的 `m_fontSizeBase`"
                            + $" 折回 = **{WantBasePx} px**（实得 {basePx:F2}）"
                            + "。改坏法：把 `Battle/Label.cs` 的 `float baseCur = basePx > 0f ? cur * (basePx / nomPx) : cur;`"
                            + $" 改回 `= cur` ⇒ 量出 {NominalPx:F2} ⇒ 红");
                }
                Object.DestroyImmediate(probeGo);

                // ⚠️ **本轮如实记的一条（不在本件白名单内 ⇒ 只记录、没动）**：联机页那两个输入框
                //    （`MenuInputField.Create`）的**矩形**是**裸设计值**、没过 `Screen()`（字号本批已修）——
                //    它比同页的标签大 11%、位置也偏外（`Shell/SettingsWindow.cs` 的 `BuildRoleBlock` 传的是
                //    未缩放的 `x1/OnFieldT/OnFieldH`）。⇒ 已写进 `波8_A171_设置窗字号.md` 的报告，另行派活。
            }

            // 关窗
            Click(FindChild(Area(root), "Generic Close Button"), "Hit");
            Check(win.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗口进 Closed 态");

            Shoot("settings_online.png", true);
        }
        finally
        {
            NetConfig.OverridePath = null;
            SmallScreenUI.PersistOverride = false;      // A165：把注入点也放回去（开关的内存态上面已放回出厂值）
            AutoZoom.PersistOverride = false;          // A172：同上（那颗开关的内存态也放回去了）
            // 🆕 A176：同上；另外把**那份 URP 资产**放回出厂值并清脏位 —— 自检绝不许把工程资产留在改过的状态
            //    （同族先例：`BattlePostFx` 那条「`battlearena1_PostFx.asset` 被自检弄脏」的教训）。
            SuperSampling.PersistOverride = false;
            SuperSampling.QualityLevelGetter = null;
            SuperSampling.ResetForTest();
            RestoreRenderScale();
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Debug.Log(P + $"===== 通过 {_pass} · 失败 {_fail} =====");
        // 🔴 **2026-10-12（A443 · 调度台裁定）**：这一串是**失败表的【重列】**（每条失败在 `Check()` 里
        //   **已经现场打过一次**、行首是真 `✗`，见本文件 `:47`）⇒ 重列这里**不能再带 `✗`** ——
        //   原来是 `✗` 时日志里 `✗` 行数 = 失败数 **×2**，连「按行首标记数」都数不准
        //   （`资料/已知的坑.md`「别用 `grep -c ✗` 数失败」）。同族五处已改 →
        //   `ShellScene` / `CollectionScene` / `RewardsScene` / `ShopScene`（A350）· `MainMenuScene`（A443）；
        //   本处是 A443 补上的最后一处。⚠️ **别顺手改 `:47` 那条真 `✗`**（`Check()` 现场那条**不是重列**）。
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "   失败重列：" + f);
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ============================================================ 🆕 A176：那份 URP 资产（超采样真正写进去的地方）

    /// <summary>那份 URP 资产 —— **独立解一次**：走 `QualitySettings.renderPipeline`（= **当前画质档自己那份**，
    /// 工程设置里那一格的值），⛔ 不是实现里那条 `GraphicsSettings.currentRenderPipeline`
    /// （两处都能到同一个对象，这里取的不是被测那一份代码）。
    /// ⚠️ 两条路都不成 ⇒ null（断言会以 −1 报出来，**不静默**）。</summary>
    static UniversalRenderPipelineAsset UrpAsset()
    {
        var rp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (rp != null) return rp;
        return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    }

    /// <summary>把那份资产放回**原版常态值 1.0** 并清脏位（A176 那节收尾 + `finally` 各调一次）。
    /// 🔴 **必须清脏位**：我们在编辑器里改的是一个**真实存在的工程资产**（`Assets/Settings/PC_RPAsset.asset`）——
    /// 留着脏位，编辑器退出/保存时就可能落盘成 `m_RenderScale: 2`，而这条设置**不该**靠改文件生效
    /// （落地走运行时，见 `Shell/SettingsWindow.cs` 的 `SuperSampling`）。
    /// 同族先例：`Battle/BattlePostFx.cs` 文件头记的「`battlearena1_PostFx.asset` 被自检弄脏」那件事。</summary>
    static void RestoreRenderScale()
    {
        var rp = UrpAsset();
        if (rp == null) return;
        if (!Mathf.Approximately(rp.renderScale, 1f)) rp.renderScale = 1f;
        EditorUtility.ClearDirty(rp);
    }

    // ============================================================ 建场景

    static SettingsWindow Build(out Transform root, out Transform canvasAnchor)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        // 🔴 **A154（2026-10-06）**：原来这里只建 `3 - PopUp Holder`（那时本窗的 `placement` 是 15）——
        //    本窗的档位**照原版改成 5（Canvas）**之后，这里必须给**5 那一档**的 Holder，否则
        //    `AttachToAnchor` 会「找不到 5 的锚点」而把窗留在场景根上（**那是假绿**：三颗 Holder 在我们这儿
        //    都是 identity 的空 GO ⇒ 几何断言照样过）。名字照原版 `2 - Canvas Holder Above upper bar`。
        //    `3 - PopUp Holder` 留着：下面 A166 那条探针窗与任何走弹窗档的件都要它。
        //    👉 **2026-10-11（A351）**：这条要求现在由 `WindowsManager.EnsureHost()` 保证（三颗**一起**建）
        //      —— 下面那段订正就是它；本条 A154 注释作为**历史**保留（它解释了「为什么当时非建 Canvas 那颗不可」）。
        var wm = WindowsManager.EnsureHost();      // 它自己建 "Window Anchors" + 三颗 Holder + 管理器，并**登记 `Instance`**
        // 🔴 **2026-10-11（A351）就地订正（铁律 5）**：这里原来**手抄了第三份**「三颗 Holder +
        //    `AddComponent<WindowsManager>()`」（上面的 A154 注释就是给那份手抄写的）。
        //    · **硬伤**：`WindowsManager` **没有 `[ExecuteAlways]`**（`Shell/WindowsManager.cs` 里**只有** `WindowHolder` 那颗**有**）
        //      ⇒ 批处理（编辑模式）**`Awake` 不跑** ⇒ 手抄那一下 **`Instance` 恒 null**
        //      （`Instance` 只在 `Awake` 里赋，**批处理下那句从不执行**）。判据（**四条独立记录**，
        //      全是踩过的坑）：`Shell/PromptPopup.cs:868` · `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 ·
        //      `Shell/PointerLayer.cs:47-48` · `资料/已知的坑.md:704`。
        //    · ⇒ **任何走 `WindowsManager.EnsureHost()` 的开窗路径都会【再建一台】**（`Instance == null` 时
        //      不查「场景里是不是已经有一台」，直接再建一套管理器 + 锚点）
        //      ⇒ 窗落进**第二台**、`wm.openWindows` 里没有它、读 `WindowsManager.Instance` 的代码静默拿到另一台。
        //      2026-10-11 那 8 条红就是这个形状造成的（判据 → `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1）。
        //      **最小改法 = 走公共件**（`EnsureHost()` 的文档注释：「壳与「单独打开某个界面场景」**都走它**，
        //      两处各建一次 = 迟早不一致」）。
        //    ⚠️ **与手抄那份有一处【有意的差异】**：手抄只建 `2 - Canvas` / `3 - PopUp`（World 那档当时没建）；
        //      `EnsureHost()` **三颗都建** ⇒ 补上 `1 - Below Upper Bar Holder`（原版那三颗 Holder
        //      **缺一不可**，`1 - Below Upper Bar Holder{10}` 正在其中）⇒ 任何走 World(10) 档的窗从此挂到**正确**的锚点上
        //      （改前会报「找不到 10 的锚点」、留在场景根 —— 那是「能跑但不是原版挂法」）。
        //    ⚠️ `canvasAnchor` 改为从**锚点表**取（`GetWindowAnchor(Canvas)`）：它与 `AttachToAnchor` 用的是
        //      **同一张表**（`WindowsManager.GetWindowAnchor`）⇒ 下面那条「`win.transform.parent == canvasAnchor`」
        //      断的仍是同一件事，⛔ **不是自证**（取不到时 `GetWindowAnchor` 会**报错并返回 null**，
        //      那条断言照样红 —— 不静默）。
        //    **改坏法（如实说 —— 今天【照不出来】，它是一笔【去掉地雷】的改动，⛔ 不是「修好了一条会红的断言」）**：
        //      把这一句换回手抄的 `AddComponent<WindowsManager>()` ⇒ `Instance` 又变回 null；而**本自检今天没有**
        //      走 `WindowsManager.EnsureHost()` / `OpenByRef()` 的开窗入口（现场全部是直调 `win.Manager.OpenWindow(...)`）
        //      ⇒ **改坏它，本文件一条断言都不会红**。它的判别力在【将来】：`WindowsManager.OpenByRef()` 的**第一句**
        //      就是 `EnsureHost()` —— 谁在这几扇窗里接一条走它的入口（`BattleLogTab` / `LeaderboardRow` 那一族就是
        //      这么接的），第一次跑就会**另建一台管理器 + 第二套锚点**、窗落进第二台 ⇒ 现象与判据 →
        //      `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1（`RewardsScene` 那 8 条红就是同一个形状）。
        canvasAnchor = WindowsManager.GetWindowAnchor(WindowsPlacement.Canvas);

        var win = SettingsWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

    /// <summary>⚠️ **2026-10-11（A351）起 `Build()` 不再调它** —— 那几颗 Holder 现在由
    /// `WindowsManager.EnsureHost()` 建（同一个形状、名字与 placement 逐字相同，见 `Build()` 里那段订正）。
    /// **它留着不删**：这是「单独打开某个界面场景」那条路的**形状存档**（同形手抄全仓原有 4 处，A351 全收口）
    /// —— 留着比删掉更能让下一个会话看出「原来长什么样」。⛔ 新代码别调它。
    /// ⚠️ 它**不是** `WindowsManager` 里那份同名私有件（那份在 `Shell/WindowsManager.cs` 里是 `static` 私有、复用不了）
    /// —— 这正是当年四处各抄一份的来由。</summary>
    static Transform MakeHolder(Transform parent, string name, WindowsPlacement p)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        var h = t.gameObject.AddComponent<WindowHolder>();
        h.placement = p;
        h.RegisterNow();
        return t;
    }

    // ============================================================ 截图

    static void Shoot(string file, bool allowBlank = false)
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
        float lum = MeanBrightness(tex);
        if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（**按已知情况放行**）");
        else CheckTrue(lum > 3f, $"{file} 不是空图（平均亮度 {lum:F1} > 3）");
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    static float MeanBrightness(Texture2D t)
    {
        var px = t.GetPixels32();
        double s = 0;
        for (int i = 0; i < px.Length; i += 7) s += (px[i].r + px[i].g + px[i].b) / 3.0;
        return (float)(s / (px.Length / 7.0));
    }
}
