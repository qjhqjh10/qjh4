// EventTiming.cs — 「引擎事件 → 隔多久播下一条」的时间表
//
// 引擎把一整套动作发成**按发生顺序排好的事件流**（`Attack → Hit → Death → Trigger`），
// 但**没说每条之间隔多久**。以前表现层是同一帧把整串一起播掉的，看不出节奏。
//
// ⚠️ **先说清楚这张表的性质**：原版**没有「事件级」的间隔表**。翻遍了
//    `数据/游戏数据/tween/*.json`（74 个）·`animinfo_lookup.json`（418 条）·
//    `card_anim_map.json`（506 条）·`animfx_components.json`（2346 个组件）——
//    能拿到的只有**单个特效内部**的 `delay`/`duration`。
//    但**真反编译的方法体也在本地**（`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/`，1800 个），
//    从里面挖到了两条**真正的动作间隔**（见 `AttackWindUp` 和 `TriggerHold`）。
//    每条都标了出处；**以「拍的」结尾的是估的，不是原版的数**。
//
// 唯一一处「事件之间要互相等」的**显式**标记：`UnitTweenSO.waitAnimation = 1`
// （18 个 tween，如 `Mutation` / `Execution_BL` / `Hammer Slam` / `AeldariRecall`）
// —— 原版会**等这段 tween 播完再走下一步**。我们用 `Duration` 表达同一件事。
//
// 🆕 2026-10-14（`A1767`）：**「发动能力」这一格不再是常数** —— 它按**卡**取值，
//   判据 = 该卡 `activeAbility` 各 CardAnim 的 `AnimInfo.GetAnimDuration()` 之和。
//   表在 `Resources/ability_anim_durations.json`（🔴 **现读 93 张卡 / 160 条 CardAnim**，2026-10-22
//   `W64` 重出后的数；⚠️ 这里原写「56 张卡」「62 张卡」**都已过期**，账 `A2095` ——
//   ⛔ 别手抄这个数，读表里的 `count`），入口 `Core/AbilityAnimDurations.cs`，
//   消费点 = 本文件的 `DurationOf(BattleEvent)`（⚠️ **只按 `EvtKind` 的那条重载给兜底**）。
//   ⛔ 改这一格之前先读那两个文件的文件头。
//
// 🔴 **2026-10-22（`W64` · B17）就地订正三条（铁律 5）**：
//   ① **真值源不是「2022 快照」** —— 现读是**当前 build 的卡 SO**
//      （`资料/普查产出_1018/allcards_1350/cards_raw.json` 的
//      `activeAbility.activeAbilityLogic[*].cardAnimations[*]`，时长 = `AnimInfo.GetAnimDuration()`）。
//      所以 UM / SW 那两批**不再是「判据为空」**；第二条链 `cards_engine.json` 的 `animTimes`（`tg == -1` 行）
//      与它**逐卡一致 93 / 不一致 0**（全量件 `_crossCheck`）。
//   ② **3 张例外**（全量件 `uncovered`，都是「会发 Ability 事件、但判据不足以定值」）：
//      `SOR30 Celestian Sacresant` · `SOR65 Celestian Sacresant Aveline` · `SW9` ——
//      它们的 `activeAbility` **一条非空 CardAnim 引用都没有**（`m_AssetGUID` 全空）
//      ⇒ 原版这条路上**不放任何动画** ⇒ 我们退回兜底并**出声**（不是「表里漏了」）。
//   ③ **`SW21 Wulfen Pack Leader` 是条件分支**（铁律 5·c：同一张卡、不同棋盘状态 = 不同时长）：
//      **普通 `activeAbility` = 0.4**（表里存的就是这支）· **替代能力 = 0.2**（`Cut Wulfen`）。
//      🔴 **2026-10-23 就地订正（铁律 5 · 判据 = 第一权威反编译）**：上一行原来写
//      「`hasAlternativeAbility=1` 且 `MeetsCriteria(**selectCriteria**)` **满足时**原版走替代支」
//      —— **字段名与方向都不对**。`RawCardScript__GetActiveAbility.c` 逐句是
//      `if (hasAlternativeAbility(+0x2AA)) { if (!BoardAnalysis.MeetsCriteria(param_2, *(this+0x2B0), …))
//          return *(this+0x2B8); }  return *(this+0x260);`
//      ⇒ 判据字段 = **`normalAbilityBoardCriteria`(+0x2B0)**（卡上**没有**叫 `selectCriteria` 的字段），
//        且**满足时走的是【普通】支**、**不满足**才落到 `alternativeAbility`(+0x2B8)。
//      ⛔ 表只记普通那一支；运行时**要不要切到 0.2 还没接** —— 而且**数据侧也缺**：
//        `normalAbilityBoardCriteria` **不在**我们的卡导出里（`cards_raw.json` 的 `cards[*]` 只有
//        `hasAlternativeAbility` / `activeAbility` / `alternativeAbility` 三样）⇒ 要接得先补那个字段的导出。
//        逐条见全量件 `conditional`。
using RuleEngine;

namespace CardPresentation
{
    public static class EventTiming
    {
        // ==================================================================
        //  🆕 2026-10-22（`W64` · 反击演出）：**两个取数口（由 `BattleDriver` 挂钩）**
        // ==================================================================
        //  为什么需要：`DurationOf(Retaliation)` 要算「守方那件**逐卡**件自己演多久」，
        //  它需要 **① 该座位的阵营**（原版读「施法者自己的 clan」）与 **② 那张卡的
        //  `CardDef.AnimTimes`**（逐候选时长真值）—— 这两样只有驱动有（本局阵营 / 卡池）。
        //  模式与 `AnimAnchors.*Of` 完全一样：静态钩子 + 幂等覆盖；**没挂钩 = 拿不到逐卡数据**
        //  ⇒ 走 `RangedVfxFallback` 并出声（`RetaliationHoldOf`）。
        //  ⛔ 别在 `EventTiming` 里自己去载卡池（那是第二份数据源，且拿不到「本局座位 ↔ 阵营」）。

        /// <summary>座位（0/1）→ 阵营名。`null` = 没挂钩 / 判不出。</summary>
        public static System.Func<int, string> FactionOfSeat;

        /// <summary>(卡名, 阵营) → 那张卡的 `CardDef.AnimTimes`（`null` = 这张卡没有逐候选时长）。</summary>
        public static System.Func<string, string, System.Collections.Generic.IReadOnlyList<CardDef.CardAnimTime>>
            AnimTimesOf;

        /// <summary>🆕 **2026-10-23（R19）：座位（0/1）是不是「本地玩家」那一侧** —— `null` = 没挂钩。
        ///
        /// <para>**为什么需要**：`VarsGlobal.TimeToDrawCard(bool isPlayer)` 原版是**分敌我的一对**
        /// （我方 `timeToDrawPlayerCard = 0.3` / 敌方 `timeToEnemyDrawCard = 0.15`，
        /// `资料/VarsGlobal_原版数值.md` §一），而 `EventTiming` 这张表拿不到「哪一方是我」——
        /// 只有 `BattleEvent.Player`（座位号）与驱动里的 `_me`。
        /// ⇒ 由 `BattleDriver` 挂钩回答（模式与 <see cref="FactionOfSeat"/> 完全一样：静态钩子 + 幂等覆盖，
        /// 每次 `Begin` 重挂；没挂 = 纯引擎自检 ⇒ 退回「按我方档并出声」）。</para>
        ///
        /// <para>⚠️ 语义是「**本地玩家**」**不是**「座位 0」：联机局里本机可能是座位 1
        /// （原版那个 `bool isPlayer` 也是这个意思 —— `PlayerHand._AddDrawnCardToHand` 的实参）。</para></summary>
        public static System.Func<int, bool> IsLocalSeat;

        /// <summary>🆕 **2026-10-25（`AU1`）：(卡名, 关键词) → 这张卡**有没有**这个关键词**（按卡面/基础关键词表，
        /// 不是运行时 buff）。`null` = 没挂钩 ⇒ 一律回 `false` **并出声一次**（见 `ReturnHoldOf`）。
        ///
        /// <para>**为什么需要**：`EvtKind.Return`（回手）这一拍在原版对**带 `teleport` 的卡**多等一次
        /// `GetHandToBoardAnimEventTime()`（`CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:134-141`
        /// 的 `RawCardScript.HasDefaultTrait(card, 0x4ce)` 支，`0x4ce = 1230 = teleport`，
        /// 判据 → `数据/游戏数据/trait_collection.json` 的 `definedTrait: 1230`）——
        /// 而「这张卡有没有 teleport」只有驱动（卡池）答得出来。模式与
        /// <see cref="FactionOfSeat"/> / <see cref="AnimTimesOf"/> 完全一样：静态钩子 + 幂等覆盖，每次 `Begin` 重挂。</para></summary>
        public static System.Func<string, string, bool> HasTraitOf;

        // ==================================================================
        //  🆕 2026-10-25（`AU1` · 第三批 · 判据代理 `J1`）—— **三个新钩子（同样由 `BattleDriver` 挂）**
        // ==================================================================
        //  为什么都要钩子而不是在这里自己查：这三格的判据都在**驱动/引擎**那一侧（卡池、棋盘状态、
        //  这份实例现在落在手牌还是牌库）—— 本类是纯时间表，⛔ 别在这里载第二份数据源。

        /// <summary>🆕 **2026-10-25（`AU1` · `J1` §3）：这一条 `Return` 的**目的地** ——
        /// `0` = 回手 · `1` = 回牌库（含牌库顶）· `-1` = 判不出（旧事件 / 自检手工合成）。`null` = 没挂钩。
        ///
        /// <para>**为什么要钩子**：`BattleEvent` 只带 `Slot`（在场格号）、**没有「回手还是回牌库」那一位**
        /// （`RuleEngine` 侧 `op.Dest` 没进事件）—— 但驱动**判得出**：引擎是先把那份实例搬进
        /// `Players[p].Hand` 或 `.Deck` **之后**才发这条事件的（`EffectResolver` 的 `switch (op.Dest)`），
        /// 而事件带 `InstanceId` ⇒ 按实例号在手上/牌库里反查即可。`cardback` 那一档（`MoveToDeck` 归零）**没接**，见 `ReturnHoldOf`。</para></summary>
        public static System.Func<BattleEvent, int> ReturnDestOf;

        /// <summary>🆕 **2026-10-25（`AU1` · `J1` §5）：这一条 `Hit` 打的是**易伤伤害**（`DamageType 7/8`）吗。
        /// `null` = 没挂钩（⇒ 一律按普通命中 1.0 s）。判据与等价物 → <see cref="HitHoldOf"/>。</summary>
        public static System.Func<BattleEvent, bool> HitIsVulnerable;

        /// <summary>🆕 **2026-10-25（`AU1` · `J1` §6）：这一次「发动能力」原版走的是**替代支**吗。
        /// `true` = `alternativeAbility`（SW21 那类）· `false` = `activeAbility`。`null` = 没挂钩。
        /// 判据 = `RawCardScript.GetActiveAbility`（`hasAlternativeAbility &amp;&amp; !MeetsCriteria(normalAbilityBoardCriteria)`
        /// ⇒ 替代支），驱动侧已有纯函数 `CardDef.UsesAlternativeAbility(ctx, owner, actor)`。→ <see cref="AbilityHoldOf"/></summary>
        public static System.Func<BattleEvent, bool> AbilityUsesAlternative;

        /// <summary>🆕 **2026-10-22（`W64`）反击那一拍占多久**（秒，相对事件起播）。
        ///
        /// <para>**判据（第一权威）= `BattleManager._ResolveAttack_d__438__MoveNext.c:1114-1152`**：
        /// 远程打法那一支 `:1119-1131` 起 `CardScript.ResolveAttackRangedAnim(守方, 攻方, 0, 0)`
        /// （`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c`：回正三条 tween `AttackStepTime` +
        /// `WaitForSeconds(AttackStepTime)` → `DelayedVFXPlay` → `WaitForSeconds(GetAnimDuration())`），
        /// **协程跑完才**落伤害（`:1136-1147`）⇒ 这一拍 = **前摇 `AttackStepRanged` + 那件件自己的链时长**。
        /// 近战打法那一支（`:1132-1150`）守方**什么都不播** ⇒ **0**。</para>
        ///
        /// <para>⚠️ 件本身走 `VfxMap.ChainDuration(AttackRanged, 守方阵营, 守方卡, AnimTimes)` ——
        /// 与**攻方**那一发同一个口；查不到（本工程常态：`customRangedAttackParticles` 是卡 SO 的
        /// 顶层字段，不进 `AnimTimes`）⇒ `RangedVfxFallback` 并**出声**（每张卡一次）。</para></summary>
        public static float RetaliationHoldOf(BattleEvent retaliation)
        {
            if (retaliation == null || !retaliation.Ranged) return 0f;
            string faction = FactionOfSeat != null ? FactionOfSeat(retaliation.Player) : null;
            var times = (AnimTimesOf != null && !string.IsNullOrEmpty(retaliation.CardId))
                      ? AnimTimesOf(retaliation.CardId, faction) : null;
            // 🔴 **2026-10-23（P1 · 演出时序 ④）**：改走 `CardAnimClips.RangedChainDuration` ——
            //   它在 `VfxMap.ChainDuration` 那两档之后**多一档**「按 CardAnim 名查 `AnimInfo` 真值」
            //   （逐卡远程件大多挂在卡 SO 顶层 `customRangedAttackParticles`、不进 `AnimTimes`
            //   ⇒ 原来 441 个（卡,件）组合全落回 0.75 兜底）。契约：**≥ 0 = 真值（0 也是真值）**、
            //   **&lt; 0 = 判据为空** ⇒ 才走 `RangedVfxFallback` 并出声。
            float chain = CardAnimClips.RangedChainDuration(VfxMap.AttackRanged, faction, retaliation.CardId, times);
            if (chain >= 0f) return AttackStepRanged + chain;
            WarnRetaliationFallback(retaliation.CardId);
            return AttackStepRanged + RangedVfxFallback;
        }

        static readonly System.Collections.Generic.HashSet<string> _retaliationFallbackWarned =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>「反击这件的逐卡时长查不到 ⇒ 用 `RangedVfxFallback`」的出声（每张卡一次、有上限；
        /// ⛔ 不静默）。⚠️ 与**攻方**那一档同一条口径（它也走 `RangedVfxFallback`）。</summary>
        static void WarnRetaliationFallback(string cardId)
        {
            string key = string.IsNullOrEmpty(cardId) ? "(没卡名)" : cardId;
            if (_retaliationFallbackWarned.Count >= 64 || !_retaliationFallbackWarned.Add(key)) return;
            UnityEngine.Debug.Log($"[EventTiming] 反击 `{key}`：解开的那件**没有** `AnimInfo` 时长（`ChainDuration` = 0）"
                    + $" ⇒ 这一拍按 `RangedVfxFallback` {RangedVfxFallback:F2}s 排（与攻方远程那一档同一条兜底）");
        }

