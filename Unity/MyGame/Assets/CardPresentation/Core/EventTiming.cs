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
using RuleEngine;

namespace CardPresentation
{
    public static class EventTiming
    {
        /// <summary>
        /// 上一条事件播完到这一条开始，隔多久（秒）。
        /// `prev == null` = 这一串里的第一条。
        /// </summary>
        public static float DelayBetween(BattleEvent prev, BattleEvent next)
        {
            if (next == null) return 0f;

            // 一串里的第一条：出手要先**抬刀**
            if (prev == null) return next.Kind == EvtKind.Attack ? AttackWindUp : 0f;

            switch (next.Kind)
            {
                // **命中那一下 → 真正扣血**（不是「弹道飞行」—— 飞行那一段已经算在起手→命中里了）。
                // 近战 0.30（`_AttackMeleeAnim…:258,260`）· 远程 = 该 VFX 的 `AnimInfo.GetAnimDuration()`
                // （`_ResolveAttackRangedAnim…:122-124`；没有全局常数，填死 1.0）。见两个常量的注释。
                case EvtKind.Hit:
                    if (prev.Kind != EvtKind.Attack) return 0f;
                    return prev.Ranged ? RangedFlight : MeleeImpactLag;

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

                default: return 0f;
            }
        }

        /// <summary>这条事件「占」多久（秒）—— 下一个环节要等这么久。出处见每条后面</summary>
        public static float DurationOf(EvtKind kind)
        {
            switch (kind)
            {
                // `Summon Troop Tween`：ScaleTween d=0 + **DelayTween d=1.0** + ResetTween 0.3
                case EvtKind.Deploy: return 1.0f;

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

                // 技能：`Mutation` 0.5 / `Execution_BL` 1.5 / `Vanguard` 1.2 / `Hammer Slam` 1.5 → 取中
                case EvtKind.Ability: return 1.0f;

                // 触发：见 `TriggerLen`（有出处，但那两个常量是引擎的**通用节拍**，不是 Trigger 专用）
                case EvtKind.Trigger: return TriggerLen;

                default: return 0f;
            }
        }

        /// <summary>带事件的重载 —— **只有攻击要分远近两档**（起手→命中：近战 0.10 / 远程 0.20），
        /// 其余一律转发给 <see cref="DurationOf(EvtKind)"/>。
        /// 为什么要这个重载：`DurationOf(EvtKind)` 拿不到 `BattleEvent.Ranged`，
        /// 而原版这两档**是两个不同的秒数**（见 `AttackStepMelee` / `AttackStepRanged` 的出处）。
        /// **别退回「一刀切」** —— 那会让远程的出手比原版快一倍。</summary>
        public static float DurationOf(BattleEvent e)
        {
            if (e == null) return 0f;
            if (e.Kind == EvtKind.Attack)
                return e.Ranged ? AttackStepRanged : AttackStepMelee;
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

        /// <summary>远程档的起手→命中（`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:84,96,102`）。</summary>
        public const float AttackStepRanged = 0.2f;

        /// <summary>**命中那一下到真正扣血**之间的空档。
        /// 出处：`CardScript._AttackMeleeAnim_d__357__MoveNext.c:258,260`（近战 0.30）·
        /// `_ResolveAttackRangedAnim_d__360__MoveNext.c:122-124`（远程 = 该 VFX 的 `AnimInfo.GetAnimDuration()`）。
        /// 远程**没有全局常数**（随 VFX 变，124 条 `Atk_*` 里众数 1.0 / 34 条）⇒ 填死用 1.0。</summary>
        public const float RangedFlight = 1.0f;
        public const float MeleeImpactLag = 0.3f;

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

        /// <summary>触发效果占多久。出处：`sec5FractionDelay = 0.5`（同上，引擎通用节拍）</summary>
        public const float TriggerLen = 0.5f;

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
                case EvtKind.Deploy: return "`Summon Troop Tween` 的 DelayTween duration=1.0";
                case EvtKind.Attack: return "`CardScript.attackStepTime`=0.1（位移完成即命中帧；远程 0.2）";
                case EvtKind.Hit: return "`Impact Light Tween`：Punch 0.5 + ResetBody 0.25（`appendType=After`）";
                case EvtKind.Ability: return "Mutation/Execution_BL/Vanguard/Hammer Slam 取中";
                case EvtKind.Trigger: return "`sec5FractionDelay`=0.5（引擎通用节拍，非 Trigger 专用）";
                case EvtKind.Death: return "`VarsGlobal.deathTimeMinionDuration`=0.2（督军那档 0.5 见 `CardFeel.DeathDissolve`）";
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
