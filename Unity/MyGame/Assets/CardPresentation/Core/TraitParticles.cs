// TraitParticles.cs — trait 的「**从代码播**」粒子
//                    （原版 `CardScript.ActivateTraitParticlesFromCode` / `…InTarget`）
//
// 判据（2026-10-01 逐文件读过 `d:/2/tools/decomp_full/`；全文（含 8 个调用点与 trait id →
// `资料/待办判据_战场与战斗视图.md` 的「trait 粒子」那一段）：
//   · 取什么：`CardTraitCollection.GetTrait(trait).TraitEffects.Find(e => (int)e.TriggerType == 5)`
//     （`EffectTriggerType.FromCode`，两个方法都**硬编码 5**）→ `BattleManager.GetTraitEffect`
//     → `CardTraitCollection.GetTraitEffectObject` → 那个 **`CardAnim`** → `ResolveTraitAnim` + 协程。
//   · 🔴 **与 `TraitFrames` 那两本字典不是一条链**：这条**不进字典、也不用清**（两个方法都不带 callback）；
//     那边的值是 **GameObject**（进表、可整表清），这条是 **CardAnim**。
//     `swarm` 两处都有 —— **是两个不同的用法**，别合并。
//
// 🔴🔴 **2026-10-23（`T1` · Trait 全量权威化）就地改判（铁律 5，覆盖本文件原来那一整版「反推」说明）：**
//   原文写「哪些 trait 有 FromCode 的 effectAnim —— 本地【判不了】…⇒ 下面这张表是**按 CardAnim 名字 + trait 名
//   逐字对应反推**出来的（**是我们的口径**）」。**不再成立**：`CardTrait` SO 就在
//   `Warpforge_Data/sharedassets0.assets`（pid 458–530 + 531 `TraitsCollection`），早已逐字节解出成
//   `数据/游戏数据/trait_collection.json`（安卓 1.35.0 交叉验证 0 差异）。
//   ⇒ **这张表由数据驱动**：`TriggerType == 5 (FromCode)` 的那 10 条 `TraitEffect`，
//   prefab = `effectAnim` GUID → CardAnim → `anim_address_map` 两跳（生成器 `工具/gen_trait_effect_bindings.py`，
//   产物 `Resources/trait_effect_bindings.json`）。**C# 里已经没有手写的 FromCode 绑定。**
//
//   ⚠️ **数据推翻的两条手写口径**（别再抄回来）：
//     · **`huntMark` 原来「故意留白」是错误的** —— 原版 `HuntMark.asset` **确实有一条 FromCode(5)**：
//       `HuntMark Damage Anim` → `HuntMark_DamageEffect`。原文「映射表里没有任何 `…InTarget` 结尾的 CardAnim
//       ⇒ 宁可留白」是**按名字找绑定的思路**（= 正是这次要去掉的兜底）。现在照数据绑上。
//     · **`stomp` 原来绑 `StompEffect` 是错的** —— 原版 FromCode(5) → `BluntImpact_Stomp_Forward` → **`Attack_Stomp`**
//       （`StompEffect` 不是任何一条 TraitEffect 的 prefab）。`ferocity` 同理：
//       原版 FromCode(5) → `Ferocity Recall to deck` → **`Ferocity_RecallToDeck`**（不是 `FerocityEffect`；
//       `FerocityEffect` 是它 **Start(0)** 那条的 prefab，属于另一档）。
//     · 新增两条原来根本没有的：`shuriken → ShurikenEffect` · `teleport → Teleport Trait Summon`。
//
//   🆕 **2026-10-23（`W65`）`ambush` 的「`FromCode(5)` 槽位」结案 —— 原版【没有】这一槽**（不是缺口）：
//     `Ambush`（DefinedTrait 1200）在 `数据/游戏数据/trait_collection.json` 里**只有一条 `TraitEffect` = End(1)**
//     → `Ambush Effect` → `AmbushEffect`。两个 `ActivateTraitParticlesFromCode*` 都**硬编码 5**
//     ⇒ 原版这条链对 ambush **本来就什么都不播**（铁律 11 例外①）。
//     而 End(1) 那一档反编译里**没有调用点**（`DisplayStatusAnim` 0/2 · `DisplayTriggerAnim` 3/6 ·
//     `CardScript__RemoveTrait` 只调 `RemoveStatusAnim`）⇒ `VfxMap.AmbushExit` 继续留空 + 出声，
//     ⛔ 别把 `AmbushEffect` 按名字塞进这张表（`AnimFXCheck` ⑦ 有反向断言盯着）。
//
// 用法：`BattleDriver.PlayTraitParticles(e)` 在事件流上按「事件 → 该查哪几个 trait」调 `Play`。
using System.Collections.Generic;
using RuleEngine;          // 🆕 2026-10-20（W57 · SC-7）：`BattleEvent.SplitKeywordAbility` —— `#i` 后缀的**唯一解码口**
using UnityEngine;
using WarpforgeVFX;

