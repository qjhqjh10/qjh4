// CardDatabase.cs — 卡表加载
//
// ⚠️ **`Core/` 下的文件一律不碰 UnityEngine**（加 asmdef 隔离时要保证 `Core/` 能单独编过）。
//    `Data/` 下允许碰 —— 本文件和 `Data/DeckStore.cs`（存档要走 persistentDataPath）都用。
//
// 数据不是直接读 `数据/游戏数据/card_stats.json`（978 KB，大半是引擎用不到的路径字符串），
// 而是读 `工具/gen_cards_engine.py` 生成的精简版（232 KB，数值已归一化、没有 null）。
// 改数据 → 重跑那个脚本 → 再跑 `--check` 对账。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static class CardDatabase
    {
        /// <summary>`Assets/RuleEngine/Resources/cards_engine.json`</summary>
        public const string ResourcePath = "cards_engine";

        [Serializable]
        public class FileDto
        {
            public int version;
            public int count;
            public string source;
            public CardDto[] cards;
            /// <summary>🆕 v12（`E12` · `A2142`）：天赋名 → 天赋卡 id（键已按 `CreatePool.Norm` 归一）。
            /// `null` / 空 = 旧表（&lt; v12）—— 下面的 version 守卫会出声。</summary>
            public TalentNameIdDto[] talentNameIds;
        }

        [Serializable]
        public class CardDto
        {
            /// <summary>**稳定 id = 原版 `uniqueId`**（`AM12` 这种短码，或 32 位 GUID 形态）。
            /// 🔴 **2026-10-18（`W25`）订正**：原来这里写「或 `AM_Some_Card` 这样的自造 id」——
            /// **现已不成立**：池内 **1127/1127 全是真 id、自造 0 张**（`gen_cards_engine.py` 按
            /// `(阵营码, 卡名) → uniqueId` 唯一落位直取；生成器末尾有硬断言，自造 ⇒ 非零退出、不落盘）。
            /// ⛔ **卡名不再是身份**：`id` 为空的那张卡会被 `Parse()` **报错并跳过**（见那里的注释）。</summary>
            public string id;
            public string name;
            public string type;
            public int cost;
            public int attack;
            public int health;
            public int ranged;
            public string[] keywords;
            public string desc;
            public string faction;
            public string rarity;
            /// <summary>兵种（`Infantry` / `Vehicle` / `Drone` / `Elixir` / `Secret` …）。
            /// 空串 = 原版数据里就没有。见 `CardDef.Subtype` 的说明。</summary>
            public string subtype;
            /// <summary>🆕 v19（2026-10-23 · 引擎线 `新账-97` ③/⑤）：原版 `RawCardScript.spellType`
            /// （`SpellType` 枚举：0 Normal / 60 SecretOrder / 120 Quest / 150 Secret / 230 Sabotage …）。
            /// 🔴 **缺省 `-1` = 未知**（旧表 &lt; v19 整列没有）—— ⛔ 不是 `Normal(0)`；
            /// 旧表由上面那条 `version &lt; 19` 守卫出声。语义 → `CardDef.SpellType` 的 doc。</summary>
            public int spellType = -1;
            /// <summary>中文名（可选 —— 没翻译的卡没有这两个字段）。来自 `数据/卡牌翻译/zh_cards.json`</summary>
            public string nameZh;
            public string descZh;
            /// <summary>🔴 🆕 2026-10-14（`A1070`）：督军后手那一局会拿到的那张**防御卡的 id**
            /// —— 原版 `RawCardScript.goSecondCardInHand`（督军 SO 上的 PPtr）。
            /// 只有督军卡有值；**只有 11 个阵营有真值**（见 `CardDef.GoesSecondCard`）。
            /// 数据源 = `数据/游戏数据/warlord_goes_second.json` → `工具/gen_cards_engine.py`。</summary>
            public string goesSecond;
            /// <summary>🔴 🆕 2026-10-14（`A1798`）：原版 `RawCardScript.inventoryOptions`
            /// （`CardInventoryOptions`：`InInventory = 0` / `CantAddToDeck = 5` / `NotInInventory = 10`）。
            /// 判据消费者 = `DeckRules.CannotAddToDeck`（原版 `DeckUtility__ValidateDeckCards.c:68-72`）。
            /// ⚠️ **`-1` = 真值里查不到这张卡**（真值快照 2025-07-30、比现池旧）—— ⛔ **别当成 0**。
            /// 数据源 = `数据/游戏数据/card_inventory_options.json` → `工具/gen_card_inventory_options.py`
            /// → `工具/gen_cards_engine.py`。</summary>
            public int inventoryOptions;
            /// <summary>同上的 `RawCardScript.showInCollectionOptions`
            /// （`ShowInCollectionOptions`：`Hide = 0` / `Show = 5` / `OnlyIfOwned = 10`）。
            /// 原版 `DeckUtility__ValidateDeckCards.c:73-77` 也拿它拦卡组。`-1` 含义同上。</summary>
            public int showInCollectionOptions;
            /// <summary>🔴 🆕 2026-10-14（`A1775`）：原版 `RawCardScript.initiative`
            /// （`Initiative` 枚举：`Undefined = 0` / `Low = 10` / `Medium = 20` / `High = 30` /
            /// `VeryHigh = 40`）—— **谁先手**的判据。
            /// ⚠️ **`-1` = 真值里查不到这张卡**（快照 2025-07-30、比现池旧）—— ⛔ **别当成 `0`**；
            /// 判先后手时 `-1` **不比**（退回原版最后一步），见 `CardDef.Initiative` /
            /// `RuleEngine/Core/FirstSeatRules.cs`。
            /// 数据源 = `数据/游戏数据/card_initiative.json` → `工具/gen_card_initiative.py`
            /// → `工具/gen_cards_engine.py`。</summary>
            public int initiative;

            /// <summary>🔴 🆕 2026-10-21（引擎线 `T03·b`）：**开局加成三列**
            /// （原版 `RawCardScript.initialHandModifier / initialManaModifier /
            /// initialSpiritStoneModifier`，偏移 `+0x118 / +0x11C / +0x128`）。
            ///
            /// <para>判据与消费点 → `CardDef.InitialHandModifier` / `InitialManaModifier` /
            /// `InitialSpiritStoneModifier` 三个属性的 doc（⛔ 别在这里抄第二份）。</para>
            ///
            /// <para>⚠️ **缺值口径 = `0`**（不是 `-1`）：这三个字段的 `0` 是**合法取值**
            /// 「不加不减」；原版真值 1,351 张**全是 0**，生成器**只在非 0 时才落列**
            /// ⇒ 今天卡表里根本没有这三个键，读进来就是 `0`。</para></summary>
            public int initialHandModifier;
            /// <inheritdoc cref="initialHandModifier"/>
            public int initialManaModifier;
            /// <inheritdoc cref="initialHandModifier"/>
            public int initialSpiritStoneModifier;

            /// <summary>🔴 🆕 2026-10-21（引擎线 `T03·b`）：**`skipCardDraw` 手牌效果的生产列**
            /// —— 原版 `cardAbilities[].abilityLogic[].handBuff` 里
            /// `cardEffect.buffType == BuffType.skipCardDraw(7)` 的那些条目。
            ///
            /// <para>形状 `[{"keyword": …, "uses": …, "limited": …, "source": …}]`：
            /// 原版的 `trigger`（`AbilityTrigger` 数值）在 `工具/gen_cards_engine.py` 里翻成
            /// **引擎触发关键词**；对不上的**不落并在跑完摘要里出声**。</para>
            ///
            /// <para>⚠️ 真值 1,351 张里 `handBuff.buffType` **没有一张是 7**（只有 0 / 2 / 3）
            /// ⇒ 今天这个键**根本不会出现**（休眠机制，铁律 11 先建）。</para>
            /// </summary>
            public SkipDrawDto[] skipDraws;

            /// <summary>🔴 🆕 **v9（2026-10-15 · P2 相 `A1776`）**：**护甲值**（卡底那枚银盾里的数字）
            /// —— `Armour N` 关键词里的那个 `N` 的**一等公民**版本。
            ///
            /// 为什么单开一列：`A1776` 要的是 **「护甲值」与「trait 清单」解耦** ——
            /// 真值 `CardTraits` 里 `name == "armour"` **全快照只有 2 条**，而 `armour`
            /// **数值字段 >0 有 62 张**（`资料/普查产出_第二十七会话/A1748_原版trait真值.md` §7）
            /// ⇒ **护甲值走 `armour` 字段，不走 trait**。
            ///
            /// 🔴 **取值 = PnP 成品卡图优先、真值兜底**（闸 = `施工设计 §七 已裁⑥`）：
            /// PnP 那一层 = `keywords` 里那个 `Armour N`（来源：OCR + `cardface_fixes` +
            /// `fix_own_armour` 从 `desc` 正则补的）；PnP 读不到才退真值 `armour` 字段。
            /// 实测（2026-10-15 现读生成物）：**80 张 >0、全部取 PnP 那一层、真值兜底 0 张**
            /// —— 而真值只有 62 张 >0（PnP ⊃ 真值），差额那 3 张（`Abominant` / `Hulking Aberrant` /
            /// `Rukkatrukk Squigbuggy`）是**亲开 PnP 图核过、图上确实印着盾 `1`** 的。
            ///
            /// 🔴 **消费者【已切换】· 2026-10-15 接线批（本条原写「消费者尚未切换」）**：
            /// 引擎的读点已经切到本列 —— 落点 = `Core/CardDef.cs` 构造函数里
            /// 「卡池 v9 的 `armour` 列优先、`keywords` 字符串旧口径兜底」那一段，
            /// 全仓护甲读点收成一处 = `_keywords[KeywordTable.Armour]`
            /// （`KwValue` / `Has` / `UnitState.Armor` / 表现层卡面显示全走它）。
            /// 两套口径的对账口 = `CardDatabase.ReportArmourDisagreements`（装载时出声）
            /// + `RuleEngineTest.TestCardDatabase` 那条断言；⛔ 别在别处再写第二份「护甲从哪来」。
            /// ⚠️ **实测那 1 张例外**：`DA66 Ravenwing Ballistus Dreadnought` 本列写 `0`，
            /// 而它的 `keywords` 是**裸 `Armour`**、`desc` 写着 `Armour 1.`、
            /// PnP 成品卡图（`Dark Angels/3部队/Warpforge_09_Ballistus-Dreadnought.png`，
            /// 2026-10-15 亲读）右下银盾印 **1** ⇒ **真值 = 1、错的是本列**（生成器那一侧已另立账）。
            /// ⇒ 引擎对「本列 = 0 而 `keywords` 有值」这一支**按旧口径兜底**（不静默丢护甲）+ 出声。
            /// `0` = 卡底没有银盾（**真值**，不是「缺值」）。</summary>
            public int armour;

            /// <summary>🔴 🆕 **v9（`A1776`）**：真值 **`CardTraits`** 的全量显示写法
            /// （`["Armour 1", "Long Range"]` / `["Blast 2"]` 这种，`_traitValue > 0` 才带数字）。
            ///
            /// 🔴 **读 `CardTraits`，⛔ 不读 `ShowTraits`** —— 后者非空只 **32** 张，而与它平行的
            /// `CardTraits` 有 **650** 张，且 `A1748` 那 18 张的 `ShowTraits` **全是空**
            /// ⇒ 对着 `ShowTraits` 判会把 633 张误判成「没挂 trait」。
            ///
            /// ⚠️ **本列不喂引擎**（纯落值）：它**包含** `Frenzied` / `Rage` / `Unstoppable` 三个
            /// **`CardDef.Prefixes` 未登记**的词（那三个**不进 `keywords`** —— 写了会被
            /// `KeywordTable.Normalize` 返回 null 后静默丢弃）。要看「引擎真正认得的」
            /// 请读 `keywords`；要看「**原版挂了什么**」读本列。
            ///
            /// ⚠️ `cardTraits` 与 `keywords` 的关系 = **权威序第 1 层**（主对话已裁③）：
            /// 真值 `CardTraits` **只补空**（OCR 那格为空时才填、⛔ 不覆盖已有值），
            /// 上面还压着 OCR 源表 / `cardface_fixes.json` / `fix_own_armour` / `_manual_keywords`。
            /// 生成器跑完会打出「填了多少 / 被更高层换掉多少 / 因引擎不认跳过多少」三个数。
            /// 🆕 **2026-10-23（`新账-95`）**：**全仓唯一的读点 = `Editor/RuleEngineTest.cs`** 那条
            /// 「`cardTraits` 里不许出现 `Frenzied` / `Rage`」的断言（它读的是**原始 DTO**）。
            /// 运行期引擎**一个读点都没有** ⇒ 它**不算死列**（有消费者），但也别拿它当引擎输入。</summary>
            public string[] cardTraits;

            // ================================================================================
            //  🆕 **v9（2026-10-15 · P2 相 `A1799`）**：其余「原版有真值、我们在猜」的卡 SO 字段
            //
            //  🔴 **这一族【只落值】—— 目前没有任何消费者**（机制/接线另立一笔）：
            //     判据原文 → `资料/普查产出_第二十七会话/allcards_未挖字段盘点.md` §2 ·
            //                `资料/普查产出_第三十一会话/Q_A1770与A1799判据.md`「A1799」那张表。
            //     `attackDamageType` 尤其要点名：它是**补机制不是换数据**
            //     （我们引擎**整条没有伤害类型概念** —— `RuleCore.cs` 自己写着）。
            //
            //  🆕 **2026-10-23（引擎线 `新账-95`）：死列的【读取守卫】** ——
            //     审计 `资料/普查产出_1023c/引擎深审C_数据吻合.md` §二 现读：这一族 **0 个运行期消费者**。
            //     `cardTraits` / `useOverDraw` / `fullHeroSpriteReference` **不在死列里**
            //     （前者被 `RuleEngineTest` 的「不许出现 Frenzied/Rage」那条读、后两者已接线/生成期用）。
            //     其余 9 列**加了 `[Obsolete]`**：谁在 C# 里读它 → **编译期就出声**（一句点名原因的警告），
            //     ⛔ 不再「看着像有值、其实没人用」。⚠️ `[Obsolete]` **不影响** `JsonUtility` 反序列化
            //     （那是反射），表里的值照旧读得进来 —— 它只是**读点守卫**，不是把列删掉。
            //
            //  🔴 **缺值 = `-1`（整数）/ `""`（字符串）** —— 与 `inventoryOptions` / `initiative`
            //     同一条口径：真值快照是 2025-07-30 的、比现池旧，现池 **118 张**新卡快照里根本没有。
            //     ⛔ **别把 `-1` 当成 `0`** —— 这一族里 `0` 多半是**合法取值**：
            //       · `attackDamageType = 0` 是 `DamageType.Normal`（54 张，多为督军）
            //       · `useOverDraw = 0` 是「立绘不溢出卡框」
            //       · `cantBeRandomlyCreated = 0` 是「可以被随机生成」
            //       · `targeted = 0` 是「不需要选目标」
            // ================================================================================

            /// <summary>`DamageType` 枚举（`Assembly-CSharp/DamageType.cs`）：`0 Normal` / `4 Attack`。
            /// 🔴 我们引擎**没有伤害类型概念** ⇒ 本列只落值。原版消费点 =
            /// `GameStaticData__IsVulnerableDamage`（`(uint)(t - 7) < 2`）+ `CardScript._ReceiveDamage:189-197`。</summary>
            [System.Obsolete("死列（新账-95）：attackDamageType 没有运行期消费者 —— 引擎整条没有伤害类型概念。"
                           + "读它之前先立机制（原版读点见本字段 doc），否则就是把「有值」当「在用」。")]
            public int attackDamageType;

            /// <summary>原版 `RawCardScript.cardShortId`（1…914、860 种、294 张为 0）。
            /// 🔴 **⛔ 不许拿它当卡 id 换** —— `Q_A1770与A1799判据.md` 现核：它与 `A970` 那套
            /// （= PnP 图文件名编号）**0/763 重合**，按名引用在 `decomp_full` 全库 **0 处**。
            /// 实测形状 = 「标的是**哪张卡**、不是哪个卡 SO」（本体与 `(tutorial)` 副本同号）。</summary>
            [System.Obsolete("死列（新账-95）：cardShortId 没有运行期消费者，且 ⛔ 不许当卡 id 用（与 A970 那套 0/763 重合）。")]
            public int cardShortId;

            /// <summary>立绘**溢出卡框**的逐卡 opt-in（0/1，缺值 `-1`）。原版消费点 =
            /// `CardDisplayWindow__ChangeCardPosition.c:75-79` → `BasicCardUI.SetImageOverDraw`。
            /// ⚠️ `CardPresentation/Core/CardView.cs` 里那句「值在服务器、我们拿不到」**已过期**（铁律 5）。
            /// 🆕 2026-10-22（卡牌线 `T04`）：**已接线** —— `CardDef.UseOverDraw`（`0` ⇒ false，其余 ⇒ true）
            /// → `CardView.UseOverDraw` → `ArtFrontVisible` 的最内层门控。</summary>
            public int useOverDraw;

            /// <summary>「不能被随机生成」（0/1）。⚠️ **读取点没找到**（`allcards_未挖字段盘点.md` §四）
            /// ⇒ 它是不是 `CreatePool` 该用的闸门**未证** ⇒ 本列只落值、⛔ 不接。</summary>
            [System.Obsolete("死列（新账-95）：cantBeRandomlyCreated 没有运行期消费者 —— 原版读取点未找到（判据不足），"
                           + "⛔ 别自己编语义（比如拿它当 CreatePool 的闸）。")]
            public int cantBeRandomlyCreated;

            /// <summary>`CardExpansionType`（10 LevaithanDrop1 … 80 DarkAngelsDrop1）；`0` = 基础。
            /// 用途（商店 / 卡包 / 收藏筛选）另接。</summary>
            [System.Obsolete("死列（新账-95）：expansionPack 没有运行期消费者（商店/卡包/收藏筛选还没接这一列）。")]
            public int expansionPack;

            /// <summary>卡名的 **I2 键后缀**（`Card_Name/<值>`）⇒ 这 70 张的**显示名 ≠ `cardName`**。
            /// ⚠️ **本列在现池全空** —— 那 70 条**全在 GUID 卡上**，而它们在 P1 相没进我们池（Q 现核）。</summary>
            [System.Obsolete("死列（新账-95）：refNameId 没有运行期消费者；而且本列在现池全空（那 70 条全在 GUID 卡上、没进池）。")]
            public string refNameId;

            /// <summary>卡面「说明行」的 I2 键。现读只有两种：`Item_Description/OffenceCardExplanation`
            /// （进攻卡）与 `…/DefenceCardExplanation`（防御卡）——**中文已在官方表里**。</summary>
            [System.Obsolete("死列（新账-95）：itemDescriptionKey 在**卡表这一层**没有消费者 —— 卡背/卡包文案走的是"
                           + "cosmetic item SO 上的同名字段（`CardPresentation/**` 那几处说的是那个，不是本列）。")]
            public string itemDescriptionKey;

            /// <summary>督军**整幅立绘**引用。有 `m_SubObjectName` 时写它（实测是
            /// `UI_Deck_Warlord_<卡名>` 这种**资产名**），否则写 `m_AssetGUID`（32 位十六进制）。
            /// 🔴 **GUID 那条路解析不出文件名**：`guid_map.tsv` 那 9 类引用里**没含它**（命中 0/59）。</summary>
            public string fullHeroSpriteReference;

            /// <summary>督军「事件」立绘的 sprite 名。⚠️ 实测 **44 张值全同** =
            /// `Warlord_events_Loken` ⇒ 多半是**图集名**、单图按 id 取（语义**未查清**）⇒ 只落值。
            /// 🔴 **2026-10-23 就地订正（引擎线 `新账-95` · 铁律 5）**：上面那个「44 张」是**旧快照**口径 ——
            /// 旧快照 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 现读是 **54 张**
            /// （值仍全同 `Warlord_events_Loken`）；而**当前 build 快照**
            /// （`资料/普查产出_1018/allcards_1350/cards_raw.json`，1,351 张）**这一列全空**，
            /// 我们卡池现读 **0 / 1,127**（`资料/普查产出_1023c/引擎深审C_数据吻合.md` P2·8）。
            /// ⇒ 本列今天**没有任何真值**，语义更无从查起；⛔ 别拿「44 张」当现读数。</summary>
            [System.Obsolete("死列（新账-95）：eventSpriteName 没有运行期消费者，且当前快照这一列全空（0/1127）。")]
            public string eventSpriteName;

            /// <summary>这张牌**要选目标**（0/1）。⚠️ 我们**从效果解析推**（`SimpleAI` 引
            /// `EntityScript__get_targeted`）—— 两者是否等价**未核** ⇒ 只落值、⛔ 不替换现有推断。</summary>
            [System.Obsolete("死列（新账-95）：targeted 没有运行期消费者 —— 引擎从效果解析推「要不要选目标」"
                           + "（`EffectText.PickTarget`），本列只落值。")]
            public int targeted;

            // ================================================================================
            //  🆕 **v10（2026-10-19 · 引擎线波 2b `A1959`/`A1960`）**：原版 `RawCardScript.choicesDefinition`
            //  （`TargetCriteria`，`dump.cs:23341` @0x2A0）—— 「**这张卡要做一次选牌**」的规格。
            //
            //  🔴 **判据**：`BattleManager__GetChoiceFullPool.c:36-49` ——
            //     `targetsAffected == 0xb4(180 = TargetsAffected.choiceResolution)` ⇒ 候选池 =
            //     `filterSpecificCards`（@0x70）；**为空时回落 `BattleManager+0x308`**（`pooledAbilityData`，
            //     我们**没有**那一格）；`maxTargets`（@0x40）在 `GetChoiceOptions.c:40-45` 被夹到候选张数。
            //
            //  🔴 **缺值 = `-1`（整数）/ 空表** —— 真值快照比现池旧的那批卡没有这张 SO。
            //     ⛔ 别把 `-1` 当成 `0`：`targetsAffected = 0` 是合法取值 `any`、
            //     `maxTargets = 0` 是合法的「不摆」。
            // ================================================================================

            /// <summary>原版 `choicesDefinition.targetsAffected`（`TargetsAffected` 枚举）。</summary>
            public int choicesTargetsAffected;
            /// <summary>原版 `choicesDefinition.maxTargets`（面板摆几张）。</summary>
            public int choicesMaxTargets;
            /// <summary>原版 `choicesDefinition.filterSpecificCards` 解出来的卡 id 表
            /// （PPtr → truth `path_id` → `uniqueId`）。⚠️ 里面的 id **未必在本池**
            /// （变体卡 SO）—— 消费侧如实跳过，别当数据错。</summary>
            public string[] choicesSpecificCardIds;

            /// <summary>🆕 v11（2026-10-19 · `E12`）：**逐候选动画时间**。
            /// 见 <see cref="CardDef.CardAnimTime"/> 的 doc（字段一一对应）。
            /// ⚠️ 缺值 = `null` / 空数组；旧表（&lt; v11）读进来是 `null` ⇒ 下面那条 version 守卫会出声。</summary>
            public AnimTimeDto[] animTimes;

            /// <summary>🆕 v13（2026-10-21 · 引擎线 `T03·c` 批 B）：逐触发关键词的**原版
            /// `CardAbility.priority`**（`ActionPriority`：0 Player / 1 Instant / 2 Interrupt /
            /// 3 DelayedInstant），喂 <see cref="CardDef.TriggerPriority(string)"/>。
            ///
            /// 🔴 **只出现 != 1 的那几条**（缺键 = 原版默认 **1**，见 `CardDef.DefaultTriggerPriority`）——
            /// 2026-10-21 现读全池 1127 张**只有 4 张**有本键：
            /// `ASH53` spiritstone 3 · `SAU13`/`SAU70` artifice 3 · `UM37` codex 3。
            /// ⇒ 对**本列**而言「这一列不存在」与「这张卡全是默认值」是同一件事；但旧表（&lt; v13）
            /// **整列没有** ⇒ 下面的 version 守卫照样出声（别让「表是旧的」被读成「全池都是默认」）。
            ///
            /// ⚠️ `keyword` = **引擎侧触发关键词**（`artifice` / `codex` / `spiritstone` …，21 个），
            /// ⛔ 不是原版枚举名 —— 映射表在 `工具/gen_cards_engine.py` 的 `ABILITY_TRIGGER_KEYWORD`。
            /// 数据源 = 真值 SO 快照 `cardAbilities[].priority`（与 `cards_truth.json` 同一份数据）。</summary>
            public TriggerPriorityDto[] triggerPriority;

            /// <summary>🔴🆕 v18（2026-10-23 · 引擎线 `P1·①`）：逐条 ability 的**原版
            /// `CardAbility.priority`，键 = 原版 `AbilityTrigger` 枚举名** —— 补的是
            /// `triggerPriority`（键 = 引擎关键词，21 个）覆盖不到的那一半：
            /// `WhenEvent` / `ResolveAtTurn` / 打出族**没有引擎关键词**，手上只有原版 trigger 名。
            /// 喂 <see cref="CardDef.AbilityTriggerPriority(string)"/> → `ActionPump.ListenerLane`。
            ///
            /// 🔴 **只出现 != 1 的那几条**（缺键 = 原版默认 **1** = Instant）。
            /// 2026-10-23 现读全池 1127 张**只有 7 张**有本键，其中 3 张是本列新增：
            /// `DA12` OnCollectQuestPoints 2 · `BL72` UnitReceivesMarkOfChaos 2 · `DA14` FollowUpResolution 0。
            /// ⚠️ 与 `triggerPriority` **不是一套主键**（枚举名 vs 引擎关键词），⛔ 别把两张表合并
            /// —— 语义分叉的判据 → `CardDef.AbilityTriggerPriority` 那一节。
            /// 数据源 = 真值 SO 快照 `cardAbilities[].priority`（与 `triggerPriority` 同源、不同投影）。</summary>
            public AbilityTriggerPriorityDto[] abilityTriggerPriority;

            /// <summary>🆕 v14（2026-10-22 · 引擎线 `W63` · `T03·c/d`）：逐条 ability 的**原版
            /// `CardAbility.abilityContext`**（载体此刻必须待在哪个区域），喂
            /// <see cref="CardDef.AbilityContextOfTrigger"/> / <see cref="CardDef.AbilityContextOfKeyword"/>。
            ///
            /// 🔴 **只落 `context != 0` 的行**（`0` = `Play`，占绝大多数；缺行与写 0 语义相同）。
            /// 六档语义 / skip 表 / `-1` 契约 → `CardDef` 里本节那段注释（⛔ 别在这里抄第二份）。
            /// ⚠️ `name` = 原版 `AbilityTrigger` 枚举名；`keyword` = 引擎触发关键词，
            /// **只在生成器 `ABILITY_TRIGGER_KEYWORD` 有映射时出现**（⛔ 只按 keyword 落会漏掉
            /// `UnitAttack` 这一族）。旧表（&lt; v14）整列没有 ⇒ 下面的 version 守卫出声，
            /// 访问器一律 `-1`（未知）。
            /// 数据源 = 真值 SO 快照 `cardAbilities[].abilityContext`。</summary>
            public AbilityContextDto[] abilityContext;

            /// <summary>🆕 v15（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16`）：原版**结构化条件**
            /// `TargetCriteria.conditionsFilter` ＋ `excludeCondition`，喂
            /// <see cref="CardDef.ConditionOf"/> / <see cref="CardDef.TryGetCondition"/>。
            ///
            /// 🔴 **只落 `cond != 0` 的行**（`0` = `UnitConditions.none` = 没有条件；缺行也是这样）。
            /// 十个位置（`role`）/ `ab`/`lg` 位置编码 / `count` 那三个参数 / `-1` 契约 →
            /// <see cref="CardDef.ConditionOf"/> 那一节的长注释（⛔ 别在这里抄第二份）。
            /// ⚠️ 旧表（&lt; v15）**整列没有** ⇒ 下面的 version 守卫出声，
            /// `CardDef.ConditionOf` 一律 `-1`（**未知**，⛔ 不是「没有条件」）。
            /// 数据源 = 真值 SO 快照 `full_raw` 的十处 `TargetCriteria`。</summary>
            public UnitConditionDto[] unitConditions;

            /// <summary>🆕 v17（2026-10-23 · 引擎线 · `B-16` 残余）：**`cardAbilities[].trigger` 的
            /// 全量集合**（`sorted(set(...))`），喂 <see cref="CardDef.AbilityTriggerPresence"/> →
            /// `ConditionEval` 的 `241 hasUseSpiritStone` / `242 hasUseFaith`。
            ///
            /// 判据 = `FilterMethods__MeetsConditionCriteria.c:286-293` →
            /// `RawCardScript__HasAbilityType(raw, 600/605)` → `RawCardScript__HasAbilityType.c:16-38`
            /// （遍历 `rawCardData.cardAbilities`，**只看 `trigger`**，不看 `abilityLogic` 空不空）。
            ///
            /// 🔴 **每张卡都落这一列**（没 ability 的卡落 `[]`）—— 「缺列」与「空数组」必须分得开：
            /// 缺列 = 真值快照里没有这张卡（**未知**，访问器回 `-1`）；
            /// 空数组 = **明确没有任何 ability**（`HasAbilityType` ⇒ 假）。
            /// ⚠️ 与 `abilityContext` **不是**同一份数据：那个只落 `context != 0` 的行 ⇒
            /// 拿它当 trigger 表会漏掉 `context == 0` 的 ability（`241/242` 因此一直判不了）。</summary>
            public int[] abilityTriggers;

            /// <summary>🆕 v17（2026-10-23 · 引擎线 · 战斗审计 `B-17`）：**逐 ability / 逐 logic 的
            /// `targetsAffected`**（原版 `AbilityLogic.targetCriteria.targetsAffected`），喂
            /// <see cref="CardDef.AbilityTargets"/> → `EffectText.AssignAbilityTargets`。
            ///
            /// 形状 = `[{"trigger":…, "ta":[…]}]`，**按 `cardAbilities` 顺序**；`ta[j]` 对应
            /// `cardAbilities[i].abilityLogic[j]`。
            /// 🔴 **每张卡都落**（没 ability = `[]`）；缺列 = 真值里没有这张卡 = **未知**。
            /// ⚠️ 非整数落 `-1`（`0` 是合法的 `any`）。判据 → `AbilityLogic__GetTargets.c` 的 `+0x10` 分发。</summary>
            public AbilityTargetDto[] abilityTargets;
        }

        /// <summary>🆕 v17（2026-10-23 · 引擎线 · `B-17`）：`abilityTargets` 的一行
        /// （**字段名故意与 JSON 列名逐字一致**，`JsonUtility` 直读）。
        /// 语义 → <see cref="CardDef.AbilityTargetSpec"/>。</summary>
        [System.Serializable]
        public class AbilityTargetDto
        {
            /// <summary>`-1` = JSON 里没有 `trigger`（坏值；本列只作可读性，消费端按**下标**对齐）。</summary>
            public int trigger = -1;
            /// <summary>逐 `abilityLogic` 的 `targetsAffected`；`-1` = 该格坏值/缺键。</summary>
            public int[] ta;
        }

        /// <summary>🆕 v15（2026-10-22 · 引擎线 `W64`）：`unitConditions` 的一行
        /// （**字段名故意与 JSON 列名逐字一致**，`JsonUtility` 直读）。
        /// 语义 → <see cref="CardDef.ConditionSpec"/>。
        ///
        /// 🔴🆕 **`ab` / `lg` / `cond` 都用 `-1` 以外的哨兵，⛔ 别退回 `0`。**
        /// 为什么：**这三个字段的 `-1` 都是合法取值** ——
        ///   · `ab = -1` = `activeAbility`（`-2` = `alternativeAbility`、`-3` = 卡级）；
        ///   · `lg = -1` = 「直接挂在 ability / 卡对象上、不在 `abilityLogic` 里」；
        ///   · `cond = -1` 本身是 `ConditionUnknown`（但这一列只落非 0 行 ⇒ 缺键必须能被认出来）。
        /// 缺键若静默读成 `0`：`ab` 会落到**第 0 条 ability**（错卡错位）、`lg` 会落到**第 0 条 logic**、
        /// `cond` 会落成 `none`（= 「没有条件」）—— 三种都是**静默错**。
        /// 做法与 `TriggerPriorityDto` / `AbilityContextDto` 同款 —— 字段初值取 `Missing*` 哨兵：
        /// `JsonUtility.FromJson` 用默认构造造对象、**只覆盖 JSON 里出现的字段**
        /// ⇒ `MapUnitConditions` 能认出「缺键」并**出声 + 丢弃**。
        /// ⚠️ `MissingIndex` 取 `-99`（不是 `-1`）—— 因为 `-1/-2/-3` 都是合法位置。
        /// </summary>
        [System.Serializable]
        public class UnitConditionDto
        {
            /// <summary>「JSON 里没有 `ab` / `lg` 这个键」的哨兵值（`-1/-2/-3` 都是合法位置 ⇒ 不能拿它们当缺值）。
            /// ⚠️ 唯一来源 = <see cref="CardDef.ConditionSpec.MissingIndex"/>（探针只编 `Core/`，也引它）。</summary>
            public const int MissingIndex = CardDef.ConditionSpec.MissingIndex;
            /// <summary>「JSON 里没有 `cond` 这个键」的哨兵值（`0` 是合法值 `none`，⛔ 不能拿它当缺值）。</summary>
            public const int MissingCondition = -1;
            /// <summary>`cardAbilities` 下标 / `-1` active / `-2` alternative / `-3` 卡级。</summary>
            public int ab = MissingIndex;
            /// <summary>子数组下标；`-1` = 直接挂在对象上。</summary>
            public int lg = MissingIndex;
            /// <summary>十个位置之一（`CardDef.ConditionRoleNames`）。</summary>
            public string role;
            /// <summary>原版 `UnitConditions` 数值（非 0）。</summary>
            public int cond = MissingCondition;
            /// <summary>`UnitConditions` 枚举名（**只给人看**；⚠️ 名字 ≠ 语义）。</summary>
            public string name;
            /// <summary>原版 `excludeCondition`（`0/1`；语义 = 取反）。</summary>
            public int ex;
            /// <summary>`BoardCriteria.countOptions`（只有 `role == "count"` 有）。</summary>
            public int opt = MissingCondition;
            /// <summary>`BoardCriteria.triggerIf`（`StatFilterOptions`；只有 `role == "count"`）。</summary>
            public int op = MissingCondition;
            /// <summary>`BoardCriteria.referenceValue`（只有 `role == "count"`；
            /// JSON 键叫 `refValue` —— `ref` 是 C# 关键字，落不了同名字段）。</summary>
            public int refValue = MissingCondition;
            /// <summary>🆕 v16（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16` 缺口）：**被数集合**
            /// （原版 `BoardCriteria.countCriteria` ＋ `playerTurn`）—— **只有 `role == "count"` 的行有**。
            /// `null` = 没落/旧表（&lt; v16）⇒ <see cref="CardDef.ConditionSpec.CountSet"/> 也是 null
            /// （**未知**，⛔ 不是「默认集合」）。
            /// 逐字段语义 → <see cref="CardDef.CountSetSpec"/> 的 doc（⛔ 别在这里抄第二份）。</summary>
            public CountSetDto countSet;
        }

        /// <summary>🆕 v16：`unitConditions[].countSet` 的传输形状（**字段名与 JSON 键名逐字一致**，
        /// `JsonUtility` 直读；语义 → <see cref="CardDef.CountSetSpec"/>）。
        ///
        /// 🔴 **缺键哨兵 = `MissingIndex`（-99）**，⛔ 不是 `-1` / `0`：
        /// 这些字段的取值域里 `0` 到处都是合法值（`TargetsAffected.any` / `false` / `countOptions` …），
        /// 而 `-1` 是 `CardDef` 那一层「未知」的哨兵。用 `-99` 才能把「JSON 里没这个键」与
        /// 「值是 0」分开（同 `ab`/`lg` 的理由）。
        /// ⚠️ 两个引用列表 `null` = 缺键/未知；`[]` = 明确的空表。
        /// </summary>
        [System.Serializable]
        public class CountSetDto
        {
            /// <summary>缺键哨兵 —— **唯一来源**是 <see cref="UnitConditionDto.MissingIndex"/>（`-99`）。</summary>
            const int MissingIndex = UnitConditionDto.MissingIndex;
            /// <summary>原版 `BoardCriteria.playerTurn`（`PlayersAffected`：0 Both / 10 Self / 20 Enemy）。</summary>
            public int playerTurn = MissingIndex;
            /// <summary>原版 `TargetCriteria.targetsAffected`（`TargetsAffected`）。</summary>
            public int targetsAffected = MissingIndex;
            /// <summary>原版 `TargetCriteria.chosenArmy`（`CardArmy`）。</summary>
            public int chosenArmy = MissingIndex;
            /// <summary>原版 `TargetCriteria.armyFilter`（`ArmyFilterOptions`）。</summary>
            public int armyFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.keywordFilter`（`Keyword`）。</summary>
            public int keywordFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.traitsFilter`（`DefinedTrait`）。</summary>
            public int traitsFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.maxTargets`。</summary>
            public int maxTargets = MissingIndex;
            /// <summary>原版 `TargetCriteria.targetSelectMethod`（`TargetSelectMethod`）。</summary>
            public int targetSelectMethod = MissingIndex;
            /// <summary>原版 `TargetCriteria.attackFilter`。</summary>
            public int attackFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.attackFilterOption`（`StatFilterOptions`）。</summary>
            public int attackFilterOption = MissingIndex;
            /// <summary>原版 `TargetCriteria.currentHealthFilter`。</summary>
            public int currentHealthFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.currentHealthFilterOption`。</summary>
            public int currentHealthFilterOption = MissingIndex;
            /// <summary>原版 `TargetCriteria.manaCostFilter`。</summary>
            public int manaCostFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.manaCostFilterOption`。</summary>
            public int manaCostFilterOption = MissingIndex;
            /// <summary>原版 `TargetCriteria.rarityFilter`。</summary>
            public int rarityFilter = MissingIndex;
            /// <summary>原版 `TargetCriteria.rarityFilterOption`。</summary>
            public int rarityFilterOption = MissingIndex;
            /// <summary>原版 `TargetCriteria.spellType`（`SpellType`）。</summary>
            public int spellType = MissingIndex;
            /// <summary>原版 `TargetCriteria.friendlyUnits`（0/1）。</summary>
            public int friendlyUnits = MissingIndex;
            /// <summary>原版 `TargetCriteria.enemyUnits`（0/1）。</summary>
            public int enemyUnits = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeHero`（0/1）。</summary>
            public int includeHero = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeMinions`（0/1）。</summary>
            public int includeMinions = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeSpells`（0/1）。</summary>
            public int includeSpells = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeWhispers`（0/1）。</summary>
            public int includeWhispers = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeWeapons`（0/1）。</summary>
            public int includeWeapons = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeRemnants`（0/1）。</summary>
            public int includeRemnants = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeHeroParts`（0/1）。</summary>
            public int includeHeroParts = MissingIndex;
            /// <summary>原版 `TargetCriteria.includeSelf`（0/1）。</summary>
            public int includeSelf = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeNotSelf`（0/1）。</summary>
            public int excludeNotSelf = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeSimilarCards`（0/1）。</summary>
            public int excludeSimilarCards = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeTargetted`（0/1）。</summary>
            public int excludeTargetted = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeFriendlyHero`（0/1）。</summary>
            public int excludeFriendlyHero = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeEnemyHero`（0/1）。</summary>
            public int excludeEnemyHero = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeRace`（0/1）。</summary>
            public int excludeRace = MissingIndex;
            /// <summary>原版 `TargetCriteria.excludeTrait`（0/1）。</summary>
            public int excludeTrait = MissingIndex;
            /// <summary>原版 `TargetCriteria.selectFromChooseResolution`（0/1）。</summary>
            public int selectFromChooseResolution = MissingIndex;
            /// <summary>原版 `TargetCriteria.filterSpecificCards` 解出来的卡 id（`null` = 缺键）。</summary>
            public string[] filterSpecificCards;
            /// <summary>原版 `TargetCriteria.excludeEnchantments` 解出来的卡 id（`null` = 缺键）。</summary>
            public string[] excludeEnchantments;
        }

        /// <summary>🆕 v11：`animTimes` 的传输形状（字段名**故意与 JSON 列名一致**，JsonUtility 直读）。
        /// 语义 → <see cref="CardDef.CardAnimTime"/>。</summary>
        [System.Serializable]
        public class AnimTimeDto
        {
            public string a;      // CardAnim 资产名
            public string n;      // prefab 名
            public int tg;        // 原版 CardAbility.trigger 数值（-1 = activeAbility）
            public string tn;     // 枚举名
            public int rc;        // randomAbilityChoice
            public string o;      // 原版字段路径的索引三元组（i.j.k / -.j.k）
            public float d;       // vfxDelayTime
            public float sd;      // startDelay
            public float s;       // timeAtStartPos
            public float m;       // timeMoving
            public float e;       // timeAtEndPos
            public float u;       // Duration = m + s + e + d
            public float t;       // 组内起点偏移（前面各候选 u 之和）
        }

        /// <summary>🆕 v12（2026-10-19 · `E12` · `A2142`）：**天赋名 → 天赋卡 id** 真值表的一行。
        /// `n` = `CreatePool.Norm` 归一后的名字（两侧同一份归一）。</summary>
        [System.Serializable]
        public class TalentNameIdDto { public string n; public string id; }

        /// <summary>🆕 v13（2026-10-21 · 引擎线 `T03·c` 批 B）：`triggerPriority` 的一行
        /// （**字段名故意与 JSON 列名逐字一致**，`JsonUtility` 直读）。
        /// `keyword` = 引擎侧触发关键词；`priority` = 原版 `CardAbility.priority`（0..3）。
        /// 语义 → <see cref="CardDef.TriggerPriority(string)"/>。
        ///
        /// 🔴🆕 **2026-10-21（审查 `W61b` 第 7 条）：`priority` 用 `-1` 哨兵，⛔ 别退回 `0` 默认。**
        /// 为什么：`int` 的默认值 `0` = **`Player` 道**（合法取值）⇒ 「JSON 里没写 `priority` 这个键」
        /// 与「写了 `priority: 0`」**在 DTO 上分不开** ⇒ 缺键那一行会**静默走最后才结算的 Player 道**，
        /// 而探针（`工具/ruleprobe/Program.cs`）对同一条数据是「报 1 条形不全并跳过」——
        /// 两侧口径不一致（原版对非法 priority 是 `LogError` 后丢弃：
        /// `BattleManager__AddAutoActionToQueue.c:226-228`）。
        /// 做法 = 字段初值 `-1`：`JsonUtility.FromJson` 用默认构造造对象、**只覆盖 JSON 里出现的字段**
        /// （`FromJsonOverwrite` 的语义：没出现的字段保持原值）⇒ 缺键那一行读出来就是 `-1`
        /// ⇒ `MapTriggerPriority` 能认出「缺键」并**出声 + 丢弃**（与探针同口径）。
        /// 🧨 若哪天 `-1` 不再保留（换了序列化器），`RuleEngineTest.W61T28` 会红 —— 那一轮就改走原始文本扫描。
        /// </summary>
        [System.Serializable]
        public class TriggerPriorityDto
        {
            /// <summary>「JSON 里没有 `priority` 这个键」的哨兵值（见类注释）。</summary>
            public const int MissingPriority = -1;
            public string keyword;
            public int priority = MissingPriority;
        }

        /// <summary>🔴🆕 v18（2026-10-23 · 引擎线 `P1·①`）：`abilityTriggerPriority` 的一行
        /// （**字段名故意与 JSON 列名逐字一致**，`JsonUtility` 直读）。
        /// `name` = 原版 `AbilityTrigger` 枚举名（**唯一按键**）；`priority` = 原版 `CardAbility.priority`。
        /// 语义 → <see cref="CardDef.AbilityTriggerPriority(string)"/>。
        ///
        /// 🔴 **`trigger` 与 `priority` 都用 `-1` 哨兵**（与 `AbilityContextDto` / `TriggerPriorityDto` 同一条理由）：
        /// `trigger = 0` 是 `ThisCardPlayed`、`priority = 0` 是 **Player 道**，两者都是**合法取值**
        /// ⇒ 「JSON 里没写这个键」与「明确写 0」在 DTO 上必须分得开，缺键由
        /// `MapAbilityTriggerPriority` 出声丢弃（⛔ 不许静默落进 Player 道）。
        /// </summary>
        [System.Serializable]
        public class AbilityTriggerPriorityDto
        {
            /// <summary>「JSON 里没有 `trigger` 这个键」的哨兵值。</summary>
            public const int MissingTrigger = -1;
            /// <summary>「JSON 里没有 `priority` 这个键」的哨兵值（同 `TriggerPriorityDto.MissingPriority`）。</summary>
            public const int MissingPriority = -1;
            /// <summary>原版 `CardAbility.trigger`（`AbilityTrigger` 枚举数值；`0` = `ThisCardPlayed` 合法）。</summary>
            public int trigger = MissingTrigger;
            /// <summary>原版 `AbilityTrigger` **枚举名**（`FollowUpResolution` / `OtherUnitDeath` …）。</summary>
            public string name;
            /// <summary>原版 `CardAbility.priority`（0 Player / 1 Instant / 2 Interrupt / 3 DelayedInstant）。</summary>
            public int priority = MissingPriority;
        }

        /// <summary>🆕 v14（2026-10-22 · 引擎线 `W63` · `T03·c/d`）：`abilityContext` 的一行
        /// （**字段名故意与 JSON 列名逐字一致**，`JsonUtility` 直读）。
        /// 语义（六档 / skip 表 / `-1` 契约）→ <see cref="CardDef.AbilityContextOfTrigger"/>。
        ///
        /// 🔴🆕 **`trigger` 与 `context` 都用 `-1` 哨兵，⛔ 别退回 `0`。**
        /// 为什么：**两者的 `0` 都是合法取值** —— `trigger = 0` 是 `ThisCardPlayed`、
        /// `context = 0` 是 `Play`（「必须活着在场」）⇒「JSON 里没写这个键」与「明确写 0」
        /// 在 DTO 上分不开。缺键若静默读成 0：`context` 那条会把**死亡族 / 墓地族**的触发
        /// **掐死**（0 = 载体必须活着在场），`trigger` 那条会把「没落 trigger」伪装成一条
        /// 合法的 `ThisCardPlayed` 行。做法与 `TriggerPriorityDto` 同款 —— 字段初值 `-1`：
        /// `JsonUtility.FromJson` 用默认构造造对象、**只覆盖 JSON 里出现的字段**
        /// ⇒ `MapAbilityContext` 能认出「缺键」并**出声 + 丢弃**。
        /// </summary>
        [System.Serializable]
        public class AbilityContextDto
        {
            /// <summary>「JSON 里没有 `trigger` 这个键」的哨兵值（见类注释）。</summary>
            public const int MissingTrigger = -1;
            /// <summary>「JSON 里没有 `context` 这个键」的哨兵值（见类注释）。
            /// ⚠️ 它同时是 `CardDef.AbilityContextUnknown` 的值 —— 语义正好对齐（`-1` = 不可用）。</summary>
            public const int MissingContext = -1;
            /// <summary>原版 `CardAbility.trigger`（`AbilityTrigger` 枚举数值；`0` = `ThisCardPlayed` 合法）。</summary>
            public int trigger = MissingTrigger;
            /// <summary>原版 `AbilityTrigger` **枚举名**（`UnitAttack` / `Strike` / `UseFaith` …）。</summary>
            public string name;
            /// <summary>引擎侧触发关键词（`KeywordTable.*`）；**只在生成器有映射时出现**（无映射 = `null`）。</summary>
            public string keyword;
            /// <summary>原版 `AbilityContext` 数值：`0/10/20/30/40/50`。</summary>
            public int context = MissingContext;
        }

        /// <summary>🆕 2026-10-21（引擎线 `T03·b`）：`skipDraws` 的一行
        /// （**字段名故意与 JSON 列名逐字一致**，`JsonUtility` 直读）。
        /// 语义 → <see cref="CardDef.SkipDrawSpec"/>。
        /// 🔴 **本列【不】提卡表 version** —— 与 v9..v13 那几列不同：「缺列」与「空表」语义相同
        /// （都 = 没有卡带这条效果），所以旧表读进来**不需要出声**。理由写在
        /// `工具/gen_cards_engine.py` 的 `INITIAL_MODIFIER_COLS` 上面那一段。
        /// ⚠️ `limited` **显式落成 0/1**（`JsonUtility` 不写 `bool` 序号时默认 `false`，
        /// 而原版 `HandEffect.limitedUses` 的语义是「真 = 限次」⇒ 少写这一列会把限次效果
        /// **静默变成永久**，所以生成器**永远显式写它**）。</summary>
        [System.Serializable]
        public class SkipDrawDto
        {
            public string keyword;   // 引擎触发关键词（`KeywordTable.*`）
            public int uses;         // 原版 `HandEffect.numberOfUses // +0x2C`
            public int limited;      // 原版 `HandEffect.limitedUses // +0x28`（1 = 限次）
            public string source;    // 原文（日志用）
        }


        /// <summary>🆕 **2026-10-18（T09）**：解析结果的缓存。
        /// 🔴 `JsonUtility.FromJson` + 1127 张卡的构造实测 **≈2.4 s**，而全仓有 **216 个调用点**，
        /// 其中 `BattleDriver.MechanismFails` 是**逐卡**调的（卡面 `*` 判据）⇒ 不缓存 = **每张卡 2.4 s**
        /// （T09「首次开窗 48.9 s / 17.3 s」的**唯一根因**；探针实测 `UnimplementedNotes` 单卡首次
        /// **2,477 ms**、同一张卡第二次 **0.23 ms**）。
        /// ⚠️ **每次返回一份新的 `List` 浅拷贝** —— 有调用方会改这个列表（排序 / 筛选 / 增删），
        /// 共享同一份会互相污染（本工程吃过「两处共用一份可变表」的亏）。</summary>
        static List<CardDef> _cache;

        /// <summary>🆕 2026-10-18（T09）：丢掉缓存 —— 编辑器里改完卡表、或自检要重读时用。</summary>
        public static void InvalidateCache() { _cache = null; }

        /// <summary>从 Resources 读卡表。取不到返回空表（不抛异常 —— 让调用方能自己决定怎么报）。
        /// 🔴 **2026-10-18（T09）起有进程级缓存**（见 `_cache` 的说明）；读不到的那一路**不缓存**，
        /// 这样补上资源后下一次调用还能读到。</summary>
        public static List<CardDef> Load()
        {
            if (_cache != null) return new List<CardDef>(_cache);
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[RuleEngine] 找不到卡表 Resources/{ResourcePath}.json —— "
                             + "跑一下：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_cards_engine.py");
                return new List<CardDef>();
            }
            _cache = Parse(asset.text);
            return new List<CardDef>(_cache);       // 浅拷贝（见 `_cache` 的说明）
        }

        public static List<CardDef> Parse(string json)
        {
            var list = new List<CardDef>();
            if (string.IsNullOrEmpty(json)) return list;

            var dto = JsonUtility.FromJson<FileDto>(json);
            if (dto == null || dto.cards == null) return list;

            // 🆕 2026-10-19（第五十五会话 · 引擎线 `E12` · `A2142`）：把「天赋名 → 天赋卡 id」真值表
            //   交给 `TalentCardData`（`SpawnTalents` 的按名字兜底**先查它、按 id 取**）。
            //   ⚠️ `JsonUtility` 不支持字典 ⇒ 文件里是 `[{n,id}]`，这里翻成 `KeyValuePair`。
            if (dto.talentNameIds != null && dto.talentNameIds.Length > 0)
            {
                var pairs = new List<KeyValuePair<string, string>>(dto.talentNameIds.Length);
                foreach (var t in dto.talentNameIds)
                    if (t != null && !string.IsNullOrEmpty(t.n) && !string.IsNullOrEmpty(t.id))
                        pairs.Add(new KeyValuePair<string, string>(t.n, t.id));
                TalentCardData.SetNameMap(pairs);
            }

            // ⚠️ 卡表 < v6 就没有 `id` 字段（v6 = 2026-09-13 第三十三轮开始，每张卡带稳定 id）。
            //    🔴 **2026-10-18（`W25`）起缺 id 的卡会被【跳过】**（卡名不再当身份，见下面那段的注释）
            //    ⇒ 读旧表得到的是**空池**（每张卡各报一条错），不是「能跑但身份不稳」。
            if (dto.version < 6)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 6 —— 没有稳定 id，"
                               + "**整份卡表都会被跳过**（卡名不再当身份）。跑一下 gen_cards_engine.py 重生成。");
            // 🆕 2026-10-14（`A1775`）：v8 起才有 `initiative`。**旧表读进来会全是 `0`**，
            //   而 `0` = `Initiative.Undefined` —— 那会让 `FirstSeatRules.ByInitiative` 把
            //   **「没有这个字段」读成「先攻值 = 0」**（静默把「没数据」当数据）。
            //   ⇒ 这里必须出声（判据：`FirstSeatRules` 只认 `-1` 为缺值，`0` 是**合法取值**）。
            if (dto.version < 8)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 8 —— 没有 `initiative` 一列，"
                               + "它会被读成 **0**（`Initiative.Undefined`）而不是「缺值(-1)」⇒ "
                               + "先后手判据拿到的是一堆假值。跑一下 gen_cards_engine.py 重生成。");
            // 🆕 2026-10-15（P2 相 `A1776`/`A1799`）：v9 起才有 `armour` / `cardTraits` 与
            //   `A1799` 那 10 列。**旧表读进来这些列全是 `0`/`null`** —— 而它们里面 `0` 是
            //   **合法取值**（`attackDamageType=0` 是 `Normal`、`useOverDraw=0` 是「不溢出」…）
            //   ⇒ **静默把「这一列不存在」读成「值是 0」**，正是本工程红线里那条。
            //   ⇒ 必须出声（`施工设计 §五·3`：**加列必须 bump 版本并补一条守卫**）。
            if (dto.version < 9)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 9 —— 没有 `armour` / "
                               + "`cardTraits` / A1799 那 10 列（`attackDamageType` · `cardShortId` · "
                               + "`useOverDraw` · `cantBeRandomlyCreated` · `expansionPack` · `refNameId` · "
                               + "`itemDescriptionKey` · `fullHeroSpriteReference` · `eventSpriteName` · "
                               + "`targeted`），它们会被读成 **0/空** 而不是「缺值(-1)/空串」⇒ "
                               + "「这一列不存在」会被静默读成「值是 0」。跑一下 gen_cards_engine.py 重生成。");
            // 🆕 2026-10-19（波 2b `A1959`/`A1960`）：v10 起才有 `choicesTargetsAffected` /
            //   `choicesMaxTargets` / `choicesSpecificCardIds` 三列（原版 `choicesDefinition`）。
            //   **旧表读进来 = `-1`/空**（本文件 `CardDto` 的默认值就是 0/`null`，JsonUtility 不会给 -1）——
            //   ⚠️ 这正是要出声的那一格：`0` 是**合法取值**（`TargetsAffected.any` / `maxTargets = 0` =
            //   「不摆」）⇒ 「这一列不存在」会被静默读成「这张卡定义了一个空选择」。
            if (dto.version < 10)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 10 —— 没有 `choicesDefinition` 三列"
                               + "（`choicesTargetsAffected` / `choicesMaxTargets` / `choicesSpecificCardIds`），"
                               + "它们会被读成 **0/空** 而不是「缺值(-1)」⇒ 选牌面板的逐卡 `maxTargets` 拿不到真值"
                               + "（`A1959`）。跑一下 gen_cards_engine.py 重生成。");
            // 🆕 2026-10-19（第五十五会话 · 引擎线 `E12`）：v11 起才有 `animTimes`（逐候选动画时间）。
            //   **旧表读进来 = `null`**（`JsonUtility` 不会给默认元素）⇒
            //   「没有这一列」会被静默读成「这张卡没有原版动画」—— 而真实区分是
            //   「这张卡**确实**没有 `cardAnimations`」与「表是旧的」。⇒ 出声。
            if (dto.version < 11)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 11 —— 没有 `animTimes` 一列"
                               + "（原版 `cardAbilities[].abilityLogic[].cardAnimations[]` 的逐候选时间），"
                               + "多候选会退回「同一拍全播」且**看不出来是缺数据** ⇒ "
                               + "跑一下 gen_cards_engine.py 重生成（`E12`）。");
            // 🆕 2026-10-19（`E12` · `A2142`）：v12 起才有顶层 `talentNameIds`（天赋名 → 天赋卡 id）。
            //   **旧表读进来 = `null`** ⇒ `TalentCardData.NameMapKeys == 0` ⇒ 兜底退回「按显示名查」，
            //   也就是「卡一改名就静默失配」那条老路 —— 必须出声，别让「缺数据」与「没天赋」混在一起。
            if (dto.version < 12)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 12 —— 没有 `talentNameIds` 真值表"
                               + "（天赋名 → 天赋卡 id）；`RuleCore.SpawnTalents` 的最后兜底会退回"
                               + "「按卡面显示名查」那条路（改名即静默失配）。跑一下 gen_cards_engine.py 重生成（`E12`）。");
            // 🆕 2026-10-21（引擎线 `T03·c` 批 B · 数据层）：v13 起才有逐卡 `triggerPriority`
            //   （原版 `CardAbility.priority`）。**旧表整列没有 = 每张卡都按默认 1 处理** ——
            //   而本列的约定正是「缺键 = 1」⇒ 「表是旧的」与「全池都是默认值」在数据上**分不开**
            //   ⇒ 必须出声，否则非默认那 4 张卡（`ASH53`/`SAU13`/`SAU70`/`UM37`）会被**静默当默认**。
            if (dto.version < 13)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 13 —— 没有 `triggerPriority` 一列"
                               + "（原版 `CardAbility.priority`：0 Player / 1 Instant / 2 Interrupt / 3 DelayedInstant）；"
                               + "所有触发都会退回默认 **1（Instant）** ⇒ 四道动作队列接线之后，"
                               + "`ASH53`/`SAU13`/`SAU70`/`UM37` 这 4 张卡的结算顺序会按默认排。"
                               + "跑一下 gen_cards_engine.py 重生成（`T03·c`）。");
            // 🆕 2026-10-22（引擎线 `W63` · `T03·c/d` · 数据层）：v14 起才有逐条 ability 的
            //   `abilityContext`（原版 `CardAbility.abilityContext // +0x14`）。
            //   **旧表整列没有** ⇒ `CardDef.AbilityContextOfTrigger/OfKeyword` **一律 `-1`（未知）**。
            //   🔴 **必须出声，而且必须说清回退到哪一档**：`0` 是**合法取值**
            //   （`Play` = 载体必须活着在场）⇒ 「缺列」若被读成 0，会把**死亡族 / 墓地族**的触发
            //   **静默掐死**；反过来也⛔ 不能把「缺列」当「全池都是 Play」。未知的处置 =
            //   消费端**回退旧口径**（`QueuedAction.RequireActorInPlay` 那一套），⛔ 不许自己挑一档。
            if (dto.version < 14)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 14 —— 没有 `abilityContext` 一列"
                               + "（原版 `CardAbility.abilityContext`：0 Play / 10 Hand / 20 Deck / 30 Cemetery"
                               + " / 40 InPlayOrDying（**含**待死） / 50 InPlayOrCementery（**不含**待死））；"
                               + "`CardDef.AbilityContextOfTrigger/OfKeyword` 会一律返回 **-1 = 未知**"
                               + "（⛔ 不是 Play(0)：那会把死亡族/墓地族触发掐死）⇒ 消费端按「未知」回退旧口径。"
                               + "跑一下 gen_cards_engine.py 重生成（`W63`）。");
            // 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：v15 起才有逐卡 `unitConditions`
            //   （原版 `TargetCriteria.conditionsFilter` ＋ `excludeCondition`）。
            //   **旧表整列没有** ⇒ `CardDef.ConditionOf` 一律 **`-1`（未知）**。
            //   🔴 **必须出声，而且必须说清回退到哪一档**：`0` 是**合法取值**
            //   （`UnitConditions.none` = 「这一格没有条件」）⇒ 「缺列」若被读成 0，
            //   条件从句会被**静默当成无条件放行**（正是 `B-16` 的错法）；反过来也⛔ 不能把
            //   「缺列」当「全池都有条件」。未知的处置 = 消费端**回退旧的文本口径**并出声。
            if (dto.version < 15)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 15 —— 没有 `unitConditions` 一列"
                               + "（原版 `TargetCriteria.conditionsFilter` ＋ `excludeCondition`：`UnitConditions` "
                               + "0 none / 40 damaged / 220 EnoughPendingDamageToDie / 244 hasArmour …）；"
                               + "`CardDef.ConditionOf` 会一律返回 **-1 = 未知**"
                               + "（⛔ 不是 0 = 「没有条件」：那会让条件从句被静默跳过）⇒ 消费端按「未知」"
                               + "回退旧的文本口径并出声。跑一下 gen_cards_engine.py 重生成（`W64`）。");
            // 🆕 v16（2026-10-22 · 引擎线 `W64` · `B-16` 的明确缺口）：`count` 行的**被数集合**
            //   （原版 `BoardCriteria.countCriteria` ＋ `playerTurn`，字段 `countSet`）。
            //   **旧表整列没有** ⇒ `CardDef.ConditionSpec.CountSet` 一律 **null（未知）**。
            //   🔴 **必须出声**：这些字段全是**非负**的原版枚举/计数 —— 「缺列」若被读成「全 0」，
            //   就等于说「数全部单位（`TargetsAffected=any`）、友方 + 敌方、含督军含小兵 …」，
            //   那是一个**具体的集合选择**，不是「不知道」。消费端拿到 null 必须回退旧口径并出声。
            //   ⚠️ **只对 `15 <= version < 16` 出声**：`< 15` 的表**连 `unitConditions` 整列都没有**
            //      ⇒ 上面那条 `< 15` 的守卫已经说清「全未知」，再来一条只是重复（而且会让
            //      「旧表装载恰好一条 `unitConditions` 警告」的自检变红 —— `RuleEngineTest` ④）。
            if (dto.version >= 15 && dto.version < 16)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 16 —— `unitConditions` 的 `count` 行"
                               + " **没有 `countSet`（被数集合）**（原版 `BoardCriteria.countCriteria`："
                               + "`targetsAffected` / `friendlyUnits` / `enemyUnits` / `includeHero` / "
                               + "`includeMinions` / `includeRemnants` / `filterSpecificCards` … ＋ "
                               + "`BoardCriteria.playerTurn`）；`CardDef.ConditionSpec.CountSet` 会一律 **null = 未知**"
                               + "（⛔ 不是「默认集合」：全 0 是一个具体的集合选择）⇒ 消费端按「未知」"
                               + "回退旧口径并出声。跑一下 gen_cards_engine.py 重生成（`W64`）。");
            // 🆕 2026-10-23（引擎线 · `B-16` 残余 241/242）：v17 起才有逐卡 `abilityTriggers`
            //   （`cardAbilities[].trigger` 的全量集合）。**旧表整列没有** ⇒
            //   `CardDef.AbilityTriggerPresence` 一律 **`-1`（未知）**。
            //   🔴 **必须出声**：空表是**合法取值**（「这张卡没有任何 ability」⇒ `HasAbilityType` 假）——
            //   「缺列」若被读成空表，`241/242` 会把**所有卡**判成假（静默错一批）；反过来也⛔
            //   不能当未知 ⇒ 条件从句只剩「回退旧文本口径」一条路。
            if (dto.version < 17)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 17 —— 没有 `abilityTriggers` / "
                               + "`abilityTargets` 两列（`cardAbilities[].trigger` 的全量集合；逐 ability / 逐 logic 的 "
                               + "`targetsAffected`）。`CardDef.AbilityTriggerPresence` 会一律返回 **-1 = 未知**"
                               + "（⛔ 不是「没有 ability」：那会让条件 241/242 静默判假）；"
                               + "`CardDef.AbilityTargets` 为空表 ⇒ 「无主语正文」那条**回落旧的「己方全体」近似**并出声"
                               + "（⛔ 不是「数据说就是己方全体」）。判据 → `RawCardScript__HasAbilityType.c:16-38` / "
                               + "`AbilityLogic__GetTargets.c`。跑一下 gen_cards_engine.py 重生成（`B-16` 残余 ＋ `B-17`）。");
            // 🔴🆕 v18（2026-10-23 · 引擎线 `P1·①`）：v18 起才有逐卡 `abilityTriggerPriority`
            //   （原版 `AbilityTrigger` 名 → `CardAbility.priority`）。**旧表整列没有** ⇒
            //   `CardDef.AbilityTriggerPriority(name)` 一律回默认 **1（Instant）= 改前行为**。
            //   ⚠️ **这一列与 `triggerPriority` 不同**：缺列时**行为逐字不变**（改动前监听器就是一律
            //   取 1），所以这里出声只是「表是旧的，这条修复还没生效」，⛔ 不是数据错。
            if (dto.version < 18)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 18 —— 没有 `abilityTriggerPriority` 一列"
                               + "（原版 `AbilityTrigger` 名 → `CardAbility.priority`：`WhenEvent` / 回合 / 打出族"
                               + "那一半的触发道）。`CardDef.AbilityTriggerPriority` 会一律返回 **1 = Instant**"
                               + "（= 改前的行为，⛔ 不是「数据说全是 Instant」）⇒ 非默认道的监听器"
                               + "（现读 3 张：`DA12`/`BL72`/`DA14`）仍会排在 Instant 道上。"
                               + "跑一下 gen_cards_engine.py 重生成（引擎线 `P1·①`）。");

            // 🆕 v19（2026-10-23 · 引擎线 `新账-97` ③/⑤）：`spellType` 一列。
            //   缺列 ⇒ `CardDef.SpellType = -1`（未知）—— 消费端只在**这一格非 -1** 时才敢按
            //   `Secret(150)` / `SecretOrder(60)` / `Quest(120)` 筛；⛔ 不拿 `Normal(0)` 顶
            //   （那会让「是不是密令」永远判成「不是」）。
            if (dto.version < 19)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 19 —— 没有 `spellType` 一列"
                               + "（原版 `RawCardScript.spellType`：`CardCriteria.SpellType` 的主语筛，"
                               + "以及附魔层 `HasSecretOrders` / `HasQuests` 的判据）。"
                               + "`CardDef.SpellType` 会一律是 **-1 = 未知**（⛔ 不是 Normal(0)）"
                               + "⇒ 按 `spellType` 筛的维度退回正文口径。"
                               + "跑一下 gen_cards_engine.py 重生成（引擎线 `新账-97`）。");

            int noId = 0;

            // 🆕 2026-10-22（卡牌线 `T04`）：`useOverDraw` 一列的**缺列兜底** —— v9 才有这一列，
            //   而 `JsonUtility` 对**整列不存在**给的是 `0`（= 「这张卡不允许溢出」）⇒ 旧表会把
            //   **全池 1127 张静默变成「不放大、不压框」**。上面那条 `version < 9` 只出声、不拦。
            //   ⇒ 缺列时统一传 `-1`（`CardDef` 那边 `!= 0` ⇒ **允许** = 与接线前行为一致）。
            //   ⚠️ 判据是**表版本**、不是「这张卡的值是不是 0」—— `0` 在 v9+ 里是**合法真值**（486 张）。
            bool useOverDrawKnown = dto.version >= 9;

            // 🔴 **2026-10-21：卡名索引必须【先】建 —— 在造 `CardDef` 之前。**
            //   `CardDef` 的**构造函数**里就跑 `CollectTriggerOps` ⇒ `EffectText.Parse` ⇒
            //   目标解析 ⇒ `TailCardName` ⇒ `CreatePool.MatchCardName` —— 它读的正是这张索引。
            //   原来这里是「先把 1126 张 `CardDef` 全造完、最后才 `BuildNameIndex(list)`」
            //   ⇒ **构造期索引 = 上一趟留下的那份**：
            //     · 本进程**第一次** `Load()` ⇒ 索引**空** ⇒ 卡面写卡名的目标**一个都认不出**；
            //     · 之后每一次 `Load()` ⇒ 索引是上一趟那份（内容一样 ⇒ 看不出来）。
            //   ⇒ 同一份卡表，**第一次装载**与**第二次装载**解析出**不同的 op** = 对局不可复现（红线）。
            //   实测（`d:/4/_tmp_n1/` 探针 `idxdiff`，全池 1126 张、比
            //   `TriggerTexts`/`WhenTriggers`/`SpiritOps`/`OathOps`/`FaithOps`/`AuraSpecs`/`Ability`
            //   的**全部 op 与目标规格**）：**差 1 张** —— `UM_Angels_of_Death`
            //   （`Codex: Give +1 [Ranged] to your Primaris Intercessor`）：
            //     空索引 ⇒ `give 载荷「+1 ranged」目标 = null`（卡面写的名字整个丢了）；
            //     有索引 ⇒ `own/unit ×1 · name=Primaris Intercessor`（照卡面）。
            //   ⚠️ 下面那句末尾的 `BuildNameIndex(list)` **照旧保留** —— 它是「同一件事的
            //    `CardDef` 形态」（内容与这一趟逐字相同，幂等）；删了反而多一处口径分叉。
            CreatePool.BuildNameIndexFromNames(
                System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Where(dto.cards,
                        c => c != null && !string.IsNullOrEmpty(c.name)),
                    c => new KeyValuePair<string, string>(c.name, c.nameZh)));

            foreach (var c in dto.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                string cid = c.id;
                // 🔴 **2026-10-18（`W25`）：缺 id 不再拿卡名顶 —— 报错并【跳过这张卡】。**
                //   为什么：卡名**不是身份**。跨阵营 3 组同名（`Terminator` / `Terminator Champion` /
                //   `Maulerfiend`）会把两张不同的卡并成同一个 `CardDef.Id` ⇒ 费用修正（按 id）、
                //   复制品/临时卡、卡组存档全部**串在一起**，而且是**静默**的。
                //   身份 = 原版 `uniqueId`（现池 1127/1127 真 id、0 自造；生成器末尾有硬断言）。
                //   判据 → `资料/普查产出_1018/R_引擎线_完整ID与SO后的调整评估.md` §4 第 5 行。
                if (string.IsNullOrEmpty(cid))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.faction}）**缺 id** —— 已跳过该卡"
                                 + "（卡名不再当身份：同名卡会串）。重跑工具/gen_cards_engine.py"
                                 + "（它会断言 id_made == 0 且每个 id ∈ 原版 uniqueId 集合）。");
                    noId++;
                    continue;
                }
                list.Add(new CardDef(
                    id: cid,                    // 稳定 id = 原版 uniqueId（v6 起有该列；W25 起缺 id 直接跳过）
                    name: c.name,
                    type: c.type,
                    desc: c.desc,
                    rarity: c.rarity,
                    faction: c.faction,
                    cost: c.cost,
                    attack: c.attack,
                    health: c.health,
                    rangedAttack: c.ranged,
                    keywords: c.keywords,
                    nameZh: c.nameZh,
                    descZh: c.descZh,
                    fromOriginalPool: true,     // 这条路上来的都是原版卡（卡面文字取它自己的效果原文）
                    subtype: c.subtype,
                    goesSecondId: c.goesSecond,
                    inventoryOptions: c.inventoryOptions,
                    showInCollectionOptions: c.showInCollectionOptions,
                    // 🆕 2026-10-22（卡牌线 `T04`）：立绘溢出的**逐卡** opt-in —— 原样透传（`0` = 不许溢出，
                    //   `-1`/缺列 = 允许；口径见 `CardDef.UseOverDraw` 的 doc）。
                    //   ⚠️ 缺列（表 < v9）时传 `-1`、⛔ 不传 `c.useOverDraw`（那会是 `0` ⇒ 全池变严）——
                    //   见上面 `useOverDrawKnown` 那一段。
                    //   消费点 = `CardPresentation/Core/CardView.cs` 的 `UseOverDraw` → `ArtFrontVisible`。
                    useOverDraw: useOverDrawKnown ? c.useOverDraw : -1,
                    initiative: c.initiative,
                    armour: c.armour,
                    // 🆕 v10（2026-10-19 · `A1959`/`A1960`）：原版 `choicesDefinition` 三列 ——
                    //    ⚠️ 缺值口径是 `-1`（旧表读进来是 `0` ⇒ 上面那条 version 守卫会出声）。
                    choicesTargetsAffected: c.choicesTargetsAffected,
                    choicesMaxTargets: c.choicesMaxTargets,
                    choicesSpecificCardIds: c.choicesSpecificCardIds,
                    // 🆕 v11（2026-10-19 · `E12`）：逐候选动画时间 —— 只落数据，
                    //    ⚠️ 缺值口径 = 空表（旧表整列没有 ⇒ 上面的 version 守卫已经出过声）。
                    animTimes: MapAnimTimes(c.animTimes),
                    // 🆕 v13（2026-10-21 · 引擎线 `T03·c` 批 B）：逐触发关键词的原版 priority ——
                    //    只落数据（今天没有消费点）；**0..3 的合法性校验在 `CardDef` 构造函数里**
                    //    （非法 ⇒ `LogError` + 丢弃该条，与原版 `AddAutoActionToQueue` 同口径）。
                    //    🔴 2026-10-21（审查 `W61b` 第 7 条）：**「缺 `priority` 键」在 `MapTriggerPriority`
                    //    里就出声丢弃**（`-1` 哨兵；⛔ 不许静默落成 0 = Player 道）—— 见那边 doc 的 ③。
                    //    ⚠️ 缺值口径 = 空表（缺键 = 原版默认 1；旧表整列没有 ⇒ 上面的 version 守卫出声）。
                    triggerPriority: MapTriggerPriority(c),
                    // 🆕 2026-10-21（引擎线 `T03·b`）：开局加成三列 + `skipCardDraw` 生产列 ——
                    //    ⚠️ 缺值口径 = `0` / 空表（见 `CardDto.initialHandModifier` 的 doc）。
                    //    ⚠️ 这两族**不提卡表 version**（缺席 ⇔ 0 / 空表，语义完全相同）。
                    initialHandModifier: c.initialHandModifier,
                    initialManaModifier: c.initialManaModifier,
                    initialSpiritStoneModifier: c.initialSpiritStoneModifier,
                    skipDraws: MapSkipDraws(c),
                    // 🆕 v14（2026-10-22 · 引擎线 `W63` · `T03·c/d`）：逐条 ability 的原版
                    //    `abilityContext` —— **只落数据**（消费点 = `ActionPump` / `EffectResolver`，
                    //    另批接线）。四道闸（缺 `name` / 缺 `trigger` / 非法 `context` / 同 key 重复）
                    //    在 `MapAbilityContext` 里**出声丢弃**，⛔ 不在这里另判一遍。
                    //    ⚠️ 缺值口径 = 空表 + `AbilityContextsVersioned = false`（旧表整列没有
                    //    ⇒ 上面的 `< 14` 守卫已经出过声）；访问器一律 `-1`（未知），⛔ 不是 Play(0)。
                    abilityContext: MapAbilityContext(c),
                    abilityContextsVersioned: dto.version >= 14,
                    // 🆕 v15（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16`）：原版结构化条件
                    //    （`TargetCriteria.conditionsFilter`）—— **只落数据**（消费点 = `EffectResolver` /
                    //    `RuleCore` 的条件层，另批接线）。四道闸（缺 `role` / 缺 `ab`·`lg` /
                    //    非法 `cond` / 同 key 重复）在 `MapUnitConditions` 里**出声丢弃**，
                    //    ⛔ 不在这里另判一遍。
                    //    ⚠️ 缺值口径 = 空表 + `ConditionsVersioned = false`（旧表整列没有
                    //    ⇒ 上面的 `< 15` 守卫已经出过声）；访问器一律 `-1`（未知），⛔ 不是 `none(0)`。
                    abilityConditions: MapUnitConditions(c),
                    conditionsVersioned: dto.version >= 15,
                    // 🆕 v19（2026-10-23 · 引擎线 `新账-97`）：原版 `spellType` —— 原样透传；
                    //   缺列（< v19）时 `CardDto.spellType` 的字段初值就是 `-1`（未知），
                    //   ⛔ 不要在这里补 0（0 是合法的 `Normal`）。
                    spellType: c.spellType,
                    // 🆕 v16（2026-10-22 · 引擎线 `W64` · `B-16` 缺口）：`count` 行的**被数集合**
                    //    来自 v16 卡表的**正面信号**。⚠️ 它**不参与**访问器判定（看 `CountSet` 本身
                    //    在不在）；缺值口径 = `CountSet == null`（未知），旧表整列没有 ⇒ 上面
                    //    `< 16` 的守卫已经出声。⛔ 不是「默认集合」。
                    conditionsCountSetVersioned: dto.version >= 16,
                    // 🆕 v17（2026-10-23 · 引擎线 · `B-16` 残余 241/242）：`cardAbilities[].trigger`
                    //    的全量集合 —— **每张卡都落**（没 ability 的卡 `[]`），缺列 = 真值里没这张卡（未知）。
                    //    ⚠️ 缺值口径 = `null` + `AbilityTriggersVersioned = false`（旧表整列没有 ⇒
                    //    上面的 `< 17` 守卫已经出过声）；访问器一律 `-1`（未知），⛔ 不是「没有 ability」。
                    abilityTriggers: c.abilityTriggers,
                    abilityTriggersVersioned: dto.version >= 17,
                    // 🆕 v17（2026-10-23 · 引擎线 · 战斗审计 `B-17`）：逐 ability / 逐 logic 的
                    //    `targetsAffected` —— **每张卡都落**（没 ability = `[]`），缺列 = 真值里没这张卡。
                    //    ⚠️ 缺值口径 = 空表 + `AbilityTriggersVersioned = false`（同一列版本位；旧表
                    //    上面 `< 17` 的守卫已经出过声）⇒ 消费端回落旧的「己方全体」近似并出声。
                    abilityTargets: MapAbilityTargets(c),
                    // 🔴🆕 v18（2026-10-23 · 引擎线 `P1·①`）：逐条 ability 的 `priority`，**键 = 原版
                    //    `AbilityTrigger` 枚举名** —— 补 `triggerPriority`（键 = 引擎关键词）覆盖不到的
                    //    `WhenEvent` / 回合 / 打出族。四道闸（缺 `name` / 缺 `trigger` / 非法 `priority` /
                    //    同 key 重复）在 `MapAbilityTriggerPriority` 里**出声丢弃**，⛔ 不在这里另判一遍。
                    //    ⚠️ 缺值口径 = 空表 + `AbilityTriggerPrioritiesVersioned = false`（旧表整列没有
                    //    ⇒ 上面的 `< 18` 守卫已经出过声）；访问器一律回 **1 = Instant**（= 改前行为）。
                    abilityTriggerPriority: MapAbilityTriggerPriority(c),
                    abilityTriggerPrioritiesVersioned: dto.version >= 18));
            }
            // 🔴 🆕 2026-10-15（接线批）：**护甲两套口径一旦分叉就出声** ——
            //   引擎读的那一份（`CardDef` 构造里定下来的 `_keywords[armour]`）由
            //   「v9 `armour` 列优先、`keywords` 旧口径兜底」合成，两条腿**在正常数据上必须一致**。
            //   实测（2026-10-15）全池 1127 张只有 **1 张**分叉：`DA66 Ravenwing Ballistus Dreadnought`
            //   （列写 0、`keywords` 写裸 `Armour` ⇒ 旧口径 1，而 PnP 卡图银盾印 1 ⇒ **列是错的那一侧**，
            //   已另立账）⇒ 这里出声是为了**下一张**再出现时不必靠人肉比对。
            //   ⚠️ `RuleEngineTest.TestCardDatabase` 里有一条断言盯着同一件事（这里出声、那里断红）。
            ReportArmourDisagreements(list);
            // ⚠️ 2026-10-17 订正：下面这句原来写「跨阵营那 **4** 组」—— 第 4 组
            //    （`Bladeguard Veteran` = DarkAngels / Ultramarines）是 09-13 一次错改名的产物，
            //    10-17 已撤回（`UM34` 现在叫 `Bladeguard Lieutenant`）⇒ 实测 **3** 组
            //    （`Terminator` / `Terminator Champion` / `Maulerfiend`）。
            //    判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①。
            if (noId > 0)
                Debug.LogError($"[RuleEngine] 卡表里有 {noId} 张卡**没有 id**，**已跳过**（卡名不再当身份）—— "
                             + "这几张卡在对局/卡组里不存在了。重跑工具/gen_cards_engine.py"
                             + "（生成期断言：id_made 必须 = 0、每个 id 必须 ∈ 原版 uniqueId 集合）。");
            // 卡名索引（2026-09-16）：「按卡名指目标」那条路的判据 —— `ParseTarget` 要问
            // 「卡面写的那个名词是不是一张卡的名字」。**只此一处建**，见 `CreatePool.BuildNameIndex`。
            CreatePool.BuildNameIndex(list);

            // 🔴 🆕 **2026-10-14（`A1070` 第 ③ 档）：督军后手那张防御卡 —— id → 对象引用反查。**
            //   **只此一处**（`CreatePool.ResolveGoesSecond`），探针 `ruleprobe` 也调它 ——
            //   两处各写一份必然分叉（`A1339` 那条「探针与引擎不同口径」的教训）。
            //   ⚠️ 它**住在 `Core/`**（探针只编 `Core/*.cs`，见 `ruleprobe.csproj`）。
            CreatePool.ResolveGoesSecond(list);
            return list;
        }

        /// <summary>
        /// **护甲那两套口径的对账口**（2026-10-15 · 接线批新加）。
        ///
        /// 逐张比 <see cref="CardDef.ArmourFromPool"/>（v9 的 `armour` 列）与
        /// <see cref="CardDef.ArmourFromKeywords"/>（`keywords` 字符串那条旧口径）：
        ///   · 两张都 > 0 且**值不等** ⇒ 🔴 出声（引擎取的是列）；
        ///   · **只有列**有值 ⇒ 🔴 出声（那张卡的 `keywords` 里没有 `Armour N` —— 是列多写还是数据反了要人判）；
        ///   · **只有旧口径**有值 ⇒ ⚠️ 出声（**列的缺陷**：引擎这次按旧口径兜底，所以**不丢护甲**）。
        ///
        /// 正常数据上三类都应当是 0 条（2026-10-15 现读：**只有 `DA66` 一条**，属第三类）。
        /// ⛔ 只出声，**不抛异常、不改任何值**（本项目红线：静默失败比报错更贵）。
        /// </summary>
        /// <summary>🆕 v11（2026-10-19 · `E12`）：`animTimes` DTO → `CardDef.CardAnimTime`。
        /// `null` / 空 ⇒ 空数组（`Parse` 那边一律传非 null 的 `IEnumerable`）。
        /// ⚠️ 字段是**逐字搬运**，这里不做任何取舍/排序 —— 顺序 = JSON 数组顺序 = 原版 `abilityLogic` 顺序。</summary>
        static List<CardDef.CardAnimTime> MapAnimTimes(AnimTimeDto[] src)
        {
            var list = new List<CardDef.CardAnimTime>();
            if (src == null) return list;
            foreach (var a in src)
            {
                if (a == null) continue;
                list.Add(new CardDef.CardAnimTime
                {
                    Anim = a.a, Prefab = a.n, Trigger = a.tg, TriggerName = a.tn,
                    RandomChoice = a.rc != 0, Order = a.o,
                    Delay = a.d, StartDelay = a.sd, TimeAtStartPos = a.s,
                    TimeMoving = a.m, TimeAtEndPos = a.e,
                    Duration = a.u, Offset = a.t,
                });
            }
            return list;
        }

        /// <summary>🆕 v13（2026-10-21 · 引擎线 `T03·c` 批 B）：`triggerPriority` DTO →
        /// `CardDef` 构造参数（`IEnumerable&lt;KeyValuePair&lt;string,int&gt;&gt;`）。
        /// `null` / 空 ⇒ 空表（`Parse` 那边一律传非 null 的 `IEnumerable`）。
        ///
        /// 🔴 **本方法只搬值，不判 0..3 合不合法** —— 那条校验在 `CardDef` 构造函数里
        /// （非法 ⇒ `LogError` + 丢弃该条，与原版 `AddAutoActionToQueue` 同口径；⛔ 不 clamp）。
        /// 这里只出声**本方法才看得见**的三种数据缺陷（到了 `CardDef` 那边会被**静默**吃掉）：
        ///   ① `keyword` 为空 ⇒ `CardDef` 直接 `continue`（静默丢）；
        ///   ② 同一张卡同一个关键词出现两次 ⇒ `CardDef` 是「后写覆盖」（静默取后者）。
        ///      生成器那一侧对「同卡同词两个不同 priority」已整条丢弃并报 `report["conflict"]`
        ///      （2026-10-21 现读 0 条）—— 这里是**第二道闸**。
        ///   ③ 🔴🆕 2026-10-21（审查 `W61b` 第 7 条）：**`priority` 键缺失**（= 哨兵
        ///      `TriggerPriorityDto.MissingPriority`）⇒ **出声 + 丢弃该条**。
        ///      为什么必须丢：`0` 是**合法取值**（`Player` 道，最后才结算）⇒ 缺键被读成 0
        ///      就是把一条坏数据**静默塞进最后那一道**。口径与探针
        ///      （`工具/ruleprobe/Program.cs` 的 `nBadTp`）**逐字对齐**：
        ///      原版对非法 priority 也是 `LogError` 后丢弃（`BattleManager__AddAutoActionToQueue.c:226-228`）。
        ///      ⚠️ **越界值（4 / -2 …）仍交给 `CardDef` 构造函数出声丢弃**（那条规则只留一处，见上面）。
        /// ⚠️ ①③跳过、②照旧两条都往下传（保持 `CardDef` 的既有覆盖口径，⛔ 不在这里另立一套）。
        /// </summary>
        static List<KeyValuePair<string, int>> MapTriggerPriority(CardDto c)
        {
            var list = new List<KeyValuePair<string, int>>();
            if (c == null || c.triggerPriority == null) return list;
            HashSet<string> seen = null;
            foreach (var tp in c.triggerPriority)
            {
                if (tp == null) continue;
                if (string.IsNullOrEmpty(tp.keyword))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `triggerPriority` 有一条没有 "
                                 + $"`keyword`（priority={tp.priority}）—— 已跳过该条（`CardDef` 那边也会静默丢）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // 🔴🆕 2026-10-21（审查 `W61b` 第 7 条）：缺 `priority` 键 ⇒ **出声 + 丢弃**
                //   （⛔ 不许让它静默落进 Player(0) 道；判据与后果 → 方法 doc 的 ③）。
                if (tp.priority == TriggerPriorityDto.MissingPriority)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `triggerPriority` 里 `{tp.keyword}` "
                                 + "**没有 `priority` 键** —— 已丢弃该条（⛔ 没当成 0 = Player 道："
                                 + "`0` 是合法取值，缺键与「明确写 0」在数据上必须分开）。"
                                 + "探针 `ruleprobe check` 对同一条数据同样报「形状不全」并跳过；"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (seen == null) seen = new HashSet<string>(StringComparer.Ordinal);
                if (!seen.Add(tp.keyword))
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `triggerPriority` 里 `{tp.keyword}` "
                                 + "**出现了不止一次** —— 引擎口径是「后写覆盖」（`CardDef` 的字典赋值）⇒ "
                                 + "最终生效的是最后一条。一张卡一个关键词只能有一个 priority；"
                                 + "重跑工具/gen_cards_engine.py（它会整条丢弃并报 `conflict`）。");
                list.Add(new KeyValuePair<string, int>(tp.keyword, tp.priority));
            }
            return list;
        }

        /// <summary>🔴🆕 v18（2026-10-23 · 引擎线 `P1·①`）：`abilityTriggerPriority` 列 →
        /// `CardDef.AbilityTriggerPrioritySpec`。`null` / 空 ⇒ 空表（`Parse` 那边一律传非 null 的 `IEnumerable`）。
        ///
        /// 🔴 **四道闸，全部「出声 + 丢弃该条」**（与 `MapTriggerPriority` / `MapAbilityContext` 同口径：
        /// 坏数据挡在 `CardDef` 之前 —— 到了那边只会「后写覆盖」，也就是**静默**）：
        ///   ① `name` 为空 ⇒ 丢弃（`AbilityTrigger` 枚举名是唯一按键）；
        ///   ② `trigger` 缺键（`-1`）⇒ 丢弃（⛔ 别让它静默落成 0 = `ThisCardPlayed`）；
        ///   ③ `priority` 缺键（`-1`）或不在 **0..3** ⇒ 丢弃
        ///      （`0` = Player 道是**合法值**；原版对非法值 LogError 后丢弃，
        ///       `BattleManager__AddAutoActionToQueue.c:226-228`）；
        ///   ④ 同 `name` 重复 ⇒ **丢弃后出现的那条**（先到的保留）＋出声
        ///      （生成器对「同卡同 trigger 两个不同 priority」已整条丢弃并报 `conflict`，现读 0 条）。
        /// </summary>
        static List<CardDef.AbilityTriggerPrioritySpec> MapAbilityTriggerPriority(CardDto c)
        {
            var list = new List<CardDef.AbilityTriggerPrioritySpec>();
            if (c == null || c.abilityTriggerPriority == null) return list;
            HashSet<string> seen = null;
            foreach (var atp in c.abilityTriggerPriority)
            {
                if (atp == null) continue;
                // ① 缺 `name`（`AbilityTrigger` 枚举名 = 唯一按键）。
                if (string.IsNullOrEmpty(atp.name))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityTriggerPriority` 有一条没有 "
                                 + $"`name`（trigger={atp.trigger}, priority={atp.priority}）—— 已丢弃该条"
                                 + "（`AbilityTrigger` 枚举名是唯一按键；`CardDef` 那边也会静默丢）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ② 缺 `trigger` 键。
                if (atp.trigger == AbilityTriggerPriorityDto.MissingTrigger)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityTriggerPriority` 里 `{atp.name}` "
                                 + "**没有 `trigger` 键** —— 已丢弃该条（⛔ 没当成 0 = ThisCardPlayed："
                                 + "`0` 是合法取值，缺键与「明确写 0」在数据上必须分开）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ③ 缺 `priority` / 越界。
                if (atp.priority == AbilityTriggerPriorityDto.MissingPriority)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityTriggerPriority` 里 `{atp.name}` "
                                 + "**没有 `priority` 键** —— 已丢弃该条（⛔ 没当成 0 = Player 道："
                                 + "`0` 是合法取值，缺键与「明确写 0」在数据上必须分开）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (atp.priority < 0 || atp.priority > 3)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityTriggerPriority` 里 `{atp.name}` "
                                 + $"priority={atp.priority} **非法**（合法 0..3）—— 已丢弃该条"
                                 + "（原版对非法 priority 也是 LogError 后丢弃）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ④ 同 key 重复 ⇒ 丢弃后出现的那条（先到的保留）。
                if (seen == null) seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!seen.Add(atp.name))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityTriggerPriority` 里 `{atp.name}` "
                                 + "**出现了不止一次** —— 已丢弃**后出现**的那条（先到的保留）。"
                                 + "一张卡一个 trigger 名只能有一个 priority；"
                                 + "重跑工具/gen_cards_engine.py（它会整条丢弃并报 `conflict`）。");
                    continue;
                }
                list.Add(new CardDef.AbilityTriggerPrioritySpec
                {
                    Trigger = atp.trigger,
                    Name = atp.name,
                    Priority = atp.priority,
                });
            }
            return list;
        }

        /// <summary>🆕 v14（2026-10-22 · 引擎线 `W63` · `T03·c/d`）：`abilityContext` 列 →
        /// `CardDef.AbilityContextSpec`。`null` / 空 ⇒ 空表（`Parse` 那边一律传非 null 的 `IEnumerable`）。
        ///
        /// 🔴 **四道闸，全部「出声 + 丢弃该条」**（与 `MapTriggerPriority` 同口径：坏数据挡在
        /// `CardDef` 之前 —— 到了那边只会「后写覆盖」，也就是**静默**）：
        ///   ① `name` 为空 ⇒ 丢弃。`AbilityTrigger` **枚举名**是我们唯一的按键（关键词只覆盖
        ///      53/159 行），空名查不到任何东西。
        ///   ② `trigger` 缺键（= `AbilityContextDto.MissingTrigger`，`-1`）⇒ 丢弃。
        ///      ⛔ 别让它静默落成 0：`0` = `ThisCardPlayed`，是**合法** trigger。
        ///   ③ `context` 缺键（`-1`）或不在 **{0,10,20,30,40,50}** ⇒ 丢弃。
        ///      `0` 是**合法值**（`Play`）—— 生成器只落 `!= 0` 的行，但这里**不拒** 0
        ///      （手写/测试数据直接给 0 语义清楚）。原版对非法值也是 `LogError` 后丢弃。
        ///   ④ 同一张卡 `name` 或 `keyword` 重复 ⇒ **丢弃后出现的那条**（先到的保留）＋出声。
        ///      生成器对「同卡同 trigger 两个不同 context」已整条丢弃并报 `conflict`
        ///      （2026-10-22 现读 0 条）—— 这里是**第二道闸**，且**必须**丢：
        ///      `CardDef` 的索引是「后写覆盖」⇒ 不丢就是静默取最后一条。
        /// ⚠️ 槽位语义（0 活着在场 / 40 含待死 / 50 不含待死 / skip 表）**只在 `CardDef` 那一段
        /// 注释里有一份**，⛔ 别在这里抄第二份。
        /// </summary>
        static List<CardDef.AbilityContextSpec> MapAbilityContext(CardDto c)
        {
            var list = new List<CardDef.AbilityContextSpec>();
            if (c == null || c.abilityContext == null) return list;
            HashSet<string> seenName = null;
            HashSet<string> seenKeyword = null;
            foreach (var ac in c.abilityContext)
            {
                if (ac == null) continue;
                // ① 缺 `name`（`AbilityTrigger` 枚举名）。
                if (string.IsNullOrEmpty(ac.name))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityContext` 有一条没有 "
                                 + $"`name`（trigger={ac.trigger}, context={ac.context}）—— 已丢弃该条"
                                 + "（`AbilityTrigger` 枚举名是唯一按键；`CardDef` 那边也会静默丢）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ② 缺 `trigger` 键（⛔ 不许静默落成 0 = ThisCardPlayed）。
                if (ac.trigger == AbilityContextDto.MissingTrigger)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityContext` 里 `{ac.name}` "
                                 + "**没有 `trigger` 键** —— 已丢弃该条（⛔ 没当成 0 = ThisCardPlayed："
                                 + "`0` 是合法取值，缺键与「明确写 0」在数据上必须分开）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ③ `context` 缺键 / 非法值。
                if (ac.context == AbilityContextDto.MissingContext)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityContext` 里 `{ac.name}` "
                                 + "**没有 `context` 键** —— 已丢弃该条（⛔ 没当成 0 = Play："
                                 + "`0` = 「载体必须活着在场」，缺键读成 0 会把死亡族/墓地族触发掐死）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (!IsLegalAbilityContext(ac.context))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityContext` 里 `{ac.name}` "
                                 + $"context={ac.context} **非法**（合法只有 0 Play / 10 Hand / 20 Deck / "
                                 + "30 Cemetery / 40 InPlayOrDying / 50 InPlayOrCementery）—— 已丢弃该条。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ④ 同 key 重复 ⇒ 丢弃后出现的那条（先到的保留）。name 与 keyword 两本索引都要防。
                if (seenName == null) seenName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!seenName.Add(ac.name))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityContext` 里 `{ac.name}` "
                                 + "**出现了不止一次** —— 已丢弃**后出现**的那条（先到的保留）。"
                                 + "一张卡一个 trigger 只能有一个 context；"
                                 + "重跑工具/gen_cards_engine.py（它会整组丢弃并报 `conflict`）。");
                    continue;
                }
                if (!string.IsNullOrEmpty(ac.keyword))
                {
                    if (seenKeyword == null) seenKeyword = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!seenKeyword.Add(ac.keyword))
                    {
                        Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityContext` 里关键词 "
                                     + $"`{ac.keyword}` **出现了不止一次** —— 已丢弃**后出现**的那条"
                                     + "（先到的保留；`CardDef` 的关键词索引是「后写覆盖」，不丢就是静默取最后一条）。"
                                     + "重跑工具/gen_cards_engine.py。");
                        continue;
                    }
                }
                list.Add(new CardDef.AbilityContextSpec
                {
                    Trigger = ac.trigger,
                    Name = ac.name,
                    Keyword = string.IsNullOrEmpty(ac.keyword) ? null : ac.keyword,
                    Context = ac.context,
                });
            }
            return list;
        }

        /// <summary>🆕 v17（2026-10-23 · 引擎线 · 战斗审计 `B-17`）：`abilityTargets` 列 →
        /// <see cref="CardDef.AbilityTargetSpec"/>（逐 ability / 逐 logic 的 `targetsAffected`）。
        ///
        /// 🔴 **口径：只做形状校验，坏格落 `-1`、⛔ 不丢整条 ability**（与 `MapCountSet` 同族，
        /// 而不是 `MapAbilityContext` 那四道闸）—— 因为这一列的消费端**已经有自己的两道闸门**
        /// （`EffectText.AssignAbilityTargets`：块数 == ability 数、块内 op 数 == logic 数），
        /// 这里再丢行只会让「第几条 ability」**整体错位**（下标就是语义）。
        /// 真正非法的只有「`ta` 为 null」—— 那一格按空表处理（= 这条 ability 没有 logic）。
        /// </summary>
        static List<CardDef.AbilityTargetSpec> MapAbilityTargets(CardDto c)
        {
            var list = new List<CardDef.AbilityTargetSpec>();
            if (c == null || c.abilityTargets == null) return list;
            foreach (var at in c.abilityTargets)
            {
                if (at == null) { list.Add(null); continue; }   // 下标即语义 ⇒ 占位、不错位
                var ta = at.ta ?? new int[0];
                for (int i = 0; i < ta.Length; i++)
                    if (ta[i] < -1)
                    {
                        // 负值只允许 `-1`（= 坏值/缺键）。更小的数说明生成器/数据坏了 ⇒ 出声并落 -1。
                        Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityTargets` 有一条 "
                                     + $"`targetsAffected = {ta[i]}` **非法**（只允许 ≥ 0 或 -1）—— 该格按 -1 处理。"
                                     + "重跑工具/gen_cards_engine.py。");
                        ta[i] = -1;
                    }
                list.Add(new CardDef.AbilityTargetSpec { Trigger = at.trigger, TargetsAffected = ta });
            }
            return list;
        }

        /// <summary>🆕 v16（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16` 的**明确缺口**）：
        /// `count` 行的**被数集合**（原版 `BoardCriteria.countCriteria` ＋ `playerTurn`）→
        /// <see cref="CardDef.CountSetSpec"/>。
        ///
        /// 🔴 **这里的口径与那四道闸不同：坏值落 `-1`，⛔ 不丢整行。**
        /// 为什么：丢一行 ⇒ `CardDef.ConditionOf` 对 v15+ 的表会返回 `ConditionNone`（**没有条件**）
        /// ⇒ 条件从句被**静默放行** —— 那正是 `B-16` 要治的错法。所以：
        ///   · `countSet` 整个缺 → 出声 + `ConditionSpec.CountSet = null`（**未知**，消费端回退旧口径）；
        ///   · 单格缺键/非法 → 出声 + **那一格**落 `-1`（其余格照落，数据仍然可用）。
        /// 三道值域检查：
        ///   ① 每个整数字段不能是 `MissingIndex`（缺键）；
        ///   ② 18 个布尔字段只能是 `0/1`（JSON 侧就是整数）；
        ///   ③ `playerTurn` 只能是 `PlayersAffected` 的三档 `0/10/20`。
        /// ⚠️ 其余枚举字段（`TargetsAffected` / `CardArmy` / `DefinedTrait` …）**只做类型与缺键检查**，
        ///   不做成员校验 —— 那需要把 8 张原版枚举表抄进来（⛔ 两处枚举迟早不一致）；
        ///   语义判定由消费端下一波按 `d:/2/tools/decomp_full/` 做。
        /// ⚠️ 两个引用列表里**有空串** ⇒ 出声 + 丢掉那一条（保留其余）。
        /// </summary>
        static CardDef.CountSetSpec MapCountSet(CardDto c, UnitConditionDto uc)
        {
            var d = uc.countSet;
            if (d == null)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 里有一条 `count` 行"
                             + " **没有 `countSet`（被数集合）** —— 该行的 `CountSet` 落 **null = 未知**"
                             + "（⛔ 不是「默认集合」：`TargetsAffected=any` 是一个具体选择）。"
                             + "表 version ≥ 16 时这是坏数据。重跑工具/gen_cards_engine.py。");
                return null;
            }
            var s = new CardDef.CountSetSpec();

            // ③ `playerTurn` = `PlayersAffected` 三档（0 Both / 10 Self / 20 Enemy）。
            if (d.playerTurn == UnitConditionDto.MissingIndex)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet` 缺 `playerTurn` 键"
                             + " —— 落 -1（未知）。重跑工具/gen_cards_engine.py。");
            }
            else if (d.playerTurn != 0 && d.playerTurn != 10 && d.playerTurn != 20)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet.playerTurn = "
                             + $"{d.playerTurn}` **不在 `PlayersAffected`（0/10/20）里** —— 落 -1（未知；"
                             + "⛔ 不 clamp、不默认）。重跑工具/gen_cards_engine.py。");
            }
            else
            {
                s.PlayerTurn = d.playerTurn;
            }

            s.TargetsAffected            = CountSetInt(c, "targetsAffected", d.targetsAffected);
            s.ChosenArmy                 = CountSetInt(c, "chosenArmy", d.chosenArmy);
            s.ArmyFilter                 = CountSetInt(c, "armyFilter", d.armyFilter);
            s.KeywordFilter              = CountSetInt(c, "keywordFilter", d.keywordFilter);
            s.TraitsFilter               = CountSetInt(c, "traitsFilter", d.traitsFilter);
            s.MaxTargets                 = CountSetInt(c, "maxTargets", d.maxTargets);
            s.TargetSelectMethod         = CountSetInt(c, "targetSelectMethod", d.targetSelectMethod);
            s.AttackFilter               = CountSetInt(c, "attackFilter", d.attackFilter);
            s.AttackFilterOption         = CountSetInt(c, "attackFilterOption", d.attackFilterOption);
            s.CurrentHealthFilter        = CountSetInt(c, "currentHealthFilter", d.currentHealthFilter);
            s.CurrentHealthFilterOption  = CountSetInt(c, "currentHealthFilterOption", d.currentHealthFilterOption);
            s.ManaCostFilter             = CountSetInt(c, "manaCostFilter", d.manaCostFilter);
            s.ManaCostFilterOption       = CountSetInt(c, "manaCostFilterOption", d.manaCostFilterOption);
            s.RarityFilter               = CountSetInt(c, "rarityFilter", d.rarityFilter);
            s.RarityFilterOption         = CountSetInt(c, "rarityFilterOption", d.rarityFilterOption);
            s.SpellType                  = CountSetInt(c, "spellType", d.spellType);

            s.FriendlyUnits              = CountSetFlag(c, "friendlyUnits", d.friendlyUnits);
            s.EnemyUnits                 = CountSetFlag(c, "enemyUnits", d.enemyUnits);
            s.IncludeHero                = CountSetFlag(c, "includeHero", d.includeHero);
            s.IncludeMinions             = CountSetFlag(c, "includeMinions", d.includeMinions);
            s.IncludeSpells              = CountSetFlag(c, "includeSpells", d.includeSpells);
            s.IncludeWhispers            = CountSetFlag(c, "includeWhispers", d.includeWhispers);
            s.IncludeWeapons             = CountSetFlag(c, "includeWeapons", d.includeWeapons);
            s.IncludeRemnants            = CountSetFlag(c, "includeRemnants", d.includeRemnants);
            s.IncludeHeroParts           = CountSetFlag(c, "includeHeroParts", d.includeHeroParts);
            s.IncludeSelf                = CountSetFlag(c, "includeSelf", d.includeSelf);
            s.ExcludeNotSelf             = CountSetFlag(c, "excludeNotSelf", d.excludeNotSelf);
            s.ExcludeSimilarCards        = CountSetFlag(c, "excludeSimilarCards", d.excludeSimilarCards);
            s.ExcludeTargetted           = CountSetFlag(c, "excludeTargetted", d.excludeTargetted);
            s.ExcludeFriendlyHero        = CountSetFlag(c, "excludeFriendlyHero", d.excludeFriendlyHero);
            s.ExcludeEnemyHero           = CountSetFlag(c, "excludeEnemyHero", d.excludeEnemyHero);
            s.ExcludeRace                = CountSetFlag(c, "excludeRace", d.excludeRace);
            s.ExcludeTrait               = CountSetFlag(c, "excludeTrait", d.excludeTrait);
            s.SelectFromChooseResolution = CountSetFlag(c, "selectFromChooseResolution", d.selectFromChooseResolution);

            s.FilterSpecificCardIds  = CountSetIds(c, "filterSpecificCards", d.filterSpecificCards);
            s.ExcludeEnchantmentIds  = CountSetIds(c, "excludeEnchantments", d.excludeEnchantments);
            return s;
        }

        /// <summary>被数集合的一个整数字段：缺键 ⇒ 出声 + `-1`（未知）。</summary>
        static int CountSetInt(CardDto c, string field, int raw)
        {
            if (raw == UnitConditionDto.MissingIndex)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet` 缺 "
                             + $"`{field}` 键 —— 该格落 -1（未知；⛔ 不默认成 0）。重跑工具/gen_cards_engine.py。");
                return CardDef.CountSetSpec.Unknown;
            }
            return raw;
        }

        /// <summary>被数集合的一个布尔字段（JSON 侧是 0/1）：缺键 / 非 0-1 ⇒ 出声 + `-1`（未知）。</summary>
        static int CountSetFlag(CardDto c, string field, int raw)
        {
            if (raw == UnitConditionDto.MissingIndex)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet` 缺 "
                             + $"`{field}` 键 —— 该格落 -1（未知；⛔ 不当成 false）。重跑工具/gen_cards_engine.py。");
                return CardDef.CountSetSpec.Unknown;
            }
            if (raw != 0 && raw != 1)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet.{field} = {raw}`"
                             + " **不是 0/1** —— 该格落 -1（未知；⛔ 不 clamp、不当成 true/false）。"
                             + "重跑工具/gen_cards_engine.py。");
                return CardDef.CountSetSpec.Unknown;
            }
            return raw;
        }

        /// <summary>被数集合的一个卡引用列表：缺键 ⇒ 出声 + `null`（未知）；
        /// 空串条目 ⇒ 出声 + 丢掉那一条（保留其余 —— 与「整份未知」区分开）。</summary>
        static string[] CountSetIds(CardDto c, string field, string[] raw)
        {
            if (raw == null)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet` 缺 "
                             + $"`{field}` 键 —— 落 null（未知；⛔ 不当成空表）。重跑工具/gen_cards_engine.py。");
                return null;
            }
            var list = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                if (string.IsNullOrEmpty(raw[i]))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `count` 行 `countSet.{field}` 第 {i} 条"
                                 + " 是空串 —— **丢掉那一条**（保留其余）。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                list.Add(raw[i]);
            }
            return list.ToArray();
        }

        /// <summary>🆕 v15（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16`）：`unitConditions` 列 →
        /// `CardDef.ConditionSpec`。`null` / 空 ⇒ 空表（`Parse` 那边一律传非 null 的 `IEnumerable`）。
        ///
        /// 🔴 **四道闸，全部「出声 + 丢弃该条」**（与 `MapAbilityContext` / `MapTriggerPriority` 同口径：
        /// 坏数据挡在 `CardDef` 之前 —— 到了那边只会「后写覆盖」，也就是**静默**）：
        ///   ① `role` 不在 `CardDef.ConditionRoleNames`（十个位置）⇒ 丢弃。
        ///   ② `ab` / `lg` **缺键**（= `UnitConditionDto.MissingIndex`）⇒ 丢弃。
        ///      ⛔ 别让它静默落成 0：`0` 是**合法** `ab`（第 0 条 ability）与**合法** `lg`
        ///      （第 0 条 logic）⇒ 缺键读成 0 会把条件挂到**错的位置**上。
        ///   ③ `cond` **缺键**（`-1`）/ `0`（`none`，本列按约定不落）/ 不在 `UnitConditions` 枚举里
        ///      ⇒ 丢弃。`0` 在这里是**「没有条件」**，而本列的每一行都表示**有一个条件** ⇒
        ///      落成 0 的行是坏数据（⛔ 不静默当成「无条件」）。
        ///   ④ 同 `(ab, lg, role)` 重复 ⇒ **丢弃后出现的那条**（先到的保留）＋出声。
        ///      生成器每一格只落一行（同一格天然唯一）；这里是**第二道闸**，且**必须**丢：
        ///      `CardDef` 的索引是「后写覆盖」⇒ 不丢就是静默取最后一条。
        /// ⚠️ 位置语义（十个 role 分别是什么、`count` 的三个参数）**只在 `CardDef` 那一段注释里有一份**，
        /// ⛔ 别在这里抄第二份。
        /// </summary>
        static List<CardDef.ConditionSpec> MapUnitConditions(CardDto c)
        {
            var list = new List<CardDef.ConditionSpec>();
            if (c == null || c.unitConditions == null) return list;
            HashSet<string> seen = null;
            foreach (var uc in c.unitConditions)
            {
                if (uc == null) continue;
                // ① 位置（`role`）必须是那十个之一。
                if (!CardDef.IsLegalConditionRole(uc.role))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 有一条 `role` "
                                 + $"不认识（{uc.role ?? "null"}）—— 已丢弃该条（合法位置 = "
                                 + string.Join(" / ", CardDef.ConditionRoleNames)
                                 + "；`CardDef` 那边也会静默丢）。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ② `ab` / `lg` 缺键（⛔ 不许静默落成 0 = 第 0 条 ability / 第 0 条 logic）。
                if (uc.ab == UnitConditionDto.MissingIndex || uc.lg == UnitConditionDto.MissingIndex)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 有一条 "
                                 + $"`{uc.role}` **缺 `ab`/`lg` 键**（ab={uc.ab}, lg={uc.lg}）—— 已丢弃该条"
                                 + "（⛔ 没当成 0：`0` 是合法位置，缺键与「明确写 0」在数据上必须分开）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ③ `cond` 缺键 / none(0) / 不在枚举里。
                if (uc.cond == UnitConditionDto.MissingCondition)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 有一条 "
                                 + $"`{uc.role}` **没有 `cond` 键** —— 已丢弃该条（⛔ 没当成 0 = none："
                                 + "`0` 是合法取值「没有条件」，缺键与「明确写 0」在数据上必须分开）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (uc.cond == CardDef.ConditionNone)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 有一条 "
                                 + $"`{uc.role}` 的 `cond = 0`（`UnitConditions.none`）—— 已丢弃该条"
                                 + "（本列按约定**只落非 none 的行**；落 0 的行是坏数据，"
                                 + "⛔ 不静默当成「无条件」）。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (!CardDef.IsLegalUnitCondition(uc.cond))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 有一条 "
                                 + $"`{uc.role}` 的 `cond = {uc.cond}` **不在 `UnitConditions` 枚举里**"
                                 + " —— 已丢弃该条（⛔ 不猜、不 clamp）。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ④ 同 (ab, lg, role) 重复 ⇒ 丢弃后出现的那条（先到的保留）。
                //    键的拼法与 `CardDef.ConditionKey` 同口径（整数走 invariant）。
                string key = uc.ab.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":"
                           + uc.lg.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":"
                           + uc.role;
                if (seen == null) seen = new HashSet<string>(StringComparer.Ordinal);
                if (!seen.Add(key))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 里 "
                                 + $"`ab={uc.ab}, lg={uc.lg}, role={uc.role}` **出现了不止一次** —— "
                                 + "已丢弃**后出现**的那条（先到的保留；`CardDef` 的索引是「后写覆盖」，"
                                 + "不丢就是静默取最后一条）。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                // ⑤ 🆕 v16：`countSet` 只属于 `count` 行 —— 别的 role 带着它是坏数据（出声、忽略）。
                //    `count` 行的 `countSet` 缺/坏**不丢行**，只落 `null`/`-1`（理由见 `MapCountSet`）。
                CardDef.CountSetSpec countSet = null;
                if (uc.role == "count")
                {
                    countSet = MapCountSet(c, uc);
                }
                else if (uc.countSet != null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 里 `role = "
                                 + $"`{uc.role}` 的行**带着 `countSet`** —— 被数集合只属于 `count` 行，"
                                 + "已**忽略**该字段（行本身照收）。重跑工具/gen_cards_engine.py。");
                }
                list.Add(new CardDef.ConditionSpec
                {
                    Ability = uc.ab,
                    Logic = uc.lg,
                    Role = uc.role,
                    Condition = uc.cond,
                    Name = string.IsNullOrEmpty(uc.name) ? null : uc.name,
                    Exclude = uc.ex != 0,
                    CountOption = uc.opt,
                    StatOption = uc.op,
                    ReferenceValue = uc.refValue,
                    CountSet = countSet,
                });
            }
            return list;
        }

        /// <summary>`AbilityContext` 的**六档合法值**：`0 Play` / `10 Hand` / `20 Deck` / `30 Cemetery` /
        /// `40 InPlayOrDying`（含待死） / `50 InPlayOrCementery`（不含待死）。
        /// ⚠️ 常量本身在 <see cref="CardDef"/>（`AbilityContextPlay` 那一族）—— 这里只做
        /// 「是不是这六个数之一」的判定，⛔ 别在这里另立语义或另抄一组数字。
        /// </summary>
        static bool IsLegalAbilityContext(int ctx)
        {
            return ctx == CardDef.AbilityContextPlay
                || ctx == CardDef.AbilityContextHand
                || ctx == CardDef.AbilityContextDeck
                || ctx == CardDef.AbilityContextCemetery
                || ctx == CardDef.AbilityContextInPlayOrDying
                || ctx == CardDef.AbilityContextInPlayOrCemetery;
        }

        /// <summary>🆕 2026-10-21（引擎线 `T03·b`）：`skipDraws` 列 → `CardDef.SkipDrawSpec`。
        /// 语义/判据 → <see cref="CardDef.SkipDraws"/> 的 doc（⛔ 别在这里抄第二份）。
        /// 两道闸：① `keyword` 为空 ⇒ **出声并跳过**（`CardDef` 那边也会静默丢）；
        ///          ② `uses &lt; 1` ⇒ 出声并**按 1 收**（原版 `numberOfUses --` 后 `&lt; 1` 立刻摘，
        ///             0 次的效果等于不存在 —— 但不能静默改成「不限次」）。
        /// ⚠️ `limited == 0` 是**合法值**（不限次），⛔ 别拿它当缺值。
        /// 🔴 **2026-10-22 就地订正（铁律 5）**：本行原来写「🆕 **v14**（2026-10-21 · 引擎线 `T03·b`）」——
        ///    **那次并没有 bump 卡表 version**（当时仍是 v13），而本列**本来就约定不提 version**
        ///    （「缺列」⇔「空表」，见 `SkipDrawDto` 的 doc）⇒ 那个 `v14` 标签是错的。
        ///    **`v14` 已被 `abilityContext` 一列占用**（2026-10-22 · `W63`）。
        /// </summary>
        static List<CardDef.SkipDrawSpec> MapSkipDraws(CardDto c)
        {
            var list = new List<CardDef.SkipDrawSpec>();
            if (c == null || c.skipDraws == null) return list;
            foreach (var sd in c.skipDraws)
            {
                if (sd == null) continue;
                if (string.IsNullOrEmpty(sd.keyword))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `skipDraws` 有一条没有 `keyword` "
                                 + "—— 已跳过该条（`CardDef` 那边也会静默丢）。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                int uses = sd.uses;
                if (uses < 1)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `skipDraws`（`{sd.keyword}`）"
                                 + $"uses={sd.uses} 非法（小于 1）—— 按 **1** 收。");
                    uses = 1;
                }
                list.Add(new CardDef.SkipDrawSpec
                {
                    Keyword = sd.keyword,
                    Uses = uses,
                    Limited = sd.limited != 0,
                    Source = string.IsNullOrEmpty(sd.source) ? (c.name + "（抽牌封锁）") : sd.source,
                });
            }
            return list;
        }

        static void ReportArmourDisagreements(List<CardDef> pool)
        {
            var lines = new List<string>();
            foreach (var c in pool)
            {
                if (c == null) continue;
                int fromPool = c.ArmourFromPool, fromKw = c.ArmourFromKeywords;
                if (fromPool == 0 && fromKw == 0) continue;
                if (fromPool > 0 && fromKw > 0 && fromPool == fromKw) continue;
                string kind = (fromPool > 0 && fromKw == 0) ? "只有列有值"
                            : (fromPool == 0) ? "只有旧口径（keywords）有值"
                            : "两处值不等";
                lines.Add($"{c.Id} {c.Name}：{kind} —— 列={fromPool} · keywords={fromKw}");
            }
            if (lines.Count == 0) return;
            Debug.LogWarning($"[RuleEngine] 护甲两套口径分叉 {lines.Count} 张"
                           + "（引擎读的是「`armour` 列优先、`keywords` 旧口径兜底」那一份）—— "
                           + string.Join(" / ", lines.ToArray())
                           + "。判据 → `CardDef.ArmourFromPool` / `CardDef.ArmourFromKeywords` 的 doc。");
        }

        // ---- 常用筛选 ----

        public static List<CardDef> Units(IEnumerable<CardDef> pool)
        {
            var outList = new List<CardDef>();
            foreach (var c in pool) if (c != null && c.Type == "unit") outList.Add(c);
            return outList;
        }

        public static List<CardDef> OfFaction(IEnumerable<CardDef> pool, string faction)
        {
            var outList = new List<CardDef>();
            foreach (var c in pool) if (c != null && c.Faction == faction) outList.Add(c);
            return outList;
        }

        public static CardDef Find(IEnumerable<CardDef> pool, string name)
        {
            foreach (var c in pool) if (c != null && c.Name == name) return c;
            return null;
        }

        /// <summary>
        /// 按卡名在**指定阵营**里找。找不到返回 null。
        ///
        /// 🔴 **为什么需要这个重载**（2026-09-13 第三十二轮）：**原版有跨阵营同名卡**
        /// （`Terminator` / `Terminator Champion` / `Maulerfiend` 这 3 组），
        /// 而 <see cref="Find"/> 只按名字匹配、**取池子里第一个** —— 同名卡谁在前谁赢。
        /// 后果是 2026-09-13 那次改名之后暴露出来的：卡组里明明存的是 Ultramarines 那张
        /// （`UM34`），`Find` 却先撞上 DarkAngels 那张同名卡 ⇒
        /// `DeckRules.Validate` 判 `WrongFaction`、**一副合法卡组被打回自动凑**。
        /// （⚠️ 卡池按 `cards_engine.json` 的顺序遍历，所以「谁在前面」是**数据顺序**决定的，
        /// 换个数据源就可能翻车 —— 这正是不能靠巧合的地方。）
        /// ⚠️ **2026-10-17 订正**：这一段原来拿 `Bladeguard Veteran`（DarkAngels / Ultramarines）
        /// 当例子，上面那张 `UM34` 当时也确实**叫** `Bladeguard Veteran` —— 那是 09-13 一次
        /// **错改名**（照 PnP 卡图文件名改的，印刷品 ≠ 数字版卡名），10-17 已撤回，`UM34` 现在叫
        /// `Bladeguard Lieutenant` ⇒ **这一组不再同名**，同名组 4 → 3（就是上面那三组）。
        /// 坑本身是真的、也还在（判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①）。
        ///
        /// ⚠️ **`faction` 传 null / 空 = 退回旧行为**（只按名字找），所以既有调用点一处不用改。
        /// </summary>
        public static CardDef Find(IEnumerable<CardDef> pool, string name, string faction)
        {
            if (string.IsNullOrEmpty(faction)) return Find(pool, name);
            foreach (var c in pool)
                if (c != null && c.Name == name && DeckRules.SameFaction(c.Faction, faction)) return c;
            // 阵营里没有同名卡 —— 不假装找到（调用方按 null 处理）
            return null;
        }

        /// <summary>按卡名找督军（`type == "hero"`）</summary>
        public static CardDef FindHero(IEnumerable<CardDef> pool, string name)
        {
            foreach (var c in pool) if (c != null && c.Type == "hero" && c.Name == name) return c;
            return null;
        }

        /// <summary>
        /// 按**稳定 id** 找卡（2026-09-13 第三十三轮）。**这是唯一没有歧义的找法** ——
        /// 按卡名找永远要担心跨阵营重名（见 <see cref="Find(IEnumerable{CardDef}, string, string)"/>），
        /// id 不会。**凡是要指「哪一张牌」的地方，都该用这个，不该用卡名。**
        /// 找不到返回 null。
        /// </summary>
        public static CardDef FindById(IEnumerable<CardDef> pool, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var c in pool) if (c != null && c.Id == id) return c;
            return null;
        }

        /// <summary>`id → CardDef`（要反复按 id 找时先建一次，别在循环里 `FindById`）。</summary>
        public static Dictionary<string, CardDef> IdIndex(IEnumerable<CardDef> pool)
        {
            var map = new Dictionary<string, CardDef>(StringComparer.Ordinal);
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                map[c.Id] = c;         // id 撞车在生成器那一侧就炸了（见 gen_cards_engine.py），这里不判
            }
            return map;
        }

        /// <summary>
        /// **把卡组里的一条「牌引用」解析成卡** —— 解析卡组引用的**唯一一处**。
        ///
        /// 顺序有意为之（2026-09-13 第三十三轮定，配合稳定 id）：
        ///   ① **按稳定 id 找**（`IdIndex`）—— 新卡组存档写的就是 id，**没有歧义、不需要阵营**；
        ///   ② 找不到再**按卡名找**（给了 `faction` 就只在本阵营里找）——
        ///      这条只为**旧存档**留着：2026-09-13 之前卡组里存的是**卡名**，
        ///      而原版有跨阵营同名卡（`Terminator` / `Terminator Champion` / `Maulerfiend` 这 3 组），
        ///      所以走这条必须带 `faction`，否则会撞上另一个阵营那张。
        ///      ⚠️ 2026-10-17 订正：这里原来举的例子还带一个 `Bladeguard Veteran` —— 那组是
        ///      09-13 一次错改名的产物、10-17 已撤回（`UM34` 现在叫 `Bladeguard Lieutenant`），
        ///      现在的同名组就是上面那三组（判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①）。
        ///
        /// 两条都找不到返回 null，**由调用方报错**（不静默）。
        /// </summary>
        public static Func<string, CardDef> DeckLookup(IList<CardDef> pool, string faction = null)
        {
            var byId = IdIndex(pool);
            return key =>
            {
                if (string.IsNullOrEmpty(key)) return null;
                CardDef c;
                if (byId.TryGetValue(key, out c)) return c;   // ① 稳定 id
                return Find(pool, key, faction);              // ② 卡名（旧存档）
            };
        }

        /// <summary>
        /// **id 不变量自检**（给 `RuleEngineTest` 用）：每张卡都有 id、且两两不同。
        /// 返回人类的说明（过了就是空串）—— 不抛异常，让自检去断言。
        /// </summary>
        public static string CheckIds(IEnumerable<CardDef> pool)
        {
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            int n = 0;
            foreach (var c in pool)
            {
                if (c == null) continue;
                n++;
                if (string.IsNullOrEmpty(c.Id)) return $"「{c.Name}」没有 id";
                if (seen.TryGetValue(c.Id, out var other))
                    return $"id 撞车：{c.Id} —— 「{other}」与「{c.Name}」共用";
                seen[c.Id] = c.Name;
            }
            return n == 0 ? "卡池是空的" : "";
        }

        /// <summary>数据集里出现过的阵营（按卡数从多到少）</summary>
        public static List<string> Factions(IEnumerable<CardDef> pool)
        {
            var count = new Dictionary<string, int>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Faction)) continue;
                count[c.Faction] = (count.TryGetValue(c.Faction, out var n) ? n : 0) + 1;
            }
            var list = new List<string>(count.Keys);
            list.Sort((a, b) => count[b].CompareTo(count[a]));
            return list;
        }
    }
}
