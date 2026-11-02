// B118W10_Assert.cs — 执行代理 `B118-W10`（2026-11-02）· `B117-T04`「战斗开场 FX duck 两拍」
//
// ============================ 它盯的是什么 ============================
//  原版 `BattleMusicManager.Start()` **协程**（`out_cpp2il_dev_ilspy/BattleMusicManager.cs:238`）：
//    state 0：`noBattleMusic.TransitionTo(0)` → **`SoundManager.ToggleSFX(false, instant:true)`** → `yield WaitForEndOfFrame`
//    state 1：**`SoundManager.ToggleSFX(true)`**（缺省 instant=false ⇒ **2 s 淡回**）+ `battleLow.TransitionTo(3.0f)`
//    实据：`decomp_full/BattleMusicManager._Start_d__13__MoveNext.c:69 / :85 / :88 / :91-92 / :34 / :37-38`
//          ＋ il2csharp `BattleMusicManager.cs:243-246, 264-275`（实参 `(false,true)` / `(true,false)` 逐字）
//   ⇒ **战斗头 2 s 整条 FX 总线被压在 `NoVolume`(−80 dB)，再 2 s 淡回 `General`(0 dB)**（`FX.mixer` 逐值）。
//  `ToggleSFX` 本体 = `TransitionTo((instant?0:1)*2f)`（`SoundManager.cs:597-602`）。
//
//  我们的落点：`Battle/BattleMusic.cs:154`（第 0 拍）· `:160`（第 1 拍，**必须在 `_row == null` 早退之前**）·
//              `:265`（`OnDestroy` 安全网）。读口 = `BattleMusic.OpenBeatPendingForTest`。
//
// ============================ 灭自证 ============================
//  🧨1（C 组）：**`_row == null`（BGM 表缺失）时也必须解 duck** —— 把解 duck 挪到早退之后 ⇒ C 红。
//      （原版那条链只看 `noBattleMusic != null`，**与曲子表无关**。）
//  🧨2（E 组）：删掉任一调用点、只留标志位 ⇒ E 红（源码文本里 `ToggleSFX(true)` 的出现次数 < 2）。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（自建 `BattleMusic` 根、收尾自己 `DestroyImmediate`；不读存档 / 不写 PlayerPrefs）。
//  挂法（由调度台决定）：`Editor/BattleScene.cs:46983` 那条聚合行、**合计 `Debug.Log` 之前**，**同一物理行追加**
//      B118W10_Assert.Run(); pass += B118W10_Assert.Pass; fail += B118W10_Assert.Fail;
//  判绿红：`grep "^B118W10"` 的合计行；逐条按**行首** `B118W10 ✓` / `B118W10 ✗` 数（⛔ 别 `grep -c ✗`）。
//
//  ⚠️ 批处理下不出声（`AudioMixerSnapshot.TransitionTo` 在编辑器非 Play 态是空操作）⇒ 本文件**只查状态机与接线**，
//     ⛔ 不写「听不到就失败」那一类断言。

