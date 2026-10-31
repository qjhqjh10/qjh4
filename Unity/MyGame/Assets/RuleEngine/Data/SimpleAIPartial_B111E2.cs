// SimpleAIPartial_B111E2.cs — `SimpleAI` 的兄弟分册：**AI 的「平 / 誓二选一」按原版 bot 支【等概率随机】**
//
// 🆕 2026-11-01（第一百一十一会话 · 执行代理 `B111_E2` · 账 `B-69`，接 `B110_R3` 的 `N07`）。
//
// 为什么单开一个文件：`Data/SimpleAI.cs` 被账按 `文件:行号` 引用 41 处 ⇒ 往类体里插成员会让下游
//   整体漂移。本批在那边只有**一处 1→1 的行替换**（`ExecuteActionCore` 的 `PlayCard` 支改成调
//   `PlayCardAsAi`），新成员全部住在这里（先例 = `Data/SimpleAI.ActiveEntry.cs`）。
//
// ==================================================================================
//  判据（三源交叉，2026-11-01 现读；⛔ 不要重新论证）
// ==================================================================================
// ① **什么情况下原版【随机】挑**（不是「付得起就付」）：
//    `decomp_full/BattleManager__ChoiceOfCardEnemy.c:18-31`
//      `cVar1 = IsNetworkedGame(this);`
//      `if (!networked) { if (actingCard == 0) goto 崩; if (actingCard.isPlayer != 0) goto bVar5=false;
//                        bVar5 = (get_matchType(this) != 0xa0); }  else bVar5 = false;`
//    ⇒ `flag2`（= ILSpy 里的 `isBotAndNotReplay`）= **非联网 ∧ 行动卡不是本机玩家的 ∧ 不是回放**。
//      `MatchType.Replay = 160 (0xA0)`（`out_il2csharp/Assembly-CSharp/MatchType.cs:17`）。
//    `out_il2csharp/Assembly-CSharp/BattleManager.cs:8951-8965`（同一处，ILSpy）：
//      `int num1 = selectedCard; if (flag2) { var list12 = GetChoiceFullPool(...); num1 = list12.IndexOf(list11[0]); }`
//    ⇒ **bot 拿的是「随机抽出来的第一张」**（`options[0]`），不是面板答案。
//    ⚠️ 联网 / 回放时 `flag2 == false` ⇒ 走 `actionValueTens`（报文里的那个下标，**不掷骰**）。
// ② **怎么抽、抽几次**（`decomp_full/BattleManager__GetChoiceOptions.c:40-70`，ILSpy `:8976-9005`）：
//      `iVar1 = min(fullPool.Count, choicesDefinition.maxTargets)`;
//      循环 `iVar1` 圈：`idx = GetRandomInt(0, fullPool.Count)` → `options.Add(pool[idx])` → 从池里删掉。
//    `GetRandomInt` = **预烧表按下标取**（`MatchData.GetRandomInt`，见 `Core/BattleContextRandom.cs`），
//      且它有一条「`maxValueExcl <= minValueIncl + 1` ⇒ 返回 min、**不消耗随机数**」（同文件 `:65`）。
//    池内 10 张 oath 卡**逐张现读** `choicesMaxTargets = 2`、`choicesSpecificCardIds.Count = 2`
//    （`Resources/cards_engine.json`）⇒ `iVar1 = 2`：
//      第 1 圈 `GetRandomInt(0, 2)` ⇒ **消耗 1 格**（`v % 2` ⇒ `[0,1]` 各 500/1000 ⇒ **严格等概率**）；
//      第 2 圈 `GetRandomInt(0, 1)` ⇒ 命中「不消耗」那一档 ⇒ **净消耗 = 1 格**。
//    ⇒ 我们这边**一次 `ctx.GetRandomInt(0, 2)`** 就等价（⛔ 不是「每圈掷一次」）。
//    ⚠️ ILSpy `:8993` 把这一句渲染成**两次** `GetRandomInt`（`(GetRandomInt(...) >= size) ? 0 : GetRandomInt(...)`）
//       —— 那是反编译重复求值；`.c` 里只有**一次**调用 + 一句 `if (Count <= iVar3) iVar3 = 0;`。**以 `.c` 为准**。
// ③ **池序 = 按 `uniqueId` 字符串升序**（`decomp_full/BattleManager__GetChoiceFullPool.c:55-70`；ILSpy `:9138-9154`
//    的 `Enumerable.Where(_GetChoiceFullPool_b__488_0) → OrderBy(_GetChoiceFullPool_b__488_1)`，
//    谓词 = `cards.uniqueId > 0`，键 = `cards.uniqueId`）。
//    ⇒ 「誓约变体」在池里的下标**逐卡不同**：池内 10 张逐张现读（`cards_truth.json` 比较两个变体的
//      `manaCost` / abilities —— 贵的那张 = 带誓约效果那张 = `choicesSpecificCardIds[1]`）：
//        **誓约变体排在 0**：UM109 `Adaptive Strategy` · `Angel's Wrath` · `Catechism of Death` ·
//                          UM112 `Champions of Humanity` · UM116 `No Mercy` · UM117 `No Respite` ·
//                          UM115 `Stormhawk Interception`（现读 7 张）
//        **誓约变体排在 1**：UM110 `Fall Back` · UM118 `Indomitus Crusade` · UM111 `Shall Know No Fear`（现读 3 张）
//      ⇒ 必须**按卡算**，⛔ 不能写死「`idx == 1` 就是誓约」。
//
// ==================================================================================
//  🔴 2026-11-01（`B111_E1B` · `B-85`/`B-86`）：**掷骰已挪进引擎**（本节就地改写，旧文见 git log）
// ==================================================================================
//  · 引擎侧 `RuleCore.DecideOathChoice`（`Core/EffectResolver.cs`）现在是**唯一的掷骰点**：
//      三道闸全过 ⇒ **无条件掷一次**（原版 `GetChoiceOptions` 那一格，玩家 / AI / 回放三条支都走它）；
//      有答案用答案（玩家面板 / 回放报文），**没答案就用掷出来的那一张**（= 原版 bot 支的 `options[0]`）。
//  · 本分册**不再是生产端**：`PlayCardAsAi` 出牌**之后** `ctx.TryPeekLastOathDecision(me, …)`
//      peek「这一次**最后用了**的答案」，写进 `AiAction.OathChoiceRecorded/OathChoice`
//      （`Data/AiActionPartial_B110R3.cs`）⇒ `NetProtocol.ToWire` → `MsgAction` → 录像 / 回放 / 对端。
//  · ✅ **旧残余已消除**：原来 AI 在 `SimpleAI` 掷（消耗 1 格）、而回放不走 `SimpleAI`（0 格）
//      ⇒ 此后每次 `ctx.GetRandomInt` 错开 1 格。现在玩家手 / AI 手 / 回放落地三条路都走引擎那一点 ⇒ 一致。
//  · ⚠️ `DecideOathForAi` 保留成**自检 / 诊断用的独立掷骰口**（`B111_E2_Assert.cs` 的 B/C/D/E/F 在用）；
//      产品路径⛔ 别调它 —— 会与引擎那一掷重复、一手烧掉 **2 格**。
//
//  ⚠️ 与 `AiAction.AltKeyword == "oath"`（**单位主动技**那条路，`RuleCore.UseOathAbility`）**不是一回事**。
using System;

