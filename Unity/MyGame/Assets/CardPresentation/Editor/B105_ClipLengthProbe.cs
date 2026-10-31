// B105_ClipLengthProbe.cs — 「legacy `.anim` 的 `m_StopTime=1` 到底会不会把 clip 截到 1 秒」的**读数探针**。
//
// ============================ 为什么要有这个探针 ============================
// B104 的 `.anim` 逐曲线对账（`资料/完全复刻/判据/B104_anim曲线对账.tsv`）发现：
//   46 条 legacy clip 的 476 条曲线**逐 key 0 差异**，但**我们的 `.anim` 里
//   `m_AnimationClipSettings.m_StopTime` 统一写 `1`**，而原版 legacy 的
//   `m_AnimationClipSettings` **整个是 null**（没有这个字段）。这些 clip 的**末键时间**跨度 0.25–100.2 s。
// 风险：**如果 Unity 运行时按 `StopTime` 取 `clip.length`**，这 46 条就全被截到 1 秒。
// 这个问法**只有 Unity 自己答得了** —— 静态读 YAML 只知道字段写了什么，不知道引擎怎么用它。
//
// 本探针**只读**：不写任何 `.anim`、不改任何资产、不改 `gen_anim_clips.py`。
// 唯一输出 = `<工程根>/_tmp_view/b105_clip_length.tsv`（`_tmp_view` 不进 git）。
//
// ============================ 读什么 ============================
// 对两个目录下的每个 `AnimationClip` 资产读：
//   · `clip.length`        —— **Unity 实际认为的时长**（本探针的核心读数）
//   · `clip.name` / `clip.frameRate` / `clip.legacy` / `clip.wrapMode`
//   · `AnimationUtility.GetAnimationClipSettings(clip)` 的 `startTime` / `stopTime` / `loopTime`
//                          —— 编辑器侧（Animation 窗口 / 导入器）看到的 StopTime
//   · **末键时间** = 遍历所有曲线（`AnimationUtility.GetCurveBindings` + `GetEditorCurve`）取 key 的最大 time；
//     API 拿不到曲线时退回**解析 `.anim` 文本**的 curve 段（`curveSource` 列写明是哪一种）。
//   · `.anim` 文本里的 `m_StopTime`（交叉核对 settings.stoptime）
//
// ============================ 判定规则（写进 TSV 的 verdict 列） ============================
//   `NO_CURVES`  —— 没有曲线（muscle clip / 空壳，如 `Card Explosion`）⇒ 本探针不下结论
//   `OK_LASTKEY` —— |`clip.length` − 末键| ≤ 1e-3  ⇒ **Unity 忽略 StopTime，length = 末键**
//                  （⇒ 只需台账订正，`gen_anim_clips.py` 不用改）
//   `TRUNCATED`  —— |`clip.length` − StopTime| ≤ 1e-3 且 StopTime < 末键 − 1e-3
//                  ⇒ **Unity 确实按 StopTime 截断**（⇒ 必须改生成器）
//   `MISMATCH`   —— 两者都不等（要人看：可能是 sampleRate 取整、也可能是别的口径）
//
// ============================ 用法（批处理） ============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod B105ClipLengthProbe.Run -logFile -
//   筛输出：grep "^B105 "
//   ⚠️ 本文件**只读资产、只写一个 `_tmp_view/*.tsv`**；不 SaveScene、不动任何 `.anim`。
//   ⚠️ 类名必须 == 文件名、必须在全局命名空间（本工程存盘/脚本资产的硬要求，否则会烘成 `!u!115` 存根）。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class B105ClipLengthProbe
{
    const string OutRel = "_tmp_view/b105_clip_length.tsv";
    const float Eps = 1e-3f;

    static readonly string[] Dirs =
    {
        "Assets/WarpforgeVFX/Animations",
        "Assets/CardPresentation/Resources/Animations",
    };

    static readonly string[] CurveSections =
    {
        "m_RotationCurves", "m_EulerCurves", "m_PositionCurves", "m_ScaleCurves",
        "m_FloatCurves", "m_PPtrCurves", "m_CompressedRotationCurves", "m_QuaternionCurves",
    };

    static readonly Regex TimeLine = new Regex(@"^\s*time:\s*(-?[0-9]+(?:\.[0-9]*)?(?:[eE][-+]?[0-9]+)?)\s*$");
    static readonly Regex StopTimeLine = new Regex(@"^\s*m_StopTime:\s*(-?[0-9]+(?:\.[0-9]*)?(?:[eE][-+]?[0-9]+)?)\s*$");

    /// <summary>批处理入口：`-executeMethod B105ClipLengthProbe.Run`。</summary>
    public static void Run()
    {
        string outPath = Path.Combine(ProjectRoot(), OutRel);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        var lines = new List<string>();
        lines.Add(string.Join("\t", new[]
        {
            "assetPath", "clipName", "length", "lastKey", "delta_len_minus_lastKey",
            "curveCount", "curveSource", "frameRate", "legacy", "wrapMode",
            "settings_startTime", "settings_stopTime", "settings_loopTime",
            "yaml_stopTime", "verdict",
        }));

        int seen = 0, ok = 0, truncated = 0, mismatch = 0, noCurves = 0;
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string dir in Dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogWarning("B105 SKIP 目录不存在: " + dir);
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { dir });
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !seenPaths.Add(path)) continue;
                seen++;

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    Debug.LogWarning("B105 WARN 取不到 AnimationClip: " + path);
                    continue;
                }

                int curveCount;
                float lastKey;
                string curveSource;
                ReadLastKey(clip, path, out curveCount, out lastKey, out curveSource);

                float settingsStart = float.NaN, settingsStop = float.NaN;
                float settingsLoop = float.NaN;
                try
                {
                    AnimationClipSettings st = AnimationUtility.GetAnimationClipSettings(clip);
                    settingsStart = st.startTime;
                    settingsStop = st.stopTime;
                    settingsLoop = st.loopTime ? 1f : 0f;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("B105 WARN GetAnimationClipSettings 失败 (" + path + "): " + e.GetType().Name);
                }

                float yamlStop = ReadYamlStopTime(path);

                string verdict = Judge(clip.length, lastKey, settingsStop, curveCount);
                if (verdict == "OK_LASTKEY") ok++;
                else if (verdict == "TRUNCATED") truncated++;
                else if (verdict == "MISMATCH") mismatch++;
                else noCurves++;

                lines.Add(string.Join("\t", new[]
                {
                    path,
                    clip.name,
                    N(clip.length),
                    float.IsNegativeInfinity(lastKey) ? "NaN" : N(lastKey),
                    (float.IsNegativeInfinity(lastKey) || curveCount == 0) ? "NaN" : N(clip.length - lastKey),
                    curveCount.ToString(CultureInfo.InvariantCulture),
                    curveSource,
                    N(clip.frameRate),
                    clip.legacy ? "1" : "0",
                    clip.wrapMode.ToString(),
                    N(settingsStart),
                    N(settingsStop),
                    N(settingsLoop),
                    float.IsNaN(yamlStop) ? "NaN" : N(yamlStop),
                    verdict,
                }));

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "B105 {0,-12} length={1:0.###} lastKey={2:0.###} stop={3:0.###} curves={4} src={5} — {6} :: {7}",
                    verdict, clip.length,
                    float.IsNegativeInfinity(lastKey) ? -1f : lastKey,
                    float.IsNaN(settingsStop) ? -1f : settingsStop,
                    curveCount, curveSource, clip.name, path));
            }
        }

        File.WriteAllText(outPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));

        Debug.Log(string.Format(CultureInfo.InvariantCulture,
            "B105 合计 读={0} OK_LASTKEY={1} TRUNCATED={2} MISMATCH={3} NO_CURVES={4}",
            seen, ok, truncated, mismatch, noCurves));
        Debug.Log("B105 产出: " + outPath);
    }

    // ---------------------------------------------------------------- 末键

    /// <summary>所有曲线 key 的最大 time。API 优先，API 拿不到曲线时退回解析 `.anim` 文本。</summary>
    static void ReadLastKey(AnimationClip clip, string path, out int count, out float last, out string source)
    {
        count = 0;
        last = float.NegativeInfinity;
        source = "api";

        try
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings != null)
            {
                foreach (EditorCurveBinding b in bindings)
                {
                    AnimationCurve c = AnimationUtility.GetEditorCurve(clip, b);
                    if (c == null || c.length == 0) continue;
                    count++;
                    float t = c.keys[c.length - 1].time;
                    if (t > last) last = t;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("B105 WARN GetCurveBindings 失败 (" + path + "): " + e.GetType().Name);
            count = 0;
        }

        if (count > 0) return;

        // 退回：解析 YAML 文本（只认 curve 段里的 `time:`，⛔ 别把 m_Events 的时间算进来）
        int yamlCount;
        float yamlLast;
        if (TryParseCurveKeysFromText(path, out yamlCount, out yamlLast) && yamlCount > 0)
        {
            count = yamlCount;
            last = yamlLast;
            source = "yaml";
        }
        else
        {
            source = "none";
        }
    }

    static bool TryParseCurveKeysFromText(string path, out int count, out float last)
    {
        count = 0;
        last = float.NegativeInfinity;
        string full;
        try
        {
            full = File.ReadAllText(path);
        }
        catch (Exception)
        {
            return false;
        }

        bool inCurve = false;
        bool inCurveBody = false;
        foreach (string raw in full.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.Trim();
            if (t.Length == 0) continue;

            // 顶层段（两个空格缩进 + 以冒号结尾）—— ⚠️ 必须排除以 `-` 开头的**列表条目**
            //（`- curve:` / `- serializedVersion: 2` 也以冒号结尾，误当成段会把整段曲线丢掉）
            if (line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("   ", StringComparison.Ordinal)
                && t.EndsWith(":", StringComparison.Ordinal) && !t.StartsWith("-", StringComparison.Ordinal))
            {
                string name = t.Substring(0, t.Length - 1);
                inCurve = Array.IndexOf(CurveSections, name) >= 0;
                inCurveBody = false;
                continue;
            }

            if (!inCurve) continue;

            if (t.StartsWith("- curve:", StringComparison.Ordinal)) { inCurveBody = true; continue; }
            if (t.StartsWith("m_Curve:", StringComparison.Ordinal)) { inCurveBody = true; continue; }
            // `m_FloatCurves` 的条目以 `- serializedVersion: 2` 开头，真曲线在下一层 `curve:`
            if (t.Equals("curve:", StringComparison.Ordinal)) { inCurveBody = true; continue; }
            if (!inCurveBody) continue;

            Match m = TimeLine.Match(line);
            if (!m.Success) continue;
            float v;
            if (float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            {
                count++;
                if (v > last) last = v;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------- StopTime（文本侧交叉核对）

    static float ReadYamlStopTime(string path)
    {
        try
        {
            foreach (string raw in File.ReadAllLines(path))
            {
                Match m = StopTimeLine.Match(raw);
                if (!m.Success) continue;
                float v;
                if (float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    return v;
            }
        }
        catch (Exception)
        {
            // 读不到就返回 NaN，由调用方写 NaN
        }
        return float.NaN;
    }

    // ---------------------------------------------------------------- 判定

    static string Judge(float length, float lastKey, float stopTime, int curveCount)
    {
        if (curveCount <= 0 || float.IsNegativeInfinity(lastKey)) return "NO_CURVES";
        if (Mathf.Abs(length - lastKey) <= Eps) return "OK_LASTKEY";
        if (!float.IsNaN(stopTime) && Mathf.Abs(length - stopTime) <= Eps && stopTime < lastKey - Eps)
            return "TRUNCATED";
        return "MISMATCH";
    }

    // ---------------------------------------------------------------- 杂项

    static string ProjectRoot()
    {
        // Application.dataPath = <根>/MyGame/Assets  ⇒ 上两级 = <根>（= D:/4/Unity）
        string assets = Application.dataPath.Replace('\\', '/');
        var di = new DirectoryInfo(assets);
        return di.Parent != null && di.Parent.Parent != null ? di.Parent.Parent.FullName : assets;
    }

    static string N(float v)
    {
        if (float.IsNaN(v)) return "NaN";
        if (float.IsInfinity(v)) return v > 0 ? "Inf" : "-Inf";
        return v.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
