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

    /// <summary>🆕 2026-10-18（第四会话）：断言计数器 + 输出口径**收口到共用件 `Editor/MenuCheck.cs`**
    /// （唯一实现处；本文件只剩同名的一行转发 ⇒ 5,534 个调用点一个字没动）。
    /// 🔴 **逐宿主一份 `CheckSink`**（⛔ 不是全局 static）—— 「拿别处的 `Check` 去断，失败会
    /// **记进别人的合计**里 ⇒ 静默」，判据见 `Editor/RewardWindowFixture.cs:12-14`。</summary>
    static readonly CheckSink _sink = new CheckSink(P)
    {
        Near = MenuNearStyle.Compact2,
        BlankNote = MenuBlankNote.EmphPlain,     // 本文件原来那句「（**按已知情况放行**）」
    };

    static void Section(string t) => MenuCheck.Section(_sink, t);

    static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);
    static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what) => MenuCheck.HoverSwap(_sink, root, what);

    static void CheckNoMissingSwapArt(string what) => MenuCheck.NoMissingSwapArt(_sink, what);

    /// <summary>🆕 **2026-10-06（A83② —— A81 的尾巴）**：压暗层（「点窗外关窗」）命中区那条不变量。
    /// 🔴 **2026-10-07（A77⑬⑥）本文件里那份副本已删**（它就是第 5 份）—— 唯一一份在
    /// `MenuDraw.CheckShadeRule`。⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
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

    static void CheckNear(float got, float want, float tol, string msg) => MenuCheck.Near(_sink, got, want, tol, msg);

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

    /// <summary>🆕 **A491**：抓 `act` 跑的那一下里打出来的**全部**日志（**含 `Log`**）。
    /// <para>为什么另开一只网：`CaptureErrors` **只收 Error/Exception**，而「不许静默失败」那一类**出声**
    /// 走的是 `Debug.Log`（`Label.NoteDotBackendLacks` / `Label.NoteArgInvalid` / `ItemDrawer.Note` …）
    /// ⇒ 用那只网抓它**恒为空**（「必须出声」会假红）。
    /// 🔴 **2026-10-13（A596）**：`Label.SetCharSpacing` **从这个名单里去掉了** —— 它原来自己打
    /// `Debug.Log`，现在收编进 `Label.NoteDotBackendLacks`（同一次改名，见 `Battle/Label.cs` 那只口的 doc）。
    /// ⚠️ **正面（必须出声）与反面（不许出声）两条断言要共用这一只网** —— 网坏了的时候正面那条会红，
    /// 反面那条才不至于**假绿**（本工程那条系统性毛病：弱断言分不出两种状态）。</para></summary>
    static List<string> CaptureLogs(System.Action act)
    {
        var got = new List<string>();
        Application.LogCallback cb = (msg, stack, type) => { if (msg != null) got.Add(msg); };
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
    /// 一个**单张** quad 的矩形（吸收层底就是一张平图），所以直接取 `GetComponentInChildren&lt;ImageQuad>()`。
    /// <para>🔴 **2026-10-09（A1120）：子树上那颗【命中层】的 quad 也会被算进来** —— `MenuDraw.Hit` /
    /// `MakeHitQuad` 建的那颗透明 quad（tint `(0,0,0,0)`、名字恒 `"Hit"`）**是 `ImageQuad`**，
    /// 而自 `d6c4111`（`A1053`「命中区归真值」）起它**比可见面大**（关闭钮：命中 86.73×85.05 vs 脸 67.5×67.5）。
    /// ⇒ **要量「这一件长什么样」，目标就得指到【可见面那颗子件】上**（例：`…/Generic Close Button/bg`），
    /// ⛔ **别把一件的节点整个交进来**（那量到的是「脸 ∪ 命中层」）。本窗目前只有关闭钮一处踩到
    /// （两条红 → `A1120`）；**新加「量某件矩形」的断言时先问：这棵子树里有没有 `Hit`**。</para></summary>
    static bool RectOf(Transform t, out float lx, out float ty, out float rx, out float by)
    {
        // 🆕 2026-10-18（A1003）：并集算法收口到 `MenuDraw.UnionQuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
        // 🔴 两项原样保留：**激活闸 = `InHierarchy`** · **`searchInactive = true`**（`GetComponentsInChildren<>(true)`）。
        // ⚠️ 原来的 `lx = float.MaxValue …` 初值在「一块都没量到」时**原样返回**（调用方都先判 bool）；
        //    收口后那两档改成**归零** —— 与别处四个宿主的契约一致，⛔ 没有调用点读「假值」。
        // ⚠️ 本窗根 `m_LocalScale = 0.9` **是【烘进矩形】的**（`SettingsWindow.RootScale`，根节点保持 scale 1，
        //    见 `Shell/SettingsWindow.cs` 的 `Screen()`）⇒ `PosInDesignSpace` 那一除是**除 1**，读数逐位不变。
        return MenuDraw.UnionQuadRectPx(t, MenuDraw.QuadGate.InHierarchy, true,
                                        out lx, out ty, out rx, out by);
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

    /// <summary>🆕 **2026-10-18（`A1149` 第一半 · 第九会话 P7）**：量**节点自己身上**那一颗
    /// `ImageQuad` 的渲染矩形（`GetComponent`）。
    /// <para>🔴 **为什么不能复用 `CheckRectPx` / `CheckRectS`**：那两条走 `RectOf` = **子树并集**
    /// （`MenuDraw.UnionQuadRectPx`）。关窗钮归真之后圆底盘就长在钮的**根节点**上，而它底下还挂着
    /// **命中层 `Hit`**（`96.37×94.50`，**比可见面大**）⇒ 并集口径量到的是命中层、不是那张脸。
    /// 这一条只看节点自己那一颗，⛔ 不往下钻。</para>
    /// <para>`texName` 非 null 时先断贴图名（**前提·不静默**那一格）。期望值口径同
    /// <see cref="CheckRectPx"/>：**不过 `Screen()`**（字面量）。</para></summary>
    static void CheckQuadRectPx(Transform t, string texName, float x1, float y1, float x2, float y2, string what)
    {
        var q = t != null ? t.GetComponent<ImageQuad>() : null;
        if (texName != null)
        {
            string got = q == null ? "<没有 quad>" : (q.Texture == null ? "<没贴图>" : q.Texture.name);
            CheckTrue(q != null && q.Texture != null && q.Texture.name == texName,
                      $"{what}：（前提·不静默）**节点自己身上**那颗 quad 的贴图 = 原版 `{texName}`"
                    + $"｜现读「{got}」"
                    + " —— ⛔ 取不到 / 取错就不该把下一条当绿（🧨 把圆底盘改回自造子件 ⇒ 本格红）");
        }
        float lx = 0f, ty = 0f, rx = 0f, by = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
        bool ok = q != null && MenuDraw.QuadRectPx(q, out lx, out ty, out rx, out by);
        CheckTrue(ok && Mathf.Abs(lx - x1) <= 1.5f && Mathf.Abs(ty - y1) <= 1.5f
                  && Mathf.Abs(rx - x2) <= 1.5f && Mathf.Abs(by - y2) <= 1.5f,
                  $"{what} 渲出来 = [{lx:F2},{ty:F2}]–[{rx:F2},{by:F2}]（{rx - lx:F1}×{by - ty:F1}）"
                + $"，应落在 [{x1:F2},{y1:F2}]–[{x2:F2},{y2:F2}]（{x2 - x1:F1}×{y2 - y1:F1}）±1.5px"
                + " —— ⚠️ 取的是**节点自己**那颗 quad（`GetComponent`；`CheckRectPx`/`RectOf` 的"
                + "「子树并集」会把命中层 `Hit` 并进来 ⇒ 量成 96.37×94.50）");
    }

    /// <summary>同 <see cref="CheckQuadRectPx"/>，但期望值**过 `Screen()`**（本窗把根那层 0.9 烘进矩形的那个换算）。</summary>
    static void CheckQuadRectS(Transform t, string texName, float x1, float y1, float x2, float y2, string what)
    {
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        CheckQuadRectPx(t, texName, s.x1, s.y1, s.x2, s.y2, what);
    }

    /// <summary>`AlignLeft` 会把 Label 的节点挪走（`MainMenuWindowBase` 的注释里写着）⇒
    /// **不能**拿它的位置去比矩形中心，要比**左边缘**。
    /// <para>🔴 **2026-10-19（`A1189`）它的形参收了 `y1`/`y2` 却一个都没用** —— 只比 `s.x1`。
    /// 于是被它接手的那些站（本窗 `EmailText` / `PasswordText`）**纵向与渲染尺度没人管了**
    /// （出处 = `资料/普查产出_第十会话/P0_断言7条修.md` §5 第 2 条）。
    /// ⇒ 那两维由下面那颗 <see cref="CheckTopBotS"/> 补，⛔ **别把两格并进本函数**：
    /// 它还有第 3 个调用点（页标题那一族），一起改会把「只比左边缘」的语义悄悄换掉。</para></summary>
    static void CheckLeftS(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（没有 Label）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        float leftPx = lb.transform.position.x * 108f + 960f - lb.WorldW * 108f * 0.5f;
        CheckTrue(Mathf.Abs(leftPx - s.x1) <= 2f, $"{what} 左边缘 = {leftPx:F1}（应为 {s.x1:F1}）");
    }

    /// <summary>🆕 **2026-10-19（`A1189`）**：<see cref="CheckLeftS"/> 的**纵向那一半** ——
    /// 给「被 `AlignLeft` 挪过、因而不能比矩形中心」的那一族补上**上下沿**这两维。
    /// <para>🔴 **为什么这维能独立量**：`Label.AlignLeftOn` 写的是
    /// `new Vector3(worldLeftX − …, **p.y**, p.z)` —— **只挪 x、`y` 原样保留**
    /// （`Battle/Label.cs`）。所以「左边缘对」与「纵向对」是**两件互不蕴含的事**，
    /// 前者绿不代表后者绿。</para>
    /// <para>两格判据都是**原版那一颗 TMP 的矩形**（设计 px 字面量 → 过 `Screen()`，与 `CheckLeftS` 同一条换算）：
    /// ① **纵向中心**：节点中心必须落在原版矩形的纵向中心上；
    /// ② **渲出来的字高 ≤ 原版框高**：`CLAUDE.md` §二 `AutoFitBox` 那条教训的**纵向那一半**
    /// （只比字号会漏掉「字号对而字把框撑破」那一族）。量法 = <see cref="TextRenderedPx"/>
    /// （TMP 自己的 `textBounds`），⛔ **不读 `Label.WorldH`** —— 那是**被测实现自己**写的字段缓存。</para>
    /// <para>⚠️ 量的是**节点自己的 `transform`**（同 `CheckLeftS`；`AlignLeft` 挪的就是它）。
    /// ⛔ **量不到必须报红**（`TextRenderedPx` 返回 `(−1,−1)` 那一档不许当 0 混过去）。</para></summary>
    static void CheckTopBotS(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（没有 Label）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        float cy = -lb.transform.position.y * 108f + 540f;
        CheckTrue(Mathf.Abs(cy - (s.y1 + s.y2) * 0.5f) <= 2f,
                  $"{what} 纵向中心 = {cy:F1}（应为 {(s.y1 + s.y2) * 0.5f:F1}）"
                + " —— ⚠️ 这一维**必须单独量**：`CheckLeftS` 只比左边缘，而"
                + " `Label.AlignLeftOn` 保留 `p.y` ⇒ 「左边缘对」推不出「纵向对」");
        var sz = TextRenderedPx(lb);
        CheckTrue(sz.y > 0f,
                  $"{what}：TMP 的 `textBounds` **量得到**（实得 {sz.x:F1}×{sz.y:F1}px）"
                + " —— ⛔ 量不到（没有 TMP / 点阵兜底后端）不许当 0 混过去，"
                + "那会让「没量到」静默变成「装得下」（同 `CheckAutoFit` 那条）");
        if (sz.y <= 0f) return;      // 没量到 ⇒ 下面那格无从谈起
        CheckTrue(sz.y <= s.H + 1.5f,
                  $"{what} 渲出来的字高 = {sz.y:F1}px ≤ **原版框高 {s.H:F1}px**"
                + $"（原版矩形 {y1:F2}…{y2:F2} 设计 px × 0.9，容差 1.5px）"
                + " —— ⚠️ 量的是 TMP 自己的 `textBounds`（⛔ 不是 `Label.WorldH` 那个字段缓存）");
    }

    /// <summary>🆕 2026-10-18（第四会话）：收口到 `MenuCheck.FindChild`（5 份逐字相同的那一份）。</summary>
    static Transform FindChild(Transform parent, string name) => MenuCheck.FindChild(parent, name);

    /// <summary>🆕 **2026-10-19（`A1181`）**：一颗 `Label` **渲出来**那块字有多大（画布 px · `Vector2(w, h)`）。
    /// 🔴 **走哪条口**：`TMP.textBounds`（= TMP 自己渲出来那块），同 `Editor/CollectionScene.cs` 的
    /// `LabelRenderedPx` / `TmpRenderedRect` 那一族口。
    /// ⛔ **不许读 `Label.WorldW/H`** —— 那是**字段缓存**（`_tmpW/_tmpH`，只有 `RefreshBounds()` 写），
    /// 也就是**被测实现自己** ⇒ 拿它当量法就是自证（本件验收原文点名的那一条）。
    /// ⛔ **也不许退到「我们传了多少」**（那量的是实参、不是画出来的东西）。
    /// <para>⚠️ 取**组件自己的 `transform`**（`AlignLeft/Right` 会把 `Label` 的节点挪走，拿外层容器算会偏）。
    /// 量不到（`lb == null` / 没有 TMP / 点阵兜底后端）⇒ 返回 `(-1,-1)`，**调用方必须报红**
    /// （⛔ 不许当 0 混过去 —— 那会让「量不到」静默变成「装得下」）。</para></summary>
    static Vector2 TextRenderedPx(Label lb)
    {
        if (lb == null) return new Vector2(-1f, -1f);
        var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if (tmp == null) return new Vector2(-1f, -1f);
        var b = tmp.textBounds;                       // 局部空间的行盒（`Bounds`）
        var M = tmp.transform.localToWorldMatrix;
        float x1 = float.MaxValue, y1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue;
        for (int c = 0; c < 4; c++)
        {
            var corner = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? b.min.x : b.max.x,
                                                        (c < 2) ? b.min.y : b.max.y, 0f));
            float px = LayoutSpace.PxX(corner.x), py = LayoutSpace.PxY(corner.y);
            x1 = Mathf.Min(x1, px); x2 = Mathf.Max(x2, px);
            y1 = Mathf.Min(y1, py); y2 = Mathf.Max(y2, py);
        }
        return new Vector2(x2 - x1, y2 - y1);
    }

    /// <summary>🆕 **2026-10-19（`A1181`）**：一个「文字站」的自适应核查。
    /// <para>🔴 **期望值一律取【原版 prefab 的字段值】**（由调用点逐条给，出处见那张表）——
    /// ⛔ **不是**我们传给 `SettingsWindow.Text` 的实参、⛔ 更不是 `SettingsWindow` 里的常量
    /// （拿被测实现里的数当期望 = 同式自证，通则 → `A131_自证通则.md`）。</para>
    /// <para>⚠️ 四格都是**原版设计 px** ⇒ 比之前先 × **0.9**（根上那层 `m_LocalScale`）。
    /// 那个 `0.9` 在这里**写字面量**、⛔ 不写 `SettingsWindow.RootScale`（后者在被测实现里）。</para>
    /// <para>🔴 **为什么还要量「渲出来那块字」**（`CLAUDE.md` §二 `AutoFitBox` 那条教训）：
    /// 只比字号的话，「字号字段对、字却溢出框」照样全绿。量的是 TMP 自己的 `textBounds`
    /// （<see cref="TextRenderedPx"/>），⛔ 不是 `Label.WorldW/H` 那个字段缓存。</para>
    /// <para>⚠️ `skipRendered` 那一档**只给「落在**没激活**子树里」的站用**（`Login Window`：
    /// 未激活的 TMP 不重排 ⇒ `textBounds` 是旧的 ⇒ 量了也是假绿）。那种站本条**如实说清没量**，
    /// ⛔ 不许把它算成「渲出来 ≤ 框」那一格。</para>
    /// <para>🔴 **2026-10-19（第十会话 · `A1194`）补上【框高】那一半**：原来只有 `boxW`
    /// （= 原版 `m_SizeDelta.x`）⇒ 「渲出来 ≤ 框」**只钉了宽**；而 `D2` 诊断
    /// （`资料/普查产出_第十会话/D2_全跑8红诊断.md` §2·C）实测**收敛值是被【框高】夹住的**
    /// （三点独立一致：`Account Tab Title` `63 ÷ 41.46` · `Tab Toggle Title` `36 ÷ 23.54` ·
    /// `VersionText` `36.70 ÷ 24.06`）⇒ **只比宽等于只补了一半**（`CLAUDE.md` §二 `AutoFitBox` 教训）。
    /// 新形参 `boxH` = **原版那一颗的 `m_SizeDelta.y`（设计 px，与 `boxW` 同一份 `menu_dump` 读数）**，
    /// ⛔ **不是**我们传给 `SettingsWindow.Text` 的实参、⛔ 更不是 `Shell/SettingsWindow.cs` 里的常量。
    /// ⚠️ 原版有几站的 `m_SizeDelta` 是**布局组/`AspectRatioFilter` 排出来的**（prefab 里读到 `0×0`）——
    /// 那几格的值取自 `menu_dump` 的**布局后**列、逐站在调用点注明。</para></summary>
    static void CheckAutoFit(Transform start, string[] chain, string what,
                             float minPx, float maxPx, float basePx, int wrapMode, float boxW, float boxH,
                             bool skipRendered = false)
    {
        const float RS = 0.9f;      // = 原版根上那层 `m_LocalScale`（**字面量**，见上）
        // 🔴 **2026-10-09（第十会话 · `D2` 诊断）**：**哨兵闸的上界** —— TMP 在「未重排 / 文案为空」时
        //   `textBounds` 停在 `2^32` 世界单位（画布 ≈ `4.64e11` px），而**它是个大正数**，
        //   所以原来那道 `sz.x >= 0f` 的闸**挡不住它**。真画布最多几千 px ⇒ 1e6 足够把它们分开。
        const float SentinelPx = 1e6f;
        Transform t = start;
        for (int i = 0; i < chain.Length && t != null; i++) t = FindChild(t, chain[i]);
        if (t == null)
        { CheckTrue(false, $"{what}：找不到节点链 `{string.Join(" / ", chain)}`（下面几格无从谈起）"); return; }
        var lb = t.GetComponentInChildren<Label>();
        if (lb == null) { CheckTrue(false, $"{what}：那一格上找不到 `Label`"); return; }

        CheckTrue(lb.AutoSizing,
                  $"{what}：**真的开了自适应**（原版 `m_enableAutoSizing = 1`）"
                + " —— 🧨 改坏法：把 `SettingsWindow.Text` 里那次 `MenuDraw.Text(…, fitW * RootScale, …)`"
                + " 的四个实参传回 `0`（= 本笔之前的写法）⇒ `AutoSizing` 恒 false ⇒ 本行红");
        CheckNear(Label.FontSizeToPx(lb.FontSizeMin), minPx * RS, 0.2f,
                  $"{what}：自适应**下限** = 原版 `m_fontSizeMin` **{minPx:F2}px** × 0.9 = {minPx * RS:F2}"
                + $"（实得 {Label.FontSizeToPx(lb.FontSizeMin):F2}）—— ⛔ 不是我们那一侧的实参");
        CheckNear(Label.FontSizeToPx(lb.FontSizeMax), maxPx * RS, 0.2f,
                  $"{what}：自适应**上限** = 原版 `m_fontSizeMax` **{maxPx:F2}px** × 0.9 = {maxPx * RS:F2}"
                + $"（实得 {Label.FontSizeToPx(lb.FontSizeMax):F2}）—— 🧨 改坏法：上限按 `fs` 推（原版有几颗"
                + " `max ≠ fs`）⇒ 本行红");
        CheckNear(Label.FontSizeToPx(lb.FontSizeBase), basePx * RS, 0.2f,
                  $"{what}：二分起点 = 原版 `m_fontSizeBase` **{basePx:F2}px** × 0.9 = {basePx * RS:F2}"
                + $"（实得 {Label.FontSizeToPx(lb.FontSizeBase):F2}）");
        Check(lb.WrappingMode, wrapMode,
              $"{what}：折行档 = **原版 `m_TextWrappingMode = {wrapMode}`**"
            + (wrapMode == 0 ? "（`NoWrap`）" : "（`Normal` = 折行开）")
            + " —— 🧨 改坏法：把 `SettingsWindow.Text` 里那句 `if (autoMinPx > 0f && wrapPx <= 0f) lb.SetWrapping(false);`"
            + " 删掉 ⇒ 折行=0 的那些站全变 1 ⇒ 本行红");

        if (skipRendered)
        {
            // ⚠️ 断的是【跳过的理由】而不是「跳过了」：这一件必须在**没激活**的子树里
            //    （未激活的 TMP 不重排 ⇒ `textBounds` 停在旧值 ⇒ 量出来是假绿）。
            CheckTrue(!lb.gameObject.activeInHierarchy,
                      $"{what}：⚠️ **「渲出来 ≤ 框」这一格【没量】** —— 这一件在**没激活**的子树里"
                    + "（那颗 TMP 自己出厂就是 `m_IsActive = 0`，或整棵宿主是关着的）⇒ 未激活的 TMP 不重排、"
                    + "`textBounds` 停在旧值 ⇒ **如实登记**，⛔ 别当它绿；"
                    + "🧨 本行红 = 它现在其实是**激活**的（那这一格就该真量）");
            return;
        }
        var sz = TextRenderedPx(lb);
        // 🔴 **2026-10-09（第十会话 · `D2` 诊断 · α · 本条修两个病，都在这一小段）**：
        //   ① **空串先判**：TMP 对**空串**不生成字形 ⇒ `textBounds` 停在**哨兵值**（实测世界 `2^32`
        //      ⇒ 画布 ≈ `4.64e11` px）。账号页那颗 `Account Form > Error Message` **出厂就是 `""`**
        //      ⇒ 「渲出来 ≤ 框宽」对它**无意义**。原来那句注释承诺「文案为空时宽度 0 是正常的」，
        //      可代码却接着拿哨兵去比框宽 ⇒ **红**。⇒ 改成**如实登记 + 跳过**（⛔ 不是「装得下」）。
        //   ② **哨兵闸**：原来那道闸写的是 `sz.x >= 0f` —— 而**哨兵是个大正数、照样过闸**
        //      ⇒ 「没量到」被当成「量到了」，最后红报到**「字溢出框」**上：**既假红、又把真因盖住**。
        //      真画布最多几千 px ⇒ 用 `SentinelPx` 把它挡在外面。
        //   🔑 顺带：**量不到时**要手动推一次版面 —— 见下面那段 `ForceRelayout`（⛔ 不是靠读 `WorldW`：
        //      那条路只在「有【待办】的折行」时才兑现，`_pendWrapW < 0` 时**直接返回**，
        //      对「批处理没有帧循环 ⇒ 压根没重排过」这一档**不管用**）。
        if (string.IsNullOrEmpty(lb.Text))
        {
            CheckTrue(true, $"{what}：⚠️ **文案出厂为空串 ⇒ 本格【不量】渲出宽**"
                          + $"（TMP 对空串的 `textBounds` 是哨兵值，实测 {sz.x:F1}px）"
                          + " —— 这是**如实登记**，⛔ 别读成「装得下」");
            return;
        }
        bool measured = sz.x >= 0f && sz.x < SentinelPx;
        //   🔑 顺带（2026-10-09 第二轮）：**量不到就手动推一次版面** —— 批处理**没有帧循环**，
        //      TMP 的 `OnPreRenderObject` 不会自己跑 ⇒ 有些 Label 的 `textBounds` 会一直停在哨兵
        //      （实测 `Graphics Tab > FPS Limit > FPS Slider > 30 FPS` 就是）。本仓现成的口 =
        //      **`Label.ForceRelayout()`**（`SetFontSize(当前值)` 早退 + 一次 `ForceMeshUpdate`，
        //      **幂等**、按当前折行模式与 `[min,max]` 重新收敛）—— 形状同「批处理下粒子要手动 `Simulate`」。
        //      ⚠️ **只在量不到时才推**（常态一次都不推）：它按新宽度重排会挪 TMP 子节点，
        //      常态推会动到别的断言量的坐标。
        if (!measured)
        {
            lb.ForceRelayout();
            sz = TextRenderedPx(lb);
            measured = sz.x >= 0f && sz.x < SentinelPx;
        }
        CheckTrue(measured,
                  $"{what}：TMP 的 `textBounds` **量得到**（实得 {sz.x:F1}×{sz.y:F1}px）"
                + " —— ⛔ 量不到（`TextRenderedPx` 返回 −1：没有 TMP / 点阵兜底后端）不许当 0 混过去，"
                + "那会让「没量到」静默变成「装得下」；"
                + $"⚠️ **哨兵**（TMP 未重排时 `textBounds` 停在 `2^32` 世界单位 ≈ `4.64e11` 画布 px）也归这一档");
        if (!measured) return;      // 没量到 ⇒ 下面两条无从谈起（⛔ 别拿哨兵去比框宽）
        CheckTrue(sz.x > 0f,
                  $"{what}：**文案非空 ⇒ 渲出来的宽度也必须 > 0**（实得 {sz.x:F1}px；文案 = 「{lb.Text}」）"
                + " —— 这一条防的是「整颗字一个字形都没生成」（那种情况下下面那条会假绿）");
        CheckTrue(sz.x <= boxW * RS + 1.5f,
                  $"{what}：**渲出来 {sz.x:F1}px ≤ 框宽 {boxW * RS:F1}px**（原版 `m_SizeDelta.x` {boxW:F2}px × 0.9，"
                + "容差 1.5px = TMP 二分收敛粒度）—— 🧨 只比字号会漏掉「字号对而溢出」那一族（`AutoFitBox` 教训）");
        // 🔴 **2026-10-19（第十会话 · `A1194`）**：「渲出来 ≤ 框」的**另一半**。
        //   `D2` 诊断（§2·C）三点独立一致地实测出**收敛字号 = 框高 ÷ 1.52**
        //   （`Account Tab Title` 63÷41.46 · `Tab Toggle Title` 36÷23.54 · `VersionText` 36.70÷24.06）
        //   ⇒ **框高才是这几站的实际约束**，只钉框宽 = 只补了一半（`CLAUDE.md` §二 `AutoFitBox` 那条）。
        //   ⚠️ `1.52` 是 `D2` 的**算术拟合**、没从字体资产核过行高比 ⇒ ⛔ 别把这个数写进断言；
        //      本条比的是**渲出来的字高 vs 原版框高**，判据 = 原版 `m_SizeDelta.y`。
        //   🧨 改坏法：把框高传大（`Shell/SettingsWindow.cs` 里 `SetAutoFitBox` 那一格的 `r.H`
        //      换成别的数）⇒ 字被放到撑出原版框 ⇒ 本行红。
        CheckTrue(sz.y <= boxH * RS + 1.5f,
                  $"{what}：**渲出来 {sz.y:F1}px ≤ 框高 {boxH * RS:F1}px**（原版 `m_SizeDelta.y` {boxH:F2}px × 0.9，"
                + "容差 1.5px）—— 🧨 这一格是 `AutoFitBox` 教训的**纵向那一半**："
                + "`D2` §2·C 实测**收敛值是被框高夹住的**（`Tab Title` 63÷41.46 等三点一致）"
                + "⇒ 只钉框宽那一条等于只补了一半");
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

    /// <summary>🆕 **2026-10-19（波 1b 断言宿主）**：把一条**带 `{0}` 的词条**的**固定前缀**取出来当判据。
    /// <para>为什么需要它：`_flash` 那一族（「点完有话说」）的完整那句里夹着**运行期值**
    /// （画质档名 / `FpsText()`）⇒ 拿不到整句当期望；而**前缀那一截是词条自己的、跟着语档走**
    /// （例 `Settings/Graphics/Flash/Quality` 的 ZH `画质档 → {0}` / EN `Quality → {0}`
    ///  ⇒ 前缀分别 `画质档 → ` / `Quality → `）。</para>
    /// <para>🔴 ⛔ **别把中/英任一串写死在调用点** —— 那只是把红从这一档挪到那一档
    /// （同本文件 `AutoZoom` 那条 `:1478` 的口径）。</para>
    /// <para>⚠️ 词条里没有 `{0}` 时原样返回（= 整句就是判据）。</para></summary>
    static string TermHead(string key)
    {
        string v = Loc.T(key);
        if (string.IsNullOrEmpty(v)) return "";
        int i = v.IndexOf("{0}", System.StringComparison.Ordinal);
        return i < 0 ? v : v.Substring(0, i);
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
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
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
            //      它由 `Shell/SettingsWindow.cs` 里 `Solid(root, "Menu Dark Background", …)` 那一处 的 `Node(...)` + `Solid(root, "Menu Dark Background", …)`
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
                // 🆕 **2026-10-15（A672）夹具探针加固**：裸 `AddComponent<GameWindow>()` 的探针**从不赋 `type`**
                //   ⇒ 字段停在哨兵 `GameWindow.UnsetType`(-1)（口径与那一整套 → 下面 `probe-1` 那一处）。
                //   ⚠️ 钉住之后本条**只剩 `placement` 一条出声**（原来第一拍会同时报 `placement` + `type` 两条）
                //   —— 下面两句 `CaptureErrors` 的判据**一位未动**：`errs[0]` 仍是 `placement` 的文案，
                //   第二拍仍是 `Count == 0`（W8 的「兜底写回」那半边也因此不再是前提）。
                probeWin.type = WindowType.Fullscreen;
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

            // ---------------- 🆕 A672：`type` 忘了显式赋值 ⇒ **出声**（与上面 A166 同形）----------------
            // 判据：原版那颗 MB 的 `type` 是**必填**的；`WindowsManager__OpenWindowCO.c` 按它决定
            //   「把其余全部藏起来」（==0 Fullscreen）还是「把上一个压到背景」（==1 Popup）。
            // 生产侧（哨兵 `UnsetType` + 两层出声 + 兜底写回）→ `Shell/WindowsManager.cs`（写手 W8 落地；
            //   报告 = `资料/普查产出_1015/W8_WindowsManager_A672与A633.md`）。
            // 🔴 **本节点 = A672 的【夹具那一半】里的第一层**（`AttachToAnchor` = 建窗那一刻）。
            //   **第二层**（有人绕过 `AttachToAnchor` 直接开 ⇒ `OpenWindow` 那一层）挂在 `Editor/ShellScene.cs`
            //   的同名节 —— 那边有一台**真的** `WindowsManager` + 成对的开关窗夹具。两个宿主各守一层，
            //   ⛔ 别在这边再抄一遍（「两处写同一条规则 = 迟早不一致」）。
            Section("A672：`type` 忘了显式赋值 ⇒ 出声（① 建窗那一刻 `AttachToAnchor`）");
            Debug.Log(P + "  ⚠️ 下面这一条会**故意**打一行 `[Win] …` 的 LogError（就是「出声」本身）—— 那不是失败");
            {
                var tGo = new GameObject("probe window (type 未赋)");
                var tWin = tGo.AddComponent<GameWindow>();
                // `placement` 先赋成**合法档**：这一趟只剩 `type` 一条出声（否则 `errs[0]` 会被 A166 那条顶掉）
                tWin.placement = WindowsPlacement.Popup;
                CheckTrue(!tWin.HasType,
                          "裸 `GameWindow` 的 `type` 出厂是**哨兵**（= 还没显式赋过值）—— 与 `placement` 同形不同值");
                var tErrs = CaptureErrors(() => WindowsManager.AttachToAnchor(tWin));
                CheckTrue(tErrs.Count > 0 && tErrs[0].Contains("`type` **没有显式赋值**"),
                          "★ **忘了赋 `type` ⇒ 出声**（改坏法：默认值改回 `WindowType.Fullscreen` ⇒ 一声不吭 ⇒ 这条红）"
                        + "；实得 " + tErrs.Count + " 条：" + (tErrs.Count > 0 ? tErrs[0] : "**一条都没有**"));
                // 🔴 这一条压的是【兜底 + 写回】：哨兵漏给 `OpenWindow` 那句 `== Fullscreen`（**肯定式**）会
                //   **静默走弹窗支**（把别的窗压到背景）—— 与「静默走全屏支」不同的另一种坏法、同样不会红。
                CheckTrue(tWin.type == WindowType.Fullscreen && tWin.HasType,
                          "…而且照**旧默认值 `Fullscreen`(0)** 兜底 + **写回字段**（⛔ 别让它落成「当弹窗」那一支）");

                // 反面（互为对照）：显式赋过值 ⇒ **一声不吭** —— 否则上面那条只是「反正有日志」，分不出两种状态。
                var t2Go = new GameObject("probe window (type 已赋)");
                var t2Win = t2Go.AddComponent<GameWindow>();
                t2Win.placement = WindowsPlacement.Popup;
                t2Win.type = WindowType.Popup;
                CheckTrue(t2Win.HasType, "显式赋过值之后 `HasType` = true（哨兵不是「合法档位」）");
                var tErrs2 = CaptureErrors(() => WindowsManager.AttachToAnchor(t2Win));
                CheckTrue(tErrs2.Count == 0,
                          "显式赋过 `type` 的窗**不报警**（实得 " + tErrs2.Count + " 条；上面那条才分得出两种状态）");

                // 两条哨兵**一起**没赋时：**先后顺序固定（`placement` 在前）** —— W8 把 A166 那条★断言
                //   （它取的是 `errs[0]`）压在 `AttachToAnchor` 里那两段的**书写顺序**上（见 W8 报告 §三·⑤）。
                //   🔴 怎么改坏就红：把那两段对调 ⇒ A166 那条会去比 `type` 的文案 ⇒ **这条先红**（把它钉住）。
                var t3Go = new GameObject("probe window (两条哨兵都没赋)");
                var t3Win = t3Go.AddComponent<GameWindow>();
                var tErrs3 = CaptureErrors(() => WindowsManager.AttachToAnchor(t3Win));
                CheckTrue(tErrs3.Count >= 2 && tErrs3[0].Contains("`placement` **没有显式赋值**")
                                              && tErrs3[1].Contains("`type` **没有显式赋值**"),
                          "两条哨兵都没赋 ⇒ **两条都出声、且 `placement` 在前**（实得 " + tErrs3.Count + " 条："
                        + (tErrs3.Count > 0 ? tErrs3[0] : "**一条都没有**") + "）");
                Object.DestroyImmediate(tGo); Object.DestroyImmediate(t2Go); Object.DestroyImmediate(t3Go);
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
            // 🔴 **2026-10-10（A1204）就地订正**：`Separators` 的上/下沿原来断的是**未裁**的那一对常量
            //   （`SettingsWindow.BarSepT/BarSepB`）。`A1204` 把 `Mask Tabs buttons` 那层补上之后，
            //   它被**裁到遮罩框**（设计 px `132.5485 … 956.3875`）⇒ quad 从 `2.610×794.772` 变
            //   `2.610×741.455` **屏幕 px** ⇒ 旧的那一对**必红**。
            //   ⚠️ **期望值写字面量、⛔ 不写 `SettingsWindow.TabsMaskT/B`** —— 拿被测实现自己的常量
            //   去证明被测实现 = **自证**（本仓明令）。
            //   **改坏法**：删掉 `Shell/SettingsWindow.cs` 里那句 `ViewportClip.Hang(area, "Mask Tabs buttons", …)`
            //   ⇒ 裁切没了、`Separators` 回到 `794.772` ⇒ 本行红。
            CheckRectS(FindChild(Area(root), "Separators"), SettingsWindow.BarSepL, 132.5485f,
                       SettingsWindow.BarSepR, 956.3875f, "`Separators`（上下沿 = 被 `Mask Tabs buttons` 裁过）");
            // 🔴 **2026-10-09（A1120）**：这一条量的是钮的**可见面**（75×75 那张圆底），
            //   **不是「Generic Close Button」那棵子树** —— `d6c4111`（`A1053`「命中区归真值」）起，
            //   子树里多了一颗**命中层**（`Hit` 节点，矩形 = 原版射线区，**比可见面大**）⇒
            //   `RectOf` 的「子树并集」会量到那颗、不再等于钮的脸（这正是那两条红：量出 86.7×85.0、
            //   期望 67.5×67.5）。**期望值一个字没改**（仍是原版 `1559.00,91.61→1634.00,166.61` 过 `Screen()`）。
            //   🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：原来这一条取的是**子件 `bg`**
            //   （`FindChild(节点, "bg")`）—— **那颗子件已经不存在了**：挂点已归真，圆底盘现画在根节点
            //   `Generic Close Button` **自己身上**（= 原版结构：`Main Menu Settings Window > Menu Area >
            //   Generic Close Button` 那颗 `Image` 就是圆底、**没有 `bg` 这一层**；判据 = `python -I
            //   d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "Main Menu Settings Window" 12`）。
            //   ⇒ 改量**节点自己身上那一颗**（`GetComponent`，⛔ **不能**退回 `CheckRectS` 的「子树并集」：
            //   那会把命中层 `Hit` 并进来 ⇒ 量出 96.37×94.50）。这一改**同时**把
            //   「圆底盘必须长在根节点自己身上」钉住：🧨 把它改回自造子件 `bg` ⇒ 本格红。
            {
                CheckQuadRectS(FindChild(Area(root), "Generic Close Button"), "UI_Button_Round_background",
                               SettingsWindow.CloseL, SettingsWindow.CloseT,
                               SettingsWindow.CloseR, SettingsWindow.CloseB,
                               "`Generic Close Button` 的可见面（圆底 `UI_Button_Round_background`，75×75）");
            }
            CheckRectS(FindChild(FindChild(Area(root), "Generic Close Button"), "Icon"),
                       SettingsWindow.CloseIconL, SettingsWindow.CloseIconT,
                       SettingsWindow.CloseIconR, SettingsWindow.CloseIconB, "关闭钮的 `Icon`");
            // 🆕 **2026-10-09（A1120）**：**关闭钮的命中区**（`Hit` 节点；它的唯一那颗 quad 是全透明的，
            //   `MenuDraw.MakeHitQuad` 给 tint `(0,0,0,0)`）—— 期望 = **原版可射线区 86.73×85.05（画布 px）**。
            //   判据（原版 prefab 亲读）= `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all
            //   "Main Menu Settings Window" --depth 8 --substr "Generic Close Button"` ⇒
            //   全子树**只有 1 颗**可射线件：`…/Generic Close Button/Icon`（`40k_bt_close`，裸矩形
            //   **50.73×49.05**、**父链缩放 0.9**）按自己的 `m_RaycastPadding (-20)⁴` 外扩 = **86.73×85.05**
            //   （根那颗 `UI_Button_Round_background` 是 `m_RaycastTarget = 0`、不吃射线）。
            //   期望值取**设计 px**（= 原版字段手算、`Screen()` 之前的那一帧；`CheckRectS` 内部会过 `Screen()`，
            //   所以下面四个字面量是 `Icon` 矩形外扩 20 之后的样子，⛔ 别写成屏上的 1489.48/127.67 那一套）：
            //   `1568.31,101.86→1624.68,156.35` 四边各外扩 20 ⇒ `1548.31,81.86→1644.68,176.35`（96.37×94.49）。
            //   🔴 **这一条就是 `A1120`(b) 的裁断书**：我们传进 `Hit` 的那个 `96.37×94.50` 是**设计 px**
            //   （`Hit()` 入口自己过 `Screen()` ⇒ 屏上 ×0.9 = **86.73×85.04**，与原版射线区逐位吻合）
            //   ⇒ **实现是对的**，红的是「拿全子树并集当钮的脸」那个**量法**。把 96.37 当屏上值写进来 = 漏了 0.9。
            CheckRectS(FindChild(FindChild(Area(root), "Generic Close Button"), "Hit"),
                       1548.31f, 81.86f, 1644.68f, 176.35f,
                       "关闭钮的命中区（= 原版可射线区：`Icon` 外扩 `(-20)⁴`、父链 0.9 ⇒ 86.73×85.05）");
            // 🆕 **2026-10-09（`A1125`）**：**换图层那一半** —— `A1120` 只断了**命中区**，
            //   「悬停换哪一层」本窗**一条断言都没有**（`A1058` 的另一半）。
            //   判据（原版 prefab 亲读）= `Main Menu Settings Window > Menu Area/Generic Close Button` 那颗
            //   `EverguildButton` 的 **`m_TargetGraphic` = pid8542793629372546982** ⇒ **所属 GO 名 = `Icon`**、
            //   贴图 = **`40k_bt_close`**（⛔ 不是圆底盘 `bg` = `UI_Button_Round_background`；
            //   出处 = `Shell/SettingsWindow.cs` 那一段亲读注释，本文件只引用、不重推）。
            // 🧨 改坏法：把 `Hit(...)` 的 `target` 实参换成圆底盘（`bg`）⇒ **两个条件同时不成立** ⇒ 红
            //   （错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）。
            {
                var a1125HitN = FindChild(FindChild(Area(root), "Generic Close Button"), "Hit");
                CheckTrue(a1125HitN != null,
                          "（前提·不静默）A1125：SettingsWindow 关窗钮的 `Hit` 节点拿得到"
                        + " —— ⛔ 取不到就不往下断「换图层」（不静默变绿）");
                var a1125Wb = a1125HitN != null ? a1125HitN.GetComponent<WindowButton>() : null;
                CheckTrue(a1125Wb != null && a1125Wb.target != null && a1125Wb.target.Texture != null
                          && a1125Wb.target.gameObject.name == "Icon"
                          && a1125Wb.target.Texture.name == "40k_bt_close",
                          "★★ A1125：SettingsWindow 关窗钮的**换图层** = 子件 `Icon`（图 `40k_bt_close`；"
                        + "= 原版 `m_TargetGraphic` 指到的那一颗）—— 现读「"
                        + (a1125Wb == null || a1125Wb.target == null || a1125Wb.target.Texture == null
                           ? "<没绑>" : a1125Wb.target.gameObject.name + " / " + a1125Wb.target.Texture.name)
                        + "」"
                        + " —— 改坏法：把它传成圆底盘 `bg`（`UI_Button_Round_background`）⇒ 红");
            }

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
            //  ⑤ 🔴 **2026-10-09（A1120）**：量的是那颗**可见面**（矩形与「关闭钮」节点逐值相同：
            //     仍是 [1559.00,91.61]–[1634.00,166.61]），**⛔ 不是「Generic Close Button」那棵子树** ——
            //     自 `d6c4111`（`A1053`）起子树里多了一颗**命中层**（`Hit` = 原版射线区 86.73×85.05，
            //     **比可见面大**）⇒ 量子树会量到它、这条锚当场红（[1489.48,127.67]–[1576.21,212.72]）。
            //     锚的价值**一分没少**（同一个矩形、仍离画布中心最远；③④ 那两段手算与改坏法逐条照旧）。
            //  ⑥ 🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：上面 ⑤ 原来取的是子件 `bg`
            //     —— **那颗子件已经不存在了**（挂点已归真：圆底盘现画在 `Generic Close Button` **自己身上**，
            //     见 `Shell/SettingsWindow.cs` 那一段；原版 `Main Menu Settings Window > Menu Area >
            //     Generic Close Button` 那颗 `Image` 就是圆底、没有 `bg` 这一层）。
            //     ⇒ 改走新助手 `CheckQuadRectPx`（**节点自己那颗** quad，⛔ 不是子树并集）。期望值**一个字没改**。
            Section("🔴 ②（A131）映射锚断言：期望值是原版字段手算的字面量（⛔ 不过 `Screen()` / `OrigPxY`）");
            CheckQuadRectPx(FindChild(Area(root), "Generic Close Button"), null,
                            1499.10f, 136.44f, 1566.60f, 203.94f,
                            "（②锚）关闭钮可见面的渲染矩形 = 原版 [1559.00,91.61]–[1634.00,166.61]"
                            + " 经「0.9 + 画布中心」缩放（`bg` 那一层已归真到根节点自己身上）");
            var bgQ = FindChild(Area(root), "Generic Popup Background").GetComponentInChildren<ImageQuad>();
            CheckTrue(bgQ != null && bgQ.Texture != null && bgQ.Texture.name == "40k_popup",
                      "弹窗底图 = `40k_popup`");

            // ---------------- 左栏四个页签 ----------------
            // 🔴 **2026-10-17（A863）**：这一列的 VLG 参数**逐条实读原版 prefab** 后的真值 =
            //    `m_Padding = (L 0, R 0, T **13**, B 0)` · `m_Spacing = **8.920000076293945**`
            //    · `m_ChildAlignment = **5**(MiddleCenter)` · `m_ChildControlWidth/Height = 0`
            //    · `m_ChildForceExpandWidth = 1` · 五个键各自的 `m_SizeDelta = (165.0, **157.68350219726562**)`。
            //    出处 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-8185144684232147034.json`
            //    （`m_GameObject.m_PathID = -301262919896891482` = 本窗 `Tab Buttons`）+ 五份键 RT
            //    （`RectTransform_-1745314864996450394` 起）逐份实读；布局后的屏幕值 =
            //    `python 工具/menu_dump.py bundle_menus_assets_all "Main Menu Settings Window" --depth 6`。
            // ⚠️ 本节旧标题写的是「padTop 30 · 每键 178.42×157.68 · 从 y=153.10 起」—— **三项全是错的**。
            // ⚠️ 下面**期望值一律是字面量**（⛔ 不拿 `SettingsWindow.*` 当期望 —— 那样常量改坏了也照绿）。
            Section("左栏页签（原版 VLG：padTop 13 · spacing 8.92 · 每键 165×157.6835 · 贴右沿 · MiddleCenter）");
            // 🔴 **2026-10-10（F4）**：`bar` **不存**（原来这里是 `var bar = FindChild(area, "Tab Buttons");`，
            //   而 A176 那一段的 `Close()` + 两次 `OpenWindow` 会把整棵树重建 ⇒ 存下来的 `bar` 变假 null
            //   ⇒ 音频页 / 联机页**一次都没切过去**、并连带 5 条矩形断言假红）。改走**现取** `Bar(root)`。
            CheckAtS(Bar(root), SettingsWindow.BarL, SettingsWindow.BarT, SettingsWindow.BarR, SettingsWindow.BarB, "`Tab Buttons` 列");
            // 🔴 这一列的数量走过 **4 → 5 →（一度 6 → 裁定后回到）5**（`General` 是**第一个**，原版页签序
            //    `General/Media/Account/Graphics/Support` 也是它第一 —— 见 `Shell/SettingsWindow.cs` 的 `BuildTabs`；
            //    4 那一档 = `A1175` 之前、5 那一档 = `A1175` 建了 `Account`）。**下面是最后那一档的判据。**
            // 🔴 **2026-10-19（A1183 + `A1186` 裁定 ①）**：`Support` 页建出来、`Online` **从栏里撤掉**
            //     ⇒ 栏里**仍然是 5 格**，而且**逐位是原版那 5 个键**：
            //     `General / Audio`(=原版 `Media`)`/ Account / Graphics / Support`。
            //     **为什么不让它 6 格**（用户裁定、⛔ 别翻案）：原版那条栏是 `VerticalLayoutGroup` + **居中对齐**
            //     ⇒ 多一格会把原版那 5 格**整体上移 41.65px**、并两头溢出（上 67.31 / 下 80.31 设计 px）。
            //     `Online` 的入口改到 `General` 页那颗钮上（见本文件 ⑥ 那一段 + `GenOnlineEntry`）。
            //     ⚠️ **`Online` 仍是合法页号**（`SettingsTab.Online = 5`）—— 只是这一栏里没有它的键，
            //     ⛔ 因此**下面这一节不许再 `Click(Bar(root), "Online")`**（那会点了个不存在的键）。
            //     ⛔ 次序必须与 `Shell/SettingsWindow.cs` 的 `SettingsTab` 序号**逐个对齐**（`Click` 之后比 `Current`）。
            var names = new[] { "General", "Audio", "Account", "Graphics", "Support" };
            // 🆕 **2026-10-19（波 1b）**：五个页签**各自的词条键**（顺序与 `names` 逐位对齐）。
            //   ⚠️ 「节点名」（`names`）与「页签上印的字」（键）是**两件事**：节点名一个都没动
            //   （`FindChild` / `Click` 靠它），键只决定画出来那行字（见 `Shell/SettingsWindow.cs` 的 `BuildTabs`）。
            //   ⚠️ `Audio` 那一格的键是 `Settings/Media/Title` ⇒ **英文档印 `Media`**（波 0b3 已裁、有意）。
            //   🔴 **2026-10-19（A1185 + `A1183` 的收尾 · 铁律 5）就地改掉两格**：
            //     · 第 5 格 `"Online"` → **`"Settings/Support/Title"`**（栏里那第 5 格现在是 `Support` 页签）；
            //     · 第 3 格原来写的是**字面 `null`** —— 那一格当时是一条「**进表就红**」的哨兵
            //       （`Loc.HasEntry(null)` 恒 false ⇒ 期望值停在英文 `Account`）。**`Settings/Account/Title`
            //       已经在表里了**（`A1185` 那批 25 条）⇒ 那句注释说的「把期望值改成 `Loc.T(键)`」**现在做**：
            //       填**真键**，下面那行期望值就一律是 `Loc.T(键)`（五格同一条算式，不再有那一档三元式）。
            //       ⛔ **这条断言不许删** —— 它两个方向都还在盯：键被删 ⇒ 生产代码 `AcTerm` 退英文、
            //       而 `Loc.T` 会**返回键名本身** ⇒ 红。
            var tabTitleKeys = new[] { "Settings/General/Title", "Settings/Media/Title", "Settings/Account/Title",
                                       "Settings/Graphics/Title", "Settings/Support/Title" };
            // ---- ① A863 参数（**字面量**，逐条带原版出处；这几条同时是「旧值改坏了会红」的判别式）----
            //   ⚠️ 键高那条尤其要认准：`menu_dump` 印的 **141.92 是屏幕 px**（= 157.6835 × 根上那层 0.9），
            //      **不是设计值**。照 141.92 改键高会比原版**矮 11%**（正是本工程踩过的那类单位坑）。
            CheckNear(SettingsWindow.TabBtnH, 157.6835f, 0.01f,
                      "键高 = 原版五个键的 `m_SizeDelta.y` 原文 **157.6835**（设计 px；⛔ 不是 `menu_dump` 印的 141.92 —— 那是屏幕 px）");
            CheckNear(SettingsWindow.TabGap, 8.92f, 0.01f,
                      "键间缝 = 原版 VLG `m_Spacing` **8.92**（旧代码**没有这一项** ⇒ 等价于 0）");
            CheckNear(SettingsWindow.BarPadTop, 13f, 0.01f,
                      "栏顶内边距 = 原版 VLG `m_Padding.m_Top` **13**（旧值 30）");
            CheckNear(SettingsWindow.BarPadBottom, 0f, 0.01f, "…`m_Padding.m_Bottom` = **0**");
            CheckNear(SettingsWindow.TabW, 165f, 0.01f,
                      "键宽 = 原版五个键的 `m_SizeDelta.x` **165.0**（旧代码 = 满栏宽 178.42）");
            CheckNear(SettingsWindow.TabStep, 166.6035f, 0.01f, "键顶步进 = 高 157.6835 + 缝 8.92 = **166.6035**");
            // ② 键顶**逐行**：期望值是**手算字面量**（⛔ 不过 `SettingsWindow.TabTop` —— 那是被测实现）。
            //    uGUI `GetStartOffset`：content = **5**×157.6835 + **4**×8.92 = **824.09751**
            //    surplus = (966.19 − 123.10) − (824.09751 + 13 + 0) = **5.99249**；`align = 0.5`
            //    ⇒ start = 123.10 + 13 + 5.99249×0.5 = **139.09624**；加 k×166.6035。
            //    🔴 **2026-10-19（A1175）就地重算**（原来那一套是 **4** 页档：657.4940 / 172.5960 / 222.3980）——
            //    页数从 4 变 5 ⇒ 余量重新分摊，首键顶 **222.398 → 139.096**。
            //    ⚠️ 这一档**正是原版 prefab 的真值档**（原版就是 5 个键，`Shell/SettingsWindow.cs` 文件头记的
            //    「首键顶 = 139.11」与这里差 **0.014** —— 那是 `menu_dump` 布局仿真的取整，⛔ 不是我们算错）。
            //    🔴 **2026-10-19（`A1186` 裁定 ①）这一档【不许再动】**：`A1183` 那一笔一度把键数推到 6
            //    （首键顶会变成 **55.794** = 余量 −160.611 的一半、两头各溢出 67.31 / 80.31）
            //    ⇒ 用户裁定栏里就放原版那 5 格 ⇒ **这一组字面量回到上面那一条**（照旧是设计值、不过 `TabTop`）。
            var tabTops = new[] { 139.09624f, 305.69975f, 472.30325f, 638.90675f, 805.51025f };
            for (int i = 0; i < 5; i++)
            {
                // 🔴 **2026-10-19（`A1183` + `A1186` 裁定 ①）**：这一列现在 = **原版那 5 个键逐位**：
                //   `General / Audio`(=原版 `Media`)`/ Account / Graphics / Support`（`A1175` 建了 `Account`、
                //   `A1183` 建了 `Support`；`Online` **从栏里撤掉**、改从 `General` 页那颗钮进）。
                var n = FindChild(Bar(root), names[i]);
                CheckTrue(n != null, $"第 {i + 1} 个键 `{names[i]}` 建出来了（我们建 5 个 = 原版那 5 个，逐位）");
                if (n == null) continue;
                // 键那一格：左沿 **341.52**（= 栏右沿 506.52 − 键宽 165，**贴右沿**、⛔ 不是满栏宽）
                CheckAtS(n, 341.52f, tabTops[i], 506.52f, tabTops[i] + 157.6835f,
                         $"`{names[i]}` 键在 VLG 算出来的位置（第 {i + 1} 个）");
                // 🔴 **2026-10-19（波 1b）就地改掉「拿节点名当期望值」**：页签**全接词条了**
                //    （`Graphics/Audio/Online` 三条原来 `Key = (string)null`、照 `Label` 原样画）。
                //    旧写法 `names[i] == "General" ? Loc.T(…) : names[i]` 现在**两档各红几条**：
                //    · 中文档：`图像`/`媒体`/`支持` ≠ `Graphics`/`Audio`/`Support` ⇒ 红 3 条；
                //    · 英文档：`Audio` 那条印的是 `Media`（`Settings/Media/Title` 的 EN 列）⇒ 仍红 1 条；
                //      （剩两条恰好逐字相等的 ⇒ 那两档看不出问题 —— 正是「只断一种情况」的坑。）
                //    ⇒ 期望值一律走 `Loc.T(键)`：⛔ 不写死节点名、也⛔ 不写死中文（那只是把红挪到英文档）。
                //    🔴 **节点名照旧不进本地化**（上面 `FindChild` / 下面 `Click` 都靠 `names[i]`）。
                //    🆕 **2026-10-19（`A1185` 收尾）就地收掉那条哨兵**：`Account` 那一格（i == 2）原来
                //      走「表里有走表、没有退原版英文」的两步三元式（因为当时 `Settings/Account/Title`
                //      **还没进表**）。**那条键已经在表里了**（`A1185` 那批 25 条）⇒ 现在**五格同一条算式**。
                //      ⛔ 本条不是「红不了」的假断言：键被删 ⇒ `AcTerm` 退英文、而 `Loc.T` 返回**键名本身** ⇒ 红。
                //      ⛔ 判据是**生产代码那一行**（`AcTerm`），不是我们自己的常量。
                string want = Loc.T(tabTitleKeys[i]);
                Check(TextOf(n), want,
                      $"`{names[i]}` 的页签文字（实得「{TextOf(n)}」；期望 = `Loc.T(\"{tabTitleKeys[i]}\")`"
                    + " —— 生产代码走 `Term(键, 原版英文)` 两步漏斗，键在表里时即 `Loc.T`）"
                    + "⛔ 节点名 `" + names[i] + "` 与这行字是两件事 —— 节点名一个都没动）");
            }
            // ③ 🔴 **判别式**（结构上不可能与旧实现同时满足）：
            //    (a) 起排位置 —— 旧式 `BarT + 30 + i×157.68` 会给首键顶 **153.10**；
            //        改回「上对齐 + padTop 13」会给 **136.10**；**4 页档**会给 **222.398**；
            //        **6 键档**会给 **55.794** —— 只有「padTop 13 + MiddleCenter + **5 个键**」才是 **139.096**。
            CheckNear(SettingsWindow.TabTop(0, 5) - 123.10f - 13f, 2.99624f, 0.02f,
                      "🔴 首键顶 = 栏顶 + padTop 13 + **余量一半 2.99624**（`m_ChildAlignment = 5` 那条；上对齐会给 0）"
                    + " —— 🧨 键数改回 4 ⇒ 余量一半变 86.298 ⇒ 红；**改回 6 ⇒ −80.30551 ⇒ 红**"
                    + "（后者正是 `A1186` 裁定 ① 钉住的那一格：栏里只许放原版那 5 个键）");
            CheckNear(SettingsWindow.TabTop(1, 5) - SettingsWindow.TabTop(0, 5), 166.6035f, 0.02f,
                      "🔴 相邻键的顶之差 = **166.6035**（只改高不改缝 ⇒ 157.6835 ⇒ 红；旧代码正是这一档）");
            //    (b) 键上那两层**居中于【键】那一格**：原版实测 `Icon` 中心 = `Label` 中心 = 设计 **424.06**
            //        （屏幕 `477.65`）。旧代码居中的是整条栏（417.31 ⇒ 屏幕 471.58，差 6.1px ⇒ 红）。
            {
                var lbQ = FindChild(Bar(root), names[0]) != null
                        ? FindChild(Bar(root), names[0]).GetComponentInChildren<Label>() : null;
                float lbCx = lbQ != null ? lbQ.transform.position.x * 108f + 960f : -1f;
                CheckNear(lbCx, 960f + (424.06f - 960f) * SettingsWindow.RootScale, 0.5f,
                          "🔴 键上那行字**居中于【键】**（原版实测中心 = 设计 424.06 ⇒ 屏幕 477.65）"
                        + " —— 居中于整条栏会给 471.58（旧代码正是这一档）");
            }
            // 🔴 **2026-10-19（A1175 → A1183 → `A1186` 裁定 ①）就地更正（铁律 5）**：这一句原来写
            //    「`Account` / `Support` 两个键**不建**」→ 后来只断「`Support` 仍然不建」。
            //    **两个键现在都建了**（`Account` = `A1175`、`Support` = `A1183`），而且栏里就是这两格
            //    ⇒ 改断「两个都在」。⛔ 别再退回旧口径（那会让建好的键静默消失也没人管）。
            //    🔴 同一句里顺手钉住裁定 ① 的另一半：**`Online` 那一格必须【不在】栏里**
            //    （`A1183` 那一笔曾经把它摆在末位 ⇒ 6 格 ⇒ 原版那 5 格整体上移 41.65px，用户裁定撤掉）。
            CheckTrue(FindChild(Bar(root), "Account") != null && FindChild(Bar(root), "Support") != null,
                      "`Account` 键（A1175）与 `Support` 键（A1183）**都建了** —— 栏里那 5 格 = 原版那 5 个");
            CheckTrue(FindChild(Bar(root), "Online") == null,
                      "🔴 `Online` **不在页签栏里**（`A1186` 裁定 ①：栏里只放原版那 5 个键 ⇒ "
                    + "多一格会把原版那 5 格整体上移 41.65px）—— 它的入口是 `General` 页那颗 `Online Button`；"
                    + "改坏法：把它加回 `BuildTabs` 的 `specs` ⇒ 本条红、上面 `TabTop(0, 5)` 那条也红");

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
            // 🔴 **2026-10-19（A1175 → A1183 → `A1186` 裁定 ①）**：页签数 4 → 5 →（一度 6 → 裁定后回到）**5**，
            //   且 `pages` 的次序必须与 `names` **逐位对齐**（节点的名字不变：音频页那一棵仍叫 `Media Tab`、
            //   第 5 格那一棵叫 `Support Tab`）。
            //   ⚠️ **`Online Tab` 这一棵仍在 `pages` 之外**：它在栏里没有键（裁定 ①）⇒ 它的切页链
            //   由下面 ⑥ 那颗入口钮那条断言验（`Click` 之后比 `win.Current == SettingsTab.Online`）。
            var pages = new[] { "General Tab", "Media Tab", "Account Tab", "Graphics Tab", "Support Tab" };
            for (int i = 0; i < 5; i++)
            {
                Click(Bar(root), names[i]);
                Check(win.Current, (SettingsTab)i, $"点 `{names[i]}` ⇒ 切到第 {i + 1} 页");
                for (int j = 0; j < 5; j++)
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

            // ---------------- 🆕 2026-10-19（A1175）账号页 + 登录弹窗 ----------------
            // 判据（逐值出处）：
            //   · **几何** = `python 工具/menu_rect.py bundle_menus_assets_all "Account Tab" --depth 4
            //     --no-ancestor-scale`（未缩放帧 = 设计 px；本窗其余断言收的也是这一档）；
            //   · **组件 / 词条 / 颜色 / 显隐** = `python -I d:/tmp/wf_w4probe/w4probe.py bundle_menus_assets_all
            //     "Account Tab" 4` + 全量反编译 `AccountTab__*.c` / `BasicLoginWithEmailWindow__*.c`；
            //   · **层 × 出现条件**那六条 = `AccountTab__Refresh.c` 里那 6 处 `SetActive`
            //     （`+0x30` 未登录 · `+0x38` 恒关 · `+0x48` 恒开 · `+0x50` 登录态 · `+0x58` 恒关 · `+0x88` 登录态）。
            // 🔴 **期望值一律字面量**（⛔ 不从 `SettingsWindow.Ac*` / `Lw*` 读 —— 那是被测实现）。
            Section("A1175 账号页（原版 `Account Tab`）：树 / 矩形 / 层×出现条件 / 命中区");
            {
                // 自检**绝不写 `PlayerPrefs`**（`PersistOverride`）+ 收尾**逐值放回**（同本文件其余几处）。
                bool acWasPersist = SettingsWindow.AccountState.PersistOverride;
                bool acWasReg = SettingsWindow.AccountState.Registered;
                string acWasMail = SettingsWindow.AccountState.Email;
                SettingsWindow.AccountState.PersistOverride = true;
                SettingsWindow.AccountState.ResetForTest();      // 出厂态 = **未登录**（下面那一档的前提）

                Click(Bar(root), "Account");
                Check(win.Current, SettingsTab.Account, "点 `Account` ⇒ 切到第 **3** 页（原版页签序里的第三格）");
                var acPage = FindChild(root, "Account Tab");
                CheckTrue(acPage != null && acPage.gameObject.activeSelf, "`Account Tab` 那一棵建出来了、而且开着");

                // ---- ① 树（逐颗点名；名字全是**原版 GO 名**）----
                var acTree = new[]
                {
                    "Tab Title", "Player Id", "Player Id Text", "External Link Icon",
                    "Account Form", "EmailText", "InputEmail", "PasswordText", "InputPassword",
                    "Reset Password", "Forgot Password", "Error Message",
                    "Subscribe Newsletter", "Social Media Links",
                    "Discord Button", "IG Button", "Facebook Button", "Twitter Button", "Youtube Button",
                    "Buttons", "Unregistered Buttons", "Register Button", "Registered Buttons",
                    "Switch Account Button", "Logout Button", "Twitch Button", "Delete Button",
                    "Login Window",
                };
                foreach (var nm in acTree)
                    CheckTrue(FindChild(acPage, nm) != null, $"账号页有 `{nm}`（原版 GO 名）");
                // 🔴 **名字末尾那个空格是原版原文**（`GameObject/Login Button.json` 的 `m_Name` = `"Login Button "`）
                //    ⇒ 这一对是**判别式**：谁「顺手 trim」了 ⇒ 上一句红；谁「顺手补上」了 ⇒ 前一句红。
                var acULogin = FindChild(acPage, "Login Button ");
                // 🔴 **2026-10-19（A1181）就地改：判别式的【作用域】** —— 这里原来是 `FindChild(acPage, …)`
                //   = **整页**，而 `acPage` 含登录弹窗（`BuildLoginWindow(page)`），弹窗里那颗**正好也叫
                //   `Login Button`（无空格）** ⇒ 判别式第二半**恒为 false** = 这条**恒红、零验证力**
                //   （恒红与被测实现对错**无关**）。
                //   ⇒ 收窄到页内那一格：`Unregistered Buttons` 是 `Buttons` 的子件、**不含弹窗**
                //     （弹窗 `Login Window` 是 page 的直接子件）⇒ 原版那两颗各在各自那一格。
                //   ⚠️ **判别力不变**：谁把名字末尾那个空格 trim 掉 ⇒ 这一半当场红。
                var acUnreg = FindChild(acPage, "Unregistered Buttons");
                CheckTrue(acULogin != null && acUnreg != null && FindChild(acUnreg, "Login Button") == null,
                          "`Login Button ` **末尾带一个空格**（原版 `m_Name` 就是 `\"Login Button \"`）"
                        + "，而没有那颗**去掉空格**的同名件（⛔ 别 trim —— `FindChild` 按名字精确匹配）"
                        + "（作用域 = 页里 `Unregistered Buttons` 那一格 —— ⛔ 别改回整页："
                        + "弹窗里那颗也叫 `Login Button`，整页当作用域会被它顶掉）");
                // 弹窗里那一棵（原版 `Account Tab > Login Window`：**内嵌的子树**，不是外链 prefab）
                var lw = FindChild(acPage, "Login Window");
                CheckTrue(lw != null, "`Login Window` 挂在 `Account Tab` 子树里（原版 `m_Father` 实读）");
                foreach (var nm in new[] { "Backgroun filler", "Generic Popup Background", "Mask", "Background fill",
                                           "EmailText", "InputEmail", "PasswordText", "InputPassword",
                                           "Forgot Password", "ErrorMensajeContainer", "Animated Loading Image",
                                           "Cog", "Error Message", "Login Button",
                                           "Generic Close Button Green", "Icon" })
                    CheckTrue(FindChild(lw, nm) != null, $"登录弹窗里有 `{nm}`（原版 GO 名）");

                // ---- ② 关键矩形（设计 px 字面量 → 过 `Screen()`；⛔ 不过 `Screen()` 的锚在 A131 那一段）----
                CheckAtS(FindChild(acPage, "Account Form"), 596.52f, 273.14f, 1516.52f, 685.06f, "`Account Form`");
                // 🔴 **2026-10-19（A1181）就地改（#4/#5）：量法** —— `EmailText` / `PasswordText` 这两颗 Label
                //   被 `AlignLeft` 挪过（`SettingsWindow.cs` 账号页那两句），而 `Label.AlignLeftOn` 把**节点中心**
                //   移到「左沿 + 宽/2」⇒ 拿 `CheckAtS`（比**节点中心**）去量**必红** —— 中心天然不等于原版矩形中心
                //   （这不是实现错，是量法错）。⇒ 改用本窗**已有的** `CheckLeftS`（`:238` 的 doc 就是为这一档写的：
                //   「`AlignLeft` 会把 Label 的节点挪走 ⇒ 不能比中心，要比**左边缘**」）。
                //   ⚠️ 期望值都是**同一组原版矩形、一个字没改**；判别力不减 —— 矩形摆错 ⇒ 左边缘跟着错 ⇒ 红。
                CheckLeftS(FindChild(acPage, "EmailText"), 596.52f, 1056.52f, 261.43f, 321.43f, "`EmailText`");
                // 🔴 **2026-10-19（A1189）**：上面那条**只比 x**（`CheckLeftS` 收了 `y1`/`y2` 没用）
                //   ⇒ 这两颗的**纵向位置与渲染尺度**当场没人管（原委 → `CheckTopBotS` 的 doc）。
                //   两格判据 = 同一组原版矩形（`menu_dump` 的设计 px 字面量），**一个字没改**。
                CheckTopBotS(FindChild(acPage, "EmailText"), 596.52f, 1056.52f, 261.43f, 321.43f, "`EmailText`");
                CheckAtS(FindChild(acPage, "InputEmail"), 596.52f, 320.67f, 1516.52f, 380.67f, "`InputEmail`");
                CheckLeftS(FindChild(acPage, "PasswordText"), 596.52f, 1056.52f, 390.71f, 450.71f, "`PasswordText`");
                CheckTopBotS(FindChild(acPage, "PasswordText"), 596.52f, 1056.52f, 390.71f, 450.71f, "`PasswordText`");
                CheckAtS(FindChild(acPage, "InputPassword"), 596.52f, 450.75f, 1516.52f, 510.75f, "`InputPassword`");
                CheckAtS(FindChild(acPage, "Reset Password"), 1056.52f, 403.43f, 1516.52f, 447.80f, "`Reset Password`");
                CheckAtS(FindChild(acPage, "Forgot Password"), 1056.52f, 403.43f, 1516.52f, 447.80f,
                         "`Forgot Password`（与 `Reset Password` **同矩形** —— 原版就这么叠着、分时出场）");
                CheckAtS(FindChild(acPage, "Error Message"), 596.52f, 524.59f, 1516.52f, 571.69f, "`Error Message`");
                CheckAtS(FindChild(acPage, "Subscribe Newsletter"), 596.52f, 552.99f, 1195.98f, 629.70f, "`Subscribe Newsletter`");
                CheckAtS(FindChild(acPage, "Social Media Links"), 584.92f, 666.74f, 1222.15f, 747.64f, "`Social Media Links`");
                CheckAtS(FindChild(acPage, "Discord Button"), 584.92f, 659.69f, 712.36f, 754.69f, "`Discord Button`");
                CheckAtS(FindChild(acPage, "Youtube Button"), 1094.70f, 667.19f, 1222.15f, 747.19f, "`Youtube Button`");
                CheckAtS(FindChild(acPage, "Buttons"), 972.65f, 690.47f, 1072.65f, 790.47f, "`Buttons`（100×100 的空容器）");
                CheckAtS(FindChild(acPage, "Unregistered Buttons"), 891.25f, 545.27f, 1513.25f, 635.27f, "`Unregistered Buttons`");
                CheckAtS(FindChild(acPage, "Register Button"), 1213.25f, 545.27f, 1513.25f, 635.27f, "`Register Button`");
                CheckAtS(acULogin, 1202.25f, 545.27f, 1502.25f, 635.27f, "`Login Button `（Unregistered 里那颗）");
                CheckAtS(FindChild(acPage, "Registered Buttons"), 596.52f, 805.36f, 1256.52f, 895.36f, "`Registered Buttons`");
                CheckAtS(FindChild(acPage, "Switch Account Button"), 596.52f, 805.36f, 896.52f, 895.36f, "`Switch Account Button`");
                CheckAtS(FindChild(acPage, "Logout Button"), 926.52f, 805.36f, 1226.52f, 895.36f, "`Logout Button`");
                CheckAtS(FindChild(acPage, "Twitch Button"), 909.35f, 805.36f, 1206.66f, 895.36f, "`Twitch Button`");
                CheckAtS(FindChild(acPage, "Delete Button"), 1216.52f, 806.07f, 1516.52f, 896.07f, "`Delete Button`");
                CheckAtS(FindChild(acPage, "Player Id"), 1148.33f, 165.43f, 1515.44f, 204.46f, "`Player Id`");
                // 弹窗那一族
                CheckAtS(lw, 360.36f, 264.65f, 1568.94f, 664.65f, "`Login Window`（1208.58×400）");
                CheckAtS(FindChild(lw, "Backgroun filler"), 372.27f, 274.19f, 1556.31f, 650.43f, "`Backgroun filler`（原版拼写如此）");
                CheckAtS(FindChild(lw, "InputEmail"), 402.52f, 367.67f, 1214.52f, 427.67f, "弹窗 `InputEmail`");
                CheckAtS(FindChild(lw, "InputPassword"), 402.52f, 497.75f, 1214.52f, 557.75f, "弹窗 `InputPassword`");
                CheckAtS(FindChild(lw, "Forgot Password"), 777.02f, 451.74f, 1214.52f, 496.11f, "弹窗 `Forgot Password`");
                CheckAtS(FindChild(lw, "ErrorMensajeContainer"), 402.52f, 576.78f, 1214.52f, 613.51f, "`ErrorMensajeContainer`（西语残留拼写）");
                CheckAtS(FindChild(lw, "Login Button"), 1238.33f, 496.10f, 1547.50f, 556.10f, "弹窗 `Login Button`（309.17×60）");
                CheckAtS(FindChild(lw, "Generic Close Button Green"), 1526.15f, 232.15f, 1601.15f, 307.15f,
                         "`Generic Close Button Green`（75×75，**第二颗关窗钮**）");

                // ---- ③ 「层 × 出现条件」那六条（**两档各断一次** ⇒ 互为判别式）----
                var acReg = FindChild(acPage, "Register Button");
                var acReset = FindChild(acPage, "Reset Password");
                var acForgot = FindChild(acPage, "Forgot Password");
                var acNews = FindChild(acPage, "Subscribe Newsletter");
                var acSwitch = FindChild(acPage, "Switch Account Button");
                var acLogout = FindChild(acPage, "Logout Button");
                var acDelete = FindChild(acPage, "Delete Button");
                var acTwitch = FindChild(acPage, "Twitch Button");
                var acPid = FindChild(acPage, "Player Id");
                System.Func<Transform, bool> on = t => t != null && t.gameObject.activeSelf;
                CheckTrue(!SettingsWindow.AccountState.Registered, "（前提）自检从**未登录**那一档开始（`ResetForTest`）");
                CheckTrue(on(acReg) && !on(acULogin) && on(acSwitch) && !on(acReset) && !on(acForgot) && !on(acNews),
                          "未登录档：`Register Button` 开 · **`Login Button ` 关** · `Switch Account Button` 开 · "
                        + "`Reset Password` 关 · `Forgot Password` 关 · `Subscribe Newsletter` 关"
                        + "（判据 = `AccountTab__Refresh.c` 的 `SetActive(!登录态) / (0) / (1) / (登录态) / (0) / (登录态)`）");
                CheckTrue(!on(acLogout) && on(acDelete) && !on(acTwitch) && !on(acPid) && !on(lw),
                          "**原版 `Refresh` 一个字都不碰**的那几颗停在 prefab 值：`Logout` 关 · `Delete` **开** · "
                        + "`Twitch` 关 · `Player Id` 关 · `Login Window` 关（🧨 谁把它们也按登录态开关 ⇒ 本条红）");

                // 登录态那一档（**注入**，不落盘）：`Register` 关 · `Reset`/`Subscribe` 开 · 其余不变
                SettingsWindow.AccountState.SignIn("selfcheck@example.invalid");
                win.RefreshAccount();
                CheckTrue(on(acReg) == false && on(acReset) && on(acNews) && on(acSwitch) && on(acDelete) && !on(acLogout),
                          "登录档：`Register Button` 关 · `Reset Password` 开 · `Subscribe Newsletter` 开 · "
                        + "而 `Switch Account` / `Delete` **仍开**、`Logout` **仍关**（后三者是「原版不碰」那一族的判别式）");
                // 两颗输入框：登录态下回填邮箱 + 清空密码（原版 `Refresh` 那两句 `set_text`）
                Check(win.AccountEmail != null ? win.AccountEmail.Text : null, "selfcheck@example.invalid",
                      "登录档下 `InputEmail` 回填成已登录的邮箱（原版 `TMP_InputField.set_text`）");
                Check(win.AccountPassword != null ? win.AccountPassword.Text : null, "",
                      "登录档下 `InputPassword` 被清空（原版同一段）");
                Check(TextOf(FindChild(acPage, "Player Id")), "Player ID: " + SettingsWindow.AccountState.PlayerId,
                      "`Player Id` 那行 = 原版 prefab 的 `Player ID: ` 前缀 + 本机模拟 id");

                // ---- ④ 登录弹窗：开 / 关 / 「该藏的时候藏住了」----
                CheckTrue(!win.LoginWindowOpen && !lw.gameObject.activeSelf, "（前提）弹窗出厂关着（原版 `m_IsActive = 0`）");
                // 「该藏的时候藏住了」= **`PointerLayer` 扫不到它**（`HitQuad` 对 inactive 的 quad 返回 null）
                {
                    int live = 0; string who = "";
                    foreach (var b in lw.GetComponentsInChildren<WindowButton>(true))
                        if (PointerLayer.HitQuadForTest(b) != null) { live++; if (who.Length < 60) who += b.name + " "; }
                    CheckTrue(live == 0, $"弹窗**关着**时它子树里 {live} 颗命中区仍然生效（应有 0）"
                                       + (live > 0 ? $"：{who}" : "")
                                       + " —— 🧨 建了却没跟着藏 ⇒ 关着的窗照样吃点击（本仓踩过这个坑）");
                }
                // 点 `Switch Account Button` ⇒ 弹窗亮起来（判据 = `AccountTab__SwitchAccount.c`）
                Click(acSwitch);
                CheckTrue(win.LoginWindowOpen && lw.gameObject.activeSelf,
                          "点 `Switch Account Button` ⇒ `Login Window` 亮起来（原版那一跳是 `WindowsManager.OpenWindow(loginWindow)`）");
                {
                    var lb = FindChild(lw, "Login Button");
                    var wb = lb != null ? FindChild(lb, "Hit") : null;
                    var btn = wb != null ? wb.GetComponent<WindowButton>() : null;
                    CheckTrue(btn != null && PointerLayer.HitQuadForTest(btn) != null,
                              "弹窗**开着**时 `Login Button` 的命中区生效（与上面那条互为判别式）");
                }
                // 弹窗整段**高于本页内容**（否则点弹窗会穿透到底下那一页）
                {
                    var lwQ = FindChild(lw, "Login Button") != null
                            ? FindChild(FindChild(lw, "Login Button"), "bg").GetComponent<ImageQuad>() : null;
                    var pgQ = FindChild(acPage, "Account Form") != null
                            ? FindChild(FindChild(acPage, "Account Form"), "InputEmail").GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(lwQ != null && pgQ != null && lwQ.RenderQueue > pgQ.RenderQueue,
                              $"弹窗内容档 {lwQ?.RenderQueue} **严格高于**本页内容档 {pgQ?.RenderQueue}"
                            + "（分层用渲染队列、不能用 z —— 本仓红线）");
                }
                // `ESC` 先关弹窗（原版那颗 MB 的 `closeOnESC = 1`），**窗不关**
                CheckTrue(win.ESCPressed(), "`ESC` 这一下被弹窗吃掉了（返回 true = 窗不关）");
                CheckTrue(!win.LoginWindowOpen, "…而且它真的关了");
                CheckTrue(win.CurrentState == WindowState.Open, "…窗本身**还开着**");
                // 再开一次、用那颗绿关窗钮关掉
                Click(acSwitch);
                CheckTrue(win.LoginWindowOpen, "（再开一次）弹窗又亮起来了");
                {
                    var cn = FindChild(lw, "Generic Close Button Green");
                    Click(cn);
                    CheckTrue(!win.LoginWindowOpen, "绿关窗钮 ⇒ 弹窗关掉（原版那颗 `closeButton`）");
                }

                // ---- ⑤ 命中区（**原版射线件的矩形**，⛔ 不是可见面）----
                //   弹窗绿关窗钮：吃射线的是**子件 `Icon`**（局部 56.37×54.50），按它自己的
                //   `m_RaycastPadding (-20)⁴` **外扩** ⇒ 96.37 × 94.50 设计 px（⛔ 不是 75×75 的可见面）。
                //
                // 🔴 **2026-10-19（A1181）就地改（#6/#7/#8，**一个根因**）：取命中 quad 的【唯一一个口】。**
                //   `MenuDraw.Hit` 建的是「**一颗裸 `RectTransform` 节点** + **一颗同名子 `ImageQuad`**」
                //   （`new GameObject(name, typeof(RectTransform))` 然后 `ImageQuad.Create(hit, …, "Hit")`）
                //   ⇒ 把 `Hit` **节点**交给 `CheckQuadRectPx`（头一句就是 `t.GetComponent<ImageQuad>()`）
                //   **必得 null** ⇒ 这三条原来**恒红、零验证力**（打印的是归零的 `0.00×0.00`，
                //   恒红与被测实现对错**无关** —— 与 `:910` 那条走 `PointerLayer` 的绿断言正好互证）。
                //   ⇒ 取 **`Hit` 节点子树里那颗 `ImageQuad`**（`GetComponentInChildren<ImageQuad>(true)`）。
                //   🔴 **`true` 那个参数是【必须】的，⛔ 别去掉**：量 `Register Button` 那一颗时它**正关着**
                //     （③ 的登录档 `Refresh` 把它 `SetActive(0)` —— 上面「登录档…`Register Button` 关」
                //     那条断言刚刚确认过）⇒ 走**生产**那个口 `PointerLayer.HitQuadForTest`
                //     （`HitQuad` 带 `isActiveAndEnabled` 闸）会**当场 null**、本格照旧红。
                //     本格断的是「**建出来的**命中区几何」，「它今天开没开」是另一件事（归 ③ 那一族）。
                //   **判别力**（三种都红）：命中 quad **没建** / **建到别处去了（子树里取不到）** / **矩形摆错**。
                System.Func<Transform, Transform> hitQuadOf = hit =>
                {
                    var hq = hit != null ? hit.GetComponentInChildren<ImageQuad>(true) : null;
                    return hq != null ? hq.transform : null;
                };
                Click(acSwitch);
                {
                    var cn = FindChild(lw, "Generic Close Button Green");
                    var hit = cn != null ? FindChild(cn, "Hit") : null;
                    CheckQuadRectS(hitQuadOf(hit), null, 1535.46f - 20f, 242.40f - 20f, 1591.83f + 20f, 296.90f + 20f,
                                   "弹窗关窗钮的命中区 = 子件 `Icon` 矩形外扩 20（`m_RaycastPadding = (-20)⁴`，"
                                 + "负 = **外扩**）⇒ 设计 96.37×94.50，⛔ 不是可见面 75×75");
                    var lb2 = FindChild(lw, "Login Button");
                    CheckQuadRectS(hitQuadOf(lb2 != null ? FindChild(lb2, "Hit") : null), null,
                                   1238.33f, 496.10f, 1547.50f, 556.10f, "弹窗 `Login Button` 的命中区 = 它自己的矩形");
                }
                {
                    var hit = FindChild(acReg, "Hit");
                    CheckQuadRectS(hitQuadOf(hit), null, 1213.25f, 545.27f, 1513.25f, 635.27f, "`Register Button` 的命中区");
                }
                CheckTrue(win.ESCPressed(), "（收尾）把弹窗关掉（`ESC`）");
                CheckTrue(!win.LoginWindowOpen, "…弹窗关掉了");

                // ---- ⑥ 收尾：逐值放回 + 切回**页签循环收尾时的那一页** ----
                SettingsWindow.AccountState.RestoreForTest(acWasReg, acWasMail);
                SettingsWindow.AccountState.PersistOverride = acWasPersist;
                // 🔴 **2026-10-19（`A1186` 裁定 ①）就地改**：原来收尾切的是 `Online`（= 当时那个切页循环的末位）。
                //   栏里现在是**原版那 5 格** ⇒ 循环收尾停在 **`Support`**（第 5 格）⇒ 收尾跟着它
                //   （本条的语义是「放回本段进来之前的现状」，不是「回联机页」）。
                //   ⛔ 别退回 `Click(Bar(root), "Online")` —— 栏里没有那一格了，那会只报一句「点击区不在」。
                Click(Bar(root), "Support");
                Check(win.Current, SettingsTab.Support,
                      "（收尾）切回 `Support` 页 —— 与本段进来之前的现状一致（= 上面那个切页循环的末位）");
            }

            // ---------------- 🆕 2026-10-17：中英双语基础设施（`Core/Loc.cs`）----------------
            // 判据全文 = `资料/待办判据_卡面卡池与双语.md` §23（用户 2026-09-28 立项：「游戏的各个地方都做
            // 中文和英文两个语言」）；原版侧的四条硬判据（枚举取值 / `TMP_Dropdown` / 词条名 / 12 项）
            // 逐条写在 `Core/Loc.cs` 的文件头。
            // ⚠️ **本节要动 `PlayerPrefs`**（「落盘 / 回读」那条链是任务明确要求的第 ③ 条）⇒ 开头把
            //    盘上与内存都记下来、收尾**逐值放回**（⛔ 不许把玩家的真设置留在自检改过的状态上）。
            Section("中英双语 ①：语言表 / `T()` / 当前语言（`Core/Loc.cs`）");
            {
                // ⛔ 期望值一律**字面量**（不从被测实现里读 `Loc.PrefKey` 当期望 —— 那是自证）
                const string PrefKeyLiteral = "Language";
                var origLang = Loc.Current;
                int origPref = PlayerPrefs.GetInt(PrefKeyLiteral, int.MinValue);   // int.MinValue = 盘上没这个键
                string en, zh = null, jp = null, miss = null;

                Check(Loc.Languages.Length, 12, "下拉列 **12** 项（= 原版 `AvailableLanguages` 的成员数）");
                Check((int)Loc.Languages[0], 0, "第 1 项 `English` 的枚举值 = **0**（原版 `AvailableLanguages.cs` 实读）");
                Check((int)Loc.Languages[11], 110, "第 12 项 `Chinese` 的枚举值 = **110**（⛔ 不是 11 —— 原版是 0/10/…/110）");
                CheckTrue(Loc.EntryCount > 10, $"语言表非空（{Loc.EntryCount} 条词条）");
                // 键名照原版 `Localize.mTerm`（抽查三条：本页标题 / 12 项语言名 / 与对战那扇共用的那条）
                CheckTrue(Loc.HasEntry("Settings/General/Title")
                          && Loc.HasEntry("MainMenu/Settings/LanguageName/Chinese")
                          && Loc.HasEntry("MainMenu/Settings/ButtonLabel/SelectLanguage"),
                          "键名照原版 `Localize.mTerm`（抽查 `Settings/General/Title` · "
                        + "`MainMenu/Settings/LanguageName/Chinese` · `MainMenu/Settings/ButtonLabel/SelectLanguage`）");
                // 🔴 与**对战那扇**共用的那条 —— 两处各是各的常量，值必须相等（同一条词条挂两个窗）
                Check(SettingsPanel.LangLabelTermKey, "MainMenu/Settings/ButtonLabel/SelectLanguage",
                      "对战那扇（`Battle/SettingsPanel.cs`）的语言标签键 = 与主菜单**同一个字符串**");

                Loc.PersistOverride = true;         // ②③⑤ 都不写盘（盘上那条链留给 ⑥ 单独验）
                Loc.SetLanguage(AvailableLanguages.English);
                en = Loc.T("Settings/General/DisableBots");
                Loc.SetLanguage(AvailableLanguages.Chinese);
                zh = Loc.T("Settings/General/DisableBots");
                Check(en, "Disable Bots", "英文那一列 = 原版 TMP 的 `m_text` **原文逐字符**（`Disable Bots`）");
                CheckTrue(!string.IsNullOrEmpty(zh) && zh != en,
                          $"中文那一列与英文**不一样**（实得「{zh}」）—— 两条合起来才说明表真分了两列");

                // ④ 选到**本地没有文案**的语言 ⇒ 回退英文 + **出声**（⛔ 不是悄悄回退）
                int fb0 = Loc.FallbackCount;
                var fbLogs = CaptureLogs(() =>
                {
                    Loc.SetLanguage(AvailableLanguages.Japanese);
                    jp = Loc.T("Settings/General/DisableBots");
                });
                Check(jp, "Disable Bots", "选到 `Japanese`（本地只有中/英两套文案）⇒ 取到的仍是**英文**那一列");
                CheckTrue(Loc.FallbackCount > fb0, $"…而且**记账了**（`Loc.FallbackCount` {fb0} → {Loc.FallbackCount}）");
                bool fbLogged = false;
                foreach (var m in fbLogs) if (m != null && m.Contains("回退英文")) { fbLogged = true; break; }
                CheckTrue(fbLogged, "…而且**出声了**（日志里有一条写明「回退英文」—— 判据：不许静默）");

                // ⑤ 表里没有的键 ⇒ 返回**键名本身** + 出声（画出来就看得见，⛔ 不是空串）
                int miss0 = Loc.MissingCount;
                // 🔴 **2026-10-18（第五轮）补的这一行**：`Loc.T` 缺键那声 `LogWarning` 从第四轮起**按键去重**
                //   （同一条键只出声一次，见 `Core/Loc.cs` 的 `_warnedMissing`）⇒ 下面那条 `missLogged`
                //   原本隐含了「**本进程里这个键还没被问过**」这个前提。补这一行让语义**与跑了几遍无关**
                //   （⛔ 只清去重表：不动 `PlayerPrefs`、不动 `MissingCount`、不改「缺键要出声」这条语义本身）。
                Loc.ResetMissingWarnedForTest();
                var missLogs = CaptureLogs(() => { miss = Loc.T("No/Such/Key/___"); });
                Check(miss, "No/Such/Key/___", "表里没有的键 ⇒ 返回**键名本身**（那一行画出来一眼看得见）");
                CheckTrue(Loc.MissingCount == miss0 + 1 && Loc.LastMissingKey == "No/Such/Key/___",
                          "…而且**记账了**（`Loc.MissingCount` +1、`LastMissingKey` 就是它）");
                bool missLogged = false;
                foreach (var m in missLogs) if (m != null && m.Contains("没有")) { missLogged = true; break; }
                CheckTrue(missLogged, "…而且**出声了**");

                // ⑥ 落盘 / 回读（**这一条真要写盘** ⇒ 键名用字面量、收尾放回）
                Loc.PersistOverride = false;
                Loc.SetLanguage(AvailableLanguages.English);
                Check(PlayerPrefs.GetInt(PrefKeyLiteral, int.MinValue), 0,
                      "落盘：`PlayerPrefs[\"Language\"]` = **0**（= `English`；⛔ 这里用的是**字面量键名**，不是 `Loc.PrefKey`）");
                Loc.ReloadForTest();                       // = 模拟「重开游戏」（重读盘）
                Check(Loc.Current, AvailableLanguages.English, "回读：重读 `PlayerPrefs` 之后 `Loc.Current` = `English`");
                Loc.SetLanguage(AvailableLanguages.Chinese);
                Check(PlayerPrefs.GetInt(PrefKeyLiteral, int.MinValue), 110,
                      "再换到 `Chinese` ⇒ 盘上是 **110**（原版枚举取值）—— 与上一条合起来才说明**不是恒等于一个值**");
                Loc.ReloadForTest();
                Check(Loc.Current, AvailableLanguages.Chinese, "…回读也是 `Chinese`");

                // 收尾：盘上与内存**逐值放回**自检前那一档
                if (origPref == int.MinValue) PlayerPrefs.DeleteKey(PrefKeyLiteral);
                else PlayerPrefs.SetInt(PrefKeyLiteral, origPref);
                PlayerPrefs.Save();
                Loc.PersistOverride = false;
                Loc.RestoreForTest(origLang);
                CheckTrue(Loc.Current == origLang, $"收尾：语言放回自检前那一档（{origLang}；⛔ 自检不许改玩家的真设置）");
            }

            // ---------------- 🆕 2026-10-17：General 页（`Shell/SettingsWindow.cs`）----------------
            // 逐值判据 = `Shell/SettingsWindow.cs` 那组 `Gen*` 常量（每条都有自己的反算算式）。
            // ⚠️ 原版 `GeneralTab` 那三颗开关点下去在我们这边**只存值**（消费者在服务器/匹配那侧）⇒
            //    自检只断「值翻了、字没变、不写盘」，**不断它产生了别的效果**（那是没有的事）。
            Section("中英双语 ②：General 页（原版 `General Tab`：语言下拉 / 三颗开关 / 版本号 / 两颗钮）");
            Click(Bar(root), "General");
            var gtab = FindChild(root, "General Tab");
            CheckTrue(gtab != null, "`General Tab` 那一页建出来了（下面这一片才有对象可量）");
            if (gtab == null)
                CheckTrue(false, "General 页**没建出来** ⇒ 这一节其余断言**全跳过了**"
                               + "（早退，免得 20 条连锁红混在里面 —— 这是判据，⛔ 不是「忽略」）");
            if (gtab != null)
            {
                CheckAtS(gtab, SettingsWindow.TabsL, SettingsWindow.TabsT, SettingsWindow.TabsR, SettingsWindow.TabsB,
                         "General 页的根矩形 = `Tab Content`");

                // ① 版本号（原版 `VersionText`：右上角那一格 · fs28 · **Right/Middle**）
                var verNode = FindChild(gtab, "VersionText");
                CheckTrue(verNode != null, "`VersionText` 建出来了");
                if (verNode != null)
                {
                    Check(TextOf(verNode), "v" + Application.version,
                          "版本号 = **`\"v\" + Application.version`**（= 原版 `GeneralTab__OnSetup.c` 那句 `String.Concat(\"v\", 版本)` 的等价物）");
                    var verLb = verNode.GetComponentInChildren<Label>();
                    var vs = SettingsWindow.Screen(SettingsWindow.GenVerL, SettingsWindow.GenVerT,
                                                   SettingsWindow.GenVerR, SettingsWindow.GenVerB);
                    float rightPx = verLb != null ? verLb.transform.position.x * 108f + 960f + verLb.WorldW * 108f * 0.5f : -1f;
                    CheckTrue(Mathf.Abs(rightPx - vs.x2) <= 2f,
                              $"…而且**右对齐**到原版那一格的右沿（实得 {rightPx:F1}，应为 {vs.x2:F1}）—— 原版是 `Right/Middle`");
                }

                // ② 语言那一行（原版 `Language Selector` → `LanguagesDropdown` → `Label` / `Arrow`）
                float selB = SettingsWindow.GenSelT + SettingsWindow.GenSelH;
                var row = FindChild(gtab, "Language Selector");
                CheckAtS(row, SettingsWindow.GenL, SettingsWindow.GenSelT, SettingsWindow.GenR, selB,
                         "`Language Selector` 那一行");
                var fldNode = row != null ? FindChild(row, "LanguagesDropdown") : null;
                CheckRectS(fldNode != null ? FindChild(fldNode, "bg") : null,
                           SettingsWindow.GenL, SettingsWindow.GenSelT, SettingsWindow.GenFieldR, selB,
                           "下拉框底图（原版 400.70×59.33 设计 px · 图 `40K_dropdown_field_closed`）");
                var capNode = fldNode != null ? FindChild(fldNode, "Label") : null;
                Check(TextOf(capNode), Loc.LanguageName(Loc.Current),
                      "框里那行字 = **当前语言名**（原版 `TMP_Dropdown` 的 caption 就是这个语义）");
                CheckTrue(fldNode != null && FindChild(fldNode, "Arrow") != null,
                          "框右端那个箭头建出来了（原版 `LanguagesDropdown > Arrow`）");
                var selNode = row != null ? FindChild(row, "SelectLanguageText") : null;
                Check(TextOf(selNode), Loc.T("MainMenu/Settings/ButtonLabel/SelectLanguage"),
                      "左边那颗标签 = `Loc.T(\"MainMenu/Settings/ButtonLabel/SelectLanguage\")`");

                // ③ 三颗开关（原版 `Checkboxes`：行高 75.641 · 步进 80.641 —— 与图像页那一族同值）
                var toggleRows = new[] { "Disable Bots", "Disable Notifications", "Touch Input" };
                for (int i = 0; i < toggleRows.Length; i++)
                {
                    float t = SettingsWindow.GenChkT + i * SettingsWindow.ChkRowStep;
                    var n = FindChild(gtab, toggleRows[i]);
                    CheckAtS(n, SettingsWindow.GenL, t, SettingsWindow.GenR, t + SettingsWindow.ChkRowH,
                             $"勾选行 `{toggleRows[i]}`（原版 `Checkboxes` 第 {i + 1} 行）");
                    if (n == null) continue;
                    CheckTrue(n.GetComponentInChildren<Label>() != null && !string.IsNullOrEmpty(TextOf(n)),
                              $"`{toggleRows[i]}` 那一行**有字**（空标签 = 玩家看到一颗没有说明的开关）");
                    CheckTrue(FindChild(n, "Toggle") != null && FindChild(n, "CheckMark") != null,
                              $"`{toggleRows[i]}` 的勾选框与勾两层都建出来了（原版 `Toggle` + `CheckMark`）");
                }
                // ③-b 点一下那三颗：**值真的翻**，而且**互不连带**
                //     （`PersistOverride` 挡住写盘 —— 自检不许动玩家的真设置；这一段只验交互与状态）
                {
                    var hits = new[] { FindChild(gtab, toggleRows[0]), FindChild(gtab, toggleRows[1]), FindChild(gtab, toggleRows[2]) };
                    bool b0 = SettingsWindow.GeneralFlags.DisableBots, n0 = SettingsWindow.GeneralFlags.DisableNotifications, t0 = SettingsWindow.GeneralFlags.TouchInput;
                    SettingsWindow.GeneralFlags.PersistOverride = true;
                    SettingsWindow.GeneralFlags.ResetForTest();                 // 三格清零（⛔ 不动盘）
                    Click(hits[0], "HitBox");
                    Check(SettingsWindow.GeneralFlags.DisableBots, true, "点 `Disable Bots` 的**勾选框** ⇒ 那一格翻成 true");
                    Click(hits[0], "HitLabel");
                    Check(SettingsWindow.GeneralFlags.DisableBots, false, "…点那一行的**文字**也翻（原版那颗 `Label` 的 TMP 也是 `raycastTarget = 1`）");
                    Click(hits[1], "HitBox");
                    Check(SettingsWindow.GeneralFlags.DisableNotifications, true, "点 `Disable Notifications` ⇒ **那**一格翻（三颗各归各的）");
                    Check(SettingsWindow.GeneralFlags.DisableBots, false, "…而且**没有连带**把 `Disable Bots` 一起翻");
                    Click(hits[2], "HitBox");
                    Check(SettingsWindow.GeneralFlags.TouchInput, true, "点 `Touch input` ⇒ 那一格翻（三颗都真的接上了）");
                    SettingsWindow.GeneralFlags.RestoreForTest(b0, n0, t0);      // 内存放回原值
                    SettingsWindow.GeneralFlags.PersistOverride = false;
                }

                // ④ 底下两颗钮（原版 `Bottom Buttons`：300×90 · 间距 40）
                CheckAtS(FindChild(gtab, "Redeem Code"), SettingsWindow.GenL, SettingsWindow.GenBtnT,
                         SettingsWindow.GenL + SettingsWindow.GenBtnW, SettingsWindow.GenBtnT + SettingsWindow.GenBtnH,
                         "`Redeem Code` 钮");
                CheckAtS(FindChild(gtab, "Close Game Button"), SettingsWindow.GenBtn2L, SettingsWindow.GenBtnT,
                         SettingsWindow.GenBtn2L + SettingsWindow.GenBtnW, SettingsWindow.GenBtnT + SettingsWindow.GenBtnH,
                         "`Close Game Button` 钮（原版 `Exit Game`）");
                Check(TextOf(FindChild(gtab, "Redeem Code")), Loc.T("Settings/General/RedeemCode"),
                      "`Redeem Code` 上那行字 = 词条 `Settings/General/RedeemCode`");
                Check(TextOf(FindChild(gtab, "Close Game Button")), Loc.T("MainMenu/Settings/ButtonLabel/Exit_Game"),
                      "`Exit Game` 上那行字 = 词条 `MainMenu/Settings/ButtonLabel/Exit_Game`");

                // ⑤ 🔴 **切一下语言 ⇒ 屏幕上的字真的变了**（第 ② 条验收 = 拿一个已知键验）
                //    🔴 **2026-10-17（A862）换路**：这里原来是「点一下框 = 换下一个」（`CycleLanguage`，
                //      一条**已知偏离**）。现在框接的是 `ToggleLangList` ⇒ 必须**点开列表、再点那一行** ——
                //      这正是原版的链：`OnPointerClick → Show()` → `OnSelectItem:1247 → value = i + Hide()`。
                //    ⛔ 不是直调 `Loc.SetLanguage`（那样只能证明 `Loc` 有用，证不了「界面接了」）。
                //    🔴 **两态自己定死、不靠玩家现在的设置**：先把语言落回 `Chinese`，再**点第 0 行**
                //    （声明序 0 = `English`）—— 这两档隔着中/英那条边界，字**必然**不同；
                //    若顺着玩家当前的档挑，可能从 `English` 换到 `Spanish`（**两边都回退英文**）⇒
                //    「字变了没有」那一条会**假红**（`Loc.T` 的回退规则本来就该让它们一样）。
                //    ⚠️ 这一下**不改盘**（`PersistOverride` 挡住）—— 盘上那条链已在上一节单独验过。
                {
                    var langBefore = Loc.Current;
                    Loc.PersistOverride = true;
                    Loc.SetLanguage(AvailableLanguages.Chinese);
                    if (SettingsWindow.Instance != null) SettingsWindow.Instance.RefreshTexts();
                    string capBefore = TextOf(capNode), rowBefore = TextOf(FindChild(gtab, "Disable Bots"));
                    CheckTrue(!string.IsNullOrEmpty(rowBefore) && rowBefore != "Disable Bots",
                              $"前置：落回 `Chinese` 之后那一行是中文那一列（「{rowBefore}」）");
                    // ⑤-a 点框 ⇒ **开列表**
                    Click(row, "LanguageHit");
                    CheckTrue(win.LangListOpen, "点下拉框 ⇒ **那 12 行列表开出来了**（原版 `OnPointerClick → Show()`）");
                    CheckTrue(win.LangRowCount == 12, $"…列表里 **12** 行（实得 {win.LangRowCount}）");
                    // ⑤-b 点第 0 行（= `English`）⇒ 选中 + **收起**
                    Click(win.LangRowHit(0));
                    Check(Loc.Current, AvailableLanguages.English,
                          "点第 0 行 ⇒ 语言 = `English`（原版 `OnSelectItem`：行号 = 兄弟序 − 1 ⇒ `value = 0`）");
                    CheckTrue(!win.LangListOpen, "…而且**当场收起了**（原版 `OnSelectItem` 末尾就是 `Hide()`）");
                    CheckTrue(TextOf(capNode) != capBefore,
                              $"…框里那行字**当场变了**（「{capBefore}」→「{TextOf(capNode)}」）");
                    Check(TextOf(FindChild(gtab, "Disable Bots")), Loc.T("Settings/General/DisableBots"),
                          "…那一行的标签 = **当前语档那一列**（此刻刚点完第 0 行 ⇒ `English`；⛔ 期望值走 `Loc.T` —— "
                        + "原来写死 `\"Disable Bots\"`，与本块 `:816` / `:818` 那两条同族不同形）");
                    CheckTrue(TextOf(FindChild(gtab, "Disable Bots")) != rowBefore,
                              $"🔴 …而且**与切换前真的不同**（「{rowBefore}」→「{TextOf(FindChild(gtab, "Disable Bots"))}」）"
                            + " —— 这就是第 ② 条「切换后文案真的变了」");
                    Check(TextOf(FindChild(gtab, "Redeem Code")), Loc.T("Settings/General/RedeemCode"),
                          "…底下那颗钮上的字也重设了（整页走同一条 `RefreshTexts`）");
                    Check(TextOf(FindChild(Bar(root), "General")), Loc.T("Settings/General/Title"),
                          "…页签上那行字也跟着变（`RefreshTexts` 同时刷页签 —— 它不在这一页的树里）");
                    Check(TextOf(selNode), Loc.T("MainMenu/Settings/ButtonLabel/SelectLanguage"),
                          "…那颗 `Select Language` 标签 = 切换后当前语言那一列");
                    // 收尾：**内存放回自检开始前那一档**（盘上这一下没动过 —— `PersistOverride` 挡着）
                    Loc.SetLanguage(langBefore);
                    Loc.PersistOverride = false;
                    if (SettingsWindow.Instance != null) SettingsWindow.Instance.RefreshTexts();
                    CheckTrue(Loc.Current == langBefore, $"收尾：语言放回本节开始前那一档（{langBefore}）");
                }

                // ⑥ 🆕 **2026-10-19（`A1186` 裁定 ②）**：`Online` 页那颗**入口钮** ——
                //    页签栏里已经没有 `Online` 那一格了（裁定 ①）⇒ **这是进联机页的唯一 UI 入口**。
                //    🔴 **判据分两半**：几何那半是**我们挑的**（`Shell/SettingsWindow.cs` 的 `GenOnlineL`
                //    那条 doc 里写了「为什么摆这一格」、还列了被否掉的两处空地）⇒ 期望值照本窗对
                //    **自加元素**的既有写法走 `SettingsWindow.GenOnline*`（⛔ 原版没有这一颗，没有原版字段值可抄）；
                //    **而「不压原版元素」那半用【渲出来的矩形】断**（下面第二条）—— 那才是「为什么能放这里」的判据。
                //    ⚠️ 「点它切页」那一条必须放在**本节最后**：它会切到 `Online` 页，而本节上面几条都要留在
                //      General 页上量 ⇒ 收尾必须切回来（后面几节也都在 General 页上开工）。
                {
                    var onBtn = FindChild(gtab, "Online Button");
                    CheckTrue(onBtn != null, "`General` 页上那颗 `Online` 入口钮建出来了");
                    if (onBtn == null)
                        CheckTrue(false, "`Online` 入口钮**没建出来** ⇒ 下面「点它切页」那条等于没验（⛔ 不是忽略）");
                    else
                    {
                        CheckAtS(onBtn, SettingsWindow.GenOnlineL, SettingsWindow.GenBtnT,
                                 SettingsWindow.GenOnlineR, SettingsWindow.GenBtnT + SettingsWindow.GenBtnH,
                                 "`Online Button` 的矩形（**我们挑的** —— 顶/高照抄原版 `Bottom Buttons` 那一行、"
                               + "左沿留 `GenBtnGap` 40、右沿取本页内容列右沿 `GenR`，见 `GenOnlineL` 那条 doc）");
                        // 🔴 **「不压原版任何元素」**：量**渲出来的**矩形，与原版那颗钮的**渲染**矩形比
                        //   —— 判据是**不相交**，⛔ 不是拿我们自己的常量比（拿常量比 = 自证）。
                        float eLx, eTy, eRx, eBy, cLx, cTy, cRx, cBy;
                        var closeBtn = FindChild(gtab, "Close Game Button");
                        if (RectOf(onBtn, out eLx, out eTy, out eRx, out eBy)
                            && RectOf(closeBtn, out cLx, out cTy, out cRx, out cBy))
                            CheckTrue(eLx >= cRx - 0.5f,
                                      $"🔴 入口钮在**原版两颗钮的右边**、两颗不相交（新钮左沿 {eLx:F1} ≥ "
                                    + $"`Close Game Button` 右沿 {cRx:F1}）—— 原版 `Bottom Buttons` 从左起排两颗 300 宽"
                                    + " ⇒ 设计 px 里 `x > 1236.52` 一直是空的；改坏法：把它挪到 `GenL` 上"
                                    + "（= 与 `Redeem Code` 同格）⇒ 本条红、而 ① 那条矩形断言照样绿");
                        else
                            CheckTrue(false, "（前提）`Online Button` / `Close Game Button` 的渲染矩形量得到"
                                           + " —— 量不到 = 上面那条等于没验（`RectOf` 返 false）");
                        Check(TextOf(onBtn), Loc.T("Settings/Online/Title"),
                              "钮上那行字 = 词条 `Settings/Online/Title`（= 那一页的页标题，也正是它原来"
                            + "在页签栏上那行字 —— 撤掉页签之后**原样**搬到钮上，⛔ 没另造文案）");
                        // 点它 ⇒ **真的切到 `Online` 页**（判据 = `OpenTab` 的两件事：页号 + 那一棵 `activeSelf`）
                        Click(onBtn, "Hit");
                        Check(win.Current, SettingsTab.Online,
                              "点入口钮 ⇒ **切到 `Online` 页**（`SettingsTab.Online`）—— 改坏法：把那句 "
                            + "`OpenTab(SettingsTab.Online)` 改成只切视觉/或删掉 ⇒ 目测看不出、本条红");
                        CheckTrue(FindChild(root, "Online Tab") != null
                                  && FindChild(root, "Online Tab").gameObject.activeSelf,
                                  "…而且 `Online Tab` 那一棵**真的开着**（只改 `Current` 不改 `activeSelf` ⇒ 本条红）");
                        CheckTrue(FindChild(root, "General Tab") != null
                                  && !FindChild(root, "General Tab").gameObject.activeSelf,
                                  "…`General Tab` 同时**关掉了**（切页是互斥的 —— 同上面那个切页循环）");
                        // 收尾：切回 `General` —— 本节后面（以及后面几节）都假定停在 General 页上。
                        Click(Bar(root), "General");
                        Check(win.Current, SettingsTab.General,
                              "（收尾）切回 `General` 页 —— 后面几节都在这一页上开工");
                    }
                }
            }

            // ---------------- 🆕 2026-10-17（A862）：语言下拉的 12 行列表（原版 `LanguagesDropdown > Template`）----------------
            // 判据 = 原版 prefab `Main Menu Settings Window > … > LanguagesDropdown > Template` **逐字段实读**：
            //   RT `RectTransform_-8168062444739330138`：`aMin(0,0.5) aMax(1,0.5) aPos(-2.5,-22)
            //   sizeDelta(-4.9998, **573.9600219726562**) pivot(0.5,1)`；父 = `LanguagesDropdown`（宽 400.666412）
            //   ⇒ 设计 **395.666 × 573.960**、左上 **596.556, 390.667**（= 下拉框左沿 · 框心往下 22）。
            //   子件逐条（全部实读，`menu_dump … "Main Menu Settings Window" --depth 12` 交叉印证）：
            //   · `Viewport` `aMin(0,0) aMax(1,1) sizeDelta(**-17**,0) pivot(0,1)` ⇒ 右沿 = 面板右 − 17；
            //     `Mask` + `Image(UIMask, showGraphic=0)` ⇒ **不画**、只当裁切框。
            //   · `Content` `aMin(0,1) aMax(1,1) sizeDelta(0,41.7226) pivot(0.5,1)`（41.7226 = **模板位**）。
            //   · `Item` `aMin(0,0.5) aMax(1,0.5) sizeDelta(0, **40.8707**)` ⇒ 行高 40.8707、宽 = `Content` 宽。
            //   · `Item Background`（= Toggle 的 `m_TargetGraphic`）`aMin(0,0) aMax(1,1) sizeDelta(0,0)`、
            //     图 **`40K_dropdown_item`**（717×92 · 无九宫 · `m_Type=0`）、色 (0.2863,0.9647,0.6863,1)。
            //   · `Item Checkmark`（= Toggle 的 `graphic`）`aMin/aMax x = 0`、`aPos(10,0)`、20×20、白。
            //   · `Item Label`（= Dropdown `m_ItemText`）`aMin(0,0) aMax(1,1) aPos(5,-0.5)
            //     sizeDelta(**-30,-3**)` ⇒ 行内边距 左20/右10/上2/下1；TMP `m_fontSize **30**`（基准 14、
            //     auto 18~40）、`Left/Middle`、折行、色 (0.783,0.783,0.783,1)。
            //   · `Scrollbar` `aMin(1,0) aMax(1,1) sizeDelta(**20**,0) pivot(1,1)` + `Sliding Area` +
            //     `Handle`（`m_Size 0.9273`）；`ScrollRect.m_MovementType = **2**(Clamped)`、
            //     `m_VerticalScrollbarVisibility = **2**(AutoHideAndExpandViewport)`、spacing −3。
            // 行为判据（本机 ugui 源码 `Runtime/TMP/TMP_Dropdown.cs`，逐行核过 —— 见正本 §三.3 的三条旁证）：
            //   `Show():814` = 克隆 `Template` → 改名 `"Dropdown List"` → 挂到 `Template` 的父下 →
            //   `m_Template.gameObject.SetActive(false)`；`OnSelectItem:1247` = 定 `value` + **末尾 `Hide()`**；
            //   `CreateBlocker:1073-1074` 那颗全屏透明 `Blocker` 的 `onClick → Hide`；`OnCancel:763 → Hide()`。
            Section("A862：语言下拉那 12 行列表（原版 `LanguagesDropdown > Template`）");
            {
                var selRow = gtab != null ? FindChild(gtab, "Language Selector") : null;
                var fldNode = selRow != null ? FindChild(selRow, "LanguagesDropdown") : null;
                var tpl = win.LangTemplateNode;
                CheckTrue(tpl != null, "`Template` 子树建出来了（原版那颗**恒 inactive** 的原型）");
                CheckTrue(tpl != null && tpl.gameObject.activeSelf == false,
                          "★ `Template` **出厂是关着的**（原版 `m_IsActive = 0`；`Show()` 只在实例化那一瞬把它打开）");
                if (tpl != null)
                {
                    CheckTrue(tpl.parent == fldNode,
                              "…而且挂在 `LanguagesDropdown` 下面（原版链 `Language Selector > LanguagesDropdown > Template`）");
                    // 🔴 **2026-10-17（F3 · D3 红⑫）**：原来这里有一条 `CheckRectPx(量 `Template` 下的 `bg`)`
                    //    —— 它**结构上不可能绿**（三条硬证据）：① 上面三行刚刚断言并通过「`Template` 出厂是**关着的**」
                    //    （原版 `m_IsActive = 0` ✓）；② 实现照做（`Shell/SettingsWindow.cs:1232-1235` 的
                    //    `tpl.gameObject.SetActive(false)`，面板底图 `bg` 是它的子件 `:1246`）；
                    //    ③ 量法 `RectOf` **跳过 `!activeInHierarchy` 的 quad**（本文件 `:168`）⇒ 一个 active 的都没有 ⇒ 直接报红。
                    //    **即：照原版做 ⇒ 这一条永远红。** ⇒ 已**挪到列表点开之后**、改量那份**活的克隆体**
                    //    （`win.LangListNode` 下的 `bg` —— 与 `Template` **同格** ⇒ 期望值一个数都不用改）。
                    //    ⛔ **不许**为了让这条绿去让 `Template` 保持激活 —— 那是**反向**偏离原版。
                    //    ⚠️ 锚的职责没丢：本节后面那两条 `（A862 锚）第 0 行 / 第 11 行` 同样是**不过 `Screen()`**
                    //       的手算字面量 ⇒ 本条挪位的净作用只是「**更早报**」变成「**跟着列表一起报**」。
                    var vp = FindChild(tpl, "Viewport");
                    CheckAtS(vp, 596.556f, 390.667f, 975.222f, 964.627f,
                             "`Viewport`（原版 `sizeDelta.x = -17` ⇒ 右沿 = 面板右 − 17）");
                    CheckTrue(win.LangFieldNode != null && fldNode != null && win.LangFieldNode == fldNode,
                              "自检拿到的「列表的父」就是那颗 `LanguagesDropdown`");
                }
                // ③ 🔴 **开 / 关 / 点行 / 点空白 / ESC** —— 全走**真实点击链**（`WindowButton.onClick`）
                var lang0 = Loc.Current;
                Loc.PersistOverride = true;
                CheckTrue(!win.LangListOpen, "前置：**此刻列表是关着的**（`Build()` 建完就是关的）");
                Click(selRow, "LanguageHit");
                CheckTrue(win.LangListOpen, "★ 点框 ⇒ 列表**开**（原版 `OnPointerClick → Show()`）");
                var list = win.LangListNode;
                CheckTrue(list != null && list.name == "Dropdown List",
                          "★ 运行时那份叫 **`Dropdown List`**（原版 `Show():820` 那句改名；⛔ 不是 `Template`）");
                CheckTrue(list != null && list.parent == fldNode,
                          "★ …而且和 `Template` **同级**（原版 `SetParent(m_Template.transform.parent, false)`）");
                CheckTrue(list != null && list.gameObject.activeSelf,
                          "…它是开着的那一份（`Template` 仍关着 —— 两者互不干扰）");
                // 🔴 **2026-10-17（F3 · D3 红⑫）**：**本节的映射锚断言** —— 原来量的是 `Template` 子树里那份
                //    **恒 inactive** 的 `bg` ⇒ 结构上不可能绿（原委见上面 `Template` 那一节里那段注）。
                //    现在量**活的克隆体**（`Dropdown List` 下那份 `bg`）。期望值**一个字都没改** ——
                //    ⛔ 它仍是**手算的屏幕 px 字面量**、**不过 `Screen()` / 不过本窗任何常量**
                //    （= 原版 prefab 的设计矩形 × 根上那层 0.9：596.556→632.90 · 390.667→405.60 ·
                //     992.222→989.00 · 964.627→922.16）⇒ 那张映射写错时，本窗其余几何断言会一起错、**只有它会红**。
                //    ⚠️ 克隆体与 `Template` **同格**（`ShowLangList` 用 `LstL/LstT/LstR/LstB` 建它、
                //       `BuildLangListSubtree` 两处共用同一份 ⇒ 两处写同一条规则才会不一致）。
                CheckRectPx(FindChild(list, "bg"), 632.90f, 405.60f, 989.00f, 922.16f,
                            "（A862 锚）面板底图渲出来 = 原版 prefab 手算的 [632.90,405.60]–[989.00,922.16]"
                          + "（⛔ 这一条的期望值**不过 `Screen()`**：那张映射写错时本窗其余几何断言会一起错、只有它会红）");
                CheckTrue(win.LangRowCount == 12, $"…装了 **12 行**（实得 {win.LangRowCount}）");
                // 行矩形 = `Content` 里按 `i × 40.8707` 往下排（原版 `Show():840-844`：第 0 项在**最上**）
                // ⚠️ `CheckRectPx` 的期望值是**屏幕 px**（× 0.9 之后的），算式写在调用点：
                //    行 0 顶 = 540 + (390.667−540)×0.9 = **405.60**、行高 40.8707×0.9 = **36.78**；
                //    行 11 顶 = 540 + (390.667 + 11×40.8707 − 540)×0.9 = **810.22**；右沿 = 960 + (975.222−960)×0.9 = **973.70**。
                var r0 = win.LangRowBg(0); var r11 = win.LangRowBg(11);
                CheckRectPx(r0 != null ? r0.transform : null, 632.90f, 405.60f, 973.70f, 442.38f,
                            "（A862 锚）第 0 行的底图 = 原版 `Item` 那一格（屏幕高 **36.78** = 40.8707 × 0.9）");
                CheckRectPx(r11 != null ? r11.transform : null, 632.90f, 810.22f, 973.70f, 847.00f,
                            "（A862 锚）第 11 行 = `Content` 里第 11 格（`390.667 + 11 × 40.8707`）"
                          + " —— 与第 0 行合起来才说明**真是 12 行往下排**、不是叠在一处");
                var l0 = win.LangRowLabel(0); var l11 = win.LangRowLabel(11);
                CheckTrue(l0 != null && l11 != null && l0.Text != l11.Text,
                          $"…而且两行的字**不一样**（「{(l0 != null ? l0.Text : "?")}」 / "
                        + $"「{(l11 != null ? l11.Text : "?")}」）");
                // 行底图 = 原版那张（`Item Background` 的 `m_Sprite`）
                CheckTrue(r0 != null && r0.Texture != null && r0.Texture.name == "40K_dropdown_item",
                          "行底图 = `40K_dropdown_item`（原版 `Item Background` 那颗 `Image.m_Sprite`）");
                // 行字号：一条**判别式** —— 原版 `Item Label` 的 `m_fontSize = 30` ⇒ 屏幕 27px
                //（⛔ 不是 caption 那颗的 16.2、也不是勾选框的 37.8 —— 三个都在这扇窗里，容易混）
                // 🔴 **2026-10-19（第十会话 · `A1196`）本条的【口径】换成 `A171` 那一族** ——
                //   原来读的是 `FontPxNow`（**TMP 的收敛值**）。可 `A1181` 给这一颗接上 autosize 之后
                //   （`Shell/SettingsWindow.cs` 那个调用点：`18 / 40 / **14** · 折行 1`），
                //   `FontPxNow` 是**二分收敛结果**、「**停在标称**」那一态；谁哪天把 `min/max`
                //   改一档、或字号资产一换，它就会像 `A171` 当初那样变成**恒红（或恒绿）**
                //  （诊断原委 → `资料/普查产出_第十会话/D2_全跑8红诊断.md` §4「枚举式字号断言」那一格）。
                //  ⇒ 改读**字段** `m_fontSizeBase`（`Label.FontSizeBase`，一律经
                //    `Label.FontSizeToPx` 折成画布 px）—— 同 `A171` 那一节 2026-10-09 定的口径：
                //    **要断「它是什么」读【字段】；要断「它长什么样」读【渲染】。**
                //    `m_fontSizeBase` 才是那条不变式（autosize 关着时 TMP 让 `m_fontSizeBase ≡ m_fontSize`，
                //    `TMP_Text.cs:467`）⇒ 两条入口读到的是同一个口径。
                //  ⚠️ **判别力不减**（原版逐字段实读 `fs **30** · base **14** · auto[18~40]`）：
                //    30 × 0.9 = 27、14 × 0.9 = **12.6** ⇒ 与同窗另外两档（caption `fs18` → 16.2、
                //    行标签 `base36` → 32.4）**照样分得开**；谁把 `autoBasePx` 传回 0 ⇒ 字段退回标称 27 ⇒ 红。
                float l0BasePx = l0 != null ? Label.FontSizeToPx(l0.FontSizeBase) : -1f;
                CheckNear(l0BasePx, 12.6f, 0.35f,
                          "★ 行内字号 = 原版 `Item Label` 的 `m_fontSizeBase **14**` × 0.9 = **12.6px**"
                        + "（读的是【字段】`m_fontSizeBase` —— 本笔给它接上 autosize 之后 `FontPxNow` 是**收敛值**、"
                        + "本来就不该再等于「原版值 × 0.9」，口径同 `A171` 那一节）"
                        + "；同窗另外两档是 16.2 = caption 的 18、32.4 = 行标签的 base 36 —— 三档必须分得开；"
                        + "🧨 改坏法：把 `autoBasePx` 传回 0 ⇒ 字段退回调用方那一档 30 × 0.9 = 27.00 ⇒ 红");
                // ⚠️ **另加一条「实得落在钳位区间里」**（同 `A171` 那一节的 ④）—— **不是**把上面那条放宽：
                //   它挡的是「`min`/`max` 写错档 / 收敛跑到区间外」，两条合起来比原来那条更强。
                if (l0 != null)
                {
                    float l0Now = l0.FontPxNow;
                    float l0Lo = Label.FontSizeToPx(l0.FontSizeMin) - 0.35f;
                    float l0Hi = Label.FontSizeToPx(l0.FontSizeMax) + 0.35f;
                    CheckTrue(l0.AutoSizing && l0Now > 0f && l0Now >= l0Lo && l0Now <= l0Hi,
                              $"…而且这一颗**真的开着 autosize、实得落在钳位区间里**（实得 {l0Now:F2} ∈ "
                            + $"[{l0Lo:F2}, {l0Hi:F2}]；`AutoSizing` = {l0.AutoSizing}；"
                            + "原版 `auto[18~40]` ⇒ 屏幕上 `[16.20, 36.00]`）"
                            + " —— 🧨 少了这一条，把 autosize 关掉（实得回到 27.00）时上面那条照样绿");
                }
                // ④ 🔴 **点第 3 行 ⇒ 选中 + 收起**（`OnSelectItem:1247` 那条链：行号 = 兄弟序 − 1 ⇒ `value = 3`）
                //    两态自己定死：先落到 `Chinese`（声明序 11），点第 3 行 ⇒ 必须变成 `Loc.Languages[3]`。
                Loc.SetLanguage(AvailableLanguages.Chinese);
                if (SettingsWindow.Instance != null) SettingsWindow.Instance.RefreshTexts();
                var want3 = Loc.Languages[3];
                Click(win.LangRowHit(3));
                CheckTrue(!win.LangListOpen, "★ 点某一行 ⇒ **当场收起**（原版 `OnSelectItem:1310` 的 `Hide()`）");
                Check(Loc.Current, want3, "★ …而且选中的是**那一行**（第 3 行 ⇒ 声明序第 3 项 —— ⛔ 不是「下一项」）");
                // ④-b 🔴 **判别式**：同一颗框再点一次，若是老行为（`CycleLanguage`）语言会**再走一格**；
                //      新行为是**只开列表、语言一个字不动**。这一条与 ④ 合起来才分得开两种实现。
                var beforeB = Loc.Current;
                Click(selRow, "LanguageHit");
                Check(Loc.Current, beforeB, "🔴 再点一次框 ⇒ **只开列表、语言不动**（旧实现 `CycleLanguage` 会给下一项 ⇒ 红）");
                CheckTrue(win.LangListOpen, "…而列表是开的");
                // ④-c **点空白收起**（原版那颗全屏 `Blocker` 的 `onClick → Hide`）—— 语言同样不动
                CheckTrue(win.LangBlockerNode != null && win.LangBlockerNode.gameObject.activeSelf,
                          "★ 列表开着时有那颗铺满全屏的 `Blocker`（原版 `CreateBlocker:1009`）");
                Click(win.LangBlockerNode);
                CheckTrue(!win.LangListOpen, "★ **点空白 ⇒ 收起**（原版 `blockerButton.onClick.AddListener(Hide)`）");
                Check(Loc.Current, beforeB, "…而且语言一个字没动（点空白不是选中）");
                CheckTrue(win.LangBlockerNode == null || !win.LangBlockerNode.gameObject.activeSelf,
                          "…`Blocker` 也一起关掉了（否则它会带着队列 3140 盖住后面所有东西 = 静默卡死）");
                // ④-d **ESC ⇒ 先收列表、窗不关**（原版 `TMP_Dropdown.OnCancel:763 → Hide()`）
                Click(selRow, "LanguageHit");
                CheckTrue(win.LangListOpen, "（前置）列表又开出来了");
                bool escUsed = win.ESCPressed();
                CheckTrue(escUsed, "★ ESC：**这一下被列表吃掉了**（`ESCPressed` 返回 true ⇒ 输入层不再往下关窗）");
                CheckTrue(!win.LangListOpen, "★ …列表收起（原版 `OnCancel → Hide()`）");
                CheckTrue(win.IsOpen(), "🔴 …而**窗还开着** —— 不许把 ESC 变成「关掉整个设置窗」");
                // ④-e 收尾：语言放回本节开始那一档（盘上全程没动过 —— `PersistOverride` 挡着）
                Loc.SetLanguage(lang0);
                Loc.PersistOverride = false;
                if (SettingsWindow.Instance != null) SettingsWindow.Instance.RefreshTexts();
                CheckTrue(Loc.Current == lang0, $"收尾：语言放回本节开始前那一档（{lang0}）");
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
            // 🔴 **2026-10-19（波 1b）就地改掉「写死中文当需子串」**：这句 `_flash` 现在**跟着语言走**
            //    （`Shell/SettingsWindow.cs:2070` = `string.Format(Loc.T("Settings/Graphics/Flash/Quality"), QualityName())`）
            //    ⇒ 旧写法 `Contains("画质")` 只在**中文档**成立，英文档那句是 `Quality → …` ⇒ 红。
            //    期望值 = 词条的**固定前缀**（`{0}` 之前那一截，跟语档走）；⛔ 不写死中/英、⛔ 也不写死整句
            //    （`{0}` 是运行期档名）。
            string qFlashHead = TermHead("Settings/Graphics/Flash/Quality");
            CheckTrue(win.Flash != null && win.Flash.StartsWith(qFlashHead),
                      $"点完**有话说**，而且那句是照**当前语档**拼的 `Settings/Graphics/Flash/Quality`"
                    + $"（前缀「{qFlashHead}」；实得「{win.Flash}」；"
                    + "改坏法：`_flash` 写死中文字面量 ⇒ 英文档这一条红）");
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
                    // 🔴 **2026-10-19（波 1b）就地改掉「写死中文当需子串」**：同上（`Shell/SettingsWindow.cs:2396`
                    //    ⇒ `string.Format(Loc.T("Settings/Graphics/Flash/Fps"), FpsText())`）——
                    //    旧写法 `Contains("帧率上限")` 只在中文档成立，英文档那句是 `FPS limit → …`。
                    string fpsFlashHead = TermHead("Settings/Graphics/Flash/Fps");
                    CheckTrue(win.Flash != null && win.Flash.StartsWith(fpsFlashHead),
                              $"改档**有话说**，而且那句是照**当前语档**拼的 `Settings/Graphics/Flash/Fps`"
                            + $"（前缀「{fpsFlashHead}」；实得「{win.Flash}」）");
                    // ⑧ 三个刻度：节点名 / 文字 / 中心 x（期望值 = 原版框中心，行内 x + 114.01，经 0.9）
                    // 🔴 **2026-10-19（波 1b）**：第 3 格的**字**改走词条（`Shell/SettingsWindow.cs:2341`
                    //    `i == 2 ? Loc.T(lkGfxUnlimited) : FpsTickText[i]`）⇒ 期望值第 3 项改读 `Loc.T`。
                    //    `[0]/[1]` 是纯数字 `30`/`60`、**不换**（表 A 明写「不建键」）。
                    //    ⚠️ **节点名照旧**（`SettingsWindow.FpsTickName[i]` = `30 FPS`/`60 FPS`/`Unlimited`，
                    //    下面那条 `FindChild` 靠它 —— ⛔ 别把「节点名」与「这行字」混成一件事）。
                    string[] tickTxt = { "30", "60", Loc.T("Settings/Graphics/UnlimitedFPS") };
                    float[] tickCx = { 852.48f, 1069.29f, 1281.78f };
                    for (int i = 0; i < 3; i++)
                    {
                        var tk = FindChild(slNode, SettingsWindow.FpsTickName[i]);
                        CheckTrue(tk != null, $"刻度 {i + 1} 节点在（名字照原版的 GO 名 `{SettingsWindow.FpsTickName[i]}`）");
                        if (tk == null) continue;
                        CheckTrue(TextOf(tk) == tickTxt[i],
                                  $"刻度 {i + 1} 的字 = `{tickTxt[i]}`"
                                + (i == 2 ? "（= `Loc.T(\"Settings/Graphics/UnlimitedFPS\")`：**第 3 格已接词条**，"
                                          + "中文档印「不限帧」；前两格是纯数字、不换）"
                                          : "（前两格是纯数字，原版也没接词条）"));
                        var tkl = tk.GetComponentInChildren<Label>();
                        CheckNear(tkl != null ? tkl.transform.position.x * 108f + 960f : -999f, tickCx[i], 1.5f,
                                  $"刻度 {i + 1} 的**中心 x** = 原版框中心（行内 x {SettingsWindow.FpsTickX[i]} + 114.01）经 0.9");
                    }
                    // ⑨ 行标题（原版 `Title`：框左沿在行内 +16）
                    var ttl = fpsRow != null ? FindChild(fpsRow, "Title") : null;
                    // 🔴 **2026-10-19（波 1b）**：行标题也接了词条（`Shell/SettingsWindow.cs:2306` 的
                    //    `Func<string> fpsTitleText = () => Loc.T(lkGfxFrameLimit)`）⇒ 期望值走 `Loc.T`
                    //    （旧写法写死英文 `"FPS limit"`：英文档恰好绿、**中文档红**）。
                    //    原版 TMP 的 `m_text` 是西语 `'Límite de FPS'`、没挂 I2 词条 ⇒ EN 列照文件头 ② 自己写。
                    CheckTrue(ttl != null && TextOf(ttl) == Loc.T("Settings/Graphics/FrameLimit"),
                              $"行标题 = `Loc.T(\"Settings/Graphics/FrameLimit\")`（实得「{TextOf(ttl)}」；"
                            + "EN `FPS limit` / ZH `帧率上限`）");
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

            // ---------------- ★ A1092 站点②：`SettingsWindow.UpdateFpsDragAt` 的 x 换算（**必须两态**）----------------
            // 🔴 **缺陷**：`UpdateFpsDrag` 的指针路原来把世界坐标读成 **108 帧**的画布 px（`LayoutSpace.PxX/PxY`），
            //   而它喂进去的 `FpsPressAtCanvas` / `SetFpsFromCanvasX` 吃的是**设计帧**画布 px
            //   （= 先过 `SettingsWindow.Screen()` 把固定的 0.9 烘进设计 px）⇒ 两帧只在 **16:9 重合**
            //   （非 16:9 下读出来的 x 围着画布中心 960 缩放：4:3 ⇒ ×0.75、21:9 ⇒ ×1.3125）。
            //   **2026-10-18（A1092）已改成同一条读口**（`MenuDraw.PixelOfDesign`）。
            // 🔴 **为什么这一节必须两态**：本节每一点在 **16:9 下新旧两种写法逐值相同**
            //   （偏差 ≤ 2.44e-4 px，`资料/普查产出_第六会话/W_外壳量法线_四笔.md:161` 按 float32 逐点算过）
            //   ⇒ **只断 16:9 = 没断**（旧实现照样全绿）。4:3 与 21:9 各挑了一处**旧实现必然给错档**的判点。
            // 🔴 **怎么打进去**：直调 `win.UpdateFpsDragAt(world, pressedThisFrame, held)`——
            //   批处理里 `Mouse.current` **恒 null**，`UpdateFpsDrag` 头两行就早退 ⇒
            //   **那一层换算在 (B) 重构之前没有任何口能被自检驱动**（这正是它从 `A990②` 潜伏到 `A1092` 的原因）。
            // ⚠️ 夹具形状照 `Editor/ShellScene.cs:1699-1786` 那处已跑过的两态：改 `LayoutSpace.Cam.aspect`
            //   → **重建** → 读完 `finally` 还原；并断一条**前提**「`|VisibleWidth/DesignWidth − 1| > 0.1`」
            //   （前提不成立 = 这一档其实是 16:9 ⇒ 本节没有鉴别力 ⇒ **当场红**，⛔ 不许静默）。
            // ⚠️ 与 `A202①` 那节**不重复**：那一节打的是 `FpsPressAtCanvas` 自己的**按下分支**（画布 px 直调），
            //   本节补的是**它上面那一层**（世界坐标 → 画布 px）—— 也就是「拖动那一支」。
            Section("★ A1092 站点②：`UpdateFpsDragAt` 的 x 换算 = 设计帧（两态：16:9 对照 + 4:3 / 21:9）");
            {
                var a1092Cam = LayoutSpace.Cam;
                CheckTrue(a1092Cam != null,
                          "（前提）`LayoutSpace.Cam` 在（`VisibleWidth` 由它给；不在 ⇒ 本节等于没验）");
                if (a1092Cam != null)
                {
                    var a1092Back = a1092Cam.aspect;
                    int a1092F0 = Application.targetFrameRate, a1092I0 = win.FpsIndex;
                    // 三档手柄中心的**画布 px**（**原版字面量**手算：轨道左 842.57 + (12 + 值/2 × 481.18) × 0.9）
                    var wantHcx = new[] { 853.37f, 1069.90f, 1286.43f };
                    // 判别点（**画布 px**，与宽高比无关）与期望档 —— 期望值 = uGUI `Slider.UpdateDrag` 那一式
                    //   在画布 px 上手算：`Clamp(round(Clamp01((x − 842.57) ÷ (481.18 × 0.9)) × 2), 0, 2)`：
                    //     850 → 0.034 ⇒ 档 0 · 920 → 0.358 ⇒ 档 0 · 990 → 0.681 ⇒ 档 1 · 1060 → 1.004 ⇒ 档 1
                    //     1130 → 1.327 ⇒ 档 1 · **1200 → 1.651 ⇒ 档 2** · 1270 → 1.974 ⇒ 档 2
                    //   🔴 **两处判别点**（旧式在这两格上给错档、16:9 下却全对）：
                    //     · **1200**：旧式（108 帧）在 **4:3** 下读成 `842.57 + (1200 − 960) × 0.75 = 1140.00`
                    //       ⇒ `(1140.00 − 842.57) ÷ 433.06 × 2 = 1.374` ⇒ **档 1**（正确 = 档 2）。
                    //     · **1130**：旧式在 **21:9** 下读成 `842.57 + (1130 − 960) × 1.3125 = 1183.13`
                    //       ⇒ `(1183.13 − 842.57) ÷ 433.06 × 2 = 1.573` ⇒ **档 2**（正确 = 档 1）。
                    //   ⛔ 两点离最近的取整边界都约 **7 画布 px**（不是浮点边界）；也都不落在任何手柄的抓手格里。
                    var probeX = new[] { 850f, 920f, 990f, 1060f, 1130f, 1200f, 1270f };
                    var probeIdx = new[] { 0, 0, 1, 1, 1, 2, 2 };
                    try
                    {
                        foreach (float asp in new[] { 4f / 3f, 21f / 9f, LayoutSpace.DesignAspect })
                        {
                            a1092Cam.aspect = asp;
                            // ⚠️ **必须重建**：本窗每一个矩形都是 `Screen()` 之后过 `LayoutSpace.FromPixel` 摆的
                            //    ⇒ 世界位置**在建的那一刻算一次**（`Open()` = `Build()` 把窗根子件整棵重建）。
                            win.Open();
                            float ratio = LayoutSpace.VisibleWidth / LayoutSpace.DesignWidth;
                            bool isDesign = Mathf.Abs(asp - LayoutSpace.DesignAspect) < 1e-4f;
                            if (isDesign)
                                CheckNear(ratio, 1f, 1e-4f,
                                          "（对照档 16:9）`VisibleWidth / DesignWidth` = 1 ⇒ 新旧两种写法在这一档**重合**"
                                        + "（本节余下几条在这一档也必须过 —— 那就是「改动只动非 16:9」的证明）");
                            else
                                CheckTrue(Mathf.Abs(ratio - 1f) > 0.1f,
                                          $"（前提）宽高比 {asp:F4} 下 `VisibleWidth / DesignWidth` = {ratio:F4}"
                                        + " —— **偏离 1 超过 10%**（≈1 就说明这一档已经是 16:9 ⇒ 本节没有鉴别力）");

                            var aRow = FindChild(FindChild(root, "Graphics Tab"), "FPS Limit");
                            var aSl = aRow != null ? FindChild(aRow, "FPS Slider") : null;
                            var aHd = aSl != null ? FindChild(aSl, "Handle") : null;
                            CheckTrue(aHd != null,
                                      $"（前提）宽高比 {asp:F4}：手柄节点 `FPS Limit/FPS Slider/Handle` 拿得到"
                                    + "（拿不到 ⇒ 本档这几条等于没验）");
                            if (aHd == null) continue;

                            // ---- 尺子：三档手柄中心的**画布 px**（读的是**建在树上的事实** ——
                            //      `MenuDraw.PixelOfDesign` 正是建件那条换算 `LayoutSpace.FromPixel` 的逆）
                            var hcx = new float[3];
                            float hcxWorst = 0f;
                            for (int k = 0; k < 3; k++)
                            {
                                win.SetFpsIndex(k, false);                  // ⚠️ 不 fire：自检不改进程帧率
                                hcx[k] = MenuDraw.PixelOfDesign(aHd.position).x;
                                hcxWorst = Mathf.Max(hcxWorst, Mathf.Abs(hcx[k] - wantHcx[k]));
                            }
                            CheckTrue(hcxWorst <= 0.05f,
                                      $"（尺子）宽高比 {asp:F4}：三档手柄中心的**画布 px** = 原版字面量手算的 "
                                    + $"853.37 / 1069.90 / 1286.43（实得 {hcx[0]:F2} / {hcx[1]:F2} / {hcx[2]:F2}，"
                                    + $"最大偏差 {hcxWorst:F4} ≤ 0.05）—— 这一条不过 ⇒ 下面几条量的是别的东西");
                            float cy = MenuDraw.PixelOfDesign(aHd.position).y;   // 手柄与轨道**同中心线**（三档同一个 y）
                            // 画布 px → 世界坐标（= 建件那条换算；`UpdateFpsDragAt` 吃的正是世界坐标）
                            System.Func<float, Vector3> W = c => LayoutSpace.FromPixel(c, cy);
                            // 把拖动状态摆回基线：**命中带之外**按一下 ⇒ `_fpsDragging = false`（不碰档位）
                            System.Action disarm = () => win.FpsPressAtCanvas(700f, cy);
                            // 中性按下：带内、**不在任何手柄的抓手格里**（hx 最左 853.37，差 438px ≫ 半宽 15.93）
                            //   ⇒ `_fpsGrabPx == 0`、`_fpsDragging == true`（起点档 = 2 ⇒ 带右端 1302.36 才含 847.57）
                            System.Action armDrag = () =>
                            { win.SetFpsIndex(2, false); win.FpsPressAtCanvas(847.568f, cy); };

                            // ---- ②-1：三档手柄中心喂进拖动口 ⇒ 取到的档 == idx（**按下支**）
                            //   ⚠️ 2 档那一次**只能**在 `_fpsIndex == 2` 时打：命中带 = **轨道 ∪ 手柄**，
                            //      而 2 档手柄中心 1286.43 探出轨道右端 1284.63 ⇒ 只有带取到 `_fpsIndex = 2`
                            //      那一档（右端 1302.36）才包含它 ⇒ 那一次**档位断言是空转的**，
                            //      所以那一次只断 `FpsDragging`（它在 21:9 下正是牙口，见下面的改坏法）。
                            for (int k = 0; k < 3; k++)
                            {
                                disarm();
                                win.SetFpsIndex(2, false);
                                win.UpdateFpsDragAt(W(hcx[k]), true, true);
                                string broken = "；改坏法：把 `UpdateFpsDragAt` 那两处换回 `LayoutSpace.PxX/PxY` "
                                              + "⇒ 非 16:9 下读出来的 x 围着 960 缩放 ⇒ **21:9 的 0 档**（读成 820.05，"
                                              + "被命中带左端 842.57 弹掉）与 **21:9 的 2 档**（读成 1388.44，被右端 1302.36 弹掉）"
                                              + "这两下**根本进不了拖动** ⇒ 红（16:9 两帧重合 ⇒ 仍绿）";
                                if (k < 2)
                                    CheckTrue(win.FpsDragging && win.FpsIndex == k,
                                              $"★（A1092）宽高比 {asp:F4}：**手柄中心**（画布 x = {hcx[k]:F2}）按下 ⇒ "
                                            + $"开始拖**并且**取到**档 {k}**（起点档是 2 ⇒ 不是「保持原值」）" + broken);
                                else
                                    CheckTrue(win.FpsDragging,
                                              $"★（A1092）宽高比 {asp:F4}：2 档手柄中心（画布 x = {hcx[2]:F2}）按下 ⇒ "
                                            + "开始拖（⚠️ 档位那一半由「起点必须是 2 档」这条前提**逼成了空转**，"
                                            + "所以只断状态）" + broken);
                            }

                            // ---- ②-1（续）：同一批世界坐标走**拖动支**（`pressedThisFrame = false && held = true`）
                            for (int k = 0; k < 3; k++)
                            {
                                disarm();
                                armDrag();                                  // ⇒ `_fpsDragging = true`、`_fpsGrabPx = 0`
                                win.SetFpsIndex((k + 1) % 3, false);        // 起点档 ≠ k
                                win.UpdateFpsDragAt(W(hcx[k]), false, true);
                                CheckTrue(win.FpsIndex == k,
                                          $"★（A1092）宽高比 {asp:F4}：**拖动支**喂手柄中心（画布 x = {hcx[k]:F2}）⇒ "
                                        + $"**档 {k}**（起点档另设、与 k 不同 ⇒ 这一条不是同义反复）");
                            }

                            // ---- ②-1b：判别点扫描（**4:3 与 21:9 各有一处旧实现必然给错档**，见 `probeX` 的注释）
                            for (int i = 0; i < probeX.Length; i++)
                            {
                                disarm();
                                armDrag();
                                win.SetFpsIndex((probeIdx[i] + 1) % 3, false);
                                win.UpdateFpsDragAt(W(probeX[i]), false, true);
                                CheckTrue(win.FpsIndex == probeIdx[i],
                                          $"★★（A1092）宽高比 {asp:F4}：拖动支喂**画布 x = {probeX[i]:F0}** ⇒ "
                                        + $"**档 {probeIdx[i]}**（期望值 = uGUI 那一式在画布 px 上手算，"
                                        + "⛔ 不从被测实现读；起点档另设 ⇒ 不是同义反复）"
                                        + "；改坏法：换回 `LayoutSpace.PxX` ⇒ **4:3 的 x=1200** 给档 1、"
                                        + "**21:9 的 x=1130** 给档 2 ⇒ 非 16:9 两档各红一条（16:9 全绿 —— 这就是它潜伏的原因）");
                            }

                            // ---- ②-2（**灭自证**）：同一个世界坐标下，「点选」那一半（`SetFpsFromPointer`）与
                            //      拖动那一半（`UpdateFpsDragAt` 的取值那一路）必须**逐值相等**（容差 0 ——
                            //      同一像素不必再 round 一次）。
                            //   🔴 牙口边界（如实记）：**两边都调同一个 `MenuDraw.PixelOfDesign` ⇒
                            //      只要没人把拖动口改回 `PxX/PxY` 它就恒等** ⇒ 这条只防「其中一半被改回旧式」；
                            //      **「换算本身对不对」由 ②-1b 那张绝对档位表负责** —— 两条缺一不可。
                            for (int i = 0; i < probeX.Length; i++)
                            {
                                var w2 = W(probeX[i]);
                                disarm();
                                armDrag();
                                win.SetFpsIndex(2, false);
                                win.UpdateFpsDragAt(w2, false, true);
                                int byDrag = win.FpsIndex;
                                win.SetFpsIndex(2, false);
                                win.SetFpsFromPointer(w2);
                                int byPointer = win.FpsIndex;
                                CheckTrue(byPointer == byDrag,
                                          $"★（A1092）宽高比 {asp:F4} 画布 x = {probeX[i]:F0}：**点选**那一半给档 "
                                        + $"{byPointer} · **拖动**那一半给档 {byDrag} —— 必须**逐值相等**（容差 0）"
                                        + "；改坏法：只把拖动那半改回 `LayoutSpace.PxX` ⇒ 两半分家 ⇒ 红");
                            }

                            // ---- ②-3：抓手偏移（`_fpsGrabPx`）也必须跟 `px` 同帧 —— 同族第二处
                            //   （`FpsPressAtCanvas` 里 `hx = …position.x * 108f + 960f`，2026-10-18 已一并改成
                            //    `MenuDraw.PixelOfDesign(…)`）。原来的病：4:3 下 `|px − hx| = 0.25 × |px − 960|`
                            //   （手柄离画布中心 480px 时差 **120px**，远大于 `half` = 35.406 × 0.9 ÷ 2 = **15.93**）
                            //   ⇒「按在手柄上」那条判据**恒假** ⇒ `_fpsGrabPx` 恒 0 ⇒ 抓手偏移丢失。
                            //   **怎么让自检看见它**（抓手偏移只改「值跟着谁走」，不改别的）：
                            //     · 偏移在 ⇒ 值跟 **手柄中心 + 位移** 走：1069.90 + (1175 − 1084.90) = **1160.00**
                            //       ⇒ `(1160.00 − 842.57) ÷ 433.06 × 2 = 1.466` ⇒ **档 1**
                            //     · 偏移丢了 ⇒ 值按**指针自己**算 = `(1175.00 − 842.57) ÷ 433.06 × 2 = 1.535` ⇒ **档 2**
                            //   ⛔ **「行为上看得见」那一半**（拖到 1175）**必须**落在取整边界附近
                            //      （偏移最大只到 `half` = 15.93 画布 px ⇒ 最多把读数搬 0.0736 档）
                            //      —— 探针取 1175，离边界 1.5 约 **7.4/7.6 画布 px**。
                            //   🔴 **两条一起才有牙**（调度台 2026-10-18 裁定）：下面那条「拖到 1175」断的是
                            //      「这个差**在行为上真的生效**」（值确实跟着「手柄中心 + 位移」走），
                            //      紧随的 `FpsGrabPxForTest` 那条断的是「这个差**算对了**（== +15.0）」——
                            //      少任何一条，另一半都能被改坏而不红（例：把偏移恒置 15 就能骗过前者）。
                            {
                                disarm();
                                win.SetFpsIndex(1, false);
                                win.UpdateFpsDragAt(W(hcx[1] + 15f), true, true);   // 按在**手柄上**（15 ≤ 半宽 15.93）
                                // 🔴 **直断**（调度台 2026-10-18 裁定加 `FpsGrabPxForTest` 这个只读口）：
                                //    按下点离手柄中心 15 画布 px ⇒ 抓手偏移**必须就是 +15.0**。
                                //    期望值是**外部字面量**（⛔ 不是拿 `px − hx` 现算 —— 那与实现同式 = 自证）；
                                //    容差 0.01 只留 float 往返那点余量（实测量级 ~1e-4 px）。
                                //    🔴 这一条比下面那条「拖到 1175」**硬**：那条要把 0.0736 档的差推到取整边界上才看得见
                                //    （探针离边界只剩 7.4px），这一条**直接读那个量本身**、与档位取整无关。
                                CheckNear(win.FpsGrabPxForTest, 15f, 0.01f,
                                          $"★★（A1092）宽高比 {asp:F4}：按在手柄中心**右 15px** ⇒ **抓手偏移 = +15.00 画布 px**"
                                        + $"（实得 {win.FpsGrabPxForTest:F4}）"
                                        + "；改坏法：把 `FpsPressAtCanvas` 里的 `hx` 换回内联的 `… × 108f + 960f` "
                                        + "⇒ 非 16:9 下 `|px − hx|` 远大于手柄半宽 **15.93**（4:3 差 `0.25 × |px − 960|`、"
                                        + "21:9 差 `0.3125 × |px − 960|`）⇒ 这一下被判成「按在轨道空处」⇒ **偏移 = 0** ⇒ 红"
                                        + "（16:9 两帧重合 ⇒ 仍绿）");
                                CheckTrue(win.FpsDragging && win.FpsIndex == 1,
                                          $"（前提）宽高比 {asp:F4}：按在手柄中心**右 15px** ⇒ 开始拖、且**档不变**（1）"
                                        + " —— 抓手偏移只记差、不当场取值");
                                win.UpdateFpsDragAt(W(1175f), false, true);
                                CheckTrue(win.FpsIndex == 1,
                                          $"★★（A1092）宽高比 {asp:F4}：抓住手柄后再拖到**画布 1175** ⇒ **档 1**"
                                        + "（值跟「手柄中心 + 位移」= 1160.00 走）"
                                        + "；改坏法：把 `FpsPressAtCanvas` 里的 `hx` 换回内联的 `… × 108f + 960f` "
                                        + "⇒ 非 16:9 下抓手偏移恒 0 ⇒ 值按指针自己算 = 1175.00 ⇒ **档 2** ⇒ 红"
                                        + "（16:9 两帧重合 ⇒ 仍绿）");
                                win.UpdateFpsDragAt(W(1270f), false, true);        // 反向哨兵：拖动这一路**真会改档**
                                CheckTrue(win.FpsIndex == 2,
                                          $"（哨兵）宽高比 {asp:F4}：再拖到**画布 1270** ⇒ **档 2**"
                                        + "（证明上一行不是「拖动一直是空操作」的假断言）");
                            }
                        }
                    }
                    finally
                    {
                        // ⚠️ **必须还原**（本节余下每一段几何都是按 16:9 建出来的），并重建回 16:9 那棵树
                        a1092Cam.aspect = a1092Back;
                        win.Open();
                        win.FpsPressAtCanvas(700f, 0f);         // 把「正在拖」摆回基线（带外 ⇒ 只清状态）
                        win.SetFpsIndex(a1092I0, false);
                        Application.targetFrameRate = a1092F0;
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
                // 🆕 **2026-10-19（波 1b）**：需子串改走 `TermHead(键)` —— 与上面两条同族（那三条 `_flash`
                //    也都接了词条）；⛔ 不再写死那串「本来就长得像英文」的中文列字面量（键值一改就假红）。
                CheckTrue(win.Flash != null && win.Flash.StartsWith(TermHead("Settings/Graphics/Flash/SmallScreenUI")),
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
                    // ① 文字 = **当前语档那一列**（键 `Settings/Graphics/AutoZoom`：EN `Auto zoom` / ZH `自动缩放`）
                    //    🔴 **2026-10-18 就地改掉「写死单语当期望值」**：原来写死 `== "Auto zoom"` —— 而这颗标签
                    //    **已经接上语言表**（`Shell/SettingsWindow.cs:1883` 的 `azText = () => Loc.T(lkGfxAutoZoom)`，
                    //    登记进 `_gfxRowLabels` 短链）⇒ 中文档下实得「自动缩放」**必然红**（自检实测就红在这条）。
                    //    ⇒ 期望值改走 `Loc.T(键)`：⛔ 不写死英文、⛔ 也不写死中文（那样只是把红挪到英文档）。
                    //    ⚠️ 上面 `:1461` 那句「原版 TMP 的 `m_text` 就是英文」说的是**原版那一列英文**的出处，
                    //       ⛔ 不是「界面上任何时候都该印英文」—— 接了词条之后中文档印的是中文那一列。
                    CheckTrue(TextOf(azN) == Loc.T("Settings/Graphics/AutoZoom"),
                              $"行文字 = `Loc.T(\"Settings/Graphics/AutoZoom\")`（EN `Auto zoom` / ZH `自动缩放`；"
                            + $"实得「{TextOf(azN)}」）");
                    // 🔴 **灭自证**（照本文件 `:792-826` 那节的切档夹具）：只断「当前档 == 词条」不够 ——
                    //    实现若被改回**写死英文**、期望值也一起改回 `"Auto zoom"` ⇒ 两处一起变绿。
                    //    ⇒ **两语档各断一次**，且要求两档的**字真的不同**（灭自证那条）。
                    //    ⚠️ `RefreshTexts()` 走 `_gfxRowLabels` 那条短链重算 `Loc.T` ⇒ 改档后行字当场跟着变。
                    {
                        var langAz = Loc.Current;
                        bool perAz = Loc.PersistOverride;
                        Loc.PersistOverride = true;                 // 切档不写盘（同本文件其余几处）
                        Loc.SetLanguage(AvailableLanguages.Chinese);
                        win.RefreshTexts();
                        string zhWant = Loc.T("Settings/Graphics/AutoZoom"), zhGot = TextOf(azN);
                        Loc.SetLanguage(AvailableLanguages.English);
                        win.RefreshTexts();
                        string enWant = Loc.T("Settings/Graphics/AutoZoom"), enGot = TextOf(azN);
                        CheckTrue(zhGot == zhWant && enGot == enWant
                                  && !string.IsNullOrEmpty(zhGot) && !string.IsNullOrEmpty(enGot),
                                  $"★ 中/英两语档各断一次：中文档「{zhGot}」= 「{zhWant}」· 英文档「{enGot}」= 「{enWant}」");
                        CheckTrue(zhGot != enGot,
                                  $"★ …而且两档的字**真的不同**（「{zhGot}」≠「{enGot}」）—— 灭自证：实现改回"
                                + "写死英文 + 期望值也改回 ⇒ 这一条红（只断单档的写法分不出两种实现）");
                        Loc.SetLanguage(langAz);
                        Loc.PersistOverride = perAz;
                        win.RefreshTexts();
                    }
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
                    // 🆕 **2026-10-19（波 1b）**：需子串改走 `TermHead(键)`（同上面那两条 —— 这句也接了词条）。
                    CheckTrue(win.Flash != null && win.Flash.StartsWith(TermHead("Settings/Graphics/Flash/AutoZoom")),
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
                // 🔴 **2026-10-19（波 1b）**：这一行的字也接了词条（`Shell/SettingsWindow.cs:1966` 的
                //    `Func<string> ssAaText = () => Loc.T(lkGfxSuperSamp)`）⇒ 期望值走 `Loc.T`
                //    （旧写法写死英文 `"Use super sampling"`：英文档恰好绿、**中文档变「超采样」⇒ 红**）。
                //    原版 TMP 印的是西语 `Sobremuestreo` ⇒ EN 列是**我们挑的**（同文件头 ② 那条口径）。
                CheckTrue(ssOn != null && TextOf(ssOn) == Loc.T("Settings/Graphics/EnableSuperSampling"),
                          $"② …行文字 = `Loc.T(\"Settings/Graphics/EnableSuperSampling\")`（实得「{TextOf(ssOn)}」；"
                        + "EN `Use super sampling` / ZH `超采样`）");

                // ③ 点它 ⇒ **只写 flag**（原版 `SuperSamplingToggleClick`：写 +0x124 + 置脏），**不当场改分辨率**
                var ssBox = ssOn != null && FindChild(ssOn, "Toggle") != null
                          ? FindChild(ssOn, "Toggle").GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOff,
                          "（前提）出厂这一格画的是**关**的图（原版 cctor 没写 +0x124 ⇒ 零初始化）");
                Click(ssOn);
                CheckTrue(SuperSampling.Enabled, "③ 点一下 ⇒ 原版 `GameStaticData.superSampling`(+0x124) 那一半**开了**");
                CheckTrue(ssBox != null && ssBox.Texture != null && ssBox.Texture.name == SettingsWindow.ArtToggleOn,
                          "③ …方框也换成了**开**的图");
                // 🆕 **2026-10-19（波 1b）**：需子串改走 `TermHead(键)`（同族第四处 —— 这句也接了词条）。
                CheckTrue(win.Flash != null && win.Flash.StartsWith(TermHead("Settings/Graphics/Flash/SuperSampling")),
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
                // 🔴 **2026-10-15（A672）夹具探针加固**：裸 `AddComponent<GameWindow>()` 建出来的探针
                //    **从不赋 `type`** ⇒ 字段停在哨兵 `GameWindow.UnsetType`(-1)，靠「今天没人在这些探针上读它」
                //    保平安（W8 报告 §三-③ 点名的那一档 ——「不是靠赋过值，是靠没人读」）。
                //    这里显式钉成 **`Fullscreen`**：它正是 A672 之前那个默认值 ⇒ 与改前**逐位同义**；
                //    将来谁把这些探针改成走 `OpenWindow`，也不会静默落进「弹窗支」（那是更难认的一种坏法）。
                //    ⚠️ **这不是「本窗的档位判据」**（探针没有原版对应物）；哨兵口径 → `Shell/WindowsManager.cs:100`。
                //    ⚠️ 本文件里 **`tWin` / `t3Win` 两处【故意不赋】**（它们是 A672 哨兵断言自己的探针，见上面那一节）。
                w1.type = WindowType.Fullscreen;
                w1.extraScaleSmallScreen = 1.2f;
                CheckTrue(w1.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                CheckTrue(w1.GetComponent<TransformScalerBySmallScreenUI>() == null,
                          "① 开关**关**着 ⇒ **连缩放器都不挂**（照原版 `Open()` 的第一层判据）");
                CheckNear(w1.transform.localScale.x, 1f, 1e-4f, "① …而且窗口根 `localScale` 停在 1");

                // ② 开关开 + `extra = 1.2` ⇒ 乘 1.2；**再 `Tick` 一次不重复乘**（守卫）
                SmallScreenUI.Set(true);
                var p2 = new GameObject("probe-2 (flag on, extra 1.2)");
                var w2 = p2.AddComponent<GameWindow>();
                w2.type = WindowType.Fullscreen;      // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `w1` 那一处）
                w2.extraScaleSmallScreen = 1.2f;
                CheckTrue(w2.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                w3.type = WindowType.Fullscreen;      // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `w1` 那一处）
                w3.extraScaleSmallScreen = 1f;
                var baked3 = p3.AddComponent<TransformScalerBySmallScreenUI>();
                baked3.menuScale = 1.35f; baked3.Initialize();      // = prefab 里烤着的那颗（`m_Enabled: 1`）
                CheckTrue(w3.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                w4.type = WindowType.Fullscreen;      // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `w1` 那一处）
                w4.extraScaleSmallScreen = 1f;
                var baked4 = p4.AddComponent<TransformScalerBySmallScreenUI>();
                baked4.menuScale = 1.35f; baked4.Initialize();
                CheckTrue(w4.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                    // 🔴 **2026-10-15（A546①）就地订正（铁律 5）**：本条文案原来写「走点阵兜底时 `Align*On`
                    //   **首句就 return**（空操作）」—— 那句在 **A476**（`AlignLeftOn`/`AlignRightOn`）/
                    //   **A491**（`SetAlignLeft`）之后**不再成立**：两处都改成**出声后返回**
                    //   （`Battle/Label.cs` 的 `NoteDotBackendLacks`），不再静默。
                    //   ⛔ 别再照抄「首句就 return」（那是 A476 之前的实况；同族病灶 = 换口/改实现之后不回头改注释）。
                    CheckTrue(a228l1 != null && a228l1.CanRenderChinese,
                              "（前提）TMP 后端在 —— 走点阵兜底时 `Align*On` / `SetAlignLeft` 都**不产生效果**（A476 / A491 之后它们**出声**、不再静默 `return`），下面四条无从谈起");
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
                    a228w2.type = WindowType.Fullscreen;   // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `w1` 那一处）
                    a228w2.extraScaleSmallScreen = 1.2f;
                    CheckTrue(a228w2.TryOpen(null), // = 生产那条路（挂缩放器 + SetScale）
                              "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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

            // ---------------- 🆕 A491：`Label.SetAlignLeft()` —— 同族**第三个**静默口，必须出声 ----------------
            //   同族三处（都在 `Battle/Label.cs`）：`AlignLeftOn` / `AlignRightOn`（账 **A476**，2026-10-12
            //   做出声）· `SetCharSpacing`（更早就出声）· `SetAlignLeft()`（**本账**）—— 同一形状的病灶：
            //   `_tmp == null`（**点阵后端**，判据 `TmpFont.Available == false`）时「**这个后端根本没有对齐
            //   这回事**」，而原来**一个字都不留** ⇒ 字体资产缺失时**整批逐行左对齐静默退回居中**
            //   （多行时短的那些行居中 —— 正是 `SetAlignLeft` 要修的那个差别），画面错、日志空
            //   （红线：不许静默失败）。
            //   🔴 **为什么必须成对断**（本工程那条系统性毛病：弱断言分不出两种状态）：
            //     · 只断「点阵后端出声」⇒ 把 `Debug.Log` 那句搬到 `if (_tmp == null)` **外面**
            //       （= **无条件出声**）照样绿 —— 而那会让有 TMP 的那几万次调用也刷屏；
            //     · 只断「有 TMP 时不出声」⇒ 把出声整句删掉照样绿（**那正是本账要修的原始状态**）。
            //     ⇒ ①（点阵后端点名 `AlignLeftOn`，A476 回归）+ ②（点阵后端点名 `SetAlignLeft`）
            //        + ③（**有 TMP 时不许出声**）三条合起来才分得出「条件出声 / 永远出声 / 永不出声」。
            //   🔴 **② 同时是「key 不与 A476 撞车」那条硬约束的判据**：`_dotAlignNoted` 是**进程内静态**
            //     HashSet、**同一个 key 只出声一次**（key = `口名 + "|" + 节点全路径`）⇒ 若 `SetAlignLeft`
            //     照抄了 A476 的 `which`（`左对齐`），那么在同一探针节点上「① 刚响过」之后
            //     **② 会一声不响**（静默复发、而且只在同一个进程里现形）
            //     ⇒ ①② 用**同一个节点**、**必须两条都在**。
            //     ⚠️ 探针节点名必须是**本次运行没出现过**的（key 含节点全路径；用了跑过的名字 ⇒ 假红，
            //        判据 → `资料/已知的坑.md` 的「按 key 做**进程内**去重 ⇒ 断『出声』的断言必须用
            //        **本次运行没出现过的 id**」那一条）。
            Section("A491：`Label.SetAlignLeft()` 在点阵后端下出声（同族第三处静默口）");
            {
                // ⚠️ 探针**不走 `Label.Create`**：那条路在字体资产在时会建 TMP（`_tmp != null` = 正常路径，
                //    出声那一支根本不执行）。直接 `AddComponent<Label>()` ⇒ `_tmp == null`（两条后端都没建）
                //    —— 正是「点阵后端」那一档（`TmpFont.Available == false` 时生产侧就是这样）。
                var a491go = new GameObject("A491 dot-align probe");
                var a491dot = a491go.AddComponent<Label>();
                CheckTrue(a491dot != null && !a491dot.CanRenderChinese,
                          "（前提）探针落在**点阵后端**（`CanRenderChinese == false` ⇒ `_tmp == null`）"
                        + " —— 不是这一档的话，下面「必须出声」那两条无从谈起");

                // ① A476 那一口（回归）：证明「捕捉网 + 点阵后端」这套夹具本身是通的
                var a491L1 = CaptureLogs(() => a491dot.AlignLeftOn(1f));
                bool a491hit1 = a491L1.Exists(m => m.Contains("点阵后端没有对齐这回事"));
                CheckTrue(a491hit1,
                          $"★① `Label.AlignLeftOn`（A476 那一口，回归）在点阵后端下**出声**"
                        + $"（本趟共抓 {a491L1.Count} 行日志）"
                        + "；改坏法：删掉 `Battle/Label.cs` 里 `AlignLeftOn` 首句那句 `NoteDotBackendLacks(...)` ⇒ 红");

                // ② 本账那一口 —— 它与 ① **同一个节点** ⇒ 这一条同时咬住「key 不撞车」。
                //    ⚠️ 这一条**故意不断文案**（只断「出没出声」）：文案是定位锚、不是判据 ——
                //       出声那件事本身才是（红线：不许静默失败）。
                var a491L2 = CaptureLogs(() => a491dot.SetAlignLeft());
                CheckTrue(a491L2.Count > 0,
                          "★★② **同一个探针节点上** `Label.SetAlignLeft()`（本账）在点阵后端下**出声了**"
                        + $"（本趟共抓 {a491L2.Count} 行日志）—— 它同时是「**key 不与 A476 撞车**」那条判据："
                        + "`_dotAlignNoted` 是进程内静态 HashSet、同一个 key 只响一次，① 刚在这个节点上响过，"
                        + "② 还能响 ⇒ 两口 key 确实不同（A476 = `左对齐`/`右对齐`；本账 = `逐行左对齐`）"
                        + "；改坏法：① 删掉 `SetAlignLeft` 里那句 `NoteDotBackendLacks(...)`（退回静默，= 修前那状态）"
                        + "或 ② 把它那一支的 `which` 也写成 \"左对齐\" ⇒ 本条红（后者 = 静默复发）");
                Object.DestroyImmediate(a491go);

                // ③ **反面对照**：有 TMP 的那条路**一个字都不许出**（否则「条件出声」退化成「永远出声」）。
                //    ⚠️ 同理**不断文案**：断的是「这一下**一行日志都没有**」（`SetAlignLeft` 在 TMP 那条路上
                //       只有一句 `_tmp.alignment = …`，调用期间本就不该有任何日志）。
                CheckTrue(TmpFont.Available,
                          "（前提）字体资产在（`TmpFont.Available`）—— ③ 要的就是「有 TMP」那一档"
                        + "（同族前提见上面 A228 那一节：缺字体资产时该红的是那一条，不是本条）");
                if (TmpFont.Available)
                {
                    var a491root = new GameObject("A491 tmp probe root");
                    var a491tmp = Label.Create(a491root.transform, "A491", Vector3.zero, 5, Color.white,
                                               new Vector2(0.5f, 0.5f), "A491 tmp label");
                    CheckTrue(a491tmp != null && a491tmp.CanRenderChinese,
                              "（前提）③ 的探针走的是 **TMP 后端**（`CanRenderChinese == true`）");
                    if (a491tmp != null && a491tmp.CanRenderChinese)
                    {
                        var a491L3 = CaptureLogs(() => a491tmp.SetAlignLeft());
                        CheckTrue(a491L3.Count == 0,
                                  "★③ **有 TMP 时 `SetAlignLeft()` 一行日志都不出**"
                                + $"（本趟共抓 {a491L3.Count} 行日志）"
                                + " —— 与②合起来才分得出「条件出声 / 永远出声 / 永不出声」三态"
                                + "（②只断「出声」⇒ 无条件出声照样绿；③只断「不出声」⇒ 把出声整句删掉照样绿）"
                                + "；改坏法：把 `NoteDotBackendLacks` 那一句搬到 `if (_tmp == null)` **外面** ⇒ 本条红");
                    }
                    Object.DestroyImmediate(a491root);
                }
            }

            // ---------------- 🆕 A546②：`SetAlignLeft()` 的**时机**（doc 里那句是调用方责任；记账口在这儿验收）----------------
            //   判据 / 为什么只能记账、**既不硬守卫也不出声** → `Battle/Label.cs` 的 `SetAlignLeft` doc
            //   （两条现读实据：① 日志会**误报** —— `Shell/MenuDraw.Text` 的内层 `TextCore` 自己就调
            //    `SetAutoFitBox`，而 `Battle/CardDisplayWindow.cs` 的 3 处**正是**「它之后紧跟 `SetAlignLeft()`」；
            //    ② 自愈要重排，而那一刻多半在**未激活**的父链里 ⇒ 量出来是天文数字，正是 `HasMeasuredWidth` 记的坑）。
            //   🔴 **本节的判别力 = 两态**（只断一边 ⇒「恒 +1」或「恒不动」都照样绿）：
            //     先调对齐、后定版面（`Battle/UnitChatPanel.cs` + 3 处探针是这个形状）⇒ 计数**不动**；
            //     反过来（`Battle/CardDisplayWindow.cs` 那 3 处：`MenuDraw.Text` 的内层**已经定过版面**）⇒ **+1**。
            Section("A546②：`SetAlignLeft()` 排在「定版面」那条路之后 ⇒ 记账口 +1（两态）");
            {
                CheckTrue(TmpFont.Available,
                          "（前提）字体资产在（`TmpFont.Available`）—— 点阵那一支走 A491（那边已断），本节的「时机」只在 TMP 后端成立");
                if (TmpFont.Available)
                {
                    var a546root = new GameObject("A546 timing probe root");
                    var a546lb = Label.Create(a546root.transform, "A546 时机", Vector3.zero, 5, Color.white,
                                              new Vector2(0.5f, 0.5f), "A546 timing label");
                    CheckTrue(a546lb != null && a546lb.CanRenderChinese,
                              "（前提）探针走的是 **TMP 后端**（`CanRenderChinese == true`）");
                    if (a546lb != null && a546lb.CanRenderChinese)
                    {
                        int c0 = a546lb.AlignAfterLayoutCount;
                        a546lb.SetAlignLeft();                       // ✅ 好序：这一刻还没有任何「定版面」的路跑过
                        CheckTrue(a546lb.AlignAfterLayoutCount == c0,
                                  "★①（好序）先 `SetAlignLeft()`、**后**定版面 ⇒ 记账口**不动**（实得 +"
                                + (a546lb.AlignAfterLayoutCount - c0) + "）—— `Battle/UnitChatPanel.cs` 与 `Editor/ChatBoxProbe.cs`"
                                + " 那 4 处就是这个形状（`Battle/CardDisplayWindow.cs` 那 3 处相反 ⇒ 会计 3 笔，见 `Battle/Label.cs` 的 doc）"
                                + "；改坏法：把判据放宽成「每次调用都记一笔」⇒ **这条红**");
                        a546lb.SetAutoFitBox(2f, 0.5f, 8f, 40f);     // 「定版面」之一（顺带把上一次对齐推到画面上）
                        a546lb.SetAlignLeft();                       // ❌ 过晚：已经定过版面了
                        CheckTrue(a546lb.AlignAfterLayoutCount == c0 + 1,
                                  "★②（过晚）定版之后再调 ⇒ 记账口 **+1**（实得 +"
                                + (a546lb.AlignAfterLayoutCount - c0) + "）；改坏法：把 `Battle/Label.cs` 的 `SetAlignLeft`"
                                + " 里那句 `if (_defLayoutPushed) AlignAfterLayoutCount++;` 删掉（退回「一个字都不留」）⇒ 这条红"
                                + "（与①成对：只断这一边 ⇒「恒 +1」照样绿）");
                        a546lb.ForceRelayout();                      // 再定一次版面（`ForceRelayout` 也是那三条之一）
                        int c1 = a546lb.AlignAfterLayoutCount;
                        a546lb.SetAlignLeft();
                        CheckTrue(a546lb.AlignAfterLayoutCount == c1 + 1,
                                  "★③ `ForceRelayout()` 也算「定版面」（它跑完 ⇒ 下一次 `SetAlignLeft()` 同样记一笔）"
                                + "—— 实得 +" + (a546lb.AlignAfterLayoutCount - c1)
                                + "；改坏法：只在 `SetAutoFitBox` 一处置位（漏掉 `ForceRelayout`）⇒ 这条红");
                    }
                    Object.DestroyImmediate(a546root);
                }
            }

            // ---------------- 🆕 A545：`Label` 点阵后端缺的那 5 档（同族静默口收尾） ----------------
            //   与 A476 / A491 **逐字同族**（`Battle/Label.cs` 的 `if (_tmp == null) return;`），但**不在对齐那一族**：
            //     `SetWrapWidth`（折行宽 · **生产 10 处**）· `SetWrappingMode`（换行模式 · **3**）·
            //     `ForceRelayout`（重排 · **4**）· `SetFontSize`（字号 · **外部 0 处**，只有 `ForceRelayout` 内部那 1 处）·
            //     `SetAutoFitBox`（自适应框 · **39**：`MenuDraw` / `MenuWindowBase` / `CollectionWindow` / `MainMenuRuntime` …）
            //     〔计数 = 全仓 `\.<方法>(` 去掉注释行、再去掉 `Battle/Label.cs` 与本文件；`Editor/` 那些算自检探针〕
            //   —— 这 5 处原来**一个字都不留**，而 `ForceRelayout` / `SetFontSize` 两处的 doc **自己就写着**
            //   「什么都不做（如实，不假装）」/「会静默无效」= **自陈静默** ⇒ 与 `CLAUDE.md` §三
            //   「不许静默失败」正面打架（A545 已把那两句 doc 就地订正）。
            //   🔴 **为什么 5 个口挤在【同一个节点】上依次调**：`_dotAlignNoted` 是**进程内静态** HashSet、
            //     key = `口名 + "|" + 节点全路径`、**同一个 key 只响一次** ⇒ 只要有两个口用了**同一个口名**，
            //     后调的那个就**一声不响**（静默复发）。⇒「5 个口在**同一节点**上依次各响一次」这一件事本身
            //     就证明了**这 5 个 key 两两不同**（换个节点就**测不出撞车**了 —— 这是 A491 ★② 那条经验的推广）。
            //     ⚠️ 探针节点名必须是**本次运行没出现过**的（key 含节点全路径；用了跑过的名字 ⇒ **假红**，
            //        判据 → `资料/已知的坑.md` 的「按 key 做**进程内**去重 ⇒ 断『出声』的断言必须用
            //        **本次运行没出现过的 id**」那一条）。
            Section("A545：`Label` 点阵后端缺的那 5 档（折行宽 / 换行模式 / 重排 / 字号 / 自适应框）都出声");
            {
                // ⚠️ 同 A476 / A491：探针**不走 `Label.Create`**（那条路在字体资产在时会建 TMP ⇒ 出声那一支
                //    根本不执行）。直接 `AddComponent<Label>()` ⇒ `_tmp == null`（两条后端都没建）= 点阵后端。
                var a545go = new GameObject("A545 dot probe");
                var a545dot = a545go.AddComponent<Label>();
                CheckTrue(a545dot != null && !a545dot.CanRenderChinese,
                          "（前提）探针落在**点阵后端**（`CanRenderChinese == false` ⇒ `_tmp == null`）"
                        + " —— 不是这一档的话，下面「必须出声」那 5 条无从谈起");

                // ★⓪ 夹具自检：这只网 + 点阵后端这套是通的（`AlignLeftOn` = A476 那一口，回归）。
                //     🔴 它**同时是「A545 那 5 个口不与 A476 的 `左对齐` 撞 key」的前提**：先在这个节点上把
                //        `左对齐` 那一格消费掉，下面 5 条**还能各自响** ⇒ 它们没有一个叫 `左对齐`。
                var a545L0 = CaptureLogs(() => a545dot.AlignLeftOn(1f));
                CheckTrue(a545L0.Exists(m => m != null && m.Contains("点阵后端没有对齐这回事")),
                          "★⓪ `Label.AlignLeftOn`（A476 那一口，回归）在点阵后端下**出声**"
                        + $"（本趟共抓 {a545L0.Count} 行日志）—— 它不响 ⇒ 是**网 / 夹具**坏了，不是下面 5 个口的问题");

                // ★① `SetWrapWidth`
                var a545L1 = CaptureLogs(() => a545dot.SetWrapWidth(7.5f));
                CheckTrue(a545L1.Exists(m => m != null && m.Contains("[Label]") && m.Contains("点阵后端不会折行")),
                          "★① `Label.SetWrapWidth(7.5f)` 在点阵后端下**出声**"
                        + $"（本趟共抓 {a545L1.Count} 行日志）"
                        + "；改坏法：删掉 `Battle/Label.cs` 里 `SetWrapWidth` 首句那个 `NoteDotBackendLacks(...)` ⇒ 红");

                // ★② `SetWrappingMode` —— ⚠️ 它的口名**不能**与 ★① 相同（同了就静默 ⇒ 本条红）
                var a545L2 = CaptureLogs(() => a545dot.SetWrappingMode(3));
                CheckTrue(a545L2.Exists(m => m != null && m.Contains("[Label]") && m.Contains("点阵后端只有「单行」这一档")),
                          "★② `Label.SetWrappingMode(3)` 在点阵后端下**出声**"
                        + $"（本趟共抓 {a545L2.Count} 行日志）"
                        + "；改坏法：① 删掉那一句 `NoteDotBackendLacks(...)` ⇒ 红；"
                        + "② 把它的口名抄成 `折行宽`（★① 那个）⇒ ★① 刚在**同一节点**上消费过那个 key"
                        + " ⇒ 本条静默、红（**静默复发**，与 A491 ★② 同一个形状）");

                // ★③ `ForceRelayout`
                var a545L3 = CaptureLogs(() => a545dot.ForceRelayout());
                CheckTrue(a545L3.Exists(m => m != null && m.Contains("[Label]") && m.Contains("点阵后端不经过 TMP 排版")),
                          "★③ `Label.ForceRelayout()` 在点阵后端下**出声**"
                        + $"（本趟共抓 {a545L3.Count} 行日志）"
                        + "；改坏法：删掉那一句 `NoteDotBackendLacks(...)` ⇒ 红"
                        + "（它的 doc 原来写着「什么都不做（如实，不假装）」= 自陈静默，A545 已就地订正）");

                // ★④ `SetFontSize`（⚠️ 传的是**正数**：非正数那一支是「无效入参」、**故意不出声**，见方法头）
                var a545L4 = CaptureLogs(() => a545dot.SetFontSize(4f));
                CheckTrue(a545L4.Exists(m => m != null && m.Contains("[Label]") && m.Contains("点阵后端没有 TMP 的 fontSize")),
                          "★④ `Label.SetFontSize(4f)` 在点阵后端下**出声**"
                        + $"（本趟共抓 {a545L4.Count} 行日志）"
                        + "；改坏法：① 把两句 `if` 合成原来的 `if (_tmp == null || worldSize <= 0f) return;` ⇒ 红；"
                        + "② 把出声那一句删掉 ⇒ 红（它的 doc 原来写着「会静默无效」—— 同样是自陈静默）");

                // ★⑤ `SetAutoFitBox`
                var a545L5 = CaptureLogs(() => a545dot.SetAutoFitBox(5f, 2f, 10f, 30f));
                CheckTrue(a545L5.Exists(m => m != null && m.Contains("[Label]") && m.Contains("点阵后端既不会折行、也没有自适应")),
                          "★⑤ `Label.SetAutoFitBox(5f, 2f, 10f, 30f)` 在点阵后端下**出声**"
                        + $"（本趟共抓 {a545L5.Count} 行日志）"
                        + "；改坏法：删掉那一句 `NoteDotBackendLacks(...)` ⇒ 红");
                Object.DestroyImmediate(a545go);

                // ★⑥ **反面对照**：有 TMP 时这 5 个口**一行 `[Label]` 都不许出**。
                //    🔴 与 ★①~★⑤ 合起来才分得出**三态**：「条件出声 / 永远出声 / 永不出声」——
                //       只断「出声」⇒ 把 `NoteDotBackendLacks` 那一句搬到 `if (_tmp == null)` **外面**照样绿；
                //       只断「不出声」⇒ 把出声整句删掉照样绿（**那正是 A545 要修的原始状态**）。
                //    🔴 **谓词与 ★①~★⑤ 逐字相同**（`Contains("[Label]")` ＋ 该口的锚），否则两边量的不是一件事。
                //    ⚠️ 探针**另起一棵树**（`A545 tmp probe root`）—— 与点阵那棵同名会让两条节点路径撞在一起。
                CheckTrue(TmpFont.Available,
                          "（前提）字体资产在（`TmpFont.Available`）—— ★⑥ 要的就是「有 TMP」那一档"
                        + "（缺字体资产时该红的是上面 A228 那一节，不是本条）");
                if (TmpFont.Available)
                {
                    var a545root = new GameObject("A545 tmp probe root");
                    var a545tmp = Label.Create(a545root.transform, "A545", Vector3.zero, 5, Color.white,
                                               new Vector2(0.5f, 0.5f), "A545 tmp label");
                    CheckTrue(a545tmp != null && a545tmp.CanRenderChinese,
                              "（前提）★⑥ 的探针走的是 **TMP 后端**（`CanRenderChinese == true`）");
                    if (a545tmp != null && a545tmp.CanRenderChinese)
                    {
                        var a545L6 = CaptureLogs(() =>
                        {
                            a545tmp.SetWrapWidth(7.5f);
                            a545tmp.SetWrappingMode(3);
                            a545tmp.ForceRelayout();
                            a545tmp.SetFontSize(4f);
                            a545tmp.SetAutoFitBox(5f, 2f, 10f, 30f);
                        });
                        var a545tmpLogs = a545L6.FindAll(m => m != null && m.Contains("[Label]"));
                        CheckTrue(a545tmpLogs.Count == 0,
                                  "★⑥ **有 TMP 时这 5 个口一行 `[Label]` 日志都不出**"
                                + $"（本趟共抓 {a545L6.Count} 行日志，其中带 `[Label]` 的 {a545tmpLogs.Count} 行"
                                + (a545tmpLogs.Count > 0 ? "：`" + a545tmpLogs[0] + "`" : "")
                                + "）—— 与 ★①~★⑤ 合起来才分得出三态"
                                + "；改坏法：把任一句 `NoteDotBackendLacks(...)` 搬到 `if (_tmp == null)` **外面** ⇒ 本条红");
                    }
                    Object.DestroyImmediate(a545root);
                }
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
                a167w1.type = WindowType.Fullscreen;   // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `w1` 那一处）
                a167w1.extraScaleSmallScreen = a167M;
                CheckTrue(a167w1.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
                a167w2.type = WindowType.Fullscreen;   // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `w1` 那一处）
                a167w2.extraScaleSmallScreen = a167M;
                CheckTrue(a167w2.TryOpen(null), // 挂缩放器 + `SetScale`
                          "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
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
            // 🔴 **2026-10-20（`A1202`）**：`Sound Effects` / `Voice-overs` → **`FX` / `Voiceovers`**
            //   —— 这三个是**行容器的节点名**（生产代码里 `names[i] + " Container"`），
            //   原版 `m_Name` 逐字就是 `Music Container` / `FX Container` / `Voiceovers Container`
            //   （判据 = `python 工具/menu_rect.py bundle_menus_assets_all "Audio Settings" --depth 3 --cs`）。
            //   ⛔ 页面上印的字**不是**这一排（那三条走词条表的 `keys[]`，见 `Shell/SettingsWindow.cs`）。
            var auNames = new[] { "Music", "FX", "Voiceovers" };
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
                    // ⚠️ 靠**层名**找节点（`slider_fill` = `WfSlider.Create` 建它时用的 `name:`）。
                    //   🔴 **2026-10-21 更正（`A1365` · 铁律 5）**：这里原来写「那一层在 `WfSlider` 上
                    //   **没有对外访问器**，而 `Battle/WfSlider.cs` 不在本件白名单 ⇒ 不改它，用名字取」
                    //   —— **前提已不成立**：`A1359` / `A1351`② 起那三个读口就在（`FillWorldW` /
                    //   `FillWorldLeftX` / `FillCapWorldW`）。⚠️ **但本条仍按层名取** —— 这里量的是
                    //   `Fill` 层的**并集高**（`ImageQuad.WorldH`），那三个读口**全是横向量**、都不报高。
                    //   （「用名字取节点」这个量法本身仍是本仓库既有的，见 `Editor/CollectionScene.cs` 量 `Unlock`。）
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
                        // 🔴 **2026-10-21 更正（`A1365` · 铁律 5）**：这一句 `SetValue(1f, false)` 原来
                        //   标注的理由是「值拉到 1 ⇒ 填条不缩放的那一帧」（下面那条消息里也这么写）
                        //   —— **那个理由与 `capFl` 无关，已就地删掉**：`capFl` 读的是子件自己的 `WorldW`，
                        //   而 `WorldW` **从来不含**任何缩放（`A1359` 前后都一样，`A1359` 把横向缩放打在
                        //   三块子 quad 的 `localScale.x` 上）⇒ 摆哪个值都读出同一个数。
                        //   ⚠️ **这一句本身【不能删】** —— 它是**载荷**：下面 `hx0`（值 1 那一帧的手柄中心）
                        //   靠它摆出来，行程那条断言（`CheckNear(hx0 - hxAt0, 606.78f, 0.4f)`）要的正是它。
                        s.SetValue(1f, false);          // ⚠️ `fire: false` —— 自检**不许**改总线/存档（只摆值）
                        float capFl = 0f;
                        foreach (var q in flL.GetComponentsInChildren<ImageQuad>(true))
                            if (q != null && q.WorldW * 108f < 200f) capFl = Mathf.Max(capFl, q.WorldW * 108f);
                        // ⚠️ 上面那个 `WorldW * 108f < 200f` 是**按宽筛出九宫格两端端帽**的启发式
                        //   （本窗中段的**建件宽** ≈ 615.77 − 2×13.5 = 588.77，远超 200 ⇒ 被筛掉在外）。
                        //   🔴 **它本身与缩放无关**（`WorldW` 不含缩放）⇒ 它上面那句「值拉到 1」**不是**
                        //   它成立的条件（理由句已删，见上）。
                        //   ⚠️ 那个 200 是个**隐藏耦合**：只有「中段建件宽 ≫ 200」时才成立 —— 若哪天轨道
                        //   宽缩到 200 + 27 以下，中段会漏进这个 `max`、这条就量错对象。✅ 换成
                        //   `s.FillCapWorldW`（直接报端帽那块**画出来**的宽）能去掉这个耦合 ——
                        //   那是**行为改动**（自检宿主），本批**只订正注释、表达式一个字节没动**，
                        //   换法已写进 `A1365` 的报告交调度台裁（铁律 11：要做，只是先后问题）。
                        CheckNear(capFl, 13.5f, 1.5f,
                                  $"{who} 填条的**端帽**宽 = 原版 `30 ÷ ppuMul 2` = 15 设计 px × 0.9 ⇒ **13.5**"
                                + $"（实得 {capFl:F2}；这一格读的是子件自己的 `WorldW`、**不含任何缩放** ⇒ "
                                + "与上面那句 `SetValue(1f,false)` 无关；改坏法：传 30 ⇒ 30；只除 2 ⇒ 15 ⇒ 都红）");

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

            // ---------------- 🆕 2026-10-21（`A1359` / `A1351`② 的**断言**那一半）：填条那三块的几何 ----------------
            // 判据 = `资料/普查产出_第十三会话/W6c_A1350A1351A1359.md` §三「该补什么断言」。
            // 🔴 期望值一律写字面量、⛔ 不引用 `SettingsWindow.AuTrackW` / `RootScale`（那是**被测实参** ——
            //    与本节上面 `A125①`/`A125②` 那两条自证同一口径）。
            //   本页那三根实参：`trackW = 原版 684.19 × 根 0.9 = 615.771` · `capScale = 0.9`
            //   ⇒ 端帽 = 原版 `m_Border 30 ÷ ppuMul 2 × 0.9` = **13.5** 画布 px。
            // 🔴 与 `Editor/BattleScene.cs` 那一条**不是同一个数**（战斗那根是 561.08 / 15）⇒ 两边各断各的。
            Section("A1359：音频页三根滑块的**填条**（端帽恒宽 / 宽随值 / 左沿不动）");
            {
                float[] sVals = { 1f, 0.5f, 0.2f };
                string[] sTags = { "值 1", "值 0.5", "值 0.2" };
                for (int i = 0; i < 3; i++)
                {
                    if (sl == null || i >= sl.Length || sl[i] == null)
                    { CheckTrue(false, $"第 {i + 1} 根滑块在（量不到填条几何）"); continue; }
                    var s = sl[i];
                    string who = $"第 {i + 1} 根滑块";
                    float sVWas = s.Value, sCapFirst = -1f;
                    for (int k = 0; k < sVals.Length; k++)
                    {
                        float v = sVals[k];
                        s.SetValue(v, false);      // ⚠️ `fire: false` —— 自检**只摆值**（不碰总线 / 存档）
                        float fw = s.FillWorldW * 108f;
                        float flx = s.FillWorldLeftX * 108f;
                        float fcap = s.FillCapWorldW * 108f;
                        CheckNear(flx, -307.8855f, 0.05f,
                                  $"{who} / {sTags[k]}：填条**左沿** = 原版 684.19 × 0.9 ÷ 2 ⇒ **−307.8855**"
                                + $"（实得 {flx:F3}；值怎么变都不动 —— 原版 `Slider` 把 `Fill` 锚在左边的锚点上）");
                        CheckNear(fw, v * 615.771f, 0.05f,
                                  $"{who} / {sTags[k]}：填条**画出来的宽** = 值 × 原版 684.19 × 0.9 = **{v * 615.771f:F3}**");
                        CheckNear(fcap, 13.5f, 0.05f,
                                  $"{who} / {sTags[k]}：端帽**恒宽** 13.5 画布 px（原版 `m_Border 30 ÷ ppuMul 2 × 0.9`）"
                                + $"（实得 {fcap:F3}；改坏法：传 30 ⇒ 30、只除 2 不过 0.9 ⇒ 15 ⇒ 都红）");
                        if (k == 0) sCapFirst = fcap;
                        else
                            // 🧨 **灭自证那一半**：跨两个值**相等** —— 旧的整体横向缩放路**结构上做不到**
                            //    （值 0.5 时端帽掉到 6.75）；把实现与读口一起改回去也救不了这条。
                            CheckNear(fcap, sCapFirst, 0.02f,
                                      $"★★ {who} / {sTags[k]} 的端帽宽与「值 1」**逐值相等**"
                                    + $"（{fcap:F3} vs {sCapFirst:F3}）—— 旧的整体缩放路在值 0.5 时掉一半 ⇒ 红");
                    }
                    // （还原）摆回本格进来时的那个值 —— 上面 `A169` 那一段收尾留下的也是它
                    s.SetValue(sVWas, false);
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
                // 🆕 **2026-10-20（`A1201`）**：原版三行 `Label` 的**框宽**（= 折行宽，**屏幕 px**，逐颗实读）。
                //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Audio Settings" --depth 3`
                //   那三行 `Label` 的「宽」列 = **307.89 / 615.77 / 615.77**（= 原版设计值 342.0975 / 684.195 × 0.9）。
                //   🔴 设计值那一侧来自**锚点字段**（`menu_rect.py … --depth 3 --cs`）：
                //   `Music Container > Label` 的 `aMax.x = **0.5**`（`aMin.x` 恒 0、`m_SizeDelta.x` 恒 0
                //   ⇒ 宽 = 行宽 684.195 × 0.5 = **342.0975**）、另两颗 `aMax.x = **1**`（= 整行 684.195）。
                //   ⚠️ 逐颗写，⛔ **别一刀切成同一个数**（原版那一颗是半行、另两颗是整行）。
                //   ⚠️ 与上面 `CheckAutoFit(…, boxW: …)` 的关系：那一条断的是「**渲出来** ≤ 框」
                //     （那一行的字本来就短 ⇒ 框放宽一倍也是绿的），**这一条才断「框本身」**。
                float[] auLblWpx = { 307.89f, 615.78f, 615.78f };
                var auBox = FindChild(FindChild(root, "Media Tab"), "Audio Settings");
                CheckTrue(auBox != null, "音频页的 `Audio Settings` 组在（下面 12 条都要它）");
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
                    {
                        CheckNearPx(OrigPxY(auLb) - auLblH[i] * 0.5f, auTop[i] - 13.5f,
                                    $"第 {i + 1} 行 `Label` 的**顶** = 行顶 − 13.5"
                                    + $"（旧摆法把它摆在行顶 {auTop[i]:F3} ⇒ 差 13.5px）");
                        // ---- 🆕 2026-10-20（`A1201`）：三行 `Label` 的**框宽**（= 折行宽）----
                        //  🔴 **为什么单开这一条**：上面 `CheckAutoFit(…, boxW)` 断的是「**渲出来**的字 ≤ 框」
                        //    —— 而 `Music` 那一行的字本来就短（42px 档下 ≪ 框宽），**框被放宽一倍照样绿**
                        //    （`A1201` 报的就是这个：实现传整行、期望值也写整行 ⇒ 假绿）。
                        //    本仓先例 = `Editor/CollectionScene.cs:4259-4277`（`Back` 那颗钮的文字框宽
                        //    150 与 132.86 中心只差 0.25px ⇒ 必须**单独断框宽**）。
                        //  ⚠️ **量法**：读**那颗 TMP 的 `rectTransform.sizeDelta.x`** —— 那正是
                        //    `SetWrapWidth` / `SetAutoFitBox` 真写进去的值（`Label.SetWrapWidth` →
                        //    `TmpFont.SetWrapWidthRect`：`sizeDelta = (width, 0)`），× 108 换成画布 px
                        //    （本窗那 0.9 **烘进矩形**、节点 `lossyScale == 1`，见上面 `slider_vol` 那条前提）。
                        //    ⛔ **不是**读我们传进 `SettingsWindow.Text` 的实参、⛔ 更不是读
                        //    `SettingsWindow.AuLabelAnchorWs` 再算一遍（那是被测实现 = 自证）。
                        //    判据 = 原版那一颗自己的**锚点跨度 × 行宽**（本数组上面那段逐颗写了算式）。
                        //  🧨 **改坏法**：把 `Shell/SettingsWindow.cs` 里 `Text(…, boxW, …)` 那一格的
                        //    `boxW` 换回 `AuR - AuL`（整行）⇒ 第 1 行读 **615.77**、原版 307.89 ⇒ **红**。
                        var auLbTmp = auLb.GetComponentInChildren<TMPro.TextMeshPro>(true);
                        CheckNearPx(auLbTmp != null ? auLbTmp.rectTransform.sizeDelta.x * 108f : -1f,
                                    auLblWpx[i],
                                    $"★ A1201：第 {i + 1} 行 `Label` 的**框宽**（= 折行宽）= {auLblWpx[i]:F2}px"
                                    + (i == 0
                                       ? "（原版那一颗锚 `aMax.x = **0.5**` ⇒ 半行 = 行宽 684.195 × 0.5 × 0.9）"
                                       : "（原版那一颗锚 `aMax.x = 1` ⇒ 整行 = 行宽 684.195 × 0.9）"));
                    }
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
            // 🔴 **2026-10-19（`A1186` 裁定 ①/②）换路**：这一页**在页签栏里没有键**了
            //   （裁定 ①：栏里只放原版那 5 格）⇒ 进它要**照玩家走的那条路**：先到 `General` 页、
            //   再点那颗 `Online Button`（裁定 ②）。⛔ 别改成直调 `win.OpenTab(...)` —— 那样就**不验入口**了。
            Click(Bar(root), "General");
            Click(FindChild(root, "General Tab"), "Online Button");
            Check(win.Current, SettingsTab.Online, "（前提）入口钮把这一页切出来了（下面这一节才有对象可量）");
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

            // ================================================================
            // 🆕 **2026-10-19（波 1b 断言宿主）**：`Shell/SettingsWindow.cs` 接进语言表的那批字 —— **两语档各断一次**
            // ================================================================
            // 判据 = `资料/普查产出_第五会话/交件_波1b_设置窗接线.md` ①（逐行清单）
            //       + `资料/普查产出_第五会话/查证_23双语键盘点.md` 表 B1（每处该用哪条键）。
            //
            // 🔴 **为什么要单开一节、为什么每处都要两语档**（**灭自证**）：本波把「四页签 / 四个页标题 /
            //   图像页 6 处 / 联机页 9 处」的字从**字面量**换成了 `Loc.T(键)`。只断「当前档 == `Loc.T(键)`」
            //   **不够** —— 实现若被改回写死、而期望值也一起改回写死 ⇒ **两处一起变绿**（本工程那条系统性毛病）。
            //   ⇒ 每一处**中/英各断一次**，并用 `Loc.HasCjk` 钉住「两档的字真的不是同一串」
            //   （形状照本文件 `AutoZoom` 那条 `:1478` 的先例）。
            // 🔴 **两档各断一次**还有一层用处：上一版那些断言**只在一种语言下看着对**
            //   （例：写死英文 `"FPS limit"` 在英文档恰好绿、中文档才红）—— 单档的写法分不出这种「恰好相等」。
            //
            // ⚠️ **只切内存里的语言**（`Loc.PersistOverride` 挡住写盘），收尾**逐值放回**（同本文件其余几处）。
            // ⚠️ 节点名**一个都没动**（上游逐条核过 ⇒ `FindChild` / `Click` 照旧靠节点名）；本节断的全是**字**。
            // ⚠️ 取节点一律走**闭包现取**（`Open()` = `Build()` 会把窗根子件整棵重建 ⇒ 存下来的 `Transform`
            //     当场变假 null，见 `Area` / `Bar` 那对助手的注释）。
            // 🔴 **2026-10-19（`A1186` 裁定 ①/②）就地订正标题里的格数**：那一栏现在有 **5 格**，
            //   且 `Online` 那一格换成了「`General` 页上那颗入口钮」（挂同一条键）⇒ 本节这两处都登记。
            Section("波 1b：接进语言表的那批字（五页签 + `Online` 入口钮 / 四个页标题 / 图像页 6 处 / 联机页 8 处）—— 两语档各断一次");
            {
                var langB1 = Loc.Current;
                bool perB1 = Loc.PersistOverride;
                int ssB1 = ssQuality;                        // 进来时那一档（收尾逐值放回）
                Loc.PersistOverride = true;                  // 切档不写盘

                // ---- ① 先把图像页那一列摆成「超采样那一行在」那一态 ----
                //   那一行的字只有它在树上时才量得到（`SetActive(withSS)`）；`RebuildGfxRows()` 会把整列
                //   **重建** ⇒ 下面那些取节点的闭包必须**现取**（⛔ 别在外面先抓一批 `Transform`）。
                ssQuality = SuperSampling.PcQualityIndex;    // = `PC` 档（允许超采样）
                win.RebuildGfxRows();

                // ---- ② 逐处登记（名字 / 取节点 / 键 / 两档是否**该不同**）----
                //   `Differ = false` 只有一条：`Settings/Graphics/Vsync` —— 它的中英两列**逐字都是 `VSync`**
                //   （原版那颗 TMP 本来就是英文，本包唯一一条原版英文文案）⇒ 它断的是「两档字相同、且都 == `Loc.T`」。
                //   ⚠️ 用**匿名对象数组**（不是一个 `string[]` + 一个 `Func[]` 平行表）：四个字段绑在一起，
                //      改一行不会把「名字/键/取法」错位到隔壁那一格上（那会**静默**验错对象）。
                var probes = new[]
                {
                    // —— 页签（`Bar(root)` 下的 5 格；节点名 = 原版 GO 名）——
                    new { What = "页签 `General`", Key = "Settings/General/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(Bar(root), "General")) },
                    new { What = "页签 `Graphics`", Key = "Settings/Graphics/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(Bar(root), "Graphics")) },
                    new { What = "页签 `Audio`", Key = "Settings/Media/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(Bar(root), "Audio")) },
                    // 🆕 **2026-10-19（`A1183` + `A1186` 裁定 ①）**：栏里那第 5 格现在是 **`Support`**
                    //   （原来这一行点的是 `Online` 那一格 —— 它**已经不在栏里了**，直接换掉会**少验一格**）
                    //   ⇒ 两处都登记上：栏里的 `Support` 页签 + `General` 页上那颗 `Online` 入口钮
                    //   （后者挂的是**同一条键** `Settings/Online/Title` —— 正好把裁定 ② 那颗也纳进「两档字真的不同」的判据）。
                    new { What = "页签 `Support`", Key = "Settings/Support/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(Bar(root), "Support")) },
                    new { What = "`General` 页那颗 `Online` 入口钮（栏里已无这一格）", Key = "Settings/Online/Title",
                          Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "General Tab"), "Online Button")) },
                    // —— 四个页标题（每页自己那棵 `Tab Title`；与页签**共用同一条键**，这是设计如此）——
                    new { What = "`General Tab` 的页标题", Key = "Settings/General/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "General Tab"), "Tab Title")) },
                    new { What = "`Graphics Tab` 的页标题", Key = "Settings/Graphics/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Graphics Tab"), "Tab Title")) },
                    new { What = "`Media Tab` 的页标题", Key = "Settings/Media/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Media Tab"), "Tab Title")) },
                    new { What = "`Online Tab` 的页标题", Key = "Settings/Online/Title", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Online Tab"), "Tab Title")) },
                    // —— 图像页 6 处（**节点名一个都没动**，只有画出来那行字走键）——
                    new { What = "图像页 `Quality selector text`", Key = "Settings/Graphics/SelectQuality", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(root, "Graphics Tab"),
                                          "Quality Selector"), "Quality selector text")) },
                    new { What = "图像页 `Small Screen UI` 那一行", Key = "Settings/Graphics/IncreaseUISize", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Graphics Tab"), "Small Screen UI")) },
                    new { What = "图像页 `Use super sampling` 那一行", Key = "Settings/Graphics/EnableSuperSampling", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Graphics Tab"), "Use super sampling")) },
                    new { What = "图像页 `VSync` 那一行", Key = "Settings/Graphics/Vsync", Differ = false,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Graphics Tab"), "VSync")) },
                    new { What = "图像页 `FPS Limit/Title`", Key = "Settings/Graphics/FrameLimit", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(root, "Graphics Tab"),
                                          "FPS Limit"), "Title")) },
                    new { What = "图像页 `FPS Slider` 第 3 格刻度", Key = "Settings/Graphics/UnlimitedFPS", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(FindChild(root, "Graphics Tab"),
                                          "FPS Limit"), "FPS Slider"), SettingsWindow.FpsTickName[2])) },
                    // —— 🆕 2026-10-19（`A1059` / `A1048`）：音频页 **4 处** ——
                    //   🔴 这四处**只在 `BuildAudioPage` 里建一次**、本页没有重建链 ⇒ 挂的是**长链** `_onLabels`
                    //     （图像页那族每滚一格都被 `RebuildGfxRows()` 整批重建 ⇒ 走短链 `_gfxRowLabels`）
                    //     ⇒ 它们在两档之间**真的跟着变**，正是 `A1048` 那条刷新链的判据；而三根音量行标签
                    //     另是 `A1059`「键早在表里、代码画字面量」那四笔里的三笔（第四笔 = 图像页 `Auto zoom`，
                    //     那条在本文件 `A172` 那一节里已两语档断过）。
                    //   ⚠️ 节点名一个都没动**显示字**（显示字由 `keys[]` 那一行走表给、跟着语言变）：
                    //      行容器 = `{"Music","FX","Voiceovers"} + " Container"`（🔴 2026-10-20（`A1202`）
                    //      已照原版 `m_Name` 订正 —— 原写 `Sound Effects` / `Voice-overs`）、
                    //      说明行仍是 `Note`（⛔ 别按显示字找，那是会随语言变的那一层）。
                    new { What = "音频页 `Music Container` 行标签", Key = "MainMenu/Settings/SettingLabel/Music", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(FindChild(root, "Media Tab"),
                                          "Audio Settings"), "Music Container"), "Label")) },
                    new { What = "音频页 `FX Container` 行标签", Key = "MainMenu/Settings/SettingLabel/SoundFx", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(FindChild(root, "Media Tab"),
                                          "Audio Settings"), "FX Container"), "Label")) },
                    new { What = "音频页 `Voiceovers Container` 行标签", Key = "Settings/Media/VoiceOvers", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(FindChild(root, "Media Tab"),
                                          "Audio Settings"), "Voiceovers Container"), "Label")) },
                    new { What = "音频页 `Note` 说明行", Key = "Settings/Media/AudioMixerNote", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(root, "Media Tab"), "Note")) },
                    // —— 联机页 9 处（两颗角色钮 / 测外网 / 两个标签 / 刷新 / 保存 / 检查连接）——
                    new { What = "联机页 `Role Host` 钮上的字", Key = "Settings/Online/RoleHost", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(root, "Online Tab"),
                                          "Role Host"), "Text")) },
                    new { What = "联机页 `Role Client` 钮上的字", Key = "Settings/Online/RoleClient", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(root, "Online Tab"),
                                          "Role Client"), "Text")) },
                    new { What = "联机页 `Echo Button` 钮上的字", Key = "Settings/Online/TestPublicIp", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(FindChild(root, "Online Tab"),
                                          "Echo Button"), "Text")) },
                    new { What = "联机页 `IP Label`", Key = "Settings/Online/IpLabel", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(win.HostBlock, "IP Label")) },
                    new { What = "联机页 `Password Label`", Key = "Settings/Online/PasswordLabel", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(win.HostBlock, "Password Label")) },
                    new { What = "联机页 `Refresh` 钮上的字", Key = "Settings/Online/Refresh", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(win.HostBlock, "Refresh"), "Text")) },
                    new { What = "联机页 `Save Button` 钮上的字", Key = "Settings/Online/Save", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(win.HostBlock, "Save Button"), "Text")) },
                    new { What = "联机页 `Check Button` 钮上的字", Key = "Settings/Online/CheckConnection", Differ = true,
                          Find = (System.Func<Transform>)(() => FindChild(FindChild(win.ClientBlock, "Check Button"), "Text")) },
                };

                // ---- ③ 两语档各断一次（⛔ 每一处都要在这一趟里被断到，别只断一档）----
                string[] zhG = new string[probes.Length], enG = new string[probes.Length];
                var b1Langs = new[] { AvailableLanguages.Chinese, AvailableLanguages.English };
                for (int L = 0; L < b1Langs.Length; L++)
                {
                    bool zh = b1Langs[L] == AvailableLanguages.Chinese;
                    Loc.SetLanguage(b1Langs[L]);
                    win.RefreshTexts();                      // 换语言只在内存里（`PersistOverride` 挡着写盘）
                    for (int i = 0; i < probes.Length; i++)
                    {
                        // ⚠️ 先核「键**在表里**」：键名写错时 `Loc.T` 会**把键名原样返回**
                        //    （`Loc.T` 的缺键语义）⇒ 那样「拿键名当期望值」可能**看着对**。这一条把它挡住。
                        CheckTrue(Loc.HasEntry(probes[i].Key),
                                  $"（波 1b 前提）键 `{probes[i].Key}` **在表里**（⛔ 缺键时 `Loc.T` 返回键名本身）");
                        var t = probes[i].Find();
                        string got = t != null ? TextOf(t) : null;
                        if (zh) zhG[i] = got; else enG[i] = got;
                        if (t == null)
                        {
                            CheckTrue(false, $"（波 1b 前提）`{probes[i].What}` 的**节点**取得到"
                                           + " —— 取不到 = 下面那条等于没验（`FindChild` 返 null）");
                            continue;
                        }
                        Check(got, Loc.T(probes[i].Key),
                              $"★（波 1b · {(zh ? "中文档" : "英文档")}）`{probes[i].What}` 上的字 = "
                            + $"`Loc.T(\"{probes[i].Key}\")`（实得「{got}」）"
                            + "；改坏法：调用点写死字面量 ⇒ 另一档红（写死英文则在中文档红）");
                    }
                }

                // ---- ④ **灭自证**：两档的字**真的不是同一串**（+ 中文那列含汉字、英文那列不含）----
                //   只断单档的写法在「实现改回写死 + 期望值也改回写死」时**两边一起变绿** ⇒ 必须有这一条。
                for (int i = 0; i < probes.Length; i++)
                {
                    if (probes[i].Differ)
                    {
                        CheckTrue(zhG[i] != null && enG[i] != null && zhG[i] != enG[i],
                                  $"🔴（波 1b 灭自证）`{probes[i].What}` 两档的字**真的不同**"
                                + $"（中「{zhG[i]}」/ 英「{enG[i]}」）"
                                + " —— 把调用点与期望值**一起**改回写死 ⇒ 这一条红（只断单档时那种改法全绿）");
                        if (zhG[i] == null || enG[i] == null) continue;
                        // ⚠️ `Loc.HasCjk` 的判定区间**含 `U+3000–U+303F` 与 `U+FF00–FFEF`**（全角标点/全角字母
                        //   也算）⇒ 英文列里**不许放全角**（波 0b3 为这一条把两处全角空格改成半角）。
                        CheckTrue(Loc.HasCjk(zhG[i]) && !Loc.HasCjk(enG[i]),
                                  $"🔴（波 1b 灭自证）`{probes[i].What}`：中文那列**含汉字**、英文那列**不含**"
                                + $"（`Loc.HasCjk`：中={Loc.HasCjk(zhG[i])} / 英={Loc.HasCjk(enG[i])}）"
                                + " —— 这一条把「两档只是随手拼了两串」与「真的读了两列」分开");
                    }
                    else
                    {
                        CheckTrue(zhG[i] != null && enG[i] != null && zhG[i] == enG[i],
                                  $"（波 1b）`{probes[i].What}` 是本节**唯一一条中英同字**的键"
                                + $"（两档都是「{zhG[i]}」）—— 它接词条的意义是「换语言时跟着刷新」，⛔ 不是「换字」");
                    }
                }

                // ---- ④′ 🔴（🆕 2026-10-19 · `A1048`）联机页那行说明 = **两条键拼出来的** ----
                //   为什么单开一小段：上面那张 probe 表**一格只装得下一条键**，而这一行是
                //   `Loc.T(TitleNote) + "\n" + Loc.T(TitleNoteBody)` —— 它正是 `_onLabels` 那条链**存 `Action`
                //   而不是存 `Keyed`** 的两个理由之一（另一个 = 占位符那颗字不在 `Label` 上）。
                //   ⇒ 这一条要是没接上（或 `RefreshTexts()` 漏扫那条链），玩家看到的是**旧语言**的说明行，
                //     而上面那张表**照不到它**。判据 = 切档 + `RefreshTexts()` 之后**整句**跟上。
                {
                    var noteOn = FindChild(FindChild(root, "Online Tab"), "Note");
                    CheckTrue(noteOn != null, "（`A1048` 前提）`Online Tab > Note` 取得到（下面才有对象可量）");
                    CheckTrue(Loc.HasEntry("Settings/Online/TitleNote") && Loc.HasEntry("Settings/Online/TitleNoteBody"),
                              "（`A1048` 前提）拼这一行的**两条键**都在表里"
                            + "（⛔ 缺键时 `Loc.T` 返回键名本身 ⇒ 下面那条会拿键名当期望值、假绿）");
                    string gotZh = null, gotEn = null;
                    for (int L = 0; L < b1Langs.Length; L++)
                    {
                        bool zh = b1Langs[L] == AvailableLanguages.Chinese;
                        Loc.SetLanguage(b1Langs[L]);
                        win.RefreshTexts();
                        string got = noteOn != null ? TextOf(noteOn) : null;
                        string want = Loc.T("Settings/Online/TitleNote") + "\n" + Loc.T("Settings/Online/TitleNoteBody");
                        if (zh) gotZh = got; else gotEn = got;
                        Check(got, want,
                              $"★（`A1048` · {(zh ? "中文档" : "英文档")}）联机页那行说明 = `Loc.T(\"Settings/Online/TitleNote\") "
                            + "+ \"\\n\" + `Loc.T(\"Settings/Online/TitleNoteBody\")`（实得「{got}」）"
                            + "；改坏法：这一行没登记进 `_onLabels`（或 `RefreshTexts()` 漏扫那条链）"
                            + " ⇒ 换语言后它**停在旧语言** ⇒ 另一档红");
                    }
                    CheckTrue(gotZh != null && gotEn != null && gotZh != gotEn,
                              $"🔴（`A1048` 灭自证）这行字两档**真的不同**（中「{gotZh}」/ 英「{gotEn}」）"
                            + " —— 把那条链与期望值**一起**改回「建一次就不管」⇒ 上面那条与这一条绿不了");
                    CheckTrue(gotZh != null && gotEn != null && Loc.HasCjk(gotZh) && !Loc.HasCjk(gotEn),
                              "🔴（`A1048` 灭自证）…而且中文那列含汉字、英文那列不含（`Loc.HasCjk`）"
                            + " —— 这一条把「确实读了两列」与「两档只是随手拼了两串」分开");
                    // 旁证 —— ⛔ **不能单独当判据**（登记了不生效照样能过这一条；真判据 = 上面那些字真的变了）
                    CheckTrue(win.OnLabelCount >= 15,
                              $"（`A1048` 旁证）`_onLabels` 链上挂着 **{win.OnLabelCount}** 条"
                            + "（四页 `Build()` 里登记的那一批：音频页 4 + 联机页 12 + 图像页 2 上下）");
                }

                // ---- ④″ 🔴 **反向断**（⛔ 防「顺手把不该接的也接上」）：`FPS Slider` 前两格是**纯数字** ----
                //   判据 = 这两格**表 A 明写「不建键」**（原版那两颗 TMP 印的就是 `30` / `60`，与语言无关）
                //   ⇒ 两档下都必须逐字是 `30` / `60`。谁把它们接进词条（或接错键）⇒ 这一条红。
                {
                    var fslider = FindChild(FindChild(FindChild(root, "Graphics Tab"), "FPS Limit"), "FPS Slider");
                    CheckTrue(fslider != null, "（反向断前提）`Graphics Tab > FPS Limit > FPS Slider` 取得到");
                    var t30 = fslider != null ? FindChild(fslider, SettingsWindow.FpsTickName[0]) : null;
                    var t60 = fslider != null ? FindChild(fslider, SettingsWindow.FpsTickName[1]) : null;
                    CheckTrue(t30 != null && t60 != null,
                              "（反向断前提）`30 FPS` / `60 FPS` 两颗刻度节点都在（⛔ 按时**节点名**找，不是按显示字）");
                    for (int L = 0; L < b1Langs.Length; L++)
                    {
                        bool zh = b1Langs[L] == AvailableLanguages.Chinese;
                        Loc.SetLanguage(b1Langs[L]);
                        win.RefreshTexts();
                        CheckTrue(TextOf(t30) == "30" && TextOf(t60) == "60",
                                  $"（反向断 · {(zh ? "中文档" : "英文档")}）`FPS Slider` 前两格仍是**纯数字**"
                                + $"（实得「{TextOf(t30)}」/「{TextOf(t60)}」）—— 这两格不建键（表 A），"
                                + "谁把它们接进词条/接错键 ⇒ 这一条红");
                    }
                }

                // ---- ⑤ 两条「点一下才拼」的 `_flash`：整句**跟着语档**（`{0}` 是运行期值）----
                //   上游没给这两条配断言（它只把字面量换掉了）⇒ 本波补上：**两语档各断一次整句**。
                {
                    // ① VSync 那条：`{0}` = `MainMenu/General/{On,Off}`（本波新接的两条键）。
                    //    ⚠️ 点**真会**翻 `QualitySettings.vSyncCount` ⇒ 走 `VSyncSetterOverride` 挡住
                    //    （自检不许改工程设置 —— 同 `:1083` 那条）。⚠️ `next` 只看**当前** `vSyncCount`
                    //    ⇒ 反复点得到同一个 flash（两档各点一次也不会漂）。
                    string[] vsFlash = new string[2];
                    for (int L = 0; L < b1Langs.Length; L++)
                    {
                        bool zh = b1Langs[L] == AvailableLanguages.Chinese;
                        Loc.SetLanguage(b1Langs[L]);        // 🔴 先切档再点 —— `_flash` 是**点那一刻**按当前语档拼的
                        int vNow = QualitySettings.vSyncCount, vAsked2 = -1;
                        SettingsWindow.VSyncSetterOverride = cc => vAsked2 = cc;
                        Click(FindChild(FindChild(root, "Graphics Tab"), "VSync"), "Hit");
                        SettingsWindow.VSyncSetterOverride = null;
                        int vNext = vNow > 0 ? 0 : 1;
                        Check(vAsked2, vNext, $"（波 1b）点 `VSync` 那一行 ⇒ 要求翻成 {vNext}（{vNow} → {vAsked2}）");
                        string wantVs = string.Format(Loc.T("Settings/Graphics/Flash/Vsync"),
                                                      vNext > 0 ? Loc.T("MainMenu/General/On")
                                                                : Loc.T("MainMenu/General/Off"));
                        Check(win.Flash, wantVs,
                              $"★（波 1b · {(zh ? "中文档" : "英文档")}）点完那句 `_flash` = "
                            + "`string.Format(Loc.T(\"Settings/Graphics/Flash/Vsync\"), "
                            + "Loc.T(\"MainMenu/General/{On,Off}\"))`"
                            + $"（实得「{win.Flash}」）—— 这是 `MainMenu/General/{{On,Off}}` 两条键唯一的消费点");
                        CheckNear(QualitySettings.vSyncCount, vNow, 0.01f,
                                  "（波 1b）…🔴 自检没真去改 `vSyncCount`（注入点挡住了）");
                        vsFlash[L] = win.Flash;
                    }
                    CheckTrue(vsFlash[0] != null && vsFlash[1] != null && vsFlash[0] != vsFlash[1],
                              $"🔴（波 1b 灭自证）那条 `_flash` 两档真的不同（中「{vsFlash[0]}」/ 英「{vsFlash[1]}」）");

                    // ② FPS 那条：`{0}` = `FpsText()`（键 `Settings/Graphics/FpsText/{Unlimited,Value}`）。
                    //    档 2 ⇒ `Application.targetFrameRate = −1` ⇒ 走 `Unlimited` 那一支。
                    //    🔴 **自检要还回去**（同 `:1260-1262` 那两行：不把进程帧率留在别的值上）。
                    string[] fpsFlash = new string[2];
                    int fSaveB1 = Application.targetFrameRate;
                    for (int L = 0; L < b1Langs.Length; L++)
                    {
                        bool zh = b1Langs[L] == AvailableLanguages.Chinese;
                        Loc.SetLanguage(b1Langs[L]);
                        win.SetFpsIndex(2, true);            // 档 2 = 不限帧（真 fire —— 才会拼 `_flash`）
                        Check(Application.targetFrameRate, -1, "（波 1b）档 2 ⇒ `targetFrameRate = −1`（不限帧）");
                        string wantFps = string.Format(Loc.T("Settings/Graphics/Flash/Fps"),
                                                       Loc.T("Settings/Graphics/FpsText/Unlimited"));
                        Check(win.Flash, wantFps,
                              $"★（波 1b · {(zh ? "中文档" : "英文档")}）改档那句 `_flash` = "
                            + "`string.Format(Loc.T(\"Settings/Graphics/Flash/Fps\"), "
                            + "Loc.T(\"Settings/Graphics/FpsText/Unlimited\"))`"
                            + $"（实得「{win.Flash}」）");
                        fpsFlash[L] = win.Flash;
                    }
                    CheckTrue(fpsFlash[0] != null && fpsFlash[1] != null && fpsFlash[0] != fpsFlash[1],
                              $"🔴（波 1b 灭自证）那条 `_flash` 两档真的不同（中「{fpsFlash[0]}」/ 英「{fpsFlash[1]}」）");
                    Application.targetFrameRate = fSaveB1;   // 🔴 进程帧率逐值放回
                    win.SetFpsIndex(SettingsWindow.FpsIndexOfTarget(fSaveB1), false);
                    Check(Application.targetFrameRate, fSaveB1,
                          $"（波 1b 收尾）`Application.targetFrameRate` 放回本节进来时那一档（{fSaveB1}）");
                }

                // ---- ⑥ `_gfxRowLabels` 那条**短链不许漏**（`GfxRowLabelCount` 是唯一测得出它的只读口）----
                //   本波把它从 1 改成 **6**（`Small Screen UI` / `Auto Zoom` / `Use super sampling` / `VSync`
                //   四行各 1 + `FPS limit` 那一行 **2** = 行标题 + 第 3 格刻度）。
                //   泄漏形态 = `RebuildGfxRows()` 挂在 `_gfxScroll.OnChanged` 上（**每滚一格都跑**）却在重建前
                //   **不清链** ⇒ 每次多几条指向刚被销毁的 `Label` 的闭包（不报错、无界增长 = 静默）。
                int gfxN0 = win.GfxRowLabelCount;
                win.RebuildGfxRows();
                int gfxN1 = win.GfxRowLabelCount;
                win.RebuildGfxRows();
                int gfxN2 = win.GfxRowLabelCount;
                Check(gfxN1, gfxN0, $"★（波 1b）`RebuildGfxRows()` **一次**之后短链长度不变（{gfxN0} → {gfxN1}）"
                                  + " —— 涨了就是「重建前没清链」那个静默泄漏（挂 `OnChanged` ⇒ 每滚一格都跑）");
                Check(gfxN2, gfxN1, $"★（波 1b）…**再重建一次**也不涨（{gfxN1} → {gfxN2}）");
                CheckTrue(gfxN0 == 6, $"★（波 1b）…而且它现在 = **6**（实得 {gfxN0}）—— "
                                    + "四行标签各 1 + `FPS limit` 那行 2（行标题 + 第 3 格刻度）；"
                                    + "出处 = `Shell/SettingsWindow.cs` 的 `GfxRowLabelCount` doc");

                // ---- ⑦ 语言那两句 `_flash`（`Settings/General/Flash/Language` + `Settings/General/LangHasNoTable`）----
                //   上游 ①-D 也改了这两条（`Shell/SettingsWindow.cs:1643-1644`），**但一条断言都没有** ⇒ 本波补。
                //   🔴 **只有「真的换了一档」才拼这句** —— `ChooseLanguage` 在「点的就是当前那一档」时
                //   **提前 return**、连 `_flash` 都不碰 ⇒ 要两档各断一次，必须**从另一种语言切过去**
                //   （⛔ 不能原地断：原地那条会拿上一轮留下来的旧 flash 去比）。
                //   ⚠️ 三步都是**真的换档**（先把语言摆到另一种，再点目标那一行）⇒ 与玩家当前设置无关。
                {
                    int iZh = System.Array.IndexOf(Loc.Languages, AvailableLanguages.Chinese);
                    int iEn = System.Array.IndexOf(Loc.Languages, AvailableLanguages.English);
                    int iNoOwn = System.Array.FindIndex(Loc.Languages, l => !Loc.HasOwnText(l));
                    CheckTrue(iZh >= 0 && iEn >= 0 && iNoOwn >= 0,
                              $"（波 1b 前提）下拉里找得到 `Chinese`（第 {iZh} 行）/ `English`（第 {iEn} 行）/ "
                            + $"第一款**本地没文案**的（第 {iNoOwn} 行）—— 下面三条才有对象可点");
                    var selRowB1 = FindChild(FindChild(root, "General Tab"), "Language Selector");
                    CheckTrue(selRowB1 != null, "（波 1b 前提）`General Tab > Language Selector` 在（拿它开列表）");
                    var langFlash = new string[2];
                    if (iZh >= 0 && iEn >= 0 && iNoOwn >= 0 && selRowB1 != null)
                    {
                        // 那句 flash 的**前半截**（两个占位现取当前语档 —— 与实现同一条公式，但两边都是
                        // 从 `Loc` 的公开面现取，⛔ 不是把中/英任一串抄进断言）。
                        // ⚠️ 用**局部函数**而不是在外面算一次：`Loc.Current` 每次点完都变了，期望值必须**现算**。
                        string FlashLangHead()
                        {
                            return string.Format(Loc.T("Settings/General/Flash/Language"),
                                                 Loc.LanguageName(Loc.Current), Loc.Current);
                        }
                        // ① 中文档：先摆到 `English`，再点 `Chinese` 那一行 ⇒ 那句 flash 是**中文**的
                        //    （`_flash` 是 `SetLanguage` **之后**才拼的 ⇒ 印的是**新**语档那一列）
                        Loc.SetLanguage(AvailableLanguages.English);
                        win.RefreshTexts();
                        Click(selRowB1, "LanguageHit");
                        CheckTrue(win.LangListOpen, "（波 1b 前提）点一下框 ⇒ 那 12 行列表开出来了");
                        Click(win.LangRowHit(iZh));
                        Check(Loc.Current, AvailableLanguages.Chinese, "（波 1b）点 `Chinese` 那一行 ⇒ 语言切过去");
                        langFlash[0] = win.Flash;
                        Check(win.Flash, FlashLangHead(),
                              "★（波 1b · 中文档）切到中文那句 `_flash` = `string.Format(Loc.T(\"Settings/General/Flash/Language\"), "
                            + "语言名, 枚举)`" + $"（实得「{win.Flash}」）");
                        CheckTrue(Loc.HasOwnText(Loc.Current) && win.Flash == FlashLangHead(),
                                  "（波 1b）…中/英这两档**本地有文案** ⇒ 那句 flash **不该**带后半句"
                                + "（键 `Settings/General/LangHasNoTable`）");
                        // ② 英文档：反向再切一次（此刻是中文 ⇒ 点 `English` 那一行）
                        Click(selRowB1, "LanguageHit");
                        Click(win.LangRowHit(iEn));
                        Check(Loc.Current, AvailableLanguages.English, "（波 1b）再点 `English` 那一行 ⇒ 切回去");
                        langFlash[1] = win.Flash;
                        Check(win.Flash, FlashLangHead(),
                              $"★（波 1b · 英文档）同一条键按当前语档拼（实得「{win.Flash}」）");
                        CheckTrue(langFlash[0] != null && langFlash[1] != null && langFlash[0] != langFlash[1],
                                  $"🔴（波 1b 灭自证）那句 `_flash` 两档真的不同"
                                + $"（中「{langFlash[0]}」/ 英「{langFlash[1]}」）");
                        // ③ 本地没文案那一档 ⇒ **多出后半句**（键 `Settings/General/LangHasNoTable`）
                        Click(selRowB1, "LanguageHit");
                        Click(win.LangRowHit(iNoOwn));
                        Check(Loc.Current, Loc.Languages[iNoOwn],
                              $"（波 1b）点第 {iNoOwn} 行 ⇒ 切到 `{Loc.Current}`（本地**没有**这一套文案）");
                        CheckTrue(!Loc.HasOwnText(Loc.Current),
                                  "（波 1b）…而且这一刻确实是「本地没文案」那一档（`Loc.HasOwnText` = false）");
                        string head3 = FlashLangHead();
                        Check(win.Flash, head3 + Loc.T("Settings/General/LangHasNoTable"),
                              "★（波 1b）那一档 ⇒ 那句 `_flash` **多出后半句**（键 `Settings/General/LangHasNoTable`，"
                            + "值里**自带前导全角空格 U+3000** ⇒ 调用点 ⛔ 没再补一个）" + $"（实得「{win.Flash}」）");
                        // 🔴 判别式：只印前半句也行的话，上一条与「漏了后半句」就分不开了
                        CheckTrue(win.Flash != null && win.Flash != head3 && win.Flash.Length > head3.Length,
                                  "🔴（波 1b）…而且它**确实比前半句长**（后半句真的拼上去了；"
                                + "改坏法：漏掉 `LangHasNoTable` 那一截 ⇒ 这一条红）");
                    }
                    // 还原：语言放回本节入口那一档（⛔ 别把界面留在别的语档上）
                    Loc.SetLanguage(langB1);
                    win.RefreshTexts();
                }

                // ---- ⑧ 收尾：图像页那一列摆回进来时的档位 + 语言逐值放回 ----
                ssQuality = ssB1;
                win.RebuildGfxRows();
                Loc.SetLanguage(langB1);
                Loc.PersistOverride = perB1;
                win.RefreshTexts();
                CheckTrue(Loc.Current == langB1, $"收尾：语言放回本节开始前那一档（{langB1}；⛔ 自检不许改玩家的真设置）");
            }

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
            //   · 量具 = `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Main Menu Settings Window"
            //     --depth 20 --no-sprite --no-layout`（逐颗印 `字号=` / `基准=` / `auto[min~max]`）
            //   · 全 prefab 共 **102 颗 TMP**：`m_enableAutoSizing = 1` 的 **100 颗**，其 `m_fontSizeBase`
            //     **只取 {12, 14, 26, 35, 36, 37, 44} 这 7 个值**；另 2 颗自适应**关**着
            //     （`Debug button text` / `Account Tab > Player Id`），它们的 `m_fontSize` = **{36, 40}**
            //   ⇒ 屏幕上只可能是【原版那一颗的 `base`（自适应关着时 = `m_fontSize`）】× **0.9**。
            //   🔴 **2026-10-10 订正（A207）**：本行原写「常规 **40**（`FontLabel`/`FontButton`）」——
            //      **「行标签」那一族（开关行 / 音轨行 / `Quality selector text`）的原版是 42、不是 40**，
            //      已改用新常量 `SettingsWindow.FontRowLabel`（亲读 `Vsync/Label` = `m_fontSize 42` 作证）。
            //      `FontLabel`（40）**原地留着**，它现在只服务两个判据未定的站（`Quality Value` + 我们自己的联机页）。
            //      ⚠️ 但 **42 不再是这一扫的允许值** —— 那一族的 `base` 是 **36**（见下面 2026-10-09 的删值说明）。
            //
            // 🔴 **期望值全写字面量**：⛔ 不写 `SettingsWindow.RootScale` / `PageTitleFontPx` / `FontSmall`
            //    —— 那是**被测实现里的常量**，拿它算期望就是同式自证（通则 → `A131_自证通则.md`）。
            // 🔴 **2026-10-09（第十会话 · `D2` 诊断 · δ）本条量的是【字段】`m_fontSizeBase`**
            //    （`Label.FontSizeBase`，一律经 `Label.FontSizeToPx` 折成画布 px），⛔ **不是 `FontPxNow`**：
            //    本批给 25 站接上 autosize 之后，`FontPxNow` 是 **TMP 的收敛值**（可以低到 `min`；
            //    实测 10.80 / 24.06 / 23.54 …），与「原版值 × 0.9」**本来就不该再相等** ——
            //    原来那条读 `FontPxNow` 是**前提过期**（诊断 `普查产出_第十会话/D2_全跑8红诊断.md` §1 第 2 行）。
            //    `m_fontSizeBase` 才是那条不变式：**自适应关着时 TMP 让 `m_fontSizeBase ≡ m_fontSize`**
            //    （`TMP_Text.fontSize` 的 setter：`if (!m_enableAutoSizing) m_fontSizeBase = m_fontSize;`，
            //    `TMP_Text.cs:467`）⇒ 两条入口（接了自适应 / 没接）读到的是同一个口径。
            //    ⛔ 量法**不是** `GlyphHeightWorld`/`CapHeightWorld`（那两个是**回读传入值**的伪测量，见 `已知的坑.md`）。
            //    ⚠️ 另加一条「实得必须落在钳位区间里」（见下面扫描 ④）—— **不是**把这条放宽，两条合起来比原来更强。
            Section("A171：本窗文字字号 = 原版那一颗的 `m_fontSizeBase`（自适应关着时 = `m_fontSize`）× 根上那层 0.9（**全窗一起缩**）");
            {
                // 🔴 **2026-10-09（第十会话 · `D2` 诊断 · δ）本表重写**：旧表的 13 个值**大多是那几颗的
                //    `m_fontSize` × 0.9**（43 / 42 / 38 / 32 / 31.05 / 30 / 28 …），而本批给那些站接上 autosize 之后
                //    这一扫读的是 **`base`** ⇒ 那 7 个值**已不可达**（38.7 · 37.8 · 34.2 · 28.8 · 27.945 · 27 · 25.2）。
                //    ⛔ **不是「扫不过就删」**：留着它们 = 把「某颗的 `autoBasePx` 传回 0 ⇒ base 退回调用方那一档
                //    （= 旧标称）」这档改坏法**静默变绿**（A1181 那一节第 4 条钉的正是这一档）⇒ **必须删**。
                //    现值 = **{原版 7 个 base} ∪ {原版 2 个「自适应关」的 fs} ∪ {18（被共用闸挡住那一颗）} ∪ {34（`OneText` 那一档）}**，
                //    每一个都逐颗回原版取过出处（⛔ 一个来源不明的值都不许加；⛔ 加值 = **放宽**这一扫，不是修 bug）：
                //      55        → 49.5   `{General,Media,Graphics,Account,Online} Tab > Tab Title`
                //                        （`fs 55 · base 55 · auto[4~55]`；Online 那颗是 `auto[10~55]`）
                //      **44**    → 39.6   `Account Tab > Subscribe Newsletter`（`fs 40 · base 44 · auto[32~40]`；
                //                        **base > max** —— TMP 渲染时夹到 max，但**字段就是 44**）
                //      **40**（自适应关） → 36   `Account Tab > Player Id`（`字号=40.0 基准=40.0 auto=OFF`）——
                //                        我们这一族（`FontLabel`/`FontButton` 40 · 两个输入框 40）原版**也没开**自适应
                //      37        → 33.3   `EmailText` / `PasswordText` / `Error Message`（账号页 3 颗 + 登录窗 3 颗）
                //      **36**    → 32.4   「行标签」家族：`… Toggle > Label` ×7 · `FPS Limit > Title` · FPS 三刻度 ·
                //                        `Quality selector text` · `Audio Settings > {Music,FX,Voiceovers} Container > Label` ·
                //                        `SelectLanguageText` · `Reset|Forgot Password > Text`（**base 36 ≠ 它的 fs 42/32**）
                //      35        → 31.5   `… Tab Buttons > */Label/Tab Toggle Title` ×5（`fs 35 · base 35`）
                //      **34**    → 30.6   本窗 `FontSmall` 那一族（音频 `Note` + 联机页 `Status`/`Note`/`Refresh`）。
                //                        ⚠️ **联机页是我们自加的**（原版没有这一页）⇒ **没有原版对应件**；取 34 是因为
                //                        本窗原版确有这个字号（`OneText`：`字号=34.0 基准=36.0 auto[18~34]`），
                //                        且「联机页沿用本窗既有档」是既有口径 —— **这是我们的选择，不冒充原版**
                //      **26**    → 23.4   `General Tab > VersionText`（`fs 28 · base 26 · auto[1~28]`）
                //      18        → 16.2   `LanguagesDropdown > Label`（原版 `fs 18 · base 14 · auto[18~40]`；
                //                        我们这颗 `fs 18 == min 18` ⇒ 共用闸 `fontPx > autoMinPx` 过不去 ⇒
                //                        **字段停在 18**，见 `Shell/SettingsWindow.cs` 那个调用点与 A1181 那一节）
                //      **14**    → 12.6   `… Template > Viewport > Content > Item > Item Label`（语言下拉 12 行；
                //                        `fs 30 · base 14 · auto[18~40]`）
                //      **12**    → 10.8   **所有钮那一族**（`Button > Button Text`：`Bottom Buttons` ·
                //                        `Social Media Links` · 账号页 / 登录窗那几颗；原版 `base` **恒 12**）
                //    ⚠️ 第一列带 **粗体** 的 = 「原版那一颗的 `base` ≠ 它的 `fs`」⇒ **只有读字段才分得出来**的那几档。
                float[] wantPx = { 49.5f, 39.6f, 36f, 33.3f, 32.4f, 31.5f, 30.6f, 23.4f, 16.2f, 12.6f, 10.8f };
                // 🔴 上表**只此一份**：断言文案里那个列表由它生成（`D2` §4 记的「数组与文案两份」已收口）
                string wantTxt = "";
                for (int k = 0; k < wantPx.Length; k++) wantTxt += (k > 0 ? "／" : "") + wantPx[k].ToString("0.###");
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
                //    窗根下**每一个** `Label` 的**字体字段**都必须落在上面那 11 个值里（±0.35px）。
                //    ⛔ **故意不点名节点**：点名只盖得住点到的那些；扫全树才抓得住「新加一段字忘了缩」——
                //      `MenuInputField`（联机页两个输入框）就是**绕过** `SettingsWindow.Text` 漏斗的第二个入口，
                //      本批也在它自己那边过了 `RootScale`（`MenuInputField.InputFontPx`）。
                var allLb = root.GetComponentsInChildren<Label>(true);
                int badN = 0, zeroN = 0; string badList = "";
                int autN = 0, autBadN = 0, autSkipN = 0; string autBadList = "", autSkipList = "";
                // 🔴 **2026-10-09**：原来截断在 300 字符 ⇒ 实测 28 个越界**只印得出 17 个名字**
                //    （诊断 `D2` §5 第 3 条：那 11 个看不见的正是最该看的）⇒ 放到 1200，并在文案里写明「印到上限为止」。
                const int TruncLen = 1200;
                for (int i = 0; i < allLb.Length; i++)
                {
                    var lb = allLb[i];
                    // ★ 读的是【字段】`m_fontSizeBase`（⛔ 不是收敛值 `FontPxNow`）—— 见本节头部 2026-10-09 那条
                    float px = Label.FontSizeToPx(lb.FontSizeBase);
                    bool ok = false;
                    for (int k = 0; k < wantPx.Length; k++)
                        if (Mathf.Abs(px - wantPx[k]) <= TolPx) { ok = true; break; }
                    if (!ok)
                    {
                        if (px <= 0f) { zeroN++; }   // 点阵兜底后端（`FontSizeBase` 恒 −1）—— 与「没缩」分开报
                        else
                        {
                            badN++;
                            if (badList.Length < TruncLen) badList += $"{lb.name}={px:F2} ";
                        }
                    }

                    // ④ 🆕 **2026-10-09（第十会话）另加的一条**：开着**自适应**的那些，**实得渲染字号**
                    //    （`FontPxNow`）必须落在**它自己**的钳位区间 `[FontSizeToPx(min), FontSizeToPx(max)]` 里
                    //    （两个字段都经 `Label.FontSizeToPx` 折成画布 px，⛔ 不再乘第二遍 0.9 —— 那层缩放
                    //    `SetAutoFitBox` 写字段时已经过了一次）。它挡的是「`min`/`max` 写错档 / 收敛跑到区间外」。
                    //    ⚠️ **未激活**的 `Label` 只放行「**停在 `base`**」那一档：TMP **不给未激活的对象重排**
                    //       （同族判据 → `CheckAutoFit` 的 `skipRendered` 那一格），那颗的 `fontSize` 停在
                    //       `SetAutoFitBox` 写下的 `base` 上 —— 实测那 5 颗外链钮正是 `10.80 = base`，
                    //       诊断 `D2` §2 也记着「未激活 ⇒ 实得 = base」⇒ **它不是「渲出来的字号」**，
                    //       拿它断区间没有意义。⛔ 但它**不是被静默跳过**：③ 那条照样管着它的字段。
                    if (lb.AutoSizing && px > 0f)
                    {
                        float now = lb.FontPxNow;
                        float lo = Label.FontSizeToPx(lb.FontSizeMin) - TolPx;
                        float hi = Label.FontSizeToPx(lb.FontSizeMax) + TolPx;
                        // 🔴 **2026-10-10（第十会话 · 收口复跑抓到）**：**先给它一次「手动推版面」的机会** ——
                        //   批处理**没有帧循环** ⇒ TMP 不会自己重排，字段可能停在「上一次写入的残留」上。
                        //   `ForceRelayout()` 是**幂等**的（按当前的折行模式与 `[min,max]` 重新收敛一次），
                        //   与 `CheckAutoFit` 里那处**同一套口**。
                        if (!(now >= lo && now <= hi)) { lb.ForceRelayout(); now = lb.FontPxNow; }
                        bool inRange = now >= lo && now <= hi;
                        // ⚠️ **未激活**的 Label：TMP **不给未激活对象重排** ⇒ 那个字段**既不是 base、也不是收敛值**，
                        //   而是**上一次写入的残留**（实测那颗恒关的社交 `Button Text` 停在**标称** `38.70 = 43×0.9`）。
                        //   ⇒ 拿它断区间**没有意义** ⇒ **放行，但【如实印出来】**（⛔ 不是静默跳过）。
                        //   🔑 订正上一版的判据：它原来只放行「**停在 `base`**」那一档（`|now - px| ≤ Tol`），
                        //   实测**太窄** —— 那颗停的是**标称**、不是 base。
                        bool inactive = !lb.gameObject.activeInHierarchy;
                        autN++;
                        if (!inRange)
                        {
                            if (inactive)
                            {
                                autSkipN++;
                                if (autSkipList.Length < TruncLen)
                                    autSkipList += $"{lb.name}={now:F2}∉[{lo:F2},{hi:F2}]（未激活） ";
                            }
                            else
                            {
                                autBadN++;
                                if (autBadList.Length < TruncLen)
                                    autBadList += $"{lb.name}={now:F2}∉[{lo:F2},{hi:F2}] ";
                            }
                        }
                    }
                }
                CheckTrue(allLb.Length >= 15,
                          $"窗根下扫到 **{allLb.Length}** 个 `Label`（≥ 15 这一扫才有意义 —— 扫不到就等于没扫；"
                        + "⛔ 别把这个门槛删掉）");
                CheckTrue(badN == 0,
                          $"★ **全窗 {allLb.Length} 个 `Label` 的字体字段都 = 原版值 × 0.9**"
                        + $"（读的是字段 `m_fontSizeBase`；允许的 {wantPx.Length} 个：{wantTxt}，±{TolPx}px）"
                        + (badN > 0 ? $" —— **有 {badN} 个不在里面**：{badList}"
                                    + (badList.Length >= TruncLen ? "…（名字太多，印到上限为止）" : "") : "")
                        + (zeroN > 0 ? $"；另有 {zeroN} 个 `FontSizeBase` ≤ 0（点阵兜底后端 —— 那是另一回事，"
                                     + "`Label.FontSizeBase` 在点阵后端恒 −1）" : "")
                        + "；改坏法：把 `SettingsWindow.Text` 的 `fs * RootScale` 去掉（或新加一段字直接调"
                        + " `MenuDraw.Text`、没自己过 0.9）⇒ 那一批的字段回到 55/42/40/35/34 ⇒ 这条红；"
                        + "把某颗的 `autoBasePx` 传回 0 ⇒ 字段退回调用方那一档（= 旧标称）⇒ 同样红");
                CheckTrue(autN >= 10,
                          $"…而且这一扫**确实覆盖到自适应站**：全窗 **{autN}** 个 `Label` 开着自适应（≥ 10 才有意义）");
                CheckTrue(autBadN == 0,
                          $"★ 全窗 **{autN}** 个开着自适应的 `Label`，**实得字号都落在各自的钳位区间**里"
                        + $"（`[FontSizeToPx(min), FontSizeToPx(max)]` ±{TolPx}px）"
                        + (autBadN > 0 ? $" —— **有 {autBadN} 个越界**：{autBadList}" : "")
                        + (autSkipN > 0 ? $"；另有 **{autSkipN} 个是【未激活】的、按判据放行**（如实登记、⛔ 不是静默跳过）：{autSkipList}" : "")
                        + " —— 🧨 这一条挡的是「`min`/`max` 写错档 / 收敛跑到区间外」；"
                        + "🔴 **订正（2026-10-10）**：原来这里写「未激活的只认『停在 `base`』那一档」—— **实测太窄**："
                        + "TMP **不给未激活对象重排** ⇒ 那格的字段**既不是 base、也不是收敛值**，而是**上一次写入的残留**"
                        + "（实测那颗恒关的社交 `Button Text` 停在**标称** `38.70 = 43×0.9`）⇒ 拿它断区间没有意义，"
                        + "**整档放行并如实印出来**（③ 那条字段检查照样管着它）");

                // 🆕 **2026-10-10（A207）点名钉那一族「行标签」= 原版 42** ——
                //    ③ 那条全窗扫描**同时允许 36（= 40×0.9）与 37.8（= 42×0.9）** ⇒
                //    「把 `SettingsWindow.FontRowLabel` 合并回 `FontLabel`（40）」这种错**它抓不住**（会静默绿）。
                //    🔴 **2026-10-09（第十会话）就地订正**：③ 从「读实得」改成「读字段 `m_fontSizeBase`」（见本节那两条说明），
                //    而这一族的 **`base` 是 36**（`fs` 42 只是它的标称）⇒ **表里如今只剩 36 那一档、37.8 已删掉**
                //    ⇒ 上面那句话照样成立（③ 分不出 40 与 42）、而且**更成立**：下面这两条读的是 **`m_fontSizeMax` 字段**。
                //    判据 = 原版 `Vsync/Label` 亲读 **`m_fontSize = 42`**（`m_fontSizeMin 29` / `m_fontSizeMax 42` / 折行 1）
                //    —— `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2205799620510384038.json`。
                var vsN = FindChild(root, "VSync");
                var vsLb = vsN != null ? vsN.GetComponentInChildren<Label>() : null;
                CheckTrue(vsLb != null, "`VSync` 那一行在（下面两条才有对象可量）");
                // 🔴 **2026-10-09（第十会话 · `D2` 诊断 · δ）**：本批给这一族**接上了 autosize**（29/42/36·折行1）
                //   ⇒ `FontPxNow` 是 **autosize 的收敛值**（实测 32.40 = `36 × 0.9`），**再也不是** `42 × 0.9 = 37.8`。
                //   ⚠️ **42 是【上限】`m_fontSizeMax`**、**36 才是 `m_fontSizeBase`** ⇒ 这条不变的判据要读 **`FontSizeMax`**
                //   （仍 = 37.8，**仍能把「42 族」与「40 族」分开**：若谁把调用点改回 `FontLabel`(40)，max 会跟着变 40 ⇒ 36.00）。
                //   ⛔ **不是把断言放宽** —— 下面另加一条「**实得**必须落在 autosize 区间里」，两条合起来比原来更强。
                float vsPx = vsLb != null ? Label.FontSizeToPx(vsLb.FontSizeMax) : -1f;
                CheckNear(vsPx, 37.8f, TolPx,
                          "★ 「**行标签**」族字号 = **原版 42 × 0.9 = 37.8 px**（`VSync` 作证；读的是"
                        + " `m_fontSizeMax` —— 那一档是**作者填的原版值**、autosize 开关不会改它）"
                        + "（改坏法：把调用点改回 `FontLabel`（40）⇒ 实得 36.00 ⇒ 红）");
                CheckTrue(Mathf.Abs(vsPx - 36f) > 0.5f,
                          $"…而且它**不是** 40 那一档缩出来的 36（实得 {vsPx:F2}）"
                        + " —— 这一条与上一条合起来，才把「42 族」与「40 族」**分开**");
                if (vsLb != null)
                {
                    float vsNow = vsLb.FontPxNow;
                    float vsLo = Label.FontSizeToPx(vsLb.FontSizeMin) - TolPx;
                    float vsHi = Label.FontSizeToPx(vsLb.FontSizeMax) + TolPx;
                    CheckTrue(vsLb.AutoSizing && vsNow > 0f && vsNow >= vsLo && vsNow <= vsHi,
                              $"…并且它**真的开着 autosize、实得落在钳位区间里**（实得 {vsNow:F2} ∈ "
                            + $"[{vsLo:F2}, {vsHi:F2}]；`AutoSizing` = {vsLb.AutoSizing}）"
                            + " —— 🧨 少了这一条，把 autosize 关掉（实得回到 37.8）时上面两条照样绿");
                }

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

            // ---------------- 🆕 2026-10-19（A1181）：本窗文字漏斗的【自适应】 ----------------
            //
            // 缺陷（本笔）：`Shell/SettingsWindow.cs` 的 `Text(...)` 是本窗文字的**唯一漏斗**（它自己的
            //   注释写着「新加文字必须走这个漏斗」），可它**形参里连 autosize 都没有** ⇒ 走它的
            //   **41 个调用点一处都开不了自适应**（`MenuDraw.Text` 的自适应那一整段写在 `if (wrapPx > 0f)` 里）。
            //   而原版 `Main Menu Settings Window` 里**压倒多数**的 TMP 是 `m_enableAutoSizing = 1`。
            //
            // 判据（第一权威 = 原版 prefab 逐字段实读，`bundle_menus_assets_all` + `bundle_scenes_scenes_mainmenuwarpforge`）：
            //   量具 = `python 工具/menu_dump.py bundle_menus_assets_all "Main Menu Settings Window" --depth 10
            //           --no-sprite --no-layout`（印 `字号=` / `基准=` / **`auto[min~max]`** / `折行=`）；
            //   收录表 = `资料/普查产出_第十会话/R2_A1126原版autosize真值.md` §3 注⑥ + §7。
            //   下表**逐站四格**（`min / max / base / 折行`）+ **原版那一颗的折行宽 / 容器宽**。
            //
            // 🔴 **期望值一律是【原版字段值 × 0.9】的字面量**（⛔ 不写 `SettingsWindow.RootScale` /
            //   `PageTitleFontPx` 这类**被测实现里的**常量 —— 拿它算期望 = 同式自证，见 `A171` 那节的口径）。
            // 🔴 **⛔ 本笔一个字没改 `Shell/MenuDraw.cs`**（共用件，不在白名单）—— 收尾那条探针（④）钉的就是这一件。
            //
            // 🧨 **旧写法下会红在哪**：把 `SettingsWindow.Text` 里那四个实参传回 `0`（= 本笔之前那一版）
            //   ⇒ 每一站的 `AutoSizing` 那一条立刻红（自适应那一段**整段不执行**，
            //   `fontSizeMin/Max` 停在 TMP 出厂 `0/0`）。⛔ **本表不是「怎么都能绿」**：四条字段级断言
            //   （`AutoSizing` / `min` / `max` / `base`）+ 折行档 + 「渲出来 ≤ 框」各钉一种改坏法。
            Section("A1181：本窗文字漏斗的自适应 —— 原版四格（min/max/base/折行）真的落进 TMP");
            {
                var tabWas = win.Current;

                // 🔴 **2026-10-19（第十会话 · `A1194`）形参表多了一格 `boxH`** = 原版那一颗的
                //   **`m_SizeDelta.y`（设计 px）** —— 「渲出来 ≤ 框」的**纵向那一半**（原委 → `CheckAutoFit` 的 doc）。
                //   读数出处 **只此一份**：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                //   "Main Menu Settings Window" --depth 20 --no-sprite --no-ancestor-scale`
                //   （设计帧、与 `boxW` 同一份表；`--no-ancestor-scale` 那一档的「高」列）。
                //   ⚠️ **有两族的 `m_SizeDelta` 在 prefab 里读不到**（布局组 / `AspectRatioFilter` 排出来的）：
                //     · 四行开关的 `… Toggle > Label`：prefab 读到 `0×0`（`HorizontalLayoutGroup` `ctrlH=1`
                //       排出来的）⇒ 取**布局后**那一列 `75.64`（同 `boxW` 那条「这一格没有字段值可抄」）；
                //     · `… Button Text` 那一族带 `AspectRatioFilter`：**prefab 字段**（`--no-layout` 列）
                //       才是「作者填的原版值」，也正是我们照抄进 `Shell/SettingsWindow.cs` 的那一档
                //       （逐站核过：`33.55/13.55/50.06/52.36/90/90` 全对得上）⇒ 一律取**字段值**。
                //   📌 **逐站对账表**（我们传的框高 vs 原版 `m_SizeDelta.y`）→
                //       `资料/普查产出_第十会话/P7_A1189A1194A1196.md` §2。

                // ---- ① 页签那一行（不在任何页里，恒在） ----
                //   原版 `… > Tab Buttons > */Label/Tab Toggle Title` 五颗逐颗同值：10 / 35 / 35 · 折行 0
                //   ⚠️ 框高 40 = 原版 `m_SizeDelta.y`（`139.50×36.00` 屏幕 ÷ 0.9 = `155×40`）
                CheckAutoFit(Bar(root), new[] { "General", "Tab Toggle Title" },
                             "页签 `Tab Toggle Title`（原版 5 颗逐颗同值）",
                             10f, 35f, 35f, 0, 155f, 40f);

                // ---- ② `General` 页 ----
                Click(Bar(root), "General");
                Check(win.Current, SettingsTab.General, "点 `General` ⇒ 切到第 1 页（下面这些站的宿主）");
                var genTab = FindChild(root, "General Tab");
                CheckTrue(genTab != null && genTab.gameObject.activeSelf, "（前提）`General Tab` 开着");
                //  页标题：General / Media / Graphics / Support 四页都是 `4 / 55 / 55 · 折行 1`
                CheckAutoFit(genTab, new[] { "Tab Title" }, "`General Tab > Tab Title`", 4f, 55f, 55f, 1, 942.26f, 70f);
                //  `VersionText`：⚠️ `base 26` **≠ fs 28** —— 这条自带「base 不是退回 fs」的判别力
                //  （退回调用方那一档 ⇒ 量出 28 × 0.9 = 25.2 ≠ 26 × 0.9 = 23.4 ⇒ 红）
                CheckAutoFit(genTab, new[] { "VersionText" }, "`General Tab > VersionText`", 1f, 28f, 26f, 1, 273f, 40.78f);
                CheckAutoFit(genTab, new[] { "Language Selector", "SelectLanguageText" },
                             "`General Tab > Language Selector > SelectLanguageText`", 29f, 42f, 36f, 1, 407.44f, 59.4f);
                CheckAutoFit(genTab, new[] { "Disable Bots", "Label" },
                             "`General Tab > Checkboxes > Disable Bots > Label`", 29f, 42f, 36f, 1, 755.81f, 75.64f);
                //  🔴 **2026-10-10（A1216/A1210②）就地订正**：这两条的**框**原来写的是整颗钮 `300×90`，
                // 而 `Button Text` 那颗**挂着 `AspectRatioFitter`**（`m_Enabled=1` · **宽控高** ·
                // ratio `5.140573`）⇒ 原版**跑起来**的框 = 钮内缩后的 `274 × (274 ÷ 5.140573) = 274 × 53.3015`。
                // ⚠️ **这条订正不是「可有可无」**：`CheckAutoFit` 的框那两格是**单边上限**（`sz ≤ boxW*RS + 1.5`）
                // ⇒ 框被改小之后**旧的（更大的）常量仍然放行** ⇒ 那几条会**静默变成空判据**。
                CheckAutoFit(genTab, new[] { "Redeem Code", "Button Text" },
                             "`General Tab > Bottom Buttons > Redeem Code > Button Text`", 12f, 40f, 12f, 0, 274f, 53.3015f);
                //  `Close Game Button` 那颗原版 `m_fontSize = 38`（⚠️ 与上一颗**不同**）⇒ `max` 跟着它走
                CheckAutoFit(genTab, new[] { "Close Game Button", "Button Text" },
                             "`General Tab > Bottom Buttons > Close Game Button > Button Text`", 12f, 38f, 12f, 0, 274f, 53.3015f);
                //  🆕 **2026-10-19（`A1186` 裁定 ②）**：`General` 页那颗 `Online` 入口钮 ——
                //  **原版没有这一颗** ⇒ 这四格是**我们照同族那两颗抄的**（`12 / 38 / 12 · 折行 0`），
                //  框宽 = 本颗钮那一格 **154.81** 设计 px（= `GenOnlineR − GenOnlineL` = `1431.33 − 1276.52`，
                //  ⛔ 别照抄 300）。
                //  ⚠️ 这一条同时是「字形装得下」的量法（`sz.x ≤ boxW × 0.9`）—— 它才是这一批要的那一档。
                CheckAutoFit(genTab, new[] { "Online Button", "Button Text" },
                             "`General Tab > Online Button > Button Text`（**我们自加**）", 12f, 38f, 12f, 0, 154.81f, 90f);

                //  🔴 **原版【关着】autosize 的站 —— 不许接**（本笔的反面：接了就是主动制造偏离）
                //   ① 语言框里那行 `Label`：原版 `m_fontSizeMin == m_fontSize`（18/18）
                //      ⇒ 共用闸 `fontPx > autoMinPx` **三条全真**那条过不去 ⇒ 我们**照原版把四格接上了**、
                //      但它**不会**生效。⚠️ **如实登记**：这条红了有两种可能 —— ⓐ 共用闸被放宽了
                //      （那时应当把它改成「断真」）ⓑ 有人把 `autoMinPx` 往下改动了（**不许**）。
                {
                    var capN = FindChild(FindChild(FindChild(genTab, "Language Selector"), "LanguagesDropdown"), "Label");
                    var capLb = capN != null ? capN.GetComponentInChildren<Label>() : null;
                    CheckTrue(capLb != null, "（前提）语言框里那行 `Label` 在");
                    CheckTrue(capLb != null && !capLb.AutoSizing,
                              "⚠️ `LanguagesDropdown > Label`：原版 `m_fontSizeMin == m_fontSize = 18` ⇒ "
                            + "**共用闸 `MenuDraw.TextCore` 的 `fontPx > autoMinPx` 过不去** ⇒ 传了也不生效"
                            + "（四格照原版接了，见 `SettingsWindow.Text` 的调用点）"
                            + " —— 🧨 本行红 = ① 共用闸被人放宽了（那请把它改成断真、并销掉这条备注）"
                            + "或 ② 有人把 `autoMinPx` 改成 < 18（**那是发明一个原版没有的值，不许**）");
                }

                // ---- ③ `Graphics` 页（四行开关 + FPS 那一行 + 三个刻度） ----
                Click(Bar(root), "Graphics");
                Check(win.Current, SettingsTab.Graphics, "点 `Graphics` ⇒ 切到第 4 页（下面这些站的宿主）");
                var gfxTab = FindChild(root, "Graphics Tab");
                CheckTrue(gfxTab != null && gfxTab.gameObject.activeSelf, "（前提）`Graphics Tab` 开着");
                CheckAutoFit(gfxTab, new[] { "Tab Title" }, "`Graphics Tab > Tab Title`", 4f, 55f, 55f, 1, 942.26f, 70f);
                CheckAutoFit(gfxTab, new[] { "Quality Selector", "Quality selector text" },
                             "`Graphics Tab > Quality Selector > Quality selector text`", 29f, 42f, 36f, 1, 407.51f, 59.4f);
                //  四行开关的 `Label`：原版 `Small Screen Size Toggle` / `Auto Zoom Toggle` /
                //  `Use super sampling` / `VSync` 四颗**逐值相同** = 29 / 42 / 36 · 折行 1
                //  ⚠️ 框高 75.64：这四颗的 `m_SizeDelta` 是 `HorizontalLayoutGroup`（`ctrlH=1`）排出来的、
                //     prefab 里读到 `0×0` ⇒ 取**布局后**那一列（同 `boxW` 那条「没有字段值可抄」的如实登记）。
                CheckAutoFit(gfxTab, new[] { "Small Screen UI", "Label" }, "`Graphics Tab > Small Screen UI > Label`",
                             29f, 42f, 36f, 1, 325.21f, 75.64f);
                CheckAutoFit(gfxTab, new[] { "Auto Zoom", "Label" }, "`Graphics Tab > Auto Zoom > Label`",
                             29f, 42f, 36f, 1, 325.21f, 75.64f);
                CheckAutoFit(gfxTab, new[] { "VSync", "Label" }, "`Graphics Tab > VSync > Label`",
                             29f, 42f, 36f, 1, 325.21f, 75.64f);
                //  FPS 那一行：原版 `FPS Limit > Title` = 18 / 42 / 36 · 折行 1；
                //  ⚠️ 下限是 **18**（**不是** 29 —— 与上面那四行**不同**，逐颗实读，别一刀切）
                CheckAutoFit(gfxTab, new[] { "FPS Limit", "Title" }, "`Graphics Tab > FPS Limit > Title`",
                             18f, 42f, 36f, 1, 309.55f, 62f);
                CheckAutoFit(gfxTab, new[] { "FPS Limit", "FPS Slider", "30 FPS" },
                             "`Graphics Tab > FPS Limit > FPS Slider > 30 FPS`", 18f, 42f, 36f, 1, 228.02f, 62f);

                // ---- ④ `Media` 页（三行音轨） ----
                // 🔴 **2026-10-09（第十会话 · `D2` 诊断 · α）**：这里原来写 `Click(Bar(root), "Media")`
                //   —— **把【页签印的那个字】当成了【节点名】**。原版那一条页签**印的是 `Media`**，
                //   可**我们的页签节点名是 `Audio`**（`SettingsWindow.BuildTabs` 的 `specs[i].Label`；
                //   同窗另外 8 处 `Click(Bar(root), …)` 用的都是节点名，含本页的 `"Audio"`）。
                //   ⇒ 点不到 ⇒ 第 2 页没切过去 ⇒ **连带下面两条（切页 / 前提）一起红**（一个字符 = 三条红）。
                //   ⚠️ 页**节点**名仍是 `Media Tab`（`BuildAudioPage` 里 `Node(area, "Media Tab", …)`），
                //   ⛔ 别把这一行的改动顺手抄到下面那两行。
                Click(Bar(root), "Audio");
                Check(win.Current, SettingsTab.Audio, "点 `Media` ⇒ 切到第 2 页（下面这些站的宿主）");
                var meTab = FindChild(root, "Media Tab");
                CheckTrue(meTab != null && meTab.gameObject.activeSelf, "（前提）`Media Tab` 开着");
                CheckAutoFit(meTab, new[] { "Tab Title" }, "`Media Tab > Tab Title`", 4f, 55f, 55f, 1, 942.26f, 70f);
                //  原版三颗 `… Container > Label` 逐颗同值 = 18 / 42 / 36 · 折行 1（⚠️ 下限 18，同 FPS 那行）
                //  ⚠️ 框高逐颗实读：`Music Container` **62.00**、另两颗 **63.00**（⛔ 别一刀切取同一个数）。
                //  🔴 **2026-10-20（`A1201`）**：`Music Container > Label` 的**框宽**原来是错的（传了另两颗
                //     的值 684.19 = 整行）—— 原版那一颗的**横幅锚点**是 `aMax.x = 0.5`（**半行**），
                //     另两颗才是 `1`（整行）。判据 = `python 工具/menu_rect.py bundle_menus_assets_all
                //     "Audio Settings" --depth 3 --cs`：`N(2,"Label", 0,0.5, **0.5**,0.5, 0,0.5, 0,35, 0,62)`；
                //     算式 = **行宽 684.195（= `0.75 × 1032.26 − 90`）× `aMax.x`** ⇒ 本格 = **342.0975** 设计 px。
                //     （第二路复核：`menu_dump … --depth 3` 那一行「宽」列 = **307.89** = 342.0975 × 0.9。）
                //     ⛔ 本行的期望值**不许再写 684.19**（那是放宽一倍 = 假绿）；⛔ 另两颗**不许**跟着改半行。
                //     ⚠️ 只改 `boxW` 这一格还是**分辨不出**「实现摆回整行」（那一行的字本来就短）——
                //       真正有分辨力的是下面「三行行顶/标签」那一节新加的**框宽**断言（读 TMP 的 `sizeDelta`）。
                CheckAutoFit(meTab, new[] { "Audio Settings", "Music Container", "Label" },
                             "`Media Tab > Audio Settings > Music Container > Label`", 18f, 42f, 36f, 1, 342.10f, 62f);
                CheckAutoFit(meTab, new[] { "Audio Settings", "FX Container", "Label" },
                             "`Media Tab > Audio Settings > FX Container > Label`", 18f, 42f, 36f, 1, 684.19f, 63f);
                CheckAutoFit(meTab, new[] { "Audio Settings", "Voiceovers Container", "Label" },
                             "`Media Tab > Audio Settings > Voiceovers Container > Label`", 18f, 42f, 36f, 1, 684.19f, 63f);

                // ---- ⑤ `Account` 页 + 登录弹窗 ----
                Click(Bar(root), "Account");
                Check(win.Current, SettingsTab.Account, "点 `Account` ⇒ 切到第 3 页（下面这些站的宿主）");
                var acTab = FindChild(root, "Account Tab");
                CheckTrue(acTab != null && acTab.gameObject.activeSelf, "（前提）`Account Tab` 开着");
                //  🔴 页标题这一页的**下限是 10**（另外四页是 4）—— 逐颗实读，⛔ 别一刀切
                CheckAutoFit(acTab, new[] { "Tab Title" }, "`Account Tab > Tab Title`", 10f, 55f, 55f, 1, 942.26f, 70f);
                //  🔴 **原版【关着】autosize 的站**：`Player Id` 那颗 TMP 的字段里没有 `auto`（fs 40 / 折行 1）
                {
                    var pidT = FindChild(acTab, "Player Id");
                    var pidLb = pidT != null ? FindChild(pidT, "Label") : null;
                    var pidL = pidLb != null ? pidLb.GetComponentInChildren<Label>() : null;
                    CheckTrue(pidL != null, "（前提）`Player Id > Label` 在（原版那颗 TMP 长在 `Player Id` 自己身上）");
                    CheckTrue(pidL != null && !pidL.AutoSizing,
                              "⚠️ `Player Id > Label`：原版那颗 TMP **没开** `m_enableAutoSizing`"
                            + "（逐字段实读：只有 `fs 40` / `m_fontSizeBase 40` / `折行 1`，**没有 auto**）"
                            + " ⇒ **我们也不接** —— 🧨 本行红 = 有人给它接了自适应（那是**主动制造偏离**）");
                }
                //  账号页那两颗标签的下限是 **10**（⚠️ 与登录弹窗里同名的两颗**不同**，见下）
                CheckAutoFit(acTab, new[] { "Account Form", "EmailText" }, "`Account Form > EmailText`", 10f, 37f, 37f, 1, 460f, 60f);
                CheckAutoFit(acTab, new[] { "Account Form", "PasswordText" }, "`Account Form > PasswordText`", 10f, 37f, 37f, 1, 460f, 60f);
                //  两颗链接（原版那颗 TMP 就长在钮节点自己身上）：29 / 32 / 36 · 折行 1（⚠️ base 36 **>** max 32）
                CheckAutoFit(acTab, new[] { "Account Form", "Reset Password", "Text" },
                             "`Account Form > Reset Password > Text`", 29f, 32f, 36f, 1, 460f, 44.36f);
                CheckAutoFit(acTab, new[] { "Account Form", "Forgot Password", "Text" },
                             "`Account Form > Forgot Password > Text`", 29f, 32f, 36f, 1, 460f, 44.36f);
                CheckAutoFit(acTab, new[] { "Account Form", "Error Message" }, "`Account Form > Error Message`", 29f, 37f, 37f, 1, 920f, 47.1f);
                //  订阅钮：32 / 40 / 44 · 折行 1 —— ⚠️ base **44 比 max 还大**（原版原文）⇒ 再单钉一条
                CheckAutoFit(acTab, new[] { "Subscribe Newsletter", "Label" }, "`Subscribe Newsletter > Label`", 32f, 40f, 44f, 1, 599.46f, 76.71f);
                //  五条外链的 `Button Text`（原版恒关）：12 / 38 / 12 · 折行 0；⚠️ `max 38 < fs 43`
                //  ⚠️ 我们这五颗也照原版 `SetActive(false)`（`m_IsActive = 0`）⇒ 未激活的 TMP 不重排
                //  ⇒ 这两条**只断字段级**（`skipRendered: true`），如实说清「渲出来 ≤ 框」那一格**没量**。
                //  ⚠️ 框高 = prefab **字段值**（`AspectRatioFilter` 排出来的那一列更大/更小，⛔ 不取它，
                //     理由见本节头那段）—— 这两颗恒关、这一格本来也不量。
                //  🔴 **2026-10-10（A1200/A1216）就地订正框高**：原记「框⛔ 不取 ARF 排出来的那一列」
                //  —— **那条被推翻了**：`R3`/`P-A` 现读逐组件核出该子树 **647 个 RectTransform 里挂这颗 ARF 的
                //  = 36 颗、`m_Enabled=1` 的 36 颗、0 禁用** ⇒ 原版**跑起来用的就是 ARF 后的值**。
                //  这五颗的宽都**没变**（`101.45`），高 = `101.45 ÷ 5.140573 = 19.7344`。
                //  ⚠️ 同前一条：框是**单边上限** ⇒ 不订正就**静默变成空判据**。
                CheckAutoFit(acTab, new[] { "Social Media Links", "Discord Button", "Button Text" },
                             "`Social Media Links > Discord Button > Button Text`", 12f, 38f, 12f, 0, 101.45f, 19.7344f, true);
                CheckAutoFit(acTab, new[] { "Social Media Links", "IG Button", "Button Text" },
                             "`Social Media Links > IG Button > Button Text`（fs 31.05 那颗）", 12f, 38f, 12f, 0, 101.45f, 19.7344f, true);
                //  七颗大钮：12 / max=那一颗的 fs / 12 · 折行 0（Register 40 · Twitch **38**）
                CheckAutoFit(acTab, new[] { "Buttons", "Unregistered Buttons", "Register Button", "Button Text" },
                             "`Buttons > Unregistered Buttons > Register Button > Button Text`", 12f, 40f, 12f, 0, 277.6f, 54f);
                CheckAutoFit(acTab, new[] { "Buttons", "Twitch Button", "Button Text" },
                             "`Buttons > Twitch Button > Button Text`（fs 38 那颗）", 12f, 38f, 12f, 0, 271.31f, 52.7787f);
                //  登录弹窗那五件：⚠️ 与页内同名的两颗**下限不同**（EmailText 32 / PasswordText 29）
                //  ⚠️ 整棵 `Login Window` **出厂关着** ⇒ 未激活的 TMP 不重排、`textBounds` 是旧的
                //  ⇒ 那五条**只断字段级**（`skipRendered: true`），如实说清「渲出来 ≤ 框」那一格**没量**。
                CheckAutoFit(acTab, new[] { "Login Window", "EmailText" },
                             "`Login Window > EmailText`", 32f, 37f, 37f, 1, 437.5f, 60f, true);
                CheckAutoFit(acTab, new[] { "Login Window", "PasswordText" },
                             "`Login Window > PasswordText`", 29f, 37f, 37f, 1, 437.5f, 60f, true);
                CheckAutoFit(acTab, new[] { "Login Window", "Forgot Password", "Text" },
                             "`Login Window > Forgot Password > Text`", 29f, 32f, 36f, 1, 437.5f, 44.36f, true);
                CheckAutoFit(acTab, new[] { "Login Window", "ErrorMensajeContainer", "Error Message" },
                             "`Login Window > ErrorMensajeContainer > Error Message`", 29f, 37f, 37f, 1, 774.1f, 36.73f, true);
                CheckAutoFit(acTab, new[] { "Login Window", "Login Button", "Button Text" },
                             "`Login Window > Login Button > Button Text`", 12f, 40f, 12f, 0, 283.17f, 55.0857f, true);

                // ---- ⑥ `MenuDraw` 的默认路径零变化（= 别的窗那一百多处调用） ----
                //   本笔只加形参、**给的都是缺省 0** ⇒ 不传的那 31 个调用点（含别窗全部）走的是逐字节相同的老路。
                //   ⚠️ 这条与「`Player Id` 那颗没开自适应」是**两条**：那条钉「原版关着的站我们没接」，
                //   这条钉「**共用件**的缺省行为没被顺手改掉」（⛔ 那是别的 7 个宿主窗共用的那一层）。
                {
                    var probeGo2 = new GameObject("A1181 probe (MenuDraw.Text 默认路径)");
                    var p0 = MenuDraw.Text(probeGo2.transform, new PxRect(0f, 0f, 300f, 60f), "p0",
                                           Color.white, "p0", 40f, MenuDraw.QText);
                    CheckTrue(p0 != null && !p0.AutoSizing,
                              "★ 共用件默认路径：`MenuDraw.Text(…40f…)` **不开自适应**（`AutoSizing` = false）"
                            + " —— 别窗那一百多处调用拿到的是逐字节相同的代码；"
                            + "🧨 改坏法：把自适应那一段从 `if (wrapPx > 0f)` 里挪出来、或给 `autoMinPx` 一个非 0 缺省 ⇒ 本行红");
                    Object.DestroyImmediate(probeGo2);
                }

                // ---- ⑦ 收尾：把当前页还原（本段进来时是哪一页就退回哪一页） ----
                win.OpenTab(tabWas);
                Check(win.Current, tabWas, "（收尾）切回本段进来之前那一页 —— 与本段之前的现状一致");
            }

            // 关窗
            Click(FindChild(Area(root), "Generic Close Button"), "Hit");
            Check(win.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗口进 Closed 态");

            // 🆕 **2026-10-15（A796）**：压暗层「**点了会不会关**」—— 走公共口
            //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs` 的 `CheckShadeClickRule`）；
            //   逐站点表 / 与账上 24 的对账 → `资料/普查产出_1015/W7_A796调用点.md`。
            //   🔴 **本口会把窗【真的关掉】** ⇒ 必须排在**本窗其它断言之后**（这里就是本窗的收尾：
            //      下面只剩一张实拍 + `finally` 里的还原，⛔ 别把本块往上挪 —— 同族翻车留档 →
            //      `Editor/RewardsScene.cs` 的 `Run` 里「探针跑在关闭的窗上」那一段「探针跑在关着的窗上」）。
            //   ⚠️ 上面那句收尾点的是**关闭钮**（`Generic Close Button`），与压暗层**不是同一颗**；
            //      本窗的压暗层断言（`:2290` 那条 `CheckAbsorbRule`）收尾也会把窗点关 ⇒ 这里先开回来。
            //   ⚠️ 重开 = `Open()` → `Build()` **整棵树重建**（见 `:2294-2299` 那条订正）⇒ 下面那颗
            //      `ShadeHit` 必须是**现取**的（本行就是现取），⛔ 别缓存成跨重建的局部变量。
            CheckTrue(win.TryOpen(), "（A796 现场）把设置窗开回来 —— 下面那条要在**开着**的窗上点");
            MenuDraw.CheckShadeClickRule(CheckTrue, "设置窗", win.transform, win.ShadeHit,
                                         () => win.CurrentState);

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

        // ============================================================ 🆕 2026-10-09（`A1025`）本批键全 `Loc.HasEntry` + 两语档取真文案
        // 判据（坑表 #18）：**「原版有词条」≠「我们表里有键」** —— 键不在 ⇒ `Loc.T` 返回**键名本身**、界面上就印键名。
        // 🔴 **与 `A1057(f)`（`Editor/NetSelfTest.cs` 那张**表级**扫描）不是同一条、方向相反**：那条是 **表 → 表**
        //   （表自身健康），本条是 **代码 → 表**（**本批代码引的键**有没有落进表）—— 表级扫描永远看不见后者。
        // 🔴 **Net/ 那批已有等价物**（`Editor/NetSelfTest.cs` 的 `TestNetTermBilingual`）⇒ 本笔不重复覆盖它。
        // 数组 = 本批生产文件里出现的**活键字面量** ∩ `Core/Loc.cs` 的表键（超集无害、且更严）。
        // 本宿主覆盖的生产文件 = `Shell/SettingsWindow.cs`（81 条）。
        // 🧨 改坏法：① `Loc.cs` 删掉本批任一条键 ⇒ ①红；② 某条**英文列**填中文/全角空格 ⇒ ③红；
        //   ③ **中文列**清空 ⇒ ②红；④ 值改成键名本身 ⇒ ②红；⑤ 表删掉一半 ⇒ ④红（`EntryCount` 掉到基线之下）；
        //   ⑥「把实现与期望一起改回写死中文 **并** 把表里那条删掉」⇒ ①红（数组里那条键仍在、`HasEntry` 假）—— 这正是本数组存在的唯一理由。
        {
            string[] a1025Keys =
            {
                "Demo/MainMenu/ExitButton", "Demo/MainMenu/ExitGame", "MainMenu/General/OK",
                "MainMenu/General/Off", "MainMenu/General/On", "MainMenu/Settings/ButtonLabel/Exit_Game",
                "MainMenu/Settings/ButtonLabel/SelectLanguage", "MainMenu/Settings/SettingLabel/Music", "MainMenu/Settings/SettingLabel/SoundFx",
                "Settings/General/DisableBots", "Settings/General/DisableNotifications", "Settings/General/Flash/Language",
                "Settings/General/LangHasNoTable", "Settings/General/RedeemCode", "Settings/General/RedeemCodeUnavailable",
                "Settings/General/Title", "Settings/General/TouchInput", "Settings/Graphics/AutoZoom",
                "Settings/Graphics/EnableSuperSampling", "Settings/Graphics/Flash/AutoZoom", "Settings/Graphics/Flash/Fps",
                "Settings/Graphics/Flash/Quality", "Settings/Graphics/Flash/SmallScreenUI", "Settings/Graphics/Flash/SuperSampling",
                "Settings/Graphics/Flash/Vsync", "Settings/Graphics/FpsText/Unlimited", "Settings/Graphics/FpsText/Value",
                "Settings/Graphics/FrameLimit", "Settings/Graphics/IncreaseUISize", "Settings/Graphics/SelectQuality",
                "Settings/Graphics/Title", "Settings/Graphics/UnlimitedFPS", "Settings/Graphics/Vsync",
                "Settings/Media/AudioMixerNote", "Settings/Media/Title", "Settings/Media/VoiceOvers",
                "Settings/Online/CheckConnection", "Settings/Online/ClickAgain", "Settings/Online/HostFailed",
                "Settings/Online/HostReady", "Settings/Online/HostReadyNoPassword", "Settings/Online/HostReadyToFriend",
                "Settings/Online/HowToConnect/DontUseTestSite", "Settings/Online/HowToConnect/Intro", "Settings/Online/HowToConnect/Lan",
                "Settings/Online/HowToConnect/LocalCheckTitle", "Settings/Online/HowToConnect/NoHolePunching", "Settings/Online/HowToConnect/PublicDirect",
                "Settings/Online/HowToConnect/PublicV6No", "Settings/Online/HowToConnect/PublicV6Yes", "Settings/Online/HowToConnect/UpnpNote",
                "Settings/Online/HowToConnect/VirtualLan", "Settings/Online/HowToConnect/VirtualNicNo", "Settings/Online/HowToConnect/VirtualNicYes",
                "Settings/Online/IpLabel", "Settings/Online/IpPlaceholder", "Settings/Online/IsV6",
                "Settings/Online/LocalAddr", "Settings/Online/NetRuntimeMissing", "Settings/Online/NoNicFound",
                "Settings/Online/PasswordLabel", "Settings/Online/PasswordPlaceholder", "Settings/Online/ProbingPublicAddress",
                "Settings/Online/PublicAddress/BothOk", "Settings/Online/PublicAddress/LocalV6", "Settings/Online/PublicAddress/Mismatch",
                "Settings/Online/PublicAddress/None", "Settings/Online/PublicAddress/NotFound", "Settings/Online/PublicAddress/Title",
                "Settings/Online/PublicAddress/V4", "Settings/Online/PublicAddress/V6", "Settings/Online/Refresh",
                "Settings/Online/RoleClient", "Settings/Online/RoleHost", "Settings/Online/Save",
                "Settings/Online/StatusNoSession", "Settings/Online/TestPublicIp", "Settings/Online/Title",
                "Settings/Online/TitleNote", "Settings/Online/TitleNoteBody", "Settings/Online/VirtualNic",
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

        Debug.Log(P + $"===== 通过 {_sink.Pass} · 失败 {_sink.Fail} =====");
        // 🔴 **2026-10-12（A443 · 调度台裁定）**：这一串是**失败表的【重列】**（每条失败在 `Check()` 里
        //   **已经现场打过一次**、行首是真 `✗`，见本文件 `:47`）⇒ 重列这里**不能再带 `✗`** ——
        //   原来是 `✗` 时日志里 `✗` 行数 = 失败数 **×2**，连「按行首标记数」都数不准
        //   （`资料/已知的坑.md`「别用 `grep -c ✗` 数失败」）。同族五处已改 →
        //   `ShellScene` / `CollectionScene` / `RewardsScene` / `ShopScene`（A350）· `MainMenuScene`（A443）；
        //   本处是 A443 补上的最后一处。⚠️ **别顺手改 `:47` 那条真 `✗`**（`Check()` 现场那条**不是重列**）。
        if (_sink.Fail > 0) foreach (var f in _sink.Failures) Debug.LogError(P + "   失败重列：" + f);
        if (Application.isBatchMode) EditorApplication.Exit(_sink.Fail == 0 ? 0 : 1);
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
        //      全是踩过的坑）：`Shell/PromptPopup.cs` 的 `WindowButton` 类注 · `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 ·
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

    /// <summary>截图 —— 本文件那一份的空图护栏走共用件（「已放行」那句文案 = 本文件原来那一句，
    /// 由 `_sink.BlankNote` 保住）。⚠️ `MeanBrightness` **仍留在本文件**：它与另外四份**浮点值不等价**
    /// （分母相除方式不同），合并会动 `✓` 行文本 —— 判据见 `Editor/MenuCheck.cs` 的 `Shoot` 文件头。</summary>
    static void Shoot(string file, bool allowBlank = false)
        => MenuCheck.Shoot(_sink, ShotDir, file, allowBlank, guardBlank: true, meanBrightness: MeanBrightness);

    static float MeanBrightness(Texture2D t)
    {
        var px = t.GetPixels32();
        double s = 0;
        for (int i = 0; i < px.Length; i += 7) s += (px[i].r + px[i].g + px[i].b) / 3.0;
        return (float)(s / (px.Length / 7.0));
    }
}
