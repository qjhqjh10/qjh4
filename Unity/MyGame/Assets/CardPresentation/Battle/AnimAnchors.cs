// AnimAnchors.cs — 原版 `BattleManager.GetAnimTransform`（`PositionOption` → 锚点）的复刻
//
// ─────────────────────────────────────────────────────────────────────────────
// 【原版是谁 · 第一权威】
//   `d:/2/tools/decomp_full/BattleManager__GetAnimTransform.c`（**全篇 144 行，逐句亲读**）
//   签名：`Transform GetAnimTransform(PositionOption option, CardScript acting, CardScript target)`
//   它是 `AnimInfo` 那三个字段（`startPosOption 0x18` / `endPosOption 0x1C` /
//   `pointTowardsOption 0x20`）**在动画播放那一刻**被解析成「一个锚点 Transform」的地方。
//   ⛔ 别拿 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 那份**签名桩**当判据（方法体是空的）。
//
// 【它只实现了 10 档】`0 self` · `1 cardTarget` · `2 abilityTarget` · `3 friendlyHero` · `4 enemyHero` ·
//   `5 boardCenter` · `14 nextFriendlyMinionSpot` · `18 friendlyHand` · `19 enemyHand` ·
//   `50 friendlyDeck` · `55 enemyDeck`，
//   其余全部 = `CustomDebug.LogError`（「这一档本不该走到」）+ 退回**本地玩家**督军卡的 `EffectAnchor`。
//   ⛔ **别照 `PositionOption` 的 36 个值各写一档** —— 原版自己就只做这 10 档，多写的档反而与原版不一致。
//
// 【逐档出处（行号 = 上面那份 `.c`）】
//   · `0 self`          `:60-63`   → `acting + 0x290 + 0x1a0`（卡自己的 `effectsAnchor`）
//   · `1/2` 目标档       `:64-67`   → `target + 0x290 + 0x1a0`
//   · `3/4` 督军档       `:68-75`   → `PlayerManager + 0x28`（= `heroCard`）→ `CardScript.GetEffectAnchor()`
//   · `5 boardCenter`   `:85-86`   → `BattleManager + 0x1d0`（**裸 `Transform`，直接返回**）
//   · `14` 空位          `:88-112`  → `+0xe0`（= **`playerMinionManager`，本地玩家的**）下**新建一个空
//                                     GameObject**，位置 = `MinionManager.GetNextSlotPos(mouseClickPos,1)`，
//                                     随即 `Object.Destroy(go, …)`（`DAT_1834b2da8`）
//   · `18 friendlyHand` `:114,128-135` → 按行动卡 `+0x40` 二选一（`playerHand` / `enemyHand`）
//   · `19 enemyHand`    `:21-36`   → 同上两个字段、**取反**
//   · `50/55` 牌堆       `:37-57`   → 按同一个 `+0x40` 二选一（`playerDeck` / `enemyDeck`）
//   · 其余              `:114-126` → `LogError` + **本地玩家** `playerManager.heroCard` 的 `GetEffectAnchor()`
//
// 【`+0x40` 是什么 —— 已现核】= `EntityScript.isPlayer`（`il2cpp_out/dump.cs:21963` 起，
//   `0x40` 那行现读 `<isPlayer>k__BackingField`）。语义 = **「这张卡是本地玩家的」**，
//   ⛔ 不是「这张卡是行动方的」。
//
// 🔴 【2026-10-16 就地订正（铁律 5）—— `任务/T05` §2·6③ 那句「18/19 极性相反、原版自己拧着」**不成立**】
//   那句写的是「`18 friendlyHand` 按 `isPlayer==0 ⇒ 0xd8(enemyHand)`；而 `19 enemyHand` **极性相反**
//   （`isPlayer==0 ⇒ 0xd0`）⇒ 同一件事、写法却反了」。
//   **两段逐句重读后是【自洽】的**：`friendly` 指的是**行动方那一侧**、`enemy` 指的是**对面那一侧**，
//   所以「按同一个 `isPlayer` 标志去取」本来就该取反 —— 原版**没有拧**，是那条记录把它读岔了。
//   ⇒ 本实现按「先算**行动方座位**、再按座位取」写（见 <see cref="Ctx.actorSeat"/>），
//     ⛔ **不照那句错的记录各写一档死分支**（照它写至少有一档与原版不一致）。
//   ⚠️ 但 `14` 与「其余（兜底）」这两格**确实**钉死用**本地玩家**（`+0xe0` / `+0xc0`）、**不是**行动方 ——
//     那是原版真的不对称，**照抄**（见 <see cref="Resolve"/> 里那两处的注释），⛔ 别「顺手统一」。
//
// 【返回什么】原版返回的是**一个 `Transform`**（不是坐标）—— 本类照做。
//   ⚠️ 那些锚点**不在同一个空间里**（场上卡 = 竞技场空间；手牌/牌堆 = HUD 空间）——
//     把「锚点 → 我们要播特效的那个平面」的换算**留给消费方**（`BattleDriver.AnimAnchorWorld`），
//     ⛔ 别在本类里偷偷做一次换算：那正是 T05 §2·4 那笔「空间」大账要一次理清的东西。
//
// 🔴🔴 【2026-10-16 补 · 必读 —— 本文件【只】是**兜底**那条路】
//   同一个会话另派的只读代理把「技能那条路」查清了，**推翻了一个前提**
//   （报告 → `d:/4/Unity/资料/普查产出_1016战斗判据/T05b_cardAnim索引与参数喂给谁.md`）：
//   · **技能（ability）路径的解析器是【另一个函数】** —— `AbilityLogic.GetPos`
//     （`d:/2/tools/decomp_full/AbilityLogic__GetPos.c`，331 行，**36 档全实现**）；
//     而 `BattleManager.GetAnimTransform`（**本文件 port 的就是它**）**只是它的兜底**。
//   · 两者三处**实质不同**（逐条出处 → T05b §一 那张对照表）：
//       `0/1/2`  GetPos 是 **state-aware**（`CardScript.GetEffectAnchor()` 按卡状态 `+0x228` 二选一）；
//                本文件照 GetAnimTransform，取的是**不论状态的固定节点** `effectsAnchor`。
//       `14`     GetPos 用**行动方**那一侧（`CreateTempAbilityTransform(isPlayer,…)`）；
//                本文件照 GetAnimTransform，用**本地玩家**（`+0xe0`）。
//       `6-17 / 60-155`  GetPos **有真锚点**；本文件落「报错 + 兜底」那一档。
//   · ⇒ **要接「逐卡演出」那条真路，得先按 `GetPos` 补那 36 档**
//     （缺的锚点 = `BattleHud.GetUIAnchor` 的内部映射，T05b §三 列为「判不了」）。
//     **⛔ 别把本文件当成终点**，也别因为它绿了就以为接完了。
//   ⚠️ 本文件下列判据**仍然成立**、与上面不冲突：`+0x40` = `EntityScript.isPlayer` · `18/19` 两段自洽。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BattleManager.GetAnimTransform` 的复刻。**判据只此一处** —— 调用点别自己 switch。</summary>
    public static class AnimAnchors
    {
        /// <summary>解析的输入。对应原版的三个实参（`option` / `param_3` acting / `param_4` target）
        /// 加上两个「座位」换算量。</summary>
        public struct Ctx
        {
            /// <summary>**行动卡**（原版 `param_3`）。`self` 档要用它。</summary>
            public CardView self;
            /// <summary>**目标卡**（原版 `param_4`）。`cardTarget` / `abilityTarget` 两档要用它。</summary>
            public CardView target;
            /// <summary>**行动方座位号**。原版没有这个量，它是从 `acting + 0x40`（`isPlayer`，**本地玩家相对**）
            /// 推出来的：行动方就是「`isPlayer` 为真则本地玩家、否则对面」。
            /// ⚠️ 别把「行动方」直接当成「本地玩家」—— 对手出牌时两者是反的。</summary>
            public int actorSeat;
            /// <summary>**本地玩家座位号**。`14` 那一档与兜底那一档原版钉死用本地玩家（见文件头）。</summary>
            public int mySeat;
        }

        // ── 解析口（由 `BattleDriver.HookAnimAnchors()` 挂上；只有驱动知道视图/棋盘/手牌在哪）──
        //    每个都是 `座位 → 锚点`。座位取不到（关着的、没这个座位）⇒ 返回 null 并**出声**。

        /// <summary>某座位的**督军卡**的 `GetEffectAnchor()`（原版 `3/4` 两档 + 兜底那一档）。</summary>
        public static Func<int, Transform> HeroAnchorOf;
        /// <summary>某座位的**手牌容器**的 transform（原版 `18/19` 两档）。</summary>
        public static Func<int, Transform> HandAnchorOf;
        /// <summary>某座位的**牌堆**的 transform（原版 `50/55` 两档）。</summary>
        public static Func<int, Transform> DeckAnchorOf;
        /// <summary>棋盘中心那颗**裸 Transform**（原版 `5 boardCenter` = `BattleManager + 0x1d0`）。</summary>
        public static Func<Transform> BoardCenterOf;
        /// <summary>某座位「下一个空位」的锚点（原版 `14`）。</summary>
        public static Func<int, Transform> NextMinionSpotOf;

        // ── 诊断（自检盯它们；**不许静默失败**）──
        /// <summary>解析过多少次。</summary>
        public static int Calls { get; private set; }
        /// <summary>其中多少次落进「其余 = 报错 + 兜底」那一档。</summary>
        public static int Fallbacks { get; private set; }
        /// <summary>最后一次落兜底的是哪一档（数值 + 名字）。</summary>
        public static string LastFallback { get; private set; }
        /// <summary>最后一次因为「锚点取不到」而返回 null 的原因（**出声**用）。</summary>
        public static string LastMissing { get; private set; }

        /// <summary>每次开一段断言/一局之前清零。</summary>
        public static void ResetCounters()
        {
            Calls = 0; Fallbacks = 0; LastFallback = null; LastMissing = null;
        }

        /// <summary>把 10 档 + 兜底解析成一个锚点。取不到 ⇒ 返回 null（并**出声**，⛔ 不静默）。
        /// ⚠️ 原版取不到时走的是 `FUN_1803f47a0()`（**抛异常**，反编译标了 `Subroutine does not return`）——
        /// 我们**不抛**（会把一局打断），改成「记一行 + 返回 null」，由消费方决定不播。这是**我们改的**。</summary>
        public static Transform Resolve(PositionOption opt, Ctx ctx)
        {
            Calls++;

            switch (opt)
            {
                // 0 self —— 原版 `:60-63`：行动卡 `+0x290 (+0x1a0)`
                //   `+0x1a0` = `BattleCardUI.effectsAnchor`（**不论手牌/场上都是这一颗**，
                //   不是 `GetEffectAnchor()` 那两支）⇒ 我们这边 = `EffectAnchorBoard`。
                case PositionOption.self:
                    return CardAnchorBoard(ctx.self, opt);

                // 1 cardTarget / 2 abilityTarget —— 原版 `:64-67`：同上，换成 `param_4`（目标卡）。
                case PositionOption.cardTarget:
                case PositionOption.abilityTarget:
                    return CardAnchorBoard(ctx.target, opt);

                // 3 friendlyHero / 4 enemyHero —— 原版 `:68-75`：`PlayerManager+0x28`（`heroCard`）
                //   走 `CardScript.GetEffectAnchor()`（**按卡状态二选一**）。督军恒在场上 ⇒ 场上档。
                case PositionOption.friendlyHero:
                    return Hero(ctx.actorSeat, opt);
                case PositionOption.enemyHero:
                    return Hero(1 - ctx.actorSeat, opt);

                // 5 boardCenter —— 原版 `:85-86`：`BattleManager+0x1d0`，**裸 Transform 直接返回**
                //   （不判空、不走任何转换）。
                case PositionOption.boardCenter:
                    if (BoardCenterOf != null)
                    {
                        var t = BoardCenterOf();
                        if (t != null) return t;
                    }
                    return Missing("boardCenter", "`BoardCenterOf` 没挂 或 它回了 null");

                // 14 nextFriendlyMinionSpot —— 原版 `:88-112`：**本地玩家**的 `MinionManager`（`+0xe0`）
                //   ⚠️ 钉死是 `playerMinionManager`，**不是行动方**的（对手用这一档时也取本地那一侧）—— 照抄。
                case PositionOption.nextFriendlyMinionSpot:
                    if (NextMinionSpotOf != null)
                    {
                        var t = NextMinionSpotOf(ctx.mySeat);
                        if (t != null) return t;
                    }
                    return Missing("nextFriendlyMinionSpot", "`NextMinionSpotOf` 没挂 或 它回了 null");

                // 18 friendlyHand / 19 enemyHand —— 原版 `:21-36` / `:114,128-135`：
                //   按行动卡的 `isPlayer` 二选一；两档**互取反**（friendly = 行动方那一侧）。
                //   ⚠️ 两档都只取**手牌容器自己的 transform**（**不**走 `GetEffectAnchor`）。
                case PositionOption.friendlyHand:
                    return Hand(ctx.actorSeat, opt);
                case PositionOption.enemyHand:
                    return Hand(1 - ctx.actorSeat, opt);

                // 50 friendlyDeck / 55 enemyDeck —— 原版 `:37-57`：同一个 `isPlayer` 标志，两个字段互取反。
                case PositionOption.friendlyDeck:
                    return Deck(ctx.actorSeat, opt);
                case PositionOption.enemyDeck:
                    return Deck(1 - ctx.actorSeat, opt);
            }

            // 其余（`6`–`13`、`15`–`17`、`60`+ …）—— 原版 `:114-126`：
            //   `CustomDebug.LogError(<这档本不该走到>)` + 退回**本地玩家**督军卡的 `EffectAnchor`。
            //   🔴 实测数据里真出现过的 `12`/`13`（能量）/`15`（敌方空位）**本来就落这一档** ⇒ 不是我们缺件。
            Fallbacks++;
            LastFallback = (int)opt + " " + opt;
            Debug.LogError($"[AnimAnchors] `PositionOption.{opt}`（{(int)opt}）**不在原版实现的那 10 档里** —— "
                         + "原版这一档也是 `LogError` + 退回**本地玩家**督军的 `EffectAnchor`"
                         + "（判据：`BattleManager__GetAnimTransform.c:114-126`）。我们照做。");
            return Hero(ctx.mySeat, opt);
        }

        /// <summary>`0/1/2` 三档：卡的 `effectsAnchor`（原版 `+0x290 +0x1a0`）= 我们的 `EffectAnchorBoard`。
        /// ⚠️ 原版对 `acting` / `target` 为空是**抛异常**；我们出声 + 返回 null。</summary>
        static Transform CardAnchorBoard(CardView card, PositionOption opt)
        {
            if (card == null)
                return Missing(opt.ToString(), "那张卡（行动卡 or 目标卡）是 null");
            return card.EffectAnchor(true);
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

        static Transform Missing(string what, string why)
        {
            LastMissing = what + "：" + why;
            Debug.LogError($"[AnimAnchors] `{what}` 锚点取不到（{why}）⇒ 返回 null、这一件特效不播"
                         + "（⛔ 不退回某个默认点 —— 猜错就是播错地方；原版这一格是抛异常）。");
            return null;
        }
    }
}
