// AchievementTally.cs — **一局**的成就计数（原版 `challenge` 的触发源在本地的等价物）
//
// ============================================================================
//  这是什么 / 为什么单开一个文件（2026-10-25 · 执行代理 `X2`）
// ============================================================================
//  判据与施工单 = `资料/待办判据_成就计数器_1025b.md` §B-2 / §B-3。
//  97 条成就里能数到的那些，原始信号散在攻击结算 / 事件流 / 出牌两跳上；
//  只在**容器里累加**、到结算时一次读走（`BattleDriver.AwardAchievements`）。
//
//  🔴 **纪律三条**（照抄 `BattleContext.DamageToEnemy` 那一族的原话）：
//    ① **只累加、不参与任何规则判定** —— 引擎的胜负 / 数值与它无关；
//    ② 下标一律 `seat * 步长 + 槽位`，⛔ 不要散成二十个 int；
//    ③ **归谁算**：伤害记给「**打出去的那一方**」（`1 - 被打单位的主人`）——
//       与 `BattleContext.DamageToEnemy` **同一条口径**（打自己人不记「对敌伤害」）。
//
//  ⚠️ **中途退出 / 投降不记**（原版每拍 `TryUpdateSave` 上传，我们只在结算写一次 ——
//     有意偏离，见判据 §B-2 最后一段）。
//  🔴 **它不是存档**：计数活一局，落盘那一层是 `ProfileData`（`profile.json` 的 `achIds/achValues`）。
namespace RuleEngine
{
    /// <summary>
    /// 「这一下伤害算哪一类」—— 原版挂在 `DamageInfo` 上（`damageType(+0x1c)` +
    /// `dealtByShuriken / dealtBySentry / dealtByMarkerlight` 三个 bool），
    /// 我们**没有**那一格 ⇒ 由**调用点**在 `ApplyDamage` 上打标（缺省 `Unknown` = 不计）。
    ///
    /// <para>取值与成就的对应（判据 = 成就定义 SO 的 `challenge.data`，逐条现读）：</para>
    /// <list type="bullet">
    ///   <item>`Melee` ⇒ `Ach_2`（`attackType=1` = `AttackTypes.Melee`）</item>
    ///   <item>`Ranged` ⇒ `Ach_3`（`attackType=2`）</item>
    ///   <item>`Spell` ⇒ `Ach_4`（`attackType=3` = `AttackTypes.Spell`，即**效果伤害**那几条路）</item>
    ///   <item>`Shuriken` ⇒ `Ach_7`（`onlyDamageByShuriken=1`）</item>
    ///   <item>`Markerlight` ⇒ `Ach_53`（`onlyDamageByMarkerlight=1`）</item>
    ///   <item>`Stomp` ⇒ `Ach_86`（`damageType=11` = `DamageType.Stomp`）</item>
    ///   <item>`Blast` / `Sentry` ⇒ **今天没有成就用**，但**照样打标** —— 判据 §B-3 #1 要求
    ///         「8 个调用点逐个打标」，将来有成就（或调试）时不用回来找。</item>
    /// </list>
    /// ⚠️ `Unknown` = 「这一下**不属于任何一类**」（毒 / 不稳定 / 督军挨打…）⇒ **不计**，
    ///    ⛔ 别把未知当 `Melee` 兜底（那会静默多算一张成就）。
    /// </summary>
    public enum DamageKind
    {
        Unknown = 0,
        Melee = 1,
        Ranged = 2,
        Spell = 3,
        Shuriken = 4,
        Markerlight = 5,
        Stomp = 6,
        Blast = 7,
        Sentry = 8,
    }

