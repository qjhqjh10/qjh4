// B117_WCOOL3_Assert.cs — 执行代理 `B117-WCOOL3`（2026-11-02）· **剩余三处**音效冷却收口
//
// ============================ 它盯的是什么 ============================
//  2026-11-02 的 `B117-WSOUNDCOOL` 把 FX cue 的冷却收成一处后，**还有三处各自独立、且都漏了原版那半句**
//  （`last != 0` / 或者干脆一句冷却都没有）。本文件把收口后的三处各钉两条（窗口内不播 / 窗口外能播）
//  ＋ 一条**公用公式的灭自证**：
//    · **A `Battle/RemnantSfx.cs`** —— 键 = 档名（`Aeldari/collect` …）、时钟 `Time.time`。
//      `Editor/BattleScene.cs` 有一条活断言读它的 `Throttled`（本文件**不动**它）。
//    · **B `Shell/WindowSounds.cs`** —— 键 = cue 名。**时钟从 `Time.realtimeSinceStartup` 改成 `Time.time`**
//      （原版 `AudioCue__ConfigAudioSource.c:19` 判的就是 `Time.time`）。`Editor/B97_SHOP_Assert.cs:133` 读它的 `Throttled`。
//    · **C `Core/WarpforgeAudio.cs` 的 `PlayJingle`** —— 改前**一行冷却都没有**；原版壳侧两条 Jingles cue
//      与 FX 那条走**同一个** `SoundManager.Play(AudioCue)` ⇒ 一样要过 `timeToPlayAgain` 闸。
//
// ============================ 判据（第一权威）============================
//  · `d:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21`
//      `if ((Time.time - last < timeToPlayAgain) && (last != 0)) return 0;`
//  · `AudioCue__ConfigAudioSource.c:31` —— **先把 `last = Time.time` 写下去，再挑 clip**。
//  · 唯一的调用点族：`SoundManager__Play`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:257`）
//    ⇒ `Play2D(cue, MixerType.FX)` 与 `Play2D(cue, MixerType.Jingles)` 过的是**同一道闸**。
//  · 公式的**唯一实现** = `WarpforgeVFX.WFSoundPlayer.CanPlayByCooldown`（`internal`，见那个方法的 doc）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `public static void Run()`：**零前提**（不读存档 · 不写 PlayerPrefs · 不出声 —— 全程 `WFSoundPlayer.Enabled = false`，
//  且夹具 cue 的 `clips` 为空 ⇒ 连音源对象都不会建）。挂法（由调度台决定，本文件不改任何 `Editor/*Scene.cs`）：
//      在宿主聚合行**同一物理行**追加：
//      B117_WCOOL3_Assert.Run(); pass += B117_WCOOL3_Assert.Pass; fail += B117_WCOOL3_Assert.Fail;
//  判绿红：按**行首** `B117WCOOL3 ✓` / `B117WCOOL3 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
//  ⚠️ A 段需要 `Resources/Art/audio/sfx/Aeldari Waystone Destruction`（`Resources/Art/` 是 `.gitignore` 的）。
//     取不到时 A1 会红（那是「跑一次 `工具/import_remnant_sfx.py`」的信号，不是本文件的错）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **D1** ← 把公用公式里的 `last != 0` 那半句删掉（= 把表里没有 / 初值 0 当成「刚刚播过」）⇒ D1 红。
//        这条用 `float.MaxValue` 当窗口 ⇒ `now - 0` 永远 `< ttp`，**只要漏了那半句，第一次就必被拦**。
//  · **D5** ← 把 `WFSoundPlayer.CooldownClock` 改回裸 `Time.time`（批处理里恒 0）⇒ D5 红
//        （A2/B2/C2 也会跟着红 —— 它们是同一条根因的三个观测面）。
//  · A2 / B2 / C2 ← 把各自那处的冷却整段删掉（回到改前状态）⇒ 第二次不再被挡、`Throttled` / `JingleThrottled` 不涨 ⇒ 红。
//  · B4 ← 把 `WindowSounds.Play` 改回自己那份式子（不再转调 `CanPlayByCooldown`）⇒ B4 红。
//  · B5 ← 把时钟改回 `Time.realtimeSinceStartup` ⇒ B5 红（源码面判据，跳过注释行）。
//  · C2 ← `PlayJingle` 不调冷却 ⇒ `JingleThrottled` 停在 0 ⇒ C2 红（C1 仍绿，单看 C1 看不出来）。
//
// ============================ 为什么能离线跑（不需要帧循环）============================
//  三处都走 `WFSoundPlayer.CanPlayByCooldown` 的时钟（原版 = `Time.time`），而**批处理里 `Time.time` 恒 `0.000`**
//  ⇒ 不只是「不前进」：原版 `&& (last != 0)` 哨兵会把 0 读成「从没播过」⇒ 冷却一秒都挡不住。
//  ⇒ 🔴 **2026-11-02 `B117-FIXA`**：`Run()` 把时钟钉成显式时刻（`WFSoundPlayer.ClockOverride = 2000`，
//     `finally` 里还原）—— 三处生产路径共用同一个闸，钉一次全部都生效。生产端照旧用真实时钟
//     （`CooldownClock`：`Time.time`，批处理里回退 `Time.realtimeSinceStartup`，判据见那个属性 + 本文件 D5）。
//  「窗口外」那三条仍用**把 `timeToPlayAgain` 临时压到 0**（`finally` 里改回，⛔ 不留给后面的宿主）：
//  闸只比较 `now - last < ttp`，`ttp = 0` 与「真的等够 ttp」是同一个形状（`now - last >= ttp`）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;

