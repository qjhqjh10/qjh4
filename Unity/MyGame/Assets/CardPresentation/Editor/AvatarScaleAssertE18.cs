// AvatarScaleAssertE18.cs — 执行代理 `E18`（2026-10-24）的断言宿主：**头像三层「绕中心放大」**
//
// 本文件**只放断言**（⛔ 不改任何产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主）。
// 调用方式（主对话在 `MainMenuScene.Run` 收尾处加两行 —— 那一刻档案窗与它的六页都建好了）：
//
//     AvatarScaleAssertE18.Run();
//     pass += AvatarScaleAssertE18.Pass; fail += AvatarScaleAssertE18.Fail;
//
// 或者单独跑：`-executeMethod AvatarScaleAssertE18.Run`（要求场景里已经有建好的档案窗）。
//
// ==================================================================
//  用户 2026-10-24 的原话：「账户—头像那里，头像框里面的头像应该更大（甚至超出头像框，
//  营造立体感），现在是缩小在里面」。
//
//  原版判据（**只读代理 S5 逐颗读 prefab 的 `RectTransform.m_LocalScale`** →
//  `资料/普查产出_1024b/S5_头像组装.md` §①，12/12 个 `AvatarDisplay` 实例同值）：
//    · 头像 `Image` × **2.0** · 框 `Border`(= `avatarHolder`) × **1.25** · 高亮 `Highlight` × **1.9**
//    · 三者 pivot 都是 (0.5,0.5) ⇒ **绕各自布局矩形的中心放大**（布局矩形一个像素都不动）
//    · 画序（底→顶）= `Highlight → Border → Image`（**头像画在框之上**）· 无遮罩 · `Simple` + `preserveAspect`
//
// ==================================================================
//  本文件断四条（每条都写清「🧨 怎么改会红」）：
//   ① 格子：头像**可见内容宽** / 框**实绘宽** = **1.006 ± 0.02**（🧨 不放大 ⇒ 0.628 ⇒ 红）
//   ② 格子：头像实绘框的**顶沿比框顶高出 ≥ 30 px**（原版 70.45；🧨 不放大 ⇒ 只有 16.96 ⇒ 红）
//   ③ 画序：`Image` 的渲染队列**大于** `Border`（🧨 改反 ⇒ 头像被那面「不透明黑内胆」的框盖死 ⇒ 红）
//   ④ 左侧大图预览（`Avatar Menu Item`）：同一组比值 / 顶沿 / 画序
//   ⑤（附加，同族同病）`ProfileTab` 与 `RankedTab` 的 `Avatar Item Small` 那一格：同一组
//
//  🔴 **期望值从哪来**（⛔ 不许拿我们自己的常量证明我们自己的实现）：
//    · 放大倍数 2.0 / 1.25 / 1.9 = prefab 的 `m_LocalScale`（S5 §①，第一权威）；
//    · **实绘框**（`preserveAspect` 内接之后）= 布局矩形 × 倍数 再按贴图比例内接 —— 这一层算式
//      与 uGUI `Image.GetDrawingDimensions` 同一条（`ImageQuad.FitHeight` 的注释里也写着），
//      所以每条期望值都能从「原版布局矩形 × 原版倍数 × 贴图比例」手算出来（见下）；
//    · **可见内容宽占比 288/512** = 头像 sprite 的自带透明边：512² 画布、alpha bbox 主流款 288×384
//      （S5 §① 抽了 7 张；本代理另外实测 11 张里 6 张正好 288 宽）⇒ 这是**原版图**的实测值，不是我们的常量。
//
//  ⚠️ **为什么①要乘 288/512 而不是直接比实绘框**：`Image` 的实绘框 285.26 是**画布**大小（含透明边），
//     框的实绘宽 159.59 是它自己的可见宽 ⇒ 不乘那一下比出来是 1.79（不是用户看到的比例）。
//     用户看到的「跟框一样宽」= **可见内容** vs 框 = 1.006。
//
//  ⚠️ **量法**：`MenuDraw.QuadRectPx(ImageQuad)`（= `WorldW/WorldH × 108` + 设计帧位置，**真测量**）
//     —— ⛔ 不读 `PxRect` 常量、不读 `RectTransform.sizeDelta`（本工程的图块**没有** `RectTransform`）。
//     ⚠️ 视口外的部分会被滚动区那层裁掉 ⇒ 量到的是**裁后**框：所以格子一律取**整块都在视口内**的那一颗。
// ==================================================================
using CardPresentation;
using UnityEngine;

