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
//     🔴 **2026-09-19 改**：原来靠「生成前备份成 `.asset.bak`、建完拷回来」，
//     而 `.bak` 会**过期**（实测那个是 09-11 生成的、没有模块数据），照样盖回去
//     ⇒ 编辑器里「0 个效果带模块」。**现在改成建完 `EffectLibraryBuilder.Run()` 重新生成**
//     （库本来就是生成物，永远不会过期）。见 `RestoreLibraryInternal` 的注释。
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

    [MenuItem("Tools/Warpforge/构建验证用 player（子集效果库）")]
    public static void RunWithSubsetLibrary()
    {
        int code = 0;
        try
        {
            // ⚠️ **不再备份全量库**（2026-09-19 改）：还原改成**重跑生成器**，
            //    所以不需要 `.bak`，也就不存在「备份过期 / 备份被当成权威」那一类坑。
            //    见 `RestoreLibraryInternal` 的注释。
            if (!EffectLibraryBuilder.Build(WhiteboardBuilder.Effects, LibPath,
                                            EffectLibraryBuilder.DefaultReportPath))
            {
                Debug.LogError(P + "子集效果库没生成成功 —— 不构建（免得打出一个没有特效的包还以为通过了）");
                EditorApplication.Exit(2);
                return;
            }
            Debug.Log(P + $"已换成子集库（{WhiteboardBuilder.Effects.Length} 个效果）…");
            code = Build() ? 0 : 1;

            // 🔴 **建完立刻还原全量库**（2026-09-16 加的）。
            //    原来要人**另外跑一次** `RestoreLibrary`，而那条命令很容易忘 ——
            //    实测忘了的后果是：**编辑器里只剩 17 个特效**，`BattleScene.Run` 直接报
            //    「VfxMap 里 14 个特效名在库里都找得到（缺 14）」，看着像代码回归。
            //    子集库**只在构建那一小段**需要，player 打完包就不用了 ⇒ 还原放这里最稳。
            RestoreLibraryInternal();
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
        RestoreLibraryInternal();
        EditorApplication.Exit(0);
    }

    /// <summary>还原本体（不带 `Exit`，因为 `RunWithSubsetLibrary` 建完包也要调它）。
    ///
    /// 🔴 **2026-09-19 改成「重新生成」而不是「从 `.bak` 拷回来」—— 因为拷回来的库会过期。**
    ///   实据：`.bak` 是 **2026-09-11** 生成的，那时效果库**还没有模块数据**；而
    ///   `BackupFullLibrary` 的「备份已存在就不覆盖」把它当成了权威 ⇒ 还原之后
    ///   `AnimFXCheck` 从 **39/0 掉到 8/5**（「0 个效果带模块」）、`BattleScene.Run` 报
    ///   「VfxMap 里 14 个特效名在库里都找得到（**缺 14**）」——
    ///   **看着像代码回归，其实是把一个过期的库盖了回去。**
    ///   （这正是 `资料/特效还原_进度与交接.md` §七 记的那类坑，只是那次是「忘了还原」，这次是「还原错了」。）
    ///
    ///   效果库本来就是**生成物**（`EffectLibraryBuilder` 从 `数据/游戏数据/animfx_*.json` +
    ///   `数据/索引/effect_index.json` 生成）⇒ **还原 = 重跑生成器，永远不会过期。**
    ///   `.bak` 机制**整个删掉了**（连 `BackupFullLibrary` 一起去掉）——
    ///   留着一个过期的备份文件本身就是个陷阱：谁点一下「还原全量效果库」就会踩同一个坑。</summary>
    static bool RestoreLibraryInternal()
    {
        EffectLibraryBuilder.Run();                      // 全量：`NameFilter` 为空 = 全收
        AssetDatabase.Refresh();

        if (!File.Exists(LibPath))
        {
            Debug.LogError(P + $"还原失败：{LibPath} 不存在");
            return false;
        }
        int n = CountEntries(LibPath);
        Debug.Log(P + $"全量效果库已重新生成 → {LibPath}（{n} 条）");
        if (n < 100)
        {
            // 不许静默：这个数一不正常，编辑器里就会「只剩十几个特效」，而报错会指向别处
            Debug.LogError(P + $"⚠️ 还原出来的库只有 {n} 条 —— **不正常**（全量应当 900+）。" +
                              "别把它当成代码回归，先查 `EffectLibraryBuilder` 的日志。");
            return false;
        }
        return true;
    }

    /// <summary>数库 YAML 里有多少条 `  - name:`（还原后自查用）。</summary>
    static int CountEntries(string path)
    {
        int n = 0, at = 0;
        var text = File.ReadAllText(path);
        while ((at = text.IndexOf("\n  - name:", at, StringComparison.Ordinal)) >= 0) { n++; at++; }
        return n;
    }

    // ── 内部 ────────────────────────────────────────────────────────────

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
