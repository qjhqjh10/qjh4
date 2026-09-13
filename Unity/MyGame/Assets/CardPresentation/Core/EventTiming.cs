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
                // 远程攻击：**弹道要飞一段**才命中。出处：`card_anim_map` 里
                // `Atk_BulletImpact_*` 的 `timeAtStartPos`（非 0 的有 137 条，范围 0.2–1.5）。
                // 取 0.75（`Atk_BulletImpact_Eldar_Deathspinner` 就是 0.75）。
                case EvtKind.Hit:
                    return (prev.Kind == EvtKind.Attack && prev.Ranged) ? RangedFlight : 0f;

                // 阵亡：链路查到了（`_DestroyUnitsAfterBattleEnd_d__392__MoveNext.c` 里
                // `UnitDeath` 之后 `WaitForSeconds(VarsGlobal.deathTimeMinionDuration + …)`），
                // **但 `VarsGlobal` 资产不在解包数据里** → 具体秒数**查不到**，这是估的。
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

                // ⚠️ **2026-09-13 更正**：原来这里只写 `Recoil Normal Tween` 的 punch duration = 0.3。
                //    漏了出手前的**蓄力** —— 卡预制体 `MonoBehaviour_1744609728290659264.json` 的
                //    `timeToChargeAttack = 0.35`（配套 `chargeAttackAngle -10°` / `chargeBackModifier 0.5` /
                //    `chargeUpModifier 0.35`，都由 `CardFeel.Charge` 用上了）。
                //    出手 = 蓄力 0.35 + 冲一下 0.3 = **0.65**。
                case EvtKind.Attack: return CardFeel.ChargeTime + CardFeel.AttackPunchDuration;

                // `Impact Light Tween`：Punch 0.3(After) + Punch 0.5(Same，与上一条重叠)
                // + ResetBody 0.25(After) → **0.3 与 0.5 取长的 0.5，再接 0.25 的复位 = 0.75**。
                // ⚠️ 2026-09-13 更正：这里原来返回 **0.5**，但上一行的注释自己就写着还有一条
                //    `ResetBodyTween`。`appendType` 在数据里是 `After(0)`/`Same(5)` ——
                //    复位那一条是 `After`，所以它**是串在后面**的，不是重叠。
                case EvtKind.Hit: return CardFeel.HitRotDuration + CardFeel.ResetDuration;

                // **拍的**：原版没有通用阵亡 tween（只有 `EC Heldrake Dissapear UP`、
                // `Tyranid_Burrow_Tween` 两个单体专用），而 `VarsGlobal` 又缺 → 0.4 是估的
                case EvtKind.Death: return 0.4f;

                // 技能：`Mutation` 0.5 / `Execution_BL` 1.5 / `Vanguard` 1.2 / `Hammer Slam` 1.5 → 取中
                case EvtKind.Ability: return 1.0f;

                // 触发：见 `TriggerLen`（有出处，但那两个常量是引擎的**通用节拍**，不是 Trigger 专用）
                case EvtKind.Trigger: return TriggerLen;

                default: return 0f;
            }
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

        /// <summary>
        /// 远程弹道飞行时间 —— **半有出处**：`card_anim_map` 的 `timeAtStartPos` 范围是 0.2–1.5，
        /// 取 0.75 是挑了 `Atk_BulletImpact_Eldar_Deathspinner` 的值。
        /// ⚠️ 「Attack 之后多久 Hit」原版**没有独立等待值** —— 它隔着
        /// `AttackMeleeAnim` / `ResolveAttackRangedAnim` 两个协程的完整时长，
        /// 而那两段的 `MoveNext` **没被反编译**，秒数查不到。
        /// </summary>
        public const float RangedFlight = 0.75f;

        /// <summary>**拍的**：命中和阵亡之间那一下（原版的 `VarsGlobal` 资产缺失，查不到）</summary>
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
        /// 这条数值**有没有原版出处**。`Death` 是唯一的 false ——
        /// 它的链路查到了但 `VarsGlobal` 资产缺失，秒数只能是估的。
        /// </summary>
        public static bool IsSourced(EvtKind kind) { return kind != EvtKind.Death; }

        /// <summary>这条数值的出处（自检报数用）。**以「拍的」结尾的就不是原版的数**</summary>
        public static string SourceOf(EvtKind kind)
        {
            switch (kind)
            {
                case EvtKind.Deploy: return "`Summon Troop Tween` 的 DelayTween duration=1.0";
                case EvtKind.Attack: return "卡预制体 `timeToChargeAttack`=0.35 + `Recoil Normal Tween` 的 PunchTween 0.3";
                case EvtKind.Hit: return "`Impact Light Tween`：Punch 0.5 + ResetBody 0.25（`appendType=After`）";
                case EvtKind.Ability: return "Mutation/Execution_BL/Vanguard/Hammer Slam 取中";
                case EvtKind.Trigger: return "`sec5FractionDelay`=0.5（引擎通用节拍，非 Trigger 专用）";
                default: return "**拍的** —— `VarsGlobal` 资产缺失，查不到";
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
