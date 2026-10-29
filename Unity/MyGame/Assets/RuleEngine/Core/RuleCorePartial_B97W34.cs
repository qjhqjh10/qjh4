// RuleCorePartial_B97W34.cs — 第九十七会话 · 波 4-A · 执行代理 `B97_W34` 的 `RuleCore` partial 扩展
//
// 🔴 为什么单开文件：`RuleEngine/Core/RuleCore.cs` 被 `账/*.tsv` 按【行号】引用 400+ 处 ⇒
//   往它中间插成员会让下游落点整体下移。本批的做法与 `RuleCorePartial_B88E1.cs` /
//   `RuleCorePartial_B94B8.cs` 同形：**源文件只做「行数不变」的就地改写**，新成员一律落本文件。
//
// 本批（B97_W34）落在本文件的三件（逐条对应 `账/战斗逻辑驱动.tsv` 的行）：
//   · `AttackActionPowerGate`   ← 账 `:325` `BattleManager.GetAvailableAttackActions` 的两条闸之一
//   · `StartWithNamesLogged`    ← 账 `:607` `BattleManager.StartBattlePhase` 的「开局上手」一跳
//   · `SetNextCardAsDeckDrawing` / `IsDeckDrawing` / `ClearDeckDrawingMark`
//                               ← 账 `:573` `BattleManager.SetNextCardAsDeckDrawing`
//
// ⚠️ 判据出处逐条写在各自方法头上（铁律 2/3：没有出处就写「查不到」）。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  账 325 —— `GetAvailableAttackActions` 的「攻击力 / 压制」两道闸
        // ==================================================================
        /// <summary>
        /// 🔴 **2026-10-29（`B97_W34` · 账 `:325`）**：「这一种打法**排得出攻击动作**吗」的**唯一判据**。
        ///
        /// <para>**判据（第一权威，`decomp_full/BattleManager__GetAvailableAttackActions.c` 现读）**：
        /// 近战那一支（`:128-140`）的合取 = `!EnoughPendingDamageToDie(目标)` ∧
        /// **`0 &lt; get_CurrentMeleeAttack(攻方)`** ∧ **`!HasCurrentTrait(攻方, 0x3ca)`**；
        /// 远程那一支（`:154-166`）的合取 = `!EnoughPendingDamageToDie(目标)` ∧ **`0 &lt; get_CurrentRangeAttack(攻方)`**
        /// —— **远程没有 `0x3ca` 那一条**（与 `CardScript__CanAttackNow.c` 的「压制只禁近战」自洽）。
        /// `0x3ca = 970 = DefinedTrait.pindown`（本仓现用 `KeywordTable` 里的 `"pindown"` 字面量，
        /// 与 `CanAttackNow` 那一句同源）。</para>
        ///
        /// <para>🔴 **为什么要有这个函数（而不是在调用点各写一遍）**：
        /// `RuleCore.CanAttackNow` 里本来就有这两条（改前是两处散写），
        /// 而 `RuleCore.IsValidTarget`（= AI 动作表逐目标问的那个口）**一条都没有** ⇒
        /// 动作表会多出「0 攻近战 / 0 攻远程 / 被压制却排近战」的条目。
        /// 原版那两条闸**同时服务**「这一格能不能打」与「动作表排不排这一条」两件事 ——
        /// 我们这一层没有 `AvailableAction` 中间层 ⇒ 两个调用点**共用这一份**（铁律：两处写同一条规则 = 迟早不一致）。
        /// 这是**实现结构不同、语义等价**（与账 `:193` ③ 同一裁定口径），§18 不要求打 `🔶`。</para>
        ///
        /// <para>⚠️ **顺序照原版**：先 `pindown`（近战专属）再攻击力 —— 与 `CanAttackNow` 改前的次序逐字一致
        /// （那两句的顺序被 `RuleEngineTest` 的一批断言盯着，⛔ 别调换）。</para>
        /// </summary>
        /// <returns>`RuleCodes.OK` = 排得出；否则 = 原版那一支会拒绝的返回码（与 `CanAttackNow` 同码）。</returns>
        internal static int AttackActionPowerGate(BattleContext ctx, int p, UnitState attacker, bool ranged)
        {
            if (attacker == null) return RuleCodes.ErrNotUnit;
            // 压制（pindown）：**只禁近战**，远程照常（判据 `.c:154-166` 远程那一支没有这个 trait）
            if (!ranged && attacker.Has("pindown")) return RuleCodes.ErrPindown;
            // 攻击力 ≤ 0 ⇒ 这一档排不出动作（近战/远程各有各的码，判据 `.c:128-140 / :154-166`）
            if (FieldAttack(ctx, p, attacker, ranged) <= 0)
                return ranged ? RuleCodes.ErrNoRangedAttack : RuleCodes.ErrNoMeleeAttack;
            return RuleCodes.OK;
        }

        // ==================================================================
        //  账 607 —— 「开局上手」的取值口（`startingCardInHand` 真值优先）
        // ==================================================================
        /// <summary>
        /// 🔴 **2026-10-29（`B97_W34` · 账 `:607`）**：`RuleCore.SetupStartWith` 要发的那几张卡，
        /// **并把手牌侧与 SO 真值不一致的每一条如实出声**。
        ///
        /// <para>判据 = `decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c:213-247`：
        /// 开局对双方督军各读一次 `hero.rawCard(+0x28).startingCardInHand(+0x148)`，非空 ⇒ 直接进手牌。
        /// ⇒ **原版只看 SO 那一格**，不看卡面文字。我们这一层的真值口 =
        /// `CardDef.StartWithNames(notes)`（`CardDefPartial_B97W5.cs:91`），
        /// 它在「整列未知」（旧表 / 夹具）时退回卡面文本抽出来的那一份（= 改前行为，零变化），
        /// 已知时**以真值为准**、把「卡面写了但 SO 没有」的每一条写进 `notes`。</para>
        ///
        /// <para>⚠️ 这里只做「取值 + 出声」，发牌仍是 `SetupStartWith` 那一段（判据只一处）。</para>
        /// </summary>
        public static List<string> StartWithNamesLogged(BattleContext ctx, PlayerState ps)
        {
            if (ps == null || ps.Warlord == null || ps.Warlord.Card == null) return new List<string>();
            var notes = new List<string>();
            var names = ps.Warlord.Card.StartWithNames(notes);
            for (int i = 0; i < notes.Count; i++)
                ctx?.Log($"{ps.Name} 的督军「{ps.Warlord.Name}」：{notes[i]}");
            return names;
        }

        // ==================================================================
        //  账 573 —— `BattleManager.SetNextCardAsDeckDrawing(bool isPlayer)`
        // ==================================================================
        //
        // **判据（🥇 本轮现读，两个源头都看了）**：
        //   · `decomp_full/BattleManager__SetNextCardAsDeckDrawing.c` 全文 42 行：按 `isPlayer` 取那一侧的牌库列表，
        //     从 **下标 0** 起找**第一张** `cardState(+0x228) == 0 (inDeck)` 的卡，把它置成
        //     `0x10 (16 inDeckDrawing)`；找不到就什么都不做。
        //   · `CardScript__SetAsDeckDrawing.c`（全文两句）= `*(undefined4 *)(this + 0x228) = 0x10;`
        //     ⇒ **没有别的副作用**（不动父节点、不动 collider、不发事件）。
        //
        // 🔴 **本轮新查明（推翻账上「视觉消费方不存在 ⇒ 只落状态位」之外的第三种情况）**：
        //   这个方法名在 `decomp_full` / `out_cpp2il_dev_ilspy` **都是 0 调用点** —— 但
        //   **它的行为是活的**：ILSpy `BattleManager.cs:40419-40460` 的
        //   **`AddDrawCard(bool isPlayer, …)`** 里**内联着同一段循环**（找第一张 `inDeck` 的、置
        //   `SetAsDeckDrawing()`），`:40529` 的 `AddDrawSpecificCard` 也直接对目标卡调 `SetAsDeckDrawing()`。
        //   ⇒ 这是「私有小方法被编译器内联 ⇒ 符号 0 调用点 ≠ 死代码」的又一例（同 CLAUDE.md 已记的那族）。
        //   **本仓的等价活点是 `RuleCore.Draw`（`:2952`）那一条，不是本方法名。**
        //
        // ⚠️ **为什么本笔只落「状态口」、没有接进 `Draw`**（如实标，⛔ 不假装等价）：
        //   · 原版那一位是**纯呈现态**：`card2DController` 按它把牌库顶那张画成「正在抽」的动效；
        //     我们的牌库没有 `card2DController`（表现层不存在）⇒ 落上了也**零可观测**。
        //   · 原版**从不显式清 16** —— 牌离库进手牌时卡状态被另一句覆盖。我们这一层「在不在牌库」
        //     **是派生的**（`PlayerState.Deck` 这个列表本身），而抽牌是**同步**的
        //     （`Draw` 里 `MoveDeckTopToHand` 立刻搬走）⇒ 若照原版「只置不清」，那张牌会带着 16
        //     走进手牌、而我们的 `IsInDeckStateOf(16)` 会把它当成「还在牌库」= **假话**。
        //     ⇒ 接进 `Draw` 必须先有一个「离库即清」的点，而原版**没有**那个点可抄（它是被别的赋值顺手覆盖的）
        //     ⇒ 那就是我们自己在发明口径。⛔ 不发明。
        //   ⇒ **状态口已按要求落好**（含 `ClearDeckDrawingMark`，将来表现层接上时用），
        //     **接点留给表现层那一批**（账 `:573` 维持 C，理由写进补丁）。
        //   时间限定 = 本 build；一旦有了牌库顶的「正在抽」动效，接点 = `RuleCore.Draw` 的
        //   `MoveDeckTopToHand` 前后各一句。

        /// <summary>
        /// 原版 `BattleManager.SetNextCardAsDeckDrawing(isPlayer)`：把该侧牌库**第一张还在牌库里**的卡
        /// 置成 `cardState 16 (inDeckDrawing)`。判据与「为什么不接进 `Draw`」见上面那一段。
        /// </summary>
        /// <param name="seat">`0` / `1`（= 原版那个 `isPlayer` 布尔换成座位号，本仓一律用座位）。</param>
        /// <returns>被置位的那一张（牌库空 / 已有标记 ⇒ `null`）。</returns>
        public static CardInstance SetNextCardAsDeckDrawing(BattleContext ctx, int seat)
        {
            if (ctx == null || seat < 0 || seat > 1) return null;
            var ps = ctx.Players[seat];
            if (ps == null || ps.Deck == null) return null;
            var marks = ctx.DeckDrawingInstanceIds;
            // 原版从下标 0 起扫，取**第一张** `cardState == inDeck(0)` —— 我们已经标记过的那几张
            // 等价于原版「cardState 已经是 16 ⇒ 条件 `== 0` 为假 ⇒ 跳过」。
            for (int i = 0; i < ps.Deck.Count; i++)
            {
                var inst = ps.Deck[i];
                if (inst == null) continue;
                if (marks.Contains(inst.Id)) continue;
                marks.Add(inst.Id);
                ctx.Log($"{ps.Name} 的牌库顶「{inst.Card?.Name}」置成 `cardState 16 (inDeckDrawing)`"
                      + "（原版 `BattleManager__SetNextCardAsDeckDrawing.c:19-40` → `CardScript__SetAsDeckDrawing.c`）");
                return inst;
            }
            return null;
        }

        /// <summary>这一份此刻是不是「正在从牌库抽」那一档（原版 `cardState == 16`）。见 <see cref="SetNextCardAsDeckDrawing"/>。</summary>
        public static bool IsDeckDrawing(BattleContext ctx, CardInstance inst)
        {
            if (ctx == null || inst == null) return false;
            return ctx.DeckDrawingInstanceIds.Contains(inst.Id);
        }

        /// <summary>把这一份的「正在抽」标记撤掉（原版没有显式清零点 —— 它是被「进手牌 / 进坟场」的赋值顺手覆盖的；
        /// 我们这一层的等价时机 = 这张牌**真的离开牌库列表**的那一刻，由将来的接点显式调）。</summary>
        public static bool ClearDeckDrawingMark(BattleContext ctx, CardInstance inst)
        {
            if (ctx == null || inst == null) return false;
            return ctx.DeckDrawingInstanceIds.Remove(inst.Id);
        }
    }

    // ==================================================================
    //  账 573 的状态载体
    // ==================================================================
    //  🔴 **为什么放在 `BattleContext` 的分部里、而不是 `CardInstance` 上加字段**：
    //    `CardInstance.cs` 不在本批白名单（那一份被 68 处账落点按行号引用），而
    //    `BattleContext` 本来就是 `partial`（`UnitStateCardState.cs:220` 已经用同一个办法加过
    //    `CardStateProbe`）⇒ 在这里开一份分部是**既有先例**，零越界。
    //  ⚠️ 它**不进 `Fingerprint`**（那一份只读棋盘 / 手牌 / 牌库的**内容**，不读这个旁表）⇒
    //    本地录像与 P2P 两端的指纹**逐字节不变**。
    //  ⚠️ 键用 `CardInstance.Id`（一局唯一、由 `BattleContext.NextInstanceId` 发 ⇒ 可复现，不用 Guid）。
    public partial class BattleContext
    {
        /// <summary>原版 `cardState == 16 (inDeckDrawing)` 的那几张（见 `RuleCore.SetNextCardAsDeckDrawing`）。</summary>
        public readonly HashSet<int> DeckDrawingInstanceIds = new HashSet<int>();
    }
}
