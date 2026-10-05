// CollectionScene.cs — 收藏线（`Collection Menu Variant`）的**场景 / 自检 / 截图**
//
// 用法：`Unity -batchmode -quit -executeMethod CollectionScene.Run`
// 判据全部来自 **原版参数**（`资料/阶段二_卡组线_原版规格.md` + `资料/普查产出_0923/A1~A4`），
// **不是我们自己的常量**（否则就是自证 —— 见 CLAUDE.md §二 那条）。
//
// ⚠️ 本文件的辅助函数（`Check*/FindChild/Shoot`）是**照 `ShopScene` / `RewardsScene` 又抄了一份**
//    —— 那三份各自有一整套，**这是一笔明账**（该收口成 `Editor/MenuCheck.cs`）。已记在
//    `项目任务.md` §三 第 15 条；**本轮先不动那三个绿着的文件**。
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuleEngine;
using CardPresentation;     // ⚠️ 类本身**留在全局命名空间** —— 见下面那条注释

// 🔴 **`-executeMethod` 按类名找，带命名空间就找不到**（2026-09-23 踩：写成 `namespace CardPresentation`
//    之后跑出「executeMethod class 'CollectionScene' could not be found」，三次都是这一条）。
//    其余三个自检（`RewardsScene`/`ShopScene`/`DeckScene`）也都在**全局命名空间** ⇒ 照它们来。
public static class CollectionScene
{
        const string P = "[Collection] ";
        const string ShotDir = "d:/4/_tmp_view/collection";
        const string TestDeckFile = "d:/4/_tmp_view/collection/_test_decks.json";

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
        /// 🔴 **2026-10-07（A77⑬⑥）本文件里的副本已删** —— 唯一一份在 `MenuDraw.CheckShadeRule`。
        /// ⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
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
        /// （⛔ 不写被测那份实现**传进去的实参** —— 那是最浅一档的同式自证）；
        /// 档 = 该窗自己的**原版档常量**（`qShade` / `qContentMin`；⚠️ 这两个是 `Absorb` 的入参来源，
        /// 与压暗层那条 `MenuDraw.CheckShadeRule` 不是同一套判据 —— 后者 2026-10-07 起改成量场景真值了）。</para>
        ///
        /// <para>🔴 **为什么两条行为必须一起断**：只断「点面板 ⇒ 不关」时，一个**根本关不掉的窗**也能绿；
        /// 只断「点面板外 ⇒ 关」时，把窗建小到「点哪儿都关」也绿。两条互为对照才分得出这两条路。</para>
        ///
        /// <para>⚠️ **点哪儿（两个点，判据不同）**：
        /// · **面板内**：先试**原版矩形中心**，被窗内真件（按钮）盖住时沿一圈**固定的**候选点找一个
        ///   「命中是吸收层」的点。那一处「命中是谁」**不是期望值**，它只是**选点的条件** ——
        ///   断的是**窗的状态**（`state()`）。
        /// · **面板外**：⛔ **不扫、钉死屏幕左上角 (5,5)**，而且「命中是谁」**就是期望值**
        ///   （必须是**本窗压暗层那一颗**：`IsChildOf(winRoot)` ∧ `MenuDraw.WasShadeHit`）。
        ///   扫一圈会让「某颗命中区过大、把压暗层吃掉一半」这类缺陷从别的候选点上绕过去。
        ///   为什么钉 (5,5) 是**原版判据**算出来的 → 那一段的行内注释。</para></summary>
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
            var q = node != null ? node.GetComponentInChildren<ImageQuad>() : null;
            if (q == null)
            {
                CheckTrue(false, $"{what}：吸收层下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这一层等于没建）");
            }
            else
            {
                float w = q.WorldW * 108f, h = q.WorldH * 108f;
                float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
                CheckNear(cx - w * 0.5f, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图");
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
            // 点面板外：**钉死屏幕左上角 (5,5)**。
            // 🔴 **为什么偏偏是 (5,5)，而且不许改成「扫一圈找第一个命中压暗层的点」**（2026-10-06 FIX-1）：
            //    · 原版 `Deck info Popup > Warlord Image` 那颗 `Image` 的 rect 是
            //      `−108.98,−33.99 → 999.02,1074`（1108²）—— **它确实盖着 (5,5)**；但它带
            //      `m_RaycastPadding = (246.8, 84.44, 338.6, 132.38)`，**正 = 往里缩**（见实现侧注释），
            //      ⇒ 原版的**命中区**只剩 `137.82,98.39 → 660.42,989.56` ⇒ 原版在 (5,5) 命中的就是压暗层。
            //    · ⛔ 若改成「四角/四边按固定顺序扫，取第一个命中本窗压暗层的点」：立绘命中区一旦
            //      **又变回过大**（= 我们刚修掉的那个缺陷），搜索会从 `(1915,5)` 之类**绕过去**、
            //      照样绿 ⇒ 这一条就再也查不出那个缺陷了（本工程那一族「弱断言分不出两种状态」）。
            //      ⇒ **选点的判据是原版 prefab，不是「扫到一个能用的」** —— 点钉死、期望钉死。
            //    · 打印实测点与实测命中名（下面那条），出红时能直接看出「是被谁吃掉的」。
            const float OutX = 5f, OutY = 5f;
            var oHit = pl.ButtonAt(OutX, OutY);
            // 🔴 **判据 = 两条合起来**，⛔ 不许再写成「非吸收层 ∧ 属于本窗」那种**分不出两种状态**的弱条件
            //    —— 旧写法下 `WarlordHit`（立绘命中区）三条全满足、**照样绿**，正是它把这个缺陷放过去了：
            //      · `oHit.transform.IsChildOf(winRoot)` = **是这一扇自己的**命中区（别家的窗顶掉它就红）；
            //      · `MenuDraw.WasShadeHit(oHit.transform)` = **是压暗层那一颗**（`ShadeHit` 建的，
            //        按节点上的标记认、**不按名字认** —— 本工程三扇窗里这颗节点**两个名字**：
            //        卡组信息窗/导入卡组窗叫 `BackgroundHit`、**聊天窗叫 `CloseHit`**（名字是各调用点自己传的
            //        `MenuDraw.ShadeHit(..., name)` 形参）⇒ 按名字写 `Find("BackgroundHit")` 会把聊天窗那条**误判成红**）。
            //      ⛔ **别只写 `WasShadeHit`**：它认的是「是不是压暗层那颗」、**不认「是哪一扇的」**。
            CheckTrue(oHit != null && oHit.transform.IsChildOf(winRoot) && MenuDraw.WasShadeHit(oHit.transform),
                      $"{what}：**({OutX:F0},{OutY:F0}) 命中的就是这扇窗自己的压暗层那一颗**"
                      + "（立绘命中区 / 吸收层 / 别家的窗把它顶掉时**这条红** —— 旧写法分辨不出，就是它放过了 A94）"
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
        static void CheckNear(float got, float want, float tol, string msg)
            => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");
        static void CheckText(string got, string want, string msg)
            => CheckTrue(got == want, $"{msg} —— 实测「{got}」，期望「{want}」");

        /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1px）。期望值必须来自原版像素矩形。</summary>
        static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
        {
            if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
            var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
            float d = Vector3.Distance(t.position, want);
            CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d * 108f:F2}px）");
        }

        /// <summary>🆕 2026-10-11（F6）：**像素空间 2D** 版的 `CheckAt` —— ⛔ **z 不参与比较**。
        /// <para>为什么单开一条（而不是改 `CheckAt`）：`CheckAt` 比的是 `Vector3.Distance(节点, 期望)`，
        /// **含 z**、容差 **0.01 世界单位 = 1.08px**；而命中区节点可以被**有意**前移 z ——
        /// `Shell/DeckInfoPopup.cs:213-218` 的 `HitZFront` 就是那把「同队列（`QDIHit` 3123）同 z 打平 ⇒
        /// 显式排出谁深谁浅」的尺子（原版靠 `m_Children` 的兄弟序定胜负，我们这侧只能靠 z）。
        /// **那不是「节点没摆对」**，却**一个人就能吃满 `CheckAt` 的容差** ⇒ 那种红是**口径错**，不是缺陷。</para>
        /// <para>口径：容差 **±1.5px**、**逐轴**比（与四条边那组 `CheckNear` 同一个口径；换算尺子仍只此一份
        /// —— 转调 `LayoutSpace.ToPixel`）。判别力照旧：抓的是「节点停在容器 `(0,0)`」这类
        /// **几百 px** 的错（2026-09-23 那个「节点全停在容器原点、只有里面的 quad 画对了」的坑）。</para></summary>
        static void CheckAtPx(Transform t, float cx, float cy, float tol, string what)
        {
            if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
            var p = LayoutSpace.ToPixel(t.position);
            float dx = p.x - cx, dy = p.y - cy;
            CheckTrue(Mathf.Abs(dx) <= tol && Mathf.Abs(dy) <= tol,
                      $"{what}（实测中心 ({p.x:F2},{p.y:F2})，期望 ({cx:F2},{cy:F2})±{tol:F2}px，"
                      + $"差 ({dx:F2},{dy:F2})；**z 不参与** —— 那颗节点的 z 是按 `HitZFront` 有意前移的）");
        }

        static void CheckRectPx(Transform t, float x1, float x2, float y1, float y2, string what)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
            CheckNear(q.WorldW * 108f, x2 - x1, 2.0f, what + " 宽(px)");
            CheckNear(q.WorldH * 108f, y2 - y1, 2.0f, what + " 高(px)");
        }

        static void CheckArt(Transform t, string want, string what)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            var nm = q != null && q.Texture != null ? q.Texture.name : null;
            CheckText(nm, want, what);
        }

        /// <summary>🆕 2026-10-05（A32③）读一棵子树里那个 `ImageQuad` 的 tint 并比对。
        /// ⚠️ 期望值写的是**原版 prefab 里 `offColor` 的字面量**（不是我们代码里的常量 —— 否则就是自证）；
        /// 容差 **2/255**（颜色是从 float 字面量来的，四舍五入到 1 位足够）。
        /// 取不到那个 quad 时返回 `(-1,-1,-1,-1)` ⇒ **必红**（不会静默放过）。</summary>
        static void CheckTint(Transform t, Color want, string what)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            var got = q != null ? q.Tint : new Color(-1f, -1f, -1f, -1f);
            bool ok = Mathf.Abs(got.r - want.r) < 2f / 255f && Mathf.Abs(got.g - want.g) < 2f / 255f
                      && Mathf.Abs(got.b - want.b) < 2f / 255f && Mathf.Abs(got.a - want.a) < 2f / 255f;
            CheckTrue(ok, $"{what} —— 实测 ({got.r:F3},{got.g:F3},{got.b:F3},{got.a:F3})，"
                          + $"期望 ({want.r:F3},{want.g:F3},{want.b:F3},{want.a:F3})");
        }

        static Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>一格卡的渲染队列（取卡内所有层里**最小的那个** —— 卡内层序靠 z 偏移、整格一起平移，
        /// 所以最小号就代表这一格；见 `CardFan.SetCardQueue`）。
        /// ⚠️ 读 `sharedMaterial`：`SetCardQueue` 走 `.material`（会把实例写回 `sharedMaterial`），
        /// 两边读到的是同一份，且**不会再实例化一次**。</summary>
        static int CardQueue(CardView v)
        {
            if (v == null) return -1;
            int q = int.MaxValue;
            foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial != null) q = Mathf.Min(q, mr.sharedMaterial.renderQueue);
            return q == int.MaxValue ? -1 : q;
        }

        /// <summary>Deck 页那条 `Empty Collection Warning` 现在亮着没有。
        /// ⚠️ **必须限定在 Deck 页里找** —— 四个页各有一份**同名**节点（原版如此，矩形逐页不同）。</summary>
        static bool DeckEmptyShown(CollectionWindow win)
        {
            var ew = FindChild(win.PageRoot(0), "Empty Collection Warning");
            return ew != null && ew.gameObject.activeSelf;
        }

        static float PxOf(float worldX) { return worldX * 108f + 960f; }
        static float PxYOf(float worldY) { return 540f - worldY * 108f; }

        /// <summary>🆕 2026-10-08（A181）：一个节点子树里**所有启用中的 `MeshRenderer`** 的网格顶点，
        /// 在画布像素里的范围（左上原点 · y 向下）。返回 false = 一个顶点都没量到。
        /// 🔴 **为什么要它**：视口裁切那件事**量节点位置量不出来** —— `CardView` 的自建网格是按局部系摆的，
        /// 节点全对而**画出来的层越界**（这正是 A181 当初漏掉的那一条）。只有逐顶点换算才看得见。
        /// ⚠️ 跳过 `ImageQuad`（它自己会 `RebuildMesh`，是战斗里光环那一族；要量它请用
        /// `RectOf` / `WorldW/H`，如卡背页那条断言）。
        /// 🆕 2026-10-11（F3）：**TMP 那份网格只量字形顶点**（`GlyphVertsOf`）——
        /// 它那份数组里夹着零面积的**非字形占位槽**，全扫会量出假的范围。</summary>
        static bool RenderExtentPx(Transform t, out float x1, out float y1, out float x2, out float y2, out int verts)
        {
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue; verts = 0;
            if (t == null) return false;
            foreach (var mr in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || !mr.enabled || mr.GetComponent<ImageQuad>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var vs = mf.sharedMesh.vertices;
                if (vs == null) continue;
                var tmp = mr.GetComponent<TMPro.TextMeshPro>();
                List<int> gi = null;
                if (tmp != null)                      // TMP 自家那份网格 ⇒ 按字取四角（见 `GlyphVertsOf`）
                {
                    gi = GlyphVertsOf(tmp, vs.Length);
                    if (gi.Count == 0) continue;      // 这一段没有字形 ⇒ 一个点都不量（别把占位槽当几何）
                }
                int cnt = gi != null ? gi.Count : vs.Length;
                for (int i = 0; i < cnt; i++)
                {
                    var p = LayoutSpace.ToPixel(mr.transform.TransformPoint(vs[gi != null ? gi[i] : i]));
                    verts++;
                    x1 = Mathf.Min(x1, p.x); x2 = Mathf.Max(x2, p.x);
                    y1 = Mathf.Min(y1, p.y); y2 = Mathf.Max(y2, p.y);
                }
            }
            return verts > 0;
        }

        /// <summary>同上，但量的是**一段文字**（TMP 网格顶点，画布像素范围）。
        /// ⚠️ TMP 的网格只在「重排过」之后才有内容 —— 这里如实返回 false（**别当通过**）。
        /// `verts == 0` = 这一段没有可量的网格。
        /// 🆕 2026-10-11（F3）：**按字取**（`characterInfo[i].vertexIndex`），⛔ 不扫整条数组 ——
        /// 同一份数组里还夹着零面积的**非字形占位槽**（见 `GlyphVertsOf`），
        /// 它们停在「这一段字自己的原点」上 ⇒ 整条一扫量出来的是**假的范围**。
        /// ⚠️ 这里读的仍旧是 `textInfo`（**模型**）；要读**真上传的那一份**见本文件那条
        /// 「★ 卡上没有一个**字的顶点**画到视口外」（A250 块）。</summary>
        static bool TextExtentPx(Transform t, out float x1, out float y1, out float x2, out float y2, out int verts)
        {
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue; verts = 0;
            if (t == null) return false;
            foreach (var tmp in t.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            {
                if (tmp == null) continue;
                var ti = tmp.textInfo;
                if (ti == null || ti.meshInfo == null || ti.characterInfo == null) continue;
                int cn = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
                for (int ci = 0; ci < cn; ci++)
                {
                    var ch = ti.characterInfo[ci];
                    if (!ch.isVisible) continue;                 // 非字形槽：四角全被 TMP 写成零、画不出来
                    int mi = ch.materialReferenceIndex;
                    if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                    var mv = ti.meshInfo[mi].vertices;
                    if (mv == null) continue;
                    int v = ch.vertexIndex;
                    if (v < 0 || v + 3 >= mv.Length) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        var p = LayoutSpace.ToPixel(tmp.transform.TransformPoint(mv[v + k]));
                        verts++;
                        x1 = Mathf.Min(x1, p.x); x2 = Mathf.Max(x2, p.x);
                        y1 = Mathf.Min(y1, p.y); y2 = Mathf.Max(y2, p.y);
                    }
                }
            }
            return verts > 0;
        }

        /// <summary>🆕 2026-10-11（F3）：一份 TMP 的网格里，**真正是字形的那些顶点**的下标
        /// （每个可见字 4 个角 · 序 = BL·TL·TR·BR，与 `MenuDraw.ClipTmpMesh` / `BaseCornerAlpha` 同一个序）。
        /// <para>🔴 **为什么要它**：`mesh.vertices` **不是**「一个个字的四角」—— 同一份数组里还夹着
        /// **非字形的退化占位槽**（零面积、不进任何三角形 ⇒ **画不出来**）：
        /// ① TMP 对**不可见的字**（空格 / 超出 `maxVisibleCharacters` 的字）把四角全写成 `Vector3.zero`
        ///   并置 `isVisible = false`，**却照旧占这 4 个槽**
        ///   （判据 = `TextMeshPro.cs:4536-4541` 写零 + `:4550-4552` `FillCharacterVertexBuffers(i)` 照跑）；
        /// ② 数组按 2 的幂扩容 ⇒ 尾巴上还有一段**从没写过的零槽**（`TMP_MeshInfo.ResizeMeshInfo`）。
        /// 换算成像素，这些点全落在**那一段字自己的原点**（= 该 TMP 的 pivot）上 —— 字被裁到视口边时
        /// **它们不动**（`ClipTmpMesh` 只认 `isVisible` 的字，`:867` ⇒ **压根不夹这些槽**），
        /// 于是一整条数组扫下来，它们会被当成「字画到视口外」。
        /// **2026-10-11 实红**：`CollectionScene.Run` 的 qOut 期望 [0] 实得 [12] = **3 槽 × 4 点**。</para>
        /// <para>⚠️ 只收 `materialReferenceIndex == 0` 的字：别的材质那份顶点**不在这一份网格上**
        /// （`TextMeshPro.UpdateVertexData` 把 `meshInfo[0]` 推给 `m_mesh`、`meshInfo[i>0]` 推给子件
        /// `m_subTextObjects[i].mesh`，判据 `TextMeshPro.cs:416-424`）⇒ 拿 `vertexIndex` 索引**本网格**会串位。
        /// 本工程的卡面标签只有一个字体资产（`Resources/Fonts/NotoSerifCJK-Regular SDF.asset`，
        /// `m_FallbackFontAssetTable: []`）⇒ 实际恒为 0。</para>
        /// <returns>字形顶点下标（空表 = 这一段没有可量的字形）。</returns></summary>
        static List<int> GlyphVertsOf(TMPro.TextMeshPro tmp, int meshVertCount)
        {
            var dst = new List<int>(64);
            if (tmp == null) return dst;
            var ti = tmp.textInfo;
            if (ti == null || ti.characterInfo == null) return dst;
            int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
            for (int ci = 0; ci < n; ci++)
            {
                var ch = ti.characterInfo[ci];
                if (!ch.isVisible) continue;
                if (ch.materialReferenceIndex != 0) continue;
                int v = ch.vertexIndex;
                if (v < 0 || v + 3 >= meshVertCount) continue;
                dst.Add(v); dst.Add(v + 1); dst.Add(v + 2); dst.Add(v + 3);
            }
            return dst;
        }
        static float Wpx(Transform t)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            return q != null ? q.WorldW * 108f : 0f;
        }
        static float Hpx(Transform t)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            return q != null ? q.WorldH * 108f : 0f;
        }

        /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
        /// 图走 `ImageQuad.WorldW/H`、字走 `Label.WorldW/H`（**都是真测量**，不是回读我们传进去的数）；
        /// 取的是**组件自己的 transform**（`AlignLeft/Right` 会把 `Label` 的节点挪走，拿外层容器算就会偏）。
        /// 🆕 2026-10-03：本文件原来只有 `Wpx/Hpx`（只给宽高）—— 要量「这块矩形落在哪」时不够用，
        /// 照 `RewardsScene.RectOf` / `ShopScene.RectOf` 的同名口子补一份
        /// （本文件开头那条注释已经明记：「四个自检各自一套辅助函数」是**一笔明账**，本轮不动那三个绿着的文件）。</summary>
        static bool RectOf(Transform t, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (t == null) return false;
            Transform node = t; float w, h;
            var lb = t.GetComponentInChildren<Label>();
            var q = t.GetComponentInChildren<ImageQuad>();
            if (lb != null) { node = lb.transform; w = lb.WorldW * 108f; h = lb.WorldH * 108f; }
            else if (q != null) { node = q.transform; w = q.WorldW * 108f; h = q.WorldH * 108f; }
            else return false;
            float cx = PxOf(node.position.x), cy = PxYOf(node.position.y);
            x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
            y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
            return true;
        }
        static string TextOf(Transform t)
        {
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            return lb != null ? lb.Text : null;
        }

        /// <summary>筛选栏里一行小标题 **渲出来的左沿**（画布 px · 左上原点 · y 向下）。
        /// 🔴 量的东西：`Label.WorldW`（TMP `textBounds` 的**真测量**，见 `Label.RefreshBounds`）反推的左缘 ——
        /// **不是**节点位置、更**不是**「对齐枚举 == Left」（那种断言是同义反复：把渲染那一句删掉照样绿）。
        /// ⛔ 期望值由**调用方**给（取自原版读数），本函数只负责量。
        /// 量不出来（那行小标题不在 / `WorldW` 是垃圾）⇒ 返回 **−9999** ⇒ 断言必红，**不静默放过**
        /// （宽度上下界那道守卫与 `Label.HasMeasuredWidth` 同一条：TMP 在未激活 / 空串时给的是天文数字）。</summary>
        static float TitleLeftPx(Transform panel, string title)
        {
            var t = FindChild(panel, "Title " + title);
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            if (lb == null) return -9999f;
            float w = lb.WorldW * 108f;
            if (!(w > 20f && w < 2000f)) return -9999f;
            return PxOf(lb.transform.position.x) - w * 0.5f;
        }

        // ============================================================ 🆕 2026-10-04：软边接线探针
        //
        // 判据 = `Shell/MenuDraw.cs` 的 `ApplySoftEdges`（原版 `RectMask2D.m_Softness` 的几何等效物）：
        //   非 0 时把一块**沿渐隐带的内沿切开**（原节点留含矩形中心的那一格、其余格建**子 quad**，
        //   命名 `…_soft<i><j>`）⇒ 父块与子块那条**共享边**就是带的内沿。
        // 🔴 **为什么这就是「接没接」的判据**：软边 = 0（硬边）时**一个子块都不会有** ⇒ 表空。
        //   于是「表空 = 没接」；而「切线该在哪」是**自己拿原版值 + 原版视口矩形算出来的**
        //   （⛔ 不是从被测实现里读常量）⇒ 改坏实现（删赋值 / 改数值 / 把两轴写反）都会真红。

        /// <summary>一个 `ImageQuad` **渲出来**的像素矩形。⚠️ 只用**这个组件自己**
        /// （不比 `RectOf`：那个会先找 `Label`、还会往子树里钻 —— 量切出来的每一块必须逐块量）。</summary>
        static bool QuadRectOf(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (q == null) return false;
            float cx = PxOf(q.transform.position.x), cy = PxYOf(q.transform.position.y);
            float w = q.WorldW * 108f, h = q.WorldH * 108f;
            x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
            y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
            return true;
        }

        /// <summary>扫 `root` 子树，回传里面**所有软边切线**的位置（同 `RewardsScene.ScanSoftCuts`）。
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
                if (!QuadRectOf(q, out hx1, out hy1, out hx2, out hy2)) continue;
                for (int i = 0; i < q.transform.childCount; i++)
                {
                    var c = q.transform.GetChild(i).GetComponent<ImageQuad>();
                    // ⚠️ **只认软边切出来的子块**（`ApplySoftEdges` 的命名 `baseName + "_soft" + i + j`）——
                    //    `ImageQuad.CreateNineSlice` 那 9 块是**兄弟**不是父子，但留一道名字闸更保险。
                    if (c == null || c.name.IndexOf("_soft") < 0) continue;
                    float cx1, cy1, cx2, cy2;
                    if (!QuadRectOf(c, out cx1, out cy1, out cx2, out cy2)) continue;
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

        /// <summary>切线清单的**逐条**判据：每条都必须落在 `want` 里（±`tol`），且 `want` 每一项**都出现过**。
        /// `what` 里写清每一侧的算式（判据要能在失败信息里一眼看懂）。</summary>
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
                CheckTrue(k >= 0, what + $"：切线 #{i + 1} 在 {cuts[i]:F2} ⇒ 必须是带的内沿"
                    + "（" + string.Join(" / ", System.Array.ConvertAll(want, v => v.ToString("F2"))) + "）");
                if (k >= 0) hit[k] = true;
            }
            for (int j = 0; j < want.Length; j++)
                CheckTrue(hit[j], what + $"：**{want[j]:F2} 这条切线确实出现**（少一条就说明那侧的软边没生效）");
        }

        // ============================================================ 🆕 A327：两态夹具（一条共用 · 四份【函数体】逐字同源）
        //
        // 🔴 **为什么要它**：2026-10-11（W4）把「世界 → 设计」那一族（`MenuDraw.PosInDesignSpace` / 各窗的
        //   `Local`·`Local3` / `CampaignTab.BuildLine` / `ShopWindow.BuildTimeCounter` / `CampaignTab.BuildArmyItems`）
        //   修完之后发现：**`k == 1`（小屏缩放开关出厂关着）时新旧两式逐位相同** ⇒ 那 8 处全是**潜伏缺陷**
        //   —— **今天一条现有断言都不会红**（不是「有断言挡着」，是**还没有断言**）。
        //   ⚠️ **2026-10-11（FX3）收窄一处口径**：那 8 处里**基准恰好就是窗根**的那几处，新旧两式在生产里
        //   **永远**逐位相同（`basis == 窗根` ⇒ 除的是 Holder，恒单位缩放）⇒ 是 **no-op**，不是「潜伏」；
        //   真带牙口的是**非根基准**那一族。展开见本段后面那条订正。
        //   判据 / 逐处清单 / 「该断言什么」→ `资料/普查产出_1011/W4_子3.md` §四·b。
        //
        // 🔴 **夹具形状**（判据给的就是这一条，⛔ 别另设计一套）：
        //   ① 态一 = 开关**关**（出厂态）⇒ 量一次 → `p1`；② 态二 = 开关**开** + **被乘的那一级**乘 M（走**生产那条路**
        //   `TransformScalerBySmallScreenUI`：`SetScale(M)` + `Tick()`，批处理没有帧循环）⇒ 再量同一个对象 → `p2`；
        //   ③ 断 **`p2 == M × p1`**（⛔ **一个我们自己的常量都不读** —— 只读 M）。
        //
        // 🔴 **2026-10-11（FX3）三处订正 —— 上一版夹具【自己把这条恒等式砸了】**（Shell 4 + Collection 4 条红；
        //   判据全文 → `资料/普查产出_1011/DIAG-A_Shell与Collection八条红.md`）：
        //   ① **M 加在【基准的父级】那一级**，⛔ **不是基准自己** —— `PosInDesignSpace` 除的正是
        //      `t.parent.lossyScale`（`Shell/MenuDraw.cs:74-78`），而 `p2 == M × p1` **只在「基准的父级就是
        //      那个被乘 M 的根、且那个根在世界原点」时成立**（`Shell/MenuDraw.cs:57-61` 自己写着适用范围）。
        //      上一版把 M 加在**窗根自己**身上、又把**窗根**挪到 (2,1.5) ⇒ 恒等式被夹具亲手破坏：
        //      偏差逐条 = `(1−M)×(2,1.5)` = **(−0.400, −0.300)**，与实现无关（日志里 8 条逐条对到小数点后 3 位）。
        //   ② **可观测余量** = 「**基准相对被乘那一级的位移** ≥1 设计单位」（⛔ 不是「离**世界原点**」——
        //      上一版量的就是后者，所以它逼着调用方去挪窗根）。坏式与好式相差 `M(M−1)×|那个位移|` ≈ `0.24 × |位移|`，
        //      容差 **0.02 单位（2.2px）** ⇒ `|位移| ≥ 1` 时偏差 ≥ 0.24 单位 = **26px**，远远超出容差 ⇒ 真会红。
        //      （四个调用点取 (2,1.5) ⇒ 0.6 单位 = **65px**。）
        //   ③ **态二的 `measure` 里必须【重建】**（`Open()` → `Build()` 首句清空子件）—— 不重建时被量的局部位置
        //      是 `k == 1` 那一趟**冻结**下来的值，新旧两式在那时**逐位相同** ⇒ 断言恒真（= 假绿）。
        //      带牙口的判据 → `资料/普查产出_1011/W4_子3.md:91-106`；同族先例 = `Editor/ShopScene.cs` 的 **A294** 那一段（同文件的两态探针）。
        // 🔴 **参数 1 = 被乘 M 的那一级**（本文件与 `ShellScene` 那四个调用点里它是**窗根的父级探针根**；
        //   ⚠️ 另两份副本（`RewardsScene` / `ShopScene`）传的是**窗根自己**、基准是窗根的子件 —— 2026-10-11（A350）四份已同步到**同一口径**；两族各自都对，⛔ 别按「哪一族更对」去改）。
        // ⚠️ **态二会把那一级乘 M 再还原**（`localScale` 放回 1 · 组件销毁 · 开关放回关）—— 直线写法，没有提前 return。
        static void CheckScaleTwo(GameObject scaleRoot, Transform basis, System.Func<Vector3> measure, float m, string what)
        {
            CheckTrue(scaleRoot != null && basis != null && measure != null, "（前提）" + what + "：夹具的件齐了");
            if (scaleRoot == null || basis == null || measure == null) return;
            SmallScreenUI.Set(false);                              // 态一：开关**关**（出厂态）
            Vector3 p1 = measure();
            Vector3 b1 = basis.position;                           // 态一的基准位置 = 它的**设计**位置（k == 1）
            Vector3 w1 = scaleRoot.transform.position;             // 被乘那一级的位置（态一；生产里 = 原点）
            // （前提②·可观测余量）**基准相对被乘那一级的位移** ≥1 设计单位 —— 基准落在那一级的原点上时
            // 「除不除缩放」两式**恒等** ⇒ 断言「什么都不中」也全绿。⛔ 上一版量的是「离**世界原点**」，
            // 逼着调用方去挪窗根、又把恒等式砸了（见本段文件头 ①②）。
            CheckTrue(Mathf.Abs(b1.x - w1.x) > 1f || Mathf.Abs(b1.y - w1.y) > 1f,
                      $"（前提）{what}：**基准相对被乘 M 那一级的位移 ≥1 设计单位**（实测 {b1.x - w1.x:F2},{b1.y - w1.y:F2}）"
                    + " —— 位移≈0 时「除不除缩放」两式恒等 ⇒ 这一条会退化成假绿");
            CheckNear(scaleRoot.transform.localScale.x, 1f, 1e-4f, "（前提）" + what + "：态一那一级没被谁乘过");
            SmallScreenUI.Set(true);                               // 态二：开关**开** + 那一级乘 M
            var sc = scaleRoot.GetComponent<TransformScalerBySmallScreenUI>();
            if (sc == null) sc = scaleRoot.AddComponent<TransformScalerBySmallScreenUI>();
            sc.SetScale(m);
            sc.Tick();                                            // 批处理没有帧循环 ⇒ 手动推一次
            CheckNear(scaleRoot.transform.localScale.x, m, 1e-4f,
                      "（前提）" + what + "：态二那一级 `localScale` = M（真走的生产那条路）");
            // （前提①）**被除的那一级真的被乘了 M** —— `PosInDesignSpace` 除的是 `basis.parent.lossyScale`；
            // 那一级是单位缩放时新旧两式**逐位相同** ⇒ 下面那条 ★ 等于没查。改坏法：M 仍加在 `basis` 自己身上
            // （= 上一版那种塞法）⇒ 这条红。
            CheckNear(basis.parent != null ? basis.parent.lossyScale.x : 1f, m, 1e-3f,
                      "（前提）" + what + "：**基准的【父级】在态二被乘了 M**（`PosInDesignSpace` 除的正是这一级，"
                    + "`Shell/MenuDraw.cs:74-78`）—— 父级单位缩放时新旧两式**恒等**，这条断言就等于没查");
            Vector3 p2 = measure();
            CheckNear(p2.x, m * p1.x, 0.02f,
                      $"★ {what}：**态二 == M × 态一**（x：{p2.x:F3} vs {m:F2}×{p1.x:F3}）"
                    + " —— 两态合起来才证明「这一处的落位真的跟着被乘 M 的那一级缩放走」"
                    + "（`k == 1` 时新旧两式逐位相同 ⇒ 只断态一的话，改坏了照样绿）");
            CheckNear(p2.y, m * p1.y, 0.02f, "★ " + what + "：……y 分量同理（只改 x 不改 y 时只有上一条红）");
            Object.DestroyImmediate(sc);                           // 还原
            scaleRoot.transform.localScale = Vector3.one;
            SmallScreenUI.Set(false);
            CheckNear(measure().x, p1.x, 0.02f, "（收尾）" + what + "：那一级放回 1 之后位置也回到态一那一份");
        }

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
            if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（按已知情况放行）");
            else CheckTrue(lum > 3f, $"{file} **不是空图**（平均亮度 {lum:F1} > 3）");
            Object.DestroyImmediate(tex);
            RenderTexture.ReleaseTemporary(rt);
            Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
        }

        static float MeanBrightness(Texture2D t)
        {
            if (t == null) return 0f;
            var px = t.GetPixels32();
            if (px.Length == 0) return 0f;
            long sum = 0;
            for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b;
            return sum / 3f / ((px.Length + 6) / 7);
        }

        // ============================================================ 建场景

        static CollectionWindow Build(out Transform root)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);

            // 🔴 **2026-10-11（A351）就地订正（铁律 5）**：这里原来**手抄了第三份**「三颗 Holder +
            //    `AddComponent<WindowsManager>()`」—— 那套手抄在**批处理（编辑模式）**下有一处硬伤：
            //    · `WindowsManager` **没有 `[ExecuteAlways]`**（`Shell/WindowsManager.cs` 里**只有** `WindowHolder` 那颗**有**）
            //      ⇒ `Awake` 不跑 ⇒ **`Instance` 恒 null**（`Instance` 只在 `Awake` 里赋，
            //      **批处理下那句从不执行**）。判据（**四条独立记录**，全是踩过的坑）：
            //      `Shell/PromptPopup.cs:868` · `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 · `Shell/PointerLayer.cs:47-48`
            //      · `资料/已知的坑.md:704`（「编辑模式下 `Awake/OnEnable`/`Update` **只对带 `[ExecuteAlways]`
            //      的脚本**才跑」）。
            //    · ⇒ **任何走 `WindowsManager.EnsureHost()` 的开窗路径都会【再建一台】**（它在 `Instance == null`
            //      时不查「场景里是不是已经有一台」、直接再建一套管理器 + 锚点）
            //      ⇒ 窗落进**第二台**，而 `wm` 是第一台 ⇒ `wm.openWindows` 里没有它、读
            //      `WindowsManager.Instance` 的代码（如 `Shell/DailyData.cs` 里读 `WindowsManager.Instance` 的那一处）**静默**拿到另一台。
            //      2026-10-11 的那 8 条红（`RewardsScene` 十一条里的 #1 及其级联）就是**这一处形状**造成的，
            //      判据全文 → `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1；A351 = 剩下这几处一起收口。
            //    **最小改法 = 走公共件**：`EnsureHost` 里那句 `Instance = wm` 就是为此加的；
            //    而 `EnsureHost()` 的文档注释白纸黑字写着「壳（`ShellRuntime`）与「单独打开某个界面场景
            //    按 Play」**都走它**，两处各建一次 = 迟早不一致（CLAUDE.md §三）」。
            //    ⚠️ 与改前的场景层级**一致**：`EnsureHost()` 建的 Holder 名 / placement 与手抄那份**逐字相同**
            //      （同一个 `MakeHolder` 形状：`1 - Below Upper Bar Holder` / `2 - Canvas Holder Above upper bar` /
            //      `3 - PopUp Holder`，三颗都齐、都在场景根），且 `root == null` ⇒ 同样没有父。
            //    ⚠️ 顺带把 `PointerLayer` 的创建时机提前到 `Build()` 那一刻（`EnsureHost` 第一句就是
            //      `PointerLayer.Ensure(root)`）—— **已核：无可观测差异**（两边都是无父的
            //      `new GameObject("Pointer Layer")`；`RegisterScroll` 读的是惰性 getter
            //      ⇒ 登记表内容一字不变。同 `RewardsScene` 那条，见 `资料/普查产出_1011/FX4_Rewards十一条红修复.md` §六·6）。
            //    **改坏法（如实说 —— 今天【照不出来】，它是一笔【去掉地雷】的改动，⛔ 不是「修好了一条会红的断言」）**：
            //      把这一句换回手抄的 `AddComponent<WindowsManager>()` ⇒ `Instance` 又变回 null；而**本自检今天没有**
            //      走 `WindowsManager.EnsureHost()` / `OpenByRef()` 的开窗入口（现场全部是直调 `win.Manager.OpenWindow(...)`）
            //      ⇒ **改坏它，本文件一条断言都不会红**。它的判别力在【将来】：`WindowsManager.OpenByRef()` 的**第一句**
            //      就是 `EnsureHost()` —— 谁在这几扇窗里接一条走它的入口（`BattleLogTab` / `LeaderboardRow` 那一族就是
            //      这么接的），第一次跑就会**另建一台管理器 + 第二套锚点**、窗落进第二台 ⇒ 现象与判据 →
            //      `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1（`RewardsScene` 那 8 条红就是同一个形状）。
            var wm = WindowsManager.EnsureHost();      // 它自己建 "Window Anchors" + 三颗 Holder + 管理器，并**登记 `Instance`**

            var win = CollectionWindow.Create(wm);
            wm.OpenWindow(win);
            root = win.transform;
            return win;
        }

        /// <summary>⚠️ **2026-10-11（A351）起 `Build()` 不再调它** —— 那三颗 Holder 现在由
        /// `WindowsManager.EnsureHost()` 建（同一个形状、名字与 placement 逐字相同，见 `Build()` 里那段订正）。
        /// **它留着不删**：这是「单独打开某个界面场景」那条路的**形状存档**（同形手抄全仓原有 4 处，A351 全收口）
        /// —— 留着比删掉更能让下一个会话看出「原来长什么样」。⛔ 新代码别调它。
        /// ⚠️ 它**不是** `WindowsManager` 里那份同名私有件（那份在 `Shell/WindowsManager.cs` 里是 `static` 私有、复用不了）
        /// —— 这正是当年四处各抄一份的来由。</summary>
        static void MakeHolder(Transform parent, string name, WindowsPlacement p)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var h = t.gameObject.AddComponent<WindowHolder>();
            h.placement = p;
            h.RegisterNow();
        }

        // ============================================================ 自检

        public static void Run()
        {
            _pass = 0; _fail = 0; _failures.Clear();
            Directory.CreateDirectory(ShotDir);

            // 自检**不碰玩家的真存档**：`DeckStore.OverridePath` 指到临时文件，并**先造 14 套卡组**
            //（14 套 = 3 行 > 视口 2.4 行 ⇒ 列表**滚得动**，才验得到滚动那条）
            DeckStore.OverridePath = TestDeckFile;
            if (File.Exists(TestDeckFile)) File.Delete(TestDeckFile);
            CollectionData.ResetForTest();
            {
                var lib = DeckLibrary.Load();
                for (int i = 0; i < 14; i++) lib.Create("测试卡组 " + (i + 1));
                lib.Save();
            }
            CollectionData.ResetForTest();
            int fixtureCards = 0;      // 夹具那副的卡数（下面填；断言放在「开始」横幅之后，别抢在标题前面打印）
            // 🔴 给第 1 套塞**真卡** —— `Deck info Popup` 的 `Deck List` 要有东西可画
            //   （`DeckLibrary.Create` 只建空壳，卡组里 0 张卡 ⇒ 第一版那条「画了 N 行」量到 0）
            // 🆕 **2026-10-05 改成【一副合法的 30 张卡组】**（原来只塞督军 + 3 张）。
            //   为什么：`Practice Deck` 那颗钮的 `interactable = DeckUtility.ValidateDeck(deck, …)`
            //   （原版 `DeckInfoControls__Initialize:201-207`；我们这一侧**同一份判据** = `RuleEngine.DeckRules.Validate`）
            //   只在**卡数正好 30** 时为真。3 张 ⇒ `DeckError.TooFewCards` ⇒ 那颗钮被闸门挡掉
            //   ⇒ 本文件 A10 尾巴那一整段（`dp.CurrentState` 变 `Closed` / `DeckSelectionPopup.LastOpened`
            //   / 隐藏卡那一支）**全是假红**（点了什么都不发生）。
            //   ⚠️ 「正好 30」的出处：原版 `GameStaticData.deckSize`，我们这一侧住在
            //   `GameplayVariables.Classic.deckSize`（判据入口 `DeckRules.CardCount(false)`）。
            //   写法与 `Editor/MainMenuScene.cs:416-440`（A65④① 的**阳性对照**那一副）**逐字一致**：
            //   同阵营 + 一张一名（⇒ 不碰⑦同名上限）+ 只取 `unit`（⇒ 不碰⑥督军混入、⑥b 效果卡）。
            {
                var lib = DeckLibrary.Load();
                if (lib.Decks.Count > 0)
                {
                    var pool = CardDatabase.Load();
                    var d0 = lib.Decks[0];
                    d0.CardIds.Clear();
                    // 阵营取卡池里 unit 最多的那个（实测 `RuleEngine/Resources/cards_engine.json`：Ultramarines **69** 张）。
                    // ⚠️ 这里是**写死的**、不现算「哪个阵营够 30」—— 那种现算等于把判据搬进自检。
                    // 真凑不满 30 张时下面那条 `Check` 会**红**（不静默变绿）。
                    const string LegalFaction = "Ultramarines";
                    foreach (var c in pool)
                        if (c.Type == "hero" && RuleEngine.DeckRules.SameFaction(c.Faction, LegalFaction))
                        { d0.WarlordId = c.Id; break; }
                    foreach (var c in pool)
                    {
                        if (d0.CardIds.Count >= 30) break;
                        if (c.Type != "unit") continue;
                        if (!RuleEngine.DeckRules.SameFaction(c.Faction, LegalFaction)) continue;
                        if (d0.CardIds.Contains(c.Id)) continue;   // 一张一名 ⇒ 不碰同名上限
                        d0.CardIds.Add(c.Id);
                    }
                    fixtureCards = d0.CardIds.Count;
                    lib.Save();
                }
            }
            CollectionData.ResetForTest();
            CollectionWindow.ResetStylesForTest();      // 异画那批卡的筛选状态（静态缓存）
            CardProgress.ResetForTest();                // 卡片详情窗的拥有数/等级（单机口径，静态缓存）
            Debug.Log(P + "=== 「收藏线」自检 开始 ===");

            // 🔴 **2026-10-05 新增**：夹具那一副（第 1 套）必须是**合法**的 30 张卡组。
            //   它是本文件 A10 尾巴整段的**前提**，不是可选项 ⇒ 必须钉住（缺了它那一段的
            //   「点 `Practice Deck` ⇒ 关自己 + 开选卡组窗」会**静默**变成「点了什么都不发生」）。
            //   期望值 **30** 出自原版 `GameStaticData.deckSize`（不是抄上面那个循环的上限）。
            Check(fixtureCards, 30,
                  "自检夹具：第 1 套 = **Ultramarines 督军 + 30 张同阵营不同名 unit**"
                  + "（原版 `GameStaticData.deckSize = 30`）—— A10 尾巴那一整段的阳性前提");

            var win = Build(out var root);

            // ---------------- 窗口参数（原版 MB `-6401214277658680619`）----------------
            Section("窗口参数（正本 §一；`Collection Menu Variant` 根 MB 原文）");
            Check(win.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（原文）");
            Check(win.placement, WindowsPlacement.Canvas,
                  "`windowsPlacement` = **5 Canvas**（⚠️ **商店是 10 World** —— 逐窗不同，别互推）");
            Check(win.closeOnEsc, true, "`closeOnESC` = **1**（⚠️ **奖励窗是 0**）");
            CheckNear(win.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

            // ---------------- 外壳（与奖励窗/商店**同一个壳**）----------------
            Section("外壳：`Content Area` / 左栏 / 四页签（正本 §二）");
            var area = FindChild(root, "Content Area");
            CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
            CheckRectPx(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");

            var bar = FindChild(root, "Tab Buttons");
            CheckAt(bar, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`（左栏）");
            var keys = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("CollectionTabButton_")) keys.Add(t);
            Check(keys.Count, 4, $"左栏**四个键**（实测 {keys.Count}）");

            // 🔴 左栏第一个键的顶边 = **188.64**（条从 158.64 起 + VLG padTop 30）
            //    —— 这是本窗与奖励窗/商店**唯一的外壳差别**（那两个是 70.94 + 120 = 190.94）
            if (keys.Count >= 4)
            {
                var want = LayoutSpace.RectCenter(167.17f, 188.64f, 332.17f, 368.64f);
                float d = Vector3.Distance(keys[0].position, want);
                CheckTrue(d <= 0.01f,
                          $"**第一个键的矩形** = 167.17,188.64→332.17,368.64（原版；差 {d * 108f:F2}px）"
                          + " —— 本条是「`BarPadTop` 覆写成 117.7」的判据");
            }

            // ---------------- 四页签：**卡面文案不是页节点名** ----------------
            Section("四页签：文案 / 切页（正本 §二；A1 表 `:17-22`）");
            string[] wantLabels = { "DECKS", "CARDS", "COSMETICS", "STYLES" };
            for (int i = 0; i < 4 && i < keys.Count; i++)
            {
                // ⚠️ 键上的字是 `Text`；**原版 `m_fontStyle = UpperCase`**（基类已 `ToUpperInvariant`）
                var lab = FindChild(keys[i], "Text");
                CheckText(TextOf(lab), wantLabels[i],
                          $"左栏第 {i + 1} 键的文案 = **{wantLabels[i]}**"
                          + (i == 2 ? "（⚠️ 页节点叫 `Cardback Tab`，**卡面印的是 Cosmetics**）" : "")
                          + (i == 3 ? "（⚠️ 页节点叫 `Alternate Art Tab`，**卡面印的是 Styles**）" : ""));
            }
            // 🆕 **2026-10-08（波 C3 · A212 主表 #31 验收）：左栏键文案的【渲染】断言**（四窗这一族原来一条都没有）。
            //   判据 = 原版四窗左栏键文案的 `m_TextWrappingMode` **一律 `0`（`NoWrap`）**—— 逐窗现读的四窗表
            //   只写一处：`Shell/MenuWindowBase.cs` 的 `BuildTabButton`（此处不抄第二份，铁律 6）。
            //   ⛔ **为什么必须量渲染、不能只比字号**：`SetAutoFitBox` 只把 `fontSizeMin/Max` 交出去，
            //   **装不装得下由 TMP 算** ⇒ 字号对而字冲出去，自检照样全绿（`AutoFitBox` 那条教训，2026-09-22 踩过）。
            //   框宽 **155** = 原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`
            //   （⛔ 不读 `BuildTabButton` 的 `labW` —— 那也是被测实现里的数，读了就是自证）。
            //   **改坏法**：删掉 `MenuWindowBase.BuildTabButton` 末句 `txt.SetWrapping(false)` ⇒
            //   `SetAutoFitBox` 开出来的 `Normal` 留着 ⇒ `折行=` 那条立刻红（0 → 1）。
            //   ⚠️ 本窗四颗键是 `DECKS`/`CARDS`/`COSMETICS`/`STYLES` —— **都不带空格**
            //   ⇒ 「就一行」那条在这里的红法要 `auto` 缩不动才轮到（真正当场能红的是 `折行=` 那条）。
            {
                int tabN = 0;
                for (int i = 0; i < 4 && i < keys.Count; i++)
                {
                    var klb = FindChild(keys[i], "Text") != null
                        ? FindChild(keys[i], "Text").GetComponentInChildren<Label>() : null;
                    CheckTrue(klb != null, $"（左栏渲染断言 · 前提）第 {i + 1} 键的文案 `Label` 取得到");
                    if (klb == null) continue;
                    tabN++;
                    CheckTrue(klb.WrappingMode >= 0,
                              $"（左栏渲染断言 · 前提）第 {i + 1} 键 `{klb.Text}` 走的是 **TMP 后端**"
                            + "（`-1` = 点阵后端 ⇒ 下面两条渲染断言不成立，如实红、不假装）");
                    Check(klb.WrappingMode, 0, $"★ 第 {i + 1} 键 `{klb.Text}`：**`折行=0`**（原版四窗左栏键一律 0）");
                    Check(klb.LineCount, 1, $"★ …而且渲出来**就一行**（`Normal` 会把装不下的键名折行）");
                    CheckTrue(klb.WorldW * 108f <= 155f + 0.5f,
                              $"★ …而且**渲出来的宽 {klb.WorldW * 108f:F1} ≤ 框宽 155**"
                            + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
                }
                Check(tabN, 4, "四颗键的文案都量到了（少于 4 ⇒ 上面那几条等于没查）");
            }
            var tabsRoot = FindChild(root, "Tabs");
            CheckTrue(FindChild(tabsRoot, "Select Deck Tab") != null, "页节点 `Select Deck Tab` 在（**不叫 `Deck Tab`**）");
            // 🔴 **2026-10-12（A288）新增**：第 2 页的节点名是 **`CardsTab`** —— 我们原来建的是
            //   `"Card Collection Tab"`（**把脚本类名 `CardCollectionTab` 当成节点名**了）。
            //   原版实据：`bundle_menus_assets_all/GameObject/CardsTab.json` 的 `m_Name = "CardsTab"`。
            //   ⚠️ **这一条是「按名字取」才有的检出能力**：`FindChild` 是 `GetComponentsInChildren` + `name ==`
            //   ⇒ 名字不对就是 `null` ⇒ 红（下面 `:2680` 那条在切页路径上再钉一次）。
            CheckTrue(FindChild(tabsRoot, "CardsTab") != null,
                      "页节点 **`CardsTab`** 在（⚠️ **不叫 `Card Collection Tab`** —— 那是脚本类名 `CardCollectionTab`，"
                    + "节点名只有这一页与前三个不同名；判据 = `…/GameObject/CardsTab.json` 的 `m_Name`）");
            CheckTrue(FindChild(tabsRoot, "Cardback Tab") != null, "页节点 `Cardback Tab` 在");
            CheckTrue(FindChild(tabsRoot, "Alternate Art Tab") != null, "页节点 `Alternate Art Tab` 在");
            // 🆕 **2026-10-09（A264）**：`Tabs` 底下还有一层 **`Shared`**（原版**只有收藏窗**有这一层），
            //   关闭钮「Back」连它下面的 `Button Text` 都装在这一层里面。
            //   🔴 **判据就是【层级】本身 ⇒ 只能按【路径】取**：`FindChild` 是 `GetComponentsInChildren` + `name ==`
            //      （**递归按名字找**），父链读错的那一版（`Close Button` 直接挂窗口根）**照样能找得到**
            //      ⇒ 拿它写这条等于**分不出「挪进 `Shared`」与「留在窗口根」两种状态**（弱断言）。
            //   🔴 **期望值全是原版字面量**（⛔ 不读 `CollectionWindow.CloseBtnL` 那些实现常量 —— 那是自证）：
            //     · 父链（**逐级现读 `m_Father`**，⛔ 不是 `menu_dump` 的缩进；A264 立项时那条「`Shared` 是
            //       `Content Area` 的**兄弟**」就是这么读错的）：
            //       `Collection Menu Variant` → `Content Area` → `Tabs` → `Shared` → `Close Button` → `Button Text`
            //     · `Shared` 那颗 RT 是**拉伸锚**（`a=(0,0)-(1,1)` · `sd=(0,0)` · `ap=(0,0)`）
            //       ⇒ 矩形 = 父件 `Tabs` 的矩形 = **167.17,70.94 → 1920.01,1080（1752.83 × 1009.06）**
            //     · 它的 GO（`bundle_menus_assets_all/GameObject/Shared.json`）`m_Component` **只有 1 项**（= 那颗 RT）
            //       ⇒ **纯容器节点**（没有 `ImageQuad`）⇒ 只量位置，⛔ 别用 `CheckRectPx`（那个要 `ImageQuad`）
            //     · `Close Button` 的原版矩形 = **192.17,83.44 → 342.17,143.44**（150×60）
            //   **改坏法**：① 不建 `Shared`、把 `Close Button` 挂回窗口根（A264 之前那一版）⇒ 下面三条路径**全取不到** ⇒ 红；
            //             ② 建了 `Shared` 但 `Close Button` 仍留在窗口根 ⇒ 第 2 条取不到 ⇒ 红。
            {
                var shared = root != null ? root.Find("Content Area/Tabs/Shared") : null;
                CheckTrue(shared != null,
                          "★ `Shared` 在 **`Tabs` 的直接子件**上（按路径 `Content Area/Tabs/Shared` 取得 —— "
                          + "`Transform.Find` 逐段只找直接子件；原版父链 = `Collection Menu Variant` → `Content Area` → `Tabs` → `Shared`）");
                var shClose = shared != null ? shared.Find("Close Button") : null;
                CheckTrue(shClose != null,
                          "★ `Close Button` 装在 **`Shared` 之下**（原版那颗 `Shared` 的 `m_Children` **只有它一个**）");
                CheckTrue(shClose != null && shClose.Find("Button Text") != null,
                          "★ …而 `Button Text` 又在 **`Close Button` 之下**（原版那条六级链的最末一节）");
                // `Shared` = **纯容器**（原版那颗 GO 上只挂着 RT 一个组件，没有 `Image` / 没有 `RectMask2D`）
                //   ⇒ 只量位置。「1752.83×1009.06」这条边由**拉伸锚 + 父件同矩形**推出来（我们不建图 ⇒ 量不到宽高）。
                CheckAt(shared, 167.17f, 1920.01f, 70.94f, 1080f,
                        "`Shared` 摆在原版那个矩形里（= 父件 `Tabs` 的矩形 167.17,70.94→1920.01,1080 · **1752.83×1009.06**）");
                // 🔴 本件最该防的回归：**换父链把世界矩形一起带偏了** —— `MenuWindowBase.Node/Rect` 走的是
                //   `Local(parent, …)`（世界 − 父的世界位置）⇒ 理论上逐位不变，这里把它钉住。
                CheckAt(shClose, 192.17f, 342.17f, 83.44f, 143.44f,
                        "★ `Close Button` **挪进 `Shared` 之后世界矩形没动**（原版 192.17,83.44→342.17,143.44 = 150×60）");
            }
            Check(win.CurrentTab, WindowTabType.CollectionDecks, "开窗默认落在第 1 页（Decks）");

            // ---------------- Deck 页：视口 / 格 / 6 列 ----------------
            Section("Deck 页：卡组列表的几何（正本 §三；A2 实测）");
            var listHolder = FindChild(tabsRoot, "Deck Scroll View");
            CheckAt(listHolder, 330.9f, 1920f, 155.9f, 1080f, "`Deck Scroll View`（视口）");
            int n = CollectionData.DeckCount();
            CheckTrue(n > 0, $"卡组列表里有 {n} 套卡组（自检用的临时存档）");

            var cells = win.DeckCells;
            CheckTrue(cells.Count > 0, $"画出了 {cells.Count} 格（视口外的不建 = `RectMask2D` 那套裁切）");
            if (cells.Count > 0)
            {
                var c0 = cells[0];
                CheckNear(PxOf(c0.position.x), 453.4f, 0.6f,
                          "第 1 格中心 x = **453.4**（视口左边 330.9 + padL 10 + 225/2）");
                CheckNear(PxYOf(c0.position.y), 338.15f, 0.6f,
                          "第 1 格中心 y = **338.15**（视口顶 155.9 + 364.5/2）");
                var frame = FindChild(c0, "Frame");
                CheckNear(Wpx(frame), 246f * 0.9f, 2f, "格内卡框宽 = **221.4**（原版 246 × 显示比例 0.9）");
                CheckNear(Hpx(frame), 368f * 0.9f, 2f, "格内卡框高 = **331.2**（原版 368 × 0.9）");
                CheckArt(frame, "40K_bt_deck", "卡框的图 = `40K_bt_deck`（原版）");
                CheckTrue(!string.IsNullOrEmpty(TextOf(FindChild(c0, "Deck Name"))), "格上有卡组名");
                // 6 列：第 6 格右边界 = 340.9 + 5×245 + 225 = 1790.9 ≤ 1920
                if (cells.Count >= 6)
                    CheckNear(PxOf(cells[5].position.x) + DeckCellWpx() / 2f, 1790.9f, 0.6f,
                              "第 **6** 格右边界 = **1790.9** ≤ 1920（**这是「6 列」的判据** —— 7 列要 2035.9）");
            }

            // ---- 左抽屉 `Deck Filters`（§三 第 15 条 **第 49 行**；2026-09-24 建）----
            // 出处：`menu_rect.py bundle_menus_assets_all "Deck Filters" --depth 6`（直读）。
            {
                var fh = FindChild(win.PageRoot(0), "FiltersHit");
                var fw = fh != null ? fh.GetComponent<WindowButton>() : null;
                CheckTrue(fw != null, "`Filters` 圆钮**有命中区**了（原来没有 ⇒ 玩家点它没反应）");
                var dflt = FindChild(win.PageRoot(0), "Deck Filters");
                CheckTrue(dflt != null, "左抽屉 `Deck Filters` 建了（原版这一页有抽屉）");
                if (dflt != null)
                {
                    // 🔴 **2026-10-04 订正（X3 审查的 R3）：这条位置断言原来是在「收着」的时候量的**，
                    //   A11 之后「收起」= 整栏**真的滑出去** —— 原版 `SetupFilters` 就是这么干的：
                    //   记下 `originalAnchorPosition` 之后立刻 `anchoredPosition = (hiddenPosition.x, …)`
                    //   （`资料/卡组编辑界面_查证_0920.md:433-434` 的 VA 反汇编）⇒ **关着时它已经不在原位**。
                    //   改法：**两态各量一条**（关着 = 真的滑出屏幕 · 开着 = 停在原版矩形），⛔ 不是把断言改软。
                    CheckNear(CollectionWindow.DeckFltView.W, 335.50f, 0.6f, "抽屉宽 **335.50**（原版）");
                    // 🆕 2026-10-05（A101）升格：这条**不再是我们挑的** —— 原版 `Deck Filters` 的 `m_IsActive`
                    //   实读 = **`T`**（`资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 的实读表 Deck 那一行），
                    //   起手停在哪一头由 `SetupFilters` 定（`0x1815F0740` 收尾 = `anchoredPosition =
                    //   (hiddenPosition.x, originalAnchorPosition.y)`）⇒ **起手收起**，与 Cards 页**同一套**。
                    //   🔴 `act = T` **只等于「节点启用」**，⛔ 别读成「出厂展开」（`资料/已知的坑.md` 2026-10-05 那节）。
                    CheckTrue(!dflt.gameObject.activeSelf,
                              "起手收起（**照原版**：`SetupFilters` 把抽屉摆到 `hiddenPosition` 那一头）");
                    CheckNear(win.DrawerSlide(3), 0f, 0.001f,
                              "🆕 A11：Deck 页这一列也走**同一套滑动**，收起 = 进度 **0**（不只在原位隐身）");
                    float dCx0 = PxOf(dflt.position.x);
                    CheckNear(dCx0, 167.81f - 385f, 1.5f,
                              "…收起 = 按**原版行程 −385px** 滑出去（`hiddenPosition.x − originalAnchorPosition.x`"
                              + " = −550 −(−165)；原位中心 167.81 → −217.19）—— 有洞的实现在这里就红");
                    CheckTrue(dCx0 + CollectionWindow.DeckFltView.W * 0.5f < 0f,
                              "★ …而且整栏**真的在屏幕左外**（右缘 "
                              + (dCx0 + CollectionWindow.DeckFltView.W * 0.5f).ToString("F1")
                              + " < 0）—— 这才叫「收起」");
                    // ★ **R4 的判据**：关着时那一列的命中区必须**已经是关的**。
                    //   ⚠️ 量的是 `WindowButton.enabled` 的**真值**、不是我们自己那个布尔（那个是自证）——
                    //   真命中路 `PointerLayer.CollectHits` 只挑 `isActiveAndEnabled`（`Shell/PointerLayer.cs:488`）。
                    //   原来 Deck 这一份**不走** `RebuildFilterRows`（格子自己 `AddHit` 建）⇒ 少了那次 `force: true`，
                    //   建出来的按钮停在默认 `enabled=true`，Play 里第一次滑出来的 0.3 秒真鼠标点得到
                    //   （被 R1 的 108 倍位移掩盖着，R1 修完才现形）。
                    var dwbs = dflt.GetComponentsInChildren<WindowButton>(true);
                    int dOn0 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn0++;
                    CheckTrue(dwbs.Length > 0 && dOn0 == 0,
                              $"★ 起手收起 ⇒ 抽屉里 **{dwbs.Length}** 个命中区**全部 `enabled=false`**"
                              + $"（实测还开着 **{dOn0}** 个 —— 这一条是「第一次滑出来的 0.3 秒点得到」的判据）");
                    if (fw != null) fw.Click();
                    CheckTrue(dflt.gameObject.activeSelf, "点 `Filters` ⇒ 抽屉打开（原版这颗钮开的就是它）");
                    CheckNear(win.DrawerSlide(3), 1f, 0.001f, "🆕 A11：打开 = 进度 **1**（停在原位）");
                    // 「停在原版矩形」这一条原来在关着的时候量（见上面那段订正）⇒ 挪到**开着**的时候
                    CheckAt(dflt, 0.06f, 335.56f, 155.94f, 1080f,
                            "…打开后**停在原版矩形**中心（A11 起这条只能在开着时量）");
                    int dOn1 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn1++;
                    CheckTrue(dwbs.Length > 0 && dOn1 == dwbs.Length,
                              $"★ …而到位后 **{dwbs.Length}** 个命中区**全回来**（实测 {dOn1} 个）—— "
                              + "与上面那条正好两态对比：写成恒开/恒关都过不了这两条");
                    var a0 = FindChild(dflt, "Cell_fac_" + CampaignData.Armies[0]);
                    CheckTrue(a0 != null,
                              $"`Army Filter` 里 **{CampaignData.Armies.Length}** 个阵营格建了"
                              + $"（第 1 个 = {CampaignData.Armies[0]}）");
                    // 正例：筛第 1 套卡组的阵营 ⇒ 只剩它那几套；反例：筛一个没有卡组的阵营 ⇒ 空 + 提示亮
                    string fac0 = CollectionData.DeckAt(0).Faction;
                    CheckTrue(!string.IsNullOrEmpty(fac0), "第 1 套卡组推得出阵营（筛选用）");
                    int nAll = win.DeckCellCount;
                    win.ToggleDeckFacFilter(fac0);
                    Check(win.DeckFacFilter, fac0, $"点阵营格 ⇒ 筛选条件 = {fac0}");
                    CheckTrue(win.DeckCellCount > 0 && win.DeckCellCount <= nAll,
                              $"筛「{fac0}」⇒ 卡组格 {nAll} → {win.DeckCellCount}（真筛得动）");
                    string other = "";
                    foreach (var a in CampaignData.Armies) if (a != fac0) { other = a; break; }
                    win.ToggleDeckFacFilter(fac0);          // 先取消
                    Check(win.DeckFacFilter, "", "再点同一格 ⇒ 取消（与 Cards 页 `$fac:` 同一条手感）");
                    win.ToggleDeckFacFilter(other);
                    Check(win.DeckCellCount, 0, $"筛一个**没有卡组**的阵营（{other}）⇒ 一格都不剩");
                    CheckTrue(DeckEmptyShown(win), "……而且 `Empty Collection Warning` **亮起来**（原版判据：过滤后为空）");
                    win.ClearDeckFilters();
                    Check(win.DeckFacFilter, "", "`Clear filters` ⇒ 筛选清掉");
                    Check(win.DeckCellCount, nAll, $"……卡组格回到 {nAll}");
                    CheckTrue(!DeckEmptyShown(win), "……而且那条提示关回去");
                    if (fw != null) fw.Click();
                    CheckTrue(!dflt.gameObject.activeSelf, "再点 `Filters` ⇒ 抽屉收起");
                    CheckNear(win.DrawerSlide(3), 0f, 0.001f, "…而且进度回到 **0**（A11：收 = 滑出去）");
                    int dOn2 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn2++;
                    CheckTrue(dwbs.Length > 0 && dOn2 == 0,
                              $"…收起到位 ⇒ 命中区**又关回去**（实测还开着 {dOn2} 个）—— "
                              + "三态（收起 / 打开 / 再收起）走的是同一份实现");
                }
            }

            // `Empty Collection Warning`（**Deck 页那一份**）—— 原版**四个页各有一份、矩形各不相同**，
            // 逐页的实读值写在 `CollectionWindow.BuildDeckList` 那条注释里。
            {
                var ew = FindChild(win.PageRoot(0), "Empty Collection Warning");
                CheckTrue(ew != null, "Deck 页有 `Empty Collection Warning`（原版四页各一份）");
                if (ew != null)
                {
                    CheckAt(ew, 165.88f, 1970.01f, 70.94f, 1080f, "Deck 页 `Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf,
                              "有卡组 ⇒ 不显示（判据 = `CollectionData.DeckCount() <= 0`）");
                }
            }

            // ---------------- 交互：点一格 / 点 Create / 滚动 ----------------
            Section("交互（批处理直调，与真点同一条 `WindowButton.onClick`）");
            int before = CollectionData.CurrentIndex();
            int pick = (before + 1) % Mathf.Max(1, CollectionData.DeckCount());
            if (cells.Count > pick)
            {
                var hit = FindChild(cells[pick], "Hit");
                var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                CheckTrue(wb != null && wb.onClick != null, "格子上的点击区挂到了 `WindowButton`");
                if (wb != null) wb.Click();
                Check(CollectionData.CurrentIndex(), pick, "点第 " + (pick + 1) + " 格 ⇒ 当前卡组换成它");
                // 🔴 **2026-09-23 换路**：原来这里是「**再点一下同一格** = 进编辑」—— 那是 `Deck info Popup`
                //    还没建时的**顶替路**（当时就出声了）。现在那扇窗建好了 ⇒ 改验**原版那条路**：
                //    点格 ⇒ 开窗 ⇒ 窗里点 `Edit Deck` ⇒ 才交接下标。
                var pop = CollectionWindow.LastOpened;
                CheckTrue(pop != null, "点一格卡组 ⇒ **开出了 `Deck info Popup`**（原版那条路）");
                Check(pop != null ? pop.DeckIndex : -1, pick, "弹窗收到的是**这一格**的下标");
                CheckTrue(pop == null || pop.CurrentState == WindowState.Open, "弹窗进了 `Open` 态");
                CollectionData.PendingEditDeck = -1;
                var eBtn = pop != null ? pop.Btn("Edit Deck") : null;
                var eWb = eBtn != null ? eBtn.GetComponent<WindowButton>() : null;
                CheckTrue(eWb != null, "窗里 `Edit Deck` 有点击区");
                if (eWb != null) eWb.Click();
                Check(CollectionData.PendingEditDeck, pick,
                      "窗里点 `Edit Deck` ⇒ 交接给 `DeckRuntime` 的下标 = " + pick
                      + "（「从收藏进编辑」的判据；真机上这一步会 `LoadScene(\"DeckEditor\")`）");
                if (pop != null) pop.Close();
            }
            var createHit = FindChild(tabsRoot, "CreateHit");
            var createBtn = createHit != null ? createHit.GetComponent<WindowButton>() : null;
            CheckTrue(createBtn != null, "`Create` 钮有点击区");
            if (createBtn != null)
            {
                int n0 = CollectionData.DeckCount();
                createBtn.Click();
                Check(CollectionData.DeckCount(), n0 + 1, "点 `Create` ⇒ 卡组数 +1（原版走 `Deck Editing Menu`，本轮只建卡组）");
            }
            // 🔴 **实拍抓的一条**：`Clear filters` 原来摆在 1218.6（**我们推错的**）⇒ 它和 `Import` 压掉 77.6px。
            //    真值 = A2 §161 的**实测落点 612.2**（容器宽 0 + 子件 `m_IgnoreLayout=1`）。
            {
                var cf = FindChild(tabsRoot, "Clear filters Text");
                var il = FindChild(tabsRoot, "Import Text");
                var lcf = cf != null ? cf.GetComponent<Label>() : null;
                var lil = il != null ? il.GetComponent<Label>() : null;
                if (lcf != null && lil != null)
                {
                    float cfR = PxOf(lcf.transform.position.x) + lcf.WorldW * 54f;
                    float iL = PxOf(lil.transform.position.x) - lil.WorldW * 54f;
                    CheckTrue(cfR < iL,
                              $"`Clear filters`（右缘 **{cfR:F0}**）与 `Import Deck`（左缘 **{iL:F0}**）**不叠**"
                              + " —— 判据是 A2 §161 的实测落点 612.2，**别自己按容器推**");
                }
                CheckText(TextOf(FindChild(tabsRoot, "Import Text")), "Import Deck", "`Import` 钮文案（原版 `Import Deck`）");
                CheckText(TextOf(FindChild(tabsRoot, "Create Text")), "Create Deck", "`Create` 钮文案（原版 `Create Deck`）");
            }
            // 🔴 **2026-10-05（A89）补：`Control Buttons` 的三颗视觉序** —— 原版那个 HLG 是
            //    **`m_ReverseArrangement = 1`**（spacing 25 · `pad.right` 14 · align `MiddleRight` · `expandW=1`）
            //    ⇒ **树序 `[Create, Import, Unlock]` 倒排** ⇒ 视觉左→右 = `Unlock` **1180.00** →
            //    `Import` **1391.00** → `Create` **1661.01**（组矩形 1179.99,80.94 → 1920.01,140.94）。
            //    判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`；跑后矩形 =
            //    `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 20 --md`。
            //    ⚠️ 这与 `Shell/CollectionWindow.cs` 的 `CreateX = 1661` / `ImportX = 1391` **逐位吻合**
            //    （实现本来是对的，那段注释写成了镜像，已同批订正）。
            //    🔴 量的是**节点的世界 x**（`CreateHit`/`ImportHit`），不是常量 ——
            //       断常量 = 同义反复；镜像摆法会把这一条连同上面那条 `Clear filters` 不叠一起打红。
            //    ⚠️ `MenuDraw.Hit` 的**节点摆在父原点、quad 在矩形中心**（`MenuDraw.cs` `Hit` 的注释）
            //       ⇒ 必须量 `GetComponentInChildren<ImageQuad>()`。
            {
                var chq = FindChild(tabsRoot, "CreateHit");
                var ihq = FindChild(tabsRoot, "ImportHit");
                var cq = chq != null ? chq.GetComponentInChildren<ImageQuad>() : null;
                var iq = ihq != null ? ihq.GetComponentInChildren<ImageQuad>() : null;
                float ccx = cq != null ? PxOf(cq.transform.position.x) : -1f;
                float icx = iq != null ? PxOf(iq.transform.position.x) : -1f;
                CheckNear(icx, 1513.50f, 1f, "`Import` 中心 x = **1513.50**（原版左缘 1391.00 + 245/2）");
                CheckNear(ccx, 1783.50f, 1f, "`Create` 中心 x = **1783.50**（原版左缘 1661.01 + 245/2）");
                CheckNear(ccx - icx, 270f, 1f,
                          "★ `Create` 在 `Import` 的**右边** 270px（= 245 + spacing 25）—— "
                          + "照「树序 = 视觉序」镜像摆 ⇒ 这条变 **−270**、红（判据：`Control Buttons` 的 `reverse=1`）");
                // 原版这一组的**最左**是 `Unlock`（1180.00→1366.01）—— 它的 GO `m_IsActive = 1` 但**三个可见
                // 组件全 `m_Enabled = 0`**（`Image` / `Button Outline` / `Text` 的 TMP）⇒ 原版也画不出东西。
                // ⚠️ 与「顺序错」是**两条账**，别拿上面几条断言去替它。
                // 🆕 2026-10-06（A98）：那条账已结 —— `Control Buttons` 组节点 + `Unlock` 占位都建了，见下面那一块。
            }

            // 🆕 **2026-10-06（A98）：`Control Buttons` 组节点 + `Unlock` 占位**（原来两颗钮**直接挂在页节点上**、
            //    这一层没建 ⇒ 见 `项目任务.md` §三 A98 行）。判据（**逐字段直读** prefab `bundle_menus_assets_all`）：
            //    · 组 rect **1180.00,80.94 → 1920.01,140.94**（740.01×60）；组上挂
            //      `HorizontalLayoutGroup`（reverse=1 · spacing 25 · align MiddleRight · pad.right 14 · expandW 1）
            //      + `ContentSizeFitter(m_HorizontalFit = 1 = MinSize)`
            //    · `Unlock` 组内**最左**：**1180.00 → 1366.01**（宽 186.01），三个可见组件**全 `m_Enabled = 0`**
            //      ⇒ **原版也画不出东西**（是**组件级**禁用、不是 GO 级）
            //    🔴 量的是**节点的世界位置**（`MenuDraw.Node` 把节点摆在它那个矩形的中心）+ **挂载关系**，
            //       ⛔ 不是量我们的常量（断常量 = 同义反复）。
            {
                var cbg = FindChild(tabsRoot, "Control Buttons");
                CheckTrue(cbg != null,
                          "★ `Control Buttons` **组节点**建了（原版 `Select Deck Tab>Header/Control Buttons`）");
                if (cbg != null)
                {
                    CheckNear(PxOf(cbg.position.x), 1550.01f, 1f,
                              "★ 组节点中心 x = **1550.01**（原版组矩形 1180.00→1920.01；宽 740.01"
                              + " = 186.01+245+245+25+25+**14**(pad.right)）");
                    CheckNear(PxYOf(cbg.position.y), 110.94f, 1f, "★ …中心 y = **110.94**（原版 80.94→140.94）");
                    var bNew = FindChild(cbg, "Create");
                    var bImp = FindChild(cbg, "Import");
                    CheckTrue(bNew != null && bImp != null,
                              "`Create` / `Import` 两颗钮在**组节点底下**（原版 `Control Buttons>{Create,Import}`）");
                    var uUn = FindChild(cbg, "Unlock");
                    CheckTrue(uUn != null,
                              "★ 组里那颗 `Unlock` **占位**建了（原版树里有它；它占的 186.01px 正是"
                              + " `Import` 落在 1391.01 的原因）");
                    if (bNew != null && bImp != null && uUn != null)
                    {
                        // 树序照原版（⛔ 与视觉序**相反** —— `reverse=1`；视觉左→右 = Unlock → Import → Create）
                        CheckTrue(bNew.GetSiblingIndex() < bImp.GetSiblingIndex()
                                  && bImp.GetSiblingIndex() < uUn.GetSiblingIndex(),
                                  "★ 组内**树序** = 原版 `[Create, Import, Unlock]`（`reverse=1` ⇒ 视觉序**反之**："
                                  + "最左是 `Unlock`）");
                        CheckNear(PxOf(uUn.position.x), 1273.01f, 1f,
                                  "★ `Unlock` 中心 x = **1273.01**（原版 1180.00→1366.01，宽 186.01）");
                        CheckNear(PxYOf(uUn.position.y), 110.94f, 1f, "…中心 y = **110.94**（与另两颗同一行）");
                        // 🆕 **2026-10-06（A125③）：`Unlock` 的**两个子件**（原来一条断言都没有 —— 摆错不会红）**。
                        //    判据 = **逐字段直读** prefab（`bundle_menus_assets_all`；我另跑了一遍 RT 索引复核，
                        //    与 `Shell/CollectionWindow.cs` 的 `BuildUnlockPlaceholder` 注释同一份数据）：
                        //      · `Button Outline`（GO pid `-8849062637360586027`）拉伸锚 (0,0)→(1,1) ·
                        //        `m_SizeDelta (-1, 0)` · `m_AnchoredPosition (0,0)` ⇒ 左右各内缩 0.50、上下不缩
                        //        ⇒ **相对 `Unlock` 左上角 (0.50,0)→(185.51,60)**；
                        //      · `Text`（GO pid `-220370555535629611`）同锚 · `m_SizeDelta (-16.72, -17.19)`
                        //        ⇒ **相对 `Unlock` 左上角 (8.36,8.59)→(177.65,51.41)**（左右各内缩 8.36、
                        //        上下各内缩 8.59）；
                        //      · 两者**四边都是对称内缩** ⇒ 与 `Unlock` **同心** ⇒ 中心 = `Unlock` 的中心。
                        //    🔴 期望值写**字面量**（`Unlock` 中心 = 1180.00 + 186.01/2 = **1273.01** ·
                        //       y = 80.94 + 60/2 = **110.94**）—— ⛔ **别拿 `CollectionWindow` 的常量再算一遍**
                        //       （那是同义反复：常量改了这条照样绿）。
                        //    ⚠️ **这条量得到的只有中心**：`MenuDraw.Node` 建的是**普通 `Transform`**（没有
                        //       `RectTransform`）⇒ 节点上**不带宽高** ⇒ `185.51` / `177.65` / `60` / `42.82`
                        //       这几个尺寸**本条钉不住**（要钉住得让节点带尺寸语义 = 改公共件 `MenuDraw.Node`，
                        //       不在本件白名单）⇒ 报告里如实记着，别当成「尺寸已钉住」。
                        CheckTrue(uUn.childCount == 2,
                                  $"★ `Unlock` 底下**正好两个**子件（原版子树就 `Button Outline` + `Text`；"
                                  + $"实测 {uUn.childCount} 个）");
                        var uBo = FindChild(uUn, "Button Outline");
                        var uTx = FindChild(uUn, "Text");
                        if (uBo == null)
                            CheckTrue(false, "★ `Unlock` 的子件 `Button Outline` 在（原版树里有它）"
                                             + " —— 不在 ⇒ 下面两条等于没验");
                        else
                        {
                            CheckNear(PxOf(uBo.position.x), 1273.01f, 1f,
                                      "★ `Button Outline` 中心 x = **1273.01**（原版相对 `Unlock`"
                                      + " (0.50,0)→(185.51,60)：左右各内缩 0.50 ⇒ 与 `Unlock` 同心）");
                            CheckNear(PxYOf(uBo.position.y), 110.94f, 1f,
                                      "★ `Button Outline` 中心 y = **110.94**（原版上下**不**内缩 ⇒ 与 `Unlock` 同中心）");
                        }
                        if (uTx == null)
                            CheckTrue(false, "★ `Unlock` 的子件 `Text` 在（原版树里有它，文案「Debug Unlock」）"
                                             + " —— 不在 ⇒ 下面两条等于没验");
                        else
                        {
                            CheckNear(PxOf(uTx.position.x), 1273.01f, 1f,
                                      "★ `Text` 中心 x = **1273.01**（原版相对 `Unlock` (8.36,8.59)→(177.65,51.41)："
                                      + "左右各内缩 8.36 ⇒ 与 `Unlock` 同心）");
                            CheckNear(PxYOf(uTx.position.y), 110.94f, 1f,
                                      "★ `Text` 中心 y = **110.94**（上下各内缩 8.59 ⇒ 与 `Unlock` 同心）");
                        }
                        if (uBo != null && uTx != null)
                            CheckTrue(uBo.GetSiblingIndex() < uTx.GetSiblingIndex(),
                                      "★ `Unlock` 两个子件的**树序** = 原版 `[Button Outline, Text]`"
                                      + "（判据 = 原版 `RectTransform` 的 `m_Children` 逐位实读，不是照名字猜）");
                        var bImpQ = bImp.GetComponent<ImageQuad>();
                        // 🔴 **2026-10-06（A125③）**：这里原来是 **`if` 没有 `else`** —— 拿不到 quad 时
                        //    下面那条**一条都不跑、section 照样全绿**（**A52-F10** 那一族的形状：
                        //    弱断言分不出两种状态）。**今天它不可能 null**（`Shell/MenuDraw.cs` 的 `Rect`
                        //    转调 `ImageQuad.Create`，quad 组件加在**节点自己**身上，`Import` 那颗既是
                        //    节点也是 quad）⇒ 不是当下假绿，但形状要堵住：改成**前提断言 + 早退**，
                        //    条件不成立当场红、并说清「下面那条等于没验」。
                        if (bImpQ == null)
                            CheckTrue(false, "★ `Import` 那颗钮的**图片本体**（`ImageQuad`）拿得到"
                                             + " —— 拿不到就量不了它相对 `Unlock` 的位置，下面那条等于没验");
                        else
                            CheckNear(PxOf(bImpQ.transform.position.x) - PxOf(uUn.position.x), 240.5f, 1f,
                                      "★ `Unlock` 中心 →`Import` 中心 = **240.5**（= 186.01/2 + spacing **25** + 245/2）"
                                      + " —— 原版那颗占位**参与排布**，位置摆错这条就红");
                        int uq = uUn.GetComponentsInChildren<ImageQuad>(true).Length;
                        int ul = uUn.GetComponentsInChildren<Label>(true).Length;
                        CheckTrue(uq == 0 && ul == 0,
                                  $"★ `Unlock` 底下**一个图、一个字都没有**（实测 quad **{uq}** · label **{ul}**）—— "
                                  + "原版那三个可见组件**全 `m_Enabled = 0`**（自己的 `Image` · 子 `Button Outline` 的"
                                  + " `Image` · 子 `Text` 的 TMP「Debug Unlock」）⇒ **原版也画不出东西**；"
                                  + "给它加图/加字 = 与原版可见内容不符");
                        var bnL = FindChild(cbg, "Create Text");
                        var biL = FindChild(cbg, "Import Text");
                        CheckTrue(bnL != null && bnL.IsChildOf(cbg) && biL != null && biL.IsChildOf(cbg),
                                  "两颗钮的文案在**组节点子树**里（原版 = `Create>Button Text` / `Import>Button Text`）");
                    }
                }
            }
            var impHit = FindChild(tabsRoot, "ImportHit");
            CheckTrue(impHit != null && impHit.GetComponent<WindowButton>() != null,
                      "`Import` 钮有点击区（点了**如实说没实现**，不静默）");
            if (win.DeckScroll != null)
            {
                float max = win.DeckScroll.MaxOffset;
                CheckTrue(max >= 0f, $"卡组列表的滚动极值 = {max:F0}px（`MenuScroll` 纵向；格数少时可以为 0）");
                if (max > 0f)
                {
                    win.DeckScroll.ScrollBy(max);
                    var last = CollectionData.DeckCount() - 1;
                    var lastCell = FindChild(tabsRoot, "CollectionDeck_" + last);
                    CheckTrue(lastCell != null
                              && PxYOf(lastCell.position.y) + DeckCellHpx() / 2f <= 1080.5f,
                              "**滚到底 ⇒ 最后一格完整落进视口**（这是「够不着」那个缺口的判据）");
                    win.DeckScroll.ScrollBy(-max);
                }
            }

            // ---- 🆕 2026-10-03（A12 收口）：**滚动视口的裁切也管点击区** —— 真界面回归 ----
            //   判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid` 就是一句
            //   `RectangleContainsScreenPoint` ⇒ 框外的点判不中任何东西）。判据正本 = 本工程唯一一份
            //   `MenuDraw.ClipRect`（它的注释里有 UGUI 源码出处）；这一段量的是**真界面**、不是临时节点。
            //   ⚠️ 这一段量的是**节点的位置**（= 交集块的中心），不是宽高。
            //      🔴 **2026-10-04 订正（A26）**：原来这里写着「`MenuDraw.DeckCell` 的 `Hit` 是**裸节点**（不带 quad）
            //      ⇒ 它进不了 `PointerLayer` 的命中表」—— **那条已经不成立**：裸节点已经补上了透明 quad
            //      （见 `MenuDraw.DeckCell` 的注释与新加的「真命中路」那一段断言）。
            {
                var ds = win.DeckScroll;
                if (ds != null)
                {
                    float savedD = ds.Offset;
                    ds.SetOffset(0f);                       // 起手态：第 3 行压在视口下边上
                    Transform edge = null; float edgeCy = 0f;
                    for (int i = 0; i < win.DeckCells.Count; i++)
                    {
                        var c = win.DeckCells[i];
                        if (c == null) continue;
                        float cy = PxYOf(c.position.y);
                        if (cy + DeckCellHpx() * 0.5f > CollectionWindow.DeckViewport.y2 + 0.5f)
                        { edge = c; edgeCy = cy; break; }
                    }
                    CheckTrue(edge != null, "起手有一格**压在视口下边上**（否则这一条等于没验）");
                    if (edge != null)
                    {
                        float ecx = PxOf(edge.position.x);
                        var full = new PxRect(ecx - DeckCellWpx() * 0.5f, edgeCy - DeckCellHpx() * 0.5f,
                                              ecx + DeckCellWpx() * 0.5f, edgeCy + DeckCellHpx() * 0.5f);
                        PxRect vis;
                        bool inView = MenuDraw.ClipRect(full, CollectionWindow.DeckViewport, out vis);
                        CheckTrue(inView && !MenuDraw.SameRect(vis, full),
                                  "现算：这一格与视口的**交集比整格小**（= 真被裁了，下面才有可验的）");
                        var eHit = FindChild(edge, "Hit");
                        CheckTrue(eHit != null, "压在边上的那一格**建了** `Hit`（部分越界 ⇒ 建、但位置被截）");
                        if (eHit != null)
                        {
                            CheckNear(PxOf(eHit.position.x), vis.CX, 0.5f,
                                      "命中区中心 x = **露出来那块的中心**（横向没裁 ⇒ 与格中心同）");
                            CheckNear(PxYOf(eHit.position.y), vis.CY, 0.5f,
                                      "命中区中心 y = **露出来那块的中心**（**不是**整格中心）");
                            CheckNear(Mathf.Abs(PxYOf(eHit.position.y) - edgeCy), (full.H - vis.H) * 0.5f, 0.5f,
                                      "…与整格中心的差 = 被裁掉那半截的一半（"
                                      + $"{Mathf.Abs(PxYOf(eHit.position.y) - edgeCy):F1} ≈ {(full.H - vis.H) * 0.5f:F1}）");
                        }
                    }
                    ds.SetOffset(savedD);
                }
            }

            // ---- 🆕 2026-10-04（A26）：**卡组格真鼠标点得到**（走真命中路，**不是**直调 `wb.Click()`）----
            //   判据 = `PointerLayer.CollectHits` 取的是「按钮下**第一个 `ImageQuad`**」
            //   （`Shell/PointerLayer.cs:492`）。`MenuDraw.DeckCell` 的 `Hit` 当年是 `Node()` 建的**裸节点**
            //   ⇒ 命中表里**恒没有它** ⇒ 真鼠标点不动；而 `WindowButton.onClick` 在着、自检直调 `wb.Click()`
            //   也过 ⇒ **只有这条路能验出来**（这正是 `真Play待验清单` A 鼠标那条的意义）。
            //   ⚠️ 这一段是**会红的**：把 `DeckCell` 改回裸节点它立刻红（不是自证 —— 期望值取自
            //      `PointerLayer` 那条与实现无关的路）。
            {
                var pl = PointerLayer.Instance;
                CheckTrue(pl != null, "`PointerLayer` 在（真鼠标 → 界面**唯一**那条路）");
                if (pl != null && win.DeckScroll != null)
                {
                    float savedD = win.DeckScroll.Offset;
                    win.DeckScroll.SetOffset(0f);               // 起手态：行 0 完整在视口里
                    var dc = win.DeckCells;                     // ⚠️ `SetOffset` 会重建 ⇒ 重建之后再取表
                    CheckTrue(dc.Count > 0, "有卡组格可点（否则这一段等于没验）");
                    if (dc.Count > 0)
                    {
                        var c0 = dc[0];
                        var h0 = FindChild(c0, "Hit");
                        CheckTrue(h0 != null, "第 1 格有 `Hit` 节点");
                        var hq = h0 != null ? h0.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(hq != null,
                                  "★ 命中区节点下**真的挂着 `ImageQuad`** —— `PointerLayer` 只认它"
                                  + "（裸节点 = 进不了命中表 = 真鼠标点不动）");
                        if (hq != null)
                            Check(hq.RenderQueue, CollectionWindow.QPageRow,
                                  "命中 quad 的渲染队列 = `QPageRow`（与 `DeckCell` 的入参同档）");
                        var b0 = pl.ButtonAt(PxOf(c0.position.x), PxYOf(c0.position.y));
                        CheckTrue(b0 != null && b0.transform == h0,
                                  "★ **真命中路**（`PointerLayer.ButtonAt`）在格中心拿到的是**这一格**"
                                  + "（拿到 " + (b0 == null ? "**null** = 真鼠标点不动" : "`" + b0.name + "`") + "）");
                    }

                    // 压在视口下边上的那一格：**露出来那块的中心**也点得到，且命中 quad 的高**跟着裁切走**
                    Transform edge = null; float edgeCy = 0f;
                    for (int i = 0; i < win.DeckCells.Count; i++)
                    {
                        var c = win.DeckCells[i];
                        if (c == null) continue;
                        float cy = PxYOf(c.position.y);
                        if (cy + DeckCellHpx() * 0.5f > CollectionWindow.DeckViewport.y2 + 0.5f)
                        { edge = c; edgeCy = cy; break; }
                    }
                    CheckTrue(edge != null, "起手有一格**压在视口下边上**（否则下面那条等于没验）");
                    if (edge != null)
                    {
                        float ecx = PxOf(edge.position.x);
                        var full = new PxRect(ecx - DeckCellWpx() * 0.5f, edgeCy - DeckCellHpx() * 0.5f,
                                              ecx + DeckCellWpx() * 0.5f, edgeCy + DeckCellHpx() * 0.5f);
                        PxRect vis;
                        bool partClip = MenuDraw.ClipRect(full, CollectionWindow.DeckViewport, out vis)
                                        && !MenuDraw.SameRect(vis, full);
                        // 🔴 **A52-F10 修（2026-10-06）**：这一段原来是个**没有 `else` 的 `if`** ——
                        //   条件不成立（那一格**整块**落在视口外 ⇒ `ClipRect` 返回 false）时，下面那两条 ★
                        //   **一条都不跑**，而 section 照样全绿 ⇒ 「弱断言分不出两种状态」那一族。
                        //   改成**前提断言 + `if`**：条件不成立时当场红，并说清「下面两条等于没验」。
                        //   （判据同上面 A12 那一段的 `现算：交集比整格小`；写法照 `Editor/ShellScene.cs` 的 R7 探针。）
                        CheckTrue(partClip,
                                  $"★ 前提：压边那一格必须**部分**越界（`ClipRect` 为真 **且** 交集 ≠ 整格；"
                                  + $"实测 `vis`=( {vis.x1:F1},{vis.y1:F1} )→( {vis.x2:F1},{vis.y2:F1} )"
                                  + $" vs 整格 ( {full.x1:F1},{full.y1:F1} )→( {full.x2:F1},{full.y2:F1} )）"
                                  + " —— 不成立则下面两条 ★ 等于没验（A52-F10：原来这个 `if` 没有 `else`，整段静默空转）");
                        if (partClip)
                        {
                            var eh = FindChild(edge, "Hit");
                            var ehq = eh != null ? eh.GetComponentInChildren<ImageQuad>() : null;
                            CheckNear(ehq != null ? ehq.WorldH * 108f : -1f, vis.H, 1.5f,
                                      "★ 压在边上的那一格：命中 quad 的高 = **露出来那块**的高"
                                      + "（`Hit` 跟着 `RectMask2D` 的射线那一面裁）");
                            var be = pl.ButtonAt(vis.CX, vis.CY);
                            CheckTrue(be != null && be.transform == eh,
                                      "★ …而**露出来那块的中心**（不是整格中心）点得到它"
                                      + "（拿到 " + (be == null ? "**null**" : "`" + be.name + "`") + "）");
                        }
                    }
                    win.DeckScroll.SetOffset(savedD);
                }
            }

            // ---------------- `Deck info Popup`（A1 §2 逐节点表）----------------
            Section("`Deck info Popup`：版面（A1 §2）");
            {
                var pop = win.OpenDeckInfo(0);
                CheckTrue(pop != null, "开得出来");
                if (pop != null)
                {
                    var pr = pop.transform;
                    CheckHoverSwap(pop.transform, "Deck info Popup");   // 🆕 A17：三颗钮 + 关闭钮的圆底
                    Check(pop.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(pop.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                    Check(pop.closeOnEsc, true, "`closeOnESC` = **1**（⚠️ 提示窗是 0）");
                    CheckAt(FindChild(pr, "Generic Window Red Background Big"),
                            134.50f, 1839.50f, 82f, 1032f, "红底 `UI_Deck_Information_Back`");
                    CheckAt(FindChild(pr, "Info Panel"), 659f, 1799f, 218.10f, 868.10f, "`Info Panel`");
                    var dn = CollectionData.DeckAt(0);
                    CheckText(TextOf(FindChild(pr, "Deck Name")), dn.Name, "`Deck Name` = 被点的那套卡组的名字");
                    var wlc = CollectionData.Warlord(0);
                    CheckText(TextOf(FindChild(pr, "Warlord Name")), wlc != null ? wlc.Name : "未选督军",
                              "`Warlord Name` = 该卡组的督军名");
                    CheckTrue(pop.Btn("Practice Deck") != null && pop.Btn("Edit Deck") != null
                              && pop.Btn("Select Deck") != null, "`Buttons` 三个钮**都建了**（在不在 ≠ 露不露，见下）");
                    // 🆕 2026-10-05（A65② / A82）：`Edit Deck` 那颗的**变灰**落到哪一层。
                    // 判据：原版 `DeckInfoControls__Initialize.c:79-83` 无条件调
                    //   `EverguildButton__SoftDisable(editButton, !CanImportDeck(popup, context.Deck))`，
                    //   而那颗钮的 `colorTintGreyOnDisable = 1`（真包实读 MB `-8697463422759302744`）
                    //   ⇒ `SwitchMaterial` → **图形件材质换成 `Everguild/UI/Greyscale`**（不是改颜色、不是调 alpha）。
                    // 我们这一侧 = `WindowButton.SetSoftDisabled`（`Shell/PromptPopup.cs`），实现只有一份。
                    // 🔴 **要核的是「可见的那张底图」**：这颗钮的 `WindowButton` 挂在**透明命中区** `Btn_Edit Deck`
                    //    上，可见的底是**兄弟节点** `Bg Edit Deck`（原版 `Selectable.m_TargetGraphic` 就指向它）
                    //    ⇒ 只灰命中区 = 一个像素都不变（静默）。`CanImportDeck` 恒 `false`（判据见它的注释）
                    //    ⇒ 这颗钮**应当**一直是灰的。
                    {
                        var bgEdit = FindChild(FindChild(pr, "Buttons"), "Bg Edit Deck");
                        var bgq = bgEdit != null ? bgEdit.GetComponent<ImageQuad>() : null;
                        var bmr = bgq != null ? bgq.GetComponent<MeshRenderer>() : null;
                        CheckText(bmr != null && bmr.sharedMaterial != null && bmr.sharedMaterial.shader != null
                                  ? bmr.sharedMaterial.shader.name : "<没有材质>",
                                  "Everguild/UI/Greyscale",
                                  "`Edit Deck` 的**可见底图**换成了原版灰化 shader（不是只灰了那个透明命中区）");
                        // ⚠️ **上面这一条只核「换没换 shader」（弱断言）** —— 它分不出「换了材质、但队列掉了」。
                        //    🔴 **2026-10-05（A85）：换材质不许把显式分层抹掉。** 变灰走 `ImageQuad.SetMaterial`，
                        //    而 `new Material(Everguild/UI/Greyscale)` 的队列是 shader 自带的
                        //    **`Transparent(3000)`**（`工具/dump_shader.py` 实读它的 SubShader 标签），
                        //    本窗红底在 **`QDI = 3120`** ⇒ 不补回去这几颗钮会**掉到红底之下**
                        //    （画面上「按钮没了」，而上面那条**照样全绿** —— 「弱断言分不出两种状态」）。
                        //    ✅ 修法两层：① `Battle/ImageQuad.cs` 的 `SetMaterial` **保留调用前的旧队列**（通用）；
                        //       ② `Shell/DeckInfoPopup.cs` 的 `ReassertButtonQueues()` 在状态翻转**之后**钉回绝对值。
                        // 🔴 **量与【红底】的相对档位**（原版语义：按钮画在窗底图之上），⛔ **不是跟我们自己的常量比**。
                        //    ⚠️ 覆盖 **8 颗钮里变灰会碰到的每一张 quad**（可见底 + 命中区）—— 只量可见那张的话，
                        //    「命中区掉档」这条静默错就漏了（它全透明，肉眼与截图都看不出来）。
                        var plateNode = FindChild(pr, "Generic Window Red Background Big");
                        var plateQ = plateNode != null ? plateNode.GetComponentInChildren<ImageQuad>(true) : null;
                        if (plateQ == null)
                            CheckTrue(false, "红底 `UI_Deck_Information_Back` 的 quad 没建出来 ⇒ 这条队列不变量判不了");
                        else
                        {
                            string[] qkeys = { "Btn:Practice Deck", "Btn:Edit Deck", "Btn:Select Deck",
                                               "Opt:Switch Deck Info", "Opt:Duplicate", "Opt:Share",
                                               "Opt:Share On Chat", "Opt:Delete" };
                            int nq = 0, badq = 0;
                            var badqList = new System.Text.StringBuilder();
                            foreach (var k in qkeys)
                            {
                                // `Btn:`/`Opt:` 两个前缀 = `Shell/DeckInfoPopup.cs` 登记 `_wbs` 时用的同一套键
                                var node = k.StartsWith("Opt:") ? pop.Opt(k.Substring(4)) : pop.Btn(k.Substring(4));
                                var wb = node != null ? node.GetComponent<WindowButton>() : null;
                                if (wb == null)
                                { badq++; badqList.Append("「").Append(k).Append("」没建出 `WindowButton`；"); continue; }
                                // 变灰碰到的就是这两组：`target`（可见底）+ 子树（全局唯一的 `Hit` 命中区）
                                // —— 与 `PromptPopup.GrayTargets()` 同一套分组，别只量一半
                                var qs = new List<ImageQuad>();
                                if (wb.target != null) qs.Add(wb.target);
                                foreach (var q in wb.GetComponentsInChildren<ImageQuad>(true))
                                    if (q != null && q != wb.target) qs.Add(q);
                                foreach (var q in qs)
                                {
                                    nq++;
                                    if (q.RenderQueue <= plateQ.RenderQueue)
                                    {
                                        badq++;
                                        badqList.Append("「").Append(k).Append("」的 `").Append(q.name)
                                                .Append("` 队列 ").Append(q.RenderQueue)
                                                .Append(" ≤ 红底 ").Append(plateQ.RenderQueue).Append("；");
                                    }
                                }
                            }
                            CheckTrue(nq >= 8, $"量到了 8 颗钮的 quad 共 **{nq}** 张（`target` + 命中区，至少各一张）");
                            // 🔴 **这条断言红的准确路径**（说清楚，别写成半真不假的）：
                            //    A85 之后**有两道**同时在保这个不变量 —— ① `ImageQuad.SetMaterial` 保留旧队列（通用）
                            //    ② 本窗 `ReassertButtonQueues()` 在状态翻转后钉回绝对值。**两道都去掉**才红
                            //    （= 修之前那个状态：变灰后那两张 quad 读出来是 3000 ≤ 红底 3120）。
                            //    ⚠️ 只去掉其中一道仍然绿 —— 这不是「断言没用」，而是**两道保险**，
                            //    所以别把「删一处它不红」当成断言失灵（要证它带电，就把两处一起去掉）。
                            CheckTrue(badq == 0,
                                      $"8 颗钮的**每一张** quad 都画在红底之上（核了 {nq} 张，红底队列 = {plateQ.RenderQueue}）"
                                      + "：⛔ 换灰材质（`Everguild/UI/Greyscale` 自带 3000）不许把显式队列抹掉 —— "
                                      + "（把 `ImageQuad.SetMaterial` 的 `if (q >= 0) m.renderQueue = q;` **与**本窗的"
                                      + " `ReassertButtonQueues()` 两道**一起**去掉，这一条就红）：" + badqList);
                        }
                    }
                    // 🆕 2026-10-04（A31）：**这三颗按 state 显隐** —— 收藏窗这条 = 原版 `DeckCollectionTab.OnItemSelected`
                    //   = **state 0**（判据 → `Shell/DeckInfoPopup.cs` 文件头那张表）。state 0 下：
                    //   `Edit Deck` 露（`state < 2`）· `Practice Deck` 露（`isPlayerDeck && state ∈ {0,2}`）·
                    //   `Select Deck` **不露** —— 原版判据是 `context.SelectButton != null`，而**6 个调用点全传 null**
                    //   （逐处实读 `DeckInfoContext__ctor` 的第 3 个实参）⇒ 本 build 里这颗钮从不出现。
                    CheckTrue(pop.IsItemShown("Btn:Edit Deck"), "state 0 ⇒ `Edit Deck` **露着**（原版 `state < 2`）");
                    CheckTrue(pop.IsItemShown("Btn:Practice Deck"), "state 0 ⇒ `Practice Deck` **露着**（`isPlayerDeck && state∈{0,2}`）");
                    CheckTrue(!pop.IsItemShown("Btn:Select Deck"),
                              "state 0 ⇒ `Select Deck` **不露**（原版 `context.SelectButton != null`，而 6 个调用点全传 null）");
                    var pb = pop.Btn("Practice Deck");
                    CheckNear(pb != null ? PxOf(pb.position.x) : -1f, 887.45f, 1f,
                              "`Practice Deck` 中心 x = **887.45**（三个钮右缘到 1770.70、spacing 36）");
                    var sb = pop.Btn("Select Deck");
                    // ⚠️ 量的是**关着的**那颗（`Transform.Find` 找得到关着的节点）—— 位置不因显隐而变
                    CheckNear(sb != null ? PxOf(sb.position.x) : -1f, 1608.45f, 1f, "`Select Deck` 中心 x = **1608.45**");
                    CheckTrue(pop.Opt("Delete") != null && pop.Opt("Switch Deck Info") != null
                              && pop.Opt("Share") != null && pop.Opt("Share On Chat") != null
                              && pop.Opt("Duplicate") != null, "`Deck Options` 五个圆钮都在");
                    // 🆕 2026-10-04（A31）：这一扇是 **state 0**（原版 `DeckCollectionTab.OnItemSelected`）
                    //   ⇒ 四颗 `state == 0 && isPlayerDeck` 的**都该露**（两态对比的另一半 → 下面那节 A31）
                    CheckTrue(pop.IsItemShown("Opt:Share") && pop.IsItemShown("Opt:Share On Chat")
                              && pop.IsItemShown("Opt:Delete") && pop.IsItemShown("Opt:Duplicate"),
                              "state 0 ⇒ `Share`/`Share On Chat`/`Delete`/`Duplicate` **四颗都露着**");
                    var od = pop.Opt("Delete");
                    var os2 = pop.Opt("Switch Deck Info");
                    // 🔴 **2026-10-05 就地订正（铁律 5）**：**原文**（2026-10-03「A10」）写的是
                    //    「原版左→右 = **树序**：`Switch Deck Info` → `Duplicate` → `Share` → `Share On Chat`
                    //    → `Delete`……那句『`reverse=1` 把 GO 顺序倒过来』**实测不成立**」——
                    //    **那次订正本身是错的**。**错因**：读数出自**还不建模 `m_ReverseArrangement`** 的
                    //    `工具/menu_dump.py`（输出的是「正序 + 模板位」= **镜像读数**）⇒ A10 把一处**本来正确**的
                    //    实现（`Delete` 最左）改成了错的，并把这句话也抄进了这两条 `CheckNear`。
                    //    **实况**：uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`
                    //    （`startIndex = reverse ? Count−1 : 0` / `increment = reverse ? −1 : 1`）
                    //    ⇒ `reverse=1` 时**树序最后一个（`Delete`）落在最左**；跑后真值（左→右）
                    //    = `Delete` 1333.33 · `Share On Chat` 1427.31 · `Share` 1521.28 · `Duplicate` 1615.26
                    //    · `Switch Deck Info` 1709.23（步进 **93.976**）。算式与出处 →
                    //    `Shell/DeckInfoPopup.cs` 第 7) 节注释；重取命令见该处。
                    // 🔴 **下面三条断的是【建出来的世界 x 的序】、不是「`opts[]` 数组怎么写的」** ——
                    //    断数组顺序是同义反复（期望值会跟着实现走）；断世界 x 才能「改回镜像就红」。
                    var oNames = new[] { "Switch Deck Info", "Duplicate", "Share", "Share On Chat", "Delete" };
                    var oPx = new float[oNames.Length];
                    for (int oi = 0; oi < oNames.Length; oi++)
                    {
                        var ot = pop.Opt(oNames[oi]);
                        oPx[oi] = ot != null ? PxOf(ot.position.x) : float.NaN;
                    }
                    // ① 逐颗严格递减（树序第 i 颗必须比第 i+1 颗**靠右** —— 这就是「视觉序 = 树序倒排」）
                    bool oReversed = true;
                    string oTrace = "";
                    for (int oi = 0; oi + 1 < oPx.Length; oi++)
                    {
                        if (!(oPx[oi] > oPx[oi + 1] + 1f)) oReversed = false;
                        oTrace += (oi > 0 ? " > " : "") + oNames[oi] + " " + oPx[oi].ToString("F2");
                    }
                    oTrace += " > " + oNames[oNames.Length - 1] + " " + oPx[oNames.Length - 1].ToString("F2");
                    CheckTrue(oReversed,
                              "★ `Deck Options` **视觉序 = 树序倒排**（原版 `m_ReverseArrangement = 1`）—— "
                              + "树序从左往右必须**逐颗更靠左**；实测：" + oTrace
                              + "（**改成镜像 ⇒ 这条红**）");
                    // ② 身位：最左 = `Delete` 中心 **1370.52**（= 组左沿 1263.74 + 69.59 + 74.386/2）；最右 = `Switch Deck Info`
                    CheckNear(od != null ? PxOf(od.position.x) : -1f, 1370.52f, 1f,
                              "**最左**那颗是 `Delete`（中心 1370.52 = 1333.33 + 74.386/2；旧镜像读数 1746.43 是它【最右】的位置）");
                    CheckNear(os2 != null ? PxOf(os2.position.x) : -1f, 1746.43f, 1f,
                              "**最右**那颗是 `Switch Deck Info`（中心 1746.43；旧镜像读数 1648.89 作废）");
                    // ③ 步进 = **93.976**（原版 `childSize 143.976 + spacing(−50)`）⇒ 首尾差 = 4 × 93.976 = 375.90
                    CheckNear(oPx[0] - oPx[4], 375.90f, 1f,
                              "…首尾差 = **4 × 93.976 = 375.90**（≠ 4 × (74.386 − 50) = 97.54 —— 旧模型会差 278.36）");

                    // ============ 🆕 2026-10-11（A299）：五颗圆钮的【射线那一面】= 子件矩形外扩 20 ============
                    //   与 A180（关窗钮）**完全同源**，判据也是同一套解包实读（2026-10-11 沿 `m_Children` 整棵核过）：
                    //     `Deck info Popup > Deck Options` 的五个子件 —— 每颗**自己的 `Image`** 都是
                    //     `m_RaycastTarget = 0` · pad 全 0；吃射线的是它的**两个子件** `Background` / `Icon`
                    //     （**都 `m_RaycastTarget = 1` · `m_RaycastPadding = (−20,−20,−20,−20)`**，负 = 外扩）。
                    //     子件矩形（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Options" --depth 3`）
                    //     = **`56.86 × 58.13`**（按钮自身 `74.386 × 75.605`）⇒ 外扩 20 = **96.86 × 98.13**。
                    //   ⛔ 期望值写**原版 dump 的子件矩形外扩 20 后**的字面量（dump 印到 1 位小数），
                    //     ⛔ **不写** `DeckInfoPopup.OptPad` / `OptW` / `OptIconW` —— 那是被测实现**传进去的实参**，
                    //     拿它当期望 = 同式自证（改实现它跟着绿）。
                    //   ⚠️ 节点与 quad 都量（同 A180）：`CheckAtPx` 管**节点**（抓「节点停在容器 (0,0)」那类错），
                    //     四条边 + 宽高管**画出来那张 quad**。
                    {
                        // 五颗各自的「子件矩形外扩 20」（按**树序**给，与上面 `oNames` 同一个集合 —— 铁律：
                        //   断言必须与【被测的那件事】用同一个集合）。x 逐颗不同、y 五颗同值：
                        //   `oX1[i] = 子件左沿 − 20` · `oX2[i] = 子件右沿 + 20`（子件左/右沿 = 上表 dump 的 1717.4/1774.2 …）
                        float[] oX1 = { 1697.4f, 1603.4f, 1509.4f, 1415.5f, 1321.5f };
                        float[] oX2 = { 1794.2f, 1700.3f, 1606.3f, 1512.3f, 1418.3f };
                        const float oTY = 118.2f, oBY = 216.3f;               // = 子件 138.2 / 196.3 各外扩 20
                        for (int oi = 0; oi < oNames.Length; oi++)
                        {
                            var hNode = pop.Opt(oNames[oi]);
                            // ① **节点**位置（2D、**z 不进比较** —— 那颗节点的 z 是按 `HitZFront` 有意挪过的，
                            //    A180 那次就是被 `CheckAt` 的「含 z」口径坑成恒红，见 `CheckAtPx` 的注释）。
                            //    🔴 **2026-10-11（A308）容差 1.5 → 0.3**：修完之后残差 ≤ 0.02px，
                            //      而「子件矩形退回居中」的偏差是 0.61/0.76 ⇒ 这一档正好夹在中间（见 ② 那条注释）。
                            //    改坏法：节点没摆（停在容器 (0,0) ≈ 画布中心 (960,540)）⇒ 差几百 px ⇒ 红；
                            //      子件矩形退回居中 ⇒ 差 0.61/0.76 ⇒ 也红（改前 1.5 那一档够不着）。
                            CheckAtPx(hNode, (oX1[oi] + oX2[oi]) * 0.5f, (oTY + oBY) * 0.5f, 0.3f,
                                      $"`Opt_{oNames[oi]}` 命中区**节点**在原版**射线区**中心（子件矩形外扩 20 后复算）");
                            var oQ = hNode != null ? hNode.GetComponentInChildren<ImageQuad>() : null;
                            if (oQ == null)
                            {
                                CheckTrue(false, $"`Opt_{oNames[oi]}` 命中区下面**没有 `ImageQuad`**"
                                                 + "（`PointerLayer` 的命中候选靠它 ⇒ 这颗等于点不动）");
                                continue;
                            }
                            float oW = oQ.WorldW * 108f, oH = oQ.WorldH * 108f;
                            float oCX = PxOf(oQ.transform.position.x), oCY = PxYOf(oQ.transform.position.y);
                            // ② 四条边 + 宽高（绝对值 · **±0.3px** —— 容差是 2026-10-11 A308 从 ±1.5 收下来的，
                            //    理由见下面「为什么能收」那段）。
                            //    改坏法（逐条都会红）：去掉外扩 ⇒ 实测退回按钮矩形 `x1,130.20 → x1+74.386,205.805`
                            //    ⇒ 左沿差 +11.8 / 右沿 −10.6 / 上沿 +12.0 / 下沿 −10.5（全部 > 0.3）；
                            //    把 pad 加在**按钮矩形**上 ⇒ 每边多 ≈8.76（宽高变 114.386×115.605）；
                            //    **把子件矩形退回「居中」**（`(OptW−OptIconW)/2`）⇒ 左/上沿差 +0.61/+0.76、
                            //    右/下沿同量反向 ⇒ **左沿那条必然红**（判别力正是本件加的）；
                            //    整颗搬走 ⇒ 直接红。
                            // 🔴 **为什么能从 ±1.5 收到 ±0.3**：① 修完之后我们与这四个字面量的残差
                            //    **≤ 0.045px**（逐颗复算 `1333.33 + (4−i)×93.976 + 8.151 − 20` ⇒ 1697.385 /
                            //    1603.409 / 1509.433 / 1415.457 / 1321.481 vs 字面量 1697.4 / … / 1321.5）；
                            //    ② 字面量本身是原版 dump 的 1 位小数（±0.05）；③ 而「居中」那半边的偏差是
                            //    **0.61(x) / 0.76(y)** ⇒ 0.3 落在「残差 0.05」与「缺陷 0.61」之间。
                            //    ⚠️ **改前用 1.5 是「够不着缺陷」**（这条一直是绿的）—— 那正是 A308 能被漏掉的原因。
                            CheckNear(oCX - oW * 0.5f, oX1[oi], 0.3f,
                                      $"`Opt_{oNames[oi]}` 命中区**左沿** = **{oX1[oi]:F1}**（= 原版子件左沿 {oX1[oi] + 20f:F1} 外扩 20）");
                            CheckNear(oCX + oW * 0.5f, oX2[oi], 0.3f,
                                      $"…**右沿** = **{oX2[oi]:F1}**（= 原版子件右沿 {oX2[oi] - 20f:F1} 外扩 20）");
                            CheckNear(oCY - oH * 0.5f, oTY, 0.3f, "…**上沿** = **118.2**（= 原版子件上沿 138.2 外扩 20）");
                            CheckNear(oCY + oH * 0.5f, oBY, 0.3f, "…**下沿** = **216.3**（= 原版子件下沿 196.3 外扩 20）");
                            CheckNear(oW, 96.86f, 1.5f, "…**宽 = 96.86**（= 子件宽 56.86 + 2×20 外扩）");
                            CheckNear(oH, 98.13f, 1.5f, "…**高 = 98.13**（= 子件高 58.13 + 2×20 外扩）");
                            // ③ 🔴 **相对断言**（A180 那两条的同一手法）—— 外扩是加在**子件矩形**上的：
                            //    拿**同一颗自己**画出来的 `Face` 层当尺子（不读任何常量、也不看绝对矩形）。
                            //    ⛔ 为什么非要有这一组：绝对量只要有一份**抄错的文档**，实现与期望就会**一起偏**
                            //    （铁律：补相对断言）。
                            //    改坏法（两条差得很开，一眼能分辨）：去掉外扩 ⇒ 相对量 **0**（≠20）；
                            //    pad 加在**按钮矩形**上 ⇒ 相对量 **28.76 / 29.37**（≠20 / ≠20.63）—— 四条都红。
                            var oFace = FindChild(hNode.parent, "Face " + oNames[oi]);
                            var fQ = oFace != null ? oFace.GetComponentInChildren<ImageQuad>() : null;
                            if (fQ == null)
                                CheckTrue(false, $"`Face {oNames[oi]}` 那一层不在 ⇒ 下面四条「外扩量 = 20/边」没法量");
                            else
                            {
                                float fW = fQ.WorldW * 108f, fH = fQ.WorldH * 108f;
                                float fCX = PxOf(fQ.transform.position.x), fCY = PxYOf(fQ.transform.position.y);
                                CheckNear((fCX - fW * 0.5f) - (oCX - oW * 0.5f), 20f, 0.5f,
                                          "★ A299：命中区**左外扩 = 20**（相对同颗的 `Face` 层）");
                                CheckNear((oCX + oW * 0.5f) - (fCX + fW * 0.5f), 20f, 0.5f,
                                          "★ …**右外扩 = 20**（同上）");
                                // 上下：`Face` 层是**等比内接**过的（原版子件那颗 `Image` 的 `m_PreserveAspect = 1`；
                                //   贴图 `40k_general_bt_yellow` 实测 **71×71 正方形**，而子件矩形是 56.86×58.12
                                //   ⇒ 内接缩的是【高】、x 不动）⇒ 它的上下缘比**子件矩形**各内缩
                                //   `(58.13 − 脸层高)/2`。原版子件矩形高写**字面量 58.13**（⛔ 不读 `OptIconH`），
                                //   于是相对量 = `20 + 那半个内缩量`（方图那一档 = 20.63）。
                                //   ⚠️ 这样写**不把「脸层是不是内接过」也一起钉死**——本条的题面只有「外扩 20/边」。
                                float fInset = (58.13f - fH) * 0.5f;
                                CheckNear((fCY - fH * 0.5f) - (oCY - oH * 0.5f), 20f + fInset, 0.5f,
                                          $"★ …**上外扩 = 20**（+{fInset:F2} = `Face` 层等比内接的半个内缩量）");
                                CheckNear((oCY + oH * 0.5f) - (fCY + fH * 0.5f), 20f + fInset, 0.5f,
                                          "★ …**下外扩 = 20**（同上）");
                            }
                            // ②b 🔴 **2026-10-11（A308）：原版子件矩形**不居中于按钮矩形****（真偏离 · 亚像素）
                            //    判据 = **逐字段实读**（`bundle_menus_assets_all`；五颗按钮的 `Background`/`Icon`
                            //    **十个节点逐值相同**，铁律 5·c 逐颗核过）：`m_AnchorMin (0.114, 0.12429)` ·
                            //    `m_AnchorMax (0.8733663, 0.88814)` · `m_AnchoredPosition (−0.14, 0.29)` ·
                            //    `m_SizeDelta 0.3774` · pivot `(0.5, 0.5)`（按钮自身 74.386×75.605）
                            //    ⇒ 左内缩 **8.151** / 右 **9.371** ⇒ **中心比按钮中心偏 −0.610**；
                            //       上内缩 **7.978** / 下 **9.498** ⇒ **偏 −0.760**（y 向下）。
                            //    尺子 = **同一颗自己的 `Bg` 层**（九宫底图 `UI_Button_Round_background` 是方图、
                            //    `m_PreserveAspect` 只缩高 ⇒ 它的**左右缘 = 按钮矩形的左右缘**、中心 = 按钮中心）。
                            //    ⛔ 不读 `DeckInfoPopup.OptInL` 那组常量（= 被测实现传进去的实参 ⇒ 同式自证）。
                            //    改坏法：`fx1/fy1` 退回「居中」（`(OptW−OptIconW)/2` · `(OptH−OptIconH)/2`）
                            //      ⇒ 两个相对量都变 **0** ⇒ 两条都红（`|0 − (−0.610)| = 0.610 > 0.3`）；
                            //      只改一半（例如只把 x 改回居中）⇒ **恰好红一半** —— 能分辨是哪半边漏了。
                            var oBg = FindChild(hNode.parent, "Bg " + oNames[oi]);
                            var bQ = oBg != null ? oBg.GetComponentInChildren<ImageQuad>() : null;
                            if (bQ == null)
                                CheckTrue(false, $"`Bg {oNames[oi]}` 那一层不在 ⇒ 「子件矩形不居中」两条没法量");
                            else
                            {
                                float bCX = PxOf(bQ.transform.position.x), bCY = PxYOf(bQ.transform.position.y);
                                CheckNear(oCX - bCX, -0.610f, 0.3f,
                                          $"★ A308：`{oNames[oi]}` 的子件矩形中心 **比按钮中心偏左 0.610**"
                                          + "（原版左内缩 8.151 / 右 9.371，⛔ 不是「居中」的 8.763）");
                                CheckNear(oCY - bCY, -0.760f, 0.3f,
                                          $"★ …`{oNames[oi]}` 的子件矩形中心 **比按钮中心偏上 0.760**"
                                          + "（原版上内缩 7.978 / 下 9.498，⛔ 不是「居中」的 8.7425）");
                            }
                        }
                        // ④ 🔴 **深度 / 胜负**（A180 那条的同一手法，但**方向相反**）：外扩之后**相邻两颗的
                        //    命中区重叠**（**原版如此**：`96.865 − 93.976` ≈ **2.888px**；我们 2.884 ——
                        //    ⚠️ **2026-10-11（A308）就地订正**：原来把这 0.004px 归给「子件矩形那 0.6px 的亚像素差」，
                        //    那个**位置**差 A308 已收掉；剩下的是 `OptIconW` 的四舍五入（56.86 vs 解出的 56.8636）
                        //    ⇒ `96.86 − 93.976` vs `96.8636 − 93.976`）⇒ 那条带里谁赢**只能靠兄弟序**。
                        //    原版判据 = uGUI「**深度大者先**」（`Library/PackageCache/…/EventSystem/EventSystem.cs:239-240`
                        //    `return rhs.depth.CompareTo(lhs.depth)`；`Graphic.depth` = `canvasRenderer.absoluteDepth`
                        //    = 层级遍历序）⇒ **树序靠后的那颗赢** = 视觉上**靠左**的那一颗（`Delete` 是 `m_Children`
                        //    最后一个、`m_ReverseArrangement=1` ⇒ 它在最左）。
                        //    我们这侧 = **队列 → z**（`Shell/PointerLayer.cs` 的 `HitButton`：同队列比 z、
                        //    **越小越靠前**）⇒ 树序靠后的必须 z **更小**。
                        //    ⛔ 期望值不写常量：这里比的是**两颗 quad 的 z 相对大小**（题面即断言）。
                        //    改坏法：删掉 `Shell/DeckInfoPopup.cs` 第 7) 节那两句 z ⇒ 五个 z 全相等 ⇒ 红
                        //    （那就退回「看 `FindObjectsByType` 返回序」的运气判）。
                        float[] zOf = new float[oNames.Length];
                        for (int zi = 0; zi < oNames.Length; zi++)
                        {
                            var zT = pop.Opt(oNames[zi]);
                            var zQ = zT != null ? zT.GetComponentInChildren<ImageQuad>() : null;
                            zOf[zi] = zQ != null ? zQ.transform.position.z : float.NaN;
                        }
                        bool zOk = true;
                        string zTrace = "";
                        for (int zi = 0; zi + 1 < zOf.Length; zi++)
                        {
                            if (!(zOf[zi] > zOf[zi + 1])) zOk = false;      // 树序靠后 ⇒ z 更小 ⇒ 更靠前
                            zTrace += (zi > 0 ? " > " : "") + oNames[zi] + " " + zOf[zi].ToString("F4");
                        }
                        zTrace += " > " + oNames[oNames.Length - 1] + " " + zOf[zOf.Length - 1].ToString("F4");
                        CheckTrue(zOk,
                                  "★ A299：`Deck Options` 五颗的**深度序 = 原版兄弟序**（树序靠后的 z **更小** = 更靠前）"
                                  + " —— 原版 uGUI 是「深度大者先」，`Delete` 的树序最后 ⇒ 重叠带里它赢；实测 z 序：" + zTrace
                                  + "（删掉 `Shell/DeckInfoPopup.cs` 第 7) 节那两句 z ⇒ 五颗全同 z ⇒ 这条红）");
                        // ⑤ 跨组：原版窗根 `m_Children` 序 = `… Deck Options(5) → Info Panel(6) → Close(7)`
                        //    ⇒ 关窗钮比**整组**五颗都深 ⇒ 我们这五颗**一律排在关窗钮之后（z 更大）**。
                        //    改坏法：把这五颗也改成「往前挪」（负 z）⇒ 红，并且 A180 那条「关窗钮更深」跟着红。
                        var oCl = FindChild(pr, "CloseHit");
                        var clq = oCl != null ? oCl.GetComponentInChildren<ImageQuad>() : null;
                        if (clq != null)
                        {
                            bool zBehind = true;
                            string zBTrace = "";
                            for (int zi = 0; zi < zOf.Length; zi++)
                            {
                                if (!(zOf[zi] > clq.transform.position.z)) zBehind = false;
                                zBTrace += (zi > 0 ? ", " : "") + oNames[zi] + " " + zOf[zi].ToString("F4");
                            }
                            CheckTrue(zBehind,
                                      $"★ A299：五颗**全在关窗钮之后**（z 更大；关窗钮 z {clq.transform.position.z:F4}）—— "
                                      + "原版窗根最后一个子件是关窗钮 ⇒ 它最深；实测：" + zBTrace
                                      + "（把这五颗改成「往前挪」= 负 z ⇒ 这条红，A180 那条也跟着红）");
                        }
                        // ⑥ 🔴 **真命中路**（判别力最强的一条）：在那条 2.888px 重叠带里真问一次「谁赢」。
                        //    取点 (1417.2, 168.0)：**同时**落在 `Delete` 与 `Share On Chat` 的命中区里
                        //    （原版带 x∈[1415.46, 1418.35]；我们带 A308 之后 = **x∈[1415.46, 1418.34]**
                        //      —— ⚠️ **2026-10-11（A308）就地订正**：这里原来写「我们带 [1416.07, 1418.95]」，
                        //      那是子件矩形还没按实读内缩（左 8.151）画之前的值；取点 1417.2 在新旧两条带里
                        //      都是交集中心 ⇒ **不必改那个字面量**；
                        //      y=168.0 是五颗的按钮中心 = 两条带都含）。原版那里点下去是 **`Delete`**（树序最后一个）。
                        //    ⚠️ 这条同时兜住「z 打平 ⇒ 看 `FindObjectsByType` 返回序（**不保证**）」那种运气判。
                        //    改坏法：删 z 那两句 / 把胜负方向搞反 / 把 `Delete` 的命中区摆错 ⇒ 拿到别的钮或 null ⇒ 红。
                        var plA299 = PointerLayer.Instance;
                        if (plA299 != null)
                        {
                            var oWin = plA299.ButtonAt(1417.2f, 168.0f);
                            CheckTrue(oWin != null && oWin.name == "Opt_Delete",
                                      "★ A299：重叠带里**靠左那颗（`Delete`）赢** —— 原版靠兄弟序（它是 `m_Children` "
                                      + "最后一个 = 最深），我们靠 z；实得「"
                                      + (oWin == null ? "**null**（这一点上谁都没中）" : oWin.name) + "」");
                            // 反向对照：另一颗的**中心**（远离重叠带，1464.5 = `Share On Chat` 按钮中心 x）
                            // 必须还是它自己 —— 防「赢家恒为某一颗」这种假绿。
                            var oWin2 = plA299.ButtonAt(1464.5f, 168.0f);
                            CheckTrue(oWin2 != null && oWin2.name == "Opt_Share On Chat",
                                      "★ …反向对照：远离重叠带的 `Share On Chat` **中心**拿到的还是它自己（实得「"
                                      + (oWin2 == null ? "**null**" : oWin2.name) + "」）");
                        }
                    }

                    Check(DeckInfoPopup.ListCols, 3,
                          "卡列表列数 = **3** = floor((1140 − 15 − 15 + 11) ÷ 371)（照 GridLayoutGroup 那套算）");
                    CheckTrue(pop.Rows.Count > 0, $"卡组内容画了 {pop.Rows.Count} 行");
                    var r0 = pop.Rows.Count > 0 ? pop.Rows[0] : null;
                    CheckNear(r0 != null ? PxOf(r0.position.x) : -1f, 854f, 1f,
                              "第 1 行中心 x = **854**（Info Panel 左 659 + pad 15 + 180）");
                    CheckNear(r0 != null ? PxYOf(r0.position.y) : -1f, 247.10f, 1f,
                              "第 1 行中心 y = **247.10**（218.10 + 58/2）");

                    // ---------------- 🆕 2026-10-03（§三第29条 A10）补的四件 + 两处订正 ----------------
                    //   逐值 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`
                    //   🔴 两个**新抓到的真缺陷**：`Deck Details` 整块原来用的是**布局组跑之前的模板位**、
                    //      `Deck Options` 顺序反了（上面那条已改）。这里把订正后的值钉住。
                    Section("`Deck info Popup` A10：游戏模式图标 / `Deck Info` 抽屉 / 两个抽屉切换 / 两处订正");
                    {
                        // ① `Deck Details` 跑后真值（原来那套是模板位 ⇒ Army Icon 画到容器外）
                        CheckAt(FindChild(pr, "Army Icon"), 767.9f, 867.9f, 106.7f, 216.7f,
                                "`Army Icon`（**跑后**真值 —— 原来那个 363.10 落在 `Deck Details` 容器之外）");
                        CheckAt(FindChild(pr, "Game Mode Separator"), 759.0f, 767.9f, 106.7f, 216.7f,
                                "`Game Mode Separator`（跑后真值）");
                        // ⚠️ `Deck Name` / `Warlord Name` 是 **`Align.Left`** ⇒ `AlignLeftOn` **会把 Label 的节点挪走**
                        //    ⇒ 拿节点中心去比 `CheckAt` 会差 100+px（第一版就是这么红的）。
                        //    **量它渲出来的左缘**（同 `已知的 MainMenuScene` 那两句的写法）。
                        {
                            var dnT = FindChild(pr, "Deck Name");
                            var dnl = dnT != null ? dnT.GetComponentInChildren<Label>() : null;
                            if (dnl != null)
                                CheckNear(PxOf(dnl.transform.position.x) - dnl.WorldW * 54f, 872.9f, 2f,
                                          "`Deck Name` 左缘 = **872.9**（跑后真值 · 原版 hAlign=Left）");
                            var wnT = FindChild(pr, "Warlord Name");
                            var wnl = wnT != null ? wnT.GetComponentInChildren<Label>() : null;
                            if (wnl != null)
                                CheckNear(PxOf(wnl.transform.position.x) - wnl.WorldW * 54f, 872.9f, 2f,
                                          "`Warlord Name` 左缘 = **872.9**（同上）");
                        }
                        // ② `Game Mode Icon`（A10 补的第一件）
                        var gm = FindChild(pr, "Game Mode Icon");
                        var gmq = gm != null ? gm.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(gmq != null && gmq.Texture != null
                                  && gmq.Texture.name == "40k_gamemode_icon_classic",
                                  "`Game Mode Icon` = `40k_gamemode_icon_classic`（这副是经典模式；"
                                  + "原来记的「本地没图」是**过期**的 —— 两张图本来就在工程里）");
                        CheckAt(gm, 659.0f, 759.0f, 106.7f, 216.7f, "`Game Mode Icon` 的矩形（跑后真值）");
                        // ③ 两个抽屉：出厂只有 `Deck List` 开
                        var dlist = FindChild(pr, "Deck List");
                        var dinfo = FindChild(pr, "Deck Info");
                        CheckTrue(dlist != null && dlist.gameObject.activeSelf, "`Deck List` 出厂开着");
                        CheckTrue(dinfo != null && !dinfo.gameObject.activeSelf,
                                  "`Deck Info` **建成但关着**（原版出厂 `INACT`）");
                        CheckTrue(!pop.InfoDrawerShown, "自检读数：现在展示的是 `Deck List`");
                        // ④ `Switch Deck Info` ⇒ 真切（两个互斥）
                        var swHit = pop.Opt("Switch Deck Info");
                        var swWb = swHit != null ? swHit.GetComponent<WindowButton>() : null;
                        CheckTrue(swWb != null, "`Switch Deck Info` 有点击区");
                        if (swWb != null)
                        {
                            swWb.ClickForTest();
                            CheckTrue(pop.InfoDrawerShown, "点它 ⇒ 切到 `Deck Info`");
                            CheckTrue(!FindChild(pr, "Deck List").gameObject.activeSelf
                                      && FindChild(pr, "Deck Info").gameObject.activeSelf,
                                      "两个抽屉**互斥**（`Deck List` 关了、`Deck Info` 开了）");
                            // ⑤ `Deck Info` 里的三件：标题 / 费用曲线 9 行 / 卡背
                            var di = FindChild(pr, "Deck Info");
                            CheckText(TextOf(FindChild(di, "Deck Information Cost/balance text")), "Cards / Cost",
                                      "费用那条标题（⚠️ 原版 prefab 里是**葡语占位** `Cartas / Coste` ⇒ 这行文案是我们挑的）");
                            Check(pop.CostRowCounts.Count, 9, "费用曲线 **9 行**（费用 0..8 —— 原版序列化就是 9 行）");
                            int sum = 0; for (int k = 0; k < pop.CostRowCounts.Count; k++) sum += pop.CostRowCounts[k];
                            CheckTrue(sum > 0, $"…曲线里**真的有张数**（合计 {sum} 张；这副的卡都算进去了）");
                            var cbk = FindChild(di, "Cardback");
                            var cbq = cbk != null ? cbk.GetComponentInChildren<ImageQuad>() : null;
                            // ⚠️ 卡背图取不到是**已知缺口**（`Art/cards/back_*.png` 只有 4 个阵营 ⇒
                            //    多数阵营**没有「默认卡背」**，正本 §七 ③ 记着）⇒ 这条只断
                            //    「**有图就必须画出来**」；取不到时 `DeckInfoPopup` 会**出声**（日志里那条
                            //    「卡背取不到（`` / 阵营 X）⇒ 那一层不画」），**不是静默**。
                            var cbInfo = CollectionData.DeckAt(0);
                            var cbTex = CardArt.DeckCardback(cbInfo.CardbackId, cbInfo.Faction);
                            CheckTrue(cbTex == null || (cbq != null && cbq.Texture != null),
                                      "`Cardback`：**有图就必须画出来**（实测图 "
                                      + (cbTex != null ? "有" : "**没有**（已知缺口，已出声）") + "）");
                            CheckTrue(FindChild(di, "Lore Text") == null,
                                      "`Lore Text` **不建**（原版出厂 `act=N` + 我们引擎没有 lore 字段）");
                            swWb.ClickForTest();
                            CheckTrue(!pop.InfoDrawerShown, "再点 ⇒ 切回 `Deck List`");
                        }
                        // ⑥ 督军立绘可点（原版 `EverguildButton` ⇒ 开卡详情窗）
                        var wh = FindChild(pr, "WarlordHit");
                        CheckTrue(wh != null && wh.GetComponent<WindowButton>() != null,
                                  "`Warlord Image` 上有点击区（原版那层就是 `EverguildButton`）");
                        // 🆕 **2026-10-07（波 4 件① A142②）：立绘命中区的四条边。**
                        //   判据 = 原版 prefab `Deck info Popup > Warlord Image` 那颗 `Image` 的
                        //   **`m_RaycastPadding`（UGUI 分量序 **L, B, R, T**）** ：
                        //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7131536541767857752.json`
                        //   = `(246.8, 84.44, 338.6, 132.38)`（`m_GameObject` 指回
                        //   `GameObject/Warlord Image_-6735770576364533336.json`）。
                        //   那颗的 rect（同目录 `RectTransform/RectTransform_8411164374367242664.json`：
                        //   anchor (0.5,0.5) · `m_AnchoredPosition = (-514.981, -534.0)` ·
                        //   `m_SizeDelta = 1107.994²` · `m_Pivot = (0.5, 0.0)`）= `-108.978,-33.994 → 999.016,1074.0`
                        //   ⇒ **命中区 = 四边各往里缩**（正分量 = 缩；符号判据见实现侧 `WarlordHit` 那段）：
                        //     x1 = −108.978 + 246.80 = **137.82** · y1 = −33.994 + 132.38 = **98.39**
                        //     x2 =  999.016 − 338.60 = **660.42** · y2 = 1074.000 − 84.44 = **989.56**
                        //   ⛔ 这四个数写**原版 prefab 复算出来的字面量**，⛔ **不写** `DeckInfoPopup.WarlordPad` /
                        //      `WarlordL..B` —— 那是被测实现传进去的实参（同式自证：改实现它照样绿）。
                        //   ⚠️ **与立绘 `Img` 的 `keepAspect` 无关** —— 命中区只由 `WarlordPad` 决定；
                        //      「渲染矩形拉满 1108²」是**另一条**（下面那段），两条分开、别混在一句里。
                        const float WnL = 137.82f, WnT = 98.39f, WnR = 660.42f, WnB = 989.56f;
                        var wq = wh != null ? wh.GetComponentInChildren<ImageQuad>() : null;
                        if (wq == null)
                            CheckTrue(false, "`WarlordHit` 下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 立绘等于点不动）");
                        else
                        {
                            float wqW = wq.WorldW * 108f, wqH = wq.WorldH * 108f;
                            float wqX = LayoutSpace.PxX(wq.transform.position.x);
                            float wqY = LayoutSpace.PxY(wq.transform.position.y);
                            CheckNear(wqX - wqW * 0.5f, WnL, 1.5f,
                                      "立绘命中区**左沿** = **137.82**（原版 `m_RaycastPadding` 复算 · 1108² 里那 246.8 的缩）");
                            CheckNear(wqY - wqH * 0.5f, WnT, 1.5f, "…**上沿** = **98.39**");
                            CheckNear(wqX + wqW * 0.5f, WnR, 1.5f, "…**右沿** = **660.42**");
                            CheckNear(wqY + wqH * 0.5f, WnB, 1.5f, "…**下沿** = **989.56**");
                        }
                        // 🆕 **2026-10-07（波 4 件① A142①）：立绘【渲染】矩形 = 原版那样拉满 1108²。**
                        //   判据 = 原版那颗 `Image` 的 **`m_PreserveAspect = 0`** + `m_Type = 0`(Simple)
                        //   + rect `1107.994²`（同一份 JSON，见上）⇒ **拉伸画满**，不是等比内接。
                        //   我们原来传 `keepAspect: true` ⇒ 按贴图自己的比例缩、左右留边
                        //   （督军那族贴图是 671×1024 ⇒ 宽只剩 ≈ 1108×671/1024 ≈ **726**）。
                        //   🔴 这是那处改动的**唯一自动化尺子**（批处理能量几何、量不到「像不像」——
                        //     观感差仍归 `资料/真Play待验清单.md`）。
                        var wlImg = FindChild(pr, "Warlord Image");
                        var wlQ = wlImg != null ? wlImg.GetComponentInChildren<ImageQuad>() : null;
                        if (wlQ == null)
                            CheckTrue(false, "`Warlord Image` 那一层没建出来（督军立绘取不到 ⇒ 实现里已经出声）");
                        else
                        {
                            CheckNear(wlQ.WorldW * 108f, 1108f, 2f,
                                      "立绘**渲染宽度** = **1108**（原版 `m_PreserveAspect = 0` 拉满；"
                                      + "退回 `keepAspect: true` 时这里 ≈ 726 ⇒ 这条红）");
                            CheckNear(wlQ.WorldH * 108f, 1108f, 2f, "立绘**渲染高度** = **1108**（同上）");
                        }
                        if (wh != null)
                        {
                            wh.GetComponent<WindowButton>().ClickForTest();
                            // ⚠️ 用 `DeckInfoPopup.LastWarlordDetail`，**不是** `CardDetailPopup.LastOpened`
                            //    （后者只在 `RebuildKeepingState()` 里赋值 ⇒ 这条路拿到的恒是 null）
                            var wd = DeckInfoPopup.LastWarlordDetail;
                            CheckTrue(wd != null && wlc != null && wd.Card != null && wd.Card.Name == wlc.Name,
                                      "点它 ⇒ 开督军的**卡片详情窗**（「" + (wlc != null ? wlc.Name : "?") + "」）");
                            if (wd != null) wd.Close();
                        }
                        // ⑦ `Share` / `Share On Chat`：给卡组串（原版走平台/服务端）
                        var shHit = pop.Opt("Share");
                        CheckTrue(shHit != null && shHit.GetComponent<WindowButton>() != null, "`Share` 有点击区");
                        Debug.Log(P + "   卡组串自检：Share 的实现在 `DeckInfoPopup.ShareDeck`");
                    }
                    Shoot("05_收藏_DeckInfo弹窗.png");
                    var popCloseHit = FindChild(pr, "CloseHit");
                    var popCloseWb = popCloseHit != null ? popCloseHit.GetComponent<WindowButton>() : null;
                    CheckTrue(popCloseWb != null, "关闭圆钮有点击区");
                    // 🆕 **2026-10-07（波 4 件① A126）：这颗带按钮脸的关窗钮，原来一条断言都没有。**
                    //   三条各补一样（① 矩形四边 · ② 档位 · ③ 脸图绑定），逐条说判据：
                    //
                    //  ① **命中区四条边** —— 🔴 **2026-10-11（A180）本条已按原版的【射线那一面】改写**。
                    //    原版那颗按钮**自己的 `Image` 不吃射线**（`m_RaycastTarget = 0`，pad 全 0），
                    //    吃射线的是**两个子件** `Background` / `Icon`（`m_RaycastTarget = 1`）——
                    //    而这两颗都带 **`m_RaycastPadding = (−20,−20,−20,−20)`**（**负 = 外扩**）。
                    //    ⇒ 期望 = **子件 rect 外扩 20**，**不是**按钮那颗 `RectTransform` 的矩形
                    //      （旧断言写的就是按钮矩形 `1782.81/63.20/1857.19/138.80` ⇒ 比原版**每边小 ≈20px** = A180 本身）。
                    //    判据（解包实读）：
                    //      · 按钮自身 Image `MonoBehaviour_3635446896823339432.json` = `m_RaycastTarget 0`；
                    //      · 两个子件 `MonoBehaviour_-583606675015890520.json`（`Background`）/
                    //        `…_7496897533368830376.json`（`Icon`）= `m_RaycastTarget 1` · pad `(−20,…)`；
                    //      · 子件 rect（`menu_dump … "Deck info Popup" --depth 4`）= **`1791.0, 71.2 → 1847.8, 129.3`**
                    //        ⇒ 外扩 20 = **`1771.0, 51.2 → 1867.8, 149.3`（96.8 × 98.1）**。
                    //    复算 —— ✅ **2026-10-12（A329）订正**：原文写「我们那两层画在
                    //      `CloseL + (CloseR−CloseL−OptIconW)/2 = 1791.573` ⇒ `1771.573, 51.18 → 1868.433, 149.30`
                    //      ⇒ 与原版差 **0.573px**（那条亚像素差早已记账）」。那是 **A329 之前**的状态：
                    //      A329 把那颗的 `fx1` 改成 `CloseL + OptInL = 1790.961`（与 `Deck Options` 五颗同一套
                    //      内缩，判据 → `Shell/DeckInfoPopup.cs` 第 8) 节与 `OptInL` 那段）⇒
                    //      现在 = **`1770.961, 51.18 → 1867.821, 149.30`**，四边与原版差 **≤0.039px**
                    //      （中心 0.009 / 0.010）⇒ 下面**五条容差同批从 1.5 收到 0.3**（0.039 的残差 vs 0.612 的缺陷）。
                    //    ⛔ 期望值写**原版复算出来的字面量**，⛔ **不写** `DeckInfoPopup.ClosePad` / `CloseL/T/R/B`
                    //     —— 那是被测实现**传进去的实参**，拿它当期望 = 同式自证（改实现它跟着绿）。
                    //    ⚠️ 节点与 quad 都量：`CheckAtPx` 管**节点**（2026-09-23 那个「节点全停在容器 (0,0)、
                    //        只有里面的 quad 画对了」的坑就靠它抓），四条边管**画出来那张**。
                    //    🔴 **2026-10-11（F6）这一条改走【像素空间 2D】**（原来是 `CheckAt` —— 3D、**含 z**）。
                    //       A180 给这颗节点显式前移了 z（`Shell/DeckInfoPopup.cs` 的 `HitZFront = 0.01`
                    //       = **1.08px**，落在第 8) 节末的 `localPosition -= (0,0,HitZFront)`；那把尺子**是对的**
                    //       —— 就是下面 ①-b 那条「比邻居更深」的相对断言），而 `CheckAt` 的容差**恰好就是**
                    //       0.01 世界单位 = **1.08px** ⇒ **光 z 这一项一个人就吃满**；再加 x 的 0.603px
                    //       （= 上面那句 0.573px 亚像素差的同一条）⇒ `sqrt(0.603² + 1.08²)` = **1.24px**
                    //       ⇒ **恒红**（`_tmp_view/collection.log:3970`）。**那是口径错，不是缺陷。**
                    //       ⛔ 不把 `CheckAt` 改软（全文件 24 处共用，本轮不动）—— **这一处**改成与下面
                    //       四条边**同一个口径**：`LayoutSpace.ToPixel` 比中心 · **z 不进比较**。
                    //       判别力不变：它唯一的增量是抓「节点停在容器 (0,0)」那种**差几百 px** 的错。
                    //       改坏法：把 `closeHitNode` 的 x/y 写成 0（节点落到世界原点 = 画布中心 (960,540)）
                    //       ⇒ 差 **(−859.40, +439.75) px** ⇒ 红。
                    //       ✅ **2026-10-12（A329）订正容忍度**：F6 当时给 1.5px，理由是「**0.603px 的亚像素差**
                    //         与 1.08px 的（有意）z 前移都不该被算成位置错」—— A329 把那个 0.603px 收掉之后
                    //         （残差 0.009px），**这条理由不存在了** ⇒ 与同族 A308 对齐收成 **0.3px**
                    //         （z 本来就不进这个比较，不受影响）。
                    const float ChL = 1771.0f, ChT = 51.2f, ChR = 1867.8f, ChB = 149.3f;
                    const float ChCx = (ChL + ChR) * 0.5f, ChCy = (ChT + ChB) * 0.5f;   // = 1819.40 / 100.25
                    //  🔴 **2026-10-12（A329）：容差 1.5 → 0.3**（上面那句「容差 1.5px 的取法」是 F6 当时的口径，
                    //    当时的理由是「0.603px 的亚像素差…」—— A329 把那一处收掉之后这条理由**不存在了**）：
                    //    现在实测残差 = **中心 (0.009, 0.010) · 四边 (0.039, 0.020, 0.021, 0.000)**，
                    //    而**缺陷那一档**是 0.612px（x 侧两条边 0.573 / 0.633）⇒ 0.3 正夹在中间，
                    //    与同族 A308 那两条（`|Δ| ≤ 0.3`，`:1670`/`:1673`）**同一个口径**。
                    //    改坏法（A329 那一行退回「居中」`(r.W − OptIconW) * 0.5f`）⇒ **左/右两条边红**
                    //      （0.573 / 0.633 > 0.3），上/下两条仍绿（y 侧本来就没这个毛病，见 `DeckInfoPopup` 第 8) 节）。
                    CheckAtPx(popCloseHit, ChCx, ChCy, 0.3f,
                              "关闭钮命中区**节点**在原版**射线区**中心（子件 rect 外扩 20 后复算；节点没摆对就红）");
                    var chQ = popCloseHit != null ? popCloseHit.GetComponentInChildren<ImageQuad>() : null;
                    if (chQ == null)
                        CheckTrue(false, "关闭钮命中区下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这颗等于点不动）");
                    else
                    {
                        float chW = chQ.WorldW * 108f, chH2 = chQ.WorldH * 108f;
                        float chCx = LayoutSpace.PxX(chQ.transform.position.x);
                        float chCy = LayoutSpace.PxY(chQ.transform.position.y);
                        CheckNear(chCx - chW * 0.5f, ChL, 0.3f, "关闭钮命中区**左沿** = **1771.0**（子件 1791.0 外扩 20）");
                        CheckNear(chCy - chH2 * 0.5f, ChT, 0.3f, "…**上沿** = **51.2**");
                        CheckNear(chCx + chW * 0.5f, ChR, 0.3f, "…**右沿** = **1867.8**");
                        CheckNear(chCy + chH2 * 0.5f, ChB, 0.3f, "…**下沿** = **149.3**");
                        // 🔴 **2026-10-11（A180）两条【相对】断言** —— 上面四条只钉「摆在哪」，
                        //   这两条钉「**大小**」：外扩是加在**子件矩形**（56.86×58.12）上的，
                        //   ⛔ 不是加在按钮矩形（74.386×75.605）上。三种错法各红一条：
                        //     · 去掉外扩（pad 写 `Vector4.zero`）⇒ 74.386 / 75.605 ⇒ 两条都红（= A180 原状）；
                        //     · 把 pad 加在**按钮矩形**上 ⇒ 114.386 / 115.605 ⇒ 两条都红（**每边多 ≈9px**，
                        //       那 9px = `(74.386 − 56.86)/2` 那道内缩，也是「看着像对了」的那一档）。
                        CheckNear(chW, 96.86f, 1.5f,
                                  "★ A180：命中区**宽 = 原版 96.86**（= 子件宽 56.86 + 2×20 外扩）");
                        CheckNear(chH2, 98.13f, 1.5f,
                                  "★ A180：命中区**高 = 原版 98.13**（= 子件高 58.12 + 2×20 外扩）");
                        //  ② **档位 = `QDIHit` 3123**（⛔ 写字面量、不写常量名）：常量与实现同源，
                        //     只改常量那种写法会跟着一起动、照样绿（= 丙-3 报告 §四那笔「自证残余」的修法）。
                        //     语义：它必须**压在压暗层 `QDI`(3120) 之上**，否则「点窗外关窗」那颗会先吃到命中。
                        Check(chQ.RenderQueue, 3123,
                              "关闭钮命中区的档 = `QDIHit` **3123**（字面量钉死；同档的还有 8 颗钮的命中区与 "
                              + "`WarlordHit` —— 它们都要在压暗层的 3120 之上）");
                    }
                    //  ①-b 🔴 **2026-10-11（A180）外扩带来的那条重叠带 —— 谁赢必须与原版同向**。
                    //    外扩之后关窗钮与邻居 `Switch Deck Info Button` 的命中区重叠 ≈12×19px
                    //    （原版 ≈24×30px —— 它邻居那颗**也是**子件外扩，实测同一套模型）。
                    //    原版靠**深度**定胜负：解包实读窗根 `Deck info Popup` 的 `m_Children` 顺序 =
                    //    `Menu Dark Background(0) → Generic Window Red Background Big(1) → Warlord Image(2) →
                    //     Deck Details(3) → Buttons(4) → Deck Options(5) → Info Panel(6) → Generic Close Button Orange(7)`
                    //    ⇒ **关窗钮是最后一个子件、最深** ⇒ 那条带里点下去**应当关窗**。
                    //    我们这一侧的「谁压谁」= **队列 → z**（`Shell/PointerLayer.cs` 的 `HitButton`：
                    //    同队列再比 z、**越小越靠前**），而本窗 9 颗命中区**全在 3123、z 又全相等** ⇒ 打平，
                    //    胜负落在 `FindObjectsByType` 的返回序上（**不可靠**）。
                    //    ⇒ 本窗 `Build()` 第 8) 节把那颗节点的 z 显式往前挪一点点（相对量）。
                    //    ⛔ 期望值不写那个常量：这里比的是**两颗 quad 的 z 相对大小**（题面即断言）。
                    var optHitT = FindChild(pr, "Opt_Switch Deck Info");
                    var optQ = optHitT != null ? optHitT.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(optQ != null,
                              "（前提）邻居 `Switch Deck Info` 那颗命中区在（下面那条深度对照靠它）");
                    if (chQ != null && optQ != null)
                    {
                        float zc = chQ.transform.position.z, zo = optQ.transform.position.z;
                        CheckTrue(zc < zo,
                                  $"★ A180：关窗钮的命中区**比邻居更深**（z {zc:F4} **<** {zo:F4}）"
                                  + " —— 原版那颗是窗根**最后一个**子件（`m_Children` 实测序 `… Deck Options(5) → "
                                  + "Info Panel(6) → Close(7)`），那条重叠带里点下去**应当关窗**；"
                                  + "删掉 `Shell/DeckInfoPopup.cs` 第 8) 节那两句 z 前移 ⇒ 两个 z 相等 ⇒ 红"
                                  + "（并退回「看 `FindObjectsByType` 返回序」的运气判 —— 同队列同 z 时先到者赢）");
                    }
                    //  ③ **脸图绑定** —— 判据 = 原版那颗 `Selectable`：
                    //     `m_Transition = 2`(SpriteSwap) · `m_TargetGraphic` → pid `-583606675015890520`
                    //     = 子件 **`Background`**（`m_Sprite` 解出来 **`40k_general_bt_yellow`**，`m_RaycastTarget = 1`）·
                    //     `m_SpriteState.m_HighlightedSprite` = **`40k_general_bt_yellow_hover`** ·
                    //     `m_PressedSprite` = `40k_general_bt_yellow_pressed`
                    //     （出处 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3635446896823339432.json`；
                    //      sprite 名由 `menu_dump.py` 的包内 sprite 索引解出，同一行还印出 `Icon` = `40k_general_bt_yellow_close`）。
                    //     ⇒ 换图那一层**必须是「按钮脸」那张**（`Close Face` = `40k_general_bt_yellow`），
                    //       ⛔ 不是圆底 `UI_Button_Round_background`、⛔ 不是 `Close Icon`、⛔ 不是那颗全透明命中区。
                    //     ⚠️ 与 ① 的**命中区无关**（那是 `Hit` 建的另一颗）—— 两条分开写，别混。
                    CheckTrue(popCloseWb != null && popCloseWb.target != null && popCloseWb.target.Texture != null
                              && popCloseWb.target.Texture.name == "40k_general_bt_yellow"
                              && popCloseWb.target.gameObject.name == "Close Face",
                              "关闭钮换图那一层 = 带按钮脸的 `Close Face`（图 **`40k_general_bt_yellow`**；原版 "
                              + "`m_TargetGraphic` 指的就是子件 `Background`）——实得「"
                              + (popCloseWb == null || popCloseWb.target == null || popCloseWb.target.Texture == null
                                 ? "<没绑>" : popCloseWb.target.gameObject.name + " / " + popCloseWb.target.Texture.name)
                              + "」");
                    // `art` 实参那一半：`WindowButton.Bind` 只存纹理、**不存那个字符串** ⇒ 从它算出来的
                    // 高亮图名字反推（`40k_general_bt_yellow` 的表外后备 = `…_hover`）。
                    // ⚠️ 悬停**真的换得动 / 离开真的还原**由本节开头那条 `CheckHoverSwap(pop.transform, …)` 管
                    //    （`AuditHoverSwap` 逐颗走过，含这一颗）—— 这里只核「绑的是哪张图」，别重复。
                    CheckText(popCloseWb != null && popCloseWb.HoverTexForTest != null
                              ? popCloseWb.HoverTexForTest.name : "<null>",
                              "40k_general_bt_yellow_hover",
                              "换图的 `art` 实参 = **`40k_general_bt_yellow`**（原版 `m_SpriteState.m_HighlightedSprite`）"
                              + " —— 把这个实参拿掉时这里取到 `<null>` ⇒ 红");
                    CheckTrue(popCloseWb != null && popCloseWb.onClick != null,
                              "关闭钮的 `onClick` 挂着（「有点击区」≠「点了有反应」；下面那条真点一次再断窗的状态）");
                    if (popCloseWb != null) popCloseWb.Click();
                    Check(pop.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗进 `Closed` 态");
                }
            }

            // ---------------- 🆕 2026-10-04（§三第29条 A31）：8 颗钮**按 state 显隐** ----------------
            //   判据（唯一出处）= `DeckInfoControls__Initialize.c` 的 SetActive :60-112 + 「offset → 节点名」
            //   对照表（**在 `Shell/DeckInfoPopup.cs` 文件头**）。逐颗：
            //     `Edit Deck` ← `state < 2` · `Select Deck` ← `context.SelectButton != null`
            //     · `Share`/`Share On Chat`/`Delete`/`Duplicate` ← `state == 0 && isPlayerDeck`（**四颗同一条**）
            //     · `Practice Deck` ← `isPlayerDeck && state ∈ {0,2}` · `Switch Deck Info` ← **常显**
            //   `isPlayerDeck` = 原版 **`InventoryManager.HasItem(context.Deck)`**（`DeckInfoPopup__Open.c:43-48`）
            //     —— 我们三处调用点拿的都是玩家自己的卡组 ⇒ 恒 true。
            //   🔴 **真红法**（2026-10-04 逐条推演订正过 —— X3 审查的 **R6**：原来说「任何一行写成恒真/恒假
            //     都必有一条红」**不成立**）：下面 ①～③ 那三条**只覆盖 state 0 与 state 2**，而
            //       · `Practice Deck` 这一行**两态都该露** ⇒ 写成恒 `true` 全绿；
            //       · `isPlayerDeck`（那个 `mine`）默认 true、**没有任何断言把它置 false** ⇒ 丢掉这个合取项全绿。
            //     ⇒ ④ / ⑤ 两条就是给它们补「该藏」的那一态（判据里有、我们的调用点没有，
            //       但字段是 public、自检造得出）。**改坏法**：把 `mine` 换成恒 `true` ⇒ ④ 红；
            //       把 `Practice Deck` 那行的 `state ∈ {0,2}` 换成恒 `true` ⇒ ⑤ 红；
            //       把 `state < 2` 换成 `state < 3` / `<= 1` ⇒ ⑤ 那条「`Edit Deck` 仍露」红。
            Section("`Deck info Popup`：8 颗钮**按 state 显隐**（A31）");
            {
                var mi0 = win.Manager;
                // ① state 2（View）= 原版 `DeckGeneralInfoDemo.CardInDeckInfoButtonOnClick` 那一态
                //    （= 我们的 `PracticeModePopup.ShowDeckContent` **该给**的那一态 —— 见文件头「仍欠」①）
                var v2 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.View);
                mi0.OpenWindow(v2);
                CheckTrue(v2 != null && v2.State == DeckInfoPopup.DeckInfoState.View, "开出一扇 `state = 2`（View）的");
                // 🆕 **2026-10-09（A229）**：`Shell/DeckInfoPopup.cs` 的 `Txt` helper 补了
                //   `basis == parent` 守卫（与同文件 `Nine` 同一条口径、同一句话）—— 这里造 **`basis != parent`**
                //   **真调一次**，断「**会出声**」（✅ 本仓有现成的「出声」断言范式：`Application.logMessageReceived`
                //   计数，先例 `Editor/RewardsScene.cs` 的 `hWarn` 那一段）。
                //   判据 = 本文件自己的约定（`DeckInfoPopup.Local3` 的注释 + `Nine` 的守卫）：坐标一律页面绝对 px、
                //   `basis` 与 `parent` 给同一个节点；不等 ⇒ 世界位置 = `RectCenter + (parent.position − basis.position)`。
                //   🔴 **改坏法**：删掉 `Txt` 开头那句 `if (!ReferenceEquals(basis, parent)) Debug.LogWarning(…)`
                //   ⇒ 反例读出 **0** 条警告 ⇒ 红（正例照样 0 ⇒ 这一条断的是**守卫本身**，不是「有没有警告」）。
                if (v2 != null)                                   // ⚠️ 窗都没开出来时，上面那条已经红了 —— 这里别 NRE
                {
                    var probeT = new GameObject("A229_TxtProbe", typeof(RectTransform)).transform;
                    probeT.SetParent(v2.transform, false);
                    probeT.localPosition = new Vector3(0.5f, 0.5f, 0f);
                    int wSame = 0, wDiff = 0; string lastW = null;
                    Application.LogCallback hSame = (c, s, t) =>
                    { if (t == LogType.Warning && c != null && c.Contains("`Txt` 的 `basis`")) wSame++; };
                    Application.logMessageReceived += hSame;
                    var lbSame = v2.TxtBasisProbeForTest(probeT, probeT);          // 正例：同一个 ⇒ 不许出声
                    Application.logMessageReceived -= hSame;
                    Application.LogCallback hDiff = (c, s, t) =>
                    { if (t == LogType.Warning && c != null && c.Contains("`Txt` 的 `basis`")) { wDiff++; lastW = c; } };
                    Application.logMessageReceived += hDiff;
                    var lbDiff = v2.TxtBasisProbeForTest(probeT, v2.transform);    // 反例：不同 ⇒ 必须出声
                    Application.logMessageReceived -= hDiff;
                    CheckTrue(lbSame != null && lbDiff != null, "A229：两次探针都**真调到了** `Txt`（都建出了标签）");
                    Check(wSame, 0, "A229 正例：`basis == parent` ⇒ **不出声**（守卫不误报）");
                    Check(wDiff, 1, "★ A229 反例：`basis != parent` ⇒ **出一条警告**"
                                    + "（读 0 = 守卫被删掉了）" + (lastW != null ? "；实得：" + lastW : ""));
                    Object.DestroyImmediate(probeT.gameObject);
                }
                // 🆕 A83②（A81 的尾巴）：压暗层（「点窗外关窗」）那条不变量 —— 档 = **压暗层自己那一档**
                //   `QDI`(3120)，**严格低于**本窗内容命中区档 `QDIHit`(3123)；并核「这节点确实是
                //   公共件 `MenuDraw.ShadeHit` 建的」。期望值全是本窗自己的**原版档常量**（⛔ 不从被测实现里读）。
                //   ⚠️ 它与本窗那颗**带按钮脸的** `CloseHit`（`Shell/DeckInfoPopup.cs` 的
                //   `Hit(root, root, "CloseHit", …)`，**现 `:847`**）**不是一件事**（两颗都要有）；
                //   ✅ **2026-10-07（波 4 件① A126）行号订正**：这里原来写的是旧号 `780`、并说源文件那两处注释
                //   「引的还是旧行号（已漂，本件没动它）」—— **三处都订正了**：源文件那两处注释
                //   （现在在 `:223` 那一块 / `:629` 那一块）与**本行**一律改成实读出来的 `:847`。
                //   错因：那颗节点被同批别的改动推下去过两次（旧号 `701` → `780` → 现 `847`），
                //   而引用它的注释**没跟着走**。⛔ 下次再引行号，先 `grep -n '"CloseHit"'` 实读一遍
                //   （⚠️ **本件自己又把它推下去了一次** —— 波 4 件① 在它上面加了注释块 ⇒ 822 → 847）。
                //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档（它由窗口自己那句
                //      `Solid(root, root, …, "Menu Dark Background")` 建，**另一处代码 + 另一个对象**）。
                MenuDraw.CheckShadeRule(CheckTrue, "卡组信息窗", v2.ShadeHit,
                                        v2.transform.Find("Menu Dark Background"), DeckInfoPopup.QDIHit);
                CheckTrue(!v2.IsItemShown("Btn:Edit Deck"), "state 2 ⇒ `Edit Deck` **藏起来**（原版 `state < 2`）");
                CheckTrue(!v2.IsItemShown("Opt:Share") && !v2.IsItemShown("Opt:Share On Chat")
                          && !v2.IsItemShown("Opt:Delete") && !v2.IsItemShown("Opt:Duplicate"),
                          "state 2 ⇒ **四颗同一条**（`Share` / `Share On Chat` / `Delete` / `Duplicate`）全藏");
                CheckTrue(v2.IsItemShown("Btn:Practice Deck"),
                          "state 2 ⇒ `Practice Deck` **仍露**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                CheckTrue(v2.IsItemShown("Opt:Switch Deck Info"), "`Switch Deck Info` **常显**（原版那件没看到 SetActive）");
                CheckTrue(v2.Btn("Edit Deck") != null && v2.Opt("Delete") != null,
                          "…藏起来的那几颗**节点还在**（是 `SetActive(false)`，不是没建 —— 位置/命中区都还按原版摆着）");
                // ② **同一条路的两态**：只把 `State` 改回 0 再摆一次 ⇒ 那五颗必须回来
                v2.State = DeckInfoPopup.DeckInfoState.Edit;
                v2.ApplyStateVisibility();
                CheckTrue(v2.IsItemShown("Btn:Edit Deck") && v2.IsItemShown("Opt:Delete")
                          && v2.IsItemShown("Opt:Duplicate") && v2.IsItemShown("Opt:Share")
                          && v2.IsItemShown("Opt:Share On Chat"),
                          "把 `State` 改回 **0** ⇒ `Edit Deck` 与 4 颗圆钮**都回来了**"
                          + "（同一份实现的两态对比 —— 写成恒真/恒假这里就红）");
                // ③ `Select Deck`：**给了 `context.SelectButton` 才露**（原版 6 个调用点全传 null ⇒ 我们默认恒藏）
                var v3 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                mi0.OpenWindow(v3);
                CheckTrue(v3.IsItemShown("Btn:Select Deck"),
                          "给了 `SelectButton` ⇒ `Select Deck` **露**（原版 `context.SelectButton != null`）");
                CheckTrue(!v2.IsItemShown("Btn:Select Deck"),
                          "…而没给的那一扇**不露**（两条一起才是判据；只断一条分不出「恒露」和「按判据露」）");
                // ④ 🔴 **`isPlayerDeck` 那一半**（2026-10-04 补，X3 审查的 **R6**）：原版四颗圆钮与
                //    `Practice Deck` 都**与**了 `isPlayerDeck`（= `InventoryManager.HasItem(context.Deck)`，
                //    `DeckInfoPopup__Open.c:43-48`）。我们的三处调用点恒 true ⇒ 上面 ①～③ **抓不到**
                //    「把这个合取项丢掉」。这里造一扇 `IsPlayerDeck = false` 的（字段是 public，
                //    ⚠️ 与铁律 5·c 同理：**一个值 ≠ 全部情况**）。
                var v4 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Edit);
                v4.IsPlayerDeck = false;                  // Create 之后、OpenWindow（会跑 Build→ApplyStateVisibility）之前
                mi0.OpenWindow(v4);
                CheckTrue(!v4.IsItemShown("Opt:Share") && !v4.IsItemShown("Opt:Share On Chat")
                          && !v4.IsItemShown("Opt:Delete") && !v4.IsItemShown("Opt:Duplicate"),
                          "`isPlayerDeck = false` ⇒ 四颗圆钮**全藏**（原版 `state == 0 && isPlayerDeck` 的**后半条**）"
                          + " —— 把 `mine` 写成恒 `true` 只有这一条会红");
                CheckTrue(!v4.IsItemShown("Btn:Practice Deck"),
                          "…而且 `Practice Deck` **也藏**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                CheckTrue(v4.IsItemShown("Btn:Edit Deck"),
                          "…但 `Edit Deck` **照旧露**（原版只看 `state < 2`，与 `isPlayerDeck` 无关 —— 别把它也乘进去）");
                // ⑤ 🔴 **`Practice Deck` 那一行不能恒 true**（R6 的反例①）：state 0 与 state 2 **两态都该露**
                //    ⇒ 上面 ①～③ 一条都抓不到它。state 1（Import）是判据里「该藏」的那一态
                //    （原版 `DeckInfoControls__Initialize.c:84-97` 有这条；我们**没有**这个调用点，
                //     但字段是 public ⇒ 自检造得出来）。
                var v5 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Import);
                mi0.OpenWindow(v5);
                CheckTrue(!v5.IsItemShown("Btn:Practice Deck"),
                          "state 1（Import）⇒ `Practice Deck` **藏**（原版 `state ∈ {0,2}`）"
                          + " —— 只看 state 0/2 的话这一行写成恒 `true` 也全绿");
                CheckTrue(v5.IsItemShown("Btn:Edit Deck"),
                          "…而 `Edit Deck` **仍露**（`state < 2`）—— 与 state 2 正好两态对比"
                          + "（判据写成 `state < 3` 或 `<= 1` 都在这一条上红）");
                CheckTrue(!v5.IsItemShown("Opt:Delete") && !v5.IsItemShown("Opt:Share"),
                          "…四颗圆钮藏（`state == 0` 那一半）");
                CheckTrue(v5.IsItemShown("Opt:Switch Deck Info"),
                          "…`Switch Deck Info` **常显**（三个 state 都一样，原版那件没看到 SetActive）");
                // ⚠️ **R13（本轮没修，改点在 `Shell/DeckInfoPopup.cs:270-274`）**：`IsItemShown` 读的是
                //    **该节点的 `activeSelf`**、父链不参与 ⇒ 上面这些断言的前提是「窗开着」。
                //    谁要是关着窗口来断 A31，会**恒绿**（那是本工程第 N 次「弱断言分不出两态」）。
                v5.Close();
                v4.Close();
                // 🔴 **2026-10-04（A66）补的两句**：上面两扇关了，**这两扇一直没关** —— 它们会一路开着
                //   走到收工（层 3123 的 `Warlord Image` 命中区比屏还大，**顶掉后面所有的真命中路**）。
                //   实据：`_tmp_view/collection.log:6055` 那条「A11 前置：收掉 **2** 扇还开着的
                //   `Deck info Popup`」—— 全场只有这两扇从头到尾没有 `Close()`，正好 2。
                v3.Close();
                v2.Close();

                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                //   期望矩形 = **原版 prefab** `Deck info Popup > Generic Window Red Background Big`
                //   那颗 `Image` 的 rect（`134.50, 82 → 1839.50, 1032`，2026-10-06 `rayscan` 实读）；⛔ 不写
                //   `Shell/DeckInfoPopup.cs` 里那个同名常量（那是被测实现**传进去的实参**）。
                //   ⚠️ 这一段**另开一扇**（上面那几扇的压暗层**同档** 3120 ⇒ (5,5) 上谁吃到由枚举顺序定，
                //   拿它们当现场会让「点面板外」那一步变成抛硬币）。
                {
                    var va = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.View);
                    mi0.OpenWindow(va);
                    CheckTrue(va != null && va.CurrentState == WindowState.Open, "（A94 现场）又开出一扇 `Deck info Popup`");
                    if (va != null)
                        CheckAbsorbRule("卡组信息窗", va.transform, "AbsorbHit",
                                        134.50f, 82f, 1839.50f, 1032f,
                                        DeckInfoPopup.QDI, DeckInfoPopup.QDIHit, () => va.CurrentState);
                }
            }

            // ---------------- 🆕 2026-10-03（A10 的尾巴）：`Practice Deck` ⇒ 挑对手 ⇒ 进练习赛 ----------------
            //   判据（唯一一处）= 原版 `Everguild.MatchMakerManager.StartMatch(PlayModes, PlayerBattleData,
            //   CardDeck **playerDeck**, CardDeck **enemyDeck**, …)` 的**形参名**
            //   （签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/MatchMakerManager.cs:136`）：
            //     · 点 `Practice Deck` 的那一副 = `DeckInfoPopup.context.Deck` ⇒ **playerDeck（我的）**
            //     · 之后在 `DeckSelectionPopup` 里选的那一副 = 回调形参 ⇒ **enemyDeck（对手）**
            //     · 开打前 `GameStaticData.CheckHiddenCardsInDeck(我的那副)` ⇒ 有隐藏卡就弹提示、**不开打**
            //   ⚠️ 本工程 2026-10-03 之前把这两副写反了（`项目任务.md` §三第29条 A10 的提要）—— 已就地订正。
            //   🔴 **真红法（先说清把哪一行改坏它会红）**：
            //     · `DeckInfoPopup.SelectPracticeOpponentDeck` 里 `modeFilter: info.GameMode` 改成 `null` ⇒ 「按模式筛」红
            //     · `DeckInfoPopup.StartPracticeMatch` 里把「我的」与「对手」换个来源（例如 `mine := 选中的那副`、
            //       `OpponentDeck := 被点的那副`）⇒ 下面「我 / 对手」两条同时红
            //     · `PracticeModePopup.StartBotBattle` 里 `SetPendingOpponentDeck(OpponentDeck)` 那行删掉 ⇒ 「放进通道」红
            //     · `PracticeModePopup.HasHiddenCards` 里 `ForceHiddenCardsDeck` 那一支删掉 ⇒ 隐藏卡那两条红
            //     · `PracticeModePopup.StartPracticeMatch` 里去掉 `w.StartBattle()` ⇒ 「选定即开打」那两条红
            Section("`Practice Deck`：这一副 = 我的 · 挑**对手** · 选定即开打（A10 尾巴）");
            {
                int curSaved = CollectionData.CurrentIndex();
                var mi = CollectionData.DeckAt(0);                    // 被点的那一副（自检给第 1 套塞了**合法的 30 张** —— 见 `Run()` 开头那个夹具块）
                // 🔴 让「按模式筛」这一条**真有鉴别力**：14 套测试卡组默认**全是经典(0)** ⇒
                //    不区分模式的话「筛了」与「没筛」结果一样（筛了也红不了 = 等于没查）。
                //    把**最后一副**改成遭遇(13)，验完还原（只改内存，`DeckStore` 指向临时文件）。
                int flipIdx = CollectionData.DeckCount() - 1;
                int flipSaved = flipIdx > 0 ? CollectionData.Raw(flipIdx).GameMode : 0;
                if (flipIdx > 0) CollectionData.Raw(flipIdx).GameMode = (int)GameMode.Skirmish;
                DeckSelectionPopup.LastOpened = null;
                DeckInfoPopup.LastOpponentSelection = null;
                PracticeModePopup.LastOpened = null;
                PracticeModePopup.ClearPendingOpponentDeck();

                var dp = win.OpenDeckInfo(0);
                var dpb = dp != null ? dp.Btn("Practice Deck") : null;
                var dpw = dpb != null ? dpb.GetComponent<WindowButton>() : null;
                CheckTrue(dpw != null, "`Practice Deck` 有点击区");
                if (dpw != null)
                {
                    dpw.ClickForTest();
                    CheckTrue(dp != null && dp.CurrentState == WindowState.Closed,
                              "点它 ⇒ **把自己关掉**（原版开完那扇窗就 `Close` 自己）");
                    var sel = DeckSelectionPopup.LastOpened;
                    CheckTrue(sel != null && sel == DeckInfoPopup.LastOpponentSelection,
                              "…并开**选卡组窗**（原版 `SelectPracticeOpponentDeck` 开的就是这扇，**不另建**）");
                    Check(sel != null ? sel.ModeFilter : null, (int?)mi.GameMode,
                          "选卡组窗按**这一副的模式**筛（原版 lambda：候选.GameMode == `context.Deck.GameMode`）");
                    CheckTrue(sel != null && !sel.OwnDecks, "起手落在**预组卡组**页（`DeckSelectionTabController.Start`）—— 对手多半是预组");
                    if (sel != null)
                    {
                        sel.SwitchTab(true);                          // 换到「我的卡组」页才看得见模式筛选
                        int wantOwn = 0;
                        for (int i = 0; i < CollectionData.DeckCount(); i++)
                            if (CollectionData.DeckAt(i).GameMode == mi.GameMode) wantOwn++;
                        Check(sel.ShownCount, wantOwn,
                              $"「我的卡组」页列出 **{wantOwn}** 副同模式的（筛多 = 没传 `ModeFilter`；筛少 = 筛错档）");
                        CheckTrue(wantOwn < CollectionData.DeckCount(),
                                  "…而且这一条**真有鉴别力**：库里还有 "
                                  + (CollectionData.DeckCount() - wantOwn) + " 副别的模式的（上面刚把最后一副改成遭遇）被筛掉了");

                        // 🆕 2026-10-04（A26）：**第二扇窗**（选卡组窗）的卡格也要**真鼠标**点得到 ——
                        //    它与收藏窗共用 `MenuDraw.DeckCell`（同一处裸节点）⇒ 一起修好的，这里也钉一条。
                        //    判据 = `PointerLayer.CollectHits`（`Shell/PointerLayer.cs:492`），不是 `wb.Click()`。
                        CheckTrue(sel.Cells.Count > 0, "选卡组窗画出了格子（否则下面那条等于没验）");
                        if (sel.Cells.Count > 0)
                        {
                            var sc0 = sel.Cells[0];
                            var sh0 = FindChild(sc0, "Hit");
                            var shq = sh0 != null ? sh0.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(shq != null,
                                      "★ 选卡组窗的格子：命中区节点下**真的挂着 `ImageQuad`**（A26）");
                            var pl2 = PointerLayer.Instance;
                            if (shq != null && pl2 != null)
                            {
                                var sb = pl2.ButtonAt(PxOf(sc0.position.x), PxYOf(sc0.position.y));
                                CheckTrue(sb != null && sb.transform == sh0,
                                          "★ …而**真命中路**在格中心拿到的就是这一格"
                                          + "（拿到 " + (sb == null ? "**null** = 真鼠标点不动" : "`" + sb.name + "`") + "）");
                            }

                            // 🆕 **2026-10-11（A198③）**：这一格改走 `MenuDraw.DeckCell(GameWindow, …)` 之后，
                            //   **「裁哪一块」由【本窗的 `Clip`】说了算**（= 原版模型：mask 挂在**视口节点**上），
                            //   调用点不再自己把 `SvRect` 当 `clip` 传。⇒ 钉两条：
                            //    ① **反向对照**：完全落在视口里的那一格**不许被裁**（裁多了 = 命中区比格还小 ⇒
                            //       四角点不到；也把「`Clip` 取错矩形」这一类错误抓出来）；
                            //    ② 压在视口边上的那一格：**命中区 = 格 ∩ 窗 `Clip`**（整格高 ⇒ 压根没吃到窗口那一份）。
                            //   ⚠️ 期望值用 `CellW/CellH`（**原版 prefab 的格尺寸 225×364.5**，同族已有多条断言钉着它）
                            //     —— 这里比的是**「有没有被多裁」与「交集算没算」**，不是格子摆在哪。
                            //   ⛔ 别去读 `sel.Clip` 来断这件事：`RebuildCells` 是「临时设 → 循环 → 还原」
                            //     （还原后是 null），读它恒红 —— 要断的是**裁切的结果**，不是那个字段的瞬时值。
                            //   ⚠️ 反向对照那一格是**现找**的（要求它**整格**在视口里 —— 视口 778.06 > 格高 364.5
                            //     ⇒ 起手**必然**有）：⛔ 别拿 `Cells[0]` 顶替（滚动位置一变它就可能压边 ⇒ 假红）。
                            Transform inC = null;
                            for (int i = 0; i < sel.Cells.Count; i++)
                            {
                                var c = sel.Cells[i];
                                if (c == null) continue;
                                float cy = PxYOf(c.position.y);
                                if (cy - DeckSelectionPopup.CellH * 0.5f >= DeckSelectionPopup.SvRect.y1 - 0.5f
                                    && cy + DeckSelectionPopup.CellH * 0.5f <= DeckSelectionPopup.SvRect.y2 + 0.5f)
                                { inC = c; break; }
                            }
                            if (inC == null)
                                CheckTrue(false, "（前提）选卡组窗里至少有一格**整格**落在视口里"
                                                 + "（视口 778.06 高于格高 364.5 ⇒ 必然有；一格都没有 = 前面那套建格就错了）");
                            else
                            {
                                var inH = FindChild(inC, "Hit");
                                var inQ = inH != null ? inH.GetComponentInChildren<ImageQuad>() : null;
                                if (inQ == null)
                                    CheckTrue(false, "（前提）整格那一格的 `Hit` 下面有 `ImageQuad`");
                                else
                                {
                                    CheckNear(inQ.WorldW * 108f, DeckSelectionPopup.CellW, 1.5f,
                                              "★ A198③ 反向对照：整格都在视口里的那格，命中区**宽 = 整格宽 225**"
                                              + "（裁过头 / `Clip` 取错矩形 ⇒ 这条红）");
                                    CheckNear(inQ.WorldH * 108f, DeckSelectionPopup.CellH, 1.5f,
                                              "★ …**高 = 整格高 364.5**（同上）");
                                }
                            }
                            Transform edge2 = null; float e2cy = 0f;
                            for (int i = 0; i < sel.Cells.Count; i++)
                            {
                                var c = sel.Cells[i];
                                if (c == null) continue;
                                float cy = PxYOf(c.position.y);
                                if (cy + DeckSelectionPopup.CellH * 0.5f > DeckSelectionPopup.SvRect.y2 + 0.5f)
                                { edge2 = c; e2cy = cy; break; }
                            }
                            if (edge2 == null)
                            {
                                // ⚠️ 只有「这一页的格**不超过两整行**」时才允许找不到压边的格
                                //   （两整行 = `2×Cols` 格，行高 364.5 × 2 = 729 < 视口 778.06 ⇒ 整行都进得去）。
                                //   超过两整行 ⇒ **必然**有一格压在视口下沿上 ⇒ 找不到就是真缺陷（红）。
                                //   这一页的夹具 = 13 副 ⇒ 3 行（6+6+1），第 3 行那 1 格只露 49px ⇒ 一定找得到。
                                CheckTrue(sel.Cells.Count <= DeckSelectionPopup.Cols * 2,
                                          "（前提）选卡组窗这一页的格 ≤ 两整行 ⇒ 可以没有压边的格"
                                          + $"（实得 {sel.Cells.Count} 格 / 每行 {DeckSelectionPopup.Cols}）"
                                          + " —— 超过两整行却一格都找不到压边的，说明那套裁切没生效");
                                Debug.Log(P + $"  （本页 {sel.Cells.Count} 格 ≤ 两整行 ⇒ 这次没有压边的格，"
                                            + "下面那条 ★★ 本次不计入 —— 同一条机制在收藏窗卡组页那条里验到）");
                            }
                            else
                            {
                                float e2cx = PxOf(edge2.position.x);
                                var full2 = new PxRect(e2cx - DeckSelectionPopup.CellW * 0.5f,
                                                       e2cy - DeckSelectionPopup.CellH * 0.5f,
                                                       e2cx + DeckSelectionPopup.CellW * 0.5f,
                                                       e2cy + DeckSelectionPopup.CellH * 0.5f);
                                PxRect vis2;
                                bool part2 = MenuDraw.ClipRect(full2, DeckSelectionPopup.SvRect, out vis2)
                                             && !MenuDraw.SameRect(vis2, full2);
                                CheckTrue(part2,
                                          $"★ 前提：压边那一格必须**部分**越界（实测 `vis` = ( {vis2.x1:F1},{vis2.y1:F1} )→"
                                          + $"( {vis2.x2:F1},{vis2.y2:F1} ) vs 整格 ( {full2.x1:F1},{full2.y1:F1} )→"
                                          + $"( {full2.x2:F1},{full2.y2:F1} )）—— 不成立则下一条 ★ 等于没验");
                                if (part2)
                                {
                                    var e2h = FindChild(edge2, "Hit");
                                    var e2q = e2h != null ? e2h.GetComponentInChildren<ImageQuad>() : null;
                                    CheckNear(e2q != null ? e2q.WorldH * 108f : -1f, vis2.H, 1.5f,
                                              "★★ A198③：压边那一格的命中区**高 = 格 ∩ 窗 `Clip`** 的高"
                                              + "（`clip` 没吃到窗口那一份 ⇒ 退回整格高 ⇒ 红）");
                                }
                            }
                        }
                    }
                    // 挑一副**不是我自己**的当对手（期望值由数据算，不写死名字）
                    int foeIdx = -1;
                    for (int i = 1; i < CollectionData.DeckCount(); i++)
                        if (CollectionData.DeckAt(i).GameMode == mi.GameMode) { foeIdx = i; break; }
                    CheckTrue(foeIdx > 0, "测试库里另有一副同模式的（没有它就没法验「谁是对手」）");
                    if (sel != null && foeIdx > 0)
                    {
                        var foe = CollectionData.Raw(foeIdx);
                        sel.Pick(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = false,
                            Info = CollectionData.DeckAt(foeIdx),
                            OwnIndex = foeIdx,
                            PrebuiltDeck = null,
                        });
                        Check(sel.CurrentState, WindowState.Closed, "选完 ⇒ 选卡组窗自己关（原版 `Select` 的两步：关窗 + 回调）");
                        var prac = PracticeModePopup.LastOpened;
                        CheckTrue(prac != null, "回调 ⇒ **开练习窗并立刻开打**（原版 `StartPracticeMatch` 选完就 `StartMatch`）");
                        if (prac != null)
                        {
                            // 🆕 **2026-10-09（A229）**：`Shell/PracticeModePopup.cs` 里**带 `basis`** 的那两个
                            //   helper（`Nine` 早就有守卫、`Hit`/`HitOn` 没有）—— 探针造 `basis != parent`
                            //   **真调一次 `HitOn`**，断「**会出声**」（范式同上面 `Txt` 那一段）。
                            //   ⚠️ **如实标注**：A229 原文写的是「`PracticeModePopup` 的 **`Txt`** 缺守卫」，
                            //   而**现读该文件的 `Txt`（`:1537` 那一行）根本没有 `basis` 参数**
                            //   （签名 = `(Transform parent, string text, …)`、落位用 `Local3(parent, …)`
                            //   ⇒ 结构上不可能不等）⇒ 那句判据在本文件**指向不存在的东西**；
                            //   同一形状的真身 = `HitOn`（位置 `Local3(basis, …)`、树父 `parent`）。
                            //   🔴 **改坏法**：删掉 `HitOn` 开头那句守卫 ⇒ 反例读出 0 条警告 ⇒ 红。
                            {
                                var probeH = new GameObject("A229_HitProbe", typeof(RectTransform)).transform;
                                probeH.SetParent(prac.transform, false);
                                probeH.localPosition = new Vector3(0.5f, 0.5f, 0f);
                                int hSame = 0, hDiff = 0; string lastH = null;
                                Application.LogCallback gSame = (c, s, t) =>
                                { if (t == LogType.Warning && c != null && c.Contains("`HitOn` 的 `basis`")) hSame++; };
                                Application.logMessageReceived += gSame;
                                var nSame = prac.HitBasisProbeForTest(probeH, probeH);          // 正例
                                Application.logMessageReceived -= gSame;
                                Application.LogCallback gDiff = (c, s, t) =>
                                { if (t == LogType.Warning && c != null && c.Contains("`HitOn` 的 `basis`"))
                                  { hDiff++; lastH = c; } };
                                Application.logMessageReceived += gDiff;
                                var nDiff = prac.HitBasisProbeForTest(probeH, prac.transform);  // 反例
                                Application.logMessageReceived -= gDiff;
                                CheckTrue(nSame != null && nDiff != null,
                                          "A229：两次探针都**真调到了** `HitOn`（都建出了命中区）");
                                Check(hSame, 0, "A229 正例：`basis == parent` ⇒ **不出声**");
                                Check(hDiff, 1, "★ A229 反例：`basis != parent` ⇒ **出一条警告**"
                                                + "（读 0 = 守卫被删掉了）" + (lastH != null ? "；实得：" + lastH : ""));
                                Object.DestroyImmediate(probeH.gameObject);
                            }
                            Check(prac.DeckIndex, 0,
                                  "**我** = 被点 `Practice Deck` 的那一副（原版 `playerDeck` = `DeckInfoPopup.context.Deck`）");
                            CheckTrue(prac.OpponentDeck != null && prac.OpponentDeck.Name == foe.Name,
                                      "**对手** = 刚在窗里选中的那一副（原版 `enemyDeck` = 回调回来那副）");
                            CheckTrue(prac.OpponentDeck != null && prac.OpponentDeck.Name != mi.Name,
                                      "两副**不是同一副** —— 放反了这条就红（这两副名字本来就不同）");
                            CheckTrue(prac.SearchingMatch, "选定 ⇒ 立刻进「等对手」那 12 秒（原版 `ShowPopUp(等待窗)` → `StartMatch`）");
                            CheckTrue(!prac.StartedBattle, "12 秒还没到 ⇒ 不抢跑");
                            prac.TickSearch(12f);
                            CheckTrue(prac.StartedBattle, "等满 12 秒 ⇒ **真开打**（批处理只记账；真机上这一步 `LoadScene(\"Battle\")`）");
                            Check(CollectionData.CurrentIndex(), prac.DeckIndex,
                                  "开战前把**我那一副**交给 `DeckLibrary`（`BattleDriver.PickSavedDeck` 读的就是它）");
                            var pend = PracticeModePopup.PendingOpponent;
                            CheckTrue(pend != null && pend.Name == foe.Name,
                                      "开战时把**对手那副**放进「本局对手」通道 —— `BattleDriver.BeginFromDeckLibrary` 该读的就是它");
                            CheckTrue(pend != null && pend.Name != mi.Name, "…通道里**不是**我自己那副");
                            var took = PracticeModePopup.TakePendingOpponentDeck();
                            CheckTrue(took != null && took.Name == foe.Name,
                                      "`TakePendingOpponentDeck()` 拿得到 —— 开局那条路读的就是它");
                            CheckTrue(PracticeModePopup.TakePendingOpponentDeck() == null,
                                      "**读一次就清**（下一局不会带着上一局的对手）");
                            prac.Close();       // 清理：别盖住后面的截图
                        }
                    }
                    // ② **隐藏卡**前置检查（原版 `GameStaticData.CheckHiddenCardsInDeck`）：注入 ⇒ 弹提示、**不开打**。
                    //   ⚠️ 为什么用注入：原版判据是 `PlayerItem.IsHidden()`，而这个 build 里 `RawCardScript` 没覆写它、
                    //      我们的卡数据也没有「隐藏」字段 ⇒ 不注入的话这条分支**永远走不到**（= 等于没查）。
                    // 🔴 **2026-10-12（A416）改清场法**：`Manager.ShowPopUp` 的宿主已收编成 `PopUpGameWindow`
                    //    （原版 `MessagePopupWindow`）⇒ 按 `PromptPopup` 这个【类型】清场**再也清不到它**
                    //    （会静默变成空做，模态窗留着盖住后面的截图与断言）。改成**类型无关**那一句
                    //    （本仓现成先例 = `Editor/ShopScene.cs:1512-1513` —— 它本来就是类型无关的，
                    //      收编前后都绿；⛔ 别按 `H5` 报告里那个 `:1454-1455` 去找，那是漂掉的行号）。
                    var wmFixC = win.Manager != null ? win.Manager : WindowsManager.Instance;
                    if (wmFixC != null && wmFixC.popUpWindow != null) wmFixC.popUpWindow.Close();
                    var dp3 = win.OpenDeckInfo(0);
                    PracticeModePopup.LastOpened = null;
                    PracticeModePopup.ClearPendingOpponentDeck();
                    PracticeModePopup.ForceHiddenCardsDeck = CollectionData.Raw(0);   // = 「我这一副」那个对象
                    var d3 = dp3 != null ? dp3.Btn("Practice Deck") : null;
                    var d3w = d3 != null ? d3.GetComponent<WindowButton>() : null;
                    if (d3w != null) d3w.ClickForTest();
                    var sel3 = DeckSelectionPopup.LastOpened;
                    if (sel3 != null && foeIdx > 0)
                        sel3.Pick(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = false,
                            Info = CollectionData.DeckAt(foeIdx),
                            OwnIndex = foeIdx,
                            PrebuiltDeck = null,
                        });
                    CheckTrue(PracticeModePopup.LastOpened == null,
                              "我自己那副带**隐藏卡** ⇒ **不开练习赛**（原版 `CheckHiddenCardsInDeck` 那一支：弹提示、不 `StartMatch`）");
                    CheckTrue(PracticeModePopup.PendingOpponent == null, "…连「本局对手」通道都不该被写上");
                    // 🔴 **2026-10-12（A416）改取窗法**：宿主已收编成 `PopUpGameWindow`，按 `PromptPopup`
                    //    这个【类型】找窗会**一路找不到**（`hp` 恒 null ⇒ 下一条断言退化成「一个提示窗都没有」
                    //    的空断）。改成读 `WindowsManager` 那个**字段**（`popUpWindow`，类型无关 ——
                    //    `ShowPopUp` 两条实现都写它）。正文节点名两边同名（`MessageText`，见
                    //    `PopUpGameWindow.Build()`）⇒ 读法一个字都不用改。
                    var wmFixD = win.Manager != null ? win.Manager : WindowsManager.Instance;
                    GameWindow hp = wmFixD != null ? wmFixD.popUpWindow : null;
                    var hpTxt = hp != null ? TextOf(FindChild(hp.transform, "MessageText")) : null;
                    CheckTrue(hpTxt != null && hpTxt.Contains("隐藏卡"),
                              "…并且**弹出提示说清原因**（不许静默）—— 实测文案「" + (hpTxt ?? "<没有提示窗>") + "」");
                    PracticeModePopup.ForceHiddenCardsDeck = null;
                    if (hp != null) hp.Close();
                    // 🔴 **2026-10-04（A66）**：这一扇（`dp3`）走到这里必须**显式收掉**。
                    //   它的「开完自己关」那条路在 `DeckInfoPopup.SelectPracticeOpponentDeck():667` 里，
                    //   但**只在点击真的发生了**（`d3w != null`）时才走得到 ⇒ 补一句兜底，
                    //   别让一扇层 3123 的模态一路盖到收工（那会顶掉后面所有人的真命中路）。
                    if (dp3 != null) dp3.Close();
                    // ③ 预组也能当对手（原版那条链默认就落在预组页）—— 判据只一份：`PracticeModePopup.PlayerDeckOf`
                    if (PrebuiltDecks.Available && PrebuiltDecks.Tab.Count > 0)
                    {
                        var pk = PrebuiltDecks.Tab[0];
                        var pd = PracticeModePopup.PlayerDeckOf(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = true,
                            Info = DeckSelectionPopup.InfoOf(pk),
                            OwnIndex = -1,
                            PrebuiltDeck = pk,
                        });
                        CheckTrue(pd != null && pd.WarlordId == pk.heroId,
                                  "选预组当对手 ⇒ 搓出来的 `PlayerDeck` 督军 = 那一副的督军（" + pk.deckId + " → " + pk.heroId + "）");
                    }
                    else Debug.LogWarning(P + "   预组数据读不到 ⇒ ③ 那一条跳过了（**不是通过**）");
                }
                if (flipIdx > 0) CollectionData.Raw(flipIdx).GameMode = flipSaved;   // 还原上面动过的那一副
                CollectionData.Select(curSaved);      // 上面开战那一步会改「当前卡组」⇒ 还原
            }

            // ---------------- 🆕 2026-10-06（A132）：练习窗 `Army Selector` 的底图 + 它的吸收层 ----------------
            //   原版 `Practice Mode Menu > Deck Selector > Army Selector > Background`
            //   （`Image` · **`UI_Background faction buttons`** 54×420 · **Sliced** border (2,201,2,202) ·
            //    **`m_RaycastTarget = 1`**；矩形 = **69.42,182.18→246.54,880.17** = `Army Selector` 自己那一格）。
            //   🔴 **A94 相 1 §四·1 当时判「这一块不接」**（理由：我们那一列**没画**那颗底图）——
            //     「没画」是**真话**，但本轮把**两件一起补了**：底图 + 吸收层。
            //     只补底图会多出一块「看着是块底、点了却把窗关掉」的**死区**（图不吃射线 ⇒ 穿到压暗层）。
            //   ⛔ 期望值一律写 **prefab 字面量**（⛔ 不写 `PracticeModePopup.ArmL…` —— 那是被测实现**传进去的实参**，
            //     同式自证）；只有「档」那两个常量照旧取本窗的原版档常量（⚠️ 那是 `CheckAbsorbRule` 的入参来源，
            //     与压暗层那条 `MenuDraw.CheckShadeRule` **不是同一套判据** —— 后者 2026-10-07 起改成量场景真值）。
            //   🔴 **真红法**：把 `BuildArmySelector` 里那两行删掉 ⇒ ①（底图节点不在）红；把 `Nine(...)` 那一行
            //     留着、只删 `Absorb` 那一行 ⇒ `CheckAbsorbRule` 的①②③④⑤ 一起红。
            Section("练习窗：`Army Selector` 的底图 + 它的吸收层（A132）");
            {
                var mgrA = win.Manager;
                PracticeModePopup.LastOpened = null;
                var pw = PracticeModePopup.Create(mgrA);          // 本段专用的一扇（不借上面那扇 ——
                mgrA.OpenWindow(pw);                              //   那时它正 `SearchingMatch`，搜索窗盖着它）
                CheckTrue(pw != null && pw.CurrentState == WindowState.Open, "开出一扇练习窗（本段末尾关掉）");
                if (pw != null)
                {
                    // ① 底图**建出来了**（A132 的本体就是「视觉缺漏」⇒ 先断它，别只断吸收层）
                    var bg = FindChild(FindChild(pw.transform, "Army Selector"), "Background");
                    CheckTrue(bg != null,
                              "`Army Selector/Background` **建出来了**（原版那颗底图，A132 之前我们一颗都没有）");
                    var bgq = bg != null ? bg.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(bgq != null && bgq.Texture != null
                              && bgq.Texture.name == "UI_Background_faction_buttons",
                              "★ 画的是**原版那张** `UI_Background faction buttons`（实得 `"
                              + (bgq != null && bgq.Texture != null ? bgq.Texture.name : "<没取到>") + "`）");
                    if (bg != null)
                    {
                        var pc = LayoutSpace.ToPixel(bg.position);
                        CheckNear(pc.x, 157.98f, 1.5f,
                                  "★ 底图中心 x = **69.42..246.54 的中点**（= `Army Selector` 自己那一格）");
                        CheckNear(pc.y, 531.175f, 1.5f, "…中心 y = **182.18..880.17 的中点**");
                        // 🔴 **四沿也要断**：⚠️ 上面那条「中心 y」**分不出**「照父件画」和「照 `Viewport` 画」——
                        //    两格**中点恰好同号**（`Viewport` 上下各探出 33.11 ⇒ (149.07+913.28)/2 = 531.175
                        //    = (182.18+880.17)/2）⇒ 真判据在**上下沿**（各差 33.11）。
                        //    九宫格是 9 颗子块**正好铺满**目标矩形 ⇒ 量子块的**并集**就是底图那四沿。
                        float ux1 = float.MaxValue, uy1 = float.MaxValue;
                        float ux2 = float.MinValue, uy2 = float.MinValue;
                        foreach (var q2 in bg.GetComponentsInChildren<ImageQuad>())
                        {
                            if (q2 == null) continue;
                            var p2 = LayoutSpace.ToPixel(q2.transform.position);
                            float hw = q2.WorldW * 108f * 0.5f, hh = q2.WorldH * 108f * 0.5f;
                            ux1 = Mathf.Min(ux1, p2.x - hw); uy1 = Mathf.Min(uy1, p2.y - hh);
                            ux2 = Mathf.Max(ux2, p2.x + hw); uy2 = Mathf.Max(uy2, p2.y + hh);
                        }
                        CheckNear(ux1, 69.42f, 1.5f, "★ 底图**左沿** = 原版那一格");
                        CheckNear(uy1, 182.18f, 1.5f,
                                  "★ 底图**上沿** = **182.18**（⚠️ **不是** `Viewport` 的 149.07 —— 差 33.11，"
                                  + "这一条才分得出「照父件画」和「照视口画」）");
                        CheckNear(ux2, 246.54f, 1.5f, "★ 底图**右沿** = 原版那一格");
                        CheckNear(uy2, 880.17f, 1.5f, "★ 底图**下沿** = **880.17**（同上，不是 913.28）");
                    }
                    CheckAbsorbRule("练习窗（阵营纵列底图）", pw.transform, "Army Selector/AbsorbHitArmy",
                                    69.42f, 182.18f, 246.54f, 880.17f,
                                    PracticeModePopup.QPr, PracticeModePopup.QPrHit, () => pw.CurrentState);
                }
                if (pw != null) pw.Close();                       // 别让它盖住后面那些真命中路
            }

            // ============================================================ §A92 节点类型（2026-10-07 新增）
            //
            // 🔴 **判据 = 原版自己的节点类型**（不是我们的常量）：下面这 5 个名字在
            //    `bundle_menus_assets_all` 里**逐个实读过组件** —— `Deck info` · `Background Info` ·
            //    `General container` · `Army Selector` · `Viewport` · `Filters` · `Decks Scroll view` · `Content`
            //    **全是 `RectTransform`**（该包 16768 个 `GameObject` 里 **16510 是 `RectTransform`**，
            //    剩下 258 个裸 `Transform` 全是卡框 3D 子锚与粒子件 —— **一个菜单容器都没有**）。
            //    ⇒ `PracticeModePopup.New` 原来建的是**裸 `Transform`**（连 `rect` 都没有，宽高无从验收）——
            //    现在必须是 `RectTransform`。
            // ⚠️ **反面那一半**（原版**就是**裸 `Transform` 的件不许被顺手改齐）在 `RewardsScene` 的 §A92 那节，
            //    那边是**成对**断的（`Particle System nebula` **没有** / 上面两级宿主**有**）。
            // ⚠️ 本段自开一扇窗（照上面 A132 那段的做法），**末尾关掉并清 `LastOpened`** —— 不留状态给后段。
            Section("§A92 节点类型：`PracticeModePopup.New` 建的空节点都是 `RectTransform`");
            {
                var mgrB = win.Manager;
                PracticeModePopup.LastOpened = null;
                var pwB = PracticeModePopup.Create(mgrB);
                mgrB.OpenWindow(pwB);
                CheckTrue(pwB != null, "开出一扇练习窗（本段末尾关掉）");
                if (pwB != null)
                {
                    var a92sel = FindChild(pwB.transform, "Army Selector");
                    CheckTrue(a92sel != null && a92sel.GetComponent<RectTransform>() != null,
                              "`Army Selector` 是 **`RectTransform`**（原版那一件是 `ScrollRect` 的宿主）");
                    var a92vp = a92sel != null ? FindChild(a92sel, "Viewport") : null;
                    CheckTrue(a92vp != null && a92vp.GetComponent<RectTransform>() != null,
                              "`Army Selector/Viewport` 是 **`RectTransform`**（原版挂 `RectMask2D` 的那一件）");
                    var a92fil = a92vp != null ? FindChild(a92vp, "Filters") : null;
                    CheckTrue(a92fil != null && a92fil.GetComponent<RectTransform>() != null,
                              "`…/Viewport/Filters` 是 **`RectTransform`**（原版 `GridLayoutGroup` 的格容器）");
                    var a92di = FindChild(pwB.transform, "Deck info");
                    CheckTrue(a92di != null && a92di.GetComponent<RectTransform>() != null,
                              "`Deck info` 是 **`RectTransform`**（右半那块面板的根）");
                    var a92gc = a92di != null ? FindChild(a92di, "General container") : null;
                    CheckTrue(a92gc != null && a92gc.GetComponent<RectTransform>() != null,
                              "`Deck info/General container` 是 **`RectTransform`**（原版出厂可见那个抽屉）");
                    // 命中区那一族（`PracticeModePopup.HitOn` 是本窗自己的工厂）—— 原版这一层 = 按钮自己的 `RectTransform`
                    var a92th = FindChild(pwB.transform, "ToggleHit");
                    CheckTrue(a92th != null && a92th.GetComponent<RectTransform>() != null,
                              "`ToggleHit`（`HitOn` 那条路建的透明命中区）是 **`RectTransform`**"
                              + "（原来建的是裸 `Transform`；判据同 `MenuDraw.Hit`）");
                }
                if (pwB != null) pwB.Close();
                PracticeModePopup.LastOpened = null;
            }

            // ---------------- `Import Deck Popup`（A1 §4）----------------
            Section("`Import Deck Popup`：版面 + **导入闭环**（A1 §4）");
            {
                var imp = win.OpenImportPopup();
                CheckTrue(imp != null, "开得出来");
                if (imp != null)
                {
                    var iroot = imp.transform;
                    // 🆕 A17：`Confirm`（九宫底 `40K_button`）与绿色关闭钮逐个悬停验一遍
                    CheckHoverSwap(imp.transform, "Import Deck Popup");
                    Check(imp.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(imp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                    CheckAt(FindChild(iroot, "Generic Popup Background"), 560f, 1360f, 234.07f, 685.93f,
                            "`Window`（560,234.07 → 1360,685.93）");
                    CheckText(TextOf(FindChild(iroot, "Main Search message")), "Paste your deck", "提示行文案");
                    var ph = FindChild(iroot, "Input Text");
                    CheckText(TextOf(ph), "Enter text...", "空输入时显示**占位符**");
                    // 🔴 实拍抓的：占位符第一版**跑到输入框上面去了**（`basis` 给了 root 而 parent 是 `Window`）
                    var phLb = ph != null ? ph.GetComponent<Label>() : null;
                    CheckTrue(phLb != null && Mathf.Abs(PxYOf(ph.transform.position.y) - 441.03f) <= 1.5f,
                              $"占位符**落在输入框里**（中心 y 实测 {(ph != null ? PxYOf(ph.transform.position.y) : -1f):F1}"
                              + "，原版 `Text Area` 377→505.06 ⇒ 中心 **441.03**）");
                    CheckText(TextOf(FindChild(iroot, "Confirm Text")), "Confirm", "确认钮文案");
                    Shoot("06_收藏_ImportDeck弹窗.png");   // ⚠️ **趁窗开着拍**（第一版拍在导入成功之后 ⇒ 窗已经关了）
                    // ⚠️ 别拿九宫格的**根**量宽 —— `GetComponentInChildren<ImageQuad>()` 取到的是**角块**
                    //    （第一版量出 190.76 = 一个角）。量**点击区那个单 quad**（= 整个按钮矩形）。
                    CheckNear(imp.OkHit != null ? Wpx(imp.OkHit) : -1f, 478.343f, 2f,
                              "确认钮宽 = **478.343**（原版；VLG 只有一个钮 ⇒ 在容器里居中）");
                    var okBtn = imp.OkHit != null ? imp.OkHit.GetComponent<WindowButton>() : null;
                    CheckTrue(okBtn != null, "`Confirm` 有点击区");
                    // ① 空串
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, "先粘贴卡组串",
                              "空串 ⇒ 给**人话**（**与卡组编辑那边逐字一致** —— 判据只有 `CollectionData.ImportDeck` 一份）");
                    Check(imp.CurrentState, WindowState.Open, "失败**不关窗**");
                    // ② 乱串
                    imp.SetTextForTest("这不是一条卡组串");
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, "这不是一条合法的卡组串", "乱串 ⇒ 另一句人话");
                    // ③ 真串（拿卡组 1 导出的串再导回来）
                    int n0 = CollectionData.DeckCount();
                    var src = DeckLibrary.ExportString(DeckLibrary.Load().Decks[0]);
                    imp.SetTextForTest(src);
                    if (okBtn != null) okBtn.Click();
                    Check(CollectionData.DeckCount(), n0 + 1, "合法串 ⇒ 卡组数 +1（**导入真的接上了**）");
                    Check(imp.CurrentState, WindowState.Closed, "成功 ⇒ 窗关上");
                    // ④ 点背景也关（原版 `backgroundCloseButton`）
                    var imp2 = win.OpenImportPopup();
                    var shade = imp2 != null && imp2.ShadeHit != null ? imp2.ShadeHit.GetComponent<WindowButton>() : null;
                    CheckTrue(shade != null, "背景有点击区（原版 `backgroundCloseButton`）");
                    // 🆕 2026-10-04（**A25⑥ 的断言模板**）：压暗层的命中区**档**必须落在压暗层自己那一档、
                    //   且**严格低于**本窗内容命中区档 —— 同档时 `ImageQuad` 的世界 z 恒 0，谁吃到退化成
                    //   「枚举顺序」，症状是**点不动的钮看着像正常工作**（A27 那批用真缺陷买来的）。
                    //   期望值 = 本窗自己的两个**原版档常量**（`QImp` / `QImpHit`），⛔ 不从被测实现里读。
                    if (imp2 != null)
                    {
                        // 🔴 A77⑬③⑥：判据收口到唯一那份 `MenuDraw.CheckShadeRule`，且期望值改成
                        //   **量**本窗那块视觉压暗层（`ImportDeckPopup` 里它叫 `Background`，
                        //   见 `Solid(root, 960f, 540f, …, "Background")`）的 quad 档。
                        MenuDraw.CheckShadeRule(CheckTrue, "导入卡组窗", imp2.ShadeHit,
                                                imp2.transform.Find("Background"), ImportDeckPopup.QImpHit);
                    }
                    if (shade != null) shade.Click();
                    Check(imp2 != null ? imp2.CurrentState : WindowState.Open, WindowState.Closed, "点背景 ⇒ 关窗");

                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `Import Deck Popup > Window > Generic Popup Background`
                    //   那颗 `Image` 的 rect（`560, 234.07 → 1360, 685.93`）；⛔ 不写 `ImportDeckPopup.WinL…`
                    //   —— 那是被测实现**传进去的实参**（同式自证）。⚠️ 上面那扇 `imp2` 已经关了 ⇒ **另开一扇**。
                    var imp3 = win.OpenImportPopup();
                    CheckTrue(imp3 != null && imp3.CurrentState == WindowState.Open,
                              "（A94 现场）又开出一扇 `Import Deck Popup`");
                    if (imp3 != null)
                        CheckAbsorbRule("导入卡组窗", imp3.transform, "AbsorbHit",
                                        560f, 234.07f, 1360f, 685.93f,
                                        ImportDeckPopup.QImp, ImportDeckPopup.QImpHit, () => imp3.CurrentState);
                }
            }

            // ---------------- 切页（`visualTypes` 必须整表替换） ----------------
            Section("切页（正本 §二；`visualTypes` 被本窗整表替换）");
            if (win.tabButtons != null)
            {
                win.tabButtons.Click(1);
                Check(win.CurrentTab, WindowTabType.CollectionCards, "点第 2 键 ⇒ 切到 **Cards** 页");
                var p2 = FindChild(tabsRoot, "CardsTab");
                CheckTrue(p2 != null && p2.gameObject.activeSelf,
                          "**`CardsTab`** 开着（A288：页节点名 = `CardsTab`，⛔ 不是 `Card Collection Tab`）");
                CheckTrue(!FindChild(tabsRoot, "Select Deck Tab").gameObject.activeSelf, "`Select Deck Tab` 关着");
                win.tabButtons.Click(0);
                Check(win.CurrentTab, WindowTabType.CollectionDecks, "点回第 1 键 ⇒ 回 Decks 页");
                // 🔴 选中态：**只亮当前那个键**（第一版漏了刷高亮 —— 截图里 DECKS 页却亮着 CARDS）
                var hi = new List<Transform>();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Highlight" && t.parent != null && t.parent.name.StartsWith("CollectionTabButton_"))
                        hi.Add(t);
                Check(hi.Count, 4, $"左栏四个键的选中底图都建了（实测 {hi.Count}）");
                int on = 0;
                foreach (var h in hi) if (h.gameObject.activeSelf) on++;
                Check(on, 1, $"**恰好一个键亮着**（实测 {on} 个）—— 「切页要跟着刷高亮」的判据");
                if (hi.Count == 4)
                    CheckTrue(hi[0].gameObject.activeSelf && !hi[1].gameObject.activeSelf,
                              "亮的是**第 1 键**（当前页 = Decks）");
            }

            // ---------------- Cards 页：卡池网格（正本 §四；A3 实测）----------------
            Section("Cards 页：卡池网格（6 列 · 262.5×384 · **贴左但整体居中**）");
            win.tabButtons.Click(1);                       // 切到 Cards
            var cardsHolder = FindChild(tabsRoot, "Scroll View");
            CheckAt(cardsHolder, 330.2f, 1919.9f, 155.9f, 1079.9f,
                    "`Scroll View`（卡池视口；⚠️ 与 Deck 页的视口**差 0.7/0.1**，两份实测都记着）");
            CheckTrue(win.CardsVisibleCount > 1000, $"卡池读到 {win.CardsVisibleCount} 张（`CardDatabase.Load()`）");
            CheckTrue(win.CardsCells.Count > 0, $"画出了 {win.CardsCells.Count} 格（视口外的不建 = `RectMask2D` 那套）");
            if (win.CardsCells.Count > 0)
            {
                var cc0 = win.CardsCells[0];
                CheckNear(PxOf(cc0.position.x), 468.8f, 1.5f,
                          "第 1 格中心 x = **468.8**（贴左但整体居中：330.2 + 7.35 + 262.5/2）");
                CheckNear(PxYOf(cc0.position.y), 347.9f, 1.5f, "第 1 格中心 y = **347.9**（155.9 + 384/2）");
            }
            if (win.CardsScroll != null && win.CardsScroll.MaxOffset > 0f)
            {
                win.CardsScroll.ScrollBy(win.CardsScroll.MaxOffset);
                int lastIdx = win.CardsVisibleCount - 1;
                var lc = FindChild(tabsRoot, "CollectionCard_" + lastIdx);
                CheckTrue(lc != null && PxYOf(lc.position.y) + 192f <= 1080.5f,
                          $"**滚到底 ⇒ 第 {lastIdx + 1} 张（最后一张）完整落进视口**");
                win.CardsScroll.ScrollBy(-win.CardsScroll.MaxOffset);
            }

            // 🆕 2026-10-03（A12 收口）：Cards 页 —— 压在视口下边上的那一格，**命中区 == 露出来的那部分**
            //   （这一页的 `Hit` 走 `AddHit` ⇒ **有 quad** ⇒ 能直接量渲染矩形；判据同 `MenuDraw.ClipRect`）
            if (win.CardsScroll != null)
            {
                var cs = win.CardsScroll;
                float savedC = cs.Offset;
                cs.SetOffset(0f);
                int edgeIdx = -1;
                for (int i = 0; i < win.CardsVisibleCount; i++)
                {
                    var rr = CollectionWindow.CardsCellRect(i);
                    if (rr.y1 < CollectionWindow.CardsViewport.y2 - 0.5f
                        && rr.y2 > CollectionWindow.CardsViewport.y2 + 0.5f) { edgeIdx = i; break; }
                }
                CheckTrue(edgeIdx >= 0, "起手有一格**压在卡池视口下边上**（否则这一条等于没验）");
                if (edgeIdx >= 0)
                {
                    // 视口 330.2,155.9 → 1919.9,1079.9（本文件上面 `CheckAt(cardsHolder, …)` 已钉住）
                    var vp = CollectionWindow.CardsViewport;
                    var onScreen = cs.Shift(CollectionWindow.CardsCellRect(edgeIdx));
                    PxRect vis;
                    bool inView = MenuDraw.ClipRect(onScreen, vp, out vis);
                    var h = FindChild(tabsRoot, "CardHit_" + edgeIdx);
                    float x1, y1, x2, y2;
                    CheckTrue(h != null && RectOf(h, out x1, out y1, out x2, out y2),
                              $"第 {edgeIdx + 1} 格（压边那一格）的 `Hit` 建了、渲染矩形量得到");
                    if (h != null && RectOf(h, out x1, out y1, out x2, out y2))
                    {
                        CheckTrue(inView, "现算：这一格与视口**有交集**");
                        CheckNear(y2, vp.y2, 0.5f, "命中区的**下边缘 = 视口下边 1079.9**（被裁在那儿）");
                        CheckNear(y2 - y1, vis.H, 1.0f, $"命中区高 = **露出来的那部分**（{vis.H:F1}px）");
                        CheckTrue(y2 - y1 < CollectionWindow.CardsCellH - 1f,
                                  $"…而且确实**比整格矮**（{y2 - y1:F1} < {CollectionWindow.CardsCellH}）");
                        CheckNear(x2 - x1, CollectionWindow.CardsCellW, 1f, "…横向没裁 ⇒ 宽仍是格的宽 262.5");
                    }
                    // 对照：完全落在视口里的那一格 ⇒ 命中区是**整格**
                    var h0 = FindChild(tabsRoot, "CardHit_0");
                    float ax1, ay1, ax2, ay2;
                    if (h0 != null && RectOf(h0, out ax1, out ay1, out ax2, out ay2))
                    {
                        CheckNear(ax2 - ax1, CollectionWindow.CardsCellW, 1f, "对照：视口里的那一格 ⇒ 命中区**整格宽**");
                        CheckNear(ay2 - ay1, CollectionWindow.CardsCellH, 1f, "对照：…**整格高**（没被裁）");
                    }
                }
                cs.SetOffset(savedC);
            }

            // ═══════════ 🆕 2026-10-08（A181）：卡池的卡**真的被视口裁住了** ═══════════
            //  判据 = 原版 `CardsTab/Collection Display/Scroll View/Viewport` 上那颗 `RectMask2D`
            //  （`m_Softness = (0,0)` · `m_Padding = (0,0,0,0)` ⇒ **硬边、不内缩**；实读命令与四条 Viewport 的值
            //   见 `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）。
            //  🔴 **为什么必须量渲染几何**：原来漏掉正是因为断言只量「节点在不在 / 矩形对不对」——
            //   卡是 `CardView` 的自建网格，节点位置全对而**画出来的层越界**（原来滚 192px 时第一排卡
            //   一路顶到 155.9−192 = −36.1，压在页头那条空带上）。这里逐顶点把网格换算成画布像素来量。
            //  **改坏法**：抽掉 `CollectionWindow.RebuildCardsCells` 里 `v.SetPose(…, CardsViewport)` 的第 4 个实参
            //  （或 `CardView.CropLayer`）⇒ 被切那一排的层会顶到视口上沿之上 ⇒ 这一组**立刻红**。
            if (win.CardsScroll != null)
            {
                var cs = win.CardsScroll;
                float savedC = cs.Offset;
                cs.SetOffset(192f);              // 上移 192 ⇒ 起手第一排（155.9..539.9）被视口上沿切掉 192px
                var vp = CollectionWindow.CardsViewport;
                int cutIdx = -1;
                for (int i = 0; i < CollectionWindow.CardsCols; i++)
                {
                    var rr = cs.Shift(CollectionWindow.CardsCellRect(i));
                    if (rr.y1 < vp.y1 - 0.5f && rr.y2 > vp.y1 + 8f) { cutIdx = i; break; }
                }
                CheckTrue(cutIdx >= 0, "（前提）滚 192px 后第一排有一张卡**压在视口上沿上**");
                var cutCard = cutIdx >= 0 ? FindChild(tabsRoot, "CollectionCard_" + cutIdx) : null;
                CheckTrue(cutCard != null, $"被上沿切到的那张卡（`CollectionCard_{cutIdx}`）建出来了");
                float cx1, cy1, cx2, cy2;
                int cverts;
                CheckTrue(RenderExtentPx(cutCard, out cx1, out cy1, out cx2, out cy2, out cverts) && cverts >= 20,
                          $"被切的那张卡**量得到渲染几何**（实测 {cverts} 个顶点；量到 0 就是这一条没验）");
                CheckTrue(cy1 >= vp.y1 - 0.5f && cy2 <= vp.y2 + 0.5f && cx1 >= vp.x1 - 0.5f && cx2 <= vp.x2 + 0.5f,
                          $"★ 卡上**没有一层画到视口外**（渲染范围 {cx1:F1},{cy1:F1} → {cx2:F1},{cy2:F1}；"
                          + $"视口 {vp.x1:F1},{vp.y1:F1} → {vp.x2:F1},{vp.y2:F1}）");
                CheckTrue(cy1 <= vp.y1 + 0.5f,
                          $"★ 而且它**真的被裁在视口上沿**（最高顶点 {cy1:F1} ≈ {vp.y1:F1}）"
                          + " —— 没有被裁的话它会一直顶到 " + $"{vp.y1 - 192f:F0}（= 页头那条空带上）");
                float tx1, ty1, tx2, ty2;
                int tverts;
                if (TextExtentPx(cutCard, out tx1, out ty1, out tx2, out ty2, out tverts) && tverts > 0)
                    CheckTrue(ty1 >= vp.y1 - 0.5f && ty2 <= vp.y2 + 0.5f && tx1 >= vp.x1 - 0.5f && tx2 <= vp.x2 + 0.5f,
                              $"★ 卡上的**文字也没有一个字画到视口外**（字的范围 {tx1:F1},{ty1:F1} → {tx2:F1},{ty2:F1}；"
                              + $"{tverts} 个顶点）");
                else
                    CheckTrue(false, "★ 卡上的文字**量不到网格** ⇒ 这一条没验（⛔ 别当通过；TMP 网格没建起来要先修那件事）");

                // ═══════ 🆕 2026-10-11（A250）：卡上那段字**真的走了「夹顶点」那条路** ═══════
                //  为什么单开这一条：上面那两条读的是 `textInfo`（TMP 的**模型**）与「启用的 `MeshRenderer` 网格」，
                //  它们都**分辨不出**「那一刀只改了模型、没上传到会被画出来的网格」—— 而 A250 把
                //  `CardView.ClipTextMesh` 收口成**转调** `MenuDraw.ClipTmpMesh`，那份公共件里**正好有**
                //  `MeshFilter.sharedMesh` 守卫（拿不到就不上传、数进 `TextClipUploadSkipped`）⇒ 这条契约要钉住。
                //  🔴 **用哪张卡**：卡面上那几段字（卡名 / 效果 / 兵种 / 数值）**全在卡高 58% 以下**
                //  （`UnitNameAt.y = 0.5821`、`RaceText` 0.8682 —— 单位「从卡顶算」，见 `CardView.PlaceAt`）
                //  ⇒ 滚 192px 那种「切上沿」（切掉上半张）**一段字都碰不到**。要验字就得用**压在下沿上**的那张
                //  （卡片下半截在视口外 ⇒ 卡上每一段字都整块越界）。期望值（**两重独立来源**）：
                //  ① `vp` = 原版 `Viewport` 的字面量；② 「整块越界 ⇒ 四角被夹到同一条边」是**几何事实**
                //  ⇒ 夹完应当有顶点**恰好贴在 `vp.y2`**（不夹的话它们停在卡自己那一带、整段在视口外）。
                //  **改坏法**：① 删掉 `CardView.ClipTextMesh` 里那句转调（文字不裁）⇒ 第 3 条红；
                //    ② 把公共件改成「只写 `textInfo`、不上传」（删 `UpdateVertexData`）⇒ 读上传网格时还是原始
                //      位置 ⇒ 第 3 条红（第 1、2 条读模型，看不出来 —— 这正是这条断言值钱的地方）；
                //    ③ 抽掉 `RebuildCardsCells` 里 `v.SetPose(…, CardsViewport)` 第 4 个实参 ⇒ 整卡不裁 ⇒ 红。
                //  🆕 **2026-10-11（F3）：本条原来是把 `tm.vertices`【整条数组】扫一遍 ⇒ 假红**
                //    —— 那份数组里夹着零面积的**非字形占位槽**（空格那种不可见的字占的 4 个零槽；
                //    见 `GlyphVertsOf` 里那两条判据）。现在按 `characterInfo[i].vertexIndex` **只数字形四角**，
                //    读的仍是**真上传的那一份**；上面三条改坏法逐条复核过、**都还成立**（不裁时字形整段在视口外）。
                {
                    // ⚠️ **本块自己把滚动拉回 0**（下面那句 `cs.SetOffset(0f)` 是同一件事 ⇒ 它成了空操作）：
                    //    切「下沿」这张卡只在**不滚**时才有（`CardsViewport` 924px = 2.4 行 ⇒ 第 3 行只露出
                    //    上面一截）；滚 192 时第 3 行露出 348px ⇒ 卡上那几段字**刚好又回到视口里**了，
                    //    那一条就成了「没验」（`qEdge` 恒 0）。
                    cs.SetOffset(0f);
                    int bIdx = -1;
                    for (int i = 0; i < CollectionWindow.CardsCols * 3; i++)
                    {
                        var rr = cs.Shift(CollectionWindow.CardsCellRect(i));
                        if (rr.y1 < vp.y2 - 8f && rr.y2 > vp.y2 + 0.5f) { bIdx = i; break; }
                    }
                    CheckTrue(bIdx >= 0, "（前提）起手就有一张卡**压在下沿上**（格子下半截在视口外）");
                    var botCard = bIdx >= 0 ? FindChild(tabsRoot, "CollectionCard_" + bIdx) : null;
                    CheckTrue(botCard != null, $"压在下沿的那张卡（`CollectionCard_{bIdx}`）建出来了");
                    float qy1 = float.MaxValue, qy2 = float.MinValue;
                    int qn = 0, qEdge = 0, qOut = 0;
                    if (botCard != null)
                        foreach (var tmp in botCard.GetComponentsInChildren<TMPro.TextMeshPro>(true))
                        {
                            if (tmp == null) continue;
                            var tmf = tmp.GetComponent<MeshFilter>();
                            var tm = tmf != null ? tmf.sharedMesh : null;   // ⛔ 不是 `tmp.textInfo`
                            if (tm == null) continue;
                            var tvs = tm.vertices;
                            if (tvs == null) continue;
                            //  🔴 **2026-10-11（F3）：只数「字形」的四角**（`characterInfo[i].vertexIndex`），
                            //     ⛔ 别再扫整条 `tvs` —— 那份数组里**还夹着非字形的退化占位槽**（零面积、画不出来）：
                            //     TMP 对**不可见的字**（空格 / 超出 `maxVisibleCharacters` 的字）把四角全写成
                            //     `Vector3.zero`（`TextMeshPro.cs:4536-4541`）**却照旧占 4 个槽**
                            //     （`:4550-4552 FillCharacterVertexBuffers(i)` 照跑），尾巴上还有一段零槽。
                            //     换算成像素后它们停在**这一段字自己的原点**上，而 `ClipTmpMesh` 只认 `isVisible`
                            //     的字 ⇒ **压根不夹它们** ⇒ 卡压在下沿时它们留在视口外，整条一扫就成了
                            //     「字画到视口外」（**当天实红：期望 [0] 实得 [12] = 3 槽 × 4 点，一个字形都没有**）。
                            //     ⚠️ 改法**不是**退回 `textInfo`：读的仍旧是**真上传的那一份**，只是按字取下标。
                            //     ⚠️ 详细判据（含「为什么不能整条扫」「为什么只收 `materialReferenceIndex == 0`」）
                            //     → 本文件 `GlyphVertsOf`。
                            var gv = GlyphVertsOf(tmp, tvs.Length);
                            for (int i = 0; i < gv.Count; i++)
                            {
                                var p = LayoutSpace.ToPixel(tmp.transform.TransformPoint(tvs[gv[i]]));
                                qn++;
                                qy1 = Mathf.Min(qy1, p.y); qy2 = Mathf.Max(qy2, p.y);
                                if (Mathf.Abs(p.y - vp.y1) <= 0.5f || Mathf.Abs(p.y - vp.y2) <= 0.5f) qEdge++;
                                if (p.x < vp.x1 - 0.5f || p.x > vp.x2 + 0.5f
                                    || p.y < vp.y1 - 0.5f || p.y > vp.y2 + 0.5f) qOut++;
                            }
                        }
                    CheckTrue(qn > 0, $"（前提）那张卡上的 TMP 量得到**上传的网格里的字形顶点**（{qn} 个角）"
                                      + " —— 量到 0 就是这一条没验（⛔ 别当通过）");
                    Check(qOut, 0, "★ 卡上没有一个**字的顶点**画到视口外（读 `MeshFilter.sharedMesh`："
                                  + "**真上传的那一份**、按 `characterInfo[i].vertexIndex` 只数字形，⛔ 不是 `textInfo`）");
                    CheckTrue(qEdge >= 4,
                              $"★ 而且**真有字被夹在视口边上**（贴边 `{vp.y1:F2}` / `{vp.y2:F2}` 的顶点 {qEdge} 个；"
                              + $"整卡字的 y 范围 {qy1:F1}..{qy2:F1}）—— 那几段字全在卡高 58% 以下、"
                              + "而这张卡只露出上面一小截 ⇒ **整块越界** ⇒ 夹完四角落在同一条边上；"
                              + "**不裁的话它们整段留在视口外**");
                }

                // **起手（不滚）也有**：第一排卡的 `SDF 影` 比卡本体大一圈（4.4281 vs 3.3313 卡单位、
                // 画出来 510.4px 见方）⇒ 影子**上沿本来就压在视口上沿之上 ≈62px**
                // （卡中心 347.9 + `ShadowY` 1.45 − 影子半高 255.2 ≈ 94.2 < 155.9）——
                // 这一截原来一直画在页头那条空带上。
                cs.SetOffset(0f);
                var top0 = FindChild(tabsRoot, "CollectionCard_0");
                float zx1, zy1, zx2, zy2;
                int zverts;
                CheckTrue(RenderExtentPx(top0, out zx1, out zy1, out zx2, out zy2, out zverts) && zverts >= 20,
                          $"起手第一张卡量得到渲染几何（{zverts} 个顶点）");
                CheckTrue(zy1 >= vp.y1 - 0.5f && zy1 <= vp.y1 + 2.5f,
                          $"★ 起手那张卡的**影子也被裁在视口上沿**（最高顶点 {zy1:F1}，视口上沿 {vp.y1:F1}；"
                          + "没裁的话是 ≈94.2 —— 那 62px 就画在页头上了）");

                // 对照：**没被上沿切到的那一排**（第 2 排）⇒ **纵向不许被裁**（只在越界时裁，不是一律压扁）
                var fullCard = FindChild(tabsRoot, "CollectionCard_" + CollectionWindow.CardsCols);
                float fx1, fy1, fx2, fy2;
                int fverts;
                CheckTrue(RenderExtentPx(fullCard, out fx1, out fy1, out fx2, out fy2, out fverts) && fverts >= 20,
                          $"对照：第 2 排那张卡量得到渲染几何（{fverts} 个顶点）");
                CheckTrue(fy1 > vp.y1 + 100f,
                          $"对照：没压边的那一排**纵向没被裁**（最高顶点 {fy1:F1}，比视口上沿 {vp.y1:F1} 低 "
                          + $"{fy1 - vp.y1:F0}px —— 被一起裁掉的话它会停在 {vp.y1:F1}）");
                CheckTrue(zy1 <= vp.y1 + 2.5f && fy1 - zy1 > 100f,
                          $"★ 两张卡的**最高顶点差 {fy1 - zy1:F0}px** ⇒ 裁切只落在压边的那一张上"
                          + $"（压边那张停在 {zy1:F1} = 视口上沿）");
                // ⚠️ 横向则**每张卡都被裁**：`SDF 影` 比卡宽一圈（510.4 vs 241），第一列那张的左沿会伸到
                //    213.6 < 视口左沿 330.2 ⇒ 那 116.6px 原来画在视口左边之外（抽屉收起时看得见）。
                CheckTrue(fx1 >= vp.x1 - 0.5f,
                          $"★ 第 2 排那张卡的**左沿也被裁在视口左沿上**（{fx1:F1} ≥ {vp.x1:F1}）"
                          + " —— 影子的横向溢出（到 213.6）在抽屉收起时本来是看得见的");
                cs.SetOffset(savedC);
            }
            // 🆕 2026-10-08（A212）：`Shared/Close Button` 那颗钮的字（**"Back"**）——原版 **`折行=0`**
            //  （实读：`Button Text … 'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0`）。
            //  改坏法：删掉 `Shell/CollectionWindow.cs` 那颗钮后面的 `SetWrapping(false)` ⇒ 退回
            //  `SetAutoFitBox` 开出来的 `1` ⇒ 红。
            {
                var closeBtn = FindChild(root, "Close Button");
                var bk = closeBtn != null ? FindChild(closeBtn, "Button Text") : null;
                var bl = bk != null ? bk.GetComponent<Label>() : null;
                CheckTrue(bl != null, "`Shared/Close Button/Button Text` 在（A22② 那颗 Back 钮）");
                Check(bl != null ? bl.WrappingMode : -1, 0,
                      "★ `Back` 那颗钮的字 **`折行=0`**（原版实读，别让 `SetAutoFitBox` 开的折行留着）");
                // 🆕 **2026-10-09（A265）**：那颗 `Button Text` 用的是**它自己的矩形 132.86×48.24**，
                //   **不是**整颗钮的 150×60。中心只差 0.25px ⇒ `CheckAt` 那一档**分辨不出**这两态，
                //   所以必须**单独断框宽/框高**（`SetAutoFitBox` 写进 `sizeDelta` 的就是这两数）。
                //   判据 = 原版 dump 字面量（`python 工具/menu_dump.py bundle_menus_assets_all
                //   "Collection Menu Variant" --depth 12` 第 255 行：
                //   `Button Text  200.5, 89.3 → 333.4, 137.5   132.86   48.24`）。
                //   ⚠️ 读的是**场景里那个 TMP 的 `rectTransform.sizeDelta`**（= `SetWrapWidth` 真写进去的值），
                //      **不是**传进 `SetAutoFitBox` 的实参、也不是代码里那两个常量。
                //   改坏法：`Shell/CollectionWindow.cs` 里把 `bt` 换回 `cr`（整颗钮 150×60）
                //   ⇒ 下面两条读 150.00 / 60.00 ⇒ **红**。
                //   ⚠️ 「132.86×48.24 是序列化的、不是 ARF 算的」已查实：那颗节点上的
                //      `AspectRatioFitter` 是 `m_Enabled=0`（判据与算式 → `CollectionWindow.BackTxtL` 注释）。
                {
                    var tmp = bl != null ? bl.GetComponentInChildren<TMPro.TextMeshPro>() : null;
                    CheckNear(tmp != null ? tmp.rectTransform.sizeDelta.x * 108f : -1f, 132.86f, 0.5f,
                              "★ `Back` 的**文本框宽 = 132.86**（= 333.4 − 200.5；原版那颗 `Button Text` 自己的矩形，"
                              + "⛔ 不是整颗钮的 150）");
                    CheckNear(tmp != null ? tmp.rectTransform.sizeDelta.y * 108f : -1f, 48.24f, 0.5f,
                              "★ …框高 = 48.24（= 137.5 − 89.3；同一颗节点的另一半）");
                    CheckAt(bk, 200.5f, 333.4f, 89.3f, 137.5f,
                            "`Button Text` 那颗字**在它自己的矩形中心**（原版 200.5,89.3→333.4,137.5）");
                }
            }
            // 筛选：**复用卡组编辑那套 `DeckEditorState.Filter`**（别写第二套）
            {
                int all = win.CardsVisibleCount;
                var f = DeckFilter.None; f.Rarity = "legendary";
                CollectionWindow.CardsState.SetFilter(f);
                win.RebuildCardsPage();
                CheckTrue(win.CardsVisibleCount > 0 && win.CardsVisibleCount < all,
                          $"筛 `legendary` ⇒ 可见卡 **{all} → {win.CardsVisibleCount}**（筛选真的接上了）");
                CollectionWindow.CardsState.SetFilter(DeckFilter.None);
                win.RebuildCardsPage();
                Check(win.CardsVisibleCount, all, "清空筛选 ⇒ 卡数回到原来的数");
            }

            // ---------------- Cards 页：**完整筛选面板**（正本 §五·1；A3 + `menu_rect.py` 实读）----------------
            Section("Cards 页：筛选面板（7 行 · 面板内坐标逐条比原版）");
            var fltPanel = FindChild(tabsRoot, "Card Filters");
            CheckTrue(fltPanel != null, "面板节点 `Card Filters` 建了");
            if (fltPanel != null)
            {
                // 原版**起手是收起的**（整栏滑到 `hiddenPosition=(-550,0)`，行程 −385px）
                // ⇒ 我们也是「收起 = 滑出去 + 滑完 SetActive(false)」（🆕 A11 起；原来只有后面那半截）
                CheckTrue(!fltPanel.gameObject.activeSelf, "**起手收起**（原版 `hiddenPosition = (-550, 0)`）");
                win.ToggleFilters();
                CheckTrue(fltPanel.gameObject.activeSelf, "`Filters` 圆钮 ⇒ 面板打开");

                CheckAt(fltPanel, 0.25f, 335.56f, 155.9f, 1079.99f,
                        "面板矩形 = **0.25,155.9 → 335.56,1080**（原版 x/w；高按**屏幕可见**的 924.1 裁）");
                CheckNear(FilterPanelModel.ContentHFor(CollectionWindow.CardsState), 1389.02f, 0.1f,
                          "内容高 = **1389.02** = 79.02+50+50 + **Army 550**（13 格 3 列 = 5 行）+ 280+230+150"
                          + "（⚠️ 2026-09-28 前写的是 989.02 —— 那是把 Army 行当成 150 算的，见 `FilterPanelModel.ArmyRowH`）");
                var fscr = win.FilterScroll;
                CheckTrue(fscr != null, "面板挂了滚动区（原版 `Scroll View` sens **50**、`Viewport` + `Mask showGraphic=0`）");
                if (fscr != null)
                    CheckNear(fscr.MaxOffset, 1389.02f - 924.1f, 0.6f,
                              "可滚量 = **约 464.9**（1389.02 − 924.1）—— **「Cost / Type 够得着」的判据**");

                // 🔴 「一个值 ≠ 全部情况」（铁律 5·c）：**选项表是从 MB 实读的**，不是按枚举直觉编
                //    （`Card*Filter.options` 的 `alternativeText`）：Army 13 / Rarity 5 / Cost **8** / Type 3
                //    + `Owned only` / `Upgradable only` 两格（原版 `filters[6]` 里的前两个）
                Check(win.FilterCellCount, 31,
                      "格子总数 = **31** = Owned + Upgradable + Army **13** + Rarity **5** + Cost **8** + Type **3**"
                      + "（⚠️ Cost 是**区间档**不是每费一格；⚠️ Type **没有防御卡那一档**）");

                // Army 第 1 格：Content 从行内 y+50 起、pad L14、cell 100×100
                //   ⚠️ 阵营名别写死 —— 按 `CardsState.Factions()`（**排序过**）的第 1 个取
                var facs = CollectionWindow.CardsState.Factions();
                CheckTrue(facs.Count > 0, $"卡池里有 {facs.Count} 个阵营（Army 行按它铺格）");
                var a0 = facs.Count > 0 ? FindChild(fltPanel, "Cell_fac_" + facs[0]) : null;
                CheckTrue(a0 != null,
                          "Army 那一格在（按**名字**找，不按序号 —— 视口外的格不建）；"
                          + "第 1 格 = 「" + (facs.Count > 0 ? facs[0] : "?") + "」");
                if (a0 != null)
                {
                    CheckNear(PxOf(a0.position.x), 64.25f, 0.6f, "Army 第 1 格中心 x = **64.25**（0.25+14+50）");
                    CheckNear(PxYOf(a0.position.y), 434.92f, 0.6f,
                              "Army 第 1 格中心 y = **434.92**（155.9+179.02+50+50）");
                    CheckArt(a0, DeckRuntime.FactionIcon(facs[0]), "Army 格的图 = 该阵营图标（原版运行时赋）");
                    // 🆕 2026-10-05（A32③）：Army 行**关着时**的 off 色 = `(0.5,0.5,0.5,1)`（出厂无阵营筛选 ⇒ 全是关的）
                    CheckTrue(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Faction),
                              "（前提）没有阵营筛选 ⇒ Army 格是【关】的（下面断的是 off 色）");
                    CheckTint(FindChild(a0, "Background"), new Color(0.5f, 0.5f, 0.5f, 1f),
                              "Army 格 off 色 = 原版 `offColor (0.5,0.5,0.5,1)`（不是共用的 0.349）");
                }
                // Rarity 第 1 格：Content 从行内 y+65 起；图**比格小**（格 100²、图 50² 居中）
                var r0 = FindChild(fltPanel, "Cell_rar_common");
                CheckTrue(r0 != null, "Rarity 的 Common 格在");
                if (r0 != null)
                {
                    CheckNear(PxYOf(r0.position.y), 999.92f, 0.6f,
                              "Rarity 第 1 格中心 y = **999.92**（155.9 + **729.02** + 65 + 50；729.02 = Army 行 550 之后）");
                    CheckArt(r0, "1_40k_cardframe_rarity_common", "Rarity Common 的图 = `1_40k_cardframe_rarity_common`");
                    var bg = FindChild(r0, "Background");
                    CheckNear(Wpx(bg), 50f, 2f, "Rarity 格里的图宽 = **50**（格 100 ⇒ 图只有一半，原版如此）");
                    // 🔴 实拍又抓一条：标签像**贴着面板左边界、被切掉**。
                    //    做法同 CLAUDE.md 的「量渲染真值」——**量 Label 自己的宽度算左边缘**，别只看节点位置。
                    //    ⚠️ 第一版这条断言**恒真**（宽度量出来 0 ⇒ 左边缘 = 右边界）⇒ 现在**同时要求宽度合理**。
                    //    ⚠️ 第二版又抓出 `Legendary` 105.9px > 原版格宽 100 ⇒ 补上原版的 `auto(10-27)`
                    //      （`SetAutoFitBox`），再断「缩完之后落进格子里」。
                    var lab0 = FindChild(r0, "Label");
                    var lb0 = lab0 != null ? lab0.GetComponent<Label>() : null;
                    float lw = lb0 != null ? lb0.WorldW * 108f : 0f;
                    float lleft = lab0 != null ? PxOf(lab0.position.x) - lw * 0.5f : -999f;
                    CheckTrue(lb0 != null && lw > 20f,
                              $"Rarity 格的标签**量得出宽度**（实测 {lw:F1}px）—— 量到 0 就是「TMP 在非激活对象上量不出尺寸」");
                    CheckTrue(lb0 != null && lw <= 100.5f,
                              $"标签**缩进原版的 100px 格宽里**（实测 {lw:F1}px ≤ 100）"
                              + " —— 判据是原版那几处标着 `auto(10-27)`；不开自适应会冲出格子");
                    CheckTrue(lb0 != null && lb0.FontPxNow >= 9.5f && lb0.FontPxNow <= 27.5f,
                              $"标签字号落在原版 `auto(10-27)` 区间里（实测 {lb0?.FontPxNow:F1}px）");
                    // 🆕 **2026-10-12（A333 · F1 §三 #3）：上限那一格是【27】，不是标称的 23.2** ——
                    //   判据 = 原版 `Collection Menu Variant` 两页各一颗的 `m_fontSizeMax = 27`
                    //   （普查逐颗实读 → `资料/普查产出_1011/V7_A305_A304_普查.md`；本件亲跑
                    //    `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12 --md`
                    //    的 `auto[10.0~27.0]` 一节）。本工程原来把**调用方那一档（`fontPx 23.2`）**当上限。
                    //   🔴 口径（F1 §三 末）：**反射直读 TMP 的真字段**（`Label.FontSizeMax`，不是我们的账本），
                    //      期望值 **27 写字面量**（原版资产字段）——
                    //      ⛔ **不许读 `FilterPanelModel.RarityFontAutoMax` 来比**（那是被测实现里的常量 = 自证）。
                    //   ⚠️ `FontSizeMax` 是 **TMP 的 `fontSize` 单位**（不是 px）⇒ 经 `Label.FontSizeToPx` 折回；
                    //      `_tmp == null`（点阵兜底）时它恒 0 ⇒ 折出 0 ⇒ **这条会红，不会静默绿**。
                    float rarMaxPx = lb0 != null ? Label.FontSizeToPx(lb0.FontSizeMax) : -1f;
                    CheckNear(rarMaxPx, 27f, 0.35f,
                              $"★ Rarity 族 `Label` 的**上限** = **原版 `m_fontSizeMax` 27.0 px**（实得 {rarMaxPx:F2}；"
                            + "标称那一档是 23.2）"
                            + "。改坏法：把 `Core/FilterPanelModel.cs` 的 `LabelAutoMax = RarityFontAutoMax` 删掉"
                            + " ⇒ 上限退回标称 23.2 ⇒ 红");
                    CheckTrue(Mathf.Abs(rarMaxPx - 23.2f) > 0.5f,
                              $"…而且它**不是**标称那一档 23.2（实得 {rarMaxPx:F2}）—— 这条证明上面那个 27 分得出两种状态");
                    // 🆕 2026-10-08（A212）：**折行**要按这一族自己的原版值 —— 稀有度族 = `0`
                    //  （`SetAutoFitBox` 会**无条件**把 `m_TextWrappingMode` 开成 `1` ⇒ 不显式还原就是「碰巧错」）。
                    //  ⛔ 别按 `LabelCenter` 之类反推（那会把三族一起漏掉）；判据 = 原 prefab 那一格自己的字段。
                    Check(lb0 != null ? lb0.WrappingMode : -1, 0,
                          "★ Rarity 族的标签 **`折行=0`**（原版实读；删掉 `TextAligned` 里那句 `SetWrapping` ⇒ 退回 1 ⇒ 红）");
                    CheckTrue(lb0 != null && lleft >= 0f && lleft + lw <= 335.6f,
                              $"Rarity 格的标签**落在面板内**（左边缘 {lleft:F1}px · 文字宽 {lw:F1}px · "
                              + $"原版右对齐到格子右边 {114.25f:F1}px）");
                    // 🆕 2026-10-05（A32③）：Rarity 那一行**关着时**的 off 色 = `(0.5,0.5,0.5,`**0.749**`)`
                    //   —— 原来是**一份共用值 0.349**（Army/Rarity 两行都偏深，而且丢了 alpha）。
                    CheckTrue(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Rarity),
                              "（前提）没有稀有度筛选 ⇒ Rarity 格是【关】的");
                    CheckTint(FindChild(r0, "Background"), new Color(0.5f, 0.5f, 0.5f, 0.749f),
                              "Rarity 格 off 色 = 原版 `offColor (0.5,0.5,0.5,0.749)`（原来是共用的 0.349）");
                }

                // ============================================================ 🆕 2026-10-05（A32①③④）
                //  开关行那三件事：**状态换图** · **逐行 off 色** · **标签左对齐**。
                //  判据 = 原 prefab 的字段（`EverguildToggle.onSprite/offSprite` · `colorTintOnValueChange` ·
                //  `offColor` · `Label.m_HorizontalAlignment`）——复读命令见 `Core/FilterPanelModel` 的
                //  `OffTintFaction/Rarity/CostType` 与 `Build()` 里那两处长注释。
                {
                    // ① 状态换图（这扇窗原来**恒画 on 那张**：镜像模型时漏了 `IconOff` ⇒ 开/关只差一点色偏）
                    CheckArt(FindChild(fltPanel, "Cell_owned"), "40_main_bt_toggle_on",
                             "`Owned only`（出厂**开**）画的是 `40_main_bt_toggle_on`");
                    CheckArt(FindChild(fltPanel, "Cell_upgradable"), "40_main_bt_toggle_off",
                             "`Upgradable only`（出厂**关**）画的是 `40_main_bt_toggle_off` —— "
                             + "🔴 A32① 收的就是这一条（原来两扇窗都恒画 on 图）");
                    win.ApplyCardFilter("$owned");                       // 关掉它
                    CheckTrue(!CollectionWindow.CardsState.Filter.Owned, "（前提）`Owned only` 切到关");
                    CheckArt(FindChild(fltPanel, "Cell_owned"), "40_main_bt_toggle_off",
                             "关掉 ⇒ 换成 `40_main_bt_toggle_off`（原版 `changeSpriteOnValueChange = 1`）");
                    win.ApplyCardFilter("$owned");                       // 切回来
                    CheckArt(FindChild(fltPanel, "Cell_owned"), "40_main_bt_toggle_on",
                             "再切回来 ⇒ 又变回 on 那张（判据可逆，不是单向的）");

                    // ② 开关那一类**不吃 tint**（原版 `colorTintOnValueChange = 0`）⇒ 两个态都是**白**。
                    //    ⚠️ 这条是「原来那套色偏」的对照：改回 0.349 就红。
                    CheckTint(FindChild(FindChild(fltPanel, "Cell_upgradable"), "Background"), Color.white,
                              "开关格（`IconOff != null`）**不打 off 色偏**（原版 `colorTintOnValueChange = 0`"
                              + " ⇒ 开/关**只靠换图**区分）");

                    // ③ 标签**左对齐**（原版 `m_HorizontalAlignment = 1`）——挂在**文字左缘**上量，
                    //    不是看节点位置（判据同上面 Rarity 那条：`center − WorldW/2`）。
                    //    期望值 25.25 来自**原版 `Label` 的 rect 左缘**（`25.25,234.96→234.96,284.96`）。
                    var olab = FindChild(FindChild(fltPanel, "Cell_owned"), "Label");
                    var olb = olab != null ? olab.GetComponent<Label>() : null;
                    float owl = olb != null ? olb.WorldW * 108f : 0f;
                    float oleft = olab != null ? PxOf(olab.position.x) - owl * 0.5f : -999f;
                    CheckTrue(olb != null && owl > 20f, $"`Owned only` 的标签量得出宽度（实测 {owl:F1}px）");
                    CheckNear(oleft, 25.25f, 1.0f,
                              "`Owned only` 的标签**左对齐**在面板内 x = **25.25**（原版 `Label` rect 的左缘）"
                              + " —— 居中的话左缘会落在 ~95px（`LabelCenter` 改回 `true` 就红）");
                    // 🆕 2026-10-08（A212）：开关族（`Owned`/`Upgradable`）原版 **`折行=0`**。
                    Check(olb != null ? olb.WrappingMode : -1, 0,
                          "★ `Owned only` 的标签 **`折行=0`**（原版实读；`SetAutoFitBox` 会把它开成 1 ⇒ 必须显式还原）");
                }
                // Cost 第 1 格 / Type 那一行：**2026-09-28 起都在视口外** ——
                //   Army 行的高度现在按**它自己的内容**算（13 格 · 3 格/行 = 5 行 = 550），Rarity 及以下整排往下挪
                //   ⇒ **不滚到底，Cost / Type 的格子根本不建**（原版 `RectMask2D` 那套裁切 + 我们的视口剔除）。
                //   判据与两处证据（A3 的 150 vs 卡组编辑那棵树的 `332x0`）→ `FilterPanelModel.ArmyRowH` 的注释。
                if (fscr != null)
                {
                    CheckTrue(FindChild(fltPanel, "Cell_cost_1") == null && FindChild(fltPanel, "Cell_type_hero") == null,
                              "**不滚时 Cost / Type 的格子不建**（都在视口下方）");
                    fscr.ScrollBy(fscr.MaxOffset);                       // 滚到底
                    var c0 = FindChild(fltPanel, "Cell_cost_1");
                    CheckTrue(c0 != null, "滚到底 ⇒ Cost 的 `1-` 格建出来了");
                    if (c0 != null)
                    {
                        CheckNear(PxOf(c0.position.x), 47.75f, 0.6f, "Cost 第 1 格中心 x = **47.75**（0.25+15+32.5）");
                        // 面板内 y = 609.02+65+32.5（旧值，见 `ArmyRowH`）→ 现在 = 1009.02+65+32.5，再减可滚量 464.92
                        CheckNear(PxYOf(c0.position.y), 155.9f + 1009.02f + 65f + 32.5f - 464.92f, 0.8f,
                                  "Cost 第 1 格中心 y（滚到底后）= **797.5**");
                        CheckNear(Wpx(c0), 65f, 2f, "Cost 格 = **65×65**（原版 `cell 65×65`）");
                        CheckArt(c0, "Card_Frame_Cost_Icon", "Cost 格的图 = `Card_Frame_Cost_Icon`");
                        // 🆕 2026-10-05（A32③）：Cost / Type 两行的 off 色 = `(0.349,0.341,0.341,1)`
                        //   ⚠️ **三个通道不是一个数**（0.349/0.341）—— 写成 `(0.349,0.349,0.349)` 也会红，
                        //     这正是「照抄 prefab 字面量」与「随手抹一个灰」的区别。
                        CheckTrue(CollectionWindow.CardsState.Filter.Cost == DeckEditorState.AnyCost,
                                  "（前提）没有费用筛选 ⇒ Cost 格是【关】的");
                        CheckTint(FindChild(c0, "Background"), new Color(0.349f, 0.341f, 0.341f, 1f),
                                  "Cost 格 off 色 = 原版 `offColor (0.349,0.341,0.341,1)`");
                        // 🆕 2026-10-08（A212）：**费用桶是四族里【唯一】折行的那一族**（原版 `折行=1`）
                        //  ⇒ 三族设 0 的时候别把它一起设成 0（这条与 Rarity/Owned 那两条互为对照）。
                        var cvLab = FindChild(c0, "Label");
                        var cvLb = cvLab != null ? cvLab.GetComponent<Label>() : null;
                        Check(cvLb != null ? cvLb.WrappingMode : -1, 1,
                              "★ Cost 族的标签 **`折行=1`**（原版实读；三族一起设成 0 就红）");
                    }
                    var th = FindChild(fltPanel, "Cell_type_hero");
                    CheckTrue(th != null && PxYOf(th.position.y) + 50f <= 1080.5f,
                              "滚到底 ⇒ Type 那 3 格**完整落进视口**");
                    CheckArt(th, "40k_menu_search_icon_warlord", "Type 第 1 格（Warlord）的图 = `40k_menu_search_icon_warlord`");
                    CheckTrue(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Type),
                              "（前提）没有类型筛选 ⇒ Type 格是【关】的");
                    CheckTint(FindChild(th, "Background"), new Color(0.349f, 0.341f, 0.341f, 1f),
                              "Type 格 off 色 = 原版 `offColor (0.349,0.341,0.341,1)`");
                    CheckTrue(FindChild(fltPanel, "Cell_type_unit") != null
                              && FindChild(fltPanel, "Cell_type_tactic") != null,
                              "Type 另两格 `Troops` / `Stratagem` 也在（图 = `..._troop` / `..._stratagem`）");
                    fscr.ScrollBy(-fscr.MaxOffset);                  // ⚠️ 量完**滚回顶部** —— 下面几条断言
                    //    （四个小标题、搜索框）量的都是**顶部**那些件，不滚回去它们根本不建
                }

                // 四行的小标题（原版 `Title` TMP · **fs32 · hAlign=Left/Middle**）
                //   🔴 2026-09-23 **实拍补的缺口**：第一版只建了格子、**四个标题一个都没建**，
                //      96 条断言全绿 —— 因为它们不是「摆错位」而是「根本不在」，而当时没有盯这一条的断言。
                //   🔴 **2026-10-05（A93②）**：判据那句 `hAlign=Center` 是**读错了** —— 原版是 `Left/Middle`
                //      （判据 = `Core/FilterPanelModel.cs` 的 `TitleFontPx` 那段，两扇窗逐行实读）。
                //      对齐单独断（下面那四条量**左沿**），不只是「在不在」。
                foreach (var ttl in new[] { "Army", "Rarity" })
                    CheckText(TextOf(FindChild(fltPanel, "Title " + ttl)), ttl,
                              $"小标题 `{ttl}` 在（原版 `Title` TMP fs32；它在**顶部视野内**）");
                // 🔴 **左沿断言（4 条，2026-10-05 A93②）** —— 期望值是**原版的读数**，不是我们的常量：
                //   原版面板内左沿 = Army 行 **0** · 其余三行 **25**（`Collection Menu Variant` 的 Cards 页
                //   `Title` 实读 `0.3 / 25.3`、面板原点 `0.3`；卡组编辑那棵同族 `2.2 / 27.2`、原点 `2.2`）
                //   ⇒ 画布绝对 x = 面板原点 **0.25** + 那个数 = **0.25 / 25.25**（**写死字面量**，
                //   ⛔ 不拿 `CollectionWindow.FltL` + 模型常量去算 = 那是自证）。
                //   量的东西：`Label.WorldW`（TMP `textBounds` 的**真测量**）反推的**渲染左缘** ——
                //   ⛔ 不是节点位置、更不是「对齐枚举 == Left」（那是同义反复，改坏实现照样绿）。
                //   改坏会红：把 `CollectionWindow.TitleRow` 里那句 `MenuDraw.AlignLeft` 删掉
                //   （或 `Title.Left` 退回 false）⇒ 左沿落在矩形**中心**附近（Army ~129 / 其余 ~135）。
                CheckNear(TitleLeftPx(fltPanel, "Army"), 0.25f, 1f,
                          "小标题 `Army` 的**渲染左沿** = 面板内 **0**（原版 `Title` `m_HorizontalAlignment=1`"
                          + " ⇒ 左对齐；居中画的话会落在 ~129）；量不出来时这里给 −9999");
                CheckNear(TitleLeftPx(fltPanel, "Rarity"), 25.25f, 1f,
                          "小标题 `Rarity` 的**渲染左沿** = 面板内 **25**（原版那三行都从 x=25 起；居中会落在 ~135）");
                // ⚠️ 2026-09-28：`Energy Cost` / `Type` 两个标题落在 Army 行（550 高）之后 ⇒ **要滚下去才建**
                if (fscr != null)
                {
                    fscr.ScrollBy(fscr.MaxOffset);
                    foreach (var ttl in new[] { "Energy Cost", "Type" })
                        CheckText(TextOf(FindChild(fltPanel, "Title " + ttl)), ttl,
                                  $"小标题 `{ttl}` 在（滚到底之后才够得着）");
                    // 同样量左沿（**滚到底之后**才够得着 ⇒ 这两条必须在 `ScrollBy(-MaxOffset)` 之前）
                    CheckNear(TitleLeftPx(fltPanel, "Energy Cost"), 25.25f, 1f,
                              "小标题 `Energy Cost` 的**渲染左沿** = 面板内 **25**（滚到底时量的）");
                    CheckNear(TitleLeftPx(fltPanel, "Type"), 25.25f, 1f,
                              "小标题 `Type` 的**渲染左沿** = 面板内 **25**（滚到底时量的）");
                    fscr.ScrollBy(-fscr.MaxOffset);
                }

                // ---- 筛选**真的接上了**（每一条都拿 `DeckEditorState` 的结果数对照）----
                int all = win.CardsVisibleCount;
                System.Action<string> clickCell = key =>
                {
                    var cell = FindChild(tabsRoot, key);
                    var hit = cell != null ? FindChild(cell, "Hit") : null;
                    var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    if (wb != null) wb.Click();
                };
                clickCell("Cell_rar_legendary");
                var vis = CollectionWindow.CardsState.VisibleCards();
                CheckTrue(vis.Count > 0 && vis.Count < all, $"点 `Legendary` ⇒ 卡池 {all} → {vis.Count}");
                bool allLeg = true;
                foreach (var c in vis) if (c.Rarity != "legendary") { allLeg = false; break; }
                CheckTrue(allLeg, "筛出来的**每一张**都是 `legendary`");

                win.ClearCardFilters();
                Check(win.CardsVisibleCount, all, "`Clear filters` ⇒ 卡数回到 " + all);

                // ⚠️ Cost / Type 的格子**滚到底才建**（见上一段那条）⇒ 点它们之前先滚下去
                if (fscr != null) fscr.ScrollBy(fscr.MaxOffset);
                clickCell("Cell_cost_8");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allGe8 = vis.Count > 0;
                foreach (var c in vis) if (c.Cost < 8) { allGe8 = false; break; }
                CheckTrue(allGe8, $"点 `8+` ⇒ {vis.Count} 张**全部 cost ≥ 8**"
                                  + "（原版 Cost 是**区间档**：`1-`/2…7/`8+`，不是每费一格）");
                win.ClearCardFilters();

                clickCell("Cell_cost_1");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allLe1 = vis.Count > 0;
                foreach (var c in vis) if (c.Cost > 1) { allLe1 = false; break; }
                CheckTrue(allLe1, $"点 `1-` ⇒ {vis.Count} 张**全部 cost ≤ 1**（下界那档的效果）");
                win.ClearCardFilters();

                clickCell("Cell_type_hero");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allHero = vis.Count > 0;
                foreach (var c in vis) if (c.Type != "hero") { allHero = false; break; }
                CheckTrue(allHero, $"点 `Warlord` ⇒ {vis.Count} 张**全部是督军**（= `CardTypeOptions.Hero`）");
                win.ClearCardFilters();
                if (fscr != null) fscr.ScrollBy(-fscr.MaxOffset);        // 回顶部（搜索框在上面）

                // 搜索框：**外壳自己没有键盘**（`PointerLayer` 原来明写「键盘没实现」）⇒ 2026-09-23 补上
                var pl = PointerLayer.Instance;
                CheckTrue(pl != null, "`PointerLayer` 在（键盘走它）");
                if (pl != null)
                {
                    clickCell("Name Filter");
                    CheckTrue(pl.TextEditing, "点搜索框 ⇒ **进入文本编辑**");
                    foreach (var ch in "impe") pl.TypeChar(ch);
                    CheckText(pl.TextBuffer, "impe", "逐字输入 `impe` ⇒ 缓冲对得上");
                    pl.EndText(true);
                    vis = CollectionWindow.CardsState.VisibleCards();
                    CheckTrue(vis.Count > 0 && vis.Count < all,
                              $"卡名筛 `impe` ⇒ 卡池 {all} → {vis.Count}（**外壳的键盘真的接上了**）");
                    win.ClearCardFilters();
                    Check(win.CardsVisibleCount, all, "再清空 ⇒ 回到 " + all);
                    CheckText(CollectionWindow.CardsState.Filter.Name ?? "", "", "清空把卡名也一起清了");
                    // 🔴 2026-09-23 实拍抓的：**搜索框停在 `impe_` 没回到占位符**（断言当时一条都没报）
                    //    ⇒ 补这一条盯**画面上的字**（`Label.Text`），别只盯状态
                    var nf = FindChild(FindChild(fltPanel, "Name Filter"), "Input Text");
                    CheckText(TextOf(nf), "Search",
                              "清空后搜索框回到占位符 `Search`（**实拍抓出来的那条**：光看状态量不到）");
                }

                // 🔴 层序：筛选栏**必须盖在卡池之上**（原版兄弟序 `Collection Display` 在前、`Card Filters` 在后）
                var fbg = FindChild(fltPanel, "Panel");
                var fq = fbg != null ? fbg.GetComponentInChildren<ImageQuad>() : null;
                if (fq != null && win.CardsCells.Count > 0)
                {
                    var mrs = win.CardsCells[0].GetComponentsInChildren<MeshRenderer>(true);
                    int maxQ = int.MinValue;
                    foreach (var mr in mrs)
                    {
                        if (mr.sharedMaterial == null) continue;
                        maxQ = Mathf.Max(maxQ, mr.sharedMaterial.renderQueue);
                    }
                    CheckTrue(maxQ >= 0 && fq.RenderQueue > maxQ,
                              $"筛选栏底板队列 **{fq.RenderQueue}** > 卡池最高层 **{maxQ}**"
                              + " —— 「面板被卡池盖住」那类 bug 的判据（矩形断言量不到它）");
                }
                Shoot("03_收藏_Cards_筛选栏.png");
                win.ToggleFilters();
                CheckTrue(!fltPanel.gameObject.activeSelf, "再点一次 `Filters` ⇒ 面板收起");
            }

            // ---------------- 🆕 2026-10-04（§三第29条 A11）：筛选栏的**滑入/滑出** ----------------
            //   判据（原文 → `资料/待办判据_卡面卡池与双语.md` §四 那条操作链 + `卡组编辑界面_查证_0920.md:434`）：
            //     `Filter Toggle` → `CollectionDisplay.OnEnable → ToggleFilters(bool)`
            //     → `CollectionFilterController.Toggle(bool,bool)` → `DOTween.Kill` +
            //       **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`：收起 x = `hiddenPosition.x` = **−550**、
            //       展开 x = `originalAnchorPosition.x` = **−165**（**两个都是父系里的 `anchoredPosition`**）
            //       ⇒ **行程 = −385px**（🆕 2026-10-04 订正，X3 审查的 R5 —— 原来我们按 −550 走，多 43%）；
            //       ⚠️ **只动 x**（`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）。
            //   ⚠️ 批处理**没有帧循环**（CLAUDE.md §二）⇒ `Update` 一次都不跑、`Toggle*` **直接到位**；
            //      动画本身由**确定性口**复验（`TickDrawers` / `SetDrawerProgressForTest`）——
            //      两条路走的是**同一个** `ApplyDrawerSlide`（不是「自检走一份、跑起来走另一份」）。
            //   🔴 **真红法（先说清把哪一行改坏它会红）** —— 2026-10-04 逐条推演订正过
            //     （X3 审查的 R12：原来 5 条里有 2 条说错）：
            //     · `ApplyDrawerSlide` 里 `lp.x += LayoutSpace.Px(FltHiddenDx) * (1 − p.Slide)` 那句**删掉**
            //       ⇒ 只红**两条**：「挪了半个行程」与「还差 10%」—— 而「回到原位」那条**照样绿**
            //       （它量的是「回到了 `BasePos`」，删掉那句它**恒**在 `BasePos`）。
            //     · 那句里把 **`LayoutSpace.Px(...)` 去掉**（= 同步点实跑抓到的那条 **R1**：px 裸进世界单位）
            //       ⇒ 上面那两条红（275→29700、55→5940）**而且**「★ 这一格此刻还在屏内」也红
            //       （实测量到 −5876 —— 那一条正是「点不到」的前提，前提塌了「点不到」就没意义）。
            //     · 行程改回 −550（行程偏 43%）⇒ 上面两条也红（它们把**轨迹**钉住了，不只是「回没回原位」）。
            //     · `StepDrawer` 里 `dt / FltAnimTime` 改成 `dt` ⇒ 「推进 0.15s ⇒ 进度 0.5」红。
            //     · `SetDrawerInteractive` 里 `WindowButton.enabled` 那一行删掉 ⇒ 红的是 **②「点不到」**
            //       （那一行是**唯一**关这些命中区的地方 —— 建的时候那次 `force: true` 也走它；
            //        位置断言照样全绿，正是「画面看着对、真鼠标还点得到」那类静默 bug）；
            //       而 ③「推到 1 **又点得到**」会**照样绿**（按钮从没被关过）—— 说清是哪一条才有用。
            //     · 「撤登记滚动区」那半份删掉 ⇒ ②的「滚轮也不该被这一列吃掉」红。
            //     · `DrawerSettled` 改成恒 `true` ⇒ 「不参与命中/滚轮」「起点不算到位」「不再吃命中/滚轮」三条红。
            //     · `ToggleFilters` 不调 `StartDrawerSlide`（只翻 `Open`）⇒ 「点一次 ⇒ 进度 1」与
            //       「到位了 ⇒ 命中/滚轮生效」红。
            //     · `BuildDeckFilterDrawer` 收尾那次 `force: true` 退回 `force: false`（= X3 审查的 **R4**）
            //       ⇒ Deck 页那三条数 `WindowButton.enabled` 的断言红（第 1 条最直接）。
            Section("筛选栏的滑入/滑出：位移 + 0.3 秒 + 位移期间命中/滚轮失效（A11）");
            {
                var pl3 = PointerLayer.Instance;
                CheckTrue(pl3 != null, "`PointerLayer` 在（下面两条要拿它问「真鼠标点不点得到」）");
                // ⚠️ **前置：把前几段留下的弹窗收干净** —— ② 那两条走的是**真命中路**（`ButtonAt`），
                //    而 `Deck info Popup` 的 `Warlord Image` 命中区盖着 x∈[−109, 999]、y∈[−34, 1074]（比屏还大），
                //    层又是 `QDIHit = 3123` > 筛选格的 `QFltHit = 3043` ⇒ 留着它，
                //    「点不到 / 点得到」两条量到的都是**它**顶掉的结果（不是筛选格的真值）。
                // 🔴 **2026-10-04（A66）：这笔债已经还清** —— 原来一路开到收工的是 **2 扇**
                //    （`_tmp_view/collection.log:6055`「收掉 2 扇」）：A31 那一段建的四扇里 **`v2`/`v3` 没关**
                //    （上面已补），加上 `Practice Deck` 那一段的 `dp3`（也补了兜底）。
                //    ⇒ 这一段从「只打日志」升成**断言**：走到这里还开着 = **新开的一笔债**，当场红，
                //      别再被下面这次清扫静默盖住（收紧的判据，不是放松）。
                int closedLeftovers = 0;
                foreach (var lw in Object.FindObjectsByType<DeckInfoPopup>(FindObjectsSortMode.None))
                    if (lw != null && lw.CurrentState != WindowState.Closed) { lw.Close(); closedLeftovers++; }
                Check(closedLeftovers, 0,
                      "★ A11 前置：这一路跑完，场上**没有**还开着的 `Deck info Popup`（A66 —— 有 = 又漏关了一扇"
                      + (closedLeftovers == 0 ? "）" : $"：实测 {closedLeftovers} 扇，它层 3123、"
                         + "`Warlord Image` 命中区比屏还大，会顶掉下面两条真命中路）"));
                // 起点：上面那一段刚把它收回去
                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "起点进度 = **0**（整栏停在 `hiddenPosition` 那一头）");
                CheckTrue(!win.DrawerSettled(0), "…不参与命中/滚轮");

                win.ToggleFilters();                                   // 开（批处理里直接到位）
                CheckNear(win.DrawerSlide(0), 1f, 0.001f, "点一次 ⇒ 进度 **1**（原位）");
                CheckTrue(win.DrawerSettled(0), "…到位了 ⇒ 命中/滚轮生效");
                float baseCx = PxOf(fltPanel.position.x);

                // ① 时间推进：0.15 秒 = 半个 0.3 ⇒ 进度正好 0.5（这一步**只有真按 `animationTime` 走**才成立）
                win.SetDrawerProgressForTest(0, 0f);
                CheckTrue(fltPanel.gameObject.activeSelf,
                          "钉在**滑出来的起点**（进度 0 · 目标 1）⇒ 节点**还活着** —— 原版也是先 `SetActive(true)` 再动 tween，"
                          + "不然滑出来的那 0.3 秒根本看不见东西（**滑完**那一下才关，见本段末尾）");
                CheckTrue(!win.DrawerSettled(0), "…起点当然不算到位 ⇒ 命中/滚轮仍失效");
                win.TickDrawers(0.15f);
                CheckNear(win.DrawerSlide(0), 0.5f, 0.01f, "推进 **0.15 秒**（`animationTime` = 0.3）⇒ 进度 **0.5**");
                CheckTrue(fltPanel.gameObject.activeSelf, "…滑动途中整栏**活着**（要看得见它在滑）");
                float midCx = PxOf(fltPanel.position.x);
                CheckNear(baseCx - midCx, 385f * 0.5f, 1f,
                          "…而且**真的挪了半个行程**（原位 → 左移 **192.5px** = 385 × 0.5，"
                          + "行程 = 原版 `−550 −(−165)`）");

                // ② 位移没停稳 ⇒ 命中区与滚轮都失效（**这一条量的是「真鼠标路」，不是我们自己那个布尔**）
                win.SetDrawerProgressForTest(0, 0.9f);
                CheckNear(baseCx - PxOf(fltPanel.position.x), 385f * 0.1f, 1f,
                          "进度 0.9 ⇒ 离原位还差 **10%**（左移 **38.5px**；位移 = `行程 × (1 − 进度)`、只动 x）");
                var rc = FindChild(fltPanel, "Cell_rar_common");
                var rcq = rc != null ? rc.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(rcq != null, "拿得到一格（否则下面两条等于没验）");
                if (rcq != null)
                {
                    float cx = PxOf(rcq.transform.position.x), cy = PxYOf(rcq.transform.position.y);
                    // ★ 前置：这一格**现在还在屏内** —— 否则「点不到」是因为它滑出屏外、不是命中失效（等于没验）
                    CheckTrue(cx > 0f && cx < 1920f && cy > 0f && cy < 1080f,
                              $"★ 这一格此刻**还在屏内**（{cx:F0},{cy:F0}）—— 这条是下面「点不到」那个断言的前提");
                    var wbMid = pl3 != null ? pl3.ButtonAt(cx, cy) : null;
                    CheckTrue(wbMid == null,
                              "★ 位移没停稳 ⇒ **真命中路点不到它**（`PointerLayer.CollectHits` 只挑 "
                              + "`WindowButton.isActiveAndEnabled`，`Shell/PointerLayer.cs:488`）—— 拿到 "
                              + (wbMid == null ? "**null** ✓" : "`" + wbMid.name + "` = **还点得到**"));
                    var sMid = pl3 != null ? pl3.ScrollUnder(cx, cy) : null;
                    CheckTrue(sMid != win.FilterScroll,
                              "★ …滚轮也不该被这一列吃掉（`HitScroll` 那条 `Owner.activeInHierarchy` 的语义；"
                              + "拿到 " + (sMid == null ? "**null** ✓" : "`" + sMid.GetType().Name + "`"));
                }

                // ③ 推到目标 ⇒ 回原位、命中恢复（**同一格、同一条真命中路** —— 与 ② 正好两态）
                win.TickDrawers(0.2f);
                CheckNear(win.DrawerSlide(0), 1f, 0.001f, "再推 0.2 秒 ⇒ 收尾到 **1**（`MoveTowards` 夹住）");
                CheckNear(PxOf(fltPanel.position.x), baseCx, 0.5f, "…回到原位（位移是加在 `BasePos` 上的）");
                CheckTrue(win.DrawerSettled(0), "…到位 ⇒ 命中/滚轮**恢复**");
                if (rcq != null)
                {
                    var wbBack = pl3 != null
                        ? pl3.ButtonAt(PxOf(rcq.transform.position.x), PxYOf(rcq.transform.position.y)) : null;
                    CheckTrue(wbBack != null && rc != null && wbBack.transform == FindChild(rc, "Hit"),
                              "★ 同一格现在**又点得到了**（拿到 " + (wbBack == null ? "**null**" : "`" + wbBack.name + "`") + "）");
                }

                // 收尾：还它一个「收着」的状态（后面那些断言/截图按这个来）
                win.ToggleFilters();
                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "再点一次 ⇒ 进度回 **0**（整栏滑出去）");
                CheckTrue(!fltPanel.gameObject.activeSelf, "…滑完就整块关掉（省渲染；原版那套也配 `SetActive`）");
                CheckTrue(!win.DrawerSettled(0), "…并且不再吃命中/滚轮");
                // 「收起」的语义 = **整栏真的在屏幕左外**（不是停在原位隐身）——
                //   判据要的是这一条，不是某个定点（原版那个 −550 是**父系里的绝对锚点值**；
                //   我们的**行程** = `hiddenPosition.x − originalAnchorPosition.x` = −385px，见 `ApplyDrawerSlide`）
                CheckTrue(PxOf(fltPanel.position.x) + CollectionWindow.FltView.W * 0.5f < 0f,
                          "…而且此刻**整栏都在屏幕左外**（右缘 "
                          + (PxOf(fltPanel.position.x) + CollectionWindow.FltView.W * 0.5f).ToString("F1")
                          + " < 0）—— 这才叫「滑出去」"
                          + "（行程 −385 时右缘 = −49.4；⚠️ 行程若再缩小到 < 335 这条就红）");
            }

            // ---------------- 🆕 2026-10-05（§三第29条 **A76①**）：**起滑那一下必须把命中区按下去** ----------------
            //   判据（原文 → `资料/已知的坑.md`「同一个洞会在下一个新入口重开」· `待办判据_阶段二与联机.md` §A25⑥）：
            //   `StartDrawerSlide` 的 **Play 那一支**原来调 `ApplyDrawerSlide(p, p.Slide)`（`force` 取默认 `false`）
            //   ⇒ **收起态起滑**时算出来的 `on=false` 与 `p.Interactive` 的现值**相等**（false）⇒
            //   撞上 `SetDrawerInteractive` 里「没变就不动」那条短路 ⇒ **整栏的 `WindowButton` 一下都不按**。
            //   只要哪个新入口建完漏了收尾那次 `force: true`（新按钮的出厂默认就是 `enabled=true`），
            //   症状就是「**滑入那 0.3 秒里真鼠标点得到**」——画面在滑、命中区却是活的（X3 的 R4 形态）。
            //   ⚠️ **批处理里 `Toggle*` 走的是「直接到位」那一支** ⇒ Play 这一支**一处断言都覆盖不到**
            //     （这正是 2026-10-04 那次「改成它就完了 ⇒ 没有断言能覆盖」的顾虑所在）⇒ 本段用**故障注入**补齐：
            //     ① 先把按钮按成脏态（= 模拟一个漏了 `force` 的新入口）② 再走**真正的起滑口**（`playLike: true`）。
            //   🔴 **真红法**（逐条推演过，别抄成「全红」）：把 `StartDrawerSlide(p, playLike)` 里那句
            //     `ApplyDrawerSlide(p, p.Slide, true)` 退回 `force: false` ⇒ 红的是 **②与④两条**
            //     （起滑那一下按钮**一个都不会被关**）；**③ 照样绿** —— 它量的是「推到 1 之后」，
            //     那时 `on` 变成 `true`、与 `Interactive` 的 `false` 不相等 ⇒ 本来就会真按一遍。
            //     把 `SetDrawerInteractive` 里 `wbs[i].enabled = on;` 删掉 ⇒ ②③④**全红**（那行是**唯一**
            //     按命中区的地方，建的那次 `force` 也走它）—— 说清是哪几条才有用。
            //   ⚠️ 与上面那三条**不是重复**：那三条量的是「建完之后**静止**时是关的」（`Toggle*` 那一支），
            //     这一段量的是「**起滑那一刻**即使状态被弄脏也会被按下去」——两条各管一个入口。
            Section("抽屉起滑口：**脏态起滑 ⇒ 命中区当场全关**（A76① · 只覆盖 Play 那一支）");
            {
                CheckTrue(!win.DeckFiltersOpen && !win.DrawerSettled(3),
                          "前置：Deck 抽屉此刻是**收起态**（起滑前 `Slide`/`SlideTarget` 都还是 0）");
                // ① 故障注入：把这一栏的命中区按回出厂默认（`enabled=true`），而 `Interactive` 仍是 false
                int stale3 = win.MakeDrawerHitsStaleForTest(3);
                var dfNode = FindChild(win.PageRoot(0), "Deck Filters");
                var dfWbs = dfNode != null
                    ? dfNode.GetComponentsInChildren<WindowButton>(true) : System.Array.Empty<WindowButton>();
                CheckTrue(stale3 > 0 && dfWbs.Length == stale3,
                          $"★ ① 注入的按钮数 = 真读到的 {dfWbs.Length} 个（`MakeDrawerHitsStaleForTest` 报 {stale3}）"
                          + " —— 数不上的话下面两条等于没验");
                int onA = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onA++;
                CheckTrue(dfWbs.Length > 0 && onA == dfWbs.Length,
                          $"★ ① 注入后 **{dfWbs.Length}** 个命中区**全开着**（实测 {onA}）—— 这就是「新入口漏了 `force`」的样子；"
                          + "而 `DrawerSettled(3)` 仍是 false ⇒ 脏态 = 「按钮开着 + 标志是关的」");
                // ② 真正的起滑口（Play 那一支；`Toggle*` 在批处理里永远走不到这里）
                win.StartDrawerSlideForTest(3, true, true);
                CheckNear(win.DrawerSlide(3), 0f, 0.001f,
                          "② 起滑这一刻进度**还是 0**（动画刚要开始 —— 下面那条量的是「滑入期间」而不是「到位后」）");
                CheckTrue(!win.DrawerSettled(3), "② …所以按定义还没到位");
                int onB = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onB++;
                CheckTrue(dfWbs.Length > 0 && onB == 0,
                          $"★ ② 起滑那一下 ⇒ 命中区**当场全关**（实测还开着 **{onB}** 个）—— "
                          + "这一条红了就说明 `StartDrawerSlide` 的 Play 分支把 `force` 丢了（A76①）");
                // ③ 对照组：推完动画 ⇒ 全回来（证明 ② 不是「一刀切关死」）
                win.TickDrawers(0.3f);
                CheckNear(win.DrawerSlide(3), 1f, 0.001f, "③ 推 0.3 秒（原版 `animationTime`）⇒ 进度 **1**（到位）");
                CheckTrue(win.DrawerSettled(3), "③ …到位了 ⇒ 命中/滚轮生效");
                int onC = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onC++;
                CheckTrue(dfWbs.Length > 0 && onC == dfWbs.Length,
                          $"★ ③ …而且 **{dfWbs.Length}** 个命中区**全回来**（实测 {onC}）"
                          + " —— ②③ 两态正好相反：写成恒关（或恒开）都过不了这两条");
                // ④ 起滑口是**共用**的（`StartDrawerSlide` 只有一份）—— Cards 那一栏同样得按下去
                //   ⚠️ 自己按页找节点（`PageRoot(1)` = Cards 页）—— `Card Filters` 这个名字**Styles 页也有一份**
                //     （同一个 builder 建的），别用「全树第一个」去找（那可能落到另一页的份上）。
                int stale0 = win.MakeDrawerHitsStaleForTest(0);
                var cfNode = FindChild(win.PageRoot(1), "Card Filters");
                var cfWbs = cfNode != null
                    ? cfNode.GetComponentsInChildren<WindowButton>(true) : System.Array.Empty<WindowButton>();
                CheckTrue(stale0 > 0 && cfWbs.Length == stale0,
                          $"★ ④ Card Filters 那一栏也注入成脏态（真读到 {cfWbs.Length} 个命中区，注入报了 {stale0} 个）");
                win.StartDrawerSlideForTest(0, true, true);
                int onD = 0; foreach (var b in cfWbs) if (b != null && b.enabled) onD++;
                CheckTrue(cfWbs.Length > 0 && onD == 0,
                          $"★ ④ 同一份实现 ⇒ 它起滑时也一样**全关**（实测还开着 {onD} 个）");
                // 还它一个「收着」的状态（后面那些断言/截图按这个来）
                win.ToggleFilters();                       // Cards 抽屉：Open 翻回 false ⇒ 批处理那一支直接到位
                win.ToggleDeckFilters();                   // Deck 抽屉：同上
                CheckTrue(!win.FiltersOpen && !win.DrawerSettled(0), "自检收尾：Cards 抽屉收回**收起**态");
                CheckTrue(!win.DeckFiltersOpen && !win.DrawerSettled(3), "自检收尾：Deck 抽屉收回**收起**态");
                int onE = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onE++;
                CheckTrue(dfWbs.Length > 0 && onE == 0, "自检收尾：Deck 那一栏的命中区**又关回去**了");
            }

            // 🔴 层序：**窗口底图必须在页内容之下** —— `CardView` 的各层都落在默认队列 **3000**
            //    （全工程只有 SDF 那层显式设过 3000），而基类给 `Background` 的是 **3005**
            //    ⇒ 实拍抓到过：**整片卡池被底图盖住、画面全空，而所有矩形断言全绿**。
            //    判据只有这一条（量队列，量不到「谁盖谁」）。
            {
                var bgN = FindChild(root, "Background");
                var bq = bgN != null ? bgN.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bq != null && bq.RenderQueue < 3000,
                          $"窗口底图的队列 = **{bq?.RenderQueue}**，必须 **< 3000**（`CardView` 的层都在 3000）"
                          + " —— 「底图盖住卡池」那个 bug 的判据");
                if (bq != null && win.CardsCells.Count > 0)
                {
                    // ⚠️ **`CardView` 的层不是 `ImageQuad`**（那套是卡面渲染器，走**自定义 mesh + MeshRenderer**）
                    //    ⇒ 队列要量**材质**的 `renderQueue`（渲染真值），量 `ImageQuad` 会得到空数组。
                    var mrs = win.CardsCells[0].GetComponentsInChildren<MeshRenderer>(true);
                    int minQ = int.MaxValue, cnt = 0; string qlist = "";
                    foreach (var mr in mrs)
                    {
                        if (mr.sharedMaterial == null) continue;      // **没材质的渲染器不画东西**，不算层
                        cnt++; minQ = Mathf.Min(minQ, mr.sharedMaterial.renderQueue);
                        qlist += mr.sharedMaterial.renderQueue + ",";
                    }
                    CheckTrue(cnt > 0 && minQ > bq.RenderQueue,
                              $"卡池的格**每一层**都在底图之上（底图 {bq.RenderQueue} · 格内最低队列 {minQ}；"
                              + $"{cnt} 层，实测 [{qlist}]）");
                }
            }

            // ---- `Empty Collection Warning`（§三 第 15 条 **第 50 行**；2026-09-24 补建）----
            // 判据 = 原版 `CollectionDisplay.RefreshCollection`：**过滤后为空** ⇒ `SetActive(true)`
            //        （反汇编 `工具/disasm_va.py` 读 `0x1815ECC5B`）。
            // 坐标 = **135.22,70.94 → 1970.01,1080** —— **比父还宽、左右都溢出**，原版就这样，别"修正"。
            {
                // ⚠️ **必须限定在 Cards 页里找** —— 四个页**各有一份**同名 `Empty Collection Warning`
                //    （原版就这样，矩形逐页不同，见 `CollectionWindow.BuildDeckList` 那条注释）。
                //    用 `FindChild(tabsRoot, …)` 会先撞上 **Deck 页**那一份（第一版就栽在这儿，差 15.33px）。
                var ew = FindChild(win.PageRoot(1), "Empty Collection Warning");
                CheckTrue(ew != null, "Cards 页有 `Empty Collection Warning` 这一件（原版**四页各一份**）");
                if (ew != null)
                {
                    CheckAt(ew, 135.22f, 1970.01f, 70.94f, 1080f, "`Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf, "没筛选 ⇒ **不显示**（原版出厂 `act=F`）");
                    // 🔴 **正例与反例必须成对** —— 只验「不显示」的话，「永远不显示」也能过
                    var f = DeckFilter.None;
                    f.Name = "zzz_本地没有这张卡_zzz";
                    win.UiSetCardFilter(f);
                    CheckTrue(ew.gameObject.activeSelf,
                              "筛到一个**空结果** ⇒ **显示**（原版判据 `filteredCollection.Count <= 0`）");
                    win.ClearCardFilters();
                    CheckTrue(!ew.gameObject.activeSelf, "`Clear filters` ⇒ 又关回去（判据跟着数据走）");
                }
            }

            // 两张截图：当前还停在 Cards 页 ⇒ 先拍它，再切回 Decks 拍第一张
            Shoot("02_收藏_Cards.png");
            win.tabButtons.Click(0);
            Shoot("01_收藏_Decks.png");

            // ---------------- Cosmetics 页：233 张卡背（正本 §五；A4 §二 + §2·1）----------------
            Section("Cosmetics 页：卡背网格（列数**算出来的** · 250×405 · 233 张）");
            win.tabButtons.Click(2);
            Check(win.CurrentTab, WindowTabType.CollectionCosmetics, "点第 3 键 ⇒ 切到 **Cosmetics** 页");
            CheckTrue(win.PageRoot(2) != null, "页节点 `Cardback Tab` 在（**卡面文案是 `COSMETICS`**）");
            Check(CollectionWindow.CosmoTotal, 233,
                  "卡背读数 = **233**（`Resources/Art/cardbacks/`；全在**工程外**导进来的，见正本 §七 ③）");
            // 🔴 列数是**算出来的**：A4 §2·1 原来写「`_segments=5` 是定值」⇒ **已就地更正**
            //    （`_controlSegmentSize=1` ⇒ `ConfigureColumnNumber` 按宽度覆盖 `_segments`，证据同 A3 §3·5）
            // 🔴 **2026-09-24 整套订正**：本页原来那组坐标（`168.27,85→1920,1080`、7 列）是
            //    **在 `Content Area` 的局部系里**算的 ⇒ 整页偏 (167.17, 70.94)、列数**多算一列**。
            //    真值（从 `Collection Menu Variant` 根一路走下来）见 `CollectionWindow.CosmoView` 的注释；
            //    根因是 `menu_rect.py` 父链那个坑（`资料/已知的坑.md`）。
            Check(CollectionWindow.CosmoCols, 6,
                  "列数 = **6** = floor(1584.56 ÷ 250)（⚠️ **不是** `_segments=5` —— 那个是死值；"
                  + "也**不是**上一版算的 7 —— 那是拿偏了 167.17px 的视口宽算出来的）");
            var cpage = win.PageRoot(2);
            var cbh = cpage != null ? FindChild(cpage, "Scroll View") : null;
            CheckAt(cbh, 335.44f, 1920.01f, 155.94f, 1080f, "`Scroll View`（卡背视口 **1584.56 × 924.06**）");
            CheckTrue(win.CosmoCells.Count > 0, $"画出了 {win.CosmoCells.Count} 格（视口外的不建 = 那套裁切）");
            if (win.CosmoCells.Count > 0)
            {
                var k0 = win.CosmoCells[0];
                // 内容**整体居中**：pad = (1584.56 − 6×250) / 2 = **42.285** ⇒ 首格中心 335.44+42.285+125
                CheckNear(PxOf(k0.position.x), 502.73f, 0.6f, "第 1 格中心 x = **502.73**（335.44 + pad 42.285 + 250/2）");
                CheckNear(PxYOf(k0.position.y), 358.44f, 0.6f, "第 1 格中心 y = **358.44**（155.94 + 405/2）");
                var art = FindChild(k0, "Cardback");
                CheckNear(Wpx(art), 250f, 2f, "格里的卡背宽 = **250**（原版 `Cardback` 铺满 250×405）");
                CheckNear(Hpx(art), 405f, 2f, "格里的卡背高 = **405**");
                CheckTrue(art != null && art.GetComponentInChildren<ImageQuad>() != null
                          && art.GetComponentInChildren<ImageQuad>().Texture != null,
                          "卡背**真的有贴图**（不是空图 —— 导没导错就看这一条）");

                // ---- 🆕 2026-09-26：卡背底下那层 **SDF**（原版 `Cardback Shadow SDF`）----
                // 逐值出处：`资料/普查产出_0923/A4_装饰页与驱动链.md:91` ——
                //   rect **-42.5,-70.87,337.5,550.8**（格式是 `x,y,w,h`，y 向下相对格左上）·
                //   锚点 (-0.17,-0.185)-(1.18,1.175) + sizeDelta (0,0)（拉伸）· `act=T` ·
                //   组件 = Image + `UIImageMaterialColorChanger`（换色即悬停高亮）。
                var sdf = FindChild(k0, "Cardback Shadow SDF");
                CheckTrue(sdf != null, "卡背格里有 **`Cardback Shadow SDF`** 那一层（原版两层的底那层）");
                if (sdf != null)
                {
                    // 🔴 **2026-10-08 就地订正（铁律 5）—— 这两条原来写 337.5 / 550.8，现在红在 479.93。**
                    //    根因**不是新缺陷**，是**旧断言拿「布局矩形」当「渲染矩形」量**：
                    //    · `k0` 是**第一排**那一格（上沿 = 视口上沿 155.94），而 SDF 比格大一圈、**上溢 70.875px**；
                    //    · 原版 `Cardback Display/Scroll View/Viewport` 上挂着 `RectMask2D`
                    //      （`m_Softness=(0,0)` · `m_Padding=(0,0,0,0)`，实读见
                    //      `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）⇒ **那 70.875px 在原版里本来就画不出来**；
                    //    · A181（`Shell/CollectionWindow.cs` 的 `RebuildCosmoCells`）把这一层改走
                    //      `MenuDraw.Rect(…, CosmoView, …)` 之后，`WorldW/H` 量到的就是**截过的那一段**
                    //      （`MenuDraw.Rect` 把 quad 建在 `ClipRect` 求交后的矩形上）：
                    //      `CosmoView.y1` 155.94 → 格上沿 + 479.925 = 635.865 ⇒ 高 **479.925**（= 原来报的 479.93）。
                    //    ⇒ **实现是对的（更贴原版），错的是期望值**；⛔ 别为了这两条把 `CosmoView` 实参去掉
                    //      （那正是 A181 修掉的真偏离）。修法 = **两件事分开、各有一条能红的断言**：
                    //      ① 这一格断「**真被视口上沿截住**」；② 「布局矩形 = 337.5 × 550.8」改到
                    //      **整块落在视口里**的那一格上量（见下面那组「对照」）。
                    {
                        var sq0 = sdf.GetComponentInChildren<ImageQuad>();
                        float sh0 = sq0 != null ? sq0.WorldH * 108f : -1f;
                        float st0 = sq0 != null ? PxYOf(sq0.transform.position.y) - sh0 * 0.5f : -1f;
                        CheckNear(sh0, 479.925f, 1.5f,
                                  "★ 第一排那一格的 SDF **上溢被视口截住**：渲染高 = **479.925**"
                                + "（= `CosmoView.y1` 155.94 → 格上沿 + 479.925；原版布局高 550.8 里那 70.875 在上溢那一段）");
                        CheckNear(st0, CollectionWindow.CosmoView.y1, 0.8f,
                                  "…而且它**正好停在视口上沿 155.94**（= 原版 `RectMask2D` 那条边，不是「整层被关掉」）");
                    }
                    var qi = sdf.GetComponentInChildren<ImageQuad>();
                    CheckTrue(qi != null && qi.Texture != null
                              && qi.Texture.name.EndsWith("_sdf"),
                              "SDF 层贴的是**这张卡背自己的 `_SDF` 掩码**"
                            + "（`CosmeticItemCardback.GetCardBackSprites()` 成对返回；100×130.5 是距离场的本意）");
                    CheckTrue(qi != null && qi.GetComponent<MeshRenderer>().sharedMaterial != null
                              && qi.GetComponent<MeshRenderer>().sharedMaterial.shader != null
                              && qi.GetComponent<MeshRenderer>().sharedMaterial.shader.name
                                 == "Everguild/FX/Card Highlight And Shadow",
                              "SDF 层用的是**原版 shader** `Everguild/FX/Card Highlight And Shadow`"
                            + "（材质值照原版 `Card Backs SDF`，不是 shader 默认值 —— 否则会多一圈白框）");
                    // 🔴 **SDF 必须在卡背底下**：两层给的是**两个渲染队列**（同一个队列里谁盖谁不可控，踩过三次）
                    var qArt = art != null ? art.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(qi != null && qArt != null && qi.RenderQueue < qArt.RenderQueue,
                              $"SDF 的渲染队列**低于**卡背（{CollectionWindow.QPageSdf} < {CollectionWindow.QPageRow}）"
                            + "—— 同队列排不出稳定次序");
                }
                // ---- 对照（🆕 2026-10-08）：**整块落在视口里**的那一格 ⇒「渲染矩形 == 布局矩形 == 原版字面量」----
                //  上面那一格（`k0`）只能证明「上溢被截住」，量不到布局矩形本身 ⇒ 判据要挪到一格**四边都不越界**的：
                //  第 2 排第 2 列（`CosmoCells[CosmoCols + 1]`，`CosmoCellRect` 是**行优先**、视口外的格不建）：
                //    · SDF 相对格左上 = `−42.5, −70.875 → +295, +479.925`（= 宽 337.5 / 高 550.8，`rebuild` 里的逐值出处）；
                //    · 该格上沿 = `CosmoView.y1` 155.94 + 格高 405 = **560.94** ⇒ SDF 上沿 **490.065** > 视口上沿 ✓；
                //      左沿 = 335.44 + `CosmoPadX` 42.285 + 250 = **627.725** ⇒ SDF 左沿 **585.225** > 视口左沿 335.44 ✓；
                //      下沿 560.94 + 479.925 = **1040.865** < 1080 ✓ · 右沿 627.725 + 295 = **922.725** < 1920.01 ✓。
                //  **改坏法**：把 `RebuildCosmoCells` 里那句 `sr` 的 337.5 / 550.8 改错（或退回「按卡背等比内接」）⇒ 红。
                {
                    var sdfFull = win.CosmoCells.Count > CollectionWindow.CosmoCols + 1
                        ? FindChild(win.CosmoCells[CollectionWindow.CosmoCols + 1], "Cardback Shadow SDF") : null;
                    CheckTrue(sdfFull != null,
                              "（对照 · 前提）第 2 排那一格的 `Cardback Shadow SDF` 也在（它整块落在视口里 ⇒ 量得到布局矩形）");
                    if (sdfFull != null)
                    {
                        CheckNear(Wpx(sdfFull), 337.5f, 2f,
                                  "★ 对照（整块不越界的格）：SDF 层宽 = **337.5**（原版 `/…/Cardback Shadow SDF` 的 rect 宽；"
                                + "比 250 的卡背大一圈 —— 露出来的就是落地感）");
                        CheckNear(Hpx(sdfFull), 550.8f, 2f,
                                  "★ 对照（整块不越界的格）：SDF 层高 = **550.8**"
                                + "（`-42.5,-70.87,337.5,550.8`，格式是 x,y,w,h；比 405 的卡背高一圈）");
                    }
                }
            }
            // 页头（A3 那条「每页自己的实例值」：本页 35/33，**异画页是 42**）
            CheckText(TextOf(FindChild(cpage, "Header Label")), "Your cosmetics collection",
                      "页头标题 = `Your cosmetics collection`（原版 fs38）");
            CheckText(TextOf(FindChild(cpage, "Filters Label")), "Filters",
                      "页头 `Filters` 文案在（原版 **fs35** auto 10-35 —— ⚠️ 异画页同名的是 42）");
            CheckText(TextOf(FindChild(cpage, "Clear filters Text")), "Clear filters",
                      "页头 `Clear filters` 文案在（原版 **fs33** auto 10-33）");
            // 🔴 **实拍抓的一条**：第一版照抄了 dump 的 `1405`（= **布局组跑之前的模板位**，
            //    父容器 `Header/Filters` 是 425..1385 的 VLG）⇒ **它和右对齐到 1821 的标题叠在一起**。
            //    ⇒ 现在断「按钮在容器内」**并且**「标题与按钮不叠」—— 这类错**矩形断言量不到**，
            //      得**量文字自己的宽度**算边缘（同筛选栏标签那两条）。
            // 🔴 2026-09-24：**x 也从 425 改成 612.2** —— 与 Deck/Cards/Styles 三页**同一个值**
            //    （四页共用同一条 `Header`；理由见 `CollectionWindow.BuildFilterHeader` 的注释）。
            {
                var bq = FindChild(cpage, "Clear filters");
                CheckTrue(bq != null && Mathf.Abs(PxOf(bq.position.x) - 737.17f) <= 1f,
                          "`Clear filters` 底图中心 x = **737.17**（= 612.17 + 250/2 —— 与另外三页同值）"
                          + " —— **别照抄 dump 里的 1405 / 1488.59**（那是布局组跑之前的模板位）");
                var ttlT = FindChild(cpage, "Header Label");
                var clrT = FindChild(cpage, "Clear filters Text");
                var lt = ttlT != null ? ttlT.GetComponent<Label>() : null;
                var lc2 = clrT != null ? clrT.GetComponent<Label>() : null;
                if (lt != null && lc2 != null)
                {
                    float tLeft = PxOf(lt.transform.position.x) - lt.WorldW * 54f;
                    float cRight = PxOf(lc2.transform.position.x) + lc2.WorldW * 54f;
                    CheckTrue(tLeft > cRight,
                              $"标题（左缘 **{tLeft:F0}**，右对齐到 1821）与 `Clear filters`（右缘 **{cRight:F0}**）**不叠**");
                }
            }
            // 左抽屉：**出厂 act=F**，靠 `Filter Toggle` 开合
            CheckTrue(!win.CosmoFiltersOpen, "筛选抽屉 `Cosmetic FIlter` **起手是收起的**（实证 act=F）");
            var cHit = FindChild(cpage, "FiltersHit");
            var cBtn = cHit != null ? cHit.GetComponent<WindowButton>() : null;
            CheckTrue(cBtn != null, "`Filter Toggle` 有点击区");
            if (cBtn != null)
            {
                cBtn.Click();
                CheckTrue(win.CosmoFiltersOpen, "点 `Filters` ⇒ 抽屉打开");
                cBtn.Click();
                CheckTrue(!win.CosmoFiltersOpen, "再点 ⇒ 收起");
            }
            // ---------------- 🆕 2026-10-03（§三 第 29 条 A11）：卡背页的筛选抽屉（**两行都建了**）----------------
            //   判据 = `FilterPanelModel.BuildCosmetics`（与卡组编辑那扇窗**同一份模型** ——
            //   🔴 **2026-10-10 订正（A271）**：**不再逐字相同**了。那颗 `'Owned only'` 的**自适应下界**：
            //   **卡组编辑窗显式传 26**（原版 `auto[26~32]`，A247）· **本窗走缺省 18**（原版 `auto[18~32]`，本来就对））
            //        + `CardbackTable.NamesFor`（阵营 → 卡背，**判据只有一份**）。
            //   原来那句「A4 只给了容器 rect、没给格子尺寸 ⇒ 没建」**早就不成立**（2026-09-28 在卡组编辑实测补全）。
            Section("卡背页筛选抽屉：Army 13 格 + Owned（**行序 Army 在前**，与卡牌那套相反）");
            if (cBtn != null)
            {
                cBtn.Click();                       // 再开一次（上一组开合把它关回去了）
                CheckTrue(win.CosmoFiltersOpen, "抽屉开着才量得到格");
                var drw = FindChild(cpage, "Cosmetic FIlter");
                Check(win.CosmoFilterCellCount, 14,
                      "格子总数 = **14** = Army **13** + `Owned` **1**"
                      + "（⚠️ 这一页**没有**搜索框 / 稀有度 / 费用 / 类型 —— 原版那棵树里就没有）");

                var cf = CollectionWindow.CosmoState.Factions();
                CheckTrue(cf.Count > 0, $"阵营表有 {cf.Count} 个（按它铺 Army 格）");
                var ca0 = cf.Count > 0 ? FindChild(drw, "Cell_fac_" + cf[0]) : null;
                CheckTrue(ca0 != null, "Army 第 1 格在（按**名字**找，不按序号）");
                if (ca0 != null)
                {
                    // 面板内 (pad L14, 15+50) ⇒ `Abs` 加 (0.25,155.9) ⇒ 中心 (64.25, 270.9)
                    CheckNear(PxOf(ca0.position.x), 64.25f, 0.6f, "Army 第 1 格中心 x = **64.25**（0.25+14+50）");
                    CheckNear(PxYOf(ca0.position.y), 270.9f, 0.6f,
                              "Army 第 1 格中心 y = **270.9**（155.9+15+50+50）"
                              + " —— 这一页 **Army 在前**（卡牌那套是 Owned/Upgradable 在前）");
                    CheckArt(ca0, DeckRuntime.FactionIcon(cf[0]), "Army 格的图 = 该阵营图标（原版运行期赋）");
                }
                // `Owned` 行：`CosmoOwnedTop(13)` = 15 + 550 + 12.81 = 577.81 ⇒ 绝对行顶 733.71、中心 758.71
                var co = FindChild(drw, "Cell_owned");
                CheckTrue(co != null, "`Owned` 那一格在");
                if (co != null)
                {
                    CheckNear(PxYOf(co.position.y), 758.71f, 0.6f,
                              "`Owned` 行中心 y = **758.71**（155.9 + 577.81 + 25 —— Army 行 550 一高，它跟着往下走）");
                    CheckText(TextOf(FindChild(co, "Label")), "Owned only", "`Owned` 的标签文案");
                    // 🆕 **2026-10-09（A270）**：这颗 `'Owned only'` 的**自适应窗口**（TMP 真字段）。
                    //   🔴 **为什么单开一条**：今天**全仓没有一条断言量得到本窗这一颗** ——
                    //     本页走 `FilterPanelModel.BuildCosmetics` 的**缺省形参** `labelAutoMin`
                    //     （缺省 = 共用常量 `ToggleFontAutoMin` = 18），而**卡组编辑窗显式传 26**
                    //     （原版 `auto[26~32]`，A247）⇒ 谁动那个缺省值、或动那个共用常量，
                    //     **本窗会静默变歪而自检照样绿**。判据 = 原版卡背页那颗的实读值：
                    //     **字号 32 · `auto[18~32]` · 折行 0**（A270）—— **18 本来就是对的**。
                    //   ⚠️ 读的是 `Label.FontSizeMin/Max`（**TMP 里那两个真字段**，经工程唯一那份
                    //     `Label.FontSizeToPx` 折成画布 px），⛔ **不是** `cell.LabelAutoMin` 那个实参（那是自证）。
                    //   **改坏法**：把 `Core/FilterPanelModel.BuildCosmetics` 的缺省形参改成别的值（例：26）
                    //     ⇒ 下界读成 26 ≠ 18 ⇒ 下面那条红。
                    {
                        var olb = FindChild(co, "Label") != null
                                ? FindChild(co, "Label").GetComponent<Label>() : null;
                        CheckTrue(olb != null && olb.CanRenderChinese,
                                  "（前提）`Owned only` 那颗走的是**真 TMP** —— 点阵后端没有「自适应」这回事"
                                + "（`FontSizeMin/Max` 恒 0），不前置下面两条会假绿");
                        if (olb != null && olb.CanRenderChinese)
                        {
                            CheckNear(Label.FontSizeToPx(olb.FontSizeMin), 18f, 0.5f,
                                      "★ `Owned only` 的**自适应下界** = 原版 `m_fontSizeMin` **18px**"
                                    + "（本页走缺省形参 —— 卡组编辑窗那颗是 26，两窗**不再逐字相同**，A270/A271）");
                            CheckNear(Label.FontSizeToPx(olb.FontSizeMax), 32f, 0.5f,
                                      "★ …**上界** = 原版 `m_fontSizeMax` **32px**（= 原版那颗的 `m_fontSize`）");
                        }
                    }
                    CheckNear(Wpx(FindChild(co, "Background")), 70.59f, 1.5f,
                              "开关底图宽 = **70.59** = 0.3×335.31 − 30（原版那条锚点式子）");
                }

                // ---- **真的筛得动**（重画会重建格 ⇒ 每次点完要**重新找**那个节点）----
                System.Action<string> clickCell = key =>
                {
                    var c = FindChild(FindChild(cpage, "Cosmetic FIlter"),
                                      "Cell_" + key.Replace("$", "").Replace(":", "_"));
                    var h = c != null ? FindChild(c, "Hit") : null;
                    var b = h != null ? h.GetComponent<WindowButton>() : null;
                    CheckTrue(b != null, "格子 `" + key + "` 的点击区在");
                    if (b != null) b.ClickForTest();
                };
                int allN = CollectionWindow.CosmoTotal;
                if (cf.Count > 0)
                {
                    int wantFac = CardbackTable.NamesFor(cf[0], CardArt.CosmeticNames()).Length;
                    clickCell("$fac:" + cf[0]);
                    Check(CollectionWindow.FilteredCosmoNames().Length, wantFac,
                          $"点「{cf[0]}」⇒ 筛出 **{wantFac}** 张（判据 = `CardbackTable.NamesFor` 那一份）");
                    CheckTrue(wantFac > 0 && wantFac < allN,
                              $"…比全部 {allN} 张少 ⇒ **这是真筛**（不是摆设；`Army` 那半本来一直是空的）");
                    // 卡背格也跟着重画了（格数只能是**变少**）
                    CheckTrue(win.CosmoCells.Count > 0 && win.CosmoCells.Count <= wantFac,
                              $"卡背格重画了（{win.CosmoCells.Count} 格 ≤ {wantFac} 张）");
                    clickCell("$fac:" + cf[0]);        // 再点一次 = 取消（`FilterPanelModel.Click` 的语义）
                    Check(CollectionWindow.FilteredCosmoNames().Length, allN, "再点一次 ⇒ 取消阵营筛选，回到全部");
                }
                // `Owned only`：单机全解锁 ⇒ **切得动但不改变结果**（如实标的差异，不是静默失效）
                bool ownedBefore = CollectionWindow.CosmoState.Filter.Owned;
                clickCell("$owned");
                Check(CollectionWindow.CosmoState.Filter.Owned, !ownedBefore, "`Owned only` 那个开关切得动");
                Check(CollectionWindow.FilteredCosmoNames().Length, allN,
                      "…但**结果不变**（单机全解锁 —— 与卡组编辑那扇窗同一条如实标注）");
                clickCell("$owned");
                // `Clear filters`：回到全部
                if (cf.Count > 0) clickCell("$fac:" + cf[0]);
                win.ClearCosmoFilters();
                Check(CollectionWindow.FilteredCosmoNames().Length, allN, "`Clear filters` ⇒ 回到全部 233 张");
                CheckTrue(CollectionWindow.CosmoState.Filter.Faction == null
                          || CollectionWindow.CosmoState.Filter.Faction.Length == 0, "…阵营条件真的清掉了");

                Shoot("05_收藏_卡背筛选抽屉.png");
                cBtn.Click();                       // 收回去（下一张实拍要的是收起态）
                CheckTrue(!win.CosmoFiltersOpen, "量完收回去");
            }

            if (win.CosmoScroll != null && win.CosmoScroll.MaxOffset > 0f)
            {
                win.CosmoScroll.ScrollBy(win.CosmoScroll.MaxOffset);
                int last = CollectionWindow.CosmoTotal - 1;
                var lc = FindChild(cpage, "CollectionCosmetic_" + last);
                CheckTrue(lc != null && PxYOf(lc.position.y) + 405f * 0.5f <= 1080.5f,
                          $"**滚到底 ⇒ 第 {last + 1} 张（最后一张卡背）完整落进视口**");
                win.CosmoScroll.ScrollBy(-win.CosmoScroll.MaxOffset);
            }

            // ═══════════ 🆕 2026-10-08（A181）：卡背格的两层也被视口裁住 ═══════════
            //  判据 = 原版 `Cardback Display/Scroll View/Viewport` 上那颗 `RectMask2D`
            //  （`m_Softness = (0,0)` · `m_Padding = (0,0,0,0)`）。
            //  🔴 这一页最容易露馅：格里的 **`Cardback Shadow SDF` 比格大一圈**
            //  （左 −42.5 / 上 −70.875 / 下 +74.925，见 `RebuildCosmoCells` 那段逐值出处）⇒ 压边那几格
            //  光靠「格与视口求交」拦不住它。原来那两行是直调 `ImageQuad.Create` ⇒ **整块画出去**。
            //  **改坏法**：把 `MenuDraw.Rect(…, CosmoView, …)` 的 `CosmoView` 实参去掉 ⇒ 越界 quad 立刻出现 ⇒ 红。
            if (win.CosmoScroll != null)
            {
                var csc = win.CosmoScroll;
                float savedCO = csc.Offset;
                csc.SetOffset(192f);            // 上移 192 ⇒ 第一排格（155.94..560.94）被视口上沿切掉 192px
                var vpC = CollectionWindow.CosmoView;
                int cidx = -1;
                for (int i = 0; i < CollectionWindow.CosmoCols; i++)
                {
                    var rr = csc.Shift(CollectionWindow.CosmoCellRect(i));
                    if (rr.y1 < vpC.y1 - 0.5f && rr.y2 > vpC.y1 + 8f) { cidx = i; break; }
                }
                CheckTrue(cidx >= 0, "（前提）卡背页滚 192px 后第一排有一格压在视口上沿上");
                var ccell = cidx >= 0 ? FindChild(cpage, "CollectionCosmetic_" + cidx) : null;
                CheckTrue(ccell != null, $"被切的那一格（`CollectionCosmetic_{cidx}`）建出来了");
                if (ccell != null)
                {
                    int quads = 0, qover = 0;
                    foreach (var q in ccell.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (q == null || !q.gameObject.activeInHierarchy) continue;
                        float w = q.WorldW * 108f, h = q.WorldH * 108f;
                        float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
                        float x1 = cx - w * 0.5f, x2 = cx + w * 0.5f, y1 = cy - h * 0.5f, y2 = cy + h * 0.5f;
                        quads++;
                        if (y1 < vpC.y1 - 0.5f || y2 > vpC.y2 + 0.5f
                            || x1 < vpC.x1 - 0.5f || x2 > vpC.x2 + 0.5f)
                        {
                            qover++;
                            Debug.LogError(P + $"     ↳ `{q.name}` 的渲染矩形 {x1:F1},{y1:F1} → {x2:F1},{y2:F1} 越出视口");
                        }
                    }
                    CheckTrue(quads >= 2, $"那一格**两层都量得到**（实测 {quads} 个 quad = SDF + 卡背本体）");
                    Check(qover, 0, "★ 卡背格的两层都**落在视口内**（越界 quad 0 个 —— SDF 比格大一圈，最容易露）");
                }
                // 对照：没被切的那一排（第 2 排）⇒ 仍是**整格**（没被一起压扁）
                var cfull = FindChild(cpage, "CollectionCosmetic_" + CollectionWindow.CosmoCols);
                if (cfull != null)
                {
                    var cq = FindChild(cfull, "Cardback");
                    var cqq = cq != null ? cq.GetComponent<ImageQuad>() : null;
                    CheckTrue(cqq != null && Mathf.Abs(cqq.WorldH * 108f - CollectionWindow.CosmoCellH) < 2f,
                              $"对照：没被切的那一排卡背仍是**整格高 405**（实测 "
                              + $"{(cqq != null ? cqq.WorldH * 108f : -1f):F1}）—— 只在越界时裁，不是一律压扁");
                }
                csc.SetOffset(savedCO);
            }
            Shoot("04_收藏_Cosmetics.png");

            // `Empty Collection Warning`（**Cosmetics 页那一份**；矩形与别页又不同）
            {
                var ew = FindChild(win.PageRoot(2), "Empty Collection Warning");
                CheckTrue(ew != null, "Cosmetics 页有 `Empty Collection Warning`");
                if (ew != null)
                {
                    CheckAt(ew, 170.44f, 1970.00f, 70.94f, 1080f, "Cosmetics 页 `Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf,
                              $"233 张卡背 ⇒ 不显示（🆕 2026-10-03：抽屉两行齐了 ⇒ 这条判据**真能触发**；"
                              + "13 个阵营各 9~20 张 ⇒ 正常筛不空）");
                }
            }

            // ============================================================ 卡片详情窗（2026-09-24）
            // 出处：`资料/阶段二_卡片详情窗_原版规格.md`（逐节点表 + 三块面板 + 计数条）。
            Section("卡片详情窗（点一张卡开 · 原版菜单版 `CardDisplayWindow`）");
            win.tabButtons.Click(1);                       // 回 Cards 页
            {
                var cp = win.PageRoot(1);
                var hit0 = cp != null ? FindChild(cp, "CardHit_0") : null;
                var hb0 = hit0 != null ? hit0.GetComponent<WindowButton>() : null;
                CheckTrue(hb0 != null, "卡池第 1 格有点击区");
                if (hb0 != null)
                {
                    hb0.Click();
                    var cd = CollectionWindow.LastCardDetail;
                    CheckTrue(cd != null, "点第 1 格 ⇒ **开出了卡片详情窗**");
                    if (cd != null)
                    {
                        var disp = FindChild(cd.transform, "Card Display");
                        CheckAt(disp, 584f, 1336f, 106f, 974f, "`Card Display`（752 × 868）");
                        // 卡本体尺寸：量**渲出来的**网格包围盒（原版 = 523.25 × 832.75）
                        // 🔴 **只能量「卡框」那一层** —— 整棵子的包围盒会被**立绘抠图的溢出**撑大
                        //    （实测 1106.9px，是「角色越出卡框」那一层的功劳，第一版就这么误报的）。
                        //    卡框那层的贴图名以 **`frame_`** 开头（`CardArt.Frame(faction, rarity)` 给的，
                        //    落在 `Art/cards/frame_<阵营>[_strat]_tier<N>`）；
                        //    🔴 **别用 `_tier` 去找** —— SDF 那层叫 `<阵营>_tier<N>`（在 `Art/card_sdf/`），
                        //    也带 `_tier`，而且它是「软光/影」那张 **4.4281² = 1106.9px** 的方图（实测踩过）。
                        var mrs = disp != null ? disp.GetComponentsInChildren<MeshRenderer>(true) : null;
                        if (mrs != null && mrs.Length > 0)
                        {
                            Bounds? frameB = null;
                            foreach (var mr in mrs)
                            {
                                var tx = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
                                if (tx != null && tx.name.StartsWith("frame_")) { frameB = mr.bounds; break; }
                            }
                            CheckTrue(frameB.HasValue, $"找到了卡框那一层（{mrs.Length} 层网格里贴图名以 `frame_` 开头的那个）");
                            if (frameB.HasValue)
                            {
                                // 🔴 **2026-09-27 用户拍板：卡框改成【按固定矩形画】**（原来是「裁到 bbox 再等比内接」——
                                //    **那个设计是错的**）。硬证据：原版 `2DCard/CardFrame` 那个 Image 的
                                //    **`m_PreserveAspect = 0`** ⇒ 原版就是把框贴图**拉伸**进这个固定矩形的。
                                //    改之前：四档画形不同（bbox w/h：tier1/2 = 0.672、tier3/4 = 0.733，而矩形是 0.6893）
                                //    ⇒ 一律用 tier4 会让每张卡的框**矮 5.7%**（768.2）；改完**四档都是 814.25**（= 矩形本身）。
                                CheckNear(frameB.Value.size.y * 108f, 814.25f, 6f,
                                          $"**卡框渲出来的高 = {frameB.Value.size.y * 108f:F1}px**（原版 `CardFrame` = 561.25 × **814.25**；"
                                        + "按固定矩形画 ⇒ **与卡框档无关**）");
                                // ⚠️ **宽对不上，别拿它当判据**：我量到 535.19px、原版 `CardFrame` 是 561.25 —— 差 ~26px。
                                //    **还没查清**（疑 `CardView` 那层按贴图自身宽高比画、而原版 `CardFrame` 的 rect
                                //    比贴图比例宽）。已记进 `项目任务.md` §三 第 15 条，**不在本轮擅自改卡面**。
                                Debug.Log(P + $"   · 卡框宽实测 {frameB.Value.size.x * 108f:F1}px（原版 `CardFrame` 561.25 —— 差 "
                                          + $"{(frameB.Value.size.x * 108f - 561.25f):F1}px，**还没查清**，见 §三 第 15 条）");
                            }
                            CheckTrue(mrs.Length > 3, $"卡面上画了 {mrs.Length} 层网格（不是空卡位）");
                        }
                        // 三块面板的标题 —— 🔴 **2026-09-27：创建副本 / 升级两块【都不建】**（用户拍板，见 §三 第 21 条）
                        //   ⚠️ 原来这里断言的是这两块的**标题** —— 用户 2026-09-27 定了「不做升级、不做合成」
                        //     ⇒ 那两条判据**没有对象了**，改成「这两块根本不在」（`TitleOf` 查不到会返回 `(无)`）。
                        CheckText(cd.TitleOf("Crafting"), "(无)",
                                  "「创建副本」那块**不建**（本作不做合成 —— 用户 2026-09-27 拍板）");
                        CheckText(cd.TitleOf("Upgrade"), "(无)",
                                  "「升级」那块**不建**（本作不做升级 —— 用户 2026-09-27 拍板）");
                        CheckText(cd.TitleOf("AltArt"), "Alternate art",
                                  "异画面板标题 —— ⚠️ 原版这一格印的是**升级文案**（复制粘贴 bug），**我们不抄那个 bug**（出声）");
                        // 计数条：格式 = `x{min(拥有,卡组上限)}` + `"/ "` + `{拥有−该数}`
                        var cnt = cd.Counter;
                        CheckTrue(cnt != null, "`Card Counter` 在");
                        if (cnt != null)
                        {
                            var t1 = FindChild(cnt, "Counter");
                            string s1 = TextOf(t1);
                            CheckTrue(s1.StartsWith("x"),
                                      $"计数条左数 = **{s1}**（原版格式串 `\"x{{0}}\"`，= min(拥有, 卡组上限)）");
                            CheckText(TextOf(FindChild(cnt, "Slash")), "/ ",
                                      "中间那个 `/ ` **是写死的**（原版代码不改它）");
                            var t2 = FindChild(cnt, "Duplicates text");
                            CheckTrue(t2 != null && int.TryParse(TextOf(t2), out _),
                                      $"右数 = **{TextOf(t2)}**（= 多余副本数；> 0 ⇒ 走 `Duplicate Counter` 那一支）");
                        }
                        // 三块面板的动作：**创建副本 +1** / **升级 +1 级**（单机口径：不扣货币）
                        int cap = CardProgress.DeckCap(cd.Card.Rarity);
                        int owned0 = CardProgress.Owned(cd.Card.Id, cd.Card.Rarity);
                        var ch = cd.CraftHit; var chb = ch != null ? ch.GetComponent<WindowButton>() : null;
                        if (chb != null)
                        {
                            chb.Click();
                            Check(CardProgress.Owned(cd.Card.Id, cd.Card.Rarity), owned0 + 1,
                                  $"点 `Craft` ⇒ 拥有数 {owned0} → **{owned0 + 1}**（单机**不扣万能卡**，出声）");
                        }
                        int lv0 = CardProgress.Level(cd.Card.Id);
                        var uh = cd.UpgradeHit; var uhb = uh != null ? uh.GetComponent<WindowButton>() : null;
                        if (uhb != null)
                        {
                            uhb.Click();
                            Check(CardProgress.Level(cd.Card.Id), lv0 + 1, $"点 `Upgrade` ⇒ 等级 {lv0} → **{lv0 + 1}**");
                        }
                        // `Show Card Text` 切效果文字条；语音钮「没有就出声」
                        var ehit = cd.EyeHit; var ehb = ehit != null ? ehit.GetComponent<WindowButton>() : null;
                        if (ehb != null && cd.LoreVisible)
                        {
                            ehb.Click();
                            CheckTrue(!cd.LoreVisible, "点 `Show Card Text` ⇒ 效果文字条**藏起来**");
                            ehb.Click();
                            CheckTrue(cd.LoreVisible, "再点 ⇒ 显示回来");
                        }
                        // ================================================================
                        //  🆕 2026-09-27：「相关卡」那一块（**1 主卡 + 8 相关卡 = 9 格** · 扇形 · 换位）
                        //  判据 → 正本 **§8·7**（扇形真值 = 原版那条 legacy clip `Card Display Open`）
                        //  与 **§9·6**（换位：三个闸 · 0.25s 六条 tween · `SetSiblingIndex` 两两互换 ·
                        //  收尾重设 lore/语音）。用户口径 → `项目任务.md` §三 第 19 条。
                        //  ⚠️ **槽 0–4 是原版真值，槽 5–8 是我们外推的**（原版只有 5 格）—— 下面分开钉。
                        // ================================================================
                        {
                            // 量「卡框」那一层的渲染包围盒（同上面那条注释：整棵子会被立绘溢出撑大）
                            Bounds? FB(CardView v)
                            {
                                if (v == null) return null;
                                foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
                                {
                                    var tx = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
                                    if (tx != null && tx.name.StartsWith("frame_")) return mr.bounds;
                                }
                                return null;
                            }

                            CardDef poc = null, sgDef = null, moa = null;
                            foreach (var c in CardDatabase.Load())
                            {
                                if (c.Name == "Path of Command") poc = c;
                                if (c.Name == "Storm Guardian") sgDef = c;
                                if (c.Name == "Master of Arcana") moa = c;
                            }
                            CheckTrue(poc != null && sgDef != null,
                                      "（前提）卡池里有 `Path of Command` 与 `Storm Guardian`（用户举的那个例子）");
                            if (poc != null && sgDef != null)
                            {
                                cd.ShowCard(poc);      // 复用同一扇窗换一张卡（原版 `ShowCard` 就是这个意思）
                                CheckTrue(cd.SlotCount >= 2,
                                          $"`Path of Command` ⇒ 卡片那一叠 **{cd.SlotCount} 格**（主卡 + 相关卡）");
                                int sg = cd.SlotIndexOf(sgDef.Id);
                                CheckTrue(sg > 0,
                                          $"★ **`Storm Guardian` 就在这一叠里（第 {sg} 格）** —— "
                                        + "用户 2026-09-26 举的例子：卡面写 `Deploy a Storm Guardian`，"
                                        + "总不能让玩家不知道那张是什么");

                                // ① 前台那张 = 位姿槽 0：**屏心 (960,480) · 转角 0**（原版 clip 末帧）
                                var s0 = cd.SlotView(0);
                                var b0 = FB(s0);
                                CheckTrue(b0.HasValue, "位姿槽 0（前台）画出来了");
                                // 🔴 **位置断言要用「节点位置」，不能用渲染包围盒的中心**：
                                //    卡框那层（561.25×814.25）与卡本体（523.25×832.75）**本来就不同心**（差 ≈7.5px），
                                //    而且**旋转过的卡 AABB 还会再涨** —— 第一版就是这么误报的（实测 487.5 / 494.0）。
                                //    尺寸才用渲染盒（下面平台那条 `frame_` 高度就是渲染真值）。
                                if (b0.HasValue)
                                {
                                    var c0 = LayoutSpace.ToPixel(s0.transform.position);
                                    CheckNear(c0.x, 960f, 2f, "前台那张**中心 x = 960**（原版 clip 末帧 `anchoredPosition.x = 0`）");
                                    CheckNear(c0.y, 480f, 2f, "…**中心 y = 480**（原版 `anchoredPosition.y = 60` ⇒ 屏幕 y = 540−60）");
                                }

                                // ② 槽 1（第一张相关卡）：**原版真值 (−121,53) · 2.510° · scale 232.954**
                                var s1 = cd.SlotView(1);
                                if (s1 != null && s0 != null)
                                {
                                    var c1 = LayoutSpace.ToPixel(s1.transform.position);
                                    CheckNear(c1.x, 960f - 121f, 2.5f,
                                              "槽 1 中心 x = **839**（原版 clip：`anchoredPosition.x = −121` ⇒ 扇形**朝左开**）");
                                    CheckNear(c1.y, 480f + 7f, 2.5f,
                                              "槽 1 中心 y = **487**（原版 `y = 53` ⇒ 屏幕 y = 540−53，比前台**低 7**）");
                                    CheckNear(s1.transform.eulerAngles.z, 2.510f, 0.05f,
                                              "槽 1 转角 = **2.510°**（原版 clip 末帧的 z 旋转）");
                                    // 🔴 缩放比**别用渲染包围盒比** —— 旋转过的 AABB 会被撑大（实测给 0.96，真值 0.932）
                                    CheckNear(s1.transform.localScale.x / s0.transform.localScale.x, 232.954f / 250f, 0.005f,
                                              "槽 1 缩放 ÷ 前台缩放 = **232.954 / 250**（原版 clip 的 `localScale`）");
                                }

                                // ③ **外推的那几格**（槽 5–8）：只钉走向（更靠左 / 更斜 / 更小）——
                                //    原版没有第 6 格可比，⚠️ 这一段是**我们挑的**（正本 §8·7）。
                                //    ⚠️ 真实卡池里相关卡通常只有 1–2 张（全池 129 处点名摊在 100 来张卡上）
                                //    ⇒ **换一张相关卡够多的**再走这段：`Master of Arcana` 的天赋是
                                //    `Choose an Ultramarines Psychic Power…`（**池子 4 张**）⇒ 至少 5 格。
                                {
                                    CheckTrue(moa != null, "（前提）卡池里有 `Master of Arcana`");
                                    if (moa != null)
                                    {
                                        // 🔴 **2026-10-12（A437）换卡靠的是 `ShowCard` 里那句显式 `Build()`** ——
                                        //   A217② 把 `GameWindow.TryOpen` 按原版改成按 `CurrentState` 分三档之后，
                                        //   「同窗再开 ⇒ 重建内容」**没有了**（`Open` 那一支一个字段都不写就 return）
                                        //   ⇒ 这一句是**同窗换卡**唯一的重建入口。判据 = 原版 `CardDisplayWindow.ShowCard`
                                        //   复用同一个窗（`WindowsManager.OpenWindow(this)`）+ 窗自己那份刷新
                                        //   ⇒ 我们的等价物 = **窗实例不变 + 内容按新卡重建**（下面两条分开断）。
                                        var wBefore = cd.gameObject;      // 换卡前那一扇（下面断它**不换**）
                                        cd.ShowCard(moa);
                                        CheckTrue(cd.gameObject == wBefore,
                                                  "★ A437：`ShowCard(A)` 紧跟 `ShowCard(B)` ⇒ **窗对象不变**（复用同一扇；"
                                                + "原版 `WindowsManager.OpenWindow(this)` 就是这个意思）"
                                                + " —— 改坏法：把 `ShowCard` 改成「销毁旧窗 + 建一扇新的」⇒ 这条红");
                                        CheckTrue(cd.FrontDef != null && cd.FrontDef.Id == moa.Id,
                                                  "★ A437 …而且前台那张**真的换成 `Master of Arcana` 了**（现在的 `FrontDef` = "
                                                + (cd.FrontDef != null ? "`" + cd.FrontDef.Name + "`" : "(null)") + "）"
                                                + " —— 🔴 **这条最锋利**：它专打「以为复用会自动重建」的那个人。"
                                                + "改坏法：删掉 `Shell/CardDetailPopup.cs` 的 `ShowCard` 里那句显式 `Build()`"
                                                + "（「关过再开」那一支仍会因 `Closed` 支重建、**红不出来**）"
                                                + "⇒ **同窗换卡这一支**的 `FrontDef` 还是上一张 ⇒ 红");
                                        // ⚠️ 下面那条 `inPool`（相关卡有没有列出来）**单独不能当换卡的证据**：
                                        //   `Path of Command` 自己也是 Ultramarines 的卡，池子可能与它重叠
                                        //   ⇒ 换卡这件事由上面那条 `FrontDef` 断，`inPool` 管的是另一件事（相关卡来源②）。
                                        CheckTrue(cd.SlotCount >= 5,
                                                  $"`Master of Arcana`（天赋是个 **4 张的池子**）⇒ 卡片那一叠 {cd.SlotCount} 格");
                                        int inPool = 0;
                                        foreach (var c in CardDatabase.Load())
                                            if (c.Faction == "Ultramarines" && c.Subtype == "Psychic Power"
                                                && cd.SlotIndexOf(c.Id) > 0) inPool++;
                                        CheckTrue(inPool >= 3,
                                                  $"★ **池子里的卡真列进相关卡了**（{inPool} 张）—— "
                                                + "相关卡来源②：天赋是个池子 ⇒ 池里那几张跟出来（判据 → 正本 §九）");
                                        int pairs = 0;
                                        for (int i = 2; i < cd.SlotCount; i++)
                                        {
                                            var sa = cd.SlotView(i - 1); var sb = cd.SlotView(i);
                                            if (sa == null || sb == null) continue;
                                            pairs++;
                                            CheckTrue(LayoutSpace.ToPixel(sb.transform.position).x
                                                       < LayoutSpace.ToPixel(sa.transform.position).x - 10f,
                                                      $"槽 {i} 比槽 {i - 1} **更靠左**（扇形继续张开）");
                                            CheckTrue(sb.transform.localScale.x < sa.transform.localScale.x,
                                                      $"槽 {i} 比槽 {i - 1} **更小**（原版就是越远越小）");
                                            CheckTrue(sb.transform.eulerAngles.z > sa.transform.eulerAngles.z,
                                                      $"槽 {i} 比槽 {i - 1} **更斜**（原版就是越远越斜）");
                                        }
                                        CheckTrue(pairs >= 3, $"比得出至少 3 对相邻卡位（实得 {pairs} 对）");
                                        // 🔴 分层：**每格一个独立队列、越靠前台号越大** ——
                                        //    不然「谁盖谁」只剩「到相机的距离」在排，而那是**不可控**的
                                        //    （实测第一版：后面那张的**卡名画到了前面那张的立绘之上**）。
                                        {
                                            int prevQ = int.MaxValue; bool mono = true;
                                            for (int i = 0; i < cd.SlotCount; i++)
                                            {
                                                var sv = cd.SlotView(i);
                                                if (sv == null) continue;
                                                int q = -1;
                                                foreach (var mr in sv.GetComponentsInChildren<MeshRenderer>(true))
                                                    if (mr.sharedMaterial != null) { q = mr.sharedMaterial.renderQueue; break; }
                                                if (q >= prevQ) mono = false;
                                                prevQ = q;
                                            }
                                            CheckTrue(mono,
                                                      "★ **每格的渲染队列逐格递减**（前台最高）—— 分层靠队列，不靠距离"
                                                    + "（`资料/已知的坑.md`：同队列的两层谁盖谁不可控）");
                                        }
                                        cd.ShowCard(poc);        // 换回来 —— 下面第 ④ 段要在 `Path of Command` 这叠上验换位
                                    }
                                }

                                // ④ 换位：点相关卡 ⇒ 它和前台**两两互换**（原版 `ChangeCardPosition`）
                                if (sg > 0)
                                {
                                    var before0 = cd.FrontDef;
                                    var hit1 = FindChild(cd.transform, "CardHit " + sg);
                                    var hb1 = hit1 != null ? hit1.GetComponent<WindowButton>() : null;
                                    CheckTrue(hb1 != null, $"第 {sg} 格有点击区（原版 `AddCardsListeners`：5 个卡位各挂一个）");
                                    var posFront = cd.SlotView(0).transform.position;
                                    var posOther = cd.SlotView(sg).transform.position;
                                    // 🔴 批处理没有帧循环 ⇒ 补间要**手动推进**（同 `BattleScene` 那几处）
                                    CardTween.Mode = DG.Tweening.UpdateType.Manual;
                                    if (hb1 != null) hb1.Click();
                                    CheckTrue(cd.IsSwapping, "点了相关卡 ⇒ 换位在播（原版 `swappingCards` 闸置上）");
                                    // 播完之后再点一次 ⇒ **该被闸①挡掉**（原版：上一次没播完什么都不做）
                                    if (hb1 != null) hb1.Click();
                                    CheckTrue(cd.FrontDef == before0,
                                              "★ 换位播到一半再点 ⇒ **什么都不做**（原版闸① `swappingCards`）");
                                    CardTween.Advance(0.3f);          // 0.25s 那条 tween 走完
                                    CheckTrue(!cd.IsSwapping, "0.25s 之后换位收尾（开闸）");
                                    CheckTrue(cd.FrontDef != null && cd.FrontDef.Id == sgDef.Id,
                                              $"★ **被点那张换到了前台**（现在是「{cd.FrontDef.Name}」）—— 原版「点谁就把谁换到前面」");
                                    CheckNear(Vector3.Distance(cd.SlotView(0).transform.position, posFront), 0f, 0.01f,
                                              "★ 被点那张现在站在**原来的前台位**（两两互换，不是「把谁提到最前」）");
                                    CheckNear(Vector3.Distance(cd.SlotView(sg).transform.position, posOther), 0f, 0.01f,
                                              "★ 原来那张前台让到了**被点卡的槽位**（同上）");
                                    // 闸②：点前台自己 ⇒ 什么都不做（也别让这一下落到遮罩上把窗关掉）
                                    int hitFront = 0;
                                    var hf = FindChild(cd.transform, "CardHit " + hitFront);
                                    var hfb = hf != null ? hf.GetComponent<WindowButton>() : null;
                                    CheckTrue(hfb != null, "**前台那格也有点击区**（不然点它会落到遮罩上**把窗关掉**）");
                                    if (hfb != null) hfb.Click();
                                    CheckTrue(cd.CurrentState != WindowState.Closed,
                                              "★ 点前台那张 ⇒ **窗不关、也不换位**（原版闸②）");
                                    CheckTrue(cd.FrontDef.Id == sgDef.Id, "…而且前台还是刚换上去那张（没被点回去）");
                                    // 拍照前换回**格子最多**的那张（5 格）—— 截图是拿来**看扇形**的
                                    if (moa != null) cd.ShowCard(moa);
                                }
                            }
                        }
                        // ⑤ **着色 / 分层** —— 2026-09-28 按用户给的实拍（《点击卡片查看详情的参考.png》）
                        //    订正的两条（判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §十·2 / §十·5）
                        {
                            var s0 = cd.SlotView(0);
                            var sN = cd.SlotView(cd.SlotCount - 1);
                            CheckTrue(s0 != null, "（前提）前台那张在");
                            CheckNear(s0 != null ? s0.Tint.r : -1f, 1f, 0.01f,
                                      "★ 前台那张**不压暗**（原版前台色 = (1,1,1,1)）");
                            CheckTrue(cd.SlotCount < 2 || sN.Tint.r < 0.99f,
                                      "★ 相关卡**压暗**（原版 `cardInBackGroundColorTint` = 0.65）");
                            if (cd.SlotCount >= 2)
                                CheckNear(sN.Tint.r, 0.65f, 0.01f, "…而且是 **0.65**，不是我们随手取的");
                            // 🔴 遮罩必须在**卡格之下**：原来卡格 3009–3017、遮罩 3110 ⇒ **整叠卡被压暗一半**
                            var shadeNode = FindChild(cd.transform, "Menu Dark Background");
                            var shadeQuad = shadeNode != null ? shadeNode.GetComponentInChildren<ImageQuad>() : null;
                            int shadeQ = shadeQuad != null ? shadeQuad.RenderQueue : -1;
                            int cardQ = CardQueue(s0);
                            CheckTrue(shadeQ >= 0, $"（前提）遮罩在（队列 {shadeQ}）");
                            // 🆕 A47：压暗层命中区那条不变量 —— 档 = `QShade`(3105)（**压暗层自己那一档**），
                            //   严格低于内容命中区最低档 `QCdHit`(3118)。改前是 `QCdHit − 1` = 3117
                            //   （= `QCdText`，**文字那一档** ⇒ 落在别的层上）。
                            //   🔴 A77⑬③：期望值改用**上面已经量到的那颗视觉压暗层 quad**（`shadeNode`）——
                            //      本行**不再传 `CardDetailPopup.QShade`**（那与被测实参同源 = 同义反复）。
                            MenuDraw.CheckShadeRule(CheckTrue, "卡片详情窗", cd.ShadeHit, shadeNode,
                                                    CardDetailPopup.QCdHit);
                            var clHit = FindChild(cd.transform, "BackgroundHit");
                            CheckTrue(clHit != null && clHit.GetComponent<WindowButton>() != null,
                                      "…而且那块命中区带 `WindowButton`（`PointerLayer` 靠它派发点击）");
                            CheckTrue(cardQ > shadeQ,
                                      $"★ 卡格队列（{cardQ}）**高于遮罩**（{shadeQ}）—— 卡画在压暗层之上"
                                      + "（原版那棵树里 `Menu Dark Background` 是第一个孩子）");
                            // 风味底图：**按阵营**选图（2026-09-28 刚导进工程的那 13 张）
                            var loreBgNode = FindChild(cd.transform, "FlavourTextBG");
                            var loreImg = loreBgNode != null ? FindChild(loreBgNode, "Image") : null;
                            var loreQuad = loreImg != null ? loreImg.GetComponent<ImageQuad>() : null;
                            CheckTrue(loreQuad != null,
                                      "★ 风味底图建出来了（原版 `FlavourTextSO.GetClanFlavorBackground` 按阵营选）");
                            var frontDef = cd.FrontDef;
                            if (loreQuad != null && frontDef != null)
                                CheckTrue(loreQuad.Texture != null
                                          && loreQuad.Texture.name == "flavourbg_" + frontDef.Faction.ToLowerInvariant(),
                                          $"★ …而且取的是**这个阵营**那张：`{loreQuad.Texture.name}`"
                                          + $"（卡是 {frontDef.Faction}）—— 判据是原版那张 army→资产 表");
                        }
                        // ================================================================
                        //  🆕 A156（2026-10-07）：**点击区（`CardHit 0`）的双轴比例 + 偏置**
                        //  判据 = 解包原件字段（第一权威 · 现读现核）：
                        //    · 菜单树 `bundle_scenes_scenes_mainmenuwarpforge/RectTransform/RectTransform_1220.json`
                        //    · 战斗树 `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2801.json`
                        //    两颗 `UI Collider` **逐字段相同**：
                        //      `m_AnchorMin(0,0)` · `m_AnchorMax(1,1)`（**拉伸锚**）· `m_Pivot(0.5,0.5)`
                        //      · `m_AnchoredPosition(0, −0.02)` · `m_SizeDelta(−0.2, −0.44)`
                        //    父件 `2DCard`（`RectTransform_1323` / `RectTransform_2876`，两棵同为
                        //      `anchor(0.5,0.5)` · `ap(0,0)` · **`m_SizeDelta = 2.0927 × 3.3313`** 卡单位）
                        //    ⇒ 点击区 = (2.0927−0.2) × (3.3313−0.44) = **1.8927 × 2.8913** 卡单位，
                        //      中心比卡心**低 0.02 卡单位**（= 前台 5px）
                        //    ⇒ 前台（scale 250 · 卡心 (960,480) · 卡体 523.175 × 832.825）：
                        //      **四沿 = 723.412 / 123.587 / 1196.588 / 846.413**
                        //      = 左右各缩 **25**（= 0.2 卡单位 × 250）· **上缩 60 · 下缩 50**
                        //   ⛔ 期望值写**原版 prefab 复算出来的字面量**，⛔ **不写** `CardFan.HitRatioX/Y`
                        //      —— 那是被测实现传进去的实参（同式自证：改实现它照样绿）。
                        //  ⚠️ 改前是「**一个 0.9043 双轴同用 + 居中**」⇒ 上沿 103.438 / 下沿 856.562
                        //      （y 轴多吃上 20.149 / 下 10.149）—— 那两条窄带**在原版是会关窗的**。
                        {
                            var hitq0 = FindChild(cd.transform, "CardHit 0");
                            var hq0 = hitq0 != null ? hitq0.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(hq0 != null, "`CardHit 0`（前台那格的点击区）在");
                            if (hq0 != null)
                            {
                                float hx1, hy1, hx2, hy2;
                                QuadRectOf(hq0, out hx1, out hy1, out hx2, out hy2);
                                CheckNear(hx1, 723.412f, 1.5f,
                                          "★ 点击区**左沿** = **723.412**（原版 `UI Collider` `sd(−0.2,−0.44)` 复算"
                                        + " —— 左缩 25px = 0.2 卡单位 × 250）");
                                CheckNear(hy1, 123.587f, 1.5f,
                                          "★ …**上沿** = **123.587** ⇒ **上缩 60**（⚠️ 不是 50 —— 中心被"
                                        + " `m_AnchoredPosition.y = −0.02` 拉低了 5px；改前量到 **103.438**）");
                                CheckNear(hx2, 1196.588f, 1.5f, "…**右沿** = **1196.588**（右缩 25）");
                                CheckNear(hy2, 846.413f, 1.5f,
                                          "…**下沿** = **846.413** ⇒ **下缩 50**（改前量到 **856.562**）");
                            }
                        }
                        cd.PlayVoice();     // 有就播、没有就出声 —— 两种都接受（判据是它**不静默**）
                        Shoot("08_收藏_卡片详情窗.png");
                        var sh = cd.ShadeHit; var shb = sh != null ? sh.GetComponent<WindowButton>() : null;
                        if (shb != null) shb.Click();
                        Check(cd.CurrentState, WindowState.Closed, "点遮罩 ⇒ 窗关上（**原版全树没有关闭钮**，就这一条路 + ESC）");
                    }
                }
            }

            // ============================================================ Styles 页（异画，2026-09-24）
            //
            // 几何出处：`CollectionWindow.StyleView` 那段注释（**从 `Collection Menu Variant` 根走下来实读的**）。
            // 判据一律是**原版数**，不是我们自己的常量（否则是自证）。
            Section("Styles 页：换风格条 + 异画网格（6 列 · 262.5×384 · 本地 7 张 / 2 种风格）");
            win.tabButtons.Click(3);
            Check(win.CurrentTab, WindowTabType.CollectionStyles, "点第 4 键 ⇒ 切到 **Styles** 页");
            CheckTrue(win.PageRoot(3) != null, "页节点 `Alternate Art Tab` 在（**卡面文案是 `STYLES`**）");
            Check(CollectionWindow.AltArtCards.Length, 7,
                  "异画读数 = **7**（本地只有这 7 张督军异画；文件名 `AA_HB_…` / `…_v2`）");
            Check(CollectionWindow.AltStyles.Length, 2,
                  "风格数 = **2**（`AA_HB` 6 张 + `v2` 1 张；其余风格在远端 CCD 的 `alternateartstyles` 包）");
            Check(CollectionWindow.StyleCols, 6,
                  "列数 = **6** = floor(1589.78 ÷ 262.5)（⚠️ 不是 `_segments=4` —— 那是死值）");
            var spage = win.PageRoot(3);
            var svp = spage != null ? FindChild(spage, "Scroll View") : null;
            CheckAt(svp, 330.22f, 1920.01f, 287.67f, 1080f, "`Scroll View`（异画视口 **1589.78 × 792.33**）");
            // 两个换风格圆钮：**按位置**（脚本字段名反着：`leftStyleButton` 挂的是右边那颗）
            var aL = spage != null ? FindChild(spage, "Select Art Button Left") : null;
            var aR = spage != null ? FindChild(spage, "Select Art Button Right") : null;
            CheckNear(aL != null ? PxOf(aL.position.x) : -1f, 720.595f, 1f,
                      "`Select Art Button Left` 中心 x = **720.60**（683.40 + 74.39/2）");
            CheckNear(aR != null ? PxOf(aR.position.x) : -1f, 1357.59f, 1f,
                      "`Select Art Button Right` 中心 x = **1357.59**（1320.40 + 74.39/2）");
            CheckTrue(aL != null && aR != null && Mathf.Abs(PxYOf(aL.position.y) - 228.47f) <= 1f,
                      "两个圆钮中心 y = **228.47**（190.67 + 75.61/2）");
            // 🔴 **箭头那一层必须比黄底高一级队列** —— 两层摆在同一个矩形上，同队列时「谁盖谁不可控」
            //    （2026-09-24 实拍：箭头**整个没出现**、只看得见黄底，而矩形断言全绿）。
            //    判据照坑表那条：**比 `RenderQueue`，不比 z**。
            {
                var lIcon = aL != null ? aL.Find("Icon") : null;
                var lBg = aL != null ? aL.Find("Background") : null;
                var iq = lIcon != null ? lIcon.GetComponentInChildren<ImageQuad>() : null;
                var bq2 = lBg != null ? lBg.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(iq != null && iq.Texture != null, "左箭钮的箭头图**真的有贴图**（`40k_general_bt_arrow`）");
                CheckTrue(iq != null && bq2 != null && iq.RenderQueue > bq2.RenderQueue,
                          $"箭头那层队列 **{(iq != null ? iq.RenderQueue : -1)}** > 黄底那层 **{(bq2 != null ? bq2.RenderQueue : -1)}**"
                          + "（同队列时箭头会被盖掉 —— 实测过一次）");
            }
            // `Art Style Logo` 那一格：原版 `sprite=0`（运行时喂风格图 SO，**本地没有**）⇒ 我们画风格名（**我们挑的**）
            CheckText(TextOf(FindChild(spage, "Art Style Logo")), "Hammer and Bolter",
                      "风格名 = **Hammer and Bolter**（`AA_HB` 的显示名，出处 `解包资源使用地图.md:1174`）"
                      + " —— ⚠️ **原版这格是图不是字**，我们这里是**我们挑的做法**");
            // 🔴 **量渲染宽度**（不是比字号）：那一格是 **512×128**，56px 的 `Hammer and Bolter`
            //    实测宽 ≈1270px ⇒ **会压到右箭钮上**（第一版实拍一眼可见）。判据照 `AutoFitBox` 那条教训。
            CheckTrue(win.StyleLogoWidthPx <= 512f + 1f,
                      $"`Art Style Logo` 那行字的**渲染宽度 {win.StyleLogoWidthPx:F0}px ≤ 512**"
                      + "（超出就会压到右边那颗换风格钮上 —— 这条**矩形断言量不到**，得量 `Label.WorldW`）");
            // 网格：7 张里当前风格 6 张 ⇒ 2 行；首格中心
            Check(win.StyleVisibleCount, 6, "当前风格（`AA_HB`）下可见 **6** 张异画");
            Check(win.StyleCells.Count, 6, $"画出了 {win.StyleCells.Count} 格（视口外的不建 = 那套裁切）");
            if (win.StyleCells.Count > 0)
            {
                var s0 = win.StyleCells[0];
                // 内容**整体居中**：pad = (1589.78 − 6×262.5) / 2 = **7.39** ⇒ 首格中心 330.22+7.39+131.25
                CheckNear(PxOf(s0.position.x), 468.86f, 0.7f, "第 1 格中心 x = **468.86**（330.22 + pad 7.39 + 262.5/2）");
                CheckNear(PxYOf(s0.position.y), 479.67f, 0.7f, "第 1 格中心 y = **479.67**（287.67 + 384/2）");
                // 🔴 判「卡面用的是**异画**立绘」—— 扫这一格**所有** `MeshRenderer` 的材质贴图名字。
                //    ⚠️ **别只看第一个**：第一版取「第一个有贴图的」拿到的是**卡框**
                //    （`astramilitarum_tier3`），断言因此误报。`CardView` 的层走的是**自定义 mesh**
                //    （不是 `ImageQuad`）⇒ 只能按 `MeshRenderer.sharedMaterial.mainTexture` 判。
                int meshCount = 0; string altTex = null;
                foreach (var mr in s0.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.sharedMaterial == null || mr.sharedMaterial.mainTexture == null) continue;
                    meshCount++;
                    if (mr.sharedMaterial.mainTexture.name.StartsWith("alt_") && altTex == null)
                        altTex = mr.sharedMaterial.mainTexture.name;
                }
                CheckTrue(meshCount > 0, $"第 1 格里画出了 `CardView` 的 {meshCount} 层网格");
                CheckTrue(altTex != null,
                          $"格里的立绘贴图 = **{altTex ?? "(一个 alt_* 都没有)"}**（**必须出现 `alt_*`** —— "
                          + "异画页画的就是它；一个都没有就说明 `CardData.artOverride` 没接上、退回了普通立绘）");
            }
            // 左抽屉：**起手收起**。
            // ⚠️ **2026-10-05 更正（铁律 5）**：原文写「出厂展开（实证 act=T —— ⚠️ 与 Cosmetics 页相反）」
            //   并断 `StyleFiltersOpen == true` —— **字段读数（`act=T`）对、推论错**：
            //   `act=T` 只等于「节点启用」，**推不出**「抽屉停在哪一头」（同 prefab 的卡组编辑窗那份
            //   `Card Filters` 也是 `act=T`，而它早已独立证实起手收起）。
            //   判据（VA 反汇编 —— `decomp_full` 里这几个**泛型方法体确实没有**）：
            //     `CollectionTab<object>$$Setup`（`0x1815F3C00`）尾调用 `display.Initialize(GetCollection())`
            //     → `CollectionDisplay<object>$$Initialize`（`0x1815EC0A0`）挂 `filterToggle.onValueChanged`
            //       并调 `filters.SetupFilters()`（`0x1815EC278`）
            //     → `SetupFilters`（`0x1815F0740`）收尾 `anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`
            //   ⇒ **每个页签一建出来就停在「收起」那一头**，之后没有任何一处起手把它打开
            //   （唯一的开启者 = 页头那颗 toggle 的 `onValueChanged`）。旁证：四颗 `Filter Toggle`
            //   的 `m_IsOn` 全是 0、出厂画的是 `offSprite`。
            //   逐字段实读 = `资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 件 B。
            // 🔴 两条**能区分两种状态**的断言（⛔ 不是同义反复、也不是一条恒假）：
            //    ① 断**节点真收着**那一头 —— 量的是**可见性**（`activeSelf`，= `ApplyDrawerSlide` 里
            //       那句「滑出去了才关」的结果）、**位置**进度 `DrawerSlide`、**命中区** `DrawerSettled`，
            //       ⛔ **不是**那个逻辑态 bool 自己；
            //    ② 真去**点一次页头那颗钮**（`FiltersHit` 上的 `WindowButton.Click()`，与 `PointerLayer`
            //       派发的是**同一个** `Click()`）⇒ **才开**。
            //   少了 ②，「起手收起」可以被「一直收着、点了也不开」蒙过去；少了 ①，② 也证明不了起手态。
            var styleDrawer = spage != null ? FindChild(spage, "Card Filters") : null;
            CheckTrue(styleDrawer != null, "（前提）异画页左抽屉节点 `Card Filters` 建出来了 —— 下面几条都靠它");
            CheckTrue(!win.StyleFiltersOpen, "异画页左抽屉**逻辑态起手收起**（`FilterPanel.Open == false`）");
            CheckTrue(styleDrawer != null && !styleDrawer.gameObject.activeSelf,
                      "…而且**节点真的收着**（`activeSelf == false`）—— 起手整栏在屏幕左外（`hiddenPosition.x`）");
            CheckNear(win.DrawerSlide(1), 0f, 0.001f,
                      "…位置进度 = **0**（0 = 已滑出到 `hiddenPosition`；1 = 回到原位）");
            CheckTrue(!win.DrawerSettled(1), "…没到位 ⇒ 这一栏的命中区/滚轮都不生效");
            var styleFltHit = spage != null ? FindChild(spage, "FiltersHit") : null;
            var styleFltBtn = styleFltHit != null ? styleFltHit.GetComponent<WindowButton>() : null;
            CheckTrue(styleFltBtn != null, "页头那颗 `Filter Toggle` 的命中区 `FiltersHit` 在（点它才开）");
            if (styleFltBtn != null)
            {
                styleFltBtn.Click();        // 🔴 **真点一次**（不是直调 `win.ToggleStyleFilters()`）
                CheckTrue(win.StyleFiltersOpen && styleDrawer != null && styleDrawer.gameObject.activeSelf,
                          "点一下页头 ⇒ 抽屉**才开**：逻辑态 true **且**节点 `activeSelf == true`");
                CheckNear(win.DrawerSlide(1), 1f, 0.001f,
                          "…而且真滑回原位：进度 **1**（批处理里 `Toggle*` 直接到位，见 `StartDrawerSlide`）");
                CheckTrue(win.DrawerSettled(1), "…到位 ⇒ 命中区/滚轮恢复");

                // ---------------- 🆕 2026-10-11（A248 · A249）：异画页抽屉的**字号那一档**（与卡牌页不同）
                //
                // 🔴 **「一个值 ≠ 全部情况」（铁律 5·c）**：同一个 `FilterPanelModel`、**同一套矩形**，
                //   而**异画页**是**另一套字号**。判据 = **现读的 dump 原始行**（可复跑）：
                //   `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 22 --no-sprite`
                //     · 异画页 `Name FIlter/…/Text Area/{Placeholder,Text}`   = **35 · `auto[10~35]`**
                //     · 异画页 `Owned Toggle/Label` 与 `Upgradable only/Label` = **36 · `auto[10~36]`**
                //     · 异画页四个 `Title`（Army/Rarity/Energy Cost/Type）     = **36**（**无 `auto[…]`** ⇒ 不开自适应）
                //   对照（同一份 dump）：**卡牌页**那一份 = `30 · auto[18~30]` / `32 · auto[18~32]` / `32`。
                //   稀有度 / 费用 / 类型三族**两页相同**（23.2 auto[10~27] / 45 auto[25~45]）⇒ 不走这一档。
                //
                // ⛔ **期望值一律写上面的原版字面量**，⛔ 不从 `FilterPanelModel.*Styles` 常量读
                //    （那是被测实现**传进去的实参** ⇒ 同式自证）；读的是 **TMP 自己那两个真字段**
                //    （`fontSizeMin/Max`，经工程唯一那份 `Label.FontSizeToPx` 折回画布 px —— 与 A247/A270 同一口径）。
                // 🔴 **改坏哪里它会红**：把 `BuildFilterPanel(…, styles: true)` 那个实参删掉 / `_flt.Styles` 忘了往
                //    下传 ⇒ 异画页那 6 条全红（退回 30/18、32/18、32）；把**共用常量**改成 35/36 ⇒ 下面的
                //    **卡牌页对照组**红（且卡牌页会一起被改歪）。
                {
                    var sNF = FindChild(styleDrawer, "Name Filter");
                    var sInT = sNF != null ? FindChild(sNF, "Input Text") : null;
                    var sInLb = sInT != null ? sInT.GetComponent<Label>() : null;
                    CheckTrue(sInLb != null, "（前提）异画页搜索框的 `Input Text` 建出来了");
                    if (sInLb != null)
                    {
                        CheckNear(Label.FontSizeToPx(sInLb.FontSizeMax), 35f, 0.6f,
                                  "★ A248：异画页搜索框字号 = 原版 **35**（卡牌页是 30）");
                        CheckNear(Label.FontSizeToPx(sInLb.FontSizeMin), 10f, 0.6f,
                                  "…自适应下界 = 原版 **10**（卡牌页是 18）—— 只改上界不改下界 ⇒ 这条红");
                        // 🔴 **A249** 与字号无关、**两页同档**：`Text` 那一半原版就是 `折行=3`
                        //    （`Placeholder` 是 0，我们这颗 `Label` 兼作两者 ⇒ 取 `3`，同 `DeckEditInputWrap` 那条先例）。
                        //    改坏法：删掉 `TextAligned` 里那句 `SetWrappingMode` ⇒ 退回 `SetAutoFitBox` 开的 1 ⇒ 红。
                        Check(sInLb.WrappingMode, 3,
                              "★ A249：异画页搜索框 **`折行=3`**（`PreserveWhitespaceNoWrap`，TMP 给单行输入框那一档）"
                              + "—— `SetAutoFitBox` 会无条件开成 1（`Normal`）⇒ 不显式设就退回 1");
                    }
                    var sOwT = FindChild(styleDrawer, "Cell_owned");
                    var sOwL = sOwT != null ? FindChild(sOwT, "Label") : null;
                    var sOwLb = sOwL != null ? sOwL.GetComponent<Label>() : null;
                    CheckTrue(sOwLb != null, "（前提）异画页 `Owned only` 那一格的标签在");
                    if (sOwLb != null)
                    {
                        CheckNear(Label.FontSizeToPx(sOwLb.FontSizeMax), 36f, 0.6f,
                                  "★ A248：异画页 `Owned only` 字号 = 原版 **36**（卡牌页 32）");
                        CheckNear(Label.FontSizeToPx(sOwLb.FontSizeMin), 10f, 0.6f,
                                  "…自适应下界 = 原版 **10**（卡牌页 18）");
                        Check(sOwLb.WrappingMode, 0,
                              "…而折行**两页同档**（原版 `'Owned only' 折行=0`）—— 按页分的是**字号**、不是折行；"
                              + "把 `LabelWrap` 也按页分 ⇒ 这条红");
                    }
                    string[] sTitleNames = { "Title Army", "Title Rarity", "Title Energy Cost", "Title Type" };
                    int sTitleSeen = 0;
                    for (int ti = 0; ti < sTitleNames.Length; ti++)
                    {
                        var tt = FindChild(styleDrawer, sTitleNames[ti]);
                        var tlb = tt != null ? tt.GetComponent<Label>() : null;
                        if (tlb == null)
                        {
                            // ⚠️ **正常**：四行是按视口建的（`TitleRow` 先 `Intersects` 再建）——
                            //   `Type` 那一行在 y 1155.9 而视口到 1079.99 ⇒ 这一档**本来就该没有节点**。
                            //   ⛔ 别把这条写成 `CheckTrue(false,…)`（那会把「按视口建」这件对的事判成红）。
                            Debug.Log(P + $"  （异画页小标题 `{sTitleNames[ti]}` 这次在视口外 ⇒ 不建节点，跳过）");
                            continue;
                        }
                        sTitleSeen++;
                        CheckNear(tlb.FontPxNow, 36f, 0.6f,
                                  "★ A248：异画页 `" + sTitleNames[ti] + "` 字号 = 原版 **36**（卡牌页 32）"
                                  + "（原版这四行**不开自适应**、也没有 `auto[…]` ⇒ 这一档落在标称字号上）");
                    }
                    CheckTrue(sTitleSeen >= 3,
                              $"★ A248：异画页真的建出了 **≥3** 行小标题（实得 {sTitleSeen}）——"
                              + "只建出 1~2 行 ⇒ 上面那一组等于没验");

                    // ---- 🧪 **对照组：卡牌页必须还是原版那一套**（30 / 18 · 32 / 18 · 32）----
                    //   ⛔ 这是「按页分参数」这条裁定的**另一半**：只把异画页改对、顺手把共用常量也改掉 ⇒ 卡牌页被改歪。
                    //   卡牌页抽屉此刻是**收着**的，但节点都在（`FindChild` 走 `GetComponentsInChildren(true)`）。
                    var cDrawer = win.PageRoot(1) != null ? FindChild(win.PageRoot(1), "Card Filters") : null;
                    CheckTrue(cDrawer != null, "（前提）卡牌页的 `Card Filters` 节点在（对照组靠它）");
                    if (cDrawer != null)
                    {
                        var cNF = FindChild(cDrawer, "Name Filter");
                        var cInT = cNF != null ? FindChild(cNF, "Input Text") : null;
                        var cInLb = cInT != null ? cInT.GetComponent<Label>() : null;
                        if (cInLb != null)
                        {
                            CheckNear(Label.FontSizeToPx(cInLb.FontSizeMax), 30f, 0.6f,
                                      "★ **对照**：卡牌页搜索框仍是原版 **30**（把共用常量改成 35 ⇒ 这条红）");
                            CheckNear(Label.FontSizeToPx(cInLb.FontSizeMin), 18f, 0.6f,
                                      "★ …下界仍是原版 **18**");
                            Check(cInLb.WrappingMode, 3,
                                  "★ **对照**：卡牌页搜索框**也是** `折行=3`（原版两页同档 —— 这条钉「折行没被按页分」）");
                        }
                        else CheckTrue(false, "（前提）卡牌页搜索框的 `Input Text` 在");
                        var cOwT = FindChild(cDrawer, "Cell_owned");
                        var cOwL = cOwT != null ? FindChild(cOwT, "Label") : null;
                        var cOwLb = cOwL != null ? cOwL.GetComponent<Label>() : null;
                        if (cOwLb != null)
                        {
                            CheckNear(Label.FontSizeToPx(cOwLb.FontSizeMax), 32f, 0.6f,
                                      "★ **对照**：卡牌页 `Owned only` 仍是原版 **32**（改成 36 ⇒ 这条红）");
                            CheckNear(Label.FontSizeToPx(cOwLb.FontSizeMin), 18f, 0.6f,
                                      "★ …下界仍是原版 **18**");
                        }
                        else CheckTrue(false, "（前提）卡牌页 `Owned only` 那一格的标签在");
                        var cTt = FindChild(cDrawer, "Title Army");
                        var cTlb = cTt != null ? cTt.GetComponent<Label>() : null;
                        if (cTlb != null)
                            CheckNear(cTlb.FontPxNow, 32f, 0.6f,
                                      "★ **对照**：卡牌页小标题仍是原版 **32**（改成 36 ⇒ 这条红）");
                        else CheckTrue(false, "（前提）卡牌页小标题 `Title Army` 在");
                    }
                }
                styleFltBtn.Click();        // 关回去 —— 本节自己开自己关，不给后面的段留状态
                CheckTrue(!win.StyleFiltersOpen && styleDrawer != null && !styleDrawer.gameObject.activeSelf,
                          "再点一下 ⇒ 收回**收起**态（本节收尾 = 与进本节时同一个状态）");
            }
            Check(win.StyleVisibleCount, 6, "抽屉开合都不影响卡数（起手没有筛选条件）");
            // 换风格：右箭钮 = 下一个（`v2` 只有 1 张）
            var rhit = FindChild(spage, "ArrowHit Right");
            var rbtn = rhit != null ? rhit.GetComponent<WindowButton>() : null;
            CheckTrue(rbtn != null, "右箭钮有点击区");
            if (rbtn != null)
            {
                rbtn.Click();
                Check(win.StyleIndex, 1, "点右箭 ⇒ 风格下标 = **1**（切到 `v2`）");
                CheckText(TextOf(FindChild(spage, "Art Style Logo")), "v2",
                          "风格名跟着变（`v2` 的**显示名查不到** ⇒ 直接印 token，如实记）");
                Check(win.StyleVisibleCount, 1, "`v2` 风格下只有 **1** 张异画（Azrael —— 本地就这么一张）");
                rbtn.Click();
                Check(win.StyleIndex, 0, "再点一下（`v2` 只有一格）⇒ **回绕**到第 0 种");
            }
            var lhit = spage != null ? FindChild(spage, "ArrowHit Left") : null;
            var lbtn = lhit != null ? lhit.GetComponent<WindowButton>() : null;
            if (lbtn != null)
            {
                lbtn.Click();
                Check(win.StyleIndex, 1, "点**左**箭 ⇒ 往回一个（`(0−1+2)%2` = 1）");
                lbtn.Click();
                Check(win.StyleIndex, 0, "再点左箭 ⇒ 回到 0");
            }
            // 筛选**真的筛得动**（复用同一套 `DeckEditorState`：筛 `legendary` 只剩传奇那几张）
            {
                int altBefore = win.StyleVisibleCount;
                win.ApplyStyleFilter("$rar:legendary");
                CheckTrue(win.StyleVisibleCount < altBefore,
                          $"筛 `Legendary` ⇒ 异画从 {altBefore} 张降到 **{win.StyleVisibleCount}** 张（真筛得动）");
                win.ClearStyleFilters();
                Check(win.StyleVisibleCount, altBefore, $"`Clear filters` ⇒ 回到 {altBefore} 张");
            }
            Shoot("07_收藏_Styles.png");
            win.tabButtons.Click(0);
            Debug.Log(P + "   " + win.Dump());
            SaveScene();

            // ---------------- 页头那颗 `Filter Toggle`：四页各一颗，按本页抽屉的逻辑态换图（A93①）----------------
            // 判据（本轮**现读**原版，两处独立）：
            //   ① `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12`
            //      —— 四页页头**各有一颗** `Filter Toggle`（`Image,EverguildToggle,EverguildButtonMaterialModifier`
            //      · `40k_menu_bt` · `isOn=0` · `onSprite=4570862220269996290` · `offSprite=4472012397149938974`），
            //      含 **Deck 页**（`Select Deck Tab>Header>Filter Toggle` = 367.2,88.4 **50×50**，
            //      与另外三页同值）—— 我们原来给 Deck 页单写的那条页头**连这颗 quad 都没建**。
            //   ② 两个 pid 解名（`d:/4/_tmp_view/sprite_pids_ALL.json`，由 UnityPy 扫真包 `o.path_id` 建的）：
            //      `4570862220269996290 → 40k_menu_bt_pressed` · `4472012397149938974 → 40k_menu_bt`。
            // ⚠️ 断言量的是**渲出来的图名**（`CheckArt` 读 `ImageQuad.Texture.name`），
            //    ⛔ 不是「有没有调换图那个方法」——后者是同义反复。
            // 🔴 改坏会红的样子：把 `CollectionWindow.RefreshFilterToggle` 里 `p.Open ? …pressed : …` 那一句
            //    写死成 `40k_menu_bt`（或三条开合路径里那句 `RefreshFilterToggles()` 删掉）⇒ 每页的
            //    「展开 ⇒ 按下图」那条红；把 Deck 页退回原来那条自写页头 ⇒ 第 0 页两条**都**红（节点不在）。
            Section("页头 `Filter Toggle`：**四页各一颗**，展开 ⇒ `40k_menu_bt_pressed` / 收起 ⇒ `40k_menu_bt`（A93①）");
            {
                var tog = new System.Action[]
                {
                    win.ToggleDeckFilters, win.ToggleFilters, win.ToggleCosmoFilters, win.ToggleStyleFilters,
                };
                var nowOpen = new System.Func<bool>[]
                {
                    () => win.DeckFiltersOpen, () => win.FiltersOpen,
                    () => win.CosmoFiltersOpen, () => win.StyleFiltersOpen,
                };
                string[] pgTag = { "Deck（`Select Deck Tab`）", "Cards", "Cosmetics", "Styles" };

                // 先把四页各自的开合态记下来（本段结束时**原样还回去**，别把状态漏给后面的段）
                var wasOpen = new bool[4];
                for (int p = 0; p < 4; p++) wasOpen[p] = nowOpen[p]();

                for (int p = 0; p < 4; p++)
                {
                    // 🔴 **必须先把这一页切过去**：`CheckArt` 走的是 `GetComponentInChildren<ImageQuad>()`
                    //   （**不含未激活**），而四个页节点只有当前那一页是 `activeSelf` 的
                    //   ⇒ 不切页的话另外三页会「取不到 quad」而**假红**（报的是「实测 null」）。
                    //   这也正是原版的语义：每页页头那颗粒只在自己那一页上看得见。
                    win.tabButtons.Click(p);
                    CheckTrue(win.PageRoot(p) != null && win.PageRoot(p).gameObject.activeSelf,
                              $"（前提）第 {p} 页（{pgTag[p]}）已经切过去了");
                    // 归一：先确保这一页是【收起】的（⚠️ **2026-10-05 更正（铁律 5）**：原文写
                    // 「Deck/Cards/Cosmetics 起手本来就关；Styles 出厂展开」—— 后半句是把 `act=T`
                    // 读成了结论；**四页起手全是收着的**，判据见 Styles 段那一段更正）
                    // 这一句留着**不是**为 Styles 那半句：它是本段「进任何一页都从收起态开量」的前置，
                    // 对将来任何一页改成起手展开照样成立。
                    if (nowOpen[p]()) tog[p]();
                    CheckTrue(!nowOpen[p](), $"（前提）第 {p} 页（{pgTag[p]}）的抽屉此刻是**收起**的");
                    CheckArt(FindChild(win.PageRoot(p), "Filters Button"), "40k_menu_bt",
                             $"第 {p} 页（{pgTag[p]}）页头那颗 `40k_menu_bt` **建出来了**，收起态 = 常态图"
                             + "（原版 `EverguildToggle.offSprite`；Deck 页原来**连这颗都没建**，会红在这里）");
                    tog[p]();                                     // 开
                    CheckTrue(nowOpen[p](), $"（前提）第 {p} 页点一下 ⇒ 抽屉开着");
                    CheckArt(FindChild(win.PageRoot(p), "Filters Button"), "40k_menu_bt_pressed",
                             $"第 {p} 页（{pgTag[p]}）抽屉**开着** ⇒ 换成 `40k_menu_bt_pressed`"
                             + "（原版 `onSprite`；原来开着也恒画 `40k_menu_bt`）");
                    tog[p]();                                     // 关
                    CheckArt(FindChild(win.PageRoot(p), "Filters Button"), "40k_menu_bt",
                             $"第 {p} 页再点一下 ⇒ 换回 `40k_menu_bt`（判据**可逆**，不是单向的）");
                }
                win.tabButtons.Click(0);                          // 页签还原（本节进来时就在 Deck 页）
                // 还原：谁进来时是开的，就把它开回去
                for (int p = 0; p < 4; p++)
                {
                    if (wasOpen[p] != nowOpen[p]()) tog[p]();
                    CheckTrue(wasOpen[p] == nowOpen[p](),
                              $"（收尾）第 {p} 页（{pgTag[p]}）的开合态还原成进来时的样子（{wasOpen[p]}）");
                }
            }

            // ---- 卡片详情窗 · 「创建副本」/「升级」两块面板**都不建**（用户 2026-09-27 拍板）----
            // 🔴 用户原话：「直接全部卡都是最高级别的卡框，这样就不用升级了。也不需要合成卡牌了。」
            //   · **升级**：`CardArt.TierOf` 已改成**所有卡一律最高档** ⇒ 没有可升的（而且我们这侧升级本就**不改卡面**）；
            //   · **合成**：本作全解锁（资源 9999、卡池全开、`Owned` 直接给足）⇒ 没有要合的。
            //   ⚠️ 上一轮还在这里验过「四档稀有度 → 四张万能卡图标」—— 面板停掉后那条判据**没有对象了**；
            //     `CardDetailPopup.Craftable` / `WildcardIconFor` **保留不删**，恢复那块面板时直接用。
            Section("卡片详情窗 · 「创建副本」/「升级」都不建");
            foreach (var want in new[] { "common", "rare", "epic", "legendary", "special" })
            {
                CardDef cd = null;
                var pool = CollectionWindow.CardsState.Pool;
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i].Rarity == want) { cd = pool[i]; break; }
                if (cd == null) { Check(true, false, $"卡池里找不到稀有度 `{want}` 的卡"); continue; }
                var dw = win.OpenCardDetail(cd);
                if (dw == null) { Check(true, false, $"打不开卡片详情窗（{cd.Name}）"); continue; }
                CheckTrue(FindChild(dw.transform, "Craft Icon") == null,
                          $"★ `{want}` 的卡**没有**「创建副本」那一格（本作不做合成）—— 拿 `{cd.Name}` 试的");
                CheckTrue(FindChild(dw.transform, "Upgrade Title") == null,
                          $"★ `{want}` 的卡**没有**「升级」那一格（本作不做升级）—— 拿 `{cd.Name}` 试的");
                // 🆕 A17：卡片详情窗的两颗圆钮（语音 / 显示卡面文字）
                if (want == "common") CheckHoverSwap(dw.transform, "卡片详情窗");
                dw.Close();
            }

            // 🆕 A17：本窗的换图按钮（四页共用的 `Clear filters` / `Import` / `Create` / 换风格箭头 / 关闭钮「Back」）
            CheckHoverSwap(win.transform, "收藏窗");
            CheckNoMissingSwapArt("收藏窗这条链");

            // ============================================================ 🆕 2026-10-04：软边接线（三处）
            //
            // 🔴 **这一节量的是「接线」，不是机制** —— 机制（按带的内沿切开 + 逐角 alpha 斜坡）的逐条判据
            //    在 `Editor/RewardsScene.cs` §三·b4-d-2/d-4（对的是原版剖面手算值）。这里补的是另一半：
            //    **原版逐处不同的 `m_Softness` 真的被喂进那几处 `Clip` 了吗**（铁律 5·c：四处四个值）。
            // 判据（逐条实读，全量表 `d:/4/_tmp_view/q1_rm2d.txt`）：
            //   · 档案窗 `Avatar Tab/Item Display Panel/Scroll Rect` = **(0,50)**（:221-222）
            //   · 档案窗 `Title Tab/Item Display Panel/Scroll Rect` = **(0,50)**（:265-266）
            //   · 商店三页 `…/Packs Scroll View/Viewport` = **(0,25)**（`:177-178` / `:295-296` / `:59-60`）
            // 期望的**切线位置**全部由「原版值 + 原版视口矩形」现算（⛔ 不从被测实现里读常量）：
            //   带的内沿 = 视口该边的坐标 ± `m_Softness` 的那个分量。
            // ⚠️ 这两扇窗是**现场建的**（`PlayerProfileWindow` 是 Popup、`ShopWindow` 是 Fullscreen）
            //    —— 放在 `Run` 的**最后**，免得动到前面那些断言的现场。
            Section("软边接线（原版 `RectMask2D.m_Softness`）：档案窗 Avatar / Title 两页 + 商店");
            {
                // ---------------- ① 档案窗 `Avatar Tab`：(0,50) ----------------
                var pp = PlayerProfileWindow.Create(win.Manager);
                win.Manager.OpenWindow(pp);
                CheckTrue(pp.CurrentState == WindowState.Open, "`Player Profile Window` 开起来了（下面量它的两个页）");
                pp.tabButtons.Click(1);
                Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点第 2 个键 ⇒ 切到 `Avatar` 页");

                var avPage = FindChild(pp.transform, "Avatar Tab");
                var avGrid = FindChild(avPage, "Item Drawer");
                CheckTrue(avGrid != null, "`Avatar Tab/Item Display Panel/Scroll Rect/Item Drawer` 在");
                // 视口 = `AvatarTab` 的 `Scroll Rect`：654.16,210.69 → 1680.12,855.46（原版实测）
                // ⇒ 带内沿：上 210.69 + 50 = **260.69**、下 855.46 − 50 = **805.46**
                // 第 1 行的格（y 250.70..430.70）与第 3 行的格（710.70..890.70）各压在一条带上 ⇒ 两条都该出现
                CheckSoftCuts(ScanSoftCuts(avGrid, false, new PxRect(654.16f, 210.69f, 1680.12f, 855.46f)), new[] { 260.69f, 805.46f }, 0.6f,
                              "`Avatar Tab` 的格子（原版 `m_Softness = (0,50)`）");
                Check(ScanSoftCuts(avGrid, true, new PxRect(654.16f, 210.69f, 1680.12f, 855.46f)).Count, 0,
                      "`Avatar Tab` **一条竖切线都没有** —— 这一处只渐变上下（`(50,0)` 那种写反的实现这里会冒横竖两种）");

                // ---------------- ② 档案窗 `Title Tab`：(0,50)（同一个窗口的另一页）----------------
                pp.tabButtons.Click(2);
                Check(pp.CurrentTab, WindowTabType.ProfileTitle, "点第 3 个键 ⇒ 切到 `Title` 页");
                // ⚠️ 这一页的格子里**没有吃 `Clip` 的图件**：底板走 `ProfilePage.Solid`，而那个口子**不收 `Clip`**
                //    （`PlayerProfileWindow.cs` 的 `Solid` —— 与 `Rect/Nine/Text` 不同，**这是一条真缺口**，已写进报告）。
                //    ⇒ 这一处的可观测面是**压在带里的文字**：`MenuDraw.ClipText` 对带内顶点按同一剖面削 alpha。
                var ttGrid = FindChild(FindChild(pp.transform, "Title Tab"), "Item Drawer");
                var ttLb = FindChild(FindChild(ttGrid, "TitleDrawer_9"), "Name");
                var ttTmp = ttLb != null ? ttLb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
                CheckTrue(ttTmp != null,
                          "第 4 行第 1 格（`TitleDrawer_9`：y 790.70..920.70，**压着视口底 855.46**）的名字有 TMP 网格");
                if (ttTmp != null)
                {
                    var ti = ttTmp.textInfo;
                    int nV = 0, nBand = 0, nBad = 0, nBelow = 0, nBadBelow = 0;
                    float minA = 255f, worst = 0f;
                    if (ti != null && ti.characterInfo != null && ti.meshInfo != null)
                    {
                        int cn = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
                        for (int ci = 0; ci < cn; ci++)
                        {
                            var ch = ti.characterInfo[ci];
                            if (!ch.isVisible) continue;
                            int mi = ch.materialReferenceIndex;
                            if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                            var mesh = ti.meshInfo[mi];
                            if (mesh.vertices == null || mesh.colors32 == null) continue;
                            for (int k = 0; k < 4; k++)
                            {
                                int v = ch.vertexIndex + k;
                                if (v < 0 || v >= mesh.vertices.Length || v >= mesh.colors32.Length) continue;
                                float y = LayoutSpace.ToPixel(ttTmp.transform.TransformPoint(mesh.vertices[v])).y;
                                int a = mesh.colors32[v].a;
                                nV++;
                                minA = Mathf.Min(minA, a);
                                if (y > 805.46f)          // 带内：alpha 必须 = 255 × (视口底 855.46 − y) ÷ 50
                                {
                                    nBand++;
                                    float want = Mathf.Clamp01((855.46f - y) / 50f) * 255f;
                                    float err = Mathf.Abs(a - want);
                                    worst = Mathf.Max(worst, err);
                                    if (err > 3f) nBad++;
                                }
                                else                      // 带外：**一个顶点都不该被动**（硬裁那半句照旧）
                                {
                                    nBelow++;
                                    if (a < 250) nBadBelow++;
                                }
                            }
                        }
                    }
                    CheckTrue(nV > 0, $"量得到这一格的文字网格（{nV} 个顶点）");
                    CheckTrue(nBand > 0, $"**有 {nBand} 个顶点落在渐隐带里**（y > 805.46）—— 0 个 = 软边没接上"
                                       + "（顶点还会被硬裁夹到 855.46，但 alpha 一个都不动）");
                    CheckTrue(minA < 250f, $"带内的字**确实被削了 alpha**（最小 {minA:F0} < 250）—— 只夹顶点不削 alpha 是硬边");
                    Check(nBad, 0, $"带内每个顶点的 alpha = **255 × (855.46 − y) ÷ 50**（原版剖面；最差差 {worst:F2}）");
                    CheckTrue(nBadBelow == 0, $"带外（y ≤ 805.46）的顶点 {nBelow} 个**一个都没被动**"
                                            + "（软边只改带内；动的那些就是写错了带的位置）");
                }

                pp.Close();
                Check(pp.CurrentState, WindowState.Closed, "量完把档案窗关掉（别影响后面的现场）");

                // ---------------- 🆕 2026-10-05：聊天窗 `ChatPanel` ----------------
                //   （§三第29条 **A38 顺手发现①** = 消息行的**滚动区** + **A77⑧** = 压暗层命中区走公共件）
                //   🔴 原版参数是**现读的**（`bundle_mainmenualwaysloaded_assets_all` 的 `Chat Tab` 那棵树）：
                //     `Chat Tab` 那个 GO 上**没有 `ScrollRect`** —— 它是 OSA 虚拟列表
                //     （`ChatContentView : OSA<BaseParamsWithPrefab, ChatEntryView>`，参数在 `_Params` 里：
                //      `_ContentPadding = (25,25,5,5)` · `_ContentSpacing = 10` · `_DefaultItemSize = 60` ·
                //      `_ScrollSensivity = 20` …）。⛔ 所以**不能照抄** `BattleLogPopup` 那组 `Clamped/1.0`
                //     —— 铁律 5·c。下面每一条期望值都从**原档那几个字段**算出来，不是回读实现。
                Section("聊天窗：消息行的**滚动区**（A38 顺手发现①）+ 压暗层命中区（A77⑧）");
                {
                    SocialData.ChatMessages.Clear();                  // 本地没有服务器 ⇒ 这一页出厂就是空的
                    var chat = ChatPanel.Create(win.Manager);
                    win.Manager.OpenWindow(chat);
                    chat.tabButtons.Click(0);
                    Check(chat.CurrentState, WindowState.Open, "聊天窗开得起来（`ChatPanel.Create` + `OpenWindow`）");
                    Check(chat.type, WindowType.Popup, "`type` = **1 Popup**（与社交窗相反）");
                    CheckTrue(ChatPanel.LastOpened == chat, "`ChatPanel.LastOpened` 指到它（自检口）");

                    // ① 压暗层命中区（A77⑧）：档 = 压暗层自己那一档（`QPanel` = 3300）、且严格低于内容档
                    //    （`QHit` = 3308）；并且**确实是公共件 `MenuDraw.ShadeHit` 建的**（改回本窗自己那份
                    //    `MenuDraw.Hit` 就红 —— 两条路没有任何可见行为差异，只有这一句分得出来）。
                    //    🔴 A77⑬③：期望值改成**量**本窗那块**视觉**压暗层 —— 它的节点名是 `CloseBackground`
                    //      （原版语义同族、名字不同；那颗 quad 在它子件 `Image` 上，见 `Shell/ChatPanel.cs:152-153`），
                    //      所以这条走 `Holder/CloseBackground` 路径，而**不**按 `Menu Dark Background` 找。
                    MenuDraw.CheckShadeRule(CheckTrue, "聊天窗", FindChild(FindChild(chat.transform, "CloseBackground"), "CloseHit"),
                                            chat.transform.Find("Holder/CloseBackground"), ChatPanel.QHit);

                    // ② 滚动区（A38 顺手发现①）
                    var ctab = chat.tabs.Count > 0 ? chat.tabs[0] as ChatTab : null;
                    CheckTrue(ctab != null, "第 0 页就是 `ChatTab`（Global）");
                    var csc = ctab != null ? ctab.RowsScroll : null;
                    CheckTrue(csc != null,
                              "★ 这一页**有滚动区了**（`MenuScroll`）—— 补之前 `grep MenuScroll Shell/ChatPanel.cs`"
                              + " **零命中**：消息行既不滚也不裁，**超一屏直接画到框外**、第一屏之外的行永远看不到");
                    if (csc != null && ctab != null)
                    {
                        // 视口 = 原版 `Chat Tab/Viewport`（同一棵树里那一段就是 `Tabs` 给的矩形：
                        // 613.88,161 → 1813.88,911 ⇒ 高 750）。期望值是**原档矩形**，不是回读实现。
                        CheckNear(csc.Viewport.x1, 613.88f, 0.6f, "滚动视口 = 原版 `Chat Tab/Viewport` 左沿 **613.88**");
                        CheckNear(csc.Viewport.y1, 161f, 0.6f, "…上沿 **161**");
                        CheckNear(csc.Viewport.H, 750f, 0.6f, "…高 **750**（911 − 161）");
                        CheckTrue(csc.Vertical, "是**纵向**滚动（原版 `_Orientation = 0`）");

                        // 空表：内容高也要写（写成视口顶）—— 不写就是「上一次的脏值留在区里」（同 BattleLogPopup 那条）
                        CheckNear(ctab.ContentBottom, 161f, 0.6f,
                                  "空表（0 条）也写 `ContentX2` = 视口顶 —— 这一句是「空表不清脏值」那类静默 bug 的判据");
                        CheckTrue(csc.ClampHi <= 0.01f, "…而且此刻**滚不动**（内容比视口短 ⇒ 上下界都收到 0）");

                        // 喂 20 条（60px 行高 + 10px 行距 ⇒ 内容 1400 > 视口 750 ⇒ 真能滚）
                        for (int i = 0; i < 20; i++)
                            SocialData.ChatMessages.Add(new SocialData.ChatMessage
                            {
                                Channel = "Global", Sender = "Probe" + i, Time = "0d 0h",
                                Text = "msg-" + i.ToString("00"), Mine = false, Height = 0f,
                                AvatarArt = ProfileData.AvatarArt,
                            });
                        chat.RefreshMessages();

                        var ccon = ctab.transform.Find("Viewport/Content");
                        CheckTrue(ccon != null, "`Chat Tab/Viewport/Content` 在（消息行挂它下面）");
                        Transform RowOf(string txt)
                        {
                            if (ccon == null) return null;
                            foreach (var t in ccon.GetComponentsInChildren<Transform>(true))
                                if (t.name == "ChatMessageRow" && TextOf(FindChild(t, "Message")) == txt) return t;
                            return null;
                        }

                        // 内容高 = **原版 OSA 的算式**：padT 5 + 20×60 + 19×10 + padB 5 = **1400**
                        // ⇒ `ClampHi` = 1400 − 750 = **650**（这两个数是从 `_Params` 那四个字段算出来的）
                        CheckNear(ctab.ContentBottom, 161f + 1400f, 1f,
                                  "★ 20 条 ⇒ 内容底 = 视口顶 + **1400**（原版 `_ContentPadding`/`_ContentSpacing`/`_DefaultItemSize`）");
                        CheckNear(csc.ClampHi, 650f, 1f, "★ …所以**能滚 650px**（1400 − 750）—— 滚不动的话这条直接红");
                        Check(ctab.BuiltRows, 11,
                              "偏移 0 ⇒ 建出 **11** 行（视口 750 / 每行占 70 ⇒ 161..911 里正好 11 行；"
                              + "整行在视口外的**连节点都不建**）");
                        var r0 = RowOf("msg-00");
                        CheckTrue(r0 != null, "偏移 0 ⇒ **第 1 行**建出来了");
                        if (r0 != null)
                            CheckNear(PxYOf(r0.position.y), 196f, 0.6f,
                                      "…它的中心 = 视口顶 161 + `_ContentPadding.top` 5 + 半行 30 = **196**");
                        CheckTrue(RowOf("msg-19") == null,
                                  "★ 偏移 0 ⇒ **最后一行根本没建**（它整行在 y 1496..1556，视口底下；"
                                  + "补之前是「画到框外」，现在是「不建」——两种都不该出现在画面上）");

                        // 滚到最下（`SetOffset` 就是滚轮/拖拽那条路公用的那一个口）
                        float y10 = RowOf("msg-10") != null ? PxYOf(RowOf("msg-10").position.y) : float.NaN;
                        csc.SetOffset(csc.ClampHi);
                        CheckNear(csc.Offset, 650f, 0.01f, "滚到最下 ⇒ 偏移 = `ClampHi` = **650**");
                        var last = RowOf("msg-19");
                        CheckTrue(last != null,
                                  "★★ 滚到最下 ⇒ **最后一行建出来了**（「超一屏的内容滚得到」的判据 —— 只断「行数」"
                                  + "分不出这两态：两种偏移下都是 11 行）");
                        if (last != null)
                        {
                            float ly = PxYOf(last.position.y);
                            CheckTrue(ly >= csc.Viewport.y1 && ly <= csc.Viewport.y2,
                                      $"…而且它**落在视口里**（中心 y = {ly:F1}，视口 {csc.Viewport.y1:F1}..{csc.Viewport.y2:F1}）");
                        }
                        CheckTrue(RowOf("msg-00") == null,
                                  "★★ …而**第一行滚出视口 ⇒ 不建了**（与上面那条合起来 = 「建的是哪几行」随偏移变）");
                        var r10b = RowOf("msg-10");
                        CheckTrue(r10b != null, "…中间那行（msg-10）两种偏移下都在视口里（下面拿它量位移）");
                        if (r10b != null && !float.IsNaN(y10))
                            CheckNear(y10 - PxYOf(r10b.position.y), 650f, 0.6f,
                                      "★ 同一行**真的换了位置**：内容上移的像素数 == 偏移（650）");

                        // ★ 「**裁**」那半份也要有判据（改之前那一页是「既不滚也不裁」，行直接画到框外）。
                        //   最后那一行的头像命中区：它自己的矩形 = (545.87,841)→(691.90,1004.14)
                        //   （左 68px、下 93px 都在视口外）⇒ 实建出来那块应当**被截到视口**：
                        //   **(613.88,841)→(691.90,911)**。期望值全是几何算出来的（原档行内偏移 + 视口矩形），
                        //   ⛔ 不是回读实现；`clip` 那一格要是丢回 `null`，下沿会变回 1004.14 ⇒ 这两条红。
                        var hitLast = last != null ? FindChild(last, "Hit") : null;
                        float hx1, hy1, hx2, hy2;
                        if (hitLast != null && RectOf(hitLast, out hx1, out hy1, out hx2, out hy2))
                        {
                            CheckNear(hx1, csc.Viewport.x1, 0.8f,
                                      "★ 头像命中区**左沿被裁到视口左缘 613.88**（不裁的话它在 545.87）");
                            CheckNear(hy2, csc.Viewport.y2, 0.8f,
                                      "★ 下沿被裁到**视口底 911**（不裁的话它在 1004.14）");
                            CheckNear(hy1, 841f, 0.8f, "…上沿**没被裁**（841，本来就落在视口里）");
                        }
                        else CheckTrue(false, "拿不到最后一行头像命中区的矩形 —— 这一条等于没验（滚出视口/被裁没了？）");

                        var cpl = PointerLayer.Instance;
                        var under = cpl != null ? cpl.ScrollUnder(1200f, 500f) : null;
                        CheckTrue(under == csc,
                                  "★ 视口中央那一点，**滚轮落到的就是这一区**（`SocialPage.RegisterScroll` 那条路）——"
                                  + "拿到 " + (under == null ? "**null**（没登记）" : "`" + under.GetType().Name + "`"));
                    }

                    // 收尾：清数据 + 关窗（它的压暗层是整屏的，留着会顶掉后面那些真命中路）
                    SocialData.ChatMessages.Clear();
                    chat.RefreshMessages();
                    if (ctab != null) Check(ctab.BuiltRows, 0, "清掉消息 ⇒ 0 行（收尾）");

                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `ChatPanel > Holder > Chat > ChatBackground`
                    //   那颗 `Image` 的 rect（`563.88, 146 → 1863.88, 1076`）；⛔ 不写 `ChatPanel.ChatBgR`
                    //   —— 那是被测实现**传进去的实参**（同式自证）。
                    CheckAbsorbRule("聊天窗", chat.transform, "AbsorbHit",
                                    563.88f, 146f, 1863.88f, 1076f,
                                    ChatPanel.QPanel, ChatPanel.QHit, () => chat.CurrentState);
                    // ⚠️ 上面那一组**结尾就把窗关掉了**（「点面板外 ⇒ 关」那一步）⇒ 这里重开一次，
                    //   下面那条「收尾：聊天窗关掉」才是**真**在断 `Close()`（不开回来它就恒绿了）。
                    //   `ChatPanel.Open()` = `Build()` 重建（不依赖 `Data`）⇒ 重开是安全的。
                    CheckTrue(chat.TryOpen(null), "（A94 收尾）把聊天窗**开回来** —— 下面那条才不是空断");
                    chat.Close();
                    Check(chat.CurrentState, WindowState.Closed, "收尾：聊天窗关掉");
                }

                // ---------------- ③ 商店：三页的 `Packs Scroll View` 都是 (0,25) ----------------
                // ⚠️ **ShopWindow 是 Fullscreen** ⇒ `OpenWindow` 会**把关着的当前主窗关掉**
                //    （`WindowsManager.OpenWindow` 那条原版判定）—— 所以这一段放在**最后**。
                var shop = ShopWindow.Create(win.Manager);
                win.Manager.OpenWindow(shop);
                shop.tabButtons.Click(0);
                var shopPage = FindChild(shop.transform, ShopData.Pages[0].Prefab);
                // ⚠️ 本文件没有 `FindPath`（`FindChild` 是**按名字**找的、不认识 `A/B/C`）⇒ 用 `Transform.Find`
                var shopContent = shopPage != null ? shopPage.Find("Packs Scroll View/Viewport/Content") : null;
                CheckTrue(shopContent != null, "商店 `Card Shop Tab/Packs Scroll View/Viewport/Content` 在");
                // 视口 = 329.76,127.62 → 1920.00,1080.00（原版实测）
                // ⇒ 带内沿：上 127.62 + 25 = **152.62**、下 1080.00 − 25 = **1055.00**
                // 第 1 行格底（133.62..610.62）与第 2 行格底（608.62..1080.00，硬裁到视口底）各压一条 ⇒ 两条都该出现
                CheckSoftCuts(ScanSoftCuts(shopContent, false, ShopTabPage.ScrollView), new[] { 152.62f, 1055.00f }, 0.6f,
                              "商店 `Packs Scroll View` 的格子（原版 `m_Softness = (0,25)`）");
                Check(ScanSoftCuts(shopContent, true, ShopTabPage.ScrollView).Count, 0,
                      "商店这一处**一条竖切线都没有** —— `m_Softness = (0,25)` 只渐变上下");
                shop.Close();
            }

            // ================= 🆕 2026-10-11（A327 · A306② + A306④）：两态夹具 =================
            //   判据 / 断言什么 / 为什么这个形状能照出它 → `资料/普查产出_1011/W4_子3.md` §四·b（两行）。
            //   🔴 **2026-10-11（FX3）订正一处说法（铁律 5）**：原来这两行写「两处都是**潜伏缺陷**」—— **不准确**。
            //   `basis == 窗根` ⇒ `PosInDesignSpace` 除的是窗根的**父级** = Holder（恒单位缩放，
            //   `Shell/WindowsManager.cs` 的 `MakeHolder`）、而 `AttachToAnchor` 把窗根钉在 `localPosition = 0`
            //   （同一文件里那句 `SetParent(anchor, false)` + `localPosition = zero`）⇒ 新旧两式在**该调用形状下永远逐位相同**（⛔ 不是「今天观测不到」）
            //   ⇒ A306①②③④ 那四处码的改动在生产里**是 no-op**（留着只因口径更对 / 防御性）。
            //   ⛔ **别再说成「修好了一个带电的潜伏缺陷」**；真带电的是**非根基准**那一族
            //   （`Shell/PracticeModePopup.cs:494/502/792/796/853/924/1238`）。
            //   ⚠️ 本夹具（M 加在**窗根的父级** + 态二**重建**）照的正是**那一族**的形状（判据 → 文件头 ①②③）。
            SmallScreenUI.PersistOverride = true;      // ⛔ 自检不许动玩家的真设置
            Section("A327 · A306②：`DeckInfoPopup.Local3` 的落位（关 = 逐值不变 / 开 = 设计点 × M）");
            {
                var dipA = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.View);
                CheckTrue(dipA != null, "（前提）`Deck info Popup` 建出来了");
                if (dipA != null)
                {
                    // 🔴 **2026-10-11（FX3）换夹具姿势**（原来是把**窗根**挪到 (2,1.5) ⇒ 那条恒等式必红，见文件头 ①②）：
                    //   M 加在**窗根的父级**那一颗探针根上；窗根留在它**下面**、只给它一个非零位移 (2,1.5)。
                    //   ⇒ 基准（= 窗根）相对被乘那一级的位移 = (2,1.5)（|·| = 2.5 ≥ 1）：坏式与好式相差
                    //     `M(M−1)×2.5` = **0.6 世界单位 = 65px**（容差 0.02 = 2.2px）⇒ 真会红。
                    var probeB = new GameObject("A327② probe root");          // ← 这一颗才是「被乘 M 的那一级」
                    dipA.transform.SetParent(probeB.transform, false);        // 窗根留在被乘那一级**下面**……
                    dipA.transform.localPosition = new Vector3(2f, 1.5f, 0f); // ……并在它下面有一个非零位移
                    dipA.TryOpen(null);                                      // 态一（开关关）建一遍 ⇒ p1 == 设计点
                    var btnsA = FindChild(dipA.transform, "Buttons");
                    CheckTrue(btnsA != null, "（前提）`Buttons` 在（那一层就是 `Local3(root, …)` 摆的）");
                    if (btnsA != null)
                        CheckScaleTwo(probeB, dipA.transform,
                                      () =>
                                      {
                                          // 🔴 **2026-10-12（A437）态二那次必须【先 `Close()` 再开】** ——
                                          //   A217② 把 `GameWindow.TryOpen` 按原版改成按 `CurrentState` 分三档之后，
                                          //   **同窗再开（`Open` 支）会早退、不重建** ⇒ 光再调一次 `TryOpen` 拿到的还是
                                          //   态一那颗 `Buttons`（旧节点）⇒ 下面那条「真的重建了」的前提先红、★ 退化成假绿。
                                          //   `Close()` 走的是**生产那条链**（`Close` → `NotifyClosed` → `state = Closed`）
                                          //   ⇒ 紧接着的 `TryOpen` 落回 `Closed` 支、重建照旧。⛔ **别改成直调 `Open()`**。
                                          dipA.Close();               // ← A437：把 state 送回 `Closed`
                                          dipA.TryOpen(null);         // 🔴 态二**必须重建**（见文件头 ③）
                                          var n = FindChild(dipA.transform, "Buttons");
                                          // （前提③）**真的重建了**：拿到的是**新**节点 ⇒ 被量的局部位置是在
                                          // `k ≠ 1` 那一趟**重算**出来的，不是态一冻结的那份。改坏法：`measure`
                                          // 改回纯读（`() => btnsA.position`）⇒ 这条红（而 ★ 会退化成假绿）。
                                          CheckTrue(n != null && n != btnsA,
                                                    "（前提）…：态二的 measure **真的重建了** `Buttons`"
                                                  + "（`Build()` 首句清空子件 ⇒ 拿到的是新节点）");
                                          if (n != null) btnsA = n;
                                          return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                      },
                                      1.2f,
                                      "A306② `DeckInfoPopup.Local3`（`Buttons` 那一层的落位）"
                                    + " —— 改坏法：`DeckInfoPopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 偏"
                                    + " `M(M−1)×|基准相对被乘那一级的位移 (2,1.5)|` = 0.24×2.5 = **0.6 单位 = 65px**"
                                    + "（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿（那是假绿）");
                    Object.DestroyImmediate(dipA.gameObject);
                    Object.DestroyImmediate(probeB);
                }
                SmallScreenUI.Set(false);
            }

            Section("A327 · A306④：`PracticeModePopup.Local3` 的落位（关 = 逐值不变 / 开 = 设计点 × M）");
            {
                var pmpA = PracticeModePopup.Create(win.Manager);
                CheckTrue(pmpA != null, "（前提）`Practice Mode Menu` 建出来了");
                if (pmpA != null)
                {
                    // 同 A306②：M 加在**窗根的父级**探针根上，窗根留在它下面、只给它一个 (2,1.5) 的位移。
                    // ⚠️ 本窗 `extraScaleSmallScreen = 1.07`（原版实证）：态二那次 `TryOpen` 会照原版那段
                    //   往**窗根**上挂一颗 `TransformScalerBySmallScreenUI`（`menuScale = 1.07`）—— 但
                    //   批处理**没有帧循环**、产品路径只在 `LateUpdate` 里乘，本夹具也不推它 ⇒ 量到的仍是 1.0 那档。
                    var probeD = new GameObject("A327④ probe root");           // ← 被乘 M 的那一级
                    pmpA.transform.SetParent(probeD.transform, false);
                    pmpA.transform.localPosition = new Vector3(2f, 1.5f, 0f);
                    pmpA.TryOpen(null);
                    var selA = FindChild(pmpA.transform, "Army Selector");
                    CheckTrue(selA != null, "（前提）`Army Selector` 在（它是 `Local3(root, …)` 摆的）");
                    if (selA != null)
                        CheckScaleTwo(probeD, pmpA.transform,
                                      () =>
                                      {
                                          // 🔴 **2026-10-12（A437）**：态二必须先 `Close()` —— 理由与 A306② 那一处逐字相同
                                          //   （A217② 之后同窗再开走 `Open` 支、**早退不重建**；`Close()` 走生产链把 state
                                          //   送回 `Closed` ⇒ `TryOpen` 落回 `Closed` 支、重建照旧）。⛔ 别改成直调 `Open()`。
                                          pmpA.Close();               // ← A437：把 state 送回 `Closed`
                                          pmpA.TryOpen(null);         // 🔴 态二**必须重建**（见文件头 ③）
                                          var n = FindChild(pmpA.transform, "Army Selector");
                                          // （前提③）重建真的发生了 —— 改坏法：`measure` 改回纯读 ⇒ 这条红。
                                          CheckTrue(n != null && n != selA,
                                                    "（前提）…：态二的 measure **真的重建了** `Army Selector`"
                                                  + "（`Build()` 首句清空子件 ⇒ 拿到的是新节点）");
                                          if (n != null) selA = n;
                                          return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                      },
                                      1.2f,
                                      "A306④ `PracticeModePopup.Local3`（`Army Selector` 那一层的落位）"
                                    + " —— 改坏法：`PracticeModePopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 偏"
                                    + " `M(M−1)×|基准相对被乘那一级的位移 (2,1.5)|` = 0.24×2.5 = **0.6 单位 = 65px**"
                                    + "（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿（那是假绿）");
                    Object.DestroyImmediate(pmpA.gameObject);
                    Object.DestroyImmediate(probeD);
                }
                SmallScreenUI.Set(false);
                SmallScreenUI.PersistOverride = false;
                CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327：自检跑完把开关放回**出厂值 关**");
            }

            int total = _pass + _fail;
            if (_fail == 0) Debug.Log(P + $"=== 结束：{_pass}/{total} 全过 ✅ ===");
            else
            {
                var sb = new System.Text.StringBuilder(P + $"=== 结束：{_pass}/{total} 通过，**{_fail} 条失败** ❌ ===");
                // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是失败表的【重列】（`Check` 里已经逐条打过）
                //   ⇒ 行首标记 = `失败重列：`，⛔ 不再是 `✗`（原来是 `✗` 时日志里 `✗` 行数 = 失败数 ×2）。
                foreach (var f in _failures) sb.Append("\n").Append(P).Append("   失败重列：").Append(f);
                Debug.LogError(sb.ToString());
            }
            if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
        }

        static float DeckCellWpx() { return CollectionWindow.DeckCellW; }
        static float DeckCellHpx() { return CollectionWindow.DeckCellH; }

        static void SaveScene()
        {
            // 收藏窗是**挂在壳锚点上的窗口**，不该有自己的场景 ⇒ 这里只存一张自检场景供人工看
            var path = "Assets/CardPresentation/Scenes/CollectionCheck.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            Debug.Log(P + $"  自检场景 → {path}");
        }
}
