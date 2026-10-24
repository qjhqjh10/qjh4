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
        //     `DurationOf` 的 `switch` 里加 `case`；零时长 → 加进 `ZeroDurationKinds`）。
        //  ⚠️ `ShowCardsWindow` **显式登记成零时长**（原版那一拍**非阻塞**：`FinishResolvingAction`
        //     紧跟其后、无 `yield`，见 `BattleDriver.FireEventAll` 里 `OpenShowCardsWindow` 那段判据）
        //     —— ⛔ 别让它掉进「未登记」，那会把一条**本来正确**的 0 s 变成噪音。
        // ==================================================================

        /// <summary>🆕 2026-10-24（`A2`）：**明确登记为「不占时间线（`DurationOf` = 0）」的事件种类**。
        /// 每一档都写清为什么是 0 —— 新加一档照这个格式补，⛔ 别默默依赖 `default: 0`。</summary>
        static readonly EvtKind[] ZeroDurationKinds =
        {
            EvtKind.Play,           // 出牌本身不占（登场位移由紧随的 `Deploy` 负责，见 `DurationOf(Deploy)`）
            EvtKind.Return,         // 离场位移是表现层补间（`CardFeel.ReturnToHand`），不进动作队列
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
        };

        /// <summary>🆕 2026-10-24（`A2`）：**在 `DurationOf(EvtKind)` 里【有值】的事件种类**
        /// （每一项都在下面的 `switch` 里有自己的 `case`）。加值时**两处一起改**。
        /// ⚠️ `Attack` / `Deploy` / `Retaliation` / `Draw` / `Ability` 的**精确值**还要看
        /// `DurationOf(BattleEvent)` 那个重载（按远近 / 来源 / 座位 / 卡分档），这里只登记「它有时间」。</summary>
        static readonly EvtKind[] TimedKinds =
        {
            EvtKind.Deploy, EvtKind.Attack, EvtKind.Hit, EvtKind.Death, EvtKind.Ability,
            EvtKind.Trigger, EvtKind.Draw, EvtKind.Heal, EvtKind.Retaliation,
        };

        /// <summary>自检用：`DurationOf` 里登记成「零时长」的那一档吗（不触发未登记告警）。</summary>
        public static bool IsZeroDurationKind(EvtKind kind)
        {
            for (int i = 0; i < ZeroDurationKinds.Length; i++) if (ZeroDurationKinds[i] == kind) return true;
            return false;
        }

        /// <summary>自检用：这一档**登记过**吗（`ZeroDurationKinds` 或 `TimedKinds`）。
        /// 未登记 ⇒ 三处 `default:` 会出声 + `IsSourced` 回 `false`。</summary>
        public static bool IsRegisteredEventKind(EvtKind kind)
        {
            if (IsZeroDurationKind(kind)) return true;
            for (int i = 0; i < TimedKinds.Length; i++) if (TimedKinds[i] == kind) return true;
            return false;
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

                // 🔴 **2026-10-23（P2 · 演出时序 · `新账-78②`）：这个数的【出处已经倒了】，但值暂时不动**
                //   （判据不齐 ⇒ ⛔ 别猜着改）。原出处写的是 `Impact Light Tween` 的 Punch 0.5 + ResetBody 0.25
                //   （`appendType=After`），而 `CardFeel` 的 `B02`（2026-10-22）已判定
                //   **`Impact Light/Heavy Tween` 是【远程弹着 AnimFX 模块】**（90 件 `BulletImpact_*` 引用它），
                //   **不是近战挨打** ⇒ 拿它当 `Hit` 拍的长度=**机制错位**，这条出处**自相矛盾**。
                //   候选真值 = `CardScript._ReceiveDamage_d__381__MoveNext.c:342` 末尾那句
                //   `WaitForSeconds(DAT_1834b2bb8)`（`.rdata` 4 字节实读 = **1.0**），但它被构造参数
                //   `+0x34` 这个 bool 把着（`_ResolveAttack:993` 的调用点传 `ReceiveDamage(dmg, 0, 0)` = 双 false
                //   ⇒ 会走到那句），**那个 bool 的语义还没查清** ⇒ 0.75 → 1.0 这一步**判据未闭合**。
                //   ⚠️ 改这个数会动**整条事件节奏基线**（要重跑 `BattleScene.Run` 重定基线）⇒ 与查清语义一起做。
                case EvtKind.Hit: return CardFeel.HitRotDuration + CardFeel.ResetDuration;

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

                // 🆕 2026-10-22（`W64` · 反击演出）：**只覆盖远程档**（原版 `_ResolveAttack…:1119-1131` ——
                //   远程打法守方才 `ResolveAttackRangedAnim`；近战那一支 `:1132-1150` 守方什么都不播 ⇒ 0）。
                //   本重载**拿不到 `Ranged` / 卡** ⇒ 回「远程档的兜底」（前摇 + `RangedVfxFallback`），
                //   ⛔ 别把它当成生产值：生产走 `DurationOf(BattleEvent)` → `RetaliationHoldOf`。
                case EvtKind.Retaliation: return AttackStepRanged + RangedVfxFallback;

                // 🆕 2026-10-24（`A2` · `J2` 顺手发现 3）：**不再一把兜住** —— 只有登记成
                // 「零时长」的那一档才静默回 0；没登记的**出声 + 计数**（见 `ZeroDurationKinds` 上面那段）。
                default:
                    if (IsZeroDurationKind(kind)) return 0f;
                    ReportUnregisteredKind(kind, "DurationOf(EvtKind)");
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
            if (e.Kind == EvtKind.Attack)
                return e.Ranged ? AttackStepRanged : AttackStepMelee;
            // 🔴 **2026-10-21（W61 第 2 批 · 第 3 条）**：`Deploy` **按「怎么进来的」分档** ——
            //    判据见 <see cref="DeployHoldOf"/>。⛔ 别退回 `DurationOf(EvtKind.Deploy)`（那是兜底 1.3，
            //    手牌出牌会被它多排 0.9 s —— 正是这条账要修的那 0.9）。
            if (e.Kind == EvtKind.Deploy) return DeployHoldOf(e.DeployFrom);
            // 🆕 `A1767`：**发动能力 = 逐卡真值**。`e.CardId` 装的是 `CardDef.Name`（英文卡名），
            //    表里 id 与卡名两个键都有（见 `AbilityAnimDurations.TryGet`）。
            //    查不到 ⇒ 退回 `AbilityFallbackSeconds`，`TryGet` 自己会**出声**（每张卡一次，不刷屏）。
            if (e.Kind == EvtKind.Ability)
            {
                float s;
                if (AbilityAnimDurations.TryGet(e.CardId, out s)) return s;
                return AbilityFallbackSeconds;
            }
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
        /// `false` 只剩**三种**：`Death` · `Ability` · **未登记**（后者出声）。</para>
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
                //    错因：`IsRegisteredEventKind`（本文件 `:263`，登记表**两张都查**）就在旁边，这里却只查了一张。
                //    ⛔ 别写回「只查零时长表」—— 那会让登记表与查表人口径不一致（自相矛盾的两条断言一绿一红）。
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
                // 🔴 **2026-10-23（P2 · `新账-78②`）就地订正（铁律 5）**：原文 = 「`Impact Light Tween`：
                //   Punch 0.5 + ResetBody 0.25」—— **那个出处已经不成立**（`Impact Light/Heavy Tween` 是
                //   **远程弹着** AnimFX 模块，`CardFeel` `B02` 判的；近战挨打只有 `DOPunchPosition` 一句）。
                //   现行值 0.75 **仍是我们自己的**（= `HitRotDuration + ResetDuration`，**出处待补**）。
                //   候选 = `_ReceiveDamage:342` 的 `WaitForSeconds(1.0)`（被 `+0x34` bool 把着，语义未查）。
                case EvtKind.Hit: return "⚠️ **出处待补**（旧出处 `Impact Light Tween` 已被 `CardFeel` `B02` 证伪"
                                       + "：那是远程弹着模块）—— 现值 = `CardFeel.HitRotDuration` 0.5 + "
                                       + "`CardFeel.ResetDuration` 0.25 = 0.75；候选真值 = "
                                       + "`CardScript._ReceiveDamage_d__381:342` 的 `WaitForSeconds(1.0)`"
                                       + "（被 `+0x34` 的 bool 把着，**语义未查清** ⇒ 判据未闭合，⛔ 别猜）";
                case EvtKind.Ability: return "🆕 2026-10-14（`A1767`）**逐卡真值**：该卡 `activeAbility` 各 CardAnim 的 "
                                             + "`AnimInfo.GetAnimDuration()`（`vfxDelayTime + timeAtStartPos + timeMoving "
                                             + "+ timeAtEndPos`）之和 → `AbilityAnimDurations`（**现读 93 张卡**）；"
                                             + "**只按 `EvtKind` 的这条重载拿不到卡** ⇒ 返回兜底 "
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
                // 🆕 2026-10-24（`A2`）：登记成零时长的那一档照旧回「（无）」；**未登记**的出声 + 明写。
                default:
                    if (IsZeroDurationKind(kind)) return "（无）";
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
