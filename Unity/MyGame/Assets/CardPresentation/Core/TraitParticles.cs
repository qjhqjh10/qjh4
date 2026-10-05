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

        /// <summary>这个 trait 有没有 FromCode 粒子（断言与出声用）。</summary>
        public static bool Has(string trait)
        {
            return !string.IsNullOrEmpty(trait) && FromCode.ContainsKey(Norm(trait));
        }

        /// <summary>绑定的全部 trait（断言用：**每一件的 prefab 都该在效果库里**）。</summary>
        public static IEnumerable<string> Traits { get { return FromCode.Keys; } }

        /// <summary>按那张表查 prefab 名（没绑 ⇒ null）。</summary>
        public static string PrefabFor(string trait)
        {
            string s;
            return (trait != null && FromCode.TryGetValue(Norm(trait), out s)) ? s : null;
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

        static string Norm(string s) { return string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant(); }
    }
}
