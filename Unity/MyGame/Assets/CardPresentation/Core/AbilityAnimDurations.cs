// AbilityAnimDurations.cs — 账 `A1767`：**逐卡「发动能力」时长**的运行期表。
//
// 为什么要有它：`EventTiming.DurationOf(EvtKind.Ability)` 原来返回**一条常数 `1.0`**
// （= 本地 506 条有名 `AnimInfo` 时长之和的**中位数** —— 可复查，但**不是逐卡真值**）。
// 原版的判据是**逐技能**的：`AnimInfo.GetAnimDuration()` = `vfxDelayTime + timeAtStartPos
// + timeMoving + timeAtEndPos`（`d:/2/tools/decomp_full/AnimInfo__GetAnimDuration.c`，
// 四个偏移 0x24/0x2C/0x30/0x34 与 `dump.cs` 的 `AnimInfo` 字段逐项对应）。
//
// ==================================================================
//  判据链（逐跳可复核）
// ==================================================================
//   · **该算哪几条**（`AbilityLogic__PlayAnim.c`，逐行读过）：一个 `AbilityLogic` 的
//    `cardAnimations` 列表会被**逐条** `BattleManager.AddCardAnim(...)` 排队播完
//    （调用点 `AbilityLogic__PlayAbility.c:273`）⇒ **一张卡的值 = 它这一串时长之和**。
//    🔴 **2026-10-17 更正（铁律 5）**：这里原来写「**排在末尾那条**若 `animInfo.waitAnimToApplyEffect != 0`
//    才 `waitForCompletion = true`」—— **【错】**。实际是 **每一拍各读【自己那份】`AnimInfo.waitAnimToApplyEffect`（`0x58`）**：
//    `AbilityLogic__PlayAnim.c:274`（单目标那一支）把它**原样传走、没有任何「最后一拍」门**。
//    **错因** = 把 `PlayAnim(…, `**`List<CardScript> abilityTargets`**`)`（= **目标**）误当成了 `cardAnimations`（= **步骤**）；
//    `(iVar11 == param_4._size - 1)` 判的是「这一拍循环到的**最后一个目标**」。
//    ⚠️ **本表的【时长求和】不受这条更正影响**（求和与 wait 标记无关）；改的只是「**哪一拍带 wait 标记**」。
//    判据与现核 → `d:/4/Unity/资料/普查产出_1017/BSC62/R4_waitForAnim语义.md`（`dump.cs:19516` / `:19457`）。
//    由 `_ResolveAnim` 播。
//  · **播多久**（`BattleManager._ResolveAnim_d__466__MoveNext.c`）：先（仅当
//    `animInfo.tweenAnim != null && tweenAnim.waitAnimation != 0`）`WaitForCompletion(BuildSequence(…))`，
//    之后**一定**再 `WaitForSeconds(AnimInfo.GetAnimDuration())`。
//    ⚠️ **tween 那一段本表没算** —— 见下面 `TryGet` 的返回值说明。
//      🔴 **2026-10-24 就地订正（铁律 5）**：括号里原来写「`UnitTweenSO` 的长度**本地没接**」—— **已过期**：
//      数据侧 2026-10-23（`A11`）已把 `UnitTweenSO` 的 PathID → 名字 → 长度全解出（全量件 `tweenRows`），
//      并证明 `waitAnimation = 1` 的 **4 条全部落在触发链** ⇒ 对 `active` **零影响**
//      （`activeWithTween` 与 `active` 93/93 逐卡相等）。运行时**仍未把 tween 加进 `s`**（`s` = `active`）
//      ⇒「**本表没算**」这半句**仍然成立**。判据 → `资料/普查产出_1023e/A11_特效第三批.md` §2·1。
//  · **哪一支**：只取 `activeAbility.activeAbilityLogic[*].cardAnimations[*]`
//    （= 原版「单位主动发动能力」那条路）。`cardAbilities[*]`（`ThisCardPlayed` / `Slay` / …）
//    是**别的时机**播的 ⇒ 归 `EvtKind.Trigger` / `Play`，**不进这张表**（全量件里备查）。
//
// 数据：`工具/gen_ability_anim_durations.py` → `Resources/ability_anim_durations.json`
//       （🔴 **2026-10-22（`W64`）现读 = 93 张卡 / 160 条 CardAnim**，min 0.2 / 中位 1.0 / max 4.0。
//        ⚠️ 这行历史上的三个数**全部过期**：「56 张卡 / 89 条」（账 `A2095`）→「62 张卡 / 99 条」（`A1767`）
//        → **93 / 160**（本行；`W64` 重出后的现读数）。
//        ⛔ 别再手抄这两个数：读 `Resources/ability_anim_durations.json` 的 `count`／全量件的 `_headline`）。
//       全量件（含逐条 CardAnim、未覆盖名单、判据长文）= `数据/游戏数据/ability_anim_durations.json`。
//
// 🔴 **三处如实标注（铁律 3 / 11）—— 2026-10-22（`W64`）全部订正（铁律 5，原来三条都过期）**：
//   ① ✅ **不再「只覆盖 2022 快照」**：真值源已是**当前 build 的卡 SO**
//      （`资料/普查产出_1018/allcards_1350/cards_raw.json` 的
//      `activeAbility.activeAbilityLogic[*].cardAnimations[*]`）；第二条链 `cards_engine.json` 的
//      `animTimes`（`tg == -1` 行）与它**逐卡一致 93 / 不一致 0**（全量件 `_crossCheck`）
//      ⇒ UM / SW 那两批**不再是「判据为空」**；
//   ② **3 张例外**（全量件 `uncovered`，都是「会发 `EvtKind.Ability`、但判据不足以定值」）：
//      `SOR30 Celestian Sacresant` · `SOR65 Celestian Sacresant Aveline` · `SW9` ——
//      它们的 `activeAbility` **一条非空 CardAnim 引用都没有**（`m_AssetGUID` 全空）⇒ 原版这条路
//      不放任何动画 ⇒ 我们退回兜底并**出声**（不是「表里漏了」）；
//   ③ **`SW21 Wulfen Pack Leader` 是条件分支**（铁律 5·c：同一张卡、不同棋盘状态 = 不同时长）：
//      **普通 `activeAbility` = 0.4**（表里存的就是这支）· **替代能力（`Cut Wulfen`）= 0.2**。
//      🔴 **2026-10-23 就地订正（铁律 5 · 判据 = 第一权威反编译）**：上一行原来写
//      「`hasAlternativeAbility=1` 且 `MeetsCriteria(**selectCriteria**)` **满足时**原版走替代支」
//      —— **字段名与方向都不对**（`RawCardScript__GetActiveAbility.c` 逐句）：
//        `if (hasAlternativeAbility(+0x2AA)) { if (!MeetsCriteria(param_2, *(this+0x2B0), …))
//             return *(this+0x2B8); } return *(this+0x260);`
//      ⇒ 判据字段 = **`normalAbilityBoardCriteria`(+0x2B0)**，**满足时走【普通】支**、
//        **不满足**才落到 `alternativeAbility`(+0x2B8)。
//      ⛔ 表只记普通那一支；运行时**要不要按棋盘切到 0.2 还没接** —— **数据侧也缺**：
//        🔴 **2026-10-24 就地订正（铁律 5 · 判据 = `资料/普查产出_1023e/A11_特效第三批.md` §2·2(b)）**：
//        原来这里写「`normalAbilityBoardCriteria` **不在**卡导出里（`cards_raw.json` 的 `cards[*]`
//        只有 `hasAlternativeAbility` / `activeAbility` / `alternativeAbility`）」—— **一半不准**。
//        它**在**卡导出里，只是挂在 **`cards[*].full_raw.normalAbilityBoardCriteria`**（不在 `cards[*]` 顶层）：
//        现读 1,351 张**全都带这个结构体**，其中**非默认值 2 张** —— `SW21 Wulfen Pack Leader`
//        与 `SpaceWolves-Fenrisian Wolf`（后者 `hasAlternativeAbility = 0` ⇒ `MeetsCriteria` 根本不会被调）。
//        `工具/gen_ability_anim_durations.py:434` **一直在读它** ⇒ 全量件 `conditional` 一直是准的。
//        **真正缺的是引擎那份** `MyGame/Assets/RuleEngine/Resources/cards_engine.json`：现读全池
//        （张数见它的 `count`，⛔ 别写死）**逐张看**，`normalAbilityBoardCriteria` / `hasAlternativeAbility`
//        **一个都没有** ⇒ 要接得先让 `gen_cards_engine.py` 把这两个字段带出来（卡牌线的文件）。
//        逐条见全量件 `conditional`。
//   ④ **tween 那一段仍没算**（见上「播多久」那一条）。
//   ⑤ 命中不了时**退回 `1.0` 并出声**（不静默）—— 那是本表的**中位数**，与旧常数恰好相等。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>运行期一条：我们卡池的 id / 卡名 / 时长（秒）/ 由几条 CardAnim 累出来。</summary>
    [Serializable]
    public class AbilityAnimEntry
    {
        public string id;
        public string name;
        public float s;      // Σ GetAnimDuration()（秒）
        public int n;        // 累了几条 CardAnim（诊断：=1 的卡只有一条动画）
    }

    [Serializable]
    public class AbilityAnimFile
    {
        public string schema;
        public float fallbackSeconds;
        public int count;
        public AbilityAnimEntry[] items = new AbilityAnimEntry[0];
    }

    /// <summary>逐卡「发动能力」时长。**判据链见文件头**；这张表是它的唯一入口。</summary>
    public static class AbilityAnimDurations
    {
        public const string ResourcePath = "ability_anim_durations";

        /// <summary>载不到资源 / 表里没有这张卡时的兜底秒数（= 表的中位数，当前 **1.0**）。
        /// 🔴 **不是「原版就是这个数」** —— 它是「不知道这张卡时用的那一个数」，
        /// `EventTiming.AbilityFallbackSeconds` 与它**必须相等**（自检盯这一条）。</summary>
        public const float DefaultFallback = 1.0f;

        static Dictionary<string, AbilityAnimEntry> _byName;
        static Dictionary<string, AbilityAnimEntry> _byId;
        static AbilityAnimEntry[] _all;
        static float _fallback = DefaultFallback;

        /// <summary>表载进来了没有（没载 = 资源不在；**不静默**：`LastError` 有话说）</summary>
        public static bool Loaded { get { EnsureLoaded(); return _all != null && _all.Length > 0; } }
        public static int Count { get { EnsureLoaded(); return _all != null ? _all.Length : 0; } }
        public static string LastError { get; private set; }

        /// <summary>表里写的兜底值（载不到 ⇒ `DefaultFallback`）</summary>
        public static float FallbackSeconds { get { EnsureLoaded(); return _fallback; } }

        /// <summary>**退回兜底值的次数与最近一次是谁**（诊断；自检盯它）。
        /// 🔴 这不是「静默失败」—— 同一张卡只在**第一次**退时打一条 warning（去重），计数照累。</summary>
        public static int FallbackHits { get; private set; }
        public static string LastFallbackCard { get; private set; }

        /// <summary>已经为哪几张卡出过声（去重；warning 只打一次）。</summary>
        static readonly HashSet<string> _warned = new HashSet<string>();
        /// <summary>出声上限 —— 超过就只累计数（一局里几十张卡逐条刷屏没有意义，
        /// 而 `FallbackHits` / `LastFallbackCard` 仍然把事实留着）。</summary>
        const int WarnCap = 16;

        static void EnsureLoaded()
        {
            if (_all != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LastError = "`Resources/" + ResourcePath + ".json` 载不到 —— 跑一次 "
                          + "`python 工具/gen_ability_anim_durations.py`";
                _all = new AbilityAnimEntry[0];
                _byName = new Dictionary<string, AbilityAnimEntry>();
                _byId = new Dictionary<string, AbilityAnimEntry>();
                return;
            }
            AbilityAnimFile f = null;
            try { f = JsonUtility.FromJson<AbilityAnimFile>(ta.text); }
            catch (Exception e) { LastError = "`" + ResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.items != null ? f.items : new AbilityAnimEntry[0];
            _all = items;
            if (f != null && f.fallbackSeconds > 0f) _fallback = f.fallbackSeconds;
            _byName = new Dictionary<string, AbilityAnimEntry>(items.Length);
            _byId = new Dictionary<string, AbilityAnimEntry>(items.Length);
            foreach (var it in items)
            {
                if (it == null) continue;
                if (!string.IsNullOrEmpty(it.name)) _byName[it.name] = it;
                if (!string.IsNullOrEmpty(it.id)) _byId[it.id] = it;
            }
            if (LastError == null && items.Length == 0)
                LastError = "`" + ResourcePath + ".json` 里一条都没有";
        }

        /// <summary>按**卡名或卡 id** 取这张卡的「发动能力」时长。
        ///
        /// ⚠️ **两个键都要**：`BattleEvent.CardId` 装的是 **`CardDef.Name`（英文卡名）**
        /// （`RuleCore.UseAbility` / `UseOathAbility` 传的是 `u.Name`），而本表的主键是**卡 id**；
        /// 卡名会重（全池 3 组同名）⇒ 名字这一半走「重名就取先到的那个」不可靠，
        /// 所以**先查 id、再查卡名**，两边都查不到才出声退回。
        ///
        /// 返回 `false` = 这张卡**不在真值表里**（2022 快照没有它 / 原版这张卡没有主动能力动画）
        /// ⇒ 调用方用兜底值。**本方法会出声**（每张卡一次），不必再在外面重打一遍。
        /// </summary>
        public static bool TryGet(string cardIdOrName, out float seconds)
        {
            EnsureLoaded();
            seconds = _fallback;
            if (string.IsNullOrEmpty(cardIdOrName)) return false;

            AbilityAnimEntry e;
            if (_byId.TryGetValue(cardIdOrName, out e) && e != null) { seconds = e.s; return true; }
            if (_byName.TryGetValue(cardIdOrName, out e) && e != null) { seconds = e.s; return true; }

            FallbackHits++;
            LastFallbackCard = cardIdOrName;
            if (Loaded && _warned.Count < WarnCap && _warned.Add(cardIdOrName))
                Debug.LogWarning(
                    $"[发动能力时长] `{cardIdOrName}` **不在真值表里** ⇒ 退回 {_fallback:F2}s"
                    + "（= 表的中位数，与旧常数恰好相等）。原因只有两种：① 它不在 2022 快照里"
                    + "（UM/SW 那批扩军）② 原版这张卡的 `activeAbility` 一条 CardAnim 都没有。"
                    + "逐张名单 → `数据/游戏数据/ability_anim_durations.json` 的 `uncovered`");
            return false;
        }

        /// <summary>表里全部条目（自检对账用）。</summary>
        public static IEnumerable<AbilityAnimEntry> Items { get { EnsureLoaded(); return _all; } }

        /// <summary>表里是否**有**这个键（id 或卡名）—— 与 <see cref="TryGet"/> 分开，是因为
        /// 自检要区分「表里确实有 0.0 秒那种条目」和「表里没有」。</summary>
        public static bool Has(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return false;
            return _byId.ContainsKey(key) || _byName.ContainsKey(key);
        }
    }
}
