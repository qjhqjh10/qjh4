// PlayerBuild.cs — 「构建后 player 验证」的构建入口
//
// 为什么需要它：本工程**从来没有构建过 player**。2026-09-16 全仓 grep 的结论 ——
// `BuildPipeline` / `BuildPlayer` / `-buildWindowsPlayer` **0 命中**，
// `ProjectSettings/EditorBuildSettings.asset` 里只登记了模板自带的 `SampleScene.unity`。
// 而「构建后 player 验证」是 `项目任务.md` 顶部待办表的**第 1 行**（用户 2026-09-16 拍板）：
// **三条线都欠它 —— 到现在所有绿灯都来自编辑器**，而本工程最贵的一类 bug 恰恰是
// 「编辑器里好、进真包坏」（运行时从 bundle 加载 shader · `StreamingAssets` 路径 ·
// TMP 动态中文字体 · 1.6 G `Resources`）。
//
// 唯一出处（为什么做 / 卡在哪 / 验收标准）：`资料/特效还原_进度与交接.md:772-786`。
//
// ── 用法（CLI）───────────────────────────────────────────────────────────
//   U="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
//
//   # ① 出子集效果库 + 建 player（全量库 1.6 GB prefab，光验证用不着）
//   unset ELECTRON_RUN_AS_NODE && "$U" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod PlayerBuild.RunWithSubsetLibrary -logFile -      # 筛 PBLD 看结果
//
//   # ② 收敛现场：把全量效果库还回去（**一定要跑**，否则编辑器里只剩 12 个效果）
//   ... -executeMethod PlayerBuild.RestoreLibrary -logFile -
//
//   # ③ 跑 player（三个场景各一遍；参数入口是 `PlayerBoot`）
//   _tmp_view/player/WarpforgePlayer.exe -wfscene VFXWhiteboard -wfshot ... -wfquit 20
//
// ── 三条设计取舍 ───────────────────────────────────────────────────────
//  ① **场景直接塞进 `BuildPlayerOptions.scenes`，不动 `EditorBuildSettings.asset`** ——
//     那是仓库里的文件，为了验一次改它 = 给下个会话留一个「Scenes In Build 怎么变了」的谜。
//  ② **子集库写回原路径**：运行时是按常量 `WarpforgeEffectLibrary.ResourcesPath`
//     （`"WarpforgeVFX/WarpforgeEffectLibrary"`）找库的，**换路径读不到而且不报错**。
//     ⇒ 生成前先把全量库备份成同名 `.asset.bak`（Unity 不导入这个扩展名，也不会进构建）。
//  ③ **`-executeMethod` 一次只能给一个**（本工程的实测坑）⇒ 「生成子集库」和「构建」
//     放在同一个入口（`RunWithSubsetLibrary`）里，不能分成两次命令行。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PlayerBuild
{
    const string P = "PBLD ";

    /// <summary>要验的三个场景。**顺序 = 构建顺序，第一个是启动场景**（白板，因为它最独立）。
    /// 名字要和 `PlayerBoot -wfscene <子串>` 对得上。</summary>
    public static readonly string[] Scenes =
    {
        "Assets/WarpforgeArena1/Scenes/VFXWhiteboard.unity",
        "Assets/CardPresentation/Scenes/Battle.unity",
        "Assets/CardPresentation/Scenes/DeckEditor.unity",
    };

    const string ExePath = "d:/4/_tmp_view/player/WarpforgePlayer.exe";
    const string LibPath = "Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset";
    const string LibBackup = "Assets/WarpforgeVFX/WarpforgeEffectLibrary_full.asset.bak";

    [MenuItem("Tools/Warpforge/构建验证用 player（子集效果库）")]
    public static void RunWithSubsetLibrary()
    {
        int code = 0;
        try
        {
            if (!BackupFullLibrary()) { EditorApplication.Exit(2); return; }
            if (!EffectLibraryBuilder.Build(WhiteboardBuilder.Effects, LibPath,
                                            EffectLibraryBuilder.DefaultReportPath))
            {
                Debug.LogError(P + "子集效果库没生成成功 —— 不构建（免得打出一个没有特效的包还以为通过了）");
                EditorApplication.Exit(2);
                return;
            }
            Debug.Log(P + $"已换成子集库（{WhiteboardBuilder.Effects.Length} 个效果）。"
                        + $"⚠️ 别忘了跑 PlayerBuild.RestoreLibrary 把全量库还回去。");
            code = Build() ? 0 : 1;
        }
        catch (Exception e) { Debug.LogError(P + "异常：" + e); code = 3; }
        EditorApplication.Exit(code);
    }

    /// <summary>不换库，直接按当前工程状态构建（调试构建入口本身时用）。</summary>
    [MenuItem("Tools/Warpforge/构建验证用 player（当前效果库）")]
    public static void Run()
    {
        int code = 0;
        try { code = Build() ? 0 : 1; }
        catch (Exception e) { Debug.LogError(P + "异常：" + e); code = 3; }
        EditorApplication.Exit(code);
    }

    /// <summary>把全量效果库还回去（跑过 `RunWithSubsetLibrary` 之后**必须**跑一次）。</summary>
    [MenuItem("Tools/Warpforge/还原全量效果库")]
    public static void RestoreLibrary()
    {
        if (!File.Exists(LibBackup))
        {
            Debug.LogWarning(P + $"没有备份 {LibBackup} —— 说明没换过库（或者备份被删了）。"
                              + "要全量库就重跑 Tools > Warpforge > 生成效果库");
            EditorApplication.Exit(0);
            return;
        }
        File.Copy(LibBackup, LibPath, true);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(LibPath, ImportAssetOptions.ForceUpdate);
        Debug.Log(P + $"全量效果库已还原 → {LibPath}（备份还留着：{LibBackup}）");
        EditorApplication.Exit(0);
    }

    // ── 内部 ────────────────────────────────────────────────────────────

    static bool BackupFullLibrary()
    {
        if (!File.Exists(LibPath))
        {
            Debug.LogError(P + $"没有 {LibPath} —— 先跑 Tools > Warpforge > 生成效果库");
            return false;
        }
        // 🔴 **备份已存在就不覆盖** —— 否则「第二次跑本入口」会把**上一次的备份（那时已经是子集库了）**
        //    再拷一遍，把真正的全量库**覆盖掉**，然后 `RestoreLibrary` 还回来的就是子集库。
        //    这是个一次性、静默、且要重跑几分钟生成器才发现的坑，所以在这里挡住。
        if (File.Exists(LibBackup))
        {
            Debug.Log(P + $"备份已存在，原样保留 → {LibBackup}"
                        + "（要重做全量库就删掉它再跑 Tools > Warpforge > 生成效果库）");
            return true;
        }
        File.Copy(LibPath, LibBackup, true);
        Debug.Log(P + $"全量效果库已备份 → {LibBackup}（{new FileInfo(LibBackup).Length / 1024} KB）");
        return true;
    }

    static bool Build()
    {
        // 场景先自查：路径写错的话 BuildPlayer 只会给一句很难查的结果
        var missing = new List<string>();
        foreach (var sc in Scenes) if (!File.Exists(sc)) missing.Add(sc);
        if (missing.Count > 0)
        {
            Debug.LogError(P + "找不到场景（路径写错了？）：" + string.Join(" | ", missing));
            return false;
        }
        Debug.Log(P + "场景清单（第一个是启动场景）：" + string.Join(" | ", Scenes));

        Directory.CreateDirectory(Path.GetDirectoryName(ExePath));

        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        Debug.Log(P + $"=== 开始构建 {opts.target} → {ExePath} ===");
        var report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;

        Debug.Log(P + $"=== 构建结束：{s.result} ===");
        Debug.Log(P + $"  产物 {ExePath}（{s.totalSize / 1024 / 1024} MB）· 耗时 {s.totalTime.TotalSeconds:F0} s" +
                  $" · 错误 {s.totalErrors} · 警告 {s.totalWarnings}");

        // 哪一步炸的要说出来 —— 光看 result 分不清「编译失败」还是「打包失败」。
        // ⚠️ `BuildStep` 只有 `name` / `depth` / `duration` / `messages` —— **没有 `result`**
        //    （那上面两个枚举 `BuildStepResult` 在本工程这套 Unity 里也解析不到，实测 `CS0103`）。
        //    所以步骤只打名字与耗时，出没出错看下面 messages 里有没有 Error。
        foreach (var step in report.steps)
        {
            int errs = 0;
            foreach (var m in step.messages) if (m.type == LogType.Error || m.type == LogType.Exception) errs++;
            Debug.Log(P + $"  步骤「{step.name}」{step.duration.TotalSeconds:F0} s"
                      + $"（{step.messages.Length} 条消息，其中报错 {errs}）");
        }

        if (s.result != BuildResult.Succeeded)
        {
            bool logged = false;
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (!logged && (m.type == LogType.Error || m.type == LogType.Exception))
                    {
                        Debug.LogError(P + $"  第一个错误：[{step.name}] {m.content}");
                        logged = true;
                    }
            return false;
        }

        // 复现「跑起来看什么」那一步用到的命令，免得到时候又去翻文档。
        // ⚠️ Battle 多一个 `-wfdrive`（自动打一局，见 `BattleAutoDrive`）—— 它要跑完整局，
        //    所以 `-wfquit` 给得比另外两个大得多；跑完的截图在 `auto_*.png`。
        var dir = Path.GetDirectoryName(ExePath).Replace('\\', '/');
        foreach (var sc in Scenes)
        {
            var name = Path.GetFileNameWithoutExtension(sc);
            bool isBattle = name.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0;
            Debug.Log(P + $"  跑「{name}」：" + Path.GetFileName(ExePath)
                      + $" -wfscene {name}"
                      + (isBattle ? " -wfdrive" : "")
                      + $" -wfshot {dir}/{name}.png"
                      + (isBattle ? " -wfshotat 12 -wfquit 240" : " -wfshotat 15 -wfquit 30")
                      + $" -logFile {dir}/{name}.log");
        }
        return true;
    }
}
