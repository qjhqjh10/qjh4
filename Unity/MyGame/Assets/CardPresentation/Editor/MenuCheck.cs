// MenuCheck.cs — **「7 份自检宿主」那套逐份重复的辅助函数的唯一实现处**（🆕 2026-10-18 · 第四会话）。
//
// 判据 / 逐份差异表 / 调用面规模 / 撞车清单 → `资料/普查产出_第四会话/施工单_自检辅助函数收口.md`
// （本文件按它 ⑤ 的「**状态对象 + 逐宿主一行转发**」形状落地）。
//
// 收口前 = 7 份逐字重复（`CollectionScene` / `DeckScene` / `MainMenuScene` / `RewardsScene` /
// `SettingsScene` / `ShellScene` / `ShopScene`），**≈ 5,534 个调用点 · 0 处跨文件调用**。
//
// 🔴 **为什么计数器不能是一份全局 `static`**：`Editor/RewardWindowFixture.cs` 的 `static` 已经踩过并写下判据
//    —— 「`ClickCollectAndDismiss` 留在 `RewardsScene`：它要靠那个宿主的 `Check`/`CheckTrue`（**前两个
//    是逐宿主的计数器：拿别处的 `Check` 去断，失败会记进别人的合计里 ⇒ 静默**）」。
//    ⇒ 计数器住在 `CheckSink`（**一个宿主一个实例**），宿主保留**同名的一行转发**。
//
// 🔴 **为什么每个宿主【必须】仍留名叫 `CheckTrue` / `CheckNear` 的符号**：
//    `Shell/MenuDraw.cs` 的 `CheckShadeRule(MenuCheck chk, …)`（32 个调用点）与 `Shell/MenuDraw.cs` 的 `ShadeRuleOk` 的
//    `CheckAbsorbRule(MenuCheck chk, MenuNear near, …)`（33 个调用点）**把宿主的方法组当参数吃进去**
//    ⇒ 这 65 个调用点 + `MenuDraw` 的公开签名**一个字都不用改**。
//
// 🔴 **逐宿主的口径一格都不许统一**（`CheckSink.Near` / `.BlankNote` / `.StarNotBlank`）：
//    `CheckNear` 有 **3** 个变体、`Shoot` 有 **6** 个变体，差别都在**日志文案**；而本活的回归判据是
//    **stdout 逐字节相同**（含 `✓` 行文本，以及 `SettingsScene.CaptureLogs` 那一族**按文本**判的断言）。
//
// 🔴 **类名与 `MenuDraw` 里那个同名委托**：`Shell/MenuDraw.cs` 声明了嵌套委托
//    `MenuDraw.MenuCheck`（= `CardPresentation.MenuDraw.MenuCheck`），与本全局类**不是同一个类型**。
//    **2026-10-18 零风险探针实测**：先只建一个空类 `MenuCheck` → 跑秒级类型检查 ⇒ 运行时/编辑器两个
//    程序集**均 0 错**，且 `CS0433` / `CS0436`（同名类型冲突）**0 条** ⇒ 照账用 `MenuCheck`
//    （`Editor/RewardWindowFixture.cs` 明写**这个文件名是留给本族**的）。

using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>🆕 2026-10-18（第四会话）：**逐宿主**的断言计数器 + 输出口径。
/// 一个宿主一个实例 —— `static readonly CheckSink _sink = new CheckSink(P) { … };`。
/// <para>🔴 **它就是「失败不许记进别人账」那道口**（见本文件头那条判据）：7 个宿主各持自己的
/// `Pass` / `Fail` / `Failures`，⛔ **不许**改成一份全局 `static`。</para></summary>
public sealed class CheckSink
{
    /// <summary>本宿主在日志里的前缀（`[Shell] ` / `DK ` / `[Collection] ` …）—— 逐宿主常量，
    /// ⛔ 不许共用一份（那会让 `✓`/`✗`/合计行全部串味）。</summary>
    public readonly string P;

