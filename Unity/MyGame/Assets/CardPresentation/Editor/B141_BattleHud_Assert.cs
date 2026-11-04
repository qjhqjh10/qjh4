// B141_BattleHud_Assert.cs — 执行写手 `B142-E33`（第一百二十九会话 · 2026-11-04）
//   ★ 补的是 **`B141-W46`（8 条 `BattleHud*` clip 斜率键名 `is`/`os` → `si`/`so`）的断言缺口**。
//
// ============================ 它盯的是什么 ============================
//  `工具/gen_battlehud_clips.py` 原来把逐键斜率写进 JSON 的 **`is`/`os`**，而运行期
//  `CardPresentation/Battle/BattleHudAnim.cs` 的 DTO 字段名是 **`si`/`so`**
//  ⇒ `JsonUtility` **按字段名绑定** ⇒ `si`/`so` **恒 null ⇒ 恒 0**（哑弹）；
//  `BattleHudAnim.BuildCurve` 的 `k.si != null && …` 又把它兜成 0，所以**不报错、断言也抓不到**。
//
//  🔴 **为什么以前恒绿**：8 条 clip 的斜率现值**全是 0**（原版今日就是 0，`weightedMode=0` ⇒ 插值是
//     smoothstep），所以「读到 0」与「绑不上而当 0」结果相同 ⇒ 任何**只比数值**的断言都是恒等断言。
//     本文件因此**不比数值**，改盯三件事：
//       ① **键名 ↔ 字段名**（生成器源码 ↔ C# DTO，结构检查）；
//       ② **产物真的绑得上**（`JsonUtility` 反序列化后 `si`/`so` 非 null 且维度 = `v`）；
//       ③ **非零合成用例**（反射调 `BuildCurve`，把 0.37/-0.61 真写进 `Keyframe.in/outTangent`）。
//
//  判据 = `报告/B141_W129_BattleHudAnim.md` §一/§二 · `报告/B141_W129_AnimClips.md` ·
//         `工具/gen_battlehud_clips.py:182-183`（键名） · `Battle/BattleHudAnim.cs:60`（DTO 字段）
//         ＋ `:311-321`（`BuildCurve` 逐键 `new Keyframe(t, v, it, ot)`）。
//
// ============================ 分组 ============================
//  A · 结构：`gen_battlehud_clips.py` 的斜率键名 == `BattleHudKey` 的字段名（且不再是 `is`/`os`）
//  B · 产物：`Resources/battlehud_clips.json` 8 条 / 119 曲线 / **170 个键**，逐键 `si`/`so` 非 null 且维度 = `v`
//  C · 非零合成：反射 `BuildCurve`，`si=0.37 / so=-0.61` 必须逐位写进 `Keyframe`
//  D · 消费端：`BattleHudAnim` 仍然按 `k.si` / `k.so` 读（挡住「改 JSON 去迁就旧字段名」这种反向修法）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 生成器改回 `"is": _vec(k["inSlope"]…)` ⇒ **A1/A2/A4 红**；重生成产物后 B1/B3 也红。
//  · 手工把 JSON 的 `si`/`so` 改回 `is`/`os` ⇒ **A5/B1/B3 红**（`JsonUtility` 绑不上 ⇒ null）。
//  · DTO 字段改名（`si` → `is`）⇒ A3/A4 红（A 组是双向比对，不是只看一边）。
//  · `BuildCurve` 退回 `new Keyframe(t, v)`（斜率丢掉）⇒ **C2 红**。
//  · 盘上产物少键 / 少曲线 ⇒ B2/B4 红。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B141_BattleHud_Assert.Run -logFile -
//   接宿主（**零前提**：只读源码 + 读一份 JSON + 调一个纯静态函数，不建对象、不改状态）：
//     同一物理行末尾追加（行数零变化）：
//       B141_BattleHud_Assert.Run(); pass += B141_BattleHud_Assert.Pass; fail += B141_BattleHud_Assert.Fail;
//   ⚠️ 追加前先看那一行已有的 `//` 在哪（本仓踩过 6 次「代码落进行末注释」）。
//   判绿红看 `B141HUDSLOPE === 合计` 那一行，⛔ 不看退出码。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CardPresentation;
using UnityEngine;

