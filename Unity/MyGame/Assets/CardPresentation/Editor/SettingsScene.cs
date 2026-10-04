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

    /// <summary>🆕 **2026-10-06（A83② —— A81 的尾巴）**：压暗层（「点窗外关窗」）命中区那条不变量 ——
    /// 「档 = **该窗压暗层自己那一档**，且**严格低于**本窗任何内容命中区档」，并核「这个节点
    /// **确实是公共件 `MenuDraw.ShadeHit` 建的**」。
    /// <para>🔴 期望值全是该窗自己的**原版档常量**（⛔ 别从被测实现里读）；第三句断的是**全工程不变量**
    /// （`ShadeHit` 的档位告警一次都没响过）。🔴 **为什么必须问 `WasShadeHit`**：档本来就对的那几扇窗，
    /// 走不走公共件**没有任何可见行为差异** ⇒ 只有这一句能分出两种状态（改回自己那份 `MenuDraw.Hit` 就红）。</para>
    /// <para>⚠️ 与 `Editor/{CollectionScene,MainMenuScene,RewardsScene,ShopScene}.cs` 里那份**逐字同源**
    /// （本文件原来没有这个 helper —— 见 `项目任务.md` §三 第 29 条 A 表的 A83②）。</para></summary>
    static void CheckShadeRule(string what, Transform darkHit, int qShade, int qContentMin)
    {
        string why;
        CheckTrue(MenuDraw.ShadeRuleOk(darkHit, qShade, qContentMin, out why),
                  $"{what}：压暗层的命中区「档 = 压暗层那一档({qShade}) 且 < 内容命中区档({qContentMin})」"
                  + "（" + (why.Length > 0 ? why : "三条都过：节点在 + 带 `ImageQuad` + 档号对") + "）");
        CheckTrue(MenuDraw.WasShadeHit(darkHit),
                  $"{what}：这条命中区**是公共件 `MenuDraw.ShadeHit` 建的**"
                  + "（改回本窗自己那份 `MenuDraw.Hit(...)` 这条就红）");
        Check(MenuDraw.ShadeHitTierWarns, 0,
              $"{what}：`MenuDraw.ShadeHit` 的**档位告警一次都没响过**（响过 = 有人把 `qShade` 传成了派生值）");
    }

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
        Check(MenuDraw.AbsorbTierWarns, 0,
              $"{what}：`MenuDraw.Absorb` 的**档位告警一次都没响过**（响过 = 该窗没有空档，档算错了）");

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
    /// 一个**单张** quad 的矩形（吸收层底就是一张平图），所以直接取 `GetComponentInChildren<ImageQuad>()`。</summary>
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
    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }
    static void Click(Transform t)
    {
        var b = t != null ? t.GetComponentInChildren<WindowButton>() : null;
        if (b == null || b.onClick == null) { CheckTrue(false, "点击区 `" + (t != null ? t.name : "?") + "` 不在（或没接 onClick）"); return; }
        b.onClick();
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
            CheckShadeRule("设置窗", win.ShadeHit, SettingsWindow.QShade, SettingsWindow.QOverlay);
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
            var area = FindChild(root, "Menu Area");
            CheckAtS(area, SettingsWindow.PopL, SettingsWindow.PopT, SettingsWindow.PopR, SettingsWindow.PopB,
                     "`Menu Area`（弹窗本体）");
            CheckRectS(FindChild(area, "Generic Popup Background"), SettingsWindow.PopL, SettingsWindow.PopT,
                       SettingsWindow.PopR, SettingsWindow.PopB, "`Generic Popup Background`（九宫格 `40k_popup`）");
            CheckRectS(FindChild(area, "Background fill"), SettingsWindow.FillL, SettingsWindow.FillT,
                       SettingsWindow.FillR, SettingsWindow.FillB, "`Background fill`（`40k_popup_texture` 平铺）");
            CheckRectS(FindChild(area, "Separators"), SettingsWindow.BarSepL, SettingsWindow.BarSepT,
                       SettingsWindow.BarSepR, SettingsWindow.BarSepB, "`Separators`");
            CheckRectS(FindChild(area, "Generic Close Button"), SettingsWindow.CloseL, SettingsWindow.CloseT,
                       SettingsWindow.CloseR, SettingsWindow.CloseB, "`Generic Close Button`（75×75）");
            CheckRectS(FindChild(FindChild(area, "Generic Close Button"), "Icon"),
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
            CheckRectPx(FindChild(area, "Generic Close Button"),
                        1499.10f, 136.44f, 1566.60f, 203.94f,
                        "（②锚）关闭钮渲染矩形 = 原版 [1559.00,91.61]–[1634.00,166.61] 经「0.9 + 画布中心」缩放");
            var bgQ = FindChild(area, "Generic Popup Background").GetComponentInChildren<ImageQuad>();
            CheckTrue(bgQ != null && bgQ.Texture != null && bgQ.Texture.name == "40k_popup",
                      "弹窗底图 = `40k_popup`");

            // ---------------- 左栏三个页签 ----------------
            Section("左栏页签（原版这一列是 VLG：padTop 30 · 每键 178.42×157.68 · 从 y=153.10 起）");
            var bar = FindChild(area, "Tab Buttons");
            CheckAtS(bar, SettingsWindow.BarL, SettingsWindow.BarT, SettingsWindow.BarR, SettingsWindow.BarB, "`Tab Buttons` 列");
            var names = new[] { "Graphics", "Audio", "Online" };
            for (int i = 0; i < 3; i++)
            {
                float t = SettingsWindow.BarT + SettingsWindow.BarPadTop + i * SettingsWindow.TabBtnH;
                var n = FindChild(bar, names[i]);
                CheckTrue(n != null, $"第 {i + 1} 个键 `{names[i]}` 建出来了（我们只建 3 个 —— 原版 5 个，见文件头 ③）");
                if (n == null) continue;
                CheckAtS(n, SettingsWindow.BarL, t, SettingsWindow.BarR, t + SettingsWindow.TabBtnH,
                         $"`{names[i]}` 键在 VLG 算出来的位置（第 {i + 1} 个）");
                CheckTrue(TextOf(n) == names[i], $"`{names[i]}` 的页签文字");
            }
            CheckTrue(FindChild(bar, "General") == null && FindChild(bar, "Account") == null,
                      "原版的 `General`/`Account`/`Support` 三个键**不建**（那几页没做，不摆假键）");

            // 🆕 A17：本窗的换图（关闭钮的圆底 → `40k_bt_close_hover` · 三个页签 → `…_selected` · 画质下拉 → `…_opened`
            //   · 动作钮 → `40K_button_hover`）逐个悬停验一遍；顺带盯 A21「选中态用 `_hover`」
            CheckHoverSwap(win.transform, "设置窗");
            CheckTrue(SettingsWindow.ArtTabBgSel == "40K_settings_button_hover",
                      "A21：页签**选中态**用的是 `…_hover`（原版 `EverguildToggle.onSprite`），**不是** `…_selected`");
            CheckNoMissingSwapArt("设置窗");

            // ---------------- 切页 ----------------
            Section("切页（只切 activeSelf）");
            var pages = new[] { "Graphics Tab", "Media Tab", "Online Tab" };
            for (int i = 0; i < 3; i++)
            {
                Click(FindChild(bar, names[i]));
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
            Click(FindChild(bar, "Graphics"));
            int q0 = QualitySettings.GetQualityLevel();
            int qAsked = -1, vAsked = -1;
            SettingsWindow.QualitySetterOverride = lv => qAsked = lv;
            Click(FindChild(FindChild(root, "Graphics Tab"), "QualityHit"));
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
            Click(FindChild(FindChild(FindChild(root, "Graphics Tab"), "VSync"), "Hit"));
            SettingsWindow.VSyncSetterOverride = null;
            Check(vAsked, v0 > 0 ? 0 : 1, $"点 `VSync` 行 ⇒ 要求翻转（{v0} → {vAsked}）");
            CheckNear(QualitySettings.vSyncCount, v0, 0.01f, "🔴 `vSyncCount` 也没被自检改掉");
            Debug.Log(P + "  （图像页建了 `Quality`/`Small Screen UI`/`Auto Zoom`/`VSync`/`FPS limit` 五件 ——"
                    + " `FPS limit` 自 A168 起是**滑块**；那一列自 A170 起是**真的 `Scroll View`**"
                    + "（原版 `RectMask2D` 视口），**A172 起行位回正、整列塞得进视口 ⇒ 滚不动**；"
                    + "原版那几行里 `Text In Hand Selector` / `Hi FPS` / `Android extra compatibility` 运行时**都不在**，"
                    + "`Use super sampling` 只在 Ultra 档出现（我们没超采样能力 ⇒ 不建，见 `BuildGraphicsPage` 里那条出声））");

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
            //    `Use super sampling` 只在 **Ultra** 档（`allowSuperSampling` 五档 = 0/0/0/0/1）⇒ 两种排法：
            //      VeryLow–High：`Small Screen(0)·Auto Zoom(1)·Vsync(2)·FPS(3)` ⇒ 内容高 **346.923**
            //      Ultra       ：中间插 `Use super sampling(2)` ⇒ `Vsync(3)·FPS(4)` ⇒ 内容高 **508.205**
            //    我们照前者（= 出货默认档 High）⇒ **内容高 346.92 < 视口高 521.51 ⇒ 这一列滚不动**
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
                    CheckNearPx(gsc.ContentX2 - gsc.ContentX1, 312.23f,
                                "内容高 = 原版运行时 4 行合计 346.923 设计 px × 0.9（3 × 75.641 + 105 + 3 × 5）"
                              + "（改坏法：写成旧的「7 行 588.84」或「3 行」⇒ 红）");
                    // ★★ A172 的核心：**这一列滚不动**（内容高 346.92 < 视口高 521.51 ⇒ `ClampHi` 收成 0）
                    CheckNearPx(gsc.MaxOffset, -157.12f,
                                "内容高 346.923 − 视口高 521.5072 = **−174.58 设计 px** ⇒ × 0.9 = **−157.12 画布 px**"
                              + "（🔴 `MaxOffset` 本身是**负的** —— 别把它当「能滚多远」，能不能滚要看 `ClampHi`）"
                              + "；改坏法：`GfxContentH` 写回 588.84（旧的「7 行」）⇒ 这里变 +60.61 ⇒ 红");
                    CheckNearPx(gsc.ClampHi, 0f,
                                "★★ **没有可停留的滚动范围**（`ClampHi` = Max(0, MaxOffset) = **0**）"
                              + "—— 这就是原版那条链的等效物：`Content.m_SizeDelta.y = 300` + CSF `m_VerticalFit = 0`"
                              + " + `ScrollRect.GetBounds()` 只取 Content 自己的矩形 ⇒ `CalculateOffset` 恒 0"
                              + "（判据全文 → `资料/普查产出_1007/审查_A170两条前提.md` §①）");
                    CheckNearPx(gsc.ClampLo, 0f, "夹取下界也是 0（内容从视口上沿起排）");
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
                              + "（改坏法：跟音频页那根一样按 22.406 画 ⇒ 20.17 ⇒ 红）");
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

            // ---------------- 🆕 A172：图像页**第 1 行** = Auto Zoom（原版 `GraphicsTab.autoZoom`）----------------
            // 判据（2026-10-07 实读）：
            //  · `GraphicsTab__AutoZoomClick.c`：**同时**写 `GameStaticData.autoZoom`(+0x125) 与
            //    `autoZoomChosenManually`(+0x12f) —— 与旁边那颗 `SmallScreenToggleClick` 同一形状；
            //  · 有消费者：`BattleSettingsWindow__OnAutoZoomChanged.c` 先写同一个字段、再
            //    `CombatAutoZoom.ResetCameraZoomUIAction()` ⇒ 原版 = **战斗相机的自动缩放**
            //    （🔴 我们**没做** `CombatAutoZoom` ⇒ 这一格目前不产生效果，点它必须出声）；
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
                    CheckTrue(AutoZoom.Enabled, "★ 点一下 ⇒ 原版 `GameStaticData.autoZoom` 那一半**开了**");
                    CheckTrue(AutoZoom.ChosenManually,
                              "★ 同一下 ⇒ 原版 `autoZoomChosenManually`(+0x12f) 那一半**也置了**（只写一个 = 跟原版不一样 ⇒ 红）");
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

                // 🔴 **收尾**：开关放回**出厂值**（原版 cctor = 0）—— 下面那几十条断言与最后那张截图
                //    都该在「原版出厂态」下跑（`PersistOverride` 仍是 true ⇒ 玩家的真设置一个字节都没动）。
                SmallScreenUI.Set(false);
                CheckTrue(!SmallScreenUI.Enabled, "（收尾）自检跑完把开关放回**出厂值 关**");
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
            Click(FindChild(bar, "Audio"));
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
            Click(FindChild(bar, "Online"));
            CheckTrue(win.HostBlock != null && win.ClientBlock != null, "主机块与客机块都建了");
            Check(win.Role, NetRole.Host, "出厂是「主机」那一块（用户规格：勾选主机或客机）");
            CheckTrue(win.HostBlock.gameObject.activeSelf && !win.ClientBlock.gameObject.activeSelf,
                      "出厂只显示「主机」块");

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

            Click(FindChild(win.HostBlock, "Refresh"));
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
                Click(FindChild(win.HostBlock, "Refresh"));
                CheckTrue(win.IpField.Text != first,
                          $"★ 再点一次【刷新】⇒ **换到下一个地址**（{first} → {win.IpField.Text}）；"
                        + $"本机共 {usable.Count} 个能用的候选");
            }
            for (int k = 0; k <= usable.Count; k++)     // 转满一圈
            {
                Click(FindChild(win.HostBlock, "Refresh"));
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
            Click(FindChild(FindChild(root, "Online Tab"), "Role Client"));
            Check(win.Role, NetRole.Client, "点「Client」⇒ 角色切成客机");
            CheckTrue(!win.HostBlock.gameObject.activeSelf && win.ClientBlock.gameObject.activeSelf,
                      "切成客机 ⇒ 块也跟着换（只有一块可见）");
            Check((int)NetConfig.Current.role, (int)NetRole.Client, "角色**落盘**了（`NetConfig`）");

            // 【检查连接】打一个没人听的端口 ⇒ 必须**如实失败**
            win.IpField.SetText("127.0.0.1");
            var c = NetConfig.Current; c.port = 1;              // 端口 1 不会有人听
            Click(FindChild(win.ClientBlock, "Check Button"));
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
            //   🔴 重开走 `Open()` = `Build()` **重建** ⇒ 上面抓的 `area` 已经销毁了（Unity 假 null），
            //   要**重新取一次**，否则下面 `FindChild(area, …)` 会静静地拿到 null（那是假红）。
            CheckTrue(win.TryOpen(null), "（A94 收尾）把设置窗开回来 —— 下面那句 `Close()` 才不是空断");
            area = FindChild(root, "Menu Area");

            // 关窗
            Click(FindChild(FindChild(area, "Generic Close Button"), "Hit"));
            Check(win.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗口进 Closed 态");

            Shoot("settings_online.png", true);
        }
        finally
        {
            NetConfig.OverridePath = null;
            SmallScreenUI.PersistOverride = false;      // A165：把注入点也放回去（开关的内存态上面已放回出厂值）
            AutoZoom.PersistOverride = false;          // A172：同上（那颗开关的内存态也放回去了）
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Debug.Log(P + $"===== 通过 {_pass} · 失败 {_fail} =====");
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "  ✗ " + f);
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
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
        var anchors = new GameObject("Window Anchors").transform;
        canvasAnchor = MakeHolder(anchors, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);
        MakeHolder(anchors, "3 - PopUp Holder", WindowsPlacement.Popup);

        var wmGo = new GameObject("WindowsManager");
        var wm = wmGo.AddComponent<WindowsManager>();

        var win = SettingsWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

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