        /// <summary>🆕 **2026-10-25（`AU1`）：`EvtKind.Return`（回手 / 回牌库 / 墓地取回）这一拍占多久**。
        ///
        /// <para>**判据（第一权威；`J1` 2026-10-25 现查、订正了本方法第一版的回牌库值）** ——
        /// 原版这条**不是**瞬时的：</para>
        /// <list type="number">
        /// <item>`BattleManager._ResolveRecallToHand_d__542__MoveNext.c:273-278` 对在场单位
        ///   `StartCoroutine(CardScript.TransformUnitInPlayToCard)` 并 **`yield return`**；
        ///   那条协程（`CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:80-85`）在 state 0 末尾
        ///   `WaitForSeconds(GetHandToBoardAnimEventTime())` = **0.55 s**（= `Card Hand To Board` 那条 clip
        ///   **唯一一条 AnimationEvent** 的时刻：`AnimationClip_-3614292623624764332.json` 的
        ///   `m_Events[0].time = 0.55` / `CardHandToBoardAnimationFinished`）；
        ///   ⚠️ 同一 state 里那条 `DOMove(…, 0.208)` 是**并行**的，不额外加时间。</item>
        /// <item>收尾按目的地分两支：
        ///   · **回手** ⇒ `:55-60` `yield PlayerHand.AddCardNotDrawnToHand`，那条
        ///     （`PlayerHand._AddCardNotDrawnToHandAtIndex_d__40__MoveNext.c:42-47`）末尾
        ///     `WaitForSeconds(VarsGlobal.timeToPositionCard)` = **0.2 s** ⇒ **0.75 s**；
        ///   · **回牌库 / 牌库顶** ⇒ `_ResolveRecallToDeck_d__543:350-356` 的 `WaitForSeconds(MoveToDeck(card,1,0))`，
        ///     而 `CardScript__MoveToDeck.c` 的**返回值 = `timeToScaleIntoMoving 0.1 + 0.3 = 0.4`**
        ///     （那个 0.3 是移动/翻转那一段 `DAT_1834b2dc8`、0.1 是尾段缩放 `VarsGlobal+0x78`；**两段相加**）
        ///     ⇒ **0.95 s**。🔴 第一版记的 **0.85 是错的**（只算了 0.3）。
        ///     ⚠️ 两个例外把 0.3 归零 ⇒ `MoveToDeck` 只返 0.1 ⇒ **0.65 s**：`BattleCardUI.ShouldShowCardback(card)`
        ///     或 `card + 0x54 != 0`（**= `EntityScript.armedTrap`**，`J3` §1 已定名；那个 0.3 段整段跳过）—— **这两档没接**（见下）。</item>
        /// </list>
        ///
        /// <para>⚠️ **一个值 ≠ 全部情况（铁律 5·c）**：</para>
        /// <list type="bullet">
        /// <item>**带 `teleport`（`HasDefaultTrait(0x4ce)` = 1230）的卡**：state 1 在 `SetStateInHand`
        ///   之后**再等一次**同一个 0.55（`_d__256__MoveNext.c:108-115`）⇒ 回手 **1.30** / 回牌库 **1.50**。
        ///   判据见 <see cref="HasTraitOf"/>。</item>
        /// <item>**回手 vs 回牌库**：由 <see cref="ReturnDestOf"/> 回答（驱动按 `InstanceId` 反查那份实例
        ///   现在落在 `Hand` 还是 `Deck`）。**判不出** ⇒ 按**回手 0.75** 排并出声一次。</item>
        /// <item>**卡背朝上那一档（0.65）—— 判据已闭合（`J3` §1），但今天【不可达】、故意不接**：
        ///   `CardScript__MoveToDeck.c:45-49` 的门是**并列**两条 —— `BattleCardUI.ShouldShowCardback(cardState)`
        ///   **或** `card + 0x54 != 0`；后者 = **`EntityScript.armedTrap`**（`dump.cs:21983`；唯一置 1 点 =
        ///   `CardScript__SetAsActiveTrap.c` ⇒ 「这张陷阱卡已布置、待触发」）。
        ///   `ShouldShowCardback` 真值表（`J3` §1·④）：`inDeck(0)`/`inDeckDrawing(16)` ⇒ **1**；
        ///   `inHand(1)` ⇒ **只有敌方手牌**；`inHandJustCreated(12)` ⇒ 我方看 `armedTrap`、敌方恒 1；**其余 11 档 ⇒ 0**。
        ///   ⇒ 从**场上**回牌库（`cardState = inPlay(2)`）⇒ 那 0.3 段**照走** = 我们今天的 0.95（**对**）；
        ///   只有「卡**没离过牌库**就回牌库（`_AddCardNotDrawnToDeck`，`state = inDeck(0)`）」与
        ///   「`armedTrap == 1`」这两条路才是 0.65 —— **这两条路我们的引擎都没有**
        ///   （我们只从场上发 `Return` 且有格号）⇒ ⛔ 不凭「回牌库就该 0.65」去改。</item>
        /// </list>
        ///
        /// <para>⚠️ **`Slot &lt; 0`（不在棋盘上）两档**（`J1` §4 已闭合）：
        /// · **墓地 → 手牌 / 牌库顶 / 牌库**（`EffectResolver` 的 `ResolveCopyUnitInCemetery` 那一族）：
        ///   原版 `CardScript.CreateCardInGame(card, …, VarsGlobal.timeToDrawPlayerCard = 0.3)` 并 `yield`
        ///   （`BattleManager._ResolveCopyUnitInCemetery_d__541:125` + `CardScript._CreateCardInGame_d__250:78-84`），
        ///   **整条协程只有这一个等待** ⇒ **0.3 s**（= `CardFeel.DealDuration`，同一格字段）。
        /// · **`ephemeral`（临时卡移出游戏）**：**判据为空**（全量反编译里战斗侧只有 `BattleCardUI.ShowEphemeral`
        ///   —— 在卡位置 `Instantiate` 一颗粒子、**不等待、不协程**；没有「回合末移出」的原版实现可对）
        ///   ⇒ **保持 0**（铁律 11 例外①）。判别式 = `BattleEvent.Effect == "ephemeral"`（`RuleCore` 那条发出点填的）。</para>
        /// <para>⛔ 判据只此一处：`BattleDriver.PlayReturnFeel` 播的那条表现补间（0.9167 s）**不是**这个数 ——
        /// 原版队列只等上面那几段，clip 剩下的时间与下一批动作**重叠**。</para></summary>
        public static float ReturnHoldOf(BattleEvent e)
        {
            if (e == null) return 0f;

            // ---- 不在棋盘上：墓地取回 0.3 / ephemeral 0 ----
            if (e.Slot < 0)
                return e.Effect == "ephemeral" ? 0f : ReturnGraveHold;

            float s = ReturnCardAnimHold;
            int dest = ReturnDestOf != null ? ReturnDestOf(e) : -1;
            bool deck = dest == 1;
            if (dest < 0) WarnNoReturnDest();
            s += deck ? MoveToDeckTotal : ReturnHandRelayoutHold;

            if (HasTraitOf == null) WarnNoTraitHook();
            else if (HasTraitOf(e.CardId, KeywordTable.Teleport)) s += ReturnCardAnimHold;
            return s;
        }

        static bool _warnedNoReturnDest;
        static void WarnNoReturnDest()
        {
            if (_warnedNoReturnDest) return;
            _warnedNoReturnDest = true;
            UnityEngine.Debug.LogWarning(
                "[EventTiming] `ReturnDestOf` 没挂 / 判不出这一条的**目的地** ⇒ `EvtKind.Return` 按**回手**那一档 "
              + $"{ReturnCardAnimHold + ReturnHandRelayoutHold:F2}s 排（回牌库应 {ReturnCardAnimHold + MoveToDeckTotal:F2}s，"
              + $"差 {MoveToDeckTotal - ReturnHandRelayoutHold:F2}s）。生产路径由 `BattleDriver.HookEventTimingCardSource` 挂钩。");
        }

        static bool _warnedNoTraitHook;
        static void WarnNoTraitHook()
        {
            if (_warnedNoTraitHook) return;
            _warnedNoTraitHook = true;
            UnityEngine.Debug.LogWarning(
                "[EventTiming] `HasTraitOf` 没挂（没有驱动）⇒ `EvtKind.Return` 一律按**非 teleport** 那一档 排；"
              + $"带 teleport 的卡那一档（+{ReturnCardAnimHold:F2}s）在这条路径上取不到。"
              + "生产路径由 `BattleDriver.HookEventTimingCardSource` 挂钩。");
        }

        /// <summary>🆕 2026-10-25（`AU1`）**回手时「倒放 `Card Hand To Board`」那一段的等待** = **0.55 s**
        /// = `CardScript.GetHandToBoardAnimEventTime()`（`CardScript__GetHandToBoardAnimEventTime.c` 读的是
        /// `AnimationClip.get_events()[0].time`）＝ 那张 clip 的**唯一一条 AnimationEvent**
        /// `CardHandToBoardAnimationFinished`（clip 真件
        /// `bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3614292623624764332.json`
        /// 的 `m_Events` 只有这一条、`time = 0.55`）。
        /// 🔑 **引用 `CardFeel.HandToBoardDone`**（那条常量的 doc 就是「这个事件时刻」，⛔ 别另写一个 0.55 —
        /// 铁律 6：两处写同一条规则迟早不一致）。消费点 = `ReturnHoldOf`。</summary>
        public const float ReturnCardAnimHold = CardFeel.HandToBoardDone;

        /// <summary>🆕 2026-10-25（`AU1`）**回手之后「手牌重新摆位」那一段** = `VarsGlobal.timeToPositionCard`
        /// **0.2 s**（`PlayerHand._AddCardNotDrawnToHandAtIndex_d__40__MoveNext.c:42-47` 读 `globalVars + 0xa4`；
        /// 与 `CardTween.RelayoutDuration` 同一个数、同一个出处 —— 这里**引用那个常量**，⛔ 别另写 0.2）。</summary>
        public const float ReturnHandRelayoutHold = CardTween.RelayoutDuration;

        /// <summary>🆕 2026-10-25（`AU1` · `J1` §3 订正）**回牌库那一支的收尾** = `CardScript.MoveToDeck(card,1,0)` 的**返回值**
        /// = **`timeToScaleIntoMoving 0.1` ＋ `0.3`** = **0.4 s**（`CardScript__MoveToDeck.c`：
        /// `fVar14 = VarsGlobal+0x78`（= `timeToScaleIntoMoving` 0.1，尾段缩放）＋ `fVar19 = DAT_1834b2dc8` 0.3
        /// （移动/翻转那一段）；**两段相加**，尾段整体 `SetDelay(0.3)` 再跑 0.1）。
        /// 🔑 0.1 直接引用 `CardTween.PickUpDuration`（同一条 `VarsGlobal.timeToScaleIntoMoving`，⛔ 别另写）；
        /// 0.3 是那个通用字面量 `DAT_1834b2dc8`（与 `DeployLandHold` / `timeToLand` 同址不同用途 ⇒ 这里自带 doc）。
        /// 消费点 = <see cref="ReturnHoldOf"/>（⛔ 别在别处再写 0.4）。</summary>
        public const float MoveToDeckTotal = CardTween.PickUpDuration + 0.3f;

        /// <summary>🆕 2026-10-25（`AU1` · `J1` §4）：**墓地 → 手牌/牌库**那一拍 = `VarsGlobal.timeToDrawPlayerCard`
        /// **0.3 s**（`CardScript.CreateCardInGame(card, …, vars+0xa8)` 整条协程只有这一个等待；
        /// 判据 = `BattleManager._ResolveCopyUnitInCemetery_d__541:125` + `CardScript._CreateCardInGame_d__250:78-84`）。
        /// 🔑 引用 `CardFeel.DealDuration`（同一个字段，⛔ 别另写 0.3）。消费点 = <see cref="ReturnHoldOf"/> 的 `Slot<0` 支。
        /// ⚠️ `ephemeral`（临时卡移出）**判据为空**（原版没有这条链）⇒ 仍是 0。</summary>
        public const float ReturnGraveHold = CardFeel.DealDuration;

        /// <summary>🆕 2026-10-25（`AU1` · `J1` §5）：**易伤伤害那一拍多出来的前置等待** = **0.4 s**
        /// （`CardScript._ReceiveDamage_d__381__MoveNext.c:291-303`，`DAT_1834b3158` 实读 0.4 —— 只对
        /// `GameStaticData.IsVulnerableDamage(t)`（`(t−7) &lt; 2`，即 `DamageType.Vulnerable 7 / VulnerableCombat 8`）那两档）。
        /// 🔑 那 0.4 **不会跳过**后面那个 1.0（`state 2` 之后照旧走到 `:340-346`）⇒ 一拍 **1.4 s**。
        /// ⚠️ `doItFast` 那个「快进」例外**本 build 不可达**（全库 6 个调用点第 3 个实参全是 0）⇒ ⛔ 不实现。
        /// 数值只此一处：消费点 <see cref="HitHoldOf"/>。</summary>
        public const float HitVulnerablePreWait = 0.4f;

        /// <summary>🆕 2026-10-25（`AU1` · `J1` §1）：**手牌打出的普通单位：`play` 桶起播相对 `Deploy` 这一刻的偏移** = **1.30 s**。
        ///
        /// <para>判据（`BattleManager._ResolvePlayCardFromHand_d__447` 的 state 3→4→ 0xb；广播点 `:1426`
        /// `BattleManagerSupport.BroadcastCardPlayed`，全库**唯一**调用点）：
        /// `PlayMinionFromHand` 飞行 0.3 之后 `WaitForSeconds(GetMinionConversionTime)`
        /// = `GetHandToBoardAnimEventTime()`(**0.55**) + `minionToConversionPointTime`(**0.3**) = **0.85**
        /// （`MinionManager__GetMinionConversionTime.c:5-12`）＋ `yield MinionPlayedIntoField` = `delay(0.1)`
        /// + `timeBeforeLand(0.05)` + `timeToLand(0.2)` + `attackStepTime(0.1)` = **0.45**
        /// ⇒ **1.30**。⚠️ 改前我们按 0.4（`DeployHoldOf` 那三段）排 ⇒ `play` 桶早 0.9 s。</para>
        ///
        /// <para>⚠️ **别的档**（`J1` §1 的表；⛔ 别一刀切）：`teleport` 的卡 ≈ **1.25**（**这一格待核**，
        /// 我们按 1.30 排）；**战术 / 法术 = 0**；**自动打出 / 效果召唤 ⇒ 原版根本不广播 `CardPlayed`**
        /// （所以这两个来源的卡**不该**播 `play` 桶；我们引擎也只在 `RuleCore.PlayCard` / `PlayTactic` 发 `Play`）。
        /// 战术卡 `targeted &amp;&amp; fromEnemyHand` 还有 0.5 的居中展示前置（`vars.targettingAnimTime`）—— 未接。</para></summary>
        public const float PlayBucketHandPlayedHold = 1.30f;

