// B148_W4_Assert.cs — 执行写手 `W4`（2026-11-05 · 批次 `B148`）· 账 `B146-B02-A24-IDENTITY-ASSERT`
//
// ============================ 这条断言补的是什么 ============================
// `B147_对账_R5Unity构建.md` §一·`B146-B02-A24-IDENTITY-ASSERT` 现读结论：
//   `Editor/` 全目录对 `BoardCamera` 再筛 `rot` / `quaternion` / `euler` = **0 命中** ⇒
//   「原断言已不存在」成立；而**生产侧是对的**（`Editor/BattleScene.cs:47771` 读清单 `d.rot`、
//   `:47770` 读 `d.pos`，与独立场景那条 `ArenaBuilder.BuildSceneTail` 的
//   `ApplyTransform(…, mf.camera.rot, …)` 同一份数据）。
//   ⇒ 缺的只是一条**钉住这个语义**的断言：相机被摆成**清单里的那个朝向**。
//
// ============================ 判据（唯一来源）============================
// 🥇 `Assets/WarpforgeArena1/arenas/<场>/<场>_manifest.json` 的 **`camera.rot`**（四元数，世界朝向）。
//    13 场逐场现读（2026-11-05）：
//      · `battlearenagenestealers` = `[-5.3e-05, 2.5e-05, 0, 1]` —— **唯一非单位四元数的一场**；
//      · 其余 12 场 = 单位四元数 `[0, 0, 0, 1]`。
//    出处：`ArenaBuilder.CameraData.rot`（`ArenaBuilder.cs` 的 `CameraData`）；
//          生产侧落点 `CardPresentation/Editor/BattleScene.cs:47771`；
//          原版值来源 = 原版场景 `Battle Arena *.unity` 的 `BoardCamera` Transform。
//
// ============================ 阈值与它的来历 ============================
// `Quaternion.Angle(a, b)` 给的是**夹角**（0–180°，天然处理 q/−q 双覆盖）⇒ 比对不看分量符号。
// 容差 **0.05°**：
//   · 13 场里离单位四元数最远的一条 = `battlearenagenestealers`
//     `[-5.3e-05, 2.5e-05, 0, 1]` ⇒ 偏角 = `2·asin(√(5.3e-5² + 2.5e-5²))` ≈ **0.0061°**
//     （= `B147` 那句「容差 ≥ 0.0061°」的来历）；
//   · 0.05° 比它宽 **8 倍**，足够吸收浮点/序列化误差，又远小于「相机被摆成别的角度」这类真回退
//     （原版那条偏角只有 0.0061° 且只出现在 x/y 两个分量上）。
//   ⚠️ 这**不是**在放宽判据：改成 `Quaternion.identity` 的写法会让 genestealers 那场红（0.0061° > 0）。
//   ⛔ 别把容差调到 0.0061° 以下 —— 那会让 genestealers 那场永远红。
//
// ============================ 怎么用（本批⛔ 不接线）============================
//   · 独立入口（**单独跑**，会**打开** `CardPresentation/Scenes/Battle.unity`）：
//       `-executeMethod B148_W4_Assert.Run`
//     ⚠️ 它 `OpenScene(Single)`；⛔ **别在别的宿主里调**（会把宿主刚建好的场景换掉）。
//   · 将来接线的入口（**只查当前打开的场景**，不换场景）：
//       `B148_W4_Assert.CheckOpenScene();`
//     接线位置建议：`BattleScene.Run` 的相机断言段（**必须在 `BuildAndSaveScene` 之后跑** ——
//     场景得先按清单重存，否则读到的还是旧值；`B147` §〇·三·4 已写死这个顺序）。
//   · 判绿红：`grep "^B148_W4 "` 的合计行；逐条按**行首** `B148_W4 ✓` / `B148_W4 ✗` 数
//     （⛔ 别 `grep -c ✗` —— `✗` 也出现在文案里）。
//
// ============================ 三条「不许静默」的硬前置 ============================
// ① 清单取不到 ⇒ **红**（⛔ 不跳过）；② `camera.rot` 长度 < 4 或四元数模长不在 1±1e-3 ⇒ **红**
// （说明清单被写坏了，拿它比对没有意义）；③ 场景里找不到 `BoardCamera` ⇒ **红**（⛔ 不当「没有就不查」）。
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>`B148-W4`：钉「BoardCamera 的 `localRotation` ≈ 清单 `camera.rot`」这一条语义。
/// ⚠️ 本批**不接线**（账 `B146-B02-A24-IDENTITY-ASSERT` 只要求「断言存在且可跑」）。</summary>
public static class B148_W4_Assert
{
    const string T = "B148_W4";

    /// <summary>战斗场景（相机就烘焙在这份场景里；`B139_S_CameraSerialized_Assert.cs` 同一份路径）。</summary>
    public const string BattleScenePath = "Assets/CardPresentation/Scenes/Battle.unity";