public static class AvatarScaleAssertE18
{
    const string P = "[E18] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    // ---------------- 判据常量（全部来自 S5 §① 的原版读数，⛔ 不是我们的实现） ----------------
    /// <summary>头像 sprite 512² 画布里的**可见内容宽占比**（主流款 alpha bbox 288 宽）。</summary>
    const float VisibleWOfCanvas = 288f / 512f;
    /// <summary>框贴图 `Player_Profile_Border` = 256×286（`preserveAspect` 时的高度受限比）。</summary>
    const float BorderAspect = 256f / 286f;
    /// <summary>「可见内容宽 / 框实绘宽」的原版值（285.26×288/512 ÷ 159.59 = 1.0055）。</summary>
    const float WantRatio = 1.006f;
    /// <summary>头像实绘高 / 框实绘高 = 2.0 / 1.25 = **1.6**（与立绘是哪张**无关** ⇒ 最稳的一条）。</summary>
    const float WantHOverH = 1.6f;

    /// <summary>`Avatar Tab` 的滚动视口（原版 `Item Display Panel/Scroll Rect` = 654.16..1680.12 × 210.69..855.46；
    /// 同一组数已在 `Editor/MainMenuScene.cs` 的 `CheckAtWorld(scr2, …)` 钉过）—— 用来判「这一格有没有被裁」。</summary>
    const float VpL = 654.16f, VpT = 210.69f, VpR = 1680.12f, VpB = 855.46f;

