// CardText.cs — 卡面上的中文文案（显示名 / 关键词 / 效果小字）
//
// **只放显示用的字**：规则、存档、测试、美术文件名一律用英文 `CardDef.Id` / `Name`，一个字都不改。
// 和原版一个路子 —— 原版卡上只存 `refNameId`，显示名走 I2 Localization 的 term 表。
//
// 中文有**两个来源**，别混：
//   1. **我们自己设计的 26 张卡** → 本文件里那几张表（`Names` / `KeywordNames` / `Phrases`…），
//      名字是我们自己起的。
//   2. **原版 1131 张卡** → 卡表里的 `CardDef.NameZh` / `DescZh`
//      （`数据/卡牌翻译/zh_cards.json` → `工具/gen_cards_engine.py` → `cards_engine.json`）。
//      ⚠️ 那是**原版的文案**，走的是和原版美术一样的口径：**个人使用、不进发布版本**，
//      而且它和 `cards_engine.json` 里的英文 `desc` 一样是**已进仓库的卡牌数据**
//      （红线第 4 条讲的是「原版解包资源与直接衍生物留在本地」，这条数据在 2026-09-12 之前就已经在仓库里了，
//      本次只是把中文和它并到一起，**没有新开口子**）。真要发布，两份一起换掉。
//
// ⚠️ 走中文的前提是**拿得到中文字体资产**（`TmpFont.Available`）：自写的 5×7 点阵字库
//    一个汉字也画不出来。字体不在就**自动回英文**，不会出现「汉字变方块/空白」。
//    建字体资产时统计要烘哪些字走 `AllChinese()`（那个不看当前语言，永远给中文）。
using System.Collections.Generic;
using RuleEngine;

namespace CardPresentation
{
    public static class CardText
    {
        /// <summary>当前显示中文吗。没字体就回英文 —— 见文件头</summary>
        public static bool Zh { get { return TmpFont.Available; } }

        // ==================================================================
        //  卡名（键 = `CardDef.Name`，也就是英文 id）
        // ==================================================================

        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            // ---- Ember Legion：猛攻，近战高、有飞行和破甲 ----
            { "Ember Warlord",   "余烬督军" },
            { "Scavenger",       "拾荒者" },
            { "Bulwark",         "壁垒" },
            { "Falcon",          "猎鹰" },
            { "Ember Archer",    "余烬弓手" },
            { "Veteran",         "老兵" },
            { "Ironclad",        "铁甲兵" },
            { "Longbowman",      "长弓手" },
            { "Flamecaller",     "焰唤者" },
            { "Shadowblade",     "影刃" },
            { "Battering Ram",   "攻城槌" },
            { "War Drake",       "战龙" },
            { "Molten Colossus", "熔岩巨像" },

            // ---- Tide Swarm：消耗，血厚护甲高、远程多、有护卫 ----
            { "Tide Warlord",    "潮汐督军" },
            { "Tide Minion",     "潮汐仆从" },
            { "Wave Rider",      "踏浪者" },
            { "Reef Guard",      "珊瑚卫" },
            { "Siren",           "海妖" },
            { "Coral Archer",    "珊瑚弓手" },
            { "Shellback",       "贝壳兽" },
            { "Ballista",        "弩炮" },
            { "Deep Hunter",     "深渊猎手" },
            { "Iron Shell",      "铁壳" },
            { "Storm Priest",    "风暴祭司" },
            { "Leviathan",       "利维坦" },
            { "Abyss Titan",     "深渊泰坦" },
        };

        // ==================================================================
        //  关键词（键 = `KeywordTable` 的规范名）
        // ==================================================================

        static readonly Dictionary<string, string> KeywordNames = new Dictionary<string, string>
        {
            { KeywordTable.Vanguard,  "先锋" },     // 对手必须先打它
            { KeywordTable.Flying,    "飞行" },     // 免疫近战
            { KeywordTable.Stealth,   "潜行" },     // 不能被选为目标
            { KeywordTable.LongRange, "远射" },     // 远程攻击不吃反击
            { KeywordTable.Armour,    "护甲" },     // 后面跟数值
            { KeywordTable.Shield,    "护盾" },
            { KeywordTable.Rally,     "集结" },     // 登场时触发
            { KeywordTable.Strike,    "猛击" },     // 攻击后触发
            { KeywordTable.Slay,      "斩杀" },     // 打死单位后触发
            { KeywordTable.Backlash,  "反噬" },     // 自己阵亡时触发
            { KeywordTable.Penitence, "忏悔" },     // 受伤没死时触发
            { KeywordTable.Ability,   "技能" },     // 本工程自定，非原版关键词
        };

