// RuleCore.cs — 简单版规则引擎的核心
//
// 语义来源：（🔴 2026-10-17 订正措辞 —— 这一句原来写的是「语义来源：`d:/warpforge/scripts/
//   rule_core.gd`（4719 行，照着规则书 + 原版反编译核过）」，**那是错的**：那份 `.gd` 不是原版，见下。）
//   · ✅ **权威 = 原版全量反编译** `d:/2/tools/decomp_full/`（26,282 个方法体，2026-09-18 全量）。
//     规则语义一律以它为准；成品卡图的卡面文字/数值与解包资源字段是旁证。
//   · ⚠️ `d:/warpforge/scripts/rule_core.gd` = **我们自己的上一版 Godot 复刻**（70 个 `.gd`），
//     **只能当对照/旁证**（读作「我们当时是怎么写的」），**不能当原版语义判据**。
//     出处 = `CLAUDE.md` 铁律 2 的 2026-09-18 更正 —— 全仓已按该口径订正过。
// **改任何一条规则之前，先回去看反编译** —— 下面每一处的注释都标了出处；
// 注释里凡引 `rule_core.gd:<行>` 的，按「我们上一版当时的做法」读，别当原版结论。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。
//    随机数走 System.Random(seed)，同一个种子必须永远得到同一局。
//
// v1 只实现 5 个关键词：Vanguard / Stealth / Flying / Armour / Shield。
// 其余关键词会被解析出来但不参与结算 —— 用 `UnimplementedKeywords()` 查有哪些被忽略了。
//
// 2026-09-12 增补：**技能与触发**
//   · 触发类关键词（Rally / Strike / Slay / Backlash / Penitence）—— 时机照抄规则书 :161 的 61 关键词表
//   · 主动技能 `Ability:` —— 花掉本单位一次行动放效果（原版「替代行动」的简化版）
//   · 效果文字走 `EffectSpec` 那个封闭文法，**不解析原版 desc**
//   · 每次发生什么都往 `ctx.Signals` 里发一条 **结构化事件**（见 BattleEvent.cs）
//     表现层据此播「部队卡发动技能」「触发效果」这两类特效 —— 这两类以前接不上，
//     就是因为引擎里没有这两种事件。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>🆕 **2026-10-18（A913）**：原版 `BattleResult` 的**逐值复刻**
    /// （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleResult.cs` —— 7 个值、名字与数值一字不差）。
    ///
    /// <para>它是什么：原版 `BattleManager.DeadHero(bool isPlayerDying, BattleResult battleResult)`
    /// 的**第二个实参** = 「这一局是**怎么**结束的」。13 个调用点逐条现读 →
    /// `资料/普查产出_1018/R3_联机三档现核.md` §2·1。</para>
    ///
    /// <para>🔴 **它不过网**（硬判据）：原版 `BattleCommsManager.SendForfeit` 发的是
    /// `PhotonView.RPC(…, System.Array.Empty&lt;T&gt;())` = **空参数表**，对面
    /// `BattleManager.ReceiveEnemyForfeit` **把 2 写死** ⇒ 两端各按**本地知道的那一半**填码，
    /// 协议里从来没有它。⇒ 我们**也不往 `MsgResign` 里塞**（它是个空类）。</para>
    ///
    /// <para>🔴 **只用于调用点日志/文案**（`RuleCore.Forfeit` 里那两句 `ctx.Log`）。
    /// ⛔ **不写进 `ctx` 的任何参与回放/指纹的字段** —— `NetProtocol.Fingerprint` / `StateHash`
    /// 里都没有它，加了会让**旧录像、旧联机局立刻假红**（主对话 2026-10-18 已裁定）。</para>
    ///
    /// <para>🔴 **零 UI 消费者**（原版全量反编译里这个类型只被 6 个文件引用：`DeadHero` /
    /// `BasicBattleEndSequence` / `GameAnalytics`×3 / `SaveMatchWinnerInServer`，**没有任何 UI 文件**）
    /// ⇒ ⛔ 别拿它去改 `EndPanel` 的副标题（那是我们自加的，按码改**不算复刻**）。</para>
    /// </summary>
    public enum BattleResult
    {
        /// <summary>0 —— 没指定（我们拿不到理由时用它，日志里会写明「未指定」；⛔ 不许拿它冒充某一档）。</summary>
        Undefined = 0,
        /// <summary>1 —— 正常打完（督军倒下）。原版唯一一处用它的调用点是 `FinishResolvingAction.c:84`；
        /// 我们这条路由 <see cref="RuleCore.CheckWinner"/> 走，**不经过 <see cref="RuleCore.Forfeit"/>**。</summary>
        BattleVictory = 1,
        /// <summary>2 —— **有人点了投降**（原版 `ClickExitBattle.c:59` 我点退出对局 ·
        /// `ReceiveEnemyForfeit.c:37` 收到对面投降）。</summary>
        Forfeit = 2,
        /// <summary>3 —— **掉线那一档**（原版 `_d__322__MoveNext.c:144` 掉线倒计时到点判对面 ·
        /// `FailedToReconnectAfterDisconnect.c:32` 本机重连失败判自己）。</summary>
        Disconnect = 3,
        /// <summary>4 —— 跳过 / 调试直接判胜（原版 `ClickSkip.c:29` · `DebugWinBattle.c:15` ·
        /// `ClickSkipTutorial.c:42`）。我们**没有**对应的调试入口。</summary>
        WinButton = 4,
        /// <summary>5 —— 取消匹配（原版 `CancelMatch.c:29`）。</summary>
        Cancelled = 5,
        /// <summary>6 —— 调试判平（原版 `DebugDrawBattle.c:14`）。
        /// ⚠️ `GetWinnerAfterBattleEnd` **只对 6 特判成平局** —— 我们引擎的平局走 `Winner = 3`，与这条不是一回事。</summary>
        DrawButton = 6,
    }

    public static partial class RuleCore
    {
        // ---- 规则常量（对齐 rule_core.gd:44-49）----
        public const int BoardSize = BoardSpec.Size;
        public const int WarlordSlot = BoardSpec.WarlordSlot;
        /// <summary>🆕 2026-09-26：**这两个已经不是常量了** —— 它们按模式变
        /// （经典 起手 3 / 上限 10；遭遇 起手 4 / 上限 8），真值住在
        /// <see cref="GameplayVariables"/>，引擎里一律读 `ctx.Vars.startingHand` / `ctx.Vars.handLimit`。
        /// 留这两个**属性**是给「不持有 `BattleContext` 的调用方」（自检断言那几处）用的，
        /// 语义 **= 经典模式的值** —— 别拿它当「本局的起手张数」。
        /// 🔴 原来它们是 `const`；改成属性是为了**消灭第二份常量**（两处写同一条规则 = 迟早不一致）。</summary>
        public static int StartHand { get { return GameplayVariables.Classic.startingHand; } }
        /// <summary>见 <see cref="StartHand"/>（经典 = 10）。</summary>
        public static int HandMax { get { return GameplayVariables.Classic.handLimit; } }
        public const int DefaultWarlordHealth = 30;
        public const int DefaultWarlordAttack = 2;

        static readonly CardDef FallbackWarlord = new CardDef(
            "warlord_default", "Warlord", "hero", "", null, null,
            0, DefaultWarlordAttack, DefaultWarlordHealth, 0, null);

        // ==================================================================
        //  开局
        // ==================================================================

        /// <summary>
        /// 创建对局：提取督军 → 洗牌 → 双方各起手 <see cref="StartHand"/> 张。
        /// 牌组里第一张 <c>type == "hero"</c> 的卡被提为督军，其余进牌库；没有就用默认督军。
        /// </summary>
        /// <param name="shuffle">
        /// 关掉洗牌 → 牌库保持传入顺序，测试就能摆出确定的起手。
        /// （`rule_core.new_battle` 也有这个开关，语义一致。）
        /// </param>
        /// <param name="cardPool">
        /// **全卡池** —— `create` 造牌的候选来源（`CreatePool`）。不传 = 这一局不能造牌
        /// （造牌那条会**如实报**「没有卡池」，不会退化成从牌库里抽）。
        /// 调用方通常就是 `CardDatabase.Load()` 那一份。
        /// </param>
        /// <param name="firstSeat">谁先手（默认 0 = P1，保持老行为）。
        /// 🆕 **教程局会被关卡覆盖**，见 <paramref name="tutorial"/>。</param>
        /// <param name="tutorial">🆕 2026-10-17（B29）：**教程局的关卡执行器**（`null` = 不是教程局 ⇒
        /// 下面每一段教程分支都**一次都不走**，老调用方一个字都不用改）。
        /// 给了它就同时落这几条**引擎级开关**（原版出处逐条见 <see cref="TutorialRules"/>）：
        /// ① **不洗牌**（`MatchData.ShouldShuffleDeck(100) == false`）；
        /// ② **先手照关卡**（`GetPlayerGoesFirst` → `TutorialStage.playerStarts`，S3 是 AI 先手）；
        /// ③ **没有换牌阶段**（教程走 `_TutorialStartSequence`，不是 `_SetupMulliganPhase`）；
        /// ④ **起手 = 关卡指定那几张**（`CreatePlayerDeck`：InHand 插牌库顶 + 抽 N 张）；
        /// ⑤ **起始单位**直接落场（`SetupStartingTroops`）；⑥ **初始法力/伤害**（`SetupInitialMana`）。
        /// ⚠️ 参数没给时按 <see cref="GameplayVariables.Tutorial"/>（别再自己拼一份）。</param>
        public static BattleContext NewBattle(IList<CardDef> deckA, IList<CardDef> deckB,
                                              int seed = 0, bool shuffle = true,
                                              IList<CardDef> cardPool = null,
                                              bool openMulligan = false,
                                              GameplayVariables vars = null,
                                              int firstSeat = 0,
                                              TutorialScript tutorial = null)
        {
            var ctx = new BattleContext(seed);
            // 🆕 教程局：执行器（关卡数据 + 本局指针）**在这里落位**（唯一写点）。
            //    ⛔ 别塞进 `BattleContext` 的字段初始化里 —— 那会变成「每个 ctx 都有教程」。
            ctx.Tutorial = tutorial;
            if (tutorial != null && vars == null) vars = GameplayVariables.Tutorial;
            ctx.CardPool = cardPool == null ? null : new List<CardDef>(cardPool);
            // 🆕 2026-09-26：**模式参数必须在下面任何一步之前落位** ——
            //   督军生命（`BuildPlayer`）、起手张数、换牌开关三样全从它读。
            //   不传 = 经典 ⇒ 老调用方一个字都不用改。
            // 🔴 **存副本、不存共用那份**（2026-09-26 改）：`GameplayVariables.Classic` / `.Skirmish`
            //   是**缓存的单例**，原来这里直接把它落进 `ctx.Vars`，而 `Vars` 是 **public 可变字段**
            //   ⇒ **谁写一句 `ctx.Vars.handLimit = 5`，之后每一局都跟着变**（跨对局的静默污染）。
            //   `Clone()` 本来就是为这件事准备的（此前**零调用点**）。
            ctx.Vars = (vars ?? GameplayVariables.Classic).Clone();
            // 🆕 2026-09-26：**谁先手**（默认 0 = P1，保持老行为）—— 它往下管四件事：
            //   ① 第一张牌谁先出（`Active`）② 起始能量基数 ③ **防御卡发给谁** ④ 加时判哪一边的能量。
            //   ⚠️ 这四处在 2026-09-26 之前**全部写死成「座位 1 = 后手」**。
            // 🆕 2026-10-17（B29）：**教程局的先手由关卡说了算**（原版 `BattleManager.GetPlayerGoesFirst`
            //   的 `matchType == 100` 那一支 ⇒ 取 `PlayerDataManager.currentTutorialStage.playerStarts`）。
            //   判据与另外两条开关一起收在 `TutorialRules` 里，⛔ 别在这里再展开写一遍。
            if (tutorial != null)
            {
                var st = tutorial.Stage;
                // ① **不洗牌**（原版 `MatchData.ShouldShuffleDeck(100) == false`；它唯一调用点 =
                //    `BattleManager.CreatePlayerDeck` —— 洗完再插起手卡，所以「不洗」这件事**必须在建牌库之前**）。
                if (shuffle && !TutorialRules.ShouldShuffleDeck(MatchType.Tutorial))
                {
                    shuffle = false;
                    ctx.Log("教程局：**不洗牌**（原版 `MatchData.ShouldShuffleDeck(Tutorial 100)` = false）"
                          + " —— 牌库保持关卡给的顺序");
                }
                // ② **先手照关卡**（S3 是 AI 先手 —— 关卡数据实测 `playerStarts = false`）。
                firstSeat = TutorialRules.PlayerStarts(st) ? 0 : 1;
                // ③ **没有换牌阶段**（教程走 `_TutorialStartSequence` → 直接 `StartBattlePhase`）。
                openMulligan = false;
                ctx.Log($"教程局：第 {st.stage} 关（`{st.so}`）· 先手 = {(firstSeat == 0 ? "玩家" : "AI")}"
                      + $"（关卡 `playerStarts = {st.playerStarts}`）· 不换牌");
            }
            ctx.FirstSeat = firstSeat == 1 ? 1 : 0;

            // 🔴 **防御卡只发给【后手】**（2026-09-26 更正：原来两边都给 —— 那是一条**已记录的偏离**，
            //   理由是「玩家恒先手 ⇒ 只给后手的话玩家永远看不到自己编的那张」）。
            //   原版语义（规则书 `:105`/`:121` + 反编译 `AddGoesSecondCardToDeck.c:154-166`）：
            //   **后手（防守方）持有**防御卡，用来弥补先手优势；**先手没有**。
            //   ⇒ 要「玩家也能看到它」，正确做法是**让先手真的会换人**（见下面的 `firstSeat`），
            //     而不是两边都发。用户 2026-09-26 指出这一点。
            ctx.Players[0] = BuildPlayer(ctx, deckA, "P1", shuffle, getsDefenceCard: ctx.FirstSeat == 1);
            ctx.Players[1] = BuildPlayer(ctx, deckB, "P2", shuffle, getsDefenceCard: ctx.FirstSeat == 0);
            // 🆕 **2026-10-18（`W5` · 审查 §2 作者自陈 b 那一笔的收尾）**：
            //   原版 `PlayerHand.SetupCardInHand` 的**四个调用点**都在「进手牌」那一刻、**不看卡类型**
            //   （`PlayerHand__SetupCardInHand.c:31/34/35/118`）⇒ 「每一个进手牌入口都要调它」。
            //   ⚠️ `BuildPlayer` 里那次（后手起手就把防御卡放进 `p.Hand`）**当时拿不到座位号** ——
            //      它是 `static PlayerState BuildPlayer(...)`，`ctx.Players[seat]` 此刻还是 `null`
            //      （正是上面这两行的赋值在写它）。⇒ **不硬塞进 `BuildPlayer`**，改成**两边都建好之后**
            //      统一补调一次：语义等价（都发生在「牌已进手牌、对局还没开始行动」这个窗口里），
            //      而且 `SetupCardInHand` 自己的守卫（`ctx.Players[owner] == null ⇒ 0`）在这里天然成立。
            //   ⚠️ **今天这一趟实际是空过**（建场期手牌登记表必然是空的）—— 但**理由不能是「空过」**，
            //      而是「原版每一个入口都调」（审查 §2 b 点名否掉的就是「插了也空过」这个理由）。
            for (int seat = 0; seat < 2; seat++)
                foreach (var handInst in ctx.Players[seat].Hand)
                    SetupCardInHand(ctx, seat, handInst);
            ctx.Active = ctx.FirstSeat;
            ctx.Turn = 0;

            // 轮流发牌（和 rule_core 一致：i 循环里两边各抽一张）
            int startHand = ctx.Vars.startingHand;      // 经典 3 · 遭遇 4（`GameplayVariables.startingHand`）
            if (tutorial == null)
            {
                for (int i = 0; i < startHand; i++)
                {
                    // 🔴 **2026-10-18（`W5` · `D1` §5·1）：起手发牌走 `DealOpeningHand`、不走 `Draw`。**
                    //   差别只有两条：**不发 `When the card is drawn` 广播**、**不置
                    //   `DrawnThisTurn`**（判据见 `DealOpeningHand` 的头注释
                    //   —— 原版 `_ResolveDrawCard_d__432__MoveNext.c:283-295` 把两件事都
                    //   包在 `!IsDuringMulligan` 里）。改之前我们用 `Draw` ⇒ 起手那几张
                    //   会**多发 3 次 `draw` 广播**（`D1` 那两条红的根因之一就是它）。
                    DealOpeningHand(ctx, 0);
                    DealOpeningHand(ctx, 1);
                }
            }
            else
            {
                // ============================================================
                // 🆕 2026-10-17（B29）：**教程局的起手不是抽出来的，是关卡指定好的那一批**。
                //
                // 判据链（三段，逐段可查）：
                //   ① 原版普通局的起手牌是在**换牌阶段**发的 —— `BattleManager._SetupMulliganPhase`
                //      → `PlayerHand.AddCardsToMulligan(hand, isPlayer, count)`，
                //      它算的 `count = scenarioVariables.startingHand(0x1c) + 督军(+0x118) + 后手补偿(0x20)`，
                //      **但被第三个实参覆盖**：`if (param_3 != 0) count = param_3`
                //      （`PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:76`）。
                //   ② 而 `_SetupMulliganPhase` 传进去的那个 `param_3` 就是
                //      **`TutorialStage.playerStartingTroopsInHand.Count`**（`:63` 取 `+0x98`、`:71` 取 `+0xa0`）
                //      —— 也就是说**原版自己就用「关卡那几张」去顶掉起手张数**。
                //   ③ 教程**根本不跑换牌阶段**（`_StartBattleSequence` 里二选一：
                //      `IsTutorialMatch` ⇒ `_TutorialStartSequence`）⇒ 教程的起手只能来自
                //      `CreatePlayerDeck` 那一段（InHand 插牌库顶 → 抽 N 张）。
                //   ⇒ **教程起手 = `{player,enemy}StartingTroopsInHand` 那一批**，
                //     不走「各抽 `startingHand` 张」。S1/S2 那两批是空的 ⇒ 起手 0 张（关卡就是这么设计的：
                //     S1 第一句是「这是你的督军，拖到敌人身上打他」，根本不用手牌）。
                // ⚠️ 这是**有意与普通局分叉**的一段；`tutorial == null` 时一行都不走。
                // ============================================================
                var st = tutorial.Stage;
                var lookup = TutorialRules.PoolLookup(ctx);
                int a = TutorialRules.SetupInitialHand(ctx, st, 0, st.playerStartingTroopsInHand, lookup);
                int b = TutorialRules.SetupInitialHand(ctx, st, 1, st.enemyStartingTroopsInHand, lookup);
                // 起始单位**直接落场**（原版 `SetupStartingTroops`）——
                // 时机在 `_StartBattleSequence` 里、`SetupStartingTroops` 那一支（教程/战役才跑）。
                int placed = TutorialRules.SetupStartingTroops(ctx, st, lookup);
                // 初始法力 / 初始伤害（原版 `SetupInitialMana`；6 关四个值全 0）。
                TutorialRules.SetupInitialManaAndDamage(ctx, st);
                ctx.Log($"开局（教程）：玩家起手 {a} 张 / 对手起手 {b} 张 · 起始单位 {placed} 只"
                      + "（起手卡走 `CreatePlayerDeck`：插到牌库顶再抽 —— 见 `TutorialRules.SetupInitialHand`）");
            }
            // 🆕 2026-09-29：**后手补偿之一 —— 多抽 `secondExtraCards` 张**（原版默认 1）。
            //   出处：原版 `ScenarioVariables.secondExtraCards` → `PlayerHand.GetSecondExtraCardsCount`；
            //   判据 → `资料/加时与冲突模式_原版规格.md` §2.8「先手/后手的四件差异」第 1 条。
            //   **只给后手**（`SecondSeat`）—— 与「防御卡只发后手」同一个口径。
            //   ⚠️ 排在**轮流发牌之后、`SetupStartWith` 之前**：它是起手的一部分（`startingHand + N`），
            //      不是「某张卡必上手」那种额外指定。
            //   ⚠️ 教程那一档 `Vars.secondExtraCards = 0`（【TutorialScenario】）⇒ 这段对教程是空转。
            int extraCards = ctx.Vars.secondExtraCards;
            // 🔴 **2026-10-18（`W5`）：也走 `DealOpeningHand`** —— 这一批是**起手的一部分**
            //   （上限注释里写着：`startingHand + N`，原版它由 `PlayerHand.GetSecondExtraCardsCount`
            //   折进 `AddCardsToMulligan` 的张数）⇒ 与上面那 `startHand` 张同一条口径
            //   （都在换牌阶段之前、都不算「抽到」）。
            for (int i = 0; i < extraCards; i++) DealOpeningHand(ctx, ctx.SecondSeat);
            if (tutorial == null)
            {
                ctx.Log($"开局：双方各起手 {startHand} 张"
                      + (extraCards > 0 ? $"（后手 {ctx.Players[ctx.SecondSeat].Name} 另加 {extraCards} 张）" : "")
                      + $"，{ctx.Players[ctx.FirstSeat].Name} 先手"
                      + $"（防御卡发给了后手 {ctx.Players[ctx.SecondSeat].Name}）");
            }
            // 开局上手（`Start the game with <卡名> in hand.`）—— 见 `CardDef.StartWithInHand`。
            // ⚠️ **排在发完起手牌之后**：它是「**额外**指定某张卡一定在手里」，不是替换起手牌。
            for (int p = 0; p < 2; p++) SetupStartWith(ctx, p);

            // 换牌阶段（原版：抽完起手牌进 `_SetupMulliganPhase`，双方换完才 `StartBattlePhase`）。
            // ⚠️ 默认**关**：这是「要不要进这个阶段」的选择，由调用方说 —— 表现层单机默认开，
            //    规则自检默认关（不然每个用例都要先换一副牌才能验回合 1 的账）。
            // 🆕 2026-09-26：**模式说不行就是不行**（遭遇模式文案 `No mulligan`，
            //    `GameplayVariables.showMulligan = false` ⇒ 这里直接压掉，
            //    表现层那边另有它的守卫 —— **两处都要**，因为自检不经过表现层）。
            ctx.MulliganOpen = openMulligan && ctx.Vars.showMulligan;

            CheckWinner(ctx);
            return ctx;
        }

        // ==================================================================
        //  开局换牌（原版 `MulliganManager` / `PlayerHand.FinishMulligan`）
        //
        //  规则书 :46「**换牌（Mulligan）| 可弃回任意起手牌后重洗补抽**」——
        //  三个动作都要有：**弃回**（进牌库）、**重洗**（洗牌库）、**补抽**（抽同样张数）。
        //  原版这条链在反编译里是明的：`_SetupMulliganPhase` → `MulliganManager.ActivateMulligan`
        //  →（玩家点完）`_FinishMulliganFirstPhase` → `_FinishMulliganFinalPhase`
        //  → **`BattleManager.ShuffleDeck`** → `PlayerHand.CompleteMulliganPhase` → `StartBattlePhase`。
        // ==================================================================

        /// <summary>
        /// 换掉第 `player` 方手里的 `handIndices` 那几张牌：**弃回牌库 → 洗牌 → 补抽同样张数**。
        /// 返回真正换掉的张数（-1 = 不在换牌阶段，调用方该把它报出来，别当成功）。
        ///
        /// ⚠️ 用 `ctx.Rng`（种子化）—— 对局必须可复现（本工程的铁律）。
        /// </summary>
        public static int Mulligan(BattleContext ctx, int player, IList<int> handIndices)
        {
            if (ctx == null || player < 0 || player > 1) return -1;
            if (!ctx.MulliganOpen) return -1;
            if (handIndices == null || handIndices.Count == 0) return 0;

            var ps = ctx.Players[player];

            // 去重 + **从大到小**删 —— 从小到大删的话，删掉一个后面的下标就全错位了
            var idx = new List<int>();
            for (int i = 0; i < handIndices.Count; i++)
            {
                int k = handIndices[i];
                if (k < 0 || k >= ps.Hand.Count || idx.Contains(k)) continue;
                // ⚠️ **防御卡不参与换牌**（2026-09-13 第三十三轮）——
                //    原版是「抽完起手牌 → 换牌 → **再**置入防御卡」（规则书 `:121`），
                //    我们图省事把防御卡放在了**换牌之前**，所以这里必须挡一道，
                //    否则它会**被换掉**，而那是原版流程里不可能出现的事。
                //    ⏭️ 等把「置入」挪到换牌之后，这条就该删掉。
                if (ps.Hand[k].Card.Type == "defence") continue;
                idx.Add(k);
            }
            if (idx.Count == 0) return 0;
            idx.Sort();

            for (int i = idx.Count - 1; i >= 0; i--)
            {
                ps.Deck.Add(ps.Hand[idx[i]]);
                ps.Hand.RemoveAt(idx[i]);
            }

            // 重洗：换回去的牌要**洗匀**，不然对手能从牌库顺序推出你换掉了什么
            //（原版是 `FinishMulliganFinalPhase` 里统一 `ShuffleDeck`，我们在这里洗同一件事）
            Shuffle(ps.Deck, ctx.Rng);

            // 补抽同样张数
            for (int i = 0; i < idx.Count; i++) Draw(ctx, player);

            ctx.Log($"{ps.Name} 换牌 {idx.Count} 张（弃回牌库 → 重洗 → 补抽）");
            return idx.Count;
        }

        /// <summary>换牌阶段结束（双方都决定了）。关掉标志，之后 `Mulligan` 不再有效。</summary>
        public static void EndMulligan(BattleContext ctx)
        {
            if (ctx == null || !ctx.MulliganOpen) return;
            ctx.MulliganOpen = false;
            ctx.Log("换牌阶段结束");
        }

        /// <summary>🆕 2026-09-29（§25）：**记下进攻卡的选择**（原版 `BattleManager.ClickChosenCardDone`
        /// 写 `matchData.playerEnviromentalEffect(+0x90)` / `enemy…(+0x98)`）。
        ///
        /// 时机 = **换牌之后、战斗开始之前**，由**先手方**选（后手方选的是防御卡 —— 那是另一条链，
        /// 走 `ChooseDefensiveCard`）。`slotIdx = -1` = 「不使用进攻卡」。
        /// <paramref name="envSO"/> = 那张卡对应的环境 SO 名；**空串 = 不改环境**
        /// （原版 `_ApplyOffensiveAndDefensiveEffects` 在空串时整个协程早退）。
        /// 🔴 **这一段每场只发生一次**（生效点 = `FinishMulliganFinalPhase`，不在回合结算里）。</summary>
        public static void ChooseOffensiveCard(BattleContext ctx, int seat, int slotIdx, string envSO)
        {
            if (ctx == null || seat < 0 || seat > 1) return;
            ctx.OffensiveSeat = seat;
            ctx.OffensiveSlotIdx = slotIdx;
            ctx.OffensiveEnvSO = envSO ?? "";
            ctx.OffensiveChosen = true;
            ctx.Log(slotIdx < 0
                ? $"先手（P{seat + 1}）选择**不使用进攻卡**（环境按默认）"
                : $"先手（P{seat + 1}）选定进攻卡槽 {slotIdx}"
                  + (string.IsNullOrEmpty(envSO) ? "" : $"（环境 `{envSO}`）"));
        }

        /// <summary>🆕 2026-09-29（§25）：后手方选的**防御卡**槽号（原版那条链与进攻卡成对，
        /// 但**不发环境**）。</summary>
        public static void ChooseDefensiveCard(BattleContext ctx, int seat, int slotIdx)
        {
            if (ctx == null || seat < 0 || seat > 1) return;
            ctx.DefensiveSlotIdx = slotIdx;
            ctx.Log($"后手（P{seat + 1}）选定防御卡槽 {slotIdx}");
        }

        /// <summary>🆕 2026-10-01：把后手方手里那张防御卡**换成玩家/ AI 选的那张**
        /// （原版 `ClickChosenCardDone` 之后那条链：防御卡那一路的落点是「**后手方手牌**」）。
        ///
        /// 为什么需要它：我们开战时已经由 `DeckBuilder` 随机补了一张（= 原版 `AddGoesSecondCardToDeck`
        /// 那条路，只在特定 matchType 上跑）⇒ 面板上再挑一张时**必须先把原来那张摘掉**，
        /// 否则手里会有两张防御卡。
        /// ⚠️ 摘的是**任意一张 `Type == "defence"`**（原版那两条路不会同时给两张 ⇒ 我们这边最多一张）。
        /// ⚠️ 时机：这条链跑在**换牌之后**（原版 `FinishMulliganFirstPhase` → `SetupEnviromentalEffectPhase`）
        /// ⇒ 不会打乱起手牌/换牌那一套。</summary>
        public static void SetDefensiveCard(BattleContext ctx, int seat, CardDef card)
        {
            if (ctx == null || seat < 0 || seat > 1 || card == null) return;
            var ps = ctx.Players[seat];
            int removed = 0;
            for (int i = ps.Hand.Count - 1; i >= 0; i--)
                if (ps.Hand[i] != null && ps.Hand[i].Card != null && ps.Hand[i].Card.Type == "defence")
                {
                    ps.Hand.RemoveAt(i);
                    removed++;
                }
            ps.Hand.Add(ctx.NewInstance(card));
            // 🆕 2026-10-18（`W4` 整改 · 审查 §2 作者自陈 b）：**照原版口径，进手牌就调**
            //    （`PlayerHand__SetupCardInHand.c:31/34/35/118` **不看卡类型**）。
            //    这一处两步都空过：① 防御卡不是单位（`HandEffectFits` 那一档会拒）；
            //    ② 这一步跑在**换牌之后、开战之前** ⇒ 手牌登记表本来就是空的。
            //    ⚠️ 之所以还是插上：**理由不能是「插了也空过」**（那是拿结果当借口），
            //      而是「原版每一个进手牌入口都调」——
            //      `BuildPlayer` 里那一处（后手起手发防御卡）**拿不到座位号**（`BuildPlayer` 没有
            //      玩家下标参数）⇒ 那一处如实留在报告里，不硬塞。
            SetupCardInHand(ctx, seat, ps.Hand[ps.Hand.Count - 1]);
            ctx.Log($"后手（P{seat + 1}）的防御卡换成「{card.Name}」"
                  + $"（原版 `ClickChosenCardDone` → 进后手方手牌；先摘掉了原来那张 {removed} 张）");
        }

        /// <param name="getsDefenceCard">**这一方是不是后手**（只有后手才发防御卡，判据见下面那段注释）。
        /// 🆕 2026-09-26 加：原来两边都发，那是一条已记录的偏离。</param>
        static PlayerState BuildPlayer(BattleContext ctx, IList<CardDef> deck, string name, bool shuffle,
                                       bool getsDefenceCard)
        {
            Random rng = ctx.Rng;
            var p = new PlayerState { Name = name };
            CardDef warlordCard = null;
            CardDef defenceCard = null;      // 防御卡 —— 进**手牌**，不进牌库（见下）

            if (deck != null)
            {
                foreach (var c in deck)
                {
                    if (c == null) continue;
                    if (warlordCard == null && c.Type == "hero") warlordCard = c;
                    // 防御卡和督军一样是**独立的一格**（规则书 `:45`：1 督军 + 1 防御卡 + 30 张）。
                    // **分流判据只此一处** —— `DeckBuilder.FromDeck` 只是把它放进牌表，不判断去处。
                    else if (defenceCard == null && c.Type == "defence") defenceCard = c;
                    else p.Deck.Add(ctx.NewInstance(c));      // 初始牌库：**每一张各发一份实例**
                }
            }

            if (shuffle) Shuffle(p.Deck, rng);

            // 督军也是「一份」：实例由对局计数器发（第 7 行第 1 步）。
            // ⚠️ 督军**不进**手牌/牌库/弃牌堆（阵亡时 `CleanupDeaths` 直接 return），
            //    所以这一份的身份暂时只有「棋盘那一个单位」在用 —— 但发的号必须与别处同源。
            p.Warlord = new UnitState(ctx.NewInstance(warlordCard ?? FallbackWarlord), true);
            p.Warlord.Exhausted = false;        // 督军不受「部署当回合不可行动」约束
            // 🆕 2026-09-26：**督军生命增减**（遭遇模式 −10）。原版文案逐字：
            //   `Warlords start with 10 less Health`（`资料/加时与冲突模式_原版规格.md` §2.2）。
            //   走 `GameplayVariables.warlordLifeChange`（经典 0 ⇒ 这一段一次都不走，老行为逐字不变）。
            //  ⚠️ `MaxHealth` 与 `Health` **都要改** —— 只改 `Health` 的话第一个回合结束的
            //     「回复到上限」那类效果会把它加回去。
            if (ctx.Vars.warlordLifeChange != 0)
            {
                p.Warlord.MaxHealth = System.Math.Max(1, p.Warlord.MaxHealth + ctx.Vars.warlordLifeChange);
                p.Warlord.Health = p.Warlord.MaxHealth;
            }
            p.Board[BoardSpec.WarlordSlot] = p.Warlord;

            // ---- 防御卡：**开局就在手里**（2026-09-13 第三十三轮；2026-09-26 改成只给后手）----
            // 规则书 `:105`「后手（防守方）可打出的特殊战术」· `:121`「**后手**取得防御卡
            // （**抽牌后置入起手牌**）」 —— 它不是抽来的，所以**不参与洗牌、也不进牌库**。
            // 🔴 **2026-09-26 更正**：原来这里写着「两处我们挑的」，其中**①**已经改掉 ——
            //   **原来的错**：两边都给，理由写成「我们还没做掷骰决定先手，玩家恒先手 ⇒ 严格照规则书的话
            //   玩家永远看不到自己编的那张防御卡」。**那个理由本身是成立的，但解法错了**：
            //   正确的解法是**让先手真的会换人**（`NewBattle` 的 `firstSeat`），而不是**把补偿发给先手** ——
            //   两边都发等于**先把后手优势抹平、再加一条不属于先手的补偿**。
            //   用户 2026-09-26 原话：「防御卡的意义就是当玩家后手的时候的补偿……**先手没有防御卡的**」。
            //   判据：规则书 `:105`/`:121` + 反编译 `BattleManager__AddGoesSecondCardToDeck.c:154-166`
            //   （只有 `playerGoesFirst == false` 那一方拿）。
            // ② **放在换牌之前**（这条**仍然是我们挑的**）：规则书说「抽牌后置入」，我们是在换牌阶段
            //   **之前**就给了。为此 `Mulligan` 里加了一条「防御卡不许换掉」——否则会被换走，
            //   而换牌发生在「置入」之前是原版没有的状态。
            if (getsDefenceCard && defenceCard != null) p.Hand.Add(ctx.NewInstance(defenceCard));
            // 🔴 **2026-09-29 补的（原来这里是【静默】丢掉的）**：先手那一方拿到的防御卡**直接没了**，
            //   一句日志都没有 —— 而「不许静默失败」是本项目的红线。现在如实打出来。
            //   ⚠️ 走 `Debug.Log` 而不是 `ctx.Log`：后者会进**对局内的战斗日志面板**，玩家看这个没意义。
            else if (defenceCard != null)
                UnityEngine.Debug.Log($"[RuleEngine] {p.Name} 是**先手** ⇒ 不发防御卡（原版：防御卡是后手补偿）"
                                    + $"—— 卡组里那张「{defenceCard.Name}」这一局用不上（**这是原版行为**，不是丢了）");
            return p;
        }

        /// <summary>
        /// **这张牌现在要几费** —— 卡面印的费用 + 本方的费用修正。
        ///
        /// **判据只此一处**：能不能打（`CanPlayCard` / `CanPlayTactic`）、扣费（`PlayCard` /
        /// `PlayTactic`）、AI 挑牌、卡面显示，全都问它。各写各的话会出现
        /// 「画面显示 2 费、点下去说能量不够」这种对不上的毛病。
        ///
        /// ✅ 2026-09-13 第三十三轮起**按卡 id 匹配**（`CostMod.Key` = `CardDef.Id`）——
        ///    以前是按**卡名**匹配的，后果是**同名卡一起降价**（原版有跨阵营同名卡，
        ///    给一张降费会连另一阵营那张一起降；⚠️ 2026-10-17 订正：这里原来举的例子是
        ///    `Bladeguard Veteran`（DarkAngels / Ultramarines）—— 那组的「同名」是 09-13 一次
        ///    错改名的产物、10-17 已撤回，UM 那张现在叫 `Bladeguard Lieutenant`，同名组 4 → 3；
        ///    判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①）。
        ///    卡表 v6 起每张卡都有稳定 id，这个身份问题就不存在了 —— **不要再改回按卡名**。
        ///    卡组构筑（`DeckBuilder`）和候选池筛选（`CreatePool`）**用印的费用**，不走这里 ——
        ///    那是「这张牌的数值」，不是「这一局打它要花多少」。
        /// </summary>
        public static int CostOf(BattleContext ctx, int owner, CardDef c)
        {
            return CostOf(ctx, owner, c, null);
        }

        /// <summary>
        /// **这一份**现在要几费（第 7 行第 3 步）。
        ///
        /// 🔴 **能拿到实例的地方就该用这一个**：按份的费用修正（`CostMod.HandInstanceId`）**只有它看得见**。
        /// 卡模板那个重载返回的是「不限份的那部分」——它是给**拿不到实例**的场合用的
        /// （卡面预览、卡池里的卡），不是给「手牌里这一张能不能打」用的。
        /// </summary>
        public static int CostOf(BattleContext ctx, int owner, CardInstance inst)
        {
            return inst == null ? 0 : CostOf(ctx, owner, inst.Card, inst);
        }

        /// <summary>
        /// **费用判据只此一处**（模板 + 可选的「哪一份」）。
        /// `inst == null` = 这次查询不区分份 ⇒ **跳过按份登记的修正**（见 `CostMod.HandInstanceId`）。
        /// </summary>
        static int CostOf(BattleContext ctx, int owner, CardDef c, CardInstance inst)
        {
            if (c == null) return 0;

            // ---- `This costs N less if you control a unit with <关键词>`（**静态条件降费**）----
            // 2026-09-13 A4 批 1 加。`Fate Inescapable`：`This costs 1 less if you control a unit with Stealth`。
            // ⚠️ 它**不进 `ctx.CostMods`** —— 那套是「记一条、可过期、按卡 id 匹配」的语义，
            //    而这一个是**常驻条件**：场上还有那样的单位就便宜，人一没了立刻恢复原价。
            //    所以在这里**每次现算**，判据也只此一处。
            // ⚠️ 这条要在 `CostMods.Count == 0` 的早退**之前**判 ——
            //    否则「一张修正都没有」的对局里这类卡永远不会便宜（静默失效）。
            int v = c.Cost;
            for (int i = 0; i < c.CostIfControls.Count; i++)
            {
                var r = c.CostIfControls[i];
                if (ControlsKeyword(ctx, owner, r.Keyword)) v += r.Delta;
            }

            // `Costs 1 less for each card in enemy hand`（`Patriarch`，Genestealers，2026-09-14 A5 批 4）——
            // **每次现算**、**不登记 `ctx.CostMods`**（对手手牌一直在变，登记成快照当场就错了），
            // 和上面那条「静态条件降费」走同一条路。
            // ⚠️ **必须排在 `CostMods.Count == 0` 的早退之前** —— 否则一张费用修正都没有的对局里
            //    这张卡永远不会便宜（静默失效，和 `CostIfControls` 当初踩的是同一个坑）。
            if (c.CostPerEnemyHandCard > 0 && ctx != null && owner >= 0 && owner <= 1
                && ctx.Players[1 - owner] != null)
                v -= c.CostPerEnemyHandCard * ctx.Players[1 - owner].Hand.Count;

            if (ctx == null || ctx.CostMods.Count == 0) return System.Math.Max(0, v);

            string key = c.Id;          // **稳定 id**（2026-09-13 第三十三轮起；以前是卡名，见上面那条）
            for (int i = 0; i < ctx.CostMods.Count; i++)
            {
                var m = ctx.CostMods[i];
                if (!CostModApplies(m, ctx, owner, c, key, inst)) continue;
                v += m.Delta;
            }
            return System.Math.Max(0, v);
        }

        /// <summary>
        /// 本方场上有没有**带这个关键词**的单位（`This costs 1 less if you control a unit with Stealth`）。
        ///
        /// **判据只此一处** —— 将来别的卡要判「控制着带 X 的单位」也读它，别各写各的。
        /// 关键词比对走 <see cref="UnitState.Has"/>（它把**授予来的**关键词也算上）。
        /// </summary>
        public static bool ControlsKeyword(BattleContext ctx, int owner, string keyword)
        {
            if (ctx == null || string.IsNullOrEmpty(keyword)) return false;
            var p = ctx.Players[owner];
            if (p == null) return false;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = p.Board[s];
                if (u != null && u.IsAlive && u.Has(keyword)) return true;
            }
            return false;
        }

        /// <summary>
        /// 一条费用修正在**这个场合**成不成立（`CostOf` 的判据，**只此一份**）。
        ///
        /// 四个维度，都要满足（没写的维度不参与）：
        ///   · **哪一份**（<see cref="CostMod.HandInstanceId"/>；`0` = 不限份。第 7 行第 3 步加）
        ///   · **谁**（<see cref="CostMod.HandOf"/> / <see cref="CostMod.Player"/>）
        ///   · **哪张**（<see cref="CostMod.Key"/> 卡 id · <see cref="CostMod.Criteria"/> 筛选条件）
        ///   · **到什么时候**（<see cref="CostMod.ExpireTurn"/>）
        /// </summary>
        /// <param name="inst">
        /// 这次查询是在问**哪一份**；`null` = 不区分份（卡面预览 / 卡池）。
        /// **按份登记的修正**在不区分份的查询里**一律不算** —— 它根本回答不了「是哪一份」。
        /// </param>
        static bool CostModApplies(CostMod m, BattleContext ctx, int owner, CardDef c, string key,
                                   CardInstance inst = null)
        {
            if (m.ExpireTurn >= 0 && ctx.Turn > m.ExpireTurn) return false;

            // **钉在某一份上**的修正：只认那一份
            if (m.HandInstanceId != 0 && (inst == null || inst.Id != m.HandInstanceId)) return false;

            // **作用在哪一方的手牌**上。`HandOf >= 0` 的只有 `… cards in the enemy hand cost N more`
            // 那种（正主是**对手手里**的牌）；`-1` = 不限、按老规矩只看 `Player` ——
            // 旧的降费全走 `-1`，行为与加这一维之前**完全一致**。
            if (m.HandOf >= 0)
            {
                if (m.HandOf != owner) return false;
            }
            else if (m.Player != owner) return false;

            // 卡 id 与筛选条件是**与**关系：两个都写了就都要满足
            if (m.Key != "*" && m.Key != key) return false;
            if (m.Criteria != null && !m.Criteria.IsEmpty && !m.Criteria.Matches(c)) return false;
            return true;
        }

        /// <summary>
        /// **开局上手** —— 卡面 `Start the game with &lt;卡名&gt; in hand.`（2026-09-14 A5 批 4）。
        ///
        /// 实测**只 2 张督军**：`Logan Grimnar`（`Tyrnak and Fenrir`）·
        /// `Sylar Hexcorn`（`an Abaddon's Chosen` —— 注意卡池里那张叫 `Abaddons Chosen`，**没撇号**，
        /// 靠 `CreatePool.Norm` 的归一才配得上）。
        ///
        /// 这是**开局长效**，不是「结算得了的效果」：那一刻连回合都还没开始。
        /// 判据只此一处 —— 名字由 `CardDef.StartWithInHand`（解析层）抽好，这里只管发牌。
        ///
        /// ⚠️ **牌库里有就先从牌库拿**（那是同一张卡，凭空多一张会改变整副牌的构成）；
        ///    牌库里没有才**补一张**，并且**如实打日志**，不静默。
        /// </summary>
        static void SetupStartWith(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];
            if (ps == null || ps.Warlord == null || ps.Warlord.Card == null) return;
            if (ctx.CardPool == null || ctx.CardPool.Count == 0) return;   // 没给卡池就没得查，静默跳过
            foreach (string name in ps.Warlord.Card.StartWithInHand)
            {
                var card = CreatePool.FindByName(ctx.CardPool, name);
                if (card == null)
                {
                    ctx.Log($"{ps.Name} 的督军「{ps.Warlord.Name}」开局要把「{name}」放进手牌，"
                          + "但卡池里查不到同名卡 —— **这条没生效**");
                    continue;
                }
                int at = -1;
                for (int i = 0; i < ps.Deck.Count; i++)
                    if (ReferenceEquals(ps.Deck[i].Card, card)) { at = i; break; }
                // 第 7 行第 2 步：牌库里有就**挪那一份**（沿用实例），没有才**补一张**（发新实例）
                var give = at >= 0 ? ps.Deck[at] : ctx.NewInstance(card);
                if (at >= 0) ps.Deck.RemoveAt(at);
                ps.Hand.Add(give);
                SetupCardInHand(ctx, p, give);     // 🆕 `A885` ②：进手牌就补登记表上的效果
                ctx.Log($"{ps.Name}：「{ps.Warlord.Name}」开局把「{card.Name}」直接放进手牌"
                      + (at >= 0 ? "（从牌库里拿的）" : "（牌库里本来没有，**补了一张**）"));
            }
        }

        /// <summary>清掉已过期的费用修正（回合结束时调）</summary>
        static void ExpireCostMods(BattleContext ctx)
        {
            for (int i = ctx.CostMods.Count - 1; i >= 0; i--)
                if (ctx.CostMods[i].ExpireTurn >= 0 && ctx.Turn > ctx.CostMods[i].ExpireTurn)
                    ctx.CostMods.RemoveAt(i);
        }

        /// <summary>
        /// **用完即销**的费用修正 —— 打出一张牌、费用**真的付掉之后**调（2026-09-14 A5 批 3 第 2 条）。
        ///
        /// 卡面：`Your next Stratagem this turn costs 0`（`Winged Tyrant`）那一族共 8 句，
        /// 解析层把 `next` 记进 `EffectOp.NextOnly`、登记时落到 <see cref="CostMod.Once"/>。
        ///
        /// 🔴 **为什么必须在这里撤、不能在 `CostOf` 里撤**：`CostOf` 是**纯查询**，
        ///    一局里被反复调用（能不能打得起、表现层显示多少费、`CanPlayCard` 预判…）。
        ///    在那儿撤 = 看一眼就把修正烧掉了（而且**看不出错**）。
        ///
        /// ⚠️ 调用点**只有两个**（单位 / 战术各一），都在**扣完费之后**：
        ///    `PlayCard`（单位）与 `EffectResolver.PlayTactic`（战术）。
        ///    加新出口时**别忘了这一句** —— 漏了就是「一次性修正永远不过期」。
        /// </summary>
        public static void ConsumeOnceCostMods(BattleContext ctx, int p, CardInstance inst)
        {
            if (ctx == null || inst == null || inst.Card == null) return;
            var card = inst.Card;
            for (int i = ctx.CostMods.Count - 1; i >= 0; i--)
            {
                var m = ctx.CostMods[i];
                if (!m.Once) continue;
                // 第 7 行第 3 步：带上 `inst` —— 按份登记的「下一张」只对**那一份**有效
                // （不带的话：手里两张同名时打哪一张都会销掉它，那是改之前的近似）。
                if (!CostModApplies(m, ctx, p, card, card.Id, inst)) continue;
                ctx.CostMods.RemoveAt(i);
                ctx.Log($"「{card.Name}」用掉了那条**一次性**费用修正（{m}）—— 它只对「下一张」有效");
            }
        }

        /// <summary>Fisher–Yates。用 ctx 的种子化随机源，对局才可复现</summary>
        static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        // ==================================================================
        //  回合
        // ==================================================================

        /// <summary>
        /// 回合开始：能量 → 解疲劳 → 抽牌。（rule_core.begin_turn）
        ///
        /// ⚠️ **能量按「每方自己的回合数」算，不是全局回合数。**
        ///    rule_core.gd 记着这条修正：「此前按全局回合数 → 后手首回合 2 能 /
        ///    先手第 2 回合 3 能，全对局能量曲线偏高」。
        ///    结果就是：先手第 1 回合 2 能、后手第 1 回合也是 2 能。
        /// </summary>
        public static void BeginTurn(BattleContext ctx)
        {
            if (ctx.IsOver) return;

            ctx.Turn++;
            // 「本回合」的费用修正到期 —— **必须在 Turn++ 之后**：
            // 它是在**上一回合**登记的（`ExpireTurn = 登记时的 Turn`），
            // 放到 `EndTurn` 里撤的话那会儿 Turn 还没变，会晚撤一个回合（撞到过）。
            ExpireCostMods(ctx);
            ctx.DiedThisTurn = 0;      // 「本回合阵亡数」按回合清零（`For each one that dies …` 用）
            var p = ctx.ActivePlayer;
            // 「这回合从牌库抽到的牌」也按回合清零（传送 `Teleport` 判据的来源，
            // 见 `CardInstance.DrawnThisTurn`）—— 只清**当前行动方**名下的：
            // 另一方上一回合抽的牌在它的回合开始时就该失效，而那个时刻就是这里。
            // 🔴 **2026-09-18 第 3 步**：账从「每玩家一个 `Dictionary`」搬到了**每一份**上，
            //    所以这里改成扫**本方三个区域**（手牌/牌库/弃牌堆）把标记清掉
            //    —— 抽到的牌只可能在这三处（上场了就无所谓了：`Teleport` 只在部署那一刻判）。
            for (int i = 0; i < p.Hand.Count; i++) if (p.Hand[i] != null) p.Hand[i].DrawnThisTurn = false;
            for (int i = 0; i < p.Deck.Count; i++) if (p.Deck[i] != null) p.Deck[i].DrawnThisTurn = false;
            for (int i = 0; i < p.Discard.Count; i++) if (p.Discard[i] != null) p.Discard[i].DrawnThisTurn = false;
            p.TurnCount++;
            // `Choose a friendly troop that died **since your last turn**` 的窗口起点。
            // 记在 `Turn++` 之后 = 「本回合开始的那一刻」，见 `PlayerState.LastTurnStartMark`。
            p.LastTurnStartMark = ctx.Turn;
            // 🆕 2026-09-26：**能量合成改成从 `ctx.Vars` 读**（经典/遭遇只是同一式子代两套值）。
            //   式子 = `(后手 ? startingManaSecond : startingMana) + TurnCount × manaPerTurn`
            //   —— **字段语义照原版**（`BattleManager__SetupInitialMana.c:20,28,46`：`playerGoesFirst`
            //      XOR「哪一侧」在 `startingMana` / `startingManaSecond` 之间选一个当基数）
            //   · 经典 (1,1,1)：第 1 个自己的回合 = 1+1 = **2** —— **与改造前逐字一致**
            //   · 遭遇 (1,2,2)：P1 第 1 回合 = **3** · P2 第 1 回合 = **4**
            //     —— 正好是原版文案那句 `P1 3 Energy P2 4 Energy`
            //   ⚠️ 判「后手」**走 `ctx.SecondSeat`**（座位 0/1 都可能先手，见 `BattleContext.FirstSeat`）。
            //   ⚠️ `p.Energy` 在这个时刻**还是上一回合剩下的**（下面那行才覆盖它）——
            //      遭遇模式的「存能量」就靠这一点，见下。
            p.Energy = 0;                                  // 先清掉，下面按 MaxEnergy 满上
            p.MaxEnergy = (p == ctx.Players[ctx.SecondSeat] ? ctx.Vars.startingManaSecond : ctx.Vars.startingMana)
                        + p.TurnCount * ctx.Vars.manaPerTurn
                        + p.ManaCarry                     // 上一回合**结算**出来的结转（经典恒 0）
                        // 🆕 2026-10-17（B29）：**教程关卡的初始法力增量**（原版 `SetupInitialMana`：
                        //   `ResetMana(manager, 模式基数 + 关卡 starting*Mana, …)`）。
                        //   非教程局恒 0（`ctx.Tutorial == null` ⇒ 这一项不加）。
                        //   ⚠️ 6 关的 `startingPlayerMana`/`startingEnemyMana` **实测全是 0** ⇒ 今天不改变局面，
                        //      但机制照做（铁律 11）。⚠️ 我们这边能量是**每回合重算**的，所以它落成
                        //      **每回合都加的增量**，而不是「一次性起始值」。
                        + (ctx.Tutorial == null ? 0 : TutorialRules.ManaBonus(ctx.Tutorial.Stage, ctx.Active));
            p.ManaCarry = 0;                               // 已兑现 ⇒ 清掉；下一次在 `EndTurn` 里重算
            p.Energy = p.MaxEnergy;

            // ---- 🆕 加时（Overtime）判定 ----
            // **每回合开始判一次，判过不再判**（原版 `BattleManager._NextTurn` 那道 `if (!IsOvertime)` 闸）。
            // 判据 = **后手那一方的最大能量达阈值**（用户 2026-09-17 给，中文规则书 :51 原文
            // 「后手玩家最大能量达 10 时进入」加时）。
            // ⚠️ **与回合时钟没有任何关系** —— 原版那一段里一个 `ClockManager` 调用都没有。
            // ⚠️ 位置必须在 `MaxEnergy` 更新**之后**（后手方自己那回合开始时能量才涨到阈值）。
            // 🆕 2026-09-26：阈值从 `ctx.Vars.overtimeTurn` 读（**两个模式都是 10**），
            //   而它是**可空的**（照原版 `Nullable<int>` 的形状）：`null` ⇒ 这一局永不进加时
            //   —— 原版就是拿 `null` 表达「服务器不下发这个值」的。⚠️ 目前两个模式都有值。
            //
            // 🔴 **判定必须用 `>=`，不能是 `==`**（用户 2026-09-26 给的判据：**达到「或者超过」10**）：
            //   遭遇 `manaPerTurn = 2` + `manaAccumulation = 1` ⇒ 后手 MaxEnergy 走 **4 → 7 → 9 → 11**，
            //   **跳过 10** ⇒ 写成 `== 10` 的话**遭遇永远进不了加时**。
            //   `>=` 同时也就是「遭遇第 4 个自己的回合就进（经典要第 9 个）」= 文案那句 `Overtime begins earlier`。
            // 📌 **位置也关键**：判定在 `MaxEnergy` 更新**之后**、**抽牌之前** ⇒
            //   **进入的那一回合就抽 2 张**（用户点名的行为）；而 `IsOvertime` 是共享标志 ⇒ **双方都抽 2**。
            // ⛔ 2026-09-26 这里曾被按「遭遇没有加时」改成 `null` —— 用户当天更正「是我搞错了」，已改回。
            //   全过程留痕 → `GameplayVariables.overtimeTurn` 的注释。
            // 出处 = `资料/加时与冲突模式_原版规格.md` §1.1 / §1.3 / §1.7。
            if (!ctx.IsOvertime && ctx.Vars.overtimeTurn.HasValue
                && ctx.Players[ctx.SecondSeat].MaxEnergy >= ctx.Vars.overtimeTurn.Value)
            {
                ctx.IsOvertime = true;
                // ⚠️ 报的是**后手那一方**的能量 —— 别再写 `Players[1]`：先手是**掷硬币**定的
                //    （`ctx.FirstSeat` 可以是 1），写死 1 会报成先手那一方的数。
                ctx.Log($"★ 进入加时（后手方最大能量已达 {ctx.Players[ctx.SecondSeat].MaxEnergy}）—— 此后每回合多抽 "
                      + $"{DeckRules.OvertimeExtraDraw} 张");
            }

            // ---- 🆕 伏击（`Ambush`）：**撑到自己的下个回合 ⇒ 翻开并触发效果**（规则书 `:166`）----
            // 「下次回合前若被伤害：翻开无效果；若**未被伤害**：翻开并触发效果」——
            // 窗口的终点就是**控制者下一个回合开始**这一刻（另一条出口在 `ApplyDamage`）。
            RevealAmbush(ctx, ctx.Active);

            // 「直到你的下个回合」的限时增益，在**施放者自己的回合开始时**撤            // （`rule_core.gd:3019`）。两边场上都要扫 —— buff 可能在对方单位身上（`give -1 attack to an enemy`）。
            int reverted = 0;
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u2 = ctx.Players[pl].Board[s];
                    if (u2 != null) reverted += u2.RevertBuffs(false, ctx.Active);
                }
            if (reverted > 0) ctx.Log($"（{reverted} 条「直到你下个回合」的增益到期）");

            // ---- 失明到期（卡面写 `until your next turn`）----
            //      **在施放者自己的下一个回合开始时清**，和「直到你的下个回合」的限时增益同一个口径
            //      （那一条就在上面几行 `RevertBuffs(false, ctx.Active)`）。
            //      结果：卡在**对手的整个回合**里都还有效 —— 那正是这张牌的用处。
            //      ⚠️ 原版这条清除读的是 `blind_turn`、写的是 `blind_turn_end`（字段名对不上），
            //         所以原版实际表现为**永不恢复**。我们按卡面语义实现，不照抄那个笔误。
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u == null || !u.IsBlind) continue;
                    if (u.BlindOwner != ctx.Active) continue;        // 只按**施放者**的回合算
                    if (u.BlindTurnEnd >= 0 && ctx.Turn >= u.BlindTurnEnd)
                    {
                        u.IsBlind = false;
                        u.BlindTurnEnd = -1;
                        u.BlindOwner = -1;
                        u.RemoveAll("blind");
                        ctx.Log($"{u.Name} 的失明恢复（远程攻击力回到 {u.RangedAttack}）");
                    }
                }

            // ---- 🔴 `turn_setup` 相位（原版 `OnTurnSetup` · **抽牌之前**）----
            //    原版在这里做的是「**按 owner 清理过期的 activeEffects**」（`CardScript__OnTurnSetup.c`
            //    遍历 `activeEffects` 列表、按 `+0xa0` 的归属匹配 ⇒ `SendRemoveEffect`）—— 对应我们上面
            //    那两段（**限时增益到期** `RevertBuffs` · **失明到期**）。
            //    ⚠️ **本相位目前没有独立钩子**：我们的 `ResolveAtTurn` 只认 `turn_start` / `turn_end`
            //       —— 原版 `OnTurnSetup` 那份「清理」已经由上面两段直接做了，**加一个空钩子没有意义**。
            //       真出现「只在 `OnTurnSetup` 触发的效果」时，在这里加 `ResolveAtTurn(ctx, "turn_setup")` 即可
            //       （`ResolveAtTurn` 对未知相位是**安全**的：`EffectText.SplitAtTurn` 出来的 `at[0] != phase` ⇒ continue）。

            // 加时里**多抽一张**（原版 `_NextTurn` 只做这一件事；规则书那个「抽 2 张」= 常规 1 + 加时 1）
            // 🆕 2026-09-26：常规张数从 `ctx.Vars.drawCardsPerTurn` 读（两模式都是 1 —— 留着是
            //    为了「按模式会变的量都从一处读」这条纪律，**不是**暗示遭遇模式要改它）。
            int nDraw = ctx.Vars.drawCardsPerTurn + (ctx.IsOvertime ? DeckRules.OvertimeExtraDraw : 0);
            ctx.Log($"回合 {ctx.Turn} 开始：{p.Name} 能量 {p.Energy}，抽 {nDraw} 张");
            for (int i = 0; i < nDraw; i++) Draw(ctx, ctx.Active);
            // ============================================================
            // 🔴 **`turn_start` 相位（原版 `OnTurnStart`）—— 必须在【抽牌之后】**
            //    原版次序（`BattleManager._NextTurn_d__395__MoveNext.c:360/362/371/385`）：
            //      能量 → **`BroadcastTurnSetup`** → **`TurnStartCardDraw`** → **`BroadcastTurnStart`** → `BroadcastTurnStarted`
            //    ⇒ 抽牌**之前**跑 `CardScript.OnTurnSetup`（清理/到期），抽牌**之后**跑 `CardScript.OnTurnStart`（唤醒 + 触发）。
            //    ⚠️ **2026-10-17 就地订正（铁律 5）**：这一段原来放在**抽牌之前**，依据是 `rule_core.gd` ——
            //       🔴 **那是我们自己的 Godot 复刻、不是原版**（见 `CLAUDE.md` 铁律 2 的 2026-09-18 更正）。
            //       放错的后果是**行为级、且静默的**：所有「回合开始时」的效果**看到的手牌比原版少一张**，
            //       而原版落在 `OnTurnStart` 的那批触发在时间轴上被**整体前移**。
            //    🔴 **同批一起搬过来的还有三件**（原版都属 `OnTurnStart`）：
            //       · **解疲劳**（原版 `CardScript.OnTurnStart` 里的 `ActivateMinion`）
            //       · **Stealth 到期**（`CardScript__OnTurnStart.c:112-118` —— 那一节的注释**本来就写着**它属于这个相位）
            //       · **天赋**（`CardScript__OnTurnStart.c:160`）
            //    内容是：当前行动方**手牌里的陷阱卡** + 他登记的**常驻效果**。
            ResolveAtTurn(ctx, "turn_start");
            if (ctx.IsOver) return;      // 触发段能打死督军（`your troops take 1 damage` 那类）

            // ---- 🆕 A7：「一回合到期」那一族 —— **Stealth 在拥有者回合开始时失效** ----
            //
            // **两层独立证据**（2026-09-14 核过）：
            //   · 规则书中文版 `:211`：「**潜行（Stealth）** | **一回合内或本单位攻击前**，
            //     不能被任何方式选中」（英文原版同）
            //   · 反编译 `CardScript__OnTurnStart.c:112-118`：`HasCurrentTrait(0x46)`（`0x46 = 70 =
            //     DefinedTrait.stealth`）**且「这一回合属于这张卡的拥有者」** ⇒
            //     `RemoveTraitAndEffects(0x46)` + `BroadcastUnitLoseStealth`
            //     （所以扫的是**当前行动方的**单位，不是两边都扫）
            //
            // 🔴 **不补这一条的后果是真实的行为偏差**：我们原来只有「**攻击后**失去」
            //    （`DeclareAttack` 里那一段）⇒ **不攻击的潜行单位会永久隐身、永久不可被选中**。
            // ⚠️ **位置在光环重算之前**：万一有光环给 `Stealth`，重算会把它重新挂上（那是要的）；
            //    反过来的话，光环刚挂上的 Stealth 会被这一段立刻摘掉。
            {
                int lostStealth = 0;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var su = p.Board[s];
                    if (su == null || !su.Has(KeywordTable.Stealth)) continue;
                    su.RemoveAll(KeywordTable.Stealth);     // 原版是 `RemoveTraitAndEffects`：**整条**摘
                    // 和「攻击后现身」发同一种事件 —— 卡面写 `When a friendly unit loses Stealth, …`
                    // 的那几张**必须**收到它，否则那半句在「到期」这条路上就是死的。
                    BroadcastKeywordEvent(ctx, WhenEventKind.LosesStealth, su);
                    lostStealth++;
                }
                if (lostStealth > 0)
                    ctx.Log($"（{lostStealth} 个单位的 Stealth 到一回合、失效 —— 规则书 :211）");
            }

            // ---- 🆕 A7：**光环重算**（回合开始）----
            // 为什么必须在这一刻做：`during your turn` 那一族（`Fyrri Askar` 的
            // `Friendly units with Pack have Invulnerable **during your turn**`）的**亮/灭跟着回合走**
            // —— 换了边就得把上一方的那份收掉、给这一方挂上。
            // ⚠️ 放在「限时增益到期」**之后**：那一段会 `RevertBuffs` 改属性，
            //    光环要基于**改完之后**的棋盘重算（顺序反了会把到期的那份又加回去）。
            Auras.Recompose(ctx);
            // 己方单位解疲劳（对方的不动）
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = p.Board[s];
                if (u != null) u.RefreshForNewTurn();
            }

            // ---- 天赋（Talent）：**回合开始时**往手牌塞一张同名战术卡（规则书 `:218`）----
            // 🔴 **2026-10-17 就地订正（铁律 5）**：原写「放在**抽牌之后**：回合开始段的先后（能量 → 解疲劳 →
            //    到期 → 触发段 → 抽牌 → 天赋）· 规则书**没写死**，**这个次序是我们挑的**」—— **两半都不对**：
            //    ① **位置不是我们挑的** —— 原版 `CardScript__OnTurnStart.c:160` 里 `talent` 就在这个钩子里，
            //       而 `OnTurnStart` **在抽牌之后**（`_NextTurn:362` 抽牌 / `:371` `BroadcastTurnStart`）⇒ **照原版**；
            //    ② 那句「能量 → 解疲劳 → 到期 → 触发段 → 抽牌 → 天赋」**已作废** —— 现行次序见本函数里
            //       `turn_setup` / `turn_start` 两段注释。
            SpawnTalents(ctx, ctx.Active);
        }

        /// <summary>回合结束：能量作废 → 移交。返回 <see cref="CheckWinner"/> 的结果。</summary>
        public static int EndTurn(BattleContext ctx)
        {
            if (ctx.IsOver) return ctx.Winner;

            var p = ctx.ActivePlayer;

            // 「本回合」的限时增益在**这一回合结束时**撤（`rule_core.gd:3018`）——
            // 两边场上都要扫（`give -1 attack to an enemy troop this turn` 也可能打在对方身上）
            int reverted = 0;
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u != null) reverted += u.RevertBuffs(true, ctx.Active);
                }
            if (reverted > 0) ctx.Log($"（{reverted} 条「本回合」增益到期）");

            // ---- 🆕 2026-10-18（`A886`）**手牌效果的到期清扫** ----
            // 原版 `PlayerHand.UpdateCardEffects(bool endOfTurn)`：`BattleManager__ResolveEndTurn.c:678`
            // 与 `:680` 对**两方手牌**各调一次、传 `endOfTurn = 1`（查实过，不是我们挑的）。
            // 卡面 `this turn` 挂上去的手牌效果（原版 `CardEffect.untilEndOfTurn // +0x32`）就在这一趟到期；
            // ⚠️ 卡面没写时长的（今天卡池的全部六张）**永远不过期** —— `Beast Snagga Nob` 那族靠它攒份数。
            ExpireHandBuffs(ctx);

            // ---- 🆕 2026-09-16 归还「本回合抢来的单位」（`takecontrol`）----------------
            // 卡面只有 `GSC_Telephatic_Domination`（`Take control of an enemy troop this turn …`）。
            // 「还」= 把它从我的 `Board[]` 挪回原主的 `Board[]`（归属就是数组，见
            // `BattleContext.TempControl`）。**位置**：原版那条 `GetNextSlotWithoutDisplacing`
            // （人少的一侧的最外一格、平手走右）—— 见下面 2026-10-01 那条注释。
            // ⚠️ 三条都是**我们挑的**（原版那个协程没导出，见 `TempControl` 的注释）：
            //    ① 排在「限时增益到期」**之后**；② 还回去之后**置 `Exhausted`**（它这回合替对面动过）；
            //    ③ 原主那边**没空格**时**不还**（留在抢它的人那儿）并如实打日志 ——
            //      悄悄把它销毁/塞进弃牌堆都更糟。
            if (ctx.TempControls.Count > 0)
            {
                int back = 0;
                foreach (var tc in ctx.TempControls)
                {
                    var u = tc.Unit;
                    if (u == null || !u.IsAlive) continue;          // 死了的不用还（尸体不在棋盘上）
                    int nowP, nowSlot;
                    if (!FindSlot(ctx, u, out nowP, out nowSlot)) continue;
                    if (nowP == tc.Owner) continue;                 // 已经在对面的棋盘上（不该发生）

                    int to = -1;
                    // 🔴 2026-10-01：还回去的落点与抢人那边的落点**同一条口径** = 原版
                    //    `GetNextSlotWithoutDisplacing`（人少的一侧的最外一格、平手走右）——
                    //    下面抢人的那处（`EffectResolver` 的 `takecontrol`）原来用的就是它，
                    //    这里原来写的是「原来那格还空着就还原位」（**我们挑的**，见上面 ⚠️）。
                    //    ⚠️ 棋盘改成连续模型之后「原来那格还空着」几乎必然为真（洞不存在了），
                    //    所以这两条路现在**结果一致**；仍然统一到原版那一条，免得留两份判据。
                    to = BoardSlots.NextWithoutDisplacing(ctx.Players[tc.Owner]);
                    if (to < 0)
                    {
                        ctx.Log($"⚠️ {ctx.Players[tc.Owner].Name} 的部署位也满了 —— **{u.Name} 还不回去**，"
                              + "暂时留在抢它的人那儿（本版没做「放不下怎么办」）");
                        continue;
                    }

                    // 从「抢它的人」那边摘掉 = 原版 `RemoveMinion` ⇒ **要补位**
                    BoardSlots.RemoveAt(ctx.Players[nowP], nowSlot);
                    ctx.Players[tc.Owner].Board[to] = u;    // `to` 就是 `NextWithoutDisplacing` 给的最外一格
                    u.Exhausted = true;                             // 见上面 ②
                    ctx.Log($"{u.Name} 归还给 {ctx.Players[tc.Owner].Name}（{to} 号格）——「本回合控制」到期");
                    back++;
                }
                ctx.TempControls.Clear();
                if (back > 0) Auras.Recompose(ctx);                  // 棋盘动了 ⇒ 光环重算
            }

            // ---- 临时卡清扫（规则书 :183 + :229）------------------------------------
            // ⚠️ **位置是挑过的、顺序有意义**：
            //    ① 在「本回合限时增益到期」**之后** —— `rule_core.gd:2010` 那一串的开头对得上；
            //    ② 在 `ResolveAtTurn("turn_end")` **之前** —— 规则书 :229 说的是
            //       「回合结束**未打出即消失**」，而 `turn_end` 那张表是「回合结束时**触发**的效果」。
            //       把清扫放前面 = **手牌里等你回合结束的那张临时卡，不会看到那一下触发**
            //       （它已经没了）。⚠️ 规则书**没写**这两者谁先 ⇒ **这是我们挑的**，如实标着。
            //    ③ 在 `ctx.Active` 换边**之前** —— 扫的是**这一方自己的**手牌（见 `SweepEphemeral`）。
            SweepEphemeral(ctx, ctx.Active);

            // 👆 和「本回合限时增益到期」一样，两边场上都要扫 —— 但**摧毁只发生在自己回合结束时**，
            //    所以下面那一趟只扫**当前行动方**（规则书 `:203`「控制者回合结束时被摧毁」）。
            DestroyRemnants(ctx, ctx.Active);

            // ---- 回合**结束**触发段（规则书回合结构**第 14 步**"End of turn abilities"）----
            // 放在「本回合限时增益到期」**之后**、**能量清零与再生之前** ——
            // 和 `rule_core.gd:2010-2024`（`_expire_temp_buffs(true)` → `_at_turn_effects("end")`
            // → `energy = 0` → Regeneration）的相对次序一致。
            ResolveAtTurn(ctx, "turn_end");
            if (ctx.IsOver) return ctx.Winner;

            // ---- 再生 X：**每回合结束时**治疗 X（规则书 :201「每回合结束时治疗 X」）----
            //      我们自己的 `rule_core.gd:2025`（旁证、非权威） 明写 `at the end of EACH turn → 双方单位`，
            //      而且是在 `energy = 0` **之前**结算的 —— 顺序照抄。
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u == null || !u.IsAlive || !u.Has("regeneration")) continue;
                    int heal = u.KwValue("regeneration");
                    int before = u.Health;
                    u.Health = System.Math.Min(u.Health + heal, u.MaxHealth);
                    if (u.Health != before)
                    {
                        ctx.Log($"{u.Name} 的 Regeneration {heal}：{before} → {u.Health}"
                              + $"（上限 {u.MaxHealth}）");
                        EmitUnit(ctx, EvtKind.Hit, u, -(u.Health - before));   // 负数 = 治疗，表现层据此走绿字
                    }
                }

            // ---- 🆕 2026-10-18（`W5` · `K3` 账 · `+0x120` 第 8 个写点）：**带 `oath` 的单位重挑攻击型** ----
            //   原版 `CardScript.OnTurnEnd`（`CardScript__OnTurnEnd.c:214-218`）：
            //   ```
            //   cVar11 = HasCurrentTrait(param_1, 0x4fb /*oath 1275*/);
            //   if (cVar11 != '\0') { iVar12 = (melee < ranged) + 1;
            //                         if (*(int *)(param_1 + 0x120) != iVar12) *(int *)(param_1 + 0x120) = iVar12; }
            //   ```
            //   ⇒ **只对带 `oath` 的单位**（不是全场），算式 = `ChooseAttackTypeAutomatically`。
            //   ⚠️ 时机：原版这一支在 `BattleManager.ResolveEndTurn` 的逐卡循环里（回合翻面**之前**）
            //     ⇒ 放在这里（`ctx.Active` 换边之前）。⚠️ **我们这边没有逐卡的 `OnTurnEnd`**
            //     （那边是「每张牌一个 `CardScript`」），所以用一趟双方棋盘的循环代替 —— 判据是
            //     「谁带 `oath`」，与逐卡等价。
            for (int pl = 0; pl < 2; pl++)
            {
                var b = ctx.Players[pl].Board;
                for (int s = 0; s < b.Length; s++)
                    if (b[s] != null && b[s].Has(KeywordTable.Oath))
                        b[s].CurrentAttackType = ChooseAttackTypeAutomatically(b[s]);
            }

            // 未用完的能量：**遭遇模式保存 1 点**（`manaAccumulation`），经典不保存（它是 0）。
            // 🔴 **2026-09-26 修（原来从来没生效过）**：这一段原来只有 `p.Energy = 0;`，
            //   而 `BeginTurn` 判结转用的是 `p.Energy > 0` —— 进到这里时 `Energy` 已经被清成 0，
            //   ⇒ `ManaCarry` **恒为 0**，`BeginTurn` 里那个 `+ p.ManaCarry` 是死代码。
            //   ⚠️ 那一行自己的注释还写着「遭遇模式才保存 1 点」，**注释写了、代码没做**。
            //   ⇒ 改成**在回合结束这里结算**（原版语义就是「回合结束未用完的能量保存 1 点」）。
            // 📌 **实测影响**（用户 2026-09-26 给的数）：遭遇后手的 `MaxEnergy` 曲线
            //   修前 = **4 → 6 → 8 → 10**（进加时那回合是 10）；修后 = **4 → 7 → 9 → 11** ✓
            //   （正好是用户说的 11）。⚠️ 经典 `manaAccumulation = 0` ⇒ 恒 0，**行为一字不变**。
            p.ManaCarry = (ctx.Vars.manaAccumulation > 0 && p.Energy > 0) ? ctx.Vars.manaAccumulation : 0;
            p.Energy = 0;                       // 未用能量不留在手里（要留的那 1 点已记进 `ManaCarry`）
            ctx.Active = 1 - ctx.Active;
            ctx.Log($"回合 {ctx.Turn} 结束，轮到 {ctx.ActivePlayer.Name}");
            return CheckWinner(ctx);
        }

        /// <summary>
        /// **临时卡清扫段**（规则书 `:183` + `:229`）—— 回合结束时，**手牌里的临时卡从游戏中移除**。
        ///
        /// 规则书原文（`:229`）：
        /// 「天赋、伴生生成的部队、带潮涌的复制（在手牌时）均临时，回合结束未打出即消失
        ///  —— **从游戏中移除（非弃置）**。督军天赋每回合循环，故可反复使用。」
        ///
        /// 三条判据，每条都有出处：
        ///   ① **扫谁的手牌** = **当前行动方**（这个方法在 `EndTurn` 里、`ctx.Active` 换边**之前**调）——
        ///      临时卡是**持有者自己的**回合结束才消失，对方手里的要等他自己回合结束。
        ///      这条和 `:91`「督军天赋每回合开始加入手牌、回合结束移除」对得上（那就是循环）。
        ///   ② **只扫手牌** —— 场上/牌库/弃牌堆里的不管（`未打出` 这个限定词只对手牌有意义）。
        ///   ③ **去向是 `ctx.Removed`（「移出游戏」），不是 `Discard`** —— 规则书明写「非弃置」。
        ///
        /// ⚠️ **原版的这一段没查到**（`BattleManager.cs` 与 1800 个反编译 `.c` 里搜 `ephemeral`
        ///    只有三处**表现层**的命中，见 `资料/临时卡Ephemeral_设计与实现计划.md` §2.3）
        ///    ⇒ **按规则书做**，不是照抄原版。
        ///
        /// ⚠️ **先快照再改**：移除会改 `Hand` 列表，边遍历边改是未定义行为
        ///    （和 `ResolveDeploy` / `BroadcastWhen` 同一条教训）。
        /// </summary>
        static void SweepEphemeral(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];

            // ---- ① 快照：这一方手牌里**是临时卡**的那些 ----
            // 🔴 **判据是 `TryTakeOneEphemeral`，不是 `IsEphemeral`**（2026-09-13 自检抓出来的真 bug）：
            //    `CardDef` 是共享不可变模板，「标记」只能按卡记份数 ——
            //    而 `IsEphemeral(cardDef)` 对**同名的每一份**都返回 true。
            //    手里有两张同名卡、只标了其中一张时，`if (IsEphemeral(c))` 会把**两张都移走**
            //    （原件被当成复制一起消失）。`TryTakeOneEphemeral` 会**吃掉一份标记**，
            //    所以同名的下一份就判 false 了 —— 只走一张。
            //    ⚠️ 快照时**必须先吃掉标记**（它就是靠销标记来「认领」的），
            //       否则快照会对同一份标记认领多次。
            // 🔴 **2026-09-18 第 7 行第 3 步**：标记搬到了 `CardInstance.EphemeralMarked` 上，
            //    「认领哪一份」不再靠遍历顺序猜 —— `TryTakeOneEphemeral(这一份)` 问的**就是它**。
            //    （改之前按卡模板记份数：同名两张里只标了一张时，快照按顺序认领，
            //      虽然 `Remove(c)` 销的是被认领那份的标记，但**配到的可能是另一张**。）
            List<CardInstance> doomed = null;
            foreach (var c in ps.Hand)
            {
                if (!ctx.TryTakeOneEphemeral(c)) continue;
                if (doomed == null) doomed = new List<CardInstance>();
                doomed.Add(c);
            }
            if (doomed == null) return;

            // ---- ② 逐个移出 ----
            bool first = true;
            foreach (var c in doomed)
            {
                if (!ps.Hand.Remove(c)) continue;         // 按**实例**去一份 —— 同名两张也分得开
                if (first)
                {
                    ctx.Log($"—— 回合 {ctx.Turn} 结束：{ps.Name} 手牌里的临时卡从游戏中移除（规则书 :229「非弃置」）——");
                    first = false;
                }
                ctx.Removed.Add(new RemovedCard
                {
                    Instance = c,
                    Owner = p,
                    Turn = ctx.Turn,
                    Reason = "ephemeral",
                });
                // ⚠️ 标记**已经在 ① 的 `TryTakeOneEphemeral` 里销掉了** —— 别在这儿再销一次
                //    （再销一次会把「同名的另一份」的标记也吃掉，那一份就永远不会被移除了）。

                ctx.Log($"    · {c.Card.Name} —— 移出游戏（手牌 {ps.Hand.Count} 张）");
                // 表现层要**知道该给哪张卡播消失动画**，但它已经不在手牌里了 ——
                // 所以额外发一条事件把卡名带上（`EvtKind.Return` 那条注释里讨论过
                // 「离开手牌但不是阵亡」这一类，这里是最接近的先例）。
                ctx.Emit(EvtKind.Return, p, -1, c.Card.Name, effect: "ephemeral");
            }
        }

        /// <summary>
        /// 抽 1 张。牌库空 → 疲劳 +1 并让督军挨这么多伤害。（rule_core._draw）
        /// ⚠️ 疲劳能打死督军，所以这里必须复查胜负。
        /// </summary>
        public static void Draw(BattleContext ctx, int p)
        {
            var inst = MoveDeckTopToHand(ctx, p);
            if (inst == null) return;                   // 牌库空 ⇒ 已经走过疲劳那一支
            var card = inst.Card;
            // 🆕 `When you draw a card, …`（2026-09-13 第三十三轮）。
            // ⚠️ 发在**入牌库 → 进手牌之后**：监听方看到的是「抽到了」这个事实。
            // ⚠️ `card` 传进去 —— 监听器的筛选（`a troop` / `a Stratagem`）要拿它判。
            //
            // 🔴 **2026-10-18（`S10` · 待办 `A962` ② 的收口 · 铁律 11）：外面套一层「指代槽」。**
            //    广播传下去的实参是 **`CardDef card`（卡模板）**，而监听器要「指代抽出来那张」时
            //    要的是**那一份 `CardInstance`**（`DoLowerCost` 的 `(指代上一张)` 支按份钉
            //    `HandInstanceId`）⇒ 它只能去 `ctx.DrawnThisResolve` 找，而那个槽**原来没有写点**
            //    （只有 `DoDraw` / `DoChooseCard` / `DoDrawRef` 三条，全在别的路上）。
            //    ⇒ 普通抽牌（回合开始那一抽就走这里）的监听器**永远读不到指代对象** ——
            //      `Company Master`（`DA31`）的降费**一次都不生效**（离线实测 `CostOf` 仍是印价）。
            //    种子/还原的判据、为什么必须套在广播**外面**、以及窗口放宽的连带，
            //    全写在 `EffectResolver.DrawReferentScope` 的头注释里（⛔ 别在这儿再抄一份）。
            using (DrawReferentScope.Seed(ctx, inst))
                BroadcastWhen(ctx, WhenEventKind.Draw, p, card, null);
            // 🆕 **「这一份是这回合从牌库抽到的」** 记账（2026-09-13 A2，2026-09-18 第 3 步搬到实例上）——
            // 传送（`Teleport`）判的就是它：规则书 `:219`「**当回合从牌库抽到即打出时**触发能力」。
            // 🔴 现在是**一份一个布尔**（`CardInstance.DrawnThisTurn`）：同名两张里
            //    抽到一张、打出另一张，**不会再误触发**（改之前按卡模板记份数，会）。
            //
            // 🔴 **2026-10-18（`S7` · `A962` ② · 铁律 11）：这一句【挪到广播之后】—— 原版是「广播早于记账」。**
            //    判据（现读 `d:/2/tools/decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c`，case 5）：
            //      ```
            //      BattleManager__RemoveCardFromDeck(...);                       // :282 出牌库
            //      if (IsDuringMulligan == 0) {
            //          BattleManagerSupport__BroadcastCardDrawn(...);            // :285 ← 广播
            //          … BroadcastTrapResolved / CemeteryManager.AddDrawTrapAction …
            //          CardScript__SetCardTurnDrawn(card);                       // :295 ← 记账
            //      }
            //      ```
            //    ⇒ 我们这一侧「抽到了」的记账（`DrawnThisTurn` ⇔ 原版 `SetCardTurnDrawn` 写的 `+0x310`）
            //      排在广播**前面**，与原版相反。挪到后面即可（两件事都在 `Draw` 之内，中间没有别的语句）。
            //    ⚠️ **今天【观测不到】差异（如实标着）**：全工程读 `DrawnThisTurn` 的只有
            //      `RuleCore.PlayCard` 的 `Teleport` 那一支（与 `CardPresentation` 的高亮）——
            //      而**没有任何 `draw` 监听器能在这段广播里把牌打出去**（效果动词表里没有「打出」）
            //      ⇒ 广播期间没人读它。仍然照原版做（铁律 11）。
            //      ⛔ 别把这条写成「修掉了一个可见缺陷」。
            //    📌 连带核过、**不成立**：原版那三件事发生时，牌**早已在手牌里**
            //      （`AddDrawnCardToHand` 的子协程在 case 0 就启动、case 5 之前已经
            //       `List.Add` + `PlayerHand.SetupCardInHand` —— 见 `PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c:111-120`）
            //      ⇒ 「原版是『已出牌库、还没进手牌』」那句**不成立**，我们「进了手牌才广播」与原版同形。
            inst.DrawnThisTurn = true;
        }

        /// <summary>
        /// **把牌库顶那一张挪进手牌** —— `Draw` 与 <see cref="DealOpeningHand"/> 的**共用中段**
        /// （含 `SetupCardInHand` 与 `EnforceHandLimit`）。
        /// 牌库空 ⇒ 疲劳 +1、督军挨那一下伤害、复查胜负，**返回 `null`**。
        ///
        /// ⛔ **它【不】做那两件「抽牌才有」的事**（由调用方决定）：
        /// `CardInstance.DrawnThisTurn = true` 与 `BroadcastWhen(Draw, …)`。
        /// 判据见 <see cref="DealOpeningHand"/> —— 原版起手那一趟**两件都不做**。
        /// </summary>
        static CardInstance MoveDeckTopToHand(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];

            if (ps.Deck.Count == 0)
            {
                ps.Fatigue++;
                ps.Warlord.Health -= ps.Fatigue;
                ctx.Log($"{ps.Name} 牌库抽空 —— 疲劳 {ps.Fatigue} 点伤害（督军剩 {ps.Warlord.Health}）");
                // 疲劳也算「挨了一下」—— 不带走 ApplyDamage（它不吃护盾/护甲），但要发事件，
                // 否则画面上督军莫名其妙掉血、一点反馈都没有
                ctx.Emit(EvtKind.Hit, p, BoardSpec.WarlordSlot, ps.Warlord.Name, amount: ps.Fatigue);
                CheckWinner(ctx);
                return null;
            }

            // 从牌库**末尾**抽（和 rule_core 的 pop_back 一致）——
            // 这样 `_deck([a,b,c])` 这种「构造好顺序的牌库」测试才和原实现对得上
            int last = ps.Deck.Count - 1;
            var inst = ps.Deck[last];          // 第 7 行第 2 步：牌库里是**实例**
            ps.Deck.RemoveAt(last);
            ps.Hand.Add(inst);                 // ……**挪那一份**（实例跟着牌走，不新发）
            // 🆕 **2026-10-18（`A885` ②）：进手牌就把「还在生效的手牌效果」补给它** ——
            //    原版 `PlayerHand.SetupCardInHand`（调用点之一是 `_AddDrawnCardToHand_d__37:118`，
            //    正是抽牌这一条路）。这是我们这一侧**最主要的那个入口**。
            SetupCardInHand(ctx, p, inst);
            EnforceHandLimit(ctx, p);
            return inst;
        }

        /// <summary>
        /// 🆕 **2026-10-18（`W5` · D1 §5·1「起手牌到底算不算『抽到』」）**：
        /// **起手发牌** —— 把牌库顶那一张挪进手牌，但**不做**「抽牌」那两件事：
        /// **不置 `DrawnThisTurn`**、**不发 `WhenEventKind.Draw` 广播**。
        ///
        /// 🔴 **判据（查实了，不再是「倾向」）**：原版 `BattleManager._ResolveDrawCard` 的
        ///   协程里那两件事**都包在同一个 `IsDuringMulligan` 守卫里**
        ///   （`BattleManager._ResolveDrawCard_d__432__MoveNext.c:283-295`）：
        ///   ```
        ///   cVar4 = BattleManager__IsDuringMulligan(lVar8);
        ///   if (cVar4 == '\0') {
        ///       BattleManagerSupport__BroadcastCardDrawn(lVar8, card);   // ← 抽到才广播
        ///       … BroadcastTrapResolved … CemeteryManager.AddDrawTrapAction …
        ///       CardScript__SetCardTurnDrawn(card);                      // ← 抽到才记「本回合抽的」
        ///   }
        ///   ```
        ///   ⇒ **换牌阶段（= 起手那一趟）里，`CardDrawn` 广播与「本回合抽到的」记账【两件都不发生】**。
        ///   旁证两条（互相独立）：
        ///    · `PlayerHand._AddCardsToMulligan_d__41__MoveNext` 里对
        ///      `CardDrawn / DrawCard / ResolveDraw` **零命中**（起手走的是 `MoveCardToMulliganPos` /
        ///      `SetupCardInMulligan` 那条路，与抽牌协程无关）；
        ///    · `BattleManagerSupport__BroadcastCardDrawn` 的**唯一**调用点就是上面那一处。
        ///
        /// ⚠️ **范围**：只改**普通局的两处起手发牌**（`NewBattle` 的轮流发牌 + 后手的
        ///   `secondExtraCards`）。⛔ **不动**回合开始那一抽（`BeginTurn`，`RuleCore.cs` 的
        ///   `for (nDraw)`）—— 那是真·抽牌，走 `Draw`。
        /// ⚠️ 教程局的起手走 `TutorialRules.SetupInitialHand` → `RuleCore.Draw`，**本条不动它**：
        ///   教程**根本不跑换牌阶段**（`_TutorialStartSequence`），`IsDuringMulligan` 那一刻是不是真
        ///   我们**判不出来**（原版那个协程的启动点 grep 不到）⇒ 如实挂着、不猜。
        /// ⚠️ **`DrawnThisTurn` 那一半今天【观测不到】差异**（如实标着）：`BeginTurn` 会清掉
        ///   **当前行动方**名下所有牌的 `DrawnThisTurn`（`RuleCore.cs` 那一趟三循环），
        ///   而任何一个座位能在自己回合里出牌之前，必然已经过了自己的 `BeginTurn`
        ///   ⇒ 起手牌的那个布尔**在它能被读到之前就被清掉了**。仍然照原版做（铁律 11），
        ///   但别把这条写成「修掉了一个可见缺陷」。
        /// </summary>
        static void DealOpeningHand(BattleContext ctx, int p)
        {
            MoveDeckTopToHand(ctx, p);
        }

        /// <summary>
        /// 手牌超上限 → 多出来的进弃牌堆（原版同样规则）。
        ///
        /// **只此一处**：抽牌和造牌（`create`）都调它 —— 两处各写一份，迟早出现
        /// 「抽牌会爆牌、造牌不会」这种对不上的行为。
        /// 从**末尾**丢（= 最后到手的那张），和 `Draw` 从牌库末尾抽是对称的。
        /// </summary>
        public static void EnforceHandLimit(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];
            // 🆕 2026-09-26：上限从 `ctx.Vars` 读（经典 10 · 遭遇 8）。
            // 🔴 **2026-10-18（`W4` 整改 · 审查问题 11）· 就地把「语义一致」那句改诚实（铁律 5）**：
            //    它**丢表尾那一张**，而原版是 `_AddCardNotDrawnToHand_d__39:26-30/63-65`
            //    **先判 `Count < MaxCardsInHand` 再 `Add`**、满了就 `SendToCemetery(刚造那张)`。
            //    ⇒ **手牌没超上限时两者等价**（刚造的那张 `Add` 在末尾 = 被丢的就是它）；
            //      **手牌本来就已经超上限时我们会多丢**（原版那一张根本不会进手牌）。
            //    ⚠️ 手牌**可以**超上限：`SpawnTideCopies`（潮涌）**不调**这个方法，
            //      打完一轮潮涌手里可能就 >10 ⇒ 「多丢」这一支是真会走到的（不是理论情形）。
            //    🔴 **2026-10-18（`W5` · 审查 §9·3）就地改口（铁律 5）**：上面那半句
            //      「`SpawnTideCopies` **不调**这个方法」**已经过期** —— 潮涌那一路**补上了**
            //      （判据：原版 `ProcessTide` 也是走 `AddNewCardToHand` → `AddCardNotDrawnToHand`，
            //       与伴生同一条路；见 `SpawnTideCopies` 里那段注释）。
            //      ⚠️ **「多丢」那一支仍然存在**（别的路仍可能把手牌堆到上限之上，
            //      例如「造牌进手牌」的其它入口在**同一次结算里连着造**）⇒ 这句提醒留着。
            //    ⛔ 别把这里读成「与原版逐字一致」。
            int limit = ctx.Vars.handLimit;
            while (ps.Hand.Count > limit)
            {
                int over = ps.Hand.Count - 1;
                var dropped = ps.Hand[over];
                ps.Hand.RemoveAt(over);
                ps.Discard.Add(dropped);       // 第 7 行第 2 步：丢的是**那一份实例**（不是模板）
                ctx.Log($"{ps.Name} 手牌超上限，{dropped.Card.Name} 进弃牌堆");
            }
        }

        // ==================================================================
        //  出牌
        // ==================================================================

        /// <summary>
        /// 只判不执行 —— 表现层拖拽过程中要**实时**知道这张牌能不能打，不能等松手才发现。
        /// 和 <see cref="PlayCard"/> 共用这一份判据，不是另写一份。
        /// </summary>
        public static int CanPlayCard(BattleContext ctx, int p, int handIdx, int slot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;

            var ps = ctx.Players[p];
            if (handIdx < 0 || handIdx >= ps.Hand.Count) return RuleCodes.ErrBadHand;

            var inst = ps.Hand[handIdx];           // 第 7 行第 3 步：算费用要连**哪一份**一起给
            var card = inst.Card;      // 第 7 行第 2 步：手牌存实例，取模板走 `.Card`

            // 战术卡走**另一条判据**（不落格位、可能要选目标 —— 见 `EffectResolver.CanPlayTactic`），
            // 但**入口仍然是这一个**：表现层只问 `CanPlayCard`，免得两处各判一份、迟早不一致。
            //
            // ⚠️ 顺序照旧：**先于费用判断**。否则一张用不起的战术卡会报「能量不足」，
            //    把「本版不支持」误导成「再等等就能打」（原注释记的就是这个坑）。
            if (!card.IsUnit) return CanPlayTactic(ctx, p, handIdx, slot);

            // ⚠️ 校验顺序和 rule_core.play_card 一致：**先费用、后格位**
            //    （测试断言过「非法格不扣费」—— 顺序反了会出现「判了格位却已经扣过费」的中间态）
            if (CostOf(ctx, p, inst) > ps.Energy) return RuleCodes.ErrCost;

            // 🔴 **2026-10-01：棋盘改成「连续无洞」模型**（照原版 `MinionManager` 那两条 `List`）——
            //    落点不再是「必须空着的一格」，而是**插到哪一格**：插进中间会把后面的单位整体外移一格
            //    （`BoardSlots.Insert` ≡ 原版 `List.Insert`）。所以原来那句
            //    「`ps.Board[slot] != null` ⇒ `ErrSlot`」**已经作废**（原版没有这条路：格与格之间没有洞，
            //    拖到有人的地方就是插在它前面）。⚠️ **督军格（4）也照样能落** ——
            //    原版 `AdjustedSlot` 见到 `slot == 0` 走「挑人少的一侧、平手走右」
            //    （`MinionManager__AdjustedSlot.c:45-61`），我们照它（`BoardSlots.Resolve`）。
            //    🔴 **2026-10-01 晚更正**：这里原来写「真正还会被拒的只剩一条：**那一侧满 4 个**」——
            //    **错的**：原版 `AdjustedSlot` 见到「请求的那一侧满了」是**换到对侧最外那一格**
            //    （`:64-72` / `:104-107`），不是拒绝。现在**只剩「两侧都满」**会被拒
            //    （原版那一步靠调用方事先筛；我们返回 `ErrSlot`，失败形态不同但不可观测）。
            if (!BoardSpec.IsValid(slot)) return RuleCodes.ErrSlot;   // 越界（含 -1）
            if (!BoardSlots.HasRoomFor(ps, slot)) return RuleCodes.ErrSlot;

            return RuleCodes.OK;
        }

        /// <summary>
        /// **部署豁免**：身上带 `fast`（迅捷）/ `flank`（侧翼）/ `ferocity`（狂暴）之一的单位
        /// **部署当回合就能行动** —— 规则书 `:98`「部署当回合不能行动（**除非注明，如迅捷/侧翼/狂暴**）」、
        /// `:187`「侧翼：打出当回合可攻击任意敌方部队」；原版 `rule_core.gd:2248` 也把这三个写在同一句里
        /// （`fast` / `flank` → `exhausted = false`）。
        ///
        /// 🔴 **2026-10-17（F6 #2）：这条判据原来散着写，现在部署那一步要用它重算一次。**
        ///   起因：`UnitState` 的 `Exhausted` 是**构造时**按**卡模板**的关键词算的一次快照
        ///   （`Core/UnitState.cs:257`；F6 当时在 `:250`），而**手牌加成**（`ApplyHandBuffs`）与**光环**（`Auras.Recompose`）
        ///   都是**在那之后**才可能把三个词挂上来的 ⇒ 这两路给的侧翼**谁都判不到**
        ///   （`Has("flank")` 为真、单位却仍然疲劳 —— 静默错；**修之前** `RuleEngineTest` 的
        ///   「侧翼 ⇒ 部署当回合不疲劳」那一条实测红）。
        ///   现由 <see cref="PlayCard"/>、<see cref="DeployFree"/> 与 `EffectResolver.DoReanimate`
        ///   **三个「新单位落地」入口**在「手牌加成 + 光环重算」**之后**各调本方法重算一次
        ///   （F6 修了 `PlayCard`、F8 补了 `DeployFree`、F9 补了 `DoReanimate` —— 效果免费部署 / 召唤 / 再造
        ///   走 `DeployFree`，残骸翻回来走 `DoReanimate`）。
        ///   ✅ **三处齐了**，而且 F9 已**全树再 grep 一遍确认没有第四个入口**（「有新单位落地」的引擎写点
        ///   只有这 3 处 + 督军那处**恒不疲劳**；清单 → `资料/普查产出_1017/F9_第三入口.md` §③）。
        ///   🔴 **2026-10-17 订正（铁律 5）**：本段原来写「第三个同形入口 `EffectResolver.cs:6362`（`DoReanimate`）
        ///   **还没补** … 已立账」—— **已过期**（F9 当天补上了，落点 `EffectResolver.cs:6378`）。
        ///
        /// ⚠️ **重算只能对着「刚部署的那一个单位」做，⛔ 不能遍历全场** —— 本回合**已经行动过**的单位
        ///   也是 `Exhausted = true`，一律按本方法解掉就会把它**放活**（一回合动两次，同样是静默错）。
        ///   ⛔ 同理，别把这一段写进 `UnitState.AddKeyword`：那里**分不清**「刚落地」与「已行动」。
        ///   （判别式已配两条：`RuleEngineTest` 的「`PlayCard` 那条 ④′」与「免费部署」那条。）
        ///
        /// ⚠️ **2026-10-17 更正（铁律 5）**：这一段原来写着「**还欠两处没并进这个判据**：
        ///   `Core/UnitState.cs:250` 与 `Core/EffectResolver.cs:3988`」—— **两句都已不成立**：
        ///   · `Core/UnitState.cs:250`（现 `:257`）**已并** —— 构造那一行现在就是
        ///     `Exhausted = !RuleCore.HasDeployExemption(this);`（F7 收口；逐关键词逐位等价已实测）；
        ///   · `Core/EffectResolver.cs:3988`（现 `:3997-4002`）**不能并**（**F7 判决 + 错版实测**）
        ///     —— 它**不是同一条判据**：那两句读的是**卡面文本预判「马上要给的豁免」**
        ///     （`takecontrol` 的尾句 `give it Fast`），而 `ResolveOps` 是**按序**跑 ⇒ 走到那一刻
        ///     尾句**还没结算**、`t.Has("fast")` 仍是 false ⇒ 照字面并成
        ///     `if (HasDeployExemption(t)) t.Exhausted = false;` 就**永远不成立** ⇒ 抢过来的单位
        ///     仍疲劳（`give it Fast` 成了空话）—— 那是**把已发布的卡打坏**，不是收口。
        ///     判据全文与错版实测（离屏 `D:/tmp/wf_f7_probe_bad` 实测 `Exhausted=True`）→
        ///     `资料/普查产出_1017/F7_判据收口.md` §②；受影响的那条断言在 `RuleEngineTest` ⑨
        ///     （「★ `and give it Fast` ⇒ 现在就能动」，2026-10-17 收工时在 `:11575` ——
        ///     ⚠️ **行号会漂**，按那句断言文案找）。
        ///   ⇒ **收口结果 = 判据只此一处**（本方法的方法体）。§⑥ 有 grep 证据。
        /// </summary>
        public static bool HasDeployExemption(UnitState u)
        {
            if (u == null) return false;
            return u.Has("fast") || u.Has("flank") || u.Has(KeywordTable.Ferocity);
        }

        /// <summary>
        /// 打出第 handIdx 张手牌到 slot 格。（rule_core.play_card）
        ///
        /// ⚠️ **战术卡走同一个入口、不同的分支**：它不落格位，`slot` 的含义变成「效果打谁」，
        ///    交给 `EffectResolver.PlayTactic`（扣费 → 结算 → 弃牌堆）。表现层不用分两条路调。
        /// </summary>
        public static int PlayCard(BattleContext ctx, int p, int handIdx, int slot)
        {
            // 单位卡的判据在下面；战术卡先分流（判据共用 `CanPlayTactic`，不在这儿重写一份）
            if (p >= 0 && p < 2 && handIdx >= 0 && handIdx < ctx.Players[p].Hand.Count
                && !ctx.Players[p].Hand[handIdx].Card.IsUnit)
                return PlayTactic(ctx, p, handIdx, slot);

            int code = CanPlayCard(ctx, p, handIdx, slot);
            if (code != RuleCodes.OK) return code;

            var ps = ctx.Players[p];
            // 🔴 第 7 行第 2 步：`inst` = **手牌里的那一份**（下面要原样搬上场，不是新造一张）
            var inst = ps.Hand[handIdx];
            var card = inst.Card;

            int costPaid = CostOf(ctx, p, inst);
            ps.Energy -= costPaid;
            // `next …` 那族费用修正**用完即销** —— 必须在**付费之后**调（见 `ConsumeOnceCostMods`）
            ConsumeOnceCostMods(ctx, p, inst);
            ps.Hand.RemoveAt(handIdx);

            // 「打出了这张牌」——单位卡紧接着还会发一条 `Deploy`，**日志那边会把连着的那条合并掉**
            // （`BattleContext.AppendLog`）。这条也是表现层「出牌那一下」的锚点：
            // 以前只有 `Deploy`，**战术卡压根没有事件**。
            ctx.Emit(EvtKind.Play, p, slot, card.Name);

            // 部署当回合不可行动 —— UnitState 构造出来就是 Exhausted = true
            // 🔴 第 7 行第 2 步：**手牌那一份原样上场**（`inst`），不再 `NewInstance` ——
            //    这一行就是「手牌 → 场上会不会丢实例」的那一跳。丢了的话单位死了回不到原来那一份。
            var unit = new UnitState(inst, false);
            unit.DeployedTurn = ctx.Turn;   // 🆕 誓约能力的「本回合部署」判据（`UnitState.DeployedTurn`）
            // 🔴 **2026-10-01：落格走「插入」**（原版 `MinionManager.InsertMinion`）——
            //    它真正落在哪一格由 `BoardSlots.Insert` 算（可能**不是**玩家拖到的那一格：
            //    插进中间会把后面的单位整体外移一格），所以**下面所有事件都用回填后的 `slot`**
            //    （`Deploy` 事件、日志、`FireTriggerAt` 的格位…），表现层才摆得对人。
            //    ⚠️ `HasRoomFor` 已经在 `CanPlayCard` 里判过 ⇒ 这里 `Insert` 不会失败。
            slot = BoardSlots.Insert(ps, unit, slot);
            ctx.TroopsPlayed[p]++;   // 🆕 2026-09-23 战果：本局打出的部队卡张数（督军不走这条路）
            // 🆕 2026-10-17（B19）：**记一笔「本局打出过的牌」**（候选域 `played` 的唯一来源）。
            //    位置 = 「校验过了、费用付掉了、手牌已经拿走、也落位了」之后 ⇒
            //    **打不出去的牌一张都不会记上**（前面任一关没过就 return 了）。
            //    ⚠️ **战术卡不在这里写** —— 它们在函数开头就分流进 `PlayTactic` 了，
            //    走到这一行的只可能是单位卡（两处各写一笔、不会重记）。
            ctx.NotePlayed(p, inst, false);
            // 🆕 2026-09-16 **手牌加成兑现**（`TL53 Infinite Biomorphologies` 的「给手牌里的部队」）——
            //    必须排在下面 `Auras.Recompose` **之前**：加成可能带关键词（`Armour 1` / `Flank`），
            //    而光环重算只认**当前**的场上状态，先重算再加就会漏算这一份。
            //    🔴 2026-10-18（`A885`）：效果就挂在**这一份牌自己**身上（`inst.HandBuffOps`）——
            //    兑现的是**打出的那一份**，同名其它份各挂各的（对局级的那张表已删）。
            //    `A886` 的次数核销（原版 `CardPlayedWithEffects`）也在这个方法里。
            ApplyHandBuffs(ctx, p, inst, unit);
            // 🆕 光环重算（A7）：棋盘一变就得重算 —— 新来的这个**自己可能就是光环来源**，
            //    也可能**落进了别人的光环范围**。放在这里（不是函数末尾）是为了让后面那几步
            //    （`give it Flank` 之类自指触发、`Rally` 结算）**看得见光环已经生效**。
            Auras.Recompose(ctx);

            // 🔴 **2026-10-17（F6 #2）：部署豁免要在这儿重算一次。**
            //    上面那句 `new UnitState(inst, false)` 里的 `Exhausted` 是**构造时**按**卡模板**的
            //    关键词算的快照（`Core/UnitState.cs:250`），而 `ApplyHandBuffs`（手牌加成）与
            //    `Auras.Recompose`（光环）都是**在那之后**才可能把 `fast`/`flank`/`ferocity` 挂上来的
            //    ⇒ 不重算的话，这两路给的侧翼/迅捷**静默失效**（关键词给了、单位却仍疲劳、动不了）。
            //    ⚠️ **只重算刚部署的这一个 `unit`** —— ⛔ 别遍历全场：本回合**已经行动过**的单位也是
            //       `Exhausted = true`，一律解掉会把它们放活（静默错）。判据共用 `HasDeployExemption`。
            unit.Exhausted = !HasDeployExemption(unit);

            // ---- 🆕 2026-10-18（`W5` · `K3` 账）：**部署收尾按【当前】数值重挑攻击型** ----
            //   `+0x120` 的写点里，原版有**两个**都落在这一刻：
            //     · `CardScript__CardSetup.c:263-267`（卡初始化那一刻，`(近战<远程)+1`）
            //       —— 我们的「初始化」= `new UnitState(...)` 那一刻，那时**手牌加成与光环都还没落**
            //         （构造器里的快照只用了卡面数值）⇒ 在这里补一次才是「当时的数值」；
            //     · `CardScript__UpdateAttackText.c:21-29`（数值一变就重挑）—— 上面那两句
            //       `ApplyHandBuffs` / `Auras.Recompose` 正是我们这边「数值变了」的时刻。
            //   ⚠️ **只重挑刚部署的这一个** —— 全场重挑留给 `RecomputeCurrentAttackTypes`
            //      （那几处挂在 `Auras.Recompose` 后面，见那些调用点）。
            RecomputeCurrentAttackType(ctx, p, slot);
            //   · `CardScript__ActivateMinion.c:70-72`（召唤病 + `ferocity`/`oath` ⇒ 写 `4`）
            //     —— 原版 `ActivateMinion` 排在 `CardSetup` **之后**（它会被反复调），
            //     所以这一句排在重挑**之后**。⛔ 别把两条并成一条（一个是 `(近战<远程)+1`、
            //     一个是**无条件 4**，合并就丢了一条）。
            ForceAttackTypeOnDeploy(ctx, p, slot);

            // ---- 🆕 伏击（`Ambush`）：**面朝下打出**（规则书 `:166`）----
            // 之后两条出口各有一处判据：`ApplyDamage`（挨到伤害 → 翻开、无效果）与
            // `RevealAmbush`（撑到自己下个回合开始 → 翻开并触发）。
            if (card.Has(KeywordTable.Ambush) && card.TriggerOps(KeywordTable.Ambush) != null)
            {
                unit.FaceDown = true;
                ctx.Log($"{unit.Name} **面朝下**落在 {slot} 号格 —— "
                      + "在它翻开前挨到伤害就白搭，撑到你下个回合就触发伏击效果");
            }

            ctx.Log($"{ps.Name} 部署 {unit.Name}（{costPaid} 费，{unit.Attack}/{unit.Health}）"
                  + $"到槽 {slot}，能量剩 {ps.Energy}");
            ctx.Emit(EvtKind.Deploy, p, slot, unit.Name);

            // 🆕 `When you play a troop, …`（2026-09-13 第三十三轮）。
            // ⚠️ **排在 `Deploy` 广播之前**：「打出」在「落到格位」之前是合乎直觉的顺序，
            //    而且这条是**打出者**的监听（`When **you** play …` ⇒ `OwnerIs = RelFriendly`）。
            // ⚠️ 单位**已经放进棋盘**了才广播 —— 监听器的效果要能看见刚打出的这张
            //    （`give it Flank` 那种自指）。
            BroadcastWhen(ctx, WhenEventKind.Play, p, card, unit);

            // **事件层广播**（`When you deploy a Vehicle, …`）—— 第三十二轮。
            // ⚠️ 排在 `ResolveDeploy` **之后**：那条是「常驻效果盯着某类牌」，
            //    这条是「场上的牌盯着某类牌」——两者是**同一个事件**的两种监听者
            //    （原版都是 `OtherUnitSummoned=190` 那一支，见 `BroadcastUnitSummoned`）。
            //    先老后新，纯粹是「不改变既有顺序」的保守选择。
            BroadcastWhen(ctx, WhenEventKind.Deploy, p, unit.Card, unit);

            // **部署时触发**（`For the rest of this battle, give Shield to all Drones you deploy`）
            // —— 原版 `OtherUnitSummoned=190`，见 `ResolveDeploy` 的注释。
            // ⚠️ 排在 `Rally` **之前**：这样 Rally 结算时看得见刚给出的关键词。
            //    这一条是**我们挑的**（原版这一段的先后无据可查，已如实标在那边）。
            ResolveDeploy(ctx, p, unit);

            // ---- 🆕 潮涌（`Tide X`）：**从手牌打出时**，本回合可以再打出 X 张复制 ----
            // 规则书 `:220`「从手牌打出时：本回合可打出 X 张额外复制；**费用与首张相同**」；
            // 英文原版 `:369`「When played from the hand: you can play X additional copies this turn.
            // Energy cost of copies is the same as the original Troop played.」
            SpawnTideCopies(ctx, p, inst, unit.KwValue(KeywordTable.Tide), costPaid);

            // ---- 🆕 伴生（`Companion X: <部队名>`）：**造一张新卡放进手牌** ----
            // 🔴 **2026-10-18（`A920` 的续）· 就地订正（铁律 5）**：这一段原来写的是
            // 规则书 `:176`「从手牌打出时，可打出至多 X 张其伴生部队」+「伴生部队来自手牌」——
            // **两句话都错**（那是粉丝实体版规则书，非官方；原版是**造一张新的进手牌**）。
            // 判据与全链见 `PlayCompanions` 的头注释。
            PlayCompanions(ctx, p, card, inst);

            // ---- 🆕 起义（`Uprising`）：**之后每部署一个部队时**，场上带它的单位各触发一次 ----
            // 规则书 `:222`「本单位**之后**部署的部队，在其部署当回合触发能力」；
            // 英文原版 `:377` 那句更直白：「**Trigger an ability each time a troop is deployed
            // after this one** on the same turn it is deployed」。
            // ⇒ 判据 = 「**不是我**」+「同方」+「带 `Uprising:` 正文」。
            // ⚠️ 「之后」这件事不需要额外记：部署是**顺序发生**的，此刻在场上而它不是它自己的，
            //    就都是「在它之前部署的」—— 反过来说，**刚落地这一个不算**（`except: unit`）。
            // ⚠️ **一次部署只响一次**（每个带词单位各一次），不是每回合重复触发。
            FireTriggerOnSide(ctx, p, KeywordTable.Uprising, unit);

            // ---- 🆕 灵魂石能力（`N [Spirit Stone]: …`）：**打出这张卡时**结算那一条 ----
            // 原版 `AbilityTrigger.UseSpiritStone = 600`；付费那一步在打出牌协程里当闸门
            // （`CardScript.CanUseSpiritStone` 在 dump 里唯一的调用点 =
            //  `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:719-731`）。
            // ✅ **2026-10-07 加固（A121）**：反编译里 **600 的发起源只有两处** ——
            //    `RawCardScript__OnCardPlayedWithTarget.c:66`「打出一张带目标的牌」时 ·
            //    `CardScript__TriggerSpiritStone.c:16-18`（77 号动作那条），
            //    **而扣石就在那个 600 分支里**（`RawCardScript__TriggerAbility.c:42-53`：
            //    `thisCard == cardPlayed` 时 `UseMana(pm, ability+0x20, 5 /*SpiritStone*/, 0, 0)`）
            //    ⇒ **正面支持「打出时」**。⚠️ **口径来源 = 用户口径（2026-09-19）＋ 反编译有据；
            //    【实况未核】**（原版已关服 + 手牌注入失败，本地核不了）。
            //    判据全文 → `资料/普查产出_1007/波7判据核查.md` §A120 / §A121。
            // 🔴 **2026-09-14 之前，这一族「没有任何一层消费」** —— 句子解析得出来、载荷也有机制，
            //    却**永远不会发生**，而且**报表看不见它们**（判据「解析得出 + 有机制 + 没有触发点」，
            //    见 `资料/单位卡desc与光环_批次划分.md` §一⑦）。全池 28 张，逐卡见
            //    `资料/查证_裸写触发点_四批.md` §丙 文末「附录 · 灵魂石卡逐张核」
            //    （⚠️ 更正：原来指 `资料/灵魂石卡_逐张核.md`，2026-10-16 已并入）。
            // ⚠️ **排在 `Rally` 之前**：理由同 `ResolveDeploy` —— Rally 结算时看得见这里刚给出的
            //    关键词。⚠️ **这个先后是我们挑的**（原版这一段的先后无据可查，如实标着）。
            // ⚠️ **不是 `useWaystone`**：那个（`BattleActionType = 76`）是「**收集**」石头
            //    （点场上那枚灵族残骸／灵魂石 —— ⚠️ **2026-10-07 更正：原来这里写「已翻面」＋
            //    「本版没做」，两句都不成立**：① 「翻面」**不是翻面、更不是卡背**
            //    （`资料/查证_useWaystone_语义.md:19-29`）；② 「收集」**2026-09-25 已做完**
            //    （`CanCollectWaystone` `:3081` / `CollectWaystone` `:3103`）），
            //    见 `资料/查证_useWaystone_语义.md`。
            ResolveSpiritAbility(ctx, p, unit);

            // Rally（集结）：「从手牌部署后触发效果」—— 规则书 :200。
            // ⚠️ 触发在**部署之后**，所以效果里 `Self` 指向的已经是场上这个单位
            FireTriggerOnBoard(ctx, unit, KeywordTable.Rally);

            // ---- 🆕 传送（`Teleport`）：**当回合从牌库抽到即打出时**触发能力 ----
            // 规则书 `:219`「当回合**从牌库抽到即打出**时触发能力」；问题机制那一节 `:234` 更明确：
            // 「抽到即激活能力的卡（常见于暗黑天使传送）：**仅当回合从牌库抽到时触发**」。
            // 判据 = **这一份**是不是本回合从牌库抽到的（`CardInstance.DrawnThisTurn`；
            // `Draw` 置位、这里就地清）。🔴 第 3 步之后它按**份**判：同名两张里
            // 抽到一张、打出另一张**不会**再误触发（改之前按卡模板记份数，会）。
            // ⚠️ 位置排在 `Rally` 之后：两者都是「这张卡落地时」的事，先后**无据可查**
            //    ⇒ **我们挑的**（Rally 是通用那条，先让它跑完）。
            if (inst.DrawnThisTurn && unit.Has(KeywordTable.Teleport))
            {
                inst.DrawnThisTurn = false;      // 用完即销（这一份的一次性触发已经兑现）
                ctx.Log($"{card.Name} 是**本回合从牌库抽到的** —— 传送（Teleport）触发");
                FireTriggerAt(ctx, unit, KeywordTable.Teleport, p, slot);
            }

            // ---- 虫群（`Swarm`）：**打出在右侧同名部队旁边时合并**（规则书 `:216`）----
            // 「打出在同名部队左侧时：合并（置于其下，攻击生命相加）」
            // ⚠️ **位置在召唤触发之后** —— 照原版 `CardScript__ResolveCardPlayed` 里的先后
            //    （同一个函数里 `ResolveUnitSummoned` 在前、合并那一段在后）。
            TrySwarmMerge(ctx, p, slot, unit);

            // ---- 🆕 典籍（`Codex`）的自动触发点：**打出之后的能量为 0**（2026-09-14 A5）----
            // 照我们自己的参考实现 `rule_core.gd:2337`（旁证、非权威）（单位部署完）与 `:2244`（虫群合并之后）——
            // 那两处在这个函数里是**同一个末尾**，所以这里只调一次。
            // ⚠️ **位置在 `CheckWinner` 之前**（那边也是先 `_check_codex` 再回到 `play_card` 的收尾）：
            //    正文可能打死对面的督军，胜负要在它之后判。见 `CheckCodex` 的完整说明。
            CheckCodex(ctx, p);
            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        /// <summary>
        /// **潮涌（`Tide X`）**：从手牌打出带它的单位时，往手牌塞 **X 张复制**
        /// （规则书 `:220` / 英文原版 `:369`）。
        ///
        /// 三条语义：
        ///   ① **X 张**（`Tide 2` → 2 张）—— `KwValue` 取的是关键词后面那个数（取不到时 = 1，见
        ///      `UnitState.KwValue` 的口径）；
        ///   ② **复制品是临时卡** —— 规则书 `:229` 把「带潮涌的复制」和天赋/伴生**并列**为临时，
        ///      回合结束未打出即**从游戏中移除**。所以每塞一张就 `MarkEphemeral` 一次
        ///      （⚠️ 必须走标记 —— 卡面本身没印 `Ephemeral`，不标就会**赖在手里不走**）；
        ///   ③ **费用与首张相同** —— 用一条 `CostMod` 把这个 id 的费用**钉回实付价**
        ///      （`Delta = 实付 − 牌面`，只在本回合有效）。首张原价时 delta = 0，不动。
        ///
        /// ⚠️ **简化（如实标着）**：「本回合最多打出 X 张」这件事**靠临时卡自己到期**表达
        ///    （回合结束全清），没有另加一个计数器 —— 玩家真去打第 X+1 张也打不出来（手里已经没有了）。
        /// 🔴 **2026-09-18 第 7 行第 2 步**：X 张复制**各发一份新实例**（`ctx.NewInstance`）——
        ///    改之前它们是「同一个 `CardDef` 对象」，现在**原件与复制品分得开了**。
        ///    ⚠️ 但下面那条「费用与首张相同」的 `CostMod` **仍然是按卡 id 登记的**（模板级）⇒
        ///       同名两张会一起改价 —— 那是第 3 步（`CostMod.Key` 改按实例）的事，**已知近似**。
        /// </summary>
        static void SpawnTideCopies(BattleContext ctx, int p, CardInstance origin, int x, int paidCost)
        {
            if (x <= 0 || origin == null || origin.Card == null) return;
            var card = origin.Card;
            var ps = ctx.Players[p];
            var copies = new List<CardInstance>(x);
            for (int i = 0; i < x; i++)
            {
                var copy = ctx.NewInstance(card);      // 复制品 = **新的一份**（不是原件那一份）
                copy.EphemeralMarked = true;           // 第 7 行第 3 步：标记打在**那一份**上
                ps.Hand.Add(copy);
                // 🆕 2026-10-18（`A885` ②）：造牌进手牌**也是进手牌** —— 补登记表上的效果
                //    （原版这一支走 `PlayerHand.AddCardNotDrawnToHand`，而它内部就叫
                //     `SetupCardInHand`，见 `PlayerHand__SetupCardInHand.c` 的四个调用点）。
                SetupCardInHand(ctx, p, copy);
                copies.Add(copy);
            }
            // 🆕 **2026-10-18（`W5` · 审查 §9·3 那条残账）**：**潮涌也要收手牌上限。**
            //
            //   判据（第一权威 = 反编译）：原版这一支造完牌同样走 `PlayerHand.AddCardNotDrawnToHand`
            //   （`BattleManager._ResolveCreateHandCard_d__512` 那一族），而它**先判 `Count < MaxCardsInHand`
            //   再 `Add`**、满了就 `SendToCemetery(刚造的那张)`，随后内部调 `SetupCardInHand`
            //   （`PlayerHand._AddCardNotDrawnToHand_d__39__MoveNext.c:26-30/63-65` ·
            //   （`PlayerHand._AddCardNotDrawnToHand_d__39__MoveNext.c:26-30/63-65` ·
            //    `PlayerHand__AddCardNotDrawnToHand.c`）—— 也就是说**造牌这条路本来就有上限**。
            //   ⇒ 我们这里原来**一句都不收**（手牌能无限超上限），而 `PlayCompanions` 那一支
            //     （同样造牌进手牌）**早就调了** `EnforceHandLimit` —— 同一条规则两处写法不一致。
            //
            //   ⚠️ **这是近似，如实标着**（与 `EnforceHandLimit` 方法头那条注释同源）：
            //     原版是**逐张**「满了就不进手牌」，我们是**先全进、再丢表尾** ——
            //     手牌**没超上限**时两者等价（丢的正是刚 `Add` 在末尾的那一批）；
            //     手牌**本来就已经超上限**时我们**多丢**（原版那几张根本不会进）。
            //     手牌真能超上限的原因之一就是**这之前没调它**（本批补上之后这一支会收窄）。
            EnforceHandLimit(ctx, p);
            // ⚠️ 被上限丢进弃牌堆的那几份**不再发 `CostMod`**（它们已经不在手牌里了）——
            //    否则会在 `ctx.CostMods` 里留一条指向弃牌堆实例的死记录。
            copies.RemoveAll(c => c == null || !ps.Hand.Contains(c));
            // ⚠️ **delta 要拿「当时的现价」比**，不是拿牌面价：首张本身可能已经被别的效果
            //    折扣过（例如 `-1`），拿牌面价算会**再折一次**（实测：3 → 实付 2 → 复制品变成 1）。
            int delta = paidCost - CostOf(ctx, p, origin);
            if (delta != 0)
                // 🔴 第 7 行第 3 步：**每张复制各钉各的**（`Key` 留 `*`）——
                //    原来是 `Key = card.Id` 一条管全部 ⇒ 手里**别的**同名卡也被改了价（D-8）。
                foreach (var copy in copies)
                    ctx.CostMods.Add(new CostMod
                    {
                        Player = p, Key = "*", HandInstanceId = copy.Id, Delta = delta,
                        ExpireTurn = ctx.Turn,          // 只在本回合（规则书：「本回合可打出」）
                    });
            ctx.Log($"潮涌 {x}：{card.Name} 的 {x} 张复制进了手牌"
                  + $"（本回合可打出、回合结束消失；费用与首张相同"
                  + (delta != 0 ? $"，本张实付 {paidCost}（牌面 {card.Cost}）" : "") + "）");
        }

        /// <summary>
        /// **伴生**（`Companion X: &lt;部队名&gt;`，2026-09-13 A2）。
        ///
        /// 🔴 **2026-10-18（`A920` 的续）：整条链照原版重写（铁律 5 就地订正 + 铁律 11）。**
        ///    改之前这一段的判据写的是**粉丝实体版规则书**（`Warpforge_Offline_Rulebook_…:176`
        ///    「从手牌打出时，可打出至多 X 张其伴生部队」）—— 那份**不是官方文档**
        ///    （它自己第 5 行写着 `Not official. Fan project.`，见 `CLAUDE.md` §一·2）
        ///    ⇒ **只能当第二来源**，不能拿来定机制。做法也与原版**三点全反**：
        ///    「遍历手牌找同名」·「`DeployFree` 直接上场」·「`Hand.RemoveAt` 把它从手里拿走」。
        ///
        ///    **原版判据（第一权威 = 反编译，逐跳核过）**：
        ///    ```
        ///    _ResolvePlayCardFromHand_d__447:1337/:1340  ProcessTide / ProcessCompanion(打出的那张牌)
        ///    CardScript__ProcessCompanion.c:26  HasCurrentTrait(0x49c = `Companion`)
        ///                                   :28  带词条 ⇒ 取 `rawCardData.relatedCard1 // +0xE8`
        ///                                   :29  不带词条 ⇒ `if (companionCounter < 1) return;`
        ///                                   :70  不带词条 ⇒ 源 = **它自己的 `rawCardData`**（= 造一张自己）
        ///                                   :85  AddNewCardToHand(manager, 源【卡定义】, 源卡, isPlayer, …)
        ///    BattleManager__AddNewCardToHand.c:65  BattleCardManager.CreateCard(…) ← **新造一张**
        ///                                   :97  BattleActionType 0x17 = createHandCard（`BattleActionType.cs:26`）
        ///    _ResolveCreateHandCard_d__512:135     PlayerHand.AddCardNotDrawnToHand(新卡) ← **进手牌**
        ///    ProcessCompanion.c:88-89  newCard.isCompanion = 1 · newCard.companionCounter = 源 − 1
        ///    ```
        ///    ⇒ 三点：**按【卡定义】造一张新卡 · 进【手牌】· 全程零 `Remove*`**
        ///      （P1 已把全链五个方法体逐符号搜过：`RemoveCardFromHand` / `RemoveFromGameCard` /
        ///       `RemoveFromDeck` / `SendToCemetery` / `RemoveCard` / `FindCard` / `GetCard` **零命中**）。
        ///
        ///    🔴 **那个 `N` 是【链长】，不是「一次给 N 张」**：原版一次 `ProcessCompanion` **只造一张**、
        ///    并把 `companionCounter = 源 − 1` 回填到**新卡**上；那张新卡（自己**不带** `Companion` 词条）
        ///    再被打出时走 `:29` 那一支 ⇒ **再造一张自己**、计数再减一。
        ///    ⇒ `Companion 2: Gun Drone` 一共带出 **2 张**，但是「**打一次给一张**」。
        ///    同一说明见 `CardInstance.CompanionCounter` 的注释。
        ///
        ///    ⚠️ **`relatedCard1` 本地拿不到**（序列化引用，解包资产里为空 ——
        ///    `资料/阶段二_卡片详情窗_原版规格.md:199-200` 已定案「数据不在库里」）⇒
        ///    名字那一跳仍然得留着：把卡面 `Companion X: &lt;名字&gt;` 里的 `&lt;名字&gt;`
        ///    当 `relatedCard1` 的**替身**去卡池里查（`CreatePool.FindByName`）。
        ///    ⛔ 但**绝不能**拿它当「从手牌取同名卡」的理由 —— 那是两件事。
        ///
        ///    ⚠️ **查不到那张卡时不许静默**：如实打日志（红线）。
        /// </summary>
        static void PlayCompanions(BattleContext ctx, int p, CardDef card, CardInstance inst)
        {
            if (card == null) return;

            // ---- ① 该不该触发 + 要造的是**哪一张卡的定义** ----
            //   原版 `ShouldTriggerCompanion`（`CardScript__ShouldTriggerCompanion.c:12-29`）：
            //   「（带 `Companion` 词条 **且** `relatedCard1 != null`）**或** `companionCounter > 0`」。
            CardDef def;                    // 要造的那张卡的**定义**
            int n;                          // 回填到新卡上的 `companionCounter`
            string why;
            if (card.Has(KeywordTable.Companion))
            {
                string name = card.CompanionName;
                int x = card.KwValue(KeywordTable.Companion);
                if (string.IsNullOrEmpty(name) || x <= 0)
                {
                    ctx.Log($"伴生：{card.Name} 有 `Companion` 但**名字或数量读不出来**"
                          + $"（名字「{name}」，数量 {x}）—— 这次没带出任何一张");
                    return;
                }
                if (ctx.CardPool == null)
                {
                    ctx.Log($"伴生：{card.Name} 要给一张「{name}」，但**这一局没给卡池**"
                          + " —— 查不了（原版这里是 `rawCardData.relatedCard1` 那个引用）");
                    return;
                }
                def = CreatePool.FindByName(ctx.CardPool, name);
                if (def == null)
                {
                    ctx.Log($"伴生：{card.Name} 的伴生部队「{name}」**在卡池里查不到**"
                          + "（原版取的是 `rawCardData.relatedCard1` 那个引用，本地解包资产里是空的"
                          + " ⇒ 我们只能按名字查）—— 这次没带出任何一张");
                    return;
                }
                n = x - 1;
                why = $"`Companion {x}: {name}`";
                // ⚠️ **表示上的已知差异（行为等价，2026-10-18 第三轮如实记着）**：
                //    原版新卡的 `companionCounter = 源 +0x60 − 1`，而源卡的 `+0x60` 是
                //    **词条值** —— `Companion:`（**没写数字**，如 `Strike Team`）时那一格是 **0**
                //    ⇒ 原版记 **−1**，我们（`KwValue` 兜底 1）记 **0**。
                //    **判据（下游只看正负）**：`ProcessCompanion.c:29` 是 `if (counter < 1) return;`
                //    ⇒ −1 与 0 **行为完全一样**（都只造一张、都停）。⛔ 别为了「数字对齐」去改兜底 1
                //    （`CardDef.cs` 的 `FirstNumber` 那条兜底被几十处当「有这个关键词」用）。
                // ⚠️ **同名多份** ⇒ `FindByName` 取的是**池序第一个**（可能张冠李戴）⇒ **出声**。
                //    原版这里是 `rawCardData.relatedCard1` 那个**引用**、不会歧义；我们的替身只能按名字查。
                //    实测（审查 2026-10-18）：全池有 3 组同名归一化（`Terminator Champion`（BL44/EC33）·
                //    `Maulerfiend`（BL46/EC39）· `Terminator`（EC22/UM82））—— **今天不咬人**
                //    （8 张伴生卡的伴生名全是 Tau Drone 一族），但这条歧义是潜伏的 ⇒ 走到就报。
                {
                    string want = CreatePool.Norm(name);
                    int dupN = 0;
                    foreach (var cc in ctx.CardPool)
                        if (cc != null && CreatePool.Norm(cc.Name) == want) dupN++;
                    if (dupN > 1)
                        ctx.Log($"伴生：卡池里有 **{dupN} 张**叫「{name}」的卡 —— 我们取的是"
                              + "**池序第一个**（原版取的是 `rawCardData.relatedCard1` 那个引用、"
                              + "不会有歧义；我们的替身只能按名字查）");
                }
            }
            else if (inst != null && inst.CompanionCounter > 0)
            {
                // 不带词条但计数 > 0 ⇒ **再造一张【自己】**（`ProcessCompanion.c:70`：
                // 不带 `Companion` 词条时源就是它自己的 `rawCardData`）。
                def = card;
                n = inst.CompanionCounter - 1;
                why = $"伴生链还剩 {inst.CompanionCounter} 环";
            }
            else
            {
                // ---- 无事可做（原版 `ProcessCompanion.c:29` 就是 `return`）----
                // 🔴 **2026-10-18（`W4` 整改 · 审查问题 4）：这一支不许「静默地」吞掉我们自己的漏解析。**
                //    原版这儿**真的没有事可做**（没词条、计数 0）⇒ 对**普通卡**打日志是噪声、
                //    而且会淹掉战斗日志面板（每次部署都来一行）—— 那不是「复刻」。
                //    ⇒ 出声的条件收紧成**我们自己的失败长相**：**卡面/关键词里读得出伴生名，
                //      却没有 `Companion` 词条**（`TAU38 Coldstar` / `TAU28 Strike Team` 修之前
                //      就是这个长相：`desc` 印着 `Companion 2: Marker Drone`、
                //      `keywords` 数组里却没有它 ⇒ `Has(Companion)` 恒假）。
                //    ⚠️ `CardDef.CollectCompanionKeyword`（2026-10-18 同批修的**输入侧**）落地之后
                //      这一支**预期永不触发** —— 留着它是**护栏**：将来再有同类数据缺陷，这里会喊。
                if (!string.IsNullOrEmpty(card.CompanionName))
                    ctx.Log($"伴生：{card.Name} 的卡面/关键词里**读得出伴生名**"
                          + $"（「{card.CompanionName}」），但**没有 `Companion` 词条**"
                          + " —— 这是**我们解析层漏了**（`CardDef.CollectCompanionKeyword` 那一层），"
                          + "这次没带出任何一张");
                return;
            }

            // ---- ② 造一张新卡 → **进手牌** ----
            //   ⛔ **不碰手牌里的同名卡**（原版全链一次都没读 `currentHand` / 牌库）
            //   ⛔ **不 `DeployFree`**（原版进的是**手牌**，不是场）
            //   ⛔ **不 `Remove`**（原版全链零移除）
            var ps = ctx.Players[p];
            var made = ctx.NewInstance(def);
            made.IsCompanion = true;        // 原版 `newCard + 0x64 = 1`
            made.CompanionCounter = n;      // 原版 `newCard + 0x60 = 源 +0x60 − 1`
            ps.Hand.Add(made);
            // 🆕 进手牌 ⇒ 也要吃上「还在生效的手牌效果」（原版同一条路
            //    `PlayerHand.AddCardNotDrawnToHand` 内部就叫 `SetupCardInHand` —— `A885` ②）。
            SetupCardInHand(ctx, p, made);
            // ⚠️ 手牌满 ⇒ 原版把**刚造的这张** `SendToCemetery`（`AddCardNotDrawnToHand_d__39:64`）。
            //    我们这边 `EnforceHandLimit` 丢**表尾那一张**，而上面刚把它 `Add` 在末尾
            //    ⇒ **手牌没超上限时**丢的就是它（等价）；⚠️ **手牌本来就超上限时会多丢一张**
            //    （原版那一张根本不会进手牌）—— 如实标注，判据详见 `EnforceHandLimit` 的注释。
            EnforceHandLimit(ctx, p);
            ctx.Log($"伴生（{why}）：{card.Name} 造出「{def.Name}」**放进手牌**"
                  + $"（`isCompanion`，这条链上还剩 {n} 环 —— 原版 `ProcessCompanion` → "
                  + "`AddNewCardToHand`；**不是从手牌取一张、也不上场**）");
        }

        /// <summary>
        /// **虫群合并**（`Swarm`，2026-09-13 A2）。语义全部照原版反编译
        /// （`decomp_out/CardScript__ResolveCardPlayed.c`）：
        ///   · 只看**右边的紧邻格**（`BattleManager.GetAdjacentUnitRight`）—— 不是全盘找同名；
        ///   · 判据是**卡名全等**（`System_String__op_Equality`）；
        ///   · 合并 = 数值相加、新来的**压在下面**（`UnitState.SwarmUnder`）。
        ///
        /// ⚠️ **简化（如实标着）**：原版 `AddExecuteSwarm(self, right, 3, 1)` 还带两个参数
        ///    （看着像动画/来源标记），我们只做数值合并 —— 表现层看到的是「新卡落地又立刻并进去」。
        /// ⚠️ 合并后**只有右边那一格还在**，所以「新来的那张」自己的 `Strike`/`Slay` 之类
        ///    以后不会再单独触发（它已经不在场上了）—— 和「压在下面」的物理含义一致。
        /// </summary>
        static void TrySwarmMerge(BattleContext ctx, int p, int slot, UnitState just)
        {
            if (just == null || just.Card == null || !just.Has(KeywordTable.Swarm)) return;
            int right = slot + 1;
            if (!BoardSpec.IsValid(right)) return;
            var host = ctx.Players[p].Board[right];
            if (host == null || host.IsAlive == false || host.Card == null) return;
            if (host.Card.Name != just.Card.Name) return;         // 卡名**全等**

            host.Attack += just.Attack;
            host.RangedAttack += just.RangedAttack;
            host.Health += just.Health;
            host.MaxHealth += just.MaxHealth;
            host.SwarmUnder.Add(just.Instance);   // 第 7 行第 2 步：压在下面的是**那一份实例**
            // 🔴 **2026-10-01：走 `RemoveAt`（会补位）而不是直接置 null** —— 原版棋盘是连续列表，
            //    被并掉的那一张离开列表之后，**它外侧的单位整体内移一格**
            //    （`MinionManager__RemoveMinion.c:35-36` 的 `List.Remove` + 紧跟的
            //     `RefreshOccupationSlots`）。本例里 `just` 是**刚插进来的那一张**：
            //     它**外侧**（离督军更远）的那些人会整体内移一格补上这个空档。
            BoardSlots.RemoveAt(ctx.Players[p], slot);
            Auras.Recompose(ctx);          // 🆕 A7：棋盘变动 ⇒ 光环重算
            ctx.Log($"虫群：{just.Name} 合并到右侧的同名部队上"
                  + $"（现在 {host.Attack}/{host.Health}，新来的**压在下面**）");

            // `When a friendly unit triggers Swarm, …`（`Tyranid Prime` / `Termagant Brood`）——
            // 触发者是**合并后还活着的那一个**（新来的已经不在场上了）。
            // ⚠️ 走 `BroadcastKeywordEvent` 而不是 `FireTriggerAt`：虫群**没有卡面正文**，
            //    而 `FireTriggerAt` 在「没写效果」时会提前 return、**连广播都不发**。
            BroadcastKeywordEvent(ctx, WhenEventKind.Triggers(KeywordTable.Swarm), host);
            // 🆕 2026-09-30：**补表现层那条 `EvtKind.Trigger`** —— 上面那条广播走的是
            //   `BroadcastWhen`（只唤醒「写了 `When … triggers Swarm` 正文的监听者」），
            //   **整段体内没有任何 `ctx.Emit`** ⇒ 事件流里**没有**这一格，表现层（trait 粒子/框）永远收不到。
            //   这正是判据文件与 `BattleDriver.PlayTraitParticles` 头部记的那条缺口（原来记成「要做」）。
            //   🔴 **格位怎么取（2026-10-01 二次订正）**：连续棋盘下**宿主会补位**，
            //   而往哪边补**取决于是哪一侧** ——
            //     · **左侧**：`right = slot+1` 是**靠里**那一格（下标更小）⇒ 摘掉外面的 `slot`
            //       之后宿主**不动**，格位还是 `right`；
            //     · **右侧**：`right = slot+1` 是**靠外**那一格（下标 +1）⇒ 补位之后宿主**滑到 `slot`**。
            //   ⇒ 别再写死哪一个（第一版写死 `right`、第二版写死 `slot`，**两次都在另一侧错**）。
            //     **按身份现查**：表现层是拿 `Ctx.Players[e.Player].Board[e.Slot]` 反查单位视图的，
            //     给的格号必须指向**还活着的那一个**。
            int hostSlot = right;
            {
                int hp, hs;
                if (FindSlot(ctx, host, out hp, out hs)) hostSlot = hs;
            }
            ctx.Emit(EvtKind.Trigger, p, hostSlot, host.Name,
                     keyword: KeywordTable.Swarm, effect: null, amount: 0);
        }

        // ==================================================================
        //  免费部署（`Deploy …` 效果的落点）
        // ==================================================================

        /// <summary>
        /// **免费把一个单位放进本方「不挤别人」的那一格** —— 不花能量、不占手牌。
        /// ⚠️ **2026-10-05 更正（铁律 5）：这一行原来写「免费把一个单位放进本方第一个空格」** ——
        ///   正文 ① 早在 **2026-10-01** 就订正成「人少的那一侧的最外一格」了，**summary 这半句没跟着改**
        ///   （改前 ① 写的是「从槽 0 起找第一个空格（跳过督军槽）」），下个会话只看 summary 会再读到一次过期口径。
        /// 逐条照抄我们自己的 `rule_core.gd:3871` 的 `_deploy_unit`（⚠️ `rule_core.gd` 是我们自己的 Godot 复刻，**非权威**；
        ///   ① 的落点以原版反编译为准，出处见下）：
        ///   ① **人少的那一侧的最外一格**（平手走右）——
        ///      出处 `MinionManager__GetNextSlotWithoutDisplacing.c:14-30` ＋
        ///      `BattleManager._ResolveSummonUnit_d__510__MoveNext.c:135,274`；
        ///      **不是**「从 0 号格起第一个空格」。**满场就什么都不做**（返回 false，不挤掉别人）。
        ///      ⚠️ 名字里的 **without displacing** 是判据：这条路**不挤人** ⇒ 直接写在那一格上、**不走 `Insert`**
        ///      （对比：**出牌**走 `BoardSlots.Insert` = 插入后它后面的单位整体外移一格）。
        ///   ② `fast` / `flank` 的部署当回合不疲劳 —— `UnitState` 构造里先按**卡模板**关键词算一次，
        ///      落地后**再按光环给的那一份重算一次**（判据 `HasDeployExemption`，见下面正文里那段 🔴 F8）；
        ///   ③ 发一条部署事件（表现层靠它播登场特效）。
        ///
        /// ⚠️ **不触发 Rally**。这是**故意的**，不是漏了：规则书 `:200` 写的是
        ///   「集结：**从手牌**部署后触发效果」，原版也只在 `play_card` 那条路上触发
        ///   （`rule_core.gd:2314`），`_deploy_unit` 里没有（它的注释明说 play_card 路径自己广播）。
        ///   `RuleCore.PlayCard` 那边照旧触发 —— 两条路不一样是对的。
        /// </summary>
        /// <param name="slot">放到哪一格；失败时是 -1</param>
        /// <param name="inst">
        /// **上去的是哪一份**（第 7 行第 2 步）。两条来源必须分清：
        ///   · **从某个区域挪过来的**（手牌 / 牌库 / 弃牌堆 / 残骸）⇒ 传**那个实例**（沿用）；
        ///   · **凭空造一张**（卡池 / 指代 / 造衍生物）⇒ 传 `ctx.NewInstance(card)`（新发一份）。
        /// </param>
        public static bool DeployFree(BattleContext ctx, int owner, CardInstance inst, out int slot)
        {
            slot = -1;
            if (ctx == null || inst == null || inst.Card == null) return false;
            if (owner < 0 || owner >= ctx.Players.Length) return false;

            var card = inst.Card;
            var ps = ctx.Players[owner];
            // 🔴 **2026-10-01：落点改成原版那一条** —— 效果召唤走 `GetNextSlotWithoutDisplacing`：
            //    **人少的那一侧的最外一格**（平手走右），**不是**「从 0 号格起第一个空格」。
            //    出处 = `BattleManager._ResolveSummonUnit_d__510__MoveNext.c:135,274`
            //    （`SummonMinion(卡, GetNextSlotWithoutDisplacing(), …)` 那个实参就是它）
            //    ＋ `MinionManager__GetNextSlotWithoutDisplacing.c:14-30`（左 < 右 ⇒ 左，否则右）。
            //    ⚠️ 名字里的 **without displacing** 是判据：这条路**不挤人** ⇒ 直接写在那一格上，
            //      不需要走 `Insert`（那一格按定义就是空的）。
            int dst = BoardSlots.NextWithoutDisplacing(ps);
            if (dst < 0) return false;      // 满场 ⇒ 什么都不做（这条口径没变）
            {
                var unit = new UnitState(inst, false);   // 第 7 行第 2 步：实例原样上场
                unit.DeployedTurn = ctx.Turn;   // 🆕 同上：免费部署也算「本回合上场」
                ps.Board[dst] = unit;
                Auras.Recompose(ctx);      // 🆕 A7：棋盘变动 ⇒ 光环重算（理由同 `PlayCard`）
                // 🔴 **2026-10-17（F8）：部署豁免在【这条入口】上也要重算一次** ——
                //    与 `PlayCard` 里 `Auras.Recompose` 之后那一句**同一个理由、同一份判据**（别在这边另写一份）：
                //    上面 `new UnitState(inst, false)` 的 `Exhausted` 是**构造时**按**卡模板**关键词算的
                //    快照（`Core/UnitState.cs:257`），而光环（`Auras.Recompose` → `Core/Aura.cs:653`
                //    的 `AddAuraKeyword`）是在**那之后**才可能把 `fast`/`flank`/`ferocity` 挂上来的
                //    ⇒ 不重算就是「**光环给了侧翼、单位却动不了**」（静默错）。
                //    卡池里真有这类光环（四张，清单一处：`资料/普查产出_1017/F8_第二入口.md` §①）：
                //    `AM73 Vitus Gryf` · `DA30 Ravenwing Talonmaster` · `EC8 Alluress` · `TAU31 Devilfish`
                //    —— 它们都走「效果免费部署 / 召唤 / 再造」这条路（本方法）。
                //    ⚠️ **只重算刚落地的这一个 `unit`** —— ⛔ 别遍历全场：本回合**已经行动过**的单位
                //      也是 `Exhausted = true`，一律解掉会把它们放活（一回合动两次，同样是静默错）。
                unit.Exhausted = !HasDeployExemption(unit);
                // 🆕 2026-10-18（`W5` · `K3`）：**第二条部署入口**也要落那两个写点 ——
                //   理由与 `PlayCard` 里那两句**逐字同源**（`CardSetup` 的重挑 + `ActivateMinion` 的 `= 4`），
                //   判据别在这儿另写一份（铁律 10 第 5 条：一个对象有多个入口时每个入口都要显式设置）。
                RecomputeCurrentAttackType(ctx, owner, dst);
                ForceAttackTypeOnDeploy(ctx, owner, dst);
                slot = dst;
                ctx.Log($"{ps.Name} 免费部署 {unit.Name}（{unit.Attack}/{unit.Health}）到槽 {dst}");
                ctx.Emit(EvtKind.Deploy, owner, dst, unit.Name);
                // **事件层广播**也管这条路 —— 理由同 `ResolveDeploy`：
                // 卡面写的是 `you deploy a Vehicle`，效果免费部署同样是「部署」（`资料/事件层_数据与设计.md` §三）。
                BroadcastWhen(ctx, WhenEventKind.Deploy, owner, unit.Card, unit);
                // **部署时触发也管这条路**：卡面写的是 `you **put in play**`，
                // 效果免费部署同样是「放进场上」（`Armoured Support` 那张 UM 卡说的就是它）。
                // ⚠️ 但 **Rally 不管** —— 规则书写的是「**从手牌**部署后触发」（见上面注释）。
                //    两条路一个管一个不管，是**故意的**，别顺手改齐。
                ResolveDeploy(ctx, owner, unit);
                return true;
            }
            ctx.Log($"{ps.Name} 场上没空格了 —— {card.Name} 部署不了");
            return false;
        }

        /// <summary>
        /// **凭空造一张再免费部署**（卡池 / 指代 / 造衍生物那条路）。
        /// ⚠️ 手里/牌库里**已有的那一份**要走上一个重载（传实例）—— 用这一个会把实例身份丢掉
        /// （新发一份，回不了原来那一份）。两者**共用同一段实现**，只差「谁发实例」。
        /// </summary>
        public static bool DeployFree(BattleContext ctx, int owner, CardDef card, out int slot)
        {
            slot = -1;
            if (ctx == null || card == null) return false;
            return DeployFree(ctx, owner, ctx.NewInstance(card), out slot);
        }

        // ==================================================================
        //  攻击
        // ==================================================================

        /// <summary>
        /// 场上攻击力。近战和远程是两套数值。**所有攻击力修正都要走这里**，别在调用处直接读字段。
        ///
        ///   · 兽群（Pack）：场上每有 1 个**友方部队** +1 近战 +1 远程
        ///     （规则书 :195；我们自己的 `rule_core.gd:4172` `field_attack`（旁证、非权威） —— 原版只有这一处修正，
        ///      ⚠️ 它**不数督军**：过滤条件是 `not tu.is_warlord`，但**包含自己**）
        ///   · 失明（Blind）：**远程攻击设为 0**（规则书 :166；原版 `:4212` 是直接 `return ERR_NO_ATTACK`，
        ///     效果等价 —— 攻击力 0 就发不出攻击。走这里而不是在 `DeclareAttack` 里提前 return，
        ///     是为了让「为什么打不了」在界面上仍然显示成「没有攻击力」这一个原因）
        /// </summary>
        public static int FieldAttack(BattleContext ctx, int p, UnitState u, bool ranged)
        {
            if (ranged && u.IsBlind) return 0;

            int baseAtk = ranged ? u.RangedAttack : u.Attack;

            // `This troop's Melee is always equal to its Health`（`Scarab Swarm`，2026-09-14 A5 批 4）——
            // **只换基础值**：下面那些加值（`Pack` 的 +N）照常叠上去；
            // **只有近战**（卡面写的是 `Melee`，远程不受影响）。
            // 读点只此一处；「加值算不算」这一点是我们挑的，见 `CardDef.MeleeEqualsHealth` 的注释。
            if (!ranged && u.Card != null && u.Card.MeleeEqualsHealth) baseAtk = u.Health;

            if (u.Has("pack"))
            {
                int n = 0;
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var tu = board[s];
                    if (tu != null && !tu.IsWarlord) n++;
                }
                baseAtk += n;
            }
            return baseAtk;
        }

        /// <summary>
        /// 目标合法性。（rule_core.is_valid_target）
        /// </summary>
        public static int IsValidTarget(BattleContext ctx, int p, int atkSlot, int tgtP, int tgtSlot, bool ranged)
        {
            if (tgtP != 0 && tgtP != 1) return RuleCodes.ErrTarget;
            if (!BoardSpec.IsValid(atkSlot) || !BoardSpec.IsValid(tgtSlot)) return RuleCodes.ErrNotUnit;

            var attacker = ctx.Players[p].Board[atkSlot];
            if (attacker == null) return RuleCodes.ErrNotUnit;
            if (attacker.Has(KeywordTable.CantAttack)) return RuleCodes.ErrNoAttack;

            var target = ctx.Players[tgtP].Board[tgtSlot];
            if (target == null) return RuleCodes.ErrNotUnit;
            if (tgtP == p) return RuleCodes.ErrSelf;

            // Stealth：隐身单位不可被攻击（攻击后揭示）
            if (target.Has(KeywordTable.Stealth)) return RuleCodes.ErrTarget;

            // Vanguard：敌方场上有 Vanguard → 只能打 Vanguard。**卡牌游戏的核心目标规则**
            bool enemyHasVanguard = false;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[tgtP].Board[s];
                if (u != null && u != target && u.Has(KeywordTable.Vanguard))
                {
                    enemyHasVanguard = true;
                    break;
                }
            }
            // `This troop can ignore enemy units with Vanguard when attacking`（`Canoptek Wraith`，
            // Sautekh，2026-09-14 A5 批 4）—— 带这个标记的攻击者**跳过**这条限制。
            if (enemyHasVanguard && !target.Has(KeywordTable.Vanguard)
                && !(attacker.Card != null && attacker.Card.IgnoresVanguard))
                return RuleCodes.ErrTarget;

            // 毁灭者（Destroyer）：规则书 `:180`「总是优先攻击可被摧毁的单位」。
            // ✅ 2026-09-14 做掉。**判据照抄参考实现 `rule_core.gd:4136-4147`** ——
            //    它和 Vanguard **同构**：一条**硬约束**，不是「AI 打分偏好」。
            //      攻击者带 destroyer，且敌方场上存在**另一个「可被摧毁」的单位**
            //      （**不是 invulnerable、也不是 remnant**）
            //      ⇒ 这一下**不能打 invulnerable 的目标**。
            // 🔴 **「可被摧毁」不等于「这一下能打死」** —— 那是另一条规则。
            //    本工程原来记的是「判据 `RuleCore.WouldKill` 已现成、只是没接」，
            //    **那句是错的**（`WouldKill` 回答的是「这一下会不会致死」）。已就地更正。
            // ⚠️ 比参考实现多一条 `du.IsAlive` —— 我们的棋盘上**死掉的督军会留在槽 4**
            //    （`RemoveIfDead` 只挪非督军），不给这一条会把一具尸体算成「可被摧毁的目标」。
            if (attacker.Has(KeywordTable.Destroyer))
            {
                bool enemyDestroyable = false;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var du = ctx.Players[tgtP].Board[s];
                    if (du == null || du == target || !du.IsAlive) continue;
                    if (!du.Has("invulnerable") && !du.IsRemnant) { enemyDestroyable = true; break; }
                }
                if (enemyDestroyable && target.Has("invulnerable")) return RuleCodes.ErrTarget;
            }

            // Flying：**检查的是目标**（飞行单位不能被近战打到），远程正常，同为飞行可以。
            // ⚠️ rule_core.gd 修正过方向：「此前禁止飞行单位近战打地面、却允许地面近战打飞行」—— 正好反了
            if (!ranged && target.Has(KeywordTable.Flying) && !attacker.Has(KeywordTable.Flying))
                return RuleCodes.ErrTarget;

            return RuleCodes.OK;
        }

        /// <summary>「**这一格现在能不能发起攻击**」—— **唯一判据**。
        ///
        /// 原版 = `CardScript.CanAttackNow()`：`CardScript.ActivateMinion(isPlayerTurn)` 里调它，
        /// 结果直接喂给 `BattleCardUI.canAttackAnim`（偏移 `+0x140`）的 `SetActive`
        /// ⇒ **场上那圈「未行动」的绿光（`CanActParticles` + `RotatingRing`）亮不亮就是它**。
        ///
        /// 🔴 **为什么要单开一个方法**：视图层也要问同一个问题（绿光亮不亮），
        ///    而「两处写同一条规则 = 迟早不一致」是这工程的旧账 ⇒ **判据共用一份**：
        ///    `DeclareAttack` 开头那段检查**就是**调这里（顺序与返回码与原来逐字一致）。
        ///
        /// ⚠️ `ranged` 只影响**压制**那一条（`pindown` 只禁近战）——
        ///    视图要问「**至少有一种打法**能打」时，近战/远程各问一次（见 <see cref="CanActNow"/>）。
        /// </summary>
        /// <returns>`RuleCodes.OK` = 能打；否则是**第一处不通过的原因**（与 `DeclareAttack` 同源）。</returns>
        public static int CanAttackNow(BattleContext ctx, int p, int atkSlot, bool ranged = false)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!BoardSpec.IsValid(atkSlot)) return RuleCodes.ErrNotUnit;

            var u = ctx.Players[p].Board[atkSlot];
            if (u == null) return RuleCodes.ErrNotUnit;
            if (u.Exhausted) return RuleCodes.ErrExhausted;
            if (u.IsStunned) return RuleCodes.ErrStunned;

            // **攻击配额**：嗜血（Blood Thirst）每回合最多 2 次，其余 1 次。
            // 规则书 :172「你的回合可进行至多 2 次攻击」；我们自己的 `rule_core.gd:4205`（旁证、非权威）
            int atkLimit = u.Has("bloodthirst") ? 2 : 1;
            if (u.AttacksThisTurn >= atkLimit) return RuleCodes.ErrExhausted;

            // 压制：**无法执行近战攻击**（规则书 :194；原版 `:4209`）。⚠️ 只禁近战，远程照常 ——
            // 顺序也在原版那个位置：挨在攻击配额之后、攻击力判定之前。
            // （失明不在这里提前 return，它在 `FieldAttack` 里把远程攻击力算成 0，见那边的注释）
            if (!ranged && u.Has("pindown")) return RuleCodes.ErrPindown;

            if (FieldAttack(ctx, p, u, ranged) <= 0) return RuleCodes.ErrNoAttack;
            return RuleCodes.OK;
        }

        /// <summary>视图用：这一格**至少有一种打法**能打（近战或远程）。
        /// 原版 `CanAttackNow` 里那句「近战 &lt; 1 且 远程 &lt; 1 才算不能打」就是它。
        ///
        /// 🔴 **它顺带就是「该不该亮绿光」的判据**，理由（这一段差点读反，记下来）：
        ///   原版把「是谁的回合」放在**调用方** —— `CardScript.ActivateMinion(isPlayerTurn)` 头一句是
        ///   `if (this.isPlayer == isPlayerTurn &amp;&amp; state == 2)`，也就是「**这张牌的归属方 == 当前行动方**」；
        ///   而 `MinionManager.ActivateMinions` 会**两侧的管理器都过一遍**（`BroadcastActivateMinions`）
        ///   ⇒ 结论 = **谁行动谁那排亮，两个人看都一样**（敌方那排在敌方回合也会亮，不是只亮自己那排）。
        ///   ⇒ 我们这边 `p != ctx.Active ⇒ ErrNotTurn` **恰好就是**那个 `isPlayer == isPlayerTurn`，
        ///      所以本方法原样就能当视觉判据用，**不需要再加一条「是不是我方」**。
        /// </summary>
        public static bool CanActNow(BattleContext ctx, int p, int atkSlot)
        {
            return CanAttackNow(ctx, p, atkSlot, false) == RuleCodes.OK
                || CanAttackNow(ctx, p, atkSlot, true) == RuleCodes.OK;
        }

        /// <summary>
        /// 🆕 2026-10-18（A938）：**照卡上当前的攻击型打**（原版 `AiScripted.ExecuteAction` 的
        /// `Attack(30)` / `AttackFreeMode(31)` 那一支）。
        ///
        /// 判据（`AiScripted__ExecuteAction.c:1139-1143`）：
        ///   `uVar18 = *(undefined4 *)(actingCard + 0x120)` ⇒ `AddAttackAction(bm, actingCard, targetCard,
        ///   uVar18, globalVars + 0xb4, …)` —— **打法不是脚本字段，是卡上的持久状态**
        ///   ⇒ 也就是「**未显式指定打法**」那一条在引擎里的唯一落点。
        ///
        /// 🔴 **为什么不改 `DeclareAttack` 的默认值**：那个形参是 `bool ranged = false`，
        ///   全工程有 **60+ 处**省略它（本意都是「近战」）⇒ 把默认改成「读当前攻击型」会把它们
        ///   全部静默改道（`CurrentAttackType` 初值是「近战 < 远程 ? 远程 : 近战」）。
        ///   ⇒ 另开一个**名字不同**的重载（本方法），只给教程脚本那一档用。
        /// </summary>
        public static int DeclareAttackByCurrentType(BattleContext ctx, int p, int atkSlot,
                                                     int tgtP, int tgtSlot)
        {
            return DeclareAttack(ctx, p, atkSlot, tgtP, tgtSlot, CurrentAttackIsRanged(ctx, p, atkSlot));
        }

        /// <summary>🆕 2026-10-18（A938）：这一格上的单位**当前的攻击型是不是远程**
        /// （= 原版 `*(actingCard + 0x120) == 2`）。`ctx` 或那一格为空 ⇒ `false`（近战）。</summary>
        public static bool CurrentAttackIsRanged(BattleContext ctx, int p, int atkSlot)
        {
            if (ctx == null || p < 0 || p > 1 || !BoardSpec.IsValid(atkSlot)) return false;
            var u = ctx.Players[p].Board[atkSlot];
            return u != null && u.CurrentAttackType == UnitState.AttackTypeRanged;
        }

        /// <summary>🆕 2026-10-18（A938）：**改这一格的攻击型** —— 原版
        /// `CardScript.ChangeAttackType(card, attackType, playSound)`（`CardScript__ChangeAttackType.c:15`）。
        /// 原版那一支的副作用**只有一个音效**（`SoundManager.Play2D`），没有别的状态改
        /// ⇒ 我们这里就是纯赋值。返回 false = 那一格没有单位（**调用方出声**，别静默）。
        /// ⚠️ `type` 用 <see cref="UnitState.AttackTypeMelee"/> / <see cref="UnitState.AttackTypeRanged"/>。</summary>
        public static bool SetCurrentAttackType(BattleContext ctx, int p, int atkSlot, int type)
        {
            if (ctx == null || p < 0 || p > 1 || !BoardSpec.IsValid(atkSlot)) return false;
            var u = ctx.Players[p].Board[atkSlot];
            if (u == null) return false;
            u.CurrentAttackType = type;
            return true;
        }

        // ==================================================================
        //  🆕 2026-10-18（`W5` · `K3` 账）：`+0x120` **其余写点**那条链
        //  —— 「数值一变就自动重挑一档」（原版 `UpdateAttackText` / `OnTurnEnd` /
        //     `ResolveActiveAbilityPlayed` / `ChooseAttackTypeAutomatically`）
        //  ⛔ 别另起一套状态：全链只写 `UnitState.CurrentAttackType` 那一格。
        // ==================================================================

        /// <summary>
        /// 🆕 2026-10-18（`W5` · `K3` · `+0x120` 第 9 个写点）：**用了主动能力之后，带
        /// `duty`/`ferocity`/`oath` 的单位按当前数值重挑攻击型** —— 原版
        /// `CardScript.ResolveActiveAbilityPlayed`（`CardScript__ResolveActiveAbilityPlayed.c:29-42`）：
        /// ```
        /// *(int *)(card + 0x50) += 1;  *(char *)(card + 0x4c) = 1;      // 本回合次数 / usedActiveAbility
        /// if (HasCurrentTrait(0x4c4 /*duty 1220*/) || HasCurrentTrait(0x4f1 /*ferocity 1265*/)
        ///     || HasCurrentTrait(0x4fb /*oath 1275*/)) { … *(card + 0x120) = (melee < ranged) + 1; }
        /// ```
        /// ⇒ **只对那三个关键词的单位**（不是全场 —— 别改成 `RecomputeCurrentAttackTypes`）。
        /// trait id 出处：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:121/130/132`。
        /// 三个调用点 = 我们这边的「主动能力」三条路（`UseAbility` / `UseOathAbility` / `UseAlternative`）。
        /// </summary>
        static void RecomputeAttackTypeAfterActiveAbility(UnitState u)
        {
            if (u == null) return;
            if (!u.Has(KeywordTable.Duty) && !u.Has(KeywordTable.Ferocity) && !u.Has(KeywordTable.Oath))
                return;
            u.CurrentAttackType = ChooseAttackTypeAutomatically(u);
        }

        /// <summary>
        /// **按当前数值挑一档攻击型** —— 原版 `CardScript.ChooseAttackTypeAutomatically`
        /// （`CardScript__ChooseAttackTypeAutomatically.c:8-12`）：
        /// `(CurrentMeleeAttack &lt; CurrentRangeAttack) + 1` ⇒ **远程更高才取远程，平手取近战**。
        ///
        /// 🔴 **这是全仓【唯一】的这份算式**（同一条算式在原版出现在 5 个方法里：
        /// `CardSetup:263` · `ChooseAttackTypeAutomatically:10` · `OnTurnEnd:215` ·
        /// `ResolveActiveAbilityPlayed:37` · `UpdateAttackText:24`）；
        /// `UnitState` 的构造器也调它（原来是各写一遍）。
        /// ⚠️ 它与 `SimpleAI.UseRanged`（`Data/`，我们那处唯一的「近战还是远程」AI 判据）
        /// **逐个局面等价** —— `Core/` 不许引 `Data/`，所以这一份只服务「攻击型」这一格。
        /// </summary>
        public static int ChooseAttackTypeAutomatically(UnitState u)
        {
            if (u == null) return UnitState.AttackTypeMelee;
            return u.Attack < u.RangedAttack ? UnitState.AttackTypeRanged : UnitState.AttackTypeMelee;
        }

        /// <summary>
        /// **照当前数值重挑这一格单位的攻击型**（= 原版 `ChooseAttackTypeAutomatically` 那次写入）。
        /// 返回 false = 那一格没有单位（调用方出声，别静默）。
        /// </summary>
        public static bool RecomputeCurrentAttackType(BattleContext ctx, int p, int atkSlot)
        {
            if (ctx == null || p < 0 || p > 1 || !BoardSpec.IsValid(atkSlot)) return false;
            var u = ctx.Players[p].Board[atkSlot];
            if (u == null) return false;
            u.CurrentAttackType = ChooseAttackTypeAutomatically(u);
            return true;
        }

        /// <summary>按**引用**找这一刻在棋盘上的那一格并重挑（`ApplyOneGain` 那种「手上只有
        /// `UnitState`、没有格号」的场合用）。不在场上 ⇒ 什么都不做（它没有「攻击型」可言）。</summary>
        public static bool RecomputeCurrentAttackTypeOf(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return false;
            for (int p = 0; p < 2; p++)
            {
                var b = ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++)
                    if (ReferenceEquals(b[s], u)) return RecomputeCurrentAttackType(ctx, p, s);
            }
            return false;
        }

        /// <summary>
        /// **双方棋盘全量重挑** —— 对应原版 `CardScript.UpdateAttackText` 那条「数值变了就重挑」的链
        /// （`CardScript__UpdateAttackText.c:21-29`）。我们的「数值变了」发生在两处：
        /// `Auras.Recompose`（光环加/撤攻值）与 `EffectResolver.ApplyOneGain`（逐条增减益）。
        /// ⇒ 这两处之后各调一次（见各自的调用点）。
        /// ⚠️ **原版那一条有 `+0x40 == 0` 守卫**（只对「不是本机玩家那一侧」的卡重挑）——
        ///   我们**没有复刻**，理由逐条写在 `UnitState.CurrentAttackType` 的注释里（`Core/` 无本机概念）。
        /// </summary>
        public static void RecomputeCurrentAttackTypes(BattleContext ctx)
        {
            if (ctx == null) return;
            for (int p = 0; p < 2; p++)
            {
                var b = ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++)
                    if (b[s] != null) b[s].CurrentAttackType = ChooseAttackTypeAutomatically(b[s]);
            }
        }

        /// <summary>
        /// 🆕 2026-10-18（`W5` · `K3` 第 7 个写点）：**刚部署、还带着召唤病、又带 `ferocity`/`oath`
        /// 的单位 ⇒ 攻击型写 `4`（主动技能那一档）** —— 原版 `CardScript.ActivateMinion`
        /// （`CardScript__ActivateMinion.c:43-47,70-72`）：
        /// ```
        /// if (HasCurrentTrait(0x4f1 /*ferocity 1265*/) || HasCurrentTrait(0x4fb /*oath 1275*/))
        ///     if (displaySummonSickness &amp;&amp; *(int *)(card + 0x120) != 4) *(int *)(card + 0x120) = 4;
        /// ```
        /// ⚠️ 原版那个 `displaySummonSickness` 是**展示层那一份**（`+0x228 == 2` 那套条件），
        ///   我们的对应物是 `UnitState.Exhausted`（部署当回合不可行动 = 召唤病；
        ///   见构造器里 `Exhausted = !RuleCore.HasDeployExemption(this)`）。
        ///   ⚠️ `ferocity` 本身是**部署豁免**（`HasDeployExemption` 含它）⇒ 带 `ferocity` 的单位
        ///   基本不会是召唤病；真正会走到这一支的是 **`oath`** 单位（誓约不是豁免）。
        ///   如实标着：这一格今天**只影响表现层的「当前打法」提示**，不改伤害
        ///   （我们的伤害是显式传 `ranged` 算的）。
        /// </summary>
        public static bool ForceAttackTypeOnDeploy(BattleContext ctx, int p, int atkSlot)
        {
            if (ctx == null || p < 0 || p > 1 || !BoardSpec.IsValid(atkSlot)) return false;
            var u = ctx.Players[p].Board[atkSlot];
            if (u == null) return false;
            if (!u.Exhausted) return false;                       // 原版：`displaySummonSickness` 为真才写
            if (!u.Has(KeywordTable.Ferocity) && !u.Has(KeywordTable.Oath)) return false;
            u.CurrentAttackType = UnitState.AttackTypeActive;
            return true;
        }

        /// <summary>
        /// 攻击结算。（rule_core.declare_attack）
        ///
        /// ⚠️⚠️ **远程攻击默认也吃反击。** 只有 Long Range 免。
        ///    直觉上会写成「远程不受反击」，那是错的 —— rule_core.gd 有明确修正记录：
        ///    「2026-08-21 修正：此前远程完全不吃反击，Long Range/Sniper 成死代码」。
        /// </summary>
        public static int DeclareAttack(BattleContext ctx, int p, int atkSlot,
                                        int tgtP, int tgtSlot, bool ranged = false)
        {
            // 🆕 2026-09-26：开头这一整段（回合/格位/疲劳/眩晕/配额/压制/攻击力）**只写一次** ⇒
            //    搬进 `CanAttackNow`，与视图层那圈「未行动」绿光共用（见它的注释）。
            //    ⚠️ **顺序与返回码与原来逐字一致** —— 2995 条规则断言盯着它们，别顺手重排。
            int pre = CanAttackNow(ctx, p, atkSlot, ranged);
            if (pre != RuleCodes.OK) return pre;

            var attacker = ctx.Players[p].Board[atkSlot];
            int atk = FieldAttack(ctx, p, attacker, ranged);
            int atkLimit = attacker.Has("bloodthirst") ? 2 : 1;   // 上面 `CanAttackNow` 已查过一次，这里要用来记账

            int code = IsValidTarget(ctx, p, atkSlot, tgtP, tgtSlot, ranged);
            if (code != RuleCodes.OK) return code;

            // 攻击者消耗：**达到配额上限才疲劳** —— 我们自己的 `rule_core.gd:4255`（旁证、非权威）：
            // `attacks_turn += 1; if attacks_turn >= atk_limit: exhausted = true`
            // （嗜血单位打完第一次**不**疲劳，所以还能再打一次）
            attacker.AttacksThisTurn++;
            if (attacker.AttacksThisTurn >= atkLimit) attacker.Exhausted = true;
            // 🆕 2026-09-30：嗜血的「亮一下」—— 原版 `FinishAfterAttack` 那条路（判据 → `EmitBloodThirst`）
            EmitBloodThirst(ctx, p, atkSlot, attacker);
            // 🆕 2026-09-29：记下「这次用的是哪一档打法」（原版 `EntityScript.currentAttackType`）
            // —— 只给表现层用（`AttackSelector` 那圈高亮挂的就是它），判据 → `UnitState.LastAttackType`。
            attacker.LastAttackType = ranged ? 2 : 1;
            if (attacker.Has(KeywordTable.Stealth))
            {
                attacker.RemoveKeyword(KeywordTable.Stealth);
                // 🆕 `When a friendly unit loses Stealth, …`（2026-09-13 第三十四轮）。
                //    ⚠️ **2026-09-14 A7 更正**：这里原来写「**全仓只有这一处**会摘掉 Stealth」——
                //    现在**有两处**了：另一处是 `BeginTurn` 的「一回合到期」
                //    （规则书 `:211`「一回合内**或**本单位攻击前」的**前半句**）。
                //    **两处都必须广播**这个事件，否则「失去潜行」的监听器只有一半会响。
                BroadcastKeywordEvent(ctx, WhenEventKind.LosesStealth, attacker);
                ctx.Log($"{attacker.Name} 攻击后现身（失去 Stealth）");
            }
            // 伪装：**攻击前**不能被敌方战术/效果选中，攻击之后就没了（规则书 :173；原版 `:4259` 一带）
            if (attacker.Has("camouflage"))
            {
                attacker.RemoveKeyword("camouflage");
                ctx.Log($"{attacker.Name} 攻击后失去伪装（Camouflage）");
            }

            var target = ctx.Players[tgtP].Board[tgtSlot];

            // ---- **替身**（`Any attack against your Warlord targets this troop instead.`）----
            //  出处：`Vargard Obyron`（Sautekh），2026-09-14 A5 批 4。
            //  ⚠️ **位置照参考实现**（`rule_core.gd:4267`）：**合法性已经按督军验过之后**才改目标 ——
            //     改在验证之前会和「督军格特殊」那套判据打架。
            //  ⚠️ 只找**防御方场上第一个**带标记的**非督军**单位（参考实现同口径）；
            //     一个都没有时照常打督军（不是「打不了」）。
            if (target != null && target.IsWarlord)
            {
                var tb = ctx.Players[tgtP].Board;
                for (int gs = 0; gs < BoardSpec.Size; gs++)
                {
                    var gu = tb[gs];
                    if (gu == null || !gu.IsAlive || gu.IsWarlord) continue;
                    if (gu.Card == null || !gu.Card.Bodyguard) continue;
                    ctx.Log($"{attacker.Name} 打的是督军，但「{gu.Name}」是**替身** —— 改打它");
                    tgtSlot = gs;
                    target = gu;
                    break;
                }
            }

            // 攻击宣言：**在伤害之前**发 —— 表现层才有「抬手 → 命中」的余地
            // `targetCardId` 现在就记下来：留存日志以后回看时，那个格位早就换人了
            ctx.Emit(EvtKind.Attack, p, atkSlot, attacker.Name, ranged: ranged,
                     targetPlayer: tgtP, targetSlot: tgtSlot,
                     targetCardId: target != null ? target.Name : null);

            // **事件层广播**（`When a friendly unit attacks, …`）—— 第三十二轮。
            // ⚠️ 和 `Emit` 同位置：都在**伤害之前**（监听方该看到的是「**谁要打谁**」，
            //    而不是「打完的结果」；要结果的用 `die` / `kills` 那一族）。
            // 🆕 2026-09-13 A3：**宾语（被打的那个）也传下去** ——
            //    `When this unit attacks an enemy with Hunt Mark, …`（`Long Fang`）要拿它做筛选，
            //    正文里的 `the target of the attack` / 裸 `adjacent` 也要指它（`ctx.EventTarget`）。
            //    **原版依据**：`BattleManagerSupport.BroadcastUnitAttacked(manager, actingCard, targetCard, …)`
            //    (`BattleManagerSupport.cs:131`) 逐支都带 `targetCard`，
            //    触发判定拿 `CardAbility.targetCriteria.traitsFilter` 去筛它
            //    （`CardAbility.cs:8-32` · `TargetCriteria.cs:46`，`huntMark = 1260` 是 DefinedTrait）。
            //    ⚠️ 原来只传攻击者 ⇒ 卡面写「打的敌人带猎杀标记」时，判据会去问**攻击者**有没有标记，
            //       **永远判不中且不报错**（子代理 2026-09-13 核出来的）。
            BroadcastWhen(ctx, WhenEventKind.Attack, p, attacker.Card, attacker,
                          actor: attacker, target: target);

            // ---- 这一段的局部状态（要在批**外面**声明，批里批外都要用）----
            bool targetDied = false;
            int dealt = 0;
            int hpBefore = 0;                  // 践踏要算「溢出多少」，所以得记打之前那一下
            // 反击值**现在就取**：规则书 `:145` 那个例子里，被这一下打死的初生者**照样反击** 1 点，
            // 所以取的是「受伤**之前**」的攻击力。⚠️ 我们自己的 `rule_core.gd:4310`（旁证、非权威） 有一条修正记录
            // 「此前『目标死则不反击』= 近战击杀免反（规则偏差）」——**别退回**。
            int counterAtk = target.Attack;

            // ══════════════════════════════════════════════════════════════════
            //  **「同时伤害」批**（2026-09-13 第三十二轮）—— 规则书 :145 + :238
            //
            //  规则书 `:145`：「伤害按声明的攻击类型**同时结算**」，
            //  例子是「兽人小子造成 3 点伤害，**同时**受到 1 点反击。初生者（0 生命）进入弃牌堆」——
            //  注意顺序：**先两边都打，然后才有人进弃牌堆**。
            //  `:238`：「序列：攻击 → **双方结算伤害** → 生命归 0 方触发效果 → 摧毁方触发效果」。
            //
            //  🔴 修之前：`Hurt` 一边扣血一边**当场**放死亡触发（`CleanupDeaths` → Backlash），
            //     所以**目标一死，它的 Backlash 就比反击先放**。被攻击方的 Backlash 若把攻击者打死，
            //     反击整下被跳过 —— **每一局带 Backlash/Penitence 的攻击都算错。**
            //
            //  ⚠️ **批里放哪几步，是按规则书的顺序挑的**：
            //     · **哨戒**（`:205`）在批内 → 它可能**先打死攻击者**，而攻击「照常结算」
            //     · **星镖**（`:207`）在批内 → 它**在主伤害之前**，可能直接打死目标、跳过主伤害
            //     · **主攻击 + 反击**在批内 → 这就是 `:145` 那句「同时」
            //     · **践踏 / 爆裂**留**批外**（各自另开一个批）—— 它们是「攻击时**对相邻**」的衍生伤害，
            //       规则书把攻击段列成 ①哨戒 ②星镖 ③攻击/反击 ④践踏 ⑤爆裂，是**逐步**的；
            //       而且留批外能保住一条既有行为：目标还在场上时能吃到「主伤害 + 爆裂」两次
            //       （`FieldAttack` 的模拟器给的就是这个口径）。**如实标着：这一条是照原版攻击段顺序排的，
            //       规则书 :238 那句「同时」只点名了攻击与反击。**
            using (SimultaneousDamage(ctx))
            {
                // ---- 哨戒 X：**攻击哨戒单位时攻击者先受 X 伤害**，「然后照常结算攻击」----
                //      规则书 :205；我们自己的 `rule_core.gd:4280`（旁证、非权威）（在星镖**之前**，是攻击结算的第 0 步）。
                //      ⚠️ 「照常结算」= 挨了哨戒**不打断攻击**，攻击者就算被打死也照样把这一下打完
                //      —— 原版就是顺序执行、没有中断。别自作主张加「死了就取消攻击」。
                if (target.Has("sentry"))
                {
                    int se = target.KwValue("sentry");
                    int sd = Hurt(ctx, attacker, se, target.Name + " 的 Sentry");
                    ctx.Log($"Sentry {se}：{target.Name} 反击了正在攻击它的 {attacker.Name} {sd} 伤"
                          + $"（剩 {attacker.Health}）");
                }

                // ---- 星镖 X：**攻击伤害之前**先对目标追加 X 点（规则书 :207；原版 `:4285`）----
                //      目标被这 X 点打死就**跳过攻击伤害**（原版那支 `target_died`）
                if (attacker.Has("shuriken"))
                {
                    int sh = attacker.KwValue("shuriken");
                    int extra = Hurt(ctx, target, sh, attacker.Name + " 的 Shuriken");
                    ctx.Log($"Shuriken {sh}：{attacker.Name} 先对 {target.Name} 追加 {extra} 伤"
                          + $"（剩 {target.Health}）");
                    // ⚠️ 判据是「**血量**见底了」而不是「已经不在场上了」——
                    //    死亡处理延后到批末，此刻它还站在棋盘上（见 `DeferDeaths`）。
                    if (!target.IsAlive) targetDied = true;
                }

                hpBefore = target.Health;      // 践踏要算「溢出多少」，所以得记打之前那一下
                if (!targetDied)
                {
                    int dmg = atk;
                    // 标记光 X：目标带标记光时，**远程**攻击伤害 +X；受远程伤害后**移除全部**标记光
                    // （规则书 :192；原版 `:4296` 一带）
                    if (ranged && target.Has("markerlight"))
                    {
                        int ml = target.KwValue("markerlight");
                        dmg += ml;
                        // ⚠️ 规则书 :192 是「移除**全部**标记光」—— 用 `RemoveKeyword` 只会减一层
                        target.RemoveAll("markerlight");
                        ctx.Log($"{target.Name} 身上的 Markerlight {ml} 让这次远程伤害 +{ml}，标记光随后移除");
                    }
                    dealt = Hurt(ctx, target, dmg, attacker.Name, p);
                }
                ctx.Log($"{attacker.Name} {(ranged ? "远程" : "近战")}攻击 {target.Name}："
                      + $"{atk} 攻 → 实际 {dealt} 伤（{target.Name} 剩 {target.Health}）");

                // ⚠️ `targetDied` 只在星镖分支里被赋过值 —— 普通攻击打死的那一枪没有标记，
                //    而 Sniper 的判断依据正是它。这里补上：`dealt > 0` 是「真的打中了」
                //    （护盾全挡 = 0、无敌 = 0、打空 = 0），打中了且没血了就是摧毁。
                if (dealt > 0 && !target.IsAlive) targetDied = true;

                // 反击：目标用**近战攻击力**反击（不是远程）。只有两个来源能免：
                //   · Long Range：远程攻击不承受伤害（规则书 :191）
                //   · Sniper：「若**远程**攻击**会摧毁**目标：不承受反击伤害」（规则书 :209；原版 `:4312`）
                // ⚠️ 原版 `rule_core.gd:4310` 有一条修正记录：「此前『目标死则不反击』= 近战击杀免反（规则偏差）
                //    + Sniper 成死代码」—— 所以反击**不**因目标死亡而跳过，只能靠这两个关键词免。
                // ⚠️ 而规则书 `:145` 那个例子正是「被打死的初生者**照样反击** 1 点」——
                //    这条改动和它一致，别退回。
                bool noCounter = ranged && attacker.Has(KeywordTable.LongRange);
                bool sniperKill = ranged && attacker.Has("sniper") && targetDied;
                if (!noCounter && !sniperKill && counterAtk > 0)
                {
                    int back = Hurt(ctx, attacker, counterAtk, target.Name);
                    ctx.Log($"{target.Name} 反击 {attacker.Name}："
                          + $"{counterAtk} 攻 → 实际 {back} 伤（{attacker.Name} 剩 {attacker.Health}）");
                }
                else if (sniperKill)
                {
                    ctx.Log($"{attacker.Name} 的 Sniper：远程击杀 {target.Name} —— **不承受反击**");
                }
            }
            // ⬆️ 出批：**到这儿两边伤害才算完**，死亡触发（Backlash / 死亡监听器 / 离场）现在才跑 ——
            //    顺序就是 `:238` 那句「双方结算伤害 → 生命归 0 方触发效果」。
            // ⚠️ 别再在这里手工调 `CleanupDeaths`：`FlushDeaths` 已经处理过了，
            //    重复调虽然安全（第二遍 `u == null` 直接返回），但**猎杀标记**那段在函数最前面，
            //    会**结算两遍**（`AddPendingDeath` 的去重正是为这个加的，别再绕开它）。

            // ---- 践踏 Stomp：**溢出伤害**对目标**相邻随机一个**敌方单位造成 ----
            //      规则书 :213「攻击时，溢出伤害对目标相邻随机敌方单位造成」；
            //      我们自己的 `rule_core.gd:4372`（旁证、非权威） 的判据 + `_stomp_splash:4489` 的实现，逐条照抄：
            //        ① 目标**被打死**了（没死就谈不上「溢出」）
            //        ② `dealt > 打之前的血量`（护盾全挡 / 无敌时 dealt 是 0，不成立）
            //        ③ 候选 = 目标格**左右紧邻**那两格里的单位（**不排除督军** —— 原版没排除）
            //        ④ 候选里**随机**挑一个（走 `ctx.Rng`，同一局可复现；原版是 `randi()`）
            if (targetDied && attacker.Has("stomp") && dealt > hpBefore)
            {
                var cands = new List<int>();
                var adjSlots = new List<int>();
                BoardSpec.AdjacentSlots(tgtSlot, adjSlots);      // 🔴「谁算相邻」只此一处（`BoardSpec`）
                foreach (int adj in adjSlots)
                {
                    var au0 = ctx.Players[tgtP].Board[adj];
                    if (au0 != null && au0.IsAlive) cands.Add(adj);
                }
                if (cands.Count > 0)
                {
                    int pickSlot = cands[ctx.Rng.Next(cands.Count)];
                    var au = ctx.Players[tgtP].Board[pickSlot];
                    int excess = dealt - hpBefore;
                    int sd = Hurt(ctx, au, excess, attacker.Name + " 的 Stomp");
                    ctx.Log($"Stomp：{attacker.Name} 的 {excess} 点溢出伤害溅到相邻的 {au.Name}"
                          + $"（实际 {sd}，剩 {au.Health}）");
                }
            }

            // ---- 爆裂 X：攻击时对目标**相邻的敌方单位**造成 X 伤害（规则书 :170；原版 `:4348`）----
            //      ⚠️ 只溅射**部队**，不溅射督军（原版那儿写着 `au.is_warlord: continue`）
            // ⚠️ **另开一个批**（不是接着践踏那个）：同一次攻击里的两段衍生伤害是**先后**的
            //    （规则书把攻击段列成 …④践踏 ⑤爆裂），各自结算完自己的死亡。
            //    不分开的话，被践踏打死的那张**还站在棋盘上**（死亡延后了），
            //    爆裂会把伤害**打在尸体上** —— 而 `au.IsAlive` 那道守卫本来是挡这个的。
            using (SimultaneousDamage(ctx))
            {
                if (attacker.Has("blast"))
                {
                    int blast = attacker.KwValue("blast");
                    var adjSlots = new List<int>();
                    BoardSpec.AdjacentSlots(tgtSlot, adjSlots);  // 🔴「谁算相邻」只此一处（`BoardSpec`）
                    foreach (int adj in adjSlots)
                    {
                        var au = ctx.Players[tgtP].Board[adj];
                        if (au == null || au.IsWarlord || !au.IsAlive) continue;
                        int bd = Hurt(ctx, au, blast, attacker.Name + " 的 Blast");
                        ctx.Log($"Blast {blast}：溅射 {au.Name} {bd} 伤（剩 {au.Health}）");
                    }
                }
            }

            // ---- 震荡：**被本单位攻击的单位获得眩晕**（规则书 :177；原版 `:4363`）----
            //      原版只在**目标没死**时施加
            if (!targetDied && target.IsAlive && attacker.Has("concussion"))
            {
                // 🆕 同一件事的**另一个发生点**（`When an enemy receives a Stun, …`）：
                //    Concussion 造成的也是「被眩晕」，卡面分不出来 ⇒ 走**同一个**事件。
                //    ⚠️ 守卫是行为保持的：原来重复眩晕只是把 `true` 再赋一次，
                //       但广播不能重复（卡面写的是「收到**一次**眩晕」）。
                if (!target.IsStunned)
                {
                    target.IsStunned = true;
                    BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, target);
                }
                ctx.Log($"{target.Name} 被 {attacker.Name} 打晕了（Concussion）");
            }

            // ---- 攻击之后的触发：规则书 :208 / :214，都写着「（本单位存活时）」----
            // 存活判据要连**还在不在场上**一起看 —— 督军血 ≤ 0 时仍占着槽 4，但它已经不算活着了
            if (attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
            {
                // Slay（斩杀）：「攻击并**摧毁单位**后触发能力」。督军不是「被摧毁」，所以不算
                // 🆕 2026-09-13 A3：**这条判据现在两处共用** —— `Slay:` 关键词和卡面写法
                //    `When this unit kills an enemy, …`（`Sisters Repentia`）。
                //    合并是**照原版来的**，不是图省事：原版**只有一个**「击杀」时机
                //    —— `AbilityTrigger.Slay = 120`（`AbilityTrigger.cs:22`，全枚举里唯一与击杀相关的值；
                //    没有 slain / destroyedBy 之类的第二项）。投递走 `BattleActionType.triggerSlay = 89`
                //    （`BattleManager.cs:6477 AddTriggerSlay(targetCard, actingCard, …)` —— **击杀者是显式传参**），
                //    结算时判 `IsInPlayOrDying()`（`BattleManager__ResolveTriggerSlay.c:129`），
                //    对本卡自己发 0x78=120（`CardScript__TriggerSlay.c:17`）。
                //    ⇒ 「触发式关键词」与「`When` 监听器」在原版是**同一件事的两种写法**。
                bool killed = !target.IsWarlord && !target.IsAlive;
                if (killed)
                {
                    FireTriggerAt(ctx, attacker, KeywordTable.Slay, p, atkSlot);
                    // `subject` = **被击杀的那个**（正文里的 `it` 指它），
                    // `actor` = **凶手**（`this unit …` 的自指判据问的是它）—— 两个槽不能混，见 `WhenEvent`。
                    // `who` = 死者的阵营：极性（`kills an enemy` = 敌方）靠它判。
                    BroadcastWhen(ctx, WhenEventKind.Kills, tgtP, target.Card, target, actor: attacker);
                }

                // Strike（猛击）：「攻击后触发能力」—— 打没打死都算。放在斩杀之后：
                // 先结算「干掉了」这件更具体的事，再结算「攻击过了」这件泛化的事
                if (attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
                    FireTriggerAt(ctx, attacker, KeywordTable.Strike, p, atkSlot);

                // ---- 🆕 **「被这一下打到的那个」**（2026-09-14 A5 批 3）----
                //   卡面：`Destroy any troop attacked by this unit`（`Venomthrope`）·
                //   `Destroy any enemy troop with Armour attacked by this unit`（`Blastmaster Noise Marine`）·
                //   `Stun enemy troops attacked and give them -1 [armor] and -1 [attack]`（`Sonic Blaster`）·
                //   `Stun enemies attacked`（`Stikkbomb Boy`）· `Stun troops attacked.`（`Snakebite Grot`）·
                //   `Destroys any enemy troop with Hunt Mark attacked.`（`Arjac Rockfist`）—— **全池 6 张**。
                //
                //   **原版出处**：`AbilityTrigger.UnitAttack = 50`（`decomp_out/CardScript__ResolveUnitAttacked.c:30`
                //   传 `0x32`）—— 和 Slay / Strike / Mob / Regiment **在同一个函数里**（这也是
                //   「引擎既有的 Mob/Regiment 切分」被独立印证的地方）。所以触发点就排在这一族旁边。
                //   ⚠️ **位置（紧接 Strike 之后、Mob / Regiment 之前）是「我们挑的」**：那一支里
                //      这几条的先后在反编译里看不到（和上面 Mob 那条注释同一个理由）。
                //   ⚠️ **「伤害之前还是之后」有分歧，如实标着**：两路子代理独立取证给出了相反的顺序 ——
                //      EC 那路读 `CardScript__ResolveUnitAttacked` 判「伤害之后」；Leviathan 那路读
                //      `decomp_out2/BattleManager._ResolveAttack_d__438__MoveNext.c:991` 广播、
                //      `:1002` 才 `ReceiveDamage`，判「**伤害之前**」。**没有实况可判**（跑原版
                //      打不出这一族）。这里**沿用本块既有的近似**（Slay/Strike/Mob/Regiment 全都排在
                //      伤害之后），理由：改动面最小、且与本块其余五条**同一时点**，
                //      不会出现「同一支函数里两条排序不同」的新歧义。
                //   ⚠️ **`seed: target` = 这一下的被打者** —— 正文里的「那个被打的」全靠它
                //      （代词走 `LastTargets`，`AttackedBySelf` 也走那儿，见 `ResolveTargets`）。
                if (attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
                {
                    var atkOps = attacker.Card != null ? attacker.Card.AttackedOps : null;
                    if (atkOps != null)
                    {
                        if (ctx.EffectChain >= BattleContext.MaxEffectChain)
                        {
                            ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，"
                                  + $"{attacker.Name} 的「被这套打过的单位」不再连锁");
                        }
                        else
                        {
                            ctx.Emit(EvtKind.Trigger, p, atkSlot, attacker.Name,
                                     keyword: "attacked", effect: attacker.Card.AttackedText, amount: 0);
                            ctx.Log($"{attacker.Name} 触发「被这套打过的单位」："
                                  + $"「{attacker.Card.AttackedText}」");
                            ctx.EffectChain++;
                            ResolveOps(ctx, p, attacker, atkOps, "被这套打过的", seed: target);
                            ctx.EffectChain--;
                        }
                    }
                }

                // 群体（Mob）：「**近战**攻击后触发」（规则书 :193）—— **远程不算**，
                // 这是它和 Strike 唯一的差别（原版 `CardScript__ResolveUnitAttacked` 判
                // `param_4 == AttackTypes.Melee`）。Goff 一族 15 张卡用它。
                // ⚠️ **排在 Slay / Strike 之后是「我们挑的」**：原版这三个都长在同一个
                //    `ResolveUnitAttacked` 里，**先后顺序反编译里看不到**。取「更具体的先」——
                //    和 Slay 先于 Strike 是同一条理由。
                // ⚠️ **原版那一支 `BroadcastUnitMob`（通知除攻击者外的所有卡，触发 645）我们没有照抄**：
                //    实测听众只有一张，而它要的「再触发一次」原版**没有通用原语**。见 `CardDef.Mob` 的注释。
                //    ✅ 2026-09-16 起那张听众的行为**做掉了**（下面的 `TakeExtraTrigger` 那一段）。
                if (!ranged && attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
                {
                    FireTriggerAt(ctx, attacker, KeywordTable.Mob, p, atkSlot);
                    // 🆕 2026-09-16 **「再来一次」**（`GOF_Big_Choppa_Nob` 的
                    //   `When a friendly unit triggers Mob, it triggers an additional time`）——
                    //   额度是上面那次触发**广播时**由监听者挂到 `attacker` 身上的
                    //   （`EffectResolver.DoExtraTrigger`）⇒ 就地消费。
                    //   ⚠️ 守卫（`FireExtraTriggers` 里的 `ExtraTriggerDepth`）：那一次额外触发
                    //      会再广播一遍 `triggers:mob`，不挡的话额度会一直被重新挂上。
                    int mobExtra = TakeExtraTrigger(ctx, attacker, KeywordTable.Mob);
                    if (mobExtra > 0)
                        FireExtraTriggers(ctx, mobExtra, i =>
                        {
                            if (!attacker.IsAlive || ctx.Players[p].Board[atkSlot] != attacker) return;
                            ctx.Log($"（群体 Mob **再触发一次** —— 第 {i + 1} 次额外）");
                            FireTriggerAt(ctx, attacker, KeywordTable.Mob, p, atkSlot);
                        });
                }

                // 团（Regiment）：和 Mob **成对**，差别只在**远程**（规则书 `:202` vs `:193`）。
                // ⚠️ 规则书一条写「后」一条写「时」，我们没有能分辨的依据 ⇒ **两条同位置**（我们挑的）。
                // AstraMilitarum 一族 14 张用它。
                if (ranged && attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
                    FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot);
            }

            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        /// <summary>
        /// **这一下打上去实际掉多少血** —— **纯函数**：不改状态、不发事件、不打日志。
        ///
        /// 🔴 **伤害公式全仓只此一份**（<see cref="ApplyDamage"/> 也读它）—— 所以「这一下会不会打死」的
        ///    **预测**与**实际结算**不可能分叉（本工程红线：两处写同一条规则 = 迟早不一致）。
        ///    谁要用这个公式（目前：`EffectResolver.PreferKillable`），**读这里，别抄第二份**。
        ///
        /// 四道（顺序照原版，逐条出处见 <see cref="ApplyDamage"/> 的注释）：
        ///   护盾挡下 → 0 · 无敌 → 0 · **易伤** +X · **护甲** `max(1, …)`。
        ///
        /// ⚠️ **不含**星镖 / 哨戒 / 爆裂 / 践踏 —— 那些是同一个攻击里的**其它**伤害段。
        ///    判「能不能打死」时**故意取保守口径**：只算主伤害 ⇒
        ///    判「打得死」的一定打得死；判「打不死」的可能其实打得死（无害，只是退回原顺序）。
        /// </summary>
        public static int DamageAfterReduction(UnitState u, int dmg)
        {
            if (u == null) return 0;
            if (u.HasShield) return 0;                  // 盾挡下（并会碎）—— 所以「带盾的」判不出「打得死」
            if (u.Has("invulnerable")) return 0;        // 无敌完全免疫
            int actual = dmg;
            // **易伤 X**：受到伤害 **+X**（原版 `:4418`）。⚠️ 是加伤，别看成减伤
            if (u.Has("vulnerable")) actual += u.KwValue("vulnerable");
            if (u.Armor > 0) actual = Math.Max(1, actual - u.Armor);
            return actual;
        }

        /// <summary>
        /// 这一下会不会**摧毁**它 —— **纯预测**，不改任何状态。
        /// 判据只有一条：打完的剩血 ≤ 0（<see cref="UnitState.IsAlive"/> 就是 `Health &gt; 0`）。
        ///
        /// 消费者：`EffectResolver.PreferKillable` —— **强制攻击**卡面没写打谁时「优先挑打得死的」
        /// （**用户 2026-09-14 给的口径**：「没有这个说明，那么就是优先选择可以摧毁的单位」）。
        /// ⚠️ `destroyer`（毁灭者）的规则书原文 `:180`「**总是优先攻击可被摧毁的单位**」是同一句话，
        ///    但那条管的是**普通攻击**的目标挑选，**这一版没接**（要接就读这个函数，别再写第二份判据）。
        /// </summary>
        public static bool WouldKill(UnitState u, int dmg)
        {
            return u != null && u.IsAlive && u.Health - DamageAfterReduction(u, dmg) <= 0;
        }

        /// <summary>
        /// 通用伤害结算。（rule_core._damage_unit）
        ///
        /// **顺序照我们自己的 `rule_core.gd:4406`（旁证、非权威）的函数头写着**：
        ///   `Shield → Invulnerable → Vulnerable/护甲修正 → 扣血`
        ///   ① `Shield` 全挡（不受伤害）
        ///   ② `Invulnerable` **免疫伤害**（规则书 :190「无法被伤害或摧毁」）
        ///   ③ `Vulnerable X` **多加 X 点**（⚠️ 名字容易看反 —— 它是**加伤**，原版 `:4418`）
        ///   ④ `Armour X` 减免，**最低 1**（不是 0）—— 且原版注明这是**任何来源**的伤害都减（`:4420`）
        /// 返回实际扣血量。
        ///
        /// ⚠️ 会**造成伤害**的地方请用 <see cref="Hurt"/>，别直接调这个 ——
        ///    它少了「受伤触发」和「离场结算」两步，漏掉就会出现「血是负的但人还在场上」。
        /// </summary>
        public static int ApplyDamage(BattleContext ctx, UnitState u, int dmg, string source)
        {
            // ⚠️ 数值**一律走 `DamageAfterReduction`**（全仓唯一一份公式）——
            //    这一支只负责**副作用**：日志 / 事件 / 消费盾 / 翻伏击 / 扣血。
            //    2026-09-14：公式原来内联在这里，抽出去是为了让 `WouldKill` 的**预测**与实际**不可能分叉**。
            int actual = DamageAfterReduction(u, dmg);

            if (u.HasShield)
            {
                u.HasShield = false;
                ctx.Log($"{u.Name} 的 Shield 挡下了 {source} 的伤害");
                // 挡下也发事件（Amount = 0）：画面上「盾碎了」也要有反馈，
                // 而「掉血了」是另一回事 —— 旧表现层靠对比血量，这两件事根本分不开
                EmitHit(ctx, u, 0);
                return 0;
            }

            // **无敌**：免疫伤害（规则书 :190；原版 `_damage_unit:4414` 直接 return 0）。
            // 挡下也发事件（Amount = 0）—— 和 Shield 同理，表现层要能看到「打不动」
            if (u.Has("invulnerable"))
            {
                ctx.Log($"{u.Name} 有 Invulnerable —— {source} 的 {dmg} 点伤害被完全挡下");
                EmitHit(ctx, u, 0);
                return 0;
            }

            // ---- 🆕 伏击（`Ambush`）：**被伤害就翻开、而且那次的伏击效果作废**（规则书 `:166`）----
            // 「面朝下打出；**下次回合前若被伤害：翻开无效果**；若未被伤害：翻开并触发效果」
            // ⚠️ 位置在**所有减免之后**（盾挡下 / 无敌 / 护甲减到 0 都到不了这里）——
            //    「被伤害」按字面是**真掉血**，不是「被打了一下」。
            if (u.FaceDown)
            {
                u.FaceDown = false;
                ctx.Log($"{u.Name} 面朝下时挨了 {actual} 点伤害 → **翻开来，这次伏击效果没有了**");
            }

            u.Health -= actual;
            EmitHit(ctx, u, actual);
            // 🆕 2026-09-23 战果：算给**被打那个单位的对方**（「打自己人」因此不会被记成「对敌方伤害」）
            // ⚠️ `OwnerOf` **是 `RuleCore` 这个 partial 类里早就有的那一份**（定义在 `Core/EffectResolver.cs`），
            //    **不要再写第二份** —— `EffectResolver.cs` 那段的注释点名过「各写各的迟早不一致」。
            int _own = OwnerOf(ctx, u);
            if (_own == 0 || _own == 1) ctx.DamageToEnemy[1 - _own] += actual;
            return actual;
        }

        /// <summary>
        /// **会造成伤害的地方的唯一入口**：扣血 → 受伤触发（Penitence）→ 死了就离场（Death / Backlash）。
        /// </summary>
        /// <param name="killer">谁干的这一下（用于**猎杀标记** —— 它要把击杀者的督军治回来）。
        /// `-1` = 无来源（星辰镖 / 爆炸 / 反击…都不是「击杀者」）。</param>
        static int Hurt(BattleContext ctx, UnitState u, int amount, string source, int killer = -1)
        {
            // 已经死了的不再挨第二遍。
            // 什么时候会走到这：攻击者先被对方的**忏悔**打死了，回来还要结算反击 ——
            // 不给这一条的话，会往一个已经不在场上的单位发一条 Player/Slot 全是 -1 的 Hit 事件
            if (u == null || !u.IsAlive) return 0;

            int hpBefore = u.Health;          // 狂喜判「**降至**」要用「打之前」那一格，见下
            int dealt = ApplyDamage(ctx, u, amount, source);

            // Penitence（忏悔）：「受到伤害但未死亡时触发效果」—— 规则书 :196。
            // ⚠️ 只有**真掉血**才算受伤（Shield 全挡 = 没受伤，Armour 也只可能减到最低 1，不会变 0）
            // ⚠️ 2026-09-13：这里用的还是 **`u.IsAlive`（扣血之后、离场之前）** —— 和规则书 `:196`
            //    「受到伤害**但未死亡**」的字面一致。**不**因为它现在延后离场就放宽成「只要掉血就触发」：
            //    那样「被这一下打死」的也会触发 Penitence，是**多算了**。
            if (dealt > 0 && u.IsAlive) FireTriggerOnBoard(ctx, u, KeywordTable.Penitence);

            // ---- 狂喜 X（Ecstasy X）—— ✅ **2026-09-14 做掉**（用户点名要求）----
            // 规则书 `:182`「本单位生命降至 X 或以下未死亡时触发效果」。
            // 判据**照抄参考实现** `rule_core.gd:4438-4449`：
            //     `health > 0 and kw_has("ecstasy") and health <= kw_val("ecstasy")
            //      and not _ecstasy_fired` ⇒ 置位 + 触发。
            // 🔴 **是「首次越线、一辈子一次」**（那个 `_ecstasy_fired` 就是防重复的）——
            //    **不是**「只要 ≤ X 就每次挨打都触发」。上面那个 `hpBefore` 因此**用不上**
            //    （原注释猜的是「判跨越」，参考实现用的是**置位**，更简单也更不容易错）。
            //    我们对应的字段 = `UnitState.EcstasyFired`。
            // 🔴 阈值 X 的来源**只此一处**：`CardDef.EcstasyX`（正文正则 → 触发前缀 → 卡表兜底）。
            //    —— 原来这里标着「没做」，理由是「X 拿不到」；那条理由 2026-09-14 解掉了。
            if (u.IsAlive && u.Has(KeywordTable.Ecstasy) && !u.EcstasyFired)
            {
                int ex = u.Card != null ? u.Card.EcstasyX : 1;
                if (u.Health <= ex)
                {
                    u.EcstasyFired = true;
                    ctx.Log($"{u.Name} 的**狂喜 {ex}** 越过阈值（{hpBefore} → {u.Health}，阈值 {ex}）—— 触发");
                    FireTriggerOnBoard(ctx, u, KeywordTable.Ecstasy);
                }
            }

            // ---- 残忍（Cruelty）：**你的回合**、**敌方**单位受伤未死时，**己方**带该词的牌触发 ----
            // 规则书 `:178`「你的回合敌方单位受伤害未死亡时激活效果」。
            // 🔴 **触发方是挨打方的对面，不是挨打那个自己** —— 和 `Penitence` 是**两条**，别合并。
            if (dealt > 0 && u.IsAlive)
            {
                int hurtOwner = OwnerOf(ctx, u);
                if (hurtOwner >= 0 && hurtOwner != ctx.Active)
                    FireTriggerOnSide(ctx, ctx.Active, KeywordTable.Cruelty);
            }

            RemoveIfDead(ctx, u, killer);
            return dealt;
        }

        /// <summary>死了就从棋盘上拿掉（督军除外 —— 它留在槽 4，胜负交给 <see cref="CheckWinner"/>）</summary>
        static void RemoveIfDead(BattleContext ctx, UnitState u, int killer = -1)
        {
            int owner, slot;
            if (!FindUnit(ctx, u, out owner, out slot)) return;
            CleanupDeaths(ctx, owner, slot, killer);
        }

        /// <summary>
        /// 找这个单位在**谁的第几格**。不在场上返回 false（owner/slot 置 -1）。
        ///
        /// 为什么不给 `UnitState` 加个「我在第几格」的字段：格位是**棋盘的事**，单位状态是**单位的事**，
        /// 同一件事记两份早晚不同步 —— 这工程踩过一模一样的坑（见 `资料/规则引擎_进度与交接.md` 第五节）。
        /// 一局最多 18 个格子，扫一遍不值当省。
        /// </summary>
        static bool FindUnit(BattleContext ctx, UnitState u, out int owner, out int slot)
        {
            if (u != null)
            {
                for (int p = 0; p < 2; p++)
                {
                    var b = ctx.Players[p].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        if (b[s] != u) continue;
                        owner = p; slot = s; return true;
                    }
                }
            }
            owner = -1; slot = -1; return false;
        }

        /// <summary>按「单位 → 它在谁的第几格」发一条事件（不在场上就带 -1 的格位，表现层会跳过）。
        /// **返回它归谁**（`-1` = 不在场上）—— 调用方常要拿它再发一条，别再自己找一遍（判据只有一处）。</summary>
        static int EmitUnit(BattleContext ctx, EvtKind kind, UnitState u, int amount)
        {
            int owner, slot;
            FindUnit(ctx, u, out owner, out slot);
            ctx.Emit(kind, owner, slot, u != null ? u.Name : null, amount: amount);
            return owner;
        }

        /// <summary>
        /// 发一条「挨打了」的**双份**事件：表现层的 <see cref="EvtKind.Hit"/> ＋ 事件层的 `damaged` 广播。
        ///
        /// **为什么收在一处**：`ApplyDamage` 有三条出口（`Shield` 全挡 / `Invulnerable` 免疫 / 正常扣血），
        /// 三处各写一遍「发 Hit + 广播 damaged」迟早只剩一处是对的
        /// —— 和「判据要共用一份」是同一条规矩，只是对象从规则换成了**事件**。
        ///
        /// ⚠️ **被挡下也算「被打了一下」**（`amount == 0` 照样广播）：卡面写的是 `receives damage`，
        ///    而原版对「被盾挡下」也是当一次命中处理的 —— 见 `ApplyDamage` 里那两处
        ///    「挡下也发事件（Amount = 0）」的注释。口径与 `EvtKind.Hit` **完全一致**。
        ///
        /// ⚠️ **递归**：监听器的效果可能再造成伤害 ⇒ 再走这里。靠 `ctx.EffectChain`
        ///    （`BroadcastWhen` 里 `++`/`--`）截断，上限 `BattleContext.MaxEffectChain`。
        ///    `RuleEngineTest` 里有一条专门验它的用例（**别删**）。
        /// </summary>
        static void EmitHit(BattleContext ctx, UnitState u, int amount)
        {
            int owner = EmitUnit(ctx, EvtKind.Hit, u, amount);

            // 🆕 `When <单位> receives damage, …`（2026-09-13 第三十四轮）。
            // ⚠️ 在这之前 `WhenEventKind.Damaged` **一个广播点都没有** ——
            //    有 2 张卡收下了监听器，但一辈子不会响，而卡面照旧不打 `*`（静默失效）。
            if (u != null && u.Card != null)
                BroadcastWhen(ctx, WhenEventKind.Damaged, owner, u.Card, u);
        }

        /// <summary>
        /// **伏击（`Ambush`）翻开并触发**（2026-09-13 A2）—— 规则书 `:166`
        /// 「面朝下打出；下次回合前若被伤害：翻开无效果；**若未被伤害：翻开并触发效果**」。
        ///
        /// 调用点 = 控制者的**回合开始**（`BeginTurn`）：那一刻「下次回合前」这个窗口正好到期。
        /// 另一条出口（被伤害 → 翻开、**不触发**）在 `ApplyDamage` 里。
        /// ⚠️ 只扫**当前行动方**的场 —— 面朝下的单位是**打出者的**伏击，
        ///    窗口按**它的控制者的回合**算（对手的回合不算）。
        /// </summary>
        static void RevealAmbush(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];
            var slots = new List<int>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                if (u != null && u.FaceDown && u.Card != null && u.Card.Has(KeywordTable.Ambush))
                    slots.Add(s);
            }
            foreach (int s in slots)
            {
                var u = ps.Board[s];
                if (u == null || !u.FaceDown) continue;
                u.FaceDown = false;
                ctx.Log($"{u.Name} 面朝下撑了整整一轮 → **翻开，伏击效果触发**");
                FireTriggerAt(ctx, u, KeywordTable.Ambush, p, s);
            }
        }

        /// <summary>
        /// **回合结束时摧毁某一方的残骸**（`Remnant`，2026-09-13 A2）——
        /// 规则书 `:203`「残骸受伤害**或控制者回合结束时**被摧毁」。
        ///
        /// 🔴 **2026-09-25 更正：只有【死灵的】残骸会在回合结束时消失，【灵族的】不会。**
        ///    出处 = `d:/2/tools/decomp_full/SupportMethods__ShouldRemnantDestroyOnTurnEnd.c`（**逐行读过**）：
        ///      `faction == 0x28 (Necrons/Sautekh)` **且** `isRemnant` **且没有** `notDestroyRemnant(1190)`
        ///    ⇒ 灵族那种（路标石留下的灵魂石）**一直留在场上**，等人来点它收集 ——
        ///    这正是规则书 `:210` 那句「**控制者回合可收集**」能成立的前提。
        ///    ⚠️ 我们这边判据写的是 **`Has(KeywordTable.Remnant)`** 而不是查阵营：
        ///    全卡池里 `Remnant.` **只出现在 Sautekh（36 张）**、`Waystone.` **只出现在 SaimHann（24 张）**
        ///    （2026-09-25 实测）⇒ 两者等价，而写关键词比写阵营更抗「以后加卡」。
        ///
        /// ⚠️ **位置是我们挑的**：排在「本回合限时增益到期」与临时卡清扫之后、
        ///    `ResolveAtTurn("turn_end")` **之前** —— 规则书**没写**它和「回合结束触发效果」谁先，
        ///    如实标着。放前面意味着：**回合结束时才翻回来的残骸**，下个回合结束还会再被摧毁。
        /// </summary>
        static void DestroyRemnants(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];
            var slots = new List<int>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                // ⚠️ 那半句 `Has(Remnant)` 是**必须的**（2026-09-25 加）：没有它，
                //    灵族的灵魂石会被这里扫掉，玩家永远没机会点它收集。
                if (u != null && u.IsRemnant && u.Has(KeywordTable.Remnant)) slots.Add(s);
            }
            if (slots.Count == 0) return;
            foreach (int s in slots)
            {
                var u = ps.Board[s];
                if (u == null || !u.IsRemnant) continue;
                // 🆕 A7：`Adjacent Remnants do not disappear at the end of your turn`
                //    （`Nemesor Zahndrekh`）—— 被光环罩住的那些**留下**。
                //    标记由 `Auras.Recompose` 置（相邻格 + 来源在场），这里只管读。
                if (u.AuraRemnantStay)
                {
                    ctx.Log($"{u.Name} 的残骸**留场**（{ps.Name} 的 `Nemesor Zahndrekh` 罩着它）");
                    continue;
                }
                u.Health = 0;                       // 走正常的「被摧毁」那条路（进弃牌堆）
                CleanupDeaths(ctx, p, s);
            }
        }

        /// <summary>
        /// 生命归零的单位离场。督军不离场（留在槽 4），胜负交给 CheckWinner
        /// </summary>
        /// <param name="killer">击杀方（0/1）。`-1` = 无来源。**猎杀标记要用它**。</param>
        static void CleanupDeaths(BattleContext ctx, int p, int slot, int killer = -1)
        {
            if (!BoardSpec.IsValid(slot)) return;

            var ps = ctx.Players[p];
            var u = ps.Board[slot];
            if (u == null || u.IsAlive) return;

            // ---- 🆕 在「同时伤害」批里 ⇒ **只入队，不处理** ----
            // 规则书 `:145`「伤害同时结算」+ `:238`「攻击 → **双方结算伤害** → 生命归 0 方触发效果」。
            // ⚠️ **督军例外 —— 立刻处理**：督军倒下直接判负（`CheckWinner`），
            //    延后的话「双方都倒」要等到批末才知道谁赢，而中间那几行效果已经按「还没分出胜负」
            //    结算过了。原版规则书也没写平局判定，**不拿这个去赌**（如实标着：这一条是我们定的）。
            if (ctx.DeathsDeferred && !u.IsWarlord)
            {
                ctx.AddPendingDeath(p, slot, killer);
                return;
            }
            // ---- 猎杀标记：**带标记的敌方部队被摧毁时** ----
            //   「对敌方督军造成伤害并治疗我方督军，数值 = 其标记数」（规则书 :189；我们自己的 `rule_core.gd:4562`（旁证、非权威））
            //   ⚠️ 三个细节照原版：
            //     ① 督军**也算** —— 原版只排除了「被摧毁的这一个是督军」，清理标记的那段没有排除督军；
            //     ② 治疗的是**击杀者的督军**，不是击杀者本人；
            //     ③ 只有**敌方**单位会被打上标记（卡面都写 `give Hunt Mark to an enemy troop`），
            //        所以这里再判一次 killer != p，免得自己人误伤时触发。
            if (u.Has("huntmark") && killer >= 0 && killer != p)
            {
                int marks = u.KwValue("huntmark");
                var enemyW = ctx.Players[p].Warlord;          // 标记单位的**主人**的督军 → 挨打
                var killerW = ctx.Players[killer].Warlord;    // 击杀者的督军 → 回血
                if (enemyW != null)
                {
                    // ⚠️ **不要再补一条 `EmitUnit(EvtKind.Hit)`** —— `ApplyDamage` 自己已经发过了
                    //    （2026-09-13 第三十四轮修）。原来这里多发的那一条让一次伤害产生**两条 Hit**
                    //    ⇒ 表现层重复飘字、重复播命中特效。而它**不报错、断言也看不见**
                    //    （断言只数「有没有 Hit」，不数「几条」），属于本工程红线里的「静默错打」。
                    int dmg = ApplyDamage(ctx, enemyW, marks, "Hunt Mark");
                    ctx.Log($"Hunt Mark {marks}：{ps.Name} 的督军 {enemyW.Name} 挨 {dmg} 伤"
                          + $"（剩 {enemyW.Health}）");
                }
                if (killerW != null)
                {
                    int before = killerW.Health;
                    killerW.Health = System.Math.Min(killerW.Health + marks, killerW.MaxHealth);
                    int healed = killerW.Health - before;
                    if (healed > 0) EmitUnit(ctx, EvtKind.Hit, killerW, -healed);
                    ctx.Log($"Hunt Mark {marks}：{ctx.Players[killer].Name} 的督军 {killerW.Name}"
                          + $" 回 {healed} 血（{before} → {killerW.Health}）");
                }
                CheckWinner(ctx);
            }


            if (u.IsWarlord)
            {
                ctx.Log($"{ps.Name} 的督军 {u.Name} 倒下");
                ctx.Emit(EvtKind.Death, p, slot, u.Name);
                return;
            }

            // ---- 残骸**被摧毁**（`Remnant`，2026-09-13 A2）----
            // 规则书 `:203`「残骸**受伤害**或控制者回合结束时**被摧毁**」。
            // 这一刻**原来那张卡才进弃牌堆**（它死在格位上一次，被摧毁时再走一次流程）。
            // ⚠️ **不再触发** `Backlash` / `Unstable` / 死亡监听器 —— 残骸是**一张背面朝上的牌**，
            //    没有任何能力；那些触发在它「死」的那一次已经结算过了（见下面那一段的注释）。
            if (u.IsRemnant)
            {
                // 🔴 **2026-10-01：走 `RemoveAt`（会补位）** —— 残骸被摧毁 = 它离开那条连续列表，
                //    外侧的单位整体内移一格（原版 `List.Remove` + `RefreshOccupationSlots`）。
                BoardSlots.RemoveAt(ps, slot);
                ps.Discard.Add(u.Instance);      // 第 7 行第 2 步：进弃牌堆的是**那一份**（残骸本来就是它）
                ctx.DeadUnits.Add(new DeadUnit { Card = u.Card, Owner = p, DeathTurn = ctx.Turn });
                ctx.Emit(EvtKind.Death, p, slot, u.Name);
                ctx.Log($"残骸 {u.Name} 被摧毁，那张卡进弃牌堆");
                // 🔴 **A7 光环重算 —— 这里必须自己调一次**（2026-09-19 补，由
                //    `资料/普查产出_0919/光环写入点_钩子对账.md` 逐处对账查出来的唯一一个洞）。
                //    这一支**从 `:2035` 直接 `return`**，跳过了方法末尾 `:2147` 那次兜底重算
                //    ⇒ 棋盘**改了**（残骸离场）光环却没重算，`Recompose` 是「整份摘掉再重加」
                //    才有「来源离场就收回」的语义 ⇒ 症状是**残骸自己的光环继续挂在邻格上**，
                //    直到下一次重算（`BeginTurn:512`）为止。
                //    可复现的例子：`SAU68 Damaged Plasmacyte`（`Remnant` +
                //    `Adjacent units have Regeneration 2`）—— 它被摧毁之后，邻格仍在
                //    `EndTurn` 的再生段（`RuleEngine/Core/RuleCore.cs` 的 `EndTurn` 里那句 `u.KwValue("regeneration")` 读 `KwValue("regeneration")`）里回血，
                //    也就是「一张躺在弃牌堆里的牌还在给邻居回血」。
                //    ⚠️ 这一支**故意不跑** `Backlash` / `Unstable` / 死亡监听器（残骸没有任何能力，
                //       见上面那段注释）—— 那几样与本行无关，别顺手补。
                Auras.Recompose(ctx);
                return;
            }

            // ---- 🆕 残骸（`Remnant` / `Waystone`）：**本部队死亡时在原位变成残骸** ----
            // 「残骸**受伤害或控制者回合结束时被摧毁**」⇒ 它**留在格位上**，不是进弃牌堆。
            // 原版出处：残骸在场上是一个独立的 3D 体（`BattleCardUI.CreateRemnantBody` /
            // `RemnantBody3D` + `BattleManager.AddTransformIntoRemnant`），
            // 而且 `GetEnemyMinionsAndRemnantInPlay` 说明它**算「场上」**（能被选中、能挨打）。
            //
            // 🔴 **2026-09-25 更正 —— 触发条件不止 `Remnant`，`Waystone`（灵族的）**走同一条路**。**
            //    出处 = `d:/2/tools/decomp_full/SupportMethods__HasToTransformIntoRemnant.c`（**逐行读过**）：
            //        `HasCurrentTrait(card, 0x41a = 1050 = remnant) || HasCurrentTrait(card, 0x474 = 1140 = waystone)`
            //    被调的地方正是死亡路径（`BattleManager__ResolveDestroyUnit` / `CardScript__CheckIfDead` /
            //    `…HasEnoughPendingDamageAndHasToTransformIntoRemnant` / `…ResolveBacklash`）
            //    ⇒ **「灵族的灵魂石」和「死灵的残骸」是同一套底座的两个阵营皮肤**，
            //      区别只在**残骸体长什么样**（灵族 = 漂浮的灵魂石，死灵 = 碎裂的卡）
            //      和**谁能把它变回去**：死灵走 `reanimate`（效果动词），灵族走**玩家点击收集**。
            //    改动前我们是「路标石一死就直接 +1 灵魂石」，**那是简化**（见下面那段已删的注释）。
            //
            // ⚠️ **残骸自己再被摧毁时走另一条路**（上面那个 `if (u.IsRemnant)`）——
            //    不加那道守卫就会「残骸死了又变残骸」，永远赖在场上。
            // ⚠️ 变成残骸**不影响**下面那几段死亡触发（不稳定 / 反噬）：
            //    那些是「**这张卡**死的时候」的事，现在正发生在这一刻。
            // 🔴 2026-10-17：「进弃牌堆 / 阵亡登记」两笔账下移到反噬之后 ⇒ 用一个局部标记
            //    记住「这一支是**真的离场**（要记账）」，还是「翻面成残骸（仍在格位上、不记账）」。
            bool leftPlay = false;
            if ((u.Has(KeywordTable.Remnant) || u.Has(KeywordTable.Waystone)) && !u.IsRemnant)
            {
                // 🔴 第 7 行第 1 步：**沿用同一个实例** —— 翻面成残骸**没有换牌**，
                //    所以从残骸再被摧毁时，进弃牌堆的还得是原来那一份（第 2 步之后才有意义）。
                var rem = new UnitState(u.Instance, false)
                {
                    IsRemnant = true,
                    Attack = 0, RangedAttack = 0,
                    Health = 1, MaxHealth = 1,          // 挨任何一下就没（规则书：受伤害即被摧毁）
                    Exhausted = true,                   // 残骸不能行动
                };
                ps.Board[slot] = rem;
                ctx.Log($"{ps.Name} 的 {u.Name} 阵亡 → **翻面成残骸**（留在 {slot} 号格；"
                      + "受伤害、或你的回合结束时被摧毁）");
                // 🆕 2026-09-16：**「变为残骸」的事件广播**（全仓唯一产生点就在上面这三行）。
                // 卡面只有 `SAU61 Undying Legions` 在等它（`when a friendly troop becomes a Remnant
                // it gains Shield`，中文「每当友方部队变为残骸时，其获得护盾」）。
                // ⚠️ **这是我们补的、不是照抄原版**：2026-09-16 子代理把反编译翻了一遍 ——
                //    `CardScript.TransformIntoRemnant`（桩 `CardScript.cs:2580`，体
                //    `decomp_out/CardScript__TransformIntoRemnant.c`）**一条广播都不发**，
                //    `BattleManagerSupport` 的 43 个 `Broadcast*` 里**没有任何 remnant 条目**
                //    （唯一沾边的 `BroadcastUnitReanimated` 是「再造」，不是「变成残骸」）。
                //    ⇒ 原版这张卡靠的是通用重估（`BroadcastWhileInPlay`），我们没有那套；
                //      这一条按「监听器语义」补，**记成我们挑的**。
                // ⚠️ 广播排在 `ctx.Log` 之后、`Death` 广播（下面那行）**之前** ——
                //    监听方看到的是「已经翻面了」这个事实：正文 `it gains Shield` 加的那个 `it`
                //    必须是**残骸**（`u` 已经被换掉了，传新的 `rem`，别传 `u`）。
                BroadcastWhen(ctx, WhenEventKind.BecomesRemnant, p, u.Card, rem);
            }
            else
            {
                // 🔴 **2026-10-01：阵亡 = 离开连续列表 ⇒ 外侧的单位整体内移一格**（原版
                //    `_ResolveMinionDeath` → `MinionManager.RemoveMinion`（`List.Remove`）
                //    → `AddReassembleMinionsOrder` → `ReassembleMinions` 把它们摆回各自的下标）。
                //    ⚠️ **残骸那一支不算阵亡**（上面那个 `if`）：它只是翻了个面、**还在列表里占着那一格**。
                BoardSlots.RemoveAt(ps, slot);
                // ⚠️ **这一条【没有】跟着下移**（刻意的，别顺手挪）：`DiedThisTurn` 是
                //    `For each one that dies this turn …` 用的**回合计数**，不是那两张登记表；
                //    它和旧行为逐字一致（本批只动 `Discard` / `DeadUnits` 两笔账）。
                //    还没查清的那一格：原版那个计数的写入点在哪 —— **没查清**（如实标着）。
                ctx.DiedThisTurn++;      // `For each one that dies …` 按它计数（回合开始清零）
                // 🔴 **2026-10-17 下移**：「进弃牌堆」+「阵亡登记」两笔账**挪到反噬之后**
                //    （本方法末尾那一大段，逐跳判据写在那边）。**别挪回来** ——
                //    原版进墓地是 `CardScript.UnitDeath` 协程的**最后一跳**，反噬在前面。
                leftPlay = true;
            }
            // 先发 Death 再结算反噬：表现层要**趁格位还有意义的时候**播阵亡特效
            ctx.Emit(EvtKind.Death, p, slot, u.Name);

            // **事件层广播**（`When a friendly troop dies, …` / `When an enemy dies, …`）—— 第三十二轮。
            // ⚠️ **排在 `Backlash` 之前**，和「先发 Death 再结算反噬」是同一条理由：
            //    监听方看到的是「死了」这个事实，而不是「死者的反噬打完了」。
            // ⚠️ `who = p`（**死者的阵营**）—— 极性判据（friendly / enemy）在
            //    `WhenEvents.Matches` 里拿它和监听者的阵营比。给错就等于整档反着触发。
            BroadcastWhen(ctx, WhenEventKind.Die, p, u.Card, u);

            // 🆕 2026-09-16 「它本回合内死了就把这条效果转给另一个」（`BL77 Spreading Corruption`）——
            //    登记在 `EffectResolver.DoGive`，这里消费。**排在死亡广播之后**：
            //    「它死了」这件事先让监听器们看见，转移是紧接着的**追加**效果（我们挑的先后）。
            FlushDeathWatches(ctx, u, p);

            // ---- 路标石 → 灵魂石 ----
            // ✅ **2026-09-25：这段「一死就直接 +1」的简化已删掉，改成原版的两段式。**
            //    原版：**死亡 → 留在原格成为残骸体（= 一枚可收集的灵魂石）→ 玩家点它才 +1**
            //    （`PlayerActions.clickWaystone = 6` → `BattleActionType.useWaystone = 76` → 协程
            //     `ResolveUseWaystone`：`AddSpiritStoneMana(收集方, waystoneGiveMana=1)` → `DestroyUnit`）。
            //    产残骸那半在上面那段 `if`（`Waystone` 已并入条件）；**收集那半 = `CollectWaystone`**。
            //    ⚠️ 原来那句「先让灵魂石这条链能跑通」的权宜话**不要了** —— 两段式已经做出来了，
            //    而且「一死就 +1」会让那 24 张卡的**1 血残骸体 / 能被对手打掉**这些机制整个消失
            //    （那是**明显比原版强**的改动，属静默失真）。判据 → `资料/查证_useWaystone_语义.md` §六。

            // Unstable（不稳定）：「**本单位死亡时：对随机单位造成 1-3 伤害**」—— 规则书 `:221`。
            // ⚠️ **排在 Backlash 之前** —— 我们自己的 `rule_core.gd:4529`（旁证、非权威）就是这个顺序（先自爆、再反噬）。
            // ⚠️ 那道 `ctx.EffectChain < MaxEffectChain` 守卫是**连锁保护**
            //    （自爆打死别人 → 那人也自爆 → …），我们自己的 `rule_core` 用的是 `depth < 6`。**别删**。
            if (u.Has(KeywordTable.Unstable) && ctx.EffectChain < BattleContext.MaxEffectChain)
                UnstableBlast(ctx);

            // Backlash（反噬）：「单位死亡时触发效果」—— 规则书 :169（「被摧毁时的触发效果立即结算」）。
            // ⚠️ 单位**已经不在棋盘上了**，所以得把格位显式传进去 ——
            //    它既决定特效播在哪，也是「这张卡死在哪」的唯一记录
            FireTriggerAt(ctx, u, KeywordTable.Backlash, p, slot);

            // ---- 🔴 2026-10-17：**「进弃牌堆」+「阵亡登记」两笔账下移到反噬之后** ----
            // **判据 = 原版全量反编译 `d:/2/tools/decomp_full/` 的逐跳方法体**（不是我们自己的
            // `rule_core.gd`）：
            //   ① `CardScript__CheckIfDead.c:124` 生命归零 → `state(0x228) = 5`（濒死）；
            //      `:135`（「变残骸」那一支是 `:141`）当场 `TriggerUnitBacklashActions`
            //      —— **反噬在这一跳**。⚠️ 「从棋盘移除」不在这条链上：它在**另一条协程**
            //      `BattleManager._ResolveMinionDeath_d__454__MoveNext.c:55`（`MinionManager.RemoveMinion`
            //      + `List.Remove`）里 —— 也就是说原版**反噬入队比从棋盘移除还早**
            //      （`ResolveMinionDeath` 在 dump 里查不到调用点 ⇒ 它和 `ResolveBacklash` 一样
            //       只能靠协程入口触发，**这一格没查清**；我们这台机器上「先移除、再反噬」保持现状）。
            //   ② `CardScript__TriggerUnitBacklashActions.c:99` 把反噬包成一条 action
            //      `BattleManager.AddAutoActionToQueue` **入队** ⇒ 它自己一次都不碰墓地。
            //   ③ 死亡触发那一族（`CardScript__ResolveDeadCard.c`：`TriggerOnMinionDeath` /
            //      `RemoveExtrinsicEffectsFrom` / `BroadcastUnitRequiem` / `OnTrigger(0x1ae)`）
            //      里**一次 `Cemetery` 都没有**。
            //   ④ **`BattleManager__AddToCemetery.c` 在全量反编译里只有一个调用者** ——
            //      `CardScript__GoToCemetery.c:17`（全库 grep `AddToCemetery` 只有 2 个文件命中，
            //      另一个是它自己）。而 `GoToCemetery` 在单位死亡链上**只在
            //      `CardScript._UnitDeath_d__446__MoveNext.c:152` 被调用** ——
            //      那是 `UnitDeath` 协程的**最后一跳**（state 2；之前已经 `WaitForSeconds` 两跳：
            //      `:126` 死亡音效/抖动那一拍、`:383` 死亡演出那一拍）。
            //   ⑤ 真身 `CemeteryManager__AddCardToCemetery.c:17-47` 才做 `List.Add` + 换父 +
            //      `SetAsCemetery` + `SetActive(false)` —— **那才是「进弃牌堆」**。
            // ⇒ **反噬结算时它一定还没进弃牌堆、也还没进阵亡表**。我们原来是「先记账、后反噬」，
            //    **顺序正好反了**（2026-10-17 改）。⚠️ 与之配套：`DeadUnits` 与 `Discard`
            //    **仍要同时写**（`BattleContext.TakeFromGraveyard` 一条规则只写一处）。
            //
            // ⚠️ **原版在进墓地那一跳之前还会「复核一次」**：`CardScript._UnitDeath_d__446__MoveNext.c:63`
            //    判 `state(0x228) != 5` 就 `CustomDebug.LogWarning` + `return 0` —— **压根不调
            //    `GoToCemetery`**。⇒ **中途被别人捞走的单位不进墓地**（同一张卡被挪回手牌时，
            //    原版那张 `CardScript` 的状态已经变了）。
            //    我们的等价复核 = 「那一份实例已经不在『等待入墓』的状态」（回到手牌了、
            //    或已经被别处登记过）⇒ 跳过登记，否则同一张卡会**既在手牌又在弃牌堆**。
            bool refiled = ps.Hand.Contains(u.Instance)      // 被 `Backlash: Return(s) to your hand`
                           || ps.Discard.Contains(u.Instance);  // 捞回去了/`EnforceHandLimit` 又弃掉
            if (leftPlay && !refiled)
            {
                ps.Discard.Add(u.Instance);      // 第 7 行第 2 步：**那一份**回弃牌堆（原来放的是模板）
                // 虫群合并时**压在下面**的那些牌一起进弃牌堆（2026-09-13 A2）——
                // 物理上就是「宿主死了，下面压着的一起走」
                if (u.SwarmUnder.Count > 0)
                {
                    foreach (var under in u.SwarmUnder) ps.Discard.Add(under);
                    ctx.Log($"（{u.Name} 下面压着的 {u.SwarmUnder.Count} 张一起进弃牌堆）");
                    u.SwarmUnder.Clear();
                }
                // 阵亡登记（`Choose a … that died this game / this battle / since your last turn`
                // 的候选来源）—— 与 `Discard` **同时**写，取走时也**同时**移除
                // （`BattleContext.TakeFromGraveyard`，一条规则只写一处）。
                // 督军在上面那条 `if (u.IsWarlord) … return` 里已经返回了，**不会**进这张表。
                ctx.DeadUnits.Add(new DeadUnit { Card = u.Card, Owner = p, DeathTurn = ctx.Turn });
                ctx.Log($"{ps.Name} 的 {u.Name} 阵亡，进弃牌堆");
            }
            else if (leftPlay)
            {
                // 不许静默：翻了这一支一定要说出来，否则「弃牌堆里少一张」查不出原因
                ctx.Log($"{ps.Name} 的 {u.Name} 在入墓前被捞走（反噬把它收回手牌 / 别处已登记）"
                      + " ⇒ **不进弃牌堆**（原版在进墓地前也复核一次，见上面那条）");
            }

            // 🆕 A7：**光环重算**（放在这里 = 死亡那一整套触发都跑完之后）。
            //    ⚠️ 上面那几段（路标石 / 不稳定 / 反噬）**自己也可能改棋盘**（自爆打死别人、
            //    反噬把人收回手牌）—— 那些路径各自有钩子；这里这一次是兜「死者**本人**离场」。
            Auras.Recompose(ctx);
        }

        /// <summary>
        /// **处理攒下的死亡** —— 「同时伤害」批结束时调（见 `BattleContext.DeferDeaths` 那一大段注释）。
        ///
        /// **规则依据**：规则书 `:145`「伤害**同时结算**」+ `:238`
        /// 「序列：攻击 → **双方结算伤害** → 生命归 0 方触发效果 → 摧毁方触发效果」。
        /// ⇒ 两边伤害都算完之后，才轮到「谁死了」这件事。**这个函数就是那一步。**
        ///
        /// **出队顺序**（这是本函数唯一真正需要判断的地方）：
        ///   规则书 `:238` 只说「**被攻击方优先**」，没说同一方多人同时死怎么排。
        ///   ⇒ 照我们自己的 `rule_core.gd`（旁证、非权威）的确定性口径：**按 (属于哪一方, 格号) 升序**，不随机。
        ///   ⚠️ 对局的**可复现性**靠它（工程铁律：只用 `System.Random(seed)`、定死的规则不许改成随机）。
        ///
        /// ⚠️ **批会嵌套**：这个函数只处理「出批的那一层」攒下的死亡。
        ///    被处理的那几个单位自己的 Backlash / 死亡监听器**又可能打死人** ——
        ///    那些发生在**批外**（`_deferDeaths` 已经减回 0），所以由 `Hurt` 当场处理掉，
        ///    正是我们要的「效果按序列触发，与伤害不同」（规则书 `:238`）。
        ///    实现上就是**先 `ReleaseDeferDeaths()`、再逐个处理** —— 顺序反了会变成「无限延后」。
        /// </summary>
        static void FlushDeaths(BattleContext ctx)
        {
            ctx.ReleaseDeferDeaths();
            if (ctx.DeathsDeferred) return;      // 还在外层批里 ⇒ 死亡继续攒着，由外层收尾

            var pending = ctx.TakePendingDeaths();
            if (pending.Count == 0) return;

            pending.Sort((a, b) =>
            {
                if (a.Player != b.Player) return a.Player.CompareTo(b.Player);
                return a.SlotAtDeath.CompareTo(b.SlotAtDeath);   // 按**它死的那一刻**的格号（出队顺序没变）
            });
            foreach (var pd in pending)
            {
                // 🔴 **2026-10-01：按身份现查它现在在哪一格** —— 连续棋盘下，前面那条死亡会让外侧的人
                //    整体内移一格 ⇒ 记下来的 `SlotAtDeath` 到这一刻**可能已经指到别人身上**（甚至指空）。
                //    （旧代码直接 `CleanupDeaths(ctx, pd[0], pd[1], pd[2])`，同一批死两个时第二个会被静默漏掉。）
                int nowP, nowSlot;
                if (!FindSlot(ctx, pd.Unit, out nowP, out nowSlot)) continue;   // 已经不在场上了 ⇒ 跳过
                CleanupDeaths(ctx, nowP, nowSlot, pd.Killer);
            }
        }

        /// <summary>
        /// **「同时伤害」批的作用域** —— `using` 一包就对了：
        /// <c>using (RuleCore.SimultaneousDamage(ctx)) { 打一下; 再反手打一下; }</c>
        ///
        /// 为什么要有这个包装：`DeferDeaths` / `FlushDeaths` 必须**成对**，而中间那段又一定会
        /// `return`（`Hurt` 的返回值要用来判践踏/狙击）。手写 `try/finally` 迟早有人漏一处 ——
        /// 漏了的后果是**整局剩下的死亡全部延后、再也没人处理**（静默，极难查）。
        /// 用 `IDisposable` 把「成对」这件事交给编译器。
        /// </summary>
        public struct DeathBatch : System.IDisposable
        {
            readonly BattleContext _ctx;
            internal DeathBatch(BattleContext ctx) { _ctx = ctx; _ctx.DeferDeaths(); }
            public void Dispose() { FlushDeaths(_ctx); }
        }

        /// <summary>开一个「同时伤害」批，见 <see cref="DeathBatch"/>。</summary>
        public static DeathBatch SimultaneousDamage(BattleContext ctx) { return new DeathBatch(ctx); }

        // ==================================================================
        //  技能与触发（2026-09-12 增补）
        //
        //  两类东西，共用同一套「效果」结算：
        //    · **主动技能**（`Ability:`）—— 玩家/AI 主动放，花掉本单位一次行动
        //    · **触发效果**（Rally / Strike / Slay / Backlash / Penitence）—— 时机到了自动放
        //  两者都会往 `ctx.Signals` 发一条事件，表现层据此播那两类特效。
        // ==================================================================

        /// <summary>
        /// **取走**「某个机制的触发再发生几次」的额度（取走即清零）—— 🆕 2026-09-16。
        ///
        /// 🔴 **只此一处消费**；挂额度在 `EffectResolver.DoExtraTrigger`（监听者在广播里挂给事件主语）。
        /// 两个消费点：`DeclareAttack` 的 Mob 那一段 · `RepeatTacticOnAdjacent` 的 Synapse 那一段。
        /// 卡面（全池两句）：`GOF_Big_Choppa_Nob` 的 `it triggers an additional time` ·
        /// `TL30 Broodlord` 的 `it applies the effect twice`。
        /// </summary>
        public static int TakeExtraTrigger(BattleContext ctx, UnitState u, string keyword)
        {
            if (ctx == null || u == null || string.IsNullOrEmpty(keyword)) return 0;
            int n;
            if (!u.ExtraTriggers.TryGetValue(keyword, out n) || n <= 0) return 0;
            u.ExtraTriggers.Remove(keyword);
            return n;
        }

        /// <summary>
        /// 跑 N 次「再触发一次」—— 全程把 `ctx.ExtraTriggerDepth` 顶起来（**重入守卫**）。
        ///
        /// 为什么必须有：那一次额外触发**自己会再广播一遍同一件事** ⇒ 监听者会**再挂一次额度**
        /// ⇒ 下一次攻击白捡一次（静默、看不出来）。守卫让 `DoExtraTrigger` 在我们跑的这段时间里
        /// 只记日志、不挂账。
        /// ⚠️ `fire` 里请自己判「那个单位还在不在场上」——额外触发可能把人打死/打飞。
        /// </summary>
        public static void FireExtraTriggers(BattleContext ctx, int n, System.Action<int> fire)
        {
            if (ctx == null || n <= 0 || fire == null) return;
            ctx.ExtraTriggerDepth++;
            try
            {
                for (int i = 0; i < n; i++)
                {
                    if (ctx.IsOver) break;
                    fire(i);
                }
            }
            finally { ctx.ExtraTriggerDepth--; }
        }

        /// <summary>
        /// **手牌加成兑现** —— 那张牌真打出来时，把它在手牌上攒着的那份效果加上去
        /// （🆕 2026-09-16，`TL53 Infinite Biomorphologies` 的「给手牌里的部队」）。
        ///
        /// 🔴 **2026-10-18（`A885`）**：效果挂在**那一份牌自己**身上了（`inst.HandBuffOps` 那一族，
        ///    = 原版 `CardScript +0x108` 的对应位），这里**不再**去对局级的表里按
        ///    `ReferenceEquals(h.Instance, inst)` 回查（那张表已删，见 `BattleContext` 里那段订正）。
        ///
        /// 语义与数据结构写在 `CardInstance.HandBuffOps` 的注释里。这里说三条：
        ///   · 效果以**刚上场的那个单位**为目标（`ResolveOps(..., seed: unit)` + 载荷里那条
        ///     `Subjectless` 目标），所以**带关键词的加成**（`Armour 1`）会正常生效；
        ///   · **兑现即摘**：这一份上的 `Ops` 跑一遍、跑完清空；
        ///   · **次数核销**（🆕 `A886`）= 原版 `PlayerHand.CardPlayedWithEffects`
        ///     （`:28-45`：`limitedUses` 为真、且打出的这张牌身上有那条效果 ⇒ `numberOfUses--`，
        ///     `< 1` 就 `RemoveHandEffectAt` —— 那是**从整副手上摘掉**）。
        ///
        /// 🔴 **两条口径的关系（哪条优先）—— 写清，别让它们互相咬**：
        ///   · **「兑现即摘 / 额度钉在这一份上」= 我们自己的口径**（2026-09-16 起）。它管的是
        ///     「**打出去的那一份**自己的 `Ops` 跑完就清」。⚠️ 原版那条 `CardEffect` 其实**跟着牌上场、
        ///     不摘**（`PlayerHand__RemoveCardFromHand.c:20` 只是把牌从 `currentHand` 里拿走）；
        ///     我们没复刻「效果跟到场上」这一层，于是用「跑完即清」达到同一个可见结果
        ///     （加成已经落到 `UnitState` 上，牌本身不再带着它）。
        ///   · **`limitedUses` / `numberOfUses` = 原版口径**（🆕 `A886`），管的是
        ///     「**这条效果总共还能被触发几次**」—— 计数器**共享**（原版长在 `HandEffect` 记录上，
        ///     不在牌上，见 `CardInstance.HandBuffUses`），减到 0 时把这条效果
        ///     **从手牌里所有还带着它的牌上**摘掉。
        ///   ⇒ **两者并存、不互相取代**：前者清的是「**这一份**」，后者清的是「**这条效果**」。
        ///     `HandBuffUsesRef == null`（= 不限次，**今天卡池的全部情况**）时只有前者起作用 ——
        ///     与 2026-09-16 起的既有行为**逐字一致**。
        /// </summary>
        static void ApplyHandBuffs(BattleContext ctx, int owner, CardInstance inst, UnitState unit)
        {
            if (inst == null || unit == null) return;
            if (inst.HandEffects.Count == 0) return;

            // 🔴 **2026-10-18（`A885` ②）：消费者**不在这里**。**「后进手牌的牌吃上既有手牌效果」
            //    那一跳落在**各自的进手牌入口**（`RuleCore.Draw` / 造牌进手牌那几条，
            //    见 `SetupCardInHand` 的注释）—— **⛔ 不是「打出前补齐」**：
            //    那样会把「夹具/别处手工 `Hand.Add` 进来的牌」也补上，掩盖真判别式
            //    （`RuleEngineTest` 的 `Dynamic Offensive` 那一节就靠「第三份没被指到 ⇒ 身上 0 条」）。

            // 先**取下来再跑**：跑的过程中可能又给这一份挂上新的（效果里再抽牌 / 再挂那族），
            // 那些新的不该被这一趟顺手清掉。⛔ 别改成「跑完再清」。
            var ops = new List<EffectOp>();
            var boxes = new List<CardInstance.HandBuffUses>();   // 这一份身上引用的**各个**计数盒（去重）
            var src = (inst.LastHandEffect != null ? inst.LastHandEffect.Source : null);
            foreach (var e in inst.HandEffects)
            {
                if (e == null) continue;
                if (e.Op != null) ops.Add(e.Op);
                // 🔴 `A885` ④：**逐条**收盒子 —— 改之前只有一个 `HandBuffUsesRef`。
                if (e.Uses != null && !boxes.Contains(e.Uses)) boxes.Add(e.Uses);
            }
            ClearHandEffect(inst);

            ctx.Log($"（手牌加成：{unit.Name} 打出时兑现「{src}」）");
            ResolveOps(ctx, owner, unit, ops, "手牌加成", unit);

            // ---- 次数核销（原版 `PlayerHand.CardPlayedWithEffects.c:28-45`）----
            foreach (var uses in boxes)
            {
                if (!uses.Limited) continue;
                uses.Left--;
                if (uses.Left < 1) RemoveHandEffectFromHand(ctx, owner, uses, unit.Name + " 打出");
            }
        }

        /// <summary>把**这一份**身上挂的手牌效果全部清掉（**所有条**：`Op` / 来源 / 到期 / 次数盒子）。
        /// ⚠️ **次数盒子只是「这一份不再引用它」** —— 同一个盒子的其它份由
        /// <see cref="RemoveHandEffectFromHand"/> 负责摘。
        /// ⚠️ 要摘**单条**（不是整份）时，用 `RemoveHandEffectAt` 那一族的粒度 —— 见
        /// <see cref="RemoveHandEffectFromHand"/> 与 <see cref="ExpireHandBuffs"/>。</summary>
        static void ClearHandEffect(CardInstance inst)
        {
            if (inst == null) return;
            inst.HandEffects.Clear();
        }

        /// <summary>
        /// **一条次数用尽的手牌效果 ⇒ 从整副手上摘掉** —— 原版 `PlayerHand.RemoveHandEffectAt`
        /// （`PlayerHand__RemoveHandEffectAt.c:30-40`：遍历 `currentHand` 逐张 `CardScript.RemoveEffect`
        /// ⇒ 摘的是**所有还带着它的牌**，**不是**打出去的那一张 —— 那一张已经上场了）。
        /// 调用点判据 = `PlayerHand__CardPlayedWithEffects.c:43-45`（`numberOfUses < 1`）。
        ///
        /// ⚠️ 认的是**同一个计数盒**（`HandBuffUsesRef` 同一个对象）—— 那正是原版「一条 `HandEffect`
        /// 记录发给 N 张牌」的形状；⛔ 别改成「按来源卡名匹配」（两个来源可能同名）。
        /// 🔴 **2026-10-18（`A885` ④）**：摘的是**条**，不是「整份牌身上的一切」 ——
        /// 一份牌上还挂着**别的**效果（别的盒子 / 不限次的那条）时，那些**必须留着**
        /// （原版 `CardScript.RemoveEffect(那条 cardEffect)` 也是逐条摘）。
        /// </summary>
        static int RemoveHandEffectFromHand(BattleContext ctx, int owner, CardInstance.HandBuffUses uses,
                                            string why)
        {
            if (ctx == null || uses == null) return 0;
            var recs = ctx.Players[owner].HandEffectRecords;
            // ---- ① 先从**记录表**上把这条记录摘掉（原版 `PlayerHand.RemoveHandEffectAt` 就是摘记录）
            //        —— 摘了之后「后进手牌的牌」才不会再补上它。
            var ids = new List<int>();
            for (int k = recs.Count - 1; k >= 0; k--)
                if (recs[k] != null && ReferenceEquals(recs[k].Uses, uses))
                { ids.Add(recs[k].RecordId); recs.RemoveAt(k); }
            // ---- ② 再从**每一份牌身上**摘掉那几条（按记录号认；`Uses` 引用那条是兜底，
            //        防「记录先被别处摘了、牌身上还留着」）----
            int n = 0;
            var hand = ctx.Players[owner].Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var inst = hand[i];
                if (inst == null) continue;
                int cut = inst.HandEffects.RemoveAll(
                    e => e != null && (ReferenceEquals(e.Uses, uses) || ids.Contains(e.RecordId)));
                if (cut > 0) n++;
            }
            if (n > 0)
                ctx.Log($"（手牌效果次数用尽：{why} ⇒ 把这条效果从手牌里**还带着它的 {n} 张牌**上摘掉"
                      + $"，起始 {uses.Max} 次）");
            return n;
        }

        /// <summary>
        /// **手牌效果的到期清扫** —— 原版 `PlayerHand.UpdateCardEffects(bool endOfTurn)`
        /// （`PlayerHand__UpdateCardEffects.c`）。
        ///
        /// 🔴 **调用点不是我们挑的（2026-10-18 查实；原来记的是「没查到」）**：
        ///   原版**只在回合结束**跑这一趟 —— `BattleManager__ResolveEndTurn.c:678` 与 `:680`
        ///   对**两方的手牌**各调一次、传 `endOfTurn = 1`；
        ///   `BattleManager__ResolveAction.c:2576/2578` 那次传 `0`，只处理 `extrinsic`
        ///   （来源已经不在场 / 已变身的那一族，**我们没复刻**）。
        ///   ⇒ 所以这里**只挂在 `EndTurn` 上、两边手牌都扫**，**没有** `BeginTurn` 那一趟。
        ///
        /// 三条支线（逐句对着方法体写的；`CardEffect` 的偏移都在 `d:/2/tools/il2cpp_out/dump.cs` 核过）：
        ///   · `untilEndOfTurn // +0x32`（`:100`）—— **无条件摘**（那条判断**不比较谁的回合**）；
        ///   · `untilPlayerTurnStart // +0x33` / `untilFollowingPlayerTurnStart // +0x35`（`:119-137`）——
        ///     `IsPlayerTurn() != cardEffect.originalIsPlayer` 时摘（那一刻的下一回合就轮到拥有者了）；
        ///   · `untilEnemyTurnStart // +0x34`（`:156-190`）—— **我们的解析层产不出这一档**
        ///     （`EffectText.ExtractDuration` 只认 `this turn` / `until your next turn`）
        ///     ⇒ 如实标「没做」，⛔ 别当它做了；
        ///   · 原版四个 `until*` 全为假 ⇒ **不过期**（`Beast Snagga Nob` 那族每回合结束再挂一份、
        ///     一直攒着 —— `RuleEngineTest.TestBeastbossAndPayloadSegments` ④ 钉着这一条）。
        ///
        /// ⚠️ `ctx.Active` 在这一刻**还是正在结束回合的那一方**（`BeginTurn` 才换人）——
        ///    所以 `IsPlayerTurn()` 直接读成 `pl == ctx.Active`。
        /// ⚠️ **只扫手牌**：牌库 / 弃牌堆里那一份留着（原版只遍历 `currentHand`，同一条）。
        /// </summary>
        static int ExpireHandBuffs(BattleContext ctx)
        {
            int n = 0;
            int dropped = 0;   // 记录表上摘掉了几条（原版 `UpdateCardEffects.c:332` 摘的就是**记录**）
            for (int pl = 0; pl < 2; pl++)
            {
                var hand = ctx.Players[pl].Hand;
                var recs = ctx.Players[pl].HandEffectRecords;
                // 🔴 **2026-10-18（`W4` 整改 · 账 5）**：**记录表也要跟着到期** ——
                //    原版 `PlayerHand__UpdateCardEffects.c:332` 摘的就是 `activeEffects` 里
                //    那**一条记录**（`RemoveHandEffectAt`），记录不摘的话「后进手牌的牌」
                //    会把一条**已经过期**的效果补上去（静默错）。
                //    ⇒ ① 先在记录表上判、摘；② 再按记录号 + 逐条判，把牌身上那几条摘干净。
                var deadIds = new List<int>();
                for (int k = recs.Count - 1; k >= 0; k--)
                    if (recs[k] != null && HandEffectExpired(ctx, pl, recs[k]))
                    { deadIds.Add(recs[k].RecordId); recs.RemoveAt(k); dropped++; }

                // ⚠️ 逐条判、逐条摘（改之前读的是**整份牌一个到期位**，一份牌上两条不同到期的
                //    效果会**一起**被摘 / **一起**活着；原版一条 `CardEffect` 一个到期位）。
                for (int i = 0; i < hand.Count; i++)
                {
                    var inst = hand[i];
                    if (inst == null || inst.HandEffects.Count == 0) continue;
                    n += inst.HandEffects.RemoveAll(
                        e => e != null && (deadIds.Contains(e.RecordId) || HandEffectExpired(ctx, pl, e)));
                }
            }
            if (n > 0 || dropped > 0)
                ctx.Log($"（手牌效果到期：记录表上摘掉 **{dropped}** 条（原版 `PlayerHand.UpdateCardEffects` "
                      + "摘的就是**记录**），牌身上共清掉 **{n}** 条）");
            return n;
        }

        /// <summary>
        /// **一条手牌效果到期了没有** —— 逐句对着 `PlayerHand__UpdateCardEffects.c` 的方法体写。
        ///
        /// 分两段，**第二段（extrinsic）无条件跑、和 `endOfTurn` 那个参数无关**：
        ///   · 回合结束那三条（`endOfTurn = 1` 才走）：`+0x32` / `+0x33`~`+0x35` / `+0x34`；
        ///   · **无条件那三条**（`:332` 之前那一段）：`entry.cardEffect.extrinsic // +0x30` 为真时，
        ///     看 `cardEffect.enchantingCard`（**施放者**）——`!IsInPlay(施放者)` ⇒ 摘；
        ///     `HasCurrentTrait(施放者, 0x82)` ⇒ 摘（`0x82` 是哪个词条**本笔没查清** —— 如实标着，
        ///     我们**没有**实现这一条）。
        /// 🔴 `+0x30` 的语义与写点（本笔逐条核过）：tooltip `Effect is tied to acting card,
        ///    not target card`（`dump.cs:119071`）；写点 `AbilityLogic__PlayAbility.c:1197 / :1354`
        ///    的 `extrinsic = AbilityData.whileInPlay || whileInPlayVariable`
        ///    （`dump.cs:19557-19566`）⇒ **「这条效果系在【在场上才成立的那个来源】身上」**。
        /// </summary>
        static bool HandEffectExpired(BattleContext ctx, int pl, CardInstance.HandEffect e)
        {
            switch (e.Expire)
            {
                case CardInstance.HandBuffExpiry.EndOfTurn:
                    return true;                                   // 原版 `:100`：无条件
                case CardInstance.HandBuffExpiry.OwnerTurnStart:
                    return ctx.Active != pl && ctx.Turn >= e.ExpireTurn;   // 原版 `:119-137`
                default:
                    break;                                         // 原版四个 `until*` 全为假 ⇒ 不过期
            }
            // ---- 无条件那一支：extrinsic（原版 `UpdateCardEffects.c` 的 `+0x30` 那几条）----
            if (e.Extrinsic && !IsUnitInPlay(ctx, e.ActingUnit)) return true;
            // 🆕 **2026-10-18（`W4` 整改 · 审查问题 6 / 账 5）：第二条无条件支线 ——
            //    `extrinsic && HasCurrentTrait(施放者, 0x82 /* jam */)` ⇒ 摘**
            //    （`PlayerHand__UpdateCardEffects.c:235`：`HasCurrentTrait(lVar4,0x82,0)`，
            //     而 `lVar4 = *(cardEffect + 0x18)` = **`enchantingCard`（施放者）**）。
            //    · `0x82` = 130 = **`DefinedTrait.jam`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/
            //      DefinedTrait.cs:13`）；我们词表里的名字 = `KeywordTable.Jam`（本批新登记）。
            //    · 判据挂在**施放者**身上（不是那张手牌）⇒ 用 `UnitState.Has`（那是「卡面印的 +
            //      运行时加的」的全集，对应原版 `CurrentTraits`）。
            //    ⚠️ 今天全池 **0 张卡**带 `jam` ⇒ 这一支不会变异任何一局的行为（不是死代码：
            //      它等的是数据 —— 词表已认得它）。
            if (e.Extrinsic && e.ActingUnit != null && e.ActingUnit.Has(KeywordTable.Jam)) return true;
            return false;
        }

        /// <summary>那个施放者**还在不在场上**（自己的或对手的棋盘上都算 —— 原版
        /// `CardScript.IsInPlay(施放者)` 不区分谁那边；它只是个「这个对象还在局里」的判据）。
        /// ⚠️ 按**引用**找，不按卡名 —— 同名两张是两份，摘的时候认的是那一份。</summary>
        static bool IsUnitInPlay(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return false;
            for (int p = 0; p < 2; p++)
            {
                var b = ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++) if (ReferenceEquals(b[s], u)) return true;
            }
            return false;
        }

        // ==================================================================
        //  🆕 **2026-10-18（`A885` ②）：手牌效果的【消费者】**
        //     —— 「后进来的牌也会吃上既有手牌效果」那一层
        // ==================================================================

        /// <summary>
        /// **一张牌进手牌时，把「手牌登记表」上还挂着的效果补给它** ——
        /// 原版 `PlayerHand.SetupCardInHand(hand, card)`（`PlayerHand__SetupCardInHand.c:38-74`）：
        /// 遍历 `hand.activeEffects // +0x48`，对每一条
        /// `FilterMethods.CheckIfMeetsCriteria(card, handEffect.targetCriteria, 0)`（`:58`）命中、
        /// 且 `CardScript.AlreadyContainsEffect(card, effect)`（`:65`）为假 ⇒
        /// `CardScript.AddEffect(card, effect)`（`:71`）。
        /// 四个调用点 = **进手牌的每一条路**：`_AddActiveTrapToHand`（`:31`）·
        /// `_AddCardNotDrawnToHand`（`:34`）· `_AddCardNotDrawnToHandAtIndex`（`:35`）·
        /// `_AddDrawnCardToHand`（`:118`）⇒ 这才是「**后进来的牌也吃上既有手牌效果**」那一层。
        /// （📌 对照：`PlayerHand.CheckEffectsOnNewCard` **只管变形/换卡** —— 调用点只有
        ///  `ResolveTransformUnit.c:1931/:1981`；别把这两条混了。）
        ///
        /// 🔴 **结构性差异（如实标注）**：原版遍历的是**手牌级登记表**
        /// （`PlayerHand.activeEffects // +0x48`），我们把效果**挂在每一份牌自己身上**
        /// （`CardInstance.HandEffects`，= 原版 `CardScript +0x108`）。
        /// ⇒ 我们**两张表都有、且是同一批对象**：逐张的那份在实例上
        /// （兑现用），**记录表**在 `PlayerState.HandEffectRecords` 上（= 原版 `activeEffects`，
        /// 见 <see cref="HandEffectRegistry"/>）。⛔ **别再另造一张对局级的载荷副本表**
        /// （`A885` 删掉过一张，那个形状是错的）。
        ///
        /// ⚠️ **`targetCriteria` 从哪来 —— 2026-10-18（`W5` · 审查 §5 账 9「criteria 根治」）
        ///   就地订正（铁律 5）**：原来这里写着「判不出来时**退回「单位卡」这一档**」——
        ///   **那个退路已经删了**（它会把 `all **Beasts** in your hand` 发给每一个部队）。
        ///   现在三档，按原版 `FilterMethods.CheckIfMeetsCriteria(card, handEffect.targetCriteria)` 逐维落：
        ///     · `CardCriteria.FromTarget(条目上的 Target)` —— `Kind` / `KeywordFilter` / `NameFilter` 三维；
        ///     · **`SubtypeFilter`** 单独补一道（`CardCriteria.FromTarget` 不看它）
        ///       —— `Beast Snagga Nob` 的 `all Beasts in your hand` 靠它；
        ///     · **代词那一族**（`give them …` / `give it …`，`Target.Kind == "prev"`）走解析层新填的
        ///       `EffectTargetSpec.PrevAntecedent`（= 上一条 `drawtype` 那个「抽的是哪一类牌」的词，
        ///       见 `EffectText.LinkPrevAntecedent`）—— 这一格就是原版 `HandEffect.targetCriteria`
        ///       在我们这边的落点。
        /// ⛔ **三档都判不出来时【不放行】**（`HandEffectFits` 那一行 `return false`）：
        ///   那是**我们解析层的缺口**，不是原版的一种状态 —— 宁可少给，也别乱给（工程红线）。
        /// 🔴 退路**会自己说出来**（`SetupCardInHand` 末尾那句日志，2026-10-18 第三轮 + `W5` 改口）。
        /// ⚠️ **2026-10-18 订正（铁律 5）**：这句在当天**是假的** —— 那句出声**是死代码**
        /// （循环里 `continue` 排在 `loose++` 之前 ⇒ `loose` 恒 0，全 4 MB 日志零命中），
        /// 当天已按 `DB` 只读诊断 §6.1 修好（先取值 / 先计数 / 再判去留，见循环里那段注释）；
        /// 现在它**真的**会在「criteria 判不出来」的输入上出声。
        /// ⚠️ **只影响「后进手牌的牌」**：条目的**首次挂载**（`AttachHandEffect`，几条 `GrantHandBuff*`
        ///    / `AttachEffectToHandInstances`）**不走这个方法** ⇒ 手里那几张照旧吃得上。
        ///
        /// ⚠️ **`SetAutoFit`（时刻）**：原版是**进手牌那一刻**补，我们落在**各自的进手牌入口**
        /// （`RuleCore.Draw` + 几条「造牌进手牌」的路）。⛔ **不是**「打出前补齐」——
        /// 那样会把「手工塞进 `Hand` 的牌」也补上（自检里有这种夹具，会掩盖真判别式）。
        /// </summary>
        public static int SetupCardInHand(BattleContext ctx, int owner, CardInstance inst)
        {
            if (ctx == null || inst == null || inst.Card == null) return 0;
            if (owner < 0 || owner > 1) return 0;
            // ⚠️ 建场期（`BuildPlayer` 发起手牌）那一趟 `ctx.Players[owner]` 可能还没建出来。
            if (ctx.Players == null || ctx.Players[owner] == null) return 0;
            var reg = HandEffectRegistry(ctx, owner);
            if (reg.Count == 0) return 0;
            int n = 0, loose = 0;
            foreach (var e in reg)
            {
                if (e == null || e.Op == null) continue;
                // 🔴 **2026-10-18（`WC` · 修 `DB` §6.1 的 (β) 死代码）**：原来这三句是
                //    `if (!HandEffectFits(..., out isLoose)) continue;` **排在** `if (isLoose) loose++;` **之前**
                //    ⇒ 只有返回 `true` 才走到计数，而 `HandEffectFits` **只有**在
                //    `{ loose = true; return false; }` 那一支置位 ⇒ `loose` 恒 0，
                //    末尾 `if (loose > 0)` 那段「退路档自己说出来」**永远打不出来**（违反工程红线「不许静默失败」）。
                //    改成**先取值、先计数、再判去留** —— 行为完全不变（`isLoose == true` ⇔ `fit == false`），
                //    只让那句出声真的可能触发。
                bool isLoose;
                bool fit = HandEffectFits(inst.Card, e, out isLoose);
                if (isLoose) loose++;
                if (!fit) continue;
                if (AttachHandEffectCopy(ctx, inst, e)) n++;
            }
            // 🔴 **2026-10-18（`W4` 整改 · 审查问题 9）：退路档要【自己说出来】**，⛔ 不能只写在注释里。
            //    （原来只打「吃上了」，不说自己走的是「判不出条件 ⇒ 单位卡都放行」那一档 ——
            //      那会让「条件真的判中了」和「条件其实判不出来」在日志里长得一样。）
            // 🔴 **2026-10-18（`W5` · 审查 §5 账 9）就地改口（铁律 5）**：退路档**已经收窄成「不放行」**
            //    （`HandEffectFits` 那一行 `return false`）⇒ 这句文案跟着改 —— 它原来写的是
            //    「按『是单位卡就算』**放行**了」（旧行为）。⛔ 两处说法打架比没有更糟。
            // 🔴 **2026-10-18（`WC` 交件后 · 调度台当场订正 · 铁律 5）：这段文案说过头了** ——
            //    它原来写「⇒ **这一趟一条都没给它**」，而 `loose` 数的**只是判不出的那几条**：
            //    同一张牌**完全可能既有吃上的、又有判不出的**（例：命中 `SubtypeFilter` 的那条 `n++`，
            //    另一条 `Kind=="prev"` 无先行词的进 `loose`）⇒ 那时 `n ≥ 1`，旧文案仍是「一条都没给它」= **说谎**。
            //    ⚠️ 旧写法恒 `loose == 0`、这句从不打印，所以这个错**从未显形** ——
            //    是「把死代码修活」（`WC` 这趟）带出来的**新可见错误**。
            if (loose > 0)
                ctx.Log($"（进手牌：「{inst.Card.Name}」有 **{loose} 条**效果的筛选条件我们**判不出来**"
                      + $" ⇒ **那 {loose} 条没给它**（这一趟另外吃上 **{n} 条**）（`HandEffectFits` 的退路档，"
                      + "原版判据是 `HandEffect.targetCriteria`，`PlayerHand__SetupCardInHand.c:58`）"
                      + "。⚠️ 这是**我们解析层的缺口**，不是原版的一种状态；"
                      + "宁可少给，也不按「是单位卡就算」乱给）");
            return n;
        }

        /// <summary>
        /// **「手牌登记表」** —— 🆕 **2026-10-18（`W4` 整改 · 账 5）：改读
        /// `PlayerState.HandEffectRecords`**（= 原版 `PlayerHand.activeEffects // +0x48` 那张**记录表**）。
        ///
        /// 🔴 **原来是从手牌实例上「现场扫」的 —— 那是错的形状**：原版 `SetupCardInHand.c:38`
        ///    遍历的就是这张**记录表** ⇒ **某条效果的载体全部离开手牌之后，记录还在**，
        ///    后来进手牌的牌**照样**吃得上。现扫的写法在那一刻就看不见它了。
        ///    （`Beast Snagga Nob` 给手牌 +1：手里那张唯一的 Beast 打出去之后，新抽到的 Beast
        ///      在原版**照样**吃 +1，我们原来吃不上。）
        ///
        /// ⚠️ 表里是**逐张牌身上那一条条 `HandEffect` 本身**（同一批引用，不是副本）⇒ 不是第二份状态；
        ///    ⛔ 别退回「扫手牌」，也⛔ 别另造一张对局级的载荷副本表（那正是 2026-10-18 删掉的形状）。
        ///    🔴 **2026-10-18（`G3` 真差异 ①）：表里现在**允许**有「**没有任何载体的记录**」** ——
        ///    原版 `PlayerHand.AddHandEffect` 是**先收记录（`:90-110`）、再逐张贴（`:112-139`）**
        ///    ⇒ 「手上一个符合条件的都没有」时**记录照样在**（所以后来的来客吃得上）。
        ///    那一条由 `EffectResolver.RegisterHandEffectRecord`（`inst == null`，只登记不贴牌）放进来
        ///    ⇒ **它不在任何一份牌的 `HandEffects` 上，这是正常的、不是脏数据**。
        ///    ⛔ 别写「表里每一条都能在牌身上找到」这种断言。
        /// ⚠️ **按 `RecordId` 去重**（一次挂载发给 N 张牌 = **一条**记录）+ **按号升序**返回
        ///    （原版遍历的是按插入序排的表；现扫的次序 = 手牌序，不等于挂载序 —— 多条同时兑现时
        ///      `Ops` 的应用次序会跟着变，今天没有数据能让它显形，但次序错了就是静默错打）。
        /// </summary>
        static List<CardInstance.HandEffect> HandEffectRegistry(BattleContext ctx, int owner)
        {
            var r = new List<CardInstance.HandEffect>();
            var recs = ctx.Players[owner].HandEffectRecords;
            for (int i = 0; i < recs.Count; i++)
            {
                var e = recs[i];
                if (e == null || e.Op == null || e.Op.Verb != "give") continue;
                bool dup = false;
                for (int k = 0; k < r.Count; k++)
                    if (r[k].RecordId == e.RecordId) { dup = true; break; }
                if (!dup) r.Add(e);
            }
            r.Sort((a, b) => a.RecordId.CompareTo(b.RecordId));
            return r;
        }

        /// <summary>后进手牌的这张卡吃不吃得上这一条 —— 判据与理由见 <see cref="SetupCardInHand"/>。
        /// <paramref name="loose"/> = **走的是退路档**（条件判不出来）——
        /// 🔴 2026-10-18（`W4` 整改 · 审查问题 9）：它**必须带回给调用方**，
        /// 由 `SetupCardInHand` 打一句日志。⛔ 不许只是注释里写「这是近似」。
        /// 🔴 **2026-10-18（`W5` · 审查 §5 账 9）就地改口（铁律 5）**：退路档从
        ///   「只按**单位卡**放行」收窄成「**不放行**」（`return false`）—— 理由逐条写在方法体里。
        ///   这一格原来写的是「条件判不出来 ⇒ 按单位卡放行」，是**旧行为**。
        ///
        /// 🔴 **2026-10-18（`W4` 整改 · 第三轮）补一道兵种筛**：`CardCriteria.FromTarget`
        ///    读的是 `Kind` / `KeywordFilter` / `NameFilter`，**不看 `SubtypeFilter`** ——
        ///    而 `Beast Snagga Nob` 的 `all **Beasts** in your hand` 正是靠 `SubtypeFilter` 表达的
        ///    ⇒ 不补这一道的话，那条记录会**发给每一个后进手牌的部队**（卡面只管野兽）——
        ///    「打得比卡面宽」的静默错。（判据与写法 = `GrantHandBuffForTargets` 收候选那一段，
        ///     **同一份纪律**：`Subtype` 为空的卡**不许悄悄排除**。）</summary>
        static bool HandEffectFits(CardDef card, CardInstance.HandEffect e, out bool loose)
        {
            loose = false;
            if (card == null) return false;
            // 🔴 **2026-10-18（`W5` · 审查 §2 作者自陈 b + §5 账 9）：原来这里有一道
            //    `if (card.Type != "unit") return false;` —— 已删。**
            //    原版 `PlayerHand.SetupCardInHand` **不看卡类型**（`PlayerHand__SetupCardInHand.c:38-74`
            //    只有 criteria 那一关）；「非单位卡吃不上」应当是 **criteria 判出来的结果**，
            //    不该是这里另加的一条闸（两处写同一条规则 = 迟早不一致）。
            //    ⚠️ 删掉它**不会放宽**：今天所有能产出的 criteria 都是 `KindWord = unit/troop`
            //      （`HandTroopCriteria`）或具体兵种词 —— 而 `CardCriteria.Matches` 走
            //      `CreatePool.MatchesKind`，对非单位卡一律 false。下面那条「判不出来就不放行」
            //      也兜住了其余情形。
            var crit = CardCriteria.FromTarget(e.Target);
            bool hasSubtype = e.Target != null && !string.IsNullOrEmpty(e.Target.SubtypeFilter);
            // 🆕 **2026-10-18（`W5`）**：代词（`them` / `it`）那一族 —— `FromTarget` 对 `Kind == "prev"`
            //   返回 `null`，但**解析层已经把先行词的筛条件填好了**
            //   （`EffectText.LinkPrevAntecedent` → `EffectTargetSpec.PrevAntecedent`，= 原版
            //   `HandEffect.targetCriteria`）。先补这一档，再谈「判不出来」。
            if (crit == null && e.Target != null && e.Target.PrevAntecedent != null)
                crit = e.Target.PrevAntecedent;
            // 🔴 **2026-10-18（`W5` · 审查 §5 账 9 的收窄）**：退路档从「判不出来就**放行单位卡**」
            //    改成「判不出来就**不放行**」。
            //    · **判据**：原版这一关是 `FilterMethods.CheckIfMeetsCriteria(card, handEffect.targetCriteria)`
            //      —— 条件**判不出来**时，原版无从谈起「放行」（它拿的是作者在 ability 资产上标好的
            //      `TargetCriteria`，永远判得出来）⇒ **我们的「判不出来」不是原版的一种状态**，
            //      而是我们解析层的缺口。缺口的正确处置是**不出声地错打？不，是宁可少给**
            //      （工程红线「宁可认不出，也别静默错打」：放行 = 把 `all **Beasts** in your hand`
            //      发给每一个部队）。
            //    · **为什么现在能改**：先做完了「解析层产出 criteria」那一半
            //      （`PrevAntecedent` + 既有的 `SubtypeFilter`/`CardCriteria` 两维）⇒ 真卡池上
            //      剩下的「判不出来」已经收窄到**没有先行词可判**的那几张（如 `GSC75 Rogue Informant`
            //      的 `If it's a troop, give it Stealth` —— 那句条件我们解析层不产出）
            //      ⇒ 它们**少给**而不是**乱给**。
            //    · ⚠️ **只影响「后进手牌的牌」**（`SetupCardInHand` 那条消费者路）——
            //      条目的**首次挂载**（`AttachHandEffect`）不走这里 ⇒ 手里那几张照旧吃得上
            //      （`Beast Snagga Nob` 那一族**一点没受影响**，它的 `SubtypeFilter = "Beast"` 可判）。
            if (crit == null && !hasSubtype) { loose = true; return false; }   // ← 收窄：判不出来就不放行
            if (crit != null && !crit.Matches(card)) return false;
            if (hasSubtype)
            {
                bool hit = string.IsNullOrEmpty(card.Subtype);   // 查不到兵种的卡不排除（同一条纪律）
                foreach (var w in e.Target.SubtypeFilter.Split('|'))
                    if (string.Equals(card.Subtype, w.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    { hit = true; break; }
                if (!hit) return false;
            }
            return true;
        }

        /// <summary>
        /// **「它本回合内死了，就把这条效果转给另一个」** —— 死亡那一刻的追加结算
        /// （🆕 2026-09-16，`BL77 Spreading Corruption`）。
        ///
        /// 语义与出处全写在 `BattleContext.DeathWatch` 的注释里；这里只说两条判断：
        ///   · **只转一次、不递归**：命中就**先从表里摘掉再结算** ⇒ 转过去的那个再死也不传染。
        ///     递归语义**三层零依据**（规则书 / `rule_core.gd` / 反编译都查不到）⇒ **这是我们定的**，
        ///     卡面也只说「apply this effect to another …」，没写会连锁。
        ///   · **过期就清**（`ExpireTurn != ctx.Turn`）—— 卡面写 `this turn`。
        /// </summary>
        static void FlushDeathWatches(BattleContext ctx, UnitState dead, int who)
        {
            if (ctx.DeathWatches.Count == 0) return;
            for (int i = ctx.DeathWatches.Count - 1; i >= 0; i--)
            {
                var w = ctx.DeathWatches[i];
                if (w.ExpireTurn != ctx.Turn) { ctx.DeathWatches.RemoveAt(i); continue; }
                if (w.Target == null || !ReferenceEquals(w.Target, dead)) continue;
                ctx.DeathWatches.RemoveAt(i);          // ← 先摘再结算 = 「只转一次、不递归」
                if (w.Ops == null || w.Ops.Count == 0) continue;
                ctx.Log($"（{dead.Name} 本回合内死亡 ⇒「{w.Source}」**转给另一个目标**）");
                var un = new List<string>();
                ResolveOps(ctx, w.Owner, null, w.Ops, null, out un);
                // 🔴 2026-09-16：原来 `un` 建了不读（见 `EffectResolver.ReportUnresolved` 的注释）
                ReportUnresolved(ctx, "「" + w.Source + "」结转给另一个目标的那一遍", un);
            }
        }

        /// <summary>
        /// 这个单位现在能不能**开始**发动技能 —— 只看它自己：在不在、有没有技能、行动过没有。
        /// **不判目标。**
        /// 为什么要和 <see cref="CanUseAbility"/> 分开：要选目标的技能，表现层得先
        /// 「进入选目标状态、点亮合法目标」，那一步还没选呢。拿要求目标的判据去问，
        /// 只会得到 `ErrTarget`，于是永远进不了选目标状态（踩过，见 BattleScene 的技能用例）。
        /// </summary>
        public static int CanStartAbility(BattleContext ctx, int p, int slot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!BoardSpec.IsValid(slot)) return RuleCodes.ErrNotUnit;

            var u = ctx.Players[p].Board[slot];
            if (u == null) return RuleCodes.ErrNotUnit;
            if (!u.HasAbility) return RuleCodes.ErrNoAbility;
            if (u.Exhausted) return RuleCodes.ErrExhausted;
            if (u.IsStunned) return RuleCodes.ErrStunned;
            return RuleCodes.OK;
        }

        /// <summary>
        /// 能不能发动技能，**并且**打中 <paramref name="targetSlot"/>。
        /// 不需要选目标的技能（治疗/抽牌/打督军）传不传 targetSlot 都一样。
        ///
        /// ⚠️ **技能不是攻击**，所以攻击目标那三条限制里只有一条适用：
        ///
        /// | 关键词 | 管不管技能 | 出处 |
        /// |---|---|---|
        /// | 先锋 Vanguard | **不管** —— 原文是「其他单位不能被选为**攻击目标**」 | 规则书 :223 |
        /// | 飞行 Flying | **不管** —— 原文是「只能被…以**近战攻击**选中」 | 规则书 :188 |
        /// | 潜行 Stealth | **管** —— 原文是「不能被**任何方式**选中」 | 规则书 :211 |
        /// | 无法攻击 Can't Attack | **不管** —— 只禁攻击。0 攻单位能放技能，正是它的价值 | 规则书 :174 |
        /// </summary>
        /// <param name="targetSlot">技能目标需要选单位时才用（`EffectTargets.NeedsPick`）</param>
        public static int CanUseAbility(BattleContext ctx, int p, int slot, int targetSlot = -1)
        {
            int code = CanStartAbility(ctx, p, slot);
            if (code != RuleCodes.OK) return code;

            if (!EffectTargets.NeedsPick(ctx.Players[p].Board[slot].Ability.Target)) return RuleCodes.OK;

            if (!BoardSpec.IsValid(targetSlot)) return RuleCodes.ErrTarget;
            var t = ctx.Players[1 - p].Board[targetSlot];
            // 要选**单位**的目标就只能是部队 —— 想打督军的卡，目标那一栏会直接写 `EnemyWarlord`
            if (t == null || t.IsWarlord) return RuleCodes.ErrTarget;
            if (t.Has(KeywordTable.Stealth)) return RuleCodes.ErrTarget;

            return RuleCodes.OK;
        }

        /// <summary>
        /// 发动主动技能：**花掉这个单位本回合的行动**，结算它的效果。
        ///
        /// ⚠️ 技能**不吃反击**（反击是「被攻击」的结果，技能不是攻击）。
        ///    这是主动技能相对普通攻击的立身之本 —— `Ironclad` 2 攻打 2 伤技能，选技能就少挨还手。
        /// </summary>
        public static int UseAbility(BattleContext ctx, int p, int slot, int targetSlot = -1)
        {
            int code = CanUseAbility(ctx, p, slot, targetSlot);
            if (code != RuleCodes.OK) return code;

            var u = ctx.Players[p].Board[slot];
            u.LastAttackType = 4;   // 🆕 表现用：主动技能那一档（原版 `currentAttackType = Active`）
            // 🆕 2026-10-18（`W5` · `K3` 第 9 个写点）：用了主动能力 ⇒ 带 duty/ferocity/oath 的那个
            //   重挑攻击型（原版 `ResolveActiveAbilityPlayed.c:29-42`，判据见本方法）。
            RecomputeAttackTypeAfterActiveAbility(u);
            var spec = u.Ability;
            var chosen = EffectTargets.NeedsPick(spec.Target) ? ctx.Players[1 - p].Board[targetSlot] : null;

            u.Exhausted = true;
            u.AttacksThisTurn++;      // 算「本回合已行动」
            // 🆕 2026-09-30：嗜血的「亮一下」—— 原版 `UsedActiveAbility` 那条路（判据 → `EmitBloodThirst`）
            EmitBloodThirst(ctx, p, slot, u);
            // 🆕 2026-09-29 原版 `EntityScript.usedActiveAbility`（`+0x4C`）在这里置 1
            // （`CardScript__ResolveActiveAbilityPlayed.c:31-32`）—— 读它的是 `Duty` 徽标的未激活态。
            u.UsedActiveAbilityThisTurn = true;
            // ⚠️ 技能**不解除 Stealth**：规则书 :211 说的是「**攻击**前不能被选中」，
            //    放技能不是攻击。v1 里没有既带 Stealth 又带技能的单位，这条先按原文来。

            ctx.Emit(EvtKind.Ability, p, slot, u.Name,
                     keyword: KeywordTable.Ability, effect: spec.Source, amount: spec.Amount);
            ctx.Log($"{ctx.Players[p].Name} 的 {u.Name} 发动技能「{spec.Source}」");

            // ⚠️ **2026-09-13 A2 更正**：这里原来还发一条 `When a friendly unit prays`（`Prays`）——
            //    那是把 Pray / Duty / Ferocity / Agenda **收成一条**时代的近似，理由是
            //    「严格说只有祈祷该触发它」。现在那四个关键词**拆开**了（`UseAlternative`），
            //    各自发各自的广播 ⇒ **这条近似撤掉**：走 `Ability:` 的技能不是「祈祷」，
            //    再发它就会让 `When a friendly unit prays` **多响**（打得比卡面宽）。
            //    真祈祷走 `UseAlternative(..., "pray")`，在那里发 `Prays`。

            ctx.EffectChain++;
            ResolveEffect(ctx, p, u, spec, chosen);
            ctx.EffectChain--;

            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        // ==================================================================
        //  **誓约能力**（`Oath N: …`）—— 🆕 2026-09-16
        //
        //  卡面：`Oath 1: Deal 1 damage` 这种「花 N 费激活一次」的能力（全池 22 张单位卡 + 3 张
        //  「改规则」的卡）。`EffectText` 早就认这个前缀（`ReOathPaid`），战术卡那支也早就在结算，
        //  **只有单位卡这一支从来没接**（正文收不到 ⇒ 能力一直是死的，且不报错）——
        //  见 `CardDef.OathOps` 的注释。这一段补齐引擎侧，表现层那格按钮在
        //  `BattleDriver.OpenCommand`（和 `Ability:` / 替代行动**共用同一格**，原版也是一个按钮）。
        // ==================================================================

        /// <summary>同方场上带 `OathDouble` 的**牌数** —— 「友方誓约能力多结算几次」的次数。
        /// 🔴 **只此一处**（照原版 `BattleManager__IsThereDoubleOathEffect.c:33` 那个循环写的：
        /// 遍历同方场上每张牌、带该 trait 就 `++`）。</summary>
        public static int OathExtraReplays(BattleContext ctx, int p)
        {
            int n = 0;
            var board = ctx.Players[p].Board;
            for (int i = 0; i < BoardSpec.Size; i++)
            {
                var u = board[i];
                if (u != null && u.Card != null && u.Card.OathDouble) n++;
            }
            return n;
        }

        /// <summary>每回合能激活几次誓约：默认 **1**；同方场上有 `OathTripleActivation` ⇒ **3**
        /// （原版 `CardScript__CanUseOathAbility.c:16-20`）。
        /// ⚠️ 原版那两个 trait 同时在时上限退化成 **1**（那里是 `&amp;&amp;`）—— **全池没有同时带两张的卡**，
        ///    这条怪癖我们**不复现**（复现了也没有一张卡能走到），如实记在这儿。</summary>
        public static int OathActivationCap(BattleContext ctx, int p)
        {
            var board = ctx.Players[p].Board;
            for (int i = 0; i < BoardSpec.Size; i++)
            {
                var u = board[i];
                if (u != null && u.Card != null && u.Card.OathTripleActivation) return 3;
            }
            return 1;
        }

        /// <summary>同方场上有没有 `OathInAllTurns` —— 有就豁免「必须本回合部署」
        /// （原版 `CardScript__CanUseOathAbility.c:8`）。</summary>
        public static bool OathAllTurns(BattleContext ctx, int p)
        {
            var board = ctx.Players[p].Board;
            for (int i = 0; i < BoardSpec.Size; i++)
            {
                var u = board[i];
                if (u != null && u.Card != null && u.Card.OathInAllTurns) return true;
            }
            return false;
        }

        /// <summary>这张牌现在能不能激活誓约能力。
        ///
        /// 与 <see cref="CanStartAbility"/> 的差别（**别混**）：
        ///   · **不看 `Exhausted`** —— 誓约不占「本回合那次行动」。原版有两套计数器：
        ///     行动与它无关，誓约自己数 `+0x50`（`CardScript__ResolveActiveAbilityPlayed.c:31`）。
        ///     ⇒ 一个 0 攻的督军随从照样能在行动过后激活誓约。
        ///   · **默认「必须本回合部署」**（原版 `CardScript__IsTheSameTurnPlayed.c:24-38`），
        ///     由 `oathInAllTurns` 豁免。
        ///   · **每回合次数上限**（默认 1，`OathActivationCap`）。
        ///   · 眩晕照旧挡（规则书 `:150`「受与攻击相同的限制」那一族的共同点）。
        /// </summary>
        public static int CanUseOathAbility(BattleContext ctx, int p, int slot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!BoardSpec.IsValid(slot)) return RuleCodes.ErrNotUnit;

            var u = ctx.Players[p].Board[slot];
            if (u == null) return RuleCodes.ErrNotUnit;
            if (u.Card == null || u.Card.OathOps.Count == 0) return RuleCodes.ErrNoAbility;
            if (u.IsStunned) return RuleCodes.ErrStunned;
            if (u.OathUsesThisTurn >= OathActivationCap(ctx, p)) return RuleCodes.ErrExhausted;
            if (!OathAllTurns(ctx, p) && u.DeployedTurn != ctx.Turn) return RuleCodes.ErrExhausted;
            return RuleCodes.OK;
        }

        /// <summary>激活誓约能力：**付 N 费**（`ResolveOneCore` 开头那套付费分支），跑正文
        /// （多结算几次见 <see cref="OathExtraReplays"/>），并把这次激活记进 `OathUsesThisTurn`。
        ///
        /// ⚠️ **付不起 ⇒ 整条不生效**且**不消耗次数**（付费判据在结算层，见
        ///    `EffectResolver.ResolveOathAbility` 的返回值）—— 否则「点一下没了」会是静默的坑。
        /// ⚠️ **不用 `Exhausted`**：理由见 <see cref="CanUseOathAbility"/>。
        ///
        /// 🔴 **2026-10-17 修：`EvtKind.Ability` 原来发在【付费之前】**（`ctx.Emit` 排在
        ///    `ResolveOathAbility` 上面）⇒ **付不起的那一次也发过一条「能力发动了」的事件**，
        ///    而监听方全在表现层 —— 战斗日志按它写一行「发动技能」（`BattleDriver.cs:4510`）、
        ///    VFX 映射（`BattleDriver.cs:6661`）、trait 粒子（`BattleDriver.cs:6825`，
        ///    `EvtKind.Ability` 那一档对应的正是原版 `UsedActiveAbility` = trait `0x4f1` = ferocity）
        ///    ⇒ 它们都会以为能力真的发动了。
        ///    ⇒ 现在**整段（`Emit` + 日志）挪到付费成功之后** —— 语义 = 「**能力真的发动了**」。
        ///
        ///    **原版顺序**（三段，逐条有方法体）：闸 → **付费** → 结算。
        ///      ① `CardScript__CanUseOathAbility.c`（能不能用；次数上限 3 由
        ///         `CardScript__CanUseOathAbility.c:16-20` 判）；
        ///      ② **`BattleManager__PayActiveAbilityCostOath.c`** —— 代价读该卡的 trait
        ///         `0x4fb`（`EntityScript.GetCurrentTraitValue(card, 0x4fb)`）后
        ///         **`PlayerManager.UseMana(cost)`**，**先付费**；
        ///      ③ `CardScript__ResolveActiveAbilityPlayed.c` —— 到这一步才 `+0x50`（本回合次数）
        ///         `+= 1`、`+0x4C`（`usedActiveAbility`）置 1，再调 `ResolveActiveAbility`。
        ///      ⇒ 原版**没有任何一步**在付费之前宣布「能力发动了」；我们那条 `Emit` 与
        ///        「③ 已经进去了」是同一件事 ⇒ 必须排在付费**之后**。
        /// </summary>
        public static int UseOathAbility(BattleContext ctx, int p, int slot)
        {
            int code = CanUseOathAbility(ctx, p, slot);
            if (code != RuleCodes.OK) return code;

            var u = ctx.Players[p].Board[slot];
            u.LastAttackType = 4;   // 🆕 表现用：誓约能力也走「技能」那一格（原版同）
            // 🆕 2026-10-18（`W5` · `K3` 第 9 个写点）：誓约单位重挑攻击型（原版
            //    `ResolveActiveAbilityPlayed.c:29-42` 的 `duty||ferocity||oath` 那一支）。
            RecomputeAttackTypeAfterActiveAbility(u);

            // ⚠️ `ResolveOathAbility` 定义在 `EffectResolver.cs` 里，但**那个文件里的类也是 `RuleCore`**
            //    （`public static partial class RuleCore`）—— 照文件名写 `EffectResolver.` 编译不过。
            bool ok = ResolveOathAbility(ctx, p, u);
            if (!ok)
            {
                // 结算层已经打过「为什么没生效」的日志（付不起 / 条件不成立…）——
                // 这里**不消耗次数、也不当成激活过**，玩家再点一次仍然可以。
                // 🔴 **而且一条 `EvtKind.Ability` 都不发**（见上面那段 —— 发在付费之前是本条要修的缺陷）。
                ctx.Log($"（这次激活没有生效 —— 次数没有消耗）");
                return RuleCodes.ErrUnimplemented;
            }

            // ---- 到这里 = **付费成功、正文也跑过了** ⇒ 现在才宣布「能力发动了」----
            ctx.Emit(EvtKind.Ability, p, slot, u.Name,
                     keyword: "oath", effect: "Oath " + u.Card.OathCost, amount: u.Card.OathCost);
            ctx.Log($"{ctx.Players[p].Name} 的 {u.Name} 激活誓约能力（Oath {u.Card.OathCost}）");

            u.OathUsesThisTurn++;
            // 🆕 2026-09-29 誓约走的是**同一条** `ResolveActiveAbilityPlayed`
            // ⇒ 原版那一处把 `+0x4C` 也置 1（我们的 `OathUsesThisTurn` 就是 `+0x50`）。
            u.UsedActiveAbilityThisTurn = true;
            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        // ==================================================================
        //  **替代行动**（`Duty` / `Pray` / `Ferocity` / `Agenda`）—— 2026-09-13 A2
        // ==================================================================

        /// <summary>
        /// **替代行动**那一族（规则书 `:150`）：四个关键词各自是「**花掉本单位一次行动**换一个效果」，
        /// 同形，差别只在下面这几条（逐条都有出处）：
        ///
        /// | 关键词 | 差别 | 出处 |
        /// |---|---|---|
        /// | `ferocity` 狂暴 | **快速**：部署当回合就能用（`UnitState` 构造里豁免）；用完之后**洗回牌库** | 规则书 `:186` · `:98` |
        /// | `pray` 祈祷 | **缓慢**：部署当回合不能用（「缓慢」全书**没有定义段**，见 `KeywordTable.Pray`） | 规则书 `:198` · `:98` |
        /// | `duty` 职责 | **本局一次**（可由效果装填，装填动词还没做）；**即使无法攻击也能激活** | 规则书 `:181` · `:152` |
        /// | `agenda` 议程 | 「以触发效果代替攻击」⇒ 与**攻击**同限制 | 规则书 `:165` · `:150` |
        ///
        /// 🔴 **这是把一条近似拆开**：以前我们自定了一个 `Ability:` 关键词把这一族**收成一条**
        /// （`KeywordTable.Ability` 的注释），后果是 `When a friendly unit prays` 在放
        /// `Duty` / `Ferocity` 时**也会响**（那正是 `WhenEvent` 里记着的近似）。现在**逐关键词判**。
        ///
        /// ⚠️ **共同点照 `:150`**：「受与攻击相同的限制条件约束」⇒ 不是你的回合 / 眩晕 / 本回合已行动
        ///    都不行（判据和 `CanStartAbility` 同一套；「部署当回合不能动」那一条由
        ///    `UnitState` 构造函数里的 `Exhausted` 表达，`fast`/`flank`/`ferocity` 三个豁免）。
        /// </summary>
        public static readonly string[] AlternativeActions = {
            KeywordTable.Duty, KeywordTable.Pray, KeywordTable.Ferocity, KeywordTable.Agenda };

        /// <summary>这个词是不是「替代行动」那一族。</summary>
        public static bool IsAlternativeAction(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            foreach (string k in AlternativeActions) if (k == keyword) return true;
            return false;
        }

        /// <summary>
        /// 这个单位现在**能用**的那条替代行动（没有返回 null）。**至多一条** ——
        /// 实测全卡池**没有任何一张卡同时带两个**（duty 23 / pray 10 / ferocity 11 / agenda 12，
        /// 两两不重叠）⇒ 表现层「一格按钮」就够，和原版那张主动技能按钮是同一个位置。
        /// </summary>
        public static string AvailableAlternative(BattleContext ctx, int p, int slot)
        {
            foreach (string k in AlternativeActions)
                if (CanUseAlternative(ctx, p, slot, k) == RuleCodes.OK) return k;
            return null;
        }

        /// <summary>
        /// 本单位现在能不能用这个替代行动。**只判不执行** —— 表现层要拿它决定「给不给这个按钮」，
        /// 拿 `UseAlternative` 去试会在日志里留下一堆假失败。
        /// </summary>
        public static int CanUseAlternative(BattleContext ctx, int p, int slot, string keyword)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!IsAlternativeAction(keyword)) return RuleCodes.ErrNoAction;
            if (!BoardSpec.IsValid(slot)) return RuleCodes.ErrNotUnit;

            var u = ctx.Players[p].Board[slot];
            if (u == null) return RuleCodes.ErrNotUnit;
            if (!u.Has(keyword)) return RuleCodes.ErrNoAction;
            // 正文一条都收不到 = 这条替代行动**没有效果可放** ⇒ 明说不支持（别给一个点了没反应的按钮）
            // ⚠️ 判据走 `HasFx`（原生正文 / **运行时挂上去的**正文 / 封闭文法那条，三合一，
            //    全仓只此一处）—— 原来这儿只查前两者里的两个，**漏了挂上去的那份**。
            if (!u.HasFx(keyword)) return RuleCodes.ErrNoAction;
            if (u.IsStunned) return RuleCodes.ErrStunned;
            if (u.Exhausted) return RuleCodes.ErrExhausted;
            // 职责：**本局一次**（`Exhausted` 每回合重置，这个不重置）
            if (keyword == KeywordTable.Duty && u.DutyUsed) return RuleCodes.ErrDutyUsed;
            return RuleCodes.OK;
        }

        /// <summary>
        /// 执行替代行动：**花掉这个单位本回合的行动**，结算它自己那条正文。
        ///
        /// 和 <see cref="UseAbility"/> 的关系：那一个是本工程自定的 `Ability:`（v1 的近似），
        /// 这一个才是原版的四个关键词。两者**并存**（有的卡用前者），但**不再互相冒充**。
        ///
        /// ⚠️ **狂暴用完洗回牌库**（规则书 `:186`「执行时触发效果，**然后洗回牌库**」）——
        ///    我们照做：离场 → 卡回**牌库**（不是弃牌堆）→ 洗牌（走 `ctx.Rng`，同一局可复现）。
        /// </summary>
        public static int UseAlternative(BattleContext ctx, int p, int slot, string keyword,
                                         int targetSlot = -1)
        {
            int code = CanUseAlternative(ctx, p, slot, keyword);
            if (code != RuleCodes.OK) return code;

            var u = ctx.Players[p].Board[slot];
            u.LastAttackType = 4;   // 🆕 表现用：替代行动也走「技能」那一格（原版同）
            // 🆕 2026-10-18（`W5` · `K3` 第 9 个写点）：替代行动同样是 `ResolveActiveAbilityPlayed`
            //    那一支（`ferocity` 就在这三个关键词里），所以同样重挑。
            RecomputeAttackTypeAfterActiveAbility(u);
            // ⚠️ **`FxOps`**（原生正文 → **挂上去的正文**）：`Give "💀 Backlash: …" to a friendly troop`
            //    那种挂上来的也要能执行替代行动（原来只读 `Card.TriggerOps`，挂的那份看不见）。
            var ops = u.FxOps(keyword);
            var spec = u.Effect(keyword);
            // 目标：这一族的正文走 **`EffectText`**（不是封闭文法），所以要按**正文解析出来的规格**
            // 问「要不要玩家点一个」以及**点哪一侧**（`Pray: Give Shield to a friendly unit` 点的是**自己人**）。
            // ⚠️ 原来这里照 `Ability`（`EffectSpec`）那条路写死了 `1 - p`（敌方）—— 那会让
            //    「给一个友方单位」的技能把玩家的选择**落到对面棋盘上**（静默打错人）。
            UnitState chosen = null;
            if (ops != null)
            {
                var pspec = EffectText.PickTarget(ops);
                if (pspec != null && BoardSpec.IsValid(targetSlot))
                    chosen = ctx.Players[pspec.Side == "enemy" ? 1 - p : p].Board[targetSlot];
            }
            else if (spec != null && EffectTargets.NeedsPick(spec.Target))
                chosen = ctx.Players[1 - p].Board[targetSlot];

            u.Exhausted = true;
            u.AttacksThisTurn++;                       // 算「本回合已行动」（规则书 `:150`）
            // 🆕 2026-09-30：嗜血的「亮一下」—— 替代行动（Duty/Pray/Ferocity/Agenda）也走「激活主动能力」那条路
            //   （原版那一位同样是 `UsedActiveAbility` 里 `+0x48` 那一档；判据 → `EmitBloodThirst`）
            EmitBloodThirst(ctx, p, slot, u);
            if (keyword == KeywordTable.Duty) u.DutyUsed = true;
            // 🆕 2026-09-29 替代行动（Duty / Pray / Ferocity / Agenda）同样走
            // 「激活一个主动能力」这条路 ⇒ 原版那一处置的是同一个 `+0x4C`。
            u.UsedActiveAbilityThisTurn = true;
            // 「正在祈祷」= **状态**，一直挂到本单位控制者的下个回合开始
            // （`UnitState.Prayed` 的注释里有出处：`rule_core.gd:2371` 置位 / `:1953` 复位）。
            // ⚠️ 和下面那条 `BroadcastWhen(Prays)` 是**两件事** ——
            //    那个是「祈祷发生了」的**事件**（监听者当场响应），这个是**状态**（`Devout Serenity` 那种
            //    「每个正在祈祷的单位」要读的）。两者都要，缺一个就有一类卡不响。
            if (keyword == KeywordTable.Pray) u.Prayed = true;

            // 走**所有触发唯一的那个出口** —— 事件、递归保护、`When … triggers <关键词>` 广播
            // 全都免费拿到（`When a friendly unit uses Ferocity` 那几条就是要它）
            FireTriggerAt(ctx, u, keyword, p, slot, chosen);
            // `When a friendly unit prays, …`（卡面写的是 **prays**，不是 `triggers Pray`）——
            // 两个短语在 `WhenEvent` 里是**两个 kind**（`Prays` vs `Triggers("pray")`），都要发。
            // ⚠️ 只有真·祈祷发这条；`Duty` / `Ferocity` / `Agenda` 不发
            //    （原来收成一条时它们也发 —— 那是「打得比卡面宽」，2026-09-13 A2 拆开时改掉）。
            if (keyword == KeywordTable.Pray) BroadcastWhen(ctx, WhenEventKind.Prays, p, u.Card, u);
            ctx.Log($"{ctx.Players[p].Name} 的 {u.Name} 执行了{AlternativeActionName(keyword)}");

            // 狂暴：**然后洗回牌库**（离场要在触发**之后** —— 正文里可能用到它自己的格位）
            //
            // 🆕 2026-09-14 A4 批 3：这里其实是**两个独立的短路**，照反编译的顺序抄 ——
            //   `CardScript__UsedActiveAbility.c:52-64`：`has(ferocity)` → **广播**（我们上面 `FireTriggerAt` 就是）
            //   → `has(dontReturnFerocity)`（`DefinedTrait:131 = 1270`）**或** `EnoughPendingDamageToDie`
            //   → **才**不回牌库；否则 `AddRecallToDeck`。
            //   下面那个 `u.IsAlive && Board[slot]==u` 就是第二个短路的对应物（已经死了/不在了就不回）。
            // ⚠️ **标记必须在**这一支里**消费掉**（反编译 `:75-88` 用完就 `SendRemoveEffect(..., 1)`）——
            //   「**下一次**」全靠这一清。只在 `RefreshForNewTurn` 清的话会变成「本回合每次狂暴都留场」。
            if (keyword == KeywordTable.Ferocity)
            {
                bool stayInPlay = u.FerocityStay;
                if (stayInPlay) u.FerocityStay = false;      // 用掉即清（不管下面走哪一支）
                if (stayInPlay)
                {
                    ctx.Log($"{u.Name} 的狂暴结算完 —— **留在场上**"
                          + "（`Bjorn's Shrine` 给的「本回合下一次」标记用掉了）");
                }
                else if (u.IsAlive && ctx.Players[p].Board[slot] == u)
                {
                    // 🔴 2026-10-01：离场要**补位**（原版 `_ResolveRecallToDeck` 同样走 `RemoveMinion`）
                    BoardSlots.RemoveAt(ctx.Players[p], slot);
                    ctx.Players[p].Deck.Add(u.Instance);   // 第 7 行第 2 步：**同一份**洗回牌库（不新发）
                    Shuffle(ctx.Players[p].Deck, ctx.Rng);
                    Auras.Recompose(ctx);  // 🆕 A7：棋盘变动 ⇒ 光环重算
                    ctx.Log($"{u.Name} 的狂暴结算完 —— **洗回牌库**（规则书 :186）");
                    // ⚠️ 发 `Return`（「离开格位但不是阵亡」）而不是 `Death` —— 表现层据此播
                    //    「回手/回牌库」那套，不会误播阵亡消散
                    ctx.Emit(new BattleEvent { Kind = EvtKind.Return, Player = p, Slot = slot, CardId = u.Name });
                }
            }

            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        /// <summary>替代行动的显示名（日志与界面用）</summary>
        public static string AlternativeActionName(string keyword)
        {
            if (keyword == KeywordTable.Duty) return "职责";
            if (keyword == KeywordTable.Pray) return "祈祷";
            if (keyword == KeywordTable.Ferocity) return "狂暴";
            if (keyword == KeywordTable.Agenda) return "议程";
            return keyword;
        }

        /// <summary>
        /// 替代行动要选目标时：**这一格能不能选**。
        /// 判据和战术卡那份**共用**（`EffectResolver.IsLegalPick` —— 它管的正是「这个格位的单位
        /// 在不在这个目标规格的候选池里」），所以「高亮能点、打出去却空过」那类不一致不会再出现。
        /// </summary>
        public static bool CanPickAlternativeTarget(BattleContext ctx, int p, int slot,
                                                    string keyword, int targetSlot)
        {
            if (ctx.IsOver || p != ctx.Active) return false;
            if (!BoardSpec.IsValid(targetSlot)) return false;
            if (CanUseAlternative(ctx, p, slot, keyword) != RuleCodes.OK) return false;

            var u = ctx.Players[p].Board[slot];
            var ops = u != null ? u.Card.TriggerOps(keyword) : null;
            if (ops == null) return false;
            var spec = EffectText.PickTarget(ops);
            if (spec == null) return false;

            int side = spec.Side == "enemy" ? 1 - p : p;
            var t = ctx.Players[side].Board[targetSlot];
            return t != null && IsLegalPick(ctx, p, spec, t);
        }

        /// <summary>替代行动这一手**要点的目标在哪一方**：`1 - p` = 敌方、`p` = 己方、
        /// `-1` = 不用点（正文里没有要玩家选的目标）。表现层拿它决定点亮哪半边棋盘。</summary>
        public static int AlternativeTargetSide(BattleContext ctx, int p, int slot, string keyword)
        {
            var u = ctx.Players[p].Board[slot];
            var ops = u != null ? u.Card.TriggerOps(keyword) : null;
            if (ops == null) return -1;
            var spec = EffectText.PickTarget(ops);
            if (spec == null) return -1;
            return spec.Side == "enemy" ? 1 - p : p;
        }

        // ==================================================================
        //  路标石收集（灵族）—— 原版 `PlayerActions.clickWaystone = 6`
        //  → `BattleActionType.useWaystone = 76` → 协程 `ResolveUseWaystone`
        // ==================================================================

        /// <summary>原版 `GameStaticData.waystoneGiveMana` = **1**：收集一颗灵魂石加几点。
        /// 出处：桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/GameStaticData.cs:323`，
        /// 值 = `GameStaticData__.cctor.c` 该槽 `+0x29c = 1`（**实读**）。
        /// 🔴 **全仓唯一出处** —— 别在表现层/别处再抄一个 `1`。</summary>
        public const int WaystoneGiveMana = 1;

        /// <summary>
        /// **这一格是不是一颗「可以收集的路标石」**（灵族的灵魂石）。**只判不执行** ——
        /// 表现层拿它决定「点这一下走收集、还是走攻击选择器」。
        ///
        /// **原版判据**（`d:/2/tools/decomp_full/BattleManager__CanUseWaystone.c`，**逐行读过**）：
        /// ① `card.isPlayer(+0x40) == isPlayerAsking`（**只能点自己那侧**）
        /// ② 提问方此刻允许行动（`+0x244 allowPlayerActionsFlag` / `+0x246 allowEnemyActionsFlag`）
        /// ③ `UIstate(+0x290) == normalBattle(1)`　④ `cardState(+0x228) == inPlay(2)`　⑤ 教程闸门。
        /// ⚠️ **它【不查】灵魂石余额、也【不查】这张卡有没有 600 能力** ——
        ///    那两条在**另一条链**上（打出牌时「从池里选一个」的 `CanUseSpiritStone`），两条**互不调用**。
        /// ⚠️ **也【不查】这个单位本回合行动过没有**：残骸本身恒为 `Exhausted`，
        ///    而收集在原版是**独立于单位行动的一个动作**（`PlayerActions` 里单开的一项）——
        ///    在这里加 `Exhausted` 判断会让**所有**灵魂石永远收不了（静默）。
        /// ⚠️ 我们这边**没有** `UIstate` 这个状态机（只有 `ctx.IsOver`），③ 没落地。
        /// </summary>
        public static int CanCollectWaystone(BattleContext ctx, int p, int slot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!BoardSpec.IsValid(slot)) return RuleCodes.ErrSlot;
            var u = ctx.Players[p].Board[slot];
            if (u == null) return RuleCodes.ErrNotUnit;
            if (!u.IsRemnant || !u.Has(KeywordTable.Waystone)) return RuleCodes.ErrNotWaystone;
            return RuleCodes.OK;
        }

        /// <summary>
        /// **收集一颗灵魂石**（玩家点了场上那颗**自己**留下的灵魂石）。
        ///
        /// 【读】原版顺序（`BattleManager._ResolveUseWaystone_d__480__MoveNext.c`）：
        /// `PlayerManager.AddSpiritStoneMana(收集方, waystoneGiveMana)` → `BroadcastCollectedSpiritStone`
        /// → `CollectSpiritStoneSignal(isPlayer, N, fromWaystone: true)` + `Signal.Raise`
        /// → `BattleManager.DestroyUnit(…, UnitDeathType.collectWaystone = 50, …)` → 记进战斗日志。
        /// ⚠️ **先加石、再销毁残骸** —— 顺序照抄原版；反过来的话，销毁残骸引发的那一串监听
        ///    会看到**还没加上**的余额。
        /// ⚠️ 收集发的是 `GainSpirit` 事件（与 `gainspirit` 那族同一个 kind）——
        ///    卡面写 `When you collect a Spirit Stone, …` 的那 4 张灵族单位靠它，不给那半句就是死的。
        /// </summary>
        public static int CollectWaystone(BattleContext ctx, int p, int slot)
        {
            int code = CanCollectWaystone(ctx, p, slot);
            if (code != RuleCodes.OK) return code;

            var ps = ctx.Players[p];
            var u = ps.Board[slot];
            ps.SpiritStones += WaystoneGiveMana;
            ctx.Log($"{ps.Name} 收集了 {u.Name} 留下的灵魂石 → 灵魂石 +{WaystoneGiveMana}"
                  + $"（现 {ps.SpiritStones}）");
            // ① 玩家资源那一条 —— **`Slot` 按 `GainFaith` 那族的约定写 -1**（那条链上三个事件
            //    都是「给玩家的、不属于任何格位」，见 `BattleEvent.GainFaith` 的注释）。
            //    卡面 `When you collect a Spirit Stone, …`（4 张灵族单位）认的是它。
            ctx.Signals.Add(new BattleEvent
            {
                Kind = EvtKind.GainSpirit, Player = p, Slot = -1, Amount = WaystoneGiveMana,
                Effect = "收集路标石", Turn = ctx.Turn,
            });
            // ② **格位上**那一条 —— 表现层靠它在那颗石头原来的位置播收集特效
            //    （原版 `RemnantAeldari.CollectWaystoneEffect`，出处见 `EvtKind.CollectWaystone`）。
            ctx.Signals.Add(new BattleEvent
            {
                Kind = EvtKind.CollectWaystone, Player = p, Slot = slot, CardId = u.Name,
                Amount = WaystoneGiveMana, Effect = "收集路标石", Turn = ctx.Turn,
            });

            // 石头被收走 ⇒ 那具残骸没了。走**正常那条「残骸被摧毁」**的路（`CleanupDeaths` 里
            // `if (u.IsRemnant)` 那一支：原地清空 + 那张卡进弃牌堆 + 重算光环）。
            u.Health = 0;
            CleanupDeaths(ctx, p, slot);
            return RuleCodes.OK;
        }

        /// <summary>🆕 2026-09-30：**嗜血「亮一下」的触发点** —— 原版 `CardScript.ActivateBloodThirst` 的等价物。
        ///
        /// **判据（亲读反编译，逐条）**：
        ///   · 原版那个「本回合攻击/行动计数」= `CardScript + 0x48` —— 写方逐条读过：
        ///     `_AttackMeleeAnim__MoveNext.c:116` +1 · `_ResolveRangedAttack__MoveNext.c:17` +1 ·
        ///     `_ResolveAttackRangedAnim__MoveNext.c:69` +1 · `FinishAttackActionWithoutMoving.c:13` +1 ·
        ///     `UsedActiveAbility.c:20` +1 · `ResetActions.c:30` −1 · `RemoveAttackThisTurn.c:6` −1 ·
        ///     `OnTurnEnd.c:209` = 0 · `CardSetup.c:116` = 0
        ///     ⇒ **就是我们这边的 `UnitState.AttacksThisTurn`**（同口径、同重置换算）。
        ///   · `CardScript.ActivateBloodThirst`：`HasCurrentTrait(0xdc=bloodThirst) &amp;&amp; (+0x48 == 1)
        ///     &amp;&amp; 是本方回合 &amp;&amp; (+0x228 != 5)` ⇒ 为真才 `SendHighlightBloodThirstAction`
        ///     （`ActivateBloodThirst.c`；另两个调用点 `FinishAfterAttack.c:83-99` · `UsedActiveAbility.c:21-23`
        ///     用的是同一个判据）。`SendHighlightBloodThirstAction` 把那一下排进动作队列，
        ///     最终跑 `CardScript.HighlightBloodThirst` → `BattleCardUI.HighlightBloodThirst` +
        ///     `DisplayTriggerAnim(card, 0xdc, 1, 0)`。
        ///   · ⇒ **语义 = 「带嗜血的单位本回合的计数刚变成 1 时亮一下」**（= 第一刀打完/第一次用能力之后，
        ///     那第二次攻击已经到手）。`+0x228 != 5` 那一档是原版的临时状态位，我们没有等价物 ⇒ 不判。
        ///
        /// ⚠️ **两个口径说明**（别当成原版事实）：① `0x48` 的含义是从**写方逐条读出来的**（上表），
        /// **不是**从字段名读到的；② 我们**多发一个事件、不发效果**（`effect: null`）——
        /// 原版那一下也只演出、不结算。
        ///
        /// ⚠️ **还缺一层（如实记，按铁律 11 是「要做」）**：原版还有
        /// `BattleCardUI.HighlightBloodThirst` 里那条 `DOFade(α, dur).SetLoops(6, Yoyo)` 的**脉冲**，
        /// 我们只做了「挂框」（`TraitFrames`）那一半。
        /// </summary>
        static void EmitBloodThirst(BattleContext ctx, int p, int slot, UnitState u)
        {
            if (ctx == null || u == null) return;
            if (!u.Has(KeywordTable.BloodThirst)) return;
            if (u.AttacksThisTurn != 1) return;          // 原版判据：`+0x48 == 1`（第 1 次之后才亮）
            // 格位**显式传**（`UnitState` 上没有格位字段 —— 与 `FireTriggerAt` 同一个理由：
            // 表现层要按「这一刻这一格」去反查视图）
            ctx.Emit(EvtKind.Trigger, p, slot, u.Name,
                     keyword: KeywordTable.BloodThirst, effect: null, amount: 0);
        }

        /// <summary>
        /// 触发一个效果 —— **所有触发都走这一个口子**（发事件 + 递归保护 + 结算）。
        /// 格位显式传入：反噬这类「单位已经离场」的触发，靠它才知道特效该播在哪。
        /// </summary>
        static bool FireTriggerAt(BattleContext ctx, UnitState u, string keyword,
                                  int owner, int slot, UnitState chosen = null)
        {
            // `FxOps` = 卡上原生的正文 **或** 运行时挂上去的那份（二选一，全仓只此一处判据）
            var ops = u != null ? u.FxOps(keyword) : null;
            var spec = u != null ? u.Effect(keyword) : null;
            // 没写效果 = 不触发（不是「触发了但没效果」）—— 卡上没这条就不该有反馈
            if (ops == null && spec == null) return false;

            // 递归保护：Rally 打死人 → 反噬 → 又打到带忏悔的单位 → …… 这类环**天生存在**
            // （规则书 :237「同时触发」那一节讲的就是它），靠深度上限截断
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{u.Name} 的 {keyword} 不再连锁");
                return false;
            }

            // 日志/事件要的那句话：① 有 op 就用**原文**（卡上原生的，没有就用**挂上去的那份原文**），
            // ② 否则用封闭文法那条的原文
            string what = ops != null ? (u.Card.TriggerText(keyword) ?? u.GrantedText(keyword)) : spec.Source;

            // 事件先发：表现层要的是「这一刻、这一格，有个触发发生了」，
            // 效果成不成立（比如对面场上没人可打）是另一回事
            ctx.Emit(EvtKind.Trigger, owner, slot, u.Name,
                     keyword: keyword, effect: what, amount: ops != null ? 0 : spec.Amount);
            ctx.Log($"{u.Name} 触发 {keyword.ToUpperInvariant()}：「{what}」");

            ctx.EffectChain++;
            if (ops != null) ResolveOps(ctx, owner, u, ops, keyword.ToUpperInvariant());
            else ResolveEffect(ctx, owner, u, spec, chosen);
            ctx.EffectChain--;

            // 🆕 **「关键词被触发」事件**（2026-09-13 候选 E）——
            //    卡面：`When a friendly unit triggers Mob, …` / `… uses Ferocity, …` /
            //          `When this unit triggers Synapse, …` / `… triggers Duty, …`。
            //
            // **收在这里的理由**：`FireTriggerAt` 是**所有触发唯一的出口**
            //   （见它的注释：「所有触发都走这一个口子」）—— 挂在这里，
            //   任何一个触发关键词**天然**就带上了这条广播，不用在每个时机点各补一行。
            //   ⇒ 以后新加触发关键词，`When … triggers <它>` 自动可用。
            //
            // ⚠️ **排在效果结算之后**（本工程原版反编译里看不到先后，这是**我们挑的**）：
            //    取「**这件事真的发生了、再通知听众**」—— 和 `Slay` 先于 `Strike` 是同一条理由
            //    （更具体的那件事先落定）。反过来放的话，听众会在触发者效果还没落地时就被叫醒，
            //    而听众的效果可能把触发者打死，后面的正文就会打在一个已经不在场上的单位上。
            //
            // ⚠️ **`owner` 显式传，不用 `BroadcastKeywordEvent`** —— 那一个靠 `OwnerOf(ctx,u)`
            //    反查，而这里 `u` 刚被自己的效果打死是**常有的事**（反噬 / 不稳定那一类），
            //    反查会得到 `-1`（不在场上）⇒ 带方向的监听器**静默一次都不响**。
            //    这个 `owner` 是调用方给的、触发那一刻的事实，不受结算影响。
            if (u != null && u.Card != null)
                BroadcastWhen(ctx, WhenEventKind.Triggers(keyword), owner, u.Card, u);

            return true;
        }

        /// <summary>单位还在场上时触发（自己找格位）</summary>
        static bool FireTriggerOnBoard(BattleContext ctx, UnitState u, string keyword)
        {
            int owner, slot;
            if (!FindUnit(ctx, u, out owner, out slot)) return false;
            return FireTriggerAt(ctx, u, keyword, owner, slot);
        }

        /// <summary>
        /// **典籍（Codex）的自动触发点** —— 「**打出任何一张牌之后**，你的能量**恰好为 0**
        /// ⇒ 触发**一个**带 `Codex` 的单位的正文」。
        ///
        /// **出处**（⚠️ 2026-10-17 订正：原来写「**原版出处**」—— 错，`rule_core.gd` 是
/// **我们自己的 Godot 复刻**、不是原版）：我们自己的参考实现
/// `d:/warpforge/scripts/rule_core.gd:2397 _check_codex`（旁证、非权威）。
        /// 两条语义**照抄，没有自己发挥**：
        ///   ① 判据是 `energy == 0`（**恰好**为 0 —— 那边写的就是 `== 0`，不是 `&lt;= 0`）；
        ///   ② 扫 0→8 号格，命中**第一个**就 `break` ⇒ **一次只触发一个单位**，不是全体各来一次。
        /// 调用点也照那边三个来（`rule_core.gd:2183` 战术打完 · `:2244` 虫群合并之后 ·
        /// `:2337` 单位部署完）：我们把三条路各自汇到 <see cref="PlayCard"/> 与
        /// `PlayTactic` 的**末尾**各调一次（虫群合并与普通部署本来就都在 `PlayCard` 里）。
        ///
        /// ⚠️ **为什么非有它不可**（2026-09-14 A5 补）：`Codex:` 的正文原来**只有**
        ///    `Author of the Codex` 那一类「**强行**触发」会消费，**没有任何自动触发点**
        ///    ⇒ 20 张带 `Codex` 的卡（10 张写前缀 + 10 张裸写）的正文**一条都不会自己发生**，
        ///    而且**报表上看不出来**（解析得了、载荷也有机制）—— 正是「覆盖率绿了、机制没跑」。
        ///
        /// ⚠️ 条件那一半在 op 上（`EffectText.MarkCodexCondition` 挂的 `EnergyZero`）：
        ///    这里只在「能量为 0」时才调，所以条件**自然成立**；`Author of the Codex` 那种
        ///    「能量不为 0 也要触发」走的是 `ForcedTriggerDepth`（见 `ConditionHolds`）。
        /// </summary>
        public static void CheckCodex(BattleContext ctx, int p)
        {
            if (ctx == null || ctx.IsOver) return;
            if (p < 0 || p >= ctx.Players.Length) return;
            if (ctx.Players[p].Energy != 0) return;              // ① 恰好为 0
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[p].Board[s];
                if (u == null || !u.Has(KeywordTable.Codex)) continue;
                FireTriggerAt(ctx, u, KeywordTable.Codex, p, s);
                break;                                           // ② 一次只触发第一个
            }
        }

        /// <summary>
        /// **强行触发**区间开始 —— 这段区间里 `ConditionKind == EnergyZero`（`Codex:` 的那个条件）
        /// **一律判成立**。见 <see cref="BattleContext.ForcedTriggerDepth"/> 的完整说明。
        /// ⚠️ **必须与 <see cref="EndForcedTrigger"/> 配对**（用计数器，可嵌套 —— `Duty's End` 的
        ///    `Backlash:` 正文本身就是一句强行触发）。
        /// </summary>
        public static void BeginForcedTrigger(BattleContext ctx)
        {
            if (ctx != null) ctx.ForcedTriggerDepth++;
        }

        /// <summary>**强行触发**区间结束（与 <see cref="BeginForcedTrigger"/> 配对）。</summary>
        public static void EndForcedTrigger(BattleContext ctx)
        {
            if (ctx != null && ctx.ForcedTriggerDepth > 0) ctx.ForcedTriggerDepth--;
        }

        /// <summary>
        /// **强行触发**某个单位身上某个关键词的正文 —— 「不等它自己的时机，现在就结算一遍」。
        ///
        /// 卡面（2026-09-14 A4 批 4 实测量出来的两族写法，共 8 张卡）：
        ///   · 带目标 —— `Trigger the Codex ability of a friendly unit`（`Author of the Codex`）·
        ///     `trigger the codex ability of all friendly units`（`Duty's End`）·
        ///     `Trigger the Ambush abilities of all friendly troops`（`Atalan Leader`）
        ///   · 尾句 —— `… and trigger their &lt;X&gt; abilities`（`Regimental Doctrine` / `Deathwing Assault` /
        ///     `Open Insurrection` / `Stomp Em` / `Codex Discipline`）
        ///
        /// **为什么走 <see cref="FireTriggerAt"/> 而不另写一遍**：那个函数是**所有触发唯一的出口**
        ///   —— 日志、`EvtKind.Trigger` 事件、递归保护、以及「关键词被触发」那条广播
        ///   （`When you trigger the Codex ability, …` / `Avenging Zeal` 就靠它）全在那一处。
        ///   绕开它 = 那些东西**静默不发生**。
        ///
        /// ⚠️ **`forced` 只对 `EnergyZero` 那一类条件有效**（`ConditionHolds` 里读深度）。
        ///    别的条件（`if …`）照旧判。
        /// ⚠️ **没有正文/没有授予的效果** ⇒ 返回 false、**什么都不做** —— 调用方要如实报出来，
        ///    别让「触发了」和「触发了但那条关键词这张卡上压根没有」长得一样（红线）。
        /// </summary>
        public static bool TriggerKeywordOf(BattleContext ctx, UnitState u, string keyword, bool forced)
        {
            if (ctx == null || u == null || u.Card == null || keyword == null) return false;
            if (!u.HasFx(keyword)) return false;
            int owner, slot;
            if (!FindUnit(ctx, u, out owner, out slot)) return false;   // 不在场上了

            if (forced) BeginForcedTrigger(ctx);
            FireTriggerAt(ctx, u, keyword, owner, slot);
            if (forced) EndForcedTrigger(ctx);
            return true;
        }

        /// <summary>
        /// 给**某一方场上所有带这个触发关键词的单位**各触发一次。
        ///
        /// 为什么要它：<see cref="FireTriggerOnBoard"/> 只点**一个**单位，而有的触发是
        /// 「**你这边**发生了某件事」—— `Cruelty`（敌方挨打未死 → **己方**带该词的牌各响一次）
        /// 的收听者就是**一整排**。
        ///
        /// ⚠️ **先快照格位再触发**：触发会改棋盘（能打死人、能再部署），
        ///    边遍历边读数组是未定义行为 —— 和 `ResolveDeploy` / `BroadcastWhen` 同一条教训。
        /// ⚠️ 只在**有正文**（`TriggerOps`）**或**有授予的效果（`Effect`）时才收进快照 ——
        ///    否则每挨一次打都要为整排白跑一遍。
        /// </summary>
        static void FireTriggerOnSide(BattleContext ctx, int side, string keyword)
        {
            FireTriggerOnSide(ctx, side, keyword, null);
        }

        /// <summary>
        /// 同上，但**排除一个单位**（`except`）。给「起义（`Uprising`）」用 ——
        /// 它的判据是「**本单位之后**部署的部队」（规则书 `:222`），所以刚落地的那一个不算。
        /// </summary>
        static void FireTriggerOnSide(BattleContext ctx, int side, string keyword, UnitState except)
        {
            var slots = new List<int>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var t = ctx.Players[side].Board[s];
                if (t == null || !t.IsAlive || t.Card == null) continue;
                if (except != null && ReferenceEquals(t, except)) continue;
                if (!t.HasFx(keyword)) continue;
                slots.Add(s);
            }
            foreach (int s in slots)
            {
                var t = ctx.Players[side].Board[s];
                if (t == null || !t.IsAlive) continue;      // 快照之后可能已经被前一个打死了
                FireTriggerAt(ctx, t, keyword, side, s);
            }
        }

        /// <summary>
        /// **不稳定**（`Unstable`）：本单位死亡时，对**场上随机一个单位（含双方）**造成 **1-3** 伤害。
        /// 规则书 `:221`；目标池与伤害范围照我们自己的 `rule_core.gd:4578 _unstable_blast`（旁证、非权威）。
        ///
        /// ⚠️ **随机源必须是 `ctx.Rng`** —— 同一局必须可复现（工程铁律，不许用 `UnityEngine.Random`）。
        /// ⚠️ 打死了会**再走一遍 `CleanupDeaths`**（链式自爆），靠 `ctx.EffectChain` 截断 ——
        ///    调用点那道 `ctx.EffectChain &lt; MaxEffectChain` 守卫**是连锁保护，别删**。
        /// </summary>
        static void UnstableBlast(BattleContext ctx)
        {
            var cands = new List<int>();      // 编码成 owner * Size + slot，省一个元组类型
            for (int q = 0; q < 2; q++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var t = ctx.Players[q].Board[s];
                    if (t != null && t.IsAlive) cands.Add(q * BoardSpec.Size + s);
                }
            if (cands.Count == 0) return;

            int pick = cands[ctx.Rng.Next(cands.Count)];
            var target = ctx.Players[pick / BoardSpec.Size].Board[pick % BoardSpec.Size];
            int dmg = 1 + ctx.Rng.Next(3);                 // 1..3

            ctx.EffectChain++;
            int dealt = Hurt(ctx, target, dmg, "Unstable");
            ctx.EffectChain--;
            ctx.Log($"不稳定：{target.Name} 挨了 {dealt} 点（随机自爆）");
        }

        /// <summary>
        /// **天赋**（`Talent`）：回合开始时，把**卡池里那张同名战术卡**塞进本方手牌。
        /// 规则书 `:218`「回合开始时在手牌中生成临时战术」。
        ///
        /// **和别的关键词最不一样的一点**：它的效果**不是卡面正文**，而是「**按名字去卡池查一张卡**」——
        /// 实测 **80 个天赋名里 72 个查得到同名卡**，而且**全是 `tactic`**
        /// （`Witchfire` / `Path of the Seer` / `Flickerjump` / `Wrath of Khaine` …）。
        ///
        /// ⚠️ **生成的牌必须自己调 `MarkEphemeral`** —— 规则书 `:229` 把「天赋生成的牌」与
        ///    伴生 / 潮涌复制并列为**临时**，而**多数天赋卡面并没有印 `Ephemeral`**
        ///    （`Witchfire` 的卡面是 `Deal 1-3 damage. (1) Repeat this effect`）。
        ///    不标记 = 它会**赖在手里不走**（该走的不走，是本工程的红线之一）。
        ///    `CardDef.Ephemeral` 那条注释末尾写着「三个来源还没做，它们生成的牌要自己调 `MarkEphemeral`」
        ///    —— 这里就是兑现它。
        ///
        /// ⚠️ **查不到同名卡时如实打日志**，不静默（那 8 个名字列在 `CardDef.TalentName` 的注释里）。
        /// ⚠️ **一格一张**：几个带天赋的单位就生成几张 —— 卡面写的是「每个天赋…」。
        /// </summary>
        /// <summary>天赋名是**池子**（`A random &lt;阵营> &lt;子类型>`）时从池里抽一张。
        ///
        /// 🔴 **判据** → `资料/阶段二_卡片详情窗_原版规格.md` **§9·2 第三种形式**：`BL36 Sorcerer` /
        /// `BL5 Sylar Hexcorn` 的 `Talent: A random Black Legion Psychic Power` 是**一条真形式**，
        /// **不是正则误抓**（那个定性 2026-09-26 已订正）。原来没有这一支 ⇒ 它俩的天赋**从来没生效过**。
        ///
        /// ⚠️ **解池子复用 `CreatePool.FilterChoose`**（阵营词/兵种词/`non-legendary`/费用那套判定**只此一处**）
        /// —— 别在这儿另写一份筛选（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// ⚠️ **抽哪一张用 `ctx.Rng`**（只用 `System.Random(seed)` ⇒ 对局可复现）。
        /// ⚠️ **只认 `random …` 这一种写法**：其余查不到同名卡的仍走原来那条「如实打日志」的路 ——
        /// 乱兜底会把「真查不到」变成「悄悄抽了一张」。
        /// </summary>
        static CardDef PickTalentFromPool(BattleContext ctx, string talentName, out string why)
        {
            why = null;
            if (ctx == null || string.IsNullOrEmpty(talentName)) { why = "没写天赋"; return null; }
            // 🔴 **解池子只此一处**（`CreatePool.PoolFromPhrase`）—— 卡片详情窗显示「相关卡」时也走它，
            //    剥词/阵营判定/兵种判定**不写第二份**（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            // ⚠️ `randomOnly: true`：`Choose a …` 那一种是**玩家挑**，这里**不许替玩家自动挑**
            //    （那是另一条链 `EffectResolver.TakePickCard`）。
            var list = CreatePool.PoolFromPhrase(ctx.CardPool, talentName, true, out string what, out why);
            if (list == null) return null;
            var pick = list[ctx.Rng.Next(list.Count)];
            ctx.Log($"天赋池「{talentName}」（筛选词 `{what}`）解出 {list.Count} 张 ⇒ 按种子抽中「{pick.Name}」");
            return pick;
        }

        static void SpawnTalents(BattleContext ctx, int side)
        {
            if (ctx == null || ctx.IsOver) return;
            var ps = ctx.Players[side];
            int made = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                if (u == null || u.Card == null || u.Card.TalentName == null) continue;

                // ① 天赋名就是**一张卡的名字**（`Talent: Author of the Codex`）
                var c = CreatePool.FindByName(ctx.CardPool, u.Card.TalentName);
                bool fromPool = false;
                if (c == null)
                {
                    // ② 🆕 天赋名是个**池子**（`Talent: A random Black Legion Psychic Power`）
                    c = PickTalentFromPool(ctx, u.Card.TalentName, out string poolWhy);
                    fromPool = c != null;
                    if (c == null)
                    {
                        ctx.Log($"{ps.Name} 的「{u.Name}」天赋「{u.Card.TalentName}」"
                              + $"在卡池里查不到同名卡，也不是能解的池子（{poolWhy}）—— **这条没生效**");
                        continue;
                    }
                }
                var talent = ctx.NewInstance(c);   // 天赋生成 = **新造一张**（第 7 行第 2 步）
                talent.EphemeralMarked = true;     // 临时：回合结束还没打就移出游戏（`SweepEphemeral`）
                ps.Hand.Add(talent);
                SetupCardInHand(ctx, side, talent);   // 🆕 `A885` ②（天赋卡是战术 ⇒ 今天恒空过，机制留着）
                made++;
                ctx.Log($"{ps.Name} 的「{u.Name}」天赋{(fromPool ? "是池子 ⇒ **抽到**" : "生成了")}「{c.Name}」（临时卡）");
            }
            if (made > 0) EnforceHandLimit(ctx, side);
        }

        /// <summary>
        /// 结算一个效果。**这是效果的唯一出口** —— 主动技能和触发都调它。
        /// </summary>
        /// <param name="chosen">调用方已选定的目标单位（目标栏写 `EnemyUnit` 时才有）</param>
        static void ResolveEffect(BattleContext ctx, int owner, UnitState source, EffectSpec spec, UnitState chosen)
        {
            string by = source != null ? source.Name : "效果";

            switch (spec.Verb)
            {
                case "damage":
                {
                    var t = ResolveTarget(ctx, owner, source, spec.Target, chosen);
                    if (t == null)
                    {
                        ctx.Log($"{by} 的效果「{spec.Source}」没有合法目标，空过");
                        return;
                    }
                    int dealt = Hurt(ctx, t, spec.Amount, by + " 的效果");
                    ctx.Log($"{by} 的效果「{spec.Source}」对 {t.Name} 造成 {dealt} 伤（剩 {t.Health}）");
                    break;
                }

                case "heal":
                {
                    var t = ResolveTarget(ctx, owner, source, spec.Target, chosen);
                    if (t == null) return;
                    int before = t.Health;
                    // 不超过上限 —— 否则「治疗」会变成变相溢出伤害的储备
                    t.Health = Math.Min(t.MaxHealth, t.Health + spec.Amount);
                    ctx.Log($"{by} 的效果「{spec.Source}」治疗 {t.Name}：{before} → {t.Health}");
                    break;
                }

                case "draw":
                    // ⚠️ `Draw(ctx, p)` 一次只抽 1 张（和 rule_core 一致），数量靠自己循环
                    for (int i = 0; i < spec.Amount && !ctx.IsOver; i++) Draw(ctx, owner);
                    ctx.Log($"{by} 的效果「{spec.Source}」抽了 {spec.Amount} 张");
                    break;
            }
        }

        /// <summary>
        /// 效果的目标是谁。
        ///
        /// ⚠️ `EnemyUnit` 在没有「选目标」这一步的**触发类**效果里，取敌方**槽号最小**的活部队 ——
        ///    **定死规则，不掷骰**：种子只该影响洗牌，同一局必须永远可复现。
        ///    对面场上一个部队都没有时效果**空过**，不自动改打督军（那是替玩家做决定）。
        /// </summary>
        static UnitState ResolveTarget(BattleContext ctx, int owner, UnitState source, string target, UnitState chosen)
        {
            switch (target)
            {
                case EffectTargets.Self:
                    return source;
                case EffectTargets.OwnWarlord:
                    return ctx.Players[owner].Warlord;
                case EffectTargets.EnemyWarlord:
                    return ctx.Players[1 - owner].Warlord;
                case EffectTargets.EnemyUnit:
                {
                    if (chosen != null && chosen.IsAlive && !chosen.IsWarlord) return chosen;
                    var b = ctx.Players[1 - owner].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                        if (b[s] != null && !b[s].IsWarlord && b[s].IsAlive) return b[s];
                    return null;
                }
            }
            return null;
        }

        // ==================================================================
        //  胜负
        // ==================================================================

        /// <summary>
        /// 投降 / 判弃权（原版 `BattleResult.Forfeit` / `Disconnect`）：**立刻**判对方胜，不看督军血量。
        /// 🆕 **2026-10-18（A913）**：是**哪一档**由第三个形参 `reason` 说清（原版 `DeadHero(_,_,BattleResult)`）。
        ///
        /// ⚠️ 和 `CheckWinner` 的关系：那边是「督军倒下才算」，这边是玩家自己认输 ——
        ///    所以**不复用** `CheckWinner`（它见到两边都没倒会返回 0，等于投降无效）。
        ///    判过了就不再改（和 `CheckWinner` 一样：**胜负只判一次**）。
        /// 单机没有对手可以投降（AI 不投降），所以这条路只有玩家走得到。
        /// </summary>
        /// <param name="reason">🆕 **2026-10-18（A913）理由码** —— 原版
        ///   `BattleManager.DeadHero(bool isPlayerDying, BattleResult battleResult)` 的**第二个实参**。
        ///   <para>默认 = <see cref="BattleResult.Forfeit"/>(2)：本方法**今天就是这个入口** ——
        ///   名字、玩家那颗「投降」钮、既有调用点说的都是「有人投降」（给默认值是为了不动
        ///   判不出理由的那几处）。</para>
        ///   <para>⚠️ **凡是知道理由的调用点都要显式传**：掉线/重连失败那几条传
        ///   <see cref="BattleResult.Disconnect"/>(3)（原版那两条就是 `DeadHero(_,3)`）；
        ///   对面点投降传 2（原版 `ReceiveEnemyForfeit.c:37`）。</para>
        ///   <para>⛔ **判不出就别猜** —— 显式传 <see cref="BattleResult.Undefined"/>，
        ///   日志里会写明「未指定」，自检也认得出（拿 2 顶替 = 静默说错一句话，红线）。</para>
        ///   <para>🔴 码**只进 `ctx.Events` 那条日志**（`ctx.Log`，我们自己的诊断通道，不是
        ///   `ctx.ActionLog` 那本给玩家看的战斗日志），**不进 `ctx` 的任何状态** ——
        ///   原因见 <see cref="BattleResult"/>（指纹/回放零风险）。</para>
        /// </param>
        public static int Forfeit(BattleContext ctx, int player, BattleResult reason = BattleResult.Forfeit)
        {
            if (ctx.Winner != 0) return ctx.Winner;          // 已经结束了，投降不作数
            if (player != 0 && player != 1) return 0;        // 越界就什么都不做（调用方的问题）
            // 🆕 2026-10-17（B29）：**教程关可以禁止投降**（`TutorialStage.preventPlayerResign`）。
            //   原版出处：`TutorialStage` 字段 `preventPlayerResign`（`dump.cs:42080`，偏移 `+0xB9`）
            //   —— 6 关实测**全是 false**（`tutorial_stages.json`）⇒ 今天一次都不触发，**机制照做**（铁律 11）。
            //   ⚠️ 判据只有这一处：⛔ 别在 UI 那一层再挡一道（两处写同一条规则 = 迟早不一致）。
            //   原版同一支：`ClickExitBattle.c:59` 那个 `if (*(char *)(stage + 0xb9) == '\0')`
            //   —— 真 ⇒ 才走 `DeadHero(我, 2)`；假 ⇒ `LogWarning` + **不判负**（正是我们这一支）。
            if (ctx.Tutorial != null && ctx.Tutorial.ResignBlocked)
            {
                ctx.Log($"{ctx.Players[player].Name} 想投降 —— **这一关不允许投降**"
                      + $"（关卡 `preventPlayerResign`）⇒ 挡下，对局继续"
                      + $"（原版理由码 `BattleResult.{reason}` = {(int)reason}，这一支**没有**走到 `DeadHero`）");
                return ctx.Winner;                           // = 0（什么都没发生）
            }
            ctx.ForfeitedBy = player;
            ctx.Winner = player == 0 ? 2 : 1;
            // 🔴 **码与文案分开写**（同 `CLAUDE.md`/本批反复强调的那条：不许拿给人看的句子当逻辑）：
            //   ① **机器可读**那一行 —— 稳定 token，自检认它；换措辞、换语言它都不变。
            ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}");
            //   ② **给人看**那一行 —— 措辞按码分档（`BattleResultWord`）。
            //   ⚠️ 理由码 == `Forfeit`(2) 时这句与改动前**逐字相同**（既有断言照旧绿）。
            ctx.Log($"{ctx.Players[player].Name} {BattleResultWord(reason)} —— {ctx.Players[1 - player].Name} 获胜");
            return ctx.Winner;
        }

        /// <summary>理由码 → **给人看**的那半句（`BattleResult` 的中文说法）。
        /// 🔴 **它只用于文案**：逻辑一律直接比 <see cref="BattleResult"/> 本身，⛔ 别拿这个串当判据
        /// （换措辞/换语言会静默失效 —— 本批已经栽过四次的那个模式）。</summary>
        public static string BattleResultWord(BattleResult r)
        {
            switch (r)
            {
                case BattleResult.BattleVictory: return "判负（督军倒下）";
                case BattleResult.Forfeit:       return "投降";            // ← 与改动前逐字相同
                case BattleResult.Disconnect:    return "掉线判弃权";
                case BattleResult.WinButton:     return "跳过（调试判胜）";
                case BattleResult.Cancelled:     return "取消匹配";
                case BattleResult.DrawButton:    return "调试判平";
                default:                         return "弃权（**理由未指定**）";
            }
        }

        /// <summary>0 = 进行中，1/2 = 该方胜，3 = 平局。（rule_core.check_winner）</summary>
        public static int CheckWinner(BattleContext ctx)
        {
            if (ctx.Winner != 0) return ctx.Winner;   // 已经判过了 —— 别再刷日志

            bool d0 = ctx.Players[0].IsDefeated;
            bool d1 = ctx.Players[1].IsDefeated;

            // 🆕 2026-10-17（B29）：**教程关的 `playerAlwaysWins`** —— 「战斗结束时**一律算玩家赢**」。
            //   原版出处 = `BattleManager.GetWinnerAfterBattleEnd`（`:63-66`）：
            //     `if (IsCampaignLike(bm)) { var st = GetCurrentTutorialStage(bm); if (st != null && st + 0xB8 != 0)
            //        { CustomDebug.LogWarning(…); uVar6 = 10; } }`   // 10 = 玩家胜
            //   位置**很关键**：它在**原来那套「谁死了谁输」算完之后**覆盖结果
            //   ⇒ ⛔ **不是「一开局就判玩家赢」**（那会把教程局当场结算掉）。
            //   ⚠️ 6 关实测**全是 false** ⇒ 今天一次都不触发，**机制照做**（铁律 11）。
            //   🔴 唯一读点 = 这里（`TutorialScript.PlayerAlwaysWins`）—— 别在别处再判一遍。
            if ((d0 || d1) && ctx.Tutorial != null && ctx.Tutorial.PlayerAlwaysWins)
            {
                ctx.Winner = 1;
                ctx.Log("★ 教程关：本关 `playerAlwaysWins` ⇒ **判玩家胜**（原版 `GetWinnerAfterBattleEnd`）");
                return ctx.Winner;
            }

            if (d0 && d1)
            {
                ctx.Winner = 3;
                ctx.Log("双方督军同时倒下 —— 平局");
            }
            else if (d0)
            {
                ctx.Winner = 2;
                ctx.Log($"{ctx.Players[1].Name} 获胜（{ctx.Players[0].Name} 的督军倒下）");
            }
            else if (d1)
            {
                ctx.Winner = 1;
                ctx.Log($"{ctx.Players[0].Name} 获胜（{ctx.Players[1].Name} 的督军倒下）");
            }
            return ctx.Winner;
        }

        // ==================================================================
        //  诊断
        // ==================================================================

        /// <summary>
        /// 这套牌里**有、但 v1 不结算**的关键词。用来量化「还差多少才像原版」，
        /// 而不是让它们静默失效。
        /// </summary>
        public static List<string> UnimplementedKeywords(IEnumerable<CardDef> cards)
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            if (cards == null) return new List<string>();

            foreach (var c in cards)
            {
                if (c == null) continue;
                foreach (var kv in c.Keywords)
                    if (!KeywordTable.Implemented.Contains(kv.Key)) found.Add(kv.Key);
            }
            return new List<string>(found);
        }

        /// <summary>
        /// 带了效果文字、但**文法解析不出来**的卡，逐条列成「卡名 · 关键词 · 原文」。
        ///
        /// 和 <see cref="UnimplementedKeywords"/> 是两件事：
        ///   前者问「**关键词**实现了没有」，这条问「**这张卡的效果**能不能真的跑」。
        ///   `rally` 已经实现了，但一张原版卡的 `Rally: &lt;一大段中文&gt;` 照样跑不了 ——
        ///   那种卡必须被看见（卡面标 `*`），不能装作它会结算。
        /// </summary>
        public static List<string> UnparsedEffects(IEnumerable<CardDef> cards)
        {
            var found = new List<string>();
            if (cards == null) return found;

            var seen = new HashSet<string>();
            foreach (var c in cards)
            {
                if (c == null) continue;
                foreach (var s in c.UnparsedEffects)
                {
                    string line = c.Name + " · " + s;
                    if (seen.Add(line)) found.Add(line);
                }
            }
            return found;
        }
    }
}
