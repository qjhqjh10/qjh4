// TraitFrames.cs — 单位身上那套「状态框」（trait frame）
//
// 原版判据（2026-09-29 逐句读过全量反编译；正本 → `资料/待办判据_战场与战斗视图.md` **Q8 第 3 条**）：
//   · **两本字典挂在 `BattleCardUI` 上**：`TraitFrameEffectsPlay`（偏移 `+0x2d0`）与
//     `TraitFrameEffectsTrigger`（`+0x2d8`），值 = 实例化出来的那个 GameObject。
//   · **分工按 `CardTrait.EffectTriggerType`**：
//       `TraitFrameEffectsPlay`    ← **OnPlay(2)**   （`BattleCardUI__DisplayStatusAnim.c:120-126`）
//       `TraitFrameEffectsTrigger` ← **Trigger(3) 与 TriggerOnPlay(6)**（`…__DisplayTriggerAnim.c:71`）
//   · 🆕 **2026-10-23（`A7` · 特效线第二批）`Start(0)` 那一档也接了** —— 判据
//     `BattleCardUI__DisplayStatusAnim.c:86-119`：`if (TraitEffect.TriggerType == 0)` 时**拿这条 effect 直接
//     `BattleManager.ResolveTraitAnim` + `StartCoroutine`**（`:104-119`）—— **不进任何字典、不查重、
//     不写回调**（对比 `== 2` 那一支 `:121-198` 才有 `TraitFrameEffectsPlay` 的 ContainsKey/写表）。
//     ⇒ **每一次 `DisplayStatusAnim` 调用都会重新演一遍**（只要该 trait 有 Start(0) 这条）。
//     ⚠️ **这一档原版【不】过 `playIfWarlord` 门** —— 那道门只在 `…__DisplayTriggerAnim.c:62-68` 里
//     （所以 `armour`（`playIfWarlord=false`）落在督军身上**照样演** `ArmourEffect`）。
//     ⚠️ **与 OnPlay(2) 同一次调用里可以两条都播**（原版是 `for` 逐条遍历资产顺序）：
//     `huntMark` 的资产顺序是 `6,0,2,5,1`、`markerlight` 是 `0,2,5,1` ⇒ 我们的实现落在
//     `DisplayStatus` 里「先播 Start(0) 那几条、再处理 OnPlay(2)」—— 对**当前数据**与资产顺序等价
//     （`AnimFXCheck` ⑧ 有一条数据驱动的断言盯着「凡同时有 0/2 的 trait，0 都在 2 前面」）。
//     ⚠️ `firstStrike` / `piercing` 的 Start(0) 那条 `effectAnim` **是空引用** ⇒ 原版那一档什么都不播
//     （`ResolveTraitAnim` 协程拿到空 CardAnim 直接结束）⇒ 我们**留空 + 出声**，⛔ 别猜一个替身。
//   · `DisplayStatusAnim(trait)`：框实例化在**卡的 `effectsAnchor`** 下
//     （`BattleManager.GetAnimTransform` case 0 返回 `BattleCardUI.effectsAnchor`，`:59-63`；
//      `CreateAnim` 里 `Instantiate(prefab, anchor.position, prefab.localRotation, anchor)`、
//      **缩放随 prefab 自己**）。播完的回调把 GO **写进字典** + `localPosition = Vector3.zero`。
//     已在字典里 ⇒ 只归位 + `SetActive(true)`，**不重播**。
//   · `DisplayTriggerAnim(trait)`：**只有字典里没有才播**；回调**只对 `TriggerOnPlay(6)`** 写进字典
//     （`Trigger(3)` 是**每次触发都放**、不进表）。
//     🔴 **2026-10-20 就地订正（`W58 · TRIGGERFIX`，铁律 5）**：本文件原来的 `DisplayTrigger` **没有照这条做** ——
//     它把**所有**触发类框都写进 `_trigger` 去重表，于是 `Trigger(3)` 也变成「一辈子只演第一次」。
//     原版逐句：`BattleCardUI__DisplayTriggerAnim.c:71` 只处理 `TriggerType == 3/6`；
//     `:88-91` 的守卫是 `TraitFrameEffectsTrigger.ContainsKey(traitId)`；
//     回调 `BattleCardUI.__c__DisplayClass91_1___DisplayTriggerAnim_b__0.c:19-21`
//     `if (TraitEffect.TriggerType != 6) return;` → `:48-52` 才写表
//     ⇒ **只有含 `TriggerOnPlay(6)` 的 trait 会被去重**，其余（纯 `Trigger(3)`）每触发一次演一次。
//     ⚠️ 去重表的键是 **trait id**（不是某条 effect），所以一个 trait 只要有 6 那条 effect，
//     它后续的**所有**触发（含 3 那条）都会被守卫挡住 —— 见下面 `DedupesTrigger`。
//     ✅ **2026-10-25（`E`）亲核订正（铁律 5）——「动画播完的回调」这句是错的**：
//       原版那个回调是 **实例化回调**，不是「播完」回调。逐句（一手）：
//         · `BattleManager__ResolveTraitAnim`（协程）：等 `CardAnim` 的 tween `delay` → `DelayedVFXPlay(...)`；
//         · `BattleManager__DelayedVFXPlay`（协程）：等 `CardAnim+0x24` 的 delay → `CreateAnim(..., param_8=那个回调)`；
//         · `BattleManager__CreateAnim.c:62-79`：**先 `Instantiate`、紧接着就 `invoke(param_8, GameObject)`**
//           （**在** `CardAnimController.Initialize` **之前**）⇒ **写表 = 实例化那一拍**，动画还在第 0 帧。
//       ⇒ **我们「同步写」与它等价**（`Play()` 同步实例化、随后写表）—— 唯一差别 = 原版会先等
//         `CardAnim` 的 delay（当前 6 档里能核到的 `Pray_Idle` 三个时间字段全 0；其余几条本地没有
//         该 CardAnim 的时间表 ⇒ **未逐条核**，但都不影响「写表时机 = 实例化」这条结论）。
//       ⚠️ F 的 `判据/CardTrait对账.md` 第 1/8/17/18/26… 行仍写着「播完的回调」—— 已报给调度台改判；
//          ⛔ 别照那句把写表挪到「动画结束」（我们没有、也不该造一个「结束」事件来当判据）。
//     🆕 **2026-10-23（新账-70）三条补完**：
//       ① **逐条播**：原版 `:47-111` 是 `for` 遍历 `_traitEffects`，3 和 6 **各播各的**
//          （去重表在 6 的回调才写 ⇒ 首次调用两条都能过守卫）⇒ 本类现在一个 trait 可绑多行；
//       ② **`playIfWarlord` 门**：`:62-68` —— 卡是督军且该条 `playIfWarlord == 0` ⇒ **整条 return**
//          （`Oath.asset` 那条正好是 0）⇒ `DisplayTrigger(trait, isWarlord)`；
//       ③ **`codex` / `vanguard` 按真值表归位**（前者 Trigger(3)、后者 OnPlay(2) → `VanguardIdleEffect`）。
//   · `RemoveStatusAnim(trait)`：两本字典各自「有就 `Exit()`（没 `AnimFXController` 就 `Destroy`）+ 移除」。
//   · `CleanStatusAnims(clearAll)`：清掉「卡上**已经不再持有**的 trait」的框（`clearAll` 时全清）；
//     另外**无条件**销毁 `codexFrame` 与 `activeQuestFrame`（**这两个不在两本字典里**）。
//   · 存活时间：框 prefab 上带 `AnimFXController`，`destroyTime > 0 && !preventDestroy` 时自己 `Destroy`。
//
// 🔴 **「哪张框配哪个 prefab」在原版**：`BattleAnims.json` 里
//    `codexActivePrefab` / `stealthPrefab` / `reflectedPrefab` / `summonSickness` 全是空引用 `{FileID:0,PathID:0}`。
//    ⚠️ **2026-10-20 就地订正（铁律 5）**：这里原来还写着「**`CardTrait` 那份 SO 本体全库 0 命中**」——
//    **不成立**：`CardTrait` / `CardTraitCollection` 就在 `d:/2/unity_run_ref/Warpforge_Data/sharedassets0.assets`
//    （pid 458–530 = 73 个 trait · pid 531 `TraitsCollection`），只是 `assets_full` 对根资产文件的 JSON 导出有缺口、
//    按文本搜必然 0 命中。2026-10-19 `K12` 已按原始字节解全（判据 → `资料/普查产出_1019/W_卡牌ID线_K12清零_1019b.md` §1.2）。
//    ⇒ **现在每一条绑定都能报出它对应的 trait effect GUID**（🆕 2026-10-23 起：**全部**绑定都来自那份 SO，
//    **没有一条**是「我们的口径」—— 见下面那段）。
//    （另：`Vanguard` 那条——原版 `CardTrait.Vanguard` 的 OnPlay effect 是 `VanguardIdleAnim → VanguardIdleEffect`，
//     与具名字段 `vanguardFrame` = `Vanguard Frame Animated VAT` 是**两套东西**；🆕 2026-10-23 已按真值改绑前者。）
//    🆕 **2026-10-23（`T1` · Trait 全量权威化）：两本字典【都】改成由原版 `CardTrait` SO 驱动** ——
//    **C# 里再没有任何一条手写的 trait → prefab 绑定**。
//      · 数据 = `Resources/trait_effect_bindings.json`（生成器 `工具/gen_trait_effect_bindings.py`；
//        源 = `数据/游戏数据/trait_collection.json` = 原版 `TraitsCollection` + 73 个 `CardTrait` 资产逐字节解出；
//        安卓 1.35.0 与 PC 关服前 build **逐字段 0 差异**，判据 → `资料/普查产出_1023b/安卓存档_CardTraitSO普查.md`）。
//      · 每行带 **`DefinedTrait / TriggerType / effectAnim GUID / CardAnim / prefab / playIfWarlord`** 六列原版值
//        （`prefab` 那一列 = GUID → CardAnim → `anim_address_map.json` 两跳解出，⛔ 不是按名字猜）。
//      · 分本：`TriggerType == 2(OnPlay)` → `PlayFrames`（原版 `TraitFrameEffectsPlay`）；
//        `TriggerType ∈ {3(Trigger), 6(TriggerOnPlay)}` → `TriggerFrames`（原版 `TraitFrameEffectsTrigger`）。
//    ⚠️ **数据驱动之后被推翻/改判的四条**（都是原先手写表的错，别再抄回来）：
//      · **`stealth`** → OnPlay `StealthTraitPlay` → `StealthEffect`（手写表原来就绑对了，保号）；
//      · **`swarm` 从 OnPlay 那本【移除】** —— 原版 `Swarm` 只有**一条 FromCode(5)**（`SwarmTraitFromCode`
//        → `Swarm_Trigger_OnTarget`），**没有 OnPlay effect**；原来那行是「按名字硬绑」的，属于被去掉的兜底。
//        它现在只走 `TraitParticles`（FromCode 那条链）。原版另有具名字段 `swarmFrame`，那是**另一套**。
//      · **`vanguard`** → OnPlay `VanguardIdleAnim` → `VanguardIdleEffect`（与具名字段 `vanguardFrame`
//        = `Vanguard Frame Animated VAT` **是两套东西**；2026-10-23 早就改绑前者，这次由数据坐实）。
//      · **`slay` 从 OnPlay 那本【移除】、归到触发那本** —— `Slay.asset`（id 260）唯一的 TraitEffect 是
//        **Trigger(3)** → `Slay_Effect_target` → `SlayEffect`。原来那行是按
//        `CardScript__AddEffect.c:889-891` 那个调用点的**意图**（附魔加身演一次）绑的，但原版那一档
//        **没有 OnPlay effect**（文件头 §判据空那段已记）⇒ 附魔加身这一刻原版不播框，如实照数据。
//      · 另外 **13 个 trait 的 OnPlay 框由此第一次接上**（`flying` / `shield` / `stun` / `blind` /
//        `camouflage` / `vulnerable` / `huntMark` / `markerlight` / `pindown` / 四个 `markOf*`）——
//        原版 `BattleCardUI__DisplayStatusAnim` 确实为它们演（`:297-310` 里 `0x186`=flying / `0x3d4`=vanguard
//        两段专门处理飞行动画），⛔ 别当成「我们多加了」。
//    ✅ **按阵营覆盖（`overrideByArmies`）2026-10-23 起【已消费】** —— 原版那条链是
//    `CardTraitCollection.GetTraitEffectObject(trait, triggerType, army)`（`…GetTraitEffectObject.c:24-48`
//    逐条比 `override.army`，命中就用 override 的 `effectAnim`，没命中才回落到默认那条），
//    等价于 `TraitEffect.GetEffect(army)`（`TraitEffect__GetEffect.c:9-34`）。army = **卡自己的**
//    `rawCardData.cardArmy`（`ObtainableItem.cardArmy`，偏移 `+0x2C`）。
//    ⇒ 本文件现在有 **`PrefabFor(trait, trigger, army)` / `PrefabOf(row, army)` 两个阵营维度**，
//    `DisplayStatus` / `DisplayTrigger` 各自把阵营传进去（10 条覆盖：`shield` 4 · `invulnerable` 6）。
//    ✅ **2026-10-25 已解决（铁律 5 就地订正；原来这句写的是「已知偏差」）**：A 已改
//    `BattleDriver.ToCardData(UnitState, faction, isPlayer)` —— **优先取 `u.Card.Faction`、没有才回落拥有者阵营**
//    （`BattleDriver.cs:26519-26530` 的 `cardFaction`），而 `CardData.faction` 就是 `TraitFrames.ArmyId` 读的那一格
//    ⇒ **我们拿到的已经是卡自己的阵营**（跨阵营盟军/中立卡也对上）。
//    ⛔ **别再在这里加 `overrideArmy` / setter** —— 那会和 `CardData.faction` 变成两份口径。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;
using WarpforgeVFX;

