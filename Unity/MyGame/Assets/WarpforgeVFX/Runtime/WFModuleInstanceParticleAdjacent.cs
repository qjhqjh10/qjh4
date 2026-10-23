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
//     逐个单位各来一份。
//     🔴 **2026-10-22（`W64` · B18）就地订正 —— 起点/终点【不是】「出手卡 / 那个相邻单位」**：
//     那串实参里两个 `Transform` 只是给「别的 option」用的；本效果的
//     `startPosOption = endPosOption = 1`，而 `GetAnimTransform` 的 **case 1/2** 取的是
//     **第二个 CardScript 的 `CardUI.effectsAnchor`**：
//       · `BattleManager__GetAnimTransform.c:64-67,137-138` —— `case 1/2` → `*(param_4 + 0x290)` → `*(… + 0x1a0)`；
//       · `CardScript.CardUI`（`<CardUI>k__BackingField`）= **+0x290**（`dump.cs:35486`）；
//       · `BattleCardUI.effectsAnchor` = **+0x1A0**（`dump.cs:26190`）；
//       ⇒ **起点 = 终点 = 那个相邻单位的 `effectsAnchor`**。
//     · 相邻单位取的是 **`controller.targetCard`** 的（`AnimFXController.targetCard` = +0x58，`dump.cs:47988`；
//       调用 `AnimFXInstanceParticleAdjacent__ExecuteEffect.c:43-44`）—— **不是出手卡的邻居**。
//     · `AnimInfo`（`Deathspinner Slice Card Target.json`）逐字段：`timeAtStartPos = 0.5` ·
//       `timeMoving = 0` · `timeAtEndPos = 0` · `shouldMoveVFX = 0` ⇒ **不飞**：
//       **在锚点上停 0.5 s**，然后结束。
//   · 「延时中」的**时序**：原版是协程，**`Exit` 不停它**（协程只在对象销毁时断）
//     ⇒ 我们在 `ModuleTick` 里计时，`Exit` 之后**继续**计（照原版）。
//     ✅ **2026-10-22 坐实**（不再是我方的推断）：`AnimFXInstanceParticleAdjacent._DelayActivation_d__4__MoveNext.c`
//     的状态机读出来了 —— `new WaitForSeconds(this.delay(+0x40))` → 让出 → 下一拍 `ExecuteEffect()`
//     ⇒ **`delay` 的语义 = `WaitForSeconds(delay)`**（先前提的「按字段名推」已可撤）。
//
// 🔴 我们自己定的（**原版不是这样**）
// ----------------------------------------------------------------
//   1. **延时的实现**：原版 `StartCoroutine(DelayActivation())`。
//      ✅ **2026-10-22 已坐实**：那块状态机的 `MoveNext` 现在**反编译出来了**
//      （`AnimFXInstanceParticleAdjacent._DelayActivation_d__4__MoveNext.c`：`WaitForSeconds(this.delay)` →
//      让出 → `ExecuteEffect()`）⇒ 我们「`delay` 秒后触发」**与方法体一致**，不再是推断。
//      ⚠️ 旧记录（保留痕迹）：「那个 `MoveNext` 没被反编译（RVA 6796352 有名字、方法体没有）」—— 已过期。
//   2. 「资产引用有效」的判据在我们这边是 `cardAnimGuid` / `prefabName` 非空
//      （原版是 `AssetReferenceTyped<CardAnim>` 的虚调用，方法名未核实，块2 §6 ②）。
//   3. **播什么、在谁身上播 = 交给下游**（`OnExecute` 钩子）。两条原因，**都不是我们偷懒**：
//      ① ~~`cardAnim` 是 Addressables 资产引用，我们**没有 Addressables** ⇒ 连「它本该是哪份动画」都查不到~~
//         🔴 **2026-10-01 更正：这条【查得到】了，而且【不需要导入任何资产】。**
//         原件里写的是「工程里没有这个 bundle、也没有名字映射（记为 None/?）」——
//         **「本地没有」是错的**：`bundle_aeldarisaimhanncardanims_assets_all` 在本地（66 份），
//         GUID 就在它 `AssetBundle/AssetBundle_1.json` 的 `m_Container` 里。
//         更要紧的是：**CardAnim 是纯数据 MonoBehaviour**（`m_GameObject = {0,0}`、该包 0 个 GameObject、
//         全文只有 4 个 PPtr、**贴图/材质/mesh/clip 一个都没有**）⇒ **它要引用的东西早就进工程了**。
//         整条链**逐跳核过**（2026-10-01）：
//           `cardAnimGuid = df138b834086ffe43a025ca6ec25d46f`（CardAnim 在本包 `m_Container` 的键）
//           → PathID `-1978868168703704829` → `MonoBehaviour/Deathspinner Slice Card Target.json`
//           → 它的 `animInfo.animAdressable.m_AssetGUID = a36694584a38cdc4aa4b051afcb79afb`
//           → `数据/索引/anim_address_map.json` 的 `guid_to_asset[…]`
//             = GameObject **`Deathspinner Cut Effect`**（`bundle_battleprefabs_vfxandmisc_assets_all`）
//           → **工程里已有**：`Assets/WarpforgeVFX/Prefabs/Deathspinner Cut Effect.prefab`、**已进库**。
//         ⏭ **真正缺的只有一跳**：`anim_address_map.json` 的 `cardanim_to_asset` 是**按【名字】做键**的
//         （1099 条 / **0 条是 32 位 GUID**），而数据里存的是 **GUID** ⇒ 生成器补一条
//         「CardAnim 容器 GUID → 名字」索引（改 `工具/gen_anim_address_map.py`），
//         再由 `工具/gen_animfx_modules.py` 把 **`prefabName`** 一并写出来
//         （该字段现在恒空、`Configure()` 也不读它）——**然后才轮到接 `OnExecute`**。
//         ⚠️ 仍未变的两条：本 prefab 唯一材质用的 `Shader Graphs/Doomweaver effect` **我们没有**
//         ⇒ 材质仍是近似/占位；下面 ② ③ 两条理由**照旧成立**。
//      ② 相邻单位在**规则引擎的棋盘**上（同排 `slot±1`），**VFX 层不认识牌局**（这条线上
//         的分层：卡牌可以不知道特效，特效也不该知道棋盘）；
//      ③ 原版那份「把一份 CardAnim 从 A 播到 B」的接口（`BattleManager.CreateAnimFromAnimFxController`）
//         我们**没有对应物**。
//      ⇒ 我们保留**这个模块自己的那部分**（反击闸门 + 延时 + 「资产/钩子缺了要出声」），
//        把「逐个相邻单位各播一份」交给 `OnExecute`（挂法一行）。
//        ⚠️ ~~**它自己一个人不会播任何东西**（今天没接 = 每次都会 LogWarning，计数可见）。~~
//        🔴 **2026-10-23 订正**：`OnExecute` 已于 2026-10-01 在 `BattleDriver.cs:2124` 挂上
//        ⇒ 今天**会播**；`DroppedExecutions` 只在「别的宿主（自检 / 无对局）」里才会涨。
//
// 🔴 没还原的
// ----------------------------------------------------------------
//   ① **起点/终点**：🔴 **2026-10-22 订正** —— 原版**不是**「出手卡 → 那个单位」，而是
//      **那个相邻单位的 `BattleCardUI.effectsAnchor`（+0x1A0）上、原地停 0.5 s**（判据见上）。
//      这反而**简化**了：不需要「从 A 到 B」那趟位移（`timeMoving = 0` / `shouldMoveVFX = 0`）。
//      我们这边现在只差**一个「锚点在哪」的输入**：`WarpforgeEffectPlayer.Play(name, parent, localPosition)`
//      能表达「挂在某个 transform 的某个 localPos」⇒ 接的时候应该传
//      **那张相邻单位卡的 `CardView` 的锚点**（我们侧对应物 = 卡节点本身 / `AnimAnchors` 那一族），
//      `localPosition = Vector3.zero`。
//   ② 「谁何时播这份特效」的上层接线（`VfxMap` 目前只支持「一个事件 → 一个特效」，
//      这条要的是「一个事件 → **对目标卡的每个相邻单位**各播一份」）—— 见块2 §6「Unity 侧重写」。
//      ⛔ 注意主语：是**目标卡**的相邻单位（`controller.targetCard` = +0x58），不是出手卡的。
//      🔴 **2026-10-23 就地订正（铁律 5 · `普查产出_1022/A1_自造与兜底_特效动画.md` §四 D2）**：
//        这一条**已经接上了**（2026-10-01）：`BattleDriver.cs:2124` 挂 `OnExecute` ——
//        按**被打那张卡**的一方/格号取 `BoardSpec.AdjacentSlots`，对每个相邻视图
//        `WarpforgeEffectPlayer.Play(module.prefabName, 相邻单位.transform, Vector3.zero)`。
//        原文那句「运行时接线没做」是写在接线之前的，之后没回来改（错因同 `D1`）。
//      ⚠️ **仍差的两点**（都不影响「播不播」，影响「播在哪 / 停多久」）：
//        a. 原版挂在 `BattleCardUI.effectsAnchor`(+0x1A0) 上，我们挂在**卡节点本身** ——
//           我们这一侧的对应物是 `AnimAnchors` 那一族，还没接（要 `CardView` 的锚点句柄）；
//        b. 原版那份 `AnimInfo` 是 `timeAtStartPos = 0.5` / `timeMoving = 0`（停 0.5 s 再结束），
//           我们现在靠 prefab 自己的时长。数据侧已落地 `数据/游戏数据/adjacent_vfx_animinfo.json`
//           （起终点 = 相邻单位的 `effectsAnchor` · 不飞 · 停 0.5 s），
//           ⚠️ **但它到 2026-10-23 为止【还没有任何运行期消费者】**（全仓 `grep adjacent_vfx` 只命中本文件的注释，
//           `CardPresentation/Resources/` 下也没有它的副本）⇒ **这一半是待接，不是「已接在别处」**。
//           接的时候要连带 `effectsAnchor` 一起（a 与 b 是同一处改动：`Play` 要能指定锚点 + 时长）。
//   ✅ `W64` · B18 的另外两半：Doomweaver 材质 —— `Shader Graphs/Doomweaver effect` 已按 Ruri 字节码
//      重写成 `WarpforgeVFX/FX/DoomweaverEffect`（`WarpforgeShaderMap.cs` 的 `Replacements`，2026-10-22 `B11`）；
//      「一事件→逐单位」—— 见上 ② 已接。
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

        /// <summary>要播的那个 prefab 名。
        /// 🔴 **2026-10-01 起这个字段是【数据填的】**（原来只当「手挂用」）——
        /// 生成器 `工具/gen_animfx_modules.py` 拿 `cardAnim.m_AssetGUID` 去
        /// `数据/索引/anim_address_map.json` 的 `cardanim_guid_to_name` 换出 CardAnim 名，
        /// 再取它的 `targetName`。本效果 = **`Deathspinner Cut Effect`**（工程里已有、已进库）。
        /// 手挂时仍可直接填。</summary>
        [Tooltip("要播的特效名。数据链会填（cardAnim GUID → prefab 名）；手挂也能直接给")]
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
        /// 实现时照原版来（见文件头）：`GetAdjacentUnits(controller.targetCard)` →
        /// `foreach (unit)` → **在 `unit` 的 `BattleCardUI.effectsAnchor` 上原地播**（`0.5 s`、不飞；
        /// 🔴 2026-10-22 订正：**不是**「出手卡 → 那个单位」）。
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
            // 🆕 2026-10-01：生成器已经把 `cardAnim GUID` 换成工程里的 prefab 名了
            // （`工具/gen_animfx_modules.py` 写 `prefabName` 键）—— 手挂时给了值就别覆盖。
            var pn = def.GetString("prefabName");
            if (!string.IsNullOrEmpty(pn)) prefabName = pn;
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
                                   + "（起点=终点=`unit` 的 `BattleCardUI.effectsAnchor`(+0x1A0)，原地停 0.5 s、不飞）。"
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
