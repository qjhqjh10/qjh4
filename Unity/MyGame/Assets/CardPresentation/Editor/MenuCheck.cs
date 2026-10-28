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
    /// Label 节点走」）。🔴 **2026-10-21（`T02`）就地订正**：原来这里写「`MainMenuScene` 那一份**本批未迁**
    /// —— 不在白名单」—— **已过期**：它的**尺寸覆盖块**已随四份一起提回本函数（见下一条 `designSize`），
    /// **中心档仍是 `Node`、一个字没动**。</para>
    /// <para>⚠️ `t == null` / 子树里既没有 `Label` 也没有 `ImageQuad` ⇒ 返回 `false`、四个 `out` 全 0
    /// （契约与三份 `RectOf` 逐字一致 —— 调用方必须先判它，否则 `0 ≤ 期望值` 会假绿）。</para>
    /// <para>⚠️ **尺寸项与中心在「父链带缩放」时原本不同帧**（这是 `A1350` §二·3 已登记的既有缺陷）：
    /// 字走 `SelfCheckTextMetrics.RenderedPx`，它的 TMP 路径是
    /// **世界帧**（`localToWorldMatrix` ⇒ 父链缩放会乘进来）；而**量不到时的退路** `Label.WorldW/H × 108`
    /// 与**图那一支** `ImageQuad.WorldW/H × 108` 是**设计长度**。⇒ 16:9 + 父链单位缩放下两半自洽
    /// （逐位不变）；父链带 M 时会分家。</para>
    /// <para>🆕 **2026-10-21（`T02` · `A1350` §二·3 的收口）**：原来自检侧**四份**各自带一块「尺寸覆盖块」
    /// （`CollectionScene.RectOf` / `RewardsScene.RectOf` / `ShopScene.RectOf` /
    /// `MainMenuScene.RenderedRect`，四份**逐字相同**：拿 `RenderedRectDesign` 的宽高原地重铺中心）。
    /// 现已**提回本函数** ⇒ <c>designSize: true</c> 时字那一支改走
    /// <see cref="SelfCheckTextMetrics.RenderedRectDesign"/>（宽高 = **设计宽高**），
    /// **量不到时与缺省档逐字同一条退路**（`RenderedPx` → `Label.WorldW/H × 108`）；
    /// 图那一支两档**不变**（`ImageQuad.WorldW/H × 108` 本来就是设计长度）。
    /// ⇒ 单位缩放下两档逐位相同（除 1）；父链带 M 时只有 `true` 档报设计值（那正是这笔账要修的）。
    /// 两态判据 = <see cref="DesignSizeTwoStateProbe"/>（四份宿主各一条断言钉自己的那一行）。
    /// ⏭ **缺省档（`designSize: false`）有意保留世界帧**（`TmpRenderedRect` 那一族的口径，`A1350` §一·2 的甲式）
    /// —— ⛔ 别把缺省也改成设计帧，那会动别的宿主的读数。</para></summary>
    public static bool NodeRectPx(Transform t, RectCenterMode center,
                                  out float x1, out float y1, out float x2, out float y2,
                                  bool designSize = false)
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
            // 🆕 **2026-10-21（`T02`）**：`designSize` 档 = **尺寸也读设计帧**（正是四份覆盖块那句
            // `RenderedRectDesign` 的宽高「以原中心重新铺开」—— 中心仍由下面的 `DesignPx(node)` 给）。
            // 🧨 **把这一支删掉（或把宿主改回 `designSize: false`）⇒ 父链 ×2 时这里报 `2 × 设计宽`**
            //     ⇒ `DesignSizeTwoStateProbe` 那条 `设计宽 ≈ 世界宽 ÷ 2`（四份宿主各一条）**当场红**。
            if (designSize)
            {
                float dx1, dy1, dx2, dy2;
                if (SelfCheckTextMetrics.RenderedRectDesign(lb.transform, out dx1, out dy1, out dx2, out dy2))
                {
                    w = dx2 - dx1; h = dy2 - dy1;
                }
                else
                {
                    // 量不到（点阵后端 / 底下没有 TMP）⇒ **与缺省档同一条退路**（`Label.WorldW/H × 108` 本来就是设计长度）
                    var szd = SelfCheckTextMetrics.RenderedPx(lb);
                    w = szd.x; h = szd.y;
                }
            }
            else
            {
                var sz = SelfCheckTextMetrics.RenderedPx(lb);
                w = sz.x; h = sz.y;
            }
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

    /// <summary>宿主自检侧的「矩形量法」形参类型（= 四份 `RectOf` / `RenderedRect` 的同形签名）。
    /// 🆕 **2026-10-21（`T02`）**：只给 <see cref="DesignSizeTwoStateProbe"/> 用 —— 把**被测的那一行**
    /// （宿主自己的量法）当方法组传进来。</summary>
    public delegate bool RectMeasure(Transform t, out float x1, out float y1, out float x2, out float y2);

    /// <summary>🆕 **2026-10-21（`T02` · `A1350` §二·3 收口的验收口）**：**尺寸帧两态探针**的夹具与量法
    /// —— 自检侧**唯一一份**（四份宿主各一条断言，⛔ 别在宿主里再抄一份夹具）。
    ///
    /// <para>🔴 **它证明什么**：在**父链 `localScale = 2`** 的夹具上，`measured`（= 宿主自己的
    /// `RectOf` / `RenderedRect`，已传 `designSize: true`）报的宽 **必须 ≈ 世界帧宽 ÷ 2**；
    /// 而 `worldW` 由 <see cref="NodeRectPx"/> 的**缺省档**（世界帧 · `SelfCheckTextMetrics.RenderedPx`）
    /// **独立**量出 ⇒ **两边不是同一处代码**。</para>
    /// <para>🔴 **灭自证（结构上不可能同时满足）**：
    /// ① 宿主把 `designSize: true` 改回缺省 ⇒ `designW == worldW` ⇒ `designW ≈ worldW/2` 必不过；
    /// ② 把 <see cref="NodeRectPx"/> 里 `designSize` 那一支删掉（参数被忽略）⇒ 同 ①；
    /// ③ 把缺省档也改成设计帧（「两边一起改回同一帧」）⇒ `worldW` 也减半 ⇒ `designW == worldW` ⇒ 仍必不过。
    /// ⇒ 三面都堵；而期望值**不是本机量出来的字面量**（字体度量不钉死），是**夹具缩放这条不变量**。</para>
    /// <para>⚠️ **父链带 2× 的节点必须落在【它自己父级的原点】**（`mid.localPosition = 0`；同
    /// `MainMenuScene.CheckDesignPxUnderScaledParent` / `CollectionScene.CheckPointerFrameFromDesign` 的硬约束，
    /// 判据 = `Shell/MenuDraw.cs` 的 `PosInDesignSpace` 适用范围）。</para>
    /// <para>⚠️ 夹具自建自毁（`finally` 里 `DestroyImmediate`），⛔ 不留在场景里；
    /// 量不到（建不出 `Label` / 宿主量法返回 `false`）⇒ 返回 `false` 且两个 `out` 为 0
    /// —— 调用方**必须先判它**（⛔ 不许静默跳过那两条）。</para></summary>
    public static bool DesignSizeTwoStateProbe(RectMeasure measured, out float designW, out float worldW)
    {
        designW = worldW = 0f;
        if (measured == null) return false;
        var host = new GameObject("A1350 DesignSize Probe Host");        // 世界原点 · 单位缩放
        var mid = new GameObject("A1350 DesignSize Probe Scaled");
        mid.transform.SetParent(host.transform, false);                   // 局部位置进 0（被除的那一级落在父级原点）
        mid.transform.localScale = new Vector3(2f, 2f, 1f);
        try
        {
            var lb = MenuDraw.Text(mid.transform, new PxRect(1200f, 700f, 1500f, 760f), "A1350",
                                   Color.white, "A1350 Design Text", 30f, 3000);
            if (lb == null) return false;
            float dx1, dy1, dx2, dy2;
            if (!measured(lb.transform, out dx1, out dy1, out dx2, out dy2)) return false;
            designW = dx2 - dx1;
            // 世界帧那一半：**缺省档**（与 `measured` 走的不是同一条尺寸支）
            float wx1, wy1, wx2, wy2;
            if (!NodeRectPx(lb.transform, RectCenterMode.LabelNode,
                            out wx1, out wy1, out wx2, out wy2)) return false;
            worldW = wx2 - wx1;
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);                    // ⛔ 别把探针留在场景里
        }
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

    // ============================================================ 🆕 2026-10-28（B89-R3c）`RaycastFilter` 的三条真值
    // 判据 = `Shell/RaycastFilter.cs` 文件头（原版 `RaycastFilter.cs` 整类 253 行逐句读过）+ `TutorialManager.cs:1371-1397`
    //   （`if (!filterRaycast) ClearRaycastFilters(); else { ClearRaycastFilters(); RegisterObjectAndChildrenGraphics(target); }`
    //    —— 这就是「注册 / 清空成对」的原版出处）。
    // 🔴 为什么补这三条：`grep RaycastFilter Editor/*.cs` 此前 **0 命中** —— 这族一条断言都没有，而写坏**不会报任何错**：
    //   ① 把「空表」当成「全禁」⇒ **整层菜单点不动**（教程指针撤掉后玩家卡死）；
    //   ② 给 `SetRaycastFilter<T>()` 编一个调用点 ⇒ 出货行为与原版分叉（原版那方法是**死码**）；
    //   ③ 注册了不反注册 ⇒ 教程结束后**只有那个子树能点**。
    /// <summary>原版 `RaycastFilter`（教程期「只许点某个子树」的全局允许表）的三条真值：
    /// **空表 = 不过滤** · **`filteredType` 恒 null（死码口）** · **注册 / 清空成对**。
    /// <para>⚠️ 本函数在**全局静态态**上作业 ⇒ 自己建一棵小树、**进出都把表清空**（`finally` 里也清）。</para>
    /// <para>⚠️ 还没有调用方：设计上应由**外壳宿主**（`Editor/ShellScene.cs` 的 `Run()`）调一行 —— 见报告 §5 交接。</para></summary>
    public static void RaycastFilterAsserts(CheckSink s, string what = "RaycastFilter（教程期命中白名单）")
    {
        Section(s, what);
        RaycastFilter.ClearRaycastFilters();          // 起点必须是干净态（唯一出口，见实现文件）
        True(s, !RaycastFilter.Active && RaycastFilter.AllowedCount == 0,
             what + "：**起点是干净态**（`Active == false` 且表为空 —— 否则下面几条的前提不成立）");

        var root = new GameObject("RF_Root");
        var c1 = new GameObject("RF_C1"); c1.transform.SetParent(root.transform, false);
        var c2 = new GameObject("RF_C2"); c2.transform.SetParent(root.transform, false);
        var other = new GameObject("RF_Other");
        try
        {
            // ── ① 空表 = 不过滤（⛔ 别实现成「空表 = 全禁」：那会让整层菜单点不动，而且零报错）──
            True(s, RaycastFilter.Allows(root), what + "：**空表 ⇒ 放行**（原版语义）");
            True(s, RaycastFilter.Allows(null), what + "：空表时传 null 也放行（谓词①根本不进）");

            // ── ② 死码口：`SetRaycastFilter<T>()` 全库 0 调用点 ⇒ 出货 `filteredType` **恒 null** ──
            True(s, RaycastFilter.FilteredTypeForTest == null,
                 what + "：**`filteredType` 恒 null**（`SetRaycastFilter<T>` 是死码，⛔ 别给它编调用点）");
            RaycastFilter.SetRaycastFilter<RectTransform>();     // 只为本函数断「它按形状仍生效」而调一次
            True(s, RaycastFilter.FilteredTypeForTest == typeof(RectTransform),
                 what + "：`SetRaycastFilter<T>` 按形状仍生效（仅自检调用；生产里 0 调用点）");
            True(s, !RaycastFilter.Allows(c1),
                 what + "：置成 `RectTransform` 后 `Allows` 走**谓词②**（无该组件 ⇒ 剔除）");
            RaycastFilter.ClearRaycastFilters();
            True(s, RaycastFilter.FilteredTypeForTest == null && !RaycastFilter.Active,
                 what + "：`ClearRaycastFilters()` 连 `filteredType` 一起清空");

            // ── ③ 注册 / 反注册成对（含「原版不去重」与「两颗 GameObject 任一放行」）──
            RaycastFilter.RegisterObjectAndChildrenGraphics(root);
            int n = RaycastFilter.AllowedCount;
            True(s, n > 0, what + "：注册后表非空（n=" + n + "）");
            True(s, RaycastFilter.Allows(root), what + "：注册后**目标自己**放行");
            True(s, RaycastFilter.Allows(c1), what + "：注册后**子件**放行（原版扫 `Graphic` / 我们扫全子树）");
            True(s, !RaycastFilter.Allows(other), what + "：**表外的对象被剔除**（谓词①）");
            True(s, RaycastFilter.AllowsHit(other, c1),
                 what + "：`AllowsHit(按钮, 命中图)` **任一被登记即放行**（只登记 button 会漏掉「目标 = 按钮里的某颗图」）");

            int before = RaycastFilter.AllowedCount;
            RaycastFilter.Register(c2); RaycastFilter.Register(c2);
            Check(s, RaycastFilter.AllowedCount, before + 2,
                  what + "：`Register` **不去重**（原版如此：同一颗注册两次就有两条）");
            RaycastFilter.UnRegister(c2); RaycastFilter.UnRegister(c2);
            Check(s, RaycastFilter.AllowedCount, before, what + "：`UnRegister` 一次只摘一条（`List.Remove` 语义）");

            RaycastFilter.UnRegisterObjectAndChildrenGraphics(root);
            Check(s, RaycastFilter.AllowedCount, 0,
                  what + "：**反注册后表回到 0**（注册/清空成对 —— 不自洽 = 教程结束后只有那个子树能点）");
            True(s, !RaycastFilter.Active, what + "：表空 ⇒ `Active == false`（回到起点）");
        }
        finally
        {
            RaycastFilter.ClearRaycastFilters();      // 静态全局态必须还原（别的宿主可能紧接着跑）
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(other);
        }
    }

    // ============================================================ 通道断言（B89-E16d 追加在类尾）

    /// <summary>剪贴板取值（批处理 / 无剪贴板环境会抛 ⇒ 一律折成 null，⛔ 不让它把整条自检炸掉）。</summary>
    static string SafeClip()
    {
        try { return GUIUtility.systemCopyBuffer; }
        catch (System.Exception e) { Debug.LogWarning("[MenuCheck] 读系统剪贴板抛异常（" + e.GetType().Name + "）⇒ 按 null 处理"); return null; }
    }

    /// <summary>**「瞬时横幅」（原版 `UIMessageController`）≠「模态弹窗」（`WindowsManager.ShowPopUp`）** 的四条断言。
    /// <para>起因 = 2026-10-28 `B89-E16b/E16c`：原版三处 `UIMessageController.ShowError` 被我们接到了模态窗上
    /// （玩家可见差别 = **自己消失** vs **必须点一下**）。本函数把修好的三处钉住。</para>
    /// <para>🔑 **判据三件同时成立才算「走横幅」**：`MessageToast.ShowCount` +1 · `LastRawText` == 本案例的键 ·
    /// `WindowsManager.popUpWindow == null`。⚠️ 单看前两条会被「横幅 + 模态一起弹」骗过 ⇒ 第三条是牙。</para>
    /// <para>⚠️ 本函数会**临时建**一颗 `MessageToast`（宿主挂壳上）与两个一次性 GameObject ⇒ `finally` 全部销毁：
    /// `ShellScene.Run` 收尾有 `SaveScene`，漏销就会被烘进 `Shell.unity`。</para>
    /// <para>⚠️ 第 ④ 条只能**读源码**断：`DeckInfoPopup` 的 `state == Import` 那一支在本 build **不可达**
    /// （`CanImportDeck` 恒 true 且该 state 无调用点）⇒ 同 `CollectionScene` 的 `A1051(4)` 那条先例。</para></summary>
    public static void ToastChannelAsserts(CheckSink s, string what = "Toast/横幅通道（原版 UIMessageController）")
    {
        Section(s, what);
        var made = new List<GameObject>();
        string clipBefore = SafeClip();
        try
        {
            var mt = MessageToast.Ensure();
            True(s, mt != null && mt.Built, what + "：横幅宿主建出来了（`MessageToast.Ensure` ⇒ 池子 5 条）");

            // ── ① 决斗窗「没这个模式的卡组」⇒ 横幅（原版 `DuelPopupWindow__CanPlay.c:24`）──
            int c0 = mt != null ? mt.ShowCount : 0;
            bool can = DuelPopupWindow.CanPlayForTest((RuleEngine.GameMode)999);   // 999 = 没有任何卡组的模式值 ⇒ 必走错误支
            True(s, !can, what + "：`CanPlay(没有卡组的模式)` ⇒ false（前提：错误支真的走到了，否则下面两条空转）");
            int c1 = mt != null ? mt.ShowCount : 0;
            Check(s, c1, c0 + 1, what + "：**走横幅** —— `MessageToast.ShowCount` +1（若是模态窗，这一条**不变** ⇒ 红）");
            Check(s, mt != null ? mt.LastRawText : null, "MainMenu/General/DontHaveDeckForGameMode",
                  what + "：弹的是官方词条键（原版 `UIMessageController.ShowError(键, localize:1)`）");
            True(s, WindowsManager.Instance == null || WindowsManager.Instance.popUpWindow == null,
                 what + "①：**没有模态窗**（`popUpWindow == null`）—— 这条挡「横幅 + 模态一起弹」");

            // ── ② 删卡组失败 ⇒ 横幅（原版 `__DeleteDeck_b__27_3.c:18`，键 `MenuDeck/Error/FailedToDelete`）──
            var dipGo = new GameObject("E16d_DeckInfo"); made.Add(dipGo);
            var dip = dipGo.AddComponent<DeckInfoPopup>();
            dip.DeckIndex = -1;                       // 越界 ⇒ `CollectionData.DeleteDeck` 必失败（⛔ 不会真删卡组）
            int c2 = mt != null ? mt.ShowCount : 0;
            dip.DeleteDeckNowForTest();
            int c3 = mt != null ? mt.ShowCount : 0;
            Check(s, c3, c2 + 1, what + "：删卡组失败 ⇒ **走横幅** +1（改前只 `Debug.LogWarning`，屏幕上一个字都没有）");
            Check(s, mt != null ? mt.LastRawText : null, "MenuDeck/Error/FailedToDelete",
                  what + "：键 = 原版 `DAT_1842cf1f0` → `0x42CF1F0`（`stringliteral.json` 现读；官方 13 语言表命中）");
            True(s, WindowsManager.Instance == null || WindowsManager.Instance.popUpWindow == null,
                 what + "②：**没有模态窗**（原版这一支是瞬时横幅，⛔ 不是 `ShowPopUp`）");

            // ── ③ 点 Player ID：**没有 id 就不许弹「已复制」**（原版 `ProfileTab.<Initialize>b__11_0` 是**白档**横幅）──
            var ptGo = new GameObject("E16d_ProfileTab"); made.Add(ptGo);
            var pt = ptGo.AddComponent<ProfileTab>();
            int c4 = mt != null ? mt.ShowCount : 0;
            pt.CopyPlayerIdForTest();
            int c5 = mt != null ? mt.ShowCount : 0;
            string clipAfter = SafeClip();
            bool copied = !string.Equals(clipAfter, clipBefore, System.StringComparison.Ordinal);
            Check(s, c5 - c4, copied ? 1 : 0,
                  what + "③：**剪贴板变了 ⇔ 弹了提示**（⛔ 不许「弹了却说没复制」，也⛔ 不许「复制了却静默」）"
                + "（今天 `PlayerIdForCopy()` 恒 null = PlayfabId 属 P3 ⇒ 期望两边都是 0；接上账户域后自动变 1）");
            if (copied) True(s, !string.IsNullOrEmpty(clipAfter) && clipAfter != ProfileTab.Dash,
                             what + "③：真复制了 ⇒ 剪贴板非空、且**不是**空态占位 `-`（⛔ 不拿假值糊弄）");

            // ── ④ 卡组信息窗「导入态点编辑」⇒ 也必须是横幅（原版 `DeckInfoPopup__EditDeck.c:21-45`）──
            string src = Path.Combine(Application.dataPath, "CardPresentation/Shell/DeckInfoPopup.cs");
            if (!File.Exists(src))
                True(s, false, what + "④：读得到 `Shell/DeckInfoPopup.cs`（路径 " + src + "）—— 读不到时这一条**等于没验** ⇒ 当场红");
            else
            {
                bool hasBanner = false, hasModal = false;
                foreach (var raw in File.ReadAllLines(src))
                {
                    var t = raw.TrimStart();
                    if (t.StartsWith("//")) continue;                      // 注释不算（含 `///`，里面有旧写法）
                    if (t.IndexOf("MessageToast.ShowError(msg, false)", System.StringComparison.Ordinal) >= 0) hasBanner = true;
                    if (t.IndexOf("ShowPopUp(msg", System.StringComparison.Ordinal) >= 0) hasModal = true;
                }
                True(s, hasBanner, what + "④：`Import` 支调的是 `MessageToast.ShowError(msg, false)`（瞬时红横幅，原版 `localize = 0`）");
                True(s, !hasModal, what + "④：而且**不再**调 `ShowPopUp(msg`（模态 + OK 钮 = 原版这一支没有的行为）");
            }
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i]);
            MessageToast.DisposeForTest();            // 壳场景收尾有 `SaveScene` ⇒ 漏销会被烘进 `Shell.unity`
            try { if (clipBefore != null) GUIUtility.systemCopyBuffer = clipBefore; } catch { }   // 别把用户的剪贴板改掉
        }
    }
}
