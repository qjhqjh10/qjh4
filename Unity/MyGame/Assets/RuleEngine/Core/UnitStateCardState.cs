// UnitStateCardState.cs — 原版 `CardScript.cardState(+0x228)`（枚举 `CardStateOptions`）在我们引擎里的等价物
//
// 🔴 **为什么单独开一个文件**（同 `CardDefExtraTraits.cs` / `CardDefActiveAbility.cs` 的纪律）：
//   `UnitState.cs` 里有一批账行按【行号】钉着（现读最大 = `:3323`）⇒ 在类中间插成员会让它们整体漂移。
//   `Core/*.cs` 是**通配**编进 `RuleCore` / `ruleprobe` 的 ⇒ 新文件自动被带上。
//
// ==================================================================================================
//  账 `战斗逻辑驱动` · `CardScript.cardState`（B1 反复撞到的那堵墙）
// ==================================================================================================
//  【判据一：字段是谁】`d:/2/tools/il2cpp_out/dump.cs:35454`
//      `private CardStateOptions <cardState>k__BackingField; // 0x228`，类 = `CardScript : EntityScript`。
//      同族偏移互相反证（六个逐一吻合）：`:35370` 起 —— `0x224 <activeAbilityLoaded>` · **`0x228 <cardState>`** ·
//      `0x22C uniqueInGameID` · `0x230 <canAct>` · `0x290 <CardUI>`；`:21963` 起 EntityScript —— `0x28 <rawCardData>` ·
//      `0x40 <isPlayer>` · `0x55 <stunnedAtStartOfTurn>` · `0x108 <activeEffects>`。
//      第二来源（签名桩）：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardScript.cs:1490`。
//
//  【判据二：18 档全表】`dump.cs:45465-45487`（本文件 `CardStateOptions` 逐值照抄，⛔ 别改名 / 改数）
//  【判据三：四个「在场上」谓词的**逐句原文**】`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs`
//      · `IsInPlay()`          `:20755-20770`  ⇒ `cardState ∈ {2 inPlay, 15 inPlayTransforming, 3 inPlayAttacking, 17 inPlayAminingAttack}`
//      · `IsInPlayOrDying()`   `:20772-20795`  ⇒ 上面 4 档 **＋ `5 waitingToDie`**
//        （ILSpy 里那串 `object obj2 = cardState - 5; return obj2 == null;` 就是 `== waitingToDie`；
//         同一条在 `.c` 里是 `CardScript__IsInPlayOrDying.c:5-11`，两份一致）
//      · `IsInHand()`          `:20806-20833`  ⇒ `{12 inHandJustCreated, 10 inHandPlaying, 9 inHandMoving, 8 inHandShowing, 4 waitingToBePlayed, 1 inHand}`
//      · `IsInDeck()`          `:20835-20845`  ⇒ `{0 inDeck, 16 inDeckDrawing}`
//      · `IsInCemetery()`      `:20797-20804`  ⇒ `== 6`
//
//  【判据四：写入点（谁置这 18 档）】`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs` 全扫（40 处赋值），
//      逐档对应到**原版方法名**（下面 `CardStateOptions` 每个成员的注释里都标了）。
//      ⚠️ 还有两条**通用写口**：`SetCardState(CardStateOptions newState)`（`:9529`，直写）与属性 setter（`:5937`）。
//
//  【判据五：读点（谁按它判）】全扫 52 处，全部落在 `BattleManager` / `BattleCardUI` / `PlayerHand` /
//      `AbilityLogic` / `CardScript` 自己。**对引擎有意义的就那 5 个谓词**（本文件把它们逐句复刻）。
//
// ==================================================================================================
//  🔴 **我们这边的落地口径（如实标：这是「派生」不是「新状态机」）**
// ==================================================================================================
//  原版那 18 档里，**只有 5 档对引擎行为有影响**（其余 12 档全是手牌 / 牌库 / 动画中间态，
//  由 `CardPresentation` 与 `PlayerHand` 那一侧表达，⛔ 不在这里造）：
//      2 inPlay · 3 inPlayAttacking · 5 waitingToDie · 15 inPlayTransforming · 17 inPlayAminingAttack
//  我们**能 100% 拼出的两档**（不需要任何新写点）：
//      · `2 inPlay` —— `UnitState` 存在于 `PlayerState.Board[slot]` 就是「在场上」，默认值；
//      · `5 waitingToDie` —— `ctx.IsPendingDeath(u)`（`BattleContext.cs:1741`，`DeferDeaths` 窗口内）
//                             ∨ `ctx.DyingThisAction` 含它（`ActionPump.cs:1336 IsDyingThisAction`）
//  我们**暂时拼不出的三档**（3 / 15 / 17 —— 它们是**结算过程中的瞬态**，原版在
//  `SetupAttack` / `SetupActiveAbility` / `ResolveWeaponKilled` 里置、在 `FinishAfterAttack` /
//  `CancelAttack` / `CancelActiveAbility` / `UsedActiveAbility` 里复位）：
//      ⇒ 本文件给两个标记口 `MarkCardState` / `ClearCardStateMark`，
//        **写点由 `RuleCore` / `EffectResolver` 的对应方法接**（⛔ 不在本文件假装已接）。
//        ⚠️ **未标时按 `inPlay(2)` 处理** —— 这与「没标 = 不在攻击/瞄准/变身中」一致，
//        但**不等于**原版那三档已经复刻：见报告 §5「还差什么」。
//
//  🔴 **与现有那两个 `RuleCore` 谓词的关系（⛔ 别当成「同一件事实写两遍」）**：
//    · `RuleCore.IsUnitInPlay(ctx,u)`（`RuleCore.cs:10782`，doc 自称 `CardScript.IsInPlay(施放者)` 的等价物）
//      = **「在某一方棋盘上」** ⇒ 它**无法**排除 `waitingToDie`：`DeferDeaths` 窗口内死者**仍占着槽**
//      ⇒ 那一格它返回 **true**，而原版 `IsInPlay()` 对 `cardState == 5` 返回 **false**（5 ∉ {2,15,3,17}）。
//      ⇒ 两者**只在「待死窗口」这一格分叉**；`IsInPlayState(ctx)` 才是原版的直译（它先问 `IsPendingDeath`）。
//    · `RuleCore.IsUnitInPlayOrDying(ctx,u)`（`:10806`）是**加效果 / 指代目标**用的**宽口径**
//      （`IsAlive ∨ 在棋盘上 ∨ DyingThisAction`，**含「已回手 / 已进牌库但仍活着」那一档**）
//      —— 原版那个谓词只按 `cardState`，**不含「已回手」** ⇒ 两口径各自有出处，⛔ 别互相顶替。
//
//  ⚠️ **`inHand(1)` / `inDeck(0)` / `inCemetery(6)` / `removedFromGame(11)` 等档的等价物在
//  `CardInstance` 的所在容器上（`Deck` / `Hand` / `Cemetery` 列表），不在 `UnitState`**
//  ⇒ 本文件的 `IsInHandStateOf` / `IsInDeckStateOf` / `IsInCemeteryStateOf` 是**纯枚举判定**（给定值判真假），
//  给「持有 `CardStateOptions` 值的一方」用（例如将来的 `CardInstance.CardStateOf()`），⛔ 不宣称已在 `UnitState` 上可得。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 原版 `CardStateOptions`（`d:/2/tools/il2cpp_out/dump.cs:45465-45487`）—— **18 档，逐值照抄**。
    /// <para>⛔ **别改名、别改数值、别删档**：原版是按数值判的（`+0x228` 直接 `==`），改名会让
    /// 「逐档对拍」失去意义。每档后面的注释 = 原版**在哪个方法里置它**（`.c` / ILSpy 行号见文件头）。</para>
    /// </summary>
    public enum CardStateOptions
    {
        inDeck = 0,                 // `SetAsDeck` / `SetupInDeck` / `MoveToDeck`
        inHand = 1,                 // `OnTurnSetup` / `ActivateCard` / `SetStateInHand` / `SetAsUnitInHand`
        inPlay = 2,                 // `SetAsHero` / `SetAsUnitInPlay` / `CancelActiveAbility` / `UsedActiveAbility` / `FinishAfterAttack`
        inPlayAttacking = 3,        // `SetupActiveAbility`
        waitingToBePlayed = 4,      // `CardWaitingToBePlayed`
        waitingToDie = 5,           // `CheckIfDead` / `SetAsUnitDestroyed`
        inCemetery = 6,             // `NonTargetSpellPlayed` / `TargetedSpellPlayed` / `GoToCemetery` / `SetCardStateInCemetery`
        drawing = 7,                // ⚠️ **原版 40 个赋值点里没有置 7 的**（只读不写 ⇒ 休眠档，照抄保留）
        inHandShowing = 8,          // `ShowCardInHand`
        inHandMoving = 9,           // `ShowCardInCenter` / `ShowCardBeingDrawn` / `StartMovingFromHand` / `OutFromPlayingAreaAnim`
        inHandPlaying = 10,         // `MoveIntoFieldFromHand`
        removedFromGame = 11,       // `RemoveFromGameCard`
        inHandJustCreated = 12,     // （匿名/局部方法内，:809 一带）
        inMulliganSelected = 13,    // `SetStateAsMulligan(false)` / `ClickMulliganSelected`
        inMulliganDiscarded = 14,   // `SetStateAsMulligan(true)` / `ClickMulliganSelected`
        inPlayTransforming = 15,    // `ResolveWeaponKilled` / `SetAsUnitTransforming`
        inDeckDrawing = 16,         // `SetAsDeckDrawing`
        inPlayAminingAttack = 17,   // `TryDraggingFromBoard` / `SetupAttack`
    }

    public partial class UnitState
    {
        // ==============================================================================================
        //  🆕 2026-10-28（B92 · `B6c`）：原版 `CardScript.cardState(+0x228)` 的等价物
        // ==============================================================================================
        //  只装「结算过程中的瞬态」三档（3 / 15 / 17）；`2` 是默认、`5` 由 `BattleContext` 派生
        //  ⇒ **不存 2，也不存 5**（那两档已经有唯一来源，再存一份就是第二份状态，迟早不一致）。

        /// <summary>结算瞬态标记：只允许 <see cref="CardStateOptions.inPlay"/>（= 未标）/
        /// <see cref="CardStateOptions.inPlayAttacking"/> / <see cref="CardStateOptions.inPlayTransforming"/> /
        /// <see cref="CardStateOptions.inPlayAminingAttack"/> 这四个值。</summary>
        CardStateOptions _cardStateMark = CardStateOptions.inPlay;

        /// <summary>
        /// 这张单位**此刻**的原版 `cardState` —— **派生**，不存第二份。
        /// <para>① `ctx.IsPendingDeath(this)`（`DeferDeaths` 窗口内，`BattleContext.cs:1741`）
        ///     ∨ `ctx.DyingThisAction` 含它（`ActionPump.IsDyingThisAction`）⇒ **`waitingToDie(5)`**
        ///     —— 判据 `CardScript__CheckIfDead.c` 里 `cardState = waitingToDie`（ILSpy `CardScript.cs:15300`）
        ///     与 `SetAsUnitDestroyed`（`:15600`）两处置它，之后才进坟场。</para>
        /// <para>② 否则看瞬态标记（3 / 15 / 17，由 `MarkCardState` 置、`ClearCardStateMark` 复位）。</para>
        /// <para>③ 都没有 ⇒ **`inPlay(2)`**（`UnitState` 只存在于 `PlayerState.Board` 上）。</para>
        /// <para>⚠️ `ctx == null` ⇒ 跳过 ①（离线/单测夹具），只给 ② 或 ③。</para>
        /// </summary>
        public CardStateOptions CardStateOf(BattleContext ctx)
        {
            if (ctx != null)
            {
                if (ctx.IsPendingDeath(this)) return CardStateOptions.waitingToDie;
                var dying = ctx.DyingThisAction;                 // `HashSet<UnitState>`（`BattleContext.cs:1764`）
                if (dying != null && dying.Contains(this)) return CardStateOptions.waitingToDie;
            }
            return _cardStateMark;
        }

        /// <summary>把这张单位标成「结算瞬态」（原版 `SetupAttack`(`:12406` → 17) /
        /// `SetupActiveAbility`(`:12113` → 3) / `SetAsUnitTransforming`(`:15656` → 15) 置的那三档）。
        /// <para>⛔ **只收那三个值**（其它值传进来会被拒并出声）：`2` 用 <see cref="ClearCardStateMark"/> 表达、
        /// `5` 由 `BattleContext` 派生 —— 都在别处有唯一来源，⛔ 别用本方法伪造。</para>
        /// </summary>
        public bool MarkCardState(CardStateOptions state)
        {
            if (state == CardStateOptions.inPlayAttacking
                || state == CardStateOptions.inPlayTransforming
                || state == CardStateOptions.inPlayAminingAttack)
            {
                _cardStateMark = state;
                return true;
            }
            UnityEngine.Debug.LogWarning("[RuleEngine] `MarkCardState(" + state + ")` 被拒：只允许 "
                + "inPlayAttacking(3) / inPlayTransforming(15) / inPlayAminingAttack(17)"
                + "（inPlay(2) 用 ClearCardStateMark；waitingToDie(5) 由 BattleContext 派生）");
            return false;
        }

        /// <summary>复位瞬态标记 ⇒ `CardStateOf` 回到 `inPlay(2)`（原版 `FinishAfterAttack`(`:13033`) /
        /// `CancelAttack`(`:12500`) / `CancelActiveAbility`(`:12158`) / `UsedActiveAbility`(`:12180`) 四处置 2）。</summary>
        public void ClearCardStateMark() { _cardStateMark = CardStateOptions.inPlay; }

        // ---- 五个「逐句复刻」的谓词（原版 `CardScript.IsInPlay/IsInPlayOrDying/IsInHand/IsInDeck/IsInCemetery`）----

        /// <summary>原版 `CardScript.IsInPlay()`（ILSpy `CardScript.cs:20755-20770` / `.c:7-14`）：
        /// **`{2 inPlay, 15 inPlayTransforming, 3 inPlayAttacking, 17 inPlayAminingAttack}`** —— ⛔ **不含 `waitingToDie(5)`**。</summary>
        public static bool IsInPlayStateOf(CardStateOptions s)
        {
            return s == CardStateOptions.inPlay
                || s == CardStateOptions.inPlayTransforming
                || s == CardStateOptions.inPlayAttacking
                || s == CardStateOptions.inPlayAminingAttack;
        }

        /// <summary>原版 `CardScript.IsInPlayOrDying()`（ILSpy `CardScript.cs:20772-20795` / `.c:5-11`）：
        /// 上面 4 档 **＋ `5 waitingToDie`**。</summary>
        public static bool IsInPlayOrDyingStateOf(CardStateOptions s)
        {
            return IsInPlayStateOf(s) || s == CardStateOptions.waitingToDie;
        }

        /// <summary>原版 `CardScript.IsInHand()`（ILSpy `CardScript.cs:20806-20833`）：
        /// `{1 inHand, 4 waitingToBePlayed, 8 inHandShowing, 9 inHandMoving, 10 inHandPlaying, 12 inHandJustCreated}`。</summary>
        public static bool IsInHandStateOf(CardStateOptions s)
        {
            return s == CardStateOptions.inHand
                || s == CardStateOptions.waitingToBePlayed
                || s == CardStateOptions.inHandShowing
                || s == CardStateOptions.inHandMoving
                || s == CardStateOptions.inHandPlaying
                || s == CardStateOptions.inHandJustCreated;
        }

        /// <summary>原版 `CardScript.IsInDeck()`（ILSpy `CardScript.cs:20835-20845`）：`{0 inDeck, 16 inDeckDrawing}`。</summary>
        public static bool IsInDeckStateOf(CardStateOptions s)
        {
            return s == CardStateOptions.inDeck || s == CardStateOptions.inDeckDrawing;
        }

        /// <summary>原版 `CardScript.IsInCemetery()`（ILSpy `CardScript.cs:20797-20804`）：`== 6 inCemetery`。</summary>
        public static bool IsInCemeteryStateOf(CardStateOptions s) { return s == CardStateOptions.inCemetery; }

        // ---- 单位这一侧的两个便捷重载（= 原版那两个谓词在 `CardScript` 上的直译）----

        /// <summary>`this` 现在算不算「在场上」（= 原版 `IsInPlay()`）。</summary>
        public bool IsInPlayState(BattleContext ctx) { return IsInPlayStateOf(CardStateOf(ctx)); }

        /// <summary>`this` 算不算「在场上或待死」（= 原版 `IsInPlayOrDying()`）。</summary>
        public bool IsInPlayOrDyingState(BattleContext ctx) { return IsInPlayOrDyingStateOf(CardStateOf(ctx)); }
    }
}