        /// <summary>🆕 **2026-10-25（`AU1` · `J3` §4）：敌方从手牌打出 `targeted` 的**战术 / 低语**时，
        /// `play` 桶前面的那 0.5 s = **准星定位 `EnemyTargetingAnim`**（不是「居中展示」——`J1` 那个**值**对、名错）。
        ///
        /// <para>判据：`BattleManager__GetTargettingTime.c` = `VarsGlobal.targettingAnimTime`（`+0x88`）= **0.5**
        /// （`资料/VarsGlobal_原版数值.md:47`）；消费点 `BattleManager._ResolvePlayCardFromHand_d__447:477-491`：
        /// 取牌 + `PayCardCost` 之后 → `EnemyTargetingAnim(该方督军, 目标, 3)` + `WaitForSeconds(GetTargettingTime())`
        /// → `state 0xC` → `TargetedSpellPlayed` ⇒ **法术生效之前**。
        /// 门 = `cardType ∈ {Tactic(20), Whispers(40)}` ∧ `rawCardData.targeted(+0x268)` ∧ `BattleAction.fromEnemyHand(+0x3C)`。</para>
        /// <para>⚠️ 真时演那一半**早就有了**（`BattleDriver` 的 `StartAiTargetingAnim`/`TickAiTargetingAnim`，
        /// 时长 = <see cref="CardFeel.EnemyTargetingAnimTime"/>、OutQuad）⇒ 缺的只是**时间线**上的这个偏移。</para>
        /// <para>🔑 引用 `CardFeel.EnemyTargetingAnimTime`（同一个 `VarsGlobal` 字段，⛔ 别另写 0.5）。</para></summary>
        public const float EnemyTargetingHold = CardFeel.EnemyTargetingAnimTime;

        /// <summary>🆕 2026-10-25（`AU1` · `J3` §4）：`ReplaySkipAnims()` 为真时上面那 0.5 要**除以 3**
        /// （`BattleManager__GetTargettingTime.c` 的 `÷ DAT_1834b2e8c`，实读 **3.0**）⇒ **0.1667**。
        /// 我们的等价门 = `BattleDriver._replaySession`（回放局）。消费点 → `PlayBucketOffsetOf`。</summary>
        public const float ReplaySkipAnimDivisor = 3f;

        /// <summary>🆕 **2026-10-25（`AU1` · `J1` §5 + `J3` §3 + `E4`）：这一条 `Hit` 占多久** —— 普通 **1.0**、易伤第二拍 **1.4**。
        /// <para>普通档 = <see cref="HitWaitSeconds"/>（`_ReceiveDamage:342` 的 `WaitForSeconds(1.0)`；
        /// `_ResolveAttack:1152` 以 0.2 一拍轮询 ⇒ 放行 ≈1.0）。</para>
        /// <para>易伤档 = **+<see cref="HitVulnerablePreWait"/>**（`:291-303` 的 0.4，**不跳过**那个 1.0）。
        /// 🔴 **2026-10-25（`AU1` · 第七批 · `E4`）就地改口径（铁律 5）**：原来这里写「我们的引擎把易伤折进同一条
        /// `Hit`，我们只有一条 ⇒ 取 1.4 仍比原版短 1.0」—— **已过期**：引擎已把它拆成**两条串行**的 `EvtKind.Hit`
        /// （主 = `actual − vulnPart`、第二拍 = `vulnPart` 且带 **`BattleEvent.IsVulnerableSecondHit`**）
        /// ⇒ **主拍 1.0 + 第二拍 1.4 = 2.4 s**，与原版 1.0+1.4 **逐段同形**；
        /// <see cref="HitVulnerablePreWait"/> 那 0.4 就是**第二拍自己头上的**等待（原版那一次 `_ReceiveDamage`
        /// 里先等 0.4 再等 1.0），**不是额外前置**（两拍是串行入队，中间没有别的等待）。</para>
        /// <para>⚠️ 判别式只读那一格：`EventTiming.HitIsVulnerable = e => e.IsVulnerableSecondHit`
        /// （⛔ 别拿「受击方现在带 `vulnerable`」当替身 —— 那会让**两拍都**排 1.4）。</para></summary>
        public static float HitHoldOf(BattleEvent e)
        {
            if (e == null) return HitWaitSeconds;
            return (HitIsVulnerable != null && HitIsVulnerable(e))
                 ? HitWaitSeconds + HitVulnerablePreWait
                 : HitWaitSeconds;
        }

        /// <summary>🆕 **2026-10-25（`AU1` · `J1` §6）：这一条「发动能力」占多久 —— 先按【分支】分，再按卡查表**。
        /// <list type="number">
        /// <item>替代支（`CardDef.UsesAlternativeAbility` 为真，现读全池只有 `SW21 Wulfen Pack Leader`）⇒
        ///   `AbilityAnimDurations.TryGetAlternative`（`alternative.seconds`，SW21 = **0.2**）；</item>
        /// <item>普通支 / 没有替代表 ⇒ `AbilityAnimDurations.TryGet`（`active`，SW21 = **0.4**）；</item>
        /// <item>查不到 ⇒ <see cref="AbilityFallbackSeconds"/>（**不是原版值**，`IsSourced` 如实回 false）。</item>
        /// </list>
        /// <para>✅ **2026-10-25（`TL` §2）**：`alt` 那一列已进运行期表（现读只有 `SW21 = 0.2`）⇒
        /// 替代支**现在真的按 0.2 排**；没替代支的卡仍走普通支。</para></summary>
        public static float AbilityHoldOf(BattleEvent e)
        {
            if (e == null) return AbilityFallbackSeconds;
            bool alt = AbilityUsesAlternative != null && AbilityUsesAlternative(e);
            float s;
            if (alt && AbilityAnimDurations.TryGetAlternative(e.CardId, out s)) return s;
            if (AbilityAnimDurations.TryGet(e.CardId, out s)) return s;
            return AbilityFallbackSeconds;
        }

        /// <summary>
        /// 上一条事件播完到这一条开始，隔多久（秒）。
        /// `prev == null` = 这一串里的第一条。
        /// <para>⚠️ **这一条重载拿不到「哪张卡 / 哪个阵营」** ⇒ 远程那一档只能用兜底时长
        /// （<see cref="RangedVfxFallback"/>）。**生产路径必须走三参那个重载**
        /// （`BattleDriver.PlaySignals` 从 `VfxMap.ChainDuration` 现算）—— 同
        /// <see cref="DurationOf(EvtKind)"/> / <see cref="DurationOf(BattleEvent)"/> 那一对的分工。</para>
        /// </summary>
        public static float DelayBetween(BattleEvent prev, BattleEvent next)
        {
            return DelayBetween(prev, next, -1f);
        }

        /// <summary>带「远程那条 VFX 自己的时长」的重载（🔴 **2026-10-21（W61 第 1 批 · 第 2 条）**）。
        /// <paramref name="rangedVfxDuration"/> = 该次远程攻击 VFX 的 `AnimInfo.GetAnimDuration()`
        /// （`VfxMap.ChainDuration` 现算）；**&lt; 0 = 没传** ⇒ 用兜底 <see cref="RangedVfxFallback"/>。</summary>
        public static float DelayBetween(BattleEvent prev, BattleEvent next, float rangedVfxDuration)
        {
            if (next == null) return 0f;

            // 一串里的第一条：出手要先**抬刀**
            if (prev == null) return next.Kind == EvtKind.Attack ? AttackWindUp : 0f;

            switch (next.Kind)
            {
                // 🔴 **2026-10-21（W61 第 1 批 · 第 2 条）：这条 `Hit` 已经是【伤害结算】那一拍。**
                //   原版 `ReceiveDamage`（改血 / 伤害计数器）在**整条攻击动画跑完之后**才进：
                //     · 近战 0.4 s（`_AttackMeleeAnim`：位移 0.1 + 转向 0.1 + 停 0.1 + 归位 0.2）⇒ Attack+0.4；
                //     · 远程 0.2 s（`_ResolveAttackRangedAnim`：回正 0.1 + 等 0.1）+ 该 VFX 的
                //       `AnimInfo.GetAnimDuration()`（`_ResolveAnimInfoRangeAttack…` state 3 的
                //       `WaitForSeconds`，`:111-124`）⇒ Attack+0.2+VFX。
                //   而「出手到命中帧」那一段（近战 0.1 / 远程 0.2）已经算在 `DurationOf(Attack)` 里
                //   ⇒ 这里返回的是**剩余**那一截（近战 0.3 / 远程 = VFX 时长）。
                //   👉 命中帧的**手感**（后坐/震屏/命中粒子/命中音）不在这一拍 —— 见 <see cref="HitFrameLag"/>。
                case EvtKind.Hit:
                    if (prev.Kind != EvtKind.Attack) return 0f;
                    if (!prev.Ranged) return MeleeDamageLag - AttackStepMelee;
                    return rangedVfxDuration >= 0f ? rangedVfxDuration : RangedVfxFallback;

                // 阵亡：链路查到了（`_DestroyUnitsAfterBattleEnd_d__392__MoveNext.c` 里
                // `UnitDeath` 之后 `WaitForSeconds(VarsGlobal.deathTimeMinionDuration + …)`）。
                // 🔴 **2026-09-18 更正**：原来这里写「`VarsGlobal` 资产不在解包数据里 ⇒ 秒数查不到」——
                //    那句话**已经过期了**：`VarsGlobal` 2026-09-17 整表解出来了
                //    （`资料/VarsGlobal_原版数值.md`：小兵 `deathTimeMinionDuration = 0.2` /
                //     督军 `deathTimeWarlordDuration = 0.5`）。
                //    ⇒ **动画那一段**照原版 0.2（见下面 `DurationOf`），**后面这个「等一拍」仍是我们挑的**
                //    （原版那个 `WaitForSeconds` 等的正是同一个字段，我们这里已经把它算进动画时长了，
                //     再加同样的数会**重复计一次**）。
                // 🔴 **2026-10-23（P1 · 演出时序 ②）**：`DurationOf(Death)` 已从 0.2 改成 **0.7**
                //    （= `CardFeel.DeathTotal`，整条阵亡协程的寿命）⇒ 「两条 `Death` 的间隔」现在是
                //    `0.7 + DeathHold 0.1 = 0.8 s` ≥ 每条演出自己的 0.7 s ⇒ **两条阵亡不再重叠**（原版就是串行）。
                //    ⚠️ `DeathHold` 本身**不动** —— 它是「Hit 那一拍收尾之后到阵亡开场」的间隔，另一件事。
                case EvtKind.Death: return DeathHold;

                // 触发：用 `BattleManager` 里那两个命名延迟常量（见 `TriggerHold` 的注释）
                case EvtKind.Trigger: return TriggerHold;

                // 🔴 **2026-10-14（收口自检 β 回归）：抽卡那一条【从本函数搬去 `DurationOf`】**。
                //   原来 `case EvtKind.Draw: return DrawDeal;` 写在这里（= 「上一条到这一条的**间隔**」），
                //   而三处自述（`DrawDeal` 的 doc · `SourceOf(Draw)` · `A1816` 的断言）说的都是
                //   「**占多久**」；原版也是「入队之后才 `WaitForSeconds`」（`_AddDrawnCardToHand:78-80`）
                //   ⇒ 落到 `DurationOf` 才对。⚠️ **⛔ 别两处都留**（那会多排 0.3 s）。

                // 🆕 2026-10-14（`A1816`）**伏击翻面**占 **0** —— 🔴 **这不是「没查到时长的填死值」**，
                //   是查过之后**故意**取 0：原版那一下是 `CardScript.SetAmbush(card, false)`
                //   （**普通方法、不阻塞**，`CardScript__SetAmbush.c` 逐行读过：它换材质/开数值/
                //    记 `AddExitAmbushActionToCemetery` 之后直接返回），而
                //   `RuleCore.ApplyDamage` 里它**紧跟着同一击的 `Hit`**（`:5651` → `:5655`）。
                //   ⇒ 本表若给它一段时长，就会把**同一下伤害的挨打反馈**整段推后（1.35 s 那种量级），
                //     那是实打实的**新偏离**。那件特效自己多长是**它自己的事**（见 `SourceOf`），
                //     `WarpforgeEffectPlayer` 会照 prefab 的 `destroyTime` 播完 —— **不进时间线**。
                case EvtKind.AmbushExit: return 0f;

                // 🆕 2026-10-22（`W64`）：**反击与上一条之间不加额外间隔** —— 原版那一跳是
                //   `if (0 < GetCombatDamage) { … }` 直接接上（`_ResolveAttack…:1114-1152`），
                //   没有 `WaitForSeconds`；这一拍**自己演多久**由 `DurationOf` 负责（⛔ 别两处都排）。
                case EvtKind.Retaliation: return 0f;

                default: return 0f;
            }
        }

        // ==================================================================
        //  🆕 2026-10-24（执行代理 `A2` · `J2` 顺手发现 3）：**`EvtKind` 的登记表**
        //
        //  问题：`IsSourced` / `SourceOf` / `DurationOf(EvtKind)` 三处原来都是 `default:` 一把兜住
        //  ⇒ **往 `EvtKind` 里加一个新值时**，它会被自动当成「有出处」（`IsSourced` = true）
        //  且时长 `0f` —— 而 `BattleScene.cs` 那条 `unsourced == 2` 的断言**不会**因此报警
        //  （它只数 `IsSourced` 为假的那几个）。整条链路**静默**。
        //
        //  ⇒ 现在三处的 `default:` 都先查这张登记表：
        //    · **登记为「不占时间线（0 s）」** ⇒ 照旧（行为一个字不变）；
        //    · **没登记** ⇒ **出声（每个 kind 只喊一次）+ 计数**，并且 `IsSourced` 回 `false`
        //      （带原因）⇒ `BattleScene` 的 `unsourced == 2` 会变红，**逼着**加值的人来登记。
        //
        //  ⚠️ **新增一个 `EvtKind` 时要做的事**：在下面两张表里挑一张登记（有值 → 去
        //     `DurationOf` **和 `SourceOf`** 的 `switch` 里各加一个 `case`；零时长 → 加进 `ZeroDurationKinds`）。
        //     🔴 **只登记不加 `case` 也算病**（2026-10-24 `E2`）：`DurationOf` / `SourceOf` 会走
        //     `ReportMissingCase` 出声，`EngineClosedDeckAssert` §B 那条全枚举断言会红。
        //  ⚠️ `ShowCardsWindow` **显式登记成零时长**（原版那一拍**非阻塞**：`FinishResolvingAction`
        //     紧跟其后、无 `yield`，见 `BattleDriver.FireEventAll` 里 `OpenShowCardsWindow` 那段判据）
        //     —— ⛔ 别让它掉进「未登记」，那会把一条**本来正确**的 0 s 变成噪音。
        // ==================================================================

