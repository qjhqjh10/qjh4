// RelatedCardData.cs — **原版「相关卡」真值表**（`relatedCard1..4`）的读取口（`Core/` 那一半）
//
// ── 这是什么 ────────────────────────────────────────────────────────────
// 原版每张卡 SO（`RawCardScript`）上带着四个引用 `relatedCard1..4`
// （偏移 `+0xE8/+0xF0/+0xF8/+0x100`；字段名见签名桩 `RawCardScript.cs:66-72`）。
// 它有**两个**互不相干的消费者，两处都靠这张表：
//   ① **卡牌详情窗** `CardDisplayWindow.InitializeRelatedCards`（`…InitializeRelatedCards.c:26`
//      调 `GetRelatedCards(0)`；`RawCardScript__GetRelatedCards.c` 把四个引用原样塞进 List；
//      消费时 `iVar4` 从 **1** 起、**遇到第一个 null 就 break**）
//      → 我们这边 = `CardPresentation.Core.RelatedCards.Find`（两扇详情窗共用）；
//   ② **引擎 · 伴生部队** `CardScript.ProcessCompanion`（`ProcessCompanion.c:28` 取 `rawCardData +0xE8`；
//      `ShouldTriggerCompanion.c` = 「有 `Companion` 词条 **且** `relatedCard1 != null`」或 `counter > 0`）
//      → 我们这边 = `RuleCore.PlayCompanions`（造哪一张的定义）。
//
// 真值由 `工具/gen_related_cards.py` 从原版包解出来，落在
// `Assets/CardPresentation/Resources/related_cards.json`（键 = **我们卡池的 id**）。
//
// ── 为什么 `Core/` 这一半只有 API、没有装载 ──────────────────────────
// 🔴 本文件在 `Core/` 下 ⇒ **不许碰 `UnityEngine`**（只许 `Debug` / `Random`）。
//   装载要 `Resources.Load` + `JsonUtility` ⇒ 那半页在 `Data/RelatedCardMap.cs`
//   （同一个 `partial class` 的另一半），照 `Data/TutorialDatabase.cs` 那套先例。
//   **离屏探针**（`工具/ruleprobe` 只编 `Core/*.cs`）里那一半**不在编译集**
//   ⇒ C# 把 `LoadFromUnity()` 的**声明与调用一起抹掉** ⇒ `_map` 恒 null
//   ⇒ 两个消费者都落回各自的老路（替身/按名查），**探针照样编得过、跑得动**。
//   ⛔ 所以：**别把 `Resources` / `JsonUtility` 写进本文件**，也别把本文件挪进 `Data/`
//   （`Editor/RuleEngineTest.cs` 的 `TestCoreLayerPurity` 有一条源文扫描断言盯着 `Core/*.cs`）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `relatedCard1..4` 的查询口。**两级**：`Get`（要 id）/ `Resolve`（要 `CardDef`）。</summary>
    public static partial class RelatedCardData
    {
        /// <summary>`Data/RelatedCardMap.cs` 里那一半的实现（`Resources.Load` + `JsonUtility`）。
        /// 离屏探针里没有实现 ⇒ 调用被抹掉 ⇒ 永远「没载到」（这是**故意**的：探针不碰 Resources）。</summary>
        static partial void LoadFromUnity();

        /// <summary>`我们的卡 id → 原版 relatedCard1..4`（按槽序，至多 4）。**没载到 / 没这条键 ⇒ null**。
        /// 🔴 **自检夹具也往这张表里写**（见 <see cref="SetForTest"/>）⇒ 装载那一半必须是
        /// **合并**而不是「整个换掉」（同一个 id 以夹具那次为准，产品 id 不会撞）。</summary>
        static Dictionary<string, string[]> _map = null;

        /// <summary>🔴 **只为自检开门**（与 `EffectResolver.HurtForTest` / `CreatePool.MatchZhNameForTest`
        /// 同一条纪律：**不改任何产品行为**，产品路径一次都不调它）。
        ///
        /// <para>**为什么需要它**：原版 `ProcessCompanion` 的**入口门**是
        /// 「有 `Companion` 词条 **且** `relatedCard1 != null`」（`CardScript__ShouldTriggerCompanion.c:12-29`），
        /// 而合成夹具（`RuleEngineTest.TestCompanion` 的 `FixtureHost`）**不是卡池里的真卡**、
        /// 在 `related_cards.json` 里当然没有键 ⇒ 严格门一落地，那些夹具就会被判死。
        /// ⇒ 让夹具**显式声明**「这张卡的原版 `relatedCard1` 是哪几张」，与真卡走**同一条**判据
        /// （⛔ 不是给实现开一条旁路 —— 判据仍然只读 `Get()`）。</para>
        ///
        /// <para>**落位**：写进同一张 `_map`；`Resources` 那份装载（`Data/RelatedCardMap.cs`）
        /// 只关心**卡池里那些 id**，与夹具的 `Fixture*` 不会撞名。</para></summary>
        public static void SetForTest(string cardId, string[] relatedIds)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (_map == null) _map = new Dictionary<string, string[]>(System.StringComparer.Ordinal);
            _map[cardId] = (relatedIds == null || relatedIds.Length == 0) ? null : relatedIds;
        }

        /// <summary>表里有多少张卡（0 = 没载到 —— 那不是「原版没写」，是**我们没取到**，别混）。</summary>
        public static int TableCards { get { LoadFromUnity(); return _map == null ? 0 : _map.Count; } }

        /// <summary>这张卡的原版相关卡 **id 列表**（按 `relatedCard1..4` 顺序，至多 4）。
        /// 🔴 **返回 `null` = 原版没给这张卡写相关卡**（或表没载到）—— 调用方要能分清
        /// 「原版没写」（⇒ 可以走替身）与「列表非空但一张都落不进我们池子」（⇒ 那条要出声）。</summary>
        public static string[] Get(string cardId)
        {
            LoadFromUnity();
            if (_map == null || string.IsNullOrEmpty(cardId)) return null;
            string[] v;
            return _map.TryGetValue(cardId, out v) ? v : null;
        }

        /// <summary>表里那四个 id → 卡池里的 `CardDef`（**按 id 精确等值**，⛔ 不按卡名 ——
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
                    // 要么生成器那一步漏了。⛔ 绝不静默少列一张。
                    UnityEngine.Debug.LogWarning($"[相关卡] 真值表里 `{cardId}` 的相关卡 `{id}`"
                        + "**在卡池里翻不到**（id 精确等值都没中）—— 这一格列不出来，出声");
                    continue;
                }
                outp.Add(c);
            }
            return outp;
        }

        /// <summary>只要**第一张**（= `relatedCard1`）—— 引擎侧 `ProcessCompanion` 用的就是它。
        /// 没有真值 / 池子里翻不到 ⇒ **null**（调用方自己决定退回什么）。</summary>
        public static CardDef ResolveFirst(IReadOnlyList<CardDef> pool, string cardId)
        {
            var ids = Get(cardId);
            if (ids == null || ids.Length == 0 || pool == null) return null;
            return Find(pool, ids[0]);
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
    }
}