    /// <summary>`CardPlayedCount` 那 5 条成就的**标签位**（`Tally.CardsPlayedByTag` 的槽位）。</summary>
    public static class CardPlayedTag
    {
        /// <summary>`Ach_12`：`withTrait=[5]` = `DefinedTrait.ephemeral`。</summary>
        public const int Ephemeral = 0;
        /// <summary>`Ach_54`：`onlyCountCompanions=1`（伴生**造出来**的那张牌被打出）。</summary>
        public const int Companion = 1;
        /// <summary>`Ach_87`：`keyword=35` = `Keyword.Beast`（= 我们数据里的 `Subtype == "Beast"`）。</summary>
        public const int Beast = 2;
        /// <summary>`Ach_89`：`spellType=260` = `CombatElixir`。</summary>
        public const int Elixir = 3;
        /// <summary>`Ach_89` 的第二格闸：`anyCardType=0` ⇒ **只看某种卡型**（原版那张卡是计谋）。</summary>
        public const int Tactic = 4;
        /// <summary>`Ach_97`：`withTrait=[610]` = `DefinedTrait.pack`（数据自相矛盾，见判据 §F4）。</summary>
        public const int Pack = 5;
        /// <summary>`Ach_89` 的**合取**标签：`spellType=260` **且** 卡型 = 计谋 —— 那条成就两格闸都要
        /// （`anySpellType=0` + `anyCardType=0`）。单纯取 `Elixir` 或 `Tactic` 都会**多算**
        /// （战术卡有 448 张、Elixir 只有 6 张 ⇒ 取 `Tactic` 会把全池计谋都算进去）。</summary>
        public const int ElixirTactic = 6;
    }

    /// <summary>
    /// 一局内的成就计数（见文件头）。**只有 `RuleCore` / `BattleContext` 往里写**，
    /// 读点只有结算那一个（`BattleDriver.AwardAchievements` → `ProfileData.ApplyAchievementTally`）。
    /// </summary>
    public sealed partial class AchievementTally
    {
        /// <summary>`DamageKind` 的取值个数（含 `Unknown`；数组按它分块）。</summary>
        public const int KindCount = 9;

        /// <summary>`AbilityTrigger` 的**数值上界** —— 原版枚举现值域 `0…730`
        /// （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` 全表逐条读过）。
        /// ⚠️ 按**数值**直接下标（不是按序号压缩）：这样加触发时不用回来改映射表。
        /// 超界 ⇒ **丢掉并出声一次**（⛔ 不静默写坏别的格子）。</summary>
        public const int AbilityMax = 800;

        /// <summary>`CardPlayedTag` 的个数。</summary>
        public const int TagCount = 7;

        /// <summary>`[打出去那一方的座位 * 9 + DamageKind]` —— 累加的是**实际伤害**
        /// （`RuleCore.ApplyDamageCore` 里扣盾 / 减伤 / 易伤拆分之后的 `actual`）。</summary>
        public readonly int[] DamageByKind = new int[2 * KindCount];

        /// <summary>`[座位 * AbilityMax + AbilityTrigger 数值]` —— 触发**次数**（不是目标数）。</summary>
        public readonly int[] TriggerByAbility = new int[2 * AbilityMax];

        /// <summary>`[座位 * TagCount + CardPlayedTag]` —— 打出**张数**（一张卡可以同时落多个标签）。</summary>
        public readonly int[] CardsPlayedByTag = new int[2 * TagCount];

        /// <summary>`[座位]` —— **造出来的隐秘**张数（`Ach_78` `CreateSecret`，判据 §B-3 #14）。
        /// <para>写点 = `EffectResolver` 里**全部三处**「造出隐秘」的落点（判据 = 它们各自对应原版
        /// `BroadcastSecretCreated` 的同一个函数）：① `DoCreate` 的 `hand` / `enemyhand` 支 ·
        /// ② `DoChooseCard` 的 `todeck` 支（`DA44 Secret Agenda`）· ③ 任务点满阈值造隐秘入牌库
        /// （`AddSecretToDeckQuestMilestone`）。归属一律 = **造牌的施放者**（与那三处广播同一条口径）。</para>
        /// <para>⚠️ 数的是「**造出来**」而不是「真进了手牌」—— 与那三处 `BroadcastWhen` 同一条边界
        /// （手牌满了被送墓地时事件照样发，我们也照样记）。</para></summary>
        public readonly int[] SecretsMade = new int[2];

