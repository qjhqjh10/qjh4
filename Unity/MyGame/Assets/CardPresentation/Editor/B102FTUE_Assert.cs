// B102FTUE_Assert.cs — 执行代理 `B102` / 账 `战斗逻辑驱动:48`
// （原版 `AIBotsConfig.BotConfigFTUE.GetBotDecks()`）的断言。
//
// 判据（⛔ 不拿我们的常量证明我们的实现）：
//   · 原版 `BotConfigFTUE.botDecks` = 序列化字段（`+0x10`），`GetBotDecks()` 直接返回它
//     （`out_cpp2il_dev_ilspy/AIBotsConfig.cs:46-66`；⚠️ `decomp_full/AIBotsConfig.BotConfigFTUE__GetBotDecks.c`
//     的**内容不是它**（首行 = `<>f__AnonymousType0<object>__get_itemId`）⇒ ⛔ 别拿那份当判据）。
//   · 那张表本身 = **远端 LiveOps 配置**，本机 0 命中（账行 48 的搜证，可复跑）；
//     但它指向的牌本机全在：`bundle_prebuiltdecks` 的 12 副 `*_BOT_FTUE_*` 已在
//     `RuleEngine/Resources/prebuilt_deck_lists.json`（`Type == Bot`，12 副 = 6 阵营 × `_Deck1` / `_DeckEasy`）。
//   ⇒ 被测的是「**池的成员来自数据文件、且是那 12 副**」，不是「我们写死了一张 12 副的表」。
//
// 用法（宿主里两行，想并进总数就带后一行；本组**不依赖场景**，可在任意宿主里跑）：
//
//     B102FTUE_Assert.Run();
//     pass += B102FTUE_Assert.Pass; fail += B102FTUE_Assert.Fail;
//
// 也可以单独跑：`-executeMethod B102FTUE_Assert.Run`
//
// ⚠️ **前提（不满足 = 红、⛔ 不是跳过）**：`Resources/prebuilt_deck_lists.json` 必须已在工程里
//   （`PrebuiltDecks.Decks` 读得到，与 `BattleDriver.PickBotDeck` 同一条数据链）。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B102FTUE_Assert
{
    const string P = "[B102FTUE] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            // ---------- A0 硬前提（三条都必须先真；⛔ 别写成 `if (池非空) 断言`）----------
            if (!Ok(PrebuiltDecks.Decks.Count > 0,
                    "（A0 前提）`prebuilt_deck_lists.json` 读得到（Decks=" + PrebuiltDecks.Decks.Count + " 副）"))
                return;     // 表本身没有 ⇒ 下面每条都会是「拿不到」的假红，直接收口
            int botCount = PrebuiltDecks.BotDecks.Count;
            if (!Ok(botCount > 0, "（A0 前提）`BotDecks`（原版 `Type == Bot` 那一半）非空（现读 " + botCount + " 副）"))
                return;

            var pool = PrebuiltDecks.GetFTUEBotDecks();
            if (!Ok(pool != null, "（A0 前提）`GetFTUEBotDecks()` 返回非 null"))
                return;

            // ---------- A1 ★ 池 = 12 副（原版配置指向的那一批）----------
            Ok(pool.Count == 12, "（A1 ★）FTUE bot 池 = 12 副（原版 12 副 `*_BOT_FTUE_*`）· 现读 " + pool.Count);

            // ---------- A1b 每副都拼得齐、且每张卡 id 真在卡池里 ----------
            var cards = CardDatabase.Load();
            if (!Ok(cards != null && cards.Count > 0, "（A1b 前提）卡池读得到（现读 " + ((cards != null) ? cards.Count : 0) + " 张）"))
                return;
            var idx = CardDatabase.IdIndex(cards);
            int badComplete = 0, badCard = 0, badHero = 0, badZero = 0;
            var badCardSample = "";
            foreach (var d in pool)
            {
                if (!d.complete) badComplete++;                       // 原版 `!HasHiddenCards`
                if (d.cardIds == null || d.cardIds.Length == 0) { badZero++; continue; }
                foreach (var cid in d.cardIds)
                    if (string.IsNullOrEmpty(cid) || !idx.ContainsKey(cid))
                    {
                        badCard++;
                        if (badCardSample.Length == 0) badCardSample = (d.deckId + " → " + cid);
                    }
                if (string.IsNullOrEmpty(d.heroId) || !idx.ContainsKey(d.heroId)) badHero++;
            }
            Ok(badComplete == 0 && badZero == 0,
               "（A1b）池里每一副都 `complete` 且牌表非空（不齐 " + badComplete + " · 空牌 " + badZero + "）");
            Ok(badCard == 0,
               "（A1b）池里每一张卡 id 都在卡池里（查不到 " + badCard + " 张"
               + (badCardSample.Length > 0 ? " · 例 " + badCardSample : "") + "）");
            Ok(badHero == 0, "（A1b）池里每一副的督军 id 都在卡池里（查不到 " + badHero + " 副）");

            // ---------- A1c 池是 `BotDecks` 的子集（原版 `botDecks` 挂在 `BotConfig` 下）----------
            int notBot = 0;
            foreach (var d in pool)
            {
                var m = PrebuiltDecks.MetaOf(d);
                if (m == null || m.type != PrebuiltDecks.DeckTypeBot) notBot++;
            }
            Ok(notBot == 0, "（A1c）池里每一副的 `MetaOf().type == Bot(0)`（不是 Bot 的 " + notBot + " 副）");

            // ---------- A1d 按阵营收窄那一格与池自洽（每个阵营 ≥1，且总数不丢不重）----------
            var factions = new List<string>();
            foreach (var d in pool) if (!factions.Contains(d.faction)) factions.Add(d.faction);
            int sum = 0, emptyFaction = 0;
            foreach (var f in factions)
            {
                int n = PrebuiltDecks.GetFTUEBotDecksOf(f, 0).Count;
                sum += n;
                if (n == 0) emptyFaction++;
            }
            Ok(factions.Count > 0 && emptyFaction == 0 && sum == pool.Count,
               "（A1d）`GetFTUEBotDecksOf(faction, Classic)` 与池自洽：阵营 " + factions.Count + " 个 · 合计 " + sum
               + " = 池 " + pool.Count + " · 空阵营 " + emptyFaction);

            // ==================================================================
            //  A2 · 🧨 灭自证 —— 四条「结构上不可能同时作弊」
            //
            //  🧨 改法（谁把哪条改红说明什么）：
            //    · **A2a**：把实现换成「自己 `new Deck{...}` 拼 12 副」⇒ 池里每一副都**不是** `Decks` 里的那个实例 ⇒ 红。
            //      （⇒ 池必须是数据表的**过滤视图**，不是另一份手抄表。）
            //    · **A2b**：把 `IsFtueBotDeck` 改成恒 `true`（让它"看起来"能筛）⇒ 池变 196 副 ⇒ A1 红；改成恒 `false` ⇒
            //      A2b 的「是的那一侧」红。**A1 与 A2b 因此不可能一起作弊**。
            //    · **A2c**：给「池里没有该阵营」的情况加个兜底（随便拿池里第一副）⇒ 红 —— 那样对手会拿到错阵营的牌组。
            //    · **A2d**：把随机取副换回「永远取 `pool[0]`」⇒ 红的不是 A1（条数对），是这条覆盖率断言。
            //      反过来，若有人为了让它绿去改池的顺序 ⇒ A1d 跟着动。
            // ==================================================================
            // A2a 引用同一性
            var deckSet = new List<PrebuiltDecks.Deck>(PrebuiltDecks.Decks);
            int notSame = 0;
            foreach (var d in pool)
            {
                bool found = false;
                for (int i = 0; i < deckSet.Count; i++)
                    if (ReferenceEquals(deckSet[i], d)) { found = true; break; }
                if (!found) notSame++;
            }
            Ok(notSame == 0,
               "（A2a 🧨）池里每一副都是 `Decks` 里的**同一个实例**（filter 视图，不是新建的 Deck）· 不是的 " + notSame + " 副");

            // A2b 判定与池 ⇔ 一致（双向）
            int inPoolNotFlagged = 0, flaggedNotInPool = 0;
            foreach (var d in PrebuiltDecks.BotDecks)
            {
                bool inPool = false;
                for (int i = 0; i < pool.Count; i++) if (ReferenceEquals(pool[i], d)) { inPool = true; break; }
                bool flagged = PrebuiltDecks.IsFtueBotDeck(d);
                if (inPool && !flagged) inPoolNotFlagged++;
                if (!inPool && flagged) flaggedNotInPool++;
            }
            Ok(inPoolNotFlagged == 0 && flaggedNotInPool == 0,
               "（A2b 🧨）`BotDecks` 逐副：在池里 ⇔ `IsFtueBotDeck` 为真（池内漏判 " + inPoolNotFlagged
               + " · 池外误判 " + flaggedNotInPool + "）");

            // A2c 负例：池里没有该阵营 ⇒ null（不跨阵营兜底）。先断言「这种阵营真的存在」再断言结果。
            string otherFaction = null;
            foreach (var d in PrebuiltDecks.BotDecks)
                if (d != null && !string.IsNullOrEmpty(d.faction) && !factions.Contains(d.faction)) { otherFaction = d.faction; break; }
            if (Ok(otherFaction != null, "（A2c 前提）`BotDecks` 里存在「没有 FTUE 副」的阵营（现读有这类阵营）"))
            {
                // 前提不成立 ⇒ 上面那条已经是红，下面两条不跑（⛔ 不静默跳过：红已经记在 A2c 前提上）
                var neg = PrebuiltDecks.GetFTUEBotDeck(otherFaction, 0, 0, new System.Random(11));
                Ok(neg == null, "（A2c 🧨）没有 FTUE 副的阵营「" + otherFaction + "」⇒ 返回 null（⛔ 不跨阵营兜底）");
                Ok(PrebuiltDecks.GetFTUEBotDecksOf(otherFaction, 0).Count == 0,
                   "（A2c）`GetFTUEBotDecksOf(" + otherFaction + ")` 也是 0 副（与上面那条同一口径）");
            }

            // A2d 确定性 + 覆盖：同 seed 同结果；多个 seed 能覆盖到池里每一副（排除「永远取第 0 副」）
            if (factions.Count > 0)
            {
                string f0 = factions[0];
                var p0 = PrebuiltDecks.GetFTUEBotDecksOf(f0, 0);
                var a = PrebuiltDecks.GetFTUEBotDeck(f0, 0, 0, new System.Random(7));
                var b = PrebuiltDecks.GetFTUEBotDeck(f0, 0, 0, new System.Random(7));
                Ok(a != null && b != null && a.deckId == b.deckId,
                   "（A2d）同一个 seed ⇒ 同一副（" + (a != null ? a.deckId : "null") + " vs " + (b != null ? b.deckId : "null") + "）");

                var seen = new List<string>();
                for (int s = 0; s < 32; s++)
                {
                    var k = PrebuiltDecks.GetFTUEBotDeck(f0, 0, 0, new System.Random(s));
                    if (k != null && !seen.Contains(k.deckId)) seen.Add(k.deckId);
                }
                Ok(seen.Count == p0.Count && p0.Count > 0,
                   "（A2d 🧨）32 个 seed 覆盖到「" + f0 + "」FTUE 池的**每一副**（见到 " + seen.Count
                   + " / 池 " + p0.Count + " 副）⇒ 不是永远取第 0 副");
            }
            else
            {
                Ok(false, "（A2d 前提）池里一个阵营都没有 ⇒ 随机取副那几条无从谈起（**算红**）");
            }
        }
        catch (Exception e)
        {
            Ok(false, "断言抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== 账 :48 FTUE bot 池 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }
}
