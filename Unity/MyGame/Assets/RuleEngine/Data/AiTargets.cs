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
//   90 / 100 / 106 / 110 / 113 / 115 / 120 / 130 / 140 / 150 / 160 / 161 / 162 / 170 /
//   190 / 210 / 220 / 240 / 245
//   （池内评分判据**只用到** 0/10/20/30/50/51/52/60/65/90/110/113/120/140/161/220 —— 见 `AUD_T06` §三·三）；
//   其余（105/180/200/201/230/235）**返回空表** —— 逐条理由写在 `GetTargets` 的
//   `default` 分支里（235 在原版里**本来就没有分支**，空表是对的）。
//   🔴 **2026-10-24（引擎线 `B6` 第三批 · 判据由 `C1` §4/§顺手发现1 闭合）就地订正**：
//     · **`0 any` 原来返回空表是【错的】** —— 原版 `AbilityLogic__GetTargets.c:884-891` 明写
//       `GetUnitsInPlayList(…, 全 1)` = **场上全部单位**（忽略 criteria 其余全部筛位），
//       而 `AI__ScoreFromCriteria.c:93` 正是拿 `targetCriteria` 调它 ⇒ 池内 `DA50` / `UM86`
//       两张卡的 AI 评分原来一律算 0 个目标（详见 `addAllUnitsInPlay` 的 doc）；
//     · **`140 previousAbilityTargets` / `150 previousAbilityFiltered` / `240 lowestHealth` 已实现**
//       （判据 = `GetTargets.c:849-883` / `:835-844`）。
//   ⚠️ `55 inPlayOrDying` 按「在场」处理（濒死目标我们引擎里还在棋盘上）。
//   ✅ **2026-10-23（`W64 B-07`/`B-08`）就地订正**：`160/161/162/170/245` 都是**伪列表** ——
//     原版 `while (resource > i) { list.Add(hero); i++ }` ⇒ **列表长度 = 资源点数**。
//     本条原来写的是「161/162 按『资源 > 0 时返回督军一个』近似」「160/170 评分数据 0 条 ⇒ 不实现」
//     —— 两句都已过期（近似已改成原版形状；160/170 已按同一形状实现）。
//
// ⚠️ **与原版的一处【刻意不同】：返回值是【新表】，不是原版的【静态池】**
//   原版 `AbilityLogic.GetTargets` 每次调用**先清空**那份静态池 `pooledPossibleTargets` 再填
//   （判据 §3·3 ②），所以**调用方不能缓存返回值**；我们每次返回新的 `List`，
//   语义上更安全（缓存了也不会被下一次调用冲掉），但**不要因此假设原版也能缓存**。
//   —— 这一条是 `R6-09`（判据 §8）要求写清的，⛔ 别把「我们返回新表」当成「原版也是」。
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

        /// <summary>🆕 **2026-10-24（引擎线 `G3` · `C5` §2）**：当前**远程**攻击（在场用运行时值，手牌用卡面值）。
        /// 用途 = `targetSelectMethod == 1` 的排序键 `GetHighestAttackValue`（见 <see cref="HighestAttack"/>）。</summary>
        public int RangedAttack { get { return Unit != null ? Unit.RangedAttack : (Card != null ? Card.RangedAttack : 0); } }

        /// <summary>
        /// 🆕 `CardScript.GetHighestAttackValue()` = **`max(CurrentRangeAttack, CurrentMeleeAttack)`**。
        /// 判据 = `d:/2/tools/decomp_full/CardScript__GetHighestAttackValue.c:6-14`：
        /// <code>
        ///   iVar1 = EntityScript__get_CurrentRangeAttack(param_1);   // 远程
        ///   iVar2 = EntityScript__get_CurrentMeleeAttack(param_1);   // 近战
        ///   if (iVar2 < iVar1) return 远程;  else return 近战;        // ⇒ max(近战, 远程)
        /// </code>
        /// ⚠️ **旧实现只比 `Attack`（近战）** —— 远程单位（`RangedAttack > Attack`）的排序键被低估。
        /// </summary>
        public int HighestAttack
        {
            get
            {
                int melee = Attack, ranged = RangedAttack;
                return melee < ranged ? ranged : melee;
            }
        }

        /// <summary>🆕 原版 `EntityScript.isRemnant // +0x65`
        /// （`EntityScript__get_isRemnant.c`；`targetSelectMethod == 1` 的**第一排序键**）。
        /// 手牌/牌库候选不是残骸 ⇒ `false`。</summary>
        public bool IsRemnant { get { return Unit != null && Unit.IsRemnant; } }

        /// <summary>🆕 进场序（= 原版 `BattleManager.unitsInPlay // +0x470` 的追加序）。
        /// **并列时的次序判据**：LINQ `OrderBy` 是**稳定排序**，并列会保留源表顺序，
        /// 而源表就是这张追加序表（`C5` §2·3 已闭合）。手牌/牌库候选不在那张表里 ⇒ `-1`
        /// （`RuleCore.StableSortByPlayOrder` 同口径：负数排末尾）。</summary>
        public int PlayOrder { get { return Unit != null ? Unit.PlayOrder : -1; } }
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
            if (ctx == null || thisCard == null) return list;

            // 🔴 **2026-10-24（引擎线 `B6` 第三批 · `C1` 顺手发现 1）：`ta == 0 (any)` 的语义改了。**
            //
            //   改前这里返回**空表**，理由是「原版候选表进 case 前是空的、`any` 不在 case 列表里
            //   所以落到函数尾部 ⇒ 空表」。**那条推断是错的** —— 现读
            //   `AbilityLogic__GetTargets.c:884-891`：
            //   <code>
            //     if (*(int *)(param_2 + 0x10) == 0) {                    // targetsAffected == Any
            //       BattleManager__GetUnitsInPlayList(param_1, result, 0, 1,1,1,1,1,1,1,1, 0);
            //       goto LAB_180818485;                                   // ⇒ 直接 return
            //     }
            //   </code>
            //   即 **场上全部单位**（`include*` 八个筛位**全是 1** ⇒ 忽略 criteria 的其余筛位），
            //   建完就 `goto` 到函数出口 ⇒ **不过筛、也不 `maxTargets` 截断**。
            //   而 `AI__ScoreFromCriteria.c:93` 正是拿 `targetCriteria(+0x28)` 调它
            //   （`multiplierCriteria(+0x30)` 那一路才走 `ta==0 ⇒ 1` 的特判，见 `AiScoring`）⇒
            //   **AI 评分里凡 `ta=0` 的判据，改前一律算 0 个目标**。
            //   池内真受影响的两条（`ai_scoring.json` 现读，`ta==0` 且 scoringOption 会遍历目标的）：
            //     · `DA50 Sacred Standard`（option 1 `Count`）· `UM86 Assault Doctrine`（option 2 `SumValueInPlay`）。
            //   ⚠️ **`c == null` 也是这一档**：`cards_truth.json` 把**全默认对象整个省略**
            //   （`gen_cards_truth.py` 的 `compact_*`），而原版 `ScoringCriteria.targetCriteria` 是
            //   `[Serializable]` 类字段、`AbilityLogic__GetTargets` 进门就解引用它（`:87`
            //   `if (param_2 == 0) goto <NRE>`）⇒ 原版数据里它**不可能是 null**，缺键只能是
            //   「ta=0 全默认」被省略。`UM86` 正是这一形状。⇒ 与 `MatchesTarget` 的既有口径
            //   （`c == null` / `ta == 0` 都 `return true`）一致。
            if (c == null || c.targetsAffected == 0)
            {
                AddAllUnitsInPlay(list, ctx);
                return list;
            }

            int ta = c.targetsAffected;

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
                Trim(ctx, list, c);
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
                // ---- 🔴 2026-10-24（引擎线 `B6` 第三批 · `C1` §4）：140 / 150 / 240 三档补上。----
                //   判据全部在 `AbilityLogic__GetTargets.c`（顺序 `if` 链，`+0x10` = targetsAffected）：
                case 140: // previousAbilityTargets（`:849-866`，`iVar6 == 0x8c`）
                    // 原版 = `AddRange(result, <静态池 pooledPossibleTargets>)`，然后 **`:865 goto` 直接返回**
                    // ⇒ **不过筛、也不 Trim**（与 150 的唯一差别就在这里）。
                    AddPreviousAbilityTargets(list, ctx);
                    return list;
                case 150: // previousAbilityFiltered（`:867-882`，`iVar6 == 0x96`）
                    // 同一个静态池，但这一支**不 return** ⇒ 落到 `:1083-1105` 的通用筛选尾
                    // （`FilterMethods.CheckIfMeetsCriteria` 里那条「候选必须在表里」+ 阵营/卡种/条件筛）。
                    // ⇒ 本处 `break`，由下面的 `ApplyFilters` / `Trim` 收尾。
                    AddPreviousAbilityTargets(list, ctx);
                    break;
                case 240: // lowestHealth（`:835-844`，`iVar6 == 0xf0`）
                {
                    // 原版：`uVar8 = BattleManager__GetLowestHealthUnit(param_1, bVar16)`，
                    // 把**那一个**单位塞进结果表，然后 `:843 goto` 直接返回（不过筛、不 Trim）。
                    AddLowestHealthUnit(list, ctx, thisSide, c);
                    return list;
                }
                case 230: // highestHealthWarlord（`AbilityLogic__GetTargets.c:811`，`iVar6 == 0xe6`）
                {
                    // ① 选法（`BattleManager__GetHighestHealthWarlord.c`，全文）：读两方督军当前血量
                    //    （`CardScript+0x68`）—— `+0xc0`(=Player 0) 记 A、`+0xc8`(=Player 1) 记 B：
                    //    B < A ⇒ A；B > A ⇒ B；**相等 ⇒ 返回 null**。
                    // ② 消费端（`GetTargets.c:811` 的 ta=230 支）：GetHighestHealthWarlord 返回 null（相等）时
                    //    **不退化成空表**，而是取 `BattleManager.GetHero(bm, 1)`（= `+0xc0` = Player 0 的督军）顶上。
                    //    ⇒ 归纳 = 「Player 1 督军血量**严格大于** Player 0 ⇒ 取 Player 1；否则（含相等）取 Player 0」，
                    //    **永远不会是空表**。
                    //    ⚠️ 这一条比施工判据 §二 G16 写的「相等 ⇒ 空表」更全 —— 判据文件只读了
                    //    GetHighestHealthWarlord 本体、没跟到 GetTargets 的 null 兜底；以 `.c` 为准（铁律 2）。
                    // 🔴 映射：`+0xc0` = Player 0（`isPlayer`=真/友方）、`+0xc8` = Player 1（敌方）
                    //    —— 判据 = `GetHero(bool playerHeroY)`：真 ⇒ `+0xc0`、假 ⇒ `+0xc8`
                    //    （`资料/任务线/战斗线/任务/T05_逐卡演出与特效接线.md:170-171`：3 friendlyHero=`+0xc0` · 4 enemyHero=`+0xc8`）。
                    var w0 = ctx.Players[0].Warlord;
                    var w1 = ctx.Players[1].Warlord;
                    // 原版直接读两格 `+0x68`（缺一方会 NRE）—— 我们不复制崩溃语义：缺 w0 时 AddUnit 空转。
                    if (w1 != null && w0 != null && w1.Health > w0.Health) AddUnit(list, w1, 1);
                    else AddUnit(list, w0, 0);
                    break;   // 落通用筛选尾（ApplyFilters/Trim）—— 与 120/130 两个「取督军」支同口径
                }
                default:
                    // 105 / 180 / 200 / 201 / 235 —— 判据/输入还没闭合（2026-10-24 现核；230 已于 2026-10-27 G16 实现，从此表移除）：
                    //   · 105 —— 读的是**静态缓存表** `DAT_1842c3a28 + 0xb8`（`GetTargets.c:1010`），
                    //     同族 140/150 的写点已查明（`SetPreviousAbilityTargets`），**105 没有对应的写点**
                    //     ⇒ ⛔ 不猜；
                    //   · 180 —— `PlayerDataManager.fullCardCollection`（**卡牌收藏**，不是对局），
                    //     引擎里没有这个输入；
                    //   · 200/201 —— `CardScript.referencedCards`(+0x278)，**我们卡表没有这一列**
                    //     （`cards_engine.json` 现读的 36 个键里没有）⇒ 差的是数据管线，不是实现；
                    //   · 230 —— ✅ 2026-10-27（W68ENG3 · G16）已实现（见上面的 `case 230`，不再落本 default）——
                    //     `GetHighestHealthWarlord` 全文已读完，这里那句「完整选法未读完」已过期，就地订正（铁律 5）。
                    //   · **235 在 `GetTargets.c` 里没有任何分支**（现核 grep `0xeb` = 0 命中）
                    //     ⇒ 原版对 235 本来就返回空表 ⇒ 我们返回空**是对的**。
                    // 其余未列出的值同理：原版没有候选 ⇒ 空表（⛔ 不造兜底）。
                    return list;
            }

            ApplyFilters(ctx, c, thisCard, thisInst, thisSide, targetUnit, list);
            Trim(ctx, list, c);
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

        /// <summary>
        /// **`ta == 0 (any)` 的候选池** = 场上全部单位（双方，含督军）。
        ///
        /// 判据 = `AbilityLogic__GetTargets.c:884-891`：
        /// `BattleManager__GetUnitsInPlayList(manager, result, /*clear*/0, 1,1,1,1,1,1,1,1)` ——
        /// 那 8 个筛位**全是 1** ⇒ 「忽略 criteria 其余全部筛位」；随后 `goto LAB_180818485`
        /// （= 函数出口 `return *result`）⇒ **不过筛、不 Trim**。
        ///
        /// ⚠️ 这里**仍然要判「在不在场」**：`GetUnitsInPlayList` 内部走的是 `CardScript.IsInPlay`
        /// （`BattleManager__GetUnitsInPlayPooledList.c:70-73`），所以**待死（`waitingToDie=5`）**
        /// 与「血已归零还没结算」的都不算。⛔ 别拿 `AddBoard` 直接顶 —— 那个不过 `IsInPlay`
        /// （它服务的那几档后面还有 `ApplyFilters` 兜，这一档没有）。
        /// </summary>
        static void AddAllUnitsInPlay(List<AiTargetRef> list, BattleContext ctx)
        {
            for (int side = 0; side < 2; side++)
            {
                var board = ctx.Players[side].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || !u.IsAlive || ctx.IsPendingDeath(u)) continue;
                    list.Add(new AiTargetRef { Unit = u, Inst = u.Instance, Side = side });
                }
            }
        }

        /// <summary>
        /// **`ta == 140 / 150`（`previousAbilityTargets` / `previousAbilityFiltered`）的候选池**
        /// = <see cref="BattleContext.PreviousAbilityTargets"/>（原版静态表 `pooledPossibleTargets`，
        /// 写入 = `AbilityLogic__SetPreviousAbilityTargets.c:19-28` 的「先 `Clear` 再 `AddRange`」整表覆盖）。
        ///
        /// 两档**共用这一份候选**，差别只在**后面**：140 直接返回（不过筛），150 落到通用筛选尾
        /// （`GetTargets.c:849-866` vs `:867-882` —— 前者 `:865 goto` 出函数，后者不 return）。
        ///
        /// ⚠️ **`Side` 判不出就出声 + 跳过**（⛔ 不静默算成敌方）：AI 评分按 `Side` 分朋友/敌人
        /// （`AiScoring.ScoreFromCriteria` 的 `Count` / `SideMul`），`-1` 会被算进敌方那一栏。
        /// 今天本表只装**场上单位**（写点 = `EffectResolver.NotePreviousAbilityTargets`）⇒ 这一支零可达；
        /// 原版表里可以有手牌/牌库的卡（`SW51` 的上一跳）。
        /// ⚠️ **2026-10-24 订正（铁律 5 · E2）**：这里原写「取的是 `ctx.LastTargets`」——
        /// **已不成立**：现在发布视图是**本条 ability 至今的并集**（逐 op 同步，`EffectResolver.cs:9793`），
        /// 详见 `资料/普查产出_1024b/E2_结算残值与EvtKind.md`。
        /// </summary>
        static void AddPreviousAbilityTargets(List<AiTargetRef> list, BattleContext ctx)
        {
            var prev = ctx.PreviousAbilityTargets;
            for (int i = 0; i < prev.Count; i++)
            {
                var u = prev[i];
                if (u == null) continue;
                int side = SideOf(ctx, u);
                if (side < 0)
                {
                    UnityEngine.Debug.LogWarning(
                        "[AiTargets] `previousAbilityTargets` 里有**判不出阵营**的候选「"
                        + (u.Name ?? "?") + "」—— **跳过它**（不按敌方算）。"
                        + "判据 → Data/AiTargets.cs 的 AddPreviousAbilityTargets doc");
                    continue;
                }
                list.Add(new AiTargetRef { Unit = u, Inst = u.Instance, Side = side });
            }
        }

        /// <summary>
        /// **`ta == 240 (lowestHealth)`** —— 原版 `AbilityLogic__GetTargets.c:835-844`
        /// （`iVar6 == 0xf0`）：`BattleManager__GetLowestHealthUnit(manager, bVar16)` 取**一个**单位。
        ///
        /// **选的是哪一侧**（`bVar16` 的来路 = `GetTargets.c:207-219`，相对 `thisCard` 解析）：
        /// <code>
        ///   if (thisCard.isPlayer == 0) { bVar16 = criteria.enemyUnits;  ... }   // 敌方 = player 0 那一侧
        ///   else                        { bVar16 = criteria.friendlyUnits; ... }
        /// </code>
        /// 而 `BattleManager__GetLowestHealthUnit.c:63-64` 里 `param_2 == 0` 只收 `isPlayer == 0` 的卡、
        /// `param_2 != 0` 只收 `isPlayer != 0` 的卡 ⇒ **`bVar16` 的含义是「要不要 player 0 那一侧」**。
        /// ⇒ 翻译成本工程的 `thisSide`：
        /// `player0 = (thisSide == 0) ? friendlyUnits : enemyUnits`；选中 = `player0 ? 0 : 1`。
        /// ⚠️ **两侧都勾/都不勾时的行为照抄原版**：两侧都勾且 `thisSide==0` ⇒ 只取 player 0 那侧；
        /// 都不勾 ⇒ 取 player 1 那侧（`bVar16` 假）。
        ///
        /// **挑法**（`BattleManager__GetLowestHealthUnit.c:150-215`）：
        ///   · 只在**该侧**的卡里挑，候选闸 = `IsInPlay` ∧ `!EnoughPendingDamageToDie` ∧
        ///     `cardType ∉ {20 Tactic, 40 Whispers}` ∧ `!isRemnant(+0x65)`；
        ///   · 比的是 `currentHealth`，**严格小于**才换人 ⇒ **并列留先者**；
        ///   · 初始 best 就是**该侧督军**（`:100-104` 那句 `lVar9 = *(hero + 0x28)`），
        ///     所以**空场回落督军**、**并列也优先督军**；遍历序 = `allCardsInPlay` 追加序（= `PlayOrder`）。
        /// </summary>
        static void AddLowestHealthUnit(List<AiTargetRef> list, BattleContext ctx, int thisSide,
                                        AiTargetCriteriaDto c)
        {
            bool player0 = thisSide == 0 ? c.friendlyUnits : c.enemyUnits;
            int side = player0 ? 0 : 1;

            UnitState best = ctx.Players[side].Warlord;   // 原版初始 best = 该侧督军（空场回落 + 并列优先）
            int bestHp = best != null ? best.Health : int.MaxValue;

            // 遍历序照原版 `allCardsInPlay` = 进场追加序 ⇒ 按 `PlayOrder` 稳定排序（⛔ 别用格号：
            // 格号序在「中间补位」之后与进场序不同）。
            var board = ctx.Players[side].Board;
            var order = new List<UnitState>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                if (u == null || ReferenceEquals(u, best)) continue;
                if (!u.IsAlive || ctx.IsPendingDeath(u)) continue;
                if (u.IsRemnant) continue;                       // `+0x65 == 0`
                int ct = CardTypeOf(u.Card);
                if (ct == 20 || ct == 40) continue;              // Tactic / Whispers 不入池
                order.Add(u);
            }
            // 🔴 2026-10-24（F7）：⛔ 别用 `List.Sort` —— 它**不稳定**，而未取号的单位 `PlayOrder == -1`
            //    ⇒ 同号之间次序任意，「并列留先者」会**静默退化**（同一局面两次挑出不同人）。
            //    统一走 `RuleCore.StableSortByPlayOrder`（`-1` 排末尾 + 下标做第二排序键 ⇒ 稳定）。
            RuleCore.StableSortByPlayOrder(order, u => u.PlayOrder);

            for (int i = 0; i < order.Count; i++)
            {
                var u = order[i];
                if (u.Health < bestHp) { best = u; bestHp = u.Health; }   // **严格小于** ⇒ 并列留先者
            }

            if (best != null) AddUnit(list, best, side);
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
        /// 🔴 **2026-10-27（批次 9 · `E7ENG` · 普查 `判据/cardType判据点普查.md` §3·3）就地改口径**：
        /// **真值卡表优先**（`CardDef.CardType`，卡表 v34 起有 `cardType` 列，`-1` = 未知）→
        /// 其次 `ai_scoring.json` 的真值（1,351 张快照，与本列同源）→ 最后才按 `CardDef.Type` 映射兜底。
        /// 改前只有后两条：`ai_scoring` 里没有这张卡时一律 `else ⇒ 20`，会把将来的 `Whispers(40)` 判成 Tactic
        /// （今天 0 张，但那是**猜**）。兜底那一支**出声一次**（同一次装载只喊一次）。
        /// ⚠️ `Type` 映射今天与真值**逐值等价**（池内双射：`unit↔0` · `hero↔10` ·
        /// `{tactic,defence,enchantment}↔20` · `whispers↔40`=0 张）⇒ 兜底不改任何现有行为。</summary>
        static int CardTypeOf(CardDef card)
        {
            if (card == null) return -1;
            if (card.CardType >= 0) return card.CardType;          // ① 真值卡表（v34 起）
            var dto = AiScoringData.Get(card.Id);
            if (dto != null) return dto.cardType;                  // ② ai_scoring 真值
            if (!_cardTypeFallbackWarned)                          // ③ 兜底（猜）—— 出声一次
            {
                _cardTypeFallbackWarned = true;
                UnityEngine.Debug.LogWarning("[AiTargets] `cardType` 真值拿不到（卡表 < v34 且 `ai_scoring.json` "
                    + $"里也没有这张卡）⇒ 按 `CardDef.Type` 兜底（首例 = `{card.Id} {card.Name}`）；"
                    + "同一次装载只喊这一次。");
            }
            if (card.Type == "unit") return 0;
            if (card.Type == "hero") return 10;
            return 20;  // tactic / defence / enchantment —— 真值这三个都是 `cardType = 20`
        }

        /// <summary><see cref="CardTypeOf"/> 第 ③ 支（按 `Type` 猜）的「只喊一次」闩。</summary>
        static bool _cardTypeFallbackWarned;

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
        //
        //  🔴 **2026-10-24（引擎线 `G3`）整块按原版重写** —— 判据 = `C5_剩余判据普查.md` §2
        //     （第一权威：`d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:1164-1366`）。
        //     `targetSelectMethod` 是**纯客户端字段**（卡 SO 的 `TargetCriteria.targetSelectMethod`），
        //     ⛔ 不是服务端下发 —— 所以这一块能逐字复刻。
        //
        //  **状态 → 行为**（`+0x44` = targetSelectMethod；`:1170` 起 `switch`）：
        //  | 值 | 原版 | 本实现 |
        //  |---|---|---|
        //  | 0 | **落 `default:`**：随机取 N（`:1250-1273`） | 同（`TrimRandom`） |
        //  | 1 | `OrderBy(isRemnant(+0x65)).ThenByDescending(max(近战,远程))`（`:1172-1207`） | 同 |
        //  | 2 | `OrderByDescending(GetValueInPlay)`（`:1208-1228`） | 同 |
        //  | 3 | `OrderBy(GetValueInPlay)`（`:1229-1249`） | 同 |
        //  | 4 | **switch 里没有 case 4** ⇒ 落 `default:` = **随机取 N** | 同（并进 `default`） |
        //  | 5 | 建新表、**从尾往前**拷 `maxTargets` 个（`:1274-1310`） | 保尾（`RemoveRange(0, …)`） |
        //  | 6 | `OrderBy(CurrentIntrinsicHealth)`（`:1311-1331`） | 同（`CurrentIntrinsicHealth`） |
        //  | 7 | 建新表、从头拷 `maxTargets` 个（`:1332-1360`） | 保头 |
        //  其余没列的值（含 8 以后）在原版里**同样落 `default:` = 随机** ⇒ 本实现一并走随机。
        //
        //  **并列**：LINQ `OrderBy` 稳定 ⇒ 并列保留源表顺序 = `unitsInPlay(+0x470)` 的**追加序**
        //  = 进场序（`C5` §2·3）。我们的等价物 = `UnitState.PlayOrder`
        //  ⇒ `StableSortWithPlayOrder` 把「主键 → `PlayOrder` → 原下标」三段做全序。
        //  ⚠️ **⛔ 不能用 `List.Sort`**：它**不稳定**（同样输入可能给出不同次序）。
        //
        //  **随机源**：`ctx.AiRng`（`System.Random(seed ^ 0x0A1A1A1A)`，见 `BattleContext.AiRng` 的 doc）
        //  —— 同一局面同 seed 结果固定；⛔ 不用 `UnityEngine.Random`（工程铁律）。
        //  🔴 **有意偏离原版（2026-10-24 主对话拍板 · 不是「等价」）**：
        //     原版 `BattleManager.GetRandomInt` 是**单流**，AI 评分与伤害/随机目标共用它；
        //     本仓既有决定是「**AI 这一侧的随机走 `ctx.AiRng`**，不挪动共享流」
        //     （`BattleContext.cs:371-377`，理由 = `ShowRng` 同族：共享流一动，
        //      同一副牌的动作序列就整体错位，而**回放不重掷**）。⇒ 本处照那条决定走 `AiRng`。
        //     ⚠️ 后果：同一 seed 下，我们的 AI 抽签序列**与同种子的原版局不同** ——
        //       这是**契约优先**的选择（回放 / 自检可复现 > 逐位复刻原生随机序列）。
        //  🔴 **2026-10-24（`G3` 现读就地订正 —— 铁律 5）**：本段原来写「走随机的 0/4 那条
        //  **没有任何评分判据今天会踩到**」——**不成立**。现读 `ai_scoring.json`（1,351 张）：
        //  `targetSelectMethod != 0 或 maxTargets > 0` 的判据共 **7 条**，其中 **6 条是
        //  `method == 0` 且 `maxTargets > 0`**（method 0 一样落 `default:` = 随机，不是「不截」）：
        //    · `BL53 Heldrake Strike`（max 3）· `SOR48 Imperial Creed`（max 3）
        //    · `Lord of the Storm` / `Hero of the Empire` / `GSC56 A Trap Sprung` /
        //      `Grisly Feast`（各 max 1）
        //  ⇒ 这 6 条从「保前 N」变成**随机**，每次评估都会从 `ctx.AiRng` 取数
        //    （**不碰 `ctx.Rng`** ⇒ 不影响伤害/随机目标那一路，也不影响回放动作流）。
        //  📌 只走排序的那 1 条 = `SOR51 Divine Intervention`（`method 1` / `maxTargets 1`），
        //     它的 `includeRemnants` 缺省 false ⇒ 残骸早被筛掉 ⇒ `isRemnant` 主键对它今天**空转**。
        // ==================================================================

        /// <summary>
        /// 按 `targetSelectMethod` 把候选表截断到 `maxTargets` 个。
        /// <para>⚠️ `ctx` 只用于**随机**那一支（原版 `BattleManager.GetRandomInt` → 我们 **`ctx.AiRng`**，
        /// 刻意不走共享的 `ctx.Rng`，理由见上方块注释「有意偏离原版」）。</para>
        /// </summary>
        static void Trim(BattleContext ctx, List<AiTargetRef> list, AiTargetCriteriaDto c)
        {
            // 原版 `:1164-1170`：`param_6`(applyMaxTargets) 为假 或 `maxTargets < 1` ⇒ 不截；
            // `list.Count <= maxTargets` ⇒ 不截（连排序都不做）。
            if (c.maxTargets <= 0 || list.Count <= c.maxTargets) return;

            switch (c.targetSelectMethod)
            {
                case 1: // highestAttack —— 🆕 主键 `isRemnant`（升序：非残骸在前）、次键 `max(近战,远程)` 降序
                    StableSortWithPlayOrder(list, delegate (AiTargetRef a, AiTargetRef b)
                    {
                        int c1 = (a.IsRemnant ? 1 : 0).CompareTo(b.IsRemnant ? 1 : 0);
                        if (c1 != 0) return c1;
                        return b.HighestAttack.CompareTo(a.HighestAttack);
                    });
                    break;
                case 2: // highestValueInPlay
                    StableSortWithPlayOrder(list, delegate (AiTargetRef a, AiTargetRef b)
                    {
                        return SimpleAI.ValueInPlay(b.Unit).CompareTo(SimpleAI.ValueInPlay(a.Unit));
                    });
                    break;
                case 3: // lowestValueInPlay
                    StableSortWithPlayOrder(list, delegate (AiTargetRef a, AiTargetRef b)
                    {
                        return SimpleAI.ValueInPlay(a.Unit).CompareTo(SimpleAI.ValueInPlay(b.Unit));
                    });
                    break;
                case 6: // lowestHealth —— 🆕 键从裸 `Health` 换成 `CurrentIntrinsicHealth`
                    StableSortWithPlayOrder(list, delegate (AiTargetRef a, AiTargetRef b)
                    {
                        return CurrentIntrinsicHealth(a).CompareTo(CurrentIntrinsicHealth(b));
                    });
                    break;
                case 5: // lastAdded —— 保尾部
                    list.RemoveRange(0, list.Count - c.maxTargets);
                    return;
                case 7: // firstAdded —— 保头部
                    break;
                default: // 0 random / 4 cardTarget（原版**没有 case 4**）—— 随机取 N
                    TrimRandom(ctx, list, c.maxTargets);
                    return;
            }
            if (list.Count > c.maxTargets) list.RemoveRange(c.maxTargets, list.Count - c.maxTargets);
        }

        /// <summary>
        /// 原版 `default:` 支（`GetTargets.c:1250-1273`）：
        /// <code>
        ///   lVar7 = new List&lt;…&gt;();
        ///   for (i = 0; i &lt; maxTargets; i++) {
        ///     uVar4 = BattleManager__GetRandomInt(param_1, 0, list.Count);  // [0, Count)
        ///     List__Add(lVar7, List__get_Item(list, uVar4));
        ///     List__RemoveAt(list, uVar4);                                  // 取一个删一个
        ///   }
        ///   list = lVar7;                                                   // **结果按抽中序**
        /// </code>
        /// ⇒ 无放回随机取 `maxTargets` 个，**结果的次序 = 抽中的次序**（不是原表次序）。
        /// 我们照抄这个形状（`ctx.AiRng.Next(list.Count)` 同 `GetRandomInt(0, Count)`），
        /// 但**随机流换成 `AiRng`**（原版是单流 `GetRandomInt`）——这处**有意偏离**，
        /// 理由 = 本仓「AI 随机不挪动共享流」的既有决定，见上方块注释。
        /// </summary>
        static void TrimRandom(BattleContext ctx, List<AiTargetRef> list, int keep)
        {
            if (keep >= list.Count) return;
            var picked = new List<AiTargetRef>(keep);
            for (int i = 0; i < keep; i++)
            {
                int k = ctx.AiRng.Next(list.Count);    // 上界开区间 ⇒ [0, Count)；流 = AiRng（见上）
                picked.Add(list[k]);
                list.RemoveAt(k);
            }
            list.Clear();
            list.AddRange(picked);
        }

        /// <summary>
        /// **稳定排序 + 并列按进场序**：主键相同时比 `PlayOrder`（`-1` 排末尾），仍然相同再比原下标。
        /// ⇒ 等价于原版「源表 = `unitsInPlay` 追加序」上的 LINQ 稳定 `OrderBy`。
        /// ⚠️ **⛔ 别改成 `list.Sort(cmp)`**：那是**不稳定**排序，并列时次序取决于内部实现。
        /// </summary>
        static void StableSortWithPlayOrder(List<AiTargetRef> list, System.Comparison<AiTargetRef> primary)
        {
            int n = list.Count;
            if (n < 2) return;
            var items = new AiTargetRef[n];
            var rank = new int[n];
            var idx = new int[n];
            for (int i = 0; i < n; i++)
            {
                items[i] = list[i];
                int po = items[i].PlayOrder;
                rank[i] = po < 0 ? int.MaxValue : po;     // 没进场号 ⇒ 末尾（与 `RuleCore.StableSortByPlayOrder` 同口径）
                idx[i] = i;
            }
            System.Array.Sort(idx, delegate (int a, int b)
            {
                int r = primary(items[a], items[b]);
                if (r != 0) return r;
                r = rank[a].CompareTo(rank[b]);
                if (r != 0) return r;
                return a.CompareTo(b);                   // 全序兜底 ⇒ 确定性
            });
            for (int i = 0; i < n; i++) list[i] = items[idx[i]];
        }

        /// <summary>
        /// 🆕 **`EntityScript.CurrentIntrinsicHealth()`**（`targetSelectMethod == 6` 的排序键）。
        ///
        /// 判据 = `d:/2/tools/decomp_full/EntityScript__get_CurrentIntrinsicHealth.c:44-62`：
        /// <code>
        ///   iVar3 = 0;
        ///   foreach (var eff in activeEffects(+0x108))            // List&lt;CardEffect&gt;
        ///     if (eff.extrinsic(+0x30) != 0 &amp;&amp; eff.buffType(+0x38) == 1 /*addStats*/)
        ///       iVar3 += eff.healthChange(+0x60);
        ///   return currentHealth(+0x68) - iVar3;
        /// </code>
        /// ⇒ **当前生命 − 「extrinsic（在场上才成立）的 addStats 效果」给的那份生命**。
        /// （`buffType` 枚举 → `dump.cs:45430-45450`；`CardEffect` 字段 → `dump.cs:119071-119094`。）
        ///
        /// 🔴 **我们的等价物与它的边界**（如实标注，⛔ 别当成逐字等价）：
        ///   · 我们**没有** `CardEffect` 对象表（属性 buff 直接折进 `UnitState.Health/MaxHealth`），
        ///     唯一**带账**的 extrinsic 一族 = **光环层**（`Auras.ApplyAura` 走
        ///     `UnitState.RecordGrant(…, Auras.GrantTag)`，`Core/Aura.cs:805`）；
        ///     原版 `extrinsic = abilityData.whileInPlay || whileInPlayVariable`（`dump.cs:19557-19566`）
        ///     而我们的 whileInPlay 能力**全都落在光环层**（`CardDef.AuraSpecs`）⇒ 这一族对得上。
        ///   · 因此**只减 `Source == Auras.GrantTag` 且 `Attr == "health"` 的那几条**。
        ///     ⛔ **不减**其它 health 记账（例：黑暗契约 `EffectResolver.cs:10376` 的 grant 是
        ///     **永久关键词**给的、原版 `extrinsic` 为假 ⇒ 按原版「不减」）。
        ///   · ⚠️ **已知不覆盖**：(a) 不走 `RecordGrant` 的 extrinsic 生命增益（今天没有产出者）；
        ///     (b) 原版 `buffType == 9 setHealth`（如 `Eternal Servitude` 翻倍）**原版本来也不减** ——
        ///     我们也不减 ✓；(c) 手牌/牌库候选没有运行时附魔 ⇒ 直接返回卡面 `Health`（原版同样只减
        ///     `activeEffects`，手里那份为空）。
        ///   ⛔ **别改用 `MaxHealth − Card.Health`**：那会把**非 extrinsic** 的生命增益（一次性 buff）
        ///     和 `setHealth` 一起减掉 —— 原版明确只减 extrinsic 那一族。
        /// </summary>
        static int CurrentIntrinsicHealth(AiTargetRef t)
        {
            if (t.Unit == null)
            {
                var card = t.Card;
                return card != null ? card.Health : 0;
            }
            int h = t.Unit.Health;
            var grants = t.Unit.PendingGrants;
            for (int i = 0; i < grants.Count; i++)
            {
                var g = grants[i];
                if (g == null || g.Attr != "health") continue;
                if (g.Source != Auras.GrantTag) continue;    // 只减 extrinsic（whileInPlay）那一族
                h -= g.Value;
            }
            return h;
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

        /// <summary>
        /// 原版 `CardScript.get_mightAct` —— `AI.ScoreFromCriteria` 的 option 5 循环体第一句就是它
        /// （`AI__ScoreFromCriteria.c` 的 `:0x18093A934-0x18093A936`：`get_mightAct` 假 ⇒ `je` 跳过该目标）。
        ///
        /// 🔴 **不在这里另写一份判据** —— 转发到引擎侧唯一判据 `ConditionEval` 的 `case 20`
        ///   （同源：`CardScript__get_mightAct.c`，逐句见 `ConditionEval.cs` 的 `20 mightAttack` 段：
        ///   ① 本回合已攻击过 ⇒ 必须 `bloodThirst` 且 `== 1`；② 必须是单位/督军；③ `displaySummonSickness`；
        ///   ④ `stun` / `cantAttack` / `nonCombatant` 任一）。
        ///   `ConditionEval` 判不了时返回 `null` ⇒ 这里取 `false`（AI 侧保守口径：原版不成立就不算目标）。
        ///
        /// ⚠️ **不在场的候选**（手牌 / 牌库 / 弃牌堆里那一份）**没有 `EntityScript`** ⇒ 原版
        ///   `mightAct` 无对象可读 ⇒ 这里返回 **false**（等价于「手牌卡不能行动」）。
        /// </summary>
        public static bool MightAct(BattleContext ctx, AiTargetRef t)
        {
            if (t.Unit == null) return false;
            bool? r = ConditionEval.MeetsUnit(ctx, t.Unit, 20, false);
            return r ?? false;
        }
    }
}