        /// <summary>`[座位]` —— **造出来的破坏**张数（`Ach_65` `AddSabotageCard`，判据 §B-3 #14）。
        /// <para>写点 = `DoCreate` 的 `hand` / `enemyhand` 支（破坏按定义造到**敌方手上**，
        /// `decktop` 那一支不造它）。归属 = **造牌的施放者**（收牌方是敌方，⛔ 不是 `who`）。</para></summary>
        public readonly int[] SabotageMade = new int[2];

        /// <summary>`[座位]` —— **授予出去的猎杀标记**个数（`Ach_96` `GiveHuntsMark`，判据 §B-3 #12）。
        /// <para>写点 = `EffectResolver.ApplyOneGain` 的 `val &gt; 0 &amp;&amp; Keyword=="huntmark"` 那一支
        /// （与 `BroadcastKeywordEvent(GetsHuntMark)` **同一跳**）。归属 = **施放者 `owner`**
        /// —— 原版计数口 = `GiveHuntsMark__OnAddCardEffect.c`（`isKeyword &amp;&amp; buffType==0x4ec` 即
        /// 每加一次猎杀标记 +1，**按目标个数**，不是按「一次效果」）。</para></summary>
        public readonly int[] HuntMarksGiven = new int[2];

        /// <summary>`[座位]` —— **授予出去的黑暗契约**份数（`Ach_6` `GiveMarkOfChaos`，判据 §B-3 #13）。
        /// <para>写点 = `EffectResolver.GrantDarkPactCore` 里 `AddKeyword(DarkPact, 1)` 之后那一跳。
        /// 归属 = **施放者 `owner`** —— 原版计数口 = `GiveMarkOfChaos__OnAddCardEffect.c`
        /// （`isKeyword &amp;&amp; buffType==0x14a` 即每加一份 +1）。</para></summary>
        public readonly int[] MarksOfChaosGiven = new int[2];

        /// <summary>`[座位]` —— **再造回来的单位数**（`Ach_14` `UnitReanimated`，判据 §B-3 #15）。
        /// <para>写点 = `EffectResolver.DoReanimate` 的**两条支**（从场上残骸翻回 / 墓地退路），
        /// **每翻回一个 +1**。归属 = 施放者 `owner`。
        /// ⚠️ 原版的真判据（`UnitReanimated__OnUnitReanimated.c`）里还有一格 `unit+0x40 != 0`
        /// 的过滤 —— **那一格语义没解出来**（反编译产物撞名，见判据 §N2 同族问题）⇒ 我们按「翻了就算」
        /// 🔶 自己设计（非原版）：`ReanimatedCount` 按「翻了就算」计数 · 原版 `unit+0x40 != 0` 那格过滤语义没解出来（判据 §N2 同族问题） · TODO(谁定的)｜原注：实现，如实标为**近似**。</para></summary>
        public readonly int[] ReanimatedCount = new int[2];

        /// <summary>`[座位]` —— **虫群合并发生次数**（`Ach_10` `UnitSwarmCount`，判据 §B-3 #15）。
        /// <para>写点 = `RuleCore.SwarmMergeNow`（**合并真的发生**那一跳，紧跟 `FireTriggerAlways(Swarm)`）。
        /// ⚠️ 为什么放这里而不是 `TrySwarmMerge`（入队那一跳）：入队到出队之间棋盘可能变
        /// （载体会死 / 右邻没了 ⇒ `SwarmMergeNow` 会早返）⇒ **放在出队执行体才等于「真的合并了」**。
        /// ⚠️ 原版 `UnitSwarmCount__OnUnitSwarm.c` 的**方法体在反编译产物里撞名丢失**
        /// （文件里装的是 `UnitReanimated__OnUnitReanimated`）⇒ 「按广播算」还是「按合并算」**查不到**；
        /// 按调度台口径取后者（真的合并了才算），⛔ 别两处都加（会双计）。</para></summary>
        public readonly int[] SwarmMerges = new int[2];

        /// <summary>座位号是否合法（`0` / `1`）。</summary>
        static bool SeatOk(int seat) { return seat == 0 || seat == 1; }

