// BattleContextPlayedThisTurn.cs — `BattleContext` 的「本回合出过什么牌」那一格（`partial`，与 `BattleContext.cs` 同一个类）
//
// 🔴 2026-10-27（执行代理 `A2` · C 档施工批次 `B04`）新开这个文件的原因
//   （与 `BattleContextRandom.cs` / `BattleContextTurnCounter.cs` 同一条纪律 —— 铁律 18·b）：
//   `BattleContext.cs` 被账按 `文件:行号` 引用（二十多处落点），**任何加行的动作**都会让下游落点整体漂移
//   ⇒ 新成员放进**同一个类的另一个 partial 文件**，源文件一行不动。
//
// ==================================================================
//  原版的「本回合出过牌 / 出过部队」= 四格 bool（`BattleManager` 上）
// ==================================================================
//
//  判据（第一权威 · 现读 `d:/2/tools/decomp_full/`）：
//   · 读者 = `BattleManager__AlreadyPlayedTroopThisTurn.c:1-12`（全文）：
//       `isPlayer ? this+0x242 : this+0x243`；同族的 `BattleManager__AlreadyPlayedCardThisTurn.c`
//       读的是 `+0x240` / `+0x241`（`playerPlayerCardInTurn` / `enemyPlayerCardInTurn`）。
//   · 唯一的写点（全量反编译逐条核过）= `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1430-1455`
//       `card.isPlayer(+0x40) == 0`（= 敌方那张）⇒ `+0x241 = 1`，且 `RawCardScript.IsUnit(card)` ⇒ `+0x243 = 1`；
//       否则（我方那张）⇒ `+0x240 = 1`，且 `IsUnit` ⇒ `+0x242 = 1`。
//   · `IsUnit` 的口径 = `RawCardScript__IsUnit.c:1-12`：`cardType(+0x94) == 10`（hero）**或** `== 0`（minion）
//       ⇒ 与我们 `CardDef.IsUnit`（`Type == "unit" || "hero"`）逐项等价。
//
//  ⚠️ **判据的一处真空（本代理 2026-10-27 现查，⛔ 别当成「已证实随回合清零」）**：
//     `grep -rn "0x242\|0x243" decomp_full/` 只有 6 个文件 —— 两个属性 get/set 桩、读者本身、
//     写点那一支、外加 `xxHash`（反作弊哈希表，无关）；**没有任何一处把这四格写回 0**；
//     属性 setter（`set_playerPlayerTroopInTurn` / `set_playerPlayerCardInTurn`）在全量反编译里
//     **0 个调用者**（写点那一支是 IL 内联的直写）。
//     ⇒ 原版这四格**可能其实从不复位**（= 「本局」而不是「本回合」），只是名字叫 InTurn；
//       也可能是复位点落在**没有方法体**的 724 条里（反编译器没产出）。
//     ⇒ 本实现按**施工单要求的口径（本回合 = `ctx.Turn` 相等）**做（`判据/B9_剩余C_施工单.tsv` 第 26 行
//       施工步骤 ②「`RuleCore.BeginTurnCore` 清零两格」），并**不宣称**那等于已证实的原版行为。
//       消费端今天全池 0 调用点（`grep AlreadyPlayedTroopThisTurn` 只命中本文件与账），可观察后果为零。
//
// ==================================================================
//  为什么是「按出牌流水现算」而不是「加四格 bool + 两个写点」
// ==================================================================
//
// 施工单原本写的是「`BattleContext` 加 `bool[2] TroopsPlayedThisTurn` + `RuleCore.BeginTurnCore` 清零 +
// `RuleCore.PlayCard` 置 true」。本代理改成**从已有的出牌流水 `PlayedCards` 现算**，理由三条：
//   ① **`PlayedCards` 的写点就是原版那唯一的写点** —— `NotePlayed` 只由
//      `RuleCore.PlayCard`（单位卡，`RuleCore.cs:4104`）与 `EffectResolver.PlayTactic`（战术卡，
//      `EffectResolver.cs:2619`）调，正对 `_ResolvePlayCardFromHand`；表里带 `Owner` 与 `Turn`
//      ⇒ 「本回合 · 这一方 · 打出的单位卡」是可以精确算出来的，**不需要另立状态**；
//   ② 施工单那两个写点在 `RuleCore.cs` —— **不在本批的白名单**（本批独占 `BattleContext.cs` + `PlayerState.cs`）；
//      而现算版**一个写点都不需要**，`RuleCore` 一行不动 ⇒ 不与其他批撞车、也不改任何既有行为；
//   ③ 少两格状态就少两处「写入点漏挂钩子」的静默缺陷面（CLAUDE.md §三 那条血泪）。
//
//  🔶 自己设计（非原版）：**表示法**（按 `PlayedCards` 现算 vs 原版四个 bool 格）· 为什么必须自己定：
//     原版那四格的复位点在全量反编译里**查不到**（见上「判据的一处真空」），而且写点在 `RuleCore`（本批白名单外）·
//     调度台 2026-10-27 简报裁定「B04 独占 `BattleContext.cs` + `PlayerState.cs`」。
//     ⇒ **行为**按施工单口径（`ctx.Turn` 相等 = 本回合）；⚠️ 与原版「可能从不复位」的差异已在上文写明。
//
//  ⚠️ 别拿 `ctx.TroopsPlayed[side]` 顶这一格：那是**本局**累计张数（写点 `RuleCore.cs:4001` 出牌、
//     `EffectResolver.cs:3909` 效果部署的复制品），既不分回合、也把「效果部署」算进去了。
//  ⚠️ 也别拿 `ctx.CardsPlayedInTurn` 顶：那一格是**全局**（不分方）的「本回合打出张数」，
//     对应原版 `+0x3D0`（`BattleManager.CardsPlayedInTurn`），是另一个量。
namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>
        /// 原版 `BattleManager.AlreadyPlayedTroopThisTurn(bool isPlayer)`
        /// （`d:/2/tools/decomp_full/BattleManager__AlreadyPlayedTroopThisTurn.c:1-12` 全文 12 行）。
        ///
        /// <para>「**这一方在本回合（= `ctx.Turn`）有没有打出过单位卡**」。单位卡口径 = `CardDef.IsUnit`
        /// （`unit ∨ hero`，与 `RawCardScript__IsUnit.c` 的 `cardType ∈ {0, 10}` 逐项等价）。</para>
        ///
        /// <para>**只算「从手牌打出」**：数据源 `PlayedCards` 的写点只有 `RuleCore.PlayCard` 与
        /// `EffectResolver.PlayTactic`（见文件头）；`DeployFree` / 潮涌复制品 / 伴生卡 / 效果造出来的
        /// 单位**都不进这张表** —— 与原版 `_ResolvePlayCardFromHand` 那一支同口径。</para>
        ///
        /// <para>`side` = 座位号（`0` / `1`，与 `Players[]` / `TroopsPlayed[]` 同一个下标）。</para>
        /// </summary>
        public bool AlreadyPlayedTroopThisTurn(int side)
        {
            // `PlayedCards` 按时间追加且 `Turn` 单调不减 ⇒ 从尾往头扫，一遇到**别的回合**就可以停。
            for (int i = PlayedCards.Count - 1; i >= 0; i--)
            {
                var c = PlayedCards[i];
                if (c.Turn != Turn) break;                 // 更早的都属上一个回合（单调 ⇒ 可以早退）
                if (c.Owner != side) continue;
                var def = c.Card;
                if (def != null && def.IsUnit) return true;
            }
            return false;
        }

        /// <summary>原版同名签名的孪生口（`isPlayer` 版，`AlreadyPlayedTroopThisTurn.c:5-7`）：
        /// `isPlayer == true` ⇒ 座位 `0`（本体玩家），否则座位 `1`。
        /// ⚠️ 引擎内部一律用 <see cref="AlreadyPlayedTroopThisTurn(int)"/>（座位直给，联机两侧都是「玩家」）；
        /// 这个重载只为「照着原版签名对接」时不必自己换座位号。</summary>
        public bool AlreadyPlayedTroopThisTurn(bool isPlayer)
        {
            return AlreadyPlayedTroopThisTurn(isPlayer ? 0 : 1);
        }

        /// <summary>
        /// 原版 `BattleManager.AlreadyPlayedCardThisTurn(bool isPlayer)`
        /// （`d:/2/tools/decomp_full/BattleManager__AlreadyPlayedCardThisTurn.c:1-8`，
        /// 读 `+0x240` / `+0x241`；写点同 `AlreadyPlayedTroopThisTurn` —— 一次出牌两张卡族各置一位）。
        ///
        /// <para>「**这一方在本回合有没有打出过牌（任意种）**」—— 单位卡与战术卡**都算**
        /// （原版那一支在判 `IsUnit` **之前**就无条件置位）。</para>
        ///
        /// <para>⚠️ 与 `ctx.CardsPlayedInTurn`（原版 `+0x3D0`）**不是同一个量**：那一格是**全局**张数、
        /// 不分方；本方法是**按方**的 bool。</para>
        /// </summary>
        public bool AlreadyPlayedCardThisTurn(int side)
        {
            for (int i = PlayedCards.Count - 1; i >= 0; i--)
            {
                var c = PlayedCards[i];
                if (c.Turn != Turn) break;
                if (c.Owner == side) return true;
            }
            return false;
        }

        /// <summary>原版同名签名的孪生口（`isPlayer == true` ⇒ 座位 0）。见
        /// <see cref="AlreadyPlayedCardThisTurn(int)"/>。</summary>
        public bool AlreadyPlayedCardThisTurn(bool isPlayer)
        {
            return AlreadyPlayedCardThisTurn(isPlayer ? 0 : 1);
        }
    }
}
