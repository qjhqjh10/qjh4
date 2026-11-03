// B124_TweenTestUtil.cs — 执行写手 `B124-W19`（2026-11-03 · 批次 `B124`）· **批处理专用的「真杀补间」口**
//
// ============================ 为什么需要它 ============================
//  判据全部来自工程自己的 `Plugins/Demigiant/DOTween/DOTween.dll`（`d:/2/tools/ilspycmd/ilspycmd.exe` 反编译现读）：
//   · `TweenExtensions.Kill(Tween t, bool complete = false)` 的**第一句**就是
//       `if (!DOTween.initialized || t == null || !t.active) return;`
//   · `DOTween.Init(...)` 只在 Play 模式干活：`if (!Application.isPlaying || isQuitting) return null;`
//     （`InitCheck()` 同款：`if (!initialized && Application.isPlaying && !isQuitting) AutoInit();`）
//   ⇒ **批处理（`Application.isPlaying == false`）里 `Kill` / `Complete` 全家都是静默空操作**。
//   · 但推进**不看** `initialized`：`DOTween.ManualUpdate` → `TweenManager.hasActiveManualTweens` →
//     `TweenManager.Update(UpdateType.Manual, …)` 照推。
//   ⇒ 补间照常被 `CardTween.Advance` 推，**却谁都杀不掉**；凡把收口写在 `OnKill` 上的代码
//     （`CardFeel.HandToBoard` / `CardFeel.ReturnToHand` / `DeploySequence` 那一族）在自检里
//     **一次都不会执行** ⇒ 一整批断言假红（`B123C3` 39 条）或假绿（`B121_Q4B` A6）。
//
//  「真的杀」= `TweenManager.Despawn(t)`：它**会**派 `onKill`
//     （`if (t.onKill != null) Tween.OnTweenCallback(t.onKill, t);`），然后 `RemoveActiveTween` +
//      回收池 + `t.active = false; t.Reset();`。
//  而 `TweenExtensions.Kill` 在不处于 update loop 时**走的正是这一句**
//     （`if (TweenManager.isUpdateLoop) t.active = false; else TweenManager.Despawn(t);`）
//  ⇒ 本辅助 = 「`Kill(false)` **去掉 `!initialized` 那道早退**」，语义逐句对齐，⛔ 不是另造一套杀法。
//
// ============================ 硬约束 ============================
//  · `DG.Tweening.Core.TweenManager` 是 `internal` ⇒ **只能反射**。拿不到 / 调不动 ⇒ **`LogError` 出声 + 返回 false**，
//    ⛔ 绝不静默跳过（铁律：不许静默失败）。
//  · **幂等**：对已 `active == false`（已死）或**不在活表里**的补间 ⇒ 返回 `false`、一个字段都不动
//    （与 `Kill` 的第一道早退同义）。连杀两次 = 第二次 false。
//  · ⛔ **只给自检用**：Play 模式下 `Kill` 本来就是好的，别拿它去替换产品代码。
//  · ⛔ 不用 `DOTween.KillAll` / `TweenManager.DespawnAll` —— 自检宿主里还挂着别人家的补间。
using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;

/// <summary>批处理（`Application.isPlaying == false`）里唯一能**真正杀掉**一条补间的口。
/// <para>等价于 `TweenExtensions.Kill(t, complete: false)`，只去掉 `!DOTween.initialized` 那道早退
/// （批处理里 DOTween 从不初始化 ⇒ 原生 `Kill` 静默空操作）。判据与全套推理见文件头。</para></summary>
public static class B124_TweenTestUtil
{
    static bool _probed;
    static MethodInfo _despawn;     // `internal static void TweenManager.Despawn(Tween t, bool modifyActiveLists = true)`
    static FieldInfo _activeField;  // `internal static Tween[] TweenManager._activeTweens`

    /// <summary>反射找口成功没有（拿不到 ⇒ 每个调用点都会 `LogError` 并返回 false）。</summary>
    public static bool Available { get { return Probe(); } }

