// UnitState.cs — 场上的一个单位（可变实例状态）
//
// 和 `CardDef`（不可变的卡牌定义）分开：同一张卡多次上场是两个独立的 UnitState，
// 掉血/疲劳/增益互不影响。
//
// 字段语义对齐 `rule_core.gd` 的 `_make_unit`。
using System.Collections.Generic;

namespace RuleEngine
{
    public class UnitState
    {
        public readonly CardDef Card;
        public readonly string Name;
        public readonly bool IsWarlord;

        public int Attack;            // 当前近战攻击力
        public int RangedAttack;      // 当前远程攻击力
        public int Armor;             // 伤害减免（最低 1）。**只有 Armour X 关键词给护甲**
        public int Health;
        public int MaxHealth;

        public bool Exhausted;        // 本回合是否已行动（部署当回合 = true）
        public bool IsStunned;
        /// <summary>
        /// **职责已经用过了**（2026-09-13 A2）—— 规则书 `:181`「职责：**一次性能力**；
        /// 可由其他卡牌效果**装填**再次使用」。
        ///
        /// ⚠️ 它**不随回合重置**（和 <see cref="Exhausted"/> 正好相反）—— 这是本局一次的标记。
        /// 装填（`Reload the Duty abilities of all your units`）把它清回 false ——
        /// ✅ **2026-09-13 A4 已实现**（`EffectResolver.DoReloadDuty`）。
        /// </summary>
        public bool DutyUsed;

        /// <summary>
        /// **正在祈祷**（2026-09-13 A4 批 1）—— 执行过 `Pray` 替代行动的单位，**按回合重置**。
        ///
        /// 出处：规格书 `rule_core.gd:2371`（`u["prayed"] = true`，在 `Pray` 那一点）·
        ///       `:1953`（`u["prayed"] = false`，和 `exhausted` / `attacks_turn` 同一批清）。
        /// 卡面两处：`Each friendly unit that is Praying heals 3`（`Devout Serenity`）·
        ///           `If any friendly unit is Praying, …`（`Sororitas Rhino`）。
        /// ⚠️ 它和 `When a friendly unit Prays` **不是一回事**：那个是**事件**（发生的那一下），
        ///    这个是**状态**（本回合一直挂着，回合开始才掉）。两张卡各要各的。
        /// </summary>
        public bool Prayed;

        /// <summary>
        /// **下一次用狂暴时不回牌库**（2026-09-14 A4 批 3）—— 卡面
        /// `The next time it uses Ferocity this turn, it stays in play`（`Bjorn's Shrine`，SpaceWolves）。
        ///
        /// 🔴 **语义出处（反编译里唯一有完整体的一条）**：`CardScript__UsedActiveAbility.c:52-64` ——
        ///    `has(ferocity)` → **广播** → `has(dontReturnFerocity)`（`DefinedTrait:131 = 1270`）
        ///    **或** `EnoughPendingDamageToDie` → 才跳过回牌库；否则 `AddRecallToDeck`。
        ///    `:75-88` 用完之后 `SendRemoveEffect(..., 1)` 把它**摘掉** ⇒ **一次性**。
        /// ⚠️ **「一次」和「本回合」两个修饰都要**（`Exhausted` 那套按回合清，表达不了「下一次」）：
        ///    消费点在 `RuleCore.UseAlternative` 的狂暴那一段（**用掉就清**），
        ///    兜底复位在 <see cref="RefreshForNewTurn"/>（「本回合」过了就没了）。
        /// ⚠️ 别和 `SW42 Bjorn the Fell-Handed` 的 `When a friendly unit uses Ferocity, it stays in play`
        ///    （**常驻、无 `next time`、无 `this turn`**）搞混 —— 那是**另一张卡、另一条语义**。
        /// </summary>
        public bool FerocityStay;

        /// <summary>
        /// **压在下面那几张牌**（虫群合并来的，2026-09-13 A2）。规则书 `:216`「置于其下」。
        /// 宿主进弃牌堆时它们**一起进**（`RuleCore.CleanupDeaths`）—— 物理上就是「压在下面」。
        /// </summary>
        public readonly List<CardDef> SwarmUnder = new List<CardDef>();

