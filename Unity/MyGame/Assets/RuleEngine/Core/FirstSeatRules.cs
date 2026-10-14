// FirstSeatRules.cs — **谁先手**：原版 `SupportMethods.GetPlayerGoesFirstWithInitiative` 的引擎侧判据
//
// ══════════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-14（账 `A1775`）**：先后手原来在 `BattleDriver` 里**只掷硬币**
//     （`:2985-2993` 那句注释写着「所有督军 `initiative` 相同」）——
//     那是 **2026-09-26 的一次【用户拍板】**（「投硬币更公平、不需要为每个督军设先攻值」），
//     **不是原版事实**。**用户 2026-10-14 改成「全部照原版复刻」** ⇒ 补上原版那一段。
//
//  **判据（第一权威 = 反编译方法体，`d:/2/tools/decomp_full/`，逐句实读）**
//
//  ① 顶层分派 `BattleManager__GetPlayerGoesFirst.c`：
//       `matchType == 80 (0x50)`  ⇒ return true            （EventAI：**我方**恒先手）
//       `matchType == 100`         ⇒ 教程：`currentTutorialStage.playerStarts`（`+0x28`）
//       `matchType ∈ {130(0x82), 140(0x8c), 190(0xbe)}` ⇒ 战役：`currentCampaignStage.playerStarts`
//       其余                       ⇒ 走下面 ②③④⑤
//  ② `SupportMethods__GetPlayerGoesFirstWithInitiative.c`（比先攻值那一支的真身）：
//       `a = MatchData.playerRawBattleData (+0x58) → +0x18 → +0x300 (0x12C = initiative)`
//       `b = MatchData.enemyRawBattleData  (+0x50) → +0x18 → +0x300`
//       `if (matchType == 20 && GetBotConfigFTUEShouldUseFirstGameLogic()) return true;`
//       `if (a > b) return true;`      // **玩家**（座位 0）先手
//       `if (b > a) return false;`     // 对手先手
//  ③ 平局缓存：`MatchData.forceFirstPlayer (+0x64) != 0 ⇒ return forceFirstPlayer == 1`
//  ④ `MatchData.playerIdGoesFirst (+0x48)` 非空（PvP）⇒ 与本地玩家 id 比 ⇒ 定先后手
//  ⑤ 都没有 ⇒ 掷硬币：`MatchData.playerGoesFirstRandomInt (+0xE8)` 与
//       `firstPlayerInPvProom (+0x60)`（`GetRandomInt(0,2)` 抽的那个偶数/奇数）
//
//  ⇒ **本文件只做 ②**（③④⑤ 的「掷硬币」那一步已经在 `BattleDriver.FirstSeatForSeed` 里，
//     且它**必须留** —— 它正是原版的 ⑤；③④ 的前两级在我们这边由「调用方显式指定
//     / 自检钉子」承担，联机那条路走的也是显式指定）。
//
// ══════════════════════════════════════════════════════════════════════════════════
//  🔴 **现读数据的实况（这一条比代码更重要，别当成「没做」）**
//     真值 = `数据/游戏数据/card_initiative.json`（原版 `allcards` 快照 2025-07-30）：
//       · 全表 **1190 行 = `20`(Medium)** · **2 行 = `30`(High)** · 3 行缺这个键；
//       · 那 **2 行**是 `Marneus Calgar (tutorial)` / `Uriel Ventris (tutorial)`
//         —— **教程装订的督军**，走的是上面 ① 的**教程分支**（`TutorialStage.playerStarts`），
//         **永远到不了 ②**；
//       · 我们池子（1126 张）里 `initiative` 接上真值 **955 张、全是 `20`**，其余 171 张记 `-1`。
//     ⇒ **我们池子里这 56 位督军全是 `20`** ⇒ ② 这一段**必然平局**、落到 ⑤ 掷硬币 ——
//       这就是「按真值判先后手」在今天这份数据下的**真实结果**，**与原版逐字一致**
//       （不是「我们没实现」）。⛔ 别为了「让先后手有差别」去编一个先攻值分布（铁律 2）。
//
//  ⚠️ 本文件在 `Core/` 下 ⇒ **不碰 UnityEngine**（只许 `Debug` / `Random`，这里连它们都不用）——
//     见 `Editor/RuleEngineTest.cs` 的 `TestCoreLayerPurity`。
using System;

namespace RuleEngine
{
    /// <summary>谁先手：原版「督军 `initiative` 高者先手」那一段（判据全文见文件头）。</summary>
    public static class FirstSeatRules
    {
        /// <summary>督军的先攻值；**`-1` = 查不到**（调用方与 <see cref="ByInitiative(int,int)"/>
        /// 一起用，别自己判）。</summary>
        /// <param name="lookup">`CardDatabase.DeckLookup(pool, faction)` 那种查表口。
        /// 传 `null` / id 空 / 查不到 ⇒ `-1`。</param>
        public static int WarlordInitiative(Func<string, CardDef> lookup, string warlordId)
        {
            if (lookup == null || string.IsNullOrEmpty(warlordId)) return -1;
            var c = lookup(warlordId);
            // ⚠️ 非督军卡也照取：原版读的就是「那一方那副牌的督军那张 SO」的字段，
            //    我们这里只有**卡**这一层，判「它是不是 hero」是 `Validate` 的活，不是本判据的活。
            if (c == null) return -1;
            return c.Initiative < 0 ? -1 : c.Initiative;
        }

        /// <summary>**比先攻值** —— 原版 `SupportMethods.GetPlayerGoesFirstWithInitiative.c` 那两行
        /// （`if (player > enemy) return true; if (enemy > player) return false;`）。
        ///
        /// <returns>`0` = **座位 0（玩家 / `deckA`）先手**；`1` = 座位 1（对手）先手；
        /// **`null` = 判不了** ⇒ 调用方**继续走原版剩下的 ③④⑤**
        /// （我们那边就是原来的掷硬币 `BattleDriver.FirstSeatForSeed`）。
        /// ⛔ **不许把 `null` 当成「平局就玩家先手」** —— 那会变成一个原版没有的偏置。</returns>
        /// <para>`null` 的两种来源要分得清：**平局**（`a == b`，原版真会走到 ③④⑤）与
        /// **缺真值**（`a < 0 || b < 0`，我们这边真值快照比现池旧 —— 171 张没有值）。
        /// 两者在本判据里**处置相同**（都交给调用方），但**含义不同**，排查时别混。</para>
        /// </summary>
        public static int? ByInitiative(int playerInit, int enemyInit)
        {
            if (playerInit < 0 || enemyInit < 0) return null;   // 缺真值 ⇒ 不比（⛔ 不猜）
            if (playerInit > enemyInit) return 0;               // 原版 `return true`
            if (enemyInit > playerInit) return 1;               // 原版 `return false`
            return null;                                        // 平局 ⇒ 原版 ③④⑤
        }

        /// <summary>按**卡**比（`a` = 座位 0 那一方的督军，`b` = 座位 1 的）。
        /// 任一张为 `null` / 它的 <see cref="CardDef.Initiative"/> `< 0` ⇒ `null`。</summary>
        public static int? ByInitiative(CardDef a, CardDef b)
        {
            if (a == null || b == null) return null;
            return ByInitiative(a.Initiative, b.Initiative);
        }
    }
}