/// <summary>执行代理 `B117_WCOOL3`：剩余三处音效冷却（`RemnantSfx` / `WindowSounds` / `PlayJingle`）的断言。</summary>
public static class B117_WCOOL3_Assert
{
    const string Tag = "B117WCOOL3";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    // ============================================================ 入口
    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        bool wasEnabled = WFSoundPlayer.Enabled;
        float wasClock = WFSoundPlayer.ClockOverride;
        try
        {
            WFSoundPlayer.Enabled = false;      // 只判闸、不出声、不建音源池（闸排在 `!Enabled` **之外**）
            // 🔴 **2026-11-02 `B117-FIXA`**：把冷却时钟**钉成显式时刻** —— 批处理里 `Time.time` 恒 `0.000`，
            //   原版 `&& (last != 0)` 哨兵会把 0 读成「从没播过」⇒ 冷却永远不生效（A2/B2/C2/D2 注定红）。
            //   三处生产路径（`RemnantSfx` / `WindowSounds` / `PlayJingle`）全走同一个 `CanPlayByCooldown`
            //   ⇒ 钉一次就够了；生产端照旧用真实时钟（`finally` 里还原）。
            WFSoundPlayer.ClockOverride = 2000f;
            SectionA_RemnantSfx();
            SectionB_WindowSounds();
            SectionC_PlayJingle();
            SectionD_公用公式();
        }
        finally
        {
            WFSoundPlayer.ClockOverride = wasClock;     // ⛔ 别把钉住的时刻留给后面的宿主
            WFSoundPlayer.Enabled = wasEnabled;
            // 把三处的静止态清回起点（⛔ 别把冷却/计数留给后面的宿主）
            RemnantSfx.ResetCounters();
            RemnantSfx.ResetThrottle();
            WindowSounds.ResetCounters();
            WarpforgeAudio.ResetJingleThrottle();
            WFSoundPlayer.ResetThrottle();      // Jingles 与 FX **共用**这张 cue 表（见 `WarpforgeAudio.JingleThrottled` 那段）
            // `RemnantSfx.Play` 会自建一个名为 `RemnantSfx` 的宿主节点（它自己不销毁）⇒ 本文件替它收尾，
            // 免得宿主收尾 `SaveScene()` 把它烘进场景（同 `WFSoundPlayer` 的 `A1534` 那条坑）。
            var host = GameObject.Find("RemnantSfx");
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    // ============================================================ A `Battle/RemnantSfx.cs`
    static void SectionA_RemnantSfx()
    {
        Debug.Log(Tag + " --- A `Battle/RemnantSfx.cs`：键 = 档名 · 时钟 `Time.time` ---");
        RemnantSfx.ResetCounters();
        RemnantSfx.ResetThrottle();

        var cue = RemnantSfx.PropsOf(RemnantSfx.Moment.Death, true);
        float ttp = cue != null ? cue.timeToPlayAgain : -1f;
        Check(cue != null && ttp > 0f,
              $"A0 前提：灵族 `Death` 那档的 `timeToPlayAgain` = {ttp:F3} > 0（区间表 `remnant_cue_props` 读得到）"
            + "｜⛔ 前提不成立时 A1/A2 无从谈起 —— 这一条已经红了，不是静默跳过");
        if (cue == null || ttp <= 0f) return;

        bool first = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        int played1 = RemnantSfx.Played, thr1 = RemnantSfx.Throttled;
        bool second = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        int played2 = RemnantSfx.Played, thr2 = RemnantSfx.Throttled;

        Check(first && played1 == 1 && thr1 == 0,
              $"A1 前提：第一次**真的播了**且没被节流（返回 {first} · Played={played1} · Throttled={thr1}）"
            + "｜取不到 clip 时这里会红 —— 那是「跑一次 `工具/import_remnant_sfx.py`」的信号");

        Check(!second && played2 == 1 && thr2 == thr1 + 1,
              $"A2 ★ 同一档在 `timeToPlayAgain`({ttp:F3}s) 内**再播被挡**（返回 {second} · Played {played1}→{played2} ·"
            + $" Throttled {thr1}→{thr2}）｜判据 = `AudioCue__ConfigAudioSource.c:19-21`"
            + "｜🧨 把这处冷却整段删掉 ⇒ 第二次照播、`Throttled` 不涨 ⇒ 本条红");

        // 窗口外：批处理 `Time.time` 不前进 ⇒ 把 ttp 压到 0（与「真的等够 ttp」同一判据形状）
        float saved = cue.timeToPlayAgain;
        bool outside1, outside2;
        int thr3;
        try
        {
            cue.timeToPlayAgain = 0f;
            outside1 = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
            outside2 = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
            thr3 = RemnantSfx.Throttled;
        }
        finally { cue.timeToPlayAgain = saved; }

        Check(outside1 && outside2 && thr3 == thr2,
              $"A3 ★ 窗口外（`timeToPlayAgain` 压到 0）连播两次**都放行**（返回 {outside1}/{outside2} ·"
            + $" Throttled 停在 {thr3}）｜🧨 把闸写成「只要播过就永远不许」⇒ 本条红");
    }

    // ============================================================ B `Shell/WindowSounds.cs`
    static void SectionB_WindowSounds()
    {
        Debug.Log(Tag + " --- B `Shell/WindowSounds.cs`：键 = cue 名 · 时钟改回 `Time.time` ---");

        WFSoundCue cue;
        bool found = WFSoundBank.TryGetCue(WindowSounds.DefaultButtonCue, out cue);
        float ttp = (found && cue != null) ? cue.timeToPlayAgain : -1f;
        Check(found && cue != null && ttp > 0f,
              $"B0 前提：`{WindowSounds.DefaultButtonCue}` 在音效表里且 `timeToPlayAgain` = {ttp:F3} > 0");
        if (!found || cue == null || ttp <= 0f) return;

        WindowSounds.ResetCounters();
        bool first = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B117_WCOOL3.B");
        int played1 = WindowSounds.Played, thr1 = WindowSounds.Throttled;
        bool second = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B117_WCOOL3.B");
        int played2 = WindowSounds.Played, thr2 = WindowSounds.Throttled;

        Check(first && played1 == 1 && thr1 == 0,
              $"B1 前提：第一次交给了播放器且没被节流（返回 {first} · Played={played1} · Throttled={thr1}）");

        Check(!second && played2 == 1 && thr2 == thr1 + 1,
              $"B2 ★ 同一 cue 在 `timeToPlayAgain`({ttp:F3}s) 内**再播被挡**（返回 {second} · Played {played1}→{played2} ·"
            + $" Throttled {thr1}→{thr2}）｜🧨 把冷却整段删掉 ⇒ 第二次照播 ⇒ 本条红"
            + "（`Editor/B97_SHOP_Assert.cs` 的 B4/B5 读的是同一个 `Throttled`，语义未变）");

        float saved = cue.timeToPlayAgain;
        bool outside1, outside2;
        int thr3;
        try
        {
            cue.timeToPlayAgain = 0f;
            outside1 = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B117_WCOOL3.B");
            outside2 = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B117_WCOOL3.B");
            thr3 = WindowSounds.Throttled;
        }
        finally { cue.timeToPlayAgain = saved; }

        Check(outside1 && outside2 && thr3 == thr2,
              $"B3 ★ 窗口外（`timeToPlayAgain` 压到 0）连播两次**都放行**（返回 {outside1}/{outside2} · Throttled 停在 {thr3}）");

        // 源码面判据：**时钟口径**与**唯一实现**（这两条都没有运行时可观测点 —— 批处理里两种时钟都不前进）
        string src = Application.dataPath + "/CardPresentation/Shell/WindowSounds.cs";
        bool hasCall = false, hasRealtime = false;
        if (!System.IO.File.Exists(src))
            Check(false, "B4 前提：读得到 `Shell/WindowSounds.cs`（" + src + "）—— 读不到时 B4/B5 等于没验 ⇒ 当场红");
        else
        {
            foreach (var raw in System.IO.File.ReadAllLines(src))
            {
                var t = raw.TrimStart();
                if (t.StartsWith("//")) continue;        // ⚠️ 注释里有旧写法 / 本次改动的说明，不算实现
                if (t.IndexOf("CanPlayByCooldown", StringComparison.Ordinal) >= 0) hasCall = true;
                if (t.IndexOf("realtimeSinceStartup", StringComparison.Ordinal) >= 0) hasRealtime = true;
            }
            Check(hasCall,
                  "B4 ★ `Play` 的冷却走的是**全项目唯一实现** `WFSoundPlayer.CanPlayByCooldown`"
                + "｜🧨 改回自己那份式子 ⇒ 本条红（铁律 6：公式只留一处）");
            Check(!hasRealtime,
                  "B5 ★ 代码里**不再有** `Time.realtimeSinceStartup`（时钟 = `Time.time`，原版 `AudioCue__ConfigAudioSource.c:19`）"
                + "｜🧨 改回旧时钟 ⇒ 本条红");
        }
    }

    // ============================================================ C `Core/WarpforgeAudio.cs` 的 `PlayJingle`
    static void SectionC_PlayJingle()
    {
        Debug.Log(Tag + " --- C `WarpforgeAudio.PlayJingle`：Jingles 组改前**一行冷却都没有** ---");

        // 夹具 cue：不在表里（`clips` 为空）⇒ `PickClip` 直接返 null ⇒ **不建音源、不出声**；
        // 但「先写戳、再挑 clip」的次序让闸照样可观测（`JingleThrottled`）—— 判据 `ConfigAudioSource.c:31`。
        var cue = new WFSoundCue
        {
            name = "B117WCOOL3_jingle_fixture",
            clips = new string[0],
            timeToPlayAgain = 1000f,
        };
        Check(WFSoundBank.PickClip(cue, new System.Random(1)) == null,
              "C0 前提：夹具 cue 挑不出 clip（`clips` 为空）⇒ 下面走「写戳后挑空」那条路，不建对象、不出声");

        WarpforgeAudio.ResetJingleThrottle();
        var a = WarpforgeAudio.PlayJingle(cue);
        int t1 = WarpforgeAudio.JingleThrottled;
        var b = WarpforgeAudio.PlayJingle(cue);
        int t2 = WarpforgeAudio.JingleThrottled;

        Check(a == null && t1 == 0,
              $"C1 前提：第一次**过闸**（挑不出 clip ⇒ 返回 null，但 `JingleThrottled` 仍是 {t1}）");

        Check(b == null && t2 == t1 + 1,
              $"C2 ★ 窗口内第二次**被冷却挡掉**（`JingleThrottled` {t1}→{t2}）"
            + "｜判据 = 原版 `SoundManager.Play2D(cue, MixerType.Jingles)` 与 FX 过的是**同一个** `ConfigAudioSource`"
            + "｜🧨 改前这里一行冷却都没有 ⇒ 本条红（C1 仍绿，单看 C1 看不出来）");

        float saved = cue.timeToPlayAgain;
        AudioClip x, y;
        int t3;
        try
        {
            cue.timeToPlayAgain = 0f;
            x = WarpforgeAudio.PlayJingle(cue);
            y = WarpforgeAudio.PlayJingle(cue);
            t3 = WarpforgeAudio.JingleThrottled;
        }
        finally { cue.timeToPlayAgain = saved; }

        Check(x == null && y == null && t3 == t2,
              $"C3 ★ 窗口外（`timeToPlayAgain` 压到 0）连播两次**都不被挡**（Throttled 停在 {t3}）");
    }

    // ============================================================ D 公用公式（灭自证 + 窗口两态）
    static void SectionD_公用公式()
    {
        Debug.Log(Tag + " --- D `WFSoundPlayer.CanPlayByCooldown<TKey>`：灭自证 + 窗口两态 ---");

        // `internal` + 本仓没有 `InternalsVisibleTo` ⇒ 编辑器程序集只能反射调（同族先例：
        // `Editor/B108_A6_ReuseShop_Assert.cs:221` / `Editor/B97W34_Assert.cs:26`）。
        var mi = FindGenericCooldown();
        Check(mi != null,
              "D0 前提：反射找得到 `WFSoundPlayer.CanPlayByCooldown<TKey>(Dictionary<TKey,float>, TKey, float)`"
            + "（`internal` 静态；找不到 ⇒ D1/D2/D3 无从谈起，本条已红）");
        if (mi == null) return;
        var g = mi.MakeGenericMethod(typeof(string));

        // ---- D1 灭自证：`last == 0` = 从没播过 ⇒ 必须放行 ----
        //   窗口给 `float.MaxValue` ⇒ 只要实现里漏了原版那句 `&& (last != 0)`（把初值 0 当成「刚刚播过」），
        //   `now - 0 < ttp` 恒真 ⇒ 第一次就必被拦 ⇒ 本条红。**三处共用这一个公式** ⇒ 一条覆盖三处。
        var t0 = new Dictionary<string, float> { { "never", 0f } };
        bool firstAtZero = (bool)g.Invoke(null, new object[] { t0, "never", float.MaxValue });
        Check(firstAtZero,
              "D1 ★★ 灭自证：`last == 0`（原版语义 = 从没播过，`AudioCue__OnEnable.c` 把它清成 0）**必须放行**"
            + "｜🧨 把 `last != 0` 那半句删掉 / 用「表里没有 ⇒ 拿 0 当上次时刻」⇒ 本条红（A/B/C 三处全中）");

        // ---- D2 窗口内：第二次被挡 ----
        var t1 = new Dictionary<string, float>();
        bool ok1 = (bool)g.Invoke(null, new object[] { t1, "k", 1000f });
        bool ok2 = (bool)g.Invoke(null, new object[] { t1, "k", 1000f });
        Check(ok1 && !ok2,
              $"D2 ★ 同一 key：第一次放行（{ok1}）、窗口内第二次被挡（{ok2}）");

        // ---- D3 窗口外：放行 ----
        var t2 = new Dictionary<string, float> { { "k", Time.time - 1e6f } };   // 一百万秒前播过
        bool ok3 = (bool)g.Invoke(null, new object[] { t2, "k", 1f });
        Check(ok3,
              "D3 ★ 窗口外（`now - last = 1e6 s > 1 s`）**放行**"
            + "｜🧨 把闸写成「只要播过就永远不许」⇒ 本条红");

        // ---- D4 `timeToPlayAgain == 0` ⇒ 永远放行（数据面 158/513 条靠这一档）----
        var t3 = new Dictionary<string, float>();
        bool ok4 = (bool)g.Invoke(null, new object[] { t3, "k", 0f });
        bool ok5 = (bool)g.Invoke(null, new object[] { t3, "k", 0f });
        Check(ok4 && ok5,
              $"D4 ★ `timeToPlayAgain == 0` ⇒ 连播两次都不拦（{ok4}/{ok5}）"
            + "｜🧨 用 `<=` 或把 0 当「必须冷却」⇒ 本条红");

        // ---- D5 生产端的冷却时钟必须**非 0** ----
        //   批处理里 `Time.time` = 0.000（`_tmp_view/{shop,battle}.log` 实测）⇒ 若 `CanPlayByCooldown` 直接用裸
        //   `Time.time`，`last` 会被写成 0、原版那句 `&& (last != 0)` 就把每一次播放都当「从没播过」
        //   ⇒ 冷却整个不生效（`Throttled` 永远 0）。这条直接量**生产端**的时钟（不受本文件钉住的 `ClockOverride` 影响）。
        float savedClock = WFSoundPlayer.ClockOverride;
        float prodClock;
        try
        {
            WFSoundPlayer.ClockOverride = -1f;                  // = 生产端口径（真实时钟）
            prodClock = WFSoundPlayer.CooldownClock;
        }
        finally { WFSoundPlayer.ClockOverride = savedClock; }
        Check(prodClock > 0f,
              $"D5 ★ 生产端的冷却时钟 = {prodClock:F3} > 0 —— 批处理里 `Time.time` 恒 0 ⇒ `CooldownClock` 回退到"
            + " `Time.realtimeSinceStartup`；没有这条回退，原版 `&& (last != 0)` 哨兵会让 A2/B2/C2 与"
            + " `Editor/BattleScene.cs:17013` 的 `RemnantSfx.Throttled` 断言**全部**红"
            + "｜🧨 把 `CooldownClock` 改回裸 `Time.time` ⇒ 本条红");
    }

    static MethodInfo FindGenericCooldown()
    {
        var ms = typeof(WFSoundPlayer).GetMethods(BindingFlags.Static | BindingFlags.NonPublic
                                                 | BindingFlags.Public);
        for (int i = 0; i < ms.Length; i++)
        {
            var m = ms[i];
            if (m.Name != "CanPlayByCooldown" || !m.IsGenericMethodDefinition) continue;
            if (m.GetParameters().Length == 3) return m;
        }
        return null;
    }
}