/// <summary>写手 `B142-E33`：`B141-W46` 的斜率键名（`si`/`so` ↔ C# 字段）回归断言 + 非零合成用例。</summary>
public static class B141_BattleHud_Assert
{
    const string Tag = "B141HUDSLOPE";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Eq<T>(T got, T want, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(got, want);
        Check(ok, ok ? msg : msg + $" —— 期望 [{want}]，实得 [{got}]");
    }

    static void Section(string t) { Debug.Log(Tag + " --- " + t + " ---"); }

    // ============================================================ 常量
    const string DtoSrc = "CardPresentation/Battle/BattleHudAnim.cs";
    const string RuntimeJson = "CardPresentation/Resources/battlehud_clips.json";

    /// <summary>8 条 clip（`gen_battlehud_clips.py` 的 `CLIP_ORDER`）。</summary>
    static readonly string[] ClipOrder =
    {
        "BattleHudHidden", "BattleHudHide", "BattleHudShow", "BattleHudShowInstant",
        "BattleHudHiddenNoHand", "BattleHudHideNoHand", "BattleHudShowNoHand", "BattleHudShowInstantNoHand",
    };

    /// <summary>运行期副本的键数（`报告/B141_W129_BattleHudAnim.md` §三现读：170 个键 / 119 条曲线）。
    /// ⚠️ 原版数据变了就要同刻改这里与报告，⛔ 别让断言跟着数据一起漂。</summary>
    const int ExpectKeys = 170;

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("B141-HUD · 8 条 `BattleHud*` clip：斜率键名 `si`/`so` 的绑定断言（数值今日全 0 ⇒ 只比数值是恒等断言）");

        StructureGroup();
        ArtifactGroup();
        SyntheticGroup();
        ConsumerGroup();