        /// <summary>记一笔伤害。`dealerSeat` = **打出去的那一方**（不是被打的那个）。</summary>
        public void AddDamage(int dealerSeat, DamageKind kind, int amount)
        {
            if (!SeatOk(dealerSeat) || amount <= 0) return;
            int k = (int)kind;
            if (k <= (int)DamageKind.Unknown || k >= KindCount) return;   // `Unknown` 不计
            DamageByKind[dealerSeat * KindCount + k] += amount;
        }

        /// <summary>某一方某一类的伤害累计（给结算与自检读）。</summary>
        public int DamageOf(int seat, DamageKind kind)
        {
            if (!SeatOk(seat)) return 0;
            int k = (int)kind;
            if (k < 0 || k >= KindCount) return 0;
            return DamageByKind[seat * KindCount + k];
        }

        /// <summary>记一次能力触发。`ability` = 原版 `AbilityTrigger` 的**数值**
        /// （`CardDef.AbilityTriggerNumber(name)` 就是那个口）。</summary>
        public void AddTrigger(int seat, int ability)
        {
            if (!SeatOk(seat) || ability < 0) return;
            if (ability >= AbilityMax)
            {
                if (!_abilityOverflowWarned)
                {
                    _abilityOverflowWarned = true;
                    UnityEngine.Debug.LogWarning("[Tally] 能力触发数值 " + ability
                        + " 超出 `AchievementTally.AbilityMax`（" + AbilityMax + "）⇒ 这一次**没记**"
                        + "（⛔ 静默写坏别的格子更糟）。要支持它请把 `AbilityMax` 提上去。");
                }
                return;
            }
            // 🆕 **2026-10-27（批次 9 · 执行代理 `E7ENG`）：原版 `ChallengeLogMgr.LogTriggerAbility`
            //   对这三档**根本不发信号** ⇒ 我们的成就触发计数也不该记。**
            //   判据（🥇 `d:/2/tools/decomp_full/ChallengeLogMgr__LogTriggerAbility.c`，全文 19 行亲读）：
            //   ```
            //   if (((param_3 != 0x14) && (param_3 != 0xf0)) && (param_3 != 0x276)) {
            //       TriggerAbilitySignal ctor(...); Signal.Raise(...);      // ← 只有非这三档才发
            //   }
            //   ```
            //   ⇒ `0x14(20) WhileInPlay` / `0xf0(240) WhileInPlayVariable` / `0x276(630) TriggeredSynapse`
            //   这三档**整段跳过**。它们是「常驻 / 派生」触发（每次结算都要问一次），
            //   原版主动把它们摘出挑战计数 ⇒ 我们照抄，**这是「成就口径 = 挑战口径」的对齐、不是新发明**。
            //   ⚠️ 以前我们照单全记 ⇒ 把原版**永远不数**的那三条也计进去了（账 532 的 ③）。
            if (ability == TriggerWhileInPlay
                || ability == TriggerWhileInPlayVariable
                || ability == TriggeredSynapse) return;
            TriggerByAbility[seat * AbilityMax + ability]++;
        }
        static bool _abilityOverflowWarned;

        /// <summary>某一方某条 trigger 触发过几次。</summary>
        public int TriggerOf(int seat, int ability)
        {
            if (!SeatOk(seat) || ability < 0 || ability >= AbilityMax) return 0;
            return TriggerByAbility[seat * AbilityMax + ability];
        }

        /// <summary>记一张打出的牌（`tagMask` = `CardPlayedTag` 的位集合，一张卡可落多个标签）。</summary>
        public void AddCardPlayed(int seat, int tagMask)
        {
            if (!SeatOk(seat) || tagMask == 0) return;
            for (int i = 0; i < TagCount; i++)
                if ((tagMask & (1 << i)) != 0) CardsPlayedByTag[seat * TagCount + i]++;
        }

        /// <summary>某一方某个标签打出过几张。</summary>
        public int CardsPlayedOf(int seat, int tag)
        {
            if (!SeatOk(seat) || tag < 0 || tag >= TagCount) return 0;
            return CardsPlayedByTag[seat * TagCount + tag];
        }

