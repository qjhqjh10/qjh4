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
        ///      🔴 **喂的是【卡面正文那一串】**（`A1405`，2026-10-22）：直接调 `BattleDriver.FaceTextFull`
        ///      —— 语档 / 关键词段 / 图标那几条口径**只由那一处说了算**。原来这里另有一个私有
        ///      `TextForLang` **只复现了语档那一半**（不拼关键词段），而它自己的注释还写着
        ///      「判据逐字照 `FaceTextFull`」—— 对不上；那一份已删，理由见下面调用点的注释。
        ///      中文卡名与英文卡名是两套串，不按语档取就「卡面写着中文、相关卡却按英文找」。
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

            // ① **效果文本里点名的卡** —— 🔴 **喂进去的就是【卡面正文那一串】**（`A1405`，2026-10-22）：
            //    直接调 `BattleDriver.FaceTextFull(card)`（**卡面正文那条唯一的路** —— 卡面渲染
            //    `ToCardData(...).keywords`、单卡探针、两扇详情窗走的就是它）
            //    ⇒ 「语档取哪份字段」「关键词段拼不拼」「正文里的记号换不换图标」这几条口径
            //    **只有那一处说了算**，这里一个字都不再自己拼。
            //    **为什么非换不可（`A1405` 治的就是它）**：本行原来是个私有 `TextForLang(Desc, DescZh)`，
            //    它**只复现了 `FaceTextFull` 的一小半** —— 语档选择（`CardText.Zh && DescZh 非空`）
            //    那一半，**不拼关键词段、不换图标** ⇒ 它自己那句「判据逐字照 `FaceTextFull`」
            //    **对不上**（`FaceTextFull` 的产物 = `CardText.KeywordSegment(...)` + `CardIcons.Rewrite(...)`）。
            //    两处写同一条规则 = 迟早不一致（工程红线），而这一条已经漏过半：
            //    `FaceTextFull` 2026-10-18 刚把语档判据改对过一次，那时这里**必须跟着改**才不裂开。
            //    ⚠️ 语档那一半的后果是硬的：玩家在中文档下**看到的是中文**，而**中文卡名（`NameZh`）
            //    与英文卡名（`Name`）是两套串** —— 拿英文文本去匹配，玩家眼里「卡面明明写着
            //    『风暴守护者』」却一张相关卡都跟不出来。
            // 🔴 **`A1405` 的实测（离线复算，全池 1126 张 · 中英两档各跑一遍）**：
            //    · **喂进去的文本有变的卡 = 815（中）/ 817（英）**（关键词段 622/633 张 + 图标改写）；
            //    · **「点名那一支」的【结果】一处都没变**（`max = 8`，改前改后逐卡同集合）。
            //      为什么能这么干净（两条，都在 §三 那一段里逐项验过）：
            //      ① **没有任何一张卡的名字以 `link`/`sprite`/`name`/`nobr` 开头**
            //         —— 英文那一趟是「按空白切词 → 拼回短语 → `Norm` → 查索引」，
            //         而新加进来的每一个词都以这些标记名开头（`<link=…>` 与 `<sprite name="…">`
            //         里的空格把标记切成两个词）⇒ 拼出来的键**不可能**等于任何卡名；
            //      ② **没有任何一个关键词显示名（中/英）等于某张卡名**，也没有任何一个
            //         图标计划里的 token 是卡名 ⇒ 关键词段与图标改写**既不会多出也不会吃掉**一处命中。
            //    ⇒ 所以这一笔是**口径收口**（消灭第二份判据），**不是**行为改动。
            //    ⚠️ 读数出处与复算器的自证（中文那一趟复算出全池 **132 处**，与 `CreatePool` 里
            //    记的那条 132 逐字相同）→ `资料/普查产出_第十四会话/W_A1405相关卡文案.md` §三。
            // ⚠️ **别把 `.body` 换成别的**（比如「把 `<link>`/`<sprite>` 剥掉再喂」）—— 那等于在这儿
            //    **再立一条文本口径**，正是本条要治的病。
            string mine = BattleDriver.FaceTextFull(card).body;
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

        // 🔴 **`A1405`（2026-10-22）：这里原来有一个私有 `TextForLang(en, zh)`** ——
        //    `return (CardText.Zh && !string.IsNullOrEmpty(zh)) ? zh : en;`
        //    **已删**，改由调用点直接调 `BattleDriver.FaceTextFull(card).body`。
        //    它不是「等价的一小段」而是**第二条判据**：`FaceTextFull` 的产物是
        //    `CardText.KeywordSegment(...) + CardIcons.Rewrite(...)`，而它只做了语档选择那一半。
        //    ⛔ **别再在这儿加回任何「取哪段文本」的助手** —— 口径只许有一处（工程红线），
        //       要改就改 `BattleDriver.FaceTextFull`。
        //    ⚠️ 那个 ⚠️ 本身仍然值钱、别丢：**「只要 `DescZh` 非空就用中文」是错的** —— 那是
        //       「这张卡有没有中文」，**不是**「当前语档是不是中文」⇒ **切成 English 之后会半中半英**。
        //       正确写法就是 `FaceTextFull` 里那一行 `CardText.Zh && !string.IsNullOrEmpty(c.DescZh)`
        //       （`Core/CardText.cs` 的 `Zh` 注释里记着这个坑，`FaceTextFull` 2026-10-18 刚改过来）。

        static void AddUnique(List<CardDef> list, CardDef c)
        {
            if (c == null) return;
            foreach (var x in list) if (x.Id == c.Id) return;
            list.Add(c);
        }
    }
}
