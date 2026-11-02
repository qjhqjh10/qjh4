// B117_G10_10b_Assert.cs — 执行代理 `B117-G10-10b`（战斗域第 10 部分**第二片**：`WarpforgeVFX/Runtime`）的断言
//
// ============================ 它盯的是什么 ============================
// 本片**只做了一处代码改动**（其余全是缺口清单，见 `资料/完全复刻/判据/B117-G10-10b_VFX运行层缺口清单.md`）：
//   · `WarpforgeVFX/Runtime/WFExtraShakeAndSound.cs` 的 `ExtraTick`：
//     原版 `PlaySoundOnTime__Update.c:68-70` 每调一次**最多播一条**（末尾 `numberOfTimesPlayed += 1; return;`），
//     改前我们是 `while`（一帧把欠的全播掉）。同仓 2026-10-29（账 `音频_语音:71`）已把
//     `WarpforgeEffectPlayer.TickSounds` 的同一处从 `while` 改成 `if`，这一处当时漏改，
//     而且它的注释还写着「与 `WarpforgeEffectPlayer.TickSounds` 同口径」——**那句在改完之后就不成立了**。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/PlaySoundOnTime.cs:19-48`（**有真体**：`if (played < n)` +
//     末尾 `numberOfTimesPlayed += 1`，**没有 while / for**）
//   · `d:/2/tools/decomp_full/PlaySoundOnTime__Update.c:64-70`（机器码同形：一次调用只 `+1` 然后 `return`）
//   · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/ShakeAndSoundObject.cs:12-40`
//     （`OnEnable` 逐个 `Play()`；`Update` 逐条 `PlaySoundOnTime.Update(Time.deltaTime, transform)`）
//   · 出货数据（`Resources/WarpforgeVFX/effect_extras.json` 现读）：`ShakeAndSoundObject` **35 挂载 / 24 条音效槽**，
//     **24/24 `time == 0` 且 `total == 1`** ⇒ 今天「`if` vs `while`」**零可见差异**（`total == 1` 时两种写法都只播一次）。
//     这条断言是**结构判据**（防将来数据里出现 `total > 1` 的那一天，也防有人把它改回去）。
//
// ============================ 怎么用 ============================
//   `B117G1010bAssert.Run();`   （静态类 + 无参 `Run()`；宿主由调度台接，本文件不改任何 `Editor/*Scene.cs`）
//   本文件**不建持久资产**：建出来的 GameObject 在 `finally` 里 `DestroyImmediate`；
//   `WFSoundPlayer.Enabled` 与 `WFSoundPlayer.Played` 计数在 `finally` 里还原。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2 ← 把 `WFExtraShakeAndSound.ExtraTick` 的 `if` 改回 `while`（`total = 3`、`interval = 0` 的夹具）
//          ⇒ 一次 Tick 播 **3** 条而不是 1 条 ⇒ A2 红。
//   · A3 ← 同上（两次 Tick 之后累计 6 而不是 2）⇒ A3 红。
//   · A4 ← 去掉 `_played[i] < s.total` 这个上界（或让 `_played` 不累加）⇒ 第 4 次 Tick 还在播 ⇒ A4 红。
//   · A1 是**前提硬检查**（⚠️ 不是 `if (前提) Check(结论)`）：音效表读不到 / 挑不出可用 cue 时**当场红**，
//     ⛔ 不许静默跳过（本仓踩过三次「前提不成立 ⇒ 断言一条没跑，日志里只有 ✓」）。
//
// ============================ 🔴 本文件**不覆盖**的 ============================
//   · 音效**真的响了**、随机 clip / 音高 / 音量听感对不对 —— 那要真机。
//   · `timeToPlayAgain`（原版 `AudioCue.ConfigAudioSource` 的逐 cue 冷却）—— **我们整个 `WFSoundPlayer` 层没有它**
//     （355/513 条 FX cue 的该值 > 0）。那是**已知缺口**，清单第 1 条；
//     ⛔ 这里**不写一条断言把它锁成「正确」**（锁了就等于把缺口写成判据）。
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;