        /// <summary>记一张**造出来的隐秘**（`Ach_78`）。</summary>
        public void AddSecret(int makerSeat) { if (SeatOk(makerSeat)) SecretsMade[makerSeat]++; }

        /// <summary>某一方造出过几张隐秘。</summary>
        public int SecretsOf(int seat) { return SeatOk(seat) ? SecretsMade[seat] : 0; }

        /// <summary>记一张**造出来的破坏**（`Ach_65`）。</summary>
        public void AddSabotage(int makerSeat) { if (SeatOk(makerSeat)) SabotageMade[makerSeat]++; }

        /// <summary>某一方造出过几张破坏。</summary>
        public int SabotageOf(int seat) { return SeatOk(seat) ? SabotageMade[seat] : 0; }

        /// <summary>记一份**授予出去的猎杀标记**（`Ach_96`）。</summary>
        public void AddHuntMark(int giverSeat) { if (SeatOk(giverSeat)) HuntMarksGiven[giverSeat]++; }

        /// <summary>某一方授予过几份猎杀标记。</summary>
        public int HuntMarksOf(int seat) { return SeatOk(seat) ? HuntMarksGiven[seat] : 0; }

        /// <summary>记一份**授予出去的黑暗契约**（`Ach_6`）。</summary>
        public void AddMarkOfChaos(int giverSeat) { if (SeatOk(giverSeat)) MarksOfChaosGiven[giverSeat]++; }

        /// <summary>某一方授予过几份黑暗契约。</summary>
        public int MarksOfChaosOf(int seat) { return SeatOk(seat) ? MarksOfChaosGiven[seat] : 0; }

        /// <summary>记一个**再造回来的单位**（`Ach_14`）。</summary>
        public void AddReanimated(int ownerSeat) { if (SeatOk(ownerSeat)) ReanimatedCount[ownerSeat]++; }

        /// <summary>某一方再造回来过几个单位。</summary>
        public int ReanimatedOf(int seat) { return SeatOk(seat) ? ReanimatedCount[seat] : 0; }

        /// <summary>记一次**虫群合并**（`Ach_10`）。</summary>
        public void AddSwarmMerge(int ownerSeat) { if (SeatOk(ownerSeat)) SwarmMerges[ownerSeat]++; }

        /// <summary>某一方虫群合并过几次。</summary>
        public int SwarmMergesOf(int seat) { return SeatOk(seat) ? SwarmMerges[seat] : 0; }

        // ================================================================================================
        //  🆕 2026-10-27（批次 9 · 执行代理 `E7ENG`）—— **原版 `LogTriggerAbility` 不发信号的三档**
        //  （追加在类尾；判据与后果 → `AddTrigger` 里那一跳的注释，⛔ 别在这里抄第二份）
        // ================================================================================================

        /// <summary>`AbilityTrigger.WhileInPlay = 20`（常驻）—— 原版 `ChallengeLogMgr.LogTriggerAbility`
        /// 用 `param_3 != 0x14` 把它**挡在信号之外** ⇒ 成就不记它。</summary>
        public const int TriggerWhileInPlay = 20;

        /// <summary>`AbilityTrigger.WhileInPlayVariable = 240`（`0xf0`）—— 同上，原版不记。</summary>
        public const int TriggerWhileInPlayVariable = 240;

        /// <summary>`AbilityTrigger.TriggeredSynapse = 630`（`0x276`）—— 同上，原版不记。</summary>
        public const int TriggeredSynapse = 630;

        // ================================================================================================
        //  🆕 2026-10-28（B88 · 波 2 · 执行代理 `A2`）—— **挑战/成就缺的两格发射端**（追加在类尾）
        //  判据 = `资料/完全复刻/判据/B88_R5_挑战计数发射点_施工单.md` §4·1（原版 `ChallengeLogMgr` 10 个
        //  计数口里，我们**缺发射**的只有「击杀」与「治疗」两条；其余 8 条早有等价格子）。
        //  ⚠️ 与 `Shell/ChallengeData.cs` 那五格的关系：**这里才是唯一正本**（引擎侧），
        //     表现层在**结算处**读出喂 `ChallengeData`（照既有 `DailyData.OnBattleEnd` / `AwardAchievements`
        //     的先例）—— ⛔ 别在 `RuleEngine/` 里插 `CardPresentation.ChallengeEngine.*`（反向依赖）。
        // ================================================================================================

