// RelatedCards.cs — **「这张卡的相关卡是谁」**（判据只此一份）
//
// 为什么单开一个文件：菜单那扇详情窗（`Shell/CardDetailPopup`）与战斗那扇
// （`Battle/CardDisplayWindow`）**都要算相关卡**，这套判据写两份迟早不一致
// （项目准则：「判据要共用一份」）。判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §八 / §九。

using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public static class RelatedCards
    {
        // ✅ 已复刻（2026-11-05 · B146-E2 · 用户 2026-11-05 裁定删替身 · 判据：RelatedCardData 真值 191 张/239 边，其余四格全空）
        /// <summary>相关卡（最多 `max` 张）。**唯一来源 = 原版真值 `relatedCard1..4`**。
        ///
        /// 🔴 **2026-11-05（`B146-E2` · 用户 2026-11-05 裁定）：替身三支已删** ——
        ///   原先还有「① 效果文本点名的卡 / ② 天赋池子 / ③ 黑暗契约」三支兜底，**全部删除**。
        ///   判据：原版 `CardDisplayWindow__InitializeRelatedCards.c:26` 只调 `card.GetRelatedCards(0)`，
        ///   而 `RawCardScript__GetRelatedCards.c` 是把卡的 `+0xE8/+0xF0/+0xF8/+0x100` 四个引用
        ///   **原样塞进 List（无筛选、无匹配）**；字段名见签名桩 `RawCardScript.cs:66-72` 的
        ///   `relatedCard1..4`。⇒ 四格全空就是**0 张**，⛔ 不许再用任何替身补。
        ///   （这也解释了「原版为什么正好 5 格」。）
        ///
        /// 真值 = `Resources/related_cards.json`（生成器 `工具/gen_related_cards.py`，⛔ 别手改；
        /// 运行时读 `RuleEngine.RelatedCardData`）。**现读 191 张 / 239 条边**，其余四格全空
        /// ⇒ 那些卡**一张相关卡都没有**（这是原版行为，不是缺口）。
        /// ⚠️ 本文件旧注释里的「240 条边 · 196 张卡」是更早一轮生成的口径，已按现表订正（铁律 5）。
        /// 🔑 顺带证成过一条：真值那四个字段装的就是「**这张卡的天赋** + **它带出来的部队/权能**」
        ///   （`AM1`→`Lord Commander`；`TAU9`→`Marker Drone`）。
        ///
        /// 顺序 = 显示顺序：主卡在前、相关卡按**槽 1→4**（真值表顺序敏感）；`max` 从**尾部**截断
        /// （= 原版 `while(truth.Count > max) RemoveAt(Count-1)` 的同义写法，断言 D7 盯着）。
        /// </summary>
        public static List<CardDef> Find(CardDef card, int max)
        {
            var outp = new List<CardDef>();
            if (card == null || max <= 0) return outp;
            var pool = CardDatabase.Load();
            if (pool == null)
            {
                Debug.LogWarning("[相关卡] 没有卡池 ⇒ 一张都算不了（**出声**，不许静默）");
                return outp;
            }

            // ⓪ **原版真值**（`A1796`）：只读 `relatedCard1..4`，顺序 = 槽 1→4。
            //    · 真值表里**没有这张卡的键** ⇒ 原版本来就没给它写相关卡 ⇒ **0 张**（⛔ 不再落替身）；
            //    · 键在、但四个 id 一个都落不进我们卡池 ⇒ `Resolve` 已**逐条出声**，这里同样返回空表。
            var truth = RelatedCardData.Resolve(pool, card.Id);
            if (truth.Count == 0)
            {
                if (RelatedCardData.Get(card.Id) != null)
                    Debug.LogWarning($"[相关卡] `{card.Id}` 在**原版真值表里有键**，但那几张"
                                   + "**一张都落不进我们卡池** ⇒ 本窗一张相关卡都不列（原版也是空，出声）");
                return outp;
            }
            while (truth.Count > max) truth.RemoveAt(truth.Count - 1);
            return truth;
        }

        // 🔴 **2026-11-05（`B146-E2`）· 「替身三支」已删除**（用户 2026-11-05 裁定）：
        //    ① 效果文本点名（`CreatePool.MentionedCards` + `BattleDriver.FaceTextFull`）
        //    ② 天赋池子（`CreatePool.PoolFromPhrase`）③ 黑暗契约（`CreatePool.MentionsDarkPact` /
        //    `DarkPactContracts`）。原版 `InitializeRelatedCards` **只读 `relatedCard1..4`、无筛选无匹配**
        //    ⇒ 真值没有就是 0 张。⛔ 别以「有些卡看起来该有相关卡」为由把任何一支加回来。
        //    ⚠️ `CreatePool` 那几个助手**本身保留**（`IconSetup` / `RuleCore.SpawnTalents` /
        //    `RuleEngineTest` 还在用它们，那是别的语义：池子解析 / 黑暗契约识别 —— 只是**不再喂相关卡**）。
        //    📌 旧口径留痕：`A1405`（2026-10-22）曾把 ① 的文本来源收口到 `FaceTextFull`，并记下
        //    「`CardText.Zh && DescZh 非空` ≠『这张卡有没有中文』」（语档那一半的坑）—— 那支已不存在，
        //    但这条教训仍然成立；卡面正文的口径仍**只许一处** = `BattleDriver.FaceTextFull`。
    }
}