        /// <summary>🆕 2026-10-24（`A2`）：**明确登记为「不占时间线（`DurationOf` = 0）」的事件种类**。
        /// 每一档都写清为什么是 0 —— 新加一档照这个格式补，⛔ 别默默依赖 `default: 0`。</summary>
        static readonly EvtKind[] ZeroDurationKinds =
        {
            EvtKind.Play,           // 出牌本身不占（登场位移由紧随的 `Deploy` 负责，见 `DurationOf(Deploy)`）
            // 🔴 **2026-10-25（`AU1`）`EvtKind.Return` 已从这张表搬走** —— 原版回手/洗回牌库
            //   **会 `yield` 两段**（0.55 + 0.2 = 0.75，判据 → `ReturnHoldOf`）⇒ 它现在是「有时间」那一档，
            //   ⛔ 别再登记回这里（那会让它重新变成 0 s）。
            EvtKind.GainFaith,      // 纯资源变化（HUD 计数），不起播任何占时动作
            EvtKind.GainSpirit,
            EvtKind.GainQuest,
            EvtKind.CollectWaystone,// 收集粒子是**并行**的一发（原版 `CollectWaystoneEffect` 不 `yield`）
            EvtKind.AmbushExit,     // 原版 `SetAmbush(card,false)` 是普通方法、不阻塞（见 `SourceOf`）
            EvtKind.SecretOrder,    // 日志/揭密那一拍；我们没有它的演出（`RuleCore` 无发出点）
            EvtKind.ShowCardsWindow,// 🆕 原版**非阻塞**（`FinishResolvingAction` 紧跟、无 `yield`）
            // 🆕 2026-10-24（引擎线 `G1b` · `C2` §6）：关键词施加那一拍**不占时间线** ——
            //   原版 `BattleCardUI__DisplayStatusAnim.c:78-119` 是当场
            //   `StartCoroutine(ResolveTraitAnim(…))`、**不 `yield`**；那件 Start(0) 由 prefab 自己的
            //   `destroyTime` 收尾（同 `CollectWaystone` 那一档的口径）。
            EvtKind.StatusApplied,
            // 🆕 2026-10-26（执行代理 `E11` · 诊断 `D1` #15/#18）：**关键词被摘掉** 这一拍**不占时间线**。
            //   判据：① 原版 `CardScript.RemoveTrait` 之后进 `Expiring State` 那一步
            //     （`MarkerlightController.MarkerLightUsed()`）是**普通方法、不 `yield`**
            //     （见 `BattleEvent.cs:374-377` 对 `判据/TraitEffectController_钩子层.md` §6.1③ 的引用）；
            //   ② 我们这两个发出点（`EffectResolver.EmitTraitRemoved` · `RuleCore` 回合末摘标记光）
            //     都在**同步结算里**，`BattleDriver` 侧今天**没有任何消费者**（纯表现信号）。
            //   ⇒ 与 `StatusApplied` 同档口径。⛔ 别改成 `TimedKinds`：那要在 `DurationOf` / `SourceOf`
            //   各补一个 `case`，而我们**没有**任何「这一拍演多久」的判据。
            EvtKind.TraitRemoved, EvtKind.EnchantmentRemoved, EvtKind.SabotageCreated,   // 🆕 2026-10-30（`B102_506` · 账 `战斗逻辑驱动:506`）：造破坏卡进手牌那一拍**不占时间线** —— 中央展示走 `BattleDriver.PlaySignal` 的 `else if (e.Kind == EvtKind.SabotageCreated && animateFeel)`（`BattleDriver.cs:23064` 调 `MaybeDisplayCreatedSabotageCardInCenter`），是 DOTween 序列、不推时间线。⚠️ **降级说明**：原版 `.c:100-107` 其实是 `yield return StartCoroutine(...)`、**会阻塞**；我们降级成「不断时间线」，与 `EvtKind.Play`（中央展示是 DOTween 序列）同一降级口径。⛔ 别改成 `TimedKinds`：那要在 `DurationOf` / `SourceOf` 各补一个 `case`，而我们没有「这一拍演多久」的独立判据。｜🆕 2026-10-27（`E7ENG` · 账 532 的 1b）：摘附魔效果那一拍**不占时间线** —— 原版 `ResolveRemoveEffect`（内联进 `ResolveAction` case 0x10）是同步方法、不 `yield`，与 `StatusApplied` / `TraitRemoved` 同档；成员 doc 见 `BattleEvent.cs`。⚠️ 写在同一行 = **刻意零行数变化**（本文件下游有账按行号引，见铁律 18·b）
        };

        /// <summary>🆕 2026-10-24（`A2`）：**在 `DurationOf(EvtKind)` 里【有值】的事件种类**
        /// （每一项都在下面的 `switch` 里有自己的 `case`）。加值时**两处一起改**。
        /// ⚠️ `Attack` / `Deploy` / `Retaliation` / `Draw` / `Ability` / **`Return`** 的**精确值**还要看
        /// `DurationOf(BattleEvent)` 那个重载（按远近 / 来源 / 座位 / 卡 / `Slot` / teleport 分档），
        /// 这里只登记「它有时间」。</summary>
        static readonly EvtKind[] TimedKinds =
        {
            EvtKind.Deploy, EvtKind.Attack, EvtKind.Hit, EvtKind.Death, EvtKind.Ability,
            EvtKind.Trigger, EvtKind.Draw, EvtKind.Heal, EvtKind.Retaliation,
            // 🆕 2026-10-25（`AU1`）：回手/洗回牌库（原版 `yield return` 两段协程，见 `ReturnHoldOf`）
            EvtKind.Return, EvtKind.DiscardReveal, EvtKind.StealReveal,   // 🆕 2026-10-30（执行代理 `B100_F11`）：弃牌/偷牌的「揭示」那一拍**各占自己的 `stepTime`**（0.2 / 0.4，判据 = `CardFeel.DiscardRevealStepTime` / `StealRevealStepTime` 的 doc：`.rdata` 实读 + 生产调用点 `_ResolveDiscardFromHand_d__544:95` / `_ResolveStealFromDeck_d__547:243`）。改前这两档**没登记** ⇒ `DurationOf` 落 `default: 0`、`IsSourced` 回 false（`BattleScene` 的 `unsourced == 2` 变 4、`A2·J2③` 报「未登记 2 档」）。⚠️ 写在同一物理行 = **刻意零行数变化**（本文件 `:1014` 有账落点，铁律 18·b）
        };

        /// <summary>自检用：`DurationOf` 里登记成「零时长」的那一档吗（不触发未登记告警）。</summary>
        public static bool IsZeroDurationKind(EvtKind kind)
        {
            for (int i = 0; i < ZeroDurationKinds.Length; i++) if (ZeroDurationKinds[i] == kind) return true;
            return false;
        }

        /// <summary>自检用：这一档登记成「有时间」吗（在 `TimedKinds` 里）。
        /// 🔴 2026-10-24（执行代理 `E2`）新增：`IsRegisteredEventKind` 只能证明「登记表里有名字」，
        /// **无法证明 `DurationOf` / `SourceOf` 的 `switch` 里真有 `case`** —— 这两件事要分开查
        /// （登记了却没 `case` ⇒ 静默落回 `default`）。诊断与断言都用这个口。</summary>
        public static bool IsTimedKind(EvtKind kind)
        {
            for (int i = 0; i < TimedKinds.Length; i++) if (TimedKinds[i] == kind) return true;
            return false;
        }

        /// <summary>自检用：这一档**登记过**吗（`ZeroDurationKinds` 或 `TimedKinds`）。
        /// 未登记 ⇒ 三处 `default:` 会出声 + `IsSourced` 回 `false`。
        /// ⚠️ 「登记过」**不等于**「`switch` 里真有 `case`」—— 见 <see cref="IsTimedKind"/>。</summary>
        public static bool IsRegisteredEventKind(EvtKind kind)
        {
            return IsZeroDurationKind(kind) || IsTimedKind(kind);
        }

        /// <summary>自检用：累计「遇到未登记的事件种类」的次数（每个 `EvtKind` 每次会话只出声一次，
        /// 但**计数每次都加**）。⚠️ 只增不减 —— 断言「报了没有」用它。</summary>
        public static int UnregisteredKindHits { get; private set; }

        // 🔴 2026-10-24（`F2` · R1 P2-5）：**「只喊一次」的闩（`_warnedUnregistered`）与「自检计数」
        //   （`UnregisteredKindHits`）原来是同一个 `Reset` 一起清的** ⇒ 任一自检调过 `ResetUnregisteredKindForTest()`
        //   之后，**生产路径**再遇到同一个未登记 kind **会再喊一遍**（每局一次 → 变多次），
        //   日志噪音会**掩盖真正新加的那一档**（新加的那一档是「第一次出现」，老的那一档又在重喊）。
        // ⇒ 现在两件事分开：
        //    · `ResetUnregisteredKindForTest()` —— **只清计数**（自检要「从 0 开始数」时用），**不动闩**；
        //    · `ResetWarnedForTest()` —— 显式清闩，**只给「要重新观察『出声』这件事本身」的自检**用；
        //      生产路径永远不碰它 ⇒ 每条未登记 kind 在整个会话里**至多喊一次**。
        //   ⚠️ 读数保持：`BattleScene` 那组断言只用计数（`hitsBefore → hits`），所以 `unsourced == 2` 等读数一个字不变。
        static readonly System.Collections.Generic.HashSet<EvtKind> _warnedUnregistered =
            new System.Collections.Generic.HashSet<EvtKind>();

        /// <summary>自检用：**「已经喊过」的未登记 kind 有几种**（= 「只喊一次」那个闩的读数）。
        /// 🔴 `ResetUnregisteredKindForTest()` **不会**让它变小 —— 这正是 R1 P2-5 要证的那件事
        /// （清计数 ≠ 清闩）。只有 <see cref="ResetWarnedForTest"/> 会清零。</summary>
        public static int UnregisteredKindWarnedCount { get { return _warnedUnregistered.Count; } }

        /// <summary>自检用：清零**计数**（不改登记表本身，**也不清「已经喊过」的闩** —— 见上面那段）。</summary>
        public static void ResetUnregisteredKindForTest()
        {
            UnregisteredKindHits = 0;
        }

        /// <summary>自检用：清「已经喊过」的闩（**生产路径不调**；只有「要重新观察『出声』这件事本身」的
        /// 自检才调）。⚠️ 与 <see cref="ResetUnregisteredKindForTest"/> 分开，正是为了让自检**清计数**
        /// 时不要把生产的「只喊一次」清掉（R1 P2-5）。</summary>
        public static void ResetWarnedForTest()
        {
            _warnedUnregistered.Clear();
        }

        /// <summary>遇到未登记的 `EvtKind` ⇒ 计数 + 出声一次（同一种类每次会话只喊一次，别刷屏）。</summary>
        static void ReportUnregisteredKind(EvtKind kind, string where)
        {
            UnregisteredKindHits++;
            if (!_warnedUnregistered.Add(kind)) return;
            UnityEngine.Debug.LogError($"[EventTiming] `EvtKind.{kind}`（值 {(int)kind}）**没在登记表里**"
                + $"（`{where}` 走到了 `default:`）⇒ 它现在被当成「零时长 / 无出处」。"
                + "新增事件种类必须显式登记：**零时长** → `EventTiming.ZeroDurationKinds`；"
                + "**有时间** → `EventTiming.TimedKinds` + `DurationOf` 的 `switch` 加 `case`。"
                + "⛔ 别默默依赖 `default:` —— 那正是 J2 顺手发现 3 那条缺陷的形状。");
        }

        /// <summary>「登记在 `TimedKinds` 里、但 `switch` 里没有它的 `case`」⇒ 计数 + 出声一次。
        /// 🔴 **2026-10-24（执行代理 `E2`）**：这是与 <see cref="ReportUnregisteredKind"/> **不同的一种病**，
        /// 所以措辞分开 —— 原版口径是「登记了就有值」，可登记表只是**声明**，真正的值在 `switch` 里；
        /// 只有声明没有 `case` 时，`DurationOf` / `SourceOf` 会落回 `default:`，而 `IsSourced` 因为
        /// 「登记表里有名字」**照样回 `true`** ⇒ 那一处**仍然是静默的**（正是 J2 顺手发现 3 的形状）。
        /// ⚠️ 两种病共用同一个计数（`UnregisteredKindHits`）与同一个「只喊一次」的闩 ——
        /// 对「报了没有」的断言来说它们是同一件事；⛔ 别为此再加第二个计数器（两份数迟早不一致）。</summary>
        static void ReportMissingCase(EvtKind kind, string where)
        {
            UnregisteredKindHits++;
            if (!_warnedUnregistered.Add(kind)) return;
            UnityEngine.Debug.LogError($"[EventTiming] `EvtKind.{kind}`（值 {(int)kind}）**登记在 `TimedKinds` 里，"
                + $"但 `{where}` 的 `switch` 里没有它的 `case`** ⇒ 它现在静默落回 `default:`，"
                + "`IsSourced` 却仍会因为「登记表里有名字」回 `true`。"
                + "新事件必须**两处一起改**：`TimedKinds` ＋ `DurationOf` / `SourceOf` 的 `case`。");
        }

