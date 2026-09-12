// VfxMap.cs — **卡牌动作 → 特效名** 的对照表
//
// 原版这张表在 `d:/4/Unity/数据/游戏数据/`：
//   · `card_anim_map.json`  —— 阵营 → {动作名 → 参数 + vfx guid}（13 个阵营）
//   · `card_vfx_tree.json`  —— guid → 那棵特效树（400 条）
//   · `atk_vfx_map.json`    —— 攻击类型名 → 特效名（297 条）
//   · `effect_index.json`   —— 958 个特效，**带还原质量判定**（`对得上`/`偏亮`/`只有原版有内容`…）
//
// ⚠️ 我们的卡是自研的（Scavenger / Bulwark…），原版的「卡 → guid」查不到，
//    所以这里按**事件 + 阵营**兜底，先保证「有特效、且是还原得对的那个」。
//    以后要逐卡配，把 `ByCard` 填起来即可（键用卡名）。
//
// **挑特效的依据是 `effect_index.json` 的判定列**，别凭名字挑 —— 名单里有一批
// `只有原版有内容（导出整个丢了）` / `两边全程空（纯脚本驱动）`，播出来是**空的**。
// 下面这些全是「对得上 + 高置信」的。
using System.Collections.Generic;

namespace CardPresentation
{
    public static class VfxMap
    {
        // ---- 事件名 ----
        public const string PlayCard     = "play";            // 出牌（手牌打出去的那一刻）
        public const string Deploy       = "deploy";          // 登场（落到格位上）
        public const string AttackMelee  = "attack_melee";    // 近战攻击
        public const string AttackRanged = "attack_ranged";   // 远程攻击
        public const string Hit          = "hit";             // 挨打（掉血）
        public const string Death        = "death";           // 阵亡
        public const string Ability      = "ability";         // 部队卡在场上发动技能
        public const string Trigger      = "trigger";         // 触发效果

        /// <summary>
        /// 按事件兜底。
        ///
        /// ⚠️ **这些是「在战斗场景里实拍过」才选进来的**（`Editor/VfxPicker.cs` 的试片台，
        ///    每个效果拍 3 个时刻）—— `effect_index.json` 判定「对得上」**不等于**在我们场景里也好看：
        ///    实测 `BulletImpactBurst_Blood` 渲成一整块灰方块、`Attack_Stomp` 某一帧是一大块黑菱形，
        ///    换个效果就没这问题。挑特效一定要看实拍。
        /// </summary>
        static readonly Dictionary<string, string> ByEvent = new Dictionary<string, string>
        {
            { PlayCard,     "Tap Blue Glow" },              // 手牌打出去的一下（轻）
            { Deploy,       "Sororitas Summon Basic" },     // 登场：火柱升起（实拍很好看）
            { AttackMelee,  "BulletImpactBurst_crowd" },    // 近战：碎屑迸溅
            { AttackRanged, "BulletImpact_2shot" },         // 远程：弹着点
            { Hit,          "Tap Firepit" },                // 挨打：小火星
            { Death,        "Explosion_Possession" },       // 阵亡：橙红爆炸 + 飞散的火线
            // 🆕 2026-09-12 挑的（这两类以前**引擎里没有对应事件，压根播不出来**，
            //    所以从来没被挑过 —— 之前填的两个是占位）：
            //      · `Tap Webway Portal` 试片时几乎看不见（只有几点蓝星）
            //      · `Tap Fire Eldar`    试片时是个**硬边绿方块**
            //    换成下面这两个，都是「对得上 + 高置信」且试片里干净可读的：
            { Ability,      "Buff_Blue_SW" },               // 发动技能：单位身上升起蓝色光柱
            { Trigger,      "Faith_trigger_unit" },         // 触发效果：金色环形符记 + 火苗
        };

        /// <summary>阵营覆盖：登场特效按阵营换（火/水两套明显不同的）</summary>
        static readonly Dictionary<string, Dictionary<string, string>> ByFaction =
            new Dictionary<string, Dictionary<string, string>>
        {
            { "Ember", new Dictionary<string, string> { { Deploy, "Sororitas Summon Basic" } } },
            { "Tide",  new Dictionary<string, string> { { Deploy, "Tau_SummonCircle" } } },
        };

        /// <summary>逐卡覆盖（键 = 卡名，和 `CardData.id` 一致）。现在是空的，留给以后配。</summary>
        static readonly Dictionary<string, Dictionary<string, string>> ByCard =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>查一个事件该播什么。查不到返回 null（调用处会跳过）。</summary>
        public static string Resolve(string evt, string faction = null, string cardId = null)
        {
            if (string.IsNullOrEmpty(evt)) return null;

            Dictionary<string, string> m;
            if (!string.IsNullOrEmpty(cardId) && ByCard.TryGetValue(cardId, out m))
            {
                string s;
                if (m.TryGetValue(evt, out s)) return s;
            }
            if (!string.IsNullOrEmpty(faction) && ByFaction.TryGetValue(faction, out m))
            {
                string s;
                if (m.TryGetValue(evt, out s)) return s;
            }
            string fallback;
            return ByEvent.TryGetValue(evt, out fallback) ? fallback : null;
        }

        /// <summary>自检用：把表里所有名字列一遍</summary>
        public static IEnumerable<string> AllNames()
        {
            foreach (var kv in ByEvent) yield return kv.Value;
            foreach (var f in ByFaction) foreach (var kv in f.Value) yield return kv.Value;
            foreach (var c in ByCard) foreach (var kv in c.Value) yield return kv.Value;
        }

        /// <summary>自检用：事件 → 最终会播的名字（带阵营/卡覆盖）</summary>
        public static string Describe(string faction = null)
        {
            var parts = new List<string>();
            string[] evts = { PlayCard, Deploy, AttackMelee, AttackRanged, Hit, Death, Ability, Trigger };
            foreach (var e in evts) parts.Add($"{e}={Resolve(e, faction) ?? "—"}");
            return (faction != null ? $"[{faction}] " : "") + string.Join("  ", parts.ToArray());
        }
    }
}