        // ==================================================================
        //  效果小字的词（`Damage 2 EnemyUnit` 那类，`EffectSpec` 的封闭文法）
        // ==================================================================

        const string WDamage = "伤害";
        const string WHeal = "治疗";
        const string WDraw = "抽牌";
        const string WSelf = "自身";
        const string WOwnWarlord = "己方督军";
        const string WEnemyWarlord = "敌方督军";
        const string WEnemyUnit = "敌方单位";

        // ---- 技能卡面板（`ActiveSkillDesc`）那句整话用的词 ----
        // 「对敌方单位造成 2 点伤害」/「为自身回复 3 点生命」/「抽 1 张牌」
        // 单独列成常量是为了让 `AllChinese()` 能把它们收进字体语料（漏一个字 = 那个字没被检查过）
        const string WTo = "对";
        const string WDealMid = "造成";
        const string WDealEnd = "点伤害";
        const string WHealFor = "为";
        const string WHealMid = "回复";
        const string WHealEnd = "点生命";
        const string WDrawOne = "抽";
        const string WDrawEnd = "张牌";
        const string WTargets = "可选目标";

        // ==================================================================
        //  HUD 固定短语
        // ==================================================================
        //
        // 键就是**英文原文**，加一条新串只要往表里加一行；中文字体那边的语料
        // 会自动跟着走（见 `AllChinese`），不会出现「改了文案忘了重烘」。
        // ⚠️ HUD 走的是 `Label`，它有 TMP 后端之后才画得出汉字 ——
        //    拿不到字体资产时 `Zh == false`，`Phrase` 一律回英文，行为跟以前一样。

        static readonly Dictionary<string, string> Phrases = new Dictionary<string, string>
        {
            { "END TURN",     "结束回合" },
            { "YOUR TURN",    "你的回合" },
            { "ENEMY TURN",   "对手回合" },
            { "GAME OVER",    "对局结束" },
            { "YOU WIN",      "你赢了" },
            { "YOU LOSE",     "你输了" },
            { "DRAW",         "平局" },
            { "HAND",         "手牌" },
            { "DECK",         "牌组" },
            { "DISC",         "弃牌" },
            { "HP",           "生命" },
            { "CHOOSE ACTION",  "选择行动" },
            { "PICK A TARGET",  "选择目标" },
            { "NO LEGAL TARGET", "没有合法目标" },
            { "MELEE",        "近战" },
            { "RANGED",       "远程" },
            { "THIS UNIT ALREADY ACTED", "这个单位已经行动过了" },
            { "STUNNED",      "眩晕中" },
            { "THIS UNIT CANNOT ACT",    "这个单位无法行动" },
        };

        /// <summary>固定短语的中文。**没收录的照原样回英文**（不静默变空白）</summary>
        public static string Phrase(string en)
        {
            if (!Zh || string.IsNullOrEmpty(en)) return en;
            string zh;
            return Phrases.TryGetValue(en, out zh) ? zh : en;
        }

        /// <summary>`TURN 3` / `第 3 回合`（数字在中间，所以单列一个）</summary>
        public static string TurnLabel(int n)
        {
            return Zh ? "第 " + n + " 回合" : "TURN " + n;
        }

        /// <summary>阵营名（键 = `CardDef.Faction` / `StarterCards.*Faction`）</summary>
        static readonly Dictionary<string, string> FactionNames = new Dictionary<string, string>
        {
            { StarterCards.EmberFaction, "余烬" },
            { StarterCards.TideFaction,  "潮汐" },
        };

        /// <summary>阵营名的中文。**没收录的照原样大写回英文**（和以前的行为一致）</summary>
        public static string Faction(string key)
        {
            if (!Zh || string.IsNullOrEmpty(key)) return key == null ? key : key.ToUpperInvariant();
            string zh;
            return FactionNames.TryGetValue(key, out zh) ? zh : key.ToUpperInvariant();
        }

        // ==================================================================

        /// <summary>
        /// 卡的显示名。没收录的照原样回英文 —— **不要返回空串**，
        /// 那会变成一张没名字的卡，比英文名难查得多。
        /// </summary>
        public static string Name(string id)
        {
            if (!Zh || string.IsNullOrEmpty(id)) return id;
            string zh;
            return Names.TryGetValue(id, out zh) ? zh : id;
        }

        /// <summary>
        /// 显示名的**带上「卡表里的中文名」**那版 —— 原版卡走这条。
        ///
        /// 两份中文来源分工：
        ///   · **我们自己设计的 26 张** → 上面那张 `Names` 表（我们起的名字）
        ///   · **原版 1131 张** → 卡表里的 `CardDef.NameZh`（`数据/卡牌翻译/zh_cards.json`，
        ///     2026-09-12 由 `工具/gen_cards_engine.py` 并进 `cards_engine.json`）
        /// 两边都没有就回英文 id（**不静默**：1131 张里目前有 3 张没有中文名）。
        /// </summary>
        public static string Name(string id, string nameZh)
        {
            if (!string.IsNullOrEmpty(nameZh)) return nameZh;
            return Name(id);
        }