        /// <summary>这条事件「占」多久（秒）—— 下一个环节要等这么久。出处见每条后面</summary>
        public static float DurationOf(EvtKind kind)
        {
            switch (kind)
            {
                // `Summon Troop Tween`：ScaleTween d=0 + **DelayTween d=1.0** + ResetTween 0.3
                // 🆕 2026-09-29：再加**落位之后那一拍**（`DeployLandHold`）——
                //    原版 `_ResolvePlayCardFromHand` 的 state 4→5 就是 `WaitForSeconds(0.3)`
                //    （`DAT_1834b2dc8` 实读 = 0.3），发生在**落位动画跑完之后**。
                // 🔴 **2026-10-21（W61 第 2 批 · 第 3 条）：这一格【按来源分档】了** ——
                //    手牌打出 = **0.4**（`_MinionPlayedIntoField` state 3 尾的 `attackStepTime` 0.1 +
                //    `_ResolvePlayCardFromHand` state 4 的 0.3）；`Summon Troop Tween` 的 1.0 只给
                //    **效果召唤/复活**。分档要 `BattleEvent.DeployFrom` ⇒ 见 `DurationOf(BattleEvent)`。
                //    ⛔ **这条只按 EvtKind 的重载拿不到来源** ⇒ 返回**兜底**（老口径，给探针/旧路径用）。
                case EvtKind.Deploy: return DeploySummonHold + DeployLandHold;

                // 出手 → 命中：**近战档 0.10**（远程 0.20，走 `DurationOf(BattleEvent)` 那个重载）。
                // 🔴 **2026-09-18 更正**：这里原来是 `ChargeTime + AttackPunchDuration = 0.65`，
                //    两处都错 —— ① `timeToChargeAttack`(0.35) 在攻击时序里**没有消费点**
                //    （只在 `CardScript__OrientToTargetingDirection.c:62` 当朝向插值）；
                //    ② 出手那一下的真判据是 `attackStepTime`(0.1)，**位移完成即命中帧**。
                //    规格·出处见 `资料/普查产出_0918/第18行_UI三小条_规格.md` §③。
                case EvtKind.Attack: return AttackStepMelee;

                // 🔴 **2026-10-24（`W16`）判据已闭合 ⇒ 就地改值（铁律 5）：0.75 → 1.0。**
                //   0.75 的旧出处 = `Impact Light Tween` 的 Punch 0.5 + ResetBody 0.25 —— 已被
                //   `CardFeel` 的 `B02` 证伪（那两条 punch 是**远程弹着** AnimFX 模块，不是近战挨打）。
                //   真判据 = 原版攻击序列上的一道**门**：`_ReceiveDamage` 收尾的 `WaitForSeconds(1.0)`
                //   （普通攻击命中 `doItFast = false`，`_ResolveAttack_d__438__MoveNext.c:999`）＋
                //   `_ResolveAttack` 以 0.2 s 一拍轮询 `wholeDamageAnimFlag` 等它清零 ⇒ 放行 ≈ 1.0 s。
                //   🔑 **数值只此一处** = <see cref="HitWaitSeconds"/>（出处、两个例外都写在它的 doc 里，
                //   ⛔ 别在这儿另写 `1.0f`）。判据全文 → `资料/普查产出_1024d/S2_受伤时长bool语义.md`。
                case EvtKind.Hit: return HitWaitSeconds;

                // **拍的**：原版没有通用阵亡 tween（只有 `EC Heldrake Dissapear UP`、
                // `Tyranid_Burrow_Tween` 两个单体专用），而 `VarsGlobal` 又缺 → 0.4 是估的
                case EvtKind.Death:
                    // 🔴🔴 **2026-10-23（P1 · 演出时序 ②）就地改口径（铁律 5）：原来返回 0.2。**
                    //   **错在把「消散那一小段」当成了「这条演出占多久」** —— 原版
                    //   `BattleManager._ResolveMinionDeath_d__454__MoveNext.c:113-121` 对 `UnitDeath`
                    //   是 **`StartCoroutine` + `yield return`**（`+0x10 = 1` 之后返回 1）⇒ 动作队列
                    //   **真等整条阵亡协程跑完**，而那条协程的总寿命 = `CardFeel.DeathTotal` = **0.7 s**
                    //   （抖 `DOShakePosition` **不等它** → 同帧起算的 `WaitForSeconds(gVars+0x34)` 0.5
                    //    → 0.2 后 `GoToCemetery`；判据 → `CardFeel.DeathBeats` 的 doc）。
                    //   ⇒ 原来 0.2 让「连续两条 `Death`」只隔 `0.2 + DeathHold 0.1 = 0.3 s`：
                    //     **两条 0.7 s 的演出重叠 0.4 s**、整批也**早 0.5 s 放行**
                    //     （闸 = `BeginBlockingWindow`）。现在 0.7 ⇒ 两条阵亡**串行**（原版就是串行）。
                    //   🔑 **判据只此一处** = `CardFeel.DeathTotal`（⛔ 别在这儿另写 0.7）。
                    //   ⚠️ 参数（督军与否）现在**不影响总寿命**（两档都是 0.7，只影响抖动长度）
                    //     ⇒ 传 `false` 即可；`DurationOf` 这条重载本来就拿不到它。
                    return CardFeel.DeathTotal(false);

                // 技能：**逐卡真值**（🆕 2026-10-14 `A1767`）——
                //   `DurationOf(BattleEvent)` 那个重载按事件里的卡（`CardId`）去
                //   `AbilityAnimDurations` 查这张卡的 `activeAbility` 时长；
                //   **这个只按 `EvtKind` 的重载拿不到卡** ⇒ 一律返回兜底值
                //   （= 真值表的中位数 `AbilityFallbackSeconds`，见那个常量的注释）。
                //   ⛔ **别把逐卡那一半写回这里** —— 它需要事件（`BattleEvent.CardId`），
                //      而这条重载的签名里没有；两条路都要能跑到才是接完。
                case EvtKind.Ability: return AbilityFallbackSeconds;

                // 触发：见 `TriggerLen`（有出处，但那两个常量是引擎的**通用节拍**，不是 Trigger 专用）
                case EvtKind.Trigger: return TriggerLen;

                // 🆕 2026-10-14（`A1816` · 收口时从 `DelayBetween` **搬到这里**）**抽卡**占多久 =
                //   发牌入场那条补间自己的时长（`CardFeel.DealDuration` = `VarsGlobal.timeToDrawPlayerCard` = 0.3）。
                //   ⚠️ 原版**是分敌我的一对**（我方 0.3 / 敌方 0.15），而本表**拿不到「哪一方是我」**
                //      ⇒ 这里只覆盖**我方档**（见 `DrawDeal` 的注释）。敌方**动画**那一侧是对的
                //      （`CardFeel.DealInQuad` 走 `DealDurationFoe = 0.15`），只有这条时间线会多排 0.15 s。
                case EvtKind.Draw: return DrawDeal;

                // 🆕 2026-10-21（战斗线四笔静态项 · 笔 1）**治疗**占多久 = 卡上 `HealText` 的**生命周期**
                //   （显示 → `SetActive(false)`，1.5 s）。原版 `CardScript.Heal` 是**协程**，
                //   `ResolveHeal` 对每个目标 `yield return` 等它放完
                //   （`BattleManager._ResolveHeal_d__491__MoveNext.c:114-147`）⇒ 多目标治疗**严格串行**、
                //   每目标 1.5 s。
                //   🔑 **判据只此一处** = `CardView.HealTextHoldSeconds`（⛔ 别在这里另写 1.5；
                //      这里引的是**那个常量本身**，所以仍然只有一张表）。
                case EvtKind.Heal: return CardView.HealTextHoldSeconds;

                // 🆕 **2026-10-25（`AU1`）回手 / 洗回牌库**：原版队列对它是**两段 `yield return`**
                //   （`TransformUnitInPlayToCard` 的 0.55 + `AddCardNotDrawnToHand` 的 0.2，判据 →
                //   <see cref="ReturnHoldOf"/>）。这条重载**拿不到「哪张卡 / 回手还是回牌库」**
                //   ⇒ 只给**非 teleport 的基准 0.75**；生产路径走 `DurationOf(BattleEvent)`。
                //   ⛔ 别把 teleport 那 +0.55 写在这里（那会让所有回手都多 0.55）。
                case EvtKind.Return: return ReturnCardAnimHold + ReturnHandRelayoutHold;

                // 🆕 2026-10-22（`W64` · 反击演出）：**只覆盖远程档**（原版 `_ResolveAttack…:1119-1131` ——
                //   远程打法守方才 `ResolveAttackRangedAnim`；近战那一支 `:1132-1150` 守方什么都不播 ⇒ 0）。
                //   本重载**拿不到 `Ranged` / 卡** ⇒ 回「远程档的兜底」（前摇 + `RangedVfxFallback`），
                //   ⛔ 别把它当成生产值：生产走 `DurationOf(BattleEvent)` → `RetaliationHoldOf`。
                case EvtKind.Retaliation: return AttackStepRanged + RangedVfxFallback; case EvtKind.DiscardReveal: return CardFeel.DiscardRevealStepTime; case EvtKind.StealReveal: return CardFeel.StealRevealStepTime;   // 🆕 2026-10-30（`B100_F11`）**揭示那一拍按 `CardFeel.ShowCardBeingDrawn` 的 `stepTime` 排**（弃牌 0.2 / 偷牌 0.4，两档**不同值、⛔ 别统一**）—— 消费口 `BattleDriver.PlayCardBeingDrawn:37859` 用的就是同一对常量；⛔ 别退回 0（那会让下一拍在揭示动画中途就开场）。挤在同一物理行 = **刻意零行数变化**（本文件 `:1014` 有账落点，铁律 18·b）

                // 🆕 2026-10-24（`A2` · `J2` 顺手发现 3）：**不再一把兜住** —— 只有登记成
                // 「零时长」的那一档才静默回 0；没登记的**出声 + 计数**（见 `ZeroDurationKinds` 上面那段）。
                // 🔴 2026-10-24（执行代理 `E2`）：再分一层 —— 登记成「有时间」却没有 `case` 的，
                //   是**另一种病**（登记了没落地），走 `ReportMissingCase`（措辞不同、一样出声）。
                default:
                    if (IsZeroDurationKind(kind)) return 0f;
                    if (IsTimedKind(kind)) ReportMissingCase(kind, "DurationOf(EvtKind)");
                    else ReportUnregisteredKind(kind, "DurationOf(EvtKind)");
                    return 0f;
            }
        }

        /// <summary>带事件的重载 —— **两处要按事件分档**：① 攻击分远近（起手→命中：近战 0.10 / 远程 0.20）；
        /// ② 🆕 2026-10-14（`A1767`）**发动能力分逐卡**（见 <see cref="AbilityAnimDurations"/> 的判据链）。
        /// 其余一律转发给 <see cref="DurationOf(EvtKind)"/>。
        /// 为什么要这个重载：`DurationOf(EvtKind)` 拿不到 `BattleEvent` 里那两个参数
        /// （`Ranged` / `CardId`），而原版这两处**各自都是分档的**
        /// （见 `AttackStepMelee` / `AttackStepRanged` 与 `AbilityFallbackSeconds` 的出处）。
        /// **别退回「一刀切」** —— 那会让远程的出手比原版快一倍、所有卡的发动能力都变成同一个数。</summary>
        public static float DurationOf(BattleEvent e)
        {
            if (e == null) return 0f;
            // 🆕 2026-10-22（`W64` · 反击演出）：**反击**这一拍的长度。
            //   近战档 = 0（原版守方**什么都不播**，`:1132-1150`）；远程档 = 前摇 + 守方那件件自己的链时长。
            //   ⚠️ 这条**必须**在带事件的重载里判（`Ranged` 只有事件知道），
            //      只按 `EvtKind` 的那条重载给的是**远程档兜底**（见那里的注释）。
            if (e.Kind == EvtKind.Retaliation) return RetaliationHoldOf(e);
            // 🆕 **2026-10-25（`AU1`）回手/洗回牌库**：按 `Slot` 与「这张卡有没有 teleport」分档
            //   （判据 → `ReturnHoldOf`；改前它登记成零时长 ⇒ 后面的事件在退场动画刚起播时就开场）。
            if (e.Kind == EvtKind.Return) return ReturnHoldOf(e);
            // 🆕 **2026-10-25（`AU1` · `J1` §5）：`Hit` 按「是不是易伤伤害」分档**（1.0 / 1.4）。
            //   ⛔ 别退回 `DurationOf(EvtKind.Hit)`（那条只给普通档 1.0，易伤目标会早 0.4 s 放行）。
            if (e.Kind == EvtKind.Hit) return HitHoldOf(e);
            if (e.Kind == EvtKind.Attack)
                return e.Ranged ? AttackStepRanged : AttackStepMelee;
            // 🔴 **2026-10-21（W61 第 2 批 · 第 3 条）**：`Deploy` **按「怎么进来的」分档** ——
            //    判据见 <see cref="DeployHoldOf"/>。⛔ 别退回 `DurationOf(EvtKind.Deploy)`（那是兜底 1.3，
            //    手牌出牌会被它多排 0.9 s —— 正是这条账要修的那 0.9）。
            if (e.Kind == EvtKind.Deploy) return DeployHoldOf(e.DeployFrom);
            // 🆕 `A1767`：**发动能力 = 逐卡真值**。`e.CardId` 装的是 `CardDef.Name`（英文卡名），
            //    表里 id 与卡名两个键都有（见 `AbilityAnimDurations.TryGet`）。
            //    🆕 2026-10-25（`AU1` · `J1` §6）：**再按分支分**（替代支 `alternative.seconds`，SW21 = 0.2）
            //    —— 判据与今天的数据缺口 → <see cref="AbilityHoldOf"/>。
            if (e.Kind == EvtKind.Ability) return AbilityHoldOf(e);
            // 🆕 **2026-10-23（R19）抽卡按【座位】分档** —— 原版 `VarsGlobal.TimeToDrawCard(bool isPlayer)`
            //    是一对（0.3 / 0.15）。`e.Player` 就是抽牌那一方 ⇒ 这里能分。
            //    ⛔ 别退回 `DurationOf(e.Kind)`（它只会给我方档 0.3，敌方每抽一张多排 0.15 s）。
            if (e.Kind == EvtKind.Draw) return DrawDealOf(e.Player);
            return DurationOf(e.Kind);
        }

        // ==================================================================
        //  数值 + 出处
        // ==================================================================

        /// <summary>
        /// 出手前的**抬刀停顿**。
        /// 出处（真反编译）：`BattleManager._ResolveAttack_d__438__MoveNext.c:842`
        ///   `CardScript.SetupAttack(...)` 之前 `WaitForSeconds(attackStepTime * 2.0)`。
        /// `attackStepTime` 是 `CardScript` 的私有序列化字段，**卡预制体上的实测值是 0.1**
        /// （`08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_1744609728290659264.json`
        /// 的 `"attackStepTime": 0.10000000149011612`）→ **0.1 × 2.0 = 0.2 秒**。
        /// 这是全项目**唯一一处按角色数据算出来的动作间隔**。
        /// </summary>
        public const float AttackWindUp = 0.2f;

        /// <summary>**出手到命中那一下**（起手→命中）—— 近战档。
        /// 出处：`CardScript` 的私有序列化字段 `attackStepTime = 0.1`
        /// （卡预制体 `08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_1744609728290659264.json`），
        /// 全量反编译 `CardScript._AttackMeleeAnim_d__357__MoveNext.c:194,198,258,260,264`
        /// —— **位移完成即命中帧**。远程那一档是 0.20，见 `DurationOf(BattleEvent)`。
        /// 🔴 **2026-09-18 更正**：原来这里用 `ChargeTime + AttackPunchDuration = 0.65`。
        ///    `timeToChargeAttack`(0x190)=0.35 **在攻击时序里没有消费点** ——
        ///    它只在 `CardScript__OrientToTargetingDirection.c:62` 当**朝向插值**用。
        ///    ⇒ 旧值偏大 0.55 s，整场节奏都跟着慢。规格见 `资料/普查产出_0918/第18行_UI三小条_规格.md` §③。</summary>
        public const float AttackStepMelee = 0.1f;

