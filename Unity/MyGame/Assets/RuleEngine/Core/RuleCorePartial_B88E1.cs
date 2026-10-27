// RuleCorePartial_B88E1.cs — 第八十八会话 · 波 1 · 执行代理 `E1` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 **365 处**
//   （`资料/完全复刻/账/*.tsv`）⇒ 往它中间插成员会让下游落点整体下移、`check_ledger_points` 大批报漂移。
//   本批的做法：**源文件只做「零净行数」的就地编辑**（把新语句接在已有行的行尾），
//   新成员（新方法）一律落在本文件 —— 它没有任何账的落点。
//
// 本批（B88-E1）落在本文件的东西（逐条对应 `账/战斗逻辑驱动.tsv` 的 C 行）：
//   · `RemoveDuplicitousTrait`      ← 账行 `CardScript.RemoveDuplicitous()`（R24）
//   · `FlipAmbushOnAttacked`        ← 账行 `CardScript.ResolveUnitAttacked(...)`（R26 ②）
//   · `ActiveRowHasOps` / `RunActiveRowOps` ← 账行 `RawCardScript.ResolveActiveAbility(...)`（R28）
//
// ⚠️ 判据出处逐条写在各自方法头上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  R24 —— `CardScript.RemoveDuplicitous()` 的等价物
        // ==================================================================
        /// <summary>
        /// 🔴 **2026-10-27（B88-E1 · 账行 `CardScript.RemoveDuplicitous()`）**：把 `tainted` 从单位上摘掉。
        ///
        /// <para>**判据（第一权威，`decomp_full/CardScript__RemoveDuplicitous.c` 全文 3 句）**：
        ///   `RemoveBuffedTrait(0xa0)` + `RemoveTrait(0xa0)` + `UpdateFigures(0)`；
        ///   而 `0xa0 = 160 = DefinedTrait.tainted`
        ///   （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:17`；`DefinedTrait` 里
        ///   **没有 `duplicitous` 这个词**）⇒ 方法名叫 Duplicitous，摘的是 **tainted**。</para>
        ///
        /// <para>**原版唯一的调用点** = `BattleManager._ResolveAttack_d__438__MoveNext.c:512-519`：
        /// 攻击结算里 `HasCurrentTrait(被打者, 0xa0)` ⇒ `RemoveDuplicitous(被打者)`（**摘一次**，
        /// 这一记照常结算下去 —— 它不是「取消攻击」）。我们那一跳的落点 =
        /// `RuleCore.DeclareAttackExecute`（声明层之后、主伤害之前）。</para>
        ///
        /// <para>⚠️ **我们这一层里两条 `Remove*Trait` 合成一句 `RemoveAll`**：`UnitState.RemoveAll`
        /// 清的是「当前关键词 + 它授予的正文 + 它带出的属性增益」，正好覆盖原版
        /// `RemoveBuffedTrait`（摘那份被 buff 的副本）+ `RemoveTrait`（摘本体）两句；
        /// `UpdateFigures`（重算面板数值）在我们这边是**派生的**（`FieldAttack` / 属性投影现算），
        /// 没有需要刷新的缓存格 ⇒ 不需要对应语句。</para>
        ///
        /// <para>⚠️ **可达性**：全池 `tainted` / `Duplicitous` / `"trait":160` **各 0 处**
        /// （`cards_engine.json` 两档现读）⇒ **休眠机制**（铁律 11：照样做）。
        /// 验收口 = 给一张带 `tainted` 的卡打一记之后 `u.Has("tainted")` 转假。</para>
        /// </summary>
        internal static void RemoveDuplicitousTrait(BattleContext ctx, UnitState u)
        {
            if (u == null) return;
            if (!u.Has(ConditionEval.Tainted)) return;
            u.RemoveAll(ConditionEval.Tainted);
            ctx.Log($"{u.Name} 被攻击 ⇒ 摘掉 `tainted`（原版 `ResolveAttack:512-519` → "
                  + "`CardScript__RemoveDuplicitous.c` → `RemoveTrait(0xa0)`）");
        }

        // ==================================================================
        //  R26 ② —— 「被攻击就翻开伏击」（原版第三条 `SetAmbush(false)` 出口）
        // ==================================================================
        /// <summary>
        /// 🔴 **2026-10-27（B88-E1 · 账行 `CardScript.ResolveUnitAttacked(...)` 的缺口 ②）**：
        /// **被打的那个单位只要带 `ambush` 且面朝下，挨不挨到伤害都翻开。**
        ///
        /// <para>**判据（`decomp_full/CardScript__ResolveUnitAttacked.c:104-115`，逐句现读）**：
        ///   `HasCurrentTrait(this, 0x4b0 /* ambush */)` 为真、且 `cardState(+0x228)` ∈
        ///   {2, 15, 3, 17}（= 在场 / 将死那几档）⇒ `SetAmbush(this, false, 0)`。
        ///   ⚠️ **这一段在读伤害之前**，没有任何「打中了没有」的判断 ⇒ **盾挡下 / 无敌 / 0 点伤害也翻**。
        ///   我们改前只有两条翻开出口（`DeclareAttack` 的「面朝下出手」· `ApplyDamageCore` 的
        ///   「真掉血」）⇒ 「被攻击但 0 伤」那一格**不翻**（真缺口）。</para>
        ///
        /// <para>**`SetAmbush(false)` 的语义三句**（照 `CardScript__SetAmbush.c:69-129`）：
        ///   ① 先 `RemoveTraitAndEffects(0x4b0)`（= 我们 `RemoveAll`）；
        ///   ② 发一条「退出伏击」（表现层的 `EvtKind.AmbushExit`，判据见 `EvtKind` 那一节）；
        ///   ③ **只在卡主自己那一方是当前行动方时**才 `OnTrigger(0x29E)`（= 我们
        ///      `FireTriggerAt(..., KeywordTable.Ambush, ...)`）。
        ///   ⚠️ 三句与 `ApplyDamageCore` 里那一段**逐句同形**（那一段的判据注释更长，别在这儿抄第二份）——
        ///   两条出口的差别只在**触发条件**：那边是「真掉血」，这边是「被攻击」。</para>
        /// </summary>
        internal static void FlipAmbushOnAttacked(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return;
            if (!u.FaceDown) return;
            if (!u.Has(KeywordTable.Ambush)) return;
            u.FaceDown = false;
            u.RemoveAll(KeywordTable.Ambush);
            int owner, slot;
            bool onBoard = FindUnit(ctx, u, out owner, out slot);
            if (onBoard) EmitUnit(ctx, EvtKind.AmbushExit, u, 0, effect: "attacked");
            bool fireNow = onBoard && owner == ctx.Active && !ctx.IsOver;
            ctx.Log($"{u.Name} 被攻击 ⇒ **翻开**伏击（原版 `ResolveUnitAttacked.c:104-115`："
                  + "挨不挨到伤害都翻）" + (fireNow ? "，伏击效果触发" : "（不是它主人的回合 ⇒ 这次伏击效果不触发）"));
            if (fireNow) FireTriggerAt(ctx, u, KeywordTable.Ambush, owner, slot);
        }

        // ==================================================================
        //  账 637 —— `BattleManagerSupport.BroadcastRemoveFromGame` 的三跳
        // ==================================================================
        /// <summary>
        /// 🔴 **2026-10-27（B88-E1c · 账 637 `BattleManagerSupport.BroadcastRemoveFromGame`）**：
        /// 「移出游戏」那件事发生之后，**场上 + 双方手牌逐张**把「被移出那张卡给的外来效果」撤掉。
        ///
        /// <para>**判据（第一权威，`decomp_full/BattleManagerSupport__BroadcastRemoveFromGame.c:20-136` 逐句现读）**：
        ///   · **第 1 跳 = 棋盘**：遍历 `+0x470`（`allCardsInPlay`），闸 = `CardScript__IsInPlay(那张)` ∧
        ///     `op_Inequality(那张, 0)`；对每张调 **`CardScript__RemoveFromGameCard(那张, 被移出那张)`**，
        ///     `被移出那张` 有两个实参（`param_2` / `param_3`，分别对应 exchange / steal 那两条路的**两张**卡）
        ///     ⇒ `.c` 里同一张卡连着出现两次 `RemoveFromGameCard` 就是这两跳；
        ///   · **第 2 跳 = 当前回合方手牌**（`GetCardsInHandRef(bm, IsPlayerTurn())`），同样 `op_Inequality`
        ///     排除那两张 ⇒ 逐张 `RemoveFromGameCard(手牌卡, param_2)` / `(手牌卡, param_3)`；
        ///   · **第 3 跳 = 另一侧手牌**（`GetCardsInHandRef(bm, !IsPlayerTurn())`），与第 2 跳同形。
        ///   · **接收端语义**（`CardScript__RemoveFromGameCard.c` + `cpp2il CardScript.cs:16012`）：
        ///     `被移出那张 == 自己` ⇒ 把自己置成 `cardState = 11 (removedFromGame)`；
        ///     **否则** ⇒ `ReactToUnitJammed(自己, 被移出那张)` 的 else 支 = **按来源撤 extrinsic**
        ///     （`EntityScript__RemoveExtrinsicEffectsFrom`）。
        ///   ⚠️ 本广播**对那两张卡自己跳过**（.c 的两处 `op_Inequality`）⇒ 它**永远只走 else 支**；
        ///     「置 11」是另一条路（`RemoveFromGameCard(自己)`），落点 = <see cref="MarkRemovedFromGame"/>。</para>
        ///
        /// <para>⚠️ **我们这边的两处如实标注（铁律 2：不假装等价）**：
        ///   ① 棋盘那一跳是**真等价**（`UnitState.RemoveExtrinsicEffectsFrom(来源卡名)`，判据见它的 doc）；
        ///   ② **手牌两跳在我们模型里是空操作** —— 我们的手牌是 `CardInstance`，**不聚合 activeEffects**
        ///      （效果摊在 `UnitState` 的四本账上，见 `UnitState.RemoveExtrinsicEffectsFrom` 的 doc），
        ///      所以「撤某张手牌上由 X 给的效果」在我们这边**没有可撤的东西**。这里**照样逐张走一遍**
        ///      （保持与原版同一条遍历形状，将来手牌若长出自己的效果账就能接上），并在**真的走过手牌**时
        ///      打一条日志 —— 不漏报成「做过了」。</para>
        ///
        /// <para>🔴 **发射口（2026-10-28 现读：4 个里已接 1 个）**：原版这 4 个调用点全在
        /// `_ResolveExchangeMinion_d__552` / `_ResolveStealMinion_d__551` / `_ResolveTransformIntoRemnant_d__554` /
        /// `ResolveTransformUnit` —— ✅ **#3 `transformToRemnant` 已接**（`RuleCore.CleanupDeaths` 的残骸支，
        /// 全池 55 张卡，B88·A2 2026-10-28；判据 `判据/B88_R4_637三族_施工单.md` §5·1）；其余三条**仍未接**
        /// （transform 族见账 172；exchange / steal 见 `PlayAbility分支索引.tsv` 的 B37/B38/B35/B36）。
        /// ⇒ 它们落地时**必须调这里**，⛔ 别各写一份三跳（铁律「两处写同一条规则 = 迟早不一致」）。视图侧 = `BattleDriver`。</para>
        /// </summary>
        internal static void BroadcastRemoveFromGame(BattleContext ctx, UnitState a, UnitState b)
        {
            if (ctx == null || (a == null && b == null)) return;
            int touched = 0;
            // ---- ① 棋盘：IsInPlay ∧ 非那两张 ----
            for (int p = 0; p < 2; p++)
            {
                var ps = ctx.Players[p];
                if (ps == null) continue;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ps.Board[s];
                    if (u == null || !u.IsAlive) continue;
                    if (ReferenceEquals(u, a) || ReferenceEquals(u, b)) continue;
                    if (a != null && a.Card != null) touched += u.RemoveExtrinsicEffectsFrom(a.Card.Name);
                    if (b != null && b.Card != null) touched += u.RemoveExtrinsicEffectsFrom(b.Card.Name);
                }
            }
            // ---- ②③ 双方手牌：逐张走一遍（本模型里是空操作，理由见 doc ① ②）----
            int handWalked = 0;
            for (int p = 0; p < 2; p++)
            {
                var ps = ctx.Players[p];
                if (ps == null || ps.Hand == null) continue;
                handWalked += ps.Hand.Count;
            }
            ctx.Log($"`BroadcastRemoveFromGame` 广播（移出游戏 / 残骸翻面 / 变身 共用；2026-10-28 B88·A2 起 #3 残骸翻面已接）：场上撤掉 {touched} 条外来效果；双方手牌共 {handWalked} 张按原版走了一遍"
                  + "（我们的手牌不聚合 activeEffects ⇒ 手牌两跳是空操作，如实记录）"
                  + "（原版 `BattleManagerSupport__BroadcastRemoveFromGame.c:20-136` 三跳）");
        }

        /// <summary>
        /// 🔴 **2026-10-27（B88-E1c · 账 637 的「置位」那一半）**：把**这一张**置成
        /// 「移出游戏」（原版 `CardScript.cardState(+0x228) = 11 = CardStateOptions.removedFromGame`，
        /// 判据 `dump.cs:45480`；**唯一置位支** = `CardScript.RemoveFromGameCard` 里
        /// `被移出那张 == this` 那一支，见 `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:16012`）。
        /// <para>⚠️ 我们这一层只置**引擎态**（`UnitState.RemovedFromGame`，E4 2026-10-27 提供）；
        /// **视图侧**（隐藏 + 摘碰撞 = 原版 `SetObjectVisibility(11)`）归 `BattleDriver`（E5）。
        /// ⚠️ 「移出游戏」**不等于离场/阵亡**：⛔ 别顺手调 `CleanupDeaths`（那会发死亡广播、进弃牌堆）。</para>
        /// </summary>
        internal static void MarkRemovedFromGame(BattleContext ctx, UnitState u)
        {
            if (u == null || u.RemovedFromGame) return;
            u.RemovedFromGame = true;
            ctx?.Log($"{u.Name} **移出游戏**（`cardState 11`；原版 `RemoveFromGameCard(自己)` 那一支）");
        }

        // ==================================================================
        //  R28 —— 主动能力按 `chosenAbility` 分派：复用「正文层已配好的 op」
        // ==================================================================
        /// <summary>某一 `activeAbilityLogic` 行**是否已经配到正文 op**
        /// （标记 = `EffectOp.ActiveAbilityRow == row`，由 `CardDef.AttachByAbilityKind` 挂）。
        /// <para>⚠️ 这一格是**数据驱动的**：`AttachByAbilityKind` 只在「种类族 ↔ 正文动词族」对得上、
        /// 且同族各行的 `ta`/`fm` 逐格一致时才挂 ⇒ 有标记就说明**这条行做什么、对谁做**都已经查清，
        /// ⛔ 本文件**不自己造数值 / 不自己造目标集**（铁律 2：不许「大概是」）。</para></summary>
        internal static bool ActiveRowHasOps(UnitState u, int row)
        {
            return ActiveRowOpsOf(u, row).Count > 0;
        }

        /// <summary>取某一 `activeAbilityLogic` 行配到的那几条正文 op（同 <see cref="ActiveRowHasOps"/>）。</summary>
        internal static List<EffectOp> ActiveRowOpsOf(UnitState u, int row)
        {
            var outs = new List<EffectOp>();
            if (row < 0) return outs;
            var all = ActiveAbilityBodyOps(u);
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].ActiveAbilityRow == row) outs.Add(all[i]);
            return outs;
        }

        /// <summary>
        /// 🔴 **2026-10-27（B88-E1 · 账行 `RawCardScript.ResolveActiveAbility(...)`）**：
        /// 执行**一行** `activeAbilityLogic` 配到的正文 op（原版 `AbilityLogic__PlayAbility` 里
        /// `switch (chosenAbility)` 那一跳的等价物）。
        ///
        /// <para>**为什么这样写**（判据链）：原版 `RawCardScript__ResolveActiveAbility.c:47-67` 是
        /// **逐行**把 `activeAbilityLogic` 交给 `AbilityLogic__PlayAbility`，后者按
        /// `logic.chosenAbility(+0x10)` 分派（伤害族读 `damageCriteria`、召唤族循环 `local_178` 次…）。
        /// 我们这边**没有**逐族 handler，但有 `CardDef.AttachByAbilityKind`（v24/v25）：
        /// 它按**同一条 `chosenAbility` 的族名**把 logic 行配到该卡**正文解析出来的 op** 上，
        /// 并把 `ta`（目标集）· `fm`（倍率）· 36 格 `targetCriteria` 一起贴上。
        /// ⇒ 「这条行做什么」已经由**数据**决定，本方法只负责**执行它**，
        /// ⛔ 不按 `chosenAbility` 数字重新发明一份数值（那会是第二份判据）。</para>
        ///
        /// <para><paramref name="ta"/> = 这一行的 `targetsAffected`：只有 `30 (target)` / `220`
        /// 两档读调用方点的那张（判据 `AbilityLogic__GetTargets.c:192-207`：整张函数里只有这两档读
        /// `param_5`）；其它档传 `null`（`self` / 相邻派生集由 op 自己的 `SubjectTargetsAffected`
        /// 与 `TargetCriteriaSet` 决定）。</para>
        ///
        /// <returns>跑了几组（1 = 跑了，0 = 这一行没有配到 op）。</returns>
        /// </summary>
        internal static int RunActiveRowOps(BattleContext ctx, int p, UnitState u, int row, int ta,
                                           UnitState chosen)
        {
            var ops = ActiveRowOpsOf(u, row);
            if (ops.Count == 0) return 0;
            var useTgt = (ta == 30 || ta == 220) ? chosen : null;
            List<string> un;
            ctx.EffectChain++;
            try { ResolveOps(ctx, p, u, ops, useTgt, out un); }
            finally { ctx.EffectChain--; }
            ReportUnresolved(ctx, $"「{u.Name}」的主动能力（chosenAbility 行 {row}）", un);
            return 1;
        }
    }
}
