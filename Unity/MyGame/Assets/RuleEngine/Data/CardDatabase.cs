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
    public static partial class CardDatabase   // 🆕 v37（`E6`）：`partial` 是为了把 `CardDto` 的两个新字段追加到【文件末】—— 见文件末尾那段（⛔ 别挪进 `CardDto` 体内：下游 5 处 A 行账落点会整体漂移）
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
        public partial class CardDto   // 🆕 v37（`E6`）：`partial` —— 两个新字段在【文件末】那段
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
            /// <summary>🆕 2026-10-25（P3 · 账里 3 条 C）：原版 `RawCardScript.specialAttackScoringCriteria`
            /// （**bool**，`+0x2F8`，`dump.cs:23358`）。语义 → `CardDef.SpecialAttackScoring` 的 doc。
            /// ⚠️ 它是 **bool** ⇒ **没有「未知」档**：缺键即 `0`（不带）。
            /// 与 `spellType` 的 `-1` 口径**不同**，⛔ 别照抄那一条。
            /// 真值现读 1,351 张**全 0**（`cards_raw.json` 的 `full_raw`；与 `AUD_T06` §二 同结论）。</summary>
            public int specialAttackScoring;
            /// <summary>🆕 2026-10-27（完全复刻 · 执行代理 `W46ALTD`）：原版 `RawCardScript.alternateArts`
            /// （**PPtr 数组** `AlternateArtCard[]`，`+0x80`）落到我们卡表的**异画副本 id 列表**
            /// （形如 `["#HB_SW1"]`）。语义 / 三态口径 / 7 条真值 → `CardDef.AttachAlternateArts` 上面那段注
            /// （⛔ 别在这里抄第二份）。
            /// <para>🔴 **`null` ≠ `[]`**（生成器契约 `工具/gen_cards_engine.py:1193`）：
            /// `null` = 整行**没有这个键**（旧表 / 夹具直建）⇒ 「判不了」；
            /// `[]` = 表里**明确说了这张卡没有异画**。`CardDatabase.Parse` 原样把两者分开传下去。</para></summary>
            public string[] alternateArts;
            public string targetingTextReferenceId;   // 🆕 2026-10-27（`A4`）：原版 `RawCardScript.targetingTextReferenceId`（v35 起；语义/三态 → `CardDef.AttachTargetingTextReference` 的 doc，⛔ 不抄第二份）
            public string cardDescriptionTextReferenceId;   // 🆕 2026-10-27（`A8` · §12·94）：原版 `RawCardScript.cardDescriptionTextReferenceId`（v35 起；语义/三态 → `CardDef.AttachCardDescriptionTextReference` 的 doc，⛔ 不抄第二份）。消费点 = `BattleDriver.OfficialDescKey` → `FaceTextFull` 那 4 处取键。
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

            /// <summary>「不能被随机生成」（0/1）。
            /// 🔴 **2026-10-25 就地订正（铁律 5 · 完全复刻 · 执行代理 A）**：原写「读取点没找到（判据不足）」——
            /// **读点找到了**：原版**唯一**的运行期读取点 = `AbilityLogic.GetTargets`
            /// （`AbilityLogic.cs:723-…` 的 `targetsAffected == choiceResolution` 分支，
            /// `:132` 那句 `if (!rawCardScript.cantBeRandomlyCreated)`），与 `IsHidden()` 一起过滤
            /// 「回落全卡池」的候选。`grep -rln cantBeRandomlyCreated` 全量反编译只命中 `AbilityLogic` +
            /// `RawCardScript`（定义），所以读点只有这一处。
            /// ⚠️ **仍然不接线**，理由换了：我们**还没有**「`filterSpecificCards` 为空 ⇒ 回落全卡池」
            ///    那一段（`CardDef.HasSpecificCardChoice` 要求 `filterSpecificCards` 非空）⇒
            ///    没有消费方可以挂它。现读卡池 1128 张里 **167 张 =1** ⇒ 那一档一旦建起来，
            ///    这个闸就必须一起建（否则随机池会混进「不该被随机生成」的卡）。</summary>
            public int cantBeRandomlyCreated;

            /// <summary>`CardExpansionType`（10 LevaithanDrop1 … 80 DarkAngelsDrop1）；`0` = 基础。
            /// 🔴 **2026-10-25（完全复刻 · 执行代理 A）：这一列【不是死列】—— `[Obsolete]` 已摘掉。**
            /// 原版读点 = `CardNameFilter.CheckInputTextSearch`（`CardNameFilter.cs:132-142`）：
            /// 收藏页「名字」搜索框会拿 `((Enum)expansionPack).ToString().ToLower()`（**枚举成员名**）
            /// 跟输入做包含匹配，`Default(0)` 不参与。
            /// ⇒ 消费点 = <see cref="RuleEngine.CardDef.ExpansionSearchName"/>（卡池搜索侧读它）。
            /// ⚠️ 原来这里写「没有运行期消费者（商店/卡包/收藏筛选还没接这一列）」—— 那句的**前提**
            ///    （找不到读点）已被推翻；商店/卡包那两处**仍然**没接（那是另一笔账）。</summary>
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

            /// <summary>这张牌**要选目标**（0/1）。
            /// <para>⚠️ 历史注记（2026-10-23 前）：这里写着「我们**从效果解析推**（`SimpleAI` 引
            /// `EntityScript__get_targeted`）—— 两者是否等价**未核** ⇒ 只落值、⛔ 不替换现有推断」。</para>
            /// <para>🔴🆕 **2026-10-23（引擎线 `新账-95` #2）就地改口径（铁律 5）：这一列【不再是死列】** ——
            /// 它现在是卡级 `role="cardtarget"` 条件行的唯一闸（见 `CardDef.Targeted` 的 doc：
            /// 原版 `RawCardScript__IsValidTarget.c:8-44` 只对要选目标的卡生效；全池有 3 张
            /// `targeted=0` 的卡也带 `cardTargetCriteria`，没有这一位就会筛错目标池）。
            /// ⇒ `[System.Obsolete]` 已摘掉。缺省 `-1` = 未知（旧表/真值没这张卡），⛔ 不是 `false`。</para></summary>
            public int targeted = -1;

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

            /// <summary>🆕 v20（2026-10-24 · 引擎线 `A12`）：**逐 `abilityLogic` 的 `furtherMultiplier`**
            /// （原版 `AbilityLogic.furtherMultiplier`，`+0x38`），喂
            /// <see cref="CardDef.AbilityTargetSpec.FurtherMultiplier"/> → `EffectText.AssignAbilityTargets`
            /// → `EffectOp.AbilityFurtherMultiplier`。
            ///
            /// 形状 = `[{"trigger":…, "fm":[…]}]`，**与 `abilityTargets` 逐条同形**（同一批 ability / logic）；
            /// 判据 = `AbilityLogic__PlayAbility.c:235-236` 的 `local_178 *= furtherMultiplier`。
            /// 🔴 **每张卡都落**（没 ability = `[]`）；缺列 = 真值里没有这张卡 = **未知**。
            /// ⚠️ `-1` = 缺键/坏值；`0` = **原版明确「不乘」**（合法取值）。</summary>
            public AbilityMultiplierDto[] abilityMultipliers;

            /// <summary>🆕 v36（2026-10-28 · 执行代理 `A1c`）：**逐行 `abilityLogic[].damageCriteria`**
            /// （`minDamage` / `maxDamage` / `valueOptions` / `damageType`）。
            ///
            /// <para>为什么新增：`valueOptions == 90 (RandomizedOnResolution)` 的**逐目标掷**只能靠这一位分流，
            /// 而 `CardDef.ConditionSpec` 的十种 role 里**没有** `damage`（`CardDef.cs:3579-3583`）。
            /// 判据 → `资料/完全复刻/判据/B88_R1_90档_逐卡施工单.md` §二·3 / §三·4。</para>
            ///
            /// <para>形状 = `[{"ab":i,"lg":j,"min":…,"max":…,"opt":…,"type":…}]`（行类型 →
            /// <see cref="AbilityDamageRowDto"/>）。🔴 **每张卡都落**（没有 ability = `[]`）；
            /// 缺列（v35 及更早）= **未知**。⚠️ 只有 damage 族（`chosenAbility` ∈ 10/12/13/15）才被消费。</para></summary>
            public AbilityDamageRowDto[] abilityDamage;

            /// <summary>🆕 v23（2026-10-24 · 引擎线 `G12`）：**主动能力整条链** ——
            /// 原版 `activeAbility` / `alternativeAbility` 两个对象里 `activeAbilityLogic[]`
            /// **逐行**的 `targetsAffected` / `furtherMultiplier`（形状 → <see cref="ActiveAbilityLogicDto"/>）。
            ///
            /// <para>判据（逐行读过反编译）：`RawCardScript__ResolveActiveAbility.c:18/26-31` 选对象、
            /// `:50` 取那个对象的 `activeAbilityLogic`、`:58-67` 逐行 `PlayAbility(…, 400, 0)`；
            /// 目标 = `AbilityLogic__PlayAbility.c:262`、倍数 = `:235-236`。
            /// `hasActiveAbility == 0` 的卡整条路径被 `CardScript__ResolveActiveAbilityPlayed.c:22`
            /// 短路 ⇒ 数据照落、带 `gate = 0`（**本 build 死数据**，⛔ 不是「没有这份数据」）。</para>
            ///
            /// <para>🔴 **每张卡都落这一列**（没有 active/alt 行的 995 张落 `[]`）——
            /// 「缺列（旧表 = 未知）」与「空数组（明确没有）」必须分得开（同 `abilityTargets`）。
            /// ⚠️ **别把它并进 `abilityTargets`**：那一列的契约是「按 `cardAbilities` 下标对齐」，
            /// 混进来会打坏 `EffectText.AssignAbilityTargets` 的 G1 闸门（判据 →
            /// `资料/普查产出_1024/D10_abilityTargets管线.md` §三）。</para></summary>
            public ActiveAbilityLogicDto[] activeAbilityLogic;

            /// <summary>🆕 v25（2026-10-24 · 执行代理 `E9` · `A2_引擎战斗判据总表.md` Q7）：
            /// **logic / op 级 `targetCriteria`（两条支）** —— 原版
            /// `activeAbility.activeAbilityLogic[j].targetCriteria` 与
            /// `alternativeAbility.activeAbilityLogic[j].targetCriteria` 的**其余 36 格**
            /// （`conditionsFilter` / `excludeCondition` 已由 `unitConditions` 落成那一行的 `cond`/`ex`）。
            ///
            /// <para>形状 = `[{"ab":-1|-2, "lg":j, "criteria":{36 格 + playerTurn}}]`
            /// （行类型 → <see cref="TargetCriteriaRowDto"/>）。`ab = -1` active / `-2` alternative，
            /// 与 `activeAbilityLogic` / `unitConditions` **同词**；`lg` = 该能力对象里
            /// `activeAbilityLogic` 的下标。</para>
            ///
            /// <para>判据：`AbilityLogic.cs:16-17`（`targetCriteria` 字段）·
            /// `AbilityLogic__PlayAbility.c:262`（**每行**拿自己的 criteria 去 `GetTargets`）·
            /// `AbilityLogic__GetTargets.c` + `FilterMethods__CheckIfMeetsCriteria.c`（逐字段筛）。
            /// 🔴 我们原来只落了 `targetsAffected` 一格（→ `activeAbilityLogic[].ta`），
            /// 其余 35 格一行都没有 ⇒ 「目标 = 己方全体」那条近似缺的就是这一列。</para>
            ///
            /// <para>🔴 **每张卡都落**（没有行 = `[]`）；缺列（v24 及更早的表）= **未知**
            /// （⛔ 不是「原版说没有目标」，见 `< 25` 那条守卫）。
            /// ✅ 2026-10-24（执行代理 `W2`）：`CardDef` 消费者**已接** —— 见
            /// <see cref="CardDef.ActiveAbilityTargetCriteriaAt"/>。</para></summary>
            public TargetCriteriaRowDto[] activeAbilityTargets;

            /// <summary>🆕 v25（2026-10-24 · 执行代理 `E9` · `A2` Q7）：**普通 ability 的 logic 级
            /// `targetCriteria`（36 格）** —— 原版
            /// `cardAbilities[i].abilityLogic[j].targetCriteria`（`AbilityLogic.cs:16-17`）。
            ///
            /// <para>形状 = `[{"ab":i, "trigger":…, "lg":j, "criteria":{36 格 + playerTurn}}]`
            /// （行类型 → <see cref="TargetCriteriaRowDto"/>）。
            /// 🔴 **比 `A2` Q7 的建议多一个 `ab`**：实测 **64 张池内卡**（真值 89 张）有两条 ability
            /// **共用同一个 `trigger`** ⇒ `(trigger, lg)` **不唯一**，只按它对齐会串行（`ab` = `cardAbilities` 下标）。</para>
            ///
            /// <para>🔴 **每张卡都落**（没有 ability = `[]`）；缺列（v24 及更早）= **未知**。
            /// ⚠️ `trigger` 只作可读性（消费端按 `ab`/`lg` 对齐）；`-1` = 该 ability 的 trigger 是坏值。
            /// ✅ 2026-10-24（执行代理 `W2`）：`CardDef` 消费者**已接** —— 见
            /// <see cref="CardDef.AbilityTargetCriteriaAt"/>。</para></summary>
            public TargetCriteriaRowDto[] abilityTargetCriteria;

            /// <summary>🆕 v26（2026-10-25 · 执行代理 `E2` · `A3-2`）：**ability 级 `targetCriteria`
            /// 的其余 36 格** —— 原版 **`CardAbility.targetCriteria`（`+0x30`）**，🔴 **不是**
            /// 上面那一列（`AbilityLogic.targetCriteria`，logic 级）。两者**同名但不同层**。
            ///
            /// <para>形状 = `[{"ab":i, "trigger":…, "criteria":{36 格}}]`
            /// （行类型 → <see cref="TargetCriteriaRowDto"/>；`lg` **不落** ⇒ 恒 `-1`，
            /// 因为这一列**只有** ability 级那一档）。`ab` = `cardAbilities` 下标。</para>
            ///
            /// <para>判据：`CardAbility__CanTriggerAbility.c:131-135`
            /// `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`
            /// （形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` ⇒ 被筛对象 = **`targetCard`**，
            /// ⛔ 不是 `actingCard` —— 那是第一道闸 `+0x28` 的候选）。
            /// `targetsAffected == 0 (any)` ⇒ 原版 `return 1`（零信息）⇒ 生成器**不落**那一行。</para>
            ///
            /// <para>🔴 **每张卡都落**（没有行 = `[]`）；缺列（v25 及更早）= **未知**
            /// （⛔ 不是「原版说没有触发条件」）。
            /// ✅ 消费端 = <see cref="CardDef.AbilityLevelCriteriaForTrigger"/>（**唯一入口**）→
            /// `ActionPump.EnqueueAbility` 的触发前闸。
            /// 🆕 **v29（2026-10-25 · 执行代理 `X6` · 口径乙）**：每行多带一格 **`ca`（能力种类族名）**
            /// —— `ab` 定位不唯一（23 张卡 `trigger 0` 有两条 ability）时，消费端按家族把行认回来。
            /// 三档取值 → <see cref="CardDatabase.TargetCriteriaRowDto.ca"/> 的 doc；
            /// 消费端 = <see cref="CardDef.AbilityLevelCriteriaForFamily"/> →
            /// `RuleCore.AbilityFamilyTargetAllows` → `EffectResolver` 出牌正文的逐 op 闸。</para></summary>
            public TargetCriteriaRowDto[] abilityLevelCriteria;

            /// <summary>🆕 v27（2026-10-25 · 执行代理 `E3` · `E2` 报告 §六）：**ability 级
            /// `actingCardCriteria` 的其余 36 格** —— 原版 **`CardAbility.actingCardCriteria`（`+0x28`）**，
            /// 🔴 与上面那一列（`CardAbility.targetCriteria`，`+0x30`）是**同一层的两道闸、两个候选**。
            ///
            /// <para>形状 = `[{"ab":i, "trigger":…, "criteria":{36 格}}]`
            /// （行类型 → <see cref="TargetCriteriaRowDto"/>；`lg` **不落** ⇒ 恒 `-1`）。
            /// `ab` = `cardAbilities` 下标。</para>
            ///
            /// <para>判据：`CardAbility__CanTriggerAbility.c:127-130`
            /// `CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`
            /// （形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` ⇒ 被筛对象 = **`actingCard`**，
            /// 短路次序 = **acting(0x28) → target(0x30) → board(0x38)**）。
            /// `targetsAffected == 0 (any)` ⇒ 原版 `return 1`（零信息）⇒ 生成器**不落**那一行。</para>
            ///
            /// <para>🔴 **每张卡都落**（没有行 = `[]`）；缺列（v26 及更早）= **未知**
            /// （⛔ 不是「原版说没有这条条件」）。
            /// ✅ 消费端 = <see cref="CardDef.ActingLevelCriteriaForTrigger"/>（**唯一入口**）→
            /// `ActionPump.EnqueueAbility` 的**第一道闸**。</para></summary>
            public TargetCriteriaRowDto[] abilityActingCriteria;

            /// <summary>🆕 v28（2026-10-25 · 执行代理 `E4` · `E4-2`）：
            /// **`ActiveAbility.targetCriteria`（`+0x38`）的其余 36 格** —— 主动能力的**目标闸**
            /// （「这个格位能不能被点」）。
            ///
            /// <para>形状 = `[{"ab":-1|-2, "criteria":{36 格}}]`（行类型 → <see cref="TargetCriteriaRowDto"/>；
            /// **没有 `lg`**（对象级）⇒ 缺键 `-1`；也没有 `trigger`）。</para>
            ///
            /// <para>判据：`RawCardScript__IsValidActiveAbilityTarget.c:103-107`
            /// `CheckIfMeetsCriteria(thisCard, 候选, 候选, *(activeAbility+0x260)+0x38, 0)`；
            /// 调用者 = `BattleManager.IsValidActiveAbilityTarget`（高亮 / 点选 / AI / 最后一刻复核）。
            /// `targetsAffected == 0 (any)` ⇒ 原版 `return 1`（零信息）⇒ 生成器**不落**那一行。</para>
            ///
            /// <para>🔴 **与 `activeAbilityTargets`（v25）不是同一格**：那一列是
            /// `activeAbilityLogic[j].targetCriteria`（**logic 级**、带 `lg`），本列是
            /// `ActiveAbility.targetCriteria`（**对象级**、只有 `ab`）。⛔ 别合并。</para>
            ///
            /// <para>🔴 **每张卡都落**（没有行 = `[]`）；缺列（v27 及更早）= **未知**
            /// （⛔ 不是「原版说没有这一格条件」）。
            /// ✅ 消费端 = <see cref="CardDef.ActiveAbilityPickCriteriaFor"/>（**唯一入口**）→
            /// `RuleCore.CanUseAbility` 的主动能力目标闸。</para>
            ///
            /// <para>🔴🔴 **可达性（2026-10-25 现读，如实标着 —— ⛔ 别把这条当成「已经验过真卡」）**：
            /// · 这道闸挂在 `RuleCore.CanUseAbility` 上，而那个口要求 `Card.Ability != null`
            ///   （正文里有一条 `Ability:`）⇒ 现读**全池 0 张**卡有解析成功的 `Ability:`
            ///   ⇒ **今天一张真卡都走不到**（`CanStartAbility` 先回 `ErrNoAbility`）。
            ///   闸是为「**结构化 `ActiveAbility` 执行**」那一批**预置**的。
            /// · 而且它只对 **`enemyUnits == 1` 的 48 张**忠实生效 —— 我们的候选**恒为敌方单位**
            ///   （`CanUseAbility` / 表现层 `CommandTargetSide` 都只查对手棋盘）⇒ 只有这一档我们的候选集
            ///   是原版候选集的**子集**；其余（34 张 `ta=20 self` + 十几张只打友方）**放行 + 出声**
            ///   （⛔ 不拿「不在候选集里」当「不满足」，那会把那批主动能力整条变成不可用）。</para></summary>
            public TargetCriteriaRowDto[] activeAbilityPickCriteria;

            // ==================================================================================
            //  🆕 v21（2026-10-24 · 引擎线 `A20` · 第三批 ①/②）：主动能力的**分支闸 / 装填位 / 任务位**
            // ==================================================================================
            //  判据与落值口径 → `工具/gen_cards_engine.py` 的 `ABILITY_GATE_REPORT` 上面那一段；
            //  消费端 = `ConditionEval` 的 280 `siege` / 320 `hasQuestCompleted`
            //  ＋ `CardDef.UsesAlternativeAbility`（分支选择）。
            //
            //  🔴 **六列全是「只在非默认时才落」** —— `0` 是**合法取值**（假 / 无）。
            //      ⇒ 缺键 = `0`，**不是**未知；只有生成器明确写 `-1` 才是「真值里查不到这张卡」。
            //      ⚠️ 与 `spellType`（缺键 = `-1`）的口径**不同**，别照抄：那边 `0` 是 `Normal`
            //      这个具体枚举值，这边 `0` 就是「不成立」。

            /// <summary>🆕 v21：原版 `CardScript.hasActiveAbility`（`+0x220`，由卡数据
            /// `RawCardScript.hasActiveAbility(+0x2A9)` 灌）。`0` = 没有主动能力；`-1` = 真值里没这张卡。
            /// <para>🔴 **⛔ 别拿 `CardDef.Ability != null` / 卡面 `Ability:` 文本顶它** ——
            /// 池内 `desc` 含 `Ability:` 的卡 **0 张**，而真值 `hasActiveAbility == 1` 有 **87 张**
            /// （判据 → `资料/普查产出_1023e/C2_引擎线判据_卡表加载位与字段.md` F-01）。</para>
            /// 读点 = `CardDef.HasActiveAbility` → `ConditionEval` 的 280 / 190。</summary>
            public int hasActiveAbility;

            /// <summary>🆕 v21：原版 `ActiveAbility.needsLoading`（**`ActiveAbility` 对象内部**的 `+0x22`）。
            /// ⚠️ 那个 `+0x22` **不是** `CardScript` 的偏移（`dump.cs:35444-35449` 那一批
            /// `k__BackingField` 也在 0x220 附近 —— 两者只是数字巧合）。
            /// `0` = 不需要装填位；`-1` = 真值里没这张卡。现读 1,351 张**全是 0**。</summary>
            public int activeNeedsLoading;

            /// <inheritdoc cref="activeNeedsLoading"/>
            public int alternativeNeedsLoading;

            /// <summary>🆕 v21：原版 `RawCardScript.hasAlternativeAbility`（`+0x2AA`）。
            /// 真=1 时才可能走替代支（判据 `RawCardScript__GetActiveAbility.c` 全文 17 行）。
            /// `-1` = 真值里没这张卡。现读池内**只有 `SW21` 一张是 1**。</summary>
            public int hasAlternativeAbility;

            /// <summary>🆕 v21：原版 `RawCardScript._quest`（`bool @+0x240`）。`0` = 不是任务卡；
            /// `-1` = 真值里没这张卡。现读 1,351 张**全是 0**（数据事实，⛔ 不是「判不了」）。</summary>
            public int quest;

            /// <summary>🆕 v21：原版 `RawCardScript.questTimes`（`int @+0x7C`）——
            /// **另一条链**用（`EntityScript__ReachedQuestCounter.c:5-14`：`questTimes > 1` 时
            /// 每次触发把 `questCounter(+0x270)` +1，到 `questTimes` 才达成），
            /// **不在 320 的求值链里**。现读全是 0。</summary>
            public int questTimes;

            /// <summary>🆕 v21：原版 `RawCardScript.normalAbilityBoardCriteria`（`BoardCriteria @+0x2B0`）
            /// —— 「这次该走**普通** `activeAbility` 还是**替代** `alternativeAbility`」的板面判据。
            ///
            /// <para>🔴 **只落「非默认」的那几张**（现读 2 张：`SW9` / `SW21`）。缺席 = 原版那个
            /// 全 0 的默认 criteria —— 它在 `BoardAnalysis__MeetsCriteria.c` 里走到
            /// `:101`（`countCriteria.targetsAffected == 0(any)`）**立即为真** ⇒ 行为上与
            /// 「没有这一列」等价，⛔ 不是「判不了」。</para>
            /// <para>形状 = `{countOptions, triggerIf, referenceValue, countSet}`，
            /// **`countSet` 与 `unitConditions[].countSet` 逐字同形**（`playerTurn` 就在里面）。
            /// 语义 → <see cref="CardDef.BoardGateSpec"/>。</para></summary>
            public AbilityGateDto normalAbilityBoardCriteria;

            /// <summary>🆕 v31（2026-10-26 · 执行代理 `B8_AH` · 账 `战斗逻辑驱动.tsv` 的 `IsKeyword` 行）：
            /// 原版 `RawCardScript.keyword`（`int @+0x98`，`Keyword` 枚举：`none 0 / Infantry 5 /
            /// Vehicle 10 / Monster 15 / Building 20 / Battlesuit 25 / Drone 30 / Beast 35 / Daemon 40`，
            /// `il2cpp_out/dump.cs:45899-45912`）。**缺列 / 真值里没这张卡 = `-1`（未知）**。
            ///
            /// <para>🔴 **⛔ 别拿 `subtype`（卡面那行橙字）顶它** —— 原版
            /// `RawCardScript.IsKeyword(Keyword)` 就是 `*(int*)(raw+0x98) == param`
            /// （`decomp_full/RawCardScript__IsKeyword.c:5-7`，全文 7 行），两者在全池 **4 张**上不一致
            /// （`BL1` SO=Daemon 而卡面印 Infantry；`EC55/56/57` SO=Infantry 而卡面印 Warlord）。
            /// `subtype` 继续只当**显示**用。判据全文 → `资料/完全复刻/报告/B8_Q_trait口径与卡定义.md` §2.2。</para>
            /// <para>读点 = `CardDef.AttachKeywordId` → `CardDef.IsKeyword` →
            /// `ConditionEval` 的条件 **275 / 276 / 277**（`isWarlordOrVehicle` /
            /// `isBattlesuitOrVehicle` / `isInfrantryOrBeast`）。</para></summary>
            public int keyword = -1;

            /// <summary>🆕 v33（2026-10-27 · 批次 9 · 执行代理 `E7ENG` · 账 532 的 1a）：
            /// 原版 `CardEffect.trait`（`+0x3C`）的**数据来源** —— 这张卡**附带魔效果**自带的
            /// `DefinedTrait` 关键词名（今天只有 `pray` / `vulnerable`）。
            /// 🔴 **不是附魔卡自己的 trait**（`Pray Enchantment` 模板 `cardTraits = []`，
            /// `1185` 写在**挂它的那张卡**的 buff 上）⇒ 所以列在**载体卡**身上。
            /// 三态：`null` = **未知**（旧表 &lt; v33 整列没落）· `""` = **明确没有** · 其它 = 名字。
            /// 语义 / 判据 / 消费链 → `CardDef.EnchantTrait` 的 doc（⛔ 别在这里抄第二份）。</summary>
            public string enchantTrait;

            /// <summary>🆕 v34（2026-10-27 · 批次 9 · 执行代理 `E7ENG` · 第三批）：
            /// 原版 `RawCardScript.cardType`（`int @+0x94`；`CardTypeOptions` =
            /// `Minion 0 / Hero 10 / Tactic 20 / Whispers 40`）。
            /// 消费点 = `CardDef.IsTrapCard`（原版 `EntityScript__get_isTrapCard.c` =
            /// `cardType == 20` ∧ `spellType == 70`）→ `CardInstance.ArmedTrap`。
            /// `-1` = **未知**（旧表 &lt; v34 整列没落 / 这张卡不在真值快照里）—— ⛔ 别当成 `Minion(0)`；
            /// 🔴 **别拿 `type` 字符串反推**（`Pray Enchantment` 真值 `cardType = 20` 而 `type = "enchantment"`）。
            /// 语义 → `CardDef.CardType` 的 doc（⛔ 别在这里抄第二份）。</summary>
            public int cardType = -1;

            /// <summary>🆕 v35（待生成器落列；2026-10-27 · `B9_W65ABIL`）：原版 `ActiveAbility` 对象自己的四个标量
            /// `manaCost` / `psychicCost` / `plasmaCost` / `targeted`（`Assembly-CSharp/ActiveAbility.cs`）。
            /// ⚠️ 本列今天**还不存在**（卡表 v34）⇒ 停在缺键初值 `-1` = **未知**（与 `cardType` 同一写法）；
            /// `0` 是合法值（`SOR66` 的 `manaCost` 真值 = 0）⇒ ⛔ 别把缺键读成 0。语义 → `CardDef.ActiveAbilityManaCost` 那一族。</summary>
            public int activeAbilityManaCost = -1;
            public int activeAbilityPsychicCost = -1;
            public int activeAbilityPlasmaCost = -1;
            public int activeAbilityTargeted = -1;
        }

        /// <summary>🆕 v21（2026-10-24 · 引擎线 `A20`）：`normalAbilityBoardCriteria` 的传输形状
        /// （**字段名与 JSON 键名逐字一致**，`JsonUtility` 直读；语义 → <see cref="CardDef.BoardGateSpec"/>）。
        /// <para>缺键哨兵 = `UnitConditionDto.MissingIndex`（`-99`）—— 同 `CountSetDto` 的理由
        /// （`0` 到处都是合法值）。</para></summary>
        [System.Serializable]
        public class AbilityGateDto
        {
            const int MissingIndex = UnitConditionDto.MissingIndex;
            /// <summary>原版 `BoardCriteria.countOptions`（`0 AllTargets` / `10 EnemiesMinusAllies`）。</summary>
            public int countOptions = MissingIndex;
            /// <summary>原版 `BoardCriteria.triggerIf`（`StatFilterOptions`：1 EqualTo / 2 EqualOrHigher / …）。</summary>
            public int triggerIf = MissingIndex;
            /// <summary>原版 `BoardCriteria.referenceValue`（拿来比的那个数）。</summary>
            public int referenceValue = MissingIndex;
            /// <summary>原版 `BoardCriteria.countCriteria` ＋ `playerTurn` —— 与 `countSet` 列同形。</summary>
            public CountSetDto countSet;
        }

        /// <summary>🆕 v20：`abilityMultipliers` 的一行（**字段名与 JSON 列名逐字一致**）。
        /// 语义 → <see cref="CardDef.AbilityTargetSpec.FurtherMultiplier"/>。</summary>
        [System.Serializable]
        public class AbilityMultiplierDto
        {
            /// <summary>`-1` = JSON 里没有 `trigger`（只作可读性；消费端按**下标**对齐）。</summary>
            public int trigger = -1;
            /// <summary>逐 `abilityLogic` 的 `furtherMultiplier`；`-1` = 该格坏值/缺键。</summary>
            public int[] fm;
        }

        /// <summary>🆕 v36（2026-10-28 · 执行代理 `A1c`）：`abilityDamage` 的一行
        /// （= 原版 `AbilityLogic.damageCriteria`）。字段名与 JSON 键名**逐字一致**。
        /// <para>⚠️ 四格都用 `-1` 当「坏值/缺键」哨兵 —— `0` 是**合法取值**
        /// （`valueOptions = Normal` / `damageType = Normal` / `minDamage = 0`）⇒ ⛔ 别把缺键读成 0。</para>
        /// <para>语义 → <see cref="CardDef.AbilityDamageSpec"/>；判据 → `判据/B88_R1_90档_逐卡施工单.md`。</para></summary>
        [System.Serializable]
        public class AbilityDamageRowDto
        {
            /// <summary>`cardAbilities` 下标（`-1` = 缺键）。</summary>
            public int ab = -1;
            /// <summary>`abilityLogic` 下标（`-1` = 缺键）。</summary>
            public int lg = -1;
            /// <summary>原版 `damageCriteria.minDamage`。</summary>
            public int min = -1;
            /// <summary>原版 `damageCriteria.maxDamage`。</summary>
            public int max = -1;
            /// <summary>原版 `damageCriteria.valueOptions`（`0` = 固定 / `80` = `min+max×mult` / `90` = 区间逐目标）。</summary>
            public int opt = -1;
            /// <summary>原版 `damageCriteria.damageType`（`5/10/12` = 随机三档）。</summary>
            public int type = -1;
        }

        /// <summary>🆕 v23（2026-10-24 · 引擎线 `G12`）：`activeAbilityLogic` 的一行
        /// （**一个元素 = 一个能力对象**；`ta` / `fm` 是**逐 logic** 的数组）。
        /// 字段名与 JSON 键名**逐字一致**（`JsonUtility` 直读）。语义 → <see cref="CardDef.ActiveAbilityLogicSpec"/>。
        ///
        /// <para>🔴 **`ab` / `gate` 用 `MissingIndex`（-99）当缺键哨兵，⛔ 别退回 0 或 -1** ——
        /// `-1` / `-2` 都是合法的 `ab`（active / alternative），`-1` 也是合法的 `gate`（闸未知）。
        /// 缺键静默读成 `-1` 会把「JSON 里没这个键」认成「alternativeAbility」/「闸未知」——
        /// 前者**错对象**、后者**永远不挂**，两种都是静默错。</para>
        /// <para>`ta` / `fm` 的坏值口径**与 `abilityTargets` / `abilityMultipliers` 逐字相同**：
        /// 非整数/缺键 ⇒ 该格 `-1`（`0` 是合法取值：`any` / 「不乘」）。</para></summary>
        [System.Serializable]
        public class ActiveAbilityLogicDto
        {
            /// <summary>缺键哨兵 —— 唯一来源 <see cref="UnitConditionDto.MissingIndex"/>（`-99`）。
            /// 理由：`-1` / `-2` 都是合法 `ab`、`-1` 也是合法 `gate`（同 `AbilityGateDto` 的写法）。</summary>
            const int MissingIndex = UnitConditionDto.MissingIndex;

            /// <summary>`-1` = `activeAbility` · `-2` = `alternativeAbility`（与 `unitConditions` 的 `ab` 同词）。
            /// `MissingIndex` = JSON 里没有这个键（坏行 ⇒ 出声丢弃）。</summary>
            public int ab = MissingIndex;
            /// <summary>`1` 可达 · `0` 本 build 死数据 · `-1` 闸未知。`MissingIndex` = 缺键（坏行）。</summary>
            public int gate = MissingIndex;
            /// <summary>原版逐行传的**虚拟时机**（恒 `400` = `ThisUnitAbilityUsed`）；只作可读性。</summary>
            public int trigger = -1;
            /// <summary>该能力对象的 `needsLoading`；`-1` = 未知。</summary>
            public int needsLoading = -1;
            /// <summary>逐行 `targetsAffected`；`-1` = 该格坏值/缺键（⛔ `0` 是合法的 `any`）。</summary>
            public int[] ta;
            /// <summary>逐行 `furtherMultiplier`；`-1` = 该格坏值/缺键（`0` = 原版明确「不乘」）。</summary>
            public int[] fm;
            /// <summary>🆕 v24（2026-10-24 · 执行代理 `E1`）：逐行 `chosenAbility`（原版 `AbilityEffect` 枚举）。
            /// `-1` = 该格坏值/缺键（`0` = `drawCard` 合法）；缺列（v23 旧表）⇒ `null` = 未知。</summary>
            public int[] ca;
            /// <summary>🆕 v24：逐行 `playersAffected`（`0` = 牌/单位 · `10` = 玩家自己）。
            /// `-1` = 该格坏值/缺键；缺列 ⇒ `null` = 未知。</summary>
            public int[] pa;
            /// <summary>🆕 v24：逐行 `damageCriteria`（`{min,max,opt,type}`）；元素 `null` = 该行未知，
            /// 整个缺列 ⇒ `null` = 未知。⚠️ 只有 damage 族（`ca` ∈ 10/12/13/15）才读。</summary>
            public ActiveAbilityDamageDto[] dmg;
        }

        /// <summary>🆕 v24（2026-10-24 · 执行代理 `E1`）：`activeAbilityLogic[j].damageCriteria` 的原值。
        /// 字段名与 JSON 键名**逐字一致**（`JsonUtility` 直读）。语义 → <see cref="CardDef.ActiveAbilityDamageSpec"/>。
        /// <para>🔴 四格都用 `MissingIndex`（-99）当缺键哨兵 —— `0` 在四格上**都是合法取值**
        /// （`0` 伤害 / `valueOptions=0` = 固定 / `damageType=0` = 普通）。缺键静默读成 0 会把
        /// 「JSON 里没有这一格」认成「0 点伤害」—— 静默错。</para></summary>
        [System.Serializable]
        public class ActiveAbilityDamageDto
        {
            const int MissingIndex = UnitConditionDto.MissingIndex;
            /// <summary>原版 `damageCriteria.minDamage`。</summary>
            public int min = MissingIndex;
            /// <summary>原版 `damageCriteria.maxDamage`。</summary>
            public int max = MissingIndex;
            /// <summary>原版 `damageCriteria.valueOptions`。</summary>
            public int opt = MissingIndex;
            /// <summary>原版 `damageCriteria.damageType`。</summary>
            public int type = MissingIndex;
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

        /// <summary>🆕 v25（2026-10-24 · 执行代理 `E9`）：`activeAbilityTargets` / `abilityTargetCriteria`
        /// **共用**的一行 —— 一行的 36 格装进子对象 <see cref="criteria"/>（类型 <see cref="CountSetDto"/>）。
        ///
        /// <para>🔴 **为什么 36 格装子对象、不平铺在行上**：C# 侧 36 格的字段表**只有一份**
        /// （<see cref="CountSetDto"/>，与 `unitConditions[].countSet` 共用）。平铺就必须在这里再抄一份
        /// ⇒ 「两处写同一条规则 = 迟早不一致」。JSON 那边由生成器的 `criteria_set_of()` 统一写（同一份字段表）。</para>
        ///
        /// <para>🔴 **`ab` / `lg` 的缺键哨兵**：
        /// · `ab` 用 `MissingIndex`（-99）—— `-1` / `-2` **都是合法值**（active / alternative）；
        /// · `lg` 用 `-1` —— 这两列的行**都是 logic 级**（`lg >= 0`），没有「直接挂在对象上」那一档；
        /// · `trigger` 用 `-1`（只有 `abilityTargetCriteria` 有这一格；`activeAbilityTargets` 没有 ⇒ 恒 -1）。
        /// 缺键静默读成 0 会落到**第 0 条 ability / 第 0 条 logic** = 静默串行。</para>
        ///
        /// <para>⚠️ 每张卡都落这两列（没有行 = `[]`）；缺列（v24 及更早）= `null` = **未知**。</para>
        /// </summary>
        [System.Serializable]
        public class TargetCriteriaRowDto
        {
            const int MissingIndex = UnitConditionDto.MissingIndex;
            /// <summary>`cardAbilities` 下标；主动能力那一列是 `-1` active / `-2` alternative。
            /// `MissingIndex`（-99）= JSON 里没有这个键（坏行）。</summary>
            public int ab = MissingIndex;
            /// <summary>该 ability 的 `trigger`（只作可读性；`activeAbilityTargets` 的行没有这一格 ⇒ `-1`）。</summary>
            public int trigger = -1;
            /// <summary>`abilityLogic` 下标（`>= 0`）；`-1` = 缺键/坏行。</summary>
            public int lg = -1;
            /// <summary>🆕 v29（2026-10-25 · 执行代理 `X6` · 口径乙）：该行所属 ability 的
            /// **能力种类族名**（原版 `cardAbilities[ab].abilityLogic[*].chosenAbility` 的家族，
            /// 表 = <see cref="CardDef.ActiveAbilityKindFamilyNames"/>）。
            ///
            /// <para>🔴 **三档，⛔ 别合并**（判据 → `<see cref="CardDef.TargetCriteriaRowSpec.Family"/>`）：
            /// `null` = **JSON 里没有这个键**（坏行/旧生成器 ⇒ 出声）；`""` = **家族判不了**
            /// （同一 ability 混了两种家族 / 一条 logic 都没有）；其余 = 族名（可判）。</para>
            /// <para>⚠️ 目前**只有 `abilityLevelCriteria` 这一列**生成器会写它（那条闸需要按家族
            /// 把行认回来 —— 23 张卡 `trigger 0` ability 定位不唯一）。别的列留 `null` = 没落。</para></summary>
            public string ca;
            /// <summary>这一行的 36 格（原版 `TargetCriteria`，除 `conditionsFilter`/`excludeCondition`）。
            /// `null` = 缺键/坏行。逐字段语义 → <see cref="CardDef.CountSetSpec"/> 的 doc
            /// （⚠️ 那一份是**被数集合**用途；字段同源，语义解释按 `TargetCriteria` 走）。</summary>
            public CountSetDto criteria;
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
            /// <summary>🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`role == "mult"` 行的
            /// **整条 criteria**（原版 `abilityLogic[].multiplier`）。`null` = 没落/旧表。
            /// 语义 / 判据 → <see cref="CardDef.ConditionSpec.MultiplierSet"/> 的 doc。</summary>
            public CountSetDto multiplierSet;
            /// <summary>🆕 v22：`role == "multi"` 行（原版 `CardAbility.multiCountCriteria`）。
            /// → <see cref="CardDef.ConditionSpec.MultiSet"/>。</summary>
            public CountSetDto multiSet;
            /// <summary>🆕 v22：`role == "avail"` 行（原版 `RawCardScript.targetsAvailableCounter`）。
            /// → <see cref="CardDef.ConditionSpec.AvailSet"/>。</summary>
            public CountSetDto availSet;
            /// <summary>🆕 v22：`role == "choice"` 行（原版 `RawCardScript.choicesDefinition`）。
            /// → <see cref="CardDef.ConditionSpec.ChoiceSet"/>。</summary>
            public CountSetDto choiceSet;
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
        public static void InvalidateCache() { _cache = null; InvalidateEnchantmentGrants(); InvalidateOffensiveEnvCards(); }   // 🆕 B93/E16：第三张表（进攻卡）一并丢 —— 同一句里挤着写是故意的（加行会让下游 A 行账落点整体漂移）

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
            _cache = Parse(asset.text); EnsureEnchantmentGrantsFrom(asset.text);   // 🆕 v38（B90/B3）：顺手把 `enchantment` 列建成反查表（⛔ 不新增一次解析：复用这一份文本）
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

            // 🆕 v20（2026-10-24 · 引擎线 `A12`）：`abilityMultipliers` 一列
            //   （原版 `AbilityLogic.furtherMultiplier`，`PlayAbility.c:235-236` 的 `value *= fM`）。
            //   缺列 ⇒ `CardDef.AbilityMultipliersVersioned = false` + 每格 `null` ⇒ 消费端**不乘**
            //   （= 改前的行为，⛔ 不是「数据说 fM 全是 0」）。
            if (dto.version < 20)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 20 —— 没有 `abilityMultipliers` 一列"
                               + "（原版 `AbilityLogic.furtherMultiplier`：`PlayAbility.c:235-236` 的 "
                               + "`if (0 < fM) value *= fM`）。`CardDef.AbilityTargetSpec.FurtherMultiplier` "
                               + "会一律是 **null = 未知** ⇒ `EffectResolver` 那一步**不乘**"
                               + "（= 改前的行为，⛔ 不是「数据说 fM 全是 0」）。"
                               + "跑一下 gen_cards_engine.py 重生成（引擎线 `A12`）。");

            // 🆕 v21（2026-10-24 · 引擎线 `A20` · 第三批 ①/②）：主动能力的**分支闸 / 装填位 / 任务位**。
            //   缺列 ⇒ `CardDef.HasActiveAbility` / `HasAlternativeAbility` / `Quest` 一律 `-1`（未知）
            //   ⇒ `ConditionEval` 的 280 / 320 照旧返回 **null（判不了）**，⛔ 不是「全池都假」
            //   （拿「表是旧的」当「数据说假」正是本仓红线里最贵的那一类静默错）。
            if (dto.version < 21)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 21 —— 没有主动能力闸那六列"
                               + "（`hasActiveAbility` / `activeNeedsLoading` / `alternativeNeedsLoading` /"
                               + " `hasAlternativeAbility` / `quest` / `questTimes` /"
                               + " `normalAbilityBoardCriteria`）。`CardDef` 那边读到的是 `-1 = 未知`"
                               + "⇒ `UnitConditions` 的 280 `siege` / 320 `hasQuestCompleted` 会**继续返回 null**"
                               + "（⛔ 不是「判成假」）。跑一下 gen_cards_engine.py 重生成（引擎线 `A20`）。");

            // ⚠️ **只对 `15 <= version < 22` 出声**（同上面 v16 那条守卫的口径，2026-10-24 修复代理 P1 补）：
            //   `< 15` 的表**连 `unitConditions` 整列都没有** ⇒ 上面那条 `< 15` 的守卫已经说清「全未知」，
            //   再来一条只是重复；而且这条文案里**也写着 `unitConditions`** ⇒ 它会让
            //   `RuleEngineTest` ④「旧表装载恰好一条 `unitConditions` 警告」**实得 2、红**
            //   （2026-10-24 `RuleEngineTest.Run` 就是这么红的，读数 → `资料/普查产出_1023e/P1_引擎三条红.md` §1）。
            if (dto.version >= 15 && dto.version < 22)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 22 —— `unitConditions` 的 "
                               + "`mult` / `multi` / `avail` / `choice` 四行**没有**整条 criteria"
                               + "（`multiplierSet` / `multiSet` / `availSet` / `choiceSet`）。"
                               + "`CardDef.ConditionSpec` 那四格读到的是 `null = 未知`"
                               + "⇒ 结构化乘数（`mult`）**不生效、退回正文口径**（⛔ 不是「没有乘数」）。"
                               + "跑一下 gen_cards_engine.py 重生成（引擎线 `新账-95` #2）。");

            // 🆕 v23（2026-10-24 · 引擎线 `G12`）：`activeAbilityLogic` 一列
            //   （原版 `activeAbility` / `alternativeAbility` 的 `activeAbilityLogic[]` 逐行 ta/fm）。
            //   缺列 ⇒ `CardDef.ActiveAbilityLogic = **null**（未知）` + `ActiveAbilityLogicVersioned = false`
            //   ⇒ 主动能力那批 op 的 `ActiveAbilityRow` / `SubjectTargetsAffected` / `AbilityFurtherMultiplier`
            //   全保持 `-1`（= 改前的行为）。⛔ 旧表**不许**被读成「全池都没有主动能力」
            //   （那正是本仓红线里最贵的那类静默错：缺列 ≠ 空数组）。
            if (dto.version < 23)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 23 —— 没有 `activeAbilityLogic` 一列"
                               + "（原版 `activeAbility.activeAbilityLogic[j].targetCriteria.targetsAffected` 与 "
                               + "`furtherMultiplier`；判据 `RawCardScript__ResolveActiveAbility.c:50-67`、"
                               + "`AbilityLogic__PlayAbility.c:235-236/262`）。"
                               + "`CardDef.ActiveAbilityLogic` 会是 **null = 未知** ⇒ 主动能力那批 op 的 "
                               + "`ActiveAbilityRow` / `SubjectTargetsAffected` / `AbilityFurtherMultiplier` "
                               + "一律保持 **-1（读不到）**（= 改前行为，⛔ 不是「原版说没有目标」）。"
                               + "跑一下 gen_cards_engine.py 重生成（引擎线 `G12`）。");

            // 🆕 v24（2026-10-24 · 执行代理 `E1`）：`activeAbilityLogic` 每行的 `ca` / `pa` / `dmg`
            //   （原版 `AbilityLogic.chosenAbility` / `playersAffected` / `damageCriteria`）。
            //   缺这三格 ⇒ `CardDef.ActiveAbilityLogicSpec.{ChosenAbility,PlayersAffected,Damage} = null`
            //   = **未知** ⇒ `ApplyActiveAbilityLogic` **只能**回到 v23 那一档（38 张「1 对象/1 行/1 op」），
            //   其余 48 张继续留 `-1`（⛔ 不按行号硬配）。
            //   ⛔ 旧表**不许**被读成「每行都没有能力种类」。
            if (dto.version < 24)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 24 —— `activeAbilityLogic` 每行"
                               + "没有 `chosenAbility` / `playersAffected` / `damageCriteria` 三格"
                               + "（原版 `AbilityLogic.chosenAbility` = `AbilityEffect` 枚举，判据 "
                               + "`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`；"
                               + "`AbilityLogic__PlayAbility.c:266` 按它 `switch` 分派）。"
                               + "`CardDef.ActiveAbilityLogicSpec.ChosenAbility / PlayersAffected / Damage` 会是 "
                               + "**null = 未知** ⇒ 主动能力的「行 ↔ op」只能按 v23 那一档"
                               + "（1 个对象 / 1 行 / 1 条 op）配；其余形状的 `ActiveAbilityRow` / "
                               + "`SubjectTargetsAffected` / `AbilityFurtherMultiplier` 保持 **-1（读不到）**，"
                               + "⛔ 不是「原版说没有目标」。"
                               + "跑一下 gen_cards_engine.py 重生成（执行代理 `E1`）。");

            // 🆕 v25（2026-10-24 · 执行代理 `E9` · `A2` Q7）：**logic / op 级 `targetCriteria`（36 格）**
            //   两列 —— `activeAbilityTargets`（主动能力两条支）与 `abilityTargetCriteria`（普通 ability）。
            //   缺列（v24 及更早）⇒ 两列都是 `null` = **未知**，而且**当前还没有 CardDef 消费者**
            //   （`Core/` 另批接）⇒ 这一条出声只是「表是旧的，这两列的数据还没进来」。
            //   ⛔ 旧表**不许**被读成「原版说这些 criteria 全是默认值」—— 那是静默错（同 v16 那次口径）。
            if (dto.version < 25)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 25 —— 没有 `activeAbilityTargets` /"
                               + " `abilityTargetCriteria` 两列（原版 `AbilityLogic.targetCriteria` 的**其余 36 格**："
                               + "`AbilityLogic.cs:16-17` · 消费点 `AbilityLogic__PlayAbility.c:262` → "
                               + "`AbilityLogic__GetTargets.c`）。`CardDto` 那两列会是 **null = 未知**"
                               + "（⛔ 不是「原版说全是默认、不过滤」）。⚠️ 本批只落数据：`CardDef` 的消费者**还没接**，"
                               + "接上之前这两列不改变任何行为。跑一下 gen_cards_engine.py 重生成（执行代理 `E9`）。");

            // 🆕 v26（2026-10-25 · 执行代理 `E2` · `A3-2`）：**ability 级 `targetCriteria`（36 格）**
            //   —— 原版 `CardAbility.targetCriteria`（`+0x30`，`CardAbility__CanTriggerAbility.c:131-135`
            //   的触发前闸）。缺列（v25 及更早）⇒ `CardDto.abilityLevelCriteria` 是 `null` = **未知**。
            //   ⛔ 旧表**不许**被读成「原版说这些触发条件全是默认值」—— 那是静默放行（同 v16/v25 口径）。
            if (dto.version < 26)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 26 —— 没有 `abilityLevelCriteria` 列"
                               + "（原版 `CardAbility.targetCriteria` 的**其余 36 格**："
                               + "`CardAbility__CanTriggerAbility.c:131-135`）。`CardDto` 那一列会是 **null = 未知**"
                               + "（⛔ 不是「原版说没有触发条件」）。消费端 `CardDef.AbilityLevelCriteriaForTrigger`"
                               + " 在未知时**出声并跳过**（不筛 = 改前行为）。跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v27（2026-10-25 · 执行代理 `E3` · `E2` 报告 §六）：**ability 级 `actingCardCriteria`
            //   （36 格）** —— 原版 `CardAbility.actingCardCriteria`（`+0x28`，
            //   `CardAbility__CanTriggerAbility.c:127-130` 的**第一道闸**）。缺列（v26 及更早）
            //   ⇒ `CardDto.abilityActingCriteria` 是 `null` = **未知**。
            //   ⛔ 旧表**不许**被读成「原版说这些条件全是默认值」—— 那是静默放行（同 v16/v25/v26 口径）。
            if (dto.version < 27)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 27 —— 没有 `abilityActingCriteria` 列"
                               + "（原版 `CardAbility.actingCardCriteria`（`+0x28`）的**其余 36 格**："
                               + "`CardAbility__CanTriggerAbility.c:127-130`）。`CardDto` 那一列会是 **null = 未知**"
                               + "（⛔ 不是「原版说没有这一格条件」）。消费端 `CardDef.ActingLevelCriteriaForTrigger`"
                               + " 在未知时**出声并跳过**（不筛 = 改前行为）。跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v28（2026-10-25 · 执行代理 `E4` · `E4-2`）：**`ActiveAbility.targetCriteria`（36 格）**
            //   —— 原版主动能力的目标闸（`RawCardScript__IsValidActiveAbilityTarget.c:103-107`）。
            //   缺列（v27 及更早）⇒ `CardDto.activeAbilityPickCriteria` 是 `null` = **未知**。
            //   ⛔ 旧表**不许**被读成「原版说没有这一格条件」—— 那是静默放行（同 v16/v25/v26/v27 口径）。
            if (dto.version < 28)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 28 —— 没有 `activeAbilityPickCriteria` 列"
                               + "（原版 `ActiveAbility.targetCriteria`（`+0x38`）的**其余 36 格**："
                               + "`RawCardScript__IsValidActiveAbilityTarget.c:103-107`）。"
                               + "`CardDto` 那一列会是 **null = 未知**"
                               + "（⛔ 不是「原版说没有这一格条件」）。消费端 `CardDef.ActiveAbilityPickCriteriaFor`"
                               + " 在未知时**出声并放行**（不筛 = 改前行为）。跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v29（2026-10-25 · 执行代理 `X6` · 口径乙）：**`abilityLevelCriteria` 每行的 `ca`**
            //   （该 ability 的 `chosenAbility` 家族）—— 23 张卡 `trigger 0` ability 定位不唯一时，
            //   消费端按家族把行认回来。
            //   缺列（v28 及更早）⇒ 每行的 `ca` 是 `null` = **未知** ⇒ 消费端**出声 + 放行**。
            //   ⛔ 旧表**不许**被读成「这张卡没有这一格条件」（那是另一种静默放行）。
            if (dto.version < 29)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 29 —— `abilityLevelCriteria` 各行"
                               + "**没有 `ca`（能力种类族名）** ⇒ 按家族认行的那道闸**判不了**。"
                               + "消费端 `CardDef.AbilityLevelCriteriaForFamily` / `RuleCore.AbilityFamilyTargetAllows`"
                               + " 会在未知时**出声 + 放行**（不筛 = 改前行为；⛔ 不是「原版说没有这一格条件」）。"
                               + "跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v31（2026-10-26 · 执行代理 `B8_AH` · 账 `战斗逻辑驱动.tsv` 的 `IsKeyword` 行）：
            //   **`keyword` 列**（原版 `RawCardScript.keyword`，`int @+0x98`）。
            //   缺列（v30 及更早）⇒ `CardDto.keyword` 的字段初值 `-1` = **未知**
            //   ⇒ `CardDef.IsKeyword` 返回 `null`（判不了），条件 275/276/277 的消费端
            //   **退回 `Subtype` 口径并出声**（`ConditionEval.KeywordOrSubtype`）。
            //   ⛔ 旧表**不许**被读成 `none(0)`（那会让 275/276/277 全池变假 = 静默筛空）。
            if (dto.version < 31)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 31 —— 没有 `keyword` 列"
                               + "（原版 `RawCardScript.keyword` `+0x98`，`RawCardScript__IsKeyword.c:5-7`）。"
                               + "`CardDto.keyword` 会是 **-1 = 未知**（⛔ 不是 `none(0)`）。"
                               + "消费端 `ConditionEval` 的条件 275/276/277 会**退回 `Subtype` 口径并出声**"
                               + "（旧行为 = 改前行为）。跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v33（2026-10-27 · `E7ENG` · 账 532 的 1a）：**`enchantTrait` 列**
            //   （原版 `CardEffect.trait` `+0x3C`；判据 → `CardDef.EnchantTrait` 的 doc）。
            //   缺列（v32 及更早）⇒ `CardDto.enchantTrait` 是 `null` = **未知**
            //   ⇒ `ActiveCardEffect.Trait` 也是 `null` ⇒ 摘附魔那一下**状态框不会被销毁**
            //   （原版 `RemoveDestroyableAnims` 靠它当键），而且 `Enchantments.Attach` 会**出声**。
            //   ⛔ 旧表**不许**被读成 `""`（= 「明确没有」，那会静默吞掉 `pray` 那 23 张）。
            if (dto.version < 33)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 33 —— 没有 `enchantTrait` 列"
                               + "（原版 `CardEffect.trait` `+0x3C`：附魔效果自带的关键词）。"
                               + "`CardDef.EnchantTrait` 会是 **null = 未知**（⛔ 不是 `\"\"` = 没有）"
                               + "⇒ 摘附魔时 `RemoveDestroyableAnims` 找不到件（状态框留着）。"
                               + "跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v34（2026-10-27 · `E7ENG` · 第三批）：**`cardType` 列**
            //   （原版 `RawCardScript.cardType` `+0x94`；消费点 = `CardDef.IsTrapCard` → `CardInstance.ArmedTrap`）。
            //   缺列（v33 及更早）⇒ `CardDto.cardType` 是 `-1` = **未知**（⛔ 不是 `Minion(0)`）
            //   ⇒ `CardDef.IsTrapCard` 回 `null`（**判不了**）⇒ `CardInstance.ArmedTrap = -1`。
            //   ⚠️ 今天全池本来就是 0 张陷阱（`spellType == 70` = 0）⇒ **无可观察差异**；
            //      这条守卫存在的意义是「将来数据出现时，旧表不会被静默读成『这些都不是陷阱』」。
            if (dto.version < 34)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 34 —— 没有 `cardType` 列"
                               + "（原版 `RawCardScript.cardType` `+0x94`；陷阱判定 `isTrapCard` = "
                               + "`cardType 20 (Tactic)` ∧ `spellType 70 (Trap)`）。"
                               + "`CardDef.CardType` 会是 **-1 = 未知**（⛔ 不是 `Minion(0)`）"
                               + "⇒ `IsTrapCard` 回 `null`（判不了）、`CardInstance.ArmedTrap = -1`。"
                               + "跑一下 gen_cards_engine.py 重生成。");

            // 🆕 v35（2026-10-27 · 执行代理 `A8` · `调度台日志.md` §12·94）：**`cardDescriptionTextReferenceId` 列**
            //   （原版 `RawCardScript.cardDescriptionTextReferenceId`；消费点 = `BattleDriver.OfficialDescKey`
            //   → `FaceTextFull` 那 4 处取键）。缺列（v34 及更早）⇒ `CardDto.cardDescriptionTextReferenceId`
            //   是 `null` = **判不了** ⇒ 取键退回 `Card_Description/` + 自己 id（= 改前行为：那 15 张印官方**旧**正文）。
            //   ⛔ 旧表**不许**被读成「原版说这 15 张都没有 ref」。
            if (dto.version < 35)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 35 —— 没有 `cardDescriptionTextReferenceId` 列"
                               + "（原版 `RawCardScript.cardDescriptionTextReferenceId`；消费点 = `BattleDriver.OfficialDescKey`）。"
                               + "`CardDef.CardDescriptionTextReferenceId` 会是 **null = 判不了** ⇒ 卡面正文取键退回"
                               + " `Card_Description/` + 自己 id（= 改前行为；那 15 张印官方**旧**正文）。"
                               + "跑一下 gen_cards_engine.py 重生成。");

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
                var def = new CardDef(
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
                    // 🆕 2026-10-25（完全复刻 · 执行代理 A · 收藏域 `C` 请求 ②）：资料片档位 ——
                    //   原版 `PlayerItem.expansionPack`。消费点 = `CardDef.ExpansionSearchName`
                    //   （收藏页「名字」搜索会匹配它的枚举成员名，判据 `CardNameFilter.cs:132-142`）。
                    //   ⚠️ 缺列/缺值一律按 `-1`（未知），⛔ 别当成 `Default(0)` —— 见 `CardDef.Expansion` 的 doc。
                    expansion: c.expansionPack,
                    // 🆕 2026-10-25（账 2b）：原版 `RawCardScript.cantBeRandomlyCreated`（0/1）。
                    //   消费点 = `EffectResolver.ChooseCardCandidates` 的「回落全卡池」那一段
                    //   （判据 `AbilityLogic.GetTargets` 的 `choiceResolution` 分支）。
                    //   ⚠️ 缺值按 `false`（= 可以被随机生成）—— 与原版 bool 的默认值同义。
                    cantBeRandomlyCreated: c.cantBeRandomlyCreated != 0,
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
                    // 🆕 2026-10-25（P3 · 账 3 条 C）：原版 `specialAttackScoringCriteria`（bool）——
                    //   原样透传（缺键 = 0 = 不带；bool 没有未知档）。消费点 = `SimpleAI.KillerOverride`
                    //   与 `SimpleAI.HasSpecialAttackScoring`。
                    specialAttackScoring: c.specialAttackScoring,
                    // 🔴🆕 2026-10-23（引擎线 `新账-95` #2）：原版 `targeted` 原样透传（`-1` = 未知）。
                    //   消费点 = `EffectText.AssignCardTargetFallback`（`cardtarget` 那一条 role 的闸）。
                    targeted: c.targeted,
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
                    // 🆕 v20（2026-10-24 · 引擎线 `A12`）：逐 `abilityLogic` 的 `furtherMultiplier` ——
                    //    与 `abilityTargets` **逐条同下标**（`CardDef` 构造函数把它们并进同一批 spec）。
                    //    ⚠️ 缺值口径 = 空表 + `AbilityMultipliersVersioned = false`（旧表整列没有
                    //    ⇒ 上面的 `< 20` 守卫已经出过声）；未知格 = `-1`（⛔ 不是 0 = 「不乘」）。
                    abilityMultipliers: MapAbilityMultipliers(c),
                    abilityMultipliersVersioned: dto.version >= 20,
                    // 🆕 v23（2026-10-24 · 引擎线 `G12`）：主动能力整条链（`activeAbilityLogic` 列）——
                    //    **每张卡都落**（没行的 995 张 `[]`），缺列 = 真值里没这张卡 = 未知。
                    //    ⚠️ 缺值口径 = `null`（⛔ 不是空表）+ `ActiveAbilityLogicVersioned = false`
                    //    （旧表整列没有 ⇒ 上面的 `< 23` 守卫已经出过声）。
                    //    ⚠️ 挂到 op 上不在这里：`CardDef` 收完 `TriggerOps` / `OathOps` 之后自己做
                    //    （`ApplyActiveAbilityLogic`，闸门与「为什么只挂 1↔1」写在那边）。
                    activeAbilityLogic: MapActiveAbilityLogic(c),
                    activeAbilityLogicVersioned: dto.version >= 23,
                    // 🆕 v25（2026-10-24 · 执行代理 `W2` · `A2` Q7 / `C5` Q4）：logic 级
                    //    `targetCriteria` 的其余 36 格（两条支）——
                    //    ⚠️ 缺值口径 = `null`（⛔ 不是空表）+ `…Versioned = false`
                    //    （旧表整列没有 ⇒ 上面的 `< 25` 守卫已经出过声）。
                    //    消费端 = `EffectText.AssignAbilityTargets` / `CardDef.ApplyActiveAbilityLogic`
                    //    → `EffectOp.TargetCriteriaSet` → `EffectResolver.ResolveTargets` 的筛选。
                    abilityTargetCriteria: MapTargetCriteriaRows(c, c.abilityTargetCriteria, active: false),
                    abilityTargetCriteriaVersioned: dto.version >= 25,
                    activeAbilityTargetCriteria: MapTargetCriteriaRows(c, c.activeAbilityTargets, active: true),
                    activeAbilityTargetCriteriaVersioned: dto.version >= 25,
                    // 🆕 v26（2026-10-25 · 执行代理 `E2` · `A3-2`）：**ability 级** `targetCriteria`
                    //    （原版 `CardAbility.targetCriteria` `+0x30`）的其余 36 格 —— **只落数据 + 消费端**：
                    //    `CardDef.AbilityLevelCriteriaForTrigger`（唯一入口）→ `ActionPump.EnqueueAbility`
                    //    的触发前闸（定位不唯一 ⇒ 出声并跳过）。
                    //    ⚠️ 缺值口径 = `null`（⛔ 不是空表）+ `…Versioned = false`
                    //    （旧表整列没有 ⇒ 上面的 `< 26` 守卫已经出过声）。
                    abilityLevelCriteria: MapAbilityLevelCriteria(c),
                    abilityLevelCriteriaVersioned: dto.version >= 26,
                    // 🆕 v29（`X6` · 口径乙）：`abilityLevelCriteria` 各行带 `ca`（能力种类族名）。
                    //   缺列（< 29）⇒ `CardDef.AbilityLevelCriteriaForFamily` 一律「判不了」⇒ 出声 + 放行。
                    abilityLevelCriteriaFamilyVersioned: dto.version >= 29,
                    // 🆕 v27（2026-10-25 · 执行代理 `E3` · `E2` 报告 §六）：**ability 级**
                    //    `actingCardCriteria`（原版 `CardAbility.actingCardCriteria` `+0x28`）的其余 36 格
                    //    —— **第一道闸**（排在 `targetCriteria` 那道之前，原版短路次序）。
                    //    消费端 = `CardDef.ActingLevelCriteriaForTrigger`（唯一入口）→
                    //    `ActionPump.EnqueueAbility`（Attack / Die 两族；拿不到 `actingCard` ⇒ 出声并跳过）。
                    //    ⚠️ 缺值口径 = `null`（⛔ 不是空表）+ `…Versioned = false`
                    //    （旧表整列没有 ⇒ 上面的 `< 27` 守卫已经出过声）。
                    abilityActingCriteria: MapAbilityActingCriteria(c),
                    abilityActingCriteriaVersioned: dto.version >= 27,
                    // 🆕 v28（2026-10-25 · 执行代理 `E4` · `E4-2`）：**`ActiveAbility.targetCriteria`**
                    //    （`+0x38`，**对象级**、没有 `lg`）的其余 36 格 —— 主动能力的**目标闸**。
                    //    消费端 = `CardDef.ActiveAbilityPickCriteriaFor(ab)`（唯一入口）→
                    //    `RuleCore.CanUseAbility`（高亮 / 点选 / 最后一刻复核都走它）。
                    //    ⚠️ 缺值口径 = `null`（⛔ 不是空表）+ `…Versioned = false`
                    //    （旧表整列没有 ⇒ 上面的 `< 28` 守卫已经出过声）。
                    activeAbilityPickCriteria: MapActiveAbilityPickCriteria(c),
                    activeAbilityPickCriteriaVersioned: dto.version >= 28,
                    // 🔴🆕 v18（2026-10-23 · 引擎线 `P1·①`）：逐条 ability 的 `priority`，**键 = 原版
                    //    `AbilityTrigger` 枚举名** —— 补 `triggerPriority`（键 = 引擎关键词）覆盖不到的
                    //    `WhenEvent` / 回合 / 打出族。四道闸（缺 `name` / 缺 `trigger` / 非法 `priority` /
                    //    同 key 重复）在 `MapAbilityTriggerPriority` 里**出声丢弃**，⛔ 不在这里另判一遍。
                    //    ⚠️ 缺值口径 = 空表 + `AbilityTriggerPrioritiesVersioned = false`（旧表整列没有
                    //    ⇒ 上面的 `< 18` 守卫已经出过声）；访问器一律回 **1 = Instant**（= 改前行为）。
                    abilityTriggerPriority: MapAbilityTriggerPriority(c),
                    abilityTriggerPrioritiesVersioned: dto.version >= 18,
                    // 🆕 v21（2026-10-24 · 引擎线 `A20` · 第三批 ①/②）：主动能力闸六列 ——
                    //    ⚠️ 缺值口径 = `0`（合法取值「假/无」）；**只有生成器写 `-1` 才是未知**。
                    //    旧表（< v21）整列没有 ⇒ 上面的 `< 21` 守卫已经出过声 ⇒ `CardDef` 统一按
                    //    **`-1`（未知）** 收（⛔ 不让「表旧」静默变成「全池都假」）。
                    hasActiveAbility: dto.version >= 21 ? c.hasActiveAbility : -1,
                    activeNeedsLoading: dto.version >= 21 ? c.activeNeedsLoading : -1,
                    alternativeNeedsLoading: dto.version >= 21 ? c.alternativeNeedsLoading : -1,
                    hasAlternativeAbility: dto.version >= 21 ? c.hasAlternativeAbility : -1,
                    quest: dto.version >= 21 ? c.quest : -1,
                    questTimes: dto.version >= 21 ? c.questTimes : -1,
                    normalAbilityBoardCriteria: dto.version >= 21 ? MapBoardGate(c) : null);

                // ================================================================================
                //  🆕 v31（2026-10-26 · 执行代理 `B8_AH`）：**两个「构造后挂载」**
                // ================================================================================
                //  🔴 **为什么不写成构造参数**：`CardDef` 的构造函数在 `CardDef.cs:839-1000`，
                //     它下面有 40 处账落点按**行号**引用 ⇒ 在那里加两行会让它们整体漂移
                //     （判据 → `资料/完全复刻/报告/B8_Q_trait口径与卡定义.md` §⑥ 的警告）。
                //     `AttachCardTraits` / `AttachKeywordId` 就是为此建在 `CardDef` **类尾**的。
                //
                //  ① `cardTraits` —— 原版 `RawCardScript.CardTraits`（`List<CardTraitData> @+0x230`）。
                //     本列 **v9 起就在**（`CardDto.cardTraits` 的 doc 明写「本列不喂引擎」）；
                //     现在起它是 trait 家族四个查询（`CountTraits` / `HasDefaultTrait` /
                //     `GetDefaultTraitValue` / `FindDefaultTrait`）的**唯一数据源**。
                //     `null`（旧表整列没有）⇒ `CardTraitsKnown == false` ⇒ 四个查询返回「判不了」+ `why`。
                //  ② `keyword` —— 原版 `RawCardScript.keyword`（`int @+0x98`，**v31 新列**）。
                //     `-1`（旧表 / 真值里没这张卡）⇒ `IsKeyword` 返回 `null`（判不了），
                //     条件 275/276/277 退回 `Subtype` 口径并出声（见 `ConditionEval.KeywordOrSubtype`）。
                //
                //  ⚠️ **`codex` / `strike` / `stomp` 三个词不进 `cardTraits`**（调度台 2026-10-26 裁定）：
                //     原版 `CardTraits(+0x230)` 只有 trait 家族那 5 个方法在扫，而这三个词
                //     **只出现在 `keywords` 里** ⇒ 按原版数据记，⛔ 别为了让 `Has()` 与
                //     `HasDefaultTrait()` 一致去补数据（判据 → `B8_Q` 报告 §⑤-1 + 调度台裁定）。
                def.AttachCardTraits(c.cardTraits);
                def.AttachKeywordId(c.keyword);
                // 🆕 v33（2026-10-27 · `E7ENG` · 账 532）：附魔效果自带的 trait —— 同样是**构造后挂载**
                //   （理由同上：`CardDef` 构造函数那 40 处账落点）。`null` = 旧表 / 没喂（未知）。
                def.AttachEnchantTrait(c.enchantTrait);
                // 🆕 v34（2026-10-27 · `E7ENG` · 第三批）：原版 `RawCardScript.cardType` —— 同样**构造后挂载**。
                //   消费者 = `CardDef.IsTrapCard` → `CardInstance.ArmedTrap`（陷阱那两格）。
                def.AttachCardType(c.cardType);
                // 🆕 v34+（2026-10-27 · 执行代理 `W46ALTD` · 账 `HUD:92`/`HUD:93`）：异画副本 id 列表
                //   —— 同样**构造后挂载**（理由同上：`CardDef` 构造函数那 40 处账落点）。
                //   🔴 `null`（整行没这个键 = 旧表）与 `[]`（明确没有异画）**必须分开传**，
                //   否则「缺列」会被静默读成「这张卡没有异画」→ 收紧口径会把全部异画判死。
                //   消费点 = `CardDef.HasAlternateArt` / `CardDatabase.ResolveDeckEntry`。
                def.AttachAlternateArts(c.alternateArts);
                // 🆕 2026-10-27（执行代理 `A4`）：选目标提示的文案引用 id（v35 列；`null` = 列缺席 ⇒ 判不了）。
                //   消费点 = `BattleDriver.ShowPlayTargetPrompt` → `Loc.TargetedKey(uniqueId, refId)`（原版 :2450 三元）。
                def.AttachTargetingTextReference(c.targetingTextReferenceId);
                // 🆕 2026-10-27（执行代理 `A8` · §12·94）：**卡面正文的文案引用 id**（v35 列；`null` = 列缺席 ⇒ 判不了）。
                //   消费点 = `BattleDriver.OfficialDescKey` → `FaceTextFull` 的 4 处取键（逐字照原版三元）。
                def.AttachCardDescriptionTextReference(c.cardDescriptionTextReferenceId);
                // 🆕 v35（待生成器落列；2026-10-27 · `B9_W65ABIL`）：`ActiveAbility` 对象自己的四个标量 ——
                //   同样**构造后挂载**（理由同上）。缺键 ⇒ DTO 初值 `-1` = 未知。今天零行为变化。
                //   语义 / 三跳表 / 45-56 张督军的实测口径 → `Core/CardDefActiveAbility.cs` 文件头。
                def.AttachActiveAbilityCost(c.activeAbilityManaCost, c.activeAbilityPsychicCost,
                                            c.activeAbilityPlasmaCost, c.activeAbilityTargeted);
                // 🆕 v36（2026-10-28 · 执行代理 `A1c`）：逐行 `damageCriteria`（`abilityDamage` 列）——
                //   同样**构造后挂载**。缺列（< 36）⇒ 传 `null` = **整列未知** ⇒ 消费端一律不逐目标掷
                //   （与加这一列之前**逐字相同**）。消费点 = `RuleCore.StampRollPerTarget`。
                def.AttachAbilityDamage(MapAbilityDamage(c)); def.AttachWhileInPlayAnim(dto.version >= 37 ? c.whileInPlayAnim : -1); def.AttachPermanentBuffs(dto.version >= 37 ? MapPermanentBuffs(c) : null); def.AttachAbilitySummon(MapAbilitySummon(c)); RecordAbilityBuffs(def, c); def.AttachAbilityMutations(MapAbilityMutations(c)); def.AttachTriggerAbilityIndices(MapTriggerAbilityIndices(c));   // 🆕 v37（`E6`）：后两格同样「构造后挂载」（理由同上）；`null` = **未知**（旧表 < 37），⛔ 不要 `?? new[0]`。⚠️ 三条挤一行是**故意的**：加行会把 `:1799` 等下游 A 行账落点整体推走。｜🆕 v40（`B23`）：`RecordAbilityBuffs` 写的是**旁表**（`_abilityBuffsById`，查询口 `CardDatabase.AbilityBuffsOf`）—— 因为 `RuleEngine/Core/**`（`CardDef`）不在该代理白名单；⛔ 别把数据改成 `def.Attach*`（会与旁表两处写同一条规则）。
                list.Add(def);
            }
            // 🆕 v23（2026-10-24 · 引擎线 `G12`）：`activeAbilityLogic` 的**挂载读数**（一行，⛔ 不逐卡刷屏）。
            //   为什么要有这一行：这一列只在「1 个对象 ↔ 1 行 ↔ 1 条 op」时能挂上（理由 →
            //   `CardDef.ApplyActiveAbilityLogic` 的 doc），其余档**必须看得见**——否则下一个人
            //   会以为「132 张全接上了」。逐卡原因在 `CardDef.ActiveAbilityMap`，这里是汇总。
            //   ⚠️ 旧表（< v23）整列没落 ⇒ 不出这一行（上面那条 `< 23` 守卫已经说清了）。
            if (dto.version >= 23)
            {
                int withRows = 0, logicRows = 0;
                // 🆕 v24（执行代理 `E1`）：map 取值多了 `gap:kind-*` 三档 ⇒ 改成**逐取值计数**，
                //   漏一档就等于「有一批卡悄悄没归因」（本轮之前的写法只认四档、其余一律 `其它`）。
                var buckets = new List<string>
                {
                    "attached", "gate-0", "gap:gate-unknown", "gap:groups-differ", "gap:two-keywords",
                    "gap:no-keyword", "gap:no-ops", "gap:shape", "gap:multi-object",
                    "gap:kind-ambiguous", "gap:kind-no-op", "gap:kind-unknown",
                };
                var counts = new int[buckets.Count];
                int unattributed = 0;
                foreach (var cd in list)
                {
                    if (cd == null || cd.ActiveAbilityLogic == null || cd.ActiveAbilityLogic.Count == 0) continue;
                    withRows++;
                    foreach (var r in cd.ActiveAbilityLogic) if (r != null) logicRows += r.LogicCount;
                    string m = cd.ActiveAbilityMap ?? "";
                    int at = buckets.IndexOf(m);
                    if (at >= 0) counts[at]++;
                    else if (m.Length > 0) unattributed++;      // 新取值没登记 ⇒ 看得见
                }
                if (withRows > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < buckets.Count; i++)
                        if (counts[i] > 0) sb.Append($" · `{buckets[i]}` {counts[i]} 张");
                    if (unattributed > 0) sb.Append($" · **未登记的结论 {unattributed} 张**");
                    Debug.Log($"[RuleEngine] `activeAbilityLogic`（卡表 v{dto.version}）：有行的 **{withRows} 张 / {logicRows} 行**；"
                            + "挂载结论 →" + sb
                            + "（闸门 / 每档含义 → `CardDef.ApplyActiveAbilityLogic` 与其 `ActiveAbilityMap` 的 doc）。"
                            + "⛔ 未挂 ≠ 原版没有目标，是**我们读不到**。");
                }
            }

            // 🔴 🆕 2026-10-15（接线批）：**护甲两套口径一旦分叉就出声** ——
            //   引擎读的那一份（`CardDef` 构造里定下来的 `_keywords[armour]`）由
            //   「v9 `armour` 列优先、`keywords` 旧口径兜底」合成，两条腿**在正常数据上必须一致**。
            //   实测（2026-10-15）全池 1127 张只有 **1 张**分叉：`DA66 Ravenwing Ballistus Dreadnought`
            //   （列写 0、`keywords` 写裸 `Armour` ⇒ 旧口径 1，而 PnP 卡图银盾印 1 ⇒ **列是错的那一侧**，
            //   已另立账）⇒ 这里出声是为了**下一张**再出现时不必靠人肉比对。
            //   ⚠️ `RuleEngineTest.TestCardDatabase` 里有一条断言盯着同一件事（这里出声、那里断红）。
            ReportArmourDisagreements(list);

            // 🆕 v25（2026-10-24 · 执行代理 `E9`）：`activeAbilityTargets` / `abilityTargetCriteria`
            //   的**解析读数 + 结构校验**（一次装载一行日志）。
            //   为什么必须出声：这两列本批**没有 `CardDef` 消费者** ⇒ 不打印就完全看不见
            //   「`JsonUtility` 有没有把子对象 `criteria` 绑上」（静默绑不上 = 两列白落）。
            //   ⛔ 口径：`MissingIndex`（-99）= JSON 里没有这个键 = **坏行**（出声）；
            //   `-1` = 生成器明确的「未知/不适用」，**不算坏**（`playerTurn` 在两列上恒 `-1`）。
            if (dto.version >= 25)
                ReportTargetCriteriaColumns(dto);

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

        /// <summary>🆕 v20（2026-10-24 · 引擎线 `A12`）：逐 `abilityLogic` 的 `furtherMultiplier` →
        /// 与 <see cref="MapAbilityTargets"/> **逐条同下标**的 `int[]` 列表（由 `CardDef` 构造函数
        /// 并进那批 `AbilityTargetSpec`）。返回 `null` 元素 = 这一条 ability 读不到（保持下标不错位）。
        /// <para>口径同 `MapAbilityTargets`：**只做形状校验，坏格落 `-1`**（`-1` = 未知；
        /// `0` 是合法取值 = 「原版明确不乘」）。</para></summary>
        static List<int[]> MapAbilityMultipliers(CardDto c)
        {
            var list = new List<int[]>();
            if (c == null || c.abilityMultipliers == null) return list;
            foreach (var am in c.abilityMultipliers)
            {
                if (am == null) { list.Add(null); continue; }   // 下标即语义 ⇒ 占位、不错位
                var fm = am.fm;
                if (fm == null) { list.Add(null); continue; }
                for (int i = 0; i < fm.Length; i++)
                    if (fm[i] < -1)
                    {
                        Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityMultipliers` 有一条 "
                                     + $"`furtherMultiplier = {fm[i]}` **非法**（只允许 ≥ 0 或 -1）—— 该格按 -1 处理。"
                                     + "重跑 工具/gen_cards_engine.py。");
                        fm[i] = -1;
                    }
                list.Add(fm);
            }
            return list;
        }

        /// <summary>🆕 v36（2026-10-28 · 执行代理 `A1c`）：`abilityDamage` 列 →
        /// <see cref="CardDef.AbilityDamageSpec"/>（**逐行 `damageCriteria`**）。
        /// <para>⚠️ 行级的坏值口径与 `abilityMultipliers` 同族：缺 `ab`/`lg`、四格非整数 ⇒ 出声并按 `-1`；
        /// **同一 `(ab,lg)` 撞键 ⇒ 出声丢弃后一条**（⛔ 谁赢都说不清）。</para></summary>
        static CardDef.AbilityDamageSpec[] MapAbilityDamage(CardDto c)
        {
            if (c == null || c.abilityDamage == null) return null;   // 缺列 = 未知（⛔ 不是「没有行」）
            var list = new List<CardDef.AbilityDamageSpec>(c.abilityDamage.Length);
            var seen = new HashSet<long>();
            foreach (var r in c.abilityDamage)
            {
                if (r == null) continue;
                if (r.ab < 0 || r.lg < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityDamage` 有一行缺 `ab`/`lg`"
                                 + $"（ab={r.ab}, lg={r.lg}）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                long k = ((long)r.ab << 32) ^ (uint)r.lg;
                if (!seen.Add(k))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityDamage` 有**重复的 "
                                 + $"(ab={r.ab}, lg={r.lg})** —— 后一条已丢弃（⛔ 撞键 = 谁赢都说不清）。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                list.Add(new CardDef.AbilityDamageSpec
                {
                    Ab = r.ab, Lg = r.lg, Min = r.min, Max = r.max, ValueOptions = r.opt, Type = r.type,
                });
            }
            return list.ToArray();
        }

        /// <summary>🆕 v23（2026-10-24 · 引擎线 `G12`）：`activeAbilityLogic` 列 →
        /// <see cref="CardDef.ActiveAbilityLogicSpec"/> 列表（**一个元素 = 一个能力对象**）。
        ///
        /// <para>🔴 **返回 `null` ≠ 返回空表**：
        /// · `c.activeAbilityLogic == null`（旧表没这一列 / 卡上没有这一格）⇒ **`null` = 未知**；
        /// · `[]`（JSON 里就是空数组）⇒ **空表 = 明确没有主动能力行**（现池 995 张）。
        /// 两者由 `CardDef.ActiveAbilityLogicVersioned` 再分一层，消费端**不许**把 `null` 当空表。</para>
        ///
        /// <para>**只做形状校验**（坏行出声丢弃 / 坏格出声落 `-1`），不做语义判断 ——
        /// 与 `MapAbilityMultipliers` / `MapUnitConditions` 同一分工：
        /// ① `ab` 不是 `-1` / `-2` ⇒ 出声 + 丢掉那一行（`ab` 是分组键，错一个就挂到别的对象上）；
        /// ② `gate` 不是 `1` / `0` / `-1` ⇒ 出声 + 丢掉那一行（三值之外没有别的语义）；
        /// ③ `ta` / `fm` 的格 `< -1` ⇒ 出声 + 该格落 `-1`（`0` 合法，⛔ 不猜）；
        /// ④ `ta` 与 `fm` **长度不一致** ⇒ 出声 + 两格都置 `null`（未知）—— 逐行对齐的前提没了，
        ///    留着会让「第 j 行」的定义自相矛盾。</para>
        /// </summary>
        static List<CardDef.ActiveAbilityLogicSpec> MapActiveAbilityLogic(CardDto c)
        {
            if (c == null || c.activeAbilityLogic == null) return null;   // 🔴 null = 这一列没落（未知）
            var list = new List<CardDef.ActiveAbilityLogicSpec>(c.activeAbilityLogic.Length);
            foreach (var r in c.activeAbilityLogic)
            {
                if (r == null) continue;                                   // 不是下标语义 ⇒ 不用占位
                if (r.ab != CardDef.ActiveAbilityObject && r.ab != CardDef.AlternativeAbilityObject)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic` 有一条 "
                                 + $"`ab = {r.ab}` **非法**（只允许 {CardDef.ActiveAbilityObject}"
                                 + $" / {CardDef.AlternativeAbilityObject}）—— 那一行**已丢弃**。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.gate != 1 && r.gate != 0 && r.gate != CardDef.GateUnknown)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                 + $"`gate = {r.gate}` **非法**（只允许 1 / 0 / -1）—— 那一行**已丢弃**。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                var ta = r.ta;
                var fm = r.fm;
                if (ta != null)
                    for (int i = 0; i < ta.Length; i++)
                        if (ta[i] < -1)
                        {
                            Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                         + $"第 {i} 行 `targetsAffected = {ta[i]}` **非法**（只允许 ≥ 0 或 -1）"
                                         + "—— 该格按 -1 处理。重跑 工具/gen_cards_engine.py。");
                            ta[i] = -1;
                        }
                if (fm != null)
                    for (int i = 0; i < fm.Length; i++)
                        if (fm[i] < -1)
                        {
                            Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                         + $"第 {i} 行 `furtherMultiplier = {fm[i]}` **非法**（只允许 ≥ 0 或 -1）"
                                         + "—— 该格按 -1 处理。重跑 工具/gen_cards_engine.py。");
                            fm[i] = -1;
                        }
                if (ta != null && fm != null && ta.Length != fm.Length)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                 + $"`ta` 长度 {ta.Length} 与 `fm` 长度 {fm.Length} 不一致 —— "
                                 + "**两格都按未知(null)处理**（逐行对齐的前提没了，⛔ 不压缩、不猜）。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    ta = null; fm = null;
                }
                // ---- 🆕 v24（执行代理 `E1`）：`ca` / `pa` / `dmg` 三格 —— 与 `ta` **同长度**才可用 ----
                // 口径同 ③：`0` 是合法值（`ca=0 drawCard` / `pa=0` 牌单位 / `dmg` 四格全 0），
                // 只有 `< -1` 才是坏值。缺列（v23 旧表）⇒ `null` = 未知。
                var ca = r.ca;
                var pa = r.pa;
                var dmgDto = r.dmg;
                if (ca != null)
                    for (int i = 0; i < ca.Length; i++)
                        if (ca[i] < -1)
                        {
                            Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                         + $"第 {i} 行 `chosenAbility = {ca[i]}` **非法**（只允许 ≥ 0 或 -1）"
                                         + "—— 该格按 -1 处理。重跑 工具/gen_cards_engine.py。");
                            ca[i] = -1;
                        }
                if (pa != null)
                    for (int i = 0; i < pa.Length; i++)
                        if (pa[i] < -1)
                        {
                            Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                         + $"第 {i} 行 `playersAffected = {pa[i]}` **非法**（只允许 ≥ 0 或 -1）"
                                         + "—— 该格按 -1 处理。重跑 工具/gen_cards_engine.py。");
                            pa[i] = -1;
                        }
                int[] taLen = ta ?? fm;
                int rows = taLen != null ? taLen.Length : -1;
                if (ca != null && rows >= 0 && ca.Length != rows)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                 + $"`ca` 长度 {ca.Length} 与逐行长度 {rows} 不一致 —— `ca` 按未知(null)处理。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    ca = null;
                }
                if (pa != null && rows >= 0 && pa.Length != rows)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                 + $"`pa` 长度 {pa.Length} 与逐行长度 {rows} 不一致 —— `pa` 按未知(null)处理。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    pa = null;
                }
                CardDef.ActiveAbilityDamageSpec[] dmg = null;
                if (dmgDto != null && rows >= 0 && dmgDto.Length != rows)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `activeAbilityLogic`（ab={r.ab}）"
                                 + $"`dmg` 长度 {dmgDto.Length} 与逐行长度 {rows} 不一致 —— `dmg` 按未知(null)处理。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    dmgDto = null;
                }
                if (dmgDto != null)
                {
                    dmg = new CardDef.ActiveAbilityDamageSpec[dmgDto.Length];
                    for (int i = 0; i < dmgDto.Length; i++)
                    {
                        var d = dmgDto[i];
                        if (d == null) continue;                    // 该行未知 ⇒ 留 null
                        dmg[i] = new CardDef.ActiveAbilityDamageSpec
                        {
                            Min = d.min == UnitConditionDto.MissingIndex ? -1 : d.min,
                            Max = d.max == UnitConditionDto.MissingIndex ? -1 : d.max,
                            ValueOptions = d.opt == UnitConditionDto.MissingIndex ? -1 : d.opt,
                            Type = d.type == UnitConditionDto.MissingIndex ? -1 : d.type,
                        };
                    }
                }
                list.Add(new CardDef.ActiveAbilityLogicSpec
                {
                    Ability = r.ab,
                    Gate = r.gate,
                    Trigger = r.trigger,
                    NeedsLoading = r.needsLoading,
                    TargetsAffected = ta,
                    FurtherMultiplier = fm,
                    ChosenAbility = ca,
                    PlayersAffected = pa,
                    Damage = dmg,
                });
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
            return MapCountSetDto(c, uc.countSet, "`unitConditions` 里有一条 `count` 行");
        }

        /// <summary>🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`unitConditions` 里**某一 role 的整条 criteria**
        /// → <see cref="CardDef.CountSetSpec"/>（`mult` / `multi` / `avail` / `choice` 四格共用）。
        ///
        /// <para>· 缺（`d == null`）⇒ 返回 `null` = **没落/未知**（旧表 &lt; v22 / 这一格不是那个 role）——
        /// ⛔ **不出声、不报错**：那是合法缺省（池内只有 10 行带这几格）。</para>
        /// <para>· 错位（别的 role 带着这一格）⇒ **出声并忽略**（⛔ 不静默挂到别的 role 上；
        /// 与 `countSet` 只属于 `count` 行同一条口径）。</para>
        /// <para>· `noPlayerTurn: true` —— 这四格都是纯 `TargetCriteria`，**原版没有 `playerTurn`**。</para>
        /// </summary>
        static CardDef.CountSetSpec RoleSet(CardDto c, UnitConditionDto uc, string role,
                                            CountSetDto d, string field)
        {
            if (d == null) return null;
            if (uc.role != role)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 里 "
                             + $"`role = {uc.role}` 的行**带着 `{field}`** —— 那一格只属于 `role = {role}` 的行，"
                             + "已**忽略**该字段（行本身照收）。重跑工具/gen_cards_engine.py。");
                return null;
            }
            return MapCountSetDto(c, d, $"`unitConditions` 的 `{role}` 行（`{field}`）",
                                  noPlayerTurn: true);
        }

        /// <summary>🆕 v21（2026-10-24 · 引擎线 `A20`）：`CountSetDto → CardDef.CountSetSpec` 的**唯一一份**实现
        /// —— `unitConditions[].countSet`（v16 起）与 `normalAbilityBoardCriteria.countSet`（v21 起）
        /// **逐字同形**（原版都是 `TargetCriteria` ＋ `BoardCriteria.playerTurn`）⇒ ⛔ 别各写一份。
        /// <paramref name="where"/> = 报错文案里的位置（「`unitConditions` 的 count 行」/「`normalAbilityBoardCriteria`」）。</summary>
        static CardDef.CountSetSpec MapCountSetDto(CardDto c, CountSetDto d, string where,
                                                   bool noPlayerTurn = false)
        {
            if (d == null)
            {
                Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 {where}"
                             + " **没有 `countSet`（被数集合）** —— 该行的 `CountSet` 落 **null = 未知**"
                             + "（⛔ 不是「默认集合」：`TargetsAffected=any` 是一个具体选择）。"
                             + "重跑工具/gen_cards_engine.py。");
                return null;
            }
            var s = new CardDef.CountSetSpec();

            // ③ `playerTurn` = `PlayersAffected` 三档（0 Both / 10 Self / 20 Enemy）。
            // 🔴 2026-10-24（`新账-95` #2）：`noPlayerTurn` = 这一格**结构上就没有 `playerTurn`**
            //    （`mult` / `multi` / `avail` / `choice` 四格都是纯 `TargetCriteria`）——
            //    生成器落 `-1`，那是「不适用」**不是坏值** ⇒ 出声的条件反过来：
            //    这四格**带着** 0/10/20 才是坏数据（⛔ 不静默当闸用）。
            if (noPlayerTurn)
            {
                if (d.playerTurn != UnitConditionDto.MissingIndex
                    && d.playerTurn != CardDef.ConditionUnknown)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 {where}"
                                 + $" 带着 `playerTurn = {d.playerTurn}` —— 这一格是纯 `TargetCriteria`，"
                                 + "**原版没有 `playerTurn`** ⇒ 已忽略该格（⛔ 不拿它当闸）。"
                                 + "重跑工具/gen_cards_engine.py。");
                }
            }
            else if (d.playerTurn == UnitConditionDto.MissingIndex)
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

        /// <summary>🆕 v21（2026-10-24 · 引擎线 `A20`）：`normalAbilityBoardCriteria` →
        /// <see cref="CardDef.BoardGateSpec"/>（原版 `BoardCriteria @+0x2B0`）。
        /// <para>🔴 **`null` 返回 = 原版那个恒真的默认 criteria**（缺席这一列），
        /// ⛔ 不是「判不了」——`CardDef.UsesAlternativeAbility` 会把它读成「条件满足」。</para>
        /// <para>只做**形状校验**（缺键 / 非整数 ⇒ 出声 + `-1` = 未知），⛔ 不做语义解释。</para></summary>
        static CardDef.BoardGateSpec MapBoardGate(CardDto c)
        {
            var d = c.normalAbilityBoardCriteria;
            if (d == null) return null;      // 缺席 = `targetsAffected=any` 的恒真默认值（见 DTO 的 doc）
            var s = new CardDef.BoardGateSpec();
            s.CountOption = CountSetInt(c, "normalAbilityBoardCriteria.countOptions", d.countOptions);
            s.StatOption = CountSetInt(c, "normalAbilityBoardCriteria.triggerIf", d.triggerIf);
            s.ReferenceValue = CountSetInt(c, "normalAbilityBoardCriteria.referenceValue", d.referenceValue);
            s.CountSet = MapCountSetDto(c, d.countSet, "`normalAbilityBoardCriteria`");
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
        ///   ③ `cond` **缺键**（`-1`）/ 不在 `UnitConditions` 枚举里 ⇒ 丢弃。
        ///      `0`（`none`）**只对 `mult` / `multi` / `avail` / `choice` / `count` 五格放行**
        ///      （判据 → `IsUnconditionalCriteriaRole`）；⚠️ 2026-10-26 批次 5 `G3` 新增 `count`：
        ///      那一行回答的是第三道闸（`CardAbility.boardCriteria +0x38`）的「数几个/跟谁比」，
        ///      `cond` 只是它的一个字段。其余 role 落成 0 **仍是坏数据**（⛔ 不静默当「无条件」）。
        ///   ④ 同 `(ab, lg, role)` 重复 ⇒ **丢弃后出现的那条**（先到的保留）＋出声。
        ///      生成器每一格只落一行（同一格天然唯一）；这里是**第二道闸**，且**必须**丢：
        ///      `CardDef` 的索引是「后写覆盖」⇒ 不丢就是静默取最后一条。
        /// ⚠️ 位置语义（十个 role 分别是什么、`count` 的三个参数）**只在 `CardDef` 那一段注释里有一份**，
        /// ⛔ 别在这里抄第二份。
        /// </summary>
        /// <summary>🆕 **2026-10-24（执行代理 `G6` · `C6` §1·3 A）**：这四个 role 的行是「**整条 criteria**」
        /// （`multiplierSet` / `multiSet` / `availSet` / `choiceSet`），`cond = 0`（`UnitConditions.none`）
        /// 是**合法档**（= 这条 criteria 不附加条件），⛔ 不是坏数据。
        /// <para>判据：原版里这四个位置都是**一个完整 `TargetCriteria`**，`conditionsFilter` 只是其中一个字段
        /// —— 它等于 0 时，这条 criteria 仍在回答「**数谁 / 候选池是谁**」（`AM18` 的
        /// `multiCountCriteria` 就是 `ta 10 + keywordFilter 10(Vehicle) + cond 0`）。</para>
        /// <para>🔴 **2026-10-26（批次 5 `G3`）加第五个 `count`**：判据 = `BoardAnalysis__MeetsCriteria.c:150-152`
        /// （`ta == 0` ⇒ 整条只剩 `playerTurn`）；生成器同口径 → `count_row_carries_board_info`。其余 role 仍不放行。</para>
        /// </summary>
        static bool IsUnconditionalCriteriaRole(string role)
        {
            return role == "mult" || role == "multi" || role == "avail" || role == "choice" || role == "count";
        }

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
                if (uc.cond == CardDef.ConditionNone && !IsUnconditionalCriteriaRole(uc.role))
                {
                    // 🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 A）**：这句原来对所有 role 一律丢弃。
                    //   现在 `mult` / `multi` / `avail` / `choice` 四格**允许** `cond = 0`（判据 →
                    //   `IsUnconditionalCriteriaRole`）—— 那四格是「**整条 criteria**」行，
                    //   `conditionsFilter` 只是其中一个字段。其余 role（`target` / `acting` / `count` /
                    //   卡级计分那几族）**照旧丢弃**：它们的「缺行 = 没有条件」已被消费端依赖。
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `unitConditions` 有一条 "
                                 + $"`{uc.role}` 的 `cond = 0`（`UnitConditions.none`）—— 已丢弃该条"
                                 + "（除 `mult`/`multi`/`avail`/`choice` 四格外，本列按约定**只落非 none 的行**；"
                                 + "落 0 的行是坏数据，⛔ 不静默当成「无条件」）。重跑工具/gen_cards_engine.py。");
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
                var spec = new CardDef.ConditionSpec
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
                    // 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：四格「整条 criteria」——
                    //   各归各的 role（错位在 `RoleSet` 里出声丢弃），缺 = `null`（**未知**）。
                    MultiplierSet = RoleSet(c, uc, "mult", uc.multiplierSet, "multiplierSet"),
                    MultiSet = RoleSet(c, uc, "multi", uc.multiSet, "multiSet"),
                    AvailSet = RoleSet(c, uc, "avail", uc.availSet, "availSet"),
                    ChoiceSet = RoleSet(c, uc, "choice", uc.choiceSet, "choiceSet"),
                };
                list.Add(spec);

                // 🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 D）：这条告警原来写着「本版**没有实现**
                //    ability 级重复 ⇒ 按 1 遍处理」—— **两半都不成立了**：
                //    ① 数据侧：`gen_cards_engine.py` 改前对 `conditionsFilter == 0` 的 criteria
                //       **整行丢弃** ⇒ 池内 `multi` 行只落了 `UM77` 一条（`ta = any(0)`）⇒ 这条告警
                //       **永远响不了**（护栏在、喂不到数据）。`G6` 的 A 笔已让 `ta != 0` 的 8 条
                //       `multi` 行落下来（`AM18` / `ASH52` / `DA57` / `GOF46` / `GSC56` / `SOR45` /
                //       `SW51` / `SW58`）。
                //    ② 引擎侧：`G6` 的 C 笔已在触发层实现「整条 ability 触发 N 遍」
                //       （`EffectResolver.TryStructuredMultiTrigger` + `TryComputeRepeats`），
                //       判据 = `RawCardScript__TriggerAbilities.c:230-250`。
                //    ⇒ 这里**不再是缺口**，改成一条**数据形状校验**：`multi` 行的 `multiSet` 必须落全
                //    （旧表 / 生成器漏落 ⇒ 消费端只能退回正文口径，那才是真的静默）。
                if (spec.Role == "multi" && spec.MultiSet == null)
                {
                    Debug.LogWarning(
                        $"[RuleEngine] 卡「{c.name}」（{c.id}）有一条 `multi` 行**没有 `multiSet`**"
                      + "（旧卡表 / 生成器没落整条 criteria）—— 触发层只能退回**正文口径**"
                      + "（`for each …`），⛔ 不是「原版就这样」。重跑工具/gen_cards_engine.py。");
                }
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

        /// <summary>🆕 v25（2026-10-24 · 执行代理 `W2` · `A2` Q7 / `C5` Q4）：
        /// `activeAbilityTargets` / `abilityTargetCriteria` → <see cref="CardDef.TargetCriteriaRowSpec"/>。
        ///
        /// <para>🔴 **`null` 返回 = 这一列没落（旧表 &lt; v25）**，⛔ 不是空表：
        /// 空表 = 「原版明确没有这一行」。</para>
        /// <para>**行级四道闸**（缺 `ab` / `ab` 不属这一列 / `lg &lt; 0` / `criteria` 为 null）
        /// ⇒ **出声并丢弃那一行**（其余行照收）—— ⛔ 不静默落成 `ab=0, lg=0`
        /// （那会把这一格挂到**第 0 条 ability / 第 0 条 logic** 上，正是最坏的一种静默错）。
        /// 36 格本体走 <see cref="MapCountSetDto"/>（**同一份字段表**，⛔ 别另抄）。
        /// ⚠️ `playerTurn` 在这两列上恒 `-1`（纯 `TargetCriteria`，结构上没有那一格）⇒ `noPlayerTurn: true`。
        /// ⚠️ 错误文案里的「`count` 行」是 `MapCountSetDto` 的既有提法（它服务三处）—— 读数不变。</para>
        /// </summary>
        static List<CardDef.TargetCriteriaRowSpec> MapTargetCriteriaRows(
            CardDto c, TargetCriteriaRowDto[] src, bool active)
        {
            if (src == null) return null;                    // 列没落（旧表）⇒ 未知
            var list = new List<CardDef.TargetCriteriaRowSpec>(src.Length);
            string col = active ? "activeAbilityTargets" : "abilityTargetCriteria";
            foreach (var r in src)
            {
                if (r == null) continue;
                if (r.ab == UnitConditionDto.MissingIndex)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行**缺 `ab` 键**"
                                 + " —— 那一行**已丢弃**（⛔ 缺键落 0 = 挂到第 0 条 ability 上）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (active)
                {
                    if (r.ab != CardDef.ActiveAbilityObject && r.ab != CardDef.AlternativeAbilityObject)
                    {
                        Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 `ab = {r.ab}`"
                                     + $" **非法**（只允许 {CardDef.ActiveAbilityObject} active / "
                                     + $"{CardDef.AlternativeAbilityObject} alternative）—— 那一行**已丢弃**。"
                                     + "重跑工具/gen_cards_engine.py。");
                        continue;
                    }
                }
                else if (r.ab < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 `ab = {r.ab}`"
                                 + " **非法**（这一列只收 `cardAbilities` 下标 `>= 0`；active/alternative "
                                 + "在另一列）—— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.lg < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) 的 `lg = {r.lg}` **非法**（这一列只收 logic 级 `>= 0`；"
                                 + "ability 级 criteria 在 `unitConditions` 里以 `lg = -1` 落）"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.criteria == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}, lg={r.lg}) **没有 `criteria`** —— 那一行**已丢弃**。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                var cs = MapCountSetDto(c, r.criteria, $"`{col}` 行 (ab={r.ab}, lg={r.lg})",
                                        noPlayerTurn: true);
                if (cs == null) continue;                    // 里面已经出过声
                list.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = r.ab,
                    Trigger = r.trigger,
                    Logic = r.lg,
                    Criteria = cs,
                });
            }
            return list;
        }

        /// <summary>🆕 v26（2026-10-25 · 执行代理 `E2` · `A3-2`）：`abilityLevelCriteria` →
        /// <see cref="CardDef.TargetCriteriaRowSpec"/>（**ability 级那一档**）。
        ///
        /// <para>🔴 **`null` 返回 = 这一列没落（旧表 &lt; v26）**，⛔ 不是空表：
        /// 空表 = 「原版明确没有这一行的触发条件」。</para>
        /// <para>**行级四道闸**（缺 `ab` / `ab &lt; 0` / `lg != -1` / `criteria` 为 null）
        /// ⇒ **出声并丢弃那一行**（其余行照收）——理由同 <see cref="MapTargetCriteriaRows"/>：
        /// 缺键落 0 会把这一格挂到**第 0 条 ability** 上，是最坏的一种静默错。
        /// 36 格本体走 <see cref="MapCountSetDto"/>（**同一份字段表**，⛔ 别另抄）。
        /// ⚠️ `playerTurn` 在这一列上恒 `-1`（纯 `TargetCriteria`，结构上没有那一格）⇒ `noPlayerTurn: true`。</para>
        /// </summary>
        static List<CardDef.TargetCriteriaRowSpec> MapAbilityLevelCriteria(CardDto c)
        {
            var src = c?.abilityLevelCriteria;
            if (src == null) return null;                    // 列没落（旧表）⇒ 未知
            const string col = "abilityLevelCriteria";
            var list = new List<CardDef.TargetCriteriaRowSpec>(src.Length);
            foreach (var r in src)
            {
                if (r == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行是 **null**"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab == UnitConditionDto.MissingIndex)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行**缺 `ab` 键**"
                                 + " —— 那一行**已丢弃**（⛔ 缺键落 0 = 挂到第 0 条 ability 上）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 `ab = {r.ab}`"
                                 + " **非法**（这一列只收 `cardAbilities` 下标 `>= 0`）"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.lg != -1)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) 的 `lg = {r.lg}` **非法**（这一列只收 ability 级 = `lg = -1`；"
                                 + "logic 级在 `abilityTargetCriteria` 那一列）"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.criteria == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) **没有 `criteria`** —— 那一行**已丢弃**。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                var cs = MapCountSetDto(c, r.criteria, $"`{col}` 行 (ab={r.ab}, lg=-1)",
                                        noPlayerTurn: true);
                if (cs == null) continue;                    // 里面已经出过声
                // 🆕 v29（`X6` · 口径乙）：`ca` 的**缺键**要出声、但**不能丢行**
                //   （丢行 = 这一格回到「没有条件」= 静默放行 + 少一条闸；留着并标 `""` 才是
                //    「判不了 ⇒ 出声 + 放行」，两者方向相同但后者**留得住痕迹**）。
                //   `""` 是**合法值**（家族判不了），所以缺键哨兵只能用 `null`（DTO 字段初值）。
                string fam = r.ca;
                if (fam == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) **缺 `ca` 键** —— 按「家族判不了」处置（空串）："
                                 + "消费端会**出声 + 放行**，⛔ 不会静默当成「没有条件」。"
                                 + "重跑 工具/gen_cards_engine.py（卡表 v29 起每行都带 `ca`）。");
                    fam = "";
                }
                list.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = r.ab,
                    Trigger = r.trigger,
                    Logic = -1,
                    Family = fam,
                    Criteria = cs,
                });
            }
            return list;
        }

        /// <summary>🆕 v27（2026-10-25 · 执行代理 `E3` · `E2` 报告 §六）：`abilityActingCriteria` →
        /// <see cref="CardDef.TargetCriteriaRowSpec"/>（**ability 级 · 第一道闸那一档**）。
        ///
        /// <para>🔴 **`null` 返回 = 这一列没落（旧表 &lt; v27）**，⛔ 不是空表：
        /// 空表 = 「原版明确没有这一行的条件」。</para>
        /// <para>**行级四道闸**（缺 `ab` / `ab &lt; 0` / `lg != -1` / `criteria` 为 null）
        /// ⇒ **出声并丢弃那一行**（其余行照收）——理由同 <see cref="MapAbilityLevelCriteria"/>：
        /// 缺键落 0 会把这一格挂到**第 0 条 ability** 上，是最坏的一种静默错。
        /// 36 格本体走 <see cref="MapCountSetDto"/>（**同一份字段表**，⛔ 别另抄）。
        /// ⚠️ `playerTurn` 在这一列上恒 `-1`（纯 `TargetCriteria`，结构上没有那一格）⇒ `noPlayerTurn: true`。</para>
        /// </summary>
        static List<CardDef.TargetCriteriaRowSpec> MapAbilityActingCriteria(CardDto c)
        {
            var src = c?.abilityActingCriteria;
            if (src == null) return null;                    // 列没落（旧表）⇒ 未知
            const string col = "abilityActingCriteria";
            var list = new List<CardDef.TargetCriteriaRowSpec>(src.Length);
            foreach (var r in src)
            {
                if (r == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行是 **null**"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab == UnitConditionDto.MissingIndex)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行**缺 `ab` 键**"
                                 + " —— 那一行**已丢弃**（⛔ 缺键落 0 = 挂到第 0 条 ability 上）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 `ab = {r.ab}`"
                                 + " **非法**（这一列只收 `cardAbilities` 下标 `>= 0`）"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.lg != -1)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) 的 `lg = {r.lg}` **非法**（这一列只收 ability 级 = `lg = -1`；"
                                 + "logic 级在 `abilityTargetCriteria` 那一列）"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.criteria == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) **没有 `criteria`** —— 那一行**已丢弃**。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                var cs = MapCountSetDto(c, r.criteria, $"`{col}` 行 (ab={r.ab}, lg=-1)",
                                        noPlayerTurn: true);
                if (cs == null) continue;                    // 里面已经出过声
                list.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = r.ab,
                    Trigger = r.trigger,
                    Logic = -1,
                    Criteria = cs,
                });
            }
            return list;
        }

        /// <summary>🆕 v28（2026-10-25 · 执行代理 `E4` · `E4-2`）：`activeAbilityPickCriteria` →
        /// <see cref="CardDef.TargetCriteriaRowSpec"/>（**`ActiveAbility` 对象级 · 主动能力目标闸**）。
        ///
        /// <para>🔴 **`null` 返回 = 这一列没落（旧表 &lt; v28）**，⛔ 不是空表：
        /// 空表 = 「原版明确没有这一格条件」。</para>
        /// <para>**行级三道闸**（`ab` 缺键 / `ab` 不在 `{-1,-2}` / `criteria` 为 null）
        /// ⇒ **出声并丢弃那一行**（其余行照收）。⚠️ **不校验 `lg`**：这一列**结构上没有**那一格
        /// （缺键 ⇒ DTO 初值 `-1`，见 <see cref="TargetCriteriaRowDto"/>）—— 校验它会把每一行都丢掉。
        /// 36 格本体走 <see cref="MapCountSetDto"/>（**同一份字段表**，⛔ 别另抄）；
        /// `playerTurn` 恒 `-1`（结构上没有那一格）⇒ `noPlayerTurn: true`。</para>
        /// </summary>
        static List<CardDef.TargetCriteriaRowSpec> MapActiveAbilityPickCriteria(CardDto c)
        {
            var src = c?.activeAbilityPickCriteria;
            if (src == null) return null;                    // 列没落（旧表）⇒ 未知
            const string col = "activeAbilityPickCriteria";
            var list = new List<CardDef.TargetCriteriaRowSpec>(src.Length);
            foreach (var r in src)
            {
                if (r == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行是 **null**"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab == UnitConditionDto.MissingIndex)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行**缺 `ab` 键**"
                                 + " —— 那一行**已丢弃**（⛔ 缺键落 0 = 挂到别的对象上）。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab != CardDef.ActiveAbilityObject && r.ab != CardDef.AlternativeAbilityObject)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 `ab = {r.ab}`"
                                 + " **非法**（这一列只收 `-1` active / `-2` alternative）"
                                 + " —— 那一行**已丢弃**。重跑工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.criteria == null)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `{col}` 有一行 "
                                 + $"(ab={r.ab}) **没有 `criteria`** —— 那一行**已丢弃**。"
                                 + "重跑工具/gen_cards_engine.py。");
                    continue;
                }
                var cs = MapCountSetDto(c, r.criteria, $"`{col}` 行 (ab={r.ab})", noPlayerTurn: true);
                if (cs == null) continue;                    // 里面已经出过声
                list.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = r.ab,
                    Trigger = r.trigger,
                    Logic = -1,
                    Criteria = cs,
                });
            }
            return list;
        }

        /// <summary>🆕 v25（2026-10-24 · 执行代理 `E9`）：两列 `targetCriteria` 的解析读数
        /// （一次装载打**一行**日志 · ⛔ 不逐卡刷屏）。
        ///
        /// <para>校验的是**结构**、不是语义（语义属于 `CardDef` 那一批）：
        /// · `ab`：`activeAbilityTargets` = `-1`/`-2`；`abilityTargetCriteria` = `>= 0`；
        /// · `lg`：两列都 `>= 0`；
        /// · `criteria`：非 null，且 `targetsAffected` / `playerTurn` **不是 `MissingIndex`（-99）**
        ///   —— 子对象只要有一个字段绑上，就说明 `JsonUtility` 把整块绑上了
        ///   （`playerTurn` 在两列上恒 `-1`：那一格结构上不适用，⛔ 不是坏值）。</para>
        /// </summary>
        static void ReportTargetCriteriaColumns(FileDto dto)
        {
            int aatCards = 0, aatRows = 0, atcCards = 0, atcRows = 0;
            int alcCards = 0, alcRows = 0;   // 🆕 v26：ability 级那一列（`CardAbility.targetCriteria`）
            // 🆕 v29（`X6` · 口径乙）：`abilityLevelCriteria` 每行 `ca`（能力种类族名）的三档读数。
            int alcCaKnown = 0, alcCaUnknown = 0, alcCaMissing = 0;
            int aacCards = 0, aacRows = 0;   // 🆕 v27：第一道闸那一列（`CardAbility.actingCardCriteria`）
            int aapCards = 0, aapRows = 0;   // 🆕 v28：主动能力目标闸（`ActiveAbility.targetCriteria`）
            int badAb = 0, badLg = 0, nullCriteria = 0, unbound = 0;
            string firstBad = null;
            foreach (var c in dto.cards)
            {
                if (c == null) continue;
                if (c.activeAbilityTargets != null)
                {
                    if (c.activeAbilityTargets.Length > 0) aatCards++;
                    foreach (var r in c.activeAbilityTargets)
                    {
                        if (r == null) { badAb++; continue; }
                        aatRows++;
                        if (r.ab != -1 && r.ab != -2) { badAb++; if (firstBad == null) firstBad = $"{c.id} ab={r.ab}"; }
                        if (r.lg < 0) { badLg++; if (firstBad == null) firstBad = $"{c.id} lg={r.lg}"; }
                        if (r.criteria == null) { nullCriteria++; if (firstBad == null) firstBad = $"{c.id} criteria=null"; }
                        else if (r.criteria.targetsAffected == UnitConditionDto.MissingIndex
                              || r.criteria.playerTurn == UnitConditionDto.MissingIndex)
                        { unbound++; if (firstBad == null) firstBad = $"{c.id} criteria 未绑定"; }
                    }
                }
                if (c.abilityTargetCriteria != null)
                {
                    if (c.abilityTargetCriteria.Length > 0) atcCards++;
                    foreach (var r in c.abilityTargetCriteria)
                    {
                        if (r == null) { badAb++; continue; }
                        atcRows++;
                        if (r.ab < 0) { badAb++; if (firstBad == null) firstBad = $"{c.id} ab={r.ab}"; }
                        if (r.lg < 0) { badLg++; if (firstBad == null) firstBad = $"{c.id} lg={r.lg}"; }
                        if (r.criteria == null) { nullCriteria++; if (firstBad == null) firstBad = $"{c.id} criteria=null"; }
                        else if (r.criteria.targetsAffected == UnitConditionDto.MissingIndex
                              || r.criteria.playerTurn == UnitConditionDto.MissingIndex)
                        { unbound++; if (firstBad == null) firstBad = $"{c.id} criteria 未绑定"; }
                    }
                }
                // 🆕 v26（执行代理 `E2`）：ability 级那一列 —— `lg` 必须恒 `-1`。
                if (c.abilityLevelCriteria != null)
                {
                    if (c.abilityLevelCriteria.Length > 0) alcCards++;
                    foreach (var r in c.abilityLevelCriteria)
                    {
                        if (r == null) { badAb++; continue; }
                        alcRows++;
                        if (r.ab < 0) { badAb++; if (firstBad == null) firstBad = $"{c.id} ab={r.ab}"; }
                        if (r.lg != -1) { badLg++; if (firstBad == null) firstBad = $"{c.id} lg={r.lg}"; }
                        if (r.criteria == null) { nullCriteria++; if (firstBad == null) firstBad = $"{c.id} criteria=null"; }
                        else if (r.criteria.targetsAffected == UnitConditionDto.MissingIndex
                              || r.criteria.playerTurn == UnitConditionDto.MissingIndex)
                        { unbound++; if (firstBad == null) firstBad = $"{c.id} criteria 未绑定"; }
                        // 🆕 v29（`X6`）：`ca` 的三档读数。`null` = 缺键（`MapAbilityLevelCriteria`
                        //   已出声并按 `""` 处置）· `""` = 家族判不了（合法值，出声 + 放行那条路）。
                        if (r.ca == null) alcCaMissing++;
                        else if (r.ca.Length == 0) alcCaUnknown++;
                        else alcCaKnown++;
                    }
                }
                // 🆕 v27（执行代理 `E3`）：第一道闸那一列 —— `lg` 必须恒 `-1`。
                if (c.abilityActingCriteria != null)
                {
                    if (c.abilityActingCriteria.Length > 0) aacCards++;
                    foreach (var r in c.abilityActingCriteria)
                    {
                        if (r == null) { badAb++; continue; }
                        aacRows++;
                        if (r.ab < 0) { badAb++; if (firstBad == null) firstBad = $"{c.id} ab={r.ab}"; }
                        if (r.lg != -1) { badLg++; if (firstBad == null) firstBad = $"{c.id} lg={r.lg}"; }
                        if (r.criteria == null) { nullCriteria++; if (firstBad == null) firstBad = $"{c.id} criteria=null"; }
                        else if (r.criteria.targetsAffected == UnitConditionDto.MissingIndex
                              || r.criteria.playerTurn == UnitConditionDto.MissingIndex)
                        { unbound++; if (firstBad == null) firstBad = $"{c.id} criteria 未绑定"; }
                    }
                }
                // 🆕 v28（执行代理 `E4`）：主动能力目标闸那一列 —— `ab` 只收 `-1`/`-2`、`lg` 恒 `-1`。
                if (c.activeAbilityPickCriteria != null)
                {
                    if (c.activeAbilityPickCriteria.Length > 0) aapCards++;
                    foreach (var r in c.activeAbilityPickCriteria)
                    {
                        if (r == null) { badAb++; continue; }
                        aapRows++;
                        if (r.ab != -1 && r.ab != -2) { badAb++; if (firstBad == null) firstBad = $"{c.id} ab={r.ab}"; }
                        if (r.lg != -1) { badLg++; if (firstBad == null) firstBad = $"{c.id} lg={r.lg}"; }
                        if (r.criteria == null) { nullCriteria++; if (firstBad == null) firstBad = $"{c.id} criteria=null"; }
                        else if (r.criteria.targetsAffected == UnitConditionDto.MissingIndex
                              || r.criteria.playerTurn == UnitConditionDto.MissingIndex)
                        { unbound++; if (firstBad == null) firstBad = $"{c.id} criteria 未绑定"; }
                    }
                }
            }
            Debug.Log($"[RuleEngine] logic 级 `targetCriteria`（卡表 v{dto.version}）："
                    + $"`activeAbilityTargets` {aatCards} 张 / {aatRows} 行 · "
                    + $"`abilityTargetCriteria` {atcCards} 张 / {atcRows} 行"
                    + "（36 格/行；每张卡都落、没行 = `[]`）"
                    + "。✅ 2026-10-24（执行代理 `W2`）：`CardDef` 消费者**已接**"
                    + "（`CardDef.AbilityTargetCriteriaAt` / `ActiveAbilityTargetCriteriaAt` → "
                    + "`EffectOp.TargetCriteriaSet` → `EffectResolver.ResolveTargets`）。"
                    + (dto.version >= 26
                        ? $" ｜ ability 级 `targetCriteria`（`CardAbility.targetCriteria` `+0x30`）："
                        + $"`abilityLevelCriteria` {alcCards} 张 / {alcRows} 行（消费端 = "
                        + "`CardDef.AbilityLevelCriteriaForTrigger` → `ActionPump.EnqueueAbility` 的触发前闸）"
                        + (dto.version >= 29
                            ? $" ｜ 其中 `ca`（能力种类族名）：可判 {alcCaKnown} 行 · "
                            + $"判不了（空串）{alcCaUnknown} 行 · 缺键 {alcCaMissing} 行"
                            + "（消费端 = `CardDef.AbilityLevelCriteriaForFamily` → "
                            + "`RuleCore.AbilityFamilyTargetAllows` → 出牌正文逐 op 闸）"
                            : " ｜ `ca` 那一格：**这份表还没有**（v&lt;29 ⇒ 按家族认行一律判不了）")
                        : " ｜ ability 级那一列：**这份表还没有**（v&lt;26）")
                    + (dto.version >= 27
                        ? $" ｜ ability 级 `actingCardCriteria`（`CardAbility.actingCardCriteria` `+0x28`）："
                        + $"`abilityActingCriteria` {aacCards} 张 / {aacRows} 行（消费端 = "
                        + "`CardDef.ActingLevelCriteriaForTrigger` → `ActionPump.EnqueueAbility` 的**第一道闸**）"
                        : " ｜ 第一道闸那一列：**这份表还没有**（v&lt;27）")
                    + (dto.version >= 28
                        ? $" ｜ 主动能力目标闸 `ActiveAbility.targetCriteria`（`+0x38`）："
                        + $"`activeAbilityPickCriteria` {aapCards} 张 / {aapRows} 行（消费端 = "
                        + "`CardDef.ActiveAbilityPickCriteriaFor` → `RuleCore.CanUseAbility` 的目标闸）"
                        : " ｜ 主动能力目标闸那一列：**这份表还没有**（v&lt;28）"));
            int bad = badAb + badLg + nullCriteria + unbound;
            if (bad > 0)
                Debug.LogError($"[RuleEngine] `targetCriteria` 各族有 **{bad}** 处结构坏点"
                             + $"（ab {badAb} · lg {badLg} · criteria 缺 {nullCriteria} · 未绑定 {unbound}）"
                             + $"；首个 → {(firstBad ?? "?")}。重跑工具/gen_cards_engine.py。");
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

        /// <summary>
        /// 按阵营取卡（**不含隐藏附魔模板**）。
        ///
        /// <para>⛔ **🆕 2026-10-25（引擎线 `X20`）：`Type == "enchantment"` 的卡不在这里返回。**
        /// 它们（原版 `cardType 20` 的附魔卡，现读池内 1 张 `Pray Enchantment`）**不是可打出的卡** ——
        /// 不进卡组、不进手牌、不进收藏、不出现在卡包里；唯一存在形式是挂在某个 `CardInstance` 上
        /// （`Core/Enchantment.cs`）。判据/语义 → `CardDef.Type` 的 doc。</para>
        /// <para>⚠️ **为什么在这里挡而不是在每个调用点挡**：这个方法就是「这个阵营有哪些**卡**」的唯一入口，
        /// 下游有 `BattleDriver.DefensiveChoices`（防御池）/ `BoosterPackOpenWindow.RollPack`（开包摇号）等
        /// —— 后者按 `System.Random(seed)` 摇，把模板卡混进去会**静默改掉每一包的确定性内容**。
        /// 引擎侧要取那张附魔卡走 `ctx.CardPool`（`CardDatabase.Load()` 的**全池**，不过这一层）。</para>
        /// </summary>
        public static List<CardDef> OfFaction(IEnumerable<CardDef> pool, string faction)
        {
            var outList = new List<CardDef>();
            foreach (var c in pool)
                if (c != null && !c.IsEnchantment && c.Faction == faction) outList.Add(c);
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
        ///
        /// 🔴 **2026-10-24（执行代理 `E6`）：未命中不再静默**（判据 →
        /// `资料/普查产出_1024b/E4_录像头与对手聊天.md` 顺手发现 2/3）。
        /// 改前这一支的两种落空（**池里根本没这个名字** / **有这个名字、但都在别的阵营**）
        /// **都只是 `return null`** —— 调用方（阵亡语音、聊天兜底、卡组校验…）于是
        /// **静默什么都不做**，属于本工程红线里的「假装没有作用」那一档。
        /// ⇒ 现在两种落空都 `LogWarning` 出声，并**如实区分是哪一种**（消息里带 `pool` 的条数 /
        /// `name` / `faction`）。去重键 = `faction|name`：同一对只喊一次（这些调用点在
        /// 卡组校验那种循环里会被问很多遍，不去重会刷屏）。
        /// ⚠️ 这就是一条**告警**，返回值语义一个字没改（仍然是 `null`）。
        /// 读口 <see cref="FindMissWarningsForTest"/> 给自检数条数用（⛔ 别拿日志文本当判据）。
        /// </summary>
        public static CardDef Find(IEnumerable<CardDef> pool, string name, string faction)
        {
            if (string.IsNullOrEmpty(faction)) return Find(pool, name);
            foreach (var c in pool)
                if (c != null && c.Name == name && DeckRules.SameFaction(c.Faction, faction)) return c;
            // 阵营里没有同名卡 —— 不假装找到（调用方按 null 处理）**但要出声**
            NoteFactionFindMiss(pool, name, faction);
            return null;
        }

        /// <summary>🆕 2026-10-24（`E6`）：<see cref="Find(IEnumerable{CardDef}, string, string)"/> 的
        /// **未命中告警**去重表（键 = `faction|name`）。</summary>
        static readonly HashSet<string> _findMissWarned = new HashSet<string>(StringComparer.Ordinal);

        static int _findMissWarnCount;

        /// <summary>🆕 2026-10-24（`E6`）：<see cref="Find(IEnumerable{CardDef}, string, string)"/> 一共
        /// **喊过几次未命中**（按 `faction|name` 去重后）。自检的数条状判据走它 —— ⛔ 别去数日志文本
        /// （日志会被别的用例的 `Application.logMessageReceived` 抢走 / 被过滤）。</summary>
        public static int FindMissWarningsForTest { get { return _findMissWarnCount; } }

        static void NoteFactionFindMiss(IEnumerable<CardDef> pool, string name, string faction)
        {
            string key = faction + "|" + (name ?? "");
            if (!_findMissWarned.Add(key)) return;      // 同一对只喊一次
            _findMissWarnCount++;
            // ② 这一趟要回答的是「名字根本不在池里」还是「在、但阵营对不上」——
            //    后者十有八九是调用方传错了阵营（`E4` 那次 C4 断言差点因为样本跨阵营
            //    「因为错的原因变绿」）。两个数都报出来，别让读者再查一遍。
            int poolCount = 0, nameHits = 0; string firstOther = null;
            try
            {
                foreach (var c in pool)
                {
                    if (c == null) continue;
                    poolCount++;
                    if (c.Name != name) continue;
                    nameHits++;
                    if (firstOther == null) firstOther = c.Faction;
                }
            }
            catch (Exception e)
            {
                // 池子是 `IEnumerable`（可能是惰性迭代器）—— 真炸了也不能把查找搞崩。
                Debug.LogWarning($"[RuleEngine] `CardDatabase.Find(pool, \"{name}\", \"{faction}\")` "
                               + $"未命中，且**复查池子时抛了 {e.GetType().Name}** —— "
                               + "结论停在「没找到」，⛔ 别当成「池里没有」。");
                return;
            }
            if (nameHits == 0)
                Debug.LogWarning($"[RuleEngine] `CardDatabase.Find(pool, \"{name}\", \"{faction}\")` "
                               + $"**没找到**：池子里（{poolCount} 张）**根本没有这个名字**。"
                               + "调用方拿到的是 `null` —— 这一处**不会生效**（如实报出来，⛔ 不是静默跳过）。");
            else
                Debug.LogWarning($"[RuleEngine] `CardDatabase.Find(pool, \"{name}\", \"{faction}\")` "
                               + $"**没找到**：池子里有 **{nameHits} 张**叫这个名字，但**阵营都不是 "
                               + $"\"{faction}\"**（首张在 \"{firstOther}\"，池子共 {poolCount} 张）。"
                               + "调用方拿到的是 `null` —— 这一处**不会生效**；"
                               + "⚠️ 十有八九是**传错了阵营**（`Find` 是按 `DeckRules.SameFaction` 比的）。");
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

        /// <summary>把**卡组里的一条条目**解析成引擎那张卡 —— 异画感知 + 「**异画必须在卡表里**」收紧。
        ///
        /// <para>这是**显示侧**（`Shell/DeckInfoPopup.cs` 的督军位与卡表行、`Core/CostCurveDrawer.cs` 的
        /// 费用曲线）按 id 查卡时该走的那一个口：那三处原来是裸查 `CollectionData.Card(id)`
        /// ⇒ `#HB_SW1` 这种异画条目**查不到** ⇒ 督军立绘位空着 / 卡表少一行 / 费用曲线少 1 张
        /// （账 `HUD:92`/`HUD:93` 的显示侧那一半）。</para>
        ///
        /// <para>🔴 **「必须在卡表里」这条收紧的【唯一实现】= `PlayerDeck.ResolveEntry`**（判据 = `CardDef.AlternateArtsKnown`
        /// + `CardDef.HasAlternateArt`）：卡表里没登记的那一条由它返回 `null`，调用方按「查不到」处置
        /// （= 原版「资产里没有这张卡」同义）；卡表**没有这一列**（旧表 / 夹具直接 `new CardDef(...)`）⇒ 判不了 ⇒
        /// 按「接受」处置，⛔ 不许把「缺列」静默读成「这张卡没有异画」（三态口径 → `CardDef.AttachAlternateArts` 那段注）。</para>
        ///
        /// <para>⚠️ 🔴 2026-10-27（执行代理 `B9_W62FIX`）：本方法已**退化成纯转发** —— 原来这里还留着收紧的第二份半句，
        /// 收紧搬进 `ResolveEntry` 之后那半句**永远走不到**；两处写同一条规则迟早不一致 ⇒ 已收口到一处。</para>
        ///
        /// <para>📌 今天 **7 张异画全是督军** ⇒ 实际生效的是 `WarlordId` 位的 `#HB_SW1` 一类；
        /// `CardIds` 里今天不存在「合法的」异画条目（没有非督军的异画）。</para>
        /// </summary>
        public static CardDef ResolveDeckEntry(string id, Func<string, CardDef> lookup)
        {
            // 只转发：剥前缀 / 份数口径 / 「异画必须在卡表里」的**唯一实现都在 `PlayerDeck`**
            //   （`ResolveEntry` / `SameCardKey`，见 `RuleEngine/Core/DeckRules.cs`）⇒ ⛔ 这里不另写一套判断
            //   （本项目铁律：两处写同一条规则 = 迟早不一致）。收口后**行为逐条等价**：原来那半句
            //   `if (AlternateArtsKnown && !HasAlternateArt(id)) return null;` 现在**永远走不到**（收紧已在 `:890`）。
            return PlayerDeck.ResolveEntry(id, lookup, out _);
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

        // ==========================================================================================
        //  🆕 v37（2026-10-28 · 第八十九会话 · 执行代理 `E6b` · 账 L1044 `whileInPlayAnim` / L847 `permanentBuff`）
        //     —— `CardDto` 的两个新字段（+ 本层唯一的 `MapPermanentBuffs`）。
        //
        //  🔴 **为什么写在【文件末】、而不是 `CardDto` 体内**：
        //     `CardDatabase.cs` 有 **9 处账落点按【行号】引用**（现读 `74` / `1040` / `1610` / `1611` / `1690` /
        //     `1799` / `2303` / `2449` / `3291`），其中 `:1610`（5 行）· `:1611` · `:1799` · `:2449` 都是 **A 行**
        //     ⇒ 在 `CardDto`（`:33`–`:651`）里插行会让它们**整体漂移**（同族教训 → `CLAUDE.md` 那条「新成员一律追加到类尾」）。
        //     所以把 `CardDatabase` / `CardDto` 都标成 `partial`（`partial` 关键字是**原地改词、零行数变化**），
        //     新成员全部追加在**全部落点之后** ⇒ 零漂移。
        //     ⚠️ `JsonUtility` 按 `Type.GetFields()` 反射所有 public 字段，**不管它们在哪个 partial 段**
        //     ⇒ 两个字段放在这里与放在 `CardDto` 体内**完全等价**（字段名 = JSON 键名，逐字一致）。
        //  📌 判据 / 语义 / 缺值口径 **不在本文件抄第二份** → `Core/CardDef.cs` 的 `WhileInPlayAnim` 与
        //     `PermanentBuffSpec` 的 doc（铁律 6）。
        // ==========================================================================================
        public partial class CardDto
        {
            /// <summary>🆕 v37：原版 `RawCardScript.whileInPlayAnim`（`+0x288`，`int 0/1`）。
            /// <para>`-1` = **未知**（旧表 &lt; v37 / 真值快照里没这张卡）—— 由下面的调用点按 `dto.version >= 37` 落。
            /// ⛔ 别把缺键读成 `0` = 「原版就是 false」（`CardDef.WhileInPlayAnim` doc 里有同一句）。</para></summary>
            public int whileInPlayAnim = -1;

            /// <summary>🆕 v37：原版 `CardEffect.permanentBuff(+0x74) != 0` 的那些行（**稀疏**；`[]` = 明确没有）。
            /// <para>`null` / 缺列 = **未知**（旧表 &lt; v37 / 真值里没这张卡）⇒ `CardDef.HasPermanentBuffs == false`。</para></summary>
            public PermanentBuffRowDto[] permanentBuffs;
        }

        /// <summary>🆕 v37：`permanentBuffs` 的一行（**字段名与 JSON 键名逐字一致**，`JsonUtility` 直读）。
        /// <para>语义 / 为什么按 `(at, ab, lg)` 落 → <see cref="CardDef.PermanentBuffSpec"/> 的 doc
        /// （那是 🔶 自己设计的数据形状，⛔ 别拿它反推原版结构）。</para></summary>
        [Serializable]
        public class PermanentBuffRowDto
        {
            /// <summary>`"buffEffect"` | `"handBuff"` | `"enchantment"`（`null`/`""` = 坏值）。</summary>
            public string at;
            /// <summary>`cardAbilities` 下标（`-1` = 缺键/坏格）。</summary>
            public int ab = -1;
            /// <summary>`abilityLogic` 下标（`-1` = 缺键/坏格）。</summary>
            public int lg = -1;
            /// <summary>那一行的 `CardEffect.buffType(+0x38)`（`-1` = 坏值）。</summary>
            public int buffType = -1;
            /// <summary>`at == "enchantment"` 时 = 被挂上去那张模板的 `uniqueId`（其余 `null`）。</summary>
            public string token;
        }

        /// <summary>🆕 v37：`permanentBuffs` 列 → <see cref="CardDef.PermanentBuffSpec"/> 列表（**逐行**）。
        ///
        /// <para>⚠️ **返回 `null` ≠ 返回空表**（与 `MapAbilityDamage` / `MapActiveAbilityLogic` 同一契约）：
        /// `c.permanentBuffs == null`（旧表 / 真值里没这张卡）⇒ **`null` = 未知**；
        /// `[]` ⇒ **空表 = 明确没有永久效果**（现池 1,128 张）。</para>
        ///
        /// <para>**只做形状校验**（坏行出声丢弃 / 坏格出声落 `-1`），不做语义判断：
        /// ① `at` 不在三档白名单 ⇒ 出声 + 丢掉那一行（它是分组键，错一个就挂到别处）；
        /// ② `ab` / `lg` 缺或 &lt; 0 ⇒ 出声 + 丢掉那一行；
        /// ③ **同一 `(at, ab, lg)` 撞键** ⇒ 出声丢弃后一条（⛔ 谁赢都说不清；现读 0 例）。
        /// 📌 生成器侧口径 → `工具/gen_cards_engine.py` 的 `permanent_buffs_of` 的 doc。</para></summary>
        static CardDef.PermanentBuffSpec[] MapPermanentBuffs(CardDto c)
        {
            return c == null ? null : MapPermanentBuffs(c.permanentBuffs, c.id, c.name);
        }

        /// <summary>（同上，但吃**裸行数组** —— 第二张表 `enchantment_templates.json` 复用**同一条规则**，⛔ 别另写一份。）
        /// <para>`rows == null` ⇒ `null`（未知）；`[]` ⇒ 空表（明确没有）。`cardId` / `cardName` 只用于出声。</para></summary>
        internal static CardDef.PermanentBuffSpec[] MapPermanentBuffs(PermanentBuffRowDto[] rows,
                                                                      string cardId, string cardName)
        {
            if (rows == null) return null;   // 缺列 = 未知（⛔ 不是「没有行」）
            var list = new List<CardDef.PermanentBuffSpec>(rows.Length);
            var seen = new HashSet<string>();
            foreach (var r in rows)
            {
                if (r == null) continue;
                if (r.at != "buffEffect" && r.at != "handBuff" && r.at != "enchantment")
                {
                    Debug.LogError($"[RuleEngine] 卡「{cardName}」（{cardId}）的 `permanentBuffs` 有一行 `at` 不认识"
                                 + $"（at=\"{r.at}\"）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.ab < 0 || r.lg < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{cardName}」（{cardId}）的 `permanentBuffs` 有一行缺 `ab`/`lg`"
                                 + $"（at={r.at}, ab={r.ab}, lg={r.lg}）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                string k = r.at + "|" + r.ab + "|" + r.lg;
                if (!seen.Add(k))
                {
                    Debug.LogError($"[RuleEngine] 卡「{cardName}」（{cardId}）的 `permanentBuffs` 有**重复的 "
                                 + $"(at={r.at}, ab={r.ab}, lg={r.lg})** —— 后一条已丢弃（⛔ 撞键 = 谁赢都说不清）。");
                    continue;
                }
                list.Add(new CardDef.PermanentBuffSpec
                {
                    At = r.at, Ab = r.ab, Lg = r.lg, BuffType = r.buffType, TokenCardId = r.token,
                });
            }
            return list.ToArray();
        }

        // ==========================================================================================
        //  🆕 B89 / E6c（2026-10-28 · 执行代理 `E6c`）：**第二张表** —— 附魔模板
        //     `Assets/RuleEngine/Resources/enchantment_templates.json`（由 `工具/gen_cards_engine.py` 写）
        // ==========================================================================================
        //  🔴 **它为什么不是第 1129 张卡**：收录的是真值里 `m_Name` 尾 `_enchantment` 的**模板**（现读 85 张），
        //     它们**不在** `cards_engine.json` 的 `cards[]` 里（v30 入池闸 = `cardType == 20` ∧ 名字含 `enchant`）。
        //     调度台 2026-10-28 裁定：**⛔ 不放宽入池闸**（会改 `count` ⇒ 影响可玩池与探针基线；
        //     附魔模板**不是可玩卡**）⇒ 单开一张表，只服务「**按 id 查模板**」这一条读路。
        //  📌 口径 / 字段语义 / 张数 → 生成器 `build_enchantment_templates` 的 doc 与产物自己的 `note`
        //     （⛔ 不在本文件抄第二份，铁律 6）。
        //  🔴 **注入点不在本文件**：`Core/` 不能引用 `Data/`（`Enchantment.cs:703`：`ruleprobe` 只编 `Core/*.cs`）
        //     ⇒ 本方法只负责**读成 `List<CardDef>`**，由外层（`BattleDriver` / `ruleprobe` 夹具）把它交给
        //     `BattleContext` 的新格（**待接**，`BattleContext.cs` 归 `E14`；可照抄的一行见
        //     `资料/完全复刻/报告/B89_E6c_附魔模板表.md` §3）。
        // ==========================================================================================
        /// <summary>第二张表的资源名（`Resources/enchantment_templates.json`）。</summary>
        public const string EnchantmentTemplatesResourcePath = "enchantment_templates";

        /// <summary>「表读不到」只出声一次（否则每次战斗第一个附魔查找都会刷一条）。</summary>
        static bool _enchantTemplatesMissingLogged;

        /// <summary>🆕 B89 / E6d：把第二张表的加载器登记进 `BattleContext` 的钩子。
        /// <para>🔴 **这是「Core 不引用 Data」这条方向约束下的唯一注入点** —— `Core/BattleContext.cs` 只声明
        /// `EnchantmentTemplatesProvider` 这个委托，由**本静态构造**填上（`Data` → `Core` 是允许的方向）。</para>
        /// <para>⚠️ 静态构造在**第一次碰到 `CardDatabase` 任何成员**时跑（生产侧 = 开局 `Load()`），
        /// 所以 `ruleprobe` / 夹具只要碰过本类，钩子就是通的。</para></summary>
        static CardDatabase()
        {
            BattleContext.EnchantmentTemplatesProvider = LoadEnchantmentTemplates; Enchantments.GrantedEnchantmentsProvider = EnchantmentGrantsOf;   // 🆕 v38（B90/B3）：附魔「打出即挂」的数据源钩子（Core 不引用 Data ⇒ 只能这样注入）
        }

        /// <summary>第二张表的文件 DTO（字段名与 JSON 键名逐字一致，`JsonUtility` 直读）。</summary>
        [Serializable]
        public class EnchantmentTemplateFileDto
        {
            public int version;
            public int count;
            public string source;
            public string note;
            public EnchantmentTemplateDto[] templates;
        }

        /// <summary>一张附魔模板（口径 → 生成器 `build_enchantment_templates` 的 doc）。</summary>
        [Serializable]
        public class EnchantmentTemplateDto
        {
            /// <summary>原版 `uniqueId`（**唯一 join 键**：`buffEffect.enchantment.m_PathID` → 这张模板）。</summary>
            public string id;
            public string name;
            public string nameZh;
            /// <summary>原版 `m_Name`（尾 `_enchantment`；排查用）。</summary>
            public string mName;
            public int cardType = -1;
            public int spellType = -1;
            public int manaCost = -1;
            /// <summary>模板自己的 `ActiveAbility.psychicCost`（`-1` = 未知）。</summary>
            public int psychicCost = -1;
            public int plasmaCost = -1;
            public int targeted = -1;
            /// <summary>原版 `RawCardScript.whileInPlayAnim(+0x288)`：`1` / `0` / `-1`（未知）。</summary>
            public int whileInPlayAnim = -1;
            public string[] cardTraits;
            /// <summary>与卡表 v37 的 `permanentBuffs` **同形**（复用 `MapPermanentBuffs`）。</summary>
            public PermanentBuffRowDto[] permanentBuffs;
            /// <summary>模板自己的 ability 行（只带四格标量：`ca` / `pa` / `ta` / `fm`）。</summary>
            public EnchantmentTemplateAbilityDto[] abilities;
            /// <summary>谁挂它（反向索引；`inPool` = 挂载者自己是不是**可玩池**里的卡）。</summary>
            public EnchantmentTemplateOwnerDto[] owners;
        }

        [Serializable]
        public class EnchantmentTemplateAbilityDto
        {
            public int trigger = -1;
            public EnchantmentTemplateLogicDto[] logic;
        }

        [Serializable]
        public class EnchantmentTemplateLogicDto
        {
            /// <summary>`AbilityLogic.chosenAbility`。</summary>
            public int ca = -1;
            /// <summary>`AbilityLogic.playersAffected`。</summary>
            public int pa = -1;
            /// <summary>`AbilityLogic.targetCriteria.targetsAffected`。</summary>
            public int ta = -1;
            /// <summary>`AbilityLogic.furtherMultiplier`。</summary>
            public int fm = -1;
        }

        [Serializable]
        public class EnchantmentTemplateOwnerDto
        {
            public string id;
            public int buffType = -1;
            public int trigger = -1;
            public bool inPool;
        }

        /// <summary>读第二张表 → 模板的 `CardDef` 列表（**不并入** <see cref="Load"/> 的可玩池）。
        ///
        /// <para>每张模板造一个 `CardDef`：`type = "enchantment"` · `rarity`/`faction` 空（模板不是可玩卡、不进收藏）·
        /// `inventoryOptions = 10 (NotInInventory)` · `showInCollectionOptions = 0 (Hide)`（与 v30 那一批隐藏模板同口径）·
        /// `spellType` / `cardType` / `whileInPlayAnim` / `permanentBuffs` 从表里灌。</para>
        /// <para>⚠️ **读不到就出声并返回空表**（⛔ 不许静默）；调用方要能区分「表不存在」与「表是空的」请先看日志。</para></summary>
        public static List<CardDef> LoadEnchantmentTemplates()
        {
            var list = new List<CardDef>();
            var asset = Resources.Load<TextAsset>(EnchantmentTemplatesResourcePath);
            if (asset == null)
            {
                if (!_enchantTemplatesMissingLogged)   // 只出声一次（每次战斗都会问到，别刷屏）
                {
                    _enchantTemplatesMissingLogged = true;
                    Debug.LogError($"[RuleEngine] 读不到第二张表 `Resources/{EnchantmentTemplatesResourcePath}.json`"
                                 + " —— 附魔模板的 `whileInPlayAnim` / 效果行**一律取不到**。"
                                 + "重跑 `python -I -X utf8 工具/gen_cards_engine.py --write`。");
                }
                return list;
            }
            var file = JsonUtility.FromJson<EnchantmentTemplateFileDto>(asset.text);
            if (file == null || file.templates == null)
            {
                Debug.LogError($"[RuleEngine] 第二张表解析失败（{asset.text.Length} 字符）—— 返回空表。");
                return list;
            }
            var seen = new HashSet<string>();
            foreach (var t in file.templates)
            {
                if (t == null || string.IsNullOrEmpty(t.id))
                {
                    Debug.LogError("[RuleEngine] 附魔模板表里有一行**没有 `id`** —— 该行丢弃。");
                    continue;
                }
                if (!seen.Add(t.id))
                {
                    Debug.LogError($"[RuleEngine] 附魔模板表里 `id = {t.id}` **重复** —— 后一条丢弃。");
                    continue;
                }
                var def = new CardDef(t.id, string.IsNullOrEmpty(t.name) ? t.id : t.name,
                                      "enchantment", "", "", "",
                                      t.manaCost < 0 ? 0 : t.manaCost, 0, 0, 0, null,
                                      spellType: t.spellType,
                                      inventoryOptions: 10, showInCollectionOptions: 0);
                def.AttachCardType(t.cardType);
                def.AttachWhileInPlayAnim(t.whileInPlayAnim);
                def.AttachPermanentBuffs(MapPermanentBuffs(t.permanentBuffs, t.id, t.name));
                list.Add(def);
            }
            if (file.count != list.Count)
                Debug.LogError($"[RuleEngine] 附魔模板表 `count` = {file.count}，实读 {list.Count} 张 —— 表坏了。");
            Debug.Log($"[RuleEngine] 附魔模板表 v{file.version}：**{list.Count}** 张"
                    + $"（`whileInPlayAnim == 1` {CountWhileInPlayAnim(list)} 张）—— 来自 `Resources/{EnchantmentTemplatesResourcePath}.json`");
            return list;
        }

        /// <summary>`whileInPlayAnim == 1` 的张数（读数辅助；⛔ 不参与任何判据）。</summary>
        public static int CountWhileInPlayAnim(List<CardDef> templates)
        {
            int n = 0;
            if (templates == null) return 0;
            for (int i = 0; i < templates.Count; i++)
                if (templates[i] != null && templates[i].WhileInPlayAnim == 1) n++;
            return n;
        }

        // ==========================================================================================
        //  🆕 v38（2026-10-28 · B90 波 2 · 执行代理 `B3`）：**卡表 `enchantment` 列**的装载端
        // ==========================================================================================
        //  🔴 为什么加**第三个 partial 段**、而不是改 `Parse()`：`账/*.tsv` 按【行号】引用本文件
        //     （`1610` / `1611` / `1690` / `1744` / `1799` / `2303` / … / `3567`），`Parse()` 里插一行
        //     会让它们整体漂移 ⇒ 新成员只许追加在**全部落点之后**（= 这里）。
        //     ⇒ 索引不新增解析规则，而是**复用 `Load()` 刚解析出来的那份文本**（`Load()` 末尾那一行
        //       追加了 `EnsureEnchantmentGrantsFrom(asset.text)`，⛔ 不是第二套 `FromJson` 口径）。
        //  🔴 **`ruleprobe` 里的行为必须逐字保持**：桩 `Resources.Load`（`工具/ruleprobe/Stub.cs:25`）
        //     恒 `null` ⇒ `Load()` 直接返回空表、**根本走不到**建索引那一步 ⇒ 索引为空 ⇒
        //     `Enchantments.GrantedEnchantmentsProvider` 返回 `null` ⇒ 消费端退**白名单**（= 改前行为）
        //     ⇒ `play all dump` / `play check` 的**指纹不变**。⛔ 别把「建不出来」改成「返回空表」，
        //     那会让 `Enchantments.GrantedEnchantmentRowsOf` 误以为「这张卡确实不挂」而**跳过白名单兜底**。
        //  📌 列形状 / 判据 / 为什么 `buffType 0/1/3` 不算 → `工具/gen_cards_engine.py` 的
        //     `enchantment_grants_of` 上面那段长注释（铁律 6：⛔ 别在这里抄第二份）。
        // ==========================================================================================

        /// <summary>`enchantment` 列的一行（字段名与 JSON 键名逐字一致，`JsonUtility` 直读）。</summary>
        [Serializable]
        public class EnchantmentGrantRowDto
        {
            /// <summary>效果挂在哪个对象上（现读 58 行全是 `"buffEffect"`；`handBuff.cardEffect` 0 行）。</summary>
            public string at;
            /// <summary>`cardAbilities` 下标（只用于排查，⛔ 不参与判据）。</summary>
            public int ab = -1;
            /// <summary>`abilityLogic` 下标（同上）。</summary>
            public int lg = -1;
            /// <summary>原版 `CardEffect.buffType`（只有 `12` / `15` / `16` 三档才会走到 `AddEnchantment`）。</summary>
            public int buffType = -1;
            /// <summary>原版 `CardAbility.trigger`（`AbilityTrigger`；`0 = ThisCardPlayed`）。</summary>
            public int trigger = -1;
            /// <summary>原版 `targetCriteria.targetsAffected`（`-1` = 读不到）。**载体是谁**由它定：
            /// `50 = inHandFiltered`（选中的手牌，`DA42` 那一档 ⇒ 专属链已接，⛔ 本钩子不许再挂）。</summary>
            public int ta = -1;
            /// <summary>被挂上去那张模板的 `uniqueId`（= 第二张表 `enchantment_templates.json` 的 `id`）。</summary>
            public string token;
        }

        public partial class CardDto
        {
            /// <summary>🆕 v38：这张卡**自己**「挂附魔」的那些行（稀疏）。
            /// <para>`null` / 缺列 = **未知**（旧表 &lt; v38 / 真值里没这张卡）；`[]` = 明确没有。</para></summary>
            public EnchantmentGrantRowDto[] enchantment;
        }

        /// <summary>`uniqueId → 该卡的「挂附魔」数据行`（懒建；`null` = 还没建）。</summary>
        static Dictionary<string, List<Enchantments.GrantedEnchantmentRow>> _enchantGrants;

        /// <summary>「建不出来」只出声一次（每次出牌都会问到，别刷屏）。</summary>
        static bool _enchantGrantsMissingLogged;

        /// <summary>丢掉索引（`InvalidateCache()` 里一并调 —— 编辑器改完卡表重读时用）。</summary>
        public static void InvalidateEnchantmentGrants() { _enchantGrants = null; _offensiveEnvGrantsMerged = false; }   // 🆕 B93/E25：同一句里重置「进攻卡那 37 行已并表」的标记（⛔ 别另加一行 —— 下游有 A 行落点）

        /// <summary>这张卡（按 `uniqueId`）的「挂附魔」数据行；`null` = 没有 / 索引建不出来。</summary>
        public static List<Enchantments.GrantedEnchantmentRow> EnchantmentGrantsOf(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            List<Enchantments.GrantedEnchantmentRow> rows;
            EnsureOffensiveEnvGrantsMerged(); return (_enchantGrants != null && _enchantGrants.TryGetValue(cardId, out rows)) ? rows : null;   // 🆕 B93/E25：进攻卡的 37 行在**同一张**表里（并表见 `EnsureOffensiveEnvGrantsMerged`；挤在一行是故意的，加行会让下游 A 行落点漂移）
        }

        /// <summary>把卡表文本里的 `enchantment` 列物化成一棵反查表（幂等；`Load()` 末尾调）。
        /// <para>⚠️ 表太旧（&lt; v38）/ 解析失败 ⇒ **出声一次并保持空表**（消费端退白名单）——
        /// ⛔ 不许静默把「读不到」当成「这张卡不挂附魔」。</para></summary>
        public static void EnsureEnchantmentGrantsFrom(string json)
        {
            if (_enchantGrants != null || string.IsNullOrEmpty(json)) return;
            var dto = JsonUtility.FromJson<FileDto>(json);
            if (dto == null || dto.cards == null)
            {
                if (!_enchantGrantsMissingLogged)
                {
                    _enchantGrantsMissingLogged = true;
                    Debug.LogError($"[RuleEngine] 卡表解析失败（{json.Length} 字符）—— 附魔「打出即挂」"
                                 + "整条链**只剩白名单那 3 张**。");
                }
                return;
            }
            if (dto.version < 38)
            {
                if (!_enchantGrantsMissingLogged)
                {
                    _enchantGrantsMissingLogged = true;
                    Debug.LogError($"[RuleEngine] 卡表 version={dto.version} < 38 —— 没有 `enchantment` 列"
                                 + "（原版 `CardEffect.enchantment +0x88` ∧ `buffType ∈ {12,15,16}` ⇒ "
                                 + "`CardScript__AddEnchantment`）。附魔「打出即挂」**只剩白名单那 3 张**；"
                                 + "跑一下 gen_cards_engine.py --write 重生成。");
                }
                return;
            }
            var idx = new Dictionary<string, List<Enchantments.GrantedEnchantmentRow>>();
            int bad = 0, rows = 0;
            foreach (var c in dto.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.id) || c.enchantment == null || c.enchantment.Length == 0)
                    continue;
                var list = new List<Enchantments.GrantedEnchantmentRow>(c.enchantment.Length);
                foreach (var r in c.enchantment)
                {
                    if (r == null || string.IsNullOrEmpty(r.token))
                    {
                        bad++;
                        continue;
                    }
                    list.Add(new Enchantments.GrantedEnchantmentRow
                    {
                        SourceCardId = c.id,
                        TemplateCardId = r.token,
                        BuffType = r.buffType,
                        Trigger = r.trigger,
                        TargetsAffected = r.ta,
                        AbilityIndex = r.ab,
                        LogicIndex = r.lg,
                    });
                    rows++;
                }
                if (list.Count > 0) idx[c.id] = list;
            }
            _enchantGrants = idx;
            Debug.Log($"[RuleEngine] 附魔挂载索引 v{dto.version}：**{idx.Count}** 张卡 / **{rows}** 行"
                    + $"（坏行 {bad}）—— 来自 `Resources/{ResourcePath}.json` 的 `enchantment` 列");
            if (bad > 0)
                Debug.LogError($"[RuleEngine] `enchantment` 列有 **{bad}** 行没有 `token` —— 已丢弃（不许静默）。");
        }
    }

    // ==================================================================================================
    //  🆕 v39（2026-10-28 · 执行代理 `A4` · 判据 `资料/完全复刻/判据/B91_P3_战斗数据列缺口.md` §二·1 / §二·8）
    //     —— 卡表 `abilitySummon` 列（原版 `SummonCriteria` 的 7 格）
    //
    //  🔴 **为什么另起一个 `partial` 块、写在【文件最末】**：本文件里已有 **23 处账落点按【行号】引用**
    //     （现读最大 `CardDatabase.cs:3772`）⇒ 在类中段插成员会让它们整体漂移
    //     （同族纪律 → `Core/CardDef.cs` 末尾 `E6` 那段的注释）。
    //  ⚠️ 挂载那一句**挤在 `:1749` 原有那一行里**（故意不加行）—— 那一行自己的行尾注释已写明
    //     「加行会把 `:1799` 等下游 A 行账落点整体推走」。⛔ 别为了排版把它拆成新行。
    //  ⚠️ **本列只落数据 / 只挂载，没有任何消费者** ⇒ 零行为变化、不改指纹。
    //     消费端（`RuleCore.DeployFreeCore` 的 `requestSlot` / `EffectResolver.DoDeploy`）在别的代理白名单里。
    // ==================================================================================================
    public static partial class CardDatabase
    {
        /// <summary>🆕 v39：`abilitySummon` 的一行（= 原版 `SummonCriteria` 的七格）。
        /// 字段名与 JSON 键名**逐字一致**（`JsonUtility` 直读）。语义 → <see cref="CardDef.AbilitySummonSpec"/>。
        /// <para>⚠️ 七格都用 `-1` 当「坏值/缺键」哨兵 —— `0` 是**合法取值**
        /// （`cap = 0` 就是「贴施放者」那一档）。⛔ 别把缺键读成 0。</para></summary>
        [Serializable]
        public class AbilitySummonRowDto
        {
            /// <summary>`cardAbilities` 下标（`-1` = 缺键）。</summary>
            public int ab = -1;
            /// <summary>`abilityLogic` 下标（`-1` = 缺键）。</summary>
            public int lg = -1;
            /// <summary>原版 `SummonCriteria.createAtPos (+0x20)`：`0` self / `1` cardTarget / `3` friendlyHero /
            /// **其余 ⇒ 出声 + 默认落点**（`14` 也在这一档，⛔ 不是「贴卡」）。</summary>
            public int cap = -1;
            /// <summary>原版 `SummonCriteria.moveToPos (+0x24)`（**只供演出取起点**，盘面不读）。</summary>
            public int mv = -1;
            /// <summary>原版 `SummonCriteria.summonAllTargets (+0x28)`：真 ⇒ 整张候选表全给；否则随机取一张。</summary>
            public int all = -1;
            /// <summary>原版 `SummonCriteria.chooseCardInPlay (+0x29)`：真 ⇒ `unitsToSummon` 空时改取 `GetTargets` 的目标集。</summary>
            public int choose = -1;
            /// <summary>原版 `SummonCriteria.keepEffects (+0x2A)`（`AddSummonUnit` 实参；池内全 0 = 休眠）。</summary>
            public int keep = -1;
            /// <summary>原版 `SummonCriteria.shouldRemoveFromCemetery (+0x2B)`：造完把候选从墓地移除。</summary>
            public int cem = -1;
            /// <summary>原版 `SummonCriteria.isForEnemy (+0x10)`：选卖哪个 `MinionArea`。</summary>
            public int enemy = -1;
        }

        public partial class CardDto
        {
            /// <summary>🆕 v39：这张卡**逐 `abilityLogic` 行**的 `summonCriteria`（与 `abilityDamage` 同构）。
            /// <para>`null` / 缺列 = **未知**（旧表 &lt; v39）；`[]` = 明确没有行。</para></summary>
            public AbilitySummonRowDto[] abilitySummon;
        }

        /// <summary>🆕 v39：`abilitySummon` 列 → <see cref="CardDef.AbilitySummonSpec"/>。
        /// <para>⚠️ 行级坏值口径与 `MapAbilityDamage` **逐字同族**：缺 `ab`/`lg` ⇒ 出声 + 丢弃该行；
        /// 同一 `(ab,lg)` 撞键 ⇒ 出声 + 丢弃后一条（⛔ 撞键 = 谁赢都说不清）。</para></summary>
        static CardDef.AbilitySummonSpec[] MapAbilitySummon(CardDto c)
        {
            if (c == null || c.abilitySummon == null) return null;   // 缺列 = 未知（⛔ 不是「没有行」）
            var list = new List<CardDef.AbilitySummonSpec>(c.abilitySummon.Length);
            var seen = new HashSet<long>();
            foreach (var r in c.abilitySummon)
            {
                if (r == null) continue;
                if (r.ab < 0 || r.lg < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilitySummon` 有一行缺 `ab`/`lg`"
                                 + $"（ab={r.ab}, lg={r.lg}）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                long k2 = ((long)r.ab << 32) ^ (uint)r.lg;
                if (!seen.Add(k2))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilitySummon` 有**重复的 "
                                 + $"(ab={r.ab}, lg={r.lg})** —— 后一条已丢弃（⛔ 撞键 = 谁赢都说不清）。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                list.Add(new CardDef.AbilitySummonSpec
                {
                    Ab = r.ab, Lg = r.lg,
                    CreateAtPos = r.cap, MoveToPos = r.mv,
                    SummonAllTargets = r.all, ChooseCardInPlay = r.choose,
                    KeepEffects = r.keep, ShouldRemoveFromCemetery = r.cem,
                    IsForEnemy = r.enemy,
                });
            }
            return list.ToArray();
        }
    }

    // ==================================================================================================
    //  🆕 B93 / E16（2026-10-28 · 执行代理 `B93_E16`）：**第三张表** —— 进攻卡（环境效果卡）
    //     `Assets/RuleEngine/Resources/offensive_env_cards.json`（由 `工具/gen_offensive_env_cards.py` 写）
    // ==================================================================================================
    //  🔴 **它们为什么不是组牌池的第 1130…1168 张**：原版判据 = 39/39 `inventoryOptions 5
    //     (CantAddToDeck)` ∧ `showInCollectionOptions 0 (Hide)`（`DeckUtility__ValidateDeckCards.c:68-77`：
    //     `inv != 0` ⇒ 非法；`show != 5` ⇒ 非法）⇒ 这 39 张**不是组牌池成员**。塞进 `cards_engine.json`
    //     会改 `count`（1129 → 1168）、重钉 `PinnedPoolMd5` / `PinnedPlayMd5`、并要改
    //     `Editor/RuleEngineTest_EngineE1.cs:80` 那条硬编码张数断言。
    //     调度台 2026-10-28 裁定「**方案乙**」：单开一张表（与同日的附魔模板 `enchantment_templates.json`
    //     同一档待遇、同一句先例 `工具/gen_cards_engine.py:4536-4546`：「⛔ 不放宽入池闸（会改 `count`
    //     ⇒ 影响可玩池与探针基线；附魔模板**不是可玩卡**）」）。
    //  📌 口径 / 收录范围 / 三张表怎么 join / 缺值哨兵 → 生成器 `工具/gen_offensive_env_cards.py` 的
    //     头部注释 ＋ 产物自己的 `note`（⛔ 不在本文件抄第二份，铁律 6）。
    //  🔴 **本表今天【只有数据、还没有消费者】**：`BattleDriver.offensiveCardEnabled` 现读仍是 `false`
    //     （用户 2026-10-11「进攻卡暂时不要」⇒ 2026-10-28 改判「恢复，照原版做」）。要让它真正跑起来，
    //     `BattleDriver` 三处 + `chosenAbility` 分派口要一起改 —— 逐处清单（含现读行号）→
    //     `资料/完全复刻/报告/B93_E16.md` §4。⛔ 本笔只交数据与读取端，**没碰 `BattleDriver.cs`**。
    //  ⚠️ **`ruleprobe` 的行为必须逐字保持**：`工具/ruleprobe/Stub.cs:25` 的 `Resources.Load` 恒 `null`
    //     ⇒ 懒加载拿到空表（只出声一次）⇒ 本段**零行为变化**、`PinnedPlayMd5` / `PinnedPoolMd5` 都不动。
    //  ⚠️ **本段追加在【文件最末】**：`账/*.tsv` 按【行号】引用本文件（现读最大落点 `:3772`），
    //     在类中段插成员会让它们整体漂移（与上面 A4 那段同一条纪律）。
    // ==================================================================================================
    public static partial class CardDatabase
    {
        /// <summary>第三张表的资源名（`Resources/offensive_env_cards.json`）。</summary>
        public const string OffensiveEnvResourcePath = "offensive_env_cards";

        /// <summary>「表读不到」只出声一次（别每次开局都刷一条）。</summary>
        static bool _offensiveEnvMissingLogged;

        /// <summary>懒加载缓存：`null` = 还没读过（**表不存在时也留 `null`** —— 补上资源后还能读到）；
        /// 空表 = 读到了但**解析失败**（不再重试，别刷屏）。</summary>
        static List<CardDef> _offensiveEnvCards;

        /// <summary>丢掉这张表的缓存（<see cref="InvalidateCache"/> 里一并调）。</summary>
        public static void InvalidateOffensiveEnvCards() { _offensiveEnvCards = null; _offensiveEnvRows = null; _offensiveEnvById = null; _offensiveEnvGrantsMerged = false; }   // 🆕 B93/E25：重读表 ⇒ 「已并表」标记一并作废（并表见 `EnsureOffensiveEnvGrantsMerged`）

        /// <summary>第三张表的文件 DTO（字段名与 JSON 键名**逐字一致**，`JsonUtility` 直读）。</summary>
        [Serializable]
        public class OffensiveEnvFileDto
        {
            public int version;
            public int count;
            public string source;
            public string note;
            public OffensiveEnvCardDto[] cards;
        }

        /// <summary>一张进攻卡（口径 → 生成器头部注释；⚠️ 以下所有 `-1` 都是「真值里没有这个键」的哨兵，
        /// `0` 是**合法取值** —— 例 `targetsAffected 0 = any`、`playersAffected 20 = Enemy`）。</summary>
        [Serializable]
        public class OffensiveEnvCardDto
        {
            /// <summary>原版 `uniqueId`（= 我们的 `CardDef.Id`，`AM63` / `ASH59` 这种短码）。</summary>
            public string id;
            /// <summary>**官方显示名**（`Card_Name/&lt;id&gt;` 英文列）。</summary>
            public string name;
            public string nameZh;
            /// <summary>`cards_truth.json` 的 `name`（**旧名**，只供对账）。</summary>
            public string nameTruth;
            /// <summary>运行时 `Resources/OffensiveCards.json` 里那一格看到的卡名 ——
            /// ⚠️ 有 **6 处**与 <see cref="name"/> 不同（`nameMatchOfficial == 0`，都是「贴图名 / SO 占位名反推」）。</summary>
            public string offensiveCardName;
            /// <summary>`1` = 运行时卡名就是官方显示名；`0` = 不是（6 处）；`-1` = 表里没这一格。</summary>
            public int nameMatchOfficial = -1;
            /// <summary>阵营串 —— **与 `OffensiveCards.json` 的 `army` / `CardDef.Faction` 同一个写法**
            /// （`BattleDriver.FactionOf(...)` 拿到的就是这一串）。</summary>
            public string faction;
            /// <summary>原版 `CardArmy` 枚举数值（10 Ultramarines / 20 Goff / … / 130 SpaceWolves）。</summary>
            public int army = -1;
            /// <summary>槽号 **1..3**（`offensive_cards.json` 的 `slot`）。</summary>
            public int slot = -1;
            /// <summary>槽号 **0..2** = `slot - 1` —— 与 `Resources/OffensiveCards.json` 的 `cards[].idx`
            /// **同一个数**，也就是 `BattleContext.OffensiveSlotIdx` 存的那个值
            /// （`-1` = 「不使用进攻卡」那张空卡，**不在本表**）。</summary>
            public int idx = -1;
            /// <summary>原版 SO 的 `forgeLevel`（4 / 25 / 40|45；`OffensiveCards.ChoicesOwnedAt` 那道闸用它）。</summary>
            public int forgeLevel = -1;
            /// <summary>环境 SO 名（`ApplyEnvironment` 要的那一条，= `OffensiveCards.json` 的 `envSO`）。</summary>
            public string envSO;
            /// <summary>卡面正文英文（官方 `Card_Description/&lt;id&gt;`）—— 🔴 是**触发句**
            /// （「When a player earns a Skull, …」），**不是**「打出去做什么」的可解析效果文本。</summary>
            public string desc;
            /// <summary>卡面正文中文（同上中文列）。</summary>
            public string descZh;
            /// <summary>原版 `RawCardScript.cardType`（39/39 = `20` = `CardTypeOptions.Tactic`）。</summary>
            public int cardType = -1;
            /// <summary>原版 `RawCardScript.spellType`（39/39 = `200` = `OffensiveCard`）。</summary>
            public int spellType = -1;
            /// <summary>原版 `cardRarity`（39/39 = `5` ⇒ 我们的 `"special"`）。</summary>
            public int rarity = -1;
            /// <summary>39/39 = `5` = `CardInventoryOptions.CantAddToDeck`（⇒ `DeckRules.CannotAddToDeck` 拦）。</summary>
            public int inventoryOptions = -1;
            /// <summary>39/39 = `0` = `ShowInCollectionOptions.Hide`（⇒ 收藏 / 卡组编辑都不显示）。</summary>
            public int showInCollectionOptions = -1;
            /// <summary>39/39 = `1`（⇒ 「随机生成一张卡」抽不到，`EffectResolver.cs:4689`）。</summary>
            public int cantBeRandomlyCreated;
            /// <summary>`cardAbilities[].trigger` 的去重集合（39/39 = `[0]` = `ThisCardPlayed`）。</summary>
            public int[] abilityTriggers;
            /// <summary>能力树（逐叶摊平，字段一个不丢；口径 → 生成器 `_ability_rows`）。</summary>
            public OffensiveEnvAbilityDto[] abilities;
        }

        [Serializable]
        public class OffensiveEnvAbilityDto
        {
            /// <summary>原版 `CardAbility.trigger`（39/39 = `0` = `AbilityTrigger.ThisCardPlayed`）。</summary>
            public int trigger = -1;
            public OffensiveEnvLogicDto[] logic;
        }

        /// <summary>`abilityLogic[j]` 的一行（原版 `AbilityLogic` ＋ `targetCriteria` ＋ `buff` ＋ `manaBuff`）。</summary>
        [Serializable]
        public class OffensiveEnvLogicDto
        {
            /// <summary>原版 `AbilityLogic.chosenAbility`（`AbilityEffect`：0 / 30 / 40 / 60）。</summary>
            public int chosenAbility = -1;
            /// <summary>原版 `AbilityLogic.playersAffected`（`PlayersAffected`：0 Both / 10 Self / 20 Enemy）。</summary>
            public int playersAffected = -1;
            /// <summary>原版 `TargetCriteria.targetsAffected`（120 `friendlyHero` / 130 `enemyHero` / 20 `self` / 0 `any`）。</summary>
            public int targetsAffected = -1;
            public int friendlyUnits = -1;
            public int enemyUnits = -1;
            public int includeHero = -1;
            public int includeMinions = -1;
            public int includeSelf = -1;
            /// <summary>原版 `CardEffect.buffType`（现读 `12` × 39 行 / `15` × 2 行，都在 `SW62 First Light`）。</summary>
            public int buffType = -1;
            /// <summary>`buffEffect.enchantment.m_PathID` 解出来的**附魔模板 id**
            /// （= `enchantment_templates.json` 的 `id`；41/41 命中，37 个不同模板）。
            /// <para>🔴 **这才是「环境生效期间」那段效果的本体** —— 模板本身已经在
            /// `enchantment_templates.json`（v1 · 85 张）里，缺的只是「谁来挂它」。</para></summary>
            public string enchantmentId;
            /// <summary>原版 PPtr 原值（排查用；`0` = 真值里没有这个键）。</summary>
            public long enchantmentPathId;
            /// <summary>`manaBuff.manaAmount` 等四格（只有 `chosenAbility 60` 那 3 行有）。</summary>
            public int manaAmount = -1;
            public int manaFriendlyPlayer = -1;
            public int manaEnemyPlayer = -1;
            public int manaCreateMana = -1;
        }

        /// <summary>这张表的 **`CardDef` 列表**（懒加载；⛔ **不是** <see cref="Load"/> 的可玩池，
        /// 别把它塞进 `DeckRules` / 收藏 / 轮抽的任何一条链 —— 原版判据说它们既不可组牌也不显示）。
        ///
        /// <para>配 <see cref="FindById"/> 用：`CardDatabase.FindById(CardDatabase.OffensiveEnvCards, "AM63")`。
        /// <paramref name="faction"/> + `idx` 那条路见 <see cref="OffensiveEnvIdAt"/>。</para>
        /// <para>⚠️ **读不到就出声一次并返回空表**（⛔ 不许静默）—— 调用方要区分「表不存在」与「表是空的」
        /// 请先看日志。</para></summary>
        public static List<CardDef> OffensiveEnvCards
        {
            get
            {
                if (_offensiveEnvCards != null) return _offensiveEnvCards;
                var asset = Resources.Load<TextAsset>(OffensiveEnvResourcePath);
                EnsureOffensiveEnvFrom(asset != null ? asset.text : null);
                return _offensiveEnvCards;
            }
        }

        /// <summary>把第三张表物化成 `CardDef` 列表（**幂等**；已在 <see cref="InvalidateOffensiveEnvCards"/> 之后重调即重建）。
        ///
        /// <para>每张卡造一个 `CardDef`：`type = "tactic"` · `cardType` / `spellType` / `rarity` /
        /// `inventoryOptions` / `showInCollectionOptions` / `cantBeRandomlyCreated` /
        /// `abilityTriggers` / `abilityTargets` 从表里灌。</para>
        /// <para>⚠️ **能力树本身不进 `CardDef`**（`CardDef` 没有这个格子）：它留在表里，按 id 从
        /// <see cref="OffensiveEnvAbilitiesOf"/> 取 —— 因为「按 `chosenAbility` 分派」那台机器今天**还不存在**
        /// （见 <see cref="OffensiveEnvAbilitiesOf"/> 的 doc）。</para>
        /// <para>⚠️ `json` 为 `null` / 空 = 表不存在 ⇒ **出声一次 + 落空表**（而且**不缓存**，与
        /// <see cref="Load"/> 读不到那一路同口径：补上资源后还能读到）。</para></summary>
        public static void EnsureOffensiveEnvFrom(string json)
        {
            if (_offensiveEnvCards != null) return;
            if (string.IsNullOrEmpty(json))
            {
                if (!_offensiveEnvMissingLogged)
                {
                    _offensiveEnvMissingLogged = true;
                    Debug.LogError($"[RuleEngine] 读不到第三张表 `Resources/{OffensiveEnvResourcePath}.json`"
                                 + " —— 进攻卡（环境效果卡）的 39 张卡**一张都取不到**。"
                                 + "重跑 `python -I -X utf8 工具/gen_offensive_env_cards.py --write`。");
                }
                return;      // ⚠️ 不缓存：补上资源后下一次还能读到
            }
            var file = JsonUtility.FromJson<OffensiveEnvFileDto>(json);
            if (file == null || file.cards == null)
            {
                Debug.LogError($"[RuleEngine] 第三张表解析失败（{json.Length} 字符）—— 落空表（⛔ 不静默）。");
                _offensiveEnvCards = new List<CardDef>();   // 解析坏了 ⇒ 缓存空表，别每次重试刷屏
                return;
            }
            var list = new List<CardDef>(file.cards.Length);
            var seen = new HashSet<string>();
            int bad = 0;
            foreach (var c in file.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.id))
                {
                    bad++;
                    Debug.LogError("[RuleEngine] 进攻卡表里有一行**没有 `id`** —— 该行丢弃。");
                    continue;
                }
                if (!seen.Add(c.id))
                {
                    bad++;
                    Debug.LogError($"[RuleEngine] 进攻卡表里 `id = {c.id}` **重复** —— 后一条丢弃。");
                    continue;
                }
                // 能力树 → 逐 ability 的 `targetsAffected`（`-1` 保持「这一格是缺键」，⛔ 不压成 0）
                var abilityTargets = new List<CardDef.AbilityTargetSpec>();
                if (c.abilities != null)
                {
                    foreach (var a in c.abilities)
                    {
                        if (a == null || a.logic == null) { abilityTargets.Add(null); continue; }
                        var ta = new int[a.logic.Length];
                        for (int i = 0; i < a.logic.Length; i++)
                            ta[i] = a.logic[i] != null ? a.logic[i].targetsAffected : -1;
                        abilityTargets.Add(new CardDef.AbilityTargetSpec { Trigger = a.trigger, TargetsAffected = ta });
                    }
                }
                // ⚠️ `rarity 5` ⇒ 我们的 `"special"`（与 `gen_cards_engine.py` 的 `TRUTH_RARITY` 同一口径）。
                //    其它值不可能（生成器 39/39 断言 5），出现就照原样留空并出声。
                string rarity = c.rarity == 5 ? "special" : "";
                if (c.rarity != 5)
                    Debug.LogError($"[RuleEngine] 进攻卡「{c.name}」（{c.id}）的 `cardRarity` = {c.rarity} ≠ 5"
                                 + " —— 稀有度留空（⛔ 不猜）。重跑 工具/gen_offensive_env_cards.py。");
                var def = new CardDef(c.id, string.IsNullOrEmpty(c.name) ? c.id : c.name,
                                      "tactic", c.desc ?? "", rarity, c.faction ?? "",
                                      // ⚠️ `keywords` 传**空数组**（⛔ 不是 `null`）：`CardDef` 的
                                      //    `CollectTriggerOps` 逐条往下传这个集合，`Parse()` 那条老路
                                      //    喂的一直是数组 ⇒ 空数组是**走过千万遍**的形状，`null` 没人验过。
                                      0, 0, 0, 0, Array.Empty<string>(),
                                      nameZh: c.nameZh, descZh: c.descZh,
                                      inventoryOptions: c.inventoryOptions,
                                      showInCollectionOptions: c.showInCollectionOptions,
                                      spellType: c.spellType,
                                      cantBeRandomlyCreated: c.cantBeRandomlyCreated != 0,
                                      abilityTriggers: c.abilityTriggers,
                                      abilityTriggersVersioned: c.abilityTriggers != null,
                                      // 🔴 `abilityTriggers` / `abilityTargets` **两列共用一个版本位**
                                      //    （`CardDef` 的 `AbilityTargetsVersioned` 的 doc 写着「不是原版语义，
                                      //    是我们数据管线的形状」）⇒ 这里**没有** `abilityTargetsVersioned` 这个形参，
                                      //    ⛔ 别自己发明一个。
                                      abilityTargets: abilityTargets.Count > 0 ? abilityTargets : null);
                def.AttachCardType(c.cardType);
                list.Add(def);
            }
            _offensiveEnvCards = list;
            if (file.count != list.Count)
                Debug.LogError($"[RuleEngine] 进攻卡表 `count` = {file.count}，实读 {list.Count} 张 —— 表坏了。");
            Debug.Log($"[RuleEngine] 进攻卡表 v{file.version}：**{list.Count}** 张 / "
                    + $"{CountFactions(list)} 个阵营（坏行 {bad}）—— 来自 `Resources/{OffensiveEnvResourcePath}.json`"
                    + "（⚠️ 只是**数据**：`BattleDriver.offensiveCardEnabled` 还关着 ⇒ 今天不产生任何行为）");
        }

        static int CountFactions(List<CardDef> list)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < list.Count; i++) if (list[i] != null) set.Add(list[i].Faction);
            return set.Count;
        }

        /// <summary>**按「阵营 ＋ 槽号」取 id** —— 这是进攻卡那条链**唯一无歧义的找法**
        /// （<paramref name="idx"/> 就是 `BattleContext.OffensiveSlotIdx` 存的那个 0..2；`-1` = 空卡 ⇒ 返回 `null`）。
        ///
        /// <para>🔴 **为什么不用卡名**：`Resources/OffensiveCards.json` 的卡名有 **6 处**与官方显示名对不上
        /// （`B93_Q7` 施工单 §顺手发现①）⇒ 今天 `BattleDriver.ChosenOffensiveCardDef` 那句
        /// `CardDatabase.Find(pool, name)` **恒落空**。改成「名字 → 本表的 `id` → `FindById`」才修得掉，
        /// 而最省事的是**直接按 `(faction, idx)`**（本表两列直接来自原版 SO 的 `cardRef.m_PathID` join，
        /// 39/39 一对一）。</para>
        /// <para>读不到表 ⇒ 返回 `null`（并已由 <see cref="OffensiveEnvCards"/> 出声一次）。</para></summary>
        public static string OffensiveEnvIdAt(string faction, int idx)
        {
            if (idx < 0 || string.IsNullOrEmpty(faction)) return null;
            var list = OffensiveEnvCards;
            if (list == null) return null;
            // ⚠️ `CardDef` 上没有槽号格子 ⇒ 回表里查（一次线性扫描，39 条，开销可忽略）。
            var raw = OffensiveEnvRows();
            if (raw == null) return null;
            for (int i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (c != null && c.idx == idx && string.Equals(c.faction, faction, StringComparison.Ordinal))
                    return c.id;
            }
            return null;
        }

        /// <summary>表里那些**原始行**（`OffensiveEnvCardDto`；按 id 建的索引，懒建）。
        /// <para>⚠️ 给 `CardDef` 装不下的那几列用（`slot` / `idx` / `envSO` / `forgeLevel` / `abilityTriggers` /
        /// 能力树）。**`null` = 表读不到**（已出声）。</para></summary>
        public static OffensiveEnvCardDto[] OffensiveEnvRows()
        {
            EnsureOffensiveEnvIndex();
            return _offensiveEnvRows;
        }

        static OffensiveEnvCardDto[] _offensiveEnvRows;
        static Dictionary<string, OffensiveEnvCardDto> _offensiveEnvById;

        static void EnsureOffensiveEnvIndex()
        {
            // 先触发一次懒加载（`CardDef` 那一路读不到时它已经出过声；这里只关心原始行）
            if (_offensiveEnvCards == null && OffensiveEnvCards == null) return;
            if (_offensiveEnvRows != null) return;
            var asset = Resources.Load<TextAsset>(OffensiveEnvResourcePath);
            if (asset == null) return;                                        // 已经在上面出过声
            var file = JsonUtility.FromJson<OffensiveEnvFileDto>(asset.text);
            if (file == null || file.cards == null) return;
            _offensiveEnvRows = file.cards;
            _offensiveEnvById = new Dictionary<string, OffensiveEnvCardDto>(StringComparer.Ordinal);
            foreach (var c in _offensiveEnvRows)
                if (c != null && !string.IsNullOrEmpty(c.id) && !_offensiveEnvById.ContainsKey(c.id))
                    _offensiveEnvById[c.id] = c;
        }

        /// <summary>这张进攻卡**按原版 `cardAbilities` 长出来的能力树**（逐叶摊平；`null` = 表里没有这张卡）。
        ///
        /// <para>🔴 **`CardDef` 里没有这个格子，也不该硬塞进去**：`trigger 0` 那条 ability 的
        /// `chosenAbility` 是 **30 / 40 / 60 / 0**，而我们的 `EffectText` 只解得动**卡面正文**那一套
        /// （「Deal 3 damage…」这种祈使句）；环境卡的正文是**触发句**（「When a player earns a Skull, …」）
        /// ⇒ `TriggerOps("this_card_played")` 恒定取不到 ⇒ `RuleCore.RunAbilityOfAnotherCard` 会
        /// 如实报「本版这条能力的正文解析不出来」。**这不是数据缺口，是分派口缺口**：
        /// 真正的效果体是 `enchantmentId` 指向的那张模板（`enchantment_templates.json`）。</para></summary>
        public static OffensiveEnvAbilityDto[] OffensiveEnvAbilitiesOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureOffensiveEnvIndex();
            if (_offensiveEnvById == null) return null;
            OffensiveEnvCardDto row;
            return _offensiveEnvById.TryGetValue(id, out row) && row != null ? row.abilities : null;
        }

        // ==========================================================================================
        //  🆕 2026-10-28（B93 波 · 执行代理 `E25`）：进攻卡那 41 行 `abilityLogic` **并进同一张**附魔反查表
        // ==========================================================================================
        //  【为什么必须并进同一张】`_enchantGrants`（v38 那张表）只从 `cards_engine.json` 的 `enchantment`
        //    列建；而 39 张进攻卡原版 `inventoryOptions = 5 (CantAddToDeck)` ∧
        //    `showInCollectionOptions = 0 (Hide)` ⇒ **不在组牌池** ⇒ 表里查不到 ⇒
        //    `Enchantments.GrantedEnchantmentRowsOf` 对它们恒 `null`。再建一张同义表就是
        //    「两处写同一条规则 = 迟早不一致」（铁律 6）⇒ 并进 `_enchantGrants`。
        //  【判据：哪几行进表】原版 `AbilityLogic__PlayAbility.c` 按 `abilityLogic.chosenAbility` 分派：
        //    · `30 buffOthers`（`.c:1182-1260` 那一支 —— 闸在 `:1177-1180`、`AddEffect` 在 `:1218`）与 `40 buffSelf`（`.c:1310`）会走到
        //      `BattleManager__AddEffect` → `CardScript__AddEffect.c` 的 `case 0xc / 0xf`（buffType 12 / 15）
        //      → `AddEnchantment` ⇒ **只有这两支产生「挂附魔」**（现读 34 + 3 = **37 行**）；
        //    · `0 drawCard`（1 行）与 `60 buffMana`（3 行）**不产生 `AddEffect`**
        //      （`.c:685` 的 `case 0x3c` 打完 `AddManaBuff` 就 `return`）⇒ **不进表**（否则会挂出原版不会有的附魔）。
        //    ⇒ 41 行 = 37 行入表 + 4 行**明确排除**（个数在日志里打出来，⛔ 不静默丢）。
        //  【载体】`targetsAffected`：120 / 130 / 20 —— 三档载体**全是督军**，
        //    判据与逐分支出处 → `Core/Enchantment.cs` 的 `TargetsAffectedFriendlyHero` 等三个常量。
        //    🔑 现读那 37 行的 `targetsAffected` = 120 × 33 / 130 × 1 / 20 × 3 ⇒ **没有 `-1`**
        //      （4 个 `-1` 正好就是被剔掉的那 4 行）⇒ 载体永远可判。
        // ==========================================================================================

        /// <summary>「进攻卡那 41 行已经并进 `_enchantGrants`」的标记（`Invalidate*` 里重置）。
        /// <para>⚠️ 它是**「尝试过一次」**语义（进方法第一句就置位），不是「成功了」——
        /// 这样 `Resources` 里缺这张表时不会每次出牌都重试一次 `Resources.Load`。</para></summary>
        static bool _offensiveEnvGrantsMerged;

        /// <summary>
        /// 把进攻卡表里那 41 行 `abilityLogic` 并进 <see cref="_enchantGrants"/>（**幂等**；
        /// 由 <see cref="EnchantmentGrantsOf"/> 顺手调一次 —— 那里是唯一的读口，⛔ 别在别处再造入口）。
        ///
        /// <para>行映射：`SourceCardId` = 卡 id · `TemplateCardId` = `enchantmentId` ·
        /// `BuffType` = `buffType` · `Trigger` = 该 ability 的 `trigger`（39/39 = `0`）·
        /// `TargetsAffected` = `targetsAffected` · `AbilityIndex` / `LogicIndex` 只作排查。</para>
        ///
        /// <para>⚠️ 与 `cards_engine.json` 那 58 行的关系：**同一张表、不同的键**（池卡 vs 不进池的
        /// 环境卡），现读**零重叠**（`spellType == 200` 的池卡 0 张）。真出现同 id 时**出声**并保留先到的那份。</para>
        /// </summary>
        public static void EnsureOffensiveEnvGrantsMerged()
        {
            if (_offensiveEnvGrantsMerged) return;
            _offensiveEnvGrantsMerged = true;        // 见字段 doc：「尝试过一次」语义
            var raw = OffensiveEnvRows();
            if (raw == null || raw.Length == 0) return;   // 表读不到（加载器已经出过声）⇒ 什么都不做
            if (_enchantGrants == null)
                _enchantGrants = new Dictionary<string, List<Enchantments.GrantedEnchantmentRow>>(StringComparer.Ordinal);
            int cards = 0, rows = 0, notAddEffect = 0, collide = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (c == null || string.IsNullOrEmpty(c.id) || c.abilities == null) continue;
                List<Enchantments.GrantedEnchantmentRow> list = null;
                for (int ai = 0; ai < c.abilities.Length; ai++)
                {
                    var a = c.abilities[ai];
                    if (a == null || a.logic == null) continue;
                    for (int li = 0; li < a.logic.Length; li++)
                    {
                        var g = a.logic[li];
                        if (g == null) continue;
                        // 🔴 只有 `chosenAbility 30 / 40` 两支会走到 `AddEffect`（判据见本段头注）
                        if (g.chosenAbility != 30 && g.chosenAbility != 40) { notAddEffect++; continue; }
                        if (string.IsNullOrEmpty(g.enchantmentId)) { notAddEffect++; continue; }
                        if (a.trigger != Enchantments.TriggerThisCardPlayed) { notAddEffect++; continue; }
                        if (list == null) list = new List<Enchantments.GrantedEnchantmentRow>(4);
                        list.Add(new Enchantments.GrantedEnchantmentRow
                        {
                            SourceCardId = c.id,
                            TemplateCardId = g.enchantmentId,
                            BuffType = g.buffType,
                            Trigger = a.trigger,
                            TargetsAffected = g.targetsAffected,
                            AbilityIndex = ai,
                            LogicIndex = li,
                        });
                        rows++;
                    }
                }
                if (list == null || list.Count == 0) continue;
                if (_enchantGrants.ContainsKey(c.id))
                {
                    collide++;
                    Debug.LogError($"[RuleEngine] 进攻卡「{c.name}」（{c.id}）在附魔反查表里**与池卡撞了同一个 id** —— "
                                 + "保留先到的那一份（⛔ 不静默覆盖）。这不该发生：`spellType == 200` 的池卡现读 0 张。");
                    continue;
                }
                _enchantGrants[c.id] = list;
                cards++;
            }
            Debug.Log($"[RuleEngine] 进攻卡并进附魔反查表：**{cards}** 张卡 / **{rows}** 行"
                    + $"（41 行里 {notAddEffect} 行**不入表**：`chosenAbility 0 drawCard` / `60 buffMana` 两支"
                    + "在原版 `AbilityLogic__PlayAbility.c` 里不产生 `AddEffect`；撞 id "
                    + $"{collide} 张）—— 与池卡那 58 行共用同一张表（`spellType == 200` 的池卡现读 0 张）");
        }
    }

    // ==================================================================================================
    //  🆕 v40（2026-10-29 · 第九十四会话 · 波 6 · 执行代理 `B94_B23`）：卡表 `abilityBuffs` 列
    //     = 原版 **`CardEffect`** 的逐行引擎侧载荷（判据 → `资料/完全复刻/判据/B94_R-W_战斗38条分诊.md` §二 **B-3**）
    //
    //  🔴 **缺的是什么**：我们今天的引擎**只从卡面正文解析**出效果（`give/gain/lose` → `PayloadOp` /
    //     `EffectResolver.ApplyOneGain` 一族），而原版 `CardScript.AddEffect`（`decomp_full/CardScript__AddEffect.c:209-967`）
    //     的输入是 **`CardEffect` 对象**，分派键 = `CardEffect.buffType(+0x38)`。**这一格此前一个字都没落进卡表**
    //     ⇒ 分派器的 11 个分组（`5 swapStats` · `6 changeRace` · `10 changeAbilityCost` · `11 removeExtrinsic` ·
    //     `25 ephemeralCopy` · `30 addRace` · `40 replaceAbility` · `45 addStatsDropPod` · `70 setMeleeAttack` ·
    //     `85 setRangeAttack` · `95 setCost`）**结构上产不出来**（账 `战斗逻辑驱动:686`）。`secretBuff/questBuff`
    //     那一对（账 `:1053`）的生产者也在同一支（`AddEffect.c:853-855` / `:887`）。
    //
    //  🔴 **为什么不把属性挂在 `CardDef` 上**（本仓其余列都是 `def.AttachXxx(...)`）：
    //     `RuleEngine/Core/**` **不在本代理白名单**（`CardDef` 会由别的代理按自己的落点改）。
    //     ⇒ 本列走**旁表 + 静态查询口**（`CardDatabase.AbilityBuffsOf`），`CardDef.cs` 一个字节不动。
    //     🔑 **将来若要把 `def.AbilityBuffs` 做成属性**：在 `Core/` 里加一句转发即可（`=> CardDatabase.AbilityBuffsOf(this)`），
    //        **⛔ 别把数据本身搬过去**（两处写同一条规则，迟早不一致 —— 铁律「判据只留一处」）。
    //
    //  ⚠️ **本列今天【零消费者】**：只落数据 + 只挂载 ⇒ **零行为变化、不改 `PinnedPoolMd5` / `PinnedPlayMd5`**。
    //     消费端（`EffectResolver` 那 11 个分组）在别的代理白名单里。
    //  ⚠️ **挂载那一句挤在 `:1749` 原有那一行里**（故意不加行）—— 那一行自己的行尾注释已写明
    //     「加行会把 `:1799` 等下游 A 行账落点整体推走」。⛔ 别为了排版把它拆成新行。
    //  ⚠️ **本块追加在【文件最末】**：`账/*.tsv` 按【行号】引用本文件（现读最大落点 `:3772`），
    //     在类中段插成员会让它们整体漂移（与上面 A4 / 进攻卡那两段同一条纪律）。
    // ==================================================================================================
    public static partial class CardDatabase
    {
        /// <summary>🆕 v40：`abilityBuffs` 的一行（= 原版 `CardEffect` 的引擎侧载荷）。
        /// <para>🔴 **字段名与 JSON 键名逐字一致**（`JsonUtility` 直读，没有映射表可写错）。
        /// 偏移 → `dump.cs:119082-119105`（`CardEffect`）；分派器 → `CardScript__AddEffect.c:209-967`。</para>
        /// <para>⚠️ **载荷格一律是「原值」**（不是差值、也不是 `-1` 哨兵）：SO 里每一格都有确定的值。
        /// `setMeleeAttack(70)` / `setCost(95)` / `setCurrentHealth(100)` / `setHealth(9)` 这几档
        /// **`0` 是合法且有语义的**（「设成 0」），所以生成器**不稀疏**、每行都带全 26 格。</para></summary>
        [Serializable]
        public class AbilityBuffRowDto
        {
            /// <summary>`cardAbilities` 下标；**`-1` = `activeAbility` · `-2` = `alternativeAbility`**
            /// （与 `abilityTargetCriteria` / `activeAbilityLogic` 同一套哨兵）。</summary>
            public int ab = -1;
            /// <summary>`abilityLogic` 下标。</summary>
            public int lg = -1;
            /// <summary>载荷在哪个容器：`"buffEffect"`（`CardEffect`，走 `AddEffect`）或
            /// `"handBuff"`（`handBuff.cardEffect`，**同一个类型**，走 `PlayerHand.AddHandEffect`）。
            /// 🔴 消费端**必须**分这一档：把 `handBuff` 当 `AddEffect` 结算 = 做多。</summary>
            public string at;
            /// <summary>原版 `CardEffect.buffType (+0x38)`（`BuffType`，枚举值抄在
            /// `工具/gen_cards_engine.py` 的 `BUFF_TYPE_NAMES`）。`0 (none)` 的行**不落**（`AddEffect.c:964-966` 什么都不做）。</summary>
            public int buffType;

            /// <summary>`CardEffect.trait (+0x3C)`（`DefinedTrait`；`addTrait`/`removeTrait`/`addTraitRemnant`/附魔族用）。</summary>
            public int trait;
            /// <summary>`CardEffect.traitValue (+0x40)`。</summary>
            public int traitValue;
            /// <summary>`CardEffect.newRace (+0x44)`（`Keyword`；`changeRace(6)` / `addRace(30)` 用；**池内现读全 0**）。</summary>
            public int newRace;

            /// <summary>`CardEffect.meleeAttackChange (+0x48)`（近战增减；`doubleAttack(20)` 那一支也会被原地改写成它）。</summary>
            public int meleeAttackChange;
            /// <summary>`CardEffect.meleeAttackValueOptions (+0x4C)`（`ChooseValueOptions`：「这个数从谁身上取」）。</summary>
            public int meleeAttackValueOptions;
            /// <summary>`CardEffect.rangeAttackChange (+0x50)`。</summary>
            public int rangeAttackChange;
            /// <summary>`CardEffect.rangeAttackValueOptions (+0x54)`。</summary>
            public int rangeAttackValueOptions;
            /// <summary>`CardEffect.armourChange (+0x58)`。</summary>
            public int armourChange;
            /// <summary>`CardEffect.armourValueOptions (+0x5C)`。</summary>
            public int armourValueOptions;
            /// <summary>`CardEffect.healthChange (+0x60)`。</summary>
            public int healthChange;
            /// <summary>`CardEffect.healthValueOptions (+0x64)`。</summary>
            public int healthValueOptions;
            /// <summary>`CardEffect.costChange (+0x68)`（`changeCost(2)` / `setCost(95)` / `changeAbilityCost(10)` 用）。</summary>
            public int costChange;
            /// <summary>`CardEffect.costValueOptions (+0x6C)`。</summary>
            public int costValueOptions;
            /// <summary>`CardEffect.randomValueChange (+0x70)`。</summary>
            public int randomValueChange;

            /// <summary>`CardEffect.permanentBuff (+0x74)`（「不能被 jam」）。**与 v37 `permanentBuffs` 列不同源用途**：
            /// 那列是「哪些行是永久」，本格是**这一行自己的**那个 bool（消费端按行判）。</summary>
            public int permanentBuff;
            /// <summary>`CardEffect.doNotStack (+0x75)`（「不叠加」）。</summary>
            public int doNotStack;
            /// <summary>`CardEffect.markOfChaosType (+0x78)`（`markOfChaos(8)` / `lastMarkOfChaos(17)` / 附魔族用）。</summary>
            public int markOfChaosType;
            /// <summary>`CardEffect.daemonHostType (+0x7C)`（`daemonHost(35)`；**池内现读全 0**）。</summary>
            public int daemonHostType;
            /// <summary>`CardEffect.mutationType (+0x80)`（`IsCumulativeMutation` 判 `== 0x14`；**池内现读全 0**）。</summary>
            public int mutationType;

            /// <summary>`CardEffect.extrinsic (+0x30)`（「效果绑在**施放者**而不是目标身上」）。</summary>
            public int extrinsic;
            /// <summary>`CardEffect.extrinsicVariable (+0x31)`。</summary>
            public int extrinsicVariable;
            /// <summary>`CardEffect.untilEndOfTurn (+0x32)`。</summary>
            public int untilEndOfTurn;
            /// <summary>`CardEffect.untilPlayerTurnStart (+0x33)`。</summary>
            public int untilPlayerTurnStart;
            /// <summary>`CardEffect.untilEnemyTurnStart (+0x34)`。</summary>
            public int untilEnemyTurnStart;
            /// <summary>`CardEffect.untilFollowingPlayerTurnStart (+0x35)`。</summary>
            public int untilFollowingPlayerTurnStart;
            /// <summary>`CardEffect.untilNextAttack (+0x36)`。</summary>
            public int untilNextAttack;
        }

        public partial class CardDto
        {
            /// <summary>🆕 v40：这张卡**逐 `abilityLogic` 行**的 `CardEffect` 载荷（形状同 `abilityDamage` / `abilitySummon`）。
            /// <para>`null` / 缺列 = **未知**（旧表 &lt; v40）；`[]` = 明确没有行。⛔ 不要 `?? new[0]`。</para></summary>
            public AbilityBuffRowDto[] abilityBuffs;
        }

        /// <summary>🆕 v40：`abilityBuffs` 的一行（映射后的形状；语义逐字同 <see cref="AbilityBuffRowDto"/>，
        /// 只是把「字符串 `at`」换成 <see cref="IsHandBuff"/>，省掉消费端的字符串比较）。</summary>
        public sealed class AbilityBuffSpec
        {
            /// <summary>`cardAbilities` 下标；`-1` = `activeAbility` · `-2` = `alternativeAbility`。</summary>
            public int Ab;
            /// <summary>`abilityLogic` 下标。</summary>
            public int Lg;
            /// <summary>真 = `handBuff.cardEffect`（走 `PlayerHand.AddHandEffect`，⛔ **不是** `AddEffect`）。</summary>
            public bool IsHandBuff;
            /// <summary>原版 `CardEffect.buffType (+0x38)`（`0 (none)` 的行不会出现在这里）。</summary>
            public int BuffType;

            public int Trait, TraitValue, NewRace;
            public int MeleeAttackChange, MeleeAttackValueOptions;
            public int RangeAttackChange, RangeAttackValueOptions;
            public int ArmourChange, ArmourValueOptions;
            public int HealthChange, HealthValueOptions;
            public int CostChange, CostValueOptions;
            public int RandomValueChange;
            public int PermanentBuff, DoNotStack;
            public int MarkOfChaosType, DaemonHostType, MutationType;
            public int Extrinsic, ExtrinsicVariable;
            public int UntilEndOfTurn, UntilPlayerTurnStart, UntilEnemyTurnStart;
            public int UntilFollowingPlayerTurnStart, UntilNextAttack;
        }

        /// <summary>🆕 v40：`id → 行[]`。**只在本类里写**（`RecordAbilityBuffs` 是唯一写口）。
        /// <para>🔴 用 **id** 而不是 `CardDef` 做键：`Parse` 可能被调多次（自检/探针），
        /// 每次都会造**新的** `CardDef` 实例 ⇒ 按实例建表会让「上一次拿到的 def」查不到自己的行。</para></summary>
        static readonly Dictionary<string, AbilityBuffSpec[]> _abilityBuffsById =
            new Dictionary<string, AbilityBuffSpec[]>(StringComparer.Ordinal);

        /// <summary>🆕 v40：某个卡 id 的 `CardEffect` 行。**`null` = 未知**（旧表 &lt; v40 / 这张卡不在表里），
        /// `空数组` = 明确没有行 —— 两者必须分得开（同 `CardDef.PermanentBuffs` 那套口径）。</summary>
        public static IReadOnlyList<AbilityBuffSpec> AbilityBuffsOf(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _abilityBuffsById == null) return null;
            AbilityBuffSpec[] rows;
            return _abilityBuffsById.TryGetValue(cardId, out rows) ? rows : null;
        }

        /// <summary>🆕 v40：按 `CardDef` 取（转发到 <see cref="AbilityBuffsOf(string)"/>）。
        /// 将来 `CardDef` 那边要加属性就转调这里（⛔ 别把数据搬过去）。</summary>
        public static IReadOnlyList<AbilityBuffSpec> AbilityBuffsOf(CardDef def)
            => def == null ? null : AbilityBuffsOf(def.Id);

        /// <summary>🆕 v40：`abilityBuffs` 列 → <see cref="AbilityBuffSpec"/>。
        /// <para>⚠️ 行级坏值口径与 `MapAbilitySummon` / `MapAbilityDamage` **逐字同族**：
        /// 缺 `ab`/`lg` ⇒ 出声 + 丢弃该行；同一 `(ab, lg, at)` 撞键 ⇒ 出声 + 丢弃后一条
        /// （⛔ 撞键 = 谁赢都说不清）；`at` 认不出 ⇒ 出声 + 丢弃。</para>
        /// <para>⚠️ `buffType == 0` 的行走**这一层**也丢（生成器已经不落它；这里再兜一次，
        /// 防有人手工改表 —— `AddEffect.c:964-966` 对它什么都不做）。</para></summary>
        static AbilityBuffSpec[] MapAbilityBuffs(CardDto c)
        {
            if (c == null || c.abilityBuffs == null) return null;   // 缺列 = 未知（⛔ 不是「没有行」）
            var list = new List<AbilityBuffSpec>(c.abilityBuffs.Length);
            var seen = new HashSet<long>();
            foreach (var r in c.abilityBuffs)
            {
                if (r == null) continue;
                if (r.ab < -2 || r.lg < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityBuffs` 有一行下标不合法"
                                 + $"（ab={r.ab}, lg={r.lg}）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                bool hand;
                if (r.at == "buffEffect") hand = false;
                else if (r.at == "handBuff") hand = true;
                else
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityBuffs` 有一行 `at` 认不出"
                                 + $"（`{r.at ?? "(null)"}`，只认 `buffEffect` / `handBuff`）—— **该行丢弃**。");
                    continue;
                }
                if (r.buffType == 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityBuffs` 有一行 `buffType == 0`"
                                 + $"（ab={r.ab}, lg={r.lg}, at={r.at}）—— 原版 `CardScript__AddEffect.c:964-966`"
                                 + " 对它什么都不做，**该行丢弃**。");
                    continue;
                }
                // 撞键键 = (ab, lg, at)：同一个 (ab,lg) 的 buffEffect 与 handBuff 是**两条独立效果**。
                long k = ((long)(r.ab + 2) << 40) ^ ((long)r.lg << 8) ^ (uint)(hand ? 1 : 0);
                if (!seen.Add(k))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityBuffs` 有**重复的 "
                                 + $"(ab={r.ab}, lg={r.lg}, at={r.at})** —— 后一条已丢弃（⛔ 撞键 = 谁赢都说不清）。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                list.Add(new AbilityBuffSpec
                {
                    Ab = r.ab, Lg = r.lg, IsHandBuff = hand, BuffType = r.buffType,
                    Trait = r.trait, TraitValue = r.traitValue, NewRace = r.newRace,
                    MeleeAttackChange = r.meleeAttackChange,
                    MeleeAttackValueOptions = r.meleeAttackValueOptions,
                    RangeAttackChange = r.rangeAttackChange,
                    RangeAttackValueOptions = r.rangeAttackValueOptions,
                    ArmourChange = r.armourChange, ArmourValueOptions = r.armourValueOptions,
                    HealthChange = r.healthChange, HealthValueOptions = r.healthValueOptions,
                    CostChange = r.costChange, CostValueOptions = r.costValueOptions,
                    RandomValueChange = r.randomValueChange,
                    PermanentBuff = r.permanentBuff, DoNotStack = r.doNotStack,
                    MarkOfChaosType = r.markOfChaosType, DaemonHostType = r.daemonHostType,
                    MutationType = r.mutationType,
                    Extrinsic = r.extrinsic, ExtrinsicVariable = r.extrinsicVariable,
                    UntilEndOfTurn = r.untilEndOfTurn, UntilPlayerTurnStart = r.untilPlayerTurnStart,
                    UntilEnemyTurnStart = r.untilEnemyTurnStart,
                    UntilFollowingPlayerTurnStart = r.untilFollowingPlayerTurnStart,
                    UntilNextAttack = r.untilNextAttack,
                });
            }
            return list.ToArray();
        }

        /// <summary>🆕 v40：**唯一写口** —— 由 `Parse` 在每张卡构造完（`:1749` 那一句）调用一次。
        /// <para>`null`（旧表）⇒ 不写进表 ⇒ `AbilityBuffsOf` 返回 `null`（= 未知）。
        /// ⚠️ 同 id 重复调用 = `Parse` 被跑第二次 ⇒ 覆盖（同 id 内容相同，⛔ 不累加）。</para></summary>
        static void RecordAbilityBuffs(CardDef def, CardDto c)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            var rows = MapAbilityBuffs(c);
            if (rows == null) return;                      // 缺列 = 未知 ⇒ 表里没有这个 id
            if (_abilityBuffsById.ContainsKey(def.Id)) _abilityBuffsById[def.Id] = rows;
            else _abilityBuffsById.Add(def.Id, rows);
        }
    }

    // ==================================================================================================
    //  🆕 v41 候选（2026-10-29 · 执行代理 `B95_WCD`）：卡表 `abilityMutations` 列
    //     = 原版 `CardEffect.mutationType`（`+0x80`）的**逐 `(cardAbility, abilityLogic)` 行**落表。
    //
    //  判据（第一权威 = `d:/2/tools/decomp_full/`，2026-10-29 逐句亲读）：
    //    `RawCardScript__IsCumulativeMutation.c:17-47` —— 扫 `cardAbilities(+0x290)` →
    //    每个 `CardAbility.abilityLogic(+0x50)` → 该行的 `buffEffect(+0x50)` → `mutationType(+0x80) == 0x14(20)`
    //    ⇒ **任一命中即真**。⚠️ `buffEffect` 是**单个对象**（不是列表）⇒ 键 = `(ab, lg)`，一行一格。
    //    唯一读点 = `CardScript__HasSingleUseMutation`（`IsCumulativeMutation` 真 ⇒ 直接返 0）
    //    ← `AbilityLogic.GetTargets` 在 `criteria.traitsFilter == 780 (mutation)` 时用它做去重。
    //
    //  🔴 **为什么【不能】复用 v40 的 `abilityBuffs` 列**：那一列**只落 `buffType != 0` 的行**
    //     （判据 `AddEffect.c:964-966`：`buffType == 0` 什么都不做）⇒ 一张「`buffType == 0` 但
    //     `mutationType == 20`」的卡会被整行漏掉，而原版谓词**根本不看 `buffType`**。
    //     ⇒ 两列用途不同，⛔ 别拿一列顶另一列（顶了就是「静默少数行」）。
    //
    //  ⚠️ **生成器那一半还没做**（`工具/gen_cards_engine.py` 需出 v41 的 `abilityMutations`；
    //     行形状 = `[{ab, lg, mut}]`，`mut` 照原值落 `0/10/20`，**不稀疏**）⇒ 今天卡表里没有这个键
    //     ⇒ `MapAbilityMutations` 返 `null` = **整列未知** ⇒ `CardDef.IsCumulativeMutation()` 恒 `false`
    //     = 加这一列之前**逐字相同**的行为（零行为变化、不改 `PinnedPoolMd5`）。
    //     ⛔ 别为了让谓词「有值」拿 `abilityBuffs` 拼一份（上面那行理由）。
    //
    //  ⚠️ 挂载那一句挤在 `:1749` 原有那一行里（同 v37 / v40 的理由：加行会把下游 A 行账落点推走）。
    //  ⚠️ 本块追加在【文件最末】：账按【行号】引用本文件，中段插成员会让它们整体漂移。
    // ==================================================================================================
    public static partial class CardDatabase
    {
        /// <summary>🆕 v41：`abilityMutations` 的一行（= 原版 `CardEffect.mutationType`）。
        /// <para>🔴 字段名与 JSON 键名逐字一致（`JsonUtility` 直读）。
        /// `ab` / `lg` 的哨兵同 <see cref="AbilityBuffRowDto"/>：`ab = -1` = `activeAbility` ·
        /// `-2` = `alternativeAbility`；`lg` = 该能力对象里 `abilityLogic` / `activeAbilityLogic` 的下标。</para>
        /// <para>🔴 `mut` 三态：`0 none` / `10 Normal` / `20 Cumulative` 是**真值**，`-1` = 坏值/缺键
        /// （⛔ 别把缺键当 `0` —— 原版只认 `== 20`，`0` 与 `10` 都返 false，但「未知」要能被上层查到）。</para></summary>
        [Serializable]
        public class AbilityMutationRowDto
        {
            public int ab = -1;
            public int lg = -1;
            public int mut = -1;
        }

        public partial class CardDto
        {
            /// <summary>🆕 v41：逐 `abilityLogic` 行的 `buffEffect.mutationType`（形状同 `abilityBuffs`，
            /// 但**不带 `at`** —— 原版 `IsCumulativeMutation` 只扫 `buffEffect`，不扫 `handBuff`，
            /// 判据 `.c:26-45` 的三层 `+0x50`）。
            /// <para>`null` / 缺列 = **未知**（旧表 &lt; v41）；`[]` = 明确没有行。⛔ 不要 `?? new[0]`。</para></summary>
            public AbilityMutationRowDto[] abilityMutations;
        }

        /// <summary>🆕 v41：`abilityMutations` 列 → <see cref="CardDef.AbilityMutationSpec"/>。
        /// <para>⚠️ 行级坏值口径与 `MapAbilityBuffs` 同族：缺 `ab`/`lg` ⇒ 出声 + 丢弃该行；
        /// 同一 `(ab, lg)` 撞键 ⇒ 出声 + 丢后一条（原版 `.c` 是一行一对象，撞键 = 表坏了）；
        /// `mut &lt; 0` ⇒ 出声 + 该行带 `-1`（**保留**，因为 `MutationTypeAt` 的三态要看得见它）。</para></summary>
        static CardDef.AbilityMutationSpec[] MapAbilityMutations(CardDto c)
        {
            if (c == null || c.abilityMutations == null) return null;   // 缺列 = 未知（⛔ 不是「没有行」）
            var list = new List<CardDef.AbilityMutationSpec>(c.abilityMutations.Length);
            var seen = new HashSet<long>();
            foreach (var r in c.abilityMutations)
            {
                if (r == null) continue;
                if (r.ab < -2 || r.lg < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityMutations` 有一行下标不合法"
                                 + $"（ab={r.ab}, lg={r.lg}）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                long k = ((long)(r.ab + 2) << 40) ^ (long)r.lg;
                if (!seen.Add(k))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityMutations` 有**重复的 "
                                 + $"(ab={r.ab}, lg={r.lg})** —— 后一条已丢弃（原版 `buffEffect` 是单对象，撞键 = 表坏了）。"
                                 + "重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                if (r.mut < 0)
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `abilityMutations` 有一行缺 `mut`"
                                 + $"（ab={r.ab}, lg={r.lg}）—— 该格记 `-1`（未知），⛔ 别当 `0 none`。");
                list.Add(new CardDef.AbilityMutationSpec { Ab = r.ab, Lg = r.lg, MutationType = r.mut });
            }
            return list.ToArray();
        }

        // ==============================================================================================
        //  🆕 账 `战斗逻辑驱动:1300`（`SupportMethods.GetCardListFromIds`）的**原形状**实现
        //     （2026-10-29 · 执行代理 `B95_WCD`）
        //
        //  判据（第一权威 = `decomp_full/SupportMethods__GetCardListFromIds.c:1-47`，逐句亲读）：
        //    ① `for i in 0..ids.Count`；
        //    ② `cardCollection.Find(x => x.uniqueId == id)`；
        //    ③ **`list.Add(找到的那个)` —— 找不到也 Add(null)** ⇒ 结果表与输入**等长、下标对齐、带 null 洞**。
        //
        //  🔴 **为什么要有它**（与既有 <see cref="DeckLookup"/> 不是一件事）：
        //    `DeckLookup` 是「单值解析 + 调用方自己跳过 null」（我们的本地装配口）；
        //    原版这条是「**保洞**」—— 因为调用方按**下标**继续用（`DeckAndWarlordData__SetupSerializedData.c:325`，
        //    入参 = 在线对手的 `SerializedPlayerDeckData.cardLibrary`）。**丢掉 null 会让后面的下标整体错位**。
        //    ⇒ 接 P3 / 联机对手卡组时**必须**走这个口（账 `:1300` 的备注已写死这条）。
        //
        //  ⚠️ **今天 0 调用者**（唯一调用点在 P3 序列化卡组那条链上）⇒ 零行为变化。
        //  ⚠️ 匹配键 = 我们的 `CardDef.Id`（= 原版 `RawCardScript.uniqueId`）；**不回落卡名**
        //     （原版 `Find` 只比 uniqueId —— 回落卡名会在这条「保洞」语义上造出原版没有的命中）。
        // ==============================================================================================
        /// <summary>≡ 原版 `SupportMethods.GetCardListFromIds(string[] enemyDeckIDs, List&lt;RawCardScript&gt; cardCollection)`。
        /// <para>逐个 id 在 <paramref name="pool"/> 里按 <see cref="CardDef.Id"/> 找；**找不到填 <c>null</c>**
        /// ⇒ 返回数组长度恒等于 <paramref name="ids"/> 的长度（原版 `list.Add(Find(...))`，判据 `.c:38-40`）。</para>
        /// <para>`ids == null` ⇒ 返回空数组（原版会在 `ids.Count` 上 NRE；我们出声 + 空表，同
        /// <see cref="HasHidden"/> 那套「原版抛、我们出声」的口径）。</para>
        /// <para>⚠️ 查不到的条目**不逐条刷错误日志** —— 原版这里是**静默填 null**（洞是它的正常形状）；
        /// 要不要把洞当错由调用方判（⛔ 别在这里替它决定）。</para></summary>
        public static CardDef[] CardListFromIds(string[] ids, IEnumerable<CardDef> pool)
        {
            if (ids == null)
            {
                Debug.LogError("[RuleEngine] CardListFromIds 收到 null ids —— 原版会 NRE；返回空表。");
                return new CardDef[0];
            }
            var byId = IdIndex(pool ?? new CardDef[0]);
            var outList = new CardDef[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                CardDef c;
                outList[i] = !string.IsNullOrEmpty(ids[i]) && byId.TryGetValue(ids[i], out c) ? c : null;
            }
            return outList;
        }
    }

    // ==================================================================================================
    //  🆕 v42（2026-10-29 · 执行代理 `B96_GEN` · 账 `战斗逻辑驱动:1288`）：卡表 `triggerAbilityIndices` 列
    //     = 原版 `RawCardScript.GetAbilitiesForTriggerRawcard(trigger)` 的**数据半**
    //       （上游 U1「`trigger → ability 下标` 表」，判据 `资料/完全复刻/判据/B92_战斗判据_B6.md` §3·2 / §3·11）。
    //
    //  🔴 **为什么必须新开一列、⛔ 不能拿 v-old 的 `abilityTriggers` 顶**：那一列是
    //     `sorted(set(trigger))`（**只有数值、没有 ability 下标**）⇒ 同一 trigger 下挂多条 ability 时分不开
    //     「该跑哪一条 / 哪几条」，而原版 `GetAbilitiesForTriggerRawcard` 返回的是**列表**。
    //     现读池内 **65 组**「同一 trigger 多 ability」⇒ 这一列对它们**有实际信息量**。
    //
    //  ⚠️ **本列今天零消费者**（消费端 = `CardDef.CollectTriggerOps` 的「按 trigger 收 op」那半，由 `B96_CR` 做）
    //     ⇒ **零行为变化、不改 `PinnedPoolMd5` / `PinnedPlayMd5`**。
    //  ⚠️ **挂载那一句挤在 `:1749` 原有那一行里**（同 v37 / v40 / v41 的理由：加行会把下游 A 行账落点整体推走）。
    //  ⚠️ **本块追加在【文件最末】**：账按【行号】引用本文件，中段插成员会让它们整体漂移。
    // ==================================================================================================
    public static partial class CardDatabase
    {
        /// <summary>🆕 v42：`triggerAbilityIndices` 的一行 = 「一条 `abilityLogic` 的 trigger」。
        /// <para>🔴 字段名与 JSON 键名逐字一致（`JsonUtility` 直读）。只扫 `cardAbilities(+0x290)`
        /// —— `activeAbility(+0x260)` / `alternativeAbility` **不在**原版这一格。</para>
        /// <para>⚠️ `lg = -1` = 这条 ability **没有任何 `abilityLogic` 行**（原版 `GetAbilitiesForTriggerRawcard`
        /// 只按 trigger 过滤、不看 logic 条数 ⇒ 这种 ability 也要有它那一行）。</para></summary>
        [Serializable]
        public class TriggerAbilityIndexDto
        {
            /// <summary>`cardAbilities` 下标（`-1` = 坏值/缺键）。</summary>
            public int ab = -1;
            /// <summary>`abilityLogic` 下标；`-1` = 这条 ability 没有 logic 行。</summary>
            public int lg = -1;
            /// <summary>原版 `AbilityTrigger` 数值（`-1` = 坏值/缺键，⛔ 不是 `0 ThisCardPlayed`）。</summary>
            public int trigger = -1;
        }

        public partial class CardDto
        {
            /// <summary>🆕 v42：`[{"ab":…, "lg":…, "trigger":…}, …]`（按 `(ab, lg)` 升序）。
            /// <para>`null` / 缺列 = **未知**（旧表 &lt; v42）；`[]` = 明确没有。⛔ 不要 `?? new[0]`。</para></summary>
            public TriggerAbilityIndexDto[] triggerAbilityIndices;
        }

        /// <summary>🆕 v42：`triggerAbilityIndices` 列 → <see cref="CardDef.TriggerAbilityIndexSpec"/>。
        /// <para>⚠️ 行级坏值口径与 <see cref="MapAbilityMutations"/> 同族：`ab &lt; 0` / `lg &lt; -1` /
        /// `trigger &lt; 0` ⇒ 出声 + 丢弃该行；同一 `(ab, lg)` 撞键 ⇒ 出声 + **丢弃后一条**
        /// （原版是一条 logic 一个 `CardEffect`，撞键 = 表坏了）。</para></summary>
        static CardDef.TriggerAbilityIndexSpec[] MapTriggerAbilityIndices(CardDto c)
        {
            if (c == null || c.triggerAbilityIndices == null) return null;   // 缺列 = 未知（⛔ 不是「没有行」）
            var list = new List<CardDef.TriggerAbilityIndexSpec>(c.triggerAbilityIndices.Length);
            var seen = new HashSet<long>();
            foreach (var r in c.triggerAbilityIndices)
            {
                if (r == null) continue;
                if (r.ab < 0 || r.lg < -1 || r.trigger < 0)
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `triggerAbilityIndices` 有一行下标不合法"
                                 + $"（ab={r.ab}, lg={r.lg}, trigger={r.trigger}）—— **该行丢弃**。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                long k = ((long)r.ab << 40) ^ (long)(r.lg + 1);
                if (!seen.Add(k))
                {
                    Debug.LogError($"[RuleEngine] 卡「{c.name}」（{c.id}）的 `triggerAbilityIndices` 有**重复的 "
                                 + $"(ab={r.ab}, lg={r.lg})** —— 后一条已丢弃（原版一条 logic 一个 `CardEffect`，"
                                 + "撞键 = 表坏了）。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                list.Add(new CardDef.TriggerAbilityIndexSpec { Ab = r.ab, Lg = r.lg, Trigger = r.trigger });
            }
            return list.ToArray();
        }
    }
}
