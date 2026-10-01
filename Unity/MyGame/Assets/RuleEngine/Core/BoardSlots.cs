// BoardSlots.cs — **棋盘是「连续无洞」的**：每一侧都是一条会补位的列表，照原版 `MinionManager`。
//
// 🔴 **为什么要单开一个文件**（2026-10-01 定案）：原版的棋盘**不是九宫格**，是
//    **每侧一份 `List<CardScript>`**（`leftMinions` / `rightMinions`，`dump.cs:39326-39329`）
//    ＋ 一份平行的 `List<bool>` 占位表：
//
//      · **放牌 = `Insert`**（`MinionManager__InsertMinion.c:47-59` 亲读）——
//        按格号算出**该侧的下标**，`List.Insert(侧表, idx, 卡)` ⇒ **插入点之后的单位整体外移一格**；
//      · **离场 = `List.Remove`**（`MinionManager__RemoveMinion.c:35-36,50-51`）——
//        后面的单位**整体内移一格补位**（列表恒为**连续无洞**）；
//      · 每次增删之后都跟一次 `RefreshOccupationSlots`（`:58` / `:58`），
//        再由 `ReassembleMinions`（`MinionManager__ReassembleMinions.c:97,154`）把**每个下标的单位
//        摆回该下标的槽位** ⇒ 这就是我们在画面上看到的「让位」。
//
//    ⇒ **格与格之间不可能留洞**：一侧有 n 个单位，它们永远占着**贴督军那 n 格**。
//      我们原来是「固定 9 格数组、允许留洞」（`PlayerState.Board` 只写 `= null` / `= unit`，
//      全库没有任何位移代码）—— 那会**多出原版做不出来的局面**（例如左侧只在 0 号格站一个人，
//      2/3 号格空着；在原版里那个单位必然站在 3 号格，而且它和谁都相邻）。
//
// 🔴 **格号 ↔ 下标的映射**（本文是唯一出处）：
//     · 我们：`0 1 2 3 [4=督军] 5 6 7 8`
//     · 原版：**有符号下标** —— `+1..+4` = 右侧（从贴督军那格往外数）、`-1..-4` = 左侧、`0` = 督军格
//       （`MinionManager__AdjustedSlot.c:77-89` 的 `~slot` / `slot-1` 就是这个编码）
//     · 两者只差一个 `+4`：**`ourSlot = 4 + origSlot`** ⇒ 侧 = 符号，下标 = `|origSlot| - 1`。
//
// ⚠️ **督军格（4 号）不是「不能放」，是「拖动落点算不出两侧时的那条退化路」**：
//    原版 `AdjustedSlot` 见 `slot == 0` 会 `LogWarning` 然后**挑人数少的那一侧**、
//    平手走右侧（`MinionManager__AdjustedSlot.c:45-61`）——
//    同一个选择在 `GetNextSlotWithoutDisplacing`（免费部署 / 抢单位）里也是这一条
//    （`…GetNextSlotWithoutDisplacing.c:14-30`：左 < 右 ⇒ 放左，否则放右）。
//    ⇒ 我们照它：`Resolve` 对 4 号格给出「人数少的那一侧的最外一格」。
//
// 🔴 **2026-10-01 晚：`AdjustedSlot` 的字段逐条坐实了**（第一权威 `d:/2/tools/decomp_full/MinionManager__AdjustedSlot.c`，
//    方法体完整）。**结论推翻了我们原来的猜测**（「那六个偏移决定拖到两单位之间算左还是算右」—— 不是）：
//      · `+0x20/0x28/0x30/0x38` = `left/rightSlotPos{Normal,Weapon}` 四个 `Vector2[]` —— 全文**只读 `.Length`**（`:24-39`），
//        即**两侧容量**；元素坐标**一次都没读**。`+0x40 isWeaponSetup` 只是「用哪一套数组」。
//      · `+0x60/0x68` = `left/rightMinions`（`List<CardScript>`）—— 读 `.Count` = 人数。
//      · `+0x70/0x78` = `left/rightMinionsOccupation`（`List<bool>`）—— 读 `.Count` = 已占格数（与人数恒等）。
//      ⇒ **它们不参与「算左还是算右」**：侧完全由**符号**定、下标由 `|slot|-1` 定，
//        唯一的判定是「下标 > 该侧人数 ⇒ 夹到该侧最外」。
//      ⇒ **「拖到两个单位之间算哪边」是【上游】的事**：`GetClosestAvailableSlot`
//        （`MinionManager__GetClosestAvailableSlot.c:95-143`）在**屏幕坐标**里对**合法插入区间**
//        （左 `-(左人数+1)..-1`、右 `+1..+(右人数+1)`）求最近 ⇒ **满的那一侧根本不在候选里**。
//      ⇒ 🔴 **顺带查出一处实现差异（已改）**：**请求的那一侧满了**时，原版**换到对侧最外那一格**
//        （`:64-72` / `:104-107`），不是拒绝；我们原来返回 false（打不出去）。见 `Resolve` 的注释。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>棋盘的「连续无洞」模型 —— 原版那两条 `List` 的等价物。</summary>
    public static class BoardSlots
    {
        public const int Left = -1;
        public const int Right = 1;

        /// <summary>这一格在哪一侧：<see cref="Left"/> / <see cref="Right"/> / 0 = 督军格。</summary>
        public static int SideOf(int slot)
        {
            if (!BoardSpec.IsValid(slot)) return 0;
            return slot == BoardSpec.WarlordSlot ? 0 : (slot < BoardSpec.WarlordSlot ? Left : Right);
        }

        /// <summary>这一格是**本侧的倒数第几格**（0 = 贴督军，3 = 最外）。非两侧格返回 -1。</summary>
        public static int IndexOf(int slot)
        {
            int side = SideOf(slot);
            if (side == 0) return -1;
            return System.Math.Abs(slot - BoardSpec.WarlordSlot) - 1;
        }

        /// <summary>本侧第 <paramref name="index"/> 格（0 = 贴督军）的**绝对格号**。</summary>
        public static int SlotOf(int side, int index)
        {
            return BoardSpec.WarlordSlot + side * (index + 1);
        }

        /// <summary>这一侧场上有几个单位（含残骸 —— 残骸在原版里也占着列表的一格）。</summary>
        public static int CountOnSide(PlayerState ps, int side)
        {
            int n = 0;
            for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                if (ps.Board[SlotOf(side, i)] != null) n++;
            return n;
        }

        /// <summary>这一侧还能再放几个。</summary>
        public static int RoomOnSide(PlayerState ps, int side)
        {
            return BoardSpec.SlotsPerSide - CountOnSide(ps, side);
        }

        /// <summary>
        /// **把「想放这一格」翻译成「插到哪一侧的第几个下标」** —— 判据只此一处。
        ///
        /// 返回 false = 真的放不下（**两侧都满**）。三条分支，逐条照原版 `AdjustedSlot`：
        ///   · 请求格在某一侧、**那一侧没满** ⇒ 就用请求的下标，
        ///     超出当前人数的请求**夹到最外**（`AdjustedSlot.c:81-96` 的 `GetNextLeftSlot` / 「返回请求值」两条分支）；
        ///   · 请求格在某一侧、**那一侧满了** ⇒ 🔴 **换到对侧最外那一格**（追加），
        ///     不是「拒绝」（`AdjustedSlot.c:64-72` 右满 ⇒ 返回 `~leftMinions.Count`；
        ///     `:104-107` 左满 ⇒ 返回 `rightOccupation.Count + 1`）。
        ///     ⚠️ 2026-10-01 之前我们这里返回 false（= 打不出去），**那是错的** —— 已按原版改；
        ///   · 请求督军格（或越界）⇒ 退化路：**挑人数少的一侧、平手走右**（`AdjustedSlot.c:45-61`），
        ///     落到该侧最外那一格。
        ///
        /// ⚠️ **全盘满**时原版是 `LogError` 之后**原样返回请求格**（`:38-44`）—— 那会让
        ///    `InsertMinion` 算出越界下标，说明**调用方事先用 `SpacesInTheBoard` 筛过**；
        ///    我们这里返回 false（= 打不出去），失败形态不同但**不可观测**（调用方同样会先问）。
        ///
        /// ⚠️ 「拖到**两个单位之间**算左边还是右边」**不在这个函数里** —— 那是**上游**在
        ///    **屏幕坐标**上对「合法插入格」求最近（`GetClosestAvailableSlot.c:95-143`，
        ///    候选范围已经排除了满的那一侧）⇒ 到不了这里。详见文件头那张表。
        /// </summary>
        public static bool Resolve(PlayerState ps, int requestedSlot, out int side, out int index)
        {
            int reqSide = SideOf(requestedSlot);
            if (reqSide == 0)
            {
                // 退化路（督军格 / 越界）：原版 `AdjustedSlot.c:45-61` —— 左 < 右 ⇒ 左；否则右（平手也走右）
                int l = CountOnSide(ps, Left), r = CountOnSide(ps, Right);
                side = l < r ? Left : Right;
                if (CountOnSide(ps, side) >= BoardSpec.SlotsPerSide) { index = 0; return false; }  // 两侧都满
                index = CountOnSide(ps, side);     // 该侧最外那一格
                return true;
            }

            side = reqSide;
            int count = CountOnSide(ps, side);
            if (count >= BoardSpec.SlotsPerSide)
            {
                // 🔴 请求的那一侧满了 ⇒ **换到对侧最外那一格**（原版 `AdjustedSlot.c:64-72 / :104-107`）
                int other = -side;
                int otherCount = CountOnSide(ps, other);
                if (otherCount >= BoardSpec.SlotsPerSide) { index = 0; return false; }   // 全盘满
                side = other;
                index = otherCount;                // 追加 = 对侧最外
                return true;
            }

            index = IndexOf(requestedSlot);
            if (index < 0) index = 0;
            if (index > count) index = count;      // 夹到最外那一格（= 追加）
            return true;
        }

        /// <summary>
        /// 🔴 **不变量守卫**（2026-10-01 加）：引擎自己的每一条增删路径都会维持「每侧连续无洞」，
        /// 所以**真对局里 `Board[]` 不可能出现洞**。能造出洞的只有**自检夹具**
        /// （它们直接写 `Board[x] = …`，而且常常图省事写在 1 / 2 号格上 —— 那两格是「左侧第 3 / 第 2 格」，
        /// 里面还空着就成洞了）。
        ///
        /// 有洞时怎么办 —— **降级成老行为 + 大声报错**，两条都是刻意的：
        ///   · **降级**：有洞的棋盘上「补位」没有唯一说得通的语义（任何选择都要挪动某些单位），
        ///     而强行压实会把夹具**悄悄换成另一个局面** —— 2026-10-01 实测：压实之后
        ///     `TestStealthUntargetable` 里那只隐身单位被挪走，后面几条断言全崩、还看不出根因。
        ///     ⇒ 宁可按**逐格**处理（= 我们改模型之前的行为）。
        ///   · **报错**：这是「不许静默失败」那条红线的落地 —— 日志里出现 `[BoardSlots]`，
        ///     就该去把那个夹具改成合法局面（**只有合法局面的语义才是原版的语义**）。
        /// </summary>
        static void WarnHoley(PlayerState ps, string where)
        {
            string why;
            IsContiguous(ps, out why);
            UnityEngine.Debug.LogError($"[BoardSlots] ⚠️ 棋盘不连续（{where}）：{why}"
                + " —— 引擎的增删路径都维持「每侧从贴督军那格起连续无洞」，出现洞说明**有地方（多半是自检夹具）"
                + "直接写了 `Board[]`**。本次**按老行为逐格处理**（不补位）；请把那个夹具改成合法局面。");
        }

        /// <summary>只问「放得下吗」，不关心落到哪一格 —— `CanPlayCard` 用。</summary>
        public static bool HasRoomFor(PlayerState ps, int requestedSlot)
        {
            int side, index;
            return Resolve(ps, requestedSlot, out side, out index);
        }

        /// <summary>
        /// **插入**：把 <paramref name="u"/> 放到「请求格」对应的下标上，**它之后的单位整体外移一格**
        /// （原版 `List.Insert` 的语义）。返回它真正落在的格号；放不下时返回 -1（不动棋盘）。
        /// </summary>
        public static int Insert(PlayerState ps, UnitState u, int requestedSlot)
        {
            if (!IsContiguous(ps, out _))
            {
                // 有洞 ⇒ 降级（见 `WarnHoley`）：**请求哪一格就写哪一格**（空着才行），不推别人
                WarnHoley(ps, "Insert");
                if (BoardSpec.IsValid(requestedSlot) && requestedSlot != BoardSpec.WarlordSlot
                    && ps.Board[requestedSlot] == null)
                {
                    ps.Board[requestedSlot] = u;
                    return requestedSlot;
                }
            }
            int side, index;
            if (!Resolve(ps, requestedSlot, out side, out index)) return -1;

            int count = CountOnSide(ps, side);
            // 从最外那一个开始倒着搬，免得覆盖（外移 = 下标 +1 = 离督军更远）
            for (int i = count - 1; i >= index; i--)
                ps.Board[SlotOf(side, i + 1)] = ps.Board[SlotOf(side, i)];

            int landed = SlotOf(side, index);
            ps.Board[landed] = u;
            return landed;
        }

        /// <summary>
        /// **摘掉**某一格上的单位，**后面的单位整体内移一格补位**（原版 `List.Remove` 的语义）。
        /// 返回被摘掉的那个（格上本来没人时返回 null）。
        /// ⚠️ 督军格不动（督军不离场，`RuleCore.CleanupDeaths` 那条 `if (u.IsWarlord) return` 管着）。
        /// </summary>
        public static UnitState RemoveAt(PlayerState ps, int slot)
        {
            int side = SideOf(slot);
            if (side == 0) return null;
            var u = ps.Board[slot];
            if (u == null) return null;
            if (!IsContiguous(ps, out _))
            {
                // 有洞 ⇒ 降级（见 `WarnHoley`）：**只把那格置空，别人的格号不动**（= 我们改模型之前的行为）
                WarnHoley(ps, "RemoveAt");
                ps.Board[slot] = null;
                return u;
            }

            int index = IndexOf(slot);
            int count = CountOnSide(ps, side);
            ps.Board[slot] = null;
            for (int i = index + 1; i < count; i++)
                ps.Board[SlotOf(side, i - 1)] = ps.Board[SlotOf(side, i)];
            ps.Board[SlotOf(side, count - 1)] = null;    // 最外那一格空出来
            return u;
        }

        /// <summary>
        /// **「不挤别人」的下一格** —— 原版 `GetNextSlotWithoutDisplacing`：
        /// **人少的那一侧的最外一格**（平手走右）。免费部署 / 抢来的单位 / 还回去都走这一条
        /// （`_ResolveSummonUnit` 与 `_ResolveStealMinion` 都在用）。
        /// 满场时返回 -1。
        /// </summary>
        public static int NextWithoutDisplacing(PlayerState ps)
        {
            int l = CountOnSide(ps, Left), r = CountOnSide(ps, Right);
            int side = l < r ? Left : Right;
            if (CountOnSide(ps, side) >= BoardSpec.SlotsPerSide) return -1;
            return SlotOf(side, CountOnSide(ps, side));
        }

        /// <summary>
        /// **自检用**：这一方的棋盘是不是「每侧连续、贴督军起无洞」。
        /// 只应该在断言里被调用 —— 引擎自己每一处增删都走 <see cref="Insert"/> / <see cref="RemoveAt"/>，
        /// 不变量自然成立；**夹具**（自检里直接 `Board[x] = …` 的地方）是唯一可能破坏它的地方。
        /// </summary>
        public static bool IsContiguous(PlayerState ps, out string why)
        {
            why = null;
            if (ps.Board[BoardSpec.WarlordSlot] == null || !ps.Board[BoardSpec.WarlordSlot].IsWarlord)
            { why = "督军不在 4 号格"; return false; }
            for (int s = 0; s < 2; s++)
            {
                int side = s == 0 ? Left : Right;
                int count = CountOnSide(ps, side);   // 人数 = 非空格数 ⇒ 只要「连续段之外还有人」就是洞
                for (int i = count; i < BoardSpec.SlotsPerSide; i++)
                    if (ps.Board[SlotOf(side, i)] != null)
                    { why = $"{(side == Left ? "左" : "右")}侧 {SlotOf(side, i)} 号格有人，但它在连续段之外（有洞）"; return false; }
            }
            return true;
        }
    }
}
