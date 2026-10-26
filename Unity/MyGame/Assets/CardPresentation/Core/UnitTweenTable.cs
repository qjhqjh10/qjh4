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
//   ① ✅ **2026-10-22（W64 · `B10`）坐实了**：`awayFromUnit` 那条支里 `get_TargetDirection` 的
//      **退化分支**读的是 `CardScript +0x290`（= `CardUI`）的 `+0x158` 子节点，而
//      `il2cpp_out/dump.cs:26166-26173`（`public class BattleCardUI : BasicCardUI`）实读：
//      **`+0x158 = public GameObject body3D;`** ⇒ 原版取的是**场上那个 3D 卡体节点的 forward**。
//      我们照它：`DefaultForward` 先找卡下的 `body3D` 子节点、取它的 forward，没有才退回卡自己的
//      forward（手牌卡没有 3D 体，这一档在原版也取不到 —— 原版那条路只对场上单位有意义）。
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
        // 🆕 2026-10-22（W64 · `B09`）：原版 `ease == 0` 时 `SetEase(AnimationCurve)` 的那条曲线。
        // 四条平行数组（`JsonUtility` 认不了 `Keyframe` 这种结构）。**生成器搬过来之前是空的**
        // ⇒ 运行期退回 `UnitTweenTable.CurveFallback`（见那边的注释）。
        public float[] curveTimes;
        public float[] curveValues;
        public float[] curveInSlopes;
        public float[] curveOutSlopes;
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
        /// <summary>🆕 2026-10-25（`P6`）：`useCallback` 那条 `InsertCallback` 插过几次（诊断用）。
        /// ⚠️ 当前数据下应为 **0**（74 份 SO 的 `useCallBack` 全是 0）—— 变正数说明换了一批数据。</summary>
        public static int CallbackInserted { get; private set; }
        public static string LastSkip { get; private set; }

        /// <summary>🆕 **「取督军」解析口**（原版 `GetTargetUnit` 的 40/50 两档要「取某方的督军」，
        /// 而规则引擎不认识 GameObject）⇒ 由表现层挂上。
        /// 参数 = (施放者的 transform, 是不是「施放者那一方」的督军) ⇒ 督军的 Transform。
        /// ⚠️ **没挂时**用到 40/50 的那几条补间会被**跳过并出声**（当前数据里共 8 条）。</summary>
        public static Func<Transform, bool, Transform> HeroOf;

        /// <summary>**「取朝向」口**（原版 `get_TargetDirection` 的退化分支，见文件头 ①）。
        /// 🔴 2026-10-22（`B10`）：默认值 = <see cref="DefaultForward"/>（**卡下 `body3D` 节点的 forward**，
        /// 判据 = `BattleCardUI +0x158 = body3D`）。原来默认取卡自己的 forward —— 那是**替身**，已换。
        /// 表现层若要换实现仍可赋值。</summary>
        public static Func<Transform, Vector3> ForwardOf = DefaultForward;

        /// <summary>原版 `cardUI(+0x290) → body3D(+0x158)` 的 forward。
        /// `body3D` 是我们 `CardView.BuildBody3D` 建的**直接子节点**（名字就是 `body3D`）。
        /// 没有它（手牌/非场上形态）⇒ 退回卡自己的 forward（原版那条路也只对场上单位有意义）。</summary>
        static Vector3 DefaultForward(Transform card)
        {
            if (card == null) return Vector3.forward;
            var body = card.Find("body3D");
            return body != null ? body.forward : card.forward;
        }

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
        /// 返回 null = 这一串**一条都没建出来**（调用方出声）。
        /// <para>🆕 **2026-10-25（`P6`）：补上 `useCallBack` 那一支** —— 原版方法体末尾
        /// （`cpp2il/UnitTweenSO.cs:287-298`）逐句：
        /// <c>if (useCallBack) { TweenCallback cb = () =&gt; onSequenceSignal?.Invoke();
        /// sequence.InsertCallback(callBackTime, cb); }</c>。
        /// ⇒ 这里同序：**整串建完之后**（`built == 0` 那一档已经提前返回 null，不建无意义的时间轴），
        /// `e.useCallback != 0` 就把 <paramref name="onSequenceSignal"/> 插到 `e.callbackTime` 那一拍上。
        /// ⚠️ <paramref name="onSequenceSignal"/> 为 null 时照原版是**空操作**（`?.Invoke()`），
        /// 回调照样插（原版也插）。</para>
        /// <para>🔴 **当前数据下不会触发**（不是没做）：74 份 `UnitTweenSO` 里 `useCallBack` **全 0**
        /// （`数据/游戏数据/tween/*.json` 现读：53 份显式 `"useCallBack": 0`、其余 21 份连字段都没有
        /// = Unity 默认 false）—— 见账 `特效动画` 那条的备注（调用点清单也在那里）。</para></summary>
        public static Sequence Build(UnitTweenEntry e, Transform source, Transform target, string why = null,
                                    Action onSequenceSignal = null)
        {
            if (e == null) return null;
            var seq = DOTween.Sequence();
            int built = 0;
            // 原版那两个状态位（`bVar2` / `bVar12`）：**跳过 muted 之后，下一条要不要强制 Append**
            bool skipped = false, skippedWasAppend = false;

            for (int si = 0; si < e.tweens.Length; si++)
            {
                var s = e.tweens[si];
                if (s == null) continue;
                var tw = MakeTween(s, source, target, why, e.name, si);
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

            // 原版 `UnitTweenSO.BuildSequence` 末尾那一支（`cpp2il/UnitTweenSO.cs:287-298`）：
            //   `if (useCallBack) sequence.InsertCallback(callBackTime, () => onSequenceSignal?.Invoke());`
            // ⚠️ 插入点 = 秒（DOTween `InsertCallback(float atPosition, TweenCallback)`），值 = 数据里的 `callBackTime`。
            if (e.useCallback != 0)
            {
                seq.InsertCallback(e.callbackTime, () => { if (onSequenceSignal != null) onSequenceSignal(); });
                CallbackInserted++;
            }

            BuiltCount++;
            return seq;
        }

        /// <summary>一条补间。返回 null = **这条做不出来**（原版也是 null ⇒ 跳过）。
        /// 判据 = 各子类的 `GetTween` 方法体，逐个见下面的注释。</summary>
        static Tween MakeTween(UnitTweenStep s, Transform source, Transform target, string why,
                               string entryName = null, int stepIndex = -1)
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
            else
            {
                // 🔴 **2026-10-22（W64 · `B09`）**：原版 `TweenInfoAnimationBase.GetTween` 在
                //   `ease == 0` 时走的是 **`SetEase(curve)`**（`AnimationCurve`），**不是 Linear**。
                //   74 份 SO 里 `curve` 非空的只有 2 条（`Tester` / `Levitate Slam`，共 6 个关键帧），
                //   而真正会用它的是 **`Tester` 第 0 步**（`MoveTween`、`ease = 0`；
                //   `Levitate Slam` 那条 `ease = 30` ⇒ 曲线是惰性的）。判据 =
                //   `数据/游戏数据/tween/Tester.json` 的 `MoveTween.data.curve.m_Curve`（4 个关键帧，实读）。
                var curve = CurveFor(s, entryName, stepIndex);
                if (curve != null) tw = tw.SetEase(curve);
                else
                {
                    tw = tw.SetEase(Ease.Linear);
                    // `delay` 那两步也是 `ease == 0`、而且没有曲线 ⇒ 计入就会把「真的退回 Linear」淹没。
                    // 它们本来就是个空转的等待补间（`DOTween.To(_ => {})`），缓动对画面零影响 ⇒ 不计数。
                    if (s.kind != "delay") EaseZeroFallback++;
                }
            }
            return tw;
        }

        /// <summary>`ease == 0` 但**连曲线都取不到**、只能退 Linear 的次数（诊断）。
        /// ⚠️ 在当前数据下应为 **0** —— `Tester#0` 有曲线（见 `CurveFallback`）；
        /// 另外两条 `ease == 0` 的步骤是 `delay` 类（纯等待，缓动本来就不影响画面）。
        /// 🧨 这个数 > 0 就说明「有 ease==0 的补间又退回 Linear 了」= B09 那个近似回来了。</summary>
        public static int EaseZeroFallback { get; private set; }

        /// <summary>🆕 2026-10-22（`B09`）：`ease == 0` 那条曲线的**运行期兜底表**（键 = `资产名#步骤序号`）。
        /// 为什么在代码里而不是 JSON：`工具/gen_unit_tweens.py` **不搬 `curve` 字段**，
        /// 而那个生成器不在本批的文件白名单里 ⇒ 先把原版关键帧逐位抄在这里，
        /// 等生成器补上 `curveTimes/Values/InSlopes/OutSlopes` 之后 **以 JSON 为准**（`CurveFor` 先读 JSON）。
        /// 🔴 判据（一手，逐位实读）：`数据/游戏数据/tween/Tester.json`
        ///   的 `MoveTween.data`——`ease: 0`、`duration: 1.0`、`curve.m_Curve` 四个关键帧。
        /// ⛔ 别「照手感」改这些数。</summary>
        static readonly Dictionary<string, (float t, float v, float inS, float outS)[]> CurveFallback =
            new Dictionary<string, (float t, float v, float inS, float outS)[]>
        {
            {
                "Tester#0", new (float, float, float, float)[]
                {
                    (0.0f,                  0.0f,                  3.5101411f,   3.5101411f),
                    (0.64677107f,           0.98289812f,           0.26425928f,  0.26425928f),
                    (0.86642885f,           0.91523606f,          -0.010785313f, -0.010785313f),
                    (1.0f,                  1.0f,                  0.019904172f, 0.019904172f),
                }
            },
        };

        /// <summary>取 `ease == 0` 那条曲线：**先读数据**（生成器搬过来之后就走这条），再退回
        /// <see cref="CurveFallback"/>。两处都没有 ⇒ null（调用方退 Linear 并计数）。</summary>
        static AnimationCurve CurveFor(UnitTweenStep s, string entryName, int stepIndex)
        {
            var c = new AnimationCurve();
            if (s.curveTimes != null && s.curveTimes.Length > 0
                && s.curveValues != null && s.curveValues.Length == s.curveTimes.Length)
            {
                for (int i = 0; i < s.curveTimes.Length; i++)
                    c.AddKey(new Keyframe(s.curveTimes[i], s.curveValues[i],
                                          At(s.curveInSlopes, i), At(s.curveOutSlopes, i)));
                return c;
            }
            if (string.IsNullOrEmpty(entryName) || stepIndex < 0) return null;
            (float t, float v, float inS, float outS)[] kfs;
            if (!CurveFallback.TryGetValue(entryName + "#" + stepIndex, out kfs)) return null;
            foreach (var k in kfs) c.AddKey(new Keyframe(k.t, k.v, k.inS, k.outS));
            return c;
        }

        static float At(float[] a, int i) { return (a != null && i < a.Length) ? a[i] : 0f; }

        /// <summary>🆕 2026-10-22（`B09`）**自检口**：按「资产名 + 步骤序号」取那条 `ease == 0` 的曲线。
        /// 取不到 ⇒ `null`（= 运行期会退 Linear）。判据与数据 → <see cref="CurveFallback"/>。</summary>
        public static AnimationCurve CurveForTest(string entryName, int stepIndex)
        {
            var e = Get(entryName);
            if (e == null || e.tweens == null || stepIndex < 0 || stepIndex >= e.tweens.Length) return null;
            return CurveFor(e.tweens[stepIndex], entryName, stepIndex);
        }

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
        /// <para>🔴 **2026-10-25（`E` 现读并订正，判据 = `cpp2il/.../TweenInfoAnimationBase.cs:44-130`，
        /// `decomp_full/TweenInfoAnimationBase__get_TargetDirection.c` 逐句同）**——</para>
        /// <list type="number">
        ///   <item>**方向 = `away 单位的位置 − target 单位的位置`**（反编译里先取 `awayFromUnit` 的 position、
        ///     再取 `targetUnit` 的 position，然后 `num = position.x − position2.x` / `num2 = position.z − position2.z`）。
        ///     ⇒ 它是 **`away − target`**（向量从目标指向「远离它」那一侧）。
        ///     ⚠️ 我们原来写成 `target − away`（**反了**）——`Move/Rotate` 里
        ///     `Quaternion.LookRotation(TargetDirection) * v` 会把位移转到**朝向目标**，与 `awayFromUnit` 语义相反。</item>
        ///   <item>**y 恒为 0**（两支都只写 x/z；`Vector3 vector = default` 的 y 从没被赋过）⇒ 是**水平方向**。
        ///     ⚠️ 我们原来把 y 也带上了。</item>
        ///   <item>退化：距离 `≤ 9.9999997e-06` ⇒ **`Vector3.zero`**（不是退回 forward）。
        ///     ⚠️ 我们原来退回 `ForwardOf(...)`。</item>
        ///   <item>`away` 取不到（`!cardScript`）⇒ 取 **`target` 的 `CardUI.body3D.forward`**，**并且同样只有 x/z**。</item>
        /// </list>
        /// <para>⚠️ 原版在「`away` 有、`target` 没有」那一支会抛 `NullReferenceException`（反编译 artifact 之外的真实路径：
        /// 后续解引用没有 null 检查）。我们**不抛**：出声一次并返回 `zero` —— 那是原版数据里到不了的路径，
        /// 如实标成我们的偏离（⛔ 不静默）。</para></summary>
        static Vector3 TargetDirection(UnitTweenStep s, Transform source, Transform target)
        {
            var away = Target(s.awayFromUnit, source, target);
            if (away == null)
            {
                // 原版这一支：`target.CardUI.body3D.forward`，**只取 x/z**（y = 0）
                var tgt = Target(s.targetUnit, source, target);
                var f = ForwardOf(tgt != null ? tgt : target);
                return new Vector3(f.x, 0f, f.z);
            }
            var t = Target(s.targetUnit, source, target);
            if (t == null)
            {
                if (!_targetDirWarned)
                {
                    _targetDirWarned = true;
                    Debug.LogWarning("[UnitTween] `awayFromUnit` 有、`targetUnit` 取不到 ⇒ 原版这一支会抛 "
                                   + "NullReferenceException；我们回 Vector3.zero（零方向 = 不做朝向变换）");
                }
                return Vector3.zero;
            }
            var d = new Vector3(away.position.x - t.position.x, 0f, away.position.z - t.position.z);
            float len = d.magnitude;
            if (len > 9.9999997e-06f) return d / len;
            return Vector3.zero;
        }
        static bool _targetDirWarned;

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
                tw = tn.SetRelative(s.relative != 0);
            }
            else
            {
                float k = t.localScale.x;
                v = new Vector3(v.x * k, v.y * k, v.z * k);
                // 🔴 2026-10-26（`C38` 补齐，判据 = `decomp_full/MoveTween__GetTween.c:80-89`）：
                //   **非** awayFromUnit（= 0）**且**目标单位是玩家侧时，`z` 再 ×−1
                //   （实读常量 `DAT_1834b2bc8` = −1.0f）。原版只在 `isLocal` 这条支做它。
                if (s.awayFromUnit == 0 && IsPlayerSide(t)) v.z = -v.z;
                if (s.from != 0) v += t.position;
                tn = t.DOLocalMove(v, s.duration);
                // 🔴 `decomp_full/MoveTween__GetTween.c:104-107`：本地支的 `isRelative = relative && !from`
                //   （非本地支是 `relative`，见上面那条）。原来两边共用 `relative` ⇒ `from=1` 时会多相对一次。
                tw = tn.SetRelative(s.relative != 0 && s.from == 0);
            }
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
            else if (s.isLocal != 0 && IsPlayerSide(t))
            {
                // 🔴 2026-10-26（`C48` 补齐，判据 = `RotateTween.cs:50-67` + `:111-116`）：
                //   原版是 `awayFromUnit == undefined && isLocal && 目标.isPlayer ⇒ obj2 = -1，然后
                //   `rotation.x * obj2`` ⇒ **x 取反**。**与 `relative` 无关**（`relative` 只进最后的
                //   `SetRelative`）—— 我们原来把这句误挂在 `relative` 名下、而且没取反。
                e = new Vector3(-e.x, e.y, e.z);
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

        // `ShakeTween.GetTween`：shakeType 1/其它 ⇒ Position · 2 ⇒ Rotation · 3 ⇒ Scale　🔴 **2026-10-26（`B9_E3P13`）就地订正**：账 `CardScript.DoUnitShake` 原来把落点指到这张表（「我们那一档走 DOShakeScale」）—— **指错了**：`CardScript.DoUnitShake` 是 `CardScript` 上的**直接调用**（`transform.DOShakePosition(duration, (s×0.05, s×0.03, 0), vibrato 13, randomness 30, fadeOff… )`，判据 = VA 反汇编 `0x1805EB7A0`），参数**不来自** `Resources/UnitTweens.json`；它的等价物是 `CardFeel.ShakeUnit`（要接线的是 `unitsShake` 那条数据 → `CardAnimOrder`/`BattleDriver`，⛔ 不是这张表）。本表只服务 AnimFX 的 `ShakeTween` 数据行。
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

        // ==================================================================
        //  🆕 2026-10-26（`C38` / `C48`）：原版 `MoveTween` / `RotateTween` 里那句
        //  「目标单位 `isPlayer` ⇒ z / x 取反」需要的 **isPlayer 判据**。
        // ==================================================================
        /// <summary>「这个 Transform 属于**本地玩家**那一侧吗」—— 原版读 `CardScript.isPlayer`（+0x40）。
        ///
        /// <para>我们 `CardView` **不带阵营字段**（阵营只在 `BattleDriver` 的两个视图字典里，
        /// 见 `CardFeel.IsPlayerRow` 的注释）⇒ 默认判据 = **静止位离哪一行近**
        /// （`CardFeel.IsPlayerRow`，与 `CardFeel.ApplyDepthScale` 的兜底档**同一条口径**）。
        /// 原版只在这两条很少用到的数据上依赖它：`MoveTween.isLocal=1` **1 条**、`RotateTween.isLocal=1`
        /// **1 条**，且都只被 `Execution_BL` 引用（我们侧 0 引用）⇒ 当前无可观测后果。</para>
        /// <para>表现层若要更准（例如 P2P 里本地座位被翻转），挂 <see cref="IsPlayerSideOf"/> 覆盖即可；
        /// 没挂就走上面那条兜底。⛔ 别在别处再写第二份判据（CLAUDE.md 三·5）。</para></summary>
        public static Func<Transform, bool> IsPlayerSideOf;

        static bool IsPlayerSide(Transform t)
        {
            if (t == null) return false;
            if (IsPlayerSideOf != null) return IsPlayerSideOf(t);
            return CardFeel.IsPlayerRow(t.position.z);
        }
    }
}