        /// <summary>关键词的中文名。`ARMOUR 2` 那种带数值的由调用方拼（`Keyword(Armour) + " " + n`）。
        /// 没收录的照样回大写英文，**行为跟以前一致**。</summary>
        public static string Keyword(string norm)
        {
            if (string.IsNullOrEmpty(norm)) return norm;
            if (!Zh) return norm.ToUpperInvariant();
            string zh;
            return KeywordNames.TryGetValue(norm, out zh) ? zh : norm.ToUpperInvariant();
        }

        /// <summary>效果的中文小字（`伤害2·敌方单位`）。字体不在时退回引擎原文（`DMG2 UNIT`）</summary>
        public static string Effect(EffectSpec spec)
        {
            if (spec == null) return null;
            if (!Zh) return spec.Short();

            string verb = spec.Verb == "damage" ? WDamage
                        : spec.Verb == "heal" ? WHeal
                        : WDraw;
            string s = verb + spec.Amount;
            string t = TargetZh(spec.Target);
            return t.Length > 0 ? s + "·" + t : s;
        }

        static string TargetZh(string t)
        {
            switch (t)
            {
                case EffectTargets.Self: return WSelf;
                case EffectTargets.OwnWarlord: return WOwnWarlord;
                case EffectTargets.EnemyWarlord: return WEnemyWarlord;
                case EffectTargets.EnemyUnit: return WEnemyUnit;
                default: return "";
            }
        }

        static string TargetEn(string t)
        {
            switch (t)
            {
                case EffectTargets.Self: return "self";
                case EffectTargets.OwnWarlord: return "your warlord";
                case EffectTargets.EnemyWarlord: return "the enemy warlord";
                case EffectTargets.EnemyUnit: return "an enemy unit";
                default: return "?";
            }
        }

        /// <summary>
        /// 效果 → **一整句**（技能卡面板 `ActiveSkillDesc.DescText` 用）。
        /// 和 `Effect()` 的区别：那个是卡面上省地方的小字（`伤害2·敌方单位`），这个是句子。
        /// </summary>
        public static string EffectSentence(EffectSpec spec)
        {
            if (spec == null) return "";

            if (spec.Verb == "draw")
                return Zh ? WDrawOne + " " + spec.Amount + " " + WDrawEnd
                          : "Draw " + spec.Amount + (spec.Amount > 1 ? " cards" : " card");

            if (spec.Verb == "heal")
                return Zh ? WHealFor + TargetZh(spec.Target) + WHealMid + " " + spec.Amount + " " + WHealEnd
                          : "Restore " + spec.Amount + " health to " + TargetEn(spec.Target);

            return Zh ? WTo + TargetZh(spec.Target) + WDealMid + " " + spec.Amount + " " + WDealEnd
                      : "Deal " + spec.Amount + " damage to " + TargetEn(spec.Target);
        }

        /// <summary>技能卡面板上的「可选目标数」：`3 available`（原版默认文本 `"0 available"`）/`可选目标 3`</summary>
        public static string TargetsAvailable(int n)
        {
            return Zh ? WTargets + " " + n : n + " available";
        }

        /// <summary>
        /// 表里**所有**中文串。给 `TmpSetup` 建字体资产时统计要烘哪些字用 ——
        /// **不看当前语言**（建资产那会儿字体还不存在，`Zh` 必然是 false）。
        /// 以后加卡/加关键词，字会自动进语料，不会漏（「两处写同一条规则 = 迟早不一致」）。
        /// </summary>
        public static IEnumerable<string> AllChinese()
        {
            foreach (var v in Names.Values) yield return v;
            foreach (var v in KeywordNames.Values) yield return v;
            foreach (var v in Phrases.Values) yield return v;
            foreach (var v in FactionNames.Values) yield return v;
            yield return WDamage; yield return WHeal; yield return WDraw;
            yield return WSelf; yield return WOwnWarlord;
            yield return WEnemyWarlord; yield return WEnemyUnit;
            // 技能卡面板那句整话的构件 —— 漏一个 = 那个字没被字体覆盖检查扫到
            yield return WTo; yield return WDealMid; yield return WDealEnd;
            yield return WHealFor; yield return WHealMid; yield return WHealEnd;
            yield return WDrawOne; yield return WDrawEnd; yield return WTargets;
        }
    }
}