    static bool Probe()
    {
        if (_probed) return _despawn != null && _activeField != null;
        _probed = true;
        var tm = typeof(Tween).Assembly.GetType("DG.Tweening.Core.TweenManager", false);
        if (tm == null)
        {
            Debug.LogError("[B124_TweenTestUtil] 找不到 `DG.Tweening.Core.TweenManager`（DOTween 版本/命名变动？）"
                         + " ⇒ 批处理里**杀不掉**补间，所有依赖 `OnKill` 的断言都会是空转（⛔ 没有静默跳过，这里出声）");
            return false;
        }
        _despawn = tm.GetMethod("Despawn",
                                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                                null, new[] { typeof(Tween), typeof(bool) }, null);
        _activeField = tm.GetField("_activeTweens", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (_despawn == null || _activeField == null)
        {
            Debug.LogError("[B124_TweenTestUtil] `TweenManager.Despawn(Tween,bool)` / `_activeTweens` 反射失败"
                         + "（Despawn=" + (_despawn != null) + " · _activeTweens=" + (_activeField != null) + "）"
                         + " ⇒ 批处理里**杀不掉**补间（⛔ 不静默）");
            return false;
        }
        return true;
    }

    /// <summary>活表快照（只读；用来圈出「这一段里新起的补间」）。拿不到 ⇒ 空数组 + 已经 `LogError`。</summary>
    public static Tween[] SnapshotActive()
    {
        if (!Probe()) return new Tween[0];
        var arr = _activeField.GetValue(null) as Tween[];
        if (arr == null) return new Tween[0];
        var list = new List<Tween>();
        for (int i = 0; i < arr.Length; i++) if (arr[i] != null) list.Add(arr[i]);
        return list.ToArray();
    }

    /// <summary>把 <paramref name="t"/> **真的杀掉**（派 `onKill`）。返回 true = 这一下真杀了；false = 它本来就死了
    /// （幂等档，与 `Kill` 的第一句早退同义）或反射失败（后者已 `LogError`）。</summary>
    /// <param name="where">出问题时定位用（写「哪个断言/哪一组」）。</param>
    public static bool KillInBatch(Tween t, string where)
    {
        if (t == null) return false;      // 与 `Kill` 一致：null 直接跳过
        if (!t.active) return false;      // 与 `Kill` 一致：`!t.active` 早退（幂等档，第二次杀走这里）
        if (!Probe()) return false;       // 已 LogError，⛔ 不静默成功
        int idx = IndexInActive(t);
        if (idx < 0)
        {
            // 不在活表里 = 已经 Despawn 过，或者是某个 Sequence 的**子补间**（`Sequence.DoInsert` 会
            // `RemoveActiveTween`，子补间的生命周期归父序列管，`Kill` 对它们也直接 `return`）
            // ⇒ 此时**不能**调 `Despawn(t, modifyActiveLists:true)`：`activeId` 已是 −1，会下标越界。
            Debug.LogWarning("[B124_TweenTestUtil] " + where + "：这条补间不在活表里"
                           + "（已死 / 或是 Sequence 的子补间，其生命周期归父序列）⇒ 本辅助不动它");
            return false;
        }
        try { _despawn.Invoke(null, new object[] { t, true }); }
        catch (Exception e)
        {
            Debug.LogError("[B124_TweenTestUtil] " + where + "：`TweenManager.Despawn` 抛异常 ⇒ **没杀掉**："
                         + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            return false;
        }
        if (t.active)
        {
            Debug.LogError("[B124_TweenTestUtil] " + where + "：`Despawn` 之后补间**仍然 active** ⇒ 没杀成功"
                         + "（后续推进还会跑它 ⇒ 依赖 `OnKill` 的断言会是空转）");
            return false;
        }
        return true;
    }

    /// <summary>把「快照之后新起、且现在还活着」的补间逐条真杀（= 这一组夹具自己起的那批）。
    /// <para>用于「杀动作在产品代码里」的场合（例：`CardInteraction.CancelDeployFlight` 的 `tween.Kill(false)`
    /// 在批处理里是空操作）——自检侧补同一刀，让断言验的是「序列真的死了」。返回真杀掉的条数
    /// （`−1` = 反射失败）。</para></summary>
    public static int KillNewSince(Tween[] snapshot, string where)
    {
        return KillNewSince(snapshot, -1f, where);
    }

    /// <summary>同上，但只杀**时长 ≈ <paramref name="durationSeconds"/>** 的那几条（`< 0` ⇒ 不过滤）。
    /// <para>为什么要过滤：快照之后新起的补间可能不止一条（例如起手那一下 `PickUp` 0.1 s 的位姿补间、
    /// `Relayout` 的 `_animGate` 0.2 s 延时回调）——那些**不是**要杀的对象，误杀会让后续断言读到一个
    /// 半途状态（`_animGate` 被杀 ⇒ `IsDoingAnim` 永远为真 ⇒ 后面的 `Relayout` 全被拦）。</para></summary>
    public static int KillNewSince(Tween[] snapshot, float durationSeconds, string where)
    {
        if (!Probe()) return -1;
        var old = snapshot ?? new Tween[0];
        var now = SnapshotActive();
        int n = 0;
        for (int i = 0; i < now.Length; i++)
        {
            var t = now[i];
            if (t == null) continue;
            bool wasThere = false;
            for (int j = 0; j < old.Length; j++) if (ReferenceEquals(old[j], t)) { wasThere = true; break; }
            if (wasThere) continue;
            if (durationSeconds >= 0f && Mathf.Abs(t.Duration() - durationSeconds) > 1e-2f) continue;
            if (KillInBatch(t, where)) n++;
        }
        return n;
    }

    static int IndexInActive(Tween t)
    {
        if (!Probe()) return -1;
        var arr = _activeField.GetValue(null) as Tween[];
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++) if (ReferenceEquals(arr[i], t)) return i;
        return -1;
    }
}
