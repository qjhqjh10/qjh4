// B94V1_Assert.cs — 执行代理 V1（第九十四会话 · 波 1.5）两件活的自检。
//
// ============================ 它盯的是哪两行账 ============================
//  · **`其他/基础设施:292`** — `LoadableReference.GetReferenceObjEditor()` → **A**
//    落点 = `Core/LoadableRef.cs` 的 `GenericObjectReference`（getter 判型拆箱 / setter 写 `reference`）。
//  · **`未分类/其他:689`** — `UIMessageController.OnDestroy()` → **A**
//    落点 = `Battle/ErrorMessageBanner.cs` 类尾那一段（`Instance` / `Awake` / `OnDestroy` / `EnsureSingleton`）。
//  逐条的判据、做法、为什么这么裁 → `资料/完全复刻/报告/B94_V1.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94V1_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；建出来的临时节点在收尾 `DestroyImmediate`。
//   · 两个宿主都合适（`Editor/BattleScene.cs` = 横幅的主场；`Editor/ShellScene.cs` 也见过 `MessageToast`）。
//     ⚠️ 本文件**还没接到宿主里**（文件所有权只到我这三个文件）⇒ 由调度台在宿主 `Run()` 里加两行：
//       `B94V1_Assert.Run();`
//       `_sink.Pass += B94V1_Assert.Pass; _sink.Fail += B94V1_Assert.Fail;`
//
// ============================ 🔴 灭自证（每条都把「改哪一处会红」写进文案）============================
//   · A2/A3 两态：把 getter 改回「直接返回 `genericObjectReference`」⇒ **A2 红**（那个字段本仓 0 写入者）；
//     把控件的 `as` 去掉（无脑返回 `reference`）⇒ **A3 红**。
//   · A4/A5 两态：把 setter 改回写 `genericObjectReference` ⇒ **A4 红**（写进去读不出来）。
//   · B3/B4：把 `EnsureSingleton` 的 `Destroy` 那一支删掉 ⇒ **B3/B4 红**；写成「恒返回 false」⇒ **B1 红**。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 V1：`LoadableRef.GenericObjectReference`（账 `其他:292`）＋ `ErrorMessageBanner` 单例
/// （账 `未分类:689`）的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量 / 引用相等**，**不从被测实现里读**（否则就是「拿我们的实现证明我们的实现」）。
/// 「同场景只剩一颗」那一条用**数出来的**（`GetComponentsInChildren`），也不是读实现里的计数器。</para></summary>
public static class B94V1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94V1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_V1：`LoadableRef.GenericObjectReference`（其他:292）＋ `ErrorMessageBanner` 单例（未分类:689）");

        SectionA_LoadableRef();
        SectionB_ErrorMessageBanner单例();

        Debug.Log(_sink.P + "B94_V1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A `GenericObjectReference`
    /// <summary>夹具：`LoadableRef&lt;T&gt;` 只要 `T : class`；这一格专门用来演「**不是** `UnityEngine.Object`」那一档。</summary>
    sealed class Plain { public string Id; public Plain(string id) { Id = id; } }

    static void SectionA_LoadableRef()
    {
        MenuCheck.Section(_sink, "A `GenericObjectReference` = 原版 `Get/SetReferenceObjEditor`（纯类型判定 + 拆箱，**无** `AssetDatabase`）");

        // ---- 前提（硬检查）：`reference == null` ⇒ null（原版 getter 首句）
        var empty = new LoadableRef<GameObject>();
        Tr(empty.GenericObjectReference == null, "A1 前提：`reference == null` ⇒ **null**（原版 `GetReferenceObjEditor` 首句）");

        var go = new GameObject("B94V1_RefTarget");
        try
        {
            // ---- 两态 ① `T` 就是 `UnityEngine.Object` ⇒ 交回**同一个**对象
            var asObj = new LoadableRef<GameObject>(go);          // ctor ⇒ SetReferenceByObject ⇒ reference = go
            Tr(ReferenceEquals(asObj.GenericObjectReference, go),
                "A2 ★ `T = GameObject`（可赋给 `UnityEngine.Object`）⇒ getter 交回**同一个**对象"
                + "（判据 = `…___GetReferenceObjEditor__101ab70.c`：判箱后类可赋 ⇒ 返回它）");

            // ---- 两态 ② `T` 不是 `UnityEngine.Object` ⇒ 一律 null
            var plain = new LoadableRef<Plain>(new Plain("p1"));
            Tr(plain.Reference != null, "A3 前提：`LoadableRef<Plain>` 的 `reference` 确实非空（否则下面那条是空转）");
            Tr(plain.GenericObjectReference == null,
                "A4 ★ `T = Plain`（普通 class、**不是** `UnityEngine.Object`）⇒ **null**（与 A2 两态对照）"
                + " —— 若有人把 getter 改回「直接返回 `genericObjectReference`」，A2 会红（那个字段本仓 0 写入者）");

            // ---- setter = 原版 `SetReferenceObjEditor`：判型可赋给 `T` ⇒ 写 `reference`（+0x20）
            var tgt = new LoadableRef<GameObject>();
            tgt.GenericObjectReference = go;
            Tr(ReferenceEquals(tgt.GenericObjectReference, go),
                "A5 ★ setter 判型通过 ⇒ 写进 `reference`（判据 = `…___SetReferenceObjEditor__101b6c0.c` 写 `+0x20`；不是 `+0x10`）");

            // ---- setter 判型不过 ⇒ **不写**（+出声），⛔ 不静默塞进去
            var tgt2 = new LoadableRef<Plain>();
            tgt2.GenericObjectReference = go;                      // GameObject 不能赋给 Plain
            Tr(tgt2.GenericObjectReference == null,
                "A6 ★ setter 判型不过 ⇒ **不写**（`reference` 仍是 null）、并出声（原版同一支 `LogError`）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ B `ErrorMessageBanner` 单例
    static void SectionB_ErrorMessageBanner单例()
    {
        MenuCheck.Section(_sink, "B `ErrorMessageBanner` 单例（原版 `UIMessageController.Awake` / `OnDestroy`；作用域 = 场景）");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        int before = CountInScene(scene);
        var root = new GameObject("B94V1_SingletonRoot");
        try
        {
            // ---- 前提（硬检查）：工厂必须交回一颗**活的**（⛔ 不许交回已销毁的引用 —— 调用方拿它 ShowError 会抛）
            var a = ErrorMessageBanner.Create(root.transform, ErrorMessageBanner.BattlePreset);
            Tr(a != null, "B1 前提：`Create` 交回的横幅**非空且活着**（建前这一幕里已有 " + before + " 颗）");
            if (a == null) return;                                  // 前提不成立 ⇒ 不往下空转

            Tr(ReferenceEquals(ErrorMessageBanner.Instance, a),
                "B2 `Instance` = 刚领取的那一颗（原版 `Awake` 的 `Instance = this` 那一支）");

            // ---- ★ 原版 `Awake`：`Instance != null && Instance != this ⇒ Destroy(this.gameObject)`
            var b = ErrorMessageBanner.Create(root.transform, ErrorMessageBanner.BattlePreset);
            Tr(b != null && ReferenceEquals(b, a),
                "B3 ★ 同场景重复建 ⇒ 新那颗被销毁（原版 `Destroy(gameObject)` 那一支）、工厂交回**活着的那一颗**"
                + "（⛔ 不是 null、也不是已销毁的引用）");

            C(CountInScene(scene), before == 0 ? 1 : before,
                "B4 ★ 数出来的：这一幕里**没有多出第二颗**横幅（建前 " + before + " 颗 ⇒ 建两次后仍是这个数）");

            // ---- 灭自证：上面几条不是「两颗都没建出来」的空转绿
            Tr(!ReferenceEquals(ErrorMessageBanner.Instance, null),
                "B5 `Instance` 不是 null（B3/B4 的绿不是「什么都没建」造成的）");
        }
        finally
        {
            // 拆掉临时根（挂在它下面的横幅一起没）；`Instance` 若已是 shell 里那颗则不受影响
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>这一幕（`scene`）里现存的横幅个数（**含 inactive**）。
    /// ⚠️ 按**场景**数、不是全进程 —— 壳是 `DontDestroyOnLoad`，它那颗会活在别的场景里，不能算进来。</summary>
    static int CountInScene(UnityEngine.SceneManagement.Scene scene)
    {
        int n = 0;
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i] != null) n += roots[i].GetComponentsInChildren<ErrorMessageBanner>(true).Length;
        return n;
    }
}
