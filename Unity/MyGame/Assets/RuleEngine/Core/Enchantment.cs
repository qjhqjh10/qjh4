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
//  · **可达性**：⚠️ **2026-10-27 就地订正（铁律 5）** —— 下面这段「0 张卡会挂附魔」**已过期**：
//    ① v30（2026-10-25 `X20`）起池里有**隐藏附魔模板** `Pray Enchantment`（`type = "enchantment"`）；
//    ② v33（2026-10-27 `E7ENG`，账 532）起池里多了 `enchantTrait` 列 ⇒ 23 张 SOR 卡祈祷时真会挂上它
//       （`buffEffect{buffType 15, trait 1185 pray, enchantment → PE}`）。
//    ⇒ 「本层不会改变任何现有对局」**只对自由对战里的非祈祷路径成立**；`HasPray` / `AttachPray` 那条链
//    在池内 23 张 SOR 卡上是**活的**。断言仍然必须**人造夹具**（那 23 张的祈祷时机要自己摆）。
//    原文（2026-10-19 语境）：当时卡池 0 张会挂附魔（`cards_engine.json` 没有附魔列，
//    全池 `desc`/`keywords` 搜 `enchant` / `secret order` 各 0 命中）。
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

        /// <summary>
        /// 🆕 **2026-10-27（批次 9 · 执行代理 `E7ENG` · 账 532 的 1a）：原版 `CardEffect.trait`（`+0x3C`）**
        /// —— 这条附魔**效果**自带的 `DefinedTrait` **关键词名**（`"pray"` / `"vulnerable"`）。
        ///
        /// <para>**原版唯一消费点** = `BattleManager__RemoveDestroyableAnims.c`（内联进
        /// `ResolveAction` case 0x10）：`buffType ∈ {15,16}` 时拿本格当键，去
        /// `BattleCardUI.TraitFrameEffectsPlay(+0x2d0)` / `TraitFrameEffectsTrigger(+0x2d8)`
        /// 两本字典里找那件状态框、`DoDestroy()` 并摘字典。**不参与任何数值结算。**</para>
        ///
        /// <para>⚠️ **`null` = 未知**（卡表 &lt; v33 没这一列 / 调用方没给）· **`""` = 明确没有**
        /// —— ⛔ 别把两者混成一种（`CardDef.EnchantTrait` 的 doc 同一条契约）。</para>
        /// <para>数据源 = `cards_engine.json` 的 `enchantTrait` 列 → `CardDef.EnchantTrait`
        /// （由 <see cref="Enchantments.Attach"/> 按「载体卡 → 附魔卡」的次序取）。</para>
        /// </summary>
        public string Trait;
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
        /// ✅ 2026-10-27 订正：`cards_engine.json` **已有 `spellType` 列**（1128/1128）、`CardDef.SpellType` 有值 —— 原文「卡表没有这一列」写于该列落地之前，已过期。
        ///    ⛔ 仍成立：`-1` 是「未知」兜底值，别把它当成某个具体值。
        /// </summary>
        public int SpellType = -1;

        /// <summary>指回 `+0x108` 里那一条 `CardEffect`。</summary>
        public ActiveCardEffect Effect;

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "#" + EnchantmentCardId;
        }

        /// <summary>
        /// 原版 `RawCardScript.whileInPlayAnim // +0x288`（`dump.cs:23338`）——
        /// `EntityScript__HasEnchantmentWhileInPlay()` 唯一读的那一格（账 1044）。
        ///
        /// <para>🔶 自己设计（非原版的数据形状）：原版这个开关在**附魔卡自己**身上（`RawCardScript`），
        ///   而我们的卡表（`cards_engine.json` / `CardDef`）**还没有这一列** ⇒ 先在**挂载记录**上留一格三态，
        ///   由 <see cref="Enchantments.Attach"/> 的新形参 `whileInPlayAnim` 填。
        ///   P-07 把 `CardDef.WhileInPlayAnim` 落进卡表之后，把 `Attach` 的缺省值改成读它即可，
        ///   消费端 <see cref="Enchantments.HasEnchantmentWhileInPlay"/> 不用动。</para>
        /// <para>取值：`1` = 真 · `0` = 假 · `-1` = **未知**（数据列还没接，见
        ///   <see cref="Enchantments.UnknownWhileInPlayAnim"/>）。</para>
        /// <para>📌 数据在哪（给 P-07 的判据）：`资料/普查产出_1018/allcards_1350/cards_raw.json` 的
        ///   **`full_raw.whileInPlayAnim`**（1,351 张各一条；`true` = **41** 张，其中**附魔卡 3 张**：
        ///   `eab54e4c…` T16 Sautekh-Undying Legions · `a5960475…` T18 Leviathan-Hive Fleet Leviathan ·
        ///   `7c06b811…` Z-SW-Everstorm）。⚠️ 它在 `full_raw` 里、不在解析视图那一层。</para>
        /// </summary>
        public int WhileInPlayAnim = Enchantments.UnknownWhileInPlayAnim;
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
        /// 🔴 **2026-10-26（第八十四会话 · `W13EN` · 账 687）：原版 `:91` 的
        ///   `ActivateTraitsOnSummonOrEnchantment(carrier, onSummon: false)` **已补**（= 本方法末尾那一句）。
        ///   它重算的是**载体自己的** `CurrentTraits` 对「能动 / 能攻击」的影响（`fast`/`flank`/`ferocity`/`oath`），
        ///   **不把附魔卡的 trait 并进载体**（旧记「缺『附魔卡 → 关键词』数据通路」是读错了那一跳，订正 → 报告 §五）。新增形参 `whileInPlayAnim` 见 `ActiveEnchantment.WhileInPlayAnim` 的 doc（等 P-07 落列后改缺省读 `CardDef`）。 🆕 2026-10-28（`A4` · 判据 `判据/B91_P3_战斗数据列缺口.md` §二·7）：新增形参 `permanent`（原版 `CardEffect.permanentBuff +0x74`）写进 `ActiveCardEffect.Permanent` —— 「摘不掉」那条闸本来就在 `Remove`（`:329`），以前**恒 false = 恒可摘**。⛔ 默认 `false`（共用件，别改缺省）。
        /// </summary>
        /// <param name="spellType">附魔卡的 `spellType(+0x90)`；**不知道就留 `-1`**（⛔ 别猜）。
        /// 密令判定（`SpellTypeSecretOrder`）只认显式给对的这一格。</param>
        public static ActiveCardEffect Attach(BattleContext ctx, CardInstance carrier,
                                              CardDef enchantmentCard, int buffType = BuffTypeEnchantment,
                                              int spellType = -1, string source = null,
                                              UnitState acting = null, int whileInPlayAnim = UnknownWhileInPlayAnim,
                                              string trait = null, bool permanent = false)
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
            // 🆕 2026-10-27（批次 9 · `E7ENG` · 账 532 的 1a）：`CardEffect.trait(+0x3C)` 的数据源。
            //   ① 调用方显式给的优先；② 否则读**载体卡**的 `CardDef.EnchantTrait`
            //      （原版这个值写在「挂它的那条 buff」上，而那条 buff 在**载体卡**的 ability 里 ——
            //       `Pray Enchantment` 模板自己 `cardTraits = []`，⛔ 别只读模板）；
            //   ③ 模板自己有值时兜底（将来真有「模板自带 trait」的附魔）。
            //   `null` = 未知（卡表 < v33 / 这张卡不在真值快照里）· `""` = 明确没有。
            string _tr = trait;
            if (_tr == null && carrier != null && carrier.Card != null) _tr = carrier.Card.EnchantTrait;
            if (_tr == null && enchantmentCard != null) _tr = enchantmentCard.EnchantTrait;
            var ench = new ActiveEnchantment { Card = enchantmentCard, EnchantmentCardId = id, SpellType = _st, WhileInPlayAnim = EnchantmentWhileInPlayAnimOf(enchantmentCard, whileInPlayAnim) };   // 🆕 B89/E6d：调用方没给 ⇒ 读**附魔卡自己**的 `CardDef.WhileInPlayAnim`（原版 `EntityScript__HasEnchantmentWhileInPlay` 读的就是它；P-07 待办收口）
            var eff = new ActiveCardEffect
            {
                EnchantmentCard = enchantmentCard,
                BuffType = buffType,
                Source = source,
                ActingUnit = acting,
                Enchantment = ench,
                Trait = _tr, Permanent = permanent,
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
                if (_tr == null)
                {
                    // 红线：不许静默。原版 `RemoveDestroyableAnims` 靠 `cardEffect.trait` 找两件状态框
                    // ⇒ 未知时**摘效果那一下状态框不会被销毁**，后果当场说清（判据 → `Trait` 的 doc）。
                    ctx.Log("  ⚠️ 这条附魔的 `trait` **未知**（`CardDef.EnchantTrait == null`：卡表 < v33，"
                          + "或这张卡不在真值快照里）⇒ 摘效果时状态框不会被销毁"
                          + "（原版 `BattleManager__RemoveDestroyableAnims` 靠它当键）");
                }
            }
            ActivateTraitsAfterEnchant(ctx, carrier); return eff;   // 原版 `:91`（判据/取舍见方法头 2026-10-26 那一段与 `ActivateTraitsAfterEnchant` 的 doc）
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
            // 🆕 **2026-10-26（`W14EC` · 排队摘之后才需要）：空过要【如实出声】，⛔ 别打一句假的「摘掉」并返回真。**
            //   排队的 `removeEffect` 可能晚于「载体变残骸时整表清过」（`EffectResolver.cs` 的残骸支对
            //   `inst.ActiveEffects` / `inst.ActiveEnchantments` 两条 `Clear()`）⇒ 出队时这条记录已经不在。
            bool wasThere = carrier.ActiveEffects.Remove(effect);
            if (effect.Enchantment != null) carrier.ActiveEnchantments.Remove(effect.Enchantment);
            if (ctx != null)
            {
                if (wasThere)
                    ctx.Log($"附魔层：从「{NameOf(carrier.Card)}」身上摘掉「{NameOf(effect.EnchantmentCard)}」");
                else
                    ctx.Log($"附魔层：「{NameOf(effect.EnchantmentCard)}」本来就不在「{NameOf(carrier.Card)}」身上"
                          + "（已摘 / 载体变残骸时整表清过）—— 这条 `removeEffect` **空过**");
            }
            // 🆕 **2026-10-27（批次 9 · `E7ENG` · 账 532 的 1b）：摘成功 ⇒ 发一条
            //   `EvtKind.EnchantmentRemoved`**（本类是四个生产落点的**唯一汇合处**：
            //   `EnqueueRemove` 的出队体 / `RemovePray` / `RemoveByCard`（今天 0 生产调用点）/ 直调）。
            //   字段约定 → `EvtKind.EnchantmentRemoved` 的 doc（`CardId` = trait 名 · `Amount` = buffType）。
            //   ⚠️ **`EffectResolver` 残骸支那两条整表 `Clear()` 不走这里** —— 原版那条也不走
            //   `ResolveRemoveEffect`（判据 → `判据/T4_532与PremiumCampaign_判据.md` §1b 表），⛔ 不该发。
            //   ⚠️ 不在棋盘上（挂到手里 / 墓地）⇒ `Slot = -1`（原版那两件状态框本来也不在屏上）。
            if (wasThere && ctx != null)
            {
                int _o, _s;
                BoardOwnerSlotOf(ctx, carrier, out _o, out _s);
                ctx.Emit(EvtKind.EnchantmentRemoved, _o, _s,
                         cardId: effect.Trait,
                         amount: effect.BuffType,
                         effect: NameOf(effect.EnchantmentCard),
                         instanceId: carrier.Id);
            }
            return wasThere;
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
        /// · 🔴 2026-10-27（W68ENG3 · G37）**就地订正**：原来写「原版 `cardType==40` 在 `CardDef` 里没有对应字段、
        ///   只用 `SpellType` 判」—— 那句已过期：`CardDef.cs` 有 `CardTypeWhispers = 40`（`CardDef.CardType` 属性，
        ///   原版 `RawCardScript.cardType@+0x94`）。原版**三条闸** `cardType==40 ∧ spellType==120 ∧ questTimes>1`
        ///   现在照抄（见下面循环体那道判据），三条全过才动 `questCounter`。
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
                if (e == null || e.Card == null || e.Card.CardType != CardDef.CardTypeWhispers || st != SpellTypeQuest || times <= 1)
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
        /// 原版 `EntityScript__HasSecretOrder.c` / `EntityScript.cs:1977-2035`（逐句）：
        ///   ① `n = HasSecretOrders()` —— 数 **`activeEnchantments(+0x110)`** 里 `spellType == SecretOrder` 的件数；
        ///   ② **只有 `n &gt; 0` 才进循环**；
        ///   ③ 遍历 **`activeEffects(+0x108)`**，只取 `IsEnchantment()` 的；
        ///   ④ 且 `enchantmentCard(+0x88)` 等于给定那张 ⇒ 返回该 effect；⑤ 否则 `null`。
        ///
        /// 🔴 **原版这两个列表不是一条**：闸看 `activeEnchantments`、体查 `activeEffects`
        /// （`activeEffects` 里同时装着非附魔族的记录，所以只看它会把「不是密令」的判成密令）。
        /// 🆕 **2026-10-26（第八十四会话 · `W13EN` · 账 1047）：② 那道闸补上了** ——
        /// 改前「身上没有任何 SecretOrder 附魔、但 `ActiveEffects` 里恰有一条同卡的附魔型效果」时
        /// 我们会返回它、原版返回 `null`。
        /// </summary>
        public static ActiveCardEffect FindSecretOrder(CardInstance carrier, CardDef only)
        {
            if (carrier == null || only == null) return null;
            if (CountSecretOrders(carrier) <= 0) return null;   // 原版 `:1977-1981` 的 ②：闸看 `activeEnchantments`（⛔ 别改成数 `activeEffects` —— 那正是原版的坑）
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
            return byName != null ? byName : ctx.FindEnchantmentTemplate(PrayEnchantmentCardId);   // 🆕 B89/E6d：卡池找不到 ⇒ 兜底查**第二张表**（`BattleContext.FindEnchantmentTemplate` = 先池后表）。⛔ 别再写第二个查找；Core 不引用 Data ⇒ 走那个钩子
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

        /// <summary>**同步摘掉祈祷附魔**（= `removeEffect(16)` 的【出队体】/ 直调口）。返回真 = 真摘掉了。
        /// ✅ 2026-10-26（`W14EC`）：三个生产落点已改走**排队**版 <see cref="EnqueueRemovePray"/>
        /// （原版 340 那支是 `AddRemoveEnchantment` → `AddAutoActionToQueue`）。</summary>
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

        // ================================================================================================
        //  🆕 2026-10-26（第八十四会话 · `W13EN` · 账 1044 / 763 / 770 / 481·532 / 687）
        //  —— **本段全部追加在类尾**（⛔ 别插到中间的成员之间：`账/*.tsv` 按 `文件:行号` 引，插一行就漂）
        // ================================================================================================

        /// <summary>`whileInPlayAnim` 三态里的「未知」（见 <see cref="ActiveEnchantment.WhileInPlayAnim"/>）。</summary>
        public const int UnknownWhileInPlayAnim = -1;

        /// <summary>
        /// 原版 `EntityScript.HasEnchantmentWhileInPlay()`（`out_cpp2il_dev_ilspy/EntityScript.cs:1843-1874`，
        /// 逐句核过）—— 判的是 **附魔卡自己的 `whileInPlayAnim(+0x288)` 开关**，
        /// **不是**「身上有没有附魔」（那个是 <see cref="HasAny"/>，语义完全不同；账 1044 的差异列就是这条）。
        /// <code>
        ///   foreach (ActiveEnchantment e in activeEnchantments(+0x110))
        ///       if (e != null &amp;&amp; e.enchantmentCard != null &amp;&amp; e.enchantmentCard.whileInPlayAnim) return true;
        ///   return false;
        /// </code>
        /// <para>⚠️ 原版对 `e == null` / `enchantmentCard == null` 那一格**会 NRE**（ILSpy 的
        ///   `NullReferenceException` 桩）⇒ 我们不复制崩溃语义：跳过，件数由
        ///   <see cref="CountUnknownWhileInPlayAnim"/> 单独查。</para>
        /// <para>⚠️ 三态：`WhileInPlayAnim &lt; 0`（数据列还没接）**按 false 算** ——
        ///   ⛔ 不静默当作「原版就是 false」，要查判不了几件请用 <see cref="CountUnknownWhileInPlayAnim"/>。</para>
        /// <para>📌 今天不可观察：本仓**没有任何**附魔卡的 `whileInPlayAnim` 被填过（`Attach` 的缺省 = 未知）；
        ///   数据已在 `cards_raw.json` 的 `full_raw.whileInPlayAnim`（3 张附魔卡为真）⇒ 等 P-07 落列。</para>
        /// </summary>
        public static bool HasEnchantmentWhileInPlay(CardInstance carrier)
        {
            if (carrier == null) return false;
            foreach (var e in carrier.ActiveEnchantments)
                if (e != null && e.WhileInPlayAnim == 1) return true;
            return false;
        }

        /// <summary>载体身上**判不了** `whileInPlayAnim` 的附魔件数（诚实计数：数据列没接时 = 全部件数）。</summary>
        public static int CountUnknownWhileInPlayAnim(CardInstance carrier)
        {
            int n = 0;
            if (carrier == null) return 0;
            foreach (var e in carrier.ActiveEnchantments)
                if (e != null && e.WhileInPlayAnim < 0) n++;
            return n;
        }

        /// <summary>
        /// 原版 `CardScript.GetQuestTimes()`（`decomp_full/CardScript__GetQuestTimes.c:1-45` ·
        /// `out_cpp2il_dev_ilspy/CardScript.cs:10604-10654`，亲读）：
        /// 遍历 `activeEnchantments(+0x110)`，**返回第一条 `enchantmentCard.questTimes(+0x7c) &gt; 0` 的那个值**；
        /// 一条都没有 ⇒ `0`。
        ///
        /// <para>⚠️ 与 <see cref="FilterByQuestCounter"/> **不是同一件事**：那个是 `ReachedQuestCounter`
        ///   的消费点（逐条累计 + 比 `questTimes`、不放行时摘件），本方法是**取值口**。</para>
        /// <para>⚠️ 原版对 `enchantmentCard == null` 那一格是 NRE 桩 ⇒ 跳过（不复制崩溃语义）。</para>
        /// <para>📌 **本方法暂不接线**（原版调用点要先 VA 现扫，见账 763 的施工步骤 ②）；
        ///   池内 `spellType == 120` 的卡 **0 张** ⇒ 接上也不改任何对局。</para>
        /// </summary>
        public static int GetQuestTimes(CardInstance carrier)
        {
            if (carrier == null) return 0;
            foreach (var e in carrier.ActiveEnchantments)
            {
                if (e == null || e.Card == null) continue;
                if (e.Card.QuestTimes > 0) return e.Card.QuestTimes;
            }
            return 0;
        }

        /// <summary>
        /// 原版 `CardScript.HasCancelAttackEffect(actingCard, targetCard)` 的**完整形态**
        /// （`decomp_full/CardScript__HasCancelAttackEffect.c:1-66`，逐句亲读）。
        ///
        /// <para>原版逐条：① 取 `activeEnchantments(+0x110)`，空/无 ⇒ false；② 逐件取 `enchantmentCard(+0x10)`；
        /// ③ `RawCardScript.HasAbilityType(enchantmentCard, 0x118 /* AboutToAttack = 280 */)` 为假 ⇒ 下一件；
        /// ④ 真 ⇒ 遍历该卡 `cardAbilities(+0x290)`，逐条
        /// `CardAbility.CanTriggerAbility(ability, 0x118, bm, **载体**, actingCard, targetCard, ignoreRandom:0)`；
        /// ⑤ 任一真 ⇒ 返回该件；⑥ 全无 ⇒ `null`。</para>
        ///
        /// <para>**`CanTriggerAbility` 的五道闸**（`decomp_full/CardAbility__CanTriggerAbility.c:1-143` 逐句）
        /// ⇒ 我们的等价口：
        ///   ① `ability.trigger == 280`（<see cref="KeywordTable.AboutToAttack"/> 这一族）；
        ///   ② `IsRightContext(ability, thisCard = 载体)` ⇒ <see cref="RuleCore.IsRightContext"/>；
        ///   ③ `actingCardCriteria(+0x28)` ⇒ <see cref="RuleCore.ActingLevelCriteriaAllows"/>；
        ///   ④ `targetCriteria(+0x30)` ＋ ⑤ `boardCriteria(+0x38)` ⇒ <see cref="RuleCore.AbilityLevelCriteriaAllows"/>。</para>
        ///
        /// <para>三处如实记的残余（⛔ 别当成「做全了」）：
        ///   ① 原版**逐条 ability 判**、任一真即真；我们那三个入口按 `(卡, trigger)` 定位**唯一**一条
        ///     ⇒ 同卡同 trigger 多条 ability 时走既有口径「定位不唯一 ⇒ 不触发 + 出声」（⛔ 不猜第几条）。
        ///   ② `ignoreRandom` 我们没有等价位（`AbilityLevelCriteriaAllows` 不收它）⇒ 传 `true` 时**出声**
        ///     并按 `false` 判（原版那一格只影响 `BoardAnalysis.MeetsCriteria` 的随机项）。
        ///   ③ 附魔卡一般**不在** `cards_engine.json` 里（附魔卡不进可打卡池）⇒ 那三道闸多半回「判不了 ⇒ 放行」，
        ///     与原版「按 ability 数据判」不等价 —— 池内 0 张卡挂「带 280 能力」的附魔 ⇒ **今天零可观察**。</para>
        /// <para>📌 旧的单参重载（<see cref="FindCancelAttack(UnitState)"/>）保持原样（只判「在不在」）——
        ///   `RuleCore` 那个调用点还没切，切的时候改成这个重载（改动请求 → 报告 §五）。</para>
        /// </summary>
        public static ActiveEnchantment FindCancelAttack(BattleContext ctx, UnitState carrier,
                                                         UnitState actingCard, UnitState targetCard,
                                                         bool ignoreRandom = false)
        {
            if (ctx == null || carrier == null || carrier.Instance == null) return null;
            int owner, slot;
            BoardOwnerSlotOf(ctx, carrier.Instance, out owner, out slot);
            if (ignoreRandom)
                ctx.Log("附魔层：`FindCancelAttack(…, ignoreRandom: true)` —— 我们没有它的等价位"
                      + "（`AbilityLevelCriteriaAllows` 不收这个实参）⇒ **按 false 判**"
                      + "（原版只把它传给 board 条件里的随机项）");
            foreach (var e in carrier.Instance.ActiveEnchantments)
            {
                if (e == null || e.Card == null) continue;
                if (!HasAboutToAttackAbility(e.Card)) continue;                     // ③ `HasAbilityType(0x118)`
                string why;
                int abilityContext = RuleCore.AbilityContextOfAbilityV14(
                                         e.Card, TriggerNameAboutToAttack, KeywordTable.AboutToAttack);
                if (!RuleCore.IsRightContext(ctx, owner, slot, abilityContext, carrier.Instance))
                {
                    ctx.Log($"附魔层：「{NameOf(e.Card)}」的 AboutToAttack 能力**载体不在原版要求的区域**"
                          + $"（`abilityContext {abilityContext}`）⇒ 这一件取消不了这一记");
                    continue;
                }
                bool? act = RuleCore.ActingLevelCriteriaAllows(ctx, owner, carrier, e.Card,
                                                               TriggerNameAboutToAttack, actingCard,
                                                               targetCard, out why);
                if (act == false)
                {
                    ctx.Log($"附魔层：「{NameOf(e.Card)}」的 AboutToAttack 被 `actingCardCriteria` 挡掉：{why}");
                    continue;
                }
                if (act == null)
                    ctx.Log($"附魔层：「{NameOf(e.Card)}」的 AboutToAttack **第一道闸判不了 ⇒ 放行**（原版会 NRE）：{why}");
                bool? tgt = RuleCore.AbilityLevelCriteriaAllows(ctx, owner, carrier, e.Card,
                                                                TriggerNameAboutToAttack, targetCard, out why);
                if (tgt == false)
                {
                    ctx.Log($"附魔层：「{NameOf(e.Card)}」的 AboutToAttack 被 `targetCriteria`/`boardCriteria` 挡掉：{why}");
                    continue;
                }
                if (tgt == null)
                    ctx.Log($"附魔层：「{NameOf(e.Card)}」的 AboutToAttack **第二/三道闸判不了 ⇒ 放行**（原版会 NRE）：{why}");
                return e;                                                          // ⑤
            }
            return null;
        }

        /// <summary>原版 `AbilityTrigger.AboutToAttack = 280` 的**枚举名**（与 `CardDef.AbilityTriggerNumber` 同一套词）。
        /// 判据 = `dump.cs` 的 `AbilityTrigger` 枚举 + `CardDef.cs` 的 `{"AboutToAttack", 280}` 那一行。</summary>
        public const string TriggerNameAboutToAttack = "AboutToAttack";

        /// <summary>
        /// 原版 `RawCardScript.HasAbilityType(card, 0x118 /* AboutToAttack */)` 的等价物 ——
        /// **「这张卡有没有一条 trigger 280 的 ability」**（不是「有没有正文」）。
        /// 取值口径：先读 v17 真值列 <see cref="CardDef.AbilityTriggerPresence(string)"/>
        /// （`1` = 有 / `0` = 明确没有 / `-1` = 旧表没这列）；旧表才退回「有没有 AboutToAttack 的
        /// 触发正文」（= 旧的 <see cref="FindCancelAttack(UnitState)"/> 那一口径）。
        /// </summary>
        static bool HasAboutToAttackAbility(CardDef card)
        {
            if (card == null) return false;
            int presence = card.AbilityTriggerPresence(TriggerNameAboutToAttack);
            if (presence >= 0) return presence == 1;
            return card.TriggerOps(KeywordTable.AboutToAttack) != null
                || card.Effect(KeywordTable.AboutToAttack) != null;
        }

        /// <summary>
        /// 🆕 **2026-10-26（`W13EN` · 账 481）：原版 `BattleManager.RemoveEnchantment(card, enchantment, priority)`
        /// 的入队口。**
        ///
        /// <para>原版这个方法**不摘任何东西**（`decomp_full/BattleManager__RemoveEnchantment.c:26-79`，83 行亲读）：
        /// 它组装一条 `BattleAction{actionType = 0x10 (removeEffect)}`（带 carrier + 附魔 effect 两个对象引用），
        /// 再 `AddAutoActionToQueue(action, priority)` ⇒ 出队时由 `ResolveRemoveEffect` 再调
        /// `CardScript.RemoveEffect`。**我们的等价物** = <see cref="RuleCore.EnqueueAbility"/>
        /// （`AddAutoActionToQueue` 的对照物）+ 出队时调 <see cref="Remove"/>。</para>
        ///
        /// <para>✅ **2026-10-26（`W14EC` · 本批）：四个调用点已切到本口** ——
        /// `RuleCore.RevealSecretOrders`（密令揭示那一跳，原版 `RemoveEnchantment(…, 2)` ⇒ `Interrupt`）
        /// 与`RemovePray` 的三个落点（回合开始 `GrantPrayEnchantmentFaith` / 疲劳 `MoveDeckTopToHand` / 真受伤 `ApplyDamageCore`；
        /// 原版都由 PME 的 `chosenAbility 340 removeThisEnchantment` → `AbilityLogic__PlayAbility.c:2250-2282`
        /// → `BattleManager__AddRemoveEnchantment.c` → `AddAutoActionToQueue` ⇒ **排队摘**）。
        /// `Remove` / `RemoveByCard` / `RemovePray`（同步版）保留为**出队体 / 直调口**。</para>
        /// <para>📌 532 的另两段（`CancelAnimFromUnitFromQueue` 连走四条队列道 / `RemoveDestroyableAnims`
        /// 的 buffType 15/16 帧特效）在**表现层**（anim cue 与 trait 帧特效都在 `CardPresentation/Battle/`）
        /// ⇒ 不在本层；已作为改动请求写进报告（那是 P-11 的面）。</para>
        /// </summary>
        /// <param name="lane">进哪条道（原版那个 `priority` 实参 = 来源 ability 的 `priority(+0x44)`）。
        /// 缺省 `Instant` = 原版「不写 priority 的 ability」那条默认道（`CardAbility__ctor.c:5` 写 `+0x18 = 1`）。</param>
        public static bool EnqueueRemove(BattleContext ctx, CardInstance carrier, ActiveCardEffect effect,
                                        ActionPriority lane = ActionPriority.Instant, string label = null)
        {
            if (ctx == null || carrier == null || effect == null) return false;
            bool ok = RuleCore.EnqueueAbility(ctx, lane,
                                              label ?? ("removeEffect " + NameOf(effect.EnchantmentCard)),
                                              () => Remove(ctx, carrier, effect));
            if (!ok)
                ctx.Log($"附魔层：摘「{NameOf(effect.EnchantmentCard)}」的 `removeEffect(16)` **没能入队** —— 这一条没摘");
            return ok;
        }

        /// <summary>
        /// 原版 `CardScript__AddEnchantment.c:91` →
        /// `CardScript__ActivateTraitsOnSummonOrEnchantment(carrier, onSummon: false)` 的等价物（账 687 第五步）。
        ///
        /// <para>🔴 **那一跳不改任何 trait** —— 它遍历的是**载体自己的** `CurrentTraits(+0x128)`，
        /// 把「能不能动 / 能不能攻击」按当前 trait 集重算一遍
        /// （`ActivateTraitsOnSummonOrEnchantment.c:75-99`：`fast`/`flank` ⇒ `canAct = true`；
        /// `ferocity`/`oath` ⇒ `canAct = true` 且 `canAttack = false`；`dropPod(0xe6)` 那支只在
        /// `onSummon == true` 时重置 drop pod 数值 ⇒ 我们这一跳 `onSummon:false` **不进那一支**）。
        /// 等价口 = <see cref="RuleCore.ActivateTraitsOnSummonOrEnchantment"/>（**唯一的写点**，
        /// 判据/逐行注释都在 `RuleCore.cs`）—— ⛔ 别在本层另写一份。</para>
        ///
        /// <para>原版的守卫是 `cardState(+0x228) ∈ {2 在场, 3, 15, 17}`（`.c:43-44`）⇒
        /// 我们按「载体此刻在棋盘上」等价；不在棋盘上（挂到手里 / 墓地）就**不调**。</para>
        ///
        /// <para>⚠️ 原版同一跳还会对每条 trait 调 `BattleCardUI.DisplayStatusAnim`（表现层）—— 我们没有对应层，
        /// 如实留缺口（另：池内能挂到附魔层的卡当前 0 张带 trait，见报告 §五）。</para>
        /// </summary>
        static void ActivateTraitsAfterEnchant(BattleContext ctx, CardInstance carrier)
        {
            if (ctx == null || carrier == null) return;
            int owner, slot;
            BoardOwnerSlotOf(ctx, carrier, out owner, out slot);
            if (owner < 0 || slot < 0) return;
            var u = ctx.Players[owner].Board[slot];
            if (u == null) return;
            RuleCore.ActivateTraitsOnSummonOrEnchantment(u);
        }

        /// <summary>这张牌此刻在**哪一格**（找不到 ⇒ 两个都 `-1`）。
        /// ⚠️ 这是 `RuleCore.FindBoardCell`（**private**，本类看不见）的**同义扫描** —— 语义逐字相同
        /// （按 `PlayerState.Board` 两方逐格比 `CardInstance` 引用）；⛔ 两边别各改一半。</summary>
        static void BoardOwnerSlotOf(BattleContext ctx, CardInstance carrier, out int owner, out int slot)
        {
            owner = -1;
            slot = -1;
            if (ctx == null || carrier == null) return;
            for (int p = 0; p < ctx.Players.Length; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var cell = board[s];
                    if (cell != null && ReferenceEquals(cell.Instance, carrier)) { owner = p; slot = s; return; }
                }
            }
        }

        /// <summary>
        /// 🆕 **2026-10-26（`W14EC` · 账 481 的收尾）：把「摘祈祷附魔」也走【排队摘】。**
        ///
        /// <para>判据（🥇 全量反编译，本批亲读）：祈祷附魔 `Pray Enchantment` 的两条能力都是
        /// `chosenAbility 340 removeThisEnchantment` ——
        /// ability[0] `trigger 60 UnitDamaged`（受伤即撤）· ability[1] `trigger 31 TurnStartPlayer`
        /// （`buffMana{faithMana:1}` → 340）。而 340 那支在
        /// `AbilityLogic__PlayAbility.c:2250-2282` 里调的是 `BattleManager__AddRemoveEnchantment`
        /// → `AddAutoActionToQueue(action, priority = ±0x44)`（`BattleManager__AddRemoveEnchantment.c` 尾部）
        /// ⇒ **原版是「入队、等当前动作结束再摘」**，不是当场摘。</para>
        ///
        /// <para>⚠️ **顺序（`RuleCore.GrantPrayEnchantmentFaith` 那一格必须先看懂）**：`buffMana` 走的 `BattleManager__AddManaBuff`
        /// 同样是「入队」（同文件尾部也是 `AddAutoActionToQueue`）—— 两条 action 同一条道、FIFO
        /// ⇒ 原版的相对次序是 **先加信仰、后排摘**。本方法只负责「排摘」这一半；
        /// 调用点（`RuleCore.GrantPrayEnchantmentFaith`）已经把 `Faith += 1` 挪到它**之前**。</para>
        ///
        /// <para>⚠️ **与 <see cref="RemovePray"/> 的分工**：那一份是**同步**摘（出队体 / 直调口）；
        /// 这一份只入队。`Effect` 丢了（正常挂载路径走不到）时退回同步摘 —— ⛔ 不留半条。</para>
        /// </summary>
        /// <param name="lane">缺省 `Instant` = 祷告附魔两条 ability 的 `priority` **数据真值 1**
        /// （`cards_raw.json` 的 `Z-NC Sororitas-Pray_enchantment`：`cardAbilities[].priority = 1`；
        /// 与 `CardAbility__ctor.c:5` 的默认 `+0x18 = 1` 同值）。</param>
        public static bool EnqueueRemovePray(BattleContext ctx, UnitState carrier,
                                            ActionPriority lane = ActionPriority.Instant)
        {
            var e = FindPray(carrier);
            if (e == null) return false;
            if (e.Effect != null && carrier.Instance != null)
                return EnqueueRemove(ctx, carrier.Instance, e.Effect, lane,
                                     "removeEffect 祈祷附魔：「" + NameOf(e.Card) + "」");
            // 兜底（正常挂载路径走不到）：`Effect` 丢了也要把 `+0x110` 那条摘掉，⛔ 不留半条。
            if (carrier.Instance != null) carrier.Instance.ActiveEnchantments.Remove(e);
            return true;
        }

        // ================================================================================================
        //  🆕 2026-10-27（批次 9 · 执行代理 `B9_W43DA42`）—— **`DA42 Reconnaissance Mission` 附魔链**
        //
        //  判据（第一权威 = 全量反编译 + `cards_truth.json`，全文 7 跳）：
        //     `资料/完全复刻/判据/DA42与BroadcastHandWhen剩余_判定.md` §一·1 / §三（A2/A3/A5）。
        //
        //  一句话：打出 DA42 → 从**对手手牌**挑一张 → 给**那份手牌实例**挂一条 `buffType 15` 附魔 →
        //    **那张牌被打出时**，附魔的 `trigger 0` 结算 `buffMana{3, enemyPlayer:1, questPoints:1}`
        //    ⇒ **+3 任务点给「载体属主的对手」** → 再 `340 removeThisEnchantment` 把自己摘掉（FIFO：先加、后排摘）。
        //
        //  ⚠️ 这两段是**本链独有的接线端**（挂 / 派发）；「+3 落点」那一跳复用
        //     `RuleCore.GrantQuestPointsToOpponentOfCarrier`（= `DoFactionResource` 的换边语义，⛔ 不另写一份）。
        //  —— 本段全部追加在类尾（⛔ 别插到中间：`账/*.tsv` 按 `文件:行号` 引）。
        // ================================================================================================

        /// <summary>
        /// `DA42` 的**附魔模板** uniqueId（= 我们卡池的 `id`）。
        /// 判据 = `资料/普查产出_1018/allcards_1350/cards_raw.json` 的
        /// `m_Name = Reconnaissance Mission`（`cardType 20` · `path_id` 见判据件 §一·2 P0）；
        /// 它的 `abilities[0].logic` = `[{chosenAbility 60, manaBuff{3, enemyPlayer:1, questPoints:1}},
        /// {chosenAbility 340, targetCriteria{targetsAffected 20}}]`，**两行都挂 `trigger 0`**
        /// （`cards_truth.json` 亲读）。
        /// <para>⛔ **今天不在池里**（`cards_engine.json` 1128 张查无此 id）：入池那一步是判据件 §三 **A1a**
        /// （`gen_cards_engine.py` 的 `ENCHANTMENT_EXECUTORS`），**会改池指纹** ⇒ 只能由调度台在
        /// 「没有账写手」的窗口做（本笔 ⛔ 不改数据、⛔ 不跑生成器）。</para>
        /// </summary>
        public const string DA42EnchantmentCardId = "e8cf1eb55ca41534abe38efc90b302e9";

        /// <summary>打出这条链的**那张卡自己**的 `id`（`cards_engine.json` = `DA42`）。</summary>
        public const string DA42CardId = "DA42";

        /// <summary>附魔模板的显示名（**兜底判据**：按 id 找不到时才用，且必须同时是
        /// <see cref="CardDef.IsEnchantment"/>）。⚠️ 它和战术卡 `DA42` 同名 —— 所以兜底**必须**
        /// 带 `IsEnchantment` 闸，否则会把那张战术卡当成附魔模板（静默挂错卡）。</summary>
        public const string DA42EnchantmentName = "Reconnaissance Mission";

        /// <summary>
        /// 这张附魔模板在本局卡池里（找不到返回 `null`）。**只按 id 认**，id 不中才退
        /// 「名字 + `enchantment` 型」—— 形状照 <see cref="FindPrayCard"/>（同一条判据，⛔ 别各写一份）。
        /// <para>⚠️ **A1a 之前恒 `null`**（模板不入池，见 <see cref="DA42EnchantmentCardId"/> 的 doc）——
        /// 调用方必须**如实出声**，⛔ 不许把它当「这条链不适用」。</para>
        /// </summary>
        public static CardDef FindDA42Enchantment(BattleContext ctx)
        {
            if (ctx == null || ctx.CardPool == null) return null;
            CardDef byName = null;
            for (int i = 0; i < ctx.CardPool.Count; i++)
            {
                var c = ctx.CardPool[i];
                if (c == null) continue;
                if (c.Id == DA42EnchantmentCardId) return c;
                if (byName == null && c.IsEnchantment && c.Name == DA42EnchantmentName) byName = c;
            }
            return byName != null ? byName : ctx.FindEnchantmentTemplate(DA42EnchantmentCardId);   // 🆕 B89/E6d：同上（`FindPrayCard` 那条兜底），⛔ 别各写一份
        }

        /// <summary>
        /// **这个载体身上，哪些附魔对这一条触发关键词有话说**（原版
        /// `RawCardScript__TriggerAbilities.c:50-88` 把 `+0x110` 里每个附魔的 `cardAbilities(+0x290)`
        /// 一起算进来）—— **与 <see cref="Matching"/> 同一条口径，但吃 `CardInstance`**。
        ///
        /// <para>⚠️ **为什么不能直接复用 <see cref="Matching"/>**：那个吃 `UnitState`（只覆盖棋盘），
        /// 而 DA42 的载体是**手牌/刚打出的那一份实例**（判据件 §一·2 P2）。</para>
        /// <para>🔴 **认能力的两条路（按序）**：
        /// ① **卡面正文**（= <see cref="Matching"/> 的判据：`CardDef.TriggerOps` / `CardDef.Effect`）；
        /// ② **id 白名单 + trigger 名**（DA42 模板**没有卡面正文** —— 它不在 `cards_engine` 的 OCR 层，
        ///   所以 ① 对它**恒 false**，判据件 §四·4）。</para>
        /// </summary>
        public static List<ActiveEnchantment> MatchingForInstance(CardInstance carrier, string triggerName)
        {
            var r = new List<ActiveEnchantment>();
            if (carrier == null || string.IsNullOrEmpty(triggerName)) return r;
            foreach (var e in carrier.ActiveEnchantments)
            {
                if (e == null || e.Card == null) continue;
                // ① 卡面正文（Pray 那一族与将来任何「自带正文」的附魔都走这里）
                if (e.Card.TriggerOps(triggerName) != null || e.Card.Effect(triggerName) != null) { r.Add(e); continue; }
                // ② id 白名单：DA42 模板的两条 ability 都挂 `trigger 0 = ThisCardPlayed`
                if (IsDA42Enchantment(e.Card) && IsThisCardPlayed(triggerName)) { r.Add(e); continue; }
            }
            return r;
        }

        /// <summary>这张附魔卡是不是 DA42 那条模板（按 id；⛔ 不按名字 —— 名字与战术卡同名）。</summary>
        public static bool IsDA42Enchantment(CardDef enchantmentCard)
        {
            return enchantmentCard != null && enchantmentCard.Id == DA42EnchantmentCardId;
        }

        /// <summary>`trigger 0` 的名字（`AbilityTrigger.ThisCardPlayed`，判据 = `CardDef` 的
        /// trigger 名表 `{"ThisCardPlayed", 0}`）。大小写不敏感 —— 与 `ActionPump` 同口径。</summary>
        static bool IsThisCardPlayed(string triggerName)
        {
            return string.Equals(triggerName, "ThisCardPlayed", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// **承载卡被打出时，派发它身上 DA42 附魔的 `trigger 0`**
        /// （原版 `RawCardScript__OnCardPlayedWithTarget.c:43` 的 `thisCard == cardPlayed` 支
        /// → `TriggerAbilities(trigger 0, includeEnchantments: 1)` → `RawCardScript__TriggerAbilities.c:202`
        /// 的共享尾块扫 `thisCard.activeEnchantments(+0x110)`）。
        ///
        /// <para>**逐跳照原版（判据件 §一·1 ⑤⑥⑦）**：
        /// ① 每条命中的附魔 **入队** 一次「+3 任务点」——落点 = **`1 - carrierOwner`**
        ///   （原版 `buffMana{manaAmount 3, enemyPlayer 1, questPoints 1}`：`ResolveManaBuff` 对
        ///    `enemyPlayer` 非零**把那一格取反**，而 `isPlayer` 来自**载体**）；
        /// ② 再 **入队** 摘掉那一条（原版 `chosenAbility 340` → `AddRemoveEnchantment` → `AddAutoActionToQueue`）。
        ///   两条同一条道（缺省 `Instant`）FIFO ⇒ **先加、后排摘**。⚠️ 顺序反了 = 摘了才加（数值还是一样，
        ///   但 `EvtKind.EnchantmentRemoved` 会早于 `GainQuest`，与原版的事件序相反）。</para>
        ///
        /// <para>⚠️ **幂等性**：原版每打出一次就扫一遍 `+0x110`、结算完就摘 ⇒ 同一件附魔**只响一次**；
        /// 我们靠「排摘」达到同一结果（不需要 `Spend` —— 原版这一跳也不调 `SpendEnchantment`，
        /// 那两处调用点只在 `spellType 130` / `chosenAbility 280`，与附魔自身结算无关）。</para>
        ///
        /// <para>调用点（判据件 §三 A4）：`EffectResolver.PlayTacticCore` 的 `BroadcastWhen(Play)` 之后、
        /// `RuleCore.PlayCardCore` 的同名广播之后。</para>
        /// </summary>
        public static void OnCarrierPlayed(BattleContext ctx, CardInstance carrier, int carrierOwner)
        {
            if (ctx == null || carrier == null) return;
            var hits = MatchingForInstance(carrier, "ThisCardPlayed");
            if (hits.Count == 0) return;
            int gainOwner = 1 - carrierOwner;   // 原版 `manaBuff.enemyPlayer`：相对【载体属主】取反
            string carrierName = NameOf(carrier.Card);
            for (int i = 0; i < hits.Count; i++)
            {
                var e = hits[i];
                if (e == null) continue;
                var eff = e.Effect;
                // ① 先入队 +3（原版 `buffMana` → `AddManaBuff` → `AddAutoActionToQueue`）
                RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "DA42附魔:+3任务点",
                    () => RuleCore.GrantQuestPointsToOpponentOfCarrier(ctx, carrierOwner, 3,
                                                                     "Reconnaissance Mission 附魔"));
                ctx.Log($"附魔层：「{carrierName}」被打出 ⇒ 它身上的「{NameOf(e.Card)}」结算 `trigger 0`："
                      + $"**{ctx.Players[gainOwner].Name} +3 任务点**"
                      + $"（`buffMana.enemyPlayer` 相对载体属主取反 ⇒ 给 {gainOwner} 号玩家）");
                // ② 再入队摘掉这一条（原版 `chosenAbility 340 removeThisEnchantment`；同道 FIFO ⇒ 先加后排摘）
                if (eff != null)
                    EnqueueRemove(ctx, carrier, eff, ActionPriority.Instant,
                                  "removeEffect DA42 附魔：「" + NameOf(e.Card) + "」");
            }
        }

        /// <summary>
        /// **DA42 的挂载端**：把附魔模板挂到「刚才选中的那一份手牌实例」上
        /// （原版 `AbilityLogic.PlayAbility` 的 `chosenAbility 30 buffOthers` → `CardScript__AddEffect`
        /// `case 0xf(15) → AddEnchantment`，**无区域闸** ⇒ 手牌实例照挂；判据件 §一·1 ③）。
        ///
        /// <para>🔴 **只在「这次出牌结算的卡 = DA42」时动手**（<see cref="BattleContext.CardPlaySource"/>）；
        /// 其余 `Choose a card in your opponent's hand` 的卡**一个字都不改**（它们今天仍是
        /// 「只挑出来、什么都不做」）。</para>
        /// <para>⚠️ **模板不在池里（A1a 之前）⇒ 不挂 + 如实出声**（⛔ 不静默、不拿别的卡顶替）。</para>
        /// <para>⚠️ `trait` 传 **`""`**（明确没有）：原版 `CardEffect.trait` 取自**挂它的那条 buff**，
        /// 而 DA42 的 buff 在 **DA42 自己**的 ability 里（`cards_truth` 那一行的 `logic[0]` 没有 `trait`）
        /// —— ⛔ 别让它退到 <see cref="Attach"/> 的「读载体卡 `EnchantTrait`」那条路（那是 Pray 的约定，
        /// 会把对手手牌那张卡的 enchantTrait 错记成这条附魔的 trait）。</para>
        /// </summary>
        public static ActiveCardEffect AttachDA42ToChosenHandCard(BattleContext ctx, CardInstance chosenHandInst)
        {
            if (ctx == null || chosenHandInst == null) return null;
            var src = ctx.CardPlaySource;
            if (src == null || src.Id != DA42CardId) return null;   // ⛔ 别的「从对手手牌选一张」的卡不受影响
            var tmpl = FindDA42Enchantment(ctx);
            if (tmpl == null)
            {
                ctx.Log("附魔层：⚠️ `DA42` 要挂附魔，但本局卡池里找不到模板「" + DA42EnchantmentName
                      + "」（`id " + DA42EnchantmentCardId + "`）—— **这条附魔没挂上**，"
                      + "那张牌被打出时**不会** +3 任务点（模板入池 = 判据件 §三 A1a，待调度台做；"
                      + "⛔ 不静默退回「打出即 +3」）");
                return null;
            }
            var eff = Attach(ctx, chosenHandInst, tmpl, BuffTypeSingleUse,
                             source: src.Name, acting: null, trait: "");
            if (eff != null)
                ctx.Log("附魔层：「" + NameOf(chosenHandInst.Card) + "」（对手手牌）挂上「"
                      + DA42EnchantmentName + "」—— **它被打出时才 +3 任务点**（打出当刻不结算）");
            return eff;
        }

        /// <summary>
        /// **A7 的结算层闸**（`B9_W43DA42`）：`DA42` 卡面那句 `When played, gain 3 Quest Points`
        /// **不属于「打出这张卡时的正文」** —— 它是**附魔的触发条件**（原版：那句 buff 挂在
        /// `cardAbilities[trigger 0].abilityLogic[]` 的 `buffEffect{enchantment → 模板}` 上，
        /// 由 `CardScript__AddEffect` 挂到**承载卡**的 `+0x110`，**承载卡被打出时**才结算；
        /// 判据件 §一·1 ③④⑤）。
        ///
        /// <para>🔴 **为什么不改解析器（与判据件 §三 A7 的字面口径有出入，如实写明）**：
        /// 原计划是「让主解析器不再产 `gainquest`」。实测 `ruleprobe check` 的
        /// **【输出指纹】哨兵**（`Program.PinnedParseMd5`）钉死了**整池解析输出的 md5**，
        /// 而它「重生成基线也抹不掉」⇒ 动解析输出会让 `check` 永久 `rc=1`，且重钉要改
        /// `工具/ruleprobe/**`（本波别人正在写）。⇒ 改成**结算层跳过**：解析输出逐字不变
        /// （`scan` / 基线 / 指纹全绿），**行为**与原计划一致（打出当刻 +0 任务点）。</para>
        ///
        /// <para>闸门收得很紧：只认「**这次结算的来源卡是 DA42**」+「**op 是 `gainquest`**」+
        /// 「**原句以 `When played` 开头**」三条同时成立 —— 现池里同时满足的只有**那一条 op**
        /// （`cards_engine.json` 的 `DA42.desc` 亲读）。</para>
        /// </summary>
        public static bool IsEnchantTriggerClause(CardDef sourceCard, EffectOp op)
        {
            if (sourceCard == null || op == null) return false;
            // 🔶 自己设计（非原版）：用「卡 id 白名单」代替原版的 `CardEffect.enchantment` PPtr 引用
            //   · 为什么必须自己定：`cards_engine.json` 没有 `enchantment` 列、`CardDef` 也没有对应字段
            //     （判据件 §一·2 P0），模板本身还要等 A1a 才入池 ⇒ 今天只能按 id 认；
            //     A1b（通用被动 `abilityLogic` 解释器）落地后应改读数据列。
            //   · 调度台 2026-10-27 裁定（本笔 A1a/A7 的口径）。
            if (sourceCard.Id != DA42CardId) return false;
            if (op.Verb != "gainquest") return false;
            return op.Source != null
                && op.Source.IndexOf("when played", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ==============================================================================================
        //  🆕 2026-10-27（B88 · 波 2 · 执行代理 `E2` · 账 1287）：**附魔触发的通用入口**
        // ==============================================================================================

        /// <summary>
        /// **原版 `RawCardScript.TriggerEnchantment(manager, actingCard, targetCard, onlyEnchantments)`**
        /// （🥇 `decomp_full/RawCardScript__TriggerEnchantment.c:1-22` 全文；ILSpy `RawCardScript.cs:1881-1897` 交叉）。
        ///
        /// <para>**原版逐句**：对**这张附魔卡的 `cardAbilities`** 连跑**两次** `TriggerAbilities` ——
        /// ① `trigger = 0`（`ThisCardPlayed`）· ② `trigger = 500`（`TriggerEnchantment`）；
        /// 两次都 `isEnchantment = 1`（🔴 `.c` 第 8 个实参是字面量 `1`；
        /// ILSpy 那一份把该局部渲染成 `default`（= false）—— 这是**ILSpy 丢 `= true` 常量**的已知形态
        /// （`CLAUDE.md` §三 同族三条），**以 `.c` 为准**）、`resolveOnlyEnchantments = param_5`、
        /// 两次各新建一个空 `DamageInfo`（`actingCard` / `targetCard` 原样传，`cardPlayed` 传 `default`）。</para>
        ///
        /// <para>🔴 **它【不是】`Enchantments.OnCarrierPlayed`**（那是账 1287 的诊断要点，别混）：
        /// `OnCarrierPlayed` 是 **DA42 专用**的硬编码（`Enchantment.cs:1190-1214`：写死「+3 任务点 +
        /// 摘附魔」、按 `CardId + gainquest + when played` 三条闸认卡）。它长得像通用派发，
        /// 实际只服务**一张卡的一种时机**。本方法才是「**拿任意附魔卡、按它的两条 trigger 派发**」的通用形状。</para>
        ///
        /// <para>**我们的两条 trigger 今天各有多少数据（现读，如实）**：</para>
        /// <list type="bullet">
        /// <item>**trigger 0**：附魔模板的 ability 在 `desc` 里**不写**（`Pray Enchantment` 的 `desc` 为空、
        ///   DA42 模板 `e8cf1eb5…` 的两条 logic 是 `chosenAbility 60/340` 而不是文字）⇒ 我们这一侧
        ///   **能拿到的只有 `EffectText.Parse(card.Desc)`**；模板卡 `desc` 空 ⇒ 这一跳**没有 op 可跑**。</item>
        /// <item>**trigger 500**：全池 `abilities[].trigger == 500` **0 行**（`cards_truth.json` 现读）⇒
        ///   `CardDef.TriggerOps(KeywordTable.TriggerEnchantment)` 恒 `null`。</item>
        /// </list>
        /// <para>⇒ 本方法**结构照抄**，并在「两跳都没东西可跑」时**出声**（⛔ 不静默空转、⛔ 不发明效果；
        /// 本工程红线：不许让玩家以为它生效了）。**返回值** = 至少跑成了一跳。</para>
        ///
        /// <para>📊 **可达性**：原版两个调用点在我们引擎里**都不存在** ——
        /// ① `BattleManager.ResolveEndTurnEnchantment`（教程关卡数据 `TutorialStage.playerTurnEndEnchantment`，
        ///    我们没有战役教程关卡数据）；② `CardScript.cs:14810/14827` 的
        ///    `activeAbility.loadingAbilityLogic.TriggerEnchantment(...)`（我们没有 `loadingAbilityLogic` 这一格）。
        /// ⇒ 本入口今天 **0 调用点**（休眠机制，铁律 11：休眠也做）。</para>
        /// </summary>
        /// <param name="acting">原版 `actingCard`（谁让这条附魔响的）；可为 null。</param>
        /// <param name="target">原版 `targetCard`；可为 null。</param>
        /// <param name="onlyEnchantments">原版 `resolveOnlyEnchantments`（今天原版所有调用点传的都是 false）。</param>
        public static bool TriggerEnchantment(BattleContext ctx, int owner, CardDef enchantmentCard,
                                              UnitState acting = null, UnitState target = null,
                                              bool onlyEnchantments = false)
        {
            if (ctx == null || enchantmentCard == null) return false;
            string name = enchantmentCard.Name ?? "?";
            bool any = false;

            // ---- ① `trigger = 0`（ThisCardPlayed）：附魔卡自己 `desc` 里那一条正文 ----
            var playOps = ParseEnchantOps(enchantmentCard);
            if (playOps != null && playOps.Count > 0)
            {
                ctx.Log($"附魔触发：「{name}」跑 `trigger 0`（{playOps.Count} 条；原版 "
                      + "`RawCardScript__TriggerEnchantment.c:12` 的第一次 `TriggerAbilities`）");
                RuleCore.ResolveOps(ctx, owner, acting, playOps, "附魔触发(trigger 0):" + name,
                                    seed: target ?? acting);
                any = true;
            }

            // ---- ② `trigger = 500`（TriggerEnchantment）：卡面/数据里标了 500 的那一族 ----
            var entOps = enchantmentCard.TriggerOps(KeywordTable.TriggerEnchantment);
            if (entOps != null && entOps.Count > 0)
            {
                ctx.Log($"附魔触发：「{name}」跑 `trigger 500`（{entOps.Count} 条；原版 `.c:20` 的第二次 `TriggerAbilities`）");
                RuleCore.ResolveOps(ctx, owner, acting, entOps, "附魔触发(trigger 500):" + name,
                                    seed: target ?? acting);
                any = true;
            }

            if (!any)
            {
                // ⛔ 不静默：这是「结构已复刻、数据今天没有」的**已知空转**，必须说出来。
                ctx.Log($"（附魔触发：「{name}」的 `trigger 0` / `trigger 500` 两跳**都没有可跑的 op**"
                      + "（模板卡 `desc` 为空、全池 `trigger == 500` 0 行）⇒ 原版这两跳今天在我们这边空转。"
                      + "⚠️ DA42 那条链的 `trigger 0` 半走的是**专用的** `Enchantments.OnCarrierPlayed`，"
                      + "不是本入口）");
            }
            return any;
        }

        /// <summary>
        /// <see cref="TriggerEnchantment"/> 的 `trigger 0` 取数口：附魔卡 `desc` 解析出的 op。
        /// 解析不了/没有正文 ⇒ 返回 `null`（⛔ 不返回空表冒充「跑过了」）。
        /// </summary>
        static List<EffectOp> ParseEnchantOps(CardDef card)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.Desc)) return null;
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            return ops != null && ops.Count > 0 ? ops : null;
        }

        // ==================================================================
        //  ⑥ 原版 `EntityScript.HasEnchantment(AbilityTrigger)` 的**三个「闸」调用点**
        //     🆕 2026-10-28（完全复刻 · B89 第 1 波 · 执行代理 `E4` · 账 `L1042`）
        //  —— 本段全部追加在类尾（⛔ 别插到中间：`账/*.tsv` 按 `文件:行号` 引 `Enchantment.cs`）
        // ==================================================================
        //  【原版那个方法】（`cpp2il/out_cpp2il_dev_ilspy/EntityScript.cs:1806-1841`，
        //    C 侧 `decomp_full/EntityScript__HasEnchantment.c`，两边逐句一致）：
        //    `foreach (ActiveEnchantment ae in activeEnchantments(+0x110))
        //         if (ae != null && ae.enchantmentCard(+0x10) != null
        //             && ae.enchantmentCard.HasAbilityType(trigger)) return true;`
        //    `HasAbilityType(trigger)` = 遍历该卡 `cardAbilities(+0x290)`、比 `CardAbility.trigger(+0x10)`
        //    （`RawCardScript__HasAbilityType.c:16-38`，**只看 trigger、不看 abilityLogic 空不空**）。
        //    ⇒ 判的是「**附魔卡自己**有没有一条这个 trigger 的 ability」，**不是**「载体身上有没有附魔」。
        //
        //  【全树只有三个调用点】（`grep -rn "HasEnchantment(AbilityTrigger"` 现读 = 3 行 / 4 个 trigger）：
        //    ① `CardScript.cs:6827`，在 `CardScript.HasWhileInPlayAbility()` 里（方法起于 `:6800`）：
        //         `if (!HasEnchantment(WhileInPlay) && !HasEnchantment(WhileInPlayVariable)
        //              && !HasCurrentTrait(DefinedTrait.pack /* 610 */)) { …再查 activeEffects… }`
        //       ⚠️ **第三个析取项是 `pack` trait，不是附魔** ⇒ 不在本层，调用方自己 OR 上
        //         （我们这边 = `UnitState.Has("pack")`）。
        //    ② `CardScript.cs:14072`，在 `ResolveUnitAttacked` 的近战那支：
        //         `if (HasCurrentTrait(DefinedTrait.mob /* 0x398 */) || HasEnchantment(Mob)) BroadcastUnitMob(this);`
        //    ③ `CardScript.cs:14298`，在伤害结算里（`damagedCard == this` 那一支）：
        //         `if (… HasDefaultTrait(rage) || damagedCard.HasCurrentTrait(rage)
        //               || damagedCard.HasEnchantment(Rage)) { OnTrigger(Rage, …); … }`
        //    ⇒ 本层只提供**三个闸里属于「附魔」的那一半**（另外两半是 `UnitState.Has` / `CardDef`），
        //      **调用点接线归批次 4 的 `E3`**（它只接调用点），本批不动 `RuleCore`。
        //
        //  【`enum → 关键词` 的映射】数值来自 `d:/2/tools/il2cpp_out/dump.cs` 的 `AbilityTrigger`
        //    （`WhileInPlay = 20` · `Rage = 130` · `WhileInPlayVariable = 240` · `Mob = 300`）；
        //    我们引擎里那套「名字 ↔ 数值」的唯一正本是 `CardDef.AbilityTriggerNumberOfName`
        //    （现读 `{"WhileInPlay", 20}` / `{"WhileInPlayVariable", 240}` / `{"Mob", 300}` / `{"Rage", 130}`，
        //    逐条已核）⇒ 下面只写**名字常量**，取数一律走 `CardDef.AbilityTriggerPresence(名字)`
        //    （它内部就是 `AbilityTriggerNumber(名字)` + `_abilityTriggers` 集合），⛔ 别在这里抄第三份数字表。
        //
        //  【三值口径（`-1` 不许读成「没有」）】`CardDef.AbilityTriggerPresence` 返回
        //    `1` 有 / `0` 明确没有 / `-1` 旧表缺列。`1` ⇒ 真；`0` ⇒ 这一件跳过；
        //    `-1` ⇒ **退回关键词口径**（`TriggerOps(名字) != null || Effect(名字) != null`，
        //    与 `HasAboutToAttackAbility` 同一套兜底、与本文件 `Matching` 同一口径）。
        //    ⚠️ 为什么必须分三档：`-1` 当「没有」= 把「判不了」静默变成「没有」（铁律 2 的老毛病）。
        //
        //  📊 **可达性**：本文件 `HasAny` 的 doc 已记「今天 0 张卡有附魔」⇒ 三个闸**全部休眠**，
        //    接上也不改任何对局（与 `L1042` 账上「今天只有 `FireTriggerAt` 一处消费者」自洽）。
        // ==================================================================

        /// <summary>原版 `AbilityTrigger.WhileInPlay = 20` 的枚举名
        /// （判据 = `dump.cs` 的 `AbilityTrigger` + `CardDef.AbilityTriggerNumberOfName` 那一行）。</summary>
        public const string TriggerNameWhileInPlay = "WhileInPlay";

        /// <summary>原版 `AbilityTrigger.WhileInPlayVariable = 240` 的枚举名。</summary>
        public const string TriggerNameWhileInPlayVariable = "WhileInPlayVariable";

        /// <summary>原版 `AbilityTrigger.Mob = 300` 的枚举名。</summary>
        public const string TriggerNameMob = "Mob";

        /// <summary>原版 `AbilityTrigger.Rage = 130` 的枚举名。</summary>
        public const string TriggerNameRage = "Rage";

        /// <summary>
        /// **原版 `EntityScript.HasEnchantment(AbilityTrigger trigger)` 的等价物**
        /// （`EntityScript.cs:1806-1841`，逐句 → 本段头注）。
        ///
        /// <para>载体身上**有没有一件**附魔卡，它自己带一条 `trigger == <paramref name="triggerName"/>`
        /// 的 ability。`triggerName` 用 `CardDef.AbilityTriggerNumberOfName` 那套词
        /// （本类给了四个常量：`WhileInPlay` / `WhileInPlayVariable` / `Mob` / `Rage`）。</para>
        ///
        /// <para>⚠️ **三值**（见本段头注）：`AbilityTriggerPresence` 返 `-1`（旧表缺列）时退回
        /// <see cref="Matching"/> 的关键词口径 —— 那是**判不了时的兜底**，不是等价物。</para>
        ///
        /// <para>⚠️ 与原版两处形状差：① 原版对 `ae == null` / `enchantmentCard == null` 那一格**会 NRE**
        /// （ILSpy 的 `NullReferenceException` 桩）⇒ 我们跳过（不复制崩溃语义，与本文件
        /// `HasEnchantmentWhileInPlay` 的取舍同一条）；② 原版逐件返回 `true`/扫完 `false`，我们同。</para>
        /// </summary>
        public static bool HasEnchantmentForTrigger(CardInstance carrier, string triggerName)
        {
            if (carrier == null || string.IsNullOrEmpty(triggerName)) return false;
            foreach (var e in carrier.ActiveEnchantments)
            {
                if (e == null || e.Card == null) continue;
                int presence = e.Card.AbilityTriggerPresence(triggerName);
                if (presence == 1) return true;                 // 明确有那条 trigger 的 ability
                if (presence == 0) continue;                    // 明确没有 ⇒ 这一件跳过
                // -1 = 旧表 / 缺列 ⇒ 退回关键词口径（判不了时的兜底，⛔ 别读成「没有」）
                if (e.Card.TriggerOps(triggerName) != null || e.Card.Effect(triggerName) != null) return true;
            }
            return false;
        }

        /// <summary><see cref="HasEnchantmentForTrigger(CardInstance,string)"/> 的棋盘形态。</summary>
        public static bool HasEnchantmentForTrigger(UnitState carrier, string triggerName)
        {
            return carrier != null && HasEnchantmentForTrigger(carrier.Instance, triggerName);
        }

        /// <summary>
        /// **闸 ①**：原版 `CardScript.HasWhileInPlayAbility()`（`CardScript.cs:6800`，闸在 `:6827`）
        /// 里属于附魔的那**两项析取** —— `HasEnchantment(WhileInPlay) || HasEnchantment(WhileInPlayVariable)`。
        ///
        /// <para>🔴 **调用方还要 OR 上第三项 `HasCurrentTrait(DefinedTrait.pack /* 610 */)`** ——
        /// 那是 **trait** 不是附魔（`UnitState.Has("pack")`），⛔ 别塞进本方法（本类是附魔层）。
        /// 原版那一整句：
        /// <c>if (!HasEnchantment(WhileInPlay) &amp;&amp; !HasEnchantment(WhileInPlayVariable)
        ///    &amp;&amp; !HasCurrentTrait(pack)) { …再扫 activeEffects 里的 pack 记录… }</c></para>
        /// </summary>
        public static bool HasWhileInPlayGate(UnitState carrier)
        {
            return HasEnchantmentForTrigger(carrier, TriggerNameWhileInPlay)
                || HasEnchantmentForTrigger(carrier, TriggerNameWhileInPlayVariable);
        }

        /// <summary>
        /// **闸 ②**：原版 `ResolveUnitAttacked` 的近战那支（`CardScript.cs:14072`）里属于附魔的那一项 ——
        /// `HasCurrentTrait(DefinedTrait.mob) || HasEnchantment(AbilityTrigger.Mob)` 的**后一半**。
        /// 🔴 前一半 `HasCurrentTrait(mob)` 是 trait（0x398），调用方自己 OR。
        /// </summary>
        public static bool HasMobGate(UnitState carrier)
        {
            return HasEnchantmentForTrigger(carrier, TriggerNameMob);
        }

        /// <summary>
        /// **闸 ③**：原版伤害结算里那一支（`CardScript.cs:14298`）属于附魔的那一项 ——
        /// `HasDefaultTrait(rage) || HasCurrentTrait(rage) || HasEnchantment(AbilityTrigger.Rage)`
        /// 的**最后一半**。🔴 前两项是 trait（卡面印的 / 当前生效的），调用方自己 OR。
        /// </summary>
        public static bool HasRageGate(UnitState carrier)
        {
            return HasEnchantmentForTrigger(carrier, TriggerNameRage);
        }

        // ==================================================================
        //  🆕 B89 / E6d（2026-10-28 · 执行代理 `E6d`）：`whileInPlayAnim` 的取值口
        // ==================================================================
        /// <summary>一条附魔记录该记哪个 `whileInPlayAnim`（账 **L1044** 收口的那一格）。
        ///
        /// <para>**原版读点** = `EntityScript__HasEnchantmentWhileInPlay()`（`EntityScript.cs:1843-1882`）：
        /// 逐件扫 `activeEnchantments(+0x110)`，读的是**附魔卡自己**的 `RawCardScript.whileInPlayAnim(+0x288)`。</para>
        ///
        /// <para>**本方法的两级**：① 调用方**显式**传了值 ⇒ 用它（原版那条链同样是「记录上的值」）；
        /// ② 没传（= `UnknownWhileInPlayAnim` 哨兵）且附魔卡的卡表**有这一列** ⇒ 读 `CardDef.WhileInPlayAnim`；
        /// ③ 都没有 ⇒ 保持 `-1`（未知，⛔ 不猜 `0`）。</para>
        ///
        /// <para>⚠️ 数据来源是**两张表都算**：可玩池里的附魔卡（`cards_engine.json` 的 `whileInPlayAnim` 列，v37）
        /// 与**第二张表**的模板（`enchantment_templates.json`）—— 后者走
        /// `BattleContext.FindEnchantmentTemplate`（先池后表，Core 不引用 Data，靠静态钩子）。</para></summary>
        static int EnchantmentWhileInPlayAnimOf(CardDef enchantmentCard, int explicitValue)
        {
            if (explicitValue != UnknownWhileInPlayAnim) return explicitValue;
            if (enchantmentCard != null && enchantmentCard.WhileInPlayAnimKnown)
                return enchantmentCard.WhileInPlayAnim;
            return UnknownWhileInPlayAnim;
        }

        // ================================================================================================
        //  🆕 2026-10-28（B89 第 1 波 · 执行代理 `E26` · 简报「真战斗里会不会挂上去」那一问）：
        //  **`buffType 12/15` 附魔「打出即挂」的接线端**
        //  —— 本段全部追加在类尾（⛔ 别插到中间：`账/*.tsv` 按 `文件:行号` 引 `Enchantment.cs`）
        // ================================================================================================
        //  【原版这一跳在哪】`CardScript__AddEffect.c:826-828`：`buffType ∈ {12,15,16}` ⇒ `CardScript__AddEnchantment`
        //    ⇒ 把 `buffEffect.enchantment` 那张模板挂到**被选中的那张牌**的 `activeEnchantments(+0x110)`。
        //    挂载者（`owners`）全池 **114 张卡 / 130 行**（`cards_truth.json` 的
        //    `abilities[].logic[].buff.enchantment.m_PathID`，现读）；模板侧的反向索引在
        //    `enchantment_templates.json` 的 `owners[]`（`{id, buffType, trigger, inPool}`）。
        //
        //  🔶 自己设计（非原版的数据通路）：**本表只是 3 张「已知模板 ↔ 挂载者」的 id 白名单**。
        //    · 为什么必须自己定：**Core 拿不到 `owners[]`** —— `Data/CardDatabase.LoadEnchantmentTemplates()`
        //      物化时只造 `List<CardDef>`、把 `owners` 丢了；`CardDef` 也没有「我由谁挂载 / 我挂的是哪张模板」
        //      的列（`cards_engine.json` v37 同样没有 `enchantment` 列）⇒ 要数据驱动得同时改
        //      生成器 + `CardDef` + `CardDatabase`（都不在本代理的文件所有权内）。
        //    · 覆盖边界（如实）：全池 114 张挂载者里**只接了这 3 张**（`inPool` 的 2 张 + 休眠的 `SW64`）；
        //      其余 111 张**仍未挂**（判据 ↗ 报告 `报告/B89_E26_附魔模板真战斗挂载.md` §三）。
        //    · 谁定的：本波简报（B89 第 1 波 `E26`）「能修就修」授权，2026-10-28。
        //    · 数据列一落地就删本表、改读 `CardDef` 的通用列（⛔ 别在本表上继续加卡 —— 那是把缺口藏起来）。
        // ================================================================================================

        /// <summary>一条「这张牌被打出 ⇒ 给目标挂这张模板」的登记（<see cref="PlayerGrantedEnchantments"/> 的行）。</summary>
        public sealed class PlayedGrantedEnchantment
        {
            /// <summary>挂载者（可玩池里的那张卡）的 `uniqueId`。判据 = 卡池 `CardDef.Id`。</summary>
            public string SourceCardId;
            /// <summary>要挂上去的**附魔模板** `uniqueId`（= `enchantment_templates.json` 的 `id`）。</summary>
            public string TemplateCardId;
            /// <summary>原版 `CardEffect.buffType`（这三张全是 `12 enchantment`）。</summary>
            public int BuffType;
            /// <summary>给人看的说明（日志/断言用）。</summary>
            public string Note;
        }

        /// <summary>🔶 自己设计（非原版）：3 张「打出即挂附魔」的挂载者白名单 —— 见本节头注的「为什么必须自己定」。</summary>
        public static readonly PlayedGrantedEnchantment[] PlayerGrantedEnchantments =
        {
            // `SAU53 Undying Legions`（3 费战术）→ `T16 Sautekh-Undying Legions_enchantment`（`whileInPlayAnim = 1`）。
            //   挂载行 = `cardAbilities[trigger 0].abilityLogic[0].buffEffect{buffType 12, meleeAttackChange 2,
            //   rangeAttackChange 2, enchantment → path_id -5318231028992627218}`。
            new PlayedGrantedEnchantment
            {
                SourceCardId = "SAU53",
                TemplateCardId = "eab54e4c131034e4dacf8ce72807ed07",
                BuffType = BuffTypeEnchantment,
                Note = "Undying Legions（残骸 Shield 的载体；模板自身 ability = trigger 90 → buffType 90 addTraitRemnant shield）",
            },
            // `TL47 Hive Fleet Leviathan`（6 费战术）→ `T18 Leviathan-Hive Fleet Leviathan_enchantment`（`wp = 1`）。
            //   模板**自己 `cardAbilities` 为空** ⇒ 唯一可观察量就是 `HasEnchantmentWhileInPlay`（表现层那一下）。
            new PlayedGrantedEnchantment
            {
                SourceCardId = "TL47",
                TemplateCardId = "a5960475756bec144a1d6d8a9d141fc9",
                BuffType = BuffTypeEnchantment,
                Note = "Hive Fleet Leviathan（模板无 ability ⇒ 只承载 whileInPlayAnim=1）",
            },
            // `SW64 Z-SW-Everstorm`（**不在可玩池**，`manaCost = -1`）→ `Z-SW-Everstorm_enchantment`（`wp = 1`）。
            //   ⚠️ 休眠登记：今天打不出来，但数据在表里（`owners[].inPool = false`）⇒ 保留，免得将来入池再补一遍。
            new PlayedGrantedEnchantment
            {
                SourceCardId = "SW64",
                TemplateCardId = "7c06b8117fe86e74dbc454139da8653b",
                BuffType = BuffTypeEnchantment,
                Note = "Everstorm（休眠：SW64 不在可玩池）",
            },
        };

        /// <summary>这张卡（按 `uniqueId`）有没有「打出即挂附魔」的登记；没有返回 `null`。</summary>
        public static PlayedGrantedEnchantment GrantedEnchantmentOf(CardDef sourceCard)
        {
            if (sourceCard == null || sourceCard.Id == null) return null;
            var t = PlayerGrantedEnchantments;
            for (int i = 0; i < t.Length; i++)
                if (t[i] != null && t[i].SourceCardId == sourceCard.Id) return t[i];
            return null;
        }

        /// <summary>
        /// **一张牌被打出 ⇒ 把它那一族附魔挂到被选中的目标身上**（原版
        /// `CardScript__AddEffect.c:826-828` → `CardScript__AddEnchantment.c` 的等价入口）。
        ///
        /// <para>调用点 = `EffectResolver.PlayTacticCore`（`BroadcastWhen(Play)` 之后、紧挨着
        /// <see cref="OnCarrierPlayed"/>）—— 那里同时拿得到「打出的那一份**实例**」与「玩家点定的**目标**」。</para>
        ///
        /// <para>🔴 **静默条件是【故意的】**：没有登记 / 没有目标 / 查不到模板 ⇒ **一声不响地返回 0**。
        /// 理由：`ruleprobe`（只编 `Core/*.cs`）**没有第二张表**（`EnchantmentTemplatesProvider` 为 null），
        /// 而 `ctx.Log` 会进 `ctx.Events`、`ctx.Events` 进 `Fingerprint` ⇒ 在这里出声会让
        /// `play all dump` / `play check` 的指纹整片变化。⇒ 「没挂上」这件事**由断言钉**
        /// （`Editor/EnchantmentTemplateCheck.cs` §G 的 G2/G3），不靠运行期日志。</para>
        ///
        /// <para>⚠️ 与原版的两处**如实差异**：① 一次只挂**一个**目标（原版 `TL47` 有 3 条 logic、
        /// 其中一条是 `chosenAbility 30 buffOthers` ⇒ 可能多目标）；② 本仓的卡面解析（`desc`）
        /// 对这些卡**本来就没有「选一个目标挂附魔」这一步**，目标来自**落牌那一格**（`PlayTacticCore` 的 `chosen`）。</para>
        /// </summary>
        /// <returns>真挂上了几条（0 = 什么都没做，见上面的静默条件）。</returns>
        public static int AttachGrantedByPlayedCard(BattleContext ctx, CardInstance playedInst, UnitState chosen)
        {
            if (ctx == null || playedInst == null || playedInst.Card == null || chosen == null) return 0;
            var carrier = chosen.Instance;
            if (carrier == null) return 0;
            var rows = GrantedEnchantmentRowsOf(playedInst.Card);   // 🆕 v38（B90/B3）：数据源优先（卡表 `enchantment` 列），没数据才退白名单
            if (rows == null || rows.Count == 0) return 0;
            int attached = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var g = rows[i];
                if (g == null) continue;
                // 🔴 本钩子只负责原版 `AbilityTrigger.ThisCardPlayed = 0` 那一档：`CardScript__AddEffect` 是
                //    「**这条 ability 结算时**」调用的，trigger 20 / 410 / 655 … 各有各的钩子
                //    ⇒ 在本钩子上把它们挂上去 = **时机错**（做多）。⛔ 别为了「覆盖数好看」放宽这一行。
                if (g.Trigger != TriggerThisCardPlayed) { GrantedSkippedNonPlayTrigger++; continue; }
                // 🔴 载体闸：`targetsAffected == 50 (inHandFiltered)` 指的是**选中的那张手牌**，而本钩子的
                //    `chosen` 是**场上单位** ⇒ 挂上去就是挂错人。`DA42` 那一档有专属链
                //    （`AttachDA42ToChosenHandCard`，已接线）⇒ 这里必须让开，⛔ 别把两条链都跑一遍（会双发）。
                if (g.TargetsAffected == TargetsAffectedInHand) { GrantedSkippedHandTarget++; continue; }
                // 同一次出牌里 `(模板, buffType, trigger)` 相同的行只挂一次：`TL47` 有 3 条同模板 logic，
                // 而本钩子只拿得到**一个**目标 ⇒ 重复挂会让 `activeEnchantments` 多出原版不会有的记录。
                bool dup = false;
                for (int k = 0; k < i; k++)
                    if (rows[k] != null && rows[k].Trigger == g.Trigger
                        && rows[k].BuffType == g.BuffType && rows[k].TemplateCardId == g.TemplateCardId)
                    { dup = true; break; }
                if (dup) continue;
                var tmpl = ctx.FindEnchantmentTemplate(g.TemplateCardId);
                if (tmpl == null) { GrantedTemplateMissing++; continue; }   // ⛔ 静默（理由见 doc）；G3 那条断言就是这一格的锁
                var eff = Attach(ctx, carrier, tmpl, g.BuffType,
                                 source: playedInst.Card.Name,
                                 acting: null,
                                 trait: "");   // 明确 `""`（= 没有 trait）：原版 `CardEffect.trait` 取自**挂它的那条 buff**
                if (eff != null) attached++;
            }
            return attached;
        }

        // ==========================================================================================
        //  🆕 v38（2026-10-28 · B90 波 2 · 执行代理 `B3`）：附魔「打出即挂」的**数据驱动**通路
        // ==========================================================================================
        //  【为什么上一波只有 3 张】B89/E26 在这里写了一张 🔶 自己设计的 id 白名单 —— 因为当时
        //    **Core 拿不到「谁挂哪张模板」这份数据**。本波把数据补齐了：
        //    `cards_engine.json` v38 的 `enchantment` 列（per-ability：`trigger` / `buffType` / 模板 id；
        //    反向视图 = `enchantment_templates.json` 的 `owners[]`），由 `Data/CardDatabase` 建成反查表，
        //    再通过 <see cref="GrantedEnchantmentsProvider"/> 注入本文件。
        //    ⇒ <see cref="PlayerGrantedEnchantments"/> **降级为兜底**（数据源没注册 / 表是旧的 /
        //    `ruleprobe` 里读不到资源时用），⛔ 别在白名单上继续加卡（那是把缺口藏起来）。
        //  📌 判据 → `CardScript__AddEffect.c:826-828`（`buffType ∈ {12,15,16}` ⇒ `AddEnchantment`）；
        //    数据口径 → `工具/gen_cards_engine.py` 的 `enchantment_grants_of` 上面那段长注释。
        //  📌 本段全部追加在**类尾**（`账/*.tsv` 按行号引用 `Enchantment.cs:1551…:1624`，⛔ 别插到中间）。
        // ==========================================================================================

        /// <summary>「这张牌被打出 ⇒ 给目标挂这张模板」的**数据行**（来源 = 卡表 `enchantment` 列）。</summary>
        public sealed class GrantedEnchantmentRow
        {
            /// <summary>挂载者的 `uniqueId`（= 卡池 `CardDef.Id`）。</summary>
            public string SourceCardId;
            /// <summary>要挂上去的**附魔模板** `uniqueId`（= `enchantment_templates.json` 的 `id`）。</summary>
            public string TemplateCardId;
            /// <summary>原版 `CardEffect.buffType`（`12` / `15` / `16` 三档才会走到 `AddEnchantment`）。</summary>
            public int BuffType;
            /// <summary>原版 `CardAbility.trigger`（`AbilityTrigger`；`0 = ThisCardPlayed` = 打出即挂）。</summary>
            public int Trigger;
            /// <summary>原版 `targetCriteria.targetsAffected`（`-1` = 读不到）。**载体是谁**由它定：
            /// `50 = inHandFiltered`（选中的手牌）⇒ 那种行**不走本钩子**（见 `AttachGrantedByPlayedCard` 的闸）。</summary>
            public int TargetsAffected = -1;
            /// <summary>`cardAbilities` 下标（排查用，⛔ 不参与判据）。</summary>
            public int AbilityIndex = -1;
            /// <summary>`abilityLogic` 下标（同上）。</summary>
            public int LogicIndex = -1;
        }

        /// <summary>`AbilityTrigger.ThisCardPlayed = 0` —— 本文件唯一认的「打出即挂」时机。</summary>
        public const int TriggerThisCardPlayed = 0;

        /// <summary>`TargetsAffected.inHandFiltered = 50` —— 载体是**选中的那张手牌**，不是场上单位。
        /// <para>`DA42 Reconnaissance Mission` 就是这一档，它有自己的专属链
        /// <see cref="AttachDA42ToChosenHandCard"/>；本钩子的 `chosen` 是场上单位 ⇒ 挂上去就是**挂错人**。</para></summary>
        public const int TargetsAffectedInHand = 50;

        /// <summary>数据源钩子（`Data/CardDatabase` 的静态构造里登记）：`卡 uniqueId → 数据行`。
        /// <para>`null` = 没人注册（`ruleprobe` 里就是这个状态）；返回 `null` / 空 = 数据里没有这张卡。</para>
        /// <para>⚠️ 返回的是数据源自己的列表（⛔ 调用方不许改）。</para></summary>
        public static System.Func<string, List<GrantedEnchantmentRow>> GrantedEnchantmentsProvider;

        /// <summary>诊断计数（**不写 `ctx.Log`** —— 那会进 `ctx.Events`、进而改对局指纹）：
        /// 「数据行的 trigger 不是 0，本钩子没挂」的次数。给断言读，⛔ 不参与任何判据。</summary>
        public static int GrantedSkippedNonPlayTrigger;

        /// <summary>诊断计数：数据行指向的模板在本局**查不到**（多半是第二张表没加载）的次数。</summary>
        public static int GrantedTemplateMissing;
        /// <summary>诊断计数：数据行的载体是**手牌**（`targetsAffected == 50`）⇒ 本钩子不挂（有专属链）。</summary>
        public static int GrantedSkippedHandTarget;

        /// <summary>这张卡**全部**「挂附魔」数据行：① 数据源；② 数据里没有它（或没有数据源）⇒ 退白名单。
        /// <para>顺序有语义：数据源是权威，<see cref="PlayerGrantedEnchantments"/> 只是那 3 张的兜底。</para></summary>
        public static List<GrantedEnchantmentRow> GrantedEnchantmentRowsOf(CardDef sourceCard)
        {
            if (sourceCard == null || string.IsNullOrEmpty(sourceCard.Id)) return null;
            var p = GrantedEnchantmentsProvider;
            if (p != null)
            {
                var fromData = p(sourceCard.Id);
                if (fromData != null && fromData.Count > 0) return fromData;
            }
            var w = PlayerGrantedEnchantments;
            for (int i = 0; i < w.Length; i++)
            {
                if (w[i] == null || w[i].SourceCardId != sourceCard.Id) continue;
                var one = new List<GrantedEnchantmentRow>(1);
                one.Add(new GrantedEnchantmentRow
                {
                    SourceCardId = w[i].SourceCardId,
                    TemplateCardId = w[i].TemplateCardId,
                    BuffType = w[i].BuffType,
                    Trigger = TriggerThisCardPlayed,       // 白名单这 3 张全是 `trigger 0`（ThisCardPlayed）
                    AbilityIndex = -1,
                    LogicIndex = -1,
                });
                return one;
            }
            return null;
        }
    }
}
