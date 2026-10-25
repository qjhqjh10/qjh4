// Enchantment.cs — **附魔层**（原版 `EntityScript.activeEffects` + `activeEnchantments`）
//
// 🔴 **2026-10-19（第五十五会话 · 引擎线 `E3` · 账 `A1567`）新建。**
//    在这一层之前，全工程**没有「战将/单位身上挂着的附魔卡」这个概念**（`grep Enchant` 只有注释）
//    ⇒ 原版「取消攻击」的第 ① 条通道（扫攻方身上的附魔卡）**连载体都不存在**。
//
// ==================================================================
//  原版结构（第一权威 = `d:/2/tools/decomp_full/` + `d:/2/tools/il2cpp_out/dump.cs`）
// ==================================================================
//  · `EntityScript` 上**两条并列 list**：
//      `activeEffects      // +0x108`  `List<CardEffect>`         （每一份挂上来的效果记录）
//      `activeEnchantments // +0x110`  `List<ActiveEnchantment>`  （其中「附魔」那一族）
//    判据：`dump.cs` 的 `EntityScript` 段 `<activeEffects> // 0x108` / `<activeEnchantments> // 0x110`
//          （`dump.cs:22027` 附近；`EntityScript__get_activeEnchantments.c` 读的就是 `+0x110`）。
//  · `ActiveEnchantment = { RawCardScript enchantmentCard @+0x10; int enchantmentCardID @+0x18 }`
//    （`dump.cs:118926-118930`）。
//  · **挂**：`CardScript__AddEffect.c:826-828` —— `buffType(+0x38) ∈ {12 enchantment,
//    15 singleUseEnchantment, 16 choiceEnchantment}` ⇒ `CardScript__AddEnchantment.c`：
//      · `:25` `enchantmentCard(+0x88) == null` ⇒ `LogWarning` + **return**（不挂）；
//      · `:37` 惰性建 `+0x110` 那个 list；
//      · `:44` 新记录的 `+0x18` = **`bm.enchatmentsUsed(+0x4e4)` 的当时值**，随后
//        `BattleManager__UsedEnchantment`（`:48`，= `+0x4e4 += 1`）；
//      · `:51` 追加到 `+0x110`；`:63` **同时**把那条 `CardEffect` 追加到 `+0x108`；
//      · `:69` 末尾 `CardScript__ActivateTraitsOnSummonOrEnchantment(card, 0, 1, 0)`。
//  · **取**：`CardScript__RemoveEffect.c` —— `permanentBuff(+0x74)` 为真则**拒绝**（摘不掉）。
//  · **花**：`EntityScript__SpendEnchantment.c` —— 只在 `buffType ∈ {15, 16}` 时把
//    **`CardEffect.usedEnchantment(+0x92)`** 置 1（别的档什么都不做）；
//    `EntityScript__IsSpentEnchantment.c` 的语义 = 「这个附魔卡还**有没有没花掉的那一条**」。
//  · **结算**：`RawCardScript__TriggerAbilities.c:50-88` —— **派发触发时把这张牌
//    `+0x110` 里每个附魔的 `cardAbilities(+0x290)` 一起算进来**
//    （`GetAbilitiesForTrigger(thisCard, trigger, includeEnchantments: true)`）
//    ⇒ **附魔卡的能力与载体自己的触发走同一条派发**（本仓落点 = `RuleCore.FireTriggerAt`）。
//  · **开局**：`BattleManager__ShouldActivatInitialEnchantments.c` —— **只在战役关卡
//    `id ∈ {0x32(50), 0x50(80)}` 时为真**；真时 `BattleManager__SetupWarlordsLifeAndEnchantment.c`
//    做的是**改督军最大生命**（关卡数据），**不是「把附魔挂上去」**。
//    ⇒ 这一格在自由对战/遭遇战里**恒假**，而且我们的引擎**没有战役关卡数据**
//    ⇒ 落点是一个如实出声的空实现（见 `SetupInitial`）。
//  · **存续不按回合清** —— 靠 `CardEffect` 那五个 `until*` 位（`+0x32..+0x36`）+ `RemoveEffect`；
//    回合边界只清 `untilEndOfTurn` 那一档（`CardScript__ClearEndOfTurnEffects.c:23,27`）。
//    ⇒ 本层**不提供任何「回合结束清空」**；要摘一律显式走 `Remove`。
//
// ==================================================================
//  与我们既有模型的关系（**别把两条 list 搞混**）
// ==================================================================
//  · `CardInstance.HandEffects` **也是** 原版 `+0x108` 的对应物（手牌加成那一族，`A885`/`A886`）。
//    但它的语义是「**打出时兑现即摘**」（`RuleCore.ApplyHandBuffs` 里 `ClearHandEffect`），
//    与附魔「跟着牌在场、不摘」**正好相反** ⇒ **附魔的 `CardEffect` 绝不放进 `HandEffects`**。
//    所以 `CardInstance` 上另开 `ActiveEffects`（本文件）—— 两条 list 都对应原版 `+0x108`，
//    按**消费者**拆开：手牌那一族归 `HandEffects`，附魔那一族归 `ActiveEffects`。
//    这条拆分是**我们模型的选择**（原版是同一条 list），已如实记在这里。
//  · **存续宿主** = `CardInstance`（原版 `EntityScript` 的对应物：手牌 → 场上 → 弃牌堆是同一份）。
//    理由与 `CardInstance.HandEffects` 逐字相同（见那个字段的注释）。
//  · **可达性**：今天卡池 **0 张**卡会挂附魔（`cards_engine.json` 里没有附魔列，
//    全池 `desc`/`keywords` 搜 `enchant` / `secret order` 各 0 命中）
//    ⇒ 本层**不会改变任何现有对局**；它存在是为了「原版有、我们缺」的补课（铁律 11）。
//    ⚠️ 断言必须**人造夹具**才算数（否则恒真）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 原版 `CardEffect`（`EntityScript.activeEffects // +0x108` 里的一条）—— **本层只承载附魔那一族**。
    ///
    /// ⚠️ 手牌加成那一族**不在这里**：它们的存放处是 <see cref="CardInstance.HandEffects"/>
    ///    （理由见文件头「与我们既有模型的关系」）。
    /// </summary>
    public sealed class ActiveCardEffect
    {
        /// <summary>原版 `CardEffect.enchantment // +0x88` —— 这张附魔卡（`RawCardScript` 的对应物）。</summary>
        public CardDef EnchantmentCard;

        /// <summary>
        /// 原版 `CardEffect.buffType // +0x38`。本层只收三档：
        /// `12` = enchantment · `15` = singleUseEnchantment · `16` = choiceEnchantment
        /// （`CardEffect__IsEnchantment.c:9-12` 的判别式）。
        /// </summary>
        public int BuffType;

        /// <summary>原版 `CardEffect.usedEnchantment // +0x92` —— 这条**花掉**了没有（<see cref="Enchantments.Spend"/>）。</summary>
        public bool Used;

        /// <summary>原版 `CardEffect.permanentBuff // +0x74` —— 为真 ⇒ **摘不掉**（`RemoveEffect` 拒绝）。</summary>
        public bool Permanent;

        /// <summary>来源（日志用；= 施放/挂载它的那张卡名）。</summary>
        public string Source;

        /// <summary>施放者（原版 `CardEffect.enchantingCard // +0x18`）。挂载那一刻的施放单位，可为 null。</summary>
        public UnitState ActingUnit;

        /// <summary>指回 `+0x110` 里同一次挂载的那一条（两条 list 的连结点）。</summary>
        public ActiveEnchantment Enchantment;

        public override string ToString()
        {
            return (EnchantmentCard != null ? EnchantmentCard.Name : "?")
                 + "#" + (Enchantment != null ? Enchantment.EnchantmentCardId : 0)
                 + (Used ? "（已花）" : "");
        }
    }

    /// <summary>
    /// 原版 `ActiveEnchantment`（`dump.cs:118926-118930`，`EntityScript.activeEnchantments // +0x110` 的条目）：
    /// `{ RawCardScript enchantmentCard @+0x10; int enchantmentCardID @+0x18 }`。
    /// </summary>
    public sealed class ActiveEnchantment
    {
        /// <summary>原版 `+0x10 enchantmentCard` —— 附魔卡本身。</summary>
        public CardDef Card;

        /// <summary>
        /// 原版 `+0x18 enchantmentCardID`。⚠️ **它【不是】卡的 id** ——
        /// `CardScript__AddEnchantment.c:44` 写的是 **`bm.enchatmentsUsed(+0x4e4)` 的当时值**，
        /// 即「这是本局第几件附魔」（每次挂载自增，见 <see cref="BattleContext.EnchantmentsUsed"/>）。
        /// </summary>
        public int EnchantmentCardId;

        /// <summary>
        /// 附魔卡的 `spellType(+0x90)`（`EntityScript__HasSecretOrders.c:31` 读它：
        /// `enchantmentCard + 0x90 == 0x3c(60)` ⇒ 这一件是**密令**）。
        /// ⚠️ **`-1` = 未知**（我们的卡表 `cards_engine.json` **没有 `spellType` 列**，
        ///    见本文件末尾「没查清 / 还差什么」）—— ⛔ 别把 `-1` 当成某个具体值。
        /// </summary>
        public int SpellType = -1;

        /// <summary>指回 `+0x108` 里那一条 `CardEffect`。</summary>
        public ActiveCardEffect Effect;

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "#" + EnchantmentCardId;
        }
    }

    /// <summary>
    /// **附魔层**的全部入口 —— 判据只此一处（挂 / 取 / 花 / 结算 / 开局 五格）。
    /// 详见文件头。
    /// </summary>
    public static class Enchantments
    {
        /// <summary>`BuffType.enchantment = 12`（`CardEffect__IsEnchantment.c:9-12`）。</summary>
        public const int BuffTypeEnchantment = 12;
        /// <summary>`BuffType.singleUseEnchantment = 15`。</summary>
        public const int BuffTypeSingleUse = 15;
        /// <summary>`BuffType.choiceEnchantment = 16`。</summary>
        public const int BuffTypeChoice = 16;

        /// <summary>
        /// `SpellType.secretOrder = 0x3C(60)` —— 判据 = `EntityScript__HasSecretOrders.c:31`
        /// （`ActiveEnchantment.enchantmentCard(+0x10) → spellType(+0x90) == 0x3c`）
        /// 与 `AI__ValueOfEnchantments.c:65`（同一个判据）。
        /// </summary>
        public const int SpellTypeSecretOrder = 0x3C;

        /// <summary>🆕 2026-10-23（引擎线 `新账-97` ⑤）：`SpellType.quest = 0x78(120)` ——
        /// 判据 = `EntityScript__HasQuests.c`（`+0x110` 里 `enchantmentCard.spellType(+0x90) == 0x78`，
        /// 返回的是**件数**）。与 <see cref="SpellTypeSecretOrder"/> 同形。
        /// <para>⚠️ **当前快照 `spellType == 120` 的卡 0 张**（`cards_raw.json` 1,351 张全表扫过；
        /// 生成器 v19 的摘要会打出来）⇒ 这一格今天恒为 0，⛔ 不是「原版没有这条」。</para></summary>
        public const int SpellTypeQuest = 0x78;

        /// <summary>`CardEffect__IsEnchantment.c:9-12`：`buffType ∈ {12, 15, 16}`。</summary>
        public static bool IsEnchantmentBuffType(int buffType)
        {
            return buffType == BuffTypeEnchantment
                || buffType == BuffTypeSingleUse
                || buffType == BuffTypeChoice;
        }

        // ==================================================================
        //  ① 挂（原版 `CardScript.AddEffect` → `CardScript.AddEnchantment`）
        // ==================================================================

        /// <summary>
        /// **把一张附魔卡挂到 <paramref name="carrier"/> 身上**（原版 `CardScript__AddEnchantment.c` 的等价物）。
        ///
        /// 逐条照原版：
        ///   · `:25` `enchantmentCard == null` ⇒ `LogWarning` + **不挂**（返回 `null`）；
        ///   · `:37` 惰性建 `+0x110`（我们这边 list 在 `CardInstance` 构造时就建好了，语义相同）；
        ///   · `:44/:48` 新记录的 `ID` = **本局当前 `enchatmentsUsed` 的值**，然后计数器 **+1**；
        ///   · `:51` 追加到 `+0x110`；`:63` **同时**追加 `CardEffect` 到 `+0x108`。
        ///
        /// ⚠️ **原版 `:69` 的 `ActivateTraitsOnSummonOrEnchantment` 我们【没做】**（如实标着）：
        ///   它重算的是「被附魔之后才生效的静态 trait」（`canAct` / `ferocity` / `oath` …）。
        ///   我们没有任何「附魔卡 → 关键词」的数据来源（卡表没有附魔列）⇒ 这一步**没有输入**，
        ///   硬写一个空转会更糟（看着像做了）。等数据侧有了再补，判据原样留在这一行。
        /// </summary>
        /// <param name="spellType">附魔卡的 `spellType(+0x90)`；**不知道就留 `-1`**（⛔ 别猜）。
        /// 密令判定（`SpellTypeSecretOrder`）只认显式给对的这一格。</param>
        public static ActiveCardEffect Attach(BattleContext ctx, CardInstance carrier,
                                              CardDef enchantmentCard, int buffType = BuffTypeEnchantment,
                                              int spellType = -1, string source = null,
                                              UnitState acting = null)
        {
            if (enchantmentCard == null)
            {
                if (ctx != null)
                    ctx.Log("附魔层：要挂的附魔卡是 null —— **这一条没挂上**"
                          + "（原版 `CardScript__AddEnchantment.c:25` 同一条：`LogWarning` + return）");
                return null;
            }
            if (carrier == null)
            {
                if (ctx != null)
                    ctx.Log($"附魔层：「{enchantmentCard.Name}」没有载体（carrier 是 null）—— **这一条没挂上**");
                return null;
            }
            if (!IsEnchantmentBuffType(buffType))
            {
                // 原版 `CardScript__AddEffect.c:826-828` 只把这三档送进 `AddEnchantment`
                if (ctx != null)
                    ctx.Log($"附魔层：「{enchantmentCard.Name}」的 buffType = {buffType}，"
                          + "**不属于附魔那三档（12/15/16）** —— 这一条没挂上");
                return null;
            }

            // `:44` ID = 本局当前 `enchatmentsUsed`；`:48` 随后 +1（判据 = `BattleManager__UsedEnchantment.c`）
            int id = ctx != null ? ctx.EnchantmentsUsed : 0;
            if (ctx != null) ctx.EnchantmentsUsed = id + 1;

            // 🆕 2026-10-23（v19 · `新账-97` ⑤）：调用方**没给** `spellType` 时，用**附魔卡自己的**
            //   `CardDef.SpellType` —— 原版读的就是 `enchantmentCard.spellType(+0x90)`
            //   （`EntityScript__HasSecretOrders.c:31` / `HasQuests.c`），我们以前只有调用方手传这一条路
            //   ⇒ 绝大多数附魔的 `SpellType` 停在 `-1`，密令/任务两条判定永远「未知」。
            //   ⚠️ 卡表还是 `< v19`（没这一列）时 `CardDef.SpellType == -1` ⇒ 原样保持未知（⛔ 不猜 0）。
            int _st = spellType >= 0 ? spellType : enchantmentCard.SpellType;
            var ench = new ActiveEnchantment { Card = enchantmentCard, EnchantmentCardId = id, SpellType = _st };
            var eff = new ActiveCardEffect
            {
                EnchantmentCard = enchantmentCard,
                BuffType = buffType,
                Source = source,
                ActingUnit = acting,
                Enchantment = ench,
            };
            ench.Effect = eff;

            carrier.ActiveEnchantments.Add(ench);   // `+0x110`
            carrier.ActiveEffects.Add(eff);         // `+0x108`

            if (ctx != null)
            {
                string carrierName = carrier.Card != null ? carrier.Card.Name : "?";
                ctx.Log($"附魔层：把「{enchantmentCard.Name}」挂到「{carrierName}」身上"
                      + $"（#id {id}，buffType {buffType}"
                      + (_st >= 0 ? $"，spellType {_st}" : "，spellType 未知")
                      + (source != null ? $"，来源 {source}" : "") + "）");
                if (_st < 0)
                {
                    // 红线：不许静默。密令/任务判定只认 spellType ⇒ 未知时**当场说清后果**。
                    // 🆕 2026-10-23：v19 卡表起 `spellType` 已有这一列（`CardDef.SpellType`），
                    //   再走到这里只剩「这张附魔卡不在真值快照里」或「卡表还是旧的」两种原因。
                    ctx.Log("  ⚠️ spellType 未知 ⇒ **密令（SecretOrder）/ 任务（Quest）检测看不到这一件**"
                          + "（`CardDef.SpellType == -1`：卡表 < v19，或这张附魔卡不在真值快照里）");
                }
            }
            return eff;
        }

        // ==================================================================
        //  ② 取（原版 `CardScript.RemoveEffect` / `EntityScript.HasEnchantment`）
        // ==================================================================

        /// <summary>
        /// **摘掉一条附魔效果**（原版 `CardScript__RemoveEffect.c`）。
        /// 逐条照原版：**`permanentBuff(+0x74)` 为真 ⇒ 拒绝**（返回 `false`，不出声静默是不可能的——
        /// 我们在拒绝时**打日志**，因为「以为摘了、其实没摘」是典型静默失效）。
        /// </summary>
        public static bool Remove(BattleContext ctx, CardInstance carrier, ActiveCardEffect effect)
        {
            if (carrier == null || effect == null) return false;
            if (effect.Permanent)
            {
                if (ctx != null)
                    ctx.Log($"附魔层：「{NameOf(effect.EnchantmentCard)}」带 `permanentBuff` —— **摘不掉**"
                          + "（原版 `CardScript__RemoveEffect` 同一条闸）");
                return false;
            }
            carrier.ActiveEffects.Remove(effect);
            if (effect.Enchantment != null) carrier.ActiveEnchantments.Remove(effect.Enchantment);
            if (ctx != null)
                ctx.Log($"附魔层：从「{NameOf(carrier.Card)}」身上摘掉「{NameOf(effect.EnchantmentCard)}」");
            return true;
        }

        /// <summary>按**附魔卡**摘 —— 摘掉该卡在该载体上的**全部**挂载记录。返回摘掉几条。</summary>
        public static int RemoveByCard(BattleContext ctx, CardInstance carrier, CardDef enchantmentCard)
        {
            if (carrier == null || enchantmentCard == null) return 0;
            int n = 0;
            // 倒着走：`Remove` 会改 list
            for (int i = carrier.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var e = carrier.ActiveEffects[i];
                if (e == null || !ReferenceEquals(e.EnchantmentCard, enchantmentCard)) continue;
                if (Remove(ctx, carrier, e)) n++;
            }
            return n;
        }

        /// <summary>载体身上有没有这张附魔卡（原版 `EntityScript__HasEnchantment.c`）。</summary>
        public static bool Has(CardInstance carrier, CardDef enchantmentCard)
        {
            return Find(carrier, enchantmentCard) != null;
        }

        /// <summary>取第一条该附魔卡的挂载记录（没有返回 `null`）。</summary>
        public static ActiveCardEffect Find(CardInstance carrier, CardDef enchantmentCard)
        {
            if (carrier == null || enchantmentCard == null) return null;
            foreach (var e in carrier.ActiveEffects)
                if (e != null && ReferenceEquals(e.EnchantmentCard, enchantmentCard)) return e;
            return null;
        }

        /// <summary>载体身上挂着的全部附魔（`+0x110`，只读视图；没挂过返回空表）。</summary>
        public static IReadOnlyList<ActiveEnchantment> On(CardInstance carrier)
        {
            return carrier != null ? (IReadOnlyList<ActiveEnchantment>)carrier.ActiveEnchantments
                                   : EmptyEnchantments;
        }

        static readonly List<ActiveEnchantment> EmptyEnchantments = new List<ActiveEnchantment>();

        // ==================================================================
        //  ③ 花（原版 `EntityScript.SpendEnchantment` / `IsSpentEnchantment`）
        // ==================================================================

        /// <summary>
        /// **花掉这件附魔**（原版 `EntityScript__SpendEnchantment.c`）：
        /// **只有 `buffType ∈ {15 singleUse, 16 choice}` 才会置 `usedEnchantment(+0x92) = 1`**；
        /// 别的档（含 12 enchantment）**什么都不做**（原版 `:41` 那个 `!= 0xf` 的早退）。
        /// 返回真 = 这一次真的把某一条从「没花」改成了「花掉」。
        /// </summary>
        public static bool Spend(BattleContext ctx, CardInstance carrier, CardDef enchantmentCard)
        {
            if (carrier == null || enchantmentCard == null) return false;
            foreach (var e in carrier.ActiveEffects)
            {
                if (e == null || !ReferenceEquals(e.EnchantmentCard, enchantmentCard)) continue;
                if (e.BuffType != BuffTypeSingleUse && e.BuffType != BuffTypeChoice) continue;
                if (e.Used) continue;
                e.Used = true;
                if (ctx != null)
                    ctx.Log($"附魔层：「{NameOf(enchantmentCard)}」被**花掉**（原版 `SpendEnchantment`："
                          + "只置 `CardEffect.usedEnchantment(+0x92)`）");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 原版 `EntityScript__IsSpentEnchantment.c` 的语义：**这张附魔卡的每一条都花掉了**
        /// （找不到任何**没花**的那一条 ⇒ 真）。⚠️ 注意它**不是**「这一条花没花」——
        /// 那个直接读 `ActiveCardEffect.Used`。
        /// </summary>
        public static bool AllSpent(CardInstance carrier, CardDef enchantmentCard)
        {
            if (carrier == null || enchantmentCard == null) return false;
            foreach (var e in carrier.ActiveEffects)
            {
                if (e == null || !ReferenceEquals(e.EnchantmentCard, enchantmentCard)) continue;
                if (!e.Used) return false;
            }
            // ⚠️ **一条都找不到时原版返回【真】**（`IsSpentEnchantment.c` 循环走到底那条 `return 1`）——
            //    语义是「这张附魔卡身上**没有一条还没花**」。别按直觉改成 `false`
            //    （那是「这张卡不在他身上」，原版把这两种情况并成一档）。
            return true;
        }

        // ==================================================================
        //  ④ 结算（原版 `RawCardScript.TriggerAbilities(includeEnchantments: true)`）
        // ==================================================================

        /// <summary>
        /// **这个载体身上，哪些附魔对这一条触发关键词有话说** —— 原版
        /// `RawCardScript__TriggerAbilities.c:50-88` 把 `activeEnchantments(+0x110)` 里每个附魔的
        /// `cardAbilities(+0x290)` 一起算进来（`GetAbilitiesForTrigger(thisCard, trigger, true)`）。
        ///
        /// 判据落法：附魔卡的 `TriggerOps(keyword)`（卡面 `<关键词>: <正文>`）
        /// 或 `Effect(keyword)`（我们那台封闭文法）任一存在 ⇒ 算它一份。
        /// 调用点 = <see cref="RuleCore.FireTriggerAt"/>（**全仓所有触发唯一的出口**）。
        /// </summary>
        public static List<ActiveEnchantment> Matching(UnitState carrier, string keyword)
        {
            var r = new List<ActiveEnchantment>();
            if (carrier == null || carrier.Instance == null || string.IsNullOrEmpty(keyword)) return r;
            foreach (var e in carrier.Instance.ActiveEnchantments)
            {
                if (e == null || e.Card == null) continue;
                if (e.Card.TriggerOps(keyword) != null || e.Card.Effect(keyword) != null) r.Add(e);
            }
            return r;
        }

        /// <summary>
        /// 🆕 **2026-10-24（引擎线 `G2`）：`questTimes` 的**消费点** —— 原版
        /// `EntityScript__ReachedQuestCounter.c` 的等价物（逐句照抄，全文 15 行）。
        ///
        /// <para>**语义（原版原文）**：`(cardType == 0x28 Whispers) ∧ (spellType == 0x78 Quest)
        /// ∧ (1 &lt; questTimes)` 那一档 —— 每次这条附魔的 ability 要触发时：
        /// `carrier.questCounter(+0x270) += 1`；若仍 `< questTimes` ⇒ **`return 0`（不放行）**
        /// 并 `UpdateFigures`；否则 `return 1`（放行）。**计数器不复位** ⇒ 达到之后**每次照常放行**。
        /// ⚠️ 计数位在**载体**上（`param_3`），不在附魔条目上 ⇒ 同一张牌的两条任务附魔共用一个计数器。</para>
        ///
        /// <para>**本仓的取舍**：
        /// · 原版那个 `cardType == 40` 在 `CardDef` 里**没有对应字段**（`Type` 只有
        ///   `unit/tactic/hero/defence` 四档，Whispers 卡**不进卡池**）。但**能挂到附魔层的卡片
        ///   按定义就是原版的 Whispers**（`CardScript.AddEnchantment` 只由 `buffType 12/15/16` 触发，
        ///   见本文件头）⇒ 这里只用 `SpellType == 120` 判，不加一个永远为假的 `Type` 串判断。
        /// · `questTimes ∈ {-1, 0, 1}` ⇒ **原版那条闸不适用**（`1 < questTimes` 为假）⇒ 原样放行。
        ///   ⚠️ `-1` = 未知（旧表）⇒ **不猜**，按「不加闸」处理，并在日志里出声一次（下面那句）。</para>
        ///
        /// <para>**今天不可达**：`cards_raw.json` 1,351 张 `spellType == 120` **0 张**、
        /// `questTimes` 全 0 ⇒ 本方法在现池里**一次都不会改变行为**（那是数据事实，⛔ 不是省略）。</para>
        ///
        /// <param name="matched">已经按关键词筛过的附魔条目（<see cref="Matching"/> 的产出）。
        /// 返回**新 list**（不放行的被摘掉）；全部放行时原样返回实参。</param>
        /// </summary>
        public static List<ActiveEnchantment> FilterByQuestCounter(
                BattleContext ctx, UnitState carrier, List<ActiveEnchantment> matched, string keyword)
        {
            if (carrier == null || carrier.Instance == null || matched == null || matched.Count == 0)
                return matched;
            List<ActiveEnchantment> kept = null;
            for (int i = 0; i < matched.Count; i++)
            {
                var e = matched[i];
                int times = (e != null && e.Card != null) ? e.Card.QuestTimes : -1;
                int st = e != null && e.Card != null ? e.Card.SpellType : -1;
                // 只认「任务」那一种附魔 + `questTimes > 1`（原版那三条判据；未知一律不加闸）。
                if (st != SpellTypeQuest || times <= 1)
                {
                    if (kept != null) kept.Add(e);
                    continue;
                }
                carrier.Instance.QuestCounter = carrier.Instance.QuestCounter + 1;
                int counter = carrier.Instance.QuestCounter;
                if (counter < times)
                {
                    if (ctx != null)
                        ctx.Log($"附魔层：「{e.Card.Name}」的 `questTimes` 还没到 —— 计数 {counter} / {times}"
                              + $" ⇒ 这一拍（`{keyword}`）**不结算**"
                              + "（原版 `EntityScript__ReachedQuestCounter.c:11-14` 返回 0）");
                    if (kept == null)
                    {
                        kept = new List<ActiveEnchantment>(matched.Count);
                        for (int j = 0; j < i; j++) kept.Add(matched[j]);
                    }
                    continue;
                }
                if (counter == times && ctx != null)
                    ctx.Log($"附魔层：「{e.Card.Name}」的 `questTimes` 达成（{times} 次）"
                          + " ⇒ 从此每一拍都照常结算（原版计数器不复位）");
                if (kept != null) kept.Add(e);
            }
            return kept ?? matched;
        }

        /// <summary>
        /// 这个载体**身上有没有任何附魔** —— `FireTriggerAt` / `FireTriggerAlways` 的**廉价前置**
        /// （今天 0 张卡有附魔 ⇒ 这一句挡住后面那次 list 分配与逐件扫）。
        /// </summary>
        public static bool HasAny(UnitState carrier)
        {
            return carrier != null && carrier.Instance != null
                && carrier.Instance.ActiveEnchantments.Count > 0;
        }

        // ==================================================================
        //  ⑤ 开局（原版 `ShouldActivatInitialEnchantments` / `SetupWarlordsLifeAndEnchantment`）
        // ==================================================================

        /// <summary>
        /// **开局附魔** —— 原版这一格**不是「给谁挂附魔」**：
        ///   · `BattleManager__ShouldActivatInitialEnchantments.c` 只在**战役关卡 id ∈ {50, 80}** 时为真；
        ///   · 为真时 `BattleManager__SetupWarlordsLifeAndEnchantment.c` 做的是**改督军最大生命**
        ///     （读关卡数据的 `+0xa4..+0xb4` 五个整数），**不碰 `+0x110`**。
        /// ⇒ 自由对战 / 遭遇战**恒假**；我们的引擎**没有战役关卡数据**
        ///   （`GameplayVariables` 里也没有这几个字段）⇒ 这里**如实地说「没有输入」**，
        ///   ⛔ 不拿任何默认值顶替（那会把「没数据」静默变成「有数据」）。
        /// 返回真 = 这一次真的挂了附魔（今天恒 `false`）。
        /// </summary>
        public static bool SetupInitial(BattleContext ctx)
        {
            // 只在有东西可挂时才出声：今天没有战役关卡数据 ⇒ **一声不响**（不污染任何现有日志/指纹）。
            return false;
        }

        // ==================================================================
        //  密令（`A1819` / `A1985`）—— 原版 `EntityScript.HasSecretOrders` / `HasSecretOrder`
        // ==================================================================

        /// <summary>
        /// 原版 `EntityScript__HasSecretOrders.c`：数 `+0x110` 里
        /// **`enchantmentCard.spellType(+0x90) == 0x3c(60)`** 的件数。
        /// ⚠️ `SpellType == -1`（未知）的**不计入**（⛔ 不猜）；那些件数用
        /// <see cref="CountUnknownSpellType"/> 单独查。
        /// </summary>
        public static int CountSecretOrders(CardInstance carrier)
        {
            int n = 0;
            if (carrier == null) return 0;
            foreach (var e in carrier.ActiveEnchantments)
                if (e != null && e.SpellType == SpellTypeSecretOrder) n++;
            return n;
        }

        /// <summary>🆕 2026-10-23（`新账-97` ⑤）：原版 `EntityScript__HasQuests.c` —— 数 `+0x110` 里
        /// **`enchantmentCard.spellType(+0x90) == 0x78(120)`** 的件数（返回件数，不是 bool）。
        /// ⚠️ `SpellType == -1`（未知）不计入（⛔ 不猜）—— 那种件数用
        /// <see cref="CountUnknownSpellType"/> 单独查（`ConditionEval` 的 250 靠它决定「判不了」）。</summary>
        public static int CountQuests(CardInstance carrier)
        {
            int n = 0;
            if (carrier == null) return 0;
            foreach (var e in carrier.ActiveEnchantments)
                if (e != null && e.SpellType == SpellTypeQuest) n++;
            return n;
        }

        /// <summary>载体身上 `spellType` 未知的附魔件数（诚实计数：这些**可能**是密令，但我们判不了）。</summary>
        public static int CountUnknownSpellType(CardInstance carrier)
        {
            int n = 0;
            if (carrier == null) return 0;
            foreach (var e in carrier.ActiveEnchantments)
                if (e != null && e.SpellType < 0) n++;
            return n;
        }

        /// <summary>载体身上**全部**密令（按挂载顺序）。</summary>
        public static List<ActiveEnchantment> SecretOrders(CardInstance carrier)
        {
            var r = new List<ActiveEnchantment>();
            if (carrier == null) return r;
            foreach (var e in carrier.ActiveEnchantments)
                if (e != null && e.SpellType == SpellTypeSecretOrder) r.Add(e);
            return r;
        }

        /// <summary>
        /// 原版 `EntityScript__HasSecretOrder.c`：在 `+0x108` 里找
        /// **`IsEnchantment` 且 `enchantmentCard(+0x88)` 等于给定那张**的那条 `CardEffect`。
        /// 返回 `null` = 没有。（密令的「揭示」那一步拿的就是它 —— 看它的 `buffType` 决定要不要摘。）
        /// </summary>
        public static ActiveCardEffect FindSecretOrder(CardInstance carrier, CardDef only)
        {
            if (carrier == null || only == null) return null;
            foreach (var e in carrier.ActiveEffects)
            {
                if (e == null || !ReferenceEquals(e.EnchantmentCard, only)) continue;
                if (!IsEnchantmentBuffType(e.BuffType)) continue;
                return e;
            }
            return null;
        }

        // ==================================================================
        //  「取消攻击」第 ① 条通道的载体（`A1258`）
        // ==================================================================

        /// <summary>
        /// **攻方身上有没有「即将攻击时取消这一记」的附魔** —— 原版
        /// `CardScript__HasCancelAttackEffect.c`：
        ///   · `:18` 取 `activeEnchantments(+0x110)`；
        ///   · `:31` 逐件 `RawCardScript.HasAbilityType(enchantmentCard, 0x118 /* AboutToAttack = 280 */)`；
        ///   · `:36-50` 在该附魔的 `cardAbilities(+0x290)` 里逐条
        ///     `CardAbility.CanTriggerAbility(ability, 0x118, bm, 攻方, 攻方, 原目标)` ⇒ 命中返回真。
        ///
        /// 我们的落法：附魔卡有 `abouttoattack` 的触发正文（`TriggerOps` / `Effect`）⇒ 视为
        /// 「有一条 trigger == 0x118 的 ability」。⚠️ **`CanTriggerAbility` 的那几条 criteria
        /// （每回合一次 / 目标条件…）我们没有等价物** —— 如实标着：本层只判「在不在」。
        /// 返回 `null` = 这一记没有附魔要取消它。
        /// </summary>
        public static ActiveEnchantment FindCancelAttack(UnitState attacker)
        {
            if (attacker == null || attacker.Instance == null) return null;
            foreach (var e in attacker.Instance.ActiveEnchantments)
            {
                if (e == null || e.Card == null) continue;
                if (e.Card.TriggerOps(KeywordTable.AboutToAttack) != null
                    || e.Card.Effect(KeywordTable.AboutToAttack) != null)
                    return e;
            }
            return null;
        }

        // ==================================================================
        //  ⑥ 祈祷附魔（`X20` · 2026-10-25）—— 原版隐藏卡 `Pray Enchantment`
        // ==================================================================
        // 🔴 **为什么这一段在附魔层**：原版这条语义**不在 C# 里，在卡数据里** ——
        //   23 张带 `pray` 特性的卡在 `cardAbilities[trigger = 655 (AbilityTrigger.Pray)]
        //   .abilityLogic[0].buffEffect` 挂 `{buffType: 15 singleUseEnchantment, enchantment → PE}`
        //   ⇒ **祈祷那一刻把这张附魔挂到自己身上**（`CardScript__AddEffect.c:826-828` →
        //   `CardScript__AddEnchantment.c`，本文件 ①）。
        //   `X13` 的旧版是「引擎内 bool」（`UnitState.PrayEnchantment`）；`X20` 起改成**真的挂这张卡**，
        //   bool 已删除（⛔ 别再长回来 —— 两处写同一条规则 = 迟早不一致）。
        //
        //   该附魔卡自己的两条能力（`cards_raw.json` 现读，`path_id 1528516720421242597`）：
        //     · ability[0] `trigger 60 (UnitDamaged)` → `chosenAbility 340 removeThisEnchantment`
        //       = **受伤即撤**（⚠️ `CardScript__ResolveDamageDealt.c:25-28` 对 `damage == 0` 直接 `return`
        //       ⇒ **盾/甲挡掉的 0 伤不算**；落点 = `RuleCore.ApplyDamage` 那一闸 ＋ 疲劳那一支）；
        //     · ability[1] `trigger 31 (TurnStartPlayer)` → `chosenAbility 60 buffMana`
        //       `{manaAmount:1, faithMana:1}` → 再 `340 removeThisEnchantment` = **自己回合开始 +1 信仰、一次性**。
        //
        //   ⚠️ **`buffType == 15` 的映射沿用本文件 `BuffTypeSingleUse` 那张既有表**（判据
        //   `CardEffect__IsEnchantment.c:9-12` 只给了「∈ {12,15,16}」，**`BuffType` 枚举本体没有在
        //   `dump.cs` 里逐值核到**）—— 这里如实标着：**沿用既有表、未逐值核**。
        //
        //   ⚠️ **卡从 `ctx.CardPool` 取**（`Core` 不能引用 `Data/CardDatabase`：`ruleprobe` 只编 `Core/*.cs`）。
        //   生产侧 `BattleDriver` 把 `CardDatabase.Load()` 传进 `NewBattle(cardPool:)`；自检夹具要自己传
        //   （`RuleEngineTest_EngineX13` 传真卡池 ⇒ 断言钉的是**卡数据**，不是引擎里那面 flag）。

        /// <summary>原版 `Pray Enchantment` 的 `uniqueId`（= 我们卡池的 `id`）。
        /// 判据 = `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `m_Name = Z-NC Sororitas-Pray_enchantment`。</summary>
        public const string PrayEnchantmentCardId = "9218576fd3e26554db5b4c239a36dbc7";

        /// <summary>显示名（`cards_raw.json` 的 `cardName`）。⚠️ 它是**兜底判据**（按 id 找不到时才用它），
        /// 而且必须同时是 <see cref="CardDef.IsEnchantment"/> —— ⛔ 别只按名字匹配普通卡。</summary>
        public const string PrayEnchantmentName = "Pray Enchantment";

        /// <summary>这张附魔在本局卡池里（找不到返回 `null`）。**只按 id 认**，id 不中才退「名字 + `enchantment` 型」。</summary>
        public static CardDef FindPrayCard(BattleContext ctx)
        {
            if (ctx == null || ctx.CardPool == null) return null;
            CardDef byName = null;
            for (int i = 0; i < ctx.CardPool.Count; i++)
            {
                var c = ctx.CardPool[i];
                if (c == null) continue;
                if (c.Id == PrayEnchantmentCardId) return c;
                if (byName == null && c.IsEnchantment && c.Name == PrayEnchantmentName) byName = c;
            }
            return byName;
        }

        /// <summary>载体身上挂着的那一条祈祷附魔（没有返回 `null`）。</summary>
        public static ActiveEnchantment FindPray(UnitState carrier)
        {
            if (carrier == null || carrier.Instance == null) return null;
            foreach (var e in carrier.Instance.ActiveEnchantments)
                if (e != null && e.Card != null && e.Card.Id == PrayEnchantmentCardId) return e;
            return null;
        }

        /// <summary>载体身上有没有祈祷附魔。</summary>
        public static bool HasPray(UnitState carrier)
        {
            return FindPray(carrier) != null;
        }

        /// <summary>
        /// **祈祷那一刻把「祈祷附魔」挂到 <paramref name="carrier"/> 身上**（原版
        /// `cardAbilities[trigger = 655].abilityLogic[0].buffEffect{buffType 15, enchantment → PE}`）。
        ///
        /// <para>**幂等**：已经挂着就返回那一条、不重复挂 —— 与 `X13` 的 bool 语义一致
        /// （bool 置两遍还是 `true`；原版理论上会追加第二条，但祈祷一回合一次、且它在**自己下个回合开始**
        /// 就摘掉 ⇒ 实战中到不了「同一个人身上两条」那一格）。</para>
        /// <para>⚠️ **卡池里没有这张卡 ⇒ 不挂、并且出声**（⛔ 不拿引擎内 flag 顶替 —— 那会静默回到旧口径）。
        /// 生产侧卡池恒有它；自检夹具要自己传（见文件尾 ⑥ 段顶上的说明）。</para>
        /// </summary>
        public static ActiveEnchantment AttachPray(BattleContext ctx, UnitState carrier)
        {
            if (carrier == null) return null;
            var existing = FindPray(carrier);
            if (existing != null) return existing;
            if (carrier.Instance == null)
            {
                if (ctx != null) ctx.Log("附魔层：「祈祷附魔」没有载体实例（`UnitState.Instance == null`）—— 这一条没挂上");
                return null;
            }
            var card = FindPrayCard(ctx);
            if (card == null)
            {
                if (ctx != null)
                    ctx.Log("附魔层：⚠️ 本局卡池里找不到「" + PrayEnchantmentName + "」（`id "
                          + PrayEnchantmentCardId + "`）—— **祈祷附魔没挂上**，"
                          + "那一份「下回合开始 +1 信仰」因此不会发生（⛔ 不静默退回旧 flag；"
                          + "自检请给 `NewBattle(cardPool:)` 传真卡池）");
                return null;
            }
            var eff = Attach(ctx, carrier.Instance, card, BuffTypeSingleUse,
                             source: carrier.Card != null ? carrier.Card.Name : null, acting: carrier);
            return eff != null ? eff.Enchantment : null;
        }

        /// <summary>**摘掉祈祷附魔**（受伤 / 疲劳 / 回合开始结算三个落点共用）。返回真 = 真摘掉了。</summary>
        public static bool RemovePray(BattleContext ctx, UnitState carrier)
        {
            var e = FindPray(carrier);
            if (e == null) return false;
            if (e.Effect != null) return Remove(ctx, carrier.Instance, e.Effect);
            // 兜底（正常挂载路径不会走到）：`Effect` 丢了也要把 `+0x110` 那条摘掉，⛔ 不留半条。
            carrier.Instance.ActiveEnchantments.Remove(e);
            return true;
        }

        static string NameOf(CardDef c) { return c != null ? c.Name : "?"; }
    }
}