        /// <summary>
        /// **这是一具残骸**（`Remnant`，2026-09-13 A2）—— 规则书 `:203`
        /// 「本部队死亡时**翻面**表示残骸；残骸**受伤害或控制者回合结束时被摧毁**」。
        ///
        /// 残骸是**留在格位上的一个单位**（原版在场上是一个独立的 3D 体：
        /// `BattleCardUI.CreateRemnantBody` / `RemnantBody3D`，`BattleManager.AddTransformIntoRemnant`），
        /// 所以：占着格位、攻 0、**1 点生命**（挨任何一下就没）、不能行动。
        /// ⚠️ 它是**背面朝上的牌**，没有任何能力 ⇒ 被摧毁时**不再触发**它自己的
        ///    `Backlash` / `Unstable`（那些在它「死」的那一次已经触发过了）。
        /// </summary>
        public bool IsRemnant;

        /// <summary>
        /// **面朝下**（`Ambush`，2026-09-13 A2）—— 规则书 `:166`「伏击：**面朝下打出**；
        /// **下次回合前**若被伤害：翻开**无效果**；若未被伤害：翻开**并触发效果**」。
        ///
        /// 两条出口（都在 `RuleCore` 里，判据各只一处）：
        ///   · 挨到**实际伤害** → <see cref="RuleCore.ApplyDamage"/> 里翻开，**不触发**；
        ///   · 撑到自己**下一个回合开始** → <see cref="RuleCore.RevealAmbush"/> 翻开并触发。
        ///
        /// ⚠️ **我们不做「藏起来」** —— 引擎里双方都看得见对方场上是什么（全工程没有隐藏信息这一层），
        ///    这里只实现**时机**那一半。表现层要盖张卡背是它自己的事。
        /// </summary>
        public bool FaceDown;

        public bool HasShield;        // 抵挡下一次伤害后失去

        /// <summary>
        /// 失明（规则书 :166「本单位失明期间**远程攻击设为 0**」）。
        ///
        /// ⚠️ 和 `stun` 一样，原版是**独立的布尔状态字段**（`rule_core.gd:239` 的 `blind`），
        ///    不是关键词 —— 它靠 `_damage_unit` 那族置位（`:2814`）、`field_attack` 之外读它。
        ///    出处：原版 `:4212` `if is_ranged and bool(attacker.get("blind", false)): return ERR_NO_ATTACK`。
        /// </summary>
        public bool IsBlind;

        /// <summary>
        /// **狂喜已经触发过**（2026-09-14）—— 对照参考实现 `rule_core.gd:4447` 的
        /// `u["_ecstasy_fired"]`（原话：「**首次越线触发一次防重复**」）。
        ///
        /// 🔴 **这一位不能省**：`RuleCore.Hurt` 的判据是「生命 ≤ X 且未死」——
        ///    没有它的话，一个 2 血、`Ecstasy 2` 的单位**每挨一次打都会再触发一次**
        ///    （原版是**一辈子一次**）。判据与落点在 `RuleCore.Hurt`。
        /// </summary>
        public bool EcstasyFired;

        /// <summary>
        /// 失明的**到期回合**（`blind_turn_end`，原版 `rule_core.gd:2815`）。
        /// `-1` = 没有失明。语义是「到**施放者自己的下个回合开始**时清」——
        /// 也就是撑过对手的一整个回合（卡面写 `until your next turn`）。
        /// ⚠️ 原版清除时读的是**另一个字段名**（`:1962` 读 `blind_turn`、`:2815` 写 `blind_turn_end`），
        ///    所以原版这条清除**从来没生效过**（`blind` 一旦中上就永久）。我们按卡面语义实现，不照抄这个笔误。
        /// </summary>
        public int BlindTurnEnd = -1;

        /// <summary>失明是谁施放的（0/1）—— 到期按**他的**回合算（见 <see cref="BlindTurnEnd"/>）。
        /// ⚠️ 不能靠「回合号的奇偶」推施放者：那种假设在本工程里没有依据（回合所有权是可变的）。</summary>
        public int BlindOwner = -1;

        public int AttacksThisTurn;   // 重置于回合开始

        /// <summary>🆕 2026-09-16 **「某个机制的触发再发生 N 次」的额度**（按关键词分开记）。
        ///
        /// 卡面只有两句在用（都是**监听别的单位**的卡写在事件层里的）：
        ///   · `When a friendly unit triggers Mob, it triggers an additional time`（`GOF_Big_Choppa_Nob`）
        ///   · `When this unit triggers Synapse, it applies the effect twice`（`TL30 Broodlord`）
        /// 语义：**监听者**在广播里给**事件主语**（也就是那个正在触发机制的单位）挂一份额度，
        /// 机制自己跑完之后**就地消费**（`RuleCore.TakeExtraTrigger`）——
        /// 所以「再触发一次」是**这一次**的事，不跨时机、不跨回合。
        ///
        /// ⚠️ **必须是「就地消费」**：`RuleCore.DeclareAttack` 的 Mob 那一段与
        ///    `EffectResolver.RepeatTacticOnAdjacent` 的 Synapse 那一段各自消费自己那一份。
        ///    写成一个共享的 bool 就会串机制（Mob 的额度被 Synapse 吃掉）。
        /// ⚠️ `RefreshForNewTurn` 里**清空**只是兜底（正常路径用完就摘了）——
        ///    留着会让「上一次没消费掉的额度」在下一回合突然生效，那比没有更糟。
        /// </summary>
        public readonly Dictionary<string, int> ExtraTriggers = new Dictionary<string, int>();