        /// <summary>`[凶手座位]` —— **击杀的部队数**（原版 `PlayerBattleStats.enemyTroopsDied`，`+0x10`）。
        /// <para>写点 = `RuleCore.TryCreditKillCore`（`RuleCore.cs` 的 `return true;` 之前那一句）。
        /// 归属 = **凶手**（`kOwner`）—— 原版 `TroopsKilled` 按**死者**选边写进**凶手那侧**的 stats
        /// （`ChallengeLogMgr__TroopKilled.c` 尾：`deadCard+0x40 == 0` ⇒ `PlayerBattleStats`），
        /// 而消费者 `TroopsKilledCount` 只看 `killer.isPlayer` ⇒ 两条口径**同值**。</para>
        /// <para>⚠️ 覆盖差（如实记）：原版 `ResolveMinionDeath` 每个小兵死都发信号，无凶手时
        /// 消费者自身会在 `null.isPlayer` 上抛 NRE ⇒ **原版那一路也不计数**；我们只数「有凶手且凶手
        /// 是当前行动方」那一档（与消费者等价，但不是逐字等价的信号流）。</para></summary>
        public readonly int[] TroopsKilled = new int[2];

        /// <summary>`[被治疗方座位]` —— **实际治疗量**（原版 `ChallengeVariable`/`HealAmount` 那一格）。
        /// <para>写点 = `RuleCore.HealUnit`（`ctx.Emit(EvtKind.Heal)` 之后）。量 = 该方法算出的 `healed`
        /// （= `u.Health - before`，天生就是 `min(请求, MaxHealth−Health)` 的净量，与
        /// `HealAmount.cs:100-160` 的消费者公式逐字对上）。阵营过滤在消费者里，我们这里
        /// 用 `owner`（被治疗方的座位）承载 `t.isPlayer`。</para></summary>
        public readonly int[] HealDone = new int[2];

        /// <summary>记一次**击杀部队**（`TroopsKilledOf` 的对写口）。</summary>
        public void AddTroopKilled(int killerSeat) { if (SeatOk(killerSeat)) TroopsKilled[killerSeat]++; }

        /// <summary>某一方击杀过几个部队。</summary>
        public int TroopsKilledOf(int seat) { return SeatOk(seat) ? TroopsKilled[seat] : 0; }

        /// <summary>记一笔**实际治疗量**（`amount` ≤ 0 不记 —— 与 `AddDamage` 同一条闸）。</summary>
        public void AddHealDone(int ownerSeat, int amount)
        {
            if (!SeatOk(ownerSeat) || amount <= 0) return;
            HealDone[ownerSeat] += amount;
        }

        /// <summary>某一方（作为被治疗方）累计吃到过多少实际治疗量。</summary>
        public int HealDoneOf(int seat) { return SeatOk(seat) ? HealDone[seat] : 0; }
    }

