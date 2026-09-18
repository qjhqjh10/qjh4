// StarterCards.cs — 自己设计的一套测试卡牌
//
// 为什么不用原版那 1131 张：**简单版不需要**一堆复杂效果，用自设计的 26 张更可控。
//   ⚠️ 原来这里写的另一个理由是「版权（见项目任务.md「红线」）」—— **2026-09-18 用户已取消版权红线**，
//      那条理由不再成立；留下的理由只有「简单版不需要」。
// 原版卡的效果全是 desc 文本，v1 的引擎不解析 desc（战术卡还整个没实现）。
// 这里每张卡都是**纯数值 + 已实现的关键词**，打起来行为明确、可控。
//
// 名字用英文：卡面是自写的 5×7 点阵字库，**只支持 ASCII**（汉字渲染那条路没走通，
// 见 `CardPresentation/Core/TextCanvas.cs` 文件头）。
//
// 设计意图：
//   · **Ember Legion** —— 猛攻，近战数值高、有飞行和破甲
//   · **Tide Swarm**   —— 消耗，血厚护甲高、远程多、有护卫
//   两边都能打，风格不同，方便验证「护卫限制目标」「飞行免疫近战」这些规则真的生效。
//
// 费用曲线：1 费 ×2、2 费 ×3、3 费 ×3、4 费 ×2、5 费 ×1、6 费 ×1、7 费 ×1
//
// 2026-09-12：两边各挑了几张卡加上**触发效果**和**主动技能**（写法见下），
//   目的是让「部队卡发动技能」「触发效果」这两类特效在正常对局里**真的会出现**，
//   而不是只躺在 `VfxMap` 里等一个永远不来的事件。
using System.Collections.Generic;

namespace RuleEngine
{
    public static class StarterCards
    {
        public const string EmberFaction = "Ember";
        public const string TideFaction = "Tide";

        // 效果的写法（详见 `Core/EffectSpec.cs`）：`关键词: 动词 数值 目标`
        //   目标 ∈ Self / OwnWarlord / EnemyWarlord / EnemyUnit
        //   ⚠️ 目标必须写全 —— 不给默认值。默认值会让效果变成「悄悄打了谁」。

        static CardDef Unit(string name, int cost, int melee, int hp, int ranged = 0,
                            string faction = EmberFaction, string desc = "", params string[] kws)
        {
            return new CardDef(name, name, "unit", desc, "common", faction,
                               cost, melee, hp, ranged, kws);
        }

        static CardDef Warlord(string name, int melee, int hp, string faction)
        {
            return new CardDef(name, name, "hero", "", "legendary", faction, 0, melee, hp, 0, null);
        }

        /// <summary>Ember Legion —— 猛攻：近战高、有飞行、有护甲</summary>
        public static List<CardDef> Ember()
        {
            return new List<CardDef>
            {
                Warlord("Ember Warlord", 2, 30, EmberFaction),

                Unit("Scavenger",     1, 2, 2, 0, EmberFaction, "Cheap and cheerful."),
                Unit("Bulwark",       1, 1, 4, 0, EmberFaction, "Holds the line.", KeywordTable.Vanguard),
                Unit("Falcon",        2, 2, 2, 0, EmberFaction, "Strikes from above.", KeywordTable.Flying),
                // 弓手：打谁都能顺带往督军身上点一下
                Unit("Ember Archer",  2, 1, 2, 2, EmberFaction, "Picks off stragglers.",
                     "Strike: Damage 1 EnemyWarlord"),
                // 老兵：「Been through worse.」—— 挨打没死就还一手
                Unit("Veteran",       3, 4, 3, 0, EmberFaction, "Been through worse.",
                     "Penitence: Damage 1 EnemyWarlord"),
                // 铁甲兵：技能不吃反击 —— 2 攻打 2 伤，选技能就少挨一次还手
                Unit("Ironclad",      3, 2, 5, 0, EmberFaction, "Built to last.", "Armour 1",
                     "Ability: Damage 2 EnemyUnit"),
                Unit("Longbowman",    3, 1, 3, 3, EmberFaction, "Outranges everything.",
                     KeywordTable.LongRange),
                // 火法师：登场烧对面场上一把；对面没部队就空过
                Unit("Flamecaller",   4, 3, 4, 2, EmberFaction, "Burns at any range.",
                     "Rally: Damage 1 EnemyUnit"),
                // 影刃：斩杀之后顺手给督军一刀 —— 隐身单位本来就该是刺杀的路子
                Unit("Shadowblade",   4, 5, 3, 0, EmberFaction, "Unseen until it strikes.",
                     KeywordTable.Stealth, "Slay: Damage 2 EnemyWarlord"),
                Unit("Battering Ram", 5, 6, 6, 0, EmberFaction, "Armoured siege engine.", "Armour 1"),
                Unit("War Drake",     6, 6, 7, 0, EmberFaction, "Terror with wings.", KeywordTable.Flying),
                // 巨像：7 费下场就把对面场上砸掉一块
                Unit("Molten Colossus",7, 8, 8, 0, EmberFaction, "Slow. Unstoppable.", "Armour 2",
                     "Rally: Damage 2 EnemyUnit"),
            };
        }

