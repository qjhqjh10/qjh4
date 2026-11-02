// B117_WSOUNDCOOL_Assert.cs — 执行代理 `B117-WSOUNDCOOL`（2026-11-02）· `AudioCue.timeToPlayAgain` 冷却收口
//
// ============================ 它盯的是什么 ============================
//  把「cue 冷却」从**两处副本**收成**一处实现**之后，`WarpforgeVFX.WFSoundPlayer.Play(WFSoundCue, …)`
//  必须与原版逐位同形：
//    · `D:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21`
//        `if ((Time.time - last < timeToPlayAgain) && (last != 0)) return 0;`
//    · `AudioCue__ConfigAudioSource.c:31` —— **先把 `last = Time.time` 写下去，再挑 clip**
//      （挑不出 clip 也算「这次播过」）。
//    · 键 = **cue 资产实例**（`lastTimePlayed` 住在 `AudioCue` 上，`+0x34`；`AudioCue__OnEnable.c` 写回 0）。
//    · 唯一的调用点族：`SoundManager__Play`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:257`
//      `if (!((AudioCue)position).ConfigAudioSource(ref audioSource2))`）—— 原版**所有按 cue 播**的路径都过它
//      ⇒ 一处实现覆盖全部。
//  ⛔ 本文件**不重复**测「战斗 cue 表 27 条 / 12 条静音 cue / BGM 常数」—— 那是 `B117_G10_12_Assert` 的活。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景对象 · 不读存档 · 不写 PlayerPrefs · 不出声）。
//  挂法（由调度台决定，本文件不改任何 `Editor/*Scene.cs`）：在宿主聚合行**同一物理行**追加
//      B117_WSOUNDCOOL_Assert.Run(); _sink.Pass += B117_WSOUNDCOOL_Assert.Pass; _sink.Fail += B117_WSOUNDCOOL_Assert.Fail;
//  判绿红：`grep "^B117WSOUNDCOOL"` 的**合计行**；逐条按**行首** `B117WSOUNDCOOL ✓` / `B117WSOUNDCOOL ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1 ← 把 `CanPlayByCooldown` 的闸整个删掉（回到「WFSoundPlayer 没有冷却」的改前状态）⇒ A1 红。
//  · A2 ← 在最前面写一句「第一次也拦」（例如把 `last` 初值当 0、判 `now - 0 < ttp`）⇒ A2 红。
//         ⚠️ 这条**不靠 `Time.time` 的绝对值**：夹具给 `timeToPlayAgain = 1e9`，只要有「把未初始化
//         当成 last = 0」的那种写法，第一次就必被拦。
//  · A3 ← 把闸写成「**窗口过期时也不许播**」（忘了 `last` 会被重写）⇒ A3 红。
//  · A4 ← 把闸写成「`timeToPlayAgain == 0` 也拦秒级连播」（例如用 `<=` 或把 0 当「必须冷却」）⇒ A4 红。
//  · A5 ← 把「先写戳、再挑 clip」的顺序换过来（挑不出 clip 就提前 return，不写戳）⇒ 第二次不再被拦 ⇒ A5 红。
//         🧨 这一条与 A1 是**同一观测量上的反相**：A1 要「挡」，A5 要「没挑到 clip 也照样挡」。
//  · A6 ← 把冷却表写成**全局一把闸**（不按 cue 分）⇒ 两条不同的 cue 互相拦 ⇒ A6 红。
//
// ============================ 为什么能离线跑（不需要帧循环）============================
//  `WFSoundPlayer` 的闸读时钟（原版就是 `Time.time`）。批处理里 **`Time.time` 恒 `0.000`**（没有帧循环）——
//  ⚠️ 这不只是「不前进」：原版那个 `&& (last != 0)` 哨兵（0 = 从没播过）会把**每一次**播放都读成「从没播过」
//  ⇒ 冷却**一秒都挡不住**。⇒ 本文件把时钟**钉成显式时刻**（`WFSoundPlayer.ClockOverride = 1000`，
//  `finally` 里还原），A3 再把它**推过窗口**（`+ ttp + 0.01`）—— 这样「窗口内挡 / 窗口外放行」两态
//  都是确定性的，且**不再**靠「把 `timeToPlayAgain` 压到 0」（那会把「窗口宽度」这条判据一起测掉）。
//  ⚠️ 生产端（`WFSoundPlayer.CooldownClock`）仍用真实时钟；`Time.time == 0` 时回退 `Time.realtimeSinceStartup`
//     —— 那条回退是给**改不到断言的宿主**（如 `Editor/BattleScene.cs:17013`）兜底的，判据见那个属性。
//  ⚠️ A0 把「时钟可用」写成**硬前提断言**（⛔ 不是 `if (前提) Check(结论)` —— 前提不成立就当场红）。

