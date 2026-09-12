// ============================================================================
//  「打开游戏」的入口 —— 编辑器菜单 + 命令行都能进 Play 模式
//
//  这个工程的「游戏」就是 `Assets/CardPresentation/Scenes/Battle.unity` 按 Play
//  （见 `资料/规则引擎_进度与交接.md:12`）。以前只能人手动点 Play ——
//  命令行起 Unity 时没人点，就只能跑批处理自检，**画面看不见**。
//  有了这个入口，命令行也能把同一局开起来给人看：
//
//    unset ELECTRON_RUN_AS_NODE && "$UNITY" -projectPath "D:\4\Unity\MyGame" \
//        -executeMethod PlayGame.Launch -logFile "d:/4/Unity/UnityPlay.log"
//
//  ⚠️ **不要加 `-batchmode`**（批处理下没有窗口，Play 了什么也看不见），
//     也**不要加 `-quit`**（编辑器会立刻退掉，还没看到就没了）。
//  ⚠️ 同一个工程**同一时刻只能跑一个 Unity 实例** —— 要跑批处理自检，先把窗口关掉。
//
//  开局走的是**真实入口** `BattleDriver.Start()` → `BeginFromDeckLibrary()`：
//  读卡组库里当前选中的那套（没有就自动凑一副），我方阵营 = 督军的阵营，
//  对手默认 `Goff`（兽人）—— 所以「按 Play」看到的就是**极限战士 vs 兽人**。
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayGame
{
    /// <summary>能玩的那个场景。和 `BattleScene.ScenePath` 是同一个路径（那边是 private，就近抄一份）</summary>
    const string ScenePath = "Assets/CardPresentation/Scenes/Battle.unity";

    [MenuItem("Tools/CardPresentation/打开对战（Play）")]
    public static void Launch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PlayGame] 已经在 Play 模式里了，不重复开");
            return;
        }
        // ⚠️ 走 `delayCall` 而不是当场开：`-executeMethod` 是在**编辑器还没完全起来**的时候调的，
        //    那时候直接 `OpenScene` / `isPlaying = true` 会抛（场景系统还没就绪）。
        //    `delayCall` 排在编辑器环路的第一帧，人点菜单时也是下一帧 —— 两条路都安全。
        EditorApplication.delayCall += OpenAndPlay;
    }

    static void OpenAndPlay()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"[PlayGame] 打不开场景 `{ScenePath}` —— 游戏没起来");
            return;
        }
        Debug.Log($"[PlayGame] 已打开 `{ScenePath}`，进 Play 模式");
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.isPlaying = true;
    }

    /// <summary>
    /// 进了 Play 模式之后把 **Game 页签切到前面**。
    ///
    /// ⚠️ 第一次（2026-09-12）没做这件事：编辑器恢复的是上次的布局，停在**别的地方**，
    /// 游戏在跑但人在看别的页签 —— 「开了游戏」和「看得见游戏」是两件事。
    /// </summary>
    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        // `GameView` 是 internal 类型，`Type.GetType` 是官方留给这种情况的口子（找不到就只是不切页签，不影响玩）
        var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
        {
            Debug.LogWarning("[PlayGame] 找不到 `UnityEditor.GameView` —— 请手动点一下 Game 页签");
            return;
        }
        var view = EditorWindow.GetWindow(gameViewType);   // 切页签 + 带到前面
        if (view != null) view.Focus();
    }
}
