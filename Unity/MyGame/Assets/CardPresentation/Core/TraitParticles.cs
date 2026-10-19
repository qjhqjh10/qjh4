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
// 🔴 **哪些 trait 有 FromCode 的 effectAnim —— 本地【判不了】**：那份 `CardTrait` SO 本体全库 0 命中
//    ⇒ 下面这张表是**按 `CardAnim` 名字 + trait 名逐字对应反推**出来的（**是我们的口径**，逐条标了来路）。
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
        /// <summary>trait（规范化键，与 `UnitState.Keywords` 同一套）→ 效果库里的 prefab 名。
        ///
        /// `数据/索引/anim_address_map.json` 的 `cardanim_to_asset` 反查出目标 prefab（**全在库里**），
        /// 再按「CardAnim 名里那个 trait 词」认领到 trait：
        ///   · **「名字逐字对上」**（`&lt;Trait>TraitFromCode`，证据最硬）：
        ///     `SniperTraitFromCode` · `MarkerlightTraitFromCode` · `SwarmTraitFromCode` · `SynapseTraitFromCode`；
        ///   · **「该 trait 只有这一件 anim」**（**我们的口径**，名字不带 `FromCode`）：
        ///     `LongRangeAnim` · `Trait Stomp` · `FerocityAnim`。
        /// ⚠️ `huntMark` **故意不绑** —— 那条调用点是 `…InTarget`（`TriggerOnMinionDeath`），
        ///    而映射表里**没有任何 `…InTarget` 结尾的 CardAnim**；最接近的 `HuntMark Damage Anim`
        ///    → `HuntMark_DamageEffect` **名字对不上、语义也没坐实** ⇒ 宁可留白（不猜）。</summary>
        static readonly Dictionary<string, string> FromCode = new Dictionary<string, string>
        {
            { "sniper",      "SniperPreEffect" },
            { "markerlight", "MarkerlightTriggerDamage" },
            { "swarm",       "Swarm_Trigger_OnTarget" },
            { "synapse",     "SynapseEffect" },
            { "longrange",   "LongRange_GainTrait" },
            { "stomp",       "StompEffect" },
            { "ferocity",    "FerocityEffect" },
        };

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

        /// <summary>按那张表查 prefab 名（没绑 ⇒ null）。
        /// ⚠️ 同样认 `#i` 传输形态（`Play` 走的就是这里，所以 `Play` 自动跟着认）。</summary>
        public static string PrefabFor(string trait)
        {
            int abilityIndex;
            string key = Norm(trait, out abilityIndex);
            NoteKeyForTest(trait, key, abilityIndex);
            string s;
            return (key.Length != 0 && FromCode.TryGetValue(key, out s)) ? s : null;
        }

        /// <summary>在 `at` 那儿播它的 FromCode 粒子。返回 false = 没绑定 / 库里没有（**都出声**）。
        /// 原版是 `Instantiate` 在卡的 `effectsAnchor` 下（`ResolveTraitAnim` 那条路）⇒ 这里挂在卡的 transform 下。</summary>
        public static bool Play(string trait, Transform at, string why = null)
        {
            var name = PrefabFor(trait);
            if (name == null) return false;
            if (at == null) return false;
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