using UnityEngine;
using WarpforgeVFX;

/// <summary>执行代理 `B117_WSOUNDCOOL`：`WFSoundPlayer` 的 cue 冷却（原版 `AudioCue.ConfigAudioSource`）逐条断言。</summary>
public static class B117_WSOUNDCOOL_Assert
{
    const string Tag = "B117WSOUNDCOOL";
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

    /// <summary>造一条**不在音效表里**的夹具 cue（`WFSoundBank` 只对表内的 cue 给共享实例，所以这条天然独立）。</summary>
    static WFSoundCue MakeCue(float ttl)
    {
        return new WFSoundCue { name = "B117WSOUNDCOOL_fixture", clips = new string[0], timeToPlayAgain = ttl };
    }

    // 夹具 cue 全程持有：`_lastAt` 按**引用身份**计时，换一个实例就是一条新的冷却。
    static readonly WFSoundCue _cueBlocked   = MakeCue(1000f);
    static readonly WFSoundCue _cueNeverPlay = MakeCue(1e9f);    // 见 A2：只要实现「把未初始化当 last=0」就必被拦
    static readonly WFSoundCue _cueNoTtl     = MakeCue(0f);
    static readonly WFSoundCue _cuePickFails = MakeCue(1000f);
    static readonly WFSoundCue _cueOther     = MakeCue(1000f);

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        bool wasEnabled = WFSoundPlayer.Enabled;
        float wasClock = WFSoundPlayer.ClockOverride;
        try
        {
            WFSoundPlayer.Enabled = false;          // 只判闸、不建音源池、不出声（闸排在 `!Enabled` **之外**）
            // 🔴 **2026-11-02 `B117-FIXA`**：把冷却时钟**钉成显式时刻** —— 批处理里 `Time.time` 恒 `0.000`
            //   （`_tmp_view/shop.log` 实测），而原版 `&& (last != 0)` 那个哨兵会把 0 读成「从没播过」
            //   ⇒ 冷却永远不生效、A1/A3/A5/A6 全部**注定**红。这不是实现的问题，是**断言的时钟口径不适配批处理**：
            //   生产端照旧用真实时钟（`WFSoundPlayer.CooldownClock`），断言端给出显式时刻序列。
            WFSoundPlayer.ClockOverride = 1000f;
            WFSoundPlayer.ResetThrottle();          // 冷却表 + `Throttled` + `LastCueThrottled` 一起清

            // ================= A0 硬前提（⛔ 不许静默跳过）=================
            Check(WFSoundPlayer.CooldownClock > 0f && Mathf.Abs(WFSoundPlayer.CooldownClock - 1000f) < 1e-3f,
                  $"A0 前提：冷却时钟 = {WFSoundPlayer.CooldownClock:F3} > 0、且**就是**本组钉进去的那个时刻"
                + "（原版那个 `last != 0` 哨兵靠它区分「从没播过」；批处理 `Time.time` 恒 0 ⇒ 照抄原版时这句冷却"
                + "永远不生效 ⇒ 这里改用 `WFSoundPlayer.ClockOverride` 注入时刻，⛔ 不是把断言放宽）");

            // ================= A1 同一 cue 在 `timeToPlayAgain` 内第二次 ⇒ 不播 =================
            int t0 = WFSoundPlayer.Throttled;
            var r1 = WFSoundPlayer.Play(_cueBlocked, Vector3.zero, is2d: true);
            bool blocked1 = WFSoundPlayer.LastCueThrottled;
            int t1 = WFSoundPlayer.Throttled;
            var r2 = WFSoundPlayer.Play(_cueBlocked, Vector3.zero, is2d: true);
            bool blocked2 = WFSoundPlayer.LastCueThrottled;
            int t2 = WFSoundPlayer.Throttled;
            Check(r1 == null && !blocked1 && t1 == t0,
                  $"A1 前提：第一次**不是**被冷却挡掉的（实得 blocked={blocked1} · Throttled {t0}→{t1}）"
                + "｜🧨 若这里就 blocked ⇒ A2 那条「别把 last 初值当 0」已经红了，先修它");
            Check(r2 == null && blocked2 && t2 == t1 + 1,
                  $"A1 ★ 同一 cue 在 `timeToPlayAgain`({_cueBlocked.timeToPlayAgain:F0}s) 内再播**被挡**："
                + $"第 2 次 blocked={blocked2} · Throttled {t1}→{t2}（应 +1）"
                + "｜判据 = `AudioCue__ConfigAudioSource.c:19-21`；🧨 把闸整个删掉 ⇒ 本条红（那正是改前的状态）");

            // ================= A2 从没播过的 cue：第一次**不许**被拦（灭自证：把 last 初值当 0）=================
            //  夹具 `timeToPlayAgain = 1e9` ⇒ 只要实现里有「未初始化 ⇒ last = 0 / 判 now - 0 < ttp」这一路，
            //  第一次就会被拦（`now` 再大也 < 1e9）⇒ 本条必红。
            int t3 = WFSoundPlayer.Throttled;
            var r3 = WFSoundPlayer.Play(_cueNeverPlay, Vector3.zero, is2d: true);
            bool blocked3 = WFSoundPlayer.LastCueThrottled;
            Check(!blocked3 && WFSoundPlayer.Throttled == t3,
                  $"A2 ★ 从没播过的 cue **第一次一定能播**（哪怕 `timeToPlayAgain` = {_cueNeverPlay.timeToPlayAgain:E0}）："
                + $"blocked={blocked3} · Throttled {t3}→{WFSoundPlayer.Throttled} · 返回 {Desc(r3)}"
                + "｜判据 = 原版 `&& (last != 0)` 那半句（0 = 从没播过）；🧨 把 `last != 0` 去掉 / 用「表里没有 ⇒ last=0」"
                + "去判 ⇒ 本条红");

            // ================= A3 过了窗口能播（**显式推进时钟**，不靠「等」也不靠改 ttp）=================
            WFSoundPlayer.Play(_cueBlocked, Vector3.zero, is2d: true);      // 此刻仍在窗口内 ⇒ 挡住（戳**不**重写）
            bool stillBlocked = WFSoundPlayer.LastCueThrottled;
            float clock0 = WFSoundPlayer.ClockOverride;
            bool blockedAfter = false;
            try
            {
                // 窗口真的过去：`now - last = ttp + 0.01` ⇒ 原版那句 `if` 不成立 ⇒ 放行
                WFSoundPlayer.ClockOverride = clock0 + _cueBlocked.timeToPlayAgain + 0.01f;
                WFSoundPlayer.Play(_cueBlocked, Vector3.zero, is2d: true);
                blockedAfter = WFSoundPlayer.LastCueThrottled;
            }
            finally { WFSoundPlayer.ClockOverride = clock0; }
            Check(stillBlocked && !blockedAfter,
                  $"A3 ★ 窗口一过就能播：挡之前 blocked={stillBlocked} ⇒ 时钟推过 {_cueBlocked.timeToPlayAgain:F0}s 后"
                + $" blocked={blockedAfter}（应为假）"
                + "｜批处理里 `Time.time` 不前进 ⇒ 显式推进 `ClockOverride` 表达 `now - last >= ttp`"
                + "（⛔ 不再靠把 ttp 压到 0 —— 那会把「窗口宽度」这条判据一起测掉）"
                + "｜🧨 把闸写成「只要播过就永远不许」⇒ 本条红");

            // ================= A4 `timeToPlayAgain == 0` 的 cue **永不拦** =================
            int t5 = WFSoundPlayer.Throttled;
            int blockedCount = 0;
            for (int i = 0; i < 3; i++)
            {
                WFSoundPlayer.Play(_cueNoTtl, Vector3.zero, is2d: true);
                if (WFSoundPlayer.LastCueThrottled) blockedCount++;
            }
            Check(blockedCount == 0 && WFSoundPlayer.Throttled == t5,
                  $"A4 ★ `timeToPlayAgain == 0` 的 cue 连播 3 次**一次都不拦**（实得 blocked={blockedCount} ·"
                + $" Throttled {t5}→{WFSoundPlayer.Throttled}）"
                + "｜判据 = 原版：差值 `now - last` 不会 < 0 ⇒ 那句 `if` 恒不成立（数据面 355/513 条 > 0，其余 158 条靠这档）"
                + "｜🧨 用 `<=` 或把 0 当「必须冷却」⇒ 本条红");

            // ================= A5 先写戳、再挑 clip：**挑不出 clip 也算播过** =================
            //  要真的走 `PickClip` 那一步，必须临时把 `Enabled` 打开；夹具 `clips` 为空 ⇒ `PickClip` 直接返 null
            //  （不建音源池：`Rent()` 在那句 `if (clip == null) return null;` **之后**）。
            WFSoundPlayer.Enabled = true;
            try
            {
                int t6 = WFSoundPlayer.Throttled;
                var r4 = WFSoundPlayer.Play(_cuePickFails, Vector3.zero, is2d: true);   // 挑不到 clip
                var r5 = WFSoundPlayer.Play(_cuePickFails, Vector3.zero, is2d: true);   // 仍应被冷却挡住
                Check(r4 == null && r5 == null && WFSoundPlayer.LastCueThrottled
                   && WFSoundPlayer.Throttled == t6 + 1,
                      $"A5 ★ 「先写戳、再挑 clip」：第一次挑不出 clip（返回 {Desc(r4)}）但**戳已写** ⇒ 第二次被挡"
                    + $"（blocked={WFSoundPlayer.LastCueThrottled} · Throttled {t6}→{WFSoundPlayer.Throttled}）"
                    + "｜判据 = `AudioCue__ConfigAudioSource.c:31`（挑 clip 在写戳**之后**）"
                    + "｜🧨 把顺序换过来（挑不出就提前 return、不写戳）⇒ 第二次不被挡 ⇒ 本条红");
            }
            finally { WFSoundPlayer.Enabled = false; }

            // ================= A6 键 = **cue 资产实例**，不是一把全局闸 =================
            int t7 = WFSoundPlayer.Throttled;
            WFSoundPlayer.Play(_cueBlocked, Vector3.zero, is2d: true);        // 再挡一次 `_cueBlocked`
            bool otherBlocked1 = false, otherBlocked2 = false;
            WFSoundPlayer.Play(_cueOther, Vector3.zero, is2d: true);          // 另一条 cue：不受它影响
            otherBlocked1 = WFSoundPlayer.LastCueThrottled;
            WFSoundPlayer.Play(_cueOther, Vector3.zero, is2d: true);          // 它自己第二次才挡
            otherBlocked2 = WFSoundPlayer.LastCueThrottled;
            Check(!otherBlocked1 && otherBlocked2,
                  $"A6 ★ 冷却按 **cue** 分：`_cueBlocked` 正在冷却**不影响**另一条 cue（首播 blocked={otherBlocked1}），"
                + $"而它自己在窗口内第二次被挡（blocked={otherBlocked2}）"
                + "｜判据 = 原版 `lastTimePlayed` 是 `AudioCue` 的实例字段（`+0x34`，`AudioCue__OnEnable.c` 写回 0）"
                + "｜🧨 写成「全局一个 last」⇒ 首播就被挡 ⇒ 本条红。⚠️ 这也正是删掉 `BattleSfx` 按**字段名**那份的原因："
                + "两个字段指向同一条 cue 时，原版是**一起**冷却的（本条与那条改动的判据同源）");
        }
        finally
        {
            WFSoundPlayer.ClockOverride = wasClock; // ⛔ 别把钉住的时刻留给后面的宿主
            WFSoundPlayer.Enabled = wasEnabled;
            WFSoundPlayer.ResetThrottle();          // 别把冷却/计数留给后面的宿主
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    static string Desc(AudioClip c) { return c == null ? "null" : c.name; }
}
