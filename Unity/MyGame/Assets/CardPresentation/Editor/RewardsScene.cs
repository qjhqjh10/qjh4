// RewardsScene.cs — 「日常」奖励窗口的**自检入口**（阶段二第 2 层）
//
// 用法：… -executeMethod RewardsScene.Run        自检（结构 + 版面 + 交互 + 截图），退出码 0 = 全过
//
// 🔴 **每一条断言的期望值都盯「原版值」，不是盯我们自己写的常量**（否则就是自证）——
//    期望值来自 `工具/menu_rect.py`（**独立于 C# 的第二份实现**，直接回原始 JSON 复算锚点链）。
//    两边对不上 = 有一边错了，而不是「改断言让它变绿」。
//
// ⚠️ **为什么没有 `BuildAndSaveScene`**：原版整套菜单**只有 2 个场景**，其余全是 prefab 窗口
//    （`资料/日常_原版规格.md` §〇）。奖励窗是**挂在 `Shell` 的三个锚点上的窗口**，
//    由主菜单的 REWARDS 钮开 ⇒ **不该有自己的场景**。要真 Play 就从 `Shell` 里点进去。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RewardsScene
{
    const string P = "[Rewards] ";
    const string ShotDir = "d:/4/_tmp_view/rewards";

    /// <summary>🆕 2026-10-18（第四会话）：断言计数器 + 输出口径**收口到共用件 `Editor/MenuCheck.cs`**
    /// （唯一实现处；本文件只剩同名的一行转发 ⇒ 5,534 个调用点一个字没动）。
    /// 🔴 **逐宿主一份 `CheckSink`**（⛔ 不是全局 static）—— 「拿别处的 `Check` 去断，失败会
    /// **记进别人的合计**里 ⇒ 静默」，判据见 `Editor/RewardWindowFixture.cs:12-14`。</summary>
    static readonly CheckSink _sink = new CheckSink(P)
    {
        Near = MenuNearStyle.Compact2,
        BlankNote = MenuBlankNote.EmphThisOne,   // 本文件原来那句「（**这一张按已知情况放行**）」
    };

    static void Section(string t) => MenuCheck.Section(_sink, t);

    static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);

    static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what) => MenuCheck.HoverSwap(_sink, root, what);

    static void CheckNoMissingSwapArt(string what) => MenuCheck.NoMissingSwapArt(_sink, what);

    static void CheckNear(float got, float want, float tol, string msg) => MenuCheck.Near(_sink, got, want, tol, msg);

    /// <summary>🆕 2026-10-03：一个节点**在世界里的位置 → 画布像素中心**，与期望的原版像素点比（±`tol`px）。
    /// 判据 = `LayoutSpace.ToPixel`（`PointerLayer` 命中用的是同一条换算 —— 所以这里量的就是「真鼠标会落在哪」）。</summary>
    static void CheckNearPx(Transform t, float xPx, float yPx, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var px = LayoutSpace.ToPixel(t.position);
        CheckNear(px.x, xPx, 0.5f, what + " 的中心 x(px)");
        CheckNear(px.y, yPx, 0.5f, what + " 的中心 y(px)");
    }

    /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1 px）。**期望值必须来自原版像素矩形。**</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>一张图**渲出来的像素矩形**（用 `WorldW/WorldH` —— TMP/材质真值，不是回读我们传进去的数）。</summary>
    static void CheckRectPx(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldW * 108f, x2 - x1, 2.0f, what + " 宽(px)");
        CheckNear(q.WorldH * 108f, y2 - y1, 2.0f, what + " 高(px)");
    }

    /// <summary>只比**渲出来的高**（宽由别的判据管）。</summary>
    static void CheckH(Transform t, float wantHpx, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldH * 108f, wantHpx, 2.0f, what + " 高(px)");
    }

    /// <summary>只比**渲出来的宽**。</summary>
    static void CheckW(Transform t, float wantWpx, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldW * 108f, wantWpx, 2.0f, what + " 宽(px)");
    }

    /// <summary>一个 `ImageQuad` 当前的 **tint alpha**（= 它那份材质上的 color.a）。
    /// 用来验「靠 alpha 显隐」的件（原版 `UiBadgeNotification` 那条路）—— 它们 **`activeSelf` 恒为 true**，
    /// 所以 `CountVisible` 那种判据**看不见差别**。</summary>
    static float AlphaOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return float.NaN;
        var mr = q.GetComponent<MeshRenderer>();
        return mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.color.a : float.NaN;
    }

    /// <summary>一个 `ImageQuad` 当前的 **tint 颜色**（= 它那份材质上的 color）。
    /// 用来验「按状态染色」的件（战役节点那六种状态色就是这条路）。</summary>
    static Color TintOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return new Color(0f, 0f, 0f, 0f);
        var mr = q.GetComponent<MeshRenderer>();
        return mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.color : new Color(0f, 0f, 0f, 0f);
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

    /// <summary>🆕 **2026-10-18（B5）**：关窗钮**圆底盘**那颗 quad 的**实绘矩形**两连断 ——
    /// 补 `A1125Close` 上面那个**静默缺口**（那四格读的是：命中区 quad 的矩形 / 命中区中心 vs 见面中心 /
    /// 可见面的**节点名** / 可见面的**贴图名** —— **没有一格量圆底盘的矩形成像**。
    /// 因为圆底盘等比后**仍居中**，连「中心」那一格都抓不住它）。
    /// <para>🔴 **量的是【根节点自己身上】那颗 quad**（原版 `Generic Close Button Orange` 的 `Image`
    /// 就长在**根节点**自己身上，⛔ 不是自造子件）—— 所以用 `GetComponent` 而**不是** `GetComponentInChildren`：
    /// 后者会把「搬回自造子件 `Base`」这个偏离**静默量成合格**。</para>
    /// <para>⛔ **期望值来自原版 prefab 的读数**（逐字段直读，不是推的
    /// → `资料/普查产出_第八会话/B4_InboxWindow圆底盘归真.md` §1·2 / §1·3）：
    /// 三颗都是 `m_PreserveAspect = 1` · `m_Type = 0`(Simple) · 根那一格 **74.39×75.60** ·
    /// 贴图 `UI_Button_Round_background` 的 sprite `m_Rect` = **237×237（正方）**、`m_Border` / `m_Offset` 全 0
    /// ⇒ 等比内接进「根那一格」= **实绘 74.39×74.39**（高被缩到宽那一档）。
    /// 我们这侧导入的 PNG 也是 237×237（实读 PNG 头）⇒ 落到 `CardbackFace.Fit` 的兜底支 =
    /// 「拿贴图自身宽高当 `m_Rect`」⇒ 同值。⚠️ 但本格读数走 `MenuDraw.QuadRectPx`（**渲染几何**），
    /// ⛔ 期望值**不从被测实现里读回来**（那是自证）。</para>
    /// <para>`frameW` / `frameH` = **根那一格**（`InboxWindow.CloseBtn` = 74.38×75.60）——
    /// 它钉的是「**量的是根那一格**」这件事：⛔ 传子件框 `CloseBg`（56.86×58.13）就不是原版那一格了。</para>
    /// <para>🔴 **两格各钉一轴**（① 实绘**宽** = `basePx`；② 实绘**宽 == 实绘高** = 等比不变量）——
    /// 这样两种改坏法**各红不同的一格**、**结构上不可能一起变绿**：
    /// `CloseBtn`→`CloseBg` ⇒ 只 ① 红（56.86 仍是正方 ⇒ ② 绿）；去掉 `keepAspect` ⇒ 只 ② 红（宽没变 ⇒ ① 绿）。
    /// ⛔ **别把高度也塞进 ①** —— 那会让两种改坏法都红 ①、② 就没有独立作用了（弱断言 / 分不出两种状态）。</para>
    /// <para>🆕 **2026-10-18（第九会话 P6 · `A1149` 第二半）取法扩成「先根后子」**：① 先取**根节点自己**身上那一颗
    /// （= 原版结构）；② 根上没有、且调用点**显式给了**子件名 `baseChild` 时才退一步取 `btn/&lt;baseChild&gt;` 那一颗
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
        // 🔴 **本格【只钉宽】**（高由下面那格「宽==高」钉）—— 两格各钉一轴，**改坏法才各红一格**：
        //    `CloseBg` ⇒ 只有本格红（宽 56.86 ≠ 74.38，而 56.86 仍是正方 ⇒ 下格绿）；
        //    去掉 `keepAspect` ⇒ 只有下格红（宽 74.38 没变 ⇒ 本格绿）。
        //    ⛔ 别把高度也塞进本格 —— 那会让两种改坏法都红本格，下格就没有独立作用了。
        CheckTrue(okB && Mathf.Abs(bw - basePx) <= 0.5f,
                  $"★★ A1125 {win}：**圆底盘实绘宽** = 原版 **{basePx:F2}**（设计 px；配下面那格「宽==高」"
                + $"⇒ 两条一起 = 原版 **{basePx:F2}×{basePx:F2}**）"
                + (okB ? $"（现读宽 {bw:F2}，高 {bh:F2}）" : "（圆底盘 quad 取不到）")
                + $"｜= 原版根那一格 **{frameW:F2}×{frameH:F2}** 的宽（正方贴图 + `m_PreserveAspect=1` 的等比内接）"
                + "｜🧨 把矩形改回子件框 `CloseBg`（56.86×58.13）⇒ 宽少 ~17.5 ⇒ **只本格红**"
                + "（下面「宽==高」那格**仍绿** —— 56.86 也是正方）");
        // 🔴 **本格 = 等比这条不变量**（比断绝对数抗「将来换贴图」：换成别的正方贴图它照旧成立），
        //    而且**只有【去掉 `keepAspect`】这一种改坏法会红它** ⇒ 与上格**结构上不可能一起变绿**。
        CheckTrue(okB && Mathf.Abs(bw - bh) <= 0.5f,
                  $"★★ A1125 {win}：圆底盘**实绘宽 == 实绘高**（= **等比**这条不变量；原版根那一格本身是 "
                + $"{frameW:F2}×{frameH:F2} 的框 ⇒ 只有真等比才两轴相等）"
                + (okB ? $"（现读 {bw:F2}×{bh:F2}）" : "（圆底盘 quad 取不到）")
                + $"｜🧨 把 `keepAspect: true` 去掉 ⇒ 实绘变框那一格 {frameW:F2}×{frameH:F2}"
                + (Mathf.Abs(frameW - frameH) > 0.5f ? "（**宽≠高**）⇒ **只本格红**（宽没变 ⇒ 上一格绿）"
                                                     : "（= 同值）⇒ 本窗**框本身正方**，去掉 `keepAspect` 本就无差别")
                + "｜🔴 这就是「它用的是**根那一格** + 等比」的判别式 —— 少了等比，高度就顶到框高");
    }

    /// <summary>**按路径**找一个节点（`Content/Scroll View/Viewport/…`）。
    /// 🔴 2026-09-23 踩过：`FindChild` 是**按名字**找的（`GetComponentsInChildren` + `name ==`），
    /// **不认识 `A/B/C` 这种写法** —— 传路径进去**永远返回 null**，而断言只会报「不成立」，
    /// 看着像「这个件没建」，其实是找法错了。要路径就用这个。</summary>
    static Transform FindPath(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    /// <summary>一个**有渲染尺寸**的节点的宽度（画布像素）。取它子树里第一个 `ImageQuad` 的 `WorldW`
    /// （**渲染真值**，不是回读我们传进去的数）。</summary>
    static float Wpx(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        return q != null ? q.WorldW * 108f : 0f;
    }

    /// <summary>一个节点子树里**第一个 `ImageQuad` 的渲染队列**（= `PointerLayer` 命中时实际用的那一份）。
    /// 🆕 A36-⑧：队列断言要**量真值** —— 没有 quad 时返回 `int.MinValue`（等于在吼「这件没建出来」，
    /// 比返回 0 安全：0 会被 `a &lt; b` 悄悄判成真）。</summary>
    static int QueueOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>(true) : null;
        return q != null ? q.RenderQueue : int.MinValue;
    }

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

    /// <summary>一棵树里**渲出来的最高渲染队列**（含未激活的件 —— 它们也是这一扇窗的分层）。
    /// 一个 quad 都没有 ⇒ `int.MinValue`。见 `QueueOf` 的注释。</summary>
    static int MaxQueue(Transform root)
    {
        if (root == null) return int.MinValue;
        int m = int.MinValue;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            if (q != null && q.RenderQueue > m) m = q.RenderQueue;
        return m;
    }

    /// <summary>世界 x → 画布像素 x。`LayoutSpace` 是「可见高固定 10 单位、按 16:9 设计」⇒ ×108 + 960。
    /// （`FromPixel` 的逆：`worldX = (px/1920 − 0.5) × 17.7778`。）⚠️ **这个只能用在 x 上。**</summary>
    static float PxOf(float worldX) { return worldX * 108f + 960f; }

    /// <summary>世界 y → 画布像素 y。**y 是反的**（像素 y 向下）⇒ `540 − worldY × 108`。
    /// 🔴 2026-09-23 踩过：拿 `PxOf`（x 的换算）去量 y，得出「节点整体偏下 163px」的**假警报**。</summary>
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
    /// 本助手读的是 TMP 自己的 `textBounds`（mesh 的**活值**），**不在实现那条链上**。</para>
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

    /// <summary>一段文字**当前渲染网格**的全部顶点，换算成画布像素（左上原点）。
    /// 🆕 2026-10-04 加：判「文字有没有被裁到框内」只能量**网格顶点** ——
    /// 节点位置（`CheckAt`）和 `Label.WorldW`（`textBounds`）都**不随裁切变**，量不到。
    /// ⚠️ 读的是 `textInfo.meshInfo[i].vertices`（= `UpdateVertexData` 上传的那份数组，同一个引用）。
    /// <para>🔴 **2026-10-16（A844）逐处判过：与 `ShellScene.TmpSpanPx` 是【两条口径】、有意不收** ——
    /// ① 本族要的是**逐点序列**（位置 / 位置+alpha / uv 宽），而那一份只给**外接框**。
    /// ② ⚠️ **2026-10-18（A851）更正：原来这里写「本族三处网格槽**写死 `meshInfo[0]`**、是语义差异、
    ///    要改得另开一笔、并且必须先跑一次」—— 当天就改了**（判据 / 改坏法 → `资料/普查产出_1018/S2_A851槽号.md`）：
    ///    三处一律改成按**字形自己的** `chr[i].materialReferenceIndex` 取槽，取法照 `Editor/ShellScene.cs`
    ///    的 `SpanOfTmp` / 那份**同名** `TmpVertsAndAlpha`。
    ///    **改前为什么是缺陷（且静默）**：`characterInfo[i].vertexIndex` 是**按材质槽**编的 ⇒ 写死 0 时，
    ///    一行里混了拉丁+汉字（两个槽）会把后面的字**串到 0 号槽同下标的字的顶点上** —— 那个索引看着
    ///    还在数组内、量出来还「像个数」，不报错。它当年**故意**没抄本文件这一处，就是为躲开这个坑。
    ///    **改前为什么没爆**：那 8 个调用点量的是纯拉丁串（`Warpforge Offline Rulebook`）/ 数量数字
    ///    / streak 的英文字 ⇒ 都落在单槽 ⇒ 逐位同值；缺陷是**潜伏的**（哪天量一颗中英混排的字就错）。
    ///    ⚠️ 与 `ShellScene.TmpVertsAndAlpha` 现在只剩**一处**有意差别（逐角跳过 vs 整字跳过，见下面它自己的 doc）。</para>
    /// 🔴 A490 口径：这一笔**改的是量法**（读数 → 量法）⇒ 落地必须真跑一次 `RewardsScene.Run`。</summary>
    static Vector2[] TmpVertPx(Label lb)
    {
        var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null || tmp.textInfo == null || tmp.textInfo.meshInfo == null
            || tmp.textInfo.meshInfo.Length == 0 || tmp.textInfo.characterInfo == null) return null;
        // 🔴 **2026-10-18（A851）**：网格槽按**字形自己的** `materialReferenceIndex` 取（原来是写死 `[0]`，
        //    一行里混拉丁+汉字时会把字串到别的字的顶点上）。取法照 `Editor/ShellScene.cs` 的 `SpanOfTmp`。
        var meshes = tmp.textInfo.meshInfo;
        var chr = tmp.textInfo.characterInfo;
        // 「一个能用的槽都没有 ⇒ 回 null」这条**口径不变**（改前只看 0 号槽 —— 单槽时两种写法逐位同值）
        bool anyMesh = false;
        for (int s = 0; s < meshes.Length; s++) if (meshes[s].vertices != null) { anyMesh = true; break; }
        if (!anyMesh) return null;
        var list = new List<Vector2>();
        int n = Mathf.Min(tmp.textInfo.characterCount, chr.Length);
        for (int i = 0; i < n; i++)
        {
            if (!chr[i].isVisible) continue;
            int slot = chr[i].materialReferenceIndex;
            if (slot < 0 || slot >= meshes.Length) continue;   // 槽号越界 ⇒ 这个字不出点（⛔ 不静默落回 0 号槽）
            var mi = meshes[slot];
            if (mi.vertices == null) continue;
            int vi = chr[i].vertexIndex;
            for (int k = 0; k < 4; k++)
            {
                int idx = vi + k;
                if (idx < 0 || idx >= mi.vertices.Length) continue;
                list.Add(LayoutSpace.ToPixel(tmp.transform.TransformPoint(mi.vertices[idx])));
            }
        }
        return list.ToArray();
    }

    /// <summary>🆕 **2026-10-11（批次1 · 戊2 · A302）**：一段 TMP 文字**渲出来的顶点**（画布像素）与
    /// **那一份顶点色的 alpha**（0..1），**按同一个序**（逐点对得上）。
    /// <para>判据：`MenuDraw.ClipTmpMesh` 写的就是 `textInfo.meshInfo[i].colors32`（同一个数组引用 ——
    /// `UpdateVertexData` 把它推给渲染网格），而 `TmpVertPx` 读的是同一份的 `vertices`。</para>
    /// 🔴 **必须与【裁切】用同一个集合**：只认 `isVisible` 的字（`Shell/MenuDraw.cs` 的 `ClipTmpMesh` 里那句 `if (!ch.isVisible) continue;` 那句
    /// `if (!ch.isVisible) continue;`）—— TMP 给不可见的字写四角全 0、却**照占 4 个槽**
    /// （`资料/已知的坑.md` 2026-10-11（F3）那条）⇒ 扫全数组会把「画不出来的点」当成「画到视口外」（假红）。
    /// 🔴 **2026-10-16（A844）：与 `ShellScene.TmpSpanPx` 是两条口径、有意不收**（逐点序列 vs 外接框）。
    /// ⚠️ **2026-10-18（A851）更正**：原来这里还写着「本处写死 `meshInfo[0]`」—— **已改**：网格槽按字形自己的
    /// `chr[i].materialReferenceIndex` 取（理由 / 改坏法见上面 `TmpVertPx` 的 doc）。与 `Editor/ShellScene.cs`
    /// 那份**同名** `TmpVertsAndAlpha` 现在只剩**一处**有意差别：本处**逐角**跳过越界的角（`k` 那一层 `continue`），
    /// 那一份**整字**跳过 —— 那一份要保住「每字四角、`i % 4` 就是那个角」这个步长（它的 `RowAlphaRange` 靠它
    /// 分「上沿那一行 / 下沿那一行」），本处不需要那个步长 ⇒ **这一处差异是有意保留的，别照着改**。</summary>
    /// <param name="px">出参：每个可见字四角的画布像素（BL·TL·TR·BR …）。</param>
    /// <param name="alpha">出参：同一个序的 alpha（0..1）。</param>
    /// <returns>顶点个数；**取不到网格回 −1**（⛔ 别回 0 —— 那会与「一个字都没有」撞上）。
    /// A851 起这句判的是**所有材质槽**（不再是只看 0 号槽）。</returns>
    static int TmpVertsAndAlpha(Label lb, List<Vector2> px, List<float> alpha)
    {
        px.Clear(); alpha.Clear();
        var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null || tmp.textInfo == null || tmp.textInfo.meshInfo == null
            || tmp.textInfo.meshInfo.Length == 0 || tmp.textInfo.characterInfo == null) return -1;
        // 🔴 **2026-10-18（A851）**：网格槽按**字形自己的** `materialReferenceIndex` 取（原来是写死 `[0]`）。
        //    ⚠️ 本节的牙口是「**这个角的位置** ↔ **这个角的 alpha**」⇒ 这两个通道必须来自**同一份** `meshInfo`；
        //    写死 0 时只要有一个字落在别的槽里，配出来的对就是错的、而且**静默**。
        var meshes = tmp.textInfo.meshInfo;
        var chr = tmp.textInfo.characterInfo;
        // 「一个能用的槽都没有 ⇒ 回 −1」这条口径不变（改前只看 0 号槽；单槽时两种写法逐位同值）
        bool anyMesh = false;
        for (int s = 0; s < meshes.Length; s++)
            if (meshes[s].vertices != null && meshes[s].colors32 != null) { anyMesh = true; break; }
        if (!anyMesh) return -1;
        int n = Mathf.Min(tmp.textInfo.characterCount, chr.Length);
        for (int i = 0; i < n; i++)
        {
            if (!chr[i].isVisible) continue;
            int slot = chr[i].materialReferenceIndex;
            if (slot < 0 || slot >= meshes.Length) continue;   // 槽号越界 ⇒ 这个字不出点（⛔ 不静默落回 0 号槽）
            var mi = meshes[slot];
            if (mi.vertices == null || mi.colors32 == null) continue;
            int vi = chr[i].vertexIndex;
            for (int k = 0; k < 4; k++)
            {
                int idx = vi + k;
                if (idx < 0 || idx >= mi.vertices.Length || idx >= mi.colors32.Length) continue;
                px.Add(LayoutSpace.ToPixel(tmp.transform.TransformPoint(mi.vertices[idx])));
                alpha.Add(mi.colors32[idx].a / 255f);
            }
        }
        return px.Count;
    }

    /// <summary>🆕 **A302**：一个抽屉节点里**数量那段字**的 `Label`（名字 = `ItemDrawer.NodeQuantity`）。
    /// ⚠️ 走 `FindChild`（递归）而不是 `transform.Find("Quantity")` —— `transform.Find` 只找**直接子件**。</summary>
    static Label QuantityLabelOf(Transform drawer)
    {
        var q = FindChild(drawer, ItemDrawer.NodeQuantity);
        return q != null ? q.GetComponentInChildren<Label>() : null;
    }

    /// <summary>逐字量 TMP 的 **uv 宽度**（`u(TR) − u(BL)`，按可见字符顺序）。
    /// 🆕 2026-10-04 加：判「文字被切时 uv 有没有跟着截」。
    /// ⚠️ **这个数本身就是 0.02~0.05 的量级**（字形在图集里只占一小块）⇒ 绝不能单独拿它去断
    /// 「&lt; 1」（那是恒真的假断言）；**必须与「同一个字、没被裁切时」的那个数比**。
    /// 🔴 **2026-10-16（A844）：与 `ShellScene.TmpSpanPx` 是两条口径、有意不收**（要的是 uv 宽）。
    /// ⚠️ **2026-10-18（A851）更正**：原来这里还写着「本处写死 `meshInfo[0]`」—— **已改**：网格槽按字形自己的
    /// `chr[i].materialReferenceIndex` 取（理由 / 改坏法见上面 `TmpVertPx` 的 doc）。</summary>
    static float[] TmpGlyphUvW(Label lb)
    {
        var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null || tmp.textInfo == null || tmp.textInfo.meshInfo == null
            || tmp.textInfo.meshInfo.Length == 0 || tmp.textInfo.characterInfo == null) return null;
        // 🔴 **2026-10-18（A851）**：网格槽按**字形自己的** `materialReferenceIndex` 取（原来是写死 `[0]`）。
        var meshes = tmp.textInfo.meshInfo;
        var chr = tmp.textInfo.characterInfo;
        // 「一个能用的槽都没有 ⇒ 回 null」这条口径不变（改前只看 0 号槽；单槽时两种写法逐位同值）
        bool anyMesh = false;
        for (int s = 0; s < meshes.Length; s++) if (meshes[s].uvs0 != null) { anyMesh = true; break; }
        if (!anyMesh) return null;
        var list = new List<float>();
        int n = Mathf.Min(tmp.textInfo.characterCount, chr.Length);
        for (int i = 0; i < n; i++)
        {
            if (!chr[i].isVisible) continue;
            int slot = chr[i].materialReferenceIndex;
            if (slot < 0 || slot >= meshes.Length) continue;   // 槽号越界 ⇒ 这个字不出数（⛔ 不静默落回 0 号槽）
            var mi = meshes[slot];
            if (mi.uvs0 == null) continue;
            int vi = chr[i].vertexIndex;
            if (vi < 0 || vi + 3 >= mi.uvs0.Length) continue;
            list.Add(Mathf.Abs(mi.uvs0[vi + 2].x - mi.uvs0[vi].x));   // BL 的 u ↔ TR 的 u
        }
        return list.ToArray();
    }

    /// <summary>🆕 **2026-10-16（A796′）换口**：一个字件**渲出来那一块**的宽/高（画布像素）。
    /// <para>判据 = TMP 自己的 `textBounds`（= <see cref="TmpRenderedRect"/> 那条口，**mesh 的活值**）；
    /// 量不到（点阵后端 / 底下没有 TMP 网格）⇒ **退回旧口** `Label.WorldW/H` 并返回 `false`
    /// —— 那条路**本来就没有** `textBounds`（`Label.WorldW` 的点阵支读的是 `_texW`），不是「静默吞掉新口」。</para>
    /// <para>🔴 **为什么要换**（灭自证）：`Label.WorldW/H` 读的是**字段缓存** `_tmpW/_tmpH`，而那份缓存
    /// **只有 `RefreshBounds()` 写**（`Battle/Label.cs`）= **被测实现自己**；而 `SetFontSize` / `SetCharSpacing`
    /// 那一族**只重排 mesh、不刷新缓存** ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**、断言照样绿
    /// （实现与检测器共用一个口）。新口读的是 TMP 自己的 `textBounds`，**不在实现那条链上**。</para>
    /// <para>⛔ **只换「从哪个口读那个数」** —— 中心仍旧取**节点位置**（`AlignLeft/Right` 把节点挪走那条口径
    /// 一字未动）。⛔ 别改成直接用那块矩形的两条边：`TmpRenderedRect` 走 `localToWorldMatrix`，会把
    /// **父链缩放**与 TMP 子节点上那两项偏移一起算进去 ⇒ 期望值整体漂（那就不是「换口」了）。
    /// ⛔ **也不许换成「TMP 网格顶点」那条口**（`TmpVertPx`）—— 实测墨迹起点还多一个首字左边距
    /// （`Current Streak` 43.00 vs 45.22 = 2.22px）⇒ 一批期望值会**集体偏 1–2px**。
    /// 落地口径 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3；同族先例 = `Editor/ShopScene.cs`
    /// 的 `RectOf` label 支（2026-10-15 · A796′ 那一批，只换「取宽高的那一句」）。</para></summary>
    static bool LabelRenderedWH(Label lb, out float w, out float h)
    {
        w = h = 0f;
        if (lb == null) return false;
        float rx1, ry1, rx2, ry2;
        if (TmpRenderedRect(lb.transform, out rx1, out ry1, out rx2, out ry2))
        { w = rx2 - rx1; h = ry2 - ry1; return true; }
        w = lb.WorldW * 108f; h = lb.WorldH * 108f;      // 退回旧口（点阵后端 / 底下没有 TMP 网格）
        return false;                                     // 返回 false = **这一趟走的是退路**（调用方按需出声）
    }

    /// <summary>一段文字**渲染出来的**左/右边缘（画布像素）。判据 = **TMP 自己渲出来那块**（`textBounds`，真测量）。
    /// 🔴 用来抓「字还在、但飘到框外/压在别的字上」这类**量矩形量不到**的错 ——
    /// 2026-09-23 那两处 `timer` / `Refill Counter` 就是 61 条断言全绿、字却一个都看不见。
    /// 🔴 **2026-10-16（A796′）换口**：宽从 `Label.WorldW`（字段缓存）改成 `LabelRenderedWH`（TMP 活值，
    /// 量不到时它自己退回旧口）；**中心仍是节点位置** ⇒ 换口当天逐位同值（判据见那个助手的 doc）。</summary>
    static float TextLeftPx(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) return float.NaN;
        float w, h;
        LabelRenderedWH(lb, out w, out h);
        return PxOf(lb.transform.position.x) - w * 0.5f;
    }

    static float TextRightPx(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) return float.NaN;
        float w, h;
        LabelRenderedWH(lb, out w, out h);
        return PxOf(lb.transform.position.x) + w * 0.5f;
    }

    /// <summary>节点上那段字**现在写的是什么**（`Label.Text`；`Label` 走点阵兜底时也有值）。</summary>
    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    /// <summary>🆕 **2026-10-05（B4）**：一个件**画的是哪张图**（取子树里第一个 `ImageQuad` 的
    /// `Texture.name`）；没有 quad / 没有图 ⇒ `null`。
    /// 🔴 **为什么要单独一个**：B4 那几条要**按画出来的图记账**（`Wallet.Of(图名)`）——
    /// 从被测实现里读图名就变成自证了，所以量的是**渲染真值**（`ImageQuad.Texture`）。</summary>
    static string ArtOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        return q != null && q.Texture != null ? q.Texture.name : null;
    }

    /// <summary>🆕 **2026-10-05（B4）**：把一段计数文字读成整数。**读不出来回 −1**（⛔ 别回 0 ——
    /// 0 会和「合法数量 0」撞上，于是「解析失败」被当成「一致」⇒ 又一条**分不出两种状态**的弱断言）。</summary>
    static int IntOf(string s)
    {
        int v;
        return !string.IsNullOrEmpty(s) && int.TryParse(s.Trim(), out v) ? v : -1;
    }

    /// <summary>🆕 **2026-10-05（A75）**：把一颗按钮**按它渲出来那一块的中心**点一下（走 `PointerLayer` 真路径）。
    /// 返回 `true` = 指针层吃到了这一下。
    /// 🔴 **两条前提在函数里先断**（按钮在 + 那一点上命中的就是它）：少了它们，「点了没反应」会被误读成
    /// 「这个功能没做」，而不是「你点错地方了」。中心取自 `ImageQuad` 的**渲染真值**（`QuadRectOf`），
    /// 与 `PointerLayer.HitBoxPx` 用的是同一个框（`WorldW × 108` = `DesignPxH/DesignHeight`）。
    /// <para>🔴 **2026-10-11（批次1 · F8）**：那条前提原来只打 `[False]`，**看不出赢家是谁**
    /// （`RewardsWindow` 的 `Collect` 那两条红就是这么绕了一圈）⇒ 现在把**中心点**与**实得赢家 / 期望那一颗
    /// 的完整身份**（`BtnId`：名字 + 短父链 + 队列 + z + 同节点几颗）都写进文案里。</para></summary>
    static bool ClickButtonByQuad(PointerLayer pl, Transform btnNode, WindowButton wb, string what)
    {
        var q = btnNode != null ? btnNode.GetComponentInChildren<ImageQuad>() : null;
        float x1, y1, x2, y2;
        if (!QuadRectOf(q, out x1, out y1, out x2, out y2))
        {
            CheckTrue(false, what + "：量不到它渲出来的矩形（没有 `ImageQuad` ⇒ 鼠标点不到）");
            return false;
        }
        float cx = (x1 + x2) * 0.5f, cy = (y1 + y2) * 0.5f;
        var got = pl != null ? pl.ButtonAt(cx, cy) : null;
        CheckTrue(pl != null && wb != null && got == wb,
                  what + "：（前提）它**渲出来那一块的中心**命中的就是这一颗（引用相等 ⇒ 真鼠标点得到）"
                  + (pl == null ? " —— ⚠️ 这一刻 `PointerLayer` 根本取不到（命中这条路径没得走）"
                                : $" —— 中心 ({cx:F1}, {cy:F1})；**实得赢家** {BtnId(got)}；期望 {BtnId(wb)}"));
        return pl != null && pl.ClickAt(cx, cy);
    }

    /// <summary>🆕 **2026-10-11（批次1 · F8）**：一颗 `WindowButton` 的**可核对身份**（失败文案用）。
    /// 四样：**名字 + 短父链**（哪一页的哪个件）· 它**命中 quad 的队列 / z**（= `PointerLayer.HitButton`
    /// 挑赢家用那两个键）· **同一个 `GameObject` 上有几颗 `WindowButton`**。
    /// 🔴 最后那一格专治本仓那一族地雷：**同节点多颗** ⇒ 共用同一颗命中 quad ⇒ 队列与 z 逐位相同 ⇒
    /// 赢家退化成 `FindObjectsByType` 的**枚举顺序**，而 `GetComponentInChildren` 取的是**组件表第一颗**
    /// ⇒ 两把尺子各挑一颗（`Collect` 那两条红就是这么来的）。</summary>
    static string BtnId(WindowButton b)
    {
        if (b == null) return "<null>（那一点上**没有任何** `WindowButton`）";
        var q = b.GetComponentInChildren<ImageQuad>();
        // ⚠️ 与 `PointerLayer.HitQuad` **同一个门槛**（那颗 quad 在激活链上 + 按钮自己 `isActiveAndEnabled`）——
        //    不过这道门槛的件**压根不进命中表**，报出来的队列会把人带偏。
        bool inTable = q != null && q.gameObject.activeInHierarchy && b.isActiveAndEnabled;
        var same = b.gameObject.GetComponents<WindowButton>();
        int idx = 0;
        for (int i = 0; i < same.Length; i++) if (same[i] == b) { idx = i + 1; break; }
        var path = "";
        var cur = b.transform;
        for (int i = 0; i < 4 && cur != null; i++) { path = "/" + cur.name + path; cur = cur.parent; }
        return $"`{b.name}`{path} · 命中 quad 队列 "
             + (!inTable ? "（**不进命中表**：没有 quad / quad 不在激活链上 / 按钮自己 `isActiveAndEnabled` 为假）"
                         : q.RenderQueue.ToString())
             + $" · z {(inTable ? q.transform.position.z.ToString("F3") : "-")}"
             + $" · 同节点 `WindowButton` {same.Length} 颗" + (same.Length > 1 ? $"（这一颗是第 {idx} 颗）" : "");
    }

    /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
    /// 图走 `ImageQuad.WorldW/H`（**材质的真测量**）、字走 **TMP 自己渲出来那块 `textBounds`**
    /// （🆕 **2026-10-16（A796′）换口** —— 见 <see cref="LabelRenderedWH"/>；口径与同族先例 →
    /// `Editor/ShopScene.cs` 的 `RectOf`）；取**组件自己的 transform**
    /// （`AlignLeft/Right` 会把 `Label` 的节点挪走 —— 换口**没动**这一条）。见 `ShopScene.RectOf` 的同名注释。</summary>
    static bool RectOf(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        Transform node = t; float w, h;
        var lb = t.GetComponentInChildren<Label>();
        var q = t.GetComponentInChildren<ImageQuad>();
        if (lb != null) { node = lb.transform; LabelRenderedWH(lb, out w, out h); }
        else if (q != null) { node = q.transform; w = q.WorldW * 108f; h = q.WorldH * 108f; }
        else return false;
        float cx = PxOf(node.position.x), cy = PxYOf(node.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
    }

    static bool Overlaps(float ax1, float ay1, float ax2, float ay2,
                         float bx1, float by1, float bx2, float by2)
    {
        return ax1 < bx2 - 0.5f && bx1 < ax2 - 0.5f && ay1 < by2 - 0.5f && by1 < ay2 - 0.5f;
    }

    /// <summary>🆕 **2026-10-04（软边接线探针）**：一个 `ImageQuad` **渲出来**的像素矩形。
    /// ⚠️ 只用**这个组件自己**（不比 `RectOf`：那个会先找 `Label`、也会往子树里钻）—— 量软边
    /// **切出来的每一块**必须逐块量，钻进子树就只量到其中一块了。</summary>
    static bool QuadRectOf(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
    {
        // 🆕 2026-10-18（A1003）：算法收口到 `MenuDraw.QuadRectPx`（**保留本名与本形参 ⇒ 调用点 0 改动**）。
        // 🔴 改前这一份是**甲式**（裸 `q.transform.position × 108 ± 原点`，少除一次父级 `lossyScale`）；
        //    `MenuDraw` 那一份是**乙式**（先 `PosInDesignSpace` 除回设计缩放，再走设计帧读口）。
        //    两者只在「父链 `lossyScale == 1`」时逐位相同（本窗恒满足）⇒ 今天读数零变化，收的是**口径**。
        return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
    }

    /// <summary>🆕 **2026-10-04（首跑红了，就地补的）**：一个节点**整棵子树**里所有**激活**的 `ImageQuad` 的**并集**矩形；
    /// 一个 quad 都没有时退回 <see cref="RectOf"/>（`Label` 那条路）。
    /// 🔴 **为什么这条判据不能用 `RectOf`**：软边接线之后，压在阵营条渐隐带里的那一格会被**切开**
    /// （`MenuDraw.ApplySoftEdges`）⇒ `RectOf` 取「第一个 quad」只量到**其中一块**，
    /// 于是「整块落在装饰柱矩形里」这条判据会把**被切开的那一格误判成「完全在柱子里」**
    /// （首跑实测 `covered = 1`，而它是**两半里靠左的那半**）。**并集才是它真正占的那一块**。
    /// ⚠️ 只算 `activeSelf` 的块（整块出框的会被 `SetActive(false)`，算进去会把并集撑回未裁切大小）。</summary>
    static bool RectOfUnion(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        // 🆕 2026-10-18（A1003）：并集算法收口到 `MenuDraw.UnionQuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
        // 🔴 **本窗那一项【有意保留】的差异**：`QuadGate.Self`（只看块自己 `activeSelf`、父链关着也算）
        //    —— 别的宿主走 `InHierarchy` / `None`，⛔ 别统一（判据见 `MenuDraw.QuadGate` 的注释）。
        if (MenuDraw.UnionQuadRectPx(t, MenuDraw.QuadGate.Self, true, out x1, out y1, out x2, out y2))
            return true;
        // 🔴 另一项：【本窗独有】一个 quad 都没有时退回 `Label` 那条路（`RectOf`）——
        //    其余宿主回 false。⛔ 别给别人加、也别删它（见上面那条 doc 的「一个 quad 都没有时退回」）。
        return RectOf(t, out x1, out y1, out x2, out y2);
    }

    /// <summary>🆕 **2026-10-04：软边接线探针** —— 扫 `root` 子树，回传里面**所有软边切线**的位置。
    /// <para>判据 = `MenuDraw.ApplySoftEdges`（原版 `RectMask2D.m_Softness`）：非 0 时会把一块沿
    /// **渐隐带的内沿**切开 —— 原节点留含矩形中心的那一格，其余格建**子 quad**（命名 `…_soft&lt;i>&lt;j>`）。
    /// 父块与子块那条**共享边**就是带的内沿（左 `clip.x1 + soft.x` / 右 `clip.x2 − soft.x` / 上 / 下同）。</para>
    /// <para>🔴 **为什么拿它当「接没接」的判据**：软边 = 0（硬边）时**一个子块都不会有** ⇒ 空表。
    /// 于是「表空 = 没接」；「切线位置 = 自己算一遍原版值 + 视口矩形」⇒ **改坏实现会真红**、
    /// 且**不是**从被测实现里读期望值。</para>
    /// <param name="vertical">true = 只看**竖切线**（渐隐的是左右，= `soft.x`）；false = 只看横切线。</param>
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
                //    `ImageQuad.CreateNineSlice` 的 9 块是**兄弟**不是父子，但留一道名字闸更保险。
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

    // ============================================================ 🆕 A327：两态夹具（一条共用 · 四份【函数体】逐字同源）
    //
    // 🔴 **为什么要它**：2026-10-11（W4）把「世界 → 设计」那一族（`MenuDraw.PosInDesignSpace` / 各窗的
    //   `Local`·`Local3` / `CampaignTab.BuildLine` / `ShopWindow.BuildTimeCounter` / `CampaignTab.BuildArmyItems`）
    //   修完之后发现：**`k == 1`（小屏缩放开关出厂关着）时新旧两式逐位相同** ⇒ 那些处是**潜伏缺陷**
    //   —— **今天一条现有断言都不会红**（不是「有断言挡着」，是**还没有断言**）。
    //   ⚠️ **2026-10-11（FX3）收窄一处口径**：其中**基准恰好就是窗根**的那几处，新旧两式在生产里
    //   **永远**逐位相同（`basis == 窗根` ⇒ 除的是 Holder，恒单位缩放）⇒ 是 **no-op**，不是「潜伏」。
    //   判据 / 逐处清单 / 「该断言什么」→ `资料/普查产出_1011/W4_子3.md` §四·b。
    //
    // 🔴 **四份副本的【函数体】逐字同源**（文件头各记本文件的调用形状）：`Editor/ShellScene.cs` · `Editor/CollectionScene.cs` · 本文件 · `Editor/ShopScene.cs`。
    //   ⛔ **改一份就得改四份**（铁律 6：同一件事两套口径 = 迟早不一致）—— **2026-10-11（A350）**
    //   就是把本处与 `ShopScene` 那两份**从旧口径同步过来的**（此前只有 Shell/Collection 两份是新口径）。
    //
    // 🔴 **夹具形状**（判据给的就是这一条，⛔ 别另设计一套）：
    //   ① 态一 = 开关**关**（出厂态）⇒ 量一次 → `p1`；② 态二 = 开关**开** + **被乘的那一级**乘 M（走**生产那条路**
    //   `TransformScalerBySmallScreenUI`：`SetScale(M)` + `Tick()`，批处理没有帧循环）⇒ 再量同一个对象 → `p2`；
    //   ③ 断 **`p2 == M × p1`**（⛔ **一个我们自己的常量都不读** —— 只读 M）。
    //
    // 🔴 **2026-10-11（FX3）三处订正 —— 上一版夹具【自己把这条恒等式砸了】**（Shell 4 + Collection 4 条红；
    //   判据全文 → `资料/普查产出_1011/DIAG-A_Shell与Collection八条红.md`）：
    //   ① **M 加在【基准的父级】那一级**，⛔ **不是基准自己** —— `PosInDesignSpace` 除的正是
    //      `t.parent.lossyScale`（`Shell/MenuDraw.cs:74-78`）；那一级是单位缩放时新旧两式**逐位相同**。
    //   ② **可观测余量** = 「**基准相对被乘那一级的位移** ≥1 设计单位」（⛔ 不是「离**世界原点**」——
    //      上一版量的就是后者，所以它逼着调用方去挪窗根、又把恒等式砸了）。坏式与好式相差
    //      `(1−M)×|基准在态二的世界位置|` ≈ `0.2×|基准|`，容差 **0.02 单位（2.2px）** ⇒ 位移 ≥ 1 时
    //      偏差 ≥ 0.24 单位 = **26px**，远远超出容差 ⇒ 真会红。
    //   ③ **态二的 `measure` 里必须【重建】**（`Open()`/`Setup()`/`RefreshNodes()` 首句都清子件）—— 不重建时
    //      被量的局部位置是 `k == 1` 那一趟**冻结**下来的值，新旧两式在那时**逐位相同** ⇒ 断言恒真（= 假绿）。
    //      带牙口的判据 → `资料/普查产出_1011/W4_子3.md:91-106`；同族先例 = `Editor/ShopScene.cs` 的 **A294** 那一段（同文件的两态探针）。
    // 🔴 **本文件（与 `ShopScene`）的调用点与 Shell/Collection 那四处的形状【不同】**：
    //   这里 `参数1` **就是窗根自己**（`win.gameObject` —— M 加在窗根那一级），而 `basis` 是**窗根的子件**
    //   （`Content`）⇒ `basis.parent` 就是窗根那一级 ⇒ 前提① 照样成立（逐处说明见 `Run()` 的调用点）。
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

    // ============================================================ 建

    static RewardsWindow Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        // 🔴 **2026-10-11（批次3 · FX4 / DIAG-B §二·#1）就地订正（铁律 5）**：这里原来**手抄了第三份**
        //    「三颗 Holder + `AddComponent<WindowsManager>()`」（原 `:704-710`）—— 那 8 条红（#1 及其级联）的**唯一源头**：
        //    · `WindowsManager` **没有 `[ExecuteAlways]`**（`Shell/WindowsManager.cs` 里**只有** `WindowHolder` 那颗**有**）⇒ 批处理（编辑模式）
        //      **`Awake` 不跑** ⇒ 手抄那一下 **`Instance` 恒 null**（`Instance` 只在 `Awake` 里赋，**批处理下那句从不执行**）。
        //      判据（**四条独立记录**，全是踩过的坑）：`Shell/PromptPopup.cs` 的 `WindowButton` 类注 · `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 ·
        //      `Shell/PointerLayer.cs:47-48` · `资料/已知的坑.md:704`（「编辑模式下 `Awake/OnEnable`/`Update`
        //      **只对带 `[ExecuteAlways]` 的脚本**才跑」）。
        //    · 而 A309 新接的开窗出口走 `Shell/RewardWindow.cs` 的 **`WindowsManager.EnsureHost()`**
        //      —— 它在 `Instance == null` 时**不查「场景里是不是已经有一台」**、直接**再建一台 + 第二套锚点**
        //      ⇒ 领奖窗落进**第二台**，而 `wm2`（= `win.Manager`，见 `Run()`）
        //      是第一台 ⇒ `wm2.openWindows` 里当然没有它（**`:5032` 红 = #1**）。
        //    · 🔴 光让 `wm2` 找得到窗户**不够**：`RefreshOpenDailyWindows` 读的是 **`WindowsManager.Instance`**
        //      （`Shell/DailyData.cs` 里读 `WindowsManager.Instance` 的那一处）⇒ **`Instance` 必须【就是】`wm2` 那一台**（#2 的前置）。
        //    **最小改法 = 走公共件**：`EnsureHost` 里那句 `Instance = wm` 就是为此加的；
        //    而 `EnsureHost()` 的文档注释里白纸黑字写着「壳与「单独打开某个界面场景」**都走它**，
        //    两处各建一次 = 迟早不一致」—— 本夹具是**第三份手抄**（同形还有 3 处，已另立 A351）。
        //    ⚠️ 与改前的场景层级**一致**：`EnsureHost()` 建的 Holder 名 / placement 与手抄那份**逐字相同**
        //       （同一个 `MakeHolder` 形状），且 `root == null` ⇒ 同样挂在场景根、同样没有父。
        //    ⚠️ 顺带把 `PointerLayer` 也一并保住了：`EnsureHost` 第一句就是 `PointerLayer.Ensure(root)`
        //       —— 改前那条只挂在 `Awake` 里（`Shell/WindowsManager.cs` 的 `Awake()` 尾句）
        //       ⇒ **编辑模式下从来没跑过**，`PointerLayer.Instance` 一直是「有人惰性取用时才现建」。
        //    **改坏法**（这条的判别力）：把这一句换回手抄那三句 `MakeHolder` + `AddComponent<WindowsManager>()`
        //    ⇒ `:5032`「点领奖之后弹出 `Reward Window`」**立刻红**，并级联 #2 / #4-#8 / #9-#10 共 **8** 条。
        var wm = WindowsManager.EnsureHost();      // 它自己建 "Window Anchors" + 三颗 Holder + 管理器，并**登记 `Instance`**

        var win = RewardsWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

    /// <summary>⚠️ **2026-10-11（FX4）起 `Build()` 不再调它** —— 那三颗 Holder 现在由
    /// `WindowsManager.EnsureHost()` 建（同一个形状、名字与 placement 逐字相同，见 `Build()` 里那段订正）。
    /// **它留着不删**：这是「单独打开某个界面场景」那条路的**形状存档**（同形手抄全仓还有 3 处，已另立 A351）
    /// —— 留着比删掉更能让下一个会话看出「原来长什么样」。⛔ 新代码别调它。</summary>
    static void MakeHolder(Transform parent, string name, WindowsPlacement p)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        var h = t.gameObject.AddComponent<WindowHolder>();
        h.placement = p;
        h.RegisterNow();
    }

    /// <summary>一张图的平均亮度（0–255）。**空图护栏**用 —— 见 `Shoot` 里的说明。</summary>
    static float MeanBrightness(Texture2D t)
    {
        if (t == null) return 0f;
        var px = t.GetPixels32();
        if (px.Length == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b++;   // 抽样（每 7 个取 1）
        return sum / 3f / ((px.Length + 6) / 7);
    }
    /// <param name="allowBlank">**已知会是全黑的**那几个状态显式放行（不是静音 —— 每一处都在调用点上写了原因）。
    /// 判据仍是「平均亮度 &gt; 3」，只是这几张本来就拍的是「屏幕上什么都没有」。</param>
    /// <summary>截图 —— 本文件那一份的空图护栏走共用件（「已放行」那句文案 = 本文件原来那一句，
    /// 由 `_sink.BlankNote` 保住）。⚠️ `MeanBrightness` **仍留在本文件**：它与另外四份**浮点值不等价**
    /// （分母相除方式不同），合并会动 `✓` 行文本 —— 判据见 `Editor/MenuCheck.cs` 的 `Shoot` 文件头。</summary>
    static void Shoot(string file, bool allowBlank = false)
        => MenuCheck.Shoot(_sink, ShotDir, file, allowBlank, guardBlank: true, meanBrightness: MeanBrightness);

    /// <summary>拍**主壳窗**（`win`）的截图 —— **先断壳还开着**，再交给 `Shoot`（它那条「平均亮度 > 3」的护栏照旧）。
    /// <para>🔴 **2026-10-11（批次1 · F7）加**：A 组那 4 张**全黑**（平均亮度 0.0、4 张 md5 完全相同）的根因就是
    /// 「拍摄对象被关掉了」—— 新增的一句 `wm2.CloseAllWindows()` 把壳窗（它也在 `openWindows` 里）一起关了，
    /// 而相机是 `clearFlags = SolidColor` + `Color.black` ⇒ 拍出来是**空帧**。
    /// 旧护栏只报「黑了」、**报不出「为什么黑」**（得靠人去看 md5/体积）。</para>
    /// <para>**改坏法**（这条断言的判别力）：把任何一句 `wm2.CloseAllWindows()` 挪到这 4 张**之前**
    /// ⇒ 这一条**立刻红**（不用等出黑图）；把 4 张改回裸 `Shoot` ⇒ 这类改动又变成静默的。</para></summary>
    static void ShootShell(GameObject shell, string file)
    {
        CheckTrue(shell != null && shell.activeInHierarchy,
                  $"（前置）拍 `{file}` 时**壳窗还开着**（`activeInHierarchy`）—— 「只有壳」的那几张全黑就是这个原因"
                  + "（`CloseAllWindows()` 把壳也 `SetActive(false)` 了 ⇒ 空帧 + 各张 md5 相同）");
        Shoot(file);
    }

    /// <summary>拍**指定页**的截图：**先断「当前页签就是它」**，再交给 `ShootShell`（那条「壳还开着」+「不是空图」照旧）。
    /// <para>🔴 **2026-10-11（A300）加**：`01_日常_Missions.png` 拍的其实是【**锻造厂页**】——
    /// **硬证据 = 它与 `03_锻造厂.png` 字节完全相同**（2026-10-11 复核：`md5 = 5fc843461f439aef77e513f00aec4d3f`、
    /// 都是 **1505600 B**；上一版那两个文件的 md5 是 `34630922ab930d4a3ded67326e74241c`，**同样全等**）。
    /// **成因**：那句拍摄之前**最后一次会切页签的调用**是 `Click(2)`（锻造），而把页切回日常的 `Click(0)`
    /// 写在**下一行**；`Shoot` 只查「平均亮度 &gt; 3」⇒ 两个页面都是亮的 ⇒ **一条断言都不报**（静默）。
    /// 代价：那张图与 `03_锻造厂.png` 一起挂着「日常页」和「锻造页」**两个名字**，等于日常页**从来没有被拍过**。</para>
    /// <para>**改坏法**（这条的判别力）：把调用点前那句 `Click(n)` 删掉或改成别的号、或在本函数与拍摄之间
    /// 再插一次 `Click` ⇒ **立刻红**（不用出图、不用人去看 md5）。⛔ 别退回裸 `ShootShell` ——
    /// 「亮度 &gt; 3」分不出「拍的是哪一页」（两块页面都是亮的）。</para></summary>
    static void ShootShellPage(GameWindowWithTabs tabs, WindowTabType wanted, string file)
    {
        Check(tabs != null ? tabs.CurrentTab : WindowTabType.None, wanted,
              $"（A300 前置）拍 `{file}` 时当前页签 = **{wanted}**（拍错页曾经一条断言都不报）");
        ShootShell(tabs != null ? tabs.gameObject : null, file);
    }

    /// <summary>两个文件**字节完全相同**吗（A300 的判据 —— 那一件就是靠它发现的）。
    /// ⚠️ 任一个读不到 ⇒ 返回 `false`（「不相同」）—— **不是**放行：文件在不在由 `Shoot` 那两条护栏管
    /// （拍完就写盘 + 平均亮度 &gt; 3），这里只管「两张图是不是同一张」。</summary>
    static bool SameBytes(string a, string b)
    {
        try
        {
            if (!File.Exists(a) || !File.Exists(b)) return false;
            var x = File.ReadAllBytes(a); var y = File.ReadAllBytes(b);
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }
        catch { return false; }
    }

    /// <summary>🆕 **2026-10-12（A352）**：`file` 与本轮**任何一张** `others`（文件名，都在 <see cref="ShotDir"/> 下）
    /// **字节相同**吗 —— 撞了就返回**那一张的名字**，都没撞 ⇒ `null`。
    /// <para>报**名字**而不是 `bool`：红了要一眼看出**与谁撞了**（A300 那次的代价就是得靠人去看 md5 / 体积）。</para>
    /// <para>⛔ 别把这一条当成「这张图拍对了」的证明 —— 它只证「它**不是**另一张」；「拍的是谁」由调用点
    /// 那些**现场**断言（顶窗 / 有没有遗留窗）管。两者是两道不同的闸（见调用点那段注释）。</para></summary>
    static string SameBytesAsAny(string file, params string[] others)
    {
        foreach (var o in others)
            if (SameBytes(Path.Combine(ShotDir, file), Path.Combine(ShotDir, o))) return o;
        return null;
    }

    // ============================================================ 🆕 A313 / A352（2026-10-12）· 🆕 A450
    // 四条领奖出口（`CollectDaily` / `CollectWeekly` / `CollectSkulls` / `CollectLogin`）接上那扇
    // `Reward Window` 之后，夹具里**每一次「真领到」的点击都会弹一扇窗**。那扇窗是**弹窗**：
    //   · `WindowsManager.OpenWindow` 的弹窗支会对底窗调 `ToBackground()`（`Shell/WindowsManager.cs:716`）；
    //   · `PointerLayer.PointerReachable` 的判据 = 「`CurrentState != Background`」
    //     （`Shell/PointerLayer.cs:1093-1097`）⇒ **壳上每一颗钮都点不到**；
    //   · 相机只有一台、它又盖在整屏上 ⇒ **截图拍到的会是它**（A352 那张 `05_收件箱_空态.png` 就是这个形状）。
    // ⇒ 两个把手：`RewardWindowFixture.DismissRewardWindows()`（关掉，关窗会把被压到背景的壳**带回 `Open`**）与
    //   `ClickCollectAndDismiss()`（点 + 断「真弹了」+ 立刻关）。
    //
    // 🔴 **A450（2026-10-12）**：三条**纯判据**（`FindOpenRewardWindow` / `OpenRewardWindowCount` /
    //   `DismissRewardWindows`）已搬进**共用件** `Editor/RewardWindowFixture.cs` —— 商店线那 5 条 A439
    //   断言（`Editor/ShopScene.cs`，买「商品档」⇒ 弹领奖窗）要的正是它们，原来 `static` 私有 ⇒
    //   别的宿主用不了（要接就得**照抄一份**，与本仓「判据要共用一份」相抵）。
    //   ⛔ **别在本文件里再建一份**（调用点一律写 `RewardWindowFixture.xxx(...)`）。
    //   `ClickCollectAndDismiss` **留在这里**：它要靠本宿主的 `ClickButtonByQuad`，以及 `Check` / `CheckTrue`
    //   —— 那两个是**逐宿主**的计数器（拿别处的 `Check` 去断 ⇒ 失败会记进别人的合计里，**静默**）。

    /// <summary>点一颗「领到奖」的钮 ⇒ **断言真弹了那扇 `Reward Window`** ⇒ 立刻关掉它。
    /// 返回那扇窗（关掉之后**实例仍在** ⇒ 还能读 `Context` 断内容），没弹 ⇒ `null`。</summary>
    static RewardWindow ClickCollectAndDismiss(Transform hit, WindowButton wb, string what)
    {
        Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
              what + "：（前提）点下去之前**场上没有遗留的领奖窗**（有 ⇒ 这一下会落到它身上，"
              + "`ClickButtonByQuad` 那条「命中就是这一颗」会先红）");
        CheckTrue(ClickButtonByQuad(PointerLayer.Instance, hit, wb, what), what + "：点它（真路径）");
        var rw = RewardWindowFixture.FindOpenRewardWindow();
        int closed = RewardWindowFixture.DismissRewardWindows();
        Check(closed, rw == null ? 0 : 1,
              what + "：…弹出的那扇**关得掉**（关窗 ⇒ `NotifyClosed` → `ShowPreviousWindow` 把壳带回 `Open`）");
        return rw;
    }

    // ============================================================ 自检

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        Directory.CreateDirectory(ShotDir);
        ForgeData.ResetForTest();     // 锻造页的数据是静态的 ⇒ 每次自检从初值起（自检之间互不影响）
        Debug.Log(P + "=== 「日常」奖励窗口自检 开始 ===");

        var win = Build(out var root);
        var wm2 = win.Manager;   // 下面几个窗（每日奖励/连登/收件箱）都挂在同一台 `WindowsManager` 上

        // ---------------- §一 窗口根 ----------------
        Section("窗口根（§一：`MainMenuRewardsWindow` 的 GameWindow 参数）");
        Check(win.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（MB 1349677669050291967 实证）");
        Check(win.placement, WindowsPlacement.Canvas, "`windowsPlacement` = 5 Canvas（**不是 15**）");
        Check(win.closeOnEsc, false, "`closeOnESC` = 0（ESC 不关窗）");

        Section("Content Area（§一：x 167.17..1920.01 · y 70.94..1080.00）");
        var area = FindChild(root, "Content Area");
        CheckTrue(area != null, "`Content Area` 建了");
        CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
        CheckRectPx(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
        var bg = FindChild(area, "Background");
        CheckTrue(bg != null && bg.GetComponentInChildren<ImageQuad>() != null
                  && bg.GetComponentInChildren<ImageQuad>().Texture != null
                  && bg.GetComponentInChildren<ImageQuad>().Texture.name.StartsWith("grad_"),
                  "`Background` 用的是**运行时双色渐变**（原版 `Image(sprite=null)` + `UIGradient` c1 #390503 / c2 #0C0004 / angle 82）");

        // ---------------- §二 左栏 ----------------
        Section("左栏 `Tab Buttons`（§二·1：x 167.17..332.17 · y 70.94..1080 · 165×1009.06）");
        var bar = FindChild(area, "Tab Buttons");
        CheckTrue(bar != null, "`Tab Buttons` 建了");
        CheckAt(bar, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`");
        CheckRectPx(bar, 167.17f, 332.17f, 70.94f, 1080f, "左栏底图 `40k_main_tab_background`");
        CheckAt(FindChild(bar, "Shadow"), 167.18f, 214.81f, 70.94f, 1080f, "`Tab Buttons/Shadow`（窄 47.64）");

        Section("五个键（§二·2：VLG padTop 120 ⇒ 绝对 y 190.94 / 370.94 / 550.94 / 730.94 / 910.94）");
        // 🔴 **2026-10-16（A815）就地改口径（铁律 5）**：本节原来叫「四个键」、`tops` 四格 —— 当时键表
        //    （`RewardsWindow.Buttons`）**4 项**、其中第 4 项是**原版母版按钮**（`Booster Packs`，运行期关着）。
        //    A815（**用户 2026-10-15 拍板**）把第 4 格让给**我们自建的「每日连胜」页**、母版顺位到第 5 项
        //    ⇒ 键表 5 项、左栏 5 个节点（**可见 4 个**，母版那格照旧关着）。
        //    ⚠️ **原版只有 4 个节点**（`Tab Buttons` 下 Missions/Campaign/Forge/母版）——本节多出来的第 5 格
        //      是我们自建那一件的产物，**不是原版的东西**。
        float[] tops = { 190.94f, 370.94f, 550.94f, 730.94f, 910.94f };
        for (int i = 0; i < 5; i++)
        {
            var b = FindChild(bar, "RewardsTabButton_" + i);
            CheckTrue(b != null, $"第 {i + 1} 个键建了");
            CheckAt(b, 167.17f, 332.17f, tops[i], tops[i] + 180f, $"第 {i + 1} 个键");
            // `Label` 底：`pos=(0,-72.16) p=(.5,0) sz=(155,37.86)` ⇒ 底边 = 键中心 - 72.16
            float cy = tops[i] + 90f, lb = cy + 72.16f;
            CheckAt(FindChild(b, "Text Background"), 172.17f, 327.17f, lb - 37.86f, lb,
                    $"第 {i + 1} 个键的文案条");
            // `Badge Highlight`：35²，中心 (栏中心+51.7, 键中心∓27.2)。**母版那一格是 +47.9**（§二·2）
            // 🔴 A815 改口径：原来写 `i == 3`（当时第 4 项就是母版）；母版顺位到第 5 项 ⇒ **`i == 4`**。
            //    （自建的第 4 键**照母版那一套取** +47.9 —— 判据与推断见 `RewardsWindow.Buttons` 第 4 项那段注释。）
            // 🔴 **2026-10-17 订正**：上面两行注释与**实现**都写明「**第 4 键也取母版那一套（+47.9）**」
            //    （`Shell/RewardsWindow.cs` 的 `TabBtnSpec(..., 47.9f)`），只有下面这个表达式还是 `i == 4` ⇒
            //    第 4 格按 −27.2 期望、差 **75.1px**（= 47.9+27.2 = 实测 0.6954×108）⇒ 收口跑 1 条红。
            //    判据 → `资料/普查产出_1016/诊断_收口7条红.md` §6。
            float bdy = i >= 3 ? 47.9f : -27.2f;
            CheckAt(FindChild(b, "Badge Highlight"), 249.67f + 51.7f - 17.5f, 249.67f + 51.7f + 17.5f,
                    cy - bdy - 17.5f, cy - bdy + 17.5f, $"第 {i + 1} 个键的红点");
        }
        // 🆕 **2026-10-08（波 C3 · A212 主表 #31 验收）：左栏键文案的【渲染】断言**（四窗这一族原来一条都没有）。
        //   判据 = 原版四窗左栏键文案的 `m_TextWrappingMode` **一律 `0`（`NoWrap`）**—— 逐窗现读的四窗表
        //   只写一处：`Shell/MenuWindowBase.cs` 的 `BuildTabButton`（此处不抄第二份，铁律 6）。
        //   ⛔ **为什么必须量渲染、不能只比字号**：`SetAutoFitBox` 只把 `fontSizeMin/Max` 交出去，
        //   **装不装得下由 TMP 算** ⇒ 字号对而字冲出去，自检照样全绿（`AutoFitBox` 那条教训，2026-09-22 踩过）。
        //   框宽 **155** = 原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`
        //   （⛔ 不读 `BuildTabButton` 的 `labW` —— 那也是被测实现里的数，读了就是自证）。
        //   **改坏法**：删掉 `MenuWindowBase.BuildTabButton` 末句 `txt.SetWrapping(false)` ⇒
        //   ① `折行=` 那条立刻红（0 → 1）；② 带空格的 `BOOSTER PACKS`（第 4 键）会折成两行 ⇒ 「就一行」那条也红。
        {
            int tabN = 0;
            // 🔴 **2026-10-16（A815）**：`i < 4` → **`i < 5`**（键表 5 项了，见上面「五个键」那一节）。
            //    ⚠️ 第 5 格是**关着的母版**（`TabButtons.Initialize`）—— 与 A815 之前那 4 格里的第 4 格**同一种状态**
            //    （当年那一格就是关着的母版，而这几条渲染断言一直在它身上跑得通）⇒ 扩到 5 不会引入新形态。
            //    🆕 **我们自建的那一格（第 4 格）是活的** ⇒ 它的文案 `DAILY STREAK` 也由这几条守着
            //      （「折行=0 / 就一行 / 渲出来的宽 ≤ 框宽 155」；文案是我们挑的、⛔ 别当原版文案）。
            for (int i = 0; i < 5; i++)
            {
                var k = FindChild(bar, "RewardsTabButton_" + i);
                var klb = k != null ? k.GetComponentInChildren<Label>() : null;
                CheckTrue(klb != null, $"（左栏渲染断言 · 前提）第 {i + 1} 键的文案 `Label` 取得到");
                if (klb == null) continue;
                tabN++;
                CheckTrue(klb.WrappingMode >= 0,
                          $"（左栏渲染断言 · 前提）第 {i + 1} 键 `{klb.Text}` 走的是 **TMP 后端**"
                        + "（`-1` = 点阵后端 ⇒ 下面两条渲染断言不成立，如实红、不假装）");
                Check(klb.WrappingMode, 0, $"★ 第 {i + 1} 键 `{klb.Text}`：**`折行=0`**（原版四窗左栏键一律 0）");
                Check(klb.LineCount, 1, $"★ …而且渲出来**就一行**（`Normal` 会把带空格的 `BOOSTER PACKS` 折成两行）");
                // 🔴 **2026-10-13（A750）换口**：量法 `Label.WorldW`（= 缓存 `_tmpW`）→ **TMP 自己渲出来那块网格**
                //    —— 与 `Editor/MainMenuScene.cs` 的 `CheckFits`（A709）/ 档案窗两条（A714）/ 社交窗那条（A715）/
                //    高那一条（A718）**同一条口径**；本条 = 这一族的**第四处**（第五/六处 = `Editor/ShopScene.cs` ·
                //    `Editor/CollectionScene.cs`，同一批改完，三处逐字同形）。
                //    **为什么这是灭自证**：`Label.WorldW` 那份缓存**只有 `RefreshBounds()` 写**
                //    （`Battle/Label.cs`，全库唯一的写点）—— 也就是**被测实现自己**；而 `SetCharSpacing`
                //    （`:524-533`）· `SetFontSize`（`:503-514`）这一族**只重排 mesh、不刷新缓存**
                //    ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**、这条照样绿（实现与检测器**共用一个口**）。
                //    新口读的是 TMP 自己的 `textBounds`，**不在实现那条链上**。
                //    期望值 `155f` / 容差 `+ 0.5f` / 文案全文**一位未动** —— 变的只有「从哪个口读那个数」与缩进。
                // ⚠️ **量不到 ⇒ 必须显式红**：`TmpRenderedRect` 失败时四个 out **全 0** ⇒ 让下面那句拿到 0 的话，
                //    `0 ≤ 155.5` 会**假绿**。本条的判据句**没有** `> 0f` 那一半 ⇒ **不能**用 `-1f` 哨兵
                //    （`-1 ≤ 155.5` 也恒真 = 假绿的另一种写法）—— 只能像 A715/A718 那样把原句包进 `else {}`
                //    + 补一条把原因写在脸上的显式红。
                // ⚠️ 量的是 **`klb.transform`**（`Label` 自己那一颗；TMP 是它的子件 —— `Battle/Label.cs` 的 `Create` 里 `TmpFont.NewText` 那一句
                //    `TmpFont.NewText(transform, …)`）—— 取 `Label` 的写法（上面那句 `GetComponentInChildren<Label>()`）
                //    **一字未动**。⛔ 别改成 `TmpRenderedRect(k, …)`：那会捞 `k` 子树里**第一颗 TMP（含 inactive）**，
                //    与「只找激活」的 `Label` **未必是同一颗** ⇒ 会把「节点不在（红）」与「量到了别一颗（绿）」混成一档。
                // **改坏法（只咬旧口）**：在 `Shell/MenuWindowBase.cs` 的 `BuildTabButton` 里那句 `SetWrapping(false)` 那句
                //    `if (txt != null) txt.SetWrapping(false);` **之后**插一句 `if (txt != null) txt.SetCharSpacing(5f);`
                //    ——（那句话是本函数刷**最后一次**缓存的地方：`SetWrapping` → 模式真的变了 → `ForceRelayout`
                //    → `RefreshBounds()`，`Battle/Label.cs` 的 `SetWrapping` → `ForceRelayout` → `RefreshBounds` 那一路）⇒ 网格重排了、**缓存不动** ⇒ 两个口读到的数
                //    **必然不同**。⚠️ 三件套里的第三件（新口红 / 旧口绿）**本地证不出来**，见下面那条如实标。
                // ⚠️ **如实标**：这四颗键都开着 **autosize**（`BuildTabButton` 的 `SetAutoFitBox`，
                //    `enableAutoSizing = true`）⇒ TMP 重排时会把字号缩回去、渲出来的宽**仍 ≤ 框宽**
                //    ⇒ 上面那个改坏法**不一定**把绿翻红（「两个口读到的数不一样」才是换口的全部意义）。
                //    实测（`_tmp_view/rewards.log` 那一次；⚠️ 那是**换口之前**的跑，四颗读的都是**旧口**）：
                //    `MISSIONS` 128.5 · `CAMPAIGN` 144.6 · `FORGE` 89.2 · `BOOSTER PACKS` **151.6**
                //    ⇒ 最紧的第 4 键余量只有 **3.9px**，而它的 `fontSizeMin = 12` 离标称 25.65 还很远
                //    ⇒ 有充分的缩字空间。
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
            Check(tabN, 5, "五颗键的文案都量到了（少于 5 ⇒ 上面那几条等于没查）");
        }
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_0"), "Icon"), "40K_rewards_bt_missions", "第 1 键图标");
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_1"), "Icon"), "40k_main_bt_campaign",
                 "第 2 键图标（**原版就是主菜单那张导航图** —— `40K_rewards_bt_campaign` 不存在）");
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_2"), "Icon"), "40K_rewards_bt_forge", "第 3 键图标");
        // 🔴 **2026-10-16（A815）就地改口径（铁律 5）**：这一行原来断的是
        //    「`RewardsTabButton_3` 的图标 = `40K_shop_bt_boosters`」（= **原版母版那枚**，照原版写、当时是对的）。
        //    A815（**用户 2026-10-15 拍板**）把第 4 格换成**我们自建的「每日连胜」页** ⇒ 第 4 键的图标变成
        //    **我们挑的** `40k_main_bt_rewards`（126×126）；母版那枚 `40K_shop_bt_boosters` **顺位到第 5 键**
        //    （下一行）。⚠️ **原版第 4 格就是母版那枚** —— 这条改动是**改口径**，不是修 bug。
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_3"), "Icon"), "40k_main_bt_rewards",
                 "第 4 键图标（🆕 **自建页签 · 我们挑的** —— 原版这一格是母版按钮；见 `RewardsWindow.Buttons` 第 4 项）");
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_4"), "Icon"), "40K_shop_bt_boosters",
                 "第 5 键（= **原版母版** `Menu Navigation Panel Button`）图标照旧 `40K_shop_bt_boosters`"
                 + "（A815 只挪了它的位置，值逐值未动）");
        Check(CountByName(root, "Highlight"), 5, "五个键各有一层高亮（**只有选中的那个可见**）");
        Check(CountVisible(root, "Highlight"), 1, "**可见的高亮恰好 1 个**（选中态；原版出厂都亮，可见性由运行时驱动）");
        // ============================================================ 🆕 **2026-10-12（A361）**
        // 判据 = A 表那一条：本文件这份 `CountByName` **首句直接 `root.GetComponentsInChildren`** ⇒
        // 传 `null` **抛 NRE**；而同族的 `CountByPrefix` **两份都带挡**（本文件 · `Editor/ShopScene.cs` 的 `CountByPrefix`）
        // ⇒ 「同一条规则两种写法」。🔴 **NRE 在本文件不是「红一条」而是「把后面全吞掉」**：
        // `Check` 是**记账式**的（不抛、不早退），而整条自检是一条链跑到底 ⇒ 一条 NRE 会让**它后面每一条
        // 断言都不跑**（日志里只看得到一条异常，看不出还有 200 条没跑）。
        // ⚠️ 这里紧跟在那条 `CountByName(root,"Highlight") == 4` **之后**：用的是**同一个现场**，
        // 所以「非 null 时照常数得出来」那一条与前一条同源、不会因为页面后来被重建而假红。
        // **改坏法**：删掉 `CountByName` 里那句 `if (root == null) return 0;` ⇒ 下面第一条**抛** ⇒
        // 它自己红不了（异常），但**它后面到结尾的所有断言一起消失**（这正是本条要拦的东西）。
        Check(CountByName(null, "Highlight"), 0,
              "★ A361：`CountByName` 传 `null` 得 **0**（不是抛 NRE）—— 有挡才跑得到这一行");
        Check(CountByPrefix(null, "Item_"), 0,
              "★ …同族的 `CountByPrefix` 本来就有挡 ⇒ 两份**口径一致**（这条钉住「别只改一份」）");
        Check(CountByName(root, "Highlight"), 5,
              "★ …而 **非 null 时照常数得出来**（⛔ 别把挡写成「恒返回 0」—— 那是另一种静默；"
              + "与上面第一条合起来才分得出「挡 null」与「什么都不数」）");
        // 🔴 **2026-09-23 加（原版值，出处 `d:/2/tools/decomp_full/TabButtons__Initialize.c:35-44`）**：
        //    原版 `Initialize` 第一件事就是 `tabButtonPrefab.gameObject.SetActive(false)` ——
        //    母版（`Booster Packs`）只是**运行期新增页签的克隆母版**，⇒ **左栏运行期不显示它**。
        //    我们原来把它常显了（找茬点：多画了一层）。这条断言钉住「建了但关着」。
        // 🔴 **2026-10-16（A815）就地改口径（铁律 5）**：这一段原来断的是 `RewardsTabButton_3`
        //    「第 4 键（Booster Packs）运行期**隐藏**」+「左栏**可见的键恰好 3 个**」——
        //    **那两条都是照原版写的、当时是对的**（原版奖励窗 `tabs` 只有 Missions/Forge/Campaign 三条、
        //    第 4 格是母版按钮；判据 → `资料/普查产出_1015/R5_A815_A817_A819判据.md` §一）。
        //    2026-10-15 **用户拍板**：本仓**自建**第 4 个页签「每日连胜」⇒ 第 4 格变成**真页签**（可见、可点开），
        //    母版顺位到**第 5 项** ⇒ 下面三条的期望值**全部按新口径改**。⚠️ **原版行为一个字没改**
        //    （母版照旧建了即关），变的是**我们多做的这一件**。
        var b3 = FindChild(bar, "RewardsTabButton_4");
        CheckTrue(b3 != null, "第 5 项（`tabButtonPrefab` 母版 = `Booster Packs`）**照建**（以后加活动页签要克隆它）");
        CheckTrue(b3 != null && !b3.gameObject.activeSelf,
                  "第 5 项（母版）运行期**隐藏**（照原版 `TabButtons.Initialize`）");
        Check(CountVisibleChildren(bar, "RewardsTabButton_"), 4,
              "左栏**可见的键恰好 4 个**（**原版是 3** —— 第 4 个是**我们自建的「每日连胜」**、用户 2026-10-15 拍板；"
              + "母版那一格照旧关着 ⇒ 建了 5 个、看得见 4 个）");
        // ★ **灭自证**（本仓那条纪律：只断「新的对」不够 —— 要挡住「把两处一起改成另一种错、照样绿」）：
        //   上面那条只数个数 ⇒ 「把**母版**提前显出来、同时把第 4 格关掉」也照样得 4 ⇒ 必须**点名**多出来的是哪一格。
        CheckTrue(CountVisibleChildren(bar, "RewardsTabButton_") == 4
                  && FindChild(bar, "RewardsTabButton_3") != null
                  && FindChild(bar, "RewardsTabButton_3").gameObject.activeSelf
                  && b3 != null && !b3.gameObject.activeSelf,
                  "★ A815：**多出来的那一个可见键就是第 4 格**（`RewardsTabButton_3` 活着、母版那格关着）"
                  + " —— 挡「把母版提前显出来凑够 4 个」那种改法");

        // 🆕 2026-10-03（§三 第 29 条 **B2**）：左栏 `Highlight` 从「单块拉伸」改成**九宫格**
        //   —— 原版这一件是 `Image.Type = Sliced` + `m_PixelsPerUnitMultiplier = 0.92`
        //   （判据：`menu_dump.py … "MissionsRewardsButton"` ⇒ `40k_main_bt_selected BW 71×71 九宫 30,30,30,30 ppuMul=0.92`；
        //    再按图集采样 ⇒ 边是**软边晕**、不是纯色 ⇒ 「看不出来」那条旧记录**已被证伪**）。
        //   ⇒ 一棵树里 **9 个 quad**：切键时**必须整棵开关**，只切一个会静默留下 8 块。
        {
            var k0 = FindChild(bar, "RewardsTabButton_0");
            var hl0 = k0 != null ? FindChild(k0, "Highlight") : null;
            var qs = hl0 != null ? hl0.GetComponentsInChildren<ImageQuad>(true) : null;
            CheckTrue(qs != null && qs.Length == 9,
                      $"左栏 `Highlight` 是**九宫格** ⇒ 一棵树 **9 块**（实测 {(qs != null ? qs.Length : 0)}）");
            win.tabButtons.Click(1);
            int alive = 0;
            if (qs != null) foreach (var q in qs) if (q != null && q.gameObject.activeInHierarchy) alive++;
            Check(alive, 0, "切到别的键 ⇒ 第 1 键的高亮**整棵**关掉（不是只关一块）");
            win.tabButtons.Click(0);
            alive = 0;
            if (qs != null) foreach (var q in qs) if (q != null && q.gameObject.activeInHierarchy) alive++;
            Check(alive, 9, "切回来 ⇒ 9 块**全亮**");
        }

        // ---------------- §三 任务页 ----------------
        Section("`Missions Tab` 与三大块（§三·1，机械走链的数）");
        var tab = FindChild(FindChild(area, "Tabs"), "Missions Tab");
        CheckTrue(tab != null, "`Missions Tab` 建了");
        CheckAt(tab, 166.69f, 1920f, 69.20f, 1080f, "`Missions Tab`");
        var nm = FindChild(tab, "Normal Missions");
        CheckAt(nm, 372.37f, 1891.37f, 95.85f, 819.65f, "`Normal Missions`");
        // 🔴 **2026-10-06（A106）：下面两条改成【布局位】**（原来断的是 prefab 的**模板值** 1151.58 / 1271.31 / 1810.49）。
        //    `Normal Missions` 上真挂着 HLG、`Special Missions` 上真挂着 CSF（`m_HorizontalFit = 2 = PreferredSize`）
        //    ⇒ 原版运行期**确实会挪**这两件。断的是**【2 张卡】那一帧**（= 我们这一页实际画的那一帧：
        //    登录卡 + 骷髅卡都建）：`Special Missions` 宽 **706.29**（x2 = 372.37 + 706.29 = **1078.66**）·
        //    `Daily Missions` x **1190.44**..**1799.94**（宽 **609.50**）。
        //    ⚠️ **y 与高照旧**（`ctrlH = 0` ⇒ 高来自 `sizeDelta`；`align = 0` ⇒ 贴顶）—— 那两对数一个没动。
        //    手算过程 / 原版参数 / 出处 / **「卡片数 → 参数」对照表**（1 张 = 作者态 · 2 张 · 空着的 3~4 张）：
        //    **全在 `Shell/MissionsTab.cs` 的 A106 那一段**，⛔ 别在别处再抄一份数字（铁律 6：数字只留一处）。
        //    改坏法：把 `MissionsTab` 那两个常量退回模板值 ⇒ 两条红。
        //    🔴 **2026-10-06（A124）**：这两条量的是**节点 marker**，它留在**布局矩形**上（**不进 `localScale`**）
        //    —— 与 `MissionsTab` 里 `Special Missions` / `Daily Missions` 那两颗节点的建法一致
        //    （marker = 布局位；**它以下**建出来的每一件才过缩放）。⇒ 两个节点的期望值**照旧是布局值**；
        //    **子树**的视觉矩形（行 / 行内件）走 `DmView(...)`，见下面「每日任务三行」那一节。
        CheckAt(FindChild(nm, "Special Missions"), 372.37f, 1078.66f, 95.85f, 652.07f, "`Special Missions`（布局位·2 张卡）");
        CheckAt(FindChild(nm, "Daily Missions"), 1190.44f, 1799.94f, 95.85f, 651.72f,
                "`Daily Missions`（布局位·2 张卡；**子树**的视觉矩形见「每日任务三行」那节）");
        // ⚠️ `Weekly Mission Holder` 的父是 **`Missions Tab`**，与 `Normal Missions` **同级**
        //    （原版树 `菜单全树.md` + 机械走链两处一致；第一版挂在 `Normal Missions` 下，差 205.68/26.65px）
        CheckAt(FindChild(tab, "Weekly Mission"), 372.37f, 1891.36f, 759.79f, 987.30f, "`Weekly Mission`");
        // 🔴 **2026-10-05（块6）原版多这一层** —— 真包链 = `Missions Tab / Weekly Mission Holder / Weekly Mission`
        //    （`RT/RectTransform_341968686871156479.json` 的 `m_Father` = `Missions Tab` 的 RT `6703300887156823807`、
        //     `m_Children` = 唯一一项 `1711644035698464511`（= `Weekly Mission`）；`ContainerHolder.maxAmount = 1`）。
        //    ⛔ **上面那条 `CheckAt` 分不出这两种状态** —— 两层的矩形**完全重合**（Holder `sz=(1518.99,227.51)`、
        //    内层 `a=(0,0)-(1,1) sz=(0,-3.8e-06)`），量矩形两态都绿（这正是它此前一直绿的原因）⇒ 必须**量名字与父子关系**。
        //    这一段**每一条都能单独红**（怎么改坏见下面各条的文案）。
        {
            var wmh = FindChild(tab, "Weekly Mission Holder");
            CheckTrue(wmh != null, "★ `Weekly Mission Holder` **这一层建了**（原版比我们此前多一层；"
                                 + "改坏法：退回「`Weekly Mission` 直挂 `Missions Tab`」⇒ 这条红）");
            CheckTrue(wmh != null && wmh.parent == tab,
                      "…它的父 = **`Missions Tab`**（与 `Normal Missions` 同级；"
                      + "改坏法：挂到 `Normal Missions` 下 ⇒ 这条红）");
            // 🔴 用**与上面 CheckAt 同一个查法**（按名字全树找）取 `Weekly Mission` —— 这样「节点被改名」也会被抓到：
            //    若节点又叫回 `Weekly Mission Container`，这里找到的会是它里面那个**同名 ImageQuad**。
            var wmc = FindChild(tab, "Weekly Mission");
            CheckTrue(wmc != null && wmh != null && wmc.parent == wmh,
                      "★ `Weekly Mission` 的父**就是** Holder（不是与它同级挂在 `Missions Tab` 上）—— "
                      + "改坏法：少建一层、或把内层还叫 `Weekly Mission Container` ⇒ 这条红");
            CheckTrue(FindChild(tab, "Weekly Mission Container") == null,
                      "★ 我们**自造的那一层 `Weekly Mission Container` 已经不在了**（那个名字在原版里是"
                      + "**另一份独立 prefab 的根**：`GO/1384672845988647690`，其 RT `m_Father = {m_PathID: 0}`）—— "
                      + "改坏法：把它建回来 ⇒ 这条红");
            // 原版 `ContainerHolder` 的 `maxAmount = 1`（MB `2307977552517666559`）⇒ Holder 下**只挂一个** Container。
            Check(wmh != null ? wmh.childCount : -1, 1,
                  "…Holder 下**恰好一个**子节点（原版 `ContainerHolder.maxAmount = 1`）—— 改坏法：多挂一件 ⇒ 这条红");
        }

        Section("每日任务三行（§三·1：`Daily Missions Holder` VLG spacing 18.55 · 三行各 150 · align 7 LowerCenter）");
        // `RowRect` 的判据：三行靠**下**对齐 ⇒ 最后一行的底边 = 容器底边
        // 🔴 **2026-10-06（A124）：本节的矩形全部是【视觉矩形】** —— `holder` 那四个数是**设计空间**的
        //    （`RowRect` 的算式就是设计空间的：`RowH 150` / `spacing 18.55`），量节点之前统一过 `DmView`。
        //    漏了这一步 = 既有的期望值全部比屏幕上小 15%，**而它们照样能全绿**（因为量的是同一个坐标系）——
        //    所以下面还专门钉了**行宽 700.93 / 行高 172.5** 两条字面量。
        var rows = new List<Transform>();
        foreach (var t in tab.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("Daily Mission Container (")) rows.Add(t);
        Check(rows.Count, 3, "**恰好 3 行**（原版 `Daily Missions Holder` 下三份实例）");
        if (rows.Count == 3)
        {
            PxRect holder = new PxRect(RowRectHolderX1(), 150.28f, RowRectHolderX2(), 651.72f);
            for (int i = 0; i < 3; i++)
            {
                var want = DmView(MissionsTab.RowRect(holder, i));
                // 🔴 **两条承重真值**（原版字段字面量，⛔ 不是从实现读的常量）：
                //    行宽 = `609.50 × 1.15 = **700.93**` · 行高 = `150 × 1.15 = **172.5**`。
                //    ⚠️ 这两个数**不是** `965.186 / 1109.96`（那是【1 张卡·作者态】那一帧的）。
                CheckNear(want.W, 700.93f, 0.5f,
                          $"第 {i + 1} 行宽 = **609.50 × 1.15**（原版 `Daily Missions.m_LocalScale`；"
                          + "⛔ 不是作者态那一帧的 965.186）");
                CheckNear(want.H, 172.5f, 0.5f, $"第 {i + 1} 行高 = **150 × 1.15**");
                CheckAt(rows[i], want.x1, want.x2, want.y1, want.y2, $"第 {i + 1} 行（视觉矩形）");
                // **渲染真值**：行底图 `Background` 的 quad 世界尺寸（`CheckRectPx` 量的是 `WorldW/WorldH`，
                // 不是「我们传进去的矩形」）—— 上面那条 `CheckAt` 只量节点中心，量不到「画了多大」。
                CheckRectPx(FindChild(rows[i], "Background"), want.x1, want.x2, want.y1, want.y2,
                            $"第 {i + 1} 行底图的渲染矩形（{want.W:F2} × {want.H:F2}）");
            }
            // 行内两件：`description` 与 `Collect`（原版锚点依赖父宽 —— **这正是「按父宽重分布」的判据**）
            // 🔴 **本节的前提是「第 1 行未领取」**（这一节之前没有任何一段动过 `DailyData`，就是出厂态 `52/500`）。
            //    🆕 2026-10-04（A44 甲）起 `description` / `Rewards` / `Progress Bar` / `progress` / `Collect`
            //    **五件都按「已领取」显隐**（判据见 `MissionsTab.BuildDailyRow` 开头那段）⇒ 下面这几条「在」**吃这个前提**。
            //    把前提先断出来：哪天前一段顺手改了状态，这里会**先报前提**，而不是看着像「四件都没建」。
            Check(DailyData.DailyState(0), DailyData.State.InProgress,
                  "（前提）第 1 行是**未领取** ⇒ 下面 `description`/`Collect`/`progress` 几条才该为「在」");
            CheckTrue(FindChild(rows[0], "description") != null, "行里有 `description`");
            CheckTrue(FindChild(rows[0], "Generic UI Button") != null, "行里有 `Collect` 按钮（未领取态）");
            CheckTrue(FindChild(rows[0], "progress") != null, "行里有 `progress` 文本（`{0}/{1}` 口径）");
            // 🔴 **行内的字号也要跟着缩放**（A124）：原版 `localScale` 缩的是**整棵子树**，TMP 的文字网格
            //    也在里头，而 prefab 里 `m_fontSize` 是**未缩放的原值** ⇒ 只缩框不缩字号 = 字比框小一圈。
            //    判据 = **TMP 渲出来的实际字号**（`Label.FontPxNow`，⛔ 不是 `MissionsTab` 里那个 35f）。
            //    `progress` 这一件走 `Txt1`（**不换行、无自适应**）⇒ 没有自适应把它拱到别的档，字面量可钉死。
            CheckNear(FontPxOf(FindChild(rows[0], "progress")), 40.25f, 1.5f,
                      "`progress` 字号 = **35 × 1.15 = 40.25**（原版 `m_fontSize` 35 × `localScale` 1.15；"
                      + "改坏法：`FS()` 那一乘漏掉 ⇒ 这条红，而所有矩形断言照样全绿）");
        }

        // ---------------- 🆕 2026-10-05（A82）：未达成的 `Collect` **真的变灰** ----------------
        // 判据（反编译 + 真包两份，落点在 `Shell/MissionsTab.cs` 的 `BuildButton`）：
        //   · 原版那颗 `Collect` 是 `EverguildButton`，`colorTintGreyOnDisable` **全是 1**（真包实读，
        //     26/26；出处 `Shell/MissionsTab.cs` 的 `BuildButton` 注释）
        //     ⇒ `interactable = false` 时 UGUI 走 `DoStateTransition(4 Disabled)` → 那条链把**图形件的材质
        //     换成 `Everguild/UI/Greyscale`**：`EverguildButton__DoStateTransition.c:92-100`（`state==4` ⇒ `bVar4=false`
        //     → `SetToStateActiveOrDisabled(false)`）→ `__SwitchMaterial.c:9`（**头一句就是 `colorTintGreyOnDisable`
        //     那道闸**）→ `EverguildButtonHelper__get_DisabledMaterial.c` 的 `Shader.Find(<那个串>)`，
        //     串本身逐字节实读 = `"Everguild/UI/Greyscale"`（`il2cpp_out/stringliteral.json` `0x42AB248`）。
        //   · 我们这一侧的唯一实现 = `WindowButton.Interactable` 的 setter → `RefreshGray()`
        //     （`Shell/PromptPopup.cs`）⇒ 这里核的是「**真的**灰了」，不是「我们打算灰」。
        // 🔴 期望值盯的是**原版那张 shader 的名字**（`AuditGrayLook` 内部逐颗比的就是它），不是我们自己的常量。
        Section("🆕 A82：未达成的 `Collect` 变灰（原版 `colorTintGreyOnDisable = 1` ⇒ 材质换 `Everguild/UI/Greyscale`）");
        {
            CheckTrue(WindowButton.GrayShaderAvailable,
                      "原版灰化 shader `Everguild/UI/Greyscale` 取得到（随包 wf_shaders.bundle）");
            int ng; string badG = WindowButton.AuditGrayLook(tab, out ng);
            CheckTrue(ng >= 4 && badG == "",
                      "未达成的 `Collect` 真的变灰（核的是**原版 shader 名**；实得 " + ng + " 颗）：" + badG);
            CheckTrue(WindowButton.MissingGrayArt.Count == 0,
                      "没有一颗钮因为取不到 shader 而没灰：" + string.Join("、", WindowButton.MissingGrayArt.ToArray()));
            // 精确条数：拿**与 builder 同一份谓词**算「该灰几颗」（`DailyData.CanCollectXxx`；谓词只有一份）
            // —— 少一颗 = 某处没接上，多一颗 = 接到了不该灰的件上。
            // ⚠️ 已领取那一行**根本不建** `Collect`（`displayRule = 1`，见 `MissionsTab.BuildDailyRow`）⇒ 先排除它。
            int wantGray = 0;
            for (int i = 0; i < 3; i++)
                if (!DailyData.DailyClaimed(i) && !DailyData.CanCollectDaily(i)) wantGray++;
            if (!DailyData.CanCollectSkulls()) wantGray++;
            if (!DailyData.CanCollectWeekly()) wantGray++;
            if (!DailyData.CanCollectLogin()) wantGray++;
            Check(ng, wantGray, "变灰的颗数 = 本页 `CanCollectXxx` 为假且**建出来了**的颗数"
                                + "（3 行每日 + 骷髅卡 + 周常 + 登录卡；登录那颗是**单机口径**、只挡 `Claimed`）");
            // 🔴 **换材质不许把显式分层抹掉** —— ✅ **2026-10-03（A85）：这一条现在由【通用修】保证**：
            //    `ImageQuad.SetMaterial` 已改成**保留换之前那份 `renderQueue`**（`Battle/ImageQuad.cs` 的 `SetMaterial`）。
            //    🔴 **订正痕迹（铁律 5）**：本段原来写「而它**只带贴图、不带 `renderQueue`**；新材质退回
            //    `Everguild/UI/Greyscale` 自带的 **Transparent(3000)**」—— 那是**改动前的行为**，**已不成立**
            //    （shader 的 SubShader 标签仍是 `Transparent`=3000，`工具/dump_shader.py` 实读——变的是
            //    **我们换完材质会把它写回旧队列**）。⇒ 下面那条断言现在盯的是**通用修有没有生效**：
            //    本行底图在 **`QPanel = 3005`**，钮要是真掉回 3000 就会被自己的行底图盖住
            //    （画面上 = 「按钮没了」，而「变了灰」那条断言**只看 shader 名、照样全绿**）。
            // ⚠️ 量与 `Background` 的**相对档位**（原版语义：按钮画在卡底图之上），不是跟我们自己的常量比。
            if (rows.Count == 3)
                for (int i = 0; i < 3; i++)
                {
                    var bt = FindChild(rows[i], "Generic UI Button");
                    var bn = FindChild(rows[i], "Background");
                    var bq = bt != null ? bt.GetComponentInChildren<ImageQuad>() : null;
                    var gq = bn != null ? bn.GetComponentInChildren<ImageQuad>() : null;
                    if (bq == null || gq == null)
                    { CheckTrue(false, $"第 {i + 1} 行的 `Collect` / `Background` 有一个没建出来"); continue; }
                    CheckTrue(bq.RenderQueue > gq.RenderQueue,
                              $"第 {i + 1} 行：变灰那颗 `Collect` 仍画在**行底图之上**"
                              + $"（按钮队列 {bq.RenderQueue} > 底图队列 {gq.RenderQueue}）");
                }
        }
        CheckTrue(FindChild(FindChild(nm, "Daily Missions"), "name (Mission Header)") != null,
                  "`Daily Missions` 上有 `Mission Header`（'Daily Missions' fs36）");

        // 🆕 2026-10-03（§三 第 29 条 **B1**）：每日骷髅卡 `counter/icons` 的 `Army` 格
        //   判据 = `d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:51-63` ——
        //   图 = `ArmyUtilities.GetArmyIcon(challenge.Army)`，**`army == Neutral(0)` 时那一格整格 `SetActive(false)`**
        //   （HLG 跳过它 ⇒ `skull` 与计数文字**整体左移 60**）。
        //   ⚠️ 我们这份 daily **没有阵营维度**（服务端下发）⇒ 走 Neutral 分支 ⇒ **那格不建、也不占位**。
        {
            var skc = FindChild(tab, "Daily Skulls Mission Container");
            CheckTrue(skc != null, "`Daily Skulls Mission Container` 建了");
            CheckTrue(skc != null && FindChild(skc, "Army") == null,
                      "`counter/icons` 的 `Army` 那一格**不建**（原版 `army == Neutral` ⇒ `SetActive(false)`）");
            var skn = skc != null ? FindChild(skc, "skull") : null;
            float sx1, sy1, sx2, sy2;
            bool hasSkull = RectOf(skn, out sx1, out sy1, out sx2, out sy2);
            CheckTrue(hasSkull, "`skull` 建了");
            if (hasSkull)
            {
                // 🔴 **关键的判别式**：`skull` 的左边缘 = **图标区自己的左边缘**（`Army` 那格没占那 60px）。
                //    原来我们固定让出 60px ⇒ 这条会红。
                // ⚠️ **不另比宽度**：骷髅卡是按**设计空间**摆再整体缩放的（两个 quad 的缩放口径不同），
                //    比宽度会把「缩放」误判成「版面错」—— 宽度在 `MissionsTab` 里由同一个设计常量给出，
                //    真正会错的是**起点**（就是上面这条）。
                CheckNear(sx1, MissionsTab.SkullIconLeftPx, 1f,
                          "`skull` 的左边缘 = **图标区左边缘**（`Army` 那格**没有占位** —— 原版 Neutral 分支同）");
                CheckTrue(sx2 - sx1 > 1f, "`skull` 有非零宽度（不是画了个零宽的东西）");
            }
        }

        Section("进度条与里程碑（§三·2 §三·7：两张条图都是**九宫格 (4,4,4,4)**）");
        var bar0 = FindChild(rows.Count > 0 ? rows[0] : tab, "Progress Bar");
        // ⚠️ 这一条同样吃上面那个前提（未领取）。**已领取**那一行原版**不建**它 ——
        //    2026-10-04（A44 甲）起我们照做，两种状态的对比在 A23 节那三条里（只差 `St` 一个字段）。
        CheckTrue(bar0 != null, "每日任务行的 `Progress Bar` 建了（未领取态）");
        if (bar0 != null)
        {
            var nines = bar0.GetComponentsInChildren<ImageQuad>(true);
            CheckTrue(nines.Length >= 1, $"进度条至少画了底（实得 {nines.Length} 块）");
        }

        Section("图：一张都不能少");
        Check(win.MissingArt.Count, 0, "没有取不到的图（取不到的件**根本没画**，所以这条必须 0）");
        CheckHoverSwap(win.transform, "奖励窗（含 Missions 线）");

        // ---------------- §三 画面逐项对（2026-09-23 找茬式审核的回归断言）----------------
        // 🔴 这一节全部是**「上一版渲染图里看得见、而断言一条都没量到」**的项 —— 期望值要么来自
        //    `工具/menu_rect.py`（独立于 C# 的第二份实现），要么来自原版 prefab 的序列化字段（注释里带出处）。

        Section("🔴 文字真的落在框里（防「字还在、但飘到框外/压在别的字上」——矩形断言量不到这一类）");
        // 起因（2026-09-23）：`Label.AlignRightOn` 把**世界坐标**写进了 `localPosition`，
        // 每日任务三行的 `timer` 与 `Refill Counter` **一个字都看不见**，而这 61 条断言全绿。
        var refill = FindChild(tab, "Refill Counter");
        var mhName = FindChild(tab, "name (Mission Header)");
        CheckTrue(refill != null, "`Refill Counter` 建了");
        CheckTrue(mhName != null, "`Mission Header` 的 `name` 建了");
        if (refill != null && mhName != null)
        {
            float rl = TextLeftPx(refill), rr = TextRightPx(refill);
            float nl = TextLeftPx(mhName), nr = TextRightPx(mhName);
            CheckTrue(rr > nl && rr < 1920f && rl > 0f,
                      $"`Refill Counter` 的**渲染**区间落在屏幕内（实得 {rl:F1}..{rr:F1}px）");
            // 原版两条 TMP 的 `m_HorizontalAlignment` 都是实测值：`name` = **1 (Left)**、`Refill Counter` 右对齐。
            // 两者框分别为 `a=(0.03,.5)-(0.84,.5)` 与 `a=(0,.5)-(1,.5) pos.x=−26.93 sz.x=−53.86`。
            CheckTrue(rr >= nr + 4f, $"`Refill Counter`（右对齐）不压在 `name`（左对齐）上（name 右 {nr:F1} / refill 右 {rr:F1}）");
            // 🔴 **2026-10-06（A124）**：右对齐要落在**缩放后**的框右边缘上。原版这个框（`Mission Header` 的
            //    `Refill Counter`，`a=(0,.5)-(1,.5) pos.x=−26.93 sz.x=−53.86`）在设计空间里右边缘是
            //    **1746.08**，`Daily Missions.localScale = 1.15` 之后 = `1190.44 + (1746.08 − 1190.44) × 1.15
            //    = **1829.43**`。⚠️ 上面那两条**都抓不到**这一处（少了 83px 也仍在屏内、也不撞 `name`）
            //    ⇒ 这条是「`AlignRightOn` 传了设计空间 x」那个错**唯一**的尺子。
            CheckNear(rr, 1829.43f, 3f,
                      $"`Refill Counter` 的右边缘 = **它框的右边缘 × 1.15**（原版 `localScale` 的几何效果；"
                      + $"实得 {rr:F1}px —— 若等于设计空间的 1746.08 就是少了那一乘）");
        }
        // 🔴 **2026-10-04（M1）谓词订正**：原版 `IsComplete()` = **奖励已领取**（**不是**「进度到顶」；
        //   两条独立证据链 → `资料/待办判据_阶段二与联机.md` §A23 三·1）⇒
        //   `description`（`dr=1` ⇒ `show = !IsComplete`）**未领取**就显示；`timer`（`dr=2` ⇒ `show = IsComplete`）**已领取**才显示。
        //   ⚠️ 出厂第 2 行是「`10/10` 但 `St = InProgress`」= **到顶未领取** ⇒ 它**仍显示** `description`、**不**显示 `timer`
        //   （1.0 版把这里断言反了，是那次反转的直接后果）。
        CheckTrue(FindChild(rows[0], "description") != null, "未达成行显示 `description`（原版 `displayRule=1` WhenActive）");
        CheckTrue(FindChild(rows[0], "timer") == null, "未达成行**不显示** `timer`（原版 `displayRule=2` WhenComplete）");
        if (rows.Count == 3)
        {
            CheckTrue(FindChild(rows[1], "description") != null,
                      "**到顶未领取**那一行**仍显示** `description`（`IsComplete` = 已领取，到顶不算）");
            CheckTrue(FindChild(rows[1], "timer") == null, "**到顶未领取**那一行**不显示** `timer`");
            // 🆕 **再造一次「已领取」态**（只差分 `St` 一个字段、进度一个数不动）—— 两种状态比才证得住。
            //    ⚠️ `Build()` 会**销毁旧节点** ⇒ 收完新行之后必须**把 `rows` 重收一遍**（本节后面 :679/:691 还要用 `rows[0]`）。
            int keepD1 = DailyData.DailyProgressValue(1);
            var mtComp0 = tab.GetComponent<MissionsTab>();
            // 🔴 **先断非空再 `Build()`**（2026-10-04 审查挑出的低危面）：`Check` 是记账式的，
            //    缺组件时直接调 `Build()` 会 `NullReferenceException` ⇒ **自检当场崩、后面一条都不跑**。
            CheckTrue(mtComp0 != null, "`Missions Tab` 上挂着 `MissionsTab`（缺了它下面的 `Build()` 会把自检崩掉）");
            DailyData.ForceDailyClaimedForTest(1, true);
            if (mtComp0 != null) mtComp0.Build();
            var rowsC = new List<Transform>();
            foreach (var t in tab.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Daily Mission Container (")) rowsC.Add(t);
            Check(rowsC.Count, 3, "强制「已领取」之后**行数不变**（重建没漏行）");
            if (rowsC.Count == 3)
            {
                CheckTrue(FindChild(rowsC[1], "timer") != null, "**已领取**行显示 `timer`（与上一态只差 `St` 一个字段）");
                CheckTrue(FindChild(rowsC[1], "description") == null, "**已领取**行**不显示** `description`");
                // 原版 `timer` 的框左边缘 = 行左 + **136.68**（`menu_rect.py "Daily Mission Container"
                // --depth 4 --relative` 实算）。那个偏量**与行宽无关** —— `timer` 是 `a=(0,1)-(1,1)` 的
                // **拉伸子件**，`0.5×行宽` 在算式里自相消（`= 3.13226 + 0.5×267.097 = 136.68`）。
                // ⇒ 设计空间：行左 + 136.68 = `1190.44 + 136.68 = 1327.12`
                //   （行左自 2026-10-06 A106 起是**布局位·2 张卡那帧的 1190.44**；
                //     旧期望 1407.99 = 模板位 1271.31 + 136.68，中间那版 918.09 = 作者态 781.41 + 136.68）
                // 🔴 **2026-10-06（A124）**：这一件在 `Daily Missions` 的子树里 ⇒ 屏上要再乘 `localScale 1.15`：
                //   `1190.44 + 136.68 × 1.15 = **1347.62**`（缩放中心 = 那个节点的 pivot `(0,1)` 左上角，
                //   所以**只有偏量**那 136.68 被放大，行左自己不动）。
                CheckNear(TextLeftPx(FindChild(rowsC[1], "timer")), 1347.62f, 3f,
                          "第 2 行 `timer` 的渲染左边缘(px)（原版 `H=Left`，框左 = 行左 + 136.68 × 1.15）");
            }
            // 还原成「`10/10` 未领取」并按新节点**重收 `rows`**（后面还有两处用它）
            DailyData.ForceDailyClaimedForTest(1, false);
            DailyData.ForceDailyProgressForTest(1, keepD1);
            if (mtComp0 != null) mtComp0.Build();
            rows.Clear();
            foreach (var t in tab.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Daily Mission Container (")) rows.Add(t);
            Check(rows.Count, 3, "还原之后**行数仍是 3**（本节起点状态回到出厂）");
        }

        Section("§三·3 登录卡 `body.image` 的 `pos=(0,−29)`（`menu_rect.py` 实算 62.14..323.27 vs 91.14..352.27）");
        var loginCard = FindChild(tab, "Daily Login Container");
        CheckTrue(loginCard != null, "`Daily Login Container` 建了");
        if (loginCard != null)
        {
            var bag = FindChild(loginCard, "image");
            // 图高 = `image` 的 261.131 × 原版 `Special Missions.localScale 1.15` = **300.30**
            CheckH(bag, 300.30f, "登录卡 `image`");
            // 图心在卡心**上方** 64.16px —— 出处：`body` 的 `pos.y = +84.7947`（设计 px，向上为正，即**上移** 84.79）
            // 与 `image` 的 `pos.y = −29`（**下移** 29）⇒ 净上移 **84.7947 − 29 = 55.7947**，再 × 1.15 = **64.16**。
            // （`menu_rect.py` 的实算：card 心 277.5、`image` 心 221.70 于 555 高的 prefab 内 ⇒ 上移 55.80 ✓）
            CheckNear((bag.position.y - loginCard.position.y) * 108f, 64.16f, 2f,
                      "登录卡 `image` 比卡心高(px)（原版 = (84.7947 − 29) × 1.15）");
        }

        Section("§三·1 `Special Missions` 的 `localScale=1.15` **要缩到子树**（2026-09-23 修）");
        var skullsCard = FindChild(tab, "Daily Skulls Mission Container");
        CheckTrue(skullsCard != null, "骷髅卡建了");
        if (skullsCard != null)
        {
            var steps = new List<Transform>();
            foreach (var t in skullsCard.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Milestone")) steps.Add(t);
            Check(steps.Count, 5, "骷髅卡 5 格里程碑");
            if (steps.Count == 5)
            {
                // 原版 `steps` 的 LG：`align=4 sp=20 ctlW=0 ctlH=0`，格 **40×40** ⇒ 缩放后 **46×46**
                CheckH(steps[0], 46f, "里程碑格（40 × 1.15）");
                float sp = Mathf.Abs(steps[1].position.x - steps[0].position.x) * 108f;
                CheckNear(sp, 69f, 2f, "里程碑格间距(px)（(40+20) × 1.15）");
                // `align=4 (MiddleCenter)` ⇒ 5×40+4×20=280 放进 325 的容器，左右各留 **22.5**（缩放后 25.875）
                float cardCx = skullsCard.position.x * 108f + 960f;
                float stepsCx = (steps[0].position.x + steps[4].position.x) * 0.5f * 108f + 960f;
                CheckNear(stepsCx - cardCx, 0f, 3f, "里程碑排的中心 = 卡心（`MiddleCenter`）");
            }
        }

        // ============================================================ 🆕 A143（2026-10-06）：SM 卡内**字号**
        Section("🆕 A143 `Special Missions` 卡内**字号**也要 ×1.15（2026-09-23 的 D7 只缩了「位置与尺寸」）");
        {
            // 🔴 **原版判据 = prefab 的 `localScale`**：`Special Missions` 的 RT 带 `m_LocalScale = (1.15,1.15)`
            //    （`资料/日常_原版规格.md`），Unity 缩的是**整棵子树** —— TMP 的文字网格也在里头，
            //    而 prefab 里那些 `m_fontSize` 是**未缩放的原值** ⇒ 卡内每一段字都得 ×1.15。
            //    我们这一侧唯一的落地处 = `Shell/MissionsTab.cs` 的 `FS(fontPx) { return fontPx * _s; }`
            //    （`_s = SM_Scale` 只在 `Build()` 里 `BuildLoginCard` + `BuildSkullsCard` 那一段为 1.15）。
            // 🔴 **下面每条期望值都是「设计空间那个字号 × 1.15」的字面量** —— ⛔ 不调 `MissionsTab.FS`、
            //    ⛔ 不读 `MissionsTab.SM_Scale`（那是拿实现证明实现，改 `SM_Scale` 照样绿）；
            //    量的也不是「我们传进去的那个数」，是 `Label.FontPxNow`（TMP 渲出来的真值）。
            // ⛔ **本节只读不写**：不碰 `DailyData`、不 `Build()` —— 上面 §三·1/§三·3 与下面 A63/A64 那几节都吃现场状态。
            var smLogin = FindChild(tab, "Daily Login Container");
            var smSkulls = FindChild(tab, "Daily Skulls Mission Container");
            CheckTrue(smLogin != null && smSkulls != null,
                      "（前提）两张 SM 卡都在（`Daily Login Container` + `Daily Skulls Mission Container`）"
                      + " —— 下面的字号断言**吃这一条**");
            // ①【`Txt` 无自适应那一路】登录卡的奖励格 `count`：设计空间 **fs40** ⇒ 屏上 **46**
            CheckFontPx(smLogin, "count 0", 46f,
                        "登录卡奖励格 `count 0` 字号 = **40 × 1.15 = 46**"
                        + "（原版 `m_fontSize` 40 × `Special Missions.localScale` 1.15）");
            // ②【同一条 `Txt`，换一个设计字号】`Collect` 按钮文案：设计空间 **fs35** ⇒ 屏上 **40.25**
            //    （⛔ 不是「凑一个常数」—— ① 与 ② 的设计字号不同，乘完之后是 46 / 40.25 两个数）
            CheckFontPx(smLogin, "Generic UI Button Text", 40.25f,
                        "登录卡 `Collect` 文案字号 = **35 × 1.15 = 40.25**");
            // ③【`Txt1` 那一路】不换行、**没有自适应**：骷髅卡的计数 `x160`，设计空间 **fs26.8** ⇒ **30.82**
            CheckFontPx(smSkulls, "counter text", 30.82f,
                        "骷髅卡 `counter text`（`x160`）字号 = **26.8 × 1.15 = 30.82**（`Txt1` 那一路）");
            // ④【另一张卡的 `Collect` 文案】🔴 **本件起这一颗开了自适应**（原版 `m_enableAutoSizing = 1`）
            //    ⇒ **不能**再断 `FontPxNow`（那是 TMP 的**收敛值**：文案在框里装不下就会比上界小），只断**窗口**。
            //    原版判据（**两处来源一致** ✓）：`GameObject/Missions Tab.json` 的
            //    `…/Daily Skulls Mission Container/background/footer/Generic UI Button/Button Text` ⇒ `min 12` / `max 44`；
            //    独立预制体 `Daily Skulls Mission Container Small` 的同一颗也是 `min 12` / `max 44`。
            //    ⇒ 窗口 = **12/44 × 1.15 = [13.8, 50.6]**。改坏法：把 `BuildButton` 的 `12f, 44f` 去掉 ⇒
            //    窗口退回「没开自适应」（`FontSizeMin/Max` 读出来不是这两个数）⇒ 红；
            //    把 `FS()` 那一乘去掉 ⇒ 实得 12.00 / 44.00 ⇒ 红。
            CheckFontWindow(smSkulls, "Generic UI Button Text", 13.8f, 50.6f,
                            "骷髅卡 `Collect` 文案的自适应窗口 = **原版 12/44 × 1.15 = [13.8, 50.6]**"
                            + "（`m_fontSizeMax 44` ≠ 设计字号 ⇒ 必须走 `Txt` 的 `autoMaxPx`）");
            // ⑤【`Txt` 带自适应那一路】卡头 `name` 的**自适应窗口**。⚠️ 这一路**不能**断 `FontPxNow`
            //    （那是 TMP 二分出来的**收敛值**：标题装不下就比上界小）⇒ 只有窗口是定死的。
            // 🔴 **原版判据（页内真值）**= `GameObject/Missions Tab.json` 的
            //    `…/Special Missions/Daily Login Container/background/header/name`：
            //    `m_enableAutoSizing = 1` · `m_fontSizeMin = **20**` · `m_fontSizeMax = 36`。
            //    （独立预制体 `MonoBehaviour_6907930910134838868.json` 的 **min 也是 20** ⇒ 两处来源一致，故可钉。）
            //    ✅ **下界已对齐**：`MissionsTab.BuildCardHeader` 本件把 `autoMinPx` **12 → 20** ⇒ **20 × 1.15 = 23.0**。
            //    改坏法：把 `autoMinPx` 那一个乘去掉/改错值 ⇒ 实得 20.00 ⇒ 红；把 `20f` 改回 `12f` ⇒ 实得 13.80 ⇒ 红。
            //    🔴 **上界 34.5 目前钉的是「我们传的 `fontPx` 30 × 1.15」** —— 页内真值的 `m_fontSizeMax` 是 **36**
            //    （⇒ 41.4）。**这是「照独立预制体 vs 照页内」的来源分叉，待调度台裁定**（报告 §七）；
            //    本件只改「两处来源一致」的那些，**没动登录卡卡头的设计字号**。
            CheckFontWindow(smLogin, "Daily Login Bonus name", 23.0f, 34.5f,
                            "登录卡卡头标题的自适应窗口（下界 **23.0** = 原版 `m_fontSizeMin` 20 × 1.15；"
                            + "⚠️ 上界 34.5 = **我们**的设计字号 30 × 1.15 —— 页内真值是 `max 36`，见报告 §七）");
            // ⑥ 骷髅卡卡头：与 ⑤ 是**同一个** `BuildCardHeader`，但**设计字号不同**（`fs36` 不是 `fs30`）
            //    ⇒ 两张卡**各配一条自己的尺子**。页内真值 `…/Daily Skulls Mission Container/background/header/name`：
            //    `m_fontSizeMin = 20` · `m_fontSizeMax = **36**`（= 我们的设计字号 ⇒ 两半都对得上 ✓）。
            //    改坏法同 ⑤（下界 23.0：改成 20.00 或 13.80 都红）。
            CheckFontWindow(smSkulls, "Daily Skulls name", 23.0f, 41.4f,
                            "骷髅卡卡头标题的自适应窗口 = **原版 20/36 × 1.15 = [23.0, 41.4]**（两处来源一致）");
            // ⑦ 骷髅卡的时钟行 `Timer`（`BuildClockRow` 那条路，**恒建** —— 不像登录卡那颗 `Timer` 吃「已领取」状态，
            //    见 A75②）。原版判据（**两处来源一致** ✓）：`Missions Tab` 页内那颗与独立预制体
            //    `Daily Skulls Mission Container Small` 的 `footer/TimerHolder/Timer` **都是** `m_fontSizeMin = 15`
            //    · `m_fontSizeMax = 38` ⇒ 窗口 = **15/38 × 1.15 = [17.25, 43.7]**。
            //    ⚠️ `m_fontSizeMax`(38) ≠ 设计字号(30.15) ⇒ 必须走 `Txt` 的 `autoMaxPx`（`TextBox` 的上界写死成 `fontPx`）。
            //    改坏法：把 `BuildSkullsCard` 里 `BuildClockRow(…, 15f, 38f)` 那两个实参去掉 ⇒
            //    拿默认的 0/0 走 ⇒ 窗口退回 `TextBox` 那一套（`FS(12)`/`FS(30.15)`）⇒ 实得 13.80 / 34.67 ⇒ 红。
            //    ⚠️ `FindChild(smSkulls, "Timer")` 是安全的：另一个叫 `Timer` 的节点在 **`Weekly Mission`** 下
            //    （周常卡），**不在 SM 子树里**。
            CheckFontWindow(smSkulls, "Timer", 17.25f, 43.7f,
                            "骷髅卡时钟行 `Timer` 的自适应窗口 = **原版 15/38 × 1.15 = [17.25, 43.7]**");
            // 🔴 **本件【故意没动】的那几条** —— 判据有**来源分叉**（照页内 `Missions Tab` vs 照独立预制体），
            //    ⛔ 不许自己挑一个：**等调度台裁定**，全文 `资料/普查产出_1006/A143_SM字号断言.md` §七。摘要：
            //      · 登录卡 `count` / `Button Text` / `TimerHolder/Timer` 的设计字号：页内真值 **40 / 44 / 38**，
            //        我们传的是 **40 / 35 / 28**（后两个取自独立预制体 `Daily Login Bonus Container`）。
            //      · 骷髅卡 `count`：页内 **40** vs 独立预制体 Small **30**（⇒ 我上一份报告说「原版 30」是**只对了一半**）。
            //      · 骷髅卡的计数节点：页内叫 `body/counter`（fs **30**、窗口 30/35），
            //        我们建的是 Small 的 `counter text`（fs 26.8）—— **连节点名都不是同一个**。
            //   ⇒ 上面 ①②③ 现在钉的是**我们的现值**（它们仍能红在「`FS()` 那一乘被去掉」上），
            //     ⛔ **不是**页内真值 —— 裁定「照页内」之后这三条要一起重算。
        }

        Section("§三·5 周常：`Handle` 与骑在它上面的 `counter`（2026-09-23 补）");
        var weekly = FindChild(tab, "Weekly Mission");
        CheckTrue(weekly != null, "周常卡建了");
        if (weekly != null)
        {
            var handle = FindChild(weekly, "Handle");
            var wcnt = FindChild(weekly, "counter");
            CheckTrue(handle != null, "`Handle` 建了（原版**无 sprite** ⇒ UGUI 画一块实心矩形，色 (0.941,0.725,0.314,1)）");
            CheckTrue(wcnt != null, "`counter`（`13/30`）建了");
            if (handle != null && wcnt != null)
            {
                CheckH(handle, 50.60f, "`Handle`（原版 4.141×50.60）");
                float d = Mathf.Abs(handle.position.x - wcnt.position.x) * 108f;
                CheckNear(d, 0f, 2f, "`counter` 中心骑在 `Handle` 上(px)（原版 `counter` 是 `Handle` 的子节点）");
                // 把手位置 = 进度条左 + 进度 × 条宽（Slider 的行为；进度 = 13/30）
                // 🔴 **2026-10-13（A390）：这一格原来是【自证型】** —— 期望值由 `DailyData.WeeklyProgress01()`
                //   自己给（`wantPx = 条左 + 条宽 × 它`）⇒ **改 `WeeklyTarget` 时两边一起变、恒绿**
                //   （A 表 A390 那一行；原始出处 = `资料/普查产出_1012/R1_骷髅与周常.md` §六·6）。
                // 🔴🔴 **落地时又查出【更大的一层】：它在此之前不是「恒绿」，而是【一次都没跑过】** ——
                //   旧写法 `FindChild(weekly, "Progress Bar")` **恒返 null**：`"Progress Bar"` 这个**节点名**
                //   **只有每日行那条路会建**（`Shell/MissionsTab.cs:540` 的 `NodeD(parent, "Progress Bar", pb)`），
                //   而周常这条是 `BuildBar(parent, bar, …)` **把九宫格直接挂在卡上**（`Shell/MissionsTab.cs` 的 `BuildWeekly` 里那句 `BuildBar(parent, bar, …)`）
                //   ⇒ 周常卡子树里**没有这个名字** ⇒ 旧那段 `if (wbar != null) { … }` **整段空转**
                //   （既没红过、也没绿过 —— A 表 / 块 1 的「自证」这个诊断只对了一半）。
                //   ⚠️ **第二条独立旁证**：即便 `wbar` 非空，旧写法也会**红** —— 那条 bar 是**九宫格**
                //   （`MenuDraw.Nine` 建 9 块子 quad），而 `Wpx()` 取的是「子树里**第一个** `ImageQuad`」
                //   = 一块 **12×12 的角块**（`BuildNine(…, 12f, 12f, …)`）、不是整条 1008.43 宽。
                //   ⇒ 两条证据只指向一件事：**这一格从来没被验过**。
                // ✅ **本件的修法（三处，都在本格内）**：
                //   ① bar 按**结构**找 —— 周常卡里 `MenuDraw.Nine` 只有进度条这一处
                //      （`BuildNine` 全仓只被 `BuildBar` 调、共 2 次：先 `40k_generial_bar_empty`、
                //       后 `…_bar_fill`，`Shell/MissionsTab.cs` 的 `BuildBar` 里那两处 `BuildNine`）⇒ **第一棵 `Nine` = 整条 bar**；
                //      矩形走 `RectOfUnion`（9 块的**并集** = bar 自己；⛔ `Wpx()` 只量得到一块角）
                //   ② **找不到就红**（⛔ 不再静默跳过 —— 这一格踩的就是「静默空转」那颗雷）
                //   ③ 期望值 = **冻结字面量** `13/30`（⛔ 不读 `WeeklyProgress01()`），进度用现成写口钉住
                //   ⚠️ **如实标注：这是【回归判据】，不是【原版读数】的判据** —— 原版那条进度是**服务端下发的
                //     玩家数据**（prefab 里那颗 `Handle` 是 Slider 把手、序列化位只是模板位）
                //     ⇒ 本地**拿不到**「原版该在哪个 x」；这一条只能钉「13/30 → 把手 x」这条换算不改。
                //   ⚠️ **本夹具【不重建】`MissionsTab`**：`MissionsTab.Build()` 会销毁整棵子树，而本节后面
                //     （`Reward 0` / `Collect` 那两处）还要用 `rows[0]` ⇒ 重建会把它们变成已销毁对象。
                //     树是在本趟开头按出厂进度 13 建的 ⇒ 把手本来就在 13/30 那一位，钉住只是把前提写明。
                //   改坏法：`DailyData.WeeklyTarget` 改回 15（或出厂进度改掉）⇒ 实现把把手挪到 13/15 那一位
                //     ⇒ 下面带 ★ 的那两条一起红（改前那一版两条**一起变**、恒绿 = 自证）。
                Transform wbar = null;                         // 整条进度条（九宫格根）
                foreach (var t in weekly.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Nine") { wbar = t; break; }
                CheckTrue(wbar != null,
                          "（A390 前提）周常卡里找得到进度条的九宫格根（`Nine`）—— 找不到就**量不了把手**；"
                        + "⛔ 这一条是「不许静默空转」：本格在 2026-10-13 之前正是因为 bar **查不到**而整段没跑过");
                DailyData.ForceWeeklyProgressForTest(13);      // 夹具把进度钉在下面那个字面量的分子上
                Check(DailyData.WeeklyProgressValue(), 13,
                      "（A390 前提）本节起点的周常进度 = **13**（出厂值；本夹具把它钉在这里）");
                const float tA390 = 13f / 30f;
                CheckNear(DailyData.WeeklyProgress01(), tA390, 1e-4f,
                          "★ A390（数据那一半）：实现侧的进度比 = **13/30**（`WeeklyProgress01`）"
                        + " —— 这一条**不依赖 bar 找不找得到**，是去掉自证之后**一定跑得到**的那一半");
                float bx1, by1, bx2, by2;
                CheckTrue(RectOfUnion(wbar, out bx1, out by1, out bx2, out by2),
                          "（A390 前提）进度条量得到渲染矩形（九宫格 9 块的**并集** = bar 自己）");
                if (RectOfUnion(wbar, out bx1, out by1, out bx2, out by2))
                    CheckNear(handle.position.x * 108f + 960f, bx1 + (bx2 - bx1) * tA390, 3f,
                              "★ A390（几何那一半）：`Handle` 的 x = 条左 + **13/30** × 条宽(px)"
                            + " —— 期望值 = **冻结字面量**，⛔ 不再由 `WeeklyProgress01()` 现算"
                            + "（改前那一版是「拿实现当期望」，而它同时还**从来没跑过**）");
            }
        }

        Section("§三 卡片 `header` 的**渐变条**（原版 `Image(sprite=null)` + `Gradient2`，2026-09-23 补）");
        CheckTrue(CountByName(root, "Mission Header bg") == 1, "`Mission Header` 的渐变条建了（每日任务那块）");
        CheckTrue(CountByName(root, "Daily Login Bonus header bg") == 1, "登录卡的渐变条建了");
        CheckTrue(CountByName(root, "Daily Skulls header bg") == 1, "骷髅卡的渐变条建了");
        CheckTrue(CountByName(root, "header bg") == 1, "周常卡的渐变条建了（灰蓝那套）");

        Section("§三·8 奖励格 `drawerHolder` 的绝对内缩（不是我们挑的百分比）");
        var rw0 = FindChild(rows.Count > 0 ? rows[0] : tab, "Reward 0");
        CheckTrue(rw0 != null, "每日任务行的奖励格建了");
        if (rw0 != null)
        {
            // 原版 `drawerHolder` = `sz=(−35.685, −42.369) p=(.5,1)`，格 126.334×150
            // ⇒ 实算 **17.84..108.49 × 0..107.63**（`menu_rect.py`）⇒ 宽 **90.65**、高 107.63。
            // ⚠️ 图本身按 `keepAspect` 等比放进这个框 ⇒ **只看宽**（高由图的宽高比定，见
            //    `资料/日常_画面逐项对_0923.md` 的「还没查清的」）。
            // 🔴 **2026-10-06（A124）**：这一格在 `Daily Missions` 的子树里 ⇒ 屏上宽 = `90.65 × 1.15 = **104.25**`
            //    （`keepAspect` 只等比放，框宽是缩放后的框宽；图本身的宽高比没变）。
            CheckW(rw0, 90.65f * 1.15f, "奖励格 `drawerHolder`（原版 90.65 × `localScale` 1.15 = 104.25 宽）");
        }

        Section("§三 `Collect` 按钮的 `preserveAspect`（原版 `m_PreserveAspect=1`，源图 `40K_button` 489×107）");
        var collect = FindChild(rows.Count > 0 ? rows[0] : tab, "Generic UI Button");
        // ⚠️ 未领取态才有这一件（A44 甲）；「不在」的话上面那条**前提**断言会先红。
        if (collect != null)
        {
            var q = collect.GetComponentInChildren<ImageQuad>();
            if (q != null)
            {
                float w = q.WorldW * 108f, h = q.WorldH * 108f;
                CheckNear(w / h, 489f / 107f, 0.02f, "`Collect` 渲染宽高比 = 源图 489/107（等比放进框）");
                // 框是 254.61×56.48；等比放进后高 = 254.61 / 4.5701 = **55.71**（拉伸的话会顶满 56.48）
                // 🔴 **2026-10-06（A124）**：框在 `Daily Missions` 的子树里 ⇒ 屏上宽 = `254.611 × 1.15 = 292.80`
                //    ⇒ 高 = `292.80 / 4.5701 = **64.07**`（原来写死的 254.61 是**设计空间**的框宽）。
                CheckNear(h, 254.61f * 1.15f / (489f / 107f), 1.0f,
                          "`Collect` 渲染高(px)（= **缩放后的**框宽 254.611×1.15 ÷ 源图宽高比 = 64.07）");
            }
        }

        // ============================================================ 🆕 A63 + A64（2026-10-04）
        Section("🆕 A63 `progress` 到顶换文案 + A64 点 `Collect` 领到之后**重建整页**");
        {
            // 🔴 本节起点的**前提**：第 1 行还是出厂那一态（`52/500` · 未领取）。
            //    （上面那些节没动过 `DailyData`；**本节末尾会还原到这里** —— 后面的截图与断言都吃这一态。）
            Check(DailyData.DailyState(0), DailyData.State.InProgress,
                  "（前提）第 1 行是**未领取**（出厂 `52/500`）—— 下面三组对比都从这一态出发");
            var mtC63 = tab.GetComponent<MissionsTab>();
            // 🔴 **先断非空再 `Build()`**：`Check` 是**记账式**的（不抛、也不早退）⇒ 缺了组件再往下就是
            //    `NullReferenceException`，自检会**当场崩**（后面一条都不跑），而不是报一条红断言。
            CheckTrue(mtC63 != null, "`Missions Tab` 上挂着 `MissionsTab`（缺了它下面的 `Build()` 会把自检崩掉）");
            if (mtC63 != null)
            {
                int keepP63 = DailyData.DailyProgressValue(0);          // 出厂 52
                // 期望坐标 = **原版的锚点五元组** `N(1, 1,0, 1,0, .5,.5, −145.3,40.7107, 254.611,56.4767)`
                // 套在本页第 1 行的矩形上（`UguiRect.Child` 是工程里唯一一份锚点算法）—— **不从实现读**。
                // 🔴 **2026-10-06（A124）**：**先在【设计空间】里套五元组、最后再整体过 `DmView`** ——
                //    顺序不能反：`UguiRect.Child` 里的 `sizeDelta`（`254.611` / `56.4767`）也是**设计空间**的量，
                //    把它喂一个「已经缩放过的父矩形」会得到 `254.611` 而不是 `254.611 × 1.15`（静默少 15%）。
                var wantC63 = DmView(UguiRect.Child(
                    MissionsTab.RowRect(new PxRect(RowRectHolderX1(), 150.28f, RowRectHolderX2(), 651.72f), 0),
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                    new Vector2(-145.3f, 40.7107f), new Vector2(254.611f, 56.4767f)));
                var pl63 = PointerLayer.Instance;
                int gold63 = Wallet.Of("40k_topmarquee_currency_gold");

                // ---------------- (i) **未达成** ⇒ 点 `Collect` 什么都不该发生（更不该重建） ----------------
                var row63 = FindChild(tab, "Daily Mission Container (0)");
                var cb63 = row63 != null ? FindChild(row63, "Generic UI Button") : null;
                var wb63 = cb63 != null ? cb63.GetComponent<WindowButton>() : null;
                CheckTrue(wb63 != null && wb63.onClick != null, "`Collect` 上挂着 `WindowButton`（点了有事做，不是装饰）");
                if (pl63 != null && wb63 != null)
                {
                    // ⚠️ 这条是**前提**，也是后面三条不弱的根据：三下点的是**同一个坐标**，
                    //    而这一点上命中的确实是这一颗 ⇒「什么都没发生」不能归因于「点错地方了」。
                    CheckTrue(pl63.ButtonAt(wantC63.CX, wantC63.CY) == wb63,
                              "（前提）那一点上命中的**就是这一颗 `Collect`**（引用相等）");
                    CheckTrue(pl63.ClickAt(wantC63.CX, wantC63.CY), "（未达成）点它 —— 指针层吃到了这一下");
                }
                Check(DailyData.DailyState(0), DailyData.State.InProgress,
                      "未达成时点 `Collect` ⇒ 状态**一点没动**（`CollectDaily` 的三态守卫）");
                Check(Wallet.Of("40k_topmarquee_currency_gold"), gold63, "…资源也一分没动");
                CheckTrue(row63 != null && FindChild(tab, "Daily Mission Container (0)") == row63,
                          "…而且**没有重建**（原版：未达成时那颗钮 `interactable = false`，`SetChallenge.c` "
                          + "`set_interactable(collectButton, CanCollect(challenge))` ⇒ 点了连派发都没有）"
                          + " —— 旧节点还活着，说明走的是「领不到就不重建」那一支");

                // ---------------- (ii) **A63**：`progress` 到顶换文案 ----------------
                // 判据（真包 MB `3476392019656054992` 逐字段实读 + 反编译亲读）：`displayRule = 1` ·
                //   `progressTextFormat = "{0}/{1}"` · **`displayCompletedMessage = 1`** ·
                //   **`completedMessage = "Missions/Completed"`**；
                //   `MissionCounterDisplay__Setup.c:24-26,68-79`：`cur < max` ⇒ `string.Format(progressTextFormat, cur, max)`，
                //   **否则** ⇒ `GetTranslation(completedMessage)`。
                var prog63 = FindChild(row63, "progress");
                CheckTrue(prog63 != null, "（前提）第 1 行的 `progress` 建了");
                Check(TextOf(prog63), "52/500",
                      "`progress` 常态 = 原版 `progressTextFormat` 的 `{0}/{1}`（出厂数据 52/500）");
                // **同一行**、只把进度推到顶（`St` 随之变 `Collectable`）—— 两组之间只差「到没到顶」这一个维度
                DailyData.ForceDailyProgressForTest(0, 500);
                mtC63.Build();
                var rowFull63 = FindChild(tab, "Daily Mission Container (0)");
                Check(DailyData.DailyState(0), DailyData.State.Collectable,
                      "（前提）这一行现在是**到顶未领取**（`500/500` · `St = Collectable`）");
                var progFull63 = FindChild(rowFull63, "progress");
                CheckTrue(progFull63 != null,
                          "到顶那一行的 `progress` **还在**（`displayRule = 1` ⇒ 未领取才显示；到顶不算「已完成」）");
                CheckTrue(Loc.HasEntry("Missions/Completed"),
                          "（前提）词条 `Missions/Completed` 在表里（⛔ 不在 ⇒ 下一条红得看不出原因）");
                Check(TextOf(progFull63), Loc.T("Missions/Completed"),
                      "到顶 ⇒ 显示 `completedMessage` 那一行的**词条文案**"
                      // 🔴 **2026-10-18 之后·第六会话就地改（铁律 5）**：本条原来断的是**字面量**
                      //   `"Missions/Completed"`（= 把**键名**当期望值）—— 那在「显示点没接表」的年代
                      //   **恰好蒙对**（屏幕上印的真是键名，`Core/Loc.cs:945` 自己把这条列在「已经上屏的真缺陷」里）。
                      //   `WCoreDeck` 把 `Shell/DailyData.cs` 的 `return CompletedMessage;` 改成
                      //   `Loc.T(CompletedMessage)` 之后，屏幕印的是**中文列「已完成」/ 英文列「Completed」**
                      //   ⇒ 按字面量断**必红**。期望值改成 `Loc.T(键)` ⇒ 两语档都成立
                      //   （同形现成写法 = `Editor/CollectionScene.cs:4650-4655` 那一对）。
                      //   ⚠️ 上面那条 `HasEntry` **不是装饰**：键若不在表，`Loc.T` 回的是**键名本身**
                      //     ⇒ 实现印键名、期望也是键名 ⇒ 这条会**假绿**，看不出「词条丢了」。
                      + "（原版存的是 I2 词条【键】、原版在它外面套 `GetTranslation`；"
                      + "本仓 EN / ZH 两列是**自拟的**——原版中文在远端、本地拿不到，见 `Core/Loc.cs:1028-1035`）");
                CheckTrue(TextOf(progFull63) != "500/500",
                          "…而且**不是** `500/500`（A63 之前我们恒显示计数 ⇒ 拿掉那一支，这条会红）");

                // ---------------- (iii) **A64**：领到之后**重建整页** ----------------
                // 与 (i) 只差「到没到顶」这一个维度：同一行、同一个坐标、同一颗钮。
                var rowBefore63 = FindChild(tab, "Daily Mission Container (0)");
                var btnBefore63 = FindChild(rowBefore63, "Generic UI Button");
                var wbBefore63 = btnBefore63 != null ? btnBefore63.GetComponent<WindowButton>() : null;
                CheckTrue(wbBefore63 != null, "（前提）到顶那一行有 `Collect`（未领取）");
                if (pl63 != null && wbBefore63 != null)
                {
                    CheckTrue(pl63.ButtonAt(wantC63.CX, wantC63.CY) == wbBefore63,
                              "（前提）那一点上命中的还是**这一颗** `Collect`（换了个状态，不是换了颗钮）");
                    CheckTrue(pl63.ClickAt(wantC63.CX, wantC63.CY), "点到顶那一行的 `Collect` —— 指针层吃到了这一下");
                }
                Check(DailyData.DailyState(0), DailyData.State.Claimed, "领到了 ⇒ `St` 变**已领取**");
                CheckTrue(Wallet.Of("40k_topmarquee_currency_gold") > gold63, "…奖励也真的入账了（单机本地兑现）");
                // 🆕 **2026-10-12（A313）**：这一下**现在会弹那扇 `Reward Window`**（`CollectDaily` 末尾那句
                // `ShowCollectedWindow`）—— 领奖窗是**弹窗** ⇒ 开着时壳被 `ToBackground()`、指针命中全归它
                // ⇒ 不关掉，本节后面每一次「点壳上的钮」都会打空、那 4 张「屏上只有壳」的截图会被它**盖住**
                // （A352 那张 `05_收件箱_空态.png` 就是这么拍错的）。关掉它 = 同一条生产路径
                // （`NotifyClosed` → `ShowPreviousWindow` 把壳带回 `Open`）。
                var rwA64 = RewardWindowFixture.FindOpenRewardWindow();
                CheckTrue(rwA64 != null, "★★ A313：每日任务**真领到** ⇒ **弹出 `Reward Window`**"
                          + "（删掉 `CollectDaily` 末尾那句 `ShowCollectedWindow` ⇒ 红）");
                if (rwA64 != null)
                {
                    var rcA64 = rwA64.Context;
                    Check(rcA64 != null && rcA64.Rewards != null ? rcA64.Rewards.Length : -1, 1,
                          "★ …带进来的是**这一格**那一条奖励（1 条 —— 不是空窗、也不是把三行都塞进来）");
                    CheckTrue(rcA64 != null && rcA64.Rewards != null && rcA64.Rewards.Length == 1
                              && rcA64.Rewards[0].Id == "40k_topmarquee_currency_gold",
                              "★ …`Id` = **这一格发出去的那张图名**（`_daily[0].RewardArt` 的出厂字面量"
                              + " —— 写死字面量而不是读 `DailyRewardArt(0)`：读它就成了同义反复）");
                    Check(rcA64 != null ? rcA64.OnCollect : null, (System.Action<CampaignData.RewardSpec[]>)null,
                          "…`onCollect` = **null**（原版 `Collect` 那一支传的就是 null ⇒ `Collect Button` 不建）");
                }
                Check(RewardWindowFixture.DismissRewardWindows(), rwA64 != null ? 1 : 0,
                      "★ …而且**关得掉**（关窗后壳从 `Background` 回 `Open` —— 后面每一颗钮才点得到）");
                // 🔴 **「重建」的判据 = 旧节点没了**：`Build()` 第一句就是销毁整棵旧子树，而
                //    `MenuWindowBase.DestroySafe` 在非 Play 下直调 `DestroyImmediate` ⇒ 引用**当场**变假空。
                //    只改状态、不重建的话，这两个引用会**还活着** ⇒ 这一条红。
                CheckTrue(rowBefore63 == null && btnBefore63 == null,
                          "…而且**旧的整棵子树被销毁了** = 真的重建了整页（原版 `TryOpenTab` → `OnOpen` → "
                          + "`CreateMissions`；`ChangeTabCO` 在「换到的正是当前页」时也照样调 slot 7）");
                var rowAfter63 = FindChild(tab, "Daily Mission Container (0)");
                CheckTrue(rowAfter63 != null && rowAfter63 != rowBefore63,
                          "…重建后是**另一个** `Daily Mission Container (0)` 实例（不是原来那个被就地改的）");
                // 新那一行必须已经是「已领取」的样子 —— 这条把 A64 接回 A44 甲那条显隐口径（画面与状态一致）
                CheckTrue(rowAfter63 != null && FindChild(rowAfter63, "Generic UI Button") == null
                          && FindChild(rowAfter63, "Reward 0") == null
                          && FindChild(rowAfter63, "Progress Bar") == null
                          && FindChild(rowAfter63, "progress") == null,
                          "…新那一行画的是**已领取**的样子（`Collect` / 奖励格 / 进度条 / `progress` 一起不建）");
                CheckTrue(rowAfter63 != null && FindChild(rowAfter63, "timer") != null,
                          "…而那五件的对偶（`timer`，`dr = 2`）**在**（重建后画面与状态一致，不是留着一排旧的）");

                // ---------------- 还原到本节起点（后面的断言与截图都吃这一态） ----------------
                DailyData.ForceDailyClaimedForTest(0, false);
                DailyData.ForceDailyProgressForTest(0, keepP63);
                mtC63.Build();
                var rowBack63 = FindChild(tab, "Daily Mission Container (0)");
                CheckTrue(rowBack63 != null && FindChild(rowBack63, "Generic UI Button") != null,
                          "还原未领取 ⇒ `Collect` **回来了**（本节回到起点状态）");
                Check(TextOf(FindChild(rowBack63, "progress")), "52/500",
                      "…`progress` 也回到 `{0}/{1}`（`52/500`）—— 三个状态合起来才证得住「**到顶才换**」");
            }
        }

        // ============================================================ 🆕 B4（2026-10-05）
        //
        // 判据 = `项目任务.md` §三 第 29 条 **B4** 那一行 + `Shell/DailyData.cs` 上
        //        `DailyRewardText` / `DailyRewardArt` 那两段注释。
        // 症状：卡面画的是 `DailyData.RewardIcon(i)` / `RewardCount(i)` —— **按下标**的固定表，
        //       而**领取**走的是**任务**那一份（`CollectDaily` → `ParseCount(t.RewardText)`）
        //       ⇒ 第 3 行显示「骷髅图标 ×150」、实发的是 `_daily[2].RewardText = 200` 的**金块**；
        //       而且重摇换了任务之后，格子里那个数**不会跟着变**。
        Section("🆕 B4 奖励格 = **任务那一份**（图标 / 数量 / 发放三者一致；⛔ 不再是按下标的固定表）");
        {
            var mtB4 = tab.GetComponent<MissionsTab>();
            // 🔴 先断非空再 `Build()`：`Check` 是记账式的（不抛、不早退）⇒ 缺组件时直调会**把整条自检崩掉**。
            CheckTrue(mtB4 != null, "`Missions Tab` 上挂着 `MissionsTab`（缺了它下面的 `Build()` 会把自检崩掉）");
            if (mtB4 != null)
            {
                int keepP2 = DailyData.DailyProgressValue(2);        // 出厂 1（目标 3）
                var keepS2 = DailyData.DailyState(2);                // 出厂 InProgress

                // ---- (i) 出厂第 3 行：老表画的是「骷髅 ×150」，而这条任务的奖励是**金块 ×200** ----
                // 期望值 = `_daily[2]` 那两个字段的**出厂字面量**（`Shell/DailyData.cs` 的 `_daily[2]`：
                //   `RewardArt "40k_topmarquee_currency_gold"` · `RewardText "200"`）——
                //   ⚠️ 那是**我们挑的 mock 数据**（任务内容本来就由服务端下发），**不是原版值**；
                //   这条断言盯的是「**卡面读的是任务那一份**」，不是「这两个数该是多少」。
                var r2 = FindChild(tab, "Daily Mission Container (2)");
                CheckTrue(r2 != null, "第 3 行在");
                Check(ArtOf(FindChild(r2, "Reward 2")), "40k_topmarquee_currency_gold",
                      "★ 第 3 行奖励格的**图标** = **任务那一份**（`_daily[2].RewardArt`；"
                      + "B4 之前那张按下标表画的是 `40K_missions_icon_Daily_skulls`）");
                Check(TextOf(FindChild(r2, "count 2")), "200",
                      "★ …**数量** = **任务那一份**（`_daily[2].RewardText` 的出厂字面量；"
                      + "B4 之前画的是 150 —— 而 `CollectDaily` 发的一直是这条任务的 200）");

                // ---- (ii) 「图标 / 数量 / 发放」三者一致：领到手的那一份 = 格子里画的那一份 ----
                // 两边都是**量的真值**：`ArtOf` 量 `ImageQuad.Texture`（画出来的图）、
                // `TextOf` 量 TMP 的串（画出来的数）、`Wallet` 量**真的进了记账的那一份**。
                DailyData.ForceDailyProgressForTest(2, 3);           // 目标 3 ⇒ 推到顶 = 可领取
                mtB4.Build();
                var r2b = FindChild(tab, "Daily Mission Container (2)");
                var cb2 = r2b != null ? FindChild(r2b, "Generic UI Button") : null;
                var wb2 = cb2 != null ? cb2.GetComponent<WindowButton>() : null;
                CheckTrue(wb2 != null && wb2.onClick != null, "（前提）到顶那一行的 `Collect` 在（点了有事做，不是装饰）");
                string a2 = ArtOf(FindChild(r2b, "Reward 2"));
                int n2 = IntOf(TextOf(FindChild(r2b, "count 2")));
                int w2 = a2 != null ? Wallet.Of(a2) : 0;
                var rwB4 = ClickCollectAndDismiss(cb2, wb2, "第 3 行的 `Collect`");
                Check(DailyData.DailyState(2), DailyData.State.Claimed, "…领到了");
                // 🆕 **2026-10-12（A313）**：这一条出口（`CollectDaily`）领到 ⇒ 弹那扇领奖窗，
                // 带进去的就是**这一格画出来那份**（`a2` = 从 `ImageQuad.Texture` 量到的图名）。
                CheckTrue(rwB4 != null, "★ A313：每日任务领到 ⇒ **弹出 `Reward Window`**（删掉 `CollectDaily` 末尾那句 ⇒ 红）");
                if (rwB4 != null && a2 != null)
                    Check(rwB4.Context != null && rwB4.Context.Rewards != null && rwB4.Context.Rewards.Length == 1
                          ? rwB4.Context.Rewards[0].Id : null, a2,
                          "★ …窗里那一条的 `Id` = **格子里画的那张图**（与「发的就是画的那一份」同源，"
                          + "B4 之前格子画骷髅、`CollectDaily` 发金块 ⇒ 这里也会对不上）");
                if (a2 != null)
                    Check(Wallet.Of(a2) - w2, n2,
                          "★ 发的就是**格子里画的那一份**（按**画出来的图标**记账、按**画出来的数量**比 —— "
                          + "B4 之前：格子画骷髅 ×150、实发金块 200 ⇒ 这条按骷髅记账得 **0 ≠ 150**）");

                // ---- (iii) 还原到本节起点（后面那一批截图里 `01_日常_Missions.png` 拍的就是这一态）----
                DailyData.ForceDailyClaimedForTest(2, false);
                DailyData.ForceDailyProgressForTest(2, keepP2);
                if (keepS2 == DailyData.State.Claimed) DailyData.ForceDailyClaimedForTest(2, true);
                mtB4.Build();
                Check(DailyData.DailyState(2), keepS2, "（还原）第 3 行回到本节起点那一态");
                CheckTrue(FindChild(FindChild(tab, "Daily Mission Container (2)"), "Reward 2") != null,
                          "…而且奖励格**回来了**（「已领取」那一行原版不建它 —— 上面那一下刚把它藏掉过）");
            }
        }

        // ============================================================ 🆕 A75（2026-10-05）
        //
        // ① 另三张卡（登录 / 骷髅 / 周常）的 `Collect` 原版走**同一条** `OnCollect` 链 ⇒ 领到之后
        //    **整页重建**（真包实读：本页四张卡的根各挂一个 `MissionContainer`、`collectButton` 全部非空
        //    —— 见 `MissionsTab.CollectThenRebuild(string,…)` 那段注释）。
        //    🔴 断言只断「**旧子树被销毁 + 新实例**」这一层，并**各配一条负例**（没领到 ⇒ 不重建）——
        //       只断一头的话，「点了就重建」与「领到才重建」分不出来。
        // ② 登录卡的 `Timer` 原版是 **`displayRule = 2 (WhenComplete)` ⇒ 已领取才显示**
        //    （真包实读：MB `7589217681052316345` 的 `infoDisplays` 第 2 项 = MB `3730529517176468153`
        //     的 `displayRule = 2`，`m_GameObject` → `GO/Timer_-4321384230747458887.json`；判据见
        //     `DailyData.LoginClaimed` 的注释）。**改之前我们恒画它。**
        //    🔴 用**同一张卡的两个状态**比（未领取 ⇒ 不建 / 已领取 ⇒ 建）：只断一头分不出
        //       「按状态显隐」与「恒建 / 恒不建」。
        Section("🆕 A75 另三张卡的 `Collect` ⇒ **整页重建** + 登录卡 `Timer` 的显隐（`displayRule = 2`）");
        {
            var pl75 = PointerLayer.Instance;
            CheckTrue(pl75 != null, "`PointerLayer` 在场景里（真鼠标走的就是它）");
            var mt75 = tab.GetComponent<MissionsTab>();
            CheckTrue(mt75 != null, "`Missions Tab` 上挂着 `MissionsTab`（缺了它下面的 `Build()` 会把自检崩掉）");
            if (mt75 != null)
            {
                // 本节起点的三张单例卡状态**逐个快照**，收工逐项还原（下面 2841 那张截图吃这一态）
                bool login0 = DailyData.LoginClaimed();
                bool skull0 = DailyData.SkullsClaimed();
                bool week0 = DailyData.WeeklyClaimed();
                // 🔴 **2026-10-11（A375）加的这一格**：骷髅**计数**也要快照 —— 出厂现在是 **0**，
                //    而 A370 给 `CanCollectSkulls` 加的第二关是 `SkullsStepDone(0)`（= 计数 ≥ **3**）
                //    ⇒ 本节 (b) 那一下「已达成 ⇒ 领得到」**必须**先把计数顶上去，否则会红在**夹具**上
                //    （红得像功能坏了，其实是本节没给足前提）。收工还原，别把 100 留在工作区。
                int skullN75 = DailyData.SkullsCountValue();

                // ---------------------------------------------------------------- ① + ② 登录卡
                CheckTrue(!login0, "（前提）登录卡起点是**未领取**（出厂 `_loginState = InProgress`）");
                var lgCard = FindChild(tab, "Daily Login Container");
                CheckTrue(lgCard != null, "登录卡在");
                CheckTrue(lgCard != null && FindChild(lgCard, "Timer") == null,
                          "★ A75② 登录卡**未领取** ⇒ `Timer` **不建**（原版 `displayRule = 2 WhenComplete`；"
                          + "改之前我们**恒画**它 ⇒ 这条会红）");
                // B4 的另两处调用点之一：登录卡**两格**画的就是 `CollectLogin` 要发的那两份。
                // 🔴 **2026-10-05（块6）：这两格的「值」不是原版给我们的 ⇒ 是我们挑的**（铁律 3）。
                //    原来这一段写「判据 = 原版 prefab §3·3 #12/#13 的 `count` 字面量」—— **已被证伪**：
                //    · 原版那两格是**数据驱动**的：`MissionRewardsDisplay__Setup.c:46` 先 `DestroyAllChildren`、
                //      再按 `MissionChallengeProgress.AvailableRewards()` 逐格 `Instantiate` + `MissionRewardItem__Setup`
                //      ⇒ `count '100'` / `'?'` 只是**会被删掉的占位**；
                //    · 第 1 格的**占位图实测是 `40k_topmarquee_currency_crystal`（不是 gold）**。
                //    ⇒ 下面这两条的期望值 = **我们的选择**，它们钉的是**自洽**（画的那份 = `CollectLogin` 发的那份），
                //      **不是**「对上原版」。⛔ 别拿「原版 prefab 那个 count」当理由去改这几个期望值。
                //      **改坏法**：把 `_loginRewardArt[0]` 换成别的币种、或让 `CollectLogin` 不逐格发 ⇒ 这两条红。
                Check(ArtOf(FindChild(lgCard, "Reward 0")), "40k_topmarquee_currency_gold",
                      "登录卡第 1 格图标 = 金块（⚠️ **我们挑的** —— 原版那一格的占位图是 crystal 系、真值运行期填；"
                      + "B4 之前**两格都按下标表**取）");
                Check(TextOf(FindChild(lgCard, "count 0")), "100",
                      "…数量 = **100**（⚠️ **我们挑的**，不是原版数；B4 之前画的是按下标表的 150）");
                Check(ArtOf(FindChild(lgCard, "Reward 1")), "40k_Achievements_icon_seal_points",
                      "登录卡第 2 格图标 = 封印点（⚠️ **我们挑的** —— 原版那一格 prefab 里只有同族通用的"
                      + "`Campaign Glow`，真值由 `CampaignPointDrawer` 运行期画）");
                Check(TextOf(FindChild(lgCard, "count 1")), "20",
                      "…数量 20（⚠️ **我们挑的** —— 与上面第 1 格同理，两格的数都是我们的选择）");
                string lgA0 = ArtOf(FindChild(lgCard, "Reward 0"));
                int lgN0 = IntOf(TextOf(FindChild(lgCard, "count 0")));
                string lgA1 = ArtOf(FindChild(lgCard, "Reward 1"));
                int lgN1 = IntOf(TextOf(FindChild(lgCard, "count 1")));
                int lgW0 = lgA0 != null ? Wallet.Of(lgA0) : 0;
                int lgW1 = lgA1 != null ? Wallet.Of(lgA1) : 0;
                var lgBtn = lgCard != null ? FindChild(lgCard, "Generic UI Button") : null;
                var lgWb = lgBtn != null ? lgBtn.GetComponent<WindowButton>() : null;
                CheckTrue(lgWb != null && lgWb.onClick != null, "（前提）登录卡的 `Collect` 在（点了有事做）");
                var rwLogin = ClickCollectAndDismiss(lgBtn, lgWb, "登录卡的 `Collect`");
                Check(DailyData.LoginClaimed(), true, "…领到了（`_loginState` 变**已领取**）");
                // 🆕 **2026-10-12（A313）**：登录卡这条出口 = `PlayerDataManager.ProcessNewLoginResult`
                // 走的那条 `RewardService.Collect`，而它收的是**一个奖励列表** ⇒ **两格装进同一扇窗**
                // （⛔ 不是只弹第一格、也不是各弹一扇）。判据 = 卡面那两格画出来的图名/数量（`lgA0/lgN0/lgA1/lgN1`）。
                CheckTrue(rwLogin != null, "★ A313：登录卡领到 ⇒ **弹出 `Reward Window`**（删掉 `CollectLogin` 末尾那句 ⇒ 红）");
                if (rwLogin != null && rwLogin.Context != null && rwLogin.Context.Rewards != null)
                {
                    var lr = rwLogin.Context.Rewards;
                    Check(lr.Length, 2, "★ …窗里是**两条**（登录卡那两格 —— 只弹第一格 / 各弹一扇的实现这里得 1）");
                    CheckTrue(lr.Length > 0 && lr[0].Id == lgA0 && lr[0].Quantity == lgN0,
                              "★ …第 1 条 = **卡面第 1 格画的那一份**（`" + lgA0 + "` × " + lgN0 + "）");
                    CheckTrue(lr.Length > 1 && lr[1].Id == lgA1 && lr[1].Quantity == lgN1,
                              "★ …第 2 条 = **卡面第 2 格画的那一份**（`" + lgA1 + "` × " + lgN1 + "）");
                }
                // ⭐ B4：领到的**逐格**等于格子里画的那一份（按**画出来的图标**记账、按**画出来的数量**比）
                if (lgA0 != null)
                    Check(Wallet.Of(lgA0) - lgW0, lgN0,
                          "★ 登录卡第 1 格画的那一份**真的发了**（B4 之前：格子画 ×150、只发 ×100 ⇒ 这条红）");
                if (lgA1 != null)
                    Check(Wallet.Of(lgA1) - lgW1, lgN1,
                          "★ …第 2 格画的那一份**也发了**（B4 之前这一格**一分都没发** ⇒ 这条得 0 ≠ 20）");
                // ⭐ A75①：整页重建 ⇒ **旧子树被销毁 + 新实例**
                CheckTrue(lgCard == null && lgBtn == null,
                          "★ 登录卡的 `Collect` 也走 `OnCollect` ⇒ **旧子树被销毁**（= 真的重建了整页；"
                          + "A75 之前这一颗只调 `CollectLogin`、不重建 ⇒ 这两个引用还活着，这条红）");
                var lgCard2 = FindChild(tab, "Daily Login Container");
                CheckTrue(lgCard2 != null && lgCard2 != lgCard,
                          "…重建后是**另一个** `Daily Login Container` 实例（不是原来那个被就地改的）");
                // ⭐ A75② 的另一半：**已领取** ⇒ `Timer` 出现，文案 = 原版 prefab 那个串
                CheckTrue(lgCard2 != null && FindChild(lgCard2, "Timer") != null,
                          "★ …而重建后**已领取** ⇒ `Timer` **出现了**（与上面「未领取不建」合起来才证得住"
                          + "它是**按状态**显隐，不是恒建 / 恒不建）");
                Check(TextOf(FindChild(lgCard2, "Timer")), "Resets in 12h 34 m",
                      "…`Timer` 文案 = **原版 prefab 出厂那个串**（正本 §3·3 #16 `'Resets in 12h 34 m'`）");

                // ---------------------------------------------------------------- ① + B4 骷髅卡
                // 🔴 **2026-10-11（A375）**：先把**计数**顶到 100 —— A370 起「领得到」是**两关**
                //    （`_skullsState == Collectable` **且** `SkullsStepDone(0)` = 计数 ≥ 3），
                //    而 A375 把出厂计数从 mock `160` 改成了**真的 0** ⇒ 光置状态**领不到**，
                //    下面 (b) 那几条会红在**夹具**上（看着像 `CollectSkulls` 坏了）。
                //    改坏法：删掉这一句 ⇒ 紧跟着 (b) 的「领到了」立刻红。
                DailyData.ForceSkullsCountForTest(100);
                // (a) **未达成** ⇒ 点它：没领到、**不重建**（原版那颗钮 `interactable = CanCollect()` = false）
                DailyData.ForceSkullsStateForTest(DailyData.State.InProgress);
                mt75.Build();
                var skA = FindChild(tab, "Daily Skulls Mission Container");
                var skBtnA = skA != null ? FindChild(skA, "Generic UI Button") : null;
                var skWbA = skBtnA != null ? skBtnA.GetComponent<WindowButton>() : null;
                CheckTrue(skWbA != null && skWbA.onClick != null, "（前提）骷髅卡的 `Collect` 在");
                CheckTrue(ClickButtonByQuad(pl75, skBtnA, skWbA, "骷髅卡的 `Collect`（未达成）"),
                          "（未达成）点它 —— 指针层吃到了这一下");
                Check(DailyData.SkullsClaimed(), false, "…但**没领到**（`CollectSkulls` 的三态守卫）");
                // 🆕 **2026-10-12（A313）**：**负例** —— 守卫挡下的那一下**一扇窗都不许弹**。
                // 这一条把 `ShowCollectedWindow` 钉在**成功分支之内**（写在守卫之前 / 写在 `Say` 旁边
                // 都会让「点了就弹、哪怕没领到」蒙混过去）。
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ …而且**一扇领奖窗都没弹**（A313：没领到 ⇒ 不弹）");
                CheckTrue(skA != null && FindChild(tab, "Daily Skulls Mission Container") == skA,
                          "…而且**没有重建**（旧节点还活着 ⇒ 重建是「**领到了才做**」，不是「点了就做」）");
                // (b) **已达成** ⇒ 领到 + 重建（B4 的另一处调用点：格子里画的那一份就是发出去的那一份）
                DailyData.ForceSkullsStateForTest(DailyData.State.Collectable);
                mt75.Build();
                var skB = FindChild(tab, "Daily Skulls Mission Container");
                var skBtnB = skB != null ? FindChild(skB, "Generic UI Button") : null;
                var skWbB = skBtnB != null ? skBtnB.GetComponent<WindowButton>() : null;
                CheckTrue(skWbB != null && skWbB.onClick != null, "（前提）达成之后的 `Collect` 在");
                // 🔴 **2026-10-05（块6）：这一格的「值」不是原版给我们的**（铁律 3）。原来写「判据 = 原版 prefab
                //    §3·4 #6：`CampaignPointDrawer`、count 出厂字面量 `'200'`」—— **已被证伪**，两半都不成立：
                //    · 原版那一格**数据驱动**（`MissionRewardsDisplay__Setup.c:46` 先 `DestroyAllChildren` 再逐格
                //      `Instantiate`）⇒ `count '200'` 是**会被删掉的占位**；
                //    · **图更不是 prefab 给的**：该格 Image 实测 **`m_Sprite = 0`（没图）**，同级那件是同族通用的
                //      `Campaign Glow` = `40K_genearl_icon_Campaign_points_big`（这张**我们工程里也有**）。
                //
                // 🔴🔴 **2026-10-11（批次 · A370）就地订正（铁律 5）**：下面两条的期望值**换了** ——
                //    报告人给了**原版实拍**（`C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png`
                //    左栏「每日骷髅头」卡）：那一格画的是**蓝色漩涡 = 活动点图标 × 200**，**不是骷髅**。
                //    ⇒ 原来那两条（期望骷髅）**方向就是错的**：「骷髅」是**进度的输入**、不是奖励产出，
                //      照那么发 = **自己给自己发进度**。判据全文 → `Shell/DailyData.cs` 的「骷髅卡」那一段。
                //    ⛔ **上面那句「改坏法」也翻过来了**（原文写「改成照 prefab 画就会红」）——
                //      **照 prefab 画（`Campaign_points_big`）现在正是对的**，改回骷髅才红。
                //    ✅ 仍然成立的是 B4 那条**自洽**：卡面画的 = `CollectSkulls` 发的（下面四行按**画出来的**
                //      图标/数量记账，所以自洽这条与「对不对得上原版」无关，各管各的）。
                Check(ArtOf(FindChild(skB, "Reward 1")), "40K_genearl_icon_Campaign_points_big",
                      "骷髅卡奖励格的**图标** = **活动点**（照原版实拍；A370 之前画的是**骷髅** —— 那是进度的输入，"
                      + "当奖励发等于自己给自己发进度）");
                Check(TextOf(FindChild(skB, "count 1")), "200",
                      "…**数量** = 200 **活动点**（照原版实拍；数没变、**含义变了** —— A370 之前是「200 个骷髅」）");
                string skArt = ArtOf(FindChild(skB, "Reward 1"));
                int skN = IntOf(TextOf(FindChild(skB, "count 1")));
                int skW = skArt != null ? Wallet.Of(skArt) : 0;
                var rwSk = ClickCollectAndDismiss(skBtnB, skWbB, "骷髅卡的 `Collect`（已达成）");
                Check(DailyData.SkullsClaimed(), true, "…领到了");
                // 🆕 **2026-10-12（A313）**：骷髅卡这条出口（原版 `Missions.<CollectChallenge>…`）同走
                // `RewardService.Collect` ⇒ 领到 ⇒ 弹那扇窗，带进去的就是**格子里画的那一份**（`skArt`）。
                CheckTrue(rwSk != null, "★ A313：骷髅卡领到 ⇒ **弹出 `Reward Window`**（删掉 `CollectSkulls` 末尾那句 ⇒ 红）");
                if (rwSk != null && skArt != null)
                    Check(rwSk.Context != null && rwSk.Context.Rewards != null && rwSk.Context.Rewards.Length == 1
                          ? rwSk.Context.Rewards[0].Id : null, skArt,
                          "★ …窗里那一条的 `Id` = **格子里画的那张图**（A370：`40K_genearl_icon_Campaign_points_big`）");
                if (skArt != null)
                    Check(Wallet.Of(skArt) - skW, skN,
                          "★ 发的就是**格子里画的那一份**（B4 之前：格子画封印点 ×20、`CollectSkulls` 发的是"
                          + " **0 个骷髅** ⇒ 这条按骷髅记账得 0 ≠ 200）");
                CheckTrue(skB == null && skBtnB == null,
                          "★ 骷髅卡的 `Collect` 走**同一条链** ⇒ **旧子树被销毁**（= 重建了整页）");
                var skC = FindChild(tab, "Daily Skulls Mission Container");
                CheckTrue(skC != null && skC != skB, "…重建后是**另一个** `Daily Skulls Mission Container` 实例");

                // ---------------------------------------------------------------- ① 周常卡
                // (a) 未达成 ⇒ 不重建
                DailyData.ForceWeeklyStateForTest(DailyData.State.InProgress);
                mt75.Build();
                var wkA = FindChild(tab, "Weekly Mission");
                var wkBtnA = wkA != null ? FindChild(wkA, "Generic UI Button") : null;
                var wkWbA = wkBtnA != null ? wkBtnA.GetComponent<WindowButton>() : null;
                CheckTrue(wkWbA != null && wkWbA.onClick != null, "（前提）周常卡的 `Collect` 在");
                CheckTrue(ClickButtonByQuad(pl75, wkBtnA, wkWbA, "周常卡的 `Collect`（未达成）"), "（未达成）点它");
                Check(DailyData.WeeklyClaimed(), false, "…没领到（`CollectWeekly` 的守卫）");
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ …而且**一扇领奖窗都没弹**（A313 的负例；同上一条）");
                CheckTrue(wkA != null && FindChild(tab, "Weekly Mission") == wkA, "…也没有重建");
                // (b) 达成 ⇒ 领到 + 重建
                DailyData.ForceWeeklyStateForTest(DailyData.State.Collectable);
                mt75.Build();
                var wkB = FindChild(tab, "Weekly Mission");
                var wkBtnB = wkB != null ? FindChild(wkB, "Generic UI Button") : null;
                var wkWbB = wkBtnB != null ? wkBtnB.GetComponent<WindowButton>() : null;
                CheckTrue(wkWbB != null && wkWbB.onClick != null, "（前提）达成之后的 `Collect` 在");
                var rwWk = ClickCollectAndDismiss(wkBtnB, wkWbB, "周常卡的 `Collect`（已达成）");
                Check(DailyData.WeeklyClaimed(), true, "…领到了");
                // 🆕 **2026-10-12（A313）**：周常这条出口同走那一条链 ⇒ 领到 ⇒ 弹窗。
                // ⚠️ 周常**没有「奖励格」这一件**（原版 `Rewards` 出厂 `activeSelf = false`）⇒ 这里没有
                // 「画的那一份」可对，只能断**窗里那一条 = `CollectWeekly` 真发出去的那一份**
                // （500 金块是**我们挑的**，见 `Shell/DailyData.cs` 那段）。
                CheckTrue(rwWk != null, "★ A313：周常领到 ⇒ **弹出 `Reward Window`**（删掉 `CollectWeekly` 末尾那句 ⇒ 红）");
                if (rwWk != null && rwWk.Context != null && rwWk.Context.Rewards != null)
                {
                    var wr = rwWk.Context.Rewards;
                    Check(wr.Length, 1, "★ …窗里**一条**奖励（周常只发一份）");
                    CheckTrue(wr.Length > 0 && wr[0].Id == "40k_topmarquee_currency_gold" && wr[0].Quantity == 500,
                              "★ …而那一条 = `CollectWeekly` **真发出去**的那一份（金块 × 500）");
                }
                CheckTrue(wkB == null && wkBtnB == null, "★ 周常卡的 `Collect` 也走同一条链 ⇒ **旧子树被销毁**");
                var wkC = FindChild(tab, "Weekly Mission");
                CheckTrue(wkC != null && wkC != wkB, "…重建后是**另一个** `Weekly Mission` 实例");
                // ⚠️ 周常**没有「奖励格」这一件**（原版 `Rewards` 出厂 `activeSelf = false`，正本 §3·5 #8）
                //    ⇒ B4 改的是「每日行 / 登录卡 / 骷髅卡」三处，周常这份奖励只在 `CollectWeekly` 里。

                // ---------------------------------------------------------------- 还原到本节起点
                DailyData.ForceLoginStateForTest(login0 ? DailyData.State.Claimed : DailyData.State.InProgress);
                DailyData.ForceSkullsStateForTest(skull0 ? DailyData.State.Claimed : DailyData.State.InProgress);
                DailyData.ForceSkullsCountForTest(skullN75);      // 🔴 A375：计数也还原（上面顶到过 100）
                DailyData.ForceWeeklyStateForTest(week0 ? DailyData.State.Claimed : DailyData.State.InProgress);
                mt75.Build();
                Check(DailyData.SkullsCountValue(), skullN75, "（还原）骷髅计数回到本节起点那个值（A375）");
                Check(DailyData.LoginClaimed(), login0, "（还原）登录卡回到本节起点那一态");
                var lgBack = FindChild(tab, "Daily Login Container");
                CheckTrue(lgBack != null && FindChild(lgBack, "Timer") == null,
                          "…未领取 ⇒ `Timer` **又没了**（未领取不建 / 已领取建 / 还原后又不建 —— "
                          + "三个状态合起来才证得住这条显隐是**真的按状态**走的）");
            }
        }

        // ============================================================ 🆕 A370（2026-10-11）
        //
        // 每日骷髅卡三处**照原版实拍**改对（判据 = 用户提供的原版实拍
        // `C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png` 左栏「每日骷髅头」卡）：
        //   ① 五档阈值 = **3 / 10 / 25 / 50 / 100**（原来是我们挑的「200 五等分」⇒ 40/80/120/160/200）；
        //   ② 那一格奖励 = **活动点 ×200**（原来画 + 发的是**骷髅** ×200 ⇒ 等于自己给自己发进度）；
        //   ③ 可点性多一条「**至少过第 1 档**」（第 1 档现在是 **3**，不是 40）。
        // 🔴 **本节期望值一律用【字面量】**（`3/10/25/50/100` 与那张图名）——
        //    ⛔ 不从 `DailyData` 自己的常量里取：那是**同义反复**，改坏实现它会跟着一起变绿
        //    （本仓「断言自证」那条坑）。
        // 🔴 **每条都断【两态】**：只断一头的话，「按阈值算」与「恒亮 / 恒灭」分不出来
        //    （本仓「弱断言分不出两种状态」那条坑）。
        Section("🆕 A370 每日骷髅卡：五档阈值 3/10/25/50/100 + 奖励是**活动点**不是骷髅");
        {
            int skullCnt0 = DailyData.SkullsCountValue();   // 起点快照（收工还原 —— 这一格下面那几段要临时改它）
            bool skullClaim0 = DailyData.SkullsClaimed();
            var mt370 = tab.GetComponent<MissionsTab>();
            CheckTrue(mt370 != null, "（前提）`Missions Tab` 上挂着 `MissionsTab`（缺了它下面 `Build()` 会把自检崩掉）");
            if (mt370 != null)
            {
                // 数**画出来的**亮格：节点名由 `MissionsTab.BuildMilestone` 定死为 `Milestone_on` / `_off`
                // （`Draw(..., "Milestone" + (done ? "_on" : "_off"), ...)`）。
                // ⚠️ 取不到卡时回 **−1**（⛔ 不回 0 —— 0 会和「一格都没亮」撞上，又是一条分不出状态的断言）。
                System.Func<int> onCount = () =>
                {
                    var c = FindChild(tab, "Daily Skulls Mission Container");
                    if (c == null) return -1;
                    int n = 0;
                    foreach (var t in c.GetComponentsInChildren<Transform>(true))
                        if (t.name == "Milestone_on") n++;
                    return n;
                };

                // ---------------------------------------------------------------- ⓪ 出厂态（= A373 那条欠账的验收）
                // 🔴 **2026-10-11（A375）**：出厂计数从 mock **`160`** 改成**真的 0**
                //    ⇒ 卡面必须就是 **`x0` + 五格全灭**，与用户给的原版实拍逐字一致
                //    （`C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png` 左栏「每日骷髅头」卡；
                //     差异清单第 4 条 → `资料/普查产出_1011/R1_每日骷髅与登录卡.md` §六 #4）。
                //    改坏法：把 `_skullsCount` 的出厂值改回任何非零值 ⇒ 下面前两条立刻红。
                mt370.Build();
                Check(skullCnt0, 0, "★ （前提）进本节时骷髅计数 = **0**（对局那边没跑过 ⇒ 就是出厂值；"
                                    + "A375 之前这里是 mock `160`）");
                Check(onCount(), 0, "★ 计数 0 ⇒ **五格全灭**（与实拍一致；mock `160` 那个年代这里恒 **5**）");
                var skF0 = FindChild(tab, "Daily Skulls Mission Container");
                Check(skF0 != null ? TextOf(FindChild(skF0, "counter text")) : null, "x0",
                      "★ ...计数器印的是 **`x0`**（原版 `progressTextFormat = 'x{0}'` —— 实拍那张卡上印的也是 `x0`）");

                // ---------------------------------------------------------------- ① 五档阈值
                var wantA370 = new[] { 3, 10, 25, 50, 100 };
                Check(DailyData.SkullsStepCount, 5, "五档 = **5 格**（实拍上就是 5 个方框）");
                for (int i = 0; i < wantA370.Length; i++)
                    Check(DailyData.SkullsStepTarget(i), wantA370[i],
                          "...第 " + (i + 1) + " 档阈值 = **" + wantA370[i] + "**（照原版实拍；"
                          + "A370 之前是「200 五等分」⇒ " + ((i + 1) * 40) + "）");

                // ①·b **行为**那一面：上面断的是数据，这里断「计数 → 亮几格」
                //   （`DailyData.SkullsStepDone` 是**唯一**决定方框亮不亮的地方，见 `MissionsTab.BuildSkullsCard`）。
                DailyData.ForceSkullsCountForTest(2);
                mt370.Build();
                Check(onCount(), 0, "★ 计数 = **2** ⇒ **一格都没亮**（第 1 档阈值是 **3**，不是 A370 之前的 40）");
                DailyData.ForceSkullsCountForTest(3);
                mt370.Build();
                Check(onCount(), 1, "★ 计数 = **3** ⇒ **第 1 档亮**（与上一条合起来才分得清「按阈值算」与「恒灭 / 恒亮」）");
                DailyData.ForceSkullsCountForTest(99);
                mt370.Build();
                Check(onCount(), 4, "★ 计数 = **99** ⇒ 前四档亮、**第 5 档灭**（⇒ 第 5 档那个阈值**不是**别的档的）");
                DailyData.ForceSkullsCountForTest(100);
                mt370.Build();
                Check(onCount(), 5, "★ 计数 = **100** ⇒ **五档全亮**（与上一条合起来把「第 5 档 = 100」钉在行为上）");

                // ---------------------------------------------------------------- ② 那一格 = 活动点 ×200
                // ⛔ 图名写字面量（不从 `DailyData.SkullsRewardArt()` 取）。判据 = 实拍那颗蓝色漩涡 +
                //    本格 prefab 的 `Campaign Glow`（`menu_dump.py … "Daily Skulls Mission Container Small"` 实读）。
                const string CPArtA370 = "40K_genearl_icon_Campaign_points_big";
                const string SkullArtA370 = "40K_missions_icon_Daily_skulls";
                DailyData.ForceSkullsCountForTest(100);              // 至少过第 1 档（守卫那条）
                DailyData.ForceSkullsStateForTest(DailyData.State.Collectable);
                mt370.Build();
                var sk370 = FindChild(tab, "Daily Skulls Mission Container");
                CheckTrue(sk370 != null, "（前提）骷髅卡在");
                Check(ArtOf(FindChild(sk370, "Reward 1")), CPArtA370,
                      "★ 奖励格画的是**活动点**图标（照原版实拍；A370 之前画的是**骷髅**⇒ 这条红 —— "
                      + "骷髅是**进度的输入**，当奖励发等于自己给自己发进度）");
                Check(TextOf(FindChild(sk370, "count 1")), "200",
                      "...数量 200（数没变、**含义变了**：A370 之前是「200 个骷髅」）");
                // 真点一下：按**那格画出来的图**记账、再按**骷髅那把 key** 记一遍
                int cpW0 = Wallet.Of(CPArtA370);
                int skullW0 = Wallet.Of(SkullArtA370);
                var btn370 = sk370 != null ? FindChild(sk370, "Generic UI Button") : null;
                var wb370 = btn370 != null ? btn370.GetComponent<WindowButton>() : null;
                CheckTrue(wb370 != null && wb370.onClick != null, "（前提）骷髅卡的 `Collect` 在（点了有事做）");
                var pl370 = PointerLayer.Instance;
                CheckTrue(pl370 != null, "（前提）`PointerLayer` 在场景里（真鼠标走的就是它）");
                var rw370 = ClickCollectAndDismiss(btn370, wb370, "骷髅卡的 `Collect`（A370）");
                Check(DailyData.SkullsClaimed(), true, "...领到了");
                // 🆕 **2026-10-12（A313）**：这一处是**同一颗钮的第二次真点**（上面 A75 那节已点过一次、
                // 中间状态被还原过）⇒ 它同时也验「领奖窗**不是一次性**的」（第二次照样弹、照样关得掉）。
                CheckTrue(rw370 != null, "★ A313：这一下也**弹出 `Reward Window`**（隔了一节、状态还原过之后仍然弹）");
                if (rw370 != null && rw370.Context != null && rw370.Context.Rewards != null
                    && rw370.Context.Rewards.Length > 0)
                    Check(rw370.Context.Rewards[0].Id, CPArtA370,
                          "★ …带进去的就是**格子里画的活动点**那张图（与上面按图记账那两条同源）");
                Check(Wallet.Of(CPArtA370) - cpW0, 200, "★ 发的是 **200 活动点**（按格子里画出来的那张图记账）");
                Check(Wallet.Of(SkullArtA370) - skullW0, 0,
                      "★ 而 **一个骷髅都没发**（A370 之前这一下发的就是 200 个骷髅 ⇒ 这条红）");

                // ---------------------------------------------------------------- ③ 可点性：至少过第 1 档
                // 两态：`2` 点不动 / `3` 点得动 —— 这一对把「第 1 档 = 3」钉在**行为**上（不只是数据）。
                DailyData.ForceSkullsStateForTest(DailyData.State.Collectable);
                DailyData.ForceSkullsCountForTest(2);
                mt370.Build();
                var skG = FindChild(tab, "Daily Skulls Mission Container");
                var btnG = skG != null ? FindChild(skG, "Generic UI Button") : null;
                var wbG = btnG != null ? btnG.GetComponent<WindowButton>() : null;
                CheckTrue(wbG != null, "（前提）那颗 `Collect` 在");
                Check(DailyData.CanCollectSkulls(), false,
                      "★ 计数 = **2**（第 1 档都没过）⇒ **领不了**（守卫里那条 `SkullsStepDone(0)`；"
                      + "A370 之前只判状态 ⇒ 这条红）");
                CheckTrue(wbG != null && !wbG.Interactable, "...而且那颗钮**真的变灰了**（`interactable = false` 的两半之一）");
                DailyData.ForceSkullsCountForTest(3);
                Check(DailyData.CanCollectSkulls(), true, "★ 计数 = **3** ⇒ **领得了**（与上一条合起来把「第 1 档 = 3」钉住）");
                mt370.Build();     // 重建一次：**变灰**那半边也要断两态（灰 ⇄ 不灰），只断一头分不出「按状态变灰」与「恒灰」
                var skG2 = FindChild(tab, "Daily Skulls Mission Container");
                var btnG2 = skG2 != null ? FindChild(skG2, "Generic UI Button") : null;
                var wbG2 = btnG2 != null ? btnG2.GetComponent<WindowButton>() : null;
                CheckTrue(wbG2 != null && wbG2.Interactable,
                          "★ ...重建后那颗钮**又点得动了**（`CanCollectSkulls` 是 `CollectSkulls` 与按钮**共用的同一份布尔**）");

                // ---------------------------------------------------------------- 还原到本节起点
                DailyData.ForceSkullsCountForTest(skullCnt0);
                DailyData.ForceSkullsStateForTest(skullClaim0 ? DailyData.State.Claimed : DailyData.State.InProgress);
                mt370.Build();
                Check(DailyData.SkullsCountValue(), skullCnt0, "（还原）骷髅计数回到本节起点那个值");
            }
        }

        // ============================================================ 🆕 A371 + A372（2026-10-12）
        // 判据 = **用户提供的原版实拍** `C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png`：
        //   · 骷髅卡（左栏「每日骷髅头」）：5 个方框里**逐格印着** `3 10 25 50 100`；
        //   · 周常（「每周挑战」一行）：**6 个宝箱**，下面逐格印着 `5 10 15 20 25 30`，右上计数 `25/30`。
        // 🔴 本节期望值**一律是字面量**（阈值 / 格数 / 字号窗口 / 数字框相对格心的偏移）——
        //    ⛔ 不从 `DailyData` 或 `MissionsTab` 自己的常量里取（那是同义反复，改坏实现它会跟着变绿）。
        // 🔴 每条都断**两态**或断**可分辨的量**（只断一头分不出「按阈值算」与「恒亮 / 恒灭」）。
        // **改坏法**（四条，各红在不同的一组上）：
        //   ① `MissionsTab.BuildMilestone` 里那句 `Txt(..., "Step Text", ...)` 删掉 ⇒ ①② 两组红；
        //   ② 周常循环改回 `< 4`（或 `WeeklyStepCount` 改回 4）⇒ ②③ 红；
        //   ③ `WeeklyStepDone` 改回读手写布尔（与 `_weeklyProgress` 两处各说各话）⇒ ③ 那四条红；
        //   ④ 数字框改回「与格同心」（= 每日卡那一支）⇒ ② 的偏移那条红。
        Section("🆕 A371/A372 里程碑格里的**阈值数字** + 周常 **6 格**（实拍 `5 10 15 20 25 30`）");
        {
            var mt37 = tab.GetComponent<MissionsTab>();
            CheckTrue(mt37 != null, "（前提）`Missions Tab` 上挂着 `MissionsTab`");
            if (mt37 != null)
            {
                // 按**精确名**收节点（遍历是层次序、兄弟序 ⇒ 与创建序一致；两个过滤各自保序 ⇒ 下标一一对应）
                System.Func<Transform, string, List<Transform>> allNamed = (root37, nm) =>
                {
                    var l = new List<Transform>();
                    if (root37 == null) return l;
                    foreach (var t in root37.GetComponentsInChildren<Transform>(true))
                        if (t.name == nm) l.Add(t);
                    return l;
                };
                System.Func<Transform, List<Transform>> allCells = root37 =>
                {
                    var l = new List<Transform>();
                    if (root37 == null) return l;
                    foreach (var t in root37.GetComponentsInChildren<Transform>(true))
                        if (t.name == "Milestone_on" || t.name == "Milestone_off") l.Add(t);
                    return l;
                };
                int wkBase37 = DailyData.WeeklyProgressValue();      // 本节起点（③ 那段要临时改它，收工还原）
                System.Func<Transform> wkR37 = () => FindChild(tab, "Weekly Mission");   // 每次现找（重建会换节点）

                mt37.Build();
                // 🔴 **重建之后再找节点**：`Build()` 会 `DestroySafe` 掉整棵子树、再重挂一棵新的
                //    ⇒ 建之前抓到的 `Transform` 引用在重建后**是已销毁对象**（读它 = 抛异常）。
                //    ③ 那段每重建一次都要重找 ⇒ 走下面这个「每次现找」的闭包。

                // ---------------------------------------------------------------- ① 骷髅卡：5 格 · 每格一个数
                var sk37 = FindChild(tab, "Daily Skulls Mission Container");
                CheckTrue(sk37 != null, "（前提）骷髅卡在（`Daily Skulls Mission Container`）");
                var skNums = allNamed(sk37, "Step Text");
                Check(skNums.Count, 5, "★ 骷髅卡 **5 格各印一个数**（节点名 `Step Text`；"
                                       + "A371 之前那 5 格**只有底图、一个数都没画** ⇒ 这条红）");
                var wantD37 = new[] { "3", "10", "25", "50", "100" };
                for (int i = 0; i < wantD37.Length; i++)
                    Check(i < skNums.Count ? TextOf(skNums[i]) : "<格数不足 5>", wantD37[i],
                          "★ ...第 " + (i + 1) + " 格印的是 **" + wantD37[i] + "**（照原版实拍逐格；"
                          + "数就是从阈值来的 —— 与 A370 那节断的 `SkullsStepTarget` 同源）");
                // 框与字号：原版 `Mission Milestone Step/text` 实读 = **40×40（与方框同框）**· fs **42.2** ·
                // auto[**10~50**]（`工具/menu_dump.py … "Daily Skulls Mission Container Small"`）。
                // ⇒ 在 `Special Missions` 子树里还要各 ×1.15（`MissionsTab.FS`）= **[11.5, 57.5]**。
                // ⚠️ 断的是**窗口**不是 `FontPxNow`（开了自适应时那是 TMP 二分出来的收敛值，会时红时绿 ——
                //    同 A143 那条口径）。改坏法：把 `Txt(..., 10f, 50f)` 那两个实参去掉 ⇒ 上界退回设计字号
                //    （42.2 × 1.15 = 48.53）⇒ 红。
                CheckFontWindow(sk37, "Step Text", 11.5f, 57.5f,
                                "★ 骷髅卡格里的数字：自适应窗口 = 原版 `[10,50]` × 1.15");
                // 位置：原版那个 `text` 的 anchors 在 40×40 的 `holder` 上是 (0,0)-(1,1)、sizeDelta (0,0)
                // ⇒ **与格子同心**（⛔ 不是「印在方框下面」那一支，那是周常）。
                var skCells = allCells(sk37);
                if (skCells.Count == 5 && skNums.Count == 5)
                    CheckNear(Vector3.Distance(skCells[0].position, skNums[0].position) * 108f, 0f, 1f,
                              "★ 骷髅卡第 1 格的数字与格心重合(px)（原版 `text` 与 `holder` 同框）");

                // ---------------------------------------------------------------- ② 周常：6 格 · 每格一个数
                CheckTrue(wkR37() != null, "（前提）周常卡在（`Weekly Mission`）");
                var wkNums = allNamed(wkR37(), "Step Text");
                var wkCells = allCells(wkR37());
                Check(wkNums.Count, 6, "★ 周常 **6 格各印一个数**（A372 之前是 **4 格**、且一个数都没画 ⇒ 这条红）");
                Check(wkCells.Count, 6, "★ 周常卡上**画出来的里程碑格 = 6**（节点名 `Milestone_on/_off` 各算一个）");
                var wantW37 = new[] { "5", "10", "15", "20", "25", "30" };
                for (int i = 0; i < wantW37.Length; i++)
                    Check(i < wkNums.Count ? TextOf(wkNums[i]) : "<格数不足 6>", wantW37[i],
                          "★ ...第 " + (i + 1) + " 格印的是 **" + wantW37[i] + "**（照原版实拍「每周挑战」那一行）");
                // 字号窗口：🔴 **2026-10-13（调度台追加件 · 判据 = 同批 WM2 亲跑 `menu_dump`）**：期望值从 `10f`
                // 改成 **`15f`** —— 判据源原来写错了（写成了**页内作者预览**那一份）：
                //   · **运行期 `Instantiate` 用的那一份** = `Weekly Mission Milestone T1` 的 `holder/text`
                //     = **`auto[15.0~50.0]`** ← **这才是判据**；
                //   · `Weekly Mission Milestones Step (3)` 的 `auto[10.0~50.0]` 是**页内作者预览**
                //     （会被 `MissionMilestonesDisplay.Setup` 的 `DestroyAllChildren` 删掉）⇒ ⛔ 别拿它当原版值。
                // ⇒ 同一件事 `资料/普查产出_1013/WM1_任务页三笔.md` §七·2 记过（WM2 复核），**只是那条断言当时没跟着改**。
                // 周常**不在** `Special Missions` 子树里（`_s = 1`）⇒ **不乘 1.15**，就是 [15, 50]。
                CheckFontWindow(wkR37(), "Step Text", 15f, 50f,
                                "★ 周常格里的数字：自适应窗口 = 原版 `[15,50]`（这一卡无 `localScale`；"
                                + "判据 = `Weekly Mission Milestone T1/holder/text` = 运行期那一份）");
                // 格距：原版 `steps` 的 `EverguildLayoutGroup` **spacing 是「按格数填满容器」配的**
                // （4 格那份 = 262.81，且 `4×70 + 3×262.81 = 1068.43` 恰好 = 容器宽）；
                // 线上 6 格 ⇒ `(1068.43 − 6×70) ÷ 5 = 129.686` ⇒ 相邻格心 **199.686px**、
                // 首尾格心 **998.43px**（期望值是这两个字面量，不是从实现里读的）。
                // ⚠️ 这一条**同时**是「6 格真的排进容器里了」的判据：若沿用 4 格的 262.81，
                //    首尾距离会变成 5×332.81 = 1664px ⇒ 立刻红。
                if (wkCells.Count == 6)
                {
                    CheckNear(Mathf.Abs(wkCells[1].position.x - wkCells[0].position.x) * 108f, 199.686f, 2f,
                              "★ 周常相邻两格的中心距(px)（= (70 + 129.686)，6 格填满 1068.43 的容器）");
                    CheckNear(Mathf.Abs(wkCells[5].position.x - wkCells[0].position.x) * 108f, 998.43f, 3f,
                              "★ 周常首尾两格的中心距(px)（= 5 × 199.686；沿用 4 格的 262.81 会变 1664 ⇒ 红）");
                }
                // 数字框的位置：原版那 6 个数**印在宝箱【下方】**（实拍上就是），不是格心 ——
                // 判据 = `…/holder/text` 的框 **142.95×56**、**框底 = 格底 + 29.53**
                // ⇒ 框心比格心低 `70/2 + 29.53 − 56/2 = **36.53**` px；横向与格心对齐。
                if (wkCells.Count == 6 && wkNums.Count == 6)
                {
                    CheckNear((wkCells[0].position.y - wkNums[0].position.y) * 108f, 36.53f, 1.5f,
                              "★ 周常第 1 格的数字**落在格子下方**(px)（框心比格心低 36.53 = 35 + 29.53 − 28；"
                              + "照每日卡那一支画成同心 ⇒ 红）");
                    CheckNear(Mathf.Abs(wkNums[0].position.x - wkCells[0].position.x) * 108f, 0f, 1f,
                              "★ ...横向与格心对齐(px)（框 142.95 宽、以格心为中心）");
                }

                // ---------------------------------------------------------------- ③ 计数 → 亮几格（两态 ×4）
                // `WeeklyStepDone` 是**唯一**决定格子亮不亮的地方；期望值全是字面量、且**两两配对**，
                // 把「第 1 档 = 5」「第 6 档 = 30」钉在**行为**上（不只是钉数据）。
                System.Func<int> wkOn = () => allNamed(wkR37(), "Milestone_on").Count;
                DailyData.ForceWeeklyProgressForTest(4);
                mt37.Build();
                Check(wkOn(), 0, "★ 周常进度 = **4** ⇒ **一格都没亮**（第 1 档阈值是 **5**）");
                DailyData.ForceWeeklyProgressForTest(5);
                mt37.Build();
                Check(wkOn(), 1, "★ 进度 = **5** ⇒ **第 1 档亮**（与上一条合起来才分得清「按阈值算」与「恒灭」）");
                DailyData.ForceWeeklyProgressForTest(29);
                mt37.Build();
                Check(wkOn(), 5, "★ 进度 = **29** ⇒ 前五档亮、**第 6 档灭**（⇒ 第 6 档那个阈值不是别的档的）");
                DailyData.ForceWeeklyProgressForTest(30);
                mt37.Build();
                Check(wkOn(), 6, "★ 进度 = **30** ⇒ **六档全亮**（与上一条一起把「第 6 档 = 30」钉在行为上）");
                // ④ 分母 = 30（计数字符串的后半段）—— 不写死分子（分子是我们挑的 mock），
                //    只钉「终值来自实拍那个 30」。改回 15 ⇒ 这条红。
                var wkCntTxt = TextOf(FindChild(wkR37(), "counter"));
                CheckTrue(wkCntTxt != null && wkCntTxt.EndsWith("/30"),
                          "★ 周常 `counter` 的分母 = **30**（实拍右上写 `25/30`；A372 之前是 `/15`）"
                          + $"（实读 = `{wkCntTxt ?? "<节点不在>"}`）");
                // 数据面（与 ② 的视觉面是**两条独立的尺子**）：格数 = 阈值数组长度，终值 = 最后一档
                Check(DailyData.WeeklyStepCount, 6, "★ `WeeklyStepCount = 6`（格数与阈值数组**同一条口径**）");

                // ---------------------------------------------------------------- 还原到本节起点
                DailyData.ForceWeeklyProgressForTest(wkBase37);
                mt37.Build();
                Check(DailyData.WeeklyProgressValue(), wkBase37, "（还原）周常进度回到本节起点那个值");
            }
        }

        // ============================================================ 🆕 A389（2026-10-13）
        // 两张卡的里程碑格里「画什么」——期望值**全部是原版字段 / 反编译的读数**，⛔ 不从 `MissionsTab` 取：
        //   · **每日那一格**（真包 `GameObject/Mission Milestones Step (1).json` 那棵树，
        //     MB `-5679983552473563956` 实读）：`actionOnActive = 5` = `ChangeOutline|DisplayCheckMark`
        //     （`OnReach` 枚举 `1/2/4`，`dump.cs:62132-62140`）⇒ **不含 `ChangeSprite`** ⇒
        //     `holder/Image` 出厂就是 `m_Sprite = 0` · `m_Color = (0,0,0,1)` = **一块纯黑**；
        //     描边色由 `MissionMilestoneStep__Setup.c` 运行时写 = 达成 `activeColor (0.3312554,1,0)` /
        //     未达成 `disabledColor (0.9176471,0.7686275,0.4823530)`；
        //     `activeCheckmark = 40K_settings_icon_checkmark` 而 `disabledCheckmark = 0`
        //     ⇒ 基础版那句 `SetActive(sprite != null)` = **未达成时那一件根本不显示**。
        //     `holder/CheckMark` 的框 = `40 × 42.3202`、中心在格心 **+21(右) / −27.3(上)**
        //     （RT 原文 `anchors (0,0)-(1,1)` · `sizeDelta (1.526e-05, 2.32018)` ·
        //      `anchoredPosition (21, 27.3)`）；这一支整棵子树带 `localScale 1.15` ⇒ 期望值各 ×1.15。
        //   · **周常那一格**（`Weekly Mission Milestone T1..T5` 各一份 MB）：`holder/CheckMark` 画**宝箱**，
        //     档位 = `MissionMilestonesDisplay.stepPrefab[Math.Min(i, 4)]`（`MissionMilestonesDisplay__Setup.c`
        //     那三句现读）；真包三份 `Mission Milestones Progress`（MB `-2694294769260735734` /
        //     `-533312439237404929` / `-7530505938951294279`）的 `stepPrefab` **同值** =
        //     `[T1_Iron, T2_Copper, T3_Silver, T4_Gold, T5_Warp]` ⇒ **6 格时第 6 格被夹回 T5**。
        //     达成 → `_open` 图、未达成 → 闭合图；**已达成但不是最后一档**还额外灰
        //     （`WeeklyMissionMilestone__DisplayCheckmark.c`：`value < lastReached` → `SetInteractable(false)`
        //      + `activeCheckmark`；`== lastReached` → 可交互 + `activeCheckmark`；`>` → `disabledCheckmark`）；
        //     灰 = 材质换 `Everguild/UI/Greyscale`（`colorTintGreyOnDisable = 1`）**且**乘 `m_DisabledColor`。
        // **改坏法**（四条，各红在不同的一组上）：
        //   ① 每日那一支退回「画 `40k_missions_milestone_on/off` 圆点」⇒ ① 那一组红（`Box`/`Outline` 计数 0）；
        //   ② 周常档位不夹（或写成 `CrateTiers[i % 5]`）⇒ ③ 的第 6 格那条红；
        //   ③ 灰化解掉（`GreyMilestone` 不调）⇒ ④ 的「灰 = 5」那条红；
        //   ④ `current` 传错（恒 `done` / 恒 `false`）⇒ ④ 的「第 6 格不灰」「④·b 前 4 灰后 2 不灰」**分别**红。
        Section("🆕 A389 里程碑格的图：每日 = 黑方块 + 米黄描边 + 勾 · 周常 = 宝箱（档位 / 开闭 / 灰）");
        {
            var mt389 = tab.GetComponent<MissionsTab>();
            CheckTrue(mt389 != null, "（前提）`Missions Tab` 上挂着 `MissionsTab`");
            if (mt389 != null)
            {
                // 按名收子树（**每次现取** —— `Build()` 会重建整棵子树，旧引用是已销毁对象）
                System.Func<Transform, string, List<Transform>> named389 = (root, nm) =>
                {
                    var l = new List<Transform>();
                    if (root == null) return l;
                    foreach (var tr in root.GetComponentsInChildren<Transform>(true)) if (tr.name == nm) l.Add(tr);
                    return l;
                };
                System.Func<Transform, string, int> prefix389 = (root, pre) =>
                {
                    int cnt = 0;
                    if (root == null) return cnt;
                    foreach (var tr in root.GetComponentsInChildren<Transform>(true)) if (tr.name.StartsWith(pre)) cnt++;
                    return cnt;
                };
                System.Func<Transform, ImageQuad> quad389 = t =>
                    t != null ? t.GetComponentInChildren<ImageQuad>() : null;
                System.Func<Transform, string> shader389 = t =>
                {
                    var q = quad389(t);
                    var mr = q != null ? q.GetComponent<MeshRenderer>() : null;
                    return mr != null && mr.sharedMaterial != null && mr.sharedMaterial.shader != null
                           ? mr.sharedMaterial.shader.name : null;
                };
                // 原版那张灰化 shader 的**内部名**（字面量，⛔ 不读 `WindowButton.GrayShaderName` —— 那就成自证了）
                const string GREY389 = "Everguild/UI/Greyscale";
                var tier389 = new[] { "40k_Crate_Tier1_Iron", "40k_Crate_Tier2_Copper",
                                      "40k_Crate_Tier3_Silver", "40k_Crate_Tier4_Gold", "40k_Crate_Tier5_Warp" };
                System.Func<List<Transform>> wkCells389 = () =>
                {
                    var l = new List<Transform>();
                    var root = FindChild(tab, "Weekly Mission");
                    if (root == null) return l;
                    foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                        if (tr.name == "Milestone_on" || tr.name == "Milestone_off") l.Add(tr);
                    return l;
                };

                int skBase389 = DailyData.SkullsCountValue();
                int wkBase389 = DailyData.WeeklyProgressValue();

                // ---------------------------------------------------------- ① 每日：未达成态
                DailyData.ForceSkullsCountForTest(2);
                mt389.Build();
                var skA = FindChild(tab, "Daily Skulls Mission Container");
                var boxA = named389(skA, "Box");
                Check(boxA.Count, 5, "★ 骷髅卡 5 格各有一块**黑底方框**（原版 `holder/Image`：无 sprite、"
                                     + "靠 `m_Color` 画一块纯色矩形；A389 之前我们画的是圆点图 ⇒ 这条红）");
                Check(prefix389(skA, "Outline "), 20,
                      "★ ...每格 **4 条描边**（UGUI `Outline` = 四份**单轴**偏移副本 ⇒ 5 格 × 4 = 20；"
                      + "A389 之前一条都没有 ⇒ 这条红）");
                var b0 = quad389(boxA.Count > 0 ? boxA[0] : null);
                CheckTrue(b0 != null && b0.Tint.r < 0.01f && b0.Tint.g < 0.01f && b0.Tint.b < 0.01f
                          && b0.Tint.a > 0.99f,
                          "★ 方框是**纯黑**（原版 `m_Color = (0,0,0,1)`）"
                          + $"（实读 = {(b0 == null ? "<没有 quad>" : b0.Tint.ToString())}）");
                var otA = named389(skA, "Outline Top");
                var otA0 = quad389(otA.Count > 0 ? otA[0] : null);
                CheckTrue(otA0 != null
                          && Mathf.Abs(otA0.Tint.r - 0.9176471f) < 0.01f
                          && Mathf.Abs(otA0.Tint.g - 0.7686275f) < 0.01f
                          && Mathf.Abs(otA0.Tint.b - 0.4823530f) < 0.01f,
                          "★ 未达成时描边 = **米黄**（原版 `disabledColor = (0.9176471,0.7686275,0.4823530)`；"
                          + "实拍上就是那圈米黄细边）"
                          + $"（实读 = {(otA0 == null ? "<没有 quad>" : otA0.Tint.ToString())}）");
                Check(named389(skA, "CheckMark").Count, 0,
                      "★ 计数 = **2**（第 1 档阈值 3）⇒ **一个勾都没有**"
                      + "（原版 `disabledCheckmark = 0` ⇒ `SetActive(sprite != null)` = 未达成整件不显示）");
                // 描边那四条**在方框之外**（不是压在方框上）—— 量渲出来的矩形差
                if (boxA.Count > 0 && otA.Count > 0)
                {
                    var qb = quad389(boxA[0]); var qt = quad389(otA[0]);
                    var ltA = named389(skA, "Outline Left");
                    var ql = quad389(ltA.Count > 0 ? ltA[0] : null);
                    if (qb != null && qt != null && ql != null)
                    {
                        // 方框 40×1.15 = 46²；上面那条描边 46 宽 × 2.3 高；左边那条 2.3 宽 × 46 高
                        CheckNear(qt.WorldW * 108f, 46f, 1f,
                                  "★ `Outline Top` 的**宽** = 格宽 46（原版那四条副本是整块矩形沿单轴偏 ⇒ 横条与格等宽）");
                        CheckNear(qt.WorldH * 108f, 2.3f, 0.5f,
                                  "★ `Outline Top` 的**高** = 2 × 1.15 = 2.3（`m_EffectDistance = (2,-2)` 那 2px）");
                        CheckNear(ql.WorldW * 108f, 2.3f, 0.5f,
                                  "★ `Outline Left` 的**宽** = 2.3（竖条那一份；两条一起才分得清「横竖都照 `(2,-2)`」"
                                  + "与「只画了上下两条」）");
                        CheckNear((qb.WorldH - qt.WorldH) * 108f, 46f - 2.3f, 0.5f,
                                  "★ ...而且它**在方框外**（方框高 − 描边高 = 43.7 = 46 − 2.3；"
                                  + "⛔ 不是往框里缩的 stroke —— 那会把方框画小 2.3px）");
                    }
                }

                // ---------------------------------------------------------- ② 每日：达成态（第 1 格）
                DailyData.ForceSkullsCountForTest(3);
                mt389.Build();
                var skB = FindChild(tab, "Daily Skulls Mission Container");
                var ckB = named389(skB, "CheckMark");
                Check(ckB.Count, 1,
                      "★ 计数 = **3** ⇒ **第 1 格出了勾**（与上一条合起来才分得清「按阈值算」与「恒显 / 恒隐」）");
                Check(ckB.Count > 0 ? ArtOf(ckB[0]) : null, "40K_settings_icon_checkmark",
                      "★ ...那个勾的图 = 原版 `activeCheckmark`（pid 索引解出的名字）");
                var otB0 = quad389(named389(skB, "Outline Top").Count > 0
                                   ? named389(skB, "Outline Top")[0] : null);
                CheckTrue(otB0 != null
                          && Mathf.Abs(otB0.Tint.r - 0.3312554f) < 0.01f
                          && Mathf.Abs(otB0.Tint.g - 1f) < 0.01f
                          && Mathf.Abs(otB0.Tint.b - 0f) < 0.01f,
                          "★ ...而且**那一格**的描边从米黄变成**绿**（原版 `activeColor = (0.3312554,1,0)`）"
                          + $"（实读 = {(otB0 == null ? "<没有 quad>" : otB0.Tint.ToString())}）");
                var onB = named389(skB, "Milestone_on");
                if (onB.Count == 1 && ckB.Count == 1)
                {
                    CheckNear((ckB[0].position.x - onB[0].position.x) * 108f, 24.15f, 1.5f,
                              "★ 勾的中心比格心**右偏 24.15px**（原版 `anchoredPosition.x = 21` × 子树 `localScale 1.15`）");
                    // ⚠️ 世界 y 是**向上为正**（同 A371 那条「数字在格子下方 ⇒ `cell.y − num.y > 0`」的口径）
                    //    ⇒ 原版 `anchoredPosition.y = 27.3`（“上”27.3）= 勾心比格心**高** ⇒ 勾.y − 格.y = +31.395
                    CheckNear((ckB[0].position.y - onB[0].position.y) * 108f, 31.395f, 1.5f,
                              "★ ...**上偏 31.395px**（原版 `anchoredPosition.y = 27.3` × 1.15；"
                              + "把它画成「与格心重合的小贴纸」⇒ 这里变 0 ⇒ 红）");
                }
                // 层序：原版 `holder` 的兄弟序是 `Image → CheckMark → text`（`m_Children` 原文）
                // ⇒ 勾在方框**之上**、在数字**之下**。我们只靠渲染队列分层（⛔ 不能靠 z —— 见 `ImageQuad.SetRenderQueue`）。
                // 🔴 **2026-10-14（#52 · D1013 §三·32）就地订正（铁律 5）**：`qbx` 原来取的是上面那个
                //   **`boxA`**（`:2422` 从 `skA` 抓的）—— 而 `:2469` 那次 `mt389.Build()` 会把
                //   `MissionsTab._root` 的子件**整棵 `DestroySafe`**（`Shell/MissionsTab.cs:248-252`；
                //   编辑器非播放态走 `DestroyImmediate`，`Shell/MenuWindowBase.cs:170-176`）
                //   ⇒ `boxA[0]` 是**已销毁对象**、`quad389` 照例回 `null` ⇒ `qbx == null` ⇒ 这条 ★ **恒红**
                //   （而描边/勾的队列本来就是 `3009` 方框 < `3010` 勾 ⇒ 实现没错，见 `Shell/MissionsTab.cs:1237/1244/1254`）。
                //   ⇒ 与同段其它取件同规矩（`:2377` 那句注释「每次现取 —— `Build()` 会重建整棵子树，
                //   旧引用是已销毁对象」）：**从新容器 `skB` 现取**。⛔ 别再改回共用 `boxA`。
                {
                    var qck = quad389(ckB.Count > 0 ? ckB[0] : null);
                    var bxB = named389(skB, "Box");
                    var qbx = quad389(bxB.Count > 0 ? bxB[0] : null);
                    var lbTx = FindChild(skB, "Step Text");
                    var lq = lbTx != null ? lbTx.GetComponentInChildren<Label>() : null;
                    CheckTrue(qck != null && qbx != null && qck.RenderQueue > qbx.RenderQueue,
                              "★ 勾画在**方框之上**（同队列会按「到相机的距离」排 ⇒ 静默盖错）");
                    CheckTrue(qck != null && lq != null && qck.RenderQueue < lq.RenderQueue,
                              "★ ...而且画在**数字之下**（原版 `holder` 的兄弟序 `Image → CheckMark → text`；"
                              + "画反了数字会被那块 40×30.9 的勾压掉右半边）");
                }

                // ---------------------------------------------------------- ③ 周常：档位（未达成 = 闭合图）
                DailyData.ForceWeeklyProgressForTest(4);
                mt389.Build();
                var wkC = wkCells389();
                Check(wkC.Count, 6, "（前提）周常 6 格在");
                for (int i = 0; i < wkC.Count && i < 6; i++)
                {
                    string want = tier389[Mathf.Min(i, tier389.Length - 1)];      // `stepPrefab[Min(i,4)]`
                    var ck = FindChild(wkC[i], "CheckMark");
                    Check(ck != null ? ArtOf(ck) : null, want,
                          "★ 周常第 " + (i + 1) + " 格的宝箱档位 = **`" + want + "`**"
                          + (i == 5 ? "（**第 6 格 = T5**：原版 `Math.Min(下标, 长度−1)` 夹住 —— "
                                     + "⛔ 不是「第 6 档」也不是别的档）" : "") + "（进度 4 ⇒ 全**闭合**）");
                    Check(FindChild(wkC[i], "Dot") != null ? ArtOf(FindChild(wkC[i], "Dot")) : null,
                          "40k_missions_milestone_off",
                          "★ ...它底下那颗圆点也在（原版 `holder/Image` 出厂 `m_Enabled = 1`、画 `…_off`；"
                          + "⛔ 旧记录写「原版把 Image 关掉了」是**把 `Outline` 组件的 `m_Enabled` 看成了它的**）");
                }
                if (wkC.Count > 0)
                {
                    var cq0 = quad389(FindChild(wkC[0], "CheckMark"));
                    if (cq0 != null)
                    {
                        // 框 142.95 × 129.0763 里放 512² 的方图 ⇒ `PreserveAspect` 按高内接 ⇒ 画出来是正方形
                        CheckNear(cq0.WorldH * 108f, 129.0763f, 0.6f,
                                  "★ 宝箱的框高 = **129.0763**（原版 `holder/CheckMark` 的 `sizeDelta.y 59.0763` "
                                  + "加在 70² 的格上）");
                        CheckNear(cq0.WorldW * 108f, 129.0763f, 0.6f,
                                  "★ ...宽 = 高（512² 的图 `PreserveAspect` 内接进 142.95×129.0763 ⇒ 129.0763²）");
                        CheckNear((cq0.WorldW - cq0.WorldH) * 108f, 0f, 0.01f,
                                  "★ 画出来是**正方形**（改坏法：把 `keepAspect` 去掉 ⇒ 142.95×129.08 被拉宽 ⇒ 红）");
                    }
                    // 层序：原版 `holder` 的兄弟序是 `Image → CheckMark → text` ⇒ 宝箱在圆点**之上**
                    var qckW = quad389(FindChild(wkC[0], "CheckMark"));
                    var qdotW = quad389(FindChild(wkC[0], "Dot"));
                    CheckTrue(qckW != null && qdotW != null && qckW.RenderQueue > qdotW.RenderQueue,
                              "★ 宝箱画在圆点**之上**（同队列会按「到相机的距离」排 ⇒ 静默盖错）");
                    // 进度条那两张九宫格也要**低于**宝箱 —— 原版 `progress` 的子节点序
                    // `Mission Progress Bar`(N=3) → `Mission Milestones Progress`(N=4)，实拍上宝箱把金色条压住
                    var qbarW = quad389(FindChild(FindChild(tab, "Weekly Mission"), "Nine"));
                    CheckTrue(qbarW != null && qckW != null && qckW.RenderQueue > qbarW.RenderQueue,
                              "★ 宝箱也画在**进度条之上**（实拍上那 6 个宝箱把金色进度条压住 ⇒ "
                              + "条画在宝箱上面会看到金条穿胸而过 ⇒ 红）");
                }

                // ---------------------------------------------------------- ④ 周常：灰化（三档进度各一种形状）
                // ④·a 进度 5：只有第 1 格达成、且它就是最后一档 ⇒ **一格都不灰、6 格全不灰**
                DailyData.ForceWeeklyProgressForTest(5);
                mt389.Build();
                var wkD = wkCells389();
                System.Func<List<Transform>, int> greyCnt389 = cells =>
                {
                    int n = 0;
                    for (int i = 0; i < cells.Count; i++)
                        if (shader389(FindChild(cells[i], "CheckMark")) == GREY389) n++;
                    return n;
                };
                Check(greyCnt389(wkD), 0,
                      "★ 进度 = **5** ⇒ 第 1 格刚达成（= `lastReachedMilestone`）⇒ **一格都不灰**"
                      + "（与 ④·b 的「4 格灰」合起来才分得清「按达成灰」与「按进度灰」）");
                // ④·b 进度 29：前 4 格「已达成但非最后一档」⇒ 灰；第 5 格是最后一档 ⇒ 不灰；第 6 格未达成 ⇒ 不灰
                DailyData.ForceWeeklyProgressForTest(29);
                mt389.Build();
                var wkE = wkCells389();
                Check(wkE.Count, 6, "（前提）周常 6 格在（④·b）");
                Check(greyCnt389(wkE), 4,
                      "★ 进度 = **29** ⇒ **前 4 格灰**（已达成但都不是最后一档）、第 5 格不灰、第 6 格不灰"
                      + "（原版 `value < lastReachedMilestone` → `SetInteractable(false)`；改坏法："
                      + "把 `current` 传成 `done`（谁都不灰）/ 恒 `false`（前 5 都灰）⇒ 这条分别变 0 / 5）");
                for (int i = 0; i < wkE.Count && i < 6; i++)
                {
                    var ck = FindChild(wkE[i], "CheckMark");
                    Check(ck != null ? ArtOf(ck) : null,
                          tier389[Mathf.Min(i, tier389.Length - 1)] + (i < 5 ? "_open" : ""),
                          "★ ...第 " + (i + 1) + " 格的宝箱图 = " + (i < 5 ? "**开启**态" : "**闭合**态")
                          + "（原版：`≤ lastReached` → `activeCheckmark`（`_open`），`>` → `disabledCheckmark`）");
                }
                // 第 6 格（未达成）**不许**灰 —— 它走的是「可交互 + 闭合图」那一支
                if (wkE.Count == 6)
                    CheckTrue(shader389(FindChild(wkE[5], "CheckMark")) != GREY389,
                              "★ 第 6 格（进度 29 时**还没到**）**不许灰**（原版那一支 `SetInteractable(1)`）");
                // 灰化那一半的 tint 也要对（原版 UGUI 基类 ColorTint 乘 `m_DisabledColor = 0.7843137`）
                var gq = quad389(FindChild(wkE.Count > 0 ? wkE[0] : null, "CheckMark"));
                CheckTrue(gq != null && Mathf.Abs(gq.Tint.r - 0.7843137f) < 0.01f
                          && Mathf.Abs(gq.Tint.g - 0.7843137f) < 0.01f,
                          "★ 灰格还乘了 `m_DisabledColor = (0.7843137,0.7843137,0.7843137,1)`"
                          + "（`m_Transition = 1` ⇒ UGUI 基类那条 ColorTint；实读 = "
                          + $"{(gq == null ? "<没有 quad>" : gq.Tint.ToString())}）");
                // ④·c 进度 30：全达成 ⇒ 最后一格（第 6 格）是 `lastReached` ⇒ **它**不灰、前 5 灰
                DailyData.ForceWeeklyProgressForTest(30);
                mt389.Build();
                var wkF = wkCells389();
                Check(greyCnt389(wkF), 5,
                      "★ 进度 = **30** ⇒ 6 格全达成、**前 5 格灰**、第 6 格不灰（它才是 `lastReachedMilestone`）"
                      + "（这条把「第 6 格 = T5」与「最后达成的是第 6 格」两件事同时钉住）");
                if (wkF.Count == 6)
                    CheckTrue(shader389(FindChild(wkF[5], "CheckMark")) != GREY389,
                              "★ ...第 6 格此刻**不灰**（判据同 ④·b；只断前 5 灰的话「全灰」也能过 ⇒ 两半都要）");

                // ---------------------------------------------------------- 还原到本节起点
                DailyData.ForceSkullsCountForTest(skBase389);
                DailyData.ForceWeeklyProgressForTest(wkBase389);
                mt389.Build();
                Check(DailyData.SkullsCountValue(), skBase389, "（还原）骷髅计数回到本节起点");
                Check(DailyData.WeeklyProgressValue(), wkBase389, "（还原）周常进度回到本节起点");
            }
        }

        Section("页签切换（§二·3：**只切 activeSelf，不重建**）");
        Check(win.CurrentTab, WindowTabType.Missions, "开窗默认在 Missions 页");
        CheckTrue(tab != null && tab.gameObject.activeSelf, "`Missions Tab` 是开着的");
        var forge = FindChild(FindChild(area, "Tabs"), "Forge Tab");
        var camp = FindChild(FindChild(area, "Tabs"), "Campaign Tab");
        CheckTrue(forge != null && !forge.gameObject.activeSelf, "`Forge Tab` 出厂关着（原版 §二·4 `activeSelf=false`）");
        CheckTrue(camp != null && !camp.gameObject.activeSelf, "`Campaign Tab` 出厂关着");
        if (win.tabButtons != null)
        {
            win.tabButtons.Click(1);                                  // 左栏第 2 键 = Campaign
            Check(win.CurrentTab, WindowTabType.Campaign, "点左栏第 2 键切到 Campaign 页");
            CheckTrue(camp != null && camp.gameObject.activeSelf && !tab.gameObject.activeSelf,
                      "切页后 `Campaign Tab` 开、`Missions Tab` 关");
            win.tabButtons.Click(0);
            Check(win.CurrentTab, WindowTabType.Missions, "点回第 1 键切回 Missions 页");
            // 🔴 **2026-10-16（A815）就地改口径（铁律 5）**：这两行原来断的是
            //    「`Click(3)`（= 当时的第 4 键 `Booster Packs`）**不切页**（它不是页签）」—— **照原版写的、当时是对的**。
            //    现在第 4 格 = **我们自建的「每日连胜」页**（用户 2026-10-15 拍板）⇒ **点它要切页**；
            //    而**母版**（`Booster Packs`，顺位到第 5 项）那条「不切页」的语义**照旧成立**，改由下面第二段守着。
            win.tabButtons.Click(3);                                  // 第 4 键 = 每日连胜（自建）
            Check(win.CurrentTab, WindowTabType.DailyStreak, "★ A815：点左栏第 4 键切到 **`DailyStreak`** 页（自建页签）");
            CheckTrue(FindChild(FindChild(area, "Tabs"), "Daily Streak Tab") != null
                      && FindChild(FindChild(area, "Tabs"), "Daily Streak Tab").gameObject.activeSelf,
                      "★ …而且 `Daily Streak Tab` **真的开着**（只切枚举不算 —— 那条 `ChangeTab` 是唯一入口）");
            win.tabButtons.Click(4);                                  // 第 5 键 = 母版（Booster Packs），不是页签
            Check(win.CurrentTab, WindowTabType.DailyStreak,
                  "点第 5 键（母版 `Booster Packs`）**不切页**（它不是页签；照原版 `TabButtons.Initialize` 关着）");
            win.tabButtons.Click(0);                                  // 切回默认页，别把后面几节带偏
            Check(win.CurrentTab, WindowTabType.Missions, "（收尾）切回 Missions 页，后面各节的起手态不变");
        }

        // ============================================================ §二·b 🆕 A815「每日连胜」页（**自建**）
        // 🔴 **口径**：原版奖励窗**没有**这一页（`tabs` 只有 Missions/Forge/Campaign；「每日连胜」在原版是
        //    **另一扇全屏事件窗** `DailyStreakWindow : LiveOpsEventWindow<MainMenuMission>`，数据在远端 CCD）
        //    ⇒ 这一整件是**自建**（**用户 2026-10-15 拍板**）：只显示**本地累计的连续登录天数**，
        //    不做奖励、不接 LiveOps、不连服务器。判据 → `资料/普查产出_1015/R5_A815_A817_A819判据.md` §一。
        // ⚠️ 本节两类期望值分得很清：**页/键的几何**是我们挑的（逐条标「我们挑的」）；**天数**那几条**不是常量**
        //    —— 它们是「摆一个态 → 走**生产入口** → 看读数」，所以能咬住「写死一个数」那种假实现（`5`）。
        Section("🆕 A815 `Daily Streak Tab`（**自建页**）：天数来自**本地记录**（跨天 +1 / 断天归 1 / 落盘）");
        {
            var dsTabs = FindChild(area, "Tabs");
            var dsPage = FindChild(dsTabs, "Daily Streak Tab");
            CheckTrue(dsPage != null, "★ `Daily Streak Tab` 建了（原版**没有**这一页 —— 自建，用户 2026-10-15 拍板）");
            CheckAt(dsPage, 330.69f, 1920f, 70.94f, 1080f,
                    "`Daily Streak Tab` 页矩形（照同族 `Campaign Tab` 那一格；⚠️ **我们挑的** —— 原版无此页）");
            var dst = dsPage != null ? dsPage.GetComponent<DailyStreakTab>() : null;
            CheckTrue(dst != null, "★ 页上挂的是 `DailyStreakTab`（不是空页桩）");
            // ⚠️ **夹具前提**：本节几处要点左栏 ⇒ 先断 `tabButtons` 在（它 null 时直点会**抛 NRE**，
            //    而本文件开头那条注释写着：一条 NRE 会把**它后面每一条断言**一起吞掉 ⇒ 先挡、再点）。
            bool dsBtnOk = win.tabButtons != null;
            CheckTrue(dsBtnOk, "（前提）`win.tabButtons` 取得到（下面那两拍要点它）");

            // ---- 快照（本节收尾**必须**还原：本仓纪律「造出来的态不许留给后面的节 / 别的自检」）----
            int snapDays = DailyData.StreakLoggedDays;
            var snapLast = DailyData.StreakLastDayForTest();
            var snapNext = DailyData.NextResetAt;
            var today = System.DateTime.Now.Date;

            // ---- ① 天数 = 本地记录（**不是写死的 5**）----
            // 🔴 **造态一律「先摆过去、再走生产入口」**：⛔ 不直接写读数（那断的是「我写的那一句」，
            //    不是「跨天 +1」这条规则）。生产入口 = **`TryDailyReset()`** —— 它就是
            //    `MainMenuRuntime.Build()`（`Shell/MainMenuRuntime.cs:254` = **进壳那一拍**）调的那一句。
            // ⚠️ 每次先把「下一次刷新时刻」摆到明天 ⇒ 它**返回 `false`**（同一天不重置）而**连登那一拍照记**
            //    —— 这一条同时钉住「连登挂在早退之前」这个位置（挂错了下面几条就红）。
            DailyData.ForceNextResetForTest(today.AddDays(1));
            DailyData.ForceStreakForTest(6, today.AddDays(-1));            // 记到**昨天**、连了 6 天
            bool rCross = DailyData.TryDailyReset();
            Check(rCross, false, "（前提）同一天进壳：`TryDailyReset()` **不重置**（返回 false）");
            Check(DailyData.StreakLoggedDays, 7, "★ **跨天 +1**：记到昨天、连 6 天 ⇒ 今天进壳后 **7**（生产入口那一拍）");

            if (dsBtnOk) { win.tabButtons.Click(0); win.tabButtons.Click(3); }   // 离开再切回 ⇒ `OnOpen` 重读一次记录
            CheckTrue(dst != null && dst.DisplayedValue == "7",
                      "★ …而**页面上画出来的数就是它**（`OnOpen` 刷新 ⇒ 显示 `7`；"
                      + "把天数写死的实现会给 `5` —— 这条就是「天数来自本地记录」的判别式）");

            DailyData.ForceNextResetForTest(today.AddDays(1));
            DailyData.ForceStreakForTest(9, today.AddDays(-3));            // 记到**三天前**（中间断了两天）
            bool rBreak = DailyData.TryDailyReset();
            Check(rBreak, false, "（前提）同上：这一次也不重置");
            Check(DailyData.StreakLoggedDays, 1, "★ **断天归 1**：隔了 ≥ 2 天没登 ⇒ 今天进壳后回到 **1**");

            DailyData.ForceNextResetForTest(today.AddDays(1));
            DailyData.ForceStreakForTest(6, today);                        // 今天已经记过
            DailyData.TryDailyReset();
            Check(DailyData.StreakLoggedDays, 6, "★ **同一天幂等**：今天已经记过 ⇒ 再进几次壳都还是 **6**（不 +1）");

            DailyData.ForceNextResetForTest(today.AddDays(1));
            DailyData.ForceStreakForTest(0, default(System.DateTime));     // 「本机从没记过」
            DailyData.TryDailyReset();
            Check(DailyData.StreakLoggedDays, 1, "★ **从没记过 ⇒ 1**（不是 0、也不是 5）");

            // ---- ② 落盘 / 重启后还在（**读盘**那一口 = 灭自证）----
            // 🔴 本宿主此前**完全不碰盘**（`grep OverrideDir` 零命中）⇒ 这一段自带注入与还原，
            //    用的是 A429 那套现成的门（`OverrideDir` + `DeleteStoreForTest` / `SaveNowForTest` / `ReloadForTest`），
            //    ⛔ **不另发明一套**；目录与 `Editor/MainMenuScene.cs:8578` 那份**分开**（两宿主互不干扰）。
            DailyData.OverrideDir = "d:/4/_tmp_view/daily_selftest_rewards";
            DailyData.DeleteStoreForTest();
            DailyData.ReloadForTest();                                     // 「第一次跑、盘上什么都没有」
            Check(DailyData.StreakLoggedDays, 0, "（前提）盘上没有档 + 内存打回出厂 ⇒ 连登 **0**");

            DailyData.ForceStreakForTest(6, today.AddDays(-1));
            DailyData.SaveNowForTest();                                    // ⚠️ `Force*ForTest` **自己不落盘**
            Check(DailyData.StoredStreakDaysForTest(), 6,
                  "★ **天数真的写到盘上了**（从**盘上**读回 6 —— 删掉 `RecordLogin` 末尾那句 `Persist(...)` ⇒ 这条红；"
                  + "只看内存的断言验不出那个改坏法）");
            DailyData.ReloadForTest();                                     // = 模拟一次进程重启（内存全丢 + 只剩盘上那一份）
            Check(DailyData.StreakLoggedDays, 6, "★ **重启后连登数还在**（`ReloadForTest` 从盘上读回 6）");

            // ---- ③ 老档（A815 之前落的盘）里没有这一格 ⇒ 按「从没记过」走，**不许整份作废** ----
            // 🔴 这一条钉住 `StoreDto.version` **不 +1** 那个决定（判据全文在 `StoreDto.streakDays` 的注释）：
            //    旧档必须**照读**（别的字段都在），只是连登那一格回到 0（= 它的确定含义）。
            File.WriteAllText(DailyData.StorePath,
                "{\"version\":1,\"nextSet\":false,\"nextResetTicks\":0,\"rows\":null,"
                + "\"skullsCount\":3,\"skullsState\":0,\"loginState\":0}");
            DailyData.ReloadForTest();
            Check(DailyData.StreakLoggedDays, 0,
                  "老档（没有 `streakDays` 这个键）⇒ 连登读 **0** = 「本机从没记过」（⛔ 不是垃圾值、也不是硬塞一个默认数）");
            Check(DailyData.SkullsCountValue(), 3,
                  "★ …而**同一份老档的别的字段照读**（骷髅 = 3）—— 两句合起来才证「它是被**读进来**了，"
                  + "不是被当成『版本不认识』整份丢掉」（把 `version` 抬到 2 就会是这一条红）");

            // ---- 收尾：门关上 + 不留本节造出来的那些数 ----
            DailyData.OverrideDir = null;                                  // ⚠️ 关掉门（后面的节不再碰盘）
            DailyData.ReloadForTest();                                     // 门关着 ⇒ 只把内存打回出厂
            DailyData.ForceNextResetForTest(snapNext);                     // 回本节起点的「下一次刷新时刻」
            // 连登那一格：本节起点本来是 **0/从没记过**（`RewardsScene` 全程不调 `TryDailyReset`，已 `grep` 核过）；
            // 收尾按**生产入口**把它摆成「今天记过一次」（= 真机进壳一次之后的态，**1 天**）——
            // 这样下面那张 `01b_日常_每日连胜.png` 拍到的是一个**真机上会出现**的画面（既不是 0，也不是我们编的 demo 数）。
            // ⚠️ 它**不碰盘**（门已经关了），也不会影响别的节（全仓只有这一页读它）。
            DailyData.ForceStreakForTest(snapDays, snapLast);
            DailyData.RecordLoginForTest(System.DateTime.Now);             // 幂等：起点若已记过今天 ⇒ 什么都不做
            Check(DailyData.StreakLoggedDays, snapDays == 0 ? 1 : snapDays,
                  "（还原）连登那一格**不留本节造出来的 6/7/9**（没记过 ⇒ 今天记一次 = **1 天**）");
            if (dsBtnOk) win.tabButtons.Click(0);                          // 页签也切回默认页
        }

        // ============================================================ §三·b 锻造厂页
        // 🔴 **每一条的期望值都来自「原版参数」，不是我们写的常量**（否则就是自证）——
        //    出处 = `资料/阶段二_锻造厂与战役页_原版规格.md` §一/§二（原版 JSON 走链算出），
        //    矩形链由 `工具/menu_rect.py` 复算（**独立于 C# 的第二份实现**：`Core/UguiRect.cs`）。
        Section("§三·b `Forge Tab`（锻造厂，阶段二第 3 层第 1 件）：层 × 参数逐条对");
        // 🆕 **2026-10-11（A267）：进本节先把锻造页【切过去】，再量。**
        //   🔴 **原来这一整片（本节 + §三·b2「找茬」+ ③④ 那几段，约 500 行）全跑在【未激活】的
        //   `Forge Tab` 上** —— 页签要等到下面「指针层」那一段（本轮之前是 `:2082`）才第一次 `Click(2)`。
        //   **量矩形不受影响**（`Transform.Find` / `GetComponentsInChildren(true)` 都找得到关着的子树），
        //   但**任何依赖「真渲染」的东西在关着的页上必然假绿**：
        //     · TMP 在未激活对象上 `ForceMeshUpdate` 出不来网格 ⇒ `textBounds` 是垃圾/旧值
        //       （`资料/已知的坑.md`「面板画出来了、字不在」坑①）；
        //     · `MenuDraw.ClipText` 那条路在未激活的页上只能数进 `TextClipUploadSkipped`
        //       （见 `MenuDraw.ClipTmpMesh` 的注释）⇒ **在这一段里验「字被裁 / 被削 alpha」拿到的是「没生效」**
        //       —— A260 那一幕就是这么来的（`资料/普查产出_1008/X_RewardsScene崩溃修.md` §七·2）。
        //   ⚠️ **这一句不改变画面 / 行为**：`ForgeTab.OnOpen` 的三件事（`Refresh` / `FocusSelectedArmy` /
        //     `FocusClaimable`）在 `Shell/ForgeTab.cs:367-371`（`Build()` 末尾）已经各做过一次，
        //     而那两条 `FocusOn` 都是**绝对**定位（`MenuScroll.SetOffset`：偏移没变就早退）
        //     ⇒ 再切一次，两条滚动区的偏移**逐字相同**；下面那几处 `Click(2)`（`:2082` / `:2224` 那一带）
        //     从此变成**幂等**（照旧调，不改）。
        //   **改坏法**：删掉下面这两句（或把它们挪回 §三·b2 之后）⇒ 紧跟着那条 ★ 立刻红。
        //   ⚠️ 切页那句**带 `forge != null` 的闸**：页不存在时（夹具坏）别去切 ——
        //     `ChangeTab` 会把**所有页**（2026-10-16 起 4 页）全关掉，那会把后面几十条断言一起带进沟里（红要红在本节这一条上）。
        if (forge != null && win.tabButtons != null) win.tabButtons.Click(2);
        Check(win.CurrentTab, WindowTabType.Forge,
              "★ （夹具前提）进本节时页签**真的切到 `Forge`** 了 —— 本节整段都在这页上量");
        CheckTrue(forge != null && forge.gameObject.activeInHierarchy,
                  "★ （夹具前提）`Forge Tab` **此刻是活的**（`activeInHierarchy`）—— 本节的断言依赖真渲染"
                  + "（未激活的页上 TMP 出不来网格 / `ClipText` 只能数进 `TextClipUploadSkipped` ⇒ 会**假绿**）");
        if (forge != null)
        {
            // 页矩形：`Forge Tab` 实测 x 330.69..1920.00 · y 71.12..1079.82
            CheckAt(forge, 330.69f, 1920f, 71.12f, 1079.82f, "`Forge Tab` 页矩形");
            var fbg = FindChild(forge, "Background");
            CheckTrue(fbg != null, "`Background` 建了（原版 `Image(sprite=空)` + 色 **#000000** ⇒ 纯黑满铺）");
            var fwarp = FindChild(fbg, "Warp");
            CheckAt(fwarp, 741.85f, 1508.85f, 88.47f, 1062.47f, "`Background/Warp`（旋涡大图）");
            CheckRectPx(fwarp, 741.85f, 1508.85f, 88.47f, 1062.47f, "`Warp` 渲染尺寸（**767×974，与原图同尺寸 ⇒ 不拉伸**）");
            CheckArt(fwarp, "40K_ArmyTrack_bg", "旋涡大图的图");
            // 🔴 **同队列的两层「谁盖谁不可控」**（2026-09-23 实测：旋涡整张被 `Background` 的黑板盖掉，
            //    只看得到几缕紫雾 —— 而**所有断言都是绿的**）⇒ 钉一条「Warp 的队列必须大于黑板」。
            var fblackQ = fbg != null ? fbg.GetComponentInChildren<ImageQuad>() : null;
            var fwarpQ = fwarp != null ? fwarp.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(fblackQ != null && fwarpQ != null && fwarpQ.RenderQueue > fblackQ.RenderQueue,
                      "`Warp` 的**渲染队列 > 黑板**的（同队列里 Unity 按到相机的距离排，谁盖谁不可控）");

            // 奖励轨道：`Rewards Scroll View` x 330.97..1919.73 · y 318.60..1080
            var ftrack = FindChild(forge, "Rewards Scroll View");
            CheckAt(ftrack, 330.97f, 1919.73f, 318.60f, 1080f, "`Rewards Scroll View`");
            var fcontent = FindChild(FindChild(ftrack, "Viewport"), "Rewards Content");
            // 🔴 2026-09-23 起**轨道会滚** ⇒ 只建**视口里放得下的那些格**（原版 `RectMask2D` 裁掉的不建）。
            //    「一共 50 格」由 `ForgeData.MaxLevel` + 「滚到最右能建出第 50 格」两条一起管（见 §三·b2）。
            CheckTrue(fcontent != null && fcontent.childCount > 0 && fcontent.childCount < ForgeData.MaxLevel,
                      $"`Rewards Content` 下建的是**视口里放得下的那些格**（实测 {fcontent.childCount} / 共 "
                      + $"{ForgeData.MaxLevel} 格；⚠️ **格数是我们挑的**，原版在服务端 —— 见 `ForgeData.MaxLevel` 的注释）");

            // 阵营选择条：`Forge Army Selector` x 588.17..1662.53 · y 71.57..196.67
            var fsel = FindChild(forge, "Forge Army Selector");
            CheckAt(fsel, 588.17f, 1662.53f, 71.57f, 196.67f, "`Forge Army Selector`");
            var fsep = FindChild(fsel, "Separator Line");
            CheckAt(fsep, 491.43f, 1759.27f, 191.38f, 197.38f, "`Separator Line`");
            CheckArt(fsep, "40k_main_line_purple", "分隔线的图");
            var fArmy = FindChild(FindChild(fsel, "Viewport"), "Army Content");
            CheckTrue(fArmy != null && fArmy.childCount > 0 && fArmy.childCount <= ForgeData.Armies.Length,
                      $"`Army Content` 下建的是**视口里放得下的那些条目**（实测 {fArmy.childCount} / 共 "
                      + $"{ForgeData.Armies.Length} 个阵营；13 个阵营照用户边界「全解锁」）");

            // 选中信息：`Selected Army Info` x 619.40..1240.69 · y 195.76..318.48
            var finfo = FindChild(forge, "Selected Army Info");
            CheckAt(finfo, 619.40f, 1240.69f, 195.76f, 318.48f, "`Selected Army Info`");
            CheckAt(FindChild(finfo, "ArmyText"), 762.91f, 1083.82f, 209.36f, 259.36f, "`ArmyText`");
            CheckAt(FindChild(finfo, "LevelText"), 766.10f, 1097.11f, 252.76f, 302.76f, "`LevelText`");
            var ficon = FindChild(finfo, "Army Icon");
            CheckAt(ficon, 621.76f, 747.04f, 188.38f, 313.66f, "`Army Icon`");
            CheckArt(ficon, DeckRuntime.FactionIcon(ForgeData.Selected),
                     "阵营徽记（**走 `DeckRuntime.FactionIcon`，全工程唯一一份映射**）");

            CheckAt(FindChild(forge, "Help Icon"), 1685.21f, 1737.40f, 215.90f, 268.08f, "`Help Icon`");

            // ---- 纪律①：出厂 inactive 的**一律不建**（后两个另有 `Awake` 无条件关它们的硬证据）
            CheckTrue(FindChild(finfo, "Xp Points Icon") == null,
                      "`Xp Points Icon` **不建**（出厂 inactive；反编译实证全代码无人点亮它）");
            CheckTrue(FindChild(forge, "Debug Add points") == null,
                      "`Debug Add points` **不建**（`ForgeWindowTab.Awake` 无条件 `SetActive(false)`）");
            CheckTrue(FindChild(forge, "Debug Set Forge") == null, "`Debug Set Forge` **不建**（同上）");
            // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来挂着「`War ParticleSystemUI` **本轮不建**」
            //    （0917 判决：先做静态版 + 空态，粒子后补）—— **那条已经过期了**：A12-P1 正是把三个宿主
            //    建了出来（`ForgeTab.Build` 的 `BuildParticleHosts`；见那个方法头的日志）。旧断言断的是旧行为。
            //    ⇒ 新判据 = **宿主该在 + 层级/名字逐字对 + 子树里一个 `ParticleSystem` 都没有**（粒子本身仍没建），
            //      写在 §三·b3-a（`Background/War ParticleSystemUI` 三级全路径 + `Particle System nebula`
            //      没有 `RectTransform` + 三处身位 + 空态那一条）。**这里不再抄第二份**（同一条判据两处写 = 迟早不一致）。
            //    ⚠️ 别把它挪回上面那张「出厂 inactive ⇒ 不建」的清单里：它现在是**要建的**。

            // ---- 纪律③：两根石柱的**缩放必须烘进子件矩形**（右柱是镜像）
            var fdecor = FindChild(forge, "Background Elements");
            var fcl = FindChild(fdecor, "Column Left");
            var fcr = FindChild(fdecor, "Column Right");
            CheckTrue(fcl != null && fcr != null, "`Column Left` / `Column Right` 都建了");
            var fclt = FindChild(fcl, "Culumn Top");
            var fcrt = FindChild(fcr, "Culumn Top");
            CheckRectPx(fclt, 0f, 319f * 1.04f, 0f, 460f * 1.04f,
                        "左柱 `Culumn Top`（原版 319×460 × `localScale 1.04` ⇒ **实绘 331.76×478.4**）");
            CheckRectPx(fcrt, 0f, 319f * 1.04f, 0f, 460f * 1.04f, "右柱 `Culumn Top`（同上，镜像只是位置翻）");
            CheckTrue(fclt != null && fcrt != null && fclt.position.x < fcrt.position.x,
                      "右柱在左柱右边（`Column Right` 的锚点是 (1,1) ⇒ 它的设计矩形在 x=1920 起）");
            CheckArt(fclt, "40k_rewards_forge_decoration_Column_top", "石柱顶的图");

            // ---- 纪律④：`Ready for level up` 建了，且**开关 = `hasToCollectReward`**（原版判据）
            var fready = FindChild(forge, "Ready for level up");
            CheckTrue(fready != null, "`Ready for level up` **建了**（原版 prefab 里它是激活的）");
            // 🆕 **2026-10-12（A440）**：这一句的**文案**原来写「色 `#FF2DDF`」——
            //   那是**按十六进制手写**的（`(1, 45/255, 223/255)`），**原版序列化值不是它**。
            //   真值 = `Shell/ForgeTab.cs` 那颗 `GlowColor` 现读的 `(1, 0.1745283, 0.8761433, 1)`
            //   （H11 已就地订正那个常量；⚠️ **只改文案** —— 判定仍只比图名，改前改后都不会变色）。
            CheckArt(FindChild(fready, "Glow"), "Glow_UI_W40K",
                     "可领光效的图（色 = 原版序列化值 `(1, 0.1745283, 0.8761433, 1)`；原文写 `#FF2DDF`，已订正）");
            CheckTrue(fready != null && fready.gameObject.activeSelf == ForgeData.HasToCollect(ForgeData.Selected),
                      "可领光效的开关 = `hasToCollectReward`（原版 `ForgeRewardSelector.CreateRewards` 的判据）");

            // ---- 纪律⑤：等级圆牌**只有两张 sprite**（不是三张）
            var cell0 = fcontent != null && fcontent.childCount > 0 ? fcontent.GetChild(0) : null;
            var cellClaim = fcontent != null ? FindChild(fcontent, "ForgeCell_" + ForgeData.LevelOf(ForgeData.Selected)) : null;
            if (cell0 != null)
                CheckArt(FindChild(cell0, "LevelBg"), "40k_ArmyTrack_milestone_on",
                         "第 1 格圆牌 = `_on`（已领 ⇒ `Collected` 用 onSprite）");
            if (cellClaim != null)
            {
                CheckArt(FindChild(cellClaim, "LevelBg"), "40k_ArmyTrack_milestone_on", "可领格的圆牌 = `_on`");
                CheckTrue(FindChild(cellClaim, "Generic UI Button") != null,
                          "**可领格**才有 `Generic UI Button`（Claim）—— 照原版「只有 ToCollect 才开 claimButton」");
            }

            // ---- `SelectArmy`：换阵营要**真的换数据**（原版 `ForgeWindowTab.SelectArmy` 写三处 + 重建轨道）
            var ft = forge.GetComponent<ForgeTab>();
            CheckTrue(ft != null, "`Forge Tab` 上挂的是 `ForgeTab`（**不再是空页桩**）");
            if (ft != null)
            {
                ft.SelectArmy("SaimHann");
                Check(ForgeData.Selected, "SaimHann", "`SelectArmy` 把当前阵营换成了 SaimHann");
                var fat = FindChild(finfo, "ArmyText");
                var flt = FindChild(finfo, "LevelText");
                var fatL = fat != null ? fat.GetComponentInChildren<Label>() : null;
                var fltL = flt != null ? flt.GetComponentInChildren<Label>() : null;
                Check(fatL != null ? fatL.Text : null, "SaimHann",
                      "换阵营后 `ArmyText` 写成了新阵营名（原版 `ForgeSelectedArmyInfo.Initialize`）");
                Check(fltL != null ? fltL.Text : null, "Level " + ForgeData.LevelOf("SaimHann") + "/" + ForgeData.MaxLevel,
                      "`LevelText` = `\"Level {已领}/{格数}\"`（原版 `\"{0} {1}/{2}\"` 套本地化键 `\"MainMenu/Level\"`）");
                var rdy = ft.transform.Find("Ready for level up");
                CheckTrue(rdy != null && !rdy.gameObject.activeSelf,
                          "SaimHann 差 40 点 ⇒ **可领光效灭**（`hasToCollectReward = false`）");
                ft.SelectArmy("Goff");        // 换回可领态 —— 后面两张截图要用
            }

            // ============================================================ §三·b2 锻造厂页「找茬」
            // 🔴 **2026-09-23 找茬族**（`项目任务.md` §三 第 15 条第 18 行点名：锻造厂页只跑过它自己那 46 条
            //    断言，**没有**像商店 / 战役奖励窗那样把截图逐件核过一遍）。
            //    判据只有一条：**量「渲出来」的矩形** —— 「件在不在」那一类断言**验不出重叠 / 出屏 / 不齐**
            //    （`资料/已知的坑.md`「为什么照解包资料摆还会摆错」的 **C 类成因**）。
            //    期望值全部来自**原版参数**（正本 §一/§二/§五/§六 的 JSON 原文），不是我们自己的常量。
            Section("§三·b2 锻造厂页「找茬」：量【渲出来】的矩形（不越界 / 不重叠 / 同中心）");

            // ---- ① 阵营条：原版 `Army Content` = 「选择条正中心的一个零宽点」+ `ContentSizeFitter`
            //      ⇒ 13 个条目**以中心对称展开**（左对齐那一版最后两个阵营出屏、点不到 —— 本轮查出的真错）
            if (fArmy == null) CheckTrue(false, "`Army Content` 不在（下面那一族找茬没法量）");
            else
            {
                CheckNear(PxOf(fArmy.position.x), 1125.35f, 1f,
                          "`Army Content` 的中心 x = **选择条中心 1125.35**（原版锚点是「中心零宽点」⇒ 对称展开）");
                int n = fArmy.childCount;
                // 🔴 **2026-09-23 起阵营条会滚**（`MenuScroll`）：视口外的条目不建（= 原版 `RectMask2D` 裁掉的），
                //    开局又照 `FocusOnArmy` 把选中的对到视口中心 ⇒ **不能再拿「第 1 个在 391.19」这种内容坐标
                //    去量屏幕位置**（那是偏移 0 时的值，本轮断言被自检自己抓出来过）。改成量这三件事：
                var ftArmy = forge.GetComponent<ForgeTab>();
                CheckTrue(ftArmy != null && ftArmy.ArmyScroll != null, "阵营条的滚动区建了（`MenuScroll`）");
                CheckTrue(n >= 8, $"`Army Content` 下建了 **{n} 条**（≈ 视口里放得下的那些；越界的不建）");
                var selNow = FindChild(fArmy, "ForgeArmyItem_1");
                // ⚠️ **靠边的阵营对不到正中心**：`FocusOn` 会被滚动极值夹住（第 1/2 个最多只能到 −265.16），
                //    这是**对的** —— 原版 `ScrollRect` 一样夹。所以这里断「**完整落在视口里**」而不是「在中心」。
                float sx1, sy1, sx2, sy2;
                CheckTrue(selNow != null && RectOf(FindChild(selNow, "Icon"), out sx1, out sy1, out sx2, out sy2)
                          && sx1 >= 588.17f && sx2 <= 1662.53f,
                          "**选中的那个阵营完整落在选择条视口里**（照原版 `ArmySelector.FocusOnArmy` 的意图；"
                          + "⚠️ 靠边的阵营对不到正中心 —— 被滚动极值夹住，原版 `ScrollRect` 同样如此）");
                if (ftArmy != null && ftArmy.ArmyScroll != null)
                {
                    var sc = ftArmy.ArmyScroll;
                    // 滚动两端：**最边上那一条都要能完整带进视口**（= 原版靠滚动做到的事）
                    float fx1, fy1, fx2, fy2;
                    sc.ScrollBy(sc.MinOffset - sc.Offset);
                    var first = FindChild(fArmy, "ForgeArmyItem_0");
                    CheckTrue(first != null && RectOf(FindChild(first, "Icon"), out fx1, out fy1, out fx2, out fy2)
                              && fx1 >= 588.17f && fx2 <= 1662.53f,
                              "**滚到最左：第 1 个阵营的图标完整落在选择条视口里**"
                              + "（原来它整条躲在左柱的绘制矩形后面 —— 第 21 条那个缺口）");
                    sc.ScrollBy(sc.MaxOffset - sc.Offset);
                    int lastIdx = ForgeData.Armies.Length - 1;
                    var last = FindChild(fArmy, "ForgeArmyItem_" + lastIdx);
                    CheckTrue(last != null && RectOf(FindChild(last, "Icon"), out fx1, out fy1, out fx2, out fy2)
                              && fx1 >= 588.17f && fx2 <= 1662.53f,
                              $"**滚到最右：第 {ForgeData.Armies.Length} 个阵营的图标完整落在选择条视口里**"
                              + "（原来它落在 2056、整条出屏）");
                    CheckNear(sc.MaxOffset, 265.16f, 1f,
                              "内容居中 ⇒ **两侧都能滚**：右极值 = 内容右边(1927.69) − 视口右边(1662.53) = 265.16px");
                    CheckNear(sc.MinOffset, -265.16f, 1f, "左极值 = 内容左边(323.01) − 视口左边(588.17) = −265.16px");
                    ftArmy.FocusSelectedArmy();       // 还原成开局定位（后面两张截图要用）
                }

                // ---- 🆕 **2026-10-04：阵营条的软边（原版 `RectMask2D.m_Softness = (42,0)`）** ----
                //   判据 = `d:/4/_tmp_view/q1_rm2d.txt` 的三条路径（`Forge Tab/Forge Army Selector/Viewport`
                //   :9-10 · `Rewards Base Submenu Variant/…/Forge Army Selector/Viewport` :111-112 ·
                //   `Forge Army Selector/Viewport` :247-248），**值都是 (42,0)** —— 只渐变 x、y 是硬边。
                //   两条切线都由**原版值 42 + 视口矩形（原版实测 588.17..1662.53）**算出来：
                //     左 `588.17 + 42 = 630.17` · 右 `1662.53 − 42 = 1620.53`。
                //   时机：上面刚 `FocusSelectedArmy()` ⇒ 偏移被夹在 `MinOffset = −265.16`（见上一条断言）
                //   ⇒ 第 1 个阵营压左沿、第 9 个（`ForgeArmyItem_8`）压右沿，两个图标各被切开。
                //   🔴 **改坏实现会真红**：删掉 `ForgeTab.BuildArmyItems` 里那两行 `ClipSoftness`
                //   （或把 42 改成 25 / 把两轴写反）⇒ 切线为空 / 位置不对 ⇒ 下面立刻红。
                {
                    var FsVp_ = new PxRect(588.17f, 71.57f, 1662.53f, 196.67f);   // `Forge Army Selector/Viewport` = `_selR`（本文件 `:1757` 断的就是这四个数）
                    var vCuts = ScanSoftCuts(fArmy, true, FsVp_);
                    CheckTrue(vCuts.Count > 0,
                              "阵营条里**有层被软边切开**（实测竖切线 "
                              + (vCuts.Count > 0 ? string.Join("、", vCuts.ConvertAll(v => v.ToString("F2")).ToArray())
                                                 : "一条都没有")
                              + "）—— **一条都没有 = 这条软边没接上**（`ClipSoftness` 留在 0）");
                    int leftCut = 0;
                    for (int i = 0; i < vCuts.Count; i++)
                    {
                        bool nearLeft = Mathf.Abs(vCuts[i] - 630.17f) <= 0.6f;
                        bool nearRight = Mathf.Abs(vCuts[i] - 1620.53f) <= 0.6f;
                        CheckTrue(nearLeft || nearRight,
                                  $"竖切线 #{i + 1} 在 {vCuts[i]:F2} ⇒ 必须是**带的内沿**：左 `588.17 + 42 = 630.17`"
                                  + " 或右 `1662.53 − 42 = 1620.53`（原版 `m_Softness.x = 42`；"
                                  + "写成 `(0,42)` 会切横线、写成 25 会切在 613.17/1637.53）");
                        if (nearLeft) leftCut++;
                    }
                    CheckTrue(leftCut > 0,
                              "**左边那条**切线确实出现（开局偏移被夹在 −265.16 ⇒ 第 1 个阵营必然压着左沿）");
                    var hCuts = ScanSoftCuts(fArmy, false, FsVp_);
                    Check(hCuts.Count, 0,
                          "**一条横切线都没有** —— 这一处的软边是 `(42,0)`：y 方向是**硬边**"
                          + "（`(0,42)` 那种写反的实现这里会冒出一堆横切线）");
                }

                int iconOverlap = 0; float prevX2 = float.NaN;
                for (int i = 0; i < n; i++)
                {
                    float ix1, iy1, ix2, iy2;
                    if (!RectOf(FindChild(fArmy.GetChild(i), "Icon"), out ix1, out iy1, out ix2, out iy2)) continue;
                    if (!float.IsNaN(prevX2) && ix1 < prevX2 - 0.5f) iconOverlap++;
                    prevX2 = ix2;
                }
                Check(iconOverlap, 0, "相邻阵营**图标**的渲染矩形互不重叠（原版条目 136.36 宽 · 间距 −14 "
                      + $"⇒ 图标之间还有 ~20px 缝；实测重叠 {iconOverlap} 对）");
                // ✅ 2026-09-23 **缺口已修**：这条以前钉的是「4 个落在左右柱的绘制矩形里、够不着」——
                //    现在两条滚动区能把**两端**都带进视口 ⇒ 期望值翻成 **0**
                //    （照 §一 那条纪律：钉缺口的断言修好之后要翻过来，别留着旧期望值）。
                //    ⚠️ 柱子仍会盖住**恰好落在它那一条里**的图标 —— 但那是**原版也一样的**（兄弟序如此）。
                int covered = 0, iconUnmeasured = 0; string iconBad = "";
                for (int i = 0; i < n; i++)
                {
                    float ix1, iy1, ix2, iy2;
                    // 🔴 2026-10-04：用**并集**（`RectOfUnion`）—— 软边会把这颗图标沿渐隐带**切开**，
                    //    `RectOf`（取第一块）会量到半块 ⇒ 误判成「完全落在柱子里」（首跑就是这么红的）。
                    if (!RectOfUnion(FindChild(fArmy.GetChild(i), "Icon"), out ix1, out iy1, out ix2, out iy2))
                    { iconUnmeasured++; if (iconBad.Length < 60) iconBad += i + ","; continue; }
                    if (ix2 <= 662f || ix1 >= 1588f) covered++;    // 整个图标都在某根柱子的绘制矩形里
                }
                // 🔴 **2026-10-14（A797′ · WA750 §5·3）**：这里原来只有那句 `continue` —— 「**量不到**」与
                //   「**没被盖住**」于是**同形**：`covered` 是**计数**、下面那条判据是 `covered == 0`
                //   ⇒ 一条都量不到时它照样印 `0`（看着像「全都验过了」）。⇒ 把两种状态**拆开**，
                //   量不到的**单独计数 + 显式红**（同 WA750 §2·6「量不到 ⇒ 显式红」；⛔ 不是删断言/放宽容差）。
                //   ⚠️ 期望 `0` 的前提**本件成立**：`n = fArmy.childCount`（`:2813`），而 `Army Content` 的每个子件
                //      都是 `ForgeArmyItem_*`、都带 `Icon`（`Shell/ForgeTab.cs:706` 每建一格必 `"Icon"`；
                //      上面 `:2824/:2835/:2842` 三条已在量同一种 `Icon`）。
                Check(iconUnmeasured, 0,
                      $"（前提）{n} 条阵营图标的渲染矩形**全都量得到**（量不到 {iconUnmeasured} 条"
                      + (iconBad.Length > 0 ? $"：下标 {iconBad}" : "") + "）"
                      + " —— 量不到若只 `continue` 不计数，下面那条会在「一条都没量到」时照样印 0 = 假绿");
                Check(covered, 0, $"落在**左右装饰柱矩形之内**的阵营条目数 = **0**（实测 {covered}）"
                      + " —— 缺口已修：`MenuScroll` 能把两端带进视口（原版靠 `ScrollRect` 做同一件事）");
                Check(covered, 0, $"落在**左右装饰柱矩形之内**的阵营条目数 = **0**（实测 {covered}）"
                      + " —— 缺口已修：`MenuScroll` 能把两端带进视口（原版靠 `ScrollRect` 做同一件事）");

                // 选中格：`HighlightBG` 与它那一格**同中心**（原版 `pos = (0,0)`）+ 箭头在框内
                var selItem = FindChild(fArmy, "ForgeArmyItem_1");        // Goff = 第 2 个（此刻选中的就是它）
                var hb = FindChild(selItem, "HighlightBG");
                CheckTrue(selItem != null && hb != null, "选中的那一格上有 `HighlightBG`（选中 = 多显一层）");
                if (selItem != null && hb != null)
                {
                    CheckNear(PxOf(hb.position.x), PxOf(selItem.position.x), 0.5f,
                              "`HighlightBG` 与它那一格**同中心 x**（原版 `pos = (0,0)`）");
                    CheckNear(PxYOf(hb.position.y), PxYOf(selItem.position.y), 0.5f, "同上，y 也对齐");
                    float hx1, hy1, hx2, hy2, ax1, ay1, ax2, ay2;
                    if (RectOf(hb, out hx1, out hy1, out hx2, out hy2))
                    {
                        CheckNear(hx2 - hx1, 114.36f, 1f,
                                  $"`HighlightBG` 的渲染宽 = **114.36**（`Forge Army Item Button` **变体**的值；"
                                  + $"母版才是 136 —— 别套错实例；实测 {hx2 - hx1:F2}）");
                        CheckNear(hy2 - hy1, 122f, 1f, "`HighlightBG` 的渲染高 = **122**");
                    }
                    if (RectOf(FindChild(hb, "Arrow"), out ax1, out ay1, out ax2, out ay2)
                        && RectOf(hb, out hx1, out hy1, out hx2, out hy2))
                    {
                        // 变体原文：`Arrow` 锚 `(0.5,0)`（框底中点）+ pos `(0,12.1)` + 尺寸 102.38×30.71
                        // ⇒ 水平中心 = 框中心、竖向中心 = 框底上方 12.1（**会探出底边 ~3px，原版如此**）
                        CheckNear((ax1 + ax2) * 0.5f, (hx1 + hx2) * 0.5f, 0.5f,
                                  "`Arrow` **水平居中**在高亮框上（原版 `pos.x = 0`，挂在框底中点）");
                        // 🔴 **2026-10-03 改**：接上「选择条视口」的裁剪之后，箭头**探出视口底的那一截被真裁掉**
                        //    （原版 `Viewport` 上那层 `RectMask2D` 同样会裁）⇒ 再量「渲出来的中心 = 框底上方
                        //    12.1」量到的其实是**裁过之后**的中心（实测矮了 1.7px）。
                        //    ⇒ 改成量**没被裁的那条边**：上边缘 = 框底上方 12.1 + 半个箭头高（30.71/2）。
                        CheckNear(ay1, hy2 - 12.1f - 30.71f * 0.5f, 0.5f,
                                  "`Arrow` 的**上边缘**在框底上方 12.1 + 半箭头高（原版 `pos.y = 12.1`，尺寸 102.38×30.71）");
                        CheckTrue(ay2 <= hy2 + 3f + 0.5f,
                                  $"`Arrow` 探出框底的那一截**不超过原版的 ~3px**（实测 {ay2 - hy2:F2}px）");
                    }
                }

                // 黑底：原版根上 `Img[sprite=0] col=(0,0,0,1)` ⇒ 不透明纯黑（正本 §二「照画」）
                var selBg = FindChild(fsel, "Black");
                CheckTrue(selBg != null && TintOf(selBg).a > 0.99f && TintOf(selBg).r < 0.01f
                          && TintOf(selBg).g < 0.01f && TintOf(selBg).b < 0.01f,
                          "阵营条的不透明纯黑底**建了**（原版 `Img[sprite=0] col=(0,0,0,1)`；本族找茬查出原来漏画）");
                CheckTrue(selBg != null && Mathf.Abs(Wpx(selBg) - 1074.36f) < 2f,
                          $"黑底的渲染宽 = **1074.36**（= 选择条 588.17..1662.53；实测 {(selBg != null ? Wpx(selBg) : 0f):F1}）");
            }

            // ---- ② 可领格（Goff 第 4 格 = index 3）：Claim 钮与它那一格**同中心**、且不越出格
            {
                // 🔴 2026-09-23 起**轨道会滚**（`MenuScroll`）⇒ 「实建了哪些格」本身就是要验的东西：
                //    视口外的格**根本不建**（= 原版 `RectMask2D` 裁掉的那些），所以**不能**再断「50 格全在」。
                var ft0 = forge.GetComponent<ForgeTab>();
                string built = "";
                if (fcontent != null)
                    foreach (var t in fcontent.GetComponentsInChildren<Transform>(true))
                        if (t.name.StartsWith("ForgeCell_")) built += t.name.Substring(10) + ",";
                Debug.Log(P + $"   · 锻造轨道：偏移 **{(ft0 != null && ft0.TrackScroll != null ? ft0.TrackScroll.Offset : 0f):F1}px**"
                          + $" · 实建格 [{built}] · 阵营条偏移 **{(ft0 != null && ft0.ArmyScroll != null ? ft0.ArmyScroll.Offset : 0f):F1}px**");
                CheckTrue(ft0 != null && ft0.TrackScroll != null && ft0.ArmyScroll != null,
                          "锻造页的两条滚动区都建了（`MenuScroll` —— 全壳唯一一份滚动实现）");
                if (ft0 != null && ft0.TrackScroll != null)
                {
                    // ① **开局定位**：照原版 `ForgeRewardSelector` 的吸附语义，把「该领的那一格」对到视口中心
                    float wantOff = ForgeTab.TabL + ForgeTab.TrackPadL + ForgeData.LevelOf(ForgeData.Selected) * (ForgeTab.CellW + ForgeTab.TrackSpacing) + ForgeTab.CellW * 0.5f - 1125.35f;
                    CheckNear(ft0.TrackScroll.Offset, wantOff, 1f,
                              $"开机对到**该领的那一格**（照原版吸附语义）：偏移 = {wantOff:F1}px（= 第 {ForgeData.LevelOf(ForgeData.Selected) + 1} 格中心 − 视口中心）");
                    // ② **滚到最右** ⇒ 最后一格（level 50）完整落在视口里
                    float saved = ft0.TrackScroll.Offset;
                    ft0.TrackScroll.ScrollBy(ft0.TrackScroll.MaxOffset);
                    var lastC = fcontent != null ? FindChild(fcontent, "ForgeCell_" + (ForgeData.MaxLevel - 1)) : null;
                    float lx1, ly1, lx2, ly2;
                    CheckTrue(lastC != null && RectOf(lastC, out lx1, out ly1, out lx2, out ly2),
                              $"滚到最右后**最后一格（level {ForgeData.MaxLevel}）建出来了** —— 这是「第 5 格以后领不到」那个缺口的判据");
                    // ③ **滚到某一格** ⇒ 那一格的 Claim 钮落在视口里（能点到）
                    ft0.TrackScroll.ScrollBy(-ft0.TrackScroll.MaxOffset);
                    ft0.FocusClaimable();
                    var cellNow = fcontent != null ? FindChild(fcontent, "ForgeCell_" + ForgeData.LevelOf(ForgeData.Selected)) : null;
                    var cbtn = cellNow != null ? FindChild(cellNow, "Generic UI Button") : null;
                    float cx1 = 0f, cy1 = 0f, cx2 = 0f, cy2 = 0f;   // ⚠️ 要给初值：`RectOf` 是在 `&&` 短路里调 out 的
                    CheckTrue(cbtn != null && RectOf(cbtn, out cx1, out cy1, out cx2, out cy2)
                              && cx1 >= 330.97f && cx2 <= 1919.73f,
                              "**该领那一格的 Claim 钮落在视口里**（原版靠滚动做到；我们不再需要「屏幕外也能点」这种假话）");
                    // 🆕 **A48（`RectMask2D.m_Padding` 接线）**：这一格现在正对在视口中心（上面刚 `FocusClaimable`）
                    //   ⇒ 就地量它的 `ClaimHit`。判据 = **原版 mask 的实读字面量**
                    //   （`d:/4/_tmp_view/q1_rm2d.txt:189-190` 与 `:105-106`：`Forge Tab/Rewards Scroll View/Viewport`
                    //   与 `Rewards Base Submenu Variant/…/Forge Tab/…` 两条 = `m_Padding (10,0,0,0)`，UGUI 的 (L,B,R,T)）。
                    //   🔴 **2026-10-08（A188）就地订正（铁律 5）：pad 缩的是【mask 自己那个框】，不是命中区自己那一块** ——
                    //   uGUI 的射线有两关、**都要过**：`GraphicRaycaster.cs:327`（图形自己的 rect + 图形自己的
                    //   `m_RaycastPadding`；本颗按钮实测 = (0,0,0,0)）**∧** `RectMask2D.cs:178-184`
                    //   （**mask 自己那个 `rectTransform`** 缩 `m_Padding`）⇒ **命中区 = `R ∩ (V − pad)`**。
                    //   这一幕：`V − pad` 左沿 = 330.968 + 10 = **340.968**，而按钮居中、`R = 1024.969…1225.731`
                    //   ⇒ **`R ⊂ (V − pad)` ⇒ 命中区 = `R` 本身**（宽 **200.762**、左沿 = **按钮自己的左沿**）。
                    //   本节原来断的「命中宽 = 200.762 − 10 = 190.762、左沿 = `cx1 + 10`」= **我们自己的错模型**
                    //   （它把 pad 缩在命中区自己身上；实测与原版差 10px）。
                    //   🔴 **改坏法**：把 `Shell/MenuDraw.cs` 的 `Hit` 改回 `ClipRect(PaddedHitRect(r, maskPad), clip, …)`
                    //   ⇒ 宽回 **190.762**、左沿回 **`cx1 + 10`** ⇒ 下面那两条立刻红（差 10px，容差 0.5）。
                    //   ⚠️ 「pad 被整个丢掉」（完全不缩）在这一幕**给同一个数** ⇒ 上面这几条**分不出**它，
                    //   能分出来的是本节末尾那条**跨边**断言（pad 那一侧真该生效的那一幕）。
                    {
                        var clHit = cellNow != null ? FindChild(cellNow, "ClaimHit") : null;
                        var clHitQ = clHit != null ? clHit.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(clHitQ != null,
                                  "该领那一格有 `ClaimHit`（只有 `ToCollect` 才建 —— 原版 `claimButton` 同样只在 `ToCollect` 开）");
                        if (clHitQ != null && cbtn != null)
                        {
                            float hx1, hy1, hx2, hy2;
                            CheckTrue(QuadRectOf(clHitQ, out hx1, out hy1, out hx2, out hy2),
                                      "`ClaimHit` 那颗 quad 的渲染矩形量得到");
                            // 🆕 **A77⑬⑧（A48 的独立审查）：先单独钉住【钮本身有多宽】。**
                            //   期望值 = **原版 `sizeDelta.x` 字面量 200.762**（⛔ 不写 `ForgeTab` 里那个实参，
                            //   也⛔ 不写 `MenuDraw` 的实参 —— 拿被测实现当期望就是**自证**：错模型也照样绿）。
                            CheckNear(cx2 - cx1, 200.762f, 0.5f,
                                      "（前置）那一格 `Generic UI Button` **本身就是 200.762 宽**（原版 `sizeDelta.x`）"
                                      + "—— 这条先红 ⇒ 是钮变了，不是 pad 生效错");
                            CheckNear(hx2 - hx1, 200.762f, 0.5f,
                                      "★ 锻造轨道 `ClaimHit` 的**命中宽 = 200.762**（= 原版 `sizeDelta.x`，**一点没缩**）"
                                      + "—— 判据 = `命中区 = R ∩ (V − pad)`，而这一幕按钮**整块落在 `V − pad` 里** ⇒ 不缩"
                                      + "（`RectMask2D.cs:178-184` 缩的是 mask 自己那个 `rectTransform`；`GraphicRaycaster.cs:327`）");
                            CheckNear(hx1, cx1, 0.5f,
                                      "★ …**左边缘 = 按钮自己的左边缘**（同一条判据；⛔ 别写成 `cx1 + 10` —— "
                                      + "那是「pad 缩在命中区自己身上」的错模型）");
                            CheckNear(hy1, cy1, 0.5f, "…上边缘**一动不动**（`m_Padding.w` = Top = 0）");
                            CheckNear(hy2, cy2, 0.5f, "…下边缘**一动不动**（`m_Padding.y` = Bottom = 0）");
                        }
                        // 顺手补上 `PaddedHitDegenerates` 的第一个读者（R-F 审查指出它当时**一个读者都没有**）：
                        //   非 0 = 有处 `m_Padding` 比命中区还大（会算出镜像 quad ⇒ 那颗钮静默点不动）。
                        // 🔴 **2026-10-07（A77⑬⑨）如实记一笔：这一条【今天红不了】—— 它恒真。**
                        //   判据（A48 的独立审查）：全工程**只有一处**给非零 `ClipPad`
                        //   （`Shell/ForgeTab.cs` 的锻造轨道 `(10,0,0,0)`），而那块命中区是 **191×47**
                        //   ⇒ 「pad 比命中区还大」这一支**走不到**（把 `ClipPad` 改成 `(999,0,0,0)` 才会红）。
                        //   ⇒ 它的价值是「**将来**谁接上一个过大的 pad 时当场红」，⛔ 别读成「兜底逻辑已验证」。
                        Check(MenuDraw.PaddedHitDegenerates, 0,
                              "全工程**没有一处** `m_Padding` 大过它的命中区（非 0 会让那颗钮静默点不动）"
                              + "（⚠️ 今天这条恒真 —— 见 `MenuDraw.PaddedHitDegenerates` 的注释）");
                    }
                    // ============================================================ ★ **跨边那一幕**（A188）
                    // 🔴 **为什么还要这一幕**：上面那三条断的是「pad **不该**缩命中区」（这一幕 `R ⊂ V−pad`）——
                    //   它们**分不出**「pad 缩在 `clip` 上（原版）」与「pad 压根没接 / 被丢掉」：
                    //   那两种做法在这一幕给**同一个数**。要分出来，得让 pad **真该生效**：
                    //   把这一格滚到**按钮横跨** `V.x1 + padL = 330.968 + 10 = 340.968` 那条线。
                    //   · 原版（= 现实现）：命中区 = `R ∩ (V − pad)` ⇒ 左沿 = **340.968**（= `max(R.x1, V.x1+padL)`）；
                    //   · 旧模型（pad 缩命中区自己）：`(R − 10) ∩ V` ⇒ 左沿 = **330.968**
                    //     （⚠️ 比**看得见的按钮**还多出 10px —— 点得到一条看不见的边）；pad 被丢掉 ⇒ 同样是 330.968。
                    //   ⇒ 这一条**不可能**被「照实现抄期望值」满足（两种做法在这一幕不是同一个数）。
                    //   ⚠️ 可达性：这一幕要 `lv ≤ 45`（再往右 `MenuScroll.MaxOffset` 会夹住 ⇒ 这一格滚不到视口左沿）。
                    //      夹具今天固定在 level 4（`ForgeData.ResetForTest` ⇒ `Selected = Armies[1]`、level 3）⇒ 够得着；
                    //      若将来把夹具的锻炉等级改到 ≥ 46，下面那条**前提**会红（出声，不是静默跳过）。
                    {
                        int lvX = ForgeData.LevelOf(ForgeData.Selected);
                        // ① 把这一格的**中心**滚到视口左沿 330.968（= 原版 `Viewport` 左沿字面量，见上面那条 `cx1 >= 330.97`）。
                        //    ⚠️ 位置**用格节点自己的中心**换算（`ForgeCell_i` 由 `MenuDraw.Node` 摆 ⇒ 摆在**未裁**的格中心、
                        //    不吃 `Clip`），⛔ 不拿我们自己的常量算位置 —— 那是拿被测实现量它自己。
                        float cxA = cellNow != null ? PxOf(cellNow.position.x) : 0f;
                        CheckTrue(cellNow != null && cxA > 1f,
                                  $"（前提）跨边那一幕：该领那一格的**节点中心**量得到（滚之前实测 {cxA:F1}px）");
                        ft0.TrackScroll.ScrollBy(cxA - 330.968f);
                        // ② 滚完**重取**（`MenuScroll.OnChanged` 会把整条轨道重建 ⇒ 上面那些引用全成野的）
                        var cellX = fcontent != null ? FindChild(fcontent, "ForgeCell_" + lvX) : null;
                        var btnX = cellX != null ? FindChild(cellX, "Generic UI Button") : null;
                        var hitX = cellX != null ? FindChild(cellX, "ClaimHit") : null;
                        var hitXQ = hitX != null ? hitX.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(hitXQ != null && btnX != null,
                                  "（前提）跨边那一幕：按钮与 `ClaimHit` 都还建着（压在视口边上 ⇒ 建、但被截）");
                        if (hitXQ != null && btnX != null)
                        {
                            float nCx = PxOf(cellX.position.x);                                    // 这一格（**未裁**）的中心
                            float eL = nCx - 200.762f * 0.5f, eR = nCx + 200.762f * 0.5f;           // 按钮**自己的**左右沿
                            CheckTrue(eL < 340.968f - 1f && eR > 340.968f + 1f,
                                      $"（前提）按钮**真横跨** `V.x1 + padL = 340.968`：它自己的左右沿 {eL:F1} / {eR:F1}"
                                      + "（左边在线的左侧 ~110px、右边在线的右侧 ~90px ⇒ 这一幕 pad 那一侧**必须生效**）");
                            float bx1, by1, bx2, by2, gx1, gy1, gx2, gy2;
                            CheckTrue(RectOf(btnX, out bx1, out by1, out bx2, out by2),
                                      "（前提）按钮**渲出来**的矩形量得到");
                            // 前置：按钮自己仍是 200.762 宽 —— 用「**量出来的右沿** − 未裁左沿」算
                            //   （右沿离裁切框还远 ⇒ 没被截；左沿被截到 340.968，所以不能拿量出来的左沿直接相减）。
                            CheckNear(bx2 - eL, 200.762f, 0.5f,
                                      "（前置）按钮的矩形**仍是 200.762 宽**（量出来的右沿 − 未裁左沿；原版 `sizeDelta.x`）");
                            CheckTrue(QuadRectOf(hitXQ, out gx1, out gy1, out gx2, out gy2),
                                      "（前提）`ClaimHit` 那颗 quad 的渲染矩形量得到");
                            CheckNear(gx1, 340.968f, 0.5f,
                                      "★ 跨边那一幕：命中区**左沿 = 340.968**（= `V.x1(330.968) + padL(10)`）"
                                      + "—— 判据 = `R ∩ (V − pad)`：`RectMask2D.cs:178-184` 缩的是 **mask 自己那个** `rectTransform`，"
                                      + "`GraphicRaycaster.cs:327` 那一关才是图形自己的 rect");
                            CheckNear(gx1, bx1, 0.5f,
                                      "★ …而且**等于按钮渲出来的左沿**（命中区**不许比看得见的那块更宽/更靠外**）"
                                      + "—— 旧模型在这一幕给 **330.968**（比按钮多出 10px）；pad 被丢掉也给 330.968");
                            CheckNear(gx2, bx2, 0.5f,
                                      "…右沿 = 按钮渲出来的右沿（`pad.z` = Right = 0 ⇒ 右边一动不动）");
                        }
                    }
                    ft0.TrackScroll.ScrollBy(saved - ft0.TrackScroll.Offset);      // 还原，别影响后面的截图
                }

                int claimIdx = ForgeData.LevelOf(ForgeData.Selected);
                var clmCell = fcontent != null ? FindChild(fcontent, "ForgeCell_" + claimIdx) : null;
                var claim = clmCell != null ? FindChild(clmCell, "Generic UI Button") : null;
                CheckTrue(claim != null, "第 4 格（`ToCollect`）上有 `Generic UI Button`（Claim 钮）");
                if (clmCell != null && claim != null)
                {
                    CheckNear(PxOf(claim.position.x), PxOf(clmCell.position.x), 0.5f,
                              "`Claim` 钮与它那一格**同中心 x**（原版 `pos.x = −1.5e−05`）");
                    float bx1, by1, bx2, by2;
                    if (RectOf(claim, out bx1, out by1, out bx2, out by2))
                    {
                        float ccx = PxOf(clmCell.position.x);
                        CheckTrue(bx1 >= ccx - 252.95f - 0.5f && bx2 <= ccx + 252.95f + 0.5f,
                                  $"`Claim` 钮**不越出它那一格**（格宽 505.9 是原版值；钮 x {bx1:F0}..{bx2:F0}）");
                        // ⚠️ **如实记、不当断言**：第 4 格整体落在视口右边缘之外 —— 原版靠 `RectMask2D` 裁掉，
                        //    我们没建遮罩（文件头已出声）。整格一起越出、屏幕右边缘也在 1920 ⇒ 画面上看不到差别。
                        Debug.Log(P + $"   · 第 4 格的 Claim 钮右边缘 {bx2:F1}px（视口右 1919.73 ⇒ 越出 "
                                  + $"{bx2 - 1919.73f:F1}px）—— 原版靠 `RectMask2D` 裁，我们没建遮罩");
                    }
                }
            }

            // ---- ③ `Selected Army Info`：两行字与徽记**互不重叠**、且徽记在本框内
            {
                float ax1, ay1, ax2, ay2, lx1, ly1, lx2, ly2, ix1, iy1, ix2, iy2;
                bool okA = RectOf(FindChild(finfo, "ArmyText"), out ax1, out ay1, out ax2, out ay2);
                bool okL = RectOf(FindChild(finfo, "LevelText"), out lx1, out ly1, out lx2, out ly2);
                bool okI = RectOf(FindChild(finfo, "Army Icon"), out ix1, out iy1, out ix2, out iy2);
                CheckTrue(okA && okL && okI, "`Selected Army Info` 三件的渲染矩形都量得到");
                // 🔴 **这里不能断「两行不重叠」**：原版这两行的**框**本来就叠 6.6px
                //    （`ArmyText` 209.36..259.36 · `LevelText` 252.76..302.76），而 `Label.WorldH` 量的是**框**高
                //    ⇒ 断「不重叠」是**假警报**（本轮先写错、被自检自己抓出来了）。改断「两行中心 = 原版框中心」。
                if (okA)
                    CheckNear((ay1 + ay2) * 0.5f, 234.36f, 2f,
                              "阵营名的渲染中心 y = **234.36**（原版框 209.36..259.36 的中心）");
                if (okL)
                    CheckNear((ly1 + ly2) * 0.5f, 277.76f, 2f,
                              "等级行的渲染中心 y = **277.76**（原版框 252.76..302.76 的中心）");
                if (okA && okI)
                    CheckTrue(!Overlaps(ax1, ay1, ax2, ay2, ix1, iy1, ix2, iy2),
                              $"徽记**不压**阵营名（徽 x {ix1:F0}..{ix2:F0} · 名 x {ax1:F0}..{ax2:F0}）");
                if (okI)
                    CheckTrue(ix1 >= 619.40f - 0.5f && ix2 <= 1240.69f + 0.5f,
                              $"徽记在 `Selected Army Info` 框内（原版框 619.40..1240.69；实测 {ix1:F0}..{ix2:F0}）");
            }

            // ---- ④ 装饰柱：**渲染范围不许越出屏幕左右**（右柱整根是镜像出来的，翻错会飞出去）
            {
                float lx1, ly1, lx2, ly2, rx1, ry1, rx2, ry2;
                bool okL = RectOf(FindChild(fcl, "Culumn Top"), out lx1, out ly1, out lx2, out ly2);
                bool okR = RectOf(FindChild(fcr, "Culumn Top"), out rx1, out ry1, out rx2, out ry2);
                CheckTrue(okL && lx1 >= 0f && lx2 <= 1920f, $"左柱的渲染范围在屏幕内（x {lx1:F0}..{lx2:F0}）");
                CheckTrue(okR && rx1 >= 0f && rx2 <= 1920f, $"右柱的渲染范围在屏幕内（x {rx1:F0}..{rx2:F0}）");
                CheckTrue(okL && okR && lx2 < rx1, $"左右两柱**不重叠**（左到 {lx2:F0} · 右从 {rx1:F0} 起）");
            }

            // ---- ⑤ 层序：页内各层队列**严格递增**（同队列里「谁盖谁」不可控 —— 已踩两次）
            CheckTrue(ForgeTab.QTabBg < ForgeTab.QTabWarp && ForgeTab.QTabWarp < ForgeTab.QSelBg
                      && ForgeTab.QSelBg < ForgeTab.QSelLine && ForgeTab.QSelLine < ForgeTab.QArmyIcon
                      && ForgeTab.QArmyIcon < ForgeTab.QTabDecor && ForgeTab.QTabDecor < ForgeTab.QTabHelp,
                      "页内层序严格递增：黑板 < 旋涡 < **阵营条黑底** < 分隔线 < 阵营层 < 装饰柱 < Help"
                      + $"（{ForgeTab.QTabBg}/{ForgeTab.QTabWarp}/{ForgeTab.QSelBg}/{ForgeTab.QSelLine}/"
                      + $"{ForgeTab.QArmyIcon}/{ForgeTab.QTabDecor}/{ForgeTab.QTabHelp}）");
            // `Help Icon`（x 1685..1737）正落在**右柱的绘制范围**（x 1547..1920）里 ⇒ 只靠队列分层保住它
            CheckTrue(ForgeTab.QTabHelp > ForgeTab.QTabDecor,
                      "`Help Icon` 的队列**高于右柱**（右柱罩到 1685..1737 那一片，同队列就不可控）");

            // ---- ⑥ 指针层（2026-09-23 新加；判据 =「真鼠标点不动」那个缺口，见第 23 条）----
            //    🔴 原来 `WindowButton` 只实现老式 `OnMouseUpAsButton`，而工程既没给 quad 加 collider、
            //       又设成「只用新 Input System」⇒ **一次也不会派发**。
            //       现在收口成 `PointerLayer`（新输入系统轮询 + 自己算命中，照 `DeckRuntime.HandlePointer`）。
            //    ⚠️ 批处理里 `Update` 不跑、也没有输入事件 ⇒ 这里直调 `ClickAt/WheelAt/ButtonAt`，
            //       **和真点真滚走的是同一条路**。
            {
                // 🔴 **先切到锻造页**：`WindowButton` 是 `OnEnable` 登记进 `All` 的，而**非当前页的物件是关着的**
                //    ⇒ 不切过去的话登记表是空的（2026-09-23 自检报「登记表 0 个」，查了半天是这条）。
                if (win.tabButtons != null) win.tabButtons.Click(2);
                var layer = PointerLayer.Instance;
                int btnCount = Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None).Length;
                Debug.Log(P + $"   · 诊断：`PointerLayer.Instance`={(layer != null)}"
                          + $" · 场景里 `WindowButton` {btnCount} 个（指针层是**事件时扫描**，没有登记表）");
                CheckTrue(layer != null, "`PointerLayer` 拿得到（**惰性取用**：没有就现建 —— 不依赖生命周期回调）");
                CheckTrue(btnCount > 0, $"场景里找得到 `WindowButton`（实测 {btnCount} 个）");
                if (layer != null)
                {
                    var ft2 = forge.GetComponent<ForgeTab>();
                    int ci = ForgeData.LevelOf(ForgeData.Selected);
                    var cc = fcontent != null ? FindChild(fcontent, "ForgeCell_" + ci) : null;
                    var cb = cc != null ? FindChild(cc, "Generic UI Button") : null;
                    if (cb != null && ft2 != null)
                    {
                        ft2.FocusClaimable();                       // 把该领那一格对到视口中心
                        cb = FindChild(FindChild(fcontent, "ForgeCell_" + ci), "Generic UI Button");
                        float qx = PxOf(cb.position.x), qy = PxYOf(cb.position.y);
                        var hitBtn = cb != null ? layer.ButtonAt(qx, qy) : null;
                        if (hitBtn == null)
                        {
                            string who = "";
                            foreach (var wb in Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None))
                            {
                                var q2 = wb.GetComponent<ImageQuad>();
                                if (q2 == null) continue;
                                float bx = PxOf(q2.transform.position.x), by = PxYOf(q2.transform.position.y);
                                if (Mathf.Abs(bx - qx) < 300f && Mathf.Abs(by - qy) < 300f)
                                    who += $"[{wb.name} q={q2.RenderQueue} 中心 {bx:F0},{by:F0} 尺寸 {q2.WorldW * 108f:F0}x{q2.WorldH * 108f:F0} 激活 {q2.gameObject.activeInHierarchy}] ";
                            }
                            string all2 = "";
                            foreach (var wb in Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None))
                            {
                                var q3 = wb.GetComponent<ImageQuad>();
                                all2 += q3 == null ? $"[{wb.name} 无quad] " : $"[{wb.name}@{PxOf(q3.transform.position.x):F0},{PxYOf(q3.transform.position.y):F0}] ";
                            }
                            Debug.Log(P + $"   · 命中诊断：点在 {qx:F1},{qy:F1} —— 附近：{(who == "" ? "**一个都没有**" : who)}"
                                      + $"；全场景按钮：{all2}");
                        }
                        CheckTrue(hitBtn != null,
                                  "该领那一格的 `Claim` 钮**在指针层的命中表里**（真鼠标按得下去）"
                                  + " —— 这就是第 23 条那个缺口的判据");
                    }
                    CheckTrue(layer.ButtonAt(5f, 5f) == null, "页面左上角空白处**点不中任何按钮**（不误触）");
                    if (ft2 != null && ft2.TrackScroll != null)
                    {
                        float o0 = ft2.TrackScroll.Offset;
                        CheckTrue(layer.WheelAt(1125f, 700f, -120f), "在锻造轨道**视口里**滚轮被接受");
                        CheckTrue(!Mathf.Approximately(ft2.TrackScroll.Offset, o0), "…而且偏移**真的变了**");
                        CheckTrue(!layer.WheelAt(1125f, 300f, -120f),
                                  "在锻造轨道视口**外**（y=300，两条滚动区都不覆盖）滚轮不生效");
                        ft2.TrackScroll.ScrollBy(o0 - ft2.TrackScroll.Offset);     // 还原
                    }

                    // ---- 🆕 2026-10-03：**悬停 / 拖拽 / 惯性 / 回弹**（§三 第 29 条 A15 + A5）----
                    //   判据全部**照 UGUI 源码**（原版跑的滚动就是它，本地就能读：
                    //   `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs`）：
                    //   `m_DecelerationRate = 0.135` · `m_Elasticity = 0.1` · `RubberDelta` :1084 ·
                    //   拖拽阈值 = `EventSystem.m_DragThreshold = 10`（`EventSystem.cs:68`）；
                    //   悬停色 = 原版 prefab 里 1276 个按钮共用的 `m_Colors.m_HighlightedColor = 0.9607843`。
                    {
                        // ① 悬停（原版 = UGUI `IPointerEnter/Exit`）
                        int ci3 = ForgeData.LevelOf(ForgeData.Selected);
                        var cb3 = fcontent != null
                            ? FindChild(FindChild(fcontent, "ForgeCell_" + ci3), "Generic UI Button") : null;
                        if (cb3 != null)
                        {
                            float hx = PxOf(cb3.position.x), hy = PxYOf(cb3.position.y);
                            var want = layer.ButtonAt(hx, hy);
                            var hb = layer.HoverAt(hx, hy);
                            CheckTrue(hb != null && hb == want && hb.Hovered,
                                      "指针压到按钮上 ⇒ 它进**悬停**态（原版 UGUI `IPointerEnter`）");
                            // 🔴 **2026-10-03 随 A17 就地更正**：原版这一颗（`Forge Menu Reward Button` 的
                            //    `Generic UI Button`）是 **SpriteSwap** —— 悬停**换图**（`40K_button` → `40K_button_hover`），
                            //    而**不是**变暗（UGUI 一颗 `Selectable` 只有一种 transition）。
                            //    原来这条断的是「色偏 = 0.9608」= **断的是旧行为**（那时我们只有色偏兜底）。
                            CheckTrue(hb != null && hb.HoverTexForTest != null && hb.CurrentTexForTest == hb.HoverTexForTest,
                                      "悬停 ⇒ 底图换成原版的高亮图（这一颗是 SpriteSwap 档，不是色偏档）");
                            CheckTrue(hb != null && hb.TintKForTest >= 1f - 0.001f,
                                      "换图档**不叠色偏**（原版一颗只有一种 transition）");
                            CheckTrue(layer.HoveredForTest == hb, "指针层记着这一颗是当前悬停件");
                            layer.HoverAt(5f, 5f);
                            CheckTrue(hb != null && !hb.Hovered, "指针挪到空白 ⇒ 悬停态结束");
                            CheckTrue(hb != null && hb.CurrentTexForTest == hb.NormalTexForTest,
                                      "图**还原**成常态图");
                        }

                        // ② 拖拽阈值 10px + 「拖了就不点按钮」+ 惯性（速度照 UGUI `Lerp(v,newV,dt*10)`）
                        var s = ft2 != null ? ft2.TrackScroll : null;
                        if (s != null)
                        {
                            float o1 = s.Offset;
                            var downBtn = layer.ButtonAt(1125f, 700f);
                            layer.PressAt(1125f, 700f);
                            CheckTrue(downBtn == null || downBtn.Pressed,
                                      "按下时命中的那一颗进 **Pressed** 态（原版 `SelectionState.Pressed`）");
                            CheckTrue(!layer.MoveTo(1131f, 700f),
                                      "按住只挪 **6px** ⇒ **还没到** `m_DragThreshold = 10`（不算拖）");
                            CheckTrue(layer.MoveTo(1160f, 700f),
                                      "挪过 10px ⇒ 转成**拖拽**（原版 `EventSystem.m_DragThreshold = 10`）");
                            CheckTrue(s.Dragging, "滚动区自己知道在被拖");
                            CheckTrue(!Mathf.Approximately(s.Offset, o1), "拖动**真的改了偏移**（内容跟着手走）");
                            layer.TickAt(1f / 60f);                     // 一帧 ⇒ 攒速度
                            layer.MoveTo(1170f, 700f);
                            layer.TickAt(1f / 60f);
                            bool clicked = layer.ReleaseAt(1170f, 700f);
                            CheckTrue(!clicked, "**拖动中松手 ⇒ 不点按钮**（照原版：越过阈值那一下被 ScrollRect 吃掉）");
                            CheckTrue(!s.Dragging, "松手后拖动态结束");
                            CheckTrue(Mathf.Abs(s.Velocity) > 1f,
                                      $"松手时带着速度（惯性）—— 实测 {s.Velocity:F1}px/s");
                            float o2 = s.Offset;
                            layer.TickAt(1f / 60f);
                            CheckTrue(!Mathf.Approximately(s.Offset, o2), "松手后还会**自己走一段**（惯性）");
                            CheckTrue(layer.TickAt(0f) == false, "`dt <= 0` 那一帧不动（照 UGUI 那条守卫）");
                            s.Stop();                                   // 别把速度带到后面的断言里
                            s.SetOffset(o1);
                        }

                        // ③ 回弹（Elastic）—— 原版锻造轨道是 **Clamped** ⇒ 这里量的是**算式**
                        //    （照 UGUI `RubberDelta` :1084-1087 与 `SmoothDamp` 回弹那一支 :849-858）。
                        //    造一个临时区：视口 300 高 · 内容 600 ⇒ 可滚 [0,300]。
                        var es = MenuScroll.TopAligned(new PxRect(0f, 0f, 100f, 300f), 600f);
                        es.Elastic = true;
                        es.BeginDrag(0f);
                        es.DragTo(-400f);                                   // 手指往「内容尽头之外」拉 400
                        CheckTrue(es.OutOfRange, "Elastic：拉过头 ⇒ 内容**越出**可滚范围（Clamped 就拉不动了）");
                        CheckTrue(es.Offset < 400f - 1f,
                                  $"…但被 `RubberDelta` 阻尼住（拉 400 只走 {es.Offset:F1}；原版 :1084-1087）");
                        CheckTrue(es.Offset > es.ClampHi, "…而且确实越过了上界（不是被硬夹）");
                        es.EndDrag();
                        for (int i = 0; i < 400; i++) es.Tick(1f / 60f);
                        CheckNear(es.Offset, es.ClampHi, 0.5f, "松手后 `SmoothDamp` **回弹到位**");
                        CheckTrue(!es.OutOfRange, "回弹之后不再越界");
                    }
                }
            }

            // ============================================================ §三·b3 A12-P1 欠下的断言（本件**只写断言、不改实现**）
            //
            // 🔴 期望值只从两处来，**都不回读我们传进去的参数**：
            //    · **原版参数**（正本 `资料/阶段二_锻造厂与战役页_原版规格.md` §二 :73-78 的三级宿主与五元组）；
            //    · **渲染真值**（`ImageQuad.WorldW/WorldH`、`LayoutSpace.ToPixel` 的中心）。
            if (win.tabButtons != null) win.tabButtons.Click(2);      // 先切到锻造页（下面几条都在这页上）
            Section("§三·b3-a 粒子三宿主：层级 / 名字 / 身位 / 空态（正本 §二 :73-75）");
            {
                var psHost = FindPath(forge, "Background/War ParticleSystemUI");
                var psBody = FindPath(forge, "Background/War ParticleSystemUI/Warp Particle System");
                var psNeb = FindPath(forge, "Background/War ParticleSystemUI/Warp Particle System/Particle System nebula");
                CheckTrue(psHost != null, "`Background/War ParticleSystemUI` **逐字**建了（第 1 级，原版这个名字带 `UI`）");
                CheckTrue(psBody != null, "`…/Warp Particle System` 建了（第 2 级）");
                CheckTrue(psNeb != null, "`…/Particle System nebula` 建了（第 3 级）");
                // 原版这一件是 3D 子件 ⇒ **只有 `Transform`**
                CheckTrue(psNeb != null && psNeb.GetComponent<RectTransform>() == null,
                          "`Particle System nebula` **没有 `RectTransform`**（原版这一件只有一个 `Transform`）");

                // ---- 身位：三个宿主都立在**选择条中心那一列**上（原版方框 1483.64²、中心对齐）----
                CheckNearPx(psHost, 1125.35f, 575.5f, "`War ParticleSystemUI`（宿主）");
                CheckNearPx(psBody, 1125.35f, 575.5f,
                            "`Warp Particle System`（原版 `anchoredPosition ≈ −1.6e−4,0` ⇒ 与宿主同中心）");
                CheckNearPx(psNeb, 1125.35f, 575.5f,
                            "`Particle System nebula`（原版 `localPosition = (0, ~0, 0.553)` ⇒ 平面中心同宿主）");

                // ---- `Ready for level up` 底下那**两团**（原版 §二 :76 / :78）----
                var readyRoot = FindChild(forge, "Ready for level up");
                CheckTrue(readyRoot != null, "`Ready for level up` 建了");
                if (readyRoot != null)
                {
                    var names = new List<string>();
                    for (int k = 0; k < readyRoot.childCount; k++) names.Add(readyRoot.GetChild(k).name);
                    names.Sort(System.StringComparer.Ordinal);
                    Check(readyRoot.childCount, 3,
                          "`Ready for level up` 的子件**恰好 3 个**（多一个少一个都红；实测 ["
                          + string.Join("、", names.ToArray()) + "]）");
                    // 🆕 **2026-10-12（A440）**：文案里的色名同上一条 —— 原版序列化值是
                    //   `(1, 0.1745283, 0.8761433, 1)`，⛔ 不是手写的 `#FF2DDF`（判定未动）。
                    CheckTrue(names.Contains("Glow"),
                              "…其中一个是 `Glow`（那团 700² 的洋红光，色 = 原版序列化值 `(1, 0.1745283, 0.8761433, 1)`）");
                    CheckTrue(names.Contains("War ParticleSystemUI Down"), "…其中一个是 `War ParticleSystemUI Down`（带 `UI`）");
                    CheckTrue(names.Contains("War Particle System Up"),
                              "…其中一个是 `War Particle System Up`（🔴 原版**这个名字没有 `UI`** —— 逐份读的名字，别顺手改齐）");
                }
                var blobDown = readyRoot != null ? FindChild(readyRoot, "War ParticleSystemUI Down") : null;
                var blobUp = readyRoot != null ? FindChild(readyRoot, "War Particle System Up") : null;
                CheckNearPx(blobDown, 1125.35f, 949.5f, "`War ParticleSystemUI Down`（原版 `pos = (0, −374)`）");
                CheckNearPx(blobUp, 1125.35f, 191.5f, "`War Particle System Up`（原版 `pos = (0, +384)`；两件**不对称**）");
                // 两团各自的 `Rays → Glow` —— 两层都与宿主同中心（原版 `anchoredPosition` 是 1e−5 量级）
                CheckNearPx(blobDown != null ? FindChild(blobDown, "Rays") : null, 1125.35f, 949.5f,
                            "`…Down/Rays`（与宿主同中心）");
                CheckNearPx(blobDown != null ? FindChild(blobDown, "Glow (1)") : null, 1125.35f, 949.5f,
                            "`…Down/Rays/Glow (1)`（🔴 原版 Down 那一支的孙件**叫 `Glow (1)`**，与 Up 的 `Glow` 不同名）");
                CheckNearPx(blobUp != null ? FindChild(blobUp, "Rays") : null, 1125.35f, 191.5f, "`…Up/Rays`（与宿主同中心）");
                CheckNearPx(blobUp != null ? FindChild(blobUp, "Glow") : null, 1125.35f, 191.5f,
                            "`…Up/Rays/Glow`（与宿主同中心）");

                // ---- 空态：**一个 `ParticleSystem` 都没有** ----
                // 判据（`ForgeTab.Build` 末尾那条日志）：三套粒子的材质是**外链**、本工程里没有它们的导出资产
                // ⇒ **不拿「参数是我们挑的」粒子冒充原版**（铁律 3）。宿主的层级/名字照原文各就各位、不报错、不留残留。
                Check(forge.GetComponentsInChildren<ParticleSystem>(true).Length, 0,
                      "锻造厂页子树里**一个 `ParticleSystem` 都没有**（空态 —— 原版那三套的材质是外链、本工程没有导出资产）");
            }

            // ============================================================ §A92 节点类型（2026-10-07 新增）
            //
            // 🔴 **为什么单开一节**：`MenuDraw.Node` / `MenuWindowBase.Node` / `MenuWindowBase.New` 这三条
            //    「空节点工厂」原来建的是**裸 `Transform`** —— 连 `rect` 都没有，原版那些**带矩形语义**的容器
            //    我们这一层表达不了（所以上一条那三级的「宿主是 `RectTransform`」也无从判起）。
            //    **判据 = 原版自己的节点类型**（不是我们的常量）：
            //      · `bundle_menus_assets_all` 的 **16768** 个 `GameObject` 里 **16510 是 `RectTransform`**、
            //        只有 **258** 个是裸 `Transform`；
            //      · 那 258 个**清一色是卡框 3D 子锚与粒子件**（`Textbackgrounds` 41 · `Tactic Container` 41 ·
            //        `EffectAnchor` 41 · `Card Info` 41 · `MinionOrWarlord Container` 40 ＋
            //        `Wave*` / `Trails` / `Sparks` / `Glow*` / **`Particle System nebula`**）——
            //        **一个菜单容器都没有**。
            //      · 主菜单场景（`bundle_scenes_scenes_mainmenuwarpforge`）同构：592 `RectTransform` / 105 裸。
            //    ⇒ 本节断**两种状态**（弱断言分不出两态 ⇒ 必须成对）：
            //      **容器有**（三条工厂各一条）＋ **原版那个 3D 子件没有**（`NewPlainTransform`，与
            //      §三·b3-a 的那条「`Particle System nebula` **没有** `RectTransform`」成对）。
            //    ⚠️ 位置的回归不在这里管 —— §三·b3-a 的 `CheckNearPx(psHost, 1125.35, 575.5)` 那几条
            //       期望值是**原版字面量**，节点类型一改就跟着一起验了（位置偏了那几条会红）。
            Section("§A92 节点类型：三条空节点工厂都建 `RectTransform`（原版 16510/16768；例外只有粒子 3D 子件）");
            {
                var a92r = new PxRect(0f, 0f, 10f, 10f);
                var a92n1 = MenuDraw.Node(win.transform, "A92NodeProbe", a92r);
                CheckTrue(a92n1 != null && a92n1.GetComponent<RectTransform>() != null,
                          "`MenuDraw.Node` 建出来的是 **`RectTransform`**（原来连 `rect` 都没有 ⇒ 宽高无从验收）");
                var a92n2 = RewardsWindow.Node(win.transform, "A92WindowNodeProbe", a92r);
                CheckTrue(a92n2 != null && a92n2.GetComponent<RectTransform>() != null,
                          "`RewardsWindow.Node`（= `MenuWindowBase.Node`，全工程 40+ 处）建的也是 **`RectTransform`**");
                var a92n3 = RewardsWindow.New(win.transform, "A92NewProbe");
                CheckTrue(a92n3 != null && a92n3.GetComponent<RectTransform>() != null,
                          "`RewardsWindow.New`（直调那条路）建的也是 **`RectTransform`**");
                // 🔴 反向那一半：原版唯一那类「只有 `Transform`」的件（3D 粒子子件）**不许被顺手改齐**
                var a92n4 = RewardsWindow.NewPlainTransform(win.transform, "A92PlainProbe");
                CheckTrue(a92n4 != null && a92n4.GetComponent<RectTransform>() == null,
                          "🔴 `NewPlainTransform` 建的**不是** `RectTransform`（唯一用途 = 原版那个 3D 子件"
                          + " `Particle System nebula`；与 §三·b3-a 那条成对）");
                if (a92n1 != null) Object.DestroyImmediate(a92n1.gameObject);
                if (a92n2 != null) Object.DestroyImmediate(a92n2.gameObject);
                if (a92n3 != null) Object.DestroyImmediate(a92n3.gameObject);
                if (a92n4 != null) Object.DestroyImmediate(a92n4.gameObject);   // `Object.Destroy` 在批处理下不生效

                // `MenuDraw.Hit` —— 全工程那些透明命中区（原版这一层就是**按钮自己的 `RectTransform`**，
                // 见 `MenuDraw.Hit` 的头注释）；同一族的另两条路（`MenuWindowBase` 的 `New(b, "Hit")` 与
                // `AddHit` 的转调）走的是上面那几条工厂 ⇒ **一起改，免得同一件东西一半一种类型**。
                var a92hit = MenuDraw.Hit(win.transform, "A92HitProbe", new PxRect(400f, 400f, 500f, 450f), 3000,
                                          () => { }, null, null, null, null, null);
                CheckTrue(a92hit != null && a92hit.GetComponent<RectTransform>() != null,
                          "`MenuDraw.Hit` 的命中区节点是 **`RectTransform`**（原版这一层就是按钮自己的 `RectTransform`）");
                if (a92hit != null)
                {
                    var a92hq = a92hit.GetComponentInChildren<ImageQuad>();
                    // 换节点类型**不该动命中区**：quad 的宽仍 = 传进去那个矩形的宽（期望值是入参算出来的，不是读实现）
                    CheckNear(a92hq != null ? a92hq.WorldW * 108f : -1f, 100f, 0.5f,
                              "…命中区那颗透明 quad 的宽仍是 **100px**（= 传进去的矩形 400→500；换类型不许动它）");
                    Object.DestroyImmediate(a92hit.gameObject);
                }

                // ---- 真树上那三级（§三·b3-a 断的是「第 3 级没有」，这里断「1/2 级有」）----
                var a92Host = FindPath(forge, "Background/War ParticleSystemUI");
                var a92Body = FindPath(forge, "Background/War ParticleSystemUI/Warp Particle System");
                CheckTrue(a92Host != null && a92Host.GetComponent<RectTransform>() != null,
                          "原版第 1 级 `War ParticleSystemUI` 是 `RectTransform`（同名的 `GameObject` 8 个实例都如此）");
                CheckTrue(a92Body != null && a92Body.GetComponent<RectTransform>() != null,
                          "原版第 2 级 `Warp Particle System` 是 `RectTransform`（8 个实例都如此）");
            }

            // ============================================================ §A218 `sizeDelta`（2026-10-11 新增）
            //
            // 🔴 **为什么单开一节**：A92 只补了「类型」（`RectTransform`），`sizeDelta` 仍是默认值
            //    ⇒ 「空节点 + `PxRect`」的**宽高验收不了**（判据原文 = `资料/待办判据_1007.md` §A218 的 ①）。
            //    本节断两件事：
            //      ① **写进去了没有** —— 期望值 = **传进去那个 `PxRect`** 经 `LayoutSpace.Px`（唯一那一份换算）
            //         折出来的宽高（⛔ 不读被测实现、不拿 `rt.sizeDelta` 自己当期望值）；
            //      ② **写尺寸没挪位置** —— `SetPxSize` 里「先存 `localPosition`、写完再放回去」那道守卫
            //         （uGUI 的 `localPosition` ↔ `anchoredPosition` 是**互相推导**的）。
            //    **两态可分辨**：同一对工厂、**两对不同的矩形**各建一次 ⇒ 弱断言（只断 `!= 0`）分不出来。
            Section("§A218 `sizeDelta`：空节点工厂把 `PxRect` 的宽高写进 `rect`（A92 的下一半）");
            {
                // 🔴 **父链缩放核查**：`rect` 与「设计 px」同量纲**只在父链 `lossyScale == 1` 时成立**
                //    ⇒ 每条都先断这条前置（今天成立：`WindowsManager.AttachToAnchor` 把窗根写成 `localScale = one`；
                //    小屏缩放器只在开关开、且 `extraScaleSmallScreen ≠ 1` 时才乘 **窗根**那一级 ⇒ 窗内件的
                //    `sizeDelta` **不该**再折算 —— 判据与算式见 `MenuDraw.SetPxSize` 的注释）。
                var a218r1 = new PxRect(0f, 0f, 320f, 180f);
                var a218r2 = new PxRect(0f, 0f, 640f, 90f);
                var a218n1 = MenuDraw.Node(win.transform, "A218NodeProbeA", a218r1);
                var a218n2 = RewardsWindow.Node(win.transform, "A218NodeProbeB", a218r2);
                var a218rt1 = a218n1 != null ? a218n1.GetComponent<RectTransform>() : null;
                var a218rt2 = a218n2 != null ? a218n2.GetComponent<RectTransform>() : null;
                CheckTrue(a218rt1 != null && a218rt2 != null,
                          "（前提）`MenuDraw.Node` / `RewardsWindow.Node` 建出来的都是 `RectTransform`（A92 那半）");
                if (a218rt1 != null && a218rt2 != null)
                {
                    CheckNear(a218n1.lossyScale.x, 1f, 1e-3f,
                              "（前提·父链缩放）`A218NodeProbeA` 的 `lossyScale.x` = 1（= `rect` 与设计 px 同量纲那一档）");
                    CheckNear(a218rt1.rect.width, LayoutSpace.Px(320f), 0.01f,
                              "★ `MenuDraw.Node` 的 `rect.width` = **320px**（= 传进去那个矩形；原来恒是默认值 ⇒ 验收不了）");
                    CheckNear(a218rt1.rect.height, LayoutSpace.Px(180f), 0.01f, "★ …`rect.height` = **180px**");
                    CheckNear(a218rt2.rect.width, LayoutSpace.Px(640f), 0.01f,
                              "★ `RewardsWindow.Node` 的 `rect.width` = **640px**（**同一工厂、另一对入参** ⇒ 两态可分辨）");
                    CheckNear(a218rt2.rect.height, LayoutSpace.Px(90f), 0.01f, "★ …`rect.height` = **90px**");
                    // `rect` 只由 `sizeDelta` 决定的前提：锚点重合、pivot 居中（⇒ `localPosition` 就是矩形中心）
                    CheckTrue(a218rt1.anchorMin == a218rt1.anchorMax && a218rt1.pivot == new Vector2(0.5f, 0.5f),
                              "★ 锚点写死「重合」+ pivot 居中（`rect` 与父矩形无关、`localPosition` = 矩形中心）");

                    // ---- 「写 `sizeDelta` 不挪位置」：本件的**改坏法守卫** ----
                    // 改坏法：删掉 `MenuDraw.SetPxSize` 里那两行存/放 ⇒ 父矩形非 0 时 uGUI 会按
                    //   `anchoredPosition` 反算出另一个 `localPosition`（差 `(refNorm − pivot)×父宽`）⇒ 这条红。
                    var a218before = a218n1.position;
                    MenuDraw.SetPxSize(a218n1, 40f, 20f);                       // 换一个尺寸
                    CheckNear((a218n1.position - a218before).magnitude, 0f, 1e-4f,
                              "★ 写 `sizeDelta` **不挪位置**（世界坐标逐位不变 —— `SetPxSize` 先存后放 `localPosition`）");
                    CheckNear(a218rt1.rect.width, LayoutSpace.Px(40f), 0.01f, "…而且新尺寸生效（40 ≠ 320 ⇒ 两态）");
                }
                if (a218n1 != null) Object.DestroyImmediate(a218n1.gameObject);
                if (a218n2 != null) Object.DestroyImmediate(a218n2.gameObject);   // `Destroy` 在批处理下不生效

                // ---- ② 窗口根：判据 = 原版 `Rewards Base Submenu Variant` 的绝对矩形 (0,0)-(1920,1080) ----
                var a218win = win.GetComponent<RectTransform>();
                CheckTrue(a218win != null, "（前提）奖励窗根是 `RectTransform`（A92 那半）");
                if (a218win != null)
                {
                    CheckNear(a218win.rect.width, LayoutSpace.Px(1920f), 0.01f,
                              "★ 奖励窗根 `rect.width` = **1920px**（原版 stretch + `sizeDelta (0,0)` ⇒ 整屏矩形）");
                    CheckNear(a218win.rect.height, LayoutSpace.Px(1080f), 0.01f, "★ …`rect.height` = **1080px**");
                }

                // ---- ③ `MenuDraw.Hit` 的命中区节点（只写尺寸、**不挪位**：它 `localPosition` 恒 0）----
                var a218hit = MenuDraw.Hit(win.transform, "A218HitProbe", new PxRect(400f, 400f, 500f, 450f),
                                           3000, () => { }, null, null, null, null, null);
                var a218hitRt = a218hit != null ? a218hit.GetComponent<RectTransform>() : null;
                CheckTrue(a218hitRt != null, "（前提）`MenuDraw.Hit` 的节点是 `RectTransform`（A92 那半）");
                if (a218hitRt != null)
                {
                    CheckNear(a218hitRt.rect.width, LayoutSpace.Px(100f), 0.01f,
                              "★ 命中区节点 `rect.width` = **100px**（= 传进去的矩形 400→500）");
                    CheckNear(a218hitRt.rect.height, LayoutSpace.Px(50f), 0.01f, "★ …`rect.height` = **50px**（400→450）");
                    CheckNear(a218hit.localPosition.magnitude, 0f, 1e-4f,
                              "★ …而它**仍在父原点**（`Hit` 的既有约定：节点不摆位、quad 才摆在矩形中心）—— 写尺寸没破它");
                    Object.DestroyImmediate(a218hit.gameObject);
                }
            }

            Section("§三·b3-b 两团光跟着**可领态**（`hasToCollectReward`，原版 `ForgeRewardSelector.CreateRewards`）");
            {
                var ftR = forge.GetComponent<ForgeTab>();
                // 🆕 A426：那颗 700² 洋红光的 quad 与它上面接的 `BlinkGraphic`（`null` = **没接**）
                var glowQ = ftR != null ? ftR.GlowQuad : null;
                var blk = ftR != null ? ftR.Blink : null;
                var ready = FindChild(forge, "Ready for level up");
                CheckTrue(ForgeData.HasToCollect(ForgeData.Selected),
                          $"当前阵营（{ForgeData.Selected}）**正好有可领的**（否则下面几条等于没验）");
                if (ftR != null) ftR.Refresh();
                var dHost = ready != null ? FindChild(ready, "War ParticleSystemUI Down") : null;
                var uHost = ready != null ? FindChild(ready, "War Particle System Up") : null;
                CheckTrue(ready != null && ready.gameObject.activeSelf, "可领 ⇒ `Ready for level up` **亮**");
                CheckTrue(dHost != null && dHost.gameObject.activeInHierarchy
                          && uHost != null && uHost.gameObject.activeInHierarchy,
                          "…**两团宿主跟着亮**（`activeInHierarchy` —— 它们挂在这根开关下面）");

                // ================================================================
                // 🆕 **2026-10-12（A426）**：那颗 700² 洋红光在原版**是会呼吸的**
                //   判据 = `Forge Tab / Ready for level up / Glow` 的组件表 = **`Image,BlinkGraphic`**
                //     （`python 工具/menu_dump.py bundle_menus_assets_all "Forge Tab" --depth 3` 实读），
                //     `(blinkSpeed, colorVariation) = (1.0, 0.5)`（= 原版 `.ctor` 的两个立即数，两份实例逐位相同）。
                //   接线 = `Shell/ForgeTab.cs` 的 `BindBlink(_glowQuad)`；时钟由本页 `Tick(dt)` 推。
                //   公式判据 = `BlinkGraphic__Update.c`：`:17` 读时钟算 `t = Clamp01(|cos(blinkSpeed × currentTime)|)`
                //     → `:24-26` **只改 alpha** 写色 → **`:31` 才算完推进** `currentTime += deltaTime`。
                // ⛔ **期望值一个都不读被测实现**（`Shell/ForgeTab.cs` 的常量 / `BlinkGraphic.Default*`）——
                //    全部写**原版立即数**（1.0 / 0.5）与**手算值**。
                // ⛔ **不断「时钟的绝对值」**（它是 `Σdt`，夹具给几拍它就几拍）—— 只断**相对关系**与**写进去的那一档**。
                // ⚠️ 整段必须在**领掉之前**跑：领完 `Ready for level up` 就灭了 ⇒ ③④⑤ 会全成空验（⑦ 反着来）。
                CheckTrue(blk != null, "★ A426：`Ready for level up / Glow` **接了 `BlinkGraphic`**"
                          + "（删掉 `Shell/ForgeTab.cs` 的 `BindBlink(_glowQuad)` ⇒ 红）");
                CheckTrue(glowQ != null, "★ …而且自检拿得到那颗 quad（`ForgeTab.GlowQuad`）");
                if (blk != null && glowQ != null)
                {
                    CheckTrue(blk.blinkSpeed == 1f, "★ `blinkSpeed` = **1.0**（原版 `.ctor` 立即数 `0x3f800000`；"
                              + "⛔ 不读 `BlinkGraphic.DefaultSpeed` —— 那是被测侧的数）");
                    CheckTrue(blk.colorVariation == 0.5f, "★ `colorVariation` = **0.5**（原版 `.ctor` 立即数 `0x3f000000`）");
                    // ---- ③ 出厂那一档 α = **原色**（原版 `Start()` 只存不写；`Build()` 一帧都不写色）----
                    // ⚠️ 色一律用本文件既有的 `TintOf` 量（**材质不在时返回 (0,0,0,0)**，会当场把
                    //    「这颗 quad 根本没建出来」报出来 —— ⛔ 别用 `ImageQuad.Tint`：那条路
                    //    材质缺失时会兜底成**白**（α=1）⇒ 下面 ③ 会**假绿**）。
                    float aBase = TintOf(glowQ.transform).a;
                    CheckNear(aBase, 1f, 1e-4f, "★ 出厂那一档 α = **原色**（原版那颗 `Image.m_Color.a` = 1 —— "
                              + "`Restart()` 只取原色、**一帧都不写**；把 `Restart()` 改成也写一次色 ⇒ 这条红）"
                              + " ⚠️ 它同时是下面 ④⑤ 能分辨的**前提**（`0.5 × 0.5 == 0.5` ⇒ 原色若不是 1 就分不出来）");
                    // ---- ④ `Tick(0f)`：时钟 0 ⇒ `|cos 0| = 1` ⇒ α = `Lerp(A, 0.5A, 1)` = **0.5A** ----
                    ftR.Tick(0f);
                    CheckNear(TintOf(glowQ.transform).a, aBase * 0.5f, 1e-4f,
                              "★ `Tick(0f)`（时钟 0）⇒ α = **0.5 × 原色**（`|cos 0| = 1`；把公式里的 `cos` 写成 `sin`"
                              + " ⇒ 这里得**原色** ⇒ 红）");
                    // ---- ⑤ 写色与推进的**次序**（本段唯一分得出两种次序的一条）----
                    ftR.Tick(Mathf.PI * 0.5f);
                    CheckNear(TintOf(glowQ.transform).a, aBase * 0.5f, 1e-4f,
                              "★ `Tick(π/2)`：写进去的仍是**推进前**那一档（0.5A）—— 原版 `:17` 读时钟 → `:24-26` 写色"
                              + " → **`:31` 才** `+= deltaTime`；把 `BlinkGraphic.Tick` 里那句 `_clock += dt`"
                              + " 挪到写色**之前** ⇒ 这一条得原色 ⇒ 红");
                    ftR.Tick(0f);
                    CheckNear(TintOf(glowQ.transform).a, aBase, 1e-4f,
                              "★ 再 `Tick(0f)`：时钟已到 π/2 ⇒ `|cos π/2| = 0` ⇒ α 回到**原色**（配对断言："
                              + "证明每拍都真的重写了一次，不是只在第一拍写）");
                    // ---- ⑥ 时钟**只在 `Tick` 里**推，推的就是给进去的那个 `dt` ----
                    CheckNear(blk.Clock, Mathf.PI * 0.5f, 1e-4f,
                              "★ `Blink.Clock` = 0 + π/2 + 0 = **π/2**（时钟只在 `Tick` 里 `+= dt`；"
                              + "把 `Tick` 里那一句删掉 ⇒ ④⑤⑥ 全红）");
                }

                // ---- 领掉它 ⇒ 两团一起灭 ----
                int lvR = ForgeData.LevelOf(ForgeData.Selected);
                if (ftR != null) ftR.FocusClaimable();
                var cellR = FindChild(fcontent, "ForgeCell_" + lvR);
                var claimN = FindChild(cellR, "ClaimHit");
                var claimW = claimN != null ? claimN.GetComponent<WindowButton>() : null;
                CheckTrue(claimW != null, "该领那一格的 `ClaimHit` 在（下面点它）");

                // ================================================================
                // 🆕 **2026-10-12（A441）③ 负例：领不到 ⇒ 一扇窗都不许弹**
                //  🔴 **为什么不直接点「`InProgress` / `Locked` 那一格」**：`ForgeTab.BuildCell` **只在
                //     `st == ToCollect` 时**才 `AddHit("ClaimHit", …)` ⇒ 那两种状态的格子**根本没有按钮**
                //     （字面上点不到）。⇒ 用**同一个节点、同一个守卫**造这一态：先直调 `ForgeData.Claim`
                //     把这一格**在数据上**领掉（**不重建页面** ⇒ `ClaimHit` 还挂在原处），再点它
                //     —— 这一下走的正是 `ClaimCell(lvR)` 里 `ForgeData.Claim` 返回 false 那条支路。
                //  改坏法：把 `Shell/ForgeTab.cs` 的 `ClaimCell` 里那句 `RewardWindow.ShowCollected(...)`
                //     挪到 `Claim` 守卫**之前**（= 没领到也弹）⇒ 下面那条 `RewardWindowFixture.OpenRewardWindowCount() == 0` 红。
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ A441 ③：（前提）点之前场上**没有遗留的领奖窗**"
                      + "（有 ⇒ 下面那条 0 的判据会被上一处污染）");
                string whyNeg;
                CheckTrue(ForgeData.Claim(ForgeData.Selected, lvR, out whyNeg),
                          "★ …（前提）先把这一格**在数据上**领掉（页面**不重建** ⇒ `ClaimHit` 还在原处）");
                if (claimW != null) claimW.Click();
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                      "★ …守卫挡下的那一下**一扇领奖窗都不许弹**（`ShowCollected` 写在守卫之前 ⇒ 红）");
                Check(ForgeData.LevelOf(ForgeData.Selected), lvR + 1,
                      "★ …而且格号**没有二次自增**（同一道守卫挡在 `ShowCollected` 之前）");
                // ---- 还原到「还没领」那一态（下面走**真的**那一击）----
                ForgeData.ResetForTest();
                if (ftR != null) { ftR.Refresh(); ftR.FocusClaimable(); }
                cellR = FindChild(fcontent, "ForgeCell_" + lvR);
                claimN = FindChild(cellR, "ClaimHit");
                claimW = claimN != null ? claimN.GetComponent<WindowButton>() : null;
                CheckTrue(claimW != null && ready != null && ready.gameObject.activeSelf,
                          "★ …（还原）重建之后这一格**又是可领**、`Ready for level up` 又亮了"
                          + "（否则下面那几条是空验）");

                // ================================================================
                // 🆕 **2026-10-12（A441）①②：真的那一击 ⇒ 领到 + 弹那扇 `Reward Window`**
                //  判据（第一权威 = 反编译方法体）= 原版 `Everguild.LiveOps.Forge.CollectRewards` 的回包
                //   `RewardService.Collect(rewards, showAnimation: **1**, …)`（全文见 `Shell/ForgeTab.cs`
                //   的 `ClaimCell` 方法头）—— 我们这一侧落在 `RewardWindow.ShowCollected(...)`。
                //  ⚠️ **点完必须立刻关掉**：那扇窗是**弹窗**（`WindowsManager` 会把底窗压到 `Background`）
                //   ⇒ 不关就会盖住本节后面的断言与那两张截图（A313 已经吃过这个亏，见本文件
                //   `ClickCollectAndDismiss` 的注释）—— 所以这一击走 R2 那个把手（点 + 断真弹了 + 立刻关）。
                //  ⚠️ 比「窗里那一条」的期望值时**量的是格子里画出来的那张图**（`ArtOf` = 渲染真值），
                //   ⛔ 不是再读一次 `ForgeData.RewardAt(...)`（读它就成了同义反复）。
                string artCell = ArtOf(FindChild(cellR, "RewardTransform"));
                CheckTrue(artCell != null, "（前提）该领那一格里的奖励图**画出来了**（下面拿它比窗里那一条）");
                CheckTrue(artCell == null || artCell != ForgeData.RewardAt(0).Art,
                          "★ …（分辨前提）这一格画的**不是** `RewardAt(0)` 那一张 —— 否则「把格号传成 0」"
                          + "这个改坏法在下面两条里**看不出来**（两条都会绿）"
                          + "（写成 `artCell == null ||` 是**不叠红**：取不到图那条上一句已经报过了）");
                var rwForge = ClickCollectAndDismiss(claimN, claimW, "锻造轨道的 `ClaimHit`");
                Check(ForgeData.LevelOf(ForgeData.Selected), lvR + 1,
                      "点了 Claim ⇒ 已领格数 +1（原版语义：只把 `rewardsCollected` 加一，不扣经验）");
                CheckTrue(rwForge != null, "★ A441 ①：锻造领到 ⇒ **弹出 `Reward Window`**"
                          + "（删掉 `Shell/ForgeTab.cs` 的 `ClaimCell` 里那句 `RewardWindow.ShowCollected(...)` ⇒ 红）");
                if (rwForge != null)
                {
                    var rcF = rwForge.Context;
                    Check(rcF != null && rcF.Rewards != null ? rcF.Rewards.Length : -1, 1,
                          "★ …带进来的是**这一格**那一条奖励（1 条 —— 不是空窗、也不是把整条轨道塞进来）");
                    int qtyCell = ForgeData.RewardAt(lvR).Qty;
                    CheckTrue(artCell == null || (rcF != null && rcF.Rewards != null && rcF.Rewards.Length > 0
                              && rcF.Rewards[0].Id == artCell),
                              "★ …`Id` = **格子里画的那张图**（`" + artCell + "`）"
                              + "—— 传成 `RewardAt(0)` 那种错位 ⇒ 红（见上面那条分辨前提；"
                              + "`artCell == null` 那一支**不叠红** —— 上面已经报过了）");
                    Check(rcF != null && rcF.Rewards != null && rcF.Rewards.Length > 0 ? rcF.Rewards[0].Quantity : -1,
                          qtyCell,
                          "★ …`Quantity` = **那一格的 `Qty`**（= " + qtyCell + "，是个 `int`、不是 `Text` 那种字符串）"
                          + " —— 与上图同理，错位一格就是这个数对不上");
                }
                CheckTrue(!ForgeData.HasToCollect(ForgeData.Selected), "领完 ⇒ `hasToCollectReward` 变假");
                CheckTrue(ready != null && !ready.gameObject.activeSelf, "…`Ready for level up` **灭**");
                CheckTrue((dHost == null || !dHost.gameObject.activeInHierarchy)
                          && (uHost == null || !uHost.gameObject.activeInHierarchy),
                          "…**两团一起灭**（同一个开关管住它们）");
                // ================================================================
                // 🆕 **2026-10-12（A426）⑦ 负例：`Ready for level up` 灭着时 ⇒ 时钟一动不动**
                //  判据 = 原版那颗件挂在**目标节点自己**身上 ⇒ `SetActive(false)` 时它的 `Update()` **根本
                //   不跑、时钟冻住**，再亮起来是**接着那一拍**往下走（`BlinkGraphic__Update.c` 由帧循环驱动）。
                //  ⛔ 只断「隐藏期间读 `Clock`」断不出这一条 —— 公共件那一档**照推时钟**
                //   （`Shell/BlinkGraphic.cs` 文件头 ③ 那处有意不同），断的是**宿主把原语义补回来**那一句。
                //  改坏法：把 `Shell/ForgeTab.cs` 的 `Tick` 里那句 `activeInHierarchy` 判断删掉 ⇒ 红。
                if (blk != null)
                {
                    CheckTrue(ready != null && !ready.gameObject.activeInHierarchy,
                              "★ （前提）`Ready for level up` 现在**确实关着**（上面那句刚证过 —— 否则这一条等于没验）");
                    float cOff = blk.Clock;
                    ftR.Tick(1f);
                    CheckNear(blk.Clock, cOff, 1e-6f,
                              "★ A426 ⑦：关着时 `Tick(1f)` ⇒ 时钟**一动不动**（= 原版组件随节点停）");
                }
                // ---- 幂等：连调两次 `Refresh()` 不许长出第二个宿主来 ----
                if (ftR != null) { ftR.Refresh(); ftR.Refresh(); }
                CheckTrue(ready != null && ready.childCount == 3,
                          "连调两次 `Refresh()` ⇒ `Ready for level up` 的子件**仍是 3 个**"
                          + $"（实测 {(ready != null ? ready.childCount : -1)}）—— 不留残留");
            }
            // ⚠️ 上面把 Goff 那格领掉了 ⇒ **还原成起手态**（C 段与后面两张截图都要它）
            ForgeData.ResetForTest();
            {
                var ftB = forge.GetComponent<ForgeTab>();
                if (ftB != null) ftB.SelectArmy(ForgeData.Selected);
            }

            Section("§三·b3-c `Help Icon` 的 tooltip（原版 `EverguildTooltipTrigger`：悬停即出、离开即收）");
            {
                var layerT = PointerLayer.Instance;
                var helpNode = FindChild(forge, "Help Icon");
                CheckTrue(helpNode != null, "`Help Icon` 建了");
                // ① 正文：**一个字都没编**（原版是 I2 词条 `MainMenu/Forge/Help`，词条表在远端 CCD、本地没有）
                CheckTrue(ForgeTab.HelpTipBody.Trim() == "",
                          "`ForgeTab.HelpTipBody` **一个字都没编**（面板照弹、正文空 —— 裁定过，别改成「不出面板」）");
                var helpQ = helpNode != null ? helpNode.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(helpQ != null, "`Help Icon` 有渲染 quad（下面拿它的**渲染中心**当悬停点）");
                var hpx = helpQ != null ? LayoutSpace.ToPixel(helpQ.transform.position) : Vector2.zero;
                int hov0 = ForgeTab.HelpTipHovers, sc0 = Tooltip.ShowCount;
                var hb = layerT != null ? layerT.HoverAt(hpx.x, hpx.y) : null;
                CheckTrue(hb != null && hb == ForgeTab.HelpTipHit,
                          "`PointerLayer.HoverAt(图标中心)` 打到的**就是** `ForgeTab.HelpTipHit`（接线通）");
                Check(ForgeTab.HelpTipHovers, hov0 + 1, "…悬停计数 +1");
                Check(Tooltip.ShowCount, sc0 + 1, "…`Tooltip.ShowCount` +1（**面板照原版的时机弹出来了**）");
                CheckTrue(Tooltip.Visible, "…`Tooltip.Visible == true`");
                CheckTrue((Tooltip.ShownBody ?? "").Trim() == "", "…面板里的**正文是空的**（一个字都没编）");
                // ② 面板**钉在图标上**（原版 `Show()` 取的是触发器自己的 `transform.position`，**不跟鼠标**）
                //    判据 = `tooltipAnchor = 25`（右上）⇒ pivot **(1,1)** ⇒ 面板的右上角落在图标中心
                float pxc = LayoutSpace.PxX(Tooltip.PanelCenter.x), pyc = LayoutSpace.PxY(Tooltip.PanelCenter.y);
                float pw = Tooltip.PanelSizePx.x, ph = Tooltip.PanelSizePx.y;
                CheckNear(pxc, hpx.x - pw * 0.5f, 1f,
                          "面板中心 x = 图标中心 − 半宽（pivot **(1,1)** ⇒ 面板的右上角 = 图标中心）");
                CheckNear(pyc, hpx.y + ph * 0.5f, 1f, "…面板中心 y = 图标中心 + 半高（同一句话的另一半）");
                CheckTrue(Mathf.Abs(pxc - hpx.x) > 5f && Mathf.Abs(pyc - hpx.y) > 5f,
                          $"…**而且确实偏开了**（面板 {pw:F0}×{ph:F0} ⇒ 偏 ({pxc - hpx.x:F0},{pyc - hpx.y:F0})）"
                          + " —— 否则「pivot=(1,1)」这句等于没验");
                // ③ 离开 ⇒ 立刻收
                int hov1 = ForgeTab.HelpTipHovers;
                if (layerT != null) layerT.HoverAt(5f, 5f);     // 页面左上角空白（同文件另一条已证这里打不中任何按钮）
                Tooltip.FinishFade();                           // ⚠️ 批处理没有帧循环 ⇒ 手动结束淡出
                Check(ForgeTab.HelpTipHovers, hov1, "指针挪到空白 ⇒ 悬停计数**不变**（没有误触发）");
                CheckTrue(!Tooltip.Visible, "…`Tooltip.Visible == false`（原版 `OnPointerExit` **立刻**收）");
            }

            Section("§三·b3-d 两个 `Viewport` 的裁切（原版 `RectMask2D` 的渲染那一面 + 射线那一面）");
            {
                var ftV = forge.GetComponent<ForgeTab>();
                var layerV = PointerLayer.Instance;
                // ---- ① 阵营条：滚到两端 ⇒ **视口外那一端的条目一个都不建** ----
                var aScroll = ftV != null ? ftV.ArmyScroll : null;
                int nAllA = ForgeData.Armies.Length;
                CheckTrue(aScroll != null && fArmy != null, "阵营条的滚动区与 `Army Content` 都在");
                if (aScroll != null && fArmy != null)
                {
                    string lastN = "ForgeArmyItem_" + (nAllA - 1);
                    aScroll.SetOffset(aScroll.MinOffset);
                    CheckTrue(fArmy.childCount < nAllA,
                              $"滚到最左 ⇒ 建出来的条目**少于全部 {nAllA} 条**（实测 {fArmy.childCount}）—— 视口外的不建");
                    CheckTrue(FindChild(fArmy, lastN) == null,
                              $"…最左端那一条（{ForgeData.Armies[nAllA - 1]}）**整条在视口外 ⇒ 连节点都不建**");
                    CheckTrue(FindChild(fArmy, "ForgeArmyItem_0") != null,
                              "…而另一端（第 1 个阵营）**进视口了 ⇒ 建了**（否则上面那条等于没验）");
                    aScroll.SetOffset(aScroll.MaxOffset);
                    CheckTrue(fArmy.childCount < nAllA,
                              $"滚到最右 ⇒ 条目同样**少于 {nAllA} 条**（实测 {fArmy.childCount}）");
                    CheckTrue(FindChild(fArmy, "ForgeArmyItem_0") == null,
                              "…第 1 个阵营**整条在视口外 ⇒ 不建**");
                    CheckTrue(FindChild(fArmy, lastN) != null, "…而最右端那一条进视口了 ⇒ 建了");
                }

                // ---- ② 奖励轨道：滚到最后一格 ⇒ 第 1 格连节点都不建、那一带也点不中 ----
                var tScroll = ftV != null ? ftV.TrackScroll : null;
                // 原版 `Rewards Scroll View` = 330.97,318.60 → 1919.73,1080.00（本文件上面 `CheckAt(ftrack, …)` 已钉住）
                var trackR = new PxRect(330.97f, 318.60f, 1919.73f, 1080f);
                CheckTrue(tScroll != null && fcontent != null, "轨道的滚动区与 `Rewards Content` 都在");
                if (tScroll != null && fcontent != null)
                {
                    tScroll.SetOffset(tScroll.MaxOffset);
                    CheckTrue(FindChild(fcontent, "ForgeCell_0") == null,
                              "**滚到最后一格 ⇒ 第 1 格整格在视口外 ⇒ 连节点都不建**（原版 `RectMask2D` 就是裁掉它）");
                    var c0r = tScroll.Shift(UguiLayout.HorizontalChild(trackR, ForgeTab.CellW, ForgeTab.CellH, 0,
                                                                      ForgeTab.TrackPadL, ForgeTab.TrackSpacing));
                    PxRect c0v;
                    CheckTrue(!MenuDraw.ClipRect(c0r, trackR, out c0v),
                              "…（现算：第 1 格的矩形与视口**无交集** ⇒ 「不建」正是原版 `RectMask2D` 的结果）");
                    // 原版 Claim 的 `pos.y = −134.147` ⇒ 那一格 Claim 该在的位置
                    float c0x = c0r.CX, c0y = c0r.CY + 134.147f;
                    CheckTrue(layerV == null || layerV.ButtonAt(c0x, c0y) == null,
                              $"…⇒ 第 1 格 Claim 该在的那一点（{c0x:F0},{c0y:F0}）**点不中任何按钮**"
                              + "（判据 = 原版 `RectMask2D` 的**射线那一面**）");

                    // ---- 🔴 非空对照：把**该领的那一格**移到左边缘压线 ⇒ 命中区**被截**、截过之后仍然点得中 ----
                    int lvV = ForgeData.LevelOf(ForgeData.Selected);
                    Check(ForgeData.StateAt(ForgeData.Selected, lvV), ForgeData.ToCollect,
                          "该领那一格的状态 = `ToCollect`（否则下面这条非空对照没意义）");
                    var cellLv = UguiLayout.HorizontalChild(trackR, ForgeTab.CellW, ForgeTab.CellH, lvV,
                                                            ForgeTab.TrackPadL, ForgeTab.TrackSpacing);
                    tScroll.SetOffset(cellLv.CX - (trackR.x1 + 26.5f));   // 让 Claim 的左半截伸到视口左边缘之外
                    var cellEdge = FindChild(fcontent, "ForgeCell_" + lvV);
                    var claimEdge = FindChild(cellEdge, "ClaimHit");
                    float qx1, qy1, qx2, qy2;
                    CheckTrue(claimEdge != null && RectOf(claimEdge, out qx1, out qy1, out qx2, out qy2),
                              "**压在视口边上的那一格**：`ClaimHit` 建了、渲染矩形量得到（部分越界 ⇒ 建、但被截）");
                    if (claimEdge != null && RectOf(claimEdge, out qx1, out qy1, out qx2, out qy2))
                    {
                        // 🔴 **2026-10-08（A188）就地订正（铁律 5）**：这一条原来写 `trackR.x1`（= 330.97），
                        //    钉的是**旧模型**「`pad` 缩命中区自己的矩形」（`(R − pad) ∩ V`）。
                        //    A188 已按 uGUI 判据改成 **`R ∩ (V − pad)`**（`pad` 缩的是 **mask 自己那个框**）——
                        //    判据两关：`GraphicRaycaster.cs:327`（图形自己的 rect ∧ 自己的 `raycastPadding`）
                        //    ∧ `RectMask2D.cs:178-184`（mask 自己的 `rectTransform` ∧ `m_Padding`）。
                        //    本壳唯一非零 `m_Padding` = 锻造轨道 `(10,0,0,0)` ⇒ `V − pad` 的左沿 = 330.968 + 10。
                        //    ⛔ 别把它写回 `trackR.x1` —— 那是 pad 缩在命中区上的错模型（实测差 10px）。
                        CheckNear(qx1, trackR.x1 + 10f, 0.5f,
                                  "它的命中区**左边缘被截到 `V.x1 + padL = 340.97`**"
                                  + "（`RectMask2D.m_Padding` 缩的是 mask 自己那个框，不是命中区自己的矩形）");
                        CheckTrue(qx2 - qx1 < 200.762f - 1f,
                                  $"…渲出来的宽 {qx2 - qx1:F1} **比原版整块 200.762 窄**（真被截了，不是整块）");
                        var wbEdge = claimEdge.GetComponent<WindowButton>();
                        CheckTrue(layerV != null && layerV.ButtonAt((qx1 + qx2) * 0.5f, (qy1 + qy2) * 0.5f) == wbEdge,
                                  "…截剩下的那半截**仍然点得中**（视口里的点击面还在）");
                        CheckTrue(layerV == null || layerV.ButtonAt(qx1 - 20f, (qy1 + qy2) * 0.5f) != wbEdge,
                                  "…而**视口外**那半截点不中它（原版 `RectMask2D` 的射线那一面）");
                    }

                    // ---- ③ 同一格：`Reward` 图标的矩形与 **uv 一起**被截（只截矩形不截 uv 会把图压扁）----
                    var rwEdge = FindChild(cellEdge, "Reward");
                    var rwQ = rwEdge != null ? rwEdge.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(rwQ != null && rwQ.Texture != null, "那一格的 `Reward` 图标量得到（有图）");
                    if (rwQ != null && rwQ.Texture != null)
                    {
                        // 整块宽 = 框高 150（`ForgeTab.BuildRewardIcon` 里那个高度，⚠️ 那一处是「我们挑的」）
                        //          × 图自身的宽高比（`keepAspect` 那条路）
                        float fullW = 150f * ((float)rwQ.Texture.width / Mathf.Max(1f, rwQ.Texture.height));
                        CheckTrue(rwQ.WorldW * 108f < fullW - 1f,
                                  $"压在视口边上的那一格：`Reward` 渲出来的宽 **{rwQ.WorldW * 108f:F1} < 整块 {fullW:F1}**（真被截了）");
                        CheckTrue(rwQ.UvRect.width < 0.999f,
                                  $"…而且 **uv 跟着截**（`UvRect.width = {rwQ.UvRect.width:F3} < 1`）");
                    }
                    // 对照：完全落在视口里的那一格 ⇒ 整块宽、uv = 整张
                    var rwIn = FindChild(FindChild(fcontent, "ForgeCell_" + (lvV + 1)), "Reward");
                    var rwInQ = rwIn != null ? rwIn.GetComponentInChildren<ImageQuad>() : null;
                    if (rwInQ != null && rwInQ.Texture != null)
                    {
                        float fullW2 = 150f * ((float)rwInQ.Texture.width / Mathf.Max(1f, rwInQ.Texture.height));
                        CheckNear(rwInQ.WorldW * 108f, fullW2, 1f,
                                  "对照：完全落在视口里的那一格 ⇒ `Reward` **整块宽**（没被截）");
                        CheckNear(rwInQ.UvRect.width, 1f, 0.001f, "…且 uv 是整张（`width = 1`）");
                    }

                    // ---- ④ 🔴 **2026-10-13（A435 甲）：迁移后【改写】**（旧写法是「真毒框」，迁完会**静默空转**）----
                    // 旧写法 = 「把毒值下在**窗字段** `win.Clip` 上 ⇒ `Refresh()` 必须把它**原样还原**」。
                    // 迁到「裁切状态长在**视口节点**上」之后本窗**一次都不写那三个字段** ⇒ `win.Clip` 恒 `null`
                    // ⇒ 旧断言那句 `win.Clip.HasValue && …` **恒假**（红），而 `CheckTrue(win.Clip == null)` 恒真
                    // —— 两条都断不出东西。换成两条**带电**的（判据 = `Shell/ViewportClip.cs` 的契约）：
                    //   ① **窗级三件套全程干净**（本窗不再把状态搬来搬去 ⇒ 也就没有「还原」这回事）；
                    //   ② **视口节点上那两个字段是【常驻】状态**：毒下在**节点**上、跑一趟 `Refresh()`、逐位比
                    //      —— 生产代码**一个字节都不许改它们**（照 `H33` §B5 的「带电写法」）。
                    CheckTrue(win.Clip == null && win.ClipPad == Vector4.zero && win.ClipSoftness == Vector2.zero,
                              "`Refresh()`（内含 `BuildRewardCells`）跑完 ⇒ **窗级三件套全程干净**"
                            + "（A435 迁移后锻造页不再写 `Clip`/`ClipPad`/`ClipSoftness` —— 状态长在视口节点上）"
                            + "｜改坏法：把 `Shell/ForgeTab.cs` 的 `BuildRewardCells` 里那三行 `_win.Clip…` 加回去 ⇒ 红");
                    var ftTrackVp = FindPath(forge, "Rewards Scroll View/Viewport");
                    var ftVc = ftTrackVp != null ? ftTrackVp.GetComponent<ViewportClip>() : null;
                    CheckTrue(ftVc != null,
                              "（前提）锻造轨道那颗视口节点（`Rewards Scroll View/Viewport`）挂着 `ViewportClip`"
                            + " —— 这是 A435 迁移的落点；⛔ 没挂上 ⇒ 下面两条没意义");
                    if (ftVc != null)
                    {
                        // 期望值是**原版实读字面量**（`…/Forge Tab/Rewards Scroll View/Viewport` 的
                        // `m_Padding = (10,0,0,0)` · `m_Softness = (0,0)`；出处 → `WindowsManager` 的 `ClipSoftness`
                        // 那段表）—— ⛔ 不读 `ForgeTab.TrackPad`（那是**被测实现**上的常量 = 自证）。
                        CheckTrue(ftVc.padding == new Vector4(10f, 0f, 0f, 0f) && ftVc.softness == Vector2Int.zero,
                                  $"★ 那颗节点上写的是**原版那两个值**（实测 pad=({ftVc.padding.x},{ftVc.padding.y},"
                                + $"{ftVc.padding.z},{ftVc.padding.w}) · soft=({ftVc.softness.x},{ftVc.softness.y})；"
                                + "期望 **pad=(10,0,0,0) · soft=(0,0)**）"
                                + "｜改坏法：把 `Shell/ForgeTab.cs` 里 `ViewportClip.Hang(…)` 那两个实参改掉 ⇒ 红");
                        var ftPad0 = ftVc.padding; var ftSoft0 = ftVc.softness;
                        var ftPadPoison = new Vector4(40f, 40f, 40f, 40f);
                        var ftSoftPoison = new Vector2Int(10000, 10000);
                        ftVc.padding = ftPadPoison; ftVc.softness = ftSoftPoison;   // 毒下在【节点】上
                        if (ftV != null) ftV.Refresh();
                        CheckTrue(ftVc.padding == ftPadPoison && ftVc.softness == ftSoftPoison,
                                  "★★ …`Refresh()`（内含 `BuildRewardCells`）**一个字段都不许动那颗节点**"
                                + " —— 节点那两个字段是【常驻】状态（原版 `RectMask2D` 就长在视口节点上），"
                                + "不是每次重建时被生产代码搬来搬去、再搬回去的东西"
                                + "｜改坏法：把 `BuildRewardCells` 里那句 `_win.ClipPad = TrackPad;` 改写成"
                                + " `vc.padding = …` ⇒ 节点被改掉 ⇒ 红");
                        ftVc.padding = ftPad0; ftVc.softness = ftSoft0;             // 收尾复位（后面几段还要用这条视口）
                    }
                    win.Clip = null;
                    if (ftV != null) ftV.SelectArmy(ForgeData.Selected);   // 还原轨道/阵营条的起手定位
                }
            }
        }

        // ============================================================ §三·b4 `Clip` 路（`MenuDraw` / `MenuWindowBase` 那一层）
        //
        // 🔴 三条都**只用临时节点**验 `MenuDraw`/`MenuWindowBase` 已落地的 `ClipRect` / `Nine(…,clip)` /
        //    `Hit(…,clip)`：判据 = 原版 `RectMask2D`（UGUI 源码出处写在 `MenuDraw.ClipRect` 的注释里）。
        //    ⚠️ 批处理下 `Object.Destroy` 不生效 ⇒ 一律 `DestroyImmediate`；`win.Clip` 用完**必须还原**。
        Section("§三·b4-a `MenuDraw.Visible` 的判据表（求交那一份，五格）—— 🔴 2026-10-10 订正（A194）：标题原写 `MenuDraw.ClipRect`，收口后唯一一份是 `Visible`");
        {
            var r = new PxRect(100f, 100f, 200f, 200f);
            var clip = new PxRect(150f, 150f, 300f, 300f);
            PxRect o;
            // ① `clip = null` ⇒ true 且**逐字段原样返回**
            CheckTrue(MenuDraw.ClipRect(r, null, out o) && MenuDraw.SameRect(o, r),
                      "① `clip = null` ⇒ true 且 `outRect` **逐字段 = r**（没被截）");
            // ② 整块在框内 ⇒ 同上
            CheckTrue(MenuDraw.ClipRect(r, new PxRect(50f, 50f, 400f, 400f), out o) && MenuDraw.SameRect(o, r),
                      "② 整块在框内 ⇒ true 且 `outRect` **逐字段 = r**");
            // ③ 整块在框外（x 出 / y 出各一条）⇒ false
            CheckTrue(!MenuDraw.ClipRect(r, new PxRect(210f, 0f, 400f, 400f), out o),
                      "③a 整块在框外（**x 出**）⇒ false（= 调用方「不建」）");
            CheckTrue(!MenuDraw.ClipRect(r, new PxRect(0f, 210f, 400f, 400f), out o),
                      "③b 整块在框外（**y 出**）⇒ false");
            // ④ 压着框边 ⇒ out = **交集**
            // 🔴 **2026-10-03 就地更正（铁律 5）**：这一条原来写的是
            //    `o.x1 == r.x1 && o.x2 == clip.x2 && o.y1 == r.y1 && o.y2 == clip.y2` —— **那是错的**，
            //    它描述的是「**clip 只切掉 r 的右下那一小块**」那种特例（要求 r 的左上角落在框内），
            //    而本格的这组数（见 `:1343-1344`）是 `r=(100,100,200,200)` / `clip=(150,150,300,300)`
            //    ⇒ **框切掉的是 r 的左上那一半**，交集的四条边**全都要取 max/min**。
            //    实测 150,150 → 200,200 正是交集（= `ClipRect` 的实现是对的，**断言写错了**）。
            //    ⇒ 期望值改成**字面量**（**不在这里重写一遍 Max/Min** —— 拿同一个公式去验同一个公式 = 自证）。
            bool ok4 = MenuDraw.ClipRect(r, clip, out o);
            CheckTrue(ok4, "④ 压着框边 ⇒ true");
            CheckTrue(ok4 && o.x1 == 150f && o.y1 == 150f && o.x2 == 200f && o.y2 == 200f,
                      $"④ …`outRect` = **交集**（`r` ∩ `clip`；期望 **150,150 → 200,200**，"
                      + $"实测 {o.x1:F0},{o.y1:F0} → {o.x2:F0},{o.y2:F0}）");
            // ④b 另一侧：**框切掉 r 的右下那一半** ⇒ 交集落在左上（原来那条断言想描述的其实是这一格）
            bool ok4b = MenuDraw.ClipRect(r, new PxRect(0f, 0f, 150f, 150f), out o);
            CheckTrue(ok4b, "④b 压着框的右下边 ⇒ true");
            CheckTrue(ok4b && o.x1 == 100f && o.y1 == 100f && o.x2 == 150f && o.y2 == 150f,
                      $"④b …`outRect` = **交集**（期望 **100,100 → 150,150**，"
                      + $"实测 {o.x1:F0},{o.y1:F0} → {o.x2:F0},{o.y2:F0}）");
            // ⑤ 有裁切 + 退化矩形 ⇒ false（`Rect` 原来就有的行为，别丢）
            CheckTrue(!MenuDraw.ClipRect(new PxRect(100f, 100f, 100f, 200f), clip, out o),
                      "⑤ **有裁切** + 宽 ≤ 0.01 的退化矩形 ⇒ false");
        }

        // ---------------- ⑤·a-1 🆕 **A464 · B1 / A489**：取裁切状态的两条可观测不变量（**分两个时点**）
        //
        // 🔴 **2026-10-13（A435 甲）改写（铁律 5 · 口径变了）**：这里原来断的是
        //   `NodeResolutions == 0 && NodeShadowedByParam == 0` —— 那是**阶段 1 的共存保证**
        //   （「全仓不挂节点 ⇒ 取裁切状态一次都不走父链」）。**阶段 2（本批）起它作废**：
        //   本场景自己就挂了 6 颗节点（`Forge Tab` 2 · `Campaign Tab` 2 · `CampaignRewardWindow` 1 ·
        //   `RewardWindow` 1……）⇒ `== 0` 必红。按迁移表 §四 **档 2（改写）** 拆成两个时点：
        //     · **迁移中**（本批：甲块落地、乙/丙未做）= 断 **`NodeResolutions > 0`**
        //       —— 带电写法：节点一旦没挂上（或 `Resolve` 第 2 支被改坏）⇒ 立刻红；
        //     · **迁移完**（A 表 32 站点 + B 表 11 守卫口全部收编之后）= 断 **`NodeShadowedByParam == 0`**
        //       —— 「节点挂着、却一个像素都没生效」= **旧设站点没删干净**的唯一痕迹（静默）。
        //   🔴 **为什么不写成 `>= 0`**：恒真（= 什么都没断）。
        //   🔴 **为什么它在【迁移进行中】非 0 也可能是正常的**：`NodeShadowedByParam` **不区分**
        //     「漏删旧设站点」与「**合法地**显式传了同一个视口」（本文件那几处**故意下毒**的探针、
        //     `MenuDraw.Rect` 的 `clip` 形参走的就是后一类）⇒ 它是**进度指标、不是缺陷计数**
        //     （出处 → `Shell/ViewportClip.cs` 里那条字段的 doc）。⚠️ **红了先分类**：看那个 +1 是
        //     不是来自**本文件的探针**（合法的），再看是不是某个 `*_win.Clip = …` 的旧设站点没删。
        //   ⚠️ **它只能在【本段位置】断**：两个计数都是**进程级静态量**、只增不减，而本文件后半段
        //     （§十 那几处夹具）**故意**造「形参盖住节点」⇒ 那之后读到的数已经不代表迁移进度了。
        Section("★ A464·B1 / A489 取裁切状态：节点态**真的被走到**（迁移中）· 旧设站点不再盖住节点（迁移完）");
        {
            CheckTrue(ViewportClip.NodeResolutions > 0,
                      $"★ A464·B1（**迁移中**）：本场景已经挂上视口节点、取状态**真的走过节点态** "
                    + $"{ViewportClip.NodeResolutions} 次"
                    + " —— 改坏法：把任一 `ViewportClip.Hang(…)` 改回 `Node(…)`、或让 `Resolve` 第 2 支"
                    + "恒不命中 ⇒ 这个数回 0 ⇒ **立刻红**"
                    + "（⛔ 只断「结果非空」没有牙口 —— 形参那一支也非空）");
            Check(ViewportClip.NodeShadowedByParam, 0,
                  "★ A489（**迁移完**）：本场景里**不该再有「父链上有节点、却被非空形参盖住」的地方**"
                + $"（实测 {ViewportClip.NodeShadowedByParam} 次）"
                + " —— 那种状态 = **旧设站点没删干净**：节点挂着、却一个像素都没生效，"
                + "行为照旧、也不出声（静默），只有这个计数看得见"
                + "｜改坏法：把 `Shell/CampaignTab.cs` 的 `BuildTrack()` 里那三行 `_win.Clip…` 加回去 ⇒ +1");
        }

        Section("§三·b4-b 九宫格**真吃** `Clip`（左栏高亮那张现成参数：`40k_main_bt_selected_BW` 71² · border 30 · ppuMul 0.92）");
        {
            // 🔴 判据 = 原版 `Image.Type = Sliced` + `m_PixelsPerUnitMultiplier = 0.92`
            //    ⇒ **画出来的角块 = 30 ÷ 0.92 = 32.61 画布像素**（`borderOutPx` 那个参数就是它）。
            //    这一节同时验一个**独立疑点**：`ImageQuad.PixelsPerUnit = 100` 而画布是 **108 px/世界单位**
            //    ⇒ 九宫格有可能**整体大 8%**（角块 32.61 → 35.22）。**本件只写断言、不改实现** —— 红了就是发现。
            var nineTex = win.Art("40k_main_bt_selected_BW");
            CheckTrue(nineTex != null, "`40k_main_bt_selected_BW` 取得到（左栏高亮那张）");
            CheckTrue(nineTex != null && nineTex.width == 71 && nineTex.height == 71,
                      $"那张图是 **71×71**（原版 `m_Rect`；实测 {(nineTex != null ? nineTex.width + "×" + nineTex.height : "?")}）");
            CheckTrue(win.Clip == null, "画基准那块之前 `win.Clip` 是干净的（否则基准本身也会被裁）");
            var tmpN = RewardsWindow.New(win.transform, "ClipNineTest");
            const float Corner = 30f / 0.92f;                 // 32.6087 画布像素
            float side = 3f * Corner;                          // **取 3 倍角块长** ⇒ 九块理论上都是同一个数
            var nr = new PxRect(100f, 100f, 100f + side, 100f + side);
            var nclip = new PxRect(100f, 100f + 10f, 100f + side, 100f + side);   // 切掉**顶边** 10px（画布 y 向下 ⇒ y1 是顶边）
            var border = new Vector4(30f, 30f, 30f, 30f);
            var bordOut = new Vector4(Corner, Corner, Corner, Corner);
            var baseRoot = MenuDraw.Nine(tmpN, nineTex, nr, border, 71f, 71f, 3005, null, true, "Highlight", bordOut, null);
            var clipRoot = MenuDraw.Nine(tmpN, nineTex, nr, border, 71f, 71f, 3005, null, true, "HighlightC", bordOut, nclip);
            CheckTrue(baseRoot != null && clipRoot != null, "基准块与「顶边切 10px」那块都建出来了");
            if (baseRoot != null && clipRoot != null)
            {
                var baseQs = baseRoot.GetComponentsInChildren<ImageQuad>(true);
                var clipQs = clipRoot.GetComponentsInChildren<ImageQuad>(true);
                // ① 子块个数相同（**含被关掉的一起数 = 9**）
                Check(baseQs.Length, 9, "① 基准块：**含被关掉的一起数 = 9 块**（`Highlight_00 .. _22`）");
                Check(clipQs.Length, 9, "① 被切那块：**子块个数与基准相同 = 9**（整块在框外的只 `SetActive(false)`，**不删节点**）");
                // ② 🔴 **重点**：基准那块**所有子块渲出来的高都是 32.61**（= 角块长）
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        var t = baseRoot.transform.Find("Highlight_" + i + j);
                        var q = t != null ? t.GetComponent<ImageQuad>() : null;
                        CheckNear(q != null ? q.WorldH * 108f : -1f, Corner, 0.5f,
                                  $"② 基准块 `Highlight_{i}{j}` 渲出来的高(px) = **{Corner:F2}**"
                                  + "（= 原版 `m_Border 30` ÷ `ppuMul 0.92`）");
                    }
                // ③ 被切那块：**只有最上面那三块**（`_02/_12/_22`，j=2 是最上面那条）变矮，且 = 32.61 − 10
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        var t = clipRoot.transform.Find("HighlightC_" + i + j);
                        var q = t != null ? t.GetComponent<ImageQuad>() : null;
                        CheckNear(q != null ? q.WorldH * 108f : -1f, j == 2 ? Corner - 10f : Corner, 0.5f,
                                  $"③ 被切那块 `HighlightC_{i}{j}` 渲出来的高(px) = **{(j == 2 ? Corner - 10f : Corner):F2}**"
                                  + (j == 2 ? "（顶边被切掉 10px）" : "（没压到框边 ⇒ 不动）"));
                    }
                // ④ 那三块的 uv：`height = (30/71) × (22.61/32.61)`、**`y` 不变**（截的是上边 ⇒ v 的下沿不动）
                for (int i = 0; i < 3; i++)
                {
                    var tb = clipRoot.transform.Find("HighlightC_" + i + "2");
                    var ta = baseRoot.transform.Find("Highlight_" + i + "2");
                    var qb = tb != null ? tb.GetComponent<ImageQuad>() : null;
                    var qa = ta != null ? ta.GetComponent<ImageQuad>() : null;
                    CheckNear(qb != null ? qb.UvRect.height : -1f, (30f / 71f) * ((Corner - 10f) / Corner), 0.005f,
                              $"④ `HighlightC_{i}2` 的 `UvRect.height` = (30/71)×(22.61/32.61)（uv **跟着截**，否则图会被压扁）");
                    CheckNear(qb != null ? qb.UvRect.y : -1f, qa != null ? qa.UvRect.y : -2f, 1e-6f,
                              $"④ …`UvRect.y` 与基准**完全不变**（截掉的是上边 ⇒ v 的下沿不动）");
                }
                // ⑤ 其余子块 `UvRect` 与基准**逐字段相同**
                // 🔴 **2026-10-03 就地更正（铁律 5）**：这一条原来是**逐字段 `!=` 精确比 + 数「差了几块」**，
                //    实测红（6 块里报 4 块）。**判清了：错的是断言，不是 `ClipNineChildren`。**
                //    原因：九宫格子块的 uv 是**「画布 px → 世界坐标 → 画布 px」来回换算**出来的
                //    （`MenuDraw.ClipNineChildren:187-189` 拿 `transform.position` 与 `WorldW/WorldH` 反推
                //    `qr`，再交给 `ClipRect`），float32 在这个量级（~197 px 处 ulp = **1.5e-5 px**）
                //    分辨不到「贴着框边」和「差一丁点」的区别：
                //      · 左列 `_00`/`_01` 的 `qr.x1` 算成 **99.99997**（框左边 100）⇒ 落在框外 3.05e-5 px；
                //      · 底排 `_10`/`_20` 的 `qr.y2` 算成 **197.82611**（框底 197.82608）⇒ 同样差 3.05e-5 px；
                //    ⇒ `SameRect(cr, qr)` 判「压边了」，这 4 块被按「部分越界」处理：uv 各截掉
                //    **3.9e-7**（几何 3.05e-5 px）。**中间那块 `_11` 与 `_21` 一动没动** ——
                //    被碰到的**只有本来就压着框边的那几块**，幅度比 1 个像素小 5 个数量级。
                //    ⇒ **实现没有错**（`ClipNineChildren` 的语义就是「压边 ⇒ 截」，它只是把「浮点意义下的压边」
                //    也算进去了）；**错的判据是「浮点精确相等」**。
                //    （对照：`ClipRect` 那条「整块在框内 ⇒ 原样返回 `r` 的**同一个 struct**」是**精确**的，
                //      所以上一节 §三·b4-a 那几条 `SameRect(ClipRect 的产物, 原矩形)` 不受影响 ——
                //      只有 `ClipNineChildren` 里这个**重算出来的** `qr` 才有这个陷阱。）
                // 判据改成**带容差**（`CheckNear` 口径）：`1e-5`（uv 单位）。
                //   容差出处：本块 uv 的 30/71 ≈ 0.4225 ↔ 画布 **32.61 px** ⇒ **1 uv ≈ 77 px**，
                //   故 **1e-5 uv ≈ 7.7e-4 画布像素**；而**真出错哪怕只错 1 px 也是 1.3e-2 uv（差 1300 倍）**
                //   ⇒ 该容差**挡得住真缺陷**，不是「放宽到永远绿」。（实测最大差 3.95e-7，余量 25 倍。）
                // ⚠️ 缺块（`Find` 不到）仍算失败 —— 取 `+∞` ⇒ 必红，不是静默放过。
                float uvMax = 0f;
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        if (j == 2) continue;
                        var tc = clipRoot.transform.Find("HighlightC_" + i + j);
                        var tb2 = baseRoot.transform.Find("Highlight_" + i + j);
                        var qc = tc != null ? tc.GetComponent<ImageQuad>() : null;
                        var qb2 = tb2 != null ? tb2.GetComponent<ImageQuad>() : null;
                        if (qc == null || qb2 == null) { uvMax = float.PositiveInfinity; continue; }
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.x - qb2.UvRect.x));
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.y - qb2.UvRect.y));
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.width - qb2.UvRect.width));
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.height - qb2.UvRect.height));
                    }
                CheckNear(uvMax, 0f, 1e-5f,
                          $"⑤ 其余 6 块（j=0/1 那两排）的 `UvRect` 与基准**逐字段相同**"
                          + $"（容差 1e-5 uv ≈ 7.7e-4 px；实测最大差 {uvMax:E2}；"
                          + "v 方向差 1 px 会是 1.3e-2）");
                // ⑥ 根节点位置不动（原版 `RectMask2D` 也只裁渲染、不挪 `RectTransform`）
                // 🔴 **2026-10-11（A304）：这一条是全仓【最紧】的一条容差 —— 两侧谁都不许加 z。**
                //    容差 `1e-5` 世界单位 = **0.001 px**，而它比的是**两个不同节点**的 `transform.position`
                //    （不是自比较 ⇒ z 不会互相抵消）。全仓「给节点加 z」的写法有几十处
                //    （`Battle` 那一族 `HudImageZ / Z = ±0.01…0.3`、`Shell/DeckInfoPopup.cs:966` 的命中区等），
                //    任何一个 `+0.01` 的 z 前移 = **1.08 px = 容差的 1080 倍** ⇒ **这里立刻红**。
                //    ⇒ 这两块（`baseRoot` / `clipRoot`）都走 `MenuDraw.Nine`（无 z 写入点），**保持住**；
                //      ⛔ 谁要在这两块任何一侧加 z，**先来把这条容差改成 2D（`new Vector2(…).magnitude`）**，
                //      别直接加 —— 加完这条会红，而红的原因从断言文案上看不出来。
                //    （判据汇总 → `资料/普查产出_1011/V7_A305_A304_普查.md` §4·1 #21。）
                CheckNear(Vector3.Distance(baseRoot.transform.position, clipRoot.transform.position), 0f, 1e-5f,
                          "⑥ **被切那块的根节点位置 == 基准那块根节点位置**（「根不动」）");
            }
            // ⑦ 整块落在 `clip` 外 ⇒ 返回 null（连节点都不建）
            var offRoot = MenuDraw.Nine(tmpN, nineTex, new PxRect(1000f, 1000f, 1000f + side, 1000f + side),
                                        border, 71f, 71f, 3005, null, true, "OffNine", bordOut, nclip);
            CheckTrue(offRoot == null, "⑦ 整块落在 `clip` 外 ⇒ **返回 null**（连节点都不建）");
            // ⑧ 走 `win.Nine(…)` 包装再来一遍 ⇒ 与 ③④ 同值（证明包装层把 `Clip` 传下去了）
            var keepClip = win.Clip;
            win.Clip = nclip;
            var wrapRoot = win.Nine(tmpN, "40k_main_bt_selected_BW", nr, border, 3005, null, true, "Wrapped", bordOut);
            win.Clip = keepClip;
            CheckTrue(wrapRoot != null, "⑧ `win.Nine(…)`（包装层）在 `Clip` 生效时也建出来了");
            if (wrapRoot != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var t = wrapRoot.transform.Find("Wrapped_" + i + "2");
                    var q = t != null ? t.GetComponent<ImageQuad>() : null;
                    CheckNear(q != null ? q.WorldH * 108f : -1f, Corner - 10f, 0.5f,
                              $"⑧ 包装层那条路 `Wrapped_{i}2` 的高(px) 与 ③ 同值（= {Corner:F2} − 10）");
                    CheckNear(q != null ? q.UvRect.height : -1f, (30f / 71f) * ((Corner - 10f) / Corner), 0.005f,
                              $"⑧ …`UvRect.height` 与 ④ 同值（`Clip` 确实传进 `MenuDraw.Nine` 了）");
                }
            }
            Object.DestroyImmediate(tmpN.gameObject);
        }

        Section("§三·b4-c 点击区守卫（`MenuDraw.Hit` / `MenuWindowBase.AddHit` 吃 `Clip`）");
        {
            var tmpH = RewardsWindow.New(win.transform, "ClipHitTest");
            var layerH = PointerLayer.Instance;
            CheckTrue(win.Clip == null, "起手 `win.Clip` 是干净的");
            // ① 回归：`Clip == null` ⇒ 命中区是**整块**
            // ⚠️ 临时矩形**以 (5,5) 为中心** —— 同文件上面已证这里打不中别的按钮 ⇒ 下面「点不中」才有意义。
            var hitR = new PxRect(-45f, -45f, 55f, 55f);
            var hA = win.AddHit(tmpH, "H1", hitR, 3005, null);
            var qA = hA != null ? hA.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(qA != null, "① `win.AddHit` 建出了命中区（带 quad）");
            if (qA != null)
            {
                CheckNear(qA.WorldW * 108f, hitR.W, 0.5f, "① `Clip == null` ⇒ 命中区**整块宽**（100px）");
                CheckNear(qA.WorldH * 108f, hitR.H, 0.5f, "① …整块高（100px）");
                CheckTrue(layerH != null && layerH.ButtonAt(5f, 5f) == hA.GetComponent<WindowButton>(),
                          "① 而且这一点**真的打到它**（= 这一点上没有别人，下面「点不中」才说明问题）");
            }
            MenuDraw.ClearChildren(tmpH);                 // 批处理下必须立刻清掉（`ClearChildren` 走 `DestroyImmediate`）
            // ② 整块在框外 ⇒ 返回 null + 节点不建 + 打不中
            win.Clip = new PxRect(500f, 500f, 600f, 600f);
            var hB = win.AddHit(tmpH, "H2", hitR, 3005, null);
            CheckTrue(hB == null, "② 整块在视口外 ⇒ `AddHit` **返回 null**");
            CheckTrue(FindChild(tmpH, "H2") == null, "② …**连节点都不建**");
            CheckTrue(layerH == null || layerH.ButtonAt(5f, 5f) == null, "② …那一点**点不中任何按钮**");
            win.Clip = null;
            MenuDraw.ClearChildren(tmpH);
            // ③ 压右边：只切掉右半 ⇒ 命中区 = 剩下一半；框内那半点得中、框外那半点不中
            var hr3 = new PxRect(100f, 100f, 200f, 200f);
            var hc3 = new PxRect(100f, 100f, 150f, 200f);
            PxRect cr3;
            CheckTrue(MenuDraw.ClipRect(hr3, hc3, out cr3), "③ 现算：`ClipRect` 给出的是**左半块**");
            win.Clip = hc3;
            var hC = win.AddHit(tmpH, "H3", hr3, 3005, null);
            var qC = hC != null ? hC.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(qC != null, "③ `AddHit` 建出了**截过**的命中区");
            if (qC != null)
            {
                CheckNear(qC.WorldW * 108f, hr3.W * 0.5f, 0.5f, "③ 命中区宽 = **剩下一半**（100 → 50px）");
                var wbC = hC.GetComponent<WindowButton>();
                CheckTrue(layerH != null && layerH.ButtonAt(125f, 150f) == wbC, "③ **框内那半点得中**它");
                CheckTrue(layerH == null || layerH.ButtonAt(175f, 150f) != wbC, "③ **框外那半点不中**它");
                // ⑤ 顺带：`Hit` 用的就是 `ClipRect` 那条判据（渲染矩形逐字段对得上）
                float bx = PxOf(qC.transform.position.x), by = PxYOf(qC.transform.position.y);
                float hw = qC.WorldW * 108f * 0.5f, hh = qC.WorldH * 108f * 0.5f;
                CheckTrue(Mathf.Abs((bx - hw) - cr3.x1) <= 0.5f && Mathf.Abs((bx + hw) - cr3.x2) <= 0.5f
                          && Mathf.Abs((by - hh) - cr3.y1) <= 0.5f && Mathf.Abs((by + hh) - cr3.y2) <= 0.5f,
                          $"⑤ 命中区的渲染矩形 == `MenuDraw.ClipRect` 算出来的那块（{(bx - hw):F1},{by - hh:F1} → "
                          + $"{(bx + hw):F1},{by + hh:F1}）—— **同一判据，不是第二份**");
            }
            win.Clip = null;
            MenuDraw.ClearChildren(tmpH);
            // ④ 只切上下：同理换 y
            var hr4 = new PxRect(100f, 600f, 200f, 700f);
            var hc4 = new PxRect(100f, 600f, 200f, 650f);
            win.Clip = hc4;
            var hD = win.AddHit(tmpH, "H4", hr4, 3005, null);
            var qD = hD != null ? hD.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(qD != null, "④ 只切上下时 `AddHit` 也建出了截过的命中区");
            if (qD != null)
            {
                CheckNear(qD.WorldH * 108f, hr4.H * 0.5f, 0.5f, "④ 命中区高 = **剩下一半**（100 → 50px）");
                var wbD = hD.GetComponent<WindowButton>();
                CheckTrue(layerH != null && layerH.ButtonAt(150f, 625f) == wbD, "④ **框内那半点得中**它（换 y 同理）");
                CheckTrue(layerH == null || layerH.ButtonAt(150f, 675f) != wbD, "④ **框外那半点不中**它");
            }
            win.Clip = null;
            Object.DestroyImmediate(tmpH.gameObject);
        }

        // ============================================================ §三·b4-d 软边遮罩 + 文字部分越界
        // 判据两条（都在 `MenuDraw` 的注释里写了出处）：
        //   · 软边 = 原版 `RectMask2D.m_Softness`（`Runtime/UGUI/UI/Core/RectMask2D.cs:71,284-301`）——
        //     边框内 `softness` 像素宽的**线性渐隐带**（x 管左右、y 管上下）；0 = 硬边（= 我们原来那套）。
        //   · 文字 = 同一个掩码对文字与图**一视同仁** ⇒ 压在框边上的字要**切半个**（不是整块照画）。
        // 🔴 这几条**能真红**：期望值全是「原版字面量 + 手算的几何」，不是我们的常量；
        //    把 `ApplySoftEdges` 的切开那几行、或 `ClipText` 的夹顶点那几行拆掉，下面立刻红。

        Section("§三·b4-d-1 `MenuDraw.Visible`（A25④ 收口：四处内联「整块在框外就不建」的唯一实现）");
        {
            CheckTrue(!MenuDraw.Visible(new PxRect(500f, 500f, 600f, 600f), new PxRect(0f, 0f, 100f, 100f)),
                      "① 整块在框外 ⇒ **false**（调用方一律不建）");
            CheckTrue(MenuDraw.Visible(new PxRect(0f, 0f, 50f, 50f), new PxRect(0f, 0f, 100f, 100f)),
                      "② 整块在框内 ⇒ true");
            CheckTrue(MenuDraw.Visible(new PxRect(90f, 90f, 150f, 150f), new PxRect(0f, 0f, 100f, 100f)),
                      "③ 压着框边（部分可见）⇒ true（**不是**「整块在框内」）");
            CheckTrue(MenuDraw.Visible(new PxRect(500f, 500f, 600f, 600f), null),
                      "④ **没有裁切 ⇒ 一律 true**（= 四处内联里 `!clip.HasValue ||` 那半句，别丢）");
            CheckTrue(MenuDraw.Visible(new PxRect(50f, 50f, 50f, 200f), new PxRect(0f, 0f, 100f, 300f)),
                      "⑤ **退化矩形（宽 0）不判不可见** —— 这是它与 `ClipRect` 的**唯一**差别"
                      + "（`ClipRect` 那条守卫是给建不出 quad 的图用的；文字/节点建得出来，收口不许顺手改行为）");
            CheckTrue(!MenuDraw.ClipRect(new PxRect(50f, 50f, 50f, 200f), new PxRect(0f, 0f, 100f, 300f), out _),
                      "⑤b 对照：`ClipRect` 对同一个退化矩形给的是 **false**（两者确实不是一回事）");
        }

        Section("§三·b4-d-2 软边（`ClipSoftness`）：0 时一字不改 · 非 0 时按带宽切开 + 逐角 alpha 斜坡");
        {
            var tmpS = RewardsWindow.New(win.transform, "SoftEdgeTest");
            const string art = "40k_main_bt_selected_BW";
            CheckTrue(win.Art(art) != null, "探针用的图取得到（`" + art + "`）");
            // 框 500×200；被切的那块从 y 250 伸到 350（框底 300）⇒ 硬裁后剩 [250,300]
            var clipS = new PxRect(400f, 100f, 900f, 300f);
            var rS = new PxRect(450f, 250f, 550f, 350f);
            win.Clip = clipS;

            // ---- ① 回归：软边 = 0 ⇒ 与改之前**逐字段相同**（单块、没有顶点色、没有子块）----
            win.ClipSoftness = new Vector2(0f, 0f);
            var qA = win.Rect(tmpS, art, rS.x1, rS.x2, rS.y1, rS.y2, "Hard", 3005, null, false);
            CheckTrue(qA != null, "① 硬边那条建出来了");
            if (qA != null)
            {
                CheckNear(qA.transform.childCount, 0f, 0f, "① 软边 0 ⇒ **不切**（没有子块）");
                CheckTrue(qA.CornerColors == null, "① …而且**一个顶点色都没设**（软边 0 这条路完全不动顶点色）");
                CheckNear(qA.WorldH * 108f, 50f, 0.5f, "① 硬裁后高 = **50px**（250→300）");
                CheckNear(Mathf.Abs(qA.UvRect.height - 0.5f), 0f, 0.005f, "① …uv 跟着截（0.5）");
            }
            MenuDraw.ClearChildren(tmpS);

            // ---- ② 软边 (0,25)（**商店 `Packs Scroll View/Viewport` 的原版真值**）----
            //     带宽内沿 = 框底 300 − 25 = **275** ⇒ 应切成 [250,275]（平）与 [275,300]（斜坡）
            win.ClipSoftness = new Vector2(0f, 25f);
            var qB = win.Rect(tmpS, art, rS.x1, rS.x2, rS.y1, rS.y2, "Soft", 3005, null, false);
            CheckTrue(qB != null, "② 软边那条建出来了");
            if (qB != null)
            {
                var pieces = new List<ImageQuad>(qB.GetComponentsInChildren<ImageQuad>(true));
                Check(pieces.Count, 2, "② 切成 **2 块**（带宽内沿 275 落在块内 ⇒ 各带一段斜率）");
                float ux1 = float.MaxValue, ux2 = float.MinValue, uy1 = float.MaxValue, uy2 = float.MinValue;
                float ampAtEdge = -1f, ampAtFlat = -1f;
                ImageQuad flat = null, band = null;
                for (int i = 0; i < pieces.Count; i++)
                {
                    var q = pieces[i];
                    float cx = PxOf(q.transform.position.x), cy = PxYOf(q.transform.position.y);
                    float hw = q.WorldW * 108f * 0.5f, hh = q.WorldH * 108f * 0.5f;
                    ux1 = Mathf.Min(ux1, cx - hw); ux2 = Mathf.Max(ux2, cx + hw);
                    uy1 = Mathf.Min(uy1, cy - hh); uy2 = Mathf.Max(uy2, cy + hh);
                    if (cy - hh >= 274.5f) band = q; else flat = q;
                }
                CheckNear(ux1, 450f, 0.5f, "② 所有块并集的左边 = 硬裁后的 450（没多出、也没少）");
                CheckNear(ux2, 550f, 0.5f, "② …右边 = 550");
                CheckNear(uy1, 250f, 0.5f, "② …上边 = 250");
                CheckNear(uy2, 300f, 0.5f, "② …下边 = **300 = 框底**（软边只在框内渐隐，不外溢）");
                CheckTrue(flat != null && band != null, "② 平段与斜坡段都找得到");
                if (flat != null)
                {
                    CheckNear(flat.WorldH * 108f, 25f, 0.5f, "② 平段（250→275）高 = **25px** = 带宽");
                    CheckTrue(flat.CornerColors == null, "② …这一段**完全在带外** ⇒ 顶点色一个字节都没改");
                }
                if (band != null)
                {
                    CheckNear(band.WorldH * 108f, 25f, 0.5f, "② 斜坡段（275→300）高 = **25px**");
                    var cc = band.CornerColors;
                    CheckTrue(cc != null && cc.Length == 4, "② …这一段**有顶点色**（斜坡落上去了）");
                    if (cc != null && cc.Length == 4)
                    {
                        // 顶点序 = BL · BR · TR · TL；y 向下 ⇒ BL/BR 在**框边那侧**（300）、TR/TL 在 275 那侧
                        CheckNear(cc[0].a, 0f, 0.01f, "② …底边（= 框边 300）alpha = **0**（原版掩码边上就是 0）");
                        CheckNear(cc[1].a, 0f, 0.01f, "② …底右角同样 0");
                        CheckNear(cc[2].a, 1f, 0.01f, "② …顶边（= 带宽内沿 275）alpha = **1**");
                        CheckNear(cc[3].a, 1f, 0.01f, "② …顶左角同样 1");
                        CheckNear(cc[0].r, 1f, 0.001f, "② …**只乘 alpha、rgb 不动**（乘了 rgb 会在 `c.rgb *= c.a` 外再暗一次）");
                        ampAtEdge = cc[0].a;
                    }
                }
                // 斜坡是真的（不是「设了个常量」）：对着原版剖面**手算**一个中点，线性插值应当 ≈ 0.5
                if (band != null)
                {
                    var cc = band.CornerColors;
                    CheckTrue(cc != null && Mathf.Abs(cc[1].a - cc[2].a - (-1f)) < 0.01f,
                              "② 两端 alpha 差 = 1（0 → 1 的**满量程**斜坡，不是被谁压小了）");
                    ampAtFlat = flat != null && flat.CornerColors != null ? flat.CornerColors[0].a : 1f;
                    CheckNear(ampAtFlat, 1f, 0.01f, "② 平段那 25px alpha 恒 1（原版在带外也是 1）");
                    CheckNear(ampAtEdge, 0f, 0.01f, "② 框边上 alpha 0 —— 这条就是「软边」的可观测定义");
                }
            }
            MenuDraw.ClearChildren(tmpS);

            // ---- ③ 软边 (42,0)（**锻造阵营条 `Forge Army Selector/Viewport` 的原版真值**）：切的是 x ----
            win.ClipSoftness = new Vector2(42f, 0f);
            var clip3 = new PxRect(400f, 100f, 900f, 300f);
            var r3 = new PxRect(350f, 150f, 500f, 250f);      // 左边越出框（框左 400）⇒ 硬裁后 [400,500]
            win.Clip = clip3;
            var qC = win.Rect(tmpS, art, r3.x1, r3.x2, r3.y1, r3.y2, "SoftX", 3005, null, false);
            CheckTrue(qC != null, "③ 横向软边那条建出来了");
            if (qC != null)
            {
                var ps = new List<ImageQuad>(qC.GetComponentsInChildren<ImageQuad>(true));
                Check(ps.Count, 2, "③ 切成 2 块（带宽内沿 = 框左 400 + 42 = **442**）");
                ImageQuad left = null, right = null;
                for (int i = 0; i < ps.Count; i++)
                {
                    float cx = PxOf(ps[i].transform.position.x);
                    float hw = ps[i].WorldW * 108f * 0.5f;
                    if (cx - hw <= 400.5f) left = ps[i]; else right = ps[i];
                }
                CheckTrue(left != null && right != null, "③ 左（斜坡）右（平）两块都在");
                if (left != null) CheckNear(left.WorldW * 108f, 42f, 0.5f, "③ 斜坡块宽 = **42px** = 原版 x 带宽");
                if (right != null) CheckNear(right.WorldW * 108f, 58f, 0.5f, "③ 平块宽 = 100 − 42 = 58px");
                if (left != null)
                {
                    var cc = left.CornerColors;
                    CheckTrue(cc != null && cc.Length == 4, "③ …斜坡块有顶点色");
                    if (cc != null && cc.Length == 4)
                    {
                        CheckNear(cc[0].a, 0f, 0.01f, "③ …**左**边（= 框边）alpha = 0");
                        CheckNear(cc[1].a, 1f, 0.01f, "③ …**右**边（= 带内沿）alpha = 1");
                        CheckNear(cc[2].a, 1f, 0.01f, "③ …右上角也 1");
                    }
                }
            }
            MenuDraw.ClearChildren(tmpS);
            win.Clip = null;
            win.ClipSoftness = new Vector2(0f, 0f);
            Object.DestroyImmediate(tmpS.gameObject);
        }

        Section("§三·b4-d-3 文字的部分越界（原来「压在框边的字照画出去」⇒ 现在切开）");
        {
            var tmpT = RewardsWindow.New(win.transform, "TextClipTest");
            var tbR = new PxRect(400f, 400f, 700f, 450f);
            // 控制组：**没有裁切** ⇒ 量出「本来有多少字在框外」（下面那条断言才有意义）
            var lbFree = win.Text(tmpT, "Warpforge Offline Rulebook", tbR.x1, tbR.x2, tbR.y1, tbR.y2, 5,
                                  Color.white, "Free", 36f);
            var freeVerts = TmpVertPx(lbFree);
            CheckTrue(freeVerts != null && freeVerts.Length >= 8, "控制组：TMP 网格量得到（≥ 8 个顶点）");
            int outFree = 0;
            if (freeVerts != null)
                for (int i = 0; i < freeVerts.Length; i++)
                    if (freeVerts[i].x > 550.5f || freeVerts[i].x < 399.5f
                        || freeVerts[i].y > 450.5f || freeVerts[i].y < 399.5f) outFree++;
            CheckTrue(outFree > 0, $"控制组：**确实有 {outFree} 个顶点落在「框 (400,400)-(550,450)」之外**"
                                 + "（没有这一条，下面「全在框内」就是空话）");
            var freeUv = TmpGlyphUvW(lbFree);                 // ⚠️ **必须在销毁它之前量**（销毁后组件是假 null）
            if (lbFree != null) Object.DestroyImmediate(lbFree.gameObject);

            // 实验组：拿同一个矩形、同一个框 ⇒ **所有顶点必须落进框内**
            win.Clip = new PxRect(400f, 400f, 550f, 450f);
            var lbClipped = win.Text(tmpT, "Warpforge Offline Rulebook", tbR.x1, tbR.x2, tbR.y1, tbR.y2, 5,
                                     Color.white, "Clipped", 36f);
            CheckTrue(lbClipped != null, "实验组：压在框边的文字**照建**（只有整块在框外才不建）");
            var clipVerts = TmpVertPx(lbClipped);
            CheckTrue(clipVerts != null && clipVerts.Length >= 8, "实验组：TMP 网格量得到");
            int outClip = 0; float worst = 0f;
            if (clipVerts != null)
                for (int i = 0; i < clipVerts.Length; i++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(clipVerts[i].x - 550f, 400f - clipVerts[i].x));
                    float dy = Mathf.Max(0f, Mathf.Max(clipVerts[i].y - 450f, 400f - clipVerts[i].y));
                    worst = Mathf.Max(worst, Mathf.Max(dx, dy));
                    if (dx > 0.5f || dy > 0.5f) outClip++;
                }
            CheckTrue(outClip == 0, $"实验组：**没有一个顶点出框**（越界最远 {worst:F3}px ≤ 0.5px；"
                                  + $"控制组是 {outFree} 个）—— 这就是「文字也被 `RectMask2D` 切」的判据");
            // uv 跟着改（不然被切掉那半个字会被**压扁**）。
            // 🔴 **判据必须与「没被切的那一份」比** —— 字形的 uv 宽度本身就是 0.02~0.05 那种小数，
            //    单独量它「< 1」是**恒真**的假断言（等于没断）。所以逐字做**对照**：
            //    控制组（没裁切）每个字的 uv 宽 ≡ 它的原字形宽；被切过的那些字**必须变窄**。
            var clipUv = TmpGlyphUvW(lbClipped);
            CheckTrue(clipUv != null && clipUv.Length >= 8, "实验组：量得到逐字的 uv 宽度");
            int narrowed = 0; float minRatio = 9f;
            if (freeUv != null && clipUv != null && freeUv.Length == clipUv.Length)
                for (int i = 0; i < clipUv.Length; i++)
                {
                    if (freeUv[i] <= 1e-4f) continue;
                    float ratio = clipUv[i] / freeUv[i];
                    minRatio = Mathf.Min(minRatio, ratio);
                    if (ratio < 0.9f) narrowed++;
                }
            CheckTrue(narrowed > 0,
                      $"**被切过的那些字，uv 真的跟着截窄了**（实测最窄 {minRatio:F3} × 原字形宽、"
                      + $"{narrowed}/{clipUv.Length} 个字变窄）—— 只挪顶点不改 uv 会让那半个字压扁");
            win.Clip = null;
            MenuDraw.ClearChildren(tmpT);

            // ② 整块在框外 ⇒ 仍然不建（老行为不许丢）
            win.Clip = new PxRect(1000f, 1000f, 1100f, 1100f);
            var lbOut = win.Text(tmpT, "Out", tbR.x1, tbR.x2, tbR.y1, tbR.y2, 5, Color.white, "Out", 36f);
            CheckTrue(lbOut == null, "② 整块在框外 ⇒ **照旧不建**（`Visible` 那头没被削弱）");
            win.Clip = null;
            // ③ 没有裁切 ⇒ 一个字都不碰
            var lbNoClip = win.Text(tmpT, "Warpforge", tbR.x1, tbR.x2, tbR.y1, tbR.y2, 5, Color.white, "NoClip", 36f);
            CheckTrue(lbNoClip != null, "③ 没裁切时文字照建");
            CheckTrue(MenuDraw.TextClipUnavailable == 0,
                      $"③ **没有一次「拿不到渲染网格」**（实测 {MenuDraw.TextClipUnavailable} 次）"
                      + " —— 那意味着某段字**悄悄没被裁**");
            Object.DestroyImmediate(tmpT.gameObject);
        }

        Section("§三·b4-d-4 九宫格也吃软边（`Nine` 那条路：逐子块上剖面）");
        {
            var tmpN2 = RewardsWindow.New(win.transform, "NineSoftTest");
            const string artN2 = "40k_main_bt_selected_BW";
            const float Corner2 = 30f / 0.92f;
            float side2 = 3f * Corner2;
            var nr2 = new PxRect(100f, 100f, 100f + side2, 100f + side2);
            var nclip2 = new PxRect(100f, 110f, 100f + side2, 100f + side2);
            var bd2 = new Vector4(30f, 30f, 30f, 30f);
            var bo2 = new Vector4(Corner2, Corner2, Corner2, Corner2);

            // 对照组：软边 0 ⇒ 一块都不能带顶点色（= 改之前的行为）
            var plain = MenuDraw.Nine(tmpN2, win.Art(artN2), nr2, bd2, 71f, 71f, 3005, null, true, "NPlain", bo2, nclip2);
            int tinted0 = 0;
            if (plain != null)
                foreach (var q in plain.GetComponentsInChildren<ImageQuad>(true))
                    if (q.gameObject.activeSelf && q.CornerColors != null) tinted0++;
            Check(tinted0, 0, "对照组（软边 0）：**没有任何子块带顶点色**");

            // 实验组：软边 (0,25) ⇒ 至少一块被削 alpha，且**没有一块画到框外**
            var soft = MenuDraw.Nine(tmpN2, win.Art(artN2), nr2, bd2, 71f, 71f, 3005, null, true, "NSoft", bo2, nclip2,
                                     new Vector2(0f, 25f));
            CheckTrue(soft != null, "实验组（软边 25px）建出来了");
            int tinted1 = 0, outside = 0; float worstOut = 0f;
            if (soft != null)
                foreach (var q in soft.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (!q.gameObject.activeSelf) continue;
                    if (q.CornerColors != null) tinted1++;
                    float cx = PxOf(q.transform.position.x), cy = PxYOf(q.transform.position.y);
                    float hw = q.WorldW * 108f * 0.5f, hh = q.WorldH * 108f * 0.5f;
                    float dx = Mathf.Max(0f, Mathf.Max((cx + hw) - nclip2.x2, nclip2.x1 - (cx - hw)));
                    float dy = Mathf.Max(0f, Mathf.Max((cy + hh) - nclip2.y2, nclip2.y1 - (cy - hh)));
                    worstOut = Mathf.Max(worstOut, Mathf.Max(dx, dy));
                    if (dx > 0.5f || dy > 0.5f) outside++;
                }
            CheckTrue(tinted1 > 0, $"实验组：**至少一块带上了坡度**（实测 {tinted1} 块）—— 软边真的走到了九宫格上");
            CheckTrue(outside == 0, $"实验组：**没有一块画到框外**（越界最远 {worstOut:F3}px ≤ 0.5px；"
                                  + "原版 `RectMask2D` 对九宫格子件一视同仁）");
            win.Clip = null;
            Object.DestroyImmediate(tmpN2.gameObject);
        }

        Section("§三·b4-e `ImageQuad.CreateNineSlice` 的边宽调整（判据 = 原版 uGUI `Image.GetAdjustedBorders`）");
        {
            // 🔴 判据出处 = `Runtime/UGUI/UI/Core/Image.cs:1479-1506`（`GetAdjustedBorders`，逐轴判在 `:1501`）+ `:1157-1167`（`GenerateSlicedSprite` 用它算三段）：
            //    **逐轴**判「两边边宽之和 **>** 目标尺寸」才按比例缩；`==` **不缩**（中段宽就是 0，
            //    `GenerateSlicedSprite:1193-1195` 把那格 `continue` 掉）。
            var tmp9 = RewardsWindow.New(win.transform, "NineBorderTest");

            // ---- ① 「端帽铺满整张图」是**合法**形状 ⇒ 不许打「border 比图还大」那条警告 ----
            var tex324 = win.Art("WF_Special_offer_Value");
            CheckTrue(tex324 != null && tex324.width == 324 && tex324.height == 87,
                      "`WF_Special_offer_Value` = **324×87**（原版 `m_Rect`；实测 "
                      + (tex324 != null ? tex324.width + "×" + tex324.height : "取不到") + "）");
            int warns = 0; string lastWarn = null;
            Application.LogCallback hWarn = (cond, st, type) =>
            {
                if (type == LogType.Warning && cond != null && cond.Contains("border 比图还大"))
                { warns++; lastWarn = cond; }
            };
            GameObject badge9 = null;
            Application.logMessageReceived += hWarn;
            if (tex324 != null)
                badge9 = ImageQuad.CreateNineSlice(tmp9, tex324, new Vector4(162f, 0f, 162f, 0f), 324f, 87f,
                                                   Vector3.zero, LayoutSpace.Px(339f), LayoutSpace.Px(87f), "Badge324");
            Application.logMessageReceived -= hWarn;
            Check(warns, 0, $"① **合法形状不误报**（`m_Border = (162,0,162,0)`、贴图正好 324 ⇒ `uL == uR`；"
                          + $"实测 {warns} 条警告{(lastWarn != null ? "：" + lastWarn : "")}）");
            // 目标多宽就画多宽：原版 ⇒ **162 + 15 + 162**（中段那 15px 照画，只是 uv 宽 0）
            CheckTrue(badge9 != null, "① 九宫格建出来了");
            if (badge9 != null)
            {
                // ⚠️ 子块名 = `{根名}_{i}{j}`，**i = 列（0 左）· j = 行（0 下）**。
                //   这里 `m_Border` 的 y/w（下/上）都是 **0** ⇒ j=0 与 j=2 两行**高为 0 会被跳过**
                //   （`CreateNineSlice` 里 `if (w <= 0f || h <= 0f) continue;`）⇒ 只剩 **j=1 那一行**：
                //   左端帽 `_01` · 中段 `_11` · 右端帽 `_21`（**不是** `_00/_10/_20`）。
                var p0 = badge9.transform.Find("Badge324_01");
                var p1 = badge9.transform.Find("Badge324_11");
                var p2 = badge9.transform.Find("Badge324_21");
                CheckTrue(p0 != null && p1 != null && p2 != null, "① 左/中/右三块都在（**中段没被吃掉**）");
                Check(badge9.GetComponentsInChildren<ImageQuad>(true).Length, 3,
                      "① 一共 **3 块**（上下边宽 0 ⇒ 那两行高 0，不建）");
                if (p0 != null) CheckNear(p0.GetComponent<ImageQuad>().WorldW * 108f, 162f, 0.5f,
                                          "① 左端帽 = **162px**（= `m_Border.x`）");
                if (p2 != null) CheckNear(p2.GetComponent<ImageQuad>().WorldW * 108f, 162f, 0.5f,
                                          "① 右端帽 = **162px**（= `m_Border.z`）");
                if (p1 != null) CheckNear(p1.GetComponent<ImageQuad>().WorldW * 108f, 15f, 0.5f,
                                          "① 中段 = **15px** = 339 − 162 − 162（原版这里也画）");
            }
            MenuDraw.ClearChildren(tmp9);

            // ---- ② 真越界（边宽之和 > 贴图宽）**必须**出声（这条让 ① 不是「把警告删掉」那种改法）----
            int warns2 = 0;
            Application.LogCallback hWarn2 = (cond, st, type) =>
            { if (type == LogType.Warning && cond != null && cond.Contains("border 比图还大")) warns2++; };
            Application.logMessageReceived += hWarn2;
            if (tex324 != null)
                ImageQuad.CreateNineSlice(tmp9, tex324, new Vector4(200f, 0f, 200f, 0f), 324f, 87f,
                                          Vector3.zero, LayoutSpace.Px(300f), LayoutSpace.Px(87f), "Bad324");
            Application.logMessageReceived -= hWarn2;
            Check(warns2, 1, "② 边宽之和 400 **>** 贴图宽 324 ⇒ **打一条警告**（真越界仍要出声，不许静默）");
            MenuDraw.ClearChildren(tmp9);

            // ---- ③ 逐轴缩（**本次行为改变那一处**）：`40k_popup`（边 169/160）塞进 1000×90 ----
            //     横：169+169 = 338 < 1000 ⇒ **不缩**（端帽 169）；竖：160+160 = 320 > 90 ⇒ 缩到 90/320。
            var popTex = win.Art("40k_popup");
            CheckTrue(popTex != null, "`40k_popup` 取得到（`CardArt.MenuUi` 三级兜底里有 `ui_deck/`）");
            if (popTex != null)
            {
                var pop = ImageQuad.CreateNineSlice(tmp9, popTex, new Vector4(169f, 160f, 169f, 160f),
                                                    359f, 336f, Vector3.zero,
                                                    LayoutSpace.Px(1000f), LayoutSpace.Px(90f), "Pop");
                var tl = pop.transform.Find("Pop_02");            // i=列（0 左）· j=行（2 顶）
                CheckTrue(tl != null, "③ 左上角块建出来了");
                if (tl != null)
                {
                    var q = tl.GetComponent<ImageQuad>();
                    CheckNear(q.WorldW * 108f, 169f, 0.6f,
                              "③ **横轴不缩** ⇒ 端帽仍是 169px（旧写法两轴共用一个比例，这里会画成 47.5px）");
                    CheckNear(q.WorldH * 108f, 45f, 0.6f,
                              "③ 竖轴缩：160 × (90÷320) = **45px**（`GetAdjustedBorders` 的逐轴语义）");
                }
                // 竖着被挤光 ⇒ 中段那一行不画（宽/高 ≤ 0 的子块一律 `continue`）
                var qs2 = pop.GetComponentsInChildren<ImageQuad>(true);
                Check(qs2.Length, 6, "③ 竖中段高 = 90 − 45 − 45 = 0 ⇒ **不建那一行** ⇒ 建出来 6 块");
            }
            MenuDraw.ClearChildren(tmp9);
            Object.DestroyImmediate(tmp9.gameObject);
        }

        // ============================================================ §三·c 战役页
        // 期望值同样全部来自原版参数（正本 `资料/阶段二_锻造厂与战役页_原版规格.md` §一/§四/§十三）。
        Section("§三·c `Campaign Tab`（战役，阶段二第 3 层第 2 件）：层 × 参数逐条对");
        // 🔴 **2026-10-11（W3 · A301）：进本节之前**先把页签切到 `Campaign`** —— 与 A267 完全同族
        //   （`戊1_Rewards四件.md` §五·2 记的就是这一条）。
        //   **成因**：本节之前最后一次会切页签的调用是 §三·b 开头那句 `Click(2)`（`Forge`），
        //   而把页切到 `Campaign` 的 `Click(1)` 要等到本文件末尾拍 `02_战役.png` 那一段。
        //   ⇒ 本节整段跑在**未激活**的 `Campaign Tab` 上。
        //   **为什么这是缺陷**（不是洁癖）：今天本节量的是矩形/位置/字符串/quad（不吃渲染）⇒ **没有假绿**；
        //   但**任何依赖「真渲染」的断言在关着的页上必然假绿** —— TMP 在未激活对象上
        //   `ForceMeshUpdate` 出不来网格（`资料/已知的坑.md`「面板画出来了、字不在」坑①），
        //   而 `MenuDraw.ClipTmpMesh` 那条路在没网格时**只数进 `TextClipUploadSkipped`、什么都不做**
        //   ⇒ 在这一段里验「字被裁 / 被削 alpha」拿到的是「没生效」。下一批谁往这里加渲染类断言就会中招。
        //   ⚠️ **这一句不改变画面 / 行为**：`CampaignTab.OnOpen` 只做 `FocusSelectedArmy()`，
        //     而它在 §二·3 那次 `Click(1)` 已经跑过；`MenuScroll.SetOffset` 是**绝对**定位
        //     （偏移没变就早退，`Shell/MenuScroll.cs:183-191`）⇒ 两条滚动区的偏移**逐字相同**、
        //     `BuildArmyItems` / `RefreshNodes` 都不会被触发重建（本页这两条路都不建 `Label`）。
        //   **改坏法**：删掉下面那句 `Click(1)`（或把它挪回文件末尾）⇒ 紧跟着那两条 ★ 立刻红。
        //   ⚠️ 切页那句**带 `camp != null` 的闸**：页不存在时（夹具坏）别去切 ——
        //     `ChangeTab` 会把**所有页**（2026-10-16 起 4 页）全关掉，那会把后面几十条断言一起带进沟里（红要红在本节这一条上）。
        if (camp != null && win.tabButtons != null) win.tabButtons.Click(1);
        Check(win.CurrentTab, WindowTabType.Campaign,
              "★ （夹具前提）进本节时页签**真的切到 `Campaign`** 了 —— 本节整段都在这页上量");
        CheckTrue(camp != null && camp.gameObject.activeInHierarchy,
                  "★ （夹具前提）`Campaign Tab` **此刻是活的**（`activeInHierarchy`）—— 本节的断言依赖真渲染"
                  + "（未激活的页上 TMP 出不来网格 / `ClipText` 只能数进 `TextClipUploadSkipped` ⇒ 会**假绿**）");
        if (camp != null)
        {
            CheckAt(camp, 330.69f, 1920f, 70.94f, 1080f, "`Campaign Tab` 页矩形");
            var cbg = FindChild(camp, "Campaign Background");
            CheckTrue(cbg != null, "`Campaign Background` 建了（原版是 `Mask`）");
            var cbgImg = FindChild(cbg, "Background Image");
            CheckTrue(cbgImg != null,
                      "`Background Image` **建了而且开着** —— 原版出厂 `m_IsActive=false`，"
                      + "运行时由 `CampaignUIBackground` 打开并换图（**与纪律①那种「原版真不用」的不一样**）");
            CheckArt(cbgImg, CampaignData.Background(CampaignData.Selected),
                     "阵营背景图（13 张 GUID↔阵营已闭环，1024² 整图）");

            // 阵营选择条：`Campaign Army Selector` x 745.92..1920.34 · y 70.94..207.94
            var csel = FindChild(camp, "Campaign Army Selector");
            CheckAt(csel, 745.92f, 1920.34f, 70.94f, 207.94f, "`Campaign Army Selector`");
            var cArmy = FindChild(FindChild(csel, "Viewport"), "Army Content");
            CheckTrue(cArmy != null && cArmy.childCount > 0 && cArmy.childCount <= CampaignData.Armies.Length,
                      $"`Army Content` 下建的是**视口里放得下的那些条目**（实测 {cArmy.childCount} / 共 "
                      + $"{CampaignData.Armies.Length} 个阵营）");
            // 🔴 与锻造页**同形**（同一处收口，见 `UguiLayout.HorizontalContentCentered`）：
            //    原版 `Army Content` 是「选择条正中心的一个零宽点」+ `ContentSizeFitter` ⇒ 条目**居中**排。
            if (cArmy != null)
            {
                CheckNear(PxOf(cArmy.position.x), 1333.13f, 1f,
                          "`Army Content` 的中心 x = **选择条中心 1333.13**（745.92..1920.34）");
                var ctab = camp.GetComponent<CampaignTab>();
                CheckTrue(ctab != null && ctab.ArmyScroll != null, "战役页阵营条的滚动区建了（与锻造页共用 `MenuScroll`）");
                if (ctab != null && ctab.ArmyScroll != null)
                {
                    var cs = ctab.ArmyScroll;
                    // ✅ 2026-09-23 **缺口已修**：这条以前钉的是「越出屏幕 = 2」——
                    //    现在两侧都能滚 ⇒ 期望值翻成 **0**（照 §一 那条纪律）。
                    float fx1, fy1, fx2, fy2; int cOut = 0;
                    cs.ScrollBy(cs.MinOffset - cs.Offset);
                    float lo = cs.Offset;
                    cs.ScrollBy(cs.MaxOffset - cs.Offset);
                    // 两端各量一次：**两端那一条都要能完整落进视口**
                    for (int pass = 0; pass < 2; pass++)
                    {
                        if (pass == 1) cs.ScrollBy(cs.MinOffset - cs.Offset);
                        int idx = pass == 0 ? CampaignData.Armies.Length - 1 : 0;
                        var it = FindChild(cArmy, "CampaignArmyItem_" + idx);
                        if (it != null && RectOf(FindChild(it, "Icon"), out fx1, out fy1, out fx2, out fy2))
                            if (fx1 < 745.92f || fx2 > 1920.34f) cOut++;
                    }
                    Check(cOut, 0, $"阵营条**两端滚到位后没有条目越出屏幕**（实测 {cOut}）"
                          + " —— 缺口已修：`MenuScroll` 两侧都能滚（原版 `ScrollRect` 同）");
                    cs.ScrollBy(lo - cs.Offset);          // 还原
                    ctab.FocusSelectedArmy();

                    // ---- 条目内部几何（🔴 2026-09-24 照 `Campaign Army Item Button` 那棵树**直读**订正）----
                    // 判据 = 正本 §六 三变体对照表 + `menu_rect.py bundle_menus_assets_all
                    //        "Campaign Army Item Button" --depth 4 --cs`。
                    // ⛔ 原来照**母版**画：高亮框 136×122、图标用母版那套拉伸锚铺满整格 —— 三处都错。
                    {
                        // ⚠️ `Icon` 挂 `preserveAspect`（母版与变体都如此）⇒ **渲出来**是 100×100
                        //    （框 120×100 里**高度受限**）。所以「框宽 120」量不到、也不该量 ——
                        //    改判三样：渲出高 100 · 与高亮框同竖轴 · 框顶在高亮框顶下 5。
                        int selIdx = System.Array.IndexOf(CampaignData.Armies, CampaignData.Selected);
                        var sel = FindChild(cArmy, "CampaignArmyItem_" + selIdx);
                        var it0 = FindChild(cArmy, "CampaignArmyItem_0");
                        var ic0 = FindChild(it0, "Icon");
                        float x1, y1, x2, y2;
                        CheckTrue(ic0 != null && RectOf(ic0, out x1, out y1, out x2, out y2),
                                  "`CampaignArmyItem` 里有 `Icon`");
                        if (ic0 != null && RectOf(ic0, out x1, out y1, out x2, out y2))
                        {
                            CheckNear(y2 - y1, 100f, 1f,
                                      "条目 `Icon` **渲出**高 100（框是 120×100，`preserveAspect` 后高度受限）");
                            CheckNear(x2 - x1, 100f, 1f, "……渲出宽也是 100（方形图放进 120×100 的框，两侧各留 10）");
                        }
                        var hb = FindChild(sel, "HighlightBG");
                        var ic = FindChild(sel, "Icon");
                        CheckTrue(hb != null && RectOf(hb, out x1, out y1, out x2, out y2),
                                  $"选中那格（#{selIdx} {CampaignData.Selected}）有 `HighlightBG`");
                        if (hb != null && RectOf(hb, out x1, out y1, out x2, out y2))
                        {
                            float hx1 = x1, hy1 = y1, hx2 = x2;
                            CheckNear(hx2 - hx1, 120f, 1f, "`HighlightBG` 宽 **120**（母版是 136，变体是 120）");
                            CheckNear(y2 - y1, 110f, 1f, "`HighlightBG` 高 **110**（母版是 122）");
                            float ix1, iy1, ix2, iy2;
                            if (ic != null && RectOf(ic, out ix1, out iy1, out ix2, out iy2))
                            {
                                // 框高与渲出高同为 100（高度受限）⇒ **渲出的上沿就是框的上沿**
                                CheckNear(iy1 - hy1, 5f, 0.6f,
                                          "`Icon` 的框顶在高亮框顶**下 5**（原版 `pos (0,−5)`）");
                                CheckNear((ix1 + ix2) * 0.5f - (hx1 + hx2) * 0.5f, 0f, 0.6f,
                                          "`Icon` 与高亮框**同一竖轴**（原版锚 `(0.5,1)`、pos x=0）");
                            }
                        }
                        // 进度条（**这一页独有**）：宽 = 条目宽 − 10 = 110、芯 12 高
                        var sl = FindChild(it0, "Slider/Background");
                        CheckTrue(sl != null && RectOf(sl, out x1, out y1, out x2, out y2),
                                  "条目底下那条 `Slider`（**Campaign 变体独有**，母版没有）");
                        if (sl != null && RectOf(sl, out x1, out y1, out x2, out y2))
                        {
                            CheckNear(x2 - x1, 110f, 1f, "进度条宽 **110**（原版 `sizeDelta.x = −10`）");
                            CheckNear(y2 - y1, 12f, 1f, "进度条芯高 **12**（原版 `Background` 锚 0.2–0.8）");
                        }
                        // ⚠️ **不许有 `Arrow`** —— 这一变体**没有这个节点**（母版与 Forge 版才有）
                        CheckTrue(FindChild(it0, "Arrow") == null,
                                  "战役页的阵营格**没有 `Arrow`**（直读：`Campaign Army Item Button` 没有这个节点）");
                        // 进度条是**逐阵营**的：**没有内容的阵营一条填充都不该有**。
                        // 🔴 第一版对全部 13 格都填同一个值 —— 那是错的（本地只有 Ultramarines 有内容）。
                        for (int k = 0; k < 3 && k < CampaignData.Armies.Length; k++)
                        {
                            if (CampaignData.HasContent(CampaignData.Armies[k])) continue;
                            var nit = FindChild(cArmy, "CampaignArmyItem_" + k);
                            if (nit == null) continue;
                            CheckTrue(FindChild(nit, "Slider/Fill") == null,
                                      $"没有内容的阵营（{CampaignData.Armies[k]}，第 {k + 1} 格）**不画进度条填充**");
                            break;
                        }
                    }
                }
            }

            // Header：`Campaign Header` x 330.69..790.92 · y 60.94..225.94
            var chdr = FindChild(camp, "Campaign Header");
            CheckAt(chdr, 330.69f, 790.92f, 60.94f, 225.94f, "`Campaign Header`");
            CheckArt(FindChild(chdr, "bg"), "WF_Campaign_Info_Background", "Header 的底图");
            CheckArt(FindChild(chdr, "Army Icon"), DeckRuntime.FactionIcon(CampaignData.Selected),
                     "Header 的阵营徽记（走 `DeckRuntime.FactionIcon`）");
            CheckAt(FindChild(chdr, "Info Button"), 719.80f, 760.95f, 94.03f, 135.19f, "`Info Button`");

            // 轨道：`Campaign Track` x 330.69..1920.34 · y 335.47..1044.53
            var ctrack = FindChild(camp, "Campaign Track");
            CheckAt(ctrack, 330.69f, 1920.34f, 335.47f, 1044.53f, "`Campaign Track`");
            var cContent = FindChild(FindChild(ctrack, "Viewport"), "Content");
            var cTab0 = camp.GetComponent<CampaignTab>();
            int cNodes = 0; var cLines = 0;
            if (cContent != null)
                foreach (var t in cContent.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("CampaignNode_")) cNodes++;
                    else if (t.name.StartsWith("NodeLine_")) cLines++;
                }
            // 🆕 **2026-10-03：接上横向滚动 + `RectMask2D` 等效裁剪之后，只有【视口内】的节点会建**
            //    —— 视口外的连**点击区**一起不建（原版 `RectMask2D` 就是这么裁的）。
            //    ⇒ 判据从「恰好 47 个」改成「恰好 = 视口内那几个」（数量现算，不写死）。
            int cWant = 0;
            for (int t = 0; t < CampaignData.NodeCount; t++)
            {
                if (cTab0 == null || cTab0.TrackScroll == null
                    || cTab0.TrackScroll.Intersects(cTab0.NodeRectForTest(t))) cWant++;
            }
            Check(cNodes, cWant,
                  $"轨道上**恰好建了视口内的那些节点**（{cWant} / 共 {CampaignData.NodeCount}；"
                  + "视口外的被 `RectMask2D` 等效裁剪掉、**连点击一起**）");
            CheckTrue(cWant > 0 && cWant < CampaignData.NodeCount,
                      $"…而且**确实有节点落在视口外**（{CampaignData.NodeCount - cWant} 个）—— 否则这一条等于没验");
            CheckTrue(cLines > 0, $"连线建了（{cLines} 条；**原版 `SetAsFirstSibling` ⇒ 连线在节点下面**）");

            // Premium Panel：**矩形不是 JSON 值**（dump 出来高 = 0），是布局组算的（正本 §四）
            var cpan = FindChild(camp, "Premium Panel");
            CheckAt(cpan, 344.29f, 720.35f, 867.01f, 1080.00f,
                    "`Premium Panel`（**实算值**：`ContentSizeFitter` + `VerticalLayoutGroup` padding 7/11）");

            // 纪律①：出厂 inactive 的三件**不建**
            CheckTrue(FindChild(camp, "Debug Point Button") == null,
                      "`Debug Point Button` **不建**（`OnSetup` 里无条件 `SetActive(false)`）");
            CheckTrue(FindChild(camp, "Premium Button Container") == null,
                      "`Premium Button Container` **不建**（全 bundle 无脚本引用 ⇒ **发行版永不显示**）");
            CheckTrue(FindChild(camp, "Tutorial Message") == null,
                      "`Tutorial Message` **不建**（`ToggleChooseArmyText` **全库 0 调用者** ⇒ 改由教程线做）");

            // 🔴 **6 种状态色**（照原版 `SetNodeStyle` 的表）—— 起手只有根节点是 `Unlocked`
            var cTab = camp.GetComponent<CampaignTab>();
            CheckTrue(cTab != null, "`Campaign Tab` 上挂的是 `CampaignTab`（**不再是空页桩**）");
            if (cTab != null && cContent != null)
            {
                var n0 = cContent.Find("CampaignNode_0");
                var n5 = cContent.Find("CampaignNode_5");
                var b0 = n0 != null ? FindChild(n0, "Generic Round Button Variant") : null;
                var b5 = n5 != null ? FindChild(n5, "Generic Round Button Variant") : null;
                var t0 = TintOf(b0); var t5 = TintOf(b5);
                CheckNear(t0.a, 0.8078431f, 0.01f, "根节点圆盘 alpha = **0.8078**（`unlockedColor` #FFFFFFCE）");
                CheckNear(t5.a, 0.6823530f, 0.01f, "第 6 个节点圆盘 alpha = **0.6824**（`lockedColor` #B2A5A5AE）");
                CheckNear(t5.r, 0.6981132f, 0.01f, "第 6 个节点圆盘 r = **0.6981**（locked 是灰色，不是白）");

                // 点根节点 ⇒ **开奖励窗**（原版 `CampaignWindowTab.OnNodeClicked` 是组 context 开窗，
                // **不是点节点就发奖** —— 发奖在窗里那个 `Unlock` 钮上）。
                // ⚠️ 2026-09-23 改：原来这一条断的是「直接领到了」，那时 `CampaignRewardWindow` 还没建。
                CheckTrue(cTab.ClickNodeForTest(0), "点根节点 UM0 **开出了奖励窗**（`CampaignData.Claimable` 为真）");
                CheckTrue(!CampaignData.BaseClaimed(0), "**开窗本身不发奖**（原版领取发生在窗里的 `Unlock` 钮上）");
                // 🔴 **2026-10-13（A469①）就地订正（铁律 5）**：本行原来写「**再从窗里领**（`Unlock` 钮那条路：
                //   `OnCollect` → `CampaignTab.ClaimForTest`）」—— **名不副实**：下面那句是**直调**
                //   `ClaimForTest(0, TierBasic)`，**根本不经窗里那颗钮**（`OnCollect` 一次都不会被调）。
                //   它本身没错（验的是那个方法的返回值），但 A448 的「领到 ⇒ 关窗」就长在**钮那条路**上
                //   ⇒ 直调**绕过了那条新行为**（H26 §七 记的就是这一格）。窗里那条路的覆盖在下面
                //   §三·d 的「A469①/②/③」那一块（真点 `Unlock`，走 `PointerLayer`）。
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ A449：（前提）这一下之前场上**没有遗留的领奖窗**");
                CheckTrue(cTab.ClaimForTest(0, CampaignData.TierBasic), "直调 `ClaimForTest` ⇒ 根节点 UM0 **领到了基础档**"
                          + "（⛔ 这条**不**经窗里那颗钮 —— 钮那条路见 §三·d 的 A469）");
                // ================================================================
                // 🆕 **2026-10-12（A449）**：`ClaimForTest` 领到 ⇒ **弹那扇 `Reward Window`**
                //  （A438 接的：原版 `Everguild.LiveOps.Campaign.CollectRewards` 的回包
                //    `RewardService.Collect(rewards, showAnimation: **1**, …)` ⇒ 开窗；见 `Shell/CampaignTab.cs`
                //    的 `ClaimForTest` 方法头）—— **直调这条也走同一个出口**，所以这里必须收口。
                //  ⛔ **不补这一手会出两件静默的坏事**（都不是靠断言看得见的）：
                //   ① 本文件后面 §「领奖」那一节里的 `Check(RewardWindowFixture.DismissRewardWindows(), **1**, …)`
                //      （拍那 4 张「屏上只有壳」之前的那一道闸）**会数到 2 扇** —— 因为这一扇一直开着
                //      没人关 ⇒ 一条**假红**；
                //   ② 上面 `Shoot("02b_战役奖励窗.png")` 会被它**压一层** —— `Background` 态的窗
                //      **只是不可点、照渲染** ⇒ 拍出来是一张「战役奖励窗上面盖着领奖窗」的图，
                //      而断言一条都不会报（正是那种得靠人去看图的坑）。
                //      判据（本件现读的两个常量）：`RewardWindow.QShade = **3130**` >
                //      `CampaignRewardWindow.QShade = **3110**` ⇒ 领奖窗**恒画在战役奖励窗之上**。
                var rwCamp = RewardWindowFixture.FindOpenRewardWindow();
                CheckTrue(rwCamp != null, "★ A449：`ClaimForTest` 领到 ⇒ **弹出 `Reward Window`**"
                          + "（删掉 `Shell/CampaignTab.cs` 的 `ClaimForTest` 里那句 `ShowCollected` ⇒ 红）");
                Check(RewardWindowFixture.DismissRewardWindows(), rwCamp != null ? 1 : 0,
                      "★ …并**立刻关掉**它（关窗才会走 `NotifyClosed` → `ShowPreviousWindow` 把压到 "
                      + "`Background` 的底窗带回 `Open`；⛔ 别改用 `wm2.CloseAllWindows()` —— 那会把主壳窗一起关掉）");
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ …现在场上**一扇遗留的领奖窗都没有**（下面 `02b_战役奖励窗.png` 才拍得干净）");
                var n0b = cContent.Find("CampaignNode_0");
                var n1b = cContent.Find("CampaignNode_1");
                var t0b = TintOf(n0b != null ? FindChild(n0b, "Generic Round Button Variant") : null);
                var t1b = TintOf(n1b != null ? FindChild(n1b, "Generic Round Button Variant") : null);
                CheckNear(t0b.g, 1f, 0.01f, "领过之后根节点染色 = **`collectedColor`**（绿 g=1）");
                CheckNear(t0b.a, 0.6823530f, 0.01f, "领过之后根节点 alpha = **0.6824**");
                CheckNear(t1b.a, 0.8078431f, 0.01f,
                          "**后继节点自动解锁**（`unlockedColor` alpha 0.8078）—— 原版 `Collect` 里把邻居 `State 0 → 10`");
                // 轨道几何：**缩放比的判据 = 「47 个节点都落在 Viewport 竖向范围内」**
                // （原版 `CalculateRatio` 里那几个常量没全解出 ⇒ 这里不盯公式、盯**可观测的结果**）。
                // 轨道 rect / Viewport rect 照 `CampaignTab` 的常量现算（同一套锚点五元组）。
                var trackR = UguiRect.Child(new PxRect(CampaignTab.TabL, CampaignTab.TabT, 1920f, 1080f),
                                            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                                            new Vector2(0f, -114.53f), new Vector2(0.34009f, 709.06f));
                var vpR = UguiRect.Child(trackR, UguiRect.A00, UguiRect.A11, UguiRect.P01,
                                         new Vector2(0f, 50f), new Vector2(0f, 50f));
                CheckNear(128f * cTab.Ratio, (vpR.H - 100f) / (2f * 392f) * 128f, 1.0f,
                          "**行距** = 128（SO 里两行差）× ratio；ratio 照原版 `CalculateRatio` 的结构"
                          + "（视口高 − 节点高）/（2 × |y| 最大）—— **小于节点高 100 就会上下叠**");
                float top = float.MaxValue, bot = float.MinValue;
                for (int t = 0; t < CampaignData.NodeCount; t++)
                {
                    // 🆕 2026-10-03：**用数据算的矩形**（视口外的节点现在不建 ⇒ `Find` 找不到 ≠ 没有）
                    var rr = cTab.NodeRectForTest(t);
                    float y = rr.CY;
                    if (y < top) top = y;
                    if (y > bot) bot = y;
                }
                CheckTrue(top >= vpR.y1 - 60f && bot <= vpR.y2 + 60f,
                          $"**47 个节点全部落在 Viewport 的竖向范围内**（实测 {top:F0}..{bot:F0}，"
                          + $"视口 {vpR.y1:F0}..{vpR.y2:F0}）—— 这就是「缩放比算对了」的判据");

                // ---- 🆕 2026-10-03：`Campaign Track` 的**横向滚动**（原版这一件是横向 `ScrollRect`）----
                {
                    var ts = cTab.TrackScroll;
                    CheckTrue(ts != null, "轨道有横向滚动区（`MenuScroll`，与锻造页共用同一份实现）");
                    if (ts != null)
                    {
                        CheckTrue(!ts.Vertical, "是**横向**滚动（原版 `Campaign Track` 横向）");
                        ts.SetOffset(0f);
                        CheckNear(ts.Offset, 0f, 0.01f, "起手在**最左**（原版也是起手滚到最左）");
                        // 最左那个节点在视口里（= 那道「让出一个光圈半径」的补偿还在起作用）
                        var r0 = cTab.NodeRectForTest(0);
                        CheckTrue(r0.CX >= vpR.x1, $"最左节点**不被左栏压住**（中心 x {r0.CX:F1} ≥ 视口左 {vpR.x1:F1}）");

                        // 🆕 **2026-10-04（A48 接线批）**：战役这条轨道的 `RectMask2D.m_Padding` **实读 = (0,0,0,0)**
                        //   （全量表 `d:/4/_tmp_view/q1_rm2d.txt:297-298` 的 `Campaign Tab/Campaign Track/Viewport`
                        //   与 `:93-94` 的 `Rewards Base Submenu Variant/…/Campaign Tab/Campaign Track/Viewport`）
                        //   ⇒ **命中区不许被缩**：命中 quad 的矩形必须**逐边等于**节点自己那份几何。
                        //   🔴 这一条同时盯两件事：① 有人把锻造页那条 `(10,0,0,0)` 抄到战役页上（左边缘差 10px ⇒ 红）；
                        //      ② **锻造页忘了还原 `ClipPad`** ⇒ 战役页会吃到一个**漏出来的 pad**（同样红）。
                        //   ⚠️ 挑一个**完整落在视口里**的节点量（压在视口边上的会被 `Clip` 截，截出来的边本就不等于节点边）。
                        cTab.RefreshNodes();     // 幂等（先清再建）⇒ 保证量到的那棵与当前滚动偏移一致
                        {
                            int pickNode = -1;
                            for (int i = 0; i < CampaignData.NodeCount; i++)
                            {
                                var rc = cTab.NodeRectForTest(i);
                                // 留 5px 余量：编者这边重算的 `vpR` 与 `CampaignTab._vpR` 若有亚像素差，
                                // 贴在边上的节点会被 `Clip` 切掉一丝 ⇒ 那一条边就不等于节点边（假红）。
                                if (rc.x1 >= vpR.x1 + 5f && rc.x2 <= vpR.x2 - 5f
                                    && rc.y1 >= vpR.y1 + 5f && rc.y2 <= vpR.y2 - 5f)
                                { pickNode = i; break; }
                            }
                            CheckTrue(pickNode >= 0,
                                      "找得到一个**完整落在视口里**的战役节点（找不到 ⇒ 这条等于没查）");
                            if (pickNode >= 0)
                            {
                                var rt = cTab.NodeRectForTest(pickNode);
                                var nt = cContent.Find("CampaignNode_" + pickNode);
                                var ht = nt != null ? FindChild(nt, "Hit") : null;
                                var hq = ht != null ? ht.GetComponentInChildren<ImageQuad>() : null;
                                CheckTrue(hq != null, $"节点 #{pickNode} 有命中区（`Hit` + `ImageQuad`）");
                                if (hq != null)
                                {
                                    float px1, py1, px2, py2;
                                    CheckTrue(QuadRectOf(hq, out px1, out py1, out px2, out py2),
                                              "…它的渲染矩形量得到");
                                    CheckNear(px1, rt.x1, 0.5f,
                                              $"★ 战役节点 #{pickNode} 的命中区**左边缘 = 节点左边缘**"
                                              + "（pad 实读 (0,0,0,0) ⇒ 不缩；抄了锻造那条 +10 这里就红）");
                                    CheckNear(px2, rt.x2, 0.5f, "…右边缘相等（pad 的 R 也是 0）");
                                    CheckNear(py1, rt.y1, 0.5f, "…上边缘相等（`m_Padding.w` = Top = 0）");
                                    CheckNear(py2, rt.y2, 0.5f, "…下边缘相等（`m_Padding.y` = Bottom = 0）");
                                }
                            }
                        }
                        // 右端：偏移上限 = 内容右端 − 视口右端（>0 ⇒ 右边确实滚得过去）
                        CheckTrue(ts.MaxOffset > 100f,
                                  $"右侧**留了可滚的余量**（{ts.MaxOffset:F0}px —— 47 个节点铺 5000+px，视口只有 {vpR.W:F0}）");
                        ts.SetOffset(ts.MaxOffset);
                        var rl = cTab.NodeRectForTest(CampaignData.NodeCount - 1);
                        CheckTrue(rl.CX <= vpR.x2 + 0.5f,
                                  $"滚到最右 ⇒ **终点节点（UM47）进了视口**（中心 x {rl.CX:F1} ≤ 视口右 {vpR.x2:F1}）");
                        int built = 0;
                        if (cContent != null)
                            foreach (var t in cContent.GetComponentsInChildren<Transform>(true))
                                if (t.name.StartsWith("CampaignNode_")) built++;
                        CheckTrue(built > 0 && built < CampaignData.NodeCount,
                                  $"滚到最右 ⇒ 建的是**另一批**节点（{built} 个；视口外的仍然不建）");
                        ts.SetOffset(0f);
                    }
                }

                // ============================================================ 🆕 **2026-10-14（A469 · 只剩「生产接线」那一小块）**
                // 账（`项目任务.md` §A469）：**「`CampaignTab.BuildContext` → 真数据领到」**。
                // 上面那几句走的是**直调** `cTab.ClaimForTest(0, TierBasic)`（⛔ 它一个字符都不经 `BuildContext`）
                // ⇒「节点开出的窗里那颗 `Unlock` → `BuildContext` 挂的 `OnCollect` → `ClaimForTest` →
                // `CampaignData.Claim`」这条链**一条都没盯**（H33 §三·1 记的就是这一格；`§三·d` 的
                // A469①/②/③ 用的是**夹具自己的 lambda** ⇒ 只证窗那一侧）。
                // 🔴 期望值 = **原版行为**：窗里基础列那颗 `Unlock` 按下去 ⇒ 那一格的**真进度**被领掉
                //    （原版 `CampaignRewardsWindow` 的 `Unlock` → `UnlockClicked` → `TryCollect`）。
                // 🔴 改坏法（灭自证）：把 `Shell/CampaignTab.cs` 的 `BuildContext` 里 `OnCollect = tier => …` 那一句（`OnCollect = tier => ClaimForTest(i, tier)`）
                //    删掉 / 改成不转发 ⇒ 下面那两条★★一起红。
                // 排序理由（H33 §三·1 那三条风险）：本块**改真数据**（UM1 领掉）⇒ 只能排在这几条节点染色
                // 断言**之后**、`CampaignData.ResetForTest()` **之前**；它还会多弹一扇领奖窗 ⇒ 本块自己收干净。
                {
                    CampaignRewardWindow OpenCampWin()
                    {
                        // 判据同 `RewardWindowFixture`：**扫全部 `WindowsManager`**（领奖窗曾落到第二台管理器上，
                        // 见 DIAG-B §六·1）；「开着」= `CurrentState != Closed`，⛔ 不是「实例在不在」
                        // （`Close()` 只 `SetActive(false)`、实例留在锚点下 —— 老账 A123）。
                        foreach (var w in Object.FindObjectsByType<WindowsManager>(FindObjectsSortMode.None))
                            for (int i = 0; i < w.openWindows.Count; i++)
                            {
                                var c = w.openWindows[i] as CampaignRewardWindow;
                                if (c != null && c.CurrentState != WindowState.Closed) return c;
                            }
                        return null;
                    }
                    // 🔴 先把**遗留**的收干净：`PointerLayer` 的命中表**不认「窗在不在前台」**（`ToBackground()`
                    //    只改 `CurrentState`、不置灰物体）⇒ 场上同时两扇时，`Unlock Button/Hit` 的矩形与命中队列
                    //    **逐位相同** ⇒ 赢家退化成 `FindObjectsByType` 的枚举顺序（**静默、间歇**）。
                    //    ⚠️ 走到这里**确实有一扇遗留**（`:4747` 那次 `ClickNodeForTest(0)` 开的那扇还没人关）
                    //    —— 它本该由本节末尾那句**清场**收：关掉 A438 那扇
                    //    领奖窗时 `ShowPreviousWindow` 已经把 `popUpWindow` 填回了它
                    //    （`Shell/WindowsManager.cs:1059-1060`），但那一句**排在本块之后**
                    //    ⇒ `PointerLayer` 的命中表那会儿还是脏的，本块自己先收掉。
                    //    🔴 收掉它**没有副作用**（现读核过）：`prev`（= 壳 `RewardsWindow`）是 **Fullscreen**
                    //    （`Shell/RewardsWindow.cs:222`）⇒ 同一处 `:1060` 的三元把 `popUpWindow` 填成 `null`，
                    //    且 `prev.TryOpen()` 走的是「`Background` ⇒ 只提前台」那一支（**不重建内容**
                    //    ⇒ `cTab` 这个引用不受影响）。
                    //    🔴 **2026-10-14（A512）**：本节末尾那一句已从「按 `wm2.popUpWindow` 关一扇」改成
                    //    **共用的按【类型】清** `CloseModalPopups()` ⇒ 它**更**是空转（那一刻 `popUpWindow`
                    //    按上面那条推理就是 `null`），而且⚠️ 它**清不到** `CampaignRewardWindow`（那不是
                    //    两类模态宿主之一，见 `Editor/MainMenuScene.cs` 那份文档注释）⇒ 这一扇（`:4747` 的遗留）
                    //    的收尾**靠的就是本块上面那个 `OpenCampWin()` 循环**（逐扇关并断言 0），不是末尾那一句。
                    Check(RewardWindowFixture.DismissRewardWindows(), 0,
                          "（前提）走到这里时场上**没有**遗留的领奖窗（上面那句 `DismissRewardWindows() == 1` 已收干净）");
                    int campStale = 0;
                    for (var w0 = OpenCampWin(); w0 != null; w0 = OpenCampWin()) { w0.Close(); campStale++; }
                    CheckTrue(OpenCampWin() == null,
                              $"（前提）场上**没有**遗留的战役奖励窗（本块收掉了 {campStale} 扇）—— "
                              + "两扇同族窗并存时 `Unlock Button/Hit` 的矩形与命中队列逐位相同 ⇒ 下面那次真点的赢家会退化成枚举顺序");
                    CheckTrue(CampaignData.BaseClaimed(0) && !CampaignData.BaseClaimed(1),
                              "（前提）UM0 已领（上面那次直调）、**UM1 还没领** —— 前驱领过 UM1 才解锁");
                    CheckTrue(CampaignData.Claimable(1), "（前提）UM1 现在**可领**（`Claimable` 为真）");

                    // ---- ① context 本身：得是**照真进度现组**的，而且挂着 `OnCollect` ----
                    var prodCtxA = cTab.BuildContext(1);
                    CheckTrue(prodCtxA.OnCollect != null,
                              "★ A469：`CampaignTab.BuildContext` **挂着 `OnCollect`**"
                              + "（删掉 `Shell/CampaignTab.cs` 的 `BuildContext` 里 `OnCollect = tier => …` 那一句 ⇒ 红）");
                    CheckTrue(prodCtxA.BaseCollected == CampaignData.BaseClaimed(1)
                              && prodCtxA.Claimable == CampaignData.Claimable(1),
                              "★ …而且 `BaseCollected` / `Claimable` **等于真进度**（`BuildContext` 里写死任一个 ⇒ 红）");

                    // ---- ② 窗里那颗钮那条路（`PointerLayer` 真路径 —— ⛔ 不是直调 `OnUnlock`）----
                    CheckTrue(cTab.ClickNodeForTest(1), "★ A469：点 UM1 那个节点 ⇒ **开出了奖励窗**");
                    var prodWin = OpenCampWin();
                    CheckTrue(prodWin != null, "★ …而且真开出来了一扇 `Campaign Reward Window`");
                    if (prodWin != null)
                    {
                        // 🔴🔴 **缺陷（本件现读查出，⛔ 只报不改 —— `Shell/CampaignTab.cs` 不在本件白名单）**：
                        //   `ClickNodeForTest` 是 `win.Reopen(BuildContext(i))`（= 置 `_ctx` + `Build()`；`:803`）
                        //   **然后**才 `_win.Manager.OpenWindow(win)`（`:804`）；而 `OpenWindow` 调的是**带参**
                        //   `TryOpen(null)`（`Shell/WindowsManager.cs:875` → `:892`）⇒ `SetupData(null)` ⇒
                        //   `_ctx = null`（`Shell/CampaignRewardWindow.cs` 的 `SetupData`）⇒ 新窗走 `Closed` 支调
                        //   `Open()` = `Build()`（`:318`）⇒ **整棵按空 context 重建**（`:329` 的 `_ctx ?? 空`）
                        //   ⇒ **那扇窗是空的**（两列都关、一颗 `Unlock Button` 都没有）。
                        //   ⚠️ 这条链是**生产路径**（`OnNodeClicked` ← 节点那颗钮）⇒ 玩家点节点看到的是一扇空窗。
                        //   ⇒ 本段**必须**先补这一句把 `_ctx` 装回去才真点得下去（`Reopen` 幂等：首句清空根子件再建）。
                        //   ⛔ 它是**绕开那个缺陷的夹具补丁**，⛔ 别读成生产路径；缺陷修好之后这一句仍然成立
                        //   （同一个 `BuildContext(1)`、同一棵树）。
                        prodWin.Reopen(cTab.BuildContext(1));
                        var prodHit = FindPath(prodWin.transform,
                                               "Content/Scroll View/Viewport/Content/Base Rewards/Rewards/Unlock Button/Hit");
                        CheckTrue(prodHit != null, "★ A469②：（前提）窗里基础列那颗 `Unlock Button/Hit` 取得到");
                        var prodWb = prodHit != null ? prodHit.GetComponent<WindowButton>() : null;
                        CheckTrue(ClickButtonByQuad(PointerLayer.Instance, prodHit, prodWb,
                                                    "生产接线那扇窗里的 `Unlock`"),
                                  "★ A469②：**真点**那颗 `Unlock`（`PointerLayer` 真路径 —— ⛔ 不是直调 `OnUnlock`）");
                        CheckTrue(CampaignData.BaseClaimed(1),
                                  "★★ A469：**生产接线通了** —— 窗里那颗钮按下去 ⇒ UM1 的**真进度**被领掉"
                                  + "（`BuildContext` 的 `OnCollect` 不转发 ⇒ 这一条红）");
                        var prodCtxB = cTab.BuildContext(1);
                        CheckTrue(prodCtxB.BaseCollected && !prodCtxB.Claimable,
                                  "★ …再组一次 context ⇒ `BaseCollected` **翻真了**、`Claimable` 翻假"
                                  + "（= 它读的是**真进度**，不是写死的值）");
                        Check(prodWin.CurrentState, WindowState.Closed,
                              "★★ A469：**领到 ⇒ 窗自动关**（A448 那一条，这次走的是**生产那份 context**）");
                        Check(RewardWindowFixture.DismissRewardWindows(), 1,
                              "★ …并收掉 A438 弹的那扇领奖窗（关它才会走 `NotifyClosed` → `ShowPreviousWindow`"
                              + " 把压到 `Background` 的底窗带回 `Open`）");
                        Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                              "★ …现在场上**一扇遗留的领奖窗都没有**（下面 `02b_战役奖励窗.png` 才拍得干净）");
                    }
                }

                CampaignData.ResetForTest();      // 复位，后面的截图要用起手态
                cTab.RefreshNodes();
                // 上面那次点击**真开了一个奖励窗**（弹窗）——清掉，免得它盖住后面的截图。
                // 🔴 **2026-10-14（A512）**：口径统一到共用那一份 —— 原先这里是「按 `wm2.popUpWindow`
                //    这个**字段**关一扇」（那一刻那个字段装的**可能正是被测的那扇窗**）⇒ 改成**按【类型】清**、
                //    两类模态宿主（`PopUpGameWindow` / `PromptPopup`）一起覆盖；定义只有一份，
                //    见 `Editor/MainMenuScene.cs` 的 `CloseModalPopups()` 文档注释（⛔ 别在这儿再抄一份）。
                //    ⚠️ 覆盖范围如实记着：`CampaignRewardWindow`（上面那句注释说的那扇）**不在**它清的范围里
                //      —— 它由上面那个 `OpenCampWin()` 循环 + `DismissRewardWindows()` 收干净（本块已断言 0）。
                MainMenuScene.CloseModalPopups();
            }
            if (camp != null && camp.GetComponent<CampaignTab>() != null)
                Debug.Log(P + "   " + camp.GetComponent<CampaignTab>().Dump());
        }

        // ============================================================ §三·d 战役奖励窗
        // 期望值全部来自**原版参数**（正本 `资料/阶段二_锻造厂与战役页_原版规格.md` §十四）
        // 与**反编译**（`CampaignRewardsWindow__Open.c` / `__ConfigureIsPreviewState.c` / `CampaignUnlockButton__*.c`）。
        Section("§三·d `Campaign Reward Window`（第 3 层第 3 件）：层 × 参数逐条对");
        {
            // ---- 窗口参数（MB `7664330643585539206.json` 原文）----
            var cw = CampaignRewardWindow.Create(wm2);
            Check(cw.type, WindowType.Popup, "`type` = 1 Popup（原文）");
            Check(cw.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（原文）");
            Check(cw.closeOnEsc, true, "`closeOnESC` = 1（原文）");
            CheckNear(cw.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

            // ---- UM0：基础档 1 件（`UM_SK_Starter`）· 高级档 1 件（`DT Ultramarines R4`）----
            var ctx0 = new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(0),
                BaseCollected = false, PremiumCollected = false, Claimable = true,
                IsPremiumLocked = false, PointCost = CampaignData.At(0).Cost, Army = 10,
            };
            Check(ctx0.Rewards.Length, 2, "UM0 的奖励 = **2 条**（照 SO；正本 §十二）");
            wm2.OpenWindow(cw, ctx0);
            // 🆕 A83②（A81 的尾巴）：压暗层（「点窗外关窗」）那条不变量 —— 档 = **压暗层自己那一档**
            //   `QShade`(3110)，**严格低于**本窗内容命中区档 `QUnlockBg`(3118)；并核「这节点确实是
            //   公共件 `MenuDraw.ShadeHit` 建的」。🔴 这条命中区是本窗**唯一**的关窗路径（本窗没有独立关窗钮
            //   —— 见 `Shell/CampaignRewardWindow.cs:246-260` 的注释）⇒ 它不在就等于**点哪都关不掉这扇窗**。
            //   期望值全是本窗自己的**原版档常量**（⛔ 不从被测实现里读）。
            //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档（它由窗口自己那句建）。
            MenuDraw.CheckShadeRule(CheckTrue, "战役奖励窗", cw.ShadeHit,
                                    cw.transform.Find("Menu Dark Background"), CampaignRewardWindow.QUnlockBg);
            var croot = cw.transform;

            // ---- 根下三层：压暗 / 内容 / 暗角 ----
            CheckTrue(FindChild(croot, "Menu Dark Background") != null, "`Menu Dark Background` 建了");
            CheckNear(TintOf(FindChild(croot, "Menu Dark Background")).a, 0.773f, 0.005f,
                      "压暗层 alpha = **0.773**（原版 `m_Color` 原文）");
            CheckNear(TintOf(FindChild(croot, "Menu Dark Background")).r, 0f, 0.005f, "压暗层是纯黑");
            CheckRectPx(FindPath(croot, "Content"), 0f, 1920f, 165f, 965f,
                        "`Content`（`N(1,0,.5,1,.5,.5,.5, 0,−25, 0,800)` ⇒ y 165..965）");
            var vig = FindChild(croot, "Menu Vignette");
            CheckRectPx(vig, 0f, 1920f, 0f, 1080f, "`Menu Vignette` **铺满整屏**");
            CheckNear(TintOf(vig).a, 0.58f, 0.005f, "暗角 alpha = **0.58**（原版 `m_Color` 原文）");

            // ---- 出厂态 = **Preview**（`Glow Preview reward` 出厂 ACT、`Glow Get reward` 出厂 INACT）----
            CheckTrue(cw.IsPreview, "UM0 未领 ⇒ **Preview 态**（`__Open.c` 的 bVar9 分支）");
            CheckTrue(FindPath(croot, "Content/Reward Background Preview Reward") == null
                      || FindPath(croot, "Content/Reward Background Preview Reward").gameObject.activeSelf,
                      "Preview 态：`Reward Background Preview Reward` 开着");
            var gGet = FindPath(croot, "Content/Title/Glow Get reward");
            var gPre = FindPath(croot, "Content/Title/Glow Preview reward");
            CheckTrue(gGet == null || !gGet.gameObject.activeSelf, "Preview 态：`Glow Get reward` **关着**");
            CheckTrue(gPre != null && gPre.gameObject.activeSelf, "Preview 态：`Glow Preview reward` **开着**");
            // 标题文案：**prefab 里的 TMP 原文**（`ConfigureIsPreviewState` 只开关、不改文字）
            var ptNode = FindPath(gPre, "Text Preview Reward");
            CheckTrue(TextOf(ptNode) == CampaignRewardWindow.TxtPreview,
                      $"Preview 标题写的是 `{CampaignRewardWindow.TxtPreview}`（prefab TMP 原文）"
                      + $"—— 实测节点 {(ptNode != null ? "在" : "**不在**")}、文字「{(ptNode != null ? TextOf(ptNode) : "?")}」");

            // ---- 两列 holder：基础列**右边贴** 列右−65、高级列**左边贴** 列左+65 ----
            // 🔴🔴 **2026-10-13（A537）就地更正（铁律 5）：这一段原来的模型整个翻了。**
            //   本段原文写「内容宽 = padL30 + Σ子件 + spacing25×(n−1) + padR30」，并把
            //   **按钮 245 + 徽标 100** 也算成子件 ⇒ 当时的期望值是 **530 / 655**。**那是错的**：
            //   原版那三颗件（`Unlock Button` / `Warning` / `Badge`）**全都带 `LayoutElement.m_IgnoreLayout = 1`**
            //   ⇒ **一颗都不参与** holder 的 `HorizontalLayoutGroup`。三条互证（本件逐条亲读）：
            //     ① prefab 原文（`bundle_menus_assets_all`）：三颗件各自的 `LayoutElement.m_IgnoreLayout = 1`
            //        （`Unlock Button` 两列各一颗 · `Warning` · `Badge`）；
            //     ② uGUI 源码：`LayoutGroup.CalculateLayoutInputHorizontal()` 建 `m_RectChildren` 时**跳过**
            //        `ILayoutIgnorer.ignoreLayout == true`（`PackageCache/com.unity.ugui@27635d171b1a/
            //        …/Layout/LayoutGroup.cs:52-79`；`LayoutElement : ILayoutIgnorer`）
            //        —— 而 `m_RectChildren` 是**算尺寸与摆位置唯一用的那张表**；
            //     ③ 几何：prefab 里 holder 自己只有 **60 宽**（= padL30 + padR30 + **0 个子件**），
            //        而按钮**居中在 holder 中心、底边离底 15px**、徽标落在 holder **左上角**（连 padL 都没加）
            //        —— 布局组**摆不出**这两种。
            //   ⇒ **内容宽只由物品抽屉决定**：**1 物品 = 30 + 200 + 30 = 260**（0 物品 = 60 = prefab 实读值）。
            //   ⚠️ 其中 **200 是「我们挑的」**（`CampaignRewardWindow.ItemW` 的注释：原版那一格在抽屉 prefab 里、
            //      我们还没照它量）；本段其余每个数都是原版读数。
            float baseW = 30f + CampaignRewardWindow.ItemW + 30f;
            float premW = 30f + CampaignRewardWindow.ItemW + 30f;
            CheckNear(cw.BaseHolderRect.x2, 960f - 65f, 0.5f, "基础列 holder 的**右边缘 = 895**（列右 − 65）");
            CheckNear(cw.BaseHolderRect.W, baseW, 0.5f,
                      $"★★ A537：基础列 holder 内容宽 = **{baseW}**（**只有 1 个物品**：padL 30 + 200 + padR 30）"
                      + " —— A537 之前这里写的是 **530**（多算了 `UnlockW`(245)）");
            CheckNear(cw.PremHolderRect.x1, 960f + 65f, 0.5f, "高级列 holder 的**左边缘 = 1025**（列左 + 65）");
            CheckNear(cw.PremHolderRect.W, premW, 0.5f,
                      $"★★ A537：高级列 holder 内容宽 = **{premW}**（同上：`Warning` / `Badge` / 按钮**一颗都不进表**）"
                      + " —— A537 之前这里写的是 **655**；把那三颗件再塞回表里 ⇒ **980**、右边缘 **2005 出屏**");
            CheckTrue(cw.BaseHolderRect.x1 >= 0f && cw.PremHolderRect.x2 <= 1920f,
                      $"两列的 holder **都落在屏幕里**（基础 {cw.BaseHolderRect.x1:F0}..{cw.BaseHolderRect.x2:F0} · "
                      + $"高级 {cw.PremHolderRect.x1:F0}..{cw.PremHolderRect.x2:F0}）");

            // ---- 物品格数 = context 里的条数（一列一个 tier）----
            var bh = FindPath(croot, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards");
            var ph = FindPath(croot, "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");
            Check(CountByPrefix(bh, "Item_"), 1, "基础列画出 **1 个物品格**（UM0 基础档只有 1 条）");
            Check(CountByPrefix(ph, "Item_"), 1, "高级列画出 **1 个物品格**");
            CheckArt(FindChild(bh, "Unlock Button"), "UI_Button_Mulligan", "`Unlock Button` 的底图（Simple）");
            CheckArt(FindChild(ph, "Badge"), "40k_campaign_Premium-icon", "高级列的 `Badge`");

            // ============================================================ 🆕 **2026-10-13（A537 + A239 重做）**
            // **那三颗件的落点 = 锚点驱动**（它们 `m_IgnoreLayout = 1` ⇒ 不进 holder 的布局组；判据三条见上）。
            // 🔴 期望值全是**原版读数**（prefab 五元组，⛔ 不读 `CampaignRewardWindow` 的常量）：
            //   · `Unlock Button`：`anchor = pivot = (0.5,0)` · `pos (0,15)` · `245×45`
            //     ⇒ **holder 底部居中**、**底边 = holder 底 − 15**；
            //   · `Badge`：`anchor = pivot = (0,1)` · `pos (0,0)` · `100×100` ⇒ **holder 左上角**
            //     （**连 padL(30) 都没加**）。
            // 🔴 **改坏法**：把这两颗塞回 `BuildColumn` 的宽度表（`ws` 里加 `UnlockW` / `BadgeSize`）
            //   ⇒ 下面四条**一起红**（按钮被推到物品右边、徽标被推到按钮右边）。
            {
                const float itemW = 200f;    // ⚠️ **我们挑的**（原版那一格在抽屉 prefab 里，见上一段）
                const float padL = 30f, padR = 30f, btnW = 245f, btnH = 45f,
                            btnBottom = 15f, badgeSz = 100f;
                float hx1, hy1, hx2, hy2, bx1, by1, bx2, by2;
                CheckTrue(RectOfUnion(ph, out hx1, out hy1, out hx2, out hy2),
                          "★ A537：（前提）高级列 holder 量得到**渲出来**的并集（列底那张九宫格铺满 holder 矩形）");
                var ubg = FindPath(ph, "Unlock Button/bg");
                CheckTrue(RectOf(ubg, out bx1, out by1, out bx2, out by2),
                          "★ A537：（前提）`Unlock Button/bg` 量得到渲染矩形"
                          + "（⛔ 别用 `RectOf(FindChild(ph, \"Unlock Button\"))` —— 那个会先取子树里第一个 `Label`）");
                if (RectOfUnion(ph, out hx1, out hy1, out hx2, out hy2)
                    && RectOf(ubg, out bx1, out by1, out bx2, out by2))
                {
                    CheckNear(by2, hy2 - btnBottom, 1.5f,
                              $"★★ A537①：`Unlock Button` 的**底边 = holder 底 − 15**"
                              + "（原版 `anchorMin = anchorMax = (0.5,0)` · `pivot (0.5,0)` · `pos (0,15)`）"
                              + $"—— holder 底 {hy2:F1} ⇒ 应 {hy2 - btnBottom:F1}，实测 {by2:F1}");
                    CheckNear((bx1 + bx2) * 0.5f, (hx1 + hx2) * 0.5f, 1.5f,
                              "★★ A537②：它**水平居中在 holder 里**（原版 `anchorMin.x = anchorMax.x = 0.5`）"
                              + $"—— holder 中心 {(hx1 + hx2) * 0.5f:F1}，实测按钮中心 {(bx1 + bx2) * 0.5f:F1}"
                              + "；⛔ 布局组**永远摆不出水平居中**（它是从容器左沿 + `padL` 起排的）");
                    CheckNear(bx2 - bx1, btnW, 2f,
                              "★ A537②：…而且宽 = **245**（原版 `m_SizeDelta.x`）");
                    CheckNear(by2 - by1, btnH, 2f,
                              "★ A537②：…高 = **45**（原版 `m_SizeDelta.y`）");
                    CheckNear(hx2 - hx1, padL + itemW + padR, 0.5f,
                              "★★ A537③：holder **渲出来的**内容宽 = **260**（= `padL 30 + 那 1 个物品 200 + padR 30`）"
                              + $"—— 实测 {hx2 - hx1:F1}；把 `UnlockW`(245) / `BadgeSize`(100) 塞回表 ⇒ 505 / 605");
                }
                var bdg = FindPath(ph, "Badge/img");
                if (RectOfUnion(ph, out hx1, out hy1, out hx2, out hy2)
                    && RectOf(bdg, out bx1, out by1, out bx2, out by2))
                {
                    CheckNear(bx1, hx1, 2f,
                              "★★ A537④：`Badge` 的**左沿 = holder 的左沿**（原版 `anchor/pivot (0,1)` · `pos (0,0)`）"
                              + $"—— holder 左 {hx1:F1}，实测 {bx1:F1}"
                              + "；⛔ 布局组摆出来的一定是 `holder 左 + padL(30)`");
                    CheckNear(by1, hy1, 2f,
                              "★★ A537④：…**上沿 = holder 的上沿**（同一个五元组的 y 那一半）"
                              + $"—— holder 上 {hy1:F1}，实测 {by1:F1}"
                              + "（⚠️ 容差 2px：那颗 `Image` 原版就带 `m_PreserveAspect = 1`，会按图的比例内缩 0.66px）");
                    CheckNear(bx2 - bx1, badgeSz * (301f / 305f), 2f,
                              "（A537 附）徽标**渲出来的宽 = 100 × 301/305 = 98.7**"
                              + "（原版 `m_PreserveAspect = 1` + 原图 **301×305**；⛔ 不是「我们把它画成 100」）");
                }

                // 🔴 **就地更正（铁律 5）**：本段位置上原来那两条断的是
                //   「物品图标与 `Unlock Button` **竖向中心对齐**（都在列的内容区里居中）」——
                //   **在原版几何下那是错的**：物品走 `MiddleCenter`（内容区 `[295+25, 910−85]` 的中心
                //   = **572.5**），而按钮是 `anchor/pivot (0.5,0)` + `pos (0,15)` ⇒ 中心 y = **872.5**
                //   ⇒ **两者差 300px**。它当年之所以「绿」，是因为 `RectOf(…"Unlock Button")` 会先取
                //   子树里**第一个 `Label`**（= `Unlock Button/Point Count`，它竖直居中在按钮里）
                //   —— **量到的是按钮、不是「对齐」**（弱断言分不出两种状态）。
                //   ⇒ 现在两半分钉：① 物品图标居中在**内容区中心**（下面这条）；② 按钮**底边贴 holder 底 − 15**（A537①）。
                float ix1, iy1, ix2, iy2, ux1, uy1, ux2, uy2;
                var icon537 = FindChild(bh, "IconPlaceholder") ?? FindChild(bh, "Icon");
                CheckTrue(RectOf(icon537, out ix1, out iy1, out ix2, out iy2),
                          "★ A537：（前提）基础列物品格里有图标（或占位板）");
                CheckTrue(RectOf(FindPath(bh, "Unlock Button/bg"), out ux1, out uy1, out ux2, out uy2),
                          "★ A537：（前提）基础列的 `Unlock Button/bg` 也量得到");
                if (RectOf(icon537, out ix1, out iy1, out ix2, out iy2)
                    && RectOf(FindPath(bh, "Unlock Button/bg"), out ux1, out uy1, out ux2, out uy2))
                {
                    CheckNear((iy1 + iy2) * 0.5f, (295f + 25f + 910f - 85f) * 0.5f, 2f,
                              "★ A537⑥：物品图标**居中在 holder 的内容区中心**"
                              + "（原版 `MiddleCenter` + `padT 25` / `padB 85` ⇒ cy = **572.5**）");
                    CheckTrue(!Overlaps(ix1, iy1, ix2, iy2, ux1, uy1, ux2, uy2),
                              $"★ A537⑥：物品图标**不压**那颗按钮（图 x {ix1:F0}..{ix2:F0} · "
                              + $"按钮 x {ux1:F0}..{ux2:F0}）—— 它在列底，离物品 300px");
                }
            }

            // ---- 🆕 A537（**两种状态**）：内容宽 = `padL + Σ物品 + spacing×(n−1) + padR` —— 换 n 就换值 ----
            //   上半段只量了 n = 1（260）。这一条换 **n = 2**（UM2：基础档 2 条、高级档 0 条 ⇒ **只有一列**）
            //   ⇒ 期望 **30 + 200×2 + 25 + 30 = 485**，且**只有一列 ⇒ 居中**（原版 `CenterHolder`）。
            //   ⚠️ 单点断言（只量 260）分不出「算出来的」与「写死的常量」—— 这一条就是那个刻度。
            {
                var cw537 = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cw537, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(2), BaseCollected = false, PremiumCollected = false,
                    Claimable = true, IsPremiumLocked = false, PointCost = CampaignData.At(2).Cost, Army = 10,
                });
                Check(CampaignData.RewardsOf(2, CampaignData.TierBasic).Length, 2, "（A537 前提）UM2 基础档 **2 条**");
                Check(CampaignData.RewardsOf(2, CampaignData.TierPremium).Length, 0, "（A537 前提）UM2 高级档 0 条");
                CheckNear(cw537.BaseHolderRect.W, 30f + 200f * 2f + 25f + 30f, 0.5f,
                          "★★ A537⑤：**2 个物品** ⇒ holder 内容宽 = **485**（30 + 200×2 + 25 + 30）"
                          + " —— 与 n = 1 的 260 一起把「算式」钉成两态（⛔ 写死的 260 在这里就露了）");
                CheckNear(cw537.BaseHolderRect.CX, 960f, 1.0f,
                          "★ A537⑤：只有一列 ⇒ holder 居中在 **960**（原版 `CenterHolder`；n 变了它也不动）");
                cw537.Close();
            }

            // ---- 未领 + `PointCost = 100 ≥ 1` ⇒ 走「付点解锁」那一态：显点数、显 pointDrawer ----
            CheckTrue(FindPath(bh, "Unlock Button/Icon Campaign Points Drawer Variant") != null
                      && FindPath(bh, "Unlock Button/Icon Campaign Points Drawer Variant").gameObject.activeSelf,
                      "`PointCost ≥ 1` ⇒ `Icon Campaign Points Drawer Variant` **开着**（= 原版 `ToggleTexts(true)` 开 pointDrawer）");
            var costLb = FindPath(bh, "Unlock Button/Point Count");
            CheckTrue(costLb != null && TextOf(costLb) == "100",
                      $"按钮上的点数写的是**这一格的 `Cost`**（原版 `SetUnlockCost` 的 `cost.ToString()`）—— 实测「{(costLb != null ? TextOf(costLb) : "?")}」");
            // 🔴 **量渲染真值**（这一条是「`localScale` 要烘进子件矩形」那个坑的判据）：
            //    图标节点出厂 `localScale = 1.5`，**给父设 scale 再照常摆子件 ⇒ 位置与大小同时偏 1.5×**
            //    （2026-09-23 实拍：画成 150²、往右偏了半个按钮，而**所有矩形断言全绿**）。
            var ubtn = FindPath(bh, "Unlock Button");
            var pico = FindPath(bh, "Unlock Button/Icon Campaign Points Drawer Variant");
            CheckTrue(pico != null && ubtn != null, "`Icon Campaign Points Drawer Variant` 建了");
            if (pico != null && ubtn != null)
            {
                float ubtnLeft = PxOf(ubtn.position.x) - Wpx(ubtn) * 0.5f;
                CheckNear(Wpx(pico), CampaignRewardWindow.PtIconBox * CampaignRewardWindow.PtIconScale, 2f,
                          "战役点图标的**渲染宽 = 45 × 1.5 = 67.5**（缩放烘进矩形，不是给父设 scale）");
                CheckNear(PxOf(pico.position.x) + Wpx(pico) * 0.5f, ubtnLeft + 145f, 2f,
                          "图标的**右边缘 = 按钮左边 + 145**（pivot (1,0.5) + `pos.x = −77.5` + scl 1.5 实算）");
            }

            // ============================================================ 🆕 **2026-10-15（A538）**
            // 🔴🔴 **【高级列】永远免费态 —— 它【不显点数】。**（本件补的**判别式**断言；生产侧
            //   `Shell/CampaignRewardWindow.cs` 的 `else if (!isBase || ctx.PointCost < 1)` 已由 W18 收好。）
            // 🔴 **判据只此一份** —— 全在 `Shell/CampaignRewardWindow.cs` 那一句**紧上方**的判据段里
            //   （三条原文：`SetUnlockCost` 零调用点 · `SetPremiumButton.c` 全文不读 `ctx + 0x30` ·
            //   付点是 `SetBaseButton.c` **内联**的）⇒ ⛔ **别在这里抄第二份**，改判据只改那一处。
            // 🔴 **判别式**：同一个 `ctx0`（`BaseCollected=false` · `Claimable=true` · `PointCost=100`）下，
            //   基础列 = 付点态、高级列 = 免费态 —— 把 `!isBase ||` 删回 `ctx.PointCost < 1`
            //   （= W18 之前的写法）⇒ **①② 一起红**（高级列读到 `"100"` / `""`）。
            // 🔴 **对照组**（灭自证）**就在紧邻上方**、**同一次 `ctx0` / 同一个 run**：
            //   · 基础列 `Unlock Button/Icon Campaign Points Drawer Variant` **开着**（本段之上那一条）；
            //   · 基础列 `Unlock Button/Point Count` **仍是 `"100"`**（同理，再往上两条）。
            //   ⇒ 那两条**堵死「两列一起改成免费态」**这条路（那时 ① 照样绿、它们红）——
            //     它们**已经是断言**，⛔ **别在这里再写一份**（同一条规则两处写 = 迟早不一致）。
            // ⚠️ **⛔ 别把 ① 改写成「高级列那颗点图标节点不在」** —— 锁定支 / 已领支它**也不在**
            //   （三态分不开，`pi.gameObject.SetActive(false)` 在三个分支里都有）。
            {
                var pCostLb = FindPath(ph, "Unlock Button/Point Count");
                // 期望值是**空串**（⛔ 不是「节点不在」—— 节点必须在，只是在付费支才有字）：
                // 写坏法见下；`pCostLb != null` 那半堵死「找不到节点 ⇒ 读到 null ⇒ 被当成空串」。
                CheckTrue(pCostLb != null && TextOf(pCostLb) == "",
                          "★★ A538①：**高级列**那颗 `Point Count` 写的是**空串**"
                          + "（原版 `SetPremiumButton.c` 全文没读过 `ctx + 0x30`(PointCost)、"
                          + "也没有 `ToggleTexts(true)` / `costText.setText` ⇒ 那一列与点数无关）"
                          + $"—— 实测「{(pCostLb != null ? TextOf(pCostLb) : "**节点不在**")}」"
                          + "（⛔ 这里**空串**与**找不到节点**是两回事，别混）"
                          + "；改坏法：把 `Shell/CampaignRewardWindow.cs` 的 `!isBase ||` 删掉"
                          + " ⇒ 这里读到 `\"100\"` ⇒ 红");
                var pClaimedLb = FindPath(ph, "Unlock Button/Claimed Text");
                // 🔴 期望值写成**字面量**（⛔ 别读 `CampaignRewardWindow.TxtClaim` —— 那是拿被测常量
                // 证明被测实现 = 自证；字面量才是「原版那具方法体写死的那条词条」的独立判据）。
                CheckTrue(pClaimedLb != null && TextOf(pClaimedLb) == "Claim",
                          "★★ A538②：同一刻**高级列**那颗 `Claimed Text` 写的是 **`\"Claim\"`**"
                          + "（原版 `SetPremiumButton.c:40` 走 `SetAsFreeClaim` ⇒ `ToggleTexts(false)`"
                          + " ⇒ `claimedText = claimKey`）"
                          + $"—— 实测「{(pClaimedLb != null ? TextOf(pClaimedLb) : "**节点不在**")}」"
                          + "；改坏法：同上（删 `!isBase ||`）⇒ 高级列退回付费支 ⇒ `SetText(claimed, \"\")` ⇒ 红"
                          + "（= A538 之前高级列**一个字都不显**，两颗标签同时空着）");
            }

            // ============================================================ 🆕 **2026-10-13（A473 · 落 H31 §四·1）**
            // **A466：那颗 `Unlock` 钮的【可点性】** —— 本件之前**一条断言都没盯过它**
            // （既有那几条只看 `CheckArt` / `Point Cost`，**都不看可点性**）。
            // 🔴 **判据（第一权威 = 反编译方法体，H31 §三·1 逐句）**：`CampaignRewardsWindow__SetBaseButton.c`
            //   三条支给 `interactable` 的值**各不相同** ——
            //     · 已领（`ctx.BaseCollected`）⇒ `SetAsClaimed` ⇒ **假**；
            //     · **免费领**（`PointCost < 1`）⇒ `SetAsFreeClaim(btn, **1**)` ⇒ **真（硬编码，不看 `Claimable`）**
            //       （那个 `1` 直落 `set_interactable`：`CampaignUnlockButton__SetAsFreeClaim.c:15` 的第 2 个形参）；
            //     · **付点解锁**（else）⇒ `set_interactable(btn + 0x38, *(ctx + **0x2a**))` ⇒ `= ctx.Claimable`。
            //   ⇒ 我们这一侧照原文写成 `clickable = !premiumLocked && !claimedState && (ctx.PointCost < 1 || ctx.Claimable)`，
            //   并把那个布尔接到 `wb.Interactable`（= 原版 `Selectable.set_interactable` 的**两半**：变灰 + 挡派发）。
            // ⛔ **期望值全是原版读数**（上三条），不是我们自己的常量。
            // ⚠️ **这一刻手上是 `ctx0`**（`Claimable = true` · `PointCost = 100` ⇒ 走**付点**那一支）
            //   ⇒ 下面 ② 另建一扇**同格、同一个 `PointCost`、只把 `Claimable` 翻假**的窗来分辨；
            //     ③ 再建一扇 `PointCost = 0` 的钉住「免费支的硬编码真」。
            {
                // ---- ① 正例（`ctx0` · 付点支）：`Claimable = true` ⇒ **可点** ----
                var ub0 = FindPath(bh, "Unlock Button/Hit");
                var wb0 = ub0 != null ? ub0.GetComponent<WindowButton>() : null;
                CheckTrue(wb0 != null,
                          "★ A466①：（前提）`Unlock Button/Hit` 那颗节点上**直接**取得到 `WindowButton`"
                          + "（⛔ 别用 `GetComponentInChildren` —— 见 F8 那条教训：同节点多颗时会取到别人）");
                CheckTrue(wb0 != null && wb0.Interactable,
                          "★★ A466①：`Claimable = true`（付点支）⇒ 那颗钮 `interactable = true`"
                          + "（原版 `set_interactable(btn + 0x38, uVar1)`，`uVar1 = *(ctx + 0x2a) = Claimable`）"
                          + " —— 把 `|| ctx.Claimable` 写成 `&& !ctx.Claimable`（反向）⇒ 红");

                // ---- ② 负例（**同一格、同一个 `PointCost = 100`**，只把 `Claimable` 翻假）⇒ 不可点、**且变灰** ----
                var cwNo = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwNo, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(0), BaseCollected = false, PremiumCollected = false,
                    Claimable = false, IsPremiumLocked = false, PointCost = 100, Army = 10,
                });
                var bhNo = FindPath(cwNo.transform, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards");
                var uhNo = FindPath(bhNo, "Unlock Button/Hit");
                var wbNo = uhNo != null ? uhNo.GetComponent<WindowButton>() : null;
                CheckTrue(wbNo != null,
                          "★ A466②：（前提）那一扇的 `Unlock Button/Hit` 也取得到（取不到 ⇒ 下面两条等于没查）");
                CheckTrue(wbNo != null && !wbNo.Interactable,
                          "★★ A466②：`Claimable == false`（付点支）⇒ `interactable = false`"
                          + "（原版 `set_interactable(btn + 0x38, *(ctx + 0x2a))`）"
                          + " —— 删掉 `clickable` 里那句 `(ctx.PointCost < 1 || ctx.Claimable)` ⇒ 退回「恒定可点」⇒ 红"
                          + "（= A466 之前的现状，正是这一条要钉的）");
                // 🔴 这一条与上一条**必须并存**：②a 断「字段」，②b 断「画面真的灰了」——
                //   把 `wb.Interactable = clickable;` 挪到 `wb.Bind(...)` **之前** ⇒ 头一次 `RefreshGray()`
                //   只灰到那颗**透明的** `HitBox`，而 `GrayedForTest` 随即为真、后面再设只会早退
                //   ⇒ **字段是 false、画面上一像素都不变** ⇒ ②b 红而 ②a 照样绿。
                // ⚠️ 走的是**原版那张 shader 的名字**（`WindowButton.AuditGrayLook` 逐颗比的就是它）⇒ 不是自证。
                //   （同本文件 A82 那条的口径：`GrayShaderAvailable` 取不到时本文件早有一条会红。）
                CheckTrue(wbNo != null && wbNo.GrayedForTest,
                          "★★ A466②：…而且**真的灰了**（`GrayedForTest` 真 = 材质真换成 `Everguild/UI/Greyscale`，"
                          + "同本文件 A82 那条；⛔ 不是「我们打算灰」）"
                          + " —— 把 `wb.Interactable = clickable;` 挪到 `wb.Bind(bgQ, …)` **之前** ⇒ 这一条红、"
                          + "上面那条**照样绿**（⇒ 两条必须并存）");
                cwNo.Close();

                // ---- ③ 免费支（`PointCost = 0` **且** `Claimable = false`）⇒ **仍然可点** ----
                //   🔴 这一条专钉「**不是一律 `&& ctx.Claimable`**」：原版那一支是 `SetAsFreeClaim(btn, 1)`，
                //   那个 `1` 是**硬编码的真**、根本不看 `Claimable`。把条件简化 ⇒ ③ 红而 ①② 照样绿。
                //   ⚠️ **影响面如实说**：本战役 47 个节点的 `Cost` 全在 **100…1100**（`Shell/CampaignData.cs`）
                //     ⇒ 免费那一支**今天在真数据下走不到**（只有自检夹具喂 `PointCost = 0` 才到得了）——
                //     判据是原版原文（H31 §三·1 的 `:38-40`），按铁律 11 仍然要钉住它。
                var cwFree = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwFree, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(0), BaseCollected = false, PremiumCollected = false,
                    Claimable = false, IsPremiumLocked = false, PointCost = 0, Army = 10,
                });
                var bhFree = FindPath(cwFree.transform, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards");
                var uhFree = FindPath(bhFree, "Unlock Button/Hit");
                var wbFree = uhFree != null ? uhFree.GetComponent<WindowButton>() : null;
                CheckTrue(wbFree != null, "★ A466③：（前提）免费支那一扇的 `Unlock Button/Hit` 也取得到");
                CheckTrue(wbFree != null && wbFree.Interactable,
                          "★★ A466③：`PointCost = 0` **且** `Claimable = false` ⇒ **仍然可点**"
                          + "（原版 `CampaignUnlockButton__SetAsFreeClaim(btn, 1)` —— 硬编码的真，不看 `Claimable`）"
                          + " —— 把条件简化成「一律 `&& ctx.Claimable`」⇒ 这一条红（而 ② 照样绿）");
                CheckTrue(wbFree != null && !wbFree.GrayedForTest,
                          "★ A466③：…而且**没被灰掉**（免费支可点 ⇒ 不该灰；一并钉住「灰」那一半的极性）");
                cwFree.Close();
            }

            // ============================================================ 🆕 **2026-10-13（A539 = A402 + A472 · WC1 出稿）**
            // 两笔账：**A402**（锁定支 = 显 `Warning` + **整颗高级 `Unlock Button` 隐藏**）·
            //         **A472**（高级列 `interactable = ctx.BaseCollected`，「基础档领了才点得动」）。
            // 🔴 **期望值全是原版读数**（第一权威 = 反编译方法体）：
            //   `CampaignRewardsWindow__SetPremiumButton.c:81`（锁支 `SetActive(premiumWarning, 1)`）·
            //   `:85`（锁支 `SetActive(premiumUnlockButton, 0)`）·
            //   `:40`（不锁∧未领支 `SetAsFreeClaim(btn, *(ctx + 0x28))`，`0x28` = `BaseCollected`）·
            //   `CampaignUnlockButton__SetAsFreeClaim.c:15`（第 2 参直落 `set_interactable`）。
            // 🔴 **怎么自造锁定态**：真数据下 `IsPremiumLocked` 恒 `false`（用户 2026-09-22 裁决 ·
            //   `Shell/CampaignTab.cs` 里 `IsPremiumLocked = false` 那一句 写死）⇒ **只有夹具到得了那一支** —— 这正是它必须有断言的理由
            //   （本块之前，本窗的锁定态在自检里**零覆盖**：`grep "IsPremiumLocked = true"` 只命中领奖窗）。
            // 🔴 **2026-10-13（A537）落盘时改了两处期望值**（WC1 自己在 §五·2 预告过）：
            //   ① `PremHolderRect.W`：**710 → 260**；② `Badge` 渲染中心 x：**1655 → 1075**
            //   —— 那三颗件 `m_IgnoreLayout = 1` ⇒ **不进 holder 的宽度表**（判据见 §三·d 上面那段），
            //   徽标也就跟着从「按钮右边那一格」回到 **holder 的左上角**。
            //   ⚠️ 连带：③ 那条「锁定支的表不含 `UnlockW`」**已被 A537 吸收**（真判据是**任何支都不含**），
            //      ③b 那条「表里若还留着 `UnlockW` 徽标会被推到 1925」也随之失效 ——
            //      徽标的位置**不再由那张表决定**，改由 ④ 那族（`Badge` = holder 左上角）钉。
            {
                // ---- 夹具 A：**锁定态** ⇒ ①警告显示 ②按钮整个不在 ③holder 宽只有物品 ----
                var cwA402 = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwA402, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(0), BaseCollected = false, PremiumCollected = false,
                    Claimable = true, IsPremiumLocked = true, PointCost = 100, Army = 10,
                });
                var pLk = FindPath(cwA402.transform, "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");
                CheckTrue(pLk != null, "★ A402：（前提）锁定态下高级列的 `Rewards` holder 在");

                // ① `Warning`（原版 `SetPremiumButton.c:81`）
                var warnLk = FindPath(pLk, "Warning");
                CheckTrue(warnLk != null && warnLk.gameObject.activeSelf,
                          "★★ A402①：锁定态 ⇒ `Warning` **开着**（原版 `SetPremiumButton.c:81` `SetActive(premiumWarning, 1)`）"
                          + " —— 把 `Shell/CampaignRewardWindow.cs` 那句 `_warn.gameObject.SetActive(lockedHere)` 改回"
                          + " `SetActive(false)`（= 本块之前的写法）⇒ 红");
                CheckTrue(warnLk != null && TextOf(warnLk) == CampaignRewardWindow.TxtPremWarning,
                          $"★ A402①：警告写的是 `premiumUnlockWarning` 那条词条的占位"
                          + $"（`{CampaignRewardWindow.TxtPremWarning}`；原版 `SetPremiumButton.c:97-107` 锁支才 `set_text`）");
                // ①·b 🆕 A537：警告也是**锚点驱动**的（`anchor (0.1,0)-(0.9,0)` · `pivot (0.5,0)` · `pos (0,15)`）
                //   ⇒ 节点**锚在 holder 的水平中心**、中心 y = holder 底 − `15` − 半个高(22.5) = **872.5**
                //   （布局组摆出来的会是「从 holder 左沿 + padL 起」 ⇒ 这条能红）。
                //   ⚠️ 这里量的是**节点**（`MenuDraw.Node` 把节点摆在算出来的矩形中心），**不是**那颗文字的
                //     渲染矩形 —— 因为文字在框里怎么摆由 `align` 管、与 A537 无关（见报告 §九·1 那条顺手发现）。
                float px1, py1, px2, py2;
                if (warnLk != null && RectOfUnion(pLk, out px1, out py1, out px2, out py2))
                {
                    CheckNear(PxOf(warnLk.position.x), (px1 + px2) * 0.5f, 0.5f,
                              "★★ A537⑦：锁支的 `Warning` **锚在 holder 的水平中心**"
                              + "（原版 `anchorMin.x 0.1 / anchorMax.x 0.9` + `pivot 0.5`）"
                              + $"—— holder 中心 {(px1 + px2) * 0.5f:F1}，实测节点 x {PxOf(warnLk.position.x):F1}"
                              + "；把警告塞回宽度表（排在按钮右边）⇒ 红");
                    CheckNear(PxYOf(warnLk.position.y), py2 - 37.5f, 0.5f,
                              "★★ A537⑦：…中心 y = holder 底 − **37.5**（`pos.y` 15 + 半个高 22.5）"
                              + $"—— holder 底 {py2:F1} ⇒ 应 {py2 - 37.5f:F1}"
                              + " —— 与那颗按钮**同一行**（两者的 `pos` 在 prefab 里都是 `(0,15)`、高都是 45）");
                }

                // ② 整颗按钮【不在】—— 强断言：`activeSelf`（**弱断言分不出「隐藏 vs 变灰」**）
                var btnLk = FindPath(pLk, "Unlock Button");
                CheckTrue(btnLk != null,
                          "★ A402②：（前提）锁定态下 `Unlock Button` 的**节点还在**（原版 prefab 里这个节点在、只是 `SetActive(0)`）");
                CheckTrue(btnLk != null && !btnLk.gameObject.activeSelf,
                          "★★ A402②：锁定态 ⇒ 整颗 `Unlock Button` **`SetActive(false)`**（原版 `SetPremiumButton.c:85`）"
                          + " —— 🔴 这条与下面那条弱断言**必须并存**：`!wb.Interactable` 在「隐藏」与「只变灰」"
                          + "两种世界里**都是真**（= 弱断言），只有 `activeSelf` 分得开。"
                          + "删掉 `if (lockedHere) btn.gameObject.SetActive(false);` ⇒ 这条红、弱断言照样绿");
                var hitLk = FindPath(pLk, "Unlock Button/Hit");
                var wbLk = hitLk != null ? hitLk.GetComponent<WindowButton>() : null;
                CheckTrue(wbLk != null && !wbLk.Interactable,
                          "★ A402②（**弱**）：同一颗钮的 `interactable` 也是假 —— ⚠️ **单靠这条什么都证明不了**（见上）");

                // ③ holder 内容宽 = **只有那 1 个物品**（A537 之后它与锁定与否无关）
                CheckNear(cwA402.PremHolderRect.W, 260f, 0.5f,
                          "★★ A537③：锁定支的高级列 holder 内容宽 = **260**（1 物品 200 + pad 30/30 ——"
                          + " **那三颗件一颗都不进表**）"
                          + " —— 🔴 本块原写 **710**（`items + WarnW 300 + BadgeSize 100`），那是旧模型；"
                          + "把 `UnlockW`(245) / `BadgeSize`(100) / `WarnW` 塞回表 ⇒ 505 / 605 / 625 ⇒ 红");
                CheckTrue(cwA402.PremHolderRect.x2 <= 1920f,
                          $"★ A537③：锁定支的高级列 holder 落在屏内（{cwA402.PremHolderRect.x1:F0}..{cwA402.PremHolderRect.x2:F0}）");
                CheckNear(cwA402.BaseHolderRect.W, baseW, 0.5f,
                          "★ A402③：**基础列不受锁定支影响**（`IsPremiumLocked` 只在 `SetPremiumButton` 里被读）");
                // ③b 量**渲染真值**：`Badge` 在 **holder 的左上角**（A537 判据 ④ 的同一件事，
                //    这里换到**锁定态**那一扇再量一次 —— 顺带证明它与锁定支无关）
                float lkx1, lky1, lkx2, lky2;
                var badgeLk = FindChild(pLk, "Badge");
                CheckTrue(RectOf(badgeLk, out lkx1, out lky1, out lkx2, out lky2),
                          "★ A537③b：（前提）锁定支的 `Badge` 量得到渲染矩形");
                if (RectOf(badgeLk, out lkx1, out lky1, out lkx2, out lky2)
                    && RectOfUnion(pLk, out px1, out py1, out px2, out py2))
                {
                    CheckNear((lkx1 + lkx2) * 0.5f, px1 + 50f, 2f,
                              "★★ A537③b：`Badge` 的渲染中心 x = **holder 左沿 + 50**（原版 `anchor/pivot (0,1)` ·"
                              + " `pos (0,0)` · `100×100` ⇒ 贴 holder 左上角）"
                              + $"—— holder 左 {px1:F1} ⇒ 应 {px1 + 50f:F1}，实测 {(lkx1 + lkx2) * 0.5f:F1}"
                              + "（🔴 本块原写 **1655**：那是「徽标排在按钮右边」的旧模型）");
                    CheckNear(lky1, py1, 2f,
                              "★★ A537③b：…上沿 = **holder 的上沿**（同一个五元组的 y 那一半）");
                }
                cwA402.Close();

                // ---- 夹具 B：**不锁 ∧ 不可点** ⇒ 那颗钮【在】但【变灰】—— 与 ② 成对 ----
                var cwA402b = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwA402b, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(0), BaseCollected = false, PremiumCollected = false,
                    Claimable = false, IsPremiumLocked = false, PointCost = 100, Army = 10,
                });
                var pGr = FindPath(cwA402b.transform, "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");
                var btnGr = FindPath(pGr, "Unlock Button");
                var hitGr = FindPath(pGr, "Unlock Button/Hit");
                var wbGr = hitGr != null ? hitGr.GetComponent<WindowButton>() : null;
                CheckTrue(btnGr != null && btnGr.gameObject.activeSelf && wbGr != null && !wbGr.Interactable,
                          "★★ A402（对照）：**不锁**时同位置那颗钮 **在**（`activeSelf` 真）**且 `interactable` 假**"
                          + " ⇒ 与 ①② 合起来把「**隐藏**」与「**变灰**」钉成两种可分辨的状态"
                          + " —— 把锁定支改回「只 `Interactable = false`」⇒ 这条绿、②红（正是要的鉴别力）");
                CheckTrue(wbGr != null && wbGr.GrayedForTest,
                          "★ A402（对照）：而且**真的灰了**（材质换成 `Everguild/UI/Greyscale`，同 A466②b 的口径）");
                var warnGr = FindPath(pGr, "Warning");
                CheckTrue(warnGr != null && !warnGr.gameObject.activeSelf,
                          "★ A402（对照）：不锁 ⇒ `Warning` **关着**（原版 `SetPremiumButton.c:32`）");
                cwA402b.Close();

                // ---- 夹具 C/D：**A472** 那道门 ----
                System.Func<bool, bool, CampaignRewardWindow> mkA472 = (baseCollected, claimable) =>
                {
                    var w = CampaignRewardWindow.Create(wm2);
                    wm2.OpenWindow(w, new CampaignRewardsContext
                    {
                        Rewards = CampaignData.RewardsOf(0), BaseCollected = baseCollected, PremiumCollected = false,
                        Claimable = claimable, IsPremiumLocked = false, PointCost = 100, Army = 10,
                    });
                    return w;
                };
                System.Func<CampaignRewardWindow, bool, WindowButton> hitA472 = (w, baseTier) =>
                {
                    var h = FindPath(w.transform, "Content/Scroll View/Viewport/Content/"
                                    + (baseTier ? "Base Rewards" : "Premium Rewards") + "/Rewards/Unlock Button/Hit");
                    return h != null ? h.GetComponent<WindowButton>() : null;
                };

                // C：基础档**没领** ⇒ 高级那颗**点不动**（反例断言）；同一刻基础那颗可点
                var cwA472No = mkA472(false, true);
                var pbC = hitA472(cwA472No, true);
                var ppC = hitA472(cwA472No, false);
                CheckTrue(pbC != null && ppC != null, "★ A472：（前提）两列那颗 `Unlock Button/Hit` 都取得到");
                CheckTrue(pbC != null && pbC.Interactable,
                          "★★ A472：同一份 context（`Claimable = true` · `PointCost = 100`）⇒ **基础**那颗可点"
                          + "（原版 `SetBaseButton.c:84` `set_interactable(btn+0x38, *(ctx+0x2a))`）");
                CheckTrue(ppC != null && !ppC.Interactable,
                          "★★ A472（**反例**）：**基础档没领 ⇒ 高级那颗点不动**"
                          + "（原版 `SetPremiumButton.c:40` `SetAsFreeClaim(btn, ctx.BaseCollected)` ⇒"
                          + " `SetAsFreeClaim.c:15` 直落 `interactable`）—— 把高级列也按 A466 那套"
                          + "（`PointCost < 1 || Claimable`）算 ⇒ 这条红（那套在这里给的是真）");
                cwA472No.Close();

                // D：基础档**领了**（哪怕 `Claimable = false`）⇒ 高级那颗**可点**
                var cwA472Yes = mkA472(true, false);
                var pbD = hitA472(cwA472Yes, true);
                var ppD = hitA472(cwA472Yes, false);
                CheckTrue(pbD != null && !pbD.Interactable,
                          "★ A472：基础档已领 ⇒ **基础**那颗进 `SetAsClaimed`（`SetBaseButton.c:28-31`）⇒ 不可点");
                CheckTrue(ppD != null && ppD.Interactable,
                          "★★ A472：基础档已领 ⇒ **高级**那颗可点（`ctx.BaseCollected` 真）"
                          + " —— ⚠️ 这条与上面一条**同一枚 `Claimable`**（都是假）却给出相反的可点性"
                          + " ⇒ 两条合起来钉死「高级列读的不是 `Claimable`」");
                cwA472Yes.Close();
            }

            // ============================================================ 🆕 **2026-10-13（A496 · 落 H38 §六⑫）**
            // **A481：`Reward Claim` 那棵粒子子树在本窗里【建了、但恒不激活】**（原版 `Campaign Reward Window`
            // 那一份**没有任何驱动** —— H38 §五·5 逐句核过：原版两扇窗里同参数各有一份，
            // 只有 `Reward Window` 的 `Open()` 里那句 `SetActive(claimRewardParticles, !IsPreview)` 会开它）。
            // 🔴 **改坏法**：给 `Shell/CampaignRewardWindow.cs` 那一句 `SetVisible(_claimFx, false)` 也传 `true`
            //   （比如说成 `!IsPreview`）⇒ 这一条红。
            // ⚠️ **这一条今天与 `Ready` 正交**：本窗那句是硬编码 `false` ⇒ 与材质齐不齐无关
            //   （`RewardClaimFx.SetVisible(root, false)` 无论 `Ready` 是什么都关）。
            CheckTrue(cw.ClaimFx != null,
                      "★ A496⑫：（前提）`Campaign Reward Window` 里那棵 `Reward Claim` **建出来了**"
                      + "（`cw.ClaimFx` 是自检读口；`null` ⇒ 本窗那一半没接线）");
            CheckTrue(cw.ClaimFx != null && !cw.ClaimFx.activeSelf,
                      "★★ A496⑫：那棵子树**建了但恒不激活**（原版在这扇窗里没人驱动它）"
                      + " —— 给 `Shell/CampaignRewardWindow.cs` 也 `SetVisible(..., true)` ⇒ 红");

            // ---- 领完之后按钮进 claimed 态（`SetAsClaimed`：`ToggleTexts(false)` + 关 pointDrawer）----
            CampaignData.ResetForTest();
            var cw2 = CampaignRewardWindow.Create(wm2);
            var ctxC = new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(0), BaseCollected = true, PremiumCollected = false,
                Claimable = false, IsPremiumLocked = false, PointCost = CampaignData.At(0).Cost, Army = 10,
            };
            wm2.OpenWindow(cw2, ctxC);
            var bh2 = FindPath(cw2.transform, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards");
            CheckTrue(FindPath(bh2, "Unlock Button/Icon Campaign Points Drawer Variant") != null
                      && !FindPath(bh2, "Unlock Button/Icon Campaign Points Drawer Variant").gameObject.activeSelf,
                      "已领 ⇒ pointDrawer **关着**（原版 `SetAsClaimed` 走 `ToggleTexts(false)`）");
            var cl2 = FindPath(bh2, "Unlock Button/Claimed Text");
            CheckTrue(cl2 != null && TextOf(cl2) == CampaignRewardWindow.TxtClaimed,
                      $"已领 ⇒ `Claimed Text` 写的是 `claim**ed**Key` 那个词条（我们按 term 末段填 `{CampaignRewardWindow.TxtClaimed}`）");
            // 高级列没领 ⇒ 恒 Preview（`__Open.c`：只有基础列时 preview = !BaseCollected）
            CheckTrue(cw2.IsPreview, "基础已领、高级未领 ⇒ **仍是 Preview**（`bVar9` 的口径）");
            cw2.Close();

            // ---- 单列时**居中**（原版 `CenterHolder`）----
            // UM1：基础 1 条、高级 0 条 ⇒ 只有基础列 ⇒ 内容在**视口正中**（不是留在 0..960 那一半）
            var cw3 = CampaignRewardWindow.Create(wm2);
            var ctx1 = new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(1), BaseCollected = false, PremiumCollected = false,
                Claimable = true, IsPremiumLocked = false, PointCost = CampaignData.At(1).Cost, Army = 10,
            };
            Check(ctx1.Rewards.Length, 1, "UM1 只有 **1 条**奖励（且是基础档 ⇒ 高级列整列关掉）");
            wm2.OpenWindow(cw3, ctx1);
            CheckNear(cw3.BaseHolderRect.CX, 960f, 1.0f,
                      "只有一列 ⇒ holder **水平居中在 960**（原版 `CenterHolder` 把 holder **和它的父**一起改成锚 (.5,0)/(.5,1)）");
            CheckTrue(FindPath(cw3.transform, "Content/Scroll View/Viewport/Content/Premium Rewards") == null
                      || !FindPath(cw3.transform, "Content/Scroll View/Viewport/Content/Premium Rewards").gameObject.activeSelf,
                      "这一格没有高级档奖励 ⇒ **高级列整列关掉**（`__Open.c` 的 `SetActive(hasPrem)`）");
            cw3.Close();

            // ---- 两列都空 ⇒ 两列都关（`__Open.c` 的第 4 条分支）----
            var cw4 = CampaignRewardWindow.Create(wm2);
            wm2.OpenWindow(cw4, new CampaignRewardsContext
            {
                Rewards = new CampaignData.RewardSpec[0], IsPremiumLocked = false, PointCost = 0, Army = 10,
            });
            var bc4 = FindPath(cw4.transform, "Content/Scroll View/Viewport/Content/Base Rewards");
            var pc4 = FindPath(cw4.transform, "Content/Scroll View/Viewport/Content/Premium Rewards");
            CheckTrue(bc4 != null && !bc4.gameObject.activeSelf, "两列都空 ⇒ **基础列也关掉**");
            CheckTrue(pc4 != null && !pc4.gameObject.activeSelf, "两列都空 ⇒ **高级列也关掉**");
            CheckTrue(cw4.IsPreview, "两列都空 ⇒ Preview（原版默认分支）");
            cw4.Close();

            // ============================================================ 🆕 **2026-10-13（A469① · 落 H26 §三·1/§七）**
            // **A448「关窗三条路」的第 ① 条（领到 ⇒ 关窗）落地后【一条断言都没盯过它】**
            //   —— `§三·c` 那条链走的是**直调** `cTab.ClaimForTest(0, TierBasic)`（那句**刻意保留**：
            //   它验的是那个方法的返回值），**完全绕过了窗里那颗 `Unlock` 钮** ⇒ 新行为零覆盖。
            //   本块把那条路**真点**一遍（`PointerLayer` 真路径 —— ⛔ 不是直调 `OnUnlock`）。
            // 🔴 判据（第一权威 = 反编译方法体；逐句原文 → `Shell/CampaignRewardWindow.cs` 的 `OnUnlock` 方法头）：
            //   · 关窗在**领取成功那一拍** = `CampaignWindowTab.<TryCollect>g__Refresh_0` 末尾那句
            //     `WindowsManager.CloseWindow<CampaignRewardsWindow>()`；
            //   · **失败支不关** = `…_CollectRewards_g__OnFail_11_1.c` 里**没有**关窗那一跳；
            //   · **不是「两档全领完才关」** = 那个回调里**没有任何**「另一档也领了」的判据。
            // ⛔ 期望值全是**原版行为**（上三条），⛔ 不是读我们自己的常量/私有字段（不自证）。
            // ⚠️ 本块开头把上面那扇 `cw` 关掉，**为什么**：`PointerLayer` 的命中表只认「命中 quad 在不在
            //    激活链上」（`Shell/PointerLayer.cs` 的 `HitQuad`，**不认「窗在不在前台」** ——
            //    `ToBackground()` 只改 `CurrentState`）⇒ 场上同时有两扇 `CampaignRewardWindow` 时，
            //    两边的 `Unlock Button/Hit` 矩形与档**逐位相同** ⇒ 赢家退化成枚举顺序（静默、间歇）。
            //    本块结尾**不再开回来** —— 下面「实拍一张」那句 `wm2.OpenWindow(cw, ctx0)` 本来就会开它
            //    （它此刻是 `Closed` ⇒ 那一句真会走 `Open()` 重建，内容仍是 `ctx0` ⇒ 与旧行为同一张图）。
            cw.Close();
            {
                // 一扇窗的 `Unlock Button/Hit`（路径 = 本类 `BuildColumn` 建出来的那一串节点名）。
                Transform UnlockHit(CampaignRewardWindow w, bool baseTier)
                {
                    return FindPath(w != null ? w.transform : null,
                                    "Content/Scroll View/Viewport/Content/"
                                    + (baseTier ? "Base" : "Premium") + " Rewards/Rewards/Unlock Button/Hit");
                }
                // UM0 的 context（2 条奖励：基础 1 + 高级 1 —— 同 `ctx0`，只有那三个开关不同）。
                System.Func<System.Func<int, bool>, bool, bool, CampaignRewardsContext> camCtx =
                    (onCollect, baseClaimed, premClaimed) => new CampaignRewardsContext
                    {
                        Rewards = CampaignData.RewardsOf(0),
                        BaseCollected = baseClaimed, PremiumCollected = premClaimed, Claimable = true,
                        IsPremiumLocked = false, PointCost = CampaignData.At(0).Cost, Army = 10,
                        OnCollect = onCollect,
                    };

                // ---- ① 正例：**领到** ⇒ 窗自动关（另一档这一格里是**已领完**的，用来和 ③ 配对）----
                int callsA = 0, tierA = -1;
                var cwA = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwA, camCtx(t => { callsA++; tierA = t; return true; }, false, true));
                var uhA = UnlockHit(cwA, true);
                CheckTrue(uhA != null, "★ A469①：（前提）基础列那颗 `Unlock Button/Hit` 取得到"
                          + "（取不到 ⇒ 下面三条等于没查）");
                var uwA = uhA != null ? uhA.GetComponent<WindowButton>() : null;
                CheckTrue(ClickButtonByQuad(PointerLayer.Instance, uhA, uwA, "战役奖励窗基础列的 `Unlock`"),
                          "★ A469①：**真点**那颗 `Unlock`（走 `PointerLayer` 真路径 —— "
                          + "⛔ 不是直调 `OnUnlock` / `ClaimForTest`）");
                Check(callsA, 1, "★ …`ctx.OnCollect` **被调了一次**（那一下真的到了窗里那颗钮的处理函数上）");
                Check(tierA, CampaignData.TierBasic,
                      "★ …带出去的是**基础档**（把基础那颗接到 `TierPremium` 上的实现这里红）");
                Check(cwA.CurrentState, WindowState.Closed,
                      "★★ A469①：**领到 ⇒ 本窗自动关**（原版 `…g__Refresh_0` 末尾那一跳）"
                      + " —— 删掉 `Shell/CampaignRewardWindow.cs` 的 `if (_ctx.OnCollect(tier)) Close();` ⇒ 红"
                      + "（= A448 之前的现状，正是这一条要钉的）");

                // ---- ② 负例：**没领到** ⇒ 窗【不】关（本块分辨力最强的一条）----
                int callsB = 0;
                var cwB = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwB, camCtx(t => { callsB++; return false; }, false, false));
                var uhB = UnlockHit(cwB, true);
                CheckTrue(uhB != null, "★ A469②：（前提）那一扇的 `Unlock Button/Hit` 也取得到");
                var uwB = uhB != null ? uhB.GetComponent<WindowButton>() : null;
                CheckTrue(ClickButtonByQuad(PointerLayer.Instance, uhB, uwB, "战役奖励窗基础列的 `Unlock`（负例那扇）"),
                          "★ A469②：同一颗钮**照样点得动**（点不动 ⇒ 下面「窗还开着」就不是这条在管）");
                Check(callsB, 1, "★ …回调**被调了一次**（没被调同样是「上面那条不成立」）");
                Check(cwB.CurrentState, WindowState.Open,
                      "★★ A469②（负例）：**没领到 ⇒ 窗【不】关**"
                      + "（原版失败支 `…_CollectRewards_g__OnFail_11_1.c` 里没有关窗那一跳）"
                      + " —— 把 `Shell/CampaignRewardWindow.cs` 的 `UnlockClicked` 末句 改成 `_ctx.OnCollect(tier); Close();`"
                      + "（= **点了就关**）⇒ 红");
                cwB.Close();

                // ---- ③ 「另一档还有奖没领」 ⇒ **照样关**（钉住「不是两档全领完才关」）----
                //   ⚠️ 与 ① **成对**：① 的另一档是**已领完**、③ 的另一档是**还没领** —— 两条一起才
                //      钉得住「关门条件只看**这一次领取**的回包，不看另一档的状态」（只留一条会漏掉半边）。
                int callsC = 0;
                var cwC = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwC, camCtx(t => { callsC++; return true; }, false, false));
                var premColC = FindPath(cwC.transform, "Content/Scroll View/Viewport/Content/Premium Rewards");
                CheckTrue(premColC != null && premColC.gameObject.activeSelf,
                          "★ A469③：（前提）**另一档那一列真有没领的奖**（列关着 ⇒ 这一条等于没查）");
                var uhC = UnlockHit(cwC, true);
                CheckTrue(uhC != null, "★ A469③：（前提）基础列那颗 `Unlock Button/Hit` 取得到");
                var uwC = uhC != null ? uhC.GetComponent<WindowButton>() : null;
                CheckTrue(ClickButtonByQuad(PointerLayer.Instance, uhC, uwC, "战役奖励窗基础列的 `Unlock`（另一档没领）"),
                          "★ A469③：点的是**基础档**那颗");
                CheckTrue(callsC == 1, "★ …（前提）回调被调了一次");
                Check(cwC.CurrentState, WindowState.Closed,
                      "★★ A469③：**另一档（高级）还有奖没领** ⇒ 照样关 —— 钉住「**不是两档全领完才关**」"
                      + "（把关门条件写成 `if (BaseCollected && PremiumCollected) Close();` ⇒ 红）");
                // ⚠️ 本块**没碰真数据**（`OnCollect` 是夹具自己的 lambda，不走 `ClaimForTest`/
                //    `CampaignData.Claim`）⇒ 不需要复位；也**不要**在这里调 `CampaignData.ResetForTest()`
                //    —— 那会多一次没必要的状态改写（起手态由 `§三·c` 末尾那次复位负责）。
            }

            // ---- 奖励数据与节点表**同源**（`HasPremium` 与奖励表必须一致）----
            int nNodes, nRewards, nMismatch;
            CampaignData.SelfCheck(out nNodes, out nRewards, out nMismatch);
            Check(nNodes, 47, "节点表 **47** 个");
            Check(nRewards, 89, "奖励表 **89 条**（照 SO 生成；基础 70 / 高级 19）");
            Check(nMismatch, 0, "**每一格的 `HasPremium` 与奖励表一致**（同源复算，不一致就是抄错了）");

            // ---- 图缺不缺（不许静默）----
            Debug.Log(P + "   " + cw.Dump());
            CheckTrue(cw.NoIconItems.Count >= 0, "`NoIconItems` 清单存在（没有图标的物品**逐条打日志**，不是静默）");

            // ---- 实拍一张（断言测不出「像不像」；`资料/阶段二_锻造厂与战役页_原版规格.md` §十四）----
            // 把 UM0 那窗重新开出来再拍（前面几段换过 context）
            wm2.OpenWindow(cw, ctx0);
            Shoot("02b_战役奖励窗.png");
            Debug.Log(P + "   " + cw.Dump());

            // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
            //   期望矩形 = **原版 prefab** `Campaign Reward Window > Content >
            //   Reward Background Preview Reward` 那颗 `Image` 的 rect（0,165 → 1920,965）；
            //   ⛔ 不写 `CampaignRewardWindow.Content`（那是被测实现**传进去的实参**，同式自证）。
            CheckAbsorbRule("战役奖励窗", cw.transform, "AbsorbHit",
                            0f, 165f, 1920f, 965f,
                            CampaignRewardWindow.QShade, CampaignRewardWindow.QUnlockBg, () => cw.CurrentState);
            // ⚠️ 上面那一组**结尾就把窗关掉了** ⇒ 开回来，下面那句 `Close()` 才是**真**在关。
            // 🔴 **2026-10-14（A570 · WA505）就地订正（铁律 5）**：原来这句写的是「`CampaignRewardWindow.Open()`
            //   = `Build()` 重建，**`_ctx` 留着**，重开安全」—— **与代码相反**：这一句是**带参** `TryOpen(null)`，
            //   而 `WindowsManager.TryOpen(object)`（`Shell/WindowsManager.cs` 的 `GameWindow.TryOpen(object)`）**第一句就是 `SetupData(data)`**，
            //   本窗又**覆写了** `SetupData`（`Shell/CampaignRewardWindow.cs` 的 `SetupData`：
            //   `_ctx = data as CampaignRewardsContext`）⇒ **`_ctx` 被清成 `null`**，
            //   于是 `Open()` → `Build()` 画出来的是一扇**空窗**。
            //   今天无后果（下一句就 `cw.Close()`），但**若有人在这两句之间加断言 ⇒ 会静默量到空窗**。
            //   ⛔ 别改成无参 `TryOpen()`、也⛔ 别改成 `OpenWindow(cw, ctx0)` —— 那两条都被判过是错的
            //   （A447：调用方要按「原版那一跳调的是**哪条重载**」来选；这一句的语义就是「重开一次、马上关」）。
            CheckTrue(cw.TryOpen(null), "（A94 收尾）把战役奖励窗开回来 —— 下面那句 `Close()` 才不是空断");
            cw.Close();

            // 🆕 **2026-10-15（A796）**：压暗层「**点了会不会关**」—— 走公共口
            //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs` 的 `CheckShadeClickRule`）；
            //   逐站点表 / 与账上 24 的对账 → `资料/普查产出_1015/W7_A796调用点.md`。
            //   🔴 **本口会把窗【真的关掉】** ⇒ 必须排在**本窗其它断言之后**（这里就是本窗的收尾）；
            //      同族翻车留档 → 本文件 `:8715-8725`「探针跑在关着的窗上」⇒ ⛔ 别把本块往上挪。
            //   上面已经关过一次 ⇒ 这里先开回来：⚠️ 用**无参** `TryOpen()`（**不碰 `Data`**）——
            //      上一句带参 `TryOpen(null)` 已经把 `_ctx` 清成 null（见 `:5660-5668` 那段订正），
            //      本窗那一刻本来就是**空窗**，本口**不读内容**（只认 `ShadeHit` 节点 + 状态两跳）。
            CheckTrue(cw.TryOpen(), "（A796 现场）把战役奖励窗开回来 —— 下面那条要在**开着**的窗上点");
            MenuDraw.CheckShadeClickRule(CheckTrue, "战役奖励窗", cw.transform, cw.ShadeHit,
                                         () => cw.CurrentState);
            CampaignData.ResetForTest();

            // ============================================================ 🆕 **2026-10-08（A182）**
            // `Scroll View` / `Viewport` 的**视口裁切 + 软边** —— 原来 `CampaignRewardWindow.cs:62` 明文
            // 「不实现滚动；`RectMask2D` 也没做」，`:327` 还打一条出声日志，`ItemDrawerStyle.Clip = null`。
            // 判据（直读原版，⛔ 期望值写原版字面量）：
            //   · 结构 → `工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 5`：
            //     `Content` → `Scroll View`(`ScrollRect` **h=1 v=0 mode=1(Elastic)**) → `Viewport`(`RectMask2D`)
            //     → `Content`(CSF h:MinSize=0) → `Base Rewards` / `Premium Rewards`；
            //     `Scroll View` 与 `Viewport` **同矩形 0,285 → 1920,935**；
            //   · 掩码字段 → `d:/4/_tmp_view/q1_rm2d.txt`（`soft=(200,0) pad=(0.0,0.0,0.0,0.0) en=1`）。
            // 🔴 **为什么要人为加宽内容**：真数据两列都放得下（基础最多 2 件、高级最多 1 件）⇒ 这一层**永不现形**、
            //    断言就咬不住任何东西 ⇒ 造一个「高级列 **5** 件」的**加压 fixture**（只为验视口，不是实现里挑的数）。
            //    🔴 **2026-10-13（A537）就地更正（铁律 5）**：这段原来写「**4 件**的布局（照本窗自己的
            //    `HorizontalChild` 算式）：holder = 1025..**2355** ⇒ 右下两块越出视口」——
            //    **两个数都过期了**：那三颗件（`Unlock Button` / `Warning` / `Badge`）带
            //    `LayoutElement.m_IgnoreLayout = 1` ⇒ **不进 holder 的宽度表**（判据见 §三·d 那段）
            //    ⇒ 4 件时 holder = 1025..**1960**（= 30 + 4×200 + 3×25 + 30），右边只溢 40px；
            //    而**「整块在视口外」的子件一个都不剩**（按钮改走锚点、恒在 holder 中心 1370..1615）
            //    ⇒ 下面（二）那条会**变红**。⇒ 加压件从 3 加到 **4**（高级档共 5 件）：
            //    holder = 1025..**2185**（= 30 + 5×200 + 4×25 + 30），槽 4 那一格 = **1955..2155**（**整块在外**）。
            {
                var cwWide = CampaignRewardWindow.Create(wm2);
                var wide = new List<CampaignData.RewardSpec>();
                foreach (var r0 in CampaignData.RewardsOf(0)) wide.Add(r0);          // UM0 原有：基础 1 + 高级 1
                for (int i = 0; i < 4; i++)                                          // 加压：高级档再塞 4 件（⇒ 共 5 件）
                    wide.Add(new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierPremium));
                wm2.OpenWindow(cwWide, new CampaignRewardsContext
                {
                    Rewards = wide.ToArray(), BaseCollected = false, PremiumCollected = false, Claimable = true,
                    IsPremiumLocked = false, PointCost = 0, Army = 10,
                });

                var wVp = FindPath(cwWide.transform, "Content/Scroll View/Viewport");
                CheckTrue(wVp != null, "★ `Scroll View` 底下有 `Viewport` 那一层（掩码长在它身上）");
                CheckAt(wVp, 0f, 1920f, 285f, 935f,
                        "`Viewport` 摆在原版那个矩形里（与 `Scroll View` **同矩形** 0,285 → 1920,935）");

                // 🆕 **2026-10-11（A272）**：`Viewport` 底下那一层 `Content` —— 原版是**零宽点**
                //   **960,285 → 960,935**（`ContentSizeFitter m_HorizontalFit = 2(MinSize)` 撑出来的；
                //   两列由各自锚点半开），我们原来建的是「与 `Viewport` 同矩形的容器」（波 C1 报告 §四·2）。
                //   🔴 **如实标一处量不到的东西**：我们这套里 `MenuDraw.Node` **只吃矩形的中心**
                //   （它只写 `localPosition`、**不写 `sizeDelta`**）⇒ **「宽 0」这件事在场景里量不出来**
                //   —— 这一条能钉的只有**那个点的位置**（中心 960,610）。两条列的位置另有断言管
                //   （holder 右边缘 895 / 左边缘 1025 那几条），这里补的是**相对**那一半：
                //   **这个点正落在两条列中心的中线上**（谁把内容容器挪走 / 挪到别人身上 ⇒ 立刻红）。
                var wInner = FindPath(cwWide.transform, "Content/Scroll View/Viewport/Content");
                var wbCol = FindPath(cwWide.transform, "Content/Scroll View/Viewport/Content/Base Rewards");
                var wpCol = FindPath(cwWide.transform, "Content/Scroll View/Viewport/Content/Premium Rewards");
                CheckTrue(wInner != null && wbCol != null && wpCol != null,
                          "`Viewport` 底下那一层 `Content`（原版的**零宽点**）+ 两条列都在");
                if (wInner != null)
                {
                    CheckNear(PxOf(wInner.position.x), 960f, 0.5f,
                              "★ 那个零宽点的中心 x = **960**（原版 `Content` = 960,285 → 960,935）");
                    CheckNear(PxYOf(wInner.position.y), (285f + 935f) * 0.5f, 0.5f,
                              "…中心 y = **610**（同上，原版那个点的竖中）");
                }
                if (wInner != null && wbCol != null && wpCol != null)
                    CheckNear(PxOf(wInner.position.x),
                              (PxOf(wbCol.position.x) + PxOf(wpCol.position.x)) * 0.5f, 0.5f,
                              "★ …而且它**正落在两条列中心的中线上**（相对断言 —— 这条咬得住「内容容器"
                              + "被挪到别人身上」那一类；⚠️ 「宽 0」本身量不到，见上面那段）");

                var premRewards = FindPath(cwWide.transform,
                                           "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");
                CheckTrue(premRewards != null, "高级列的 `Rewards` holder 在");

                // ---- ★ 真裁住了（一）：越出视口的部分**不画** ----
                float hx1, hy1, hx2, hy2;
                CheckTrue(RectOfUnion(premRewards, out hx1, out hy1, out hx2, out hy2),
                          "高级列 holder 量得到渲染矩形");
                CheckNear(hx1, 1025f, 0.5f, "…左边缘 = **1025**（列左 960 + 65，原版锚点那个 65）");
                CheckTrue(hx2 <= 1920.5f,
                          $"★ 渲染并集**右边缘 ≤ 1920**（视口右沿；实测 {hx2:F1}）—— 未裁时它会画到 **2185**");
                CheckTrue(hx2 > 1720f,
                          $"…而且**确实越过了软边内沿 1720**（实测 {hx2:F1}）⇒ 说明是「内容真越界、被裁掉」，"
                          + "不是「内容本来就没到」（⛔ 这两件事必须分开看）");

                // ---- ★ 真裁住了（二）：整块在视口外的子件**一个 quad / 一个字都不建** ----
                // 🔴 **2026-10-13（A537）重判（本批「派活必查行」的第 ③ 条）**：本条原来钉的是
                //   `Unlock Button`（括号里写「整块落在视口外（**2135..2380**）」）—— **那个前提没了**：
                //   A537 查出那三颗件全都 `LayoutElement.m_IgnoreLayout = 1` ⇒ 按钮的落点是
                //   **holder 底部居中**（`anchor/pivot (0.5,0)` · `pos (0,15)` · `245×45`）
                //   ⇒ 它**恒在 holder 中心**；本档实测 **1370..1615**，**整块在视口内** ⇒ quads 会照建（本条会红）。
                //   ⇒ 改钉**槽 4 那个物品格**（左沿 **1955** 已在视口右沿 1920 之外）——
                //     它的每一层（卡面底图 / 阵营徽记 / 名条 / 数量）走的是**同一条** `MenuDraw.Visible` 判据。
                var wBtn = FindChild(premRewards, "Unlock Button");
                CheckTrue(wBtn != null,
                          "★ 那颗 `Unlock Button` 还在（原版它也在列里）—— ⚠️ 它**不再是「第 4 件的右邻」**"
                          + "（A537：它锚在 holder 底部居中，那一格恒在视口内 ⇒ 它自己**不能**当「整块出框」的例子）");
                Transform wOut = null;
                if (premRewards != null)
                    for (int ci3 = 0; ci3 < premRewards.childCount; ci3++)
                    {
                        var c3 = premRewards.GetChild(ci3);
                        if (!c3.name.StartsWith("Item_")) continue;              // 槽 4（第 5 格）：box 中心 x = 1025 + 30 + 4×(200+25) + 100 = **2055**
                        if (Mathf.Abs(PxOf(c3.position.x) - 2055f) <= 0.5f) { wOut = c3; break; }
                    }
                CheckTrue(wOut != null, "★ 真裁住了（二）前提：找得到**整块落在视口外**的那个物品格（box 中心 x = **2055**）");
                CheckTrue(wOut != null && wOut.GetComponentsInChildren<ImageQuad>(true).Length == 0,
                          "★★ 它整块落在视口外（**1955..2155** > 视口右沿 1920）"
                          + " ⇒ **卡面底图 / 阵营徽记一个 quad 都没建**（原版掩码下这一格连射线都吃不到）"
                          + " —— 把 `Shell/CampaignRewardWindow.cs` 的 `st.Clip = RenderClip;` 删掉 ⇒ 红");
                CheckTrue(wOut != null && wOut.GetComponentsInChildren<Label>(true).Length == 0,
                          "★★ …而且**名条 / 数量那些字也一个字都没建**"
                          + "（`ItemDrawer.ClippedText` 头一句就是 `MenuDraw.Visible`）—— 只删 `Clip` ⇒ 这条也红");

                // ---- ★ 软边 (200,0)：竖切线只在带的内沿 1720；一条横切线都没有 ----
                var WvVp_ = new PxRect(0f, 285f, 1920f, 935f);                        // `Content/Scroll View/Viewport`（同上一条 `CheckAt` 的四个字面量）
                var wCuts = ScanSoftCuts(premRewards, true, WvVp_);
                CheckTrue(wCuts.Count > 0,
                          "★ 有竖向渐隐切线（= 软边真接上了；一条都没有 ⇒ `ClipSoftness` 没设、`Clip` 单打独斗）");
                for (int i = 0; i < wCuts.Count; i++)
                    CheckTrue(Mathf.Abs(wCuts[i] - 1720f) <= 0.6f,
                              $"竖切线 #{i + 1} 在 {wCuts[i]:F2} ⇒ 必须落在带的内沿 `1920 − 200 = **1720**`"
                              + "（⚠️ 写成 `(0,200)` 会去切**横线**、写成 89 会切在 1831）");
                Check(ScanSoftCuts(premRewards, false, WvVp_).Count, 0,
                      "**一条横切线都没有** —— 这一处软边是 `(200,0)`：**y 方向是硬边**");

                // ============================================================ 🆕 **2026-10-11（A238）**
                // **物品抽屉那四层也要吃同一份软边** —— 原来 `ItemDrawerStyle` **只有 `Clip`、没有
                // `ClipSoftness`** ⇒ 同一个视口里列底/按钮/徽标是 `(200,0)` 渐隐、抽屉那几层却是**硬边截**
                // （判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §四·1；A238 那一行）。
                // 🔴 **为什么量【顶点色】而不是量切线**：第 4 个物品格（box 1730..1930、主图 140² 落在
                //   1760..1900）**整块落在渐隐带里**（带 = `1920 − 200 = 1720` 到 `1920`）——
                //   带内的块**不切几何、只按剖面乘四角 alpha**（`MenuDraw.ApplySoftEdges` 的「整块都在
                //   同一个线性段里」那一支）⇒ `ScanSoftCuts` 在它身上**一条都查不到**，得看顶点色。
                //   期望值 = **原版 `m_Softness = (200,0)` + 视口右沿 1920 + 那一块自己的左右沿**算出来的
                //   字面量（⛔ 不读 `CampaignRewardWindow.ScrollSoft`，那是被测实现）：
                //     左（x=1760）`(1920 − 1760) / 200` = **0.80**（内缩 40px 进带）
                //     右（x=1900）`(1920 − 1900) / 200` = **0.10**（离框边 20px）
                //   ⇒ **两条都 &lt; 1**（硬边那一版是 `CornerColors == null` = 全 1）＋**左 &gt; 右**（斜坡方向）。
                //   **改坏法**：把 `Shell/CampaignRewardWindow.cs` 的 `st.ClipSoftness = ClipSoftness;` 删掉
                //   （或把 `Shell/ItemDrawer.cs` 四条画路末尾的 `st.ClipSoftness` 实参去掉）⇒ 三条一起红。
                {
                    Transform wPick = null;
                    if (premRewards != null)
                        for (int ci2 = 0; ci2 < premRewards.childCount; ci2++)
                        {
                            var c2 = premRewards.GetChild(ci2);
                            if (!c2.name.StartsWith("Item_")) continue;
                            // 第 4 个物品格：box 中心 x = holder 左 1025 + padL 30 + 3 × (200 + 25) + 100 = **1830**
                            if (Mathf.Abs(PxOf(c2.position.x) - 1830f) <= 0.5f) { wPick = c2; break; }
                        }
                    CheckTrue(wPick != null,
                              "（A238 前提）找得到**压在渐隐带里**的那个物品格（box 中心 x = 1830）");
                    // ⚠️ 那一层可能是真图（`Icon`，`keepAspect` 会把矩形按图的比例内缩）或**占位板**
                    //   （`IconPlaceholder`，矩形就是 140²）—— 两种都接受（别的窗里两种都出现过，
                    //   `:3514` 那一条就是这么取的）⇒ 期望值**不能写死四角 alpha**，改成
                    //   「**量的几何 + 原版的 `m_Softness.x = 200`**」（见下）。
                    var wPickIcon = wPick != null
                        ? (FindChild(wPick, "Icon") ?? FindChild(wPick, "IconPlaceholder")) : null;
                    var wIconQ = wPickIcon != null ? wPickIcon.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(wIconQ != null, "（A238 前提）那一格的主图（或占位板）画出来了");
                    if (wIconQ != null)
                    {
                        var icc = wIconQ.CornerColors;
                        CheckTrue(icc != null && icc.Length == 4,
                                  "★ 抽屉那一层**有顶点色**（= 它真吃了 (200,0) 软边；`null` ⇒ 这一路还是**硬边截**，"
                                  + "与旁边列底/按钮的渐隐不一致）");
                        if (icc != null && icc.Length == 4)
                        {
                            float qx1, qy1, qx2, qy2;
                            CheckTrue(QuadRectOf(wIconQ, out qx1, out qy1, out qx2, out qy2),
                                      "（A238 前提）那一层的渲染矩形量得到");
                            // 顶点序 = BL · BR · TR · TL（照 `ImageQuad.RebuildMesh`，同本文件 ② 那一段）；
                            // 期望值 = **原版的剖面**（`MenuDraw.SoftAlpha` 的几何那半 = 原版 `RectMask2D`
                            // 语义）：`alpha(x) = clamp01((1920 − x) / 200)` —— 右沿 1920 + `m_Softness.x = 200`
                            // 都是**原版字面量**，x 是**量出来的角**（⛔ 不读 `CampaignRewardWindow.ScrollSoft`）。
                            //   ⚠️ 那块 quad 整块落在带里（左沿 ≥ 1760 > 带内沿 1720）⇒ 只削 alpha、不切几何，
                            //   `ScanSoftCuts` 在它身上查不到任何切线 —— 所以这里必须量**顶点色**。
                            CheckNear(icc[0].a, Mathf.Clamp01((1920f - qx1) / 200f), 0.01f,
                                      "★ …**左角 alpha = (1920 − 左沿)/200**（原版 `m_Softness.x = 200`；"
                                      + $"实测 {icc[0].a:F3}，左沿 {qx1:F1}）");
                            CheckNear(icc[1].a, Mathf.Clamp01((1920f - qx2) / 200f), 0.01f,
                                      "★ …**右角 alpha = (1920 − 右沿)/200**（由内向外递减；"
                                      + $"实测 {icc[1].a:F3}，右沿 {qx2:F1}）");
                            CheckTrue(icc[0].a < 0.999f && icc[1].a < 0.999f,
                                      "★ …两个角**都被削**（硬边那一版 `CornerColors == null` ⇒ 等于全 1 —— "
                                      + "这正是「同一条带里两种观感」那条欠账的判据）");
                            CheckTrue(icc[0].a > icc[1].a + 0.05f,
                                      "…斜坡方向：**左边比右边亮**（带是从视口右沿 1920 往内 200px 渐隐的；"
                                      + "写反成 `(0,200)` 会去削上下两条边 ⇒ 这里左右相等 ⇒ 红）");
                            CheckTrue(Mathf.Abs(icc[3].a - icc[0].a) < 0.001f && Mathf.Abs(icc[2].a - icc[1].a) < 0.001f,
                                      "…**y 方向是硬边**（`soft.y = 0` ⇒ 上角与同侧下角一样亮）");
                            CheckNear(icc[0].r, 1f, 0.001f,
                                      "…**只乘 alpha、rgb 不动**（乘了 rgb 会在 shader 的 `c.rgb *= c.a` 之外再暗一次）");
                        }
                    }
                }
                cwWide.Close();
            }
        }

        // ============================================================ 🆕 **2026-10-11（批次1 · 戊2 · A237）**
        // **原版 `Reward Window`**（pid 13701，类 `RewardWindow : GameWindow`）= **独立的全屏领奖窗**，
        // 本工程**从来没建**（判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §0·1：A182 那一行原来把
        // `(200,0)` 记在 `Shell/RewardsWindow.cs` 头上，而那是**另一扇窗** —— 两个名字只差一个 `s`）。
        //   期望值**全部直读原版**（⛔ 一个都不读 `Shell/RewardWindow.cs`）：
        //     · 树 / 矩形 / 图 / 字号 / 对齐 → `工具/menu_dump.py bundle_menus_assets_all "Reward Window" --depth 8`
        //     · 根组件 19 个字段 → `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1684643839753787023.json`
        //       （`type=1` · `windowsPlacement=15` · `closeOnESC=1` · `extraScaleSmallScreen=1.0` · `animTime=0.8`）
        //     · 行为 → `decomp_full/RewardWindow__{Open,Close,OnCollectClicked,ConfigureIsPreviewState,
        //       DoRewardAnimation}.c` + `RewardWindowContext__*.c`
        //     · 动画字面量 → `工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll 0x1834b32e0`（= 950）·
        //       `…b32e8/…b32ec`（= 0 / 0）· `…b32d0`（= 1.77778）· `…b32cc`（= 0.555555）· `…b326c`（= 300）
        //   🔴 **顺序要求**：本段**整段放在**下面的「渲染队列」与「领奖」两节**之前**没问题，但它的最后一步
        //      （`CheckAbsorbRule`）**会把窗点关**（`已知的坑.md` 2026-10-11（A287））⇒ 那一句必须排在本段**最后**。
        {
            int collectCalls = 0, closeCalls = 0;
            var rw = RewardWindow.Create(wm2);
            wm2.OpenWindow(rw, new RewardWindowContext
            {
                Rewards = new[]
                {
                    new CampaignData.RewardSpec("DT Ultramarines All", 3, CampaignData.TierBasic),
                    new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierPremium),
                },
                IsPremiumLocked = false, IsPreview = false,
                OnCollect = _ => collectCalls++,
                OnClose = _ => closeCalls++,
            });
            Debug.Log(P + "   " + rw.Dump());

            // ---- ① 窗口参数（MB 原文；与 `CampaignRewardWindow` 那三条同类）----
            Check(rw.type, WindowType.Popup, "`Reward Window` 的 `type` = 1 Popup（MB 实读）");
            Check(rw.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（**不是 5 Canvas**）");
            CheckTrue(rw.closeOnEsc, "`closeOnESC` = 1（ESC 关得掉）");
            CheckNear(rw.extraScaleSmallScreen, 1f, 0.001f, "`extraScaleSmallScreen` = 1.0");

            // ---- ② 结构 + 矩形（逐层对原版）----
            var rwShade = FindChild(rw.transform, "Menu Dark Background");
            CheckAt(rwShade, -1327.30f, 3247.30f, -746.18f, 1826.18f,
                    "`Menu Dark Background` 摆在原版那个矩形里（4574.60×2572.36）");
            CheckNear(TintOf(rwShade).a, 0.772549f, 0.002f, "压暗层 α = **0.772549**（原版 `m_Color`）");
            // ★ A411（2026-10-13）：**本窗缺的那条「档位不变量」** —— 判据与写法**逐字照同族**
            //   （唯一一份 = `MenuDraw.CheckShadeRule`；⛔ 别在本文件里发明第二套，见 `:158-176` 那段收口注释）。
            // 🔴 为什么单开这一条：`资料/普查产出_1012/M1_小件三笔.md` §五·2 查出 —— `Shell/RewardWindow.cs`
            //   是 2026-10-11（批次 2）新加的窗、**有 `MenuDraw.ShadeHit` 站点却没有档位断言**
            //   ⇒ `资料/普查产出_1008/Y_断言收尾批.md:82` 当天立的「**逐窗都有 `CheckShadeRule`**」
            //   这条性质被它又开了个洞（现读：`Shell/` 有 `ShadeHit(` 的窗 22 扇 vs 真调用 21 处/21 扇）。
            //   ⚠️ 本窗**没有** `ShadeHit` 属性（同族多数窗有）⇒ 按名字取那颗命中区，写法照
            //   `MainMenuScene` 的「排行榜弹窗 / 好友挑战弹窗」那几处（`FindChild(…, "CloseHit")` 同形）。
            //   期望值：`darkVisual` = 本窗那块**画出来的**压暗层（`Build()` 那句
            //   `MenuDraw.Rect(root, …, "Menu Dark Background", QShade, …)` 建的，**另一处代码**）、
            //   内容档 = `QCollectBg`（= 本窗 `MenuDraw.Absorb(root, "AbsorbHit", Content, QShade, QCollectBg)`
            //   与 `MenuDraw.Hit(btn, "Hit", CollectBtn, QCollectBg, …)` 传的那一档）。
            //   改坏法：把 `Build()` 那句 `MenuDraw.ShadeHit(root, Shade, QShade, …)` 的档换成 `QShade + 1`
            //   （或改回本窗自己那份 `MenuDraw.Hit(...)`）⇒ 这条立刻红（`ShadeRuleOk` 第 ③/② 条）。
            MenuDraw.CheckShadeRule(CheckTrue, "领奖窗", FindChild(rw.transform, "BackgroundHit"),
                                    rwShade, RewardWindow.QCollectBg);
            var rwContent = FindChild(rw.transform, "Content");
            CheckAt(rwContent, 0f, 1920f, 165f, 965f, "`Content` = **0,165 → 1920,965**（原版 RT）");
            var rwVp = FindPath(rw.transform, "Content/Scroll View/Viewport");
            CheckAt(rwVp, 0f, 1920f, 300f, 932.5f, "`Viewport` = **0,300 → 1920,932.5**（与 `Scroll View` 同矩形）");
            // 🔴 **2026-10-11（批次1 · F7）删掉了一条断言（铁律 5 就地订正）**：原来这里断
            //    「`Viewport` 上挂着 `UnityEngine.UI.RectMask2D`」—— **那是把我们的实现技术当成了原版行为**：
            //    · 原版那件确实是 `RectMask2D`（`Reward Window/Content/Scroll View/Viewport`，`soft=(200,0)`），
            //      但**本工程的等效物是 `MenuDraw.Clip` / `GameWindow.Clip`**（`Shell/MenuDraw.cs:121` 那节标题
            //      就写着「裁切（等效 `RectMask2D`）」）—— **全仓 `AddComponent<RectMask2D>` 零命中**，
            //      全文件只有这一句引用它 ⇒ 这条**首跑即红、从没绿过**。
            //    · 真正的裁切判据在别处，删了**不留空洞**：上面那条 `CheckAt(rwVp, …)` 已经把结构钉住，
            //      第 ⑧ 段那三条（视口外的格不建 / 压边文字被夹在 1920 / 软边切线**逐顶点对原版剖面**）更强。
            //    ⛔ 别按原版结构名去断「我们建了哪个组件」—— 同一处的同族先例：`Editor/CollectionScene.cs:2435`
            //      也只断「是那个 `RectTransform`」。
            CheckAt(FindPath(rw.transform, "Content/Title"), 660f, 1260f, 193.30f, 268.30f,
                    "`Title` = **660,193.30 → 1260,268.30**");
            var rwGlowGet = FindPath(rw.transform, "Content/Title/Glow Get reward");
            var rwGlowPrev = FindPath(rw.transform, "Content/Title/Glow Preview reward");
            CheckAt(rwGlowGet, 660f, 1260f, 187.38f, 262.38f,
                    "`Glow Get reward` = **660,187.38 → 1260,262.38**（比 `Title` **高 5.92px**，原版 `pos y +5.92`）");
            CheckAt(FindChild(rw.transform, "Collect Button"), 837.5f, 1082.5f, 902.5f, 977.5f,
                    "`Collect Button` = **837.5,902.5 → 1082.5,977.5**（下沿 977.5 **超出面板 965** —— 原版如此）");
            CheckAt(FindPath(rw.transform, "Content/Premium Disclaimer/Premium Icon"),
                    1630.33f, 1708.88f, 885f, 963.55f, "`Premium Icon` = **1630.33,885 → 1708.88,963.55**");
            var rwVig = FindChild(rw.transform, "Menu Vignette");
            CheckAt(rwVig, 0f, 1920f, 0f, 1080f, "`Menu Vignette` 铺满整屏（0,0 → 1920,1080）");
            CheckNear(TintOf(rwVig).a, 0.580392f, 0.002f, "暗角 α = **0.580392**（原版 `m_Color`）");
            // 队列：本窗是弹窗 ⇒ 必须高于所有「页」（否则被页底板盖住 —— 那种错**矩形断言量不到**）
            CheckTrue(RewardWindow.QShade > Mathf.Max(Mathf.Max(ForgeTab.QTabHelp, CampaignTab.QPanelTimer),
                                                      Mathf.Max(RewardsWindow.QOverlay, DailyStreakTab.QInfo)),
                      "★ 本窗压暗层的档**高于所有页**（`RewardsScene` 里那几页的最高档；"
                      + "🆕 2026-10-16（A815）起**自建「每日连胜」页那一档（`DailyStreakTab.QInfo`）也进了这个比较**）");
            CheckTrue(RewardWindow.QShade < RewardWindow.QCollectBg,
                      "…而**严格低于**本窗内容命中区档（同档时谁吃到命中退化成枚举顺序 ⇒ 症状是「点不动的钮看着像正常工作」）");

            // ---- ③ 图 / 字（量**渲染真值**：`ImageQuad.Texture.name` / `Label.Text`）----
            var rwBgGet = FindChild(rw.transform, "Reward Background Get Reward");
            var rwBgPrev = FindChild(rw.transform, "Reward Background Preview Reward");
            CheckArt(rwBgGet, "40k_general_popup_simple_red",
                     "`Reward Background Get Reward` 画的图（原版原名 `40k_general_popup_simple red`）");
            CheckArt(FindChild(rw.transform, "Collect Button"), "UI_Button_Mulligan",
                     "`Collect Button` 的底图（Simple；HL/Pressed 走 SpriteSwap）");
            CheckNear(TintOf(rwGlowGet).r, 0.8018868f, 0.002f, "`Glow Get reward` 的 tint = 原版 `m_Color` 的红");
            // ⚠️ **`Preview` 那一套（灰底 / 灰底光 / `You will get` / `Click to continue`）的三条挪到第 ⑨ 段** ——
            //    这一档里它们那几个节点是**关着**的，而未激活的件量不出**渲染真值**
            //    （TMP 在对象没激活时量不出 `textBounds` —— `资料/已知的坑.md`「面板画出来了、字不在」坑①）。
            //    🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：这里原来还写着「`GetComponentInChildren<T>()`
            //    单参重载**会跳过不激活的子件**」—— **取件那半句站不住**：该重载（`includeInactive = false`）
            //    只跳过**自己 `activeSelf == false` 的子件**，**⛔ 不查祖先链**（父链关着照样取得到 ——
            //    判据 = `资料/已知的坑.md` 2026-10-11（F4）那条 + 本轮日志里的两条实证：
            //    ① 第 ⑨ 段 `TextOf(FindChild(rw.transform, "Tap To Continue"))` 那条**绿**（那一刻那个节点
            //    **自身不激活**，而字照样取到了）；② 「拍壳那 4 张」紧后面那三条边缘断言
            //    （`Premium Panel/Title` 的右/左边缘 · `Campaign Header/Title` 的左边缘）在**壳窗被关掉之后**
            //    仍量出 710.2 / 408.6 / 480.69 —— **不是 NaN**（`TextLeftPx`/`TextRightPx` 取不到 `Label` 时恒回 NaN）。
            //    ⇒ **「取得到」与「在不在画面上」是两把尺子**（D3 诊断 §四 那两条「判不了」至此闭合）。
            Check(TextOf(FindPath(rw.transform, "Content/Title/Glow Get reward/Text Get Reward")), "Rewards claimed",
                  "标题那段 TMP 的原版原文 = **`Rewards claimed`**（`m_text` 实读）");
            // ★ A459（2026-10-13，H26 §三·3 待接线）：**标题那颗 TMP 的 `m_fontSizeBase` = 原版 36.0**。
            // 🔴 期望值 **36.0 是原版 prefab 的原文**（`python 工具/menu_dump.py bundle_menus_assets_all
            //   "Reward Window" --depth 6 --md --no-sprite` 的 `Content/Title/Glow Get reward/Text Get Reward`
            //   与 `…/Glow Preview…/Text Preview Reward` 两颗逐字相同：「字号=50.0 **基准=36.0** auto[25.0~50.0]」）
            //   ⇒ ⛔ **不是**读我们自己的常量（`Shell/RewardWindow.cs` 的 `TitleAutoBase` 传进去的就是它 ——
            //   拿它比 = 自证；这条读的是 **TMP 那个真字段**）。
            // 牙口：`Label.FontSizeBase`（`Battle/Label.cs`）是**反射读** `TMP_Text.m_fontSizeBase`
            //   （它没有公开访问器），`Label.FontSizeToPx` 是**唯一那条 px 口径**（1 个 fontSize ≈ 10.24px）。
            //   改坏法：删掉 `Shell/RewardWindow.cs` 里那句 `autoBasePx: TitleAutoBase` ⇒ base 退回 `fontPx`
            //   = 50 ⇒ 这里读到 50 ⇒ 红。⛔ 别只断「渲染字号」（base 只改二分的**起点**、终点两侧都收敛
            //   ⇒ 渲染差 ≤ 0.05 fontSize 单位 ⇒ 那是个**弱断言**）。
            {
                var rwTitleLb = FindPath(rw.transform, "Content/Title/Glow Get reward/Text Get Reward") != null
                    ? FindPath(rw.transform, "Content/Title/Glow Get reward/Text Get Reward").GetComponent<Label>() : null;
                CheckTrue(rwTitleLb != null, "★ A459：（前提）标题那颗挂得到 `Label`（下一条要反射读它的 `m_fontSizeBase`）");
                CheckTrue(rwTitleLb == null || rwTitleLb.FontSizeBase > 0f,
                          "★ …（前提）走的是 TMP 后端 —— `Label.FontSizeBase` 在**点阵后端**返回 **−1**（如实报、不是 0）");
                CheckNear(rwTitleLb != null ? Label.FontSizeToPx(rwTitleLb.FontSizeBase) : -1f, 36f, 0.6f,
                          "★★ A459：`Glow Get reward/Text Get Reward` 的 `m_fontSizeBase` = 原版 **36.0**"
                          + "（画布 px 口径；判据 = 上面那条 `menu_dump` 实读）"
                          + " —— 删掉 `autoBasePx: TitleAutoBase` ⇒ 读到 `fontPx` = 50 ⇒ 红");
            }
            Check(TextOf(FindChild(rw.transform, "Collect Button")), "Collect", "按钮文案 = **`Collect`**");
            // ⚠️ 「`You will get`」与「`Click to continue`」**挪到第 ⑨ 段**再断 —— 这一档里它们那两个节点
            //    是**关着**的，而**真正量不到的是渲染真值**（未激活的 TMP 量不出 `textBounds`，见坑①）
            //    —— ⛔ 不是「`Label` 取不到」（那半句 2026-10-11（批次1 · F7）已订正，见上面 ③ 段那条）。

            // ---- ④ 三组状态（**`IsPreview`** / **`IsPremiumLocked`** / `OnCollect`）----
            CheckTrue(rwBgGet != null && rwBgGet.gameObject.activeInHierarchy,
                      "`IsPreview = false` ⇒ 显 `Reward Background Get Reward`");
            CheckTrue(rwBgPrev != null && !rwBgPrev.gameObject.activeInHierarchy, "…而 `Preview` 那张关着");
            CheckTrue(rwGlowGet != null && rwGlowGet.gameObject.activeInHierarchy && rwGlowPrev != null && !rwGlowPrev.gameObject.activeInHierarchy,
                      "两团底光走**同一条判据**（`ConfigureIsPreviewState` 那四句）");
            CheckTrue(FindChild(rw.transform, "Collect Button").gameObject.activeInHierarchy,
                      "`OnCollect != null` ⇒ `Collect Button` 建着");
            // 🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：下两条原来把 flag 认错了 ——
            //    `Premium Disclaimer` 读 **`0x18` = `IsPremiumLocked`**、`Tap To Continue` 读 **`0x19` = `IsPreview`**
            //    （判据 = `d:/2/tools/decomp_full/RewardWindow__Open.c:164-185` / `:189`；偏移 ← `RewardWindowContext__.ctor.c:51-52`）。
            //    ⚠️ 本档两个 flag 都是 `false` ⇒ 期望值本身不变，**变的是它凭什么对**——
            //    两个 flag 分开的那种夹具在第 ⑧·b 段（判别档）。
            CheckTrue(!FindChild(rw.transform, "Premium Disclaimer").gameObject.activeInHierarchy,
                      "`IsPremiumLocked = false` ⇒ `Premium Disclaimer` 关着（原版 = `IsPremiumLocked && 有高级档奖励`）");
            var rwTap1 = FindChild(rw.transform, "Tap To Continue");
            CheckTrue(rwTap1 != null && rwTap1.gameObject.activeInHierarchy,
                      "`IsPreview = false` ⇒ `Tap To Continue` **开着**（原版 `SetActive(!IsPreview)` —— "
                      + "`RewardWindow__Open.c:189` 读的是 `0x19`；⛔ 不是 `IsPremiumLocked`）");
            // ⚠️ **截图放在揭示跑完之后**（第 ⑤ 段末）—— 这一刻遮罩还收拢着（原版那 0.8 秒的起始态），
            //    拍下来的会是「幕还没拉开」那一帧（`Shoot` 的亮度护栏照样过得去 ⇒ 不会报，但没法用来并排比）。

            // ---- ⑤ 开场揭示动画（`DoRewardAnimation`：`mask2D.padding` 950 → 0 · **线性** · 0.8 秒）----
            CheckNear(rw.RevealProgress, 0f, 0.001f, "★ 刚开出来时揭示进度 = **0**（原版先把 `padding` 设成初始值再 tween）");
            var rwPad0 = rw.MaskPad;
            CheckNear(rwPad0.y, 0f, 0.001f, "…遮罩 padding 的**下分量恒 0**（字面量 `(950,0,950,0)` 的第二位）");
            CheckNear(rwPad0.w, 0f, 0.001f, "…**上分量恒 0**（第四位）");
            CheckNear(rwPad0.x, rwPad0.z, 0.001f, "★ …**左右对称**（第一位 = 第三位 = 950；只设一边就是另一档）");
            CheckTrue(rwPad0.x >= 950f - 0.01f,
                      $"…左分量的**下界是 950**（原版字面量 `0x1834b32e0`；宽屏那一档还要再加，实测 {rwPad0.x:F1}）");
            // 🔴 **2026-10-11（批次2 · 诊断 D6）订正（铁律 5）**：这条原来断「此刻 `listHolder` 子树里
            //    **一个激活的 `ImageQuad` 都没有**」—— **那个前提不成立**，它只在「没有任何东西越出抽屉框」时
            //    才偶然为真（旧跑之所以绿：20px 带 [950,970] 正好坐在 40px 中缝 940→980 的中间）。
            //    我们**没有**「东西不越出框」这个性质：高级档那一格的 premium `Highlight` 照原版 prefab 的
            //    `sizeDelta (50,50)`（拉伸锚）**每边外扩 50·k**（`Shell/ItemDrawer.cs:888-893`，
            //    `k = box.H / DecorRefH = 300/1080` ⇒ 13.889px）⇒ 第 2 格（框 980→1180）那条 Highlight
            //    = **966.11 → 1193.89**，左沿**探进带里 3.89px**（带 = [950,970]）。
            //    🔴 那一小块**原版同样画**（`RectMask2D.m_Padding` 也是 950、prefab 也是 ±50）⇒ **不是缺陷**，
            //    是判据写错了。⇒ 改成它**本来那个意思**：「**能画出来的东西全部落在带里**」。
            //    判别力不减：揭示没接（或没推）时并集会跳到整条内容（实测 770.0 → 1193.9）⇒ 照样红。
            //    ⛔ 两条**不建议**的改法（D6 §四·2 点名）：① 把夹具那条 `TierPremium` 改成 `TierBasic`
            //    （会拆掉 `:4266-4267` 与 §⑧·b「有高级档奖励」那条前提）；② 改实现让装饰不外扩（**背离原版**）。
            //    改坏法：把 `RectOfUnion` 换成「随便量一个子节点」（或改成恒回 false）⇒ 这里失去意义
            //    （并集在揭示前就恒空）；把 `rw.Tick` 那两句删掉 ⇒ 并集留在 770.0 → 1193.9 ⇒ 红。
            bool anyQuad = RectOfUnion(rw.ListHolder, out var mq1, out _, out var mq3, out _);
            float bandL = rwPad0.x, bandR = 1920f - rwPad0.z;   // 950 / 970（宽屏那一档 `MaskPadAt` 已把它抬高）
            CheckTrue(!anyQuad || (mq1 >= bandL - 0.5f && mq3 <= bandR + 0.5f),
                      $"★ …此刻遮罩收到只剩中间 {bandR - bandL:F0}px（1920 − {bandL:F0} − {rwPad0.z:F0}）"
                      + $"⇒ **能画出来的东西全部落在带里**（实测 x {(anyQuad ? mq1 : 0f):F1} → {(anyQuad ? mq3 : 0f):F1}）"
                      + " —— 收拢那一刻带里只剩 premium 装饰探进来的那一条（`Highlight` = 966.1 → 1193.9 ⇒ 越出右沿"
                      + " `bandR` 的部分被 `MenuDraw.Rect` 裁掉，只建求交后那一块，`Shell/MenuDraw.cs:1229-1238`）；"
                      + "没接动画的版本这一刻已经是全开的 ⇒ 并集 770.0 → 1193.9 ⇒ 红");
            rw.Tick(RewardWindow.AnimTime * 0.5f);
            CheckNear(rw.RevealProgress, 0.5f, 0.01f, "★ 跑一半（0.4s / 0.8s）= **0.5**（`SetEase(…, 1)` = `Ease.Linear`）");
            CheckNear(rw.MaskPad.x, rwPad0.x * 0.5f, 0.5f,
                      "★ …而且 padding **正好走了一半**（相对断言 —— 避开「宽屏那一档加多少」那个分支）");
            rw.Tick(RewardWindow.AnimTime);
            CheckNear(rw.RevealProgress, 1f, 0.001f, "…再跑完 ⇒ 进度 = 1");
            CheckNear(rw.MaskPad.x, 0f, 0.001f, "★ 终值 = **0**（`DOTween.To(…, endValue, animTime)` 的终值是零向量）");
            CheckNear(rw.MaskPad.z, 0f, 0.001f, "…右边也 0");
            CheckTrue(RectOfUnion(rw.ListHolder, out var iu1, out var iu2, out var iu3, out var iu4),
                      "★ 揭示完之后物品**画得出来了**（同一条判据的两面：收拢时**带里只剩 premium 装饰探进来的"
                      + "那一条**、全开时整格都在 —— 🔴 2026-10-11（诊断 D6）随上一条一起订正："
                      + "原来写「收拢时没有 quad」，那句**不成立**，见上面那一段的订正注释）");
            CheckTrue(iu1 >= -0.5f && iu3 <= 1920.5f, $"…而且**都落在视口里**（实测 x {iu1:F1} → {iu3:F1}）");
            Shoot("07_奖励窗_Get.png");     // ⚠️ 必须**在揭示跑完之后**拍：揭示中拍下来的是「幕还收拢着」那一帧

            // ---- ⑥ 物品 / 滚动（内容比视口窄那一档）----
            Check(rw.ListHolder.childCount, 2, "两条奖励 ⇒ `listHolder` 底下**两个**抽屉节点（原版一句 `ItemDrawer.Draw` 一个）");
            CheckNear(rw.Scroll.ClampHi, 0f, 0.01f,
                      "★ 内容（120 + 2×200 + 40 = 560）比视口（1920）窄 ⇒ **可滚上界 = 0**"
                      + "（UGUI `AdjustBounds`：只有内容更大才能滚）");
            CheckNear(rw.Scroll.ClampLo, 0f, 0.01f, "…下界同样 0");

            // ---- ⑦ 点 `Collect`（原版 `OnCollectClicked`：发一次、之后 `interactable = false`）----
            var rwBtn = FindChild(rw.transform, "Collect Button");
            var rwWb = rwBtn != null ? rwBtn.GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(rwWb != null, "（前提）`Collect Button` 上有 `WindowButton`（没有 ⇒ 下面两条等于没查）");
            // 🔴 **2026-10-11（批次1 · F8）新加的一条**（下面那两条红的根因就在这一格）：
            //    同一个节点上**多挂一颗同型 `WindowButton`** ⇒ 两颗共用**同一颗**命中 quad
            //    （`PointerLayer.HitQuad` 取的是 `GetComponentInChildren<ImageQuad>()`）⇒ **队列与 z 逐位相同**
            //    ⇒ `HitButton` 那条「队列大的先、同队列 z 小的先」**分不出它们**，赢家退化成
            //    `FindObjectsByType` 的**枚举顺序**；而自检取件走 `GetComponentInChildren<WindowButton>`
            //    （= **组件表第一颗**）⇒ **两把尺子各挑一颗**。
            //    🔴 **症状极具误导性**：两条的 `onClick` 都绑着 `OnCollectClicked` ⇒ 真点那一下**照样发**，
            //    于是「`OnCollect` 发了一次」那条**绿**、只有「引用相等」那条红 —— 单看 `[False]` 读不出这一层
            //    （F8 之前那一轮就绕在这儿）。**全工程 `grep` 过：只有 `RewardWindow` 那一处那么写**，
            //    其余每一处 `AddComponent<WindowButton>` 建的都是**自己的节点**。
            //    **改坏法**：把 `RewardWindow.Build()` 里那句 `hit.gameObject.AddComponent<WindowButton>()` 加回去 ⇒ 这条红。
            var rwWbs = rwBtn != null ? rwBtn.GetComponentsInChildren<WindowButton>(true) : new WindowButton[0];
            Check(rwWbs.Length, 1,
                  $"（前提）`Collect Button` 上**只有一颗** `WindowButton`（实得 {rwWbs.Length} 颗 —— "
                  + "同节点多颗时队列/z 分不出它们，命中赢家退化成枚举顺序）");
            if (rwWb != null)
            {
                ClickButtonByQuad(PointerLayer.Instance, rwBtn, rwWb, "原版奖励窗的 `Collect`");
                Check(collectCalls, 1, "★ 点 `Collect` ⇒ `OnCollect` **发了一次**");
                ClickButtonByQuad(PointerLayer.Instance, rwBtn, rwWb, "原版奖励窗的 `Collect`（第 2 下）");
                Check(collectCalls, 1, "★ …第 2 下**不再发**（= 原版那句 `Selectable.interactable = false`；"
                                       + "我们这套没有 `interactable` ⇒ 同一句门写在 `onClick` 里，同 `CampaignRewardWindow` 的 `clickable`）");
            }

            // ============================================================ 🆕 **2026-10-11（批次1 · 戊2 · A302）**
            // **抽屉里那三层文字也要吃裁切**（原来 `MenuDraw.Text` 没有裁切形参、本库也没走 `ClipText`
            // ⇒ **图被裁、字照画**）。这里用一个**独立探针**断它（不依赖任何一扇窗的数据）：
            //   · 带口 = 原版 `Reward Window` 的 `Scroll View` 矩形 **(0,300) → (1920,932.5)**（**字面量**）
            //   · 盒子 = **1780..1980**（**fixture**：右半截在视口外）⇒ 数量文字右对齐到 `box.x2 − 10 = **1970**`
            //   ⇒ 裁过之后**一个顶点都不许越过 1920**，而且**必须有顶点被夹在 1920 上**（真被截了，不是碰巧没到）。
            var prClip = new PxRect(0f, 300f, 1920f, 932.5f);
            var prSt = ItemDrawerStyle.Default(RewardWindow.QItem, RewardWindow.QItemIcon, RewardWindow.QItemIcon);
            prSt.Clip = prClip; prSt.ClipSoftness = Vector2.zero;      // 硬边（这一处不是渐隐带）
            const string prId = "Booster Pack Ultramarines";
            var pr1 = new GameObject("A302 Probe 压边").transform;
            ItemDrawer.Draw(pr1, new PxRect(1780f, 400f, 1980f, 700f),
                            ItemDrawer.Spec(prId, CampaignData.ItemIcon(prId), CampaignData.ItemShortName(prId)),
                            3, DrawerOverride.Default, prSt);
            var pr1Lb = QuantityLabelOf(pr1);
            CheckTrue(pr1Lb != null, "（A302 前提）探针那一格的**数量文字**建出来了（整块有交集、只是压边）");
            if (pr1Lb != null)
            {
                var pxs = TmpVertPx(pr1Lb);
                CheckTrue(pxs != null && pxs.Length > 0, "（A302 前提）它的渲染顶点量得到");
                if (pxs != null && pxs.Length > 0)
                {
                    float pmx = float.MinValue;
                    for (int k = 0; k < pxs.Length; k++) pmx = Mathf.Max(pmx, pxs[k].x);
                    CheckTrue(pmx <= 1920.5f,
                              $"★ 压在带口的**文字被截到带内**（实测最右顶点 {pmx:F1} ≤ 1920；"
                              + "未接裁切时它 = **1970** = `box.x2 − 10` 那个右对齐落点）");
                    CheckTrue(pmx >= 1919.5f,
                              $"★ …而且**确实被夹在 1920 上**（实测 {pmx:F1}）—— 少了这一条，上面那条对一段"
                              + "「本来就没越界」的短文字**恒真**（红要红在「裁没生效」上）");
                }
            }
            // 对照组：整块落在视口外的那一格 —— **文字与图一个都不建**（`MenuDraw.Visible` / `MenuDraw.Rect` 同一条判据）
            var pr2 = new GameObject("A302 Probe 出框").transform;
            ItemDrawer.Draw(pr2, new PxRect(2000f, 400f, 2200f, 700f),
                            ItemDrawer.Spec(prId, CampaignData.ItemIcon(prId), CampaignData.ItemShortName(prId)),
                            3, DrawerOverride.Default, prSt);
            CheckTrue(QuantityLabelOf(pr2) == null,
                      "★ 整块落在带外的那一格：**数量文字一个节点都不建**");
            Check(pr2.GetComponentsInChildren<ImageQuad>(true).Length, 0,
                  "…**图也一样**（这条是图那一半的既有行为，放这儿当对照组：文字那条与它现在同一条判据）");
            Object.DestroyImmediate(pr1.gameObject);
            Object.DestroyImmediate(pr2.gameObject);

            // ---- ⑧ 内容比视口宽那一档：真能滚 + 文字吃软边（A302 的真窗那一半）----
            var many = new List<CampaignData.RewardSpec>();
            for (int i = 0; i < 12; i++)
                many.Add(new CampaignData.RewardSpec("Booster Pack Ultramarines", 1,
                         i % 4 == 0 ? CampaignData.TierPremium : CampaignData.TierBasic));
            // 🔴 **2026-10-11（批次2 · F2）就地订正（铁律 5）**：W1 报告 §六·b 说本段「不会红」的理由是
            //    「那 12 条的 id 全一样（节点名全是 `Item_Booster Pack Ultramarines`）⇒ 按名字/位置数都数不出差别」
            //    —— **前半句对、理由不完整**：**tier 是混的**（上一行 `i % 4 == 0 ? Premium : Basic`，3 条高级档），
            //    而 A310② 新加的 `OrderByTier`（= 原版 `Open()` 那句 `OrderBy(r => r.RewardTier)`）**确实改了它们的顺序**
            //    （3 条高级档被移到末尾 ⇒ 落在索引 9/10/11 那几格，其中**两格整块在视口外**）。
            //    ⇒ 结论仍然成立，靠的是**另一条没写出来的前提**：
            //    🔴 **`tier` 目前不参与画法** —— `Shell/RewardWindow.cs` 的 `BuildItem` **只**读 `spec.Id` 与
            //    `spec.Quantity`（`ItemDrawer.Spec(spec.Id, ArtOf(...), ItemShortName(...))` ＋ `spec.Quantity`），
            //    整条抽屉的路**没有任何一处读 `RewardTier`** ⇒ 12 格长得一模一样 ⇒ 重排在本段**不可观测**。
            //    ⚠️ **所以：A310③ 一接 premium 高亮 / `SetPremium(tier)`，这条前提就失效** ——
            //    那时 3 条高级档画得与别的格不同，而它们被排到的是**右端那几格**（含两格在视口外）
            //    ⇒ 本段量到的东西（数量文字数、顶点、软边）**会变**，尤其按位置数的 `qLabels == 8` 那几条
            //    **必须重新标定**。到那时**这里该红是正常的**，⛔ 别把本段删掉去迁就它。
            //    🔴 **2026-10-11（批次2 · FX5）实测回填**：那个「会变」**没有发生** —— A311 落地后本段
            //    `qLabels == 8` 与那三条软边顶点剖面**逐条照旧绿**（`rewards2.log` 只红了 §⑤ 那一条）。
            //    **为什么**：装饰层是**额外的 quad 节点**（名字是 `Premium Highlight`/`Highlight`/…），
            //    而本段量的是**数量文字**（`Quantity` 那几颗 `Label`）与它的顶点 —— 装饰既不建 `Quantity`
            //    也不改文字的框 ⇒ 两条路不相交（`ItemDrawer.NodeQuantity` 只在抽屉那一层建）。
            wm2.OpenWindow(rw, new RewardWindowContext
            {
                Rewards = many.ToArray(), IsPremiumLocked = false, IsPreview = false,
                OnCollect = _ => collectCalls++,
            });
            // 🔴 **2026-10-13（H45）必补的一句**：上面那次 `OpenWindow` **只换了 `_ctx`、内容一格没重画** ——
            //    `WindowsManager.OpenByState()` 的 `Open` 支是**一个字段都不写就 return**（`Shell/WindowsManager.cs:480-486`，
            //    = 原版 `GameWindow.TryOpen` 那一支，A217② 按反编译改的）⇒ `RewardWindow.Open()`（= `Build()`）
            //    **一次都没被调到**。删掉本句 ⇒ 本段量到的仍是**首开那一次**（2 条）的内容：
            //    `ClampHi = 520` 与「数量文字 8 个」两族**一起红**（本批 D10 诊断的红 1–6），
            //    ⛔ 这不是「重开必重建」那种实现缺陷，是**夹具的旧前提被 A217② 推翻**后才暴露出来的。
            //    ⛔ **也【不用】改写成 `wm2.OpenWindow(rw); rw.Build();` 那种两行写法** —— 无参那条重载
            //    **不调 `SetupData`**（`Shell/WindowsManager.cs` 的 `GameWindow.TryOpen()`（无参那条），判据 = A447 亲读指令流）⇒ `_ctx` 会**留在上一档**，
            //    `Build()` 照旧画旧内容。必须是「带参 `OpenWindow`（换 `_ctx`）+ 显式 `Build()`」这一组合。
            rw.Build();                              // 幂等：首句就清空 root 子件 + 清账 + 杀旧 tween（`Shell/RewardWindow.cs:1006-1023`）
            rw.Tick(RewardWindow.AnimTime);          // 新开一次 ⇒ 揭示从 0 起，先跑完再量
            Debug.Log(P + "   " + rw.Dump());
            // 内容宽 = 120 + 12×200 + 11×40 = **2960**；内容是**居中**的 ⇒ 两端各出 (2960−1920)/2 = **520**
            CheckNear(rw.Scroll.ClampHi, 520f, 1f, "★ 内容比视口宽 ⇒ 可滚上界 = **+520**");
            CheckNear(rw.Scroll.ClampLo, rw.Scroll.ClampHi * -1f, 1f,
                      "★ …下界**对称** = **−520**（内容在视口里居中 —— 原版 `ContentSizeFitter` + 拉伸锚的后果；"
                      + "左对齐的内容会得到 `[0, +1040]` ⇒ 这条专拦那个）");
            int qLabels = 0;
            for (int i = 0; i < rw.ListHolder.childCount; i++)
                if (QuantityLabelOf(rw.ListHolder.GetChild(i)) != null) qLabels++;
            Check(CountByPrefix(rw.ListHolder, "Item_"), 12,
                  "12 条奖励 ⇒ **12 个抽屉节点**（原版一句 `ItemDrawer.Draw` 一个；节点名 = `Item_<短名>`）");
            // 🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：这条原来期望 **10**（文案只数了右边两格），
            //    **正确值就是 8** —— 判据是**同段自己那两条已绿的算式**（⛔ 不是「实得 8」）：
            //    内容宽 2960、**居中**在 1920 视口 ⇒ 左沿 = **−520**（上面 `ClampLo = −ClampHi` 那条证的）；
            //    格距 240（`ItemW` 200 + `ItemSpacing` 40）、左侧内缩 60 ⇒
            //    **第 1 格 −460..−260 · 第 2 格 −220..−20**（这两格**整块在视口左边之外**）；
            //    右端 **第 11 格 1940..2140 · 第 12 格 2180..2380**。⇒ 整块在外的一共 **4** 格，12 − 4 = **8**。
            //    改坏法：期望改回 10 ⇒ 红。
            Check(qLabels, 8, "★ …其中**整块落在视口外的 4 格**（第 **1、2** 格：x −460..−260 / −220..−20 · "
                              + "第 **11、12** 格：x 1940..2140 / 2180..2380）**数量文字一个都不建**"
                              + "（未接裁切的版本会建满 12 个 ⇒ 红）");
            // 🔴 **相对断言**（本会话血的教训③：绝对期望值若抄了一份错文档 = 同源错误 ⇒ 再补一条**相对**的）：
            //    这里**一个常量都不写死** —— 格心从**场上节点的实际位置**量（`PxOf(ch.position.x)`；
            //    `MenuDraw.Node` 把节点摆在 `box` 的中心 ⇒ 量到的就是格心），视口边界用**原版字面量** 0..1920
            //    （上面 `CheckAt(rwVp, 0,1920, 300,932.5)` 已经单独钉过它）。
            //    ⚠️ 本夹具里「格心在外」与「整块在外」**同解**：格宽 240 的整数倍摆放，四个越界格的**最内侧**
            //    分别是 −20 / −20 / 1940（第 3、10 格则整格在内：20..220 / 1700..1900）⇒ 两种口径都判在外。
            //    改坏法：`CenteredContent` 不居中（改左对齐）⇒ 期望的 8 与这条**同时**红（这条**不靠** 520 那个数）。
            int qByGeometry = 0, drawers = 0;
            for (int i = 0; i < rw.ListHolder.childCount; i++)
            {
                var ch = rw.ListHolder.GetChild(i);
                if (ch == null || ch.name == null || !ch.name.StartsWith("Item_")) continue;
                drawers++;
                float ccx = PxOf(ch.position.x);
                if (ccx >= 0f && ccx <= 1920f) qByGeometry++;
            }
            Check(qLabels, qByGeometry,
                  $"★ …**相对断言**：建了数量文字的格数（{qLabels}）= 按**量出来的格心**数出来的视口内格数"
                  + $"（{qByGeometry} / 共 {drawers} 个抽屉节点）—— ⛔ 这一条不写死任何数");
            Label bandLb = null; float bandMax = -1f;
            for (int i = 0; i < rw.ListHolder.childCount; i++)
            {
                var lb = QuantityLabelOf(rw.ListHolder.GetChild(i));
                if (lb == null) continue;
                var vs = TmpVertPx(lb);
                if (vs == null) continue;
                float mx = float.MinValue;
                for (int k = 0; k < vs.Length; k++) mx = Mathf.Max(mx, vs[k].x);
                if (mx > bandMax) { bandMax = mx; bandLb = lb; }
            }
            CheckTrue(bandLb != null && bandMax > 1725f,
                      $"（A302 前提）找得到一格的**数量文字伸进渐隐带**（带内沿 = 1920 − 200 = 1720；"
                      + $"实测最右顶点 {bandMax:F1}）—— 找不到 ⇒ 下面三条等于没验");
            if (bandLb != null)
            {
                var vpx = new List<Vector2>(); var val = new List<float>();
                int nv = TmpVertsAndAlpha(bandLb, vpx, val);
                CheckTrue(nv > 0, "（A302 前提）那段数量文字的 TMP 顶点量得到（点阵兜底 / 未激活 ⇒ −1）");
                if (nv > 0)
                {
                    int nBad = 0; float worst = 0f, minA = 1f, maxA = 0f, xMax = -1e9f;
                    for (int k = 0; k < nv; k++)
                    {
                        // 期望值 = **原版剖面** `clamp01((1920 − x) / 200)` —— 右沿 1920 与 `m_Softness.x = 200`
                        // 都是**原版字面量**（⛔ 不读 `RewardWindow.ScrollSoft`，那是被测实现）
                        float want = Mathf.Clamp01((1920f - vpx[k].x) / 200f);
                        float err = Mathf.Abs(val[k] - want);
                        if (err > 0.02f) nBad++;
                        worst = Mathf.Max(worst, err);
                        minA = Mathf.Min(minA, val[k]); maxA = Mathf.Max(maxA, val[k]);
                        xMax = Mathf.Max(xMax, vpx[k].x);
                    }
                    Check(nBad, 0, "★ **文字吃了与旁边图同一份 (200,0) 软边** —— 每个顶点都落在原版剖面上"
                          + $"（最差偏差 {worst:F3}）");
                    CheckTrue(minA < 0.999f,
                              $"★ …最暗那个角 {minA:F3} < 1（裸 `MenuDraw.Text` 那条路 ⇒ 全 1 ⇒ 红）");
                    CheckTrue(maxA > minA + 0.05f,
                              "…而且**有斜坡**（左边比右边亮；写成 (0,200) 会去削上下 ⇒ 这里左右相等）");
                    CheckTrue(xMax <= 1920.5f, $"…**没有一个顶点画到视口右沿之外**（实测最右 {xMax:F1}）");
                }
            }

            // ---- ⑧·b ★ **判别档**（🆕 2026-10-11（批次1 · F7））：`IsPremiumLocked = true` + `IsPreview = false` ----
            //   🔴 **这一档是【唯一能把两个 flag 分开】的夹具**：上面三档里它们**恒等**
            //      （`false,false` / `false,false` / `true,true`）⇒「拿哪个 flag 当开关」这两种实现**都能过**
            //      （D3 诊断 §二 B2/B3 的原话：「没有任何断言能分辨」）—— 一旦出现「locked 但非 preview」
            //      （或反之）就会**静默画错**（(a) 类偏离，铁律 11）。
            //   判据 = 原版反编译逐句读的**偏移**（偏移与 ctor 逐位对上）：
            //     · **`0x19` = `isPreview`**（`RewardWindowContext__.ctor.c:51` ← 第 8 个形参；形参表见
            //       `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RewardWindowContext.cs` 的 ctor 签名）⇒ 管
            //       两态底图/底光（`RewardWindow__Open.c:205-217`）· `Tap To Continue`（`:189`）·
            //       `claimRewardParticles`（`:280`）· **播不播揭示动画**（`:283`）；
            //     · **`0x18` = `isPremiumLocked`**（同 `:52` ← 第 2 个形参）⇒ **只**管 `Premium Disclaimer`（`:164-185`）。
            //   ⇒ 本档（`locked=true` / `preview=false`）的期望：底图仍「Get」那一套 · `Tap To Continue` **开** ·
            //     `Premium Disclaimer` **开** · 揭示**从头播**（`_reveal = 0`，遮罩收在最拢那一档）。
            //   ⚠️ `OnClose` 故意留空：下一档 §⑨ 与 §⑩ 的 `closeCalls == 1` 是对着**它们那一档**记的账。
            wm2.OpenWindow(rw, new RewardWindowContext
            {
                Rewards = new[] { new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierPremium) },
                IsPremiumLocked = true, IsPreview = false,
                OnCollect = null,
            });
            rw.Build();     // 🔴 **2026-10-13（H45）同 §⑧ 那一句**（`OpenWindow` 落在 `Open` 支、不重建内容）；
                            //    ⛔ 本档**别**改成「先 `Close()` 再开」：`RewardWindow.Close()` 会先发 `_ctx.OnClose`
                            //    （`Shell/RewardWindow.cs` 的 `Close`）⇒ 上一档 ctx#1 那颗 `closeCalls++` 会先 +1，
                            //    兜底把 §⑩ 的 `Check(closeCalls, 1)` 打红（= D10 §㈠·3(b) 记的那个坑）。
            Debug.Log(P + "   " + rw.Dump());
            var rwBgGet3 = FindChild(rw.transform, "Reward Background Get Reward");
            var rwBgPrev3 = FindChild(rw.transform, "Reward Background Preview Reward");
            CheckTrue(rwBgGet3 != null && rwBgGet3.gameObject.activeInHierarchy
                      && rwBgPrev3 != null && !rwBgPrev3.gameObject.activeInHierarchy,
                      "★ **判别档**：`IsPremiumLocked = true` 而 `IsPreview = false` ⇒ 底图仍是「Get」那一套"
                      + "（原版那四句读 `0x19` —— 把手写实现里的 `IsPreview` 换成 `IsPremiumLocked` ⇒ 这里翻成「Preview」⇒ 红）");
            var rwTap3 = FindChild(rw.transform, "Tap To Continue");
            CheckTrue(rwTap3 != null && rwTap3.gameObject.activeInHierarchy,
                      "★ **判别档**：`Tap To Continue` **开着**（同上 —— 原版读 `0x19`；"
                      + "把 `!_tap` 那一句的实现换成 `!IsPremiumLocked` ⇒ 这里关着 ⇒ 红）");
            CheckTrue(FindChild(rw.transform, "Premium Disclaimer").gameObject.activeInHierarchy,
                      "★ **判别档**：`Premium Disclaimer` **开着**（原版读 `0x18` = `IsPremiumLocked`；"
                      + "把 `_premium` 那一句的实现换回 `IsPreview` ⇒ 这里关着 ⇒ 红）");
            CheckNear(rw.RevealProgress, 0f, 0.001f,
                      "★ **判别档**：揭示**从头播**（原版 `RewardWindow__Open.c:283` 的 `if (0x19 == 0) DoRewardAnimation()`"
                      + " 读的是 `IsPreview`）—— 拿 `IsPremiumLocked` 当开关的版本在这里恒为 1（开出来即全开）");
            CheckTrue(rw.MaskPad.x >= 950f - 0.01f,
                      $"…而且遮罩**收在最拢那一档**（`padding` 左分量下界 = 原版字面量 950；实测 {rw.MaskPad.x:F1}）");

            // ---- ⑨ 另一档：`IsPremiumLocked = true` + `IsPreview = true` + `OnCollect = null` ----
            wm2.OpenWindow(rw, new RewardWindowContext
            {
                Rewards = new[] { new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierPremium) },
                IsPremiumLocked = true, IsPreview = true,
                OnCollect = null,
                OnClose = _ => closeCalls++,     // ⚠️ 下面 `CheckAbsorbRule` 会**点窗外把窗关掉** ⇒ 这一档也要记
            });
            rw.Build();     // 🔴 **2026-10-13（H45）第三处**（同 §⑧）：不补这一句 ⇒ 红 10–13 照红 ——
                            //    `IsPreview=true` 那四件（底图 / `Tap To Continue` / `Collect Button` / `Premium Disclaimer`）
                            //    全写在本 `Build()` 读的 `_ctx` 上（`Shell/RewardWindow.cs` 的 `Build`），
                            //    而 `OpenWindow` 只换 `_ctx`、不重画 ⇒ 量到的还是首开那一档（`false,false` 那版）。
            Debug.Log(P + "   " + rw.Dump());
            var rwBgGet2 = FindChild(rw.transform, "Reward Background Get Reward");
            var rwBgPrev2 = FindChild(rw.transform, "Reward Background Preview Reward");
            CheckTrue(rwBgGet2 != null && !rwBgGet2.gameObject.activeInHierarchy && rwBgPrev2 != null && rwBgPrev2.gameObject.activeInHierarchy,
                      "★ `IsPreview = true` ⇒ 换成 `Preview` 那套底图（Get 关 / Preview 开）；"
                      + "⚠️ 本档 `IsPremiumLocked` 也是 true ⇒ **单看本档分不出两个 flag**（判别档在第 ⑧·b 段）");
            Check(TextOf(FindPath(rw.transform, "Content/Title/Glow Preview reward/Text Preview Reward")), "You will get",
                  "★ 预览态标题 = **`You will get`**（原版 `m_text`；这一档它才亮着）");
            Check(TextOf(FindChild(rw.transform, "Tap To Continue")), "Click to continue",
                  "★ 提示文案 = **`Click to continue`**（原版 `m_text`；这一档它才亮着）");
            CheckArt(rwBgPrev2, "40k_general_popup_simple_greyscale",
                     "★ `Reward Background Preview Reward` 画的图（原版原名 `… greyscale`）");
            CheckNear(TintOf(FindPath(rw.transform, "Content/Title/Glow Preview reward")).g, 0.3563546f, 0.002f,
                      "★ `Glow Preview reward` 的 tint = 原版 `m_Color` 的灰（与 Get 那团 0.8019 的红配对）");
            CheckTrue(!FindChild(rw.transform, "Collect Button").gameObject.activeInHierarchy,
                      "★ `OnCollect == null` ⇒ `Collect Button` **整颗关掉**（原版 `SetActive(OnCollect != null)`）");
            // 🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：下两条原来把 `IsPremiumLocked` 当成了
            //    `Tap To Continue` / `Premium Disclaimer` 的开关，**期望值也是反的** —— 两条都错，按反编译改：
            //    `Tap To Continue` 读 **`0x19` = `IsPreview`**（`RewardWindow__Open.c:189`，`SetActive(!IsPreview)`）、
            //    `Premium Disclaimer` 读 **`0x18` = `IsPremiumLocked`**（`:164-185`）。
            //    ⚠️ 本档两个 flag **恒等**（都是 true）⇒ 单看本档两种实现都能过 —— **判别档在第 ⑧·b 段**。
            var rwTap2 = FindChild(rw.transform, "Tap To Continue");
            CheckTrue(rwTap2 != null && !rwTap2.gameObject.activeInHierarchy,
                      "★ `IsPreview = true` ⇒ `Tap To Continue` **关着**（原版 `SetActive(!IsPreview)`，读 `0x19`）");
            CheckTrue(FindChild(rw.transform, "Premium Disclaimer").gameObject.activeInHierarchy,
                      "★ `IsPremiumLocked && 有高级档奖励` ⇒ `Premium Disclaimer` 打开（读 `0x18`；谓词 = `RewardTier == 10`）");
            CheckArt(FindPath(rw.transform, "Content/Premium Disclaimer/Premium Icon"), "40k_campaign_Premium-icon",
                     "★ `Premium Icon` 画的图（原版 sprite）");
            CheckNear(TextRightPx(FindChild(rw.transform, "Premium Disclaimer")), 1708.8f, 1.5f,
                      "★ `Premium Disclaimer` **右对齐到 1708.8**（原版锚 0.89 × 1920 宽）");
            CheckNear(rw.RevealProgress, 1f, 0.001f,
                      "★ **预览那一档不播揭示**（原版 `RewardWindow__Open.c:283` = `if (IsPreview == 0) DoRewardAnimation()`，"
                      + "读的是 `0x19`）⇒ 出厂即全开");
            CheckNear(rw.MaskPad.x, 0f, 0.001f, "…`padding` = 0（`Open()` 里那句 `set_padding(零点)`）");
            CheckTrue(RectOfUnion(rw.ListHolder, out _, out _, out _, out _),
                      "…所以那一条奖励**立刻**画得出来（与第 ⑤ 段「收拢时**带里只剩 premium 装饰探进来的那一条**」"
                      + "互为对照 —— 🔴 2026-10-11（诊断 D6）订正：第 ⑤ 段原话是「一个 quad 都画不出来」，"
                      + "**那句判据当天就地改掉了**，见那一段的订正注释；⛔ 别再照旧话写第二份）");
            Shoot("07b_奖励窗_Preview.png");

            // ---- ⑩ 关窗回调 + 吸收层（**必须最后** —— `CheckAbsorbRule` 的收尾就是「点窗外 ⇒ 关窗」且不还原）----
            CheckAbsorbRule("原版奖励窗", rw.transform, "AbsorbHit", 0f, 165f, 1920f, 965f,
                            RewardWindow.QShade, RewardWindow.QCollectBg, () => rw.CurrentState);
            Check(closeCalls, 1, "★ 关窗 ⇒ `OnClose` **发了一次**（`RewardWindow.Close()` 里那句；参数 = `Rewards`）");
            Check(rw.CurrentState, WindowState.Closed, "…而且窗真的进了 `Closed` 态");
            // 🆕 **2026-10-16（A796）**：压暗层「**点了会不会关**」—— 走公共口
            //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs` 的 `CheckShadeClickRule`）。
            //   🔴 **本口会把窗【真的关掉】** ⇒ 必须排在本窗其它断言**之后**（这里就是本窗的收尾，
            //      同族落法 → `资料/普查产出_1015/W7_A796调用点.md` §二 #17 / §三·3）。
            //   ⚠️ **W7 §四·1 点名「本件没查实」的两条，本件逐条读代码查实了**（判据都写在这儿，别再来一遍）：
            //     ① **`_ctx` 不会被清** —— 这里走的是**无参** `TryOpen()`（`Shell/WindowsManager.cs`：那条重载
            //        只调 `OpenByState()`、**不调 `SetupData`**）⇒ `Build()` 拿到的仍是本段 ⑨ 灌进去那个 `_ctx`；
            //     ② **即使 `_ctx == null` 也不炸** —— `RewardWindow.Build()` 第一句就是
            //        `var ctx = _ctx ?? new RewardWindowContext { Rewards = new RewardSpec[0] }`（`Shell/RewardWindow.cs`）。
            //   ⚠️ **重开之后再关，会【再发一次】`_ctx.OnClose`**（`Shell/RewardWindow.cs` 的 `Close()` 头一句
            //      就是 `cb = _ctx?.OnClose; if (cb != null) cb(…)`）—— 那是「**每关一次发一次**」，
            //      语义自洽；上面那条 `Check(closeCalls, 1, …)` 在**本块之前**、断的仍是它自己那一次关窗
            //      ⇒ 它照旧绿。本块之后 `closeCalls` 会是 2，而**它本来就是本块 `{ }` 里的局部量、
            //      后面零引用**（`:6466` 之后 `rw` 也零引用 —— 已 `grep` 核过）。
            //   ✅ **终态不变**：本块结束 = `rw` 在 `Closed`（与加本块之前**同一个终态**）⇒ 后面
            //      `CollectDaily(0)` / `OpenRewardWindowCount() == 0` 那两条吃的前提一个字没动。
            CheckTrue(rw.TryOpen(), "（A796 现场）把领奖窗开回来 —— 下面那条要在**开着**的窗上点");
            MenuDraw.CheckShadeClickRule(CheckTrue, "领奖窗", rw.transform,
                                         FindChild(rw.transform, "BackgroundHit"), () => rw.CurrentState);
            // 🔴 **这里【不许】再写 `wm2.CloseAllWindows();`** —— **2026-10-11（批次1 · F7）就地订正（铁律 5）**：
            //    戊2 原来把它放在这一行，而**主壳窗 `win` 也在 `openWindows` 里**
            //    （本文件 `:611` 那句 `wm.OpenWindow(win)`）⇒ `CloseAllWindows()` 逐扇 `Close()`
            //    （`Shell/WindowsManager.cs` 的 `CloseAllWindows`）**把壳也一起关了**（`GameWindow.Close()` 尾句
            //    `gameObject.SetActive(false)`，`:458`）⇒ 紧接着那 4 张「屏上只有壳」的截图
            //    （`01_日常_Missions` / `02_战役` / `03_锻造厂` / `03b_锻造厂_不可领`）**拍出来是空帧**
            //    （相机 `clearFlags = SolidColor` + `Color.black`，本文件 `:596-597`），
            //    4 张 **md5 完全相同**（各 27 260 B）、平均亮度 **0.0**，而**一条断言都不报**（静默回归）。
            //    ⇒ 它已挪到那 4 张**之后**（§四 那一节的开头，附「为什么」）；这一节要清也只清弹窗、⛔ 别动壳。
            //    🔑 **同类改动以后靠护栏拦**：那 4 张改用 `ShootShell(win.gameObject, …)` —— 拍之前先断壳还开着。
        }

        // ============================================================ 弹窗队列的次序（跨页）
        // 🔴 **唯一能自动拦住「弹窗被页底板盖住」的判据** —— 这种错**矩形断言量不到**（件都在、位置也对）。
        //    2026-09-23 实测过一次：`PromptPopup` 原来在 3018…3023，而 `CampaignTab` 的底板到 3064。
        Section("渲染队列的次序：弹窗 > 页 > 窗（不许有交叉）");
        {
            int pageMax = Mathf.Max(Mathf.Max(ForgeTab.QTabHelp, CampaignTab.QPanelTimer), RewardsWindow.QOverlay);
            int popupMin = Mathf.Min(CampaignRewardWindow.QShade, PromptPopup.QShade);
            CheckTrue(pageMax < popupMin,
                      $"**所有「页」的最高队列 {pageMax} < 所有弹窗的最低队列 {popupMin}**"
                      + $"（页：`RewardsWindow` 3014 · `ForgeTab` {ForgeTab.QTabHelp} · `CampaignTab` {CampaignTab.QPanelTimer}；"
                      + $"弹窗：`CampaignRewardWindow` {CampaignRewardWindow.QShade} · `PromptPopup` {PromptPopup.QShade}）"
                      + " —— ⚠️ 新增任何一页都要回头看这个数");
            CheckTrue(PromptPopup.QText > CampaignRewardWindow.QVignette,
                      "通用弹窗在最上层弹窗之上（`PromptPopup` > `CampaignRewardWindow`）");
        }

        Section("领奖：点了必须有反应（红线：不许静默失败）");
        DailyData.ForceCollectableForTest();
        int before = Wallet.Of("40k_topmarquee_currency_gold");
        DailyData.CollectDaily(0);
        CheckTrue(Wallet.Of("40k_topmarquee_currency_gold") >= before, "领每日任务后**资源记账动了**（原版走 PlayFab 云脚本，单机本地兑现）");
        // 🆕 **2026-10-12（A313）**：这一句是**直调**（不是点钮），但它同样会弹那扇领奖窗 ——
        // 而**紧接着就是那 4 张「屏上只有壳」的截图**（`:5088` 起）⇒ 不关掉，那 4 张会**一起被盖住**
        // （正是 A352 那张 `05_收件箱_空态.png` 的形状：`Shoot` 的「亮度 > 3」护栏**抓不到**它 —— 那扇窗是亮的）。
        // ⛔ 别改用 `wm2.CloseAllWindows()`：主壳窗也在 `openWindows` 里 ⇒ 壳会被一起关掉、4 张拍成**空帧**
        // （本文件 `:5046` 那条订正注释记的就是这个坑）。只关领奖窗 ⇒ 壳从 `Background` 回 `Open`。
        CheckTrue(RewardWindowFixture.FindOpenRewardWindow() != null,
                  "★ A313：`CollectDaily(0)` 领到 ⇒ **弹出 `Reward Window`**（直调那条路也走同一个出口）");
        Check(RewardWindowFixture.DismissRewardWindows(), 1, "★ …并把它关掉（接着那 4 张「屏上只有壳」的截图不许被它盖住）");
        Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ …现在场上**一扇遗留下来开着的领奖窗都没有**");


        // 🔴 **四张「屏上只有壳」的截图必须在这里拍** —— 下面 §四/§五 会开别的窗，而它们结尾的
        //    `CloseAllWindows()` 会把**壳窗一起关掉** ⇒ 拍出来是**空图**（2026-09-23 踩到：
        //    截图还在、内容没了，而断言一条都不会报；2026-10-11 又踩一次 —— 见下面 `ShootShell` 的说明）。
        //    **先拍完再往下开窗。**
        //    🔴 **2026-10-11（批次1 · F7）**：这 4 张改用 `ShootShell`（= 拍之前先断**壳还开着**）——
        //      原来只有「空图护栏」（平均亮度 > 3），它**报得出「黑了」、报不出「为什么黑」**
        //      （4 张 md5 完全相同是最强线索，但得人去看）。
        //    🔴 **2026-10-11（W3 · A300）**：这 4 张再收一道 —— 改走 `ShootShellPage(win, 页, 文件)`，
        //      拍之前**先断当前页签就是它要拍的那一页**。原来 `01_日常_Missions.png` 拍的是**锻造厂页**
        //      （与 `03_锻造厂.png` **字节完全相同**，md5 `5fc843461f439aef77e513f00aec4d3f`）——
        //      成因是上一句拍摄之前最后一次切页签的是 `Click(2)`，把页切回日常的那句写在**下一行**。
        win.tabButtons.Click(0);                                  // 视觉第 1 键 = Missions（**A300：拍它之前必须真的切回来**）
        ShootShellPage(win, WindowTabType.Missions, "01_日常_Missions.png");
        win.tabButtons.Click(1);                                  // 视觉第 2 键 = Campaign
        ShootShellPage(win, WindowTabType.Campaign, "02_战役.png");  // 2026-09-23：这一页**不再是空页**（`CampaignTab` 已建成）
        // 🆕 **2026-10-16（A815）第四张：自建的「每日连胜」页**（前三张的编号 / 文件名**一个字没动**，新的一张排 04）。
        //    ⚠️ 拍完**必须切回 `Campaign`** —— 紧接着下面那几条要量 `Campaign Tab` 的 TMP
        //    （未激活的页上 `textBounds` 是垃圾 ⇒ 那几条会**假绿 / 假红**，见本节那段长注释）。
        win.tabButtons.Click(3);                                  // 视觉第 4 键 = Daily Streak（🆕 A815 自建）
        ShootShellPage(win, WindowTabType.DailyStreak, "01b_日常_每日连胜.png");
        win.tabButtons.Click(1);                                  // ⚠️ 切回 `Campaign`（下面那几条依赖它正显示时量）
        // 🔴 **文字必须真的落在框里**（`资料/日常_画面逐项对_0923.md` D10 那条：原版是 `H=Left/Right`、
        //    我们画成居中 ⇒ 字压在别的件上，而**矩形断言全绿**）。这里量的是 `Label.WorldW`（TMP 真测量）。
        //    ⚠️ **必须在页面「正显示时」量** —— 未激活时 TMP 的 `textBounds` 是旧值/垃圾
        //    （实测量出 2.3e11；见 `已知的坑.md` 那条「面板画出来了、字不在」的坑①）。
        var camView = FindChild(FindChild(area, "Tabs"), "Campaign Tab");
        var cpan2 = FindChild(camView, "Premium Panel");
        CheckTrue(TextRightPx(FindChild(cpan2, "Title")) <= 720.35f + 1f,
                  $"`Premium Panel/Title` 的**右边缘 ≤ 面板右边 720.35**（原版 `TMP(右/上)`；"
                  + $"实测 {TextRightPx(FindChild(cpan2, "Title")):F1}）");
        // 🔴 **左边缘那一条 2026-10-11（A281）换掉了** —— 原文断的是「左边缘 ≥ 它自己的框左边 354.43」，
        //    推论写的是「原版 `m_enableAutoSizing = 1` ⇒ 装不下就**缩字号**，不是溢出」。
        //    🔴🔴 **2026-10-11 就地订正（批次1 · F5 · 铁律 5）：A281 当年的【理由与结论都是反的】。**
        //      原话：「TMP 横向那一支「Text Exceeds Horizontal Bounds - Reducing Point Size」
        //      （`TextMeshProUGUI.cs:3917`）**整块长在 `if (m_TextWrappingMode != NoWrap …)` 那道闸里**
        //      （同文件 `:3565`）⇒ `NoWrap` 时它一次都不跑 ⇒ **原版这条就是横向溢出**」。
        //    ⛔ **错因 = 括号配错**：那道闸的 `{`（`:3566`）**闭合在 `:3888`** · `else` 在 **`:3889`**，
        //      而 `:3917` 的 `#region Text Exceeds Horizontal Bounds - Reducing Point Size` **正落在那 `else` 里**
        //      （实配：`:3890` 的 `{` 闭合于 `:4006`）⇒ **`NoWrap` 时闸不成立、直接落进 `else` ⇒ TMP 照样缩字号。**
        //      我们实际用的那份（`Label._tmp` 是 `TextMeshPro`，不是 UGUI）同构，逐字配过
        //      （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshPro.cs`）：
        //      闸 `:3216` / `{`:3217 **闭合于 `:3539`** · `else :3540` / `{`:3541（闭合于 `:3657`）
        //      · 那一支的 `region :3568`；且 `:3540` 这个 `else` 配的是**闸**，不是外层那条
        //      `if (isBaseGlyph && textWidth > widthOfTextArea…)`（`:3211`/`{`:3212，闭合于 `:3660`）。
        //      （`else` 里先试的「Character Width Adjustments」那一支被原版 `m_charWidthMaxAdj = 0` 跳过 ⇒ 直接缩字号。）
        //    🔴 **正确的行为**：`NoWrap` + 框 355.79 ⇒ TMP 把这段 28 字的串**缩到「一行刚好塞得进框」**，**不溢出**；
        //      **原版也一样** —— 那两条 MB 里还有一个 A281 **没引**的字段 **`m_fontSizeBase = 12`**，
        //      而 `TextMeshPro.cs:2149` 每次重排都 `m_fontSize = Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`
        //      = 从 **12** 起算，再二分**涨**到「一行塞得下」为止（`m_enableAutoSizing = 1`）
        //      ⇒ **原版画出来的同样是一行、贴框宽**。
        //      ⚠️ 正本 `资料/阶段二_锻造厂与战役页_原版规格.md:676` 那句「框本身就装不下」**只对序列化的
        //      `m_fontSize = 33.3` 这一个字段成立**（同批已订正，保留了更正痕迹）。
        //    🔴 **A281 新加的第 ③ 条（「渲出宽 **>** 框宽」）因此改前改后都不可能绿**：**改前**的日志
        //      （`_tmp_view/rewards_1006{,.b,.c}.log` 里旧那条「左边缘 ≥ 354.43」）打的就是
        //      **左沿 357.1 / 右沿 710.2 ⇒ 宽 353.1**，与今天**逐位相同** ⇒ 本批**改了字段、没改渲染**。
        //      ⇒ 下面把它**反向**成「渲出宽 **≤** 框宽 + 1」（反向而不是删，理由写在那条断言自己的注释里）。
        //    ⛔ **仍然【不去】断「收敛到多少 px」**：那个数**没人查过**，而且它依赖字体资产（原版那份 ≠ 我们这份）
        //      ⇒ 只断**上界**（与字体无关）。
        //    现在这三件**确定**的事：① 折行模式 = 原版的 **0**；② 渲出来**就一行**；③ 渲出宽 **≤ 框宽 355.79**。
        //      咬住「`SetWrapping(false)` 被删掉」的是 **①**（字段级、唯一判别式）—— ③ 对那件事没有判别力
        //      （折行一开，宽同样 ≤ 框宽、两种状态都绿）；② 在那种状态下**应当**也会红，但**没跑过、不打包票**。
        {
            var cpanT = FindChild(cpan2, "Title");
            var cpanTLb = cpanT != null ? cpanT.GetComponentInChildren<Label>() : null;
            CheckTrue(cpanTLb != null, "（A281 前提）`Premium Panel/Title` 的 `Label` 取得到（下面三条才量得成）");
            if (cpanTLb != null)
            {
                CheckTrue(cpanTLb.WrappingMode >= 0,
                          "（A281 前提）`Premium Panel/Title` 走的是 **TMP 后端**"
                          + "（`-1` = 点阵后端 ⇒ 下面两条渲染断言不成立，**如实红、不假装**）");
                Check(cpanTLb.WrappingMode, 0,
                      "★ `Premium Panel/Title` 的**折行 = 0**（原版那条 TMP 的 `m_TextWrappingMode = 0`）"
                      + " —— 改坏法：删掉 `Shell/CampaignTab.cs` 里 `SetAutoFitBox` 之后那句 `SetWrapping(false)` ⇒ 回 1");
                Check(cpanTLb.LineCount, 1,
                      "★ …而且这段 28 字的串**渲出来就一行**（折行=1 时它会折成两行 —— 折行那一版正好塞进"
                      + " 355.79 的框里，所以旧的「左边缘 ≥ 框左边」在那种状态下反而是绿的）");
                // 🔴 **2026-10-11（批次1 · F5）**：这一条原来是「渲出宽 **>** 框宽 355.79」，**判据方向反了**
                //    （理由见上面那段订正）⇒ 现在反向成**上界**。**为什么是反向、不是删**：
                //    ① 它管的是「字有没有**溢出框**」这一类 —— 同一族的注释（下面 `CheckFontWindow` 那段）写着
                //       「字号对而溢出、溢出对而字号错，各自都要一条」⇒ 删掉这一类就**没人管了**；
                //    ② 上界**与字体资产无关**（原版那份 TMP font asset ≠ 我们这份 ⇒ 收敛到的具体 px 必不同）
                //       ⇒ 不会变成「拿一个没核过的数当判据」；
                //    ③ 它**照样会红**：删掉 `Shell/CampaignTab.cs` 那句 `SetAutoFitBox` ⇒ 不缩字号、33.3 号
                //       渲出 ≈545px（同一文件 `:717` 记着实测）⇒ 这条立刻红。
                //    ⚠️ 它对「删掉 `SetWrapping(false)`」**没有**判别力（折行一开、宽也 ≤ 框宽，两种状态都绿）——
                //       咬那件事的是上面那条字段级断言（`WrappingMode == 0`），如实说。
                CheckTrue(TextRightPx(cpanT) - TextLeftPx(cpanT) <= 355.79f + 1f,
                          "★ …而且它**没有溢出框**（原版框 355.79 —— `NoWrap` 走的是折行闸的 `else`，"
                          + "`TextMeshPro.cs:3568` 那一支缩字号把一行塞进框里）："
                          + $"实测渲出宽 {TextRightPx(cpanT) - TextLeftPx(cpanT):F1}px"
                          + "（改坏法：删掉 `Shell/CampaignTab.cs` 那句 `SetAutoFitBox` ⇒ 不缩字号 ⇒ 这条红）");
            }
        }
        // 🆕 **2026-10-09（A274）**：`Premium Panel/Title` 那颗的**自适应窗口**（不是边缘，是**区间**）——
        //   上面那条只量「渲出来的**右**边缘」（左边缘那一条 A281 换成了「折行 / 一行 / **不溢出**」那三条
        //     —— 第三条 2026-10-11 由「比框宽」反向成「≤ 框宽」，见上面那段订正），
        //   而窗口上下界是**另一件事**（`已知的坑.md`：字号对而溢出、溢出对而字号错，各自都要一条）。
        //   判据 = 原版那条 TMP 的字段原文（`bundle_menus_assets_all/MonoBehaviour/` 里那条
        //   `m_text = "Premium Campaign daily bonus"` 的；两份实例 `MonoBehaviour_-818462233560502899.json`
        //   与 `…1918117691617384191.json` **逐位一致**）：
        //   **`m_fontSizeMin = 10`** · 🔴 **`m_fontSizeMax = 40`**（而 `m_fontSize = 33.3` —— **上限 ≠ 字号**，
        //   同 `Shell/OfferContainer.cs` 的 `TimerTextFit` / `Editor/BattleScene.cs` §4.6 那条）。
        //   ⚠️ `CheckFontWindow` 读的是 `Label.FontSizeMin/Max`（= **TMP 里那两个真字段**，经工程唯一那份
        //   `Label.FontSizeToPx` 折成画布 px），⛔ **不是**我们传进 `SetAutoFitBox` 的实参。
        //   **改坏法**：把 `Shell/CampaignTab.cs` 那行的第 4 个实参改回 `33.3f` ⇒ 上限读成 33.3 ≠ 40 ⇒ 这条红。
        CheckFontWindow(cpan2, "Title", 10f, 40f,
                        "★ `Premium Panel/Title`（`Premium Campaign daily bonus`）的自适应窗口"
                      + " = 原版 `m_fontSizeMin/Max` 的 **10 / 40 px**");
        CheckTrue(TextLeftPx(FindChild(cpan2, "Timer Text")) >= 408.63f - 1f,
                  $"`Premium Panel/Timer Text` 的**左边缘 ≥ 它自己的框左边 408.63**（原版 `TMP(左/上)`；"
                  + $"实测 {TextLeftPx(FindChild(cpan2, "Timer Text")):F1}）");
        CheckNear(TextLeftPx(FindChild(FindChild(camView, "Campaign Header"), "Title")), 480.69f, 2f,
                  "`Campaign Header/Title` 的**左边缘 = 框左边 480.69**（原版 `TMP(左/中)`）");
        win.tabButtons.Click(2);                                  // 视觉第 3 键 = Forge（第 3 层第 1 件）
        ShootShellPage(win, WindowTabType.Forge, "03_锻造厂.png");
        // 🔴 再拍一张**不可领**的：当前阵营（Goff）是可领态，`Ready for level up` 那团 700² 的洋红光
        //    会把**旋涡大图 `Warp`** 整个盖住（原版兄弟序就是光效在背景之上）⇒ 只看那一张会以为旋涡没画。
        //    换一个「差一点」的阵营，光效灭、旋涡露出来 —— 这一下**同时验了 `SelectArmy`**。
        var fgo = forge != null ? forge.GetComponent<ForgeTab>() : null;
        if (fgo != null)
        {
            fgo.SelectArmy("SaimHann");                           // 第 3 个阵营：差 40 点 ⇒ `InProgress`
            ShootShellPage(win, WindowTabType.Forge, "03b_锻造厂_不可领.png");
        }
        // 🔴 **2026-10-11（W3 · A300）的直接判据**（不依赖「页签对不对」那条代理判据）：
        //    这几张**不许有两张字节完全相同** —— A300 就是这么被发现的
        //    （`01_日常_Missions.png` 与 `03_锻造厂.png` 曾经 md5 完全相同 = 同一页拍了两次，
        //     而当时**一条断言都不报**，只有人去看 md5 / 并排比才发现）。
        //    ⚠️ **2026-10-16（A815）**起多了一张 `01b_日常_每日连胜.png`（自建页）⇒ 它也有两条（见下）。
        CheckTrue(!SameBytes(Path.Combine(ShotDir, "01_日常_Missions.png"),
                             Path.Combine(ShotDir, "03_锻造厂.png")),
                  "★ 拍出来的 `01_日常_Missions.png` 与 `03_锻造厂.png` **不是同一张图**"
                  + "（A300：曾经字节完全相同 —— 拍错页；改坏法：删掉上面那句 `Click(0)` ⇒ 立刻相同）");
        CheckTrue(!SameBytes(Path.Combine(ShotDir, "01_日常_Missions.png"),
                             Path.Combine(ShotDir, "02_战役.png")),
                  "★ …`01` 与 `02` 也不是同一张图（同一个改坏法：把 `Click(0)`/`Click(1)` 去掉任一句）");
        // 🆕 **2026-10-16（A815）**：新拍的这一张（自建「每日连胜」页）**一并进这个两两互异的口径** ——
        //    理由与上面两条逐字相同（A300：拍错页**一条断言都不报**，只有人去看 md5 才发现）。
        //    **改坏法**：把 `01b` 那句前面的 `Click(3)` 删掉或改成别的号 ⇒ 拍成上一页 ⇒ 这两条里至少一条红。
        CheckTrue(!SameBytes(Path.Combine(ShotDir, "01b_日常_每日连胜.png"),
                             Path.Combine(ShotDir, "01_日常_Missions.png")),
                  "★ A815：`01b_日常_每日连胜.png`（自建页）与 `01_日常_Missions.png` **不是同一张图**");
        CheckTrue(!SameBytes(Path.Combine(ShotDir, "01b_日常_每日连胜.png"),
                             Path.Combine(ShotDir, "03_锻造厂.png")),
                  "★ …`01b` 与 `03_锻造厂.png` 也不是同一张图");
        win.tabButtons.Click(0);

        Section("🔴 红点靠 **alpha**、不靠 `SetActive`（原版 `UiBadgeNotification.Show()/Hide()`）");
        var badge0 = FindChild(FindChild(bar, "RewardsTabButton_0"), "Badge Highlight");
        var badge1 = FindChild(FindChild(bar, "RewardsTabButton_1"), "Badge Highlight");
        CheckTrue(badge1 != null && badge1.gameObject.activeSelf,
                  "第 2 键的红点节点**是激活的**（原版出厂四个都 `active=1`）—— 这条正是「`SetActive` 不是判据」");
        CheckNear(AlphaOf(badge1), 0f, 0.01f, "第 2 键（Campaign）的红点 **alpha = 0**（没有通知源 ⇒ 原版 `Hide()` 的样子）");
        // 两态都验：开窗那一瞬（`ForceCollectableForTest` 还没跑）应当是 0；刷新之后应当变 1
        CheckNear(AlphaOf(badge0), 0f, 0.01f, "开窗时没有「可领」的任务 ⇒ 第 1 键红点 alpha = 0");
        DailyData.ForceCollectableForTest();
        win.RefreshBadges();
        CheckNear(AlphaOf(badge0), 1f, 0.01f,
                  "造出「可领」之后 `RefreshBadges()` ⇒ 第 1 键红点 alpha = 1（原版 `UiBadgeNotification.Show()`）");

        // 🔴 **2026-10-11（批次1 · F7）**：戊2 加的那句 `wm2.CloseAllWindows();` 从**奖励窗那一节末尾挪到这里**
        //    —— 它排在几张「只有壳」的截图（`01` / `01b` / `02` / `03` / `03b`）**之前**时，会把**拍摄对象 `win` 一起关掉**
        //    ⇒ 空帧（全黑、md5 相同、静默）。放在这里的语义 = 「进 §四 之前把场上的窗清干净」，与旧代码等价
        //    （旧文件首个 `CloseAllWindows()` 排在 `03_每日奖励` 那张**之后**）；那几张本来就是在**只有壳**的现场拍的。
        //    ⚠️ **别把这句再往前挪** —— 前面那几张已改用 `ShootShell`（拍之前先断壳还开着），挪了会立刻红。
        wm2.CloseAllWindows();

        // ============================================================ 🆕 **2026-10-13（批次 4 · W-E4b）**
        // §四 / §五 两段新落的**两批断言**共用的两个「量文字的边」的助手（定义在这里 ⇒ 两段都取得到）。
        // 🔴 **共同口径**：⛔ **都不读 `Label.WorldW`**（那是缓存 `_tmpW` —— 读它 = 把我们自己写进去的数读回来，
        //    与 A490 / `H37` §五 那条同因；本文件现成的 `TextLeftPx/TextRightPx` 走的正是那条路，只当快速回归用）。
        //    两条量的都是 **TMP 自己的输出**：① mesh 顶点 · ② `textBounds`。
        // 🔴 **2026-10-14 就地订正（铁律 5）**：这一句原来写的是「两条在本工程的口径下**应当逐值相同**」
        //    —— **那是错的**（#53/#54 的根因就是照它取期望值）。两条量的是**两个不同的量**：
        //    ① 量 **`textInfo.meshInfo[<该字的 materialReferenceIndex>].vertices`**（= 字形**墨迹**；A851 起按槽取，
        //       原来是写死 `[0]`）· ② 量 **TMP 的 `textBounds`**（= **排版框**）。
        //    （旧注释里那句「渲染左缘 = `PxOf(pos.x) − WorldW·108/2`」**只对 ② 成立** —— `Battle/Label.cs` 的 `RefreshBounds`
        //     的 `RefreshBounds` 把 **`b.min.x`（框）**摆到 `−anchor.x·W`；`MenuDraw.Text` 建的标签 anchor = (.5,.5)。）
        //    ① 的墨迹起点还要多一个**首字左边距**
        //    （实测 `Current Streak`：43.00 vs 45.22 ⇒ 2.22px @ fs70 ≈ 0.032em；字越少差得越小
        //     —— `Current Streak Value`（1 字）只差 1.35px）。
        //    ⇒ **两条对不上不是「必有一边错」，而是常态**；要比就得同尺子比（A517 那两条已改用 ②）。
        //    留两条**仍然是有意的**（两批规格各带一条、互相独立）：① 抓「字形没画出来」（NaN 那一支）·
        //    ② 抓「排版框摆错」—— 但**期望值必须与尺子配对**。
        //
        // ① `TmpEdgePx`：一段文字**真渲出来的**左/右缘（画布 px）—— 走 TMP 的 mesh 顶点
        //    （`TmpVertPx` = `textInfo.meshInfo[<该字的 materialReferenceIndex>].vertices` 经 `TransformPoint` + `ToPixel`，
        //     本文件现成的；A851 起按槽取，原来是写死 `[0]`）。
        //    ⚠️ 取不到（`Label` 不在 / 一个顶点都没有）回 `NaN` ⇒ 与任何期望值比都是**红**，正好当「这件没建出来」用。
        System.Func<Transform, bool, float> TmpEdgePx = (t, right) =>
        {
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            var vs = lb != null ? TmpVertPx(lb) : null;
            if (vs == null || vs.Length == 0) return float.NaN;
            float r = right ? float.MinValue : float.MaxValue;
            for (int i = 0; i < vs.Length; i++) r = right ? Mathf.Max(r, vs[i].x) : Mathf.Min(r, vs[i].x);
            return r;
        };
        // ② `edgePx`：同一个量的**另一条量法** —— 走 TMP 自己的 `textBounds`（四角 × `localToWorldMatrix`）。
        //    ⚠️ 与 ① 一样**不许**碰 `Label.WorldW`（同一条病灶）。
        System.Func<Transform, bool, float> edgePx = (t, left) =>
        {
            var tmp = t != null ? t.GetComponentInChildren<TMPro.TextMeshPro>() : null;
            if (tmp == null) return float.NaN;
            var b = tmp.textBounds; var M = tmp.transform.localToWorldMatrix;
            float r1 = float.MaxValue, r2 = float.MinValue;
            for (int c = 0; c < 4; c++)
            {
                var p = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? b.min.x : b.max.x,
                                                       (c < 2) ? b.min.y : b.max.y, 0f));
                float px = PxOf(p.x); r1 = Mathf.Min(r1, px); r2 = Mathf.Max(r2, px);
            }
            return left ? r1 : r2;
        };
        // ③ `teeth`：**牙口自报**（只拼**日志文案**，⛔ 不是判据、也不改任何断言）。
        //    A524 那 8 条的改坏法都是「删掉那一句 `MenuDraw.Align*`」⇒ 文字块回到**框心**
        //    ⇒ 位移 = `(框宽 − 本颗渲染宽) / 2`。**位移 ≤ 断言容差(1.5px) 的那几颗就是「两态同形、无牙口」**
        //    （原版那两颗 `Current Streak` / `Current Streak Value` 的框是 `CSF(h:PreferredSize)` 撑出来的
        //     ⇒ 框宽 ≈ 原版文字宽 ⇒ 很可能正是这一档）。
        //    🔴 本批**跑不了 Unity** ⇒ 把这两个数**打进日志**，让同步点那次 `RewardsScene.Run` 的输出
        //    直接把「哪几条无牙口」写成事实（红线：⛔ 不许把「没验」写成「验过了」）。
        System.Func<Transform, float, float, string> teeth = (n, boxX1, boxX2) =>
        {
            float l = edgePx(n, true), r = edgePx(n, false);
            return $"（牙口自报：框宽 {boxX2 - boxX1:F2}px · 本颗渲染宽 {r - l:F2}px"
                 + $" ⇒ 删掉 `Align*` 后位移 {(boxX2 - boxX1 - (r - l)) * 0.5f:F2}px；"
                 + "位移 ≤ 1.5px ⇒ 本条与「不接对齐」**两态同形**、无牙口）";
        };

        Section("§四 `Daily Reward Popup`（每日奖励窗 —— 四态 + 双轨侧栏，2026-09-23 建）");
        var dr = DailyRewardPopup.Create(wm2);
        wm2.OpenWindow(dr);
        Check(dr.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（实证）");
        Check(dr.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（**0 配 15 就是原版的组合**）");
        Check(dr.closeOnEsc, true, "`closeOnESC` = 1（实证；与奖励窗的 0 不同）");
        Check(dr.entries.Count, DailyData.RewardDays, $"`Rewards Content` 下 {DailyData.RewardDays} 个 `Entry`");
        // `Rewards Content` 的 HLG 实测 **spacing = −64**、align=3(MiddleLeft) ⇒ 相邻两格**故意重叠 64**
        if (dr.entries.Count >= 2)
            CheckNear(Mathf.Abs(dr.entries[1].position.x - dr.entries[0].position.x) * 108f,
                      DailyRewardPopup.EntryPitch, 1f,
                      "相邻 `Entry` 的中心距(px)（= 315.028 − 64）");
        // 四态（`GetCurrentState` 的公式照原版；采样数据 `资料/日常_原版规格.md` §四 + `DailyData` 里已标明「我们挑的」）
        var states = new List<RewardState>();
        for (int i = 0; i < DailyData.RewardDays; i++) states.Add(DailyData.RewardStateOf(i, false));
        CheckTrue(states.Contains(RewardState.Collected) && states.Contains(RewardState.Unlocked)
                  && states.Contains(RewardState.Locked),
                  "这一屏同时覆盖 `Collected` / `Unlocked` / `Locked` 三态（采样数据）");
        Check(DailyData.RewardStateOf(0, true), RewardState.PremiumLocked,
              "Premium 轨恒为 `PremiumLocked`（⚠️ 我们挑的：单机不卖 Premium，边界②）");
        if (dr.entries.Count > 0)
        {
            // `SetState` 表（`DF:DailyRewardDrawerController__SetState.c:8-28`）：
            //   `colider` 只有 Unlocked 开 · `Gacha Reward Claimed` 只有 Collected 开 · `Premium Indicator` 只有 PremiumLocked 开
            var e0 = dr.entries[states.IndexOf(RewardState.Collected)];
            var e1 = dr.entries[states.IndexOf(RewardState.Unlocked)];
            var c0 = FindChild(e0, "colider");
            CheckTrue(c0 != null && !c0.gameObject.activeSelf, "`Collected` 那格的 `colider`（点击区）是**关的**");
            var g0 = FindChild(e0, "Gacha Reward Claimed");
            CheckTrue(g0 != null && g0.gameObject.activeSelf, "`Collected` 那格亮 `Gacha Reward Claimed`");
            var c1 = FindChild(e1, "colider");
            CheckTrue(c1 != null && c1.gameObject.activeSelf, "`Unlocked` 那格的 `colider` 是**开的**（可点）");
            // ⚠️ 每个 Entry 有**两个**抽屉，两个都带 `Premium Indicator` ⇒ 必须限定在 `Premium Reward` 子树里找
            var premNode = FindChild(e1, "Premium Reward");
            var p1 = FindChild(premNode, "Premium Indicator");
            CheckTrue(p1 != null && p1.gameObject.activeSelf,
                      "`Unlocked` 那格的 **Premium 抽屉**亮 `Premium Indicator`（普通抽屉的该件是关的）");
            // ======================================================== ★ **A524（A493 #7）**
            // `Claimed Tex` 那一段字：原版 `对齐=Left/Midline`、框左沿 **255.1**
            //   （`menu_dump.py … "Daily Reward Popup" --depth 12 --no-sprite` 实读，**8 份实例逐值相同**：
            //    `255.1 244.2 428.1 276.0` / `255.1 638.7 428.1 670.5` …）。
            // ⚠️ 这一格**只有 `Collected` 才亮** ⇒ 必须取 `Collected` 那格（就是上面那个现成的 `e0`）。
            // **改坏法**：删掉 `Shell/DailyRewardPopup.cs` 的 `BuildDrawer` 里那句
            //   `MenuDraw.AlignLeft(claimedTx, R(D_ClaimedTex));` ⇒ 文字块回到框心 ⇒ 左缘右移 `(框宽−文字宽)/2` ⇒ 红。
            Section("★ A524：A493 #7 `Claimed Tex` 的真渲染左缘 = 原版框左沿 255.1");
            CheckNear(edgePx(FindPath(e0, "NormalReward/Gacha Reward Claimed/Claimed Tex"), true), 255.1f, 1.5f,
                      "★ A493#7：`Claimed Tex` 的真渲染左缘 = **255.1**（原版 `Left/Midline`，8 份实例同值）"
                    + " —— ⛔ 量的是 TMP 自己的 `textBounds`，不是 `Label.WorldW`（同 A490 那个病灶）"
                    + teeth(FindPath(e0, "NormalReward/Gacha Reward Claimed/Claimed Tex"), 255.1f, 428.1f));
        }
        // ============================================================ ★ **A640（A516）** + **A524（A493 #8）**
        // A640：原版 `Timer` 底下是**三件**（'More Rewards In' + 时钟图 + 倒计时），我们原来只建了两件。
        //   判据 = 本件亲跑 `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12`
        //   （`245.5 990.8 → 935.0 1054.1` · `字号=50.0` · **`对齐=Right/Capline`**）+
        //   `资料/说明书/04_界面UI/菜单全树.md:1092` + 正本 `资料/日常_原版规格.md:456`（「= 三件」）。
        //   改坏法：删掉 `Shell/DailyRewardPopup.cs` 的 `BuildTimer` 里那句 `MenuDraw.Text(t, TimerMore, …)`
        //           ⇒ 下面五条**全红**（那颗直接没了）；只把那句的 `AlignRight` 写成 `AlignLeft` ⇒ 第 ③ 条红
        //           （右缘 = 245.5 + 文字宽 ≠ 935.0）。
        Section("★ A640/A524（§四 段）：A516 `Timer` 三件里的第一件 + A493 #8 倒计时那颗");
        {
            var timerN = FindChild(dr.transform, "Timer");
            var moreN = FindChild(timerN, "EverguildTextMeshPro");     // ⚠️ 直接子件取不到才退到递归 —— 这里正好都是直接子件
            CheckTrue(moreN != null && timerN != null && moreN.parent == timerN,
                      "★ A516：`Timer` 底下那颗 `EverguildTextMeshPro`（'More Rewards In'）在（原版三件里的第一件）");
            Check((timerN != null ? timerN.childCount : -1), 3,
                  "★ A516：`Timer` 的直接子件**恰好三件**（原版 = 'More Rewards In' + `WF_icon_clock` + 倒计时；"
                + "改成两件那一版 ⇒ 红）");
            CheckNear(TmpEdgePx(moreN, true), 935.0f, 1.5f,
                      "★ A516：那颗的**真渲染右缘** = 框右沿 **935.0**（原版 `对齐=Right/Capline`；"
                    + "写成 `AlignLeft` ⇒ 右缘 = 245.5 + 文字宽 ⇒ 红）");
            var moreLb = moreN != null ? moreN.GetComponentInChildren<Label>() : null;
            CheckTrue(moreLb != null && Mathf.Abs(moreLb.FontPxNow - 50f) <= 0.5f,
                      "★ A516：那颗的字号 = 原版 **50**（px 口径 `FontPxNow`；改坏法：写成 36 ⇒ 红）");
            CheckTrue(moreLb != null && Mathf.Abs(moreLb.CharSpacing) <= 0.01f,
                      "★ A516（**对照条**）：这一颗原版**没有**字距（dump 的 `字距=` 列不出现）—— "
                    + "防的是「把 A517 那个 5 一刀切到所有窗」（那样这条红）");
            // A524 #8：倒计时那颗 —— 原版 `Timer/EverguildTextMeshPro (1)`，`对齐=Left/Capline`、框左沿 **990.2**。
            //   ⚠️ 同一个 `Timer` 底下**方向相反**（上面那颗是 `Right/Capline`）⇒ ⛔ 别一刀切。
            //   **改坏法**：删掉 `Shell/DailyRewardPopup.cs` 的 `BuildTimer` 里那句 `MenuDraw.AlignLeft(tm, TimerText);` ⇒ 红。
            CheckNear(edgePx(FindPath(dr.transform, "Timer/EverguildTextMeshPro (1)"), true), 990.2f, 1.5f,
                      "★ A493#8：倒计时那颗的真渲染左缘 = **990.2**（原版 `Left/Capline`）"
                    + " —— 与上面那颗 `EverguildTextMeshPro`（`Right/Capline`）**同父不同向**"
                    + teeth(FindPath(dr.transform, "Timer/EverguildTextMeshPro (1)"), 990.20f, 1453.98f));
        }
        Check(dr.MissingArt.Count, 0, "每日奖励窗没有取不到的图");
        CheckHoverSwap(dr.transform, "每日奖励窗");
        Shoot("03_每日奖励.png");
        wm2.CloseAllWindows();

        Section("§五 `Daily Streak Popup`（每日连登窗 —— 两态互斥 + 奖格轨，2026-09-23 建）");
        var ds = DailyStreakPopup.Create(wm2);
        wm2.OpenWindow(ds);
        Check(ds.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（实证）");
        Check(ds.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（实证）");
        Check(ds.closeOnEsc, false, "`closeOnESC` = **0**（实测；⚠️ 与每日奖励窗的 1 相反）");
        // 🆕 A83②（A81 的尾巴）：压暗层（「点窗外关窗」）那条不变量 —— 档 = **压暗层自己那一档**
        //   `QShade`(3002)，**严格低于**本窗内容命中区档 `QContent`(3010)；并核「这节点确实是
        //   公共件 `MenuDraw.ShadeHit` 建的」。期望值全是本窗自己的**原版档常量**（⛔ 不从被测实现里读）。
        //   ⚠️ 本窗那条命中区的**动作**与顶栏返回钮**同源**（`DailyData.StreakAutoCollect(); Close();`，
        //   ⛔ 不是裸 `Close()` —— 判据见 `Shell/DailyStreakPopup.cs` 里那颗返回钮的动作 与 `资料/待办判据_阶段二与联机.md` §A81）；
        //   这条断言只核**档位不变量**，**别据此去改动作**。
        //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档 —— 量的是**场景真值**，
        //      ⛔ 不再传 `DailyStreakPopup.QShade`（那正是被测实现传进去的同一个符号 = 同义反复）。
        MenuDraw.CheckShadeRule(CheckTrue, "每日连登窗", ds.ShadeHit,
                                ds.transform.Find("Menu Dark Background"), DailyStreakPopup.QContent);
        Check(ds.entries.Count, DailyData.StreakDays, $"`Rewards Content` 下 {DailyData.StreakDays} 个奖格");
        // `Rewards Content` 的 HLG 实测 spacing = **−64** ⇒ 相邻两格**故意重叠 64**
        if (ds.entries.Count >= 2)
            CheckNear(Mathf.Abs(ds.entries[1].position.x - ds.entries[0].position.x) * 108f,
                      DailyStreakPopup.EntryPitch, 1f, "相邻奖格的中心距(px)（= 379.816 − 64）");
        // 🆕 **2026-10-11（A240）：条目的【纵向摆位】= 原版那个「竖向居中」**（原来顶对齐在 132.09，差 177px）。
        //   判据 = 原版 `Rewards Content` 那条 `HorizontalLayoutGroup`（`align = 3(MiddleLeft)` ·
        //   `pad = 2,0,58,0` —— ⚠️ 形状是 UGUI 的 `(L,R,T,B)` ⇒ **T 58 / B 0**，见 `DailyStreakPopup.ContentPadT`）：
        //   条目在**内缩后的内容区**里垂直居中 ⇒ 上沿 = `(132.09 + 58 + 944.19) / 2 − 516.301 / 2` = **308.99**、
        //   中心 y = **567.14**（`132.09 / 944.19` = 原版 `Rewards Content` 的 y 区间 ·
        //   `516.301` = 原版 `E_*` 那一族所在 prefab 的条目高；⛔ **不调 `DailyStreakPopup.EntryTop`**，
        //   那是被测实现 —— 期望值全部写原版字面量）。出处 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §四·4。
        //   **改坏法**：把 `Shell/DailyStreakPopup.cs` 那两条 `PxRect` 的 y 改回 `S_Content.y1`（顶对齐）
        //   ⇒ 中心回 390.24（差 176.9）⇒ 下面两条 + 末尾那条「高之比 = 1.2」一起红。
        if (ds.entries.Count > DailyData.StreakCollected + 1)
        {
            CheckNear(PxYOf(ds.entries[0].position.y), (132.09f + 58f + 944.19f) * 0.5f, 0.5f,
                      "★ 奖格的**竖向中心 = 567.14**（原版 HLG `align = 3(MiddleLeft)` + `pad T58/B0` ⇒ "
                      + "条目在内容区里**居中**；顶对齐那一版是 390.24）");
            // 渲出来的真值（不是节点位置）：第 6 格（= 放大 1.2 的那一格）`BG` 的**上沿** ——
            //   未放大时 `BG` 顶沿 = 308.99 + 26.95(`E_Bg.y1`) = 335.94，格心 567.14
            //   ⇒ 绕格心放大 1.2 = `567.14 − (567.14 − 335.94) × 1.2` = **289.70**。
            // 🔴 **这条是「放大那一格不再顶出视口」的判据**：顶对齐那一版算出来是 **80.46**（< 视口顶 159.33）
            //   ⇒ 会被 `Viewport` 裁到 159.33（原版不会；A240 那一行就是拿它记的账）。
            float tx1, ty1, tx2, ty2;
            if (RectOfUnion(FindChild(ds.entries[DailyData.StreakCollected], "BG"),
                            out tx1, out ty1, out tx2, out ty2))
                CheckNear(ty1, 567.14f - (567.14f - 335.94f) * 1.2f, 0.5f,
                          "★ 「第一个还没领的」奖格的 `BG` **上沿 = 289.70**（原版「竖向居中 + 放大 1.2 绕格心」"
                          + "的几何后果；**顶出视口那一版是 80.46**，会被裁到 159.33 ⇒ 与原版不同）");
            else
                CheckTrue(false, "（A240）第 6 格的 `BG` 量得到渲染矩形（下面的放大几何才验得成）");
        }
        // 两态**互斥**：`HasFailed` 决定谁开。我们的数据默认 `false` ⇒ 连胜态开、断签态关
        var succ = FindChild(ds.transform, "Streak Successful");
        var fail = FindChild(ds.transform, "Streak Failed");
        CheckTrue(succ != null && succ.gameObject.activeSelf, "连胜态开着（`Streak Successful`）");
        CheckTrue(fail != null && !fail.gameObject.activeSelf, "断签态关着（`Streak Failed`）");
        CheckTrue(FindChild(succ, "Timer") != null,
                  "`Timer` 在**连胜面板里**（原版实况 ⇒ 断签态下看不到倒计时）");

        // ============================================================ ★ **A524（A493 #1–#8）**
        // 8 颗字的【真渲染边】= 原版框边（**逐颗实读**，⛔ 不是抄 `H37` §四·1 那张表的「现状」列）。
        //   判据（本件亲跑，不是抄表）：
        //     python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup"  --depth 10 --no-sprite
        //     python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12 --no-sprite
        //   ⛔ 期望值写【原版字面量】（含 ⚠️ 那两条特殊的），不许写被测实现里的 `S_*` / `H_*` / `TimerText` / `D_ClaimedTex`。
        //   🔴 量法 = 上面那个 `edgePx`（读 TMP 自己的 `textBounds`），⛔ **不许用 `Label.WorldW`**（同 A490 那个病灶）。
        //   **改坏法**：删掉 `Shell/` 里对应那一句 `MenuDraw.AlignLeft/AlignRight` ⇒ 文字块回到框心
        //     ⇒ 左缘右移 `(框宽 − 文字宽)/2` ⇒ 红（#2/#6 两颗的框宽 ≈ 文字宽 ⇒ 那两条**可能同形**，见本批报告 §四 牙口表）。
        //   🔴 **每条都带「牙口自报」**（`teeth(...)`，只拼日志文案）：把那颗的**框宽 / 实测量到的渲染宽 /
        //     删掉 `Align*` 后的位移**打进日志 ⇒ 同步点那次 run 的输出直接告诉我们**哪几条两态同形（位移 ≤ 1.5px）**，
        //     ⛔ 别把「没实测」写成「验过了」（本批跑不了 Unity）。
        Section("★ A524：A493 #1–#6 的真渲染边 = 原版框边（逐颗实读）");
        {
            // #1 `Current Streak`：原版左沿 43.0（`Streak Successful/Current Streak`，Left/Capline）
            var a1 = FindChild(succ, "Current Streak");
            CheckTrue(a1 != null && a1.GetComponentInChildren<TMPro.TextMeshPro>() != null,
                      "（前提）`Current Streak` 量得到 TMP（取不到 ⇒ 下面那条读回 NaN ⇒ 一样红）");
            CheckNear(edgePx(a1, true), 43.0f, 1.5f,
                      "★ A493#1：`Current Streak` 的真渲染左缘 = 框左沿 **43.0**（原版 `对齐=Left/Capline`）"
                    + "（⛔ 不显式对齐 ⇒ 停在框心 288.46 − 文字宽/2 ⇒ 红）" + teeth(a1, 43.0f, 533.91f));
            // #2 `Current Streak Value`：原版左沿 545.91（框宽仅 37.85 ⇒ ⚠️ 牙口另说，看下面自报那三个数）
            CheckNear(edgePx(FindChild(succ, "Current Streak Value"), true), 545.91f, 1.5f,
                      "★ A493#2：`Current Streak Value` 的真渲染左缘 = 框左沿 **545.91**（原版 `Left/Capline`）"
                    + teeth(FindChild(succ, "Current Streak Value"), 545.91f, 583.76f));
            // #3 `Info`（连胜面板那颗；⚠️ 断签面板那颗是 `Center/Midline`，别取错树）
            CheckNear(edgePx(FindChild(succ, "Info"), true), 47.47f, 1.5f,
                      "★ A493#3：`Streak Successful/Info` 的真渲染左缘 = **47.47**（原版 `Left/Midline`）"
                    + teeth(FindChild(succ, "Info"), 47.47f, 1872.53f));
            // #4 `Next Rewards text`：**右**缘 = 935.0（**全窗唯一一颗右**）
            CheckNear(edgePx(FindPath(succ, "Timer/Next Rewards text"), false), 935.0f, 1.5f,
                      "★ A493#4：`Next Rewards text` 的真渲染**右**缘 = 框右沿 **935.0**（原版 `Right/Midline`）"
                    + "（写成 `AlignLeft` ⇒ 左缘对、右缘差一整个文字宽 ⇒ 红）"
                    + teeth(FindPath(succ, "Timer/Next Rewards text"), 573.31f, 935.0f));
            // #5 `Timer Text`：左缘 = 985.0（⚠️ 与 #4 同一个 `Timer` 底下、方向相反）
            CheckNear(edgePx(FindPath(succ, "Timer/Timer Text"), true), 985.0f, 1.5f,
                      "★ A493#5：`Timer Text` 的真渲染左缘 = **985.0**（原版 `Left/Midline`）"
                    + teeth(FindPath(succ, "Timer/Timer Text"), 985.0f, 1346.69f));
            // #6 `Window Title`：左缘 = 155.0。
            // 🔴 **本件对规格做的一处适配（如实记）**：`A524` 的成品块写的是
            //   `FindPath(ds.transform, "Header With Back Button/Window Title")` —— 那**已经过期**：
            //   A519（同一批）把 `Window Title` 改挂到 `Header Background` **底下**了（原版父链）
            //   ⇒ 那条 FindPath 只会返回 null ⇒ 读回 NaN ⇒ **假红**。
            //   按调度台口径（`WD3` §六）按名字取一律走**递归**的 `FindChild`；
            //   父链本身由下面 A519 那两条钉着（那才是它该断的地方）。
            //   ⚠️ 与 A475 那颗（`Shell/LiveOpsEventWindow.cs`）**同值不同颗**（本窗自建顶栏）。
            CheckNear(edgePx(FindChild(ds.transform, "Window Title"), true), 155.0f, 1.5f,
                      "★ A493#6：本窗（自建顶栏）`Window Title` 的真渲染左缘 = **155.0**（原版 `Left/Capline`）"
                    + "—— 与 A475 那颗（`LiveOpsEventWindow`）**同值不同颗**"
                    + teeth(FindChild(ds.transform, "Window Title"), 155.0f, 534.30f));
        }

        // ============================================================ ★ **A640（A517–A520）**
        Section("★ A640（§五 段）：A517 字距 5 / A518 `Fill Line` 几何 / A519 顶栏两颗底图 / A520 父子");
        {
            // ★ A517：`字距=5` 三颗 —— 原版 `m_characterSpacing = 5.0`（组件序列化字面量）。
            //   判据 = 同一次 dump 的 `字距=` 列（`Current Streak` / `Current Streak Value` / `Window Title` 三颗有、其余没有）
            //   + **全包普查**（W-D3 亲跑）：4894 颗 TMP 里只有 12 颗带 5、其中 `fs 67.55` 那一族 **8/8 全带**、
            //     同字体资产的 540 颗里 537 颗是 0 ⇒ **逐颗手填**（不是预设/字体表）⇒ 我们也逐颗补，⛔ 别给整窗加。
            //   两条**一起**断（单断一条分不出两种状态）：
            //     ① `CharSpacing == 5` —— 抓「有没有补」；
            //     ② 真渲染**左缘 == 框左沿** —— 抓「次序对不对」（`SetCharSpacing` 排在 `AlignLeft` 之后 ⇒ 按旧宽定位 ⇒ 偏 Δ宽/2）。
            //   改坏法：删掉任一句 `SetCharSpacing(5f)` ⇒ ① 红；把它挪到 `MenuDraw.AlignLeft` **之后** ⇒ ② 红。
            var csNodes = new (Transform node, float leftPx, string what)[]
            {
                (FindChild(succ, "Current Streak"),       43.00f, "`Current Streak`（fs 70）"),
                (FindChild(succ, "Current Streak Value"), 545.91f, "`Current Streak Value`（fs 80）"),
                (FindChild(ds.transform, "Window Title"), 155.00f, "`Window Title`（原版 `m_fontSize` 67.55；"
                                                                    + "⚠️ 那一颗**开自适应** ⇒ 实际渲染字号更小，见下面 A968 那两条）"),
            };
            for (int i = 0; i < csNodes.Length; i++)
            {
                var lb = csNodes[i].node != null ? csNodes[i].node.GetComponentInChildren<Label>() : null;
                CheckTrue(lb != null && Mathf.Abs(lb.CharSpacing - 5f) <= 0.01f,
                          $"★ A517：{csNodes[i].what} 的字距 = 原版 **5**（实得 {(lb != null ? lb.CharSpacing : float.NaN)}）");
                // 🔴 **牙口自报**（⛔ 不是判据，只进日志）：`SetCharSpacing` 每字加宽 ≈ `0.05 × fontPx`
                //   —— 判据 = TMP 源码 `TextMeshPro.cs:2235` `currentEmScale = m_fontSize * 0.01 * 0.1`
                //   （非正交 TMP ⇒ `orthographicMultiplier = 0.1`）× `:3853` 的
                //   `m_xAdvance += … + characterSpacingAdjustment * currentEmScale`；换算到本工程的 px 口径
                //   （1 fontSize = 10.238px）⇒ 每字 = `5 × 0.001 × fontSize` = `0.05 × fontPx`。
                //   ⇒ 次序反了的位移 ≈ 它的一半 × (字数−1)。**字数 1 的那颗（`Current Streak Value`）加宽恒 0**
                //   ⇒ 那条 ② 对「次序」这个改坏法**无牙口**（本批没能跑 Unity ⇒ 只能由同步点那次 run 的自报读数坐实）。
                int nch = lb != null && !string.IsNullOrEmpty(lb.Text) ? lb.Text.Length : 0;
                float dW = 0.05f * (lb != null ? lb.FontPxNow : 0f) * Mathf.Max(0, nch - 1);
                // 🔴 **2026-10-14（#53/#54 · D1013 §三·33/34）就地订正（铁律 5）**：这里原来走的是
                //   **`TmpEdgePx(…, false)`**（= TMP **mesh 顶点**的最小 x = 字形**墨迹**起点）配一个
                //   从 `edgePx`（= `textBounds` = **排版框**）量出来的期望值 ⇒ **两把尺子差一个「首字左边距」**
                //   （实得 `Current Streak` **45.22** vs 期望 43.00 · `Window Title` **156.92** vs 155.00；
                //   而同 run 里 A493 用 `edgePx` 量同两颗**逐值全绿**＝`43.0` / `155.0`
                //   ⇒ 实现没错、只是尺子换了。字越少墨迹越贴框沿 —— `Current Streak Value`（1 字）
                //   两条只差 1.35px ⇒ 它一直绿，正是这个形状）。
                //   ⇒ 换成与 A493 那批**同一把尺子** `edgePx(node, true)`（左缘）；判别力**不变**：
                //     次序反了的位移 ≈ Δ宽/2（`Current Streak` 那颗 ≈ 24.5px）≫ 1.5 容差。
                //   ⚠️ 两个助手的布尔参**极性相反**（`TmpEdgePx(t, right)` / `edgePx(t, left)`）—— 别照抄错。
                CheckNear(edgePx(csNodes[i].node, true), csNodes[i].leftPx, 1.5f,
                          $"★ A517：{csNodes[i].what} 的真渲染**左缘** = 框左沿 **{csNodes[i].leftPx}**"
                            + "（钉「字距在前、对齐在后」—— 次序反了会偏 Δ宽/2）"
                            + $"（牙口自报：{nch} 字 @ {(lb != null ? lb.FontPxNow : 0f):F1}px ⇒ 字距加宽预判 ≈ {dW:F1}px）");
            }

            // ★ A968（2026-10-18）：本窗 `Window Title` 的**自适应窗口** —— 原版那一颗是**开自适应**的。
            //   判据（**亲读解包资源、逐字段**，⛔ 不是抄表）：`bundle_menus_assets_all` 里那颗 `Window Title`
            //     （父链现读 = `Window Title < Header Background < Header With Back Button < Daily Streak Popup 根`）
            //     的 TMP = `MonoBehaviour_3324232435684942507.json`（`m_text "Daily Streak"`）：
            //     **`m_enableAutoSizing = 1`** · **`m_fontSizeMin = 18.0`** · **`m_fontSizeMax = 67.55`** ·
            //     `m_fontSizeBase = 36.0` · `m_characterSpacing = 5.0`；同包**另 7 颗 `Window Title` 逐颗现读、逐值相同**
            //     （8/8）⇒ 可钉。
            //   🔴 量的东西 = `Label.FontSizeMin/Max`（= **TMP 里那两个真字段**，经工程唯一那份 `Label.FontSizeToPx`
            //     折成画布 px），⛔ **不是**我们传进 `SetAutoFitBox` 的实参（那是拿实现证明实现）。
            //   ⛔ **不断 `FontPxNow`** —— 开了自适应之后它是 TMP 二分出来的**收敛值**（文案装不下就比上界小），
            //     拿它比字面量会时红时绿；只有**窗口**是定死的（同 §A143 那条口径）。
            //   改坏法：把 `Shell/DailyStreakPopup.cs` 的 `Spec.TitleMode` 改回 `SpacingOnly`
            //     （= 共件那一支**只调 `SetCharSpacing`、不调 `SetAutoFitBox`**）⇒ 这两个字段停在 **TMP 的出厂值**
            //     ⇒ 红（A968 落地前就是这个状态）。
            CheckFontWindow(ds.transform, "Window Title", 18f, 67.55f,
                            "★ A968：`Window Title` 的自适应窗口 = 原版 `m_fontSizeMin/Max` **18 / 67.55**"
                          + "（`m_enableAutoSizing = 1`；同包 8 颗 `Window Title` 逐值相同）");
            // 自适应**起点** `m_fontSizeBase` = 36.0。
            // 🔴 **如实标注：这一条单独【没有鉴别力】** —— `36.0` 正是 TMP 的序列化默认值（`TMP_Text.cs:473`
            //    `m_fontSizeBase = 36`），**没接自适应时读出来也是 36** ⇒ 它只用来钉「共件把原版那个 base 传对了」，
            //    真正的判别式是上面那条**窗口**。
            var a968t = FindChild(ds.transform, "Window Title");
            var a968lb = a968t != null ? a968t.GetComponentInChildren<Label>() : null;
            CheckTrue(a968lb == null || a968lb.FontSizeBase > 0f,
                      "（前提 · A968）走的是 TMP 后端 —— `Label.FontSizeBase` 在**点阵后端**返回 **−1**（如实报、不是 0）");
            CheckNear(a968lb != null ? Label.FontSizeToPx(a968lb.FontSizeBase) : -1f, 36f, 0.6f,
                      "★ A968：`Window Title` 的自适应**起点** = 原版 `m_fontSizeBase` **36.0**"
                    + "（⚠️ 单独没有鉴别力：TMP 的序列化默认值也是 36 —— 见上面那条注释）");

            // ★ A518：`Fill Line` 的**视觉框**（原版 `m_LocalScale = 1.2 × 绕 pivot (0,0.5)`）与 `Sliced`。
            //   判据 = prefab `RectTransform_3187973920738910891.json` 亲读（`m_Pivot=(0,0.5)` · `m_AnchorMin=(0,0.5)` ·
            //     `m_AnchoredPosition=(134.42572,0)` · `m_SizeDelta=(1487.97852,66.37200)` · `m_LocalScale=1.2000001668930054`）
            //     + `menu_dump.py … "Daily Streak Popup" --depth 10` 的行末 `视觉框=×1.2 → 视觉 1785.57×79.65`。
            //   🔴 四条各自钉一件：左沿钉「绕哪一点」（绕中心 ⇒ −14.37）、右沿/高钉「1.2 有没有接」。
            //   改坏法：① 不接 1.2（画布局框）⇒ 右沿 1622.40、高 66.37 ⇒ 红；② 改成绕**中心**缩 ⇒ 左沿 −14.37 ⇒ 红。
            float fx1, fy1, fx2, fy2;
            bool okF = RectOfUnion(FindChild(succ, "Fill Line"), out fx1, out fy1, out fx2, out fy2);
            CheckTrue(okF, "★ A518：`Fill Line` 量得到渲染矩形（九宫 ⇒ 走**并集**，⛔ 别用 `Wpx`/`CheckRectPx`，那会只量到第一块子块）");
            if (okF)
            {
                CheckNear(fx1, 134.43f, 0.5f,
                          "★ A518：`Fill Line` 渲染**左沿 = 134.43**（原版 `m_Pivot=(0,0.5)` ⇒ 绕**左中**放大、左沿不动；"
                        + "改成绕中心缩 ⇒ **−14.37** ⇒ 红）");
                CheckNear(fx2, 1919.99f, 0.5f,
                          "★ A518：渲染**右沿 = 1920.0**（= 134.4257 + 1487.9785×1.2 ⇒ 正好铺满；**没接 `scl=1.2` ⇒ 1622.40** ⇒ 红）");
                CheckNear(fy1, 519.06f, 0.5f, "★ A518：渲染**上沿 = 519.06**（= 558.885 − 66.372×0.6）");
                CheckNear(fy2 - fy1, 79.64f, 0.5f, "★ A518：渲出来的**高 = 79.64**（= 66.372 × 1.2）");
            }

            // ★ A519：顶栏底下原版**两颗底图**，我们原来只建一颗（少了 `Header Background`），且 `Window Title` 挂在它底下。
            //   判据 = `menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10` 的树（深 1/2/3 + 矩形）。
            //   ⚠️ 矩形要取 dump 那一份（**595.30**）—— `menu_rect.py --relative` 给 `0.00→0.00` 是**布局跑之前**的模板位
            //      （这一颗挂 `CSFMinMax + HLG`，宽 = 155 + 379.30 + 61 = 595.30）。
            //   改坏法：删掉 `Shell/DailyStreakPopup.cs` 的 `BuildHeader` 里那句 `MenuDraw.Node(h, "Header Background", H_Plate)`
            //           ⇒ ①②③ 红（节点不在 ⇒ 后面那四条**不报**，它们是 `if (okP)` 里的）；把 `Window Title` 改回挂 `h` ⇒ 只第 ② 条红。
            var plate = FindPath(ds.transform, "Header With Back Button/Header Background");
            CheckTrue(plate != null,
                      "★ A519：顶栏底下有 `Header Background`（原版两颗底图里的**第一颗**；我们原来只有 `Header Background (1)`）");
            CheckTrue(plate != null && FindChild(plate, "Window Title") != null,
                      "★ A519：`Window Title` 挂在 **`Header Background`** 之下（原版父链；挂在顶栏根 ⇒ 红）");
            float px1, py1, px2, py2;
            bool okP = RectOfUnion(plate, out px1, out py1, out px2, out py2);
            CheckTrue(okP, "★ A519：那颗底图量得到渲染矩形（九宫 ⇒ 走**并集**）");
            if (okP)
            {
                CheckNear(px1, 0f, 1f, "★ A519：`Header Background` 渲染**左沿 = 0**（原版 `m_AnchorMin=(0,0.5)` · dump 0.00）");
                CheckNear(px2, 595.30f, 1f, "★ A519：渲染**右沿 = 595.30**（= CSFMinMax 撑出来的 155+379.30+61；⛔ 别用 0）");
                CheckNear(py1, 21.65f, 1f, "★ A519：上沿 = 21.65（与 `Header Background (1)` **逐值相同**）");
                CheckNear(py2, 137.01f, 1f, "★ A519：下沿 = 137.01");
            }

            // ★ A520：`Current Streak Value` 是 `Current Streak` 的**直接子件**（原版深 2 / 深 3）。
            //   改坏法：把它建回 `Streak Successful` 的兄弟（`MenuDraw.Text(p, …)`）⇒ 两条一起红。
            //   ⛔ 按调度台口径**不写** `FindPath(succ, "Current Streak/Current Streak Value")` —— 用**递归**的 `FindChild`。
            var cl = FindChild(succ, "Current Streak");
            var cv = FindChild(succ, "Current Streak Value");
            CheckTrue(cl != null && cv != null && cv.parent == cl,
                      "★ A520：`Current Streak Value` 是 `Current Streak` 的**直接子件**（原版父链；兄弟那一版 ⇒ 红）");
            CheckTrue(cv != null && cl != null && cv.IsChildOf(cl),
                      "★ A520：…而且**还在这棵子树里**（防止「挂到别处但恰好也叫这个名」）");
        }

        // 🔴 `scaleMultiplierFirstElement = 1.2`：唯一读取点 = `RefreshRewards` 的第一次循环
        //    （`i == challenge.collectedRewards`）⇒ **第一个「还没领」的奖格**放大 1.2（该 prefab 的 pivot 实测 (.5,.5)）
        //    · 🔴 **1.2 的原版出处**（⛔ 不是我们挑的、也不是我们的常量）= prefab MB `8654310213240890027` 的
        //      **序列化字段** `scaleMultiplierFirstElement = 1.2000000476837158`
        //      （`assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_8654310213240890027.json` 亲读；
        //       同一个值 → `资料/日常_原版规格.md:435`）。
        //      该类**全类没有赋值点** ⇒ 运行时用的就是这一个序列化值（`资料/日常_调用链_三窗.md:199,207`：
        //      字段偏移 `0xA8`、**只读**，把首格的 `localScale` 三分量**各乘**它一次 ⇒ 绕 pivot `.5,.5`）。
        //      ⚠️ **2026-10-17 更正（铁律 5 · A262-M-b #31）**：原来写 `资料/日常_调用链_DailyStreak.md:55,63`，
        //      实际是 —— 该文件 **2026-10-10 已合并**进 `资料/日常_调用链_三窗.md` 的 **§二**
        //      （旧行号统一 **+144**：55→**199** · 63→**207**），**旧件已不在盘上**。
        //      错因：合并那天本文件没有改 `.cs` 的权限（合并件自己头部 `:13` 就记着这条）
        //      ⇒ 指针一直留在**已被合并的旧件**上。两条新行已现读亲核（`0xA8` 只读 · `RefreshRewards:96` · 三分量各乘一次）。
        //    🔴 **2026-10-08（A182 收尾）：本条改口径 —— 原来比「渲出来的宽」，现在比「渲出来的左沿」。**
        //       · **为什么不能比宽**：A182 把 `Viewport` 的**硬裁**接上之后，这两格的 `BG` **横竖都被裁**
        //         ⇒ 渲出来的宽变成 270.43 / 8.54 = **31.7**（A182 之前两格都是 420.83 宽，比出来正好 1.2）。
        //         两条实测数 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §0·2。
        //       · 🔴 **「渲出来的高」那一条 2026-10-11（A240）已经【改回 = 1.2】**（就是下面第三条）。
        //         2026-10-08 写上面那段时它**确实不成立** —— 当时的条目**顶对齐在 132.09**
        //         （`DailyStreakPopup.cs` 的已知偏差，波 C1 报告 §四·4）⇒ 放大那一格**顶出视口**
        //         （`BG` 顶沿 80.46 < 视口顶 159.33）被硬裁 ⇒ 高之比只有 **1.098**。
        //         **A240 把摆位改成原版的竖向居中之后**（上沿 308.99 · 中心 567.14）两格的 `BG` 都**整块**
        //         落在视口里 ⇒ 高之比**严格 = 1.2**：未放大 = `480.15 − 26.95` = **453.20**、
        //         放大 1.2 ⇒ **543.84**。⛔ **别把这一条当「恒真」** —— 摆位一退回顶对齐它就红。
        //       · ✅ **现在量什么**：`BG` 的**渲染左沿** —— 它是这两格唯一**两次裁切都碰不到**的边：
        //         `BG` 那条路 `keepAspect = false` ⇒ 矩形逐字段等于原版 `E_Bg`（`14.56,26.95 → 365.25,480.15`）；
        //         左沿在视口内（> 0），也不吃软边那条带（`1831..1920` 只改四角 alpha、不改几何）。
        //         **期望值 = 原版常量算出来的字面量**（⛔ 不调 `DailyStreakPopup.*`、不读被测实现的实参）：
        //           · 第 6 格（下标 `StreakCollected` = 5）：未缩放左沿 = `pad.L 2 + 5 × 315.816 + E_Bg.x1 14.56`
        //             = **1595.64**；格心 = `2 + 5 × 315.816 + 379.816 / 2` = **1770.988**
        //             ⇒ 绕格心放大 1.2 = `1770.988 − (1770.988 − 1595.64) × 1.2` = **1560.57**（向左挤出 **35.07**）；
        //           · 第 7 格（邻格，不放大）= `2 + 6 × 315.816 + 14.56` = **1911.46**。
        //         ⚠️ 第 7 格那一条**也在钉「1.2 只作用在那一格」**（⛔ 别只留一条：只比第 6 格的话，
        //            「所有格子一起放大」这种实现会假绿）。
        //    🔴 **改坏法**（三条都真红）：① 删掉 `BuildTrack` 里 `if (i == first) r = ScaleAbout(r, 1.2f);`
        //       ⇒ 第 6 格的左沿回到 **1595.64**（差 35.07）；② 把 `i == first` 改成对所有 `i` 成立
        //       ⇒ 第 7 格变 **1876.39**（差 35.07）；③ 把 `ScaleAbout` 改成绕**左上角**放大（pivot 错的实现）
        //       ⇒ 第 6 格变 **1598.55**（差 37.98）。
        //    ⚠️ 这两条依赖**开机滚动偏移 = 0**（原版 `FocusOnItem` 在本机数据下不触发；下面 A182 那一段
        //       另有断言钉着 `Offset = 0`）—— 偏移一非 0，这两个期望值要整体跟着平移。
        if (ds.entries.Count > DailyData.StreakCollected + 1)
        {
            float bx1, by1, bx2, by2, nx1, ny1, nx2, ny2;
            bool okB = RectOfUnion(FindChild(ds.entries[DailyData.StreakCollected], "BG"),
                                   out bx1, out by1, out bx2, out by2);
            bool okN = RectOfUnion(FindChild(ds.entries[DailyData.StreakCollected + 1], "BG"),
                                   out nx1, out ny1, out nx2, out ny2);
            // 两格都压在视口右沿 ⇒ 用**并集**量（软边会把 `BG` 沿 1831 切成两块）
            CheckTrue(okB && okN, "两格的 `BG` 都量得到渲染矩形（它们都压在视口右沿 ⇒ 量到的是被裁过的那一块）");
            // 量不到就只报上面那一条（免得同一个问题报两次）
            if (okB)
                CheckNear(bx1, 1770.988f - (1770.988f - 1595.64f) * 1.2f, 0.5f,
                          "「第一个还没领的」奖格的 `BG` **渲染左沿** = 1560.57（原版 `scaleMultiplierFirstElement` "
                          + "= 1.2 **绕格心**放大的几何后果：未缩放左沿该在 1595.64 ⇒ 向左挤出 **35.07**）");
            if (okN)
                CheckNear(nx1, 2f + 6f * 315.816f + 14.56f, 0.5f,
                          "邻格（第 7 格）的 `BG` 渲染左沿 = 1911.46 —— **没有被放大**"
                          + "（钉着「1.2 只作用在第一个还没领的那一格」，⛔ 不是「所有格子一起放大」）");
            // 🆕 **2026-10-11（A240）**：**渲出来的高之比 = 1.2** —— 2026-10-08 那版做不到的那一条，
            //   等 `Shell/DailyStreakPopup.cs` 的条目摆位改成原版的竖向居中之后就成立了（见上面那段）。
            //   两条数（原版几何字面量）：未放大 `BG` = `480.15 − 26.95` = **453.20**；绕格心放大 1.2 ⇒ **543.84**。
            //   **改坏法**：把摆位退回顶对齐（`S_Content.y1`）⇒ 两格的 `BG` 都被视口顶裁一截（497.31 / 452.91）
            //   ⇒ 比值 **1.098** ⇒ 红。
            if (okB && okN)
                CheckNear((by2 - by1) / (ny2 - ny1), 1.2f, 0.01f,
                          "★ 「第一个还没领的」奖格的 `BG` **渲出来的高 = 邻格的 1.2 倍**"
                          + "（原版 `scaleMultiplierFirstElement`；A240 修好摆位之后这一条才成立）");
        }
        Check(ds.MissingArt.Count, 0, "连登窗没有取不到的图");
        CheckHoverSwap(ds.transform, "连登窗");
        Shoot("04_每日连登.png");
        // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
        //   期望矩形 = **原版 prefab** `Daily Streak Popup > bg` 那颗 `Image` 的 rect
        //   （0,152.84 → 1920,964.94）；⛔ 不写 `DailyStreakPopup.Bg`（那是被测实现**传进去的实参**）。
        CheckAbsorbRule("每日连登窗", ds.transform, "AbsorbHit",
                        0f, 152.84f, 1920f, 964.94f,
                        DailyStreakPopup.QShade, DailyStreakPopup.QContent, () => ds.CurrentState);

        // ============================================================ 🆕 **2026-10-08（A182）**
        // 奖格轨的**视口 / 裁切 / 软边 / 滚动**四件 —— **本件之前一层都没有**
        // （`Rewards Scroll View` 底下直接挂 `Rewards Content`，全文件 0 处 `Clip` ⇒ 第 6、7 格照画到屏幕外）。
        // 判据全是**直读原版**，⛔ 期望值一律写原版字面量（不写 `DailyStreakPopup.TrackSoft` 那类实参 = 同式自证）：
        //   · 结构（三层同矩形）→ `工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 5`
        //     （`Rewards Scroll View` 的 `ScrollRect` = `h=0 v=0 mode=1` 且 **`m_Enabled=0`**；
        //      `Viewport`(RectMask2D) 与 `Rewards Scroll View` 同矩形 0,159.33 → 1920,964.94）；
        //   · 掩码字段 → `d:/4/_tmp_view/q1_rm2d.txt:87-88`（`soft=(89,0) pad=(0.0,0.0,0.0,0.0) en=1`）；
        //   · 内容宽 → 该 prefab 的 `Rewards Content` 是 `HorizontalLayoutGroup`（spacing **−64** ·
        //     `pad=2,0,58,0`）+ `ContentSizeFitter` ⇒ 跑完 = 2 + 7×379.816 + 6×(−64) = **2276.71**。
        {
            var trackVp = FindPath(ds.transform, "Streak Successful/Rewards Scroll View/Viewport");
            CheckTrue(trackVp != null, "★ 奖格轨有 `Viewport` 那一层（原版三层的中间层 —— 掩码就长在它身上）");
            // ⚠️ `Viewport` 是**纯容器节点**（我们这套里它不带渲染 ⇒ 没有 `ImageQuad`）
            //    ⇒ 只能量**位置**（`CheckAt`），⛔ 别用 `CheckRectPx`（那个要 `ImageQuad`，会假红）。
            CheckAt(trackVp, 0f, 1920f, 159.33f, 964.94f,
                    "`Viewport` 摆在原版那个矩形里（与 `Rewards Scroll View` **同矩形** 0,159.33 → 1920,964.94）");
            CheckTrue(FindPath(ds.transform, "Streak Successful/Rewards Scroll View/Viewport/Rewards Content") != null,
                      "`Rewards Content` 挂在 **`Viewport` 之下**（原版父链 = …/Rewards Scroll View/Viewport/Rewards Content）");

            // ---- 滚动范围（原版 `ContentSizeFitter` 跑完的内容宽 2276.71 − 视口 1920 = 356.71）----
            CheckTrue(ds.TrackScroll != null, "滚动区建出来了（`MenuScroll`）");
            CheckNear(ds.TrackScroll != null ? ds.TrackScroll.MaxOffset : -9999f, 356.71f, 0.5f,
                      "可滚右极值 = **356.71px**（原版内容宽 2276.71 − 视口 1920）");
            CheckNear(ds.TrackScroll != null ? ds.TrackScroll.Offset : -9999f, 0f, 0.01f,
                      "开机偏移 = **0**（原版 `DailyRewardSelector` 那句 `if (5 < index)` 在 `index = 5` 时为假 ⇒ 不移动）");

            // ---- ★ 真裁住了（一）：整块在视口外的**子件不建** ----
            var eLast = ds.entries.Count > 6 ? ds.entries[6] : null;
            CheckTrue(eLast != null, "第 7 格（最后一个）的节点在");
            // 角标原版局部 x 242.41..342.41，第 7 格 x1 = 2 + 6×315.816 = 1896.90 ⇒ 屏幕 2139.3..2239.3（> 1920）
            // 🔴 **改坏法**：拿掉 `BuildTrack` 里那三件套（`Clip`/`ClipSoftness`/`ClipPad`）⇒ 它会被建出来
            //    并画在屏幕外（x 2139..2239），这一条当场红。
            CheckTrue(FindChild(eLast, "Extra Reward Indicator") == null,
                      "★ 第 7 格的 `Extra Reward Indicator`（x 2139..2239，**整块在视口外**）**没建**");

            // ---- ★ 真裁住了（二）：压在右沿的那一块被截到视口内 ----
            float lx1, ly1, lx2, ly2;
            CheckTrue(RectOfUnion(FindChild(eLast, "BG"), out lx1, out ly1, out lx2, out ly2),
                      "第 7 格的 `BG` 量得到渲染矩形（它压在右沿 ⇒ 画出的是被裁过的那一块）");
            CheckTrue(lx2 <= 1920.5f,
                      $"★ 第 7 格 `BG` 的**渲染并集右边缘 ≤ 1920**（实测 {lx2:F1}）"
                      + " —— 未裁时它会一直画到 2262.15（= 屏幕外 342px）");

            // ---- ★ 软边 (89,0)：竖切线只在带的内沿 89 / 1831；一条横切线都没有 ----
            var SuVp_ = new PxRect(0f, 159.33f, 1920f, 964.94f);                  // `Streak Successful/Rewards Scroll View/Viewport`（同上一条 `CheckAt`）
            var vCuts = ScanSoftCuts(succ, true, SuVp_);
            CheckTrue(vCuts.Count > 0,
                      "★ 有竖向渐隐切线（= 软边真接上了；**一条都没有 ⇒ 软边没接**，`ClipSoftness` 留在 0）");
            for (int i = 0; i < vCuts.Count; i++)
                CheckTrue(Mathf.Abs(vCuts[i] - 89f) <= 0.6f || Mathf.Abs(vCuts[i] - 1831f) <= 0.6f,
                          $"竖切线 #{i + 1} 在 {vCuts[i]:F2} ⇒ 必须落在带的内沿：左 `0 + 89 = 89` 或右 `1920 − 89 = 1831`"
                          + "（⚠️ 写成 `(0,89)` 会去切**横线**、写成 200 会切在 200/1720）");
            Check(ScanSoftCuts(succ, false, SuVp_).Count, 0,
                  "**一条横切线都没有** —— 这一处软边是 `(89,0)`：**y 方向是硬边**"
                  + "（`(0,89)` 那种写反的实现这里会冒出一堆横切线）");
        }

        // ============================================================ 🆕 **2026-10-12（A316）**
        // A 表那一条：「`CollectStreak` 这条路**零断言覆盖**（`Editor/` 里 `grep CollectStreak` = 0 命中）
        // + 守卫口径与刚修好的 `CollectReward` **不一致**（那一条守卫问状态、这一条只判下标）」。
        // 🔴 **守卫那一半已在 `Shell/DailyData.cs` 就地统一**：`StreakRewardUnlocked` 现在**读 `_streakClaimed`**
        //    （= 与 `RewardStateOf` / `CollectReward` 同口径：「可领」蕴涵「还没领过」）。
        //    本节钉的是**行为**：① 真路径领到 ⇒ 弹窗；② **同一格第二次领不到**（改回去之前它**发两份**）；
        //    ③ 领过之后那一格的 `Collect` **不画了**（与 ① 合起来才是「按状态」而不是「恒画」）。
        // 🔴 **期望值一律用字面量 / 从 `ds.entries` 量出来的真值**，⛔ 不从 `DailyData` 自己的表里取
        //    （那是同义反复：改坏实现它会跟着一起变绿 —— 本仓「断言自证」那条坑）。
        // **改坏法**（三条，各红在不同的一组上）：
        //   ① `StreakRewardUnlocked` 退回 `SI(i) == StreakCollected`（不读 `_streakClaimed`）
        //      ⇒ 「第二次领不到」与「一份都没再发」那两条红；
        //   ② 删掉 `CollectStreak` 末尾那句 `ShowCollectedWindow` ⇒ 「弹出 `Reward Window`」那条红；
        //   ③ `DailyStreakPopup.BuildEntry` 里那句 `if (unlocked && !claimed)` 改成恒真
        //      ⇒ 「领过之后不画 `Collect`」那条红（还原后那一条则相反 —— 改成恒假就红）。
        Section("🆕 A316 连登奖格：真路径领到 ⇒ 弹窗 + **同一格领不到第二次**（守卫问状态）");
        {
            int dClaim = DailyData.StreakClaimableDay;                 // = `StreakCollected`（只有这一格可领）
            // 🔴 **2026-10-13（H45 · 铁律 5·b 就地订正）**：**顺序反了** —— 原来先读「起点快照」再显式置位，
            //    而那一刻读到的**不是出厂值、是上一段的残留**：上面 §五 的
            //    `CheckAbsorbRule("每日连登窗", …)` 收尾那一步**真点了一次压暗层**（本文件 `:303` 的 `pl.ClickAt(5f, 5f)`），
            //    而连登窗那颗压暗层的动作 = `StreakAutoCollect(); Close();`
            //    （`Shell/DailyStreakPopup.cs` 里压暗层那颗命中区的动作），A400 之后 `StreakAutoCollect` 是**真收**
            //    （`Shell/DailyData.cs:909` → `:852` 置 `_streakClaimed`）⇒ 快照读到 **`true`**。
            //    ⇒ 「起点」的定义改成「**本节自己显式置位之后**的读数」；`:5974` / `:6055` 两处还原**一个字都不用改**
            //    （它们仍取 `claimed0`，从此 = `false`）。
            //    ⛔ 别改成「把 §五 那条 `CheckAbsorbRule` 换掉 / 跳过」——那等于把「点窗外 ⇒ 关窗 **+ 关窗自动收**」
            //    这条**真行为**的现场从自检里抹掉（A81 + A400 刚接上的东西）。
            DailyData.ForceStreakClaimedForTest(dClaim, false);        // 显式从「没领过」起（⛔ 别吃上一段残留）
            bool claimed0 = DailyData.StreakRewardClaimed(dClaim);     // 起点快照（本节收工要还原 ⇒ 必须取这之后的值）
            Check(claimed0, false,
                  "（前提）起点那一格**已被本节显式置成「没领过」**"
                  + "（§五 的「关窗自动收」在它之前已经**真收过**这一格 —— 见本行上面那段订正注释；"
                  + "这一条不成立 ⇒ 下面「又画出来了」那条等于没查）");
            wm2.OpenWindow(ds);                                        // 重开（上面 `CheckAbsorbRule` 把它点关了）⇒ 内容重建

            var eClaim = ds.entries.Count > dClaim ? ds.entries[dClaim] : null;
            CheckTrue(eClaim != null, "（前提）连登窗第 " + (dClaim + 1) + " 格建出来了（可领的那一格）");
            var cNode = eClaim != null ? FindChild(eClaim, "Collect") : null;
            var cBtn = cNode != null ? cNode.GetComponent<WindowButton>() : null;
            CheckTrue(cBtn != null && cBtn.onClick != null,
                      "（前提）可领那一格上挂着 `Collect` + `WindowButton`（`BuildEntry` 的 `unlocked && !claimed` 为真）");
            string drawnArt = eClaim != null ? ArtOf(FindChild(eClaim, "Reward Holder")) : null;
            // ⚠️ **前提**：`drawnArt` 取不到的话，下面那条「窗里的 `Id` = 画的那张图」与「一份都没再发」
            // 都会**空转**（`null == null` / `0 == 0` 恒真）⇒ 先把这一条钉住（本仓「弱断言分不出两种状态」）。
            CheckTrue(drawnArt != null, "（前提）那一格的 `Reward Holder` 画得出来（取不到图名 ⇒ 下面两条会空转成恒真）");
            int wStreak0 = drawnArt != null ? Wallet.Of(drawnArt) : 0;

            var rwStreak = cBtn != null ? ClickCollectAndDismiss(cNode, cBtn, "连登奖格的 `Collect`") : null;
            CheckTrue(rwStreak != null,
                      "★★ A316：连登奖格**真领到** ⇒ 弹出那扇 `Reward Window`"
                      + "（删掉 `CollectStreak` 末尾那句 `ShowCollectedWindow` ⇒ 红；这条路原来一条断言都没有）");
            if (rwStreak != null && rwStreak.Context != null && rwStreak.Context.Rewards != null
                && rwStreak.Context.Rewards.Length == 1 && drawnArt != null)
            {
                var sr = rwStreak.Context.Rewards[0];
                Check(sr.Id, drawnArt,
                      "★ …窗里那一条的 `Id` = **格子里画出来的那张图**（`Reward Holder` 上 `ImageQuad.Texture` 的真值）");
                Check(Wallet.Of(drawnArt) - wStreak0, sr.Quantity,
                      "★ …而且它的 `Quantity` = 这次**真的记进 `Wallet` 的增量**（发出去的 = 窗里报的）");
            }

            // ---- ★★ **同一格、第二次**：领不到 ----
            // 🔴 **这就是「守卫问状态」那一条**：`_streakClaimed[i]` 已置真 ⇒
            //    `StreakRewardUnlocked` 必须转假。⛔ 别改成「靠 UI 画不画那颗钮」来证 ——
            //    守卫是**两处共用的同一份布尔**，它自己要对（本仓「两处写同一条规则 = 迟早不一致」）。
            int wStreak1 = drawnArt != null ? Wallet.Of(drawnArt) : 0;
            Check(DailyData.CollectStreak(dClaim), false,
                  "★★ A316：**同一格再领一次 ⇒ `false`（领不到）** —— 守卫原来只判下标、不读 `_streakClaimed`"
                  + "⇒ 这一下会得 `true` 并**再发一份**（改坏法见本节头注释 ①）");
            Check(drawnArt != null ? Wallet.Of(drawnArt) - wStreak1 : 0, 0,
                  "★ …而且**一份都没再发**（A316 之前这一下会再发一份，与已修好的 `CollectReward` 那种「无限重领」同型）");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                  "★ …而且**一扇窗都没弹**（`ShowCollectedWindow` 在成功分支**之内**：写在守卫之前 ⇒ 红）");

            // ---- ★ 领过之后那一格**不画 `Collect` 了**（与上面「未领时画了」合起来才分得出「按状态」）----
            ds.Build();                                                // = 领奖窗关掉之后 `RefreshOpenDailyWindows` 那一跳
            var eAfter = ds.entries.Count > dClaim ? ds.entries[dClaim] : null;
            CheckTrue(eAfter == null || FindChild(eAfter, "Collect") == null,
                      "★ …重建之后那一格的 `Collect` **不画了**（`unlocked && !claimed` —— 领过之后两半都假）");

            // ---- 还原到本节起点 ----
            DailyData.ForceStreakClaimedForTest(dClaim, claimed0);
            ds.Build();
            var eBack = ds.entries.Count > dClaim ? ds.entries[dClaim] : null;
            CheckTrue(eBack == null || FindChild(eBack, "Collect") != null,
                      "（还原）起点是「没领过」⇒ 那一格的 `Collect` **又画出来了**"
                      + "（三个态合起来才证得住它是**按状态**画的，不是恒画 / 恒不画）");

            // ============================================================ 🆕 **2026-10-13（A482 · 落 H29 §3·2）**
            // **A400：「关窗 ⇒ 真收到」**（`Shell/DailyData.cs` 的 `StreakAutoCollect`）—— 这条路原来是个**空壳**：
            // `if (…) return;` 之后**只写一句 `Say`**，既不置 `_streakClaimed`、也不发奖，而且**不出声**
            // ⇒ 「关窗自动收」这条**原版行为一次都没发生过**。
            // 🔴 **判据链（逐环实读，H29 §3·1 —— 没有一环是推的）**：
            //   原版 `DailyStreakWindow.Close()` → `MissionEvent<object,object>.TryCollect(Action)`
            //   （`VA 0x18102F400`；`decomp_full` 里没有这份 `.c`，是按 `dump.cs` 的 RVA 反汇编读出来的）
            //   = `CurrentChallenges.FirstOrDefault(x => x.canCollect)`：取到 ⇒ 尾调 `Collect(challenge, onComplete)`；
            //   取不到 ⇒ **直接调 `onComplete()`**。而**点 `Claim` 与关窗在原版是【同一个调用】**
            //   （`DailyStreakWindow__CollectRewardClicked.c` 末句**也是** `0x18102F400`）⇒ 两处收的是**同一份**。
            //   ⇒ 我们这一侧的落点 = **转调 `CollectStreak`**（同一条路 = 同一份守卫 / 同一份 `Wallet.Grant` /
            //   同一扇 `ShowCollectedWindow` / 同一句 `Say`）。
            // 🔴 **期望值全是原版行为 / 两个独立读数互证**（⛔ 不从 `DailyData` 自己的表里取 —— 那是同义反复）：
            //   ② 返回真（= 原版「取到了可领的那一条」那一支）；③ 那一格记成已领（`StreakRewardClaimed` 读口）；
            //   ④ 「真记进 `Wallet` 的增量」**= 那扇窗自己报的 `Quantity`**（两个独立读数互证，同 A316① 的手法）；
            //   ⑤ 弹出的那扇窗里那一条的 `Id` = **格子里画出来的那张图**（`ImageQuad.Texture` 的真值）。
            // **改坏法**（逐条）：① 删掉末尾那句 `CollectStreak`（退回空壳）⇒ ② 得假 ⇒ 红（这正是 A400 要拦的）；
            //   ② 只在外面 `Say` 一句、不置位 ⇒ ③ 红；③ 自己另写一份 `Wallet.Grant` 但数量抄错 ⇒ ④ 红；
            //   ④ 只置位不发奖（不调 `CollectStreak`）⇒ ⑤ 红（开窗那一步会缺）；开窗传错格 ⇒ ⑤ 也红；
            //   ⑤ 把 `if (!StreakRewardUnlocked(…))` 那半句删掉、改成**恒收** ⇒ ⑥ 三条一起红。
            // ⚠️ **起点**：A316 那一段的收尾已经把那一格还原成「没领过」并 `ds.Build()` 过（`claimed0` 就在手上）
            //   ⇒ 这里**再显式置一次**（⛔ 别吃静态残留）。
            //   🔴 **2026-10-13（H45）订正**：`claimed0` 的取法变了 —— 现在取的是 **A316 开头那次
            //   `ForceStreakClaimedForTest(dClaim, false)` 之后**的读数（原来取到的是 §五 那一下
            //   「关窗自动收」的残留 `true`）⇒ 它**恒为 `false`**，本段收尾（`:6085`）还原的仍是「没领过」。
            //   两处都**一个字没改**，跟的是同一个变量（同节同块 ⇒ 它才在作用域里）。
            // ⚠️ **⑦（那句 `Say` 的日志）本件不做**：H29 §3·2 明写「本件**没**为此加读口」——
            //   要断它得用 `Application.logMessageReceived` 抓 `[Daily]`（本文件已有那种抓法），本件照报告**不写死**。
            {
                int dAuto = DailyData.StreakClaimableDay;                  // = `StreakCollected`（只有这一格可领）
                DailyData.ForceStreakClaimedForTest(dAuto, false);
                ds.Build();
                var eAuto = ds.entries.Count > dAuto ? ds.entries[dAuto] : null;
                CheckTrue(eAuto != null, "★ A482①：（前提）连登窗那一格（可领的那一格）建出来了");
                string artAuto = eAuto != null ? ArtOf(FindChild(eAuto, "Reward Holder")) : null;
                CheckTrue(artAuto != null,
                          "★ A482①：（前提）那一格的 `Reward Holder` 画得出来"
                          + "（取不到图名 ⇒ 下面几条会**空转成恒真**）");
                int wAuto0 = artAuto != null ? Wallet.Of(artAuto) : 0;
                // 前提：场上先清干净（`FindOpenRewardWindow` / `OpenRewardWindowCount` 扫的是**全部**管理器）
                RewardWindowFixture.DismissRewardWindows();
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                      "★ A482①：（前提）这一刻场上没有遗留的领奖窗（不成立 ⇒ ⑤ 会数到别人家的窗）");

                // ---- ②③⑤④：**关窗那一拍真收**（= 原版 `Close()` 里那个 `TryCollect`）----
                Check(DailyData.StreakAutoCollect(), true,
                      "★★ A482②：**关窗 ⇒ 真收到了**"
                      + "（原版 `DailyStreakWindow.Close()` = `MissionEvent.TryCollect(() => base.Close())`）"
                      + " —— 把末尾那句 `CollectStreak` 删掉、退回空壳 ⇒ 得 `false` ⇒ 红（这正是 A400 要拦的东西）");
                CheckTrue(DailyData.StreakRewardClaimed(dAuto),
                          "★ A482③：…而且**那一格记成已领**（`_streakClaimed` 置位；只在外面 `Say` 一句 ⇒ 红）");
                var rwAuto = RewardWindowFixture.FindOpenRewardWindow();
                CheckTrue(rwAuto != null && rwAuto.Context != null && rwAuto.Context.Rewards != null
                          && rwAuto.Context.Rewards.Length == 1
                          && rwAuto.Context.Rewards[0].Id == artAuto,
                          "★★ A482⑤：…而且**弹的是那扇 `Reward Window`、装的就是那一格画的那张图**"
                          + "（`Id` = `Reward Holder` 上 `ImageQuad.Texture` 的真值 ——"
                          + " 只置位不发奖 ⇒ 这里没有窗 ⇒ 红；开窗传错格 ⇒ 也红）");
                int srQty = -9999;      // ⚠️ `RewardSpec` 是 **struct** ⇒ 不能拿 `null` 当「取不到」
                if (rwAuto != null && rwAuto.Context != null && rwAuto.Context.Rewards != null
                    && rwAuto.Context.Rewards.Length == 1) srQty = rwAuto.Context.Rewards[0].Quantity;
                Check(artAuto != null ? Wallet.Of(artAuto) - wAuto0 : 0, srQty,
                      "★ A482④：…而且**真记进 `Wallet` 了**（增量 = 那扇窗自己报的 `Quantity` —— 两个独立读数互证；"
                      + "转调改成自己写一份 `Wallet.Grant` 但数量抄错 ⇒ 红）");

                // ---- ⑥ 负例：**再收一次 ⇒ 什么都没发生**（守卫 = A316 统一过的那份共用布尔 `StreakRewardUnlocked`）----
                RewardWindowFixture.DismissRewardWindows();     // ⚠️ 先关掉 ② 弹的那扇（不然下面要按**绝对量**数窗）
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                      "★ A482⑥：（前提）② 弹的那扇窗已经收掉 —— 下面那条才能按绝对量数窗");
                int wAuto1 = artAuto != null ? Wallet.Of(artAuto) : 0;
                Check(DailyData.StreakAutoCollect(), false,
                      "★★ A482⑥（负例）：**已领过再关一次 ⇒ 什么都没发生**（返回假）"
                      + " —— 把 `if (!StreakRewardUnlocked(StreakCollected))` 那半句删掉、改成**恒收** ⇒ 红");
                Check(artAuto != null ? Wallet.Of(artAuto) - wAuto1 : 0, 0, "★ A482⑥：…而且**一份都没再发**");
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                      "★ A482⑥：…而且**一扇窗都没弹**（三条一起才钉得住「守卫走的是那份共用布尔」）");

                // ---- ⑧ 收工还原：把那一格拨回本节起点那个态（A316 那一节的收尾写法就是范本）----
                DailyData.ForceStreakClaimedForTest(dAuto, claimed0);
                ds.Build();
                var eAutoBack = ds.entries.Count > dAuto ? ds.entries[dAuto] : null;
                CheckTrue(eAutoBack == null || FindChild(eAutoBack, "Collect") != null,
                          "（A482 还原）那一格回到「没领过」⇒ `Collect` 又画出来"
                          + "（不还原 ⇒ 后面每一节都吃到一条「已领过」的连登轨）");
            }
            ds.Close();                                                // 交还给下一节（§九 开头自己还会再清一次）
        }
        // ============================================================ 🆕 **2026-10-11（批次1 · W1 · A309 + A310）**
        // **「玩家真能走到的那条领奖流程会弹出那扇窗」**（A309）+ 补那扇窗没做的两处（A310① 逐件 punch ·
        // A310⑤ `Tap To Continue` 的 `BlinkGraphic`）。
        // 判据（期望值**全部直读原版**，⛔ 不读 `Shell/RewardWindow.cs` 里的常量当期望）：
        //   · 开窗时机 → `d:/2/tools/decomp_full/RewardService__Collect.c:190-206`：**发完奖**就
        //     `new RewardWindowContext(rewards, isPremiumLocked, onCollect: **null**, onClose: onCollected,
        //      …, isPreview: **0**)` → `OpenWindow<RewardWindow>`（= 「Rewards claimed」那一态、无 `Collect` 钮）；
        //   · punch 的四个字面量 → `RewardWindow__DoRewardAnimation.c:110-115` + `工具/read_literal.py`
        //     （`0x1834b3158` = **0.4** · 立即数 vibrato **5** · `0x1834b2bb0` = **0.2** ·
        //      `0x1834b2dc4` = **0.1** · `0x1834b2e84` = **0.75**）；
        //   · 闪烁 → `BlinkGraphic__Update.c`（`t = Clamp01(|sin(blinkSpeed × time)|)`，
        //     只改 alpha：`Lerp(a, a × colorVariation, t)`）+ **36 个 prefab 实例实读**
        //     （`blinkSpeed` **1.0** / `colorVariation` **0.5**，与 ctor 默认值逐位相同）。
        Section("§九 领奖窗的接线（A309）+ 逐件 punch / 闪烁提示（A310①⑤）");
        {
            // 批处理里没有帧循环 ⇒ 补间要手动推（`CardTween.Mode` 的既有约定，见 `Core/CardTween.cs`）
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            // 场上先清干净：同类窗叠两扇时 `PointerLayer.HitButton` 的赢家会退化成枚举顺序（本仓已知的坑）
            wm2.CloseAllWindows();

            var oneSpec = new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierBasic);
            var twelve = new List<CampaignData.RewardSpec>();
            for (int i = 0; i < 12; i++) twelve.Add(oneSpec);

            // ---- (a) ★ 真路径：点**每日奖励窗那个可领的抽屉** ⇒ 弹出领奖窗 ----
            var dr2 = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(dr2);
            Transform cHit = null; WindowButton cBtn = null; int cDay = -1;
            for (int day = 0; day < DailyData.RewardDays && cHit == null; day++)
            {
                if (DailyData.RewardStateOf(day, false) != RewardState.Unlocked) continue;
                if (dr2.entries.Count <= day) break;
                var col = FindChild(FindChild(dr2.entries[day], "NormalReward"), "colider");
                if (col == null || !col.gameObject.activeInHierarchy) continue;
                var h = FindChild(col, "Hit");
                var wb = h != null ? h.GetComponentInChildren<WindowButton>(true) : null;
                if (wb == null) continue;
                cHit = h; cBtn = wb; cDay = day;
            }
            CheckTrue(cHit != null,
                      "（前提）找得到一个**可领**（`Unlocked`）的奖励抽屉，`colider/Hit` 上有 `WindowButton`"
                      + "（原版 `SetState`：只有 `Unlocked` 才开 `colider`）—— 找不到 ⇒ 下面整段等于没验");
            string cArt = null; int cAmt = 0; bool clicked = false;
            if (cHit != null)
            {
                cArt = DailyData.RewardIconOf(cDay, false);
                cAmt = DailyData.RewardAmount(cDay, false);
                int wBefore = Wallet.Of(cArt);
                clicked = ClickButtonByQuad(PointerLayer.Instance, cHit, cBtn, "每日奖励窗的领奖抽屉");
                CheckTrue(clicked, "点得动（走 `PointerLayer` 真路径，不是直调 `onClick`）");
                Check(Wallet.Of(cArt), wBefore + cAmt,
                      $"★ 领到了 ⇒ 资源记账 **+{cAmt}**（单机口径：原版这一步走 PlayFab 云脚本 + `RewardService.Collect`）");
            }

            RewardWindow rw2 = null;
            for (int i = 0; i < wm2.openWindows.Count; i++)
            {
                var w = wm2.openWindows[i];
                if (w == null || w.CurrentState == WindowState.Closed) continue;
                if (w is RewardWindow) { rw2 = (RewardWindow)w; break; }
            }
            CheckTrue(rw2 != null,
                      "★★ **点领奖之后弹出原版那扇 `Reward Window` 了**（判据 = `RewardService.Collect` 的公共出口："
                      + "发完奖就 `OpenWindow<RewardWindow>(ctx)`）—— 这条就是 A309 要的「有真调用点」");
            if (rw2 != null)
            {
                Check(rw2.IsPreview, false, "★ 是 **Collect 那一态**（原版那句传 `isPreview: 0` ⇒ 标题 `Rewards claimed`）");
                Check(rw2.IsPremiumLocked, true, "…`isPremiumLocked` 走原版形参默认值 **true**");
                var rc = rw2.Context;
                Check(rc != null && rc.Rewards != null ? rc.Rewards.Length : -1, 1,
                      "…带进来的是**这一格**那条奖励（1 条 —— 不是空窗、也不是把所有格都塞进来）");
                CheckTrue(rc != null && rc.Rewards != null && rc.Rewards.Length == 1 && rc.Rewards[0].Id == cArt,
                          "…而它的 `Id` 就是那一格**发的图名**（日常线没有服务端 item id —— 见 `DailyData.ShowCollectedWindow`）");
                Check(rc != null ? rc.OnCollect : null, (System.Action<CampaignData.RewardSpec[]>)null,
                      "…`onCollect` = **null**（原版 `Collect` 传的就是 null ⇒ 按钮整颗不建）");
                var cbtn = FindChild(rw2.transform, "Collect Button");
                CheckTrue(cbtn != null && !cbtn.gameObject.activeInHierarchy,
                          "★ **`Collect Button` 关着**（原版 `SetActive(OnCollect != null)`）—— 传了 `onCollect` 的实现这里会开");
                var tapN = FindChild(rw2.transform, "Tap To Continue");
                CheckTrue(tapN != null && tapN.gameObject.activeInHierarchy,
                          "…而 `Tap To Continue` 开着（原版 `SetActive(!IsPreview)`，读 `0x19`）");
                rw2.Close();
            }
            // 关窗 ⇒ `Close()` 里那句 `onClose(Rewards)` ⇒ 还开着的日常窗被**重建**（抽屉从 `Unlocked` 变 `Collected`）
            // 🔴 **2026-10-14（A671 · WA569 §2·R1）就地订正（铁律 5）**：这一段原来外面套着 `if (cHit != null)`，
            //   而 `cHit` 此刻**已经是死引用** —— `rw2.Close()` → `RewardWindow.Close()`（`Shell/RewardWindow.cs` 的 `Close`）
            //   **先发 `OnClose`** → `DailyData.RefreshOpenDailyWindows`（`Shell/DailyData.cs` 的 `RefreshOpenDailyWindows`）**只跳过
            //   `state == Closed` 的窗**，而 `dr2` 此刻是 **`Background`**（不是 `Closed`）⇒ `dr2.Build()` 真跑了
            //   → `DailyRewardPopup.Build()` 首句 `DestroySafe(整棵子树)`（`Shell/DailyRewardPopup.cs:215-219`）
            //   （⚠️ 上面这两处行号是 2026-10-14 现读值 —— 本文件/本仓行号一直在漂，按**锚点**找：
            //     `if (w == null || w.CurrentState == WindowState.Closed) continue;` · `public void Build()`）
            //   ⇒ 那层 `if` 判**假** ⇒ 里面两条 ★ 断言**一条都不跑**（含「日常窗真被重建过」那条 ——
            //     **恰恰就是被这次重建干掉的**）：**静默假绿**（跳过 ≠ 失败，`_pass++` 从此没加过分），
            //     而紧跟其上的注释自己就白纸黑字写着「关窗 ⇒ 还开着的日常窗被重建」。
            //   ⇒ 改成用 `cDay` **现取**，并补一条**前提出声**（「量不到」与「真重建了」不再同形）。
            //   ⚠️ `cDay < 0`（上面没找到可领的抽屉）时**不能**索引 `entries` —— 那正是上面那条
            //      `CheckTrue(cHit != null, "（前提）…")` 在报的错；这里给一句显式红，⛔ 别改成静默跳过。
            if (cDay >= 0)
            {
                Check(DailyData.RewardStateOf(cDay, false), RewardState.Collected,
                      "★ 关窗之后那一格进了 `Collected`（`onClose` 回调那一跳）");
                var colAfter = dr2.entries.Count > cDay
                    ? FindChild(FindChild(dr2.entries[cDay], "NormalReward"), "colider") : null;
                CheckTrue(colAfter != null,
                          "（前提）关窗重建之后那一格仍在（`entries[cDay]` **现取** —— ⛔ 别用上面那个 `cHit`："
                          + "它此刻已是死引用）—— 抓不到 ⇒ 下面那条等于空断");
                if (colAfter != null)
                    CheckTrue(!colAfter.gameObject.activeInHierarchy,
                              "★ …而且**日常窗真被重建过**：`colider` 整颗关掉（= 原版 `SetState` 的 `Collected` 那一档）"
                              + " —— 少了 `onClose → Build()` 那一跳，它会**停在 `Unlocked`**（静默陈旧；"
                              + "而且能无限重领 —— `CollectReward` 的守卫问的就是这个状态）");
            }
            else
                CheckTrue(false, "（前提）上面一个可领的抽屉都没找到（`cDay` 仍为 −1）⇒ 关窗后那两条**没验成**");
            dr2.Close();

            // ---- (b) 逐件 punch 的**算式**（纯函数，⛔ 不依赖任何窗口）----
            // 期望值 = 原版那条式子的**手算结果**：分母 = 视口半宽 960（`ScrollView.W/2`）· 系数 0.75
            CheckNear(RewardWindow.PunchNorm(480f, 960f), 0.5f, 1e-4f, "归一化距离 = |480| ÷ 960 = **0.5**");
            CheckNear(RewardWindow.PunchNorm(-480f, 960f), 0.5f, 1e-4f,
                      "…**取绝对值**（原版那一位是 `Mathf.Abs` 的位掩码 `0x7FFFFFFF`）—— 写成带符号的实现这里给 −0.5");
            CheckNear(RewardWindow.PunchNorm(2000f, 960f), 1f, 1e-4f, "…**夹到 [0,1]**（超出视口半宽的格不再加延迟）");
            CheckNear(RewardWindow.PunchNorm(5f, 0f), 0f, 1e-4f,
                      "…分母为 0 ⇒ **判 0**（原版 `if (fVar17 == 0.0)` 那道守卫；少了它这里会得 ∞）");
            // 🔴 **2026-10-11（批次2 · F2）就地标定（铁律 5）**：下面这条的期望值 0.1875 是用 **我们挑的**
            //    `ItemW = 200` 现算的（`Shell/RewardWindow.cs` 自己写着「⚠️ **这不是原版值**，别当判据用」）
            //    ⇒ 它**只证「我们那条式子自洽」**，⛔ **证不了「与原版一致」**（原版抽屉尺寸在 prefab 里、
            //    尺寸不同延迟本来就不同）。⇒ 保留为 **fixture**（当回归网），真判据是再下面两条**相对**断言
            //    ＋ 本段那三条 `PunchNorm`（两条的数都是原版那边的：**0.75** = 读出来的字面量 `0x1834b2e84`；
            //    **960** = 原版 `Scroll View` 宽 1920 × pivot 推得的等价量，见 `PunchRefPx` 的注释）。
            CheckNear(RewardWindow.PunchDelayAt(2, 3), 0.25f * 0.75f, 1e-4f,
                      "（fixture · 证自洽、不证原版）3 格时第 3 格：按**我们挑的** `ItemW = 200` ⇒ 内容宽 800 ⇒ "
                      + "它离内容中心 240 ⇒ 240/960 = 0.25 ⇒ 延迟 = 0.25 × **0.75** = 0.1875");
            // ★ **相对断言**（R6 补）：⛔ 一个 `ItemW` 都不引 —— 只比**两条延迟之间**的关系。原版那条式子
            //   = `clamp01(|抽屉离内容中心的距离| ÷ 视口半宽) × 0.75` ⇒ 延迟**只**随「离内容中心的距离」变；
            //   抽屉是从中心往两边摆的（HLG `MiddleCenter`）⇒ 最外那格**必须**比正中那格晚。
            float midDelay = RewardWindow.PunchDelayAt(5, 12), edgeDelay = RewardWindow.PunchDelayAt(11, 12);
            CheckTrue(edgeDelay > midDelay + 0.05f,
                      $"★★ 相对断言（⛔ 不引 `ItemW`）：12 格时**最外那格比正中那格晚**（{edgeDelay:F4}s > {midDelay:F4}s）"
                      + " —— 「延迟随离内容中心的距离变」直接来自原版那条式子；"
                      + "「所有格同一个延迟」的实现两边相等 ⇒ 红");
            // ★ 左右对称（同上，仍不引 `ItemW`）：第 1 格与最后一格到内容中心的距离相同
            //   （`PadL == PadR` = **原版值** 60/60）⇒ 两条延迟必须相等。
            CheckNear(RewardWindow.PunchDelayAt(0, 12), edgeDelay, 1e-4f,
                      "★★ 相对断言（⛔ 不引 `ItemW`）：**第 1 格与最后一格的延迟相同**（左右对称）"
                      + " —— 「延迟随序号单调递增」的实现这里给 0 vs 0.75 ⇒ 红");

            // ---- (b2) ★ 抽屉**按 `RewardTier` 升序**摆（A310②：原来只是推断，现已坐实）----
            //   判据 = 那个泛型 LINQ 助手的地址 `FUN_180c99af0`（RVA `0xC99AF0`）在 `dump.cs` 的
            //   `GenericInstMethod` 表里正落在 `Enumerable.OrderBy<object, Int32Enum>` 那一行。
            //   夹具 = **混排**的 4 条（高级 / 基础 / 高级 / 基础）⇒ 期望 = 基础两条在前（**原相对次序**）、高级两条在后。
            var shuffled = new[]
            {
                new CampaignData.RewardSpec("C2", 400, CampaignData.TierPremium),
                new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierBasic),
                new CampaignData.RewardSpec("DT Ultramarines All", 3, CampaignData.TierPremium),
                new CampaignData.RewardSpec("WildcardUltramarines1", 1, CampaignData.TierBasic),
            };
            var rwS = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwS, new RewardWindowContext { Rewards = shuffled, IsPreview = true });
            var gotNames = new List<string>();
            for (int i = 0; i < rwS.ListHolder.childCount; i++) gotNames.Add(rwS.ListHolder.GetChild(i).name);
            Check(string.Join("|", gotNames.ToArray()),
                  "Item_Booster Pack Ultramarines|Item_WildcardUltramarines1|Item_C2|Item_DT Ultramarines All",
                  "★ 抽屉按 **`RewardTier` 升序**摆（原版 `Open()` 那句 `OrderBy(r => r.RewardTier)`）："
                  + "**基础档两条**（原序 Booster → Wildcard1）在前、**高级档两条**（原序 C2 → DT）在后；"
                  + "**不排序**的实现会得 `Item_C2|Item_Booster…|Item_DT…|Item_Wildcard…` ⇒ 红；"
                  + "⛔ 同档内保持**原相对次序**（LINQ `OrderBy` 是稳定排序）");

            // ---- (c) 逐件 punch 的**行为**（12 格，最右那格延迟满 0.75s）----
            var rwP = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwP, new RewardWindowContext { Rewards = twelve.ToArray(), IsPreview = false });
            // ⛔ 这一段**只推 tween、不推 `Tick`**（不触发重建 ⇒ 可以采得很密）
            float punchPeak = 0f, punchMin = 0f;
            for (int k = 0; k < 60; k++)
            {
                CardTween.Advance(0.02f);                       // 60 × 0.02 = 1.2s ≥ PunchEnd（0.75 + 0.4 = 1.15）
                float dev = rwP.PunchScaleOf(11).x - 1f;
                if (Mathf.Abs(dev) > punchPeak) punchPeak = Mathf.Abs(dev);
                if (dev < punchMin) punchMin = dev;             // 🔴 曲线**形状**那一条要用它（`punchFlips` 已废，见下）
            }
            CheckTrue(punchPeak > 0.02f,
                      $"★ 最后一格**真的弹了**（采样到的最大偏离 {punchPeak:F3}；一点都没做的实现恒为 0）");
            // 🔴 **2026-10-11（批次2 · F2）就地订正（铁律 5）**：下面这条的上界原来取的是**被测实现自己的常量**
            //    （`RewardWindow.PunchAmount`，而 `StartPunches` 里那个乘数读的就是它 ⇒ 只改这一处）
            //    ⇒ 把 `PunchAmount` 改成 0.5，这条**照样绿**（同式自证）。现在写的是**原版字面量**：
            //    `DAT_1834b2dc4` = **0.1**（`RewardWindow__DoRewardAnimation.c:113`；`工具/read_literal.py` 复算过 = 0.1）。
            CheckTrue(punchPeak <= 0.1f + 0.02f,
                      $"…幅度不超过原版那个 **0.1**（字面量 `0x1834b2dc4`；实测 {punchPeak:F3} —— "
                      + "写成 0.3/0.5 的实现这里超界；⛔ 上界**不读** `RewardWindow.PunchAmount`，那是被测实现的常量）");
            // 🔴 **2026-10-11（批次3 · FX4 / DIAG-B §二·#3）就地订正（铁律 5）**：这里原来是
            //    `CheckTrue(punchFlips >= 1, "★ …而且**来回穿零**（实测 {punchFlips} 次）—— 单调地放一下再收回来那种实现是 **0** 次")`
            //    —— **那条判据与 DOTween 的算法矛盾**（不是与本实现矛盾）：
            //    `DG.Tweening.DOTween.Punch` 的段数 = `(int)(vibrato × duration)`、`< 2` 时钳到 **2**
            //    （方法体 **IL 逐条读过**：`Assets/Plugins/Demigiant/DOTween/DOTween.dll`；且原版
            //     `GameAssembly.dll` 的同名函数反汇编逐句一致 —— 两边的 `count` 算式与 `endValues` 构造完全对位），
            //    我们这里传的是**原版那四个字面量**（`Shell/RewardWindow.cs:273-276`：0.4 / 5 / 0.2 / 0.1）
            //    ⇒ `count = (int)(5 × 0.4) = **2**` ⇒ `endValues = [direction, Vector3.zero]`
            //    ⇒ 轨迹 = `1.0 ──0.133s──▶ 1.1 ──0.267s──▶ 1.0` = **单峰、构造性不穿零**
            //    （`elasticity = 0.2` 在 `count == 2` 时**一次都没被读到** —— `i % 2 != 0` 那一支要 `count ≥ 3` 才可能走到）。
            //    ⇒ **`punchFlips == 0` 不是缺陷，是原版行为**（原版同一 API、同一组实参 ⇒ 原版也是 0 次）；
            //      而作者原来写的那条「改坏法」（「单调地放一下再收回来那种实现是 0 次」）**正是正确实现的样子**。
            //    ⇒ 现在断的是**曲线的形状**（只涨、不跌到 1 以下 + 两个段界），那才既反映原版、又挡得住瞎写的实现。
            CheckTrue(punchMin >= -1e-3f,
                      $"★ …而且**从不向下穿零**（采样到的最小偏离 {punchMin:F4}）—— `count = (int)(5 × 0.4) = 2`"
                      + " ⇒ 曲线 = `1.0 → 1.1 → 1.0`，**只涨、不跌到 1 以下**。"
                      + " 改坏法：写成 `1.0 → 1.1 → 0.9 → 1.0`（过冲），或把 `vibrato` 调大到"
                      + " `count = (int)(vibrato × 0.4) ≥ 3`（**vibrato ≥ 8** ⇒ 8 给 3、10 给 4；"
                      + " 那时 `end[1] = −ClampMagnitude(direction, mag × elasticity)`）⇒ 第二段整段压到 1 以下 ⇒ 红");
            // ★ **段界那两条**（端点值与缓动无关 ⇒ 能判「幅度对不对 / 段时长对不对」）。
            //   段界算式的出处 = DOTween 的段时长 `dur[i] ∝ (i+1)/count` **归一化到 duration**：
            //   `count = 2` ⇒ 原始 `[0.4×1/2, 0.4×2/2] = [0.2, 0.4]`、合计 0.6 ⇒ 归一化（×0.4/0.6）
            //   ⇒ `[0.4/3, 0.8/3]` ⇒ **第一段走完那一拍 = `delay + 0.4/3`**、**末点回 0 = `delay + 0.4`**。
            //   `endValues[0] = direction = 0.1 × oneVector`、`endValues[1] = Vector3.zero` ⇒ 两个期望值都是**原版字面量手算**。
            //   ⚠️ 采样点**必须精确落在 `t` 上**（一次 `Advance` 推到位）：段界那一拍的斜率 = `0.1 ÷ (0.4/3)` = **0.75/s**
            //      ⇒ 用 0.02 的步长「走过去」会偏 0.015（> 容差 0.01）⇒ **对的实现也会红**。
            //   ⚠️ 用**新建的一扇窗**（时钟从 0 起）⇒ 一次 `Advance(t)` 就落在 `t` 上，不必记账。
            //   ⚠️ `0.4f` 是**原版字面量**（`RewardWindow__DoRewardAnimation.c:110-114` 的 0.4），⛔ 不读 `RewardWindow.PunchTime`
            //      —— 拿被测实现的常量当期望值就是**同式自证**（本批 F2 刚为此订正过上面那两条）。
            //   改坏法：把幅度写成 0.3 / 0.05 ⇒ 第一条红；把 `PunchTime` 改成 0.5（段界跟着挪）⇒ 两条都红。
            //   ⚠️ 如实说：这两条**判不到缓动**（`SetEase` 只改路上、不改端点）—— 端点测试本来就不该管那个。
            {
                var rwE = RewardWindow.Create(wm2);
                wm2.OpenWindow(rwE, new RewardWindowContext { Rewards = twelve.ToArray(), IsPreview = false });
                CardTween.Advance(RewardWindow.PunchDelayAt(11, 12) + 0.4f / 3f);
                CheckNear(rwE.PunchScaleOf(11).x - 1f, 0.1f, 0.01f,
                          "★（段界）`delay + duration/3` 那一拍正好停在原版那个幅度 **0.1** 上"
                          + "（`endValues[0] = direction`；`count = 2` ⇒ 段 1 = `0.4/3`）"
                          + " —— 幅度写成 0.3/0.05、或把段时长写成各半的实现这里红");
                CardTween.Advance(0.4f - 0.4f / 3f);        // 合计推到 `delay + 0.4`
                CheckNear(rwE.PunchScaleOf(11).x - 1f, 0.0f, 0.005f,
                          "★（段界）`delay + duration`（0.75 + 0.4）**正好回到 0**（`endValues[count-1] = Vector3.zero`）"
                          + " —— 收不回去（停在 1.1）的实现这里红");
            }
            CheckNear(rwP.PunchScaleOf(11).x, 1f, 0.005f, "…跑完（1.2s > 0.75 + 0.4）**回正到 1**");
            CheckNear(RewardWindow.PunchDelayAt(11, 12), 0.75f, 1e-4f,
                      "（前提）12 格时最右那格延迟**正好顶到 0.75**（归一化距离被夹到 1）");

            // ---- (d) ★ 揭示跑完之后 punch 还得继续（这一条专治「tween 绑在 Transform 上被重建毁掉」）----
            //   采样点全部落在**第 12 格 punch 的活动区间**（延迟 0.75 + 时长 0.4 = [0.75, 1.15]）里，
            //   而揭示在 0.8s 就结束了 ⇒ 这几拍走的是 `Tick` 的**第二个分支**（不重建、只把值重贴给节点）。
            var rwQ = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwQ, new RewardWindowContext { Rewards = twelve.ToArray(), IsPreview = false });
            for (int k = 0; k < 17; k++) { CardTween.Advance(0.05f); rwQ.Tick(0.05f); }   // 0.85s：揭示（0.8s）刚跑完
            CheckNear(rwQ.RevealProgress, 1f, 1e-3f, "（前提）揭示已跑完（t = 0.85s > 0.8s）");
            float qWorst = 0f; int qSampled = 0; bool qMatch = true;
            // 采样点 t = 0.90 → 1.10 ⇒ 第 12 格那条 tween 此刻正处在**前半段**（相位 0.15 → 0.35 / 0.4），
            // ⛔ 别把采样往后拖 —— 越靠尾部振幅越衰，`qWorst > 0.01` 那条会变成看 DOTween 的衰减曲线脸色。
            for (int k = 0; k < 5; k++)
            {
                CardTween.Advance(0.05f); rwQ.Tick(0.05f);
                if (!rwQ.PunchRunning) break;      // 这一拍**没重贴**（簿记照旧在动）⇒ 不比（浮点边界，别硬撞）
                var node = rwQ.ListHolder != null && rwQ.ListHolder.childCount > 11
                    ? rwQ.ListHolder.GetChild(11) : null;
                if (node == null) { qMatch = false; break; }
                qSampled++;
                float ns = node.localScale.x;
                float dev = Mathf.Abs(ns - 1f);
                if (dev > qWorst) qWorst = dev;
                // ★ 判据：节点上那个值 == 簿记里那一刻的值。少了「不重建、只重贴」那一支，节点会
                //   **冻在最后一次重建那一刻的值**上，而簿记（tween）还在动 ⇒ 这几拍必然对不上。
                if (Mathf.Abs(ns - rwQ.PunchScaleOf(11).x) > 1e-3f) qMatch = false;
            }
            CheckTrue(qSampled >= 3, $"（前提）采到 {qSampled} 拍「揭示已完、punch 未停」的样本（要 ≥ 3）");
            CheckTrue(qWorst > 0.01f,
                      $"★ 揭示跑完（0.8s）之后第 12 格**还在弹**（实测最大偏离 {qWorst:F3}）—— 恒为 1 的实现这里红");
            CheckTrue(qMatch,
                      "★★ …而且**每一步**节点上的值与簿记对得上（同一个采样时刻）—— 把 tween 打在抽屉 `Transform` 上"
                      + "（而不是簿记数组）的实现：那个节点每帧被重建 ⇒ 值**冻在最后一次重建那一刻** ⇒ 这几拍全对不上");
            for (int k = 0; k < 2; k++) { CardTween.Advance(0.05f); rwQ.Tick(0.05f); }    // 推到 1.20s > PunchEnd
            CheckNear(rwQ.PunchScaleOf(11).x, 1f, 0.005f, "…跑到 1.20s（> 0.75 + 0.4）⇒ 簿记值回正到 1");

            // ---- (e) 预览态**不播** punch（用 `locked = false` 把两个 flag 分开 ⇒ 拿 `IsPremiumLocked` 当开关的实现会红）----
            var rwV = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwV, new RewardWindowContext { Rewards = twelve.ToArray(), IsPreview = true, IsPremiumLocked = false });
            CheckTrue(!rwV.PunchRunning, "★ 预览态**不播** punch（原版 `RewardWindow__Open.c:283` 的 `if (0x19 == 0)`）");
            CardTween.Advance(0.5f); rwV.Tick(0.5f);
            var vNode = rwV.ListHolder != null && rwV.ListHolder.childCount > 0 ? rwV.ListHolder.GetChild(0) : null;
            CheckNear(rwV.PunchScaleOf(0).x, 1f, 1e-4f,
                      "…推半秒也还是 1（⚠️ 本档 `IsPremiumLocked = **false**` ⇒ 拿 `IsPremiumLocked` 当开关的实现**会弹** ⇒ 这条分得出）");
            CheckTrue(vNode == null || Mathf.Abs(vNode.localScale.x - 1f) < 1e-3f, "…渲染节点上也没有缩放");

            // ---- (f) `Tap To Continue` 的闪烁（采样点取在原版公式的**已知值**上）----
            // 🔴 **2026-10-11（批次2 · F2）整组重算（铁律 5）**：下面几条期望值原来是按**我们自己的 `sin` 公式**
            //    写的（时钟 0 / π/2 / π ⇒ α = 1.0 / 0.5 / 1.0）。`Shell/RewardWindow.cs` 的 `BlinkT` 已按原版改成
            //    `Clamp01(|**cos**(blinkSpeed × currentTime)|)`（判据 = `BlinkGraphic__Update.c:17`；三条 sin/cos
            //    判别见 F1）⇒ **三条全翻**成 **0.5 / 1.0 / 0.5**（`α = Lerp(A, A×0.5, |cos t|)`，`A` = 原色 = 1）。
            // ⛔ **期望值一个都不读 `RewardWindow` 的常量**（`BlinkSpeed` / `BlinkVariation` 是被测实现里的数，
            //    拿它们当期望值 = 同式自证 —— 这一组原来的毛病）。★ 下面写的是**原版 ctor 的两个立即数**
            //    （`BlinkGraphic__.ctor.c:4-5`：`0x28 = 0x3f800000` = **1.0** · `0x2c = 0x3f000000` = **0.5**）
            //    ＋ `|cos|` 的**手算结果** ⇒ 换一个 `blinkSpeed` / `colorVariation` 的实现，这一组立刻红。
            // ⚠️ **时钟语义**（🔴 **2026-10-11（批次2 · A355）：这两句已经按原版落地，「次序差」不存在了**）：
            //    原版 `Update()` = 「**先按此刻的 `currentTime` 算颜色、算完才** `currentTime += deltaTime`」
            //    （`BlinkGraphic__Update.c:17` 读时钟 → `:24-26` 写色 → **`:31` 才**推进）；我们 `BlinkTick`
            //    现在**逐字同序** ⇒ **要采「时钟 T 那一刻的颜色」必须先 `Tick(T − 上一次)` 把时钟推到 T、
            //    再 `Tick(0f)`**（推进 0 ⇒ 只按当前时钟写一次色）——
            //    ⚠️ 这次序**正是本组期望值一个都不用改**的原因（下面每一档都按它采）。
            //    🔴 另外每次**推进那一拍**还断一条「次序判别式」：`dt ≠ 0` 的那一步写进标签的必须是
            //    **推进前**那一档 —— 少了它，`Tick(T)` + `Tick(0f)` 这一对在新旧次序下最后一笔相同 ⇒
            //    **分不出两种次序**（本仓那条「弱断言分不出两种状态」的老毛病）。
            var rwB = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwB, new RewardWindowContext { Rewards = new[] { oneSpec }, IsPreview = false });
            var tapT = FindChild(rwB.transform, "Tap Text");
            var tapLb = tapT != null ? tapT.GetComponentInChildren<Label>() : null;
            CheckTrue(tapLb != null, "（前提）`Tap To Continue` 那段字建出来了（= 原版 `BlinkGraphic.graphic` 的落点）");
            // ⚠️ **这一条不验闪烁公式** —— `Build()` 不调 `BlinkTick`，所以此刻读到的是**建的时候那个原色**
            //    （原版 `BlinkGraphic.Start()` 也只是把 `graphic.color` 存下来、当帧不改它）。
            //    ⛔ 别把它读成「时钟 0 ⇒ α = 原色」（那是 `sin` 口径；cos 下时钟 0 是 **0.5**，见下一条）。
            CheckNear(tapLb != null ? tapLb.color.a : -1f, 1f, 1e-3f,
                      "（前提）建完还没 `Tick` ⇒ 颜色还是出厂那个原色（`Build()` 不调 `BlinkTick`；原版 `Start()` 同样只存不写）");
            // ★ 时钟 0（= 原版第一帧）：`|cos 0| = 1` ⇒ α = `Lerp(1, 1×0.5, 1)` = **0.5**
            //   🔴 **这一条就是 sin/cos 的分水岭**：`sin` 的实现这里是 **1.0**（原色）。
            rwB.Tick(0f);
            float aAt0 = tapLb != null ? tapLb.color.a : -1f;
            CheckNear(aAt0, 0.5f, 0.01f,
                      "★★ 时钟 **0** ⇒ `|cos 0| = 1` ⇒ alpha = **0.5**（`Lerp(1, 1×0.5, 1)`；1.0 与 0.5 都是原版 ctor 的立即数）"
                      + "；写成 `sin` 的实现这里是 **1.0** ⇒ 红");
            CheckNear(rwB.BlinkT, 1f, 0.005f, "…而 `t` = 1（`|cos 0|`）");
            // ★ 时钟 π/2：`|cos(π/2)| = 0` ⇒ α = **原色**（1.0）
            rwB.Tick(Mathf.PI * 0.5f);                      // 推时钟这一拍：按**推进前**那一档写色
            // 🔴 **次序判别式**（A355 落地后新加的这一条）—— 见上面「时钟语义」那一段：
            //    推进那一拍（`dt ≠ 0`）**不改变**已写进标签的颜色（原版 `:31` 的推进在写色之后）。
            //    改坏法：把 `Shell/RewardWindow.cs` 的 `_blinkT += dt;` 挪回写色**之前** ⇒ 这里读到 1.0 ⇒ 红。
            CheckNear(tapLb != null ? tapLb.color.a : -1f, 0.5f, 0.01f,
                      "★★（**次序**）`Tick(π/2)` 这一步写进标签的仍是**推进前**那一档（时钟 0 ⇒ `|cos 0| = 1` ⇒ α = **0.5**）"
                      + "（原版 `BlinkGraphic__Update.c`：`:17` 读时钟 → `:24-26` 写色 → **`:31` 才** `+= deltaTime`）"
                      + "；先 `+= dt` 再写色的实现这里是 **1.0**（时钟 π/2 那一档）⇒ 红");
            rwB.Tick(0f);                                   // 按**当前**时钟（π/2）写色、推进 0 ⇒ 取到 T = π/2 那一档
            float aAtHalf = tapLb != null ? tapLb.color.a : -1f;
            CheckNear(aAtHalf, 1f, 0.01f,
                      "★★ 时钟 **π/2** ⇒ `|cos| = 0` ⇒ alpha = **原色 1.0**（`Lerp(1, 0.5, 0)`）—— 与上一条凑成余弦的两个端点；"
                      + "`colorVariation` 不是 0.5 的实现这里就错");
            CheckNear(rwB.BlinkT, 0f, 0.005f, "…而 `t` = 0（`blinkSpeed = 1` ⇒ 半周期 π/2 处到 0）");
            // ★ **非端点采样**（R2：期望值 = **原版算式**在本文件里手算 —— `clamp01(|cos|)` ＋ 上面那两个立即数）
            rwB.Tick(Mathf.PI / 6f);                        // 时钟 = 2π/3（推时钟那一拍按 2π/3 **前**那一档写色）
            rwB.Tick(0f);                                   // 再按当前时钟（2π/3）写一次 ⇒ 取到 2π/3 那一档
            float aAt2p3 = tapLb != null ? tapLb.color.a : -1f;
            CheckNear(aAt2p3, 0.75f, 0.01f,
                      "★ 时钟 **2π/3** ⇒ `|cos| = 0.5` ⇒ alpha = `Lerp(1, 0.5, 0.5)` = **0.75**"
                      + "（期望值 = 原版算式的**手算值**，⛔ 没有引任何 `RewardWindow` 常量）");
            rwB.Tick(Mathf.PI / 3f);                        // 时钟 = π
            rwB.Tick(0f);                                   // 再按当前时钟（π）写一次 ⇒ 取到 π 那一档
            float aAtPi = tapLb != null ? tapLb.color.a : -1f;
            CheckNear(aAtPi, 0.5f, 0.01f,
                      "★ 时钟 **π** ⇒ `|cos π| = 1` ⇒ alpha = **0.5**（`|cos|` 的周期是 **π** —— 0 与 π 是两个极小值点）");
            CheckNear(rwB.BlinkClock, Mathf.PI, 0.01f, "…时钟累计 = π（`currentTime += deltaTime`）");
            // ★ **相对断言**（R2 第二条）：只比**两条不同时刻之间**的关系，⛔ 一个常量都不引 ——
            //   `α = Lerp(A, A×0.5, |cos t|)` 随 `|cos|` 单调**降**，而 `|cos|` 在 `[0, π/2]` 降、`[π/2, π]` 升
            //   ⇒ α 必须**先升后降**（极大值落在 π/2、两个极小值落在 0 与 π）。`sin` 的实现三段全反 ⇒ 红。
            CheckTrue(aAtHalf > aAt0 + 0.1f && aAt2p3 < aAtHalf - 0.1f && aAtPi < aAt2p3 - 0.1f,
                      $"★ 相对断言（不引任何常量）：α **先升后降**（时钟 0 → π/2 → 2π/3 → π 实测 "
                      + $"{aAt0:F3} → {aAtHalf:F3} → {aAt2p3:F3} → {aAtPi:F3}）—— 极大值必须落在 π/2 上");

            // 预览态那一档：`Tap To Continue` 整颗关着（原版 `SetActive(!IsPreview)`）⇒ `BlinkGraphic` 也不跑
            var rwVb = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwVb, new RewardWindowContext { Rewards = new[] { oneSpec }, IsPreview = true });
            var tapV = FindChild(rwVb.transform, "Tap To Continue");
            CheckTrue(tapV != null && !tapV.gameObject.activeInHierarchy,
                      "预览态 ⇒ `Tap To Continue` 整颗关着（闪烁那件跟着它一起不跑，= 原版组件挂在那个节点上）");

            // ============================================================ 🆕 **2026-10-11（批次2 · A356）**
            // A311（抽屉层四跳）的断言 —— **它落地时一条都没进宿主**（当时 `Editor/RewardsScene.cs` 在黑名单里），
            // 而本批那条唯一的红（§⑤）**正是它带出来的副作用** ⇒ 这一批把 6 组一起落进来。
            // 判据逐条见下面每段头；**每组都写了「改坏法」**（本仓那条「弱断言分不出两种状态」的老毛病）。

            // ---- (g) **A311 第 1/2/3 跳**（`TogglePremiumHighlight` / `SetEphemeralDisplay` / `SetConvertedItem`）----
            //   判据 = `RewardWindow__Open.c:111-132` 逐句（**四跳**里的前三跳；第 4 跳在 (h)）＋ 三个方法体的
            //   **现读反汇编**（`ItemDrawer<T>` 的泛型实例在 `decomp_full/` 里**没有 `.c`** ⇒ 按 VA 读；
            //   地址与字段偏移的互证见 `Shell/ItemDrawer.cs` §⑤ 那段）：
            //     ① `drawer.<虚表 0x1b8>((reward.rewardTier(+0x30) == 10))` = `TogglePremiumHighlight(bool)`
            //     ② `convertedInto(+0x38) == null && reward.IsEphemeral` ⇒ `drawer.<0x1e8>(ephemeralState(+0x28))`
            //     ③ `convertedInto != null` ⇒ `drawer.<0x1d8>(convertedInto)` —— ②③ **互斥**、③ 优先
            //   ⚠️ 三件的**节点名逐字照 prefab**（`Premium Highlight` / `Highlight` / `Blackout` / `Badge` /
            //   `Converted Drawer` / `Ephemeral Drawer` / `Price Display` / `icon` / `text` / `AlreadyOwned`）。
            //   ⚠️ 夹具用 `IsPreview = true`：那一档**揭示恒开**（`_reveal = 1`）⇒ 遮罩不会把装饰层裁掉。
            {
                var rwJ = RewardWindow.Create(wm2);
                wm2.OpenWindow(rwJ, new RewardWindowContext
                {
                    Rewards = new[]
                    {
                        // ① 高级档（`Tier == 10`）：**只有这一格**该长 premium 三件
                        new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierPremium),
                        // ② 限时：`2` 天整 ⇒ 原版那串先出 `2d 0h`、再被两个 `Replace` 削成 `2d`
                        new CampaignData.RewardSpec("DT Ultramarines All", 1, CampaignData.TierBasic,
                                                    true, 2L * 24L * 60L * 60L * 1000L, null),
                        // ③ 已拥有 ⇒ 折算成 7 个野牌（**那一项自己有图** ⇒ 能判「画的是折算项的图、不是本格的图」）；
                        //    🔴 这一条**同时** `IsEphemeral = true` —— 故意让它**两个判据都真**：原版那两句
                        //    是 **if/else**（`convertedInto != null` 优先）⇒ 这一刻只该长 `Converted Drawer`
                        //    （「两个 `if` 都判」的实现会**同时**长出两条 ⇒ 下面那条红）。
                        new CampaignData.RewardSpec("WildcardUltramarines1", 1, CampaignData.TierBasic,
                                                    true, 2L * 24L * 60L * 60L * 1000L,
                                                    new CampaignData.RewardConversion("WildcardUltramarines2", 7)),
                        // ④ 第 4 跳的**反例**：`quantity = 3`，但类型**可堆叠**（`Everguild.LiveOps.ShopContainer`）
                        new CampaignData.RewardSpec("Booster Pack Legendary Ultramarines", 3, CampaignData.TierBasic),
                    },
                    IsPreview = true,
                });
                var prD = FindChild(rwJ.ListHolder, "Item_Booster Pack Ultramarines");
                var baD = FindChild(rwJ.ListHolder, "Item_DT Ultramarines All");
                var cvD = FindChild(rwJ.ListHolder, "Item_WildcardUltramarines1");
                CheckTrue(prD != null && baD != null && cvD != null,
                          "（前提）三格抽屉都建出来了（按 `Item_<短名>` 取件 —— 找不到 ⇒ 本段等于没查）");
                // ⚠️ **2026-10-12（A361）就地订正（铁律 5）**：这里原来写着「`CountByName` **不挡 null**」——
                // **那一句现在不成立了**（A361 给 `CountByName` 补了 `if (root == null) return 0;`）。
                // ✅ **这个小包装仍然要留**：它故意回 **−1**（节点不在 ⇒ 那条断言**自己红**），
                // 而 `CountByName` 那一份挡回的是 **0** —— 对本段「数出来该是 1/2」的判据来说
                // **0 与「什么都没数到」撞上**，不如 −1 有鉴别力（本仓那条「弱断言分不出两种状态」）。
                // ⇒ 两件事**各管各的**：`CountByName` 的挡管「别抛 NRE 把整条自检吞掉」，这层包装管「读数可分辨」。
                System.Func<Transform, string, int> cnt = (t, n) => t != null ? CountByName(t, n) : -1;

                // ---- 第 1 跳：premium 三件 ----
                var ph = FindChild(prD, "Premium Highlight");
                CheckTrue(ph != null,
                          "★ 高级档 ⇒ 第 1 跳真调了：抽屉下建出 `Premium Highlight` 容器"
                          + "（原版 `TogglePremiumHighlight(rewardTier == 10)`）");
                Check(cnt(baD, "Premium Highlight"), 0,
                      "★ …而基础档那格**一个都没有**（判据就是 `rewardTier == 10`；不看 tier 的实现两格都建 ⇒ 红）");
                Check(cnt(ph, "Highlight"), 1, "…`Highlight`（高亮框）在");
                Check(cnt(ph, "Blackout"), 1, "…`Blackout`（压暗层）在");
                Check(cnt(ph, "Badge"), 1,
                      "…`Badge`（角标）在 —— **三件齐**（少画一件 ⇒ 上面某条红）");
                // 🔴 几何：`Highlight` 铺满抽屉框、`sizeDelta (50,50)`（**拉伸锚 ⇒ 每边外扩 50**）
                //   ⇒ 画出来 = 框 ± `50 × k`、`k = box.H / DecorRefH = 300/1080 = 0.27778` ⇒ **13.889px**。
                //   ⚠️ 这条是 **fixture 型**（`50` 与 `1080` 是**原版字面量** —— prefab 的 `sizeDelta` 与
                //   `ItemDrawer.DecorRefH`；`200/300` 是**我们挑的**格子尺寸 `RewardWindow.ItemW/ItemH`）
                //   ⇒ 它证「折算按实现自己那条式子算对了」，**不证**版式与原版相同（原版抽屉在 prefab 里另有尺寸）。
                //   改坏法：把 `ItemDrawer` 的 `float pad = 50f * k;` 改成 `0f`（不外扩）⇒ 下面两条红。
                float pad50 = 50f * 300f / 1080f;
                CheckW(FindChild(ph, "Highlight"), 200f + 2f * pad50,
                       "★ `Highlight` 的宽 = 抽屉 200 + **每边外扩 50·k**（k = 300/1080 ⇒ 各 13.9）");
                CheckH(FindChild(ph, "Highlight"), 300f + 2f * pad50, "…高 = 300 + 2×13.9");
                // 🔴 颜色：两条都是**原版 `m_Color` 原文**（不是我们的选择）
                CheckNear(TintOf(FindChild(ph, "Highlight")).a, 0.8f, 0.01f,
                          "…`Highlight` 的 `m_Color` = 原版 `(1,1,1,0.8)`（**alpha 0.8** 那一半；写成 1 ⇒ 红）");
                CheckNear(AlphaOf(FindChild(ph, "Blackout")), 0.5529412f, 0.002f,
                          "★ …`Blackout` 的 `m_Color.a` = 原版 `0.5529412031173706`"
                          + "（改成 1 = 整格涂黑、看不出底下的图 ⇒ 红）");
                // 🔴 `Badge`：锚 `(0.0,0.7)-(0.3,1.0)` = **左上 30%×30%**、`m_PreserveAspect 1`
                var bdNode = FindChild(ph, "Badge");
                float prCx = prD != null ? PxOf(prD.position.x) : 0f;
                float prCy = prD != null ? PxYOf(prD.position.y) : 0f;
                CheckNearPx(bdNode, prCx - 70f, prCy - 105f,
                            "★ `Badge` 落在抽屉**左上 30%×30%** 那一格的中心"
                            + "（框 200×300 ⇒ 角标框 = 左上 60×90 ⇒ 中心 = 框心 +(−100+30, −150+45)）"
                            + " —— 做成右上/右下、或 `BadgeFrac` 改成 0.5 ⇒ 红");
                var bdQ = bdNode != null ? bdNode.GetComponentInChildren<ImageQuad>(true) : null;
                float bdW = bdQ != null ? bdQ.WorldW * 108f : 0f;
                float bdH = bdQ != null ? bdQ.WorldH * 108f : 0f;
                CheckNear(bdW, 200f * 0.3f, 2f,
                          "…角标的宽 = 框宽 × **0.3**（原版那对锚的比例）—— `BadgeFrac` 改成 0.5 ⇒ 红");
                CheckTrue(bdQ != null && bdQ.Texture != null && bdW > 0f
                          && Mathf.Abs(bdH / bdW - (float)bdQ.Texture.height / bdQ.Texture.width) < 0.02f,
                          $"★ …而且**按图自己的比例**放进那个 60×90 的框里（原版 `m_PreserveAspect = 1`；"
                          + $"实测 高/宽 = {(bdW > 0f ? bdH / bdW : 0f):F3}，贴图 高/宽 = "
                          + $"{(bdQ != null && bdQ.Texture != null && bdQ.Texture.width > 0 ? (float)bdQ.Texture.height / bdQ.Texture.width : 0f):F3}）"
                          + " —— 不管宽高比（拉伸填满 60×90）的实现这里得 1.5 ⇒ 红");
                // 🔴 层级：三件按原版**兄弟序** `Highlight` → `Blackout` → `Badge`（后者压前者）；
                //   整组还要**夹在**「物品自己的内容」与「整屏暗角」之间（⛔ 全用同一个队列 ⇒ 红；
                //   放到暗角之上 ⇒ 高亮/角标在整屏压暗里比周围亮一截，看得见的错）。
                int qIcon = QueueOf(FindChild(prD, "Icon"));
                int qHl = QueueOf(FindChild(ph, "Highlight"));
                int qBl = QueueOf(FindChild(ph, "Blackout"));
                int qBd = QueueOf(FindChild(ph, "Badge"));
                int qVig = QueueOf(FindChild(rwJ.transform, "Menu Vignette"));
                CheckTrue(qHl != int.MinValue && qHl < qBl && qBl < qBd,
                          $"★ 三件的层级按原版兄弟序（`Highlight` {qHl} → `Blackout` {qBl} → `Badge` {qBd}，后者压前者）"
                          + " —— 三件同一个队列（或次序反了）⇒ 红");
                CheckTrue(qIcon != int.MinValue && qVig != int.MinValue && qHl > qIcon && qBd < qVig,
                          $"★ …而且**夹在物品内容（{qIcon}）与整屏暗角（{qVig}）之间**（实得 {qHl} … {qBd}）"
                          + " —— 低于物品内容 ⇒ 被抽屉自己的图盖住；高于暗角 ⇒ 在整屏压暗里亮一截");

                // ---- 第 2 跳：限时条 ----
                var eph = FindChild(baD, "Ephemeral Drawer");
                CheckTrue(eph != null,
                          "★ 第 2 跳：`IsEphemeral` 那条 ⇒ 建出 `Ephemeral Drawer`（原版 `SetEphemeralDisplay`）");
                Check(cnt(prD, "Ephemeral Drawer") + cnt(cvD, "Ephemeral Drawer"), 0,
                      "★ …而高级档那格（`IsEphemeral` 假）与折算那格（**这一条两个判据都真**、但折算优先）"
                      + "**一个都没有** —— 不看判据、每格都建的实现 ⇒ 红");
                var ephPd = FindChild(eph, "Price Display");
                CheckTrue(ephPd != null, "…里面是 `Price Display`（原版那个挂 `PriceDisplay` 组件的节点名，逐字）");
                Check(ephPd != null ? ephPd.GetComponentsInChildren<ImageQuad>(true).Length : -1, 0,
                      "★ …而且这一行**只有一段字、一张图都没有**（原版那一跳传 `iconSprite: null` ⇒ "
                      + "`PriceDisplay.Setup` 把 `icon` 整颗关掉）—— 给这一行补个图标 ⇒ 红");
                string ephTxt = ephPd != null ? TextOf(ephPd) : null;
                CheckTrue(ephTxt != null && ephTxt.IndexOf("0h", System.StringComparison.Ordinal) < 0
                          && ephTxt.IndexOf("0m", System.StringComparison.Ordinal) < 0,
                          $"★ …那串里**没有 `0h` / `0m`**（原版方法体尾上那两句 `Replace`；实测「{ephTxt}」）"
                          + " —— 删掉 `Replace(\"0h\", \"\")` 的实现这里会画出 `2d 0h` ⇒ 红");
                Check((ephTxt ?? "").Trim(), "2d",
                      "…2 天整 ⇒ 画出来的就是 `2d`（`{0}d {1}h` 削掉 `0h` 那一支；⚠️ **格式串是我们挑的** —— "
                      + "六个词条 key 解出来了、正文在远端 CCD，`Replace` 那两句是原版的）"
                      + " —— 删掉那个 `Replace` ⇒ 这里实得 `2d 0h` ⇒ 红");
                // 竖向：条在抽屉的**下半**（原版锚 `(0.05,0.15)-(0.95,0.25)`，uGUI 的 y 自下往上
                // ⇒ 落到框的下半）—— 本工程 `PxRect.y1` 是**上沿**，所以「翻没翻」正是最容易错的一处。
                if (eph != null && baD != null)
                {
                    CheckNear(PxOf(eph.position.x), PxOf(baD.position.x), 1f,
                              "★ 限时条**横向居中**在框里（原版锚 0.05/0.95 对称 ⇒ 条心 = 框心）"
                              + " —— 那两个锚比例写成不等的值（或写成相对别的框）⇒ 红");
                    CheckNear(PxYOf(eph.position.y), PxYOf(baD.position.y) + 90f, 1.5f,
                              "★ …而且落在框**下半**：条心 y = 框心 **+90**（原版锚 0.15..0.25 是**自下往上**，"
                              + "换算过来 = `box.y2 − 0.20×300`；⚠️ 90 里那个 300 是**我们挑的**格子高；"
                              + "竖向没翻的实现这里得 **−90** ⇒ 红）");
                }

                // ---- 第 3 跳：折算条（`convertedInto != null`，与第 2 跳**互斥**）----
                var conv = FindChild(cvD, "Converted Drawer");
                CheckTrue(conv != null,
                          "★ 第 3 跳：`convertedInto != null` 那条 ⇒ 建出 `Converted Drawer`（原版 `SetConvertedItem`）");
                Check(cnt(prD, "Converted Drawer") + cnt(baD, "Converted Drawer"), 0,
                      "★ …另外两格没有（判据是 `convertedInto != null`）");
                Check(cnt(cvD, "Ephemeral Drawer"), 0,
                      "★★ …而这一格**没有** `Ephemeral Drawer` —— 夹具这一条**两个判据都真**"
                      + "（`convertedInto != null` 且 `IsEphemeral`），而原版那两句是 **if/else**"
                      + "（`convertedInto != null` 优先、② 只在**否则**那一支）⇒ 这一刻只该长折算那一条；"
                      + "写成「两个 `if` 各自判」的实现这里会同时长出两条 ⇒ 红");
                var convPd = FindChild(conv, "Price Display");
                Check(convPd != null ? TextOf(convPd) : null, "7",
                      "★ …那一行写的是 **折算项的数量 `7`**（原版 `Setup(convertedTo.Quantity.ToString(), 图)`）"
                      + " —— 写成 1（或写本格自己的数量）的实现 ⇒ 红");
                var convIcon = FindChild(convPd, "icon");
                Check(convIcon != null ? ArtOf(convIcon) : null,
                      CampaignData.ItemIcon("WildcardUltramarines2"),
                      "★ …`icon` 那格画的是 **折算那一项**的图（原版 `((ICurrency)convertedTo.Item).GetIcon(Small)`）"
                      + " —— 拿本格自己的图顶、或不建这一格 ⇒ 红");
                Check(FindChild(conv, "AlreadyOwned") != null ? TextOf(FindChild(conv, "AlreadyOwned")) : null,
                      ItemDrawer.AlreadyOwnedText,
                      "★ …条下面那句说明字 = 原版 prefab 里那串 `Already Owned`"
                      + "（词条 `MainMenu/RewardWindow/AlreadyOwned` 的**正文在远端 CCD** ⇒ 画兜底串，同 `TxtPremium` 口径）"
                      + " —— 少画这一句、或换成别的串 ⇒ 红");

                // ---- (h) **A311 第 4 跳**：`!Options.Stackable` 时按数量展开成 N 格 ----
                //   判据 = `RewardWindow__Open.c:133-147`：`options.stackable == 0 && reward.convertedInto == null`
                //   ⇒ 循环 `quantity − 1` 次、每次 `ItemDrawer.Draw(parent, item, **1**, Default)`
                //   （每格照样调第 1 跳、**不再**调第 2/3 跳）。
                //   `stackable` 那张表 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §②（SO 原始字节解出）：
                //   **20 条里只有 3 条是 0** —— `RawCardScript` / `PremiumItem` / `VIPPremiumItem`。
                //   ⚠️ 我们读不到「那个抽屉的 `options`」（`ItemDrawerStyle` 不表达它）⇒ 只能按**类型**查表；
                //   判据空时**返回「可堆叠」并出声**（猜「不可堆叠」会把格子数乘上 quantity，错得更明显）。
                //   🔴 **今天这条链在真数据上一次都不触发**（能走到 `Spec` 的 id 全都是 `stackable = 1`：
                //   `Booster Pack*` ⇒ `ShopContainer` / `DT Ultramarines*` ⇒ `DropTableItem` / `Wildcard*` ⇒ `Wildcard`）
                //   ⇒ 窗口层只能断**反例**，正例在库那一层直调（见下）。⛔ 这不是「不做」，是**如实标出覆盖面**。
                var stRaw4 = new ItemSpec { Id = "RawCardScript", Kind = ItemKind.Generic, Type = "RawCardScript" };
                var stPrem4 = new ItemSpec { Id = "PremiumItem", Kind = ItemKind.Generic, Type = "PremiumItem" };
                var stVip4 = new ItemSpec { Id = "VIPPremiumItem", Kind = ItemKind.Generic, Type = "VIPPremiumItem" };
                var stExpPrem4 = new ItemSpec { Id = "ExpansionPremiumItem", Kind = ItemKind.Generic, Type = "ExpansionPremiumItem" };
                var stWild4 = new ItemSpec { Id = "WildcardUltramarines1", Kind = ItemKind.Wildcard, Type = "Wildcard" };
                Check(ItemDrawer.IsStackable(stRaw4, DrawerOverride.Default), false,
                      "★ `RawCardScript` ⇒ **不可堆叠**（`options.stackable = 0`；那张表里只有 3 条是 0）");
                Check(ItemDrawer.IsStackable(stPrem4, DrawerOverride.Default), false, "★ `PremiumItem` ⇒ 不可堆叠");
                Check(ItemDrawer.IsStackable(stVip4, DrawerOverride.Default), false, "★ `VIPPremiumItem` ⇒ 不可堆叠");
                Check(ItemDrawer.IsStackable(stExpPrem4, DrawerOverride.Default), true,
                      "★★ …而 `ExpansionPremiumItem` ⇒ **可堆叠**（判据是**它自己那条 `options`**，它只是"
                      + " `PremiumItem` 的子类）—— 沿基类链上溯去继承祖宗那个 0 的实现这里 ⇒ 红");
                Check(ItemDrawer.IsStackable(stWild4, DrawerOverride.Default), true,
                      "★ `Wildcard` ⇒ 可堆叠（表里 17 条是 1 的那一侧）");
                Check(ItemDrawer.ExpandsByQuantity(stPrem4, DrawerOverride.Default), true,
                      "★ …展开的**极性**：不可堆叠 ⇒ `ExpandsByQuantity` 真（判据反了 ⇒ 可堆叠的那批全被展开 ⇒ 红）");
                Check(ItemDrawer.ExpandsByQuantity(stWild4, DrawerOverride.Default), false, "…可堆叠 ⇒ 不展开");
                // 判据空那一支：**返回「可堆叠」**（⛔ 不许猜「不可堆叠」）+ **必须出声**（`[ItemDrawer]` 警告）。
                // ⚠️ 出声那条用**全新的 id**（`ItemDrawer.Note` 是**按 key 去重**的 ⇒ 用一个跑过的 id 会一声不响）。
                int stackWarns = 0; string stackLast = "";
                Application.LogCallback hStack = (cond, st, type) =>
                {
                    if (type == LogType.Warning && cond != null && cond.Contains("[ItemDrawer]"))
                    { stackWarns++; stackLast = cond; }
                };
                var stNoType4 = ItemDrawer.Spec("A356-Probe-无类型", null, "无类型");
                bool stackNoType = false;
                Application.logMessageReceived += hStack;
                try { stackNoType = ItemDrawer.IsStackable(stNoType4, DrawerOverride.Default); }
                finally { Application.logMessageReceived -= hStack; }
                Check(stackNoType, true,
                      "★ 类型判据空（`C2`/32 位 hex 那批本地没有 SO）⇒ **按「可堆叠」处理**"
                      + "（猜「不可堆叠」会把格子数乘上 quantity ⇒ 错得更明显）");
                CheckTrue(stackWarns >= 1,
                          $"★ …而且**出声**：一条 `[ItemDrawer]` 警告（实测 {stackWarns} 条"
                          + (stackLast.Length > 0 ? "，末条：" + stackLast : "") + "）"
                          + " —— 判据空那一支改成静默返回 ⇒ 红");
                // 窗口层的**反例**：第 ④ 格 `quantity = 3` 但类型可堆叠 ⇒ **还是一格**（数量画成 `x3`）。
                Check(CountByPrefix(rwJ.ListHolder, "Item_"), 4,
                      "★（**第 4 跳的反例**）四条奖励 ⇒ **四个**抽屉节点"
                      + "（第 4 条 `quantity = 3` 但类型可堆叠 ⇒ **不展开**；"
                      + "无条件按数量展开的实现这里得 **6** ⇒ 红）");
                var fourD = FindChild(rwJ.ListHolder, "Item_Booster Pack Legendary Ultramarines");
                Check(fourD != null ? TextOf(FindChild(fourD, ItemDrawer.NodeQuantity)) : null, "x3",
                      "…而且那一格的数量就画成 **x3**（不展开 ⇒ 数量留在同一格上）");
                wm2.CloseAllWindows();
            }

            // ---- (i) 🆕 **A355：`graphic == null` 那一支** ----------------------------------------------
            //   判据 = `BlinkGraphic__Update.c:20-30`：推进那句**写在 `if (graphic != null)` 之内** ——
            //   空那一支走 `FUN_1803f47a0()`（**抛 NRE**，反编译标着 `/* WARNING: Subroutine does not return */`）
            //   ⇒ **`currentTime` 一次都不会走**。我们**不抛**（批处理里抛一下整条自检就没了），
            //   但**同样不推时钟**（另加一条只响一次的 `[RewardWindow]` 警告 —— 不许静默）。
            //   ⚠️ 夹具是**人工造的**：`Build()` 里 `Text(...)` 无条件建那颗字 ⇒ 正常路径造不出
            //   `_tapLabel == null`；用 `DestroyImmediate` 把那颗 `Tap Text` 拿掉 == 原版 `graphic` 是空引用那一刻。
            //   改坏法：把 `Shell/RewardWindow.cs` 的 `_blinkT += dt;` 挪回判空**之前** ⇒ 那条红（实测 1.0）。
            {
                var rwN = RewardWindow.Create(wm2);
                wm2.OpenWindow(rwN, new RewardWindowContext { Rewards = new[] { oneSpec }, IsPreview = false });
                var tapTxtN = FindChild(rwN.transform, "Tap Text");
                CheckTrue(tapTxtN != null, "（前提）这一扇窗的 `Tap Text` 建出来了（不在 ⇒ 下面那条等于没查）");
                rwN.Tick(0.5f);
                CheckNear(rwN.BlinkClock, 0.5f, 1e-4f,
                          "（对照组）标签在时 `Tick(0.5)` ⇒ 时钟 = **0.5**（照原版每帧都推）");
                if (tapTxtN != null) Object.DestroyImmediate(tapTxtN.gameObject);
                rwN.Tick(0.5f);
                CheckNear(rwN.BlinkClock, 0.5f, 1e-4f,
                          "★★ `Tap Text` 不在了（= 原版 `graphic == null`）⇒ **时钟一步都不走**（停在 0.5）"
                          + "（原版那一支抛 NRE ⇒ 那句 `currentTime += deltaTime` 压根执行不到）"
                          + " —— 先推时钟、再判空的实现这里是 **1.0** ⇒ 红");
            }

            wm2.CloseAllWindows();
        }
        wm2.CloseAllWindows();

        // ============================================================ 🆕 **2026-10-13（WD4 · Daily 一族六件）**
        // **A495**（生产路径「先收再关」 / 程序性关窗「只关不收」）· **A496+A498**（A479 的**正例**：
        // 关窗 ⇒ 真收到了）· **A509**（连登两个读口在「可领那一格」上互为补）·
        // **A510**（`RewardWindow.Build()` 不许涨滚动登记）· **A643**（两窗共用同一条 'More Rewards In' 词条）。
        // 🔴 **为什么插在 §九 之后、A479/A481 那一节之前**：本节每一组都要**显式造出「可领那一格」那一态**
        //    （靠 2026-10-13 新加的 `DailyData.ForceRewardClaimedForTest` —— A498 的裁定），而 §九(a) 那条既有覆盖
        //    （真点抽屉 ⇒ 收掉唯一那一格）**一个字都不能动**；本节收工**显式把那一位拨回「已领」**
        //    ⇒ 紧接着的 A479⑦ 那条前提（「这一刻一格里没有可领的」）**逐字照旧**。
        // ⚠️ 本节**只在 §九 末尾与 A479 那一节之间新增**、**没改任何既有语句**（同文件上面有五段别人的改动）。
        Section("🆕 WD4 · Daily 一族六件（A495 生产/程序性关窗 · A496+A498 正例 · A509 读口 · A510 滚动登记 · A643 词条）");
        {
            // ---- (0) 前提：**显式造出「可领那一格」这一态**（A498 的写口）+ 写口本身的两态 ----
            //   🔴 **为什么必须先拨回**：§九(a) 那条既有覆盖**真点过那唯一一格**（⇒ `_rewardClaimed[2] = true`）
            //      ⇒ 进入本节时**一格里都没有 `Unlocked`**（这正是紧接着 A479⑦ 那条前提）。
            //      本节要验的**正例**（关窗 ⇒ 真收到了）必须有「可领」那一态 ⇒ 用新写口显式造 ——
            //      这就是 A498 那个口存在的**全部理由**（没有它，那一格被 §九(a) 收掉之后就再也回不去）。
            var claimedWas = new bool[DailyData.RewardDays];
            for (int i = 0; i < DailyData.RewardDays; i++)
                claimedWas[i] = DailyData.RewardStateOf(i, false) == RewardState.Collected;   // 起点快照（收工要还原）
            for (int i = 0; i < DailyData.RewardDays; i++) DailyData.ForceRewardClaimedForTest(i, false);
            int dClaim = -1;
            for (int i = 0; i < DailyData.RewardDays; i++)
                if (DailyData.RewardStateOf(i, false) == RewardState.Unlocked) { dClaim = i; break; }
            CheckTrue(dClaim >= 0,
                      "（前提）拨回「没领过」之后找得到一个 `Unlocked` 的每日奖励抽屉（`RewardsCollected = 2` ·"
                      + " `RewardCurrentValue = 3` · `_rewardTarget = {1,2,3,4}` ⇒ 全自检只有下标 2 那一格进得了这一态）"
                      + " —— 找不到 ⇒ 下面整段等于没验");
            string claimArt = dClaim >= 0 ? DailyData.RewardIconOf(dClaim, false) : null;
            int claimAmt = dClaim >= 0 ? DailyData.RewardAmount(dClaim, false) : -1;
            CheckTrue(claimArt != null && claimAmt > 0,
                      "（前提）那一格的图名 / 数量都取得出来（取不到 ⇒ 下面「+N」「一份都没再发」那几条会空转成恒真）");
            DailyData.ForceRewardClaimedForTest(dClaim, true);
            Check(DailyData.RewardStateOf(dClaim, false), RewardState.Collected,
                  "★ A498：新写口置 `true` ⇒ 那一格 `Collected`（⛔ 不是「写了个空」）");
            DailyData.ForceRewardClaimedForTest(dClaim, false);
            Check(DailyData.RewardStateOf(dClaim, false), RewardState.Unlocked,
                  "★★ A498：置回 `false` ⇒ 那一格**又回 `Unlocked`**（**两态分得开** —— 这一条就是 H42 §三"
                  + " 那两条落法都撞红的那个卡点：没有写口，那一格被 §九(a) 收掉之后就再也回不去）"
                  + "；改坏法：把这个口写成空函数（或让它去写连登轨那个数组）⇒ 本条红");

            // ---- A509：连登轨那两个读口在「可领那一格」上**互为补**（两态对照）----
            int dStreak = DailyData.StreakClaimableDay;                    // = `StreakCollected`
            bool streakWas = DailyData.StreakRewardClaimed(dStreak);       // 起点（本节收工要还原）
            DailyData.ForceStreakClaimedForTest(dStreak, false);
            Check(DailyData.StreakRewardUnlocked(dStreak), true,
                  "★ A509 态一：置成「没领过」⇒ 守卫说**领得到**（`StreakRewardUnlocked`）");
            Check(DailyData.StreakRewardClaimed(dStreak), false,
                  "★★ A509：…而「已领」那个读口必须**同时**说假 —— 两个读口问的是**同一份布尔**。"
                  + "读口原来写成 `_streakClaimed[k] || k < StreakCollected` 那个「或」：`k < StreakCollected` 那一半"
                  + "会把这份布尔**整段吞掉**（今天恰好 `StreakCollected = 5` ⇒ 看不出来），`StreakCollected` 一改就"
                  + "反过来咬人（H45 §四·1 点出的那一档）；改坏法：把读口退回只问位置那一半 ⇒ 本条红");
            DailyData.ForceStreakClaimedForTest(dStreak, true);
            Check(DailyData.StreakRewardUnlocked(dStreak), false,
                  "★ A509 态二：置成「领过」⇒ 守卫说**领不到**（与态一合起来 = 两态分得开）");
            Check(DailyData.StreakRewardClaimed(dStreak), true,
                  "★ A509：…而「已领」读口说**真**（两读口一起翻面 —— 恒真 / 恒假的实现这里各红一条）");
            DailyData.ForceStreakClaimedForTest(dStreak, streakWas);       // 还原（别把残留留给后面的节）

            // ---- A510：`RewardWindow.Build()` 重建 ⇒ `PointerLayer` 的滚动登记**一条都不许涨** ----
            var rwReg = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwReg, new RewardWindowContext
            { Rewards = new[] { new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierBasic) } });
            // ⚠️ **先重建一次、再取基线**：`RegisterScroll` 自带 `PruneScrolls()`（顺手清死条目）
            //    ⇒ 让**第一次**重建把待清的账吸掉，基线之后那一次才只反映「本窗重建涨不涨」。
            rwReg.Build();
            int nScroll0 = PointerLayer.ScrollCountForTest;
            rwReg.Build();
            Check(PointerLayer.ScrollCountForTest, nScroll0,
                  $"★★ A510：重开 `Build()` ⇒ 登记表**一条都不涨**（{nScroll0} → {PointerLayer.ScrollCountForTest}）"
                  + " —— `MenuScroll` 是**普通 class**（`== null` 恒假）而 `Owner` 是窗根（重建时不会死）"
                  + "⇒ 光靠 `PruneScrolls()` 清不掉旧条目（旧条目**仍会被滚轮命中**、`OnChanged` 指向已销毁的节点）；"
                  + "改坏法：删掉 `Shell/RewardWindow.cs` 的 `Build()` 里那句 `PointerLayer.UnregisterOwnedBy(gameObject);`"
                  + " ⇒ 每建一次涨一条 ⇒ 本条红");
            rwReg.Close();

            // ---- A495（程序性那一半）：`CloseAllWindows()` **只关不收**（原版程序性关窗走 `Hide()`、不经虚表 `Close()`）----
            RewardWindowFixture.DismissRewardWindows();
            DailyData.ForceRewardClaimedForTest(dClaim, false);            // 显式造出「可领」那一态
            var drProg = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(drProg);
            CheckTrue(RewardWindowFixture.OpenRewardWindowCount() == 0, "（前提）开这扇窗本身**不弹**领奖窗");
            int wProg = Wallet.Of(claimArt);
            wm2.CloseAllWindows();                                         // = 「程序性收口」那一圈（逐扇调虚方法 `Close()`）
            Check(drProg.CurrentState, WindowState.Closed, "★ A495：程序性收口**真的关掉了**那扇窗");
            Check(DailyData.RewardStateOf(dClaim, false), RewardState.Unlocked,
                  "★★ A495：…而**那一格没被收**（`CloseAllWindows()` 不许触发「关窗自动收」）"
                  + " —— 原版程序性关窗走的是 `Hide()`（`Close → manager.CloseWindow → CloseWindowCO` 先调 Slot 9）、"
                  + "**不经**虚表 `Close()` ⇒ 把「收」覆写进 `Close()` 的实现这里红"
                  + "（那正是裁定里「另给一个只关不收的口」的由来）");
            Check(Wallet.Of(claimArt), wProg, "★★ A495：…一份都没发（配上面那条 = 两态分得开：ESC 收 / 程序性不收）");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ A495：…也没弹领奖窗");

            // ---- A495（生产那一半 · ESC 真路径）：`PointerLayer.KeyCancel()` ⇒ **先收再关** ----
            RewardWindowFixture.DismissRewardWindows();
            DailyData.ForceRewardClaimedForTest(dClaim, false);            // 再造一次「可领」（上一条刚验过它没被收）
            var drEsc = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(drEsc);
            Check(WindowsManager.Instance, wm2,
                  "（前提）全局那台 `WindowsManager.Instance` 就是本夹具这台 —— `PointerLayer.KeyCancel` 打的是**全局**那台的顶窗");
            Check(wm2.TopWindow, drEsc, "（前提）这一刻顶窗就是这扇每日奖励窗（ESC 只打顶窗）");
            int wEsc = Wallet.Of(claimArt);
            var plEsc = PointerLayer.Instance;                              // ⛔ 别裸调：指针层取不到时先红一条、别 NRE
            bool escClosed = plEsc != null && plEsc.KeyCancel();
            CheckTrue(escClosed, "★ A495：ESC 打在这扇窗上 ⇒ **真的关掉了**（`GameWindow.ESCPressed` 两道门槛过）");
            Check(drEsc.CurrentState, WindowState.Closed, "★ …它进了 `Closed` 态");
            Check(DailyData.RewardStateOf(dClaim, false), RewardState.Collected,
                  "★★ A495：**ESC 关窗 ⇒ 那一格真收到了**（原版 `DailyRewardPopup.Close()` ="
                  + " `LiveOp.TryCollect(() => base.Close())` —— 关窗与点奖励在那边是**同一个调用**）"
                  + "；改坏法：删掉 `Shell/DailyRewardPopup.cs` 的 `ESCPressed()` 覆写里那句 `DailyRewardAutoCollect()`"
                  + " ⇒ 本条红（那正是 A495 落地前的老行为：ESC 关窗**不收**，而且不出声）");
            Check(Wallet.Of(claimArt), wEsc + claimAmt,
                  $"★★ A495：…而且真记进 `Wallet` 了（+{claimAmt}）—— 转调被换成自己写一份 `Wallet.Grant` 且数量抄错 ⇒ 红");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 1, "★ A495：…而且弹的是那扇领奖窗");
            RewardWindowFixture.DismissRewardWindows();

            // ---- A496（= H38 §六 ①–⑥ 的**正例**）：返回钮 ⇒ 真收到 ----
            DailyData.ForceRewardClaimedForTest(dClaim, false);            // ① 前提：那一格「可领」
            var drPos = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(drPos);
            var posBack = FindChild(FindChild(FindChild(drPos.transform, "Tracks Side Bar"),
                                             "Generic Round Button Variant"), "Image");
            var posWb = posBack != null ? posBack.GetComponent<WindowButton>() : null;
            CheckTrue(posWb != null && posWb.onClick != null,
                      "★ A496①：（前提）左下那颗返回圆钮（`Tracks Side Bar/Generic Round Button Variant/Image`）"
                      + "挂得到 `WindowButton` 且 `onClick` 非空");
            int wPos0 = Wallet.Of(claimArt);
            CheckTrue(ClickButtonByQuad(PointerLayer.Instance, posBack, posWb, "每日奖励窗的返回钮"),
                      "★ A496②：**真点**那颗返回钮（走 `PointerLayer` 真路径 —— ⛔ 不是直调 `onClick`）");
            Check(drPos.CurrentState, WindowState.Closed, "★ …那一下真的把窗关掉了");
            Check(DailyData.RewardStateOf(dClaim, false), RewardState.Collected,
                  "★★ A496③：**关窗 ⇒ 那一格真收到了**；改坏法：把 `CloseCollecting()` 里那句 `DailyRewardAutoCollect()`"
                  + " 删掉（退回裸 `Close()`）⇒ 本条红");
            Check(Wallet.Of(claimArt), wPos0 + claimAmt, "★★ A496④：…而且真记进 `Wallet` 了（+N）");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 1,
                  "★ A496⑤：…而且弹的是那扇领奖窗（只置位不发奖的实现这里红）");
            RewardWindowFixture.DismissRewardWindows();

            // ---- A496⑥ 负例：**已领过**再关一次 ⇒ 一份都没再发（守卫走的是 `CollectReward` 那一份）----
            //   ⚠️ **订正 H38 §六 ⑥ 的一句措辞（铁律 5）**：它写「把那一格再置回 `Unlocked`」，而它自己给的两条
            //   期望值（`RewardStateOf == Collected` + `Wallet` 差 0）**只有「已领过」那一态**才成立
            //   （`Unlocked` 那一态再关一次本来就该**再发一份**）⇒ 照**期望值**落地：显式置成「已领」。
            DailyData.ForceRewardClaimedForTest(dClaim, true);
            var drNeg = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(drNeg);
            var negBack = FindChild(FindChild(FindChild(drNeg.transform, "Tracks Side Bar"),
                                             "Generic Round Button Variant"), "Image");
            var negWb = negBack != null ? negBack.GetComponent<WindowButton>() : null;
            int wNeg0 = Wallet.Of(claimArt);
            CheckTrue(negWb != null && ClickButtonByQuad(PointerLayer.Instance, negBack, negWb,
                                                         "每日奖励窗的返回钮（已领过那一档）"),
                      "★ A496⑥：（前提）已领过那一档，返回钮照样点得动");
            Check(drNeg.CurrentState, WindowState.Closed, "★ …窗照样关得掉");
            Check(DailyData.RewardStateOf(dClaim, false), RewardState.Collected, "★ A496⑥：那一格**仍是 `Collected`**");
            Check(Wallet.Of(claimArt), wNeg0,
                  "★★ A496⑥：**一份都没再发**（守卫走的是 `CollectReward` 那一份 —— 恒收的实现这里红）");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ …也没再弹领奖窗");
            wm2.CloseAllWindows();

            // ---- A643：两窗的 'More Rewards In' 是**同一条词条**（中性口）----
            //   ⛔ 期望值是**手写字面量**（不读 `DailyData.MoreRewardsInText()` —— 那等于拿被测实现证明自己）。
            var drTxt = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(drTxt);
            var dsTxt = DailyStreakPopup.Create(wm2);
            wm2.OpenWindow(dsTxt);
            string moreTxt = TextOf(FindPath(drTxt.transform, "Timer/EverguildTextMeshPro"));
            string nxtTxt = TextOf(FindPath(dsTxt.transform, "Streak Successful/Timer/Next Rewards text"));
            Check(moreTxt, "More Rewards In",
                  "★ A643：奖励窗那颗（`Timer/EverguildTextMeshPro`）写的 = 原版那条词条的字面量");
            Check(nxtTxt, "More Rewards In", "★ A643：连登窗那颗（`Streak Successful/Timer/Next Rewards text`）同一条字面量");
            Check(moreTxt, nxtTxt,
                  "★★ A643：**两窗逐字相同**（原版两窗共用同一条 I2 词条：连登窗 prefab 是英文 `'More Rewards In'`、"
                  + "奖励窗那份落到了 es `'Más Recompensas En'`）；改坏法：任一窗改回自己写一份字面量"
                  + "（换个大小写 / 差一个字母）⇒ 本条红；把中性口的返回值改掉 ⇒ 上面那两条手写字面量红");
            wm2.CloseAllWindows();

            // ---- 收工还原：按**起点快照**逐格拨回（= §九(a) 之后的态）⇒ 紧接着的 A479⑦ 那条前提逐字照旧 ----
            for (int i = 0; i < DailyData.RewardDays; i++)
                DailyData.ForceRewardClaimedForTest(i, claimedWas[i]);
            bool anyUnlockedAfter = false;
            for (int i = 0; i < DailyData.RewardDays; i++)
                if (DailyData.RewardStateOf(i, false) == RewardState.Unlocked) anyUnlockedAfter = true;
            Check(anyUnlockedAfter, false,
                  "（收工还原）**一格里都没有可领的**了（= 本节起点那一态）—— 下一节（A479⑦）的前提就建在这上面，"
                  + "⛔ 别省这一步（少了它，A479⑦ 那条「（前提）一格里没有可领的」当场红）");
            RewardWindowFixture.DismissRewardWindows();
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "（收工还原）场上没有遗留的领奖窗");
            wm2.CloseAllWindows();
        }

        // ============================================================ 🆕 **2026-10-13（A496 · 落 H38 §六）**
        // **每日奖励窗的返回钮（关窗那一拍 = `DailyRewardAutoCollect`）+ `Reward Claim` 六套粒子（A481）**。
        Section("🆕 A479/A481：每日奖励窗的**返回钮**（关窗那一拍）+ `Reward Claim` 六套粒子（H38 §六）");
        {
            // ---- A479⑦ 关窗那一拍：**没有可领格**时 ⇒ 什么都不发生（而「返回钮真的关得掉窗」这条本身有牙）----
            //   🔴 **为什么只剩负例**（如实记，⛔ 不是漏做）：本自检**全过程只有一格**进得了 `Unlocked`
            //   （`Shell/DailyData.cs`：`RewardDays = 4` · `RewardsCollected = 2` ⇒ 只有下标 `2` 那一格，
            //   它的 `_rewardTarget[2] = 3 ≤ RewardCurrentValue = 3`；下标 3 的 target 是 4 ⇒ 恒 `Locked`）；
            //   而 §九(a)（A309 的既有覆盖：点抽屉 ⇒ 弹领奖窗）**已经把它收掉了**，且 `_rewardClaimed[]`
            //   **没有写口**（`DailyData` 文件私有；H38 §⑭ 明说不加、也明说⛔别拿连登轨那个
            //   `ForceStreakClaimedForTest` 顶替）⇒ **那一格一旦被收就回不去**。
            //   ⇒ A479 的**正例**（②「关窗 ⇒ 真收到了」· ③ 置位 · ④ 记账 · ⑤ 弹窗）**落不进来**：
            //     排在 §九(a) **之前** ⇒ §九(a) 那条前提（`CheckTrue(cHit != null, …)`）当场红；
            //     排在**之后** ⇒ 那一刻已经没有可领的格（正是下面这一段要断的负例）。
            //   📌 **要补正例**需要二选一（都**不在本件白名单**：一个动 `Shell/`、一个动既有的 §九(a)），
            //     留给调度台裁：① 给 `Shell/DailyData.cs` 加 `ForceRewardClaimedForTest(int day, bool claimed)`
            //     （照连登轨 `ForceStreakClaimedForTest` 的形状）⇒ 「收完再拨回 `Unlocked`」，
            //     两条路就能并存；② 或让 §九(a) 不真点，把那一格让给 A479 的正例。
            var drAuto = DailyRewardPopup.Create(wm2);
            wm2.OpenWindow(drAuto);
            bool anyUnlocked = false;
            for (int i = 0; i < DailyData.RewardDays; i++)
                if (DailyData.RewardStateOf(i, false) == RewardState.Unlocked) anyUnlocked = true;
            Check(anyUnlocked, false,
                  "★ A479⑦：（前提）这一刻**一格里没有可领的**（= §九(a) 已经把唯一那一格收掉了）"
                  + " —— 不成立 ⇒ 下面那几条等于没查（那时要做的是**正例**，见本块头注释）");
            var drawArts = new List<string>();
            var drawBefore = new List<int>();
            for (int i = 0; i < DailyData.RewardDays; i++)
            {
                string a = DailyData.RewardIconOf(i, false);
                if (a != null && !drawArts.Contains(a)) { drawArts.Add(a); drawBefore.Add(Wallet.Of(a)); }
            }
            CheckTrue(drawArts.Count > 0, "★ A479⑦：（前提）记到了至少一条奖励图标（一条都没有 ⇒ 下面那条空转）");
            RewardWindowFixture.DismissRewardWindows();
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ A479⑦：（前提）场上没有遗留的领奖窗");

            var backNode = FindChild(FindChild(FindChild(drAuto.transform, "Tracks Side Bar"),
                                               "Generic Round Button Variant"), "Image");
            var backWb = backNode != null ? backNode.GetComponent<WindowButton>() : null;
            CheckTrue(backWb != null && backWb.onClick != null,
                      "★ A479：（前提）左下那颗返回圆钮（`Tracks Side Bar/Generic Round Button Variant/Image`）"
                      + "挂得到 `WindowButton` 且 `onClick` 非空");
            CheckTrue(ClickButtonByQuad(PointerLayer.Instance, backNode, backWb, "每日奖励窗的返回钮"),
                      "★ A479：**真点**那颗返回钮（走 `PointerLayer` 真路径 —— ⛔ 不是直调 `onClick`）");
            Check(drAuto.CurrentState, WindowState.Closed,
                  "★★ A479：那一下**真的把窗关掉了**"
                  + "（接线断了 / 命中区被别的件盖住 ⇒ 红；`ClickButtonByQuad` 自己那条前提也会先报出来）");
            string drewArt = "<没变>";
            for (int i = 0; i < drawArts.Count; i++)
                if (Wallet.Of(drawArts[i]) != drawBefore[i]) drewArt = drawArts[i];
            Check(drewArt, "<没变>",
                  "★★ A479⑦：没有可领的格时点返回钮 ⇒ **一份都没再发**"
                  + "（`DailyRewardAutoCollect` 走的是 `RewardStateOf == Unlocked` 那道守卫 ⇒ 恒收的实现这里红；"
                  + "红了会印出**被多发的那张图**）");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                  "★★ A479⑦：…而且**一扇领奖窗都没弹**（= 原版 `FirstOrDefault` 取不到那一支：直接走续作 `onComplete()`）");
            Check(DailyData.DailyRewardAutoCollect(), false,
                  "★ A479⑦：直调那个出口也回 `false`（与上面真路径**同一份守卫** —— 两处共用一条规则，⛔ 别各写一份）");
            drAuto.Close();

            // ---- ⑧⑨⑩⑪⑬ A481：`Reward Claim` 六套粒子（两扇窗**共用一份**实现 `RewardClaimFx`）----
            //   判据 = H29 §五 那三张表（H38 逐列转写进 `Shell/RewardWindow.cs` 的 `RewardClaimFx`）——
            //   ⛔ **期望值一律手写字面量**，⛔ 不读 `RewardClaimFx.OrigScale` / `OrigLocalY` / `Spec` 那一族
            //   （那是**被测实现里的数** = 自证：把它们改错这一条会跟着变绿）。
            var fxSpec = new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierBasic);
            var rwFx = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwFx, new RewardWindowContext { Rewards = new[] { fxSpec }, IsPreview = false });
            var rwFxP = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwFxP, new RewardWindowContext { Rewards = new[] { fxSpec }, IsPreview = true });
            CheckTrue(rwFx.ClaimFx != null && rwFxP.ClaimFx != null,
                      "★ A496⑧：（前提）两扇窗的 `ClaimFx` 都建出来了（`null` ⇒ 那一扇没接线）");
            Check(RewardClaimFx.ParticleCount(rwFx.ClaimFx), 6,
                  "★★ A496⑧：`Reward Claim` 子树 = **6 颗粒子**"
                  + "（原版那 6 套：`Wave left` · `Wave Shine` · `Trails` · `Wave right` · `Wave Shine` · `Trails`）"
                  + " —— 少建一套 ⇒ 红");
            CheckTrue(rwFx.ClaimFx.transform.Find("Wave left/Wave right/Trails") != null,
                      "★★ A496⑧：形状 —— **`Wave right` 是 `Wave left` 的子件**（原版 `m_Children` 就这个形状）"
                      + " —— 把 `Wave right` 建成 `Reward Claim` 的**直接子件**（= 按派单那句错描述写）⇒ 红");

            // ---- ⑨ 预览 × 非预览两态 ----
            //   ⚠️ **2026-10-17 更正（铁律 5 · A827 残留）**：这里原来写「🔴 今天素材未导 ⇒ `Ready == false`
            //   （6 张贴图还不在 `Resources/Art/ui_menu/`，见 H38 §七·1）」—— **已过期**（与下面那条 `A496⑨（非预览）`
            //   断言里的 **2026-10-16 订正**是**同一个口径**；⛔ 这里不写行号 —— 本文件这一族指针就是被行号漂坏过）：
            //   `Reward Claim` 那 **6 张 PNG（`Laser_Wave_2` / `Shine_trail` / `Glow` / `LightningTrail` /
            //   `Up_Rays` / `Noise_Combined`）都在 `Resources/Art/ui_menu/` 且都已导入 · 两支 shader
            //   （`Everguild/FX/Extra Color` · `Everguild/FX/Unlit UV scroll`）也都在随包 `wf_shaders.bundle` 里**
            //   （2026-10-17 现读）⇒ `RewardClaimFx.Ready`（= 5 份材质建齐）**已是 true**。
            //   【以下半句是**当时**的留痕】`SetVisible` 那道 `&& built.Ready` 把那时的两态**都**压成「关」
            //   ⇒ 那时直接断「非预览 ⇒ 开」会是**假红**
            //   （那不是缺陷，是已知的素材缺口）。⇒ 这里断的是**两态之间的关系**：
            //     · 非预览 ⇒ 「开没开 = **材质齐不齐**」（= 原版 `!IsPreview` 那一半；素材一到位 `Ready` 变真，
            //       这一句**自动升级**成「非预览 ⇒ 开」的真断言 —— ⛔ 别改成恒真）；
            //     · 预览   ⇒ **恒关**（与材质齐不齐无关 —— 原版那另一半）。
            CheckTrue(rwFx.ClaimFxVisible == RewardClaimFx.Ready(rwFx.ClaimFx),
                      "★★ A496⑨（非预览）：开没开 = **材质齐不齐**"
                      + "（原版 = `SetActive(claimRewardParticles, !IsPreview)`；⚠️ **2026-10-16 就地订正（铁律 5）**："
                      + "原文「今天素材未导 ⇒ `Ready = false` ⇒ 关」**已过期** —— 6 张 PNG 都在 `Resources/Art/ui_menu/`、"
                      + "`RewardAppearParticle.prefab` 在 `Assets/WarpforgeVFX/Prefabs/` 且已登记进效果库 ⇒ `Ready` 多半已是 true、"
                      + "这一句自动升级成「非预览 ⇒ 开」；断的是**两态之间的关系**、两态都过）"
                      + $"；实得 `ClaimFxVisible={rwFx.ClaimFxVisible}` · `Ready={RewardClaimFx.Ready(rwFx.ClaimFx)}`"
                      + " —— 删掉 `Shell/RewardWindow.cs` 那句 `SetVisible(_claimFx, !ctx.IsPreview)`、"
                      + "或把 `SetVisible` 里的 `&& built.Ready` 删掉 ⇒ 红");
            Check(rwFxP.ClaimFxVisible, false,
                  "★★ A496⑨（预览）：`IsPreview = true` ⇒ **恒关**（原版那一半 `!IsPreview`；恒开的实现 ⇒ 红）");

            // ---- ⑩ TRS（量纲那一处的判据）----
            CheckNear(rwFx.ClaimFx.transform.localPosition.y, LayoutSpace.Px(-32f), 1e-6f,
                      "★★ A496⑩：`Reward Claim` 根的 `localPosition.y` = 原版 **−32**（该父链 1 单位 = 1 px）"
                      + " —— ⛔ 期望值是**手写字面量**（不读 `RewardClaimFx.OrigLocalY`）");
            CheckNear(rwFx.ClaimFx.transform.localScale.x, LayoutSpace.Px(684.3304443359375f), 1e-6f,
                      "★★ A496⑩：`localScale.x` = 原版 **684.3304443359375**"
                      + "（漏除 108 ⇒ 差 108 倍；除两遍 ⇒ 差 108² ⇒ 红）");

            // ---- ⑪ 逐字段抽 4 条（挑最容易被抄错的那几列）----
            var fxRoot = rwFx.ClaimFx.transform;
            var fxWaveL = fxRoot.Find("Wave left");
            var fxTrailL = fxRoot.Find("Wave left/Trails");
            var fxTrailR = fxRoot.Find("Wave left/Wave right/Trails");
            var fxShineL = fxRoot.Find("Wave left/Wave Shine");
            var fxShineR = fxRoot.Find("Wave left/Wave right/Wave Shine");
            CheckTrue(fxWaveL != null && fxTrailL != null && fxTrailR != null
                      && fxShineL != null && fxShineR != null,
                      "★ A496⑪：（前提）那五个节点都在（少了 ⇒ 下面几条会空转成恒真）");
            var fxPrL = fxTrailL != null ? fxTrailL.GetComponent<ParticleSystemRenderer>() : null;
            var fxPrR = fxTrailR != null ? fxTrailR.GetComponent<ParticleSystemRenderer>() : null;
            Check(fxPrL != null ? (int)fxPrL.renderMode : -1, 5,
                  "★★ A496⑪：两颗 `Trails` 的 `m_RenderMode = **5 (None)**` —— **只画拖尾、不画本体**"
                  + "（真正被画的是 `trailMaterial` = `m_Materials[1]`）—— 写成 0(Billboard) ⇒ 多画一层本体 ⇒ 红");
            Check(fxPrR != null ? (int)fxPrR.renderMode : -1, 5, "★★ A496⑪：…第二颗同（右轨那一条）");
            var fxPsSL = fxShineL != null ? fxShineL.GetComponent<ParticleSystem>() : null;
            var fxPsSR = fxShineR != null ? fxShineR.GetComponent<ParticleSystem>() : null;
            Check(fxPsSL != null && fxPsSL.trails.enabled, false,
                  "★★ A496⑪：两个 `Wave Shine` 的 **`TrailModule.enabled = False`**（表 2 那一列）"
                  + " —— 抄成 true ⇒ 多画一层拖尾 ⇒ 红");
            Check(fxPsSR != null && fxPsSR.trails.enabled, false, "★★ A496⑪：…第二颗同");
            var fxPsWL = fxWaveL != null ? fxWaveL.GetComponent<ParticleSystem>() : null;
            var fxPsTL = fxTrailL != null ? fxTrailL.GetComponent<ParticleSystem>() : null;
            var fxPsTR = fxTrailR != null ? fxTrailR.GetComponent<ParticleSystem>() : null;
            CheckNear(fxPsWL != null ? fxPsWL.main.duration : -1f, 2f, 1e-4f,
                      "★★ A496⑪：`Wave left` 的 `lengthInSec` = **2**（表 1 那一列）");
            CheckNear(fxPsTL != null ? fxPsTL.main.duration : -1f, .25f, 1e-4f,
                      "★★ A496⑪：两颗 `Trails` 的 `lengthInSec` = **0.25**（与 `Wave` 那四件**不是**同一个数）");
            CheckNear(fxPsTR != null ? fxPsTR.main.duration : -1f, .25f, 1e-4f, "★★ A496⑪：…第二颗同");
            var fxVL = fxPsTL != null ? fxPsTL.velocityOverLifetime : default(ParticleSystem.VelocityOverLifetimeModule);
            CheckNear(fxPsTL != null ? fxVL.x.constantMin : float.NaN, -4f, 1e-4f,
                      "★★ A496⑪：`Wave left/Trails` 的 `velocityOverLifetime.x.constantMin = **−4**`（左轨朝左）");
            CheckNear(fxPsTL != null ? fxVL.x.constantMax : float.NaN, -2f, 1e-4f, "★★ A496⑪：…`constantMax = −2`");
            var fxVR = fxPsTR != null ? fxPsTR.velocityOverLifetime : default(ParticleSystem.VelocityOverLifetimeModule);
            CheckNear(fxPsTR != null ? fxVR.x.constantMin : float.NaN, 2f, 1e-4f,
                      "★★ A496⑪：`Wave right/Trails` 的 `x.constantMin = **+2**`（右轨朝右）");
            CheckNear(fxPsTR != null ? fxVR.x.constantMax : float.NaN, 4f, 1e-4f,
                      "★★ A496⑪：…`constantMax = +4`（**左右轨抄反 ⇒ 红**）");

            // ---- ⑬ 素材缺时**不许静默**、更**不许硬开** ----
            //   🔴 **为什么这一条必须有**：`SetVisible` 里那道 `&& built.Ready` 是唯一拦住「拿没材质的粒子硬开
            //   （画面白块）」的闸 —— 把闸删掉**立刻**能看见。
            //   ✅ **2026-10-16 就地订正（铁律 5）**：原文写「今天正好是 `Ready == false` 那一档」**已过期**
            //   （素材已进工程）；它断的仍是**关系式**，两态都成立。
            CheckTrue(RewardClaimFx.Ready(rwFx.ClaimFx) || !rwFx.ClaimFxVisible,
                      "★★ A496⑬：**材质没建齐（`Ready == false`）⇒ 整棵不许开**"
                      + $"；实得 `Ready={RewardClaimFx.Ready(rwFx.ClaimFx)}` · `ClaimFxVisible={rwFx.ClaimFxVisible}`"
                      + "（那一档 `RewardClaimFx.Build` 会打一条出声警告 —— 红线：不许静默、也不许拿别的 shader 顶替）"
                      + " —— 把 `SetVisible` 里的 `&& built.Ready` 删掉 ⇒ 今天就会硬开 ⇒ 红");

            // ============================================================ 🆕 **2026-10-16（A820 · H3 §D）**
            // **逐格领取粒子 / 音效（`RewardAppearParticle`）—— 「素材落地后」才加得进来的那三条**。
            //   判据 = `资料/普查产出_1012/H3_领取粒子与Blink公共件.md` **§D**（原题：「**素材落地后**才能加的断言（待接线，记着做）」）：
            //     ① `AppearFxPlayed == 格数` · `AppearSfxPlayed == 格数`（每格一次）；
            //     ② **重建不掉粒子**（`DetachLiveFx` / `ReattachLiveFx` 那一对）；
            //     ③ 粒子**跟着那一格的 punch 缩放**（原版：粒子就是抽屉的子件）。
            //   🔴 **为什么今天才加得进来**：`WarpforgeEffectPlayer.Play` 取不到 prefab 时返回 `null`
            //     ⇒ 素材没进工程时 `AppearFxPlayed` **恒 0**（那一档唯一能验的是 `AppearFxFired` = §C③④）。
            //     2026-10-15 四步落地（prefab `Assets/WarpforgeVFX/Prefabs/RewardAppearParticle.prefab` ·
            //     效果库里 `RewardAppearParticle` 那一条 · `CardPresentation/Resources/Art/audio/sfx/Add card to deck.wav`）
            //     ⇒ 本段今天才成立：它验的是**两个真素材都在盘上、而且都接对了**（⛔ 不是「把 0 改成 12」那种改法）。
            //   ⛔ **期望值一律手写字面量**（`12` · `0.75` · `0.1` · `1.1`），⛔ 不读 `RewardWindow.PunchAmount` /
            //     `…FxPitchSpan` / `…AppearFxAt` 那一族当期望值 —— 那是**被测实现里的数**（本仓「断言自证」那条坑）。
            //   ⚠️ **`格数` 取 12**：12 条**同一种**基础档奖励（`quantity = 1` ⇒ 第 4 跳不展开）⇒ 格数 = 条数 = 12；
            //     最外那两格（下标 0 / 11）的归一化距离 = 1320 ÷ 960 = 1.375、被 `clamp01` 到 1
            //     ⇒ 触发时刻 = 1 × **0.75**（0.75 = 原版字面量 `0x1834b2e84`）⇒ 推 0.75s 那一拍 12 格全到点。
            var apSpec = new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierBasic);
            var apTwelve = new List<CampaignData.RewardSpec>();
            for (int i = 0; i < 12; i++) apTwelve.Add(apSpec);

            // ---- ① 每格**真播一次**：`AppearFxPlayed` / `AppearSfxPlayed` == 12 ----
            var rwAp = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwAp, new RewardWindowContext { Rewards = apTwelve.ToArray(), IsPreview = false });
            Check(rwAp.ListHolder != null ? rwAp.ListHolder.childCount : -1, 12,
                  "★ A820①：（前提）12 格抽屉都建出来了（少了 ⇒ 下面那两条「== 12」会红成假象）");
            rwAp.Tick(0.75f);                       // 最外那两格的触发时刻正好是 0.75 ⇒ 这一拍 12 格全到点
            Check(rwAp.AppearFxPlayed, 12,
                  "★★ A820①：**12 格 ⇒ 12 颗粒子真播出来了**（每格一次 —— `WarpforgeEffectPlayer.Play` 在"
                  + "「效果库里没这条 / prefab 引用是空的」时返回 `null`、这一格就不计数 ⇒ 这个数会**少**）");
            Check(rwAp.AppearSfxPlayed, 12,
                  "★★ A820①：**12 格 ⇒ 12 次音效**（`WFSoundBank.Clip(\"Add card to deck\")` 取不到 clip 时"
                  + "`FireAppearFx` 只出声、不计数 ⇒ `Resources/Art/audio/sfx/` 里那个 wav 缺了 ⇒ 这个数会**少**）");

            // ---- ② 重建**不掉粒子**（`DetachLiveFx` / `ReattachLiveFx` 那一对）----
            //   我们这条管线独有的坑：物品格**每次 `Tick` 都重建**（裁切是建的时候切进矩形/uv 的），
            //   而粒子是**那一格的子件** ⇒ 不先摘的话，揭示收尾那一拍会把它**连节点一起销毁**
            //   （现象 = 粒子只活 0.05 秒、**而且不报任何错**）。
            //   ⇒ 先把「此刻那批粒子 + 它挂在哪一格 + 它自己的 local TRS」记下来，
            //     再推**揭示收尾那一拍**（`_animT = 0.75 + 0.05 = 0.80 = AnimTime`），然后回头看它们。
            var apFx = rwAp.GetComponentsInChildren<WarpforgeVFX.WarpforgeEffectPlayer>(true);
            var apFxIdx = new int[apFx.Length];
            var apFxPos = new Vector3[apFx.Length];
            var apFxScl = new Vector3[apFx.Length];
            int apOnCell = 0;
            for (int i = 0; i < apFx.Length; i++)
            {
                var t = apFx[i] != null ? apFx[i].transform : null;
                apFxIdx[i] = t != null && t.parent != null ? t.parent.GetSiblingIndex() : -1;
                apFxPos[i] = t != null ? t.localPosition : Vector3.zero;
                apFxScl[i] = t != null ? t.localScale : Vector3.zero;
                if (t != null && t.parent != null && t.parent.parent == rwAp.ListHolder) apOnCell++;
            }
            Check(apFx.Length, 12, "★ A820②：（前提）抓到 12 个粒子实例（少了 ⇒ 下面那三条空转）");
            Check(apOnCell, apFx.Length,
                  "★ A820②：（前提）此刻每一颗都挂在 `ListHolder` 的某一格下（⛔ 不是窗根）—— "
                  + "原版那一句是 `Instantiate(particleOnAppear, drawers[i].transform)`");
            rwAp.Tick(0.05f);                       // = 揭示收尾那一拍（0.80s）⇒ **这一拍整批格子会被重建**
            CheckNear(rwAp.RevealProgress, 1f, 1e-3f,
                      "（前提）揭示已经跑完（`_animT = 0.80` = `AnimTime`）—— 上面那一拍真走了「重建」那支");
            Check(rwAp.AppearFxPlayed, 12,
                  "★★ A820①：**每格只发一次** —— 揭示收尾重建了一整批格子，计数**仍是 12**"
                  + "（少了 `FireAppearFx` 之前那句 `_fxFired[i] = true` ⇒ 每一拍都重发 ⇒ 音频爆掉）");
            int apAlive = 0, apHome = 0, apSameCell = 0, apSameTrs = 0;
            for (int i = 0; i < apFx.Length; i++)
            {
                var p = apFx[i];
                if (p == null) continue;                 // Unity 伪 null = 这一颗**随旧节点被销毁了**
                apAlive++;
                var t = p.transform;
                if (t.parent == null || t.parent.parent != rwAp.ListHolder) continue;
                apHome++;
                if (t.parent.GetSiblingIndex() == apFxIdx[i]) apSameCell++;
                if (Vector3.Distance(t.localPosition, apFxPos[i]) < 1e-4f
                    && Vector3.Distance(t.localScale, apFxScl[i]) < 1e-3f) apSameTrs++;
            }
            Check(apAlive, apFx.Length,
                  "★★ A820②：**重建不掉粒子** —— 推到揭示结束（`_animT ≥ 0.8`）之后，先前那 12 颗**一颗都没死**"
                  + "（把 `BuildItems()` 开头那句 `DetachLiveFx()` 删掉 ⇒ 它们随旧格子被 `DestroySafe` ⇒ 这里会少）");
            Check(apHome, apAlive,
                  "★★ A820②：…而且每一颗都**回到了 `ListHolder` 下的格子里**"
                  + "（删掉 `BuildItems()` 末尾那句 `ReattachLiveFx()` ⇒ 它们停在窗根 `rwAp.transform` 下 ⇒ 位置错 ⇒ 这一条红）");
            Check(apSameCell, apAlive,
                  "★★ A820②：…而且是**它自己那一格**（按 `GetSiblingIndex()` 对位 —— 挂到别格上的实现这里红）");
            Check(apSameTrs, apAlive,
                  "★★ A820②：…挂回去之后**自己那份 local TRS 逐位不变**（那一对走 `SetParent(x, false)`）—— "
                  + "`worldPositionStays: true` 的写法会把 local 位姿改成「保住世界位姿」的那个值 ⇒ 这一条红");

            // ---- ③ 粒子**跟着它那一格的 punch 缩放**（原版：粒子是抽屉的子件）----
            //   判据 = §D 第三条「`Tick` 到 punch 在飞的那一拍，读 `粒子.transform.lossyScale`」。
            //   取**同一扇窗里的两颗**做**相对**比较（同一个父链 ⇒ 窗口根/视口/内容那几级缩放自动约掉）：
            //     · 下标 **0**（最外那格）此刻在**峰上**：延迟 0.75 + 第 1 段 0.4/3 ⇒ 幅度**正好 +0.1**
            //       （0.1 = 原版字面量 `0x1834b2dc4`；段界算式见本文件 (c) 那两条）
            //     · 下标 **5**（正中那格）的 punch **早收完了**（0.09375 + 0.4 = 0.494 < 0.883）⇒ 缩放**正好 1.0**
            //   ⇒ 两颗的 `lossyScale` 之比必须 ≈ **1.1 ÷ 1.0 = 1.1**（手算）。
            //   ⚠️ **相对比较、不引 prefab 自己的 14.838**：那个数能不能「原样活过导入器」正是 H3 §E·2
            //      还挂着的那条（`Unity 导入器会静默改坏原版资产`）—— 引它当期望值会把那条待验的账变成假绿。
            //   改坏法：粒子若不挂在那格上（留在窗根 / punch 只贴给节点而粒子挂别处）⇒ 两颗同缩放 ⇒ 比值 1.0 ⇒ 红。
            var rwApP = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwApP, new RewardWindowContext { Rewards = apTwelve.ToArray(), IsPreview = false });
            float apPeakT = RewardWindow.PunchDelayAt(11, 12) + 0.4f / 3f;   // 0.75 + 0.1333 = 0.8833s
            CardTween.Advance(apPeakT);
            rwApP.Tick(apPeakT);
            CheckNear(rwApP.PunchScaleOf(0).x - 1f, 0.1f, 0.01f,
                      "★★ A820③：（前提）这一拍下标 0 那格**正在峰上**（原版幅度 **0.1** = `0x1834b2dc4`）");
            CheckNear(rwApP.PunchScaleOf(5).x, 1f, 1e-3f,
                      "★ A820③：（前提）这一拍下标 5 那格**已经收完**（它延迟 0.09375 + 时长 0.4 = 0.494 < 0.883）");
            var apEdge = rwApP.ListHolder != null && rwApP.ListHolder.childCount > 11
                ? rwApP.ListHolder.GetChild(0) : null;
            var apMid = rwApP.ListHolder != null && rwApP.ListHolder.childCount > 5
                ? rwApP.ListHolder.GetChild(5) : null;
            CheckNear(apEdge != null ? apEdge.localScale.x : -1f, 1.1f, 0.01f,
                      "★ A820③：（前提）下标 0 那格的**节点**也已经被贴上 1.1（`BuildItems` / `ApplyPunchScales` 那一跳）");
            CheckNear(apMid != null ? apMid.localScale.x : -1f, 1f, 1e-3f,
                      "★ A820③：（前提）下标 5 那格的节点回到 1.0");
            var apFxEdge = apEdge != null ? apEdge.GetComponentInChildren<WarpforgeVFX.WarpforgeEffectPlayer>(true) : null;
            var apFxMid = apMid != null ? apMid.GetComponentInChildren<WarpforgeVFX.WarpforgeEffectPlayer>(true) : null;
            CheckTrue(apFxEdge != null && apFxMid != null,
                      "★ A820③：（前提）那两格里各挂着一颗粒子（挂空 = 粒子没跟着格子走 ⇒ ② 那三条也会一起红）");
            float apScaleEdge = apFxEdge != null ? apFxEdge.transform.lossyScale.x : -1f;
            float apScaleMid = apFxMid != null ? apFxMid.transform.lossyScale.x : -1f;
            CheckTrue(apScaleMid > 0f, "★ A820③：（前提）拿到了正中那颗粒子的 `lossyScale.x`（分母非零）");
            CheckNear(apScaleEdge / (apScaleMid > 0f ? apScaleMid : 1f), 1.1f, 0.02f,
                      $"★★ A820③：粒子**跟着它那一格的 punch 一起缩放**（峰上那格 {apScaleEdge:F3} ÷ 收起那格 {apScaleMid:F3}"
                      + $" = {apScaleEdge / (apScaleMid > 0f ? apScaleMid : 1f):F3}）—— 期望 **1.1** = 幅度 0.1 手算；"
                      + "粒子不挂在那一格上的实现（比如留在窗根下）两颗同缩放 ⇒ 比值 1.0 ⇒ 红");

            // ============================================================ 🆕 **2026-10-16（A826 · H3 §C）**
            // **领取粒子的「排期 / 到点才发 / 只发一次 / 音高 / 取不到素材出声」** —— H3 §C 那张表
            //   （`资料/普查产出_1012/H3_领取粒子与Blink公共件.md` §C 六条）**从来没接线过**：
            //   改文件之前 grep 过 `AppearFxScheduled|AppearFxAt|AppearFxFired|AppearFxPitch` —— 全仓只命中
            //   生产文件 `Shell/RewardWindow.cs`（W12 §三 独立复现过同一条）⇒ 本段把它们接上，
            //   并把 H3 §D 的「**到点之前应当 == 0**」那半一并补上（W12 §四·2 自陈没写的那一半）。
            //   ⛔ **期望值一律手写字面量**（0.75 / 0.65625 / 0.09375 / 1.5 / 1.0625 …），⛔ 不读
            //     `RewardWindow.PunchDelayMax` / `FxPitchSpan` / `AppearFxAt` 那一族当期望值（那是**被测实现里的数**
            //     = 自证 —— W12 那段头也写着同一条）。★ 从生产侧读的只有「可观测点」本身（`AppearFxAt(i)` 等）。
            //   ⚠️ **夹具**：12 条同一种基础档（`quantity = 1` ⇒ 第 4 跳不展开），与 W12 §D 那批**同一份**。
            //     手算（`drawerCx(i) = 240i − 1320` · 内容宽 2960 · 参照 = 视口半宽 960）：
            //       i=0/11 → 1320/960 = 1.375 →`clamp01`→ 1 ⇒ **0.75s**（i=1/10 也是 1.125 →夹到 1 ⇒ 0.75）
            //       i=2/9 → 0.875 ⇒ **0.65625** · i=3/8 → 0.625 ⇒ **0.46875**
            //       i=4/7 → 0.375 ⇒ **0.28125** · i=5/6 → 0.125 ⇒ **0.09375**（正中那两格最早）
            //   🔴 **H3 §C③ 的 `dt` 已就地订正（铁律 5）**：原文写「`Tick(0.05f)` ⇒ `AppearFxFired(5)` 真」，
            //     与 §C② 自己的手算值 `AppearFxAt(5) = 0.09375` **矛盾**（0.05 < 0.09375 ⇒ 那一拍它**还没到点**）。
            //     按 §C② 的数拆成三步（0.05 → 0.10 → 0.75）；而 0.05 那一拍正好就是 §C④ 的正例
            //     与 §D 的「**到点之前 == 0**」那半（12 格**一个都没发**）。
            //   ⚠️ **素材腿**（干净克隆上这一族会红，⛔ 不是缺陷）：粒子那一路要 `WarpforgeEffectLibrary`
            //     ＋ `Assets/WarpforgeVFX/Prefabs/RewardAppearParticle.prefab`（**两份都在 `.gitignore` 里**：
            //     `:38` / `:26`），音效那一路要 `CardPresentation/Resources/Art/audio/sfx/`（`:91`）
            //     ⇒ 只有**计数类**断言会红；本段的**排期 / 到点 / 音高**三组**一个都不依赖素材**。

            // ---- ① 排期（两态：非预览 vs 预览）----
            CheckTrue(rwFx != null && rwFx.AppearFxScheduled,
                      "★★ A826①：非预览 ⇒ **排了期**（`ScheduleAppearFx` 给每一格建出触发时刻表）"
                      + "｜改坏法：删掉 `Shell/RewardWindow.cs` 的 `Build()` 第 ⑩ 段那句 `ScheduleAppearFx()` ⇒ 红");
            CheckTrue(rwFxP != null && !rwFxP.AppearFxScheduled,
                      "★★ A826①（**预览那一档**）：`IsPreview = true` ⇒ **一条都不排**"
                      + "（判据 `RewardWindow__Open.c:283`：预览态压根不调 `DoRewardAnimation`）"
                      + "｜改坏法：把 `ScheduleAppearFx()` 里那句 `if (IsPreview) return;` 删掉 ⇒ 预览这一条红"
                      + "（这两扇窗**只差 `IsPreview` 一个实参** ⇒ 一对合起来才不是「恒真」）");

            // ---- ② 排期值 = 原版式的手算值（`clamp01` 与「分母是半视口」两处单拎出来）----
            CheckNear(rwAp.AppearFxAt(0), 0.75f, 1e-4f,
                      "★★ A826②：最外那格（下标 0）的触发时刻 = **0.75s**（归一化距离 1320/960 = 1.375 "
                      + "被 `clamp01` 夹到 1 ⇒ 1 × 0.75）｜把 `× PunchDelayMax` 写成 `× 1` ⇒ 红");
            CheckNear(rwAp.AppearFxAt(11), 0.75f, 1e-4f, "★★ A826②：…另一头（下标 11）同值");
            CheckNear(rwAp.AppearFxAt(1), 0.75f, 1e-4f,
                      "★★ A826②：下标 1（1080/960 = **1.125**）也被夹到 1 ⇒ 仍是 0.75"
                      + "｜删掉 `PunchNorm` 里那句 `Mathf.Clamp01` ⇒ 变 **0.84375** ⇒ 红");
            CheckNear(rwAp.AppearFxAt(2), 0.65625f, 1e-4f,
                      "★★ A826②：下标 2 = 840/960 = 0.875 ⇒ **0.65625s**（手算 0.875 × 0.75）");
            CheckNear(rwAp.AppearFxAt(5), 0.09375f, 1e-4f,
                      "★★ A826②：**正中那两格**（下标 5）= 120/960 = 0.125 ⇒ **0.09375s**（全场最早）"
                      + "｜把参照写成整个视口宽 1920 ⇒ 这一条与上面几条一起红");
            int apSym = 0;
            for (int i = 0; i < 6; i++)
                if (Mathf.Abs(rwAp.AppearFxAt(i) - rwAp.AppearFxAt(11 - i)) < 1e-4f) apSym++;
            Check(apSym, 6,
                  "★ A826②（相对断言 · 一个常量都不引）：**时刻表左右对称**（i 与 11−i 逐对相等，6/6）—— "
                  + "按抽屉序号、而不是按「到内容中心的距离」算的实现这里不对称 ⇒ 红");
            CheckTrue(rwAp.AppearFxAt(0) > rwAp.AppearFxAt(2) + 0.05f
                      && rwAp.AppearFxAt(2) > rwAp.AppearFxAt(4) + 0.05f
                      && rwAp.AppearFxAt(4) > rwAp.AppearFxAt(5) + 0.05f,
                      "★ A826②（相对断言）：**由外向内一格比一格早**（原版那条错峰 =「揭示从中间往两边拉开」）"
                      + "—— 排成同一个值 / 排序反了 ⇒ 红");

            // ---- ③ 到点才发 + ④ 只发一次 + §D「到点之前 == 0」：一台**从 0 开始**的新窗，按手算的三个时刻推 ----
            var rwApC = RewardWindow.Create(wm2);
            wm2.OpenWindow(rwApC, new RewardWindowContext { Rewards = apTwelve.ToArray(), IsPreview = false });
            Check(rwApC.ListHolder != null ? rwApC.ListHolder.childCount : -1, 12,
                  "★ A826③：（前提）12 格抽屉都建出来了（少了 ⇒ 下面这一串会红成假象）");
            Check(rwApC.AppearFxPlayed, 0,
                  "★★ A826④ / §D「**到点之前**」：`Build()` 之后还没推时钟 ⇒ **一个粒子都没发**"
                  + "（原版那一支是 `WaitForSeconds(delay)`；到点前就发 = 把整条错峰抹平）");
            Check(rwApC.AppearSfxPlayed, 0, "★★ A826④ / §D：…音效也是 **0**（同一拍那两件都没发生）");
            // 🔴 出声那一族**只在本段这段时钟里抓**（`Application.logMessageReceived` 是全局的 ⇒ 进出必须成对；
            //    写法照本文件既有那两处：`border 比图还大` 那一组与 `[ItemDrawer]` 那一组）。
            int apCWarn = 0;
            Application.LogCallback hApWarn = (cond, st, type) =>
            {
                if (type == LogType.Warning && cond != null
                    && cond.Contains("[RewardWindow]") && cond.Contains("RewardAppearParticle")) apCWarn++;
            };
            Application.logMessageReceived += hApWarn;
            rwApC.Tick(0.05f);                     // 时钟 0.05 < 最早那一格（0.09375）
            Check(rwApC.AppearFxFired(5), false,
                  "★★ A826③ / §D：时钟 **0.05** ⇒ 最早那一格（0.09375）**还没到** ⇒ 它没发"
                  + "｜把 `TickAppearFx` 里那半句 `_animT < _fxAt[i]` 删掉 ⇒ 第一拍就全发 ⇒ 红");
            Check(rwApC.AppearFxFired(0), false, "★★ A826③：…最外那格（0.75）当然也没发");
            Check(rwApC.AppearFxPlayed, 0, "★★ A826③/§D：…而且**一个粒子都没真播**（到点之前 == 0）");
            Check(rwApC.AppearSfxPlayed, 0, "★★ A826③/§D：…音效同数（0）");
            rwApC.Tick(0.05f);                     // 时钟 0.10 > 0.09375
            Check(rwApC.AppearFxFired(5), true,
                  "★★ A826③：再推 0.05（时钟 **0.10**）⇒ 正中那两格**到点就发**（0.09375 ≤ 0.10）");
            Check(rwApC.AppearFxFired(6), true, "★★ A826③：…与它对称的那一格（下标 6）同拍发");
            Check(rwApC.AppearFxFired(4), false,
                  "★★ A826③：…而**紧挨着它的一格**（下标 4 = 0.28125）**还没到** ⇒ 没发"
                  + "—— 上面两条 + 这一条**同一拍上两态并存**，才分得出「各按自己的时刻发」与「一拍全发 / 永不发」");
            Check(rwApC.AppearFxFired(0), false, "★★ A826③：…最外那格仍没到（0.75 > 0.10）");
            int apFired10 = 0;
            for (int i = 0; i < 12; i++) if (rwApC.AppearFxFired(i)) apFired10++;
            Check(apFired10, 2, "★★ A826③：这一拍**只有 2 格**到过点（到点集合 = {5, 6}，逐格点过一遍数出来）");
            CheckTrue(rwApC.AppearFxPlayed <= apFired10 && rwApC.AppearSfxPlayed <= apFired10,
                      $"★★ A826④：两个计数都 **≤ 已发过的格数**（实测 粒子 {rwApC.AppearFxPlayed} · "
                      + $"音效 {rwApC.AppearSfxPlayed} ≤ {apFired10}）—— 一格最多一颗粒子 + 一声"
                      + "｜删掉 `FireAppearFx` 之前那句 `_fxFired[i] = true` ⇒ 每一拍这 2 格都重发 ⇒ 很快越过上限 ⇒ 红"
                      + "（⚠️ 干净克隆上这两个计数是 0 ⇒ 本条在那一档空过；它带电的是**素材齐**这一档）");
            rwApC.Tick(0.65f);                     // 时钟 0.75 = 最外那两格的触发时刻
            Check(rwApC.AppearFxFired(0), true, "★★ A826③：再推到 **0.75**（最外那两格的时刻）⇒ 它们发了");
            Check(rwApC.AppearFxFired(11), true, "★★ A826③：…另一头同拍");
            Check(rwApC.AppearFxFired(1), true, "★★ A826③：…被 `clamp01` 拉平的那一格（下标 1）也在这一拍到点");
            int apFired75 = 0;
            for (int i = 0; i < 12; i++) if (rwApC.AppearFxFired(i)) apFired75++;
            Check(apFired75, 12, "★★ A826③：0.75 那一拍 ⇒ **12 格全到过点**（一个都不漏）");
            Check(rwApC.AppearFxPlayed, 12,
                  "★★ A826④ / H3 §C④ 的 `== 12`（素材 2026-10-15 已落地）—— W12 断过「揭示重建那一拍仍是 12」，"
                  + "本段断的是**再往后推两拍也一样**（见下）"
                  + "｜`WarpforgeEffectPlayer.Play` 在「效果库里没这条 / prefab 引用是空的」时返回 null、"
                  + "那一格就不计数 ⇒ 这个数会**少**");
            rwApC.Tick(0.5f); rwApC.Tick(0.5f);    // 时钟 1.25 / 1.75 —— 都过了揭示收尾（0.8）与 punch 末端（1.15）
            Check(rwApC.AppearFxPlayed, 12,
                  "★★ A826④：**只发一次** —— 又推两拍（时钟 1.25 / 1.75）**仍是 12**"
                  + "｜删掉 `_fxFired[i] = true` ⇒ 每一拍 12 格重发 ⇒ 红（音频爆掉）");
            int apFiredLate = 0;
            for (int i = 0; i < 12; i++) if (rwApC.AppearFxFired(i)) apFiredLate++;
            CheckTrue(rwApC.AppearSfxPlayed <= apFiredLate,
                      $"★★ A826④：音效次数也 **≤ 已发过的格数**（实测 {rwApC.AppearSfxPlayed} ≤ {apFiredLate}）"
                      + "—— 每拍重发的实现这里会爆掉；⚠️ 干净克隆上 `Resources/Art/**` 没跑素材腿 ⇒ 这个数是 0、"
                      + "本条照样成立（它不引任何素材侧的数）");
            Application.logMessageReceived -= hApWarn;

            // ---- ⑤ 音高 = `1.0 + 0.5 × clamp01(归一化距离)`（原版两个字面量 · 手算值）----
            CheckNear(RewardWindow.AppearFxPitch(0f), 1f, 1e-5f,
                      "★★ A826⑤：归一化距离 **0**（正中那格）⇒ 音高 **1.0**"
                      + "｜把 `FxPitchBase` 改成别的数 ⇒ 红");
            CheckNear(RewardWindow.AppearFxPitch(0.25f), 1.125f, 1e-5f,
                      "★★ A826⑤：0.25 ⇒ **1.125**（1.0 + 0.5 × 0.25）—— 斜率那一档｜把 `FxPitchSpan` 写成 1.0 ⇒ 红");
            CheckNear(RewardWindow.AppearFxPitch(1f), 1.5f, 1e-5f, "★★ A826⑤：1 ⇒ **1.5**（原版那个区间的上端）");
            CheckNear(RewardWindow.AppearFxPitch(2f), 1.5f, 1e-5f,
                      "★★ A826⑤：2 ⇒ 仍 **1.5**（`clamp01` 上界）｜去掉夹取 ⇒ 变 2.0 ⇒ 红");
            CheckNear(RewardWindow.AppearFxPitch(-1f), 1f, 1e-5f,
                      "★★ A826⑤：−1 ⇒ **1.0**（`clamp01` 下界）｜去掉夹取 ⇒ 变 0.5 ⇒ 红");
            CheckNear(RewardWindow.AppearFxPitch(1320f / 960f), 1.5f, 1e-5f,
                      "★★ A826⑤（接上格子几何）：最外那两格的原版归一化距离 **1320 ÷ 960 = 1.375** ⇒ 音高 **1.5**"
                      + "（函数自己还要再夹一次 —— 原版那一条也是同一条算式）");
            CheckNear(RewardWindow.AppearFxPitch(120f / 960f), 1.0625f, 1e-5f,
                      "★★ A826⑤（接上格子几何）：正中那两格 **120 ÷ 960 = 0.125** ⇒ 音高 **1.0625**"
                      + "—— 与上一条合起来 = 那条**可听**的性质：越靠外音越高（区间 [1.0, 1.5]，⛔ 不是随机）");

            // ---- ⑥ 取不到素材要出声（关系式；两态各带电一半）----
            CheckTrue((apCWarn > 0) != (rwApC.AppearFxPlayed == 12),
                      $"★★ A826⑥：**「出声」与「12 格全播出来」恰好一个成立**（实测 警告 {apCWarn} 条 · "
                      + $"`AppearFxPlayed={rwApC.AppearFxPlayed}`）—— 素材齐（今天）⇒ 右边真、左边必须假"
                      + "（**一条警告都不许有**：乱报警会把真缺素材时的信号淹掉）；素材腿没跑 / 效果库缺那一条 ⇒ "
                      + "左边必须真（`FireAppearFx` 那条 `Debug.LogWarning` 说的是「差哪一步」）"
                      + "｜改坏法：把那条 `Debug.LogWarning` 删掉 ⇒ 缺素材那一档变成「没播、也不出声」⇒ 两态全假 ⇒ 红"
                      + "（红线：不许静默失败）");
            CheckTrue(apCWarn <= 1,
                      $"★★ A826⑥：那条出声**只响一次**（实测 {apCWarn} 条；12 格到点若逐格报 = 12 条）"
                      + "｜改坏法：把 `_fxMissingWarned`（`_fxNoCellWarned` 同）那道闩删掉 ⇒ 红"
                      + "（⚠️ 今天素材齐 ⇒ 这个数是 0、本条在那一档空过 —— 它带电的是「素材缺」那一档）");

            // ---- ⑭ 收工还原 ----
            RewardWindowFixture.DismissRewardWindows();
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                  "★ A496⑭：（收工还原）奖励窗**全**收掉了（计数 0）（⛔ 别把开着的窗留给后面的节 —— §六 那几条按现场断顶窗）");
            wm2.CloseAllWindows();
        }

        Section("§八 战果 → 任务进度（接 `Battle/EndPanel` 那条链，2026-09-23 接的）");
        // 三张每日任务卡：0 = `Deal 500 damage to enemy units` · 1 = `Play 10 troops` · 2 = `Win 3 battles`
        // 🆕 **2026-10-11（A375）**：`OnBattleEnd` 多了两个实参（`skulls` / `playMode`）。
        //    本节一律传 **0 个骷髅 + `Classic(0)`** ⇒ 下面这几条断言的期望值**逐字不变**
        //    （骷髅那半在紧跟着的 §八·2 里单独断，两节互不干扰：那节只传 `win:false, 0, 0`）。
        const int NoSkulls375 = 0, Classic375 = 0;
        DailyData.ResetMissionsForTest();      // 52/500 · 4/10 · 1/3（**确定的初值**，见 `ResetMissionsForTest`）
        DailyData.OnBattleEnd(true, 100, 3, NoSkulls375, Classic375);
        Check(DailyData.DailyProgressValue(0), 152, "「对敌伤害」按**实打伤害**推进（52 + 100）");
        Check(DailyData.DailyProgressValue(1), 7,   "「打出部队」按**打出的部队卡张数**推进（4 + 3）");
        Check(DailyData.DailyProgressValue(2), 2,   "赢了那一局 ⇒ 胜场 +1（1 + 1）");
        // 一次打够 ⇒ **封顶在 target**（原版 `MissionChallengeProgress` 也是夹住的）
        DailyData.OnBattleEnd(true, 99999, 99999, NoSkulls375, Classic375);
        Check(DailyData.DailyProgressValue(0), 500, "进度**封顶在 target**（500）");
        Check(DailyData.DailyProgressValue(1), 10,  "进度**封顶在 target**（10）");
        Check(DailyData.DailyProgressValue(2), 3,   "进度**封顶在 target**（3）");
        Check(DailyData.DailyState(2), DailyData.State.Collectable, "到顶 ⇒ 变**可领取**（原版 `MissionBackgroundHighlighter` 那一态）");
        // 输了不加胜场
        DailyData.OnBattleEnd(false, 0, 0, NoSkulls375, Classic375);
        Check(DailyData.DailyProgressValue(2), 3, "输了那一局 ⇒ 胜场**不加**");

        // ============================================================ 🆕 A375（2026-10-11）
        // 「一局结束 → 骷髅**累加**进当日计数」—— 这条线原来**整条是断的**：`_skullsCount` 是个出厂 mock
        // （`= 160`），全工程没有任何地方往它上面加过东西 ⇒ 骷髅卡恒 `x160`、五档恒全亮。
        //
        // 原版链路（`d:/2/tools/decomp_full/`，逐环亲读；全文 → `资料/普查产出_1011/R1_每日骷髅与登录卡.md` §二）：
        //   `ChallengeLogMgr__LogMatchEnd.c`（`BattleEndSignal___ctor(signal, matchData, gameMode,
        //   BattleScoreManager__GetSkullCount(manager + 0xF8), isWin)`）
        //   → `SkullsCount__OnBattleEnd.c` 末句 `MissionChallenge__UpdateProgress(this, signal.SkullsCount, **0**, 0)`
        //   → `…DisplayClass35_0___UpdateProgress_b__0.c`：`shouldOverride == false ⇒ value + currentValue`。
        //   ⇒ **累加**（当日所有战斗之和），**不是**「取最好那一局」、**不是**按胜负给。
        //
        // 🔴 **本节期望值全是字面量**（原版的 `PlayModes` 号、`1 + 2 = 3` 这种账）—— ⛔ 不从 `DailyData`
        //    自己的表里取，那是**同义反复**（本仓「断言自证」那条坑）。
        // 🔴 **每条都断【两态】**：① 累加 vs 覆盖（1 + 2 得 3，覆盖只得 2）② 给 vs 不给（同一份 `OnBattleEnd`
        //    入口，模式号换一下结果就翻）—— 只断一头分不出「按模式判」与「恒加 / 恒不加」。
        // **改坏法**（三条，各红在不同的一组上）：① `AddSkulls` 里那两句删掉（或 `+=` 改 `=`）⇒ ① 那组红；
        //    ② `ModeGivesSkulls` 的 `case 1/2/4/5/8/9` 并进 `true`（或整条恒 `return true`）⇒ ② 那 6 条 +
        //    ③ 那 6 条红；③ `_skullsCount` 的出厂值改回任何非零值 ⇒ 本节前提那条与 A370 ⓪ 那两条一起红。
        Section("🆕 A375：一局结束 ⇒ 骷髅**累加**进当日计数（原版 `SkullsCount.OnBattleEnd`）+ 那 6 个模式一颗都不给");
        {
            int skullBase375 = DailyData.SkullsCountValue();   // 本节起点（收工还原）
            // 出厂 = **0**（A375 拆掉了原来那个 `= 160` 的 mock；这条同时是那张欠账的验收）
            Check(skullBase375, 0,
                  "★ （前提）出厂骷髅计数 = **0** —— 原版实拍那张「每日骷髅头」卡印的就是 **`x0` + 五格全灭**；"
                  + "A375 之前这里是个 **mock 常量 160** ⇒ 画面恒 `x160`、五档恒全亮");

            // -------- ① 累加语义（原版 `shouldOverride: false`）
            // ⚠️ 一律传 `win: false, damage 0, troops 0` ⇒ 三条每日任务**一个数都不动**
            //    （`Advance` 对 `n <= 0` 早退；败局不加胜场）⇒ 本节不需要给那三条单独做夹具，也不会污染 §八 的结论。
            DailyData.OnBattleEnd(false, 0, 0, 1, 0);          // `Classic(0)`，本局拿到 1 个
            Check(DailyData.SkullsCountValue(), 1, "★ 本局 1 个 ⇒ 计数 = **1**（`Classic(0)` 在给骷髅那一组）");
            DailyData.OnBattleEnd(false, 0, 0, 2, 0);          // 又拿到 2 个
            Check(DailyData.SkullsCountValue(), 3,
                  "★ 再来一局 2 个 ⇒ 计数 = **3（1 + 2）**—— 这是「**累加**」；写成覆盖的实现这里得 **2** ⇒ 红"
                  + "（原版 `UpdateProgress(…, shouldOverride: false)` 那一支）");
            DailyData.OnBattleEnd(false, 0, 0, 0, 13);         // 打了一局、一颗都没削到
            Check(DailyData.SkullsCountValue(), 3, "★ 本局 **0 个**（一颗都没削到）⇒ 计数**不动**（仍是 3）");

            // -------- ② 那 6 个模式**一颗都不给**（原版 `MatchData.GetMilestones` 返回**空数组**）
            // 走的**真入口**（`OnBattleEnd`），断的是**行为**（计数动不动）—— 表本身是 ③ 那条独立的尺子。
            int[] zeroModes375 = { 1 /*Duel*/, 2 /*PracticeLodge*/, 4 /*Tutorial*/,
                                   5 /*CutScene*/, 8 /*Campaign*/, 9 /*TutorialReplay*/ };
            for (int i = 0; i < zeroModes375.Length; i++)
            {
                DailyData.OnBattleEnd(false, 0, 0, 3, zeroModes375[i]);   // 就算本局账上是 3 个，也一颗不给
                Check(DailyData.SkullsCountValue(), 3,
                      "★ 模式 " + zeroModes375[i] + " ⇒ **一颗都不给**（哪怕本局账上是 3 个）—— 计数仍是 3"
                      + "（原版 `MatchData__GetMilestones.c` 这一支 `System_Array__Empty` 返回空数组）");
            }

            // -------- ③ 那张表本身：逐模式核（与 ② 是**两条独立的尺子**：一条断行为、一条断表）
            // 判据 = `dump.cs:46188 enum PlayModes` + `MatchData__GetMilestones.c` 的 `case` 表（逐 `case` 亲读）。
            int[] giveModes375 = { 0, 3, 6, 7, 10, 11, 12, 13, 14 };
            for (int i = 0; i < giveModes375.Length; i++)
                Check(DailyData.ModeGivesSkulls(giveModes375[i]), true,
                      "`PlayModes` " + giveModes375[i] + " ⇒ **给**骷髅（Classic/Dungeon/OfflinePractice/ClosedDeck/"
                      + "Replay/RankedFriendly/OwnDeckTraining/Skirmish/Battle4Warpforge）");
            for (int i = 0; i < zeroModes375.Length; i++)
                Check(DailyData.ModeGivesSkulls(zeroModes375[i]), false,
                      "`PlayModes` " + zeroModes375[i] + " ⇒ **不给**（Duel/PracticeLodge/Tutorial/CutScene/"
                      + "Campaign/TutorialReplay —— 原版那一支返回**空数组** ⇒ 里程碑数为 0）");

            // -------- ④ 一路走到卡面：计数变了 ⇒ `counter text` 与亮格都跟着变（判据只此一处 = `DailyData`）
            var mt375 = tab.GetComponent<MissionsTab>();
            CheckTrue(mt375 != null, "（前提）`Missions Tab` 上挂着 `MissionsTab`（缺了它下面 `Build()` 会把自检崩掉）");
            if (mt375 != null)
            {
                mt375.Build();
                var sk375 = FindChild(tab, "Daily Skulls Mission Container");
                Check(sk375 != null ? TextOf(FindChild(sk375, "counter text")) : null, "x3",
                      "★ 累计 **3** ⇒ 卡面 `counter text` = **`x3`**（原版 `progressTextFormat = 'x{0}'`，逐字同格式）");
                int on375 = 0;
                if (sk375 != null)
                    foreach (var t in sk375.GetComponentsInChildren<Transform>(true))
                        if (t.name == "Milestone_on") on375++;
                Check(on375, 1,
                      "★ ...而且**只亮第 1 格**（阈值 3 / 10 / 25 / 50 / 100 ⇒ 3 只够过第 1 档）—— "
                      + "与 A370 那节的 `2 ⇒ 0 亮` / `100 ⇒ 5 亮` 合起来，把「亮几格 = 按阈值算」钉在**行为**上");
            }

            // -------- 还原到本节起点
            DailyData.ForceSkullsCountForTest(skullBase375);
            if (mt375 != null) mt375.Build();
            Check(DailyData.SkullsCountValue(), skullBase375, "（还原）骷髅计数回到本节起点那个值");
        }

        Section("§六 `Inbox Menu`（收件箱 —— **空态**，2026-09-23 建）");
        var inbox = InboxWindow.Create(wm2);
        wm2.OpenWindow(inbox);
        Check(inbox.type, WindowType.Popup, "`type` = 1 Popup（实证）");
        Check(inbox.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（实证）");
        Check(inbox.closeOnEsc, true, "`closeOnESC` = 1（实证）");
        // 🆕 A83②（A81 的尾巴）：压暗层（「点窗外关窗」）那条不变量 —— 档 = **压暗层自己那一档**
        //   `QShade`(3002)，**严格低于**本窗内容命中区档 `QOverlay`(3014)；并核「这节点确实是
        //   公共件 `MenuDraw.ShadeHit` 建的」。期望值全是本窗自己的**原版档常量**（⛔ 不从被测实现里读）。
        //   逐窗档位 → `Shell/InboxWindow.cs` 里那套档常量；公共件规矩 → `Shell/MenuDraw.ShadeHit` 的注释。
        //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档。
        MenuDraw.CheckShadeRule(CheckTrue, "收件箱窗", inbox.ShadeHit,
                                inbox.transform.Find("Menu Dark Background"), InboxWindow.QOverlay);
        Check(inbox.HasMessages, false, "单机没有消息 ⇒ 走**空态**（原版 `InboxWindow__Open.c:49` 也是这条分支）");
        var mdNode = FindChild(inbox.transform, "Message Display");
        var warnNode = FindChild(inbox.transform, "No News Warning");
        CheckTrue(mdNode != null && !mdNode.gameObject.activeSelf, "空态下 `Message Display` **是关的**");
        CheckTrue(warnNode != null && warnNode.gameObject.activeSelf, "空态下 `No News Warning` **是开的**");
        // 🆕 2026-10-09（`A1125`）：关窗钮的**命中区 + 换图层**四连断。
        //   🔴 **2026-10-18（B3）**：`Shell/InboxWindow.cs` 那处**节点名与内容错位**已按原版**改正** ——
        //   改前叫 `Background` 的 quad 画**圆底盘**、叫 `Icon` 的画**黄面**、叫 `Icon (X)` 的画叉
        //   （三个名字整体错开一格）；改后 = `Background`(**黄面** `40k_general_bt_yellow`) ·
        //   `Icon`(**叉** `40k_general_bt_yellow_close`)。
        //   ⇒ ④ 这一格从「只断贴图名」**补齐成【节点名 + 贴图名】两个条件都断**（`targetNodeName` 不再传 `null`）。
        //   🔴 **2026-10-18（B4）圆底盘【归真】**：那颗 `UI_Button_Round_background` 改前画在**自造子件** `Base` 上、
        //     用**子件矩形** `CloseBg`、**没传 `keepAspect`**；B4 现读原版（`pa.py` 逐字段）⇒ 三颗**全是**
        //     `m_PreserveAspect=1` · `m_Type=0`(Simple) · `m_PixelsPerUnitMultiplier=1.0`，圆底盘矩形 = **根矩形 `CloseBtn`**
        //     ⇒ 已照**兄弟窗先例**（`Shell/BaseOfferPopup.cs:742` · `Shell/ReferralPopupWindow.cs:386`）改成
        //     **画在根节点 `Generic Close Button Orange` 自己身上 + `keepAspect`**，自造名 `Base` **取消**。
        //     ⚠️ **本条断言的四格一个数都没变**（故这一行调用**逐字未改**）：② 量的是**命中区 quad**
        //     （`MenuDraw.Hit` 按 `PaddedRect(CloseBg, ClosePad)` 独立建 = 96.86×98.13，与三颗的矩形成像无关）；
        //     ③ 比的是**命中区中心 vs 可见面渲染中心** —— 可见面等比后**仍居中**（`CloseBg` 的中心没动）
        //     ⇒ 中心那两轴仍逐值相等；① 与 ④ 只看节点名与贴图名。
        //     ✅ **2026-10-18（B5）这条缺口已补**（这里原来写着「本文件里**没有任何一条**钉住圆底盘那颗 quad 的
        //     渲染矩形 = 74.39×74.39」⇒ 改回 `CloseBg` / 去掉 `keepAspect` 四条一条都不会红）。
        //     现在下面这一行调用之后多两格（`A1125CloseBase`）：① **圆底盘实绘【宽】** = 原版 **74.38**
        //     （= 根那一格 74.38×75.60 等比内接后高缩到宽那一档；原版读数 74.39，我们 `CloseBtn` 常量小 0.01，
        //     容差 0.5 覆盖）；② **实绘宽 == 实绘高**（= **等比**这条不变量）。
        //     🔴 **两格各只钉一轴**（① 钉宽 / ② 钉宽高相等）⇒ 两条改坏法**各红不同的一格**：
        //     `CloseBtn`→`CloseBg`（56.86×58.13）⇒ **只 ① 红**（宽少 17.5；而 56.86 仍是正方 ⇒ ② 绿）；
        //     去掉 `keepAspect: true` ⇒ **只 ② 红**（实绘 74.38×75.60，宽≠高；宽没变 ⇒ ① 绿）。
        //     尺子 = 上面 B4 那三行直读（根矩形 74.39×75.60 / `PA=1` / sprite `m_Rect` 237×237 正方）。
        //   期望值全是**原版 prefab 的读数**（本件亲跑，读数逐字抄自输出）：
        //     · `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Inbox Menu" --depth 8` 三行 ——
        //       根 `Generic Close Button Orange` 的 `Image[UI_Button_Round_background RT=0]`（**不吃射线**）·
        //       子件 `Background[40k_general_bt_yellow RT=1 pad(-20)⁴]` · 子件 `Icon[40k_general_bt_yellow_close RT=1 pad(-20)⁴]`；
        //     · `python -I d:/tmp/wf_hit/tgt.py bundle_menus_assets_all 5609434692533257010` ⇒
        //       **GO 名 = `Background` · 贴图 = `40k_general_bt_yellow`**（= 原版那颗 `EverguildButton` 的 `m_TargetGraphic`）。
        //   旁证（非唯一判据）：`资料/普查产出_第六会话/W_命中区批2.md` §⑧-2。
        //   🧨 **改坏法（两半各一条，任一改回必红）**：① **名字那半** —— 把 `Shell/InboxWindow.cs` 里画黄面那颗 quad 的
        //     节点名从 `"Background"` 改回 `"Icon"`（或把 `MenuDraw.Hit` 的 `target` 实参改去指**圆底盘那颗**
        //     `closeQ` —— 即根节点 `Generic Close Button Orange` 上那一颗）⇒ 红；
        //     ② **贴图那半** —— 把 `ArtCloseIcon`（`40k_general_bt_yellow`）换成根圆底盘 `UI_Button_Round_background` ⇒ 红。
        A1125Close("InboxWindow", inbox.transform, "Generic Close Button Orange", "Hit",
                   "Background", "40k_general_bt_yellow", "Background", 96.86f, 98.13f);
        // 🆕 **2026-10-18（B5）**：把上面那个**静默缺口**补上 —— 圆底盘那颗 quad 的**实绘矩形**两连断。
        //   期望值全是**原版 prefab 的读数**（B4 逐字段直读，⛔ 不从被测实现读回来）：
        //   · `Inbox Menu` 根 `Generic Close Button Orange` 的 `Image`：sprite `UI_Button_Round_background` ·
        //     `m_PreserveAspect=1` · `m_Type=0`(Simple) · 矩形 = **74.39×75.60**（= 本窗 `CloseBtn` 那一格）；
        //   · 那张 sprite 的 `m_Rect` = **237×237**（正方）· `m_Border`/`m_Offset` 全 0 ⇒ 等比内接 ⇒ 实绘 **74.39×74.39**。
        //   下面传的 `74.38f` = **实绘宽**（= 根那一格的宽；本窗 `CloseBtn` 1785.54→1859.92）；
        //   `74.38f, 75.60f` = **根那一格**（只进消息，用来说明「非正方的框 + 等比 ⇒ 高缩到宽那一档」）。
        //   ⚠️ 下面两格**各只钉一轴**（宽 / 宽==高）⇒ 两种改坏法各红一格，见 `A1125CloseBase` 的 doc。
        A1125CloseBase("InboxWindow", inbox.transform, "Generic Close Button Orange",
                       "UI_Button_Round_background", 74.38f, 74.38f, 75.60f);
        // ============================================================ 🆕 **2026-10-13（A473 · 落 H31 §四·3）**
        // 本窗三颗**窗级 TMP** 的自适应窗口 / 二分起点 / 折行 —— 这三笔账（A468）本件之前**一条断言都没有**。
        // 🔴 期望值全是**原版 prefab 的读数**（H31 §三·3 那三行 dump 原文**逐字抄**）：
        //   · `Inbox Menu/Content/Title`          = `'Inbox' 字号=48.0 **基准=36.0** auto[**18**~48] 对齐=Left/Middle **折行=0**`；
        //   · `…/Content/Message Display/Title`   = `'WELCOME TO…' 字号=58.0 **基准=36.0** auto[**10**~58] 对齐=Left/Middle **折行=0**`；
        //   · `…/Content/No News Warning`         = `'Game announcements…' 字号=50.0 **基准=36.0** auto[**12**~50] 对齐=Center/Middle **折行=0** 字距=2`。
        // ⚠️ `auto[...]` 那一列**只在 `m_enableAutoSizing` 开着时才打**（`工具/menu_dump.py:607`）
        //   ⇒ 它同时就是「原版开着自适应」的判据；⚠️ 三颗的 `min` **各不相同**（18 / 10 / 12）—— ⛔ 别按窗统一挑一个数。
        // ⚠️ `autoMaxPx` 三处都不传是**等价**的：原版那三档 `m_fontSizeMax`（48 / 58 / 50）**逐颗等于**我们传的 `fontPx`，
        //   而 `MenuDraw.Text` 在 `autoMaxPx <= 0` 时**退回 `fontPx`** ⇒ 下面 `CheckFontWindow` 的上界断的就是 `fontPx`。
        // ⚠️ 下面这两段（`inboxContent` 与两个读口）**故意留在本节作用域**（不裹 `{}`）——
        //   `Message Display` 那一颗要等到 `inbox.Initialize(fake);` **之后**才量得到（那时它才被打开）。
        var inboxContent = FindChild(inbox.transform, "Content");
        // ---- ★ A468（c）：`m_fontSizeBase`（= 自适应的**二分起点**）—— 只读 min/max 那两条**看不出**这一格 ----
        //   三颗都是 **36.0**（= TMP 序列化默认值 ⇒ 原版没显式设过）。改坏法：删掉 `autoBasePx: WindowLabelAutoBase`
        //   ⇒ base 退回 `fontPx`（48 / 58 / 50）⇒ 红。
        //   🔴 ⛔ **别改成断「渲染字号」**：base 只改自适应的二分起点、终点两侧都收敛（差 ≤ 0.05 fontSize 单位）
        //   ⇒ 量渲染是**弱断言**；这里读的是 `Label.FontSizeBase`（**反射读 TMP 的 `m_fontSizeBase` 真字段**）。
        // ---- ★ A468（d）：折行那一档（本件新还原的）—— `SetAutoFitBox` 内部 `SetWrapWidth` **无条件**开折行 ----
        System.Action<Transform, string, float> checkWinBase = (parent, nodeName, wantBasePx) =>
        {
            var tb = FindChild(parent, nodeName);
            var lbb = tb != null ? tb.GetComponentInChildren<Label>(true) : null;
            CheckTrue(lbb != null, $"★ A468：（前提）`{nodeName}` 那颗挂得到 `Label`（下一条要读它的 `m_fontSizeBase`）");
            CheckTrue(lbb == null || lbb.FontSizeBase > 0f,
                      $"★ A468：（前提）`{nodeName}` 走的是 TMP 后端 —— `Label.FontSizeBase` 在**点阵后端**返回 **−1**");
            CheckNear(lbb != null ? Label.FontSizeToPx(lbb.FontSizeBase) : -1f, wantBasePx, 0.6f,
                      $"★★ A468：`{nodeName}` 的 `m_fontSizeBase` = 原版 **{wantBasePx:F0}**（画布 px 口径；"
                      + "判据 = `Inbox Menu` prefab 的 `基准=36.0` 那一列，TMP 出厂默认值）"
                      + " —— 删掉那一处的 `autoBasePx: WindowLabelAutoBase` ⇒ 退回 `fontPx` ⇒ 红");
        };
        System.Action<Transform, string> checkWinWrap = (parent, nodeName) =>
        {
            var tw = FindChild(parent, nodeName);
            var lbw = tw != null ? tw.GetComponentInChildren<Label>(true) : null;
            CheckTrue(lbw != null, $"★ A468：（前提）`{nodeName}` 那颗挂得到 `Label`");
            Check(lbw != null ? lbw.WrappingMode : -2, 0,
                  $"★★ A468：`{nodeName}` 的 `折行` = 原版那一档 **0（NoWrap）**"
                  + "（判据 = 那三行的 `折行=0`；`Label.SetAutoFitBox` 内部会**无条件**开折行 ⇒ 不还原就是 1）"
                  + " —— 删掉 `Shell/InboxWindow.cs` 那句 `SetWrapping(false)` ⇒ 红"
                  + "（⚠️ 点阵后端 `WrappingMode` 返 **−1** ⇒ 那时这一条会红，属已知口径）");
        };
        // 🆕 A468④（报告里标「可选」的那一条，最直白）：`m_enableAutoSizing = 1`（= 原版 dump 的 `auto[…]` 那一列
        // 只在开着时才打，`工具/menu_dump.py:607`）。⚠️ 点阵后端 `AutoSizing` 恒 false（如实报）。
        System.Action<Transform, string> checkWinAuto = (parent, nodeName) =>
        {
            var ta = FindChild(parent, nodeName);
            var lba = ta != null ? ta.GetComponentInChildren<Label>(true) : null;
            CheckTrue(lba != null, $"★ A468：（前提）`{nodeName}` 那颗挂得到 `Label`");
            Check(lba != null && lba.AutoSizing, true,
                  $"★★ A468：`{nodeName}` 的 `m_enableAutoSizing` = **真**（原版那三颗都开着 ——"
                  + " `menu_dump` 印出 `auto[…]` 就说明它开着）—— 删掉那一处 `autoMinPx`（或 `autoMaxPx`）⇒ 红");
        };
        {
            // `CheckFontWindow(父, 子名, …)` 内部 = **按名字递归找** + **单参** `GetComponentInChildren<Label>()`
            // （只找**激活**的）⇒ 只对**开着的**件有效 —— 所以 `Message Display` 那一颗落在下面
            // `inbox.Initialize(fake);` **之后**（那时它已被 `ApplyEmptyState` 打开）。
            CheckFontWindow(inboxContent, "Title", 18f, 48f,
                            "★★ A468：`Content/Title` 的自适应窗口 = 原版 `auto[18~48]`"
                            + "（删掉 `Shell/InboxWindow.cs` 那两处实参 `Title.W` / `TitleAutoMin` ⇒ 整条自适应不跑 ⇒ 红）");
            CheckFontWindow(inboxContent, "No News Warning", 12f, 50f,
                            "★★ A468：`No News Warning` 的自适应窗口 = 原版 `auto[12~50]`"
                            + "（⚠️ 三颗的 `min` 各不相同：18 / 10 / 12 —— ⛔ 别按窗统一挑一个数；"
                            + "把 `NoNewsAutoMin` 换成 18 ⇒ 红）");

            // ---- ★ A468（c）/（d）：那两个读口声明在**本节作用域**（见上面那两个 `System.Action` ——
            //   它们还要给 `Message Display/Title` 再用一次）⇒ 这里只调用。
            checkWinBase(inboxContent, "Title", 36f);
            checkWinWrap(inboxContent, "Title");
            checkWinAuto(inboxContent, "Title");
            checkWinBase(inboxContent, "No News Warning", 36f);
            checkWinWrap(inboxContent, "No News Warning");
            checkWinAuto(inboxContent, "No News Warning");
        }

        // ============================================================ 🆕 **2026-10-13（A478 · 落 H35 §四）**
        // **A470①（两颗 `Title` 的**水平对齐**）+ A471（`No News Warning` 的**字距**）** —— 这两笔本件之前**真空白**
        // （grep 过本文件收件箱整节：只断「开/关」，**没有一条量对齐或字距**）。
        // 🔴 判据全是**原版读数**（H35 §三·1 的两行 dump 原文**逐字抄**，列名→字段见 `工具/menu_dump.py:609-613`）：
        //   · `Inbox Menu/Content/Title` = `'Inbox' … 对齐=**Left**/Middle 折行=0`（`m_HorizontalAlignment = 1`）；
        //   · `…/Content/No News Warning` = `… 对齐=Center/Middle 折行=0 … **字距=2**`（`m_characterSpacing = 2`）。
        //   ⚠️ 第三颗（`No News Warning`）原版是 `Center/Middle` ⇒ 与我们默认**一致**，本件**不做对齐**。
        {
            // ---- ★ A470①：`Content/Title` 的**渲染左缘** = 原版框左沿 **121.47** ----
            // 🔴 期望值写【字面量】121.47（= 原版 prefab `Inbox Menu > Content > Title` 的 rect 左沿，
            //   `工具/menu_rect.py` 实读）—— ⛔ 别改成 `InboxWindow.Title.x1`（那是被测实现**传进去的实参**
            //   = 同式自证：把它改错这一条会跟着变绿）。
            //   ⚠️ 找法：本窗树里有**两颗** `Title` ⇒ 用 `FindPath` 写**全路径**（`FindChild` 是递归按名字找）。
            // 🔴 顺序那一半（H35 §四·A470③）：对齐**必须排在 `SetWrapping(false)` 之后** ——
            //   `MenuDraw.AlignLeft` 走 `Label.AlignLeftOn`，它按**当时的 `WorldW`** 反推整块的位置
            //   （`Battle/Label.cs` 的 `AlignLeftOn`），而 `SetWrapping` 会改量到的宽度 ⇒ 先对齐后改折行 = 左缘偏
            //   `(旧宽 − 新宽)/2`。⚠️ **那一半没有独立断言**（`SetWrapping(false)` 对短文案改的宽度可能 ≈ 0）
            //   ⇒ 只能靠改坏法人工验一次：把 `Shell/InboxWindow.cs` 里那两行 `TitleLeft(...)` 上移一句，
            //   期望本文这一条或下面 `Message Display` 那条变红。
            var tTitleL = FindPath(inbox.transform, "Content/Title");
            float ltx1, lty1, ltx2, lty2;
            CheckTrue(RectOf(tTitleL, out ltx1, out lty1, out ltx2, out lty2),
                      "★ A470①：（前提）`Content/Title` **在树上**、且量得出**渲染**矩形"
                      + "（`RectOf` 取的是 `Label` 自己的 transform、节点不在时它自己返回假，见它的注释）");
            CheckNear(ltx1, 121.47f, 1.5f,
                      "★★ A470：`Content/Title` 的**渲染左缘** = 框左沿 **121.47**（原版 `对齐=Left/Middle`；"
                      + "我们这一侧 `MenuDraw.Text` 根本没有对齐形参、`TmpFont.NewText` 统一建 `Center` ⇒ 原来停在框心）"
                      + " —— 删掉 `Shell/InboxWindow.cs` 那句 `TitleLeft(titleLb, Title);` ⇒ 红");

            // ---- ★ A471：`No News Warning` 的**字距** = 原版 `m_characterSpacing` **2** ----
            // 🔴 期望值 2 是原版 dump 的字面量（`字距=` 那一列 = `工具/menu_dump.py:612-613` **原样打印**
            //   `m_characterSpacing`，值为 0 时不印）—— ⛔ 不是 `NoNewsCharSpacing` 那个我们自己的常量。
            //   **改坏法**：删掉 `Shell/InboxWindow.cs` 那句 `_warn.SetCharSpacing(NoNewsCharSpacing);` ⇒ 0 ⇒ 红；
            //   改成 1/5 ⇒ 也红（值不是随手挑的）。
            // ⚠️ **如实记它咬不到什么**：尾上那句 `_warn.ForceRelayout();` **没有独立断言**
            //   （`CharSpacing` 直接读 TMP 字段、与 `Label` 的度量缓存无关 ⇒ 删了它这一条照样绿）。
            // ⚠️ 点阵后端 `SetCharSpacing` **出声但无效**（`Battle/Label.cs` 的 `SetCharSpacing`），`CharSpacing` 那时读 0。
            var warnLb = warnNode != null ? warnNode.GetComponentInChildren<Label>(true) : null;
            CheckTrue(warnLb != null, "★ A471：（前提）`No News Warning` 挂得到 `Label`");
            CheckNear(warnLb != null ? warnLb.CharSpacing : float.NaN, 2f, 0.01f,
                      "★★ A471：`No News Warning` 的字距 = **2**（原版 `m_characterSpacing`）"
                      + " —— 删掉 `Shell/InboxWindow.cs` 那句 `_warn.SetCharSpacing(NoNewsCharSpacing);` ⇒ 0 ⇒ 红");
        }
        var msgList = FindChild(inbox.transform, "Message List");
        // 🔴 **2026-10-08（A182 收尾）**：这条原来写「`Message List` **出厂就是 0 个子件**（原版如此）」——
        //    **对原版的描述是错的**，而且 A182 之后我们的形状也变了（多了一层 `Viewport`）⇒ **必红**。
        //    · **原版判据**（**亲跑** `工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6`）：
        //      `Message List`（第 2 层 · `ScrollRect` h=0 v=1）→ **`Viewport`**（第 3 层 · `RectMask2D`）→
        //      `Content`（第 4 层 · `VerticalLayoutGroup` spacing 7 + CSF `v:MinSize=0`）
        //      ⇒ 原版 `Message List` **有 1 个子件**，而 **0 子件的是 `Viewport` 底下那颗 `Content`**。
        //    · **错因**：写这句的时候我们那棵树只有一个空节点（`MenuDraw.Node(c, "Message List", …)`）
        //      ⇒ **把自己的形状记成了原版**（波 C1 报告 §0·2 已记）。
        //    ⚠️ 「0 子件」这一条**必须留在灌数据之前**：下面 A182 那一段要 `Initialize(8 条)`
        //      （`Content` 底下会建出 8 个条目）—— 挪到那儿就是假红。
        CheckTrue(msgList != null && msgList.childCount == 1,
                  "`Message List` 出厂有 **1 个**子件（原版如此；条目 prefab 由服务端事件数据决定 ——"
                  + " 判据 = `menu_dump.py … Inbox Menu --depth 6`：它底下只有 `Viewport` 一颗）");
        var msgListVp = FindPath(inbox.transform, "Content/Message List/Viewport");
        CheckTrue(msgList != null && msgList.childCount == 1 && msgList.GetChild(0) == msgListVp,
                  "…那唯一一颗子件是 `Viewport`（掩码 / 裁切那一层；原版父链 = Message List/Viewport/Content）");
        var msgListContent = FindPath(inbox.transform, "Content/Message List/Viewport/Content");
        CheckTrue(msgListContent != null && msgListContent.childCount == 0,
                  "**0 子件**的是 `Viewport` 底下那颗 `Content`（原版出厂就是 0 ——"
                  + " `menu_dump.py … Inbox Menu --depth 6` 里 `Content` 那一行底下一条都没有；"
                  + "条目由服务端事件数据决定）");
        CheckTrue(FindChild(inbox.transform, "Reset Button") == null,
                  "**不建** `Reset Button`（原版 `m_OnClick` 空 + `DebugReset` 零调用者 + `Open()` 每次关它）");
        Check(inbox.MissingArt.Count, 0, "收件箱没有取不到的图");
        CheckHoverSwap(inbox.transform, "收件箱");
        // ============================================================ 🆕 **2026-10-12（A352）**
        // 🔴 **这张图原来拍的【不是收件箱】** —— 整屏是一扇**遗留的 `Reward Window`**
        //    （"Rewards claimed" / "Get Reward" / "Click to continue"，收件箱只在后面透出来；
        //     亲读记录 → `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §六·1）。
        // 🔴 **`Shoot` 那条「平均亮度 > 3」护栏抓不到这一类** —— 那扇窗是**亮的**，它只挡「拍了个空」。
        // **成因（已修，本文件 `Build()` 那句 `WindowsManager.EnsureHost()`）= 领奖窗落到了【第二台】
        //    `WindowsManager` 上**（DIAG-B §二·#1/#2 那 8 条红的唯一源头）⇒ `wm2.CloseAllWindows()` 关不掉它。
        // ⇒ 两道**直接判据**，都用「现读的现场」当尺子（⛔ 不靠亮度、不靠人去看）：
        //    ① **顶窗就是收件箱**、② **场上没有任何开着的领奖窗**（扫**全部**管理器，见 `OpenRewardWindowCount`）。
        CheckTrue(wm2.TopWindow == inbox,
                  "★ A352：（前置）拍 `05_收件箱_空态.png` 时**顶窗就是收件箱**"
                  + "（拍的是别的东西时这条直接红 —— 亮度护栏对此无能为力）");
        Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
              "★ A352：（前置）这一刻场上**没有任何开着的 `Reward Window`**（= 那一张曾经拍到的正是它；"
              + "**改坏法**：① 删掉 §九 末尾那两句 `wm2.CloseAllWindows()`（`:6170` · `:6172`）"
              + " ⇒ 那一节 (i) 造的 `rwN` 留在场上 ⇒ 红；② 把 `Build()` 的 `EnsureHost()` 换回手抄那份"
              + "（DIAG-B §二·#1 的原样重现）⇒ 自 A313 起四条 `Collect*` 弹的窗落到**第二台**管理器上，"
              + "`wm2.CloseAllWindows()` 关不掉 ⇒ 也是红）");
        Shoot("05_收件箱_空态.png");
        // ---- ★ **A352 的字节判据**（同 A300 的手法）：这张图**不许与本轮任何另一张图字节相同** ----
        // 判别力 = 「同一张照片被挂两个名字」那一族（A300 就是靠它发现的：`01_日常_Missions.png` 与
        // `03_锻造厂.png` **字节完全相同** = 同一页拍了两次，而**当时一条断言都不报**）。
        // 🔴 **如实标注它【拦不到】什么**（⛔ 别以为这一条能替代上面那两条）：A352 那一次的遗留窗
        //    本轮**并没有被拍过** —— 它是 §九(a) 那条路（点每日奖励抽屉 ⇒ `CollectReward` ⇒ `ShowCollectedWindow`）
        //    开出来的那一扇，参数上写着 `1 条 / TierBasic / IsPremiumLocked=true（形参默认）/ OnCollect=null`
        //    （`Shell/DailyData.cs` 那个三参重载），**揭示还停在 0**（`Show` 造成之后没人 `Tick` 它 ——
        //    这一句是**按代码推断**，我没跑 Unity 复核）；而 `07`/`07b` 那两张是另一档
        //    （`2 条 / IsPremiumLocked=false / 有 Collect / 揭示=1`）⇒ **字节这一条当时不会红**。
        //    ⇒ 拦那一次的是上面那两道「现场」判据；这一条是 A300 那一族的**通用网**。**两条并存**。
        string dupOf = SameBytesAsAny("05_收件箱_空态.png",
                                      "01_日常_Missions.png", "02_战役.png", "03_锻造厂.png",
                                      "03b_锻造厂_不可领.png", "03_每日奖励.png", "04_每日连登.png",
                                      "07_奖励窗_Get.png", "07b_奖励窗_Preview.png", "02b_战役奖励窗.png");
        Check(dupOf, (string)null,
              "★ A352：`05_收件箱_空态.png` 与本轮**任何另一张图都不同字节**"
              + "（与某一张撞了 ⇒ 拍的是同一张照片；A300 那次的形状就是这样 —— 红的这一行会印出撞的那张的名字）");

        // ============================================================ 🆕 **2026-10-08（A182）**
        // `Message List` 的**视口 / 裁切 / 软边 / 滚动**四件 —— 本件之前只有一个**空节点**
        // （`MenuDraw.Node(c, "Message List", MsgList)`：没有 `Viewport`、全文件 0 处 `Clip`）。
        // 判据（直读原版，⛔ 期望值写原版字面量）：
        //   · 结构 → `工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6`：
        //     `Message List`(`ScrollRect` · **h=0 v=1 mode=1** Elastic) → `Viewport`(`RectMask2D`) →
        //     `Content`(`VerticalLayoutGroup` spacing **7** · align 0 · **reverse=1** + CSF v MinSize)；
        //     三层**同矩形** 99.57,211.98 → 728.28,990.77；
        //   · 掩码字段 → `d:/4/_tmp_view/q1_rm2d.txt:250-251`（`soft=(0,25) pad=(0.0,0.0,0.0,0.0) en=1`）；
        //   · 条目模板 → `bundle_menus_assets_all/GameObject/Message Container`（`menu_dump` 实读：
        //     条目 **628.71×140.70**；`Title` 50px `(0.808,0.808,0.808)` · `Date` 40px `(0.953,0.663,0.404)` ·
        //     `New` 40px `(0.996,0.745,0.314)` 右对齐）。
        // 🔴 单机没有消息（`DailyData.InboxCount` 恒 0）⇒ 这里**灌 8 条假消息**才验得动裁切：
        //    视口高 778.79 装不下 8×140.7 + 7×7 = **1174.60** ⇒ 真有条目落在视口外。
        {
            var listVp = FindPath(inbox.transform, "Content/Message List/Viewport");
            CheckTrue(listVp != null, "★ `Message List` 底下有 `Viewport` 那一层（掩码长在它身上）");
            // ⚠️ `Viewport` 是**纯容器节点**（不带渲染 ⇒ 没有 `ImageQuad`）⇒ 只量位置，别用 `CheckRectPx`。
            CheckAt(listVp, 99.57f, 728.28f, 211.98f, 990.77f,
                    "`Viewport` 摆在原版那个矩形里（原版三层**同矩形** 99.57,211.98 → 728.28,990.77）");
            CheckTrue(FindPath(inbox.transform, "Content/Message List/Viewport/Content") != null,
                      "`Content` 挂在 **`Viewport` 之下**（原版父链 = Message List/Viewport/Content）");
            CheckTrue(inbox.ListScroll != null, "滚动区建出来了（`MenuScroll`；原版那颗 `ScrollRect` 是**活的** h=0 v=1）");
            // 🔴 2026-10-09（A269）：期望值 **0** 的判据 = UGUI `ScrollRect.AdjustBounds`
            //    （本地真源码 `MyGame/Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs:1332-1352`）：
            //    *"Scrolling is **only** possible when content is **larger** than view"* ⇒ 内容比视口小时
            //    content bounds 被撑到 view 大小 ⇒ **两个方向的极值都是 0**。
            //    ⚠️ **改坏法**：把 `MenuScroll.MaxOffset` 退回 `ContentX2 - ViewHi` 的裸值 ⇒ 这里给 −778.79（= −视口高）⇒ 红。
            CheckNear(inbox.ListScroll != null ? inbox.ListScroll.MaxOffset : -9999f, 0f, 0.01f,
                      "**空列表** ⇒ 可滚**下**极值 = 0（内容高 0 —— 与原版 prefab 出厂那颗 `ContentSizeFitter` 给的高一致）");
            CheckNear(inbox.ListScroll != null ? inbox.ListScroll.MinOffset : 9999f, 0f, 0.01f,
                      "**空列表** ⇒ 可滚**上**极值 = 0（同一条 `AdjustBounds`：内容比视口小时两个方向都滚不动）");

            // ---- ★ 灌 8 条：内容真的超出视口 ----
            var fake = new List<InboxWindow.Message>();
            for (int i = 0; i < 8; i++)
                fake.Add(new InboxWindow.Message("Message " + i, "12/06/2023 00:0" + i, i == 0));
            inbox.Initialize(fake);
            Check(inbox.Rows.Count, 8, "灌 8 条 ⇒ 建出 8 个条目节点（`reverse=1` 只改视觉次序、不改条数）");
            // ============================================================ 🆕 **2026-10-13（A473 / A478）**
            // 落到这里的原因（**落点本身是判据的一部分**）：**空态下 `Message Display` 整块是关的**
            // （`ApplyEmptyState`），而 `CheckFontWindow` 内部是**单参** `GetComponentInChildren<Label>()`、
            // `RectOf` 同理（只找**激活**的）⇒ 空态下它们会报「这条没查成」（那是**假红**、不是缺陷）。
            // `Initialize(fake)` 之后 `Message Display` 已被 `ApplyEmptyState` 打开 ⇒ 下面三件都量得到：
            //   · A468(b) 那颗的**自适应窗口** = 原版 `auto[10~58]`（`menu_dump … "Inbox Menu" --depth 6` 原文）；
            //   · A468(c)/(d) 那一颗的 `m_fontSizeBase`（**36.0**）与 `折行`（**0**）；
            //   · A470② 那一颗的**渲染左缘** = 原版框左沿 **791.02**（`对齐=Left/Middle`）。
            // ⚠️ 父节点传 `Message Display` **自己**：本窗树里有**两颗都叫 `Title`**
            //   （`Content/Title` 与 `Content/Message Display/Title`），而 `FindChild` 是按名字**递归**找、
            //   取的是**先序第一个**（= `Content/Title`）⇒ 传 `Content` 会断到错的那一颗。
            var mdContent = FindPath(inbox.transform, "Content/Message Display");
            CheckFontWindow(mdContent, "Title", 10f, 58f,
                            "★★ A468：`Message Display/Title` 的自适应窗口 = 原版 `auto[10~58]`"
                            + "（⚠️ 它是**三颗里唯一 `min = 10`** 的 —— 别按窗统一挑一个数；"
                            + "删掉 `Shell/InboxWindow.cs` 那两处实参 `MdTitle.W` / `MdTitleAutoMin` ⇒ 红）");
            checkWinBase(mdContent, "Title", 36f);
            checkWinWrap(mdContent, "Title");
            checkWinAuto(mdContent, "Title");
            // ---- ★ A470②（H35 §四）：`Message Display/Title` 的**渲染左缘** = 原版框左沿 **791.02** ----
            //   🔴 期望值写【字面量】791.02（= 原版 prefab `Inbox Menu > Content > Message Display > Title`
            //   的 rect 左沿）—— ⛔ 别改成 `InboxWindow.MdTitle.x1`（那是被测实现传进去的实参 = 同式自证）。
            //   原版那一颗的 `m_HorizontalAlignment = 1 (Left)`（判据 = `menu_dump.py … "Inbox Menu" --depth 6`
            //   的 `对齐=Left/Middle` 那一列）。
            var mdTitleL = FindPath(inbox.transform, "Content/Message Display/Title");
            float mdx1, mdy1, mdx2, mdy2;
            CheckTrue(RectOf(mdTitleL, out mdx1, out mdy1, out mdx2, out mdy2),
                      "★ A470②：（前提）`Message Display/Title` **在树上**、且量得出**渲染**矩形"
                      + "（**必须在 `Initialize(fake)` 之后** —— 空态下它整块是关的、单参 `GetComponentInChildren` 取不到）");
            CheckNear(mdx1, 791.02f, 1.5f,
                      "★★ A470：`Message Display/Title` 的**渲染左缘** = 框左沿 **791.02**（原版 `对齐=Left/Middle`）"
                      + " —— 删掉 `Shell/InboxWindow.cs` 那句 `TitleLeft(mdTitleLb, MdTitle);` ⇒ 停在框心 ⇒ 红");
            CheckNear(inbox.ListScroll != null ? inbox.ListScroll.MaxOffset : -9999f, 395.81f, 0.5f,
                      "可滚下极值 = 内容高 1174.60 − 视口高 778.79 = **395.81px**"
                      + "（⛔ 空列表时它必须是 0，上面那条钉着）");

            // ★ `reverse=1`：**树的最后一个子件在视觉最上面** ⇒ 树序第 1 个 = 视觉最下那条
            //   （`Rows[0]` 的 y = 211.98 + 7×147.7 = 1245.88 → 1386.58，**整块在视口外**）
            var rowBottom = inbox.Rows[0];
            var rowTop = inbox.Rows[7];
            CheckTrue(rowBottom != null && rowTop != null, "树序两端那两个条目节点都在");
            CheckTrue(rowBottom != null && FindChild(rowBottom, "Background") == null,
                      "★ 视觉最下那条（y 1245.88..1386.58，**整块在视口外**）**底图没建**"
                      + "（`reverse=1` 实现反了的话，落在这里的是另一条 ⇒ 会红）");
            CheckTrue(rowTop != null && FindChild(rowTop, "Background") != null,
                      "视觉最上那条（y 211.98..352.68）**画出来了**（⛔ 不许「一律不建」蒙对上面那条）");
            // ★ 条目**内部**的摆位 —— 这一族最容易静默错：条目局部坐标 → 画布坐标要加**条目的左上角**，
            //   写成「相对视口的偏移」会让整条带子平移到别处，而**矩形断言量不到内部的相对摆位**。
            //   ⚠️ 底图用**并集**量（软边会沿 y 把它切成两块 —— 只量主格会得到半条，见 `RectOfUnion` 的注释）。
            float ax1, ay1, ax2, ay2;
            CheckTrue(RectOfUnion(FindChild(rowTop, "Background"), out ax1, out ay1, out ax2, out ay2),
                      "最上面那条的底图量得到渲染并集");
            CheckNear(ax1, 99.57f, 0.5f, "…左边缘 = **视口左沿 99.57**（原版条目宽 628.71 正好铺满视口）");
            CheckNear(ax2, 728.28f, 0.5f, "…右边缘 = **视口右沿 728.28**");
            CheckNear(ay1, 211.98f, 0.5f, "…上边缘 = **视口顶 211.98**（第一条就贴在视口顶上 —— 内容上对齐）");
            CheckNear(ay2, 352.68f, 0.5f, "…下边缘 = 211.98 + 条目高 140.70 = **352.68**");
            // `Title`：局部 (20, 20.3) → 屏幕左边缘 = 99.57 + 20 = 119.57、竖向中心 = 211.98 + 51.65
            CheckTrue(RectOf(FindChild(rowTop, "Title"), out ax1, out ay1, out ax2, out ay2),
                      "…它的 `Title` 量得到渲染矩形");
            CheckNear(ax1, 119.57f, 0.5f,
                      "★ `Title` 的**左边缘 = 99.57 + 20 = 119.57**（原版 `Left/Capline`；"
                      + "局部坐标算成「相对视口的偏移」会差 99.57px，居中会差出半个文字宽）");
            CheckNear((ay1 + ay2) * 0.5f, 263.63f, 0.5f, "…竖向中心 = 211.98 + (20.3+83)/2 = 263.63");

            // ---- ★ A459（2026-10-13，H26 §三·3 待接线）：三行文字的 `m_fontSizeBase` = 原版 **41.0** ----
            // 🔴 期望值 **41.0 是原版 prefab 的原文**（`python 工具/menu_dump.py bundle_menus_assets_all
            //   "Message Container" --depth 8 --md --no-sprite` 那三行逐字：「`Title` 字号=50.0 **基准=41.0**
            //   auto[18.0~50.0]」·「`Date` 40.0 **41.0** auto[18.0~40.0]」·「`New` 40.0 **41.0** auto[18.0~40.0]」）
            //   ⇒ ⛔ **不是**读我们自己的常量（`Shell/InboxWindow.cs` 的 `RowTitleAutoBase` 传进去的就是 41
            //   —— 拿它比 = 自证；这条读的是 **TMP 那个真字段**）。
            // 🔴 判据链：`Label.FontSizeBase` 是**反射读** `TMP_Text.m_fontSizeBase`（它没有公开访问器），
            //   `Label.FontSizeToPx` 是**唯一那条 px 口径**（`SetAutoFitBox` 里 `baseCur = cur × basePx/nomPx`
            //   ⇒ `FontSizeToPx(base)` **就是**调用方传的那个 px 数）。
            //   改坏法：删掉 `Shell/InboxWindow.cs` 里 `RowText(…, RowTitleAutoBase, 1)` 那三处实参
            //   （现读 `:477` / `:479` / `:482`，或让 `RowText` 不再往下传 `autoBasePx`）⇒
            //   base 退回 `fontPx` = 50 / 40 / 40 ⇒ **三条全红**。
            //   ⛔ 别只断「渲染字号」：base 只改自适应的**二分起点**、终点两侧都收敛（差 ≤ 0.05 fontSize 单位）
            //   ⇒ 量渲染是**弱断言**。
            // ⚠️ **`rowTop` 是数据下标 0 那一条**（映射见下一段）：`fake[0]` 是**唯一 unread** 的
            //   ⇒ 只有它三颗齐（`Title` / `Date` / `New`）—— 这是本文件里唯一能三条一次断完的位置。
            {
                System.Action<string, float> checkRowBase = (nodeName, wantPx) =>
                {
                    var t2 = FindChild(rowTop, nodeName);
                    var lb2 = t2 != null ? t2.GetComponent<Label>() : null;
                    CheckTrue(lb2 != null,
                              $"★ A459：（前提）`{nodeName}` 那颗挂得到 `Label`（下一条要反射读它的 `m_fontSizeBase`）");
                    CheckTrue(lb2 == null || lb2.FontSizeBase > 0f,
                              $"★ …（前提）`{nodeName}` 走的是 TMP 后端 —— `Label.FontSizeBase` 在**点阵后端**返回 **−1**");
                    CheckNear(lb2 != null ? Label.FontSizeToPx(lb2.FontSizeBase) : -1f, wantPx, 0.6f,
                              $"★★ A459：`{nodeName}` 的 `m_fontSizeBase` = 原版 **{wantPx:F0}**（画布 px 口径；"
                              + "判据 = `Message Container` prefab 的 `m_fontSizeBase` 原文）"
                              + " —— 删掉 `Shell/InboxWindow.cs` 那一处的 `autoBasePx` ⇒ 退回 `fontPx`（50/40）⇒ 红");
                };
                checkRowBase("Title", 41f);
                checkRowBase("Date", 41f);
                checkRowBase("New", 41f);      // `rowTop` = `fake[0]`（唯一 unread）⇒ 这颗在
            }

            // ---- ★ A467（2026-10-13，落 H31 §四·2）：三行文字的 `m_TextWrappingMode` = 原版 **0** ----
            // 🔴 期望值 **0 是原版 dump 的字面量**：`python 工具/menu_dump.py bundle_menus_assets_all
            //   "Message Container" --depth 8 --md --no-sprite` 那三行的 `折行=` 列**逐颗都是 `0`**
            //   （`Title` 50px / `Date` 40px / `New` 40px）—— ⛔ 不是读我们自己的常量。
            //   我们这一侧的改动 = `RowText` 里那句**转调 `Label.SetWrapping(false)`**（照原版那一档还原）。
            // 🔴 **为什么这条有牙**：`WrappingMode` 直接读 TMP 的 `textWrappingMode`（`Battle/Label.cs` 的 `WrappingMode`）
            //   ⇒ 字段级唯一判别式（夹具那 8 条是 `"Message N"` 这种**短文案**，**折不折行渲染上都一样**
            //   ⇒ 想拿渲染级证据得另起一条长文案的场景，⛔ 别改现有夹具的文案 —— `:6854` 那一带几条几何断言
            //   都建在它上面）。
            //   **改坏法**：删掉 `Shell/InboxWindow.cs` 的 `RowText` 里那句 `lb.SetWrapping(false)`
            //   ⇒ 退回 **1**（= `SetAutoFitBox` 内部 `SetWrapWidth` **无条件**开的那一档）⇒ 三条全红。
            // ⚠️ **点阵后端**（没有 TMP 资产时）`WrappingMode` 返 **−1** ⇒ 那时这一条会红，属已知口径。
            {
                System.Action<string> checkRowWrap = (nodeName) =>
                {
                    var tw = FindChild(rowTop, nodeName);
                    // ⚠️ 取组件用**含 `includeInactive`** 那一版：单参那版只找**激活**的
                    //   （`Date` / `New` 在**非 unread** 的条目上是关的 —— 这里虽然走的是 unread 那条，仍照口径写全）。
                    var lbw = tw != null ? tw.GetComponentInChildren<Label>(true) : null;
                    CheckTrue(lbw != null, $"★ A467：（前提）`{nodeName}` 那颗挂得到 `Label`");
                    Check(lbw != null ? lbw.WrappingMode : -2, 0,
                          $"★★ A467：`{nodeName}` 的 `m_TextWrappingMode` = 原版 **0（NoWrap）**"
                          + "（判据 = `Message Container` prefab 那三行的 `折行=0`）"
                          + " —— 删掉 `RowText` 里那句 `SetWrapping(false)` ⇒ 退回 1 ⇒ 红");
                };
                checkRowWrap("Title");
                checkRowWrap("Date");
                checkRowWrap("New");     // `rowTop` = `fake[0]`（唯一 unread）⇒ 这颗在
            }

            // ---- ★ 真裁住了：压在视口底沿的那条被截到视口内 ----
            // 🔴 **下标映射**（`BuildMessageRows` 从数据末尾往前建 ⇒ 树序 = 数据序的倒序）：
            //   `Rows[k]` 装的是**数据下标 `n−1−k`**；而 `reverse=1` 又把树序倒排回视觉序
            //   ⇒ **视觉第 v 行 = 数据下标 v = `Rows[n−1−v]`**。视口底 990.77 落在视觉第 5 行
            //   （y 950.48..1091.18）⇒ 就是**数据下标 5 = `Rows[2]`**。
            float rx1, ry1, rx2, ry2;
            var rowMid = inbox.Rows[inbox.Rows.Count - 1 - 5];
            CheckTrue(RectOfUnion(FindChild(rowMid, "Background"), out rx1, out ry1, out rx2, out ry2),
                      "压在底沿的那一条（视觉第 6 行 · y 950.5..1091.2）量得到渲染矩形");
            CheckTrue(ry2 <= 991f,
                      $"★ 它的**渲染并集下边缘 ≤ 视口底 990.77**（实测 {ry2:F1}）—— 未裁时会画到 1091.2（出框 100px）");

            // ---- ★ 软边 (0,25)：横切线只在带的内沿 236.98 / 965.77；一条竖切线都没有 ----
            var LiVp_ = new PxRect(99.57f, 211.98f, 728.28f, 990.77f);              // `Content/Message List/Viewport`（同上一条 `CheckAt`）
            var hCuts = ScanSoftCuts(listVp, false, LiVp_);
            CheckTrue(hCuts.Count > 0,
                      "★ 有横向渐隐切线（= 软边真接上了；一条都没有 ⇒ `ClipSoftness` 没设）");
            for (int i = 0; i < hCuts.Count; i++)
                CheckTrue(Mathf.Abs(hCuts[i] - 236.98f) <= 0.6f || Mathf.Abs(hCuts[i] - 965.77f) <= 0.6f,
                          $"横切线 #{i + 1} 在 {hCuts[i]:F2} ⇒ 必须落在带的内沿：上 `211.98 + 25 = 236.98`"
                          + " 或下 `990.77 − 25 = 965.77`（⚠️ 写成 `(25,0)` 会去切**竖线**、写成 50 会切在 261.98/940.77）");
            Check(ScanSoftCuts(listVp, true, LiVp_).Count, 0,
                  "**一条竖切线都没有** —— 这一处软边是 `(0,25)`：**x 方向是硬边**"
                  + "（`(25,0)` 那种写反的实现这里会冒出一堆竖切线 —— 而且它**同时**会让上面那两条横切线消失）");

            // ---- 🆕 **2026-10-09（A273）**：条目底图的**悬停换图**（原版 `Message Container/Content`
            //   那颗 `Selectable` 是 `trans=2 (SpriteSwap)`）----
            //   判据（**原版实读**，`python 工具/menu_dump.py bundle_menus_assets_all "Message Container" --depth 4`）：
            //     `Content` 那颗 `Image` = **`40K_settings_button`** · `**trans=2**` · `interactable=1`
            //     · **`HL=40K_settings_button_hover`** · `P=40K_settings_button_pressed`。
            //   🔴 **改坏法**（两条，都实测过机理）：
            //     ① 把 `Shell/InboxWindow.cs` 里 `AddHit(…, ArtRowHover, …)` 那张悬停图去掉（只传常态图）
            //        ⇒ `HoverTexForTest` 变成 **`40K_settings_button_selected`**（`WindowButton.HoverNames`
            //        那条给聊天页签 Toggle 的逐颗覆盖会顶上）⇒ 下面「悬停图 = …_hover」与「底图真的换了」两条红；
            //     ② 把命中区档 `QRowHit` 退回 `QPanel` ⇒ 本窗吸收层（= `QOverlay − 1` = 3013）按
            //        `PointerLayer.HitButton`「队列大的先」把它整个盖住 ⇒ `pl.HoverAt` 命中的**不是**这一颗 ⇒ 红。
            //   ⚠️ 期望值一律是**原版 `m_SpriteState` 的字面量**，⛔ 不是我们 `Bind` 传进去的实参。
            {
                // ✅ **2026-10-10（A287）已按修法 ① 落地：本块整体前移到 `CheckAbsorbRule` 之前**（那边窗还开着）。
                //    · **原先为什么红**（留档）：本块原来跑在 `CheckAbsorbRule("收件箱窗", …)` **之后**，
                //      而那一段的**第 ⑥ 步就是「点面板外 ⇒ 窗 `Closed`」** ⇒ **探针跑的时候窗已经关了**，
                //      本块结尾那条 `plHov.HoverAt(中心)` 返回 **`<null>`**（不是命中错对象）⇒
                //      连带「底图真的换成悬停图」也红；而**本行的几何 4 条与档（3014）当时就全对**。
                //    · **判据（为什么可以前移）**：`CheckAbsorbRule`（本文件 `:191-305`）**不读 `inbox.Rows`、
                //      不依赖灌过数据的树** —— 它只用三样：调用点传进去的**矩形字面量**、`PointerLayer`、
                //      `() => inbox.CurrentState`；而本块自己的收尾（`Initialize(null)` + 空态两条）就在块末
                //      ⇒ 它看到的树**仍是空态**（与它原本的设计一致，与挪动前逐字相同）。
                //      ⛔ **不许再把本块挪回 `CheckAbsorbRule` 之后**（那样探针又会跑在关着的窗上）。
                //    · ⚠️ **别再试「就地重开窗」**（`inbox.TryOpen(null)`）：实测它**重建内容 ⇒ 上面灌的 8 条假消息没了**；
                //      补 `Initialize(fake)` 后本块两条确实转绿 ✅，**但它会把后面的 `Collect` 两条弄红** ⇒ 净亏、已回退。
                //    · **⛔ 别删这两条**（它们是 A273 唯一的「悬停真的发生了」证据，而 A273 的实现已确认没问题）。
                //      也不许降级成「不开指针层」的等价断言（量 `HoverTexForTest` + 自己算赢家）—— 那就不是真鼠标路径了。
                // 取**视觉第 1 行**（y 359.68..500.38）：整条落在视口里、且**不在软边带**（236.98..965.77）内
                // （`Rows[k]` 装的是数据下标 `n−1−k`，n = 8 ⇒ 这一条 = 数据下标 1，见上面那条下标映射）
                var rowHov = inbox.Rows.Count > 6 ? inbox.Rows[6] : null;   // ⚠️ 条数不对时上面那条已经红了 ⇒ 这里别越界崩
                CheckTrue(rowHov != null, "用于悬停探针的那一条（视觉第 1 行 · 数据下标 1）在");
                var rowHitN = FindChild(rowHov, "RowHit");
                var rowWb = rowHitN != null ? rowHitN.GetComponent<WindowButton>() : null;
                CheckTrue(rowWb != null && rowWb.target != null,
                          "★ 条目有**命中区**（`RowHit`）且挂上 `WindowButton`、换图那一层也绑上了（A273）");
                CheckTrue(rowWb != null && rowWb.onClick != null,
                          "…`onClick` 也挂着（⛔ 不许空 lambda：正文那几件没建 ⇒ 这一下要**出声**说没做）");
                Check(rowWb != null && rowWb.NormalTexForTest != null ? rowWb.NormalTexForTest.name : "<null>",
                          "40K_settings_button", "★ 条目的**常态图** = `40K_settings_button`（原版那颗 `Image`）");
                Check(rowWb != null && rowWb.HoverTexForTest != null ? rowWb.HoverTexForTest.name : "<null>",
                          "40K_settings_button_hover",
                          "★ 条目的**悬停图** = `40K_settings_button_hover`（原版 `m_HighlightedSprite` 实读；"
                          + "⛔ 不是 `HoverNames` 表里那条 `→ _selected` —— 那条属于聊天页签的 `Toggle`）");
                // **真鼠标路径**（`PointerLayer.HitButton`）—— 这一条同时钉着「没被吸收层吃掉」
                float hx, hy, hx2, hy2;
                CheckTrue(RectOfUnion(rowHitN, out hx, out hy, out hx2, out hy2),
                          "条目命中区量得到渲染矩形");
                CheckNear(hx, 99.57f, 0.5f, "…命中区左沿 = 视口左沿 **99.57**");
                CheckNear(hy, 359.68f, 0.5f, "…上沿 = 211.98 + 1 × (140.70 + 7) = **359.68**（视觉第 1 行的位置）");
                CheckNear(hx2, 728.28f, 0.5f, "…右沿 = 视口右沿 **728.28**");
                CheckNear(hy2, 500.38f, 0.5f, "…下沿 = 359.68 + 140.70 = **500.38**");
                var plHov = PointerLayer.Instance;
                CheckTrue(plHov != null, "`PointerLayer` 在场景里（真鼠标走的就是它）");
                if (plHov != null && rowWb != null)
                {
                    var hb = plHov.HoverAt((hx + hx2) * 0.5f, (hy + hy2) * 0.5f);
                    // 🔴 **2026-10-10 加诊断**（首次跑红时只有「期望 True / 实得 False」，看不出**赢家是谁**）：
                    CheckTrue(hb == rowWb,
                              "★ **真鼠标**悬停在条目上 ⇒ 指针层命中的**就是这一颗**"
                              + "（⛔ 退回 `QPanel` 档时命中的是吸收层 ⇒ 这条红）"
                              + $"；⚠️ 实得 = `{(hb == null ? "<null>" : hb.name)}`"
                              + $"（窗口态 `{inbox.CurrentState}` · 本行命中区档/队列 = "
                              + $"{(rowHitN != null && rowHitN.GetComponentInChildren<ImageQuad>() != null ? rowHitN.GetComponentInChildren<ImageQuad>().RenderQueue : -1)}）");
                    Check(rowWb.target != null && rowWb.target.Texture != null
                              ? rowWb.target.Texture.name : "<null>",
                              "40K_settings_button_hover", "★ …底图**真的换成了悬停图**");
                    // 移开：挑一个**在行之外**的点 —— 窗内右侧 `Message Display` 那片空地（x 1200 > 视口右沿 728.28）
                    plHov.HoverAt(1200f, 500f);
                    Check(rowWb.target != null && rowWb.target.Texture != null
                              ? rowWb.target.Texture.name : "<null>",
                              "40K_settings_button", "★ …移开 ⇒ **还原**成常态图（`Exit` 那一路）");
                }
            }

            // ---- 🆕 **2026-10-11（A286）**：软边切出来的子块**跟着换图**（⛔ 只换主格那一半 = 红）----
            //  现场：条目底图跨过列表视口的渐隐带内沿（`MsgList.y1 + 25 = 236.98`）⇒ `MenuDraw.ApplySoftEdges`
            //  把它切成「主格 + `_soft*` 子块」，而悬停换图挂在**这张底图**上（A273）⇒ 一换图就得**整棵**跟着换
            //  （原版那颗 `Selectable` 换的是**同一个 `Image` 的 sprite**，没有「只有一半换了图」这回事）。
            //  🔴 **三条断言（都量子块上的贴图名 / 渲染宽，⛔ 不断「调没调某个 API」）**：
            //    ① 悬停 ⇒ 主格与**每一个** `_soft*` 子块都是悬停图；② 移开 ⇒ 全部还原成常态图；
            //    ③ 子块**没有被拉宽**（宽必须 = 整条 `RowW` 628.71）。
            //  🔴 **为什么还要第三条**：本处软边是 `(0,25)` ⇒ **x 方向不切** ⇒ 子块的宽必须与主格同宽；
            //    实现里凡走到 `SetAspect(贴图比例)` 那条路（`ImageQuad.WorldW = _worldH × _aspect`）都会把它
            //    缩成 ~27px ⇒ 这一条立刻红（判据 = `ImageQuad.cs` 那两行 + `WindowButton.SetOn` 的注释）。
            //  ⚠️ **如实记（这条断言今天红不了的地方）**：光靠①②**验不出**「`SetTexture` 有没有转给子块」——
            //    悬停那条路里 `SetAspect(_targetAspect)` 的比例与贴图差得远（主格 628.71×115.7 ≈ 5.43、
            //    而 `40K_settings_button` 是 168×156 ≈ 1.0769）⇒ **不会早退** ⇒ `NotifySoftEdgeChanged`
            //    ⇒ `ReapplySoftEdges` **把子块整个重建**一遍（新子块在 `ApplySoftEdges` 里读 `q.Texture`）
            //    ⇒ 有没有转发都是对的。所以补了**第四态**：直调 `SetTexture`（⛔ 不跟 `SetAspect` ⇒
            //    `_aspect` 与目标同值那一档，= 重切链不触发）—— 那一路子块换不换**只**取决于
            //    `ImageQuad.SetTexture` 有没有转图。
            //  **改坏法（逐条）**：① 删掉 `ImageQuad.SetTexture` 里往 `_softKids` 转发那一循环
            //    ⇒ **第四态**红（子块停在常态图）；② 把转发那句的 `SetTextureOnly` 换成 `SetTexture`
            //    ⇒ 第三条红（子块的 `_aspect` 被冲成贴图比例、宽缩到 ~27）；③ 悬停那一档不动主格（`SwapTo` 去掉）
            //    ⇒ ①② 红。
            {
                var plS = PointerLayer.Instance;
                // 找一条**底图被软边切过**的条目（切过的才有 `_soft*` 子块）
                Transform hitS = null; WindowButton wbS = null;
                for (int i = 0; i < inbox.Rows.Count; i++)
                {
                    var r0 = inbox.Rows[i];
                    var h0 = FindChild(r0, "RowHit");
                    var w0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                    if (w0 != null && w0.target != null && w0.target.SoftEdgeKidCount > 0)
                        { hitS = h0; wbS = w0; break; }
                }
                CheckTrue(wbS != null && plS != null,
                          "★ **前提**：树里有一条「底图跨过渐隐带内沿（被软边切过）+ 接了悬停换图」的条目"
                          + "（一条都没有 ⇒ 下面几条等于没验；切没切由 `MsgList.y1 + 25 = 236.98` 那条内沿决定）");
                if (wbS != null && plS != null)
                {
                    var bgS = wbS.target;
                    float keepAspect = bgS.WorldH > 0f ? bgS.WorldW / bgS.WorldH : 0f;   // 换图前那份比例（第四态要还原）
                    float hx0, hy0, hx1, hy1;
                    CheckTrue(RectOfUnion(hitS, out hx0, out hy0, out hx1, out hy1),
                              "…那条条目的命中区量得到渲染矩形（真鼠标路径要用）");

                    var hbS = plS.HoverAt((hx0 + hx1) * 0.5f, (hy0 + hy1) * 0.5f);
                    CheckTrue(hbS == wbS, "…悬停打在**这一条**上（命中区没被别的件盖住）");
                    // ① 悬停：主格 + 每个 `_soft*` 子块
                    CheckTrue(bgS.Texture != null && bgS.Texture.name == "40K_settings_button_hover",
                              "★ 悬停 ⇒ **主格**换成悬停图"
                              + $"（实得 `{(bgS.Texture != null ? bgS.Texture.name : "<null>")}`）");
                    string bad1 = ""; int nSub1 = 0; float subW = -1f;
                    foreach (var c in bgS.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (c == null || c == bgS || c.name.IndexOf("_soft") < 0) continue;
                        nSub1++;
                        if (c.Texture == null || c.Texture.name != "40K_settings_button_hover")
                            bad1 += "「" + c.name + "」=" + (c.Texture != null ? c.Texture.name : "<null>") + "；";
                        float sx0, sy0, sx1, sy1;
                        if (QuadRectOf(c, out sx0, out sy0, out sx1, out sy1))
                            subW = Mathf.Max(subW, sx1 - sx0);
                    }
                    CheckTrue(nSub1 >= 1, "…（前提）这条条目的底图**真的被切开过**（有 `_soft*` 子块）");
                    CheckTrue(bad1 == "", "★ 悬停 ⇒ **每一个 `_soft*` 子块也都是悬停图**"
                                          + "（⛔「只换主格那一半」= 红）"
                                          + (bad1 == "" ? "" : "；没换的：" + bad1));
                    CheckNear(subW, 628.71f, 0.5f,
                              "★ 子块的**渲染宽 = 628.71**（= 原版 `Viewport` 宽 `728.28 − 99.57`；本处软边 "
                              + "`(0,25)` ⇒ x 方向不切 ⇒ 子块必须与主格同宽；⛔ 走到 `SetAspect(贴图比例)` "
                              + "那条路上会缩到 ~27）");
                    // ② 移开：整棵还原
                    plS.HoverAt(1200f, 500f);
                    string bad2 = ""; int nSub2 = 0;
                    foreach (var c in bgS.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (c == null || c == bgS || c.name.IndexOf("_soft") < 0) continue;
                        nSub2++;
                        if (c.Texture == null || c.Texture.name != "40K_settings_button")
                            bad2 += "「" + c.name + "」=" + (c.Texture != null ? c.Texture.name : "<null>") + "；";
                    }
                    CheckTrue(bgS.Texture != null && bgS.Texture.name == "40K_settings_button",
                              "★ …移开 ⇒ **主格**还原成常态图");
                    CheckTrue(bad2 == "" && nSub2 == nSub1,
                              "★ …移开 ⇒ **子块也全部还原**（个数 " + nSub2 + " vs 悬停时 " + nSub1 + "）"
                              + (bad2 == "" ? "" : "；没还原的：" + bad2));

                    // ③·第四态：**只有 `SetTexture` 的那一路**（⛔ 不跟 `SetAspect`）——
                    //    `_aspect` 此刻 == 换图前那份比例 ⇒ 这一档就是 `SetAspect` 会**早退**、
                    //    重切链**不触发**的情形（见本块开头那段「如实记」）。
                    bgS.SetTexture(CardArt.MenuUi("40K_settings_button_hover"));
                    string bad3 = ""; int nSub3 = 0;
                    foreach (var c in bgS.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (c == null || c == bgS || c.name.IndexOf("_soft") < 0) continue;
                        nSub3++;
                        if (c.Texture == null || c.Texture.name != "40K_settings_button_hover")
                            bad3 += "「" + c.name + "」=" + (c.Texture != null ? c.Texture.name : "<null>") + "；";
                    }
                    CheckTrue(nSub3 >= 1, "…（前提）第四态里那条条目的子块还在");
                    CheckTrue(bad3 == "",
                              "★ **只换图、不跟 `SetAspect`** 的那一路（= 重切链不触发）子块也换"
                              + "（A286：`ImageQuad.SetTexture` 往 `_softKids` 转发那一句；"
                              + "⛔ 删掉它 = 这一条红）" + (bad3 == "" ? "" : "；没换的：" + bad3));
                    // 收尾：把这一条**原样还原**（别把脏状态留给后面的断言）
                    bgS.SetTexture(CardArt.MenuUi("40K_settings_button"));
                    if (keepAspect > 0f) bgS.SetAspect(keepAspect);
                    CheckTrue(bgS.Texture != null && bgS.Texture.name == "40K_settings_button",
                              "…收尾：这一条回到常态图（⛔ 别把换过图的状态留给后面的断言）");
                }
            }
            // ---- 还原成空态（`Initialize(null)` = 原版 `:49` 那条空支）----
            inbox.Initialize(null);
            Check(inbox.Rows.Count, 0, "`Initialize(null)` ⇒ 条目全清");
            CheckTrue(FindPath(inbox.transform, "Content/Message Display") != null
                      && !FindPath(inbox.transform, "Content/Message Display").gameObject.activeSelf,
                      "空态回来了：`Message Display` 又关上（⛔ 灌完数据不收尾 = 让后面的断言跑在假状态上）");
            CheckTrue(FindChild(inbox.transform, "No News Warning") != null
                      && FindChild(inbox.transform, "No News Warning").gameObject.activeSelf,
                      "空态回来了：`No News Warning` 又开上");
        }

        // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
        //   期望矩形 = **原版 prefab** `Inbox Menu > Content > Generic Window Red Background Big`
        //   那颗 `Image` 的 rect（76.09,36.80 → 1856.86,1068.04）；⛔ 不写 `InboxWindow.RedBg`
        //   （那是被测实现**传进去的实参**，同式自证）。
        CheckAbsorbRule("收件箱窗", inbox.transform, "AbsorbHit",
                        76.09f, 36.80f, 1856.86f, 1068.04f,
                        InboxWindow.QShade, InboxWindow.QOverlay, () => inbox.CurrentState);

        // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据 → `:5670` 那一段）。
        //   ⚠️ 上面那组吸收层收尾**已经把窗点关了** ⇒ 这里先开回来（无参 `TryOpen()`：不碰 `Data`，
        //      `Closed` 支会重建内容 —— 本口只认 `ShadeHit` 节点 + 状态两跳，不读内容）。
        //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后（下一句 `wm2.CloseAllWindows()` 与它无关，
        //      更下面 `:8715` 那段「不许再把本块挪回 `CheckAbsorbRule` 之后」说的**不是**本块）。
        CheckTrue(inbox.TryOpen(), "（A796 现场）把收件箱窗开回来 —— 下面那条要在**开着**的窗上点");
        MenuDraw.CheckShadeClickRule(CheckTrue, "收件箱窗", inbox.transform, inbox.ShadeHit,
                                     () => inbox.CurrentState);

        wm2.CloseAllWindows();

        // ============================================================ 🆕 A23 每日任务行的「垃圾桶」= 【重摇任务】
        //
        // 判据 = `资料/待办判据_阶段二与联机.md` §A23（机制链 + `ReRollPopup Variant` 的逐节点实测表）。
        // 🔴 **本节必须是 `Run` 的最后一节** —— `Confirm` 会**重建整页 Missions**（前面那些 `rows[..]`
        //    抓着的是旧节点），而且这一节要**一张干净的 Missions 页**（前几节结尾的 `CloseAllWindows()`
        //    把奖励窗一起关掉了 ⇒ 这里重新开一扇）。
        // ⚠️ **x 用「行内锚点五元组」算、不抄 dump 的绝对 x**：`menu_dump` 在那张表的末尾**自己打了警告**
        //    —— `Normal Missions` 那条 `HorizontalLayoutGroup` 的**主轴尺寸算不准** ⇒ 它底下子节点的
        //    **x 别照抄**（**y 与尺寸不受影响**，行高 150 / 间距 18.55 / `Trash mission` 那 49.104×49.368 都是可信的）。
        //    🔴 **2026-10-06（A106）把这条警告升级** —— 上面那句「工具算不准」**方向对、成因说浅了**，
        //       两件事必须分开（原来只有一句，读起来像是「工具坏了、东西没坏」）：
        //      · 🔴 **原版确实会挪那两个子件**（**不是**「工具测不出来」）：`Normal Missions` 上真挂着 HLG、
        //        `Special Missions` 上真挂着 CSF（`m_HorizontalFit = 2 = PreferredSize`）⇒ **布局位 ≠ 模板位**。
        //        本轮已照**布局位**改（断的是 **【2 张卡】那一帧** = 我们这一页实际画的那一帧）：
        //        `Special Missions` 宽 **706.29** · `Daily Missions` x **1190.44..1799.94**
        //        （⇒ `Daily Missions Holder` 与**三行的宽度/位置跟着变**）。断言的落点：
        //        §三·1 的两条 `CheckAt` + 本节用的 `RowRectHolderX1/X2`。
        //        ⚠️ **该定案依赖「我们画两张卡」** —— 卡片数一改，这组数要一起改（对照表在
        //        `Shell/MissionsTab.cs` 的 A106 那一段）。
        //      · 工具那一版给的 x（**10.92 / 384.9**）之所以错，成因是 **`menu_dump` 不递归嵌套布局组的总量**
        //        （`Special Missions` 的量来自它自己那个 HLG 的总量 ⇒ 外层算不出来）⇒ 那是**工具缺口 A110**，
        //        **与「原版到底摆在哪」是两件事，别混**（一个是工具能力，一个是原版行为）。
        //      · ⛔ **这条警告仍然成立的部分**：**子件内部**的 x 一律照**行内锚点五元组**算（那是相对量），
        //        `y` 与高照旧可信。
        //    🔴 **2026-10-06（A124）再加一层**：**行内锚点五元组给出的是【设计空间】的矩形** ——
        //       `Daily Missions` 的 `m_LocalScale = (1.15,1.15)` ⇒ 本节凡是要**点在屏幕上**的坐标
        //       （`tx/ty` · `wantCollect.CX/CY`）都必须再走一次 `DmView(...)`，否则点到的是没东西的地方
        //       （本节那几条 `pl.ClickAt` / `pl.HoverAt` 会直接红 —— 它们是**真鼠标路径**，这也是它们值钱的地方）。
        //       ⚠️ 顺序：**先套五元组、后 `DmView`**（`sizeDelta` 是设计空间的量，喂缩放过的父矩形会少乘 15%）。
        Section("🆕 A23 每日任务行的 `Trash mission` = **重摇任务**（命中区 · 悬停换图 · 重摇窗）");
        {
            wm2.CloseAllWindows();
            // 🔴 **本节起点的进度必须是「三条都没到顶」**：上面那一节结尾 `OnBattleEnd(true, 99999, 99999)`
            //    把三条全推到了 100%，而按原版规则（`MissionReRollButton` 继承的 `displayRule = WhenActive`
            //    ⇒ `!IsComplete` 才显示）**完成的那一行不该有垃圾桶** ⇒ 不重置的话下面「垃圾桶在」会红。
            //    （2026-10-04 A36-① 起这条规则才实现 —— 那时才发现本节原来是「靠三条都完成」在跑的。）
            DailyData.ResetMissionsForTest();          // 52/500 · 4/10 · 1/3
            var rwA = RewardsWindow.Create(wm2);
            wm2.OpenWindow(rwA);
            var mtA = FindChild(FindChild(FindChild(rwA.transform, "Content Area"), "Tabs"), "Missions Tab");
            CheckTrue(mtA != null, "重建的奖励窗上 `Missions Tab` 在（这一节要一张干净的页）");
            var rowA = FindChild(mtA, "Daily Mission Container (0)");
            CheckTrue(rowA != null, "第 1 行在");

            // 期望矩形 = **原版的锚点五元组**（`N(1, 1,0, 1,0, .5,.5, −307.44,40.711, 49.104,49.368)`）
            // 套在本页第 1 行的矩形上 —— `UguiRect.Child` 是工程里**唯一**一份锚点算法（不是照抄常数）。
            // 🔴 **2026-10-06（A124）**：**先在设计空间套五元组、最后整体过 `DmView`**（`sizeDelta` 那两项
            //    49.104/49.368 也是设计空间的量 ⇒ 顺序反了会静默少 15%）。本节的点位是**真鼠标点击**用的
            //    （`pl.ClickAt(tx, ty)` / `pl.HoverAt`）⇒ 不缩放的话点到的是**屏幕上没东西的地方**。
            var rowAr = MissionsTab.RowRect(new PxRect(RowRectHolderX1(), 150.28f, RowRectHolderX2(), 651.72f), 0);
            var wantTrash = DmView(UguiRect.Child(rowAr, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                                                   new Vector2(-307.44f, 40.711f), new Vector2(49.104f, 49.368f)));
            // 🆕 2026-10-04（A44 甲）：`Collect` 那颗钮的期望矩形 —— 与垃圾桶**同一组锚点**
            //   （`a=(1,0)-(1,0)` · `p=(.5,.5)`），只有 `pos/sz` 不同
            //   （原版 `N(1, 1,0, 1,0, .5,.5, −145.3,40.7107, 254.611,56.4767)`）。
            //   下面那条三态对比用它 —— 正例与负例**量的是同一个坐标**，差别只有 `St` 一个字段。
            var wantCollect = DmView(UguiRect.Child(rowAr, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                                                    new Vector2(-145.3f, 40.7107f), new Vector2(254.611f, 56.4767f)));

            var trashArt = rowA != null ? rowA.Find("Trash mission") : null;
            var trashHitN = rowA != null ? rowA.Find("Hit") : null;
            CheckTrue(trashArt != null, "垃圾桶的底图 `Trash mission` 在");
            CheckTrue(trashHitN != null,
                      "🆕 垃圾桶**有命中区了**（原来只有一张图、整个文件里没有任何 `Hit` —— A23 那条缺口）");
            var pl = PointerLayer.Instance;
            CheckTrue(pl != null, "`PointerLayer` 在场景里（真鼠标走的就是它）");
            float tx = wantTrash.CX, ty = wantTrash.CY;
            if (trashHitN != null)
            {
                var hq = trashHitN.GetComponentInChildren<ImageQuad>();
                CheckTrue(hq != null, "命中区有 `ImageQuad`（`PointerLayer` 认的就是它）");
                var wb = trashHitN.GetComponent<WindowButton>();
                CheckTrue(wb != null && wb.onClick != null, "命中区上挂了 `WindowButton`（点了有事做，不是装饰）");
                if (hq != null)
                {
                    // ⚠️ 命中区的**尺寸**同样在缩放里（`MissionsTab` 那一下给的是 `R(trash)`）——
                    //    屏上 = 原版 `sz` × `Daily Missions.localScale 1.15`（**不是**裸 49.104/49.368）。
                    CheckNear(hq.WorldW * 108f, 49.104f * 1.15f, 0.6f,
                              "命中区宽(px)（原版 `sz.x` 49.104 × `localScale` 1.15 = 56.47）");
                    CheckNear(hq.WorldH * 108f, 49.368f * 1.15f, 0.6f,
                              "命中区高(px)（原版 `sz.y` 49.368 × `localScale` 1.15 = 56.77）");
                    CheckAt(hq.transform, wantTrash.x1, wantTrash.x2, wantTrash.y1, wantTrash.y2,
                            "命中区落在**原版锚点**算出来的矩形上（行内 y 偏移 = 150 − 40.711 ± 49.368/2）");
                    var bq = trashArt != null ? trashArt.GetComponentInChildren<ImageQuad>() : null;
                    if (bq != null)
                    {
                        float ddx = Mathf.Abs(hq.transform.position.x - bq.transform.position.x) * 108f;
                        float ddy = Mathf.Abs(hq.transform.position.y - bq.transform.position.y) * 108f;
                        CheckTrue(ddx <= 0.6f && ddy <= 0.6f,
                                  $"命中区与看得见的那张底图**同心**（差 {ddx:F2},{ddy:F2}px —— "
                                  + "点得到的地方必须就是看得见的地方）");
                    }
                }

                // ---- 悬停换图：原版那颗 `EverguildButton` 是 `trans = 2 (SpriteSwap)`，
                //      实读 `m_SpriteState` = `HL 40k_general_bt_yellow_hover` · `P 40k_general_bt_yellow_pressed`
                //      （`menu_dump.py bundle_menus_assets_all "Missions Tab" --depth 6` + pid 回真包反查）----
                if (pl != null)
                {
                    var hb = pl.HoverAt(tx, ty);
                    CheckTrue(hb != null && hb == wb, "**真鼠标**悬停到垃圾桶上 ⇒ 指针层命中的就是那一颗");
                    if (hb != null && hb.target != null && hb.target.Texture != null)
                    {
                        Check(hb.target.Texture.name, "40k_general_bt_yellow_hover",
                              "悬停 ⇒ 底图换成 `40k_general_bt_yellow_hover`（原版 `m_SpriteState` 实读）");
                        pl.HoverAt(100f, 1000f);                       // 移开（那一处是空的，见上面那条断言）
                        Check(hb.target.Texture.name, "40k_general_bt_yellow",
                              "移开 ⇒ 还原成 `40k_general_bt_yellow`");
                    }
                }

                // ---- 点它 ⇒ 开 `MissionReRollPopup`（原版链 = `MissionReRollButton` 的 `Setup` 里
                //      `WindowsManager.OpenWindow(MissionReRollPopup, ctx)`；反编译实证）----
                // ⚠️ 走 `PointerLayer.ClickAt`（**真路径**），不是直调 `onClick`。
                CheckTrue(pl != null && pl.ClickAt(tx, ty), "点垃圾桶 ⇒ 指针层吃到了这一下（真路径）");
                var pop = MissionRerollPopup.LastOpened;
                CheckTrue(pop != null && pop.CurrentState == WindowState.Open,
                          "…而且 `MissionReRollPopup` 真的开出来了（原来是**点了没反应**）");
                if (pop != null)
                {
                    var pv = pop.transform;
                    // ---- 窗口字段（MB 原文，prefab 根 `ReRollPopup Variant` 上的 `MissionReRollPopup`）----
                    Check(pop.type, WindowType.Popup, "`type` = 1 Popup（MB 原文）");
                    Check(pop.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（MB 原文）");
                    Check(pop.extraScaleSmallScreen, 1f, "`extraScaleSmallScreen` = 1.0（MB 原文）");
                    // 🔴 MB 原文 `closeOnESC = 0` ⇒ **ESC 不关这扇窗**。⚠️ 派单里写的是「ESC ⇒ 关窗」，
                    //    与 MB 实读**相反** ⇒ 这里**按原版**（详见 `MissionRerollPopup.cs` 文件头 ④ 与报告）。
                    Check(pop.closeOnEsc, false, "`closeOnESC` = **0**（MB 原文 —— ESC 不关这扇窗）");

                    // ---- 逐节点几何（全部是 §A23 那张表的实测数，**字面量**）----
                    var darkN = FindChild(pv, "Menu Dark Background");
                    CheckAt(darkN, -1327.30f, 3247.30f, -746.18f, 1826.18f,
                            "`Menu Dark Background`（比屏幕大一圈的那块压暗）");
                    CheckNear(AlphaOf(darkN), 0.773f, 0.005f, "压暗层 α = 0.773（原版 `m_Color`）");
                    CheckAt(FindChild(pv, "Window"), 535f, 1385f, 245f, 675f, "`Window` = **850 × 430**");
                    var pbg = FindChild(FindChild(pv, "Window"), "Generic Popup Background");
                    CheckAt(pbg, 535f, 1385f, 245f, 675f, "`Generic Popup Background`（与 `Window` 同矩形）");
                    CheckArt(pbg, "40k_popup", "`Generic Popup Background` 的图 = `40k_popup`（九宫格）");
                    var maskN = FindChild(pbg, "Mask");
                    CheckAt(maskN, 545.4f, 1375.1f, 254.4f, 665.2f, "`Mask`（比 `Window` 四边各内缩约 10）");
                    // 原版 `Mask` 是 `Image + Mask` 且 **`showGraphic = 0`** ⇒ 那一件**不渲染**；
                    // 我们的引擎没有 stencil mask ⇒ 只建节点、不画那张 `40k_popup`（画了会变双层边框）。
                    // ⇒ 它底下**只该有 `Background fill` 那一个子件**（多出别的 = 有人又把掩码图画上了）。
                    Check(maskN != null ? maskN.childCount : -1, 1,
                          "`Mask` 下**只有一个子件**（`Background fill`）—— 掩码自己那张 `40k_popup` **不画**"
                          + "（原版 `showGraphic = 0` 本来就不渲染；画了会变成双层边框）");
                    var fillN = FindChild(maskN, "Background fill");
                    CheckAt(fillN, 545.4f, 1375.1f, 254.4f, 665.2f, "`Background fill`（与 `Mask` 同矩形）");
                    if (fillN != null)
                    {
                        var fq = fillN.GetComponentInChildren<ImageQuad>();
                        CheckTrue(fq != null && fq.Texture != null && fq.Texture.name == "40k_popup_texture",
                                  "`Background fill` 是 `40k_popup_texture`（原版 **Tiled** `ppuMul 2.0` ⇒ 一格 64px）");
                    }
                    var msgN = FindChild(pv, "MessageText");
                    CheckAt(msgN, 575f, 1345f, 273.8f, 546.2f, "`MessageText`（770 × 272.4）");
                    Check(TextOf(msgN), "Discard this mission and receive a new one?",
                          "`MessageText` 文案 = prefab **出厂原文**（原版运行期由 I2 词条覆盖，词条表在远端 CCD）");
                    var btnsN = FindChild(pv, "Buttons");
                    CheckAt(btnsN, 572.3f, 1347.7f, 534f, 624f, "`Buttons` 行（775.4 × 90）");
                    var cancelN = FindChild(btnsN, "ButtonLeft");
                    // 🔴 **2026-10-05（A59）整窗 x 按工具【完整 uGUI】重落**（`_child_sizes` 补了 `flexible`/
                    //   `childSize`/`offsetInCell`）—— 外层 `Buttons`（`ctrlW=0 expandW=1 align=4`）把余额 75.4
                    //   平分进两格（每格 +37.7）、子件再居中在格里（+18.85）⇒ 两颗钮**各 ±18.85**。
                    //   旧值 `610/960` 是「漏了这三处」那一版的产物（详见 `MissionRerollPopup.cs` 文件头 A59 段）。
                    CheckAt(cancelN, 591.15f, 941.15f, 541f, 617f, "`ButtonLeft` = **350 × 76**");
                    Check(TextOf(FindChild(cancelN, "Button Text")), "Cancel", "`Cancel` 文案");
                    CheckAt(FindChild(cancelN, "Button Text"), 604.15f, 928.15f, 541f, 617f,
                            "`Cancel` 的 `Button Text`（`sz=(-26,0)` ⇒ 框 604.15..928.15）");
                    var priceBtnN = FindChild(btnsN, "Price Display");
                    CheckAt(priceBtnN, 978.85f, 1328.85f, 541f, 617f, "`Price Display` = **350 × 76**");
                    var confirmN = FindChild(priceBtnN, "Generic UI Button");
                    CheckAt(confirmN, 978.85f, 1328.85f, 541f, 617f, "`Generic UI Button`（`Confirm ` 那颗）");
                    // ⚠️ prefab 原文是 `'Confirm '`（**带尾空格**）⇒ 比的时候 trim 掉，但注释记着它
                    Check(TextOf(FindChild(confirmN, "Button Text")), "Confirm ",
                          "`Confirm ` 文案（**原版的尾空格也在**）");
                    var cellN = FindChild(confirmN, "Price Display");
                    CheckAt(cellN, 1160.10f, 1160.10f, 541f, 617f, "价钱格（出厂是**零宽**的框，靠 CSF 撑开）");
                    var iconN = FindChild(cellN, "icon");
                    // 🆕 A36-④：原版那个节点 **rect = 56² 但 `m_LocalScale = 1.2`**（原始 JSON 实读：
                    //   `m_SizeDelta.x = 56` · `m_LocalScale = (1.2,1.2,1)` · `m_Pivot = (.5,.5)`）
                    //   ⇒ **真画出来 67.2²、绕同一个中心**。我们**把缩放烘进矩形**（照 `BoosterInfoPopup`「矩形 51.88²
                    //   × scl 1.2 ⇒ 实际画出来 62.26²」那条先例）⇒ 节点矩形 = `1160.10..1227.30 × 545.4..612.6`。
                    //   · 中心 = 父框左边缘 `1160.10` + `56 × 0.5 × 1.2`（= 真 uGUI 的 `anchoredPosition.x`）= **1193.70**
                    //   · 半宽 = `56×1.2/2 = 33.6` ⇒ `1193.70 ± 33.6`（**保中心**：放大绕的就是 pivot）
                    //   ⚠️ **M4 那条「右边缘与价钱文字左边缘同值、两条独立算式撞在一起」不成立**（`5.5 ≠ 5.6`）
                    //     ⇒ 别拿它当交叉校验（详见 `MissionRerollPopup.IconR` 的注释）。
                    //   ⚠️ `CheckAt` 量的是**中心**（不是四边）—— 空节点没有 `ImageQuad`，**尺寸量不出来**；
                    //     尺寸那条判据在 `MissionRerollPopup.IconR` 的注释里 + 原版字段出处
                    //     ⇒ 这条断言**分不出**「烘过缩放的 67.2²」与「没烘的 56²」（两者中心同值）。
                    // 🔴 **2026-10-05（A59）**：`1135.65..1202.85` ⇒ **`1160.10..1227.30`**（中心 1169.25 ⇒ **1193.70**）。
                    //   M5 记的 `1174.85` / `1213.95` **已作废**（那是「只补了 `m_ChildScale`」的中间态）；
                    //   真值 = 工具【完整 uGUI】给的 **`1193.70` / `1232.80`**
                    //   （`python 工具/menu_dump.py bundle_menus_assets_all "ReRollPopup Variant" --depth 12 --md`）。
                    CheckAt(iconN, 1160.10f, 1227.30f, 545.4f, 612.6f,
                            "价钱格的 `icon`（原版 rect **56²** × `scl 1.2` ⇒ **真画出来 67.2²**、中心不变）");
                    CheckTrue(iconN != null && iconN.GetComponentInChildren<ImageQuad>() == null,
                              "`icon` 只建节点、**不画图**（原版出厂 `m_Sprite` 空、运行期按货币赋 —— "
                              + "`get_RerollPrice` 是错桩，价钱与货币都读不到 ⇒ 不拿别的图冒充）。"
                              + "⚠️ 这条钉住的是「**判据还没到**」那个状态：哪天真价钱/货币查到了，"
                              + "它**必须跟着改**（按 `PriceDisplay__Setup` 那条路画上货币图），别留着当护身符");
                    var priceTxN = FindChild(cellN, "text");
                    Check(TextOf(priceTxN), "300,00",
                          "价钱 = 出厂占位 `300,00`（⚠️ **不是真价钱**：prefab 占位，原版运行期覆盖）");
                    CheckNear(TextLeftPx(priceTxN), 1232.80f, 2f,
                              "价钱那格 `text` 的**左边缘 = 1232.80**（= 父框左边缘 1160.10 + `icon` 烘过缩放的宽 67.2 + HLG 间距 5.5）"
                              + " ⚠️ **2026-10-04（M3）订正**：原来这句还说「与 `icon` 烘过 1.2 后的右边缘同值 —— "
                              + "两条独立算式撞在一起」—— **不成立**（`5.5 ≠ 5.6`）⇒ 别当交叉校验。"
                              + "🔴 **2026-10-05（A59）`1202.75` ⇒ `1232.80`**：两截都变了（父框 +18.85 · 步进改吃 `localScale`）；"
                              + "M5 记的 `1213.95` **已作废**。");
                    // `Confirm` 的右边缘 = 价钱格左边缘 **1160.10** − 那颗钮 HLG 的间距 **12.5** = **1147.60**
                    // 🔴 **这是【推导值】，不是能直接抄的固定数**：那颗 `Button Text` 的框宽是 **0**
                    //    （`ContentSizeFitterMinMax` 撑着）、价钱格也是零宽框（`ContentSizeFitter` 撑着），
                    //    1147.60 是父级 HLG（`spacing 12.5` · **`align = 4 (MiddleCenter)`**）把「0 + 12.5 + 0」
                    //    居中在 350 宽的钮里算出来的（**978.85** + (350 − 12.5)/2）⇒ **价钱串一变宽就会跟着变**
                    //    （铁律 5·c）。我们复刻的是 prefab 出厂那一态（价钱 `300,00`）。
                    // 🔴 **2026-10-05（A59）`1128.75` ⇒ `1147.60`（+18.85）**：那颗钮自己的尺寸没变，
                    //    是**父框**被外层 `Buttons` 组从 `960..1310` 挪到 `978.85..1328.85`（见上面 `ButtonLeft` 那条）。
                    CheckNear(TextRightPx(FindChild(confirmN, "Button Text")), 1147.60f, 2f,
                              "`Confirm` 的**右边缘 = 1147.60**（⚠️ **2026-10-04（M3）措辞订正**：这是 **`menu_dump` 布局复算值**，"
                              + "**不是原始 JSON 实读** —— 原始 JSON 那颗 `Button Text` 是零宽模板位 `sz=(0,76)`，"
                              + "布局跑完才有的 `anchoredPosition.x = 168.75`；"
                              + "另：这是**推导值**，两个子件都是零宽框 + HLG 居中）");
                    // 🔴 命中区：两颗钮各一块、压暗层一块（压暗那一块**严格低于**窗内的两块 —— 见 `QShadeHit`）
                    CheckTrue(FindChild(confirmN, "Hit") != null, "`Confirm` 有命中区");
                    CheckTrue(FindChild(cancelN, "Hit") != null, "`Cancel` 有命中区");
                    CheckTrue(FindChild(darkN, "CloseHit") != null, "压暗层有点外关窗的命中区（原版 `BackgroundCloseButton`）");
                    // ---- ⑧ 队列：**量真值，不比常量** ----
                    // 🔴（2026-10-04 A36-⑧）原来这里是 `MissionRerollPopup.QBase > RewardsWindow.QOverlay`
                    //    = **常量比常量**（3080 > 3014）⇒ 把实现改成「压暗层画到页下面」它照样绿。
                    // 判据 = **原版 prefab 的子件序**：`Menu Dark Background` 是根的第 1 个子件、`Window` 是第 2 个
                    //    （uGUI 里后画的盖住先画的）⇒ 压暗层在窗内容**之下**；而整扇弹窗又必须在它盖住的
                    //    **奖励窗每一件**之上。我们这套分层靠 `ImageQuad.SetRenderQueue`（见 `QBase` 的注释）
                    //    ⇒ 直接读**建出来那些 quad 的 `RenderQueue`**，把上面这个序逐条比一遍。
                    int shadeQ2 = QueueOf(FindChild(darkN, "Image"));
                    int panelQ2 = QueueOf(FindChild(pbg, "Image"));
                    int fillQ2 = QueueOf(fillN);
                    int closeHitQ = QueueOf(FindChild(darkN, "CloseHit"));
                    int cancelHitQ = QueueOf(FindChild(cancelN, "Hit"));
                    int confirmHitQ = QueueOf(FindChild(confirmN, "Hit"));
                    int pageMaxQ = MaxQueue(rwA.transform);
                    CheckTrue(shadeQ2 > 0 && panelQ2 > 0 && fillQ2 > 0 && closeHitQ > 0
                              && cancelHitQ > 0 && confirmHitQ > 0 && pageMaxQ > 0,
                              $"七个队列**都量到了**（压暗 {shadeQ2} · 窗底 {panelQ2} · 平铺 {fillQ2} · "
                              + $"命中：点窗外 {closeHitQ} / 取消 {cancelHitQ} / 确认 {confirmHitQ} · "
                              + $"奖励窗里最高的那一件 {pageMaxQ}）—— 量不到就说明这条断言等于没查");
                    CheckTrue(shadeQ2 > pageMaxQ,
                              $"压暗层({shadeQ2}) **高于奖励窗里最高的那一件**({pageMaxQ}) —— 否则页画在它上面、盖不住");
                    CheckTrue(panelQ2 > shadeQ2,
                              $"窗底({panelQ2}) 在压暗层({shadeQ2}) **之上**（原版子件序：`Window` 在 `Menu Dark Background` 之后）");
                    CheckTrue(fillQ2 > shadeQ2, $"窗内平铺底纹({fillQ2}) 也在压暗层({shadeQ2}) 之上");
                    CheckTrue(closeHitQ > pageMaxQ,
                              $"「点窗外关窗」那块命中区({closeHitQ}) 也盖住整页（{pageMaxQ}）");
                    CheckTrue(closeHitQ < cancelHitQ && closeHitQ < confirmHitQ,
                              $"…但它**严格低于**窗内两颗钮的命中区（取消 {cancelHitQ} / 确认 {confirmHitQ}）"
                              + " —— 不然点钮会变成关窗（同队列时 `ImageQuad` 的 z 恒为 0，谁吃到命中不可控）");
                    // 🆕 A47：同一条不变量的**公共断言**（唯一一份在 `MenuDraw.CheckShadeRule`；档 = 压暗层自己
                    //   那一档 `QShade`(3080)，严格低于内容命中区档 `QHit`(3088)）+「这个节点确实是 `ShadeHit` 建的」。
                    //   🔴 A77⑬③：期望值改成**量** `darkN`（= 本窗那块 `Menu Dark Background`，见 `:3873`）
                    //      底下那颗视觉 quad 的档 —— `darkN` 是 `Node`（节点自己没 quad），quad 在它子件 `Image` 上，
                    //      `ShadeVisualQuad` 会认。
                    MenuDraw.CheckShadeRule(CheckTrue, "重摇任务窗", FindChild(darkN, "CloseHit"),
                                            darkN, MissionRerollPopup.QHit);
                    Check(pop.MissingArt.Count, 0, "重摇窗没有取不到的图");
                    CheckHoverSwap(pop.transform, "重摇窗");
                    Shoot("06_重摇任务.png");

                    // ---- `Cancel`：**关窗、什么都不做** ----
                    string bDesc = DailyData.DailyDesc(0), bCnt = DailyData.DailyCounter(0);
                    string bRew = DailyData.DailyRewardText(0), bArt = DailyData.DailyRewardArt(0);
                    int bCount = DailyData.RerollCount;
                    // 🆕 **B4 第②半（2026-10-05）**：重摇前**把格子里画的那个数记下来** ——
                    //   重摇之后要比「它换了没有」。⚠️ 量的是**渲染真值**（`TextOf` 的 TMP 串），
                    //   不是从 `DailyData` 读的期望值。
                    var rowCellB = FindChild(mtA, "Daily Mission Container (0)");
                    string bCellCnt = TextOf(FindChild(rowCellB, "count 0"));
                    if (pl != null)
                    {
                        CheckTrue(pl.ClickAt(766.15f, 579f), "点 `Cancel`（真路径）");
                        CheckTrue(pop.CurrentState == WindowState.Closed, "按 `Cancel` ⇒ 关窗");
                        Check(DailyData.DailyDesc(0), bDesc, "…而且任务**一个字都没动**");
                        Check(DailyData.DailyCounter(0), bCnt, "…进度/目标也没动");
                        Check(DailyData.DailyRewardText(0), bRew, "…奖励数量也没动");
                        Check(DailyData.DailyRewardArt(0), bArt, "…奖励图标也没动");
                        Check(DailyData.RerollCount, bCount, "…**重摇计数 +0**（`Cancel` 不是重摇）");

                        // ---- 点窗外（压暗背景）⇒ 同样是**关窗什么都不做** ----
                        // ⚠️ 每点一次垃圾桶都**新开一扇**（`MissionsTab.OpenReroll` 每次 `Create`）
                        // ⇒ 每次都要**重新取 `LastOpened`**（拿旧引用会永远看到 Closed —— 那就成了假绿）。
                        CheckTrue(pl.ClickAt(tx, ty), "再开一次（点垃圾桶）");
                        var pop2 = MissionRerollPopup.LastOpened;
                        CheckTrue(pop2 != null && pop2 != pop, "…**又开了一扇新的**（不是把旧的翻出来）");
                        CheckTrue(pop2 != null && pop2.CurrentState == WindowState.Open, "…而且开着");
                        CheckTrue(pl.ClickAt(100f, 1000f), "点窗外那块压暗背景（真路径）");
                        CheckTrue(pop2 != null && pop2.CurrentState == WindowState.Closed, "按暗背景 ⇒ 关窗");
                        Check(DailyData.DailyDesc(0), bDesc, "…任务仍然没动");
                        Check(DailyData.RerollCount, bCount, "…重摇计数仍然 +0");

                        // ---- `Confirm`：**换一条新任务**（逐项都不同）----
                        CheckTrue(pl.ClickAt(tx, ty), "第三次开窗（点垃圾桶）");
                        var pop3 = MissionRerollPopup.LastOpened;
                        CheckTrue(pop3 != null && pop3.CurrentState == WindowState.Open, "…开出来了");
                        // ⚠️ 点位 = 那颗钮（`Generic UI Button` 框 `978.85..1328.85`）的**中心** `1153.85`
                        //   —— 在 `icon` 烘过缩放后的左边缘 `1160.10` **左边**（`icon` 是空节点、没有命中区，不挡点击）
                        CheckTrue(pl.ClickAt(1153.85f, 579f), "点 `Confirm`（真路径）");
                        CheckTrue(pop3 != null && pop3.CurrentState == WindowState.Closed, "按 `Confirm` ⇒ 关窗");
                        Check(DailyData.RerollCount, bCount + 1, "**重摇计数 +1**（`Confirm` 才换任务，原版走 `RerollChallenge`）");
                        CheckTrue(DailyData.DailyDesc(0) != bDesc,
                                  $"任务**换了**：描述「{bDesc}」⇒「{DailyData.DailyDesc(0)}」");
                        CheckTrue(DailyData.DailyCounter(0) != bCnt,
                                  $"…目标/进度也不同：{bCnt} ⇒ {DailyData.DailyCounter(0)}");
                        // 「逐项不同」= 描述 / 目标 / 奖励数量（⚠️ **奖励图标不保证不同**：
                        // 池里那 5 条的图标只有 3 种，可能正好和原来那条同色 —— 这条不算判据）
                        CheckTrue(DailyData.DailyRewardText(0) != bRew,
                                  $"…奖励数量也不同：{bRew} ⇒ {DailyData.DailyRewardText(0)}");
                        CheckTrue(DailyData.DailyDesc(0) != null && DailyData.DailyDesc(0).Length > 0,
                                  "…而且新描述不是空的");
                        // `Confirm` 回来时**重建了整页** ⇒ 画面上立刻是新任务（抓的是**新**那一行）
                        var rowA2 = FindChild(mtA, "Daily Mission Container (0)");
                        Check(TextOf(FindChild(rowA2, "description")), DailyData.DailyDesc(0),
                              "页面上那一行**立刻显示新任务**（`Confirm` 之后重建了整页）");
                        // 🆕 **B4 第②半**：「重摇后重建时那个格子**一起重算**」 —— 奖励格画的是**新那条任务**的奖励。
                        //   ⚠️ 下面第三条是**分得出两种状态**的那一条：B4 之前格子按下标取 ⇒ **恒 `150`**，
                        //     前两条（等于 `DailyData` 那两个访问器）也会**同时**红。
                        Check(ArtOf(FindChild(rowA2, "Reward 0")), DailyData.DailyRewardArt(0),
                              "…奖励格的**图标**画的是**新那条任务**的（`DailyRewardArt(0)`）");
                        Check(TextOf(FindChild(rowA2, "count 0")), DailyData.DailyRewardText(0),
                              "…**数量**也是新那条任务的（`DailyRewardText(0)`）");
                        CheckTrue(TextOf(FindChild(rowA2, "count 0")) != bCellCnt,
                                  $"…而且**格子里那个数真的跟着变了**：「{bCellCnt}」⇒「"
                                  + TextOf(FindChild(rowA2, "count 0")) + "」（B4 之前它按下标取 = 恒 `150`）");
                        // ⚠️ **图标不另断「必须不同」**：新任务走的是重摇池里那 5 条，币种只有 3 种
                        //    （金 / 封印点 / 骷髅）⇒ 撞上同一个币种是**合法的**（上面那条已按 `DailyRewardArt(0)` 比过）。
                        Debug.Log(P + "   " + pop3.Dump());

                        // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                        //   期望矩形 = **原版 prefab** `ReRollPopup Variant > Window > Generic Popup Background`
                        //   那颗 `Image` 的 rect（535,245 → 1385,675）；⛔ 不写 `MissionRerollPopup.WindowR`
                        //   （那是被测实现**传进去的实参**，同式自证）。
                        //   ⚠️ `pop3` 刚关了 ⇒ 点垃圾桶**另开一扇**（它每次都是 `Create` 出来的新实例）。
                        CheckTrue(pl.ClickAt(tx, ty), "（A94 现场）再点一次垃圾桶、开出一扇重摇窗");
                        var popA = MissionRerollPopup.LastOpened;
                        CheckTrue(popA != null && popA.CurrentState == WindowState.Open, "…开出来了");
                        if (popA != null)
                            CheckAbsorbRule("重摇任务窗", popA.transform, "AbsorbHit",
                                            535f, 245f, 1385f, 675f,
                                            MissionRerollPopup.QShade, MissionRerollPopup.QHit,
                                            () => popA.CurrentState);

                        // ================= ⑨ `Close` 的「先把页签换回这一页、再关」=================
                        // 原版 `MissionReRollPopup__Close.c`：`GetOpenWindow<带页签的窗>()` 非空 ⇒
                        // `GameWindowWithTabs.ChangeTab<MissionsTab>` **然后** `GameWindow.Close`。
                        // ⚠️ 正常点击路径里那一步是**恒等操作**（弹窗那层压暗把命中区压在页之上、鼠标换不了页签）
                        //    ⇒ 用**程序化换页**把它逼出来：不实现那一步的话，最后那条断言会停在 `Campaign`。
                        CheckTrue(pl.ClickAt(tx, ty), "再开一扇（点垃圾桶）");
                        var pop4 = MissionRerollPopup.LastOpened;
                        CheckTrue(pop4 != null && pop4.CurrentState == WindowState.Open, "…开出来了");
                        if (rwA.tabButtons != null) rwA.tabButtons.Click(1);       // 左栏第 2 键 = Campaign
                        Check(rwA.CurrentTab, WindowTabType.Campaign, "（前提）页签已经切到 `Campaign`");
                        CheckTrue(pl.ClickAt(766.15f, 579f), "点 `Cancel` 关掉它（真路径）");
                        CheckTrue(pop4 != null && pop4.CurrentState == WindowState.Closed, "…关掉了");
                        Check(rwA.CurrentTab, WindowTabType.Missions,
                              "关窗后**页签自己回到了 `Missions`**（原版 `MissionReRollPopup__Close.c`：先 "
                              + "`ChangeTab<MissionsTab>` 再 `Close` —— 不实现的话这里会停在 `Campaign`）");

                        // 🆕 **2026-10-16（A796）**：压暗层「**点了会不会关**」—— 走公共口
                        //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs`）。
                        //   🔴 **本口会把窗【真的关掉】** ⇒ 落点 = **§⑨ 之后**（本窗其它断言都跑完之后；
                        //      调用点表 → `资料/普查产出_1015/W7_A796调用点.md` §二 #20 / §四·3）。
                        //   ⚠️ **W7 §四·3 点名的「`Close()` 副作用链」在本落点是零副作用**（逐条查实，别再来一遍）：
                        //     ① 那一刻页签**本来就是 `Missions`**（上面 §⑨ 刚断过），而 `ChangeTab` **只切 `activeSelf`、
                        //        不重建**（`Shell/RewardsWindow.cs` 的 `ChangeTab`：`SetActive` + `OnOpen()` + `RefreshHighlights()`）；
                        //     ② 它调的那句 `MissionsTab.OnOpen()` 是**空实现**（`Shell/MissionsTab.cs`：`public override void OnOpen() { }`）；
                        //     ③ `MissionRerollPopup.Close()` 先 `FindOpenTabbedWindow()` → `tabButtons.Click(0)` 再 `base.Close()`
                        //        （`Shell/MissionRerollPopup.cs`）—— 「先换回这一页再关」那条判据由上面 §⑨ 守着，本块**不碰它**。
                        //   ✅ **不打扰后面那几拍**：`popA` 是上一段 `CheckAbsorbRule` 点关的那一扇 ⇒ 无参 `TryOpen()` 走
                        //      `Closed` 支重建内容（`MissionRerollPopup.Open()` = `LastOpened = this` + `Build()`）；
                        //      `popA` 在**本块之后零引用**，而下面 ① 那一大段用的是 `mtComp` / 现取的 `rowFull`… 与它无关
                        //      （`:9727` 那条「点原来那个位置开不出重摇窗」的对照取的是**本块之后**的 `LastOpened`）。
                        CheckTrue(popA.TryOpen(), "（A796 现场）把重摇窗开回来 —— 下面那条要在**开着**的窗上点");
                        MenuDraw.CheckShadeClickRule(CheckTrue, "重摇任务窗", popA.transform,
                                                     FindChild(popA.transform, "CloseHit"), () => popA.CurrentState);

                        // ================= ① 垃圾桶按「**已领取**」隐藏（谓词 = `IsComplete`）=================
                        // 判据 = `MissionReRollButton : MissionInfoDisplay` + `displayRule = WhenActive(1)`（全包 8 个实例实测）
                        //   ⇒ 基类 `MissionInfoDisplay__Initialize.c` 的 `show = (WhenComplete && IsComplete)
                        //      || (WhenActive && !IsComplete)`；
                        //   ⚠️ 而 **`IsComplete` = 「奖励已领取」**（`MissionChallengeProgress__IsComplete.c` 只读
                        //      `collectedRewards`；且同行的 `progress` 节点带 `displayCompletedMessage` ⇒「到顶未领」必须可见）
                        //   ⇒ **到顶未领取那一行【有】垃圾桶；已领取才藏**（判据正本 `资料/待办判据_阶段二与联机.md` §A23 三·1）。
                        // ⚠️ 必须用**同一行的多种状态**比 —— 只看「不建」分不出「按状态隐藏」和「恒不建」。
                        var mtComp = mtA.GetComponent<MissionsTab>();
                        // 🔴 **先断非空再 `Build()`**（2026-10-04 审查挑出的低危面）：`Check` 记账式、不早退
                        //    ⇒ 缺组件时直调 `Build()` 会 `NullReferenceException`，**自检当场崩、后面一条都不跑**。
                        CheckTrue(mtComp != null,
                                  "`Missions Tab` 上挂着 `MissionsTab`（缺了它下面三段 `Build()` 会把自检崩掉）");
                        int keepP = DailyData.DailyProgressValue(0);
                        // (a) **到顶未领取** ⇒ 垃圾桶**在**（1.0 版正是在这里反了：它按「进度到顶」藏）
                        //     🆕 A44 甲：与垃圾桶**同吃 `displayRule = 1`** 的那 4 件（`Rewards` / `Progress Bar` /
                        //     `progress` / `Collect`）也一起验 —— (a) 与 (b) **只差 `St` 一个字段**
                        //     （`Progress` 两次都是 500）⇒ 这样比才证得住「是按状态显隐」，而不是「恒建 / 恒不建」。
                        DailyData.ForceDailyProgressForTest(0, 500);
                        if (mtComp != null) mtComp.Build();
                        var rowFull = FindChild(mtA, "Daily Mission Container (0)");
                        CheckTrue(rowFull != null && FindChild(rowFull, "Trash mission") != null,
                                  "**到顶未领取**那一行**有**垃圾桶（`IsComplete` = 已领取 ⇒ 到顶不算完成）");
                        CheckTrue(rowFull != null && FindChild(rowFull, "Reward 0") != null,
                                  "…（A44 甲）`Rewards`（`MissionRewardsDisplay` dr1）也在");
                        CheckTrue(rowFull != null && FindChild(rowFull, "Progress Bar") != null,
                                  "…（A44 甲）`Progress Bar`（`MissionProgressBarDisplay` dr1）也在");
                        CheckTrue(rowFull != null && FindChild(rowFull, "progress") != null,
                                  "…（A44 甲）`progress`（`MissionCounterDisplay` dr1）也在");
                        {
                            var cbtn = rowFull != null ? FindChild(rowFull, "Generic UI Button") : null;
                            CheckTrue(cbtn != null,
                                      "…（A44 甲）`Collect`（`MissionInfoDisplay` dr1）也在 —— 到顶那一行**领得到奖**");
                            // 正例：那一点上真鼠标命中的就是它。**这条同时钉住下面 (b) 那条负例量的是同一个坐标**
                            // （坐标错 ⇒ 这里先红，而不是让负例「因为点错了地方」白过 —— 弱断言正是这么长出来的）。
                            if (pl != null && cbtn != null)
                            {
                                var cb = cbtn.GetComponent<WindowButton>();
                                CheckTrue(cb != null && cb.onClick != null,
                                          "…`Collect` 上挂了 `WindowButton`（点了有事做，不是装饰）");
                                CheckTrue(pl.ButtonAt(wantCollect.CX, wantCollect.CY) == cb,
                                          "…而且 `Collect` 原来的位置上命中的**就是它**（引用相等 ⇒ (b) 的负例量的是同一处）");
                            }
                        }
                        // (b) **已领取** ⇒ 藏起来
                        DailyData.ForceDailyClaimedForTest(0, true);
                        if (mtComp != null) mtComp.Build();
                        var rowDone = FindChild(mtA, "Daily Mission Container (0)");
                        CheckTrue(rowDone != null, "（已领取态）第 1 行还在（只是没有垃圾桶）");
                        CheckTrue(rowDone != null && FindChild(rowDone, "Trash mission") == null,
                                  "**已领取**那行的垃圾桶**不建**（原版 `displayRule = WhenActive` ⇒ `!IsComplete` 才显示；"
                                  + "原来我们恒显示 ⇒ 能把一条已领取的任务重摇掉）");
                        CheckTrue(rowDone != null && FindChild(rowDone, "Reward 0") == null,
                                  "…（A44 甲）奖励格 `Rewards` 也**不建**（原来我们恒显示）");
                        CheckTrue(rowDone != null && FindChild(rowDone, "Progress Bar") == null,
                                  "…（A44 甲）`Progress Bar` 也不建");
                        CheckTrue(rowDone != null && FindChild(rowDone, "progress") == null,
                                  "…（A44 甲）`progress` 也不建");
                        CheckTrue(rowDone != null && FindChild(rowDone, "Generic UI Button") == null,
                                  "…（A44 甲）`Collect` 也不建（**领完了才藏** ⇒ 不会「领不了奖」）");
                        // 负例（真路径）：同一个坐标上现在**一个可点件都没有**。
                        // ⚠️ 用 `ButtonAt`（只命中、不派发）：`HoverAt` 会动悬停态，别在这里顺手改别的状态。
                        if (pl != null)
                            CheckTrue(pl.ButtonAt(wantCollect.CX, wantCollect.CY) == null,
                                      "…`Collect` 原来的位置**点不到任何按钮**（真的撤走了 —— 只把它画到下面/被压住会红）");
                        var popBefore = MissionRerollPopup.LastOpened;
                        bool ate = pl.ClickAt(tx, ty);
                        CheckTrue(MissionRerollPopup.LastOpened == popBefore,
                                  "…点原来那个位置**开不出重摇窗**（真路径；这一下"
                                  + (ate ? "被别的件吃了" : "没有任何件吃到") + "）");
                        // (c) 退回未领取 ⇒ 垃圾桶**回来**
                        DailyData.ForceDailyClaimedForTest(0, false);
                        DailyData.ForceDailyProgressForTest(0, keepP);
                        if (mtComp != null) mtComp.Build();
                        var rowBack = FindChild(mtA, "Daily Mission Container (0)");
                        CheckTrue(rowBack != null && FindChild(rowBack, "Trash mission") != null,
                                  "同一条任务、退回未领取 ⇒ 垃圾桶**回来了**（三条合起来才证得住）");
                        CheckTrue(rowBack != null && FindChild(rowBack, "Reward 0") != null
                                  && FindChild(rowBack, "Progress Bar") != null
                                  && FindChild(rowBack, "progress") != null
                                  && FindChild(rowBack, "Generic UI Button") != null,
                                  "…（A44 甲）那 4 件也**一起回来**（三条合起来 ⇒ 既不是恒建、也不是恒不建）");

                        // ================= ⑤ 池子不够时那条**兜底** =================
                        // 🔴 出厂池 **5 条 > 列表 3 条** ⇒ 出厂状态下「候选为空 ⇒ 兜底」**永远到不了**，
                        //    而它正是 2026-10-04 审查挑出来的那一条（当时两个 bug：**可能摇回同一条**= 白点一次
                        //    `Confirm`、`Debug.LogWarning` 的下标**差 1**）⇒ 用 `SetRerollPoolForTest` 换一个
                        //    **两条都已在列表里**的小池子把它逼出来。第一条**故意 == 被顶替的那条描述**
                        //    （旧实现会在游标 0 处把它原样取回来 ⇒ 下面第一条会红）。
                        string fOld = DailyData.DailyDesc(0);
                        DailyData.SetRerollPoolForTest(new[]
                        {
                            new DailyData.Task { Desc = fOld,             Target = 7, RewardText = "70" },
                            new DailyData.Task { Desc = DailyData.DailyDesc(1), Target = 9, RewardText = "90" },
                        });
                        string fGot = DailyData.RerollDaily(0);
                        CheckTrue(fGot != fOld, $"池子不够、走**兜底轮换**时也**不会摇回同一条**：「{fOld}」⇒「{fGot}」");
                        Check(DailyData.DailyCounter(0), "0/9",
                              "…取的是池里**唯一那条与它不同的**（下标 1 · 目标 9 · 进度归 0）");
                        DailyData.SetRerollPoolForTest(null);              // 还原出厂池
                    }
                }
            }
        }

        // ============================================================ §十
        // A266（`Core/TmpFont` 那条「折行宽」的路在**未激活**对象上整趟生成字形 —— 口径 = 形状①）
        // A303（① `CampaignTab.RefreshNodes` 设了 `Clip`/`ClipPad` 却没设 `ClipSoftness`
        //       ② `Campaign Header/Title` 与 `Points` 两处没还原折行）
        // 🔴 **本节的期望值全部来自原版实读 / TMP 源码，⛔ 不读被测实现里的常量**：
        //   · A303① 的 `(0,0)` = 原版 `Campaign Tab/Campaign Track/Viewport` 的 `m_Softness`
        //     （`d:/4/_tmp_view/q1_rm2d.txt:297-298`，同一行还有它的 `pad=(0,0,0,0)`）
        //   · A303② 的「折行=0」= `python 工具/menu_dump.py bundle_menus_assets_all
        //     "Rewards Base Submenu Variant" --depth 6` 的 `折行=` 列（`Campaign Header/Title` 与 `/Points`）
        //   · A266 的「未激活时 `LineCount == 0`」= TMP 源码（`GetTextInfo` 是那条**没有 `m_isAwake` 闸**的路，
        //     而 Phase III 要 `m_renderMode == Render && IsActive()`，`TextMeshPro.cs:360-374 / 5024`）
        Section("§十 A266（折行宽的「生成」延后到激活）+ A303（战役轨道软边 / 页头两处折行）");
        {
            // ================= A303② · `Campaign Header` 那两处 TMP 的折行档 =================
            var campHdr2 = FindChild(camp, "Campaign Header");
            var cTitleNode = FindChild(campHdr2, "Title");
            var cTitleLb = cTitleNode != null ? cTitleNode.GetComponentInChildren<Label>(true) : null;
            CheckTrue(cTitleLb != null, "（A303② 前提）`Campaign Header/Title` 那段字取得到 `Label`");
            if (cTitleLb != null)
            {
                CheckTrue(cTitleLb.WrappingMode >= 0, "（A303② 前提）走的是 TMP 后端（点阵后端「折行」恒 -1）");
                Check(cTitleLb.WrappingMode, 0,
                      "★ `Campaign Header/Title` 的**折行 = 0**（原版实读 `m_TextWrappingMode = 0`；"
                      + "改坏法：删掉 `Shell/CampaignTab.cs` 里 `SetAutoFitBox` 之后那句 `SetWrapping(false)` ⇒ 回 1）");
            }
            var cPtsNode = FindChild(campHdr2, "Points");
            var cPtsLb = cPtsNode != null ? cPtsNode.GetComponentInChildren<Label>(true) : null;
            CheckTrue(cPtsLb != null, "（A303② 前提）`Campaign Header/Points` 那段字取得到 `Label`");
            if (cPtsLb != null)
            {
                CheckTrue(cPtsLb.WrappingMode >= 0, "（A303② 前提）走的是 TMP 后端");
                Check(cPtsLb.WrappingMode, 0,
                      "★ `Campaign Header/Points` 的**折行 = 0**（原版实读；同一条 MB 里 `Title` 也是 0）");
            }

            // ================= A303① · 战役轨道**视口节点**那两个字段（A435 迁移后的形状）=================
            // 🔴 **2026-10-13（A435 甲）改写（铁律 5 —— 时点变了，旧夹具迁完会变【空转】）**：
            //   旧写法 = 「把 `(10000,10000)` 毒在**窗字段** `win.ClipSoftness` 上，再叫 `RefreshNodes()` 重建一趟
            //   ⇒ 它内部必须自己把 `ClipSoftness` 拨回 `(0,0)` ⇒ 这一趟建的 quad 一个都不该带软边」。
            //   迁到「裁切状态长在**视口节点**上」之后本页**不再写**窗字段 ⇒ 旧夹具里那句
            //   `win.ClipSoftness = (10000,10000)` **不影响任何东西**（= 空转，删掉照样绿）。
            //   ⇒ 毒改下在**节点**上（`Campaign Track/Viewport` 那颗 `ViewportClip`），按节点态拆成三条：
            //     ① 节点上写的是**原版那两个值**（`pad=(0,0,0,0)` · `soft=(0,0)`；判据 `q1_rm2d.txt:297-298`）
            //        —— 期望值是**字面量**，⛔ 不读 `CampaignTab` 的常量（那是拿实现证明实现）；
            //     ② **控制组**：把节点 `padding`/`softness` 毒成非 0 再跑一趟 `RefreshNodes()` ⇒ 那一趟建的 quad
            //        **必须**带顶点色（= 节点态这条渐隐带是**活的**）；**同一条**再断「节点逐位没被动过」
            //        （节点那两颗是【常驻】状态，不是每次重建时被生产代码搬来搬去的东西）；
            //     ③ **实验组**：节点复位成原版 `(0,0)` 再重来一趟 ⇒ 这一趟建的 quad **一个都不许带软边**。
            var campTab = camp != null ? camp.GetComponent<CampaignTab>() : null;
            var trk = campTab != null ? campTab.TrackScroll : null;
            CheckTrue(trk != null, "（A303① 前提）战役轨道的滚动区取得到（`CampaignTab.TrackScroll`）");
            if (trk != null && camp != null)
            {
                var vp = trk.Viewport;                     // 原版 `Campaign Tab/Campaign Track/Viewport` 那块
                var trkVp = FindPath(camp, "Campaign Track/Viewport");
                var trkVc = trkVp != null ? trkVp.GetComponent<ViewportClip>() : null;
                CheckTrue(trkVc != null,
                          "（A303① 前提）轨道视口那颗节点（`Campaign Track/Viewport`）挂着 `ViewportClip`"
                        + " —— 这是 A435 迁移的落点；⛔ 没挂上 ⇒ 下面三条没意义");
                if (trkVc != null)
                {
                    // ① 节点上写的是**原版那两个值**（原版 `RectMask2D` 实读：`q1_rm2d.txt:297-298`）
                    CheckTrue(trkVc.padding == Vector4.zero && trkVc.softness == Vector2Int.zero,
                              $"★ 轨道节点上写的是**原版那两个值**（实测 pad=({trkVc.padding.x},{trkVc.padding.y},"
                            + $"{trkVc.padding.z},{trkVc.padding.w}) · soft=({trkVc.softness.x},{trkVc.softness.y})；"
                            + "期望 **pad=(0,0,0,0) · soft=(0,0)** —— 判据 `q1_rm2d.txt:297-298`）");
                    var vcPad0 = trkVc.padding; var vcSoft0 = trkVc.softness;
                    // 数「`CampaignNode_*` 子树里带顶点色的 quad」——**只数这一趟重建出来的那棵子树**
                    //   （判据 = `BuildNode` 里那句 `RewardsWindow.Node(_trackContent, "CampaignNode_" + i, r)`）。
                    //   ⛔ **别数整页**：阵营条那批 quad 是 `BuildArmyItems` 建的，可能带着**别人留下的**状态
                    //   ⇒ 数整页会给出**假红**（本条的鉴别力也不需要它）。
                    int quadsSeen = 0;
                    System.Func<int> softUnderNodes = () =>
                    {
                        quadsSeen = 0; int soft = 0;
                        foreach (var q in camp.GetComponentsInChildren<ImageQuad>(true))
                        {
                            if (q == null) continue;
                            bool underNode = false;
                            for (var p = q.transform; p != null && p != camp; p = p.parent)
                                if (p.name.StartsWith("CampaignNode_", System.StringComparison.Ordinal))
                                    { underNode = true; break; }
                            if (!underNode) continue;
                            quadsSeen++;
                            if (q.CornerColors != null && q.CornerColors.Length == 4) soft++;
                        }
                        return soft;
                    };
                    // ⚠️ **先抓一个旧节点**：`RefreshNodes` 是「先销毁再重建」（`DestroyImmediate`），
                    //    抓着的那个引用在这一趟之后必须变成 `null` —— 否则说明它**早退**了、
                    //    下面那条「带顶点色」就是**空转**（旧节点本来就干净）。
                    Transform nodeBefore = null;
                    foreach (var t0 in camp.GetComponentsInChildren<Transform>(true))
                        if (t0.name.StartsWith("CampaignNode_", System.StringComparison.Ordinal)) { nodeBefore = t0; break; }
                    bool hadNodeBefore = nodeBefore != null;
                    // ② 控制组（并把「节点不许被改」一起断掉）：毒下在**节点**上，跑一趟 `RefreshNodes()`
                    var padPoison = new Vector4(40f, 40f, 40f, 40f);
                    var softPoison = new Vector2Int(10000, 10000);
                    trkVc.padding = padPoison; trkVc.softness = softPoison;
                    campTab.RefreshNodes();
                    CheckTrue(hadNodeBefore, "（A303① 前提）开跑前抓得到一颗 `CampaignNode_*`（抓不到就没法观测重建）");
                    CheckTrue(nodeBefore == null,
                              "（A303① 前提）这一趟 `RefreshNodes` **真的重建了**（开跑前抓的那颗已被销毁）"
                              + " —— 没有这一条，下面两条可能只是因为它早退了（空转）");
                    CheckTrue(trkVc.padding == padPoison && trkVc.softness == softPoison,
                              "★★ `RefreshNodes()`（含 `BuildLine`/`BuildNode`）**一个字段都不许动那颗节点**"
                            + "（毒值原样还在）—— 节点那两颗是【常驻】状态（原版 `RectMask2D` 就长在视口节点上），"
                            + "不是每次重建时被生产代码搬来搬去、再搬回去的东西"
                            + "｜改坏法：把 `Shell/CampaignTab.cs` 的 `RefreshNodes` 里那三行 `_win.Clip…` 加回去"
                            + "（顺带把节点也写一遍）⇒ 红");
                    int softPoisoned = softUnderNodes();
                    CheckTrue(quadsSeen > 0 && softPoisoned > 0,
                              $"（A303① **控制组**）节点 `padding`/`softness` ≠ 0 ⇒ 这一趟重建出来的 quad "
                            + $"**带顶点色**（实测 {softPoisoned}/{quadsSeen} 颗）"
                            + " —— = 节点态这条渐隐带是**活的**；少了这一条，下面那条「一个都没吃到」可能是空转");
                    // ③ 实验组：节点复位成原版 `(0,0)`，重来一趟 ⇒ 这一趟建的 quad 一个都不许带
                    trkVc.padding = vcPad0; trkVc.softness = vcSoft0;
                    campTab.RefreshNodes();
                    int softQuads = softUnderNodes();
                    CheckTrue(quadsSeen > 0, $"（A303① 前提）复位后重建，轨道节点上确实有 quad（实测 {quadsSeen}）");
                    Check(softQuads, 0,
                          "★ **节点上是原版的 (0,0) ⇒ 这一趟重建出来的 quad 一个都没吃到软边**"
                          + "（判据 = 原版 `Campaign Tab/Campaign Track/Viewport` 的 `m_Softness = (0,0)`，"
                          + "`q1_rm2d.txt:297-298`）"
                          + " —— 改坏法：把 `Shell/CampaignTab.cs` 里 `ViewportClip.Hang(…)` 的 softness 实参"
                          + "改成非 0 ⇒ 这一整趟都带顶点色 ⇒ 这里 > 0"
                          + "（控制组已经证明：节点上非零软边 ⇒ 建出来的 quad 会带顶点色）");
                }
            }

            // ================= A266 · 折行宽的「生成」延后到激活（口径 = `V4b_三件口径.md` §Q1 形状①）=================
            // 🔴 **复现的就是 A260 那一幕**：`MenuWindowBase.TextBox` → `MenuDraw.TextBox` → `Label.SetWrapWidth`
            //   在**未激活**的父链上建标签（原现场 = 关着的锻造页 + 滚动触发的重建；`Click(2)` 被 A267 提到
            //   §三·b 开头之后，自检里那条路已经不再经过「关着的页」了 ⇒ 这里用一个**夹具自己开关**的空节点复现，
            //   走的是**同一条真实调用链**，⛔ 不是手搓 `SetWrapWidth`）。
            var keepClipA = win.Clip; var keepPadA = win.ClipPad; var keepSoftA = win.ClipSoftness;
            win.Clip = null; win.ClipPad = Vector4.zero; win.ClipSoftness = Vector2.zero;   // 本夹具不考裁切
            var a266Root = new GameObject("A266 Probe Root");
            a266Root.SetActive(false);                 // 🔴 **必须在建标签【之前】关掉**（`AddComponent` 那一刻父链就得是不活的）
            var a266Rect = new PxRect(400f, 120f, 1240f, 200f);      // 840px 宽的框
            var a266Text = "A266 wrap probe: this sentence is deliberately long enough that it cannot fit "
                         + "on a single line inside the probe box, so the wrapped layout must have two or more lines.";
            var a266Lb = win.TextBox(a266Root.transform, a266Rect, a266Text, Color.white, "A266WrapProbe", 45f, 0f);
            CheckTrue(a266Lb != null, "（A266 前提）探针标签建出来了");
            if (a266Lb != null)
            {
                CheckTrue(a266Lb.WrappingMode >= 0,
                          "（A266 前提）走的是 TMP 后端（点阵后端没有折行这回事：`WrappingMode` 恒 -1）");
                Check(a266Lb.WrappingMode, 1,
                      "（A266 前提）`SetWrapWidth` **照旧立刻**把模式设成 `Normal(=1)`（当年就是立刻写的，这一档没变）");
                Check(a266Lb.LineCount, 0,
                      "★ **未激活时【不生成】字形** —— `LineCount == 0`（`SetWrapWidth` 只把宽度和模式写下去，"
                      + "「生成」那一步记成待办）"
                      + " —— 改坏法：把 `Battle/Label.cs` 的 `SetWrapWidth` 改回当场 `GenerateLayout` ⇒ 立刻 > 0");
                // A260 那个 NRE 的入口：未激活时 `ClipText` **不该**去上传（上传目标 `MeshFilter.sharedMesh` 当时不存在）
                int skipBefore = MenuDraw.TextClipUploadSkipped;
                MenuDraw.ClipText(a266Lb, new PxRect(500f, 120f, 900f, 200f), Vector2.zero);
                Check(MenuDraw.TextClipUploadSkipped, skipBefore,
                      "★ 未激活时 `ClipText` **不去上传那一刀**（`TextClipUploadSkipped` 不动）——"
                      + " 字模没生成 ⇒ 那条上传路根本不进（A260 的 NRE 就是它踩的）"
                      + " —— 改坏法：同上（改回当场生成）⇒ 字模有了、网格没有 ⇒ 计数 +1");
                int appliedBeforeActivate = a266Lb.PendingWrapAppliedCount;   // 激活前（按设计 = 0）
                a266Root.SetActive(true);
                int appliedAtActivate = a266Lb.PendingWrapAppliedCount;       // 🔬 诊断读数（下面那条断言会把它印进日志）
                // 🔴 **必须在 `SetActive` 【之后】重排**：`TextMeshPro.Awake()`（第一次激活时跑 —— TMP 带
                //    `[ExecuteAlways]`，`TextMeshPro.cs:19`；批处理下这个特性**确实生效**：**本夹具的日志里**
                //    就有 `WindowHolder`（同为 `[ExecuteAlways]`）在 `AddComponent` 那一刻打出告警
                //    「锚点 placement = None」= 它的 `OnEnable` 真的跑了）会把 `m_textInfo` 换成
                //    **一棵新的**（`TextMeshPro.cs:582-593`，只在 `m_mesh == null` 时）
                //    ⇒ **激活前生成过的 `lineCount` 必然被那一下清零** ⇒ 直接读它恒为 0（**旧写法就是这么红的**）。
                //    `ForceRelayout()` 走 `ForceMeshUpdate()`（此刻已激活、且 `Awake` 已跑 ⇒ `m_isAwake == true`
                //    ⇒ `TextMeshPro.cs:2119` 那道闸放行）⇒ 版面回来。
                a266Lb.ForceRelayout();
                // ★ **新判据（DIAG-B §三·#11(b)）**：「未激活时欠下的那一刀」真的被**兑现**过 —— 读的是
                //   `Label.PendingWrapAppliedCount`（**不依赖 `m_textInfo`**，所以躲开了上面那条坑）。
                //   ⚠️ **兑现点有两种、本条两种都收**：①【激活那一刻】由 `Label.OnEnable` 兑现（`Battle/Label.cs` 的 `OnEnable`）；
                //      ②【激活之后第一次真重排 / 有人要真数】由 `RefreshBounds` 的尾句或 `WorldW` 的 `EnsureMeasured` 兑现。
                //      批处理（编辑模式）下 `Awake/OnEnable` **只对 `[ExecuteAlways]` 的脚本**才跑
                //      （**四条独立记录**：`资料/已知的坑.md:704` · `Shell/PromptPopup.cs` 的 `WindowButton` 类注 ·
                //      `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 · `Shell/PointerLayer.cs:47-48`），而 **`Label` 没有那个特性**
                //      ⇒ 自检里走的是 ②。⚠️ **那一格（①）本夹具判不到** —— 所以这条**断不死**「就是 `OnEnable` 那一跳」；
                //      实测读数印在下一条消息的 `appliedAtActivate` 里（**= 激活那一刻的计数**，供下次跑时坐实）。
                //   ⚠️ 次序有意义：本条必须在**第一次读 `WorldW` 之前**（`WorldW` 的 getter 走 `EnsureMeasured`，
                //      那条路**也**会把待办销掉、但**不计数** ⇒ 摆在它后面这条会假红）。
                //   改坏法：① 把 `SetWrapWidth` 的未激活分支改回**当场生成**（待办根本不存在）；
                //   ② 删掉 `RefreshBounds` 尾句那次 `TryApplyPendingWrap()` ⇒ 计数**一次都不动** ⇒ 红
                //   （⚠️ 单删 `OnEnable` 这一条**报不出来** —— 见上面那格，如实说）。
                CheckTrue(a266Lb.PendingWrapAppliedCount > appliedBeforeActivate,
                          $"★ …而且那一刀是走【待办兑现】那条路补上的（兑现计数 {appliedBeforeActivate}"
                          + $" → {a266Lb.PendingWrapAppliedCount}；**激活那一刻的读数是 {appliedAtActivate}**）"
                          + " —— 旧写法断的 `LineCount` 证不了这一条（`TextMeshPro.Awake()` 会把 `m_textInfo` 换掉）、"
                          + "`WorldW` 也证不了（它的 getter 自己会 `EnsureMeasured()` 兜底兑现）");
                // ⚠️ 这一读**自己会兑现兜底**（`EnsureMeasured` → `TmpFont.GenerateLayout` → `GetTextInfo`，
                //    那条路**没有 `m_isAwake` 闸**）⇒ 下面那条 `LineCount` 与 `m_isAwake` / 谁先跑**都无关**。
                float a266W = a266Lb.WorldW * 108f;
                CheckTrue(a266Lb.LineCount >= 2,
                          $"★ **激活之后**：版面按框宽真的折了行（实测 {a266Lb.LineCount} 行）"
                          + " —— 读之前必须先**在激活之后重排一次**（`ForceRelayout()` 或上面那次 `WorldW` 的兜底兑现），"
                          + "否则读到的是 `TextMeshPro.Awake()` 清过零/还没建的那一份"
                          + "；改坏法：把 `Battle/Label.cs` 的 `SetWrapWidth` 里那句 `TmpFont.SetWrapWidthRect` "
                          + "删掉（框宽压根没写下去）⇒ 一行装得下 ⇒ 这里 1 行 ⇒ 红");
                CheckTrue(a266W > a266Rect.W * 0.5f && a266W <= a266Rect.W + 1f,
                          $"（结果）折行宽真的落到了框内（{a266W:F0}px，≤ 框宽 {a266Rect.W:F0}）——"
                          + " 没兑现时它是垃圾值（没生成过的 TMP 给 0 / 天文数字，见 `HasMeasuredWidth` 的文件头）。"
                          + " ⚠️ 本条**不**证「`OnEnable` 那一跳」：`WorldW` 的 getter 自己会 `EnsureMeasured()` 兑现"
                          + "（`Battle/Label.cs` 的 `WorldW`），把 `OnEnable` 删掉它照样绿 —— 那一半见上面那条计数器");
                Check(a266Lb.WrappingMode, 1, "…而这一趟里模式**没有被待办改回去**（兑现那一刀一个值都不回写）");
            }
            Object.DestroyImmediate(a266Root);
            win.Clip = keepClipA; win.ClipPad = keepPadA; win.ClipSoftness = keepSoftA;

            // ================= 🆕 A327：两态夹具（判据 → `资料/普查产出_1011/W4_子3.md` §四·b）=================
            //   2026-10-11 那 8 处「世界 → 设计」的修法在 **`k == 1` 时与旧式逐位相同** ⇒ 现有断言**一条都照不出**
            //   （⛔ 不是「有断言挡着」，是**还没有断言**）。本小节给其中两处补牙口（夹具本体 = 上面那个
            //   `CheckScaleTwo`，⛔ 别在别处再写一套）：
            //     · **A306⑤** = `CampaignTab.BuildLine` 的落位（基准 = `Content`，离窗根 ≈1.5 设计单位）
            //     · **C**     = `CampaignTab.BuildArmyItems` 的三件套（`Clip` / `ClipPad` / `ClipSoftness`）
            SmallScreenUI.PersistOverride = true;     // ⛔ 自检不许动玩家的真设置（同 `SettingsScene`）
            var campTabA = camp != null ? camp.GetComponent<CampaignTab>() : null;
            CheckTrue(campTabA != null, "（A327 前提）`CampaignTab` 取得到（战役页在本夹具里是开着的）");
            if (campTabA != null)
            {
                // ---- A306⑤：`BuildLine` 的落位 ----
                // 🔴 **2026-10-11（A346）补牙口**：`measure` 里**必须重建**（判据 = 夹具文件头 ③）——
                //   不重建时 `NodeLine_*` 的 `localPosition` 是 `k == 1` 那一趟**冻结**下来的值，
                //   而 `p2 == M × p1` 那时对**任何**实现都成立（恒等式）⇒ 那条 ★ 就**不是**在验 `BuildLine`。
                // ✔ 重建口 = `RefreshNodes()`：它首句 `ClearTrackContent()` 把 `_trackContent` 的子件全
                //   `DestroyImmediate` 掉、再重跑 `BuildLine` —— 两处都是 `Shell/CampaignTab.cs` 的**唯一一份**。
                campTabA.RefreshNodes();                        // 保证 `NodeLine_*` 在（是 `BuildLine` 建的）
                Transform lineA = null;
                foreach (var t in camp.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("NodeLine_", System.StringComparison.Ordinal)) { lineA = t; break; }
                CheckTrue(lineA != null, "（A306⑤ 前提）轨道里有 `NodeLine_*`（`RefreshNodes` → `BuildLine`）");
                if (lineA != null)
                {
                    // ⚠️ **基准 = `lineA.parent`（= `_trackContent`，名叫 `Content`）**：`RefreshNodes()` 销毁的是
                    //   `Content` 的**子件**、不是 `Content` 自己（`ClearTrackContent` 只遍历 `_trackContent` 的孩子）
                    //   ⇒ 基准**全程活着**（夹具在两次 `measure()` 之后还要读 `basis.position` / `basis.parent.lossyScale`
                    //   —— 基准要是被重建掉，那两读会抛 `MissingReferenceException`，**不是**断言红）。
                    // ✔ **前提① 成立**：`Content.parent` = `Viewport` → `Campaign Track` → `Campaign Tab` → `Tabs`
                    //   → `Content Area` → **窗根**（每一级都是 `Node()` 建的、`localScale` 恒 1）
                    //   ⇒ `basis.parent.lossyScale == 窗根.lossyScale == M`（正是 `PosInDesignSpace` 除的那一级）。
                    var lineParentA = lineA.parent;
                    CheckScaleTwo(win.gameObject, lineParentA,
                                  () =>
                                  {
                                      campTabA.RefreshNodes();          // 🔴 态二**必须重建**（见夹具文件头 ③）
                                      Transform n = null;
                                      foreach (var t in camp.GetComponentsInChildren<Transform>(true))
                                          if (t.name.StartsWith("NodeLine_", System.StringComparison.Ordinal)) { n = t; break; }
                                      // （前提③）**真的重建了**：拿到的是**新**节点 ⇒ 被量的 `localPosition` 是在
                                      // `k ≠ 1` 那一趟**重算**出来的，不是态一冻结的那份。
                                      // 改坏法：`measure` 改回纯读（`() => lineA.position`）⇒ 这条红
                                      // （而 ★ 同时退化成假绿 —— 两件事一起被挡住）。
                                      CheckTrue(n != null && n != lineA,
                                                "（前提）A306⑤：态二的 measure **真的重建了** `NodeLine_*`"
                                              + "（`RefreshNodes()` 首句清空 `_trackContent` 的子件 ⇒ 拿到的是新节点）");
                                      if (n != null) lineA = n;
                                      return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                  },
                                  1.2f,
                                  "A306⑤ `CampaignTab.BuildLine` 的落位"
                                + " —— 改坏法：`Shell/CampaignTab.cs` 里把 `PosInDesignSpace(parent)` 换回裸"
                                + " `parent.position` ⇒ 偏 `(M−1)×|基准在态二的世界位置|` = 0.24×|(1.53,−1.16)|"
                                + " ≈ **0.46 设计单位 = 50px**（容差 0.02 = 2.2px）⇒ 红"
                                + "（`|基准|` 的实测值见 `资料/普查产出_1011/FX3_夹具砸脚八条红修复.md` §四·3）");
                }

                // ---- C：三件套（**毒下在【视口节点】上** + 控制组；🔴 2026-10-13 · A435 甲 改写）----
                // 旧写法 = 「把 pad/soft 毒在**窗字段**上 ⇒ `BuildArmyItems` 内部必须自己把它们拨回原版值」。
                // 迁到「状态长在**视口节点**上」之后本页不再写窗字段 ⇒ 旧夹具**空转**（删掉照样绿）。
                // 按节点态拆成两态：毒着节点跑一趟（控制组 + 断「节点不许被改」）、复位再跑一趟（原判据）。
                var asA = campTabA.ArmyScroll;
                var selRA = asA != null ? asA.Viewport : default(PxRect);
                CheckTrue(asA != null, "（C 前提）阵营条的滚动区取得到（`CampaignTab.ArmyScroll`）");
                if (asA != null)
                {
                    var asVp = FindPath(camp, "Campaign Army Selector/Viewport");
                    var asVc = asVp != null ? asVp.GetComponent<ViewportClip>() : null;
                    CheckTrue(asVc != null,
                              "（C 前提）阵营条视口那颗节点（`Campaign Army Selector/Viewport`）挂着 `ViewportClip`"
                            + " —— 这是 A435 迁移的落点；⛔ 没挂上 ⇒ 下面几条没意义");
                    if (asVc != null)
                    {
                        // ① 节点上写的是**原版那两个值**（原版 `RectMask2D` 实读：`q1_rm2d.txt:186` 的 `soft=(0,0) pad=(0,0,0,0)`）
                        CheckTrue(asVc.padding == Vector4.zero && asVc.softness == Vector2Int.zero,
                                  $"★ 阵营条节点上写的是**原版那两个值**（实测 pad=({asVc.padding.x},{asVc.padding.y},"
                                + $"{asVc.padding.z},{asVc.padding.w}) · soft=({asVc.softness.x},{asVc.softness.y})；"
                                + "期望 **pad=(0,0,0,0) · soft=(0,0)** —— 判据 `q1_rm2d.txt:186`）");
                        var cPad0 = asVc.padding; var cSoft0 = asVc.softness;
                        // 数「`CampaignArmyItem_*` 子树里的 quad」：返回带顶点色的颗数，
                        //   `seenA` / `hitL`（最左那颗 `Hit` 的渲染左沿）从闭包带出。
                        int seenA = 0; float hitL = float.MaxValue;
                        System.Func<int> scanItems = () =>
                        {
                            seenA = 0; hitL = float.MaxValue; int soft = 0;
                            foreach (var q in camp.GetComponentsInChildren<ImageQuad>(true))
                            {
                                if (q == null) continue;
                                bool underItem = false;
                                for (var p = q.transform; p != null && p != camp; p = p.parent)
                                    if (p.name.StartsWith("CampaignArmyItem_", System.StringComparison.Ordinal))
                                        { underItem = true; break; }
                                if (!underItem) continue;
                                seenA++;
                                if (q.CornerColors != null && q.CornerColors.Length == 4) soft++;
                                if (q.name == "Hit")
                                {
                                    float hx1, hy1, hx2, hy2;
                                    if (QuadRectOf(q, out hx1, out hy1, out hx2, out hy2) && hx1 < hitL) hitL = hx1;
                                }
                            }
                            return soft;
                        };
                        // 🔴 `MenuScroll.SetOffset` 在「与当前值差 < 0.01」时**不通知**（`OnChanged` 不跑）
                        //   ⇒ 先离开最左、再回最左，保证**一定**走一趟 `BuildArmyItems`。
                        Transform itemBefore = null;
                        foreach (var t0 in camp.GetComponentsInChildren<Transform>(true))
                            if (t0.name.StartsWith("CampaignArmyItem_", System.StringComparison.Ordinal)) { itemBefore = t0; break; }
                        bool hadItemBefore = itemBefore != null;
                        // ② 控制组（并把「节点不许被改」一起断掉）：pad / 软边**双双毒在节点上**，滚到最左极端
                        var padPoison = new Vector4(40f, 40f, 40f, 40f);
                        var softPoison = new Vector2Int(10000, 10000);
                        asVc.padding = padPoison; asVc.softness = softPoison;
                        asA.SetOffset(asA.MinOffset + 50f);
                        asA.SetOffset(asA.MinOffset);                  // ⇒ `OnChanged` ⇒ 走一趟 `BuildArmyItems`
                        CheckTrue(hadItemBefore, "（C 前提）下毒之前抓得到一颗 `CampaignArmyItem_*`");
                        CheckTrue(itemBefore == null,
                                  "（C 前提）这一趟**真的重建了**阵营条（开跑前抓的那颗已被销毁）—— 否则下面是空转");
                        CheckTrue(asA.Offset <= asA.MinOffset + 0.01f,
                                  "（C 前提）滚到了**最左极端** ⇒ 内容左端压在视口左沿（`MinOffset` 的定义就是这个）");
                        CheckTrue(asVc.padding == padPoison && asVc.softness == softPoison,
                                  "★★ `BuildArmyItems()`（含 `BuildArmySlider` / `AddHit`）**一个字段都不许动那颗节点**"
                                + "（毒值原样还在）—— 节点那两颗是【常驻】状态，不是每次重建时被生产代码搬来搬去的东西"
                                + "｜改坏法：把 `Shell/CampaignTab.cs` 的 `BuildArmyItems` 里那三行 `_win.Clip…` 加回去"
                                + "（顺带把节点也写一遍）⇒ 红");
                        int softPoisoned = scanItems();
                        CheckTrue(seenA > 0, $"（C 前提）下毒那一趟重建之后阵营条上确实有 quad（实测 {seenA}）");
                        CheckTrue(softPoisoned > 0,
                                  $"（C **控制组**）节点 `padding`/`softness` ≠ 0 ⇒ 这一趟出来的 quad **带顶点色**"
                                + $"（实测 {softPoisoned}/{seenA} 颗）—— = 节点态这条带是**活的**；"
                                + "少了这一条，下面「一个都没吃到」可能是空转");
                        CheckNear(hitL, selRA.x1 + 40f, 2f,
                                  "★ C（pad 那一半 · **控制组**）：节点 pad = (40,40,40,40) ⇒ 最左那颗的"
                                + "**命中区左沿被节点那个 pad 缩到 `V.x1 + 40`**"
                                + "（判据 = `R ∩ (V − pad)`，且 pad 缩的是 **mask 自己那个框**）"
                                + " —— ⛔ 只接渲染那一半、或把 pad 丢掉的实现会停在 `V.x1` 上（差 **40px**，"
                                + "远大于本条 2px 的容差）⇒ 红");
                        // ③ 实验组：节点复位成原版 `(0,0,0,0)`/`(0,0)`，再滚一趟（强制走 `OnChanged`）
                        asVc.padding = cPad0; asVc.softness = cSoft0;
                        asA.SetOffset(asA.MinOffset + 50f);
                        asA.SetOffset(asA.MinOffset);
                        int softA = scanItems();
                        CheckTrue(seenA > 0, $"（C 前提）复位后重建，阵营条上确实有 quad（实测 {seenA}）");
                        Check(softA, 0,
                              "★ C（软边那一半）：**节点上是原版的 (0,0) ⇒ 这一趟 `BuildArmyItems` 出来的 quad"
                            + " 一个都没吃到软边**"
                            + "（判据 `q1_rm2d.txt:186` 的 `soft=(0,0)`）"
                            + " —— 改坏法：把 `Shell/CampaignTab.cs` 里 `ViewportClip.Hang(…)` 的 softness 实参"
                            + "改成非 0 ⇒ 整条阵营条都带顶点色 ⇒ 这里 > 0（控制组已证明这条带是活的）");
                        CheckTrue(hitL <= selRA.x1 + 1f,
                                  $"★ C（pad 那一半）：最左那颗**命中区的左沿 = 视口左沿 {selRA.x1:F2}**（实测 {hitL:F2}）"
                                + " —— 滚到最左极端时第 1 格正好压在左沿，而命中区 = `R ∩ (V − pad)`、"
                                + "渲染 = `R ∩ (V − pad)`（`RenderClip`）⇒ pad = 0 时就停在 `V` 上。"
                                + "**改坏法**：把 `ViewportClip.Hang(…)` 的 pad 实参改成 `(40,40,40,40)`"
                                + "（= 旧写法里那句 `_win.ClipPad = Vector4.zero;` 被删掉的效果）"
                                + " ⇒ 命中区与渲染都被缩到 `selRA.x1 + 40` ⇒ 这一条红");
                    }
                }
            }
            SmallScreenUI.Set(false);
            SmallScreenUI.PersistOverride = false;
            CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327：自检跑完把开关放回**出厂值 关**");

            // ================= 🆕 2026-10-09（`A1137`）：`CampaignTab.BuildLine` 的**视口剔除**在非 16:9 下的两态 =================
            // 本节补的是 `A1092` 那一修（`Shell/CampaignTab.cs:581-584`：剔除的中心与半宽**成对**换到
            //   `ToDesignPixel` + `PxPerWorldX`）的**牙口** —— 那一修在 16:9 与旧式【逐位相同】⇒ 现有断言一条都照不出。
            // 判据（原版）= `RectMask2D`：整段在视口外就该**不建**；中心与半宽必须**同一条斜率**。
            // 🔴 两态 = **宽高比**（`cam.aspect` = 16:9 / 4:3 / 21:9）。16:9 是「旧实现也过」的对照档 ⇒ **只断非 16:9 那两档**。
            // 🔴 **必须 `RefreshNodes()` 重建**（E15 §④ 那套没这一步 ⇒ 照它写会量到 16:9 冻结的树：位置在**建的那一刻**算一次）。
            // ⚠️ 与 `A327` 的 `CheckScaleTwo` **不同源**（那个改 `SmallScreenUI` 的父链缩放 `M`、不是宽高比）。
            {
                var a1137Cam = LayoutSpace.Cam;
                CheckTrue(a1137Cam != null,
                          "（前提·不静默）A1137：`LayoutSpace.Cam` 在（`VisibleWidth` 由它给；不在 ⇒ 本节等于没验）");
                var a1137Tab = camp != null ? camp.GetComponent<CampaignTab>() : null;
                CheckTrue(a1137Tab != null && a1137Tab.TrackScroll != null,
                          "（前提·不静默）A1137：`CampaignTab` 取得到、滚动区在");
                if (a1137Cam != null && a1137Tab != null && a1137Tab.TrackScroll != null)
                {
                    // ⚠️ `_vpR` 没有公开只读口 ⇒ 走 `TrackScroll.Viewport`（`CampaignTab.cs:325` 把 `_vpR` **原样**传进去，
                    //    今天逐位同源）—— ⛔ **别**给 `CampaignTab` 新增 `VpRectForTest`（两处写同一条规则）。
                    var a1137Vp = a1137Tab.TrackScroll.Viewport;
                    var a1137Back = a1137Cam.aspect;
                    System.Func<Transform> a1137FirstLine = () =>
                    {
                        foreach (var t in camp.GetComponentsInChildren<Transform>(true))
                            if (t.name.StartsWith("NodeLine_", System.StringComparison.Ordinal)) return t;
                        return null;
                    };
                    try
                    {
                        foreach (float asp in new[] { LayoutSpace.DesignAspect, 4f / 3f, 21f / 9f })
                        {
                            a1137Cam.aspect = asp;                       // ⚠️ 必须在【重建之前】改（位置在建的那一刻算一次）
                            var beforeLine = a1137FirstLine();
                            a1137Tab.RefreshNodes();                     // 🔴 重建
                            var afterLine = a1137FirstLine();
                            CheckTrue(afterLine != null && afterLine != beforeLine,
                                      $"（前提）A1137：换完 aspect（{asp:F4}）之后 `RefreshNodes()` **真的换了实例**"
                                    + " —— 拿的是**新**节点 ⇒ 几何是在本档下重算的，不是 16:9 冻结的那份。"
                                    + "🧨 把 `RefreshNodes()` 从夹具里删掉 ⇒ 拿到上一档的实例 ⇒ 红");
                            float a1137R = LayoutSpace.VisibleWidth / LayoutSpace.DesignWidth;
                            bool a1137IsDesign = Mathf.Abs(asp - LayoutSpace.DesignAspect) < 1e-4f;
                            if (!a1137IsDesign)
                                CheckTrue(Mathf.Abs(a1137R - 1f) > 0.1f,
                                          $"（前提·本档有鉴别力）A1137：`{asp:F4}` 档 |r−1| > 0.1（r = VisibleWidth/DesignWidth = {a1137R:F4}）");
                            // P4：两半的口径确实分家（旧式 `ToPixel`[写死 108] vs 新式 `ToDesignPixel`[实测 VisibleWidth]）
                            float a1137Sep = 0f;
                            for (int i = 0; i < CampaignData.NodeCount; i++)
                            {
                                var nr = a1137Tab.NodeRectForTest(i);
                                var w = LayoutSpace.FromPixel((nr.x1 + nr.x2) * 0.5f, (nr.y1 + nr.y2) * 0.5f);
                                a1137Sep = Mathf.Max(a1137Sep,
                                                     Mathf.Abs(LayoutSpace.ToPixel(w).x - LayoutSpace.ToDesignPixel(w).x));
                            }
                            if (!a1137IsDesign)
                                CheckTrue(a1137Sep > 50f,
                                          $"（前提·本档不是空转）A1137：旧式（`ToPixel` 写死 108）与新式（`ToDesignPixel`）"
                                        + $"在某一对上差 **{a1137Sep:F1}px** > 50 —— 不成立 ⇒ 本节没验到东西");
                            // 一次性收名字表（⛔ 别每条都 `GetComponentsInChildren`）
                            var a1137Names = new System.Collections.Generic.HashSet<string>();
                            foreach (var t in camp.GetComponentsInChildren<Transform>(true)) a1137Names.Add(t.name);
                            // ---- A 上界（不许多建）：每条**建出来的**连线，其【真 x 区间】必须与视口相交 ----
                            //   真区间 = 两端节点中心的**设计 px** x 跨度（`NodeRectForTest` 是**纯算式** ⇒ 节点剔没剔都算得出）。
                            //   🔴 **容差 100px（不是 §二·3 写的 ±15）—— 本次自己按几何重算的**：
                            //     正确实现那条剔除用 `halfLen = |ab|·PxPerWorldX/2`（把整段长度当成沿 x）⇒ 比真 x 跨度**宽**；
                            //     本 camp 图最陡一跳 = `UM1→UM3`（内容 dx=320 / dy=256）⇒ 乘 `Ratio`（= (759.06−100)/(2×264)
                            //     ≈ **1.2482**）后 (399.4, 319.5) 设计 px ⇒ 过估 = (511.6−399.4)/2 ≈ **56px**，
                            //     再加厚度项 `Px(LineH)·PxPerWorldX/2`（16:9 5.0 / 4:3 6.7 / 21:9 3.8）与 ±2 的 slack
                            //     ⇒ 上界 **≈ 65px** ⇒ 取 **100** 留 1.5 倍余量（**对正确实现不报红**）。
                            //   🔴 **为什么这条判据在三个 aspect 下都成立**：`midPx`/`halfLen` 换回设计 px 后**与宽高比无关**
                            //     （`ToDesignPixel(FromPixel(x)) == x` ⇒ 节点设计 px 位置三档相同；`|ab|·PxPerWorldX` 也相同）
                            //     ⇒ **正确实现的剔除判决三档逐位相同**；而旧式（`ToPixel`/108）在 4:3 是 **0.75×**、21:9 是 **1.3125×**
                            //     ⇒ 判决会变 ⇒ 本条与 B 就是照它两条尾巴来的。
                            int a1137Over = 0; string a1137OverFirst = null;
                            foreach (var a1137Ln in a1137Names)
                            {
                                if (!a1137Ln.StartsWith("NodeLine_", System.StringComparison.Ordinal)) continue;
                                var parts = a1137Ln.Split('_');
                                if (parts.Length != 3) continue;
                                int pi, pj;
                                if (!int.TryParse(parts[1], out pi) || !int.TryParse(parts[2], out pj)) continue;
                                var ri = a1137Tab.NodeRectForTest(pi);
                                var rj = a1137Tab.NodeRectForTest(pj);
                                float lo = Mathf.Min((ri.x1 + ri.x2) * 0.5f, (rj.x1 + rj.x2) * 0.5f);
                                float hi = Mathf.Max((ri.x1 + ri.x2) * 0.5f, (rj.x1 + rj.x2) * 0.5f);
                                if (hi < a1137Vp.x1 - 100f || lo > a1137Vp.x2 + 100f)
                                { a1137Over++; if (a1137OverFirst == null) a1137OverFirst = a1137Ln; }
                            }
                            CheckTrue(a1137Over == 0,
                                      $"★ A1137-A（上界·不许多建）：`{asp:F4}` 档下建出来的连线里，**真 x 区间**落在视口外"
                                    + $"[{a1137Vp.x1:F1},{a1137Vp.x2:F1}]（±100px）的有 **{a1137Over}** 条"
                                    + (a1137OverFirst == null ? "" : $"，第一条 `{a1137OverFirst}`")
                                    + " —— **改坏法**：把 `Shell/CampaignTab.cs:581` 的 `ToDesignPixel` 换回 `ToPixel`（或 `:582` 的"
                                    + " `PxPerWorldX` 换回 `108f`）⇒ 4:3 下区间被**压向中心**（0.75×）⇒ 该剔的没剔 ⇒ 本条红");
                            // ---- B 下界（不许少建）：两端节点**都建出来了** ⇒ 这条连线**必须存在** ----
                            //   期望值来自**两个独立口**：`CampaignData.At(i).Next`（配对表）+ 树里 `CampaignNode_*` 的**存在性**。
                            int a1137Miss = 0; string a1137MissFirst = null;
                            for (int i = 0; i < CampaignData.NodeCount; i++)
                            {
                                if (!a1137Names.Contains("CampaignNode_" + i)) continue;
                                var nx = CampaignData.At(i).Next;
                                if (nx == null) continue;
                                foreach (var jj in nx)
                                {
                                    if (jj == i) continue;          // ⛔ 自环：`BuildLine` 首句 `d.sqrMagnitude < 1e-6` 直接 return
                                    if (!a1137Names.Contains("CampaignNode_" + jj)) continue;
                                    if (!a1137Names.Contains("NodeLine_" + i + "_" + jj))
                                    { a1137Miss++; if (a1137MissFirst == null) a1137MissFirst = i + "_" + jj; }
                                }
                            }
                            CheckTrue(a1137Miss == 0,
                                      $"★ A1137-B（下界·不许少建）：`{asp:F4}` 档下**两端节点都建了、连线却没建**的有 **{a1137Miss}** 对"
                                    + (a1137MissFirst == null ? "" : $"，第一对 `{a1137MissFirst}`")
                                    + " —— 期望值 = `CampaignData.At(i).Next`（配对表）∩ 树里 `CampaignNode_*` 的存在性"
                                    + "（⛔ 不读被测的 `midPx`/`halfLen`）。**改坏法**：21:9 下 `108f > PxPerWorldX(82.29)` 半宽被放大"
                                    + " ⇒ 该建的被剔 ⇒ 本条红");
                        }
                    }
                    finally
                    {
                        a1137Cam.aspect = a1137Back;      // ⚠️ 必须还原
                        a1137Tab.RefreshNodes();          // 🔴 还原后**也要重建** —— 否则本节余下的几何全是 21:9 建的
                    }
                }
            }

            // ================= 🆕 2026-10-13（A353）：`CampaignTab` 四处「原版没有 mask」的件 =================
            // **代码侧**已经在树上（`Shell/CampaignTab.cs` 的私有 `ClearClip()` / `RestoreClip()` 把四处成对包住）；
            // 本节补的是**牙口**（判据 → `资料/普查产出_1012/E1_逐子件宽与Clip清空.md` §三·2）。
            // 判据（原版实读）：路径含 `Campaign Tab` 的 `RectMask2D` **只有两条视口**
            //   （`Campaign Track/Viewport` · `Campaign Army Selector/Viewport`）⇒ 这四件的正确值 = `Clip == null`
            //   （复扫留档 = `资料/普查产出_1012/E1_逐子件宽与Clip清空.md` §六）。
            // 🔴 **为什么必须在【新实例 + 毒框】上量**（在既有那棵 `camp` 树上量 = **空转**）：
            //   这四件是 `Build()` 建的，而第一次 `Build()`（`RewardsWindow.Build → BuildTabContents → Setup()`）
            //   跑的时候 `Clip` 本来就是 `null` ⇒ 在旧树上量，**把那四句 `ClearClip()` 全删掉照样绿**。
            // ⛔ **也别就地 `campTab.Build()`**：`MenuDraw.Node` 是**只建不找**的 ⇒ 会在 `camp` 下再建一整套
            //   同名节点（`_bgQuad`/`_title`/`_points` 重绑、`_armyScroll` 再登记一个滚动区），新/旧分不清。
            //   ⇒ 夹具自己造一个**全新**实例（节点独立 ⇒ 与既有树互不干扰），量完就地销毁。
            // 🔴 **如实标注（⛔ 别读成「三件都验到了」）**：裁切三件套里**今天只有 `Clip` 那一条能单独起作用** ——
            //   `MenuDraw.Rect` 只在 `clip.HasValue` 时才求交 / 采软边、`MenuWindowBase.Text` 只在
            //   `RenderClip.HasValue` 时才 `ClipText`、`MenuDraw.PaddedClip(null, pad)` 第一句就返 `null`
            //   ⇒ 清 `pad` / `soft` 是**纪律件**（把「这一处没有 mask」写全），今天**不产生行为差异**；
            //   ⇒ 下面四条的**改坏法只有「删掉那一处 `ClearClip()`」**（只删 `pad`/`soft` 那两句赋值**今天不红**）。
            if (camp != null)
            {
                var keepClipB = win.Clip; var keepPadB = win.ClipPad; var keepSoftB = win.ClipSoftness;
                var a353Host = MenuDraw.Node(camp.parent, "A353 CampaignTab Probe",
                                             new PxRect(330.69f, 70.94f, 1920f, 1080f));   // ⛔ 新节点，别挂进 `camp`
                var a353Tab = a353Host.gameObject.AddComponent<CampaignTab>();
                a353Tab.SetHost(win, a353Host);
                // 毒值：一块**与四件全不相交**的屏外框（挑「全不相交」是为了让改前四件**一个都不建**，信号最干净）
                win.Clip = new PxRect(2500f, 1200f, 2600f, 1300f);
                win.ClipPad = new Vector4(40f, 40f, 40f, 40f);
                win.ClipSoftness = new Vector2(10000f, 10000f);
                // 控制组：同一个毒值下画一颗**屏内**的探针 ⇒ 必须被裁掉（证明毒值真的带电、下面四条不是空转）
                var a353Ctl = win.DrawRect(a353Host, CardArt.Solid(), new PxRect(400f, 300f, 500f, 400f),
                                           "A353Ctl", CampaignTab.QTabBg, Color.white);
                CheckTrue(a353Ctl == null,
                          "（A353 控制组）毒框 (2500,1200→2600,1300) 下画一颗**屏内**的 ⇒ `MenuDraw.Rect` 判"
                        + "「整块在视口外」**返 null**（它不 null ⇒ 毒值没生效、下面四条全是空转）");
                a353Tab.Setup();                       // 这一趟的四件落在 `a353Host` 之下
                win.Clip = keepClipB; win.ClipPad = keepPadB; win.ClipSoftness = keepSoftB;  // 收尾还原（本夹具不考裁切）
                // 实验组：四件都必须**建出来**（改前：毒框把它们整块排除 ⇒ `_win.Rect` / `_win.Text` 直接返 null）
                var a353Bg = FindPath(a353Host, "Campaign Background/Background Image");
                CheckTrue(a353Bg != null && a353Bg.GetComponentInChildren<ImageQuad>(true) != null,
                          "★ A353①：`Campaign Background/Background Image` 照建"
                        + " —— **改坏法**：删掉 `Shell/CampaignTab.cs` 第①处那句 `ClearClip()` ⇒ 毒框"
                        + "(2500,1200→2600,1300) 与它 (330.69,−45.18→1920,1544.48) 不相交 ⇒ 整块不建");
                var a353SelBg = FindPath(a353Host, "Campaign Army Selector/Background");
                CheckTrue(a353SelBg != null && a353SelBg.GetComponentInChildren<ImageQuad>(true) != null,
                          "★ A353②：`Campaign Army Selector/Background` 照建（删掉第②处那句 `ClearClip()` ⇒ 红）");
                var a353Title = FindPath(a353Host, "Campaign Header/Title");
                CheckTrue(a353Title != null && a353Title.GetComponentInChildren<Label>(true) != null,
                          "★ A353③：`Campaign Header/Title` 那段字照建 —— `_win.Text` 也吃 `RenderClip`："
                        + "`Clip` 非空 ⇒ `MenuDraw.Visible` 判不可见 ⇒ **整条返 null**、静默少一段字");
                var a353PanBg = FindPath(a353Host, "Premium Panel/Background");
                CheckTrue(a353PanBg != null && a353PanBg.GetComponentInChildren<ImageQuad>(true) != null,
                          "★ A353④：`Premium Panel/Background` 照建 —— 它在 `BuildTrack()` **之后**建，"
                        + "观测的是「`BuildTrack` 有没有把 `Clip` 还原」的另一半");
                // 量化那一档（两条；期望值 = **原版字面量**，⛔ 不读 `CampaignTab` 的私有矩形字段）
                CheckAt(FindChild(a353Host, "Campaign Header"), 330.69f, 790.92f, 60.94f, 225.94f,
                        "A353③ …而且矩形 = 原版 `Campaign Header`");
                CheckAt(FindChild(a353Host, "Premium Panel"), 344.29f, 720.35f, 867.01f, 1080.00f,
                        "A353④ …而且矩形 = 原版 `Premium Panel`");
                // ⚠️ **收尾销毁是必须的**：`Setup()` 会**再登记一个滚动区**（`PointerLayer.RegisterScroll(_armyScroll)`，
                //    `Owner = a353Host`）⇒ 先按宿主撤掉、再销毁，免得多出一条指向死节点的滚动条目。
                PointerLayer.UnregisterOwnedBy(a353Host.gameObject);
                Object.DestroyImmediate(a353Host.gameObject);
            }
        }

        Debug.Log(P + win.Dump());
        Debug.Log(P + $"=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===");
        // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是失败表的【重列】（`Check` 里已经逐条打过）
        //   ⇒ 行首标记 = `失败重列：`，⛔ 不再是 `✗`（原来是 `✗` 时日志里 `✗` 行数 = 失败数 ×2）。
        //   同族四处一起改：`ShellScene` / `CollectionScene` / `ShopScene`（同一句形状）。
        if (_sink.Fail > 0) foreach (var f in _sink.Failures) Debug.LogError(P + "   失败重列：" + f);
        EditorApplication.Exit(_sink.Fail > 0 ? 1 : 0);
    }

    // `Daily Missions Holder` 的 x 边界（🔴 **2026-10-06（A106）改成布局位·2 张卡那一帧**：**1190.44..1799.94**）。
    // 它是 `Daily Missions` 的**拉伸子件**（`a=(0,0)-(1,0.5) sizeDelta.x = 0.0001`）⇒ 宽度跟着父件走；
    // 父件从模板位（1271.30..1810.49）被 `Normal Missions` 的 HLG 挪到了布局位 ⇒ 这里必须跟着改。
    // 出处 = `Shell/MissionsTab.cs` 的 A106 那一段（手算过程 + **「卡片数 → 参数」对照表**：
    // 1 张 = 作者态 / **2 张 = 我们这一页画的那一帧（本文件用这一列）** / 3~4 张 = 没算）。
    //
    // 🔴 **2026-10-06（A124）：这两个数是【设计空间】的布局值，不是屏幕上的值** —— `Daily Missions` 的 RT
    // 带 `m_LocalScale = (1.15,1.15)`（`资料/日常_原版规格.md:194`）⇒ **整棵子树绕它的 pivot `(0,1)` 左上角
    // `(1190.44, 95.85)` 缩放 1.15**。这两个数只用来**在设计空间里推三行**（`MissionsTab.RowRect` 的算式
    // 也是设计空间的）⇒ 期望值要再走一次 `DmView(...)` 才是**画在屏幕上的矩形**。
    static float RowRectHolderX1() { return 1190.44f; }
    static float RowRectHolderX2() { return 1799.94f; }

    /// <summary>🔴 **A124（2026-10-06）`Daily Missions` 子树的「设计空间矩形 → 视觉矩形」**
    /// —— 原版那个节点的 `m_LocalScale = (1.15, 1.15)`（`资料/日常_原版规格.md:194` 实读：
    /// `p=(0,1) scl=(1.15,1.15)`），pivot `(0,1)` = **左上角** ⇒ 绕 `(1190.44, 95.85)` 缩放。
    ///
    /// <para>**为什么自检要自己算一遍**：期望值与被测实现**各算一遍**才叫判据（本文件既有的
    /// `UguiRect.Child` 就是这个用法）—— ⛔ 别改成调 `MissionsTab.ScaleAbout`（那是拿实现证明实现）。</para>
    ///
    /// <para>**两条承重真值**（原版 prefab 值 × 原版 `localScale`，本文件直接钉死字面量）：
    /// 行宽 `609.50 × 1.15 = **700.93**` · 行高 `150 × 1.15 = **172.5**`。
    /// ⚠️ **不是** `965.186 / 1109.96` —— 那两个是【1 张卡·作者态】那一帧的（对照表在
    /// `Shell/MissionsTab.cs` 的 A106 段）；我们这一页画的是**【2 张卡】**那一帧（`DM_Sz.x = 609.50`）。</para>
    ///
    /// <para>自证（缩放中心取对了的判据）：`1190.44 + 609.50 × 1.15 = **1891.37**` = `Normal Missions` 的右边缘
    /// —— 布局刚好填满容器，中心取错（比如取成矩形中心）这条恒等式立刻不成立。</para></summary>
    static PxRect DmView(PxRect r)
    {
        const float ox = 1190.44f, oy = 95.85f, s = 1.15f;
        return new PxRect(ox + (r.x1 - ox) * s, oy + (r.y1 - oy) * s,
                          ox + (r.x2 - ox) * s, oy + (r.y2 - oy) * s);
    }

    /// <summary>一个节点里那段字**现在实际生效**的字号（画布 px）—— `Label.FontPxNow`
    /// （= 把 TMP 的 `fontSize` 按字形比例折回来的**渲染真值**，⛔ **不是**我们传进去的那个数）。
    /// 判据用法与 `Editor/BattleScene.cs` 的 `Run` 里那条「别拿常量自证」断言 同一条（那里写着「**别拿常量自证**：这里比的是 TMP 渲出来的实际字号」）。
    /// ⚠️ **为什么要它**：原版 `localScale` 缩的是**整棵子树**，TMP 的文字网格也在里头，而 prefab 里的
    /// `m_fontSize` 是**未缩放的原值** ⇒ 只缩框不缩字号 = 字比框小一圈，**而量矩形的断言一条都抓不到**。</summary>
    static float FontPxOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.FontPxNow : float.NaN;
    }

    /// <summary>🆕 **A143（2026-10-06）：`Special Missions` 卡内**一段字的字号** —— 必须 = **设计空间那个字号 × 1.15**。
    ///
    /// <para>🔴 **原版判据 = prefab 的 `localScale`**：`Special Missions` 那个节点的 RT 带
    /// `m_LocalScale = (1.15, 1.15)`，而 Unity 的 `localScale` 缩的是**整棵子树** ——
    /// **TMP 的文字网格也在里头**，prefab 里那些 `m_fontSize` 是**未缩放的原值**
    /// ⇒ 卡内每一段字都要 ×1.15（只缩框不缩字号 = 「框对了、字小一圈」，且**静默**：量矩形的断言一条都抓不到）。
    /// 我们这一侧唯一的落地处 = `Shell/MissionsTab.cs` 的 `FS(fontPx) { return fontPx * _s; }`
    /// （`Build()` 里 `_s = SM_Scale` … `_s = 1f` 夹住 `BuildLoginCard` + `BuildSkullsCard`）。</para>
    ///
    /// <para>⛔ **`wantPx` 一律写「设计字号 × 1.15」的字面量** —— **不调** `MissionsTab.FS`、**不读**
    /// `MissionsTab.SM_Scale`（拿实现证明实现 = 自证：把 `SM_Scale` 改回 1.0 也照样绿）。
    /// 量的也不是「我们传进去的那个数」，是 `Label.FontPxNow`（TMP 渲出来的真值，同 <see cref="FontPxOf"/>）。</para>
    ///
    /// <para>⚠️ **只用在【没有自适应】的件上**（`Txt` 不传 `autoMinPx` 的、以及 `Txt1`）：那两路字号是**定死的**
    /// ⇒ 字面量能钉死。开了自适应的件（卡头 `name` / 时钟行 `Timer`）`_tmp.fontSize` 是**二分出来的收敛值**
    /// （文字装不下就比上界小）⇒ 那一路走 <see cref="CheckFontWindow"/>，别拿它比字面量。</para></summary>
    static void CheckFontPx(Transform card, string nodeName, float wantPx, string what)
    {
        var t = card != null ? FindChild(card, nodeName) : null;
        if (t == null || float.IsNaN(FontPxOf(t)))
        {
            CheckTrue(false, $"{what}：**这条没查成** —— `{nodeName}` 不在 "
                             + $"`{(card != null ? card.name : "<卡节点不在>")}` 里（没建出来 / 太靠边被裁掉都算）");
            return;
        }
        CheckNear(FontPxOf(t), wantPx, 0.5f, what);
    }

    /// <summary>🆕 **A143**：`Txt` 那条**带自适应**的路（卡头 `title`、时钟行 `Timer`）—— 钉的是 TMP 的
    /// **`fontSizeMin` / `fontSizeMax` 窗口**（`Label.FontSizeMin/Max` 是 TMP 里的真值，
    /// 经工程唯一那份 `Label.FontSizeToPx` 折成画布 px）。
    ///
    /// <para>⚠️ **为什么这一路不能断 `FontPxNow`**：开了自适应之后 `_tmp.fontSize` 是**二分出来的收敛值**，
    /// 文字装不下就比上界小 ⇒ 拿它比字面量会**时红时绿**；而窗口是**定死的**。它同时正好覆盖 `FS()` 的
    /// **另一半**：`Txt` 把 `FS(fontPx)` 与 `FS(autoMinPx)` **各乘一次**（只乘上限会把自适应区间压窄、
    /// 收敛结果与原版不同 —— 见 `Shell/MissionsTab.cs` 的 `FS` summary）。</para>
    ///
    /// <para>⛔ 期望值同样是「设计空间的数 × 1.15」的字面量。⚠️ **这条钉的是「窗口上下界确实乘了 1.15」**，
    /// ⛔ **不是**「上界对上原版 prefab 的 `m_fontSizeMax` 字段」—— 我们 `Txt` 把**设计字号同时当上界**，
    /// 原版 prefab 里这两个字段本身**没核**（已记在 `资料/普查产出_1006/A143_SM字号断言.md` 的「没查清」）。</para></summary>
    static void CheckFontWindow(Transform card, string nodeName, float wantMinPx, float wantMaxPx, string what)
    {
        var t = card != null ? FindChild(card, nodeName) : null;
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null)
        {
            CheckTrue(false, $"{what}：**这条没查成** —— `{nodeName}` 没建出来 / 上面没有 `Label`");
            return;
        }
        CheckNear(Label.FontSizeToPx(lb.FontSizeMin), wantMinPx, 0.5f, what + " 自适应**下界**");
        CheckNear(Label.FontSizeToPx(lb.FontSizeMax), wantMaxPx, 0.5f, what + " 自适应**上界**");
    }

    static void CheckArt(Transform t, string want, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(q != null && q.Texture != null && q.Texture.name == want, $"{what} = `{want}`");
    }

    /// <summary>按**全等名字**数节点。
    /// 🔴 **2026-10-12（A361）补了 null 挡**：原来首句直接 `root.GetComponentsInChildren` ⇒ 传 `null` 会
    /// **抛 `NullReferenceException`**，而本文件的自检是**一条链跑到底**（`Check` 是记账式的、**不抛不早退**）
    /// ⇒ 那一条 NRE 会**把它后面所有断言一起吞掉**（自检提前中止，日志里只看得到一条异常）。
    /// 同族的 `CountByPrefix` **两份都带挡**（本文件 `:9830` · `Editor/ShopScene.cs` 的 `CountByPrefix`）⇒ 这是
    /// 「同一条规则两种写法」。🔴 **2026-10-14（A615 · W-E1 顺手发现②）就地订正（铁律 5）**：
    /// 原来这里接着写「⚠️ `Editor/MainMenuScene.cs` 那份**同族副本不在本件白名单**（另有人独占）⇒ 那半
    /// **待接线**（写进报告）」—— **那半已经接完了**（A361 收口）：`Editor/MainMenuScene.cs` 的 `CountByName` 那份
    /// `CountByName` 首句就是 `if (root == null) return 0;`（2026-10-14 现读，与本文这一份逐字同形）
    /// ⇒ **待接线 → 已接线**，三份口径现已一致。
    /// ⚠️ 同一句里「本文件 `:7220`」那个行号也是**漂过的**（现读 = `:9830`；行号一律按锚点找，⛔ 别信旧数）。
    /// <para>**改坏法**：把这一句删掉 ⇒ 下面那句「传 `null` 得 0」立刻红（而且会**带崩**它后面那几条）。</para></summary>
    static int CountByName(Transform root, string name)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    /// <summary>按**名字前缀**数（`CountByName` 是**全等**比较 —— 拿它数 `Item_` 恒得 0）。</summary>
    static int CountByPrefix(Transform root, string prefix)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix)) n++;
        return n;
    }

    static int CountVisible(Transform root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name && t.gameObject.activeInHierarchy) n++;
        return n;
    }

    /// <summary>名字**以 `prefix` 打头**且**在层级里可见**的节点数（左栏 `RewardsTabButton_0..4` 这种；
    /// ⚠️ 键表 5 项、**可见 4 个** —— 母版那一格关着，见 §二 那一段）。</summary>
    static int CountVisibleChildren(Transform root, string prefix)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix, System.StringComparison.Ordinal) && t.gameObject.activeInHierarchy) n++;
        return n;
    }
}
