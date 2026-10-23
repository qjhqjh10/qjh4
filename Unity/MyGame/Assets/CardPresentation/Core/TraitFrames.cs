// TraitFrames.cs — 单位身上那套「状态框」（trait frame）
//
// 原版判据（2026-09-29 逐句读过全量反编译；正本 → `资料/待办判据_战场与战斗视图.md` **Q8 第 3 条**）：
//   · **两本字典挂在 `BattleCardUI` 上**：`TraitFrameEffectsPlay`（偏移 `+0x2d0`）与
//     `TraitFrameEffectsTrigger`（`+0x2d8`），值 = 实例化出来的那个 GameObject。
//   · **分工按 `CardTrait.EffectTriggerType`**：
//       `TraitFrameEffectsPlay`    ← **OnPlay(2)**   （`BattleCardUI__DisplayStatusAnim.c:120-126`）
//       `TraitFrameEffectsTrigger` ← **Trigger(3) 与 TriggerOnPlay(6)**（`…__DisplayTriggerAnim.c:71`）
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
//     ⚠️ 原版这个写表发生在**回调**（动画播完之后）；我们仍是**同步**写（既有简化，见报告「没查清」）。
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
//    ⇒ **现在每一条绑定都能报出它对应的 trait effect GUID**；下表仍标「我们的口径」的，是没有走运行时实况验证的那几条。
//    （另：`Vanguard` 那条——原版 `CardTrait.Vanguard` 的 OnPlay effect 是 `VanguardIdleAnim → VanguardIdleEffect`，
//     与具名字段 `vanguardFrame` = `Vanguard Frame Animated VAT` 是**两套东西**；🆕 2026-10-23 已按真值改绑前者。）
//    ⚠️ **`PlayFrames`（OnPlay 那本）仍不许当成「原版就是这样」** —— 只有逐条报出 trait effect GUID 的那几行
//    才算「数据硬绑」，其余是「按名字硬绑、我们的口径」。
//    🆕 **2026-10-23**：**触发类那一本（`TriggerFrames`）已经改成「按 trait effect 真值解析」** ——
//    内容由 `TriggerEffectBindings` 派生，每行带 `DefinedTrait / TriggerType / effectAnim GUID / CardAnim / prefab /
//    playIfWarlord` **六列**原版值（判据 → `数据/游戏数据/trait_collection.json`）。`bloodthirst` 那一行就是这一步的成果：
//    Trigger(3) → `BloodThirstGainAnim` → **`BloodThirstEffect_Gain`**（原先按名字硬绑的 `BloodThirstEffect` 是错的）；
//    2026-10-23 尾账又把 6 那条（`_Idle`）、`codex` 3、`playIfWarlord` 门一起补齐。
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
        //  绑定表（trait → 我们特效库里的框 prefab 名）—— **我们的口径**，见文件头
        // ==================================================================
        //  来路分三种，逐条标：
        //   · 「同名直取」= 原版那件 prefab 的名字与我们要挂的那个 trait 语义一致、且**已在效果库里**；
        //   · 「待接」= 原版那件在解包资源里有（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`），
        //     但**没进我们的效果库**（效果库只导出「被特效引用」的 prefab，而这几件只被 `CardAnim` 引用）。
        //     ✅ **2026-10-01：导入路已经开出来了** —— `工具/extract_missing_shaders.py --prefabs`
        //     把它们 + **整棵依赖树**重打成 `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`
        //     （🔴 内层 CAB **必须改名**，否则与源包撞名、Unity 直接拒收），再由
        //     `EffectExporter.RunListed`（已改成同时扫这个目录）→ `EffectLibraryBuilder.Run` 进库。
        //     ✅ **2026-09-30：`bloodThirstFrame` 也接上了** —— 它**不是**导入路的缺口（`BloodThirstEffect`
        //     一直在库里），真卡点是**引擎从不发 `EvtKind.Trigger`** ⇒ 已在 `RuleCore.EmitBloodThirst`
        //     补上触发点、并绑进 `TriggerFrames`。
        //     ⚠️ 原写「卡点换了：我们引擎里没有 `bloodthirst` 这个词」—— **那半句是错的**（关键词一直在，
        //     见 `EmitBloodThirst` 的判据段）。
        //   · 「判据空」= 原版自己那几项引用就是空的（见文件头最后一段）⇒ 按铁律 11 第 ① 种**结案**。

        /// <summary>按 **OnPlay(2)** 归口的那本（原版 `TraitFrameEffectsPlay`）。
        /// 触发时机 = **这个 trait 被加到这张卡上**（原版挂在 `CardScript.AddEffect` 那几个分支上）。
        ///
        /// 🔴 **2026-10-23（新账-70）**：两行按真值表改过 ——
        ///   · **`codex` 移出本表**：`Codex.asset`（trait id 1000）只有一条 **Trigger(3)**
        ///     → `CodexTraitTrigger` → `CodexEffect` ⇒ 归**触发类**那本（判据 → `trait_collection.json`）。
        ///     原版在「附魔/关键词加身」这一刻**没有** codex 的 OnPlay effect。
        ///   · **`vanguard` 改绑 `VanguardIdleEffect`**：`Vanguard.asset`（id 980）唯一的 TraitEffect 是
        ///     **OnPlay(2)** → `VanguardIdleAnim` → **`VanguardIdleEffect`**（判据同上）。
        ///     原来绑的具名 `Vanguard Frame Animated VAT` 是**另一套**（`vanguardFrame` 那个字段）——
        ///     两者都在效果库里，但「状态框」这条链原版只走 TraitEffect。</summary>
        static readonly Dictionary<string, string> PlayFrames = new Dictionary<string, string>
        {
            // 「同名直取」——两件都在效果库里（`效果库报告.tsv` 有行）
            { KeywordTable.Stealth, "StealthEffect" },
            // 「走地址表」——原版 `ExecuteSwarm` 写的是 `swarmFrame`，而那一族的 CardAnim 叫
            // `SwarmTraitFromCode`；`数据/索引/anim_address_map.json` 解出它指向 prefab
            // **`Swarm_Trigger_OnTarget`**（那张表由 `工具/gen_anim_address_map.py` 从各 bundle 的
            // `m_Container` 建出来）。⚠️ 「那个 CardAnim 就是 `swarmFrame` 的源」这一步**是推断**，如实标。
            { KeywordTable.Swarm,   "Swarm_Trigger_OnTarget" },
            // ✅ **2026-10-01 接上**（原来挂在 `MissingFrames` 里，卡点是那件 prefab **没进效果库**）。
            // 🔴 **2026-10-23（新账-70）改绑真值**：原版 `Vanguard.asset`（trait id **980**）唯一那条
            //   `TraitEffect` 是 **OnPlay(2)** → guid `45746aadb340e664b87453c64c0518c8` →
            //   CardAnim `VanguardIdleAnim` → prefab **`VanguardIdleEffect`**
            //   （`trait_collection.json` + `anim_address_map.json` 两跳现读）。
            //   原来绑的具名 `Vanguard Frame Animated VAT` 来自原版**另一个字段** `vanguardFrame(+0x278)`
            //   （写方 `SetVanguardStealth`），与 TraitEffect 是**两套东西** —— 本类管的是「状态框」= TraitEffect
            //   ⇒ 按真值改绑。⚠️ `VanguardIdleEffect` 的 `cardanim_to_asset.guid` 是 `c53d2e22…`；
            //   `VanguardIdleAnim` 在 `bundle_cardanimsgeneral_assets_all`。
            { KeywordTable.Vanguard, "VanguardIdleEffect" },
            // ✅ **2026-10-20（W58 · `SLAYVIS`）接上** —— 原版那条「Slay 附魔加身时演一下」的调用点
            //   = `CardScript__AddEffect.c:889-891`：Slay 附魔（`trigger == 0x78 = 120`）加进来时
            //   先移除宿主身上其它 Slay 附魔，再 `DisplayStatusAnim(0x2ee)` + `UpdateTraitIcons()`。
            //   🔴 **但当前 build 里那一句是空转**（现读 `sharedassets0.assets` 原始字节，见下）：
            //     · `0x2ee = 750` = `DefinedTrait.quest`；当前 build 的 `TraitsCollection`
            //       （pid 531，77 条 `TraitPairs`，id 范围 5–1280）里**没有 750**
            //       ⇒ `GetTrait(750)` 返回 null ⇒ `DisplayStatusAnim` 在 `op_Equality == null` 那一支直接返回。
            //     · `slay` 自己的 id = **260**，对应 `CardTrait` pid 513：`_hasInPlayAnim = 0`，
            //       唯一一条 `TraitEffect` 的 `TriggerType = 3(Trigger)` ⇒ 原版是由
            //       **`DisplayTriggerAnim` 在 Slay 真的触发时**演（`TriggerAbility` → `PlayTriggerTraitAnim`
            //       → `ActivateTriggerTraitAnim`），**不是**在附魔加身那一刻。
            //   ⇒ 这一行按**那条调用点的意图**（附魔加身演一次）绑进 OnPlay 那本；prefab 来源是
            //     Slay 自己那条 TraitEffect 的 GUID 链（**不是拿 quest 的框顶替** —— quest 连 `CardTrait` 都没有）：
            //       `7dc58e5b008d8804f8fe911400c9bd53` → CardAnim `Slay_Effect_target`
            //       → prefab **`SlayEffect`**（`bundle_battleprefabs_vfxandmisc_assets_all`；
            //       三个变体 `Slay_Effect_self/target/warlord` 同指这一件）。
            //     `SlayEffect` **已在我们的效果库**（`效果库报告.tsv` 有行 · `WarpforgeEffectLibrary.asset` 有项）。
            //   ⚠️ 与 stealth/swarm/vanguard 同一个口径：**按数据硬绑**，不是从原版实况证出来的；
            //      「原版在附魔加身这一刻到底演不演」当前 build **判据为否**，如实标在报告里。
            { KeywordTable.Slay, "SlayEffect" },
        };

        /// <summary>原版一条 `TraitEffect` 的真值 —— **触发类那本表就是按它解析的**（⛔ 不是按 trait 名猜 prefab）。
        /// 判据 → `Unity/数据/游戏数据/trait_collection.json` 的 `traits[].traitEffects[]`
        /// （从 `sharedassets0.assets` 的 `TraitsCollection` ＋ 各 `CardTrait` 资产**逐字节**解出来的；
        /// 生成器 → `工具/export_trait_collection.py`）。**六列全部来自原版资产，没有一列是猜的**：
        ///   · `DefinedTrait` = `DefinedTrait` 枚举值（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`）；
        ///   · `TriggerType`  = `TraitEffect.TriggerType`（0 Start · 1 End · 2 OnPlay · 3 Trigger · 5 FromCode · 6 TriggerOnPlay）；
        ///   · `EffectGuid`   = `TraitEffect.effectAnim` 那份 `AssetReference` 的 GUID；
        ///   · `CardAnim`     = 该 GUID 反查出来的 CardAnim 资产名
        ///                      （`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`）；
        ///   · `Prefab`       = 那张 CardAnim 的 target prefab
        ///                      （`数据/索引/anim_address_map.json` 的 `cardanim_to_asset[&lt;CardAnim&gt;].targetName`；
        ///                       运行期同一份 join 也在 `Resources/cardanim_clips.json` 的 `a → n` 两列里）；
        ///   · `PlayIfWarlord`= 🆕 **2026-10-23（新账-70）** `TraitEffect.playIfWarlord`（资产偏移 `+0x2c`，
        ///                      `BattleCardUI__DisplayTriggerAnim.c:67` 读它）—— 卡是督军且这一位是 0 ⇒ 不播。
        ///
        /// 原版运行时怎么选（`BattleCardUI__DisplayTriggerAnim.c:47-111`，**逐条遍历**）：
        ///   · `:71` 只处理 `TriggerType ∈ {3, 6}`；
        ///   · `:62-68` **先**过 `playIfWarlord` 门：卡是督军（`GameStaticData__IsWarlordEquivalent` = 卡类型 10）
        ///     且该条 `playIfWarlord == 0` ⇒ `goto` 到函数尾 = **本次调用整条 return**（后面的 effect 也不播）；
        ///   · `:88-91` 的去重守卫**按 trait id**、只挡「表里已有这个 trait」；写表在 6 那条的**回调**里
        ///     ⇒ 同一次调用里 3 与 6 **都会播**（判据 → `…b__0.c:19-21`）。
        /// ⚠️ 我们一个 trait 现在可以绑**多条**（`TriggerFrames` 是 `TraitEffectBinding[]`），
        /// 但只覆盖 `TriggerType ∈ {3,6}` 的行 —— End(1)/Start(0) 那几档本来也不归这两本字典。</summary>
        public struct TraitEffectBinding
        {
            /// <summary>我们的规范键（`KeywordTable.*`）。</summary>
            public string Trait;
            /// <summary>原版 `DefinedTrait` 数值。</summary>
            public int DefinedTrait;
            /// <summary>原版 `TraitEffect.TriggerType`（本表只收 **3 = Trigger / 6 = TriggerOnPlay**）。</summary>
            public int TriggerType;
            /// <summary>原版 `TraitEffect.effectAnim` 的 GUID。</summary>
            public string EffectGuid;
            /// <summary>该 GUID 的 CardAnim 资产名。</summary>
            public string CardAnim;
            /// <summary>那张 CardAnim 的 target prefab（= 我们效果库里的名字）。</summary>
            public string Prefab;
            /// <summary>🆕 原版 `TraitEffect.playIfWarlord`（`false` = 督军不播这一条）。</summary>
            public bool PlayIfWarlord;
        }

        /// <summary>**触发类那一本的真值表** —— 一行 = 原版一条 `TriggerType ∈ {3,6}` 的 `TraitEffect`。
        /// `TriggerFrames` 由它派生（**prefab 名只在这里写一次**，⛔ 别在字典里再抄一份）。
        /// 同一个 trait 的多行按**资产里的顺序**播（原版逐条遍历）。</summary>
        public static readonly TraitEffectBinding[] TriggerEffectBindings =
        {
            // 誓约：`Oath.asset`（trait id 1275）**只有一条** Trigger(3) ⇒ 每次触发都演、不进去重表。
            //   🔴 `playIfWarlord = **false**` ⇒ **督军身上的 oath 框原版不播**（那道门 2026-10-23 已实现，
            //   见 `DisplayTrigger` / `BlocksForWarlord`）。这是全表唯一一条 false。
            new TraitEffectBinding
            {
                Trait = KeywordTable.Oath, DefinedTrait = 1275, TriggerType = 3,
                EffectGuid = "e88772cb08fb73243970183f0f1dff7a",
                CardAnim = "Oath_Effect", Prefab = "Oath_Effect",
                PlayIfWarlord = false,
            },
            // ✅ **2026-10-23 改挂（前一批）**：原先按名字硬绑 `BloodThirstEffect`，**改回它就是错的**。
            //   `BloodThirst.asset`（trait id **220 = 0xdc**）的三条 `TraitEffect`：
            //     · Trigger(3)      → guid `874dd68077c21cc4c86c5362b436d792` → `BloodThirstGainAnim`
            //                         → prefab **`BloodThirstEffect_Gain`**  ← 本行
            //     · TriggerOnPlay(6) → `5a60dbe6…` → `BloodThirstIdleAnim` → **`BloodThirstEffect_Idle`** ← 下一行
            //     · End(1)          → `2b63fd56…` → `BloodThirstLoseAnim`（不归这两本字典）
            //   🔴 **别拿名字更像的那两件顶替**（2026-09-30 那版就是这么挂错的）：
            //     · prefab `BloodThirstEffect` ← CardAnim **`BloodThirstTraitTrigger`**（guid `d9a21c77…`），
            //       **`BloodThirst.asset` 的 `_traitEffects` 一条都没引用它** ⇒ 引用链不成立；
            //     · `Bloodthirst_Flare_Warlord` 同指 `BloodThirstEffect`，同样**不在 `_traitEffects` 里**。
            //   ⚠️ 三条的 `playIfWarlord` **都是 true** ⇒ 那道门在 bloodthirst 上永远不拦。
            //   判据全文 → `资料/普查产出_1020/W58_战斗线_触发框只演一次修复.md` §1.3 ·
            //   `资料/普查产出_1019/W_卡牌ID线_K12清零_1019b.md` §1.2（71 条 TraitEffect 全解）。
            new TraitEffectBinding
            {
                Trait = KeywordTable.BloodThirst, DefinedTrait = 220, TriggerType = 3,
                EffectGuid = "874dd68077c21cc4c86c5362b436d792",
                CardAnim = "BloodThirstGainAnim", Prefab = "BloodThirstEffect_Gain",
                PlayIfWarlord = true,
            },
            // 🆕 **2026-10-23（新账-70 第 4 条）：多触发都播** —— 原版 `DisplayTriggerAnim` 逐条遍历，
            //   同一个 trait 的 3 与 6 **各播一次**（去重表在 6 的回调里才写 ⇒ 首次两条都过守卫）。
            //   这一行就是 `BloodThirst.asset` 的 TriggerOnPlay(6) 那一条。
            new TraitEffectBinding
            {
                Trait = KeywordTable.BloodThirst, DefinedTrait = 220, TriggerType = 6,
                EffectGuid = "5a60dbe6e84c80742bf4f9c74b0502ed",
                CardAnim = "BloodThirstIdleAnim", Prefab = "BloodThirstEffect_Idle",
                PlayIfWarlord = true,
            },
            // 🆕 **2026-10-23（新账-70 第 1 条）：`codex` 归触发类**。
            //   `Codex.asset`（trait id **1000**）**只有一条** `Trigger(3)`：
            //     guid `9869f9d036ce2854ab95004fec1a7598` → CardAnim `CodexTraitTrigger` → prefab **`CodexEffect`**
            //   ⇒ 原版是「codex 真的触发时」演，**不是**「关键词加身时」（原来挂在 `PlayFrames` = OnPlay，错的）。
            //   `playIfWarlord = true`。⚠️ 我们引擎目前**不发** `EvtKind.Trigger`「Codex」
            //   （`ActionPump` 内部结算、没有表现事件）⇒ 这一条要等引擎侧补发射口才会真上场（如实记，见报告）。
            new TraitEffectBinding
            {
                Trait = KeywordTable.Codex, DefinedTrait = 1000, TriggerType = 3,
                EffectGuid = "9869f9d036ce2854ab95004fec1a7598",
                CardAnim = "CodexTraitTrigger", Prefab = "CodexEffect",
                PlayIfWarlord = true,
            },
        };

        /// <summary>按 **Trigger(3) / TriggerOnPlay(6)** 归口的那本（原版 `TraitFrameEffectsTrigger`）。
        /// 触发时机 = 那个关键词**真的触发了**（我们这边 = `EvtKind.Trigger`，`Keyword` 字段带规范键）。
        /// **内容由 `TriggerEffectBindings` 派生** —— prefab 名在那边只写一次（铁律：两处写同一条规则迟早不一致）。
        /// 🔴 **2026-10-23（新账-70）**：值从 `string` 改成 **`TraitEffectBinding[]`** —— 原版逐条遍历，
        /// 一个 trait 可以有多条 3/6（`bloodthirst` = 3 + 6 两条），首次触发**两条都播**。</summary>
        static readonly Dictionary<string, TraitEffectBinding[]> TriggerFrames = BuildTriggerFrames();

        static Dictionary<string, TraitEffectBinding[]> BuildTriggerFrames()
        {
            // 按 trait 分组、**保持真值表里的先后**（= 原版资产里 `_traitEffects` 的顺序）
            var lists = new Dictionary<string, List<TraitEffectBinding>>();
            var order = new List<string>();
            for (int i = 0; i < TriggerEffectBindings.Length; i++)
            {
                var b = TriggerEffectBindings[i];
                var k = Norm(b.Trait);
                List<TraitEffectBinding> l;
                if (!lists.TryGetValue(k, out l)) { l = new List<TraitEffectBinding>(); lists[k] = l; order.Add(k); }
                l.Add(b);
            }
            var d = new Dictionary<string, TraitEffectBinding[]>(order.Count);
            for (int i = 0; i < order.Count; i++) d[order[i]] = lists[order[i]].ToArray();
            return d;
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
        /// ⚠️ 返回的是内部数组，只读用，⛔ 别改。</summary>
        public static TraitEffectBinding[] TriggerBindingsFor(string trait)
        {
            var t = Norm(trait);
            TraitEffectBinding[] rows;
            if (t.Length != 0 && TriggerFrames.TryGetValue(t, out rows)) return rows;
            return EmptyBindings;
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
        /// ✅ 2026-10-01：`vanguardFrame` 已接上（见 `PlayFrames`），从这里移走。</summary>
        public static readonly string[] MissingFrames =
        {
            "summonSicknessFrame / stealthFrame / reflectedFrame（原版 `BattleAnims.json` 那几项是**空引用**）",
            "perfectionFrame / courageFrame（原版桩里**没有写方** ⇒ 判据空）",
            "codexFrame / activeQuestFrame（原版不在两本字典里、无条件销毁；我们按同一语义处理 —— 见 `Clean`）",
        };

        /// <summary>查一个 trait 该用哪张框（`null` = 没有绑定）。**判据只此一处**。
        /// 触发那一本现在是**一组** ⇒ 这里返回**第一条**（`bloodthirst` = `BloodThirstEffect_Gain`）。
        /// 要全部 ⇒ <see cref="PrefabsFor"/>。</summary>
        public static string PrefabFor(string trait, bool trigger)
        {
            var t = Norm(trait);
            if (t.Length == 0) return null;
            if (trigger)
            {
                var rows = TriggerBindingsFor(t);
                return rows.Length > 0 ? rows[0].Prefab : null;
            }
            string s;
            return PlayFrames.TryGetValue(t, out s) ? s : null;
        }

        /// <summary>🆕 **这一本里该播的**全部** prefab（按顺序；空数组 = 没绑）。
        /// 触发那本 = 一个 trait 的多条 3/6（`bloodthirst` 首次触发两条都播）；OnPlay 那本恒 0/1 条。</summary>
        public static string[] PrefabsFor(string trait, bool trigger)
        {
            var t = Norm(trait);
            if (t.Length == 0) return new string[0];
            if (trigger)
            {
                var rows = TriggerBindingsFor(t);
                var names = new string[rows.Length];
                for (int i = 0; i < rows.Length; i++) names[i] = rows[i].Prefab;
                return names;
            }
            string s;
            return PlayFrames.TryGetValue(t, out s) ? new[] { s } : new string[0];
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

        /// <summary>框挂在哪个节点下 —— 原版是 `BattleCardUI.effectsAnchor(+0x1A0)`。
        /// ⚠️ **那个 anchor 的局部位置我们没读出来**（如实记）⇒ 我们的 `effectsAnchor` 建在
        /// **卡根原点**（卡中心），框的世界位置就取它。</summary>
        public Transform Anchor { get { return _anchor; } }

        /// <summary>给一张卡视图挂上这一套（已经挂过就返回原来那个）。</summary>
        public static TraitFrames Attach(CardView card)
        {
            if (card == null) return null;
            var f = card.GetComponent<TraitFrames>();
            if (f != null) return f;
            f = card.gameObject.AddComponent<TraitFrames>();
            f._card = card;
            var go = new GameObject("effectsAnchor");
            go.transform.SetParent(card.transform, false);
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
        /// ✅ 2026-10-01：`vanguard` 已接上（`PlayFrames` 里那一条），从这里移走。</summary>
        static readonly HashSet<string> MissingTraits = new HashSet<string>
        { "summonsickness", "reflected", "perfection", "courage" };
        // ⚠️ **2026-10-23 把 `bloodthirst` 从这里移走** —— 它在 `TriggerEffectBindings` 里绑着
        //   `BloodThirstEffect_Gain`，本来就走不到「挂不上」那一支（留着只会让这句话失真）。
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();

        /// <summary>把「这一轮卡上持有的关键词」报进来 —— **我们对差出「新加上的 trait」的入口**。
        /// 原版不做对差（它挂在 `CardScript.AddEffect` 那几个分支上）；我们引擎不发「关键词被授予」事件，
        /// 所以在这里按集合对差（`RefreshAll` 就在动作结算之后跑，时机与原版一致）。
        /// 返回这一轮**新挂上的框**数（断言用）。</summary>
        public int SyncKeywords(IEnumerable<string> current)
        {
            int added = 0;
            var now = new HashSet<string>();
            if (current != null)
                foreach (var k in current)
                {
                    now.Add(k);
                    if (_prev.Contains(k)) continue;
                    if (DisplayStatus(k)) added++;
                }
            _prev.Clear();
            foreach (var k in now) _prev.Add(k);
            Clean(now, false);                  // 不再持有的 ⇒ 清掉（原版 `CleanStatusAnims(false)` 那条谓词）
            return added;
        }

        readonly HashSet<string> _prev = new HashSet<string>();

        /// <summary>原版 `BattleCardUI.DisplayStatusAnim(trait)` —— **trait 被加到这张卡上**时演。
        /// 已在表里 ⇒ 只归位 + 打开，**不重播**（原版 `:157-196`）。</summary>
        public bool DisplayStatus(string trait)
        {
            var key = Norm(trait);
            if (key.Length == 0) return false;

            WarpforgeEffectPlayer old;
            if (_play.TryGetValue(key, out old))
            {
                if (old != null)
                {
                    old.transform.localPosition = Vector3.zero;
                    old.gameObject.SetActive(true);
                }
                return false;                       // 「已有 ⇒ 不重播」也是原版的行为，不是失败
            }

            var name = PrefabFor(key, false);
            if (name == null)
            {
                if (MissingTraits.Contains(key) && _warnedMissing.Add(key))
                    Debug.LogWarning($"[TraitFrames] 「{key}」在原版有状态框，但我们**挂不上**："
                                   + string.Join(" · ", MissingFrames)
                                   + " —— 如实出声，不静默（判据 → `资料/待办判据_战场与战斗视图.md` Q8 第 3 条）");
                return false;
            }
            var p = Play(name);
            if (p == null) return false;
            _play[key] = p;
            return true;
        }

        /// <summary>原版 `BattleCardUI.DisplayTriggerAnim(trait)` —— 那个关键词**真的触发了**时演。
        ///
        /// 逐条照 `BattleCardUI__DisplayTriggerAnim.c:47-111`：
        ///   ① `:88-91` **去重守卫**（键 = trait id）：表里已有这个 trait ⇒ **本次一条都不播**；
        ///   ② `:62-68` **`playIfWarlord` 门**：卡是督军、且该条 `TraitEffect.playIfWarlord == 0`
        ///      ⇒ **整条 return**（连后面的 effect 也不播）—— `oath` 正是这一档；
        ///   ③ 逐条遍历 `TriggerType ∈ {3,6}` 的 effect，**各播各的**（`bloodthirst` 的 3 + 6 首次两条都播）；
        ///   ④ 只有 `TriggerType == 6` 那条**写进**去重表（原版回调 `…b__0.c:19-21`）。⚠️ 原版写表在**回调**
        ///      （动画播完），我们是**同步**写 —— 所以判据在**入口检查一次**（否则第 ③ 步里 3 之后的 6
        ///      会被自己刚写的表挡住，与「首次两条都播」相反）。
        /// 纯 `Trigger(3)` 的 trait（`oath` / `codex` …）**每次触发都演一次、永远不写表**。
        /// ⚠️ 去重表里存的是 **6 那条的播放器**（原版字典里也只有一个 GameObject 值）；3 那条的框
        /// 由 prefab 自己的 `destroyTime` 收尾（`Oath_Effect` 3.0 s / `BloodThirstEffect_Gain` 6.0 s）。
        /// </summary>
        /// <param name="isWarlord">这张卡是不是督军（原版 `CardScript.IsWarlordEquivalent`；
        /// 我们这边 = `UnitState.IsWarlord`，由 `BattleDriver.PlayTraitPulse` 从棋盘上取）。</param>
        public bool DisplayTrigger(string trait, bool isWarlord = false)
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
            for (int i = 0; i < rows.Length; i++)                    // ③ 逐条都播（原版遍历）
            {
                var p = Play(rows[i].Prefab);
                if (p == null) continue;                             // 取不到 ⇒ 出声（`Play` 里已警告）并跳过
                playedAny = true;
                _triggerPlays++;
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
        /// 我们这两个走的就是字典那条路（`codex` 在 `PlayFrames` 里），语义等价。</summary>
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
        /// + **缩放随 prefab**）。</summary>
        WarpforgeEffectPlayer Play(string prefabName)
        {
            if (_anchor == null) return null;
            // `scale: 1` = 不额外缩放（`Play` 里那个 scale 是**乘数**，乘在 prefab 作者的缩放之上）
            var p = WarpforgeEffectPlayer.Play(prefabName, _anchor, Vector3.zero, 1f);
            if (p == null)
                Debug.LogWarning($"[TraitFrames] 效果库里没有 `{prefabName}` ⇒ 这张状态框挂不上"
                               + "（是不是重导过 prefab 没重建效果库？Tools > Warpforge > 生成效果库）");
            return p;
        }
    }
}