namespace CardPresentation
{
    public static class TraitParticles
    {
        /// <summary>trait（规范化键，与 `UnitState.Keywords` 同一套）→ 那条 FromCode 真值行（含 `Overrides`）。
        ///
        /// **唯一来源** = `TraitFrames.RowsOfTriggerType(5)`，也就是
        /// `Resources/trait_effect_bindings.json` 里 `TriggerType == 5 (FromCode)` 的那批行
        /// （= 原版每条 `CardTrait.TraitEffects[]` 里 `TriggerType == FromCode` 的那一条）。
        /// ⚠️ **走 `TraitFrames` 那次载入、不复读第二遍 JSON**：同一份数据只能有一个载入点（否则迟早不一致）。
        /// ⚠️ 同一个 trait 若有**多条** FromCode，取**资产里第一条**并出声（今天数据里一条都没有重复）。
        /// 🆕 **2026-10-23**：存的是**整条真值行**（不是裸 prefab 名）—— 原版
        /// `ActivateTraitParticlesFromCode` 调的是 `GetTraitEffect(trait, 5, **army**)`
        /// （`CardScript__ActivateTraitParticlesFromCode.c:62`），`overrideByArmies` 在这条链上同样生效
        /// （判据 `TraitEffect__GetEffect.c:9-34`；解析只走 `TraitFrames.PrefabOf`）。</summary>
        static Dictionary<string, TraitFrames.TraitEffectBinding> _fromCode;

        static Dictionary<string, TraitFrames.TraitEffectBinding> FromCode
        {
            get { EnsureFromCode(); return _fromCode; }
        }

        static void EnsureFromCode()
        {
            if (_fromCode != null) return;
            var rows = TraitFrames.RowsOfTriggerType(TraitEffectTriggerType.FromCode);
            var d = new Dictionary<string, TraitFrames.TraitEffectBinding>(rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (string.IsNullOrEmpty(r.Trait) || string.IsNullOrEmpty(r.Prefab)) continue;
                if (d.ContainsKey(r.Trait))
                {
                    Debug.LogWarning($"[TraitParticles] trait `{r.Trait}` 在原版里有**多条** FromCode effect"
                                   + $"（`{d[r.Trait].Prefab}` / `{r.Prefab}`）—— 今天只播第一条（数据里本不该出现，先出声）");
                    continue;
                }
                d[r.Trait] = r;
            }
            _fromCode = d;
            if (d.Count == 0)
                Debug.LogError("[TraitParticles] FromCode 粒子表**是空的**"
                             + "（`Resources/trait_effect_bindings.json` 没载到 / 没有 `TriggerType == 5` 的行）"
                             + " ⇒ 这一整条链（8 个调用点）一件都不会播");
        }

        /// <summary>FromCode 那档的原版枚举值（`EffectTriggerType.FromCode`；反编译里两个调用点都**硬编码 5**）。</summary>
        static class TraitEffectTriggerType
        {
            public const int FromCode = 5;
        }

        /// <summary>这张表**载进来几条**（断言用：期望值从数据重算，⛔ 不从本类读回）。</summary>
        public static int BindingCount { get { return FromCode.Count; } }

        /// <summary>表是不是空的（= 数据没载到；那时整条链不播，且上面已经 `LogError` 过）。</summary>
        public static bool Loaded { get { return FromCode.Count > 0; } }