        Debug.Log(Tag + $" === 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    // ------------------------------------------------------------ A · 结构：生成器键名 ↔ DTO 字段名
    static void StructureGroup()
    {
        Section("A · `gen_battlehud_clips.py` 的斜率键名 == `BattleHudKey` 的字段名（判据 = 报告/B141_W129_BattleHudAnim.md §二·1）");

        string py = ReadAbs(Path.GetFullPath(Path.Combine(Application.dataPath, "../../工具/gen_battlehud_clips.py")));
        Check(!string.IsNullOrEmpty(py), "A0 前提：`工具/gen_battlehud_clips.py` 读得到且非空");
        if (string.IsNullOrEmpty(py)) return;

        string inKey = GenKey(py, "inSlope");
        string outKey = GenKey(py, "outSlope");
        Eq(inKey, "si", "A1 生成器把 `inSlope` 写进键 **`si`**（改前是 `is` ⇒ `JsonUtility` 绑不上 ⇒ 恒 0）");
        Eq(outKey, "so", "A2 生成器把 `outSlope` 写进键 **`so`**（改前是 `os`）");

        string cs = ReadText(DtoSrc);
        Check(!string.IsNullOrEmpty(cs), "A3 前提：`" + DtoSrc + "` 读得到且非空");
        if (string.IsNullOrEmpty(cs)) return;

        var fields = DtoFields(cs);
        Check(fields.Contains("si"), "A3b `BattleHudKey` 有字段 `si`（`public float[] si;`）");
        Check(fields.Contains("so"), "A3c `BattleHudKey` 有字段 `so`");
        Check(fields.Contains("v"), "A3d `BattleHudKey` 有字段 `v`（维度基准）");

        Check(!string.IsNullOrEmpty(inKey) && fields.Contains(inKey),
              $"A4 生成器的 inSlope 键 `{inKey}` 在 DTO 字段里（双向比对：任一边改名都红）");
        Check(!string.IsNullOrEmpty(outKey) && fields.Contains(outKey),
              $"A5 生成器的 outSlope 键 `{outKey}` 在 DTO 字段里");

        // 反向：生成器源码里**不许**再出现把斜率写进 `is`/`os` 的写法
        Check(GenKey(py, "inSlope") != "is" && GenKey(py, "outSlope") != "os",
              "A6 生成器里 `inSlope`/`outSlope` **不再**写成 `is`/`os`（那条就是哑弹的根因）");
    }

    // ------------------------------------------------------------ B · 产物：真绑得上
    static void ArtifactGroup()
    {
        Section("B · `Resources/battlehud_clips.json`：8 条 / 170 键，逐键 `si`/`so` 非 null 且维度 = `v`");

        string txt = ReadText(RuntimeJson);
        Check(!string.IsNullOrEmpty(txt), "B0 前提：`" + RuntimeJson + "` 读得到且非空");
        if (string.IsNullOrEmpty(txt)) return;

        // 键名层（挡「手工把产物改回 is/os」）
        Eq(CountOf(txt, "\"is\":"), 0, "B1 产物里 `\"is\":` 出现 0 次（旧键名已清）");
        Eq(CountOf(txt, "\"os\":"), 0, "B1b 产物里 `\"os\":` 出现 0 次");
        Check(CountOf(txt, "\"si\":") > 0 && CountOf(txt, "\"so\":") > 0,
              "B1c 产物里 `\"si\":` / `\"so\":` 都存在（逐键各一份）");

        BattleHudFile file = null;
        try { file = JsonUtility.FromJson<BattleHudFile>(txt); }
        catch (Exception e) { Debug.LogWarning(Tag + " JsonUtility 解析失败：" + e.Message); }
        Check(file != null && file.clips != null, "B2 前提：`JsonUtility` 把产物解析成 `BattleHudFile`（字段名对不上时这里会是 null）");
        if (file == null || file.clips == null) return;

        Eq(file.clips.Length, ClipOrder.Length, "B3 产物里 8 条 clip");
        var got = new HashSet<string>();
        int curves = 0, keys = 0, missing = 0, dimBad = 0;
        foreach (var c in file.clips)
        {
            if (c != null && !string.IsNullOrEmpty(c.name)) got.Add(c.name);
            if (c == null || c.curves == null) continue;
            foreach (var cv in c.curves)
            {
                curves++;
                if (cv == null || cv.keys == null) continue;
                foreach (var k in cv.keys)
                {
                    if (k == null) continue;
                    keys++;
                    if (k.si == null || k.so == null) { missing++; continue; }
                    int dim = k.v != null ? k.v.Length : 0;
                    if (k.si.Length != dim || k.so.Length != dim) dimBad++;
                }
            }
        }
        Check(got.SetEquals(new HashSet<string>(ClipOrder)),
              "B4 8 条 clip 的名字逐个对上 `CLIP_ORDER`（缺哪条就会在下面这条里显形）"
              + $"［实得 {got.Count} 条：{string.Join(",", new List<string>(got).ToArray())}］");
        Check(curves > 0, $"B5 曲线数 > 0（实得 {curves}；报告现读 119）");
        Eq(keys, ExpectKeys, $"B6 键数 == {ExpectKeys}（产物被截断/重生成错文件都会在这里显形）");
        Eq(missing, 0, "B7 **每个键的 `si`/`so` 都绑上了**（null = `JsonUtility` 不认识这个键名 ⇒ 哑弹）");
        Eq(dimBad, 0, "B8 每个键 `si`/`so` 的维度 == `v` 的维度（1D 标量 / 3D 位置）");
    }

    // ------------------------------------------------------------ C · 非零合成用例
    static void SyntheticGroup()
    {
        Section("C · 非零合成：反射调 `BattleHudAnim.BuildCurve`，0.37 / -0.61 必须逐位写进 `Keyframe`");

        MethodInfo m = null;
        try { m = typeof(BattleHudAnim).GetMethod("BuildCurve", BindingFlags.NonPublic | BindingFlags.Static); }
        catch (Exception e) { Debug.LogWarning(Tag + " 反射取 `BuildCurve` 失败：" + e.Message); }
        Check(m != null, "C0 前提：`BattleHudAnim.BuildCurve(BattleHudKey[], int)` 反射取得到（取不到 ⇒ C1/C2 不可判）");
        if (m == null) return;

        var keys = new[]
        {
            new BattleHudKey { t = 0f, v = new[] { 1f }, si = new[] { 0.37f },  so = new[] { -0.61f } },
            new BattleHudKey { t = 1f, v = new[] { 2f }, si = new[] { 0.5f },   so = new[] { 0.25f } },
            // null 斜率必须仍按 0 走（原版「给 null 就当 0」的兜底不许动）
            new BattleHudKey { t = 2f, v = new[] { 3f }, si = null,             so = null },
        };

        AnimationCurve curve = null;
        try { curve = m.Invoke(null, new object[] { keys, 0 }) as AnimationCurve; }
        catch (Exception e) { Debug.LogWarning(Tag + " 调 `BuildCurve` 抛了：" + e.Message); }
        Check(curve != null && curve.length == keys.Length, "C0b `BuildCurve` 返回 3 个关键帧的曲线");
        if (curve == null || curve.length != keys.Length) return;

        Eq(curve[0].inTangent, 0.37f, "C1 第 1 键 `Keyframe.inTangent` == 0.37（**非零**：证明斜率真的写进了 Keyframe，不是恒 0）");
        Eq(curve[0].outTangent, -0.61f, "C2 第 1 键 `Keyframe.outTangent` == -0.61（负号也要原样保真）");
        Eq(curve[1].inTangent, 0.5f, "C3 第 2 键 `inTangent` == 0.5");
        Eq(curve[1].outTangent, 0.25f, "C3b 第 2 键 `outTangent` == 0.25");
        Eq(curve[2].inTangent, 0f, "C4 第 3 键 `si = null` ⇒ `inTangent` == 0（兜底不变）");
        Eq(curve[2].outTangent, 0f, "C4b 第 3 键 `so = null` ⇒ `outTangent` == 0");
        Check(Mathf.Abs(curve[0].value - 1f) < 1e-6f && Mathf.Abs(curve[1].value - 2f) < 1e-6f,
              "C5 键值同样写对（别为了过 C1/C2 把 `v` 挤掉）");
    }

    // ------------------------------------------------------------ D · 消费端仍在按 si/so 读
    static void ConsumerGroup()
    {
        Section("D · 消费端 `BattleHudAnim` 仍然按 `k.si` / `k.so` 读（⛔ 别改成让 JSON 去迁就旧名）");

        string cs = ReadText(DtoSrc);
        Check(!string.IsNullOrEmpty(cs), "D0 前提：`" + DtoSrc + "` 读得到");
        if (string.IsNullOrEmpty(cs)) return;
        Check(cs.IndexOf("k.si", StringComparison.Ordinal) >= 0, "D1 `BattleHudAnim` 里出现 `k.si`（`BuildCurve` 的读口）");
        Check(cs.IndexOf("k.so", StringComparison.Ordinal) >= 0, "D2 `BattleHudAnim` 里出现 `k.so`");
        Check(cs.IndexOf("new Keyframe(k.t, v, it, ot)", StringComparison.Ordinal) >= 0,
              "D3 `BuildCurve` 仍是逐键 `new Keyframe(t, v, inTangent, outTangent)`（⛔ 退回 `new Keyframe(t,v)` 会让斜率再丢一次）");
    }

    // ============================================================ 纯函数
    /// <summary>从生成器源码里取「某个原版字段被写进哪个 JSON 键」：`"<key>": _vec(k["<field>"]`。
    /// 找不到 ⇒ `null`（⛔ 不猜）。</summary>
    static string GenKey(string py, string field)
    {
        if (string.IsNullOrEmpty(py)) return null;
        var m = Regex.Match(py, "\"([A-Za-z_][A-Za-z0-9_]*)\"\\s*:\\s*_vec\\(\\s*k\\[\"" + Regex.Escape(field) + "\"\\]");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>`public class BattleHudKey { … }` 里的 `float[]` 字段名集合。</summary>
    static HashSet<string> DtoFields(string cs)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        int i = cs.IndexOf("class BattleHudKey", StringComparison.Ordinal);
        if (i < 0) return set;
        int open = cs.IndexOf('{', i);
        if (open < 0) return set;
        int close = cs.IndexOf('}', open);
        if (close < 0) return set;
        string body = cs.Substring(open, close - open);
        foreach (Match m in Regex.Matches(body, "public\\s+float\\[\\]\\s+([A-Za-z_][A-Za-z0-9_]*)"))
            set.Add(m.Groups[1].Value);
        return set;
    }

    static string ReadText(string assetsRel)
    {
        string p = Path.Combine(Application.dataPath, assetsRel);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    static string ReadAbs(string abs)
    {
        return File.Exists(abs) ? File.ReadAllText(abs) : null;
    }

    static int CountOf(string text, string needle)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