        /// <summary>远程档的**起手 → VFX 发出**（`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:84,96,102,122-124`）。
        /// ⚠️ **2026-09-19 措辞更正**：原来写的是「起手→命中」——**不准确**，这 0.2 s 只到
        /// 「回正三条 tween 走完 + `WaitForSeconds(AttackStepTime)`」、**才 StartCoroutine 发 VFX**；
        /// 真正的命中时刻在**卡数据**里（`AnimInfo.startDelay`，代码里没有常数）——
        /// 见 `BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:29-31`。</summary>
        public const float AttackStepRanged = 0.2f;

        /// <summary>**命中帧在出手事件之后的延迟**（秒）—— 原版「打中那一帧」/「VFX 发出那一刻」。
        /// <list type="bullet">
        /// <item>**近战 0.1**：段1 位移的 OnComplete 就是命中帧（`CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:16-27`），
        ///   那时 `ResolveAttackAnimationEffects`（震屏 + 命中音 + `AttackHitSmall`）与 `ReceiveAttackAnim`（后坐）同帧。</item>
        /// <item>**远程 0.2**：回正 `DOLocalMove(…, attackStepTime)` + `WaitForSeconds(attackStepTime)`
        ///   （`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:80-104,122-124`）之后才 `DelayedVFXPlay`。</item>
        /// </list>
        /// ⚠️ 它**等于** `DurationOf(Attack)` —— 也就是说「出手那一拍占的时长」正好到命中帧为止；
        /// 本函数只是给「手感/VFX 要在命中帧播」的调用点一个**有名字的口**（⛔ 别在调用点写 0.1/0.2）。</summary>
        public static float HitFrameLag(BattleEvent attack)
        {
            if (attack == null) return 0f;
            return attack.Ranged ? AttackStepRanged : AttackStepMelee;
        }

        /// <summary>**出手 → 伤害结算**的间隔（近战档）= **0.4 s** —— 原版整条近战动画：
        /// 段1 位移 0.1 + 段2 转向 0.1 + 段3 停 0.1 + 段4/5 归位 0.2
        /// （`CardScript._AttackMeleeAnim_d__357__MoveNext.c:130-207,214-268`）。
        /// 跑完之后 `BattleManager._ResolveAttack_d__438__MoveNext.c:988-1000` 才 `BroadcastUnitAttacked`
        /// → `ReceiveDamage`（伤害计数器 / 改血）。
        /// 🔴 **2026-10-21（W61 第 1 批 · 第 2 条）**：原来伤害绑在命中帧（0.1）⇒ **早 0.3 s**。</summary>
        public const float MeleeDamageLag = 0.4f;

        /// <summary>**出手 → 伤害结算**的间隔（远程档）= `AttackStepRanged`(0.2) + 该 VFX 的
        /// `AnimInfo.GetAnimDuration()`（= `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`）。
        /// 判据：`_ResolveAttackRangedAnim…` state 3 的 `WaitForSeconds(AnimInfo.GetAnimDuration())`
        /// （`BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:111-124`）。
        /// ⚠️ **没有全局常数**（124 条 `Atk_*` 里 0.25~3.0 都有）⇒ 生产走 `VfxMap.ChainDuration` 现算。</summary>
        public static float RangedDamageLag(float vfxDuration)
        {
            return AttackStepRanged + (vfxDuration >= 0f ? vfxDuration : RangedVfxFallback);
        }

        /// <summary>🔴🆕 **2026-10-24（`W16`）：命中之后「受击方那一拍」占多久 = 1.0 s。**
        /// 这**不是**某个 tween 的长度，而是原版攻击序列上的一道**真门**（判据全文 →
        /// `资料/普查产出_1024d/S2_受伤时长bool语义.md`，只读查证、2026-10-24 闭合）：
        /// <list type="number">
        /// <item>`CardScript._ReceiveDamage_d__381__MoveNext.c:312` 置 `wholeDamageAnimFlag = 1`；</item>
        /// <item>`:340-346` `if (doItFast == '\0')` ⇒ `WaitForSeconds(DAT_1834b2bb8)`
        ///   （`d:/2/unity_run_ref/GameAssembly.dll` `.rdata` **4 字节 float 实读 = 1.0**，VA `0x1834b2bb8`）；</item>
        /// <item>`:363-368` 睡醒 ⇒ 清旗、协程结束；</item>
        /// <item>而 `BattleManager._ResolveAttack_d__438__MoveNext.c:1152` 以 `twoFractDelay`（**0.2**，
        ///   `+0x4C8`，`DAT_1834b2bb0`）一拍地轮询那个旗、**清零才放行下一步**
        ///   ⇒ 放行 ≈ **1.0 s**（0.2 的粒度对齐；帧序不利时 1.2 s）。</item>
        /// </list>
        /// **普通攻击命中传的是 `doItFast = false`** —— `_ResolveAttack…:999`
        /// `ReceiveDamage(受击方, 0xf /*combatDefender*/, dmg, 0, 0)`，真实参只有前三个
        /// （第 4 个 `0` 是 Ghidra 给无原型函数补的尾位 artifact；签名桩 3 参）⇒ 那句 wait **真执行**。
        /// <para>🔴 **2026-10-24 就地改值（铁律 5）**：`DurationOf(EvtKind.Hit)` 原来返回
        /// `CardFeel.HitRotDuration + CardFeel.ResetDuration = 0.5 + 0.25 = 0.75`。
        /// **那 0.75 是机制错位**：它的出处是 `Impact Light Tween`（Punch 0.5 + ResetBody 0.25），
        /// 而 `CardFeel` 的 `B02`（2026-10-22）已判定 **`Impact Light/Heavy Tween` 是【远程弹着】的
        /// AnimFX 模块，不是近战挨打** ⇒ 拿它当命中拍长既不成立、又比原版**早 0.25 s**。</para>
        /// <para>⚠️ **两个例外 —— ⛔ 别把本值读成「所有命中都 1.0」**（铁律 5·c：一个值 ≠ 全部情况）：
        /// <list type="bullet">
        /// <item>① **`DamageType ∈ {Vulnerable(7), VulnerableCombat(8)}`** —— 协程在置旗**之前**先等
        ///   **0.4 s**（`MoveNext:291-304`，`DAT_1834b3158` 实读 0.4）⇒ `_ResolveAttack` 那一帧看到的旗
        ///   还是 **0**、序列直接往下走 ⇒ **原版这条路上根本没有这道门**；我们统一给 1.0 对它**偏长**。
        ///   ⚠️ 这类在实战里的**占比没统计** ⇒ 判据不足，**本批不给 `Hit` 分第二档**（只记在这里）。</item>
        /// <item>② **AoE（`_ResolveMultiDamage…:154 / :168`）** 的 `doItFast` 传的是
        ///   `battleAction.extraValue` ⇒ **那条路可能快进**。（`extraValue` 这名字跟「快进」看不出关系、
        ///   **谁把它置 true 未查清** —— 如实记，⛔ 别猜。）</item>
        /// </list></para>
        /// 🔑 **本常量是这一格数值的唯一出处** —— `DurationOf(EvtKind.Hit)` 与 `SourceOf(Hit)` 都指向这里，
        /// ⛔ 别在下面 `switch` 里另写一个 `1.0f`（两份数迟早不一致）。</summary>
        public const float HitWaitSeconds = 1.0f;

        /// <summary>🆕 **2026-10-23（P1 · 演出时序 ③）：一条攻击 VFX 排期之后，「队尾」应该落在哪个时刻**
        /// = `_clock + vfxAt + 该件自己的时长`（`clock` = 时间线泵的**绝对**时钟，`vfxAt` = 相对本拍的偏移，
        /// `chainDuration` = 该件的 `AnimInfo.GetAnimDuration()`）。</summary>
        ///
        /// <para><b>为什么单开一个口</b>：这个形状在 `BattleDriver` 里有四处
        /// （`ScheduleAction` 的 `_clock + delay` · 攻击 VFX 的队尾 · 技能拍的队尾 · 攻击 VFX cue 的队尾），
        /// 而**攻击 VFX 的队尾那一处漏了 `_clock +`**（`BattleDriver.cs:17505`，2026-10-23 前）⇒
        /// 中后期 `_clock > 3` 时算出来的数**恒小于已经排好的队尾**，`ExtendTimelineTail` 变成空操作 ——
        /// **等于这一件没排队尾**（`_timelineTailAt` / `_blockingQuietAt` 都不动 ⇒ 下一批事件可以插到
        /// 它还没演完的时候开场），而且**一行日志都不会有**（静默）。</para>
        /// <para>⛔ 别在调用点再手写 `_clock + ...` —— 少写一处就是这么来的；`_clock = 0` 时两种写法同值
        /// （所以「只在开局测一次」抓不到这个缺陷，自检要拿 `clock > 0` 的输入）。</para>
        public static float AttackVfxTailAt(float clock, float vfxAt, float chainDuration)
        {
            return UnityEngine.Mathf.Max(0f, clock)
                 + UnityEngine.Mathf.Max(0f, vfxAt)
                 + UnityEngine.Mathf.Max(0f, chainDuration);
        }

        /// <summary>拿不到「这张卡 / 这个阵营」时远程 VFX 的兜底时长 = **0.75 s**。
        /// 判据 = 4 个阵营的共享件 `Atk_BulletImpact_2shot`（`BulletImpact_2shot_trail*` / `NecronGauss`）
        /// 的 `AnimInfo` 时长全部 = 0.75（`数据/游戏数据/animinfo_lookup.json` 逐条现读）；
        /// `ByEvent` 那条通用兜底 `BulletImpact_2shot` 是 0.4 —— 这里取**阵营档**（生产路径绝大多数命中这一档）。
        /// 🔴 **2026-10-21**：原来这里写死 1.0（旧 `RangedFlight`），ASH77 会晚 0.25 s、1-shot 会晚 0.7 s。</summary>
        public const float RangedVfxFallback = 0.75f;

        /// <summary>
        /// 🆕 2026-09-29 **出牌落位之后那一拍**（0.3 s）—— 我们以前整个缺这一段，整局节奏因此偏快。
        ///
        /// 出处：`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` 的 **state 4→5**
        /// （`:673`）就是 `new WaitForSeconds(DAT_1834b2dc8)`，那个常量从 `GameAssembly.dll`
        /// 浮点池**实读 = 0.3f**。它排在 state 3（`CardScript.MinionPlayedIntoField` 落位协程）**之后**
        /// ⇒ 语义 = **落位动画跑完 → 停 0.3 s**。
        ///
        /// 🔴 **与 `项目任务.md` §三 第 26 条 第 2 款的措辞有一处出入，这里如实记下**：
        /// 那里写「原版 `WaitForSeconds(0.3)` × 2 ⇒ 我们 `EvtKind.Play` 时长 0，**少 0.6 s**」。
        /// 逐条读过之后是：
        /// · 那两处 0.3 **不都在同一条链上** —— 另一处是 `MinionManager.GetMinionConversionTime`
        ///   （`:417` / `:486`），也就是**飞行时长** `minionToConversionPointTime = 0.3`，
        ///   我们**早就有了**（`DeploySequence.MoveTime`，见 `BattleScene` 那条断言）。
        /// · 真正缺的只有**落位之后那一拍** 0.3。
        /// ⇒ 落在 `DurationOf(Deploy)` 上（落位之后）而**不是** `DurationOf(Play)`（那是「牌还在手上」）。
        /// 数值上整局仍会与前一条口径一样变慢，但每一处都有出处，且不会**重复计**飞行那 0.3。
        /// </summary>
        public const float DeployLandHold = 0.3f;

        /// <summary>**效果召唤 / 复活**那一档的 `Deploy` 占多久（秒）= **1.0**
        /// —— `Summon Troop Tween` 的 **`DelayTween.duration = 1.0`**
        /// （`数据/游戏数据/tween/Summon Troop Tween.json` 现读：ScaleTween d=0 · DelayTween **1.0** ·
        ///  ResetBodyTween 0.3）。原版走 `BattleManager._ResolveSummonUnit_d__510__MoveNext`。
        /// ⚠️ ResetBodyTween 那 0.3（`appendType=0` = After）**没算进去**：`waitAnimation = 0`
        ///    ⇒ 它不阻塞下一条动作；要照字面全算的话这一档会变成 1.3（**如实记**，
        ///    见 `资料/普查产出_1021/W61b_战斗线_演出链第2批.md`）。</summary>
        public const float DeploySummonHold = 1.0f;

        /// <summary>🆕 **2026-10-21（W61 第 2 批 · 第 3 条）：`Deploy` 这一拍占多久 —— 按「怎么进来的」分档。**
        ///
        /// <para>判据（逐条现读 `d:/2/tools/decomp_full/`）：</para>
        /// <list type="bullet">
        /// <item>**手牌打出**（`_ResolvePlayCardFromHand_d__447__MoveNext.c`）：
        ///   `yield MinionPlayedIntoField` 整条跑完之后 state 4→5 是 `WaitForSeconds(DAT_1834b2dc8 = 0.3)`；
        ///   而 `_MinionPlayedIntoField_d__314__MoveNext.c` 的 **state 3 尾**还有
        ///   `WaitForSeconds(get_AttackStepTime() = 0.1)` ⇒ **合计 0.4**
        ///   （0.1 + 0.3）。⚠️ 这就是对账 §2·1 #11 那句「0.1 + `_ResolvePlayCardFromHand` 0.3 = 0.4」。</item>
        /// <item>**自动从手牌打出**（`_ResolveCardAutoSummoned_d__511__MoveNext.c`）：只有
        ///   `MinionPlayedIntoField` 那一段（state 3→4 直接接下去，**没有**再等 0.3）⇒ **0.1**。</item>
        /// <item>**效果召唤 / 复活**（`_ResolveSummonUnit_d__510__MoveNext.c`）：走 `Summon Troop Tween`
        ///   ⇒ <see cref="DeploySummonHold"/> 1.0。</item>
        /// <item>**没写来源**（`DeploySource.Unknown`：旧路径 / 自检手工合成的 `Deploy`）⇒ **老口径 1.3**，
        ///   与 `BattleEvent.DeploySource` 的 doc「消费端读到 `Unknown` 就按改前的老行为处理」一致。</item>
        /// </list>
        /// <para>🔴 修之前全档都是 1.3 ⇒ **手牌出牌每个兵多 0.9 s**（对账 §2·1 #11「数值错（疑似）」坐实）。</para>
        /// </summary>
        public static float DeployHoldOf(DeploySource from)
        {
            switch (from)
            {
                case DeploySource.HandPlayed:       return AttackStepMelee + DeployLandHold;   // 0.4
                case DeploySource.AutoPlayFromHand: return AttackStepMelee;                    // 0.1
                case DeploySource.Summon:
                case DeploySource.Reanimate:        return DeploySummonHold;                   // 1.0
                default:                            return DeploySummonHold + DeployLandHold;   // 1.3（老口径）
            }
        }


