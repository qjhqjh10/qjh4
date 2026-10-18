// LegacyCardIds.cs — 存档 / 回放里的「旧卡 id」**读时迁移闸**（幂等、只改内存）
//
// ================================ 这是什么 ================================
// 卡池 id 换过**两代**，两次都**没有**迁移玩家存档 / 回放：
//   · **代 1（2026-10-14 00:09，commit `601005b`）** —— 本文件的**主表**：
//     `cards_engine.json` 里 **955/1126** 张卡换了 id（换成原版真 id 的第一批）。
//     ⇒ **10-14 之前**建的卡组 / 录的回放里，**同一个 id 现在指着另一张卡**
//       （本机那副「复仇者之刃」30 张里 13 个 id 全被改指；防御位 `ASH67` 从
//        `Aspect Shrine`(defence) 变成了 `Hornet`(unit)）。**不是孤例，是一类。**
//   · **代 2（2026-10-18，`K06`）** —— 卡池里最后那批**自造 token**（`ASH_Autarch` 这种）
//     换成真 id。
//     🔑 本表是按 **「旧池 → 现池」** 直接 join 出来的，**两代都覆盖**（自造 token 也在表里）。
//
// 为什么「读时迁移」而不是「改文件」：
//   · 玩家数据文件**一个字都不碰**（⛔ 不许删改玩家存档 / 录像）；
//   · 迁移只发生在**内存里**；玩家下次正常保存时由存档那条既存路径写回；
//   · **幂等** —— 判据**不是表**，而是 `MigrateDeckVerified` 的**双解闸门**：
//     迁完之后这副牌在**现代解**下就合法了 ⇒ 下一次进这个函数会**第一步就返回**。
//     🔴 **⛔ 别把它改成「无条件查表替换」** —— 本表**不是单射**：
//     旧 id `ASH20`→`ASH15`，而 `ASH15` 自己又是另一条映射的 key（`ASH15`→`ASH11`）
//     ⇒ **无条件再迁一次会静默改成第三张卡**。实测：表里 **890/1102** 个值是另一个 key，
//     **最长链 31 跳**。所以铁律两条：
//       ① **一次遍历、查表只看【原始值】**（`ApplyMap` —— 绝不看已替换的结果）；
//       ② **只有「现代解不合法 ∧ 旧代解合法」才落**（`MigrateByDualSolution`）。
//
// ⛔ **运行时绝不用「按卡名匹配」兜底** —— 这张表是**离线下逐条 join 出来的**。
//    生成器（不入 git）→ `Unity/_tmp_view/gen_legacy_card_ids.py`；
//    只读自验 → `Unity/_tmp_view/verify_legacy_migration.py`；
//    报告（条数 / 对不上的 / 双解规则 / 自验结果）→ `资料/普查产出_1018/W_池id代际迁移.md`。
//
// ================================ 表的来源（可复算） ================================
// ① **主表 1092 条** = `git show 601005b^:Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`
//    与**当前** `cards_engine.json` 按 `(name, faction)` **精确 join**，只收 `旧 id != 现 id` 的。
//    · 两个文件里 `(name, faction)` **各自都没有重复**（1126 / 1127 张）⇒ 没有一对多歧义。
//    · **对不上的 = 0 条**（旧池 1116 张按名字在现池都找得到；其余 10 张是**改名**，见 ②）。
// ② **改名表 10 条** —— 旧名与现名不同（名字 join 接不上）。判据 = **PnP 编号链**：
//    旧 id 的数字后缀 = **PnP 卡图编号**（`A970` 那套口径），该编号那张卡**印的名字** = 现池卡名
//    （铁律 7：卡面是印刷品上的字，可逐张核）。例：
//      `SOR19 "Sisters Repentia"` → PnP #19 = `Warpforge_19_Sister-Repentia.png`（印 `Sister Repentia`）
//      → 现池 `SOR10`。
//    逐条证据写在那 10 行的行尾注释上。
// ③ 现池**新增** 1 张（`TL40 Infestation Node` —— 旧池没有这张卡）⇒ 表里没有它，正常。
// ④ 旧池里**按名字/PnP 都对不上**的：**0 条**。
//
// ================================ 这张表**不能**判的事 ================================
//   · **代际歧义**：`CardIds` 里一个 id 既可能是旧代那张卡、也可能是现池同一 id 的另一张卡
//     （891/1102 个旧 id **今天仍是合法 id**）⇒ 单看 id 分不出来。判据 = **整副双解**，
//     两解都合法 / 都非法时**一律不动并出声**（⛔ 不许猜）。
//   · **改名表之外**：旧池还有一个「对应关系不明」的都没有；以后扫出新旧 token 再往表里加。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    /// <summary>旧（代际 1 / 代际 2）卡 id → 当前卡池 id 的**静态迁移表** + 读入口用的迁移口。</summary>
    public static class LegacyCardIds
    {
        /// <summary>日志前缀（查问题时 `grep LegacyCardIds` 即可）。</summary>
        public const string Tag = "[LegacyCardIds]";

        /// <summary>
        /// 🔴 **唯一**的映射表：`旧 id → 现池 id`。**只放已确认的映射**（判不出对应关系的，⛔ 不许猜着写进来）。
        /// 表头（生成方法 / 判据 / 覆盖范围 / 为什么不能无条件反复用）见本文件顶部那段。
        ///
        /// 行尾注释 = `卡名 · 阵营`；带 `← 改名：` 的那 10 行是「旧名与现名不同」的，判据 = PnP 编号链。
        /// </summary>
        static readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "AM1", "AM3" },   // Commissar Denkler · AstraMilitarum  ← 改名：Warpforge_01_Commissar-Denkler.png（官方 Card_Name/AM3 = Commissar Denkler，Warlord_Lore/AM3 = 'Commissar Elan Denkler'）
            { "AM10", "AM5" },   // Shock Trooper · AstraMilitarum
            { "AM11", "AM7" },   // Vox Operator · AstraMilitarum
            { "AM12", "AM14" },   // Autocannon Squad · AstraMilitarum
            { "AM13", "AM12" },   // Flamer Trooper · AstraMilitarum
            { "AM14", "AM9" },   // Kasrkin · AstraMilitarum
            { "AM15", "AM18" },   // Master of Ordnance · AstraMilitarum
            { "AM16", "AM15" },   // Minka Lesk · AstraMilitarum
            { "AM17", "3a52026b99bb56f40aa9dc130fc66f34" },   // Cadian Honour · AstraMilitarum
            { "AM18", "AM13" },   // Ogryn · AstraMilitarum
            { "AM19", "AM11" },   // Shock Trooper Sgt · AstraMilitarum
            { "AM2", "cc11838649ff68a40b7d5d2b9fccd2bc" },   // Lead by Example · AstraMilitarum
            { "AM20", "AM59" },   // Astropath · AstraMilitarum
            { "AM21", "AM19" },   // Attilan Rough Rider · AstraMilitarum
            { "AM22", "AM10" },   // Cadian Standard Bearer · AstraMilitarum
            { "AM23", "AM17" },   // Commissar · AstraMilitarum
            { "AM24", "AM16" },   // Scout Sentinel · AstraMilitarum
            { "AM25", "AM21" },   // Tech-Priest Enginseer · AstraMilitarum
            { "AM26", "AM25" },   // Armoured Sentinel · AstraMilitarum
            { "AM27", "AM23" },   // Bombast Field Gun · AstraMilitarum
            { "AM28", "AM24" },   // Bullgryn Bone’ead · AstraMilitarum
            { "AM29", "AM22" },   // Primaris Psyker · AstraMilitarum
            { "AM3", "AM1" },   // Lord Solar Leontus · AstraMilitarum
            { "AM30", "AM26" },   // Taurox · AstraMilitarum
            { "AM32", "AM28" },   // Chimera · AstraMilitarum
            { "AM33", "AM29" },   // Hydra · AstraMilitarum
            { "AM34", "AM30" },   // Manticore · AstraMilitarum
            { "AM35", "AM27" },   // Nork Deddog · AstraMilitarum
            { "AM36", "98cd4c021d615174aba88df6012219c4" },   // Renowned Bodyguard · AstraMilitarum
            { "AM37", "AM33" },   // Basilisk · AstraMilitarum
            { "AM38", "AM32" },   // Leman Russ · AstraMilitarum
            { "AM39", "AM34" },   // Valkyrie · AstraMilitarum
            { "AM40", "AM35" },   // Leman Russ Vanquisher · AstraMilitarum
            { "AM41", "AM36" },   // Maelon Dhrost · AstraMilitarum
            { "AM42", "AM37" },   // Rogal Dorn Tank · AstraMilitarum
            { "AM43", "AM38" },   // Baneblade Tank · AstraMilitarum
            { "AM44", "AM39" },   // Born Soldier · AstraMilitarum
            { "AM45", "AM40" },   // Full Throttle · AstraMilitarum
            { "AM46", "AM41" },   // Move! Move! Move! · AstraMilitarum
            { "AM47", "AM42" },   // Pinning Fire · AstraMilitarum
            { "AM48", "AM43" },   // Recon Operation · AstraMilitarum
            { "AM49", "AM44" },   // Shock Troops · AstraMilitarum
            { "AM5", "AM2" },   // Ursula Creed · AstraMilitarum
            { "AM50", "AM45" },   // Thunderous Charge · AstraMilitarum
            { "AM51", "AM47" },   // Press the Attack · AstraMilitarum
            { "AM52", "AM48" },   // Regimental Doctrine · AstraMilitarum
            { "AM53", "AM49" },   // Summary Execution · AstraMilitarum
            { "AM54", "AM50" },   // Thermal Weaponry · AstraMilitarum
            { "AM55", "AM52" },   // Armoured Squadron · AstraMilitarum
            { "AM56", "AM51" },   // Cadia Stands · AstraMilitarum
            { "AM57", "AM46" },   // High Command · AstraMilitarum
            { "AM58", "AM53" },   // Artillery Strike · AstraMilitarum
            { "AM59", "AM54" },   // At All Costs · AstraMilitarum
            { "AM6", "af98d423c7279a14c84c0d0382c53541" },   // Legendary Commandant · AstraMilitarum
            { "AM60", "AM55" },   // Mechanised Infantry · AstraMilitarum
            { "AM61", "AM56" },   // Vengeance for Cadia · AstraMilitarum
            { "AM62", "AM57" },   // Godspear Warhead · AstraMilitarum
            { "AM63", "AM58" },   // Siege Warfare · AstraMilitarum
            { "AM64", "AM61" },   // Defence Batteries · AstraMilitarum
            { "AM65", "AM60" },   // Timely Reinforcements · AstraMilitarum
            { "AM66", "AM62" },   // Munitorum Supplies · AstraMilitarum
            { "AM67", "1ff4815a506d2f34e94ec2f50900d352" },   // Efficiency and Excellence · AstraMilitarum
            { "AM68", "AM67" },   // Aquilon Servo-Sentry · AstraMilitarum
            { "AM69", "AM68" },   // Tempestus Scion · AstraMilitarum
            { "AM7", "AM4" },   // Faeburn Vanquisher · AstraMilitarum
            { "AM70", "AM69" },   // Tempestor Sergeant · AstraMilitarum
            { "AM71", "AM70" },   // Medic Scion · AstraMilitarum
            { "AM72", "AM71" },   // Tempestus Aquilon · AstraMilitarum
            { "AM73", "AM72" },   // Vitus Gryf · AstraMilitarum
            { "AM74", "AM73" },   // Taurox Prime · AstraMilitarum
            { "AM75", "AM74" },   // Forged Killers · AstraMilitarum
            { "AM76", "AM75" },   // Valkyrie Drop · AstraMilitarum
            { "AM77", "AM76" },   // Armoured Lightning · AstraMilitarum
            { "AM9", "AM6" },   // Ratling · AstraMilitarum
            { "AM_Hektor_Thenmann", "AM66" },   // Hektor Thenmann · AstraMilitarum
            { "AM_Lord_Commander", "c06453b330af61745beb5fa567ba863e" },   // Lord Commander · AstraMilitarum
            { "ASH10", "ASH7" },   // Veteran Guardian · SaimHann
            { "ASH11", "ASH8" },   // Warlock · SaimHann
            { "ASH12", "ASH61" },   // Weapons Platform · SaimHann
            { "ASH13", "ASH9" },   // Windrider · SaimHann
            { "ASH14", "ASH10" },   // Shroud Runner · SaimHann
            { "ASH15", "ASH11" },   // Striking Scorpion · SaimHann
            { "ASH16", "ASH12" },   // Swooping Hawk · SaimHann
            { "ASH17", "ASH13" },   // Warp Spider · SaimHann
            { "ASH18", "ASH14" },   // Wraithblade · SaimHann
            { "ASH19", "ASH62" },   // Dire Avenger · SaimHann
            { "ASH2", "09bcb1b3b8794c4429d82d4b2e68fa1f" },   // Path of the Seer · SaimHann
            { "ASH20", "ASH15" },   // Howling Banshee · SaimHann
            { "ASH21", "ASH16" },   // Nuadhu Fireheart · SaimHann
            { "ASH22", "ASH18" },   // Vyper · SaimHann
            { "ASH23", "ASH19" },   // Witchblade Warlock · SaimHann
            { "ASH24", "d3e99cb18b8cbba4081486f57f510f75" },   // Witchfire · SaimHann
            { "ASH25", "ASH20" },   // Wraithguard · SaimHann
            { "ASH26", "ASH17" },   // Spiritseer · SaimHann
            { "ASH27", "ASH23" },   // Striking Scorpion Exarch · SaimHann
            { "ASH28", "ASH24" },   // Vengeful Wraithblade · SaimHann
            { "ASH29", "ASH26" },   // War Walker · SaimHann
            { "ASH3", "ASH2" },   // Eliac Zephyrblade · SaimHann
            { "ASH30", "ASH25" },   // Warlock Skyrunner · SaimHann
            { "ASH31", "ASH21" },   // Crimson Hunter · SaimHann
            { "ASH32", "ASH63" },   // Dire Avenger Exarch · SaimHann
            { "ASH33", "ASH27" },   // Falcon · SaimHann
            { "ASH34", "ASH22" },   // Fire Dragon · SaimHann
            { "ASH35", "ASH28" },   // Spiritseer Qelenaris · SaimHann
            { "ASH36", "ASH30" },   // Fire prism · SaimHann
            { "ASH37", "ASH29" },   // Wave Serpent · SaimHann
            { "ASH38", "e59ac9e46f4ea804784e954c3b820e77" },   // Skyborne Deployment · SaimHann
            { "ASH39", "ASH31" },   // Winged Autarch · SaimHann
            { "ASH4", "1bfe0893592c9dd40bed8d68d5d2b516" },   // Path of the Warrior · SaimHann
            { "ASH40", "ASH33" },   // Hemlock Wraithfighter · SaimHann
            { "ASH41", "ASH32" },   // Wraithlord · SaimHann
            { "ASH42", "ASH34" },   // Avatar of Khaine · SaimHann
            { "ASH43", "33eabec155f628440a426a4ba480ec24" },   // Wrath of Khaine · SaimHann
            { "ASH44", "ASH35" },   // Wraithknight · SaimHann
            { "ASH45", "ASH36" },   // Fire and Fade · SaimHann
            { "ASH46", "ASH37" },   // Webway Incursion · SaimHann
            { "ASH47", "ASH38" },   // Will of Asuryan · SaimHann
            { "ASH48", "ASH39" },   // Deadly Ambush · SaimHann
            { "ASH49", "ASH40" },   // Nightshade Interceptors · SaimHann
            { "ASH5", "ASH3" },   // Medreyal Ghaelyn · SaimHann
            { "ASH50", "ASH41" },   // Webway Gate · SaimHann
            { "ASH51", "ASH45" },   // Banshee Mask · SaimHann
            { "ASH52", "ASH48" },   // Cosmic Serpent · SaimHann
            { "ASH53", "ASH64" },   // Devoted of Khaine · SaimHann
            { "ASH54", "ASH43" },   // Sudden Assault · SaimHann
            { "ASH55", "ASH44" },   // Wild Host · SaimHann
            { "ASH56", "ASH46" },   // Aerial Dominion · SaimHann
            { "ASH57", "ASH47" },   // Bladestorm · SaimHann
            { "ASH58", "ASH42" },   // Dance of Death · SaimHann
            { "ASH59", "ASH49" },   // Eldritch Storm · SaimHann
            { "ASH6", "9c3562ffa183ede41b1626d9c9dc1e2f" },   // Path of Command · SaimHann
            { "ASH60", "ASH50" },   // Evasive Manoeuvre · SaimHann
            { "ASH61", "ASH51" },   // Infinity Circuit · SaimHann
            { "ASH62", "ASH52" },   // Hosts of the Dead · SaimHann
            { "ASH63", "ASH53" },   // Reclaim the Stars · SaimHann
            { "ASH64", "ASH54" },   // Wailing Doom · SaimHann
            { "ASH65", "ASH56" },   // Inspiring Legacy · SaimHann
            { "ASH66", "ASH57" },   // Webway Entrance · SaimHann
            { "ASH67", "ASH55" },   // Aspect Shrine · SaimHann
            { "ASH7", "ASH4" },   // Guardian Defender · SaimHann
            { "ASH75", "851f73daf747eb642934a16429b391db" },   // Storm of Silence · SaimHann
            { "ASH76", "ASH65" },   // Shining Spear · SaimHann
            { "ASH77", "ASH66" },   // Shining Spear Exarch · SaimHann
            { "ASH78", "ASH67" },   // Hornet · SaimHann
            { "ASH79", "ASH69" },   // Howling Banshee Exarch · SaimHann
            { "ASH8", "ASH5" },   // Storm Guardian · SaimHann
            { "ASH80", "ASH70" },   // Swooping Hawk Exarch · SaimHann
            { "ASH81", "ASH68" },   // Night Spinner · SaimHann
            { "ASH82", "ASH75" },   // Yrlla the Huntress · SaimHann
            { "ASH83", "ASH71" },   // Craftworld Convergence · SaimHann
            { "ASH84", "ASH72" },   // Graceful Avoidance · SaimHann
            { "ASH85", "ASH73" },   // Whirling Death · SaimHann
            { "ASH9", "ASH6" },   // Ranger · SaimHann
            { "ASH_Ahnakh_Yth_Shrine", "ASH84" },   // Ahnakh-Yth Shrine · SaimHann
            { "ASH_Autarch", "ASH83" },   // Autarch · SaimHann
            { "ASH_Bright_Lance_Vyper", "ASH81" },   // Bright Lance Vyper · SaimHann
            { "ASH_Death_Spinner_Warp_Spider", "ASH77" },   // Death Spinner Warp Spider · SaimHann
            { "ASH_Empyric_Ambush", "dc7e575c0a328f446be1521c23ec850b" },   // Empyric Ambush · SaimHann
            { "ASH_Farseer", "ASH80" },   // Farseer · SaimHann
            { "ASH_Farseer_Skyrunner", "ASH82" },   // Farseer Skyrunner · SaimHann
            { "ASH_Fate_Inescapable", "ASH85" },   // Fate Inescapable · SaimHann
            { "ASH_Flickerjump", "67adbc7f7bce2bd4c8f6937fab3134e5" },   // Flickerjump · SaimHann
            { "ASH_Forewarned", "ASH86" },   // Forewarned · SaimHann
            { "ASH_Lhykhis", "ASH76" },   // Lhykhis · SaimHann
            { "ASH_Orian_Laratharjos", "ASH79" },   // Orian Laratharjos · SaimHann
            { "ASH_Warp_Spider_Exarch", "ASH78" },   // Warp Spider Exarch · SaimHann
            { "BL1", "BL34" },   // Abaddon the Despoiler · BlackLegion
            { "BL10", "BL62" },   // Dark Disciple · BlackLegion
            { "BL11", "BL2" },   // Black Legionary · BlackLegion
            { "BL12", "BL3" },   // Chaos Legionary · BlackLegion
            { "BL13", "BL4" },   // Chaos Sergeant · BlackLegion
            { "BL14", "BL5" },   // Hound of Abaddon · BlackLegion
            { "BL15", "BL6" },   // Rubric Marine · BlackLegion
            { "BL16", "4a48457c24877f345be2e96f867c1e94" },   // Dark Pact of Fate · BlackLegion
            { "BL17", "BL7" },   // Khorne Berzerker · BlackLegion
            { "BL18", "9a2cadc64c80a6b42912441105bb1cee" },   // Dark Pact of Blood · BlackLegion
            { "BL19", "BL8" },   // Noise Marine · BlackLegion
            { "BL2", "97e959444b53f424ea4285e2ca02c7e6" },   // Chosen of the Four · BlackLegion
            { "BL20", "b8e81b2fe85cda747a68c4d6326b669c" },   // Dark Pact of Excess · BlackLegion
            { "BL21", "BL10" },   // Plague Marine · BlackLegion
            { "BL22", "1dfc9bc738bfc4f4d93d48f165aee510" },   // Dark Pact of Resilience · BlackLegion
            { "BL23", "BL9" },   // Possessed Marine · BlackLegion
            { "BL24", "BL11" },   // Veteran Legionary · BlackLegion
            { "BL25", "BL12" },   // Aspiring Champion · BlackLegion
            { "BL26", "BL13" },   // Chosen · BlackLegion
            { "BL27", "BL14" },   // Daemon Blade Legionary · BlackLegion
            { "BL28", "BL15" },   // Master of Executions · BlackLegion
            { "BL29", "BL16" },   // Possessed Champion · BlackLegion
            { "BL3", "BL35" },   // Haarken Worldclaimer · BlackLegion
            { "BL30", "BL17" },   // Warp Talon · BlackLegion
            { "BL31", "BL18" },   // Bringer of decay · BlackLegion
            { "BL32", "BL63" },   // Havoc · BlackLegion
            { "BL33", "BL21" },   // Lheorvine Ukris · BlackLegion
            { "BL34", "BL22" },   // Master of Possession · BlackLegion
            { "BL35", "3f38d2e2c83df4b4c9052ef4a510cfbb" },   // Rites of Possession · BlackLegion
            { "BL36", "BL23" },   // Sorcerer · BlackLegion
            { "BL37", "BL19" },   // Chaos Terminator · BlackLegion
            { "BL38", "BL20" },   // Child of Torment · BlackLegion
            { "BL39", "BL24" },   // Chosen Champion · BlackLegion
            { "BL4", "61046837c330b48978b4af5201230b6e" },   // Helspear Assault · BlackLegion
            { "BL40", "BL25" },   // Dark Apostle · BlackLegion
            { "BL41", "BL27" },   // Venomcrawler · BlackLegion
            { "BL42", "BL28" },   // Helbrute · BlackLegion
            { "BL43", "BL26" },   // Obliterator · BlackLegion
            { "BL44", "BL29" },   // Terminator Champion · BlackLegion
            { "BL45", "BL30" },   // Chaos Lord · BlackLegion
            { "BL46", "BL31" },   // Maulerfiend · BlackLegion
            { "BL47", "BL32" },   // Iskandar Khayon · BlackLegion
            { "BL48", "BL33" },   // Vex Machinator · BlackLegion
            { "BL49", "BL37" },   // Abaddons Chosen · BlackLegion
            { "BL5", "BL36" },   // Sylar Hexscorn · BlackLegion  ← 改名：Warpforge_5_Sylar-Hexscorn.png
            { "BL50", "BL39" },   // Hideous Mutation · BlackLegion
            { "BL51", "BL38" },   // Dark Oratory · BlackLegion
            { "BL52", "BL40" },   // Legacy of Vengeance · BlackLegion
            { "BL53", "BL42" },   // Traitors Hate · BlackLegion
            { "BL54", "BL43" },   // Daemonic Frenzy · BlackLegion
            { "BL55", "BL47" },   // Disgustingly Resilient · BlackLegion
            { "BL56", "BL46" },   // Gifts of Chaos · BlackLegion
            { "BL57", "BL51" },   // Hosts of Chaos · BlackLegion
            { "BL58", "BL48" },   // Skeins of Fate · BlackLegion
            { "BL59", "BL45" },   // Spawndom · BlackLegion
            { "BL6", "7bf8ce67fb6be4744ae44345a5b465fd" },   // Diabolic Strength · BlackLegion
            { "BL60", "BL44" },   // Daemonic Pact · BlackLegion
            { "BL61", "BL49" },   // Delightful Agonies · BlackLegion
            { "BL62", "BL50" },   // Execution · BlackLegion
            { "BL63", "BL64" },   // Murderous Desires · BlackLegion
            { "BL64", "BL54" },   // Drachnyen · BlackLegion
            { "BL65", "BL53" },   // Heldrake Strike · BlackLegion
            { "BL66", "BL52" },   // Unholy Smite · BlackLegion
            { "BL67", "BL41" },   // Possession · BlackLegion
            { "BL68", "BL55" },   // Black Crusade · BlackLegion
            { "BL69", "BL57" },   // Hellfire Torch · BlackLegion
            { "BL7", "453b0e8f88e094a17866a6979f389ef5" },   // Infernal Gaze · BlackLegion
            { "BL70", "BL56" },   // Throne of the Heretic · BlackLegion
            { "BL71", "BL58" },   // Hellfire Pit · BlackLegion
            { "BL72", "BL70" },   // Veteran Havoc · BlackLegion
            { "BL73", "BL71" },   // Havoc Champion · BlackLegion
            { "BL74", "BL72" },   // Accursed Helbrute · BlackLegion
            { "BL75", "BL73" },   // Malicious Volleys · BlackLegion
            { "BL76", "BL75" },   // Idolatrous Despoilers · BlackLegion
            { "BL77", "BL74" },   // Spreading Corruption · BlackLegion
            { "BL8", "141c5ed661a9e42bd9a793f0d2a1c682" },   // Unhallowed Gifts · BlackLegion
            { "BL9", "BL1" },   // Chaos Spawn · BlackLegion
            { "BL_Bitter_Blows", "36892cd906b0e5e4c9bbbe249db009b8" },   // Bitter Blows · BlackLegion
            { "BL_Ghallaron_s_Champion", "BL67" },   // Ghallaron's Champion · BlackLegion
            { "BL_Ghallaron_s_Disciple", "BL66" },   // Ghallaron's Disciple · BlackLegion
            { "BL_Ghallaron_the_Pious", "BL65" },   // Ghallaron the Pious · BlackLegion
            { "BL_Litany_of_Despair", "9176318eaf469354e858f51af2333aa3" },   // Litany of Despair · BlackLegion
            { "BL_Meltagun_Legionary", "BL69" },   // Meltagun Legionary · BlackLegion
            { "BL_Zealot_Legionary", "BL68" },   // Zealot Legionary · BlackLegion
            { "DA1", "DA3" },   // Asmodai · DarkAngels
            { "DA10", "DA8" },   // Reiver · DarkAngels
            { "DA11", "DA6" },   // Sergeant Taaman · DarkAngels  ← 改名：Warpforge_11_Sergeant-Taaman.png（旧 id DA11 = PnP #11）
            { "DA12", "DA9" },   // Aggressor · DarkAngels
            { "DA13", "DA10" },   // Black Knight · DarkAngels
            { "DA14", "DA11" },   // Chaplain · DarkAngels
            { "DA15", "DA12" },   // Company Veteran · DarkAngels
            { "DA16", "DA13" },   // Librarian · DarkAngels
            { "DA17", "DA15" },   // Ravenwing Champion · DarkAngels
            { "DA18", "DA14" },   // Ravenwing Speeder · DarkAngels
            { "DA19", "DA16" },   // Captain · DarkAngels
            { "DA2", "4cdc1b5f965f99945b685bca7660e9fa" },   // Master of Repentance · DarkAngels
            { "DA20", "DA17" },   // Deathwing Terminator · DarkAngels
            { "DA21", "DA18" },   // Inner Circle Companion · DarkAngels
            { "DA22", "DA19" },   // Interrogator Chaplain · DarkAngels
            { "DA23", "DA21" },   // Land Speeder Vengeance · DarkAngels
            { "DA24", "DA20" },   // Master Lazarus · DarkAngels
            { "DA25", "DA22" },   // Techmarine · DarkAngels
            { "DA26", "DA23" },   // Bladeguard Veteran · DarkAngels
            { "DA27", "DA24" },   // Company Champion · DarkAngels
            { "DA28", "DA25" },   // Deathwing Knight · DarkAngels
            { "DA29", "DA26" },   // Ravenwing Ancient · DarkAngels
            { "DA3", "DA1" },   // Azrael · DarkAngels
            { "DA30", "DA27" },   // Ravenwing Talonmaster · DarkAngels
            { "DA31", "DA28" },   // Company Master · DarkAngels
            { "DA32", "DA29" },   // Darkshroud · DarkAngels
            { "DA33", "DA30" },   // Deathwing Strikemaster · DarkAngels
            { "DA34", "DA31" },   // Ezekiel · DarkAngels
            { "DA35", "a84a5b49f0ca9144585058db6987c2a0" },   // Master Interromancer · DarkAngels
            { "DA36", "DA32" },   // Deathwing Champion · DarkAngels
            { "DA37", "DA33" },   // Nephilim Jetfighter · DarkAngels
            { "DA38", "DA34" },   // Unforgiven Redemptor · DarkAngels
            { "DA39", "DA35" },   // Dark Talon · DarkAngels
            { "DA4", "dd535f7a54bcf6241b24ca6d0e81bbb4" },   // Supreme Grand Master · DarkAngels
            { "DA40", "DA36" },   // Sammael · DarkAngels
            { "DA41", "7c45a641ec845994097d185af58715f6" },   // Master of Manoeuvre · DarkAngels
            { "DA42", "DA37" },   // Deathwing Knight Master · DarkAngels
            { "DA43", "DA38" },   // Repulsor · DarkAngels
            { "DA44", "DA39" },   // Ancient Reliquary · DarkAngels
            { "DA45", "DA40" },   // Inner Circle · DarkAngels
            { "DA46", "DA41" },   // Martial Superiority · DarkAngels
            { "DA47", "DA42" },   // Reconnaissance Mission · DarkAngels
            { "DA48", "DA43" },   // Grim Resolve · DarkAngels
            { "DA49", "DA44" },   // Secret Agenda · DarkAngels
            { "DA5", "DA2" },   // Belial · DarkAngels
            { "DA50", "DA45" },   // Stubborn Defiance · DarkAngels
            { "DA51", "DA46" },   // The Rock · DarkAngels
            { "DA52", "DA47" },   // Blacksword Missiles · DarkAngels
            { "DA53", "DA48" },   // Deathwing Assault · DarkAngels
            { "DA54", "DA49" },   // Hunt the Fallen · DarkAngels
            { "DA55", "DA50" },   // Sacred Standard · DarkAngels
            { "DA56", "DA51" },   // Covert Operation · DarkAngels
            { "DA57", "DA52" },   // March of Vengeance · DarkAngels
            { "DA58", "DA53" },   // Steadfast Warriors · DarkAngels
            { "DA59", "DA54" },   // Unwavering Zeal · DarkAngels
            { "DA6", "9e0eecf8cb9a8df49bf18cc5d9051109" },   // Master of the Deathwing · DarkAngels
            { "DA60", "DA55" },   // Wages of Retribution · DarkAngels
            { "DA61", "DA56" },   // Desperate Mission · DarkAngels
            { "DA62", "DA57" },   // Deadly Hunters · DarkAngels
            { "DA63", "DA58" },   // Fury of the Unforgiven · DarkAngels
            { "DA64", "DA60" },   // Defensive Turrets · DarkAngels
            { "DA65", "DA59" },   // Grim Effigy · DarkAngels
            { "DA66", "DA61" },   // Plasma Generator · DarkAngels
            { "DA7", "DA4" },   // Intercessor · DarkAngels
            { "DA76", "38871853fabb5244d8148766829230cd" },   // Refuse to Yield · DarkAngels
            { "DA77", "DA71" },   // Watcher in the Dark · DarkAngels
            { "DA78", "DA70" },   // Vengeful Brethren Intercessor · DarkAngels
            { "DA79", "DA69" },   // Vengeful Brethren Hellblaster · DarkAngels
            { "DA8", "DA5" },   // Apothecary · DarkAngels
            { "DA80", "DA68" },   // Vengeful Brethren Bladeguard · DarkAngels
            { "DA81", "DA67" },   // Assault Terminator · DarkAngels
            { "DA82", "DA65" },   // Chaplain Gabutheron · DarkAngels
            { "DA83", "DA66" },   // Ravenwing Ballistus Dreadnought · DarkAngels
            { "DA84", "DA72" },   // Vengeful Brethren · DarkAngels
            { "DA85", "DA73" },   // Relic Munitions · DarkAngels
            { "DA86", "DA74" },   // Hunters of Heretics · DarkAngels
            { "DA9", "DA7" },   // Ravenwing Bikes · DarkAngels
            { "DA_Convoke_the_Circle", "3e99603a7b7b2c54c8f1ad40250ee2ae" },   // Convoke the Circle · DarkAngels
            { "DA_None_Must_Know", "24fcaf45556573549b922c06cad3aa2c" },   // None Must Know · DarkAngels
            { "DA_Obscure_Ritual", "b3364cdc54882b04e8a931e86700ac23" },   // Obscure Ritual · DarkAngels
            { "DA_Rites_of_Penance", "e01e8e933ad574b44bce5473ae486667" },   // Rites of Penance · DarkAngels
            { "DA_Smothering_Decree", "708c0306e73fd494eb0f718ddaeea48a" },   // Smothering Decree · DarkAngels
            { "EC1", "EC56" },   // Lord Kaphrael · EmperorsChildren
            { "EC10", "EC8" },   // Infractor · EmperorsChildren
            { "EC11", "EC17" },   // Konstrictus Tormentor · EmperorsChildren
            { "EC12", "EC5" },   // Tormentor · EmperorsChildren
            { "EC13", "EC11" },   // Flawless Blade · EmperorsChildren
            { "EC14", "EC9" },   // Icon of Excess Infractor · EmperorsChildren
            { "EC15", "EC6" },   // Plasma Gun Tormentor · EmperorsChildren
            { "EC16", "EC24" },   // Slaanesh's Spawn · EmperorsChildren
            { "EC17", "EC1" },   // Sonic Blaster Noise Marine · EmperorsChildren
            { "EC18", "EC19" },   // Threnodic Choir Noise Marine · EmperorsChildren
            { "EC19", "EC2" },   // Blastmaster Noise Marine · EmperorsChildren
            { "EC2", "b9665eadf1207ff49bcf0a730b0b7f9d" },   // Euphoric Strike (Lord Exultant's Talent) · EmperorsChildren
            { "EC20", "EC34" },   // Fiend · EmperorsChildren
            { "EC21", "EC10" },   // Infractor Obsessionist · EmperorsChildren
            { "EC22", "EC25" },   // Terminator · EmperorsChildren
            { "EC23", "EC7" },   // Tormentor Obsessionist · EmperorsChildren
            { "EC24", "EC22" },   // Veldras the Sublime · EmperorsChildren
            { "EC25", "EC4" },   // Veteran Noise Marine · EmperorsChildren
            { "EC26", "EC35" },   // Blissbringer · EmperorsChildren
            { "EC27", "EC3" },   // Disharmonist · EmperorsChildren
            { "EC28", "EC12" },   // Flawless Blade Champion · EmperorsChildren
            { "EC29", "EC21" },   // Malgarash the Adamant · EmperorsChildren
            { "EC3", "EC55" },   // Lucius The Eternal · EmperorsChildren
            { "EC30", "EC23" },   // Slaanesh Sorcerer · EmperorsChildren
            { "EC32", "EC16" },   // Dual Screamer Kakophonist · EmperorsChildren
            { "EC33", "EC26" },   // Terminator Champion · EmperorsChildren
            { "EC34", "EC20" },   // Turyan Ghauze · EmperorsChildren
            { "EC35", "EC13" },   // Lord Exultant · EmperorsChildren
            { "EC36", "EC27" },   // Rugged Disharmonist · EmperorsChildren
            { "EC37", "EC14" },   // Lord Kakophonist · EmperorsChildren
            { "EC38", "EC29" },   // Heldrake · EmperorsChildren
            { "EC39", "EC30" },   // Maulerfiend · EmperorsChildren
            { "EC4", "6ed51d59b9f8ea24ab0dc7ceaef7956c" },   // Duelist's Hubris (Lucius' Talent) · EmperorsChildren
            { "EC40", "EC15" },   // Winged Daemon Prince · EmperorsChildren
            { "EC41", "EC28" },   // Chaos Land Raider · EmperorsChildren
            { "EC42", "EC36" },   // Chosen of Slaanesh · EmperorsChildren
            { "EC43", "EC54" },   // Dark Apparitions · EmperorsChildren
            { "EC44", "EC43" },   // Plains of Excess · EmperorsChildren
            { "EC45", "EC37" },   // Thrill Seekers · EmperorsChildren
            { "EC46", "EC40" },   // Embrace the Pain · EmperorsChildren
            { "EC47", "EC39" },   // Eternal Servitude · EmperorsChildren
            { "EC48", "EC44" },   // Quicksilver Grace · EmperorsChildren
            { "EC49", "EC50" },   // Rapid Evisceration · EmperorsChildren
            { "EC5", "EC57" },   // Xarahan Daemon Prince · EmperorsChildren
            { "EC50", "EC41" },   // Internal Rivalry · EmperorsChildren
            { "EC51", "EC45" },   // Mechanised Murder · EmperorsChildren
            { "EC52", "EC38" },   // Pledge to the Dark Prince · EmperorsChildren
            { "EC53", "EC49" },   // Terrifying Crescendo · EmperorsChildren
            { "EC54", "EC51" },   // Coterie of the Conceited · EmperorsChildren
            { "EC55", "EC52" },   // Mercurial Host · EmperorsChildren
            { "EC56", "EC46" },   // Peerless Bladesmen · EmperorsChildren
            { "EC57", "EC58" },   // Vengeful Surge · EmperorsChildren
            { "EC58", "EC47" },   // Exquisite Swordsmanship · EmperorsChildren
            { "EC59", "EC48" },   // Carnival of Excess · EmperorsChildren
            { "EC6", "2621d4280735baf4c952e6afea8d0686" },   // Excessive Vigour (Daemon Prince's Talent) · EmperorsChildren
            { "EC60", "EC53" },   // Rapturous Ruination · EmperorsChildren
            { "EC61", "EC42" },   // Beautiful Death · EmperorsChildren
            { "EC62", "EC60" },   // Armoury of Excess · EmperorsChildren
            { "EC63", "EC61" },   // Decadent Throne · EmperorsChildren
            { "EC64", "EC59" },   // Vial Tanks · EmperorsChildren
            { "EC7", "EC32" },   // Daemonette · EmperorsChildren
            { "EC72", "5a8a11b2a89796f48b34fbec8db6f452" },   // Antrak Silk · EmperorsChildren
            { "EC73", "70199b6f183ec364082ca16e62f3d0cb" },   // Heliotrophos · EmperorsChildren
            { "EC74", "6e6d14006d8e9514b83a6c99e572081c" },   // Quail · EmperorsChildren
            { "EC75", "9215799995ff7a941b14737e27021032" },   // Shivversplint · EmperorsChildren
            { "EC76", "4eec3b13d157dae48b14ed412169e692" },   // Skorflense · EmperorsChildren
            { "EC77", "aa45079b714e3ff498549e533c5954a1" },   // Xylocil · EmperorsChildren
            { "EC8", "EC33" },   // Alluress · EmperorsChildren
            { "EC9", "EC18" },   // Glittering Myriad Infractor · EmperorsChildren
            { "GOF1", "GOF29" },   // Grukk Face-Rippa · Goff
            { "GOF10", "GOF5" },   // Trukk Boy · Goff
            { "GOF100", "GOF101" },   // Da Old Ways · Goff
            { "GOF101", "GOF103" },   // Da Hunt is On · Goff
            { "GOF102", "GOF104" },   // Drag it Down · Goff
            { "GOF103", "GOF106" },   // Power of the Waaagh! · Goff
            { "GOF104", "GOF102" },   // Thundering Stampede · Goff
            { "GOF106", "GOF107" },   // Fasta Than Yooz · Goff
            { "GOF11", "GOF10" },   // Grot Tank · Goff
            { "GOF12", "GOF8" },   // Hornhelmz Boy · Goff
            { "GOF13", "GOF9" },   // Shoota Boy · Goff
            { "GOF14", "GOF12" },   // Ork Spanner · Goff
            { "GOF15", "GOF7" },   // Warbiker · Goff
            { "GOF16", "GOF11" },   // Banner Nob · Goff
            { "GOF17", "GOF15" },   // Burna Boy · Goff
            { "GOF18", "GOF13" },   // Deffgun Loota · Goff
            { "GOF19", "GOF56" },   // Deffkopta · Goff
            { "GOF2", "GOF27" },   // Boss Zagstruk · Goff
            { "GOF20", "GOF57" },   // Ork Nob · Goff
            { "GOF21", "GOF14" },   // Skarboy Nob · Goff
            { "GOF22", "GOF16" },   // Big Krumpaz · Goff
            { "GOF23", "GOF58" },   // Boomdakka Snazzwagon · Goff
            { "GOF24", "GOF17" },   // Killa Kan · Goff
            { "GOF25", "GOF21" },   // Mekboy Gazmek · Goff
            { "GOF26", "GOF18" },   // PainBoy Sniklaw · Goff
            { "GOF27", "GOF59" },   // Big Shoota Boy · Goff
            { "GOF28", "GOF19" },   // Meganob Ugrak · Goff
            { "GOF29", "GOF61" },   // Rukkatrukk Squigbuggy · Goff
            { "GOF3", "GOF28" },   // Ghazghkull Thraka · Goff
            { "GOF30", "GOF60" },   // Tankbusta · Goff
            { "GOF31", "GOF20" },   // Veteran Flyboy · Goff
            { "GOF32", "GOF23" },   // Ardshell Gurk · Goff
            { "GOF33", "GOF22" },   // Deff Dread · Goff
            { "GOF34", "GOF62" },   // Kommando · Goff
            { "GOF35", "GOF24" },   // Battlewagon · Goff
            { "GOF36", "GOF63" },   // Gunwagon · Goff
            { "GOF38", "GOF25" },   // Gorkanaut · Goff
            { "GOF39", "GOF26" },   // Stompa · Goff
            { "GOF4", "GOF1" },   // Grot · Goff
            { "GOF40", "GOF30" },   // Attack Squig · Goff
            { "GOF41", "GOF32" },   // Grot Bomb · Goff
            { "GOF42", "GOF33" },   // Ard As Nails · Goff
            { "GOF43", "GOF31" },   // Cloud Of Smoke · Goff
            { "GOF44", "GOF35" },   // Ork Encampment · Goff
            { "GOF45", "GOF34" },   // Prophet of the Waaagh · Goff
            { "GOF46", "GOF36" },   // Scrag ’Em · Goff
            { "GOF47", "GOF43" },   // Gretchin Mob · Goff
            { "GOF48", "GOF38" },   // Proper Killy · Goff
            { "GOF49", "GOF64" },   // Pyromaniaks · Goff
            { "GOF5", "GOF2" },   // Makari the Grot · Goff
            { "GOF50", "GOF39" },   // Tide of Muscle · Goff
            { "GOF51", "GOF37" },   // Da Green Horde · Goff
            { "GOF52", "GOF42" },   // Da Irongob · Goff
            { "GOF53", "GOF45" },   // Get'em ladz! · Goff
            { "GOF54", "GOF40" },   // No Mukkin About · Goff
            { "GOF55", "GOF44" },   // Skorcha Assault · Goff
            { "GOF56", "GOF41" },   // Unbridled Carnage · Goff
            { "GOF57", "GOF46" },   // Stormboyz Strike · Goff
            { "GOF58", "GOF47" },   // Will of Gork · Goff
            { "GOF59", "GOF48" },   // Rok Invasion · Goff
            { "GOF6", "GOF3" },   // Bomb Squig · Goff
            { "GOF60", "GOF53" },   // Extractor Rig · Goff
            { "GOF61", "GOF49" },   // Gretchin Tower · Goff
            { "GOF62", "GOF52" },   // Toxic Bonfire · Goff
            { "GOF7", "GOF55" },   // Ork Boy · Goff
            { "GOF75", "GOF77" },   // Beastboss Morgrim · Goff
            { "GOF77", "GOF78" },   // Snakebite Grot · Goff
            { "GOF78", "GOF79" },   // Beast Snagga Boy · Goff
            { "GOF79", "GOF80" },   // Grot Orderly · Goff
            { "GOF8", "GOF4" },   // Slugga Boy · Goff
            { "GOF80", "GOF81" },   // Thump Gun Beast Snagga · Goff
            { "GOF81", "GOF82" },   // Beast Snagga Nob · Goff
            { "GOF82", "GOF83" },   // Grot on Bombsquig · Goff
            { "GOF83", "GOF84" },   // Runtherd · Goff
            { "GOF84", "GOF85" },   // Snakebite Nob · Goff
            { "GOF85", "GOF86" },   // Unleashed TramplaSquig · Goff
            { "GOF86", "GOF87" },   // Zodgrod Wortsnagga · Goff
            { "GOF87", "GOF88" },   // Nob on Smasha Squig · Goff
            { "GOF88", "GOF89" },   // Squighog Boyz · Goff
            { "GOF89", "GOF90" },   // Wurrboy · Goff
            { "GOF9", "GOF6" },   // Stormboy · Goff
            { "GOF90", "GOF91" },   // Beastboss on Squigosaur · Goff
            { "GOF91", "GOF92" },   // Weirdboy · Goff
            { "GOF92", "GOF93" },   // Hunta Rig · Goff
            { "GOF93", "GOF94" },   // Painboss · Goff
            { "GOF94", "GOF95" },   // Kill Rig · Goff
            { "GOF95", "GOF96" },   // Snakebite Battlewagon · Goff
            { "GOF96", "GOF97" },   // Gargantuan Squiggoth · Goff
            { "GOF97", "GOF98" },   // Gnarled and Rugged · Goff
            { "GOF98", "GOF99" },   // Skrag Every Stash! · Goff
            { "GOF99", "GOF100" },   // Cyber‑augmentation · Goff
            { "GOF_Big_Choppa_Nob", "GOF68" },   // Big Choppa Nob · Goff
            { "GOF_Boss_Nob", "GOF67" },   // Boss Nob · Goff
            { "GOF_Da_Bigger_Dey_Iz_Mozrog_s_Talent", "64a657fc5ed5bbb4fa7b585cf29c1e95" },   // Da Bigger Dey Iz... (Mozrog's Talent) · Goff
            { "GOF_Da_Red_Waaagh", "20d0f4451f16fc441836a02f99bf77da" },   // Da Red Waaagh · Goff
            { "GOF_Dead_Choppy", "GOF72" },   // Dead Choppy · Goff
            { "GOF_Dok_s_Toolz_Painboss_talent", "8f35df0536e4f7e4d86f3885e2597404" },   // Dok’s Toolz (Painboss' talent) · Goff
            { "GOF_Ferocious_Rage_Beastboss_Talent", "84d4d192e0f57a34a90ac0f6172c3358" },   // Ferocious Rage (Beastboss' Talent) · Goff
            { "GOF_Follower_of_Gork", "6ba4b132d24528844a86304f8ce67833" },   // Follower of Gork · Goff
            { "GOF_Greatest_Warboss", "3ebb159036778d14ebd1c48451aa6ca0" },   // Greatest Warboss · Goff
            { "GOF_Grizzled_Skarboy", "GOF74" },   // Grizzled Skarboy · Goff
            { "GOF_Krump_da_Gitz", "GOF71" },   // Krump da Gitz · Goff
            { "GOF_Krumpaklaw", "GOF70" },   // Krumpaklaw · Goff
            { "GOF_Mega_Blasta_Deffkopta", "GOF69" },   // Mega Blasta Deffkopta · Goff
            { "GOF_Mekaniak", "9dcbe3d0c04b02f4fbb19974ae0e37b2" },   // Mekaniak · Goff
            { "GOF_More_Dakka", "9f4e1b7ea9f00da458c6a859a4e958c8" },   // More Dakka · Goff
            { "GOF_Ramshackle", "98d3c65702746b04baf7f5ead1a9e507" },   // Ramshackle · Goff
            { "GOF_Rokkit_Boy", "GOF66" },   // Rokkit Boy · Goff
            { "GOF_Sawbonez", "b4d6d186eb95a3f468013c5be649ee3b" },   // Sawbonez · Goff
            { "GOF_Special_Dose_Zodgrod_Wortsnagga_Talent", "d19e8b68aaa9a884781d06697ad237a6" },   // Special Dose (Zodgrod Wortsnagga Talent) · Goff
            { "GOF_Stikkbomb_Boy", "GOF65" },   // Stikkbomb Boy · Goff
            { "GOF_Stomp_Em", "GOF73" },   // Stomp Em · Goff
            { "GOF_Uge_Choppa", "6899b199f5e36144ebbdd7ed9d9e7afe" },   // Uge Choppa · Goff
            { "GOF_Waaagh_Energy_Weirdboy_Talent", "e3d67d86f73f7564eb0bf886ec848af6" },   // Waaagh! Energy (Weirdboy Talent) · Goff
            { "GOF_Warboss_Gordrang", "GOF75" },   // Warboss Gordrang · Goff
            { "GOF_Worst_Temper", "8425b809e9e64e24f9035f709f5aaee4" },   // Worst Temper · Goff
            { "GOF_Wreckin_Ball", "508002f5d7ee54f4686aefa2e6adce41" },   // Wreckin Ball · Goff
            { "GSC1", "GSC3" },   // Acolyte Iconward · Genestealers
            { "GSC10", "GSC7" },   // Neophyte Hybrid · Genestealers
            { "GSC11", "GSC13" },   // Neophyte Specialist · Genestealers
            { "GSC12", "GSC8" },   // Neophyte Trooper · Genestealers
            { "GSC13", "GSC9" },   // Acolyte Hybrid · Genestealers
            { "GSC14", "GSC10" },   // Biophagus · Genestealers
            { "GSC15", "a7bb23355a30d344e9d2decf976d91e1" },   // Poisoned Supplies · Genestealers
            { "GSC16", "4959cd9e99597f74ca9af0bd65ae6307" },   // Experimental Bio-Horrors · Genestealers
            { "GSC18", "GSC11" },   // Neophyte Heavy · Genestealers
            { "GSC19", "GSC12" },   // Neophyte Iconbearer · Genestealers
            { "GSC2", "7831725387c8a774dbdf5b6492d928e0" },   // Nexus of Devotion · Genestealers
            { "GSC20", "GSC18" },   // Neophyte Leader · Genestealers
            { "GSC21", "GSC16" },   // Acolyte Heavy · Genestealers
            { "GSC22", "GSC14" },   // Acolyte Leader · Genestealers
            { "GSC23", "GSC15" },   // Acolyte Specialist · Genestealers
            { "GSC24", "GSC23" },   // Hybrid Metamorph · Genestealers
            { "GSC25", "GSC24" },   // Nexos · Genestealers
            { "GSC26", "GSC19" },   // Purestrain Genestealer · Genestealers
            { "GSC27", "GSC20" },   // Aberrant · Genestealers
            { "GSC28", "GSC21" },   // Clamavus · Genestealers
            { "GSC29", "e9952f50921d2844a94d39ce883430f4" },   // Vox-Hacker · Genestealers
            { "GSC3", "GSC2" },   // Magus Uthrel Naas · Genestealers
            { "GSC30", "GSC22" },   // Cult Sentinel · Genestealers
            { "GSC31", "GSC29" },   // Metamorph Iconbearer · Genestealers
            { "GSC32", "GSC25" },   // Sanctus · Genestealers
            { "GSC33", "GSC26" },   // Aberrant Hypermorph · Genestealers
            { "GSC34", "GSC27" },   // Achilles Ridgerunner · Genestealers
            { "GSC35", "GSC28" },   // Hulking Aberrant · Genestealers
            { "GSC36", "GSC30" },   // Metamorph Leader · Genestealers
            { "GSC37", "GSC31" },   // Reductus Saboteur · Genestealers
            { "GSC38", "GSC32" },   // Abominant · Genestealers
            { "GSC39", "GSC35" },   // Benefictus · Genestealers
            { "GSC4", "fd67b47572987b84cb2990ea1ecfe982" },   // Brood Prophet · Genestealers
            { "GSC40", "GSC33" },   // Goliath Truck · Genestealers
            { "GSC41", "GSC36" },   // Goliath Rockgrinder · Genestealers
            { "GSC42", "GSC34" },   // Kelermorph · Genestealers
            { "GSC43", "GSC37" },   // Patriarch · Genestealers
            { "GSC44", "f45dd53721b645b4ab45d334edb986af" },   // Deadliest Killer · Genestealers
            { "GSC45", "GSC38" },   // Cult Leman Russ · Genestealers
            { "GSC46", "GSC55" },   // Eve of the Uprising · Genestealers
            { "GSC47", "GSC57" },   // Perfect Ambush · Genestealers
            { "GSC48", "GSC40" },   // Backstab · Genestealers
            { "GSC49", "GSC50" },   // Clandestine Operation · Genestealers
            { "GSC5", "GSC1" },   // Primus Saffa Rhiannor · Genestealers
            { "GSC50", "GSC53" },   // Enhanced Aggression · Genestealers
            { "GSC51", "GSC42" },   // Enhanced Musculature · Genestealers
            { "GSC52", "GSC43" },   // Enhanced Resilience · Genestealers
            { "GSC53", "GSC39" },   // Lying in Wait · Genestealers
            { "GSC54", "GSC46" },   // Bore Through · Genestealers
            { "GSC55", "GSC58" },   // Gestalt Energy · Genestealers
            { "GSC56", "GSC51" },   // Living Icon · Genestealers
            { "GSC57", "GSC47" },   // Perfect Hosts · Genestealers
            { "GSC58", "GSC41" },   // Open Insurrection · Genestealers
            { "GSC59", "GSC52" },   // Summon the Cult · Genestealers
            { "GSC6", "d01a5ba5865ded4418194ea50e333b21" },   // Inscrutable Cunning · Genestealers
            { "GSC60", "GSC45" },   // Underground Network · Genestealers
            { "GSC61", "GSC56" },   // A Trap Sprung · Genestealers
            { "GSC62", "GSC44" },   // Subterranean Ambush · Genestealers
            { "GSC63", "GSC54" },   // Rampant Infestation · Genestealers
            { "GSC64", "GSC49" },   // Telepathic Domination · Genestealers  ← 改名：Warpforge_64_Telepathic-Domination.png
            { "GSC65", "GSC59" },   // Day of Ascension · Genestealers
            { "GSC66", "GSC63" },   // Alien Idol · Genestealers
            { "GSC67", "GSC65" },   // Pilfered Supplies · Genestealers
            { "GSC68", "GSC64" },   // Rusted Vent · Genestealers  ← 改名：Warpforge_68_Rusted-Vent.png
            { "GSC69", "GSC68" },   // Atalan Jackal · Genestealers
            { "GSC7", "GSC4" },   // Genestealer Familiar · Genestealers
            { "GSC70", "GSC69" },   // Jackal Outrider · Genestealers
            { "GSC72", "GSC70" },   // Jackal Specialist · Genestealers
            { "GSC73", "GSC72" },   // Atalan Wolfquad · Genestealers
            { "GSC74", "GSC73" },   // Spotter Ridgerunner · Genestealers
            { "GSC75", "GSC74" },   // Rogue Informant · Genestealers
            { "GSC78", "GSC75" },   // Roaming Outriders · Genestealers
            { "GSC8", "GSC6" },   // Neophyte Initiate · Genestealers
            { "GSC9", "GSC5" },   // Concealed Explosives · Genestealers
            { "GSC_Cult_Propaganda", "7c666a04c1b9e8b4e9a96247e9fe115a" },   // Cult Propaganda · Genestealers
            { "GSC_Improvised_Barricade", "334a32d196d46ed4387bf3740cce069b" },   // Improvised Barricade · Genestealers
            { "GSC_Jackal_Scout", "GSC67" },   // Jackal Scout · Genestealers
            { "GSC_Jammed_Communications", "68909cf02f472734ea391665e0d28e15" },   // Jammed Communications · Genestealers
            { "GSC_Lhaska_Szenari", "GSC66" },   // Lhaska Szenari · Genestealers
            { "GSC_Master_Outrider", "c433e3dde4952cc448220908226127ae" },   // Master Outrider · Genestealers
            { "SAU1", "SAU35" },   // Imotekh the Stormlord · Sautekh
            { "SAU10", "SAU4" },   // Gauss Reaper Warrior · Sautekh
            { "SAU11", "SAU5" },   // Necron Warrior · Sautekh
            { "SAU12", "SAU6" },   // Tesla Carbine Immortal · Sautekh
            { "SAU13", "SAU7" },   // Canoptek Wraith · Sautekh
            { "SAU14", "SAU8" },   // Gauss Blaster Immortal · Sautekh
            { "SAU15", "SAU9" },   // Ophydian Destroyer · Sautekh
            { "SAU16", "SAU11" },   // Technomancer · Sautekh
            { "SAU17", "SAU12" },   // Tomb Blade · Sautekh
            { "SAU18", "SAU10" },   // Triarch Praetorian · Sautekh
            { "SAU19", "SAU13" },   // Canoptek Spyder · Sautekh
            { "SAU2", "30007162b68992e41a422f2061705b65" },   // Lord of the Storm · Sautekh
            { "SAU20", "SAU14" },   // Cryptothrall · Sautekh
            { "SAU21", "SAU15" },   // Deathmark · Sautekh
            { "SAU22", "SAU16" },   // Night Scythe · Sautekh
            { "SAU23", "SAU17" },   // Psychomancer · Sautekh
            { "SAU24", "SAU18" },   // Skorpekh Destroyer · Sautekh
            { "SAU25", "SAU19" },   // Canoptek Reanimator · Sautekh
            { "SAU26", "27a4b748a7bf358499f038aa09895ead" },   // Reanimate · Sautekh
            { "SAU27", "SAU20" },   // Chronomancer · Sautekh
            { "SAU28", "SAU21" },   // Doom Scythe · Sautekh
            { "SAU29", "SAU22" },   // Lokhust Heavy Destroyer · Sautekh
            { "SAU3", "SAU36" },   // Nemesor Zahndrekh · Sautekh
            { "SAU31", "SAU24" },   // Command Barge · Sautekh
            { "SAU32", "SAU25" },   // Hexmark Destroyer · Sautekh
            { "SAU33", "SAU26" },   // Plasmancer · Sautekh
            { "SAU34", "SAU65" },   // Royal Warden · Sautekh
            { "SAU35", "SAU27" },   // Scarab Swarm · Sautekh
            { "SAU36", "SAU28" },   // Ghost Ark · Sautekh
            { "SAU37", "SAU29" },   // Necron Lord · Sautekh
            { "SAU38", "65b2be828179515458a282cf1b027182" },   // Indomitable Will · Sautekh
            { "SAU39", "SAU30" },   // Triarch Stalker · Sautekh
            { "SAU4", "d8a14171b08b54144ba9dbf324b0a042" },   // Relentless March · Sautekh
            { "SAU40", "SAU31" },   // Doomstalker · Sautekh
            { "SAU41", "SAU32" },   // Skorpekh Lord · Sautekh
            { "SAU42", "SAU33" },   // Vargard Obyron · Sautekh
            { "SAU43", "SAU34" },   // Monolith · Sautekh
            { "SAU44", "eb17cd22c6a5d7941a7456838d927ab1" },   // Particle Whip · Sautekh
            { "SAU45", "SAU38" },   // Extermination Protocol · Sautekh  ← 改名：Warpforge_45_Extermination-Protocol.png
            { "SAU46", "SAU41" },   // Reanimation Protocol · Sautekh
            { "SAU47", "SAU39" },   // Reconstitution Protocol · Sautekh  ← 改名：Warpforge_47_Reconstitution-Protocol.png
            { "SAU48", "SAU45" },   // Hyperlogical Strategist · Sautekh
            { "SAU49", "SAU40" },   // Protection Protocol · Sautekh
            { "SAU5", "SAU37" },   // Orikan the Diviner · Sautekh
            { "SAU50", "SAU42" },   // Relentless Advance · Sautekh
            { "SAU51", "SAU44" },   // Disruption Blades · Sautekh
            { "SAU52", "SAU49" },   // Resurrection Orb · Sautekh
            { "SAU53", "SAU46" },   // Spyder Nest · Sautekh
            { "SAU54", "SAU47" },   // Curse of the Phaeron · Sautekh
            { "SAU55", "SAU43" },   // Solar Pulse · Sautekh
            { "SAU56", "SAU50" },   // Dimensional Breach · Sautekh
            { "SAU57", "SAU55" },   // Immortal Pride · Sautekh
            { "SAU59", "SAU51" },   // Scythe Assault · Sautekh
            { "SAU6", "cbabeb234380634479feab1a1b4c733a" },   // Master Chronomancer · Sautekh
            { "SAU60", "SAU52" },   // Self-Destruction · Sautekh
            { "SAU61", "SAU53" },   // Undying Legions · Sautekh
            { "SAU62", "SAU54" },   // Conquering Tyrant · Sautekh
            { "SAU63", "SAU56" },   // Methodical Destruction · Sautekh
            { "SAU64", "SAU57" },   // Tomb World · Sautekh
            { "SAU65", "SAU59" },   // Awakened Obelisk · Sautekh
            { "SAU66", "SAU60" },   // Divination Menhir · Sautekh
            { "SAU67", "SAU61" },   // Resurrection Vault · Sautekh
            { "SAU68", "SAU67" },   // Damaged Plasmacyte · Sautekh
            { "SAU7", "SAU1" },   // Canoptek Scarab · Sautekh
            { "SAU70", "SAU68" },   // Lokhust Destroyer · Sautekh
            { "SAU71", "SAU70" },   // Damaged Hexmark · Sautekh
            { "SAU72", "SAU71" },   // Immortals Phalanx · Sautekh
            { "SAU73", "SAU72" },   // Lokhust Lord · Sautekh
            { "SAU74", "SAU73" },   // Annihilation Barge · Sautekh
            { "SAU75", "SAU74" },   // Dimensional Corridor · Sautekh
            { "SAU76", "SAU75" },   // Awakened Dynasty · Sautekh
            { "SAU77", "SAU76" },   // Inescapable Retribution · Sautekh
            { "SAU8", "SAU2" },   // Canoptek Plasmacyte · Sautekh
            { "SAU9", "SAU3" },   // Flayed One · Sautekh
            { "SAU_Hardwired_Destruction", "461354d98aa5e3142afa4eb4922e5456" },   // Hardwired Destruction · Sautekh
            { "SAU_Lychguard", "SAU23" },   // Lychguard · Sautekh
            { "SAU_Ramatekh_The_Cruel", "SAU66" },   // Ramatekh The Cruel · Sautekh
            { "SOR10", "SOR6" },   // Battle Sister Gunner · Sororitas
            { "SOR11", "SOR7" },   // Crusader · Sororitas
            { "SOR12", "SOR9" },   // Preacher · Sororitas
            { "SOR13", "4d348cff71208c24d8e39252d913ff9d" },   // Hymn of Battle · Sororitas
            { "SOR16", "SOR21" },   // Death Cult Assassin · Sororitas
            { "SOR17", "SOR11" },   // Dominion · Sororitas
            { "SOR18", "SOR13" },   // Simulacrum Bearer · Sororitas
            { "SOR19", "SOR10" },   // Sister Repentia · Sororitas  ← 改名：Warpforge_19_Sister-Repentia.png
            { "SOR2", "8901e35c5de30eb48804da6651cfb132" },   // Fury of the Righteous · Sororitas
            { "SOR20", "SOR18" },   // Dialogus · Sororitas
            { "SOR21", "SOR20" },   // Hospitaller · Sororitas
            { "SOR22", "SOR16" },   // Missionary · Sororitas
            { "SOR23", "SOR12" },   // Retributor · Sororitas
            { "SOR24", "SOR19" },   // Seraphim · Sororitas
            { "SOR25", "SOR17" },   // Sister Superior · Sororitas
            { "SOR27", "SOR23" },   // Canoness · Sororitas
            { "SOR28", "SOR22" },   // Dominion Superior · Sororitas
            { "SOR29", "SOR25" },   // Imagifier · Sororitas
            { "SOR3", "SOR2" },   // Junith Eruita · Sororitas
            { "SOR30", "SOR24" },   // Simulacrum Imperialis · Sororitas
            { "SOR31", "SOR30" },   // Celestian Sacresant · Sororitas
            { "SOR32", "SOR31" },   // Sister Dogmata · Sororitas
            { "SOR33", "SOR29" },   // Ephrael Stern · Sororitas
            { "SOR34", "SOR27" },   // Penitent Engine · Sororitas
            { "SOR35", "SOR28" },   // Zephyrim · Sororitas
            { "SOR36", "SOR34" },   // Celestian Superior · Sororitas
            { "SOR37", "SOR32" },   // Mortifier · Sororitas
            { "SOR38", "SOR33" },   // Saint Celestine · Sororitas
            { "SOR39", "SOR35" },   // Immolator · Sororitas
            { "SOR4", "65cfec7d2bc719040b3b4b88622931b2" },   // Lead the Righteous · Sororitas
            { "SOR40", "SOR38" },   // Paragon Warsuit · Sororitas
            { "SOR42", "SOR36" },   // Exorcist · Sororitas
            { "SOR43", "SOR58" },   // Devout Warriors · Sororitas
            { "SOR44", "SOR56" },   // Beacon of Faith · Sororitas
            { "SOR45", "SOR46" },   // Furious Reprisal · Sororitas
            { "SOR46", "SOR40" },   // Inspiring Oratory · Sororitas
            { "SOR47", "SOR44" },   // Moment of Grace · Sororitas
            { "SOR48", "SOR39" },   // Prayer · Sororitas
            { "SOR49", "SOR55" },   // Trial of Suffering · Sororitas
            { "SOR5", "SOR3" },   // Morvenn Vahl · Sororitas
            { "SOR50", "SOR53" },   // Blade of Faith · Sororitas
            { "SOR51", "SOR41" },   // Conviction of Faith · Sororitas
            { "SOR52", "SOR47" },   // Miraculous Feat · Sororitas
            { "SOR53", "SOR42" },   // Rite of Restoration · Sororitas
            { "SOR54", "SOR43" },   // Cleansing Flames · Sororitas
            { "SOR55", "SOR54" },   // Fiery Conviction · Sororitas
            { "SOR56", "SOR57" },   // Hallowed Martyrs · Sororitas
            { "SOR57", "SOR52" },   // Spirit of the Martyr · Sororitas
            { "SOR58", "SOR51" },   // Divine Intervention · Sororitas
            { "SOR59", "SOR48" },   // Imperial Creed · Sororitas
            { "SOR6", "f104d4c21abe84546abc9365d9eda8dd" },   // Righteous Repugnance · Sororitas
            { "SOR61", "SOR45" },   // Defenders of the Faith · Sororitas
            { "SOR62", "SOR49" },   // Shrineworld · Sororitas
            { "SOR63", "SOR61" },   // Guidance of the Saints · Sororitas
            { "SOR64", "SOR60" },   // Holy Fire · Sororitas
            { "SOR65", "SOR59" },   // Sacred Altar · Sororitas
            { "SOR66", "e01f4d54e8a474d96b5a1167cc28eebf" },   // Daemonbreaker · Sororitas
            { "SOR67", "SOR66" },   // Armorium Cherub · Sororitas
            { "SOR68", "SOR67" },   // Sacred Rose Sister · Sororitas
            { "SOR69", "SOR68" },   // Ministorum Priest · Sororitas
            { "SOR7", "SOR4" },   // Sister Novitiate · Sororitas
            { "SOR71", "SOR69" },   // Zephyrim Superior · Sororitas
            { "SOR72", "SOR71" },   // Adelaide the Serene · Sororitas
            { "SOR73", "SOR72" },   // Castigator · Sororitas
            { "SOR74", "SOR73" },   // Sacred Rose · Sororitas
            { "SOR76", "SOR74" },   // Emperor's Judgement · Sororitas
            { "SOR9", "SOR5" },   // Battle Sister · Sororitas
            { "SOR_Celestian_Sacresant_Aveline", "SOR65" },   // Celestian Sacresant Aveline · Sororitas
            { "SOR_Relics_of_Saint_Katherine", "SOR50" },   // Relics of Saint Katherine · Sororitas
            { "SOR_Triumph_of_Saint_Katherine", "SOR37" },   // Triumph of Saint Katherine · Sororitas
            { "SW10", "e3b5735d801fcbd49aa8d3975a5b0c31" },   // War Howl · SpaceWolves
            { "SW11", "SW4" },   // Blood Claw · SpaceWolves
            { "SW12", "SW5" },   // Hunting Wolf · SpaceWolves
            { "SW13", "SW8" },   // Blackmane Reiver · SpaceWolves
            { "SW14", "SW6" },   // Blood Claw Pack Leader · SpaceWolves
            { "SW15", "SW7" },   // Deathwolf Scout · SpaceWolves
            { "SW16", "SW9" },   // Fenrisian Wolf · SpaceWolves
            { "SW17", "SW10" },   // Grey Hunter · SpaceWolves
            { "SW18", "SW15" },   // Fyrri Askar · SpaceWolves
            { "SW19", "SW11" },   // Grey Hunter Pack Leader · SpaceWolves
            { "SW2", "b3dce3f10cc118541910156ec8da1d9b" },   // High King of Fenris · SpaceWolves
            { "SW20", "SW12" },   // Thunderwolf Cavalry · SpaceWolves
            { "SW21", "SW13" },   // Wolf Guard Headtaker · SpaceWolves
            { "SW22", "SW14" },   // Wolf Priest · SpaceWolves
            { "SW23", "SW18" },   // Hrolf the Ironhowl · SpaceWolves
            { "SW24", "SW19" },   // Iron Priest · SpaceWolves
            { "SW25", "SW16" },   // Long Fang · SpaceWolves
            { "SW26", "SW20" },   // Thunderwolf Cavalry Pack Leader · SpaceWolves
            { "SW27", "SW17" },   // Wulfen · SpaceWolves
            { "SW28", "SW21" },   // Wulfen Pack Leader · SpaceWolves
            { "SW29", "SW26" },   // Arjac Rockfist · SpaceWolves
            { "SW3", "6e4f9040e8cc59641a32f29e33ea4cbc" },   // Tyrnak and Fenrir · SpaceWolves
            { "SW30", "SW22" },   // Blackmane Inceptor · SpaceWolves
            { "SW31", "SW23" },   // Fenrisian Drop Pod · SpaceWolves
            { "SW32", "SW24" },   // Frost Claw Wulfen · SpaceWolves
            { "SW33", "SW25" },   // Wolf Guard Battle Leader · SpaceWolves
            { "SW34", "SW27" },   // Wolf Guard Terminator Pack Leader · SpaceWolves
            { "SW35", "SW28" },   // Blackmane Aggressor · SpaceWolves
            { "SW36", "SW29" },   // Morkai Eliminator · SpaceWolves
            { "SW37", "SW30" },   // Terminator Rune Priest · SpaceWolves
            { "SW38", "SW31" },   // Wolf Guard Terminator · SpaceWolves
            { "SW39", "SW34" },   // Murderfang · SpaceWolves
            { "SW4", "SW2" },   // Njal Stormcaller · SpaceWolves
            { "SW40", "SW32" },   // Stormwolf · SpaceWolves
            { "SW41", "SW33" },   // Vindicator · SpaceWolves
            { "SW42", "SW36" },   // Bjorn the Fell-Handed · SpaceWolves
            { "SW43", "SW35" },   // Venerable Dreadnought · SpaceWolves
            { "SW44", "SW37" },   // Wulfen Dreadnought · SpaceWolves
            { "SW45", "SW38" },   // Land Rider · SpaceWolves
            { "SW46", "SW40" },   // Bjorn's Shrine · SpaceWolves
            { "SW47", "SW39" },   // Canis Helix · SpaceWolves
            { "SW48", "SW44" },   // Death Shun · SpaceWolves
            { "SW49", "SW41" },   // Pack of One · SpaceWolves
            { "SW5", "a51adc8f5c311584385c3117cfd919e9" },   // High Rune Priest · SpaceWolves
            { "SW50", "SW42" },   // Savage Legacy · SpaceWolves
            { "SW51", "SW43" },   // Anvil of Endurance · SpaceWolves
            { "SW52", "SW47" },   // Saga of the Bold · SpaceWolves
            { "SW53", "SW48" },   // The Fang · SpaceWolves
            { "SW54", "SW45" },   // Unbridled Fury · SpaceWolves
            { "SW55", "SW53" },   // Embers of Prospero · SpaceWolves
            { "SW56", "SW49" },   // Fenrisian Blizzard · SpaceWolves
            { "SW57", "SW51" },   // Hunter's Guile · SpaceWolves
            { "SW58", "SW52" },   // Proper Hunt · SpaceWolves
            { "SW59", "SW46" },   // Raid Tactics · SpaceWolves
            { "SW6", "b551c8351ebe5784faf9912b573eaf93" },   // Tempest's Wrath · SpaceWolves
            { "SW60", "SW54" },   // Fenrisian Monstrosities · SpaceWolves
            { "SW61", "SW50" },   // Hordeslayer · SpaceWolves
            { "SW62", "SW55" },   // Legendary Tenacity · SpaceWolves
            { "SW63", "SW56" },   // Let Loose · SpaceWolves
            { "SW64", "SW57" },   // Birth of a Saga · SpaceWolves
            { "SW65", "SW58" },   // Fenrisian Wolfpack · SpaceWolves
            { "SW66", "SW60" },   // Fenrisian Runestones · SpaceWolves
            { "SW67", "SW59" },   // Orbital Defences · SpaceWolves
            { "SW68", "SW61" },   // Halls of Legend · SpaceWolves
            { "SW7", "4d4b9170663891f488c0e96ee8e55ba1" },   // Murderous Hurricane · SpaceWolves
            { "SW8", "7a01882fe4610244292d16b15f4b873e" },   // Living Lightning · SpaceWolves
            { "SW9", "SW3" },   // Ragnar Blackmane · SpaceWolves
            { "TAU1", "TAU2" },   // Aun'Va · TauEmpire
            { "TAU10", "TAU6" },   // Gun Drone · TauEmpire
            { "TAU11", "TAU7" },   // Marker Drone · TauEmpire
            { "TAU12", "TAU8" },   // Missile Drone · TauEmpire
            { "TAU13", "TAU9" },   // Pathfinder · TauEmpire
            { "TAU14", "TAU10" },   // Stealth Drone · TauEmpire
            { "TAU15", "TAU11" },   // Crisis Battlesuit · TauEmpire
            { "TAU16", "TAU12" },   // Darkstrider · TauEmpire
            { "TAU17", "f279c5cf1c3e16e4db6b9df4d941ad11" },   // Structural Analyzer · TauEmpire
            { "TAU18", "TAU13" },   // DS8 Support Turret · TauEmpire
            { "TAU19", "TAU14" },   // Breacher Fire Warrior · TauEmpire
            { "TAU2", "f8260229254a09746af6d9ff7a9b0b54" },   // Ethereal Supreme · TauEmpire
            { "TAU20", "TAU15" },   // Sniper Drone · TauEmpire
            { "TAU21", "TAU16" },   // Vespid Stingwings · TauEmpire
            { "TAU22", "TAU17" },   // Crisis Bodyguard · TauEmpire
            { "TAU23", "TAU18" },   // Fire Warrior Marksman · TauEmpire
            { "TAU24", "TAU19" },   // Honoured Ethereal · TauEmpire
            { "TAU25", "TAU20" },   // Piranha · TauEmpire
            { "TAU26", "TAU21" },   // Stealth Battlesuit · TauEmpire
            { "TAU27", "1688c6146abb1e04f98e540b4cbbabd7" },   // Homing Beacon · TauEmpire
            { "TAU28", "TAU22" },   // Strike Team · TauEmpire
            { "TAU29", "TAU23" },   // Breacher ShasUi · TauEmpire
            { "TAU3", "f2b80d575cbf8f64597b991022beaf35" },   // Serene Unifier · TauEmpire
            { "TAU30", "TAU27" },   // Broadside Battlesuit · TauEmpire
            { "TAU31", "TAU24" },   // Devilfish · TauEmpire
            { "TAU32", "TAU25" },   // Firesight Marksman · TauEmpire
            { "TAU33", "TAU26" },   // Stealth Shas’ui · TauEmpire
            { "TAU34", "TAU32" },   // Aunshi Ethereal · TauEmpire
            { "TAU35", "TAU29" },   // Cadre Fireblade · TauEmpire
            { "TAU36", "TAU28" },   // Enforcer Battlesuit · TauEmpire
            { "TAU37", "TAU31" },   // Hammerhead · TauEmpire
            { "TAU38", "TAU34" },   // Coldstar Battlesuit · TauEmpire
            { "TAU39", "TAU33" },   // Razorshark · TauEmpire
            { "TAU4", "TAU3" },   // Commander O'Maisos · TauEmpire
            { "TAU40", "TAU30" },   // Sky Ray Gunship · TauEmpire
            { "TAU41", "TAU35" },   // Longstrike · TauEmpire
            { "TAU42", "TAU36" },   // Riptide Battlesuit · TauEmpire
            { "TAU43", "684196db974ab254090f39ef6e89868c" },   // Shield Generator · TauEmpire
            { "TAU44", "TAU37" },   // Sun Shark Bomber · TauEmpire
            { "TAU45", "TAU38" },   // Stormsurge Battlesuit · TauEmpire
            { "TAU46", "507f79fa60945d04e93fe806aca912b7" },   // Deploy Anchors · TauEmpire
            { "TAU47", "TAU39" },   // Kauyon Tactics · TauEmpire
            { "TAU48", "TAU40" },   // Technological Supremacy · TauEmpire
            { "TAU49", "TAU41" },   // Zephyr's Grace · TauEmpire
            { "TAU5", "c4f5c920744030c4e84428dc5a0f1bf2" },   // Drone Companion · TauEmpire
            { "TAU50", "TAU43" },   // For the Greater Good · TauEmpire
            { "TAU51", "TAU44" },   // Pulse Onslaught · TauEmpire
            { "TAU52", "TAU46" },   // Sense of Stone · TauEmpire
            { "TAU53", "TAU58" },   // Coordinated Engagement · TauEmpire
            { "TAU54", "TAU47" },   // Dynamic Offensive · TauEmpire
            { "TAU55", "TAU48" },   // Emergency Dispensation · TauEmpire
            { "TAU56", "TAU49" },   // Inspired to Greatness · TauEmpire
            { "TAU57", "TAU45" },   // Recon Sweep · TauEmpire
            { "TAU59", "TAU53" },   // Fortified Positions · TauEmpire
            { "TAU6", "TAU1" },   // Shadowsun · TauEmpire
            { "TAU60", "TAU50" },   // Priority Target · TauEmpire
            { "TAU61", "TAU54" },   // Razorshark Strike · TauEmpire
            { "TAU62", "TAU55" },   // Relentless Fusillade · TauEmpire
            { "TAU63", "TAU51" },   // Saviour Protocols · TauEmpire  ← 改名：Warpforge_63_Saviour-Protocols.png
            { "TAU64", "TAU42" },   // Experimental Drones · TauEmpire  ← 改名：Warpforge_64_Experimental-Drones.png
            { "TAU65", "TAU56" },   // Valued Sacrifice · TauEmpire
            { "TAU66", "TAU57" },   // Burgeoning Empire · TauEmpire
            { "TAU67", "TAU64" },   // Tidewall Droneport · TauEmpire
            { "TAU68", "TAU63" },   // Tidewall Gunrig · TauEmpire
            { "TAU69", "TAU62" },   // Watch Tower · TauEmpire
            { "TAU7", "9f75a6fe11922eb409ffbad4ecdeb405" },   // Hero of the Empire · TauEmpire
            { "TAU70", "TAU69" },   // Krootox Rider · TauEmpire
            { "TAU71", "TAU70" },   // Flesh Shaper · TauEmpire
            { "TAU72", "TAU71" },   // Krootox Rampager · TauEmpire
            { "TAU73", "TAU72" },   // Lone-spear · TauEmpire
            { "TAU74", "TAU73" },   // Hidden Hunters · TauEmpire
            { "TAU75", "TAU74" },   // Thundering Rampage · TauEmpire
            { "TAU76", "TAU75" },   // Hunters Instincts · TauEmpire
            { "TAU8", "TAU4" },   // Guardian Drone · TauEmpire
            { "TAU9", "TAU5" },   // Fire Warrior · TauEmpire
            { "TAU_Grisly_Feast", "b3735ef3d1408d544b0947e31f898e20" },   // Grisly Feast · TauEmpire
            { "TAU_Kroot_Carnivore", "TAU66" },   // Kroot Carnivore · TauEmpire
            { "TAU_Kroot_Hound", "TAU67" },   // Kroot Hound · TauEmpire
            { "TAU_Storm_of_Fire", "TAU52" },   // Storm of Fire · TauEmpire
            { "TAU_Trail_Shaper", "TAU68" },   // Trail Shaper · TauEmpire
            { "TAU_War_Shaper", "TAU65" },   // War Shaper · TauEmpire
            { "TL1", "TL36" },   // Neurothrope · Leviathan
            { "TL10", "TL3" },   // Mucolid Spore · Leviathan
            { "TL11", "TL60" },   // Neurogaunt Nodebeast · Leviathan
            { "TL12", "TL61" },   // Stranded Termagant · Leviathan
            { "TL13", "TL4" },   // Termagant · Leviathan
            { "TL14", "TL6" },   // Biovore · Leviathan
            { "TL15", "TL7" },   // Gargoyle · Leviathan
            { "TL16", "TL8" },   // Genestealer · Leviathan
            { "TL17", "TL9" },   // Pyrovore · Leviathan
            { "TL18", "TL5" },   // Tyranid Warrior · Leviathan
            { "TL19", "TL13" },   // Lictor · Leviathan
            { "TL2", "760bd3afde513774d96d6492f995fd37" },   // Spirit Leech · Leviathan
            { "TL20", "TL62" },   // Ravener · Leviathan
            { "TL21", "TL14" },   // Sporocyst · Leviathan
            { "TL22", "TL15" },   // Tyranid Prime · Leviathan
            { "TL23", "TL11" },   // Venomthrope · Leviathan
            { "TL24", "TL10" },   // Von Ryans Leaper · Leviathan
            { "TL25", "TL20" },   // Carnifex · Leviathan
            { "TL26", "TL17" },   // Deathleaper · Leviathan
            { "TL27", "TL16" },   // Mawloc · Leviathan
            { "TL28", "TL12" },   // Tyrant Guard · Leviathan
            { "TL29", "TL18" },   // Zoanthrope · Leviathan
            { "TL3", "TL35" },   // SwarmLord · Leviathan
            { "TL30", "TL19" },   // Broodlord · Leviathan
            { "TL31", "TL22" },   // Hive Crone · Leviathan
            { "TL32", "TL38" },   // Hive Guard · Leviathan
            { "TL33", "TL21" },   // Screamer-Killer · Leviathan
            { "TL34", "TL23" },   // Tyrannocyte · Leviathan
            { "TL35", "c0ddba6cc45944f46a65f4fd93bcc0a5" },   // Aerial Seeding · Leviathan
            { "TL36", "TL24" },   // Harpy · Leviathan
            { "TL37", "TL25" },   // Hive Tyrant · Leviathan
            { "TL38", "a58e05916eb5e994daa0349cbe237001" },   // Synaptic Imperative · Leviathan
            { "TL39", "TL26" },   // Maleceptor · Leviathan
            { "TL4", "5b5425f6e2848b449807bd1643d0d936" },   // Hive Commander · Leviathan
            { "TL40", "TL27" },   // Exocrine · Leviathan
            { "TL41", "TL28" },   // Haruspex · Leviathan
            { "TL42", "TL29" },   // Winged Tyrant · Leviathan
            { "TL43", "TL30" },   // Tyrannofex · Leviathan
            { "TL44", "TL39" },   // Ambush Predator · Leviathan
            { "TL45", "TL32" },   // Encircle the Prey · Leviathan
            { "TL46", "TL48" },   // Predator Instincts · Leviathan
            { "TL47", "TL33" },   // Adrenal Surge · Leviathan
            { "TL48", "TL63" },   // Bounding Leap · Leviathan
            { "TL49", "TL42" },   // Darkened Skies · Leviathan
            { "TL5", "TL34" },   // Tervigon · Leviathan
            { "TL50", "TL31" },   // Enhanced Organism · Leviathan
            { "TL51", "TL49" },   // Hyper-adaptation · Leviathan
            { "TL53", "TL50" },   // Infinite Biomorphologies · Leviathan
            { "TL54", "TL43" },   // Augmented Ferocity · Leviathan
            { "TL55", "TL44" },   // Hardened Biology · Leviathan
            { "TL56", "TL52" },   // Leviathans Tendrils · Leviathan
            { "TL57", "TL41" },   // Overwhelming Swarm · Leviathan
            { "TL58", "TL51" },   // Toxic Miasma · Leviathan
            { "TL59", "TL64" },   // Ravenous Hunter · Leviathan
            { "TL6", "cbc9d98e39cc7fb4fa6f94c7e69bda18" },   // Brood Progenitor · Leviathan
            { "TL60", "TL53" },   // Swarming Masses · Leviathan
            { "TL61", "TL45" },   // Apex Predator · Leviathan
            { "TL62", "TL47" },   // Hive Fleet Leviathan · Leviathan
            { "TL63", "TL46" },   // Rapacious Hunger · Leviathan
            { "TL64", "TL54" },   // Digestion Pool · Leviathan
            { "TL65", "TL55" },   // Protective Bio-structure · Leviathan
            { "TL66", "TL56" },   // Toxic Vapours · Leviathan
            { "TL7", "TL1" },   // Ripper Swarm · Leviathan
            { "TL75", "8f2da557fe484ee4bbf427c13eccd4cb" },   // Alpha Warrior · Leviathan
            { "TL76", "TL65" },   // Neuroloid · Leviathan
            { "TL77", "TL66" },   // Neurogaunt · Leviathan
            { "TL78", "TL67" },   // Barbgaunt · Leviathan
            { "TL79", "TL75" },   // Termagant Brood · Leviathan
            { "TL8", "TL37" },   // Spore Mine · Leviathan
            { "TL80", "TL68" },   // Psychophage · Leviathan
            { "TL81", "468db99b4b10d0745bab346df37d3fb2" },   // Feeding Frenzy · Leviathan
            { "TL82", "TL69" },   // Neurotyrant · Leviathan
            { "TL83", "TL70" },   // Norn Emissary · Leviathan
            { "TL84", "TL71" },   // Toxic Entanglement · Leviathan
            { "TL85", "TL72" },   // Tyranid Invasion · Leviathan
            { "TL86", "TL73" },   // Bestial Rage · Leviathan
            { "TL9", "TL2" },   // Hormagaunt · Leviathan
            { "UM1", "UM46" },   // Marneus Calgar · Ultramarines
            { "UM10", "UM2" },   // Octavio Infiltrator · Ultramarines
            { "UM100", "UM117" },   // No Respite · Ultramarines
            { "UM101", "UM118" },   // Indomitus Crusade · Ultramarines
            { "UM11", "UM1" },   // Scout · Ultramarines
            { "UM12", "UM4" },   // Assault Intercessor · Ultramarines
            { "UM13", "UM3" },   // Firstborn · Ultramarines
            { "UM14", "UM6" },   // Primaris Intercessor · Ultramarines
            { "UM16", "UM11" },   // 7th Company Bike · Ultramarines
            { "UM17", "UM8" },   // Eliminator · Ultramarines
            { "UM18", "UM56" },   // Intercessor Sergeant · Ultramarines
            { "UM2", "dcbd28525888ce54aaa93a47ba4de1e6" },   // Master Tactician · Ultramarines
            { "UM23", "d94f91051ecfe014f97577ff573571aa" },   // Spear of Macragge · Ultramarines
            { "UM28", "UM16" },   // Sergeant Allectius · Ultramarines
            { "UM3", "UM47" },   // Uriel Ventris · Ultramarines
            { "UM30", "UM15" },   // Apothecary Polixis · Ultramarines
            { "UM32", "UM21" },   // Primaris Judiciar · Ultramarines
            { "UM34", "UM23" },   // Bladeguard Lieutenant · Ultramarines
            { "UM35", "UM18" },   // Company Ancient · Ultramarines
            { "UM39", "UM25" },   // Devastator Centurion · Ultramarines
            { "UM4", "9238c27730a0c424288d46ed0f4a742c" },   // Master of the Fleet · Ultramarines
            { "UM40", "UM26" },   // Epistolary Librarian · Ultramarines
            { "UM41", "UM59" },   // Stormtalon · Ultramarines
            { "UM44", "UM29" },   // Captain Sicarius · Ultramarines
            { "UM45", "0544047df88d2ba4cad9c17ff080c0eb" },   // Greatest Deeds · Ultramarines
            { "UM47", "UM32" },   // Tactical Insight · Ultramarines
            { "UM48", "UM41" },   // Sacred Bolter · Ultramarines
            { "UM49", "UM31" },   // Sheltered Location · Ultramarines
            { "UM5", "UM48" },   // Varro Tigurius · Ultramarines
            { "UM51", "UM36" },   // Hero's Respite · Ultramarines
            { "UM52", "UM60" },   // Humanity's Shield · Ultramarines
            { "UM54", "UM34" },   // Point-Blank Shot · Ultramarines
            { "UM58", "UM43" },   // Knights of Macragge · Ultramarines
            { "UM59", "UM44" },   // Armoured Support · Ultramarines
            { "UM61", "UM38" },   // Stormraven · Ultramarines
            { "UM62", "UM39" },   // Inspired Retribution · Ultramarines
            { "UM66", "315d9f293e4f56d4f813584b43f0511c" },   // Exhortation of Rage · Ultramarines
            { "UM67", "UM53" },   // Hive Factory · Ultramarines
            { "UM68", "b14d1db2b0730d649b3bec35af100363" },   // Master of Battle · Ultramarines
            { "UM69", "ef596edae1828bd46bfc4348535a3acc" },   // Supreme Strategist · Ultramarines
            { "UM7", "0740581c2c0523745a42dbec1b2ef82a" },   // Might of Heroes · Ultramarines
            { "UM70", "UM88" },   // Valius Paxor · Ultramarines
            { "UM71", "UM89" },   // Incursor · Ultramarines
            { "UM72", "UM71" },   // Storm Speeder Hailstrike · Ultramarines
            { "UM73", "UM91" },   // Tactical Marine · Ultramarines
            { "UM74", "UM92" },   // Firestrike Servo Turret · Ultramarines
            { "UM75", "UM74" },   // Rapid Deployment · Ultramarines
            { "UM76", "UM75" },   // Armoured Offensive · Ultramarines
            { "UM77", "UM95" },   // Attack Bike · Ultramarines
            { "UM78", "UM96" },   // Chapter Champion · Ultramarines
            { "UM79", "UM81" },   // Eliminator Sergeant · Ultramarines
            { "UM80", "UM98" },   // Devastator Marine · Ultramarines
            { "UM81", "UM99" },   // Phobos Librarian · Ultramarines
            { "UM82", "UM103" },   // Terminator · Ultramarines
            { "UM83", "UM101" },   // 2nd Company Terminator · Ultramarines
            { "UM84", "UM100" },   // Chaplain Cassius · Ultramarines
            { "UM85", "8482077f309b08943b190f18b5874871" },   // Catechism of Death · Ultramarines
            { "UM86", "UM105" },   // Dreadnought · Ultramarines
            { "UM87", "UM104" },   // Bladeguard Ancient · Ultramarines
            { "UM88", "UM102" },   // Terminator Captain · Ultramarines
            { "UM89", "UM106" },   // Ferren Areios · Ultramarines
            { "UM9", "cf0b346f70e748c4bbfc4dc03ab39ead" },   // Smite · Ultramarines
            { "UM90", "UM107" },   // Whirlwind · Ultramarines
            { "UM91", "UM108" },   // Brutalis Dreadnought · Ultramarines
            { "UM92", "UM109" },   // Adaptive Strategy · Ultramarines
            { "UM93", "UM110" },   // Fall Back · Ultramarines
            { "UM94", "UM112" },   // Champions of Humanity · Ultramarines
            { "UM95", "UM113" },   // Duty's End · Ultramarines
            { "UM96", "UM111" },   // Shall Know No Fear · Ultramarines
            { "UM97", "UM114" },   // Lead From the Front · Ultramarines
            { "UM98", "UM115" },   // Stormhawk Interception · Ultramarines
            { "UM99", "UM116" },   // No Mercy · Ultramarines
            { "UM_Aggressor_Sergeant", "UM19" },   // Aggressor Sergeant · Ultramarines
            { "UM_Angel_s_Wrath", "4de4fcfb5d7e0554a88b9f419a2cd7c2" },   // Angel's Wrath · Ultramarines
            { "UM_Angels_of_Death", "UM64" },   // Angels of Death · Ultramarines
            { "UM_Antaro_Chronus", "UM12" },   // Antaro Chronus · Ultramarines
            { "UM_Assault_Centurion", "UM27" },   // Assault Centurion · Ultramarines
            { "UM_Assault_Doctrine", "UM86" },   // Assault Doctrine · Ultramarines
            { "UM_Author_of_the_Codex", "40667e346ae58a84dbda1033dff70ccb" },   // Author of the Codex · Ultramarines
            { "UM_Avenging_Zeal", "UM40" },   // Avenging Zeal · Ultramarines
            { "UM_Battlefield_Supremacy", "UM62" },   // Battlefield Supremacy · Ultramarines
            { "UM_Bladeguard_Sergeant", "UM80" },   // Bladeguard Sergeant · Ultramarines
            { "UM_Chairon", "UM77" },   // Chairon · Ultramarines
            { "UM_Codex_Discipline", "UM35" },   // Codex Discipline · Ultramarines
            { "UM_Death_from_Above", "UM37" },   // Death from Above · Ultramarines
            { "UM_Desolation_Marine", "UM58" },   // Desolation Marine · Ultramarines
            { "UM_Devastator_Doctrine", "UM84" },   // Devastator Doctrine · Ultramarines
            { "UM_Exemplary_Warrior", "cc1400347b15ee3449bc18bbdb9ddec9" },   // Exemplary Warrior · Ultramarines
            { "UM_Firestrike_Turrets", "UM52" },   // Firestrike Turrets · Ultramarines
            { "UM_Forward_Deployment", "UM42" },   // Forward Deployment · Ultramarines
            { "UM_Gather_the_Company", "UM45" },   // Gather the Company · Ultramarines
            { "UM_Heavy_Intercessor", "UM78" },   // Heavy Intercessor · Ultramarines
            { "UM_Hellblaster", "UM10" },   // Hellblaster · Ultramarines
            { "UM_Honour_Guard", "UM14" },   // Honour Guard · Ultramarines
            { "UM_Inceptor_Sergeant", "UM22" },   // Inceptor Sergeant · Ultramarines
            { "UM_Invictor_Warsuit", "UM70" },   // Invictor Warsuit · Ultramarines
            { "UM_Land_Speeder", "UM69" },   // Land Speeder · Ultramarines
            { "UM_Lieutenant_Calsius", "UM24" },   // Lieutenant Calsius · Ultramarines
            { "UM_Lieutenant_Titus", "UM76" },   // Lieutenant Titus · Ultramarines
            { "UM_Light_Cover", "UM54" },   // Light Cover · Ultramarines
            { "UM_Master_of_Arcana", "f923701f207d23a4f81d6ff5891b4a0c" },   // Master of Arcana · Ultramarines
            { "UM_Master_of_Arms", "55b8e3d2040f86d4190ffb1a91abb5ed" },   // Master of Arms · Ultramarines
            { "UM_Oath_of_Moment", "UM73" },   // Oath of Moment · Ultramarines
            { "UM_Outrider", "UM67" },   // Outrider · Ultramarines
            { "UM_Paragon_of_Ultramar", "7693f2b21b2f85644b739da990bc2216" },   // Paragon of Ultramar · Ultramarines
            { "UM_Pariah_Vanguard", "UM33" },   // Pariah Vanguard · Ultramarines
            { "UM_Phobos_Lieutenant", "UM90" },   // Phobos Lieutenant · Ultramarines
            { "UM_Predator_Annihilator", "UM72" },   // Predator Annihilator · Ultramarines
            { "UM_Primarch_of_the_XIII", "e874c2bd84895e64694daa44bd5fd5c2" },   // Primarch of the XIII · Ultramarines
            { "UM_Primaris_Chaplain", "UM13" },   // Primaris Chaplain · Ultramarines
            { "UM_Primaris_Eradicator", "UM79" },   // Primaris Eradicator · Ultramarines
            { "UM_Primaris_Impulsor", "UM17" },   // Primaris Impulsor · Ultramarines
            { "UM_Primaris_Inceptor", "UM9" },   // Primaris Inceptor · Ultramarines
            { "UM_Primaris_Invader", "UM57" },   // Primaris Invader · Ultramarines
            { "UM_Primaris_Reiver", "UM5" },   // Primaris Reiver · Ultramarines
            { "UM_Primaris_Techmarine", "UM68" },   // Primaris Techmarine · Ultramarines
            { "UM_Redemptor_Dreadnought", "UM28" },   // Redemptor Dreadnought · Ultramarines
            { "UM_Repulsor_Executioner", "UM30" },   // Repulsor Executioner · Ultramarines
            { "UM_Righteous_Fury", "26d4ded12b28a3742b5c8fc155e29803" },   // Righteous Fury · Ultramarines
            { "UM_Roboute_Guilliman", "UM87" },   // Roboute Guilliman · Ultramarines
            { "UM_Scout_Sniper", "UM94" },   // Scout Sniper · Ultramarines
            { "UM_Scout_bike", "UM66" },   // Scout bike · Ultramarines
            { "UM_Scryer_s_Gaze", "c4ff279078605aa42942c8dd16b77147" },   // Scryer's Gaze · Ultramarines
            { "UM_Sergeant_Gadriel", "UM82" },   // Sergeant Gadriel · Ultramarines
            { "UM_Sergeant_Telion", "UM20" },   // Sergeant Telion · Ultramarines
            { "UM_Sternguard_Sergeant", "UM55" },   // Sternguard Sergeant · Ultramarines
            { "UM_Suppressor", "UM97" },   // Suppressor · Ultramarines
            { "UM_Tactical_Doctrine", "UM85" },   // Tactical Doctrine · Ultramarines
            { "UM_The_Chapter_s_Due", "UM61" },   // The Chapter's Due · Ultramarines
            { "UM_Tyrannic_War_Veteran", "UM7" },   // Tyrannic War Veteran · Ultramarines
            { "UM_Valtus", "UM83" },   // Valtus · Ultramarines
            { "UM_Vico_Therbeus", "UM93" },   // Vico Therbeus · Ultramarines
        };

        // ==================================================================
        //  迁 移 口
        // ==================================================================
        //
        //  🔴 四个口的分工（选错口 = 静默改错卡）：
        //    · `MigrateDeckVerified` —— **唯一**该给「一整副牌」用的口（双解 + 防御位无歧义规则）
        //    · `MigrateDeckJson`     —— 回放的 `MsgStart.hostDeckJson/clientDeckJson`（包一层验证）
        //    · `MigrateArray` / `Migrate` —— **只许在「这份录像/这副牌已判定是旧代」时**用
        //                                 （回放的 `MsgAction.pickIds` / `defId`：整份录像一代到底）
        //    · `MigrateIds`          —— 导入串那条路（先按现代解分类，再拿它做旧代那一路）
        //  ⛔ **别在别处再写一份「按查表替换」** —— 表不是单射（见文件头）。

        /// <summary>这条 id 是不是一个已知的旧代 id。
        /// ⚠️ 「表里有」**不等于**这副牌是旧代 —— **891/1102** 个旧 id 今天**仍是合法 id**（指着别的卡）。
        /// 判代际只能走 <see cref="MigrateDeckVerified"/>（整副双解）。</summary>
        public static bool IsLegacy(string id)
        {
            return !string.IsNullOrEmpty(id) && Map.ContainsKey(id);
        }

        /// <summary>一段文本里有没有任何一个已知旧 id（给 JSON 快速预检用 ——
        /// 没有就**原样返回、一个字节都不动**，干净数据零影响）。</summary>
        public static bool ContainsAny(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var k in Map.Keys)
                if (text.IndexOf(k, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        static bool _warnedNoPool;

        /// <summary>卡池查表口 —— **和全仓其它地方同一个解析规则**（`CardDatabase.DeckLookup`：
        /// ① 按稳定 id；② 找不到再**按卡名**）。
        ///
        /// 🔴 **为什么必须保留第 ② 条（按卡名）**：2026-09-13 之前的存档里存的是**卡名**
        /// （`DeckLookup` 那段注释写着这条退路就是为旧存档留的；`DeckRulesTest` 的 `_deckstore_selftest`
        /// 夹具至今还在用 `"Anvirr Keltoc"` / `"Aspect Shrine"` 这种**卡名**当 id）。
        /// 若这里用「严格 id」查表，一张**按卡名的旧存档**里的防御卡会被判成「查不到」⇒ 被清空。
        /// ⚠️ 判代际（是不是旧 id）**不看这个口** —— 那看 `Map`；这个口只答「这条引用现在指向哪张卡」。
        ///
        /// 池子没加载时返回 **null** ⇒ 调用方**跳过迁移**。
        /// ⛔ **别拿空池当「全都查不到」去清数据**（那会把玩家每张防御卡都清掉）。
        /// </summary>
        public static Func<string, CardDef> PoolLookup()
        {
            var pool = CardDatabase.Load();
            if (pool == null || pool.Count == 0)
            {
                if (!_warnedNoPool)
                {
                    _warnedNoPool = true;
                    Debug.LogWarning(Tag + " 卡池没加载（`CardDatabase.Load()` 返回空）⇒ **跳过旧 id 迁移**"
                                   + "（⛔ 不拿空池判「查不到」，那会误清玩家数据）。");
                }
                return null;
            }
            return CardDatabase.DeckLookup(pool);      // ① id ② 卡名（见上一段：第 ② 条不能去掉）
        }

        /// <summary>
        /// **一整副牌**的读时迁移闸（**唯一**入口）。两件事，次序有意为之：
        ///   ① **防御卡位** —— 无歧义规则（<see cref="MigrateDefensive"/>），**总是先跑**；
        ///   ② **督军 + 普通卡位** —— 整副**双解验证**（<see cref="MigrateByDualSolution"/>）。
        ///
        /// 返回迁移了几处；**只改内存**；**幂等**（迁完现代解合法 ⇒ 再调一次第一步就返回）。
        /// </summary>
        public static int MigrateDeckVerified(PlayerDeck deck, Func<string, CardDef> lookup, string where)
        {
            if (deck == null) return 0;
            if (lookup == null)
            {
                Debug.LogWarning(Tag + " 迁移跳过（" + where + "）：卡池没加载（lookup = null）—— "
                               + "⛔ 不改任何 id（拿空池判「查不到」会误清数据）");
                return 0;
            }
            // `JsonUtility` 反序列化的卡组可能带 `"CardIds": null` ⇒ 先归一（`Validate` 会直接遍历它）
            if (deck.CardIds == null) deck.CardIds = new List<string>();
            int n = MigrateDefensive(deck, lookup, where);
            n += MigrateByDualSolution(deck, lookup, where);
            return n;
        }

        /// <summary>
        /// **防御卡位**：无歧义的规则（2026-10-18 现核：旧池 39 个防御卡 id 与现池 39 个**一个都不重合**
        /// ⇒ 「同一 id 两代都是防御卡、但是两张不同的卡」这种情况**不存在**）。
        ///   · 现池里这个 id **是防御卡** ⇒ 不动（干净数据 / 已经迁过）；
        ///   · 现池里**不是**、而**旧代同 id 是**（= 表的目标在现池是防御卡）⇒ 按旧代迁；
        ///   · **查得到、但不是防御卡**（两代都不是）⇒ **清空**（`DeckRules.Validate` ③ 允许无防御卡；
        ///     开局照原版兜底补一张）；
        ///   · **彻底查不到**（id / 卡名 / 旧代表都没有）⇒ **原样保留 + 出声**（理由见方法里那段注释）。
        /// 为什么防御位可以单独判、不用双解：卡位语义本身就限定了「这里只能放防御卡」⇒ 无歧义。
        /// </summary>
        static int MigrateDefensive(PlayerDeck deck, Func<string, CardDef> lookup, string where)
        {
            string id = deck.DefensiveId;
            if (string.IsNullOrEmpty(id)) return 0;
            if (IsDefence(lookup, id)) return 0;
            string real;
            if (Map.TryGetValue(id, out real) && IsDefence(lookup, real))
            {
                deck.DefensiveId = real;
                Debug.Log(Tag + " 防御卡位旧 id 迁移（" + where + "）：`" + id + "` → `" + real + "`"
                        + "（现池里 `" + id + "` 不是防御卡、**旧代同 id 是** ⇒ 无歧义；⛔ 只改内存）");
                return 1;
            }
            if (lookup(id) == null && !Map.ContainsKey(id))
            {
                // 🔴 **查不到 ⇒ 不动、只出声**（2026-10-18 的一处**有意偏离**，理由要记住）：
                //    「查不到」在这份数据上**分不出**两种东西 —— ① 真·垃圾 id；② **2026-09-13 之前
                //    按【卡名】存的旧存档**（这类卡名在本函数用的 `DeckLookup` 里本该解析得出，
                //    能走到这里说明连按卡名都没有）。清空是**不可逆**的（玩家下次保存会写回空），
                //    而留着它的代价只是「开局少一张防御卡」（`DeckBuilder.FromDeck` 会 `LogError` 后丢牌，
                //    不静默）⇒ **两害相权，不清**。
                Debug.LogWarning(Tag + " 防御卡位无法判定（" + where + "）：`" + id + "` 在现池里按 id、按卡名"
                               + "都查不到，旧代表里也没有 ⇒ **原样保留、不动**（⛔ 不猜、不清空）");
                return 0;
            }
            deck.DefensiveId = "";
            Debug.LogWarning(Tag + " 防御卡位清空（" + where + "）：`" + id + "` 在现池里**查得到、但不是防御卡**，"
                           + "旧代同 id 也不是防御卡 ⇒ 清空（`DeckRules.Validate` ③ 允许无防御卡，"
                           + "开局会照原版兜底补一张）。⛔ 只改内存");
            return 1;
        }

        static bool IsDefence(Func<string, CardDef> lookup, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var c = lookup(id);
            return c != null && c.Type == "defence";
        }

        /// <summary>
        /// **督军 + 普通卡位**的整副**双解验证**（`资料/普查产出_1018/R_防御卡位错卡_成因.md` §4 第 2 条）。
        ///
        /// 为什么不能单看 id：**891/1102** 个旧 id 今天仍是合法 id，只是指着另一张卡 ⇒ 单条无解。
        /// 判据 = **整副**：
        ///   · 先按**现代** id 跑 `DeckRules.Validate`；
        ///   · 现代不合法时，再按**旧代迁移后**的 id 跑一次；
        ///   · **只有「现代不合法 ∧ 旧代合法」才落**；两解都合法 / 都非法 ⇒ **不动并出声**（⛔ 不猜）。
        ///
        /// 返回迁移了几处。**一次遍历、只看原始值**（`ApplyMap`）⇒ 链式 id 不会二次替换。
        /// </summary>
        static int MigrateByDualSolution(PlayerDeck deck, Func<string, CardDef> lookup, string where)
        {
            // 先算「旧代解」长什么样（在 Clone 上算，**没落之前 `deck` 一个字不动**）
            var alt = deck.Clone();
            var tally = new Dictionary<string, int>(StringComparer.Ordinal);
            string w = alt.WarlordId, real;
            if (!string.IsNullOrEmpty(w) && Map.TryGetValue(w, out real))
            {
                alt.WarlordId = real;
                tally[w] = 1;
            }
            int changed = ApplyMap(alt.CardIds, tally);
            if (changed == 0) return 0;      // 一个旧键都没有 ⇒ 不是这一代的数据（安静返回）

            DeckError modern = DeckRules.Validate(deck, lookup, deck.IsSkirmish);
            DeckError legacy = DeckRules.Validate(alt, lookup, alt.IsSkirmish);

            if (modern == DeckError.None)
            {
                // 两解都合法 = 真歧义（10-14 之后新建的卡组不能套旧代表 —— 但旧代表恰好也成立）
                if (legacy == DeckError.None)
                    Debug.Log(Tag + " 代际歧义（" + where + "）：现代解与旧代解**都合法**"
                            + "（旧代解会改 " + changed + " 处）⇒ **按现代解、不动**（⛔ 不猜）。");
                return 0;
            }
            if (legacy != DeckError.None)
            {
                Debug.LogWarning(Tag + " 双解都不合法（" + where + "）：现代解 = " + modern
                               + " · 旧代解 = " + legacy + "（会改 " + changed + " 处）"
                               + " ⇒ **不动**（⛔ 不猜；这副牌多半本来就不合法，或新旧 id 混用）。");
                return 0;
            }

            // **现代不合法 ∧ 旧代合法** ⇒ 落
            deck.WarlordId = alt.WarlordId;
            deck.CardIds = alt.CardIds;      // `alt` 是 `Clone()` 出来的独立 List，`deck` 独占它
            Debug.Log(Tag + " 旧 id 迁移（" + where + "）：现代解不合法（" + modern + "）、旧代解合法"
                    + " ⇒ 按**双解**判定迁移 " + changed + " 处（" + TallyLine(tally) + "）"
                    + " —— ⛔ 只改内存，文件不动");
            return changed;
        }

        /// <summary>一次遍历、**只看原始值**的并发替换（链式 id 的安全做法）。
        /// 原地改 <paramref name="ids"/>，返回改了几处。
        /// ⚠️ **别改成「边替换边查表」**：`ASH20→ASH15`、`ASH15→ASH11` 成链 ⇒ 会静默改成第三张卡。</summary>
        static int ApplyMap(List<string> ids, Dictionary<string, int> tally)
        {
            if (ids == null) return 0;
            int n = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                string oldId = ids[i];                 // ← 先读出**原始值**
                string real;
                if (string.IsNullOrEmpty(oldId) || !Map.TryGetValue(oldId, out real)) continue;
                ids[i] = real;
                n++;
                if (tally != null)
                {
                    int k;
                    tally.TryGetValue(oldId, out k);
                    tally[oldId] = k + 1;
                }
            }
            return n;
        }

        static string TallyLine(Dictionary<string, int> tally)
        {
            var keys = new List<string>(tally.Keys);
            keys.Sort(StringComparer.Ordinal);
            var sb = new System.Text.StringBuilder();
            foreach (var k in keys)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append('`').Append(k).Append("` → `").Append(Map[k]).Append("` ×").Append(tally[k]);
            }
            return sb.ToString();
        }

        // ==================================================================
        //  回放 / 导入串专用
        // ==================================================================

        /// <summary>
        /// 迁移**一份 `JsonUtility` 化的 `PlayerDeck`**（回放的 `MsgStart.hostDeckJson/clientDeckJson`）。
        /// 不含任何旧 token ⇒ **原字符串原样返回**（零改动、零往返）；解析失败 ⇒ **出声并原样返回**（⛔ 不猜、不丢）。
        /// <paramref name="migrated"/> = 这份牌**是不是真的迁移过** —— 回放的 `pickIds/defId`
        /// 靠它决定「这份录像整份是旧代吗」（旧代局的**每一个** id 都要按旧代表迁）。
        /// </summary>
        public static string MigrateDeckJson(string json, Func<string, CardDef> lookup, string where,
                                             out bool migrated)
        {
            migrated = false;
            if (string.IsNullOrEmpty(json) || !ContainsAny(json)) return json;
            PlayerDeck d;
            try { d = JsonUtility.FromJson<PlayerDeck>(json); }
            catch (Exception e)
            {
                Debug.LogError(Tag + " 旧 id 迁移失败（" + where + "）：这份卡组 JSON 解不开（" + e.Message + "）"
                             + " ⇒ **原样保留**（⛔ 不猜、不丢）");
                return json;
            }
            if (d == null)
            {
                Debug.LogError(Tag + " 旧 id 迁移失败（" + where + "）：`FromJson` 返回 null ⇒ **原样保留**");
                return json;
            }
            if (MigrateDeckVerified(d, lookup, where) <= 0) return json;
            migrated = true;
            return JsonUtility.ToJson(d);
        }

        /// <summary>迁移一条 id（**出声**）。🔴 **只许在「这份录像/这副牌已判定是旧代」时用** ——
        /// 它**不看**现代解，盲套表（表不是单射，见文件头）。</summary>
        public static string Migrate(string id, string where)
        {
            string real;
            if (string.IsNullOrEmpty(id) || !Map.TryGetValue(id, out real)) return id;
            Debug.Log(Tag + " 旧 id 迁移（" + where + "）：`" + id + "` → `" + real + "`"
                    + "（同代整份录像；⛔ 只改内存，文件不动）");
            return real;
        }

        /// <summary>迁移一个 id 数组（回放的 `MsgAction.pickIds`）。返回同一个数组（原地改）。
        /// 🔴 同上：**只许在已判定旧代的场合用**。</summary>
        public static string[] MigrateArray(string[] ids, string where)
        {
            if (ids == null || ids.Length == 0) return ids;
            var tally = new Dictionary<string, int>(StringComparer.Ordinal);
            int total = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                string oldId = ids[i];
                string real;
                if (string.IsNullOrEmpty(oldId) || !Map.TryGetValue(oldId, out real)) continue;
                ids[i] = real;
                total++;
                int n;
                tally.TryGetValue(oldId, out n);
                tally[oldId] = n + 1;
            }
            if (total > 0)
                Debug.Log(Tag + " 旧 id 迁移（" + where + "）：" + TallyLine(tally) + " —— 共 "
                        + ids.Length + " 项里改了 " + total + " 处（⛔ 只改内存）");
            return ids;
        }

        /// <summary>把一个 id 列表原地按表迁一次（**一次遍历、只看原始值**）。返回改了几处。
        /// 调用点 = `DeckLibrary.ImportString` 的「旧代解」那一路。</summary>
        public static int MigrateIds(List<string> ids)
        {
            return ApplyMap(ids, null);
        }
    }
}
