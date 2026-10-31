// B112_R1W_Assert.cs — 音频「Ogg 时长被 granule 截断」的 Unity 侧判据（Editor-only）
//
// 为什么有这个文件：`B112_R1` 查出工程里 1,779 个单声道 .ogg 的**末页 granule 是错的**
//   （内容对、时长只报 6%–20%）。但「Unity 到底按不按 granule 取长度」**本机没有证据**，
//   所以先只改 1 条探针，用这个断言读出 Unity 实际导入的长度再决定要不要全量改。
//
// 判读（只看 `CONTROL` 那一行）—— ⚠️ **这个判定 2026-11-01 已经做过了，结论 = 「Unity 认 granule」**：
//   · 当时的控制件 `Sautekh_Zahndrekh_cant`（granule=12,288 / 包里有 65,600）读到 **0.256 s** ⇒ Unity 按 granule 取长度 ⇒ A 类是真缺陷。
//   · 🔴 **第三波已经把全量 1,778 条（含这个控制件）都改成正确 granule 了** ⇒ 现在再跑，这一行会读 **1.3667 s**，
//     ⛔ **不要把它读成「Unity 不认 granule」** —— 那是「文件已经修好了」，不是「当初的判定被推翻」。
//   探针 `Sautekh_Orikan_gen2`（已改 granule）两边都应是 **3.800 s**，它只用来确认「改了没把文件改坏」。
//
// 用法（命令行；⚠️ 会读 Resources，必须在 Unity 里跑，不要 -quit 以外的花活）：
//   UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod B112_R1W_Assert.Run -logFile -
//
// ⚠️ 这是**一次性判据口**，不是常驻自检；跑完看日志里 `B112_R1W` 的行即可。
using UnityEngine;

public static class B112_R1W_Assert
{
    const string P = "B112_R1W ";

    struct Case
    {
        public string Path;      // Resources 路径（无扩展名）
        public float  Expect;    // 期望时长（秒）；<=0 表示「只打印、不算失败」
        public string Note;
        public Case(string path, float expect, string note) { Path = path; Expect = expect; Note = note; }
    }

    public static void Run()
    {
        var cases = new[]
        {
            // 🔴 原判别件（2026-11-01 第三波已把它也改对了 ⇒ 现在应为 1.3667s，不再是「0.256s=认 granule」那个判据）
            new Case("Art/audio/vo/Sautekh_Zahndrekh_cant", 1.366667f,
                     "原 CONTROL（已修：granule 12,288→65,600）—— 现应为 1.3667s；⛔ 别当成「Unity 不认 granule」"),
            // 探针：已把 granule 改成 182,400（内容本来就是 182,400）
            new Case("Art/audio/vo/Sautekh_Orikan_gen2", 3.8f, "探针（已改 granule）"),
            // B-26：重做后的 OvertimeStart（32-bit float，峰值 1.2772）
            new Case("Art/audio/sfx/OvertimeStart", 5.421083f, "B-26 重做后"),
            // 34 个立体声 ogg 里抽 4 条（修好后应等于原版 m_Length）
            new Case("Art/audio/music/main_theme", 120.4871f, "Main Theme（原版 5,313,481 样点）"),
            new Case("Art/audio/music/menu_idle_theme", 139.7260f, "Menu Idle Theme（6,161,918 样点）"),
            new Case("Art/audio/bgm/Warpforge Aeldari Combat High", 122.6083f, "bgm（5,407,045 样点）"),
            new Case("Art/audio/sfx/MatchVictory", 5.3f, "结算音效（233,730 样点）"),
        };

        int pass = 0, fail = 0, info = 0;
        Debug.Log(P + "==== 开始（granule 判定已于 2026-11-01 做完 = Unity 认 granule；本表用于回归） ====");
        foreach (var c in cases)
        {
            var clip = Resources.Load<AudioClip>(c.Path);
            if (clip == null)
            {
                fail++;
                Debug.LogWarning(P + $"✗ 载不到：{c.Path}（{c.Note}）");
                continue;
            }
            string line = $"{c.Path} · length={clip.length:F6}s · samples={clip.samples} · freq={clip.frequency} · ch={clip.channels} · {c.Note}";
            if (c.Expect < 0f)
            {
                info++;
                Debug.Log(P + "INFO " + line);
            }
            else if (Mathf.Abs(clip.length - c.Expect) <= 0.02f)
            {
                pass++;
                Debug.Log(P + "✓ " + line);
            }
            else
            {
                fail++;
                Debug.LogWarning(P + $"✗ {line} · 期望 {c.Expect:F4}s");
            }
        }
        Debug.Log(P + $"==== 汇总：✓ {pass} · ✗ {fail} · INFO {info} ====");
    }
}
