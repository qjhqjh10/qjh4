// WhiteboardBuilder.cs — 生成「白板场景」：打开就能按 Play 看着特效一个个铺出来
//
// 和 WhiteboardTest 的分工：
//   · WhiteboardTest 是**批处理自检**（不进 play 模式，数字说话，跑在 CI/命令行里）
//   · 本工具生成的是**人看的场景**（打开工程按 Play，肉眼确认「播 → 消失」长什么样）
// 两者用的是同一个 VFXWhiteboard / WarpforgeEffectPlayer，所以自检过了场景里就不该有意外。
//
// 用法（菜单）：Tools > Warpforge > 生成白板场景
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod WhiteboardBuilder.Run -logFile -
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WarpforgeVFX;

public static class WhiteboardBuilder
{
    const string P = "WBSCENE ";
    const string ScenePath = "Assets/WarpforgeArena1/Scenes/VFXWhiteboard.unity";
    const string LibraryPath = "Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset";

    /// <summary>白板上铺哪些效果。挑的都是台账「对得上 + 高置信」的，看得清、也代表还原水平。
    /// **不放全屏类的战场环境效果**（Environmental Condition 那类）—— 它们半径上百个单位，
    /// 铺进格子会糊满整屏，把别的效果全盖掉。检测见 VFXWhiteboard.IsFullscreen。</summary>
    static readonly string[] Effects =
    {
        "Antimatter Explosion",
        "AmbushEffect",
        "Lightning_Green",
        "ArmourEffect_Bladeguard_Shield",
        "Explosion_Ground",
        "Attack_Stomp",
        "Atk_GrotGrenade",
        "Explosion Hive Fleet Arrival Tendrils",
        "Card_Buff_Hand",
        "Buff_DaemonicPact_troop",
        "StunEffect_proc",
        "CardPrefab",
    };

    [MenuItem("Tools/Warpforge/生成白板场景")]
    public static void Run()
    {
        var lib = AssetDatabase.LoadAssetAtPath<WarpforgeEffectLibrary>(LibraryPath);
        if (lib == null)
        {
            Debug.LogError(P + $"没有效果库 {LibraryPath} —— 先跑 Tools > Warpforge > 生成效果库");
            return;
        }
        WarpforgeEffectLibrary.Instance = lib;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 相机：正对格子墙，背景压暗（特效大多是加法混合，亮底看不出来）。
        // 距离按「墙的宽高 + 游戏视图的宽高比」算，保证整面墙都在画面里 ——
        // 手填一个数字的话，改了列数/格宽就会有大半在画面外。
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        cam.fieldOfView = 45f;
        camGo.transform.position = CameraPos(cam.fieldOfView, 16f / 9f);
        camGo.transform.LookAt(WallCenter());

        // 有的效果用的是受光 shader（matcap/场景贴图那类），没光会渲染成黑 —— 补一盏平行光
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var wbGo = new GameObject("Whiteboard");
        var wb = wbGo.AddComponent<VFXWhiteboard>();
        wb.effects = Effects;
        wb.columns = Columns;
        wb.cellSize = CellSize;
        wb.stagger = 0.6f;          // 人看的话慢一点，能看清每个效果进场
        wb.fitToCell = true;
        wb.loop = true;             // 一轮播完自动重来，方便反复看

        var missing = new List<string>();
        var fullscreen = new List<string>();
        foreach (var n in Effects)
        {
            WFEffectEntry e;
            if (!lib.TryGet(n, out e)) { missing.Add(n); continue; }
            if (VFXWhiteboard.IsFullscreen(e.prefab)) fullscreen.Add(n);
        }
        if (missing.Count > 0)
            Debug.LogWarning(P + $"效果库里没有这几个，白板上会缺：{string.Join(", ", missing)}");
        if (fullscreen.Count > 0)
            Debug.LogWarning(P + $"这几个是全屏类，铺进格子会糊满整屏，建议从清单里去掉：{string.Join(", ", fullscreen)}");

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log(P + $"=== 白板场景写好了：{ScenePath}（{Effects.Length} 个效果，错峰 {wb.stagger}s）===");
        Debug.Log(P + "打开它按 Play 就能看；批处理自检请跑 WhiteboardTest.Run");
    }

    const int Columns = 4;
    const float CellSize = 5.5f;

    /// <summary>格子墙的中心（X 居中、Y 从 0 往下排，所以中心在墙高的中点）</summary>
    static Vector3 WallCenter()
    {
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)Effects.Length / Columns));
        return new Vector3(0f, -(rows - 1) * CellSize * VFXWhiteboard.RowFactor * 0.5f, 0f);
    }

    /// <summary>相机位（墙正前方）。距离同时满足「墙宽在水平视野内」和「墙高在垂直视野内」。</summary>
    static Vector3 CameraPos(float vfovDeg, float aspect)
    {
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)Effects.Length / Columns));
        float wide = Columns * CellSize;
        float tall = rows * CellSize * VFXWhiteboard.RowFactor;

        float halfV = Mathf.Tan(Mathf.Deg2Rad * vfovDeg * 0.5f);
        float halfH = halfV * aspect;
        float dist = Mathf.Max(tall * 0.5f / halfV, wide * 0.5f / halfH) * 1.15f;

        var c = WallCenter();
        return new Vector3(c.x, c.y, c.z - dist);
    }
}