public static class B117G1010bAssert
{
    const string Tag = "[B117-G10-10b]";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    /// <summary>从**装好的音效表**里挑一条可用 cue（前提硬检查用；挑不到直接红）。</summary>
    static string PickCue()
    {
        var ta = Resources.Load<TextAsset>(WFSoundBank.TablePath);
        if (ta == null) return null;
        WFSoundTable tbl = null;
        try { tbl = JsonUtility.FromJson<WFSoundTable>(ta.text); }
        catch { tbl = null; }
        if (tbl == null || tbl.cues == null) return null;
        for (int i = 0; i < tbl.cues.Length; i++)
        {
            var c = tbl.cues[i];
            if (c == null || string.IsNullOrEmpty(c.name)) continue;
            if (c.clips == null || c.clips.Length == 0) continue;
            if (c.timeToPlayAgain > 0f) continue;    // 只用无冷却的 cue：将来若补上冷却闸，本断言也不受影响
            return c.name;
        }
        return null;
    }

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        bool wasEnabled = WFSoundPlayer.Enabled;
        var temps = new List<Object>();

        try
        {
            WFSoundPlayer.Enabled = false;           // 只记账不发声（批处理里没有帧循环，也不该出声）
            WFSoundPlayer.ResetCounter();

            // ---------------- A1 前提（硬检查，⛔ 不许 if(前提)Check(结论)）----------------
            string cueName = PickCue();
            Check(cueName != null,
                  "A1 前提：从 `Resources/" + WFSoundBank.TablePath + ".json` 挑出一条可用 cue"
                  + (cueName == null ? "（**挑不到 ⇒ 后面的 A2–A4 全部不作数**，红在这里）" : "：`" + cueName + "`"));
            if (cueName == null) return;

            // ---------------- A2/A3/A4：一次 Tick 每条槽位最多播一条 ----------------
            var go = new GameObject("B117G1010b_ShakeAndSound");
            temps.Add(go);
            var comp = go.AddComponent<WFExtraShakeAndSound>();
            // 夹具：total = 3 · interval = 0 · time = 0 ⇒ 「一帧跨过好几个区间」这一档
            comp.presets = null;                     // 不建震（本断言只量音效节奏）
            comp.sounds = new[]
            {
                new WFSoundSlot { cue = cueName, time = 0f, interval = 0f, total = 3, is2d = 1 },
            };
            comp.Apply();                            // = 原版 `OnEnable`（计数归零 + 起震）

            int p0 = WFSoundPlayer.Played;
            comp.ExtraTick(0.05f);
            int d1 = WFSoundPlayer.Played - p0;
            Check(d1 == 1,
                  "A2 一次 Tick 只播一条（原版 `PlaySoundOnTime.Update` 末尾 `played += 1; return;`）"
                  + "：实得 " + d1 + " 条（`while` 那版会是 3）");

            comp.ExtraTick(0.05f);
            int d2 = WFSoundPlayer.Played - p0;
            Check(d2 == 2, "A3 两次 Tick 累计 2 条：实得 " + d2 + " 条（`while` 那版会是 6）");

            comp.ExtraTick(0.05f);
            comp.ExtraTick(0.05f);
            int d4 = WFSoundPlayer.Played - p0;
            Check(d4 == 3,
                  "A4 播满 `total`(=3) 之后不再播（上界 `_played[i] < s.total` 还在）：实得 " + d4 + " 条");
        }
        finally
        {
            WFSoundPlayer.Enabled = wasEnabled;
            WFSoundPlayer.ResetCounter();
            for (int i = temps.Count - 1; i >= 0; i--)
                if (temps[i] != null) Object.DestroyImmediate(temps[i]);

            Debug.Log(Tag + " 合计 ✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "）");
        }
    }
}