    public static void Run()
    {
        Pass = 0; Fail = 0;

        var av = FindBuilt<AvatarTab>(t => MenuCheck.FindChild(t, "Item Drawer") != null);
        if (!Ok(av != null, "前提：场景里有一个**建好的** `AvatarTab`（`Item Drawer` 在）"
                           + " —— 先跑 `MainMenuScene.Run`，或 `-executeMethod MainMenuScene.BuildAndSaveScene`"))
        {
            Debug.Log(P + "没有宿主 ⇒ 后面的条目全部没跑（⛔ 不许静默当通过）");
            return;
        }

        CheckGrid(av);
        CheckBigPreview(av);

        // ⑤ 同族同病：`ProfileTab` / `RankedTab` 的 `Avatar Item Small`（同一颗 prefab、同一套倍数）
        CheckSmallCell(FindBuilt<ProfileTab>(t => MenuCheck.FindChild(t, "Player Info") != null),
                       "ProfileTab", "Player Info");
        CheckSmallCell(FindBuilt<RankedTab>(t => MenuCheck.FindChild(t, "Profile Player Info") != null),
                       "RankedTab", "Profile Player Info");

        Debug.Log(P + $"=== E18 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  ① ② ③ · 网格格子（`Avatar Item Small_k`）
    // ==================================================================
    static void CheckGrid(AvatarTab av)
    {
        var drawer = MenuCheck.FindChild(av.transform, "Item Drawer");
        if (!Ok(drawer != null, "前提：`Avatar Tab/Item Display Panel/Scroll Rect/Item Drawer` 在")) return;
        if (!Ok(drawer.childCount >= 7, "前提：网格至少建出 7 格（实得 " + drawer.childCount + "）；"
                                      + "`Avatar Item Small_6` = 第 2 行第 2 列，**四边都在视口内**（⛔ 别用 0 号格：它被裁）"))
            return;

        // 🔴 **取 `_6` 那一格**（第 2 行第 2 列）：0 号格的头像实绘框（x −52.63..、y −74.02..）
        //   **三面都伸出视口**，`WorldW/H` 拿到的是**裁后**框 ⇒ 比出来的比例会假红。
        //   `_6` 的实绘框 = x 819.5..1104.8 · y 406.7..691.95，四边都在视口内（上面那两个常量）。
        var cell = drawer.Find("Avatar Item Small_6");
        if (!Ok(cell != null, "前提：`Avatar Item Small_6` 那一格建出来了")) return;
        var ic = MenuCheck.FindChild(cell, "Image Container");
        var img = ic != null ? MenuCheck.FindChild(ic, "Image") : null;
        var bd = ic != null ? MenuCheck.FindChild(ic, "Border") : null;
        if (!Ok(img != null && bd != null, "前提：格子里 `Image Container/Image` 与 `/Border` 都在")) return;

        PxRect ir, br;
        if (!Ok(Box(img, out ir), "量得到头像那块的实绘框（`ImageQuad.WorldW/H`）")) return;
        if (!Ok(Box(bd, out br), "量得到框那块的实绘框（`ImageQuad.WorldW/H`）")) return;

        Ok(ir.x1 >= VpL - 0.5f && ir.x2 <= VpR + 0.5f && ir.y1 >= VpT - 0.5f && ir.y2 <= VpB + 0.5f,
           $"（前提）`Avatar Item Small_6` 的头像**整块在视口内**（量法：{Fmt(ir)} vs 视口 "
         + $"x {VpL}..{VpR} · y {VpT}..{VpB}）—— 🧨 换了格子索引 / 视口挪了 ⇒ 这里先红，"
         + "别拿裁后的框去比比例");

        // ---- ① 可见内容宽 / 框实绘宽 ----
        float contentW = ir.H * VisibleWOfCanvas;      // 头像贴图是 512²（PA ⇒ 实绘高即画布边长）
        float got = br.W > 1e-3f ? contentW / br.W : 0f;
        Ok(Mathf.Abs(got - WantRatio) <= 0.02f,
           $"★ ① 格子：头像**可见内容宽 / 框实绘宽** = **{WantRatio} ± 0.02**"
         + $"（原版 285.26×288/512 ÷ 159.59 = 1.0055）—— 实测 {contentW:F2} / {br.W:F2} = **{got:F4}**"
         + "｜🧨 把头像那层改回**不放大**（`AvatarTab.ScaleImage` 去掉）⇒ 80.23 / 127.67 = **0.628** ⇒ 红"
         + "（那正是用户说的「缩小在里面」）");

        // ---- ①b 几何自洽（与立绘无关，最稳）----
        float hh = br.H > 1e-3f ? ir.H / br.H : 0f;
        Ok(Mathf.Abs(hh - WantHOverH) <= 0.01f,
           $"★ ①b 格子：头像实绘高 / 框实绘高 = **2.0 ÷ 1.25 = {WantHOverH}** —— 实测 {ir.H:F2} / {br.H:F2} = **{hh:F4}**"
         + "（这一条**不依赖**是哪张立绘：`preserveAspect` 之后两者都由同一个布局高决定）"
         + "｜🧨 两层倍数改成同一个数（哪怕都改成 2.0）⇒ 红");

        // ---- ①c 原版绝对值 ----
        Ok(Mathf.Abs(ir.H - 285.26f) <= 1.5f && Mathf.Abs(br.W - 159.59f) <= 1.5f,
           $"★ ①c 格子：实绘绝对值 = 头像高 **285.26**（= 布局 142.63 × 2.0）· 框宽 **159.59**"
         + $"（= 布局高 142.63 × 1.25 × 256/286）—— 实测 {ir.H:F2} / {br.W:F2}"
         + "｜🧨 少乘一个倍数、或把倍数乘到了 `Image Container` 上 ⇒ 红");

        // ---- ② 头顶戳出框顶 ----
        float over = br.y1 - ir.y1;    // y 向下 ⇒ 正值 = 头像顶沿在框顶**之上**
        Ok(over >= 30f && Mathf.Abs(over - 70.45f) <= 2f,
           $"★ ② 格子：头像实绘框的**顶沿比框顶高出 ≥ 30 px**（原版 = −74.02 − (−3.57) = **70.45**）"
         + $" —— 实测 **{over:F2} px**｜🧨 不放大 ⇒ 只有 16.96 px ⇒ 红（那时头被框包住 = 用户说的「缩在里面」）");

        // ---- ③ 画序 ----
        var imgQ = img.GetComponentInChildren<ImageQuad>(true);
        var bdQ = bd.GetComponentInChildren<ImageQuad>(true);
        Ok(imgQ != null && bdQ != null && imgQ.RenderQueue > bdQ.RenderQueue,
           $"★ ③ 格子画序：`Image` 的渲染队列**严格大于** `Border`（原版兄弟序 `Highlight → Border → Image`；"
         + $"理由是那面框的中心是**不透明黑** RGBA(0,0,0,255)）—— 实测 {Q(imgQ)} > {Q(bdQ)}"
         + "｜🧨 改回 `Image < Border`（2026-10-18 前就是）⇒ 每格立绘被黑框盖死 ⇒ 红");

        // ---- 高亮（选中光晕）×1.9：只钉「比框大」，不钉绝对值（原版两个调用点里它恒关/常亮各半）----
        var hl = MenuCheck.FindChild(ic, "Highlight");
        if (hl != null)
        {
            var hlQ = hl.GetComponentInChildren<ImageQuad>(true);
            if (hlQ != null)
            {
                var hr = MenuDraw.QuadRectPx(hlQ);
                Ok(hr.W >= br.W * 1.4f && hr.W <= br.W * 1.8f,
                   $"（附加）格子 `Highlight` 实绘宽 = 框宽的 **1.4~1.8 倍**（原版 348.34×1.9 后 PA 成 271² ÷ 框 159.59"
                 + $" ≈ 1.70）—— 实测 {hr.W:F2} / {br.W:F2} = {hr.W / Mathf.Max(1e-3f, br.W):F3}"
                 + "｜🧨 高亮不放大 ⇒ 约 1.03 ⇒ 红");
            }
        }
    }

    // ==================================================================
    //  ④ · 左侧大图预览（`Avatar Menu Item/Image Container`）
    // ==================================================================
    static void CheckBigPreview(AvatarTab av)
    {
        var sel = MenuCheck.FindChild(av.transform, "Selected Item Panel");
        var mi = sel != null ? MenuCheck.FindChild(sel, "Avatar Menu Item") : null;
        var ic = mi != null ? MenuCheck.FindChild(mi, "Image Container") : null;
        var img = ic != null ? MenuCheck.FindChild(ic, "Image") : null;
        var bd = ic != null ? MenuCheck.FindChild(ic, "Border") : null;
        if (!Ok(ic != null && img != null && bd != null,
                "前提：`Avatar Tab/Selected Item Panel/Avatar Menu Item/Image Container` 的 `Image` 与 `Border` 都在"))
            return;

        PxRect ir, br;
        if (!Ok(Box(img, out ir), "量得到大图头像那块的实绘框（`ImageQuad.WorldW/H`）")) return;
        if (!Ok(Box(bd, out br), "量得到大图框那块的实绘框（`ImageQuad.WorldW/H`）")) return;

        // 原版（S5 §「左侧大图预览」）：容器 267.08×213.58
        //   · Image 布局 267.08×213.58 ×2.0 ⇒ PA(512²) 后 **427.16²** ⇒ 可见内容宽 427.16×288/512 = 240.28
        //   · Border 布局 213.58 高 ×1.25 ⇒ 266.975 高、宽 266.975×256/286 = 238.94
        float contentW = ir.H * VisibleWOfCanvas;
        float got = br.W > 1e-3f ? contentW / br.W : 0f;
        Ok(Mathf.Abs(got - WantRatio) <= 0.02f,
           $"★ ④-a 大图预览：头像**可见内容宽 / 框实绘宽** = **{WantRatio} ± 0.02**"
         + $"（原版 427.16×288/512 = 240.28 ÷ 238.94 = 1.0056）—— 实测 {contentW:F2} / {br.W:F2} = **{got:F4}**"
         + "｜🧨 三层不放大 ⇒ 213.58×288/512 ÷ (213.58×256/286) = **0.628** ⇒ 红");

        float hh = br.H > 1e-3f ? ir.H / br.H : 0f;
        Ok(Mathf.Abs(hh - WantHOverH) <= 0.01f,
           $"★ ④-b 大图预览：头像实绘高 / 框实绘高 = **{WantHOverH}** —— 实测 {ir.H:F2} / {br.H:F2} = **{hh:F4}**"
         + "｜🧨 只放大头像不放大框（或反过来）⇒ 红");

        Ok(Mathf.Abs(ir.H - 427.16f) <= 2f && Mathf.Abs(br.W - 238.94f) <= 2f,
           $"★ ④-c 大图预览实绘绝对值 = 头像高 **427.16** · 框宽 **238.94** —— 实测 {ir.H:F2} / {br.W:F2}"
         + "（同一条判据在 `Editor/MainMenuScene.cs` 那一节只钉了**布局框中心**，尺寸这条原来没人管）"
         + "｜🧨 不放大 / 只放大一层 ⇒ 红");

        float over = br.y1 - ir.y1;
        Ok(over >= 30f && Mathf.Abs(over - 104.15f) <= 2f,
           $"★ ④-d 大图预览：头像顶沿比框顶高出 **≥ 30 px**（原版 = 250.66 vs 354.81 ⇒ **104.15**）"
         + $" —— 实测 **{over:F2} px**｜🧨 不放大 ⇒ 24.06 ⇒ 红");

        var imgQ = img.GetComponentInChildren<ImageQuad>(true);
        var bdQ = bd.GetComponentInChildren<ImageQuad>(true);
        Ok(imgQ != null && bdQ != null && imgQ.RenderQueue > bdQ.RenderQueue,
           $"★ ④-e 大图画序：`Image` 的渲染队列**严格大于** `Border` —— 实测 {Q(imgQ)} > {Q(bdQ)}"
         + "（判据 = 原版兄弟序 `Highlight → Border → Image`；2026-10-18 前这里就是反的、整块黑）"
         + "｜🧨 改回 `Highlight → Image → Border`（`Q 4 / 6 / 5` 那一版）⇒ 红");
    }

    // ==================================================================
    //  ⑤ · `ProfileTab` / `RankedTab` 的 `Avatar Item Small`（小号那一格）
    // ==================================================================
    static void CheckSmallCell(ProfilePage page, string who, string infoName)
    {
        if (!Ok(page != null, $"前提：场景里有一个**建好的** `{who}`（档案窗六页在开窗时一次建全）")) return;
        var info = MenuCheck.FindChild(page.transform, infoName);
        var av = info != null ? MenuCheck.FindChild(info, "Avatar Item Small") : null;
        var ic = av != null ? MenuCheck.FindChild(av, "Image Container") : null;
        var img = ic != null ? MenuCheck.FindChild(ic, "Image") : null;
        var bd = ic != null ? MenuCheck.FindChild(ic, "Border") : null;
        if (!Ok(ic != null && img != null && bd != null,
                $"{who}：`{infoName}/Avatar Item Small/Image Container` 的 `Image` 与 `Border` 都在")) return;

        PxRect ir, br;
        if (!Ok(Box(img, out ir), $"{who}：量得到头像那块的实绘框")) return;
        if (!Ok(Box(bd, out br), $"{who}：量得到框那块的实绘框")) return;

        // 原版（S5 §⑤「同族同病」+ `Editor/BattleScene.cs:24532` 对同一颗 `Avatar Item Small` 的独立实读）：
        //   容器 159.16×115.02 ⇒ Image ×2.0 ⇒ PA 后 **230.04²** · Border 115.02×1.25 = 143.775 高、宽 128.69
        float contentW = ir.H * VisibleWOfCanvas;
        float got = br.W > 1e-3f ? contentW / br.W : 0f;
        Ok(Mathf.Abs(got - WantRatio) <= 0.02f,
           $"★ ⑤-a {who}：可见内容宽 / 框实绘宽 = **{WantRatio} ± 0.02**（原版 230.04×288/512 = 129.40 ÷ 128.69）"
         + $" —— 实测 {contentW:F2} / {br.W:F2} = **{got:F4}**｜🧨 不放大 ⇒ 0.628 ⇒ 红");

        float hh = br.H > 1e-3f ? ir.H / br.H : 0f;
        Ok(Mathf.Abs(hh - WantHOverH) <= 0.01f,
           $"★ ⑤-b {who}：头像实绘高 / 框实绘高 = **{WantHOverH}** —— 实测 {ir.H:F2} / {br.H:F2} = **{hh:F4}**"
         + "｜🧨 两层倍数不成 2.0/1.25 ⇒ 红");

        float over = br.y1 - ir.y1;
        Ok(over >= 30f && Mathf.Abs(over - 57.33f) <= 2f,
           $"★ ⑤-c {who}：头像顶沿比框顶高出 **≥ 30 px**（原版 107.95 vs 165.28 ⇒ **57.33**）—— 实测 **{over:F2}**"
         + "｜🧨 不放大 ⇒ 14.20 ⇒ 红");

        var imgQ = img.GetComponentInChildren<ImageQuad>(true);
        var bdQ = bd.GetComponentInChildren<ImageQuad>(true);
        Ok(imgQ != null && bdQ != null && imgQ.RenderQueue > bdQ.RenderQueue,
           $"★ ⑤-d {who}：画序 `Image` 在 `Border` **之后** —— 实测 {Q(imgQ)} > {Q(bdQ)}"
         + "｜🧨 改反 ⇒ 立绘被不透明黑的框盖死 ⇒ 红");
    }

    // ==================================================================
    //  小工具
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}");
        return false;
    }

