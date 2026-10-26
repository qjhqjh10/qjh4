// WarpforgeEffectPlayer.cs — 特效的播放入口 + 生命周期
//
// 没有它，958 个 prefab 进场景就是「不会死的对象」：既没有「播一个特效」的 API，
// 也没有任何人负责销毁。这两件事才是「研究产物 → 游戏资产」的差距所在。
//
// 生命周期语义（从原版 AnimFXController 的签名桩 + 2346 个组件的参数值反推，见下）
// ------------------------------------------------------------------
//   原版字段（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXController.cs`）：
//     bool  preventDestroy    —— preventDestroy=true 时 destroyTime 带 [HideIf]，即「不由自己管销毁」
//     float destroyTime       —— 多久后自己销毁
//     float exitDestroyTime   —— "Time that will last the effect living until it gets destroy after exit"
//     const float SAFE_DESTROY_TIME = 3f
//
//   数据实测（`数据/游戏数据/animfx_components.json`，990 个实例 / 912 个效果）：
//     destroyTime      中位 3.0，范围 0–17，按效果逐个调过
//     exitDestroyTime  956/990 是 3.0 —— 基本是个常量默认值
//     preventDestroy   97 个实例是 1
//
//   两条时间线**是并行的两条路，不是串起来的**（下面这组分布当时是唯一的线索）：
//     · 自己管销毁的（preventDestroy=false）：到 destroyTime 就销毁
//     · 交给外部的（preventDestroy=true）：原版不管，由牌局/技能代码在合适时机调 Exit()
//   因为 exitDestroyTime 几乎是个常量，而 destroyTime 是逐个调过的 —— 若两者是串联的
//   「先活 destroyTime 再活 exitDestroyTime」，你不会看到这种分布。
//   ⚠️ **2026-09-18 反编译更正**：上面第 1 条当时写成「到 destroyTime 就走一次 Exit()」—— **错**，
//      原版那条路上**不调 Exit**（见下）。2026-10-25 代码已按反编译改齐。
//
// 🔴 **2026-10-25：两条时间线按反编译方法体逐句对齐了（原来是「到寿命先 Exit」的偏离，已撤）。**
//
//   原版判据（`d:/2/tools/decomp_full/`，第一权威）：
//     · `AnimFXController__OnEnable.c`：
//         `currentTime = 0;`
//         `if (destroyTime > 0 && !preventDestroy) Object.Destroy(gameObject, destroyTime);`
//       ⇒ **到点直接销毁 GameObject —— 不调 `Exit()`、不广播模块、不停发射、不播 `exitSounds`。**
//     · `AnimFXController__Exit.c`：`OnFinished?.Invoke()` → `foreach (m in modules) m.Exit()` →
//         `exiting = true` → `if (!preventDestroy) Object.Destroy(gameObject, exitDestroyTime);`
//       ⇒ Exit 这条路**也不停发射**（它只让模块收尾），再活 `exitDestroyTime`（默认 3s）才销毁。
//     · 全量 grep 调用点：`AnimFXController__Exit` **只有 2 处**
//       （`BattleCardUI__CleanStatusAnims.c` / `__RemoveStatusAnim.c`，都是**状态动画**那条）；
//       `AnimFXController__DoDestroy` **只有 1 处**（`BattleManager__RemoveDestroyableAnims.c`）。
//       ⇒ 绝大多数效果**从头到尾没人调 Exit**，「退场动画 / exitSounds」在它们身上原版根本不播。
//
//   我们现在的行为（= 上面两条，逐条对上）：
//     · `Tick` 到 `_lifetime` ⇒ `DestroyAtLifetime()`：**直接销毁**（见该方法）。
//     · `Exit()`（外部调：状态动画收场 / 我们自己要收某个效果时）⇒ 广播 OnFinished + 模块 Exit，
//       再活 `exitDestroyTime` 后销毁。⚠️ 我们**不再** `ps.Stop(StopEmitting)` —— 原版不停
//       （这一步是 2026-10-25 撤掉的；当时它是配着「到寿命先 Exit」那套写的）。
//   ⚠️ **两处有意的偏离（都不是「原版就这样」）**：
//     ① `Exit()` 有**重入保护**（原版没有：它每调一次就再 `OnFinished` 一次、再排一次销毁）；
//     ② `Kill()` / `autoDestroySeconds` 是本工程自有 API，`Kill` 会先广播一次 `DoDestroy`
//        （原版没有对应物）。`autoDestroySeconds` 走的是原版那条 `destroyTime` 语义。
//
// 用法：
//    WarpforgeEffectPlayer.Play("Antimatter Explosion", parent: cardRoot);
//    WarpforgeEffectPlayer.Play("Lightning_Green", cardRoot, scale: 1.5f, autoDestroySeconds: 2f);
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [DisallowMultipleComponent]
    public class WarpforgeEffectPlayer : MonoBehaviour
    {
        /// <summary>原版常量 SAFE_DESTROY_TIME —— 原版既没有 destroyTime 也没有可算的自然时长时的兜底。
        /// 它保证「不管什么情况都不会留一个永远不死的对象」。</summary>
        public const float SafeDestroyTime = 3f;

        /// <summary>正在播放的实例（用来证「播完真的消失了」，也是 KillAll 的抓手）。</summary>
        static readonly List<WarpforgeEffectPlayer> Active = new List<WarpforgeEffectPlayer>();

        /// <summary>当前活着的特效数。白板测试靠它证「播 → 消失」闭环，不靠肉眼。</summary>
        public static int ActiveCount { get { return Active.Count; } }

        /// <summary>🆕 2026-10-25：因为**持有者离场**（父节点销毁 / 变成 `activeInHierarchy=false`）
        /// 而被收掉的实例数（累计）。诊断/自检用 —— 它 &gt; 0 说明 `preventDestroy` 那批真的被收过。</summary>
        public static int OwnerReleased { get; private set; }

        /// <summary>当前所有活着的实例（只读快照，调用方别改）。</summary>
        public static IReadOnlyList<WarpforgeEffectPlayer> ActivePlayers { get { return Active; } }

        /// <summary>全部立刻销毁。切场景 / 退出牌局时兜底用。</summary>
        public static void KillAll()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] != null) Active[i].Kill();
            Active.Clear();
        }

        /// <summary>播完时触发（销毁之前）。</summary>
        public event Action OnFinished;

        // ---- AnimFX 模块（原版 `AnimFXController.modules`）----
        //
        // 🔴 **本类就是这条线上唯一的控制器**：原版 `AnimFXController` 对**所有模块无条件广播**
        //    Initialize / Exit / DoDestroy，**它一处都不读 `actionStart`**（要不要响应由各模块
        //    自己读那个字段）。别再加第二个 controller —— 两套销毁计时会互相打架。
        // 原版那 958 个效果走「按数据装配」（`WFModuleFactory` + entry.modules）；
        // 自制特效走「编辑器里手挂」—— 两条路共用同一批模块类，见 `WFEffectModule`。
        readonly List<WFEffectModule> _modules = new List<WFEffectModule>();

        /// <summary>本效果装上了几个模块（自检 / 诊断用）。</summary>
        public int ModuleCount { get { return _modules.Count; } }

        /// <summary>只读的模块清单。</summary>
        public IReadOnlyList<WFEffectModule> Modules { get { return _modules; } }

        /// <summary>原版 `AnimFXModuleBase.isRetaliation`（「出招的那张卡不属于当前回合方」）。
        /// 原版算法 = `BattleManager.IsPlayerTurn() XOR actingCard.isPlayer`。
        /// ⚠️ **这一层不知道牌局**，所以要由调用方（`BattleDriver`）在 `Play` 之后设进来；
        ///    不设就是 false。**没有调用方设它时，`isRetaliation` 相关的分支永远走 false 那支。**</summary>
        public bool IsRetaliation { get; set; }

        bool _finishedFired;

        [Tooltip("自己跟着 Update 推进。外部驱动时（白板 / 批处理自检）关掉，由调用方 Tick —— " +
                 "两边都推会走两倍速")]
        public bool autoTick = true;

        WFEffectEntry _entry;
        /// <summary>`_entry == null`（没走效果库）那条兜底只报一次 —— 见 `EffectName`。</summary>
        bool _warnedNoEntry;

        float _time;            // 从播放到现在
        float _lifetime;        // 到点**直接销毁**（原版 destroyTime）；float.PositiveInfinity = 不自动收
        float _exitAt = -1f;    // 调 Exit() 的时刻
        float _exitDelay;       // Exit() 之后再活多久（原版 exitDestroyTime）
        bool _exiting;
        bool _destroyed;
        /// <summary>🆕 2026-10-25（完全复刻第 1 轮 · D 域 · K5 `P1-4` 的落地）：**持有者**。
        ///
        /// <para>**为什么需要**：原版这批特效 `preventDestroy = 1`（现读一次性桶里 **15 个效果 / 37 行**），
        /// 原版**从不**给它们排计时销毁 —— 它们靠「实例化在卡的 `effectsAnchor` 下
        /// （`CardScript.cs:17167`），卡离场 ⇒ 整棵子树一起没」收场。我们原来既没有计时销毁、
        /// 也没有这一跳 ⇒ 它们**整局驻留**（其中 `MarkOfSlaanesh_enchantment` /
        /// `Buff EC Stimulate passive` 是**循环发射器**，画面一直留在场上）。</para>
        ///
        /// <para>**这一跳做什么**：`Tick` 里盯这个 Transform —— 它被销毁、或所在 GameObject
        /// 变成 `activeInHierarchy == false`（= 那张卡离场/视图收起来了）⇒ **直接销毁自己**
        /// （= 原版「父节点被销毁、子件跟着没」；**不**广播模块 `Exit`、**不**发 `OnFinished`）。
        /// 与 `Exit()` / `DoDestroy()` 都不同，见 `Tick` 里那段的注释。</para>
        ///
        /// <para>⚠️ **与「到寿命销毁」是两条路**（⛔ 别合并）：寿命那条按 `destroyTime`；
        /// 这条按**持有者还在不在**。`preventDestroy` 的特效**只走这条**。</para>
        /// <para>⚠️ **只有 `parent != null` 的实例才会被盯**（`Play` 里设）—— 根特效
        /// （`parent == null`，例如 HUD 平面那几件）没有持有者，⛔ 不猜一个。</para></summary>
        Transform _ownerWatch;
        /// <summary>`_ownerWatch` 的开关。⚠️ **必须单独记一个 bool**：Unity 的 `==` 对**已销毁**的对象
        /// 返回 null 语义（fake-null）⇒ `if (_ownerWatch != null)` 在「持有者被销毁」时是 **false**，
        /// 那个分支永远进不去（本项目踩过同形的坑）。</summary>
        bool _hasOwnerWatch;
        /// <summary>见过持有者**活着**一次没有（见 `Tick` 里那段：没见过的「inactive」不算离场）。</summary>
        bool _ownerWasActive;

        // ---- 🆕 2026-10-25：原版 `DestroyByTime` 那一跳（`WFEffectExtras` 用）----
        /// <summary>`-1` = 没排。否则 = 到 `_time` 超过它就销毁（**直接销毁**，同 `DestroyAtLifetime`）。</summary>
        float _destroyAfterAt = -1f;
        /// <summary>本实例上「非 AnimFX 模块」的逐帧组件（`IWFEffectExtraTick`；`Init` 里收一次）。</summary>
        readonly List<IWFEffectExtraTick> _extraTicks = new List<IWFEffectExtraTick>();
        /// <summary>🆕 2026-10-23（A1 R23）：销毁权已交给某个模块（原版 `AnimFXController.DoDestroy`
        /// 「有模块就不销毁」）⇒ controller 不再自己 `Destroy`，但仍要继续 `Tick` 让模块的数秒倒计时跑完。</summary>
        bool _destroyHandedOff;

        /// <summary>效果名 —— **全工程唯一的真名来源**。
        ///
        /// 正常路径：`Play()` 里 `Init(entry, …)` 填了 `_entry` ⇒ 直接是效果库条目的名字。
        /// 兜底路径（`_entry == null`：手工 `Instantiate` / 预挂实例）：
        ///   退回 `gameObject.name`，但**剥掉引擎 `Instantiate` 自动加的 `(Clone)`**，并**每个实例只报一次**。
        ///
        /// 为什么必须剥（2026-10-23 · 特效线 A1）：
        ///   `WFModuleAnimation` 拿 `EffectName + "|" + 状态名` 去 `WarpforgeAnimatorBridge.TryGetClip`
        ///   取原版片段别名（`工具/gen_simple_animation_clips.py` 登记的就是**干净的效果名**）。
        ///   多一个 `(Clone)` ⇒ 别名取不到 ⇒ **整段动画静默失效**，而且现象看起来像「这个效果本来就没动画」。
        ///   判据 → `资料/普查产出_1023/E2_dump护栏与EffectName审计.md` §②。
        ///
        /// ⛔ **别把这个剥后缀的逻辑抄到别处**：`WFModuleAnimation` 等调用方一律走本属性；
        ///   两个类各剥一次迟早不一致（铁律：判据只留一处）。</summary>
        public string EffectName
        {
            get
            {
                if (_entry != null) return _entry.name;

                var n = name;
                const string k = "(Clone)";
                if (n != null && n.Length > k.Length && n.EndsWith(k, StringComparison.Ordinal))
                    n = n.Substring(0, n.Length - k.Length).TrimEnd();

                if (!_warnedNoEntry)
                {
                    _warnedNoEntry = true;
                    Debug.LogWarning($"[WarpforgeVFX] 《{n}》这个 WarpforgeEffectPlayer **没有走效果库**" +
                                     "（`_entry == null`：手工 Instantiate / 预挂实例）⇒ `EffectName` 退回 GameObject 名" +
                                     "（已剥掉 `(Clone)`）。**按效果名拼键的模块**（`WFModuleAnimation` 的 " +
                                     "`<效果名>|<状态名>` 别名）可能因此取不到片段。正常播放请走 " +
                                     "`WarpforgeEffectPlayer.Play(name, …)`。");
                }
                return n;
            }
        }
        public WFEffectEntry Entry { get { return _entry; } }
        public bool IsExiting { get { return _exiting; } }
        public bool IsPlaying { get { return !_exiting && !_destroyed; } }

        /// <summary>预计总存活时长。不自动收的是 Infinity。
        /// ⚠️ **2026-10-25**：到寿命那条路现在是「**直接销毁**」（原版 `destroyTime`），
        /// **不再加** `exitDestroyTime` 的尾巴 —— 尾巴只在**外部调了 `Exit()`** 时才有
        /// （本属性算不出「谁会调 Exit」，所以只报自己管得着的那一段）。</summary>
        public float ExpectedLifetime
        {
            get { return _lifetime; }
        }

        // ---- 播放入口 ----

        /// <summary>按名字播一个特效。名字来自 WarpforgeEffectLibrary（= 原版 prefab 名）。
        /// 找不到时打一条警告并返回 null —— 不抛异常，特效缺失不该让玩法直接崩。
        /// <para>🔴 **`layer`（2026-10-24 · 战斗线 `C6·Q1`）** —— 实例整棵子树的层：
        ///   · `layer &lt; 0`（默认）⇒ **`parent != null` 时跟父节点的层**（层在 Unity 里**不继承**：
        ///     `Instantiate(prefab, parent)` 保的是 prefab 自己的层）；`parent == null` ⇒ 保持 prefab 自带层。
        ///   · `layer &gt;= 0` ⇒ **显式**设成这一层（压过「跟父节点」那条）—— **根特效**用这一档。
        /// 判据（原版 13 场逐场实读）：**只有战场透视相机画 layer 0**（bits 1137），UI 相机只画层 5（bits 32）
        /// ⇒ 原版的**阵亡爆散体 / 残骸 Destroy 粒子**（根实例化、根层 0）由**战场相机**画；
        /// 我们那批 prefab 的根层也是 0，而 HUD 是**正交**相机（108 px/单位）⇒ 不显式搬层就会
        /// **位置与尺度两样全错**。判据全文 → `资料/普查产出_1024c/C6_表现界面轮抽剩余判据.md` §一。</para></summary>
        public static WarpforgeEffectPlayer Play(
            string effectName,
            Transform parent = null,
            Vector3 localPosition = default(Vector3),
            float scale = 1f,
            float autoDestroySeconds = -1f,
            bool assignScale = false,
            int layer = -1)
        {
            var lib = WarpforgeEffectLibrary.Instance;
            if (lib == null)
            {
                Debug.LogWarning("[WarpforgeVFX] 没有效果库 —— 先跑菜单 Tools > Warpforge > 生成效果库");
                return null;
            }
            WFEffectEntry entry;
            if (!lib.TryGet(effectName, out entry))
            {
                Debug.LogWarning($"[WarpforgeVFX] 效果库里没有 '{effectName}'");
                return null;
            }
            return Play(entry, parent, localPosition, scale, autoDestroySeconds, assignScale, layer);
        }

        /// <summary>照着台账判定的名字播 —— 找不到精确名时退化成模糊匹配，并把命中的名字打出来。</summary>
        public static WarpforgeEffectPlayer PlayFuzzy(
            string keyword, Transform parent = null, Vector3 localPosition = default(Vector3),
            float scale = 1f, float autoDestroySeconds = -1f, bool assignScale = false, int layer = -1)
        {
            var lib = WarpforgeEffectLibrary.Instance;
            if (lib == null) { Debug.LogWarning("[WarpforgeVFX] 没有效果库"); return null; }
            var hit = lib.Find(keyword);
            if (hit.Count == 0) { Debug.LogWarning($"[WarpforgeVFX] 没有名字含 '{keyword}' 的效果"); return null; }
            if (hit.Count > 1)
                Debug.Log($"[WarpforgeVFX] '{keyword}' 命中 {hit.Count} 个，取最短的 '{hit[0].name}'");
            return Play(hit[0], parent, localPosition, scale, autoDestroySeconds, assignScale, layer);
        }

        /// <param name="layer">`-1` = 跟父节点层（`parent == null` 时保持 prefab 自带层）；
        /// `>= 0` = **显式**设成这一层。语义与判据见上面那个按名字的 `Play` 的 doc。
        /// 🆕 2026-10-25（`B14`）：`WF_GRAB501=1` 时**抓屏扭曲那一族**会被 `GrabScreen501.FinalLayer`
        /// 覆盖成 501 那一层（显式 `layer` 也一样 —— 否则它会被默认透明 pass 和 501 各画一遍）。</param>
        public static WarpforgeEffectPlayer Play(
            WFEffectEntry entry,
            Transform parent = null,
            Vector3 localPosition = default(Vector3),
            float scale = 1f,
            float autoDestroySeconds = -1f,
            bool assignScale = false,
            int layer = -1)
        {
            if (entry == null)
            {
                Debug.LogWarning("[WarpforgeVFX] Play() 拿到空的 entry");
                return null;
            }
            if (entry.prefab == null)
            {
                // ⚠️ 这条**必须出声**。prefab 引用为空最常见的原因是「重导过 prefab，
                //    但没重建效果库」—— 重导会重新生成 GameObject 的 fileID，
                //    库里的 (guid, fileID) 引用就全废了（GUID 还对得上，所以很难发现）。
                //    修法：跑一次菜单 Tools > Warpforge > 生成效果库。
                Debug.LogWarning($"[WarpforgeVFX] 效果 '{entry.name}' 的 prefab 引用是空的 —— "
                               + "多半是重导过 prefab 之后没重建效果库（Tools > Warpforge > 生成效果库）");
                return null;
            }

            var go = Instantiate(entry.prefab, parent);
            go.name = entry.name;                 // 去掉 Instantiate 加的 "(Clone)"，日志里好认
            var t = go.transform;

            // ⚠️ 位置必须归位，旋转和缩放**不能动**。三个值分别有实测依据：
            //   · localPosition：原版有一批效果把根节点停在**场外 x≈100**（"后台"位置，
            //     Unity 里常见的做法）。实测 Atk_GrotGrenade / Atk_ExileGlaiveThrow Movement
            //     根节点在 (99.59, 1.24, -0.37)、Attack_Stomp 在 (100, 0.725, 0)。
            //     不归位的话，这些效果直接播在场景外面 —— 看起来就像「什么都没渲染」。
            //   · localRotation：**是效果作者定的一部分**，不是噪音。实测 Antimatter Explosion
            //     根节点是 X=-90°。归零会把一批效果整体转 90°。
            //   · localScale：同理（Atk_GrotGrenade 是 0.5）。调用方传的 scale 默认是**乘数**，
            //     乘在作者设定的缩放之上；传 1（默认）就原样不动。
            // 🔴 **2026-10-22（W64 · `B17`）新增 `assignScale`**：原版有几件是**赋值**而不是乘 ——
            //   落地扬尘 `CardScript__LandOnGround.c:87-106`：`SetParent(MinionManager)` 之后
            //   `localScale = Vector3.one × desiredScale`。我们的等价物是「挂在卡视图上（父节点已经带
            //   desiredScale）」⇒ 根缩放必须是 **1**，⛔ 不能是「作者设定的 0.9913 × 1」。
            //   传 `assignScale: true` ⇒ **忽略 prefab 自带根缩放**、直接 `= one × scale`。
            t.localPosition = localPosition;
            if (assignScale) t.localScale = Vector3.one * scale;
            else if (!Mathf.Approximately(scale, 1f)) t.localScale = t.localScale * scale;

            var p = go.GetComponent<WarpforgeEffectPlayer>();
            if (p == null) p = go.AddComponent<WarpforgeEffectPlayer>();
            p.Init(entry, autoDestroySeconds);

            // 🔴 2026-10-25（D 域 · 完全复刻第 1 轮 · K5 `P1-4`）：`preventDestroy` 的实例**没有计时销毁**
            //    （原版语义），它靠「挂在卡的 `effectsAnchor` 下、卡离场整棵子树一起没」收场。
            //    我们记下持有者，由 `Tick` 盯它（见 `_ownerWatch` 的注释）。
            //    ⚠️ 只认 `parent != null`：根特效没有持有者，⛔ 不猜一个。
            if (entry.preventDestroy && parent != null) { p._ownerWatch = parent; p._hasOwnerWatch = true; }

            // ---- 层：**在 `Init` 之后**刷，这样模块 `Initialize` 万一挂出新子件也一起搬到同一层 ----
            // 🔴 **2026-10-24（战斗线 `C6·Q1`）：这条规则原来只在 `WarpforgeEffectHook`（`CardEffects.Play`
            //    那条钩子）里做**，而**直接调本函数**的调用点一个都不走它（`CardView` 的残骸体、
            //    `CardFeel.PlayDeathFallbackFx`、`BattleDriver` 的相邻单位 / Swarm 合并、以及
            //    `SpawnDeathBody` / `SpawnRemnantDestroy` 的**根**特效）⇒ 那些实例停在 prefab 自带层
            //    （残骸体 10 / Swarm 件 0 / 多数相邻件 0），在 3D 档被 **HUD 正交相机**画。
            //    ⇒ 把规则**下沉到这里**：一处覆盖所有调用点（含以后新增的）。
            //   ⚠️ 显式 `layer`（>= 0）**压过**「跟父节点」—— 根特效（`parent == null`）只有这一条路能搬层。
            int wantLayer = layer >= 0 ? layer : (parent != null ? parent.gameObject.layer : -1);
            // 🆕 2026-10-25（`B14` · `X10`）：501 启用时，**抓屏扭曲那一族**要搬到 501 那一层
            //   （否则它会被 URP 默认透明 pass 画一遍、501 再画一遍 = 扭曲叠两遍）。
            //   判据 / 开关 / 「为什么默认档逐位等价」→ `GrabPassTransparentFeature.cs` 里的 `GrabScreen501`。
            //   ⛔ 关着时它**原样返回 `wantLayer`**（连分支行为都与改前同值）。
            wantLayer = GrabScreen501.FinalLayer(go, wantLayer);
            if (wantLayer >= 0) SetLayerRec(go, wantLayer);

            return p;
        }

        /// <summary>把这个实例整棵子树设到某一层（层**不会**从父节点继承，必须自己走一遍）。
        /// <para>⚠️ 与 `WarpforgeEffectHook.SetLayerRec` 是同一口径的两份实现（那个文件不归本批白名单改）
        /// —— 两处都只做「递归写 `layer`」，没有第二套规则。</para></summary>
        public static void SetLayerRec(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRec(t.GetChild(i).gameObject, layer);
        }

        void Init(WFEffectEntry entry, float autoDestroySeconds)
        {
            _entry = entry;

            // 材质重建：play 模式下由 binder 的 Awake 干了，编辑器里 Awake 不跑（工具/白板要手动）
            var binder = GetComponent<WarpforgeEffectBinder>();
            if (binder != null && !Application.isPlaying) binder.Apply();

            _exitDelay = entry.exitDestroyTime > 0f ? entry.exitDestroyTime : 0f;

            if (autoDestroySeconds >= 0f) _lifetime = autoDestroySeconds;            // 调用方说了算
            else if (entry.preventDestroy) _lifetime = float.PositiveInfinity;       // 原版不让它自己死
            else _lifetime = entry.AutoLifetime();

            // 🔴 **2026-10-25（完全复刻第 1 轮 · D 域）**：`isRetaliation` 必须在**模块 `Initialize` 之前**算好
            //    —— 原版那两个东西是同一跳：`AnimFXController.SetData(actingCard,targetCard)`
            //    → `foreach (modules) module.Initialize(this)` → 基类 `Initialize` 里算
            //    `isRetaliation = actingCard.isPlayer != BattleManager.IsPlayerTurn()`
            //    （`decomp_full/AnimFXController__SetData.c:31-46` + `AnimFXModuleBase__Initialize.c:26-50`）。
            //    原来我们**一个赋值点都没有** ⇒ 恒 false ⇒ `WFModuleInstanceParticleAdjacent` 那道
            //    `playOnRetaliation` 闸**永远放行**（1 个实例会在反击拍误播）。
            ResolveIsRetaliation();

            // ---- 模块：先按数据装配（原版那批），放粒子之前建好 ----
            BuildModules(entry);

            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear(true);
                ps.Play(true);
            }

            // 🔴 2026-10-25：**非 AnimFX 模块**的那些 MonoBehaviour（`SyncParticleSeeds` / `DestroyByTime` /
            //    `SubEmitterManualTrigger` …）原来**一个都不会被携带** ⇒ 静默丢失。
            //    现在按 `Resources/WarpforgeVFX/effect_extras.json` 补挂（表只含需要它的 88 件特效）。
            //    ⚠️ **排在上面那次 `Play` 之后**：`SyncParticleSeeds` 要「先停再按同一种子起」，
            //    顺序反了它设的种子会被我们这次 `Play` 盖掉。
            WFEffectExtras.AttachAll(this);
            _extraTicks.Clear();
            foreach (var x in GetComponentsInChildren<IWFEffectExtraTick>(true)) _extraTicks.Add(x);

            Active.Add(this);
        }

        /// <summary>🆕 2026-10-25：排一次「到点直接销毁」（原版 `DestroyByTime.Start` 那一跳）。
        /// 走的是 `DestroyAtLifetime` 同一条路（不广播模块、不发 `OnFinished`）。</summary>
        public void DestroyAfter(float seconds)
        {
            if (seconds < 0f) return;
            _destroyAfterAt = _time + seconds;
        }

        /// <summary>累计「算不出 `isRetaliation`」的次数（回合方钩子 `WFEffectCards.IsLocalPlayersTurn`
        /// 没挂）。**不静默**：第一次会打一条警告。</summary>
        public static int RetaliationUnknown { get; private set; }

        /// <summary>🔴 2026-10-25：按原版那一拍算 `IsRetaliation`
        /// （`actingCard.isPlayer != IsPlayerTurn()`；判据 → `WFEffectCards.IsLocalPlayersTurn` 的注释）。
        /// **必须在模块 `Initialize` 广播之前**调 —— 原版两件事在同一跳里（`SetData` → 各模块 `Initialize`）。</summary>
        void ResolveIsRetaliation()
        {
            IsRetaliation = false;
            if (WFEffectCards.IsLocalPlayersTurn == null)
            {
                if (RetaliationUnknown++ == 0)
                    Debug.LogWarning("[WarpforgeVFX] `WFEffectCards.IsLocalPlayersTurn` 没挂 ⇒ "
                                   + "`IsRetaliation` 只能恒 false（原版 `AnimFXModuleBase.Initialize` 里那句 "
                                   + "`actingCard.isPlayer != IsPlayerTurn()` 算不出来）⇒ "
                                   + "`playOnRetaliation` 那道闸**永远放行**。挂法见 `WFEffectCards` 那格的注释。"
                                   + "这条警告只报一次（之后只累加 `RetaliationUnknown`）。");
                return;
            }
            WFEffectCardContext ctx;
            // warn:false —— 这一格不是「模块要卡但没人供」那条链（那条有它自己的 `MissingCount`），
            // 别把两者的计数混在一起。
            if (!WFEffectCards.TryGet(this, out ctx, false)) return;
            IsRetaliation = (ctx.actingIsPlayer != WFEffectCards.IsLocalPlayersTurn());
        }

        /// <summary>把 `entry.modules` 装配成本对象上的模块组件，并广播 `Initialize`。
        ///
        /// 两条路并存：
        ///  · **原版效果** → 这里按数据 `AddComponent` 出来（数据来自 bundle 里原版
        ///    `AnimFXController` 的字段，见 `数据/游戏数据/animfx_modules.json`）；
        ///  · **自制特效** → 在编辑器里手挂，本方法不动它们（手挂的已经在 `_modules` 里了）。
        /// ⚠️ **认不出的 kind 会打警告**（`WFModuleFactory` 里统一处理），不静默丢弃。</summary>
        void BuildModules(WFEffectEntry entry)
        {
            // 先把手挂的收进来（自制特效那条路）
            _modules.Clear();
            foreach (var m in GetComponentsInChildren<WFEffectModule>(true))
                if (m != null && !_modules.Contains(m)) _modules.Add(m);

            if (entry != null && entry.modules != null)
            {
                foreach (var def in entry.modules)
                {
                    if (def == null || string.IsNullOrEmpty(def.kind)) continue;

                    // `AnimFXController` **不是模块** —— 它就是本类（播放器）自己。
                    // 它的 `destroyTime`/`exitDestroyTime`/`preventDestroy` 早就用在寿命上了；
                    // `sounds`/`exitSounds` 2026-09-19 起也接了（见 `BuildSounds`）。
                    if (def.kind == "AnimFXController") { BuildSounds(def); continue; }

                    var host = gameObject;
                    var np = def.GetString("__node");
                    if (!string.IsNullOrEmpty(np))
                    {
                        var t = WFEffectModule.ResolvePath(transform, np, def.kind);
                        if (t != null) host = t.gameObject;   // 挂回原版那个节点，不是一律挂根
                    }
                    // 该节点上已经有同类组件（手挂的）就不重复加
                    var made = WFModuleFactory.Create(host, def);
                    if (made != null && !_modules.Contains(made)) _modules.Add(made);
                }
            }

            foreach (var m in _modules)
                if (m != null) m.Initialize(this);
        }

        void Update()
        {
            if (!autoTick) return;
            // Time.deltaTime（受 Time.timeScale 影响）：特效节奏跟着游戏走才对
            Tick(Time.deltaTime);
        }

        /// <summary>推进。Update 调它；批处理/测试也直接调它 —— 这样不用进 play 模式就能确定性地验生命周期。</summary>
        public void Tick(float dt)
        {
            if (_destroyed) return;
            _time += dt;

            // 🔴 2026-10-25：持有者离场 ⇒ 收掉自己（见 `_ownerWatch` 的注释）。
            //    判据顺序与「到寿命」无关：这条只看持有者还在不在。
            //    ⚠️ 走的是**直接销毁**（`DestroyAtLifetime`，不广播模块、不发 `OnFinished`）——
            //       因为原版这一跳的真相是「**父节点被销毁，子件跟着没**」：Unity 销毁父物体时
            //       不会调任何模块的 `Exit()`。⛔ 别改成 `DoDestroy()`：那对有模块的实例**不销毁**
            //       （`Teleport Recall` / `Tidewall Gunrig` / `CoordinatedEngagement` 都挂着模块），
            //       换过去等于没修。
            //    ⚠️ `_ownerWatch == null` 在「持有者已被销毁」时为真（Unity fake-null）——
            //       所以**不能**写成 `_ownerWatch != null &&`（那样永远进不来）。
            //    ⚠️ `_ownerWasActive`：**必须先在它活着的时候见过一次**，才把「inactive」当「离场」。
            //       否则「在持有者还没激活时播的特效」（视图构建中 / 池里取出前）会被当场收掉。
            if (_hasOwnerWatch)
            {
                bool ownerGone = _ownerWatch == null;
                if (!ownerGone)
                {
                    if (_ownerWatch.gameObject.activeInHierarchy) _ownerWasActive = true;
                    else ownerGone = _ownerWasActive;
                }
                if (ownerGone)
                {
                    _hasOwnerWatch = false;
                    OwnerReleased++;
                    DestroyAtLifetime();
                    return;
                }
            }

            // 模块先行：原版各模块自己写 `Update()`，这里收口成统一广播 ——
            // `autoTick=false`（批处理 / 确定性测试）时能整体停，不会漏掉某个模块。
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i] != null) _modules[i].ModuleTick(dt);

            // 🆕 2026-10-25：**非 AnimFX 模块**的逐帧组件（原版各自写 `Update`）。收口到这里广播，
            //    这样批处理（没有帧循环）也走同一条路 —— ⛔ 别让那些组件自己写 `Update()`。
            for (int i = 0; i < _extraTicks.Count; i++)
                if (_extraTicks[i] != null) _extraTicks[i].ExtraTick(dt);

            // 🆕 原版 `DestroyByTime`：到点**直接销毁**（与 `destroyTime` 那条同一个 `DestroyAtLifetime`）
            if (!_exiting && _destroyAfterAt >= 0f && _time >= _destroyAfterAt) { DestroyAtLifetime(); return; }

            if (!_exiting && _time >= _lifetime)
            {
                // 原版 `AnimFXController.OnEnable` 里那句 `Object.Destroy(gameObject, destroyTime)` 到点。
                // ⚠️ **不走 `Exit()`** —— 原版那条路上不调 Exit、不广播模块、不停发射、不播 exitSounds
                //    （判据见文件头）。`_destroyHandedOff` 也不挡：原版这句销毁在 OnEnable 就排好了。
                DestroyAtLifetime();
                return;
            }

            // 音效在**模块广播旁**（原版 `AnimFXController.Update` 里就是逐条 `sounds[i].Update(currentTime, …)`）。
            // ⚠️ 排在 `Exit()` **之后**：`exitSounds` 只有在 `exiting` 之后才开始判
            //    （但它的**时间基准仍是 Play**，见 `TickSounds` 里 2026-10-25 那条注）。
            TickSounds();

            if (_exiting && !_destroyHandedOff && _time - _exitAt >= _exitDelay)
            {
                DestroyNow();
            }
        }

        /// <summary>进入收尾（对应原版 `AnimFXController.Exit`）。
        /// 收尾时长 = 原版 `exitDestroyTime`（默认 3s）。
        ///
        /// <para>🔴 **2026-10-25：不再 `ps.Stop(StopEmitting)`。** 原版 `Exit` 方法体只有三件
        /// （`OnFinished?.Invoke()` → 模块 `Exit` → 排 `Destroy(gameObject, exitDestroyTime)`），
        /// **没有停发射** ⇒ 那 `exitDestroyTime` 里粒子照旧继续发。原来那句是我们自己加的，
        /// 已经在真机上表现为「收尾比原版早停」。</para></summary>
        public void Exit()
        {
            if (_destroyed || _exiting) return;
            _exiting = true;
            _exitAt = _time;

            // 🔴 2026-09-18 更正了 `OnFinished` 的**时机**：原版是在 `Exit()` **开头**就发、
            //    **早于**各模块的 `Exit()`（方法体读解见 `资料/AnimFX_18类方法体_块1.md` 的
            //    `AnimFXController.Exit`）。我们原来是等 `exitDestroyTime`（默认 3 秒）走完、
            //    在 `DestroyNow` 里才发 —— 晚了整整一个收尾时长。
            //    ⚠️ 改之前 **grep 过全工程：`OnFinished` 一个订阅者都没有**，所以这次改时机
            //    不影响任何现有行为；以后要用它的人请按「Exit 开始」这个语义来。
            FireFinished();

            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i] != null) _modules[i].Exit();
        }

        /// <summary>到寿命那一刻的销毁 —— 原版 `AnimFXController.OnEnable` 的
        /// `Object.Destroy(gameObject, destroyTime)` 到点（2026-10-25 补齐）。
        ///
        /// <para>**它和 `Exit()` 是两条路**：这里**不调 Exit、不广播模块的 Exit/DoDestroy、
        /// 不播 `exitSounds`、不停发射**，直接销毁 GameObject —— 判据逐句见文件头。</para>
        /// <para>⚠️ **不发 `OnFinished`**：原版这条路上没有任何委托调用（`OnFinished` 只在 `Exit()` 里发）。
        /// 本工程 `OnFinished` 的语义因此 = 「**`Exit()` 开始**」，不是「播完」。</para></summary>
        void DestroyAtLifetime()
        {
            if (_destroyed) return;
            // `IsExiting` 在这条路上也置真：对象已经走到头了，`IsPlaying` 必须为 false
            // （外部探针，例如 `UserEffectTools` 的「过了寿命 ⇒ 不在播」那条，就是按这两个读口判的）。
            _exiting = true;
            _destroyed = true;
            Active.Remove(this);

            // ⚠️ 与 `DestroyNow` 的同一个理由：**不销毁材质**（binder 的材质走静态共享缓存）。
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        /// <summary>立刻销毁（**强制** —— 不理会「销毁权交给模块」那条）。</summary>
        public void Kill()
        {
            if (_destroyed) return;
            _exiting = true;
            DestroyNow(true);
        }

        /// <summary>🆕 2026-10-23（A1 R23）：原版 `AnimFXController.DoDestroy` ——
        /// **没有模块 ⇒ 销毁自己；有模块 ⇒ 只广播、自己不动**（销毁权交给模块）。
        /// 调用点（原版）= `BattleManager.RemoveDestroyableAnims`。
        /// 🔴 **它和 `Exit()` 的计时销毁是两条路**（判据 = `Battle/AnimFXController.cs:318-345`）：
        /// `Exit()` 到点**一定**销毁；本方法才有「交给模块」那一跳。
        /// </summary>
        public void DoDestroy()
        {
            if (_destroyed || _destroyHandedOff) return;

            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i] != null) _modules[i].DoDestroy();

            // 🔴 **2026-10-25 订正（完全复刻第 1 轮 · D 域）**：判据从「有没有模块**接管**」改回原版的
            //    「**有没有模块**」—— `AnimFXController.cs:74-105`：循环里每命中一个模块就 `obj = 1`，
            //    末尾 `if (obj == null) Destroy(gameObject)` ⇒ **只要挂了模块就只广播、controller 自己死不了**。
            //    （原来按 `TakesOverDestroy` 判 ⇒ 有模块但没人接管时我们会销毁、原版不会。）
            //    ⚠️ `TakesOverDestroy` 这个口还在（`WFEffectModule` 里），但**不再是这一跳的判据** ——
            //       它现在只用于诊断/自检读「有没有模块声称会自己收」。
            if (_modules.Count > 0)
            {
                // 销毁权在模块那边：**不置 `_destroyed`** —— `Tick` 要继续给模块 `ModuleTick` 跑倒计时。
                _destroyHandedOff = true;
                FireFinished();
                return;
            }
            DestroyNow(true, alreadyBroadcast: true);   // 一个模块都没有 ⇒ 原版兜底销毁
        }

        /// <summary>🆕 **2026-10-24（`A31`）：原版 `SwarmVFXController.OnAnimationEndEvent` 的接收口。**
        ///
        /// <para>**为什么要有它**：`Swarm Card Merge VFX.prefab` 的 `Animation` 组件
        /// （`m_PlayAutomatically = 1`）播 `Swarm Triat Merge.anim`，而那份 clip 在 **t = 1.2 s**
        /// 带一个动画事件 `functionName: OnAnimationEndEvent`。原版那个方法体
        /// （`SwarmVFXController__OnAnimationEndEvent.c`，逐句）做三件事：
        /// ① 调回调（`CardScript.ExecuteSwarm` 传进来的闭包）· ② `StopAllCoroutines` ·
        /// ③ `Object.Destroy(gameObject, 5f)` —— **不是立即销毁**（字面量现读见 `SwarmAnimationEnded`）。
        /// 我们这边**没有** `SwarmVFXController`（那份 prefab 的 `m_Script` 反查出来只有 binder）
        /// ⇒ 这一拍原来既**没有接收者**（Unity 会打一条 "has no receiver"）**也不销毁**，
        /// 对象一路活到 `natural = 3.5 s` 才被计时路收掉｜🔴 **2026-10-26（B8_AB）就地订正**：这里原来写「⇒ 比原版多活 2.3 s」——**那条结论错了**（它按「原版立即销毁」推；原版是 `Destroy(go, 5f)`）。</para>
        ///
        /// <para>**为什么收在这里、而不是新加一个组件**：`WarpforgeEffectPlayer` 是
        /// `WarpforgeEffectPlayer.Play()` 给**每一个**实例都挂上的（见 `Play` 末尾
        /// `if (p == null) p = go.AddComponent<WarpforgeEffectPlayer>()`），而 legacy `Animation`
        /// 的事件是**发给同一 GameObject 上的所有组件**（SendMessage 语义）⇒ 这一个方法就够，
        /// ⛔ 不必去改 prefab 的 YAML（那要 Unity 才能可靠地落盘）。</para>
        ///
        /// <para>⚠️ **影响面已实测**：全仓 `.anim` 里带 `functionName: OnAnimationEndEvent` 的
        /// **只有 `Swarm Triat Merge.anim` 一条**（`grep -rn functionName --include=*.anim`
        /// 现读：另有 `AnimationEndEvent` / `AnimationResurrectionEnd` 等**别的名字**，不撞）。
        /// ⇒ 这个方法的效果面 = 那 1 件。以后若再有 clip 用这个名字，它们也会走同一条收尾语义
        /// （= 原版 `SwarmVFXController` 的语义），这正是我们要的默认。</para>
        ///
        /// <para>⚠️ 回调（①）那一跳我们**没有等价物**：原版那个闭包最后接到
        /// `BattleManager.FinishResolvingAction` / `DestroyUnitSilent`（在 `Battle/**`，
        /// 不在本批白名单）⇒ 本方法只做 ②③ 里的「销毁自己」那一半。接线时若要在
        /// 动画结束之后再合并数值，得由战斗侧另挂。</para></summary>
        public void OnAnimationEndEvent()
        {
            // 🔴 2026-10-26（B8_AB）：原版 ③ 是 `Destroy(gameObject, 5f)`（**5 秒尾巴**）⇒ 走 `SwarmAnimationEnded`，
            // ⛔ 别再退回「立即 `DoDestroy()`」（那样比原版少活 5 秒，见该方法里的判据与字面量）。
            SwarmAnimationEnded();
        }

        /// <param name="force">保留参数只为让调用点读起来明确；**Exit 计时路与 Kill 一律销毁**。</param>
        /// <param name="alreadyBroadcast">`DoDestroy()` 已经广播过模块，别再广播第二遍。</param>
        void DestroyNow(bool force = false, bool alreadyBroadcast = false)
        {
            if (_destroyed) return;

            // 先让模块收尾（原版 `DoDestroy` 广播；基类是空实现，只有 1 个类重写了它）
            if (!alreadyBroadcast)
                for (int i = 0; i < _modules.Count; i++)
                    if (_modules[i] != null) _modules[i].DoDestroy();

            _destroyed = true;
            Active.Remove(this);

            // 正常路径上 `Exit()` 开头已经发过；`Kill()` 直接过来时在这里补发（只发一次）
            FireFinished();

            // ⚠️ 不销毁材质。binder 重建出来的材质走的是**静态共享缓存**
            //    （WarpforgeEffectBinder.Cache，键是「材质名|原 shader 名」），
            //    在这里 Destroy(material) 会把同时在场的别的实例一起打成粉红。
            //    缓存本身是有界的（条目数 = 不同材质的数量，不随播放次数增长）。
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        // ==================================================================
        //  音效：`AnimFXController.sounds` / `exitSounds`
        //  —— ✅ **2026-09-19 接线**（原来只是「记账 + 报警」）
        //
        //  原版语义（`AnimFXController__Update.c:31-70` + `PlaySoundOnTime__Update.c:29-41`）：
        //    · `sounds[i]` = 「特效开始后第 `time` 秒播一条」，`repeat` 决定播几次：
        //        **`n = repeat ? loops : 1`**（不是 `repeat ? 无限 : 1`）
        //    · 第 k 次（0 基）的判据：`elapsed >= k * timeInterval + time`
        //    · `is2d` 决定 2D / 3D（实测 889 条 `is2d=0` = **3D 定位音**，48 条是 2D）
        //    · `exitSounds[i]` 同理，但 `elapsed` 从 **`Exit()` 那一刻**起算
        //  实测规模：`sounds` 有 `sound` 值的 **934 条** + `exitSounds` **7 条** = **941 条**
        //  （⚠️ 槽位是 937：有 3 条只有 is2d/loops/repeat/time/timeInterval、没有 `sound` 键 ——
        //    那 3 条**没有声音可播**，不在这 934 里）。
        //  `sound` 的值是 `@asset:MonoBehaviour:<cue 名>`，cue 里才是真 clip（见 `WFSoundBank`）。
        // ==================================================================

        struct SoundSlot
        {
            public string cue;
            public float time;        // 起始延迟（秒）
            public float interval;    // 重复间隔（秒）
            public int total;         // 总共播几次（`repeat ? loops : 1`）
            public bool is2d;
            public bool onExit;       // 来自 `exitSounds[]`（只在 `exiting` 之后才开始判，但**时间基准仍是 Play**）
            public int played;        // 已经播了几次（**用 `while` 追帧，不丢拍**）
        }

        readonly List<SoundSlot> _sounds = new List<SoundSlot>();

        /// <summary>这个实例挂了多少条音效条目（自检用）。</summary>
        public int SoundSlotCount { get { return _sounds.Count; } }

        /// <summary>累计**解不出 cue** 的条数（表里没有 / 名字对不上）。
        /// ⚠️ 2026-09-19 语义变了：这里原来记的是「**还没接线**的条数」，接线之后它变成
        /// **真出问题**的计数 —— 不为 0 才该有人去看。（`AnimFXCheck` 读它。）</summary>
        public static int UnwiredSoundCues { get; private set; }

        /// <summary>累计解不出 cue 的名字（去重，自检打出来好定位）。</summary>
        static readonly HashSet<string> _badCues = new HashSet<string>();
        public static string BadCueList { get { return string.Join(" / ", _badCues); } }

        /// <summary>测试用：清掉累计计数（表换过之后自检要重来一遍）。</summary>
        public static void ResetSoundDiagnostics() { UnwiredSoundCues = 0; _badCues.Clear(); }

        void BuildSounds(WFModuleDef def)
        {
            if (def == null) return;
            BuildSoundList(def, "sounds", false);
            BuildSoundList(def, "exitSounds", true);
        }

        void BuildSoundList(WFModuleDef def, string key, bool onExit)
        {
            int n = def.CountList(key);
            for (int i = 0; i < n; i++)
            {
                string p = key + "[" + i + "].";
                string refv = def.GetString(p + "sound");
                if (string.IsNullOrEmpty(refv)) continue;    // 这条槽位没有声音可播（实测 3 条）

                string kind, type, rest;
                WFModuleDef.SplitRef(refv, out kind, out type, out rest);
                if (kind != "asset" || type != "MonoBehaviour" || string.IsNullOrEmpty(rest))
                {
                    UnwiredSoundCues++;
                    _badCues.Add(refv);
                    Debug.LogWarning($"[WarpforgeVFX] 《{name}》的 `{p}sound` 形状认不出：`{refv}`");
                    continue;
                }

                bool repeat = def.GetInt(p + "repeat") != 0;
                int loops = def.GetInt(p + "loops");
                _sounds.Add(new SoundSlot
                {
                    cue = rest,
                    time = def.GetFloat(p + "time"),
                    interval = def.GetFloat(p + "timeInterval"),
                    total = repeat ? loops : 1,
                    is2d = def.GetInt(p + "is2d") != 0,
                    onExit = onExit,
                });
            }
        }

        /// <summary>把到点的条目播掉。`while` 而不是 `if` —— 一帧跨过好几个区间时不丢拍。</summary>
        void TickSounds()
        {
            for (int i = 0; i < _sounds.Count; i++)
            {
                var s = _sounds[i];
                if (s.played >= s.total) continue;

                // 🔴 **2026-10-25 订正时间基准**：原版 `AnimFXController.Update` 对 `sounds[]` 与
                //    `exitSounds[]` 传的是**同一个** `currentTime`（`+0x60`，从 Play 起算、Exit 不复位），
                //    `PlaySoundOnTime.Update` 里判据是 `elapsed < played * timeInterval + time` ⇒
                //    **`exitSounds[i].time` 也是「从 Play 起算的第 T 秒」**，只是「要不要开始判」由 `exiting` 把门。
                //    原来这里写的是 `_time - _exitAt`（从 Exit 起算）—— 与 `AnimFX_实现与接线.md` §11.4
                //    那句「`exitSounds` 的 `time` 也是从 `Play` 起算」矛盾，**代码这一侧是错的**（7 条实例）。
                float elapsed = s.onExit ? (_exiting ? _time : -1f) : _time;
                if (elapsed < 0f) continue;

                while (s.played < s.total && elapsed >= s.time + s.played * s.interval)
                {
                    PlayOneSound(ref s);
                }
                _sounds[i] = s;
            }
        }

        void PlayOneSound(ref SoundSlot s)
        {
            s.played++;
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(s.cue, out cue))
            {
                if (_badCues.Add(s.cue))                       // 只报一次，别每帧刷
                    Debug.LogWarning($"[WarpforgeVFX] 《{name}》的音效 cue `{s.cue}` 在音效表里没有 —— " +
                                     "这一条不会响。跑 `工具/import_original_sfx.py` 重生成表。");
                UnwiredSoundCues++;
                return;
            }
            WFSoundPlayer.Play(cue, transform.position, s.is2d);
        }

        /// <summary>`OnFinished` **只发一次**。正常路径在 `Exit()` 开头发（与原版一致）；
        /// `Kill()` 绕过 `Exit` 直接销毁时，在 `DestroyNow` 补发。</summary>
        void FireFinished()
        {
            if (_finishedFired) return;
            _finishedFired = true;
            var cb = OnFinished;
            OnFinished = null;
            if (cb != null) cb();
        }

        void OnDestroy()
        {
            // 手工 Destroy 掉整个对象（比如切场景）时也要从表里摘干净，否则 ActiveCount 会虚高
            if (!_destroyed)
            {
                _destroyed = true;
                Active.Remove(this);
            }
        }

        /// <summary>诊断用：把当前活着的实例列出来（谁没被销毁一目了然）。</summary>
        public static string DescribeActive()
        {
            if (Active.Count == 0) return "没有活着的特效";
            var sb = new System.Text.StringBuilder($"活着的特效 {Active.Count} 个：");
            foreach (var p in Active)
                if (p != null) sb.Append($"\n  {p.EffectName}  存活 {p._time:F2}s / 预计 {Describe(p.ExpectedLifetime)}");
            return sb.ToString();
        }

        static string Describe(float v)
        {
            return float.IsPositiveInfinity(v) ? "不自动收" : $"{v:F2}s";
        }

        // ==================================================================
        //  原版 `SwarmVFXController.OnAnimationEndEvent` 的第 ③ 跳（5 秒尾巴）
        // ==================================================================
        //  判据 🥇 `d:/2/tools/decomp_full/SwarmVFXController__OnAnimationEndEvent.c:3-27`：
        //      `onFinish?.Invoke()` → `StopAllCoroutines()` → `UnityEngine_Object__Destroy(go, DAT_1834b2df4, 0)`
        //  字面量现读（`工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2df4`）：
        //      4B float = **5.0** ✅（8B double = 5.3568e-315 ⇒ 不是字面量）
        //  ⇒ 动画事件（`Swarm Triat Merge.anim` t = 1.2 s）之后**还要活 5 秒**（≈ 6.2 s 才没），不是立即销毁。
        //  ⚠️ 本 effect 的自然寿命 = `natural 3.5 s`（**估的**：原版那份 prefab 没有 `AnimFXController`
        //     ⇒ 原版根本没有计时销毁；`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 里
        //     `Swarm Card Merge VFX` = `destroyTime -1 / modules []`）⇒ 不把 `_lifetime` 抬起来的话
        //     3.5 s 会先到、5 秒尾巴等于没接（「改了但看不出来」那类静默失效）。
        //  ⚠️ 与原版仍差两处（如实记，⛔ 别当成已对齐）：① `onFinish` 回调无对应物
        //     （原版那个闭包接在 `CardScript.ExecuteSwarm` / `Battle/**` 侧）；② `SafeExit` 看门狗
        //     （4 s 内没收到事件 ⇒ `LogError` + 回调 + 5 s 尾巴）没做 —— 我们靠自然寿命兜底
        //     （账 `特效动画.tsv:405` 的 D 依据就是这一条）。
        //  观测量（留给真 Play；本代理不跑 Unity）：`WarpforgeEffectPlayer.ActiveCount` 在 t ≈ 1.2 s
        //     动画事件之后应保持 1 到 t ≈ 6.2 s（原版同刻同一口径）。
        void SwarmAnimationEnded()
        {
            if (_destroyed) return;
            float tail = SwarmAnimationEndTail;
            if (_lifetime < _time + tail) _lifetime = _time + tail;
            DestroyAfter(tail);
        }

        /// <summary>原版 `SwarmVFXController.OnAnimationEndEvent` 里 `Destroy(gameObject, 5f)` 的 **5.0**
        /// （`DAT_1834b2df4` 现读 4 字节 float）。</summary>
        public const float SwarmAnimationEndTail = 5f;
    }
}