    // ================================================================================================
    //  🆕 2026-10-28（B92 波 4 · 执行代理 `B7`）—— 两格「原版口径」的**加法**（追加在文件末 = 零行漂移）
    //  判据 → `资料/完全复刻/判据/B92_战斗判据_B7.md` §3·5 / §3·8；报告 → `报告/B92_B7b.md`
    //  🔴 本块**只加容器 + 取数口**、不接任何写点 ⇒ 今天的 `play` 输出与指纹**逐字节不变**。
    //     两个写点都在 `RuleCore`（B1 的文件），逐条落点见各自 doc。
    // ================================================================================================
    public sealed partial class AchievementTally
    {
        // ------------------------------------------------------------------------------------------
        //  ① 扣甲前的伤害（原版 `DamageDeltToEnemyUnit.OnDamageDelt` 数的就是这一份）
        // ------------------------------------------------------------------------------------------
        /// <summary>`[打出去那一方的座位 * <see cref="KindCount"/> + DamageKind]` —— **扣甲前**的伤害累计。
        /// <para>🔴 **判据（2026-10-28 现读）**：原版 `DamageDeltToEnemyUnit__OnDamageDelt.c:81` 记的是
        /// `DamageInfo.damageValue(+0x18)`，而护甲是在**被伤害方**的 `CardScript.ResolveDamageDealt` 里才减的
        /// —— `val = damageInfo.damageValue - 护甲; num3 = Math.Max(1, val)`（`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:14317`）；
        /// 且 `BattleManager.BroadcastUnitDamaged` **不看** `damageAbsorbed` 就发 `LogDamage` 信号
        /// （dev `BattleManager.cs:24516-24527`）⇒ 原版数的是「打到身上的名义伤害」，**含被护甲/盾/absorbed 吃掉的那部分**。</para>
        /// <para>⚠️ 与 <see cref="DamageByKind"/> 的关系：那一格是**扣甲后**的 `actual`（现役口径），本格是原版口径
        /// —— **二选一**，⛔ 别让同一个消费者同时读两格（会双计）。切换两步：
        /// ① 写点 = `RuleCore.ApplyDamageCore`（现读 `RuleCore.cs:8946` 的 `AddDamage(1 - _own, kind, actual)`
        /// ⇒ 改调本方法、传**扣甲前**那一份量 + **攻方**座位）；② 消费点 = `BattleDriver.AwardAchievements` 改读
        /// <see cref="DamageRawOf"/>。⚠️ 原版还要求「absorbed 也计入」，而 `ApplyDamageCore` 在
        /// 盾 / 闪避 / 无敌 / 空投舱那几支是**早退 `return 0`**（到不了记账口）⇒ ① 那一步要顺带把早退支改成「先记再退」。</para></summary>
        public readonly int[] DamageByKindRaw = new int[2 * KindCount];

        /// <summary>记一笔**扣甲前**的伤害（`dealerSeat` = **打出去的那一方**）。闸与 <see cref="AddDamage"/> 逐条相同
        /// （座位不合法 / `amount &lt;= 0` / `Unknown` 一律不计）。</summary>
        public void AddDamageRaw(int dealerSeat, DamageKind kind, int amount)
        {
            if (!SeatOk(dealerSeat) || amount <= 0) return;
            int k = (int)kind;
            if (k <= (int)DamageKind.Unknown || k >= KindCount) return;   // `Unknown` 不计（同 AddDamage）
            DamageByKindRaw[dealerSeat * KindCount + k] += amount;
        }

        /// <summary>某一方某一类**扣甲前**的伤害累计（原版口径的取数口）。⚠️ 传**本地玩家座位**而不是写死 0
        /// ≠ 原版那道 `signal.IsPlayer` 闸（`DamageDeltToEnemyUnit__OnDamageDelt.c:39-41`）—— 只在消费侧生效。</summary>
        public int DamageRawOf(int seat, DamageKind kind)
        {
            if (!SeatOk(seat)) return 0;
            int k = (int)kind;
            if (k < 0 || k >= KindCount) return 0;
            return DamageByKindRaw[seat * KindCount + k];
        }

        // ------------------------------------------------------------------------------------------
        //  ② 花掉的能量**按类型分格**（原版 `EnergySpentSignal.Type` 那一维）
        // ------------------------------------------------------------------------------------------
        /// <summary>原版 `EnergyCostType.CardCost = 1`：**打出卡**付的那一份（`BattleManager__PayCardCost.c:34-38`）。</summary>
        public const int EnergyCostCard = 1;

        /// <summary>原版 `EnergyCostType.AbilityCost = 2`：**主动能力**付的那一份（`BattleManager__PayActiveAbilityCostOath.c:35`）。</summary>
        public const int EnergyCostAbility = 2;