namespace CardPresentation
{
    /// <summary>一张卡身上的「状态框」那一套。**每张卡视图一个**（`CardView` 建体时挂上）。</summary>
    public class TraitFrames : MonoBehaviour
    {
        // ==================================================================
        //  绑定表 —— **由原版 `CardTrait` SO 驱动**（⛔ 这里不再有手写的 trait → prefab）
        // ==================================================================
        //  数据 = `Resources/trait_effect_bindings.json`
        //    ← `工具/gen_trait_effect_bindings.py`（源 = `数据/游戏数据/trait_collection.json`，
        //       即原版 `TraitsCollection` + 73 个 `CardTrait` 资产逐字节解出；安卓 1.35.0 交叉验证 0 差异）。
        //  ⛔ **别在本文件里再写第二份绑定**（铁律：「两处写同一条规则 = 迟早不一致」）。
        //     要改绑定 ⇒ 改生成器 / 改 `trait_collection.json`，然后重跑生成器。
        //  ⚠️ 数据里的 `prefab` 一列是**两跳解出来**的（GUID → CardAnim → `anim_address_map`），
        //     生成器对每一行都做 GUID ↔ CardAnim 名的交叉校验（现读 68/68 相符、0 处不一致）。
        //  ⚠️ 原版 `TraitEffect.effectAnim` 是**空引用**的 7 条（`combo` / `daemonicAllegiance` /
        //     `firstStrike` / `large` / `mekaniak` / `piercing` / `resolution`）= 原版这几档**本来就没有动画**，
        //     **不是缺口** ⇒ 不进两本字典、也⛔ 不当缺陷出声（生成器记在 `unresolvedRows` 里留档）。

        /// <summary>绑定表的 `Resources` 路径（**没有扩展名**）。与 `VfxMap.CardTableResourcePath` 同一种玩法。</summary>
        public const string BindingsResourcePath = "trait_effect_bindings";

        /// <summary>运行期形状（`JsonUtility` → 只认 `rows`；其余键被忽略）。</summary>
        [System.Serializable]
        public class BindingOverrideRow
        {
            public int army;
            public string effectGuid;
            public string cardAnim;
            public string prefab;
        }

        [System.Serializable]
        public class BindingRow
        {
            public string trait;
            public int definedTrait;
            public int triggerType;
            public string triggerName;
            public string effectGuid;
            public string cardAnim;
            public string prefab;
            public bool playIfWarlord, inPlayAnim;
            public string unresolved;
            public BindingOverrideRow[] overrides;
        }

        [System.Serializable]
        public class BindingDoc
        {
            public string schema;
            public string generatedBy;
            public string generatedAt;
            public BindingRow[] rows;
        }

        /// <summary>原版一条 `TraitEffect` 里「按阵营覆盖」的那一档（`TraitEffect.overrideByArmies[]`）。</summary>
        public struct TraitEffectOverride
        {
            /// <summary>原版 army id（= `stats.factionArmyIds` 那一组）。</summary>
            public int Army;
            /// <summary>那份 `AssetReference` 的 GUID。</summary>
            public string EffectGuid;
            /// <summary>该 GUID 的 CardAnim 名。</summary>
            public string CardAnim;
            /// <summary>该 CardAnim 的 target prefab。</summary>
            public string Prefab;
        }

        /// <summary>原版一条 `TraitEffect` 的真值 —— **两本字典都是按它解析的**（⛔ 不是按 trait 名猜 prefab）。
        /// 判据 → `Unity/数据/游戏数据/trait_collection.json` 的 `traits[].traitEffects[]`
        /// （从 `sharedassets0.assets` 的 `TraitsCollection` ＋ 各 `CardTrait` 资产**逐字节**解出来的；
        ///  生成器 → `工具/export_trait_collection.py`；
        ///  运行时用的这一份 → `资源/…` 见 <see cref="BindingsResourcePath"/>，由 `工具/gen_trait_effect_bindings.py` 产出）。
        /// **六列全部来自原版资产，没有一列是猜的**：
        ///   · `DefinedTrait` = `DefinedTrait` 枚举值（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`）；
        ///   · `TriggerType`  = `TraitEffect.TriggerType`（0 Start · 1 End · 2 OnPlay · 3 Trigger · 5 FromCode · 6 TriggerOnPlay）；
        ///   · `EffectGuid`   = `TraitEffect.effectAnim` 那份 `AssetReference` 的 GUID；
        ///   · `CardAnim`     = 该 GUID 反查出来的 CardAnim 资产名
        ///                      （`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`；
        ///                       生成器逐行做了 GUID ↔ 名字的交叉校验）；
        ///   · `Prefab`       = 那张 CardAnim 的 target prefab
        ///                      （`数据/索引/anim_address_map.json` 的 `cardanim_to_asset[&lt;CardAnim&gt;].targetName`）；
        ///   · `PlayIfWarlord`= `TraitEffect.playIfWarlord`（资产偏移 `+0x2c`，
        ///                      `BattleCardUI__DisplayTriggerAnim.c:67` 读它）—— 卡是督军且这一位是 0 ⇒ 不播。
        /// 🆕 `Overrides` = `TraitEffect.overrideByArmies[]`（原版按阵营换 effect）。
        /// ✅ **2026-10-23 起运行期已消费**：`PrefabOf(row, army)`（= 原版 `TraitEffect.GetEffect(army)`），
        ///    `DisplayStatus` / `DisplayTrigger` 会自动把这张卡的阵营传进去。
        ///
        /// 原版运行时怎么选（`BattleCardUI__DisplayTriggerAnim.c:47-111`，**逐条遍历**）：
        ///   · `:71` 只处理 `TriggerType ∈ {3, 6}`；
        ///   · `:62-68` **先**过 `playIfWarlord` 门：卡是督军（`GameStaticData__IsWarlordEquivalent` = 卡类型 10）
        ///     且该条 `playIfWarlord == 0` ⇒ `goto` 到函数尾 = **本次调用整条 return**（后面的 effect 也不播）；
        ///   · `:88-91` 的去重守卫**按 trait id**、只挡「表里已有这个 trait」；写表在 6 那条的**回调**里
        ///     ⇒ 同一次调用里 3 与 6 **都会播**（判据 → `…b__0.c:19-21`）。
        /// ⚠️ 我们一个 trait 可以绑**多条**（值是 `TraitEffectBinding[]`），
        /// 但只覆盖 `TriggerType ∈ {3,6}` 的行 —— End(1)/Start(0) 那几档本来也不归这两本字典。</summary>
        public struct TraitEffectBinding
        {
            /// <summary>我们的规范键（`KeywordTable.*`）。</summary>
            public string Trait;
            /// <summary>原版 `DefinedTrait` 数值。</summary>
            public int DefinedTrait;
            /// <summary>原版 `TraitEffect.TriggerType`（0 Start · 1 End · 2 OnPlay · 3 Trigger · 5 FromCode · 6 TriggerOnPlay）。
            /// 两本**字典**只收 **2** 与 **3/6**；🆕 **0 (Start)** 走「一次性、每次调用都播」那一档
            /// （`_startBook`，⛔ 不进字典）；`1` / `5` 由 `DisplayStatus/DisplayTrigger` 之外的两条链处理
            /// （`5` = `TraitParticles`；`1 (End)` 反编译里没有调用点）。`RowsOfTriggerType` 能看到全部。</summary>
            public int TriggerType;
            /// <summary>原版 `TraitEffect.effectAnim` 的 GUID。</summary>
            public string EffectGuid;
            /// <summary>该 GUID 的 CardAnim 资产名。</summary>
            public string CardAnim;
            /// <summary>那张 CardAnim 的 target prefab（= 我们效果库里的名字）。空 = 原版 `effectAnim` 是空引用。</summary>
            public string Prefab;
            /// <summary>原版 `TraitEffect.playIfWarlord`（`false` = 督军不播这一条）· 同一行并写的 `InPlayAnim` = 原版 **`CardTrait._hasInPlayAnim`（`+0x20`）**，闸的判据/用法见**类尾**那一段。</summary>
            public bool PlayIfWarlord, InPlayAnim;
            /// <summary>🆕 原版 `TraitEffect.overrideByArmies[]`（按阵营换 effect）。
            /// ✅ **2026-10-23 起运行期已消费** —— 解析口只一处：<see cref="PrefabOf"/>；
            /// 原版判据 `TraitEffect.GetEffect(army)`（`TraitEffect__GetEffect.c:9-34`）。</summary>
            public TraitEffectOverride[] Overrides;
        }

        // ==================================================================
        //  两本字典 —— **全部由 `Resources/trait_effect_bindings.json` 载入**（懒加载、只做一次）
        // ==================================================================
        //  · `_playBook`    ← 原版 `TriggerType == 2 (OnPlay)` 的行（原版 `TraitFrameEffectsPlay`）
        //  · `_triggerBook` ← 原版 `TriggerType ∈ {3 (Trigger), 6 (TriggerOnPlay)}` 的行
        //                     （原版 `TraitFrameEffectsTrigger`）
        //  同一个 trait 的多行按**资产里的顺序**播（原版 `DisplayTriggerAnim` 逐条遍历）。
        //  ⚠️ `prefab` 为空的行（= 原版 `effectAnim` 是空引用的那 7 条）**不进字典** ——
        //     原版那几档本来就不演，不是缺口。

        static readonly Dictionary<string, TraitEffectBinding[]> _playBook =
            new Dictionary<string, TraitEffectBinding[]>();
        static readonly Dictionary<string, TraitEffectBinding[]> _triggerBook =
            new Dictionary<string, TraitEffectBinding[]>();
        //  🆕 `_startBook` ← 原版 `TriggerType == 0 (Start)` 的行（判据 `BattleCardUI__DisplayStatusAnim.c:86-119`）。
        //  ⚠️ **它不是一个「字典/去重表」** —— 原版这一档**不存任何对象**，每次调用都 `ResolveTraitAnim` 一次；
        //     这里存的是**真值行**（用来看「这个 trait 的 Start 该播哪几件」），⛔ 别把播出来的
        //     `WarpforgeEffectPlayer` 塞进去（那会变成「一辈子只演第一次」，与判据相反）。
        //  ⚠️ `prefab` 为空的行（`firstStrike` / `piercing`）**不进这张表**，但 trait 名记在
        //     `_startEmptyTraits` 里（= 「原版这一档本来就没有动画」，`DisplayStatus` 只出声、不猜替身）。
        static readonly Dictionary<string, TraitEffectBinding[]> _startBook =
            new Dictionary<string, TraitEffectBinding[]>();
        static readonly HashSet<string> _startEmptyTraits = new HashSet<string>();

        //  🆕 2026-10-24（执行代理 `A2` · 战斗线 `D4`）：**动画名 → 原版 `TraitEffect.TriggerType`** 的反查表。
        //  为什么需要：`trigger` 桶（`VfxMap`）把 **Start(0)**（召唤/附魔那一刻播）与
        //  **Trigger(3)/TriggerOnPlay(6)**（触发那一刻播）的 trait 行混在一起 —— 数据侧
        //  `gen_card_vfx_by_card.py` 按 `TRAIT_TRIGGER_EVENT` 归桶时没带 `TriggerType` 这一列，
        //  而本类**本来就有**那份真值（`_allRows`）。⇒ 在这里开一个只读反查口，让消费端
        //  按原版分拍（判据 = `BattleCardUI__DisplayStatusAnim.c:86-119` 只认 0/2、
        //  `…__DisplayTriggerAnim.c:71` 只认 3/6）。⛔ 不要在别处再手写一张 trait→类型表。
        //  ⚠️ 现读 67 个不同名字，**没有任何一个**同时对应两种 `TriggerType`（2026-10-24 现核）
        //     ⇒ 反查是单值的；将来若出现冲突，后写的那条会覆盖先写的（届时用 `RowsOfTriggerType` 核）。
        static readonly Dictionary<string, int> _triggerTypeOfAnim = new Dictionary<string, int>(System.StringComparer.Ordinal);
        static readonly Dictionary<string, int> _triggerTypeOfPrefab = new Dictionary<string, int>(System.StringComparer.Ordinal);