        /// <summary>Tide Swarm —— 消耗：血厚、护甲、远程、护卫</summary>
        public static List<CardDef> Tide()
        {
            return new List<CardDef>
            {
                Warlord("Tide Warlord", 3, 30, TideFaction),

                Unit("Tide Minion",   1, 1, 2, 1, TideFaction, "Endless and numerous."),
                Unit("Wave Rider",    1, 2, 1, 0, TideFaction, "First in, first to fall."),
                // 珊瑚卫：0 攻，打不了人 —— 它的价值全在技能上（这也是 AI 会**主动**放技能的那张卡）
                Unit("Reef Guard",    2, 0, 7, 0, TideFaction, "A wall of coral.",
                     KeywordTable.Vanguard, "Ability: Heal 2 OwnWarlord"),
                Unit("Siren",         2, 3, 2, 0, TideFaction, "Sings sailors to their death.",
                     KeywordTable.Flying),
                Unit("Coral Archer",  2, 1, 2, 2, TideFaction, "Patient and precise.",
                     "Strike: Damage 1 EnemyWarlord"),
                // 贝壳兽：死了也要拽你一把
                Unit("Shellback",     3, 2, 6, 0, TideFaction, "Thick shell.", "Armour 1",
                     "Backlash: Damage 2 EnemyWarlord"),
                Unit("Ballista",      3, 0, 3, 4, TideFaction, "Never gets counterattacked.",
                     KeywordTable.LongRange, "Strike: Damage 1 EnemyWarlord"),
                Unit("Deep Hunter",   4, 5, 4, 0, TideFaction, "Ambushes from the dark.",
                     KeywordTable.Stealth),
                Unit("Iron Shell",    4, 3, 7, 0, TideFaction, "Nothing gets through.", "Armour 2"),
                Unit("Storm Priest",  5, 4, 5, 2, TideFaction, "Calls the storm.",
                     "Ability: Heal 3 OwnWarlord"),
                Unit("Leviathan",     6, 7, 7, 0, TideFaction, "Guards the shoal.",
                     KeywordTable.Vanguard),
                Unit("Abyss Titan",   7, 9, 9, 0, TideFaction, "From the deepest trench."),
            };
        }

        public static List<CardDef> Both()
        {
            var all = Ember();
            all.AddRange(Tide());
            return all;
        }

        /// <summary>
        /// 按阵营名取这一套卡（`Ember` / `Tide`）。
        /// ⚠️ 只认这两个 —— **别的名字会报错并返回空表**。
        /// （2026-09-12 之前是「不是 Tide 就当 Ember」，于是传 `"Ultramarines"` 会**静默**拿到一整套
        /// Ember 卡、还打得挺像样 —— 接原版阵营时这是个会骗人的坑。现在空表 + 报错，
        /// 调用方 `DeckBuilder.StarterDeck` 会明说「这个阵营没有卡」。）
        /// </summary>
        public static List<CardDef> Of(string faction)
        {
            if (faction == EmberFaction) return Ember();
            if (faction == TideFaction) return Tide();
            UnityEngine.Debug.LogError($"[RuleEngine] `StarterCards.Of(\"{faction}\")`：只认 "
                                     + $"`{EmberFaction}` / `{TideFaction}` 这两个自设计阵营。"
                                     + "原版阵营要走卡池 `CardDatabase.Load()`（见 `BattleDriver.Begin`）");
            return new List<CardDef>();
        }
    }
}