        /// <summary>这张表里**有**哪些 trait 的从代码粒子（只读快照）。</summary>
        public static string[] Keys
        {
            get
            {
                var d = FromCode;
                if (d == null || d.Count == 0) return new string[0];
                var a = new string[d.Count];
                d.Keys.CopyTo(a, 0);
                return a;
            }
        }

        /// <summary>测试/断言用：把表重置（**只**给 Editor 自检造「载不到」的场景；生产路径别调）。</summary>
        public static void ResetForTest() { _fromCode = null; }

        /// <summary>这个 trait 有没有 FromCode 粒子（断言与出声用）。
        /// ⚠️ 传事件 `Keyword`（可能是 `UseFaith#1` 这种**传输形态**）也认 —— 见 <c>Norm(string, out int)</c>。</summary>
        public static bool Has(string trait)
        {
            int abilityIndex;
            string key = Norm(trait, out abilityIndex);
            NoteKeyForTest(trait, key, abilityIndex);
            return key.Length != 0 && FromCode.ContainsKey(key);
        }

        /// <summary>绑定的全部 trait（断言用：**每一件的 prefab 都该在效果库里**）。</summary>
        public static IEnumerable<string> Traits { get { return FromCode.Keys; } }

        /// <summary>按那张表查 prefab 名（没绑 ⇒ null）。**不带阵营** ⇒ 取那条的默认 `effectAnim`
        /// （不套 `overrideByArmies`）。生产路径走 `Play`（它会按卡反推阵营）。</summary>
        public static string PrefabFor(string trait)
        {
            return PrefabFor(trait, -1);
        }

        /// <summary>🆕 2026-10-23：**带阵营**的查表（原版这条链也带 army ——
        /// `CardScript__ActivateTraitParticlesFromCode.c:62` 把卡自己的 `cardArmy` 传给
        /// `GetTraitEffect(trait, 5, army)`）。解析只走 `TraitFrames.PrefabOf`（判据只此一处）。
        /// ⚠️ 同样认 `#i` 传输形态。</summary>
        public static string PrefabFor(string trait, int army)
        {
            int abilityIndex;
            string key = Norm(trait, out abilityIndex);
            NoteKeyForTest(trait, key, abilityIndex);
            TraitFrames.TraitEffectBinding row;
            if (key.Length == 0 || !FromCode.TryGetValue(key, out row)) return null;
            return TraitFrames.PrefabOf(row, army);
        }

        /// <summary>🆕 2026-10-23：从**挂点**反推这张卡的原版 army（`TraitEffect.GetEffect(army)` 的入参）。
        /// 挂点是 `CardView.transform`（`BattleDriver.PlayTraitParticles` 传 `v.transform`）⇒ 直接读那张卡；
        /// 取不到 ⇒ `-1`（= 不套覆盖、走默认件）。换算口 = `TraitFrames.ArmyOfFaction`（= `ArmyIcons.Id`）。
        /// ⚠️ 与 `TraitFrames.ArmyId` 同一条已知偏差（`CardData.faction` 是拥有者阵营，不是卡自己的）。</summary>
        static int ArmyOf(Transform at)
        {
            if (at == null) return -1;
            var cv = at.GetComponent<CardView>();
            if (cv == null) cv = at.GetComponentInParent<CardView>();
            return cv == null ? -1 : TraitFrames.ArmyOfFaction(cv.Data.faction);
        }

        /// <summary>在 `at` 那儿播它的 FromCode 粒子。返回 false = 没绑定 / 库里没有（**都出声**）。
        /// 原版是 `Instantiate` 在卡的 `effectsAnchor` 下（`ResolveTraitAnim` 那条路）⇒ 这里挂在卡的 transform 下。</summary>
        public static bool Play(string trait, Transform at, string why = null)
        {
            if (at == null) return false;
            var name = PrefabFor(trait, ArmyOf(at));   // 🆕 原版这一档也按卡阵营取 override
            if (string.IsNullOrEmpty(name)) return false;
            var p = WarpforgeEffectPlayer.Play(name, at, Vector3.zero, 1f);
            if (p == null)
            {
                Debug.LogWarning($"[TraitParticles] 效果库里没有 `{name}`（trait `{Norm(trait)}` 的 FromCode 粒子）"
                               + " —— 判据 → `资料/待办判据_战场与战斗视图.md` 的「trait 粒子」那一段");
                return false;
            }
            Debug.Log($"[Battle] trait `{Norm(trait)}` 的 FromCode 粒子 `{name}`"
                    + (string.IsNullOrEmpty(why) ? "" : $"（{why}）"));
            return true;
        }