        /// <summary>🆕 2026-09-16 **本回合激活过几次誓约（Oath）能力** —— 重置于回合开始。
        ///
        /// 和 `AttacksThisTurn` 分开：誓约是**独立的一次激活**，**不占单位那次行动**
        /// （原版 `CanUseOathAbility` 与「本回合已行动」是两套判据，扣的也是别的计数器：
        ///  `CardScript` 的 `+0x50` 每次激活 ++、`+0x4c` 置 1，回合末清零 ——
        ///  `decomp_out/CardScript__ResolveActiveAbilityPlayed.c:31-32` +
        ///  `CardScript__OnTurnEnd.c:209`）。
        /// 上限默认 1；场上有 `oathTripleActivation` 的友方卡时是 3
        /// （`CardScript__CanUseOathAbility.c:16-20`）。
        /// </summary>
        public int OathUsesThisTurn;

        /// <summary>🆕 2026-09-16 **这张牌上场的回合号**（`ctx.Turn`；没上场过 = -1）。
        ///
        /// 用途：原版誓约能力的默认限制是「**本回合部署的才能激活**」
        /// （`CardScript__IsTheSameTurnPlayed.c:24-38`），
        /// 由 `oathInAllTurns` 豁免（`CardScript__CanUseOathAbility.c:8`）。
        /// 全仓原来**没有任何「部署回合」字段**，这是唯一一处写点（`RuleCore.PlayCard` / `DeployFree`）。
        /// </summary>
        public int DeployedTurn = -1;

        readonly Dictionary<string, int> _keywords;

        public UnitState(CardDef card, bool isWarlord)
        {
            Card = card;
            Name = card != null ? card.Name : "?";
            IsWarlord = isWarlord;

            Attack = card != null ? card.Attack : 0;
            RangedAttack = card != null ? card.RangedAttack : 0;
            Health = card != null ? card.Health : 0;
            MaxHealth = Health;

            _keywords = new Dictionary<string, int>();
            if (card != null)
            {
                foreach (var kv in card.Keywords) _keywords[kv.Key] = kv.Value;
            }
            // 护甲只有 Armour X 关键词这一个来源。
            // ⚠️ JSON 里的 `armor` 字段经核实实为**远程攻击**（OCR 误读），已在数据层迁移到 RangedAttack
            Armor = KwValue(KeywordTable.Armour);

            // 部署当回合不可行动 —— 除非带**迅捷 / 侧翼 / 狂暴**。
            // 规则书 :98「部署当回合不能行动（除非注明，如迅捷/侧翼/狂暴）」、
            // :187「侧翼：打出当回合可攻击任意敌方部队」；原版 `rule_core.gd:2248`
            // 把这两个写在同一句里（`fast` / `flank` → `exhausted = false`）。
            // ⚠️ **狂暴（`ferocity`）是 2026-09-13 A2 补进来的** —— `:98` 那句话里
            //    本来就点着它（「如迅捷/侧翼/狂暴」），原版同一处也把它和 fast/flank 并列，
            //    只是我们先前没实现这个关键词。慢的 `pray` **不在**这一行（`:198`）。
            Exhausted = !(Has("fast") || Has("flank") || Has(KeywordTable.Ferocity));
            HasShield = Has(KeywordTable.Shield);
        }

        public bool Has(string keyword) { return _keywords.ContainsKey(keyword); }

        /// <summary>
        /// 这个单位**当前**身上有哪些关键词（含加/减益、光环给的、限时增益）—— 只读。
        ///
        /// ⚠️ **别拿它当「卡面印的关键词」用**：那是 `Card.Keywords`（卡模板上的、不变的）。
        /// 表现层要画「场上这张卡现在挂了什么 buff/debuff」时用的**是这一个**
        /// （原版 `BattleCardUI.UpdateTraitIcons` 走 `EntityScript.GetCurrentTraitValueWithModifiers`）。
        /// ⚠️ 枚举顺序**不稳定**（`Dictionary`）—— 要稳定的显示顺序得自己排，别依赖它。
        /// </summary>
        public IReadOnlyDictionary<string, int> Keywords { get { return _keywords; } }

