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
        }

        [Serializable]
        public class CardDto
        {
            /// <summary>**稳定 id**（`AM12` 这样的原版 id，或 `AM_Some_Card` 这样的自造 id）。
            /// 见 `gen_cards_engine.py` 里 `IDS_SRC` 那段 —— 原版 id 取不到时自造，两种形态一眼可分。</summary>
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
        }

        /// <summary>从 Resources 读卡表。取不到返回空表（不抛异常 —— 让调用方能自己决定怎么报）。</summary>
        public static List<CardDef> Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[RuleEngine] 找不到卡表 Resources/{ResourcePath}.json —— "
                             + "跑一下：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_cards_engine.py");
                return new List<CardDef>();
            }
            return Parse(asset.text);
        }

        public static List<CardDef> Parse(string json)
        {
            var list = new List<CardDef>();
            if (string.IsNullOrEmpty(json)) return list;

            var dto = JsonUtility.FromJson<FileDto>(json);
            if (dto == null || dto.cards == null) return list;

            // ⚠️ 卡表 < v6 就没有 `id` 字段（v6 = 2026-09-13 第三十三轮开始，每张卡带稳定 id）。
            //    读旧表**不报错**，但 `Id` 会退回卡名 —— 那是老毛病，得说出来，别让人以为身份是稳的。
            if (dto.version < 6)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 6 —— 没有稳定 id，"
                               + "`CardDef.Id` 退回**卡名**（同名卡会串）。跑一下 gen_cards_engine.py 重生成。");
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
                if (string.IsNullOrEmpty(cid)) { cid = c.name; noId++; }
                list.Add(new CardDef(
                    id: cid,                    // 稳定 id（v6 起）；缺了才退回卡名，并在下面报数
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
                    armour: c.armour));
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
                Debug.LogWarning($"[RuleEngine] 卡表里有 {noId} 张卡**没有 id**，已退回卡名当身份 —— "
                               + "同名卡（跨阵营那 3 组）会串在一起。重跑 gen_cards_engine.py。");
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
