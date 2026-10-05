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
        /// <summary>相关卡（最多 `max` 张）。**三个来源**（判据 → 正本 §八 / §九 / §十·1）：
        ///   ① **效果文本里点名的卡** —— `CreatePool.MentionedCards`（用户原话：「看效果文本的意思结合
        ///      部队卡牌名字这一关键词，**提到就是相关卡**」）；
        ///   ② **天赋是个池子**时，池子里那几张（`Choose a &lt;子类型>` / `A random &lt;阵营> &lt;子类型>`）；
        ///   ③ 🆕 **「黑暗契约」那一族**（2026-09-29 用户拍板）—— 卡面/卡名提到 `Dark Pact`
        ///      就把那四张契约列上（`CreatePool.MentionsDarkPact` / `DarkPactContracts`）。
        /// ⚠️ 顺序 = 显示顺序：**先主卡、后相关卡**（用户 2026-09-26 裁的）；相关卡内部**先文本、后池子、再契约**。
        /// ⚠️ 池子那一支**只对有天赋的卡有意义**（`TalentName` 为空就跳过）—— 它同时也是
        ///    「原版相关卡 = 天赋」那个假设**本地唯一站得住的落点**（正本 §8·2 说本地证不出来）。
        ///
        /// 🔴 **2026-09-28 查实：原版这条路走的是【卡上自带的字段】，不是文本匹配** ——
        ///    `CardDisplayWindow__InitializeRelatedCards.c:26` 调 `card.GetRelatedCards(0)`，而
        ///    `RawCardScript__GetRelatedCards.c` 逐字读出来是**把卡的 `+0xE8/+0xF0/+0xF8/+0x100` 四个引用
        ///    原样塞进 List**（无筛选、无匹配）；字段名在签名桩 `RawCardScript.cs:66-72` 是
        ///    `relatedCard1..4`。⇒ **这解释了「原版为什么正好 5 格」**，也说明**我们这套是替身**：
        ///    那 4 个字段的值**在服务端**，本地拿不到 ⇒ 只能按用户的判据算（判据全文 → 正本 §十·1）。</summary>
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

            string why;
            foreach (var r in CreatePool.MentionedCards(pool, card, card.Desc, out why, max))
                AddUnique(outp, r);
            if (why != null)
                Debug.LogWarning("[相关卡] 相关卡（点名那一支）没查成：" + why + " —— 不静默");

            // ② **池子**那一支 —— 三种来路都要认（判据 → 正本 §九；`PoolFromPhrase` 只认
            //    `ChooseSrc == "pool"` 与 `A random …`，所以不会把「从牌库里挑一个部队」误当池子）：
            //      · **这张卡自己的 desc** 就是池子（例：战术卡 `Master of Arcana`，
            //        它的 desc = `Choose an Ultramarines Psychic Power and put it in your hand`）；
            //      · `TalentName` **本身就是写法**（`Talent: A random Black Legion Psychic Power`）；
            //      · `TalentName` 是**一张天赋卡的名字**（`Talent: Master of Arcana` ⇒ 去解那张卡的 desc）
            //        —— 这正是引擎那条路（`RuleCore.SpawnTalents` 先 `FindByName` 再解）。
            var phrases = new List<string>();
            if (!string.IsNullOrEmpty(card.Desc)) phrases.Add(card.Desc);
            if (!string.IsNullOrEmpty(card.TalentName))
            {
                phrases.Add(card.TalentName);
                var talentCard = CreatePool.FindByName(pool, card.TalentName);
                if (talentCard != null && !string.IsNullOrEmpty(talentCard.Desc)) phrases.Add(talentCard.Desc);
            }
            for (int pi = 0; pi < phrases.Count; pi++)
            {
                if (outp.Count >= max) break;
                var list = CreatePool.PoolFromPhrase(pool, phrases[pi], false, out string what, out string pwhy);
                if (list == null)
                {
                    Debug.Log($"[相关卡] · 池子那一支：第 {pi + 1} 个来路不是池子（{pwhy}）");
                    continue;
                }
                foreach (var r in list) { if (outp.Count >= max) break; AddUnique(outp, r); }
                Debug.Log($"[相关卡] · 池子那一支接上了：来路 {pi + 1}（筛选词 `{what}`，{list.Count} 张）");
            }

            // ③ **「黑暗契约」那一族**（用户 2026-09-29 拍板「要做」，判据全文 → 判据文件 **Q4**）——
            //    卡面/卡名**提到 `Dark Pact`** ⇒ 附上**那四张契约**（主卡本身就是契约时**排除自己**）。
            //    为什么单开一支：`Dark Pact` **不是任何一张卡的名字**，走上面 ① 只跟得出写全名的
            //    （`Khorne Berzerker` → `Dark Pact of Blood`）⇒ 只写「a Dark Pact」的一批
            //    （`Chaos Sergeant` / `Dark Apostle` / `Chosen` …）在 ① 眼里**一张都跟不出来**。
            //    ⚠️ **这条是我们的口径，不是从原版证出来的** —— 原版那 4 个 `relatedCard1..4`
            //    字段的值在服务端（缺口 → 本文件头 + 正本 §十·1）。
            if (CreatePool.MentionsDarkPact(card))
            {
                int before = outp.Count;
                foreach (var r in CreatePool.DarkPactContracts(pool, card))
                {
                    if (outp.Count >= max) break;
                    AddUnique(outp, r);
                }
                Debug.Log($"[相关卡] · 黑暗契约那一支接上了：主卡提到 `Dark Pact` ⇒ "
                          + $"附上那四张契约（新增 {outp.Count - before} 张）");
            }
            while (outp.Count > max) outp.RemoveAt(outp.Count - 1);
            return outp;
        }

        static void AddUnique(List<CardDef> list, CardDef c)
        {
            if (c == null) return;
            foreach (var x in list) if (x.Id == c.Id) return;
            list.Add(c);
        }
    }
}
