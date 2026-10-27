// AudioSnapshotCheck.cs — 音频快照那条链的**批处理自检**（账 `音频/语音:12` / `:80`）。
//
// ============================ 为什么单开一份 ============================
//  · 账 `音频/语音:12`（`AudioMixerAddresableReferences.GetSnapshot`）与 `:80`（`SoundManager.ToggleSFX`）
//    这两条的唯一缺口是**资产**：`Resources/Audio/Main Mixer.mixer` 里要有
//    `WarpforgeAudio.SnapshotNames` 那 6 条快照（名字**只此一份**，⛔ 别在本文件再抄一遍）。
//  · 建资产要 Unity 内部 API（`-executeMethod AudioSetup.CreateSnapshots`，2026-10-27 由 `B2AU` 加）。
//    本文件**只写断言**：快照齐了没有、`GetSnapshot` 取不取得到、`SnapshotMisses` 是不是 0。
//  · 现有宿主里**没有一个**碰过 `WarpforgeAudio`（`grep -rn WarpforgeAudio Editor/` 只命中
//    `AudioSetup` 自己那两条 Play 模式探针，而 `AudioSetup.PlayCheck` **在 batchmode 下跑不起来**）
//    ⇒ 批处理里「快照缺了」此前**没有任何断言盯着**，只有一条运行时警告。
//
// ============================ 判据 ============================
//  · `WarpforgeAudio.GetSnapshot(SnapshotsType)`（`Core/WarpforgeAudio.cs:293`）= 原版
//    `AudioMixerAddresableReferences.GetSnapshot` 的等价物：取不到 ⇒ `null` + **出声一次** + `SnapshotMisses++`。
//  · `SoundManager.ToggleSFX` 那条（`WarpforgeAudio.ToggleSFX`）逐字对齐：`FxNormal` / `FxNoVolume` 两档 + `TransitionTo`。
//    ⇒ 只要 6 条快照齐，两条账的缺口就**同时**消失（这就是为什么本自检一次驱动全部 6 档）。
//
// ============================ 用法 ============================
//   bash d:/4/Unity/工具/_run_8_checks.sh 里加一条（串行）：
//     unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//       -executeMethod AudioSnapshotCheck.Run -logFile -
//   ⚠️ 判绿红看「AUD-SNAP 合计」，不看退出码（同仓其它宿主的约定）。
//   ⛔ 不要用 `AudioSetup.PlayCheck` 代替：那条要进 Play 模式，batchmode 下会直接拒绝退出（实测过）。

using System;
using UnityEngine;
using CardPresentation;

public static class AudioSnapshotCheck
{
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("AUD-SNAP ✓ " + what); }
        else { _fail++; Debug.LogError("AUD-SNAP ✗ " + what); }
    }

    /// <summary>批处理入口。</summary>
    public static void Run()
    {
        _pass = _fail = 0;
        int n = WarpforgeAudio.SnapshotNames.Length;
        Check(n == Enum.GetValues(typeof(WarpforgeAudio.SnapshotsType)).Length,
              $"（前提）`SnapshotNames` 的条数与 `SnapshotsType` 的枚举值个数一致（{n} / "
            + $"{Enum.GetValues(typeof(WarpforgeAudio.SnapshotsType)).Length}）"
            + " —— 少一条就会有一条档位静默落到 `null`");

        // 🆕 **2026-10-28（B88_B2）**：把「四根滑块到底连没连到 mixer」也钉住 ——
        //   这是**另一条**失败模式：`WarpforgeAudio.Write` 调的是 `SetFloat("VolumeMusic", …)` 这类**名字**，
        //   名字写错是**静默**的（`SetFloat` 对不存在的参数不抛、滑块白滑），而上面那 6 条快照断言**看不见它**。
        //   判据 = `WarpforgeAudio.ParamsOk`（`GetFloat` 四个名字全成功才算 true；名字对不上 `GetFloat` 返 false）。
        Check(WarpforgeAudio.ParamsOk,
              "（前提）四根音量滑块真的连到 mixer 了：`VolumeMusic`/`VolumeVoices`/`VolumeFX`/`VolumeJingles`"
            + " 四个参数名全认（`WarpforgeAudio.ParamsOk`）—— 名字对不上时 `SetFloat` 静默无效，滑块白滑");

        for (int i = 0; i < n; i++)
        {
            string name = WarpforgeAudio.SnapshotNames[i];
            var s = WarpforgeAudio.GetSnapshot((WarpforgeAudio.SnapshotsType)i);
            // 「名字 = SnapshotNames[i]」这一条**只在取到时才有意义**；取不到时 GetSnapshot 已出声 + 记 miss
            Check(s != null && s.name == name,
                  $"`SnapshotsType.{(WarpforgeAudio.SnapshotsType)i}` ⇒ 快照 `{name}` 取得到"
                + $"（{(s == null ? "**取到 null** —— mixer 里没有这条，跑 `-executeMethod AudioSetup.CreateSnapshots`" : "实得 `" + s.name + "`")}）");
        }

        // 🔴 这条是「缺件出声」那一层的**闭环**：`GetSnapshot` 取不到时会把 `SnapshotMisses` 加一，
        //    所以它 == 0 ⇔ 上面 6 条**一条都没落空**。⛔ 别改成「扫一遍名字就算过」——
        //    那会绕开 `GetSnapshot` 本身（而 `ToggleSFX` 走的就是它）。
        Check(WarpforgeAudio.SnapshotMisses == 0,
              $"`WarpforgeAudio.SnapshotMisses == 0`（实得 {WarpforgeAudio.SnapshotMisses}）"
            + " —— 非 0 = 有档位取不到快照 ⇒ 切 SFX / 切战斗音乐**没有任何听觉变化**（账 `音频/语音:12`、`:80`）");

        Debug.Log($"AUD-SNAP === 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) Application.Quit(_fail == 0 ? 0 : 1);
    }
}
