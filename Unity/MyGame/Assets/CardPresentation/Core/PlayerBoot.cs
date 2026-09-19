// PlayerBoot.cs — 独立运行的 player 的启动参数入口
//
// 为什么有它：「构建后 player 验证」（`项目任务.md` 顶部待办表第 1 行）要一次验**三个场景**，
// 而一个 player 只有一个启动场景。构建脚本把三个场景都塞进 build list（`PlayerBuild.Scenes`），
// 再用 `-wfscene <子串>` 切过去 —— **一次 2 GB 的构建验三件事**，比建三个 player 划算。
//
// 用法（在 **player** 的命令行上，不是 Unity 的）：
//   WarpforgePlayer.exe -wfscene Battle
//   WarpforgePlayer.exe -wfscene DeckEditor -wfshot d:/4/_tmp_view/player/deck.png -wfquit 25
//   WarpforgePlayer.exe                                   # 不带 = 停在 build list 第一个场景（白板）
//
// 🔴 三条纪律：
//  ① **`Application.isEditor` 直接返回** —— 这个入口绝不能影响编辑器里的四条自检。
//  ② 🔴 **`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 只在启动时跑一次** ——
//     换个场景**不会**再触发。所以「切场景」这件事必须放在**自己建的、`DontDestroyOnLoad` 的
//     那个 GameObject 的协程**里做（见 `Boot()` 的注释：第一版按「每场景一次」写，
//     结果切完场景没人截图/没人退出，跑批就卡在那儿）。
//  ③ 截图走 `ScreenCapture.CaptureScreenshot`：它**在帧末才写盘**，所以拍完要再等几帧
//     才能退，否则文件是空的（而我们一定会踩：批处理自检那边就没有帧循环，习惯不一样）。
//     🔴 **依赖 `com.unity.modules.screencapture`** —— 这个工程的 manifest 是瘦的，
//     2026-09-16 之前**没装这个模块**（`ScreenCapture` 类型根本不存在，`error CS0103`）。
//     已补进 `Packages/manifest.json`。**不能用「相机渲到 RenderTexture」代替** ——
//     本工程的 Canvas 是 **Screen Space Overlay**（没有任何地方设过 renderMode），
//     相机 `Render()` 拍不到它，截出来的图会**整块缺 UI**（而这正是要验的东西之一）。
using System;
using System.Collections;
using System.IO;
using CardPresentation;   // ⚠️ `CardTween` 在这个命名空间里（诊断开关要用）
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBoot : MonoBehaviour
{
    const string FlagScene = "-wfscene";     // 切到名字含该子串的场景
    const string FlagShot = "-wfshot";       // 截图落盘路径
    const string FlagShotAt = "-wfshotat";   // 第几秒拍（默认 8）
    const string FlagQuit = "-wfquit";       // 第几秒退出（默认：给了 -wfshot 就是 shotat + 5）
    const string FlagDrive = "-wfdrive";     // 自动打一局（只对 Battle 场景有意义，见 `BattleAutoDrive`）
    const string FlagHuman = "-wfhuman";     // 同上，但**我的回合走 UI 那条路**（选择器/技能/结束回合按钮）
    const string FlagWatch = "-wfwatch";     // 看门狗 + 面包屑（`Watch.cs`，**绕开 Unity 日志**写自己的文件）
    const string FlagWatchFile = "-wfwatchfile";   // 看门狗文件路径（默认 <当前目录>/watch.log）
    const string FlagStall = "-wfstall";     // 主线程几秒没出帧就**自杀**（默认 0 = 不判，留现场给采样）

    /// <summary>0 = 还没动 · 1 = 已建 runner（收工）</summary>
    static int _state;

    /// <summary>🔴 **这个特性只在「启动时、第一帧场景加载完之后」跑一次** ——
    /// 它**不会**因为 `SceneManager.LoadScene` 再触发。
    ///
    /// 第一版就是按「每加载一次场景跑一次」写的（用 `_state` 三态 + 切完场景再起 runner），
    /// 结果**切场景那条路上永远起不了 runner** ⇒ 不截图、不自动退出，
    /// **表现是「跑批卡住」（实测：Battle 的日志停在「凑牌组」两行、进程一直活着）**。
    /// ⇒ 现在改成：**先把 runner 建出来（`DontDestroyOnLoad`），切场景放在它的协程里做** ——
    /// 切完再接着截图/退出，不依赖那个只跑一次的特性。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Application.isEditor) return;          // 纪律 ①
        if (_state != 0) return;

        // 诊断：`WF_NO_SETLINK=1` 关掉补间生命周期绑定（用来 A/B 看某个现象是不是它引起的）
        if (Environment.GetEnvironmentVariable("WF_NO_SETLINK") == "1")
        {
            CardTween.LinkEnabled = false;
            Debug.LogWarning("[PlayerBoot] WF_NO_SETLINK=1 —— 已关掉 CardTween 的 SetLink（诊断模式）");
        }

        // 诊断：`WF_MANUAL_TWEEN=1` 把补间改成**手动推进**（= 编辑器自检那条路）。
        //
        // 🔴 **它是为了回答一个具体问题**：`DOTween` 那 22 条 null target 报错，
        //    **编辑器自检里一条都没有**。`DOTweenSettings` 两边一样（在 `Assets/Resources/` 里、
        //    `useSafeMode: 1`），所以差异只能在**行为**上。两个候选：
        //      ① 编辑器用 `UpdateType.Manual` 推进 ⇒ **只有 `CardTween.Advance` 被调到时才评估补间**，
        //         孤儿补间可能**从来没被评估过** ⇒ 不报错（= 编辑器天生看不见这一族）；
        //      ② 只是编辑器的动作编排恰好没制造出「补间在飞时对象被销毁」。
        //    这个开关就是 A/B ①：**配上 `WF_NO_SETLINK=1` 一起跑** ——
        //    报错数从 19 掉到 0 就说明是 ①。（判定记录见 `资料/特效还原_进度与交接.md` §七。）
        if (Environment.GetEnvironmentVariable("WF_MANUAL_TWEEN") == "1")
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            Debug.LogWarning("[PlayerBoot] WF_MANUAL_TWEEN=1 —— 补间改成手动推进（诊断模式）");
        }

        // 一个参数都没给就别建东西（编辑器里按 Play 不受影响，player 里也干净）
        if (Arg(FlagScene) == null && Arg(FlagShot) == null && Arg(FlagQuit) == null
            && !HasFlag(FlagDrive) && !HasFlag(FlagHuman) && !HasFlag(FlagWatch)) return;

        _state = 1;
        var go = new GameObject("~PlayerBoot");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerBoot>();             // `Start()` 会在下一帧把协程拉起来
    }

    void Start()
    {
        // 🔴 **看门狗要在 `Run()` 之前起来** —— 它要覆盖「等场景就绪」那一段
        //    （`PlayerBoot` 自己就在那一段踩过坑：切场景那条路上起不了 runner ⇒ 跑批看着像卡住）。
        if (HasFlag(FlagWatch))
        {
            var wf = Arg(FlagWatchFile);
            if (string.IsNullOrEmpty(wf)) wf = Path.Combine(Environment.CurrentDirectory, "watch.log");
            int stall = (int)ArgFloat(FlagStall, 0f);
            Watch.Start(wf, stall);
            Debug.LogWarning($"[PlayerBoot] -wfwatch：看门狗在写 {wf}（绕开 Unity 日志，卡死时看它）"
                             + (stall > 0 ? $"· 长停顿 {stall}s 自杀" : "· 长停顿不自杀（留现场）"));
        }
        StartCoroutine(Run());
    }

    /// <summary>看门狗要一个每帧递增的帧号（`Watch.cs`；没开 `-wfwatch` 时是空操作）。</summary>
    void Update() { Watch.Tick(); }

    // 焦点/暂停事件 —— 用来**证伪**「失去焦点被 Unity 暂停」那一族猜测。
    // 2026-09-17 已用外部监视器实测：前台是 VSCode 时游戏 CPU 照涨（= 本工程 runInBackground 是开的），
    // 但那要另起一个进程去猜；把证据写进看门狗文件里，下次一眼就有。
    void OnApplicationFocus(bool f) { Watch.Event($"OnApplicationFocus({f})"); }
    void OnApplicationPause(bool p) { Watch.Event($"OnApplicationPause({p})"); }

    IEnumerator Run()
    {
        string want = Arg(FlagScene);
        if (!string.IsNullOrEmpty(want) && !CurrentSceneMatches(want))
        {
            if (LoadSceneMatching(want))
            {
                // 等目标场景真的变成 active（同步 LoadScene 在帧末生效）
                for (int i = 0; i < 30 && !CurrentSceneMatches(want); i++) yield return null;
                Debug.Log($"[PlayerBoot] 已到「{SceneManager.GetActiveScene().name}」");
            }
        }

        // ---- 自动打一局（`-wfdrive` / `-wfhuman`）----
        // 放在「截图/退出」之前：只给驱动开关、不给 `-wfshot/-wfquit` 时也要能驱
        //（下面那句 `yield break` 会把没要截图也没要退出的提前收掉）。
        if (HasFlag(FlagDrive) || HasFlag(FlagHuman))
        {
            var s = Arg(FlagShot);
            var d = gameObject.AddComponent<BattleAutoDrive>();
            d.humanStyle = HasFlag(FlagHuman);
            d.shotDir = string.IsNullOrEmpty(s) ? null : Path.GetDirectoryName(s).Replace('\\', '/');
            d.Begin();
            Debug.Log($"[PlayerBoot] {(d.humanStyle ? FlagHuman : FlagDrive)}：自动打一局已启动"
                      + $"（截图目录 {d.shotDir ?? "不截图"}）");
        }

        string shot = Arg(FlagShot);
        float shotAt = ArgFloat(FlagShotAt, 8f);
        float quit = ArgFloat(FlagQuit, string.IsNullOrEmpty(shot) ? -1f : shotAt + 5f);
        if (string.IsNullOrEmpty(shot) && quit <= 0f) yield break;   // 什么都没要

        yield return Auto(shot, shotAt, quit);
    }

    IEnumerator Auto(string shotPath, float shotAt, float quitAfter)
    {
        bool shotDone = string.IsNullOrEmpty(shotPath);
        float t = 0f;
        Debug.Log($"[PlayerBoot] 场景「{SceneManager.GetActiveScene().name}」" +
                  $"，{(shotDone ? "不截图" : $"第 {shotAt:F1}s 截图 → {shotPath}")}" +
                  $"{(quitAfter > 0f ? $"，第 {quitAfter:F1}s 退出" : "，不自动退出")}");

        while (true)
        {
            t += Time.unscaledDeltaTime;

            if (!shotDone && t >= shotAt)
            {
                shotDone = true;
                try
                {
                    var dir = Path.GetDirectoryName(shotPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    ScreenCapture.CaptureScreenshot(shotPath);
                    Debug.Log($"[PlayerBoot] 截图 → {shotPath}");
                }
                catch (Exception e) { Debug.LogError($"[PlayerBoot] 截图失败：{e.Message}"); }
                for (int i = 0; i < 5; i++) yield return new WaitForEndOfFrame();   // 纪律 ③
            }

            if (quitAfter > 0f && t >= quitAfter)
            {
                Debug.Log($"[PlayerBoot] {t:F1}s 到点，退出（截图{(shotDone ? "已拍" : "没拍")}）");
                DumpAudioSummary();                   // 🆕 2026-09-19：三个场景都会打这一行
                Application.Quit(0);
                yield break;
            }
            yield return null;
        }
    }

    /// <summary>退出前把音频状态打一行 —— **三个场景都会打**（放在 `PlayerBoot` 而不是某个场景里）。
    ///
    /// 🔴 为什么要这么放：`AudioMixer.SetFloat` 在**编辑器（非 Play）里是空操作**
    ///    （实测「设 −20 → 读回 0.00」，而音频子系统本身是活的 48kHz/Stereo），**只有真包能验**。
    ///    而 **Battle 那一局是 AI 代打**，它走 VfxMap 挑的效果多半**不在子集效果库**里
    ///    （日志会明写「效果库里没有 `Tap Blue Glow`」）⇒ 音效计数恒为 0、什么都验不到；
    ///    **VFXWhiteboard 那一场会把子集里 12 个效果全播一遍**，其中 8 个带 `sounds` —— 那才验得到。
    /// </summary>
    static void DumpAudioSummary()
    {
        bool ready = WarpforgeAudio.Ready, pOk = WarpforgeAudio.ParamsOk;
        Debug.Log($"[PlayerBoot·音频] 总线 Ready={ready} · 四个暴露参数认得={pOk} · {WarpforgeAudio.Dump()}");

        float saved = WarpforgeAudio.Music;
        WarpforgeAudio.SetMusic(0.5f);
        Debug.Log($"[PlayerBoot·音频] **读回往返**：设 Music=0.5 → {WarpforgeAudio.Dump()}"
                  + "（**读到 −6.0dB 才算真生效**；编辑器里读不回来是环境限制，不是坏的）");
        WarpforgeAudio.SetMusic(saved);              // 还原，别把存档改了

        Debug.Log($"[PlayerBoot·音频] 特效音效累计请求播放 **{WarpforgeVFX.WFSoundPlayer.Played}** 次 · "
                  + $"cue 表 {WarpforgeVFX.WFSoundBank.CueCount} 个 cue（0 次 = 这一场一个带音效的效果都没触发）");
    }

    // ---- 参数读取（player 的命令行 = Environment.GetCommandLineArgs）----

    static string Arg(string flag)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < a.Length; i++)
            if (string.Equals(a[i], flag, StringComparison.OrdinalIgnoreCase)) return a[i + 1];
        return null;
    }

    /// <summary>只认「有没有这个开关」，后面不跟值的那种（`-wfdrive`）。</summary>
    static bool HasFlag(string flag)
    {
        foreach (var a in Environment.GetCommandLineArgs())
            if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static float ArgFloat(string flag, float fallback)
    {
        float v;
        return float.TryParse(Arg(flag), System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out v) ? v : fallback;
    }

    static bool CurrentSceneMatches(string want)
    {
        return SceneManager.GetActiveScene().name.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool LoadSceneMatching(string want)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = Path.GetFileNameWithoutExtension(path);
            if (name.IndexOf(want, StringComparison.OrdinalIgnoreCase) < 0) continue;
            Debug.Log($"[PlayerBoot] 切到「{name}」（build index {i}）");
            SceneManager.LoadScene(name, LoadSceneMode.Single);
            return true;
        }
        // 找不到就**如实报错**，别静默停在上一个场景里（那看起来像「切成功了但画面没变」）
        Debug.LogError($"[PlayerBoot] build list 里没有名字含「{want}」的场景 —— " +
                       $"可用的在 PlayerBuild.Scenes 里，构建日志的「场景清单」一行也打印了");
        return false;
    }
}
