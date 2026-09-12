// DOTweenSetup.cs — 无头安装 DOTween 的收尾步骤（等价于 Utility Panel 里点的那几下）
//
// 为什么要它：DOTween 装进来之后有两件事必须在编辑器里做，而 GUI 点不了：
//   1. **关掉用不上的模块**。模块的开关是「默认全开、用 `DOTWEEN_NOxxx` 关掉」
//      （见 Modules/*.cs 顶部的 `#if !DOTWEEN_NOxxx`）。本工程 manifest 是瘦的，
//      没装 `com.unity.modules.physics2d` —— 于是 DOTweenModulePhysics2D.cs 引用
//      Rigidbody2D 直接编译不过。卡牌游戏用不上 2D 物理，关掉即可。
//   2. 建 `Resources/DOTweenSettings.asset`。DOTween 靠它记「哪些模块开着 + 默认缓动」，
//      没有它的工程一进编辑器就会弹 Utility Panel。
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod DOTweenSetup.Run -logFile -
//   筛输出：grep "^DWS " d:/4/_tmp_view/dotween_setup.log
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DOTweenSetup
{
    const string P = "DWS ";

    /// <summary>本工程用不上的模块 —— 缺哪个模块就加哪个进来（对应 Modules/*.cs 的开关）</summary>
    static readonly string[] DisableDefines =
    {
        "DOTWEEN_NOPHYSICS2D",   // 没装 com.unity.modules.physics2d
    };

    const string SettingsPath = "Assets/Resources/DOTweenSettings.asset";

    public static void Run()
    {
        Debug.Log(P + "=== DOTween 收尾安装 开始 ===");

        EnsureDefines();
        EnsureSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(P + "=== 结束 ===");
    }

    static void EnsureDefines()
    {
        foreach (var target in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS })
        {
            string cur;
            try { cur = PlayerSettings.GetScriptingDefineSymbolsForGroup(target); }
            catch (Exception e) { Debug.Log(P + $"  {target}: 读不到 define（{e.GetType().Name}），跳过"); continue; }

            var list = cur.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            bool changed = false;
            foreach (var d in DisableDefines)
            {
                if (list.Contains(d)) continue;
                list.Add(d);
                changed = true;
            }
            if (!changed) { Debug.Log(P + $"  {target}: define 已是最新"); continue; }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", list));
            Debug.Log(P + $"  {target}: define → {string.Join(";", list)}");
        }
        AssetDatabase.Refresh();
    }

    /// <summary>建 DOTweenSettings 资产。类型在 DOTween.dll 里，用反射创建，
    /// 免得写死一个可能改名的类型让整个工程编译不过。</summary>
    static void EnsureSettings()
    {
        if (File.Exists(SettingsPath))
        {
            Debug.Log(P + "  DOTweenSettings 已存在");
            return;
        }

        Type t = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType("DG.Tweening.Core.DOTweenSettings");   // 真名在这里，不是 DG.Tweening
            if (t != null) break;
        }
        if (t == null)
        {
            Debug.LogWarning(P + "  找不到 DG.Tweening.Core.DOTweenSettings —— 这次跳过。"
                              + "运行时不依赖它（缺了就用默认值），但编辑器里可能会弹 Utility Panel。");
            return;
        }

        var dir = Path.GetDirectoryName(SettingsPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Resources");

        var so = ScriptableObject.CreateInstance(t);
        AssetDatabase.CreateAsset(so, SettingsPath);
        Debug.Log(P + $"  已创建 {SettingsPath}");
    }
}