    public int Pass, Fail;
    public readonly List<string> Failures = new List<string>();

    /// <summary>`CheckNear` 的文案口径（三变体，见 <see cref="MenuNearStyle"/>）。</summary>
    public MenuNearStyle Near = MenuNearStyle.Compact2;

    /// <summary>`Shoot` 里「已知会全黑、显式放行」那句日志的口径（三变体，见 <see cref="MenuBlankNote"/>）。</summary>
    public MenuBlankNote BlankNote = MenuBlankNote.Plain;

    /// <summary>`CollectionScene` 那份「不是空图」文案带 `**` 强调（`**不是空图**`）而其余六份不带
    /// ⇒ 单开一格，把逐字相同保住（见 <see cref="MenuCheck.Shoot"/>）。</summary>
    public bool StarNotBlank;

    public CheckSink(string p) { P = p; }
}

/// <summary>`CheckNear` 日志尾巴的**三种**口径（只影响文案，**不影响断言真假**）。逐宿主实读：
/// <list type="bullet">
/// <item><c>Compact2</c> `（{got:F2} ≈ {want:F2}±{tol:F2}）` —— Collection / Rewards / Settings / Shop</item>
/// <item><c>Compact3</c> `（{got:F3} ≈ {want:F3}±{tol:F3}）` —— MainMenu / Shell</item>
/// <item><c>Labeled4</c> `（实测 {got:F4} ≈ 期望 {want:F4} ± {tol:F4}）` —— Deck</item>
/// </list>
/// ⚠️ `Labeled4` **不只是精度不同，括号里那几个字也不一样** ⇒ 不能折成「一个精度参数」。</summary>
public enum MenuNearStyle { Compact2, Compact3, Labeled4 }

/// <summary>`Shoot` 里「这一张本来就拍的是空屏、显式放行」那句日志的**三种**口径（逐宿主实读）：
/// <list type="bullet">
/// <item><c>Plain</c> `（按已知情况放行）` —— Collection</item>
/// <item><c>EmphThisOne</c> `（**这一张按已知情况放行**）` —— Rewards / Shell / Shop</item>
/// <item><c>EmphPlain</c> `（**按已知情况放行**）` —— Settings</item>
/// </list>
/// ⚠️ 走这一支时**不产生** `✓`/`✗` 行（只是 `Debug.Log`）—— 但它仍在 stdout 里，照样算「逐字节」。</summary>
public enum MenuBlankNote { Plain, EmphThisOne, EmphPlain }

/// <summary>🆕 2026-10-18（第四会话）：7 份自检宿主共用的**菜单自检辅助函数**（唯一一份实现）。
/// <para>每份宿主里只剩**同名的一行转发**（`CheckTrue` / `CheckNear` / …）⇒ 调用点一个字没动。
/// ⛔ **别拿它装别的族**（那是 `Editor/RewardWindowFixture.cs` 那种独立共用件的事）。</para></summary>
public static class MenuCheck
{
    // ============================================================ 断言原语（逐份逐字相同的那 5 个）