        static bool _bindingsTried, _bindingsLoaded;
        static TraitEffectBinding[] _allRows = new TraitEffectBinding[0];
        static int _startRows;                    // TriggerType == 0 的行数（含 prefab 空的那两条）
        static string _bindingsProblem;
        static string _bindingsGeneratedAt;

        /// <summary>绑定表**载进来了吗**（断言/出声用；`false` 时两本字典是空的 ⇒ 一个状态框都挂不上）。</summary>
        public static bool BindingsLoaded { get { EnsureBindings(); return _bindingsLoaded; } }
        /// <summary>载不进来的原因（`BindingsLoaded == true` 时是 `null`）。</summary>
        public static string BindingsProblem { get { EnsureBindings(); return _bindingsProblem; } }
        /// <summary>生成的时刻（来自 JSON 的 `generatedAt`；重跑生成器之后断言会跟着变）。</summary>
        public static string BindingsGeneratedAt { get { EnsureBindings(); return _bindingsGeneratedAt; } }
        /// <summary>数据里的**总行数**（75 条 `TraitEffect` 一条不少，含 0/1 那两档 —— 0 那档 2026-10-23 起
        /// 已由 `DisplayStatus` 消费（一次性、不进字典），1 那档反编译里没有调用点、只留档）。</summary>
        public static int BindingRowCount { get { EnsureBindings(); return _allRows.Length; } }

        /// <summary>某一档 `TriggerType` 的**全部**行（拷贝；`2` / `3` / `5` / `6` 是今天用到的四档）。
        /// 断言用 —— 期望值从这份**数据**读，⛔ 不从两本字典读回（那是自证）。</summary>
        public static TraitEffectBinding[] RowsOfTriggerType(int triggerType)
        {
            EnsureBindings();
            var l = new List<TraitEffectBinding>();
            for (int i = 0; i < _allRows.Length; i++)
                if (_allRows[i].TriggerType == triggerType) l.Add(_allRows[i]);
            return l.ToArray();
        }

        /// <summary>🆕 2026-10-24（执行代理 `A2` · 战斗线 `D4`）：**一条动画属于原版哪一种
        /// `TraitEffect.TriggerType`**（先按 `CardAnim` 名、再按 prefab 名；两处都没有 ⇒ **`-1`** = 它不是 trait 行）。
        ///
        /// <para>**为什么要有这个反查口**：`VfxMap` 的 `trigger` 桶里混着两种拍的 trait 行 ——
        /// **Start(0)**（召唤/附魔那一刻，`BattleCardUI__DisplayStatusAnim.c:86-119`）与
        /// **Trigger(3)/TriggerOnPlay(6)**（触发那一刻，`…__DisplayTriggerAnim.c:71` 只认 3/6）。
        /// 原版**触发那一拍只播 3/6** ⇒ 消费端必须能把 0/1/2/5 那几档分出去。
        /// 真值来源 = 本类已载入的 `Resources/trait_effect_bindings.json`（= 原版 `CardTrait` SO），
        /// ⛔ 不是按名字猜、也⛔ 不要在调用方再写一张表。</para>
        /// <para>⚠️ 空名字按「没匹配」处理（先试 `cardAnim`、再试 `prefab`，都没有就回 `-1`）。</para></summary>
        public static int TriggerTypeOf(string cardAnim, string prefab)
        {
            EnsureBindings();
            if (!string.IsNullOrEmpty(cardAnim))
            {
                int t;
                if (_triggerTypeOfAnim.TryGetValue(cardAnim, out t)) return t;
            }
            if (!string.IsNullOrEmpty(prefab))
            {
                int t;
                if (_triggerTypeOfPrefab.TryGetValue(prefab, out t)) return t;
            }
            return -1;
        }

        /// <summary>载入并分组。**失败一定出声**（`Debug.LogError` + `BindingsProblem`）——
        /// ⛔ 不退回任何手写表（2026-10-23 起 C# 里已经没有手写绑定了）。
        /// ⚠️ 只在**第一次**尝试（`_bindingsTried`），别每帧重试。</summary>
        static void EnsureBindings()
        {
            if (_bindingsTried) return;
            _bindingsTried = true;

            var asset = Resources.Load<TextAsset>(BindingsResourcePath);
            if (asset == null)
            {
                _bindingsProblem = $"`Resources/{BindingsResourcePath}.json` **没载到**";
                Debug.LogError($"[TraitFrames] {_bindingsProblem} ⇒ 状态框(OnPlay/触发那两本)**一本都挂不上**。"
                             + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_trait_effect_bindings.py");
                return;
            }
            BindingDoc dto;
            try { dto = JsonUtility.FromJson<BindingDoc>(asset.text); }
            catch (System.Exception e)
            {
                _bindingsProblem = "解析失败：" + e.Message;
                Debug.LogError($"[TraitFrames] `Resources/{BindingsResourcePath}.json` {_bindingsProblem}");
                return;
            }
            if (dto == null || dto.rows == null || dto.rows.Length == 0)
            {
                _bindingsProblem = "是空的 / 形状不对（`rows` 为空）";
                Debug.LogError($"[TraitFrames] `Resources/{BindingsResourcePath}.json` {_bindingsProblem}");
                return;
            }
            _bindingsGeneratedAt = dto.generatedAt;

            var all = new List<TraitEffectBinding>(dto.rows.Length);
            var playLists = new Dictionary<string, List<TraitEffectBinding>>();
            var trigLists = new Dictionary<string, List<TraitEffectBinding>>();
            var playOrder = new List<string>();
            var trigOrder = new List<string>();
            //  🆕 Start(0) 那一档（原版 `DisplayStatusAnim.c:86-119`；**不是字典**，见 `_startBook` 的注释）
            var startLists = new Dictionary<string, List<TraitEffectBinding>>();
            var startOrder = new List<string>();
            var startEmptySeen = new HashSet<string>();
            int noPrefab = 0;
            int startRows = 0;

            for (int i = 0; i < dto.rows.Length; i++)
            {
                var r = dto.rows[i];
                if (r == null || string.IsNullOrEmpty(r.trait)) continue;
                var b = new TraitEffectBinding
                {
                    Trait = Norm(r.trait),
                    DefinedTrait = r.definedTrait,
                    TriggerType = r.triggerType,
                    EffectGuid = r.effectGuid ?? "",
                    CardAnim = r.cardAnim ?? "",
                    Prefab = r.prefab ?? "",
                    PlayIfWarlord = r.playIfWarlord, InPlayAnim = r.inPlayAnim,
                    Overrides = ToOverrides(r.overrides),
                };
                all.Add(b);

                if (b.TriggerType == 0)                       // 🆕 Start(0)：一次性、每次调用都演
                {
                    startRows++;
                    if (b.Prefab.Length == 0)
                    {
                        noPrefab++;
                        startEmptySeen.Add(b.Trait);          // 原版 `effectAnim` 是空引用 ⇒ 本档本来就不演
                        continue;
                    }
                    List<TraitEffectBinding> sl;
                    if (!startLists.TryGetValue(b.Trait, out sl))
                    {
                        sl = new List<TraitEffectBinding>();
                        startLists[b.Trait] = sl;
                        startOrder.Add(b.Trait);
                    }
                    sl.Add(b);
                    continue;
                }
                if (b.TriggerType != 2 && b.TriggerType != 3 && b.TriggerType != 6) continue;  // 1/5 不进这三本
                if (b.Prefab.Length == 0) { noPrefab++; continue; }   // 原版空引用 ⇒ 原版不演、不是缺口

                bool play = b.TriggerType == 2;
                var lists = play ? playLists : trigLists;
                var order = play ? playOrder : trigOrder;
                List<TraitEffectBinding> l;
                if (!lists.TryGetValue(b.Trait, out l))
                {
                    l = new List<TraitEffectBinding>();
                    lists[b.Trait] = l;
                    order.Add(b.Trait);
                }
                l.Add(b);
            }

            _allRows = all.ToArray();
            // 🆕 2026-10-24（`A2` · `D4`）：把「动画名 → `TriggerType`」建起来（反查口见 `TriggerTypeOf`）。
            //  ⚠️ 两个键都要建：运行期表里有的候选带 `CardAnim`（`a`）、有的只有 prefab（`n`）。
            for (int i = 0; i < _allRows.Length; i++)
            {
                var b = _allRows[i];
                if (b.CardAnim.Length > 0) _triggerTypeOfAnim[b.CardAnim] = b.TriggerType;
                if (b.Prefab.Length > 0) _triggerTypeOfPrefab[b.Prefab] = b.TriggerType;
            }
            for (int i = 0; i < playOrder.Count; i++) _playBook[playOrder[i]] = playLists[playOrder[i]].ToArray();
            for (int i = 0; i < trigOrder.Count; i++) _triggerBook[trigOrder[i]] = trigLists[trigOrder[i]].ToArray();
            for (int i = 0; i < startOrder.Count; i++) _startBook[startOrder[i]] = startLists[startOrder[i]].ToArray();
            //  「本档有行、但每一条 prefab 都空」⇒ 原版这一档本来就什么都不播（留空 + 出声，⛔ 不猜替身）。
            //  ⚠️ 只收「**没有**任何一条能播的」那些 trait；一条空一条不空的（今天没有）仍按能播的那条走。
            foreach (var t in startEmptySeen)
                if (!startLists.ContainsKey(t)) _startEmptyTraits.Add(t);
            _startRows = startRows;
            _bindingsLoaded = true;

            Debug.Log($"[TraitFrames] 状态框绑定表已载入（源 = 原版 `CardTrait` SO）：{_allRows.Length} 行"
                    + $" / OnPlay 那本 {playOrder.Count} 个 trait / 触发那本 {trigOrder.Count} 个 trait"
                    + $" / Start(0) 那本 {startOrder.Count} 个 trait（{startRows} 行，含 {_startEmptyTraits.Count} 个"
                    + "原版本档无动画的）"
                    + $"（`Resources/{BindingsResourcePath}.json`，生成于 {_bindingsGeneratedAt}）"
                    + (noPrefab > 0 ? $"；另有 {noPrefab} 条原版 `effectAnim` 是空引用（原版不演，不是缺口）" : ""));
        }

        static TraitEffectOverride[] ToOverrides(BindingOverrideRow[] src)
        {
            if (src == null || src.Length == 0) return EmptyOverrides;
            var o = new TraitEffectOverride[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var s = src[i];
                if (s == null) continue;
                o[i] = new TraitEffectOverride
                {
                    Army = s.army,
                    EffectGuid = s.effectGuid ?? "",
                    CardAnim = s.cardAnim ?? "",
                    Prefab = s.prefab ?? "",
                };
            }
            return o;
        }
        static readonly TraitEffectOverride[] EmptyOverrides = new TraitEffectOverride[0];

        /// <summary>一个 trait 在**某一本**里的全部真值行（按资产顺序；空数组 = 那本没它）。
        /// `trigger == false` = OnPlay 那本。⚠️ 返回内部数组，只读用，⛔ 别改。</summary>
        public static TraitEffectBinding[] BindingsFor(string trait, bool trigger)
        {
            EnsureBindings();
            var t = Norm(trait);
            TraitEffectBinding[] rows;
            if (t.Length != 0 && (trigger ? _triggerBook : _playBook).TryGetValue(t, out rows)) return rows;
            return EmptyBindings;
        }

        /// <summary>查一个 trait 在触发类那一本的**第一条**原版真值（自检用；`false` = 本表没绑这一档）。
        /// ⚠️ 与 <see cref="PrefabFor"/> 同源：`PrefabFor(trait, true)` 返回的就是这条的 `Prefab`。</summary>
        public static bool TryGetTriggerBinding(string trait, out TraitEffectBinding binding)
        {
            var rows = TriggerBindingsFor(trait);
            if (rows.Length > 0) { binding = rows[0]; return true; }
            binding = default(TraitEffectBinding);
            return false;
        }

        /// <summary>一个 trait 在触发类那一本的**全部**真值行（按资产顺序；空数组 = 没绑）。
        /// ⚠️ 返回的是内部数组，只读用，⛔ 别改。转 <see cref="BindingsFor"/>（与 OnPlay 那本同一份实现）。</summary>
        public static TraitEffectBinding[] TriggerBindingsFor(string trait)
        {
            return BindingsFor(trait, true);
        }
        static readonly TraitEffectBinding[] EmptyBindings = new TraitEffectBinding[0];

        /// <summary>这个 trait 的触发框会不会写进去重表（= 原版 `DisplayTriggerAnim` 回调只对
        /// `TriggerType == 6` 写表）。
        /// 🆕 **2026-10-23**：判据**改由真值表派生**（表里有 `TriggerType == 6` 的行 ⇒ 会写表）——
        /// 原来那份手写 `TriggerOnPlayTraits` 集合已删（两处写同一条规则迟早不一致）。
        /// 判据原文：`BattleCardUI.__c__DisplayClass91_1___DisplayTriggerAnim_b__0.c:19-21`
        /// `if (TraitEffect.TriggerType != 6) return;` → `:48-52` 写 `TraitFrameEffectsTrigger`。
        /// ⚠️ 键是 **trait id** ⇒ 只要 trait 有 6 这条，之后的触发（**包括它 3 那条**）都被挡住。</summary>
        public static bool DedupesTrigger(string trait)
        {
            var rows = TriggerBindingsFor(trait);
            for (int i = 0; i < rows.Length; i++) if (rows[i].TriggerType == 6) return true;
            return false;
        }

        /// <summary>🆕 **2026-10-23（新账-70 第 3 条）原版 `playIfWarlord` 门** ——
        /// `BattleCardUI__DisplayTriggerAnim.c:62-68`：
        /// <code>if (CardScript.IsWarlordEquivalent()) {
        ///           effect = …;
        ///           if (effect.playIfWarlord == 0) goto 函数尾;   // = **整条 return**，后面的 effect 也不播
        ///       }</code>
        /// ⇒ 本方法 = 「这个 trait 落在督军身上时，原版那一次调用**整个**不播」。
        /// 判据只此一处：`DisplayTrigger` 与自检共用它。
        /// ⚠️ 我们表里只装 3/6 两档；原版那道门对**该 trait 的所有 effect** 都先判一遍
        /// （包括 End(1) 那几条），所以对 oath（只有 3 一条、`playIfWarlord=false`）等价。</summary>
        public static bool BlocksForWarlord(string trait)
        {
            var rows = TriggerBindingsFor(trait);
            for (int i = 0; i < rows.Length; i++) if (!rows[i].PlayIfWarlord) return true;
            return false;
        }

        /// <summary>原版有、我们**暂时挂不上**的那几个（如实记：不是「不做」，是缺一件前置）。
        /// 键 = 原版那个具名字段（`…Frame 0xNNN`），值 = 缺什么。
        /// ✅ 2026-10-01：`vanguardFrame` 已接上。
        /// 🔴 **2026-10-23（铁律 5）就地订正**：原文写「vanguardFrame 已接上（见 `PlayFrames`）」——
        ///   那是**另一套**（具名字段 `vanguardFrame` = `Vanguard Frame Animated VAT`）；状态框这条链走的是
        ///   `TraitEffect`（`VanguardIdleAnim → VanguardIdleEffect`），所以「接上」的说法早就该改。
        /// ⚠️ 这几条原版引用/写方都是空的 ⇒ 按铁律 11 第 ① 种**结案**，不是缺口。</summary>
        public static readonly string[] MissingFrames =
        {
            "summonSicknessFrame / stealthFrame / reflectedFrame（原版 `BattleAnims.json` 那几项是**空引用**）",
            "perfectionFrame / courageFrame（原版桩里**没有写方** ⇒ 判据空）",
            "codexFrame / activeQuestFrame（原版不在两本字典里、无条件销毁；我们按同一语义处理 —— 见 `Clean`）",
        };

        /// <summary>查一个 trait 该用哪张框（`null` = 没有绑定）。**判据只此一处**（= 数据里那一行）。
        /// 触发那一本现在是**一组** ⇒ 这里返回**第一条**（`bloodthirst` = `BloodThirstEffect_Gain`）。
        /// 要全部 ⇒ <see cref="PrefabsFor"/>。
        /// ⚠️ 这个重载**不带阵营** ⇒ 拿的是那条的**默认** `effectAnim`（不套 `overrideByArmies`）。
        /// 生产路径请用三参重载（<see cref="PrefabFor(string,bool,int)"/>）。</summary>
        public static string PrefabFor(string trait, bool trigger)
        {
            return PrefabFor(trait, trigger, -1);
        }

        /// <summary>🆕 2026-10-23：**带阵营**的取框（原版 `TraitEffect.GetEffect(army)` 那一步）。
        /// `army &lt;= 0` / 该条没有覆盖 / 没命中 ⇒ 回落到默认 `Prefab`（原版的回落语义）。
        /// 判据 → `TraitEffect__GetEffect.c:9-34`（逐条比 `override.army`，命中就返回它的 `effectAnim`，
        /// **没有命中**才返回默认那条）。⚠️ 命中但那一格是空引用 ⇒ 原版拿到 null（**不回落**），这里同样返回空串。</summary>
        public static string PrefabFor(string trait, bool trigger, int army)
        {
            var rows = BindingsFor(trait, trigger);
            return rows.Length > 0 ? PrefabOf(rows[0], army) : null;
        }

        /// <summary>🆕 2026-10-23：把**一条真值行**按阵营解析成 prefab —— **判据只此一处**
        /// （原版 `TraitEffect.GetEffect(army)`；`TraitEffect__GetEffect.c:9-34` / `…OverrideByArmy__GetEffect.c:8-21`）。
        /// 逐条按资产顺序比 `Army`，**第一条命中**就返回它（原版那个 `for` 也是取第一个命中的）；
        /// 全不命中 ⇒ 默认 `row.Prefab`。</summary>
        public static string PrefabOf(TraitEffectBinding row, int army)
        {
            if (row.Overrides != null && row.Overrides.Length > 0 && army > 0)
                for (int i = 0; i < row.Overrides.Length; i++)
                    if (row.Overrides[i].Army == army)
                        return row.Overrides[i].Prefab ?? "";
            return row.Prefab;
        }

        /// <summary>**这一本里该播的**全部 prefab（按顺序；空数组 = 没绑）。
        /// 触发那本 = 一个 trait 的多条 3/6（`bloodthirst` 首次触发两条都播）；OnPlay 那本按数据恒 0/1 条。
        /// ⚠️ 不带阵营（= 不套 `overrideByArmies`），同 <see cref="PrefabFor(string,bool)"/>。</summary>
        public static string[] PrefabsFor(string trait, bool trigger)
        {
            return PrefabsFor(trait, trigger, -1);
        }

        /// <summary>🆕 2026-10-23：带阵营的版本（每条各自按 `PrefabOf` 解析）。</summary>
        public static string[] PrefabsFor(string trait, bool trigger, int army)
        {
            var rows = BindingsFor(trait, trigger);
            if (rows.Length == 0) return new string[0];
            var names = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) names[i] = PrefabOf(rows[i], army);
            return names;
        }

