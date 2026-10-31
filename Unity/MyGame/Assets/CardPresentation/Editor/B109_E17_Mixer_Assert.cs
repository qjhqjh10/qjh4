// B109_E17_Mixer_Assert.cs — **三份 mixer / 12 组 / 8 快照**那条链的批处理自检
//
// ============================ 为什么单开一份 ============================
//  判据 = `资料/完全复刻/判据/B109_R11_音频mixer.md`：
//   · 原版是 **3 份** AudioMixer —— `Main Mixer`（Master/FX/Music/Voices/Jingles · 1 条快照 `Snapshot`）
//     ＋ `Music`（Master/General/BattleGroupLow/Medium/High · 5 条快照，输出挂主 mixer 的 `Music` 组）
//     ＋ `FX`（Master/General · 2 条快照，挂主 mixer 的 `FX` 组）；
//   · 我们原来只有 **1 份 / 5 组 / 6 条自造快照**，且快照落在**主 mixer 的 `VolumeMusic` 那根滑块参数**上
//     ⇒「切快照」与「玩家滑块」是同一根浮点，互相覆盖（`B89-E13b` 记的那两条代价的根因）。
//  `B109_E17` 把三份 mixer 按原版建回来、并把 8 条快照的值逐格写进 YAML。本文件只**验**，不建、不改。
//
// ⚠️ 本文件**不重复实现**任何检查 —— 三条报告各自只有一份实现：
//   · `AudioSetup.MixerStructureReport()`        ← R11 §4 的 1/2/3/4/5/6/7（+ `m_SuspendThreshold`）
//   · `AudioSetup.VerifySnapshotValuesReport()`  ← 27 格的快照组音量（直接读 `.mixer` 文本）
//   · `AudioSetup.VerifyMixerDefaultsReport()`   ← 主 mixer 那 1 条快照里 `Master = −0.16685188 dB`
//   本文件再把**运行时侧**那半（`WarpforgeAudio` 的 8 条参数 / 6 档快照取不取得到 / 缺件计数）串起来。
//
// ============================ 用法 ============================
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod B109_E17_Mixer_Assert.Run -logFile -
//   ⚠️ 判绿红看「AUD-MIX 合计」，不看退出码（同仓其它宿主的约定）。
//   ⛔ 别用 `AudioSetup.PlayCheck` 代替：那条要进 Play 模式，batchmode 下会直接拒绝退出（实测过）。

using System;
using UnityEngine;
using CardPresentation;

public static class B109_E17_Mixer_Assert
{
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("AUD-MIX ✓ " + what); }
        else { _fail++; Debug.LogError("AUD-MIX ✗ " + what); }
    }

    /// <summary>批处理入口。⚠️ 默认会 `Application.Quit`；从宿主里跑必须传 <c>false</c>（否则会把宿主提前结束）。</summary>
    public static void Run(bool quitOnBatch = true)
    {
        _pass = _fail = 0;

        // ---- ① 结构七条（原版三份 mixer / 12 组 / 8 快照 + 跨资产挂接 + 灭自证 + 不互抢）----
        string rep = AudioSetup.MixerStructureReport();
        Debug.Log(rep);
        Check(AudioSetup.MixerStructureFailures == 0,
              $"三份 mixer 结构全过（`B109_R11` §4 七条；失败 {AudioSetup.MixerStructureFailures}/{AudioSetup.MixerStructureChecks} 条）");

        // ---- ② 快照组音量 27 格（直接读 `.mixer` 文本，与上面的「结构」不是同一把尺子）----
        string sv = AudioSetup.VerifySnapshotValuesReport();
        Debug.Log(sv);
        Check(AudioSetup.SnapshotValueFailures == 0,
              $"子 mixer 快照组音量与原版逐格一致（不一致 {AudioSetup.SnapshotValueFailures} 格；"
            + "非 0 ⇒ 切 SFX / 切战斗音乐**没有任何听觉变化**）");

        // ---- ③ 主 mixer 起始快照的 `Master` = −0.16685188 dB（原版值，单位是 dB）----
        string md = AudioSetup.VerifyMixerDefaultsReport();
        Debug.Log(md);
        Check(AudioSetup.MixerDefaultFailures == 0,
              $"主 mixer 起始快照的 `Master` = 原版 −0.16685188 dB（不一致 "
            + $"{AudioSetup.MixerDefaultFailures}/{AudioSetup.MixerDefaultChecks} 格）");

        // ---- ④ 运行时侧：8 条暴露参数（主 5 + Music 3）----
        Check(WarpforgeAudio.AllParamsOk,
              "`WarpforgeAudio.AllParamsOk` —— 8 条暴露参数全认得（主 `VolumeFX/Jingles/Master/Music/Voices` · "
            + "Music `TrackLow/Medium/HighVolume`）。名字对不上时 `SetFloat` **静默无效**");

        // ---- ⑤ 运行时侧：6 档 `SnapshotsType` 各取得到、且名字就是原版那 6 个 ----
        for (int i = 0; i < WarpforgeAudio.SnapshotNames.Length; i++)
        {
            string name = WarpforgeAudio.SnapshotNames[i];
            var s = WarpforgeAudio.GetSnapshot((WarpforgeAudio.SnapshotsType)i);
            Check(s != null && s.name == name,
                  $"`SnapshotsType.{(WarpforgeAudio.SnapshotsType)i}` ⇒ 快照 `{name}` 取得到"
                + $"（{(s == null ? "**取到 null**" : "实得 `" + s.name + "`")}；"
                + $"应在 {(i <= 1 ? "`Resources/Audio/FX.mixer`" : "`Resources/Audio/Music.mixer`")}）");
        }
        Check(WarpforgeAudio.SnapshotMisses == 0,
              $"`WarpforgeAudio.SnapshotMisses == 0`（实得 {WarpforgeAudio.SnapshotMisses}）—— 非 0 = 有档位取不到快照");

        // ---- ⑥ 运行时侧：战斗 BGM 的三条轨真能找到 `BattleGroup*` 三个组（挂 null = 快照切不动它）----
        for (int i = 0; i < 3; i++)
        {
            var g = WarpforgeAudio.BattleGroupGroup(i);
            string[] want = { "BattleGroupLow", "BattleGroupMedium", "BattleGroupHigh" };
            Check(g != null && g.name == want[i],
                  $"`WarpforgeAudio.BattleGroupGroup({i})` = `{want[i]}`（{(g == null ? "**null**" : "实得 `" + g.name + "`")}）");
        }

        // ---- ⑦ 两个子 mixer 与主 mixer 的挂接（运行时读回来再核一次，与 ①-2 的文本/对象比对互为补充）----
        Check(WarpforgeAudio.MusicMixer != null && WarpforgeAudio.FxMixer != null,
              "`WarpforgeAudio.MusicMixer` / `FxMixer` 都加载得到（`Resources/Audio/Music` · `Resources/Audio/FX`）");
        Check(WarpforgeAudio.MusicGroup != null,
              "`WarpforgeAudio.MusicGroup`（菜单音乐）非空 —— 现在应指向 **Music 子 mixer 的 `General`**");
        Check(WarpforgeVFX.WFSoundPlayer.FxGroup != null,
              "`WFSoundPlayer.FxGroup`（运行时特效音）非空 —— 现在应指向 **FX 子 mixer 的 `General`**");

        Debug.Log($"AUD-MIX === 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (quitOnBatch && Application.isBatchMode) Application.Quit(_fail == 0 ? 0 : 1);
    }

    // 🆕 2026-10-31（B109 · 调度台接线）：宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
