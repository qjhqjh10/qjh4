// B142_E55_Assert.cs — 执行代理 `B142-E55`（账 `菜单_外壳` L660 / L661 / L664：`Core/TMProWarpText` 建好了但**零消费者**）
//
// ============================ 它盯的是什么 ============================
//  `Core/TMProWarpText.cs`（`Awake` / `OnDisable` / `Reset` / `WarpText`）早就建好，但全仓 **0 个消费者**
//  ⇒ 原版卡包 prefab 上那 34 颗 `Army Text*` 一颗都没挂弯字组件。
//  判据（2026-11-04 现读，⛔ 不是 `Reset()` 的默认值）：
//    `d:/2/tools/assetripper_out/ExportedProject/Assets/Booster Pack Standard*.prefab`
//    ＋ `Booster Pack All Armies Variant.prefab` —— **17 棵全扫、逐棵逐颗完全一致**：
//      34 颗 `Army Text*` 上的 `TMProWarpText`（`m_Script guid 4f2ffa17c2971d847634f2152c118a82`）：
//        · `m_Enabled = 1` · `axis = 1 (Z)` · `yCurveScaling = -0.09`
//        · `m_PreInfinity = m_PostInfinity = 2 (WrapMode.Loop)` · `m_RotationOrder = 4`（非四元数，不参与）
//        · 曲线两种各 17 颗：`Army Text` = **3 键** / `Army Text 2` = **5 键**（逐键字面量在本文件 A2 段）
//    🔴 与 `Reset()` 默认值（`axis = Y` · `yCurveScaling = 100` · 单峰 0.25 · `WrapMode.Once`）**差 1000 倍**。
//  我们侧改动 = `Shell/BoosterPackOpenWindow.cs` 的 `EnsureArmyTexts` 末尾 `EnsureArmyWarps(...)`（新方法在类尾）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 **接 `CardPresentation/Editor/ShopScene.cs` 的聚合行**（`=== 合计 ===` 那句**之前**、**同一物理行末尾追加**，
//     ⛔ 别加行 —— 那一条物理行下面全是账的落点）：
//      B142_E55_Assert.Run(); _sink.Pass += B142_E55_Assert.Pass; _sink.Fail += B142_E55_Assert.Fail;
//  **为什么是 ShopScene**：卡包 prefab 只在商店的「买一包 → 自动开包」那条链上被实例化
//    （`Editor/ShopScene.cs` 的 `DoBuy` / `OpenBoosterPack` 段，`B102PACK_Assert` 也接在那里）。
//  ✅ 本文件 `Run()` **自带全部夹具**（17 棵 prefab 逐个 `Instantiate` + `EnsureArmyTexts`），不依赖宿主已建窗。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  🧨 把 `EnsureArmyTexts` 末尾的 `EnsureArmyWarps(FindArmyTexts(root))` 改回 `FindArmyTexts(root)`
//     ⇒ **A1 全部 34 条 + A3 合计条红**（组件根本没挂）。
//  🧨 把 `ArmyWarpCurve` 的任一个字面量改掉 / 换成 `TMProWarpText.DefaultCurve()` ⇒ **A2 红**（曲线逐键比）。
//  🧨 把 `ArmyWarpYCurveScaling` 改回 `100f` 或把 `Axis.Z` 改成 `Axis.Y` ⇒ **A1 红**。
//  🧨 把 `EnsureArmyWarps` 改成「遍历子树里所有 TMP」⇒ **A4 反例红**（普通 TMP 不许被挂）。
//
// ============================ 覆盖边界（如实写）============================
//  · 本文件**只验「组件挂了 + 参数逐值」**。⚠️ 那两颗节点原版出厂 `m_IsActive: 0`、clip 不碰 `m_IsActive`
//    ⇒ 原版这条弯字在自己的运行时**也不上屏**；所以**没有「画面上看得到弯字」的验收面**（与原版一致）。
//  · 未跑 Unity（本代理只跑秒级 typecheck）⇒ 本文件是**待跑**状态，跑绿前账不得收 A。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B142-E55`：卡包 17 棵 prefab 的 34 颗 `Army Text*` 必须挂上 `TMProWarpText`，
/// 且 `axis = Z` · `yCurveScaling = -0.09` · 曲线 = prefab 出货值（⛔ 不是 `Reset()` 默认值）。</summary>
public static class B142_E55_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B142E55] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ── 原版出货值（prefab 逐键实读；⛔ 不引用产品代码里的常量 —— 否则两边一起改回去仍然绿）──
    static readonly float[] Curve3Time = { 0f, 0.5f, 1f };
    static readonly float[] Curve3Val = { 0.028443113f, 1f, 0f };
    static readonly float[] Curve3Tan = { 5.8882236f, -0.00016099952f, -9.286593f };
    static readonly float[] Curve5Time = { 0f, 0.06346582f, 0.12377016f, 0.5f, 1f };
    static readonly float[] Curve5Val = { -0.38427663f, 0.14532295f, 0.52559114f, 1f, 0f };
    static readonly float[] Curve5Tan = { 9.052766f, 6.603082f, 3.949648f, -0.00016099952f, -5.9425073f };

    /// <summary>唯一入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B142-E55：卡包 34 颗 `Army Text*` 全挂 `TMProWarpText`（Z 轴 · yCurveScaling −0.09 · prefab 出货曲线）");

        // ── A0 前提（硬断言；⛔ 不写 `if (前提) Check(结论)` —— 那会让前提不成立时整段静默跳过）──
        var names = BoosterPackAssets.PrefabNames;
        T(names != null && names.Length == 17,
          "（A0 前提）`BoosterPackAssets.PrefabNames` = 17 棵卡包 prefab（现读 = "
          + (names == null ? -1 : names.Length) + "）");
        if (names == null || names.Length == 0)
        {
            S("A0 前提不成立 ⇒ A1~A4 无法执行（已在上一条红）");
            return;
        }
        BoosterPackAssets.Reset();

        var roots = new List<GameObject>();
        int warped = 0;                        // 真的挂上组件、且参数逐值通过的**节点**数
        int nodes = 0;                         // 见到的 `Army Text*` 节点数（原版 17 棵 × 2 = 34）

        try
        {
            for (int p = 0; p < names.Length; p++)
            {
                string prefabName = names[p];
                GameObject prefab;
                bool got = BoosterPackAssets.TryGetPrefab(prefabName, out prefab) && prefab != null;
                T(got, "（A0 前提）prefab `" + prefabName + "` 取得回来（`BoosterPackAssets.TryGetPrefab`）");
                if (!got) continue;

                var inst = Object.Instantiate(prefab);
                inst.name = "PackFixture " + p;
                roots.Add(inst);

                // 产品路径同款：找到那两颗节点（含 inactive）⇒ 缺 TMP 就补建 ⇒ 挂 `TMProWarpText`
                var texts = BoosterPackOpenWindow.EnsureArmyTextsForTest(inst.transform);
                int got2 = texts == null ? 0 : texts.Length;
                T(got2 == 2, "P" + p + " `" + prefabName + "`：`Army Text*` 节点 = **2** 颗（现读 " + got2 + "）");

                for (int i = 0; i < got2; i++)
                {
                    var t = texts[i];
                    if (t == null) { T(false, "P" + p + " [`" + i + "]` 是 null TMP"); continue; }
                    nodes++;
                    string label = "P" + p + " `" + prefabName + "` / `" + t.gameObject.name + "`";
                    // ⚠️ 只有 `Army Text` / `Army Text 2` 两个名字（17 棵实读）；其余名字 = 判据变了，当场红
                    bool second = t.gameObject.name == "Army Text 2";
                    T(t.gameObject.name == "Army Text" || second,
                      label + "：节点名只允许 `Army Text` / `Army Text 2`（曲线按名字分档）");
                    if (CheckWarp(t, second, label)) warped++;
                }

                // ── A4 反例：普通 TMP（名字不是 `Army Text*`）**不许**被挂组件、也不许被收进返回值 ──
                if (p == 0)
                {
                    var plainGo = new GameObject("Plain Text");
                    plainGo.transform.SetParent(inst.transform, false);
                    var plain = plainGo.AddComponent<TMPro.TextMeshPro>();
                    var texts2 = BoosterPackOpenWindow.EnsureArmyTextsForTest(inst.transform);
                    T(plain.GetComponent<TMProWarpText>() == null,
                      "★ A4 反例：普通 TMP（名字 `Plain Text`）**没有**被挂 `TMProWarpText`（⛔ 别改成遍历所有 TMP）");
                    bool picked = false;
                    if (texts2 != null) for (int i = 0; i < texts2.Length; i++) if (texts2[i] == plain) picked = true;
                    T(!picked, "★ A4 反例：`EnsureArmyTexts` **不**把普通 TMP 收进返回值（现读 "
                      + (texts2 == null ? 0 : texts2.Length) + " 颗）");
                    T(texts2 != null && texts2.Length == 2,
                      "★ A4 反例：加了一颗普通 TMP 之后返回值仍是 **2** 颗（现读 "
                      + (texts2 == null ? 0 : texts2.Length) + "）");
                }
            }
        }
        finally
        {
            for (int i = 0; i < roots.Count; i++)
                if (roots[i] != null) Object.DestroyImmediate(roots[i]);
        }

        // ── A3 合计：34 颗（17 棵 × 2），一颗都不能少 ──
        T(nodes == 34, "★ A3 合计：17 棵 prefab 上 `Army Text*` 节点共 **34** 颗（现读 " + nodes + "）");
        T(warped == 34, "★ A3 合计：**34 颗全部**挂上 `TMProWarpText` 且参数逐值通过（现读 " + warped + "）");

        S("小结：✓ " + _sink.Pass + " · ✗ " + _sink.Fail);
    }

    /// <summary>逐值比一颗节点上的 `TMProWarpText`（组件 / `axis` / `yCurveScaling` / 曲线逐键 / wrapMode）。</summary>
    static bool CheckWarp(TMPro.TextMeshPro t, bool second, string label)
    {
        var w = t.GetComponent<TMProWarpText>();
        bool has = w != null && w.enabled && ReferenceEquals(w.text, t);
        T(has, "★ A1 " + label + "：挂着 `TMProWarpText`、`enabled`、且 `text` 指向这颗 TMP");
        if (!has) return false;

        bool axisOk = w.axis == TMProWarpText.Axis.Z;                       // prefab `axis: 1`
        bool scaleOk = Mathf.Abs(w.yCurveScaling - (-0.09f)) < 1e-6f;       // prefab `yCurveScaling: -0.09`
        T(axisOk && scaleOk, "★ A1 " + label + "：`axis = Z` · `yCurveScaling = -0.09`（现读 axis=" + w.axis
          + " · scaling=" + w.yCurveScaling + "）");

        // ── 曲线逐键（⛔ 不复用产品代码的 `ArmyWarpCurve` —— 期望值在这里写死）──
        var c = w.vertexCurve;
        int want = second ? 5 : 3;
        string detail = "曲线 null";
        bool ok = false;
        if (c != null)
        {
            var ks = c.keys;
            detail = "n=" + (ks == null ? -1 : ks.Length)
                     + " · pre/post=" + c.preWrapMode + "/" + c.postWrapMode;
            ok = ks != null && ks.Length == want
                 && c.preWrapMode == WrapMode.Loop && c.postWrapMode == WrapMode.Loop;
            if (ok)
            {
                var wt = second ? Curve5Time : Curve3Time;
                var wv = second ? Curve5Val : Curve3Val;
                var wtan = second ? Curve5Tan : Curve3Tan;
                for (int i = 0; i < want && ok; i++)
                {
                    ok = Mathf.Abs(ks[i].time - wt[i]) < 1e-5f
                         && Mathf.Abs(ks[i].value - wv[i]) < 1e-5f
                         && Mathf.Abs(ks[i].inTangent - wtan[i]) < 1e-4f
                         && Mathf.Abs(ks[i].outTangent - wtan[i]) < 1e-4f;
                    if (!ok) detail += " · 键" + i + " 不符(t=" + ks[i].time + " v=" + ks[i].value + ")";
                }
            }
        }
        T(ok, "★ A2 " + label + "：曲线 = prefab 出货值（" + (second ? "5 键" : "3 键")
          + " · `WrapMode.Loop` · 逐键 time/value/inTangent/outTangent）（现读 " + detail + "）");
        return true;
    }
}
