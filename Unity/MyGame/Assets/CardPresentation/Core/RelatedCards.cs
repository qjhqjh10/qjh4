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
        ///      🔴 **喂的是哪份文本按【当前语档】取**（2026-10-18）：中文档喂 `DescZh`、英文档喂 `Desc`
        ///      —— 见 `TextForLang`。中文卡名与英文卡名是两套串，不分开取就「卡面写着中文、相关卡却按英文找」。
        ///      ⚠️ **另两支（②池子 ③黑暗契约）不跟着语档走**，理由分别写在各自那一段里；
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

            // ① **效果文本里点名的卡** —— 🔴 **文本按【当前语档】取**（2026-10-18，`项目任务.md`
            //    §三 第 19 条）。为什么非换不可：玩家在中文档下**看到的是中文**，而**中文卡名
            //    （`NameZh`）与英文卡名（`Name`）是两套串** —— 拿英文文本去匹配，玩家眼里
            //    「卡面明明写着『风暴守护者』」却一张相关卡都跟不出来。实测（全池 1126 张）：
            //    中文档 **132 处 / 67 张** vs 改前拿英文文本算的 **128 处 / 67 张**。
            //    ⚠️ 口径**逐字照** `BattleDriver.FaceTextFull`（卡面正文那条唯一的路）——
            //    `CardText.Zh && DescZh 非空` 才用中文，否则回英文（6 张没有 `descZh` 的卡就走回英文）。
            string mine = TextForLang(card.Desc, card.DescZh);
            string why;
            foreach (var r in CreatePool.MentionedCards(pool, card, mine, out why, max))
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
            // 🔴 **这一支【仍然喂英文】，不跟着语档走**（2026-10-18 查实，**与那条「按语档取」的
            //    直觉相反 —— 别「顺手对齐」**）：`PoolFromPhrase` 是个**英文句法的解析器** ——
            //    `EffectText.Parse` 只认 `Choose a …` / `a random …` 这些**英文**写法，
            //    `CreatePool.FilterChoose` 也只会剥 `" cards"` / `"friendly "` / `"non-legendary "`
            //    这类**英文**词、按**英文** subtype 与阵营名筛。喂 `DescZh` ⇒ 解析不出 `what`
            //    ⇒ 直接落进「不是池子写法」那一支 ⇒ **中文档下这一支整个消失**（那是**真回归**）。
            //    ⚠️ 而且**这一支的产出是 `CardDef` 列表、本来就与语言无关** —— 两个语档下列出的
            //    是**同样那几张卡** ⇒ 喂英文**不丢任何东西**；语档只该管上面①那种**文本匹配**。
            //    ⚠️ `TalentName` 同理也是英文的：它由 `CardDef` 从**英文 `Desc`** 里抽出
            //    （`ExtractTalent(Desc)`，见 `CardDef.cs:1012`）⇒ 连它一起"按语档取"是不成立的。
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

        /// <summary>按**当前语档**取一段卡面文本 —— 判据**逐字照** `BattleDriver.FaceTextFull`
        /// （卡面正文那条唯一的路，⛔ 别在这儿另发明一套）：**① 拿得到中文字体 ② 当前语档是中文**
        /// （两者合起来就是 `CardText.Zh`），**且**这张卡真有中文，才用中文。
        /// ⚠️ 反过来写「只要 `DescZh` 非空就用中文」是**错的** —— 那是「这张卡有没有中文」，
        /// **不是**「当前语档是不是中文」⇒ **切成 English 之后会半中半英**
        /// （`Core/CardText.cs` 的 `Zh` 注释里记着这个坑，`FaceTextFull` 2026-10-18 刚改过来）。</summary>
        static string TextForLang(string en, string zh)
        {
            return (CardText.Zh && !string.IsNullOrEmpty(zh)) ? zh : en;
        }

        static void AddUnique(List<CardDef> list, CardDef c)
        {
            if (c == null) return;
            foreach (var x in list) if (x.Id == c.Id) return;
            list.Add(c);
        }
    }
}
