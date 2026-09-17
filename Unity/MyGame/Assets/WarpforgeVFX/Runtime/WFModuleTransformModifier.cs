// WFModuleTransformModifier.cs — 原版 `AnimFXModuleTransformModifier` 的对应物
//   **18 实例 / 18 效果**
//
// 语义出处（**逐方法体**，不是按名字猜）
// ----------------------------------------------------------------
//   · `资料/AnimFX_18类方法体_块1.md` §7（字段表 + 5 个方法体 + 嵌套类那 1 个）
//   · 方法体原件（**逐行读过**）：
//     `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`
//       `AnimFXModuleTransformModifier__Initialize.c` · `__ApplyTransforms.c` · `__Update.c` · `__Exit.c`
//     `d:/2/tools/decomp_animfx_nested/AnimFXModuleTransformModifier.TransformModifier__ApplyModification.c`
//   · 报错原文逐字核过 `d:/2/tools/all_strings.txt`：
//     `:17796` `[ERROR] Can't find target card` ·
//     `:18165` `[ERROR] Use effect anchor parent for position is not implemented` ·
//     `:18174` `[ERROR] Use effect anchor parent for scale is not implemented`
//   · 字段偏移与 `dump.cs` 逐条吻合（`0x18 useEffectAnchorParent` · `0x19 modifyPosition` ·
//     `0x28 modifyRotation` · `0x38 modifyScale` · `0x10 target`）
//
// 照方法体还原了什么
// ----------------------------------------------------------------
//   **Initialize 九步**：① base → ② `cardTarget = null` → ③ `changeParent && targetCard` →
//   `transform.SetParent(targetCard.transform)` → ④ `originalParent = transform.parent` →
//   ⑤ `unParentAtStart` → `SetParent(null)` → ⑥ `cardTarget = objective==Caster ? actingCard : targetCard` →
//   ⑦ 卡为空 → `LogError("[ERROR] Can't find target card")` + `enabled = false` →
//   ⑧ 记 `originalScale` / `originalLossyScale`（**取的是卡的**，`__Initialize.c:102-114`）→
//   ⑨ `affectEnemyOnly && cardTarget.isPlayer` → 停，否则 `ApplyTransforms()`
//   **ApplyTransforms**：`foreach (m in transformModifiers)`，`m.target` **死了就整条跳过**
//   （原版是 `Object.op_Implicit` 判活，`__ApplyTransforms.c:60`，不是 NRE）：
//     · `modifyPosition`：带 anchor → `LogError("…for position is not implemented")`（**不设值**）；
//       否则 `target.localPosition = position`
//     · `modifyRotation`：带 anchor → `target.rotation = transform.parent.rotation * Quaternion.LookRotation(rotation)`
//       （四元数乘法逐项核过：`ApplyModification.c:59-69` 的 `Quaternion.op_Multiply`）；否则 `localEulerAngles = rotation`
//     · `modifyScale`：带 anchor → `LogError("…for scale is not implemented")`，**然后照样
//       `target.localScale = scale`**（报错在 if 里、赋值在 if 外，`__ApplyTransforms.c:108-119`）；
//       不带 anchor 就直接赋值
//   **Update 四分支（按原版顺序）**：`alwaysMaintainWorldScale` → `updateScaleByBoardPosition` →
//   `aimToTargetInUpdate` → `updateContinuously`（最后才 `ApplyTransforms`）
//   **Exit**：`unParentAtStart && parentAtExit` → `transform.SetParent(originalParent)`
//
// 🔴 **与 `资料/…_块1.md` §7 那张表的两处出入（我按方法体，不按表）**
//   1. 表里写「`modifyScale` → 带 anchor 时 LogError，**否则** `localScale = m.scale`」——
//      方法体是 **LogError 之后照样设值**（两份方法体一致）。数据里唯一同时开这两样的
//      （`Cut Termie Lightning Claws`：`modifyScale=1` + `useEffectAnchorParent=1`）就吃这条差别。
//   2. 表里没写 `originalScale/originalLossyScale` **取的是卡的**（不是本体的）——
//      这决定了 `alwaysMaintainWorldScale` 要拿卡才能算（见下「没还原」）。
//
// 🔴 我们自己定的（**原版不是这样**）
// ----------------------------------------------------------------
//   1. **这是唯一一处新接口：卡的上下文从哪来。**
//      原版这一类句句都在读 `controller.actingCard` / `controller.targetCard`（`CardScript`），
//      **我们的 `WarpforgeEffectPlayer` 上没有这两个字段**（`WarpforgeEffectPlayer.cs` 只有
//      `IsRetaliation`）⇒ 用静态回调 `WFEffectCards.Resolver` 供（形状见文件里的结构体定义）：
//        `WFEffectCards.Resolver = p => new WFEffectCardContext { actingCard = …, targetCard = …,
//                                                               actingIsPlayer = …, targetIsPlayer = … };`
//      **挂法就一行**，表现层在 `Play()` 之后记得住是哪两张卡就挂得上（`BattleDriver` 播特效时
//      手里有 `cardId`）。四个模块共用这一个回调（`Cardback` / `ChangeMaterial` / `ParticleAdjacent`
//      都读它）—— 定义放在本文件是**因为本轮只允许新建四个 .cs**。
//      · **回调挂了、但那张卡是 null** → 就是原版 ⑦：`LogError` + `enabled=false`，**整条不做**。
//      · **回调压根没挂**（今天的状态）→ **降级模式**：不依赖卡的那部分照做（父级挂载/复位 +
//        `transformModifiers` 的改写），**跟卡有关的分支这次不做并出声**（见
//        `DegradedWarnings` 那条警告）。**为什么不全停**：原版停是因为它「一定拿得到卡」，
//        拿不到 = 真出事了；我们是**还没接线** —— 一刀切会让 9/18 的父级操作也无谓地失效。
//      🔴 如果 `WarpforgeEffectPlayer` 将来自己长出 `ActingCard/TargetCard`，**只改这一处**。
//   2. 原版 `set_localScale` 除以 0 不设防（会出 NaN）——我们按 1 处理并出声（见 `Div`）。
//   3. 原版 `Update` 里 `aimToTargetInUpdate` 那条在 `controller.targetCard == null` 时
//      **直接 NullReferenceException**（`FUN_1803f47a0` = 抛异常）；我们**报错并跳过**（不崩）。
//
// 🔴 没还原的（各有明确的「为什么做不了」，实现里都 `LogWarning` + 计数）
// ----------------------------------------------------------------
//   ① `setDefaultUnitTransformScaleWhenUnParenting`（2 例：`Teleport Trait Summon` /
//      `Teleport Trigger Effect Quick`）：要 `BattleManager.GetUnitSizeInPlay(cardType, isPlayer)`。
//      我们**没有「单位尺寸」这个概念**（`CardPresentation/Core/CardFeel.cs:334` 已经记着
//      「我们的单位不占多格，没有『尺寸』这个概念」，那三档尺寸常量也接不上）⇒ **跳过**。
//   ② `updateScaleByBoardPosition`（2 例：`Ork_Squiggoth_Charge` / `Ork_Trampla_Charge`）：
//      要 `cardTarget.CardUI.<0x1c0>.MinionScaleByPositionSO.GetScale(isPlayer, localPosition,
//      originalScale)`。那份 SO **没有导出来**（`数据/` 里 0 命中），而且 `GetScale` 里那个
//      「全局默认尺度」静态 Vector3 在 `DAT_1842da2a8` 的静态块、**类名未定**（块1 §7 同一条）
//      ⇒ **跳过**。
//   ③ `aimToTargetInUpdate`（1 例：`Shuriken_Short_Trait`）：要
//      `controller.targetCard.GetEffectAnchor().position`。`CardScript.GetEffectAnchor` 是**原版卡
//      prefab 上的锚点节点**，我们这边**没有对应物**（特效是按世界坐标播的，不挂在卡上）
//      ⇒ **跳过**（有卡上下文也只差这一件）。
//   ④ `alwaysMaintainWorldScale` 的「没有父级」分支：原版除以 `DAT_1842da2a8` 静态块里的一个
//      Vector3（**类名未定，查不到**）。我们按 `Vector3.one` 处理 —— 语义上就是「没有父级时
//      不做补偿」，而且 `v / 1 = v` 与「无父级时 localScale == lossyScale」自洽。
//      ⚠️ 这条是**我们挑的**，不是原版的值。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `VFXObjective{Caster=0, Target=1}`：`cardTarget` 取哪一张卡。</summary>
    public enum WFVFXObjective { Caster = 0, Target = 1 }

    /// <summary>
    /// 原版 `AnimFXController.actingCard` / `.targetCard` 的对应物（**四个模块共用**）。
    /// 🔴 我们这边播放器上没有这两张卡 ⇒ 由表现层通过 `WFEffectCards.Resolver` 供。
    /// `valid == true` 只表示**回调挂了并返回了值**，不保证两张卡都非空
    /// （某种 objective 下缺一张是正常的，原版也有那张卡为空的处理）。
    /// </summary>
    public struct WFEffectCardContext
    {
        /// <summary>出手的那张卡（原版 `controller.actingCard`）。</summary>
        public Transform actingCard;
        /// <summary>被打的那张卡（原版 `controller.targetCard`）。</summary>
        public Transform targetCard;
        /// <summary>原版 `actingCard.isPlayer`。</summary>
        public bool actingIsPlayer;
        /// <summary>原版 `targetCard.isPlayer`。</summary>
        public bool targetIsPlayer;
        /// <summary>回调挂上了没有。</summary>
        public bool valid;
    }

    /// <summary>🔑 **下游钩子**：特效 ↔ 卡牌 的上下文（定义在本文件，四个模块共用）。
    /// 不挂 = 降级模式（跟卡有关的分支不做，但**会出声**）。</summary>
    public static class WFEffectCards
    {
        /// <summary>表现层挂这里：给定一个正在播的特效实例，返回它是为哪两张卡播的。</summary>
        public static Func<WarpforgeEffectPlayer, WFEffectCardContext> Resolver;

        /// <summary>拿不到卡上下文的次数（自检用；**不静默**）。</summary>
        public static int MissingCount;

        /// <summary>回调挂上了没有（挂上了才会走原版那条「卡为空 = LogError + 停」的路）。</summary>
        public static bool Wired { get { return Resolver != null; } }

        public static void ResetDiagnostics() { MissingCount = 0; }

        /// <param name="warn">true = 没挂回调时打一条警告（只报前 5 次）。
        /// 每帧调的地方（`ModuleTick`）传 false，免得刷屏。</param>
        public static bool TryGet(WarpforgeEffectPlayer p, out WFEffectCardContext ctx, bool warn = true)
        {
            ctx = new WFEffectCardContext();
            if (Resolver == null)
            {
                if (warn)
                {
                    MissingCount++;
                    if (MissingCount <= 5)
                        Debug.LogWarning("[WarpforgeVFX] 有模块需要『出手卡/目标卡』"
                                       + "（原版 `controller.actingCard/targetCard`），但 "
                                       + "`WFEffectCards.Resolver` 没挂 —— 跟卡有关的分支这次不做"
                                       + "（挂法见 `WFModuleTransformModifier.cs` 头注释，一行）。"
                                       + "这条警告只报前 5 次。");
                }
                return false;
            }
            ctx = Resolver(p);
            ctx.valid = true;
            return true;
        }
    }

    [WFModuleKind("AnimFXModuleTransformModifier")]
    public class WFModuleTransformModifier : WFEffectModule
    {
        /// <summary>一条改写（原版嵌套类 `AnimFXModuleTransformModifier.TransformModifier`）。
        /// 字段顺序照原版内存布局（`0x10 target` / `0x18 useEffectAnchorParent` /
        /// `0x19 modifyPosition` / `0x28 modifyRotation` / `0x38 modifyScale`）。</summary>
        [Serializable]
        public class TransMod
        {
            [Tooltip("要改的那个节点（@node:Transform:…）。**空/死了就整条跳过**（原版行为）")]
            public Transform target;
            /// <summary>数据里的引用值（`Configure` 填；手挂时用 `target`）。</summary>
            [HideInInspector] public string targetRef = "";

            [Tooltip("原版 `useEffectAnchorParent`：rotation 走「父级世界旋转 × 本值」，scale 走「报错+照样设」")]
            public bool useEffectAnchorParent;

            public bool modifyPosition; public Vector3 position;
            public bool modifyRotation; public Vector3 rotation;   // localEulerAngles（不带 anchor 时）
            public bool modifyScale;    public Vector3 scale;
        }

        // ---- 原版字段（块1 §7 的字段表，偏移与 dump.cs 逐条吻合）----
        [Tooltip("原版 `transformModifiers`：长度 1 = 9 例 / 0 = 9 例（一半只用来挂/脱父级）")]
        public TransMod[] transformModifiers = new TransMod[0];

        [Tooltip("原版 `alwaysMaintainWorldScale`（4 例）：每帧 localScale = 卡的 originalLossyScale / 父级 lossyScale")]
        public bool alwaysMaintainWorldScale;

        [Tooltip("原版 `objective{VFXObjective}`：cardTarget 取哪张卡（数据：Caster 16 / Target 2）")]
        public WFVFXObjective objective = WFVFXObjective.Caster;

        /// <summary>原版 `.ctor` 里 `affectEnemyOnly = true`（**默认开**，块1 §7）。</summary>
        [Tooltip("原版 `affectEnemyOnly`（默认 **true**）：cardTarget 是玩家自己的卡就整条停")]
        public bool affectEnemyOnly = true;

        [Tooltip("原版 `updateContinuously`（1 例）：每帧重跑 ApplyTransforms")]
        public bool updateContinuously;

        [Tooltip("原版 `aimToTargetInUpdate`（1 例）：每帧 transform.forward 指向目标卡 —— 见文件头「没还原」「③」")]
        public bool aimToTargetInUpdate;

        [Tooltip("原版 `updateScaleByBoardPosition`（2 例）：按棋盘位置缩放 —— 见文件头「没还原」「②」")]
        public bool updateScaleByBoardPosition;

        [Tooltip("原版 `changeParent`（1 例）：Initialize 时挂到 **targetCard** 下")]
        public bool changeParent;

        [Tooltip("原版 `unParentAtStart`（9 例）：Initialize 时 SetParent(null)")]
        public bool unParentAtStart;

        [Tooltip("原版 `parentAtExit`（2 例）：Exit 时挂回 originalParent（只在 unParentAtStart 也开时生效）")]
        public bool parentAtExit;

        [Tooltip("原版 `setDefaultUnitTransformScaleWhenUnParenting`（2 例）：见文件头「没还原」「①」")]
        public bool setDefaultUnitTransformScaleWhenUnParenting;

        // ---- 运行时（原版 0x50 / 0x58 / 0x60 / 0x6C）----
        [NonSerialized] public Transform originalParent;
        [NonSerialized] public Transform cardTarget;
        [NonSerialized] public Vector3 originalScale;
        [NonSerialized] public Vector3 originalLossyScale;

        /// <summary>`_off` + `enabled`：原版是 `Behaviour.enabled = false`。
        /// ⚠️ 我们的 `ModuleTick` 是**无条件广播**的（player 不看 `enabled`）⇒ 自己也要记一个。</summary>
        bool _off;

        // ---- 诊断 ----
        /// <summary>「卡为空 → 整条停」发生了几次（原版 ⑦ 那条路）。</summary>
        public static int StoppedNoCard;
        /// <summary>降级模式（回调没挂）下跑过几次 Initialize。</summary>
        public static int DegradedRuns;
        /// <summary>没还原的那几个分支各跳过几次。</summary>
        public static int SkippedUnitSize, SkippedBoardScale, SkippedAim, ZeroLossyScale;

        public static void ResetDiagnostics()
        {
            StoppedNoCard = 0; DegradedRuns = 0;
            SkippedUnitSize = 0; SkippedBoardScale = 0; SkippedAim = 0; ZeroLossyScale = 0;
            _warnedDegraded = _warnedAim = _warnedBoardScale = _warnedUnitSize = _warnedZeroScale = false;
        }

        static bool _warnedDegraded, _warnedAim, _warnedBoardScale, _warnedUnitSize, _warnedZeroScale;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;

            // `affectEnemyOnly` 只在**数据里有这个键**时才覆盖字段值 —— 原版 `.ctor` 给的是 true
            if (def.Has("affectEnemyOnly")) affectEnemyOnly = def.GetBool("affectEnemyOnly");
            alwaysMaintainWorldScale = def.GetBool("alwaysMaintainWorldScale");
            objective = (WFVFXObjective)def.GetInt("objective");
            updateContinuously = def.GetBool("updateContinuously");
            aimToTargetInUpdate = def.GetBool("aimToTargetInUpdate");
            updateScaleByBoardPosition = def.GetBool("updateScaleByBoardPosition");
            changeParent = def.GetBool("changeParent");
            unParentAtStart = def.GetBool("unParentAtStart");
            parentAtExit = def.GetBool("parentAtExit");
            setDefaultUnitTransformScaleWhenUnParenting =
                def.GetBool("setDefaultUnitTransformScaleWhenUnParenting");

            // transformModifiers[]：直到某条**一个已知键都没有**为止（条目里可能缺 `target`）
            var list = new List<TransMod>();
            for (int i = 0; ; i++)
            {
                string p = "transformModifiers[" + i + "]";
                if (!def.Has(p + ".modifyPosition") && !def.Has(p + ".modifyRotation") &&
                    !def.Has(p + ".modifyScale") && !def.Has(p + ".target")) break;
                list.Add(new TransMod
                {
                    targetRef = def.GetString(p + ".target"),
                    useEffectAnchorParent = def.GetBool(p + ".useEffectAnchorParent"),
                    modifyPosition = def.GetBool(p + ".modifyPosition"),
                    position = new Vector3(def.GetFloat(p + ".position.x"), def.GetFloat(p + ".position.y"),
                                           def.GetFloat(p + ".position.z")),
                    modifyRotation = def.GetBool(p + ".modifyRotation"),
                    rotation = new Vector3(def.GetFloat(p + ".rotation.x"), def.GetFloat(p + ".rotation.y"),
                                           def.GetFloat(p + ".rotation.z")),
                    modifyScale = def.GetBool(p + ".modifyScale"),
                    scale = new Vector3(def.GetFloat(p + ".scale.x"), def.GetFloat(p + ".scale.y"),
                                        def.GetFloat(p + ".scale.z")),
                });
            }
            transformModifiers = list.ToArray();
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            _off = false;

            // 引用值 → 真实 Transform（手挂的 `target` 优先，不是空就用它）
            for (int i = 0; i < transformModifiers.Length; i++)
            {
                var m = transformModifiers[i];
                if (m == null || m.target != null || string.IsNullOrEmpty(m.targetRef)) continue;
                m.target = ResolveNode<Transform>(transform, m.targetRef, name);
            }

            // ---- 卡的上下文（见文件头「我们自己定的」1）----
            WFEffectCardContext ctx;
            bool wired = WFEffectCards.TryGet(controller, out ctx);
            if (!wired)
            {
                DegradedRuns++;
                if (_warnedDegraded == false)
                {
                    _warnedDegraded = true;
                    Debug.LogWarning("[WarpforgeVFX] `WFModuleTransformModifier` 进**降级模式**："
                                   + "`WFEffectCards.Resolver` 没挂 ⇒ 只做不依赖卡的那部分"
                                   + "（`transformModifiers` 改写 / 父级挂载与复位），**跳过**："
                                   + "`changeParent` · `affectEnemyOnly` 闸门 · `alwaysMaintainWorldScale` ·"
                                   + " `updateScaleByBoardPosition` · `aimToTargetInUpdate` ·"
                                   + " `setDefaultUnitTransformScaleWhenUnParenting`。挂法见本文件头注释。"
                                   + "这条警告只报一次。");
                }
            }

            // ③ changeParent：挂到 **targetCard** 下（不是 objective 那张）
            if (changeParent && wired && ctx.targetCard != null)
                transform.SetParent(ctx.targetCard);          // 原版也是 set_parent（worldPositionStays 默认 true）

            // ④ originalParent（**在 ③ 之后**记，方法体如此）
            originalParent = transform.parent;

            // ⑤ unParentAtStart → SetParent(null)
            if (unParentAtStart)
            {
                transform.SetParent(null);
                if (setDefaultUnitTransformScaleWhenUnParenting)
                {
                    // 🔴 未还原（文件头「没还原 ①」）：原版是
                    // `BattleManager.GetUnitSizeInPlay(actingCard.cardType, actingCard.isPlayer)`，
                    // 我们**没有「单位尺寸」这个概念** ⇒ 跳过（localScale 保持不动）。
                    SkippedUnitSize++;
                    if (!_warnedUnitSize)
                    {
                        _warnedUnitSize = true;
                        Debug.LogWarning("[WarpforgeVFX] 🔴 未还原：`setDefaultUnitTransformScaleWhenUnParenting`"
                                       + "（原版 `BattleManager.GetUnitSizeInPlay`）—— 我们没有「单位尺寸」"
                                       + "这个概念（`CardFeel.cs:334` 同一条），脱父级时**不**复位 localScale。"
                                       + $"受影响 {SkippedUnitSize} 次起（`Teleport Trait Summon` / "
                                       + "`Teleport Trigger Effect Quick`）。这条警告只报一次。");
                    }
                }
            }

            // ⑥ cardTarget = objective 那张
            if (wired) cardTarget = objective == WFVFXObjective.Caster ? ctx.actingCard : ctx.targetCard;
            else cardTarget = null;

            // ⑦ 原版：卡为空 → LogError + enabled = false，**整条不做**
            if (wired && cardTarget == null)
            {
                StoppedNoCard++;
                SetOff();
                Debug.LogError("[WarpforgeVFX] [ERROR] Can't find target card"
                             + $"（效果 {EffectName} / objective={objective}）—— 原版从这里整条停掉"
                             + "（`enabled = false`）。⚠️ 卡上下文的 `actingCard/targetCard` 是表现层"
                             + "通过 `WFEffectCards.Resolver` 给的，检查那边给的是不是 null。");
                return;
            }

            // ⑧ originalScale / originalLossyScale —— **取的是卡的**（`__Initialize.c:102-114`）
            if (cardTarget != null)
            {
                originalScale = cardTarget.localScale;
                originalLossyScale = cardTarget.lossyScale;
            }

            // ⑨ affectEnemyOnly 闸门（降级模式下没有卡，闸门无从判 ⇒ 跳过并出声）
            if (affectEnemyOnly)
            {
                if (cardTarget == null) { /* 降级：见上面那条警告 */ }
                else if (IsPlayerCard(ctx))
                {
                    SetOff();
                    return;
                }
            }

            ApplyTransforms();
        }

        /// <summary>原版 `cardTarget.isPlayer`。</summary>
        bool IsPlayerCard(WFEffectCardContext ctx)
        {
            if (objective == WFVFXObjective.Caster) return ctx.actingIsPlayer;
            return ctx.targetIsPlayer;
        }

        void SetOff()
        {
            _off = true;
            enabled = false;      // 照原版（Inspector 里也看得见）
        }

        /// <summary>原版 `Update()`。我们的 `ModuleTick` 是**无条件广播**的 ⇒ 自己看 `enabled / _off`。</summary>
        public override void ModuleTick(float dt)
        {
            if (_off || !enabled) return;

            // ---- 1) alwaysMaintainWorldScale ----
            if (alwaysMaintainWorldScale)
            {
                if (cardTarget == null)
                {
                    // 降级模式：算不出（原版的分子是**卡的** lossyScale）—— 上面 Initialize 已出声，这里不刷屏
                }
                else
                {
                    var parent = transform.parent;
                    Vector3 divisor = parent != null ? parent.lossyScale : Vector3.one;
                    transform.localScale = new Vector3(
                        Div(originalLossyScale.x, divisor.x),
                        Div(originalLossyScale.y, divisor.y),
                        Div(originalLossyScale.z, divisor.z));
                }
            }

            // ---- 2) updateScaleByBoardPosition ----
            if (updateScaleByBoardPosition)
            {
                // 🔴 未还原（文件头「没还原 ②」）：要 `cardTarget.CardUI.<0x1c0>` 上的
                // `MinionScaleByPositionSO.GetScale(isPlayer, localPosition, originalScale)`。
                // 那份 SO 没导出来（`数据/` 里 0 命中），GetScale 里那个「全局默认尺度」静态
                // Vector3 的类名也查不到 ⇒ 跳过。
                SkippedBoardScale++;
                if (!_warnedBoardScale)
                {
                    _warnedBoardScale = true;
                    Debug.LogWarning("[WarpforgeVFX] 🔴 未还原：`updateScaleByBoardPosition` —— "
                                   + "原版从 `cardTarget.CardUI` 上取 `MinionScaleByPositionSO` 的曲线算缩放，"
                                   + "那份 SO **没导出来**、曲线里的「全局默认尺度」静态量也查不到"
                                   + "（块1 §7）⇒ 跳过（不动 localScale）。受影响 "
                                   + "`Ork_Squiggoth_Charge` / `Ork_Trampla_Charge`。这条警告只报一次。");
                }
            }

            // ---- 3) aimToTargetInUpdate ----
            if (aimToTargetInUpdate)
            {
                // 🔴 未还原（文件头「没还原 ③」）：要 `controller.targetCard.GetEffectAnchor()`。
                // ⚠️ 原版在这一条上 targetCard 为空会**直接 NRE**（FUN_1803f47a0），我们报错后跳过（不崩）。
                SkippedAim++;
                if (!_warnedAim)
                {
                    _warnedAim = true;
                    Debug.LogWarning("[WarpforgeVFX] 🔴 未还原：`aimToTargetInUpdate` —— 原版是 "
                                   + "`transform.forward = normalize(targetCard.GetEffectAnchor().position - "
                                   + "transform.position)`；我们**没有「卡的 effect anchor」这个节点**"
                                   + "（特效按世界坐标播、不挂卡）⇒ 跳过。受影响 `Shuriken_Short_Trait`。"
                                   + "（另：原版 targetCard 为空时这里会 NullReferenceException，"
                                   + "我们改成报错+跳过。）这条警告只报一次。");
                }
            }

            // ---- 4) updateContinuously ----
            if (updateContinuously) ApplyTransforms();
        }

        /// <summary>原版 `ApplyTransforms()`（= 逐条 `TransformModifier.ApplyModification(transform)`）。</summary>
        public void ApplyTransforms()
        {
            if (transformModifiers == null) return;
            for (int i = 0; i < transformModifiers.Length; i++) ApplyModification(transformModifiers[i]);
        }

        /// <summary>原版嵌套类 `AnimFXModuleTransformModifier.TransformModifier.ApplyModification(Transform)`
        /// —— 与 `ApplyTransforms` 是**同一份逻辑**（块1 §7 已记），所以只写这一份。</summary>
        public void ApplyModification(TransMod m)
        {
            if (m == null || m.target == null) return;      // 原版：判活，死了整条跳过（不是 NRE）

            if (m.modifyPosition)
            {
                if (m.useEffectAnchorParent)
                {
                    // 原版就是这样：**报错、不设值**（字符串表 `:18165`）
                    Debug.LogError("[WarpforgeVFX] [ERROR] Use effect anchor parent for position is not implemented"
                                 + $"（效果 {EffectName} / 节点 {m.target.name}）—— 原版本条也只报错不设值。");
                }
                else
                {
                    m.target.localPosition = m.position;
                }
            }

            if (m.modifyRotation)
            {
                if (m.useEffectAnchorParent)
                {
                    // 原版：`父级世界旋转 × LookRotation(rotation)`（四元数乘法逐项核过）
                    var parent = transform.parent;
                    if (parent == null)
                    {
                        Debug.LogWarning("[WarpforgeVFX] 带 anchor 的 modifyRotation 需要父级，"
                                       + $"但 `{gameObject.name}` 没有父级 —— 跳过这条改写。");
                    }
                    else
                    {
                        if (m.rotation == Vector3.zero)
                            Debug.LogWarning("[WarpforgeVFX] `LookRotation` 的朝向是零向量（原版这里会"
                                           + "打 Unity 自己的 error 并返回 identity）—— 照原版继续。");
                        m.target.rotation = parent.rotation * Quaternion.LookRotation(m.rotation);
                    }
                }
                else
                {
                    m.target.localEulerAngles = m.rotation;
                }
            }

            if (m.modifyScale)
            {
                // 🔴 与块1 §7 那张表的差异：**带 anchor 也照样设值**（报错在 if 里、赋值在 if 外）
                if (m.useEffectAnchorParent)
                {
                    Debug.LogError("[WarpforgeVFX] [ERROR] Use effect anchor parent for scale is not implemented"
                                 + $"（效果 {EffectName} / 节点 {m.target.name}）—— 原版报错之后**仍然会设** "
                                 + "localScale，我们照做。");
                }
                m.target.localScale = m.scale;
            }
        }

        /// <summary>原版 `Exit()`：只有 `unParentAtStart && parentAtExit` 才挂回 `originalParent`。</summary>
        public override void Exit()
        {
            if (unParentAtStart && parentAtExit && transform != null)
                transform.SetParent(originalParent);        // 原版也是 set_parent（worldPositionStays 默认 true）
        }

        /// <summary>除以 0 会出 NaN 并把渲染搞坏 —— 原版不设防，我们按 1 处理并出声。</summary>
        float Div(float a, float b)
        {
            if (b > -1e-6f && b < 1e-6f)
            {
                ZeroLossyScale++;
                if (!_warnedZeroScale)
                {
                    _warnedZeroScale = true;
                    Debug.LogWarning("[WarpforgeVFX] `alwaysMaintainWorldScale` 要除以父级的 lossyScale，"
                                   + $"但那个轴是 0（{b}）—— 按 1 处理（原版这里会得到 NaN）。"
                                   + "这条警告只报一次。");
                }
                return a;
            }
            return a / b;
        }
    }
}
