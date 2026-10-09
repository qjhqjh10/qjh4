// CreatePool.cs — `create` 造牌的**候选池**怎么算（`EffectResolver.DoCreate` 的选卡来源）
//
// 🔴 **判据链（`CLAUDE.md` 铁律 2 的 2026-09-25 口径）**：
//   · ✅ **权威 = ① 原版全量反编译方法体** `D:/2/tools/decomp_full/` → **② 解包资源字段**
//     → **③ 成品卡图卡面文字**（`d:/2/Warpforge部队卡片/`）。
//     本文件真正吃的是**判据③**：生成器卡**自己卡面那一行橙字兵种**印着什么 ——
//     那才是「生成哪个阵营的什么兵种」的最终真相。
//   · ⚠️ 《Warpforge Offline Rulebook》是**粉丝实体版规则书**（它自己第 5 行写着
//     `Not official. Fan project.`，第 9 行写着落到实体要 creative interpretation）
//     ⇒ **只作第二来源 / 旁证**，⛔ **不能当权威**。下面两处都只是**对照材料**：
//     · 附录 B「生成/复制卡的阵营指南」（`资料/规则书/…_中文翻译.md` `:254-287`）
//       —— 每张生成器卡「生成哪个阵营的什么兵种、几种、各几张、上限多少」
//     · 附录 C「骰子查找表」（同文件 `:289-320`）—— 实体版的候选**名单**，
//       我们录进来只做**对账**（见 <see cref="DiceTables"/>），不拿它当运行时数据源：
//       按名字造卡得先有 `CardDef`，而名单里的名字和数字版数据**常常对不上**（差标点/多一个词/换个写法）。
//       对账结果由 `RuleEngineTest.CheckDiceTables` 每次跑出来（差集两个方向都报）。
//   · ⚠️ `d:/warpforge/scripts/rule_core.gd` = **我们自己的上一版 Godot 复刻**（70 个 `.gd`），
//     **只作对照/旁证**，**不是原版语义判据**。
//
// ⚠️ **2026-10-18 更正（铁律 5）**：本节原来写「**权威语义来源**：规则书 **附录 B** / 附录 C」——
//   **口径错了**：那份规则书是**粉丝实体版（非官方）**，不是权威；而且本文件的判据本来是**卡面**，
//   附录 B 只是**对账用的对照表**。**只改定性措辞，数值/常量一律没动。**
//
// 为什么池子**动态算**而不是硬编码名单：附录 B（旁证）给的是「几种」这个数，
// 实测卡池算出来的数和它逐条对得上 —— Ultramarines 载具 18 种、SaimHann 载具 20 种、
// Goff 载具 14 种、次元裂隙 2 费 6 张、召唤教派 2 费 5 张。
// 对不上的少数几处（`Tyranid Prime` 的 subtype）逐条钉在自检里。
// ⚠️ 2026-09-13 更正：这里原来还列着 `Shivversplint` / `Daemonette` 两处 —— 那两处**已经修好了**：
//    **卡面逐张核对**（1118 张卡图独立抄录，见 `资料/卡表逐张核对_与对账.md`）发现它们的
//    subtype 在 OCR 源表里是错的（`Upgrade` / `Troop`），现已按卡面改成 `Combat Elixir` / `Daemon`，
//    附录 C 的 1d6 六个名字**全对上**。修法（生成器怎么读那张修正表）见
//    `工具/gen_cards_engine.py` 的 `CARD_FACE_FIXES_SRC`。
// 数字版卡池会长（补丁/新卡），名单是死的。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。卡池由调用方从 `BattleContext.CardPool`
//    传进来；**没有卡池就如实报「这局没有卡池」，绝不退化成「从双方牌库里抽」** ——
//    那会悄悄造出一张卡面上没有的牌。
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuleEngine
{
    /// <summary>一次 `create` 的候选池。</summary>
    public class CreatePoolResult
    {
        public readonly List<CardDef> Cards = new List<CardDef>();

        /// <summary>null = 池子算出来了；否则是**人话原因**（调用方照原样播给玩家）</summary>
        public string Why;

        /// <summary>诊断串（「按 X 筛出 N 张」），写进日志好排查</summary>
        public string Detail;

        /// <summary>`Create a copy of **it**` —— 指代上一条效果的目标，不是池子查询。
        /// 结算层据此去取 `ctx.LastTarget` 那张卡。</summary>
        public bool CopyOfPrev;

        /// <summary>
        /// 🆕 **2026-09-16：造出来的那张卡要打上 `Ephemeral` 标记。**
        ///
        /// 出处：`Neurotyrant`（`TL82` · Leviathan）卡面
        ///   `Whenever you play a non-Ephemeral Stratagem, create an **Ephemeral** copy of it in your hand`
        ///   —— `ephemeral` 不是名字的一部分，是**复制品的标记**。
        /// 我们上一版复刻：`rule_core.gd:722` `var ephemeral := b.contains("ephemeral copy")`（⚠️ **旁证**）·
        ///   `:825-827` 给复制品 `kws_n.append("Ephemeral")`。
        /// 落地：`EffectResolver.DoCreate` 拿到 `picked` 之后逐张 `ctx.MarkEphemeral(c)`。
        /// </summary>
        public bool MarkEphemeral;

        public bool Ok { get { return Why == null && Cards.Count > 0; } }
    }

    public static class CreatePool
    {
        // ==================================================================
        //  入口
        // ==================================================================

        /// <summary>
        /// 「造什么」→ 候选卡列表。**算不出来返回 <see cref="CreatePoolResult.Why"/>**，
        /// 调用方必须如实播报（红线：不许静默失败）。
        /// </summary>
        /// <param name="pool">全卡池（`BattleContext.CardPool`）。**null = 这一局没给卡池**</param>
        /// <param name="payload">`EffectOp.Payload` —— 「造什么」原文（`ultramarines vehicles` / `copy of no respite`）</param>
        /// <param name="casterFaction">施放者阵营（`Warlord.Card.Faction`）。卡面**没写**阵营时用它</param>
        /// <param name="unitsOnly">只要**单位卡**（`deploy` 用 —— 部署一张战术卡是没有意义的事）</param>
        /// <param name="costMin">费用下界（0 = 不限）</param>
        /// <param name="costMax">费用上界（0 = 不限）</param>
        public static CreatePoolResult Resolve(IReadOnlyList<CardDef> pool, string payload,
                                               string casterFaction,
                                               bool unitsOnly = false, int costMin = 0, int costMax = 0)
        {
            var r = new CreatePoolResult();
            if (string.IsNullOrEmpty(payload)) { r.Why = "卡面没写造什么"; return r; }

            string what = payload.Trim().ToLowerInvariant();

            // `random` 只是选法说明，不是名字的一部分（池子在结算层用 `ctx.Rng` 抽）
            while (what.StartsWith("random ")) what = what.Substring(7).Trim();
            if (what.Length == 0) { r.Why = "卡面没写造什么"; return r; }

            bool isCopy = false;
            // 🆕 **2026-09-16：`ephemeral ` 前缀**（`create an Ephemeral copy of it in your hand`，
            //   `Neurotyrant`）—— **必须先剥**：`ephemeral copy of it` 既不匹配下面开头判据的
            //   `copy of `，也不成分兵种词 ⇒ 整段落进「具名查」⇒ 报「不是卡名也不是兵种词」，
            //   那张卡**一张都造不出来**。`ephemeral` 是**复制品的标记**，不是名字的一部分。
            if (what.StartsWith("ephemeral ")) { r.MarkEphemeral = true; what = what.Substring(10).Trim(); }
            else if (what.StartsWith("an ephemeral ")) { r.MarkEphemeral = true; what = what.Substring(13).Trim(); }
            if (what.StartsWith("copies of ")) { isCopy = true; what = what.Substring(10).Trim(); }
            else if (what.StartsWith("copy of ")) { isCopy = true; what = what.Substring(8).Trim(); }

            if (what.Length == 0)
            {
                // `Create two copies in your hand` —— 「复制**哪张**」写在上一句里（`Choose a …`），
                // 本版没有 choose 那一段，所以这里**没有可复制的对象**，如实报。
                r.Why = "卡面没写复制哪一张（本版还没有 `Choose a …` 那一段）";
                return r;
            }

            // `Create a copy of **it**` —— 指代上一条效果的目标（原版 `it_target`），不是卡名
            if (isCopy && IsPronoun(what)) { r.CopyOfPrev = true; r.Detail = "复制上一条效果的目标那张卡"; return r; }

            if (pool == null)
            {
                r.Why = "这一局没有卡池（`RuleCore.NewBattle` 没传 `cardPool`）";
                return r;
            }

            // ---- ① 候选名单：`a Gun Drone, Guardian Drone or Marker Drone` ----
            if (what.IndexOf(" or ", StringComparison.Ordinal) >= 0)
            {
                foreach (string piece in SplitList(what))
                {
                    string name = CleanListName(piece);
                    var c = FindByName(pool, name);
                    if (c != null) r.Cards.Add(c);
                    else r.Detail = AppendDetail(r.Detail, "名单里的「" + name + "」原版数据里没有这张卡");
                }
                if (r.Cards.Count == 0) r.Why = "名单里的卡原版数据里一张都没有";
                else SortByName(r.Cards);
                return r;
            }

            // ---- ② 兵种池：`[阵营] <兵种词> [with <关键词>]` ----
            // ⚠️ **要先于按名字查**：`Combat Elixir` / `Sabotage` 在原版数据里是**兵种**
            //    （`subtype`），不是卡名 —— 见本文件 `KindWords` 表。
            string withWord = null;
            int wi = what.IndexOf(" with ", StringComparison.Ordinal);
            if (wi > 0) { withWord = what.Substring(wi + 6).Trim(); what = what.Substring(0, wi).Trim(); }

            // 🆕 **2026-09-16：尾部通称 `card` / `cards` 要剥掉。**
            //   出处：`Phobos Lieutenant`（`UM_Phobos_Lieutenant`）的 `Slay:` ——
            //   `Create a random Ultramarines card in hand`。`card` **不是兵种词**，
            //   `MatchKindWord` 认不出 ⇒ 整段落进「具名查」⇒ 报「既不是卡名也不是兵种词」，
            //   那张卡**一张都造不出来**。
            //   ⚠️ 判据照抄 `FilterChoose`（本文件下面那一段）—— 那是**同一条规则的另一处**，
            //      别另写一份（写两份迟早不一致）。
            //   ⚠️ **只剥尾部**，别 `Replace` 全文（`Card` 可能是卡名的一部分）。
            {
                string trimmed = System.Text.RegularExpressions.Regex.Replace(
                    what, @"\s+cards?$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                if (trimmed.Length > 0) what = trimmed;
            }

            string factionWord, kindPhrase;
            var kind = MatchKindWord(pool, what, out factionWord, out kindPhrase);
            if (kind != null)
            {
                string faction = ResolveFaction(pool, factionWord, casterFaction, r);
                foreach (var c in pool)
                {
                    if (c == null) continue;
                    if (unitsOnly && !c.IsUnit) continue;
                    if (kind[1] == "type" && c.Type != kind[2]) continue;
                    if (kind[1] == "subtype" && !SubtypeIn(c, kind, 2)) continue;
                    if (!string.IsNullOrEmpty(faction) && !SameFaction(c.Faction, faction)) continue;
                    if (withWord != null && !HasKeyword(c, withWord)) continue;
                    if (costMin > 0 && c.Cost < costMin) continue;
                    if (costMax > 0 && c.Cost > costMax) continue;
                    r.Cards.Add(c);
                }
                string scope = (factionWord.Length > 0 ? "阵营「" + factionWord + "」+ " : "")
                             + "兵种「" + kindPhrase + "」"
                             + (withWord != null ? " + 关键词「" + withWord + "」" : "")
                             + CostScope(costMin, costMax);
                r.Detail = AppendDetail(r.Detail, scope + " → " + r.Cards.Count + " 张");
                if (r.Cards.Count == 0) r.Why = "按" + scope + "在卡池里一张都没筛到";
                else SortByName(r.Cards);
                return r;
            }

            // 🆕 **2026-09-16：只剩阵营词 ⇒ 取该阵营**全部**卡**（`Create a random Ultramarines card`）。
            //   判据照抄 `FilterChoose`（本文件下面那段）—— 它**早就有这一支**，只是没接进 `Resolve`。
            //   ⚠️ 仍然要过 `unitsOnly` 与费用上下界（与上面兵种那一支**同一套**纪律）。
            {
                string facOnly;
                if (TryResolveFaction(pool, what, out facOnly) && !string.IsNullOrEmpty(facOnly))
                {
                    foreach (var c in pool)
                    {
                        if (c == null) continue;
                        if (unitsOnly && !c.IsUnit) continue;
                        if (!SameFaction(c.Faction, facOnly)) continue;
                        if (costMin > 0 && c.Cost < costMin) continue;
                        if (costMax > 0 && c.Cost > costMax) continue;
                        r.Cards.Add(c);
                    }
                    r.Detail = AppendDetail(r.Detail, "阵营「" + what + "」全部卡 → " + r.Cards.Count + " 张");
                    if (r.Cards.Count == 0) r.Why = "阵营「" + what + "」在卡池里一张都没筛到";
                    else SortByName(r.Cards);
                    return r;
                }
            }

            // ---- ③ 具名卡：`a Termagant` / `copy of No Respite` ----
            // `kindPhrase` 是原样返回的（没命中兵种词）—— 用**整段**当卡名查，别只取最后一个词。
            // ⚠️ 2026-09-16：这里改用 `FindByNameLoose`（**双向单复数容错**）—— 见它的注释。
            var named = FindByNameLoose(pool, kindPhrase);
            if (named != null)
            {
                if (unitsOnly && !named.IsUnit)
                {
                    r.Why = "「" + named.Name + "」是" + named.Type + "卡，**不是单位**，部署不了";
                    return r;
                }
                r.Cards.Add(named);
                r.Detail = "具名卡「" + named.Name + "」";
                return r;
            }
            if (unitsOnly)
            {
                // 「部署」要的是**场上的单位**。兵种词认不出时，检查一下是不是名字对不上
                var nearU = FindNearMiss(pool, kindPhrase);
                if (nearU != null && FindByName(pool, nearU) != null && !FindByName(pool, nearU).IsUnit)
                {
                    r.Why = "「" + nearU + "」不是单位卡，部署不了";
                    return r;
                }
            }

            r.Why = "「" + kindPhrase + "」既不是原版卡名、也不是我们认识的兵种词"
                  + "（原版数据里没有这张卡，不造效果）";
            // 顺便报**最接近的那个名字** —— 原版数据是 OCR + 手抄来的，名字常年对不上：
            //   `Sergeant Taaman`（实体卡）↔ 数据里写 `Sergeant Naaman`
            //   `Extermination Protocol`（实体卡）↔ 数据里写 `Extermination Protocols`
            // ⚠️ **只报，不替**。名字对不上就当成另一张卡去造，是「猜」——红线。
            string near = FindNearMiss(pool, kindPhrase);
            if (near != null) r.Why += $"；最接近的是「{near}」，但名字对不上，**没有**拿它顶替";
            return r;
        }

        /// <summary>
        /// **选牌 handler 的候选筛选** —— `Choose a &lt;筛选> and &lt;动词>` 里那个 `&lt;筛选>`。
        ///
        /// 和 <see cref="Resolve"/> 的分工：`Resolve` 回答「**造**什么」（从全卡池取），
        /// 本方法回答「在**这堆已有的卡**里，哪些符合 `&lt;筛选>`」——
        /// 所以它接一个 `candidates`（牌库 / 手牌 / 对手手牌 / 墓地），不是全池。
        /// 兵种/阵营/关键词的判定**全部复用 <see cref="Resolve"/>**，不另写一份判据。
        ///
        /// 出处 `rule_core.gd:925 _choose_cand_match`（⚠️ 我们上一版复刻，**旁证、非原版**）。**四处按我们的数据改过它**：
        ///   · <c>stratagem</c> / <c>genomic enhancement</c> / <c>rune</c> 它按 `type`/`subtitle`/
        ///     卡名前缀判，我们按 **`subtype`** 判（数据更全，见 `KindWords`）
        ///   · <c>Choose a Genomic Enhancement</c> 它整体**排除**（`:1163`），我们不排除 ——
        ///     它的排除是因为自己判不准，我们判得准
        ///
        /// 实测的 `&lt;筛选>` 写法（30 条选牌句全过一遍）：
        /// <c>troop</c> · <c>card</c> · <c>stratagem</c> · <c>drone</c> · <c>rune</c> ·
        /// <c>invocation</c> · <c>overlord power</c> · <c>psychic power</c> ·
        /// <c>genomic enhancement</c> · <c>secret</c> · <c>sabotage</c> ·
        /// <c>friendly troop</c> · <c>friendly infantry</c> · <c>2-cost leviathan troop</c> ·
        /// <c>non-legendary ultramarines card</c> · <c>non-legendary genestealer cults troop</c>
        /// </summary>
        /// <param name="candidates">候选集（**顺序即优先级**，选法由调用方决定）</param>
        /// <param name="pool">全卡池 —— 只用来解析阵营词/判兵种（`Resolve` 需要）</param>
        /// <param name="what">`EffectOp.ChooseWhat`（小写原文）</param>
        /// <param name="casterFaction">施放者阵营（筛选词没写阵营时……**这里不用**：
        /// 选牌与造牌不同，「从自己牌库里挑」天然就是自己阵营的牌）</param>
        /// <param name="detail">诊断串</param>
        /// <param name="why">筛不出来的**人话原因**；null = 正常</param>
        public static List<CardDef> FilterChoose(IReadOnlyList<CardDef> candidates,
                                                 IReadOnlyList<CardDef> pool,
                                                 string what,
                                                 out string detail, out string why)
        {
            detail = null; why = null;
            var outp = new List<CardDef>();
            if (candidates == null || candidates.Count == 0) { why = "候选是空的"; return outp; }

            string w = (what ?? "").Trim().ToLowerInvariant();

            // ---- ① 剥掉**不是筛选条件**的词 ----
            // 尾部通称 `card` / `cards`：`non-legendary ultramarines card` 里它只是「一张牌」，
            // 剥完还剩 `non-legendary ultramarines` —— 那才是条件。
            if (w.EndsWith(" cards")) w = w.Substring(0, w.Length - 6).Trim();
            else if (w.EndsWith(" card")) w = w.Substring(0, w.Length - 5).Trim();
            // 归属词 `friendly` / `your`：**来源已经决定了是谁的**（手牌/牌库/墓地都是自己的），
            // 它不构成额外筛选。⚠️ 别拿它去筛 `Side` —— 那会把「自己的牌」当敌人。
            while (w.StartsWith("friendly ") || w.StartsWith("your "))
                w = w.Substring(w.IndexOf(' ') + 1).Trim();

            bool excludeLegendary = false;
            if (w.StartsWith("non-legendary ")) { excludeLegendary = true; w = w.Substring(14).Trim(); }
            bool excludeEphemeral = false;
            if (w.StartsWith("non-ephemeral ")) { excludeEphemeral = true; w = w.Substring(14).Trim(); }

            // `2-cost` = **恰好 2 费**（不是「≤2」）—— 与 `EffectOp.CostMin/CostMax` 同一套口径
            int costExact = 0;
            var mc = ReChooseCostExact.Match(w);
            if (mc.Success)
            {
                costExact = int.Parse(mc.Groups[1].Value);
                w = w.Remove(mc.Index, mc.Length).Trim();
            }

            // `card` / 空 = **不筛**（`Choose a card from your deck`）
            bool any = w.Length == 0 || w == "card" || w == "cards";

            // ---- ② 把 `<筛选>` 拆成**逐候选的判据**（不拿全卡池当判据集）----
            //
            // ⚠️ 2026-09-13 改过一次，**原来不是这么写的**：原来拿 `Resolve` 在**全卡池**上
            //    算出的结果**按卡名**和候选求交 —— 好处是判据只有一份，坏处是
            //    **不在卡池里的卡永远筛不出来**（自检夹具里 `new` 出来的卡撞到过；
            //    将来若加 token 也会撞，而且只会报一句「候选里没有符合…的卡」，很难查）。
            //    改成逐候选直接判：`type` / `subtype` / `faction` 本来就是卡**自己的字段**，
            //    根本不需要池子。池子只剩一个用处 —— **解析阵营词**
            //    （`sautekh` → `Sautekh`、`genestealer cults` → `Genestealers` 这种归一）。
            string[] kindRow = null;
            string fac = null;
            string nameWant = null;
            if (!any)
            {
                kindRow = MatchKindWord(pool, w, out string factionWord, out string kindPhrase);
                if (kindRow != null)
                {
                    if (!string.IsNullOrEmpty(factionWord) && !TryResolveFaction(pool, factionWord, out fac))
                        fac = null;                       // 阵营词认不出就当没写（不猜）
                }
                else if (TryResolveFaction(pool, w, out fac))
                {
                    // 只剩阵营词（`non-legendary ultramarines card` 剥完就是它）
                }
                else
                {
                    // 既不是兵种词也不是阵营词 —— 退到**具名卡**
                    var named = FindByName(pool, w);
                    if (named == null)
                    {
                        why = "筛选词「" + w + "」对不上任何兵种/阵营/卡名（原版数据里没有）";
                        return outp;
                    }
                    nameWant = Norm(named.Name);
                }
                detail = "筛选「" + w + "」"
                       + (fac != null ? " 阵营「" + fac + "」" : "")
                       + (nameWant != null ? " 具名「" + nameWant + "」" : "");
            }

            foreach (var c in candidates)
            {
                if (c == null) continue;
                if (nameWant != null && Norm(c.Name) != nameWant) continue;
                if (kindRow != null)
                {
                    if (kindRow[1] == "type" && c.Type != kindRow[2]) continue;
                    if (kindRow[1] == "subtype" && !SubtypeIn(c, kindRow, 2)) continue;
                }
                if (fac != null && !SameFaction(c.Faction, fac)) continue;
                if (excludeLegendary && IsLegendary(c)) continue;
                if (excludeEphemeral && HasKeyword(c, "ephemeral")) continue;
                if (costExact > 0 && c.Cost != costExact) continue;
                outp.Add(c);
            }

            if (outp.Count == 0)
                why = "候选里没有符合「" + (what ?? "") + "」的卡";
            return outp;
        }

        /// <summary>`2-cost` —— **恰好** N 费。出处 `EffectOp.CostMin/CostMax` 的注释（`deploy` 那套实测）。</summary>
        static readonly Regex ReChooseCostExact = new Regex(@"(\d+)-cost\b", RegexOptions.Compiled);

        /// <summary>稀有度是不是**传说**。原版数据里 `rarity` 的取值只有
        /// `common / rare / epic / legendary / special`（`cards_engine.json` 1130 张实测）。</summary>
        static bool IsLegendary(CardDef c)
        {
            return string.Equals(c.Rarity, "legendary", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 名字写岔了的**最近似卡**（只用来在报错里说一句人话）。判据两条，先严后宽：
        ///   ① 归一化后一方包含另一方（`exterminationprotocol` ⊂ `exterminationprotocols`）
        ///   ② 编辑距离 ≤ 2（`sergeanttaaman` ↔ `sergeantnaaman` 差 1）
        /// 找不到返回 null。**绝不用它替代查不到的那张卡**。
        /// </summary>
        public static string FindNearMiss(IReadOnlyList<CardDef> pool, string name)
        {
            if (pool == null || string.IsNullOrEmpty(name)) return null;
            string want = Norm(name);
            if (want.Length < 4) return null;

            string best = null; int bestDist = 3;
            foreach (var c in pool)
            {
                if (c == null) continue;
                string have = Norm(c.Name);
                if (have.Length == 0) continue;
                if (have.Contains(want) || want.Contains(have)) return c.Name;
                if (WordsSubset(name, c.Name)) return c.Name;
                int d = EditDistance(want, have);
                if (d < bestDist) { bestDist = d; best = c.Name; }
            }
            return best;                 // bestDist 初值 3 → 只有 ≤2 的才会被选中
        }

        /// <summary>
        /// **词的有序子集**：`a` 的每个词按顺序都能在 `b` 里找到（中间可以插别的词）。
        ///
        /// 为什么需要这一档：实体卡表和数字版数据对同一张卡常常是**缩写 vs 全称**，
        /// 差的是中间插进去的一个词，前后缀和编辑距离都抓不到 —— 实测三例：
        ///   `Gauss Warrior`         ↔ `**Gauss Reaper** Warrior`
        ///   `Tesla Immortal`        ↔ `**Tesla Carbine** Immortal`
        ///   `Threnodic Noise Marine` ↔ `**Threnodic Choir** Noise Marine`
        /// ⚠️ **只用来在报错里说一句人话**，绝不拿它顶替查不到的那张卡（见 `FindNearMiss`）。
        /// </summary>
        static bool WordsSubset(string a, string b)
        {
            var wa = a.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var wb = b.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (wa.Length == 0 || wa.Length > wb.Length) return false;
            int j = 0;
            foreach (var w in wa)
            {
                while (j < wb.Length && wb[j] != w) j++;
                if (j >= wb.Length) return false;
                j++;
            }
            return true;
        }

        /// <summary>编辑距离（短字符串，滚动一行就够）</summary>
        static int EditDistance(string a, string b)
        {
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var t = prev; prev = cur; cur = t;
            }
            return prev[b.Length];
        }

        /// <summary>
        /// 一张具名卡按**归一化名字**查（原版数据里 `Abaddon's Chosen` 写成 `Abaddons Chosen`、
        /// 中文卡面 `Saim-Hann` 写成 `SaimHann` —— 撇号/连字符全被剥了）。
        /// **查不到返回 null**（调用方要知道是哪一张缺了，好如实报）。
        /// </summary>
        public static CardDef FindByName(IReadOnlyList<CardDef> pool, string name)
        {
            if (pool == null || string.IsNullOrEmpty(name)) return null;
            string want = Norm(name);
            if (want.Length == 0) return null;
            foreach (var c in pool)
            {
                if (c == null) continue;
                if (Norm(c.Name) == want) return c;
            }
            return null;
        }

        /// <summary>
        /// 具名查找的**双向单复数容错**：精确失配时，再比一次「**两边都剥掉一个尾 `s`**」之后的名字。
        ///
        /// 🔴 出处：`Awakened Obelisk`（`SAU65` · Defence）卡面
        ///   `Deal 1 damage to an enemy. **If target dies, add Extermination Protocol to your hand**`
        ///   —— **单数**；而我们池里那张（`SAU45`）叫 `Extermination Protocols`（**复数**，
        ///   而它自己那张 PnP 卡图印的也是单数）。原来的 `FindByName` 只做**全等**归一
        ///   ⇒ 查不到 ⇒ 那半句**从来不生效**（只报「最接近的是…但名字对不上，没有拿它顶替」）。
        /// ⚠️ **必须双向**：这一条是**卡面少一个 `s`、池里多一个 `s`** ——
        ///   只剥**查询词**的 `s`（`MatchCardName` 那种写法）**救不了它**。
        /// ⚠️ 安全性已实测：全池**没有**两张卡只差一个尾 `s`（0 组）⇒ 不会撞车。
        /// ⚠️ 只在**造牌池的具名那一支**用 —— `MatchCardName` 那条老路一个字不动。
        /// </summary>
        static CardDef FindByNameLoose(IReadOnlyList<CardDef> pool, string name)
        {
            var hit = FindByName(pool, name);
            if (hit != null) return hit;
            if (pool == null || string.IsNullOrEmpty(name)) return null;
            string want = Singular(Norm(name));
            if (want.Length == 0) return null;
            foreach (var c in pool)
            {
                if (c == null) continue;
                string got = Singular(Norm(c.Name));
                if (got.Length > 0 && got == want) return c;
            }
            return null;
        }

        /// <summary>名字归一化：小写 + 只留字母数字。
        /// 原版数据里 `Abaddon's Chosen` → `Abaddons Chosen`、`Von Ryan's Leaper` → `Von Ryans Leaper`，
        /// 而卡面写的是带撇号的原名 —— 不归一化就永远查不到。</summary>
        public static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char ch in s)
            {
                char c = char.ToLowerInvariant(ch);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            return sb.ToString();
        }

        // ==================================================================
        //  汉字（中文卡名）那一套归一化 —— 2026-10-18（`项目任务.md` §三 第 19 条：
        //  「相关卡」的中文索引）
        // ==================================================================
        //
        // 🔴 **为什么 `Norm` 一条中文都办不了**（**实测**，不是推测）：`Norm` 只留 `a-z0-9`，
        //    汉字**一个都不留** —— 全池 **1126/1126** 张卡的 `nameZh` **全是纯汉字**
        //    （实测：混字母数字的 **0** 张、纯 ASCII 的 **0** 张、2 个字的 **64** 张），
        //    于是 `Norm("幽卫") == ""`、**`Norm(nameZh)` 在 1126 张上无一例外都是空串**。
        //    ⇒ 拿 `Norm` 去建中文索引 = 建出一张**全空键**的表，而 `BuildNameIndex` 与
        //      `MentionedCards` 都有 `k.Length == 0 ⇒ 跳过` ⇒ **一条都查不到、还不报错**。
        //      这正是本条要修的东西，所以中文那边**必须**另起一套归一化。
        //
        // ⚠️ 与英文那趟的**根本差别**：英文靠**空白切词**拼 n-gram，天然带**词边界**（规则③）；
        //    中文没有词边界 ⇒ 只能「逐位置取**最长**的一个中文卡名」。后者天生**松一点**，
        //    已知且量化过的那一处见 `MentionedCards` 的注释。

        /// <summary>中日韩统一表意文字（`U+4E00`–`U+9FFF`）—— 中文卡名用的就是这一段。
        /// ⚠️ **不含扩展区 / 兼容区**（`U+3400` / `U+F900` 那些）—— 全池 `nameZh` 实测只用到基本区。</summary>
        static bool IsCjk(char c) { return c >= '\u4e00' && c <= '\u9fff'; }

        /// <summary>这段文本里**有没有汉字**。用途只有一个：`MentionedCards` 里中文那一趟的
        /// **短路闸**（那条不变量见 `_nameIndexCjk`）。⛔ **不是「识别语档」** —— 用中文还是英文
        /// 由**调用方**决定（`RuleEngine` 不认识 `CardText`/`Loc`，也不该认识）。</summary>
        static bool HasCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char ch in s) if (IsCjk(ch)) return true;
            return false;
        }

        /// <summary>**中文卡名**那一套归一化：小写 + 留字母数字（与 <see cref="Norm"/> 同）—— **外加汉字**。
        /// <paramref name="srcIndex"/> 非 null 时，顺带把每个**保留下来**的字符在**原串**里的下标填进去
        /// （中文那一趟要靠它算「出现在第几个字」，好与英文那一趟**按位置合并**）。
        /// ⚠️ **字母数字照留、不剥** —— 它们在这里兼着**分隔符**用：不剥才挡得住
        ///   `幽[1]卫` 被拼成 `幽卫` 这种假命中。（`Norm` 那边「全剥」是对的：英文靠空白切词，
        ///   词边界已经有了；中文没有，只能靠这个。）</summary>
        static string NormCjk(string s, List<int> srcIndex = null)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = char.ToLowerInvariant(s[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || IsCjk(c))
                {
                    sb.Append(c);
                    if (srcIndex != null) srcIndex.Add(i);
                }
            }
            return sb.ToString();
        }

        // ==================================================================
        //  卡名索引（2026-09-16）—— 「按卡名指目标」那一条路的判据
        // ==================================================================
        //
        // **为什么要有它**：卡面上有一族目标写的是**一张卡的名字**而不是兵种
        // （`Codex: Give +1 to your Primaris Intercessor` · `Destroy all friendly Canoptek Scarabs`）。
        // `ParseTarget` 以前认不出这种名词 ⇒ 整句判「半懂」或退化成「按整个目标池打」。
        // ⚠️ **不认识的词一律不许猜成卡名** —— 所以判据是「**池里真有一张卡叫这个名字**」，
        //    而不是「这个词看起来像卡名」。`CardCriteria.Name` 的注释（`RuleEngine/Core/CardCriteria.cs` 的 `Name` 那条注释）
        //    写的就是这条纪律。
        //
        // ⚠️ **全等匹配**，不许「包含」：`Eliminator Sergeant` 的名字里也有 `Eliminator` ——
        //    按包含匹配会让它**自己触发自己**。归一化走 `Norm`（撇号/大小写/空格全归一）。
        // ⚠️ 单复数：卡面写的是 `canoptek scarabs`、卡名是 `Canoptek Scarab`
        //    ⇒ 查不到时**再剥一个结尾的 `s`** 试一次（`Singular` 的规则，和词表那边同一份）。

        static Dictionary<string, string> _nameIndex;

        /// <summary>🆕 **中文卡名**索引（2026-10-18，`§三 第 19 条`）：`NormCjk(NameZh)` → **卡的 `Name`（英文原名）**。
        /// 🔴 **值必须是 `Name`、不是 `NameZh`** —— 回卡池取实体走的是 <see cref="PickNamed"/>，
        ///   它按 `c.Name` **全等**比（英文那张索引的约定）⇒ 两张表**共用同一个取值口径**；
        ///   值若改成 `NameZh`，中文这条就得再写一份 `PickNamed`，两处迟早不一致。
        /// ⚠️ **只收「`NameZh` 里含汉字」的卡**（全池实测 1126/1126 都含）—— 这条**不变量**换来
        ///   `MentionedCards` 里那句「文本里没汉字 ⇒ 中文这一趟整个跳过」是**严格等价**的短路
        ///   （键**个个**含汉字 ⇒ 无汉字的文本不可能命中任何一个键）。英文档因此**零额外开销**。</summary>
        static Dictionary<string, string> _nameIndexCjk;

        /// <summary>中文索引里**最长的键有几个字** —— `MentionedCards` 逐位置扫描的**上界**。
        /// 实测今天是 **15**（`NormCjk(nameZh)` 长度分布 `2..15`）。</summary>
        static int _nameIndexCjkMaxLen;

        /// <summary>中文那一趟**最短认几个字**。⚠️ 实测：全池最短的中文卡名就是 **2 个字**（**64** 张，
        /// 例：`总督` / `先知` / `幽卫` / `猎鹰`）—— 收到 1 个字会让「单字名」把整段文本点成一片
        /// （那不是判据，是噪声）。
        /// ⚠️ **别顺手收紧到 3**：实测会掉 **21** 处命中（132 → 111）。</summary>
        const int MinCjkNameChars = 2;

        /// <summary>建卡名索引（**两张**：英文 + 中文）。由 `CardDatabase.Parse` 在读完卡表之后调一次。
        /// ⚠️ 没建索引时 <see cref="MatchCardName"/> 一律返回 null ⇒ 解析行为与从前**完全一致**（不静默误判）。</summary>
        public static void BuildNameIndex(IReadOnlyList<CardDef> pool)
        {
            var idx = new Dictionary<string, string>();
            var idxCjk = new Dictionary<string, string>();     // 🆕 中文名那一张（`NormCjk`）
            int cjkMax = 0;
            if (pool != null)
                foreach (var c in pool)
                {
                    if (c == null) continue;
                    bool named = !string.IsNullOrEmpty(c.Name);
                    if (named)
                    {
                        string k = Norm(c.Name);
                        // 同名跨阵营：**先出现的赢**（筛的是名字，两个阵营都该命中）
                        if (k.Length != 0 && !idx.ContainsKey(k)) idx[k] = c.Name;
                    }
                    // ---- 🆕 中文名那一张 —— 判据与「为什么不能用 `Norm`」见上面 `NormCjk` 那段 ----
                    // ⚠️ 值仍取 `c.Name`（英文原名），理由见 `_nameIndexCjk` 的注释。
                    if (named && HasCjk(c.NameZh))
                    {
                        string kz = NormCjk(c.NameZh);
                        if (kz.Length != 0 && !idxCjk.ContainsKey(kz))
                        {
                            idxCjk[kz] = c.Name;
                            if (kz.Length > cjkMax) cjkMax = kz.Length;
                        }
                    }
                }
            _nameIndex = idx;
            _nameIndexCjk = idxCjk;
            _nameIndexCjkMaxLen = cjkMax;
        }

        /// <summary>这个名字**是不是池里某张卡的名字**。是就返回卡表里的原名，否则 null。
        /// 单复数两种写法都认（见上面那段注释）。</summary>
        public static string MatchCardName(string phrase)
        {
            if (_nameIndex == null || string.IsNullOrEmpty(phrase)) return null;
            string k = Norm(phrase);
            if (k.Length == 0) return null;
            string hit;
            if (_nameIndex.TryGetValue(k, out hit)) return hit;
            string sing = Singular(k);
            if (sing != k && _nameIndex.TryGetValue(sing, out hit)) return hit;
            return null;
        }

        /// <summary>🆕 **只给自检开门**（2026-10-18 · `WRelated` 遗留断言第 (6) 条）：按**中文卡名**
        /// 查回英文原名（走中文那一张 `_nameIndexCjk`）；查不到返回 null。
        ///
        /// **存在的理由只有一个** —— 钉住「中文索引**不是一张空表**」这条**结构不变量**：
        /// `Norm` 那版索引在中文上**键全是空的**（实测 1126/1126 张 `Norm(nameZh) == ""`），
        /// 而建索引与查名两处都有 `k.Length == 0 ⇒ 跳过` ⇒ 那版索引**一条都查不到、还不报错**。
        /// 有了这条，「谁把 `NormCjk` 改回 `Norm`」会**当场红**，而不是静默退化成空表。
        ///
        /// 🔴 **纪律与 `EffectResolver.HurtForTest` / `CountForTest` 同一条**：只为自检开门，
        /// **不改变任何行为**、引擎里**没有任何业务代码**该走它（业务走 `MentionedCards`）。
        /// ⚠️ 放 `public`（不是 `internal`）—— `RuleEngineTest` 在 **Editor 程序集**，`internal` 它看不见
        /// （与 `HurtForTest` 同一个理由，那两处也写着这句）。</summary>
        public static string MatchZhNameForTest(string nameZh)
        {
            if (_nameIndexCjk == null || string.IsNullOrEmpty(nameZh)) return null;
            string k = NormCjk(nameZh);
            if (k.Length == 0) return null;
            string hit;
            return _nameIndexCjk.TryGetValue(k, out hit) ? hit : null;
        }

        // ==================================================================
        //  「效果文本里点名的卡」（2026-09-27）—— 卡片详情窗「相关卡」那一块的判据
        // ==================================================================
        //
        // 判据 → `资料/阶段二_卡片详情窗_原版规格.md` **§9·3**（用户原话：**看效果文本的意思结合部队
        // 卡牌名字这一关键词，提到就是相关卡**）。理由明摆着：卡面写着 `Deploy a Storm Guardian`，
        // **总不能让玩家不知道 `Storm Guardian` 是什么**。
        //
        // 四条规则（§9·3 明文；**本文件以前一条都没实现过**，这是头一份）：
        //   ① **长名优先** —— `Eliminator Sergeant` 里含 `Eliminator`，短的先命中就张冠李戴；
        //   ② **先把 `Talent:` 那一段去掉** —— 那一段**天生含一个卡名**（`Talent: Author of the Codex`），
        //      不去掉会把「天赋名」当成「正文点名」；
        //      🆕 **2026-10-18（`A1078`）第二半：前缀被数据管线剥掉的「裸写」那一段也要去掉** ——
        //      全池 3 张（`Azrael` / `Aun'Va` / `Abaddon the Despoiler`），见 `StripBareTalentSegment`；
        //   ③ **词边界** —— 不然短名字会命中一整天词内子串；
        //   ④ **跳过它自己**。
        //
        // ⚠️ 实现走**逐词 n-gram 查索引**，不走正则：`Norm` 会把撇号/连字符**全剥掉**，
        //    「按空白切词 → 拼回短语 → `Norm` → 查 `_nameIndex`」与卡名是**同一套归一化**，
        //    而且**天然就是词边界**（拼出来的短语不会命中词内子串）。
        // ⚠️ 撞名（同名 4 组，如 `Terminator` 两张）**优先同阵营**那张 —— 与 `CardDatabase.Find` 同一套口径。

        /// <summary>词与词之间只按**空白**切（撇号/句号留在词里 —— `Norm` 会剥掉它们）。</summary>
        static readonly char[] NameWordSeps = { ' ', '\t', '\n', '\r' };
        /// <summary>卡名最长按几个词拼（`Master of the Watchers of the Dark` 这种长名要够）。</summary>
        const int MaxNameWords = 6;

        /// <summary>一段效果文本里**被点名的卡**（按**出现顺序**、已去重、已跳过自己）。
        /// 详见上面那段注释；<paramref name="why"/> 非空 = 这次没查成（**调用方要如实报** ——
        /// 本文件不认识 `UnityEngine`，`Core/` 的规矩是**只记不外报**）。
        ///
        /// 🆕 **2026-10-18：这里变成「两趟」—— 英文一趟 + 中文一趟**（`项目任务.md` §三 第 19 条：
        ///   关系卡的中文索引）。要点四条：
        ///   · **语档由【调用方】决定**，不是本文件猜的：中文档传 `DescZh`、英文档传 `Desc`
        ///     （口径 → `BattleDriver.FaceTextFull`）。本文件不认识 `CardText`/`Loc`，也不该认识。
        ///   · 两趟都跑**同一段**文本，命中的按**在文本里的位置**合并 ⇒ 「出现顺序」在两种文字
        ///     混着写时也成立。⚠️ 实测：`descZh` 纯汉字 1120/1120，而英文那一趟跑在 `descZh` 上
        ///     命中 **0 张** ⇒ 实际效果就是「哪一趟非空就是它」。
        ///   · 🔴 **英文那一趟一个字段都没动**（同样的词、同样的顺序、同样的 `PickNamed`），
        ///     纯英文文本下中文那一趟被短路掉 ⇒ **英文档行为与从前逐字相同**
        ///     （`RuleEngineTest.TestMentionedCards` 那 10 个期望数仍然对得上）。
        ///   · ⚠️ **中文这一趟天生比英文松，这是已知的、量化过的**：英文靠**空白切词**自带词边界
        ///     （规则③），中文没有词边界 ⇒ 只能「逐位置取**最长**的一个中文卡名」。
        ///     全池实测唯一一处可见差：`复生`（= `Reanimate`，**只有 2 个字**）把中文的
        ///     `被复生时…` 也算进来了，而英文的 `Reanimated` 被词边界挡在外面
        ///     ⇒ 全池 **+4** 处命中（128 → 132）。**这是「松」，不是漏，也不是 bug** ——
        ///     中文里那个关键词与那张卡共用同一个词，判据层面分不开。⛔ 别为此改成 3 个字起
        ///     （会掉 21 处，见 `MinCjkNameChars`）。</summary>
        public static List<CardDef> MentionedCards(IReadOnlyList<CardDef> pool, CardDef self,
                                                   string text, out string why, int max = 8)
        {
            why = null;
            var outp = new List<CardDef>();
            if (pool == null || string.IsNullOrEmpty(text) || max <= 0) return outp;
            var idx = _nameIndex;
            if (idx == null)
            {
                // 索引由 `CardDatabase.Parse` 读卡表时建一次 ⇒ 走到这儿说明调用方没先读卡表。
                // **说出来**：静默返回空会让「相关卡一张都没有」看着像正常（红线：不许静默失败）。
                why = "还没建卡名索引（`CardDatabase.Load()` 没跑过？）⇒ 相关卡会少";
                return outp;
            }

            string s = StripTalentSegments(text, self);
            // 两趟都往这两条**平行表**里塞 `(位置, 卡)`，最后按位置升序合并成返回的那一份。
            var pos = new List<int>();
            var hitDefs = new List<CardDef>();

            // ---- ① 英文那一趟（2026-09-27 的原实现；**匹配逻辑一个字未改**，只是多了「位置」）----
            var words = s.Split(NameWordSeps, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                // 每个词在**剥离后原串**里的起点。词是有序出现的 ⇒ 从上一个词的末尾往后找，不会串位。
                var wpos = new int[words.Length];
                int scan = 0;
                for (int w = 0; w < words.Length; w++)
                {
                    int at = s.IndexOf(words[w], scan, StringComparison.Ordinal);
                    if (at < 0) at = scan;                 // 理论上到不了（这些词就是从 `s` 切出来的）
                    wpos[w] = at;
                    scan = at + words[w].Length;
                }
                for (int i = 0; i < words.Length && hitDefs.Count < max; i++)
                {
                    for (int len = Math.Min(MaxNameWords, words.Length - i); len >= 1; len--)
                    {
                        string k = Norm(string.Join(" ", words, i, len));
                        if (k.Length == 0) continue;
                        string hit;
                        if (!idx.TryGetValue(k, out hit))
                        {
                            string sing = Singular(k);             // 单复数：`Canoptek Scarabs` ↔ `Canoptek Scarab`
                            if (sing == k || !idx.TryGetValue(sing, out hit)) continue;
                        }
                        var def = PickNamed(pool, hit, self);
                        if (def == null) continue;
                        AddMention(pos, hitDefs, wpos[i], def);
                        i += len - 1;                              // 这一段已经用掉了 ⇒ 别重复匹配
                        break;
                    }
                }
            }

            // ---- ② 🆕 中文那一趟（走 `NormCjk` 那张索引；为什么另起一套 → 上面 `NormCjk` 那段）----
            if (hitDefs.Count < max && _nameIndexCjk != null
                && _nameIndexCjkMaxLen >= MinCjkNameChars && HasCjk(s))
            {
                var map = new List<int>(s.Length);   // 归一化后的第 k 个字 ← `s` 里的第 `map[k]` 个
                string nt = NormCjk(s, map);
                for (int j = 0; j < nt.Length && hitDefs.Count < max; )
                {
                    CardDef def = null;
                    int take = 0;
                    // **长名优先**（规则①）—— 逐位置从最长的键往下试：
                    //   `蛇咬地精` 要在 `地精` 之前命中，否则 `Snakebite Grot` 会被 `Grot` 抢走。
                    for (int len = Math.Min(_nameIndexCjkMaxLen, nt.Length - j);
                         len >= MinCjkNameChars; len--)
                    {
                        string hit;
                        if (!_nameIndexCjk.TryGetValue(nt.Substring(j, len), out hit)) continue;
                        def = PickNamed(pool, hit, self);
                        if (def == null) continue;   // 取到的就是自己 ⇒ 再试短一点的（同英文那一趟）
                        take = len;
                        break;
                    }
                    if (def == null) { j++; continue; }
                    AddMention(pos, hitDefs, map[j], def);
                    j += take;                       // 这一段已经用掉了 ⇒ 别重复匹配
                }
            }

            return hitDefs;
        }

        /// <summary>把 `(at, c)` 按**位置升序**插进两条平行表（`pos` / `hitDefs`）——
        /// 位置相同的按**先后**排（先塞的在前）；`Id` 相同的**只收一次**（两趟**之间**也去重）。
        /// ⚠️ 用**插入**而不是「先攒齐最后 sort」：英文那一趟本来就是升序，插进去**顺序与从前逐字相同**
        ///   ⇒ `RuleEngineTest` 那些期望数不会因为「换了个合并实现」而变。</summary>
        static void AddMention(List<int> pos, List<CardDef> hitDefs, int at, CardDef c)
        {
            if (c == null) return;
            for (int i = 0; i < hitDefs.Count; i++) if (hitDefs[i].Id == c.Id) return;
            int k = hitDefs.Count;
            while (k > 0 && pos[k - 1] > at) k--;
            pos.Insert(k, at);
            hitDefs.Insert(k, c);
        }

        /// <summary>把 `Talent: …`（含中文 `天赋：`）那**一段**挖掉（规则②）。
        /// ⚠️ **只挖 `Talent:` 这一段** —— 别的关键词段（`Duty:` / `Mob:` / `Oath:`…）**正文里可能真的点到
        /// 卡名**，一起挖掉反而漏。段尾 = 下一个句号（`.` / `。`），没有句号就挖到结尾
        /// （`Talent: A random Black Legion Psychic Power` 这种整条都是它）。
        ///
        /// 🆕 **2026-10-18（`A1078`）：规则② 有第二半 —— 裸写的那一段也要挖**（见
        /// <see cref="StripBareTalentSegment"/>）。加了它才知道 `self` 是谁。</summary>
        static string StripTalentSegments(string text, CardDef self)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            string s = text;
            for (int guard = 0; guard < 8; guard++)
            {
                int at = s.IndexOf("Talent:", StringComparison.OrdinalIgnoreCase);
                int zh = s.IndexOf("天赋：", StringComparison.Ordinal);
                if (zh >= 0 && (at < 0 || zh < at)) at = zh;
                if (at < 0) break;
                int end = s.IndexOf('.', at);
                int endZh = s.IndexOf('。', at);
                if (endZh >= 0 && (end < 0 || endZh < end)) end = endZh;
                s = end < 0 ? s.Substring(0, at) : s.Remove(at, end - at + 1);
            }
            return StripBareTalentSegment(s, self);
        }

        /// <summary>🆕 **裸写天赋名那一段**也要挖掉 —— 规则② 的第二半（2026-10-18 · `A1078`）。
        ///
        /// <para>**为什么需要**：规则② 原来只认 `Talent:` 前缀，而**有 3 张卡的数据里那个前缀被剥掉了**
        /// （卡面印的是 `[Talent 图标] Talent: &lt;名&gt;`，数据管线把图标和前缀一起去了 —— 照成品卡图核过，
        /// 铁律 7）。前缀一没，规则② 就**挡不住**，那一段里的天赋名被当成了「正文点名」
        /// ⇒ 英文那一趟出**假命中**。全池就这 3 张（`CardDef.cs` 的 `ExtractBareTalentName` 注释里
        /// 与 `HandledByOtherLayer` 里那条实测名单都是同一批）：
        ///  · `Azrael`（`DA3`）—— `… put it at the top of your deck. **Supreme Grand Master**`（第二段才是名字）
        ///  · `Aun'Va`（`TAU1`）—— 整条 desc 就是 `**Ethereal Supreme**`
        ///  · `Abaddon the Despoiler`（`BL1`）—— 整条 desc 就是 `**Chosen of the Four**`（名字是从 `keywords` 里抽的）</para>
        ///
        /// <para>**判据不是这里新发明的，是转调两处现成的**（`CardDef.HandledByOtherLayer` 里那条 `ExtractBareTalentName(seg) == c.TalentName` 逐字同款）：
        /// ① `CardDef.ExtractBareTalentName(seg)` —— 「这一段像不像一个裸写的天赋名」；
        /// ② 它**恰好等于**本卡已抽出来的 <see cref="CardDef.TalentName"/> —— 第二道闸：
        ///    「像名字」还不够，得是**本卡的**那个名字。
        /// ⇒ 两道闸都在，不会把效果正文误判成名字（`RuleCoreTest` 之外，
        ///    `CardDef.IsKnownSegment` 那条同款判据 2026-09-14 起就在跑）。</para>
        ///
        /// ⚠️ **只挖那一段**，段尾照 `EffectText.Split` 的口径（`.` / 换行）外加 `。`（与上面 `天赋：` 那条同款）。
        /// ⚠️ **`self` 为 null 或本卡没有天赋名 ⇒ 一字不动** —— 全池绝大多数卡走这一支。
        /// ⚠️ **中文档不受影响**：这一段判据要求段首**大写字母**，中文段一律不过（`Aun'Va` / `Azrael` 的
        ///    `descZh` 里那两处本来就是 `天赋：` 写法，由上面那一支剥）。
        /// ⚠️ **别改成「凡像名字就挖」** —— 去掉 `== self.TalentName` 这道闸会把
        ///    `Deploy a Shock Trooper` 之类的正文段也挖掉（静默少算一整批相关卡）。</summary>
        static string StripBareTalentSegment(string s, CardDef self)
        {
            string tn = self == null ? null : self.TalentName;
            if (string.IsNullOrEmpty(tn) || string.IsNullOrEmpty(s)) return s;
            int from = 0;
            while (from < s.Length)
            {
                int end = -1;
                for (int k = from; k < s.Length; k++)
                    if (s[k] == '.' || s[k] == '。' || s[k] == '\n' || s[k] == '\r') { end = k; break; }
                int stop = end < 0 ? s.Length : end;
                string seg = s.Substring(from, stop - from).Trim();
                if (seg.Length > 0 && CardDef.ExtractBareTalentName(seg) == tn)
                {
                    int cutTo = end < 0 ? s.Length : end + 1;
                    s = s.Remove(from, cutTo - from);
                    continue;      // ⚠️ **不推进 `from`**：下一段已经顶到原位，接着从原地看
                }
                if (end < 0) break;
                from = end + 1;
            }
            return s;
        }

        /// <summary>按名字回卡池取实体：**跳过自己**、**优先同阵营**（同名 4 组）。
        /// 一个都取不到返回 null（同名那张恰好就是自己时会出现）。</summary>
        static CardDef PickNamed(IReadOnlyList<CardDef> pool, string name, CardDef self)
        {
            CardDef fallback = null;
            foreach (var c in pool)
            {
                if (c == null || c.Name != name) continue;
                if (self != null && c.Id == self.Id) continue;
                if (self != null && c.Faction == self.Faction) return c;
                if (fallback == null) fallback = c;
            }
            return fallback;
        }

        // ==================================================================
        //  「黑暗契约」那一族（2026-09-29）—— 卡片详情窗「相关卡」的**第三个来源**
        // ==================================================================
        // 判据（**用户 2026-09-29 拍板「要做」**，全文 → `资料/待办判据_战场与战斗视图.md` **Q4**）：
        //   **凡是卡面/卡名提到 `Dark Pact` 的卡 ⇒ 详情窗列出那四张契约卡**。
        //
        // 为什么单开这一支：「黑暗契约」是一个**族群**（卡面那行橙字就印 `Dark Pact`），而
        //   **`Dark Pact` 本身不是任何一张卡的名字** ⇒ 「点名」那一支（`MentionedCards`）只跟得出
        //   写全名的那些（`Khorne Berzerker` → `Dark Pact of Blood`）；只写「a Dark Pact」的一批
        //   （`Chaos Sergeant` / `Dark Apostle` / `Chosen` / `Chosen of the Four` …）**一张都跟不出来**。
        //
        // 🔴 **如实标注：这条是我们的口径，不是从原版证出来的。** 原版详情窗那份相关卡走的是
        //   卡片自带的 `relatedCard1..4` 四个字段（`RawCardScript__GetRelatedCards.c` 原样塞进 List），
        //   而那四个字段的值**在服务端**（缺口与证据 → `资料/阶段二_卡片详情窗_原版规格.md` §十·1）。

        /// <summary>那四张契约**卡面上印的兵种行**（`Dark Pact`）。**按 subtype 认、不按 id 硬编码** ——
        /// id 是数据侧的事，卡面与引擎认的是这个 subtype。全池实测正好 **4 张**（`BL16/18/20/22`，
        /// 自检钉死）。</summary>
        public const string DarkPactSubtype = "Dark Pact";

        /// <summary>**卡面 / 卡名有没有提到「黑暗契约」。**
        /// 判据原文是**卡面英文**（`Dark Pact` —— 卡面上印的就是它）；中文那半边 `黑暗契约` 是
        /// **同一族群的译名**（`descZh` 里一律这么写，是我们自己的译文），一并认是为了卡面切成中文时
        /// 判据不跟着失效。⚠️ 实测两边**正好同一批 41 张**（自检里有交叉核对）⇒ 这不是「放宽口径」，
        /// 是同一件事的两种写法。</summary>
        public static bool MentionsDarkPact(CardDef c)
        {
            if (c == null) return false;
            if (c.Subtype == DarkPactSubtype) return true;                  // 契约自己（卡名里就带）
            if (ContainsCI(c.Name, "Dark Pact") || ContainsCI(c.Desc, "Dark Pact")) return true;
            if (ContainsCI(c.TalentName, "Dark Pact")) return true;         // `Talent: … Dark Pact …`
            return ContainsCI(c.NameZh, "黑暗契约") || ContainsCI(c.DescZh, "黑暗契约");
        }

        /// <summary>那一族契约卡（**排除 <paramref name="self"/>** —— 主卡本身就是契约时只列另外三张）。
        /// 顺序 = 卡池顺序（命运 / 鲜血 / 纵欲 / 韧性）。`pool` 为空返回空表（调用方如实报）。</summary>
        public static List<CardDef> DarkPactContracts(IReadOnlyList<CardDef> pool, CardDef self)
        {
            var outp = new List<CardDef>();
            if (pool == null) return outp;
            foreach (var c in pool)
            {
                if (c == null || c.Subtype != DarkPactSubtype) continue;
                if (self != null && c.Id == self.Id) continue;
                outp.Add(c);
            }
            return outp;
        }

        /// <summary>大小写不敏感的包含（`null`/空串一律 false）。</summary>
        static bool ContainsCI(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) &&
                   haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ==================================================================
        //  「写着池子的短语」→ 池子（两种写法）—— **一处实现、两处用**
        // ==================================================================

        /// <summary>「一段写着池子的短语」→ **池子里那些卡**。
        /// 返回 null = 它不是池子写法（<paramref name="why"/> 说明为什么），调用方照自己的路走。
        ///
        /// 🔴 **一处实现、两处用**：`RuleCore.SpawnTalents`（真去生成那张天赋）与
        /// **卡片详情窗的「相关卡」**（把那几张列出来，判据 → `资料/阶段二_卡片详情窗_原版规格.md` §九）
        /// **都调它** —— 两处各写一份迟早不一致（CLAUDE.md §三）。
        ///
        /// ⚠️ 两种写法，形状不一样，别只认一种：
        ///   · **整句**（`Choose an Ultramarines Psychic Power and put it in your hand`）⇒ 走
        ///     `EffectText.Parse` 取 `choosecard` 的 `ChooseWhat`（**切句子的规则只有解析器那一份**，
        ///     别在这儿自己切）；
        ///   · **只有天赋用的那种** `A random Black Legion Psychic Power` ⇒ 解析器**不给 op**
        ///     ⇒ 剥前缀（与 `Resolve` 里剥 `random ` 同规矩）。
        /// </summary>
        /// <param name="randomOnly">**只认 `random …`**（剥前缀那一支）。`SpawnTalents` 传 true ——
        /// 因为 `Choose a …` 是**玩家挑**，不能替玩家自动挑（那是另一条链：`EffectResolver.TakePickCard`）；
        /// 详情窗传 false（它只是**显示**那几张，两种写法都要显示）。</param>
        public static List<CardDef> PoolFromPhrase(IReadOnlyList<CardDef> pool, string phrase,
                                                   bool randomOnly, out string what, out string why)
        {
            what = null; why = null;
            if (pool == null || string.IsNullOrEmpty(phrase)) { why = "没写"; return null; }
            string p = phrase.Trim();

            if (!randomOnly)
            {
                var ops = EffectText.Parse(p, out _, out _);
                if (ops != null)
                    foreach (var op in ops)
                        // 🔴 **只认 `ChooseSrc == "pool"`** —— `Choose a troop from your deck` / `in your hand` /
                        //    `that died` 那些是**从已知区域里挑**（`EffectResolver.ChooseCardCandidates` 另外那几支），
                        //    **不是「池子」**；认错会把「从牌库里挑一个部队」当成池子，列出一大堆不相关的卡。
                        if (op != null && op.Verb == "choosecard" && op.ChooseSrc == "pool"
                            && !string.IsNullOrEmpty(op.ChooseWhat))
                        { what = op.ChooseWhat; break; }
            }

            if (what == null)
            {
                string s = p.ToLowerInvariant();
                if (s.StartsWith("a random ")) what = s.Substring(9).Trim();
                else if (s.StartsWith("random ")) what = s.Substring(7).Trim();
            }
            if (what == null)
            {
                why = randomOnly ? "不是 `random …` 那种池子写法" : "不是池子写法（`Choose a …` / `random …` 都不是）";
                return null;
            }

            var list = FilterChoose(pool, pool, what, out string detail, out why);
            if (list == null || list.Count == 0)
            {
                if (string.IsNullOrEmpty(why)) why = "池子是空的";
                return null;
            }
            return list;
        }

        static void SortByName(List<CardDef> list)
        {
            // 池子顺序**必须定死**：结算层用 `ctx.Rng` 按下标抽，顺序一变同一局就不一样了
            list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        static string AppendDetail(string a, string b)
        {
            return string.IsNullOrEmpty(a) ? b : a + "；" + b;
        }

        static bool IsPronoun(string t)
        {
            t = t.Trim();
            return t == "it" || t == "them" || t == "the target" || t == "this unit" || t == "this troop";
        }

        /// <summary>`a Gun Drone, Guardian Drone or Marker Drone` → 三段</summary>
        static IEnumerable<string> SplitList(string s)
        {
            foreach (string p in s.Split(new[] { " or " }, StringSplitOptions.None))
                foreach (string q in p.Split(new[] { ", " }, StringSplitOptions.None))
                {
                    string t = q.Trim();
                    if (t.Length > 0) yield return t;
                }
        }

        /// <summary>
        /// 名单里的一段 → **卡名**。剥掉冠词与「选法说明」。
        ///
        /// 🆕 2026-09-14：`Deploy a Grot or a Snakebite Grot **at random**`
        /// （`Zodgrod Wortsnagga`）——`at random` 是**选法说明**，不是名字的一部分，
        /// 不剥就查不到卡（`FindByName` 是全等匹配）⇒ **两个候选都落空、这条静默不生效**。
        /// ⚠️ 只剥**句尾**的 `at random`，和 `Resolve` 开头剥**句首** `random ` 是同一条纪律。
        /// </summary>
        static string CleanListName(string piece)
        {
            string t = (piece ?? "").Trim().TrimEnd('.', ' ').Trim();
            if (t.EndsWith(" at random", StringComparison.Ordinal))
                t = t.Substring(0, t.Length - 10).Trim();
            if (t.StartsWith("a ", StringComparison.Ordinal)) t = t.Substring(2).Trim();
            else if (t.StartsWith("an ", StringComparison.Ordinal)) t = t.Substring(3).Trim();
            else if (t.StartsWith("the ", StringComparison.Ordinal)) t = t.Substring(4).Trim();
            return t;
        }

        static bool SubtypeIn(CardDef c, string[] kind, int from)
        {
            if (string.IsNullOrEmpty(c.Subtype)) return false;      // 原版没给兵种的**不猜**，排除
            for (int i = from; i < kind.Length; i++)
                if (string.Equals(c.Subtype, kind[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static bool HasKeyword(CardDef c, string kw)
        {
            string norm = KeywordTable.Normalize(kw.ToLowerInvariant());
            if (norm == null) return false;
            return c.Has(norm);
        }

        // ==================================================================
        //  兵种词表
        // ==================================================================

        // 行格式：{ 卡面词, 匹配方式, 值1, 值2, … }
        //   `type`    —— 比 `CardDef.Type`（`unit` / `hero` / `tactic` / `defence`）
        //   `subtype` —— 比 `CardDef.Subtype`（原版 `card_stats.json` 的 `subtype`），可给多个候选值
        // ⚠️ 顺序：**多词词条排在单词前面**（`combat elixir` 要在 `elixir` 前被匹配到）
        static readonly string[][] KindWords =
        {
            // 🔴 **`troop` 与 `unit` 不是同义词**（2026-09-14 用户指正后照规则书核过）——
            //    规则书中文版 `:70-75`：
            //      · **单位（Units）**：任何有攻击与生命值的卡（**含督军与衍生物**）
            //      · **部队（Troops）**：仅部队卡，**不含督军**
            //      · 作用于「部队」的效果**不能**影响督军；作用于「单位」的效果**可以**影响督军。
            //    实测卡面：含 `unit` 的分句 **205** 条 · 含 `troop` 的 **322** 条。
            //
            //    ⚠️ **但这一张表是「卡池筛选」用的**（手牌 / 牌库 / 全卡池），那里**本来就没有督军**
            //    ⇒ 两行写成同一个映射**在这张表的用途上是对的**。
            //    **督军算不算，判据在结算层，不在这里**：`EffectResolver.AddSide` 的
            //    `troopOnly`（`= spec.Kind == "troop"`）—— 只有卡面写 `troop` 才排掉督军。
            //    ⇒ 想改「谁算单位」的时候，**改 `AddSide` 那条判据**，别改这里。
            new[] { "troop",  "type", "unit" },
            new[] { "unit",   "type", "unit" },
            // 原版 `subtype` 取值的实测分布见 `card_stats.json`（1707/… 见 `CardDef.Subtype` 注释）
            new[] { "vehicle",    "subtype", "Vehicle" },
            new[] { "infantry",   "subtype", "Infantry" },
            new[] { "battlesuit", "subtype", "Battlesuit" },
            new[] { "beast",      "subtype", "Beast" },
            new[] { "drone",      "subtype", "Drone" },
            new[] { "monster",    "subtype", "Monster" },
            new[] { "daemon",     "subtype", "Daemon" },
            new[] { "structure",  "subtype", "Structure" },
            new[] { "spell",      "subtype", "Spell" },
            new[] { "tactic",     "type",    "tactic" },

            // 具体到卡牌类型的几个（原版把它们放在 `subtype` 里，不是 `type`）：
            //   战斗药剂 —— 附录 C「战斗药剂（1d6）」那 6 张。2026-09-13 起**六张的 subtype 都统一成
            //              `Combat Elixir`** 了（卡面逐张核对的结果，原来有 3 张写 `Elixir`、
            //              1 张错写成 `Upgrade`）。`Elixir` 这个别名**留着当兜底**：万一以后
            //              数据源再出现那种写法，筛选不会静默失效。
            new[] { "combat elixir", "subtype", "Combat Elixir", "Elixir" },
            new[] { "elixir",        "subtype", "Combat Elixir", "Elixir" },
            //   破坏 —— 基因窃取者的特殊战术（`Improvised Barricade` / `Poisoned Supplies`）
            new[] { "sabotage",   "subtype", "Sabotage" },
            //   隐秘 —— 暗黑天使（附录 C「暗黑天使隐秘（1d5）」）
            new[] { "secret",     "subtype", "Secret" },

            // 下面 7 行是 2026-09-13 做**选牌 handler**（`Choose a …`）时按实测补的。
            // 判据：卡面写了这些词，而它们**全都是卡池里真实存在的 `subtype`**
            // （`cards_engine.json` 1130 张实测；括号里是张数）。
            // ⚠️ 这几条我们上一版复刻 `rule_core.gd:925 _choose_cand_match` 判得**比我们的数据差** ——
            //    它按卡名前缀 `enhanced ` 判 genomic enhancement、按 `subtitle`/`name` 判 rune。
            //    **这几条以 subtype 为准。**
            //
            // 🔴 **2026-09-14（A4 批 4）`stratagem` 一条改判** —— 它原来是
            //    `subtype == "Stratagem"`（实测**只有 6 张**），**那是错的**。
            //    我们上一版复刻 `d:/warpforge/scripts/rule_core.gd` **三处一致**（⚠️ **旁证，不是权威** ——
            //    ⚠️ **2026-10-18 更正**：这里原来写「**权威实现** ……三处一致」—— 把那份 `.gd`
            //    当权威是错的（它是**我们自己**的复刻，见文件头）；下面三条只是它当时的写法）：
            //      · `:950`  `if w.contains("stratagem") and ty != "tactic" and ty != "defence": return false`
            //      · `:3852` `if w == "stratagem" and (ct == "tactic" or ct == "defence"): return true`
            //      · `:4695` `["stratagem","stratagems",…] → ty == "tactic" or ty == "defence"`
            //    ⇒ **stratagem = 战术大类（`type ∈ {tactic, defence}`）**，与规则书 `:72`（战术=非单位卡）
            //      和 `:105`（防御卡属战术大类）一致（⚠️ 规则书 = **粉丝实体版**，同为**旁证**）。
            //      上面那句「以 subtype 为准」**不适用于这一行**。
            //    🔴 **还欠一步**：这条改判今天的支撑**只有两条旁证**（gd + 粉丝规则书），
            //      **原版判据链那三档还没回核**（反编译里 `Stratagem` 落到哪个 CardType）
            //      —— 如实记着，**别当已证**。
            //    ⚠️ 影响面（实测）：`When you play a Stratagem` 的两条既有监听器
            //      （`Acolyte Leader` / `Cult Sentinel`）原来只被那 6 张触发 —— **打得比卡面窄**；
            //      `Draw a Stratagem`（`Astropath` 等 4 条）原来也只从 6 张里翻。
            //    ⚠️ **`spell` 那一行没动**：`rule_core.gd:4695`（**那份 `.gd`，旁证**）把 `spell` 和 `stratagem` 并列，
            //      但 `Spell` 在我们数据里是 245 张的真实 `subtype`，改它会牵动选牌那一族 ——
            //      留待数据专线核过（已记进交接）。
            new[] { "stratagem",           "type",    "tactic", "defence" },    // 战术大类（非单位卡）
            new[] { "rune",                "subtype", "Rune" },                 // 3
            new[] { "invocation",          "subtype", "Invocation" },           // 3
            new[] { "overlord power",      "subtype", "Overlord Power" },       // 3
            new[] { "psychic power",       "subtype", "Psychic Power" },        // 7
            new[] { "genomic enhancement", "subtype", "Genomic Enhancement" },  // 1
            new[] { "codicil",             "subtype", "Codicil" },              // 3
        };

        /// <summary>
        /// 「<em>修饰词…</em> + 兵种词」→ 词表行（外加阵营词）。**认不出兵种词返回 null**
        /// （调用方会退回按卡名查）。
        ///
        /// 为什么要**逐个 token 扫**而不是只看最后一个词：卡面写的是
        /// `friendly **Infantry** troops` / `**Ork** **Infantry**` / `**Emperor's Children** **Daemons**`
        /// —— 修饰词既可能是**阵营**也可能是**兵种**，而且 `Infantry troops` 里
        /// **两个词都是兵种词**（`troops` 是通称、`Infantry` 才是筛选条件）。
        ///
        /// 老写法只试「最后 1–2 个词」当兵种词、其余一律当阵营词，于是
        /// `Infantry troops` 会把 `Infantry` 当成阵营名（对不上 → 落回施放者阵营），
        /// **兵种筛选静默丢掉** —— 2026-09-12 做 `deploy` 时撞到。
        ///
        /// 逐个 token：先试**两词窗口**（`combat elixir` / `genestealer cults` 这类要黏在一起），
        /// 再试单词；**兵种词优先于阵营词**（`Infantry troops` 里 `Infantry` 是兵种不是阵营）。
        /// subtype 比 type 更具体，所以同时命中时 **subtype 赢**。
        /// </summary>
        static string[] MatchKindWord(IReadOnlyList<CardDef> pool, string phrase,
                                      out string factionWord, out string kindPhrase)
        {
            factionWord = ""; kindPhrase = phrase;
            if (string.IsNullOrEmpty(phrase)) return null;

            string[] toks = phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] kindRow = null;
            var factions = new List<string>();
            var unknown = new List<string>();

            for (int i = 0; i < toks.Length; )
            {
                string two = (i + 1 < toks.Length) ? toks[i] + " " + toks[i + 1] : null;

                // ① 两词窗口当兵种词（`combat elixir`）
                int n = 1;
                string[] row = null;
                if (two != null) row = LookupKind(Singular(two));
                if (row != null) n = 2;
                else
                {
                    // ② 单词当兵种词
                    row = LookupKind(Singular(toks[i]));
                }
                if (row != null)
                {
                    // subtype 比 type 具体 —— 同时命中时 subtype 赢（`Infantry troops`）
                    if (kindRow == null || (row[1] == "subtype" && kindRow[1] != "subtype")) kindRow = row;
                    i += n;
                    continue;
                }

                // ③ 阵营词（两词窗口 `genestealer cults` / `emperor's children`，再单词）
                string fac;
                if (two != null && TryResolveFaction(pool, two, out fac)) { factions.Add(two); i += 2; continue; }
                if (TryResolveFaction(pool, toks[i], out fac)) { factions.Add(toks[i]); i += 1; continue; }

                unknown.Add(toks[i]);
                i++;
            }

            if (kindRow == null) return null;         // 没命中兵种词 → 交给按卡名查
            factionWord = string.Join(" ", factions.ToArray());
            kindPhrase = string.Join(" ", unknown.ToArray());
            if (kindPhrase.Length == 0) kindPhrase = phrase;
            return kindRow;
        }

        /// <summary>这个词是不是我们认识的**兵种词**（`vehicles` / `beasts` / `troops` …）。
        /// `lowercost` 判「`Lower the cost of all Vehicles` 里的 `Vehicles` 是兵种还是卡名」用它。</summary>
        public static bool IsKindWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            string w = word.Trim().ToLowerInvariant();
            if (LookupKind(Singular(w)) != null) return true;
            int sp = w.IndexOf(' ');
            return sp > 0 && LookupKind(Singular(w.Substring(sp + 1))) != null;
        }

        /// <summary>
        /// 这个词**是不是我们认识的类别词**（`KindWords` 里查得到）。
        ///
        /// **为什么单独开一个**（2026-09-14 A6 族 B）：条件句
        /// `If they are Battlesuits, give them Flank` / `If it is [Destroyer], give it Armour 1`
        /// 里那个词**可能是关键词、也可能是兵种**，解析层要能先判「这个词我们认不认识」——
        /// 不认识就该**判不出来**，而不是当成「成立」（那是静默打错）。
        /// ⚠️ 判据转调 `LookupKind`（和 `MatchesKind` 同一份词表，别另写一个）。
        /// </summary>
        public static bool IsKnownKind(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            string w = word.Trim().ToLowerInvariant();
            if (LookupKind(Singular(w)) != null) return true;
            int sp = w.IndexOf(' ');
            return sp > 0 && LookupKind(Singular(w.Substring(sp + 1))) != null;
        }

        static string[] LookupKind(string canon)
        {
            foreach (var row in KindWords) if (row[0] == canon) return row;
            return null;
        }

        /// <summary>
        /// 这张卡算不算<b>某一类</b>（`troop` / `vehicle` / `spell` / `sabotage` …）——
        /// **判据只此一份**，候选池筛（`Resolve`）和定向翻找（`RuleCore.DoDrawType`）共用它。
        ///
        /// 切词和前缀同 `MatchKindWord`：先试两词（`combat elixir`），再试一词，剥一个尾 `s`。
        /// </summary>
        public static bool MatchesKind(CardDef c, string kindWord)
        {
            if (c == null || string.IsNullOrEmpty(kindWord)) return false;
            string w = kindWord.Trim().ToLowerInvariant();
            var row = LookupKind(Singular(w));
            if (row == null)
            {
                int sp = w.IndexOf(' ');
                if (sp > 0) row = LookupKind(Singular(w.Substring(sp + 1)));
            }
            if (row == null) return false;
            // `type` 这一维**也支持多值**（2026-09-14 A4 批 4）—— 应 `stratagem` 那一行
            // （`tactic` **或** `defence`）。逐个比，和 `SubtypeIn` 同一套判法，只是比 `CardDef.Type`。
            if (row[1] == "type")
            {
                for (int i = 2; i < row.Length; i++)
                    if (string.Equals(c.Type, row[i], StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            return SubtypeIn(c, row, 2);
        }

        /// <summary>只剥**一个**结尾的 `s`（`vehicles` → `vehicle`）。`battlesuit` 这种自带 s 的词
        /// 在词表里对不上时，`Singular` 不剥，所以不会被误伤。</summary>
        static string Singular(string w)
        {
            w = w.Trim().ToLowerInvariant();
            if (w.Length > 2 && w.EndsWith("s") && !w.EndsWith("ss")) w = w.Substring(0, w.Length - 1);
            return w;
        }

        // ==================================================================
        //  阵营词
        // ==================================================================

        /// <summary>
        /// 卡面写的**种族名** → 卡池里的**阵营名**。只有实测有据的才收进来。
        ///
        /// `ork → Goff`（兽人 → 高夫兽人）的证据链有三条，缺一条都不敢收：
        ///   ① 规则书附录 B 把 `比你们快（Fasta Than Yooz）`列在**「高夫兽人」**那一行；
        ///   ② 该卡的 `faction` 就是 `Goff`，卡面写 `Create three random Ork Vehicles`；
        ///   ③ 附录 B 说它生成「**14 种**」载具，而 `Goff` 的载具实测**正好 14 张**。
        /// 剩余 12 个阵营名在卡池里都能**直接**对上（`Saim-Hann` → `SaimHann` 只差连字符）。
        /// </summary>
        static readonly string[][] FactionAliases =
        {
            new[] { "ork", "Goff" },
            new[] { "orks", "Goff" },
            // `Genestealer Cults`（基因窃取者教派）→ 卡池里的 `Genestealers`。
            // 证据：附录 B 把「召唤教派（Summon the Cult）」列在**基因窃取者**那一行，写「2 个 2 费部队
            // （**5 种**）」；`Deploy 2 random 2-cost Genestealer Cults troops` 按卡池算**正好 5 张**，
            // 且与附录 C「召唤教派/变形偶像持旗者（1d5）」的 5 个名字逐个对上。
            new[] { "genestealer cults", "Genestealers" },
            new[] { "genestealer cult", "Genestealers" },
        };

        /// <summary>
        /// 阵营词 → 卡池里的阵营名。**对不上返回 false**（调用方决定是落回施放者阵营还是当成认不出的词）。
        ///
        /// 查法：① 与卡池里出现过的阵营名**归一化后相等**（`Saim-Hann` → `SaimHann`、
        /// `Emperor's Children` → `EmperorsChildren`，只差标点）；② 别名表（有据的种族名 → 阵营名）。
        /// </summary>
        static bool TryResolveFaction(IReadOnlyList<CardDef> pool, string word, out string faction)
        {
            faction = null;
            if (string.IsNullOrEmpty(word)) return false;
            string want = Norm(word);
            if (want.Length == 0) return false;

            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                {
                    var c = pool[i];
                    if (c == null || string.IsNullOrEmpty(c.Faction)) continue;
                    if (Norm(c.Faction) == want) { faction = c.Faction; return true; }
                }
            foreach (var pair in FactionAliases)
                if (Norm(pair[0]) == want) { faction = pair[1]; return true; }
            return false;
        }

        /// <summary>
        /// 阵营词 → 阵营名。**空串 = 按施放者自己的阵营**（规则书附录 B：每个生成器生成自己阵营的卡）。
        /// 词认不出来时也落回施放者阵营，但**会写进 `Detail`**（不静默）。
        /// </summary>
        static string ResolveFaction(IReadOnlyList<CardDef> pool, string word, string casterFaction, CreatePoolResult r)
        {
            if (string.IsNullOrEmpty(word)) return casterFaction;
            string f;
            if (TryResolveFaction(pool, word, out f)) return f;

            r.Detail = AppendDetail(r.Detail,
                "阵营词「" + word + "」在卡池里对不上 → 按施放者阵营「" + casterFaction + "」算");
            return casterFaction;
        }

        /// <summary>费用区间写进日志（`that cost 4 or less` / `2-cost`）</summary>
        static string CostScope(int min, int max)
        {
            if (min == 0 && max == 0) return "";
            if (min == max) return " + 恰好 " + min + " 费";
            if (max > 0) return " + " + max + " 费及以下";
            return " + " + min + " 费及以上";
        }

        static bool SameFaction(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // ==================================================================
        //  附录 C 的骰子查找表 —— **只用来对账**，不是运行时数据源
        // ==================================================================
        //
        // 出处：`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md` `:289-320`。
        // 为什么不当数据源：它是**实体版**的候选名单，里面有点名了但数字版卡池里没有的卡
        // （`Gauss Warrior` / `Tesla Immortal` / `Threnodic Noise Marine` / `Icon of Excess`）；
        // 而按名字造卡必须先有 `CardDef`。所以拿它**对账**：卡池算出来的池子该和它一致。
        //
        // 只收了**背后真的有 `Create …` 分句**的那 4 张表 —— 别的表（放牧者/纵欲狂欢/
        // 星界军战术…）对应的是单位卡上的生成效果，战术卡文本里没有，收进来没人用。

        /// <summary>表名 → 候选名单（英文卡名）。表名里的 `(1dN)` 是实体版掷骰的骰子。</summary>
        public static readonly string[][] DiceTables =
        {
            //  战斗药剂（1d6）—— 背后：`Create 3 random Combat Elixir in your hand`（Mercurial Host）等 5 张
            new[] { "战斗药剂(1d6)",
                    "Skorflense", "Xylocil", "Shivversplint", "Heliotrophos", "Quail", "Antrak Silk" },
            //  钛无人机·崇高牺牲（1d7）—— 背后：`Create in your hand a Gun Drone, Guardian Drone or Marker Drone`
            new[] { "钛无人机(1d7)",
                    "Guardian Drone", "Missile Drone", "Gun Drone", "Marker Drone",
                    "Stealth Drone", "Sniper Drone", "DS8 Support Turret" },
            //  虫群大军（1d10）—— 背后：`Create 3 random Leviathan troops with Swarm in your hand`
            new[] { "虫群大军(1d10)",
                    "Ripper Swarm", "Neurogaunt", "Neurogaunt Nodebeast", "Termagant", "Hormagaunt",
                    "Genestealer", "Gargoyle", "Barbgaunt", "Von Ryan's Leaper", "Termagant Brood" },
            //  锈蚀通风口（1d11）—— 背后：`Create a random troop with Ambush in your hand`
            new[] { "锈蚀通风口(1d11)",
                    "Concealed Explosives", "Neophyte Hybrid", "Neophyte Heavy", "Acolyte Hybrid",
                    "Jackal Outrider", "Acolyte Heavy", "Acolyte Specialist", "Sanctus",
                    "Hulking Aberrant", "Abominant", "Patriarch" },
            //  狂野宿主·灵族载具（1d20）—— 背后：`Create two random Saim-Hann Vehicles in your hand`
            new[] { "狂野宿主(1d20)",
                    "Weapons Platform", "Windrider", "Shroud Runner", "Shining Spear", "Vyper",
                    "Nuadhu Fireheart", "Shining Spear Exarch", "Bright Lance Vyper", "Warlock Skyrunner",
                    "War Walker", "Hornet", "Farseer Skyrunner", "Crimson Hunter", "Falcon",
                    "Fire Prism", "Wave Serpent", "Night Spinner", "Hemlock Wraithfighter",
                    "Wraithlord", "Wraithknight" },
            //  次元裂隙（1d6）—— 背后：`Deploy 3 random 2-cost Sautekh troops`
            //  ⚠️ 书里的 `Gauss Warrior` / `Tesla Immortal` 在卡池里叫
            //     `Gauss Reaper Warrior` / `Tesla Carbine Immortal`（中间多一个词）—— 名字对不上，如实报
            new[] { "次元裂隙(1d6)",
                    "Flayed One", "Gauss Warrior", "Necron Warrior", "Canoptek Plasmacyte",
                    "Tesla Immortal", "Damaged Plasmacyte" },
            //  纵欲狂欢·恶魔（1d5）—— 背后：`Deploy three random Emperor's Children Daemons that cost 5 or less`
            //  ⚠️ 书里这 5 张都算恶魔，但卡池里 `Daemonette` / `Alluress` 的 subtype 写的是 `Troop`
            //     —— 按 `subtype == Daemon` 筛只有 3 张。数据标得对不对**没核**，如实报。
            new[] { "纵欲狂欢(1d5)",
                    "Daemonette", "Alluress", "Slaanesh's Spawn", "Fiend", "Blissbringer" },
            //  空中播种/泰伦入侵（1d5）—— 背后：`Deploy 4 random 2-cost Leviathan troops`
            new[] { "空中播种(1d5)",
                    "Neurogaunt Nodebeast", "Hormagaunt", "Termagant", "Stranded Termagant",
                    "Mucolid Spore" },
        };

        // ⚠️ **2026-09-12 更正**：这里原来还有一张 `DiceGaps` 表，手写着「骰子表点名了、但卡池里没有」
        //    的卡（`Gauss Warrior` / `Tesla Immortal` / `Icon of Excess` / `Threnodic Noise Marine`）。
        //    **那四条有三条是错的** —— 卡都在，只是数字版名字更长：
        //    `Icon of Excess` → `Icon of Excess Infractor`、`Gauss Warrior` → `Gauss Reaper Warrior`、
        //    `Tesla Immortal` → `Tesla Carbine Immortal`、`Threnodic Noise Marine` → `Threnodic Choir Noise Marine`。
        //    错因和上一轮那张「原版数据里没有这张卡」的名单**一模一样**：拿短名字做精确匹配，
        //    对不上就当成「没有」。**手写的缺口清单一定会漂** —— 所以删掉，
        //    改成每次自检**算**出来（`RuleEngineTest.CheckDiceTables`，差集两个方向都报）。
    }
}