        // ==================================================================
        //  🆕 Start(0) 档 —— 原版 `BattleCardUI__DisplayStatusAnim.c:86-119`
        //  「拿这条 effect 直接 `ResolveTraitAnim`」：**不进字典、不查重、不写回调** ⇒ 每次调用都演一次。
        // ==================================================================

        /// <summary>原版 `TraitEffect.TriggerType == 0 (Start)` 的行数（13 条；断言用）。</summary>
        public static int StartRowCount { get { EnsureBindings(); return _startRows; } }

        /// <summary>本档里**能播**的 trait 个数（11 个；`firstStrike` / `piercing` 的 `effectAnim` 是空引用）。</summary>
        public static int StartTraitCount { get { EnsureBindings(); return _startBook.Count; } }

        /// <summary>一个 trait 的 Start(0) 真值行（按资产顺序；空数组 = 本档没有它 / 它的 prefab 是空引用）。
        /// ⚠️ 返回内部数组，只读用，⛔ 别改。</summary>
        public static TraitEffectBinding[] StartBindingsFor(string trait)
        {
            EnsureBindings();
            var t = Norm(trait);
            TraitEffectBinding[] rows;
            if (t.Length != 0 && _startBook.TryGetValue(t, out rows)) return rows;
            return EmptyBindings;
        }

        /// <summary>这个 trait 的 Start(0) 该播的 prefab（按资产顺序；空数组 = 本档没有能播的）。
        /// `army` 走 <see cref="PrefabOf"/>（原版这一档同样按 `TraitEffect.GetEffect(army)` 取覆盖）。</summary>
        public static string[] StartPrefabsFor(string trait, int army)
        {
            var rows = StartBindingsFor(trait);
            if (rows.Length == 0) return new string[0];
            var names = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) names[i] = PrefabOf(rows[i], army);
            return names;
        }

        /// <summary>不带阵营的版本（= 那条的默认 `effectAnim`）。</summary>
        public static string[] StartPrefabsFor(string trait) { return StartPrefabsFor(trait, -1); }

        /// <summary>这个 trait 的 Start(0) 档是不是**原版本来就没有动画**（`effectAnim` 空引用）——
        /// `firstStrike` / `piercing`。`DisplayStatus` 据此「留空 + 出声」，⛔ 不给它们猜替身。</summary>
        public static bool StartIsEmptyByDesign(string trait)
        {
            EnsureBindings();
            var t = Norm(trait);
            return t.Length != 0 && _startEmptyTraits.Contains(t);
        }

        /// <summary>🆕 2026-10-23：原版那个 army id（= `TraitEffect.overrideByArmies[].army` /
        /// `TraitEffect.GetEffect(army)` 入参）的**唯一换算口**：`CardData.faction` → `CardArmy`。
        /// 认不出（`null` / `Neutral` / 拼错）⇒ **`-1`**（= 不套覆盖，走默认件）。</summary>
        public static int ArmyOfFaction(string faction)
        {
            return ArmyIcons.Id(faction);
        }

        // ==================================================================
        //  两本字典（原版 `TraitFrameEffectsPlay` / `TraitFrameEffectsTrigger`）
        // ==================================================================

        readonly Dictionary<string, WarpforgeEffectPlayer> _play =
            new Dictionary<string, WarpforgeEffectPlayer>();
        readonly Dictionary<string, WarpforgeEffectPlayer> _trigger =
            new Dictionary<string, WarpforgeEffectPlayer>();

        CardView _card;
        Transform _anchor;

        /// <summary>🆕 2026-10-23：这张卡的原版 `CardArmy`（= `TraitEffect.overrideByArmies[].army` 那套 id）。
        /// **每次现算**（`CardView` 会被复用、`Data` 会换），取不到 ⇒ `-1`（不套覆盖）。
        /// 原版读的是**卡自己的** `rawCardData.cardArmy`（`ObtainableItem.cardArmy`，偏移 `+0x2C`；
        /// `CardScript__ActivateTraitParticlesFromCode.c:62` / `BattleCardUI__DisplayStatusAnim.c:105` 同一条）。
        /// ✅ **2026-10-25 已解决**：`CardData.faction` 现在 = **卡自己的**阵营（A 改的
        /// `BattleDriver.ToCardData` 优先 `u.Card.Faction`，`BattleDriver.cs:26519-26530`）
        /// ⇒ 本属性拿到的就是原版 `rawCardData.cardArmy` 那一格。⛔ 不再需要任何 override setter。</summary>
        public int ArmyId
        {
            get
            {
                var c = _card;
                if (c == null) c = _card = GetComponent<CardView>();   // 兜底：组件被单独 AddComponent 过
                return c == null ? -1 : ArmyOfFaction(c.Data.faction);
            }
        }

        /// <summary>框挂在哪个节点下 —— 原版是 `BattleCardUI.effectsAnchor(+0x1A0)`。
        /// ⚠️ **那个 anchor 的局部位置我们没读出来**（如实记）⇒ 我们的 `effectsAnchor` 建在
        /// **卡根原点**（卡中心），框的世界位置就取它。</summary>
        public Transform Anchor { get { return _anchor; } }

        /// <summary>给一张卡视图挂上这一套（已经挂过就返回原来那个）。</summary>
        public static TraitFrames Attach(CardView card)
        {
            if (card == null) return null;
            return Attach((Transform)card.transform);
        }

        /// <summary>给**任意挂点**挂上这一套（`CardView` 认不出来也照挂 —— `ArmyId` 会得到 `-1`）。
        /// 用途：① `CardView` 之外的宿主（原版 `effectsAnchor` 也可以不从卡上取）；② 自检探针
        /// （`AnimFXCheck` ⑧ 要在没有整张卡的情况下验「Start(0) 真的播了」）。
        /// ⚠️ 生产路径仍是 <see cref="Attach(CardView)"/>（它只是转发到这一个）。</summary>
        public static TraitFrames Attach(Transform host)
        {
            if (host == null) return null;
            var f = host.GetComponent<TraitFrames>();
            if (f != null) return f;
            f = host.gameObject.AddComponent<TraitFrames>();
            f._card = host.GetComponent<CardView>();
            var go = new GameObject("effectsAnchor");
            go.transform.SetParent(host, false);
            go.transform.localPosition = Vector3.zero;      // ⚠️ 我们挑的，见 `Anchor` 的注释
            f._anchor = go.transform;
            return f;
        }