    /// <summary>夹角容差（度）。来历见文件头「阈值与它的来历」—— **0.0061° 是原版最大偏角，不是本阈值**。</summary>
    public const float MaxAngleDeg = 0.05f;

    /// <summary>四元数模长容差（清单被写坏时先红前置，而不是拿脏值去比）。</summary>
    public const float NormEps = 1e-3f;

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    /// <summary>独立入口：打开盘上的 `Battle.unity` 再验一次（⚠️ `OpenScene(Single)`，别在别的宿主里调）。</summary>
    public static int Run()
    {
        EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
        return CheckOpenScene();
    }

    /// <summary>只查**当前打开的场景**（将来接线的入口；不换场景、不改任何东西）。</summary>
    public static int CheckOpenScene()
    {
        Pass = 0; Fail = 0;

        // ---- 前置 ①：清单 ----
        string arena = ArenaBuilder.ArenaFromEnv();
        var mf = ArenaBuilder.LoadManifest(arena);
        Check(mf != null && mf.camera != null,
              $"前置：`{arena}` 的清单与 `camera` 段都取到（{ArenaBuilder.ManifestPath(arena)}）");
        if (mf == null || mf.camera == null) { Summary(); return Fail; }

        var rot = mf.camera.rot;
        // ---- 前置 ②：清单里的四元数本身是合法的 ----
        bool lenOk = rot != null && rot.Length >= 4;
        Check(lenOk, $"前置：清单 `camera.rot` 有 4 个分量（现读 {(rot == null ? "null" : rot.Length.ToString())}）");
        if (!lenOk) { Summary(); return Fail; }
        var want = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
        float norm = Mathf.Sqrt(want.x * want.x + want.y * want.y + want.z * want.z + want.w * want.w);
        Check(Mathf.Abs(norm - 1f) <= NormEps,
              $"前置：清单 `camera.rot` 是单位四元数（模长 {norm:F6}，容差 {NormEps}）");

        // ---- 前置 ③：场景里有 BoardCamera ----
        var sc = SceneManager.GetActiveScene();
        GameObject cam = FindBoardCamera(sc);
        Check(cam != null,
              $"前置：场景 `{sc.name}` 里找得到 `BoardCamera`（按名字含 `BoardCamera` 认；"
            + "生产侧名字 = `BoardCamera 透视（原版值）`，见 `Editor/BattleScene.cs:47745`）");
        if (cam == null) { Summary(); return Fail; }

        // ---- 被测：localRotation ≈ 清单 rot ----
        //  ⚠️ 比 `localRotation` 而不是 `rotation`：生产侧写的就是 `localRotation = 清单 rot`
        //     （`Editor/BattleScene.cs:47771`），父链上若有别的旋转，比世界朝向会把**别人的**旋转算进来。
        float ang = Quaternion.Angle(cam.transform.localRotation, want);
        Check(ang <= MaxAngleDeg,
              $"BoardCamera 的 `localRotation` ≈ 清单 `{arena}` 的 `camera.rot`"
            + $"（清单 ({rot[0]:F6}, {rot[1]:F6}, {rot[2]:F6}, {rot[3]:F6}) · "
            + $"场景 ({cam.transform.localRotation.x:F6}, {cam.transform.localRotation.y:F6}, "
            + $"{cam.transform.localRotation.z:F6}, {cam.transform.localRotation.w:F6}) · "
            + $"夹角 {ang:F4}° ≤ {MaxAngleDeg}°，来源 = `{ArenaBuilder.ManifestPath(arena)}` 的 `camera.rot`）");

        // ---- 读数（**不算失败**）：清单那条离单位四元数多远 ----
        //  作用 = 让「这条断言其实恒真」一眼可见：若某场 = 0°，说明那一场原版本来就是 identity，
        //  这条断言对它没有鉴别力（13 场里 12 场如此；genestealers 那场才是 0.0061°）。
        float fromIdentity = Quaternion.Angle(want, Quaternion.identity);
        Debug.Log($"{T} · 读数 `{arena}`：清单 `camera.rot` 与单位四元数夹角 = {fromIdentity:F4}°"
                + (fromIdentity <= 1e-4f ? "（本场原版就是 identity ⇒ 这条断言对它没有鉴别力）" : ""));

        Summary();
        return Fail;
    }

    static GameObject FindBoardCamera(Scene sc)
    {
        if (!sc.IsValid()) return null;
        foreach (var root in sc.GetRootGameObjects())
        {
            if (root == null) continue;
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                if (cam != null && cam.name != null
                    && cam.name.IndexOf("BoardCamera", System.StringComparison.Ordinal) >= 0)
                    return cam.gameObject;
        }
        return null;
    }

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log($"{T} ✓ {what}"); }
        else { Fail++; Debug.LogError($"{T} ✗ {what}"); }
    }

    static void Summary()
    {
        Debug.Log($"{T} 合计：✓ {Pass} · ✗ {Fail}"
                + (Fail == 0 ? "（全绿）" : "（**红** —— 相机朝向与清单不符，或前置不成立）"));
    }
}
