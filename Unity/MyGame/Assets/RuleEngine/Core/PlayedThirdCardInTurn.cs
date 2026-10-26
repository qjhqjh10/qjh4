// PlayedThirdCardInTurn.cs — `AbilityTrigger.PlayedThirdCardInTurn = 15` 的**结算口** · 账 `A1547`
//
// ── 原版判据（第一权威 = 反编译方法体）────────────────────────────────
// `d:/2/tools/decomp_full/RawCardScript__OnCardPlayedWithTarget.c:105-112`：
//     cVar1 = UnityEngine_Object__op_Inequality(thisCard, cardPlayed, 0);   // thisCard != cardPlayed
//     if (cVar1 != '\0') {
//         iVar2 = BattleManager__CardsPlayedInTurn(bm);
//         if (iVar2 == 3)
//             RawCardScript__TriggerAbilities(this, 0xf /*PlayedThirdCardInTurn*/, bm,
//                                             thisCard, cardPlayed, targetCard, …);
//     }
//   · `CardsPlayedInTurn` = `BattleManager + 0x3D0` 那个 `ObscuredInt`
//     （`BattleManager__CardsPlayedInTurn.c:16-25`；写点 = `_ResolvePlayCardFromHand_d__447__MoveNext.c:1390-1406`
//      的 `op_Increment`；清点 = `_NextTurn_d__395__MoveNext.c:231` 写 0）⇒ **只认 `== 3`，不是 `>= 3`**。
//   · 🔴 **不是**「本回合打过三张牌就在刚打出的那张上跑效果」——
//     `HasPlayedThirdCardInTurn`（`RawCardScript__HasPlayedThirdCardInTurn.c:3-56`）读的是
//     `this + 0x290 cardAbilities[].trigger == 0xf`，即「**这张卡自己带不带这个能力**」；
//     触发时 `this` = **进攻卡（环境卡）那条 RawCardScript**，不是刚打出的那张牌。
//   · `thisCard != cardPlayed` 这一闸在我们这里**恒成立**：进攻卡不在手牌里，永远不是「刚打出的那张」。
//
// ── 数据从哪来 ────────────────────────────────────────────────────────
// `Core/OffensiveCardData.cs` + `Data/OffensiveCardMap.cs`（真值快照
// `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`，`trigger == 15` 全快照只有 2 张）。
// 键 = `ctx.OffensiveEnvSO`（`ChooseOffensiveCard` 第 4 实参 = `OffensiveCards.json` 的 `envSO`）。
//
// ── 效果怎么落 ────────────────────────────────────────────────────────
// `abilityLogic[]` 逐条跑（原版 `TriggerAbilities` → `AbilityLogic.PlayAbility`）：
//   · `doDamage`（`AbilityEffect = 10`）：目标池由 `friendlyUnits` / `enemyUnits` + `includeHero /
//     includeMinions / includeRemnants` 决定；`maxTargets` 超了按 `TargetSelectMethod.random`（= 0）
//     随机挑；伤害 `minDamage~maxDamage`，`DamageType.Random` / `ValueOptions.RandomizedOnResolution`
//     时**在结算这一刻**随机（原版 `ChooseValueOptions.RandomizedOnResolution = 90`）。
//   · `drawCard`（`AbilityEffect = 0`）：抽牌那一侧由 `playersAffected` 决定
//     （`Both = 0` / `Self = 10` / `Enemy = 20`）—— 原版 Tooltip 点明这个字段就是给 DrawCard 用的。
//   · 其它 effect：**出声**（⛔ 不静默）—— 今天的数据里没有，但将来加表时必须看得见。
//
// ⚠️ **`includeSelf`**（Empyric Rift 第一条 ability 有）：原版那一格指「能力自己的那张卡」，
//    而进攻卡是**环境 SO、不在棋盘上** ⇒ 它不产生额外目标。如实标着，⛔ 别拿它去挑场上单位。
// ⚠️ **卡面正文不在本地**（两条 SO 的文本引用字段为空，本地化表在远端）⇒ 日志里的效果描述
//    是**按 abilityLogic 拼的占位**，不是原版文案。⛔ 别把它当官方文本。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>
        /// **本回合第 3 张牌打出**时调一次（`PlayCard` 单位支 / `PlayTactic` 战术支的收尾各一处）。
        /// 条件不满足时**什么都不做**（原版那道 `== 3` 闸）。
        ///
        /// ⚠️ 调用点自己负责「这张牌确实打出去了」——本方法不再判 `CanPlay*`。
        /// </summary>
        /// <param name="playedBySeat">刚打出这张牌的座位（**只用于日志**：原版这道闸不按座位分）。</param>
        /// <param name="playedCard">刚打出的那张牌（同样只用于日志 / 可空）。</param>
        public static void FirePlayedThirdCardInTurn(BattleContext ctx, int playedBySeat, CardDef playedCard)
        {
            if (ctx == null) return;
            // 🔴 原版 `iVar2 == 3`（不是 >=）—— 第 4、5 张不再触发。
            if (ctx.CardsPlayedInTurn != 3) return;

            int seat = ctx.OffensiveSeat;
            if (seat < 0 || seat > 1) return;      // -1 = 这一局没选进攻卡（空卡那一路）

            var entry = OffensiveCardData.Get(ctx.OffensiveEnvSO);
            if (entry == null) return;             // 这张进攻卡不带 trigger 15（全快照只有 2 张带）

            var abilities = OffensiveCardData.ThirdCardAbilities(entry);
            if (abilities.Count == 0) return;

            // `thisCard != cardPlayed` 恒成立（进攻卡不在手牌里，见文件头）。
            string playedName = playedCard == null ? "（未知）" : playedCard.Name;
            ctx.Log($"★ 本回合第 3 张牌（{playedName}，P{playedBySeat + 1}）打出 ⇒ 进攻卡"
                  + $"「{(string.IsNullOrEmpty(entry.nameZh) ? entry.name : entry.nameZh)}」"
                  + " 的 PlayedThirdCardInTurn 触发（卡面文案在远端，本地按 abilityLogic 结算）");

            // 🔴 2026-10-21（`T03·c` 批 B 接线）：**这条 ability 也只入队**（原版
            //   `RawScript.OnCardPlayedWithTarget:105-112` → `TriggerAbility` → `PlayAbility`
            //   → `AddAutoActionToQueue`）。⚠️ 那道 `CardsPlayedInTurn != 3` 的闸留在**入队这一步**
            //   （原版也是先判后排队）；`abilities` / `seat` 按帧捕获。
            var _ab = abilities; var _seat = seat; var _entry = entry;
            EnqueueAbility(ctx, ListenerLane(null), "第3张牌:" + playedName,
                           () =>
                           {
                               for (int ai = 0; ai < _ab.Count; ai++)
                               {
                                   var ab = _ab[ai];
                                   if (ab == null || ab.logic == null) continue;
                                   for (int li = 0; li < ab.logic.Length; li++)
                                       ApplyOffensiveLogic(ctx, _seat, _entry, ab.logic[li]);
                               }
                           });
        }

        /// <summary>跑一条 `AbilityLogic`（见文件头的「效果怎么落」）。</summary>
        static void ApplyOffensiveLogic(BattleContext ctx, int seat, OffensiveCardEntry card,
                                        OffensiveLogic lg)
        {
            if (lg == null) return;
            string label = string.IsNullOrEmpty(card.nameZh) ? card.name : card.nameZh;

            if (lg.IsDamage)
            {
                var targets = PickOffensiveTargets(ctx, seat, lg);
                if (targets.Count == 0)
                {
                    ctx.Log($"「{label}」的伤害 effect 没有合法目标（进攻卡第 3 张牌触发）—— 这一步空过");
                    return;
                }
                for (int i = 0; i < targets.Count; i++)
                {
                    int dmg = RollDamage(ctx, lg);
                    var t = targets[i];
                    ctx.Log($"「{label}」对 {t.Name} 掷出 {dmg} 点伤害"
                          + (lg.maxDamage > lg.minDamage ? $"（{lg.minDamage}~{lg.maxDamage} 随机）" : ""));
                    Hurt(ctx, t, dmg, label + "（PlayedThirdCardInTurn）", seat);
                }
                return;
            }

            if (lg.IsDraw)
            {
                // 判据 = 原版 `PlayersAffected`（`PlayersAffected.cs`：0 Both / 10 Self / 20 Enemy），
                // 且 `AbilityLogic.cs` 的 Tooltip 明确写着这一格是给 DrawCard / DiscardWholeHand 用的。
                // 🔴 **2026-10-26（`B8_AN`）**：这里原来是 `if/else` 三档、**兜底 = 两侧都抽** ——
                //   原版 `SupportMethods.IsPlayerAffected.c:1-15` 对**未列出的非 0 值返回 false**
                //   （两侧都不抽）⇒ 那个兜底会把「谁都不抽」做成「两人都抽」。已改走统一判据
                //   <see cref="IsPlayerAffected"/>（判据只此一处），未知档 ⇒ 不抽并出声。
                //   参考系 = 【施放这张牌的那一侧】：原版 `AbilityLogic__PlayAbility.c:308/324` 两次问
                //   `IsPlayerAffected(card.isPlayer, pa)` 与 `IsPlayerAffected(!card.isPlayer, pa)`，
                //   `d:/2/tools/il2cpp_out/dump.cs:21973` 证 `+0x40` 就是 `<isPlayer>k__BackingField`
                //   ⇒ `Self` 落在「出牌者那一侧」，与我们传 `seat` 同义。
                int drawn = 0;
                for (int s2 = 0; s2 < 2; s2++)
                {
                    if (!IsPlayerAffected(s2 == seat, lg.playersAffected)) continue;
                    Draw(ctx, s2); drawn++;
                }
                ctx.Log($"「{label}」触发抽牌（playersAffected = {lg.playersAffected ?? "?"} ⇒ 抽 {drawn} 侧）");
                return;
            }

            // 红线：不许静默失败 —— 表里出现没实现的 effect 必须看得见。
            ctx.Log($"⚠️「{label}」的 abilityLogic `{lg.effect}` **尚未实现** —— 这一步被跳过"
                  + "（A1547 只落了 doDamage / drawCard；原版 `AbilityEffect` 词表见 `AbilityEffect.cs`）");
        }

        /// <summary>
        /// 按 `TargetCriteria` 挑目标（只挑**场上单位**；`includeSelf` 见文件头）。
        /// `friendlyUnits` / `enemyUnits` 相对**进攻卡的主人**（`seat`）而言。
        /// </summary>
        static List<UnitState> PickOffensiveTargets(BattleContext ctx, int seat, OffensiveLogic lg)
        {
            var pool = new List<UnitState>();
            if (!lg.friendlyUnits && !lg.enemyUnits) return pool;   // 没有阵营标记 ⇒ 不猜

            for (int side = 0; side < 2; side++)
            {
                bool isFriendly = side == seat;
                if (isFriendly && !lg.friendlyUnits) continue;
                if (!isFriendly && !lg.enemyUnits) continue;

                var ps = ctx.Players[side];
                if (ps == null) continue;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ps.Board[s];
                    if (u == null) continue;
                    if (EnoughPendingDamageToDie(u)) continue;       // 已经死了的不挑
                    if (u.IsWarlord)
                    {
                        if (!lg.includeHero) continue;
                    }
                    else if (u.IsRemnant)
                    {
                        if (!lg.includeRemnants) continue;
                    }
                    else
                    {
                        if (!lg.includeMinions) continue;
                    }
                    pool.Add(u);
                }
            }

            // `TargetSelectMethod.random = 0`（`TargetSelectMethod.cs:3`）—— 超上限就随机挑。
            // ⚠️ 随机源**只许 `ctx.Rng`**（不用 `UnityEngine.Random`：对局必须可复现）。
            if (lg.maxTargets > 0 && pool.Count > lg.maxTargets)
            {
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = ctx.Rng.Next(i + 1);
                    var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
                }
                pool.RemoveRange(lg.maxTargets, pool.Count - lg.maxTargets);
            }
            return pool;
        }

        /// <summary>
        /// 伤害值：`minDamage`；若 `DamageType.Random`（5）或 `ValueOptions.RandomizedOnResolution`（90）
        /// ⇒ 在**结算这一刻**在 `[min, max]` 里随机（原版 `ChooseValueOptions.cs:13`）。
        /// </summary>
        static int RollDamage(BattleContext ctx, OffensiveLogic lg)
        {
            if (lg.maxDamage > lg.minDamage
                && (string.Equals(lg.damageType, "Random", StringComparison.Ordinal)
                    || string.Equals(lg.valueOptions, "RandomizedOnResolution", StringComparison.Ordinal)))
                return lg.minDamage + ctx.Rng.Next(lg.maxDamage - lg.minDamage + 1);
            return lg.minDamage;
        }

        // ==========================================================================================
        //  🆕 2026-10-26（执行代理 `B8_AN` · 账 战斗逻辑驱动:1488）
        //  🔴 **一律追加在类尾**（本文件被账按行号引用；往中间插成员会让下游落点整体下移）。
        // ==========================================================================================

        /// <summary>
        /// 原版 `SupportMethods.IsPlayerAffected(bool isPlayer, PlayersAffected playersAffected)`
        /// （`decomp_full/SupportMethods__IsPlayerAffected.c:1-13`，🥇 全文 13 行逐句）。
        ///
        /// <para>真值表（逐句照抄）：`Both(0)` ⇒ **true** · `Self(10)` ⇒ `isPlayer` ·
        /// `Enemy(20)` ⇒ `!isPlayer` · **其它任何非 0 值 ⇒ false**
        /// （`.c:5-6` 的 `return param_2 == 0x14` 与 `.c:9-10` 的 `return param_2 == 10` ——
        /// 它们是**等值比较**、不是「非 0 即真」⇒ 未知值两边都落 `false`，⛔ 不是当 `Both`）。</para>
        ///
        /// <para>🔴 **参考系**（本轮查实的唯一一格，原来账上标着「两种读法都自洽」）：
        /// 原版**唯一**调用点 `AbilityLogic__PlayAbility.c:308/324` 是**同一个 `pa` 问两次** ——
        /// `IsPlayerAffected(card.isPlayer, pa)` 与 `IsPlayerAffected(!card.isPlayer, pa)`，
        /// 再交给 `BattleManager.GetCardsInHandList(…, 两个 bool, …)` 取并集。
        /// `d:/2/tools/il2cpp_out/dump.cs:21973` 证 `+0x40` 就是 `<isPlayer>k__BackingField`
        /// ⇒ **`isPlayer` 是「这张施放中的卡属于哪一侧」的那个位**，不取反。
        /// 在 `Self` 档上两次问法只会有一次为真 ⇒ 落在**出牌者那一侧**。</para>
        ///
        /// <para>`playersAffected` 在本工程里的载体 = <see cref="OffensiveLogic.playersAffected"/>
        /// （字符串 `Both` / `Self` / `Enemy`；`null`/空串按 `Both` 解 —— 原版缺省值就是 `0`）。</para>
        /// </summary>
        public static bool IsPlayerAffected(bool isPlayer, string playersAffected)
        {
            if (string.IsNullOrEmpty(playersAffected) || playersAffected == "Both") return true;
            if (playersAffected == "Self") return isPlayer;
            if (playersAffected == "Enemy") return !isPlayer;
            return false;                       // 原版 `.c:5-11`：未列出的非 0 值 ⇒ false（⛔ 不是 Both）
        }
    }
}
