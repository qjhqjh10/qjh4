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
//    ⚠️ **按阵营覆盖（`overrideByArmies`）今天还没消费** —— `PrefabFor` 没有阵营参数，加参数要动
//    `CardView` / `BattleDriver`（不属本次白名单）。10 条覆盖（`shield` 4 · `invulnerable` 6）的真值
//    留在 `TraitEffectBinding.Overrides` 上可查，缺口见报告。
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
            public bool playIfWarlord;
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
        /// 🆕 `Overrides` = `TraitEffect.overrideByArmies[]`（原版按阵营换 effect）。⚠️ **运行期还没消费**
        ///   （`PrefabFor` 没有阵营参数；加参数要动 `CardView` / `BattleDriver`）—— 真值留全可查。
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
            /// 两本字典只收 **2** 与 **3/6**；`RowsOfTriggerType` 能看到全部。</summary>
            public int TriggerType;
            /// <summary>原版 `TraitEffect.effectAnim` 的 GUID。</summary>
            public string EffectGuid;
            /// <summary>该 GUID 的 CardAnim 资产名。</summary>
            public string CardAnim;
            /// <summary>那张 CardAnim 的 target prefab（= 我们效果库里的名字）。空 = 原版 `effectAnim` 是空引用。</summary>
            public string Prefab;
            /// <summary>原版 `TraitEffect.playIfWarlord`（`false` = 督军不播这一条）。</summary>
            public bool PlayIfWarlord;
            /// <summary>🆕 原版 `TraitEffect.overrideByArmies[]`（按阵营换 effect；⚠️ 运行期还没消费，只留真值）。</summary>
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

        static bool _bindingsTried, _bindingsLoaded;
        static TraitEffectBinding[] _allRows = new TraitEffectBinding[0];
        static string _bindingsProblem;
        static string _bindingsGeneratedAt;

        /// <summary>绑定表**载进来了吗**（断言/出声用；`false` 时两本字典是空的 ⇒ 一个状态框都挂不上）。</summary>
        public static bool BindingsLoaded { get { EnsureBindings(); return _bindingsLoaded; } }
        /// <summary>载不进来的原因（`BindingsLoaded == true` 时是 `null`）。</summary>
        public static string BindingsProblem { get { EnsureBindings(); return _bindingsProblem; } }
        /// <summary>生成的时刻（来自 JSON 的 `generatedAt`；重跑生成器之后断言会跟着变）。</summary>
        public static string BindingsGeneratedAt { get { EnsureBindings(); return _bindingsGeneratedAt; } }
        /// <summary>数据里的**总行数**（75 条 `TraitEffect` 一条不少，含 0/1 那两档 —— 它们是原版真值、只留档）。</summary>
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
            int noPrefab = 0;

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
                    PlayIfWarlord = r.playIfWarlord,
                    Overrides = ToOverrides(r.overrides),
                };
                all.Add(b);

                if (b.TriggerType != 2 && b.TriggerType != 3 && b.TriggerType != 6) continue;  // 0/1/5 不进这两本
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
            for (int i = 0; i < playOrder.Count; i++) _playBook[playOrder[i]] = playLists[playOrder[i]].ToArray();
            for (int i = 0; i < trigOrder.Count; i++) _triggerBook[trigOrder[i]] = trigLists[trigOrder[i]].ToArray();
            _bindingsLoaded = true;

            Debug.Log($"[TraitFrames] 状态框绑定表已载入（源 = 原版 `CardTrait` SO）：{_allRows.Length} 行"
                    + $" / OnPlay 那本 {playOrder.Count} 个 trait / 触发那本 {trigOrder.Count} 个 trait"
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
        /// 要全部 ⇒ <see cref="PrefabsFor"/>。</summary>
        public static string PrefabFor(string trait, bool trigger)
        {
            var rows = BindingsFor(trait, trigger);
            return rows.Length > 0 ? rows[0].Prefab : null;
        }

        /// <summary>**这一本里该播的**全部 prefab（按顺序；空数组 = 没绑）。
        /// 触发那本 = 一个 trait 的多条 3/6（`bloodthirst` 首次触发两条都播）；OnPlay 那本按数据恒 0/1 条。</summary>
        public static string[] PrefabsFor(string trait, bool trigger)
        {
            var rows = BindingsFor(trait, trigger);
            if (rows.Length == 0) return new string[0];
            var names = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) names[i] = rows[i].Prefab;
            return names;
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