        // ---- 断言用 ----
        public int PlayCount { get { return _play.Count; } }
        public int TriggerCount { get { return _trigger.Count; } }
        /// <summary>**真正播出去的触发框累计次数**（断言用；`TriggerCount` 只数「进了去重表的」，
        /// 纯 `Trigger(3)` 的框不进表，只能靠这个数看出「每触发一次演一次」）。</summary>
        public int TriggerPlayCount { get { return _triggerPlays; } }
        int _triggerPlays;
        /// <summary>🆕 2026-10-23：**Start(0) 那一档真的播出去的累计次数**（断言用）。
        /// 这一档**不进任何字典**（原版 `DisplayStatusAnim.c:86-119` 不存对象、不查重）⇒ 只能靠这个数
        /// 看「每次调用都演一次」。只计**真建出 `WarpforgeEffectPlayer`** 的（效果库没有该 prefab ⇒ `Play`
        /// 返回 null、不计 —— 那样断言才不会把「空转」当成「播了」）。</summary>
        public int StartPlayCount { get { return _startPlays; } }
        int _startPlays;
        /// <summary>最近一次 Start(0) 播出去的 prefab 名（断言用；还没播过 = `null`）。</summary>
        public string LastStartPrefab { get { return _lastStartPrefab; } }
        string _lastStartPrefab;
        readonly Dictionary<string, int> _startPlaysByTrait = new Dictionary<string, int>();
        /// <summary>某个 trait 的 Start(0) 在这张卡上播过几次（断言用；`0` = 没播过）。</summary>
        public int StartPlayCountOf(string trait)
        {
            int n;
            return _startPlaysByTrait.TryGetValue(Norm(trait), out n) ? n : 0;
        }
        public bool HasPlayFrame(string trait) { return _play.ContainsKey(Norm(trait)); }
        public bool HasTriggerFrame(string trait) { return _trigger.ContainsKey(Norm(trait)); }
        /// <summary>自检用：某一本里现在挂着哪些 trait。</summary>
        public List<string> Traits(bool trigger)
        {
            var keys = trigger ? _trigger.Keys : _play.Keys;
            return new List<string>(keys);
        }

        static string Norm(string s) { return string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant(); }

        // ==================================================================
        //  四个方法（名字与原版一一对应）
        // ==================================================================

        /// <summary>原版有框、我们**挂不上**的那几个 trait（`MissingFrames` 的键那一侧，用来出声）。
        /// 其余没绑定的 trait 原版也没有框 ⇒ 静默是对的。
        /// ✅ 2026-10-01：`vanguard` 已接上，从这里移走。
        /// 🔴 **2026-10-23（`T1`）**：这条判断现在**完全由数据说了算** —— 绑不上 = 数据里没有那一行。
        /// 这 4 个名字在 73 个 `CardTrait` 里**一个都没有**（原版 `BattleAnims.json` 那几项是空引用/无写方），
        /// 所以留着这一格只是「万一引擎给了这个词，出声而不是静默」的兜底**提示**，不是绑定表。</summary>
        static readonly HashSet<string> MissingTraits = new HashSet<string>
        { "summonsickness", "reflected", "perfection", "courage" };
        // ⚠️ **2026-10-23 把 `bloodthirst` 从这里移走** —— 数据里它有 `BloodThirstEffect_Gain`，
        //   本来就走不到「挂不上」那一支（留着只会让这句话失真）。
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();

        /// <summary>把「这一轮卡上持有的关键词」报进来 —— **我们对差出「新加上的 trait」的入口**。
        /// 原版不做对差（它挂在 `CardScript.AddEffect` 那几个分支上）；我们引擎不发「关键词被授予」事件，
        /// 所以在这里按集合对差（`RefreshAll` 就在动作结算之后跑，时机与原版一致）。
        /// 返回这一轮**新挂上的框**数（断言用）。</summary>
        public int SyncKeywords(IEnumerable<string> current)
        {
            // 旧签名转发 —— `valueOf == null` ⇒ 「没有值可传」（判据 §6.0 P0-2：⛔ 别把值塞进 `CardData`）
            return SyncKeywords(current, null);
        }

        /// <summary>🆕 **2026-10-25（W5 · 判据 §6.0 `P0-2`）**：带**取值的委托**的版本。
        /// <para>`valueOf(key)` 必须返回**当前 trait 总值**（基值 + Σmodifier、钳 ≥ 0）——
        /// 原版 `EntityScript.GetCurrentTraitValueWithModifiers`，我们的等价物 = `UnitState.KwValue(key)`。
        /// ⛔ 不是卡面上的固定值、⛔ 不是「本次增加量」（判据 §6.0 / 报告 N15 §⑥ 的共同前提）。</para>
        /// <para>它同时驱动 **W1/W2/W3**（「新出现」那条路把值一路带到 `UpdateValue`）与 **W8**
        /// （值变化兜底：只在值真的变了才刷）。</para></summary>
        public int SyncKeywords(IEnumerable<string> current, System.Func<string, int> valueOf)
        {
            int added = 0;
            var now = new HashSet<string>();
            if (current != null)
                foreach (var k in current)
                {
                    now.Add(k);
                    if (_prev.Contains(k)) continue;
                    if (DisplayStatus(k, ValueOf(valueOf, k))) added++;
                }
            _prev.Clear(); _syncedOnce = true;   // ← 首次同步 = 单位落场那一拍（原版 `ActivateTraitsOnSummonOrEnchantment` 那条路**没有** inPlayAnim 闸；见类尾 `InPlayAnimGate`）
            foreach (var k in now) _prev.Add(k);
            RefreshChangedValues(now, valueOf);  // 🆕 W8：兜底（只在值真的变了才刷）
            Clean(now, false);                  // 不再持有的 ⇒ 清掉（原版 `CleanStatusAnims(false)` 那条谓词）
            return added;
        }

        readonly HashSet<string> _prev = new HashSet<string>();

        /// <summary>原版 `BattleCardUI.DisplayStatusAnim(trait)` —— **trait 被加到这张卡上**时演。
        ///
        /// 原版一次调用会遍历该 trait 的**全部** `TraitEffect`（资产顺序），只认两档：
        ///   · **`TriggerType == 0 (Start)`**（`:86-119`）：拿这条 effect 直接
        ///     `BattleManager.ResolveTraitAnim` + `StartCoroutine` —— **不进字典、不查重、不写回调**
        ///     ⇒ **每次调用都重新演一遍**；这一档**不过 `playIfWarlord` 门**（那道门只在 `DisplayTriggerAnim`）。
        ///   · **`TriggerType == 2 (OnPlay)`**（`:121-198`）：字典里有 ⇒ 只归位 + `SetActive(true)`、
        ///     **不重播**；没有才播，并在播完的回调里写进 `TraitFrameEffectsPlay`（我们是同步写）。
        /// 两者**互不影响**：同一个 trait 同时有 0 和 2 时（`huntMark` / `markerlight`），
        /// 一次调用**两条都播**（顺序 = 资产里的顺序；我们固定「先 Start 再 OnPlay」——
        /// 对当前数据与资产顺序等价，`AnimFXCheck` ⑧ 有数据驱动的断言盯着）。
        ///
        /// 返回 `true` = **OnPlay 那张持久框这次新挂上**（`SyncKeywords` 的 `added` 就数这个）；
        /// 只播了 Start(0)、或本来就没有 OnPlay 那条 ⇒ `false`（Start 的播放次数看
        /// <see cref="StartPlayCount"/> / <see cref="StartPlayCountOf"/>）。</summary>
        public bool DisplayStatus(string trait) { return DisplayStatus(trait, NoTraitValue); }

        /// <summary>🆕 **2026-10-25（W1 / W2 / W7 接线）**：带**当前 trait 总值**的重载。
        /// `value == <see cref="NoTraitValue"/>` ⇒ 调用方没有值可传（老调用点 / 自检），**不刷计数器**
        /// （⛔ 不许拿 0 顶替 —— 那会把计数器写成 `0`，比不刷更糟）。
        /// 判据：原版 `ResolveTraitAnim` 的回调里是 `component.UpdateValue(traitValue)`
        /// （`BattleManager.__c__DisplayClass465_0___ResolveTraitAnim_b__0.c:14-31`），
        /// 「已在字典」那一支是 `UpdateValue(GetCurrentTraitValueWithModifiers(trait))`
        /// （`BattleCardUI__DisplayStatusAnim.c:190-196`）。</summary>
        public bool DisplayStatus(string trait, int value)
        {
            var key = Norm(trait);
            if (key.Length == 0 || !InPlayAnimGate(key)) return false;   // 🆕 `CardTrait._hasInPlayAnim` 闸（判据/语义 → 类尾那一段）

            // ① Start(0) 档 —— **先播、且不受下面那张 OnPlay 去重表影响**（原版 `:86-119` 在 `:121` 那一支之前）
            //    🆕 W3：原版这一档同样把 `traitValue` 传给 `ResolveTraitAnim`（`…DisplayStatusAnim.c:86-119`）
            //    ⇒ 每次 `Play` 之后都要发一次 `UpdateValue`。⚠️ 这两条 trait 的 Start prefab **不带控制器**
            //    ⇒ 事实上空转，但**调用必须发生**（`UpdateValueCalls` 记账；A-E3 数的就是它）。
            PlayStartRows(key, value);

            // ② OnPlay(2) 档 —— 原版 `:121-198`
            WarpforgeEffectPlayer old;
            if (_play.TryGetValue(key, out old))
            {
                if (old != null)
                {
                    old.transform.localPosition = Vector3.zero;
                    old.gameObject.SetActive(true);
                }
                // 🆕 W2：原版「已在字典」这一支除了归位 + 激活，**还要再刷一次计数器**
                //   （`DisplayStatusAnim.c:126-198`，`UpdateValue` 在 `:190-196`）。⛔ 不重建、不重播。
                RefreshCounter(key, value, old, false);
                return false;                       // 「已有 ⇒ 不重播」也是原版的行为，不是失败
            }

            var name = PrefabFor(key, false, ArmyId);
            if (string.IsNullOrEmpty(name))
            {
                if (MissingTraits.Contains(key) && _warnedMissing.Add(key))
                    Debug.LogWarning($"[TraitFrames] 「{key}」在原版有状态框，但我们**挂不上**："
                                   + string.Join(" · ", MissingFrames)
                                   + " —— 如实出声，不静默（判据 → `资料/待办判据_战场与战斗视图.md` Q8 第 3 条）");
                return false;
            }
            var p = Play(name, held: true);     // 🆕 N5：`_play` 那一档由本类持有（状态消失 `Exit()` 收）
            if (p == null) return false;
            _play[key] = p;
            // 🆕 W1：原版「框刚出生」这一拍就调 `UpdateValue`（`CreateAnim` 的 `Instantiate` 之后**立刻**
            //   invoke 回调，在 `CardAnimController.Initialize` 之前 —— `BattleManager__CreateAnim.c:62-81`）。
            RefreshCounter(key, value, p, false);
            return true;
        }

