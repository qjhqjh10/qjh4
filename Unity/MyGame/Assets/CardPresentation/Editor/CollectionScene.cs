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

        /// <summary>🆕 2026-10-18（第四会话）：断言计数器 + 输出口径**收口到共用件 `Editor/MenuCheck.cs`**
        /// （唯一实现处；本文件只剩同名的一行转发 ⇒ 5,534 个调用点一个字没动）。
        /// 🔴 **逐宿主一份 `CheckSink`**（⛔ 不是全局 static）—— 「拿别处的 `Check` 去断，失败会
        /// **记进别人的合计**里 ⇒ 静默」，判据见 `Editor/RewardWindowFixture.cs:12-14`。</summary>
        static readonly CheckSink _sink = new CheckSink(P)
        {
            Near = MenuNearStyle.Compact2,
            BlankNote = MenuBlankNote.Plain,     // 本文件原来那句「（按已知情况放行）」
            StarNotBlank = true,                 // 本文件原来那句「**不是空图**」带 `**`
        };

        static void Section(string t) => MenuCheck.Section(_sink, t);
        static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);
        static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

        /// <summary>🆕 **2026-10-04（A47 接线批）**：压暗层（「点窗外关窗」）命中区那条不变量。
        /// 🔴 **2026-10-07（A77⑬⑥）本文件里的副本已删** —— 唯一一份在 `MenuDraw.CheckShadeRule`。
        /// ⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
        /// <para>🔴 **2026-10-07（A77⑬③）那条判据的期望值也换了**：不再比「调用方传进来的常量」
        /// （与 `ShadeHit` 的实参同一个符号 = 同义反复），改成**量同一扇窗里「视觉压暗层」那颗 quad 的
        /// `RenderQueue`**。🔴 **为什么仍要问 `WasShadeHit`**：档本来就对的那几扇窗，走不走公共件
        /// **没有任何可见行为差异** ⇒ 只有那一句能分出两种状态（改回自己那份 `MenuDraw.Hit` 就红）。</para></summary>

        /// <summary>🆕 **2026-10-15（A825）：本文件原来那一份 `CheckAbsorbRule` 已【收口】——
        /// 唯一一份实现在 `MenuDraw.CheckAbsorbRule`。**</summary>
        /// <para>**签名与 26 个调用点一个字都没动**（本包装的形参表与原来那份逐字相同）；
        /// 「点哪儿 / 为什么钉死 (5,5) / 六步各查什么」的判据全文 → `Shell/MenuDraw.cs` 的 `CheckAbsorbRule`
        /// （⛔ 别在本文件里再抄第二份）。</para>
        /// <para>本文件原来那份里读过的 `EdgeInset` 常量表随函数一起搬进 `MenuDraw.AbsorbEdgeInset`
        /// （本文件那一份**只被这一处读**，2026-10-15 实读）。理由 = 「**两处写同一条规则 = 迟早不一致**」
        /// （`CLAUDE.md` §三）—— 与 `CheckShadeRule`（2026-10-07 · A77⑬⑥）同族。</para>
        static void CheckAbsorbRule(string what, Transform winRoot, string nodeName,
                                    float x1, float y1, float x2, float y2,
                                    int qShade, int qContentMin, System.Func<WindowState> state)
        {
            MenuDraw.CheckAbsorbRule(CheckTrue, CheckNear, what, winRoot, nodeName,
                                     x1, y1, x2, y2, qShade, qContentMin, state);
        }

        /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
        /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
        static void CheckHoverSwap(Transform root, string what) => MenuCheck.HoverSwap(_sink, root, what);

        static void CheckNoMissingSwapArt(string what) => MenuCheck.NoMissingSwapArt(_sink, what);
        static void CheckNear(float got, float want, float tol, string msg)
            => MenuCheck.Near(_sink, got, want, tol, msg);
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

        /// <summary>🆕 2026-10-18（第四会话）：收口到 `MenuCheck.FindChild`（5 份逐字相同的那一份）。
        /// ⚠️ **它不认识 `A/B/C` 这种路径写法** —— 要路径用本文件自己的 `FindPath`。</summary>
        static Transform FindChild(Transform parent, string name) => MenuCheck.FindChild(parent, name);

        /// <summary>🆕 **2026-10-09（`A1125`）**：一颗**关窗钮**的「命中区 + 换图层」四连断。
        /// <para>四个闸落在**四个不同对象**上（前提 / 命中区尺寸 / 命中区位置 / 换图层绑定）⇒ 改坏任一处只红其中一条：
        /// ① 按钮 / 命中 / 可见面三件都取得到（取不到就不许往下断 = 不静默变绿）；
        /// ② 命中区**尺寸** = 原版可射线件**并集**（⛔ 不是根矩形）；③ 命中区**中心** == 可见面渲染中心
        /// （可见面矩形与命中区**不同源** ⇒ 位置那一半的独立锚）；④ 换图层 = 原版 `m_TargetGraphic`
        /// 指到的那一颗（节点名 + 贴图名**两个条件**；`targetNodeName == null` ⇒ 只断**贴图名**）。</para>
        /// <para>`btnName == null` ⇒ 直接在**窗根**下找 `hitName` / `faceName`（命中节点挂窗根那两扇）。
        /// ⛔ 期望值全是**原版 prefab 的读数**，不从被测实现里读。判据 → 同族五份 `A1125Close`。</para></summary>
        static void A1125Close(string win, Transform winRoot, string btnName, string hitName,
                               string faceName, string faceTex, string targetNodeName, float wPx, float hPx)
        {
            var btn = winRoot == null ? null : (btnName == null ? winRoot : FindChild(winRoot, btnName));
            CheckTrue(btn != null, $"（前提·不静默）A1125 {win}：关窗钮节点 `{btnName ?? "<窗根>"}` 拿得到"
                                 + " —— ⛔ 取不到就不往下断（不静默变绿）");
            var hitG = btn != null ? FindChild(btn, hitName) : null;
            CheckTrue(hitG != null, $"（前提·不静默）A1125 {win}：命中节点 `{hitName}` 拿得到");
            var faceG = btn != null ? FindChild(btn, faceName) : null;
            CheckTrue(faceG != null, $"（前提·不静默）A1125 {win}：可见面子件 `{faceName}` 拿得到");
            var hq = hitG != null ? hitG.GetComponentInChildren<ImageQuad>(true) : null;
            float hx1 = 0f, hy1 = 0f, hx2 = 0f, hy2 = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
            bool okH = hq != null && MenuDraw.QuadRectPx(hq, out hx1, out hy1, out hx2, out hy2);
            CheckTrue(okH && Mathf.Abs((hx2 - hx1) - wPx) <= 0.5f && Mathf.Abs((hy2 - hy1) - hPx) <= 0.5f,
                      $"★★ A1125 {win}：关窗钮**命中区** = 原版可射线件并集 **{wPx}×{hPx}**（设计 px）"
                    + (okH ? $"（现读 {hx2 - hx1:F2}×{hy2 - hy1:F2}）" : "（命中 quad 取不到）")
                    + "｜🧨 ① 改回根矩形 ⇒ 每边小 ~11（绿族 ~10.7/9.8）⇒ 红；② 改回子件裸矩形 ⇒ 每边小 20 ⇒ 红");
            var fq = faceG != null ? faceG.GetComponentInChildren<ImageQuad>(true) : null;
            float fx1 = 0f, fy1 = 0f, fx2 = 0f, fy2 = 0f;
            bool okF = fq != null && MenuDraw.QuadRectPx(fq, out fx1, out fy1, out fx2, out fy2);
            CheckTrue(okH && okF
                      && Mathf.Abs((hx1 + hx2) * 0.5f - (fx1 + fx2) * 0.5f) <= 0.6f
                      && Mathf.Abs((hy1 + hy2) * 0.5f - (fy1 + fy2) * 0.5f) <= 0.6f,
                      $"★★ A1125 {win}：命中区**中心** == 可见面 `{faceName}` 的渲染中心（独立锚）"
                    + (okH && okF ? $"（{((hx1 + hx2) * 0.5f):F2},{((hy1 + hy2) * 0.5f):F2} vs "
                                    + $"{((fx1 + fx2) * 0.5f):F2},{((fy1 + fy2) * 0.5f):F2}）" : "（取不到）")
                    + "｜🧨 把命中节点整体搬走（`localPosition += (20,0,0)`）⇒ 只错位置不错尺寸 ⇒ 只这条红（与 ② 不同源）");
            var wb = hitG != null ? hitG.GetComponent<WindowButton>() : null;
            string got = wb == null || wb.target == null || wb.target.Texture == null
                       ? "<没绑>" : wb.target.gameObject.name + " / " + wb.target.Texture.name;
            CheckTrue(wb != null && wb.target != null && wb.target.Texture != null
                      && wb.target.Texture.name == faceTex
                      && (targetNodeName == null || wb.target.gameObject.name == targetNodeName),
                      $"★★ A1125 {win}：**换图层** = 子件 `{targetNodeName ?? faceName}`（图 `{faceTex}`；"
                    + "= 原版 `m_TargetGraphic` 指到的那一颗）—— 现读「" + got + "」"
                    + "｜🧨 把它传成根圆底盘（`UI_Button_Round_background`）⇒ 红（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）");
        }

        /// <summary>🆕 **2026-10-18（`A1149` 第二半 · 第九会话 P6）**：关窗钮**圆底盘**那颗 quad 的
        /// **实绘矩形**两连断 —— 补 `A1125Close` 上面那个**静默缺口**（那四格读的是：命中区 quad 的矩形 /
        /// 命中区中心 vs 见面中心 / 可见面的**节点名** / 可见面的**贴图名** —— **没有一格量圆底盘的矩形成像**；
        /// 圆底盘等比后**仍居中** ⇒ 连「中心」那一格也抓不住它）。
        /// <para>⛔ **期望值全部来自原版 prefab 的逐字段直读**（⛔ 不是我们现在的实绘值、更不是从被测实现里读回来）：
        /// 圆底盘那几颗一律 `m_Type=0`(Simple) · **`m_PreserveAspect=1`** · `m_PixelsPerUnitMultiplier=1.0`；
        /// 贴图 `UI_Button_Round_background` 的 sprite `m_Rect` = **237×237 正方**、`m_Border` / `m_Offset` 全 0
        /// ⇒ 等比内接进「原版根那一格」= **实绘 min(框宽,框高) 见方**（橙族框 74.39×75.61 ⇒ **74.39×74.39**；
        /// 绿族框 75×75 ⇒ **75×75**）。逐窗读数 → `资料/普查产出_第八会话/B4_InboxWindow圆底盘归真.md` §1·2/§1·3
        /// （本件 P6 用 `d:/tmp/wf_b4probe/pa.py` 逐窗复跑核过）。</para>
        /// <para>`frameW` / `frameH` = **原版根那一格**的框（只进消息）；`basePx` = 期望边长。</para>
        /// <para>🔴 **两格各钉一轴**（① 实绘**宽** == `basePx`；② 实绘**宽 == 实绘高** = 等比不变量）⇒
        /// 两种改坏法**各红不同的一格**、**结构上不可能一起变绿**：把矩形改回**子件框** ⇒ 只 ① 红（那档也近正方 ⇒ ② 绿）；
        /// 去掉 `keepAspect` ⇒ 只 ② 红（宽没变 ⇒ ① 绿）。⛔ 别把高度也塞进 ①（否则 ① 两种改坏法都红、② 失去独立作用）。</para>
        /// <para>🆕 **取法 = 「先根后子」**：① 先取**根节点自己**身上那一颗（= 原版结构：原版那颗 `Image` 就长在根节点
        /// `Generic Close Button Orange` / `Generic Close Button Green` 上、**没有独立子件名**）；
        /// ② 根上没有、且调用点**显式给了**子件名 `baseChild` 时才退一步取 `btn/&lt;baseChild&gt;` 那一颗
        /// —— 那是 `A1149` 第一半现读出来的**已知偏离**（圆底盘被画在自造子件上），消息里点名。
        /// `baseChild == null` ⇒ **不许退**。⚠️ 用意：把圆底盘**归真到根节点上不会让本格变红**；
        /// ⛔ 两处都**不下钻**（不用 `GetComponentInChildren` —— 盲扫子树会把「搬到别的层」静默量成合格）。</para>
        /// <para>⚠️ 原版根那一格本身是**正方**的窗（绿族 75×75）第二格**恒真** —— 那种窗本来就没有「等比不变量」可丢，
        /// **不是缺口**（见各调用点注释）。</para></summary>
        static void A1125CloseBase(string win, Transform winRoot, string btnName, string baseTex,
                                   float basePx, float frameW, float frameH, string baseChild = null)
        {
            var btn = winRoot == null ? null : (btnName == null ? winRoot : FindChild(winRoot, btnName));
            // 🔴 `GetComponent`（**本节点自己**那一颗）而不是 `GetComponentInChildren` —— 见上面 doc：
            //    `GetComponentInChildren` 会往下钻，把「圆底盘搬到别的层」那种偏离量成合格（静默）。
            var bq = btn != null ? btn.GetComponent<ImageQuad>() : null;
            bool baseOnRoot = bq != null && bq.Texture != null && bq.Texture.name == baseTex;
            if (!baseOnRoot && btn != null && baseChild != null)
            {
                // ⚠️ **只有调用点显式点名子件时才退这一步**（= `A1149` 第一半现读的已知偏离，消息里点名）
                var baseHost = FindChild(btn, baseChild);
                bq = baseHost != null ? baseHost.GetComponent<ImageQuad>() : null;
            }
            string baseWhere = baseOnRoot ? "根节点自己身上（= 原版结构）"
                             : (baseChild != null
                                ? "子件 `" + baseChild + "`（⚠️ **已知偏离**：原版长在根节点自己身上 —— `A1149` 第一半）"
                                : "根节点自己身上（⚠️ 那颗 quad 取不到）");
            string baseGot = bq == null ? "<没有 quad>" : (bq.Texture == null ? "<没贴图>" : bq.Texture.name);
            CheckTrue(bq != null && bq.Texture != null && bq.Texture.name == baseTex,
                      $"（前提·不静默）A1125 {win}：圆底盘 `{baseTex}` 那颗 quad 拿得到、且贴图就是原版那一张"
                    + " —— ⛔ 取不到 / 取错就不往下断（不静默变绿）"
                    + $"｜现读「{baseGot}」｜取处 = {baseWhere}"
                    + "｜🧨 把圆底盘整颗删掉 / 换成别张图 ⇒ 本格红（⛔ 别改成盲扫子树去「修」它）");
            float bx1 = 0f, by1 = 0f, bx2 = 0f, by2 = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
            bool okB = bq != null && MenuDraw.QuadRectPx(bq, out bx1, out by1, out bx2, out by2);
            float bw = bx2 - bx1, bh = by2 - by1;
            // 🔴 **本格【只钉宽】**（高由下面那格「宽==高」钉）—— 两格各钉一轴，**改坏法才各红一格**。
            CheckTrue(okB && Mathf.Abs(bw - basePx) <= 0.5f,
                      $"★★ A1125 {win}：**圆底盘实绘宽** = 原版 **{basePx:F2}**（设计 px；配下面那格「宽==高」"
                    + $"⇒ 两条一起 = 原版 **{basePx:F2}×{basePx:F2}**）"
                    + (okB ? $"（现读宽 {bw:F2}，高 {bh:F2}）" : "（圆底盘 quad 取不到）")
                    + $"｜= 原版根那一格 **{frameW:F2}×{frameH:F2}** 的宽（正方贴图 + `m_PreserveAspect=1` 的等比内接）"
                    + "｜🧨 把矩形改回**子件框**（橙族 56.86×58.13 / 绿族 56.37×54.50）⇒ 宽少 ~17 ⇒ **只本格红**"
                    + "（那两档也近正方 ⇒ 下一格**仍绿**）");
            // 🔴 **本格 = 等比这条不变量**（比断绝对数抗「将来换贴图」：换成别的正方贴图它照旧成立）。
            CheckTrue(okB && Mathf.Abs(bw - bh) <= 0.5f,
                      $"★★ A1125 {win}：圆底盘**实绘宽 == 实绘高**（= **等比**这条不变量；原版根那一格本身是 "
                    + $"{frameW:F2}×{frameH:F2} 的框 ⇒ 只有真等比才两轴相等）"
                    + (okB ? $"（现读 {bw:F2}×{bh:F2}）" : "（圆底盘 quad 取不到）")
                    + $"｜🧨 把 `keepAspect: true` 去掉 ⇒ 实绘变框那一格 {frameW:F2}×{frameH:F2}"
                    + (Mathf.Abs(frameW - frameH) > 0.5f ? "（**宽≠高**）⇒ **只本格红**（宽没变 ⇒ 上一格绿）"
                                                         : "（= 同值）⇒ 本窗**框本身正方**，去掉 `keepAspect` 本就无差别")
                    + "｜🔴 这就是「它用的是**根那一格** + 等比」的判别式 —— 少了等比，高度就顶到框高");
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

        /// <summary>🆕 **2026-10-13（A750）**：量一段文字 **TMP 自己渲出来那块**的像素矩形
        /// （1920×1080 · 左上原点 · y 向下）—— **这一份才是「字真的从哪开始画」**。
        /// <para>写法与契约**照抄** `Editor/MainMenuScene.cs` 的同名助手（`TmpRenderedRect`，A490/A617 收口的那一份）；
        /// 本文件再留一份，是因为四个自检各自一套辅助函数（本文件开头那条注释已经明记这是**一笔明账**、
        /// 该收口成 `Editor/MenuCheck.cs`）。</para>
        /// <para>🔴 **为什么要用它（灭自证）**：`Label.WorldW/WorldH` 读的是**字段缓存** `_tmpW/_tmpH`，
        /// 而那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs`）—— 也就是**被测实现自己**；
        /// 反过来 `SetCharSpacing`（`:524-533`）· `SetFontSize`（`:503-514`）这一族**只重排 mesh、不刷新缓存**
        /// ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**（实现与检测器共用一个口 = 自证）。
        /// 本助手读的是 TMP 自己的 `textBounds`（mesh 的**活值**），**不在实现那条链上**。
        /// ⚠️ 与本文件既有的 `TextExtentPx`（按**字形顶点**量）**不是同一件事**：那一份不含字距/前进宽，
        /// 而本助手量的 `textBounds` 与 `_tmpW` **同源**（`RefreshBounds` 读的就是它）⇒ 换口当天两条量法**逐位同值**。</para>
        /// <para>⚠️ **取组件带 `true`（含 inactive）**：单参那版只找**激活**的对象，会把「出厂关着」的件
        /// 误报成「这一段字不在」（同 `CheckWrapMode` 那条教训）。
        /// ⚠️ **量不到时（`t` 不在 / 底下没有 TMP）返回 `false`、四个 out 全 0**（与 `RectOf` 同一契约）
        /// ⇒ 调用方**必须先判它**，否则 `0 ≤ 期望值` 会**假绿**。
        /// ⚠️ TMP 在、但字是**空串**时 `textBounds` 是 TMP 的未定义值（哨兵 **4.29e9**）⇒ 那时报出来的是
        /// **天文数字 = 量法没生效**，⛔ 别照它去改实现。</para></summary>
        static bool TmpRenderedRect(Transform t, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (t == null) return false;
            var tmp = t.GetComponentInChildren<TMPro.TextMeshPro>(true);
            if (tmp == null) return false;
            var b = tmp.textBounds;                       // 局部空间的行盒（`Bounds`）
            var M = tmp.transform.localToWorldMatrix;
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
            for (int c = 0; c < 4; c++)
            {
                var corner = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? b.min.x : b.max.x,
                                                            (c < 2) ? b.min.y : b.max.y, 0f));
                float px = LayoutSpace.PxX(corner.x), py = LayoutSpace.PxY(corner.y);
                x1 = Mathf.Min(x1, px); x2 = Mathf.Max(x2, px);
                y1 = Mathf.Min(y1, py); y2 = Mathf.Max(y2, py);
            }
            return true;
        }

        /// <summary>🆕 **2026-10-16（A796′ 换口）**：一个 `Label` **渲出来**的像素宽高（画布 px · `Vector2(w, h)`）。
        /// 🔴 **走哪条口**：`TmpRenderedRect`（= TMP 自己渲出来那块 `textBounds`）——
        ///   ⛔ **不是** `Label.WorldW/H`（那是**字段缓存** `_tmpW/_tmpH`，只有 `RefreshBounds()` 写
        ///   —— 也就是**被测实现自己**；`SetFontSize` / `SetCharSpacing` 这一族**只重排 mesh、不刷缓存**
        ///   ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**）；
        ///   ⛔ **也不是**「TMP 网格顶点」那条口（实测首字左边距：`Current Streak` 43.00 vs 45.22 = **2.22px**
        ///   ⇒ 换错口会让一批期望值**集体偏 1–2px**）。判据 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3。
        /// <para>⚠️ **两条量法同源**（`RefreshBounds` 读的就是 `textBounds`）⇒ **换口当天逐位同值**，
        /// 期望值与容差**一个都不用动**；变的只是「以后重排看得见」。</para>
        /// <para>⚠️ 量不到（`lb == null` / **点阵后端** / 底下没有 TMP）⇒ **退回旧口** `Label.WorldW/H`：
        /// 点阵那条路**本来就没有** `textBounds`（`WorldW` 读的是 `_texW`）⇒ 那是**同一条口的老行为**，
        /// ⛔ 不是「静默吞掉新口」。⚠️ TMP 在、但字是**空串**时两条口读的是**同一个** `textBounds`
        /// 哨兵（4.29e9）⇒ 这一档与换口无关，照旧由调用方的上下界守卫挡。</para>
        /// <para>⚠️ **它只给宽高**：调用方拿它算边缘时，**中心仍是节点位置**（`AlignLeft/Right` 把节点挪走那一档
        /// 一个字没动）。若改成直接用 `textBounds` 那块矩形，矩形会跟着**两项**整体挪 —— TMP 子节点上的
        /// `_vOffset`（A712 字墨校正，`Battle/Label.cs` 的 `RefreshBounds` 里那句）与 `(0.5 − anchor)` 那一项
        /// —— 两者今天**都不保证为 0** ⇒ 期望值会集体漂（那就不是「换口」了）。</para></summary>
        static Vector2 LabelRenderedPx(Label lb)
        {
            if (lb == null) return Vector2.zero;
            float x1, y1, x2, y2;
            if (TmpRenderedRect(lb.transform, out x1, out y1, out x2, out y2))
                return new Vector2(x2 - x1, y2 - y1);
            return new Vector2(lb.WorldW * 108f, lb.WorldH * 108f);   // 退回旧口（点阵后端 / 没有 TMP）
        }

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
                // 🔴 **2026-10-20（`A989②`）：这一处【不受】`materialReferenceIndex != 0` 那条过滤的影响。**
                //    理由（实读 TMP 源码）：TMP 给非 0 号槽建的子件**是这颗 TMP 的子节点**
                //    （`TMP_SubMesh.AddSubTextObject`，`TMP_SubMesh.cs:235-260`：`SetParent` +
                //    localPosition/rotation/scale **全是单位值**），而本函数是
                //    `t.GetComponentsInChildren<MeshRenderer>(true)` —— 那个子件的 `MeshRenderer`
                //    **同样会被遍历到**，只是它身上没有 `TextMeshPro` ⇒ 走下面 `gi == null` 那一支
                //    （整条数组扫，含零填充槽；对「整张卡的外接盒」这种量法无所谓）
                //    ⇒ **`<sprite>` 那枚图标照样被量进来**。⚠️ 别以为这里漏了图标而改过滤（见 `GlyphVertsOf`）。
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
        /// 「★ 卡上没有一个**字的顶点**画到视口外」（A250 块）。
        /// <para>🆕 **2026-10-16（A844 · 跨文件那一半）**：本函数**只剩名字** —— 实现全部转调
        /// `ShellScene.TmpSpanPx(Transform, …)`（全仓这一条量法**唯一一份**实现；本文件内层那段循环已删）。
        /// 🔴 **读数逐位不变**：那一份的内层与这里原来的逐句相同（同一个 `isVisible` 过滤 / 同一个
        /// `LayoutSpace.ToPixel(TransformPoint(v))` / 同一套越界检查），而 min/max 与顶点计数**与遍历次序无关**
        /// ⇒ 收口只换了「谁持有这段代码」，⛔ 不是 A490 说的「量法一变」。
        /// ⚠️ `includeInactive` 这一档**写死 `true`**（本函数的判据就是「关着的那张卡上有没有字画到框外」）
        /// —— 那一份的重载里没有这个形参，原因写在它的 doc 里。</para></summary>
        static bool TextExtentPx(Transform t, out float x1, out float y1, out float x2, out float y2, out int verts)
        {
            return ShellScene.TmpSpanPx(t, out x1, out y1, out x2, out y2, out verts);
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
        /// <para>⚠️ **只收 `materialReferenceIndex == 0` 的字**：别的材质那份顶点**不在这一份网格上**
        /// （`TextMeshPro.UpdateVertexData` 把 `meshInfo[0]` 推给 `m_mesh`、`meshInfo[i>0]` 推给子件
        /// `m_subTextObjects[i].mesh`，判据 `TextMeshPro.cs:408-446`）⇒ 拿 `vertexIndex` 索引**本网格**会串位。
        /// ⚠️ 这段**过滤本身是对的**（返回的下标必须能索引**调用方手里那一份**网格）—— 别删。</para>
        /// <para>🔴 **2026-10-20（`A989①`）就地订正（铁律 5）**：原来这里还接着写
        /// 「本工程的卡面标签**只有一个字体资产**（`Resources/Fonts/NotoSerifCJK-Regular SDF.asset`，
        /// `m_FallbackFontAssetTable: []`）⇒ **实际恒为 0**」—— **那个前提不成立**：
        /// `Core/TmpFont.cs` 给**全工程每颗 TMP** 都挂了 `CardIcons.SpriteAsset`，
        /// 而卡面效果文字里**现在就带** `&lt;sprite name=…&gt;`（`Core/CardText.cs:290` /
        /// `Core/CardIcons.cs` 的 `Rewrite`）⇒ TMP 会**切出第二个材质槽**（sprite asset 自带一份
        /// **独立的材质**）—— `A965` 已证（`资料/普查产出_1018/S6_A965卡面探针取槽.md` §2/§3：
        /// 卡面效果文字那层 `keywords` **就是 2 个槽**）。
        /// <b>⇒ 后果 = 本函数会【跳过】那些字</b>（`&lt;sprite&gt;` 那枚图标本身**不在**返回的下标里）。
        /// **两个调用点各是什么后果，见各自的注释**：`RenderExtentPx`（下面）**不受影响**；
        /// 本文件 `Run` 里那段「★ 卡上没有一个**字的顶点**画到视口外」（A250 块，约 `:3391`）
        /// **量不到图标**。⚠️ 那是**自检覆盖面**的缺口，**不是**产品缺陷 —— 裁切本身走
        /// `MenuDraw.ClipTmpMesh`（`Shell/MenuDraw.cs:1144-1190` 按**每个字自己的**
        /// `materialReferenceIndex` 取槽、写回 `ti.meshInfo[mi]`），`UpdateVertexData` 再把
        /// **每个槽**都上传（`TextMeshPro.cs:408-446`）⇒ 图标**确实被裁也确实上了屏**。</para>
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

        /// <summary>🆕 **2026-10-18（A994③）**：uGUI `Image.PreserveSpriteAspectRatio` 的
        /// **独立复述**（判据 = uGUI 源码 `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Image.cs`：
        /// 贴图比例 &gt; 框比例 ⇒ **宽定、高缩**；否则**高定、宽缩**）。返回内接后该填的 `(w, h)`。
        /// <para>🔴 **为什么自检里要单独写一份**：被测实现那一份是 `CosmeticPreview.PreserveAspectSize`
        /// （`Core/DraggableController.cs`）—— **拿它算期望值就是自证**（两边一起改错照样全绿）。
        /// 本函数只吃两样**外部输入**：**贴图资产自己的宽高**（`Texture2D.width/height`）
        /// 与**原版字面量的框**（250×405 / 337.5×550.8 / 220×330），算法照 uGUI 原文复述。
        /// <para>🔴 **2026-10-19（B2）就地收窄用途**：**卡背本体**不再走本式（它走
        /// <see cref="CardbackDrawnPx"/> 的**两段式**）—— 本式现在**只服务 `_SDF` 那两层**，
        /// 而 `_SDF` 是**故意不进** `CardbackFace` 那张表的（它的 `padding` 恒 0、两段式差 &lt;0.4%，
        /// 理由 → `Core/CardbackFace.cs` 文件头）⇒ 本式对 SDF 仍然是**正确**的期望值。</para></summary>
        static Vector2 InsetFit(float spriteAspect, float boxW, float boxH)
        {
            if (spriteAspect <= 0f || boxW <= 0f || boxH <= 0f) return new Vector2(boxW, boxH);
            return spriteAspect > boxW / boxH
                ? new Vector2(boxW, boxW / spriteAspect)          // 宽定（贴图「更宽」）
                : new Vector2(boxH * spriteAspect, boxH);         // 高定
        }

        /// <summary>🆕 **2026-10-19（B2）**：一张**卡背**在 `boxW×boxH` 的框里**画出来**该多大
        /// —— 原版 uGUI `Image` 的**两段式**（判据与算式全文 → `Core/CardbackFace.cs`）。
        ///
        /// <para>🔴 **为什么自检里要单独写一份**：被测那一份是 `CardbackFace.Fit`（`CosmeticPreview.PreserveAspectSize`
        /// 转调它）—— **直接调它就是自证**（两边一起改错照样全绿）。本函数只吃三样**外部真值**：
        /// ① **贴图资产自己的 `width/height`**（`Resources/Art/cardbacks/` 那 233 张 = 原版 `textureRect` 的裁片）；
        /// ② 原版字面量的框（250×405 / 335.31×400 / 220×330）；
        /// ③ 原版那张 sprite 的 **`m_Rect` = `707×1020`**（233/233 恒定，出处 → `Core/CardbackFace.cs` 文件头）。
        /// 算式照 uGUI 原文复述：**先按 `m_Rect` 的比例定框**（`PreserveSpriteAspectRatio`），
        /// **再按 `textureRect/m_Rect` 缩**（`GetDrawingDimensions` 里那四个 `v`）。</para>
        ///
        /// <para>⚠️ 这里拿 `tex.width/height` 顶 `textureRect` 的宽高（差 ≤0.5px 的取整 ⇒ 尺寸上 ≤0.18px，
        /// 容差 0.6px 挡得住）；**位置**那一半（按 `padding` 偏）拿不到 ⇒ 由
        /// <see cref="CardbackOffsetPx"/> 另走一路。</para></summary>
        static Vector2 CardbackDrawnPx(Texture2D tex, float boxW, float boxH)
        {
            const float RectW = 707f, RectH = 1020f;              // 原版 sprite 的 `m_Rect`（233/233 恒定）
            if (tex == null || tex.height <= 0 || boxW <= 0f || boxH <= 0f) return new Vector2(boxW, boxH);
            // ① 定框：比的是 **`m_Rect` 的比例**，⛔ 不是贴图自己的（这就是 B2 之前漏掉的那一段）
            float spriteRatio = RectW / RectH, boxRatio = boxW / Mathf.Max(1e-6f, boxH);
            float fw, fh;
            if (spriteRatio > boxRatio) { fw = boxW; fh = boxW / spriteRatio; }   // 宽定
            else { fh = boxH; fw = boxH * spriteRatio; }                          // 高定
            // ② 画心 = 框 × (textureRect / m_Rect)（两轴系数相等 ⇒ 等比）
            return new Vector2(fw * tex.width / RectW, fh * tex.height / RectH);
        }

        /// <summary>🆕 **2026-10-19（B2）**：画心相对**框中心**的偏移（画布像素 · **y 向下** · 左上原点）。
        ///
        /// <para>数据 = 原版那张 sprite 的 **`padding`**（`Resources/Cardbacks.json` 的 `padL/padR/padB/padT`
        /// 四列，由 `工具/gen_cardbacks.py` 从原版 `Sprite/&lt;名&gt;_Main.json` 的 `m_Rect − textureRect` 直读）。
        /// ⚠️ **这是「数据」、不是「被测算法」** —— 拿被测实现算期望值才是自证；读同一份**原版数据**
        /// 不算（口径先例：本文件 `InsetFit` 吃 `tex.width/height` 也是数据）。
        /// 残余风险（**明账**）：表里某一行的 `padX` 若被生成器抄错，这一条抓不到（它只验「实现有没有按这四列挪」）。
        /// 独立复核那四列要走 `d:/2/…/Sprite/*.json`，本文件**没读**（不引外部目录依赖）。</para>
        ///
        /// <para>算式的方向照 uGUI 原文**独立复述**：`offset = ((padL−padR)/2, (padB−padT)/2) × (框宽 / `m_Rect.w`)`，
        /// 而 uGUI 的 y **向上** ⇒ 本仓画布像素（y 向下）要**反号**（**这是最容易抄反的一处**）。</para>
        /// 查不到该贴图（不是卡背 / 表是旧版）⇒ 返回 `false`（调用方按「前提不成立」红出来，⛔ 不静默跳过）。</summary>
        static bool CardbackOffsetPx(Texture2D tex, float boxW, float boxH, out float dx, out float dyDown)
        {
            dx = dyDown = 0f;
            if (tex == null) return false;
            float rw, rh, pl, pr, pb, pt;
            if (!CardbackTable.TrySpriteRect(tex.name, out rw, out rh, out pl, out pr, out pb, out pt)) return false;
            float spriteRatio = rw / rh, boxRatio = boxW / Mathf.Max(1e-6f, boxH);
            float fw = spriteRatio > boxRatio ? boxW : boxH * spriteRatio;
            float k = fw / rw;                                    // == fh / rh
            dx = (pl - pr) * 0.5f * k;
            dyDown = -(pb - pt) * 0.5f * k;                       // uGUI 向上为正 ⇒ 本仓向下为正 = 反号
            return true;
        }

        /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
        /// 图走 `ImageQuad.WorldW/H`（材质的真测量，不是回读我们传进去的数）、
        /// 字走 **TMP 自己渲出来那块 `textBounds`**（🆕 **2026-10-16 · A796′ 换口** —— 见 <see cref="LabelRenderedPx"/>）；
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
            if (lb != null)
            {
                // 🔴 **2026-10-16（A796′ 换口）**：宽/高改走 `LabelRenderedPx`（= TMP 自己渲出来那块 `textBounds`）
                //    —— ⛔ 不再读**字段缓存** `Label.WorldW/H`（`_tmpW/_tmpH`，只有 `RefreshBounds()` 写 = 被测实现自己）。
                //    ⚠️ **只换「从哪个口读那个数」**：`node`（= 中心）仍是 `lb.transform`，**图那一支一字未动**。
                //    口径与先例 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3 · `Editor/ShopScene.cs` 同名处（W4 同批）。
                node = lb.transform;
                var lbSz = LabelRenderedPx(lb);
                w = lbSz.x; h = lbSz.y;
            }
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
        /// 🔴 量的东西：**TMP 自己渲出来那块 `textBounds`**（`LabelRenderedPx`；
        /// 🆕 **2026-10-16 · A796′ 换口** —— 换口前读的是 `Label.WorldW` 那份**字段缓存** `_tmpW`）反推的左缘 ——
        /// **不是**节点位置、更**不是**「对齐枚举 == Left」（那种断言是同义反复：把渲染那一句删掉照样绿）。
        /// ⛔ 期望值由**调用方**给（取自原版读数），本函数只负责量。
        /// 量不出来（那行小标题不在 / 宽度是垃圾）⇒ 返回 **−9999** ⇒ 断言必红，**不静默放过**
        /// （宽度上下界那道守卫与 `Label.HasMeasuredWidth` 同一条：TMP 在未激活 / 空串时给的是天文数字；
        ///  ⚠️ 本条**保留**那道守卫，⛔ 别因为换了口就把它删掉）。</summary>
        static float TitleLeftPx(Transform panel, string title)
        {
            var t = FindChild(panel, "Title " + title);
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            if (lb == null) return -9999f;
            float w = LabelRenderedPx(lb).x;      // 🆕 A796′：走 TMP `textBounds`（旧口 = 缓存 `Label.WorldW`）
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
            // 🆕 2026-10-18（A1003 · 账外第 6 份）：算法收口到 `MenuDraw.QuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
            // 🔴 改前这一份是**甲式**（裸 `position × 108 ± 原点`）；`MenuDraw` 那份是**乙式**
            //    （先 `PosInDesignSpace` 除回父级缩放）⇒ 父链 `lossyScale == 1` 时逐位相同。
            return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
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
            => MenuCheck.Shoot(_sink, ShotDir, file, allowBlank, guardBlank: true, meanBrightness: MeanBrightness);

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
            //      `Shell/PromptPopup.cs` 的 `WindowButton` 类注 · `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 · `Shell/PointerLayer.cs:47-48`
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
            _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
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
                    // 🔴 **2026-10-13（A750）换口**：量法 `Label.WorldW`（= 缓存 `_tmpW`）→ **TMP 自己渲出来那块网格**
                    //    —— 与 `Editor/MainMenuScene.cs` 那几处（A709/A714/A715/A718）**同一条口径**；本条 = 这一族的
                    //    **第六处**（第四/五处 = `Editor/RewardsScene.cs` · `Editor/ShopScene.cs`，同一批改完）。
                    //    **为什么这是灭自证**：`Label.WorldW` 那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs`）——
                    //    也就是**被测实现自己**；而 `SetCharSpacing`（`:524-533`）· `SetFontSize`（`:503-514`）这一族
                    //    **只重排 mesh、不刷新缓存** ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**、这条照样绿。
                    //    新口读的是 TMP 自己的 `textBounds`，**不在实现那条链上**。
                    //    期望值 `155f` / 容差 `+ 0.5f` / 文案全文**一位未动** —— 变的只有「从哪个口读那个数」与缩进。
                    // ⚠️ **量不到 ⇒ 必须显式红**：`TmpRenderedRect` 失败时四个 out **全 0** ⇒ 让下面那句拿到 0 的话，
                    //    `0 ≤ 155.5` 会**假绿**。本条的判据句**没有** `> 0f` 那一半 ⇒ **不能**用 `-1f` 哨兵
                    //    （`-1 ≤ 155.5` 也恒真）—— 只能像 A715/A718 那样把原句包进 `else {}` + 补一条显式红。
                    // ⚠️ 量的是 **`klb.transform`**（上面那句 `FindChild(keys[i], "Text").GetComponentInChildren<Label>()`
                    //    **一字未动**；TMP 是 `Label` 的子件 —— `Battle/Label.cs:784` `TmpFont.NewText(transform, …)`）。
                    //    ⛔ 别改成 `TmpRenderedRect(keys[i], …)`：那会捞 `keys[i]` 子树里**第一颗 TMP（含 inactive）**，
                    //    与「只找激活」的 `Label` **未必是同一颗** ⇒ 会把「节点不在（红）」与「量到了别一颗（绿）」混成一档。
                    // **改坏法（只咬旧口）**：在 `Shell/MenuWindowBase.cs` 的 `BuildTabButton` 里那句 `SetWrapping(false)` 那句
                    //    `if (txt != null) txt.SetWrapping(false);` **之后**插一句 `if (txt != null) txt.SetCharSpacing(5f);`
                    //    ——（那句话是四窗左栏键刷**最后一次**缓存的地方：`SetWrapping` → 模式真的变了 → `ForceRelayout`
                    //    → `RefreshBounds()`，`Battle/Label.cs:454-464`）⇒ 网格重排了、**缓存不动** ⇒ 两个口读到的数
                    //    **必然不同**。⚠️ 三件套里的第三件（新口红 / 旧口绿）**本地证不出来**，见下一条如实标。
                    // ⚠️ **如实标**：这四颗键都开着 **autosize**（`BuildTabButton` 的 `SetAutoFitBox`，
                    //    `enableAutoSizing = true`）⇒ TMP 重排时会把字号缩回去、渲出来的宽**仍 ≤ 框宽**
                    //    ⇒ 上面那个改坏法**不一定**把绿翻红（「两个口读到的数不一样」才是换口的全部意义）。
                    //    实测（`_tmp_view/collection.log` 那一次；⚠️ 那是**换口之前**的跑，四颗读的都是**旧口**）：
                    //    `DECKS` 87.3 · `CARDS` 89.0 · `COSMETICS` **153.5** · `STYLES` 96.5
                    //    ⇒ 🔴 第 3 键 `COSMETICS` 余量只有 **2.0px**（十二颗键里最紧的一颗）——
                    //    **首跑重点看它**：红了先量实得值判「实现没对齐」还是「量法没生效（全 0 / 天文数字）」，
                    //    ⛔ **别动期望值 `155f`**。
                    {
                        float rx1, ry1, rx2, ry2;
                        if (!TmpRenderedRect(klb.transform, out rx1, out ry1, out rx2, out ry2))
                        {
                            CheckTrue(false, $"★ 第 {i + 1} 键 `{klb.Text}`（前提）这一颗 `Label` 底下没有 TMP 网格"
                                           + " ⇒ 「渲出来的宽」量不到（⛔ 不是实现把字冲出去了）");
                        }
                        else
                        {
                            float wTabPx = rx2 - rx1;
                            CheckTrue(wTabPx <= 155f + 0.5f,
                                      $"★ …而且**渲出来的宽 {wTabPx:F1} ≤ 框宽 155**"
                                    + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
                        }
                    }
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

            // 🆕 **2026-10-18（第三轮整改 · 审查 P4）：四个页的空态那行字 —— 全部走词条** ----
            //   原版四份 `Empty Collection Warning/Warning` 各挂一条 `MenuCollection/*No*Found`
            //   （本审查先认树、再按 pid 亲读 `mTerm` + TMP 的 `m_text`）：
            //     Deck 页 `MenuCollection/NoDecksFound` · Cards/异画页 `MenuCollection/NoCardsFound` ·
            //     卡背页 `MenuCollection/NoCardbackFound`（卡背抽屉那一份在 `DeckRuntime`，另有断言）。
            //   ⚠️ 这里只断**字**：四页的 `act=F` 出厂态与显隐判据由上面那几条 / `BuildDeckList` 管。
            {
                var eKeys = new[] { "MenuCollection/NoDecksFound", "MenuCollection/NoCardsFound",
                                    "MenuCollection/NoCardbackFound", "MenuCollection/NoCardsFound" };
                var ePage = new[] { "Deck 页(0)", "Cards 页(1)", "卡背页(2)", "异画页(3)" };
                for (int pi = 0; pi < eKeys.Length; pi++)
                {
                    CheckTrue(Loc.HasEntry(eKeys[pi]),
                              $"（前提）词条 `{eKeys[pi]}` 在表里（⛔ 不在 ⇒ {ePage[pi]} 那条两边一起退化成键名 = 假绿）");
                    var ewN = FindChild(win.PageRoot(pi), "Empty Collection Warning");
                    var wlb = ewN != null ? FindChild(ewN, "Warning") : null;
                    CheckTrue(wlb != null, $"{ePage[pi]} 的 `Empty Collection Warning/Warning` 节点在");
                    CheckText(wlb != null ? TextOf(wlb) : null, Loc.T(eKeys[pi]),
                              $"{ePage[pi]} 空态那行字 = `Loc.T(\"{eKeys[pi]}\")`（随语档；"
                            + "原文 = 原版那颗 `Warning` 的 TMP `m_text`）");
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
                    // 🆕 **2026-10-16（A796′）换口**：半宽（`WorldW × 54`）不再读**字段缓存** `Label.WorldW`（`_tmpW`），
                    //    改走 `LabelRenderedPx`（TMP `textBounds` 那条口）—— 中心仍是**节点位置**（只换「从哪个口读那个数」）。
                    float cfR = PxOf(lcf.transform.position.x) + LabelRenderedPx(lcf).x * 0.5f;
                    float iL = PxOf(lil.transform.position.x) - LabelRenderedPx(lil).x * 0.5f;
                    CheckTrue(cfR < iL,
                              $"`Clear filters`（右缘 **{cfR:F0}**）与 `Import Deck`（左缘 **{iL:F0}**）**不叠**"
                              + " —— 判据是 A2 §161 的实测落点 612.2，**别自己按容器推**");
                }
                // 🔴 **2026-10-18（A891 的续）改「随语档」**：`Import` / `Create` 两颗钮的字从本批起走词条
                //   （`Shell/CollectionWindow.cs` 那两处 `Loc.T(...)`）⇒ 宿主跑在**出厂语言 = 中文**时，
                //   原来写死的 `"Import Deck"` / `"Create Deck"` 必红。原文本身没丢：两条词条的**英文列**
                //   = 原版 prefab 那颗 TMP 的 `m_text` 原文（本批按 pid 亲读）—— 见 `Core/Loc.cs` 的注释。
                CheckTrue(Loc.HasEntry("MenuDeck/MenuButtons/ImportDeck")
                          && Loc.HasEntry("MenuDeck/MenuButtons/CreateDeck"),
                          "（前提）两条 `MenuDeck/MenuButtons/*` 词条在表里（⛔ 不在 ⇒ 下面两条两边一起退化成键名）");
                CheckText(TextOf(FindChild(tabsRoot, "Import Text")), Loc.T("MenuDeck/MenuButtons/ImportDeck"),
                          "`Import` 钮文案（键 `MenuDeck/MenuButtons/ImportDeck`，原版 TMP 原文 `Import Deck`；随语档）");
                CheckText(TextOf(FindChild(tabsRoot, "Create Text")), Loc.T("MenuDeck/MenuButtons/CreateDeck"),
                          "`Create` 钮文案（键 `MenuDeck/MenuButtons/CreateDeck`，原版 TMP 原文 `Create Deck`；随语档）");
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
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：空战将位那一支的实现从波 1b 起走词条
                    //   （`Shell/DeckInfoPopup.cs:846` 的 `wl != null ? wl.Name : Loc.T("MenuDeck/Error/NoWarlord")`）
                    //   ⇒ 原来写死的 `"未选督军"` 是**两代前**的字面量（表里 ZH 列已换成「还没有选战将」，
                    //   见 `Core/Loc.cs:927`）⇒ 一旦走到那一支，**两语档都会红**。期望值换成 `Loc.T(键)`。
                    //   ⚠️ 如实记：本件夹具里 `Warlord(0)` 非空 ⇒ 那一支**今天走不到**，本条是为将来兜底。
                    CheckText(TextOf(FindChild(pr, "Warlord Name")),
                              wlc != null ? wlc.Name : Loc.T("MenuDeck/Error/NoWarlord"),
                              "`Warlord Name` = 该卡组的督军名"
                            + "（空战将位那一支随语档 = `Loc.T(\"MenuDeck/Error/NoWarlord\")`）");
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
                            // 🆕 **2026-10-16（A796′）换口**：半宽（`WorldW × 54`）→ `LabelRenderedPx`（TMP `textBounds`）；
                            //    中心仍是**节点位置**（`AlignLeft` 把 Label 节点挪走那一档没动）。
                            if (dnl != null)
                                CheckNear(PxOf(dnl.transform.position.x) - LabelRenderedPx(dnl).x * 0.5f, 872.9f, 2f,
                                          "`Deck Name` 左缘 = **872.9**（跑后真值 · 原版 hAlign=Left）");
                            var wnT = FindChild(pr, "Warlord Name");
                            var wnl = wnT != null ? wnT.GetComponentInChildren<Label>() : null;
                            if (wnl != null)
                                CheckNear(PxOf(wnl.transform.position.x) - LabelRenderedPx(wnl).x * 0.5f, 872.9f, 2f,
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
                        // ⑦ `Share` / `Share On Chat` —— 🔴 **两件不同的事**（⛔ 不是「两个入口一个动作」）。
                        //   · `Share` = **写系统剪贴板 + 弹一条提示**。原版 `DeckInfoPopup__ShareDeck.c` 三句：
                        //     ① `MakeDeckString()` 造串 ② `UnityEngine.GUIUtility.set_systemCopyBuffer(串)`
                        //     **写系统剪贴板** ③ `UIMessageController.ShowMessage(…)` 弹一条消息。
                        //     写剪贴板那份实现收在**全库唯一一处** = `Deck/DeckRuntime.CopyDeckToClipboard`
                        //     （`CLAUDE.md` §三：两处写同一条规则 = 迟早不一致）。
                        //   · `Share On Chat` = 原版**开一扇两键面板（群组 / 全局）→ 发一条聊天消息**、
                        //     **不写剪贴板**（`DeckInfoPopup__ShareDeckOnChat.c`）；那扇面板还没建（账 `A1045`）
                        //     ⇒ 本路**如实出声**（`NotBuilt` 的 `LogWarning` + 一句人话弹窗）。
                        //   🔴 **2026-10-18（`A1051`）就地订正（铁律 5）**：本行**原文**写
                        //     「给卡组串（**原版走平台/服务端**）」—— **那句是过期结论**：原版既不走上服务端、
                        //     也没做平台分享，调的就是**本地** API `GUIUtility.systemCopyBuffer`
                        //     （判据 = `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeck.c` 第 ② 句；
                        //      `Shell/DeckInfoPopup.cs` 的 `ShareDeck` 头注与 `Deck/DeckRuntime.cs` 的
                        //      `CopyDeckToClipboard` 头注两处都已在 2026-10-08（`A1040`）订正过 —— 只剩这一处没跟上）。
                        //   ⛔ **别把这里读成「给卡组串方便玩家自己复制」**：那是 `Share On Chat` 今天的**兜底**，
                        //     不是 `Share` 的语义（`Share` 走剪贴板、成功与否有断言钉着，见下面 A1051 那一节）。
                        var shHit = pop.Opt("Share");
                        CheckTrue(shHit != null && shHit.GetComponent<WindowButton>() != null, "`Share` 有点击区");
                        Debug.Log(P + "   卡组串自检：Share 调 `Deck/DeckRuntime.CopyDeckToClipboard`"
                                    + "（写剪贴板那份实现**全库只此一处**）");
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

            // ============================================================ 🆕 2026-10-18（账 `A1051`）
            //  `Share` / `Share On Chat` —— **四条断言**（分享收口之后这一块**一条自动化尺子都没有**）。
            //
            //  原版判据（逐句实读 `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeck.c`）：
            //   ① `MakeDeckString()` 造串 ② `UnityEngine.GUIUtility.set_systemCopyBuffer(串)`
            //   **写系统剪贴板** ③ `UIMessageController.ShowMessage(…)` 弹一条消息（原版是 toast，
            //   我们退成模态弹窗 ⇒ 账 `A1050`）。⇒「点一下 ⇒ 系统剪贴板里出现这一副的卡组串」是**原版行为**。
            //
            //  ⚠️ **本段自开一扇窗、末尾关掉并清场**（照本文件 `PracticeModePopup.LastOpened = null` 那一段的
            //    做法：自开的窗自己收，不留状态给后段）。**不借上面那一节的 `pop`** —— 这两下各会开一扇模态
            //    提示窗（`WindowsManager.ShowPopUp`），借它会把后段的截图与断言挡掉。
            Section("A1051：`Share` 真写剪贴板 / `Share On Chat` 不写（四条）");
            {
                var sPop = win.OpenDeckInfo(0);
                CheckTrue(sPop != null, "（前提）自开一扇 `Deck info Popup`");
                if (sPop != null)
                {
                    var rawShare = CollectionData.Raw(0);
                    CheckTrue(rawShare != null, "（前提）被点 `Share` 的那一副（下标 0）取得到");

                    var sharHitA = sPop.Opt("Share");
                    var sharWbA = sharHitA != null ? sharHitA.GetComponent<WindowButton>() : null;
                    CheckTrue(sharWbA != null, "（前提）`Share` 那颗钮取得到（(1)(2) 都靠它）");

                    // 期望值 = **这一副**照原版格式导出来的那一串。
                    string wantShare = rawShare != null ? DeckLibrary.ExportString(rawShare) : "";
                    CheckTrue(!string.IsNullOrEmpty(wantShare),
                              "（前提）这一副**导得出**卡组串（空串 ⇒ (1)(2) 等于空转）");

                    // ⚠️ **哨兵必须先置**：不置的话「实现根本没写」与「剪贴板里正好还留着上一次的内容」
                    //    分不开（本工程那条系统性毛病「弱断言分不出两种状态」）。哨兵**不是**合法卡组串。
                    const string ShareSentinel = "A1051-SENTINEL-NOT-A-DECK";

                    // 🔴 **批处理下 `GUIUtility.systemCopyBuffer` 能不能用【没查实】**（`Deck/DeckRuntime.cs`
                    //    的 `CopyDeckToClipboard` 末段就如实记着这一条 —— 红线不许跑 Unity 去核）⇒ 下面几条
                    //    一律走这三个小口，**每个都包一层**：异常**当场红**，⛔ 不让一个未捕获的异常把整个
                    //    `Run()` 掐掉（那会让本节之后**几百条断言一条都不跑**，症状还看着像「本批全崩」）。
                    //    ⚠️ 这三个口**只给自检用**，⛔ 不许替产品代码挡异常（真人点 `Share` 走生产那一句）。
                    bool ClipSet(string s)
                    {
                        try { GUIUtility.systemCopyBuffer = s; return true; }
                        catch (System.Exception e)
                        { Debug.LogError(P + "`systemCopyBuffer` 写入抛异常：" + e.Message); return false; }
                    }
                    string ClipGet()
                    {
                        try { return GUIUtility.systemCopyBuffer; }
                        catch (System.Exception e)
                        { Debug.LogError(P + "`systemCopyBuffer` 读取抛异常：" + e.Message); return "<读剪贴板抛异常>"; }
                    }
                    void ClipClick(WindowButton wb)
                    {
                        try { wb.ClickForTest(); }
                        catch (System.Exception e) { Debug.LogError(P + "点那颗钮抛异常：" + e.Message); }
                    }

                    // ⚠️ **前置**：这个 API 在批处理下**写得进、读得回**吗？不能 ⇒ 当场红
                    //    （那时下面几条对剪贴板的断言既可能假绿、也可能假红 —— 验不了就**说出来**，
                    //     红线「不许静默失败」；⛔ 也不许把这一条放宽成「跳过」）。
                    bool clipOk = ClipSet(ShareSentinel) && ClipGet() == ShareSentinel;
                    CheckTrue(clipOk, "（前提）批处理下 `GUIUtility.systemCopyBuffer` **写得进、读得回**"
                                    + "（置哨兵 → 读回 = 哨兵）—— ⛔ 不能的话下面几条剪贴板断言**等于没验**，所以这里先红");

                    // ---------------------------------------------------------------- (1) 真写剪贴板
                    if (sharWbA != null && !string.IsNullOrEmpty(wantShare) && clipOk)
                    {
                        ClipSet(ShareSentinel);
                        ClipClick(sharWbA);
                        string gotShare = ClipGet();
                        CheckTrue(gotShare != ShareSentinel,
                                  "★ A1051(1)：点 `Share` ⇒ 系统剪贴板**被写过**（哨兵已被顶掉）"
                                + " —— 🧨 **改坏法**：把 `Deck/DeckRuntime.CopyDeckToClipboard` 改成"
                                + "「算完 `return s` 但**不赋值**」⇒ 这一条红（哨兵原样留着）");
                        CheckText(gotShare, wantShare,
                                  "★ A1051(1)：……而且写进去的就是**这一副的卡组串**"
                                + "（原版 `DeckInfoPopup__ShareDeck.c:16` 就这一句 `set_systemCopyBuffer`）");
                    }

                    // ---------------------------------------------------------------- (2) 灭自证
                    // ⛔ **只断 (1) 不够**：(1) 的期望值 `wantShare` 是**被测实现自己调的**那个
                    //    `DeckLibrary.ExportString` ⇒ 谁把 `ExportString` 改坏（例：漏写督军 / 丢掉模式），
                    //    **期望值与实测值一起动、照样全绿**（= 本项目那条「断言自证 / 同义反复」）。
                    // ⇒ 这里把期望值**按原版格式从卡组数据现场重算**（`CardDeck__Serialize.c` 的形状：
                    //    `名字(: 转义) [':' 防御卡] [':' 督军] (':' 每张卡)* ';' 模式`，再 Base64），
                    //    **一个字节都不来自 `ExportString`** ⇒ 结构上不可能「实现与检测器一起改回去还绿」
                    //    （两边没有共用任何函数）。这一点就是本条与 (1) 合起来才算「灭自证」的原因。
                    if (sharWbA != null && !string.IsNullOrEmpty(wantShare) && clipOk)
                    {
                        string plainShare = null;
                        try
                        {
                            plainShare = System.Text.Encoding.UTF8.GetString(
                                             System.Convert.FromBase64String(ClipGet()));
                        }
                        catch { plainShare = null; }        // 不是合法 Base64 ⇒ 下面那条如实红
                        var sbShare = new System.Text.StringBuilder();
                        sbShare.Append((rawShare.Name ?? "").Replace(":", DeckLibrary.ColonEscape));
                        if (!string.IsNullOrEmpty(rawShare.DefensiveId))
                            sbShare.Append(':').Append(rawShare.DefensiveId);
                        if (!string.IsNullOrEmpty(rawShare.WarlordId))
                            sbShare.Append(':').Append(rawShare.WarlordId);
                        foreach (var cidA in rawShare.CardIds ?? new List<string>())
                            if (!string.IsNullOrEmpty(cidA)) sbShare.Append(':').Append(cidA);
                        sbShare.Append(';').Append(rawShare.GameMode);
                        CheckTrue(plainShare != null && plainShare == sbShare.ToString(),
                                  "★ A1051(2) 灭自证：剪贴板那串按**原版格式**解出来 = 名字 / 防御卡 / 督军 / 全部卡 / 模式"
                                + "（期望值由**卡组数据 + 原版格式**现场重算，⛔ 不调 `ExportString`）"
                                + " —— 🧨 改坏法：把 `ExportString` 里那句 `WarlordId` 摘掉 ⇒ (1) 两边一起动仍绿、"
                                + "**这一条必红**"
                                + (plainShare == null ? "（实得：**不是合法 Base64**）" : ""));
                    }

                    // ---------------------------------------------------------------- (3) `Share On Chat` 不写剪贴板
                    // 判据 = 原版 `DeckInfoPopup__ShareDeckOnChat.c`：开一扇两键面板 → **发一条聊天消息**、
                    // **不碰剪贴板**。那扇面板我们还没建（账 `A1045`）⇒ 本路**如实出声**（`NotBuilt`）。
                    // ⚠️ **两半都断**：「剪贴板没动」（行为）+「抓到那条告警」（出声）——
                    //    只断前者的话，把实现整条删掉它也绿（「靠『什么都没发生』的假断言」）。
                    var socHitA = sPop.Opt("Share On Chat");
                    var socWbA = socHitA != null ? socHitA.GetComponent<WindowButton>() : null;
                    CheckTrue(socWbA != null, "（前提）`Share On Chat` 那颗钮取得到");
                    if (socWbA != null)
                    {
                        var socWarns = new List<string>();
                        Application.LogCallback hSoc = (c, st, ty) =>
                        { if (c != null && ty == LogType.Warning) socWarns.Add(c); };
                        if (clipOk) ClipSet(ShareSentinel);
                        // ⚠️ **点这一下不要 gate 在 `clipOk` 上** —— 下面「如实出声」那半与剪贴板无关，
                        //    剪贴板 API 不可用时它照样该被验（那正是本窗今天唯一的可观测行为）。
                        Application.logMessageReceived += hSoc;
                        ClipClick(socWbA);
                        Application.logMessageReceived -= hSoc;
                        if (clipOk)
                            CheckText(ClipGet(), ShareSentinel,
                                      "★ A1051(3)：点 `Share On Chat` ⇒ 剪贴板**一个字节都没动**"
                                    + "（原版那条发的是聊天消息、⛔ 不写剪贴板）"
                                    + " —— 🧨 改坏法：把 `Shell/DeckInfoPopup.ShareDeck` 里那句"
                                    + " `if (key == \"Share On Chat\") { ShareOnChat(); return; }` 删掉 ⇒ 这一条红");
                        int nSocWarn = 0;
                        foreach (var lw in socWarns)
                            if (lw.Contains("[DeckInfo]") && lw.Contains("还没实现")) nSocWarn++;
                        CheckTrue(nSocWarn > 0,
                                  "★ A1051(3)：……而且**如实出声**（抓到 `[DeckInfo] … 还没实现` 的 `LogWarning`）"
                                + " —— 账 `A1045`（那扇两键面板还没建），本项目红线：⛔ 不许静默失败"
                                + (nSocWarn == 0 ? "（实得 0 条；这一下产生的警告共 " + socWarns.Count + " 条）" : ""));
                    }

                    // ---------------------------------------------------------------- (4) 结构式：只有一处实现
                    // 判据 = `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」。
                    //   (1) 证「点一下就真写了」、(4) 证「这一扇窗自己**没有**第二个写点」
                    //   ⇒ 两句合起来才能说「写的那一下必然走了 `DeckRuntime.CopyDeckToClipboard`」。
                    // ⛔ **不写死行号**（行号会漂，本项目明令）—— 读源码文本、按【行首标记】判：
                    //    先跳过 `//` 开头的行（该文件里 5 处 `systemCopyBuffer` **全在注释里** ——
                    //    讲的就是「原版那一句」+「A1040 的订正留档」），再找**孤立的 `=`**
                    //    （⛔ 不把 `==` / `!=` / `<=` / `>=` / `+=` 算成赋值）。
                    // 🧨 **改坏法**：在 `Shell/DeckInfoPopup.ShareDeck` 里就地抄一份
                    //    `GUIUtility.systemCopyBuffer = DeckLibrary.ExportString(deck)` ⇒ 这一条红。
                    // 🧨 **另一头的改坏法**（这条为什么必须配上 (1)）：把 (1) 那两下删掉、只留本条 ⇒
                    //    源码里照样没有赋值、本条照样绿 ⇒ **弱断言**。两条是一对，⛔ 别只留一条。
                    {
                        string srcDipA = System.IO.Path.Combine(Application.dataPath,
                                                                "CardPresentation/Shell/DeckInfoPopup.cs");
                        if (!System.IO.File.Exists(srcDipA))
                        {
                            CheckTrue(false, $"★ A1051(4)：读得到 `Shell/DeckInfoPopup.cs`（路径 {srcDipA}）"
                                           + " —— 读不到时这一条**等于没验**，所以当场红（⛔ 不静默放过）");
                        }
                        else
                        {
                            var hitsA4 = new List<string>();
                            var linesA4 = System.IO.File.ReadAllLines(srcDipA);
                            for (int ia = 0; ia < linesA4.Length; ia++)
                            {
                                string ta = linesA4[ia].TrimStart();
                                if (ta.StartsWith("//")) continue;                    // 注释不算（含 `///`）
                                int pa = ta.IndexOf("systemCopyBuffer", System.StringComparison.Ordinal);
                                if (pa < 0) continue;
                                string tailA = ta.Substring(pa + "systemCopyBuffer".Length);
                                bool assignA = false;
                                for (int ka = 0; ka < tailA.Length; ka++)
                                {
                                    if (tailA[ka] != '=') continue;
                                    char pv = ka > 0 ? tailA[ka - 1] : ' ';
                                    char nx = ka + 1 < tailA.Length ? tailA[ka + 1] : ' ';
                                    if (pv == '=' || pv == '!' || pv == '<' || pv == '>' || pv == '+') continue;
                                    if (nx == '=') continue;
                                    assignA = true; break;
                                }
                                if (assignA)
                                    hitsA4.Add("行" + (ia + 1) + ":" + ta.Substring(0, Mathf.Min(60, ta.Length)));
                            }
                            CheckTrue(hitsA4.Count == 0,
                                      "★ A1051(4)：`Shell/DeckInfoPopup.cs` 源码里**没有** `systemCopyBuffer` 的赋值"
                                    + " —— 写剪贴板那份实现**全库唯一** = `Deck/DeckRuntime.CopyDeckToClipboard`"
                                    + "（两处写同一条规则 = 迟早不一致）；⛔ 断言里不写行号（读文本扫）"
                                    + (hitsA4.Count > 0 ? "（命中：" + string.Join(" | ", hitsA4.ToArray()) + "）" : ""));
                        }
                    }

                    // ⚠️ **收尾顺序不能反**：先清模态提示窗（它会让 `ShowPreviousWindow` 把 `sPop` 提回前台），
                    //    再 `Close()` —— 反过来的话 `sPop` 会被那一跳**重新打开**，留一扇没人管的窗给后段。
                    MainMenuScene.CloseModalPopups();
                    sPop.Close();
                }
            }

            // ============================================================ 🆕 2026-10-18（账 `A1035`）
            //  文案**改长了**之后的溢出闸 —— 三处空态 / 占位句：
            //   · `Shell/DeckSelectionPopup.cs` 的 `Empty Collection Warning/Warning`
            //     键 `MenuCollection/NoDecksFound`（中文列「没有可选的卡组」**7 字 → 11 字**
            //     「没有符合当前筛选的卡组」）
            //   · `Shell/DeckInfoPopup.cs` 的 `Warlord Name` 与 `Shell/PracticeModePopup.cs` 的 `Warlord Name`
            //     键 `MenuDeck/Error/NoWarlord`（中文列「未选战将」**4 字 → 6 字**「还没有选战将」）
            //
            //  🔴 **断的是「渲染宽度 ≤ 框宽」，⛔ 不是「字号对不对」** —— 本项目 2026-09-22 那条教训：
            //    字号对而溢出，自检照样全绿（`AutoFitBox` 那一族）。
            //  ⚠️ 三处**都没接 autosize**（现读核过）：`Shell/DeckSelectionPopup.cs` 那颗是
            //    `MenuDraw.Text(_emptyNode, SvRect, …, 36f, QDsText, SvRect.W)` —— 传了 `wrapPx`、**没传**
            //    `autoMinPx`（缺省 0 ⇒ `MenuDraw.TextCore` 那道 `if (autoMinPx > 0f && fontPx > autoMinPx)`
            //    为假 ⇒ **不调 `SetAutoFitBox`**）；另两处走各自文件的 `Txt(...)` 私有 helper，那两个 helper
            //    **直接 `Label.Create`**、连 `autoMinPx` 这个形参都不存在 ⇒ 更不可能自适应。
            //    ⇒ 这三段字**只能靠框本身放得下**，超了就画到框外（原版那几颗正是固定框 / `NoWrap`）。
            //  ⚠️ **量法 = `LabelRenderedPx`**（TMP 自己那块 `textBounds` 的**活值**，本文件 A750/A796′ 那份），
            //    ⛔ 不是 `Label.WorldW`（那是 `RefreshBounds()` 写的**字段缓存** —— 与被测实现同一个口，自证）。
            //  ⚠️ **期望值（框宽）在断言处按原版矩形的字面量现场写出来**，⛔ 不引实现里的常量名
            //    （同式自证：改实现它跟着绿）。
            Section("A1035：三处空态/占位句 —— 渲染宽度 ≤ 框宽（文案改长之后的溢出闸）");
            {
                // 取**中文列**那一份 —— A1035 改长的**就是它**；而 `Loc.T` 按当前语档取，
                // 自检可能跑在任一语档下（`Loc.Default = Chinese`，但玩家改过就未必）⇒ 这里显式取中文列。
                // ⚠️ 临时切、finally 切回；`PersistOverride = true` 挡住写盘（⛔ 自检不许动玩家的真设置）。
                // ⛔ 别用 `??` 兜底：`Loc.T` 缺键**返回的是键名本身**（不是 null），要判缺键请先 `Loc.HasEntry`。
                string ZhCol(string key)
                {
                    var back = Loc.Current; bool po = Loc.PersistOverride;
                    Loc.PersistOverride = true;
                    string v;
                    try { Loc.SetLanguage(AvailableLanguages.Chinese); v = Loc.T(key); }
                    finally { Loc.SetLanguage(back); Loc.PersistOverride = po; }
                    return v;
                }
                // 把**两列都量一遍**、取宽的那一个（只量当前语档 = 半边绿；中文列正是改长的那一列）。
                // ⚠️ 量法 = `LabelRenderedPx`（TMP 自己那块 `textBounds` 的**活值**）。
                // ⚠️ 返回 0 = 两列都取不到字 ⇒ 调用方**必须显式红**（⛔ 不许 `0 ≤ 框宽` 假绿）。
                float WidestOf(Label lb, string key, out string widestText)
                {
                    float w = 0f; widestText = null;
                    string zh = ZhCol(key), cur = Loc.T(key);
                    for (int iv = 0; iv < 2; iv++)
                    {
                        string s = iv == 0 ? zh : cur;
                        if (string.IsNullOrEmpty(s)) continue;
                        lb.SetText(s);
                        float px = LabelRenderedPx(lb).x;
                        if (px > w) { w = px; widestText = s; }
                    }
                    return w;
                }

                // ---------------------------------------------------------------- ① 选卡组窗的空态
                // 框 = 原版那颗 `Empty Collection Warning` 的矩形 `194.50,208.63 → 1759.50,986.69`
                // （出处 = 本文件 `DeckSelectionPopup` 那一节的 `CheckAt(ewn, 194.5f, 1759.5f, 208.6f, 986.7f, …)`）。
                const float EmptyWarnBoxW = 1565f;          // = 1759.50 − 194.50
                {
                    CheckTrue(Loc.HasEntry("MenuCollection/NoDecksFound"),
                              "（前提）`MenuCollection/NoDecksFound` 在语言表里"
                            + "（缺键时 `Loc.T` 返回的是**键名本身** —— 那时下面量的是键名，等于没验）");
                    // 造「筛选之后一副都不剩」那一态：拿一个**没有任何卡组在用**的模式号去筛。
                    const int NoDeckMode = 9999;
                    for (int iq = 0; iq < CollectionData.DeckCount(); iq++)
                        CheckTrue(CollectionData.DeckAt(iq).GameMode != NoDeckMode,
                                  "（前提）没有哪一副卡组用模式 " + NoDeckMode + "（下面那个筛子才筛得空）");
                    var esel = DeckSelectionPopup.Create(win.Manager, null, NoDeckMode);
                    CheckTrue(esel != null, "（前提）自开一扇 `Deck Selection Popup`（模式筛到空）");
                    if (esel != null)
                    {
                        esel.TryOpen(null);
                        esel.SwitchTab(true);                  // 「我的卡组」页才吃模式筛（预组页不筛）
                        Check(esel.ShownCount, 0, "（前提）这一页被筛空了 ⇒ 空态那句话才真上屏（非空时它是空串）");
                        var ewnA = FindChild(esel.transform, "Empty Collection Warning");
                        var ewtA = ewnA != null ? FindChild(ewnA, "Warning") : null;   // ⛔ 不写 "…/Warning"：`FindChild` 不认斜杠路径
                        var elbA = ewtA != null ? ewtA.GetComponentInChildren<Label>(true) : null;
                        CheckTrue(elbA != null, "（前提）空态那行字的 `Label` 取得到（原版节点名 `Warning`）");
                        if (elbA != null)
                        {
                            CheckTrue(!string.IsNullOrEmpty(elbA.Text),
                                      "（前提）空态那件**真写着字**（⛔ 不是空串 —— 空串时 TMP 的 `textBounds` 是哨兵 4.29e9）");
                            string widestA;
                            float ewPx = WidestOf(elbA, "MenuCollection/NoDecksFound", out widestA);
                            CheckTrue(ewPx > 0.1f && ewPx <= EmptyWarnBoxW + 1f,
                                      $"★ A1035①：`MenuCollection/NoDecksFound` **渲出来 {ewPx:F1}px ≤ 框宽 {EmptyWarnBoxW:F1}px**"
                                    + "（框 = 原版 `Empty Collection Warning` 的矩形 194.50→1759.50；"
                                    + "最宽那一档 = 「" + (widestA ?? "<取不到>") + "」）"
                                    + " —— 🧨 改坏法：把词条中文列再改长到放不下 ⇒ 这条红"
                                    + "（⚠️ 这一颗接了 `wrapPx`，所以它只抓「一整段连不成行」那一档；另两处连折行都没有，更硬）");
                        }
                        PointerLayer.UnregisterOwnedBy(esel.gameObject);   // 自开的窗自己把滚动登记撤掉（A867 那条不变量）
                        esel.Close();
                        Object.DestroyImmediate(esel.gameObject);
                    }
                }

                // ---------------------------------------------------------------- ② 卡组信息窗的空战将位
                // 框 = 原版 `Warlord Name` 的**跑后**矩形 `872.9,166.7 → 1359.9,216.7`
                // （出处 = 本文件那一节的 `CheckNear(… , 872.9f, 2f, "`Warlord Name` 左缘 = 872.9")`
                //   —— 原版 hAlign=Left ⇒ 它是从 872.9 起、右沿 1359.9）。
                const float DiWlBoxW = 487f;                // = 1359.9 − 872.9
                {
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/NoWarlord"),
                              "（前提）`MenuDeck/Error/NoWarlord` 在语言表里");
                    var dwPop = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.View);
                    CheckTrue(dwPop != null, "（前提）自建一扇 `Deck Info Popup`");
                    if (dwPop != null)
                    {
                        dwPop.TryOpen(null);
                        var dwnT = FindChild(dwPop.transform, "Warlord Name");
                        var dwnL = dwnT != null ? dwnT.GetComponentInChildren<Label>(true) : null;
                        CheckTrue(dwnL != null, "（前提）`Warlord Name` 那颗 `Label` 取得到");
                        if (dwnL != null)
                        {
                            // ⚠️ 本夹具里 `Warlord(0)` 非空 ⇒ 走不到「空战将位」那一支 ⇒ **自己把它造出来**
                            //    （`Label.SetText` 是 public 的，且这一扇窗是本段自建的、改完就销毁）。
                            string widestD;
                            float dwPx = WidestOf(dwnL, "MenuDeck/Error/NoWarlord", out widestD);
                            CheckTrue(dwPx > 0.1f && dwPx <= DiWlBoxW + 1f,
                                      $"★ A1035②：`MenuDeck/Error/NoWarlord` **渲出来 {dwPx:F1}px ≤ 框宽 {DiWlBoxW:F1}px**"
                                    + "（框 = 原版 `Warlord Name` 跑后矩形 872.9→1359.9；那一行**不折行、不自适应**；"
                                    + "最宽那一档 = 「" + (widestD ?? "<取不到>") + "」）"
                                    + " —— 🧨 改坏法：把词条中文列再改长到放不下 ⇒ 这条红");
                        }
                        dwPop.Close();
                        Object.DestroyImmediate(dwPop.gameObject);
                    }
                }

                // ---------------------------------------------------------------- ③ 练习窗的空战将位
                // 框 = `Shell/PracticeModePopup.cs` 给 `Warlord Name` 的那个**固定框**
                // `1373.78,269.62 → 1726.64,303.62`（= 该文件的 `WnL/WnT/WnR/WnB` 四个字面量，
                // 下面按 1726.64 − 1373.78 现算 ⇒ ⛔ 不引实现里的常量名）。
                // ⚠️ **如实记**：原版那颗**宽 0 + `ContentSizeFitter` 撑开**、「右沿取 `Deck Name` 的 `DnR`」
                //    **是我们挑的**（见 `Shell/PracticeModePopup.cs` `BuildDeckInfoHead` 的头注）
                //    ⇒ 这一条的「框宽」**不是原版数字**，是**我们自己的版面约束**；
                //    它证明的是「这段字放得进我们给它的那一格」，⛔ 不是「与原版一致」。
                const float PrWlBoxW = 352.86f;             // = 1726.64 − 1373.78
                {
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/NoWarlord"),
                              "（前提）`MenuDeck/Error/NoWarlord` 在语言表里（与 ② 同一条键）");
                    var pwPop = PracticeModePopup.Create(win.Manager);
                    CheckTrue(pwPop != null, "（前提）自建一扇 `Practice Mode Menu`");
                    if (pwPop != null)
                    {
                        pwPop.TryOpen(null);
                        var pwnT = FindChild(pwPop.transform, "Warlord Name");
                        var pwnL = pwnT != null ? pwnT.GetComponentInChildren<Label>(true) : null;
                        CheckTrue(pwnL != null, "（前提）`Warlord Name` 那颗 `Label` 取得到");
                        if (pwnL != null)
                        {
                            // 同上：自己造出「空战将位」那一支（本夹具的卡组有督军）。
                            string widestP;
                            float pwPx = WidestOf(pwnL, "MenuDeck/Error/NoWarlord", out widestP);
                            CheckTrue(pwPx > 0.1f && pwPx <= PrWlBoxW + 1f,
                                      $"★ A1035③：`MenuDeck/Error/NoWarlord` **渲出来 {pwPx:F1}px ≤ 框宽 {PrWlBoxW:F1}px**"
                                    + "（框 = 本窗给 `Warlord Name` 的固定框 1373.78→1726.64；那一行**不折行、不自适应**；"
                                    + "最宽那一档 = 「" + (widestP ?? "<取不到>") + "」）"
                                    + " —— 🧨 改坏法：把词条中文列再改长到放不下 ⇒ 这条红");
                        }
                        PointerLayer.UnregisterOwnedBy(pwPop.gameObject);
                        pwPop.Close();
                        Object.DestroyImmediate(pwPop.gameObject);
                    }
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

                // 🆕 **2026-10-15（A796）**：压暗层「**点了会不会关**」那条 —— 走公共口
                //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs` 的 `CheckShadeClickRule`）；
                //   逐站点表 / 与账上 24 的对账 → `资料/普查产出_1015/W7_A796调用点.md`。
                //   🔴 **本口会把窗【真的关掉】** ⇒ 必须排在**本窗其它断言之后**（这里就是本窗的收尾，
                //      上面那句 `v2.Close()` 因此变成幂等收尾；同族翻车留档 → `Editor/RewardsScene.cs` 的 `Run` 里「探针跑在关闭的窗上」那一段
                //      「探针跑在关着的窗上」⇒ ⛔ 别把本块往上挪）。
                //   上面已经关过一次 ⇒ 这里先开回来：**无参** `TryOpen()` 不碰 `Data`（`Closed` 支会重建
                //      内容）⇒ 下面那颗 `ShadeHit` 是**现取**的，⛔ 别缓存成局部变量。
                CheckTrue(v2.TryOpen(), "（A796 现场）把卡组信息窗开回来 —— 下面那条要在**开着**的窗上点");
                MenuDraw.CheckShadeClickRule(CheckTrue, "卡组信息窗", v2.transform, v2.ShadeHit,
                                             () => v2.CurrentState);

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
                        // ============================================================ 🆕 2026-10-17（D6）
                        // **本窗标题中文化** —— 判据 = **原版 prefab 上的词条**：
                        //   `Deck Selection Popup with Tabs` 的 `Instructions 2` 节点上挂着 `Localize`，
                        //   `mTerm = MenuDeck/Tip/SelectDeckAgainst`
                        //   （实读 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4152179270747796863.json`；
                        //    它的 GameObject = `bundle_menus_assets_all/GameObject/Instructions 2.json`）。
                        //   · 中文 = **实拍那四个字「选择卡组」**
                        //     （`资料/原版参照图/用户实拍_1017/更换卡组的参考.png` 右上角）；
                        //   · 英文 = 我们原来写死的那句 `Select deck`。
                        // ⛔ **版面一个字都不改**：照 prefab（那颗节点 1236.12→1770.24、中心 1503.18、居中）
                        //   —— 实拍那版右端贴分隔线，与 prefab 有漂移，**不照它**。
                        {
                            const string TitleTerm = "MenuDeck/Tip/SelectDeckAgainst";
                            CheckTrue(DeckSelectionPopup.TitleTerm == TitleTerm,
                                      "标题的词条键 = 原版 `Instructions 2` 上的 `mTerm`"
                                    + $"（`{TitleTerm}`）—— ⛔ 不是自拟的键");

                            // ---- ① 词条层：两列各钉一次（★实拍的中文 + 我们原来那句英文）----
                            var langBack = Loc.Current;
                            Loc.PersistOverride = true;                 // 自检不许动玩家的真设置
                            try
                            {
                                Loc.SetLanguage(AvailableLanguages.Chinese);
                                CheckText(Loc.T(TitleTerm), "选择卡组",
                                          "★ 中文列 = 实拍那四个字（`更换卡组的参考.png` 右上角）");
                                Loc.SetLanguage(AvailableLanguages.English);
                                CheckText(Loc.T(TitleTerm), "Select deck", "…英文列 = 我们原来写死的那一句");
                            }
                            finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }

                            // ---- ② 实况：窗上那颗标题的字 = **词条按语言取出来的那一串** ----
                            //   ⛔ 不拿我们传进去的字面量当期望值（那是同义反复）——
                            //   期望值**从 `Loc` 现取**，比的是「窗上那颗跟不跟词条走」。
                            //   🧨 改坏法：把 `Build()` 里那句改回 `MenuDraw.Text(..., "Select deck", ...)`
                            //   ⇒ **在中文档下这一条立刻红**（英文档下它会假绿 —— 见报告里如实记的这条限度）。
                            var ins2 = FindChild(sel.transform, "Instructions 2");
                            var ins2Lb = ins2 != null ? ins2.GetComponentInChildren<Label>() : null;
                            CheckTrue(ins2Lb != null,
                                      "（前提）标题那一格取得到（原版节点名 `Instructions 2`）"
                                    + " —— 取不到的话下面那条恒真");
                            if (ins2Lb != null)
                                CheckText(ins2Lb.Text, Loc.T(TitleTerm),
                                          "★ 标题 = `Loc.T(\"MenuDeck/Tip/SelectDeckAgainst\")`"
                                        + "（⛔ 不是写死的英文）");
                        }

                        // ============================================================ 🆕 2026-10-17（D11 + D12）
                        // 判据一律**原版 prefab 的字段/字面量**（⛔ 不写我们 `DeckSelectionPopup` 里的常量 ——
                        // 那些是被测实现传进去的实参，写它们 = 自证）：
                        //  ① **D11 空态**：原版只有 `Deck Scroll View/Empty Collection Warning` **一件**
                        //     （`DeckCollectionDisplay.emptyWarning` 指向它），出厂 `act=F`；
                        //     rect = **194.5,208.6 → 1759.5,986.7**（= 整个视口那一格），
                        //     子件一颗 TMP 名叫 `Warning`：**fs 36.0 · base 36.0 · 无 auto · 折行=1 · 色(1,1,1,1)**。
                        //     实读命令：`python 工具/menu_dump.py bundle_menus_assets_all "Deck Selection Popup with Tabs" --depth 6`
                        //  ② **D12 页签**：原版两颗 `EverguildToggle` 的序列化字段 —— **两颗共用同一对**
                        //     `onSprite = 40K_tab_button` / `offSprite = 40K_tab_button_overwindow`、
                        //     `onColor = (1,0.6308285,0,1)` / `offColor = (1,0.5442529,0,1)`；
                        //     🔴 **两个开关都开着**：`changeSpriteOnValueChange = 1` · `colorTintOnValueChange = 1`
                        //     （逐个 MB 现读：`MonoBehaviour_907016068568626559`（`m_IsOn=1`）/ `…_-4477857345849663325`（`m_IsOn=0`）；
                        //      七个同名件的字段逐字相同）⇒ 换图与染色**两条都真的生效**。
                        //     落地 = `decomp_full/EverguildToggle__RefreshVisuals.c` → `ToggleSprite` / `ToggleTint`
                        //     （按**自己那颗** `m_IsOn` 取图/取色）；两颗 `Button Text` 的 TMP 都是 `(1,1,1,1)`。
                        // 🔴 **判别式**：两张底图必须是**按状态换的** —— 起手一套、切页之后**互换**。
                        //   原来那版把两张图写死在各颗身上 ⇒ 切页后这一组**必红**（本轮改成按 `on` 取）。
                        // ⚠️ **每次都要现取节点**：`SwitchTab` → `RebuildAll()` 会把整窗子件销毁重建，
                        //   缓存的 `Transform` 立刻变成「已销毁」（Unity 的 `== null` 认得出）⇒ 用旧引用会**假红**。
                        {
                            System.Func<string, Transform> tq = n => FindChild(sel.transform, n);
                            CheckTrue(!sel.OwnDecks, "（前提）起手在预组页 ⇒ 下面「哪颗是选中的」不是猜的");
                            var tPre0 = tq("Generic Tab UI Button");      // 原版出厂 `m_IsOn=1` 的那颗（= 预组页签）
                            var tOwn0 = tq("Generic Tab UI Button 1");    // 出厂 `m_IsOn=0` 的那颗（= 我的卡组页签）
                            CheckTrue(tPre0 != null && tOwn0 != null,
                                      "两个页签节点都在（原版两颗的名字：`Generic Tab UI Button` / `…1`）");
                            // ---- D12 · 起手（预组页）----
                            CheckArt(tPre0, "40K_tab_button",
                                     "★ 预组页签（`m_IsOn=1`）底图 = 原版 `onSprite`");
                            CheckArt(tOwn0, "40K_tab_button_overwindow",
                                     "★ 我的卡组页签（`m_IsOn=0`）底图 = 原版 `offSprite`");
                            CheckTint(tPre0, new Color(1f, 0.6308285f, 0f, 1f),
                                      "★ 选中那颗的染色 = 原版 `EverguildToggle.onColor` (1,0.6308285,0,1)");
                            CheckTint(tOwn0, new Color(1f, 0.5442529f, 0f, 1f),
                                      "★ 未选中那颗的染色 = 原版 `offColor` (1,0.5442529,0,1)");
                            // 文字**两态都白**（原版两颗 `Button Text` 的 TMP 都是 `(1,1,1,1)`，
                            // 而且标签上没有 `EverguildButtonMaterialModifier` ⇒ `ToggleTint` 染不到它）。
                            // 🔴 **判别力来自「两颗必须一样」**：改前那版是「选中白 / 未选中 (0.6,0.6,0.6)」
                            //    ⇒ 只要两颗**不相等**就红（⛔ 不靠「比我传进去的那个字面量」自证）。
                            var tLbPre = tq("Tab Text Pre") != null ? tq("Tab Text Pre").GetComponentInChildren<Label>() : null;
                            var tLbOwn = tq("Tab Text Own") != null ? tq("Tab Text Own").GetComponentInChildren<Label>() : null;
                            CheckTrue(tLbPre != null && tLbOwn != null,
                                      "（前提）两颗页签的文字都取得到 —— 否则下面那两条会退化成恒真");
                            if (tLbPre != null && tLbOwn != null)
                            {
                                var cPre = tLbPre.color; var cOwn = tLbOwn.color;
                                CheckTrue(Mathf.Abs(cOwn.r - cPre.r) < 2f / 255f
                                          && Mathf.Abs(cOwn.g - cPre.g) < 2f / 255f
                                          && Mathf.Abs(cOwn.b - cPre.b) < 2f / 255f,
                                          "★ 选中 / 未选中两颗的文字**颜色相同**（原版两颗 `Button Text` 都是 (1,1,1,1)）"
                                        + $"—— 实测选中 ({cPre.r:F3},{cPre.g:F3},{cPre.b:F3}) / 未选中 ({cOwn.r:F3},{cOwn.g:F3},{cOwn.b:F3})；"
                                        + "「一白一灰」就是改前那套自创写法");
                                CheckTrue(Mathf.Abs(cPre.r - 1f) < 2f / 255f && Mathf.Abs(cPre.g - 1f) < 2f / 255f
                                          && Mathf.Abs(cPre.b - 1f) < 2f / 255f,
                                          $"…而且那个共同值 = **白**（原版 `(1,1,1,1)`）—— 实测 ({cPre.r:F3},{cPre.g:F3},{cPre.b:F3})");
                            }
                            // ---- D11 · 空态那一件（**预组页**非空 ⇒ 它必须**关着**，且**只有它一件**）----
                            // ⚠️ 这一段排在切页**之前**（还在预组页）：预组页的列表来自 `PrebuiltDecks.Tab`，
                            //    「非空」是**已知前提**（那条表在仓库里）；「我的卡组」页则可能被模式筛空 ⇒
                            //    在那儿断「非空」会变成一条没意义的红。
                            CheckTrue(sel.ShownCount > 0, "（前提）预组页列表非空 ⇒ 下面那条不是空转");
                            var ewn = tq("Empty Collection Warning");
                            CheckTrue(ewn != null,
                                      "空态那一件叫 **`Empty Collection Warning`**（原版 `DeckCollectionDisplay.emptyWarning` 那个节点名）");
                            CheckTrue(ewn != null && !ewn.gameObject.activeSelf,
                                      "★ 列表非空 ⇒ 空态那一件**关着**");
                            CheckAt(ewn, 194.5f, 1759.5f, 208.6f, 986.7f,
                                    "★ 空态那一件 = 原版 rect **194.5,208.6 → 1759.5,986.7**（整格视口，⛔ 不是我们原来那个 (SvL,SvT+120)-(SvR,SvT+190) 小框）");
                            var ewt = ewn != null ? FindChild(ewn, "Warning") : null;
                            CheckTrue(ewt != null, "…它下面那颗 TMP 叫 `Warning`（原版就叫这个）");
                            var ewl = ewt != null ? ewt.GetComponentInChildren<Label>() : null;
                            if (ewl != null)
                                CheckNear(ewl.FontPxNow, 36f, 0.6f,
                                          "★ 空态那行字 **fs = 36**（原版 `Warning` 的 `m_fontSize` = 36.0，**无 auto**）");
                            CheckTrue(tq("Scope Note") == null,
                                      "★ 我们自己加的那行 `Scope Note` **已删**（原版空态只有 `Empty Collection Warning` 一件）"
                                    + " —— 改坏法：把 `Build()` 第 7b 步那两行 `MenuDraw.Text` 加回来 ⇒ 本条红");
                            // ---- D12 · 切页 ⇒ 两图**互换**（判别式那一刀）----
                            sel.SwitchTab(true);
                            CheckTrue(sel.OwnDecks, "（前提）切到「我的卡组」页了");
                            CheckArt(tq("Generic Tab UI Button"), "40K_tab_button_overwindow",
                                     "★ 切页后**预组那颗**变成 `offSprite` …");
                            CheckArt(tq("Generic Tab UI Button 1"), "40K_tab_button",
                                     "★ …而**我的卡组那颗**变成 `onSprite` ⇒ 底图**按状态换**"
                                   + "（改坏法：像原来那样把两张图写死在各自身上 ⇒ 这两条同时红）");
                            CheckTint(tq("Generic Tab UI Button"), new Color(1f, 0.5442529f, 0f, 1f),
                                      "★ 预组那颗染色也跟着换成 `offColor`");
                            CheckTint(tq("Generic Tab UI Button 1"), new Color(1f, 0.6308285f, 0f, 1f),
                                      "★ 我的卡组那颗染色换成 `onColor`");
                            sel.SwitchTab(false);              // 还原成预组页（下一句自己会再切过去验模式筛选）
                        }

                        sel.SwitchTab(true);                          // 换到「我的卡组」页才看得见模式筛选
                        // ---- 🆕 2026-10-18（A867）：切页 = **整窗重建** ⇒ 滚动登记表**一条都不涨** ----
                        //   先例（逐字同形）→ `Editor/RewardsScene.cs:8126-8141` 的 A510 那一条；
                        //   现场 / 改坏法 → `资料/普查产出_1018/S1_A840与A867.md` §2.2 / §4.1。
                        //   🔴 本窗这条路**不靠重开窗就够得着**（`SwitchTab` → `RebuildAll()` → `Build()`，
                        //   而页签的命中区就绑在 `SwitchTab` 上）⇒ 它是那 4 扇里**最容易复现**的一扇。
                        //   ⚠️ 先切一次**取基线** —— 那一次会顺手把待清的死条目吸掉（`RegisterScroll` 自带
                        //   `PruneScrolls`），基线之后那次才只反映「本窗重建涨不涨」。
                        sel.SwitchTab(false);                         // ← 吸基线
                        int nScrollDs = PointerLayer.ScrollCountForTest;
                        CheckTrue(nScrollDs > 0,
                                  $"（前提）滚动登记表非空（基线 {nScrollDs} 条）—— ⛔ 为 0 ⇒ 下面那条比的是 0 vs 0，"
                                + "等于没验（`PointerLayer.Instance == null` 时 `ScrollCountForTest` 恒 0）");
                        sel.SwitchTab(true);                          // ← 真·重建一次（顺带把页签还原成「我的卡组」）
                        Check(PointerLayer.ScrollCountForTest, nScrollDs,
                              $"★★ A867：切一次页签（= 整窗重建一次）⇒ 滚动登记表**一条都不涨**"
                            + $"（{nScrollDs} → {PointerLayer.ScrollCountForTest}）"
                            + "｜**改坏法**：删掉 `Shell/DeckSelectionPopup.cs:356` 那句 "
                            + "`PointerLayer.UnregisterOwnedBy(gameObject);` ⇒ 每切一次**净涨 1 条**"
                            + "（`Build()` 只清**子件**、窗根不死，而 `MenuScroll` 是**普通 C# 类** ⇒ "
                            + "`PruneScrolls` 那两道判断恒假），而且旧条目**还能被滚轮命中**"
                            + "（`OnChanged` 指向已销毁的节点）⇒ 本条红");
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
                            //   调用点不再自己把 `SvRect` 当 `clip` 传。
                            //   ⚠️ **2026-10-14 更正（铁律 5 / A786）**：上面这半句**已过期** —— A435 乙块（A21）
                            //   把 `DeckSelectionPopup` 那一对「`Clip = SvRect;` → 循环 → `Clip = prevClip;`」
                            //   **整对删掉**了，裁切状态迁到了 `Deck Scroll View/Viewport` 那颗 `ViewportClip` 上
                            //   （`Shell/DeckSelectionPopup.cs:357-363` / `:490-493`）⇒ 「本窗的 `Clip`」今天**恒 `null`**，
                            //   说了算的是**那颗节点**（`DeckCell(win, …)` 内部沿 `parent` 解析，同一条路）。
                            //   ⇒ 钉两条：
                            //    ① **反向对照**：完全落在视口里的那一格**不许被裁**（裁多了 = 命中区比格还小 ⇒
                            //       四角点不到；也把「`Clip` 取错矩形」这一类错误抓出来）；
                            //    ② 压在视口边上的那一格：**命中区 = 格 ∩ 窗 `Clip`**（整格高 ⇒ 压根没吃到窗口那一份）。
                            //   ⚠️ 期望值用 `CellW/CellH`（**原版 prefab 的格尺寸 225×364.5**，同族已有多条断言钉着它）
                            //     —— 这里比的是**「有没有被多裁」与「交集算没算」**，不是格子摆在哪。
                            //   ⛔ 别去读 `sel.Clip` 来断这件事 —— 要断的是**裁切的结果**，不是那个字段的瞬时值。
                            //     ⚠️ **2026-10-14 更正（铁律 5 / A786）**：原来这里给的理由是「`RebuildCells`
                            //     是「临时设 → 循环 → 还原」（还原后是 null）」—— **那个理由已经过期**：
                            //     A435 乙块（A21）已把 `DeckSelectionPopup` 里那对
                            //     「`Clip = SvRect;` → 循环 → `Clip = prevClip;`」**整对删掉**
                            //     （`Shell/DeckSelectionPopup.cs:490-493` 的更正留档）⇒ 那个 `Clip` 现在**恒 `null`**
                            //     （不是「还原之后才是 null」），读它照样恒红。
                            //     ✅ **结论一个字没改**（照样别读它、照样恒红），变的只是理由。
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
                    //    （会静默变成空做，模态窗留着盖住后面的截图与断言）。
                    // 🔴 **2026-10-14（A512）第三次改：口径统一到共用那一份** —— A416 当时改成的是「按
                    //    `WindowsManager.popUpWindow` 这个**字段**关一扇」（类型无关 ⇒ 收编前后都绿），
                    //    但那个字段**某一刻装的可能正是被测的那扇窗**（判据与踩过的红逐条写在
                    //    `Editor/MainMenuScene.cs` 的 `CloseModalPopups()` 文档注释里）⇒ 本工程口径 =
                    //    **按【类型】清、两类模态宿主（`PopUpGameWindow` / `PromptPopup`）一起覆盖**，
                    //    而且那一份**只有一个定义**（⛔ 别在这儿再抄一份；`Editor/RewardsScene.cs` 同族那处
                    //    也已改调它）。⚠️ 覆盖范围如实记着：它只清那两类**模态提示窗** —— `DeckInfoPopup` /
                    //    `CampaignRewardWindow` 这类别的 `Popup` 窗**不在**清场范围内（它们各有自己的收尾，
                    //    本段下面那句 `dp3.Close()` 就是一例）。
                    MainMenuScene.CloseModalPopups();
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
                    // 🔴 **2026-10-14（A512）补一句显式区分（⛔ 只加这一句）**：这一处读那个字段是【探针】
                    //    （看那一刻装着谁、拿它断文案），**不是清场入口** —— 清场一律走 `CloseModalPopups()`
                    //    （按【类型】清，见本段上面那一处）。两处**语义不同**，别读成「同一个写法两种说法」。
                    var wmFixD = win.Manager != null ? win.Manager : WindowsManager.Instance;
                    GameWindow hp = wmFixD != null ? wmFixD.popUpWindow : null;
                    var hpTxt = hp != null ? TextOf(FindChild(hp.transform, "MessageText")) : null;
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：提示正文从波 1b 起走词条
                    //   （`Shell/DeckInfoPopup.cs:1284` 的 `Loc.T("MenuDeck/Error/HiddenCards") + hiddenWhy`）
                    //   ⇒ 原来写死的子串「隐藏卡」**只在中文列里出现**（英文列 = `This deck contains hidden cards…`）
                    //   ⇒ 宿主一跑英文档这条必红。期望值换成该词条的**整句**（比原来的子串**更强**，且两语档都成立）。
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/HiddenCards"),
                              "（前提）词条 `MenuDeck/Error/HiddenCards` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                    CheckTrue(hpTxt != null && hpTxt.Contains(Loc.T("MenuDeck/Error/HiddenCards")),
                              "…并且**弹出提示说清原因**（不许静默；**随语档**，键 `MenuDeck/Error/HiddenCards`）"
                            + "—— 实测文案「" + (hpTxt ?? "<没有提示窗>") + "」");
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
                // ============================================================ ⑨ 占位符**走词条**（两语档各断一个字面量）
                // 🔴 **2026-10-17（F2 · 修 (α)：这条断言原来把期望值写死成英文）** —— 本批 B11/A884 已经把
                //   占位符改成走词条（`Shell/ImportDeckPopup.cs` 的 `PlaceholderTerm` =
                //   `MenuDeck/HUD/EnterText`，`Core/Loc.cs` 中文列 = 「输入文字...」），而本宿主跑在
                //   **出厂语言 = 中文**（`Core/Loc.cs` 的 `Default`）⇒ 写死 `"Enter text..."` 必红。
                //   正本写法 → `Editor/ShellScene.cs` 的 B11/A884 那一节（那边明写「⛔ 写死 `"Enter text..."`
                //   或写死中文，都过不了这一条」）。
                // 🔴 **为什么这一节另开两扇临时窗、而且必须排在本块那扇 `imp` 之前**：
                //   占位符那行字是 `ImportDeckPopup.Build() → RefreshInputText()` 里
                //   `Label.Create(holder, empty ? Loc.T(PlaceholderTerm) : _text, …)` **当场建出来的**
                //   ⇒ **换语言不会让已经建好的那扇重读词条**（`Loc` 不发事件，见那颗常量的 doc）
                //   ⇒ 要看另一档就得**重开一扇**；而 `WindowsManager.OpenWindow` 的弹窗那一支会把
                //   **当前窗压到背景**（`currentWindow.ToBackground()`）⇒ 若排在本块那扇之后，
                //   后面那串「失败**不关窗**」（`Check(imp.CurrentState, WindowState.Open, …)`）就会红在夹具上。
                var langImp = Loc.Current;
                Loc.RestoreForTest(AvailableLanguages.Chinese);      // 自检口：只改内存，⛔ 不写 `PlayerPrefs`
                var wZh = win.OpenImportPopup();
                string phZh = wZh != null ? TextOf(FindChild(wZh.transform, "Input Text")) : null;
                string msgZh = wZh != null ? TextOf(FindChild(wZh.transform, "Main Search message")) : null;
                string okZh = wZh != null ? TextOf(FindChild(wZh.transform, "Confirm Text")) : null;
                if (wZh != null) wZh.Close();
                Loc.RestoreForTest(AvailableLanguages.English);
                var wEn = win.OpenImportPopup();
                string phEn = wEn != null ? TextOf(FindChild(wEn.transform, "Input Text")) : null;
                string msgEn = wEn != null ? TextOf(FindChild(wEn.transform, "Main Search message")) : null;
                string okEn = wEn != null ? TextOf(FindChild(wEn.transform, "Confirm Text")) : null;
                if (wEn != null) wEn.Close();
                Loc.RestoreForTest(langImp);                         // 收尾：语言放回原值
                CheckTrue(wZh != null && wEn != null, "（前提）两语档各开出了一扇 `Import Deck Popup`"
                          + "（取不到 ⇒ 下面几条恒红，⛔ 不静默）");
                Check(phZh, "输入文字...", "★ 空输入时显示**占位符**：**中文档** = `Loc.T(\"MenuDeck/HUD/EnterText\")`"
                    + " 的**中文列**（⚠️ 那一列是**我们译的**：原版中文在远端 I2 表里 —— 源 `数据/本地化/i18n/zh_CN.csv:102`）");
                Check(phEn, "Enter text...", "★ …**英文档** = 同一条词条的**英文列**"
                    + "（= 原版那颗 TMP 的 `m_text` 原文逐字符照抄，含末尾那三个点）");
                CheckTrue(!string.IsNullOrEmpty(phZh) && phZh != phEn,
                          "★ 判别式：两语档下那句字**必须不一样** —— ⛔ 写死 `\"Enter text...\"`（改前那样）"
                        + "或写死中文，都过不了这一条");
                // 🆕 **2026-10-17（A891）：提示行 / `Confirm` 钮 也走词条了**（同一扇窗的另两颗，判据同占位符那一条）——
                //   `Main Search message` 的词条 = `MenuDeck/Share/PasteDeck`、`Confirm Text` 的词条 =
                //   `MainMenu/General/Confirm`（⛔ 别按行号找那两条老断言，它们在本节后半，已改成**随语档**的期望值）。
                Check(msgZh, "粘贴你的卡组", "★ **提示行（标题）**：**中文档** = `Loc.T(\"MenuDeck/Share/PasteDeck\")` 的中文列"
                    + "（⚠️ 是我们译的 —— 源 `数据/本地化/i18n/zh_CN.csv:143`）");
                Check(msgEn, "Paste your deck", "★ …**英文档** = 同一条词条的英文列"
                    + "（= 原版 `Main Search message` 那颗 TMP 的 `m_text` 原文）");
                CheckTrue(!string.IsNullOrEmpty(msgZh) && msgZh != msgEn,
                          "★ 判别式：**标题**两语档必须不一样 —— ⛔ 写死 `\"Paste your deck\"`（改前那样）过不了");
                Check(okZh, "确认", "★ **`Confirm` 钮**：**中文档** = `Loc.T(\"MainMenu/General/Confirm\")` 的中文列"
                    + "（⚠️ 是我们译的 —— 源 `数据/本地化/i18n/zh_CN.csv:83`）");
                Check(okEn, "Confirm", "★ …**英文档** = 同一条词条的英文列"
                    + "（= 原版 `Button Text` 那颗 TMP 的 `m_text` 原文；那颗挂着 `Localize`）");
                CheckTrue(!string.IsNullOrEmpty(okZh) && okZh != okEn,
                          "★ 判别式：**`Confirm`** 两语档必须不一样 —— ⛔ 写死 `\"Confirm\"`（改前那样）过不了");

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
                    // 🔴 **2026-10-17（A891）**：期望值**按当前语档取** —— 原来写死 `"Paste your deck"`，
                    //   而提示行 B11 那批改走词条、宿主又跑中文档 ⇒ 那条**必红**。⛔ 不是删掉，是换成随语档的字面量；
                    //   两语档的**固定值**在 ⑨ 那一段各断一条（那一对才是不自证的口径）。
                    CheckText(TextOf(FindChild(iroot, "Main Search message")),
                              Loc.Current == AvailableLanguages.English ? "Paste your deck" : "粘贴你的卡组",
                              "提示行文案（随当前语档）");
                    var ph = FindChild(iroot, "Input Text");
                    // ⚠️ 占位符那条断言**已挪到本块最前面**（两语档各断一个字面量 + 判别式）——
                    //   见上面 ⑨ 那一段：这里原来是 `CheckText(TextOf(ph), "Enter text...", …)`，
                    //   而宿主跑在中文档 ⇒ **期望值过期**（B11/A884 起占位符走词条）。⛔ 别在这里补第二条
                    //   （同一条规则两处写 = 迟早不一致；`ph` 本身还被下面那条 y 落位断言用着 ⇒ 保留）。
                    // 🔴 实拍抓的：占位符第一版**跑到输入框上面去了**（`basis` 给了 root 而 parent 是 `Window`）
                    var phLb = ph != null ? ph.GetComponent<Label>() : null;
                    CheckTrue(phLb != null && Mathf.Abs(PxYOf(ph.transform.position.y) - 441.03f) <= 1.5f,
                              $"占位符**落在输入框里**（中心 y 实测 {(ph != null ? PxYOf(ph.transform.position.y) : -1f):F1}"
                              + "，原版 `Text Area` 377→505.06 ⇒ 中心 **441.03**）");
                    // 同上（A891）：`Confirm` 钮的字也走词条了（原版 `Button Text` 挂着 `Localize`）⇒ 期望值随语档。
                    CheckText(TextOf(FindChild(iroot, "Confirm Text")),
                              Loc.Current == AvailableLanguages.English ? "Confirm" : "确认",
                              "确认钮文案（随当前语档）");
                    Shoot("06_收藏_ImportDeck弹窗.png");   // ⚠️ **趁窗开着拍**（第一版拍在导入成功之后 ⇒ 窗已经关了）
                    // ⚠️ 别拿九宫格的**根**量宽 —— `GetComponentInChildren<ImageQuad>()` 取到的是**角块**
                    //    （第一版量出 190.76 = 一个角）。量**点击区那个单 quad**（= 整个按钮矩形）。
                    CheckNear(imp.OkHit != null ? Wpx(imp.OkHit) : -1f, 478.343f, 2f,
                              "确认钮宽 = **478.343**（原版；VLG 只有一个钮 ⇒ 在容器里居中）");
                    var okBtn = imp.OkHit != null ? imp.OkHit.GetComponent<WindowButton>() : null;
                    CheckTrue(okBtn != null, "`Confirm` 有点击区");
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：这两句从波 1b 起走词条
                    //   （`Shell/CollectionData.ImportDeck` → `Loc.T("MenuDeck/Error/ImportEmpty")` /
                    //   `Loc.T("MenuDeck/Error/ImportBadString")`，见 `Core/Loc.cs:1128-1129`）。
                    //   原来写死的那两句中文**只是中文列的副本** ⇒ 宿主一切英文档这两条必红（**单语档断言**）。
                    //   期望值改成 `Loc.T(键)` ⇒ 两语档都成立（中文档下取到的字与改前**逐字相同**，值不变）。
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/ImportEmpty")
                              && Loc.HasEntry("MenuDeck/Error/ImportBadString"),
                              "（前提）词条 `MenuDeck/Error/Import{Empty,BadString}` 都在表里"
                            + "（⛔ 不在 ⇒ 下面两条两边一起退化成**键名** = 假绿）");
                    // ① 空串
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, Loc.T("MenuDeck/Error/ImportEmpty"),
                              "空串 ⇒ 给**人话**（**与卡组编辑那边逐字一致** —— 判据只有 `CollectionData.ImportDeck` 一份；"
                            + "期望值随语档 = `Loc.T(\"MenuDeck/Error/ImportEmpty\")`）");
                    Check(imp.CurrentState, WindowState.Open, "失败**不关窗**");
                    // ② 乱串
                    imp.SetTextForTest("这不是一条卡组串");
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, Loc.T("MenuDeck/Error/ImportBadString"),
                              "乱串 ⇒ 另一句人话（期望值随语档 = `Loc.T(\"MenuDeck/Error/ImportBadString\")`）");
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

                    // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据同上，→ `:2298` 那一段）。
                    //   ⚠️ 上面那一句是**本窗自己的**「点背景 ⇒ 关」；本块补的是**公共口**那一版
                    //   （`ShadeHit` 属于本窗 + 不是吸收层 + **点之前必须是 `Open`** 三条一起断）。
                    //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后；上面已经关过一次 ⇒ 先开回来。
                    CheckTrue(imp2 != null && imp2.TryOpen(),
                              "（A796 现场）把导入卡组窗开回来 —— 下面那条要在**开着**的窗上点");
                    if (imp2 != null)
                        MenuDraw.CheckShadeClickRule(CheckTrue, "导入卡组窗", imp2.transform, imp2.ShadeHit,
                                                     () => imp2.CurrentState);

                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `Import Deck Popup > Window > Generic Popup Background`
                    //   那颗 `Image` 的 rect（`560, 234.07 → 1360, 685.93`）；⛔ 不写 `ImportDeckPopup.WinL…`
                    //   —— 那是被测实现**传进去的实参**（同式自证）。⚠️ 上面那扇 `imp2` 已经关了 ⇒ **另开一扇**。
                    var imp3 = win.OpenImportPopup();
                    CheckTrue(imp3 != null && imp3.CurrentState == WindowState.Open,
                              "（A94 现场）又开出一扇 `Import Deck Popup`");
                    if (imp3 != null)
                    {
                        CheckAbsorbRule("导入卡组窗", imp3.transform, "AbsorbHit",
                                        560f, 234.07f, 1360f, 685.93f,
                                        ImportDeckPopup.QImp, ImportDeckPopup.QImpHit, () => imp3.CurrentState);
                        // 🆕 2026-10-09（`A1125`）：关窗钮的**命中区 + 换图层**四连断 ——
                        //   🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：上面原来写
                        //   「脸 `Close Icon` 也挂**窗根**（⛔ 不套按钮节点）」——**已经不是了**：
                        //   `Shell/ImportDeckPopup.cs` 现补建了原版那颗 `Generic Close Button Green` 节点，
                        //   图标 `Close Icon` 挂在**它**下面（`CloseHit` 仍在窗根）。
                        //   两条 `FindChild` 都是**递归**的 ⇒ 断言逐字不变仍绿。
                        A1125Close("ImportDeckPopup", imp3.transform, null, "CloseHit",
                                   "Close Icon", "40k_bt_close", "Close Icon", 96.37f, 94.50f);
                        // 🆕 **2026-10-18（`A1149` 第二半）**：圆底盘**实绘矩形**两连断。绿族框 **75×75 正方**
                        //   ⇒ 第二格恒真（那类窗没有等比不变量可丢）；第一格钉「用的是原版根那一格 **75**，
                        //   ⛔ 不是子件框 56.37×54.50」。
                        //   🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：上面原来写「本窗**没有**
                        //   原版那颗 `Generic Close Button Green` 节点 … 圆底盘现读挂在子件 `Close Bg` 上」
                        //   ——**两件都已归真**（节点补建 + 圆底盘画在它身上）⇒ `btnName` 由 `null` 改成
                        //   **`"Generic Close Button Green"`**、第 8 实参（子件名 `"Close Bg"`）**已删**。
                        A1125CloseBase("ImportDeckPopup", imp3.transform, "Generic Close Button Green",
                                       "UI_Button_Round_background", 75.00f, 75.00f, 75.00f);
                    }
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

            // ---------------- 🆕 2026-10-17（B10）：卡位**底下那条「张数」**（原版 `Collection Card/Content/Counter`）----------------
            // 判据逐条现读（原始出处写在 `CollectionWindow` 那组 `CardsCnt*` 常量上）：
            //   · 框：`m_AnchorMin = (0.2857143, 0.002)` · `m_AnchorMax = (0.7142857, 0.09966714)` · `pivot (0.5,0)` · `sd (0,0)`
            //     ⇒ 相对 262.5×384 的格 = **x 75…187.5（宽 112.5）· 距格底 0.768…38.272（高 37.504）**
            //   · 底图 `40K_main_deck_card counter`（工程里那张 PNG 实测 **116×36** · Simple · **`m_PreserveAspect = 1`**）
            //     ⇒ 内接进那个框后**按宽顶满 ⇒ 实绘 112.5×34.91**（高那一轴留白 2.59）
            //   · 文案：原版 `"x{0}"` 填**原始拥有数**；**我们印 `min(拥有, 卡组上限)`**（见 `CardsCounterText` 的注释）
            Section("Cards 页：卡位底下那条「张数」（原版 `Collection Card/Content/Counter`）");
            {
                // 量落位之前**先把滚动量归零**（上面两句 `ScrollBy` 之后不保证逐位回到 0；
                // 紧跟着的 A12 那一段也是先 `SetOffset(0)`）—— 归零会经 `OnChanged` 重建格子。
                float savedOff = win.CardsScroll != null ? win.CardsScroll.Offset : 0f;
                if (win.CardsScroll != null) win.CardsScroll.SetOffset(0f);
                int built = win.CardsCells.Count;      // 起手 = 18（3 行 × 6 列，`RectMask2D` 之外的不建）
                int i0 = -1;
                for (int i = 0; i < built; i++) if (win.CardsCellDef(i) != null) { i0 = i; break; }
                CheckTrue(i0 >= 0, $"（前提）视口里至少建成一格（实测 {built} 格）—— 否则这一段等于没验");
                var cell0 = CollectionWindow.CardsCellRect(i0 >= 0 ? i0 : 0);
                var cnt0 = FindChild(tabsRoot, "Counter " + (i0 >= 0 ? i0 : 0));
                CheckTrue(cnt0 != null, "★ 第 " + ((i0 >= 0 ? i0 : 0) + 1) + " 格底下那条「张数」**建出来了**"
                                        + "（原来一个 per-card 计数都没有）");
                if (cnt0 != null)
                {
                    CheckArt(cnt0, "40K_main_deck_card_counter",
                             "…底图 = 原版那条 `40K_main_deck_card counter`（`m_Type=0 Simple`）");
                    var q = cnt0.GetComponentInChildren<ImageQuad>();
                    CheckTrue(q != null, "…底图那个 `ImageQuad` 找得到（下面两条才有意义）");
                    if (q != null)
                    {
                        float bw = q.WorldW * 108f, bh = q.WorldH * 108f;
                        CheckNear(bw, 112.5f, 1.5f,
                                  "★ 底图**渲出来 112.5 宽** = 格宽 262.5 × (0.7142857 − 0.2857143)");
                        CheckNear(bh, 34.914f, 1.6f,
                                  "★ 底图**渲出来 34.91 高**（116×36 的图 + `m_PreserveAspect=1` 内接进 112.5×37.504）"
                                  + " —— **判别式**：把 `Rect(…, keepAspect: true)` 那个 `true` 抽掉"
                                  + " ⇒ 拉满 **37.50**（差 2.59 > 容差）⇒ 本条红");
                    }
                    // 框心：x = (75+187.5)/2 = 131.25（格左偏）；y = (345.728+383.232)/2 = 364.48（格顶偏下沿）
                    CheckAtPx(cnt0, cell0.x1 + 131.25f, cell0.y1 + 364.48f, 1.5f,
                              "★ 那条「张数」落在**格子下沿**（框心 = 格左 +131.25 · 格顶 +364.48；"
                              + "⛔ 不是格心 130.0/192.0 —— 摆错那两轴差几十 px）");
                    var t0 = FindChild(cnt0, "Text (TMP)");
                    CheckTrue(t0 != null, "…那行字是它的子件 `Text (TMP)`（原版 `/Counter/Text (TMP)`）");
                }

                // 每一格都按**它自己那张卡**的卡组上限比（**督军 1** / 传说 1 / 其余 2 —— 三档）——
                // ⚠️ 期望值**不取 `CardsCounterText`**（那会自证），这里按「卡型 + 稀有度」**独立写死三档**。
                // 🔴 **2026-10-17（F2 · 修 (α)：这条原来把「我们不建视口外那条」当成了违约）** ——
                //   `built` 是**建出来的格**（18 = 3 行 × 6 列），而视口 `155.9 → 1079.9` 只有 924 高
                //   ⇒ 第 3 行（i = 12…17）**格底 1307.9** 整行在视口外；那条「张数」的框长在**格底**
                //   （`CollectionWindow.CardsCounterRect`：距格底 0.768…38.272 ⇒ **1269.6…1307.1**）
                //   ⇒ **整条落在视口外** ⇒ `BuildCardsCounter` 里
                //   `var q = Rect(parent, CardsCounterSprite, bar, "Counter " + i, …); if (q == null) return;`
                //   —— `MenuDraw.Rect` 里 `if (!ClipRect(…)) return null;` 会**连节点一起不建**，
                //   而该函数自己的注释写着「② **整条落在视口外** …那是**正常路径**」。
                //   ⇒ 原来那条把「空的 6 条」当成违约（实测 6 条不符 = 正好第 3 行）。
                //   **现在的口径**：对**没有计数节点**的格子跳过，另立**真不变式** ——
                //   「缺的那些【恰好】是矩形落在视口外的那些」（那才是实现真正承诺的东西）。
                //   ⚠️ 原版是「建了但被 `RectMask2D` 遮住」——**两者语义不同**，⛔ 别用「把 `MenuDraw.Rect`
                //   的裁切去掉」来凑绿（那是另一条：视口外的不建 = 省下几十个 quad + 点击区也消失）。
                int bad = 0, lg = 0, nm = 0, skipped = 0, outOfView = 0, missingInView = 0;
                var vpCnt = CollectionWindow.CardsViewport;
                for (int i = 0; i < built; i++)
                {
                    var d = win.CardsCellDef(i);
                    if (d == null) continue;
                    // 这一格那条「张数」**当前**的画布矩形（滚动量 = 0 ⇒ `Shift` 是恒等；判据同
                    // `RebuildCardsCells` 里 `var r = CardsScroll.Shift(content);` 那一跳）。
                    var cellR = win.CardsScroll != null
                        ? win.CardsScroll.Shift(CollectionWindow.CardsCellRect(i))
                        : CollectionWindow.CardsCellRect(i);
                    PxRect visCnt;
                    bool barInView = MenuDraw.ClipRect(CollectionWindow.CardsCounterRect(cellR), vpCnt, out visCnt);
                    var node = FindChild(tabsRoot, "Counter " + i);
                    if (node == null)                       // 没建 ⇒ 只可能是「整条在视口外」（下面那条钉它）
                    {
                        skipped++;
                        if (barInView) missingInView++; else outOfView++;
                        continue;
                    }
                    // 🔴 **2026-10-17（A934）：期望值也要带【卡型】那一档** —— 原版 `GetMaxCopiesInDeck` 把
                    //   `if (cardType == 10) return 1;` 排在**稀有度判断之前**
                    //   （`d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`）
                    //   ⇒ **非传说督军也是 `x1`**（我们池子里 28 张：epic 15 / rare 13）。
                    //   ⚠️ 期望值仍然**不取 `CardsCounterText`**（那会自证）—— 按「督军 / 传说 / 其余」**三档**独立写死。
                    bool isWl = d.Type == "hero";
                    string want = (isWl || d.Rarity == "legendary") ? "x1" : "x2";
                    string got = TextOf(FindChild(node, "Text (TMP)"));
                    if (got != want)
                    {
                        bad++;
                        if (bad <= 3)
                            CheckText(got, want, $"第 {i + 1} 格（{d.Rarity}{(isWl ? " · 督军" : "")}）那条「张数」");
                    }
                    if (want == "x1") lg++; else nm++;
                }
                Check(missingInView, 0, $"「张数」**没建出来**的那些格，**一格都不是**落在视口**里**的"
                              + $"（实测视口内缺 {missingInView} 格 —— ⛔ 那不是裁切，是真缺口）");
                Check(skipped, outOfView, $"…而且缺的那 {skipped} 条「张数」与「框落在视口外」的那些格"
                              + $"**一一对应**（实测落在视口外 {outOfView} 格）—— 判据 = `BuildCardsCounter` 里"
                              + "那句 `if (q == null) return;`（整条在视口外 ⇒ 连节点都不建，那是正常路径）");
                CheckTrue(outOfView >= 1, $"…这条**不是空转**：本夹具真有 {outOfView} 格在视口外（第 3 行整行）"
                              + " —— 少了它上面那条等于没验");
                Check(bad, 0, $"（共 {built} 格，其中 {skipped} 格那条在本滚动量下**不建**）已建的 {lg + nm} 格里"
                              + $"**每一条**「张数」都 = 它那张卡的卡组上限（**督军/传说 `x1`** / 其余 `x2`；"
                              + $"实测 {bad} 条不符）—— 覆盖 上限 1 的 {lg} 格 / 上限 2 的 {nm} 格");
                if (i0 >= 0)
                {
                    var d0 = win.CardsCellDef(i0);
                    int own0 = CardProgress.Owned(d0.Id, d0.Rarity);
                    CheckTrue(TextOf(FindChild(cnt0, "Text (TMP)")) != "x" + own0,
                              "★ **判别式**：印的**不是原始拥有数**（`CardProgress.Owned` 是「给足」口径 = " + own0
                              + " ⇒ 照原版那条式子会印 `x" + own0 + "`；谁把 `Mathf.Min` 删掉「按公式复原」⇒ 本条红）");
                }

                // ================================================================
                //  🔴 **2026-10-17（A934）判别式**：**非传说督军**那条「张数」印 `x1`，而**同稀有度的非督军**仍是 `x2`。
                //  判据 = 原版 `GetMaxCopiesInDeck` 的 `if (cardType == 10) return 1;` **排在稀有度判断之前**
                //  （`d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`）
                //  ⇒ 我们池子里 **28 位督军不是传说**（epic 15 / rare 13），只看稀有度会把他们印成 `x2`（A934 改前正是）。
                //  ⚠️ **两条必须【同时】成立**才叫改对：只断督军 `x1` ⇒ 「一律印 x1」那种写法也能过；
                //     只断非督军 `x2` ⇒ 退回按稀有度也能过。两张卡**稀有度相同** ⇒ 这才挡得住「按稀有度一刀切」。
                //  ⛔ 两处期望值都是**字面量**，不取 `CardsCounterText`（那会自证）。
                {
                    // 卡池快照取一次（`CardsCellDef(i)` 每次都会重建整张列表 ⇒ 逐张取会白跑 1000+ 次）
                    var visAll = CollectionWindow.CardsState.VisibleCards();
                    // 挑一对**同稀有度**的「非传说督军 / 非督军」，**优先挑同一行同一屏里的那两张**
                    //   —— 那样滚一行就能两条都落进视口，屏幕口径也能各量一次。
                    int kHero = -1, kNorm = -1;
                    for (int i = 0; i < visAll.Count && kHero < 0; i++)
                    {
                        var d = visAll[i];
                        if (d.Type != "hero" || d.Rarity == "legendary") continue;
                        for (int j = 0; j < visAll.Count; j++)
                        {
                            var e = visAll[j];
                            if (e.Type == "hero" || e.Rarity != d.Rarity) continue;
                            if (j / CollectionWindow.CardsCols != i / CollectionWindow.CardsCols) continue;   // 同一行
                            kHero = i; kNorm = j; break;
                        }
                    }
                    string pairRar = kHero >= 0 ? visAll[kHero].Rarity : null;
                    CheckTrue(kHero >= 0 && kNorm >= 0,
                              "（前提）卡池里找得到**同一行、同稀有度**的「非传说督军」与「非督军」各一张"
                            + $"（督军下标 {kHero} · 非督军下标 {kNorm}）—— 少了它，下面四条等于没验");
                    // ① 函数口径（与滚到哪儿无关）：成对断，两条一起才挡得住「一刀切」
                    if (kHero >= 0)
                        CheckText(CollectionWindow.CardsCounterText(visAll[kHero]), "x1",
                                  $"★ **判别式 · 督军恒 `x1`**：非传说督军 {visAll[kHero].Name}"
                                + $"（{pairRar} · 第 {kHero + 1} 张）⇒ `x1`"
                                + "（🧨 改坏法：`CardsCounterText` 退回 `DeckCap(def.Rarity)` ⇒ 本条回 `x2`）");
                    if (kNorm >= 0)
                        CheckText(CollectionWindow.CardsCounterText(visAll[kNorm]), "x2",
                                  $"★ **判别式 · 同稀有度的非督军仍是 `x2`**：{visAll[kNorm].Name}"
                                + $"（同为 {pairRar} · 第 {kNorm + 1} 张）⇒ `x2`"
                                + " —— 与上一条**成对**：谁把式子写成「按稀有度一刀切」，这两条必有一条红");
                    // ② 屏幕上真印出来的字**也**量一次（A934 说的就是「印 x2」）
                    var csWl = win.CardsScroll;
                    if (kHero >= 0 && kNorm >= 0 && csWl != null)
                    {
                        float offBefore = csWl.Offset;
                        // 把督军那一格顶到视口上沿 ⇒ 整行落进视口（那条「张数」长在格底 ⇒ 一定建得出来；
                        // 视口高 924 > 2×384 ⇒ 这一行与下一行都是整行可见的）
                        csWl.SetOffset(CollectionWindow.CardsCellRect(kHero).y1 - CollectionWindow.CardsViewport.y1);
                        var nH = FindChild(tabsRoot, "Counter " + kHero);
                        var nN = FindChild(tabsRoot, "Counter " + kNorm);
                        CheckTrue(nH != null && nN != null,
                                  $"…滚到第 {kHero + 1} 格那一行后，**同一行的两条**「张数」都建出来了"
                                + "（⭐那条与跟它成对的那条各一颗）—— 下两条才有意义");
                        if (nH != null)
                            CheckText(TextOf(FindChild(nH, "Text (TMP)")), "x1",
                                      $"★ 非传说督军那一格**印出来的就是 `x1`**（`Counter {kHero}` 的 `Text (TMP)`）");
                        if (nN != null)
                            CheckText(TextOf(FindChild(nN, "Text (TMP)")), "x2",
                                      $"★ …而**同一行里**同为 {pairRar} 的非督军那一格仍是 `x2`（`Counter {kNorm}`）");
                        csWl.SetOffset(offBefore);      // 还原（紧跟着那句 `SetOffset(savedOff)` 再兜一次）
                    }
                }
                if (win.CardsScroll != null) win.CardsScroll.SetOffset(savedOff);   // 还原滚动量
            }

            // 万能卡计数条的 `Army Icon`（原版 `WIldcard Display` 的第三个孩子，A3 §5·3）
            {
                var ai = FindChild(win.PageRoot(1), "Army Icon");
                CheckTrue(ai != null, "★ 计数条左边那颗 `Army Icon` 建出来了（原来**没有**它 —— 那正是「按阵营」那一半）");
                if (ai != null)
                {
                    CheckAt(ai, 1470f, 1550f, 70.94f, 155.94f, "`Army Icon` 落在 **1470,70.94 → 1550,155.94**（80×85）");
                    CheckArt(ai, "40k_DeckSelection_icon_FactionBlackLegion",
                             "…喂的是**预置出厂那一张**阵营徽记（原版 `armyIcon` 运行期由 `WildcardDisplay.Initialize(army)` 换）"
                             + " —— **判别式**：改喂 `DeckRuntime.FactionIcon(null)`（= `40k_collection_bt_decks`）⇒ 本条红");
                }
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
            //  **改坏法**：抽掉 `CollectionWindow.RebuildCardsCells` 里 `v.SetClipFromTree();` 那一句
            //  （或 `CardView.CropLayer`）⇒ 被切那一排的层会顶到视口上沿之上 ⇒ 这一组**立刻红**。
            //  🔴 **2026-10-13（A435 辛 · A774）就地订正（铁律 5）**：这一行原来写的是「抽掉
            //   `v.SetPose(…, CardsViewport)` 的第 4 个实参」—— 迁移后**那个实参已经不存在**了
            //   （裁切边界改走 `CardView.SetClipFromTree()` 沿父链解析那颗 `ViewportClip`）。
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
                //    ③ 抽掉 `RebuildCardsCells` 里 `v.SetClipFromTree();` 那一句 ⇒ 整卡不裁 ⇒ 红
                //       （⚠️ **2026-10-13（A435 辛 · A774）就地订正**：原文指的「第 4 个实参」已随迁移消失）。
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
                            //  🔴 **2026-10-20（`A989②`）· 已查清的【覆盖面缺口，不是产品缺陷】**：
                            //     本处只取**这颗 TMP 自己**的 `MeshFilter.sharedMesh`（= 0 号槽那份上传网格）
                            //     ⇒ `GlyphVertsOf` 会把 `<sprite name=…>` 那些字（**非 0 号槽**）跳过
                            //     ⇒ 这条断言盯的是「**不含图标**的那部分字**」有没有画到视口外，
                            //     **图标那枚字形没被它看着**。裁切本身是好的（`MenuDraw.ClipTmpMesh`
                            //     按每个字自己的 `materialReferenceIndex` 取槽，`TextMeshPro.UpdateVertexData`
                            //     把**每个槽**都上传到 `m_subTextObjects[i].mesh`）⇒ **画面不会出错**。
                            //     ⚠️ **不能**照 `Editor/CardFaceProbe.cs:273-296` 那条改成
                            //     `chr[i].materialReferenceIndex` 取 `textInfo.meshInfo[slot]` —— 那是读**模型**，
                            //     而本块的立身之本正是「读**真上传的那一份**」（上面三条改坏法里第②条
                            //     就是「只写 `textInfo`、不上传」）。要补覆盖面只能**另找那份上传网格**
                            //     （子件 `m_subTextObjects[slot]` 的那个 `MeshRenderer`）。判据不足 ⇒ 本轮只记录。
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

            // ═══════════ 🆕 2026-10-13（A435 辛 · 第 7 种载体的收口：A773 + A774 + A775）═══════════
            //  背景：A435 阶段 2 把裁切状态从「窗级三兄弟」搬到了**视口节点**上，但还有一族没迁 ——
            //  「**调用点自己把 clip 吃掉**」（不转调 `MenuDraw.*` 的解析口），普查实锤 20 组，
            //  剩下的两组就是本段（`资料/普查产出_1013/WA766_同形口普查.md` §二 A-1/A-2）。
            //  🔴 **为什么一条计数器都不能当判据**：这一族的口**根本不调 `ViewportClip.Resolve`** ⇒
            //   `NodeShadowedByParam` / `NodeResolutions` 对它**恒不变**（普查 §五·3 已证）——
            //   只能量行为：**渲出来的几何**（网格顶点 / TMP 顶点）与**真落点**。
            //  ⚠️ 本段所有期望值都不写死成「我们自己的常量」：要么用**节点自己那份状态**现算，
            //     要么**先量对照组**再拿它当边界（见 A773 那一段）—— 两态 + 改坏法都写在每条断言里。
            Section("第 7 种载体收口（A773 抽屉文字 · A774 卡跟随视口节点 · A775 `MenuScroll.Place`）");

            // ---------- A774：卡池的卡 —— 裁切边界**沿父链解析**（不再由宿主算一个常量矩形）----------
            //  判据 = `ViewportClip.Resolve` 的契约（形参 `null` ⇒ 沿父链找最近的 `ViewportClip`）。
            //  量的东西 = **真卡池那张卡**（`RebuildCardsCells` 建的那一批）的渲染几何：
            //  把节点 `padding` 的 Top 拨成 60、再按生产那两行的形状重摆一次 ⇒ 卡的**最高顶点**必须
            //  跟着下移 60（= 边界真从节点上取，而不是宿主那个常量矩形）。
            //  改坏法：① 调用点退回 `v.SetPose(…, CardsViewport)`（显式矩形赢）⇒ 最高顶点停在 155.9 ⇒ 红；
            //    ② 删掉调用点那句 `v.SetClipFromTree()` ⇒ 整卡不裁 ⇒ 最高顶点跑到 ≈94.2（影子那 62px）⇒ 红；
            //    ③ 把 `CardView.ApplyClip` 里 `_clipFromTree` 那一支删掉 ⇒ 没有「重裁时重新解析」⇒ 态② 红。
            if (win.CardsScroll != null)
            {
                var cs774 = win.CardsScroll;
                var vp774 = CollectionWindow.CardsViewport;
                var page774 = win.PageRoot(1);
                var holder774 = FindChild(page774, "Scroll View");
                var vpNode774 = holder774 != null ? holder774.Find("Viewport") : null;
                var vc774 = vpNode774 != null ? vpNode774.GetComponent<ViewportClip>() : null;
                var card774 = FindChild(page774, "CollectionCard_0");
                var cv774 = card774 != null ? card774.GetComponent<CardView>() : null;
                CheckTrue(vc774 != null && cv774 != null,
                          "（A774 前提）卡池那颗 `ViewportClip`（`Scroll View/Viewport`）与第 1 张卡的 `CardView` 都取得到");
                if (vc774 != null && cv774 != null)
                {
                    CheckTrue(cv774.Clipped, "（A774 前提）起手这张卡**是裁着的**（调用点那句 `SetClipFromTree()` 解析到了节点）");
                    float savedOff774 = cs774.Offset;
                    var savedPad774 = vc774.padding;
                    var savedPos774 = card774.localPosition;
                    cs774.SetOffset(0f);                                       // 与 A181 那一段同一个起手（第一排压在上沿）
                    var r774 = cs774.Shift(CollectionWindow.CardsCellRect(0));
                    float sc774 = CollectionWindow.CardsCellH / (CardView.Height * 108f);
                    var pos774 = MainMenuSubmenuWindow.Local(vpNode774, r774.x1, r774.y1, r774.x2, r774.y2);
                    cv774.SetPose(pos774, 0f, sc774);                          // ← 生产那两行的形状

                    float a1, b1, c1, d1; int n1;
                    CheckTrue(RenderExtentPx(card774, out a1, out b1, out c1, out d1, out n1) && n1 >= 20,
                              $"（A774 前提）第 1 张卡量得到渲染几何（{n1} 个顶点；量到 0 这条就没验）");
                    CheckTrue(b1 >= vp774.y1 - 0.5f && b1 <= vp774.y1 + 2.5f,
                              $"★ A774 态①：`padding = 0` 时卡的裁切边界 = **节点框**（最高顶点 {b1:F1} ≈ 视口上沿 "
                              + $"{vp774.y1:F1}；⛔ 没裁的话影子会顶到 ≈94.2）");

                    vc774.padding = new Vector4(0f, 0f, 0f, 60f);              // Top 内缩 60（= 原版 `m_Padding.w`）
                    cv774.SetPose(pos774, 0f, sc774);                          // 同一次摆位 ⇒ 触发重裁
                    float a2, b2, c2, d2; int n2;
                    CheckTrue(RenderExtentPx(card774, out a2, out b2, out c2, out d2, out n2) && n2 >= 20,
                              $"★ A774 态②：**只改节点**的 `padding`、重摆一次 ⇒ 卡跟着下移（量得到几何 {n2} 个顶点）");
                    CheckTrue(b2 >= vp774.y1 + 59.5f,
                              $"★ …边界跟着 `padding` 走到 **{vp774.y1 + 60f:F1}**（最高顶点 {b2:F1}）—— "
                              + $"停在 {vp774.y1:F1} 就是「边界还在宿主那个常量矩形上」（= 第二状态源）");
                    CheckTrue(Mathf.Abs(b2 - b1) > 50f,
                              $"★ 两态的最高顶点差 **{Mathf.Abs(b2 - b1):F0}px** ⇒ 这条真能分出『跟节点』与『跟旧矩形』");

                    vc774.padding = savedPad774;                               // 还原节点
                    cv774.SetPose(pos774, 0f, sc774);
                    float a3, b3, c3, d3; int n3;
                    if (RenderExtentPx(card774, out a3, out b3, out c3, out d3, out n3))
                        CheckTrue(b3 >= vp774.y1 - 0.5f && b3 <= vp774.y1 + 2.5f,
                                  $"★ A774 态③：还原 `padding` ⇒ 边界**回到**节点框 {vp774.y1:F1}（{b3:F1}）"
                                  + " —— 读数随状态动、不是恒值（这一条挡「态② 那个 60 是碰巧量的」）");
                    cs774.SetOffset(savedOff774);
                    cv774.SetPose(savedPos774, 0f, sc774);                     // 还原（下一段还会重建整页）
                }
            }

            // ---------- A773：`ItemDrawer` 那个 `Price Display`（`TextCentered`）吃**视口节点** ----------
            //  病灶：`ItemDrawer.cs` 那句 `if (st.Clip.HasValue) MenuDraw.ClipText(lb, st.Clip.Value, …)`
            //  在**生产上恒假**（`ItemDrawerStyle.Clip` 缺省 `null`、三处派生写入已在甲块删掉）
            //  ⇒ 这两段字（`SetEphemeral` 的时长串 / `SetConverted` 的数量数字）**一个顶点都不裁**。
            //  本段**不碰任何窗**：自建探针（与 `Editor/RewardsScene.cs` 的 A302 探针同一个形状 —— 无父 `GameObject`），
            //  两态 = ① 探针上挂一颗 `ViewportClip` ② **不挂**（对照组）。
            //  🔴 **期望值不写死**：先量对照组的落点，再把节点框沿放在**它自己的中心** —— 这样
            //     「压边」这件事与 `Label.WorldW` 量出来是多少无关（⛔ 不拿我们自己的常量当尺子）。
            //  ⚠️ 前提与既有 px 断言同一条：批处理是 16:9（`VisibleWidth == DesignWidth`）。
            //  改坏法：把那句改回 `if (st.Clip.HasValue)` ⇒ 态① 的 `1234` 立刻和对照组一样越界 ⇒ 两条红。
            {
                var a773Box = new PxRect(1700f, 100f, 2600f, 1000f);           // 900×900 ⇒ 条高 90 ⇒ 字 ≈54px
                var a773St = ItemDrawerStyle.Default(10, 11, 12);              // ⚠️ `Clip` 保持缺省 `null`（= 生产那一档）
                a773St.ConvertedQuantity = 1234;                               // 画 `1234`（比单个数字宽，好压边）

                // 对照组：**不挂节点** ⇒ 这两段字一个顶点都不该被裁（= 迁移前/病灶那一态）
                var fx0 = new GameObject("A773 探针（无节点）").transform;
                ItemDrawer.SetConverted(fx0, a773Box, a773St);
                var drw0 = FindChild(fx0, ItemDrawer.NodeConvertedDrawer);
                var own0 = FindChild(drw0, ItemDrawer.NodeAlreadyOwned);
                var num0 = FindChild(FindChild(drw0, ItemDrawer.NodePriceDisplay), ItemDrawer.NodePriceText);
                float ox1 = 0f, oy1 = 0f, ox2 = 0f, oy2 = 0f, nx1 = 0f, ny1 = 0f, nx2 = 0f, ny2 = 0f;
                int ov0 = 0, nv0 = 0;
                bool okOwn0 = own0 != null && TextExtentPx(own0, out ox1, out oy1, out ox2, out oy2, out ov0) && ov0 > 0;
                bool okNum0 = num0 != null && TextExtentPx(num0, out nx1, out ny1, out nx2, out ny2, out nv0) && nv0 > 0;
                CheckTrue(okNum0, "（A773 前提）对照组：`Converted Drawer/Price Display/text` 那段数字建出来了、顶点量得到");
                CheckTrue(okOwn0, "（A773 前提）对照组：`Converted Drawer/AlreadyOwned` 那段说明字也建出来了");

                if (okNum0 && okOwn0)
                {
                    float a773Edge = (nx1 + nx2) * 0.5f;                       // 框沿 = 数字那段字的**实际中心**
                    var a773Vp = new PxRect(0f, 300f, a773Edge, 1080f);         // 只切 x 的右沿（两条带都在 y 内）
                    var fx1s = new GameObject("A773 探针（有节点）").transform;
                    // 🔴 **2026-10-14（#55/#56 · α）**：抽屉必须建在**视口节点【之下】**。
                    //   原来 `SetConverted` 的实参是 `fx1s` ⇒ `Converted Drawer` 与 `Viewport` 成了**兄弟**；
                    //   而 `ViewportClip.Resolve` / `FindAbove` **只沿父链往上走** ⇒ 兄弟上那颗节点**一次都没命中**
                    //   （自检里那两态实得值**逐位相同** ＝ 裁切一次都没发生）。
                    //   ⇒ 拿 `Hang` 的**返回节点**当父。（`MenuDraw.Node` 按**绝对设计矩形**落位 ⇒ 抽屉的位置不变，
                    //   变的只有父链 —— 这正是这两条要断的东西。）
                    //   判据 → `资料/普查产出_1013/D1013_诊断_块2_Shell与Collection.md` §16/§17。
                    var a773Vc = ViewportClip.Hang(fx1s, "Viewport", a773Vp, Vector4.zero, Vector2Int.zero);
                    ItemDrawer.SetConverted(a773Vc.transform, a773Box, a773St);
                    var drw1 = FindChild(fx1s, ItemDrawer.NodeConvertedDrawer);
                    var own1 = FindChild(drw1, ItemDrawer.NodeAlreadyOwned);
                    var num1 = FindChild(FindChild(drw1, ItemDrawer.NodePriceDisplay), ItemDrawer.NodePriceText);
                    float px1 = 0f, py1 = 0f, px2 = 0f, py2 = 0f, qx1 = 0f, qy1 = 0f, qx2 = 0f, qy2 = 0f;
                    int pv1 = 0, qv1 = 0;
                    bool okOwn1 = own1 != null && TextExtentPx(own1, out qx1, out qy1, out qx2, out qy2, out qv1) && qv1 > 0;
                    bool okNum1 = num1 != null && TextExtentPx(num1, out px1, out py1, out px2, out py2, out pv1) && pv1 > 0;
                    CheckTrue(okNum1 && okOwn1, "（A773 前提）有节点那一态：两段字都建出来了（粗筛 `VisibleAbove` 与框有交集）");

                    if (okNum1 && okOwn1)
                    {
                        // 态②（对照组）：**没有节点 ⇒ 不裁** —— 两段字都必须越过框沿（否则态① 那两条是空话）
                        CheckTrue(nx2 > a773Edge + 8f,
                                  $"★ A773 态②（对照）：**不挂节点**时数字越过框沿 {nx2:F1} > {a773Edge:F1}（框沿 = 它自己的中心）"
                                  + " —— 这条是态① 那两条的**前提**：几何本来就压在外面");
                        CheckTrue(ox2 > a773Edge + 8f,
                                  $"★ A773 态②（对照）：`Already Owned` 那段同样越过框沿（{ox2:F1} > {a773Edge:F1}）"
                                  + " —— 两条路都压边，后面才谈得上「行为一致」");

                        // 态①（有节点）：两段字都必须**被夹在框沿上**（≥ 贴边 = 真被截过，不是碰巧没到）
                        CheckTrue(px2 <= a773Edge + 0.5f,
                                  $"★ A773 态①：挂了 `ViewportClip` ⇒ **数量数字被截在框内**（最右顶点 {px2:F1} ≤ {a773Edge:F1}；"
                                  + $"⛔ 病灶态下它 = {nx2:F1}，压在视口边上整段画出去）");
                        CheckTrue(px2 >= a773Edge - 0.5f,
                                  $"★ …而且**确实被夹在框沿上**（{px2:F1} ≥ {a773Edge - 0.5f:F1}）—— "
                                  + "少了这一条，一条「本来就落在框内」的短文字也能过（假绿）");
                        CheckTrue(qx2 <= a773Edge + 0.5f && qx2 >= a773Edge - 0.5f,
                                  $"★ A773 态①：同一格里 `Already Owned` 那条路（`ClippedText`，甲块已改）**也夹在同一条框沿**"
                                  + $"（{qx2:F1} ≈ {a773Edge:F1}）⇒ 同一个函数里两条路**行为一致**");
                    }
                    Object.DestroyImmediate(fx1s.gameObject);
                }
                Object.DestroyImmediate(fx0.gameObject);
            }

            // ---------- A775：`MenuScroll.Place` 的框来源 = `ClipNode`（与 `Intersects` 同一档）----------
            //  ⚠️ **全仓零调用点**（`grep '\.Place('` 在 `Assets/` 下零命中、`git log -S` 也查不到 ⇒
            //  按「**没查到**」记）⇒ 只能**直调**验它。两态 = `ClipNode = null`（回落 `Viewport`）/ 接了节点。
            //  改坏法：把 `Place` 改回直读 `Viewport` ⇒ 态② 的 `x2` 变 1000 ⇒ 红。
            //  ⚠️ 本段与 `MenuScroll.cs` 的注释成对 —— 那里写了「为什么死代码也要补齐」。
            {
                var vpA775 = new PxRect(0f, 0f, 1000f, 500f);                  // 旧的 `Viewport` 那一份
                var vpB775 = new PxRect(0f, 0f, 600f, 500f);                   // 节点上那一份（更窄）
                var scA775 = new MenuScroll(vpA775, 0f, 2000f);
                var scB775 = new MenuScroll(vpA775, 0f, 2000f);
                var fx775 = new GameObject("A775 探针").transform;
                var vc775 = ViewportClip.Hang(fx775, "Viewport", vpB775, Vector4.zero, Vector2Int.zero);
                scB775.ClipNode = vc775;
                PxRect onA, onB; Rect uvA, uvB;
                bool okA = scA775.Place(new PxRect(0f, 100f, 2000f, 400f), out onA, out uvA);
                bool okB = scB775.Place(new PxRect(0f, 100f, 2000f, 400f), out onB, out uvB);
                CheckTrue(okA && okB, "（A775 前提）两态都算得出来（`Place` 返回 true）");
                CheckNear(onA.x2, 1000f, 0.5f,
                          "★ A775 态①：**没接节点** ⇒ 夹到 `Viewport.x2 = 1000`（= 迁移前的行为，逐位不变）");
                CheckNear(onB.x2, 600f, 0.5f,
                          "★ A775 态②：接了 `ClipNode` ⇒ 夹到**节点框** x2 = 600 —— ⛔ 不是 1000（那说明它还在直读 `Viewport`）");
                CheckNear(uvB.width, 0.3f, 0.005f,
                          "★ …而且 `uv` 跟着同一段比例截（0.3 = 600/2000）—— 只缩几何不截 uv 会把图压扁");
                CheckNear(onB.W, 600f, 0.5f, "★ …夹完的矩形宽 = 露出来的那一段");
                Object.DestroyImmediate(fx775.gameObject);
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
                // 🔴 **2026-10-18（第三轮整改 · 审查 P4）**：这颗钮的字从第三轮起**走词条**
                //   （`Shell/CollectionWindow` 的 `Loc.T("MainMenu/MainButtons/ButtonLabel/Back")`，
                //   键 = 原版那颗 `Button Text` 的 `mTerm` 原文）⇒ 顺手把它的**文案**也断上
                //   （原来这一节只断折行/框宽，一个字都没断 ⇒ 「字有没有接上词条」看不见）。
                CheckTrue(Loc.HasEntry("MainMenu/MainButtons/ButtonLabel/Back"),
                          "（前提）词条 `MainMenu/MainButtons/ButtonLabel/Back` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                Check(bl != null ? bl.Text : null, Loc.T("MainMenu/MainButtons/ButtonLabel/Back"),
                      "★ `Back` 那颗钮的**文案** = `Loc.T(\"MainMenu/MainButtons/ButtonLabel/Back\")`（随语档；"
                      + "原版 TMP 原文 `Back` / 中文「返回」`zh_CN.csv:5`）");
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
                    float lw = LabelRenderedPx(lb0).x;   // 🆕 A796′ 换口：TMP `textBounds`（旧口 = 缓存 `Label.WorldW`；null ⇒ 0）
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
                    //    不是看节点位置（判据同上面 Rarity 那条：`center − 渲出来的宽/2`；
                    //    🆕 **2026-10-16（A796′）换口**：那个「宽」改走 `LabelRenderedPx`（TMP `textBounds`），
                    //    不再读字段缓存 `Label.WorldW` —— 换口当天逐位同值，公式本身一个字没变）。
                    //    期望值 25.25 来自**原版 `Label` 的 rect 左缘**（`25.25,234.96→234.96,284.96`）。
                    var olab = FindChild(FindChild(fltPanel, "Cell_owned"), "Label");
                    var olb = olab != null ? olab.GetComponent<Label>() : null;
                    float owl = LabelRenderedPx(olb).x;   // 🆕 A796′ 换口（旧口 = 缓存 `Label.WorldW`；null ⇒ 0）
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
                // 🔴 **2026-10-18（A891 的续 · 续做 A）：四行小标题全走词条**（键 = 原版那颗 `Localize.mTerm`
                //   的原文，判据见 `Core/FilterPanelModel.cs` 的 `TitleTerm`）⇒ 期望值随语档。
                //   🔴🔴 **第三轮整改（审查 P1）就地订正（铁律 5）**：这一段原来写「`Energy Cost` 那一行
                //   原版**没有** `Localize`（全库 3 颗）⇒ 它恒英文」—— **那是错的**。错因 = **「全库」只扫了
                //   `bundle_menus_assets_all` 一个包**。复跑全部 80 个 bundle：`m_text == "Energy Cost"` 的 TMP
                //   **17 颗 / 15 个 bundle**，**每颗都挂 `Localize.mTerm = "Battle/Tips/EnergyCost"`**
                //   ⇒ 它是正经词条，已接上（本文件下面那条断言就是正面判据）。
                //   ⚠️ 上面那句「键在表里」是**必须的**：键不在 ⇒ `Loc.T` 返回键名本身、渲染那一侧同样返回
                //   键名 ⇒ 只比相等会**假绿**（两边一起退化）。
                CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army") && Loc.HasEntry("MenuDeck/HUD/Rarity")
                          && Loc.HasEntry("MenuDeck/Filters/Type") && Loc.HasEntry("Battle/Tips/EnergyCost"),
                          "（前提）四行小标题词条都在 `Loc` 表里（⛔ 有一条不在 ⇒ 下面四条两边一起退化成键名 = 假绿）");
                CheckText(TextOf(FindChild(fltPanel, "Title Army")), Loc.T("MenuDeck/Filters/Army"),
                          "小标题 `Army` 在（原版 `Title` TMP fs32；键 `MenuDeck/Filters/Army`，随语档；顶部视野内）");
                CheckText(TextOf(FindChild(fltPanel, "Title Rarity")), Loc.T("MenuDeck/HUD/Rarity"),
                          "小标题 `Rarity` 在（键 `MenuDeck/HUD/Rarity`，随语档；顶部视野内）");
                // 🔴 **左沿断言（4 条，2026-10-05 A93②）** —— 期望值是**原版的读数**，不是我们的常量：
                //   原版面板内左沿 = Army 行 **0** · 其余三行 **25**（`Collection Menu Variant` 的 Cards 页
                //   `Title` 实读 `0.3 / 25.3`、面板原点 `0.3`；卡组编辑那棵同族 `2.2 / 27.2`、原点 `2.2`）
                //   ⇒ 画布绝对 x = 面板原点 **0.25** + 那个数 = **0.25 / 25.25**（**写死字面量**，
                //   ⛔ 不拿 `CollectionWindow.FltL` + 模型常量去算 = 那是自证）。
                //   量的东西：**TMP 自己渲出来那块 `textBounds`**（`TitleLeftPx` → `LabelRenderedPx`）反推的**渲染左缘** ——
                //   🆕 **2026-10-16（A796′）换口**：换口前读的是 `Label.WorldW` 那份**字段缓存** `_tmpW`
                //   （两法同源 ⇒ 换口当天逐位同值；期望值与容差**一字未动**）。
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
                    // 🔴 2026-10-18（A891 的续 · 续做 A）：`Energy Cost` / `Type` 都走词条（随语档）。
                    //   ⚠️ **第三轮整改（审查 P1）**：`Energy Cost` 原来这条**把「原版无词条」钉成了检验标准**
                    //   —— 是最坏的一档（下一个人要接它时会先撞红）。现改为正面判据。
                    CheckText(TextOf(FindChild(fltPanel, "Title Energy Cost")), Loc.T("Battle/Tips/EnergyCost"),
                              "小标题 `Energy Cost` 在（键 `Battle/Tips/EnergyCost`，随语档；滚到底之后才够得着）");
                    CheckText(TextOf(FindChild(fltPanel, "Title Type")), Loc.T("MenuDeck/Filters/Type"),
                              "小标题 `Type` 在（键 `MenuDeck/Filters/Type`，随语档；滚到底之后才够得着）");
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
                    // 🔴 **2026-10-18（A891 的续）改「随语档」**：占位符从本批起走词条
                    //   （`Core/FilterPanelModel.InputPlaceholder` → `Loc.T("MenuDeck/HUD/SearchFilter")`）
                    //   ⇒ 宿主跑中文档时写死的 `"Search"` 必红。原文（`Search`）留在 `Loc` 的英文列里
                    //   （= 原版 `Placeholder` 那颗 TMP 的 `m_text` 原文）。
                    CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),
                              "（前提）词条 `MenuDeck/HUD/SearchFilter` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                    CheckText(TextOf(nf), Loc.T("MenuDeck/HUD/SearchFilter"),
                              "清空后搜索框回到占位符（键 `MenuDeck/HUD/SearchFilter`，随语档；"
                              + "**实拍抓出来的那条**：光看状态量不到）");
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
                var qArt0 = art != null ? art.GetComponentInChildren<ImageQuad>() : null;
                var tex0 = qArt0 != null ? qArt0.Texture as Texture2D : null;
                // 🔴 **2026-10-18（A994③）就地订正（铁律 5）**：这两条原来写 **250 / 405**（=「铺满格」）——
                //    **错了**：原版那颗 `Cardback` 的 `Image` 带 **`m_PreserveAspect = 1`**
                //    （A4 §2·1 那行「Simple **preserveAspect**」）⇒ 实绘 **≠ 拉满**。
                // 🔴 **2026-10-19（B2）就地订正（铁律 5）**：期望值原来用 `InsetFit`（= 按**贴图自己**的比例内接）
                //    —— 那只是 uGUI 的**半套**。原版是**两段**：① 按 sprite 的 **`m_Rect`（707×1020）定框**；
                //    ② 贴图再按 **`textureRect/m_Rect` 缩** ⇒ **宽也会缩**（⛔ 不是「宽恒 250」）。
                //    算法/判据全文 → `Core/CardbackFace.cs`；期望值现算走 `CardbackDrawnPx`
                //    （⛔ 不调被测实现 `CardbackFace.Fit` / `PreserveAspectSize` —— 那是自证）。
                CheckTrue(tex0 != null, "（前提）第 1 格的卡背有贴图（`Texture` 取不到 ⇒ 下面两条无从算起）");
                if (tex0 != null)
                {
                    var fit0 = CardbackDrawnPx(tex0, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH);
                    CheckNear(Wpx(art), fit0.x, 0.6f,
                              $"格里的卡背宽 = **{fit0.x:F2}**（= 框宽 250 × `textureRect`/`m_Rect` 的**宽那一半**；"
                            + $"贴图 {tex0.width}×{tex0.height} ⇒ 贴图宽/707 = {tex0.width / 707f:F4}。"
                            + $"⛔ 「宽恒 250」是 B2 之前那一版（只做了第一段））");
                    CheckNear(Hpx(art), fit0.y, 0.6f,
                              $"格里的卡背高 = **{fit0.y:F2}**（= (250×1020/707) × 贴图高/1020；"
                            + "⛔ 不是 **405** —— 405 是**框**高、也不是「仅按贴图比例内接」那一档）");
                    // 🧨 **改坏法**：把 `RebuildCosmoCells` 里卡背那一跳的 `keepAspect` 改回 `false`
                    //    （或退回直调 `ImageQuad.Create` + `SetAspect(250/405)`）⇒ 上面两条立刻红（高变回 405）。
                    // 🆕 **2026-10-19（B2）**：画心**不居中** —— 原版按 sprite 的 `padding` 偏（uGUI 第二段的另一半）。
                    //   偏移 = `((padL−padR)/2, (padB−padT)/2) × (框宽/707)`；uGUI 的 y **向上** ⇒ 本仓画布
                    //   （y 向下）要**反号**。期望值走 `CardbackOffsetPx`（吃原版 `padding` 数据、独立复述算式）。
                    float odx, ody;
                    CheckTrue(CardbackOffsetPx(tex0, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH,
                                               out odx, out ody),
                              $"（前提）`{tex0.name}` 在 `Resources/Cardbacks.json` 里有 `padL/padR/padB/padT` 四列"
                            + "（表是旧版就没有 ⇒ 下面两条**空转**；跑 `python d:/4/Unity/工具/gen_cardbacks.py` 重生成）");
                    if (CardbackOffsetPx(tex0, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH, out odx, out ody))
                    {
                        // 第 1 格**整块在视口里**（上面 Wpx/Hpx 两条已经依赖这一点）⇒ 渲染中心 == 框中心 + 偏移
                        CheckNear(PxOf(qArt0.transform.position.x) - PxOf(k0.position.x), odx, 0.6f,
                                  $"★ 卡背格里的画心**不居中**：横偏移 = **{odx:F2}**px"
                                + "（= (padL−padR)/2 × 250/707 ⇒ 没做这一半时恒为 **0**）");
                        CheckNear(PxYOf(qArt0.transform.position.y) - PxYOf(k0.position.y), ody, 0.6f,
                                  $"★ ……纵偏移 = **{ody:F2}**px（= −(padB−padT)/2 × 250/707 —— **反号那一处**："
                                + "uGUI 的 y 向上、本仓画布 y 向下；没做这一半时恒为 **0**）");
                    }
                }
                CheckTrue(art != null && art.GetComponentInChildren<ImageQuad>() != null
                          && art.GetComponentInChildren<ImageQuad>().Texture != null,
                          "卡背**真的有贴图**（不是空图 —— 导没导错就看这一条）");

                // ---- 🆕 2026-09-26：卡背底下那层 **SDF**（原版 `Cardback Shadow SDF`）----
                // 逐值出处：`资料/普查产出_0923/A4_装饰页与驱动链.md:91` ——
                //   rect **-42.5,-70.87,337.5,550.8**（格式是 `x,y,w,h`，y 向下相对格左上）·
                //   锚点 (-0.17,-0.185)-(1.18,1.175) + sizeDelta (0,0)（拉伸）· `act=T` ·
                //   组件 = Image + `UIImageMaterialColorChanger`（换色即悬停高亮）·
                //   🔴 **`m_PreserveAspect = 1`**（同表那列写着「Simple preserveAspect」—— `A994③` 起照它做）。
                // 🔴 下面那条框的两维 + 底边（相对格上沿）= 上面那串字面量的直接换算，**独立于被测实现**：
                const float SdfBoxW = 337.5f, SdfBoxH = 550.8f, SdfBoxBottomRel = 479.925f;
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
                        var stx0 = sq0 != null ? sq0.Texture as Texture2D : null;
                        // 🔴 **2026-10-18（A994③）**：SDF 那层也带 `m_PreserveAspect = 1`
                        //    ⇒ 先把 337.5×550.8 的**框**按贴图比例内接（实测 `_SDF` 全部 100×130
                        //    ⇒ 337.5×438.75、上下各内缩 `pad`），**再**与视口求交。见 `RebuildCosmoCells`。
                        float sSdf = stx0 != null ? stx0.width / (float)stx0.height : 130f / 100f;
                        var sdfFit0 = InsetFit(sSdf, SdfBoxW, SdfBoxH);
                        float sdfPad0 = (SdfBoxH - sdfFit0.y) * 0.5f;      // 上下各内缩（= 56.025）
                        float sdfBottom0 = SdfBoxBottomRel - sdfPad0;      // 相对格上沿的**底边**
                        float sh0 = sq0 != null ? sq0.WorldH * 108f : -1f;
                        float st0 = sq0 != null ? PxYOf(sq0.transform.position.y) - sh0 * 0.5f : -1f;
                        // 第一排那一格的**格上沿 == 视口上沿** ⇒ 内接后整体仍 `-14.85` 上溢（< 0）
                        // ⇒ 露出来的就是 `sdfBottom0` 那一段（= 423.9；原来是拉满时的 479.925）。
                        CheckNear(sh0, sdfBottom0, 1.5f,
                                  $"★ 第一排那一格的 SDF **上溢被视口截住**：渲染高 = **{sdfBottom0:F3}**"
                                + $"（= 格上沿 + {sdfBottom0:F3}；原版框高 550.8 里「上溢 70.875 + 内接内缩 {sdfPad0:F3}」"
                                + "那两段都画不出来 —— 前一段是 `RectMask2D`，后一段是 `m_PreserveAspect = 1`）");
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
                // ---- 对照（🆕 2026-10-08）：**整块落在视口里**的那一格 ⇒「渲染矩形 == 内接矩形」----
                //  上面那一格（`k0`）只能证明「上溢被截住」，量不到内接矩形本身 ⇒ 判据要挪到一格**四边都不越界**的：
                //  第 2 排第 2 列（`CosmoCells[CosmoCols + 1]`，`CosmoCellRect` 是**行优先**、视口外的格不建）：
                //    · SDF **框**相对格左上 = `−42.5, −70.875 → +295, +479.925`（= 宽 337.5 / 高 550.8）；
                //      🔴 `A994③` 起框内还要**按贴图比例内接**：337.5×438.75、上下各内缩 56.025
                //      ⇒ 实绘相对格左上 = `−42.5, −14.85 → +295, +423.9`；
                //    · 该格上沿 = `CosmoView.y1` 155.94 + 格高 405 = **560.94** ⇒ SDF 上沿 **546.09** > 视口上沿 ✓；
                //      左沿 = 335.44 + `CosmoPadX` 42.285 + 250 = **627.725** ⇒ SDF 左沿 **585.225** > 视口左沿 335.44 ✓；
                //      下沿 560.94 + 423.9 = **984.84** < 1080 ✓ · 右沿 627.725 + 295 = **922.725** < 1920.01 ✓。
                //  **改坏法**：把 `RebuildCosmoCells` 里那句 `sr` 的 337.5 / 550.8 改错 ⇒ 红；
                //   **把 SDF 那一跳的 `keepAspect` 改回 `false`（= 拉满）⇒ 高变回 550.8 ⇒ 红**。
                {
                    var sdfFull = win.CosmoCells.Count > CollectionWindow.CosmoCols + 1
                        ? FindChild(win.CosmoCells[CollectionWindow.CosmoCols + 1], "Cardback Shadow SDF") : null;
                    CheckTrue(sdfFull != null,
                              "（对照 · 前提）第 2 排那一格的 `Cardback Shadow SDF` 也在（它整块落在视口里 ⇒ 量得到实绘矩形）");
                    if (sdfFull != null)
                    {
                        var qSf = sdfFull.GetComponentInChildren<ImageQuad>();
                        var tSf = qSf != null ? qSf.Texture as Texture2D : null;
                        CheckTrue(tSf != null, "（对照 · 前提）那一格的 SDF 贴图取得到（100×130.5 那批）");
                        var fitSf = InsetFit(tSf != null ? tSf.width / (float)tSf.height : 130f / 100f,
                                             SdfBoxW, SdfBoxH);
                        CheckNear(Wpx(sdfFull), fitSf.x, 1.5f,
                                  $"★ 对照（整块不越界的格）：SDF 层宽 = **{fitSf.x:F2}**（= 框宽 337.5 —— 贴图比框「宽」"
                                + "⇒ 宽定、宽**不变**；比 250 的卡背大一圈 —— 露出来的就是落地感）");
                        CheckNear(Hpx(sdfFull), fitSf.y, 1.5f,
                                  $"★ 对照（整块不越界的格）：SDF 层高 = **{fitSf.y:F2}**（⛔ **不是 550.8** —— 那是**框**高；"
                                + "`m_PreserveAspect = 1` ⇒ 高缩成 `337.5 ÷ 比例`。"
                                + "🔴 **反证**：拉满时 x 缩放 337.5/100 = **3.375** 而 y 缩放 550.8/130 = **4.237**（**非等比**、"
                                + "距离场被竖向拉长）；内接后两轴**都是 3.375**）");
                    }
                }
                // ---- 🔴 2026-10-18（A994③）：**逐格**验「两层都没被拉伸」（原版两颗 `Image` 都带 `m_PreserveAspect = 1`）----
                //  判据 = **渲染宽高比 == 贴图自己的宽高比**（uGUI `Image` 的直接后果；
                //  「图没被拉伸」这件事与「我们怎么摆框」无关 ⇒ 它比「量宽/高」更靠近原版语义）。
                //  ⚠️ **2026-10-19（B2）复核**：两段式之后渲染比例 = **`textureRect` 的比例**，而 `textureRect`
                //     与「我们那张裁片 PNG」的比例实测最大只差 **1.42e-4**（233 张全量现读）⇒ 这条的 **2e-3 容差照旧成立**，
                //     **一个字不用改**（它是「没被拉伸」的看门人，与「该多大」是两件事）。
                //  ⛔ **不写死格号**：233 张比例跨 **0.6188~0.7652**，每格偏离量都不同 —— 写死一格就是赌假绿。
                //  做法 = ① 全量扫（可量的格逐格比比例）② 按**偏离量**挑「两段式 ≠ 老口径」差得**最远**的那一张做精确断言。
                //  被视口切过的格（滚动到边上那几格）渲染矩形本来就是「裁剩那块」的比 ⇒ 跳过（归 A181 那组管）。
                {
                    var vpA = CollectionWindow.CosmoView;
                    int cellsOk = 0, stretched = 0, worstI = -1; float worstDev = -1f;
                    foreach (var cn in win.CosmoCells)
                    {
                        if (cn == null || !cn.name.StartsWith("CollectionCosmetic_")) continue;
                        int ci;
                        if (!int.TryParse(cn.name.Substring("CollectionCosmetic_".Length), out ci)) continue;
                        var ca = FindChild(cn, "Cardback");
                        var cq2 = ca != null ? ca.GetComponentInChildren<ImageQuad>() : null;
                        var ct = cq2 != null ? cq2.Texture as Texture2D : null;
                        if (ct == null || ct.height <= 0) continue;
                        var lr = win.CosmoScroll != null
                            ? win.CosmoScroll.Shift(CollectionWindow.CosmoCellRect(ci))
                            : CollectionWindow.CosmoCellRect(ci);
                        if (lr.x1 < vpA.x1 - 0.5f || lr.x2 > vpA.x2 + 0.5f
                            || lr.y1 < vpA.y1 - 0.5f || lr.y2 > vpA.y2 + 0.5f) continue;   // 被视口切过 ⇒ 跳过
                        cellsOk++;
                        float sc = ct.width / (float)ct.height;
                        float rr = (cq2.WorldW * 108f) / Mathf.Max(1e-6f, cq2.WorldH * 108f);
                        // 🔴 **这一条对旧写法（一律 `SetAspect(250/405)` = 0.61728）必红** ——
                        //    本仓 233 张的比例**全都 > 0.61728**（最小 0.6188 ≥ 0.61728 + 0.0015），一张都对不上。
                        if (Mathf.Abs(rr - sc) > 2e-3f) stretched++;
                        // 🔴 **2026-10-19（B2）改挑法**：原来按「实绘高离框高 405 多远」挑 —— 那是**老口径**
                        //    （`250 ÷ 贴图比例`）的度量。两段式之后「高」的极值不再贴着 405
                        //    （最大 = `250 × 最大 texRectH / 707` = **360.68**），挑出来那一格**未必**是
                        //    「新老两态差得最远」的 ⇒ 下面的精确断言会**看着绿其实空转**。
                        //    改成按**宽**挑：两段式的画心宽 = `250 × texRectW/707`，而老口径/拉伸**恒 250**
                        //    ⇒ **贴图越窄、两态差得越远**（`texRectW` 用资产自己的 `ct.width` 顶，差 ≤0.5px）。
                        float dv = 250f - 250f * ct.width / 707f;
                        if (dv > worstDev) { worstDev = dv; worstI = ci; }
                    }
                    CheckTrue(cellsOk >= 6,
                              $"（前提）有 {cellsOk} 格**整块落在视口里**、可以量实绘矩形（⛔ < 6 格则下面两条等于没验）");
                    Check(stretched, 0,
                          $"★ **卡背格逐格比「渲染宽高比 vs 贴图自己的宽高比」**（可量 {cellsOk} 格）"
                        + " —— 一格不等就是**拉伸**（原版 `Cardback` 那颗 `Image` 带 `m_PreserveAspect = 1`）");
                    CheckTrue(worstI >= 0 && worstDev > 5f,
                              $"（前提）挑得到一格「两段式 ≠ 老口径（宽恒 250）」差 > 5px 的探针 —— 实得 {worstDev:F2}px"
                            + "（可量那几格里最小的贴图宽 < 692 ⇒ 否则下面两条**空转**，⛔ 别放它过去）");
                    if (worstI >= 0)
                    {
                        var wcell = FindChild(cpage, "CollectionCosmetic_" + worstI);
                        var wart = wcell != null ? FindChild(wcell, "Cardback") : null;
                        var wq = wart != null ? wart.GetComponentInChildren<ImageQuad>() : null;
                        var wt = wq != null ? wq.Texture as Texture2D : null;
                        CheckTrue(wt != null, "（前提）按偏离量挑出来的那一格贴图取得到");
                        if (wt != null)
                        {
                            var fitW = CardbackDrawnPx(wt, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH);
                            CheckNear(Wpx(wart), fitW.x, 0.6f,
                                      $"★ 两态差得最远的那一格（`{wt.name}` · 第 {worstI + 1} 张 · 贴图 {wt.width}×{wt.height}）"
                                    + $"画出来的**宽** = **{fitW.x:F2}**（= 250 × {wt.width}/707；"
                                    + $"**老口径 / 拉满时恒是 250**，差 {250f - fitW.x:F2}px）");
                            CheckNear(Hpx(wart), fitW.y, 0.6f,
                                      $"★ ……**高** = **{fitW.y:F2}**（= (250×1020/707) × {wt.height}/1020；"
                                    + $"**拉满时是 {CollectionWindow.CosmoCellH}**，差 "
                                    + $"{CollectionWindow.CosmoCellH - fitW.y:F2}px）");
                            // 🔴 **2026-10-19（B2）补第二条灭自证**：老口径（只做第一段 / 拉伸）时
                            //    **宽恒 == 250**⇒ 这一条与上面那条宽断言**结构上不可能同时绿**。
                            CheckTrue(Wpx(wart) < CollectionWindow.CosmoCellW - 5f,
                                      $"★ 灭自证：画心宽 {Wpx(wart):F2} **严格小于**框宽 {CollectionWindow.CosmoCellW}"
                                    + $"（实得 {Wpx(wart):F2}，差 {CollectionWindow.CosmoCellW - Wpx(wart):F2}px）"
                                    + " —— 老口径/拉满时它**恒等于**框宽，两条不可能同时绿");
                            // 🧨 **灭自证**：只要退回「拉满」（`keepAspect: false` / `SetAspect(250/405)`），
                            //    「画出来的高」就**恒 == `CosmoCellH`** ⇒ 这一条与上一条**不可能同时绿**
                            //    （挡的是「把实现和期望值一起改回旧写法」：期望值是**从贴图资产现算**的，改不回去）。
                            CheckTrue(Hpx(wart) < CollectionWindow.CosmoCellH - 10f,
                                      $"★ 灭自证：「两段式后的高」**严格小于框高 {CollectionWindow.CosmoCellH}**"
                                    + $"（实得 {Hpx(wart):F2}，差 {CollectionWindow.CosmoCellH - Hpx(wart):F2}px）"
                                    + " —— 拉满时它**恒等于**框高，两条不可能同时绿");
                        }
                    }
                }
            }
            // 页头（A3 那条「每页自己的实例值」：本页 35/33，**异画页是 42**）
            // 🔴 **2026-10-18（第三轮整改 · 审查 P4）改「随语档」**：本页页头标题从第三轮起走词条
            //   （`Shell/CollectionWindow.BuildCosmeticsPage` 的 `Loc.T("MenuCollection/Label/Cosmetics")`，
            //   键 = 原版那颗 `label` 的 `mTerm` 原文）⇒ 宿主跑中文档时写死的 `"Your cosmetics collection"` 必红。
            CheckTrue(Loc.HasEntry("MenuCollection/Label/Cosmetics"),
                      "（前提）词条 `MenuCollection/Label/Cosmetics` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
            CheckText(TextOf(FindChild(cpage, "Header Label")), Loc.T("MenuCollection/Label/Cosmetics"),
                      "页头标题 = `Loc.T(\"MenuCollection/Label/Cosmetics\")`（随语档；"
                      + "原版 TMP 原文 `Your cosmetics collection` · 原版 fs38）");
            // 🔴 **2026-10-18（A891 的续 · 续做 B）改「随语档」**：页头那颗字从本批起走词条
            //   （`Shell/CollectionWindow.BuildFilterHeader` 的 `Loc.T("MenuDeck/Filters/Filters")` —— 与
            //   卡组编辑窗页头那颗**同一条键**）⇒ 宿主跑中文档时写死的 `"Filters"` 必红。
            //   原文 = 原版那颗 TMP 的 `m_text`（`Filters`），留在 `Loc` 的英文列里（中文列 = 原版实拍「过滤器」）。
            CheckTrue(Loc.HasEntry("MenuDeck/Filters/Filters"),
                      "（前提）词条 `MenuDeck/Filters/Filters` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
            CheckText(TextOf(FindChild(cpage, "Filters Label")), Loc.T("MenuDeck/Filters/Filters"),
                      "页头 `Filters` 文案在（键 `MenuDeck/Filters/Filters`，随语档；"
                      + "原版 **fs35** auto 10-35 —— ⚠️ 异画页同名的是 42）");
            // 🔴 **2026-10-18（A891 的续）改「随语档」**：页头那颗字从本批起走词条
            //   （`Shell/CollectionWindow.BuildFilterHeader` 的 `Loc.T("MenuDeck/Filters/ClearFilters")`）
            //   ⇒ 宿主跑中文档时写死的 `"Clear filters"` 必红。原文 = 原版那颗 TMP 的 `m_text`（`Clear filters`），
            //   留在 `Loc` 的英文列里。⛔ 下面找底图那句（节点名 `"Clear filters"`）**不动**。
            CheckTrue(Loc.HasEntry("MenuDeck/Filters/ClearFilters"),
                      "（前提）词条 `MenuDeck/Filters/ClearFilters` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
            CheckText(TextOf(FindChild(cpage, "Clear filters Text")), Loc.T("MenuDeck/Filters/ClearFilters"),
                      "页头 `Clear filters` 文案在（键 `MenuDeck/Filters/ClearFilters`，随语档；"
                      + "原版 **fs33** auto 10-33）");
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
                    // 🆕 **2026-10-16（A796′）换口**：半宽（`WorldW × 54`）→ `LabelRenderedPx`（TMP `textBounds`）；
                    //    中心仍是**节点位置**（只换「从哪个口读那个数」）。
                    float tLeft = PxOf(lt.transform.position.x) - LabelRenderedPx(lt).x * 0.5f;
                    float cRight = PxOf(lc2.transform.position.x) + LabelRenderedPx(lc2).x * 0.5f;
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
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：这颗字从波 1b（P1）起走词条
                    //   （`Core/FilterPanelModel.cs` 的 `BuildCosmetics` 那颗 `Owned` 格，键 = `MenuDeck/Filters/ShowOwnedOnly`）
                    //   ⇒ 原来写死的 `"Owned only"` 只是**英文列的副本**，
                    //   宿主跑中文档时它印的是中文列 ⇒ **本条必红（这就是本轮那条红）**。
                    //   期望值改成 `Loc.T(键)` ⇒ 两语档都成立。
                    //   ⚠️ **2026-10-18 收口（铁律 5）**：本段原来引的是实现里那两个符号名
                    //     （`TermOr(OwnedOnlyTerm, "Owned only")`）—— 那道**取词闸门已删除**（键全在表里，
                    //     闸门是静默兜底）⇒ 实现现在是**裸 `Loc.T(OwnedOnlyTerm)`**，本段改成不引那个已不存在的符号。
                    //     ⚠️ 断言本身**一个字没动**（`Loc.T(键)` 那句两版都成立 —— 键在表 ⇒ 闸门与裸调用同值）。
                    //   ⚠️ 上面那条 `HasEntry` **仍然不是装饰**：键若不在表里，`Loc.T` 回的是**键名本身**
                    //      ⇒ 实现印键名、期望也是键名，这条会**假绿**（看不出「词条丢了」）—— 它挡的是这个。
                    CheckTrue(Loc.HasEntry("MenuDeck/Filters/ShowOwnedOnly"),
                              "（前提）词条 `MenuDeck/Filters/ShowOwnedOnly` 在表里"
                            + "（⛔ 不在 ⇒ 下一条红得看不出原因）");
                    CheckText(TextOf(FindChild(co, "Label")), Loc.T("MenuDeck/Filters/ShowOwnedOnly"),
                              "`Owned` 的标签文案（**随语档** —— 键 `MenuDeck/Filters/ShowOwnedOnly`，"
                            + "英文列 = 原版那颗 TMP 的 `m_text` `Owned only`）");
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
            //  （左 −42.5 / 右 +45 / 🔴 `A994③` 起上下按贴图比例内接 ⇒ **上 −14.85 / 下 +18.9**，
            //   原来是拉满时的 上 −70.875 / 下 +74.925；见 `RebuildCosmoCells` 那段逐值出处）⇒ 压边那几格
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
                // 对照：没被切的那一排（第 2 排）⇒ 仍是**内接矩形本身**（没被视口一起压扁）
                //  🔴 **2026-10-18（A994③）**：期望值从「整格高 405」改成**按该格贴图现算的内接高**；
                //    405 是**框**高（= 拉满），而原版那颗 `Image` 带 `m_PreserveAspect = 1`。
                //  🔴 **2026-10-19（B2）**：现算那一步从 `InsetFit`（只做第一段）换成 **`CardbackDrawnPx`**
                //    （两段式：先按 `m_Rect` 定框、再按 `textureRect/m_Rect` 缩）—— 口径/判据 → `Core/CardbackFace.cs`。
                var cfull = FindChild(cpage, "CollectionCosmetic_" + CollectionWindow.CosmoCols);
                if (cfull != null)
                {
                    var cq = FindChild(cfull, "Cardback");
                    var cqq = cq != null ? cq.GetComponentInChildren<ImageQuad>() : null;
                    var ctq = cqq != null ? cqq.Texture as Texture2D : null;
                    var fitF = ctq != null
                        ? CardbackDrawnPx(ctq, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH)
                        : new Vector2(CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH);
                    CheckNear(cqq != null ? cqq.WorldH * 108f : -1f, fitF.y, 1.5f,
                              $"对照：没被切的那一排卡背 = **本格自己两段式后的高 {fitF.y:F2}**（实测 "
                            + $"{(cqq != null ? cqq.WorldH * 108f : -1f):F1}）—— 只在越界时裁、不是一律压扁；"
                            + $"也**不是框高 {CollectionWindow.CosmoCellH}**（两段式之后高只可能 ≤ 360.68）");
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
                        // ============================================================ 🔴 2026-10-13（A404）
                        // 计数条那三颗 TMP 的**自适应**（原版三颗都带 `auto`）。判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92`
                        // （§三 表）+ 逐颗现读原版 MB（`bundle_scenes_scenes_mainmenuwarpforge`）：
                        //   `…/Counters/Counter`      `fs52.6`(auto **25–52.6**) sd(**73.283**,70.586) 折行 **0**（MB 1892）
                        //   `…/Counters/Slash`        `fs57`  (auto **25–57**)   sd(**28.02**,70.586)  折行 **0**（MB 1848）
                        //   `…/Slash/Duplicates text` `fs52.6`(auto **18–52.6**) sd(**44.09**,50)      折行 **0**（MB 1823）
                        //   三颗都是 `m_enableAutoSizing = 1` + `m_TextWrappingMode = 0`。
                        // 🔴 期望值**全是原版值**（⛔ 不写我们那一侧传进去的实参）：容器宽 = 原版 `m_SizeDelta.x`、
                        //   min/max = 原版 `m_fontSizeMin/Max` 的 **px 口径**（`Label.FontSizeToPx`）、折行档 = 原版 0。
                        // 🔴 **为什么必须断「渲染宽/高 ≤ 框」**（`CLAUDE.md` §二 `AutoFitBox` 那条教训）：
                        //   只比字号的话，「字号字段对、字却溢出框」照样全绿；而这三颗**正是靠自适应收缩**的
                        //   （`Duplicates text` 的框高 **50 < 字号 52.6**，正本 §三 末那条注）。量的东西 =
                        //   **TMP 自己渲出来那块 `textBounds`**（🆕 **2026-10-16（A796′）换口** → `LabelRenderedPx`；
                        //   换口前是 `Label.WorldW/WorldH` = 字段缓存 `_tmpW/_tmpH`，两法同源 ⇒ 当天逐位同值）——
                        //   ⛔ 不是节点位置、⛔ 更不是「我们传了多少」。
                        // **改坏法**：① 三处 `wrapPx` 改回 `0f` ⇒ 自适应那一段整段不执行（它写在 `if (wrapPx > 0f)` 里）
                        //   ⇒ `AutoSizing` 恒 false、`fontSizeMin/Max` 停在 TMP 出厂 `0/0`、TMP 容器宽停在出厂值 ⇒ 红；
                        //   ② `autoMinPx` 传错（如 25 → 10）⇒ `FontSizeMin` 那条红；③ 删掉 `SetWrapping(false)` ⇒ 折行档回 1 ⇒ 红。
                        // ⚠️ **如实说清判别力**（实测过这三颗的文案）：咬住 ① 的是 `AutoSizing` / `fontSizeMin/Max` /
                        //   **TMP 容器宽**那 **12 条**（字段级、唯一判别式）。宽/高那两条**对 ① 不一定有判别力** ——
                        //   本夹具的文案是 `x2`（`cap = 2`）/ `/ ` / **一位数**（`spares = 9`：
                        //   `NeedCopies[4] − NeedCopies[1] = 9 − 0`，见 `CardDetailPopup.NeedCopies`），
                        //   按 52.6 / 57px **不缩**大概也塞得进 73.28 / 28.02 / 44.09 这三个框
                        //   （⚠️ 那三个字的墨宽**没在实机上量过**，这一句是估的）⇒ 那两条是**另一族**
                        //   （「字号对而溢出」）的守卫，**不是 A404 的判别式**（同 `Editor/RewardsScene.cs` 里记过的同一件事）。
                        {
                            var fitCases = new[]
                            {
                                // 节点名 · 原版容器宽 `sd.x` · 原版容器高 `sd.y` · 原版 auto 的 min/max（px）
                                new { Name = "Counter",         W = 73.283f, H = 70.586f, Min = 25f, Max = 52.6f },
                                new { Name = "Slash",           W = 28.02f,  H = 70.586f, Min = 25f, Max = 57f   },
                                new { Name = "Duplicates text", W = 44.09f,  H = 50f,     Min = 18f, Max = 52.6f },
                            };
                            foreach (var fc in fitCases)
                            {
                                var ft = FindChild(cnt, fc.Name);
                                var flb = ft != null ? ft.GetComponentInChildren<Label>() : null;
                                CheckTrue(flb != null, $"`{fc.Name}` 上找得到 `Label`（A404 要量它的自适应）");
                                if (flb == null) continue;
                                var ftmp = ft.GetComponentInChildren<TMPro.TextMeshPro>(true);
                                CheckTrue(flb.AutoSizing,
                                          $"★ `{fc.Name}` **真的开了自适应**（原版 `m_enableAutoSizing = 1`）"
                                        + " —— 改坏法：`CardDetailPopup` 那三处 `wrapPx` 传回 `0f` ⇒ 这一段永不执行、本行红");
                                CheckNear(Label.FontSizeToPx(flb.FontSizeMin), fc.Min, 0.05f,
                                          $"`{fc.Name}` 自适应下限 = 原版 `m_fontSizeMin` **{fc.Min:F1}px**");
                                CheckNear(Label.FontSizeToPx(flb.FontSizeMax), fc.Max, 0.05f,
                                          $"`{fc.Name}` 自适应上限 = 原版 `m_fontSizeMax` **{fc.Max:F1}px**"
                                        + "（= 原版 `m_fontSize`；⛔ 不是我们那一侧的字号常量）");
                                Check(flb.WrappingMode, 0,
                                      $"`{fc.Name}` 折行档 = **原版 `m_TextWrappingMode = 0`**（`NoWrap`）"
                                    + " —— 改坏法：删掉紧跟 `MenuDraw.Text` 的那句 `SetWrapping(false)` ⇒ 回 1");
                                CheckNear(ftmp != null ? ftmp.rectTransform.sizeDelta.x : -1f, LayoutSpace.Px(fc.W),
                                          2e-4f, $"`{fc.Name}` 的 **TMP 容器宽 = 原版 `m_SizeDelta.x`**（{fc.W:F3}px）"
                                               + " —— 改坏法：`wrapPx` 传 `0f` ⇒ 容器停在出厂值");
                                // 🆕 **2026-10-16（A796′）换口**：宽/高改走 `LabelRenderedPx`（TMP `textBounds`）
                                //    —— ⛔ 不再读**字段缓存** `Label.WorldW/H`（就是 `AutoFitBox` 教训里那个口）。
                                var flbSz = LabelRenderedPx(flb);
                                float rw = flbSz.x, rh = flbSz.y;
                                CheckTrue(rw <= fc.W + 1.5f && rh <= fc.H + 1.5f,
                                          $"`{fc.Name}` **渲出来 {rw:F1}×{rh:F1}px ≤ 原版框 {fc.W:F2}×{fc.H:F2}**"
                                        + "（容差 1.5px = TMP 二分收敛粒度 `TextMeshPro.cs:4139`）"
                                        + " —— 只比字号会漏掉「字号对而溢出」那一族（`AutoFitBox` 教训）");
                            }
                        }
                        // ============================================================ 🔴 2026-10-13（A574 + A575）
                        // 计数条那三颗 TMP 的 **`m_fontSizeBase`**（自适应二分的起点）+ **水平对齐档**。
                        // 判据 = **逐颗现读原版 MB**（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`）：
                        //   `…/Counters/Counter`      pid `1892`  `m_fontSizeBase = 36.0`  `m_HorizontalAlignment = 4`(**Right**)
                        //   `…/Counters/Slash`        pid `1848`  `m_fontSizeBase = 36.0`  `m_HorizontalAlignment = 2`(Center)
                        //   `…/Slash/Duplicates text` pid `1823`  `m_fontSizeBase = 35.0`  `m_HorizontalAlignment = 1`(**Left**)
                        //   （枚举 → `Runtime/TMP/TMP_Text.cs:74-77`：`Left=1, Center=2, Right=4, Justified=8, Flush=0x10`
                        //    ⇒ `4` 是 **`Right`**、⛔ **不是** `Flush`；正本 `:90` 原写 `4(Flush)`，已被调度台就地订正）
                        // 三颗的**框**（= 我们传进去那个矩形，与正本 §三 的 `rect` 同源）→ `资料/阶段二_卡片详情窗_原版规格.md:90-92`：
                        //   `Counter` 869.41→**942.69** · `Slash` 942.69→**970.71** · `Duplicates text` **970.71**→1014.80
                        //   （三格**首尾相接**：原版的 `Right`/`Center`/`Left` 正好让 `x2` 贴 `/`、`9` 从 `/` 后起 ⇒ 读起来是连续的 `x2/ 9`）
                        // 🔴 期望值**全是原版字面量**（⛔ 不读 `CardDetailPopup.CcBaseX1` / `CcX1R` —— 那是同义反复）。
                        // **改坏法**（四条，各红在不同的一组上）：
                        //   ① 三处 `autoBasePx` 实参去掉（回到不传第 10 参）⇒ base 落到标称档 52.6/57/52.6 ⇒ 那三条红；
                        //   ② 去掉 `MenuDraw.AlignRight(lbC, rC)` ⇒ 第 1 颗退回出厂档 `Center`，整块**右缘**落到
                        //      「rect 心 906.05 + 块宽/2」（= 差 `36.64 − 块宽/2`）⇒ 红；
                        //   ③ 去掉 `MenuDraw.AlignLeft(lbX2, rX2)` ⇒ 第 3 颗**左缘**落到「rect 心 992.755 − 块宽/2」⇒ 红；
                        //   ④ 把 `Slash` 也接上 `Align*` ⇒ 第 2 颗的**块心**离开 956.70（挪半个块宽）⇒ 红。
                        // ⚠️ **如实说清判别力**：②③④ 咬住的是「**整块被挪了半个块宽**」——它们对 **A575** 有判别力，
                        //   对 **A574（base）没有判别力**：base 只改二分起点、收敛结果不变 ⇒ 落位一动不动。
                        //   A574 的判别式是下面 `FontSizeBase` 那三条。两笔各断各的，⛔ 别互相顶。
                        // ⚠️ 判别力还依赖「**渲染块比容器窄**」（否则 `Center` 与 `Right` 的落位重合）：本夹具三颗
                        //   的块宽都远小于容器宽（`x2` < 73.28 · `/ ` < 28.02 · 一位数 < 44.09）⇒ 差值 = 半个块宽。
                        {
                            // 节点名 · 原版 `m_fontSizeBase`(px) · 原版对齐档 · 对齐后的**锚点**该落在哪(原版 rect 的边/心)
                            var alignCases = new[]
                            {
                                new { Name = "Counter",         BasePx = 36f, Mode = "Right",  Want = 942.69f,  What = "右缘" },
                                new { Name = "Slash",           BasePx = 36f, Mode = "Center", Want = 956.70f,  What = "块心" },
                                new { Name = "Duplicates text", BasePx = 35f, Mode = "Left",   Want = 970.71f,  What = "左缘" },
                            };
                            foreach (var ac in alignCases)
                            {
                                var at = FindChild(cnt, ac.Name);
                                var alb = at != null ? at.GetComponentInChildren<Label>() : null;
                                CheckTrue(alb != null, $"`{ac.Name}` 上找得到 `Label`（A574/A575 要量 base 与落位）");
                                if (alb == null) continue;
                                // ---- A574：`m_fontSizeBase`（自适应起点；`Label.FontSizeBase` 反射读 TMP 那个 protected 字段）
                                CheckNear(Label.FontSizeToPx(alb.FontSizeBase), ac.BasePx, 0.5f,
                                          $"`{ac.Name}` 的自适应 **base = 原版 `m_fontSizeBase` {ac.BasePx:F0}px**"
                                        + " —— 改坏法：`CardDetailPopup` 那三处不传第 10 参 ⇒ 落到标称档 52.6/57/52.6");
                                // ---- A575：整块的落位。块宽是**渲出来的**真测量（TMP 自己那块 `textBounds`
                                //     —— 🆕 **2026-10-16（A796′）换口**：改走 `LabelRenderedPx`，⛔ 不再读字段缓存 `Label.WorldW`），
                                //     ⛔ 不是回读我们传进去的实参；`PxOf` 与 `TitleLeftPx` 同一个设计空间口径。
                                float wpx = LabelRenderedPx(alb).x;
                                float cx = PxOf(alb.transform.position.x);
                                CheckTrue(wpx > 5f,
                                          $"（前提）`{ac.Name}` 的渲染块宽 {wpx:F2}px > 5 —— 量不出宽度时下面那条会**退化**"
                                        + "（`MenuDraw.Align*` 自己也有 `HasMeasuredWidth` 早退，这条防的是「两边一起变成 0」）");
                                float anchor = ac.Mode == "Right" ? cx + wpx * 0.5f
                                             : ac.Mode == "Left"  ? cx - wpx * 0.5f
                                             : cx;
                                CheckNear(anchor, ac.Want, 0.5f,
                                          $"`{ac.Name}` 的 **{ac.What} = {ac.Want:F2}px**（原版 rect + 原版对齐档 `{ac.Mode}`；"
                                        + $"量的是渲染块，块宽 {wpx:F1}px）"
                                        + " —— 改坏法：不调 `MenuDraw.Align*` ⇒ 退回出厂档 `Center`，整块挪半个块宽");
                            }
                        }
                        // ============================================================ 🔴 2026-10-13（A691）
                        // 计数条的**两支是两棵不同的树**（`spares > 0` / `spares == 0`）—— 本笔之前我们只把
                        // `Duplicate Counter` **改了个名**、几何/字号/结构全部沿用（= 两棵树当成一棵）。
                        // 判据 ①（分支）原版反编译 `decomp_full/CardCounterDisplay__Initialize.c`：
                        //   `iVar1 = 拥有` · `iVar2 = min(拥有, 卡组上限)` · `spares = iVar1 − iVar2`
                        //   `spares > 0` ⇒ `singleCounterContent.SetActive(0)` + `duplicateCounterContent.SetActive(1)`
                        //                  + `cardCounter = "x{min}"` + `duplicateCardCounter = "{spares}"`
                        //   `spares == 0`⇒ **反过来**，而且**只填 `singleCardCounter`**（`SetSingleCounter.c` 同款写法）
                        // 判据 ②（几何/字号各是各的）本笔**现读原版 prefab**（`bundle_scenes_scenes_mainmenuwarpforge`）：
                        //   `Card Counter` RT `1596` anchors(.5,.5) pos **(0,−329.5)** sd **(269.857, 79.37)**
                        //   ├ `Duplicate Counter` RT `1420` anchors(.5,.5) pos (0,0) sd **(239.991, 70.586)**
                        //   │   ├ `Background` RT `1140` stretch sd(0,0) · `m_PreserveAspect=1` · rot z 180°
                        //   │   └ `Counters` RT `1215` pos(−3.30002,−6.9) → `Counter` RT `1351` sd(73.283,70.586)
                        //   │       TMP `1892` fs **52.6** auto[25~52.6] base 36 `H=4`；另有 `Slash` / `Duplicates text`
                        //   │       / `Duplicate image` 三件
                        //   └ `Single Counter` RT `1531` anchors(.5,.5) pos (0,0) sd **(170, 50)** · GO `m_IsActive = False`
                        //       ├ `Background` RT `1513` stretch sd(0,0) · **同一个 sprite**（PathID `3969383734418133180`）· rot z 180°
                        //       └ `Counter` RT `1151` stretch sd(0,0) · TMP `1893`（= `singleCardCounter`，字段偏移 0x40）
                        //           fs **35** · auto[**25~35**] · base **36** · `m_HorizontalAlignment = 2`(Center) · 折行 0
                        //           ⇒ **没有** `Slash` / `Duplicates text` / `Duplicate image`
                        //        （⇒ 绝对矩形 `875, 844.50 → 1045, 894.50`）
                        // 🔴 期望值**从原版五元组现算**（`UguiRect.Child` = 本工程唯一那份锚点算法），⛔ **不读**
                        //    `CardDetailPopup.CcSg*` / `CcBg*` 那一族常量（那是自证）。
                        // 🔴 **两态都走**：本夹具默认 `spares = 9 > 0`（`CardProgress.Owned` 的「给足」口径）
                        //    ⇒ 先断 dup 支；再把拥有数压到 = 卡组上限（⇒ `spares == 0`）重建、断 single 支；
                        //    最后**原样还原**（数量 + 重建）—— 不给后面的断言与 `Shoot` 留个怪状态。
                        // **改坏法**（各红在不同的一条上）：
                        //   ① 回到「只改名、沿用它那套几何/字号」⇒「框 170×50」那条 + fs 那三条红；
                        //   ② 两支都建（去掉 `if (!dup)` 的早返回）⇒ 两条「另一支不在」红；
                        //   ③ single 支漏掉 `SetWrapping(false)` ⇒ 折行档那条红；
                        //   ④ 把 dup 那颗的 `AlignRight` 抄给 single ⇒「块心 = 960」那条红；
                        //   ⑤ `wrapPx` 传 `0f` ⇒ 自适应那四条（`AutoSizing` / min / max / TMP 容器宽）红。
                        // ⚠️ **如实说清判别力**：两支的 `m_AnchoredPosition` 都是 **(0,0)** ⇒ **两支矩形心逐位同值**
                        //    ⇒ `CheckAt` 那两条**分不出是哪一支**（它们只抓「节点摆到别处去了」）；真正分辨两支的是
                        //    **尺寸**（239.991×70.586 vs 170×50）与**结构**（那三件在不在）。
                        // ⚠️ **本段没覆盖的一档**：原版那颗 `Counter` 的 **`m_VerticalAlignment = 1024 (Bottom)`**
                        //    （现读 MB `1893`；dup 那三颗都是 `512 (Middle)`）—— 我们全工程按框内居中画 ⇒ **不在此断言**
                        //    （做不了的原因 → `WM3_SingleCounter.md` §七·1：要动 `Battle/Label.cs`，不在本笔白名单里）。
                        {
                            // 原版五元组 → 绝对矩形（父 = 全屏 `Card Options Panel` 0,0 → 1920,1080）
                            var rCC = UguiRect.Child(new PxRect(0f, 0f, 1920f, 1080f),
                                                     new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), UguiRect.P50c,
                                                     new Vector2(0f, -329.5f), new Vector2(269.857f, 79.37f));
                            var rDup2 = UguiRect.Child(rCC, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                       UguiRect.P50c, Vector2.zero, new Vector2(239.991f, 70.586f));
                            var rSg2 = UguiRect.Child(rCC, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                      UguiRect.P50c, Vector2.zero, new Vector2(170f, 50f));
                            // 「空节点 + `PxRect`」这一套把矩形记在 `sizeDelta` 上（`MenuDraw.ApplyPxRect` → `SetPxSize`）
                            Vector2 SizePx(Transform t)
                            {
                                var rt = t != null ? t.GetComponent<RectTransform>() : null;
                                return rt != null ? new Vector2(rt.sizeDelta.x * 108f, rt.sizeDelta.y * 108f)
                                                  : new Vector2(-1f, -1f);
                            }

                            var cnt2 = cd.Counter;
                            CheckTrue(cnt2 != null, "`Card Counter` 在（两支共同的父）");
                            // ---------- ① dup 支（本夹具当前状态：`spares = 9 > 0`）----------
                            var dupNode = FindChild(cnt2, "Duplicate Counter");
                            CheckTrue(dupNode != null,
                                      "★ `spares > 0` ⇒ **`Duplicate Counter` 那一支**（原版 `duplicateCounterContent.SetActive(1)`）");
                            if (dupNode != null)
                            {
                                var dupSize = SizePx(dupNode);
                                CheckNear(dupSize.x, rDup2.W, 1.5f,
                                          $"★ `Duplicate Counter` 框宽 = 原版 `m_SizeDelta.x` **239.991px**（{rDup2.W:F3}）");
                                CheckNear(dupSize.y, rDup2.H, 1.5f,
                                          $"…框高 = 原版 **70.586px**（{rDup2.H:F3}）");
                            }
                            CheckTrue(FindChild(cnt2, "Single Counter") == null,
                                      "★ …而且 `Single Counter` **不该在**（原版 `singleCounterContent.SetActive(0)`）"
                                    + " —— 改坏法：去掉 `if (!dup)` 的早返回、两支都建 ⇒ 这条红");
                            // ---------- ② 把 `spares` 压到 0 ⇒ 换另一支 ----------
                            int sgCap = CardProgress.DeckCap(cd.Card.Rarity);
                            int sgOwned = CardProgress.Owned(cd.Card.Id, cd.Card.Rarity);
                            int sgDelta = sgCap - sgOwned;               // ⇒ 拥有 = 卡组上限 ⇒ `spares == 0`
                            CardProgress.AddOwned(cd.Card.Id, cd.Card.Rarity, sgDelta);
                            Check(CardProgress.Spares(cd.Card.Id, cd.Card.Rarity), 0,
                                  $"（前提）拥有数 {sgOwned} → {sgCap} ⇒ `spares == 0`（原版换支的那个条件）");
                            cd.ShowCard(cd.Card);                        // 计数条按当前 `CardProgress` 重建
                            var cnt3 = cd.Counter;
                            var sgNode = FindChild(cnt3, "Single Counter");
                            CheckTrue(sgNode != null,
                                      "★ `spares == 0` ⇒ **`Single Counter` 那一支**（原版 `singleCounterContent.SetActive(1)`）");
                            CheckTrue(FindChild(cnt3, "Duplicate Counter") == null,
                                      "★ …而且 `Duplicate Counter` **不在了**（原版把那一支 `SetActive(0)`）");
                            if (sgNode != null)
                            {
                                var sgSize = SizePx(sgNode);
                                CheckNear(sgSize.x, rSg2.W, 1.5f,
                                          "★ `Single Counter` 框宽 = 原版 `m_SizeDelta.x` **170px**"
                                        + " —— ⛔ 不是 `Duplicate Counter` 那套 239.991（改坏法：两支共用一套框 ⇒ 本条红）");
                                CheckNear(sgSize.y, rSg2.H, 1.5f, "…框高 = 原版 **50px**");
                                CheckAt(sgNode, rSg2.x1, rSg2.x2, rSg2.y1, rSg2.y2,
                                        "`Single Counter` 落在原版矩形心 (960, 869.5) 上"
                                      + "（⚠️ 两支的 `m_AnchoredPosition` 都是 (0,0) ⇒ 本条**不是**分支判别式）");
                            }
                            // 那三件只长在 dup 那棵树上
                            CheckTrue(FindChild(cnt3, "Slash") == null, "…`Slash` **不在**（原版 `Single Counter` 子树里没有它）");
                            CheckTrue(FindChild(cnt3, "Duplicates text") == null, "…`Duplicates text` **不在**");
                            CheckTrue(FindChild(cnt3, "Duplicate image") == null, "…`Duplicate image` **不在**");
                            // 底图：两支**同一张 sprite**（原版两个 `Background` 的 `m_Sprite.m_PathID` 是同一个数）
                            var sgBg = FindChild(cnt3, "Background");
                            CheckArt(sgBg, "40K_main_deck_card_counter",
                                     "`Single Counter/Background` 还是那张底图（原版 `m_PreserveAspect = 1`）");
                            var sgQ = sgBg != null ? sgBg.GetComponentInChildren<ImageQuad>() : null;
                            if (sgQ != null)
                            {
                                float sgW = sgQ.WorldW * 108f, sgH = sgQ.WorldH * 108f;
                                CheckTrue(sgW > 5f && sgH > 5f,
                                          $"（前提）底图**真量得到**（{sgW:F1}×{sgH:F1}px）—— 否则下面那条会退化成恒真");
                                CheckTrue(sgW <= rSg2.W + 1.5f && sgH <= rSg2.H + 1.5f,
                                          $"底色**渲出来 {sgW:F1}×{sgH:F1}px ≤ 原版框 170×50**"
                                        + "（`m_PreserveAspect = 1` ⇒ 按图自身比例内接；若误用 dup 那套框会渲成 227.4×70.6 ⇒ 本条红）");
                            }
                            // 那颗 `Counter`（原版 = `singleCardCounter` 字段，0x40）
                            var sgT = FindChild(sgNode, "Counter");
                            CheckTrue(sgT != null, "`Single Counter/Counter` 在");
                            if (sgT != null)
                            {
                                CheckText(TextOf(sgT), "x" + sgCap,
                                          $"…文案 = **x{sgCap}**（原版格式串 `\"x{{0}}\"` 填 `min(拥有, 卡组上限)`；"
                                        + "⛔ 这一支**不印**多余副本数 —— 那个 `Duplicates text` 只长在 dup 那一支）");
                                var sgLb = sgT.GetComponentInChildren<Label>();
                                CheckTrue(sgLb != null, "…上找得到 `Label`（下面要量它的自适应与落位）");
                                if (sgLb != null)
                                {
                                    var sgTmp = sgT.GetComponentInChildren<TMPro.TextMeshPro>(true);
                                    CheckTrue(sgLb.AutoSizing,
                                              "★ …**真的开了自适应**（原版 MB `1893` 的 `m_enableAutoSizing = 1`）"
                                            + " —— 改坏法：`wrapPx` 传 `0f` ⇒ 自适应那一段整段不执行、本条红");
                                    CheckNear(Label.FontSizeToPx(sgLb.FontSizeMax), 35f, 0.05f,
                                              "★ …自适应上限 = 原版 `m_fontSizeMax` **35px**（= `m_fontSize`；⛔ 不是 dup 那颗的 52.6）");
                                    CheckNear(Label.FontSizeToPx(sgLb.FontSizeMin), 25f, 0.05f,
                                              "…自适应下限 = 原版 `m_fontSizeMin` **25px**（⛔ 不是 `Duplicates text` 那个 18）");
                                    CheckNear(Label.FontSizeToPx(sgLb.FontSizeBase), 36f, 0.5f,
                                              "…自适应 base = 原版 `m_fontSizeBase` **36px**"
                                            + " —— 改坏法：不传第 10 参 ⇒ base 落到字号本身（35）⇒ 本条红");
                                    Check(sgLb.WrappingMode, 0,
                                          "…折行档 = 原版 `m_TextWrappingMode = 0`（`NoWrap`）"
                                        + " —— 改坏法：删掉紧跟 `MenuDraw.Text` 的那句 `SetWrapping(false)` ⇒ 回 1");
                                    CheckNear(sgTmp != null ? sgTmp.rectTransform.sizeDelta.x : -1f,
                                              LayoutSpace.Px(170f), 2e-4f,
                                              "…**TMP 容器宽 = 原版 170px**（那颗 RT 是 stretch sd(0,0) ⇒ 容器 = **解析后的父宽**；"
                                            + "⛔ 不是 `m_SizeDelta.x = 0`，也不是 dup 那三颗的 73.283/28.02/44.09）");
                                    // 🆕 **2026-10-16（A796′）换口**：宽/高改走 `LabelRenderedPx`（TMP `textBounds`）
                                    //    —— ⛔ 不再读**字段缓存** `Label.WorldW/H`。
                                    var sgLbSz = LabelRenderedPx(sgLb);
                                    float sgLbW = sgLbSz.x, sgLbH = sgLbSz.y;
                                    CheckTrue(sgLbW > 5f && sgLbH > 5f,
                                              $"（前提）那颗字**真量得到**（{sgLbW:F1}×{sgLbH:F1}px）—— 量不出时下面那条会退化");
                                    CheckTrue(sgLbW <= rSg2.W + 1.5f && sgLbH <= rSg2.H + 1.5f,
                                              $"…**渲出来 {sgLbW:F1}×{sgLbH:F1}px ≤ 原版框 170×50**"
                                            + "（容差 1.5px = TMP 二分收敛粒度；只比字号会漏掉「字号对而溢出」那一族）");
                                    // 落位：原版这颗 `m_HorizontalAlignment = 2 (Center)` = 我们的出厂档 ⇒ **不调 `Align*`**
                                    float sgLbCx = PxOf(sgLb.transform.position.x);
                                    CheckNear(sgLbCx, rSg2.CX, 0.5f,
                                              $"★ …**块心 = {rSg2.CX:F0}**（原版 `Center` = 出厂档）—— 改坏法："
                                            + "把上面 dup 那颗的 `MenuDraw.AlignRight` 抄过来 ⇒ 块心右移半个块宽");
                                }
                            }
                            // ---------- ③ 原样还原（数量 + 重建）----------
                            CardProgress.AddOwned(cd.Card.Id, cd.Card.Rarity, -sgDelta);
                            Check(CardProgress.Owned(cd.Card.Id, cd.Card.Rarity), sgOwned,
                                  $"…拥有数还原成 {sgOwned}（后面的断言与 `Shoot` 都按原状态走）");
                            cd.ShowCard(cd.Card);
                            var cnt4 = cd.Counter;
                            CheckTrue(FindChild(cnt4, "Duplicate Counter") != null && FindChild(cnt4, "Single Counter") == null,
                                      "★ 还原后又**切回 `Duplicate Counter`**（两支来回都走得通 —— 不是只搭了个壳）");
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
                                //    ⚠️ 真实卡池里相关卡通常只有 1–2 张（全池 128 处点名摊在 100 来张卡上）
                                //    🔴 **2026-10-18 之后·第六会话就地订正（铁律 5）**：本行原写「**129** 处」
                                //    —— 那是 2026-10-18 之前的口径（`S13` 把 `UM84 Chaplain Cassius` 的
                                //    `[Talent]:` 改成裸写 `Talent:` 之后，规则②第一次真生效 ⇒ 少 1 处）。
                                //    真值 = **128**，判据 = `RuleEngine/Editor/RuleEngineTest.cs` 的
                                //    `Check(mentions, 128, …)`（那里 2026-10-18 已就地订正过，本行没跟着改）。
                                //    ⚠️ 中文档另有一份**不同**的数（**132** 处 / 同样 67 张）—— 见
                                //    `资料/普查产出_第六会话/WRelated_相关卡中文索引.md`。
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

                        // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据 → `:2298` 那一段）。
                        //   ⚠️ 上面那句点的是**本窗自己的** `ShadeHit`；本块补公共口那一版（多断三条结构/前提）。
                        //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后（本块下面紧接着就是 `Styles 页` 那一节，
                        //      与 `cd` 无关）；上面已经关过一次 ⇒ 先开回来。
                        CheckTrue(cd.TryOpen(), "（A796 现场）把卡片详情窗开回来 —— 下面那条要在**开着**的窗上点");
                        MenuDraw.CheckShadeClickRule(CheckTrue, "卡片详情窗", cd.transform, cd.ShadeHit,
                                                     () => cd.CurrentState);

                        // ================================================================
                        //  🆕 2026-10-17（D17 = 账上 A856）：**没有异画 ⇒ 整块 `SetActive(false)`**
                        //  判据（第一权威 · 本轮现读）= `d:/2/tools/decomp_full/AlternateArtPanel__Initialize.c:22-38`：
                        //    · `RawCardScript.HasAlternativeArtStyles(card) == false` ⇒ 填两个 0 之后
                        //      **`UnityEngine.GameObject.SetActive(gameObject, 0)`** —— **整块关**
                        //      （⛔ 不是「画成 `0 of 0` / `No alternate art`」，那正是我们改前的样子）；
                        //    · 有异画风格那一支才是 `SetActive(…, 1)`（`:35-38`）。
                        //  ⚠️ **两向都要断**：只断「没有 ⇒ 关」，一个「恒关」的实现会假绿；
                        //    只断「有 ⇒ 开」，一个「恒开」的实现会假绿 ⇒ 两条合起来才关得住。
                        //  ⚠️ 判据是**数据**（这张卡在不在异画表里），⛔ 不是「贴图加载出来没有」——
                        //    全文与理由 → `CardDetailPopup.HasAltArtStyle` 的注释。
                        //  ⚠️ 本块会把窗**换两张卡再关掉**：排在**本窗所有断言之后**（下面只剩 `Styles 页` 那一节）。
                        {
                            var plainDef = cd.Card;      // 当前这张 —— 上面那条刚断过它的异画标题是 `Alternate art`（= 没有异画）
                            string altId = null;
                            bool plainOk = plainDef != null;
                            foreach (var a in CollectionWindow.AltArtCards)
                            {
                                if (CollectionData.Card(a.CardId) == null) continue;   // 本地卡池里没有的跳过
                                if (plainDef != null && a.CardId == plainDef.Id) plainOk = false;  // 撞上了 ⇒ 这一向换不了对象
                                if (altId == null) altId = a.CardId;
                            }
                            CheckTrue(altId != null,
                                      "（前提）异画表里至少有一张卡在本地卡池里 —— 否则下面两条都成空转");
                            CheckTrue(plainOk,
                                      "（前提）当前这张卡**不在异画表里** ⇒ 下面「没有异画」那一向不是空转");
                            // ① 有异画 ⇒ **开着**
                            if (altId != null)
                            {
                                cd.ShowCard(CollectionData.Card(altId));
                                var apOn = FindChild(cd.transform, "Alternate Art Panel");
                                CheckTrue(apOn != null && apOn.gameObject.activeSelf,
                                          $"★ 有异画的卡（`{altId}`）⇒ 异画面板**开着**（原版 `SetActive(…, 1)`）");
                            }
                            // ② 没有异画 ⇒ **整块关**（节点还在树上，只是 `activeSelf == false`）
                            if (plainOk)
                            {
                                cd.ShowCard(plainDef);
                                var apOff = FindChild(cd.transform, "Alternate Art Panel");
                                CheckTrue(apOff != null,
                                          "「没有异画」时**节点仍在树上**（原版是 `SetActive(false)`，⛔ **不是不建**）");
                                CheckTrue(apOff != null && !apOff.gameObject.activeSelf,
                                          "★ 没有异画的卡 ⇒ 异画面板**整块 `SetActive(false)`**"
                                        + "（原版 `AlternateArtPanel__Initialize.c:27-33`）"
                                        + " —— 改坏法：把 `BuildAltArt` 末尾那句 `p.gameObject.SetActive(has)` 删掉 ⇒ 本条红");
                            }
                            cd.Close();     // 收尾：本块换过两张卡 ⇒ 交还一个**关着**的窗（同本块进来时的状态）
                        }
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
            // 🆕 **2026-10-18（`A1149` 第一半 · 第九会话 P6）**：这两颗**换风格圆钮**的**圆底盘实绘矩形**
            //   —— 同族那个「圆底盘长在子件上」的偏离（判据 = 原版 `/Header/Select Art Button {L,R}` 自己带
            //   `Image[UI_Button_Round_background]` · Simple · `preserveAspect` · `sizeDelta 74.386×75.605`，
            //   → `资料/普查产出_0923/A4_装饰页与驱动链.md:129`）**已归真到根节点自己身上**
            //   （`Shell/CollectionWindow.cs:1547`）⇒ 第 8 实参不传。期望值 = 正方贴图 237×237 等比内接 ⇒ **74.39×74.39**。
            //   ⚠️ 只断左钮：两颗走**同一个** `BuildStyleArrow`（⇔ 同一份代码），右侧那颗的矩形是同一支算出来的。
            A1125CloseBase("CollectionWindow/Select Art Button Left", spage,
                           "Select Art Button Left", "UI_Button_Round_background",
                           74.39f, 74.39f, 75.61f);
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
            var sLogo = FindChild(spage, "Art Style Logo");
            CheckText(TextOf(sLogo), "Hammer and Bolter",
                      "风格名 = **Hammer and Bolter**（`AA_HB` 的显示名，出处 `解包资源使用地图.md:1174`）"
                      + " —— ⚠️ **原版这格是图不是字**，我们这里是**我们挑的做法**");
            // 🔴 **量渲染宽度**（不是比字号）：那一格是 **512×128**，56px 的 `Hammer and Bolter`
            //    实测宽 ≈1270px ⇒ **会压到右箭钮上**（第一版实拍一眼可见）。判据照 `AutoFitBox` 那条教训。
            // 🔴 **2026-10-16（A830 换口）**：那个数**不再从实现里读**。原来读 `win.StyleLogoWidthPx`，
            //    而它是 `Shell/CollectionWindow.cs` 的一个属性、读的是 `Label.WorldW`
            //    = **字段缓存** `_tmpW/_tmpH`（只有 `RefreshBounds()` 写 —— 也就是**被测实现自己**；
            //    `SetFontSize` / `SetCharSpacing` 这一族只重排 mesh、**不刷缓存** ⇒ 谁在末次刷缓存之后
            //    重排一次，旧口照旧报旧值）⇒ **尺子长在被测实现身上**（A796′ 同族「更外层还在的口」，RO 漏列）。
            //    ✅ 现在**自检自己量**：TMP 自己渲出来那块 `textBounds`（`LabelRenderedPx`，与同文件
            //    `RectOf` / `TitleLeftPx`、`Editor/ShopScene.cs` / `RewardsScene.cs` **同一条口**）。
            //    ⚠️ 两条量法**同源**（`RefreshBounds` 读的就是它）⇒ **换口当天逐位同值**，
            //    期望值 512 与容差 1px **一个都不用动**；变的只是「以后重排看得见」+「尺子不再来自实现」。
            //    ⚠️ 顺手把「量不到」这一档补红：旧写法在 `_styleLogo == null` 时返回 `0f`，而 `0 ≤ 513` 恒真
            //    ⇒ 那是一处**假绿**（`AutoFitBox` 教训的同族：字没了照样绿）。⛔ 别把 `sLogoLb != null`
            //    那一项当成多余 —— 少了它，下面这条在「节点整个不在」时反而变绿。
            //    改坏法：`SetAutoFitBox` 那两个宽/高参（`CollectionWindow.BuildStylesPage` 里那句）传 `0`
            //    ⇒ 自适应不生效、`Hammer and Bolter` 按 56px 渲出来 ≈1270px ⇒ 这条当场红。
            var sLogoLb = sLogo != null ? sLogo.GetComponentInChildren<Label>() : null;
            float sLogoW = LabelRenderedPx(sLogoLb).x;
            CheckTrue(sLogoLb != null && sLogoW > 5f && sLogoW <= 512f + 1f,
                      $"`Art Style Logo` 那行字的**渲染宽度 {sLogoW:F0}px ≤ 512**"
                      + "（超出就会压到右边那颗换风格钮上 —— 这条**矩形断言量不到**，得量 TMP 自己渲出来那块 `textBounds`）");
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
                    //   🔴 **2026-10-14（#57–#59 · γ · 夹具侧）就地订正（铁律 5）**：这里原来写
                    //     「卡牌页抽屉此刻是**收着**的，但节点都在」—— **那个前提已经不成立**：
                    //     B1/A781 之后「整块落在视口外的文字**不建**」（`GameWindow.Text` / `MenuWindowBase.Text`
                    //     头一句就是 `MenuDraw.Visible`），而本页筛选列**最后一次重建**在本文件上面
                    //     「`Empty Collection Warning`」那一节里那次 `ClearCardFilters()`
                    //     （⚠️ 原来这里写 `:3858` / `:3802` —— **行号已漂、当场改成锚点**，铁律 5）——
                    //     那一刻抽屉是**收着**的（同一节上面几行刚关）、整栏滑在屏幕左外
                    //     ⇒ 那三件 `Label`（`Input Text` / `Cell_owned/Label` / `Title Army`）**压根没建出来**
                    //     （容器节点照建 ⇒ 上面那条「`Card Filters` 节点在」仍然绿，是个假前提）。
                    //     🔴 **2026-10-16 订正（A835 · 铁律 5）：这一句只对「A811 根治之前」成立** ——
                    //     根治之后框改取 `ViewportClip.BaseRect`（宿主写进去的设计矩形）⇒ 收起期间那次
                    //     重建**照样把这三件建出来**（详见下面 ② 那两段订正）。⚠️ ①② 两步夹具照旧留着
                    //     （① 让 TMP 在**活着的那一页**上量、② 把抽屉摆到**实现真正工作的那一态**），
                    //     但「这三件在不在」**今天不再是那条修法的验收口**。
                    //   ⇒ 夹具先摆到**实现真正工作的那一态**再查（**三条断言一个字没改**）：
                    //     ① 切到卡牌页 —— 这三条读的是 TMP 自己的字段，要在**活着的那一页**上量（同异画页那段）；
                    //     ② 真点页头那颗钮，把抽屉**展开到位**（与上面异画页那条 `styleFltBtn.Click();`
                    //        同一套；⚠️ 原来这里写 `:4981` —— 行号已漂，改成锚点）；
                    //   🔴 **2026-10-16 订正（铁律 5）**：这里原来还有 ③「**在原地重排一次**」（理由是
                    //     「展开这一下**不重建** ⇒ 收起时重建过的那一版不会自己回来」）—— **那句话从
                    //     2026-10-16 起不成立**：A811 的**最小修法**当天已落进 `Shell/CollectionWindow.cs`
                    //     的 `ApplyDrawerSlide` 尾「④」（滑到展开位那一拍自动补一次 `RebuildFilterRows`）
                    //     ⇒ ③ 删掉、⛔ 只留「点展开」那一步。
                    //   🔴 **2026-10-16 再订正（A835 · 铁律 5）：末句「把 ④ 删掉 ⇒ 三条当场全红
                    //     （收起态建的那一版里这三件**真的不在**）」已经不成立** —— 那是**根治之前**的
                    //     实况。根治之后收起态建的那一版里这三件**是在的**（见下面 ② 的订正）⇒
                    //     下面三条「（前提）…在」**删掉 ④ 也不会红**（它们只查在不在、不查落点）。
                    //     ✅ **2026-10-16 当天已补（A843）**：下面「（前提）…在」那三条**不再**是 ④ 的
                    //     验收口；盯 ④ 的是**本节末尾**新加的那一段「**A843 · 落点**」——
                    //     它把抽屉摆到「收起态重建过」那一态再展开，断 `Cell_owned` 必须回到 **167.905**
                    //     （删掉 ④ ⇒ 实得 552.905 ⇒ 必红）。⛔ 别再按「三条在不在」下结论。
                    //   ⚠️ A811 的另一半（「视口外的件到底该不该建」= 那道闸）**不是待办** ——
                    //     A798 已裁定「只裁不建在画面上**等价**」：判据见下面那一大段。
                    //
                    // 🔴 **2026-10-16（A811 现核 · 只读，未跑；铁律 5·b）—— 这条账要按【两条】分开记：**
                    //   ① **「闸本身」那一半已经有裁定、不是待办**：同一道「整块在框外 ⇒ 不建」的闸，
                    //      2026-10-14（A798）也加进了 `MenuDraw.Text` / `TextBox` 的**第一句** ——
                    //      就是那两句 `if (!Visible(r, _st.RenderClip)) return null;`
                    //      （⚠️ 原来这里写 `Shell/MenuDraw.cs:1614` / `:1673`，**行号已漂**；
                    //        2026-10-16 现读 = **`:1643`（`Text`）/ `:1705`（`TextBox`）**，按语句认别按行号认），
                    //      而那段注释自己写着裁定的理由：
                    //      「**「只裁不建」在画面上等价**（整块在框外 ⇒ `ClipText` 今天也会把它夹成零面积、
                    //     画不出像素），收益是与 `Rect`/`Nine`/`Tiled`/`Hit` 同一条判据」⇒
                    //      A811 行里那条「改成只裁不删」的路**已被否掉**（判据 = `MenuDraw.Text` 的那段）。
                    //   ② **重建那一半 —— 机制已换（🔴 2026-10-16 A835 订正 · 铁律 5）**。
                    //      **原文（留痕，未跑）**：「仍然开着的是「重建时机」那一半（这才是上面那三条红的
                    //      **真因**，而且**可见**）。读实现得到的机制 = **抽屉的行程比整块面板还宽**：
                    //        · 收起时 `ApplyDrawerSlide` 把面板整块挪 **−385px**（`FltHiddenDx`），
                    //          而本页那整块面板只有 **335.31px** 宽、视口 `FltView` = x∈[0.25, 335.56]
                    //          （`FltL`/`FltW`）⇒ **面板＋视口整条滑到屏左外**；
                    //        · 那一刻看框的那两路**读的都是节点当下位置** —— 文字那一路 `ViewportClip.ClipPx`
                    //          （`Shell/ViewportClip.cs:196`，`PosInDesignSpace(transform)`）、
                    //          容器那一路 `_fltScroll.Intersects` → `MenuScroll.ClipNode`（A465 起也转节点、
                    //          `Shell/MenuScroll.cs:398`）⇒ **两条路都拿到「屏左外那一条」**；
                    //          而被比较的矩形是**页面绝对设计矩形**（`MenuDraw.Local` 按基准位算出来的，
                    //          `Shell/CollectionWindow.cs` 的 `Abs(...)` / `_fltScroll.Shift(...)`）⇒ 恒不相交。
                    //        ⇒ **收起态下发生的任何一次重建**（`Empty Collection Warning` 那一节的
                    //          `ClearCardFilters()` / `RefreshCardsAfterFilter` / `RebuildFilterRowsNow`
                    //          的任一入口）**把所有带闸的件
                    //          全判成「框外」**：三件 `Label`、`Cell_*`、`Name Filter`、底图…**一律不建**；
                    //          只有**不带闸**的裸 `Node`（面板 `Card Filters` 自己）照建
                    //          —— 这正是「（前提）卡牌页的 `Card Filters` 节点在」只是**假前提**的原因。」
                    //      ⚠️ **上面这一整段引文只对「A811 根治之前」成立**（根治落地后那两条路**不再**
                    //      拿到「屏左外那一条」）。**✅ 根治之后的状态**：框的中心改取
                    //      `ViewportClip.BaseRect`（`Hang` / `MenuDraw.ApplyPxRect` 写进去的那份设计矩形），
                    //      **不再跟实时 `localPosition` 走** ⇒ 框与被比的矩形**同一帧** ⇒ 收起期间那次重建
                    //      **不再把任何件判成框外**（静态推读、**未跑** → `资料/普查产出_1016/W11_A811根治.md` §③·C · §⑤）。
                    //      🔴 **今天还开着的那点差 = 落点**：`MenuDraw.Local` 用的是父件**当下**位置
                    //      ⇒ 收起期间建的那一版会**跟着面板滑回来**、到位那一刻偏 **+385px** ⇒ ④ 补的就是这一拍
                    //      （更彻底那条 = **A834**：让 `MenuDraw.Local` 的帧也取记录矩形 —— 另开一趟）。
                    //        ⚠️ **下面这条「括注不成立」的现读结论仍然成立**（两路确实都读**同一颗节点框**，
                    //          只是那颗框现在取的是记录矩形、不再跟着面板滑）：
                    //          `Block6_CollectionScene五条.md` §二·1 的括注写「容器 `Cell_*` / `Name Filter`
                    //           照建」—— **2026-10-16 按代码现读核实：那条括注不成立**：
                    //           `Cell_*` 那一路在 `if (!cosmo && !_fltScroll.Intersects(r)) continue;` **之后**
                    //           才 `Node(parent, "Cell_" + …)`；`Name Filter` 更直接 —— `BuildNameRow` 第二句
                    //           就是 `if (!_fltScroll.Intersects(r)) return;`。
                    //        · ⚠️ **这三行从 2026-10-16 起失效（铁律 5，留痕）** —— 原文是：「而**展开抽屉
                    //          这一下不重建**（`ToggleFiltersNow` / `ApplyDrawerSlide` 只挪位置 + 开关节点）
                    //          ⇒ 收起时漏掉的那一版**不会自己回来** —— 这就是**可见后果**（按上面那条链，
                    //          打开抽屉那一刻整列是空的，直到滚一下 / 点一下筛选触发重排）」。
                    //          **当天已按下面的「最小一条」修掉** —— 展开到位那一拍现在**会**补一次重建。
                    //      ⇒ 修法两条：
                    //        · **最小一条 —— ✅ 已做**（2026-10-16，落进 `Shell/CollectionWindow.cs` 的
                    //          `ApplyDrawerSlide` 尾「④」）：抽屉**滑到展开位**那一拍补一次
                    //          `RebuildFilterRows(p)`，把「收起期间建的那一版」**重排到位**（偏 +385px）。
                    //          落点**没选** `ToggleFiltersNow` —— 那一处在 Play 下跑在起滑之后的一瞬间、
                    //          面板还停在收起位。
                    //        · **根治 —— ✅ 2026-10-16 已落**（A811 根治：跨 `Shell/ViewportClip.cs` 的
                    //          `BaseRect` + `Shell/MenuDraw.ApplyPxRect` 那一处穿透）：框的中心 = **宿主写进
                    //          节点的设计矩形** ⇒ 框与被比的矩形**同一帧**，**不再跟实时 `localPosition` 走**。
                    //          逐处改动 / 判据 → `资料/普查产出_1016/W11_A811根治.md`。
                    //          ⏭ **同族仍开着的那一件 = A834**：`MenuDraw.Local` 的帧仍是父件**当下**位置
                    //          ⇒ 收起期间建的那批件在 0.3 秒滑入期间偏 0→+385px（到位被 ④ 拉回）。
                    //      🔴 **验收断言 —— ⚠️ 2026-10-16 订正（A835 · 铁律 5）**：原文（留痕）写的是
                    //      「**去掉**原来那句 `win.ClearCardFilters()` 重排，只「点一下展开」之后那三件字
                    //      **应当在** —— 删掉 ④ ⇒ 三条红（这条断言的电就在这儿）」。**根治落地后那句不成立**：
                    //      收起期间建的那一版现在**真的**含这三件 ⇒ 删掉 ④ 它们**照样在**、三条**照旧绿**
                    //      （那三条只查**在不在**、不查**落在哪**）⇒ **④ 今天没有断言盯着**。
                    //      ✅ **2026-10-16 当天已补（A843）**：本节末尾那段「**A843 · 落点**」就是新定的
                    //      验收口（量**落点**：`Cell_owned` 展开到位后必须回到 **167.905**；删掉 ④ ⇒
                    //      实得 **552.905** ⇒ 红）。⚠️ 那一整段**未跑**（本件没跑 Unity）—— 首跑若红，
                    //      先看它自己的前提那三条，⛔ 别直接改期望值。原文列的两条路里**选了前面那条**。
                    //      ✅ **2026-10-16 现核（A843 · 铁律 5 订正）**：`Shell/CollectionWindow.cs` 里那两处同源注
                    //      **已经改好了**（`:490-501`，用的就是上面这一套「**A843 · 落点段 = 验收口**」的说法）；
                    //      原文（留痕）写的是「**仍未改**、那文件不在本件白名单、留给调度台」—— 那是**收口之前**的态。
                    win.tabButtons.Click(1);
                    Check(win.CurrentTab, WindowTabType.CollectionCards,
                          "（夹具态）先切到卡牌页 —— 下面三条要在**活着的那一页**上量");
                    var cFltHit = win.PageRoot(1) != null ? FindChild(win.PageRoot(1), "FiltersHit") : null;
                    var cFltBtn = cFltHit != null ? cFltHit.GetComponent<WindowButton>() : null;
                    CheckTrue(cFltBtn != null,
                              "（前提）卡牌页页头那颗 `Filter Toggle` 的命中区 `FiltersHit` 在（三条前提靠它开抽屉）");
                    // 🔴 **灭自证用的探针**（说明见下面那条断言）：**点之前**抓一张活着的卡池卡
                    //   （`CardsCells` 里存的是**卡的根节点**）当探针。
                    //   ⚠️ 此刻卡池滚在顶部（上一节那次清筛选把 `CardsScroll` 拉回 0）、且没有筛选条件
                    //   （`Empty Collection Warning` 那条刚验完「不显示」）⇒ 一定有建出来的格。
                    var probeCard = win.CardsCells.Count > 0 ? win.CardsCells[0] : null;
                    CheckTrue(probeCard != null,
                              "（前提）卡池里抓得到一张活卡当「灭自证」的探针 —— 抓不到那条就等于空转");
                    if (cFltBtn != null)
                    {
                        cFltBtn.Click();            // 🔴 **真点一次**（不是直调 `win.ToggleFilters()`）
                        CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                  "…点一下 ⇒ 卡牌页抽屉**展开到位**（⚠️ 抽屉页号 **0** = Cards；异画页那一份才是 1）");
                        // 🔴 **只「点一下展开」、⛔ 不再补重排**（2026-10-16 起 —— A811 最小修法
                        //   落进 `ApplyDrawerSlide` ④）：展开到位那一拍**自动**补一次重建。
                        //   ⚠️ **2026-10-16 订正（A835 · 铁律 5）**：原文这里写「这一步就是 A811 修法的
                        //   **验收点** ⇒ 那三个节点自己回来」，而「**改坏法**：把 ④ 那个 `if`（或它里面那次
                        //   `RebuildFilterRows(p)`）删掉 ⇒ 下面三条「（前提）…在」**当场全红**」——
                        //   **那两句从 A811 根治落地起不成立**（收起期间那一版现在**真的**含这三件，
                        //   见上面 ② 的订正）⇒ 删掉 ④ 它们**照样在**、三条**照旧绿**。
                        //   ⚠️ **2026-10-16 订正（A843 · 铁律 5）**：上面这句原文（留痕）写的是「④ 今天**没有断言
                        //   盯着** …… 这一条**要主对话裁**」—— **两半都已过期**：A843 当天就把验收口定在
                        //   本节末尾那段「**A843 · 落点**」= **验收口**（= 下面紧接的那三行，详见它自己）⇒ ⛔ 不再「待裁」。
                        //   ✅ **2026-10-16 当天已补（A843）**：本节末尾那段「**A843 · 落点**」= 新验收口
                        //   （量落点：`Cell_owned` 展开到位后必须回到 **167.905**；删掉 ④ ⇒ 实得 **552.905** ⇒ 红）。
                        //   ⚠️ 它**另起一段夹具**（自己收起 → 收起态重建 → 再展开），不复用本处这颗 `cFltBtn` 的态 ——
                        //   上面那条 `probeCard` 灭自证（卡池一张都没被重建）管的是**本处这一次点击**，两者不冲突。
                        //   · ⚠️ **本条不再单配「重建之后抽屉仍停在展开那一头」那条断言**：④ 与它收尾那次
                        //     `ApplyDrawerSlide(…, force: true)` 都在 `Click()` 里**同步**跑完 ⇒ 上面那条
                        //     （`DrawerSettled(0)`）读到的**就是重建之后的态**，再断一遍 = 同一条读两次（空转）。
                        //     （原来那句断言的**存在理由**是它夹在一次真重排后面 —— 重排没了，理由也没了。）
                        // 🔴 **灭自证**（铁律：只断「新写法对」不够）：上面三条光看结果，**分不出**
                        //   「修法挣来的」与「有人把这句删掉的 `win.ClearCardFilters()` 又补回来」——
                        //   所以再钉一条**只可能被那条补偿路弄红**的：`ClearFiltersNow` 会走
                        //   `RefreshCardsAfterFilter`（`CardsScroll.SetOffset(0)` + **整批**重建卡池
                        //   `RebuildCardsCells`，先销毁再重建）⇒ 拿一张**点之前**抓的活卡当探针，
                        //   点完之后它**必须还活着**。
                        CheckTrue(probeCard != null,
                                  "★ **灭自证**：点展开**只**走了抽屉那条路 —— 卡池那批 `CardView` 一张都没被"
                                  + "重建（`ClearCardFilters()` 那条补偿路会把它们**整批销毁重建** ⇒ 这条红，"
                                  + "上面三条的绿就不是修法挣来的了）");
                    }
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

                    // ============================================================ 🆕 2026-10-16（A843）
                    // **抽屉展开后的【落点】** —— ④（`Shell/CollectionWindow.cs` 的 `ApplyDrawerSlide` 尾）
                    // 今天**唯一**被人盯着的地方。
                    // 🔴 欠账原文：上面那三条「（前提）…在」**只查在不在**，而 A811 根治之后
                    //    「收起期间那次重建」照样把这三件建得出来 ⇒ **删掉 ④ 也全绿** ⇒ ④ 等于
                    //    没有断言盯着（判据 → `资料/普查产出_1016/W18_FltCell与文档漂移.md` §④·2）。
                    //
                    // 🔴 ④ 到底在补什么（现读实现）：`MenuDraw.Local(parent, r)` =
                    //    `RectCenter(r) − PosInDesignSpace(parent)` —— 减的是**父件当下**的位置 ⇒
                    //    面板停在【收起位】（左移 385px）时建的那一版，局部坐标里**多算了 385px**：
                    //    它出生那一刻看着是对的（跟着面板一起偏），**面板滑回原位之后整列偏 +385px**。
                    //    ④ = 「`Slide` 刚跨到 1 那一拍补一次 `RebuildFilterRows`」⇒ 把这一版**重排到位**。
                    //    ⚠️ 起手那次建**不欠**（`RowsBuiltOffBase = HasBasePos && Slide < 1`；起手
                    //    `HasBasePos == false`）⇒ 只有「滑出去之后、又在收起态重建过」这一条路欠。
                    //
                    // 🔴 夹具三步（全部走**公开口**，⛔ 不读任何私有状态）：
                    //    ① **基准**必须取自「④ 碰不到」的那一态 —— 抽屉**开着**（`Slide == 1`）时调
                    //       `ClearCardFilters()` ⇒ `RebuildFilterRowsNow` 里 `Slide < 1` 为假 ⇒
                    //       这一版**按基准位建**。⛔ **别拿「刚点开之后的那一版」当基准**：那一版
                    //       **正是 ④ 挣来的**（删掉 ④ 它自己就偏了）⇒ 拿它当基准 = 自证。
                    //    ② 收起（点页头那颗钮）⇒ 收起态**再** `ClearCardFilters()` 一次 ⇒ 这一版
                    //       就是**照收起位那帧建的**（= 欠的那一版）。
                    //    ③ 再展开 ⇒ ④ 必须把它**重排到位**。
                    //    🔴 ③ 走的是**帧路**（`StartDrawerSlideForTest` + `TickDrawers` →
                    //       `ApplyDrawerSlide`），**不是**点那颗钮 —— 见下面「灭自证」那条。
                    //
                    // 🔴 期望值一律写**原版字面量**（同 `:3455` 那条口径，⛔ 不读 `CollectionWindow.FltL/FltW`）：
                    //    · `Cell_owned` 那一行**横跨整个抽屉宽**（`FilterPanelModel.ToggleRowRects` 的
                    //      `row = 0…w`、`w = FltW`）⇒ 它的中心 x = 面板矩形 0.25…335.56 的中点 = **167.905**
                    //      （面板矩形那两条字面量 = 上面那条 `CheckAt(fltPanel, 0.25f, 335.56f, …)`）；
                    //    · `Title Army` 的渲染左沿 = **0.25**（同 `:3462` 那条）。
                    //    ⚠️ 前提：抽屉滚动量此刻是 **0**（下面所有落点值都是「未滚动」那一帧的读数）。
                    if (cDrawer != null && cFltBtn != null)
                    {
                        var cFscr = win.FilterScroll;
                        CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                  "（前提 · A843）进这一段时卡牌页抽屉是**展开到位**的（基准版要在这一态上建）");
                        CheckTrue(cFscr != null && Mathf.Abs(cFscr.Offset) < 0.01f,
                                  "（前提 · A843）抽屉滚动量 = **0**（下面落点值都是未滚动那一帧的读数；实得 "
                                  + (cFscr != null ? cFscr.Offset.ToString("F2") : "null") + "）");
                        // ① 基准版：**开着**重建一次（`Slide == 1` ⇒ ④ 碰不到这一版）
                        win.ClearCardFilters();
                        var cBase = FindChild(cDrawer, "Cell_owned");
                        CheckTrue(cBase != null, "（前提 · A843）落点探针 `Cell_owned` 在（那一行横跨整个抽屉宽）");
                        if (cBase != null)
                        {
                            CheckNear(PxOf(cBase.position.x), 167.905f, 1f,
                                      "★ **A843 · 基准落点**：`Cell_owned` 中心 x = **167.905**（原版那一行横跨"
                                      + "抽屉全宽 0.25…335.56 ⇒ 取中点）—— 这一版是【抽屉开着】时建的"
                                      + "（`Slide == 1` ⇒ `RowsBuiltOffBase` 为假）⇒ **④ 碰不到它**"
                                      + "（能当基准的原因就在这儿；⛔ 别拿「刚点开那一版」当基准）");
                            // ② 收起 ⇒ 收起态再重建一次（= 欠的那一版）
                            cFltBtn.Click();
                            CheckNear(win.DrawerSlide(0), 0f, 0.001f, "…收起：进度 **0**（整栏滑出去）");
                            var cRide = FindChild(cDrawer, "Cell_owned");
                            CheckTrue(cRide != null && cRide == cBase,
                                      "…而且**没重建**（还是同一颗节点）—— 它只是跟着面板一起滑走了");
                            CheckNear(cRide != null ? PxOf(cRide.position.x) : -9999f, 167.905f - 385f, 1f,
                                      "…这一颗**跟着面板左移 385px**（167.905 − 385 = **−217.095**）——"
                                      + "「基准版按基准位建」+ 这一条 = 这杆子真被 `ApplyDrawerSlide` 挪过"
                                      + "（`HasBasePos == true` ⇒ 下面那一版才可能「欠」）");
                            win.ClearCardFilters();          // 收起态重建 ⇒ 照收起位那帧建
                            var cOff = FindChild(cDrawer, "Cell_owned");
                            CheckTrue(cOff != null && cOff != cBase,
                                      "★ **前提（欠版已建）**：收起态这一次重建**真的换了节点**"
                                      + "（`RebuildFilterRowsNow` 先销毁再重建）—— 不换 = 夹具空转、下面两条等于没验");
                            if (cOff != null)
                            {
                                float offCx = PxOf(cOff.position.x);
                                CheckTrue(Mathf.Abs(offCx - 167.905f) < 1f
                                          || Mathf.Abs(offCx - (167.905f - 385f)) < 1f,
                                          "（前提 · A843）这一版是**照某一帧建出来**的：实得 **" + offCx.ToString("F1") + "**——"
                                          + "**167.905** = 照**收起位**那帧建（今天这条实现：局部坐标里多算 385px）；"
                                          + "**−217.095** = 照**基准位**那帧建（`MenuDraw.Local` 改成取记录矩形"
                                          + "—— A834 那条彻底修法落地后就是这一支）；两个都不是 ⇒ 面板停在半路"
                                          + "（夹具没摆到收起位）");
                                // 🔴 **2026-10-18（A834 · 铁律 5 订正）**：这条**析取保留原样**（它是**夹具前提**，
                                //   ⛔ 不是 A834 的验收口 —— 写成析取正是为了「A834 落地那天不假红」，
                                //   判据 → `资料/普查产出_1016/W24_落点断言与量法收口.md:41`）。
                                //   ⚠️ **但今天恒落 `−217.095` 那一支**：A834 走的是**候选甲**（`RebuildFilterRowsNow`
                                //   建之前调 `DrawerHome(_flt)`），不是这一行注释里原来设想的那条「`MenuDraw.Local`
                                //   改成取记录矩形」。A834 自己的验收口在**下一段**（那三条 + 判别式）。
                                // ③ 帧路展开：`StartDrawerSlide` + `TickDrawers → ApplyDrawerSlide`
                                //    （与 Play 跑起来那条**同源**；⛔ 这里**故意不点那颗钮**）
                                win.StartDrawerSlideForTest(0, true, playLike: true);
                                win.TickDrawers(0.3f);
                                CheckNear(win.DrawerSlide(0), 1f, 0.001f,
                                          "…`TickDrawers(0.3)`（原版 `animationTime`）⇒ 进度 **1**（到位）");
                                var cAfter = FindChild(cDrawer, "Cell_owned");
                                CheckNear(cAfter != null ? PxOf(cAfter.position.x) : -9999f, 167.905f, 1f,
                                          "★★ **A843 · 落点**：展开到位后 `Cell_owned` 必须**重排回 167.905**。"
                                          + "🔴 **改坏法：删掉 `RebuildFilterRowsNow` 里那句 `DrawerHome(_flt)`"
                                          + "（A834，2026-10-18 落地的修法）⇒ 这一条当场红**"
                                          + "（欠的那一版跟着面板滑回来 ⇒ 实得 **552.905** = 167.905 + 385）。"
                                          + "⚠️ **2026-10-18（铁律 5 订正）**：原文写的改坏法是「删掉 `ApplyDrawerSlide` 尾那个 ④」"
                                          + "—— 那一笔**从 A834 落地当天起不再成立**：`DrawerHome` 已经让这一版建在基准帧上，"
                                          + "④ 今天只是把同一件事**再做一遍**（冗余），删它这条照样绿。");
                                CheckNear(TitleLeftPx(cDrawer, "Army"), 0.25f, 1f,
                                          "★★ 同一拍、**另一条建法**的件也一样：小标题 `Army` 的**渲染左沿**回到 "
                                          + "**0.25**（面板内 0；期望值 = `:3462` 那条）"
                                          + "—— 改坏法同上（删 `DrawerHome(_flt)` ⇒ 落在 **385.25**）");
                                // 🔴 **灭自证（挡「终点和检测器一起改回去」）**：只断「落点对」不够 ——
                                //   删掉 ④ 之后，**在夹具里补一条补偿路**（例如「点开之后再
                                //   `ClearCardFilters()` 重排一次」= 原来那个 ③ 夹具，或者把补偿塞进
                                //   `ToggleFiltersNow`）照样能把落点摆对 ⇒ 那一条就变成「验夹具」了。
                                //   上面 ③ **故意走帧路**（`StartDrawerSlideForTest` + `TickDrawers`）：
                                //   那一路只经过 `ApplyDrawerSlide`，**`Toggle*` / 夹具里的补偿一律碰不到**
                                //   ⇒ **要让这条绿，修法必须落在帧路里**（`ApplyDrawerSlide` —— ④ 就在那儿；
                                //   同族的 `TickDrawers` 也算帧路）；⛔ 往 `ToggleFiltersNow` 里补一次重建、
                                //   或在夹具里补一句 `ClearCardFilters()` 都**不管用**（它们只能骗过下面 ④ 那条）。
                                //   ⚠️ 如实说清它**挡不住**什么：`RebuildFilterRowsNow` 自己（或者
                                //   `MenuDraw.Local`）如果改成「永远按基准帧算」，这条**照样绿**——
                                //   那**是对的**。🔴 **2026-10-18（A834 · 铁律 5）**：那个状态**今天已经到了**
                                //   （`RebuildFilterRowsNow` 建之前调 `DrawerHome(_flt)`）⇒ ④ 已退化成空转、
                                //   本条仍成立（「落点对」这一半照样要它绿）。
                                //
                                // ④ 另一条路（批量里 `Toggle*` 直接到位）也过一遍 —— 与 ③ 是**同一个** ④，
                                //   但入口不同（`ToggleFiltersNow → StartDrawerSlide(playLike: false)`）。
                                cFltBtn.Click();                  // 收起
                                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "…再收起（批量那条入口）：进度 **0**");
                                win.ClearCardFilters();           // 收起态重建 ⇒ 又一版欠的
                                var cOff2 = FindChild(cDrawer, "Cell_owned");
                                CheckTrue(cOff2 != null && cOff2 != cAfter,
                                          "…收起态那一版**又换了一颗**（欠版 2 就位）");
                                cFltBtn.Click();                  // 展开（批处理里 `Toggle*` 直接到位）
                                CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                          "…点一下 ⇒ 展开到位（`Toggle*` 那一支）");
                                var cAfter2 = FindChild(cDrawer, "Cell_owned");
                                CheckNear(cAfter2 != null ? PxOf(cAfter2.position.x) : -9999f, 167.905f, 1f,
                                          "★ **A843 · 落点（`Toggle*` 那条入口）**：同上，必须回到 **167.905** ——"
                                          + "删掉 ④ ⇒ 红（实得 **552.905**）；这一条与上面那条**各管一个入口**"
                                          + "（⚠️ 它**不是**灭自证那一条：这条入口在 `Toggle*` 里，补偿路碰得到它）");
                            }
                        }
                    }

                    // ============================================================ 🆕 2026-10-18（A834）
                    // **过渡帧的落点** —— 上面那些（含 A843 那 16 条）**全部在滑动结束之后量**，
                    // 而 A834 治的正是**过渡帧**：面板停在收起位时建的那一版，局部坐标里多烘了 385px
                    // ⇒ 0.3 秒滑入期间整列**与别的件不在同一帧上**（偏 0→+385px），只有 ④ 在到位那一拍扳回来。
                    //
                    // 🔴 **修法（= `Shell/CollectionWindow.cs` 的 `DrawerHome`，调度台裁定「甲」）**：
                    //    `RebuildFilterRowsNow` **建之前**先把面板摆回基准位 ⇒ 每一版**本来就是基准帧的坐标**
                    //    ⇒ 整列跟着面板**刚性**平移、途中每一帧都对。
                    //    ⚠️ 它**同时**治好两个读口（`MenuDraw.Local` **与** `Label.ParentXInDesignSpace`）
                    //    —— 这两个口读的是**同一份「父件当下位置」**，只修一个 = 「格子跟抽屉走、字不跟」。
                    //
                    // 🔴 **期望值一律写【原版字面量】**（与上面 A843 那一段同一条口径）：
                    //    · `167.905` = `Cell_owned` 那一行的中心 x（横跨抽屉全宽 0.25…335.56 ⇒ 取中点）；
                    //    · `0.25`    = `Title Army` 的渲染左沿（面板内 0 + 面板原点 0.25）；
                    //    · `385`     = 抽屉行程（原版 `hiddenPosition.x −550` 与 `originalAnchorPosition.x`
                    //                  那一对，`FltHiddenDx`）—— 三个都是**原版读数**，⛔ 不读 `FltL/FltW/FltHiddenDx`。
                    //    · t=0.5 那一帧 ⇒ 两位移各 **−192.5px** ⇒ 读数 = `167.905−192.5 = **−24.595**` ·
                    //      `0.25−192.5 = **−192.25**`。
                    if (cDrawer != null && cFltBtn != null)
                    {
                        // 归一状态：把抽屉推到展开位（帧路，0.3 秒走完；与下面 ③ 同一条口）
                        win.StartDrawerSlideForTest(0, true, playLike: true);
                        win.TickDrawers(0.3f);
                        CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                  "（前提 · A834）进这一段时卡牌页抽屉已展开到位");
                        // ① **基准**那一版（抽屉开着时建）—— 两条读数各取一个零点
                        win.ClearCardFilters();
                        var a8Base = FindChild(cDrawer, "Cell_owned");
                        float a8Cell0 = a8Base != null ? PxOf(a8Base.position.x) : -9999f;
                        float a8Army0 = TitleLeftPx(cDrawer, "Army");
                        CheckNear(a8Cell0, 167.905f, 1f,
                                  "（前提 · A834）基准版 `Cell_owned` 中心 x = **167.905**（开着建的那一版）");
                        CheckNear(a8Army0, 0.25f, 1f,
                                  "（前提 · A834）基准版 `Title Army` 渲染左沿 = **0.25**");
                        // ② 收起 ⇒ **收起态重建**（= 那版要跟着面板滑回来的件）
                        cFltBtn.Click();
                        CheckNear(win.DrawerSlide(0), 0f, 0.001f, "…收起：进度 **0**");
                        win.ClearCardFilters();
                        var a8Ride = FindChild(cDrawer, "Cell_owned");
                        CheckTrue(a8Ride != null && a8Ride != a8Base,
                                  "★ **前提（A834 的欠版已就位）**：收起态这一次重建**真的换了节点**"
                                  + "（`RebuildFilterRowsNow` 先销毁再重建）—— 不换 = 夹具空转、下面三条等于没验");
                        CheckNear(a8Ride != null ? PxOf(a8Ride.position.x) : -9999f, 167.905f - 385f, 1f,
                                  "★★ **A834 · 建的是【基准帧】**：收起态这一版建完（面板被摆回收起位）⇒ 中心 x = "
                                  + "**−217.095**（= 167.905 − 385）。"
                                  + "🔴 **改坏法：删掉 `RebuildFilterRowsNow` 里那句 `DrawerHome(_flt)`** ⇒ 局部坐标多烘 "
                                  + "385px ⇒ 实得 **167.905**（= A834 落地前那一支）");
                        // ③ 帧路推到**半路**（0.15s / 原版 `animationTime` 0.3 = 进度 0.5）
                        win.StartDrawerSlideForTest(0, true, playLike: true);
                        win.TickDrawers(0.15f);
                        CheckNear(win.DrawerSlide(0), 0.5f, 0.001f,
                                  "（前提 · A834）`TickDrawers(0.15)` ⇒ 进度 **0.5**（`animationTime` 0.3 的一半）");
                        var a8Mid = FindChild(cDrawer, "Cell_owned");
                        float a8Cell1 = a8Mid != null ? PxOf(a8Mid.position.x) : -9999f;
                        float a8Army1 = TitleLeftPx(cDrawer, "Army");
                        CheckTrue(a8Mid != null && a8Mid == a8Ride,
                                  "★ **A834 · 过渡帧里一次都不重建**：滑到一半时还是收起态那一版"
                                  + "（`RebuildFilterRowsNow` 只在**建的那一刻**摆位、之后整列跟着面板刚性平移）"
                                  + "—— 途中任何一次重建都会让下面两条变成「验夹具」");
                        CheckNear(a8Cell1, -24.595f, 1f,
                                  "★★ **A834 · 过渡帧落点**：0.3 秒滑入到一半（t=0.5）时 `Cell_owned` 中心 x = "
                                  + "**−24.595**（= 167.905 − 385×0.5，整列跟着面板**刚性**平移）。"
                                  + "🔴 **改坏法：删掉 `DrawerHome(_flt)`** ⇒ 收起态那一版局部坐标多烘 385px ⇒ "
                                  + "实得 **360.405** = 167.905 + 385×0.5");
                        CheckNear(a8Army1, -192.25f, 1f,
                                  "★★ **同一个病的【另一个读口】**：同一帧里 `Title Army` 的渲染左沿 = **−192.25**"
                                  + "（= 0.25 − 385×0.5）。这一颗走的是 `Label.AlignLeftOn` → "
                                  + "`Label.ParentXInDesignSpace()`（`Battle/Label.cs`），与格子的 `MenuDraw.Local` "
                                  + "**读同一份「父件当下位置」** —— 只修一个 ⇒ 字与格子**撕裂**（差 385px）");
                        // 🔴 **判别式（挡「只修一半」/「两边一起改回去」）**：
                        //   上面两条**各按字面量**钉了一个读数；这一条**不读任何常量**，只比**两个实测量各挪了多少**
                        //   —— 两条读口同帧 ⇒ 位移**必须相等**（都 −192.5）。
                        //   它挡的是：把 `MenuDraw.Local` 那条路改成「按记录矩形算」、却漏了 `Label` 那条
                        //   （那时两个差 = −192.5 与 **+192.5** ⇒ 红）；也挡「把期望值改成实测量」这种改法
                        //   （差值那一条不受期望值影响）。
                        CheckNear(a8Cell1 - a8Cell0, a8Army1 - a8Army0, 0.5f,
                                  "★★ **判别式（A834）**：同一帧里**两条读口各挪了多少必须相等**"
                                  + $"（实得 格子 {(a8Cell1 - a8Cell0):F2}px · 字 {(a8Army1 - a8Army0):F2}px）");
                        // 收尾：把抽屉推回**展开**（下面那段收尾要靠「点一下」把它关掉）
                        win.StartDrawerSlideForTest(0, true, playLike: true);
                        win.TickDrawers(0.3f);
                        CheckNear(win.DrawerSlide(0), 1f, 0.001f,
                                  "…收尾：推回展开（0.3 秒走完）—— 紧接着段收尾那句「点一下」要靠它关得掉");
                    }
                    // 🔴 **2026-10-14（#57–#59 · γ · 夹具侧收尾）**：本节自己开自己关 —— 卡牌页抽屉收回去、
                    //   页签切回**异画页**（紧接着那一条还要点异画页那颗钮、把它的抽屉也关掉）。
                    //   ⛔ 两句都不能省：抽屉留着张开 ⇒ 给后面的段留状态；页签留在 Cards ⇒ 异画页那一段收不了尾。
                    if (cFltBtn != null) cFltBtn.Click();
                    win.tabButtons.Click(3);
                    Check(win.CurrentTab, WindowTabType.CollectionStyles, "（夹具态复原）页签切回异画页");
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

                // ---------------- 🆕 2026-10-18（A840）：两页视口**各记过自己的设计矩形** ----------------
                //   记录点 = `Shell/AvatarTab.cs:167` / `Shell/TitleTab.cs:140` 各补的那一句 `vc.CaptureNow();`。
                //   口径 → `Shell/ViewportClip.cs` 文件头 §①；现场 → `资料/普查产出_1018/S1_A840与A867.md` §1/§2.1。
                //   🔴 **为什么非断不可**：`MenuDraw.ApplyPxRect` 往这个节点写矩形**发生在组件存在之前**
                //   （那一句的穿透只找**已有**的 `ViewportClip`）⇒ ⛔ 少了 `CaptureNow()` 这两颗**逐位回落到
                //   旧写法**（实时反推 + `LiveDerivations`）：本页整块面板被挪过之后，框与被比矩形就**不在同一帧**
                //   （A811 那个病灶）。⇒ **逐页各一条**，⛔ 不写成「至少有一页对」那种弱断言。
                //   期望值 = **原版 prefab 字面量**（两页逐位同矩形 654.16,210.69 → 1680.12,855.46，
                //   就是上面那几条 `ScanSoftCuts` 用的那一份）；⛔ 不读 `AvatarTab.VpL` / `TitleTab.VpL`
                //   —— 那是**被测实现传进 `Node(...)` 的实参**（拿它当期望值 = 自证）。
                void A840Check(string who, Transform tabRoot, string path, PxRect want)
                {
                    var vn = tabRoot != null ? tabRoot.Find(path) : null;
                    var vv = vn != null ? vn.GetComponent<ViewportClip>() : null;
                    CheckTrue(vv != null,
                              $"（前提·不静默）{who}：`{path}` 那颗节点上挂着 `ViewportClip`"
                            + " —— ⛔ 拿不到 ⇒ 下面那两条**没跑**，不是绿");
                    if (vv == null) return;
                    CheckTrue(vv.HasBaseRect,
                              $"★ A840：{who} 这颗视口**记过设计矩形**（`ViewportClip.HasBaseRect`）"
                            + " —— 记录点 = `AddComponent<ViewportClip>()` 之后那句 `vc.CaptureNow();`"
                            + "｜**改坏法**：删掉那一句 ⇒ 本条红（该颗逐位回落到实时反推 + `LiveDerivations`）");
                    var b = vv.BaseRect;
                    CheckTrue(Mathf.Abs(b.x1 - want.x1) <= 0.05f && Mathf.Abs(b.y1 - want.y1) <= 0.05f
                              && Mathf.Abs(b.x2 - want.x2) <= 0.05f && Mathf.Abs(b.y2 - want.y2) <= 0.05f,
                              $"★★ A840：{who} 记下的矩形 = **宿主写进这个节点的那份设计矩形**"
                            + $"（实测 {b.x1:F3},{b.y1:F3} → {b.x2:F3},{b.y2:F3}；"
                            + $"期望 = 原版 prefab 的 {want.x1:F2},{want.y1:F2} → {want.x2:F2},{want.y2:F2}）"
                            + "｜**改坏法**：把 `CaptureNow()` 挪到节点被改过之后再调（= 记成**错帧**）⇒ 红"
                            + "（`HasBaseRect` 仍 true ⇒ 上面那条抓不住它，只有本条抓得住）");
                }
                A840Check("档案窗 `Avatar Tab`（`Shell/AvatarTab.cs:167`）",
                          FindChild(pp.transform, "Avatar Tab"), "Item Display Panel/Scroll Rect",
                          new PxRect(654.16f, 210.69f, 1680.12f, 855.46f));
                A840Check("档案窗 `Title Tab`（`Shell/TitleTab.cs:140`）",
                          FindChild(pp.transform, "Title Tab"), "Item Display Panel/Scroll Rect",
                          new PxRect(654.16f, 210.69f, 1680.12f, 855.46f));

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

                    // 🔴 **2026-10-18（第七轮）：输入框占位符走词条** —— 键 = 原版那颗 `Placeholder` 的
                    //   `Localize.mTerm` 原文 `MainMenu/Chat/TypeMessage`（1 颗；父链 =
                    //   `Placeholder < Text Area < InputField (TMP) < Enter Text < Chat < Holder < ChatPanel`
                    //   ⇒ 节点名与路径都和我们这一颗一致）。
                    //   ⚠️ 我们的树上**没有 `Text Area` 那一层**（`Text(...)` 直接把字挂在 `InputField (TMP)` 下）
                    //   ⇒ 路径少一节，别照抄原版那条。
                    {
                        var ci = FindChild(FindChild(FindChild(FindChild(chat.transform, "Holder"), "Chat"), "Enter Text"),
                                           "InputField (TMP)");
                        var cph = ci != null ? FindChild(ci, "Placeholder") : null;
                        CheckTrue(Loc.HasEntry("MainMenu/Chat/TypeMessage"),
                                  "（前提）词条 `MainMenu/Chat/TypeMessage` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
                        CheckText(TextOf(cph), Loc.T("MainMenu/Chat/TypeMessage"),
                                  "★ 聊天窗输入框占位 = `Loc.T(\"MainMenu/Chat/TypeMessage\")`（随语档；"
                                + "原版 TMP 原文 `Type message` / 中文「输入消息」`zh_CN.csv:183`）");
                    }

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

                    // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据 → `:2298` 那一段）。
                    //   ⚠️ 本窗那颗命中区的节点名是 **`CloseBackground/CloseHit`**（同族三扇里只有它不叫
                    //      `BackgroundHit`）⇒ 名字照上面 `CheckShadeRule` 那一行现成的实参抄，⛔ 别统一写
                    //      `BackgroundHit`（`Editor/MainMenuScene.cs:172-174` 记着那次误判）。
                    //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后（下面紧接着是 `:5552` 的 ShopWindow 那一段，
                    //      与 `chat` 无关）；上面已经关过一次 ⇒ 先开回来。
                    CheckTrue(chat.TryOpen(), "（A796 现场）把聊天窗开回来 —— 下面那条要在**开着**的窗上点");
                    MenuDraw.CheckShadeClickRule(CheckTrue, "聊天窗",
                                                 chat.transform,
                                                 FindChild(FindChild(chat.transform, "CloseBackground"), "CloseHit"),
                                                 () => chat.CurrentState);
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
                    CheckTrue(dipA.TryOpen(null), // 态一（开关关）建一遍 ⇒ p1 == 设计点
                              "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                                          CheckTrue(dipA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）
                                                    "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                    CheckTrue(pmpA.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                                          CheckTrue(pmpA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）
                                                    "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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

            // ======== 🆕 2026-10-13（A503）：`CollectionData` 的四个口在「写盘失败」时说得出话 ========
            //  判据 = **本项目红线**「⛔ 不许静默失败」（CLAUDE.md §三）+ `DeckLibrary` 侧已经补好的出口
            //  （A398：`Save()` 把原因写进 `LastError`、`SaveOrWarn` 出声、`Rename`/`Delete` 是 `return Save();`）。
            //  🔴 缺陷本来是：`Shell/CollectionData.cs` 那四个口**既不读返回值、也不读 `LastError`**，还各多调一次
            //  `Lib.Save()` ⇒ 写盘失败时玩家看到的是「成功」，而盘上一点没变。
            //  ⚠️ 这一段**必须留在 `Run()` 最后**：它会把 `OverridePath` 拐到坏路径、还会往内存里多塞几套卡组 ——
            //     前面那些断言（卡组数 / 列表格 / 开战链）全依赖 `Run()` 开头那个夹具（14 套 + 第 1 套合法 30 张）。
            Section("A503：`CollectionData` 四个口 —— **写盘失败不许说成「成功」**");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");

                // ---- ① 控制组：**好路径**下四个口都回真值（缺了它，一个「恒报失败」的实现照样绿）----
                string okCreate = CollectionData.CreateDeck();
                CheckTrue(!string.IsNullOrEmpty(okCreate),
                          $"（控制组）好路径 `CreateDeck` 回**名字**（「{okCreate}」）—— 恒返空串的实现 ⇒ 这条红");
                string okCopy = CollectionData.DuplicateDeck(0);
                CheckTrue(!string.IsNullOrEmpty(okCopy), $"（控制组）……`DuplicateDeck` 回名字（「{okCopy}」）");
                string okWhy;
                string okImp = CollectionData.ImportDeck(DeckLibrary.ExportString(CollectionData.Raw(0)), out okWhy);
                CheckTrue(!string.IsNullOrEmpty(okImp) && string.IsNullOrEmpty(okWhy),
                          $"（控制组）……`ImportDeck` 回名字（「{okImp}」）且 `why` 空（实得「{okWhy}」）");
                int c0 = CollectionData.DeckCount();
                CheckTrue(CollectionData.DeleteDeck(c0 - 1) && CollectionData.LastDeleteError.Length == 0,
                          "（控制组）……`DeleteDeck` 回 **true**，且 `LastDeleteError` 是**空串**（成功不报错）");

                // ---- ② 探针：**写不进去的路径**（`File.WriteAllText` 抛 `DirectoryNotFoundException`）----
                DeckStore.OverridePath = Path.Combine(probeDir, "__wf_a503_no_such_dir__", "x.json");
                CheckTrue(!DeckLibrary.Load().Save(),
                          "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                CollectionData.ResetForTest();     // 丢掉缓存 ⇒ 重新 `Load()`（坏路径读不到 ⇒ 空库，**不抛异常**）
                Check(CollectionData.DeckCount(), 0, "（前提）坏路径下起手是**空库**（「读不到」不是「读失败」）");

                string badCreate = CollectionData.CreateDeck();
                CheckTrue(string.IsNullOrEmpty(badCreate),
                          "★ A503：写盘失败 ⇒ `CreateDeck` 回**空串**（改坏法：把 `CollectionData.CreateDeck` 里"
                        + " `if (Lib.LastError != null)` 那一支整段删掉、照旧 `return got;` ⇒ 这条红）");
                Check(CollectionData.DeckCount(), 1,
                      "★ A503：……而它**真的建在内存里**了（1 套）—— 本条说的「失败」专指**没落盘**（内存建了、盘上没有）");

                string badCopy = CollectionData.DuplicateDeck(0);
                CheckTrue(string.IsNullOrEmpty(badCopy),
                          "★ A503：写盘失败 ⇒ `DuplicateDeck` 回**空串**（改坏法：删掉它那个 `Lib.LastError` 支 ⇒ 红）");

                string why;
                string badImp = CollectionData.ImportDeck(DeckLibrary.ExportString(CollectionData.Raw(0)), out why);
                CheckTrue(string.IsNullOrEmpty(badImp),
                          "★ A503：写盘失败 ⇒ `ImportDeck` 回**空串**（改坏法：删掉它那个 `Lib.LastError` 支 ⇒ 红）");
                // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：`why` 那两句从波 1b 起走词条
                //   （`Shell/CollectionData.ImportDeck` → `Loc.T("MenuDeck/Error/ImportEmpty")` /
                //   `Loc.T("MenuDeck/Error/ImportBadString")`）⇒ 原来写死的那两句中文**只是中文列的副本**，
                //   宿主一切英文档这条就**假绿**（第三句与英文的短键本来也不等）。
                //   期望值换成 `Loc.T(键)` —— 负判据照样成立（两语档下第三句都与那两个短键不同）。
                CheckTrue(Loc.HasEntry("MenuDeck/Error/ImportEmpty")
                          && Loc.HasEntry("MenuDeck/Error/ImportBadString"),
                          "（前提）词条 `MenuDeck/Error/Import{Empty,BadString}` 都在表里（⛔ 不在 ⇒ 下一条恒真）");
                CheckTrue(!string.IsNullOrEmpty(why)
                          && why != Loc.T("MenuDeck/Error/ImportEmpty")
                          && why != Loc.T("MenuDeck/Error/ImportBadString"),
                          "★ A503：……而且 `why` 给的是**第三种人话**（串本身没问题，是**没写进存档**；实测「"
                        + why + "」）—— 改坏法：把 `why = \"卡组串读出来了…\"` 那一行删掉 ⇒ 这条红"
                        + "（两个负判据**随语档**，键 `MenuDeck/Error/Import{Empty,BadString}`）");

                // ---- ③ 「没删」vs「删了没落盘」：**分开报**（A503 验收第 2 条）----
                int n1 = CollectionData.DeckCount();          // = 3：上面建 1 + 复制 1 + 导入 1
                bool delRange = CollectionData.DeleteDeck(99);
                string e1 = CollectionData.LastDeleteError;
                CheckTrue(!delRange, "★ A503：下标越界 ⇒ 回 false");
                Check(CollectionData.DeckCount(), n1, "★ A503：……而且**一套都没少**（「没删」这一种）");
                bool delSave = CollectionData.DeleteDeck(0);
                string e2 = CollectionData.LastDeleteError;
                CheckTrue(!delSave,
                          "★ A503：删了、但**没落盘** ⇒ 同样回 false（`DeckLibrary.Delete` 就是 `return Save();`）");
                Check(CollectionData.DeckCount(), n1 - 1, "★ A503：……但内存里**确实少了一套**（「删了没落盘」这一种）");
                CheckTrue(e1.Length > 0 && e2.Length > 0 && e1 != e2,
                          "★ A503：**两种失败报的是两句不同的话**（改坏法：把 `CollectionData.DeleteDeck` 里那两支"
                        + "合成一句「删不了」⇒ 这条红 —— 那正是原来的缺陷）"
                        + "\n              ①（没删）「" + e1 + "」\n              ②（删了没落盘）「" + e2 + "」");
                CheckTrue(e1.Contains("没删") && e2.Contains("没写进存档"),
                          "★ A503：……而且各自点明是**哪一种**（① 含「没删」· ② 含「没写进存档」）");

                // ---- ④ 收尾：路径还回去（⛔ 绝不碰玩家的真存档）+ 丢掉这一段弄脏的缓存 ----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");
            }

            // ======== 🆕 2026-10-13（A600 · A601）：`Select` 的出口 + `DeckInfoPopup` 删/复制的失败文案 ========
            //  **判据** = **本项目红线**「⛔ 不许静默失败」+ **A503 在 `CollectionData` 上刚定下的那套出口形状**
            //    （`bool` 返回值 + `LastXxxError` + `Debug.LogWarning` —— 见 `Shell/CollectionData.cs` 的 `DeleteDeck`；
            //     ⛔ 本件**没另立一套**）。⚠️ 原版**没有对应物**：原版卡组存在**服务器**上
            //    （`CardDeck.syncedToServer` / `deckId`，见 `RuleEngine/Data/DeckLibrary.cs` 文件头）⇒ 本地这条链无原版判据。
            //
            //  **A600 的缺陷原文**：`CollectionData.Select(i)` 原来是 `Lib.Select(i); Lib.Save();` —— 返回值与
            //    `LastError` **都不读**；而 `BattleDriver.PickSavedDeck` 是**从磁盘重读**的（`CollectionData` 自己
            //    在 `Select` 里写着这条因果）⇒ 写盘失败时「你选的那一套」**静默失效**、**战斗会拿磁盘上那套旧的**，
            //    而七个调用点（`DeckInfoPopup` / `CollectionWindow` / `PracticeModePopup` / `LiveOpsEventWindow`
            //    ×2 / `MainMenuScene` / `CollectionScene`）还紧接着打「已选中」。
            //  **A601**：`Shell/DeckInfoPopup.cs` 那三处（A548 刚改完的文案）当时**只有类型检查兜着**、一条断言都没有；
            //    要咬的是「**删失败 / 复制失败各自的两支，文案分得清是哪一支**」（⛔ 弱断言分不出两种状态）。
            //
            //  ⚠️ 这一段**必须留在 `Run()` 最后**（同 A503 那节的理由）：它会把 `OverridePath` 拐走、还会增删内存里的卡组。
            var grabbed = new List<string>();

            // 点一颗钮、抓住这一下产生的**全部**日志（判据范式 = 本文件 `:2111-2135` 的 A229 那一段 +
            //   `Editor/BattleScene.cs:210`：批处理里 `Debug.Log` 照样走 `Application.logMessageReceived`）。
            void ClickHit(Transform hit)
            {
                grabbed.Clear();
                Application.LogCallback h = (c, s, t) => { if (c != null) grabbed.Add(c); };
                Application.logMessageReceived += h;
                if (hit != null) { var wb = hit.GetComponent<WindowButton>(); if (wb != null) wb.ClickForTest(); }
                Application.logMessageReceived -= h;
            }
            int HitCount(string contains) { int k = 0; foreach (var l in grabbed) if (l.Contains(contains)) k++; return k; }
            string FirstHit(string contains) { foreach (var l in grabbed) if (l.Contains(contains)) return l; return null; }

            // ---------------- ① A600：`CollectionData.Select` 的出口（控制组 + 两个探针 + 调用点） ----------------
            Section("A600：`CollectionData.Select` 的落盘结果**有人看了**（出口 + 调用点）");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");

                int n0 = CollectionData.DeckCount();
                CheckTrue(n0 >= 2, $"（前提）库里 **≥ 2** 套（实得 {n0}）—— 下面要挑一个「不是现在选中的」下标");

                // ---- ①-a 控制组（好路径）：选中**成功**那一支 ----
                //   缺了它，一个「恒返 false」的实现照样能把下面两条探针全骗绿。
                int tgt = CollectionData.CurrentIndex() == 0 ? n0 - 1 : 0;
                bool okSel = CollectionData.Select(tgt);
                CheckTrue(okSel, $"（控制组）好路径 `Select({tgt})` 回 **true**"
                               + "（改坏法：把 `Select` 写成恒返 false ⇒ 这条红）");
                CheckTrue(CollectionData.LastSelectError.Length == 0,
                          "（控制组）……而且 `LastSelectError` 是**空串**（成功不报错）—— "
                        + "改坏法：成功那一支也写错误 ⇒ 这条红");
                Check(CollectionData.CurrentIndex(), tgt, "（控制组）……而且当前选中那一套**真的是它**");

                // ---- ①-b 探针一：**下标越界**（`DeckLibrary.Select` 早退、**根本不碰盘** ⇒ 就在好路径上量）----
                int keepCur = CollectionData.CurrentIndex();
                bool badRange = CollectionData.Select(n0 + 5);
                string eRange = CollectionData.LastSelectError;
                CheckTrue(!badRange, "★ A600：下标越界 ⇒ 回 **false**"
                                   + "（改坏法：删掉 `Select` 里那句 `Lib.CurrentIndex != i` 判断 ⇒ 这条红）");
                CheckTrue(eRange.Length > 0 && eRange.Contains("没选中"),
                          "★ A600：……而且报的是「**没选中**」那一支（实得「" + eRange + "」）");
                Check(CollectionData.CurrentIndex(), keepCur, "★ A600：……而且**当前选中一动没动**（越界那一支是早退）");

                // ---- ①-c 探针二：**写盘失败**（父目录不存在 ⇒ `File.WriteAllText` 抛 `DirectoryNotFoundException`）----
                string probePath = Path.Combine(probeDir, "__wf_a600_no_such_dir__", "x.json");
                DeckStore.OverridePath = probePath;
                var probeLib = DeckLibrary.Load();
                CheckTrue(!probeLib.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                string expectSaveErr = probeLib.LastError;
                CheckTrue(!string.IsNullOrEmpty(expectSaveErr),
                          "（前提）写不进去时**带了原因**（原因空 ⇒ 下面「报的是真原因」那条会**空串恒真**）");

                int saveTgt = CollectionData.CurrentIndex() == 0 ? 1 : 0;
                bool badSave = CollectionData.Select(saveTgt);
                string eSave = CollectionData.LastSelectError;
                CheckTrue(!badSave, "★ A600：写盘失败 ⇒ 回 **false**"
                                  + "（改坏法：退回裸 `Lib.Save();`、不读返回值 ⇒ 这条红 —— **那正是原来的缺陷**）");
                CheckTrue(eSave.Contains("没写进存档"),
                          "★ A600：……而且说的是「**没写进存档**」那一支（实得「" + eSave + "」）");
                CheckTrue(eSave.Contains(expectSaveErr),
                          "★ A600：……而且报的是**真原因**（`DeckStore.SaveAll` 那条 `catch` 的**运行时原话**"
                        + "「" + expectSaveErr + "」—— ⛔ 不是我们写死的一句文案）");
                Check(CollectionData.CurrentIndex(), saveTgt,
                      "★ A600：……而内存里**真的换了**那一套 —— 本条说的「失败」专指**没落盘**"
                    + "（内存改了、盘上没改），与 A503 那一节**同一条语义锚**；"
                    + "改坏法：把失败做成「回滚内存里的 `_current`」⇒ 这条红");
                CheckTrue(eRange != eSave, "★ A600：两种失败报的是**两句不同的话**（合成一句「选不了」⇒ 这条红）"
                                         + "\n              ①（没选中）「" + eRange + "」"
                                         + "\n              ②（没落盘）「" + eSave + "」");

                // ---- ①-d 调用点：`DeckInfoPopup` 的 `Select Deck` **真的读了返回值** ----
                //   ⛔ 不是自证：正例与反例**用同一份实现**、只翻「路径」与「`DeckIndex`」两个输入；
                //     反例要求日志里出现 `LastSelectError` 的**原话**（运行时值，不是我们源码里的常量）。
                DeckStore.OverridePath = keepPath;          // 先把路径还回去（正例要在**好路径**上跑）
                var pSelOk = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                win.Manager.OpenWindow(pSelOk);
                // 🆕 **2026-10-13（A646）**：上面那个 `ClickHit` 抓的是**文案**、把**级别**丢了 ⇒ 这一节另配一份
                //   **带级别**的抓取，而且**好路径与失败路径两态都抓** —— 只抓一边分不出「抓取器根本不认级别」
                //   与「级别真的对」（弱断言）。判据 = 「失败走警告级」是 A610/A611 之后本工程统一的口径
                //   （`Shell/DeckInfoPopup.cs` 里 `Blocked()` / 取不到图 / 没登记过这颗钮 一律 `LogWarning`）
                //   ⇒ 期望值是**级别枚举名**（`LogType.Log` / `LogType.Warning`），不是我们自己的常量。
                var okLv = new List<string>();
                Application.LogCallback hOkLv = (c, st2, t) => { if (c != null && c.Contains("[DeckInfo] 已选中「")) okLv.Add(t.ToString()); };
                Application.logMessageReceived += hOkLv;
                ClickHit(pSelOk.Btn("Select Deck"));
                Application.logMessageReceived -= hOkLv;
                CheckTrue(HitCount("[DeckInfo] 已选中「") == 1 && HitCount("[DeckInfo] 选中卡组失败：") == 0,
                          "（控制组）好路径点 `Select Deck` ⇒ 「已选中『…』」**且没有**失败那一句");

                DeckStore.OverridePath = probePath;
                var pSelBad = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                win.Manager.OpenWindow(pSelBad);
                if (pSelBad != null) pSelBad.DeckIndex = n0 + 7;   // ⚠️ 开完窗**之后**才改（建窗/接线跑的是有效下标那一次）
                var selLv = new List<string>();
                Application.LogCallback hSelLv = (c, st2, t) => { if (c != null && c.Contains("[DeckInfo] 选中卡组失败：")) selLv.Add(t.ToString()); };
                Application.logMessageReceived += hSelLv;
                ClickHit(pSelBad != null ? pSelBad.Btn("Select Deck") : null);
                Application.logMessageReceived -= hSelLv;
                Check(HitCount("[DeckInfo] 选中卡组失败："), 1,
                      "★ A600：调用点失败时**出声**（改坏法：退回 `CollectionData.Select(DeckIndex); "
                    + "Debug.Log(\"已选中…\")` ⇒ 这条与下一条**一起红**）");
                string selLog = FirstHit("[DeckInfo] 选中卡组失败：");
                CheckTrue(selLog != null && CollectionData.LastSelectError.Length > 0
                          && selLog.Contains(CollectionData.LastSelectError),
                          "★ A600：……而且打的是 `CollectionData.LastSelectError` 的**原话**"
                        + "（⛔ 不是另写一句 —— 两处写同一条规则 = 迟早不一致）\n              实得：「" + selLog + "」");
                Check(HitCount("[DeckInfo] 已选中「"), 0,
                      "★ A600：……而**不许**再说「已选中」（两句互斥 —— 弱断言分不出这两种状态）");

                // ---- ①-e 收尾 ----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");

                // ---- ①-f 🆕 **2026-10-13（A646）**：**日志的级别**（原来这一句是 `Debug.Log`） ----
                //   ⚠️ 放在**收尾之后**：它只读上面抓下来的 `okLv` / `selLv` 两个表，**不碰任何状态**
                //     （⛔ 别把抓取那两段挪到这儿来 —— 那必须在点钮那一刻挂着）。
                //   A646 的缺陷原文：A611 之后 `Shell/DeckInfoPopup.cs` 里只剩这一处失败走**普通级** ——
                //   它当年用 `Log` 的理由是「与 A548 那两句（删/复制失败）**同通道**」，而 **A610 已把那两句
                //   统一成 `LogWarning`** ⇒ 那条理由失效。⛔ 两条期望值都不是我们自己的常量（`LogType` 枚举）。
                //   **改坏法**：把 `Shell/DeckInfoPopup.cs` 那句改回 `Debug.Log` ⇒ ② 的实得变成「Log」⇒ 红。
                //   ① 是**对照档**：它同时证明抓取器**认得出级别**（否则 ② 是恒绿的假观测）。
                CheckTrue(okLv.Count == 1 && okLv[0] == "Log",
                          "★★ A646（对照档）：**成功**那一句仍是 `Log` 级（⛔ A646 只动失败那一句）"
                        + " —— 实得「" + (okLv.Count == 1 ? okLv[0] : "(没抓到 / 抓到 " + okLv.Count + " 条)") + "」"
                        + "；这一档同时证明抓取器**认得出级别**（不是「恒 Warning」那种假绿）");
                CheckTrue(selLv.Count == 1 && selLv[0] == "Warning",
                          "★★ A646：`Select Deck` **失败走警告级**（`Debug.LogWarning`）"
                        + " —— 实得「" + (selLv.Count == 1 ? selLv[0] : "(没抓到 / 抓到 " + selLv.Count + " 条)") + "」"
                        + "；改回 `Debug.Log` ⇒ 实得「Log」⇒ 红");
            }

            // ---------------- ② A601：`DeckInfoPopup` 的删 / 复制失败 —— 两支各自说得清是哪一支 ----------------
            Section("A601（A548）：`DeckInfoPopup` 的删 / 复制失败 —— **两支各自说得清是哪一支**");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                int n0 = CollectionData.DeckCount();
                CheckTrue(n0 >= 2 && n0 < DeckInfoPopup.MaxCustomDecks,
                          $"（前提）库里 **2 ≤ 套数 < 上限 114**（实得 {n0}）—— `Delete` / `Duplicate` 两颗钮的 "
                        + "`interactable` 才为真（原版 `1 < 卡组数` / `卡组数 < totalCustomDecks`）");

                // ---- ②-a 控制组（好路径）：两支**成功**的日志 ----
                //   缺了它，一个「恒报失败」的实现照样能把下面四条探针全骗绿。
                {
                    string tmp = CollectionData.CreateDeck();
                    int ti = CollectionData.IndexOf(tmp);
                    CheckTrue(ti >= 0, $"（控制组）先建一套临时卡组当靶子（「{tmp}」@{ti}）");

                    var pOk = DeckInfoPopup.Create(win.Manager, ti);
                    win.Manager.OpenWindow(pOk);
                    CheckTrue(pOk.DeleteInteractable && pOk.DuplicateInteractable,
                              "（控制组）靶子那扇窗的 `Delete` / `Duplicate` **都可点**"
                            + "（原版 `1 < 卡组数` / `卡组数 < totalCustomDecks`）");

                    int beforeDup = CollectionData.DeckCount();
                    ClickHit(pOk.Opt("Duplicate"));
                    CheckTrue(HitCount("[DeckInfo] 已复制成「") == 1 && HitCount("[DeckInfo] 复制失败：") == 0,
                              "（控制组）好路径点 `Duplicate` ⇒ 「已复制成『…』」**且没有**「复制失败」"
                            + "（改坏法：把那一支写成恒报失败 ⇒ 这条红）");
                    Check(CollectionData.DeckCount(), beforeDup + 1, "（控制组）……而且库里**真多了一套**");

                    int di = CollectionData.DeckCount() - 1;          // = 刚复制出来那套
                    var pDel = DeckInfoPopup.Create(win.Manager, di);
                    win.Manager.OpenWindow(pDel);
                    int beforeDel = CollectionData.DeckCount();
                    ClickHit(pDel.Opt("Delete"));
                    CheckTrue(HitCount("[DeckInfo] 已删除该卡组") == 1 && HitCount("[DeckInfo] 删卡组失败：") == 0,
                              "（控制组）好路径点 `Delete` ⇒ 「已删除该卡组」**且没有**「删卡组失败」"
                            + "（改坏法：把那一支写成恒报失败 ⇒ 这条红）");
                    Check(CollectionData.DeckCount(), beforeDel - 1, "（控制组）……而且库里**真少了一套**");
                }

                // ---- ②-b 探针（**写不进去的路径**）：同一颗钮的**两支失败**各打一句、且两句不同 ----
                //  判据 = `Shell/DeckInfoPopup.cs` 的 `OnOption`：删失败**直接打** `LastDeleteError`
                //   （它自己分成「没删」与「删了没落盘」两种）；复制失败按**调用前后 `DeckCount()` 变没变**分两支
                //   （与 `CollectionData.DeleteDeck` 分开它那两种 `false` 用的是**同一条**判据 ⇒ 不是新造的一套）。
                DeckStore.OverridePath = Path.Combine(probeDir, "__wf_a601_no_such_dir__", "x.json");
                var probeLib2 = DeckLibrary.Load();
                CheckTrue(!probeLib2.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                string expectSaveErr2 = probeLib2.LastError;
                CheckTrue(!string.IsNullOrEmpty(expectSaveErr2),
                          "（前提）写不进去时**带了原因**（原因空 ⇒ 下面「带上了真原因」那条会**空串恒真**）");

                // ②-b-1 删除 · 「**没删**（下标越界）」那一支
                int nBefore = CollectionData.DeckCount();
                var d1 = DeckInfoPopup.Create(win.Manager, 0);     // ⚠️ 先用**有效下标**开窗（建窗/接线要读到一套真的）
                win.Manager.OpenWindow(d1);
                if (d1 != null) d1.DeckIndex = n0 + 7;              // 开完再改成越界 —— 模拟「下标已失效」那一态
                ClickHit(d1 != null ? d1.Opt("Delete") : null);
                string del1 = FirstHit("[DeckInfo] 删卡组失败：");
                CheckTrue(del1 != null, "★ A601：删失败时**出声**（`[DeckInfo] 删卡组失败：…`）");
                CheckTrue(del1 != null && del1.Contains("没删") && !del1.Contains("没写进存档"),
                          "★ A601（删除 · 越界支）：文案点明是「**没删**」、且**不许**说「没写进存档」"
                        + "\n              实得：「" + del1 + "」");
                Check(CollectionData.DeckCount(), nBefore, "★ A601：……而且**一套都没少**（越界那一支是早退）");

                // ②-b-2 删除 · 「**删了，但没写进存档**」那一支
                var d2 = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(d2);
                ClickHit(d2 != null ? d2.Opt("Delete") : null);
                string del2 = FirstHit("[DeckInfo] 删卡组失败：");
                CheckTrue(del2 != null && del2.Contains("没写进存档") && !del2.Contains("越界"),
                          "★ A601（删除 · 没落盘支）：文案点明是「**没写进存档**」、且**不许**说「越界」"
                        + "\n              实得：「" + del2 + "」");
                CheckTrue(del2 != null && del2.Contains(expectSaveErr2),
                          "★ A601：……而且**带上了真原因**（`DeckStore.SaveAll` 那条 `catch` 的**运行时原话**"
                        + "「" + expectSaveErr2 + "」—— 改坏法：把 `LastDeleteError` 换成写死的文案 ⇒ 这条红）");
                Check(CollectionData.DeckCount(), nBefore - 1, "★ A601：……而内存里**确实少了一套**（两种失败因此分得开）");
                CheckTrue(del1 != del2,
                          "★ A601：**删失败的两支报的是两句不同的话**"
                        + "（改坏法：退回旧那句「删不了（`DeckLibrary.Delete` 的规矩：只剩一套时不许删 / 下标越界）」"
                        + "⇒ 这条红 —— 那一句**在两种情况下都打**，正是原来的缺陷）");

                // ②-b-3 复制 · 「**没复制**（下标越界）」那一支
                int cBefore = CollectionData.DeckCount();
                var u1 = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(u1);
                if (u1 != null) u1.DeckIndex = n0 + 7;
                ClickHit(u1 != null ? u1.Opt("Duplicate") : null);
                string dup1 = FirstHit("[DeckInfo] 复制失败：");
                CheckTrue(dup1 != null, "★ A601：复制失败时**出声**（`[DeckInfo] 复制失败：…`）");
                CheckTrue(dup1 != null && dup1.Contains("没复制") && !dup1.Contains("没写进存档"),
                          "★ A601（复制 · 越界支）：文案点明「**没复制**」、且**不许**说「没写进存档」"
                        + "\n              实得：「" + dup1 + "」");
                Check(CollectionData.DeckCount(), cBefore, "★ A601：……而且库里**一套没多**");

                // ②-b-4 复制 · 「**复制出来了，但没写进存档**」那一支
                var u2 = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(u2);
                ClickHit(u2 != null ? u2.Opt("Duplicate") : null);
                string dup2 = FirstHit("[DeckInfo] 复制失败：");
                CheckTrue(dup2 != null && dup2.Contains("没写进存档") && !dup2.Contains("越界"),
                          "★ A601（复制 · 没落盘支）：文案点明「**没写进存档**」、且**不许**说「越界」"
                        + "\n              实得：「" + dup2 + "」");
                Check(CollectionData.DeckCount(), cBefore + 1, "★ A601：……而内存里**确实多了一套**");
                CheckTrue(dup1 != dup2,
                          "★ A601：**复制失败的两支报的是两句不同的话**"
                        + "（改坏法：退回旧那句「复制失败（`DeckLibrary.Duplicate` 返回空）」—— 那一句把原因"
                        + "**一律归给「返回空」**，两种情况下都打 ⇒ 这条红 —— 正是原来的缺陷）");

                // ---- ②-c 收尾 ----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");
            }

            // ======== 🆕 2026-10-13（A610 · A611）：`DuplicateDeck` 的出口 + 失败日志**是警告级** ========
            //  **判据**：① `资料/普查产出_1013/WSmall1_Deck两尾巴.md` §五·5（A611 的原始出处：A548 当时
            //    `CollectionData.Lib` 是私有的 ⇒ 「复制失败」那一支只能拿**「调用前后 `DeckCount()` 变没变」**
            //    这个**间接判据**去分两种成因 —— 那等于把同一条规则**写了第二份**）·
            //    ② `资料/普查产出_1013/WSmall3_选中断言与两条尾巴.md` §六·2（A610）/ §六·3（「更干净的做法」）·
            //    ③ **成品范本 = `Shell/CollectionData.cs` 现读的 `LastDeleteError`**（A503 定的形状：
            //    开头清 → 按**观测状态**分两支 → `LastXxxError` + 一条 `Debug.LogWarning`）。
            //  **A611 改了**：`CollectionData.DuplicateDeck` 补 `LastDuplicateError`；调用点
            //    （`Shell/DeckInfoPopup.cs` 的 `OnOption`）**直接打它的原话**，`DeckCount()` 那个间接判据**已删**。
            //  **A610 改了**：`OnOption` 里那三支失败（删失败 1 + 复制失败 2，A611 后并成 2 句）从 `Debug.Log`
            //    **统一成 `Debug.LogWarning`**（失败本来就是警告级：`CollectionData` 自己的失败 / `Blocked()` /
            //    `PromptPopup.Click` 全是 `LogWarning`）。⛔ **成功那两句不动**（仍是 `Debug.Log`）。
            //  ⚠️ **两件都**没有原版判据：原版卡组存在**服务器**上（`CardDeck.syncedToServer`，见
            //    `RuleEngine/Data/DeckLibrary.cs` 文件头）⇒ 本地这条链没有原版对应物；判据 = 本项目红线
            //    「⛔ 不许静默失败」+ A503 已经定下的那套口径。⛔ 本件没另立一套。
            //
            //  ⚠️ **这一段必须留在 `Run()` 最后**（同 A503/A600 那两节的理由）：它会拐走 `OverridePath`、
            //    还会增删内存里的卡组。⚠️ **不碰夹具那条存档本身** —— 控制组把它**复制一份**到
            //    `_wf_a611_probe.json`（同一目录），所有**成功**的写盘都落在副本上。
            Section("A610 + A611：`DuplicateDeck` 的出口（失败两支分得清）+ 失败日志**是警告级**");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                CheckTrue(!string.IsNullOrEmpty(keepPath) && File.Exists(keepPath),
                          $"（前提）夹具那条存档在（{keepPath}）—— 控制组要把它**复制一份**，⛔ 不碰它本身");

                // 日志**连级别一起**抓（本节 A610 那几条要断的就是级别）。
                //   ⚠️ 与上面 A600/A601 两节那份 `ClickHit`（只抓正文）**并存**：那一份不在本件白名单内，
                //      ⛔ 一行都不动；两份同时挂着不影响（`ClickHit` 自己加、自己摘）。
                var lvMsg = new List<string>();
                var lvLevel = new List<LogType>();
                Application.LogCallback hLv = (c, s, t) => { if (c != null) { lvMsg.Add(c); lvLevel.Add(t); } };
                Application.logMessageReceived += hLv;
                // 只数**这一次点击之后**新产生的那几条（`mark` = 点之前的条数）
                int LvCount(int mark, string contains, LogType ty)
                {
                    int k = 0;
                    for (int i = mark; i < lvMsg.Count; i++)
                        if (lvLevel[i] == ty && lvMsg[i].Contains(contains)) k++;
                    return k;
                }
                string LvFirst(int mark, string contains)
                {
                    for (int i = mark; i < lvMsg.Count; i++) if (lvMsg[i].Contains(contains)) return lvMsg[i];
                    return null;
                }

                // ---- ① 控制组（好路径）：**直接调出口** —— 挡住「恒报失败 / 恒回空串」那种坏实现 ----
                string goodPath = Path.Combine(probeDir, "_wf_a611_probe.json");
                File.Copy(keepPath, goodPath, true);        // 夹具的**副本**：成功的写盘全落它身上
                DeckStore.OverridePath = goodPath;
                CollectionData.ResetForTest();              // 从副本重读（内容与夹具一致）

                int n0 = CollectionData.DeckCount();
                CheckTrue(n0 >= 1 && n0 < DeckInfoPopup.MaxCustomDecks,
                          $"（前提）副本里 **1 ≤ 套数 < 上限 114**（实得 {n0}）—— `Duplicate` 那颗钮的 "
                        + "`interactable`（原版 `卡组数 < totalCustomDecks`）要为真，否则下面点了不生效");

                int c0 = CollectionData.DeckCount();
                string nmOk = CollectionData.DuplicateDeck(0);
                CheckTrue(!string.IsNullOrEmpty(nmOk),
                          "（控制组）好路径 `DuplicateDeck(0)` 回**新卡组名**（「" + nmOk + "」）—— "
                        + "改坏法：把成功那一支写成恒回空串 ⇒ 这条红");
                CheckTrue(CollectionData.LastDuplicateError.Length == 0,
                          "（控制组）……而且 `LastDuplicateError` 是**空串**（成功不报错）—— "
                        + "改坏法：成功那一支也写错误 ⇒ 这条红");
                Check(CollectionData.DeckCount(), c0 + 1, "（控制组）……而且库里**真多了一套**");
                CheckTrue(File.Exists(goodPath),
                          "（控制组）……而且**真写进了那个探针文件**（" + goodPath + "）—— "
                        + "这条钉住「好路径确实写得进去」，否则上面几条可能是假绿");

                // ---- ② 控制组（好路径）：**走调用点** —— 「成功」那一句**仍然是 `Log`**（A610 只动失败那几支）----
                var pOk = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pOk);
                CheckTrue(pOk != null && pOk.DuplicateInteractable,
                          "（前提）那扇窗的 `Duplicate` **可点**（原版 `卡组数 < totalCustomDecks`；"
                        + "不可点的话下面这一下会被 `Blocked` 拦掉 = 什么都不发生）");
                int c1 = CollectionData.DeckCount();
                int m1 = lvMsg.Count;
                ClickHit(pOk.Opt("Duplicate"));
                CheckTrue(LvCount(m1, "[DeckInfo] 已复制成「", LogType.Log) == 1
                          && LvCount(m1, "[DeckInfo] 复制失败：", LogType.Warning) == 0,
                          "（控制组）好路径点 `Duplicate` ⇒ 「已复制成『…』」**恰好一条、且级别仍是 `Log`**"
                        + "（⛔ A610 只把**失败**改成警告级：把成功那句也改成 `LogWarning` ⇒ 这条红）"
                        + "，且**没有**「复制失败」（那一支写成恒报失败 ⇒ 这条红）");
                Check(CollectionData.DeckCount(), c1 + 1, "（控制组）……而且库里**真多了一套**");

                // ---- ③ A610：**删失败**那一支的级别（越界那一支不碰盘 ⇒ 在好路径上量，零副作用）----
                var pDel = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pDel);
                if (pDel != null) pDel.DeckIndex = CollectionData.DeckCount() + 7;   // 开完再改 = 模拟「下标已失效」
                int c2 = CollectionData.DeckCount();
                int m2 = lvMsg.Count;
                ClickHit(pDel != null ? pDel.Opt("Delete") : null);
                CheckTrue(LvCount(m2, "[DeckInfo] 删卡组失败：", LogType.Warning) == 1,
                          "★ A610：删失败那一句**是警告级**（`Debug.LogWarning`）—— "
                        + "改坏法：退回 `Debug.Log`（A548 当时的写法）⇒ 这条红");
                Check(LvCount(m2, "[DeckInfo] 删卡组失败：", LogType.Log), 0,
                      "★ A610：……而且**不是**普通级（同一句话的级别两种状态互斥，弱断言分不出）");
                Check(CollectionData.DeckCount(), c2, "★ A610：……而越界那一支**一套都没少**（早退，根本没碰盘）");

                // ---- ④ 探针一：**下标越界**（`DeckLibrary.Duplicate` 早退、根本不碰盘 ⇒ 也在好路径上量）----
                int c3 = CollectionData.DeckCount();
                int bad = c3 + 7;
                string rBad = CollectionData.DuplicateDeck(bad);
                string eRange = CollectionData.LastDuplicateError;
                CheckTrue(rBad.Length == 0,
                          "★ A611：下标越界 ⇒ `DuplicateDeck` 回**空串**"
                        + "（🔴 A611 **之前**也是回空串 —— 这条是**前提**、不是新增的能力："
                        + "原来缺的是「空串是哪一种」）");
                CheckTrue(eRange.Length > 0,
                          "（前提）……而且 `LastDuplicateError` 里**真有人话**"
                        + "（空串会让下面那条 `Contains` **恒真** ⇒ 假绿）");
                CheckTrue(eRange.Contains("没复制") && !eRange.Contains("没写进存档"),
                          "★ A611（越界支）：出口说的是「**没复制**」、且**不许**说「没写进存档」"
                        + "（两支合成一句 ⇒ 这条红 —— 那正是原来的缺陷）\n              实得：「" + eRange + "」");
                Check(CollectionData.DeckCount(), c3, "★ A611：……而且库里**一套没多**（越界那一支是早退）");

                var pBad = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pBad);
                if (pBad != null) pBad.DeckIndex = bad;
                int m3 = lvMsg.Count;
                ClickHit(pBad != null ? pBad.Opt("Duplicate") : null);
                CheckTrue(LvCount(m3, "[DeckInfo] 复制失败：", LogType.Warning) == 1,
                          "★ A610 + A611：走调用点、复制失败 ⇒ **恰好一条警告级**的「复制失败」"
                        + "（改坏法：退回 `Debug.Log` ⇒ 这条红）");
                string dupLog1 = LvFirst(m3, "[DeckInfo] 复制失败：");
                CheckTrue(dupLog1 != null && CollectionData.LastDuplicateError.Length > 0
                          && dupLog1.Contains(CollectionData.LastDuplicateError),
                          "★ A611：……而且打的是 `CollectionData.LastDuplicateError` 的**原话**"
                        + "（⛔ 不是在这里拿「`DeckCount()` 变没变」再判一遍 —— A548 当时就是那么写的，"
                        + "那一版**不含**出口的原话 ⇒ 这条红）\n              实得：「" + dupLog1 + "」");
                Check(LvCount(m3, "[DeckInfo] 已复制成「", LogType.Log), 0,
                      "★ A611：……而**不许**再说「已复制成」（两句互斥 —— 弱断言分不出这两种状态）");
                Check(CollectionData.DeckCount(), c3, "★ A611：……走调用点也一样，库里**一套没多**");

                // ---- ⑤ 探针二：**写盘失败**（父目录不存在 ⇒ `File.WriteAllText` 抛 `DirectoryNotFoundException`）----
                //   ⚠️ **不调** `CollectionData.ResetForTest()`：那会按新路径**读成空库**
                //      （`DeckStore.LoadAll` 见 `File.Exists(Path)` 为假 ⇒ 回空表）—— 内存里那份库**要留着**
                //      （它才是「内存里已经多了一套」的观测面）。落盘用的是**调用那一刻**的 `DeckStore.OverridePath`。
                DeckStore.OverridePath = Path.Combine(probeDir, "__wf_a611_no_such_dir__", "x.json");
                var probeLib = DeckLibrary.Load();
                CheckTrue(!probeLib.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                string expectSaveErr = probeLib.LastError;
                CheckTrue(!string.IsNullOrEmpty(expectSaveErr),
                          "（前提）写不进去时**带了原因**（原因空 ⇒ 下面「报的是真原因」那两条会**空串恒真**）");

                int c4 = CollectionData.DeckCount();
                string rSave = CollectionData.DuplicateDeck(0);
                string eSave = CollectionData.LastDuplicateError;
                CheckTrue(rSave.Length == 0, "★ A611：写盘失败 ⇒ `DuplicateDeck` 也回**空串**（两种成因同一个返回值）");
                CheckTrue(eSave.Contains("没写进存档") && !eSave.Contains("越界"),
                          "★ A611（没落盘支）：出口说的是「**没写进存档**」、且**不许**说「越界」"
                        + "\n              实得：「" + eSave + "」");
                CheckTrue(eSave.Contains(expectSaveErr),
                          "★ A611：……而且报的是**真原因**（`DeckStore.SaveAll` 那条 `catch` 的**运行时原话**"
                        + "「" + expectSaveErr + "」—— ⛔ 不是我们写死的一句文案）");
                Check(CollectionData.DeckCount(), c4 + 1,
                      "★ A611：……而内存里**真的多了一套** —— 本条说的「失败」专指**没落盘**"
                    + "（内存改了、盘上没改），与 A503 那一节**同一条语义锚**；"
                    + "改坏法：把失败做成「回滚掉那一套」⇒ 这条红");
                CheckTrue(eRange != eSave,
                          "★ A611：两种失败报的是**两句不同的话**（合成一句「复制不了」⇒ 这条红）"
                        + "\n              ①（没复制）「" + eRange + "」"
                        + "\n              ②（没落盘）「" + eSave + "」");

                var pSave = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pSave);
                int c5 = CollectionData.DeckCount();
                int m4 = lvMsg.Count;
                ClickHit(pSave != null ? pSave.Opt("Duplicate") : null);
                CheckTrue(LvCount(m4, "[DeckInfo] 复制失败：", LogType.Warning) == 1,
                          "★ A610 + A611：走调用点、**没落盘**那一支 ⇒ **恰好一条警告级**的「复制失败」"
                        + "（改坏法：退回 `Debug.Log`，或退回 A548 那条按 `DeckCount()` 分支的写法 ⇒ 这条红）");
                string dupLog2 = LvFirst(m4, "[DeckInfo] 复制失败：");
                CheckTrue(dupLog2 != null && CollectionData.LastDuplicateError.Length > 0
                          && dupLog2.Contains(CollectionData.LastDuplicateError),
                          "★ A611：……而且打的是出口的**原话**（⛔ A548 那一版这里写的是"
                        + "「原因见上面那条 `[CollectionData]` 警告」—— **不含**出口的原话）"
                        + "\n              实得：「" + dupLog2 + "」");
                CheckTrue(dupLog2 != null && dupLog2.Contains(expectSaveErr),
                          "★ A611：……**带上了运行时真原因**（「" + expectSaveErr + "」）—— "
                        + "改坏法：把 `LastDuplicateError` 换成写死的一句文案 ⇒ 这条红");
                CheckTrue(dupLog2 != null && !dupLog2.Contains("越界"),
                          "★ A611：……且**不许**说「越界」（两支互斥）");
                Check(CollectionData.DeckCount(), c5 + 1, "★ A611：……而内存里**确实多了一套**（两种失败因此分得开）");

                // ---- ⑥ 收尾：路径还回去 + 把探针副本删掉（⛔ 夹具那条存档**一个字节都没被碰过**）----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                Application.logMessageReceived -= hLv;
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");
                if (File.Exists(goodPath)) File.Delete(goodPath);
                CheckTrue(!File.Exists(goodPath), "（收尾）控制组那份**副本**删掉了（" + goodPath + "）");
            }

            // ======== 🆕 2026-10-13（A435 阶段 2 · 乙）：裁切状态迁到【视口节点】上 + A465 ========
            //   覆盖本块在收藏线的全部站点：A15（卡池）· A16（卡背）· A17（异画）· A18（左栏筛选栏）
            //   · A19（卡组列表）· B2（筛选栏标题行那记「重新裁一刀」）· A465（`MenuScroll.Intersects` 吃节点）。
            //   契约 → `Shell/ViewportClip.cs` 文件头；逐站点的「设/还原」对照 → `资料/普查产出_1013/A435_迁移表.md`。
            //   🔴 **两个变异源分开下毒**（谁红就知道是哪一档坏）：
            //     ① 挪**节点**的 `localPosition` ⇒ 只动「框在哪」，不动 `Viewport` 字段（A465 的灭自证条）；
            //     ② 收**节点**的 `sizeDelta` ⇒ 只动「框多大」，看**渲出来的真几何**（`ImageQuad.WorldW/H`）。
            //   ⛔ 全段一条计数器都不读（`NodeResolutions` / `NodeShadowedByParam` 都不碰）。
            Section("A435 阶段 2 · 乙：五个视口节点 + A465（构建循环吃节点）");
            {
                win.tabButtons.Click(0);        // 先切到 Deck 页（上面的小节把页签留在别处；节点引用一律在这个之后取）
                // ---- ① 四页各自的 `Viewport` 节点：在不在 / 参数 / **框** ----
                //   框的期望值 = **原版视口矩形字面量**（与上面各节同一批数，这里独立重写一遍：
                //   ⛔ 不回读 `CollectionWindow.*View` 那几个常量 —— 那是被测实现自己的那一份）。
                int[] pgIdx = { 0, 1, 2, 3 };
                string[] pgName = { "Deck", "Cards", "Cosmetics", "Styles" };
                string[] pgPath = { "Deck Scroll View/Viewport", "Scroll View/Viewport",
                                    "Scroll View/Viewport", "Scroll View/Viewport" };
                float[] pgL = { 330.9f, 330.2f, 335.44f, 330.22f };
                float[] pgT = { 155.9f, 155.9f, 155.94f, 287.67f };
                float[] pgR = { 1920f, 1919.9f, 1920.01f, 1920.01f };
                float[] pgB = { 1080f, 1079.9f, 1080f, 1080f };
                for (int k = 0; k < 4; k++)
                {
                    var pgRootK = win.PageRoot(pgIdx[k]);
                    var vpK = pgRootK != null ? pgRootK.Find(pgPath[k]) : null;
                    CheckTrue(vpK != null, $"（前提）`{pgName[k]}` 页的视口节点 `{pgPath[k]}` 在");
                    if (vpK == null) continue;
                    var vcK = vpK.GetComponent<ViewportClip>();
                    CheckTrue(vcK != null,
                              $"★ A435·乙：`{pgName[k]}` 页那颗 `Viewport` 上挂着 `ViewportClip`"
                            + "（= 原版 `Viewport` 上那个 `RectMask2D`）—— 迁移前这里是**裸节点** + 各 `Rebuild*Cells`"
                            + " 里一对「把本窗 `Clip` 设成页视口 → 画 → 还原」（**那一对已整对删掉**）"
                            + "。**改坏法**：把那句 `ViewportClip.Hang(...)` 换回 `Node(...)` ⇒ 本条红");
                    if (vcK == null) continue;
                    Check(vcK.padding, Vector4.zero,
                          $"★ A435·乙：`{pgName[k]}` 页节点 `padding` = (0,0,0,0)（原版那颗 `RectMask2D.m_Padding` 实读）");
                    Check(vcK.softness, Vector2Int.zero,
                          $"★ A435·乙：`{pgName[k]}` 页节点 `softness` = (0,0)（原版 `m_Softness` 实读）");
                    var bxK = vcK.ClipPx;
                    CheckTrue(bxK.HasValue, $"（前提）`{pgName[k]}` 页那颗节点给得出框（它是 `RectTransform` 且有 `sizeDelta`）");
                    if (bxK.HasValue)
                    {
                        CheckNear(bxK.Value.x1, pgL[k], 0.5f, $"★ A435·乙：`{pgName[k]}` 页节点框**左沿** = 原版视口左沿 {pgL[k]}");
                        CheckNear(bxK.Value.y1, pgT[k], 0.5f, $"★ A435·乙：……**上沿** {pgT[k]}");
                        CheckNear(bxK.Value.x2, pgR[k], 0.5f, $"★ A435·乙：……**右沿** {pgR[k]}");
                        CheckNear(bxK.Value.y2, pgB[k], 0.5f, $"★ A435·乙：……**下沿** {pgB[k]}");
                    }
                }

                // ---- ② A465 的**灭自证**条：框的来源是【节点】，不是那个非空 `Viewport` 字段 ----
                //   🔴 为什么这条能灭自证：老的错写法（块 4 建议的 `VisibleAbove(Owner.transform, onScreen,
                //   Viewport)`，或干脆不改）读的都是 `MenuScroll.Viewport` —— 那是一份**与节点无关**的常数，
                //   把节点搬走它**纹丝不动** ⇒ 下面这一条必红。
                {
                    var pgD435 = win.PageRoot(0);
                    var vpD = pgD435 != null ? pgD435.Find("Deck Scroll View/Viewport") : null;
                    var scD = win.DeckScroll;
                    CheckTrue(vpD != null && scD != null, "（前提）卡组页的视口节点 + 滚动区都在");
                    if (vpD != null && scD != null)
                    {
                        var inVp = new PxRect(500f, 400f, 700f, 500f);         // 原视口里的一小块
                        CheckTrue(scD.Intersects(inVp), "（前提）这一小块**落在原视口里** ⇒ 正常态判「可见」");
                        // 🆕 **2026-10-16（A811 根治 · 主对话裁定的补丁）**：毒药从「挪**实时 transform**」改成
                        //   「挪**节点那个框**」—— 根治之后 `ClipPx` 的中心取的是**宿主写进节点的设计矩形**
                        //   （`ViewportClip.BaseRect`）、**不再跟实时 `localPosition` 走** ⇒ 老毒药下错了地方
                        //   （命题仍成立，只是毒在了「框不再取用的那个量」上，会变成假红）。
                        //   改完语义**更强**：毒的是框**真正取用的那个量**。
                        //   出处 → `资料/普查产出_1016/W11_A811根治.md` §③·B。
                        var vpcD = vpD.GetComponent<ViewportClip>();
                        var keepRect = vpcD != null && vpcD.HasBaseRect ? vpcD.BaseRect : (PxRect?)null;
                        CheckTrue(vpcD != null && keepRect.HasValue,
                                  "（前提）那颗视口**有框记录**（`ViewportClip.BaseRect`）—— 根治后毒药的落点");
                        // 🔴 **2026-10-17 订正**：这条毒药**只挪中心** —— 根治后的 `ClipPx` 是「**中心取 `BaseRect`、
                        //    尺寸仍取活节点自己的 `rect`**」（`Shell/ViewportClip.cs:246-267`）⇒ 上面那对尺寸
                        //    **根本不参与**：毒后中心 = (1125.45, 202.95)、半尺寸仍取活的 Deck 视口 (794.55, 462.05)
                        //    ⇒ 毒后框 y = [−259.1, 665.0] **完整罩住** `inVp`=(500,400,700,500) ⇒「不相交」前提假、假红。
                        //    改法 = 把**中心**推到 |cy − [400,500]| > 462.05 之外（⛔ 别只调矩形高度，尺寸本来就不参与）。
                        //    判据 → `资料/普查产出_1016/诊断_收口7条红.md` §7。
                        if (vpcD != null) vpcD.SetBaseRect(new PxRect(330.9f, 155.9f, 1920f, -1600f));   // 与 `inVp` 不相交（中心 y ≈ −722）
                        CheckTrue(!scD.Intersects(inVp),
                                  "★ A465：把**节点那个框**搬走之后，落在 `MenuScroll.Viewport` 里那一块"
                                + "**必须判不可见** —— 这条钉的是「`Intersects` 的框来自**节点**」。"
                                + "**改坏法**：把 `MenuScroll.Intersects` 写回 `MenuDraw.Visible(onScreen, Viewport)`"
                                + "（2026-10-13 之前的老写法）⇒ 立刻红");
                        if (vpcD != null && keepRect.HasValue) vpcD.SetBaseRect(keepRect.Value);
                        CheckTrue(scD.Intersects(inVp),
                                  "（还原）把框放回去 ⇒ 又判可见 —— 这一条同时钉住「上一条不是因为别的原因红的」");
                    }
                }

                // ---- ③ 节点态真的驱动【裁切】：收小节点 ⇒ 重建 ⇒ **渲出来的真几何**跟着变 ----
                //   判据全部独立算：带子高 250（我下的毒）· 卡框上沿 = 格顶 + `DcFrameY(17) × K(0.9)`。
                {
                    var pgD435b = win.PageRoot(0);
                    var vpD2 = pgD435b != null ? pgD435b.Find("Deck Scroll View/Viewport") : null;
                    var holderD = FindChild(win.PageRoot(0), "Deck Scroll View");
                    CheckTrue(vpD2 != null && holderD != null, "（前提）卡组页的节点 + `Deck Scroll View` 都在");
                    if (vpD2 != null && holderD != null)
                    {
                        win.ClearDeckFilters();                    // 前面的小节可能留着筛选 ⇒ 先回到「全列」
                        win.DeckScroll.SetOffset(0f);              // 前面的小节可能滚过 ⇒ 回到顶端
                        win.RebuildDeckCells(holderD);
                        int nDk435 = win.DeckCellCount;
                        CheckTrue(nDk435 > CollectionWindow.DeckCols,
                                  $"（前提）正常态建出 {nDk435} 格 > {CollectionWindow.DeckCols}（6）"
                                + " ⇒ 下面那条「收成一条带」才有鉴别力（≤6 格时第 1 行本来就装得下全部）");
                        var c0 = win.DeckCells.Count > 0 ? win.DeckCells[0] : null;
                        CheckNear(Hpx(FindChild(c0, "Frame")), 368f * 0.9f, 2f,
                                  "（前提）正常态：第 1 格的卡框高 = **331.2**（原版 368 × 显示比例 0.9）—— 没被裁");

                        // 下毒：把**节点框**收成「视口顶端 250px 的一条带」（`MenuScroll.Viewport` 一个字节没动）
                        MenuDraw.ApplyPxRect(vpD2, vpD2.parent,
                                             new PxRect(330.9f, 155.9f, 1920f, 155.9f + 250f));
                        win.RebuildDeckCells(holderD);             // 走生产那条重建路（`DeckScroll.OnChanged` 也指它）
                        Check(win.DeckCellCount, Mathf.Min(CollectionWindow.DeckCols, nDk435),
                              $"★ A435·乙（A19）：把节点框收成顶端 250px 之后**只剩第 1 行**被建"
                            + $"（正常态 {nDk435} 格）—— 改坏法：把 `DeckScroll.ClipNode = deckVp;` 那一句删掉"
                            + "（退回只看 `MenuScroll.Viewport`）或把 `Hang(...)` 换回 `Node(...)` ⇒ 本条红");
                        var c0p = win.DeckCells.Count > 0 ? win.DeckCells[0] : null;
                        CheckNear(Hpx(FindChild(c0p, "Frame")), 250f - 17f * 0.9f, 2.5f,
                                  "★ A435·乙（A19）：第 1 格的卡框被**节点那条带**裁到 **234.7** 高"
                                + "（= 250 − `DcFrameY` 17 × `K` 0.9；正常态 331.2）—— 量的是**渲出来的真值**"
                                + "（`ImageQuad.WorldH × 108`，⛔ 不是计数器）。**改坏法**：节点不生效 ⇒ 回到不裁 ⇒ 331.2 ⇒ 红");

                        MenuDraw.ApplyPxRect(vpD2, vpD2.parent, CollectionWindow.DeckViewport);   // 还原
                        win.RebuildDeckCells(holderD);
                        Check(win.DeckCellCount, nDk435, "（还原）格数回到正常态");
                        var c0r = win.DeckCells.Count > 0 ? win.DeckCells[0] : null;
                        CheckNear(Hpx(FindChild(c0r, "Frame")), 368f * 0.9f, 2f,
                                  "（还原）第 1 格的卡框高回到 **331.2** —— 毒解干净了（上一条不是残留状态蒙对的）");
                    }
                }

                // ---- ④ 「两态」之二：**显式实参**仍然赢（`Resolve` 第 1 支，逐字未改）----
                //   探针挂在**带节点的子树里**、显式给一个「与节点框部分重叠」的框 ⇒ 结果必须按显式那个算。
                //   ⚠️ 这一条会让 `ViewportClip.NodeShadowedByParam` **+1**（设计如此：它是「显式传参盖住节点」
                //   的探测器、**不是缺陷计数**），而那条「== 0」的断言只落在 `RewardsScene.Run`（A489 的时点纪律）
                //   —— 本场景不断它，且每次 `*Scene.Run` 是**独立进程**，不会串到别人那儿。
                {
                    var pgD435c = win.PageRoot(0);
                    var vpD3 = pgD435c != null ? pgD435c.Find("Deck Scroll View/Viewport") : null;
                    CheckTrue(vpD3 != null, "（前提）拿得到一颗带 `ViewportClip` 的节点当代父件");
                    if (vpD3 != null)
                    {
                        var expClip = new PxRect(200f, 50f, 900f, 700f);          // 与节点框（左 330.9）**部分重叠**
                        var want = new PxRect(330.9f, 200f, 900f, 700f);          // 独立算：R ∩ 显式框
                        var probe = MenuDraw.Rect(vpD3, CardArt.Solid(),
                                                  new PxRect(330.9f, 200f, 1200f, 800f), "A435b probe", 3000,
                                                  null, false, expClip, default(Vector2));
                        CheckTrue(probe != null, "（前提）探针件建出来了（`CardArt.Solid()` 取得到）");
                        if (probe != null)
                        {
                            CheckNear(probe.WorldW * 108f, want.W, 2f,
                                      "★ A435·乙（`Resolve` 第 1 支）：**显式 `clip` 形参仍逐字优先**"
                                    + "（宽 = 900 − 330.9 = **569.1**；⛔ 节点框那一档会给 **869.1** = 1200 − 330.9）"
                                    + " —— 这一档管着全壳「不传 `null` 时的老行为」；**改坏法**：把 `Resolve` 的优先级"
                                    + "翻成「节点优先」⇒ 立刻红");
                            CheckNear(probe.WorldH * 108f, want.H, 2f,
                                      "★ A435·乙（`Resolve` 第 1 支）：……高 = 700 − 200 = 500（同样按显式那个框算）");
                            Object.DestroyImmediate(probe.gameObject);
                        }
                    }
                }
            }

            // ============================================================ 🆕 2026-10-09（`A1025`）本批键全 `Loc.HasEntry` + 两语档取真文案
            // 判据（坑表 #18）：**「原版有词条」≠「我们表里有键」** —— 键不在 ⇒ `Loc.T` 返回**键名本身**、界面上就印键名。
            // 🔴 **与 `A1057(f)`（`Editor/NetSelfTest.cs` 那张**表级**扫描）不是同一条、方向相反**：那条是 **表 → 表**
            //   （表自身健康），本条是 **代码 → 表**（**本批代码引的键**有没有落进表）—— 表级扫描永远看不见后者。
            // 🔴 **Net/ 那批已有等价物**（`Editor/NetSelfTest.cs` 的 `TestNetTermBilingual`）⇒ 本笔不重复覆盖它。
            // 数组 = 本批生产文件里出现的**活键字面量** ∩ `Core/Loc.cs` 的表键（超集无害、且更严）。
            // 本宿主的数组 = 上面 `DeckScene` 那批 ∪ `Shell/{DeckInfoPopup,DeckSelectionPopup,ImportDeckPopup,CollectionData,PracticeModePopup}.cs`。
            // 🔴 **`MenuDeck/Share/ExportSuccesful` 【不在】本数组**：它在代码里被引（`Shell/DeckInfoPopup.cs:1438`）、表里没有 ——
            //   但走的是 `ErrorMessageBanner.ShowMessage` 的 `fallback` 支（**界面不印键名、不是缺陷**），正主是 `A1050`。
            // 🧨 改坏法：① `Loc.cs` 删掉本批任一条键 ⇒ ①红；② 某条**英文列**填中文/全角空格 ⇒ ③红；
            //   ③ **中文列**清空 ⇒ ②红；④ 值改成键名本身 ⇒ ②红；⑤ 表删掉一半 ⇒ ④红（`EntryCount` 掉到基线之下）；
            //   ⑥「把实现与期望一起改回写死中文 **并** 把表里那条删掉」⇒ ①红（数组里那条键仍在、`HasEntry` 假）—— 这正是本数组存在的唯一理由。
            {
                string[] a1025Keys =
                {
                    "Battle/Tips/EnergyCost", "Card_Race/Warlord", "Card_Rarity/Common",
                    "Card_Rarity/Epic", "Card_Rarity/Legendary", "Card_Rarity/Rare",
                    "Card_Rarity/Special", "MainMenu/General/Confirm", "MainMenu/General/OK",
                    "MainMenu/MainButtons/ButtonLabel/Back", "MenuCollection/NoCardsFound", "MenuCollection/NoDecksFound",
                    "MenuDeck/Button/Random", "MenuDeck/CantImportDeck", "MenuDeck/DefaultDeckName",
                    "MenuDeck/DemoDeckName", "MenuDeck/Error/CantStartNoWarlord", "MenuDeck/Error/HiddenCards",
                    "MenuDeck/Error/ImportBadString", "MenuDeck/Error/ImportEmpty", "MenuDeck/Error/ImportNotPersisted",
                    "MenuDeck/Error/InvalidDeck", "MenuDeck/Error/NoUsablePrebuilt", "MenuDeck/Error/NoWarlord",
                    "MenuDeck/Error/PrebuiltMissing", "MenuDeck/Error/SaveFailed", "MenuDeck/Filters/Army",
                    "MenuDeck/Filters/ClearFilters", "MenuDeck/Filters/Filters", "MenuDeck/Filters/ShowOwnedOnly",
                    "MenuDeck/Filters/ShowUpgradableOnly", "MenuDeck/Filters/Type", "MenuDeck/HUD/DeckDescription/DeckInfo",
                    "MenuDeck/HUD/DeckDescription/Minions", "MenuDeck/HUD/DeckDescription/Spells", "MenuDeck/HUD/DiscardChanges",
                    "MenuDeck/HUD/DragCardsTip", "MenuDeck/HUD/EditDeckName", "MenuDeck/HUD/EnterText",
                    "MenuDeck/HUD/NoArmySelected", "MenuDeck/HUD/Rarity", "MenuDeck/HUD/SearchFilter",
                    "MenuDeck/MenuButtons/Done", "MenuDeck/NewDeckName", "MenuDeck/Share/PasteDeck",
                    "MenuDeck/Tip/SelectDeckAgainst", "MenuShop/ShopItemType/Cards", "MenuShop/ShopItemType/Cosmetics",
                    "Settings/Online/MatchCancelFailed", "Settings/Online/MatchCancelled",
                };
                foreach (var k in a1025Keys)
                    CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");
                // ② 两语档各取一次、都非空且 ≠ 键名（只断中文档 = 半边绿）
                var a1025LangWas = Loc.Current;
                foreach (var lang in new[] { AvailableLanguages.Chinese, AvailableLanguages.English })
                {
                    Loc.RestoreForTest(lang);
                    int bad = 0; string firstBad = null;
                    foreach (var k in a1025Keys)
                    {
                        string v = Loc.T(k);
                        if (string.IsNullOrEmpty(v) || v == k) { bad++; if (firstBad == null) firstBad = k; }
                    }
                    CheckTrue(bad == 0, $"★（A1025）`{lang}` 档下 {a1025Keys.Length} 条**全部取到真文案**（缺 {bad} 条"
                              + (firstBad == null ? "" : $"，第一条 `{firstBad}`") + "）"
                              + " —— 取不到时会印**键名本身**，那就是静默失败");
                }
                Loc.RestoreForTest(a1025LangWas);      // ⛔ 只改内存、不写 `PlayerPrefs`（不是 `SetLanguage`）
                // ③ 灭自证 C1：英文列不许含汉字（`Loc.HasCjk` 的区间含 `0x3000-0x303F` 与 `0xFF00-0xFFEF`）
                {
                    int cjk = 0; string firstCjk = null;
                    foreach (var k in a1025Keys)
                    {
                        string en = Loc.EnOf(k);
                        if (!string.IsNullOrEmpty(en) && Loc.HasCjk(en)) { cjk++; if (firstCjk == null) firstCjk = k; }
                    }
                    CheckTrue(cjk == 0, $"★（A1025）本批 {a1025Keys.Length} 条的**英文列无 CJK**（坏 {cjk} 条"
                                      + (firstCjk == null ? "" : $"，第一条 `{firstCjk}`") + "）");
                }
                // ④ 灭自证 D（表基线）：开工前实测 `EntryCount == 429`
                CheckTrue(Loc.EntryCount >= 429,
                          $"★（A1025）表基线：`Loc.EntryCount` = {Loc.EntryCount} ≥ **429**（开工前实测）"
                        + "｜🧨 把键删掉、断言也一起删 ⇒ 这条红");
            }

            int total = _sink.Pass + _sink.Fail;
            if (_sink.Fail == 0) Debug.Log(P + $"=== 结束：{_sink.Pass}/{total} 全过 ✅ ===");
            else
            {
                var sb = new System.Text.StringBuilder(P + $"=== 结束：{_sink.Pass}/{total} 通过，**{_sink.Fail} 条失败** ❌ ===");
                // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是失败表的【重列】（`Check` 里已经逐条打过）
                //   ⇒ 行首标记 = `失败重列：`，⛔ 不再是 `✗`（原来是 `✗` 时日志里 `✗` 行数 = 失败数 ×2）。
                foreach (var f in _sink.Failures) sb.Append("\n").Append(P).Append("   失败重列：").Append(f);
                Debug.LogError(sb.ToString());
            }
            if (Application.isBatchMode) EditorApplication.Exit(_sink.Fail == 0 ? 0 : 1);
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