        /// <summary>场上单位的**主动技能**（来自卡的 `Ability:`）。没有就是 null</summary>
        public EffectSpec Ability { get { return Card != null ? Card.Ability : null; } }

        public bool HasAbility { get { return Ability != null; } }

        /// <summary>
        /// 这个关键词对应的**触发效果**（**封闭文法**那条：`Card.Effect`，我们自己设计的那 26 张卡）。
        /// 没有返回 null —— 调用方必须判。
        /// ⚠️ **原版卡那一族不走这里**：它们的正文是 `Card.TriggerOps` / <see cref="FxOps"/> 的 op。
        /// </summary>
        public EffectSpec Effect(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || Card == null) return null;
            return Card.Effect(keyword);
        }

        // ---- 运行时补上的「触发正文」（`Give "💀 Backlash: …" to a friendly troop`）----
        //
        // 原版把这种段落当**卡片自带的一段效果文字**存着，靠 `desc.contains(...)` 现搜现解；
        // 我们没有卡片定义可写（那是不可变的 `CardDef`），所以在单位身上挂一份。
        // 判据：**卡上原生的那条优先**（那是这张卡自己的设计），没有才用后挂上去的。
        //
        // 🔴 **2026-09-14 改**：原来这里存的是 `EffectSpec`（**封闭文法** —— 那是给
        //    我们**自己设计的 26 张卡**写的极小文法，只认 `Damage/Heal/Draw`）。
        //    而挂上来的正文是**原版卡面原文**（`Return to your hand` / `Trigger this troop's
        //    Codex ability` / `Lower the cost of a random troop in hand by 1` …）—— 一条都解不了。
        //    表现：那一族（全池 **10 张**）**解析得出、有机制、但永远不会发生**，
        //    而且 `GivePayload.Mechanized` 报的「嵌入的效果文字解析不了」**报表看得见、玩家看不见**。
        //    ⇒ 改成存 **`EffectText.Parse` 出来的 op** —— 和卡上原生正文**同一台解析器、同一条
        //    结算路径**（`RuleCore.FireTriggerAt` 里那条 `ResolveOps`）。判据只有一份。
        readonly Dictionary<string, List<EffectOp>> _grantedOps = new Dictionary<string, List<EffectOp>>();
        /// <summary>挂上去的那份**正文原文**（日志与卡面要印它）</summary>
        readonly Dictionary<string, string> _grantedText = new Dictionary<string, string>();

        public void GrantOps(string keyword, List<EffectOp> ops, string text)
        {
            if (string.IsNullOrEmpty(keyword) || ops == null || ops.Count == 0) return;
            _grantedOps[keyword] = ops;
            _grantedText[keyword] = text ?? keyword;
        }

        public bool HasGrantedEffect(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && _grantedOps.ContainsKey(keyword);
        }

        /// <summary>挂上去的那份正文原文。没挂过返回 null。</summary>
        public string GrantedText(string keyword)
        {
            string s;
            return (keyword != null && _grantedText.TryGetValue(keyword, out s)) ? s : null;
        }

        /// <summary>
        /// 这个关键词**能结算的正文**（按 `FireTriggerAt` 的取法）：卡上原生的优先，
        /// 没有就用运行时挂上去的那份。两处都没有 → null。
        /// 🔴 **全仓只此一处判据** —— 原来这条「原生 or 挂的」被散在 5 个地方各写一遍。
        /// </summary>
        public IReadOnlyList<EffectOp> FxOps(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            var own = Card != null ? Card.TriggerOps(keyword) : null;
            if (own != null) return own;
            List<EffectOp> g;
            return _grantedOps.TryGetValue(keyword, out g) ? g : null;
        }

        /// <summary>这个关键词**有没有任何**可结算的东西（原生正文 / 挂上去的正文 / 封闭文法那条）。
        /// 「触发了」和「触发了但那条关键词压根没有正文」必须分得开（红线）。</summary>
        public bool HasFx(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            if (Card != null && (Card.TriggerOps(keyword) != null || Card.Effect(keyword) != null)) return true;
            return _grantedOps.ContainsKey(keyword);
        }