namespace RuleEngine
{
    public static partial class SimpleAI
    {
        /// <summary>
        /// **池下标 → 「这一张是不是誓约变体」** —— 🆕 2026-11-01（`B111_E1B`）：实现已搬到引擎侧
        /// `RuleCore.OathPoolIndexIsOathVariant`（`Core/EffectResolverPartial_B111E1B.cs`），
        /// 本方法**只做门面转发**（既有调用点 —— 含 `CardPresentation/Editor/B111_E2_Assert.cs` —— 一个字都不用改，
        /// 而 `Core` 也不必反过来依赖 `Data`）。
        ///
        /// <para>判据（原版 `GetChoiceFullPool` 的池序 = `OrderBy(uniqueId)` 字符串升序）与为什么必须用
        /// `CompareOrdinal` → 那个方法的头注，⛔ 别在这里抄第二份。</para>
        /// </summary>
        public static bool OathPoolIndexIsOathVariant(CardDef card, int poolIndex)
            => RuleCore.OathPoolIndexIsOathVariant(card, poolIndex);

        /// <summary>
        /// **AI 这一手「平 / 誓二选一」掷一次**（原版 `ChoiceOfCardEnemy` 的 bot 支）：
        /// 等概率从池里抽一张（预烧随机表、**消耗 1 格**），结果摆进 `ctx` 并盖到动作上。
        ///
        /// <para>返回 `true` = 掷了（= 原版这一次**会问池**）；`false` = 原版这次根本不问，一格随机都不该消耗。
        /// <paramref name="pay"/> 只在返回 `true` 时有效。</para>
        ///
        /// <para>三道闸与 `RuleCore.DecideOathChoice`（`EffectResolver.cs:18004-18042`）**逐条同序同参**：
        /// ① `OathChoiceNeeded`（是不是「平 / 誓」那种战术卡）·
        /// ② `NeedsToChooseFromPool`（原版那道池闸：不是这一侧回合 / 带 jam / 法力不够付誓约 ⇒ 不问）·
        /// ③ `CanPayOathChoice`（`CurrentCost + oathValue &lt;= ManaLeft`）。
        /// 🔴 **必须与消费端同参**（都传 `targetCard = null`）：不一致就会出现
        /// 「AI 掷了、引擎却按『这次不问』把答案作废」或反之。</para>
        ///
        /// <para>⚠️ 掷骰落在**执行之前**（`PlayCardAsAi`），不是打分/枚举时 ——
        /// 原版也是「牌打出去之后、结算那一刻」才抽（`_ChooseCardMethod` 协程），
        /// 而枚举期掷会让**没被执行的那些候选**白烧随机数。</para>
        /// </summary>
        public static bool DecideOathForAi(BattleContext ctx, int me, AiAction a, CardInstance inst, out bool pay)
        {
            pay = false;
            if (ctx == null || a == null || inst == null || inst.Card == null) return false;
            if (me < 0 || me > 1 || ctx.Players[me] == null) return false;

            var card = inst.Card;
            if (!RuleCore.OathChoiceNeeded(card)) return false;
            if (!RuleCore.NeedsToChooseFromPool(ctx, me, card, null, inst: inst)) return false;   // 🔴 2026-11-01（`B111_E1B`）：补上 `inst` —— 消费端 `DecideOathChoice` 是带 `inst` 判的（按份费用修正才算得准），两边**必须同参**
            if (!RuleCore.CanPayOathChoice(ctx, me, card, inst)) return false;

            // 掷骰**逻辑只此一处**：`RuleCore.RollOathPay`（原版 `GetChoiceOptions` 第 1 圈
            //   `GetRandomInt(0,2)` 消耗 1 格；第 2 圈 `GetRandomInt(0,1)` 命中「不消耗」那一档）。
            // 🔴 2026-11-01（`B111_E1B`）：**产品路径⛔ 别调本方法** —— 掷骰已经挪进引擎的
            //   `RuleCore.DecideOathChoice`（出牌时自己掷）；在这里再掷一次 = 一手烧掉 **2 格**、
            //   随机流当场错开。本方法只剩**自检 / 诊断**用途（`CardPresentation/Editor/B111_E2_Assert.cs` 的 B/C/D/E/F 段）。
            pay = RuleCore.RollOathPay(ctx, card);

            ctx.SetOathAnswer(me, pay);            // 引擎侧 `DecideOathChoice` 用答案（一次一取、同座位）
            a.OathChoiceRecorded = true;           // 随动作过网 / 进录像（`NetProtocol.ToWire` ← `AiAction`）
            a.OathChoice = pay ? 1 : 0;

            ctx.Log($"（誓约二选一：AI 打「{card.Name}」，按原版 `ChoiceOfCardEnemy` 的 bot 支**等概率随机**"
                  + "（预烧随机表按下标取 ⇒ 池下标 0/1 各半；`maxTargets` 2、池 2 项 ⇒ 净消耗 1 格）"
                  + $"⇒ {(pay ? "誓约版" : "平版")}）");
            return true;
        }

