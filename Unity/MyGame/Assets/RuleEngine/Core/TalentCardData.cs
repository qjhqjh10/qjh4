// TalentCardData.cs — **原版「天赋造哪一张」真值表**的读取口（`Core/` 那一半）· 账 `A1873`
//
// ── 这是什么 ────────────────────────────────────────────────────────────
// 卡面 `Talent: <名>` 在原版里是**一条 ability**：
//   · 触发时机 = **回合开始**（`CardScript__OnTurnStart.c:166`）——
//     对**回合方自己**的、带 `DefinedTrait.talent = 845`（`:163` `HasCurrentTrait(param_1, 0x34d, 0)`）的卡
//     发 `RawCardScript__OnTrigger(raw, 0x2e /*AbilityTrigger.Talent = 46*/, …)`；
//   · 干什么 = `chosenAbility = 100 /*createCardInHand*/` →
//     `BattleManager__AddNewCardToHand`（`AbilityLogic__PlayAbility.c:1110/1141`）= **造到手上**；
//   · **造哪一张** = `CardScript__GetTalentCard.c` —— 取 `cardAbilities` 里第一条 `trigger == 0x2e`
//     的 ability，再取它的 `abilityLogic(+0x50)[0].summonCriteria(+0x58).unitsToSummon(+0x18)`
//     = **一个候选列表**；N > 1 且 `summonAllTargets == 0` 时由
//     `AbilityLogic__GetCardToSummon.c` 走 `BattleManager__GetRandomInt` **随机挑一张**。
//
// 🔴 **为什么单开一张表**：原版那是一条**对象引用**；我们原来按卡面文本里的**名字**
//   （`CardDef.TalentName`）去 `CreatePool.FindByName` 查 —— 实测**有一批查不到**
//   （池子里那几张的名字带着 ` (某督军的 Talent)` 后缀），天赋**静默不生成**（只打一行日志）。
//   ⇒ 本表把原版那条引用翻成**我们池子的 id**，`RuleCore.SpawnTalents` **优先读它**；
//   表里没有的（本卡自己或那张天赋落不进我们卡池）才退回原来那条按名字的路。
//
// 真值由 `工具/gen_talent_cards.py` 生成，落在
// `Assets/RuleEngine/Resources/talent_cards.json`（键 = **我们卡池的 id**）。
// 明细（含落不下的每一条与原因）= `数据/游戏数据/talent_cards.json`。
//
// ── 为什么 `Core/` 这一半只有 API、没有装载 ──────────────────────────
// 🔴 与 `Core/RelatedCardData.cs` **同一条纪律**：本文件在 `Core/` 下 ⇒ **不许碰 `UnityEngine`**
//   （只许 `Debug`）。装载要 `Resources.Load` + `JsonUtility` ⇒ 那半页在 `Data/TalentCardMap.cs`
//   （同一个 `partial class` 的另一半）。**离屏探针**（`工具/ruleprobe` 只编 `Core/*.cs`）里
//   那一半不在编译集 ⇒ `LoadFromUnity()` 的声明与调用一起被抹掉 ⇒ `_map` 恒 null
//   ⇒ `SpawnTalents` 落回原来那条按名字的路，**探针照样编得过、跑得动**。
//   ⛔ 别把 `Resources` / `JsonUtility` 写进本文件（`RuleEngineTest.TestCoreLayerPurity` 有一条
//   源文扫描断言盯着 `Core/*.cs`）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `trigger == 46 /*Talent*/` 那条 ability 造哪一张（候选表）。**两级**：`Get` / `Resolve`。</summary>
    public static partial class TalentCardData
    {
        /// <summary>`Data/TalentCardMap.cs` 里那一半的实现（`Resources.Load` + `JsonUtility`）。
        /// 离屏探针里没有实现 ⇒ 调用被抹掉 ⇒ 永远「没载到」（**故意**的，同 `RelatedCardData`）。</summary>
        static partial void LoadFromUnity();

        /// <summary>`我们的卡 id → 原版那张天赋卡的候选 id 列表`（N 条；N &gt; 1 = 随机挑一张）。
        /// **没载到 / 没这条键 ⇒ null**。
        /// 🔴 **自检夹具也往这张表里写**（见 <see cref="SetForTest"/>）⇒ 装载那一半必须是
        /// **合并**而不是「整个换掉」（同一个 id 以夹具那次为准，产品 id 不会撞）。</summary>
        static Dictionary<string, string[]> _map = null;

        /// <summary>🔴 **只为自检开门**（与 `RelatedCardData.SetForTest` / `EffectResolver.HurtForTest`
        /// 同一条纪律：**不改任何产品行为**，产品路径一次都不调它）。
        /// 合成夹具（`RuleEngineTest` 的 `Fixture*`）不是卡池里的真卡、这张表里当然没有键
        /// ⇒ 让夹具**显式声明**「这张卡的天赋造哪几张」，与真卡走**同一条**判据（⛔ 不是旁路）。</summary>
        public static void SetForTest(string cardId, string[] talentIds)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (_map == null) _map = new Dictionary<string, string[]>(System.StringComparer.Ordinal);
            _map[cardId] = (talentIds == null || talentIds.Length == 0) ? null : talentIds;
        }

        /// <summary>表里有多少张卡（0 = 没载到 —— 那不是「原版没写」，是**我们没取到**，别混）。</summary>
        public static int TableCards { get { LoadFromUnity(); return _map == null ? 0 : _map.Count; } }

        /// <summary>这张卡的天赋候选 **id 列表**。**返回 `null` = 真值里没有这张卡**
        /// （或表没载到）—— 调用方要能分清「原版没写」（⇒ 退回按名字那条路）
        /// 与「列表非空但一张都落不进我们池子」（⇒ 那条要出声）。</summary>
        public static string[] Get(string cardId)
        {
            LoadFromUnity();
            if (_map == null || string.IsNullOrEmpty(cardId)) return null;
            string[] v;
            return _map.TryGetValue(cardId, out v) ? v : null;
        }

        /// <summary>候选 id → 卡池里的 `CardDef`（**按 id 精确等值**，⛔ 不按卡名 ——
        /// 卡名在全池有重名，`CreatePool.FindByName` 那种查法会静默取错）。
        /// 返回的一律非 null（没有真值 / 落不下时是**空表**）；落不下的那些**出声**。</summary>
        public static List<CardDef> Resolve(IReadOnlyList<CardDef> pool, string cardId)
        {
            var outp = new List<CardDef>();
            var ids = Get(cardId);
            if (ids == null || pool == null) return outp;
            foreach (var id in ids)
            {
                var c = Find(pool, id);
                if (c == null)
                {
                    // 出声：这句话只在「表里写着这张、池子里却翻不到」时出现 ⇒ 要么池子变了、
                    // 要么生成器那一步漏了。⛔ 绝不静默少造一张。
                    UnityEngine.Debug.LogWarning($"[天赋] 真值表里 `{cardId}` 的天赋卡 `{id}`"
                        + "**在卡池里翻不到**（id 精确等值都没中）—— 这一张造不出来，出声");
                    continue;
                }
                outp.Add(c);
            }
            return outp;
        }

        /// <summary>按 id 找卡。⚠️ 这里**自己走一遍池子**、不去调 `Data/CardDatabase.FindById` ——
        /// 那是 `Data/` 的类型，`Core/` 不许引用它（见本文件头：探针只编 `Core/*.cs`）。</summary>
        static CardDef Find(IReadOnlyList<CardDef> pool, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c != null && c.Id == id) return c;
            }
            return null;
        }

        // ==========================================================================================
        //  🆕 2026-10-19（第五十五会话 · 引擎线 `E12` · 账 `A2142`）：**天赋名 → id** 的真值兜底表
        // ==========================================================================================
        // 为什么要有它：上面 `Get(cardId)` 那张表只覆盖「本卡自己在真值快照里」的那批；
        // 快照之外的新卡走 `RuleCore.SpawnTalents` 的最后兜底 —— `CreatePool.FindByName(卡面天赋名)`，
        // **按显示名**查，卡一改名就静默失配（与 `SortByName` 同一族）。
        // 新表来自 `cards_engine.json` 顶层的 `talentNameIds`（生成器 `gen_cards_engine.py` 从
        // `cardAbilities[trigger==46].abilityLogic[0].summonCriteria.unitsToSummon` 的**对象引用**翻出来的，
        // 判据 = `CardScript__GetTalentCard.c`）⇒ 兜底也变成**按 id 取**。
        //
        // 键 = `CreatePool.Norm` 归一后的名字（**两侧同一份归一**：生成器里的 `_norm_talent_key` 逐字照它）。
        // ⚠️ 装载在 `Data/CardDatabase.Parse`（`JsonUtility` **不支持字典** ⇒ 文件里是 `[{n,id}]`）。
        static Dictionary<string, string> _nameMap = null;

        /// <summary>装载「归一名字 → id」表（`CardDatabase.Parse` 调；重复调以最后一次为准）。</summary>
        public static void SetNameMap(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            if (pairs == null) return;
            var m = new Dictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var kv in pairs)
                if (!string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                    m[kv.Key] = kv.Value;
            _nameMap = m.Count > 0 ? m : null;
        }

        /// <summary>表里有几个键（0 = 没载到 / 表是空的 —— 别当成「原版没天赋」）。</summary>
        public static int NameMapKeys { get { return _nameMap == null ? 0 : _nameMap.Count; } }

        /// <summary>
        /// **按卡面天赋名查真值表**：名字归一到 `CreatePool.Norm` 的同一形态 → id → 池子里的卡。
        /// 返回 `null` = 表里没有这个名字（调用方退回 `CreatePool.FindByName` 那条老路）。
        /// ⚠️ 歧义键在生成期已经**整条丢掉**（⛔ 不猜一张顶上去）。
        /// </summary>
        public static CardDef ResolveByTalentName(IReadOnlyList<CardDef> pool, string talentName)
        {
            if (_nameMap == null || pool == null || string.IsNullOrEmpty(talentName)) return null;
            string key = CreatePool.Norm(talentName);
            string id;
            if (string.IsNullOrEmpty(key) || !_nameMap.TryGetValue(key, out id)) return null;
            return Find(pool, id);
        }
    }
}