        /// <summary>命中和阵亡之间那一下的间隔。**这条是我们挑的**（0.1 = 引擎节拍 `minDelay`）。
        /// ⚠️ 原文写「`VarsGlobal` 资产缺失、查不到」—— **那句过期了**：整表 2026-09-17 已解出，
        /// 但**那个字段（原版等的是 `deathTimeMinionDuration`）我们算在阵亡动画时长里了**
        /// ⇒ 这里再加同样的数会重复计一次。见上面 `DelayBetween` 里 `EvtKind.Death` 那段注释。</summary>
        public const float DeathHold = 0.1f;

        /// <summary>
        /// 触发效果相对上一条的延迟。
        /// 出处：`BattleManager` 构造函数里那两个命名延迟常量 —— `sec3FractionDelay = 0.3`
        /// （`BattleManager__.ctor.c`，数值从 `GameAssembly.dll` 读出）。
        /// ⚠️ 它是引擎里反复出现的**通用「等一拍」粒度，不是 Trigger 专用**。
        /// </summary>
        public const float TriggerHold = 0.3f;

        /// <summary>触发效果占多久。出处：**2026-09-29 查实 = `BattleManager.twoFractDelay`（字段 `+0x4C8`）
        /// 实读 **0.2** s** —— 那条动作的结算器就是「`CardScript.ActivateTriggerTraitAnim` →
        /// `yield WaitForSeconds(twoFractDelay)` → Finish」（`BattleManager._ResolveTriggerTraitAnim_d__476:11-14`）。
        /// ⚠️ 原写 0.5（`sec5FractionDelay`）—— 那是**引擎的通用节拍**，不是 Trigger 这条链的数；
        /// 同一份查证里另一个动作（`_ResolveAbilityIconAnim_d__475`）也印证了这个形状：
        /// `trigger == 440` 时先等 `sec5FractionDelay(0.5)`、再 `ActivateIconAnim`、最后同样等
        /// `twoFractDelay(0.2)` ⇒ **图标/触发这条线是 0.2 s**（440 那种是 0.7 s）。</summary>
        public const float TriggerLen = 0.2f;

        /// <summary>🆕 2026-10-14（`A1767`）**发动能力**在「不知道是哪张卡」时的兜底秒数。
        ///
        /// 🔴 **它现在只是兜底，不再是「原版就是 1.0」** —— 逐卡真值走
        /// <see cref="DurationOf(BattleEvent)"/> → <see cref="AbilityAnimDurations"/>。
        ///
        /// 这个 `1.0` 的来历（**旧口径，保留痕迹**）：本地 506 条【有名字】的 `AnimInfo` 时长之和的**中位数**
        /// （表 = `数据/游戏数据/card_anim_map.json`：908 条总条目 = 506 有名 + 402 光 GUID）。
        /// 新的逐卡表（`Resources/ability_anim_durations.json`，🔴 **现读 93 张卡 / 160 条 CardAnim**；
        /// 真值源 = **当前 build 的卡 SO**，见文件头「2026-10-22 就地订正」）**中位也恰好是 1.0**
        /// ⇒ 两代口径在这一格上偶然相等 —— 所以「兜底取 1.0」不是拍脑袋，**但也不能当成原版值**。
        /// ⚠️ `AbilityAnimDurations.FallbackSeconds`（表里现读的那个数）**必须等于本常量** ——
        ///    自检会逐字比（`BattleScene.Run` 那条「兜底值与表一致」）。
        ///
        /// 🔴 **为什么这条重载一定拿不到逐卡值**：原版判据是「**那条技能自己的** `AnimInfo` 四字段之和」
        /// （`AnimInfo.GetAnimDuration` = `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`），
        /// 而「哪条技能」只存在于**事件**里（`BattleEvent.CardId`）。`DurationOf(EvtKind)` 的签名里没有卡
        /// ⇒ 它只能给兜底值。**两条路都要跑到才算接完**，缺一条就会「看着接上了、其实一半还是常数」。
        /// 判据链全文 → `Core/AbilityAnimDurations.cs` 文件头。**三条例外 + 一条条件分支**（2026-10-22 现读，
        /// 逐条见全量件 `uncovered` / `conditional`）：
        ///   · `SOR30` / `SOR65` / `SW9` —— 卡 SO 里 `hasActiveAbility=1`，但 `activeAbility` 一条非空 CardAnim
        ///     引用都没有（`m_AssetGUID` 全空）⇒ 原版这条路上不放动画 ⇒ 退回兜底并**出声**；
        ///   · `SW21 Wulfen Pack Leader` —— 普通支 **0.4**（表里存这支）/ 替代能力支 **0.2**；
        ///     运行时按棋盘切支**还没接**（判据 = 原版 `RawCardScript.GetActiveAbility`）。
        /// 另：`tweenAnim`（UnitTweenSO）那一段没算 · `cardAbilities` 那一族不进本表。</summary>
        public const float AbilityFallbackSeconds = 1.0f;

        /// <summary>🆕 2026-10-14（`A1816`）**抽卡**这一条事件占多久 = **0.3 s**。
        ///
        /// 出处 = `VarsGlobal.timeToDrawPlayerCard`（整表见 `资料/VarsGlobal_原版数值.md` §一），
        /// 也就是**发牌入场那条补间自己的时长** —— 判据只此一处，所以这里直接引用
        /// `CardFeel.DealDuration`（⛔ **别再写一个字面量 0.3**，那正是「两处写同一条规则」）。
        /// 原版这一档**确实会等**：`PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c:78-80`
        /// 在把牌送进手牌之后 `WaitForSeconds(TimeToDrawCard + DAT_1834b2dc4)` 才走下一步。
        ///
        /// ⚠️ **本值只覆盖我方档**（原版是 `VarsGlobal.TimeToDrawCard(bool isPlayer)` 分敌我的一对：
        ///    我方 0.3 / 敌方 0.15）。`DurationOf(EvtKind)` 拿不到「哪一方是我」
        ///    （`DurationOf(BattleEvent)` 那个重载也只有 `e.Player`、没有座位号）
        ///    ⇒ 敌方抽卡在时间线上会被多排 0.15 s。这不是「值查不到」，是**这张表取不到那个参数** ——
        ///    动画那一侧**没有**这个问题（`CardFeel.DealSeconds(false)` / `DealInQuad` 用的就是 0.15）。
        /// 🔴 **2026-10-23（R19）就地更新**：`DurationOf(BattleEvent)` 那一路**已经分档**了
        ///    （`DrawDealOf(e.Player)` + `IsLocalSeat` 钩子）⇒ 生产路径不再有「多排 0.15 s」这个问题；
        ///    本常量现在只给**只按 `EvtKind` 的旧重载**（探针 / 拿不到座位号时的兜底）。</summary>
        public const float DrawDeal = CardFeel.DealDuration;

        /// <summary>🆕 **2026-10-23（R19）：这一条抽卡事件占多久 —— 按【谁抽的】分档。**
        ///
        /// <para>**判据 = 原版 `VarsGlobal.TimeToDrawCard(bool isPlayer)`**
        /// （`资料/VarsGlobal_原版数值.md` §一：`timeToDrawPlayerCard = 0.3` / `timeToDrawEnemyCard = 0.15`；
        /// 消费点 `PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c:78-80` 那个 `WaitForSeconds`）。
        /// 而 <see cref="IsLocalSeat"/> 回答「这一方是不是本地玩家」—— **语义完全对齐**
        /// （原版那个 `isPlayer` 就是「本地玩家」，联机局本机可能是座位 1）。</para>
        ///
        /// <para>⚠️ **没挂钩**（纯 `RuleEngine` 自检 / 探针）⇒ 按**我方档**并**出声一次** ——
        /// ⛔ 不是静默按 0.3 走（那就是「看着有出处、其实是猜的」）。
        /// ⛔ 判据只此一处：`CardFeel.DealSeconds` 是那一对数的**唯一**来源（动画侧也用它）。</para></summary>
        public static float DrawDealOf(int seat)
        {
            // 座位没写（`-1`，手工合成的事件）⇒ 按我方档（与改前行为一致），⛔ 别把 -1 当「对面」。
            if (seat < 0) return DrawDeal;
            if (IsLocalSeat == null)
            {
                WarnNoLocalSeat();
                return DrawDeal;             // 我方档（= `DealSeconds(true)`）
            }
            return CardFeel.DealSeconds(IsLocalSeat(seat));
        }

        static bool _warnedNoLocalSeat;

        /// <summary>「`IsLocalSeat` 没挂 ⇒ 抽卡只能按我方档」的出声（只报一次）。</summary>
        static void WarnNoLocalSeat()
        {
            if (_warnedNoLocalSeat) return;
            _warnedNoLocalSeat = true;
            UnityEngine.Debug.LogWarning(
                "[EventTiming] `IsLocalSeat` 没挂（没有驱动）⇒ `EvtKind.Draw` 一律按**我方档** "
              + $"{DrawDeal:F2}s 排；敌方那一档（{CardFeel.DealDurationFoe:F2}s）在这条路径上取不到。"
              + "生产路径由 `BattleDriver.HookEventTimingCardSource` 挂钩。");
        }

        /// <summary>
        /// 这一档用到的**每一个数**都**有原版出处**吗（`DurationOf` 与 `DelayBetween` 里属于它的那几格）。
        ///
        /// <para>🔴 **2026-10-23（R17）就地重写（铁律 5）—— 原来是 `{ return true; }`，等于一条自证断言**
        /// （断言永远绿，问不出任何东西）。现在**逐档查表**，返回 `false` 的两档各有明确原因
        /// （带 `out string` 的那个重载会说给人听）：</para>
        /// <list type="bullet">
        /// <item><b>`Death`</b> —— 时长 **0.7** 有出处（`CardFeel.DeathTotal` = 整条阵亡协程，
        ///   `BattleManager._ResolveMinionDeath_d__454:113-121` 对 `UnitDeath` 是 `yield return`），
        ///   但「命中和阵亡之间那一拍」= <see cref="DeathHold"/> **0.1 是我们挑的**
        ///   （引擎节拍 `minDelay`，见那个常量的注释：「原版那个 `WaitForSeconds` 等的正是
        ///   `deathTimeMinionDuration`，我们已经把它算进动画时长，再加同样的数会重复计」）。
        ///   ⚠️ 2026-10-23 前这里写的时长是 0.2（只算了消散那一段）—— 那一条**已作废**。</item>
        /// <item><b>`Ability`</b> —— 逐卡真值有出处（`AbilityAnimDurations`），
        ///   但**查不到那张卡时**退回的 <see cref="AbilityFallbackSeconds"/> **不是原版值**
        ///   （它是真值表的中位数，恰好等于旧常数 1.0；见那个常量的 doc）。</item>
        /// </list>
        ///
        /// <para>⚠️ **不在这张表里的种类**（`Play` / `Return` / `Gain*` / `CollectWaystone` / `SecretOrder`）
        /// `DurationOf` 走 `default: 0` —— **它们不占时间**，没有「无出处」的数值可言 ⇒ 回 `true`
        /// （⛔ 别把它当「这些事件也有出处」来引用）。</para>
        ///
        /// <para>🆕 **2026-10-24（`E2` · `DG2` 红 #1~#3）**：`TimedKinds` 里那 9 档（`Deploy`/`Attack`/`Hit`/
        /// `Trigger`/`Draw`/`Heal`/`Retaliation` + 上面显式回 `false` 的 `Death`/`Ability`）**也已登记** ⇒
        /// 除 `Death`/`Ability` 外一律回 `true`。判据 = <see cref="IsRegisteredEventKind"/>（两张表任一登记）。
        /// `false` 只剩**三种**：`Death` · `Ability` · **未登记**（后者出声）。
        /// 🆕 **2026-10-25（`AU1`）**：`Return` 也进了 `TimedKinds`（回手 0.75 / teleport 1.30，判据 →
        /// <see cref="ReturnHoldOf"/>），它有出处 ⇒ 照旧回 `true`。</para>
        /// </summary>
        public static bool IsSourced(EvtKind kind)
        {
            string why;
            return IsSourced(kind, out why);
        }

        /// <summary><see cref="IsSourced(EvtKind)"/> 的带原因重载（`false` 时 <paramref name="unsourcedPart"/>
        /// = 哪一格没出处；`true` 时是 `null`）。自检直接拿它打日志。</summary>
        public static bool IsSourced(EvtKind kind, out string unsourcedPart)
        {
            unsourcedPart = null;
            switch (kind)
            {
                case EvtKind.Death:
                    unsourcedPart = $"`DelayBetween(Death)` = `DeathHold` {DeathHold:F2}s —— **我们挑的**"
                                  + "（引擎节拍 `minDelay`；原版那个 `WaitForSeconds` 等的是 "
                                  + "`deathTimeMinionDuration`，已算进阵亡动画时长，⛔ 不能重复计）"
                                  + $"；时长 {CardFeel.DeathTotal(false):F2}s 本身有出处（`CardFeel.DeathTotal`）";
                    return false;
                case EvtKind.Ability:
                    unsourcedPart = $"查不到那张卡时退回的 `AbilityFallbackSeconds` {AbilityFallbackSeconds:F2}s "
                                  + "**不是原版值**（= 真值表的中位数，恰好等于旧常数）";
                    return false;
                // 🆕 2026-10-24（`A2`）：未登记的 `EvtKind` ⇒ **如实报「无出处」**（不是「有出处」）。
                //   ⇒ `BattleScene` 那条 `unsourced == 2` 会因为多出这一条而变红，逼着加值的人来登记。
                // ✅ 已登记的**任何**一档都回 `true` —— **零时长（`ZeroDurationKinds`）** 或
                //    **有时间（`TimedKinds`）**：
                //      · 零时长档**不占时间** ⇒ 没有「无出处的数值」可言；
                //      · 有时间档各自在 `DurationOf` / `SourceOf` 里有 `case`。它们**不是**「无出处」的
                //        那种未登记；真有「值是我们挑的」情形（`Death` / `Ability`）在上面两个 `case` 里
                //        **已经显式回 `false`** ⇒ 走不到这里。
                // 🔴 **2026-10-24（`E2` · `DG2` 红 #1~#3）就地订正（铁律 5）**：这里原来只查
                //    `IsZeroDurationKind(kind)`，把「**有时间且已登记**」的 7 档（Deploy/Attack/Hit/Trigger/
                //    Draw/Heal/Retaliation）也归进了「未登记」⇒ `unsourced` 实得 9、外加 7 条 `LogError` 噪音。
                //    错因：`IsRegisteredEventKind`（本文件、就在上面）登记表**两张都查**，这里却只查了一张。
                //    ⛔ 别写回「只查零时长表」—— 那会让登记表与查表人口径不一致（自相矛盾的两条断言一绿一红）。
                // ⚠️ **2026-10-24（执行代理 `E2`）：这里答的是「登记表里有没有名字」**，
                //    它**检验不了**「`DurationOf` / `SourceOf` 的 `switch` 里真有 `case`」（那要靠执行才知道）。
                //    ⇒ 「登记了但没 `case`」这一种病由两处兜：
                //      ① `DurationOf` / `SourceOf` 的 `default:` 走 `ReportMissingCase` 出声；
                //      ② 自检 `EngineClosedDeckAssert` 的 §B 逐档调 `DurationOf` 并盯未登记计数
                //         （登记成有时间却没 `case` ⇒ 计数会涨 ⇒ 红）。
                //    ⛔ 别为了让这里也答得出来去调 `DurationOf` —— 那会**顺手打日志**（自检污染生产日志）。
                default:
                    if (IsRegisteredEventKind(kind)) return true;
                    unsourcedPart = $"**未登记的事件种类** `EvtKind.{kind}`（值 {(int)kind}）—— "
                                  + "`DurationOf` 走 `default: 0f`、`SourceOf` 也没有它 ⇒ 这个 0 与这段"
                                  + "「出处」都是**没人认领的**。登记办法见 `EventTiming.ZeroDurationKinds` 上面那段。";
                    ReportUnregisteredKind(kind, "IsSourced");
                    return false;
            }
        }

