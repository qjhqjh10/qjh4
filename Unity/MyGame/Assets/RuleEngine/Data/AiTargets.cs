// AiTargets.cs — 原版 `AbilityLogic.GetTargets` + `FilterMethods.CheckIfMeetsCriteria`
//              的**AI 打分子集**（T06 / A1797）
//
// 为什么需要它：原版 `AI.ScoreFromCriteria` 的每条评分判据都有一个 `TargetCriteria`，
// 分数 = 「按这个 criteria 筛出来的目标」上逐目标累加。**筛选口径是原版的**，不是我们推的。
//
// 判据（第一权威，全部现读）：
//   · `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`（1,427 行；`targetsAffected` 的
//     每个 case 在 :173 起是一串顺序 `if`，**不是 switch**）
//   · `d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c`（515 行，逐句读）
//     ＋ `FilterMethods__MeetsConditionCriteria.c` / `MeetsStatCriteria.c` /
//       `MeetsTraitCriteria.c` / `MeetsArmyCriteria.c`
//   · 枚举值 = `d:/2/tools/il2cpp_out/dump.cs:20735-20819`（`UnitConditions` /
//     `TargetsAffected` / `TargetSelectMethod`）与 `:45721-45859`（`DefinedTrait`）
//
// 🔴 **覆盖范围（如实标注，⛔ 别当成「全实现了」）**：
//   已实现 `targetsAffected` = 0 / 10 / 20 / 30 / 40 / 50 / 51 / 52 / 55 / 60 / 65 / 80 /
//   90 / 100 / 106 / 110 / 113 / 115 / 120 / 130 / 160 / 161 / 162 / 170 / 190 / 210 / 220 / 245
//   （池内评分判据**只用到** 0/10/20/30/50/51/52/60/65/90/110/113/120/161/220 —— 见 `AUD_T06` §三·三）；
//   其余（105/140/150/180/200/201/230/235/240）**返回空表** —— 逐条理由写在 `GetTargets` 的
//   `default` 分支里（235 在原版里**本来就没有分支**，空表是对的）。
//   ⚠️ `55 inPlayOrDying` 按「在场」处理（濒死目标我们引擎里还在棋盘上）。
//   ✅ **2026-10-23（`W64 B-07`/`B-08`）就地订正**：`160/161/162/170/245` 都是**伪列表** ——
//     原版 `while (resource > i) { list.Add(hero); i++ }` ⇒ **列表长度 = 资源点数**。
//     本条原来写的是「161/162 按『资源 > 0 时返回督军一个』近似」「160/170 评分数据 0 条 ⇒ 不实现」
//     —— 两句都已过期（近似已改成原版形状；160/170 已按同一形状实现）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>一个候选目标：在场单位（`Unit`）、或手牌/牌库/弃牌堆里的一份（`Inst`）。</summary>
    public struct AiTargetRef
    {
        public UnitState Unit;
        public CardInstance Inst;
        public int Side;

        public CardDef Card
        {
            get
            {
                if (Unit != null) return Unit.Card;
                return Inst != null ? Inst.Card : null;
            }
        }

        /// <summary>当前近战攻击（在场单位用运行时值，手牌用卡面值）。</summary>
        public int Attack { get { return Unit != null ? Unit.Attack : (Card != null ? Card.Attack : 0); } }
        public int Health { get { return Unit != null ? Unit.Health : (Card != null ? Card.Health : 0); } }
        public int Cost { get { return Card != null ? Card.Cost : 0; } }
    }

    public static class AiTargets
    {
        // ==================================================================
        //  入口：GetTargets
        // ==================================================================

        /// <summary>
        /// 原版 `AbilityLogic.GetTargets(manager, criteria, thisCard, thisCard, targetCard, true, 0)`。
        /// 返回**已筛选 + 已按 `maxTargets` 截断**的候选表。
        /// <para>`thisInst` = 正在打出的那一份（手牌实例；没有就传 null —— 自检/表现层调用时常见）。</para>
        /// <para>`targetUnit` = 这条动作选中的目标（没有 = null）。</para>
        /// </summary>
        public static List<AiTargetRef> GetTargets(BattleContext ctx, AiTargetCriteriaDto c,
                                                   CardDef thisCard, CardInstance thisInst,
                                                   int thisSide, UnitState targetUnit)
        {
            var list = new List<AiTargetRef>();
            if (ctx == null || c == null || thisCard == null) return list;

            int ta = c.targetsAffected;

            // ---- `any`：原版 tooltip「'Any' disregards all other filters」→ 直接返回空？
            //      ⚠️ 不在 `GetTargets` 的 case 列表里（它落到函数尾部）⇒ 原版返回**候选表本身**，
            //      而候选表在进入 case 之前是空的 ⇒ `any` 实际 = 空表（0 个目标）。
            //      但 `CheckIfMeetsCriteria` 对 ta==0 直接 `return true` —— 那是**另一条路**
            //      （`FilterMethods` 在别处单独被调用时）。评分走 `GetTargets` ⇒ 这里给空表。
            if (ta == 0) return list;

            // ---- self(20) / target(30/220)：**原版提前 return，不跑筛选**（`GetTargets.c:184/192`）----
            if (ta == 20 || ta == 40 || ta == 80)
            {
                var self = SelfRef(ctx, thisCard, thisInst, thisSide);
                if (self.Card != null) list.Add(self);
                return list;
            }
            if (ta == 30 || ta == 220 || ta == 90)
            {
                if (targetUnit != null) AddUnit(list, targetUnit, SideOf(ctx, targetUnit));
                // 原版 30/220 提前返回；90 还要过筛选 ⇒ 90 继续往下走
                if (ta != 90) return list;
                ApplyFilters(ctx, c, thisCard, thisInst, thisSide, targetUnit, list);
                Trim(list, c, thisCard, targetUnit);
                return list;
            }

            // ---- 候选池（按 targetsAffected）----
            switch (ta)
            {
                case 50: // inHandFiltered
                    AddZone(list, ctx.Players[thisSide].Hand, thisSide);
                    break;
                case 51: // inHandAndDeckFiltered
                    AddZone(list, ctx.Players[thisSide].Hand, thisSide);
                    AddZone(list, ctx.Players[thisSide].Deck, thisSide);
                    break;
                case 52: // inHandAndPlayFiltered
                    AddZone(list, ctx.Players[thisSide].Hand, thisSide);
                    AddBoard(list, ctx, thisSide);
                    break;
                case 55: // inPlayOrDyingFiltered —— 我们引擎里濒死目标仍在棋盘上
                case 10: // allFiltered
                case 210: // allFilteredCountTraits
                    AddBoard(list, ctx, thisSide);
                    AddBoard(list, ctx, 1 - thisSide);
                    break;
                case 60: // inCemeteryFiltered
                    AddZone(list, ctx.Players[thisSide].Discard, thisSide);
                    break;
                case 65: // inDeckFiltered
                    AddZone(list, ctx.Players[thisSide].Deck, thisSide);
                    break;
                case 100: // adjacentToSelfIfMeetsCriteria
                case 106: // adjacentToActingCard
                {
                    var self = SelfRef(ctx, thisCard, thisInst, thisSide);
                    if (self.Unit != null)
                        AddAdjacent(list, ctx, self.Unit, thisSide);
                    break;
                }
                case 110: // adjacentToTargetIfMeetsCriteria
                    if (targetUnit != null)
                        AddAdjacent(list, ctx, targetUnit, SideOf(ctx, targetUnit));
                    break;
                case 113: // adjacentToFriendlyWarlordIfMeetsCriteria
                {
                    var w = ctx.Players[thisSide].Warlord;
                    if (w != null) AddAdjacent(list, ctx, w, thisSide);
                    break;
                }
                case 115: // adjacentToEnemyWarlordIfMeetsCriteria
                {
                    var w = ctx.Players[1 - thisSide].Warlord;
                    if (w != null) AddAdjacent(list, ctx, w, 1 - thisSide);
                    break;
                }
                case 120: // friendlyHero
                {
                    var w = ctx.Players[thisSide].Warlord;
                    if (w != null) AddUnit(list, w, thisSide);
                    break;
                }
                case 130: // enemyHero
                {
                    var w = ctx.Players[1 - thisSide].Warlord;
                    if (w != null) AddUnit(list, w, 1 - thisSide);
                    break;
                }
                // ---- 🔴 2026-10-23（`W64 B-07`）：161/162 是**按资源点数重复**的伪列表，
                //      不是「有资源就给督军一个」。判据 = `AbilityLogic__GetTargets.c:321-360`
                //      （`0xa1`）与 `:422-460`（`0xa2`）：
                //        `while (resource > i) { list.Add(GetHero(side)); i++ }`
                //      ⇒ **列表长度 = 资源点数**（0 点 ⇒ 空表）。
                //      `PlayerManager.GetCurrentSpiritStone` 读的是 `spiritStoneMana`（`+0x9C`），
                //      我们对应 `PlayerState.SpiritStones`。
                case 161: // currentSpiritStone —— 伪列表：长度 = 灵魂石点数
                    AddHeroRepeated(list, ctx, thisSide, ctx.Players[thisSide].SpiritStones);
                    break;
                case 162: // currentFaith —— 伪列表：长度 = 信仰点数
                    AddHeroRepeated(list, ctx, thisSide, ctx.Players[thisSide].Faith);
                    break;
                // ---- 🔴 2026-10-23（`W64 B-08` 的一部分）：160/170 也是同一种伪列表。
                //   判据 = `AbilityLogic__GetTargets.c:220-320`（`0xa0`）与 `:624-720`（`0xaa`），
                //   形状与 161/162 逐字相同，只是取值函数不同：
                //     · 160 `currentMana`  → `BattleManager.GetUnusedMana` → `PlayerManager.unusedMana`(+0x4C)
                //     · 170 `maxMana`      → `BattleManager.GetTotalMana`  → `PlayerManager.totalMana`(+0x38)
                //   ⚠️ 我们的对应：`unusedMana` = `PlayerState.Energy`（本回合没花掉的能量）、
                //     `totalMana` = `PlayerState.MaxEnergy`（本回合的能量总量）。
                //     `ResetMana` / `AddNewMana` 那一族在 `RuleCore.BeginTurn` 那一侧已同构。
                case 160: // currentMana
                    AddHeroRepeated(list, ctx, thisSide, ctx.Players[thisSide].Energy);
                    break;
                case 170: // maxMana
                    AddHeroRepeated(list, ctx, thisSide, ctx.Players[thisSide].MaxEnergy);
                    break;
                // ---- 🔴 2026-10-23（`W64 B-08` 的一部分）：190 `lastPlayedIfMeetsCriteria`。
                //   判据 = `AbilityLogic__GetTargets.c:173-184`（`0xbe`）→
                //     `CemeteryManager__GetCardsPlayed.c`：扫坟墓动作日志里 `actionType == 10
                //     (playCard)` 的条目、**从最新往回**（`iVar1` 递减），逐条 `CheckIfMeetsCriteria`，
                //     命中的塞进结果表；日志全对局不清（= 卡面 `this game`）。
                //   我们的等价物 = `BattleContext.PlayedCards`（同一份判据，见 `PlayedCard` 的 doc）
                //     ⇒ 顺序照原版：**从最新往回**加。之后仍要过本文件的 `ApplyFilters`/`Trim`。
                case 190: // lastPlayedIfMeetsCriteria
                {
                    var played = ctx.PlayedCards;
                    for (int i = played.Count - 1; i >= 0; i--)
                    {
                        var pc = played[i];
                        if (pc == null || pc.Instance == null) continue;
                        list.Add(new AiTargetRef { Inst = pc.Instance, Side = pc.Owner });
                    }
                    break;
                }
                // ---- 🔴 2026-10-23（`W64 B-08`）：245 也是伪列表（`GetTargets.c:523-624`，`0xf5`）——
                //   `PlayerManager.GetSecretsPlayed` 循环 ⇒ 长度 = 本局打出的密令数。
                //   我们的对应 = `PlayerState.SecretsPlayed`（同一含义，`PlayerState.cs:138`）。
                case 245: // currentSecretsPlayed
                    AddHeroRepeated(list, ctx, thisSide, ctx.Players[thisSide].SecretsPlayed);
                    break;
                default:
                    // 105/140/150/180/200/201/230/240 —— 判据/输入还没闭合（2026-10-23 现核）：
                    //   · 105/140/150 —— 读的是**静态缓存表** `DAT_1842c3a28 + 0xb8`
                    //     （`GetTargets.c:849/867/1010`），**谁往里写没查到** ⇒ ⛔ 不猜；
                    //   · 180 —— `PlayerDataManager.fullCardCollection`（**卡牌收藏**，不是对局），
                    //     引擎里没有这个输入；
                    //   · 200/201 —— `CardScript.referencedCards`(+0x278)，我们没有这一列；
                    //   · 230/240 —— `GetHighestHealthWarlord` / `GetLowestHealthUnit` 的完整选法未读完。
                    //   · **235 在 `GetTargets.c` 里没有任何分支**（现核 grep `0xeb` = 0 命中）
                    //     ⇒ 原版对 235 本来就返回空表 ⇒ 我们返回空**是对的**。
                    // 其余未列出的值同理：原版没有候选 ⇒ 空表（⛔ 不造兜底）。
                    return list;
            }

            ApplyFilters(ctx, c, thisCard, thisInst, thisSide, targetUnit, list);
            Trim(list, c, thisCard, targetUnit);
            return list;
        }

        // ==================================================================
        //  候选池构造
        // ==================================================================

        static AiTargetRef SelfRef(BattleContext ctx, CardDef thisCard, CardInstance thisInst, int thisSide)
        {
            // 先按**实例身份**找场上的那一份（技能动作的施放者：`u.Instance` 就是它）
            if (thisInst != null)
            {
                var board0 = ctx.Players[thisSide].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board0[s];
                    if (u != null && ReferenceEquals(u.Instance, thisInst))
                        return new AiTargetRef { Unit = u, Inst = u.Instance, Side = thisSide };
                }
                // 手牌里那份（打出这张卡时原版 `thisCard` 就是手牌里那一份）
                if (ReferenceEquals(thisInst.Card, thisCard))
                    return new AiTargetRef { Inst = thisInst, Side = thisSide };
            }
            // 没有实例（自检/表现层）⇒ 退到「场上同一个卡模板」
            var board = ctx.Players[thisSide].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                if (u != null && ReferenceEquals(u.Card, thisCard))
                    return new AiTargetRef { Unit = u, Inst = u.Instance, Side = thisSide };
            }
            return new AiTargetRef { Side = thisSide };
        }

        static void AddBoard(List<AiTargetRef> list, BattleContext ctx, int side)
        {
            var board = ctx.Players[side].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                if (u != null) list.Add(new AiTargetRef { Unit = u, Inst = u.Instance, Side = side });
            }
        }

        static void AddZone(List<AiTargetRef> list, List<CardInstance> zone, int side)
        {
            if (zone == null) return;
            for (int i = 0; i < zone.Count; i++)
                if (zone[i] != null) list.Add(new AiTargetRef { Inst = zone[i], Side = side });
        }

        static void AddUnit(List<AiTargetRef> list, UnitState u, int side)
        {
            if (u != null) list.Add(new AiTargetRef { Unit = u, Inst = u.Instance, Side = side });
        }

        /// <summary>
        /// **伪列表：把督军重复 `count` 次**（`W64 B-07`，2026-10-23）。
        ///
        /// 判据 = `AbilityLogic__GetTargets.c:321-360` / `:422-460` / `:220-320` / `:624-720`（`0xa1`/`0xa2`/`0xa0`/`0xaa`）
        /// 与 `:523-624`（`0xf5`）—— 那几支都是同一个形状：
        /// <code>while (resource > i) { list.Add(BattleManager.GetHero(manager, side)); i++; }</code>
        /// ⇒ **列表长度 = 资源点数**（不是「有资源就一个」）。
        /// 影响面：这些目标只进 `ScoreFromCriteria` 的 `Count` 累加 ⇒ 打分随资源点数线性放大。
        /// </summary>
        static void AddHeroRepeated(List<AiTargetRef> list, BattleContext ctx, int side, int count)
        {
            if (count <= 0) return;
            var w = ctx.Players[side].Warlord;
            if (w == null) return;
            for (int i = 0; i < count; i++) AddUnit(list, w, side);
        }

        static void AddAdjacent(List<AiTargetRef> list, BattleContext ctx, UnitState anchor, int side)
        {
            var board = ctx.Players[side].Board;
            // 「谁算相邻」判据只此一处 = `BoardSpec.AdjacentSlots`（同侧行内左右各一格，含督军格）
            int slot = SlotOf(board, anchor);
            if (slot < 0) return;
            var tmp = new List<int>();
            BoardSpec.AdjacentSlots(slot, tmp);
            for (int i = 0; i < tmp.Count; i++)
            {
                var u = board[tmp[i]];
                if (u != null) list.Add(new AiTargetRef { Unit = u, Inst = u.Instance, Side = side });
            }
        }

        static int SlotOf(UnitState[] board, UnitState u)
        {
            for (int s = 0; s < board.Length; s++) if (ReferenceEquals(board[s], u)) return s;
            return -1;
        }

        static int SideOf(BattleContext ctx, UnitState u)
        {
            if (ReferenceEquals(ctx.Players[0].Warlord, u)) return 0;
            if (ReferenceEquals(ctx.Players[1].Warlord, u)) return 1;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(ctx.Players[0].Board[s], u)) return 0;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(ctx.Players[1].Board[s], u)) return 1;
            return -1;
        }

        // ==================================================================
        //  筛选（FilterMethods.CheckIfMeetsCriteria，逐句对应）
        // ==================================================================

        static void ApplyFilters(BattleContext ctx, AiTargetCriteriaDto c, CardDef thisCard,
                                 CardInstance thisInst, int thisSide, UnitState targetUnit,
                                 List<AiTargetRef> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!Match(ctx, c, thisCard, thisInst, thisSide, list[i], targetUnit)) list.RemoveAt(i);
            }
        }

        /// <summary>原版 `FilterMethods.CheckIfMeetsCriteria(thisCard, candidate, targetCard, criteria)`。
        ///
        /// 🔴 **顺序照原版**：先 special case（20/30/200/201/235），再 general 分支。
        /// `90 targetIfMeetsCriteria` 在 general 分支里只是「候选必须 == targetCard」，
        /// 之后**照样**过全部筛选。</summary>
        static bool Match(BattleContext ctx, AiTargetCriteriaDto c, CardDef thisCard,
                          CardInstance thisInst, int thisSide, AiTargetRef t, UnitState targetUnit)
        {
            int ta = c.targetsAffected;
            var card = t.Card;
            if (card == null) return false;

            // `any` 直接放行（`CheckIfMeetsCriteria.c` 头几行）
            if (ta == 0) return true;

            // 🔴 **自己 = 同一份卡实例**（原版比的是 `CardScript` 对象）。
            //    只按 `CardDef` 比会把「手里同名的另一张」与「场上早先打出的同一张」都误判成自己
            //    （我们 `CardDef` 是共享模板）⇒ 有手牌实例时一律按实例比。
            bool isSelf = thisInst != null
                        ? ReferenceEquals(t.Inst, thisInst)
                        : (t.Unit != null && ReferenceEquals(t.Unit.Card, thisCard));

            // ---- special case（原版在 general 分支之前**提前 return**，不跑任何筛选）----
            if (ta == 20) return isSelf;                                        // self
            if (ta == 30 || ta == 220)                                          // target / targetCountTraits
                return targetUnit != null && t.Unit != null && ReferenceEquals(t.Unit, targetUnit);
            if (ta == 40 || ta == 80) return isSelf;                            // attacker / actingCard

            // 90 targetIfMeetsCriteria：候选必须就是这一下的目标，**之后照样过全部筛选**
            if (ta == 90 && (targetUnit == null || t.Unit == null || !ReferenceEquals(t.Unit, targetUnit)))
                return false;

            // ---- 相似卡排除（`excludeSimilarCards`，+0x1f）----
            if (c.excludeSimilarCards && isSelf) return false;

            // ---- 阵营侧（`friendlyUnits` +0x14 / `enemyUnits` +0x15）----
            if (t.Side == thisSide)
            {
                if (!c.friendlyUnits) return false;
            }
            else
            {
                if (!c.enemyUnits) return false;
            }

            // ---- self / notSelf ----
            if (isSelf && !c.includeSelf) return false;
            if (!isSelf && c.excludeNotSelf) return false;

            // ---- 督军排除（`excludeFriendlyHero` +0x21 / `excludeEnemyHero` +0x22）----
            bool isHeroCard = IsHeroCard(t);
            if (c.excludeFriendlyHero && t.Side == thisSide && isHeroCard) return false;
            if (c.excludeEnemyHero && t.Side != thisSide && isHeroCard) return false;

            // ---- 残骸（`includeRemnants` +0x1b；`+0x65` = isRemnant）----
            if (!c.includeRemnants && t.Unit != null && t.Unit.IsRemnant) return false;

            // ---- 卡类（`includeHero/includeMinions/includeSpells/includeWhispers`）----
            int ct = CardTypeOf(card);
            if (ct == 20 && !c.includeSpells) return false;
            if (ct == 10 && !c.includeHero) return false;
            if (ct == 0 && !c.includeMinions) return false;
            if (ct == 40 && !c.includeWhispers) return false;

            // ---- spellType（`criteria.spellType` +0x68）----
            if (c.spellType != 0)
            {
                var dto = AiScoringData.Get(card.Id);
                if (dto == null || dto.spellType != c.spellType) return false;
            }

            // ---- UnitConditions（`conditionsFilter` +0x38 / `excludeCondition` +0x3d）----
            if (!MeetsCondition(ctx, t, c.conditionsFilter, c.excludeCondition)) return false;

            // ---- 兵种/种族（`keywordFilter` +0x2c / `excludeRace` +0x30）----
            if (c.keywordFilter != 0)
            {
                var dto = AiScoringData.Get(card.Id);
                bool has = dto != null && dto.keyword == c.keywordFilter;
                // 原版：(!excludeRace && !has) || (excludeRace && has) ⇒ false
                if ((!c.excludeRace) != has) return false;
            }

            // ---- trait（`traitsFilter` +0x34 / `excludeTrait` +0x3c）----
            if (!MeetsTrait(t, c.traitsFilter, c.excludeTrait)) return false;

            // ---- 数值筛选（攻击 / 当前血 / 当前费 / 稀有度）----
            //   原版第二参数**恒是 thisCard 的当前近战攻击**（`CheckIfMeetsCriteria.c` 里
            //   `uVar5 = EntityScript__get_CurrentMeleeAttack(param_1)` 那一句四处复用）。
            int thisAtk = thisCard.Attack;
            if (!MeetsStat(t.Attack, thisAtk, c.attackFilter, c.attackFilterOption, 0)) return false;
            if (!MeetsStat(t.Health, thisAtk, c.currentHealthFilter, c.currentHealthFilterOption, 0)) return false;
            if (!MeetsStat(t.Cost, thisAtk, c.manaCostFilter, c.manaCostFilterOption, 0)) return false;
            // 稀有度筛选：`RawCardScript` 的那一列在我们的真值里没有落「枚举值」这一栏
            // （`CardDef.Rarity` 是字符串）—— 池内评分数据 **0 条**用到它 ⇒ 只保留 op=0 的放行。
            if (c.rarityFilterOption != 0)
            {
                // ⚠️ 没有判据 ⇒ 不猜：保守地不排除（报告 §三 记录未覆盖）
            }

            // ---- 具名卡（`filterSpecificCards` +0x70：按卡名逐条比）----
            if (c.filterSpecificCards != null && c.filterSpecificCards.Length > 0)
            {
                bool named = false;
                for (int i = 0; i < c.filterSpecificCards.Length; i++)
                {
                    if (SameCardId(c.filterSpecificCards[i], card)) { named = true; break; }
                }
                if (!named) return false;
            }

            // ---- 排除附魔卡（`excludeEnchantments` +0x78：名字相等的排掉）----
            if (c.excludeEnchantments != null)
            {
                for (int i = 0; i < c.excludeEnchantments.Length; i++)
                    if (SameCardId(c.excludeEnchantments[i], card)) return false;
            }

            // ---- 阵营（`chosenArmy` +0x24 / `armyFilter` +0x28）----
            if (!MeetsArmy(card, c.chosenArmy, c.armyFilter)) return false;

            // 100 adjacentToSelfIfMeetsCriteria：**相邻关系已经在候选池那一步筛过**
            // （`GetTargets` 的 case 100 只把 `BoardSpec.AdjacentSlots` 的结果放进来）
            return true;
        }

        /// <summary>
        /// 单独判一个目标是否满足 criteria（= 原版 `FilterMethods.CheckIfMeetsCriteria(card, target, target, tc)`）。
        /// `ScoreFromPlayingNow` 在跑 `cardTargetScoringCriteria` 之前要这一句。
        /// </summary>
        public static bool MatchesTarget(BattleContext ctx, AiTargetCriteriaDto c,
                                         CardDef thisCard, CardInstance thisInst,
                                         int thisSide, UnitState target)
        {
            if (c == null) return true;           // 缺键 = 默认实例（ta=0）⇒ 原版 any 恒真
            if (c.targetsAffected == 0) return true;
            if (target == null) return false;
            var t = new AiTargetRef { Unit = target, Inst = target.Instance, Side = SideOf(ctx, target) };
            return Match(ctx, c, thisCard, thisInst, thisSide, t, target);
        }

        /// <summary>原版 `FilterMethods.MeetsStatCriteria`（`MeetsStatCriteria.c` 全文）。
        /// `value` = 候选的这一项，`thisValue` = 施法者的当前近战攻击，`op` = `StatFilterOptions`。</summary>
        static bool MeetsStat(int value, int thisValue, int filter, int op, int extra)
        {
            switch (op)
            {
                case 1: return value == filter;                       // EqualTo
                case 2: return filter <= value;                       // EqualOrHigher
                case 3: return value <= filter;                       // EqualOrLower
                case 4: return value <= thisValue;                    // EqualOrLowerToThisUnitAttack
                case 5: return thisValue < value;                     // HigherToThisUnitAttack
                case 6: return value + extra <= filter;               // EqualOrLowerIncludingBastion
                case 7: return value < thisValue;                     // LowerToThisUnitAttack
                case 9: return extra == filter;                       // EqualToAlt
                case 10: return filter <= extra;                      // EqualOrHigherAlt
                case 11: return extra <= filter;                      // EqualOrLowerAlt
                default: return true;                                 // op=0 ⇒ 不筛
            }
        }

        /// <summary>原版 `FilterMethods.MeetsConditionCriteria` 的**AI 打分用到的那几档**。
        /// 池内评分 `conditionsFilter` 的值 = 10 / 40 / 243 / 340 / 341 / 221（逐条现数）。</summary>
        static bool MeetsCondition(BattleContext ctx, AiTargetRef t, int cond, bool exclude)
        {
            if (cond == 0) return true;

            // 🔴 2026-10-23（引擎线 · `AiTargets 两份判据`）裁定：**只转发到 `ConditionEval`**。
            //    原版只有一张 `FilterMethods.MeetsConditionCriteria` 表；本方法此前是第二份实现，
            //    「两处写同一条规则 = 迟早不一致」（`CLAUDE.md` §三）⇒ 收口到引擎侧唯一判据
            //    `Core/ConditionEval.cs`（`MeetsUnit` / `MeetsCard`）。
            //    ⚠️ `ConditionEval` 判不了时返回 `null`，这里仍返回 `false` —— 那是 AI 侧的保守口径
            //    （原版没有单位满足 ⇒ 空表），不是第二份判据。AI 评分现用的 6 格
            //    （10 / 40 / 243 / 340 / 341 / 221）在 `ConditionEval.KindOf` 里全是 `Kind.Ok`。
            bool? r = t.Unit != null
                ? ConditionEval.MeetsUnit(ctx, t.Unit, cond, exclude)
                : (t.Card != null
                    ? ConditionEval.MeetsCard(ctx, t.Card, t.Side, cond, exclude)
                    : (bool?)null);
            return r ?? false;
        }

        /// <summary>原版 `FilterMethods.MeetsTraitCriteria`：`trait == 0` ⇒ 不筛；
        /// 否则「有没有这个 trait」，`excludeTrait` 取反。</summary>
        static bool MeetsTrait(AiTargetRef t, int trait, bool exclude)
        {
            if (trait == 0) return true;
            string name = TraitName(trait);
            if (name == null)
            {
                // 未登记的原版 trait：**没有一张卡在我们这边能证明它有它** ⇒ 不成立
                // （原版若真有卡带它，这里就会静默偏小；已覆盖池内评分用到的全部 9 个值）
                return false;
            }
            bool has = HasTrait(t, name);
            return exclude ? !has : has;
        }

        static bool HasTrait(AiTargetRef t, string name)
        {
            if (t.Unit != null) return t.Unit.Has(name);
            return t.Card != null && t.Card.Has(name);
        }

        static bool IsHeroCard(AiTargetRef t)
        {
            if (t.Unit != null) return t.Unit.IsWarlord;
            return CardTypeOf(t.Card) == 10;
        }

        /// <summary>`RawCardScript.cardType`（`CardTypeOptions`：Minion=0 / Hero=10 / Tactic=20 / Whispers=40）。
        /// 优先用真值（`ai_scoring.json` 的 `cardType`）；表里没有就按 `CardDef.Type` 映射。</summary>
        static int CardTypeOf(CardDef card)
        {
            if (card == null) return -1;
            var dto = AiScoringData.Get(card.Id);
            if (dto != null) return dto.cardType;
            if (card.Type == "unit") return 0;
            if (card.Type == "hero") return 10;
            return 20;  // tactic / defence（原版两个都是 `cardType=20`）
        }

        /// <summary>`filterSpecificCards` / `excludeEnchantments` 的元素是 `RawCardScript`；
        /// 真值里落的是 `uniqueId`。按 id 全等比（⛔ 不按名字模糊）。</summary>
        static bool SameCardId(string idOrName, CardDef card)
        {
            if (string.IsNullOrEmpty(idOrName) || card == null) return false;
            return idOrName == card.Id;
        }

        /// <summary>原版 `FilterMethods.MeetsArmyCriteria(card.army, chosenArmy, armyFilter)`。
        /// `armyFilter=0`（`NotApplicable`）⇒ 恒真。池内评分数据 **0 条**用到它 ⇒ 只实现 0 档。</summary>
        static bool MeetsArmy(CardDef card, int chosenArmy, int armyFilter)
        {
            if (armyFilter == 0) return true;
            return true; // 其余档当前无数据；⛔ 不猜（报告里记为未覆盖）
        }

        // ==================================================================
        //  `maxTargets` 截断（原版 `GetTargets.c` 尾部 switch，`targetSelectMethod`）
        // ==================================================================

        static void Trim(List<AiTargetRef> list, AiTargetCriteriaDto c, CardDef thisCard, UnitState targetUnit)
        {
            if (c.maxTargets <= 0 || list.Count <= c.maxTargets) return;

            switch (c.targetSelectMethod)
            {
                case 1: // highestAttack
                    list.Sort((a, b) => b.Attack.CompareTo(a.Attack));
                    break;
                case 2: // highestValueInPlay
                    list.Sort((a, b) => SimpleAI.ValueInPlay(b.Unit).CompareTo(SimpleAI.ValueInPlay(a.Unit)));
                    break;
                case 3: // lowestValueInPlay
                    list.Sort((a, b) => SimpleAI.ValueInPlay(a.Unit).CompareTo(SimpleAI.ValueInPlay(b.Unit)));
                    break;
                case 6: // lowestHealth
                    list.Sort((a, b) => a.Health.CompareTo(b.Health));
                    break;
                case 4: // cardTarget —— 目标排最前
                    list.Sort((a, b) =>
                        (b.Unit != null && ReferenceEquals(b.Unit, targetUnit)) ? 1
                      : (a.Unit != null && ReferenceEquals(a.Unit, targetUnit)) ? -1 : 0);
                    break;
                case 5: // lastAdded —— 保尾部
                    list.RemoveRange(0, list.Count - c.maxTargets);
                    return;
                default: // 0 random / 7 firstAdded —— 保前 N（random 在池内 0 条）
                    break;
            }
            if (list.Count > c.maxTargets) list.RemoveRange(c.maxTargets, list.Count - c.maxTargets);
        }

        // ==================================================================
        //  DefinedTrait 数值 → 我们 `KeywordTable` 的规范名
        //  出处：`d:/2/tools/il2cpp_out/dump.cs:45721-45859`
        // ==================================================================

        /// <summary>只登记**本工程认识、且池内评分判据真用到**的那些（池内用到的 9 个值：
        /// 1225 regiment · 330 markOfChaos · 390 flying · 1200 ambush · 70 stealth ·
        /// 920 mob · 1260 huntMark · 1170 markerlight · 1000 codex）。
        /// 其余返回 null ⇒ 该判据不成立（报告 §三 已记「未登记 ⇒ 静默偏小」的风险）。</summary>
        public static string TraitName(int trait)
        {
            switch (trait)
            {
                case 10: return KeywordTable.Rally;
                case 40: return "fast";
                case 70: return KeywordTable.Stealth;
                case 80: return KeywordTable.Shield;
                case 100: return "stun";
                case 120: return KeywordTable.Backlash;
                case 130: return KeywordTable.Jam;
                case 150: return KeywordTable.CantAttack;
                case 180: return "unstoppable";
                case 200: return "poisoned";
                case 230: return KeywordTable.DropPod;
                case 240: return KeywordTable.Dodge;
                case 280: return "rage";
                case 310: return "invulnerable";
                case 330: return "markofchaos";
                case 350: return "blast";
                case 360: return "berzerk";
                case 390: return KeywordTable.Flying;
                case 430: return "survivor";
                case 460: return "flank";
                case 480: return "vulnerable";
                case 610: return "pack";
                case 850: return "bastion";
                case 920: return "mob";
                case 970: return "pindown";
                case 975: return "blind";
                case 980: return KeywordTable.Vanguard;
                case 985: return "camouflage";
                case 990: return "sniper";
                case 1000: return KeywordTable.Codex;
                case 1020: return KeywordTable.Armour;
                case 1030: return "regeneration";
                case 1110: return KeywordTable.Strike;
                case 1120: return "shuriken";
                case 1140: return "waystone";
                case 1160: return KeywordTable.LongRange;
                case 1170: return "markerlight";
                case 1200: return "ambush";
                case 1220: return "duty";
                case 1225: return KeywordTable.Regiment;
                case 1260: return "huntmark";
                case 1265: return "ferocity";
                default: return null;
            }
        }
    }
}
