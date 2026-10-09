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
        // 🆕 2026-09-27：**效果生成的卡不能进卡组**（药剂 / 破坏 / 秘仪，判据见 `IsEffectOnly`）。
        //    ⚠️ 用户明确要求、**明知有一条反例也照办**（`GSC15` 在原版 GSC 官方起手牌里 —— 见 `项目任务.md` §三 第 20 条）。
        EffectOnlyCard,
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

        /// <summary>遭遇模式：整副牌最多几张传说（规则书:64）。经典模式返回 int.MaxValue（**无此限制**）。
        /// 🔴 值住在 <see cref="GameplayVariables"/>（唯一出处）。
        /// ⚠️ **经典那一支必须留 `int.MaxValue`，不许写成 `Classic.maxLegendaries`** ——
        ///    经典模式的传说卡限制是**同名 1 张**（`numberOfCopiesLegendary`），**不是**「整副最多 1 张传说」；
        ///    改成后者会**一刀砍掉所有经典卡组**（`RuleEngineTest` / 卡组编辑器立刻红）。
        ///    原版那个字段叫 `maxLegendaries`，两种读法都说得通，我们按「只有遭遇用整副总数」这条走
        ///    —— 因为那是改造前就在跑的行为（`资料/加时与冲突模式_原版规格.md` §2.5 第 1 行给的是「经典 1 / 冲突 4」，
        ///    但**没写清是整副总数还是同名上限**；真值拿到之前不改经典口径）。</summary>
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
        /// <summary>🆕 **2026-09-27 用户拍板：这三类是【效果生成的】卡，不能加入 30 张卡组。**
        ///
        /// · `Combat Elixir`（**药剂** · 6 张 · 帝王之子）—— 卡面就是 `create a random Combat Elixir in your hand`；
        /// · `Sabotage`（**破坏** · 4 张 · 基因窃取者）—— 效果把破坏卡塞进**对手手牌**
        ///   （`BattleCardManager__CreateCard.c:98` 专门判 `spellType == 0xE6` 换敌方卡背）；
        /// · `Secret`（**秘仪** · 5 张 · 暗黑天使）—— `CreateSecret`。
        ///
        /// **原版那侧的判据**是 `spellType ∈ {Sabotage 230, Secret 150, CombatElixir 260}`
        /// + **`inventoryOptions == CantAddToDeck(5)`**；而**那个字段本地解包产物里根本没有**
        /// （卡资产包只有 Sprite/Audio/Texture2D）⇒ 我们改用 `Subtype`（唯一能拿到的等价信号）。
        ///
        /// 🔴 **这是【用户定的口径】，不是从原版推出来的** —— `GSC15`（= `Poisoned Supplies`，一张破坏卡）
        ///   **确实出现在原版 GSC 官方起手牌里**（判据全文 → `项目任务.md` §三 第 20 条）。
        ///   **别再拿那条来翻案**：用户 2026-09-27 二次确认「总之药剂/破坏卡不能加入卡组」。
        /// ⚠️ **防御卡不在此列** —— 它本来就该在卡池里（玩家要从那儿挑防御卡，用户同一天指出）。</summary>
        public static bool IsEffectOnly(string subtype)
        {
            switch (subtype)
            {
                case "Combat Elixir": case "Sabotage": case "Secret": return true;
                default: return false;
            }
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

            // ⑥b 🆕 2026-09-27：**效果生成的卡**（药剂/破坏/秘仪）不能是卡组的一部分（判据 = `IsEffectOnly`）
            foreach (var c in cards)
                if (IsEffectOnly(c.Subtype)) return DeckError.EffectOnlyCard;

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
            if (legendaries > LegendaryTotalLimit(skirmish)) return DeckError.CopyLimitExceeded;

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
            return e == DeckError.None ? "" : TermPrefix + e;
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