        /// <summary>同上（不要 `pay` 的调用方用这个重载）。</summary>
        public static bool DecideOathForAi(BattleContext ctx, int me, AiAction a, CardInstance inst)
        {
            bool pay;
            return DecideOathForAi(ctx, me, a, inst, out pay);
        }

        /// <summary>
        /// `ExecuteActionCore` 的 `PlayCard` 支（🧨 **本类执行 AI 出牌的唯一口** —— 产品走
        /// `NextAction`+`ExecuteAction`、自检与兼容层走 `PlayTurn`，两条都汇到这里）。
        ///
        /// <para>🔴 **2026-11-01（`B111_E1B` · `B-85`/`B-86`）改判**：掷骰**已经挪进引擎**
        /// （`RuleCore.DecideOathChoice`，原版 `GetChoiceOptions` 那一格）⇒ 本方法**不再掷**，
        /// 改成「出牌**之后** peek 这一次**最后用了**的答案」写进 `AiAction`。
        /// 这样 **AI 手 / 玩家手 / 回放落地** 三条路都只走引擎那一个掷骰点、各消耗**正好 1 格**，
        /// 回放的预烧随机流不再与线上错开（旧形状：AI 在 `SimpleAI` 掷、回放不走 `SimpleAI` ⇒ 差 1 格）。</para>
        ///
        /// <para>⚠️ 出牌前先清一次记录（`AiAction` 将来若复用同一个对象，`oathRecorded=true` 会把一次
        /// **没发生过**的选择过网）。⚠️ 引擎拒绝那一手**不记**（`code != OK`）。</para>
        /// </summary>
        static int PlayCardAsAi(BattleContext ctx, int me, AiAction a, int hi)
        {
            if (a != null) { a.OathChoiceRecorded = false; a.OathChoice = 0; }
            if (ctx != null) ctx.ClearOathDecisionRecord();          // 出牌前清一次：残答不许报到下一手

            int code = RuleCore.PlayCard(ctx, me, hi, a.Slot);

            if (code == RuleCodes.OK && ctx != null && a != null)
            {
                bool pay;
                if (ctx.TryPeekLastOathDecision(me, out pay))
                {
                    a.OathChoiceRecorded = true;                     // → `NetProtocol.ToWire` → 线上 / 录像 / 回放
                    a.OathChoice = pay ? 1 : 0;
                }
            }
            return code;
        }
    }
}
