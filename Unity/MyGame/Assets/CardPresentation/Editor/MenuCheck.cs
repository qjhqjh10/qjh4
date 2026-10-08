// MenuCheck.cs — **「7 份自检宿主」那套逐份重复的辅助函数的唯一实现处**（🆕 2026-10-18 · 第四会话）。
//
// 判据 / 逐份差异表 / 调用面规模 / 撞车清单 → `资料/普查产出_第四会话/施工单_自检辅助函数收口.md`
// （本文件按它 ⑤ 的「**状态对象 + 逐宿主一行转发**」形状落地）。
//
// 收口前 = 7 份逐字重复（`CollectionScene` / `DeckScene` / `MainMenuScene` / `RewardsScene` /
// `SettingsScene` / `ShellScene` / `ShopScene`），**≈ 5,534 个调用点 · 0 处跨文件调用**。
//
// 🔴 **为什么计数器不能是一份全局 `static`**：`Editor/RewardWindowFixture.cs:12-14` 已经踩过并写下判据
//    —— 「`ClickCollectAndDismiss` 留在 `RewardsScene`：它要靠那个宿主的 `Check`/`CheckTrue`（**前两个
//    是逐宿主的计数器：拿别处的 `Check` 去断，失败会记进别人的合计里 ⇒ 静默**）」。
//    ⇒ 计数器住在 `CheckSink`（**一个宿主一个实例**），宿主保留**同名的一行转发**。
//
// 🔴 **为什么每个宿主【必须】仍留名叫 `CheckTrue` / `CheckNear` 的符号**：
//    `Shell/MenuDraw.cs:2284` 的 `CheckShadeRule(MenuCheck chk, …)`（32 个调用点）与 `:2454` 的
//    `CheckAbsorbRule(MenuCheck chk, MenuNear near, …)`（33 个调用点）**把宿主的方法组当参数吃进去**
//    ⇒ 这 65 个调用点 + `MenuDraw` 的公开签名**一个字都不用改**。
//
// 🔴 **逐宿主的口径一格都不许统一**（`CheckSink.Near` / `.BlankNote` / `.StarNotBlank`）：
//    `CheckNear` 有 **3** 个变体、`Shoot` 有 **6** 个变体，差别都在**日志文案**；而本活的回归判据是
//    **stdout 逐字节相同**（含 `✓` 行文本，以及 `SettingsScene.CaptureLogs` 那一族**按文本**判的断言）。
//
// 🔴 **类名与 `MenuDraw` 里那个同名委托**：`Shell/MenuDraw.cs:2274` 声明了嵌套委托
//    `MenuDraw.MenuCheck`（= `CardPresentation.MenuDraw.MenuCheck`），与本全局类**不是同一个类型**。
//    **2026-10-18 零风险探针实测**：先只建一个空类 `MenuCheck` → 跑秒级类型检查 ⇒ 运行时/编辑器两个
//    程序集**均 0 错**，且 `CS0433` / `CS0436`（同名类型冲突）**0 条** ⇒ 照账用 `MenuCheck`
//    （`Editor/RewardWindowFixture.cs:8` 明写**这个文件名是留给本族**的）。

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
    /// ⚠️ **它不认识 `A/B/C` 这种路径写法**（`RewardsScene.cs:124` 上面那条注释踩过）——
    /// 要路径用各宿主自己的 `FindPath` 那一族。</summary>
    public static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ============================================================ 截图（6 个变体）

    /// <summary>截图 —— 7 份宿主原来各自一份（**6 个变体**）。合并后**逐宿主的行为一个字不变**：
    /// <list type="bullet">
    /// <item><b>`guardBlank`</b> = 那条「空图护栏（平均亮度 &gt; 3）」开不开。`DeckScene:5901` /
    ///   `MainMenuScene:475` 那两份**原来就没有**（无 `MeanBrightness`、无 `allowBlank` 形参）
    ///   ⇒ 传 `false`。🔴 **不许顺手给这两条宿主补上护栏** —— 那会给它们**新增断言**（若它们真拍到过
    ///   全黑图，绿会变红）；那笔账**另记**，⛔ 不在本件里做。</item>
    /// <item><b>`meanBrightness`</b> 逐宿主传**方法组**（⛔ **没有**合并成一份共享实现）：5 份里
    ///   `SettingsScene:3059` 那一份是 `double` 累加 + 分母 `px.Length / 7.0`，而 Collection / Rewards /
    ///   Shell / Shop 四份是 `long` 累加 + 分母 `(px.Length + 6) / 7`（= 采样数）⇒ **数学上等价、浮点值
    ///   相对差 ~1.4e-6**（1920×1080 时 `lum≈40` ⇒ 绝对差 ≈ 6e-5）。而本活的回归判据是 **stdout 逐字节
    ///   相同**，且 `lum` 会打进 `✓ … 不是空图（平均亮度 {lum:F1} > 3）` 那一行 ⇒ **合一会动那一行**。
    ///   零风险优先 ⇒ 保留逐份实现、只把它当参数传进来（`guardBlank:false` 的两份传 `null`、不会被调）。</item>
    /// </list></summary>
    public static void Shoot(CheckSink s, string shotDir, string file,
                             bool allowBlank = false, bool guardBlank = false,
                             System.Func<Texture2D, float> meanBrightness = null)
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
        File.WriteAllBytes(Path.Combine(shotDir, file), tex.EncodeToPNG());
        // 🔴 **空图护栏**（2026-09-23 踩到）：`Shoot` 原来是「拍完就写盘」，于是一张**全黑**的图也能安静地
        //    写出去（原因：上一个窗的 `CloseAllWindows()` 把要拍的那个窗也关了）。判据 = **平均亮度**
        //    （0–255）；模板黑底大约 14~40，纯黑 ≈ 0。
        //    ⚠️ **必须在 `DestroyImmediate(tex)` 之前**（销毁之后 `tex == null`，护栏恒红）。
        if (guardBlank)
        {
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
