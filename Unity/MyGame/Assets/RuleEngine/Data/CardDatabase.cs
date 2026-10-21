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
            /// 生成器跑完会打出「填了多少 / 被更高层换掉多少 / 因引擎不认跳过多少」三个数。</summary>
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
            public int attackDamageType;

            /// <summary>原版 `RawCardScript.cardShortId`（1…914、860 种、294 张为 0）。
            /// 🔴 **⛔ 不许拿它当卡 id 换** —— `Q_A1770与A1799判据.md` 现核：它与 `A970` 那套
            /// （= PnP 图文件名编号）**0/763 重合**，按名引用在 `decomp_full` 全库 **0 处**。
            /// 实测形状 = 「标的是**哪张卡**、不是哪个卡 SO」（本体与 `(tutorial)` 副本同号）。</summary>
            public int cardShortId;

            /// <summary>立绘**溢出卡框**的逐卡 opt-in（0/1）。原版消费点 =
            /// `CardDisplayWindow__ChangeCardPosition.c:75-79` → `BasicCardUI.SetImageOverDraw`。
            /// ⚠️ `CardPresentation/Core/CardView.cs` 里那句「值在服务器、我们拿不到」**已过期**（铁律 5）。</summary>
            public int useOverDraw;

            /// <summary>「不能被随机生成」（0/1）。⚠️ **读取点没找到**（`allcards_未挖字段盘点.md` §四）
            /// ⇒ 它是不是 `CreatePool` 该用的闸门**未证** ⇒ 本列只落值、⛔ 不接。</summary>
            public int cantBeRandomlyCreated;

            /// <summary>`CardExpansionType`（10 LevaithanDrop1 … 80 DarkAngelsDrop1）；`0` = 基础。
            /// 用途（商店 / 卡包 / 收藏筛选）另接。</summary>
            public int expansionPack;

            /// <summary>卡名的 **I2 键后缀**（`Card_Name/<值>`）⇒ 这 70 张的**显示名 ≠ `cardName`**。
            /// ⚠️ **本列在现池全空** —— 那 70 条**全在 GUID 卡上**，而它们在 P1 相没进我们池（Q 现核）。</summary>
            public string refNameId;

            /// <summary>卡面「说明行」的 I2 键。现读只有两种：`Item_Description/OffenceCardExplanation`
            /// （进攻卡）与 `…/DefenceCardExplanation`（防御卡）——**中文已在官方表里**。</summary>
            public string itemDescriptionKey;

            /// <summary>督军**整幅立绘**引用。有 `m_SubObjectName` 时写它（实测是
            /// `UI_Deck_Warlord_<卡名>` 这种**资产名**），否则写 `m_AssetGUID`（32 位十六进制）。
            /// 🔴 **GUID 那条路解析不出文件名**：`guid_map.tsv` 那 9 类引用里**没含它**（命中 0/59）。</summary>
            public string fullHeroSpriteReference;

            /// <summary>督军「事件」立绘的 sprite 名。⚠️ 实测 **44 张值全同** =
            /// `Warlord_events_Loken` ⇒ 多半是**图集名**、单图按 id 取（语义**未查清**）⇒ 只落值。</summary>
            public string eventSpriteName;

            /// <summary>这张牌**要选目标**（0/1）。⚠️ 我们**从效果解析推**（`SimpleAI` 引
            /// `EntityScript__get_targeted`）—— 两者是否等价**未核** ⇒ 只落值、⛔ 不替换现有推断。</summary>
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

            int noId = 0;

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
                    skipDraws: MapSkipDraws(c)));
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

        /// <summary>🆕 v14（2026-10-21 · 引擎线 `T03·b`）：`skipDraws` 列 → `CardDef.SkipDrawSpec`。
        /// 语义/判据 → <see cref="CardDef.SkipDraws"/> 的 doc（⛔ 别在这里抄第二份）。
        /// 两道闸：① `keyword` 为空 ⇒ **出声并跳过**（`CardDef` 那边也会静默丢）；
        ///          ② `uses &lt; 1` ⇒ 出声并**按 1 收**（原版 `numberOfUses --` 后 `&lt; 1` 立刻摘，
        ///             0 次的效果等于不存在 —— 但不能静默改成「不限次」）。
        /// ⚠️ `limited == 0` 是**合法值**（不限次），⛔ 别拿它当缺值。
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
