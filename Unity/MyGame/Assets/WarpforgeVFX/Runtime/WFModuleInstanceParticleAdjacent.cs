// WFModuleInstanceParticleAdjacent.cs — 原版 `AnimFXInstanceParticleAdjacent` 的对应物
//   **1 实例 / 1 效果**：`BulletImpact_deathspinner_arc_alt`
//
// 语义出处（**逐方法体**，不是按名字猜）
// ----------------------------------------------------------------
//   · `资料/AnimFX_18类方法体_块2.md` §6（4 个方法体）
//   · 方法体原件（**逐行读过**）：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`
//     `AnimFXInstanceParticleAdjacent__{Initialize,DelayActivation,ExecuteEffect}.c`
//   · 报错原文逐字核过 `d:/2/tools/all_strings.txt`：
//     `:18021` `[ERROR] Roope, delete the Module for adjacent VFX if you are not using it`
//   · 数据：`delay = 1.6` · `playOnRetaliation = 0` ·
//     `cardAnim.m_AssetGUID = df138b834086ffe43a025ca6ec25d46f`
//
// 照方法体还原了什么
// ----------------------------------------------------------------
//   · `Initialize`（`__Initialize.c:22-46`）：
//     ① base（`isRetaliation` 是基类在这儿算的：`IsPlayerTurn() XOR actingCard.isPlayer`）
//     ② **资产引用无效** → `Debug.LogError("[ERROR] Roope, delete the Module for adjacent VFX
//        if you are not using it")` + **整条 return**
//     ③ `if (playOnRetaliation || !isRetaliation)`：`delay > 0` → 起一个**延时协程**；
//        否则**直接** `ExecuteEffect()`
//        ⇒ **跳过条件只有一条**：`playOnRetaliation == false` **且** 本次是反击
//     ④ 那个 bool 是 `IsRetaliation`：我们这边它挂在 `WarpforgeEffectPlayer.IsRetaliation`
//        （播放器上有，**由表现层在 `Play()` 之后设**；没人设就是 false —— 见
//        `WarpforgeEffectPlayer.cs:79-83`）
//   · `ExecuteEffect`（`__ExecuteEffect.c:42-104`）的**原版做法，逐字记在这里**（下游照这个接）：
//     `units = BattleManager.GetAdjacentUnits(controller.targetCard)` →
//     `asset = cardAnim.Load(cacheGroup:1)` →
//     `foreach (unit in units)` →
//     `BattleManager.CreateAnimFromAnimFxController(asset.animInfo,
//        controller.actingCard.transform, unit.transform, unit.transform,
//        controller.actingCard, unit, null, false)`
//     ⇒ **起点 = 出手卡，终点/朝向 = 那个相邻单位**，逐个单位各来一份
//   · 「延时中」的**时序**：原版是协程，**`Exit` 不停它**（协程只在对象销毁时断）
//     ⇒ 我们在 `ModuleTick` 里计时，`Exit` 之后**继续**计（照原版）。
//
// 🔴 我们自己定的（**原版不是这样**）
// ----------------------------------------------------------------
//   1. **延时的实现**：原版 `StartCoroutine(DelayActivation())`。⚠️ 那个状态机的 `MoveNext`
//      **没被反编译**（块2 §6 记着：RVA `6796352` 有名字、方法体没有）⇒
//      「等的是不是 `WaitForSeconds(delay)`」**没读到**，我们**按 `delay` 字段推**成
//      `delay` 秒后触发。**这是推断，不是方法体。**
//   2. 「资产引用有效」的判据在我们这边是 `cardAnimGuid` / `prefabName` 非空
//      （原版是 `AssetReferenceTyped<CardAnim>` 的虚调用，方法名未核实，块2 §6 ②）。
//   3. **播什么、在谁身上播 = 交给下游**（`OnExecute` 钩子）。三条原因，**都不是我们偷懒**：
//      ① `cardAnim` 是 **Addressables 资产引用**，我们**没有 Addressables**
//         （块2 §6 末）；那个 GUID 属于 `aeldarisaimhanncardanims_assets_all.bundle`
//         （查法：`数据/索引/guid_map.tsv:60`），**工程里没有这个 bundle、也没有名字映射**（记为 `None`/`?`）
//         ⇒ 连「它本该是哪份动画」都查不到；
//      ② 相邻单位在**规则引擎的棋盘**上（同排 `slot±1`），**VFX 层不认识牌局**（这条线上
//         的分层：卡牌可以不知道特效，特效也不该知道棋盘）；
//      ③ 原版那份「把一份 CardAnim 从 A 播到 B」的接口（`BattleManager.CreateAnimFromAnimFxController`）
//         我们**没有对应物**。
//      ⇒ 我们保留**这个模块自己的那部分**（反击闸门 + 延时 + 「资产/钩子缺了要出声」），
//        把「逐个相邻单位各播一份」交给 `OnExecute`（挂法一行）。
//        ⚠️ **它自己一个人不会播任何东西**（今天没接 = 每次都会 LogWarning，计数可见）。
//
// 🔴 没还原的
// ----------------------------------------------------------------
//   ① **起点/终点**：原版是「出手卡 → 那个单位」（见上面逐字记的 8 个实参）；
//      我们这边只有 `WarpforgeEffectPlayer.Play(name, parent, localPosition)`，
//      **表达不了「从 A 到 B」**（那趟位移在 CardAnim 资产内部）⇒ 由 `OnExecute` 全权决定。
//   ② 「谁何时播这份特效」的上层接线（`VfxMap` 目前只支持「一个事件 → 一个特效」，
//      这条要的是「一个事件 → 对每个相邻单位各播一份」）—— 见块2 §6「Unity 侧重写」。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXInstanceParticleAdjacent")]
    public class WFModuleInstanceParticleAdjacent : WFEffectModule
    {
        // ---- 原版字段（块2 §6）----
        /// <summary>原版 `cardAnim.m_AssetGUID`（Addressables 资产引用）。</summary>
        [Tooltip("原版 cardAnim 的资产 GUID。空 = 原版就会报 Roope 那条错并整条退出")]
        public string cardAnimGuid = "";

        /// <summary>手挂用：等价于「资产引用有效」的另一个形状（原版没有这个字段）。</summary>
        [Tooltip("手挂用：要播的特效名（原版是 CardAnim 资产，我们没有那套）")]
        public string prefabName = "";

        [Tooltip("原版 `delay`（数据里 1.6s）：延时多久触发")]
        public float delay;

        [Tooltip("原版 `playOnRetaliation`（数据里 0）：false 时**反击**不触发")]
        public bool playOnRetaliation;

        /// <summary>原版 `cardAnim.m_SubObjectName` / `m_SubObjectType`（数据里都是空串，先留着）。</summary>
        [HideInInspector] public string subObjectName = "";
        [HideInInspector] public string subObjectType = "";

        // ---- 🔑 下游钩子 ----
        /// <summary>
        /// **延时到点后，由表现层「逐个相邻单位各播一份」。** 参数 = (播放器, 本模块)。
        /// 实现时照原版那 8 个实参来（见文件头）：`GetAdjacentUnits(目标卡)` →
        /// `foreach (unit)` → **起点 = 出手卡、终点/朝向 = unit**。
        /// 卡上下文见 `WFEffectCards`（`WFModuleTransformModifier.cs` 头注释里有挂法）。
        /// 没挂 = 每次都会 `LogWarning` 并计数（**不静默**）。
        /// </summary>
        public static Action<WarpforgeEffectPlayer, WFModuleInstanceParticleAdjacent> OnExecute;

        // ---- 诊断 ----
        /// <summary>`ExecuteEffect` 被触发了几次（挂不上钩子时也计 —— 这就是「它该响却没响」的证据）。</summary>
        public static int Executed;
        /// <summary>没有下游、被丢掉几次。</summary>
        public static int DroppedExecutions;
        /// <summary>资产引用为空（原版 Roope 那条）几次。</summary>
        public static int MissingAsset;

        public static void ResetDiagnostics()
        {
            Executed = 0; DroppedExecutions = 0; MissingAsset = 0; _warnedDropped = false;
        }
        static bool _warnedDropped;

        float _timer;              // > 0 = 正在延时
        bool _fired;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;
            cardAnimGuid = def.GetString("cardAnim.m_AssetGUID");
            subObjectName = def.GetString("cardAnim.m_SubObjectName");
            subObjectType = def.GetString("cardAnim.m_SubObjectType");
            delay = def.GetFloat("delay");
            playOnRetaliation = def.GetBool("playOnRetaliation");
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            _timer = 0f;
            _fired = false;

            // ② 资产引用无效 → 原版那条开发留言 + 整条 return
            if (string.IsNullOrEmpty(cardAnimGuid) && string.IsNullOrEmpty(prefabName))
            {
                MissingAsset++;
                Debug.LogError("[WarpforgeVFX] [ERROR] Roope, delete the Module for adjacent VFX "
                             + "if you are not using it"
                             + $"（效果 {EffectName}）—— 原版就是这条报错并**整条退出**"
                             + "（一个单位都不会播）。");
                return;
            }

            // ③ 唯一的跳过条件：`playOnRetaliation == false` 且本次是反击
            bool isRetaliation = Controller != null && Controller.IsRetaliation;
            if (!playOnRetaliation && isRetaliation)
            {
                // ⚠️ 注意：`IsRetaliation` **没人设的话它就是 false**（`WarpforgeEffectPlayer.cs:79-83`），
                //    也就是说**今天这条闸门实际上是「永远放行」**。
                return;
            }

            if (delay > 0f) _timer = delay;      // 延时（见文件头「我们自己定的」1）
            else ExecuteEffect();
        }

        public override void ModuleTick(float dt)
        {
            // 原版是协程：**`enabled = false` 或 `Exit()` 都不会停它**（只有对象销毁才断）
            // ⇒ 这里**不看** `enabled` / `IsExiting`，只在 `DoDestroy` 之后自然不再被广播。
            if (_fired || _timer <= 0f) return;
            _timer -= dt;
            if (_timer > 0f) return;
            _timer = 0f;
            ExecuteEffect();
        }

        /// <summary>原版 `ExecuteEffect()`：`GetAdjacentUnits(targetCard)` → 逐个播一份。</summary>
        public void ExecuteEffect()
        {
            if (_fired) return;
            _fired = true;
            Executed++;

            if (OnExecute == null)
            {
                DroppedExecutions++;
                if (DroppedExecutions <= 5)
                {
                    Debug.LogWarning($"[WarpforgeVFX] `{gameObject.name}`（效果 {EffectName}）"
                                   + "该「逐个相邻单位各播一份」了，但 `OnExecute` 没挂 ⇒ **什么都没播**。"
                                   + "原版那一步是 `BattleManager.GetAdjacentUnits(targetCard)` + "
                                   + "`CreateAnimFromAnimFxController(animInfo, actingCard.transform, "
                                   + "unit.transform, unit.transform, actingCard, unit, null, false)`"
                                   + "（起点=出手卡、终点/朝向=那个单位）。"
                                   + $"该效果同时挂着 ScreenShake / Tween / TransformModifier 等模块，"
                                   + "少了这一份是**看得见的差别**。这条警告只报前 5 次。");
                }
            }
            else
            {
                OnExecute(Controller, this);
            }
        }
    }
}