        /// <summary>这条数值的出处（自检报数用）。**以「拍的」结尾的就不是原版的数**</summary>
        public static string SourceOf(EvtKind kind)
        {
            switch (kind)
            {
                case EvtKind.Deploy: return "🆕 2026-10-21 按 `BattleEvent.DeployFrom` 分档（见 `DeployHoldOf`）"
                                          + "：手牌打出 0.1+0.3=**0.4** · 自动打出 0.1 · 效果召唤/复活 "
                                          + "`Summon Troop Tween` 的 DelayTween **1.0**；"
                                          + "无来源（Unknown）退回老口径 1.3";
                case EvtKind.Attack: return "`CardScript.attackStepTime`=0.1（位移完成即命中帧；远程 0.2）";
                // 🔴 **2026-10-24（`W16`）就地订正（铁律 5）**：这里原来写「⚠️ **出处待补** —— 现值 = `CardFeel.HitRotDuration`
                //   0.5 + `ResetDuration` 0.25 = 0.75；候选真值 = `WaitForSeconds(1.0)`（被 `+0x34` 的 bool
                //   把着，**语义未查清**）⇒ 判据未闭合，⛔ 别猜」。**那个 bool 的语义 2026-10-24 查清了**
                //   （`+0x34` = `doItFast`，`false` ⇒ 走 wait；普通攻击命中传的就是 false），
                //   所以现在**有出处了**、值也改成了 1.0。数值只在 `HitWaitSeconds` 那一处（⛔ 别在这儿复述数）。
                case EvtKind.Hit: return "原版 `CardScript._ReceiveDamage_d__381__MoveNext.c:342` 的 "
                                       + "`WaitForSeconds(1.0)`（`DAT_1834b2bb8`，`GameAssembly.dll` `.rdata` "
                                       + "4 字节 float 实读）—— 普通攻击命中传 `doItFast = false`"
                                       + "（`BattleManager._ResolveAttack_d__438__MoveNext.c:999`）⇒ 那句真执行；"
                                       + "`_ResolveAttack…:1152` 又以 `twoFractDelay` 0.2 一拍轮询 "
                                       + "`wholeDamageAnimFlag` 等它清零 ⇒ **放行 ≈ 1.0 s**（帧序不利 1.2 s）。"
                                       + "🔴 2026-10-25（`AU1` · `J1` §5）订正两个例外："
                                       + "①`DamageType` 7/8（Vulnerable/VulnerableCombat）**先等 0.4**（`:291-303`，"
                                       + "`DAT_1834b3158`=0.4）**再走那 1.0** ⇒ **1.4 s**（不是「那条路没有这道门」）"
                                       + "—— 见 `HitHoldOf`；"
                                       + "②`doItFast`（AoE 快进）**本 build 不可达**：全库 6 个调用点第 3 实参全是 0 "
                                       + "⇒ ⛔ 不实现。判据全文 → `资料/普查产出_1025/J1_判据_战斗时间线与HUD缺口.md` §5";
                case EvtKind.Ability: return "🆕 2026-10-14（`A1767`）**逐卡真值**：该卡 `activeAbility` 各 CardAnim 的 "
                                             + "`AnimInfo.GetAnimDuration()`（`vfxDelayTime + timeAtStartPos + timeMoving "
                                             + "+ timeAtEndPos`）之和 → `AbilityAnimDurations`（**现读 93 张卡**）；"
                                             + "🆕 2026-10-25（`AU1` · `J1` §6）**再按分支**：替代支（`CardDef.UsesAlternativeAbility`"
                                             + " 为真，现读只有 `SW21`）取 `alternative.seconds`（SW21 = **0.2**）· 普通支取 `s`"
                                             + "（SW21 = 0.4）—— **替代那一列运行期表今天还没有** ⇒ 两支都按 0.4（见 `AbilityHoldOf`）；"
                                             + "查不到那张卡 ⇒ 兜底 "
                                             + $"{AbilityFallbackSeconds}（= 真值表中位数，与旧常数恰好相等）";
                case EvtKind.Trigger: return "`BattleManager.twoFractDelay`(**+0x4C8**) = 0.2 —— `_ResolveTriggerTraitAnim` 那条链的等待";
                // 🔴 **2026-10-23（P1 · 演出时序 ②）就地订正（铁律 5）**：原文写「0.2（督军那档 0.5 见
                //   `CardFeel.DeathDissolve`）」—— **那句现在不成立**：这条演出占多久不再是消散那一段，
                //   而是**整条阵亡协程**的寿命（原版对 `UnitDeath` 是 `yield return`，队列真等它跑完）。
                //   0.2 / 0.5 仍然存在，但它们只是**抖动**长度（`DeathDissolve`），不影响这条时长。
                case EvtKind.Death: return "整条阵亡协程 = `CardFeel.DeathTotal` **0.7 s**（抖不等 → "
                                         + "`WaitForSeconds(gVars+0x34)` 0.5 → 0.2 后进墓地；"
                                         + "`BattleManager._ResolveMinionDeath_d__454:113-121` 对 `UnitDeath` 是 "
                                         + "`yield return` ⇒ 队列串行等 0.7。旧值 0.2 = 只算了消散那一段）";
                case EvtKind.Draw: return "`VarsGlobal.TimeToDrawCard(bool isPlayer)` 那一对："
                                        + "我方 `timeToDrawPlayerCard`=0.3（`CardFeel.DealDuration`）/ "
                                        + "敌方 `timeToDrawEnemyCard`=0.15（`CardFeel.DealDurationFoe`）；"
                                        + "🔴 2026-10-23（R19）起**按座位分档**（`DrawDealOf(e.Player)` + `IsLocalSeat`），"
                                        + "⛔ 不是一刀切 0.3";
                case EvtKind.Heal: return "`CardScript._Heal_d__468__MoveNext.c:204` 的 `WaitForSeconds(1.5)`"
                                        + "（= `CardView.HealTextHoldSeconds`；`ResolveHeal` 每目标 `yield return` 等它跑完 "
                                        + "⇒ 多目标治疗严格串行，`BattleManager._ResolveHeal_d__491__MoveNext.c:114-147`）";
                // 🆕 2026-10-25（`AU1`）回手 / 洗回牌库 / 墓地取回 —— 判据全文见 `ReturnHoldOf` 的 doc
                case EvtKind.Return: return "`BattleManager._ResolveRecallToHand_d__542__MoveNext.c:273-278`"
                                         + "（`yield return CardScript.TransformUnitInPlayToCard`）"
                                         + " ⇒ **0.55**（`_d__256__MoveNext.c:80-85` = `GetHandToBoardAnimEventTime()`"
                                         + " = clip `Card Hand To Board` 唯一一条 AnimationEvent 的时刻）"
                                         + " **+ 收尾**：回手 `:55-60` `AddCardNotDrawnToHand` 等 `timeToPositionCard` **0.2**"
                                         + "（`_d__40:42-47`）⇒ **0.75**；回牌库 `_ResolveRecallToDeck:350-356` 等"
                                         + " `MoveToDeck(…,1,0)` 的返回值 = `timeToScaleIntoMoving 0.1 + 0.3` = **0.4**"
                                         + " ⇒ **0.95**（🔴 `J1` §3 订正：上一版记 0.85 只算了 0.3）；"
                                         + "卡背朝上那一档 0.65 的判据已闭合（`J3` §1：`ShouldShowCardback(cardState) || "
                                         + "card+0x54 = EntityScript.armedTrap`）但**我们那两条路都不存在** ⇒ ⛔ 不改；"
                                         + "⚠️ 带 `teleport`(0x4ce=1230) 的卡 state 1 再等一次 ⇒ 回手 **1.30** / 回牌库 **1.50**；"
                                         + "⚠️ `Slot < 0`：墓地取回 `CreateCardInGame` 等 `timeToDrawPlayerCard` **0.3**（`J1` §4），"
                                         + "`ephemeral` 原版无此链 ⇒ **0**";
                case EvtKind.AmbushExit: return "**占 0**（原版 `CardScript.SetAmbush(card,false)` 是普通方法、不阻塞，"
                                              + "紧跟着就是同一击的 `Hit`）；那件特效自己的材质补间 = "
                                              + "`AmbushEffect` 的 `MaterialTweenFloat(_Blend)` delay 0.35 + duration 1.0 = 1.35"
                                              + "（**不进时间线**；原件 = `新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/"
                                              + "MonoBehaviour/MonoBehaviour_6849782588473963486.json` 的 `references.RefIds[0].data`）";
                // 🆕 2026-10-22（`W64` · 反击演出）
                case EvtKind.Retaliation: return "`BattleManager._ResolveAttack_d__438__MoveNext.c:1114-1152`"
                                              + "（`:1119-1131` 远程打法才 `ResolveAttackRangedAnim(守方, 攻方, 0, 0)`，"
                                              + "`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c`：回正 0.1 + 等 0.1 → 发件 → "
                                              + "`WaitForSeconds(GetAnimDuration())`；**协程跑完才**落伤害 `:1136-1147`）"
                                              + " ⇒ 前摇 `AttackStepRanged` 0.2 + **守方那件**的 `ChainDuration`"
                                              + "（拿不到 ⇒ `RangedVfxFallback` 0.75，出声）；⚠️ 只覆盖**远程档**，近战档守方不播 = 0";
                // 🆕 2026-10-30（执行代理 `B100_F11`）：**弃牌 / 偷牌的揭示那一拍** —— 两档**各用各的**
                //   `stepTime`（⛔ 别统一成一个数；原版两条链各传自己的字面量）。
                //   判据全文 = `CardFeel.DiscardRevealStepTime` / `StealRevealStepTime` 的 doc：
                //   `.rdata 0x1834b2bb0` = 0.2f · `.rdata 0x1834b3158` = 0.4f（`工具/read_literal.py` 实读）；
                //   调用点 `BattleManager._ResolveDiscardFromHand_d__544__MoveNext.c:95` /
                //   `BattleManager._ResolveStealFromDeck_d__547__MoveNext.c:243`，两处都是
                //   `CardScript.ShowCardBeingDrawn(card, <那个常量>)` ⇒ 这一拍就演这么久。
                //   ⚠️ 它们进 `TimedKinds` 是**登记**（改前没登记 ⇒ `DurationOf` 落 `default: 0`、
                //   `IsSourced` 回 false ⇒ `BattleScene` 的 `unsourced == 2` 变 4）。
                case EvtKind.DiscardReveal: return "**弃牌揭示**：`stepTime` = **0.2**（`.rdata 0x1834b2bb0` 实读，"
                                              + "= `CardFeel.DiscardRevealStepTime`；调用点 "
                                              + "`BattleManager._ResolveDiscardFromHand_d__544__MoveNext.c:95` "
                                              + "`CardScript.ShowCardBeingDrawn(card, DAT_1834b2bb0)` ⇒ 摆进 `HandMoving` 层"
                                              + "（我方）/ 卡背翻面（敌方，时长 = 延迟 = 0.2）就演这么久）";
                case EvtKind.StealReveal: return "**偷牌揭示**：`stepTime` = **0.4**（`.rdata 0x1834b3158` 实读，"
                                              + "= `CardFeel.StealRevealStepTime`；调用点 "
                                              + "`BattleManager._ResolveStealFromDeck_d__547__MoveNext.c:243`）"
                                              + "—— 🔴 与弃牌那一档**不同值、⛔ 别统一**（两条链各用各的字面量，不是笔误）";
                // 🆕 2026-10-24（`A2`）：登记成零时长的那一档照旧回「（无）」；**未登记**的出声 + 明写。
                // 🔴 2026-10-24（执行代理 `E2`）：与 `DurationOf` 同一条口径 —— 「登记了但没 `case`」
                //   走 `ReportMissingCase`（⛔ 别把它也说成「没在登记表里」，那是两件事）。
                default:
                    if (IsZeroDurationKind(kind)) return "（无）";
                    if (IsTimedKind(kind))
                    {
                        ReportMissingCase(kind, "SourceOf");
                        return $"⚠️ **`EvtKind.{kind}`（值 {(int)kind}）登记在 `TimedKinds` 里，"
                             + "但 `SourceOf` 的 `switch` 里没有它的 `case`** —— 登记表只是声明，"
                             + "值必须落在 `case` 上（两处一起改）。";
                    }
                    ReportUnregisteredKind(kind, "SourceOf");
                    return $"⚠️ **未登记的事件种类** `EvtKind.{kind}`（值 {(int)kind}）—— "
                         + "见 `EventTiming.ZeroDurationKinds` 上面那段（新增 `EvtKind` 必须显式登记）";
            }
        }

        /// <summary>
        /// 引擎里反复出现的「等一拍」粒度（`BattleManager` 构造函数的命名常量，
        /// 数值从 `GameAssembly.dll` 读出）。**做别的节奏时优先用这几个**，别自己拍。
        /// ⚠️ `fourSecDelay` **名字说 4、实测是 3.0**（10 个里唯一不符的）。
        /// </summary>
        public static readonly float[] EngineBeats =
        {
            0.1f,   // minDelay
            0.2f,   // twoFractDelay
            0.3f,   // sec3FractionDelay
            0.5f,   // sec5FractionDelay
            0.8f,   // sec8FractionDelay
            1.0f,   // oneSecDelay
            2.0f,   // twoSecDelay
            2.5f,   // twoHalfSecDelay
            3.0f,   // threeSecDelay（`fourSecDelay` 实测也是 3.0）
        };
    }
}
