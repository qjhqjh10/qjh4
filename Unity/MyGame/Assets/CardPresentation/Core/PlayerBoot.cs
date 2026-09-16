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
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBoot : MonoBehaviour
{
    const string FlagScene = "-wfscene";     // 切到名字含该子串的场景
    const string FlagShot = "-wfshot";       // 截图落盘路径
    const string FlagShotAt = "-wfshotat";   // 第几秒拍（默认 8）
    const string FlagQuit = "-wfquit";       // 第几秒退出（默认：给了 -wfshot 就是 shotat + 5）
    const string FlagDrive = "-wfdrive";     // 自动打一局（只对 Battle 场景有意义，见 `BattleAutoDrive`）

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

        // 一个参数都没给就别建东西（编辑器里按 Play 不受影响，player 里也干净）
        if (Arg(FlagScene) == null && Arg(FlagShot) == null && Arg(FlagQuit) == null
            && !HasFlag(FlagDrive)) return;

        _state = 1;
        var go = new GameObject("~PlayerBoot");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerBoot>();             // `Start()` 会在下一帧把协程拉起来
    }

    void Start()
    {
        StartCoroutine(Run());
    }

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

        // ---- 自动打一局（`-wfdrive`）----
        // 放在「截图/退出」之前：只给 `-wfdrive` 不给 `-wfshot/-wfquit` 时也要能驱
        //（下面那句 `yield break` 会把没要截图也没要退出的提前收掉）。
        if (HasFlag(FlagDrive))
        {
            var s = Arg(FlagShot);
            var d = gameObject.AddComponent<BattleAutoDrive>();
            d.shotDir = string.IsNullOrEmpty(s) ? null : Path.GetDirectoryName(s).Replace('\\', '/');
            d.Begin();
            Debug.Log($"[PlayerBoot] -wfdrive：自动打一局已启动"
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
                Application.Quit(0);
                yield break;
            }
            yield return null;
        }
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
