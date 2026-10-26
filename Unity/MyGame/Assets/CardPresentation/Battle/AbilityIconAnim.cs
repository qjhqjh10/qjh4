// AbilityIconAnim.cs — 原版 `BattleManager.PlayIconAnim`（入队）+ `ResolveAbilityIconAnim`（协程）那一层
//
// ═════════════════════════════════════════════════════════════════════════════
//  判据链（第一权威 = `d:/2/tools/decomp_full/`；ILSpy `out_cpp2il_dev_ilspy` 只用来核字段名）
// ═════════════════════════════════════════════════════════════════════════════
//  ① **入队** `BattleManager__PlayIconAnim.c:30-87`（ILSpy `BattleManager.cs:40608`）：
//       `PlayIconAnim(CardScript card, bool waitForAnim, AbilityTrigger trigger, int priority, bool block)`
//       · `if (card == null) throw`（`.c` 末 `FUN_1803f47a0` = 抛 `NullReferenceException`）；
//       · `BattleAction___ctor(&a, **0x15**, card.IsUnit(), card, block, 0)` —— `0x15` = 21 =
//         `BattleActionType.abilityIconAnim`（`.c:31-33`）；
//       · 结构拷贝后写两个字段：`waitForAnim = param_3`（动作内偏移 `+0x98`）、
//         `trigger = param_4`（`+0xE8`）（`.c:36-37`；ILSpy 那两个字段名 = `waitForAnim` / `trigger`）；
//       · `AddAutoActionToQueue(action, priority)`（`.c:86`）—— **只入队，不播**。
//  ② **出队 / 协程** `BattleManager__ResolveAbilityIconAnim.c:25-77` = 建状态机 `_d__475`、
//     把 manager 与 BattleAction 拷进去。真逻辑在
//     `BattleManager._ResolveAbilityIconAnim_d__475__MoveNext.c`（全文 60 行，逐句读）：
//       · `state 0`：`if (action.trigger == **0x1b8 = 440 = AbilityTrigger.Landing**)`
//           ⇒ `yield return manager.sec5FractionDelay`（字段 `+0x4B0`）；
//       · 然后：`if (action.actingCard != null) { card.ActivateIconAnim(trigger);
//           yield return manager.twoFractDelay; }`（字段 `+0x4C8`）；
//       · `state 2`：`FinishResolvingAction(action.actionType)`。
//     🔴 **两个时长的值是从二进制里读出来的**（`工具/read_literal.py` 对
//        `D:/2/unity_run_ref/GameAssembly.dll` 实读，`.c` 里构造 `WaitForSeconds` 的两个字面量）：
//        `0x1834b2bb4` = **0.5**（`+0x4B0`）· `0x1834b2bb0` = **0.2**（`+0x4C8`）
//        ⇒ 与字段名 `sec5FractionDelay` / `twoFractDelay` 自洽。**触发 440 ⇒ 总 0.7 s、其余 0.2 s。**
//  ③ **图标动画本体** `CardScript__ActivateIconAnim.c`（**全文 19 行**）：
//       `trait = CardTraitCollection.GetTraitFromAbilityTrigger(trigger)` →
//       `BattleCardUI.HighlightTraitIcon(ui, trait)` → `BattleCardUI.DisplayTriggerAnim(ui, trait, 1)`。
//       ⚠️ **它没有 `trait == 0` 早退**（对比 `CardScript__ActivateTriggerTraitAnim.c:6-9` 有那一句）
//          —— 差别只落在「trait 解不出」时多跑一次空查找，视觉上等价（原版那两个方法都按 trait id 比对，
//          0 匹配不到任何一位）。
//  ④ **trigger → trait** `CardTraitCollection__GetTraitFromAbilityTrigger.c` + ILSpy
//     `CardTraitCollection.cs:266`（**那份 ILSpy 把这个 switch 还原清楚了**）：
//       `Unstable(81) → unstable` · `ThisUnitDeath(80) → backlash` · `ThisCardPlayed(0) → rally`；
//       `default`: `Enum.TryParse<DefinedTrait>(lowerFirst(abilityTrigger.ToString()))`，失败 ⇒ `none`。
//       ⇒ **名字对名字**（`AbilityTrigger.PascalCase` → `DefinedTrait.camelCase`），不是数值表。
//       ⚠️ 我们这边**没有 `DefinedTrait` 这个 C# 枚举**（它在 `d:/2` 的桩里）⇒ 用「
//       `TraitFrames` 的绑定表里有没有这一位」当同一条判据（表 = 原版 `CardTrait` SO 的转写，
//       `Resources/trait_effect_bindings.json`）。表里没有 ⇒ 与原版 `none` 等价（**出声一次**，不静默）。
//
// 🔴🔴 **原版这两条在本 build 上是【死路】—— 照抄形状，不发明调用方**（2026-10-26 现查，两棵树都查过）：
//   · `PlayIconAnim` 在 **`decomp_full/*.c` 与 `out_cpp2il_dev_ilspy/*.cs` 里都【零调用点】**
//     （`grep -rl PlayIconAnim` 两棵树各只命中 `BattleManager` 自己那个定义/方法体）；
//   · `ResolveAbilityIconAnim` 同样零调用点（它是 action 分派那一支的产物，而那一支没被反编译出来）。
//   ⇒ 本 build 里 **kind 21 从来没有被入过队**，图标动画这一路**从不触发**。
//     本文件因此**不擅自接任何触发源**（⛔ 别把它挂到 `EvtKind.Trigger` 上 —— 那条链已经有
//     `BattleDriver.PlayTraitPulse`（= `ActivateTriggerTraitAnim` 那一支），挂上去就是**同一件事演两遍**）。
//     要做的是把**形状与参数**落地：宿主（`BattleDriver`，P-11）真需要调度时调 `Enqueue`，
//     由本类按 `0.5 / 0.2 / Finish` 的次序播 —— 与 `ActionPump` 的 kind 21 分派一一对应。
//
// ⚠️ **`waitForAnim` 的消费点没查到**：`BattleManager__FinishResolvingAction.c` 全文读过，**没有读这个字段**
//   （它只清 `resolvingActionFlag(+0x280)`、判四条动作道是否全空、必要时补 `AddReassembleMinionsOrder`）；
//   真正读它的应该是 `ResolveAction` 的分派那一支（**未反编译**）⇒ 本类**只把它存成读口**
//   （`CurrentWaitsForAnim` / `Record.waitForAnim`），⛔ 不据此自己发明「阻塞多久」。**还差**：那一支的判据。
//
// 分层：本文件**不建任何渲染件**（图标/状态框都在 `CardView` + `TraitFrames` 上，用渲染队列而非 z
//   —— 见 `CLAUDE.md` §三「分层要用渲染队列」）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 原版 `PlayIconAnim`（`BattleActionType.abilityIconAnim` = 21 的入队口）+
    /// `ResolveAbilityIconAnim`（`[0.5 若 Landing] → ActivateIconAnim → 0.2 → Finish` 那条协程）。
    /// </summary>
    public static class AbilityIconAnim
    {
        /// <summary>原版 `BattleActionType.abilityIconAnim` = **21**（`0x15`，`PlayIconAnim.c:31`）。</summary>
        public const int ActionKind = 21;
        /// <summary>原版 `AbilityTrigger.Landing` = **440**（`0x1b8`，`MoveNext.c` 那一次比较）。</summary>
        public const int LandingTrigger = 440;
        /// <summary>Landing 那一档在播动画**之前**多等的那一拍（`sec5FractionDelay` `+0x4B0` = 0.5）。</summary>
        public const float LandingPreDelay = 0.5f;
        /// <summary>动画播完到 `FinishResolvingAction` 之间那一拍（`twoFractDelay` `+0x4C8` = 0.2）。</summary>
        public const float HoldSeconds = 0.2f;
        /// <summary>`AbilityTrigger.Landing` 的**名字**（引擎侧同一套词；数值 = <see cref="LandingTrigger"/>）。</summary>
        public const string LandingTriggerName = "Landing";

        /// <summary>这条 icon 动画一共占多久（原版 = 两个 `yield return WaitForSeconds` 之和）。</summary>
        public static float DurationOf(int trigger)
        {
            return trigger == LandingTrigger ? LandingPreDelay + HoldSeconds : HoldSeconds;
        }

        // ==================================================================
        //  trigger → trait（原版 `GetTraitFromAbilityTrigger`，判据见文件头 ④）
        // ==================================================================

        /// <summary>原版 `CardTraitCollection.GetTraitFromAbilityTrigger(AbilityTrigger)`。
        /// <para>返回**引擎侧的 trait 名**（小写驼峰，与 `TraitFrames` 的键同一套）；`null` = 原版会解成
        /// `DefinedTrait.none`（⇒ 那两个方法都匹配不到任何一位图标/状态框）。</para>
        /// <para>三个特例照抄原版 switch；其余走「名字对名字」（`Unstable`→`unstable` …），
        /// 命中判据 = `TraitFrames` 的绑定表里有没有这一位（见文件头 ④ 那条 ⚠️）。
        /// `triggerName` 传 null/空 ⇒ `null`（⛔ 不猜）。</para></summary>
        public static string TraitOfTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return null;
            switch (triggerName)
            {
                case "ThisCardPlayed": return "rally";       // 原版 switch 第 3 支（DefinedTrait.rally = 10）
                case "ThisUnitDeath":  return "backlash";    // 第 2 支（backlash = 120）
                case "Unstable":       return "unstable";    // 第 1 支（unstable = 1010）
            }
            char c = triggerName[0];
            string camel = char.ToLowerInvariant(c) + triggerName.Substring(1);
            // 原版这里 `Enum.TryParse<DefinedTrait>(camel)`：**命中才是 trait**。
            // 我们手上没有那张枚举 ⇒ 用「绑定表里有没有这一位」当同一件事的判据。
            // ⚠️ 认不出**不是错**：原版对绝大多数 trigger（`TurnStart` / `WhileInPlay` …）也返回 `none`。
            return TraitExists(camel) ? camel : null;
        }

        /// <summary>数值版：数值 → 名字要靠 `triggerNameOf`（引擎侧 = `CardDef` 的 trigger 名表；
        /// 本类**不另抄一份**，见 `CLAUDE.md` 铁律「两处写同一条规则」）。
        /// 认不出名字（`null`）⇒ 直接按 <see cref="LandingTrigger"/> 的数值兜底判断 Landing。</summary>
        public static string TraitOfTrigger(int trigger, System.Func<int, string> triggerNameOf)
        {
            string name = triggerNameOf != null ? triggerNameOf(trigger) : null;
            if (string.IsNullOrEmpty(name))
                return trigger == LandingTrigger ? TraitOfTrigger(LandingTriggerName) : null;
            return TraitOfTrigger(name);
        }

        /// <summary>trait 在<b>绑定表</b>里有没有这一位（三本表任一命中即算）。
        /// ⚠️ 这是原版 `Enum.IsDefined(typeof(DefinedTrait), …)` 的**代理判据**（理由见文件头 ④）。</summary>
        static bool TraitExists(string camel)
        {
            if (string.IsNullOrEmpty(camel)) return false;
            return TraitFrames.BindingsFor(camel, true).Length > 0
                || TraitFrames.BindingsFor(camel, false).Length > 0
                || TraitFrames.StartBindingsFor(camel).Length > 0;
        }

        // ==================================================================
        //  播放（原版 `CardScript.ActivateIconAnim`，判据见文件头 ③）
        // ==================================================================

        /// <summary>原版 `CardScript.ActivateIconAnim(trigger)` 那一跳：**徽标脉冲 + 触发状态框**。
        /// <para>顺序照原版：先 `HighlightTraitIcon`（= <see cref="CardView.PulseBadgeByKeyword"/>），
        /// 再 `DisplayTriggerAnim(trait, playSound: true)`（= `TraitFrames.DisplayTrigger`）。</para>
        /// <para><paramref name="traitValue"/> 默认 `TraitFrames.NoTraitValue` = **不刷计数器**
        /// （原版那个值是 `DisplayTriggerAnim` 从卡自己的 trait 列表里取的，我们没有那条内部链
        /// ⇒ 由宿主把 `UnitState.KwValue(trait)` 传进来；⛔ 别拿 0 顶替，0 会把角标刷没）。</para>
        /// <para>⚠️ 声音那一格：原版 `DisplayTriggerAnim(trait, playSound: 1)` 的 cue 是**随机化包装**
        /// （多条候选里随机取一条），走的是状态框 prefab 自带的 `sounds` —— 我们同一个口
        /// （`TraitFrames.DisplayTrigger` → `PrefabOf` → 特效播放器的 `sounds`），⛔ 不在这里另编 cue 名。</para>
        /// <returns>真的演了什么没有（trait 解不出 / 徽标与状态框都没有 ⇒ false，**已出声**）。</returns></summary>
        public static bool PlayNow(CardView card, string traitName,
                                   bool isWarlord = false, int traitValue = TraitFrames.NoTraitValue)
        {
            if (card == null)
            {
                if (!string.IsNullOrEmpty(traitName) && _warnedNoCard.Add(traitName))
                    Debug.LogWarning($"[AbilityIconAnim] `{traitName}` 的图标动画没有卡（`CardView == null`）⇒ 不播"
                                   + "（原版 `PlayIconAnim` 对 null card **抛异常**；我们出声跳过，⛔ 不抛）。");
                return false;
            }
            if (string.IsNullOrEmpty(traitName)) return false;

            bool pulsed = card.PulseBadgeByKeyword(traitName);      // = HighlightTraitIcon
            TraitFrames tf = card.GetComponent<TraitFrames>();      // = DisplayTriggerAnim 那一层
            bool framed = tf != null && tf.DisplayTrigger(traitName, isWarlord, traitValue);
            if (!pulsed && !framed && _noFrameWarned.Add(traitName))
                Debug.Log($"[AbilityIconAnim] trait `{traitName}` 既没有徽标位也没有状态框 ⇒ 原版这一跳也什么都看不到"
                        + "（`GetTraitFromAbilityTrigger` 对大多数 trigger 返回 `none`；本条只提示一次）");
            return pulsed || framed;
        }

        /// <summary>数值版入口（内部先走 <see cref="TraitOfTrigger(int, System.Func{int,string})"/>）。
        /// <paramref name="triggerNameOf"/> 传 null 时只认得出 Landing 那一档（见上面那条 doc）。</summary>
        public static bool PlayNow(CardView card, int trigger, System.Func<int, string> triggerNameOf,
                                   bool isWarlord = false, int traitValue = TraitFrames.NoTraitValue)
        {
            string trait = TraitOfTrigger(trigger, triggerNameOf);
            if (trait == null)
            {
                if (_noTraitWarned.Add(trigger))
                    Debug.Log($"[AbilityIconAnim] trigger `{trigger}` 解不出 trait（原版 `GetTraitFromAbilityTrigger` "
                            + "也返回 `none`）⇒ 图标动画这一跳没有可演的东西（本条只提示一次）。");
                return false;
            }
            return PlayNow(card, trait, isWarlord, traitValue);
        }

        static readonly HashSet<string> _noFrameWarned = new HashSet<string>();
        static readonly HashSet<int> _noTraitWarned = new HashSet<int>();
        static readonly HashSet<string> _noTraitNameWarned = new HashSet<string>();
        static readonly HashSet<string> _warnedNoCard = new HashSet<string>();

        // ==================================================================
        //  队列（原版 `AddAutoActionToQueue` 的 4 条道 —— 数值与消费序都照 `RuleEngine.ActionQueue`）
        // ==================================================================
        //  🔴 分道 / 消费序的判据**只在 `RuleEngine/Core/ActionQueue.cs` 一处**（`CLAUDE.md`：两处写同一条规则
        //    = 迟早不一致）⇒ 这里**直接复用** `RuleEngine.ActionPriority`（Player=0 / Instant=1 /
        //    Interrupt=2 / DelayedInstant=3，`AddAutoActionToQueue` 逐字同序），消费序
        //    **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**（`BattleManager__Update.c:1284-1299`）。

        /// <summary>一条待播的 icon 动画（= 原版那个 `BattleAction{type=21, waitForAnim, trigger}`）。</summary>
        public struct Entry
        {
            public CardView card;
            /// <summary>诊断用（卡被销毁后还认得出是谁）。</summary>
            public string cardId;
            /// <summary>原版 `AbilityTrigger` 的名字（我们引擎侧同一套词；`null` = 调用方只给了数值且认不出）。</summary>
            public string triggerName;
            /// <summary>原版 `AbilityTrigger` 的数值（`-1` = 调用方只给了名字）。</summary>
            public int trigger;
            /// <summary>入队时解出的 trait（`null` = 原版也解成 `none`）。</summary>
            public string trait;
            public bool waitForAnim;
            public bool blockPlayerActions;
            public RuleEngine.ActionPriority lane;
            /// <summary>「这张卡是不是督军」—— 原版 `DisplayTriggerAnim` 的 `playIfWarlord` 门要用
            /// （判据 = `BattleDriver.PlayTraitPulse` 里同一格：`UnitState.IsWarlord`）。
            /// ⚠️ 调用方不给 ⇒ `false`（= 门放行）⇒ **可能比原版多演**，见 <see cref="Enqueue(CardView,bool,string,RuleEngine.ActionPriority,bool,string)"/> 的 ⚠️。</summary>
            public bool isWarlord;
            /// <summary>计数器要刷成的值（`UnitState.KwValue(trait)`）。
            /// ⚠️ 调用方不给 ⇒ <see cref="TraitFrames.NoTraitValue"/>（不刷计数器）。</summary>
            public int traitValue;
        }

        /// <summary>自检 / 诊断：一条走完的动作。</summary>
        public struct Record
        {
            public string cardId;
            public string triggerName;
            public int trigger;
            public string trait;             // 解出的 trait（没有 = null）
            public bool waitForAnim;
            public float at;                 // 入队时刻（`Clock`）
            public float animAt;             // 动画真的播出去的时刻（= 开场 + 0.5 若 Landing；没播 = -1）
            public float finishedAt;         // `FinishResolvingAction` 那一拍
            public bool played;              // 动画有没有真演（trait 解不出 ⇒ false）
            /// <summary>「开场 → Finish」的总时长 = `finishedAt - at`（自检拿它比
            /// <see cref="DurationOf"/>，误差应为 0）。</summary>
            public float Total { get { return finishedAt - at; } }
        }

        /// <summary>本类自己的时钟（秒）—— ⚠️ **不用 `Time.time`**：批处理没有帧循环、它不走。
        /// 由 <see cref="Tick"/> 的 `dt` 推进 ⇒ 自检可以拿假时间精确验 0.5 / 0.2 那两拍。</summary>
        public static float Clock { get; private set; }
        /// <summary>队列里还没开场的条数（不含正在播的那一条）。</summary>
        public static int Pending { get { return _lane0.Count + _lane1.Count + _lane2.Count + _lane3.Count; } }
        /// <summary>正在播的那一条在不在（`false` = 空档）。</summary>
        public static bool Busy { get { return _cur.HasValue; } }
        /// <summary>正在播的那一条的 `waitForAnim`（⚠️ 原版消费点没查到，见文件头；只做读口）。</summary>
        public static bool CurrentWaitsForAnim { get { return _cur.HasValue && _cur.Value.e.waitForAnim; } }
        /// <summary>正在播的那一条解出的 trait（空档 = null）。</summary>
        public static string CurrentTrait { get { return _cur.HasValue ? _cur.Value.e.trait : null; } }
        /// <summary>动画真演出去的次数（自检口）。</summary>
        public static int Played { get; private set; }
        /// <summary>`FinishResolvingAction` 那一拍走了几次（自检口 —— 应当与 <see cref="Started"/> 相等）。</summary>
        public static int Finished { get; private set; }
        /// <summary>开过场的条数（= 出队次数；自检口）。</summary>
        public static int Started { get; private set; }
        /// <summary>卡是 null 被丢掉的次数（原版**抛异常**，我们出声不抛）。</summary>
        public static int Dropped { get; private set; }
        /// <summary>走完的动作（最多留 <see cref="MaxLog"/> 条）。</summary>
        public static readonly List<Record> Log = new List<Record>();
        /// <summary>日志上限（只防无界增长）。</summary>
        public const int MaxLog = 256;

        static readonly Queue<Entry> _lane0 = new Queue<Entry>();   // Player
        static readonly Queue<Entry> _lane1 = new Queue<Entry>();   // Instant
        static readonly Queue<Entry> _lane2 = new Queue<Entry>();   // Interrupt（消费序第一）
        static readonly Queue<Entry> _lane3 = new Queue<Entry>();   // DelayedInstant（消费序最后）

        struct Running
        {
            public Entry e;
            public float at;
            public float animAt;
            public bool animDone;
            public float readyAt;      // 下一个小节的时刻
        }
        static Running? _cur;

        /// <summary>原版 `PlayIconAnim(card, waitForAnim, trigger, priority, block)`：**只入队**。
        /// <para>⚠️ <paramref name="isWarlord"/> / <paramref name="traitValue"/> 原版是 `DisplayTriggerAnim`
        /// **自己从卡上取的**（我们没有那条内部链）⇒ 由宿主传；不传 = `false` / `NoTraitValue`
        /// —— 后果分别是「督军那道 `playIfWarlord` 门放行（可能比原版多演一次）」与「不刷计数器」，
        /// **如实记着**，⛔ 别拿 0 顶 `traitValue`。</para>
        /// <returns>true = 进队了；false = 卡是 null（原版抛异常，我们出声跳过）。</returns></summary>
        public static bool Enqueue(CardView card, bool waitForAnim, string triggerName,
                                   RuleEngine.ActionPriority lane, bool blockPlayerActions = false,
                                   string cardId = null, bool isWarlord = false,
                                   int traitValue = TraitFrames.NoTraitValue)
        {
            if (!CheckCard(card)) return false;
            var e = new Entry
            {
                card = card, cardId = cardId, triggerName = triggerName,
                trigger = triggerName == LandingTriggerName ? LandingTrigger : -1,
                trait = TraitOfTrigger(triggerName),
                waitForAnim = waitForAnim, blockPlayerActions = blockPlayerActions, lane = lane,
                isWarlord = isWarlord, traitValue = traitValue,
            };
            return TryLane(e);
        }

        /// <summary>数值版入口（原版入参就是枚举值）。
        /// <paramref name="triggerNameOf"/> = 引擎侧「数值 → 名字」那一处（⛔ 本类不另抄一份）。</summary>
        public static bool Enqueue(CardView card, bool waitForAnim, int trigger,
                                   System.Func<int, string> triggerNameOf, RuleEngine.ActionPriority lane,
                                   bool blockPlayerActions = false, string cardId = null,
                                   bool isWarlord = false, int traitValue = TraitFrames.NoTraitValue)
        {
            string name = triggerNameOf != null ? triggerNameOf(trigger) : null;
            if (string.IsNullOrEmpty(name) && trigger == LandingTrigger) name = LandingTriggerName;
            if (!CheckCard(card)) return false;
            var e = new Entry
            {
                card = card, cardId = cardId, triggerName = name, trigger = trigger,
                trait = TraitOfTrigger(trigger, triggerNameOf),
                waitForAnim = waitForAnim, blockPlayerActions = blockPlayerActions, lane = lane,
                isWarlord = isWarlord, traitValue = traitValue,
            };
            return TryLane(e);
        }

        static bool CheckCard(CardView card)
        {
            if (card != null) return true;
            Dropped++;
            Debug.LogWarning("[AbilityIconAnim] `PlayIconAnim` 的 card 是 null —— 原版这一支 `throw` "
                           + "`NullReferenceException`；我们**出声跳过**（⛔ 不崩、也不静默）。");
            return false;
        }

        static Queue<Entry> Lane(RuleEngine.ActionPriority p)
        {
            switch (p)
            {
                case RuleEngine.ActionPriority.Player: return _lane0;
                case RuleEngine.ActionPriority.Instant: return _lane1;
                case RuleEngine.ActionPriority.Interrupt: return _lane2;
                case RuleEngine.ActionPriority.DelayedInstant: return _lane3;
                default: return null;      // 枚举之外的值：原版 `AddAutoActionToQueue` 是 LogError 后**丢弃**
                                           //   ⇒ 调用方照同口径出声 + 丢弃（⛔ 不静默归到某一条道）
            }
        }

        /// <summary>入队前的公共尾部（两条 `Enqueue` 共用）：道号非法 ⇒ 出声 + 丢弃（与原版同口径）。</summary>
        static bool TryLane(Entry e)
        {
            Queue<Entry> q = Lane(e.lane);
            if (q == null)
            {
                Debug.LogError($"[AbilityIconAnim] `priority = {(int)e.lane}` 不在原版那四条道"
                             + "（0 Player / 1 Instant / 2 Interrupt / 3 DelayedInstant）⇒ **丢弃这一条**"
                             + "（原版 `AddAutoActionToQueue` 对非法值也是 LogError 后丢弃）。");
                return false;
            }
            q.Enqueue(e);
            return true;
        }

        /// <summary>推进时钟并走**一步**（宿主每帧调；自检可拿假 `dt` 精确步进）。
        /// <para>一步 = 开场 / 播动画 / Finish 三者之一 —— 与原版「`Update` 每帧最多取一条动作」同形
        /// （`BattleManager__Update.c:1284-1299`）。⚠️ 但**协程的第一拍是同步执行的**
        /// （`StartCoroutine` 立刻跑第一个 `MoveNext`）⇒ 非 Landing 的那一档在同一拍里「开场 + 播」。</para>
        /// <returns>true = 这一拍有进展。</returns></summary>
        public static bool Tick(float dt)
        {
            if (dt > 0f) Clock += dt;

            bool started = false;
            if (!_cur.HasValue)
            {
                if (!StartNext()) return false;         // 空档且没得开场
                started = true;
            }
            var r = _cur.Value;
            // 刚开场且还不到点（Landing 要先等 0.5）—— 只有「这一拍真的开了场」才算有进展
            // ⚠️ 必须是 `started`：否则 `while (Tick(0f)) …` 这种驱动会**死循环**（时钟没推进、却一直报 true）。
            if (Clock < r.readyAt) return started;

            if (!r.animDone)
            {
                // 原版 `card.ActivateIconAnim(trigger)`（`MoveNext` 的第二个分支）
                r.animDone = true;
                r.animAt = Clock;
                var card = r.e.card;                    // ⚠️ 视图可能已经销毁：判活再播
                bool played = card != null && PlayNow(card, r.e.trait, r.e.isWarlord, r.e.traitValue);
                if (played) Played++;
                r.readyAt = Clock + HoldSeconds;
                _cur = r;
                return true;
            }

            // 第二步：原版 `FinishResolvingAction(action.actionType)`
            Finished++;
            LogAdd(r, Clock);
            _cur = null;
            return true;
        }

        static bool StartNext()
        {
            // 消费序：Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)
            Queue<Entry> q = _lane2.Count > 0 ? _lane2
                          : _lane1.Count > 0 ? _lane1
                          : _lane3.Count > 0 ? _lane3
                          : _lane0;
            if (q.Count == 0) return false;
            var e = q.Dequeue();
            Started++;
            if (e.trait == null && _noTraitNameWarned.Add(e.triggerName ?? "?"))
                Debug.Log($"[AbilityIconAnim] trigger `{e.triggerName ?? "(null)"}` 解不出 trait（原版 "
                        + "`GetTraitFromAbilityTrigger` 也返回 `none`）⇒ 这一条会照原版的次序**空跑 0.2 s**"
                        + "（图标动画没有可演的东西；本条只提示一次）。");
            var r = new Running { e = e, at = Clock, animAt = -1f, animDone = false, readyAt = Clock };
            // Landing 那一档：先等 `sec5FractionDelay`(0.5) 再播（`MoveNext` 的 state 0）。
            if (e.trigger == LandingTrigger || e.triggerName == LandingTriggerName)
                r.readyAt = Clock + LandingPreDelay;
            _cur = r;
            return true;
        }

        static void LogAdd(Running r, float finishedAt)
        {
            if (Log.Count >= MaxLog) Log.RemoveAt(0);
            Log.Add(new Record
            {
                cardId = r.e.cardId, triggerName = r.e.triggerName, trigger = r.e.trigger,
                trait = r.e.trait, waitForAnim = r.e.waitForAnim,
                at = r.at, animAt = r.animAt, finishedAt = finishedAt,
                played = r.animAt >= 0f && r.e.trait != null,
            });
        }

        /// <summary>清空（自检每段之前调）。</summary>
        public static void ResetForTest()
        {
            _lane0.Clear(); _lane1.Clear(); _lane2.Clear(); _lane3.Clear();
            _cur = null;
            Clock = 0f;
            Played = Finished = Started = Dropped = 0;
            Log.Clear();
            _noFrameWarned.Clear();
            _noTraitWarned.Clear();
            _noTraitNameWarned.Clear();
            _warnedNoCard.Clear();
        }
    }
}
