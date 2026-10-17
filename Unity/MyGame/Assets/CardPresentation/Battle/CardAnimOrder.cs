// CardAnimOrder.cs — T05 第 3 步：**「逐卡逐技能的有序演出步骤」表的运行期入口**。
//
// ─────────────────────────────────────────────────────────────────────────────
// 【这张表是什么 · 判据链】
//   原版把「一个技能播哪些动画」存在卡 SO 的 `cardAnimations[]`（= 一串 `AssetReferenceTyped<CardAnim>`），
//   由 `AbilityLogic.PlayAnim` **按序 foreach 全播**（`d:/2/tools/decomp_full/AbilityLogic__PlayAnim.c`，
//   逐句读过：外层 enumerator 走的正是 `param_1 + 0x18` = `AbilityLogic.cardAnimations`，见 `R4_waitForAnim语义.md` 证据 2）。
//   ⇒ 一次「发动技能」= **N 拍**，每拍一张独立的 `CardAnim` SO，各带自己的 `AnimInfo` 参数。
//
//   我们这边**引擎不动**（`RuleEngine` 只发一条 `EvtKind.Ability`，口径 → `任务/T05` §四①），
//   由**驱动侧**按本表把它展开成 N 条演出 —— 本文件是那张表的**唯一运行期入口**。
//
// 【数据从哪来】`Resources/card_anim_order.json`（**瘦身版**，27 KB）——
//   由 `工具/gen_card_anim_order.py` 从 `Unity/数据/游戏数据/card_anim_order.json`（全量 3.5 MB）烘焙。
//   生成期**已经把 join 做完**（`steps[].ca` → `cardAnims[]` 的参数），所以这里每拍都自足，
//   运行期不再查第二张表。瘦身方案与逐字段取舍 → 那个生成器的文件头。
//
// 🔴 **只取 `activeAbility`**（**2026-10-17 主对话裁决**，判据 → 生成器文件头那一节）：
//   `EvtKind.Ability` ↔ 该卡的**主动能力**（`activeAbility`），⛔ **不是**全部 `cardAbilities`
//   （那些是 `ThisCardPlayed`/`Slay`/`TurnEnd`/… = **别的时机**，归 `EvtKind.Trigger`/`Play`）。
//   逐卡复核 **897/897 精确相等**（与真值源 `cards_1195_2022时代.json` 的
//   `activeAbility.activeAbilityLogic[].cardAnimations[].m_AssetGUID` 逐 GUID 比）。
//   ⇒ 全量件 1112 条 ability 行里只有 **109 条是 `activeAbility`** ⇒ 本表 **63 张卡 / 100 拍**。
//   ⚠️ 拿全表展开会把**出场/触发时机的动画播在「发动技能」那一拍上** —— **错误演示且完全静默**，比少播更坏。
//
// 【与既有件的分工 · ⛔ 别搞混】
//   · `Core/AbilityAnimDurations.cs` = **一张卡的技能总时长**（Σ GetAnimDuration），喂给
//     `EventTiming.DurationOf(EvtKind.Ability)` —— 它管「这条事件在时间线上占多久」。
//   · **本文件** = 「这几拍分别是什么、各多长、按什么锚点播」—— 管**展开**。
//     两者的「时长」是**同一个判据**（`AnimInfo.GetAnimDuration()`），但**用途不同**，别互相顶替。
//   · `Resources/card_vfx_by_card.json` = **折叠表**（一个 (卡,事件) 一个 prefab）——
//     它是「只有一件时播哪件」的兜底，⛔ **别拿它承载顺序**（铁律 6；口径 → `PREP_T05第二三步.md` §六）。
//
// 【如实标注（铁律 3 / 11）】
//   ① **键 = 卡 id 或卡名**：原版按卡名取不到 id 时生成器留了 GUID 形式的 `id`；
//      而 `BattleEvent.CardId` 装的是**英文卡名**（`CardDef.Name`）⇒ 两个键都收（同 `AbilityAnimDurations` 的做法）。
//      逐卡命中面（现读）：本表 **63 张卡 / 100 拍**（= 有 `activeAbility` 且那一拍的 `ca` 解得出参数的那些）。
//      另有 **84 条 `activeAbility` 的 `ca` 是空串**（原版没绑 CardAnim）在生成期已剔除。
//   ② 🔴 **「一张卡有多条 `activeAbility` 行」时【全部保留】**（如 `Leman Russ Vanquisher` 有 2 行）
//      —— 这是**忠实于原版**的：原版 `AbilityLogic.PlayAnim` 的外层遍历是**一个 `AbilityLogic` 实例**，
//      而同一张卡的两条 `activeAbilityLogic` 行各有自己的 `cardAnimations[]`；我们的事件分不出是哪一条
//      ⇒ 按行序拼成一条（**不是猜**，是如实聚合）。⚠️ 这是**唯一**还留着的近似（单 `activeAbility` 行的卡逐拍精确）。
//   ③ **0 条**解不出 guid（全量件里 `ca` 非空但 `cardAnims` 里没有的）。
//   ④ **纯延时拍**（`vfx` 为空）在本表里**0 条** —— 那 4 个 `Wait_*` 都挂在 `cardAbilities` 行上（已按 ② 剔掉）。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一拍（= 原版 `AbilityLogic.cardAnimations[i]` 那一张 `CardAnim`）。
    /// 字段短名与 `Resources/card_anim_order.json` 逐字对应（生成器文件头有对照表）。</summary>
    [Serializable]
    public class CardAnimBeat
    {
        /// <summary>CardAnim 资产 GUID（诊断用；生成期已按它 join 好参数）。</summary>
        public string ca;
        /// <summary>**播哪件** = `AnimInfo.animAdressable(0x10)` → prefab 名。
        /// 🔴 **空串 = 纯延时拍**（原版 `Wait_300ms/1s/1.5s/2s` 那 4 个）—— 没有特效，**但时长照算**
        /// （原版用它两拍之间插停顿，⛔ 别当「解不出」丢掉）。</summary>
        public string vfx;
        /// <summary>`startPosOption`（0x18）—— 起锚点，喂 `BattleDriver.ResolveAnimAnchor`。</summary>
        public int sp;
        /// <summary>`endPosOption`（0x1C）—— 终锚点（本步只解析、**未消费**，起→终位移属 T05 第 4 步）。</summary>
        public int ep;
        /// <summary>`pointTowardsOption`（0x20）—— 朝向锚点（本步只解析、**未消费**）。</summary>
        public int pt;
        /// <summary>`vfxDelayTime`（0x24）</summary>
        public float vd;
        /// <summary>`timeAtStartPos`（0x2C）</summary>
        public float ts;
        /// <summary>`timeMoving`（0x30）</summary>
        public float tm;
        /// <summary>`timeAtEndPos`（0x34）</summary>
        public float te;
        /// <summary>`waitAnimToApplyEffect`（0x58）—— **是否等这拍播完**（判据 → 下面 <see cref="Waits"/>）。</summary>
        public int w;

        /// <summary>这拍占多久 = `AnimInfo.GetAnimDuration()`（`-= vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`）。
        /// 判据 = `AnimInfo__GetAnimDuration.c`（四个偏移 0x24/0x2C/0x30/0x34）＋ `Core/AbilityAnimDurations.cs` 文件头。
        /// ⚠️ **不含 `startDelay`(0x28)** —— 原版的 `GetAnimDuration` 也不算它。</summary>
        public float Duration { get { return vd + ts + tm + te; } }

        /// <summary>**是否等这拍播完再走下一拍** —— 读的是**这一拍自己的** `waitAnimToApplyEffect`。
        /// 🔴 判据（`R4_waitForAnim语义.md` §一/§四）：`PlayAnim.c:274`（单目标那一支）把这一位**原样传走**、
        /// **没有任何「最后一拍」门**；`(iVar11 == targets.Count-1)` 那个 `&&` 是**多目标循环里的最后一个目标**。
        /// ⇒ 我们（一拍只播一次）**退化成** `= waitAnimToApplyEffect`。
        /// ⛔ 别再写成「只有最后一拍才等」（那句是把 `param_4`=targets 误当成 `cardAnimations`=steps）。</summary>
        public bool Waits { get { return w != 0; } }
    }

    [Serializable]
    public class CardAnimOrderCard
    {
        public string id;
        public string name;
        /// <summary>该卡的演出步骤（**已按全量件里的行序 + `steps[].i` 升序**拼好）。</summary>
        public CardAnimBeat[] steps = new CardAnimBeat[0];
    }

    [Serializable]
    public class CardAnimOrderFile
    {
        public int schema;
        public string source;
        public string note;
        public CardAnimOrderCard[] cards = new CardAnimOrderCard[0];
    }

    /// <summary>「逐卡逐技能有序演出步骤」表的运行期入口。**判据链见文件头**。</summary>
    public static class CardAnimOrder
    {
        public const string ResourcePath = "card_anim_order";

        static Dictionary<string, CardAnimOrderCard> _byName;
        static Dictionary<string, CardAnimOrderCard> _byId;
        static CardAnimOrderCard[] _all;

        /// <summary>表载进来了没有（没载 = 资源不在；**不静默**：<see cref="LastError"/> 有话说）</summary>
        public static bool Loaded { get { EnsureLoaded(); return _all != null && _all.Length > 0; } }
        /// <summary>表里几张卡</summary>
        public static int Count { get { EnsureLoaded(); return _all != null ? _all.Length : 0; } }
        public static string LastError { get; private set; }

        /// <summary>**没命中**的次数与最近一次是谁（诊断；自检盯它）。
        /// 「没命中」= 这张卡不在表里 ⇒ 调用方走**原样（一拍）**，⛔ 不硬扩。</summary>
        public static int FallbackHits { get; private set; }
        public static string LastFallbackCard { get; private set; }

        /// <summary>已经为哪几个键出过声（去重；只打一次）。</summary>
        static readonly HashSet<string> _warned = new HashSet<string>();
        /// <summary>出声上限 —— 超过就只累计数（一局里几十张卡逐条刷屏没有意义）。</summary>
        const int WarnCap = 16;

        static void EnsureLoaded()
        {
            if (_all != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LastError = "`Resources/" + ResourcePath + ".json` 载不到 —— 跑一次 "
                          + "`python 工具/gen_card_anim_order.py`";
                _all = new CardAnimOrderCard[0];
                _byName = new Dictionary<string, CardAnimOrderCard>();
                _byId = new Dictionary<string, CardAnimOrderCard>();
                return;
            }
            CardAnimOrderFile f = null;
            try { f = JsonUtility.FromJson<CardAnimOrderFile>(ta.text); }
            catch (Exception e) { LastError = "`" + ResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.cards != null ? f.cards : new CardAnimOrderCard[0];
            _all = items;
            _byName = new Dictionary<string, CardAnimOrderCard>(items.Length);
            _byId = new Dictionary<string, CardAnimOrderCard>(items.Length);
            foreach (var it in items)
            {
                if (it == null) continue;
                // 重名 / 重 id 就取先到的那个（同 `AbilityAnimDurations` 的口径）
                if (!string.IsNullOrEmpty(it.name) && !_byName.ContainsKey(it.name)) _byName[it.name] = it;
                if (!string.IsNullOrEmpty(it.id) && !_byId.ContainsKey(it.id)) _byId[it.id] = it;
            }
            if (LastError == null && items.Length == 0)
                LastError = "`" + ResourcePath + ".json` 里一条都没有";
        }

        /// <summary>按**卡 id 或卡名**取这张卡的**有序演出步骤**，追加进 <paramref name="into"/>，
        /// 返回追加的条数（`0` = 这张卡不在表里 ⇒ 调用方走原样）。
        ///
        /// ⚠️ **两个键都要**（同 `AbilityAnimDurations.TryGet`）：`BattleEvent.CardId` 装的是
        /// **`CardDef.Name`（英文卡名）**，而本表的 `id` 是卡 id ⇒ 先查 id、再查卡名。
        ///
        /// ⚠️ **本方法会出声**（每张卡一次，去重 + 有上限），不必再在外面重打一遍。</summary>
        public static int TryGetBeats(string cardIdOrName, List<CardAnimBeat> into)
        {
            EnsureLoaded();
            if (into == null) return 0;
            if (string.IsNullOrEmpty(cardIdOrName)) return 0;

            CardAnimOrderCard c = null;
            if (!_byId.TryGetValue(cardIdOrName, out c) || c == null)
                if (!_byName.TryGetValue(cardIdOrName, out c)) c = null;

            if (c != null && c.steps != null && c.steps.Length > 0)
            {
                into.AddRange(c.steps);
                return c.steps.Length;
            }

            FallbackHits++;
            LastFallbackCard = cardIdOrName;
            if (Loaded && _warned.Count < WarnCap && _warned.Add(cardIdOrName))
                Debug.Log($"[Battle] 逐卡演出表里没有 `{cardIdOrName}` ⇒ 这一条 `EvtKind.Ability` 走**原样（一拍）**"
                        + "（= 旧行为：只播 `VfxMap.Ability` 那一件，⛔ 不硬扩成 N 拍）。"
                        + "原因三种：① **它只有 `cardAbilities`（别的时机）没有 `activeAbility`**（绝大多数卡 —— 这不是缺陷）"
                        + " ② 它的 `activeAbility` 那一拍 `ca` 是空串（原版没绑 CardAnim）"
                        + " ③ 它不在 2022 快照 / 不在我们卡池。");
            return 0;
        }

        /// <summary>表里全部条目（自检对账用）。</summary>
        public static IEnumerable<CardAnimOrderCard> Items { get { EnsureLoaded(); return _all; } }

        /// <summary>表里**有**这个键（id 或卡名）—— 与 <see cref="TryGetBeats"/> 分开，
        /// 是因为自检要区分「表里有这张卡」和「查表命中不了」。</summary>
        public static bool Has(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return false;
            return _byId.ContainsKey(key) || _byName.ContainsKey(key);
        }

        /// <summary>自检/诊断：清零计数器（不动已载的表）。</summary>
        public static void ResetCounters()
        {
            FallbackHits = 0; LastFallbackCard = null; _warned.Clear();
        }
    }
}