using System;
using System.IO;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B118_W10`：开场两拍（FX 瞬时压 −80 dB → 下一拍 2 s 淡回）+ `_row == null` 也要解 + `OnDestroy` 兜底。</summary>
public static class B118W10_Assert
{
    const string SrcRel = "CardPresentation/Battle/BattleMusic.cs";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("B118W10 ✓ " + what); }
        else { _fail++; Debug.LogError("B118W10 ✗ " + what); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;

        SectionA_FirstBeat();
        SectionB_SecondBeat();
        SectionC_NullRowStillReleases();
        SectionD_OnDestroySafetyNet();
        SectionE_SourceWiring();

        // 收尾：⛔ 不把编辑器里的 FX 总线留在 `NoVolume`（本件在 Edit 模式下跑，快照切换是空操作，但仍归位一次）
        WarpforgeAudio.ToggleSFX(true);
    }

    // ------------------------------------------------------------------ A · 第 0 拍
    static void SectionA_FirstBeat()
    {
        var go = new GameObject("B118W10_Probe_A");
        go.hideFlags = HideFlags.DontSave;
        var bm = go.AddComponent<BattleMusic>();
        bm.Init("battlearena1");                       // 走**正常开局路径**（表载入 ⇒ `_row != null`）
        Check(bm.Config != null, "A0 **前提**：`battlearena1` 在 BGM 表里（表缺失时本节只验得到静音那一半）");

        bm.BeginBattle();

        Check(bm.OpenBeatPendingForTest && bm.CurrentLevel == BattleMusic.LevelSilent,
              "A1 ★ `BeginBattle()` 之后 = **第 0 拍**：第 1 拍还欠着（`OpenBeatPendingForTest`）+ BGM 仍在**静音**"
            + $"（实得 pending={bm.OpenBeatPendingForTest} · level={bm.CurrentLevel}）"
            + "｜🧨 把两拍合成一拍（`BeginBattle` 里直接 `StartFade(Low,3)`）⇒ 本条红"
            + "（原版 `_Start_d__13` state 0 结尾 `yield WaitForEndOfFrame`，state 1 才淡入）");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ B · 第 1 拍
    static void SectionB_SecondBeat()
    {
        var go = new GameObject("B118W10_Probe_B");
        go.hideFlags = HideFlags.DontSave;
        var bm = go.AddComponent<BattleMusic>();
        bm.Init("battlearena1");
        bm.BeginBattle();

        bm.Tick(0f, 25f);                              // 下一帧，dt=0 ⇒ 淡入**起了**但一步没走

        Check(!bm.OpenBeatPendingForTest
              && bm.TargetVolume(BattleMusic.LevelLow) > 0.99f
              && bm.CurrentLevel == BattleMusic.LevelSilent,
              "B1 ★ 第一次 `Tick` = **第 1 拍**：FX 解 duck + Low 的 3 s 淡入**已经开始**（`dt=0` 所以还没落地）"
            + $"（实得 pending={bm.OpenBeatPendingForTest} · Low={bm.TargetVolume(BattleMusic.LevelLow):F2}"
            + $" · level={bm.CurrentLevel}）"
            + "｜🧨 第 1 拍里忘了 `StartFade(Low,3)`（只解 duck 不淡入）⇒ 本条红");

        bm.Tick(0f, 25f);
        Check(!bm.OpenBeatPendingForTest,
              "B2 ★ **不重复触发**：第 1 拍只发生一次（再 `Tick` 不会重起）"
            + "｜🧨 忘记清 `_openBeatPending` ⇒ 每帧重起 3 s 淡入（并且 A1/B1 的因果全乱）");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ C · 灭自证①：表缺失也要解
    static void SectionC_NullRowStillReleases()
    {
        var go = new GameObject("B118W10_Probe_C");
        go.hideFlags = HideFlags.DontSave;
        var bm = go.AddComponent<BattleMusic>();        // ⛔ **不调 `Init`** ⇒ `_row == null`（BGM 表缺失/场名不认识）

        Check(bm.Config == null, "C0 **前提**：没 `Init` ⇒ `_row == null`（这一组才是有意义的那一档）");

        bm.BeginBattle();
        bm.Tick(0f, 25f);

        Check(!bm.OpenBeatPendingForTest,
              "C1 ★ 灭自证：**`_row == null` 时第 1 拍照样要解 duck** —— "
            + "原版那条链只看 `noBattleMusic != null`，与曲子表无关；把解 duck 写在 `if (_row == null) return;` 之后 ⇒ "
            + "**整条 FX 总线永久压在 −80 dB**（战斗全程没音效，且零报错）"
            + $"（实得 pending={bm.OpenBeatPendingForTest}）");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ D · 安全网
    static void SectionD_OnDestroySafetyNet()
    {
        var go = new GameObject("B118W10_Probe_D");
        go.hideFlags = HideFlags.DontSave;
        var bm = go.AddComponent<BattleMusic>();
        bm.BeginBattle();

        Check(bm.OpenBeatPendingForTest, "D0 **前提**：`BeginBattle()` 之后第 1 拍确实还欠着（否则 D1 是空转）");

        bm.OnDestroy();                                // 第 1 拍还没解就被销毁（`BattleDriver` 每局重建 `BattleMusic`）

        Check(!bm.OpenBeatPendingForTest,
              "D1 ★ 安全网：`OnDestroy()` 兜底把第 1 拍解掉 ⇒ ⛔ **绝不把整条 FX 总线留在 −80 dB**"
            + "｜🧨 删掉 `OnDestroy` 里那行 ⇒ 本条红（那会让下一局/菜单的音效全部静音，且零报错）");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ E · 灭自证②：源码结构
    static void SectionE_SourceWiring()
    {
        string path = Path.Combine(Application.dataPath, SrcRel);
        Check(File.Exists(path), "E0 **前提**：读得到源码 `" + SrcRel + "`（读不到时本节等于没验 ⇒ 当场红）");
        if (!File.Exists(path)) return;

        // 去注释：本文件里**注释**也提到过 `ToggleSFX(true)`（类尾 doc）与 `ToggleSFX(false, instant:true)`（带空格），
        // ⛔ 不去注释会假绿。按行把第一个 `//` 之后一律丢掉（本文件没有含 `//` 的字符串字面量）。
        var raw = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
        var sb = new System.Text.StringBuilder();
        foreach (var line in raw)
        {
            int k = line.IndexOf("//", StringComparison.Ordinal);
            sb.Append(k >= 0 ? line.Substring(0, k) : line).Append('\n');
        }
        string code = sb.ToString();

        Check(code.IndexOf("ToggleSFX(false, true)", StringComparison.Ordinal) >= 0,
              "E1 ★ 第 0 拍**真的接线**：非注释代码里有 `ToggleSFX(false, true)`（瞬时压 −80 dB）"
            + "｜🧨 只留 `_openBeatPending` 标志、删掉这行 ⇒ 本条红");

        int n = 0, at = 0;
        while ((at = code.IndexOf("ToggleSFX(true)", at, StringComparison.Ordinal)) >= 0) { n++; at += "ToggleSFX(true)".Length; }
        Check(n >= 2,
              $"E2 ★ 第 1 拍**和**安全网都在：非注释代码里 `ToggleSFX(true)` 出现 **≥2** 次（`Tick` 第 1 拍 + `OnDestroy` 兜底）"
            + $"（实得 {n}）｜🧨 删掉任一处 ⇒ 本条红（只剩 1 次）");
    }
}