    /// <summary>相等断言（`EqualityComparer&lt;T&gt;.Default`）—— 逐字搬自 7 份宿主里同名的那一份。</summary>
    public static void Check<T>(CheckSink s, T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want)) { s.Pass++; Debug.Log(s.P + $"   ✓ {msg}"); }
        else
        {
            s.Fail++;
            var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            s.Failures.Add(line);
            Debug.LogError(s.P + $"   ✗ {line}");
        }
    }

    /// <summary>7 份宿主原来是 `Check(c, true, msg)`（`T` 推导成 `bool`）—— 这里同义。</summary>
    public static void True(CheckSink s, bool c, string msg) { Check(s, c, true, msg); }

    public static void Section(CheckSink s, string t) { Debug.Log(s.P + $"--- {t} ---"); }

    /// <summary>数值比较（±`tol`）—— 用来比**原版参数**那类量（颜色系数、像素尺寸）。
    /// ⚠️ 三份文案见 <see cref="MenuNearStyle"/>：精度与括号里的字**都要逐字保住**。</summary>
    public static void Near(CheckSink s, float got, float want, float tol, string msg)
    {
        bool ok = Mathf.Abs(got - want) <= tol;
        switch (s.Near)
        {
            case MenuNearStyle.Compact3:
                True(s, ok, $"{msg}（{got:F3} ≈ {want:F3}±{tol:F3}）");
                break;
            case MenuNearStyle.Labeled4:
                True(s, ok, $"{msg}（实测 {got:F4} ≈ 期望 {want:F4} ± {tol:F4}）");
                break;
            default:
                True(s, ok, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");
                break;
        }
    }

    /// <summary>把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    public static void HoverSwap(CheckSink s, Transform root, string what)
    {
        int n; string bad = WindowButton.AuditHoverSwap(root, out n);
        True(s, n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) True(s, false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
    }

    /// <summary>取不到的悬停图一张都不许有（红线：不许静默画成没反应）。</summary>
    public static void NoMissingSwapArt(CheckSink s, string what)
        => True(s, WindowButton.MissingSwapArt.Count == 0,
                what + "：**悬停图一张都不缺**（缺的会列在这里："
                + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");

    /// <summary>按名字递归找一个节点（含 inactive）—— 5 份逐字相同的那一份（Collection / MainMenu /
    /// Rewards / Settings / Shop）。
    /// ⚠️ **它不认识 `A/B/C` 这种路径写法**（`RewardsScene.cs` 上面那条注释踩过）——
    /// 要路径用各宿主自己的 `FindPath` 那一族。</summary>
    public static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ============================================================ 矩形读口（唯一一份 · `A1350` 第一批）

    /// <summary>🆕 **2026-10-20（`A1350` 第一批）**：**设计帧点**（乙式）—— 节点的中心，
    /// 比的是**原版 prefab 的设计 px**。
    /// <para>🔴 唯一实现 = 生产侧 `MenuDraw.PixelOfDesign(PosInDesignSpace(t))`
    /// （`QuadRectPx` / 三份 `RectOf` 用的就是这一式，`A298` / `A1004` 收的口）—— 本函数只是给它一个
    /// **自检侧的公共名字**，⛔ 别再在宿主里手写第三份。</para>
    /// <para>⚠️ `t == null` ⇒ 返回 `(-99999,-99999)`：⛔ **不许**落回 `(960,540)`（那会让「节点不在」
    /// 静默变成「在画面中心」，同 `RectOf` 那条「量不到返回 `false`」的纪律）。</para>
    /// <para>⚠️ 与甲式 `LayoutSpace.PxX/PxY(裸 position)` 的差：16:9 + 父链单位缩放下 ≤ 2.5e-4 px；
    /// 父链带缩放 M ⇒ 甲式报 M 倍；非 16:9 ⇒ x 差 `VisibleWidth / DesignWidth` 倍
    /// （判据 / 两式对照 → `资料/普查产出_1019/W57_自检线_A1350读口统一计划.md` §一·2）。</para></summary>
    public static Vector2 DesignPx(Transform t)
        => t == null ? new Vector2(-99999f, -99999f)
                     : MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(t));

    /// <summary>🆕 **2026-10-20（`A1350` 第二批）**：**设计帧点**（乙式）的**世界点重载** ——
    /// 给「`GetWorldCorners` / TMP `textBounds` 四角」那几处用（节点点仍走 <see cref="DesignPx(Transform)"/>）。
    /// <para>🔴 **除的是 `owner` 的【父级】那一级 `lossyScale`** —— 这正是甲式
    /// `LayoutSpace.PxX/PxY(裸 position)` 漏掉的那一级（与 `MenuDraw.PosInDesignSpace` 同一条口径：
    /// 自己的 `localScale` 只影响**它的孩子**、不影响它自己的位置）。`owner` = 被量那棵树里的任一件；
    /// 量一个**临时件**的四角就传那个临时件自己（⛔ 不是它的父件）。</para>
    /// <para>⚠️ `owner == null` ⇒ 返回 `(-99999,-99999)`（与 <see cref="DesignPx(Transform)"/> 同一个哨兵：
    /// ⛔ **不许**落回 `(960,540)`）；`owner.parent == null` ⇒ **不除**（与 `PosInDesignSpace` 的退化档一致：
    /// 它就是根，链上没有任何缩放可除）。</para>
    /// <para>⚠️ **退化守卫逐字同 `MenuDraw.DivByScale`**（缩放非有限 / `|k| &lt; 1e-6` ⇒ 原样返回）——
    /// 那一份是 `private`，这里照抄算式，⛔ 没有去改生产侧的可见性。</para>
    /// <para>⚠️ 与 <see cref="DesignPx(Transform)"/> 的关系：`DesignPx(t)` ≡ `DesignPx(t.position, t)`
    /// （同一个除父级 + 同一个 `PixelOfDesign`），16:9 + 父链单位缩放下差 ≤ 2.5e-4 px。</para></summary>
    public static Vector2 DesignPx(Vector3 worldPoint, Transform owner)
    {
        if (owner == null) return new Vector2(-99999f, -99999f);
        var above = owner.parent;
        if (above == null) return MenuDraw.PixelOfDesign(worldPoint);   // 根：链上没有缩放可除
        var k = above.lossyScale;
        return MenuDraw.PixelOfDesign(new Vector3(DivByScale(worldPoint.x, k.x),
                                                  DivByScale(worldPoint.y, k.y),
                                                  DivByScale(worldPoint.z, k.z)));
    }

    /// <summary>`MenuDraw.DivByScale` 的**逐字副本**（那份是 `private`，本文件只读生产侧 ⇒ 不改可见性）。
    /// <para>语义：`k` 非有限 / ≈0 ⇒ **原样返回**（这一档下该件本来就画不出来，与
    /// `Label.ParentXInDesignSpace` / `PosInDesignSpace` 同一处置）。⛔ 别在这里加别的兜底。</para></summary>
    static float DivByScale(float v, float k)
        => (float.IsNaN(k) || float.IsInfinity(k) || Mathf.Abs(k) < 1e-6f) ? v : v / k;

    /// <summary>🆕 **2026-10-20（`A1350` 第二批）**：**指针帧点** —— 与 `Shell/PointerLayer.HitBoxPx`
    /// **同帧**：`LayoutSpace.ToDesignPixel(裸 t.position)`，**不除**父链缩放（画出来多大就多大，
    /// 父链缩放**留着**）。
    /// <para>🔴 **它服务的语义是「喂 `PointerLayer.ButtonAt/ClickAt`」** ——
    /// `ShellScene.PxCenterOf` 今天用的是甲式 `LayoutSpace.ToPixel`，与 `HitBoxPx` 的
    /// `ToDesignPixel(裸世界)` **不同帧**（非 16:9 下 x 差 `VisibleWidth / DesignWidth` 倍）；
    /// 修法是换成这一档，⛔ **不是**换成设计帧 <see cref="DesignPx(Transform)"/>（那个除了缩放）。
    /// 判据 → `Shell/PointerLayer.cs` 的 `HitBoxPx` doc、`Core/LayoutSpace.cs` 的 `ToDesignPixel` doc。</para>
    /// <para>⚠️ 名字是 `PointerFramePx`、**不是** `PointerPx` —— 后者是 `Battle/TutorialOverlay.cs`
    /// 里已有的私有常量（类内私有，不会编译冲突，但读起来会混）。</para>
    /// <para>⚠️ `t == null` ⇒ `(-99999,-99999)`（同一哨兵纪律：⛔ 不许落回画面中心）。</para></summary>
    public static Vector2 PointerFramePx(Transform t)
        => t == null ? new Vector2(-99999f, -99999f)
                     : LayoutSpace.ToDesignPixel(t.position);

    /// <summary>🆕 **2026-10-20（`A1350` 第二批 · `W58-PTRPORT`）**：**设计帧 → 指针帧**的点换算 ——
    /// 给「先用**设计帧**几何算出一个点、再喂 `PointerLayer.ButtonAt / ClickAt / ScrollUnder`」的那几处用。
    ///
    /// <para>🔴 **本函数只是一行转发**；**唯一实现在 `MenuDraw.PointerFramePxFromDesign`**
    /// （`Shell/MenuDraw.cs` —— 运行期件，`CheckAbsorbRule` 在运行期也要用同一条换算）。
    /// 恒等式、退化守卫、「不是整值乘 k」的推导、`owner` 该传哪一级 —— **全部写在那份 doc 里**
    /// （⛔ 别在这里抄第二份：两份公式迟早漂 = CLAUDE.md §三）。
    /// 🆕 **2026-10-20（`W58-ABSORB`）**：本函数原来自带一份实现，已按「唯一实现」原则下沉到
    /// `MenuDraw`，本文件改为转调；`ScaleAboutPx` 也随之删除（运行期侧有同名一份，私有的）。</para>
    /// <para>⚠️ 保留本签名是为了 `CollectionScene` 那 6 个调用点一个都不用改。</para></summary>
    public static Vector2 PointerFramePxFromDesign(Vector2 designPx, Transform owner)
        => MenuDraw.PointerFramePxFromDesign(designPx, owner);

    /// <summary>🆕 **2026-10-20（`A1350` 第一批）**：`NodeRectPx` 的**中心语义**（⚠️ 显式枚举、
    /// **没有默认值** —— 让编译期拦住「没想清楚该量哪一颗」的调用点）。
    /// <list type="bullet">
    /// <item><c>Node</c> = 中心**恒在传入节点** `t`（`MainMenuScene.RenderedRect` 那一档，`A668` 有意：
    ///   `t` 可能是容器，而宽高取自子树里的 `Label`/`ImageQuad`）。</item>
    /// <item><c>LabelNode</c> = 中心**挪到 Label / quad 自己的节点**（`Collection` / `Rewards` / `Shop`
    ///   三份 `RectOf` 那一档，`A796′` 定的 —— `AlignLeft/Right` 会把 `Label` 的节点挪走）。</item>
    /// </list>
    /// ⚠️ 名字**不叫** `RectCenter`：`LayoutSpace.RectCenter(…)` 是同族里已有的方法名。</summary>
    public enum RectCenterMode
    {
        /// <summary>中心恒在【传入节点】（宽高仍取自子树里的 `Label` / `ImageQuad`）。</summary>
        Node,
        /// <summary>中心挪到 `Label`（或 `ImageQuad`）自己的节点 —— 三份 `RectOf` 的口径。</summary>
        LabelNode,
    }

    /// <summary>🆕 **2026-10-20（`A1350` 第一批）**：一个件**渲出来**的像素矩形（画布 px · 左上原点 · y 向下）
    /// —— 自检侧**唯一一份**节点矩形读口。
    /// <para>🔴 **算法逐句搬自**三份 `RectOf`（`CollectionScene` / `RewardsScene` / `ShopScene`，本批机核
    /// 中心已是乙式、尺寸口径逐字相同）：字走 `SelfCheckTextMetrics.RenderedPx`（TMP `textBounds` 的活值，
    /// 量不到时退回 `Label.WorldW/H × 108`）；图走 `ImageQuad.WorldW/H × 108`（材质的真测量）；
    /// 中心走 <see cref="DesignPx"/>（乙式）。**调用点一个字没动**（三份宿主各留同名一行转发）。</para>
    /// <para>🔴 **两种中心语义见 <see cref="RectCenterMode"/>** —— ⛔ 别偷偷统一：`LabelNode` 是三份
    /// `RectOf` 的口径（`A796′`），`Node` 是 `MainMenuScene.RenderedRect` 的口径（`A668` 明确「中心不随
    /// Label 节点走」；那一份**本批未迁** —— `MainMenuScene.cs` 不在白名单）。</para>
    /// <para>⚠️ `t == null` / 子树里既没有 `Label` 也没有 `ImageQuad` ⇒ 返回 `false`、四个 `out` 全 0
    /// （契约与三份 `RectOf` 逐字一致 —— 调用方必须先判它，否则 `0 ≤ 期望值` 会假绿）。</para>
    /// <para>⚠️ **尺寸项与中心在「父链带缩放」时不同帧**（这是 `A1350` §二·3 已登记的既有缺陷，
    /// **本批未修** —— 不在第一批那两件里）：字走 `SelfCheckTextMetrics.RenderedPx`，它的 TMP 路径是
    /// **世界帧**（`localToWorldMatrix` ⇒ 父链缩放会乘进来）；而**量不到时的退路** `Label.WorldW/H × 108`
    /// 与**图那一支** `ImageQuad.WorldW/H × 108` 是**设计长度**。⇒ 16:9 + 父链单位缩放下两半自洽
    /// （逐位不变）；父链带 M 时会分家。⛔ 别在本函数里改尺寸帧 —— 那是 `RenderedRectDesign` 那笔账。</para></summary>
    public static bool NodeRectPx(Transform t, RectCenterMode center,
                                  out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
        Transform node = t;
        float w, h;
        var lb = t.GetComponentInChildren<Label>();
        if (lb != null)
        {
            if (center == RectCenterMode.LabelNode) node = lb.transform;
            var sz = SelfCheckTextMetrics.RenderedPx(lb);
            w = sz.x; h = sz.y;
        }
        else
        {
            var q = t.GetComponentInChildren<ImageQuad>();
            if (q == null) return false;
            if (center == RectCenterMode.LabelNode) node = q.transform;
            w = q.WorldW * K; h = q.WorldH * K;
        }
        var c = DesignPx(node);
        x1 = c.x - w * 0.5f; x2 = c.x + w * 0.5f;
        y1 = c.y - h * 0.5f; y2 = c.y + h * 0.5f;
        return true;
    }

    // ============================================================ 截图（6 个变体）

    /// <summary>截图 —— 7 份宿主原来各自一份（**6 个变体**）。合并后**逐宿主的行为一个字不变**：
    /// <list type="bullet">
    /// <item><b>`guardBlank`</b> = 那条「空图护栏（平均亮度 &gt; 3）」开不开。`DeckScene` /
    ///   `MainMenuScene` 那两份在 §15 收口时**还没有**它（无 `MeanBrightness`、无 `allowBlank` 形参）
    ///   ⇒ 当时传 `false`。🔴 **2026-10-18（`A1007`）已给这两条宿主补上**（照另 5 份接
    ///   `guardBlank: true` + 各新增一份 `MeanBrightness`）—— 原来那句「不许顺手补 / 那笔账另记」
    ///   随本条销掉（铁律 5·b：账做完了就把记录改成已做）。</item>
    /// <item><b>`meanBrightness`</b> 逐宿主传**方法组**（⛔ **没有**合并成一份共享实现）：5 份里
    ///   `SettingsScene:3059` 那一份是 `double` 累加 + 分母 `px.Length / 7.0`，而 Collection / Rewards /
    ///   Shell / Shop 四份是 `long` 累加 + 分母 `(px.Length + 6) / 7`（= 采样数）⇒ **数学上等价、浮点值
    ///   相对差 ~1.4e-6**（1920×1080 时 `lum≈40` ⇒ 绝对差 ≈ 6e-5）。而本活的回归判据是 **stdout 逐字节
    ///   相同**，且 `lum` 会打进 `✓ … 不是空图（平均亮度 {lum:F1} > 3）` 那一行 ⇒ **合一会动那一行**。
    ///   零风险优先 ⇒ 保留逐份实现、只把它当参数传进来。
    ///   ⚠️ **2026-10-18（`A1007`）之后 7 份全都是 `guardBlank: true`**（`DeckScene` / `MainMenuScene`
    ///   那两份也补上了）⇒ 那个 `null` 参数今天**一份都不传**；但参数**留着**（它就是「逐份实现」的载体）。</item>
    /// </list></summary>
    /// <summary>🆕 **2026-10-18（`A1007`）**：**空图护栏真的跑过几次**（= `guardBlank: true`
    /// 且真的走到了亮度那一跳）。这是**诊断计数**、**不是**断言计数（`Pass`/`Fail` 照旧逐宿主一份，
    /// 判据见本文件头）—— ⛔ 别拿它去替 `Pass`/`Fail`。
    /// <para>用途 = 钉「宿主确实把 `guardBlank: true` 接上了」：谁把某个宿主的转发改回缺省
    /// （`guardBlank: false`），那个宿主里 `✓ … 不是空图` 那一整批行会**整批消失**、这里的增量也变 **0**
    /// ⇒ 宿主的判别式断言当场红（`Editor/DeckScene.cs` 与 `Editor/MainMenuScene.cs` 各一条）。</para></summary>
    public static int GuardedShots;

    public static void Shoot(CheckSink s, string shotDir, string file,
                             bool allowBlank = false, bool guardBlank = false,
                             System.Func<Texture2D, float> meanBrightness = null)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            // 🔴 **2026-10-18（`A1007`）就地改**：这里原来是**裸 `return`** —— 不报错、也不写图，
            //   于是整条截图断言链会**靠「什么都没发生」假装通过**（本工程最忌讳的静默失败；
            //   与「把实现拿掉它还是绿的」同一族）。
            //   **行为那半句一个字不变**（照旧不写图、照旧不渲），变的是：**必须记一条 ✗ 并出声**。
            //   ⛔ 别改回裸 `return`：`Editor/DeckScene.cs` 那条**判别式探针**（摘掉 `MainCamera`
            //   标签造出无相机上下文、判「恰好一条 ✗」）会当场红。
            s.Fail++;
            var miss = $"截图 {file}：**没拍成** —— `Camera.main == null`"
                     + "（改前这一支是**静默 return**：不报错、也不写图 ⇒ 谁都不知道这一张没拍）";
            s.Failures.Add(miss);
            Debug.LogError(s.P + $"   ✗ {miss}");
            return;
        }
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
        File.WriteAllBytes(Path.Combine(shotDir, file), tex.EncodeToPNG());
        // 🔴 **空图护栏**（2026-09-23 踩到）：`Shoot` 原来是「拍完就写盘」，于是一张**全黑**的图也能安静地
        //    写出去（原因：上一个窗的 `CloseAllWindows()` 把要拍的那个窗也关了）。判据 = **平均亮度**
        //    （0–255）；模板黑底大约 14~40，纯黑 ≈ 0。
        //    ⚠️ **必须在 `DestroyImmediate(tex)` 之前**（销毁之后 `tex == null`，护栏恒红）。
        if (guardBlank)
        {
            GuardedShots++;
            float lum = meanBrightness(tex);
            if (allowBlank) Debug.Log(s.P + $"  截图 {file} 平均亮度 {lum:F1}" + BlankNoteText(s.BlankNote));
            else True(s, lum > 3f, s.StarNotBlank
                                  ? $"{file} **不是空图**（平均亮度 {lum:F1} > 3）"
                                  : $"{file} 不是空图（平均亮度 {lum:F1} > 3）");
        }
        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(s.P + $"  截图 {Path.Combine(shotDir, file)}");
    }

    /// <summary>「已放行」那半句的三种文案（逐宿主实读，⛔ 不许统一）。</summary>
    static string BlankNoteText(MenuBlankNote n)
    {
        switch (n)
        {
            case MenuBlankNote.EmphThisOne: return "（**这一张按已知情况放行**）";
            case MenuBlankNote.EmphPlain: return "（**按已知情况放行**）";
            default: return "（按已知情况放行）";
        }
    }
}