        /// <summary>`[座位 * 2 + 槽位]` —— 花掉的能量；槽位 0 = <see cref="EnergyCostCard"/>、1 = <see cref="EnergyCostAbility"/>。
        /// <para>🔴 **判据（2026-10-28 现读）**：`EnergySpentSignal` 四格 = `IsPlayer(+0x10) / Card(+0x18) /
        /// Cost(+0x20) / Type(+0x24)`（`dump.cs:59332-59335`）；两个发起源分别传 **CardCost(1)**
        /// （`BattleManager__PayCardCost.c:34-38`）与 **AbilityCost(2)**（`BattleManager__PayActiveAbilityCostOath.c:35`）；
        /// 消费者 `EnergySpent.OnEnergySpent`（`EnergySpent__OnEnergySpent.c:14-40`）三道闸 =
        /// `signal.IsPlayer` ∧ `costCondition.Compare(Cost)` ∧ `costType(+0x60) 含 signal.Type(+0x24)` ⇒ `UpdateProgress(Cost)`。
        /// ⇒ 「花掉 N 点能量」类挑战**必须能按类型筛**，而我们原来只有「按座位累计一个总数」。</para>
        /// <para>⚠️ 写点在 B1 的文件里：`RuleCore.SpendNormalMana`（现读 `RuleCore.cs:13040`；出牌调用点 `:3930`、
        /// 主动能力 `:15396`；维护费 `:1826` 传 `energySpentSignal: false` —— 与原版两个发起源**都不含维护费**一致）。
        /// 需给它加 `costType` 形参、两处分别传 Card / Ability，由它调本方法。</para>
        /// <para>⚠️ 原版记 `EntityScript.get_CurrentCost(card)`（**付费之后重取**），`SpendNormalMana` 现在记实付 `paid`
        /// —— 在「付完会改费用」的卡上会分叉，切换时一并核。</para></summary>
        public readonly int[] EnergySpentByCostType = new int[2 * 2];

        /// <summary>记一笔花掉的能量。`costType` 只接受 <see cref="EnergyCostCard"/> / <see cref="EnergyCostAbility"/>
        /// （原版 `[Flags]` 枚举但信号只发单值 1 或 2）；其它值 ⇒ **不记 + 出声一次**（⛔ 并进别的格子更糟）。</summary>
        public void AddEnergySpent(int seat, int amount, int costType)
        {
            if (!SeatOk(seat) || amount <= 0) return;
            int slot;
            if (costType == EnergyCostCard) slot = 0;
            else if (costType == EnergyCostAbility) slot = 1;
            else
            {
                if (!_energyCostTypeWarned)
                {
                    _energyCostTypeWarned = true;
                    UnityEngine.Debug.LogWarning("[Tally] `AddEnergySpent` 收到未知 `costType = " + costType
                        + "`（原版 `EnergyCostType` 只发 CardCost(1) / AbilityCost(2)）⇒ 这一次**没记**。");
                }
                return;
            }
            EnergySpentByCostType[seat * 2 + slot] += amount;
        }
        static bool _energyCostTypeWarned;

        /// <summary>某一方在某一类上花掉多少能量（`costType` 同 <see cref="AddEnergySpent"/>）。
        /// ⚠️ 传**本地玩家座位**（原版 `EnergySpent.OnEnergySpent` 第一道闸就是 `signal.IsPlayer`），⛔ 别写死 0。</summary>
        public int EnergySpentOf(int seat, int costType)
        {
            if (!SeatOk(seat)) return 0;
            int slot = costType == EnergyCostCard ? 0 : (costType == EnergyCostAbility ? 1 : -1);
            return slot < 0 ? 0 : EnergySpentByCostType[seat * 2 + slot];
        }

        /// <summary>某一方**两类合计**花掉多少能量（= 原版「不筛类型」那档挑战读的数）。</summary>
        public int EnergySpentTotalOf(int seat)
        {
            if (!SeatOk(seat)) return 0;
            return EnergySpentByCostType[seat * 2] + EnergySpentByCostType[seat * 2 + 1];
        }
    }
}
