// ManaBuffDeadBranch_B116W41.cs — `ENG-07` 的 `psyker`(620) / `reactor`(625) 两条**死支照抄**
//
// ============================ 原版整条链（逐句读过 `d:/2/tools/decomp_full/`）============================
//   ① **门 + 侧别**：`CardScript__OnTurnStart.c:177`（psyker）/ `:218`（reactor）——
//      卡状态 ∈ {2, 0xf, 3, 0x11}（= 在场）∧ `get_CurrentPsyker() > 0`（reactor 同形）；
//      侧别守卫在 `:101-103`：`param_2 != isPlayer(+0x40) ⇒ return` ⇒ **只有行动方**的单位加。
//   ② **读值**：`EntityScript__get_CurrentPsyker.c:5` = `GetCurrentTraitValueWithModifiers(0x26c)`
//      （reactor = `__get_CurrentReactor.c:5`，`0x271`）—— 带加值的**当前** trait 值。
//   ③ **表现**：`OnTurnStart.c:190-213` / `:231-252` 两段 `AddCardAnim`（卡 → `ManaManager.psychicManaText`，
//      见 `PlayerManager__GetPsychicManaTransform.c:7-9` 指向 `ManaManager +0x40`；plasma 指 `+0x48`）。
//   ④ **写 flag**：`BattleManager__AddPsychicMana.c:29-44` / `__AddPlasmaMana.c:29-44` 造
//      `ManaBuff{ manaAmount = 值, friendlyPlayer = 1, psychicMana(+0x19) = 1 }`（plasma 是 `+0x1a`），
//      交 `AddManaBuff`（action `0x22`）。
//   ⑤ **消费端**：🔴 `BattleManager__ProcessManaBuff.c:27-49` **只判** `+0x16 createMana` /
//      `+0x17 refillMana` / `+0x1b spiritStone` / `+0x1c faith` / `+0x1d questPoints` ——
//      **`+0x19` / `+0x1a` 一次都没读** ⇒ 两个 flag 全 0 的默认支里循环 `manaAmount` 次**什么都不做**。
//
//   ⇒ **整条链在原版里净效果 = 零**（只剩两段表现动画）。按 18·0「照抄原版、连死支一起照抄」，
//     本文件把它照抄成**死支**：flag 写进去、`Process` **不读它**（与 `ProcessManaBuff` 同形）；
//     池本体（`PlayerState.PsychicMana` / `PlasmaMana` = 原版 `+0x74` / `+0x88`）建出来但没有写点。
//
// ============================ 三条红线 ============================
//   ⛔ **不要**把它做成「回合开始 +N 灵能」—— 原版**不加**（`ProcessManaBuff` 不读那两个 flag），
//     加进池里就是**造出原版没有的行为**（`18·0`）。
//   ⛔ **不要**接 `ActiveAbilityCurrencyGate`（`RuleCore.cs:17716`）—— 那是**另一条**（主动能力付费闸，
//     已经在跑），两条的判据不同，混起来会互相污染。
//   ⛔ **不要**在这里复制 `createMana` / `refillMana` / `spiritStone` / `faith` / `questPoints` 五格的实作
//     —— 它们在既有落点（`EffectResolver.DoEnergy` / `DoRefill` / `DoFactionResource`）。本口只服务
//     psyker/reactor 那一条（五格全 false），撞上就**拒绝**并说清，⛔ 不静默空转。
//
// ============================ 现读面（2026-11-02 现核）============================
//   · 池内带 `psyker` / `reactor` 的卡 = **0 / 0**（`cards_engine.json` 的 `keywords` + `cardTraits` 两列）；
//   · 池内 `activeAbilityPsychicCost` / `activeAbilityPlasmaCost` = **1129/1129 = 0**
//     （`报告/B116_R7_ENG07剩余四条施工面.md` §2·3 现读；旧口径「87 张全 0」复现不出）；
//   · ⇒ 本文件对现池**零行为变化**（`play all dump` 逐字节不变），只是把「将来一有卡就静默失效」堵上。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `ManaBuff`（`BattleManager__AddPsychicMana.c:29-44` / `__AddPlasmaMana.c:29-44` 造的那个对象，
    /// 字段序号 → `ProcessManaBuff.c:27-49` 逐格读的那几个字节）。</summary>
    /// <remarks>⚠️ 只有 psyker/reactor 这一条生产者在用；⛔ 别拿它当通用「法力包」去接别的卡面效果。</remarks>
    public sealed class ManaBuff
    {
        /// <summary>`manaAmount`（`+0x18`）。</summary>
        public int ManaAmount;
        /// <summary>`friendlyPlayer`（`+0x14`）—— 收资源那一方是不是本机玩家。</summary>
        public bool FriendlyPlayer;
        /// <summary>`createMana`（`+0x16`）。</summary>
        public bool CreateMana;
        /// <summary>`refillMana`（`+0x17`）。</summary>
        public bool RefillMana;
        /// <summary>🔴 `psychicMana`（`+0x19`）—— **原版 `ProcessManaBuff` 一次都不读**（本文件照抄这条死支）。</summary>
        public bool PsychicMana;
        /// <summary>🔴 `plasmaMana`（`+0x1a`）—— 同上，没人读。</summary>
        public bool PlasmaMana;
        /// <summary>`spiritStoneMana`（`+0x1b`）。</summary>
        public bool SpiritStone;
        /// <summary>`faithMana`（`+0x1c`）。</summary>
        public bool Faith;
        /// <summary>`questPoints`（`+0x1d`）。</summary>
        public bool QuestPoints;
        /// <summary>造这个 buff 的单位名（**我们加的**：只为日志/断言可读，原版没有这一格）。</summary>
        public string Source;
    }

    /// <summary>`ENG-07` `psyker`(620) / `reactor`(625) 的**死支**：写 flag（<see cref="AddPsychicReactor"/>）＋
    /// 一个**不读那两个 flag** 的消费端（<see cref="Process"/>）。判据与红线 → 文件头。</summary>
    public static class ManaBuffProcess
    {
        /// <summary>原版 `DefinedTrait.psyker = 620`。</summary>
        public const string Psyker = "psyker";
        /// <summary>原版 `DefinedTrait.reactor = 625`。</summary>
        public const string Reactor = "reactor";

        /// <summary>原版 `EntityScript.get_CurrentPsyker / get_CurrentReactor`
        /// （`GetCurrentTraitValueWithModifiers(0x26c / 0x271)`）的等价读数。
        /// <para>**形状**与 `UnitState.HasCurrentTrait`（`B116_W19` 收口的那一条）**同一口径**：
        /// 先看运行期关键词（= 「当前表」那半，含授予与加值），再退回卡面 `cardTraits` 的值；
        /// 判不了（没喂 `cardTraits`）⇒ `0`（⛔ 不猜）。</para>
        /// <para>⚠️ 已知近似：原版 `GetCurrentTraitValueWithModifiers` 是「基值 + 修正」的**合成**，
        /// 我们是「运行期有就用运行期的、否则用卡面基值」⇒ 两者相加那种局面读出来是**较小**的那个。
        /// 池内 0 张卡带这两个词 ⇒ 今天不可观测（如实标，⛔ 不写成「等价」）。</para></summary>
        public static int CurrentTraitValueOf(UnitState u, string trait)
        {
            if (u == null || string.IsNullOrEmpty(trait)) return 0;
            int v = u.KwValue(trait);
            if (v > 0) return v;
            if (!u.HasCurrentTrait(trait)) return 0;
            string why;
            return u.Card != null ? u.Card.GetDefaultTraitValue(trait, out why) : 0;
        }

        /// <summary>🔴 **生产侧**：原版 `CardScript.OnTurnStart` 的 psyker(`:177`) / reactor(`:218`) 那两跳。
        ///
        /// <para>**闸**（逐条照抄）：① 侧别 —— **只扫行动方**（`OnTurnStart.c:101-103` 的
        /// `param_2 != isPlayer ⇒ return`；判别式：改成扫双方 ⇒ 断言 B3 红）；
        /// ② 在场（`cardState ∈ {2,0xf,3,0x11}` ⇒ 我们 `u != null && u.IsAlive`）；
        /// ③ 值 `> 0`（`get_CurrentPsyker() > 0`）。</para>
        ///
        /// <para>**返回**本次造出来的 buff（生产侧丢弃返回值）。返回它是为了让断言能证明
        /// 「flag **真的写进去了**」—— 那正是 <see cref="Process"/> 之后**没人读**的那两格。</para>
        ///
        /// <para>⚠️ 督军也扫（原版 `OnTurnStart` 是逐卡遍历、督军同样有卡）—— 与 `GrantPrayEnchantmentFaith`
        /// 的「棋盘 + 督军各一次」同形。</para></summary>
        public static List<ManaBuff> AddPsychicReactor(BattleContext ctx, int owner)
        {
            var made = new List<ManaBuff>();
            if (ctx == null || owner < 0 || owner >= ctx.Players.Length) return made;
            var ps = ctx.Players[owner];
            if (ps == null) return made;
            var board = ps.Board;
            for (int s = -1; s < board.Length; s++)   // -1 = 督军（不在 Board 里），0.. = 九格
            {
                var u = s < 0 ? ps.Warlord : board[s];
                if (u == null || !u.IsAlive) continue;                       // ② 在场
                int psy = CurrentTraitValueOf(u, Psyker);
                if (psy > 0) made.Add(Take(ctx, owner, new ManaBuff { ManaAmount = psy, FriendlyPlayer = true, PsychicMana = true, Source = u.Name }));
                int rea = CurrentTraitValueOf(u, Reactor);
                if (rea > 0) made.Add(Take(ctx, owner, new ManaBuff { ManaAmount = rea, FriendlyPlayer = true, PlasmaMana = true, Source = u.Name }));
            }
            return made;
        }

        /// <summary>原版 `AddManaBuff`（action `0x22`）→ `ResolveManaBuff` → <see cref="Process"/> 那一跳的等价物：
        /// 造好的 buff **立刻**交给消费端（我们没有动作队列这一步，`ActionPump` 那条链对死支不值当）。</summary>
        static ManaBuff Take(BattleContext ctx, int owner, ManaBuff b)
        {
            Process(ctx, owner, b);
            return b;
        }

        /// <summary>原版 `BattleManager.ProcessManaBuff`（`ProcessManaBuff.c:27-49`）的**可判子集**。
        ///
        /// <para>🔴 **它读哪几格**：`createMana` / `refillMana` / `spiritStone` / `faith` / `questPoints`
        /// —— 这五格在今天这条链上是**恒 false**（<see cref="AddPsychicReactor"/> 只设 psychic/plasma 两格），
        /// 撞上就 `return false` 并说明「实作在既有落点，⛔ 不在这里复制第二份」。</para>
        ///
        /// <para>🔴 **它不读哪两格**：`PsychicMana(+0x19)` / `PlasmaMana(+0x1a)` —— **原版一次都不读**
        /// ⇒ 走默认支：循环 `manaAmount` 次，什么都不做（池一个字节都不动）。这就是本文件存在的**全部**
        /// 意义：让「写了 flag」和「没人读」两件事在代码里**同时可证**。</para></summary>
        public static bool Process(BattleContext ctx, int owner, ManaBuff b)
        {
            if (b == null || b.ManaAmount <= 0) return false;
            if (b.CreateMana || b.RefillMana || b.SpiritStone || b.Faith || b.QuestPoints)
                return false;      // 五格的实作在 EffectResolver.DoEnergy / DoRefill / DoFactionResource
            // 🔴 原版 `ProcessManaBuff.c:27-49` 的**默认支**：`+0x19` / `+0x1a` 不参与判序 ⇒ 空转。
            for (int i = 0; i < b.ManaAmount; i++) { }
            return true;
        }
    }
}