        /// <summary>
        /// 挂一份**光环给的**触发正文（`Slay: Gain Blood Thirst this turn`，A7）。
        /// 和 <see cref="GrantOps"/> 只差一件事：**记进光环那本账**，好让重算时精确收回。
        /// ⚠️ 收回时只把**光环挂的**那一份摘掉 ——
        ///    别的效果（`Give "💀 Backlash: …"`）挂的不能被连带清掉。
        /// </summary>
        public void GrantAuraOps(string keyword, List<EffectOp> ops, string text)
        {
            if (string.IsNullOrEmpty(keyword) || ops == null || ops.Count == 0) return;
            _grantedOps[keyword] = ops;
            _grantedText[keyword] = text ?? keyword;
            _auraFx.Add(keyword);
        }

        readonly HashSet<string> _auraFx = new HashSet<string>();

        /// <summary>带了这个关键词、且它带的效果文字解析不出来 → 卡面要标 `*`</summary>
        public bool EffectUnparsed(string keyword)
        {
            if (Card == null || !Card.Has(keyword)) return HasGrantedEffect(keyword) == false;
            if (keyword == KeywordTable.Ability) return Card.HasAbility == false;
            return Card.Effect(keyword) == null;
        }

        public int KwValue(string keyword)
        {
            int v;
            return _keywords.TryGetValue(keyword, out v) ? v : 0;
        }

        /// <summary>关键词授予/叠加（`rule_core._apply_gain:3292`：`kws[name] += val`）。
        /// ⚠️ `armour`/`shield`/`stun` 三个还要**同步状态字段** —— 引擎别处是按字段结算的，
        /// 只加 kws 不改字段 = 给了护甲却不减伤（原版 `:3293-3299` 专门补过这个 bug）。</summary>
        public void AddKeyword(string keyword, int value)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords[keyword] = KwValue(keyword) + value;

