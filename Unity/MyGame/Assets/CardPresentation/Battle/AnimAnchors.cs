// AnimAnchors.cs — 原版 `AbilityLogic.GetPos`（`PositionOption` 36 档 → 锚点）的复刻
//
// ─────────────────────────────────────────────────────────────────────────────
// 【原版是谁 · 第一权威】
//   `d:/2/tools/decomp_full/AbilityLogic__GetPos.c`（**全篇 331 行，逐句亲读**）
//   签名：`GetPos(BattleManager mgr, AbilityData abilityData, CardScript abilityTarget, PositionOption option)`
//   它是【卡上技能（`AbilityLogic.PlayAnim`）】把 `AnimInfo` 那三个字段
//   （`startPosOption 0x18` / `endPosOption 0x1C` / `pointTowardsOption 0x20`）
//   在**动画播放那一刻**解析成「一个锚点 Transform」的地方。
//   ⛔ 别拿 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 那份**签名桩**当判据（方法体是空的）。
//
// 【它实现了【全部 36 档】】—— 逐档照抄 `GetPos`（行号 = 上面那份 `.c`）：
//   · `0 self`                      `:183-185,187`   → `abilityData.thisCard(+0x10)` → `GetEffectAnchor()`
//   · `1 cardTarget`                `:186-190`       → `abilityData.targetCard(+0x20)` → `GetEffectAnchor()`
//   · `2 abilityTarget`             `:191-219`       → `param_3` 非空取它；否则退 `+0x20`、再退 `+0x10`
//   · `3 friendlyHero` / `9 enemyPower` `:220-221,321-325` → `GetHero(mgr, isPlayer)` → `GetEffectAnchor()`
//   · `4 enemyHero`                 `:223-231`       → `GetHero(mgr, !isPlayer)` → `GetEffectAnchor()`
//   · `5 boardCenter`               `:232-235`       → `mgr + 0x1d0`（裸 `Transform`）
//   · `6/7/8/10/11`                 `:236-241,255-256` → `BattleHud.GetUIAnchor(档号, isPlayer=false)`
//   · `12 friendlyMana` / `13 enemyMana` `:242-254,255-256` → `GetUIAnchor(12/13, isPlayer)`
//   · `14 nextFriendlyMinionSpot`   `:258-265`       → `CreateTempAbilityTransform(isPlayer, 0, slot=1)`
//   · `15 nextEnemyMinionSpot`      `:266-273`       → `…(isPlayer, 1, slot=1)`
//   · `16 secondFriendlyMinionSpot` `:274-281`       → `…(isPlayer, 0, slot=2)`
//   · `17 thirdFriendlyMinionSpot`  `:282-289`       → `…(isPlayer, 0, slot=3)`
//   · `18 friendlyHand`             `:290-296`       → `GetPlayerHand(mgr, isPlayer)` 的 transform
//   · `19 enemyHand`                `:297-305`       → `GetPlayerHand(mgr, !isPlayer)` 的 transform
//   · `50 friendlyDeck`             `:306-318`       → `GetDeckManager(mgr, isPlayer)` 的 transform
//   · `55 enemyDeck`                `:160-170`       → `GetDeckManager(mgr, !isPlayer)` 的 transform
//   · `60 friendlyPsychicMana`      `:170-178`       → `PlayerManager.GetPsychicManaTransform`
//   · `61 friendlySpiritStoneMana`  `:110-119`       → `GetSpiritStoneManaTransform`
//   · `70 enemyPsychicMana`         `:121-131`       → `GetPsychicManaTransform`（取反对侧）
//   · `71 enemySpiritStoneMana`     `:134-143`       → `GetSpiritStoneManaTransform`（取反）
//   · `80 nextToFriendlyPsychicMana` `:144-155`      → `GetNextToPsychicManaTransform`
//   · `90 nextToEnemyPsychicMana`   `:18-27`         → `GetNextToPsychicManaTransform`（取反）
//   · `100 friendlyPlasmaMana`      `:28-38`         → `GetPlasmaManaTransform`
//   · `110 enemyPlasmaMana`         `:41-51`         → `GetPlasmaManaTransform`（取反）
//   · `120 nextToFriendlyPlasmaMana` `:52-61`        → `GetNextToPlasmaManaTransform`
//   · `130 nextToEnemyPlasmaMana`   `:65-74`         → `GetNextToPlasmaManaTransform`（取反）
//   · `140 createCardPos`           `:75,79-84`      → `GetUIAnchor(140, isPlayer=false)`
//   · `145 stratagemGeneralShowCase` `:78,79-84`     → `GetUIAnchor(145, isPlayer=false)`
//   · `150 friendlyHandWorld`       `:85-97`         → `BattleManager.get_FriendlyHandAnchor()`
//   · `155 enemyHandWorld`          `:98-104`        → `get_EnemyHandAnchor()`
//   · **其余（枚举外的值）**         `:321-329`       → `GetHero(mgr, isPlayer)` → `GetEffectAnchor()`（同 `3/9`）
//   `isPlayer` 一律 = `abilityData.thisCard(+0x10) + 0x40` = `EntityScript.isPlayer`
//   （= **这张卡是不是本地玩家的**，`dump.cs:21963`）⇒ 我们这边 = `ctx.actorSeat == ctx.mySeat`。
//
// 🔴 【与 `BattleManager.GetAnimTransform` 的关系】`GetAnimTransform`
//   （`d:/2/tools/decomp_full/BattleManager__GetAnimTransform.c`，144 行）**只 10 档**，是**另一条路**
//   （`AnimInfo` 直接播 / `CreateAnimFromAnimFxController`）的解析器，在技能路径上**只是
//   `startParent == null` 时的兜底**（T05b §一 问3）。本文件**以 `GetPos` 为准**；两者三处实质不同：
//   `0/1/2` 它 **state-aware**（我们照做）· `14` 它用**行动方**那一侧 · `6-17/60-155` 它有真锚点。
//
// 【`+0x40` 是什么 —— 已现核】= `EntityScript.isPlayer`（`il2cpp_out/dump.cs:21963`，
//   `0x40` 那行现读 `<isPlayer>k__BackingField`）。语义 = **「这张卡是本地玩家的」**，
//   ⛔ 不是「这张卡是行动方的」。
//
// 🔴 【2026-10-16 就地订正（铁律 5）—— `任务/T05` §2·6③ 那句「18/19 极性相反、原版自己拧着」**不成立**】
//   `18 friendlyHand` 按 `isPlayer` 取、`19 enemyHand` 取反 —— 两段**逐句重读后是【自洽】的**：
//   `friendly` = **行动方那一侧**、`enemy` = 对面那一侧，所以「按同一个 `isPlayer` 标志取」本来就该取反。
//   ⇒ 本实现按「先算**行动方座位**、再按座位取」写（见 <see cref="Ctx.actorSeat"/>）。
//
// 【返回什么】原版返回的是**一个 `Transform`**（不是坐标）—— 本类照做。
//   ⚠️ 那些锚点**不在同一个空间里**（场上卡 = 竞技场空间；手牌/牌堆/HUD 锚 = HUD 空间）——
//     把「锚点 → 我们要播特效的那个平面」的换算**留给消费方**（`BattleDriver.AnimAnchorWorld`），
//     ⛔ 别在本类里偷偷做一次换算：那正是 T05 §2·4 那笔「空间」大账要一次理清的东西。
//
// 🔴 【2026-10-17（T05 第 2 步）：本文件从「只 10 档」扩成「照 `GetPos` 全 36 档」】
//   原文件头写「别照 36 档实现」的那句**只对 `GetAnimTransform` 成立**（铁律 5 留痕）；
//   技能这条真路走的是 `GetPos`（36 档全实现）⇒ 现已按其补全。
//   ⚠️ `8 friendlyPower` / `10 nextToCemetery` 是**原版自己就有的死档**：
//     `GetPos` 确实调 `GetUIAnchor(8/10, false)`，可 13 个战场的 `BattleHud.uiAnimAnchors`
//     **逐场都没有 8/10**（T05d §1·4）⇒ `GetUIAnchor` 走 `LogError` + **返回 `BattleHud` 根 transform**。
//     我们**照抄**（⛔ 别为「补齐」硬造一个锚点）。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AbilityLogic.GetPos`（`PositionOption` 36 档 → 锚点）的复刻。
    /// **判据只此一处** —— 调用点别自己 switch。</summary>
    public static class AnimAnchors
    {
        /// <summary>解析的输入。对应原版 `GetPos` 的 `abilityData`（`+0x10` 行动卡 / `+0x20` 目标卡）、
        /// `abilityTarget`（那一格）加上两个「座位」换算量。</summary>
        public struct Ctx
        {
            /// <summary>**行动卡**（原版 `abilityData.thisCard`，`+0x10`）。`0 self` 档用它。</summary>
            public CardView self;
            /// <summary>**目标卡**（原版 `abilityData.targetCard`，`+0x20` ＋ `abilityTarget` 那一格）。
            /// `1 cardTarget` / `2 abilityTarget` 两档用它。</summary>
            public CardView target;
            /// <summary>**行动方座位号**。原版没有这个量，它是从 `acting + 0x40`（`isPlayer`，**本地玩家相对**）
            /// 推出来的：行动方就是「`isPlayer` 为真则本地玩家、否则对面」。
            /// ⚠️ 别把「行动方」直接当成「本地玩家」—— 对手出牌时两者是反的。</summary>
            public int actorSeat;
            /// <summary>**本地玩家座位号**。用来把 `isPlayer` 换算回来（`isPlayer ⇔ actorSeat == mySeat`）。</summary>
            public int mySeat;
        }

        // ── 解析口（由 `BattleDriver.HookAnimAnchors()` 挂上；只有驱动知道视图/棋盘/手牌/HUD 在哪）──
        //    每个都是 `座位（或档号）→ 锚点`。取不到（关着的、没这个座位、没这件）⇒ 返回 null 并**出声**。

        /// <summary>某座位的**督军卡**的 `GetEffectAnchor()`（原版 `3/4` 两档 + `9` + 枚举外兜底那一档）。</summary>
        public static Func<int, Transform> HeroAnchorOf;
        /// <summary>某座位的**手牌容器**的 transform（原版 `18/19` 两档）。</summary>
        public static Func<int, Transform> HandAnchorOf;
        /// <summary>某座位的**牌堆**的 transform（原版 `50/55` 两档）。</summary>
        public static Func<int, Transform> DeckAnchorOf;
        /// <summary>棋盘中心那颗**裸 Transform**（原版 `5 boardCenter` = `mgr + 0x1d0`）。</summary>
        public static Func<Transform> BoardCenterOf;
        /// <summary>某侧「第 N 个可用空位」的锚点（原版 `14/15/16/17` = `AbilityLogic.CreateTempAbilityTransform`）。
        /// 参数 = `(行动方座位, getEnemyPos, slot)`。</summary>
        public static Func<int, bool, int, Transform> MinionSpotOf;
        /// <summary>`BattleHud.GetUIAnchor(档号, isPlayer)` 的映射（原版 `6/7/11/12/13/140/145` 七档）。
        /// 参数 = `(档号, isPlayer)`。⚠️ `8/10` **不在那张表里**（原版死档）⇒ 那两档**不走这里**（见 <see cref="Resolve"/>）。</summary>
        public static Func<PositionOption, bool, Transform> UiAnchorOf;
        /// <summary>`BattleHud` **根节点自己的 transform** —— 原版 `GetUIAnchor` 查不到档时返回它
        /// （`BattleHud__GetUIAnchor.c:61`）；我们这边 = HUD 根（`hudRoot`）。`8/10` 两档要用。</summary>
        public static Func<Transform> HudRootOf;
        /// <summary>某座位的**手牌世界锚**（原版 `150/155` = `BattleManager.get_{Friendly,Enemy}HandAnchor`）。</summary>
        public static Func<int, Transform> HandWorldAnchorOf;
        /// <summary>`PlayerManager` 的三族能量锚（原版 `60/61/70/71/80/90/100/110/120/130`）。
        /// 参数 = `(档号, isPlayer)`。⚠️ **我们 HUD 里没有** Psychic / SpiritStone / Plasma 这三种能量的件
        /// （原版取的是 `PlayerManager.Get{X}ManaTransform` 那颗 `TMP_Text` 的 transform）⇒ 目前**不挂** ⇒ 那 10 档落「取不到」。</summary>
        public static Func<PositionOption, bool, Transform> ManaTransformOf;
        /// <summary>这张卡的 `GetEffectAnchor()` 该走**哪一支**（原版读 `card + 0x228` = `CardStateOptions`）。
        /// `true` = 场上档（`effectsAnchor`）；`false` = 手牌 / 牌堆档（`card2DController`）。
        /// 我们的判据 = **这张视图现在在不在棋盘上**（`0/1/2` 三档要用）。</summary>
        public static Func<CardView, bool> OnBoardOf;

        // ── 诊断（自检盯它们；**不许静默失败**）──
        /// <summary>解析过多少次。</summary>
        public static int Calls { get; private set; }
        /// <summary>其中多少次落进「枚举外的值」那一档（`GetPos` 的 `caseD_3` 兜底）。</summary>
        public static int Fallbacks { get; private set; }
        /// <summary>其中多少次命中「原版自己就是死档」的 `8 friendlyPower` / `10 nextToCemetery`。</summary>
        public static int DeadOptionHits { get; private set; }
        /// <summary>最后一次落「枚举外」兜底的是哪一档（数值 + 名字）。</summary>
        public static string LastFallback { get; private set; }
        /// <summary>最后一次因为「锚点取不到」而返回 null 的原因（**出声**用）。</summary>
        public static string LastMissing { get; private set; }

        /// <summary>每次开一段断言 / 一局之前清零。</summary>
        public static void ResetCounters()
        {
            Calls = 0; Fallbacks = 0; DeadOptionHits = 0; LastFallback = null; LastMissing = null;
        }

        /// <summary>把 36 档解析成一个锚点。取不到 ⇒ 返回 null（并**出声**，⛔ 不静默）。
        /// ⚠️ 原版取不到时走的是 `FUN_1803f47a0()`（**抛异常**，反编译标了 `Subroutine does not return`）——
        /// 我们**不抛**（会把一局打断），改成「记一行 + 返回 null」，由消费方决定不播。这是**我们改的**。</summary>
        public static Transform Resolve(PositionOption opt, Ctx ctx)
        {
            Calls++;
            bool isPlayer = ctx.actorSeat == ctx.mySeat;   // 原版 `abilityData.thisCard(+0x10) + 0x40`

            switch (opt)
            {
                // 0 self —— 原版 `:183-185,187`：`abilityData.thisCard(+0x10)` → `GetEffectAnchor()`（**按卡状态二选一**）
                case PositionOption.self:
                    return CardAnchor(ctx.self, opt);

                // 1 cardTarget —— 原版 `:186-190`：`abilityData.targetCard(+0x20)` → `GetEffectAnchor()`
                case PositionOption.cardTarget:
                    return CardAnchor(ctx.target, opt);

                // 2 abilityTarget —— 原版 `:191-219`：`param_3`（技能目标）非空取它；否则退 `+0x20`、再退 `+0x10`。
                //   我们 `Ctx` 只有一格 `target`（= `abilityTarget` 那一格）⇒ 非空取它、否则退行动卡。
                case PositionOption.abilityTarget:
                    return CardAnchor(ctx.target != null ? ctx.target : ctx.self, opt);

                // 3 friendlyHero / 9 enemyPower —— 原版 `:220-221,321-325`：`GetHero(mgr, isPlayer)` → `GetEffectAnchor()`。
                //   ⚠️ **9 enemyPower 在原版与 3 friendlyHero【同支】**（都 `goto caseD_3` → `GetHero(isPlayer)`）——
                //     不是我们写错，照抄（⛔ 别「顺手」改成对面督军）。
                case PositionOption.friendlyHero:
                case PositionOption.enemyPower:
                    return Hero(ctx.actorSeat, opt);

                // 4 enemyHero —— 原版 `:223-231`：`GetHero(mgr, !isPlayer)` → `GetEffectAnchor()`
                case PositionOption.enemyHero:
                    return Hero(1 - ctx.actorSeat, opt);

                // 5 boardCenter —— 原版 `:232-235`：`mgr + 0x1d0`，**裸 Transform 直接返回**
                case PositionOption.boardCenter:
                    if (BoardCenterOf != null)
                    {
                        var t = BoardCenterOf();
                        if (t != null) return t;
                    }
                    return Missing("boardCenter", "`BoardCenterOf` 没挂 或 它回了 null");

                // 6 boardCenterUI / 7 cemetery / 11 nextToClock / 140 createCardPos / 145 stratagemGeneralShowCase
                //   —— 原版 `:236-241,255-256` / `:75,79-84`：`GetUIAnchor(档号, isPlayer=false)`（这 5 档表里 `useIsPlayer=0`）
                case PositionOption.boardCenterUI:
                case PositionOption.cemetery:
                case PositionOption.nextToClock:
                case PositionOption.createCardPos:
                case PositionOption.stratagemGeneralShowCase:
                    return UiAnchor(opt, false);

                // 12 friendlyMana / 13 enemyMana —— 原版 `:242-254,255-256`：`GetUIAnchor(12/13, isPlayer)`。
                //   表里这两档 `useIsPlayer=1` ⇒ 取不到本地侧就翻到对手侧（12/13 互为镜像）。
                case PositionOption.friendlyMana:
                case PositionOption.enemyMana:
                    return UiAnchor(opt, isPlayer);

                // 8 friendlyPower / 10 nextToCemetery —— **原版自己就是死档**（见文件头）：
                //   `GetPos` 确实调 `GetUIAnchor(8/10, false)`，可 13 个战场的 `uiAnimAnchors` 逐场都没有 8/10
                //   ⇒ `GetUIAnchor` 走 `CustomDebug.LogError` + 返回 `BattleHud` 根 transform。
                //   **照抄原版语义**（⛔ 别为「补齐」硬造锚点）：出声 + 返回 HUD 根。
                case PositionOption.friendlyPower:
                case PositionOption.nextToCemetery:
                    return HudRootForDeadOption(opt);

                // 14 nextFriendlyMinionSpot —— 原版 `:258-265`：`CreateTempAbilityTransform(isPlayer, 0, slot=1)`
                case PositionOption.nextFriendlyMinionSpot:
                    return MinionSpot(ctx.actorSeat, false, 1, opt);
                // 15 nextEnemyMinionSpot —— 原版 `:266-273`：`…(isPlayer, 1, slot=1)`
                case PositionOption.nextEnemyMinionSpot:
                    return MinionSpot(ctx.actorSeat, true, 1, opt);
                // 16 secondFriendlyMinionSpot —— 原版 `:274-281`：`…(isPlayer, 0, slot=2)`
                case PositionOption.secondFriendlyMinionSpot:
                    return MinionSpot(ctx.actorSeat, false, 2, opt);
                // 17 thirdFriendlyMinionSpot —— 原版 `:282-289`：`…(isPlayer, 0, slot=3)`
                case PositionOption.thirdFriendlyMinionSpot:
                    return MinionSpot(ctx.actorSeat, false, 3, opt);

                // 18 friendlyHand / 19 enemyHand —— 原版 `:290-296` / `:297-305`：
                //   `GetPlayerHand(mgr, isPlayer / !isPlayer)` 的 transform。两档**互取反**（friendly = 行动方）。
                case PositionOption.friendlyHand:
                    return Hand(ctx.actorSeat, opt);
                case PositionOption.enemyHand:
                    return Hand(1 - ctx.actorSeat, opt);

                // 50 friendlyDeck / 55 enemyDeck —— 原版 `:306-318` / `:160-170`：同一个 `isPlayer` 标志，两个字段互取反。
                case PositionOption.friendlyDeck:
                    return Deck(ctx.actorSeat, opt);
                case PositionOption.enemyDeck:
                    return Deck(1 - ctx.actorSeat, opt);

                // 60/61/70/71/80/90/100/110/120/130 —— `PlayerManager` 的三族能量锚（原版 `:18-177` 各段）。
                //   ⚠️ 我们 HUD 里**没有** Psychic / SpiritStone / Plasma 这三种能量的件 ⇒ 目前落「取不到」。
                case PositionOption.friendlyPsychicMana:
                case PositionOption.friendlySpiritStoneMana:
                case PositionOption.enemyPsychicMana:
                case PositionOption.enemySpiritStoneMana:
                case PositionOption.nextToFriendlyPsychicMana:
                case PositionOption.nextToEnemyPsychicMana:
                case PositionOption.friendlyPlasmaMana:
                case PositionOption.enemyPlasmaMana:
                case PositionOption.nextToFriendlyPlasmaMana:
                case PositionOption.nextToEnemyPlasmaMana:
                    return Mana(opt, isPlayer);

                // 150 friendlyHandWorld / 155 enemyHandWorld —— 原版 `:85-104`：
                //   `BattleManager.get_{Friendly,Enemy}HandAnchor()`（两档按 `isPlayer` 互取反）。
                case PositionOption.friendlyHandWorld:
                    return HandWorld(ctx.actorSeat, opt);
                case PositionOption.enemyHandWorld:
                    return HandWorld(1 - ctx.actorSeat, opt);
            }

            // 枚举外的值（数据坏了）—— 原版 `:321-329` 落 `caseD_3` = `GetHero(isPlayer)` → `GetEffectAnchor()`。
            Fallbacks++;
            LastFallback = (int)opt + " " + opt;
            Debug.LogError($"[AnimAnchors] `PositionOption.{opt}`（{(int)opt}）**不在原版那 36 档里** —— "
                         + "数据/调用点给了一个非法档号。原版兜底（`GetPos.c:321-329`）= 行动方督军的 `EffectAnchor`，我们照做。");
            return Hero(ctx.actorSeat, opt);
        }

        /// <summary>`0/1/2` 三档：卡的 `GetEffectAnchor()`（原版**按卡状态 `+0x228` 二选一**）。
        /// ⚠️ 原版对卡为空是**抛异常**；我们出声 + 返回 null。</summary>
        static Transform CardAnchor(CardView card, PositionOption opt)
        {
            if (card == null)
                return Missing(opt.ToString(), "那张卡（行动卡 or 目标卡）是 null");
            bool onBoard = true;
            if (OnBoardOf != null) onBoard = OnBoardOf(card);
            else Debug.LogWarning("[AnimAnchors] `OnBoardOf` 没挂 ⇒ `0/1/2` 一律按**场上档**取"
                                + "（= 旧的 `GetAnimTransform` 行为，手牌里的卡可能锚错）");
            return card.EffectAnchor(onBoard);
        }

        static Transform Hero(int seat, PositionOption opt)
        {
            if (HeroAnchorOf == null)
                return Missing(opt.ToString(), "`HeroAnchorOf` 没挂");
            var t = HeroAnchorOf(seat);
            if (t == null) return Missing(opt.ToString(), $"座位 {seat} 的督军锚点取不到");
            return t;
        }

        static Transform Hand(int seat, PositionOption opt)
        {
            if (HandAnchorOf == null)
                return Missing(opt.ToString(), "`HandAnchorOf` 没挂");
            var t = HandAnchorOf(seat);
            if (t == null) return Missing(opt.ToString(), $"座位 {seat} 的手牌容器取不到");
            return t;
        }

        static Transform Deck(int seat, PositionOption opt)
        {
            if (DeckAnchorOf == null)
                return Missing(opt.ToString(), "`DeckAnchorOf` 没挂");
            var t = DeckAnchorOf(seat);
            if (t == null) return Missing(opt.ToString(), $"座位 {seat} 的牌堆取不到");
            return t;
        }

        static Transform MinionSpot(int seat, bool getEnemyPos, int slot, PositionOption opt)
        {
            if (MinionSpotOf == null)
                return Missing(opt.ToString(), "`MinionSpotOf` 没挂");
            var t = MinionSpotOf(seat, getEnemyPos, slot);
            if (t == null)
                return Missing(opt.ToString(), $"座位 {seat} / getEnemyPos={getEnemyPos} / slot={slot} 的空位锚点取不到");
            return t;
        }

        static Transform UiAnchor(PositionOption opt, bool isPlayer)
        {
            if (UiAnchorOf == null)
                return Missing(opt.ToString(), "`UiAnchorOf` 没挂");
            var t = UiAnchorOf(opt, isPlayer);
            if (t == null)
                return Missing(opt.ToString(), "`UiAnchorOf` 回了 null（我们的 HUD 里没有这一档对应的件？）");
            return t;
        }

        /// <summary>`8/10` 两档 —— 原版 `uiAnimAnchors` 里没有它们 ⇒ `GetUIAnchor` 走 `LogError` + 返回根 transform。</summary>
        static Transform HudRootForDeadOption(PositionOption opt)
        {
            DeadOptionHits++;
            Debug.LogError($"[AnimAnchors] `PositionOption.{opt}`（{(int)opt}）—— 原版 `BattleHud.uiAnimAnchors` "
                         + "里**没有这一档**（13 个战场逐场现读，T05d §1·4）⇒ 原版自己就走 `CustomDebug.LogError` + "
                         + "返回 `BattleHud` 根 transform（`BattleHud__GetUIAnchor.c:56-62`）。我们照做。");
            if (HudRootOf != null)
            {
                var h = HudRootOf();
                if (h != null) return h;
            }
            return Missing(opt.ToString(), "`HudRootOf` 没挂 或 它回了 null");
        }

        static Transform Mana(PositionOption opt, bool isPlayer)
        {
            if (ManaTransformOf != null)
            {
                var t = ManaTransformOf(opt, isPlayer);
                if (t != null) return t;
            }
            return Missing(opt.ToString(),
                "我们 HUD 里没有 Psychic / SpiritStone / Plasma 这三种能量的件"
              + "（原版取的是 `PlayerManager.Get{X}ManaTransform` 那颗 `TMP_Text` 的 transform），"
              + "`ManaTransformOf` 也没挂 ⇒ 这一档取不到");
        }

        static Transform HandWorld(int seat, PositionOption opt)
        {
            if (HandWorldAnchorOf == null)
                return Missing(opt.ToString(), "`HandWorldAnchorOf` 没挂");
            var t = HandWorldAnchorOf(seat);
            if (t == null) return Missing(opt.ToString(), $"座位 {seat} 的手牌世界锚取不到");
            return t;
        }

        static Transform Missing(string what, string why)
        {
            LastMissing = what + "：" + why;
            Debug.LogError($"[AnimAnchors] `{what}` 锚点取不到（{why}）⇒ 返回 null、这一件特效不播"
                         + "（⛔ 不退回某个默认点 —— 猜错就是播错地方；原版这一格是抛异常）。");
            return null;
        }
    }
}
