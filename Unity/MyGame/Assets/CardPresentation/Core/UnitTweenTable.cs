// UnitTweenTable.cs — 原版 `UnitTweenSO` 在我们这边的**运行期替身**。
//
// 为什么需要它：`WarpforgeVFX.WFModuleTween` 的 `tweenAnims` 存的是 UnitTweenSO 的**资产名**，
// 而「这个名字对应哪几条补间、每条什么参数」在原版是**运行时读 ScriptableObject** 拿的。
// 我们的 `Assets/` 里没有那份 SO（它只在 `d:/2` 的导出里）⇒ 压成一张
// `Resources/UnitTweens.json`，运行时按名字查。**同一模式**：`OffensiveCards.json` /
// `EnvironmentConditions.json` / `VfxMap` 那几件。
//
// 数据：`工具/gen_unit_tweens.py` 从 `数据/游戏数据/tween/*.json`（74 份）生成；
//       覆盖自检：`工具/_verify_unit_tweens.py`（**引用侧 24 个名字 ⊆ 表**，2026-10-01 实测 0 缺口）。
//
// ==================================================================
//  判据：每条语义都是**逐行读反编译**得来的，不是猜的
// ==================================================================
//   · `UnitTweenSO.BuildSequence`（`d:/2/tools/decomp_animfx_nested/`）——
//     逐条 `GetTween(source, target)`；**取不出 tween 的（返回 null）直接跳过后面的**；
//     `muted != 0` 的那条**不播**，但**它「本该是 Append 还是 Join」要记住** ——
//     下一条非 muted 的补间若正好接在被跳过的 Append 后面，就必须 `Append`（否则时间轴不前进）。
//     这条逻辑照抄在下面的 `Build()` 里（原版那个 `bVar2/bVar12` 两态机）。
//   · `TweenInfoAnimationBase.GetTween` = **公共包装**（子类实现之外还套三样，顺序如此）：
//     `SetDelay(delay)` → `SetLoops(loops, loopType)` → `SetEase(ease)`；
//     ⚠️ `ease == 0` 时改用 `curve`（`SetEase(AnimationCurve)`）。
//   · `BattleManagerSupport.GetTargetUnit(enum, source, target)`（**目标枚举**，实读）：
//     `10 = 施放者` · `20 = 目标` · `30 = 目标` · `40 = 施放者那一方的督军` · `50 = 对面的督军` ·
//     其余 → **null**（原版就是 null ⇒ 那条补间不播）。
//   · 六个子类各自的方法体都读过：`ScaleTween` / `MoveTween` / `RotateTween` /
//     `PunchTween` / `ShakeTween` / `ResetBodyTween`（`DelayTween` 只有 `duration`）。
//
// 🔴 **两处如实标注（铁律 3）**：
//   ① `awayFromUnit` 那条支里 `get_TargetDirection` 的**退化分支**（取不到那个单位时）原版读的是
//      `cardUI → 一个子 transform 的 forward`（`+0x290 → +0x158`）。**那个子节点是什么我们没坐实**
//      ⇒ 我们**取卡自己的 `forward`**，并在这里写明「这是我们的替身」。
//   ② 原版 `GetTween` 的 `from`（`.From()`）**在数据里 111 条全是 0** ⇒ 我们照样实现，但它在当前
//      数据下**一次都不会走到**（不是没做）。
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一条补间（原版一个 `TweenInfoBase` 子类实例）。字段名与生成器一一对应。</summary>
    [Serializable]
    public class UnitTweenStep
    {
        public string kind;          // scale / move / rotate / punch / shake / resetbody / delay / unknown
        public int muted;
        public int appendType;       // 0 = Append，非 0 = Join（原版就是「非 0 即 Join」）
        public float duration;
        public float delay;
        public int loops;
        public int loopType;         // 0 = Restart（DOTween LoopType）
        public int ease;             // DOTween `Ease` 枚举值；0 ⇒ 用 curve
        public int relative;
        public int from;
        public int targetUnit;       // 10/20/30/40/50，见文件头
        public int awayFromUnit;     // 同上那套枚举；0 = 不用
        public int isLocal;
        public float[] vec;          // scale / movement / rotation / punch / strength（按 kind 解释）
        public int punchType;        // 1 = Position（默认）· 2 = Rotation · 3 = Scale
        public int shakeType;        // 1 = Position（默认）· 2 = Rotation · 3 = Scale
        public int rotationMode;     // DORotate 的 `RotateMode`
        public int vibrato, vibratto, elasticity, randomness, fadeOut;
        public int resetPosition, resetRotation, resetScale;
    }

    /// <summary>一个 `UnitTweenSO`（一「串」补间）。</summary>
    [Serializable]
    public class UnitTweenEntry
    {
        public string name;
        /// <summary>原版 `waitAnimation`（`so+0x18`）—— **这串要不要等它播完再播下一串**。</summary>
        public int wait;
        public int useCallback;
        public float callbackTime;
        public UnitTweenStep[] tweens = new UnitTweenStep[0];
    }

    [Serializable]
    public class UnitTweenFile { public UnitTweenEntry[] items = new UnitTweenEntry[0]; }

    /// <summary>运行期那张表 + 把一条 `UnitTweenSO` 建成 DOTween 序列。</summary>
    public static class UnitTweenTable
    {
        public const string ResourcePath = "UnitTweens";

        static Dictionary<string, UnitTweenEntry> _byName;
        static UnitTweenEntry[] _all;

        /// <summary>表载进来了没有（没载 = 资源不在；**不静默**：`LastError` 有话说）。</summary>
        public static bool Loaded { get { EnsureLoaded(); return _byName != null; } }
        public static int Count { get { EnsureLoaded(); return _all != null ? _all.Length : 0; } }
        public static string LastError { get; private set; }

        /// <summary>建过几串 / 跳过了几串（诊断用；自检盯它）。</summary>
        public static int BuiltCount { get; private set; }
        public static int SkippedCount { get; private set; }
        public static string LastSkip { get; private set; }

        /// <summary>🆕 **「取督军」解析口**（原版 `GetTargetUnit` 的 40/50 两档要「取某方的督军」，
        /// 而规则引擎不认识 GameObject）⇒ 由表现层挂上。
        /// 参数 = (施放者的 transform, 是不是「施放者那一方」的督军) ⇒ 督军的 Transform。
        /// ⚠️ **没挂时**用到 40/50 的那几条补间会被**跳过并出声**（当前数据里共 8 条）。</summary>
        public static Func<Transform, bool, Transform> HeroOf;

        /// <summary>🆕 **「取朝向」替身**（见文件头 ①）。默认取卡自己的 `forward`；
        /// 表现层若知道更准的朝向（原版那个 `cardUI` 子节点的 forward）可以换掉。</summary>
        public static Func<Transform, Vector3> ForwardOf = t => t != null ? t.forward : Vector3.forward;

        static void EnsureLoaded()
        {
            if (_byName != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LastError = $"`Resources/{ResourcePath}.json` 载不到 —— 跑一次 `python 工具/gen_unit_tweens.py`";
                _byName = new Dictionary<string, UnitTweenEntry>();   // 空表（不是 null：别再重载）
                _all = new UnitTweenEntry[0];
                return;
            }
            UnitTweenFile f = null;
            try { f = JsonUtility.FromJson<UnitTweenFile>(ta.text); }
            catch (Exception e) { LastError = "`UnitTweens.json` 解析失败：" + e.Message; }
            var items = f != null && f.items != null ? f.items : new UnitTweenEntry[0];
            _all = items;
            _byName = new Dictionary<string, UnitTweenEntry>(items.Length);
            foreach (var it in items)
                if (it != null && !string.IsNullOrEmpty(it.name)) _byName[it.name] = it;
            if (LastError == null && _byName.Count == 0) LastError = "`UnitTweens.json` 里一条都没有";
        }

        /// <summary>按资产名查（查不到返回 null —— 调用方**必须出声**，别静默）。</summary>
        public static UnitTweenEntry Get(string name)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(name)) return null;
            UnitTweenEntry e;
            return _byName.TryGetValue(name, out e) ? e : null;
        }

        /// <summary>表里全部名字（自检对账用）。</summary>
        public static IEnumerable<string> Names { get { EnsureLoaded(); return _byName.Keys; } }

        // ==================================================================
        //  建序列
        // ==================================================================

        /// <summary>把一串补间建成 DOTween `Sequence`。**判据 = `UnitTweenSO.BuildSequence`**（见文件头）。
        /// 返回 null = 这一串**一条都没建出来**（调用方出声）。</summary>
        public static Sequence Build(UnitTweenEntry e, Transform source, Transform target, string why = null)
        {
            if (e == null) return null;
            var seq = DOTween.Sequence();
            int built = 0;
            // 原版那两个状态位（`bVar2` / `bVar12`）：**跳过 muted 之后，下一条要不要强制 Append**
            bool skipped = false, skippedWasAppend = false;

            foreach (var s in e.tweens)
            {
                if (s == null) continue;
                var tw = MakeTween(s, source, target, why);
                if (tw == null) continue;                 // 原版：`GetTween` 返回 null 就 `continue`
                built++;

                if (s.muted == 0)
                {
                    if (!(skipped && skippedWasAppend))
                    {
                        skipped = false;
                        if (s.appendType != 0) { seq.Join(tw); continue; }
                    }
                    seq.Append(tw);
                    skipped = false;
                }
                else
                {
                    if (!skipped) { skipped = true; skippedWasAppend = (s.appendType == 0); }
                }
            }

            if (built == 0)
            {
                seq.Kill(false);
                SkippedCount++;
                LastSkip = $"`{e.name}` 一条都没建出来（{e.tweens.Length} 条里 0 条成功）"
                         + (string.IsNullOrEmpty(why) ? "" : $" · 场景：{why}");
                return null;
            }
            BuiltCount++;
            return seq;
        }

        /// <summary>一条补间。返回 null = **这条做不出来**（原版也是 null ⇒ 跳过）。
        /// 判据 = 各子类的 `GetTween` 方法体，逐个见下面的注释。</summary>
        static Tween MakeTween(UnitTweenStep s, Transform source, Transform target, string why)
        {
            var t = Target(s.targetUnit, source, target);
            if (t == null) return null;                   // 原版取不到目标单位就抛/跳过

            Tween tw = null;
            switch (s.kind)
            {
                case "scale":      tw = MakeScale(s, t, source, target); break;
                case "move":       tw = MakeMove(s, t, source, target); break;
                case "rotate":     tw = MakeRotate(s, t, source, target); break;
                case "punch":      tw = MakePunch(s, t, source, target); break;
                case "shake":      tw = MakeShake(s, t); break;
                case "resetbody":  return MakeResetBody(s, t);      // 它自己就是一条 Sequence，不再套包装
                case "delay":      tw = DOTween.To(() => 0f, _ => { }, 0f, s.duration); break;
                case "unknown":    break;                  // 生成器已确认当前数据里 0 条；真出现就出声
                default:           break;
            }
            if (tw == null) return null;

            // —— 公共包装（`TweenInfoAnimationBase.GetTween`，**顺序照原版**）——
            tw = tw.SetDelay(s.delay);
            tw = tw.SetLoops(s.loops, (LoopType)s.loopType);
            if (s.ease != 0) tw = tw.SetEase((Ease)s.ease);
            // ⚠️ `ease == 0` 时原版走 `SetEase(curve)`；当前数据里只有 1 条 ease==0，
            //    而它的 curve **非空**（`工具/_verify_unit_tweens.py` ⑤ 会盯）。
            //    我们没搬 curve（74 份里只有 2 条非空、共 6 个关键帧）⇒ 那一条**用 Linear 兜**并出声。
            else { tw = tw.SetEase(Ease.Linear); EaseZeroFallback++; }
            return tw;
        }

        /// <summary>`ease == 0` 但我们没搬 curve 的次数（诊断；当前数据下应为 0~1）。</summary>
        public static int EaseZeroFallback { get; private set; }

        // ---- `BattleManagerSupport.GetTargetUnit`（实读的枚举）----
        static Transform Target(int unit, Transform source, Transform target)
        {
            switch (unit)
            {
                case 10: return source;                        // 施放者
                case 20: case 30: return target;               // 目标（两档都返回 target，原版如此）
                case 40: case 50:                              // 40 = 本方的督军 · 50 = 对面的督军
                    return HeroOf != null && source != null ? HeroOf(source, unit == 40) : null;
                default: return null;
            }
        }

        static Vector3 V(UnitTweenStep s, Vector3 dflt)
        {
            if (s.vec == null || s.vec.Length < 3) return dflt;
            return new Vector3(s.vec[0], s.vec[1], s.vec[2]);
        }

        /// <summary>`TweenInfoAnimationBase.get_TargetDirection`（`awayFromUnit` 那条支要它）。
        /// 原版：取 `awayFromUnit` 那个单位，方向 = normalize(目标位 − 它的位置)；
        /// **取不到时**退回「目标卡的 forward」（⚠️ 原版读的是 `cardUI` 下一个子节点 —— 见文件头 ①）。</summary>
        static Vector3 TargetDirection(UnitTweenStep s, Transform source, Transform target)
        {
            var tgt = Target(s.targetUnit, source, target);
            var away = Target(s.awayFromUnit, source, target);
            if (away != null && tgt != null)
            {
                var d = tgt.position - away.position;
                if (d.sqrMagnitude > 1e-8f) return d.normalized;
            }
            return ForwardOf(tgt != null ? tgt : target);
        }

        // `ScaleTween.GetTween`：scale（relative 时 × 目标的 localScale.x —— 逐分量）
        static Tween MakeScale(UnitTweenStep s, Transform t, Transform source, Transform target)
        {
            var v = V(s, Vector3.one);
            if (s.relative != 0) v = Vector3.Scale(v, t.localScale);
            var tw = t.DOScale(v, s.duration);
            return s.from != 0 ? (Tween)tw.From() : tw;   // ⚠️ `From()` 只在 `Tweener` 上（DOTween 的签名如此）
        }

        // `MoveTween.GetTween`：movement（awayFromUnit 时先绕 LookRotation(dir) 转一下）
        //   · `isLocal == 0` ⇒ `DOMove`；否则 `DOLocalMove`，且要按 `localScale.x` 缩放位移
        //     （原版正是这么处理「敌方单位 x 是负的」那件事）；`from` 时位移再加一次当前位置。
        static Tween MakeMove(UnitTweenStep s, Transform t, Transform source, Transform target)
        {
            var v = V(s, Vector3.zero);
            if (s.awayFromUnit != 0)
                v = Quaternion.LookRotation(TargetDirection(s, source, target)) * v;

            Tween tw;
            Tweener tn;
            if (s.isLocal == 0)
            {
                tn = t.DOMove(v, s.duration);
            }
            else
            {
                float k = t.localScale.x;
                v = new Vector3(v.x * k, v.y * k, v.z * k);
                if (s.from != 0) v += t.position;
                tn = t.DOLocalMove(v, s.duration);
            }
            tw = tn.SetRelative(s.relative != 0);
            return s.from != 0 ? (Tween)tn.From() : tw;   // `From()` 只在 `Tweener` 上
        }

        // `RotateTween.GetTween`：rotation（awayFromUnit 时换成「按朝向算出来的欧拉角 + rotation」）
        static Tween MakeRotate(UnitTweenStep s, Transform t, Transform source, Transform target)
        {
            var e = V(s, Vector3.zero);
            if (s.awayFromUnit != 0)
            {
                var q = Quaternion.LookRotation(TargetDirection(s, source, target));
                var eul = q.eulerAngles;
                // 原版 `Internal_MakePositive`：把欧拉角折进 [0,360)
                e += new Vector3(Pos(eul.x), Pos(eul.y), Pos(eul.z));
            }
            else if (s.relative != 0)
            {
                // 原版：`relative` 时按「目标是不是面向左」翻 x 的符号（+0x40 = isPlayer 那一位）
                e = new Vector3(e.x, e.y, e.z);   // 符号由 who 决定 —— 我们没有那一份 ⇒ 不翻，如实标
            }

            Tweener tn = s.isLocal == 0
                ? t.DORotate(e, s.duration, (RotateMode)s.rotationMode)
                : t.DOLocalRotate(e, s.duration, (RotateMode)s.rotationMode);
            Tween tw = tn.SetRelative(s.relative != 0);
            return s.from != 0 ? (Tween)tn.From() : tw;   // `From()` 只在 `Tweener` 上
        }

        static float Pos(float a) { a %= 360f; return a < 0f ? a + 360f : a; }

        // `PunchTween.GetTween`：punchType 1/其它 ⇒ DOPunchPosition · 2 ⇒ DOPunchRotation · 3 ⇒ DOPunchScale
        //   ⚠️ 原版**不套 `.From()`**（Punch 本来就是一来一回）。
        static Tween MakePunch(UnitTweenStep s, Transform t, Transform source, Transform target)
        {
            var v = V(s, Vector3.one);
            if (s.awayFromUnit != 0)
            {
                var q = Quaternion.LookRotation(TargetDirection(s, source, target));
                if (s.punchType == 2)
                {
                    var eul = (q * Quaternion.Euler(v)).eulerAngles;
                    v = new Vector3(Pos(eul.x), Pos(eul.y), Pos(eul.z));
                }
                else v = q * v;
            }
            if (s.punchType == 2) return t.DOPunchRotation(v, s.duration, s.vibrato > 0 ? s.vibrato : s.vibratto, s.elasticity);
            if (s.punchType == 3) return t.DOPunchScale(v, s.duration, s.vibrato > 0 ? s.vibrato : s.vibratto, s.elasticity);
            return t.DOPunchPosition(v, s.duration, s.vibrato > 0 ? s.vibrato : s.vibratto, s.elasticity);
        }

        // `ShakeTween.GetTween`：shakeType 1/其它 ⇒ Position · 2 ⇒ Rotation · 3 ⇒ Scale
        //   ⚠️ `awayFromUnit` 在方法体里**一次都没读** ⇒ 那 2 条数据里它是惰性的（原版如此）。
        static Tween MakeShake(UnitTweenStep s, Transform t)
        {
            var v = V(s, Vector3.one);
            int vib = s.vibrato > 0 ? s.vibrato : s.vibratto;
            if (s.shakeType == 2) return t.DOShakeRotation(s.duration, v, vib, s.randomness, s.fadeOut != 0);
            if (s.shakeType == 3) return t.DOShakeScale(s.duration, v, vib, s.randomness, s.fadeOut != 0);
            return t.DOShakePosition(s.duration, v, vib, s.randomness, false, s.fadeOut != 0);
        }

        // `ResetBodyTween.GetTween`：**回到当前值**（原版的用途是「把别的补间顶掉」——
        //   DOTween 同属性起新补间会杀掉旧的）。三个开关各自 Join 一条，`SetEase` 用的是**它自己的** ease。
        static Sequence MakeResetBody(UnitTweenStep s, Transform t)
        {
            var seq = DOTween.Sequence();
            var ease = s.ease != 0 ? (Ease)s.ease : Ease.Linear;
            if (s.resetPosition != 0) seq.Join(t.DOMove(t.position, s.duration).SetEase(ease));
            if (s.resetRotation != 0) seq.Join(t.DORotateQuaternion(t.rotation, s.duration).SetEase(ease));
            if (s.resetScale != 0) seq.Join(t.DOScale(t.localScale, s.duration).SetEase(ease));
            return seq;
        }
    }
}