            if (keyword == KeywordTable.Armour) Armor += value;
            if (keyword == KeywordTable.Shield && value > 0) HasShield = true;
            if (keyword == "stun" && value > 0) IsStunned = true;
            // 🆕 2026-09-14 A6 族 C：**`blind` 原来不在这里** —— 而真正生效的是 `IsBlind`
            //    （`RuleCore.FieldAttack` 直接读它：失明期间远程攻击力视为 0，规则书 `:166`）。
            //    不补这一行的话，`give them Blind` 会**给了关键词却什么都没发生**
            //    （`Has("blind")` 为真、远程照打）—— 典型的静默失效。
            //    ⚠️ 和 `DoBlind`（动词那条路）**同一个字段**，判据只有一份。
            if (keyword == "blind" && value > 0) IsBlind = true;
        }

        /// <summary>移除关键词（`lose X` 用）。**值降到 0 以下就摘掉**</summary>
        public void RemoveKeyword(string keyword, int value = 1)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            int now = KwValue(keyword) - value;
            if (now > 0) _keywords[keyword] = now;
            else RemoveAll(keyword);
        }

        /// <summary>
        /// **整个摘掉**（不管叠了几层）。
        /// 规则书里明确说「移除全部」的地方必须用这个 —— 例：`Markerlight X`
        /// 「受远程伤害后**移除全部**标记光」（:192）；用 `RemoveKeyword` 只会减一层。
        ///
        /// ⚠️ 摘掉关键词时**连带撤掉它当初授予的属性增益** —— 见 <see cref="PendingGrants"/>。
        /// </summary>
        public void RemoveAll(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords.Remove(keyword);
            if (keyword == KeywordTable.Armour) Armor = 0;
            // 🆕 2026-09-14 A6 族 C：`blind` 的**状态字段**也要跟着摘
            // （`AddKeyword` 那边补了置位，这里是它的对偶；限时增益到期走
            //  `TempBuff` → `RemoveKeyword` → 这里，所以「到你的下回合」也能正确解除）。
            if (keyword == "blind") IsBlind = false;
            // 🆕 2026-09-16：**授予的正文也要一起撤**（`GrantOps` 那一本账）。
            //   ⚠️ 不撤的话：关键词被摘掉了，`_grantedOps` 还在 ⇒ `RuleCore.FireTriggerAt`
            //   （它的门在 `FxOps` 里、**不在触发点**）照样找得到正文 ⇒
            //   表现是「**关键词没了、效果照放**」。
            //   限时嵌入效果（`Give "Slay: …"` **this turn**）刚接上 `TempBuff` 就会踩到这一条 ——
            //   光环那条路（`Recompose`）本来就在清，这里补上的是**普通路**。
            _grantedOps.Remove(keyword);
            _grantedText.Remove(keyword);
            RevertGrantsOf(keyword);
        }

        // ---- 关键词「带出来的」属性增益 ----------------------------------
        //
        // 有些关键词一旦授予就会**顺手改属性**（原版 `KW_GRANT` 那类）。
        // 记下来是为了「关键词被移除时能把增益一起撤走」——
        // 否则 `+2 Melee Attack` 会永远留在身上（标掉了、值还在 = 静默不一致）。
        public class GrantRecord
        {
            public string Keyword;
            public string Attr;        // attack / ranged / health / armour
            public int Value;
            /// <summary>谁给的（黑暗契约的种类名 / 卡名）。用来「整份收回」——见 <see cref="RevertGrantsFrom"/></summary>
            public string Source;
        }

        readonly List<GrantRecord> _grants = new List<GrantRecord>();
        public IReadOnlyList<GrantRecord> PendingGrants { get { return _grants; } }

        /// <summary>
        /// 记一条「这个属性增益是谁给的」，**并立刻加上去**。
        /// `keyword` 非空 = 是某个关键词带出来的（关键词被移除时一起撤）；
        /// `source` = 给予者（比如黑暗契约的种类名），用来在来源被替换时整份收回。
        /// </summary>
        public void RecordGrant(string attr, int value, string source = null, string keyword = null)
        {
            var g = new GrantRecord { Keyword = keyword, Attr = attr, Value = value, Source = source };
            _grants.Add(g);
            ApplyGrant(g, +1);
        }

        /// <summary>撤掉某个关键词带出来的全部属性增益</summary>
        void RevertGrantsOf(string keyword)
        {
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                if (g.Keyword != keyword) continue;
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
            }
        }

        /// <summary>撤掉**某一个来源**（例如被替换掉的那份黑暗契约）留下的全部属性增益</summary>
        public void RevertGrantsFrom(string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                if (g.Source != source) continue;
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
            }
        }

        void ApplyGrant(GrantRecord g, int sign)
        {
            int v = g.Value * sign;
            switch (g.Attr)
            {
                case "attack": Attack += v; break;
                case "ranged": RangedAttack += v; break;
                case "health":
                    Health += v;
                    MaxHealth = System.Math.Max(1, MaxHealth + v);
                    break;
                case "armour": Armor = System.Math.Max(0, Armor + v); break;
            }
        }

        // ---- 光环加成（2026-09-14 A7）--------------------------------------
        //
        // 光环是**持续**加成：来源在场就有效、离场就收回，而**重算**是「整份摘掉再重加」
        // （照原版 `CardScript__UpdateWhileInPlay`，见设计稿 §8.2/§8.4）。
        // ⇒ 每次重算都要先**精确地**把上一次给的那一份收回来。
        //
        // 🔴 **为什么不能直接用 `AddKeyword` / `RemoveAll` 收**：
        //    `RemoveAll` 是**整个摘掉**（不管叠了几层），而且 `armour` 那条会把 `Armor` **直接清零**。
        //    光环要收的**只是自己那一份**：
        //      · `Baneblade Tank` 自己印着 `Armour 2`，旁边再来一个 `Armour 1` 光环
        //        ⇒ 收回时**只能减 1**，`RemoveAll` 会把它自己的 2 点也抹掉（静默变脆）。
        //      · 同一个单位被**两个**光环加同一个关键词时，得**一人一份**地收。
        //    ⇒ 单开一本账（`_auraKw`）。
        //
        // ⚠️ 属性那一份**不在这里**，走 `RecordGrant(..., source: Auras.GrantTag)` /
        //    `RevertGrantsFrom` —— 那套「整份收回」的机器本来就有（黑暗契约在用），别写第二份。

        readonly Dictionary<string, int> _auraKw = new Dictionary<string, int>();

        /// <summary>光环（A7）给这个单位加的关键词点数。报表与自检要看它</summary>
        public int AuraKwValue(string keyword)
        {
            int v;
            return _auraKw.TryGetValue(keyword, out v) ? v : 0;
        }

        /// <summary>本回合单位身上**由光环给的**关键词（只读，报表用）</summary>
        public IReadOnlyDictionary<string, int> AuraKeywords { get { return _auraKw; } }

        /// <summary>
        /// 加一份**光环给的**关键词。语义同 <see cref="AddKeyword"/>（含那三个要同步状态字段的），
        /// 额外记进 `_auraKw` 这本账，好让 <see cref="ClearAuraGrants"/> 精确收回。
        /// </summary>
        public void AddAuraKeyword(string keyword, int value)
        {
            if (string.IsNullOrEmpty(keyword) || value <= 0) return;
            _keywords[keyword] = KwValue(keyword) + value;
            _auraKw[keyword] = AuraKwValue(keyword) + value;

            // ⚠️ 和 `AddKeyword` 一样**必须同步状态字段** —— 引擎别处是按字段结算的，
            //    只加 `_keywords` 不改字段 = 「给了护甲却不减伤」（原版 `:3293-3299` 专门补过这个 bug）。
            if (keyword == KeywordTable.Armour) Armor += value;
            if (keyword == KeywordTable.Shield) HasShield = true;
            if (keyword == "stun") IsStunned = true;
        }

        /// <summary>
        /// 把**光环给的那一份**整份收回来（关键词 + 属性）。
        /// 单位自己的、别的效果给的，一律不动 —— 见上面那段 🔴。
        /// **幂等**：没有光环加成时什么都不做。
        /// </summary>
        public void ClearAuraGrants()
        {
            if (_auraKw.Count > 0)
            {
                foreach (var kv in _auraKw)
                {
                    string kw = kv.Key;
                    int n = kv.Value;
                    int now = KwValue(kw) - n;
                    if (now > 0) _keywords[kw] = now;
                    else _keywords.Remove(kw);
                    if (kw == KeywordTable.Armour) Armor = System.Math.Max(0, Armor - n);
                }
                _auraKw.Clear();
            }
            // 光环挂的**触发效果**也一并摘（`Beastboss on Squigosaur` 给友方野兽挂的 `Slay`）——
            // ⚠️ 只摘光环挂的那几个，`Give "💀 Backlash: …"` 那种效果挂的不动
            if (_auraFx.Count > 0)
            {
                foreach (string kw in _auraFx) { _grantedOps.Remove(kw); _grantedText.Remove(kw); }
                _auraFx.Clear();
            }
            // 属性那一份：`RecordGrant` 记的账，按来源整份撤（黑暗契约用的是同一台机器）
            RevertGrantsFrom(Auras.GrantTag);
            // 障碍标记也要清 —— 它**不一定**伴随关键词/属性（`Nemesor Zahndrekh` 那条只置这个标记），
            // 所以**无条件**清，不能塞在上面那个 `if` 里
            AuraRemnantStay = false;
        }

        /// <summary>
        /// **这一个残骸不会被「回合结束时摧毁」**（`Nemesor Zahndrekh` 的
        /// `Adjacent Remnants do not disappear at the end of your turn`，A7）。
        /// 由 `Auras.Recompose` 置、`Auras` 重算时清；**读点只此一处**：`RuleCore.DestroyRemnants`。
        /// ⚠️ 它是**光环给的**，所以来源离场后就该变回 false —— 别在这儿写死。
        /// </summary>
        public bool AuraRemnantStay;

        // ---- 限时增益（原版 `temp_buffs`，`rule_core.gd:3300`）----
        //
        // 一条 = 一次**带时长**的施加。到期按两条规则撤：
        //   · `this turn`            → **本回合结束时**撤（不管谁的回合）
        //   · `until your next turn` → **施放者自己的下个回合开始时**撤
        // 出处：`_resolve_text:3018-3019`。
        public class TempBuff
        {
            public bool IsKeyword;
            public string Name;              // 属性名（attack/health/armour/ranged）或关键词名
            public int Value;
            public int Owner;                // 施放者玩家号（`until your next turn` 按他的回合算）
            public bool UntilMyNextTurn;     // true = 施放者的下回合开始撤；false = 本回合结束撤
            public string Src;               // 来源卡名（日志用）
            /// <summary>
            /// **施加它的那张卡的卡名**（2026-09-13 A4 批 1 加）。
            ///
            /// ⚠️ 和 <see cref="Src"/> **不是一回事，别合并**：`Src` 填的是结算层传进来的 `by`，
            ///    而**战术卡那条路 `by` 恒为「战术卡」**（`EffectResolver.ResolveOps` 里那句
            ///    `source != null ? source.Name : "战术卡"`）⇒ 按 `Src` 撤销会把**别的战术卡**的
            ///    限时增益一起撤掉。付费修饰型激活（`Extend effect until your next turn`）要的是
            ///    「**本卡**施加的那些」—— 所以单独记一个真卡名（取自 `ctx.PlayingCard`）。
            /// </summary>
            public string SourceCard;
        }

        readonly List<TempBuff> _buffs = new List<TempBuff>();
        public IReadOnlyList<TempBuff> TempBuffs { get { return _buffs; } }
        public void AddTempBuff(TempBuff b) { _buffs.Add(b); }

        /// <summary>
        /// 撤掉到期的限时增益。
        /// </summary>
        /// <param name="atTurnEnd">true = 正在「回合结束」；false = 正在「某玩家回合开始」</param>
        /// <param name="player">回合开始/结束时，是**哪个玩家**的回合</param>
        /// <returns>撤掉了几条</returns>
        public int RevertBuffs(bool atTurnEnd, int player)
        {
            int n = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var b = _buffs[i];
                bool due = atTurnEnd ? !b.UntilMyNextTurn : (b.UntilMyNextTurn && b.Owner == player);
                if (!due) continue;
                _buffs.RemoveAt(i);
                if (b.IsKeyword) RemoveKeyword(b.Name, b.Value);
                else
                {
                    switch (b.Name)
                    {
                        case "attack": Attack -= b.Value; break;
                        case "ranged": RangedAttack -= b.Value; break;
                        // 生命是**上限也跟着变**的（原版 `:3317` 同步 max_health），撤的时候一起回
                        case "health": Health -= b.Value; MaxHealth -= b.Value; break;
                        case "armour": Armor = System.Math.Max(0, Armor - b.Value); break;
                    }
                }
                n++;
            }
            return n;
        }

        public bool IsAlive { get { return Health > 0; } }

        /// <summary>
        /// 撤掉**某一张卡**施加的全部限时增益（不看有没有到期）。返回撤掉几条。
        ///
        /// 出处：规格书 `rule_core.gd:1777 _undo_temp_buffs_src(ctx, src)` —— 付费修饰型激活
        /// （`6 [Energy]: Extend effect until your next turn` / `8 [Energy]: Give it permanently`）
        /// 要先**撤销基础效果**，再用新时长重结算一遍。
        ///
        /// ⚠️ 撤销的**动作**和 <see cref="RevertBuffs"/> 是同一件事（关键词走 `RemoveKeyword`、
        ///    属性按名字回减、生命连上限一起回）—— 两处都照 `:3317` 那套来，**别再写第三份**。
        /// ⚠️ 按 <see cref="TempBuff.SourceCard"/>（**真卡名**）匹配，**不是 `Src`** ——
        ///    战术卡那条路上 `Src` 恒为「战术卡」，按它撤会把别的战术卡的增益一起撤掉。
        /// </summary>
        public int RemoveBuffsFromCard(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return 0;
            int n = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var b = _buffs[i];
                if (b == null || b.SourceCard != cardName) continue;
                _buffs.RemoveAt(i);
                if (b.IsKeyword) RemoveKeyword(b.Name, b.Value);
                else
                {
                    switch (b.Name)
                    {
                        case "attack": Attack -= b.Value; break;
                        case "ranged": RangedAttack -= b.Value; break;
                        case "health": Health -= b.Value; MaxHealth -= b.Value; break;
                        case "armour": Armor = System.Math.Max(0, Armor - b.Value); break;
                    }
                }
                n++;
            }
            return n;
        }

        /// <summary>每回合开始时调用</summary>
        public void RefreshForNewTurn()
        {
            Exhausted = false;
            AttacksThisTurn = 0;
            // 「正在祈祷」是**按回合**的状态（规格书 `rule_core.gd:1953` 就在这一批里清）。
            // 卡面：`Each friendly unit that is Praying heals 3`（`Devout Serenity`）·
            //       `If any friendly unit is Praying, …`（`Sororitas Rhino`）。
            Prayed = false;
            // 「本回合下一次用狂暴时留在场上」的**兜底复位**（2026-09-14 A4 批 3）——
            // 卡面写 `this turn`：这一回合没用到，就作废。
            // ⚠️ **消费点是 `RuleCore.UseAlternative` 的狂暴那一段**（用掉当场清，才是「下一次」）；
            //    这里只是「回合过了」的兜底。两处都要，缺一个就会「用两次」或者「跨回合还留着」。
            FerocityStay = false;
            // 🆕 2026-09-16 誓约（Oath）能力的每回合激活计数 —— 原版在回合末清零
            // （`CardScript__OnTurnEnd.c:209`）⇒ 「本回合没用完就作废」。
            // ⚠️ `DeployedTurn` **不清**（它记的是历史：那张牌是哪一回合上场的）。
            OathUsesThisTurn = 0;
            // 🆕 「再触发一次」的额度兜底清空（正常路径**用掉就摘**，见 `ExtraTriggers` 的注释）
            ExtraTriggers.Clear();
        }

        public override string ToString()
        {
            return $"{Name} {Attack}/{Health}{(IsWarlord ? " (督军)" : "")}{(Exhausted ? " 疲劳" : "")}";
        }
    }
}