        /// <summary>🆕 2026-10-23：播这个 trait 的 **Start(0)** 那几条（原版 `DisplayStatusAnim.c:86-119`）。
        /// **每次调用都播**（原版这一档没有字典、没有守卫），播出来的 player **不保存**
        /// —— 它由 prefab 自己的 `destroyTime` 收尾（与原版 `AnimFXController` 的销毁口径一致，
        /// 对比 OnPlay 那档要留着被 `Remove`/`Clean` 收）。
        /// ⚠️ `firstStrike` / `piercing` 的 `effectAnim` 是**空引用** ⇒ 原版那一档什么都不播
        /// ⇒ 这里**只出声一次、不猜替身**（铁律 11 例外①）。</summary>
        int PlayStartRows(string key, int value)
        {
            var rows = StartBindingsFor(key);
            if (rows.Length == 0)
            {
                if (StartIsEmptyByDesign(key) && _warnedEmptyStart.Add(key))
                    Debug.Log($"[TraitFrames] 「{key}」的 Start(0) effect 在原版里 `effectAnim` 是**空引用**"
                            + "（`CardTrait` SO 实测）⇒ 原版这一档什么都不播；我们照此留空，⛔ 不找替身。"
                            + "判据 → `BattleCardUI__DisplayStatusAnim.c:86-119` + `数据/游戏数据/trait_collection.json`");
                return 0;
            }

            int army = ArmyId;
            int played = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var name = PrefabOf(rows[i], army);
                if (string.IsNullOrEmpty(name)) continue;    // 命中覆盖但那一格空引用 ⇒ 原版拿到 null、不演
                var p = Play(name);
                if (p == null) continue;                     // 库里没有这件 ⇒ `Play` 已出声，别计数
                played++;
                _startPlays++;
                _lastStartPrefab = name;
                int n;
                _startPlaysByTrait.TryGetValue(key, out n);
                _startPlaysByTrait[key] = n + 1;
                // 🆕 W3：原版这一档也把 `traitValue` 交给 `ResolveTraitAnim` ⇒ 每播一条就发一次 `UpdateValue`
                //   （`BattleCardUI__DisplayStatusAnim.c:86-119`）。⚠️ 本档的 prefab 都不带控制器 ⇒ 空转，但调用要发生。
                RefreshCounter(key, value, p, false);
            }
            return played;
        }
        static readonly HashSet<string> _warnedEmptyStart = new HashSet<string>();

        /// <summary>原版 `BattleCardUI.DisplayTriggerAnim(trait)` —— 那个关键词**真的触发了**时演。
        ///
        /// 逐条照 `BattleCardUI__DisplayTriggerAnim.c:47-111`：
        ///   ① `:88-91` **去重守卫**（键 = trait id）：表里已有这个 trait ⇒ **本次一条都不播**；
        ///   ② `:62-68` **`playIfWarlord` 门**：卡是督军、且该条 `TraitEffect.playIfWarlord == 0`
        ///      ⇒ **整条 return**（连后面的 effect 也不播）—— `oath` 正是这一档；
        ///   ③ 逐条遍历 `TriggerType ∈ {3,6}` 的 effect，**各播各的**（`bloodthirst` 的 3 + 6 首次两条都播）；
        ///      🆕 **2026-10-23**：每一条先过 `PrefabOf(row, ArmyId)`（= 原版 `TraitEffect.GetEffect(army)`）
        ///      —— `shield` / `invulnerable` 的 10 条 `overrideByArmies` 就是在这里按阵营换件的；
        ///   ④ 只有 `TriggerType == 6` 那条**写进**去重表（原版回调 `…b__0.c:19-21`）。
        ///      ✅ 2026-10-25 亲核：那个回调是**实例化回调**（`BattleManager__CreateAnim.c:62-79`：
        ///      `Instantiate` 之后**立刻** `invoke(param_8)`，在 `CardAnimController.Initialize` 之前）
        ///      —— **不是「动画播完」**。我们同步写与它等价（见文件头那段订正）。
        ///      判据仍在**入口检查一次**（否则第 ③ 步里 3 之后的 6 会被自己刚写的表挡住，
        ///      与「首次两条都播」相反）。
        /// 纯 `Trigger(3)` 的 trait（`oath` / `codex` …）**每次触发都演一次、永远不写表**。
        /// ⚠️ 去重表里存的是 **6 那条的播放器**（原版字典里也只有一个 GameObject 值）；3 那条的框
        /// 由 prefab 自己的 `destroyTime` 收尾（`Oath_Effect` 3.0 s / `BloodThirstEffect_Gain` 6.0 s）。
        /// </summary>
        /// <param name="isWarlord">这张卡是不是督军（原版 `CardScript.IsWarlordEquivalent`；
        /// 我们这边 = `UnitState.IsWarlord`，由 `BattleDriver.PlayTraitPulse` 从棋盘上取）。</param>
        /// <param name="value">🆕 **2026-10-25（W4）**：当前 trait 总值（`UnitState.KwValue(keyword)`）。
        /// 原版 `DisplayTriggerAnim` 把 `traitValue` 一起传给 `ResolveTraitAnim`
        /// （`BattleCardUI__DisplayTriggerAnim.c:97-104`）⇒ 触发那一拍也要刷计数器。
        /// `NoTraitValue` = 调用方没有值（老调用点 / 自检）⇒ 不刷。</param>
        public bool DisplayTrigger(string trait, bool isWarlord = false, int value = NoTraitValue)
        {
            var key = Norm(trait);
            if (key.Length == 0) return false;

            var rows = TriggerBindingsFor(key);
            if (rows.Length == 0) return false;

            bool dedupes = DedupesTrigger(key);
            if (dedupes && _trigger.ContainsKey(key)) return false;   // ① 已有 ⇒ 本次一条都不播（原版守卫）

            if (isWarlord && BlocksForWarlord(key))                  // ② `playIfWarlord` 门（整条 return）
            {
                _blockedByWarlord++;
                return false;
            }

            bool playedAny = false;
            int army = ArmyId;                                       // 🆕 原版 `TraitEffect.GetEffect(army)`
            for (int i = 0; i < rows.Length; i++)                    // ③ 逐条都播（原版遍历）
            {
                // 🆕 N5：**只有 `TriggerOnPlay(6)` 那条进表（= 由本类持有）**，纯 `Trigger(3)` 每触发一次演一次、
                //   不入表 ⇒ 它不是持有框，仍按库里 `natural` 收尾（`held` 只给 6 那一支）。
                var p = Play(PrefabOf(rows[i], army), held: rows[i].TriggerType == 6);
                if (p == null) continue;                             // 取不到 ⇒ 出声（`Play` 里已警告）并跳过
                playedAny = true;
                _triggerPlays++;
                // 🆕 W4：原版这一档把 `traitValue` 传给 `ResolveTraitAnim`（`…DisplayTriggerAnim.c:97-104`）
                //   ⇒ 触发那一拍也刷计数器。⚠️ 刷的是**这一条 effect 播出来的那个实例**（原版就是那个实例的组件），
                //   ⛔ 不是 `_play` 里那张持久框 —— 两条链各管各的（判据 §2.2 那张表）。
                RefreshCounter(key, value, p, false);
                if (rows[i].TriggerType == 6) _trigger[key] = p;     // ④ 只有 6 那条写表
            }
            return playedAny;
        }
        int _blockedByWarlord;
        /// <summary>断言用：被 `playIfWarlord` 门挡掉的次数（原版这条路径完全静默，我们至少计数可查）。</summary>
        public int BlockedByWarlordCount { get { return _blockedByWarlord; } }

        /// <summary>原版 `BattleCardUI.RemoveStatusAnim(trait)` —— 两本表各自「有就 `Exit()` + 移除」。</summary>
        public void Remove(string trait)
        {
            var key = Norm(trait);
            Drop(_play, key);
            Drop(_trigger, key);
        }

        /// <summary>原版 `BattleCardUI.CleanStatusAnims(clearAll)` —— 清掉「卡上**已经不再持有**的 trait」
        /// 的框（`clearAll` = 全清，卡离场时走这条）。逐条判据见文件头。
        /// ⚠️ 原版还会**无条件**销毁 `codexFrame` / `activeQuestFrame`（那两个不在两本字典里）——
        /// 我们的 `codex` 走**触发类**那本（数据：`Codex.asset` 只有一条 Trigger(3)），
        /// 触发框由 prefab 自己的 `destroyTime` 收尾 ⇒ 语义等价（它本来也不在 `codexFrame` 那个字段上）。</summary>
        public void Clean(ICollection<string> stillHas, bool clearAll)
        {
            CleanOne(_play, stillHas, clearAll);
            CleanOne(_trigger, stillHas, clearAll);
        }

        void CleanOne(Dictionary<string, WarpforgeEffectPlayer> dict,
                      ICollection<string> stillHas, bool clearAll)
        {
            if (dict.Count == 0) return;
            var drop = new List<string>();
            foreach (var kv in dict)
                if (clearAll || stillHas == null || !stillHas.Contains(kv.Key)) drop.Add(kv.Key);
            for (int i = 0; i < drop.Count; i++) Drop(dict, drop[i]);
        }

        static void Drop(Dictionary<string, WarpforgeEffectPlayer> dict, string key)
        {
            WarpforgeEffectPlayer p;
            if (!dict.TryGetValue(key, out p)) return;
            dict.Remove(key);
            Exit(p);
        }

        /// <summary>原版 `RemoveStatusAnim` / `CleanStatusAnims` 的收尾：有 `AnimFXController` 就 `Exit()`，
        /// 否则 `Destroy()`。我们这边对应 `WarpforgeEffectPlayer.Exit()` / `Kill()`。</summary>
        static void Exit(WarpforgeEffectPlayer p)
        {
            if (p == null) return;
            if (p.IsPlaying) p.Exit();
            else p.Kill();
        }

        /// <summary>建一张框（原版 = `Instantiate(prefab, effectsAnchor.position, prefab.localRotation, effectsAnchor)`
        /// + **缩放随 prefab**）。
        /// <para>🆕 **2026-10-25（`N5` · F1）：`held` = 「这张框由本类**持有**，状态消失时由 `RemoveStatusAnim`
        /// 侧 `Exit()` 收」**（两本字典 `_play` / `_trigger` 的那两档）。判据与为什么要单开这一档：</para>
        /// <para>· 原版 `AnimFXController.OnEnable`：`if (0.0 &lt; destroyTime &amp;&amp; !preventDestroy) Destroy(go, destroyTime);`
        ///   ⇒ **原版只在 `destroyTime > 0` 时排自毁**；**没有 AnimFXController 的 prefab 根本不会自毁**
        ///   （`d:/2/tools/decomp_full/AnimFXController__OnEnable.c`）。
        /// · 我们库里把「原版没有这个组件」编码成 `WFEffectEntry.destroyTime &lt; 0`（见那个字段的 Tooltip），
        ///   而 `AutoLifetime()` 对 `< 0` 有一条**我们自拟的 `natural` 兜底**（给一次性特效收尾用）
        ///   ⇒ 持有框会被它提前收掉：实测 `HuntMark_IdleEffect`（`natural 4.3`）/ `MarkerlightIdle`（`natural 4`）。</para>
        /// <para>🔴 **收窄**：只有 `held == true` **且**该条目 `destroyTime &lt; 0` 时才显式给 **∞**；
        /// `destroyTime &gt; 0` 的持有框照旧按原版计时自毁（原版确实会），**一次性档**（`Start(0)` / 纯 `Trigger(3)`）
        /// 一个字不动（它们仍靠 `natural` 收尾 —— 那条兜底只该给它们用）。</para></summary>
        WarpforgeEffectPlayer Play(string prefabName, bool held = false)
        {
            if (_anchor == null) return null;
            // ⚠️ 空名 = 原版那条 `effectAnim` 是空引用（`overrideByArmies` 命中一格空引用也走这里）⇒ 不演、不出声
            if (string.IsNullOrEmpty(prefabName)) return null;
            // `scale: 1` = 不额外缩放（`Play` 里那个 scale 是**乘数**，乘在 prefab 作者的缩放之上）
            float life = -1f;                       // -1 = 沿用库里那条口径（`preventDestroy` / `AutoLifetime()`）
            if (held)
            {
                var lib = WarpforgeEffectLibrary.Instance;
                WFEffectEntry e;
                if (lib != null && lib.TryGet(prefabName, out e) && e != null && e.destroyTime < 0f)
                    life = float.PositiveInfinity;  // `Play` 的约定：`autoDestroySeconds >= 0` ⇒ 调用方说了算
            }
            var p = WarpforgeEffectPlayer.Play(prefabName, _anchor, Vector3.zero, 1f, life);
            if (p == null)
                Debug.LogWarning($"[TraitFrames] 效果库里没有 `{prefabName}` ⇒ 这张状态框挂不上"
                               + "（是不是重导过 prefab 没重建效果库？Tools > Warpforge > 生成效果库）");
            else if (TraitEffectControllers.Attach(p.gameObject, prefabName) != null)
                AttachedCount++;                    // 🆕 W0
            return p;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-25（第八十会话 · 执行代理 `E5`）：`TraitEffectController` 钩子层 **W0–W8 接线**
        //
        //  判据 → `资料/完全复刻/判据/TraitEffectController_钩子层.md` · 判定表 → `报告/N15_TraitEffect_第1轮.md` §⑥
        //
        //  一句话：原版这层是「**框上那个计数器组件**，在『框出生 / 框再次显示 / 触发』这几拍把
        //  `GetCurrentTraitValueWithModifiers(trait)` 写上去并弹一下」。它**不是**「值一变就刷」的观察者
        //  （`HuntMarkController__UpdateValue.c:13-30` 里没有任何同值早退）。
        //
        //  🔴 **为什么这些新成员放在类尾**：CLAUDE.md §三（2026-10-25 `N9` 实测）—— 插在中间会让
        //     整本账的落点行号整体漂移。**新增一律追到类尾**。
        //
        //  🔴 **`_prevValue` 同时是 W8 的去重锁**（= 判据 §6.3 说的「与 `StatusApplied` 同一把锁」）：
        //     · **W1–W4 / W7 是「显式显示」** ⇒ **一律刷**（`onlyIfChanged: false`）—— 原版每次
        //       `DisplayStatusAnim` / `DisplayTriggerAnim` 都会调 `UpdateValue`，哪怕值没变；
        //     · **W8 是「值变化兜底」**（我们自拟的补偿：原版还有 modifier 变化那条路）⇒ **只在值变了才刷**。
        //     ⇒ 只要显式那一路刷过，它就把当次的数记进 `_prevValue`，兜底那一路同帧就不会再刷一次
        //       （**不会双弹**）。⚠️ 这条**依赖 `RefreshAll` 的顺序**：`PlaySignals()` 在前、`SyncBoard`
        //       在后（`BattleDriver.cs` 的 `RefreshAll`）⇒ `StatusApplied`（W7）先刷、`SyncKeywords`（W8）后判。
        //       ⛔ **改那个顺序会让计数器弹两下**。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>「**调用方没有值可传**」的哨兵（老调用点 / 自检 / 事件里取不到单位）。
        /// ⛔ 别用 `0` 顶替 —— 那会把计数器写成 `0`，比不刷更糟。</summary>
        public const int NoTraitValue = int.MinValue;

        /// <summary>🆕 **W1–W4 的「调用发生了吗」计数器**（判据 §6.5 `UpdateCalls`）。
        /// ⚠️ 它数的是**本类发出更新请求的次数**，**包含「实例上没有控制器」的那些** ——
        /// 原版这两条 trait 的 `Start(0)` / 触发 prefab **本来就不带控制器**（判据 §2.2 那张表），
        /// 「空转」本身是原版行为；断言（A-E3/A-E4）要证的正是**调用必须发生**。</summary>
        public int UpdateValueCalls { get; private set; }

        /// <summary>🆕 **W0 的「挂上了几个控制器」**（判据 §6.5 `AttachedCount`）。
        /// 应恒等于「播过的、带控制器的那两张 idle 框实例数」（全库只有 2 个 prefab 带）。</summary>
        public int AttachedCount { get; private set; }

        /// <summary>最近一次写进控制器的值（`NoTraitValue` = 还没写过）。断言用。</summary>
        public int LastTraitValue { get; private set; } = NoTraitValue;

        /// <summary>W8 的去重锁 / W7 的记账 —— 「这条 trait 上一回写进计数器的是几」。
        /// ⚠️ 只有**真的打到控制器**（`ctl != null`）时才写，否则空转的 Start(0) 会把数写脏、
        /// 让紧接着的 OnPlay 那一档误以为「没变」而漏掉唯一一次弹。</summary>
        readonly Dictionary<string, int> _prevValue = new Dictionary<string, int>();

        /// <summary>取值：优先用调用方给的值；没给就问 `valueOf`；都没有 ⇒ `NoTraitValue`。</summary>
        static int ValueOf(System.Func<string, int> valueOf, string key)
        {
            if (valueOf == null || string.IsNullOrEmpty(key)) return NoTraitValue;
            return valueOf(key);
        }

        /// <summary>🆕 **W1–W4 / W8 的唯一出口**：把「这条 trait 现在几」发给**这个实例**上的控制器。
        /// <para>落点依据：原版 `BattleManager.CreateAnim` 的 `Instantiate` 之后立刻 invoke 的那个回调
        /// = `component.UpdateValue(traitValue)`（`__c__DisplayClass465_0…b__0.c:14-31`）。</para>
        /// <para>`onlyIfChanged = false` ⇒ 显式显示拍（原版每次调、⛔ 不做同值早退）；
        /// `true` ⇒ 值变化兜底（W8）。返回 1 = 真的调了控制器；0 = 没值 / 没实例 /
        /// 这一档没有控制器 / 兜底判定「值没变」。</para></summary>
        int RefreshCounter(string key, int value, WarpforgeEffectPlayer instance, bool onlyIfChanged)
        {
            if (value == NoTraitValue || instance == null) return 0;
            UpdateValueCalls++;                       // A-E3：**调用发生了就记账**，⛔ 不看有没有控制器
            var ctl = instance.GetComponent<TraitEffectController>();
            if (ctl == null) return 0;                // 该 prefab 上本来就没有控制器（全库只有 2 个带）
            if (onlyIfChanged)
            {
                int last;
                if (_prevValue.TryGetValue(key, out last) && last == value) return 0;
            }
            _prevValue[key] = value;
            LastTraitValue = value;
            ctl.UpdateValue(value);
            return 1;
        }

        /// <summary>🆕 **W8**：值变化兜底（判据 §6.3 末段）。
        /// <para>原版还有一条「非 `AddEffect` 路径导致的数值变化」（`GetCurrentTraitValueWithModifiers` 里
        /// modifier 变了）—— 那条我们引擎侧没有对应事件 ⇒ 只能靠每轮同步时对差。</para>
        /// <para>⚠️ **只在「值真的变了」才刷**：这条是**我们自拟的补偿**，⛔ 不能变成「每帧刷」——
        /// `UpdateValue` 里有 `DOPunchScale`，每帧刷会让计数器一直抖（判据 §6.3 那段警告）。</para>
        /// <para>范围：**只看 `_play` 里已有的键** —— 计数器只活在那两档 OnPlay(2) 框上
        /// （`huntMark` / `markerlight`，判据 §2.1）。别的 trait 全库没有控制器，纳进来只会有噪音。</para></summary>
        void RefreshChangedValues(ICollection<string> now, System.Func<string, int> valueOf)
        {
            if (_prevValue.Count > 0)
            {
                var drop = new List<string>();
                foreach (var kv in _prevValue) if (now == null || !now.Contains(kv.Key)) drop.Add(kv.Key);
                for (int i = 0; i < drop.Count; i++) _prevValue.Remove(drop[i]);
            }
            if (valueOf == null || _play.Count == 0) return;
            foreach (var kv in _play)                 // `RefreshCounter` 只改 `_prevValue`（另一本）⇒ 可安全遍历
            {
                if (now != null && !now.Contains(kv.Key)) continue;   // 已不再持有 ⇒ 交给 `Clean` 收
                RefreshCounter(kv.Key, ValueOf(valueOf, kv.Key), kv.Value, true);
            }
        }

        // ── W9 / W10 的**接收端**（本轮 0 调用点；缺的那一侧写在每条的 doc 里）────────────────────

        /// <summary>🆕 **W9 接收端**：原版 `CardScript.SetRemoveMarkerlight`（`CardScript.cs:11260-11296`）
        /// 在标记光被打掉那一刻，对 **play 表与 trigger 表各取一次** `GetComponent<MarkerlightController>()
        /// → MarkerLightUsed()` ⇒ Normal off / Expiring on。
        /// <para>本方法就是那个「取两次」的等价物。返回真的切了几个实例（0 = 这张卡上没有该框）。</para>
        /// <para>🔴 **本轮 0 调用点 —— 缺的是【引擎事件】**：我们引擎侧 `RuleCore.cs:6884` 打完标记光伤害就
        /// `target.RemoveAll("markerlight")`，**没有发任何事件**（现读全仓：`EvtKind` 里没有「trait 被摘掉」那一档）。
        /// ⇒ 要接上得先在 `RuleEngine/Core/BattleEvent.cs` 加一条 `EvtKind` 并在 `RuleCore` 那一处 `Emit`。
        /// ⚠️ 那条事件落地时**必须同批处理 F2**（原版标记光是**留到回合末**才摘，
        /// `CardScript__OnTurnEnd.c:135-141`；我们现在当场摘）—— 否则 Expiring 态几乎永远看不到。</para></summary>
        public int MarkerLightUsed(string trait)
        {
            var key = Norm(trait);
            int n = 0;
            if (MarkerLightUsedIn(_play, key)) n++;
            if (MarkerLightUsedIn(_trigger, key)) n++;
            return n;
        }

        static bool MarkerLightUsedIn(Dictionary<string, WarpforgeEffectPlayer> dict, string key)
        {
            WarpforgeEffectPlayer p;
            if (!dict.TryGetValue(key, out p) || p == null) return false;
            var c = p.GetComponent<MarkerlightController>();
            if (c == null) return false;
            c.MarkerLightUsed();
            return true;
        }

        /// <summary>🆕 **W10 接收端**：原版 `CardScript.SetTraitsOnPlayAttackingStatus(hideStatus)`
        /// （`CardScript.cs:11298-11333`）→ 两本字典各 `AimingAttack(...)`；
        /// 上游 = `PrepareAttack`（开始攻击 `hideStatus: true` / 结束攻击 `hideStatus: false`，
        /// `CardScript._PrepareAttack_d__353__MoveNext.c:41 / :65`）。
        /// <para>🔴 **仍然 0 调用点** —— 三样里**已补两样**（2026-10-25 · 第八十会话 · `R5`）：
        /// ① ✅ `Core/MarkerlightController.cs`：`AimingAttack` 已是原版三句（按 `alreadyAttacked` 选 fader 后
        ///    `Toggle(!option, fadeTime)` —— 反编译里写作 `(option?1u:0u) ^ 1u`），
        ///    两个 `materialFader*` 字段类型**已改回 `MaterialFader`**；
        /// ② ✅ 两个 `MaterialFader` 由 `MarkerlightController.EnsureFaders()` 建在 `Normal State/Fixed` /
        ///    `Expiring State/Fixed`（各 `BindRenderers` 4 条 + `ApplyPrefabFlags(false,false,true)`）；
        /// ③ ✅ **两拍都已接上**（2026-10-25 · 同批 `R5`）：`Battle/BattleDriver.cs` 的 `AimAttackingStatusPair`
        ///    —— 起手 = `PlaySignal` 的 `EvtKind.Attack` 那一拍（`hideStatus: true` = 淡出），
        ///    收手 = **同拍排一条**「攻方动作序列跑完」的回调（`hideStatus: false` = 淡回）。
        ///    判据两拍 = `CardScript.cs:3649`（起手）/ `:3637`（收手）。
        ///    🔴 **就地订正（2026-10-25 · R5 · 铁律 5）**：本段原来写「`EvtKind.Attack` 是**结算**那一拍，不是起手」
        ///    —— 不成立（`BattleDriver.cs:20460-20461` 写着「正好是抬手那一拍」，且被 `AttackWindUp` 排到动作起点）。
        ///    ⚠️ 唯一的**调用点**就是那两个（成对）；⛔ 别在别处再加第三个（会让同一记攻击多淡一次）。</para></summary>
        public int AimingAttack(string trait, bool option)
        {
            var key = Norm(trait);
            int n = 0;
            if (AimingAttackIn(_play, key, option)) n++;
            if (AimingAttackIn(_trigger, key, option)) n++;
            return n;
        }

        static bool AimingAttackIn(Dictionary<string, WarpforgeEffectPlayer> dict, string key, bool option)
        {
            WarpforgeEffectPlayer p;
            if (!dict.TryGetValue(key, out p) || p == null) return false;
            var c = p.GetComponent<MarkerlightController>();
            if (c == null) return false;
            c.AimingAttack(option);
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-26（执行代理 `B8_BA`）：`CardTrait._hasInPlayAnim`（`+0x20`）那道前置闸 —— 补上
        //
        //  判据（一手，`d:/2/tools/decomp_full/`）：
        //   · `CardScript__ActivateTraitAnim.c:12-25`：`GetTrait(trait)` → `if (*(char *)(trait + 0x20) != 0)`
        //     → 才 `BattleCardUI.DisplayStatusAnim(ui, trait, playSound)`。
        //   · `CardScript__AddEffect.c:645-663`（trait 被加上那一支）：同样先 `GetTrait` + `*(char*)(trait+0x20)`，
        //     为真才 `DisplayStatusAnim(ui, traitId, 1)` ⇒ **闸在【调用点】、且闸的是整次调用**
        //     （Start(0) 与 OnPlay(2) 两条腿都进不去；数据现读：`inPlayAnim=false` 却有 OnPlay(2) 行 = 0 条）。
        //   · `BattleCardUI__DisplayStatusAnim.c` 全文**不读**这个 bool（只按 `TriggerType` 分 0/2 两支）
        //     ⇒ **不是「建表时就跳过」，是「播之前查」**。
        //   · 🔴 **召唤那条路没有这道闸**：`CardScript__ActivateTraitsOnSummonOrEnchantment.c` 遍历
        //     `CurrentTraits(+0x128)` 逐条 `DisplayStatusAnim`，全篇没有 `+0x20` 读取
        //     （调用点 `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:179` ·
        //     `CardScript._HeroLandIntoField_d__318__MoveNext.c:352` · `BattleManager__CreateStartingTroop.c:59` ·
        //     `BattleManager__SetPlayerDataAndWarlord.c:28` · `…ResolveTransformFromRemnant_b__0.c:16`）
        //     ⇒ 原版是「**后加上的 trait** 才看这个 bool；单位落场时所有 trait 照演」。
        //     ⇒ 我们只有一个播放入口（`SyncKeywords` 的集合对差 / `EvtKind.StatusApplied`），用
        //     「**这张卡的第一次同步 = 落场那一拍**」把原版那两个调用点分开（见 `InPlayAnimGate`）。
        //   · ⛔ **不影响 `DisplayTrigger`**：`inPlayAnim=false` 却有 Trigger(3/6) 行的有 **15** 条
        //     （bloodThirst · codex · slay · talent · mob · large …），原版触发那一路
        //     （`BattleCardUI__DisplayTriggerAnim.c`）**也没有**这道闸 —— 照演。
        //
        //  数据（`数据/游戏数据/trait_collection.json`，77 条）：`inPlayAnim` true **39** / false **36** /
        //  null **2**。null 的那 2 条（`berzerk` / `dodge`）是 **TraitPairs 里没有 CardTrait SO**
        //  （`traitConfig == null` 且 `traitEffects == null`，判据 `工具/export_trait_collection.py:651/686`）
        //  ⇒ 它们一条 effect 都没有，闸对它们没有行为影响（JSON 里按 `false` 写；⛔ 不是「字段读不到」——
        //  原版 SO 里 `[SerializeField] bool` 不可能是 null）。
        //
        //  修完后的差（`Start(0)` 那一档，逐条）：`armour` · `companion` · `flank` · `pack` · `rally` · `tide`
        //  六条**从「会播」变「不播」**（它们有真 prefab）；`firstStrike` / `piercing` 的 `effectAnim` 是空引用，
        //  两边本来都不播（闸只让那句「本档无动画」的出声不再走）。反向证明：`inPlayAnim=true` 且有可播
        //  Start(0) 的 5 条（`cantAttack` · `fast` · `ferocity` · `huntMark` · `markerlight`）**一条没被关掉**。
        //
        //  ⚠️ 本段所有成员**必须留在类尾**（CLAUDE.md §三 2026-10-25 `N9` 实测）—— 本文件被账按行号引用。
        //     上面那两处 `playIfWarlord, inPlayAnim` / `PlayIfWarlord, InPlayAnim` 的字段**并到同一行**写、
        //     `DisplayStatus` 入口那一句也只改行内内容，都是同一个原因（⛔ 不是为了省事：加行会让
        //     `账/战斗界面_HUD.tsv:117/118/131/146/162` 与 `账/特效动画.tsv:233/414` 的落点整体漂移）。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>「这张卡的**第一次** `SyncKeywords` 已经过去了」。只在 <see cref="InPlayAnimGate"/> 里读。</summary>
        bool _syncedOnce;

        static bool _inPlayAnimScanned, _inPlayAnimKnown;

        /// <summary>原版 `CardTrait._hasInPlayAnim`（`+0x20`）—— **这个 trait「后加到一张在场的卡上」时，
        /// 原版会不会为它演状态帧**（判据见本段开头；反编译 `CardScript__ActivateTraitAnim.c:23` /
        /// `CardScript__AddEffect.c:657` 那两句前置闸）。
        ///
        /// <para>返回值口径：`false` = 原版**不演**；`true` = 演，或**表里没有这个 trait 的行**（不拦）。
        /// 断言用：`PlaysInPlayAnim("armour") == false` · `PlaysInPlayAnim("stealth") == true`
        /// （期望值来自原版 SO 导出的 `inPlayAnim` 列，⛔ 不是从本类自己的两本字典反读 —— 那是自证）。</para>
        /// <para>⚠️ 数据缺这一列时**恒 `true`**（= 照旧全播 + 出声一次），见 <see cref="ScanInPlayAnim"/>。</para></summary>
        public static bool PlaysInPlayAnim(string trait)
        {
            EnsureBindings();
            var t = Norm(trait);
            if (t.Length == 0) return true;
            ScanInPlayAnim();
            if (!_inPlayAnimKnown) return true;          // 缺列 ⇒ 不拦（`ScanInPlayAnim` 已经出过声）
            bool found = false;
            for (int i = 0; i < _allRows.Length; i++)
            {
                if (!string.Equals(_allRows[i].Trait, t, System.StringComparison.Ordinal)) continue;
                found = true;
                if (_allRows[i].InPlayAnim) return true;
            }
            return !found;                               // 表里没有它（含没有 SO 的 `berzerk`/`dodge`）⇒ 不拦
        }

        /// <summary>数据里**有没有** `inPlayAnim` 这一列。判据 = 「至少有一行 `inPlayAnim == true`」（真值 39 条）。
        /// <para>为什么要有这道兜底：2026-10-26 之前的 `Resources/trait_effect_bindings.json` **没有这一列**，
        /// 缺列时每一行都会读成 `false` ⇒ 一刀切把状态帧全关掉（静默多播变静默不播，更糟）。
        /// ⇒ 缺列 ⇒ **照旧全播 + 出声一次**。</para></summary>
        static void ScanInPlayAnim()
        {
            if (_inPlayAnimScanned) return;
            _inPlayAnimScanned = true;
            for (int i = 0; i < _allRows.Length; i++)
                if (_allRows[i].InPlayAnim) { _inPlayAnimKnown = true; return; }
            Debug.LogWarning("[TraitFrames] 绑定表里**没有一条** `inPlayAnim = true` ⇒ 这份 "
                           + "`Resources/" + BindingsResourcePath + ".json` 早于 2026-10-26 的生成器"
                           + "（缺 `CardTrait._hasInPlayAnim` 那一列）⇒ **`inPlayAnim` 闸本轮不生效**（照旧全播）。"
                           + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_trait_effect_bindings.py");
        }

        /// <summary>`DisplayStatus` 入口那道闸（判据见本段开头）。`true` = 允许播。
        /// <para>🔶 自己设计（非原版）：**「第一次同步 = 落场那一拍」这个判别是我们的** —— 原版靠两个不同
        /// 调用点分开（`AddEffect` 有闸 / `ActivateTraitsOnSummonOrEnchantment` 无闸），我们只有一个入口，
        /// 而判据侧「这张卡是不是刚进场」要跨层（`CardScript.cardState(+0x228)` 的等价物在 `UnitState`）。
        /// 调度台 2026-10-26 批次 `B8` 裁定按此实现；若将来能把「刚进场」显式传进来，⛔ 删掉这一层近似、
        /// 改成调用方给的实参。</para></summary>
        bool InPlayAnimGate(string key)
        {
            if (!_syncedOnce) return true;               // 落场那一拍：原版那条路**没有**这道闸
            return PlaysInPlayAnim(key);
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-26（第八十三会话 · 执行代理 `V3`）：原版 `BattleManager.RemoveDestroyableAnims`
        //  ══════════════════════════════════════════════════════════════════════════════════════
        //  判据（两份一起读，逐句对齐）：
        //    · `d:/2/tools/decomp_full/BattleManager__RemoveDestroyableAnims.c`（机器码）
        //    · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:26301-26349`（**带字段名的**那份）
        //  原版全文（去掉空判）：
        //      if ((uint)(cardEffect.buffType - 15) > 1u) return;          // ⇒ 只认 15 / 16
        //      var ui = targetCard.CardUI;
        //      if (ui.TraitFrameEffectsPlay.ContainsKey(cardEffect.trait)) {        // `+0x2d0`
        //          var go = ui.TraitFrameEffectsPlay[cardEffect.trait];
        //          if (go != null) {
        //              var fx = go.GetComponent<AnimFXController>();
        //              if (fx != null) fx.DoDestroy();                            // ⚠️ 没组件 ⇒ **什么都不做**
        //              ui.TraitFrameEffectsPlay.Remove(cardEffect.trait);           // 但字典照样摘
        //          }
        //      }
        //      if (!ui.TraitFrameEffectsTrigger.ContainsKey(cardEffect.trait)) return; // `+0x2d8`
        //      …同上两跳…
        //  🔴 **三个必须照抄的细节**（别「顺手改好」）：
        //    ① **闸是 `buffType ∈ {15 singleUseEnchantment, 16 choiceEnchantment}`**（`BuffType.cs` 现读），
        //       ⛔ **不是「任意一条效果被摘」** —— `addTrait(3)` / `removeTrait(4)` 走不到这里；
        //    ② **`go == null` 时连字典都不摘**（`UnityEngine.Object` 的 `!= null` 重载判的是「还活着」）；
        //    ③ **`GetComponent<AnimFXController>() == null` ⇒ 不销毁、但字典照样摘**（丢掉引用、对象留场）。
        //       这是原版的形状，照抄 —— ⛔ 别换成 `Exit()`（那是 `RemoveStatusAnim` 的另一条路）。
        //  调用点（原版）= `BattleManager.ResolveRemoveEffect` 里那一句
        //  （`cpp2il BattleManager.cs:26297`，紧跟在 4 条 `CancelAnimFromUnitFromQueue` 之后）。
        //  ⚠️ **我们的接线现状**：表现层这一半（本方法 + `BattleDriver.RemoveDestroyableAnims`）已就位；
        //     **引擎侧那一跳还差一个信号** —— 见 `BattleDriver.RemoveDestroyableAnims` 的 doc。
        /// <summary>原版 `BattleManager.RemoveDestroyableAnims(targetCard, cardEffect)` 的**表现层那一半**：
        /// 把 `trait` 名下那两张状态框（OnPlay 那本 / Trigger 那本）`DoDestroy()` 掉并从字典摘除。
        /// <para>⚠️ **它与 <see cref="Remove"/>(`RemoveStatusAnim`) 是两条路**：那条先 `Exit()`
        /// （广播模块 `Exit` + 排 `exitDestroyTime` 后销毁），这条**当场 `DoDestroy()`**
        /// （无模块 ⇒ 立即销毁；有模块 ⇒ 只广播 `DoDestroy`、自己不动）。⛔ 别合并。</para>
        /// <para>空名 / 没有这个 trait 的框 ⇒ 静默返回（原版 `ContainsKey` 为假时就是什么都不做）。</para></summary>
        public void RemoveDestroyable(string trait)
        {
            string key = Norm(trait);
            if (key.Length == 0) return;
            DestroyableOne(_play, key);
            DestroyableOne(_trigger, key);
        }

        /// <summary><see cref="RemoveDestroyable"/> 的单本那一半 —— 逐句对原版（含那三条「照抄」的怪形状）。</summary>
        static void DestroyableOne(Dictionary<string, WarpforgeEffectPlayer> dict, string key)
        {
            WarpforgeEffectPlayer p;
            if (!dict.TryGetValue(key, out p)) return;
            // ② 原版 `if (go != null)` —— Unity 的 `!= null` 重载：已销毁的 GameObject 也算 null
            if (p == null || p.gameObject == null) return;
            // ③ 原版 `go.GetComponent<AnimFXController>()`：没有那个组件 ⇒ **不销毁**（对象留场）
            var fx = p.GetComponent<AnimFXController>();
            if (fx != null) fx.DoDestroy();
            dict.Remove(key);                    // …但字典照样摘（原版 `Dictionary.Remove` 在判空之内）
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-29（执行代理 `F5` · 账 `战斗逻辑驱动:992`）：原版 `CardTraitCollection.GetTraitEffects(trait)`
        //  ══════════════════════════════════════════════════════════════════════════════════════
        //  判据（🥇 `decomp_full/CardTraitCollection__GetTraitEffects.c:1-29`，逐句）：
        //      字典按 `trait` 查到 entry 后，**直接返回 `entry + 0x38` 那一整列**
        //      （即这个 trait 名下**全部** trigger 档的效果，**不分 2/3/5/6**；查不到 ⇒ 返回 0）。
        //  我们这一侧把它**按 trigger 拆成了三本**（`_playBook` / `_triggerBook` / `_startBook`），
        //  ⇒ 「整列」= 三本各自取到的行**拼起来**。本方法补的就是「整列」这个读口
        //  （`GetTraitEffect` 那个「单条」档 = `PrefabFor(trait, trigger, army)`）。
        //  ⚠️ 顺序：按数据表里**原始行序**（`_allRows` 的次序）—— 原版返回的是整列引用、调用方自己遍历，
        //     没有「重排」这一步；这里也不重排。
        /// <summary>原版 `CardTraitCollection.GetTraitEffects(trait)` 的等价物：这个 trait 名下**全部行**
        /// （不分 trigger 档；= 我们的三本 `_playBook` + `_triggerBook` + `_startBook` 的并集，按数据原序）。
        /// <para>空名 / 表里没有它 ⇒ **空数组**（原版那一档返回 0，调用方遍历 0 次 —— 同形）。</para></summary>
        public static TraitEffectBinding[] AllRowsFor(string trait)
        {
            EnsureBindings();
            var t = Norm(trait);
            if (t.Length == 0) return new TraitEffectBinding[0];
            var l = new List<TraitEffectBinding>();
            for (int i = 0; i < _allRows.Length; i++)
                if (string.Equals(_allRows[i].Trait, t, System.StringComparison.Ordinal)) l.Add(_allRows[i]);
            return l.ToArray();
        }
    }
}
