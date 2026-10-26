// DeckRules.cs — 卡组构筑规则 + 卡组数据模型（**纯逻辑，不依赖 UnityEngine**）
//
// 数字的来源：**原版客户端判据优先**（反编译 / 解包资源 / 卡面），客户端取不到的才用
//   下面这份**粉丝实体版规则书**当旁证：
//   `d:/4/Unity/资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`
//   「游戏模式」那张表（:43-66）。行号写在每个常量后面。
//
// 🔴 **2026-10-17 更正（铁律 5）**：本节原来写「数字**全部出自原版离线规则书**」——**两处都错**：
//   ① 那份规则书**不是官方文档**（它自己第 5 行写着 `Not official. Fan project.`，
//      第 9 行写着这是「把线上游戏改成实体卡牌」的规则书、很多机制落到实体要 creative interpretation）
//      ⇒ **只能当第二来源 / 旁证**，不能当权威；
//   ② 「全部」也不成立 —— 督军 = 1 那条是**原版代码硬编码**的（见下面 `GetMaxCopiesInDeck` 的 `return 1`）。
//   错因：2026-09-25 之前全仓把「官方规则书」当成一条权威来源，后来查实它是粉丝项目
//   （判据全文 → `CLAUDE.md` 铁律 2 那张表 + `资料/全量反编译复核_靠推断的清单.md` §2.2）。
//   下面这些数**照它取**，理由不是「它权威」，而是**客户端那两处取不到运行时值**（见下一段）。
//
// ⚠️ **为什么只能照规则书**：客户端代码里也有这些规则，但都拿不到数 ——
//   · `GameplayVariablesData.GetMaxCopiesInDeck(rarity, type)` 是 LiveOps 服务器下发的
//     （`decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-13`：
//      `cardType==10`(督军/Hero) **直接 `return 1`**；`rarity==4`(传奇) → `numberOfCopiesLegendary`；
//      其余 → `numberOfCopiesOtherRarities`）
//   · `GameStaticData.maxCardsCopiesInDeck` / `maxLegendaryCopiesInDeck` 是 `static readonly`，
//     值来自配置资源。
//   ✅ **2026-09-22 更正**：这里原写「**DLL 里没有字面量**」—— **不准确**。
//     字面量**有**：`decomp_full/GameStaticData__.cctor.c:225-232` 往 `+0x17C` 写 **2**、
//     `+0x180` 写 **1**（字段名见 `il2cpp_out/dump.cs:119517-119518`；同块 `0x198=86400000`(一天毫秒)、
//     `0x194=3600` 可交叉确认就是这块静态字段）。
//     ⚠️ **但它们疑似死值**：全量反编译里**找不到读 `0x17C`/`0x180` 的地方**，
//     真正生效的是 LiveOps 下发的 `numberOfCopiesOtherRarities` / `numberOfCopiesLegendary`。
//     ⇒ **结论不变**（照规则书 :43-66 的 2 / 1；它和客户端静态默认值**一致**，可互相印证），
//       变的只是「为什么」：不是「没有」而是「有字面量、但取不到运行时值」。
//   · 督军 = 1 那条**是原版代码硬编码**（上面 `GetMaxCopiesInDeck` 那句 `return 1`）⇒ 可以放心照抄。
//   🔴 **2026-10-17 更正（铁律 5）**：本行原写「规则书是本地唯一**完整**的权威来源，
//      和它冲突的一切以它为准。」—— **口径错了**：它是**粉丝实体版**（非官方），
//      与它冲突时**以原版客户端判据为准**（反编译方法体 / 解包资源 / 成品卡面），**它只作旁证**。
//   🔴 2026-09-22 普查：「持有数 / 重复卡 / 升级」这套的完整规格与**查不到的那些数** →
//      `资料/卡牌重复与升级_原版规格.md`
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>卡组构筑/校验的失败原因。`None` = 合法。</summary>
    public enum DeckError
    {
        None = 0,
        UnknownCard,        // 卡组里有个 id 不在卡池里
        NoWarlord,
        WarlordNotHero,     // 督军位放了非督军
        DefensiveMissing,   // 缺防御卡 —— 🔴 **2026-09-26 起 `Validate` 不再返回它**（防御卡改为可选，照原版）；
                            //    枚举值留着是因为 `Describe` 的文案表与历史断言引用过它（删值会动到下标的含义）
        DefensiveNotDefence,
        WrongFaction,       // 卡与督军不同阵营（规则书:53「所有卡必须与督军同阵营」）
        TooManyCards,       // 超过 30（或遭遇模式 12）
        TooFewCards,        // 不足
        CopyLimitExceeded,  // 某张卡超过同名上限
        WarlordInCards,     // 督军/防御卡又混进了普通卡位
        WarlordAlreadySet,  // 已经有督军了，再选就是换 —— 换要走 SetWarlord（会清掉不合阵营的卡）
        DefensiveAlreadySet,
        // 🔴 **2026-10-14（`A1798`）改口径：从「我们自己按 `Subtype` 猜的三类」改成【原版卡组准入】。**
        //    判据只此一处 = `CannotAddToDeck`（原版 `DeckUtility__ValidateDeckCards.c:68-77` +
        //    `TryRemoveInvalidCards.c:39-43`）。**枚举名 `EffectOnlyCard` 故意留旧**：
        //    它就是 I2 词条键 `MenuDeck/Error/EffectOnlyCard` 的那一段（`DeckRules.Describe` 拼的），
        //    改名会**连带改掉词条键** ⇒ 与 `Core/Loc.cs` 那张表对不上（那条键在表里是 `Loc.HasEntry` 抽查过的）。
        //    ⚠️ **但「效果生成的卡」这个说法现在【不准确了】** —— 新口径管的是**原版 `inventoryOptions`**
        //    （171→ 现读 98 张，多半是「督军天赋/法术」这类不在玩家库存里的计策卡）。
        //    两列文案（`Core/Loc.cs` 那条中文「这张是效果生成的卡（药剂/破坏/秘仪），不能放进卡组」）
        //    **还没跟着改** —— 那是**白名单之外**的文件，已如实记进
        //    `资料/普查产出_第二十八会话/W9_卡组分流按原版.md` 的「没做」一节，⛔ 别当成已收口。
        EffectOnlyCard,
        // 🆕 **2026-10-25（完全复刻 · 执行代理 A · 第 4 轮）：补上原版的 catch-all 那一档。**
        //   判据 = 官方本地化 `MenuDeck/Error/InvalidDeck`（15 条具名族里的一条，EN/ZH 齐全），
        //   且它是**唯一带 `/Short` 的两条之一**（`MenuDeck/Error/InvalidDeck/Short`）。
        //   原版 `DeckUtility.ToRawLocalizationString` 对**认不出的错码**就落这一档 ⇒ 语义 = 「这副卡组无效，请编辑或换一副」。
        //   ⚠️ **追加在枚举末尾**（⛔ 别插在中间：历史断言/存档按**名字**引用，但枚举序号一旦位移，
        //      `DeckRuntime` 那一族按号取值的地方会静默错位）。
        InvalidDeck,
        // 🆕 **2026-10-25（`E1` · 第五段 · R3 §③·2 / §六·4）：补上「**传奇总数**」这一档。**
        //   改前 `CanAddCard` 的「整副传奇张数」闸**借 `CopyLimitExceeded` 当载体** ——
        //   它与「同名上限」撞同一个码，任何「只按返回值出文案」的地方都会把两句话**说反**
        //   （原版两条文案是分开的：`MenuDeck/Error/MaxCopiesReached` vs `MaxLegendayCopiesReached`）。
        //   判据 = 原版 `DeckUtility.CanAddCard.c:100-147`（这一闸只出一句话、没有错误枚举 ⇒
        //   我们这一档是**为了把两句话分开**才造的枚举值，键名取原版实有的那一条）。
        //   ⚠️ **追加在枚举末尾**（⛔ 别插中间：枚举序号一旦位移，按号取值的地方会静默错位）。
        LegendaryTotalExceeded,
    }

    public static class DeckRules
    {
        // 🔴 **2026-09-26：这张表的数值全部搬到 `GameplayVariables` 了**（那是唯一出处）。
        //    这里保留原名当**别名**，是为了不动既有调用方（卡组编辑器那一片）；
        //    **新代码请直接用 `GameplayVariables.Classic/Skirmish`**，别再往这儿加常量。
        //    来历：原版这两套模式的值住在 `GameplayVariablesData`（服务器下发）⇒ 我们收成一处对齐它。
        public const int ClassicCards = GameplayVariables.ClassicDeckSize;          // 规则书:47
        public const int ClassicHandStart = GameplayVariables.ClassicStartingHand;  // :48「起手 3 张」
        public const int ClassicHandLimit = GameplayVariables.ClassicHandLimit;     // :48「上限 10」
        public const int DrawPerTurn = GameplayVariables.ClassicDrawPerTurn;        // :48「每回合抽 1 张」
        // ── 加时 Overtime（规则书:51 + 用户 2026-09-17 给的判据）─────────────────
        // 🔴 2026-09-20 改口径：原来叫 `OvertimeDrawPerTurn = 2`（照规则书「双方每回合抽 2 张」抄），
        //    但**原版代码只做「多抽一张」**（`BattleManager._NextTurn`）—— 那个 2 = 常规 1 + 加时 1，
        //    是**结果**不是**独立常数**。写成 `OvertimeExtraDraw = 1` 才不会和 `DrawPerTurn` 打架。
        //    见 `资料/加时与冲突模式_原版规格.md` §1.2 / §1.7。
        // 🔴 **2026-09-26 删掉了 `OvertimeEnergy`**：它原来只是
        //    `GameplayVariables.ClassicOvertimeTurn` 的**同值别名**，而运行路径在 2026-09-26
        //    改成读 `ctx.Vars.overtimeTurn`（`RuleCore.BeginTurn`）之后，它**只剩测试在读**
        //    ⇒ 那几条断言等于**自证**（盯一个没人用的常量，`CLAUDE.md` §二 那条的老毛病）。
        //    ⚠️ 本工程的老规矩：**两处写同一条规则 = 迟早不一致**。
        //    **要那个数请走单源头**：值 = `GameplayVariables.ClassicOvertimeTurn`；
        //    运行时的真判据 = `ctx.Vars.overtimeTurn`（可空，`null` ⇒ 该模式永不进加时）。
        public const int OvertimeExtraDraw = 1;     // 原版只做「多抽一张」；常规那 1 张走 `DrawPerTurn`（两模式相同）

        // ── 遭遇模式 Skirmish（规则书:57-64）—— 值同样搬到 `GameplayVariables` 了 ──────────
        public const int SkirmishCards = GameplayVariables.SkirmishDeckSize;                      // :57
        public const int SkirmishHandStart = GameplayVariables.SkirmishStartingHand;              // :58
        public const int SkirmishHandLimit = GameplayVariables.SkirmishHandLimit;                 // :58
        /// <summary>⚠️ 老名字是「**扣几点**」（正数 10）；`GameplayVariables` 里存的是**增量**（`−10`）。
        /// 保留这个正数写法只为不动既有断言，新代码用 `SkirmishWarlordLifeChange`。</summary>
        public const int SkirmishWarlordHealthPenalty = -GameplayVariables.SkirmishWarlordLifeChange;  // :62
        public const int SkirmishEnergyPerTurn = GameplayVariables.SkirmishManaPerTurn;           // :60
        public const int SkirmishLegendaryLimit = GameplayVariables.SkirmishMaxLegendaries;      // :64

        /// <summary>骷髅头：把敌方督军生命削到这些值时各得 1 个（规则书:36）
        /// —— 对局结算界面那三个骷髅就是这么来的（运行时 dump 里 `skull1..3`）</summary>
        public static readonly int[] SkullThresholds = { 20, 10, 0 };

        /// <summary>
        /// 同名卡在卡组里的上限（规则书:53）：
        /// **普通 / 稀有 / 史诗 最多 2 张；传说 最多 1 张**；🔴 **督军（不分稀有度）恒定 1 张**。
        ///
        /// 遭遇模式另有一条「传说卡最多 4 张（不含督军）」—— 那条是**整副牌里传说卡的总数**，
        /// 不是单卡同名上限，含义在规则书里没有更细的说明，所以只在
        /// <see cref="LegendaryTotalLimit"/> 里单独暴露，不混进这里。
        /// </summary>
        public static int CopyLimit(string rarity)
        {
            // ⚠️ 不带卡型这一档 = **「非督军」档**（等价于 `CopyLimit(rarity, null)`）。
            //    凡手上**拿得到 `CardDef.Type`**、而且那张卡**可能是督军**的地方，一律走下面那个重载 ——
            //    只看稀有度会把**非传说督军**算成 2（A894：我们池子里有 28 张这样的）。
            return CopyLimit(rarity, null);
        }

        /// <summary>同名卡上限 —— **带卡型那一档**（对应原版
        /// `GameplayVariablesData.GetMaxCopiesInDeck(rarity, cardType)`）。
        ///
        /// <para>🔴 **判据（原版硬编码，逐句实读）**：
        /// `d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c`：
        /// <code>
        /// if (param_3 == 10) return 1;        // cardType == Hero ⇒ 恒定 1（在稀有度判断【之前】）
        /// if (param_2 == 4)  return … +0x18;  // rarity == Legendary ⇒ numberOfCopiesLegendary
        /// return … +0x14;                     // 其余稀有度          ⇒ numberOfCopiesOtherRarities
        /// </code>
        /// `cardType` 的取值表 = `d:/2/tools/il2cpp_out/dump.cs:45710`
        /// `enum CardTypeOptions { Minion = 0, Hero = 10, Tactic = 20, Whispers = 40 }`
        /// ⇒ **`10` 就是督军**（我们卡池里那一档 = `CardDef.Type == "hero"`）。
        /// 🔴 **`return 1` 排在稀有度判断之前** ⇒ **非传说督军也是 1**：我们池子实测
        /// 56 位督军里 **28 张不是传说**（`epic` 15 / `rare` 13，数据 = `RuleEngine/Resources/cards_engine.json`）
        /// —— 只看稀有度会把它们算成 2。（这条 2026-09-22 就写在本文件头部的说明里，
        /// 但代码一直没照它做 —— A894 2026-10-17 才补上。）</para>
        ///
        /// <para>⚠️ <paramref name="type"/> 传 `null`/空 = 「不按督军算」，留给拿不到卡型的调用点；
        /// ⛔ 别在**可能拿到督军**的地方省这个实参。</para></summary>
        public static int CopyLimit(string rarity, string type)
        {
            if (IsWarlordCardType(type)) return 1;      // 原版那句 `if (cardType == 10) return 1;`
            // 🔴 值住在 `GameplayVariables`（唯一出处）。**两个模式这一条相同**（4/1 那对就是 2/1）。
            var v = GameplayVariables.Classic;
            return IsLegendary(rarity) ? v.numberOfCopiesLegendary : v.numberOfCopiesOtherRarities;
        }

        /// <summary>「这张卡的**卡型**算不算督军」—— 原版 `cardType == 10`（`CardTypeOptions.Hero`，判据同上），
        /// 我们卡池里那一档写成 `CardDef.Type == "hero"`。
        /// ⚠️ 名字里带 `CardType` 是**故意的**：全仓另有两个同义不同物（`UnitState.IsWarlord` = 「这一个单位是督军」、
        /// `BoardLayout.IsWarlord(slot)` = 「这一格是督军格」）—— 本函数答的是**卡型**那一问，只服务**同名上限**。</summary>
        public static bool IsWarlordCardType(string type) { return type == "hero"; }

        /// <summary>**整副**牌最多几张传说（规则书:64）。经典模式返回 `int.MaxValue`（**无此限制**）。
        /// 🔴 值住在 <see cref="GameplayVariables"/>（唯一出处）。
        /// <para>🟢 **2026-10-25（`E1` · 第五段 · R3 §六·2）就地订正：这条不再是「两种读法都说得通」的悬案，判据已经齐了。**
        ///   · 官方文案 `Event_Description/Classic/Help` = **`No Legendary card limit`**（中文「无传奇张牌限制」）
        ///     · `Event_Description/Skirmish/Help` = `Up to 4 Legendaries (Warlord not included)`；
        ///   · 原版闸 `DeckUtility.CanAddCard.c:105` 是 `if (0 < gameplayVariables.maxLegendaries)` ⇒ 
        ///     **非正数 = 本模式无上限**（`CanAddCard.c:132` 才是 `limit <= count ⇒ 挡`）。
        ///   ⇒ 结论：**经典没有整副传奇总数上限**，遭遇是 4。`GameplayVariables.Classic.maxLegendaries` 已按此改成 **0**。</para>
        /// ⚠️ **经典那一支仍然必须写字面 `int.MaxValue`，⛔ 不许写成 `return Classic.maxLegendaries`** —— 
        ///    `maxLegendaries` 的 `0` 是**「关掉这条闸」的哨兵**，直接返回 `0` 会让
        ///    `legendaries > limit` **任何一张传奇都被挡**（= 一刀砍掉所有经典卡组）。
        ///    ⇒ 判据的两种表示在这里必须换算一次（`<= 0 ⇒ int.MaxValue`），
        ///    ⛔ 别把换算省掉、也别把哨兵改成 `1`。</summary>
        public static int LegendaryTotalLimit(bool skirmish)
        {
            return skirmish ? GameplayVariables.Skirmish.maxLegendaries : int.MaxValue;
        }

        /// <summary>这副卡组要几张阵营卡。零值住在 <see cref="GameplayVariables"/>。</summary>
        public static int CardCount(bool skirmish)
        {
            return GameplayVariables.For(skirmish ? GameMode.Skirmish : GameMode.Classic).deckSize;
        }

        public static bool IsLegendary(string rarity)
        {
            return string.Equals(rarity, "legendary", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 校验一副卡组。`lookup` 把卡 id 映射成卡（取不到返回 null）。
        ///
        /// 顺序有意为之：**先查卡在不在池子里，再查数量，最后查上限** ——
        /// 和 `rule_core.play_card` 的「先费用后格位」是同一个思路：
        /// ⚠️ **2026-10-19（`D26`）**：这里的 `rule_core` 是**我们自己的上一版 Godot 复刻**
        /// （**旁证、非权威**，见 `CLAUDE.md` 铁律 2 的 2026-09-18 更正）——
        /// 「先费用后格位」那一条的**真判据**在 `RuleCore.PlayCard` 里已经改指
        /// `decomp_full/BattleManager__PayCardCost.c` 一族；本文件**只是打个比方**，
        /// ⛔ 别把它读成「校验顺序照原版」。
        /// 后面的判据依赖前面的结果，顺序反了会给出误导性的错误码。
        /// </summary>
        // ── 卡组准入：原版 `inventoryOptions` / `showInCollectionOptions`（`A1798`）──────────
        // 取值表（原版枚举，逐条照抄）：
        //   `CardInventoryOptions`   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardInventoryOptions.cs`
        //       `InInventory = 0` · `CantAddToDeck = 5` · `NotInInventory = 10`
        //   `ShowInCollectionOptions` 同目录 `ShowInCollectionOptions.cs`
        //       `Hide = 0` · `Show = 5` · `OnlyIfOwned = 10`
        public const int OptUnknown = -1;            // 真值里查不到这张卡（⛔ 别当 0！见 `CardDef.InventoryOptions`）
        public const int InInventory = 0;
        public const int OptionCantAddToDeck = 5;
        public const int OptionNotInInventory = 10;
        public const int ShowInCollection = 5;

        /// <summary>**这张卡能不能进 30 张卡组**（`true` = 不能）。**判据只此一处**
        /// —— `Validate`、卡组编辑器的 `CanAdd`、卡池筛选都转发到它，⛔ 别在别处再写一份。
        ///
        /// <para>🔴🔴 **2026-10-14 就地订正（铁律 5）—— 本节原来写着「那个字段本地解包产物里根本没有」。**
        /// 原文（2026-09-27 加，已删）：<i>「**原版那侧的判据**是 `spellType ∈ {Sabotage 230, Secret 150,
        /// CombatElixir 260}` + **`inventoryOptions == CantAddToDeck(5)`**；而**那个字段本地解包产物里根本没有**
        /// （卡资产包只有 Sprite/Audio/Texture2D）⇒ 我们改用 `Subtype`（唯一能拿到的等价信号）。」</i>
        /// **实际**：`inventoryOptions` **有**，而且是**全字段真值** —— 它住在**卡 SO 所在的包**
        /// （原版远端包 `allcards_assets_all`，1195 张卡 SO、2025-07-30 快照），**不在图/音包**里；
        /// 实测 **332 张非 0**（`CantAddToDeck = 5` **72 张** · `NotInInventory = 10` **260 张**）。
        /// **错因**：把「卡资产包只有 Sprite/Audio/Texture2D」（那是**图与音**那两个包）读成了「字段不存在」
        /// —— 判据源认错了包。该包 2026-10-13 从旧 Windows 用户缓存里抢救出来、2026-10-14 已解出。
        /// 数据链 → `数据/游戏数据/card_inventory_options.json`（生成器 `工具/gen_card_inventory_options.py`）。
        /// ⚠️ **原来那段还有一句要订正**：「`CantAddToDeck(5)`」并**不是**药剂/破坏/秘仪那 15 张的旗标
        /// —— 那 15 张的真值是 **`NotInInventory(10)`**；`CantAddToDeck(5)` 实际打在**环境/进攻/防御卡**
        /// 那一族上（我们卡池里落进 **31 张、全是 `type == "defence"`**）。两族**都被新口径拦下**，
        /// 所以用户 2026-09-27 那条「药剂/破坏/秘仪不能进卡组」的口径**继续成立**
        /// （`GSC15` 那个反例也一并被原版口径解释：它 `NotInInventory` ⇒ 原版本来就进不了卡组）。</para>
        ///
        /// <para>**原版判据（三个方法体逐句实读，`d:/2/tools/decomp_full/`）**：
        /// · `DeckUtility__ValidateDeckCards.c:68-72` —— `*(int*)(card + 0x4C) != 0` ⇒ `*param_3 = 4`，return 0
        ///   （`+0x4C` = `inventoryOptions`，偏移判据 `d:/2/tools/il2cpp_out/dump.cs:23250`）；
        /// · 同文件 `:73-77` —— `*(int*)(card + 0x48) != 5` ⇒ `*param_3 = 4`，return 0
        ///   （`+0x48` = `showInCollectionOptions`，同 `dump.cs:23249`）；
        /// · `DeckUtility__TryRemoveInvalidCards.c:39-43` —— 上面两条 + 卡型 + 阵营，**任一成立就移除该卡**。
        ///
        /// 🔴 **已知的【原版自相矛盾】，我们按多数那一版做（本件现核，如实记）**：
        /// `CardDeck__CanAddCard.c:40` 把这两条写成 **`&&`**（`A && B` ⇒ `MenuDeck/Error/InvalidCard`），
        /// 与上面三处的「各自独立成立即拦」（= `||`）**不一致**，差集实测 **77 张**
        /// （72 张 `CantAddToDeck` + 5 张 `NotInInventory & Show` 的暗黑天使秘仪卡）。
        /// ✅ **可自洽的解释**：`NotInInventory` 的原意在原版由**持有数**兜住 ——
        /// `CanAddCard.c:76-77` 的 `InventoryManager.GetOwnedCount` 对这类卡返回 0 ⇒ 照样加不进去，
        /// 所以 `&&` 那两条只是「完全内部的卡」的**冗余兜底**，原版看不出差别。
        /// 🔴 **我们是单机全解锁（没有持有数那道闸 —— 那正是 `MenuDeckErrorNumber` 里
        /// 「`CardsNotOwned` 我们永远不回」的原因）** ⇒ 必须按 `||` 那一版做，
        /// 否则那 77 张会**静默变得能进卡组**（`Append` 里那句「不许静默失败」）。
        /// 🔴 **2026-10-18 就地订正（卡池排序与显隐）**：上面这句**只对其中 72 张成立**（那批是
        /// `inv=5`、在本池里全是 defence ⇒ 已被卡型路由先返回）；**另外 5 张** `inv=10 & show=5`
        /// 的暗黑天使秘仪按用户全解锁边界**有意放开**（主对话 2026-10-18 裁定）⇒ 本函数已改成 `&&`。</para>
        ///
        /// <para>**两条路由照原版**：`CanAddCard.c:32-39` 先把 `cardType == Hero(10)` 交给 `CanAddHero`、
        /// 把 `cardType == Tactic(20) && spellType == 210` 交给 `CanAddDefensive` —— **这两个分支在
        /// 那两个字段之前就 return 了** ⇒ **督军与防御卡不吃本条判据**（防御卡本来就该留在卡池里，
        /// 玩家要从那儿挑 —— 用户 2026-09-27 指出过）。本函数把这条路由**收在内部**（判据只此一处），
        /// 于是调用点不用各自记得先挡 hero/defence。</para>
        ///
        /// <para>**落地的效果（`A2052` 关账现读，2026-10-18；⛔ 读数会变，别当基线）**：判据 = 现扫
        /// `cards_engine.json`（**张数一律现读 `count`**）—— 全池 **1127** 张里 `inventoryOptions &lt; 0`
        /// 或 `showInCollectionOptions &lt; 0` 的 = **0 张**（真值覆盖缺口已被**当前 build 的 SO** 补上；
        /// 原文那句「真值接不上的 171 张」**已过期**）；按原版 `||` 口径**会拦 127 张**（126 张
        /// `type == "tactic"` + 1 张 `unit`，**全是 `NotInInventory(10)`**；其中 5 张 `inv=10 & show=5`
        /// 的暗黑天使秘仪见上一段的**有意放开** ⇒ **现口径实际拦 122 张**）。
        /// 🔴 **2026-10-18（卡池排序与显隐）改口径后 = 拦下 122 张** —— 那 5 张暗黑天使秘仪
        /// （`inv=10` 但 `show=5`）按用户全解锁边界**放开**（见下面 `return` 前那段注释与
        /// `资料/普查产出_1018/AUD_卡池排序与天赋卡显隐.md` §五.B.5）。</para>
        ///
        /// <para>🔴 **`A2052` 关账（2026-10-18，只补注释、⛔ 不动行为）**：那条账原来写「池里 113 张卡的
        /// id 仍是 32 位 GUID」—— **前提已被 `A2069` 推翻**（那些 GUID **就是**原版的 `uniqueId`：
        /// 快照里按同 `uniqueId` 找得到、`m_Name` = `Z-NC &lt;阵营&gt;-&lt;名字&gt;`、`cardShortId = 0`）。
        /// **仍成立的那半 = NC 卡**（`NotInInventory`）：它们**本来就该被卡组拦掉** —— 判据 = 原版
        /// `DeckUtility__ValidateDeckCards.c:68-72`（`inventoryOptions != 0` ⇒ `*param_3 = 4`、return 0）
        /// 与 `DeckUtility__TryRemoveInvalidCards.c:39-43`（逐张摘除）⇒ **本分支在那 5 张之外的行为符合原版**
        /// （那 5 张按用户全解锁边界**有意放开**，见上一段）。
        /// ⚠️ 2026-10-18（`K06`）换 id 之后，这些 NC 卡的 id 也是**真 id**（短号 / GUID 形状都可能是
        /// 原版 `uniqueId`）⇒ ⛔ **别再把 GUID 形状的 id 当成「自造残留」**。</para>
        /// </summary>
        public static bool CannotAddToDeck(CardDef c)
        {
            if (c == null) return false;
            // 原版那两条路由（`CanAddCard.c:32-39`）：督军与防御卡各走各的校验，不吃本条。
            if (c.Type == "hero" || c.Type == "defence") return false;
            // 真值里查不到（-1）⇒ 不拦，并**出声**（静默用默认值就等于「发明口径」，见铁律 2）。
            if (c.InventoryOptions < 0 || c.ShowInCollectionOptions < 0)
            {
                WarnUnknown(c);
                return false;
            }
            // 🔴 **2026-10-18（卡池排序与显隐 · 用户全解锁边界，就地改口径）**：
            //   原来是 `||`（照 `ValidateDeckCards.c:68-77` / `TryRemoveInvalidCards.c:39-43` 那三处），
            //   理由是「照 `CardDeck__CanAddCard.c:40` 的 `&&` 会让那 77 张差集静默变得能进卡组」。
            //   **今天按用户边界放开的只有其中 5 张**：`inv=10` **`show=5`** 的暗黑天使秘仪
            //   （`Convoke the Circle` / `None Must Know` / `Obscure Ritual` / `Rites of Penance` / `Smothering Decree`）。
            //   原版它们卡池里**看得见**、`CanAddCard` 的列判据也放行，最后是靠
            //   `InventoryManager.GetOwnedCount == 0`（`CanAddCard.c:76-77`）拦住的 —— 我们**单机全解锁、
            //   没有持有数这道闸** ⇒ 主对话 2026-10-18 裁定：**显示且可加**（判据 → `资料/普查产出_1018/
            //   AUD_卡池排序与天赋卡显隐.md` §五.B.5）。
            //   差集里另外那 72 张 `inv=5`（`CantAddToDeck`）在本池里 **39 张全是 `type == "defence"`**，
            //   已被上面那条路由（`CanAddCard.c:32-39`）先返回 ⇒ 换 `&&` 后**实际只放开这 5 张**，其余一个不动
            //   （现读 `cards_engine.json`：`inv=5` 39 张全是 defence；`inv=10 & show=5` 5 张；`inv=10 & show=0` 122 张）。
            //   ⚠️ 这是**有意偏离** `ValidateDeckCards` 的 `||`，锚点断言在 `Editor/DeckScene.cs`（`A1798` 那一节已跟着改）。
            return c.InventoryOptions != InInventory && c.ShowInCollectionOptions != ShowInCollection;
        }

        // 真值缺失（`-1`）**总共只出一次声** —— 卡池是 1126 张级的、实测缺 171 张，
        // 逐张打会把日志刷掉（进一次卡组编辑就是 149 行）。⚠️ 这条**不是**判据的一部分，
        // 只是「不许静默失败」那一句的落点；逐张清单在 `card_inventory_options.json` 的 `unknown` 段。
        static string _unknownReported = null;

        static void WarnUnknown(CardDef c)
        {
            if (_unknownReported != null) return;
            _unknownReported = c.Id;
            UnityEngine.Debug.LogWarning(
                $"[RuleEngine] 卡组准入真值缺失：至少 `{c.Id}`（{c.Name}）的 `inventoryOptions` / "
                + "`showInCollectionOptions` 是 `-1`（真值快照 2025-07-30 比现池旧）—— 这些卡按原版口径"
                + "**不拦**它们进卡组。**同类不再逐张打**（实测 171 张，其中非督军/防御卡 149 张）；"
                + "逐张清单见 `数据/游戏数据/card_inventory_options.json` 的 `unknown` 段。");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        //  `A1866`：原版 `DeckUtility.RemoveInvalidCards` —— **逐张摘除不合法卡**
        // ══════════════════════════════════════════════════════════════════════════════
        // 🔴 **2026-10-14（`A1866`，用户点名「照原版」）**：原版**从不**因为「卡组里有不合法的卡」
        //    把**整副**判废 —— 它跑 `DeckUtility.RemoveInvalidCards(deck)` 把**那几张摘掉、其余照用**。
        //    （我们原来那条路是 `BattleDriver.ResolveDeck`：「`Validate` 非 `None` ⇒ 整副退回自动凑」
        //      ⇒ 一份老存档里只要混进 1 张 `inv=10` 的战术卡，**玩家编的 30 张全被换掉**。）
        //
        // **判据（`d:/2/tools/decomp_full/`，四个方法体逐句实读）**：
        //   · `DeckUtility__RemoveInvalidCards.c` = 公开入口，顺序固定：
        //       ① `TryRemoveCardsNotOwned` → ② `TryRemoveInvalidHero` → ③ `TryRemoveInvalidCards`；
        //       返回值 = 「有没有摘掉东西」（`:78` `return 0 < removed.Count`）。
        //   · `DeckUtility__TryRemoveInvalidHero.c:26-27` —— 督军位上那张的 `cardType`（`+0x94`）
        //       `!= 10`(`CardTypeOptions.Hero`) ⇒ 把它从卡组里摘掉。
        //   · `DeckUtility__TryRemoveInvalidCards.c:29-50` —— **从尾到头**遍历 `deck.cardLibrary`
        //       （`+0x58`），任一条件成立就 `RemoveCardFromDeck` 那一张：
        //         a. `cardType ∉ {0 Minion, 0x14 Tactic}`（`:39-43`）
        //         b. `inventoryOptions`(`+0x4C`) `!= 0`（`:41`）
        //         c. `showInCollectionOptions`(`+0x48`) `!= 5`（`:42`）
        //         d. `cardArmy`(`+0x2C`) `!= deck.deckArmy`(`+0x50`）（`:42`）
        //         e. 自定义模式（`CustomGameModeEvent != null`）下同名张数超
        //            `GetMaxCopiesInDeck`（`:47-54`）
        //         f. 卡自己的虚方法谓词（vtable 槽 `0x2A8`）为真（`:56-57`）
        //   · 另一处调用点**只有一处**：`DeckEditingWindow__TryOpen.c:47`（**打开卡组编辑器**时对
        //       **编辑器里那份副本**跑一遍，结果用来置 `deck+0x60`）—— **不是**开局那一趟。
        //       ⇒ 原版开局不跑它；我们把它放在 `ResolveDeck`（开局）是**为了兜住「存档里已经有
        //       不合法卡」这个状态在原版结构下不会出现**（原版编辑器那一趟 + `CanAddCard` 两道闸
        //       已经清理过）—— 如实记着：**调用点比原版多一处**，判据（摘谁）逐条照抄。
        //
        // **原版那三条在本件里【做不到】的**（`CLAUDE.md` 铁律 11 例外①：判据本身是空的）：
        //   · ① `TryRemoveCardsNotOwned` —— 判据是 `InventoryManager.GetOwnedCount`（**持有数**），
        //        我们是**单机全解锁**、没有持有数这道闸（这正是 `MenuDeckErrorNumber` 里
        //        「`CardsNotOwned` 我们永远不回」的原因）⇒ **整条不做**，理由记在这里。
        //   · ⑤ 自定义模式同名上限 —— 判据是 LiveOps 事件（`CustomGameModeEvent` 我们恒 null）
        //        ⇒ 永不成立，不写。
        //   · ⑥ 那个虚方法谓词 = `PlayerItem.IsHidden()`（槽位换算：`IsHidden` 在
        //        `PlayerItem` 的 slot 23，`23*16` 落在 `0x2A8`；唯一的同槽调用点
        //        `PrebuiltDeck__HasHiddenCards.c` 的语义就是它）⇒ 判据是**远端 LiveOps 配置**
        //        （`FeatureConfig.IsItemHidden`，逐句读过：它拿服务器下发的配置去比 item）
        //        ⇒ 本地**没有判据**，不写（⛔ 不许拿我们自己的某个字段顶替）。
        //
        // ⚠️ **未知 id 那一支是我们加的**：原版 `cardLibrary` 里是**对象引用**、不可能有
        //    「查不到的 id」（真有 null 时原版直接抛——`TryRemoveInvalidCards.c:38`）。
        //    我们的卡组存的是**字符串 id**，所以「卡池里没有这张」必须有个处置 ⇒ **摘掉并出声**
        //    （= `DeckError.UnknownCard`，与 `Validate` ① 同一个码）。
        public struct RemovedCard
        {
            /// <summary>我们卡池的 id（未知 id 时就是**原样那个串**，所以可能查不到卡）。</summary>
            public string Id;
            /// <summary>原版**哪一条判据**把它摘掉的 —— 复用既有 `DeckError` 码，
            /// 于是文案可以直接走 <see cref="Describe"/>（`MenuDeck/Error/*` 那族词条）。
            /// ⛔ 本层仍然**一个给人看的字都不产**。</summary>
            public DeckError Why;
        }

        /// <summary>摘掉卡组里**不合法的那几张**（原版 `DeckUtility.RemoveInvalidCards` 的引擎侧实现）。
        /// **就地改 <paramref name="deck"/>**（照原版：它也是就地 `RemoveCardFromDeck`）。
        ///
        /// <para>返回**摘掉的那些** + 各自的原因（原版只回一个 `bool` —— 我们多给原因，
        /// 调用方要用它给玩家/日志一个交代；`Count > 0` 就等价于原版那个 `true`）。</para>
        ///
        /// <para>⚠️ **调用方该传一份副本**：原版是对**编辑器里那份 `CardDeck` 副本**跑
        /// （`DeckEditingWindow__TryOpen.c:41` 那行先 `new CardDeck(deckToCopy)`），
        /// 存盘那份不动。⇒ 开局那条路请 `saved.Clone()` 之后再传进来。</para>
        ///
        /// <para>⚠️ 摘完之后**够不够张数、还有没有督军**是**调用方**的事 —— 原版也是：
        /// 它摘完就返回，`ValidateDeck` 是**另一条**路（`RemoveInvalidCards` 摘完可能仍然不合法）。
        /// ⇒ 本函数**不判张数、不判上限、不碰防御卡位**。</para></summary>
        public static List<RemovedCard> RemoveInvalidCards(PlayerDeck deck, Func<string, CardDef> lookup)
        {
            var removed = new List<RemovedCard>();
            if (deck == null || lookup == null) return removed;

            // ---- ① 督军位（原版 `TryRemoveInvalidHero.c:26-27`）----
            //   ⚠️ 原版摘的是**那张卡**；我们的督军是**一个槽**（`WarlordId`）⇒ 摘 = 清空该槽。
            //     清空之后卡组就没有督军了（`Validate` 会报 `NoWarlord`），与原版一致。
            var warlord = string.IsNullOrEmpty(deck.WarlordId) ? null : lookup(deck.WarlordId);
            if (!string.IsNullOrEmpty(deck.WarlordId) && (warlord == null || warlord.Type != "hero"))
            {
                removed.Add(new RemovedCard
                {
                    Id = deck.WarlordId,
                    Why = warlord == null ? DeckError.UnknownCard : DeckError.WarlordNotHero,
                });
                deck.WarlordId = null;
                warlord = null;
            }

            // ---- ② 逐张（原版 `TryRemoveInvalidCards.c:29-62`）----
            //   原版**从尾到头**遍历（`iVar1 = count-1; --iVar1`）—— 位置不动照样照抄，
            //   因为摘除顺序会进日志、也会进「摘掉的是第几张」这类记录。
            //   ⚠️ `CardIds` 在 JSON 缺键时会被 `JsonUtility` 置成 null（字段初始化跑不到）——
            //      `Validate` 那条老路也一样直接 `foreach` 它，这里补一句不是发明行为、是防 NPE。
            if (deck.CardIds == null) deck.CardIds = new List<string>();
            for (int i = deck.CardIds.Count - 1; i >= 0; i--)
            {
                string id = deck.CardIds[i];
                var c = string.IsNullOrEmpty(id) ? null : lookup(id);
                DeckError why;
                if (c == null) why = DeckError.UnknownCard;                       // 我们加的（见上文）
                else if (c.Type != "unit" && c.Type != "tactic") why = DeckError.WarlordInCards;   // a：卡型
                else if (CannotAddToDeck(c)) why = DeckError.EffectOnlyCard;      // b + c：inv / show
                else if (warlord != null && !SameFaction(c.Faction, warlord.Faction))
                    why = DeckError.WrongFaction;                                 // d：阵营
                else continue;
                deck.CardIds.RemoveAt(i);
                removed.Add(new RemovedCard { Id = id, Why = why });
            }
            return removed;
        }

        public static DeckError Validate(PlayerDeck deck, Func<string, CardDef> lookup, bool skirmish = false)

        {
            if (deck == null) return DeckError.NoWarlord;
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));

            // ① 全部 id 都要能在卡池里找到
            var warlord = deck.WarlordId == null ? null : lookup(deck.WarlordId);
            var defensive = deck.DefensiveId == null ? null : lookup(deck.DefensiveId);
            var cards = new List<CardDef>();
            foreach (var id in deck.CardIds)
            {
                var c = lookup(id);
                if (c == null) return DeckError.UnknownCard;
                cards.Add(c);
            }

            // ② 督军位
            if (warlord == null) return DeckError.NoWarlord;
            if (warlord.Type != "hero") return DeckError.WarlordNotHero;

            // ③ 防御卡位 —— 🔴 **2026-09-26 改成「可带可不带」**（原来要求恰好 1 张）
            //    判据（唯一）→ `资料/加时与冲突模式_原版规格.md` §2.7c：
            //    原版 `DeckUtility.ValidateDeck` 与它的三个子校验（Cards / Ownership / Warlord）
            //    **方法体里都不出现防御卡字段 `+0x48`** ⇒ **0 张合法**；遭遇窗那道闸门
            //    （`HasValidDeckWithValidationMessage`）用的 `DeckError` 六个取值里**也没有「缺防御卡」**。
            //    原版语义 = 「**带就用手挑的那张，不带就开局发一张**」
            //    （`BattleManager.AddGoesSecondCardToDeck` 从防御卡池随机抽一张进手牌，
            //      而卡组里有就优先用卡组那张）—— 兜底那半我们做在 `DeckBuilder.FromDeck`。
            //    ⚠️ **带的那张仍然要真的是防御卡**：这条是**我们自己的槽位模型**要的（原版不查它），
            //      保留无害 —— 但这不等于原版有这条校验。
            if (defensive != null && defensive.Type != "defence") return DeckError.DefensiveNotDefence;

            // ④ 张数
            int want = CardCount(skirmish);
            if (cards.Count > want) return DeckError.TooManyCards;
            if (cards.Count < want) return DeckError.TooFewCards;

            // ⑤ 阵营：所有卡必须与督军同阵营（规则书:53）
            foreach (var c in cards)
                if (!SameFaction(c.Faction, warlord.Faction)) return DeckError.WrongFaction;

            // ⑥ 督军/防御卡不能又混进普通卡位
            foreach (var c in cards)
                if (c.Type == "hero" || c.Type == "defence") return DeckError.WarlordInCards;

            // ⑥b 2026-10-14（`A1798`）：**原版卡组准入** —— 判据只此一处 = `CannotAddToDeck`
            //     （原版 `DeckUtility__ValidateDeckCards.c:68-77`；这里是它的卡片清单那一支）。
            //     原来的 `IsEffectOnly(c.Subtype)`（按兵种猜的三类）**已删** —— 见那个方法的订正注。
            foreach (var c in cards)
                if (CannotAddToDeck(c)) return DeckError.EffectOnlyCard;

            // ⑦ 同名上限
            var copies = new Dictionary<string, int>();
            foreach (var c in cards)
            {
                int n;
                copies.TryGetValue(c.Id, out n);
                copies[c.Id] = n + 1;
            }
            foreach (var kv in copies)
            {
                var c = lookup(kv.Key);
                // ⑦ 传 `c.Type`（A894 起 `CopyLimit` 有卡型那一档）—— 本处**两条路同值**：
                //    ⑥ 已经把 `Type == "hero"` 的卡判成 `WarlordInCards` 提前返回了
                //    ⇒ 走到这里的 `cards` 里不可能有督军。仍然照传，是为了**只有一处判据**、
                //    将来谁把 ⑥ 挪了位置也不会静默退回「只看稀有度」。
                if (kv.Value > CopyLimit(c.Rarity, c.Type)) return DeckError.CopyLimitExceeded;
            }

            // ⑧ 遭遇模式的传说卡总数
            int legendaries = 0;
            foreach (var c in cards) if (IsLegendary(c.Rarity)) legendaries++;
            if (legendaries > LegendaryTotalLimit(skirmish)) return DeckError.LegendaryTotalExceeded;

            return DeckError.None;
        }

        /// <summary>
        /// 阵营名比较。卡池里的阵营是 `SaimHann` / `Ultramarines` 这种，大小写统一按不敏感比。
        /// 留这个函数是为了**只有一处**在做比较 —— 将来要加别名表也只改这里。
        /// </summary>
        public static bool SameFaction(string a, string b)
        {
            return string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 结算时拿到几个骷髅头（规则书:36「把敌方督军生命削减至 20 / 10 / 0 时（首次）各获得 1 个」）。
        ///
        /// 传**这一局里敌方督军降到过的最低生命**就行 —— 生命只会往下走（治疗会回，
        /// 但「首次得到」不回退），所以用最小值算和逐次记事件是等价的，不需要历史。
        /// </summary>
        public static int SkullsFor(int minEnemyWarlordHealth)
        {
            int n = 0;
            foreach (var t in SkullThresholds)
                if (minEnemyWarlordHealth <= t) n++;
            return n;
        }

        /// <summary>`MenuDeck/Error/` 那一族的**前缀**（原版两族键共用的那一段）。</summary>
        public const string TermPrefix = "MenuDeck/Error/";

        /// <summary>
        /// 错误码 → **I2 术语【键】**（⛔ **本层一个给人看的字都不产** —— 见下面那段判据）。
        ///
        /// <para>🔴 **2026-10-18（`G9`）改口径**：本方法**原来是引擎侧唯一一处造中文句子**的地方
        /// （`DeckError` 枚举 → 中文整句），而它同时被**显示层**（卡组编辑的消息通道 / 收藏线的
        /// `DeckInfoPopup` 日志）与**断言**当文案用 ⇒ 换语言时它**静默不跟**
        /// （正是「不许拿给人看的句子当逻辑」那一族）。现在它**只出键**，
        /// 由显示层走 <c>CardPresentation.Loc.T(键)</c> 取词条（两列文案在 `Core/Loc.cs` 的表里）。</para>
        ///
        /// <para>🔴 **键名的判据（原版的【具名】那一族）**：原版 `CardDeck.CanAddCard` /
        /// `CanAddHero` / `CanAddDefensive`（`d:/2/tools/decomp_full/`，三个方法体逐个读完）
        /// 出的是 <c>"MenuDeck/Error/" + 名字</c>，地址表实读（`d:/2/tools/il2cpp_out/stringliteral.json`）：
        /// <c>0x42CF3F0=InvalidArmy</c> · <c>0x42CF4F0=InvalidCard</c> · <c>0x42CF5F0=InvalidDeck</c> ·
        /// <c>0x42CF6F0=InvalidDeck/Short</c> · <c>0x42CF7F0=MaxCopiesReached</c> ·
        /// <c>0x42CF8F0=MaxLegendayCopiesReached</c> · <c>0x42CF9F0=MissingCopies</c> ·
        /// <c>0x42CFAF0=ThisCardIsBanned</c> · <c>0x42CF1F0=FailedToDelete</c> ·
        /// <c>0x42CF2F0=HasNotAssignedDefensiveCard</c>。
        /// ⚠️ **另有【数字】一族**（`0x42CFBF0 = "MenuDeck/Error/{0}"`，`{0}` = 原版 `DeckError` 的号；
        /// 表现层那半边 `CardPresentation.DeckRuntime.MenuDeckErrorNumber` 出的是它）——
        /// 两族**并存**，我们用具名那一族（它才是「给玩家看的那句话」的载体，
        /// 数字那一族只给 `WindowsManager.ShowPopUp` 用）。</para>
        ///
        /// <para>🔴 **我们这 13 条键是【自拟】的**：原版具名族只有上面那 8 条 + `{0}`/`{0}/Short` 两条，
        /// 而我们的 `DeckError` 有 13 档、名字也**一个都不重名**
        /// ⇒ 键名 = `"MenuDeck/Error/" + 枚举名`（形状照原版那一族）。
        /// ⛔ **别把这几条当成原版实有的字面量** —— 要看原版实有哪几条，看上面那张地址表。
        /// 两列文案同样是我们起的（原版那套在**远端 I2 语言表**，本地取不到）。</para>
        ///
        /// <para>`None` ⇒ **空串**（原版 `ToRawLocalizationString` 的 `err == 0` 那一支；
        /// `Loc.T("")` 也返回空串 ⇒ 调用点不用再判一次）。</para>
        /// </summary>
        public static string Describe(DeckError e)
        {
            if (e == DeckError.None) return "";
            // 🔴 **2026-10-17（`A1868`/`A1872`）**：原版 `MenuDeck/Error/*` **没有 `EffectOnlyCard` 这条键**
            //   （官方具名族只有 9 条；`Core/Loc.cs` 已按判据删掉那条自造键）——
            //   若继续拼 `TermPrefix + e`，`Loc.T` 会把**键名本身**印到屏幕上
            //   （`DeckRuntime.DeckErrorText(DeckError.EffectOnlyCard)` 的两个调用点会现形）。
            //   ⇒ 这一档归 **catch-all `MenuDeck/Error/InvalidDeck`**（官方表里已有、EN/ZH 两列齐全），
            //   与 `DeckRuntime` 数字族退兜底的口径一致。
            if (e == DeckError.EffectOnlyCard) return TermPrefix + "InvalidDeck";
            // 🆕 **2026-10-25（`E1` · 第五段）**：这一档的原版键叫 `MaxLegendayCopiesReached`
            //   （⚠️ 原版自己把 `Legendary` 拼成 **`Legenday`** —— 那是**本地化键的字面量**，
            //    官方 EN/ZH 两列都在这个名字下；⛔ **别顺手把拼写改对**，改了 `Loc.T` 就取不到）。
            //   判据 = 官方具名族地址表（`Describe` 的 doc 那张，`0x42CF8F0=MaxLegendayCopiesReached`）。
            if (e == DeckError.LegendaryTotalExceeded) return TermPrefix + "MaxLegendayCopiesReached";
            return TermPrefix + e;
        }

        // ============================================================================================
        //  🆕 2026-10-25（`E1` · 第八段）—— **「上屏用哪个键」的判据（只此一处）**
        //
        //  【为什么需要】`DeckError` 有 14 档，但**原版 `DeckError` 枚举只有 6 个值**：
        //    `None0 · CardsNotOwned1 · MissingHero2 · InvalidDeckBannedCards3 · InvalidDeck4 · IncompleteDeck5`
        //    （`DeckUtility.ToRawLocalizationString(DeckError)` 现读 cpp2il `DeckUtility.cs:149-170`：
        //     `term = $"MenuDeck/Error/{error}"` —— **`{0}` 装的是枚举本身，`ToString()` 印的是【名字】**；
        //     `GetTermData(term) == null ⇒ 返回 "MenuDeck/Error/InvalidDeck"`）。
        //    ⇒ **原版真实上屏 = 具名族 `MenuDeck/Error/<枚举名>`，取不到退 `InvalidDeck`**。
        //    🔴 **数字族 `MenuDeck/Error/{2,4,5}` 那条判据已被推翻**（`Loc.cs` 里 `A1872` 那一段已记；
        //       本轮复核同一结论）：官方 I2 表里**一条数字键都没有**，且 `{0}` 装的是枚举。⛔ 别再走数字族。
        //
        //  【我们这侧的 14 档怎么落】三档（⛔ **不是**按「手写 `Loc` 表里有没有」推断 —— 
        //    那正是本批踩过两次的同一个坑：手写表里有 ≠ 原版有）：
        //   ① **原版有具名键** ⇒ 直接用它：
        //      `InvalidDeck` · `EffectOnlyCard`（`Describe` 已归 `InvalidDeck`）·
        //      `LegendaryTotalExceeded`（`Describe` 已归 `MaxLegendayCopiesReached`）；
        //   ② **我们自己的槽位判据**（原版不查督军/防御卡槽位 ⇒ 原版没有对应错误码）⇒ **保留具名**
        //      （摊到 `InvalidDeck` 会变成笼统的「卡组无效」，更没信息；这两条如实标「原版没有、我们补的」）：
        //      `WarlordAlreadySet` · `DefensiveAlreadySet`；
        //   ③ **其余 10 档** ⇒ **映射到原版那 6 个枚举名**，再走具名族（= 原版真实上屏路径）：
        //      `NoWarlord → MissingHero` · `TooFewCards/TooManyCards → IncompleteDeck` · 其余 → `InvalidDeck`。
        //      ⚠️ 这些自拟键**保留在 `Loc` 表里、⛔ 不要删**（语义标签，只是不当上屏文案）。
        // ============================================================================================

        /// <summary>这一档的**具名键**是不是【原版本地化表里真有】的。
        /// 判据 = `官方本地化_en+zh-CN.json` 与 `Resources/official_loc_13lang.json` **两表一致**（2026-10-25 逐键核过）。
        /// ⚠️ **⛔ 别拿「手写 `Loc` 表里有没有」当判据** —— 手写表里那 12 个自拟键**都在**，但那不代表原版有。
        /// </summary>
        public static bool HasOfficialNamedKey(DeckError e)
        {
            return e == DeckError.InvalidDeck                 // `MenuDeck/Error/InvalidDeck`（原版有）
                || e == DeckError.EffectOnlyCard              // `Describe` 已把它映射成 `InvalidDeck`（原版有）
                || e == DeckError.LegendaryTotalExceeded;     // 映射成 `MaxLegendayCopiesReached`（原版有）
        }

        /// <summary>这一档是**我们自己的槽位判据**（原版不查督军/防御卡槽位）⇒ 原版**没有对应错误码**。
        /// 摊到 `InvalidDeck` 会变成笼统的「所选卡组无效…」，**比具名更没信息** ⇒ 保留具名
        /// （`Loc` 里那两条中文是我们起的，⛔ 别当原版文案读）。
        /// </summary>
        public static bool IsOurOwnSlotRule(DeckError e)
        {
            return e == DeckError.WarlordAlreadySet || e == DeckError.DefensiveAlreadySet;
        }

        /// <summary>我们的档 → **原版 `DeckError` 那 6 个枚举名之一**（`DeckUtility.ToRawLocalizationString` 的 `{0}`）。
        /// 只有 ③ 那一档会用到；①② 直接用 `Describe` 产出的具名键。
        /// 判据 = cpp2il `DeckUtility.cs:149-170`（`term = $"MenuDeck/Error/{error}"`）+ 原版枚举 6 值。
        /// </summary>
        public static string OriginalErrorName(DeckError e)
        {
            switch (e)
            {
                case DeckError.None: return "None";
                case DeckError.NoWarlord: return "MissingHero";
                case DeckError.TooFewCards:
                case DeckError.TooManyCards: return "IncompleteDeck";
                default: return "InvalidDeck";
            }
        }

        /// <summary>**上屏该用哪个词条键**（判据只此一处，见本段顶部那张三档表）。
        /// `None` ⇒ 空串。
        /// ① 原版有具名键 · ② 我们自己的槽位判据 ⇒ `Describe(e)`；③ 其余 ⇒ `MenuDeck/Error/<原版枚举名>`。
        /// ⚠️ 调用方一律 `Loc.T(ErrorTextKey(e))` —— ⛔ 别在调用点再拼一遍 `Describe` / 数字族。
        /// </summary>
        public static string ErrorTextKey(DeckError e)
        {
            if (e == DeckError.None) return "";
            if (HasOfficialNamedKey(e) || IsOurOwnSlotRule(e)) return Describe(e);
            return TermPrefix + OriginalErrorName(e);
        }

        /// <summary>
        /// **短档文案**（原版 `MenuDeck/Error/*` 里**只有两条**有 `/Short`）——
        /// 🆕 2026-10-25（完全复刻 · 执行代理 A · 收藏域代理 `C` 的判据）。
        ///
        /// <para>**判据（第一权威，现读官方本地化表）**：`MenuDeck/Error/*` 共 **15 条**，其中
        /// **只有 2 条**带 `/Short`：`MenuDeck/Error/InvalidDeck/Short` ·
        /// `MenuDeck/Error/InvalidDeckBannedCards/Short`（`数据/游戏数据/官方本地化_en+zh-CN.json`
        /// 现读，EN/ZH 两列齐全）。**其余 13 条都没有短档。**</para>
        ///
        /// <para>**唯一外部调用点** = 遭遇事件窗左列「这副卡组为什么不可用」那一行
        /// （`Shell/SkirmishEventWindow.cs:505`，原版用的是短档）⇒ 调用方改走本方法。</para>
        ///
        /// <para>⛔ **别按「每条都加 Short」补文案** —— 只有那两条是特例；其余档原版**回落到**
        /// `MenuDeck/Error/InvalidDeck/Short`（`DeckUtility__ToShortRawLocalizationString.c:21-24` 的常量兜底，⛔ 不是回长档）。</para>
        ///
        /// <para>⚠️ **接线依赖**：`MenuDeck/Error/InvalidDeck/Short` 这两条**词条要进 `Core/Loc.cs`**
        /// （那是 `CardPresentation` 的文件、不在本代理白名单）—— 缺条时 `Loc.T` 会把键名印到屏幕上。
        /// 本批只落**引擎侧判据**，词条那条已作为改动请求交出去。</para>
        /// </summary>
        public static string DescribeShort(DeckError e)
        {
            if (e == DeckError.None) return "";             // 原版 `.c:17` 回静态字段（ILSpy 读作 `(string)0`）；两者在 `Loc.T` 里都等于「不显示」
            // 🔴 原版（`decomp_full/DeckUtility__ToShortRawLocalizationString.c:4-33`，🥇 33 行逐句）：
            //   `term = Format("MenuDeck/Error/{0}/Short", error)`（`{0}` = 枚举名），`GetTermData(term) == null`
            //   ⇒ **返回常量 `MenuDeck/Error/InvalidDeck/Short`**（`.c:21-24` 的 `DAT_1842cf6f0`）—— ⛔ 不是回长档。
            //   现读官方表：`MenuDeck/Error/*` 15 条里只有 `InvalidDeck/Short` 与 `InvalidDeckBannedCards/Short` 在。
            //   ⚠️ 改前我们对其余档回**长档**（`Describe(e)`）⇒ 与原版不同，2026-10-26 按 `.c` 订正（铁律 5）。
            string name = OriginalErrorName(e);             // 我们 14 档 → 原版那 6 个枚举名（同一处判据）
            string key = TermPrefix + name + "/Short";
            if (name == "InvalidDeck" || name == "InvalidDeckBannedCards") return key;
            return TermPrefix + "InvalidDeck/Short";        // 原版 `.c:23-24` 那句常量兜底
        }
    }

    /// <summary>
    /// 一副卡组。**只存 id，不存 CardDef** —— 存对象的话卡池一变就对不上了，
    /// 而且序列化出来是一大坨。UI 和引擎都通过 id 去卡池查。
    /// </summary>
    [Serializable]
    public class PlayerDeck
    {
        public string Name = "新卡组";
        public string WarlordId;
        public string DefensiveId;
        public List<string> CardIds = new List<string>();

        /// <summary>
        /// 这副卡组属于**哪个对局模式**：`0` = 经典 · `13` = 遭遇（取值照原版 `PlayModes`，
        /// 例：`PlayModes.Classic = 0` / `PlayModes.Skirmish = 13`）。
        ///
        /// 🔴 **为什么模式挂在卡组上**（2026-09-26 全量反编译查证，**判据全文 → `资料/加时与冲突模式_原版规格.md` §2.7**）：
        /// 原版 `CardDeck.gameMode : Nullable&lt;PlayModes&gt;`（`dump.cs:20483`，字段 **@0x70**）——
        /// 它是**卡组的固有属性**：**新建 / 导入那一刻**打上，**之后没有任何改它的路径**
        /// （全库只有两个写点：`SelectDecksTab__CreateDeck.c:17,20,24` 与 `CardDeck__DeserializeDeckString.c:133-135`）。
        /// 卡组编辑器**不认识「当前模式」**（`DeckEditingWindow` 20 个字段里没有它），
        /// 它**从正在编辑的这副卡组上读**（`EditingDeck.GameMode` → `GameModes.GetActiveEvent`）。
        /// 「按模式分」发生在**卡组列表 / 选卡组**那一层（候选卡组模式 == 当前模式）。
        ///
        /// ⚠️ **落盘向后兼容**：旧存档没有这个键 ⇒ `JsonUtility` 给 `0` ⇒ 正好 = 经典
        /// （原版卡组串那边也是「null 写 0」，见 `CardDeck__Serialize.c:68-75`）。
        /// ⚠️ **改这个字段要同时补四处拷贝点**（`Clone` / `DeckLibrary.CommitCurrent` /
        /// `DeckLibrary.ExportString` / `DeckLibrary.ImportString`）—— 漏一处就是**静默丢模式**。
        /// </summary>
        public int GameMode;

        /// <summary>是不是遭遇（Skirmish）模式。<see cref="GameMode"/> 的派生只读视图 ——
        /// 判据只此一处，别在别处再写一遍 `== 13`。</summary>
        public bool IsSkirmish { get { return GameMode == (int)RuleEngine.GameMode.Skirmish; } }

        /// <summary>
        /// 这副卡组用的**卡背**（`Resources/Art/cardbacks/` 里那张的**文件名**，如 `Cardback_AM_Cold Blood`）。
        /// 空/null = **还没选过** ⇒ 用「该阵营的默认卡背」顶上。
        ///
        /// 出处（原版同名字段）：`CardDeck.cardbackId : string`（`Assembly-CSharp/CardDeck.cs:16`）——
        /// **整副卡组一个卡背**（不是每张卡一个）：`CosmeticsSetup(pCardbackId, pWarcryId)` 一次写整副级字段
        /// （`CardDeck__CosmeticsSetup.c`）、`GetDeckCardback()` 无 index 参数、
        /// `DeckCosmeticDrawer` 只有**一个** `cosmeticImage`。默认**空串**（`CardDeck__GetEmptyDeck.c:124-127`）。
        /// 空的时候原版走 `ArmyUtilities.GetDefaultCardback(deckArmy)`（`CardDeck__GetDeckCardback.c`），
        /// 我们对应 `CardArt.CardBack(阵营)`。
        /// ⚠️ **落盘向后兼容**：旧存档没有这个键 ⇒ `JsonUtility` 反序列化成 null ⇒ 正好走「默认卡背」。
        /// </summary>
        public string CardbackId;

        public PlayerDeck() { }

        /// <param name="gameMode">本副卡组的模式（<see cref="GameMode"/>）；不传 = 经典（0）。
        /// ⚠️ 照原版：**模式是建组时定死的**，建完再改要走「另存/导入」，没有第三条路。</param>
        public PlayerDeck(string name, string warlordId, string defensiveId, IEnumerable<string> cards,
                          int gameMode = 0)
        {
            Name = string.IsNullOrEmpty(name) ? "新卡组" : name;
            WarlordId = warlordId;
            DefensiveId = defensiveId;
            CardIds = cards == null ? new List<string>() : new List<string>(cards);
            GameMode = gameMode;
        }

        public PlayerDeck Clone()
        {
            return new PlayerDeck(Name, WarlordId, DefensiveId, CardIds, GameMode) { CardbackId = CardbackId };
        }

        /// <summary>放进/拿走一张普通卡。返回是否真的变了（UI 用来决定要不要重排）。</summary>
        public bool AddCard(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            CardIds.Add(id);
            return true;
        }

        public bool RemoveCard(string id)
        {
            int i = CardIds.LastIndexOf(id);
            if (i < 0) return false;
            CardIds.RemoveAt(i);
            return true;
        }

        public int CountOf(string id)
        {
            int n = 0;
            foreach (var c in CardIds) if (c == id) n++;
            return n;
        }
    }
}
