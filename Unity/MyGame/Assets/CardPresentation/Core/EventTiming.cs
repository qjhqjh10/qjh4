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
//   表在 `Resources/ability_anim_durations.json`（🔴 **现读 62 张卡**；⚠️ 这里原写「56 张卡」已过期，
//   账 `A2095`，⛔ 别手抄这个数），入口 `Core/AbilityAnimDurations.cs`，
//   消费点 = 本文件的 `DurationOf(BattleEvent)`（⚠️ **只按 `EvtKind` 的那条重载给兜底**）。
//   ⛔ 改这一格之前先读那两个文件的文件头（含「只覆盖 2022 快照」等三处如实标注）。
using RuleEngine;

namespace CardPresentation
{
    public static class EventTiming
    {
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

                default: return 0f;
            }
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

                // `Impact Light Tween`：Punch 0.3(After) + Punch 0.5(Same，与上一条重叠)
                // + ResetBody 0.25(After) → **0.3 与 0.5 取长的 0.5，再接 0.25 的复位 = 0.75**。
                // ⚠️ 2026-09-13 更正：这里原来返回 **0.5**，但上一行的注释自己就写着还有一条
                //    `ResetBodyTween`。`appendType` 在数据里是 `After(0)`/`Same(5)` ——
                //    复位那一条是 `After`，所以它**是串在后面**的，不是重叠。
                case EvtKind.Hit: return CardFeel.HitRotDuration + CardFeel.ResetDuration;

                // **拍的**：原版没有通用阵亡 tween（只有 `EC Heldrake Dissapear UP`、
                // `Tyranid_Burrow_Tween` 两个单体专用），而 `VarsGlobal` 又缺 → 0.4 是估的
                case EvtKind.Death:
                    // 🔴 2026-09-18：原版**有**字段（`VarsGlobal.deathTimeMinionDuration = 0.2`），
                    //    原来那个 0.4 是拍的。我们这条就是**消散动画的时长**（`CardFeel.Dissolve`），
                    //    ⇒ 用同一个判据（`CardFeel.DeathDissolve`）—— 督军那档 0.5 由消散那侧自己带。
                    return CardFeel.DeathDissolveMinion;

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

                default: return 0f;
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
        /// 新的逐卡表（`Resources/ability_anim_durations.json`，56 张卡）**中位也恰好是 1.0**
        /// ⇒ 两代口径在这一格上偶然相等 —— 所以「兜底取 1.0」不是拍脑袋，**但也不能当成原版值**。
        /// ⚠️ `AbilityAnimDurations.FallbackSeconds`（表里现读的那个数）**必须等于本常量** ——
        ///    自检会逐字比（`BattleScene.Run` 那条「兜底值与表一致」）。
        ///
        /// 🔴 **为什么这条重载一定拿不到逐卡值**：原版判据是「**那条技能自己的** `AnimInfo` 四字段之和」
        /// （`AnimInfo.GetAnimDuration` = `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`），
        /// 而「哪条技能」只存在于**事件**里（`BattleEvent.CardId`）。`DurationOf(EvtKind)` 的签名里没有卡
        /// ⇒ 它只能给兜底值。**两条路都要跑到才算接完**，缺一条就会「看着接上了、其实一半还是常数」。
        /// 判据链全文 → `Core/AbilityAnimDurations.cs` 文件头（含三处如实标注：只覆盖 2022 快照、
        /// tween 那一段没算、`cardAbilities` 那一族不进本表）。</summary>
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
        ///    动画那一侧**没有**这个问题（`CardFeel.DealSeconds(false)` / `DealInQuad` 用的就是 0.15）。</summary>
        public const float DrawDeal = CardFeel.DealDuration;

        /// <summary>
        /// 这条数值**有没有原版出处**。
        /// 🔴 **2026-09-18 更正**：原来写「`Death` 是唯一的 false —— `VarsGlobal` 资产缺失」——
        ///    **那句话过期了**：`VarsGlobal` 2026-09-17 整表解出，`Death` 现在**有出处**
        ///    （小兵 `deathTimeMinionDuration 0.2` / 督军 `deathTimeWarlordDuration 0.5`）。
        /// ⇒ 现在**每一条都有出处**（`EventTiming.SourceOf` 会逐条打出来）。
        /// </summary>
        public static bool IsSourced(EvtKind kind) { return true; }

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
                case EvtKind.Hit: return "`Impact Light Tween`：Punch 0.5 + ResetBody 0.25（`appendType=After`）";
                case EvtKind.Ability: return "🆕 2026-10-14（`A1767`）**逐卡真值**：该卡 `activeAbility` 各 CardAnim 的 "
                                             + "`AnimInfo.GetAnimDuration()`（`vfxDelayTime + timeAtStartPos + timeMoving "
                                             + "+ timeAtEndPos`）之和 → `AbilityAnimDurations`（56 张卡）；"
                                             + "**只按 `EvtKind` 的这条重载拿不到卡** ⇒ 返回兜底 "
                                             + $"{AbilityFallbackSeconds}（= 真值表中位数，与旧常数恰好相等）";
                case EvtKind.Trigger: return "`BattleManager.twoFractDelay`(**+0x4C8**) = 0.2 —— `_ResolveTriggerTraitAnim` 那条链的等待";
                case EvtKind.Death: return "`VarsGlobal.deathTimeMinionDuration`=0.2（督军那档 0.5 见 `CardFeel.DeathDissolve`）";
                case EvtKind.Draw: return "`VarsGlobal.timeToDrawPlayerCard`=0.3（= `CardFeel.DealDuration`，发牌入场那条补间）"
                                        + "；⚠️ 只覆盖**我方档**（敌方 0.15 这一张表取不到座位号）";
                case EvtKind.Heal: return "`CardScript._Heal_d__468__MoveNext.c:204` 的 `WaitForSeconds(1.5)`"
                                        + "（= `CardView.HealTextHoldSeconds`；`ResolveHeal` 每目标 `yield return` 等它跑完 "
                                        + "⇒ 多目标治疗严格串行，`BattleManager._ResolveHeal_d__491__MoveNext.c:114-147`）";
                case EvtKind.AmbushExit: return "**占 0**（原版 `CardScript.SetAmbush(card,false)` 是普通方法、不阻塞，"
                                              + "紧跟着就是同一击的 `Hit`）；那件特效自己的材质补间 = "
                                              + "`AmbushEffect` 的 `MaterialTweenFloat(_Blend)` delay 0.35 + duration 1.0 = 1.35"
                                              + "（**不进时间线**；原件 = `新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/"
                                              + "MonoBehaviour/MonoBehaviour_6849782588473963486.json` 的 `references.RefIds[0].data`）";
                default: return "（无）";
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