        /// <summary>把 trait / 事件 `Keyword` 规范成查表键：**拆 `#i` 后缀** → trim → 小写。
        ///
        /// <para>🔴 2026-10-20（W57 · `SC-7`）：事件 `BattleEvent.Keyword` 可能是**传输形态** ——
        /// 双能力卡把能力序号拼在名字后面（`BattleEvent.KeywordWithAbility`，例 `UseFaith#1`，
        /// 判据 → <see cref="BattleEvent.SplitKeywordAbility"/>）。本类那张 `FromCode` 表（以及调用方
        /// `BattleDriver.PlayTraitParticles` 的 `EvtKind.Trigger` 支）用的都是**裸 trait 名**
        /// ⇒ 不拆就**查不到**，而且是**静默的**（`Play` 只返回 false、日志也不会印 miss 那一行）。</para>
        ///
        /// <para>⛔ **解码只此一处**：转发 `BattleEvent.SplitKeywordAbility`，**不在这里另写一份解析**；
        /// ⛔ 也不改事件本体（`VfxMap` 仍拿原始 `Keyword` 筛行）。
        /// 无后缀 / 后缀不是非负整数 ⇒ 原样返回（**旧调用点逐字节不变**）。</para>
        /// </summary>
        static string Norm(string s, out int abilityIndex)
        {
            abilityIndex = -1;
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim();
            if (s.Length == 0) return "";
            s = BattleEvent.SplitKeywordAbility(s, out abilityIndex);
            return s.Trim().ToLowerInvariant();
        }

        static string Norm(string s) { return Norm(s, out _); }

#if UNITY_EDITOR
        // —— 🆕 2026-10-20（W57 · `SC-7`）：**Editor-only 只读探针** ——
        //   只给 `Editor/**` 的断言看「一个键最后被认成了什么」，⛔ **不参与播不播的判断**。
        //   记的是**最近一次** `Has` / `PrefabFor`（`Play` 内部先调 `PrefabFor` ⇒ 也算它）收到的东西。
        static string _lastRawKeyForTest;
        static string _lastTriggerKeyForTest;
        static int _lastAbilityIndexForTest = -1;

        static void NoteKeyForTest(string raw, string key, int abilityIndex)
        {
            _lastRawKeyForTest = raw;
            _lastTriggerKeyForTest = key;
            _lastAbilityIndexForTest = abilityIndex;
        }

        /// <summary>🆕 自检探针（Editor-only · 只读）：最近一次查表收到的**原始键**（可能含 `#i` 传输后缀）。</summary>
        public static string LastRawKeyForTest { get { return _lastRawKeyForTest; } }

        /// <summary>🆕 自检探针（Editor-only · 只读）：最近一次查表用的**基名**（`#i` 已拆 · 已 trim + 小写）。
        /// 断言写法：传 `UseFaith#1` ⇒ 这里必须是 `usefaith`；传 `swarm` ⇒ 仍是 `swarm`。</summary>
        public static string LastTriggerKeyForTest { get { return _lastTriggerKeyForTest; } }

        /// <summary>🆕 自检探针（Editor-only · 只读）：最近一次拆出来的能力序号（**没有后缀 = `-1`**）。</summary>
        public static int LastAbilityIndexForTest { get { return _lastAbilityIndexForTest; } }

        /// <summary>🆕 自检探针（Editor-only）：清掉上一次的读数，免得断言读到别人留下的值。</summary>
        public static void ResetProbeForTest()
        {
            _lastRawKeyForTest = null;
            _lastTriggerKeyForTest = null;
            _lastAbilityIndexForTest = -1;
        }
#else
        // player 里零成本：探针本身不参与任何判断，`NoteKeyForTest` 只在 Editor 记读数。
        static void NoteKeyForTest(string raw, string key, int abilityIndex) { }
#endif
    }
}