    static string Fmt(PxRect r) { return $"({r.x1:F2},{r.y1:F2})-({r.x2:F2},{r.y2:F2}) {r.W:F2}×{r.H:F2}"; }
    static string Q(ImageQuad q) { return q != null ? q.RenderQueue.ToString() : "<无 ImageQuad>"; }

    /// <summary>一块图**画出来**的矩形（设计帧 px · 左上原点 · y 向下）—— `MenuDraw.QuadRectPx` 是唯一实现。</summary>
    static bool Box(Transform t, out PxRect r)
    {
        r = new PxRect();
        var q = t != null ? t.GetComponentInChildren<ImageQuad>(true) : null;
        if (q == null) return false;
        r = MenuDraw.QuadRectPx(q);
        return true;
    }

    /// <summary>在场景里找一个**建好的** T 实例（含 inactive —— 档案窗只有当前页是激活的）。
    /// `built` = 判定「这一份真的建过」的依据（⛔ 别用 `FindObjectOfType`：它只看激活对象）。</summary>
    static T FindBuilt<T>(System.Func<Transform, bool> built) where T : Component
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;
            var go = c.gameObject;
            if (go == null || !go.scene.IsValid()) continue;      // 跳过资产 / prefab 模板
            if (built(c.transform)) return c;
        }
        // 退路：至少有一个活的实例（免得「页面还没建」被误报成「实例不在」）
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c != null && c.gameObject != null && c.gameObject.scene.IsValid()) return c;
        }
        return null;
    }
}
